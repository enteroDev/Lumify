namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for creating a folder (see <see cref="Controllers.FoldersController.AddFolder"/>).
/// </summary>
public sealed class AddFolderRequest
{
    /// <summary>Parent folder, or <c>null</c> to create at the root.</summary>
    public string? ParentFolderID { get; set; }
    /// <summary>Folder name (required).</summary>
    public string Name { get; set; } = "";
    /// <summary>Optional folder description.</summary>
    public string? Description { get; set; }
    /// <summary>Target workspace, or <c>null</c> for a personal folder.</summary>
    public string? WorkspaceID { get; init; }
}