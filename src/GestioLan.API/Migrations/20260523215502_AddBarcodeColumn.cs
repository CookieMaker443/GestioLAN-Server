using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestioLan.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsAdmin",
                table: "user",
                type: "tinyint(1)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "items",
                type: "longtext",
                nullable: true,
                collation: "armscii8_general_ci")
                .Annotation("MySql:CharSet", "armscii8");

            migrationBuilder.AddColumn<string>(
                name: "AssociatedProviderName",
                table: "category",
                type: "longtext",
                nullable: true,
                collation: "armscii8_general_ci")
                .Annotation("MySql:CharSet", "armscii8");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "items");

            migrationBuilder.DropColumn(
                name: "AssociatedProviderName",
                table: "category");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAdmin",
                table: "user",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true);
        }
    }
}
