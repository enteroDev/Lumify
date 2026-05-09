namespace lumify.api.Models.DTO.Responses;

public sealed class UserAccountInfoResponse
{
    public string ID { get; set; } = "";
    public string Email { get; set; } = "";

    public string Username { get; set;} = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string CreatedAt { get; set; } = "";     // Register-Date of user
    public string UpdatedAt { get; set; } = "";
}