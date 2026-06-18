namespace lumify.api.Models.Settings
{
    // Bound from the "Email" section in appsettings. The actual SMTP credentials come from the
    // mail hosting (e.g. the Hetzner mailbox) and are filled in per environment.
    public class EmailSettings
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; } = 587;
        public string User { get; set; } = null!;
        public string Password { get; set; } = null!;

        // The visible sender, e.g. "Lumify <noreply@lumify.at>".
        public string FromAddress { get; set; } = null!;
        public string FromName { get; set; } = "Lumify";

        // true  => connect on 587 and upgrade via STARTTLS (typical).
        // false => implicit TLS on connect (typical for port 465).
        public bool UseStartTls { get; set; } = true;
    }
}
