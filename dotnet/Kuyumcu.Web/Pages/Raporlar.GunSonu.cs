using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET DEGISIM-GUNSONU — gün sonu FARK'ının ADI ve kartların okunuşu (YALNIZ GÖRÜNÜM).
//
// Hakem YÜKSEK: BASİT kart "Tahsilat 12.485,67 ₺" altında kırmızı "Fark var 27.389,71 ₺" +
// "Kasaya giren para ile kesilen faturalar tutmuyor" yazıyordu; PRO'da "TOPLAM TAHSİLAT 12.485 |
// SATIŞ FATURASI 0 | FARK 27.389" çıkarma gibi okunuyordu (12.485 − 0 ≠ 27.389).
// Sunucunun FARK tanımı (RaporModulu.PosFaturaMutabakati, DEĞİŞMEDİ):
//     FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar
// Fark TAHSİLATLA ilgili değildir; cümle ve kart düzeni bu tanımı söyler:
//   • fark tamamen faturasız satıştan geliyorsa → "Faturası kesilmemiş satış: X"
//   • fark eksiyse (fatura satıştan fazla)      → "Satıştan fazla kesilen fatura: X"
//   • öteki karışık durumlar                     → "Satış ile fatura farkı: X"
// PRO kartları "Satış toplamı − Satış faturası = <fark adı>" sırasıyla dizilir; tahsilat
// kartı farkın yanından ayrılır (sonda). Hesap SUNUCUDA, burada yalnız ad ve sıra.
// ═══════════════════════════════════════════════════════════════════════════
public partial class Raporlar
{
    private DegisimGunSonuMetinleri DG => DegisimGunSonuSozlugu.Al(Dil.Dil);

    /// <summary>Farkın türü — sunucunun döndürdüğü kümelerden (hesap yok, yalnız sınıflama).</summary>
    internal enum GunSonuFarkTuru { Yok, Faturasiz, FazlaFatura, Genel }

    internal static GunSonuFarkTuru FarkTuru(Mutabakat m)
    {
        if (m.Fark == 0m) return GunSonuFarkTuru.Yok;
        if (m.Fark < 0m) return GunSonuFarkTuru.FazlaFatura;
        var bagsiz = m.FaturaEslesme?.BagimsizSayisi ?? 0;
        return bagsiz == 0 && m.Satis.FaturasizSayisi > 0 && m.Satis.FaturasizTl == m.Fark
            ? GunSonuFarkTuru.Faturasiz
            : GunSonuFarkTuru.Genel;
    }

    /// <summary>Farkın adı (BASİT cümle ve PRO kart etiketi TEK kaynaktan).</summary>
    internal static string FarkEtiketi(Mutabakat m, DegisimGunSonuMetinleri d) => FarkTuru(m) switch
    {
        GunSonuFarkTuru.Faturasiz => d.FarkFaturasiz,
        GunSonuFarkTuru.FazlaFatura => d.FarkFazlaFatura,
        _ => d.FarkGenel,
    };

    /// <summary>
    /// DEGISIM-GUNSONU tur 2 (hakem DÜŞÜK "çift olumsuz"): farkın EKRANDAKİ tutarı. Ad zaten yönü söyler
    /// ("Satıştan fazla kesilen fatura") → eksi işaret tekrar basılmaz (Math.Abs). Diğer türlerde m.Fark aynen.
    /// Hesap sunucuda; burada yalnız gösterim.
    /// </summary>
    internal static decimal FarkGosterimi(Mutabakat m) =>
        FarkTuru(m) == GunSonuFarkTuru.FazlaFatura ? Math.Abs(m.Fark) : m.Fark;

    internal static string FarkAciklamasi(Mutabakat m, DegisimGunSonuMetinleri d) => FarkTuru(m) switch
    {
        GunSonuFarkTuru.Faturasiz => d.AciklamaFaturasiz,
        GunSonuFarkTuru.FazlaFatura => d.AciklamaFazlaFatura,
        _ => d.AciklamaGenel,
    };

    /// <summary>
    /// FARK'a giren fatura tutarı — sunucunun çıkardığı (bağlı + bağsız). Eşleşme bloğu yoksa
    /// (eski yanıt) satış faturası toplamı. Böylece PRO kartları gözle sağlanır:
    /// Satış toplamı − bu = Fark (şube görünümünde bağsız fatura sunucuda farka girmez).
    /// </summary>
    internal static decimal FarkaGirenFatura(Mutabakat m) =>
        m.FaturaEslesme is { } fe ? fe.BagliTl + fe.BagimsizTl : m.Fatura.SatisFaturasiTl;

