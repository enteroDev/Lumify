

using lumify.api.Models.EF;
using lumify.api.Models.Context;
using lumify.api.Models.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using OtpNet;
using QRCoder;


namespace lumify.api.Logic
{
    /// <summary>
    /// Central helper for authentication and account security: password hashing/verification,
    /// JWT issuing, single-use token generation/hashing (password reset and e-mail verification),
    /// two-factor authentication (TOTP secret, QR code, code verification, MFA challenge tokens)
    /// and avatar file handling. Used primarily by the <see cref="Controllers.AccountController"/>.
    /// </summary>
    public class InternalLogic
    {
        private readonly ILogger<InternalLogic> _logger;
        private readonly LumifyDbContext _context;
        private readonly JwtSettings _jwt;

        /// <summary>
        /// Creates the helper with its injected logger, database context and JWT settings.
        /// </summary>
        public InternalLogic (ILogger<InternalLogic> logger, LumifyDbContext context, IOptions<JwtSettings> jwt)
        {
            _logger = logger;
            _context = context;
            _jwt = jwt.Value;
        }

        /// <summary>
        /// Hashes a password with HMAC-SHA512 using a freshly generated random key as the salt.
        /// </summary>
        /// <param name="password">The plaintext password to hash.</param>
        /// <param name="hash">Outputs the Base64-encoded hash.</param>
        /// <param name="salt">Outputs the Base64-encoded salt (the HMAC key).</param>
        internal void CreatePasswordHash(string password, out string hash, out string salt)
        {
            using var hmac = new HMACSHA512();

            var saltBytes = hmac.Key;
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            salt = Convert.ToBase64String(saltBytes);
            hash = Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verifies a password against a stored hash and salt by recomputing the HMAC-SHA512 and
        /// comparing the bytes.
        /// </summary>
        /// <param name="password">The plaintext password to check.</param>
        /// <param name="storedHash">The Base64-encoded hash from the database.</param>
        /// <param name="storedSalt">The Base64-encoded salt from the database.</param>
        /// <returns><c>true</c> if the password matches; <c>false</c> on mismatch or missing input.</returns>
        internal bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(storedHash) ||
                string.IsNullOrWhiteSpace(storedSalt))
            {
                return false;
            }

            var saltBytes = Convert.FromBase64String(storedSalt);
            var storedHashBytes = Convert.FromBase64String(storedHash);

            using var hmac = new HMACSHA512(saltBytes);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            if (computedHash.Length != storedHashBytes.Length)
            {
                return false;
            }

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != storedHashBytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a new single-use token (used for both password reset and e-mail verification).
        /// </summary>
        /// <remarks>The raw token goes into the e-mail link and is never persisted; only its
        /// SHA-256 hash is stored. The raw value is URL-safe Base64 so it can live in a query
        /// string without escaping.</remarks>
        /// <returns>A tuple of the raw token and its hash.</returns>
        internal (string rawToken, string tokenHash) GenerateResetToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            // URL-safe Base64 so the token can live in a query string without escaping surprises.
            var rawToken = Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return (rawToken, HashResetToken(rawToken));
        }

