using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class P21TicaretRezervSiparisBagiCiroRucu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "bloke_adet",
                table: "stoklar",
                type: "decimal(12,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "rucu_hareket_id",
                table: "kiymetli_evraklar",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "rucu_tarihi",
                table: "kiymetli_evraklar",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rucu_tutar",
                table: "kiymetli_evraklar",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siparis_id",
                table: "hareketler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rezervasyonlar",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urun_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    siparis_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    adet = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bitis_gunu = table.Column<DateOnly>(type: "date", nullable: true),
                    aciklama = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kapanis_gerekcesi = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    satis_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rezervasyonlar", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_siparis_id",
                table: "hareketler",
                columns: new[] { "tenant_id", "siparis_id" });

            migrationBuilder.CreateIndex(
                name: "IX_rezervasyonlar_tenant_id_cari_id_durum",
                table: "rezervasyonlar",
                columns: new[] { "tenant_id", "cari_id", "durum" });

            migrationBuilder.CreateIndex(
                name: "IX_rezervasyonlar_tenant_id_durum_bitis_gunu",
                table: "rezervasyonlar",
                columns: new[] { "tenant_id", "durum", "bitis_gunu" });

            migrationBuilder.CreateIndex(
                name: "IX_rezervasyonlar_tenant_id_siparis_id",
                table: "rezervasyonlar",
                columns: new[] { "tenant_id", "siparis_id" });

            migrationBuilder.CreateIndex(
                name: "IX_rezervasyonlar_tenant_id_sube_id_urun_id_durum",
                table: "rezervasyonlar",
                columns: new[] { "tenant_id", "sube_id", "urun_id", "durum" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rezervasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_siparis_id",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "bloke_adet",
                table: "stoklar");

            migrationBuilder.DropColumn(
                name: "rucu_hareket_id",
                table: "kiymetli_evraklar");

            migrationBuilder.DropColumn(
                name: "rucu_tarihi",
                table: "kiymetli_evraklar");

            migrationBuilder.DropColumn(
                name: "rucu_tutar",
                table: "kiymetli_evraklar");

            migrationBuilder.DropColumn(
                name: "siparis_id",
                table: "hareketler");
        }
    }
}
