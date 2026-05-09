using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class Event
{
    public string ID { get; set; } = null!;

    public string OwnerID { get; set; } = null!;

    public string? WorkspaceID { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int Status { get; set; }

    public string StartDate { get; set; } = null!;

    public string? EndDate { get; set; }

    public int IsAllDay { get; set; }

    public string? DueDate { get; set; }

    public string CreatedAt { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    public string? DeletedAt { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual Workspace? Workspace { get; set; }
}
