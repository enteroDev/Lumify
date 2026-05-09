using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class WorkspaceMember
{
    public string ID { get; set; } = null!;
    public string WorkspaceID { get; set; } = null!;
    public string UserID { get; set; } = null!;

    public int Role { get; set; }

    public string CreatedAt { get; set; } = null!;
    public string? DeletedAt { get; set; }


    public virtual User User { get; set; } = null!;
    public virtual Workspace Workspace { get; set; } = null!;
}
