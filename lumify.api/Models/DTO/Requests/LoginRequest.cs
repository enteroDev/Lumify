namespace lumify.api.Models.DTO.Requests;

public sealed class LoginRequest
{
    public required string Identifier { get; set; } // Can be username or email
    public required string Password { get; set; }
}