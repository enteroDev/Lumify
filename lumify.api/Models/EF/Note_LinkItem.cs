using System;

namespace lumify.api.Models.EF;

public partial class Note_LinkItem
{
    public string ID { get; set; } = null!;

    public string NoteID { get; set; } = null!;

    public string? Label { get; set; }

    public string Url { get; set; } = null!;

    public string CreatedAt { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    public string? DeletedAt { get; set; }

    public int NotePos { get; set; }

    

    public virtual Note Note { get; set; } = null!;
}