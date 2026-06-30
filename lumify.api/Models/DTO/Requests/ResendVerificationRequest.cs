namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for resending an e-mail verification link
/// (see <see cref="Controllers.AccountController.ResendVerification"/>).
/// </summary>
public sealed class ResendVerificationRequest
{
    /// <summary>Username or e-mail — same as the login identifier.</summary>
    public required string Identifier { get; set; }
}
