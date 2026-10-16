using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CariDovizBakiyesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "doviz_tutar",
                table: "hareketler",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "eur_bakiye",
                table: "cari_bakiyeler",
                type: "decimal(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "gbp_bakiye",
                table: "cari_bakiyeler",
                type: "decimal(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "usd_bakiye",
                table: "cari_bakiyeler",
                type: "decimal(15,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "doviz_tutar",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "eur_bakiye",
                table: "cari_bakiyeler");

            migrationBuilder.DropColumn(
                name: "gbp_bakiye",
                table: "cari_bakiyeler");

            migrationBuilder.DropColumn(
                name: "usd_bakiye",
                table: "cari_bakiyeler");
        }
    }
}
