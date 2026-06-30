namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for creating a note (see <see cref="Controllers.NotesController.AddNote"/>).
/// </summary>
public sealed class AddNoteRequest
{
    /// <summary>Containing folder, or <c>null</c> to create at the root.</summary>
    public string? FolderID { get; set; }
    /// <summary>Note title (required).</summary>
    public string Name { get; set; } = "";
    /// <summary>Target workspace, or <c>null</c> for a personal note.</summary>
    public string? WorkspaceID { get; init; }
}