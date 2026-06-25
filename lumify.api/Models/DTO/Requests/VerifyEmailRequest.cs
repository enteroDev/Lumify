namespace lumify.api.Models.DTO.Requests;

public sealed class VerifyEmailRequest
{
    // The one-time token from the verification link.
    public required string Token { get; set; }
}
