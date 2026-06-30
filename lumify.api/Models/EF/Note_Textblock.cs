using System;

namespace lumify.api.Models.EF;

/// <summary>
/// A text block module within a note (e.g. paragraph, heading or code block). Ordered within the
/// note via <see cref="NotePos"/>. Soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class Note_TextBlock
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;

    /// <summary>The note this text block belongs to.</summary>
    public string NoteID { get; set; } = null!;

    /// <summary>Block type code (distinguishes paragraph, heading, code, …).</summary>
    public int Type { get; set; }

    /// <summary>Optional block title/heading.</summary>
    public string? Name { get; set; }

    /// <summary>The block's text content.</summary>
    public string? Content { get; set; }

    /// <summary>For code blocks, the programming language for syntax highlighting.</summary>
    public string? CodeLanguage { get; set; }

    /// <summary>Collapsed-in-UI flag stored as an integer (0 = expanded, 1 = collapsed).</summary>
    public int IsCollapsed { get; set; }

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