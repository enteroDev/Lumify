

namespace lumify.api.Models.DTO.Responses
{
    /// <summary>
    /// A chat message returned to clients, with the sender's resolved display name
    /// (see <see cref="Controllers.ChatController.GetMessagesOfRoom"/>).
    /// </summary>
    public class ChatMessageResponse
    {
        /// <summary>Message ID.</summary>
        public string ID { get; set; } = null!;
        /// <summary>The room the message belongs to.</summary>
        public string RoomID { get; set; } = null!;
        /// <summary>The sender's user ID.</summary>
        public string SenderID { get; set; } = null!;
        /// <summary>The sender's resolved display name (display name → username → full name → ID).</summary>
        public string SenderName { get; set; } = null!;
        /// <summary>The message text.</summary>
        public string Content { get; set; } = null!;
        /// <summary>Creation timestamp (ISO-8601 string).</summary>
        public string CreatedAt { get; set; } = null!;
        /// <summary>Last-update timestamp (ISO-8601 string).</summary>
        public string UpdatedAt { get; set; } = null!;
    }
}