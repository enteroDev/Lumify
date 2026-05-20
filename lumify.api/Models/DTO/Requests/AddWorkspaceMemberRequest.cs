namespace lumify.api.Models.DTO.Requests;

public sealed class AddWorkspaceMemberRequest
{
    public string WorkspaceID { get; set; } = "";
    public string UserID { get; set; } = "";
}