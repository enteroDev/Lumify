namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for adding a link item module to a note
/// (see <see cref="Controllers.NotesController.AddLinkItem"/>).
/// </summary>
public sealed class AddLinkItemRequest
{
    /// <summary>The parent note (required).</summary>
    public string NoteID { get; set; } = null!;
    /// <summary>Desired position within the note.</summary>
    public int NotePos { get; set; }
    /// <summary>Optional display label for the link.</summary>
    public string? Label { get; set; }
    /// <summary>The target URL (required).</summary>
    public string? Url { get; set; }

}