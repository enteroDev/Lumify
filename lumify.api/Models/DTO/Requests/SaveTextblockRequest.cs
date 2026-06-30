namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for updating a text block module
/// (see <see cref="Controllers.NotesController.SaveTextBlock"/>). Only non-<c>null</c> fields are
/// applied (partial update).
/// </summary>
public sealed class SaveTextblockRequest
{
    /// <summary>The text block to update (required).</summary>
    public string ID { get; set; } = "";
    /// <summary>New block type, if changing.</summary>
    public int? Type { get; set; }
    /// <summary>New title/heading, if changing.</summary>
    public string? Name { get; set; }
    /// <summary>New content, if changing.</summary>
    public string? Content { get; set; }
    /// <summary>New code language, if changing.</summary>
    public string? CodeLanguage { get; set; }
    /// <summary>New collapsed state, if changing.</summary>
    public bool? IsCollapsed { get; set; }
    /// <summary>New position within the note (reorders the block), if changing.</summary>
    public int? NotePos { get; set; }
}