using Kuyumcu.Api.Data.GeriDoldurma;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HareketOdemeIzi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "nakit_tutar",
                table: "hareketler",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "veresiye_tutar",
                table: "hareketler",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_cari_id_tarih",
                table: "hareketler",
                columns: new[] { "tenant_id", "cari_id", "tarih" });

            // Payı KESİN bilinen eski satırları doldur. Tahmin edilen pay
            // yazılmaz; bilinmeyen NULL kalır (bkz. NakitPayiGeriDoldurma).
            foreach (var sql in NakitPayiGeriDoldurma.Sirayla)
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_cari_id_tarih",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "nakit_tutar",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "veresiye_tutar",
                table: "hareketler");
        }
    }
}
