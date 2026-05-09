using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class NoteAttachment
{
    public string ID { get; set; } = null!;
    public string NoteID { get; set; } = null!;
    public string OwnerID { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public int FileSize { get; set; }

    public string CreatedAt { get; set; } = null!;
    public string? DeletedAt { get; set; }


    public virtual Note Note { get; set; } = null!;
    public virtual User Owner { get; set; } = null!;
}
