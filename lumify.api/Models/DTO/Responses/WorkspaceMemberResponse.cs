

namespace lumify.api.Models.DTO.Responses
{
    /// <summary>
    /// A workspace member with their user profile and role
    /// (see <see cref="Controllers.WorkspaceController.GetWorkspaceMembers"/>).
    /// </summary>
    public class WorkspaceMemberResponse
    {
        /// <summary>The member's user ID.</summary>
        public string UserID { get; set; } = "";
        /// <summary>The member's avatar URL.</summary>
        public string? AvatarUrl { get; set; }
        /// <summary>The member's display name.</summary>
        public string DisplayName { get; set; } = "";
        /// <summary>The member's username.</summary>
        public string Username { get; set; } = "";
        /// <summary>The member's e-mail address.</summary>
        public string Email { get; set; } = "";
        /// <summary>Membership role: 1 = Owner, 2 = Admin, 3 = User.</summary>
        public int Role { get; set; }
    }
}