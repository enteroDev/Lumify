using lumify.api.Logic;
using lumify.api.Models.Context;
using lumify.api.Models.Settings;
using lumify.api.Interfaces;
using lumify.api.Services;
using lumify.api.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;



// --------------- //
// --- BUILDER --- //
// --------------- //
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<InternalLogic>();
builder.Services.AddScoped<FriendshipService>();
builder.Services.AddSingleton<IPresenceService, PresenceService>();
builder.Services.AddSignalR();


// --- CORS --- //
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5333",
                "https://localhost:5333",
                "http://192.168.0.12:5333",
                "https://192.168.0.12:5333"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});



// --- LOAD JWT --- //
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);

var jwt = jwtSection.Get<JwtSettings>();

if (string.IsNullOrEmpty(jwt.Secret))
{
    throw new InvalidOperationException("JWT Secret is not configured");
}

var key = Encoding.UTF8.GetBytes(jwt.Secret);


// --- AUTHENTICATION + JWT --- //
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("session_token", out var cookieToken) &&
                    !string.IsNullOrWhiteSpace(cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddAuthorization();


// --- EF --- //
builder.Services.AddDbContext<LumifyDbContext>(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("LumifyDb"));
});



// ----------- //
// --- APP --- //
// ----------- //
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

var avatarFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data", "avatars");
if (!Directory.Exists(avatarFolder)) Directory.CreateDirectory(avatarFolder);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(avatarFolder),
    RequestPath = "/Data/avatars",
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx =>
    {
        // Safety Header
        ctx.Context.Response.Headers["Cache-Control"] = "no-store";
    }
});

app.UseCors("DevCors");
app.UseMiddleware<CsrfMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<TodoHub>("/hubs/todos");
app.MapHub<NoteHub>("/hubs/notes");
app.MapHub<EventHub>("/hubs/events");
app.MapHub<WorkspaceHub>("/hubs/workspaces");
app.MapHub<ChatHub>("/hubs/chat");

app.Run();