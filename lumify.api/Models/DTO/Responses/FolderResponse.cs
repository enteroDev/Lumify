namespace lumify.api.Models.DTO.Responses;

/// <summary>
/// A folder returned to clients (see <see cref="Controllers.FoldersController"/>).
/// </summary>
public sealed class FolderResponse
{
    /// <summary>Folder ID.</summary>
    public string ID { get; set; } = "";
    /// <summary>The owner's user ID.</summary>
    public string OwnerID { get; set; } = "";
    /// <summary>The owning workspace, or <c>null</c> for a personal folder.</summary>
    public string? WorkspaceID { get; set; }
    /// <summary>The parent folder, or <c>null</c> if at the root.</summary>
    public string? ParentFolderID { get; set; }
    /// <summary>Folder name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Optional folder description.</summary>
    public string? Description { get; set; }
    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = "";
}