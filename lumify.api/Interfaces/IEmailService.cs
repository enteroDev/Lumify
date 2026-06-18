namespace lumify.api.Interfaces
{
    public interface IEmailService
    {
        // Sends the password-reset mail containing the one-time reset link to the user.
        Task SendPasswordResetEmailAsync(string toEmail, string? displayName, string resetLink, CancellationToken ct);
    }
}
