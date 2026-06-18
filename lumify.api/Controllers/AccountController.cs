
using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.EF;
using lumify.api.Models.Settings;
using lumify.api.Interfaces;
using lumify.api.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;



namespace lumify.api.Controllers
{
    [ApiController]
    [Route("account/[action]")]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly LumifyDbContext _context;
        private readonly InternalLogic _logic;
        private readonly IEmailService _emailService;
        private readonly AppSettings _app;

        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration config,
            IWebHostEnvironment env,
            LumifyDbContext context,
            InternalLogic logic,
            IEmailService emailService,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _env = env;
            _context = context;
            _logic = logic;
            _emailService = emailService;
            _app = appSettings.Value;
        }



        // ------------- //
        // --- LOGIN --- //
        // ------------- //

        [HttpPost]
        [ActionName("loginUser")]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest dto)
        {
            // 1) Find user (Username or e-mail, case-insensitive). Soft-deleted accounts (DeletedAt set)
            // are treated as non-existent, so a deleted user can no longer log in.
            var identifier = dto.Identifier.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Username.ToLower() == identifier ||
                u.Email.ToLower() == identifier) &&
                u.DeletedAt == null);

            if (user == null)
                return Unauthorized("Invalid credentials.");


            // 2) Check password
            if (!_logic.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
                return Unauthorized("Invalid credentials.");


            // 3) Generate JWT
            var token = _logic.GenerateJwtToken(user);


            // 4) Set Cookie (HttpOnly)
            var authCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),                                         // PROD: true!
                IsEssential = true,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(8),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None  // PROD: SameSiteMode.None
            };
            Response.Cookies.Append("session_token", token, authCookieOptions);


            // 4b) Set CSRF cookie (not HttpOnly) for double-submit protection
            var antiCsrf = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var csrfCookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = !_env.IsDevelopment(),                                         // PROD: true!
                IsEssential = true,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(8),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None  // PROD: SameSiteMode.None
            };
            Response.Cookies.Append("XSRF-TOKEN", antiCsrf, csrfCookieOptions);


            // 5) Response (Without Token - Token is in Cookie)
            return Ok(new
            {
                user = new
                {
                    user.ID,
                    user.Username,
                    user.Email,
                    Role = (int)Enum.Parse<Role>(user.Role),
                    user.AvatarUrl
                }
            });
        }




        // -------------- //
        // --- LOGOUT --- //
        // -------------- //
        [HttpPost]
        [ActionName("logoutUser")]
        public IActionResult LogoutUser()
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                Path = "/",
                Expires = DateTimeOffset.UnixEpoch // Expire instantly
            };

            // Delete Tokens
            Response.Cookies.Delete("session_token", options);
            Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions { Path = "/" });

            return Ok();
        }




        // ---------------- //
        // --- REGISTER --- //
        // ---------------- //
        [HttpPost]
        [ActionName("registerUser")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterRequest dto)
        {
            if (dto == null)
                return BadRequest("No data provided.");

            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Email, Username and Password are required.");
            }

            try
            {
                var exists = await _context.Users.AnyAsync(u =>
                    u.Username == dto.Username || u.Email == dto.Email);

                if (exists)
                    return Conflict("Username or Email already exists.");

                _logic.CreatePasswordHash(dto.Password, out var hash, out var salt);

                var avatarUrl = "/Data/avatars/default_avatar.png";

                if (!string.IsNullOrWhiteSpace(dto.AvatarBase64))
                {
                    try
                    {
                        avatarUrl = await _logic.SaveAvatarAsync(dto.Username, dto.AvatarBase64);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save custom avatar for {User}", dto.Username);
                    }
                }

                var user = new User
                {
                    ID = Guid.NewGuid().ToString(),
                    Email = dto.Email,
                    Username = dto.Username,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    AvatarUrl = avatarUrl,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Role = Role.User.ToString(),
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    UpdatedAt = DateTime.UtcNow.ToString("o")
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = _logic.GenerateJwtToken(user);

                var authCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !_env.IsDevelopment(),                                         // PROD: true!
                    IsEssential = true,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddHours(8),
                    SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None  // PROD: SameSiteMode.None
                };
                Response.Cookies.Append("session_token", token, authCookieOptions);

                var antiCsrf = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                var csrfCookieOptions = new CookieOptions
                {
                    HttpOnly = false,
                    Secure = !_env.IsDevelopment(),                                         // PROD: true!
                    IsEssential = true,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddHours(8),
                    SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None  // PROD: SameSiteMode.None
                };
                Response.Cookies.Append("XSRF-TOKEN", antiCsrf, csrfCookieOptions);

                return Ok(new
                {
                    user = new
                    {
                        user.ID,
                        user.Username,
                        user.Email,
                        Role = (int)Enum.Parse<Role>(user.Role),
                        user.AvatarUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {User}.", dto.Username);
                return StatusCode(500, "Internal server error.");
            }
        }




        // ----------------------- //
        // --- PASSWORD RESET --- //
        // ----------------------- //

        // Step 1: user requests a reset link. We ALWAYS answer with the same generic message,
        // regardless of whether the account exists, so this endpoint cannot be used to find out
        // which usernames / emails are registered (no user enumeration).
        [HttpPost]
        [ActionName("requestPasswordReset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest dto, CancellationToken ct)
        {
            IActionResult generic = Ok(new
            {
                message = "Falls ein Konto zu diesen Angaben existiert, wurde eine E-Mail zum Zurücksetzen verschickt."
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Identifier))
                return generic;

            var identifier = dto.Identifier.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Username.ToLower() == identifier || u.Email.ToLower() == identifier) &&
                u.DeletedAt == null, ct);

            // Unknown / deleted user -> same generic answer, no token, no mail.
            if (user == null)
                return generic;

            var now = DateTime.UtcNow;
            var nowIso = now.ToString("o");

            // Invalidate any still-open tokens of this user, so only the newest link works.
            var openTokens = await _context.PasswordResetTokens
                .Where(t => t.UserID == user.ID && t.UsedAt == null)
                .ToListAsync(ct);
            foreach (var open in openTokens)
                open.UsedAt = nowIso;

            // Create the new single-use token (only its hash is stored).
            var (rawToken, tokenHash) = _logic.GenerateResetToken();
            var resetToken = new PasswordResetToken
            {
                ID = Guid.NewGuid().ToString(),
                UserID = user.ID,
                TokenHash = tokenHash,
                ExpiresAt = now.AddMinutes(30).ToString("o"),
                UsedAt = null,
                CreatedAt = nowIso
            };
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync(ct);

            // Build the link to the FRONTEND reset page (not the API) and mail it.
            var baseUrl = (_app.FrontendBaseUrl ?? "").TrimEnd('/');
            var path = string.IsNullOrWhiteSpace(_app.PasswordResetPath) ? "/Auth/reset" : _app.PasswordResetPath;
            var resetLink = $"{baseUrl}{path}?token={Uri.EscapeDataString(rawToken)}";

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.DisplayName, resetLink, ct);
            }
            catch (Exception ex)
            {
                // We log, but still return the generic response so nothing leaks to the caller.
                _logger.LogError(ex, "Failed to send password reset mail for user {UserID}.", user.ID);
            }

            return generic;
        }


        // Step 2: user submits the token from the link plus a new password.
        [HttpPost]
        [ActionName("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest dto, CancellationToken ct)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("Token and new password are required.");

            if (dto.NewPassword.Length < 8)
                return BadRequest("Password must be at least 8 characters long.");

            // Look the token up by its hash - we never stored the raw value.
            var tokenHash = _logic.HashResetToken(dto.Token);
            var token = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

            // Unknown or already-consumed token.
            if (token == null || token.UsedAt != null)
                return BadRequest("Invalid or already used reset link.");

            // Expired token.
            var expiresAt = DateTimeOffset.Parse(token.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (expiresAt < DateTimeOffset.UtcNow)
                return BadRequest("This reset link has expired.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == token.UserID && u.DeletedAt == null, ct);
            if (user == null)
                return BadRequest("Invalid reset link.");

            // Set the new password and consume the token (single-use).
            _logic.CreatePasswordHash(dto.NewPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.UpdatedAt = DateTime.UtcNow.ToString("o");

            token.UsedAt = DateTime.UtcNow.ToString("o");

            await _context.SaveChangesAsync(ct);

            return Ok(new { message = "Dein Passwort wurde erfolgreich geändert. Du kannst dich jetzt anmelden." });
        }




        // Helper
        [HttpGet]
        [Authorize]
        [ActionName("whoami")]
        public IActionResult WhoAmI()
        {
            bool authenticated = User?.Identity?.IsAuthenticated ?? false;

            var claims = new List<object>();
            if (User?.Claims != null)
            {
                foreach (var claim in User.Claims)
                    claims.Add(new { Type = claim.Type, Value = claim.Value });
            }

            return Ok(new { Authenticated = authenticated, Claims = claims });
        }

    }
}
