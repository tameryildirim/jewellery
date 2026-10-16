using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RaporDortKartMaliyetiIndeksi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_urunler_tenant_id_alis_maliyeti",
                table: "urunler",
                columns: new[] { "tenant_id", "alis_maliyeti" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_urunler_tenant_id_alis_maliyeti",
                table: "urunler");
        }
    }
}
