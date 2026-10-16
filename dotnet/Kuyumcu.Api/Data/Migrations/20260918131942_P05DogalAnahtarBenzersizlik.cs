using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class P05DogalAnahtarBenzersizlik : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UQ_urunler_tenant_barkod",
                table: "urunler",
                columns: new[] { "tenant_id", "barkod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_urunler_tenant_kod",
                table: "urunler",
                columns: new[] { "tenant_id", "kod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_stoklar_tenant_sube_urun",
                table: "stoklar",
                columns: new[] { "tenant_id", "sube_id", "urun_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_urunler_tenant_barkod",
                table: "urunler");

            migrationBuilder.DropIndex(
                name: "UQ_urunler_tenant_kod",
                table: "urunler");

            migrationBuilder.DropIndex(
                name: "UQ_stoklar_tenant_sube_urun",
                table: "stoklar");
        }
    }
}
