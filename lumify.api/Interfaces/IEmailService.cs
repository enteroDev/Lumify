namespace lumify.api.Interfaces
{
    public interface IEmailService
    {
        // Sends the password-reset mail containing the one-time reset link to the user.
        Task SendPasswordResetEmailAsync(string toEmail, string? displayName, string resetLink, CancellationToken ct);

        // Sends the e-mail verification mail containing the one-time confirmation link to the user.
        Task SendVerificationEmailAsync(string toEmail, string? displayName, string verifyLink, CancellationToken ct);
    }
}
