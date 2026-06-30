
namespace lumify.api.Models.DTO.Responses;

/// <summary>
/// A text block module returned to clients (see <see cref="Controllers.NotesController"/>).
/// The collapsed flag is exposed as a bool (mapped from the entity's integer).
/// </summary>
public class TextblockResponse
{
    /// <summary>Text block ID.</summary>
    public string ID { get; set; } = null!;
    /// <summary>The parent note.</summary>
    public string NoteID { get; set; } = null!;
    /// <summary>Block type code (paragraph, heading, code, …).</summary>
    public int Type { get; set; }
    /// <summary>Optional block title/heading.</summary>
    public string? Name { get; set; }
    /// <summary>The block's text content.</summary>
    public string? Content { get; set; }
    /// <summary>For code blocks, the language for syntax highlighting.</summary>
    public string? CodeLanguage { get; set; }
    /// <summary>Whether the block is collapsed in the UI.</summary>
    public bool IsCollapsed { get; set; }
    /// <summary>Position within the note.</summary>
    public int NotePos { get; set; }
    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;
}