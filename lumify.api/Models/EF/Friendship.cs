using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A relationship between two users (friend request or accepted friendship). Stored once per
/// user pair using ordered <see cref="UserLowID"/>/<see cref="UserHighID"/> keys; the
/// requester/addressee fields record who initiated it. See <see cref="Services.FriendshipService"/>.
/// </summary>
public partial class Friendship
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;

    /// <summary>The lexicographically lower user ID of the pair (part of the unique key).</summary>
    public string UserLowID { get; set; } = null!;
    /// <summary>The lexicographically higher user ID of the pair (part of the unique key).</summary>
    public string UserHighID { get; set; } = null!;
    /// <summary>The user who sent the friend request.</summary>
    public string RequesterID { get; set; } = null!;
    /// <summary>The user who received the friend request.</summary>
    public string AddresseeID { get; set; } = null!;

    /// <summary>Relationship status (see <see cref="Models.Enum.FriendshipStatus"/>: Pending, Accepted, …).</summary>
    public int Status { get; set; }

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;
    /// <summary>When the request was accepted (ISO-8601 string), if applicable.</summary>
    public string? AcceptedAt { get; set; }
    /// <summary>When the request was rejected (ISO-8601 string), if applicable.</summary>
    public string? RejectedAt { get; set; }
    /// <summary>When the relationship was blocked (ISO-8601 string), if applicable.</summary>
    public string? BlockedAt { get; set; }
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while active.</summary>
    public string? DeletedAt { get; set; }



    /// <summary>The lower user of the pair.</summary>
    public virtual User UserLow { get; set; } = null!;
    /// <summary>The higher user of the pair.</summary>
    public virtual User UserHigh { get; set; } = null!;
    /// <summary>The user who sent the request.</summary>
    public virtual User Requester { get; set; } = null!;
    /// <summary>The user who received the request.</summary>
    public virtual User Addressee { get; set; } = null!;
}