using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A calendar event, either personal (<see cref="WorkspaceID"/> <c>null</c>) or workspace-shared.
/// Soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class Event
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The user who created the event.</summary>
    public string OwnerID { get; set; } = null!;
    /// <summary>The owning workspace, or <c>null</c> for a personal event.</summary>
    public string? WorkspaceID { get; set; }

    /// <summary>Event title.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Optional event description.</summary>
    public string? Description { get; set; }

    /// <summary>Event status code.</summary>
    public int Status { get; set; }
    /// <summary>Start date/time (ISO-8601 string).</summary>
    public string StartDate { get; set; } = null!;
    /// <summary>End date/time (ISO-8601 string), if set.</summary>
    public string? EndDate { get; set; }

    /// <summary>All-day flag stored as an integer (0 = no, 1 = yes).</summary>
    public int IsAllDay { get; set; }
    /// <summary>Optional due date (ISO-8601 string).</summary>
    public string? DueDate { get; set; }

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while active.</summary>
    public string? DeletedAt { get; set; }



    /// <summary>The owning user.</summary>
    public virtual User Owner { get; set; } = null!;
    /// <summary>The owning workspace, if any.</summary>
    public virtual Workspace? Workspace { get; set; }
}
