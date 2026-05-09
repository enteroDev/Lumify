using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class TodoEntry
{
    public string ID { get; set; } = null!;
    public string OwnerID { get; set; } = null!;
    public string TodoListID { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public int Status { get; set; }

    public string CreatedAt { get; set; } = null!;
    public string UpdatedAt { get; set; } = null!;
    public string? DeletedAt { get; set; }


    public virtual User Owner { get; set; } = null!;
    public virtual TodoList TodoList { get; set; } = null!;
}
