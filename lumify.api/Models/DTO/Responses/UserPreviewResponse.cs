

using lumify.api.Models.Enum;


namespace lumify.api.Models.DTO.Responses;


public sealed class UserPreviewResponse
{
    public string ID { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    public PresenceStatus PresenceStatus { get; set; }
}