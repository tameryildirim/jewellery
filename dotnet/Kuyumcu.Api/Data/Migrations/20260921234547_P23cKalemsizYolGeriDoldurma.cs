using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-23c — FAZ 2 GERİ DOLDURMASININ KAPSAM AÇIĞININ KAPATILMASI
    //
    // ÖLÇÜLEN AÇIK (hasım doğrulayıcı bulgusu · kuyumcu_net, 2026-09-22):
    //   ödeme tutarı YAZILI olan 13 hareket kalem ALMAMIŞTI.
    //   Bırakılma gerekçesi "araç hiçbir yerde yazılı değil" diye kayda
    //   geçmişti; ÖLÇÜLDÜ ve BU GEREKÇE YANLIŞTIR:
    //     • 9 satırda (SATIS ×5, HURDA_ALIS ×2, ISCILIK ×1, TAKAS ×1) araç
    //       kasa_hareketleri.odeme_turu = 'NAKIT' olarak YAZILIDIR;
    //     • 4 satırda (EVRAK_GIRIS) pay hareketler.veresiye_tutar'da YAZILIDIR
    //       (evrakla kapanan cari borç — kasa satırı yoktur).
    //   Gerçek sebep Faz 2 deyiminin `h.odeme_turu IS NOT NULL` süzgeciydi:
    //   bu 13 satırda hareketler.odeme_turu NULL'dur, dolayısıyla üç kaynağın
    //   HİÇBİRİ okunmadan satırlar elendi.
    //
    // BU MIGRATION O SÜZGECİ KALDIRIR, kaynakları AYNEN kullanır:
    //   1) kasa_hareketleri — araç, tutar, kasa ve kasa hareketi bağı YAZILI;
    //   2) hareketler.veresiye_tutar — kasa satırı olmayan pay.
    // Tahmin YOKTUR: yazılı olmayan hiçbir değer üretilmez. Artık (fiş toplamı
    // − 1 − 2) bu veri kümesinde 0'dır (ölçüldü) ve kalırsa denetim.sh'ın
    // CANLI FIS_TOPLAM_ODEME_KALEMI_FARKI sayacı KIRMIZI verir — sessiz geçmez.
    //
    // ⚠ KALEMİ OLAN HAREKETE DOKUNULMAZ (NOT EXISTS): canlı yazılmış kalemin
    //   üstüne yazmak fişin toplamını ikiye katlardı.
    // ⚠ STORNO SATIRLARI DIŞARIDA: ters kayıt kalem doğurmaz (StornoServisi
    //   kaynağın kalemlerini pasifleştirir); yazılsaydı aktif kalem toplamı
    //   fiş toplamını aşardı.
    // ⚠ ESKİ ALANLARA DOKUNULMAZ: odeme_turu · nakit_tutar · veresiye_tutar
    //   olduğu gibi kalır; TevsikKapisi (459 s. VUK) ve MASAK kümülatifi
    //   kaynağını DEĞİŞTİRMEZ (gerileme testi: MASAK ve tevsik testleri).
    //
    // DOWN(): yalnız bu koşumun yazdığı satırları siler — kimlik öneki 'G3'
    // ile ayırt edilir, geri_dolduruldu = 1 olan Faz 2 satırlarına DOKUNMAZ.
    // ═════════════════════════════════════════════════════════════════════════
    /// <inheritdoc />
    public partial class P23cKalemsizYolGeriDoldurma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Yarım koşumdan artakalan satır olursa üstüne YAZILMAZ, silinir.
            migrationBuilder.Sql("DELETE FROM odeme_kalemleri WHERE id LIKE 'G3%';");
            migrationBuilder.Sql(GeriDoldurmaSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM odeme_kalemleri WHERE id LIKE 'G3%';");
        }

        private const string GeriDoldurmaSql = """
            INSERT INTO odeme_kalemleri
                (id, tenant_id, fis_id, hareket_id, sira, arac, tutar_tl, para_birimi,
                 doviz_tutar, kasa_id, kasa_hareket_id, pos_tahsilat_id, taksit_adedi,
                 evrak_id, kur_snapshot_id, bilinmiyor, geri_dolduruldu, aktif,
                 storno_hareket_id, olusturuldu)
            SELECT
                CONCAT('G3', REPLACE(UUID(), '-', '')),
                b.tenant_id, b.fis_key, b.hareket_id,
                ROW_NUMBER() OVER (PARTITION BY b.tenant_id, b.fis_key
                                   ORDER BY b.oncelik, b.hareket_id) - 1,
                b.arac, ROUND(b.tutar_tl, 2), b.para_birimi,
                NULL, b.kasa_id, b.kasa_hareket_id, NULL, NULL,
                b.evrak_id, b.kur_snapshot_id, 0, 1, 1,
                NULL, b.olusturuldu
            FROM (
                /* 1) KASAYA GİREN PAY — araç ve tutar kasa defterinde YAZILI */
                SELECT h.tenant_id,
                       COALESCE(h.fis_id, h.id) AS fis_key,
                       h.id                     AS hareket_id,
                       0                        AS oncelik,
                       kh.odeme_turu            AS arac,
                       ABS(kh.tutar)            AS tutar_tl,
                       COALESCE(kh.para_birimi, 'TRY') AS para_birimi,
                       kh.kasa_id               AS kasa_id,
                       kh.id                    AS kasa_hareket_id,
                       NULL                     AS evrak_id,
                       h.kur_snapshot_id        AS kur_snapshot_id,
                       h.olusturuldu            AS olusturuldu
                FROM hareketler h
                JOIN kasa_hareketleri kh ON kh.hareket_id = h.id
                WHERE kh.odeme_turu IN ('NAKIT','KART','HAVALE','VERESIYE','DOVIZ')
                  AND ABS(kh.tutar) > 0

                UNION ALL

                /* 2) VERESİYE PAYI — kasa satırı YOKTUR, alan tek kaynaktır.
                      EVRAK_GIRIS satırında payı doğuran kıymetli evrak da
                      bağlanır (odeme_kalemleri.evrak_id ilk kez BURADA dolar;
                      bağ evraklar.giris_hareket_id'de YAZILIDIR, türetilmedi). */
                SELECT h.tenant_id, COALESCE(h.fis_id, h.id), h.id, 1,
                       'VERESIYE', h.veresiye_tutar, 'TRY',
                       NULL, NULL,
                       (SELECT e.id FROM kiymetli_evraklar e
                         WHERE e.giris_hareket_id = h.id LIMIT 1),
                       h.kur_snapshot_id, h.olusturuldu
                FROM hareketler h
                WHERE COALESCE(h.veresiye_tutar, 0) > 0
            ) b
            /* Kalemi ZATEN olan FİŞE dokunulmaz — hareket bazında bakmak
               yetmezdi: çok kalemli satış fişinde kalemler YALNIZ ilk hareketin
               kimliğine bağlıdır, aynı fişin diğer satırları "kalemsiz" görünür
               ve fiş toplamı İKİYE KATLANIRDI (ölçülmüş tuzak).
               Storno ve stornolanmış satırlar kapsam DIŞIDIR. */
            WHERE NOT EXISTS (
                    SELECT 1 FROM odeme_kalemleri ok
                     WHERE ok.tenant_id = b.tenant_id AND ok.fis_id = b.fis_key)
              AND NOT EXISTS (
                    SELECT 1 FROM hareketler hs
                     WHERE hs.id = b.hareket_id AND hs.storno_kaynak_id IS NOT NULL)
              AND NOT EXISTS (
                    SELECT 1 FROM hareketler hx WHERE hx.storno_kaynak_id = b.hareket_id);
            """;
    }
}
