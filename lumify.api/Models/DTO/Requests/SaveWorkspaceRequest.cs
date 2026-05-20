namespace lumify.api.Models.DTO.Requests;

public sealed class SaveWorkspaceRequest
{
    public string ID { get; set; } = "";
    public string? Name { get; set; }
}