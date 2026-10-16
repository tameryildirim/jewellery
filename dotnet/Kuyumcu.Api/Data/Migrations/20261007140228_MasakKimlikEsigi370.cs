using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <summary>
    /// PAKET MASAK-370 — MASAK kimlik tespiti eşiği 185.000 → 370.000 TL.
    /// <para>
    /// DAYANAK (birincil kaynak, okundu): Suç Gelirlerinin Aklanmasının ve Terörün Finansmanının
    /// Önlenmesine Dair Tedbirler Hakkında Yönetmelikte Değişiklik Yapılmasına Dair Yönetmelik —
    /// Cumhurbaşkanı Kararı sayı 11845 (karar 06.10.2026), Resmî Gazete 07.10.2026 sayı 33393.
    /// md.1: 5/1-b bendinde "yüzseksenbeşbin TL" → "üçyüzyetmişbin TL". md.6: "yayımı tarihinde
    /// yürürlüğe girer". https://www.resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf ve
    /// mevzuat.gov.tr 21.5.200713012 (dipnot 9). Rapor: dokumanlar/09-uygulama-kayitlari/
    /// MEVZUAT-KUYUMCU-BELGE-2026-10-07.md.
    /// </para>
    /// <para>
    /// GEÇMİŞ YENİDEN YAZILMAZ: 185.000 satırı (yasal1-tohum-07) silinmez, yalnız bitişi BİR KEZ
    /// doldurulur. Aralık anlamı [başlangıç, bitiş) — bitiş HARİÇ (YasalParametreSatiri.GecerliMi):
    /// bitiş = 2026-10-07 → 06.10.2026 dahil son gün 185.000; 07.10.2026 ilk gün 370.000.
    /// /yasal-sinirlar ekranı bugün başlayan değeri bilinçli olarak reddeder (YasalParametreServisi.Ekle);
    /// bu yüzden yol migration + denetim kaydıdır.
    /// </para>
    /// <para>
    /// AYNI SÜRÜMDE: appsettings.json Masak:KimlikEsigi ve MasakEsikleri.KimlikEsigi varsayılanı
    /// 370.000 — açılış eşitlik denetimi (Program.cs, YASAL_PARAM_AYRISMA) üçünü birlikte ister.
    /// 200.000 TL amaç beyanı kademesine (yasal1-tohum-08, TEYİTSİZ) DOKUNULMAZ — kurucu md.15 (a).
    /// </para>
    /// </summary>
    public partial class MasakKimlikEsigi370 : Migration
    {
        private const string Anahtar = "MASAK_KIMLIK_ESIGI_TL";
        private const string YeniId = "masak370-01";
        private const string Gecis = "2026-10-07";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // (1) 07.10.2026'da yürürlükte olan satırı (bugün: yasal1-tohum-07, 185.000, açık uçlu)
            //     geçiş gününde KAPAT. İleri tarihli bir satır varsa (sistem ucuyla eklenmiş) onun
            //     başlangıcına kadar olan aralık yeni satıra geçer — aşağıda.
            migrationBuilder.Sql($@"UPDATE yasal_parametreler
SET gecerlilik_bitis = '{Gecis}'
WHERE anahtar = '{Anahtar}'
  AND gecerlilik_baslangic < '{Gecis}'
  AND (gecerlilik_bitis IS NULL OR gecerlilik_bitis > '{Gecis}');");

            // (2) 370.000 satırı: başlangıç 07.10.2026, DOGRULANDI. Bitiş: varsa ilk ileri tarihli
            //     satırın başlangıcı, yoksa açık uçlu (NULL). Aynı gün başlayan satır zaten varsa
            //     UNIQUE (anahtar, gecerlilik_baslangic) migration'ı DURDURUR — sessiz atlama yok.
            migrationBuilder.Sql($@"INSERT INTO yasal_parametreler
(id, anahtar, deger_sayi, deger_metin, birim, gecerlilik_baslangic, gecerlilik_bitis, dayanak, ic_not, kaynak_url, teyit, aciklama_sade, olusturan, olusturuldu)
SELECT '{YeniId}', '{Anahtar}', 370000, NULL, 'TL', '{Gecis}', ileri.ilk,
  'Tedbirler Yönetmeliği md.5/1-b (11845 sayılı CK, RG 07.10.2026/33393)',
  'RG 07.10.2026/33393 · CK 11845 md.1 · Yönetmelik md.5/1-b; yürürlük CK md.6 (yayımı tarihinde); mevzuat.gov.tr 21.5.200713012 dipnot 9 ile karşılaştırıldı (paket MASAK-370)',
  'https://www.resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf',
  'DOGRULANDI',
  'Bu tutar ve üstündeki işlemde müşterinin kimliği alınır',
  'migration:MasakKimlikEsigi370',
  UTC_TIMESTAMP(6)
FROM (SELECT MIN(gecerlilik_baslangic) AS ilk FROM yasal_parametreler
      WHERE anahtar = '{Anahtar}' AND gecerlilik_baslangic > '{Gecis}') AS ileri;");

            // (3) Denetim izi — sistem kiracısı (YasalParametreServisi.Ekle ile aynı biçim).
            migrationBuilder.Sql($@"INSERT INTO denetim_kayitlari (id, tenant_id, kullanici_id, eylem, varlik_turu, varlik_id, hassas, detay, tarih, sube_id)
VALUES (UUID(), 'SISTEM', NULL, 'GUNCELLEME', 'YasalParametre', '{YeniId}', 1,
  'yasal parametre eklendi · {Anahtar} = 370000 · başlangıç {Gecis} · teyit DOGRULANDI · önceki satır {Gecis} itibarıyla kapandı · operatör: migration MasakKimlikEsigi370 · gerekçe: RG 07.10.2026/33393, CK 11845 md.1 (Yönetmelik md.5/1-b 185.000 → 370.000), yürürlük yayım tarihi',
  UTC_TIMESTAMP(6), NULL);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma: önce 370.000 satırı kaldırılır (açık uçlu satır indeksi aynı anahtarda İKİ
            // açık satıra izin vermez — sıra tersine çevrilirse ux_yasal_param_acik_uc çakışır; ölçüldü).
            // Sonra kapatılan satırın bitişi, varsa ilk ileri tarihli satırın başlangıcına, yoksa
            // NULL'a (açık uçlu) döner. Türetilmiş tablo MySQL 1093 kısıtını aşmak için.
            migrationBuilder.Sql($@"DELETE FROM yasal_parametreler WHERE id = '{YeniId}';");

            migrationBuilder.Sql($@"UPDATE yasal_parametreler p
JOIN (SELECT MIN(gecerlilik_baslangic) AS ilk FROM yasal_parametreler
      WHERE anahtar = '{Anahtar}' AND gecerlilik_baslangic > '{Gecis}') AS ileri
SET p.gecerlilik_bitis = ileri.ilk
WHERE p.anahtar = '{Anahtar}' AND p.gecerlilik_bitis = '{Gecis}';");

            migrationBuilder.Sql($@"INSERT INTO denetim_kayitlari (id, tenant_id, kullanici_id, eylem, varlik_turu, varlik_id, hassas, detay, tarih, sube_id)
VALUES (UUID(), 'SISTEM', NULL, 'GUNCELLEME', 'YasalParametre', '{YeniId}', 1,
  'yasal parametre GERİ ALINDI · {Anahtar} = 370000 satırı kaldırıldı, önceki satırın {Gecis} bitişi geri açıldı · operatör: migration MasakKimlikEsigi370 Down',
  UTC_TIMESTAMP(6), NULL);");
        }
    }
}
