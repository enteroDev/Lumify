namespace lumify.api.Models.Enum
{
    /// <summary>
    /// The status of a <see cref="EF.Friendship"/>, stored as an integer in the database.
    /// </summary>
    public enum FriendshipStatus
    {
        /// <summary>A request has been sent and is awaiting a response.</summary>
        Pending = 0,
        /// <summary>The request was accepted; the users are friends.</summary>
        Accepted = 1,
        /// <summary>The request was rejected.</summary>
        Rejected = 2,
        /// <summary>The relationship is blocked.</summary>
        Blocked = 3
    }
}