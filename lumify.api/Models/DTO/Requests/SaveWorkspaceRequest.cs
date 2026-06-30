namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for renaming a workspace
/// (see <see cref="Controllers.WorkspaceController.SaveWorkspace"/>). Only non-<c>null</c> fields
/// are applied (partial update).
/// </summary>
public sealed class SaveWorkspaceRequest
{
    /// <summary>The workspace to update (required).</summary>
    public string ID { get; set; } = "";
    /// <summary>New name, if changing.</summary>
    public string? Name { get; set; }
}