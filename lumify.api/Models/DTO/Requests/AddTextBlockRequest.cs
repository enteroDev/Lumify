namespace lumify.api.Models.DTO.Requests;

public class AddTextBlockRequest
{
    public string NoteID { get; set; } = null!;
    public int Type { get; set; }
    public int NotePos {get; set;}
    public string? Name { get; set; }
    public string? Content { get; set; }
    public string? CodeLanguage { get; set; }
}