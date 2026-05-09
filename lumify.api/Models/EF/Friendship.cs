using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class Friendship
{
    public string ID { get; set; } = null!;

    public string UserLowID { get; set; } = null!;

    public string UserHighID { get; set; } = null!;

    public string RequesterID { get; set; } = null!;

    public string AddresseeID { get; set; } = null!;

    public int Status { get; set; }

    public string CreatedAt { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    public string? AcceptedAt { get; set; }

    public string? RejectedAt { get; set; }

    public string? BlockedAt { get; set; }

    public string? DeletedAt { get; set; }

    public virtual User UserLow { get; set; } = null!;

    public virtual User UserHigh { get; set; } = null!;

    public virtual User Requester { get; set; } = null!;

    public virtual User Addressee { get; set; } = null!;
}