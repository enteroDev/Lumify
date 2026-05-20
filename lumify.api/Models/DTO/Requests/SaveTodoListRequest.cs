namespace lumify.api.Models.DTO.Requests;

public sealed class SaveTodoListRequest
{
    public string ID { get; set; } = "";
    public string? Name { get; set; }
    public int? Status { get; set; }
    public bool? IsArchived { get; set; }
}