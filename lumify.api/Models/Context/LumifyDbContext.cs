// Database Context for EF

using System;
using Microsoft.EntityFrameworkCore;
using lumify.api.Models.EF;

namespace lumify.api.Models.Context;

public partial class LumifyDbContext : DbContext
{
    public LumifyDbContext()
    {
    }

    public LumifyDbContext(DbContextOptions<LumifyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<TodoList> TodoLists { get; set; }
    public virtual DbSet<TodoEntry> TodoEntries { get; set; }

    public virtual DbSet<Folder> Folders { get; set; }
    public virtual DbSet<Note> Notes { get; set; }
    public virtual DbSet<Note_LinkItem> Note_LinkItems { get; set; }
    public virtual DbSet<Note_TextBlock> Note_TextBlocks { get; set; }
    public virtual DbSet<NoteAttachment> NoteAttachments { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
    public virtual DbSet<Friendship> Friendships { get; set; }

    public virtual DbSet<Workspace> Workspaces { get; set; }
    public virtual DbSet<WorkspaceMember> WorkspaceMembers { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Event --- //
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Event");

            entity.HasIndex(e => e.OwnerID, "ix_event_owner");
            entity.HasIndex(e => new { e.OwnerID, e.StartDate }, "ix_event_owner_start");
            entity.HasIndex(e => e.WorkspaceID, "ix_event_workspace");
            entity.HasIndex(e => new { e.WorkspaceID, e.StartDate }, "ix_event_workspace_start");

            entity.HasOne(d => d.Owner).WithMany(p => p.Events)
                .HasForeignKey(d => d.OwnerID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Workspace).WithMany(p => p.Events)
                .HasForeignKey(d => d.WorkspaceID);
        });

        // --- Folder --- //
        modelBuilder.Entity<Folder>(entity =>
        {
            entity.ToTable("Folder");

            entity.HasIndex(e => e.OwnerID, "ix_folder_owner");
            entity.HasIndex(e => e.ParentFolderID, "ix_folder_parent");
            entity.HasIndex(e => e.WorkspaceID, "ix_folder_workspace");

            entity.HasIndex(e => new { e.OwnerID, e.ParentFolderID, e.Name }, "ux_folder_private_parent_name").IsUnique();
            entity.HasIndex(e => new { e.OwnerID, e.Name }, "ux_folder_private_root_name").IsUnique();
            entity.HasIndex(e => new { e.WorkspaceID, e.ParentFolderID, e.Name }, "ux_folder_workspace_parent_name").IsUnique();
            entity.HasIndex(e => new { e.WorkspaceID, e.Name }, "ux_folder_workspace_root_name").IsUnique();

            entity.HasOne(d => d.Owner).WithMany(p => p.Folders)
                .HasForeignKey(d => d.OwnerID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ParentFolder).WithMany(p => p.InverseParentFolder)
                .HasForeignKey(d => d.ParentFolderID);

            entity.HasOne(d => d.Workspace).WithMany(p => p.Folders)
                .HasForeignKey(d => d.WorkspaceID);
        });

        // --- Note --- //
        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("Note");

            entity.HasIndex(e => e.FolderID, "ix_note_folder");
            entity.HasIndex(e => e.OwnerID, "ix_note_owner");
            entity.HasIndex(e => e.WorkspaceID, "ix_note_workspace");

            entity.HasIndex(e => new { e.OwnerID, e.FolderID, e.Name }, "ux_note_private_folder_name").IsUnique();
            entity.HasIndex(e => new { e.OwnerID, e.Name }, "ux_note_private_root_name").IsUnique();
            entity.HasIndex(e => new { e.WorkspaceID, e.FolderID, e.Name }, "ux_note_workspace_folder_name").IsUnique();
            entity.HasIndex(e => new { e.WorkspaceID, e.Name }, "ux_note_workspace_root_name").IsUnique();

            entity.HasOne(d => d.Folder).WithMany(p => p.Notes)
                .HasForeignKey(d => d.FolderID);

            entity.HasOne(d => d.Owner).WithMany(p => p.Notes)
                .HasForeignKey(d => d.OwnerID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Workspace).WithMany(p => p.Notes)
                .HasForeignKey(d => d.WorkspaceID);
        });

        // --- Note: LinkItem --- //
        modelBuilder.Entity<Note_LinkItem>(entity =>
        {
            entity.ToTable("Note_LinkItem");

            entity.HasIndex(e => e.NoteID, "ix_note_linkitem_note");

            entity.Property(e => e.NotePos).HasDefaultValue(0);

            entity.HasOne(d => d.Note).WithMany(p => p.Note_LinkItems)
                .HasForeignKey(d => d.NoteID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // --- Note: Textblock --- //
        modelBuilder.Entity<Note_TextBlock>(entity =>
        {
            entity.ToTable("Note_TextBlock");

            entity.HasIndex(e => e.NoteID, "ix_note_textblock_note");

            entity.Property(e => e.NotePos).HasDefaultValue(0);

            entity.HasOne(d => d.Note).WithMany(p => p.Note_TextBlocks)
                .HasForeignKey(d => d.NoteID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // --- NoteAttachment --- //
        modelBuilder.Entity<NoteAttachment>(entity =>
        {
            entity.ToTable("NoteAttachment");

            entity.HasIndex(e => e.NoteID, "ix_noteattachment_note");
            entity.HasIndex(e => e.OwnerID, "ix_noteattachment_owner");

            entity.HasOne(d => d.Note).WithMany(p => p.NoteAttachments)
                .HasForeignKey(d => d.NoteID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Owner).WithMany(p => p.NoteAttachments)
                .HasForeignKey(d => d.OwnerID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // --- TodoList --- //
        modelBuilder.Entity<TodoList>(entity =>
        {
            entity.ToTable("TodoList");

            entity.HasIndex(e => e.OwnerID, "ix_todolist_owner");
            entity.HasIndex(e => e.WorkspaceID, "ix_todolist_workspace");

            entity.HasOne(d => d.Owner).WithMany(p => p.TodoLists)
                .HasForeignKey(d => d.OwnerID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Workspace).WithMany(p => p.TodoLists)
                .HasForeignKey(d => d.WorkspaceID);
        });

        // --- TodoEntry --- //
        modelBuilder.Entity<TodoEntry>(entity =>
        {
            entity.ToTable("TodoEntry");

            entity.HasIndex(e => e.TodoListID, "ix_todoentry_list");
            entity.HasIndex(e => e.OwnerID, "ix_todoentry_owner");

            entity.HasOne(d => d.Owner).WithMany(p => p.TodoEntries)
                .HasForeignKey(d => d.OwnerID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TodoList).WithMany(p => p.TodoEntries)
                .HasForeignKey(d => d.TodoListID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // --- User --- //
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            // Email must be unique. MariaDB has no filtered indexes, so this is a plain
            // unique index (the registration check already treats soft-deleted emails as
            // taken, so behaviour is unchanged).
            entity.HasIndex(e => e.Email, "ux_user_email")
                .IsUnique();

            // Username must be unique (same MariaDB note as above).
            entity.HasIndex(e => e.Username, "ux_user_username")
                .IsUnique();

            // Role required
            entity.Property(e => e.Role)
                .HasConversion<string>()
                .IsRequired();

            // Username required
            entity.Property(e => e.Username)
                .IsRequired();

            // Optional profile fields
            entity.Property(e => e.AvatarUrl)
                .IsRequired(false);

            entity.Property(e => e.DisplayName)
                .IsRequired(false);

            entity.Property(e => e.Bio)
                .IsRequired(false);
        });

        // --- PasswordResetToken --- //
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetToken");

            // We look tokens up by their hash, so that lookup must be unique and indexed.
            entity.HasIndex(e => e.TokenHash, "ux_passwordresettoken_hash").IsUnique();
            entity.HasIndex(e => e.UserID, "ix_passwordresettoken_user");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // --- EmailVerificationToken --- //
        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.ToTable("EmailVerificationToken");

            // We look tokens up by their hash, so that lookup must be unique and indexed.
            entity.HasIndex(e => e.TokenHash, "ux_emailverificationtoken_hash").IsUnique();
            entity.HasIndex(e => e.UserID, "ix_emailverificationtoken_user");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // --- Friendship --- //
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.ToTable("Friendship");

            entity.HasIndex(e => e.UserLowID, "ix_friendship_userlow");
            entity.HasIndex(e => e.UserHighID, "ix_friendship_userhigh");
            entity.HasIndex(e => e.RequesterID, "ix_friendship_requester");
            entity.HasIndex(e => e.AddresseeID, "ix_friendship_addressee");
            entity.HasIndex(e => e.Status, "ix_friendship_status");

            // Plain unique pair (MariaDB has no filtered indexes). SendFriendRequest
            // resurrects a soft-deleted row instead of inserting a duplicate, so the
            // re-friend-after-unfriend flow still works without violating this index.
            entity.HasIndex(e => new { e.UserLowID, e.UserHighID }, "ux_friendship_pair")
                .IsUnique();

            entity.HasOne(d => d.UserLow).WithMany(p => p.FriendshipsAsUserLow)
                .HasForeignKey(d => d.UserLowID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.UserHigh).WithMany(p => p.FriendshipsAsUserHigh)
                .HasForeignKey(d => d.UserHighID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Requester).WithMany(p => p.SentFriendRequests)
                .HasForeignKey(d => d.RequesterID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Addressee).WithMany(p => p.ReceivedFriendRequests)
                .HasForeignKey(d => d.AddresseeID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // --- Workspace --- //
        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.ToTable("Workspace");

            entity.HasIndex(e => new { e.OwnerID, e.Name }, "ux_workspace_owner_name").IsUnique();

            entity.HasOne(d => d.Owner).WithMany(p => p.Workspaces)
                .HasForeignKey(d => d.OwnerID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // --- WorkspaceMember --- //
        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.ToTable("WorkspaceMember");

            entity.HasIndex(e => e.UserID, "ix_workspacemember_user");
            entity.HasIndex(e => e.WorkspaceID, "ix_workspacemember_workspace");
            entity.HasIndex(e => new { e.WorkspaceID, e.UserID }, "ux_workspacemember_unique").IsUnique();

            entity.HasOne(d => d.User).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Workspace).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.WorkspaceID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // --- ChatMessage --- //
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessage");

            entity.HasIndex(e => e.RoomID, "ix_chatmessage_room");
            entity.HasIndex(e => new { e.RoomID, e.CreatedAt }, "ix_chatmessage_room_created");
            entity.HasIndex(e => e.SenderID, "ix_chatmessage_sender");

            entity.HasOne(d => d.Sender).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.SenderID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);


}