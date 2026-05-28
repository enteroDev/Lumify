

namespace lumify.api.Models.DTO.Responses
{
    public class TodoListResponse
    {
        public string ID { get; set; } = "";
        public string OwnerID { get; set; } = "";
        public string? WorkspaceID { get; set; }
        public string Name { get; set; } = "";
        public int Status { get; set; }   // 1 = Open, 2 = Done
        public int IsArchived { get; set; }
        public string CreatedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";
    }
}