namespace lumify.api.Models.Settings
{
    /// <summary>
    /// Options bound from the <c>Email</c> configuration section. The actual SMTP credentials come
    /// from the mail hosting and are filled in per environment. Consumed by
    /// <see cref="Services.SmtpEmailService"/>.
    /// </summary>
    public class EmailSettings
    {
        /// <summary>SMTP server host name.</summary>
        public string Host { get; set; } = null!;
        /// <summary>SMTP server port (default 587).</summary>
        public int Port { get; set; } = 587;
        /// <summary>SMTP login user.</summary>
        public string User { get; set; } = null!;
        /// <summary>SMTP login password.</summary>
        public string Password { get; set; } = null!;

        /// <summary>The visible sender address (e.g. <c>noreply@lumify.at</c>).</summary>
        public string FromAddress { get; set; } = null!;
        /// <summary>The visible sender display name.</summary>
        public string FromName { get; set; } = "Lumify";

        /// <summary>
        /// <c>true</c> connects on 587 and upgrades via STARTTLS (typical); <c>false</c> uses
        /// implicit TLS on connect (typical for port 465).
        /// </summary>
        public bool UseStartTls { get; set; } = true;
    }
}
