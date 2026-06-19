
namespace lumify.api.Models.DTO.Responses;


public class EventResponse
{
    public string ID { get; set; } = "";
    public string OwnerID { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public string? WorkspaceID { get; set; }

    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public int Status { get; set; }
    public bool IsAllDay { get; set; }

    public string StartTime { get; set; } = "";
    public string? EndTime { get; set; }

    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}