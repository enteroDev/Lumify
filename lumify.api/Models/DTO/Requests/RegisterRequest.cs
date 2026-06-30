
namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for registration (see <see cref="Controllers.AccountController.RegisterUser"/>).
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>The new account's e-mail address.</summary>
    public required string Email { get; set; }
    /// <summary>The desired username.</summary>
    public required string Username { get; set; }
    /// <summary>The chosen password.</summary>
    public required string Password { get; set; }
    /// <summary>The user's first name.</summary>
    public required string FirstName { get; set; }
    /// <summary>The user's last name.</summary>
    public required string LastName { get; set; }
    /// <summary>Optional base64-encoded avatar image.</summary>
    public string? AvatarBase64 { get; set; }
}