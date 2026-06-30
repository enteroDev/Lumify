namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for requesting a password reset link
/// (see <see cref="Controllers.AccountController.RequestPasswordReset"/>).
/// </summary>
public sealed class RequestPasswordResetRequest
{
    /// <summary>Username or e-mail — same as the login identifier.</summary>
    public required string Identifier { get; set; }
}
