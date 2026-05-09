using System;
using System.Collections.Generic;

namespace lumify.api.Models.EF;

public partial class User
{
    public string ID { get; set; } = null!;
    public string Username {get; set;} = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? PasswordSalt { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Role { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string CreatedAt { get; set; } = null!;
    public string UpdatedAt { get; set; } = null!;
    public string? DeletedAt { get; set; }


    public virtual ICollection<Friendship> FriendshipsAsUserLow { get; set; } = new List<Friendship>();
    public virtual ICollection<Friendship> FriendshipsAsUserHigh { get; set; } = new List<Friendship>();
    public virtual ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
    public virtual ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    public virtual ICollection<NoteAttachment> NoteAttachments { get; set; } = new List<NoteAttachment>();
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    public virtual ICollection<TodoEntry> TodoEntries { get; set; } = new List<TodoEntry>();
    public virtual ICollection<TodoList> TodoLists { get; set; } = new List<TodoList>();
    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
    public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}




// ENUM for DB-Translations
public enum Role
{
    User = 1,
    Admin = 2
}