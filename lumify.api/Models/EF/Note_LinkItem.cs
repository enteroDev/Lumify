using System;

namespace lumify.api.Models.EF;

/// <summary>
/// A link item module within a note — a labelled URL. Ordered within the note via
/// <see cref="NotePos"/>. Soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class Note_LinkItem
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;

    /// <summary>The note this link item belongs to.</summary>
    public string NoteID { get; set; } = null!;

    /// <summary>Optional display label for the link.</summary>
    public string? Label { get; set; }

    /// <summary>The target URL.</summary>
    public string Url { get; set; } = null!;

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;

    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;

    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while active.</summary>
    public string? DeletedAt { get; set; }

    /// <summary>Zero-based position of this module within its note.</summary>
    public int NotePos { get; set; }



    /// <summary>The parent note.</summary>
    public virtual Note Note { get; set; } = null!;
}