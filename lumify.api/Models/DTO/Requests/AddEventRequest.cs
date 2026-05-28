namespace lumify.api.Models.DTO.Requests;

public class AddEventRequest
{
    public string? WorkspaceID { get; set; }

    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public bool IsAllDay { get; set; }

    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
}