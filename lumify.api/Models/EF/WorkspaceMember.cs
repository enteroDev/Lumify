using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// Join entity linking a <see cref="User"/> to a <see cref="Workspace"/> with a role.
/// A removed membership is soft-deleted via <see cref="DeletedAt"/>.
/// </summary>
public partial class WorkspaceMember
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>The workspace this membership belongs to.</summary>
    public string WorkspaceID { get; set; } = null!;
    /// <summary>The member user.</summary>
    public string UserID { get; set; } = null!;

    /// <summary>Membership role: 1 = Owner, 2 = Admin, 3 = User.</summary>
    public int Role { get; set; }

    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while the membership is active.</summary>
    public string? DeletedAt { get; set; }


    /// <summary>The member user.</summary>
    public virtual User User { get; set; } = null!;
    /// <summary>The workspace.</summary>
    public virtual Workspace Workspace { get; set; } = null!;
}
