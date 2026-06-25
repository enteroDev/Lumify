namespace lumify.api.Models.DTO.Requests;

public sealed class ResendVerificationRequest
{
    // Username or email - same as the login identifier.
    public required string Identifier { get; set; }
}
