

namespace lumify.api.Models.DTO.Responses
{
    public class TodoEntryResponse
    {
        public string ID { get; set; } = "";
        public string TodoListID { get; set; } = "";
        public string OwnerID { get; set; } = "";

        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int Status { get; set; }   // 1 = Open, 2 = Done

        public bool WasLastUnchecked { get; set; } = false;

        public string CreatedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";
    }
}