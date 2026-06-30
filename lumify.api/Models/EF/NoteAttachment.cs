using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A file attached to a note. Stores the upload metadata and the name under which the file is
/// stored on disk. Soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class NoteAttachment
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The note this attachment belongs to.</summary>
    public string NoteID { get; set; } = null!;
    /// <summary>The user who uploaded the attachment.</summary>
    public string OwnerID { get; set; } = null!;

    /// <summary>The file name as uploaded by the user.</summary>
    public string OriginalFileName { get; set; } = null!;
    /// <summary>The (safe, unique) name under which the file is stored on disk.</summary>
    public string StoredFileName { get; set; } = null!;
    /// <summary>The file's MIME content type.</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>The file size in bytes.</summary>
    public int FileSize { get; set; }

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while active.</summary>
    public string? DeletedAt { get; set; }


    /// <summary>The parent note.</summary>
    public virtual Note Note { get; set; } = null!;
    /// <summary>The uploading user.</summary>
    public virtual User Owner { get; set; } = null!;
}
