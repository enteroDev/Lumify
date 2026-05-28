namespace lumify.api.Models.DTO.Requests;

public sealed class AddNoteRequest
{
    public string? FolderID { get; set; }
    public string Name { get; set; } = "";
    public string? WorkspaceID { get; init; }
}