using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class TodoList
{
    public string ID { get; set; } = null!;
    public string OwnerID { get; set; } = null!;
    public string? WorkspaceID { get; set; }

    public string Name { get; set; } = null!;

    public int Status { get; set; }
    public int IsArchived { get; set; }

    public string CreatedAt { get; set; } = null!;
    public string UpdatedAt { get; set; } = null!;
    public string? DeletedAt { get; set; }


    public virtual User Owner { get; set; } = null!;
    public virtual Workspace? Workspace { get; set; }

    public virtual ICollection<TodoEntry> TodoEntries { get; set; } = new List<TodoEntry>();
    
}
