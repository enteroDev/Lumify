namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for adding a member to a workspace
/// (see <see cref="Controllers.WorkspaceController.AddWorkspaceMember"/>).
/// </summary>
public sealed class AddWorkspaceMemberRequest
{
    /// <summary>The target workspace (required).</summary>
    public string WorkspaceID { get; set; } = "";
    /// <summary>The user to add (required).</summary>
    public string UserID { get; set; } = "";
}