namespace Kuyumcu.Api.Data.GeriDoldurma;

/// <summary>
/// <c>hareketler.nakit_tutar</c> / <c>veresiye_tutar</c> alanları eklenmeden ÖNCE
/// yazılmış satırların ödeme payını, YALNIZ KESİN BİLİNEN hâllerde doldurur.
///
/// ⚠ İLKE (Anayasa md.1): tahmin edilen pay YAZILMAZ. Payı bilinmeyen satır
/// <c>NULL</c> kalır ve bu "bilinmiyor" demektir — 0 ile aynı DEĞİLDİR.
/// <c>TevsikKapisi.GunlukNakitToplam</c> NULL satırlar için eski kurala
/// (<c>odeme_turu = 'NAKIT'</c>) düşer; yani geri-doldurma hiçbir satırın
/// durumunu KÖTÜLEŞTİRMEZ, yalnız bilinenleri kesinleştirir.
///
/// Metin burada sabit tutulur ki migration ile TEST AYNI SQL'i koşsun;
/// aksi hâlde test, üretimde koşmayan bir metni doğrulamış olurdu.
/// </summary>
public static class NakitPayiGeriDoldurma
{
    /// <summary>
    /// 1) TL bacağı SIFIR olan hareketler: transfer, emanet giriş/çıkış, sayım
    /// düzeltme. Para hiç el değiştirmemiştir — bu bir çıkarım değil, kaydın
    /// kendi TL bacağının söylediği şeydir.
    /// </summary>
    public const string ParasizHareketler = """
        UPDATE hareketler
        SET nakit_tutar = 0, veresiye_tutar = 0
        WHERE nakit_tutar IS NULL AND tl_tutari = 0
        """;

    /// <summary>
    /// 2) Kasa defterinde TEK eşleşen satırı olan hareketler. Kasa hareketi
    /// ödeme aracını (<c>odeme_turu</c>) ve fiilen kasaya giren/çıkan tutarı
    /// taşır; bu bir kanıttır, tahmin değildir.
    ///
    /// <c>HAVING COUNT(*) = 1</c> şarttır: bir harekete birden çok kasa satırı
    /// bağlıysa (ileride çok bacaklı fiş) hangi payın nakit olduğu bu sorgudan
    /// çıkarılamaz — o satırlar dokunulmadan NULL bırakılır.
    /// </summary>
    public const string KasaDefterindenKanit = """
        UPDATE hareketler h
        JOIN (
            SELECT hareket_id,
                   MAX(odeme_turu) AS odeme_turu,
                   SUM(ABS(tutar)) AS tutar
            FROM kasa_hareketleri
            WHERE hareket_id IS NOT NULL
            GROUP BY hareket_id
            HAVING COUNT(*) = 1
        ) kh ON kh.hareket_id = h.id
        SET h.nakit_tutar = CASE WHEN kh.odeme_turu = 'NAKIT' THEN kh.tutar ELSE 0 END,
            -- Veresiye payı ancak kasa hareketi TL bacağının TAMAMINI karşılıyorsa
            -- kesindir (geriye borç kalmamıştır). Kısmi ödemede pay bilinmez.
            h.veresiye_tutar = CASE WHEN kh.tutar >= ABS(h.tl_tutari) - 0.005 THEN 0 ELSE NULL END
        WHERE h.nakit_tutar IS NULL AND h.tl_tutari <> 0
        """;

    /// <summary>Migration ve testin koştuğu sıra — kesinden daha kesine.</summary>
    public static readonly string[] Sirayla = [ParasizHareketler, KasaDefterindenKanit];
}