    /// <summary>
    /// PRO tahsilat kartının alt metni: yalnız SIFIR OLMAYAN kalemler; her kalemin içi bölünmez
    /// (boşluklar bölünmez boşluk) → satır kalem sınırında kırılır, tutarın ortasında değil.
    /// Hepsi sıfırsa null (alt satır yok).
    /// </summary>
    internal static string? TahsilatAltMetni(MutabakatTahsilat t, string dil)
    {
        var t5 = Toplu5Sozlugu.Al(dil);
        var kalem = new List<string>(4);
        if (t.Nakit != 0m) kalem.Add(string.Format(t5.KirilimNakit, Bicim.TlKurus(t.Nakit)));
        if (t.Kart != 0m) kalem.Add(string.Format(t5.KirilimKart, Bicim.TlKurus(t.Kart)));
        if (t.Havale != 0m) kalem.Add(string.Format(t5.KirilimHavale, Bicim.TlKurus(t.Havale)));
        if (t.Diger != 0m) kalem.Add(string.Format(t5.KirilimDiger, Bicim.TlKurus(t.Diger)));
        return kalem.Count == 0 ? null : string.Join(" · ", kalem.Select(k => k.Replace(' ', '\u00A0')));
    }

    // Sunucu uyarılarının (RaporModulu.PosFaturaMutabakati — DEĞİŞMEDİ, başka şeridin dosyası) iki kalıbı:
    //   "{n} satış faturası hiçbir satış fişine bağlı değil (toplam X) — elle kesilmiş olabilir"
    //   "Faturalanmamış satış ile fişe bağlanmamış fatura arasında X fark var"
    private const string BagsizUyariKalibi = " satış faturası hiçbir satış fişine bağlı değil";
    private const string FarkUyariOneki = "Faturalanmamış satış ile fişe bağlanmamış fatura arasında";

    /// <summary>
    /// DEGISIM-2 (hakem ORTA, PRO C senaryosu "aynı tutar için iki sarı uyarı, ikincisi yanıltıcı; aynı tutar üç adla"):
    /// YALNIZ GÖSTERİM — tutar hesabına dokunulmaz, uyarı UYDURULMAZ, yalnız ikisi süzülür/adlandırılır:
    ///  (1) bağsız fatura uyarısı TEK adla ("Satışa bağlanmamış fatura") ve sunucunun YAPISAL alanlarından (adet, toplam) yazılır;
    ///  (2) fark TAMAMEN bağsız faturadan geliyorsa (faturasız satış 0, fark = −bağsız toplam) "Faturalanmamış satış ile …
    ///      fark var" uyarısı basılmaz — (1) aynı tutarı zaten söyler (TOPLU-5/KAR-RAPOR'un faturasız tarafındaki kuralın aynası).
    /// Diğer bütün uyarılar sunucudan AYNEN ve sırasıyla geçer.
    /// </summary>
    internal static List<string> GunSonuUyarilari(Mutabakat m, DegisimGunSonuMetinleri d)
    {
        var fe = m.FaturaEslesme;
        var farkYalnizBagsizdan = fe is { BagimsizSayisi: > 0 } && m.Satis.FaturasizSayisi == 0 && m.Fark == -fe.BagimsizTl;
        var sonuc = new List<string>(m.Uyarilar.Count);
        foreach (var u in m.Uyarilar)
        {
            if (fe is { BagimsizSayisi: > 0 } && u.StartsWith(fe.BagimsizSayisi + BagsizUyariKalibi, StringComparison.Ordinal))
                sonuc.Add(string.Format(d.BagsizUyari, fe.BagimsizSayisi, Bicim.TlKurus(fe.BagimsizTl)));
            else if (farkYalnizBagsizdan && u.StartsWith(FarkUyariOneki, StringComparison.Ordinal))
                continue;
            else
                sonuc.Add(u);
        }
        return sonuc;
    }

    private List<string> GunSonuUyarilari(Mutabakat m) => GunSonuUyarilari(m, DG);

    /// <summary>Özet tablosunda adet yazımı TEK kalıp: "N adet · tutar".</summary>
    private string AdetTutar(int adet, decimal tl) => string.Format(DG.AdetTutar, adet, Bicim.TlKurus(tl));
}
