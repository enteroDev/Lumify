
namespace lumify.api.Models.DTO.Requests

{
    /// <summary>
    /// Request body for updating the current user's profile
    /// (see <see cref="Controllers.UsersController.SaveUserProfile"/>). Only non-<c>null</c>
    /// fields are applied; empty values clear the respective field.
    /// </summary>
    public class SaveUserProfileRequest
    {
        /// <summary>New public display name, if changing.</summary>
        public string? DisplayName { get; set; }

        /// <summary>New avatar URL, if changing.</summary>
        public string? AvatarUrl { get; set; }

        /// <summary>New profile bio, if changing.</summary>
        public string? Bio { get; set; }
    }
}