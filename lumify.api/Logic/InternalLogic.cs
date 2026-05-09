

using lumify.api.Models.EF;
using lumify.api.Models.Context;
using lumify.api.Models.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace lumify.api.Logic
{
    public class InternalLogic
    {
        private readonly ILogger<InternalLogic> _logger;
        private readonly LumifyDbContext _context;
        private readonly JwtSettings _jwt;

        public InternalLogic (ILogger<InternalLogic> logger, LumifyDbContext context, IOptions<JwtSettings> jwt)
        {
            _logger = logger;
            _context = context;
            _jwt = jwt.Value;
        }

        internal void CreatePasswordHash(string password, out string hash, out string salt)
        {
            using var hmac = new HMACSHA512();

            var saltBytes = hmac.Key;
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            salt = Convert.ToBase64String(saltBytes);
            hash = Convert.ToBase64String(hashBytes);
        }

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

