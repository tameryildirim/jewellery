using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class YedekOtomasyonu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "yedek_kayitlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tur = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    baslangic = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    bitis = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dosya_adi = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    boyut_bayt = table.Column<long>(type: "bigint", nullable: true),
                    sha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sifreli = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dis_kopya = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dogrulama = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dogrulama_tarih = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    tablo_sayisi = table.Column<int>(type: "int", nullable: true),
                    hata_ozeti = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_yedek_kayitlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "yedek_talepleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    islendi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    yedek_kaydi_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    iptal_gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_yedek_talepleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "yedek_tatbikatlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tarih = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    yedek_kaydi_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tablo_sayisi = table.Column<int>(type: "int", nullable: false),
                    uyusan = table.Column<int>(type: "int", nullable: false),
                    uyusmayan = table.Column<int>(type: "int", nullable: false),
                    sonuc = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sure_sn = table.Column<int>(type: "int", nullable: false),
                    rapor = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sema_dusuruldu = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    rto_dakika_olculen = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_yedek_tatbikatlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_yedek_kayitlari_baslangic",
                table: "yedek_kayitlari",
                column: "baslangic");

            migrationBuilder.CreateIndex(
                name: "IX_yedek_kayitlari_tur_baslangic",
                table: "yedek_kayitlari",
                columns: new[] { "tur", "baslangic" });

            migrationBuilder.CreateIndex(
                name: "IX_yedek_talepleri_tenant_id_durum_olusturuldu",
                table: "yedek_talepleri",
                columns: new[] { "tenant_id", "durum", "olusturuldu" });

            migrationBuilder.CreateIndex(
                name: "IX_yedek_tatbikatlari_tarih",
                table: "yedek_tatbikatlari",
                column: "tarih");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "yedek_kayitlari");

            migrationBuilder.DropTable(
                name: "yedek_talepleri");

            migrationBuilder.DropTable(
                name: "yedek_tatbikatlari");
        }
    }
}
