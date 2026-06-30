using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A note container, either personal or workspace-shared, optionally filed in a folder. Its body
/// is an ordered list of modules (text blocks and link items). Soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class Note
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The user who created the note.</summary>
    public string OwnerID { get; set; } = null!;
    /// <summary>The owning workspace, or <c>null</c> for a personal note.</summary>
    public string? WorkspaceID { get; set; }
    /// <summary>The containing folder, or <c>null</c> if the note is at the root.</summary>
    public string? FolderID { get; set; }

    /// <summary>Note title.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while active.</summary>
    public string? DeletedAt { get; set; }



    /// <summary>The containing folder, if any.</summary>
    public virtual Folder? Folder { get; set; }
    /// <summary>The owning user.</summary>
    public virtual User Owner { get; set; } = null!;
    /// <summary>The owning workspace, if any.</summary>
    public virtual Workspace? Workspace { get; set; }

    /// <summary>File attachments of this note.</summary>
    public virtual ICollection<NoteAttachment> NoteAttachments { get; set; } = new List<NoteAttachment>();
    /// <summary>Link item modules of this note.</summary>
    public virtual ICollection<Note_LinkItem> Note_LinkItems { get; set; } = new List<Note_LinkItem>();
    /// <summary>Text block modules of this note.</summary>
    public virtual ICollection<Note_TextBlock> Note_TextBlocks { get; set; } = new List<Note_TextBlock>();
}