        /// <summary>
        /// Hashes a raw token (SHA-256) for storage and lookup. No salt is needed because the
        /// token is already 32 bytes of randomness, unlike a user-chosen password.
        /// </summary>
        /// <param name="rawToken">The raw token to hash.</param>
        /// <returns>The Base64-encoded SHA-256 hash.</returns>
        internal string HashResetToken(string rawToken)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToBase64String(hashBytes);
        }


        /// <summary>
        /// Derives a file extension from a data-URL's MIME type (e.g. <c>image/png</c> → <c>.png</c>),
        /// defaulting to <c>.png</c> for unknown or missing types.
        /// </summary>
        /// <param name="dataUrl">The data URL (with or without the <c>data:</c> prefix).</param>
        /// <returns>A file extension including the leading dot.</returns>
        public string GetExtensionFromDataUrl(string dataUrl)
        {

            if (string.IsNullOrWhiteSpace(dataUrl))
            {
                return ".png";
            }

            var span = dataUrl.AsSpan();

            // Falls "data:" vorne dran ist, wegschneiden
            if (span.StartsWith("data:".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                span = span.Slice(5);
            }

            var semicolonIndex = span.IndexOf(';');
            if (semicolonIndex <= 0)
            {
                return ".png";
            }

            var mimeSpan = span.Slice(0, semicolonIndex); // "image/png"
            var mime = mimeSpan.ToString();

            return mime.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".png"
            };
        }

        /// <summary>
        /// Builds a safe avatar file name from a username and extension, lower-casing it and
        /// replacing characters that are invalid in file names.
        /// </summary>
        /// <param name="username">The user's name, used as the file-name base.</param>
        /// <param name="ext">The file extension (defaults to <c>.png</c> if empty).</param>
        /// <returns>A file name of the form <c>avatar-{username}{ext}</c>.</returns>
        internal string BuildAvatarFileName(string username, string ext)
        {
            var baseName = username.ToLowerInvariant();

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(c, '_');
            }

            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = ".png";
            }

            return $"avatar-{baseName}{ext}";
        }


        /// <summary>
        /// Issues a signed JWT session token for a user, carrying the user ID, username, e-mail
        /// and role claims, valid for 8 hours.
        /// </summary>
        /// <param name="user">The authenticated user to issue the token for.</param>
        /// <returns>The serialized JWT string.</returns>
        internal string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim("UserID", user.ID), // ID is now string (Guid)
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()) // Enum to string for the JWT
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // ---------------------------- //
        // --- TWO-FACTOR (TOTP) ------ //
        // ---------------------------- //

        /// <summary>
        /// Generates a new random Base32 TOTP secret to share with the user's authenticator app.
        /// </summary>
        /// <returns>The Base32-encoded secret.</returns>
        internal string GenerateTotpSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        /// <summary>
        /// Builds the <c>otpauth://</c> URI (6 digits, 30s period) that the QR code encodes and
        /// the authenticator app scans.
        /// </summary>
        /// <param name="secret">The Base32 TOTP secret.</param>
        /// <param name="accountLabel">The account label shown in the authenticator (e-mail or username).</param>
        /// <returns>The otpauth URI.</returns>
        internal string BuildOtpauthUri(string secret, string accountLabel)
        {
            var issuer = Uri.EscapeDataString("Lumify");
            var label = Uri.EscapeDataString($"Lumify:{accountLabel}");
            return $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&digits=6&period=30";
        }

        /// <summary>
        /// Renders text (here the otpauth URI) as a PNG QR code, returned as a Base64 data URI
        /// ready for an <c>&lt;img&gt;</c> tag.
        /// </summary>
        /// <param name="content">The text to encode in the QR code.</param>
        /// <returns>A <c>data:image/png;base64,…</c> URI.</returns>
        internal string GenerateQrCodeDataUri(string content)
        {
            using var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(10);
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Verifies a 6-digit TOTP code against the stored secret, allowing ±1 time step for
        /// clock drift.
        /// </summary>
        /// <param name="secret">The user's Base32 TOTP secret.</param>
        /// <param name="code">The 6-digit code entered by the user.</param>
        /// <returns><c>true</c> if the code is valid; <c>false</c> on mismatch or invalid input.</returns>
        internal bool VerifyTotpCode(string secret, string code)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            try
            {
                var totp = new Totp(Base32Encoding.ToBytes(secret));
                return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Issues a short-lived (5-minute) MFA challenge token proving the password step passed,
        /// used between login and the TOTP step.
        /// </summary>
        /// <remarks>This is not a session token — it only carries the user ID and an
        /// <c>mfa_pending</c> marker.</remarks>
        /// <param name="user">The user who passed the password step.</param>
        /// <returns>The serialized challenge JWT.</returns>
        internal string GenerateMfaChallengeToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim("UserID", user.ID),
                new Claim("mfa_pending", "true")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Validates an MFA challenge token (signature, issuer, audience, lifetime and the
        /// <c>mfa_pending</c> marker) and extracts the user ID.
        /// </summary>
        /// <param name="token">The challenge token from the login step.</param>
        /// <returns>The user ID if valid, or <c>null</c> if the token is missing, invalid or expired.</returns>
        internal string? ValidateMfaChallengeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var handler = new JwtSecurityTokenHandler();

            try
            {
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);

                if (principal.FindFirst("mfa_pending")?.Value != "true")
                {
                    return null;
                }

                return principal.FindFirst("UserID")?.Value;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Decodes a base64 data-URL avatar image, writes it to <c>wwwroot/avatars</c> under a
        /// safe file name, and returns its public URL.
        /// </summary>
        /// <param name="username">The user the avatar belongs to (used for the file name).</param>
        /// <param name="dataUrl">The base64 image data URL.</param>
        /// <returns>The public avatar URL, or a default avatar URL if saving fails.</returns>
        public async Task<string> SaveAvatarAsync(string username, string dataUrl)
        {
            try
            {
                // 1. Extract Base64 part
                var commaIndex = dataUrl.IndexOf(',');
                var base64 = commaIndex >= 0
                    ? dataUrl[(commaIndex + 1)..]
                    : dataUrl;

                byte[] bytes = Convert.FromBase64String(base64);

                // 2. Detect file extension
                var ext = GetExtensionFromDataUrl(dataUrl);

                // 3. Build safe filename
                var fileName = BuildAvatarFileName(username, ext);

                // 4. Build path: API/wwwroot/avatars
                var root = Directory.GetCurrentDirectory();
                var avatarRoot = Path.Combine(root, "wwwroot", "avatars");

                if (!Directory.Exists(avatarRoot))
                    Directory.CreateDirectory(avatarRoot);

                var fullPath = Path.Combine(avatarRoot, fileName);

                // 5. Save file
                await File.WriteAllBytesAsync(fullPath, bytes);

                // 6. Return URL for frontend
                return $"/avatars/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Avatar saving failed for user {User}", username);
                return "/Data/avatars/default_avatar.png";
            }
        }


    }
}

