namespace lumify.api.Models.DTO.Requests;

public sealed class RequestPasswordResetRequest
{
    // Username or email - same as the login identifier.
    public required string Identifier { get; set; }
}
