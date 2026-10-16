using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Yasal3MasakOlcuSaklamaYaptirim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "masak_olcu_tl",
                table: "hareketler",
                type: "decimal(15,2)",
                nullable: true);

            // PAKET YASAL-3 (§ 4.1) — GERİ DOLDURMA YALNIZ KANITI OLAN SATIRLAR İÇİN:
            // HURDA_ALIS satırının MASAK ölçüsü = eşleşen gider pusulasının BRÜT bedeli
            // (hareket_id eşleşmesi, aynı kiracı). Pusulasız HURDA_ALIS (mükellef
            // satıcı) ve kolondan önceki TAKAS satırları NULL KALIR — brüt değer
            // defterde yoktur, uydurulmaz (kümülatif onları TL bacağıyla okur).
            migrationBuilder.Sql(@"UPDATE hareketler h
JOIN gider_pusulalari g ON g.hareket_id = h.id AND g.tenant_id = h.tenant_id
SET h.masak_olcu_tl = g.brut_tutar
WHERE h.tur = 'HURDA_ALIS' AND h.masak_olcu_tl IS NULL;");

            migrationBuilder.CreateTable(
                name: "saklama_bekletmeleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kapsam = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kimlik_tespiti_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    baslangic = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    bitis = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    kaldirildi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    kaldiran_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saklama_bekletmeleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "yaptirim_eslesmeleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    yaptirim_kaydi_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tur = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nokta = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bildirim_son_gunu = table.Column<DateOnly>(type: "date", nullable: true),
                    masak_bildirim_tarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    masak_evrak_no = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    not_metni = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guncelleyen_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_yaptirim_eslesmeleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "yaptirim_kayitlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    karar_no = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rg_tarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    liste_tarihi = table.Column<DateOnly>(type: "date", nullable: false),
                    ad_soyad = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dogum_tarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    uyruk = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kimlik_no = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kimlik_no_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sifre_surumu = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    eklendi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    kaldirildi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    olusturan = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_yaptirim_kayitlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_saklama_bekletmeleri_tenant_id_bitis",
                table: "saklama_bekletmeleri",
                columns: new[] { "tenant_id", "bitis" });

            migrationBuilder.CreateIndex(
                name: "IX_yaptirim_eslesmeleri_tenant_id_cari_id",
                table: "yaptirim_eslesmeleri",
                columns: new[] { "tenant_id", "cari_id" });

            migrationBuilder.CreateIndex(
                name: "IX_yaptirim_eslesmeleri_tenant_id_durum",
                table: "yaptirim_eslesmeleri",
                columns: new[] { "tenant_id", "durum" });

            migrationBuilder.CreateIndex(
                name: "IX_yaptirim_kayitlari_ad_soyad",
                table: "yaptirim_kayitlari",
                column: "ad_soyad");

            migrationBuilder.CreateIndex(
                name: "IX_yaptirim_kayitlari_kimlik_no_hash",
                table: "yaptirim_kayitlari",
                column: "kimlik_no_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saklama_bekletmeleri");

            migrationBuilder.DropTable(
                name: "yaptirim_eslesmeleri");

            migrationBuilder.DropTable(
                name: "yaptirim_kayitlari");

            migrationBuilder.DropColumn(
                name: "masak_olcu_tl",
                table: "hareketler");
        }
    }
}
