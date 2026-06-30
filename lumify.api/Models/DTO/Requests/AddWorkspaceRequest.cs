namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for creating a workspace
/// (see <see cref="Controllers.WorkspaceController.AddWorkspace"/>).
/// </summary>
public sealed class AddWorkspaceRequest
{
    /// <summary>Workspace name (required).</summary>
    public string Name { get; set; } = "";
}