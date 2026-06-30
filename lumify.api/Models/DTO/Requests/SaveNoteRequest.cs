
namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for updating a note (see <see cref="Controllers.NotesController.SaveNote"/>).
/// Only non-<c>null</c> fields are applied (partial update); an empty folder moves to the root.
/// </summary>
public sealed class SaveNoteRequest
{
    /// <summary>The note to update (required).</summary>
    public string ID { get; set; } = "";
    /// <summary>New title, if changing.</summary>
    public string? Name { get; set; }
    /// <summary>New folder (empty string moves to root), if changing.</summary>
    public string? FolderID { get; set; }
}