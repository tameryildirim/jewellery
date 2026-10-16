using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmanetBirakildigiGibi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "emanet_ayar_id",
                table: "hareketler",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "emanet_gram",
                table: "hareketler",
                type: "decimal(15,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "emanet_tas",
                table: "hareketler",
                type: "decimal(15,3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "emanet_ayar_id",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "emanet_gram",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "emanet_tas",
                table: "hareketler");
        }
    }
}
