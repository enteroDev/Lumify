using lumify.api.Models.Enum;


namespace lumify.api.Models.DTO.Responses
{
    /// <summary>
    /// A user that can be added to a workspace, returned by workspace member search
    /// (see <see cref="Controllers.UsersController.SearchAvailableUsersForWorkspace"/>), enriched
    /// with live presence status.
    /// </summary>
    public class RelatedUserResponse
    {
        /// <summary>The user's ID.</summary>
        public string UserID { get; set; } = "";
        /// <summary>The user's avatar URL.</summary>
        public string? AvatarUrl { get; set; }
        /// <summary>The user's display name.</summary>
        public string DisplayName { get; set; } = "";
        /// <summary>The user's username.</summary>
        public string Username { get; set; } = "";
        /// <summary>The user's e-mail address.</summary>
        public string Email { get; set; } = "";

        /// <summary>The user's current presence status.</summary>
        public PresenceStatus PresenceStatus { get; set; }
    }
}