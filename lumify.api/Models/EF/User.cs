using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

/// <summary>
/// A registered user account. The central entity most other content references via its owner.
/// Soft-deleted via <see cref="DeletedAt"/>; timestamps are stored as ISO-8601 strings.
/// </summary>
public partial class User
{
    /// <summary>Primary key (GUID string).</summary>
    public string ID { get; set; } = null!;
    /// <summary>Unique login name.</summary>
    public string Username {get; set;} = null!;
    /// <summary>The user's e-mail address (also usable as a login identifier).</summary>
    public string Email { get; set; } = null!;
    /// <summary>Base64-encoded HMAC-SHA512 password hash.</summary>
    public string PasswordHash { get; set; } = null!;
    /// <summary>Base64-encoded salt (the HMAC key) used for the password hash.</summary>
    public string? PasswordSalt { get; set; }
    /// <summary>Optional real first name (kept private; never exposed in listings).</summary>
    public string? FirstName { get; set; }
    /// <summary>Optional real last name (kept private; never exposed in listings).</summary>
    public string? LastName { get; set; }
    /// <summary>The user's role, stored as the name of the <see cref="Role"/> enum.</summary>
    public string Role { get; set; } = null!;
    /// <summary>Relative URL of the user's avatar image.</summary>
    public string? AvatarUrl { get; set; }
    /// <summary>Public display name shown to others (falls back to username when unset).</summary>
    public string? DisplayName { get; set; }
    /// <summary>Optional free-text profile bio.</summary>
    public string? Bio { get; set; }
    /// <summary>Creation timestamp (ISO-8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>Last-update timestamp (ISO-8601 string).</summary>
    public string UpdatedAt { get; set; } = null!;
    /// <summary>Soft-delete timestamp (ISO-8601 string); <c>null</c> while the account is active.</summary>
    public string? DeletedAt { get; set; }

    // --- Two-factor (TOTP) --- //
    /// <summary>Base32 shared TOTP secret. <c>null</c> until the user starts 2FA setup.</summary>
    public string? TotpSecret { get; set; }
    /// <summary>Whether 2FA is active (only true after setup is confirmed with a valid code); drives the login challenge.</summary>
    public bool TotpEnabled { get; set; }

    // --- Email verification --- //
    /// <summary>Whether the e-mail address is confirmed. Login is blocked until this is true.</summary>
    public bool EmailConfirmed { get; set; }


    /// <summary>Friendships where this user is the lexicographically lower of the user pair.</summary>
    public virtual ICollection<Friendship> FriendshipsAsUserLow { get; set; } = new List<Friendship>();
    /// <summary>Friendships where this user is the lexicographically higher of the user pair.</summary>
    public virtual ICollection<Friendship> FriendshipsAsUserHigh { get; set; } = new List<Friendship>();
    /// <summary>Friend requests this user has sent.</summary>
    public virtual ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
    /// <summary>Friend requests this user has received.</summary>
    public virtual ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();

    /// <summary>Calendar events owned by this user.</summary>
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    /// <summary>Folders owned by this user.</summary>
    public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    /// <summary>Note attachments owned by this user.</summary>
    public virtual ICollection<NoteAttachment> NoteAttachments { get; set; } = new List<NoteAttachment>();
    /// <summary>Notes owned by this user.</summary>
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    /// <summary>Todo entries owned by this user.</summary>
    public virtual ICollection<TodoEntry> TodoEntries { get; set; } = new List<TodoEntry>();
    /// <summary>Todo lists owned by this user.</summary>
    public virtual ICollection<TodoList> TodoLists { get; set; } = new List<TodoList>();
    /// <summary>Workspace memberships of this user.</summary>
    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
    /// <summary>Workspaces owned by this user.</summary>
    public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
    /// <summary>Chat messages sent by this user.</summary>
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}




/// <summary>
/// The role of a <see cref="User"/>, persisted to the database by name.
/// </summary>
public enum Role
{
    /// <summary>Standard user.</summary>
    User = 1,
    /// <summary>Administrator.</summary>
    Admin = 2
}