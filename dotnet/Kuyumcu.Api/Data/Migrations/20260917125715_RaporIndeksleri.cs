using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RaporIndeksleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_kasa_hareketleri_tenant_id_kasa_id_tarih",
                table: "kasa_hareketleri",
                columns: new[] { "tenant_id", "kasa_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_kullanici_id_tarih",
                table: "hareketler",
                columns: new[] { "tenant_id", "kullanici_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_sube_id_tarih",
                table: "hareketler",
                columns: new[] { "tenant_id", "sube_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_tur_tarih",
                table: "hareketler",
                columns: new[] { "tenant_id", "tur", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_urun_id_tarih",
                table: "hareketler",
                columns: new[] { "tenant_id", "urun_id", "tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_kasa_hareketleri_tenant_id_kasa_id_tarih",
                table: "kasa_hareketleri");

            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_kullanici_id_tarih",
                table: "hareketler");

            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_sube_id_tarih",
                table: "hareketler");

            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_tur_tarih",
                table: "hareketler");

            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_urun_id_tarih",
                table: "hareketler");
        }
    }
}
