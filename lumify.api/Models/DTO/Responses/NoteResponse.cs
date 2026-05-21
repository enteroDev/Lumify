

namespace lumify.api.Models.DTO.Responses;
public sealed class NoteResponse
{
    public string ID { get; set; } = "";
    public string OwnerID { get; set; } = "";
    public string? OwnerName { get; set; }
    public string? WorkspaceID { get; set; }
    public string? FolderID { get; set; }
    public string Name { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}