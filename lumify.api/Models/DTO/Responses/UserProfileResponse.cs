namespace lumify.api.Models.DTO.Responses;

/// <summary>
/// The public profile of a user (display name, avatar, bio), returned by the profile endpoints
/// (see <see cref="Controllers.UsersController"/>).
/// </summary>
public sealed class UserProfileResponse
{
    /// <summary>The user's ID.</summary>
    public string ID { get; set; } = "";
    /// <summary>The user's display name.</summary>
    public string? DisplayName { get; set; }
    /// <summary>The user's avatar URL.</summary>
    public string? AvatarUrl { get; set; }
    /// <summary>The user's profile bio.</summary>
    public string? Bio { get; set; }
    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = "";
}