namespace lumify.api.Models.DTO.Responses
{
    /// <summary>
    /// A friendship/friend-request returned to clients. The requester fields are populated for
    /// incoming requests; the friend fields for outgoing requests
    /// (see <see cref="Controllers.FriendshipController"/>).
    /// </summary>
    public class FriendshipResponse
    {
        /// <summary>Friendship ID.</summary>
        public string ID { get; set; } = null!;
        /// <summary>The user who sent the request.</summary>
        public string RequesterID { get; set; } = null!;
        /// <summary>The user who received the request.</summary>
        public string AddresseeID { get; set; } = null!;

        /// <summary>Relationship status (see <see cref="Models.Enum.FriendshipStatus"/>).</summary>
        public int Status { get; set; }
        /// <summary>Creation timestamp (ISO-8601 string).</summary>
        public string CreatedAt { get; set; } = null!;

        /// <summary>Requester's username (populated for incoming requests).</summary>
        public string? RequesterUsername { get; set; }
        /// <summary>Requester's display name (populated for incoming requests).</summary>
        public string? RequesterDisplayName { get; set; }
        /// <summary>Requester's avatar URL (populated for incoming requests).</summary>
        public string? RequesterAvatarUrl { get; set; }

        /// <summary>The addressee's user ID (populated for outgoing requests).</summary>
        public string? FriendUserID { get; set; }
        /// <summary>The addressee's username (populated for outgoing requests).</summary>
        public string? FriendUsername { get; set; }
        /// <summary>The addressee's display name (populated for outgoing requests).</summary>
        public string? FriendDisplayName { get; set; }
        /// <summary>The addressee's avatar URL (populated for outgoing requests).</summary>
        public string? FriendAvatarUrl { get; set; }
    }
}