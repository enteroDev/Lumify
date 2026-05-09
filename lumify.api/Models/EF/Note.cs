using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class Note
{
    public string ID { get; set; } = null!;

    public string OwnerID { get; set; } = null!;

    public string? WorkspaceID { get; set; }

    public string? FolderID { get; set; }

    public string Name { get; set; } = null!;

    public string CreatedAt { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    public string? DeletedAt { get; set; }

    public virtual Folder? Folder { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual Workspace? Workspace { get; set; }

    public virtual ICollection<NoteAttachment> NoteAttachments { get; set; } = new List<NoteAttachment>();
    public virtual ICollection<Note_LinkItem> Note_LinkItems { get; set; } = new List<Note_LinkItem>();
    public virtual ICollection<Note_TextBlock> Note_TextBlocks { get; set; } = new List<Note_TextBlock>();
}
