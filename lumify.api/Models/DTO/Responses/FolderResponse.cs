namespace lumify.api.Models.DTO.Responses;

public sealed class FolderResponse
{
    public string ID { get; set; } = "";
    public string OwnerID { get; set; } = "";
    public string? WorkspaceID { get; set; }
    public string? ParentFolderID { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}