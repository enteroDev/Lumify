using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A todo list, either personal or workspace-shared, holding a set of <see cref="TodoEntry"/>
/// items. Its status is derived from its entries (done when all entries are done). Soft-deleted
/// via <see cref="DeletedAt"/>.
/// </summary>
public partial class TodoList
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The user who created the list.</summary>
    public string OwnerID { get; set; } = null!;
    /// <summary>The owning workspace, or <c>null</c> for a personal list.</summary>
    public string? WorkspaceID { get; set; }

    /// <summary>List name.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Status: 1 = pending/open, 2 = done.</summary>
    public int Status { get; set; }
    /// <summary>Archived flag stored as an integer (0 = active, 1 = archived).</summary>
    public int IsArchived { get; set; }

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

    /// <summary>The entries contained in this list.</summary>
    public virtual ICollection<TodoEntry> TodoEntries { get; set; } = new List<TodoEntry>();

}
