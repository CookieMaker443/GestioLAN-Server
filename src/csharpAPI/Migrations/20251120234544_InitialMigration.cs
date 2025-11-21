using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace csharpAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "armscii8");

            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id_category = table.Column<int>(type: "int", nullable: false),
                    name_category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "armscii8_general_ci")
                        .Annotation("MySql:CharSet", "armscii8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_category);
                })
                .Annotation("MySql:CharSet", "armscii8")
                .Annotation("Relational:Collation", "armscii8_general_ci");

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id_item = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name_category = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "armscii8_general_ci")
                        .Annotation("MySql:CharSet", "armscii8"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "armscii8_general_ci")
                        .Annotation("MySql:CharSet", "armscii8"),
                    image = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "armscii8_general_ci")
                        .Annotation("MySql:CharSet", "armscii8"),
                    id_category = table.Column<int>(type: "int", nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    type_quantity = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "armscii8_general_ci")
                        .Annotation("MySql:CharSet", "armscii8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_item);
                })
                .Annotation("MySql:CharSet", "armscii8")
                .Annotation("Relational:Collation", "armscii8_general_ci");

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    username = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false, collation: "armscii8_general_ci")
                        .Annotation("MySql:CharSet", "armscii8"),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "armscii8_general_ci")
                        .Annotation("MySql:CharSet", "armscii8"),
                    password = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "armscii8_general_ci")
                        .Annotation("MySql:CharSet", "armscii8"),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "armscii8")
                .Annotation("Relational:Collation", "armscii8_general_ci");

            migrationBuilder.CreateIndex(
                name: "nome_categoria_UNIQUE",
                table: "category",
                column: "name_category",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
