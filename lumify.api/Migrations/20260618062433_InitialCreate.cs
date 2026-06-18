using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lumify.api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarUrl = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    Bio = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    RoomID = table.Column<string>(type: "TEXT", nullable: false),
                    SenderID = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChatMessage_User_SenderID",
                        column: x => x.SenderID,
                        principalTable: "User",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Friendship",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    UserLowID = table.Column<string>(type: "TEXT", nullable: false),
                    UserHighID = table.Column<string>(type: "TEXT", nullable: false),
                    RequesterID = table.Column<string>(type: "TEXT", nullable: false),
                    AddresseeID = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptedAt = table.Column<string>(type: "TEXT", nullable: true),
                    RejectedAt = table.Column<string>(type: "TEXT", nullable: true),
                    BlockedAt = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendship", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Friendship_User_AddresseeID",
                        column: x => x.AddresseeID,
                        principalTable: "User",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Friendship_User_RequesterID",
                        column: x => x.RequesterID,
                        principalTable: "User",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Friendship_User_UserHighID",
                        column: x => x.UserHighID,
                        principalTable: "User",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Friendship_User_UserLowID",
                        column: x => x.UserLowID,
                        principalTable: "User",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Workspace",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerID = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspace", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Workspace_User_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "User",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerID = table.Column<string>(type: "TEXT", nullable: false),
                    WorkspaceID = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<string>(type: "TEXT", nullable: false),
                    EndDate = table.Column<string>(type: "TEXT", nullable: true),
                    IsAllDay = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDate = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Event_User_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "User",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Event_Workspace_WorkspaceID",
                        column: x => x.WorkspaceID,
                        principalTable: "Workspace",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Folder",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerID = table.Column<string>(type: "TEXT", nullable: false),
                    WorkspaceID = table.Column<string>(type: "TEXT", nullable: true),
                    ParentFolderID = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folder", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Folder_Folder_ParentFolderID",
                        column: x => x.ParentFolderID,
                        principalTable: "Folder",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Folder_User_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "User",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Folder_Workspace_WorkspaceID",
                        column: x => x.WorkspaceID,
                        principalTable: "Workspace",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "TodoList",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerID = table.Column<string>(type: "TEXT", nullable: false),
                    WorkspaceID = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoList", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TodoList_User_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "User",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TodoList_Workspace_WorkspaceID",
                        column: x => x.WorkspaceID,
                        principalTable: "Workspace",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceMember",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    WorkspaceID = table.Column<string>(type: "TEXT", nullable: false),
                    UserID = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceMember", x => x.ID);
                    table.ForeignKey(
                        name: "FK_WorkspaceMember_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_WorkspaceMember_Workspace_WorkspaceID",
                        column: x => x.WorkspaceID,
                        principalTable: "Workspace",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Note",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerID = table.Column<string>(type: "TEXT", nullable: false),
                    WorkspaceID = table.Column<string>(type: "TEXT", nullable: true),
                    FolderID = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Note", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Note_Folder_FolderID",
                        column: x => x.FolderID,
                        principalTable: "Folder",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Note_User_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "User",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Note_Workspace_WorkspaceID",
                        column: x => x.WorkspaceID,
                        principalTable: "Workspace",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "TodoEntry",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerID = table.Column<string>(type: "TEXT", nullable: false),
                    TodoListID = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoEntry", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TodoEntry_TodoList_TodoListID",
                        column: x => x.TodoListID,
                        principalTable: "TodoList",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_TodoEntry_User_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "User",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Note_LinkItem",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    NoteID = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true),
                    NotePos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Note_LinkItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Note_LinkItem_Note_NoteID",
                        column: x => x.NoteID,
                        principalTable: "Note",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Note_TextBlock",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    NoteID = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    CodeLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    IsCollapsed = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true),
                    NotePos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Note_TextBlock", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Note_TextBlock_Note_NoteID",
                        column: x => x.NoteID,
                        principalTable: "Note",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "NoteAttachment",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    NoteID = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerID = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteAttachment", x => x.ID);
                    table.ForeignKey(
                        name: "FK_NoteAttachment_Note_NoteID",
                        column: x => x.NoteID,
                        principalTable: "Note",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_NoteAttachment_User_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "User",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "ix_chatmessage_room",
                table: "ChatMessage",
                column: "RoomID");

            migrationBuilder.CreateIndex(
                name: "ix_chatmessage_room_created",
                table: "ChatMessage",
                columns: new[] { "RoomID", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_chatmessage_sender",
                table: "ChatMessage",
                column: "SenderID");

            migrationBuilder.CreateIndex(
                name: "ix_event_owner",
                table: "Event",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "ix_event_owner_start",
                table: "Event",
                columns: new[] { "OwnerID", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "ix_event_workspace",
                table: "Event",
                column: "WorkspaceID");

            migrationBuilder.CreateIndex(
                name: "ix_event_workspace_start",
                table: "Event",
                columns: new[] { "WorkspaceID", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "ix_folder_owner",
                table: "Folder",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "ix_folder_parent",
                table: "Folder",
                column: "ParentFolderID");

            migrationBuilder.CreateIndex(
                name: "ix_folder_workspace",
                table: "Folder",
                column: "WorkspaceID");

            migrationBuilder.CreateIndex(
                name: "ux_folder_private_parent_name",
                table: "Folder",
                columns: new[] { "OwnerID", "ParentFolderID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_folder_private_root_name",
                table: "Folder",
                columns: new[] { "OwnerID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_folder_workspace_parent_name",
                table: "Folder",
                columns: new[] { "WorkspaceID", "ParentFolderID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_folder_workspace_root_name",
                table: "Folder",
                columns: new[] { "WorkspaceID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_friendship_addressee",
                table: "Friendship",
                column: "AddresseeID");

            migrationBuilder.CreateIndex(
                name: "ix_friendship_requester",
                table: "Friendship",
                column: "RequesterID");

            migrationBuilder.CreateIndex(
                name: "ix_friendship_status",
                table: "Friendship",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_friendship_userhigh",
                table: "Friendship",
                column: "UserHighID");

            migrationBuilder.CreateIndex(
                name: "ix_friendship_userlow",
                table: "Friendship",
                column: "UserLowID");

            migrationBuilder.CreateIndex(
                name: "ux_friendship_pair",
                table: "Friendship",
                columns: new[] { "UserLowID", "UserHighID" },
                unique: true,
                filter: "DeletedAt IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_note_folder",
                table: "Note",
                column: "FolderID");

            migrationBuilder.CreateIndex(
                name: "ix_note_owner",
                table: "Note",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "ix_note_workspace",
                table: "Note",
                column: "WorkspaceID");

            migrationBuilder.CreateIndex(
                name: "ux_note_private_folder_name",
                table: "Note",
                columns: new[] { "OwnerID", "FolderID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_note_private_root_name",
                table: "Note",
                columns: new[] { "OwnerID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_note_workspace_folder_name",
                table: "Note",
                columns: new[] { "WorkspaceID", "FolderID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_note_workspace_root_name",
                table: "Note",
                columns: new[] { "WorkspaceID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_note_linkitem_note",
                table: "Note_LinkItem",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "ix_note_textblock_note",
                table: "Note_TextBlock",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "ix_noteattachment_note",
                table: "NoteAttachment",
                column: "NoteID");

            migrationBuilder.CreateIndex(
                name: "ix_noteattachment_owner",
                table: "NoteAttachment",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "ix_todoentry_list",
                table: "TodoEntry",
                column: "TodoListID");

            migrationBuilder.CreateIndex(
                name: "ix_todoentry_owner",
                table: "TodoEntry",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "ix_todolist_owner",
                table: "TodoList",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "ix_todolist_workspace",
                table: "TodoList",
                column: "WorkspaceID");

            migrationBuilder.CreateIndex(
                name: "ux_user_email",
                table: "User",
                column: "Email",
                unique: true,
                filter: "DeletedAt IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_user_username",
                table: "User",
                column: "Username",
                unique: true,
                filter: "DeletedAt IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_workspace_owner_name",
                table: "Workspace",
                columns: new[] { "OwnerID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workspacemember_user",
                table: "WorkspaceMember",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "ix_workspacemember_workspace",
                table: "WorkspaceMember",
                column: "WorkspaceID");

            migrationBuilder.CreateIndex(
                name: "ux_workspacemember_unique",
                table: "WorkspaceMember",
                columns: new[] { "WorkspaceID", "UserID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessage");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "Friendship");

            migrationBuilder.DropTable(
                name: "Note_LinkItem");

            migrationBuilder.DropTable(
                name: "Note_TextBlock");

            migrationBuilder.DropTable(
                name: "NoteAttachment");

            migrationBuilder.DropTable(
                name: "TodoEntry");

            migrationBuilder.DropTable(
                name: "WorkspaceMember");

            migrationBuilder.DropTable(
                name: "Note");

            migrationBuilder.DropTable(
                name: "TodoList");

            migrationBuilder.DropTable(
                name: "Folder");

            migrationBuilder.DropTable(
                name: "Workspace");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
