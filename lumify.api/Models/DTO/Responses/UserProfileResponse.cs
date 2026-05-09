namespace lumify.api.Models.DTO.Responses;

public sealed class UserProfileResponse
{
    public string ID { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}