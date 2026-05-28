
namespace lumify.api.Models.DTO.Requests;

public sealed class SaveNoteRequest
{
    public string ID { get; set; } = "";
    public string? Name { get; set; }
    public string? FolderID { get; set; }
}