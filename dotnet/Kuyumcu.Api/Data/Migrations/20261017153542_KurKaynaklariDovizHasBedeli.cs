using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class KurKaynaklariDovizHasBedeli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "doviz_kaynagi",
                table: "tenants",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ozel_matrah_kaynagi",
                table: "tenants",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "doviz_kaynak",
                table: "kur_snapshots",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "ozel_matrah_kapanis_fiyati",
                table: "faturalar",
                type: "decimal(15,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ozel_matrah_kapanis_gunu",
                table: "faturalar",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ozel_matrah_kaynak_kodu",
                table: "faturalar",
                type: "varchar(12)",
                maxLength: 12,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ozel_matrah_kapanislari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gun = table.Column<DateOnly>(type: "date", nullable: false),
                    fiyat = table.Column<decimal>(type: "decimal(15,4)", nullable: false),
                    girilen_deger = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    girilen_birim = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dayanak = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaydeden_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ozel_matrah_kapanislari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ozel_matrah_kapanislari_tenant_id_kaynak_gun",
                table: "ozel_matrah_kapanislari",
                columns: new[] { "tenant_id", "kaynak", "gun" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ozel_matrah_kapanislari");

            migrationBuilder.DropColumn(
                name: "doviz_kaynagi",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "ozel_matrah_kaynagi",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "doviz_kaynak",
                table: "kur_snapshots");

            migrationBuilder.DropColumn(
                name: "ozel_matrah_kapanis_fiyati",
                table: "faturalar");

            migrationBuilder.DropColumn(
                name: "ozel_matrah_kapanis_gunu",
                table: "faturalar");

            migrationBuilder.DropColumn(
                name: "ozel_matrah_kaynak_kodu",
                table: "faturalar");
        }
    }
}
