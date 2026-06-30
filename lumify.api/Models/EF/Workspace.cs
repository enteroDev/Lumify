using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A shared space that groups content (folders, notes, todo lists, events) and members.
/// Content belongs to the workspace, not its creator. Soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class Workspace
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The user who created and owns the workspace.</summary>
    public string OwnerID { get; set; } = null!;

    /// <summary>Display name of the workspace.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while active.</summary>
    public string? DeletedAt { get; set; }


    /// <summary>The owning user.</summary>
    public virtual User Owner { get; set; } = null!;

    /// <summary>Events belonging to this workspace.</summary>
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    /// <summary>Folders belonging to this workspace.</summary>
    public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    /// <summary>Notes belonging to this workspace.</summary>
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    /// <summary>Todo lists belonging to this workspace.</summary>
    public virtual ICollection<TodoList> TodoLists { get; set; } = new List<TodoList>();
    /// <summary>Memberships linking users to this workspace.</summary>
    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
