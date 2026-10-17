using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class P0FaturaAliciDondur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "alici_adres",
                table: "faturalar",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "alici_kimlik_turu",
                table: "faturalar",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "alici_unvan",
                table: "faturalar",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "alici_vergi_no",
                table: "faturalar",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alici_adres",
                table: "faturalar");

            migrationBuilder.DropColumn(
                name: "alici_kimlik_turu",
                table: "faturalar");

            migrationBuilder.DropColumn(
                name: "alici_unvan",
                table: "faturalar");

            migrationBuilder.DropColumn(
                name: "alici_vergi_no",
                table: "faturalar");
        }
    }
}
