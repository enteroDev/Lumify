using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A single-use token backing an e-mail verification link. Only the hash of the token is stored
/// (same approach as <see cref="PasswordResetToken"/>), so a leaked row cannot be turned back
/// into a working link.
/// </summary>
public partial class EmailVerificationToken
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The user this verification token belongs to.</summary>
    public string UserID { get; set; } = null!;

    /// <summary>SHA-256 hash of the raw token; the raw value is never persisted.</summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>Expiry timestamp (ISO-8601 string).</summary>
    public string ExpiresAt { get; set; } = null!;
    /// <summary>When the token was consumed (ISO-8601 string); <c>null</c> while still usable (single-use).</summary>
    public string? UsedAt { get; set; }
    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;


    /// <summary>The owning user.</summary>
    public virtual User User { get; set; } = null!;
}
