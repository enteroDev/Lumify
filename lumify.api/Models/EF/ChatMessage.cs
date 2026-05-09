using System;

namespace lumify.api.Models.EF;

public partial class ChatMessage
{
    public string ID { get; set; } = null!;
    public string RoomID { get; set; } = null!;
    public string SenderID { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string CreatedAt { get; set; } = null!;
    public string UpdatedAt { get; set; } = null!;
    public string? DeletedAt { get; set; }


    public virtual User Sender { get; set; } = null!;
}