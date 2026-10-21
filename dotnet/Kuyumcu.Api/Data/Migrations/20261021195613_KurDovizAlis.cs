using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class KurDovizAlis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "eur_alis",
                table: "kur_snapshots",
                type: "decimal(15,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "gbp_alis",
                table: "kur_snapshots",
                type: "decimal(15,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "usd_alis",
                table: "kur_snapshots",
                type: "decimal(15,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "eur_alis",
                table: "kur_snapshots");

            migrationBuilder.DropColumn(
                name: "gbp_alis",
                table: "kur_snapshots");

            migrationBuilder.DropColumn(
                name: "usd_alis",
                table: "kur_snapshots");
        }
    }
}
