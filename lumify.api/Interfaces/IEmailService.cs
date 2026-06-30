namespace lumify.api.Interfaces
{
    /// <summary>
    /// Abstraction for sending transactional e-mails (password reset and address verification).
    /// Implemented by <see cref="Services.SmtpEmailService"/>.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends the password-reset mail containing the one-time reset link to the user.
        /// </summary>
        /// <param name="toEmail">The recipient's e-mail address.</param>
        /// <param name="displayName">The recipient's display name for the greeting, if known.</param>
        /// <param name="resetLink">The one-time reset link to embed.</param>
        /// <param name="ct">Cancellation token for the send operation.</param>
        Task SendPasswordResetEmailAsync(string toEmail, string? displayName, string resetLink, CancellationToken ct);

        /// <summary>
        /// Sends the e-mail verification mail containing the one-time confirmation link to the user.
        /// </summary>
        /// <param name="toEmail">The recipient's e-mail address.</param>
        /// <param name="displayName">The recipient's display name for the greeting, if known.</param>
        /// <param name="verifyLink">The one-time verification link to embed.</param>
        /// <param name="ct">Cancellation token for the send operation.</param>
        Task SendVerificationEmailAsync(string toEmail, string? displayName, string verifyLink, CancellationToken ct);
    }
}
