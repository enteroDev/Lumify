

namespace lumify.api.Models.DTO.Responses
{
    public class WorkspaceMemberResponse
    {
        public string UserID { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public string DisplayName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public int Role { get; set; }
    }
}