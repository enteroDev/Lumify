
namespace lumify.api.Models.DTO.Requests;

public sealed class RegisterRequest
{
    public required string Email { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? AvatarBase64 { get; set; } // Optional: Avatar of userProfile if set
}