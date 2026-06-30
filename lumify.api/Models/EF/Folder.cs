using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A folder that organizes notes, either personal or workspace-shared. Folders can be nested via
/// <see cref="ParentFolderID"/>. Soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class Folder
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The user who created the folder.</summary>
    public string OwnerID { get; set; } = null!;
    /// <summary>The owning workspace, or <c>null</c> for a personal folder.</summary>
    public string? WorkspaceID { get; set; }
    /// <summary>The parent folder, or <c>null</c> if the folder is at the root.</summary>
    public string? ParentFolderID { get; set; }

    /// <summary>Folder name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Optional folder description.</summary>
    public string? Description { get; set; }

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while active.</summary>
    public string? DeletedAt { get; set; }


    /// <summary>The owning user.</summary>
    public virtual User Owner { get; set; } = null!;
    /// <summary>The parent folder, if any.</summary>
    public virtual Folder? ParentFolder { get; set; }
    /// <summary>The owning workspace, if any.</summary>
    public virtual Workspace? Workspace { get; set; }


    /// <summary>Direct child folders nested under this folder.</summary>
    public virtual ICollection<Folder> InverseParentFolder { get; set; } = new List<Folder>();
    /// <summary>Notes contained directly in this folder.</summary>
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();


}