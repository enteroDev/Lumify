namespace lumify.api.Models.DTO.Responses
{
    public class FriendshipResponse
    {
        public string ID { get; set; } = null!;
        public string RequesterID { get; set; } = null!;
        public string AddresseeID { get; set; } = null!;

        public int Status { get; set; }
        public string CreatedAt { get; set; } = null!;

        public string? RequesterUsername { get; set; }
        public string? RequesterDisplayName { get; set; }
        public string? RequesterAvatarUrl { get; set; }

        public string? FriendUserID { get; set; }
        public string? FriendUsername { get; set; }
        public string? FriendDisplayName { get; set; }
        public string? FriendAvatarUrl { get; set; }
    }
}