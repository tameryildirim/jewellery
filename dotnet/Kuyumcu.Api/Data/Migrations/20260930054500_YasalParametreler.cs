using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class YasalParametreler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "yasal_parametreler",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    anahtar = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    deger_sayi = table.Column<decimal>(type: "decimal(20,6)", nullable: true),
                    deger_metin = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    birim = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gecerlilik_baslangic = table.Column<DateOnly>(type: "date", nullable: false),
                    gecerlilik_bitis = table.Column<DateOnly>(type: "date", nullable: true),
                    dayanak = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    teyit = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aciklama_sade = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturan = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_yasal_parametreler", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_yasal_parametreler_anahtar_gecerlilik_baslangic",
                table: "yasal_parametreler",
                columns: new[] { "anahtar", "gecerlilik_baslangic" },
                unique: true);

            // ── PAKET YASAL-1: aynı anahtarda İKİ AÇIK UÇLU satır olamaz (K-6).
            //    MySQL 8 işlevsel anahtar parçası: açık uçlu satırda 1, kapalıda NULL —
            //    UNIQUE indeks NULL'ları çakıştırmaz. EF modeli bunu ifade edemez.
            //    RESMI_TATIL satırları her zaman kapalı aralıktır (gün, gün+1).
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_yasal_param_acik_uc ON yasal_parametreler "
                + "(anahtar, ((CASE WHEN gecerlilik_bitis IS NULL THEN 1 ELSE NULL END)));");

            // ── TOHUM (şartname § 3.1 + § 3.2). YALNIZ birincil kaynağı okunmuş
            //    değerler DOGRULANDI; diğerleri TEYITSIZ/KAPALI damgasıyla ve BUGÜNKÜ
            //    davranışı birebir koruyan değerle. Kaynağı olmayan geçmiş aralık
            //    YOKTUR (o güne sorgu 503 verir, uydurulmaz).
            migrationBuilder.Sql(@"INSERT INTO yasal_parametreler (id, anahtar, deger_sayi, deger_metin, birim, gecerlilik_baslangic, gecerlilik_bitis, dayanak, kaynak_url, teyit, aciklama_sade, olusturan, olusturuldu) VALUES
('yasal1-tohum-01', 'TEVSIK_HADDI_TL', 7000, NULL, 'TL', '2016-01-01', '2024-11-30', '459 s. VUK Genel Tebliği, RG 24.12.2015/29572 (yürürlük 01.01.2016)', 'https://www.resmigazete.gov.tr/eskiler/2015/12/20151224-6.htm', 'DOGRULANDI', 'Nakitte bir kişiyle aynı gün yapılabilecek en yüksek tutar; üstü banka ile ödenir', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-02', 'TEVSIK_HADDI_TL', 30000, NULL, 'TL', '2024-11-30', NULL, '575 s. VUK Genel Tebliği, RG 30.11.2024/32738 (yayımı tarihinde yürürlük)', 'https://www.resmigazete.gov.tr/eskiler/2024/11/20241130-10.htm', 'DOGRULANDI', 'Nakitte bir kişiyle aynı gün yapılabilecek en yüksek tutar; üstü banka ile ödenir', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-03', 'TEVSIK_BILDIRIM_IS_GUNU', 5, NULL, 'IS_GUNU', '2024-08-02', NULL, 'VUK mükerrer md.355 (7524 s. K. md.13, RG 02.08.2024/32620)', 'https://www.mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf', 'DOGRULANDI', 'Sınırı aşan nakit ödemeyi kendiliğinden bildirme süresi', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-04', 'MASAK_KIMLIK_ESIGI_TL', 20000, NULL, 'TL', '2008-04-01', '2021-02-24', 'Tedbirler Yönetmeliği md.5/1-b ilk metin (yürürlük 01.04.2008); bitiş 3580 s. CK, RG 24.02.2021/31405 — CK yürürlük maddesi ayrıca okunmadı', 'https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf', 'TEYITSIZ', 'Bu tutar ve üstündeki işlemde müşterinin kimliği alınır', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-05', 'MASAK_KIMLIK_ESIGI_TL', 75000, NULL, 'TL', '2021-02-24', '2022-06-16', 'Tedbirler Yönetmeliği md.5/1-b (3580 s. CK, RG 24.02.2021/31405) — CK yürürlük maddesi ayrıca okunmadı', 'https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf', 'TEYITSIZ', 'Bu tutar ve üstündeki işlemde müşterinin kimliği alınır', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-06', 'MASAK_KIMLIK_ESIGI_TL', 85000, NULL, 'TL', '2022-06-16', '2023-01-14', 'Tedbirler Yönetmeliği md.5/1-b (5739 s. CK, RG 16.06.2022/31868) — CK yürürlük maddesi ayrıca okunmadı', 'https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf', 'TEYITSIZ', 'Bu tutar ve üstündeki işlemde müşterinin kimliği alınır', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-07', 'MASAK_KIMLIK_ESIGI_TL', 185000, NULL, 'TL', '2023-01-14', NULL, 'Tedbirler Yönetmeliği md.5/1-b (6702 s. CK, RG 14.01.2023/32073)', 'https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf', 'DOGRULANDI', 'Bu tutar ve üstündeki işlemde müşterinin kimliği alınır', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-08', 'MASAK_AMAC_BEYANI_ESIGI_TL', 200000, NULL, 'TL', '2008-04-01', NULL, 'Dayanağı bulunamadı (şartname § 3.2) — bugünkü davranışı korumak için tutulur; başlangıç = yönetmelik yürürlüğü', 'https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf', 'TEYITSIZ', 'Bu tutar ve üstündeki işlemde işlemin amacı yazılır', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-09', 'MASAK_BEYAN_FORMU_ESIGI_TL', 2000000.01, NULL, 'TL', '2008-04-01', NULL, 'Nakit İşlem Beyan Formu tebliğinin RG metni doğrulanamadı (şartname § 3.2, M-06) — bugünkü davranış korunur', 'https://ms.hmb.gov.tr/uploads/sites/12/2025/08/SAK-Genel-Teblig-SIRA-NO-31-6203d6d5cd7cf747.pdf', 'TEYITSIZ', 'Bu tutarın üstündeki nakit işlemde beyan formu doldurulur', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-10', 'MASAK_EK_BELGE_ESIGI_TL', 20000000.01, NULL, 'TL', '2008-04-01', NULL, 'Nakit İşlem Beyan Formu tebliğinin RG metni doğrulanamadı (şartname § 3.2, M-06) — bugünkü davranış korunur', 'https://ms.hmb.gov.tr/uploads/sites/12/2025/08/SAK-Genel-Teblig-SIRA-NO-31-6203d6d5cd7cf747.pdf', 'TEYITSIZ', 'Bu tutarın üstündeki nakit işlemde ek belge istenir', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-11', 'MASAK_SIB_IS_GUNU', 10, NULL, 'IS_GUNU', '2008-04-01', NULL, 'Tedbirler Yönetmeliği md.28/2 (ilk metin, değişiklik dipnotu yok; yürürlük md.51: 01.04.2008)', 'https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf', 'DOGRULANDI', 'Şüpheli işlemi bildirme süresi', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-12', 'MASAK_SAKLAMA_YIL', 8, NULL, 'YIL', '2008-04-01', NULL, 'Tedbirler Yönetmeliği md.46/1 (ilk metin, değişiklik dipnotu yok; yürürlük md.51: 01.04.2008)', 'https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf', 'DOGRULANDI', 'Kimlik ve işlem belgelerini saklama süresi (son işlemden itibaren)', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-13', 'GIDER_PUSULASI_GUN', 7, NULL, 'GUN', '2021-11-01', NULL, 'VUK md.234 (7338 s. K. md.23, RG 26.10.2021/31640; yürürlük: yayımı takip eden ay başı)', 'https://www.resmigazete.gov.tr/eskiler/2021/10/20211026-1.htm', 'DOGRULANDI', 'Eski altın ve hurda alışında gider pusulasını yazma süresi', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-14', 'STOPAJ_ESNAF_MUAF_HURDA_ORAN', 0.02, NULL, 'ORAN', '2009-02-03', NULL, 'GVK md.94/13 + 2009/14592 BKK (RG 03.02.2009/27130) — sonraki değişiklik doğrulanamadı', 'https://www.resmigazete.gov.tr/eskiler/2009/02/20090203-3.htm', 'TEYITSIZ', 'Esnaf muafiyetli satıcıdan hurda alımında kesilen vergi oranı', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-15', 'STOPAJ_NIHAI_TUKETICI_ORAN', 0, NULL, 'ORAN', '2009-02-03', NULL, 'GVK md.94 — mükellef olmayan kişiden alıma özgü bent bulunamadı (şartname § 3.2)', 'https://www.resmigazete.gov.tr/eskiler/2009/02/20090203-3.htm', 'TEYITSIZ', 'Vergi mükellefi olmayan kişiden alımda kesilen vergi oranı', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-16', 'STOPAJ_BEYAN_GUNU', 26, NULL, 'GUN', '2026-09-30', NULL, 'GİB beyan ve ödeme süreleri belgesi — dayanak tebliğ doğrulanamadı (şartname § 3.2)', 'https://cdn.gib.gov.tr/api/gibportal-file/file/getFileResources?objectKey=arsiv%2Fyardim-kaynaklar%2Fyararli-bilgiler%2FBeyannameVermeveOdemeSureleri.pdf', 'TEYITSIZ', 'Kesilen vergiyi bildirme günü (izleyen ayın bu günü)', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-17', 'KDV_GENEL_ORAN', 0.20, NULL, 'ORAN', '2026-09-30', NULL, 'KDV genel oranı — bu pakette birincil kaynak okunmadı; bugünkü yapılandırma değeri (Kdv:VarsayilanOran)', 'https://www.mevzuat.gov.tr/mevzuatmetin/1.5.3065.pdf', 'TEYITSIZ', 'Genel KDV oranı', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-18', 'KDV_OZEL_MATRAH_KODU', NULL, '805', 'KOD', '2026-09-30', NULL, 'GİB UBL-TR kod listesi (özel matrah) — kural K-10 teyidine kadar KAPALI', 'https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/UBLTR_1.2.1_Kilavuzlar.zip', 'KAPALI', 'Altın satışında özel matrah kodu (şimdilik kullanılmıyor)', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-19', 'FATURA_HADDI_TL', 9900, NULL, 'TL', '2025-01-01', '2026-01-01', 'VUK md.232; 577 s. VUK GT (RG 30.12.2024/32768 2. Mük.) — kural bu pakette kodlanmadı', 'https://www.resmigazete.gov.tr/eskiler/2024/12/20241230M2-14-1.pdf', 'DOGRULANDI', 'Bu tutarı aşan satışta fatura zorunludur', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-20', 'FATURA_HADDI_TL', 12000, NULL, 'TL', '2026-01-01', NULL, 'VUK md.232; 588 s. VUK GT (RG 31.12.2025/33124 5. Mük.) — kural bu pakette kodlanmadı', 'https://www.resmigazete.gov.tr/eskiler/2025/12/20251231M5-31.pdf', 'DOGRULANDI', 'Bu tutarı aşan satışta fatura zorunludur', 'migration:YasalParametreler', '2026-09-30 00:00:00'),
('yasal1-tohum-21', 'KMTS_KOD_DESENI', NULL, NULL, 'KOD', '2026-09-30', NULL, 'KMTS resmî kod biçimi yayımlanmadı — KAPALI yer tutucu (YASAL-4 § 4.4)', 'https://www.mevzuat.gov.tr/', 'KAPALI', 'KMTS ürün kodu biçimi (şimdilik kullanılmıyor)', 'migration:YasalParametreler', '2026-09-30 00:00:00');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "yasal_parametreler");
        }
    }
}
