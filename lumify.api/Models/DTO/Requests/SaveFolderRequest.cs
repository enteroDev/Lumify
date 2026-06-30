namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for updating a folder (see <see cref="Controllers.FoldersController.SaveFolder"/>).
/// Only non-<c>null</c> fields are applied (partial update); an empty parent moves to the root.
/// </summary>
public sealed class SaveFolderRequest
{
    /// <summary>The folder to update (required).</summary>
    public string ID { get; set; } = "";
    /// <summary>New name, if changing.</summary>
    public string? Name { get; set; }
    /// <summary>New description, if changing.</summary>
    public string? Description { get; set; }
    /// <summary>New parent folder (empty string moves to root), if changing.</summary>
    public string? ParentFolderID { get; set; }
}