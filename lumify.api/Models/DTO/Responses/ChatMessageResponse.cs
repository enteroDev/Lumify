

namespace lumify.api.Models.DTO.Responses
{
    public class ChatMessageResponse
    {
        public string ID { get; set; } = null!;
        public string RoomID { get; set; } = null!;
        public string SenderID { get; set; } = null!;
        public string SenderName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string CreatedAt { get; set; } = null!;
        public string UpdatedAt { get; set; } = null!;
    }
}