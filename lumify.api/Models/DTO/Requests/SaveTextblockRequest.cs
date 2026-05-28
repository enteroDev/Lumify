namespace lumify.api.Models.DTO.Requests;

public sealed class SaveTextblockRequest
{
    public string ID { get; set; } = "";
    public int? Type { get; set; }
    public string? Name { get; set; }
    public string? Content { get; set; }
    public string? CodeLanguage { get; set; }
    public bool? IsCollapsed { get; set; }
    public int? NotePos { get; set; }
}