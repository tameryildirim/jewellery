using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class KiymetliEvrak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kiymetli_evraklar",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    evrak_turu = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    yon = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    para_birimi = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    vade_gunu = table.Column<DateOnly>(type: "date", nullable: false),
                    evrak_no = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    banka_adi = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    banka_ayirt_edici = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    banka_sube = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    keside_yeri = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    keside_tarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    borclu_cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    borclu_ad = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    borclu_kimlik_no = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    borclu_kimlik_no_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sifre_surumu = table.Column<int>(type: "int", nullable: false),
                    kaynak_cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    giris_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tahsile_veren_banka_hesap_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tahsile_verme_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    tahsil_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    tahsil_kasa_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tahsil_banka_hesap_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tahsil_kasa_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tahsil_banka_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    karsiliksiz_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    karsiliksiz_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    karsiliksiz_aciklama = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ciro_cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ciro_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ciro_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notlar = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kiymetli_evraklar", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_kiymetli_evraklar_tenant_id_borclu_cari_id",
                table: "kiymetli_evraklar",
                columns: new[] { "tenant_id", "borclu_cari_id" });

            migrationBuilder.CreateIndex(
                name: "IX_kiymetli_evraklar_tenant_id_borclu_kimlik_no_hash",
                table: "kiymetli_evraklar",
                columns: new[] { "tenant_id", "borclu_kimlik_no_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_kiymetli_evraklar_tenant_id_durum_vade_gunu",
                table: "kiymetli_evraklar",
                columns: new[] { "tenant_id", "durum", "vade_gunu" });

            migrationBuilder.CreateIndex(
                name: "IX_kiymetli_evraklar_tenant_id_evrak_turu_banka_ayirt_edici_evr~",
                table: "kiymetli_evraklar",
                columns: new[] { "tenant_id", "evrak_turu", "banka_ayirt_edici", "evrak_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kiymetli_evraklar_tenant_id_kaynak_cari_id",
                table: "kiymetli_evraklar",
                columns: new[] { "tenant_id", "kaynak_cari_id" });

            migrationBuilder.CreateIndex(
                name: "IX_kiymetli_evraklar_tenant_id_vade_gunu",
                table: "kiymetli_evraklar",
                columns: new[] { "tenant_id", "vade_gunu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kiymetli_evraklar");
        }
    }
}
