namespace lumify.api.Models.DTO.Responses;

/// <summary>
/// Account information for a user (name, e-mail, username, timestamps), returned by the
/// account-info endpoints (see <see cref="Controllers.UsersController"/>).
/// </summary>
public sealed class UserAccountInfoResponse
{
    /// <summary>The user's ID.</summary>
    public string ID { get; set; } = "";
    /// <summary>The user's e-mail address.</summary>
    public string Email { get; set; } = "";

    /// <summary>The user's username.</summary>
    public string Username { get; set;} = "";
    /// <summary>The user's first name.</summary>
    public string? FirstName { get; set; }
    /// <summary>The user's last name.</summary>
    public string? LastName { get; set; }
    /// <summary>Registration date (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = "";
}