namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for confirming an e-mail address
/// (see <see cref="Controllers.AccountController.VerifyEmail"/>).
/// </summary>
public sealed class VerifyEmailRequest
{
    /// <summary>The one-time token from the verification link.</summary>
    public required string Token { get; set; }
}
