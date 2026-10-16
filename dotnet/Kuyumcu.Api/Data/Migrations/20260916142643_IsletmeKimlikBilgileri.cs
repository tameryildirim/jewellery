using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IsletmeKimlikBilgileri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "adres",
                table: "tenants",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "telefon",
                table: "tenants",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "vergi_dairesi",
                table: "tenants",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "vergi_no",
                table: "tenants",
                type: "varchar(11)",
                maxLength: 11,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "adres",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "telefon",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "vergi_dairesi",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "vergi_no",
                table: "tenants");
        }
    }
}
