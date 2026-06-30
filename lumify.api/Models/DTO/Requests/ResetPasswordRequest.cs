namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for setting a new password via a reset link
/// (see <see cref="Controllers.AccountController.ResetPassword"/>).
/// </summary>
public sealed class ResetPasswordRequest
{
    /// <summary>The raw token from the reset link (query string).</summary>
    public required string Token { get; set; }
    /// <summary>The new password (minimum 8 characters).</summary>
    public required string NewPassword { get; set; }
}
