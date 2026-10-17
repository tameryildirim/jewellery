using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class M7aBankaCariKarari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cari_karari",
                table: "banka_hareketleri",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "karar_hareket_id",
                table: "banka_hareketleri",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_banka_hareketleri_tenant_id_karar_hareket_id",
                table: "banka_hareketleri",
                columns: new[] { "tenant_id", "karar_hareket_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_banka_hareketleri_tenant_id_karar_hareket_id",
                table: "banka_hareketleri");

            migrationBuilder.DropColumn(
                name: "cari_karari",
                table: "banka_hareketleri");

            migrationBuilder.DropColumn(
                name: "karar_hareket_id",
                table: "banka_hareketleri");
        }
    }
}
