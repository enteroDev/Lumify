namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for creating a calendar event (see <see cref="Controllers.EventsController.AddEvent"/>).
/// </summary>
public class AddEventRequest
{
    /// <summary>Target workspace, or <c>null</c> for a personal event.</summary>
    public string? WorkspaceID { get; set; }

    /// <summary>Event title (required).</summary>
    public string Name { get; set; } = "";
    /// <summary>Optional event description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether the event spans the whole day.</summary>
    public bool IsAllDay { get; set; }

    /// <summary>Start date/time (parseable date string, required).</summary>
    public string StartTime { get; set; } = "";
    /// <summary>End date/time (parseable date string, required; must not be before start).</summary>
    public string EndTime { get; set; } = "";
}