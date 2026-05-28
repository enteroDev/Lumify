namespace lumify.api.Models.DTO.Requests;

public sealed class AddFolderRequest
{
    public string? ParentFolderID { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? WorkspaceID { get; init; }
}