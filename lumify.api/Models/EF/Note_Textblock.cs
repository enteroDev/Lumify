using System;

namespace lumify.api.Models.EF;

public partial class Note_TextBlock
{
    public string ID { get; set; } = null!;

    public string NoteID { get; set; } = null!;

    public int Type { get; set; }

    public string? Name { get; set; }

    public string? Content { get; set; }

    public string? CodeLanguage { get; set; }

    public int IsCollapsed { get; set; }

    public string CreatedAt { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    public string? DeletedAt { get; set; }

    public int NotePos { get; set; }

    

    public virtual Note Note { get; set; } = null!;
}