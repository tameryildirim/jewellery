using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class M1UrunSatisMilyemiOfset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "fiyat_ofset_tl",
                table: "urunler",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "satis_milyemi",
                table: "urunler",
                type: "decimal(7,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fiyat_ofset_tl",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "satis_milyemi",
                table: "urunler");
        }
    }
}
