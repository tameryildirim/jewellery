using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MasakBankaKaynagi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kaynak_tur",
                table: "masak_kimlik_tespitleri",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "HAREKET")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "kaynak_tur",
                table: "masak_beyanlari",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "HAREKET")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "hareket_id",
                table: "banka_hareketleri",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_banka_hareketleri_tenant_id_cari_id_olusturuldu",
                table: "banka_hareketleri",
                columns: new[] { "tenant_id", "cari_id", "olusturuldu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_banka_hareketleri_tenant_id_cari_id_olusturuldu",
                table: "banka_hareketleri");

            migrationBuilder.DropColumn(
                name: "kaynak_tur",
                table: "masak_kimlik_tespitleri");

            migrationBuilder.DropColumn(
                name: "kaynak_tur",
                table: "masak_beyanlari");

            migrationBuilder.DropColumn(
                name: "hareket_id",
                table: "banka_hareketleri");
        }
    }
}
