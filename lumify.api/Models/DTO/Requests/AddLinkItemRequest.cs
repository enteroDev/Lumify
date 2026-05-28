namespace lumify.api.Models.DTO.Requests;

public sealed class AddLinkItemRequest
{
    public string NoteID { get; set; } = null!;
    public int NotePos { get; set; }
    public string? Label { get; set; }
    public string? Url { get; set; }

}