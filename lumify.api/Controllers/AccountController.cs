
using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.EF;
using lumify.api.Models.Settings;
using lumify.api.Interfaces;
using lumify.api.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;



namespace lumify.api.Controllers
{
    /// <summary>
    /// Endpoints for user accounts: login/logout, registration, two-factor
    /// authentication (TOTP), password reset and e-mail verification. Sessions are
    /// issued via an HttpOnly JWT cookie plus a CSRF double-submit cookie.
    /// </summary>
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
        // Whether auth cookies are flagged "Secure" (HTTPS-only). On plain-HTTP LAN this must
        // be false, otherwise the browser refuses to store them. Set Cookies:Secure=true once HTTPS is in place.
        private readonly bool _cookieSecure;

        /// <summary>
        /// Creates the controller with its injected dependencies (logging, configuration,
        /// hosting environment, database context, internal logic helpers, e-mail service
        /// and application settings).
        /// </summary>
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
            _cookieSecure = config.GetValue<bool>("Cookies:Secure", false);
        }



        // ------------- //
        // --- LOGIN --- //
        // ------------- //

        /// <summary>
        /// Authenticates a user by username or e-mail and password.
        /// </summary>
        /// <remarks>
        /// If 2FA is enabled, no session is issued yet — a short-lived MFA challenge token
        /// is returned instead and the client must call <see cref="VerifyTotpLogin"/> with
        /// the 6-digit code. Soft-deleted and unconfirmed accounts cannot log in.
        /// </remarks>
        /// <param name="dto">Credentials: <c>Identifier</c> (username or e-mail) and password.</param>
        /// <returns>
        /// 200 with user data (session in cookie) on success without 2FA; 200 with
        /// <c>mfaRequired = true</c> and an MFA token when 2FA is enabled; 401 for invalid
        /// credentials; 403 when the e-mail address is not yet confirmed.
        /// </returns>
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


            // 2b) Block login until the e-mail address has been confirmed.
            if (!user.EmailConfirmed)
                return StatusCode(403, new { error = "email_not_confirmed", message = "Bitte bestätige zuerst deine E-Mail-Adresse." });


            // 3) If 2FA is enabled, do NOT issue a session yet. Hand back a short-lived MFA
            // challenge token; the client must then call verifyTotpLogin with the 6-digit code.
            if (user.TotpEnabled)
            {
                var mfaToken = _logic.GenerateMfaChallengeToken(user);
                return Ok(new { mfaRequired = true, mfaToken });
            }


            // 4) No 2FA -> issue the session (cookies) right away.
            AppendAuthCookies(user);


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


        // --------------------- //
        // --- 2FA (TOTP) ------ //
        // --------------------- //

        /// <summary>
        /// Login step 2: verifies the 6-digit TOTP code for a 2FA-enabled account and then
        /// issues the real session.
        /// </summary>
        /// <param name="dto">The MFA challenge token from step 1 and the 6-digit code.</param>
        /// <returns>200 with user data and the session cookie on success; 400 if token or
        /// code are missing; 401 if the MFA session or the code is invalid or expired.</returns>
        [HttpPost]
        [ActionName("verifyTotpLogin")]
        public async Task<IActionResult> VerifyTotpLogin([FromBody] VerifyTotpLoginRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.MfaToken) || string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest("MFA token and code are required.");

            var userID = _logic.ValidateMfaChallengeToken(dto.MfaToken);
            if (userID == null)
                return Unauthorized("MFA session is invalid or expired. Please log in again.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userID && u.DeletedAt == null);
            if (user == null || !user.TotpEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
                return Unauthorized("Invalid credentials.");

            if (!_logic.VerifyTotpCode(user.TotpSecret, dto.Code))
                return Unauthorized("Invalid code.");

            AppendAuthCookies(user);

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


        /// <summary>
        /// Returns whether 2FA is currently active for the logged-in user.
        /// </summary>
        /// <returns>200 with <c>enabled</c> (bool); 404 if the user no longer exists.</returns>
        [HttpGet]
        [Authorize]
        [ActionName("mfaStatus")]
        public async Task<IActionResult> MfaStatus()
        {
            var userID = GetCurrentUserID();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userID && u.DeletedAt == null);
            if (user == null)
                return NotFound();

            return Ok(new { enabled = user.TotpEnabled });
        }


        /// <summary>
        /// Starts 2FA setup: generates a fresh TOTP secret and a QR code. 2FA is not active
        /// until confirmed via <see cref="ConfirmTotp"/>.
        /// </summary>
        /// <returns>200 with <c>secret</c> (for manual entry) and <c>qrCodeDataUri</c> (for
        /// scanning); 400 if 2FA is already enabled; 404 if the user no longer exists.</returns>
        [HttpPost]
        [Authorize]
        [ActionName("setupTotp")]
        public async Task<IActionResult> SetupTotp()
        {
            var userID = GetCurrentUserID();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userID && u.DeletedAt == null);
            if (user == null)
                return NotFound();

            if (user.TotpEnabled)
                return BadRequest("Two-factor authentication is already enabled.");

            // Fresh secret each time setup is (re)started.
            var secret = _logic.GenerateTotpSecret();
            user.TotpSecret = secret;
            user.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _context.SaveChangesAsync();

            var label = string.IsNullOrWhiteSpace(user.Email) ? user.Username : user.Email;
            var otpauthUri = _logic.BuildOtpauthUri(secret, label);
            var qrCodeDataUri = _logic.GenerateQrCodeDataUri(otpauthUri);

            return Ok(new
            {
                secret,          // for manual entry
                qrCodeDataUri    // for scanning
            });
        }


        /// <summary>
        /// Confirms 2FA setup: a valid TOTP code flips two-factor authentication on.
        /// </summary>
        /// <param name="dto">The 6-digit code from the authenticator app.</param>
        /// <returns>200 with <c>enabled = true</c> on success; 400 if the code is missing,
        /// setup was not started, or the code is invalid; 404 if the user no longer exists.</returns>
        [HttpPost]
        [Authorize]
        [ActionName("confirmTotp")]
        public async Task<IActionResult> ConfirmTotp([FromBody] TotpCodeRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest("Code is required.");

            var userID = GetCurrentUserID();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userID && u.DeletedAt == null);
            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(user.TotpSecret))
                return BadRequest("Start the setup first.");

            if (!_logic.VerifyTotpCode(user.TotpSecret, dto.Code))
                return BadRequest("Invalid code.");

            user.TotpEnabled = true;
            user.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _context.SaveChangesAsync();

            return Ok(new { enabled = true });
        }


        /// <summary>
        /// Disables 2FA. Requires a valid current TOTP code so a hijacked session cannot
        /// silently turn it off.
        /// </summary>
        /// <param name="dto">The 6-digit code from the authenticator app.</param>
        /// <returns>200 with <c>enabled = false</c> on success; 400 if the code is missing,
        /// 2FA is not enabled, or the code is invalid; 404 if the user no longer exists.</returns>
        [HttpPost]
        [Authorize]
        [ActionName("disableTotp")]
        public async Task<IActionResult> DisableTotp([FromBody] TotpCodeRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest("Code is required.");

            var userID = GetCurrentUserID();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userID && u.DeletedAt == null);
            if (user == null)
                return NotFound();

            if (!user.TotpEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
                return BadRequest("Two-factor authentication is not enabled.");

            if (!_logic.VerifyTotpCode(user.TotpSecret, dto.Code))
                return BadRequest("Invalid code.");

            user.TotpEnabled = false;
            user.TotpSecret = null;
            user.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _context.SaveChangesAsync();

            return Ok(new { enabled = false });
        }




        // -------------- //
        // --- LOGOUT --- //
        // -------------- //
        /// <summary>
        /// Logs the user out by expiring the session and CSRF cookies immediately.
        /// </summary>
        /// <returns>200 once the cookies have been cleared.</returns>
        [HttpPost]
        [ActionName("logoutUser")]
        public IActionResult LogoutUser()
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookieSecure,
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
        /// <summary>
        /// Registers a new user account and sends an e-mail verification link. No automatic
        /// login — the account must confirm its e-mail before it can sign in.
        /// </summary>
        /// <remarks>
        /// An optional base64 avatar is stored; failure to save it is logged but does not
        /// abort registration (a default avatar is used instead).
        /// </remarks>
        /// <param name="dto">Registration data (e-mail, username, password, optional name/avatar).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with <c>verificationRequired = true</c> on success; 400 if required
        /// fields are missing; 409 if the username or e-mail already exists; 500 on error.</returns>
        [HttpPost]
        [ActionName("registerUser")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterRequest dto, CancellationToken ct)
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

                var avatarUrl = "/src/default_avatar.png";

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

                // No auto-login: the account must confirm its e-mail first. Send the verification mail.
                try
                {
                    await SendNewVerificationMail(user, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send verification mail for {User}.", dto.Username);
                }

                return Ok(new { verificationRequired = true, email = user.Email });
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
        /// <summary>
        /// Password reset step 1: requests a reset link for the given username or e-mail.
        /// </summary>
        /// <remarks>
        /// Always returns the same generic message regardless of whether the account exists,
        /// so the endpoint cannot be used to enumerate registered users. Any previously open
        /// reset tokens for the user are invalidated; only the hash of the new token is stored.
        /// </remarks>
        /// <param name="dto">The identifier (username or e-mail).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with a generic message in all cases.</returns>
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
        /// <summary>
        /// Password reset step 2: sets a new password using the single-use token from the
        /// reset link.
        /// </summary>
        /// <remarks>
        /// The token is looked up by its hash (the raw value is never stored) and consumed on
        /// success, so each reset link works only once and only before it expires.
        /// </remarks>
        /// <param name="dto">The reset token and the new password (minimum 8 characters).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with a success message; 400 if input is missing, the password is too
        /// short, or the token is invalid, already used, or expired.</returns>
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




        // --------------------------- //
        // --- EMAIL VERIFICATION  --- //
        // --------------------------- //

        /// <summary>
        /// Confirms an account via the single-use token from the verification mail.
        /// </summary>
        /// <remarks>
        /// The token is looked up by its hash and consumed on success, so the verification
        /// link works only once and only before it expires.
        /// </remarks>
        /// <param name="dto">The verification token.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with a success message; 400 if the token is missing, invalid, already
        /// used, or expired.</returns>
        [HttpPost]
        [ActionName("verifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest dto, CancellationToken ct)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest("Token is required.");

            // Tokens are looked up by their hash - we never stored the raw value.
            var tokenHash = _logic.HashResetToken(dto.Token);
            var token = await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

            if (token == null || token.UsedAt != null)
                return BadRequest("Invalid or already used verification link.");

            var expiresAt = DateTimeOffset.Parse(token.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (expiresAt < DateTimeOffset.UtcNow)
                return BadRequest("This verification link has expired.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == token.UserID && u.DeletedAt == null, ct);
            if (user == null)
                return BadRequest("Invalid verification link.");

            // Confirm the account and consume the token (single-use).
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow.ToString("o");
            token.UsedAt = DateTime.UtcNow.ToString("o");
            await _context.SaveChangesAsync(ct);

            return Ok(new { message = "E-Mail erfolgreich bestätigt. Du kannst dich jetzt anmelden." });
        }


        /// <summary>
        /// Resends a verification mail for an existing, still-unconfirmed account.
        /// </summary>
        /// <remarks>
        /// Always answers with the same generic message (no user enumeration); a mail is only
        /// actually sent for an account that exists and is not yet confirmed.
        /// </remarks>
        /// <param name="dto">The identifier (username or e-mail).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with a generic message in all cases.</returns>
        [HttpPost]
        [ActionName("resendVerification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest dto, CancellationToken ct)
        {
            IActionResult generic = Ok(new
            {
                message = "Falls ein unbestätigtes Konto zu diesen Angaben existiert, wurde eine neue Bestätigungsmail verschickt."
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Identifier))
                return generic;

            var identifier = dto.Identifier.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Username.ToLower() == identifier || u.Email.ToLower() == identifier) &&
                u.DeletedAt == null, ct);

            // Only resend for an existing, still-unconfirmed account.
            if (user == null || user.EmailConfirmed)
                return generic;

            try
            {
                await SendNewVerificationMail(user, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification mail for user {UserID}.", user.ID);
            }

            return generic;
        }


        /// <summary>
        /// Creates a fresh single-use verification token (invalidating older open ones) and
        /// mails the verification link to the user.
        /// </summary>
        /// <param name="user">The account to send the verification mail to.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        private async Task SendNewVerificationMail(User user, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var nowIso = now.ToString("o");

            // Invalidate any still-open verification tokens, so only the newest link works.
            var openTokens = await _context.EmailVerificationTokens
                .Where(t => t.UserID == user.ID && t.UsedAt == null)
                .ToListAsync(ct);
            foreach (var open in openTokens)
                open.UsedAt = nowIso;

            var (rawToken, tokenHash) = _logic.GenerateResetToken();
            var verifyToken = new EmailVerificationToken
            {
                ID = Guid.NewGuid().ToString(),
                UserID = user.ID,
                TokenHash = tokenHash,
                ExpiresAt = now.AddHours(24).ToString("o"),
                UsedAt = null,
                CreatedAt = nowIso
            };
            _context.EmailVerificationTokens.Add(verifyToken);
            await _context.SaveChangesAsync(ct);

            var baseUrl = (_app.FrontendBaseUrl ?? "").TrimEnd('/');
            var verifyLink = $"{baseUrl}/Auth/verify?token={Uri.EscapeDataString(rawToken)}";

            await _emailService.SendVerificationEmailAsync(user.Email, user.DisplayName, verifyLink, ct);
        }



        // --------------- //
        // --- HELPERS --- //
        // --------------- //

        /// <summary>
        /// Diagnostic endpoint that returns the current authentication state and the claims
        /// of the logged-in user.
        /// </summary>
        /// <returns>200 with <c>Authenticated</c> (bool) and the list of <c>Claims</c>.</returns>
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



        // --------------- //
        // --- HELPERS --- //
        // --------------- //

        /// <summary>
        /// Issues the session (HttpOnly JWT cookie) plus the CSRF double-submit cookie.
        /// Shared by the normal login and the 2FA second step.
        /// </summary>
        /// <param name="user">The authenticated user the session is issued for.</param>
        private void AppendAuthCookies(User user)
        {
            var token = _logic.GenerateJwtToken(user);

            var authCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookieSecure,                 // true erst bei HTTPS (Config Cookies:Secure)
                IsEssential = true,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(8),
                SameSite = SameSiteMode.Lax             // Single-Origin hinter dem Proxy -> Lax reicht
            };
            Response.Cookies.Append("session_token", token, authCookieOptions);

            var antiCsrf = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var csrfCookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = _cookieSecure,
                IsEssential = true,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(8),
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("XSRF-TOKEN", antiCsrf, csrfCookieOptions);
        }

        /// <summary>
        /// Reads the current user's ID from the <c>UserID</c> claim of the authenticated request.
        /// </summary>
        /// <returns>The current user's ID.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when no user is logged in.</exception>
        private string GetCurrentUserID()
        {
            var userID = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrWhiteSpace(userID))
            {
                throw new UnauthorizedAccessException("Kein User eingeloggt.");
            }

            return userID;
        }

    }
}
