
using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.EF;
using lumify.api.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration config,
            IWebHostEnvironment env,
            LumifyDbContext context,
            InternalLogic logic)
        {
            _logger = logger;
            _env = env;
            _context = context;
            _logic = logic;
        }



        // ------------- //
        // --- LOGIN --- //
        // ------------- //

        [HttpPost]
        [ActionName("loginUser")]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest dto)
        {
            // 1) Find user (Username or e-mail, case-insensitive)
            var identifier = dto.Identifier.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username.ToLower() == identifier ||
                u.Email.ToLower() == identifier);

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
