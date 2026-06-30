namespace lumify.api.Models.Settings
{
    /// <summary>
    /// Options bound from the <c>Jwt</c> configuration section, used to sign and validate JWT
    /// session and MFA tokens (see <see cref="Logic.InternalLogic"/>).
    /// </summary>
    public class JwtSettings
    {
        /// <summary>Symmetric signing secret for the HMAC-SHA256 signature.</summary>
        public string Secret { get; set; } = null!;
        /// <summary>Token issuer claim.</summary>
        public string Issuer { get; set; } = null!;
        /// <summary>Token audience claim.</summary>
        public string Audience { get; set; } = null!;
        /// <summary>Default token lifetime in minutes.</summary>
        public int ExpirationMinutes { get; set; }
    }
}
