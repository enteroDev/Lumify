using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lumify.api.Migrations
{
    /// <inheritdoc />
    public partial class AddTotp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TotpEnabled",
                table: "User",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TotpSecret",
                table: "User",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotpEnabled",
                table: "User");

            migrationBuilder.DropColumn(
                name: "TotpSecret",
                table: "User");
        }
    }
}
