
namespace lumify.api.Models.DTO.Responses;


/// <summary>
/// A calendar event returned to clients (see <see cref="Controllers.EventsController"/>).
/// The all-day flag is exposed as a bool (mapped from the entity's integer).
/// </summary>
public class EventResponse
{
    /// <summary>Event ID.</summary>
    public string ID { get; set; } = "";
    /// <summary>The owner's user ID.</summary>
    public string OwnerID { get; set; } = "";
    /// <summary>The creator's display name (privacy-safe; "Gelöschter Benutzer" if the creator was deleted).</summary>
    public string CreatedBy { get; set; } = "";
    /// <summary>The owning workspace, or <c>null</c> for a personal event.</summary>
    public string? WorkspaceID { get; set; }

    /// <summary>Event title.</summary>
    public string Name { get; set; } = "";
    /// <summary>Optional event description.</summary>
    public string? Description { get; set; }

    /// <summary>Event status code.</summary>
    public int Status { get; set; }
    /// <summary>Whether the event spans the whole day.</summary>
    public bool IsAllDay { get; set; }

    /// <summary>Start date/time (ISO-8601 string).</summary>
    public string StartTime { get; set; } = "";
    /// <summary>End date/time (ISO-8601 string), if set.</summary>
    public string? EndTime { get; set; }

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = "";
}