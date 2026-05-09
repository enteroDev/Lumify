using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class Folder
{
    public string ID { get; set; } = null!;

    public string OwnerID { get; set; } = null!;

    public string? WorkspaceID { get; set; }

    public string? ParentFolderID { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string CreatedAt { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    public string? DeletedAt { get; set; }

    public virtual ICollection<Folder> InverseParentFolder { get; set; } = new List<Folder>();

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual User Owner { get; set; } = null!;

    public virtual Folder? ParentFolder { get; set; }

    public virtual Workspace? Workspace { get; set; }
}