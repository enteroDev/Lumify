namespace lumify.api.Models.DTO.Requests;

public sealed class SaveFolderRequest
{
    public string ID { get; set; } = "";
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ParentFolderID { get; set; }
}