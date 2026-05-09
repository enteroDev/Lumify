using lumify.api.Logic;
using lumify.api.Models.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text;



// --------------- //
// --- BUILDER --- //
// --------------- //
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
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


app.Run();