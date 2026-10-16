using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class M1bM2bAlisMilyemiKdvBinde : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "alis_milyemi",
                table: "urunler",
                type: "decimal(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "kdv_matrah_binde",
                table: "urunler",
                type: "decimal(7,3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alis_milyemi",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "kdv_matrah_binde",
                table: "urunler");
        }
    }
}
