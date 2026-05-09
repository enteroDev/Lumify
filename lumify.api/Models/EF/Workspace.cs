using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class Workspace
{
    public string ID { get; set; } = null!;

    public string OwnerID { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string CreatedAt { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    public string? DeletedAt { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<TodoList> TodoLists { get; set; } = new List<TodoList>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
