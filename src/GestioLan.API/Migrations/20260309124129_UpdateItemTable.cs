using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestioLan.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "image",
                table: "items",
                newName: "image_name");

            migrationBuilder.AddColumn<int>(
                name: "id_image",
                table: "items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "images",
                columns: table => new
                {
                    id_image = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    file_name = table.Column<string>(type: "longtext", nullable: false, collation: "armscii8_general_ci")
                        .Annotation("MySql:CharSet", "armscii8"),
                    items_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_images", x => x.id_image);
                })
                .Annotation("MySql:CharSet", "armscii8")
                .Annotation("Relational:Collation", "armscii8_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "images");

            migrationBuilder.DropColumn(
                name: "id_image",
                table: "items");

            migrationBuilder.RenameColumn(
                name: "image_name",
                table: "items",
                newName: "image");
        }
    }
}
