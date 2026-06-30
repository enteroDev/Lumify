

using lumify.api.Models.Enum;


namespace lumify.api.Models.DTO.Responses;


/// <summary>
/// A compact public preview of a user (used in lists and search), enriched with live presence
/// status (see <see cref="Controllers.UsersController"/>).
/// </summary>
public sealed class UserPreviewResponse
{
    /// <summary>The user's ID.</summary>
    public string ID { get; set; } = "";
    /// <summary>The user's username.</summary>
    public string Username { get; set; } = "";
    /// <summary>The user's e-mail address.</summary>
    public string Email { get; set; } = "";
    /// <summary>The user's display name.</summary>
    public string? DisplayName { get; set; }
    /// <summary>The user's avatar URL.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>The user's current presence status.</summary>
    public PresenceStatus PresenceStatus { get; set; }
}