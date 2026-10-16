using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SubeSuzgecIndeksleriP04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_stoklar_tenant_id_sube_id",
                table: "stoklar",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stok_parti_tuketimleri_tenant_id_sube_id",
                table: "stok_parti_tuketimleri",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_siparisler_tenant_id_sube_id",
                table: "siparisler",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_tahsilatlari_tenant_id_sube_id",
                table: "pos_tahsilatlari",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_kiymetli_evraklar_tenant_id_sube_id",
                table: "kiymetli_evraklar",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_kasalar_tenant_id_sube_id",
                table: "kasalar",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_gun_kapanislari_tenant_id_sube_id",
                table: "gun_kapanislari",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_gider_pusulalari_tenant_id_sube_id",
                table: "gider_pusulalari",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_acilis_kalemleri_tenant_id_sube_id",
                table: "acilis_kalemleri",
                columns: new[] { "tenant_id", "sube_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stoklar_tenant_id_sube_id",
                table: "stoklar");

            migrationBuilder.DropIndex(
                name: "IX_stok_parti_tuketimleri_tenant_id_sube_id",
                table: "stok_parti_tuketimleri");

            migrationBuilder.DropIndex(
                name: "IX_siparisler_tenant_id_sube_id",
                table: "siparisler");

            migrationBuilder.DropIndex(
                name: "IX_pos_tahsilatlari_tenant_id_sube_id",
                table: "pos_tahsilatlari");

            migrationBuilder.DropIndex(
                name: "IX_kiymetli_evraklar_tenant_id_sube_id",
                table: "kiymetli_evraklar");

            migrationBuilder.DropIndex(
                name: "IX_kasalar_tenant_id_sube_id",
                table: "kasalar");

            migrationBuilder.DropIndex(
                name: "IX_gun_kapanislari_tenant_id_sube_id",
                table: "gun_kapanislari");

            migrationBuilder.DropIndex(
                name: "IX_gider_pusulalari_tenant_id_sube_id",
                table: "gider_pusulalari");

            migrationBuilder.DropIndex(
                name: "IX_acilis_kalemleri_tenant_id_sube_id",
                table: "acilis_kalemleri");
        }
    }
}
