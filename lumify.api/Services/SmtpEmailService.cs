/* SmtpEmailService
 * Sends mail over SMTP via MailKit. The SMTP credentials come from the "Email" config section
 * (filled per environment). This is the only place that talks to the mail server.
 */

using lumify.api.Interfaces;
using lumify.api.Models.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace lumify.api.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string? displayName, string resetLink, CancellationToken ct)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(displayName) ? toEmail : displayName, toEmail));
            message.Subject = "Lumify – Passwort zurücksetzen";

            var greetingName = string.IsNullOrWhiteSpace(displayName) ? "" : $" {displayName}";

            var bodyBuilder = new BodyBuilder
            {
                TextBody =
                    $"Hallo{greetingName},\n\n" +
                    "du (oder jemand anderes) hat angefordert, dein Lumify-Passwort zurückzusetzen.\n" +
                    "Öffne den folgenden Link, um ein neues Passwort zu setzen:\n\n" +
                    $"{resetLink}\n\n" +
                    "Der Link ist 30 Minuten gültig und kann nur einmal verwendet werden.\n" +
                    "Wenn du das nicht warst, kannst du diese E-Mail einfach ignorieren – dein Passwort bleibt unverändert.\n\n" +
                    "Dein Lumify-Team",

                HtmlBody =
                    $"<p>Hallo{System.Net.WebUtility.HtmlEncode(greetingName)},</p>" +
                    "<p>du (oder jemand anderes) hat angefordert, dein Lumify-Passwort zurückzusetzen. " +
                    "Klicke auf den folgenden Link, um ein neues Passwort zu setzen:</p>" +
                    $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(resetLink)}\">Passwort zurücksetzen</a></p>" +
                    "<p>Der Link ist <strong>30 Minuten</strong> gültig und kann nur einmal verwendet werden.</p>" +
                    "<p>Wenn du das nicht warst, kannst du diese E-Mail einfach ignorieren – dein Passwort bleibt unverändert.</p>" +
                    "<p>Dein Lumify-Team</p>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            var secureOption = _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, secureOption, ct);
            await client.AuthenticateAsync(_settings.User, _settings.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Password reset mail sent to {Email}.", toEmail);
        }


        public async Task SendVerificationEmailAsync(string toEmail, string? displayName, string verifyLink, CancellationToken ct)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(displayName) ? toEmail : displayName, toEmail));
            message.Subject = "Lumify – E-Mail bestätigen";

            var greetingName = string.IsNullOrWhiteSpace(displayName) ? "" : $" {displayName}";

            var bodyBuilder = new BodyBuilder
            {
                TextBody =
                    $"Hallo{greetingName},\n\n" +
                    "willkommen bei Lumify! Bitte bestätige deine E-Mail-Adresse, um dein Konto zu aktivieren:\n\n" +
                    $"{verifyLink}\n\n" +
                    "Der Link ist 24 Stunden gültig.\n" +
                    "Wenn du dich nicht bei Lumify registriert hast, kannst du diese E-Mail einfach ignorieren.\n\n" +
                    "Dein Lumify-Team",

                HtmlBody =
                    $"<p>Hallo{System.Net.WebUtility.HtmlEncode(greetingName)},</p>" +
                    "<p>willkommen bei Lumify! Bitte bestätige deine E-Mail-Adresse, um dein Konto zu aktivieren:</p>" +
                    $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(verifyLink)}\">E-Mail bestätigen</a></p>" +
                    "<p>Der Link ist <strong>24 Stunden</strong> gültig.</p>" +
                    "<p>Wenn du dich nicht bei Lumify registriert hast, kannst du diese E-Mail einfach ignorieren.</p>" +
                    "<p>Dein Lumify-Team</p>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            var secureOption = _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, secureOption, ct);
            await client.AuthenticateAsync(_settings.User, _settings.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Verification mail sent to {Email}.", toEmail);
        }
    }
}
