using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace csharpAPI.Migrations
{
    /// <inheritdoc />
    public partial class SyntaxRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.RenameColumn(
                name: "name_category",
                table: "items",
                newName: "item_name");
                */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.RenameColumn(
                name: "item_name",
                table: "items",
                newName: "name_category");
                */
        }
    }
}
