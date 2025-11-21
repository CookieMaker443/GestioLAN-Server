using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace csharpAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameItemNameColumn2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name_category",  // vecchio nome nel DB
                table: "items",
                newName: "item_name");  // nuovo nome
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "item_name",
                table: "items",
                newName: "name_category");
        }
    }
}
