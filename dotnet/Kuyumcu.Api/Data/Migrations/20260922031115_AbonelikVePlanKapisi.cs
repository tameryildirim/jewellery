using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AbonelikVePlanKapisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abonelik_planlari",
                columns: table => new
                {
                    kod = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ad = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aciklama = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    seviye = table.Column<int>(type: "int", nullable: false),
                    kullanici_tavan = table.Column<int>(type: "int", nullable: true),
                    sube_tavan = table.Column<int>(type: "int", nullable: true),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abonelik_planlari", x => x.kod);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kiraci_abonelikleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    plan_kodu = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    baslangic = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    bitis = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    durum = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    operator_adi = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kiraci_abonelikleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_abonelik_planlari_seviye",
                table: "abonelik_planlari",
                column: "seviye");

            migrationBuilder.CreateIndex(
                name: "IX_kiraci_abonelikleri_tenant_id_baslangic",
                table: "kiraci_abonelikleri",
                columns: new[] { "tenant_id", "baslangic" });

            migrationBuilder.CreateIndex(
                name: "IX_kiraci_abonelikleri_tenant_id_durum_baslangic",
                table: "kiraci_abonelikleri",
                columns: new[] { "tenant_id", "durum", "baslangic" });

            migrationBuilder.CreateIndex(
                name: "IX_kiraci_abonelikleri_tenant_id_plan_kodu",
                table: "kiraci_abonelikleri",
                columns: new[] { "tenant_id", "plan_kodu" });

            // ── KATALOG TOHUMU (PAKET P-27) ──────────────────────────────────
            // Üç plan, bugün `tenants.plan` kolonunda GERÇEKTEN kullanılan
            // kodlarla birebir aynı (BASLANGIC · PRO · KURUMSAL): göç sonrası
            // hiçbir kiracının planı "katalogda yok" durumuna düşmez.
            // ⚠ TUTAR KOLONU YOK (K-16): rakamlı fiyat kurucu kalemidir.
            // ⚠ TAVANLAR NULL: kullanıcı/şube tavanı ürün davranışına HENÜZ
            //   bağlanmadı; dolu yazılsaydı uygulanmayan bir sınır vaat edilirdi.
            migrationBuilder.Sql(@"
INSERT INTO abonelik_planlari
    (kod, ad, aciklama, seviye, kullanici_tavan, sube_tavan, aktif, olusturuldu, guncellendi)
VALUES
    ('BASLANGIC', 'Başlangıç', 'Temel satış, stok ve cari takibi.', 10, NULL, NULL, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)),
    ('PRO',       'Pro',       'Çok şube, raporlar ve uyum modülleri.', 20, NULL, NULL, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)),
    ('KURUMSAL',  'Kurumsal',  'Tüm modüller ve öncelikli destek.', 30, NULL, NULL, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))
ON DUPLICATE KEY UPDATE kod = kod;");

            // ── GERİ DOLDURMA (PAKET P-27) ───────────────────────────────────
            // Her mevcut kiracıya, BUGÜNKÜ planından bir AÇILIŞ DÖNEMİ üretilir.
            // ⚠ bitis = NULL (SÜRESİZ): mevcut kiracıların yazması göç sonrası
            //   KESİLMEZ — fail-closed kapı yalnız bitişi OLAN döneme uygulanır.
            // ⚠ Planı katalogda OLMAYAN kiracıya satır ÜRETİLMEZ (uydurma plan
            //   yazmak yerine kayıtsız bırakılır; ekran gerçeği söyler).
            // ⚠ Yeniden koşmaya dayanıklı: aktif dönemi olan kiracı atlanır.
            migrationBuilder.Sql(@"
INSERT INTO kiraci_abonelikleri
    (id, tenant_id, plan_kodu, baslangic, bitis, durum, kaynak, gerekce, operator_adi, olusturuldu, guncellendi)
SELECT UUID(), t.id, t.plan, COALESCE(t.olusturuldu, UTC_TIMESTAMP(6)), NULL,
       'AKTIF', 'SISTEM',
       'P-27 göçü: mevcut tenants.plan değerinden üretilen açılış dönemi (süresiz).',
       'migration', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
FROM tenants t
WHERE t.plan IN (SELECT kod FROM abonelik_planlari)
  AND NOT EXISTS (
        SELECT 1 FROM kiraci_abonelikleri a
        WHERE a.tenant_id = t.id AND a.durum = 'AKTIF');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abonelik_planlari");

            migrationBuilder.DropTable(
                name: "kiraci_abonelikleri");
        }
    }
}
