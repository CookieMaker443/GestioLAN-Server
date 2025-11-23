using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace csharpAPI.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSomeLenghts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "user",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                collation: "armscii8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16)
                .Annotation("MySql:CharSet", "armscii8")
                .OldAnnotation("MySql:CharSet", "armscii8")
                .OldAnnotation("Relational:Collation", "armscii8_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "item_name",
                table: "items",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                collation: "armscii8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "armscii8")
                .OldAnnotation("MySql:CharSet", "armscii8")
                .OldAnnotation("Relational:Collation", "armscii8_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "image",
                table: "items",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true,
                collation: "armscii8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "armscii8")
                .OldAnnotation("MySql:CharSet", "armscii8")
                .OldAnnotation("Relational:Collation", "armscii8_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "user",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                collation: "armscii8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32)
                .Annotation("MySql:CharSet", "armscii8")
                .OldAnnotation("MySql:CharSet", "armscii8")
                .OldAnnotation("Relational:Collation", "armscii8_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "item_name",
                table: "items",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                collation: "armscii8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64)
                .Annotation("MySql:CharSet", "armscii8")
                .OldAnnotation("MySql:CharSet", "armscii8")
                .OldAnnotation("Relational:Collation", "armscii8_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "image",
                table: "items",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "armscii8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldNullable: true)
                .Annotation("MySql:CharSet", "armscii8")
                .OldAnnotation("MySql:CharSet", "armscii8")
                .OldAnnotation("Relational:Collation", "armscii8_general_ci");
        }
    }
}
