using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class PasswordResetToken
{
    public string ID { get; set; } = null!;
    public string UserID { get; set; } = null!;

    // We never store the raw token. Only its SHA-256 hash is persisted, so a leaked
    // database row cannot be turned back into a working reset link.
    public string TokenHash { get; set; } = null!;

    public string ExpiresAt { get; set; } = null!;     // ISO string, like every other date in the schema
    public string? UsedAt { get; set; }                // set once the token is consumed (single-use)
    public string CreatedAt { get; set; } = null!;


    public virtual User User { get; set; } = null!;
}
