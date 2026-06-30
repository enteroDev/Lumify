

namespace lumify.api.Models.DTO.Responses;
/// <summary>
/// A note (metadata only) returned to clients; its modules are fetched separately
/// (see <see cref="Controllers.NotesController"/>).
/// </summary>
public sealed class NoteResponse
{
    /// <summary>Note ID.</summary>
    public string ID { get; set; } = "";
    /// <summary>The owner's user ID.</summary>
    public string OwnerID { get; set; } = "";
    /// <summary>The creator's name (privacy-safe; "Gelöschter Benutzer" if the creator was deleted).</summary>
    public string? OwnerName { get; set; }
    /// <summary>The owning workspace, or <c>null</c> for a personal note.</summary>
    public string? WorkspaceID { get; set; }
    /// <summary>The containing folder, or <c>null</c> if at the root.</summary>
    public string? FolderID { get; set; }
    /// <summary>Note title.</summary>
    public string Name { get; set; } = "";
    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = "";
}