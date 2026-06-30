namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for adding a text block module to a note
/// (see <see cref="Controllers.NotesController.AddTextBlock"/>).
/// </summary>
public class AddTextBlockRequest
{
    /// <summary>The parent note (required).</summary>
    public string NoteID { get; set; } = null!;
    /// <summary>Block type code (paragraph, heading, code, …).</summary>
    public int Type { get; set; }
    /// <summary>Desired position within the note.</summary>
    public int NotePos {get; set;}
    /// <summary>Optional block title/heading.</summary>
    public string? Name { get; set; }
    /// <summary>The block's text content.</summary>
    public string? Content { get; set; }
    /// <summary>For code blocks, the programming language for syntax highlighting.</summary>
    public string? CodeLanguage { get; set; }
}