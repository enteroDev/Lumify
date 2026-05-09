namespace lumify.api.Models.DTO.Requests


{
    public class SaveUserProfileRequest
    {
        public string? DisplayName { get; set; }

        public string? AvatarUrl { get; set; }

        public string? Bio { get; set; }
    }
}