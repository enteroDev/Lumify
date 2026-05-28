
namespace lumify.api.Models.DTO.Responses;

public class TextblockResponse
{
    public string ID { get; set; } = null!;
    public string NoteID { get; set; } = null!;
    public int Type { get; set; }
    public string? Name { get; set; }
    public string? Content { get; set; }
    public string? CodeLanguage { get; set; }
    public bool IsCollapsed { get; set; }
    public int NotePos { get; set; }
    public string CreatedAt { get; set; } = null!;
    public string UpdatedAt { get; set; } = null!;
}