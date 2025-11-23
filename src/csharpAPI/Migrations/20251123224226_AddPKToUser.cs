using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace csharpAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPKToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddPrimaryKey(
                name: "PK_user",
                table: "user",
                column: "username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_user",
                table: "user");
        }
    }
}
