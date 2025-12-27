using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace csharpAPI.Migrations
{
    /// <inheritdoc />
    public partial class AumentoLunghezzaPass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "user",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                collation: "armscii8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32)
                .Annotation("MySql:CharSet", "armscii8")
                .OldAnnotation("MySql:CharSet", "armscii8")
                .OldAnnotation("Relational:Collation", "armscii8_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "user",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                collation: "armscii8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64)
                .Annotation("MySql:CharSet", "armscii8")
                .OldAnnotation("MySql:CharSet", "armscii8")
                .OldAnnotation("Relational:Collation", "armscii8_general_ci");
        }
    }
}
