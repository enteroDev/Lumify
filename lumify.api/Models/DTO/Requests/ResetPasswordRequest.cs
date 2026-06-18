namespace lumify.api.Models.DTO.Requests;

public sealed class ResetPasswordRequest
{
    // The raw token from the reset link (query string).
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
}
