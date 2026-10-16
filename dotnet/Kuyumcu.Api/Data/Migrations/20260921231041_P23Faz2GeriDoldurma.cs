using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-23 · FAZ 2 — GEÇMİŞ FİŞLERİN ÖDEME KIRILIMININ GERİ DOLDURULMASI
    //
    // ⚠ BU MIGRATION VERİ YAZAR. Plan § 11 BK-03 üç şart koşmuştu; üçü de
    //   PAKET-P-23-uygulama-raporu.md § "Geri doldurma" bölümünde kayıtlıdır:
    //   (i) salt okunur etki raporu, (ii) koşturulmuş Down() tatbikatı,
    //   (iii) öncesi/sonrası tam DB yedeği + geri dönüş provası.
    //
    // TÜRETME KURALI — TAHMİN YOK, YALNIZ YAZILI OLAN OKUNUR
    // ─────────────────────────────────────────────────────
    // Fiş = aynı fis_id'yi taşıyan hareket kümesi; fis_id yoksa hareketin
    // KENDİSİ tek satırlık bir fiştir (hareketler.fis_id çoğu eski satırda
    // NULL'dur — ölçüldü, varsayılmadı).
    // Bir fişin bacakları ÜÇ yazılı kaynaktan doğar:
    //   1) kasa_hareketleri — kasaya giren her pay (NAKIT/KART/HAVALE...).
    //      Araç, tutar, kasa ve kasa hareketi bağı BURADA YAZILIDIR.
    //   2) hareketler.veresiye_tutar — kasaya girmeyen pay. VERESIYE bacağının
    //      kasa satırı YOKTUR; kırılım kasa defterinden tamamlanamaz, bu alan
    //      olmadan pay kaybolur.
    //   3) ARTIK = fiş toplamı − (1) − (2). Artık > 0 ise:
    //      • fişin hareketleri TEK bir bilinen odeme_turu taşıyorsa (KARMA
    //        değil ve kapalı listede) artığın aracı O'DUR — yazılıdır, tahmin
    //        değildir;
    //      • aksi hâlde araç 'BILINMIYOR' damgalanır ve bilinmiyor = 1 olur.
    //
    // ⚠ ESKİ ALANLARA DOKUNULMAZ: odeme_turu · nakit_tutar · veresiye_tutar
    //   olduğu gibi kalır. TevsikKapisi (459 s. VUK) ve MASAK kümülatifi
    //   kaynağını DEĞİŞTİRMEZ; bu tablo onların yanında durur (gerileme yok).
    // ⚠ KALEMİ OLAN FİŞE DOKUNULMAZ: canlı yazılmış kalem varsa o fiş atlanır.
    // ⚠ storno_hareket_id türetilmez (geçmişte izi yok) → NULL, aktif = 1.
    //
    // DOWN(): yalnız geri_dolduruldu = 1 satırları siler, sonra kolonu düşürür.
    // Canlı yazılmış tek bir kaleme, eski alanlara ve odeme_kalemleri tablosunun
    // kendisine DOKUNMAZ (tabloyu düşüren, onu doğuran P23OdemeKalemleri'dir).
    // ═════════════════════════════════════════════════════════════════════════
    /// <inheritdoc />
    public partial class P23Faz2GeriDoldurma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠ KOLON EKLEME NEDEN KORUMALI YAZILDI (ölçülmüş olay, tahmin değil)
            // İlk koşumda AddColumn başarılı oldu, ardından gelen veri deyimi
            // only_full_group_by yüzünden HATA verdi. MySQL'de DDL işlem
            // dışıdır: kolon veritabanında KALDI, __EFMigrationsHistory satırı
            // ise YAZILMADI. Bu yarım durumda düz `AddColumn` bir daha asla
            // koşamaz ("Duplicate column name") ve migration kilitlenir.
            // Bu yüzden ekleme, kolonun VARLIĞINA bakan korumalı bir deyimdir:
            // migration yarım kalmış bir veritabanında da baştan koşabilir.
            migrationBuilder.Sql("""
                SET @p23_ddl := (SELECT IF(
                    (SELECT COUNT(*) FROM information_schema.columns
                      WHERE table_schema = DATABASE()
                        AND table_name   = 'odeme_kalemleri'
                        AND column_name  = 'geri_dolduruldu') > 0,
                    'DO 0',
                    'ALTER TABLE odeme_kalemleri ADD COLUMN geri_dolduruldu tinyint(1) NOT NULL DEFAULT 0'));
                """);
            migrationBuilder.Sql("PREPARE p23_stmt FROM @p23_ddl;");
            migrationBuilder.Sql("EXECUTE p23_stmt;");
            migrationBuilder.Sql("DEALLOCATE PREPARE p23_stmt;");

            // Yarım koşumdan artakalan satır olursa üstüne YAZILMAZ, silinir:
            // aynı fişe iki kez kalem yazmak toplamı ikiye katlardı.
            migrationBuilder.Sql("DELETE FROM odeme_kalemleri WHERE geri_dolduruldu = 1;");

            migrationBuilder.Sql(GeriDoldurmaSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Önce veri, sonra kolon: kolon düşerse satırları ayırt edemeyiz.
            migrationBuilder.Sql("DELETE FROM odeme_kalemleri WHERE geri_dolduruldu = 1;");

            migrationBuilder.DropColumn(
                name: "geri_dolduruldu",
                table: "odeme_kalemleri");
        }

        /// <summary>
        /// Tek deyim: üç kaynağın birleşimi, fiş içinde sıralanıp yazılır.
        /// Kimlik öneki 'GD' — satırın nereden geldiği kimlikten de okunur.
        /// </summary>
        private const string GeriDoldurmaSql = """
            INSERT INTO odeme_kalemleri
                (id, tenant_id, fis_id, hareket_id, sira, arac, tutar_tl, para_birimi,
                 doviz_tutar, kasa_id, kasa_hareket_id, pos_tahsilat_id, taksit_adedi,
                 evrak_id, kur_snapshot_id, bilinmiyor, geri_dolduruldu, aktif,
                 storno_hareket_id, olusturuldu)
            SELECT
                CONCAT('GD', REPLACE(UUID(), '-', '')),
                b.tenant_id, b.fis_key, b.hareket_id,
                ROW_NUMBER() OVER (PARTITION BY b.tenant_id, b.fis_key
                                   ORDER BY b.oncelik, b.hareket_id) - 1,
                b.arac, ROUND(b.tutar_tl, 2), b.para_birimi,
                NULL, b.kasa_id, b.kasa_hareket_id, NULL, NULL,
                NULL, b.kur_snapshot_id, b.bilinmiyor, 1, 1,
                NULL, b.olusturuldu
            FROM (
                /* 1) KASAYA GİREN PAYLAR — araç ve tutar kasa defterinde YAZILI */
                SELECT h.tenant_id,
                       COALESCE(h.fis_id, h.id) AS fis_key,
                       h.id                     AS hareket_id,
                       0                        AS oncelik,
                       kh.odeme_turu            AS arac,
                       ABS(kh.tutar)            AS tutar_tl,
                       COALESCE(kh.para_birimi, 'TRY') AS para_birimi,
                       kh.kasa_id               AS kasa_id,
                       kh.id                    AS kasa_hareket_id,
                       h.kur_snapshot_id        AS kur_snapshot_id,
                       0                        AS bilinmiyor,
                       h.olusturuldu            AS olusturuldu
                FROM hareketler h
                JOIN kasa_hareketleri kh ON kh.hareket_id = h.id
                WHERE h.odeme_turu IS NOT NULL
                  AND kh.odeme_turu IN ('NAKIT','KART','HAVALE','VERESIYE','DOVIZ')
                  AND ABS(kh.tutar) > 0

                UNION ALL

                /* 2) VERESİYE PAYI — kasa satırı YOKTUR, alan tek kaynaktır */
                SELECT h.tenant_id, COALESCE(h.fis_id, h.id), h.id, 1,
                       'VERESIYE', h.veresiye_tutar, 'TRY',
                       NULL, NULL, h.kur_snapshot_id, 0, h.olusturuldu
                FROM hareketler h
                WHERE h.odeme_turu IS NOT NULL
                  AND COALESCE(h.veresiye_tutar, 0) > 0

                UNION ALL

                /* 3) ARTIK — fiş toplamının kapanmayan payı */
                SELECT t.tenant_id, t.fis_key, t.hareket_id, 2,
                       CASE WHEN t.tek_tur IN ('NAKIT','KART','HAVALE','VERESIYE','DOVIZ')
                            THEN t.tek_tur ELSE 'BILINMIYOR' END,
                       t.artik, 'TRY',
                       NULL, NULL, t.kur_snapshot_id,
                       CASE WHEN t.tek_tur IN ('NAKIT','KART','HAVALE','VERESIYE','DOVIZ')
                            THEN 0 ELSE 1 END,
                       t.olusturuldu
                FROM (
                    SELECT g.tenant_id, g.fis_key, g.hareket_id, g.kur_snapshot_id,
                           g.olusturuldu, g.tek_tur,
                           ROUND(g.toplam - g.veresiye - g.kasa, 2) AS artik
                    FROM (
                        SELECT h.tenant_id,
                               COALESCE(h.fis_id, h.id) AS fis_key,
                               MIN(h.id)                AS hareket_id,
                               MIN(h.kur_snapshot_id)   AS kur_snapshot_id,
                               MIN(h.olusturuldu)       AS olusturuldu,
                               /* fişin TEK bilinen ödeme türü varsa o, yoksa NULL */
                               CASE WHEN COUNT(DISTINCT h.odeme_turu) = 1
                                    THEN MIN(h.odeme_turu) ELSE NULL END AS tek_tur,
                               SUM(ABS(h.tl_tutari))    AS toplam,
                               SUM(COALESCE(h.veresiye_tutar, 0)) AS veresiye,
                               COALESCE(kt.kasa, 0) AS kasa
                        FROM hareketler h
                        /* Kasa toplamı AYRI türetilmiş tabloda: ilişkili alt
                           sorgu only_full_group_by ile bağdaşmıyor (ölçüldü). */
                        LEFT JOIN (
                            SELECT h2.tenant_id AS tid,
                                   COALESCE(h2.fis_id, h2.id) AS fk,
                                   SUM(ABS(kh2.tutar)) AS kasa
                            FROM hareketler h2
                            JOIN kasa_hareketleri kh2 ON kh2.hareket_id = h2.id
                            WHERE h2.odeme_turu IS NOT NULL
                              AND kh2.odeme_turu IN ('NAKIT','KART','HAVALE','VERESIYE','DOVIZ')
                              AND ABS(kh2.tutar) > 0
                            GROUP BY h2.tenant_id, COALESCE(h2.fis_id, h2.id)
                        ) kt ON kt.tid = h.tenant_id
                            AND kt.fk = COALESCE(h.fis_id, h.id)
                        WHERE h.odeme_turu IS NOT NULL
                        GROUP BY h.tenant_id, COALESCE(h.fis_id, h.id), kt.kasa
                    ) g
                ) t
                WHERE t.artik > 0
            ) b
            /* Kalemi ZATEN olan fişe DOKUNULMAZ — canlı kayıt üstüne yazılmaz. */
            WHERE NOT EXISTS (
                SELECT 1 FROM odeme_kalemleri ok
                WHERE ok.tenant_id = b.tenant_id AND ok.fis_id = b.fis_key
            );
            """;
    }
}
