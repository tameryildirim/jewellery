using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class KiraciIndeksleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_urunler_tenant_id",
                table: "urunler",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_subeler_tenant_id",
                table: "subeler",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stoklar_tenant_id",
                table: "stoklar",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokenler_tenant_id",
                table: "refresh_tokenler",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_kur_snapshots_tenant_id_olusturuldu",
                table: "kur_snapshots",
                columns: new[] { "tenant_id", "olusturuldu" });

            migrationBuilder.CreateIndex(
                name: "IX_kullanicilar_tenant_id",
                table: "kullanicilar",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_kasalar_tenant_id",
                table: "kasalar",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_kasa_hareketleri_tenant_id_tarih",
                table: "kasa_hareketleri",
                columns: new[] { "tenant_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_tarih",
                table: "hareketler",
                columns: new[] { "tenant_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_faturalar_tenant_id_tarih",
                table: "faturalar",
                columns: new[] { "tenant_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_cariler_tenant_id",
                table: "cariler",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_cari_bakiyeler_tenant_id",
                table: "cari_bakiyeler",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_banka_hesaplari_tenant_id",
                table: "banka_hesaplari",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_banka_hareketleri_tenant_id_tarih",
                table: "banka_hareketleri",
                columns: new[] { "tenant_id", "tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_urunler_tenant_id",
                table: "urunler");

            migrationBuilder.DropIndex(
                name: "IX_subeler_tenant_id",
                table: "subeler");

            migrationBuilder.DropIndex(
                name: "IX_stoklar_tenant_id",
                table: "stoklar");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokenler_tenant_id",
                table: "refresh_tokenler");

            migrationBuilder.DropIndex(
                name: "IX_kur_snapshots_tenant_id_olusturuldu",
                table: "kur_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_kullanicilar_tenant_id",
                table: "kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_kasalar_tenant_id",
                table: "kasalar");

            migrationBuilder.DropIndex(
                name: "IX_kasa_hareketleri_tenant_id_tarih",
                table: "kasa_hareketleri");

            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_tarih",
                table: "hareketler");

            migrationBuilder.DropIndex(
                name: "IX_faturalar_tenant_id_tarih",
                table: "faturalar");

            migrationBuilder.DropIndex(
                name: "IX_cariler_tenant_id",
                table: "cariler");

            migrationBuilder.DropIndex(
                name: "IX_cari_bakiyeler_tenant_id",
                table: "cari_bakiyeler");

            migrationBuilder.DropIndex(
                name: "IX_banka_hesaplari_tenant_id",
                table: "banka_hesaplari");

            migrationBuilder.DropIndex(
                name: "IX_banka_hareketleri_tenant_id_tarih",
                table: "banka_hareketleri");
        }
    }
}
