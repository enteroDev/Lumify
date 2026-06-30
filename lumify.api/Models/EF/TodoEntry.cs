using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A single item within a <see cref="TodoList"/>. Toggling its status keeps the parent list's
/// status in sync. Soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class TodoEntry
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The user who created the entry.</summary>
    public string OwnerID { get; set; } = null!;
    /// <summary>The parent todo list.</summary>
    public string TodoListID { get; set; } = null!;

    /// <summary>Entry title.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Optional entry description.</summary>
    public string? Description { get; set; }

    /// <summary>Status: 1 = pending/open, 2 = done.</summary>
    public int Status { get; set; }

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while active.</summary>
    public string? DeletedAt { get; set; }


    /// <summary>The owning user.</summary>
    public virtual User Owner { get; set; } = null!;
    /// <summary>The parent todo list.</summary>
    public virtual TodoList TodoList { get; set; } = null!;
}
