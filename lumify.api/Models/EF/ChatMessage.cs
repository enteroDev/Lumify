using System;

namespace lumify.api.Models.EF;

/// <summary>
/// A single chat message in a room. Persisted by <see cref="Hubs.ChatHub"/> and read back via
/// <see cref="Controllers.ChatController"/>. Soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class ChatMessage
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The chat room (SignalR group) this message belongs to.</summary>
    public string RoomID { get; set; } = null!;
    /// <summary>The user who sent the message.</summary>
    public string SenderID { get; set; } = null!;

    /// <summary>The message text.</summary>
    public string Content { get; set; } = null!;

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while active.</summary>
    public string? DeletedAt { get; set; }


    /// <summary>The sending user.</summary>
    public virtual User Sender { get; set; } = null!;
}