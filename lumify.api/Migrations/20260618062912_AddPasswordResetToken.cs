using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lumify.api.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordResetToken",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    UserID = table.Column<string>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    UsedAt = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetToken", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PasswordResetToken_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "ix_passwordresettoken_user",
                table: "PasswordResetToken",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "ux_passwordresettoken_hash",
                table: "PasswordResetToken",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetToken");
        }
    }
}
