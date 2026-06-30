namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for login (see <see cref="Controllers.AccountController.LoginUser"/>).
/// </summary>
public sealed class LoginRequest
{
    /// <summary>The login identifier — either username or e-mail.</summary>
    public required string Identifier { get; set; }
    /// <summary>The account password.</summary>
    public required string Password { get; set; }
}