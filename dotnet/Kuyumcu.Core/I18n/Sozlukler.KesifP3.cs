using System.Text.RegularExpressions;

namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P3 · BELGE — fatura kalemi, gider pusulası, denetim izi, kasa
// raporu metinleri (#11 #23 #54 #55 #57 #59 #64 #66 #78 + TOPLU ZO-BELGE).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır ve yalnız P8'indir (orkestratör
//   kuralı: yeni metin AYRI dosyaya).
// ⚠ Ayar ADI buraya YAZILMAZ: tek kaynak Kuyumcu.Core.Domain.Ayarlar (ekranda
//   KurKaynakAdi.AyarAdi). Hareket türü adı tek kaynak KatalogSozlugu.HareketTuru.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ⚠ Denetim kaydı (sunucu metni) DEĞİŞMEZ — çeviri yalnız GÖSTERİMDE; ham kayıt
//   hücrenin title ipucunda kalır (ekip kararı 2, 30.09: görünür metinde ham kod 0).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>KESIF-P3 metinleri (bir dilde).</summary>
public sealed record KesifP3Metinleri(
    // #57: alt çubuktaki kilit sebebi — açıklamanın tamamı önizleme bölümünde bir kez yazılır.
    string KuyumKaydetKilitKisa,
    // TOPLU 08:50: defter bağlı fatura kaleminde adet salt okunur.
    string AdetFistenGelir,
    string AdetFistenDegistir,
    // TOPLU ZO-BELGE: kasa raporu geçmiş günlerde "yalnız bugünün kasası sayılabilir" bir kez.
    string YalnizBugunSayilir,
    string SayimSutunuIpucu,
    // Denetim izi belge türü adları (Çıktı belgesi okuma kaydı "SATIS_FISI belgesi görüntülendi").
    string BelgeSatisFisi,
    string BelgeTahsilatMakbuzu,
    string BelgeEmanetTeslim,
    string BelgeCariEkstre,
    // Tasarım hakemi tur 2 (01.10): denetim izi süzgecinde değişmezlik TEK kez — BASİT'te kısa
    // satır + (i) ipucu (gerekçe ipucunda), yasal madde numarası yok.
    string DenetimDegismezKisa,
    // Gider pusulası detayı: kilitli alan gerekçeleri TEK kapalı ipucunda; BASİT sade gerekçe
    // (madde numarası yok — tam dayanak PRO'da).
    string PusulaDegismezOzet,
    string PusulaDegismezSade,
    string PusulaDuzenlenebilirAciklama,
    string PusulaIptalEtSade);

public static class KesifP3Sozlugu
{
    public static readonly KesifP3Metinleri Tr = new(
        KuyumKaydetKilitKisa: "Kaydetmek için has bedelinin alınacağı işlemi yukarıdan seçin.",
        AdetFistenGelir: "Adet satış fişinden gelir; burada değiştirilemez.",
        AdetFistenDegistir: "Fişten değiştir",
        YalnizBugunSayilir: "Yalnız bugünün kasası sayılabilir; geçmiş günlerde sayım hücresi boş kalır.",
        SayimSutunuIpucu: "Sayım kuralı",
        BelgeSatisFisi: "satış fişi",
        BelgeTahsilatMakbuzu: "tahsilat makbuzu",
        BelgeEmanetTeslim: "emanet teslim belgesi",
        // Karar 1 (30.09): çıktı belgesinin resmî adı korunur.
        BelgeCariEkstre: "cari hesap ekstresi",
        DenetimDegismezKisa: "Kayıtlar değiştirilemez",
        PusulaDegismezOzet: "Hangi bilgiler değiştirilemez?",
        PusulaDegismezSade: "Satıcı, tutar ve tarih bilgisi pusula basıldıktan sonra değiştirilemez. Yanlışsa pusulayı iptal edip yenisini düzenleyin.",
        PusulaDuzenlenebilirAciklama: "Yalnız satıcı adresi ve mal açıklaması düzeltilebilir.",
        PusulaIptalEtSade: "Pusulayı iptal et");

    public static readonly KesifP3Metinleri En = new(
        KuyumKaydetKilitKisa: "To save, select above the transaction the fine-gold value is taken from.",
        AdetFistenGelir: "Quantity comes from the sales slip; it cannot be changed here.",
        AdetFistenDegistir: "Change on the slip",
        YalnizBugunSayilir: "Only today's till can be counted; the count cell stays empty for past days.",
        SayimSutunuIpucu: "Counting rule",
        BelgeSatisFisi: "sales slip",
        BelgeTahsilatMakbuzu: "collection receipt",
        BelgeEmanetTeslim: "custody delivery note",
        BelgeCariEkstre: "account statement",
        DenetimDegismezKisa: "Records cannot be changed",
        PusulaDegismezOzet: "Which details cannot be changed?",
        PusulaDegismezSade: "Seller, amount and date cannot be changed once the voucher is printed. If they are wrong, void the voucher and issue a new one.",
        PusulaDuzenlenebilirAciklama: "Only the seller address and goods description can be corrected.",
        PusulaIptalEtSade: "Void the voucher");

    public static KesifP3Metinleri Al(string dil) => dil == "en" ? En : Tr;

    // ── Denetim izi gösterimi (#54 #55) ─────────────────────────────────────

    // Hub (Sozlukler.cs, P8) ve ZO-BELGE sözlüğünde adı OLMAYAN kayıt türleri (canlı
    // denetim_kayitlari.varlik_turu taraması, 30.09): ham PascalCase ekrana düşüyordu.
    private static readonly Dictionary<string, (string Tr, string En)> Varliklar = new()
    {
        ["TevsikIhlali"] = ("Bankasız ödeme sınırı aşımı", "Bank payment limit breach"),
        ["TevsikDogrulanamayan"] = ("Bankasız ödeme sınırı doğrulanamadı", "Bank payment limit not verified"),
        ["YasalParametre"] = ("Yasal sınır ayarı", "Legal limit setting"),
    };

    /// <summary>Hub'da ve ZO-BELGE'de adı olmayan kayıt türünün görünen adı; bilinmiyorsa null.</summary>
    public static string? VarlikAdi(string? kod, string dil) =>
        kod is not null && Varliklar.TryGetValue(kod, out var a) ? (dil == "en" ? a.En : a.Tr) : null;

    /// <summary>BÜYÜK_ALT_ÇİZGİ kodu (SATIS_FISI, SAYIM_DUZELTME, HAS_GRAM).</summary>
    public static readonly Regex BuyukAltCizgiKod =
        new(@"(?<![\p{L}\p{N}_])[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+(?![\p{L}\p{N}_])", RegexOptions.CultureInvariant);

    /// <summary>BASİT: "cari" + ek ("carisi", "cariye", "carinin" …). "müşteri" de i ile
    /// biter → ekler ünlü uyumuyla AYNEN taşınır.</summary>
    private static readonly Regex CariEkli =
        new(@"(?<![\p{L}])([Cc])ari(si|sine|sini|sinin|siyle|ye|yi|nin|den|de|yle|ler|leri|lerin|lere|lerden)(?![\p{L}])",
            RegexOptions.CultureInvariant);

    /// <summary>Bilinen belge türü kodunun görünen adı; bilinmiyorsa null.</summary>
    public static string? BelgeTuruAdi(string? kod, string dil)
    {
        var m = Al(dil);
        return kod switch
        {
            "SATIS_FISI" => m.BelgeSatisFisi,
            "TAHSILAT_MAKBUZU" => m.BelgeTahsilatMakbuzu,
            "EMANET_TESLIM" => m.BelgeEmanetTeslim,
            "CARI_EKSTRE" => m.BelgeCariEkstre,
            _ => null,
        };
    }

    /// <summary>
    /// Görünür metindeki BÜYÜK_ALT_ÇİZGİ kodlarını okunur ada çevirir: önce belge türü,
    /// sonra hareket türü (tek kaynak KatalogSozlugu.HareketTuru); ikisi de tanımıyorsa
    /// kod küçük harfe ve boşluğa çevrilir (ad UYDURULMAZ; ham kod title'da kalır).
    /// </summary>
    public static string? KodlariCevir(string? metin, string dil)
    {
        if (string.IsNullOrEmpty(metin)) return metin;
        return BuyukAltCizgiKod.Replace(metin, m =>
        {
            var kod = m.Value;
            var belge = BelgeTuruAdi(kod, dil);
            if (belge is not null) return belge;
            var hareket = KatalogSozlugu.HareketTuru(kod, dil);
            if (!string.IsNullOrEmpty(hareket) && hareket != kod) return hareket;
            return kod.Replace('_', ' ').ToLowerInvariant();
        });
    }

    // PRO denetim detayında kalan küçük harfli kebab kod adları (islem-defteri, cari-yaslandirma):
    // bilinenler okunur ada, kalanlar boşluklu sözcüğe. "e-posta" Türkçe yazımdır — dokunulmaz.
    // PRO'da "cari" KALIR (karar 1); BASİT çevirisi Q10-5'te (müşteri).
    private static readonly (Regex Kalip, string Tr, string En)[] KebabAdlari =
    [
        (new(@"(?<![\p{L}-])islem-defteri(?![\p{L}-])", RegexOptions.CultureInvariant), "işlem defteri", "transaction ledger"),
        (new(@"(?<![\p{L}-])cari-yaslandirma(?![\p{L}-])", RegexOptions.CultureInvariant), "cari yaşlandırma", "account ageing"),
        (new(@"(?<![\p{L}-])arayuz-modu(?![\p{L}-])", RegexOptions.CultureInvariant), "görünüm", "view mode"),
    ];

    private static readonly Regex KebabKod =
        new(@"(?<![\p{L}\p{N}/.-])(?!e-posta(?![\p{L}]))[a-z]+(?:-[a-z]+)+(?![\p{L}\p{N}/.-])", RegexOptions.CultureInvariant);

    /// <summary>PRO: kebab kod adlarını okunur metne çevirir (ham kayıt title'da kalır).</summary>
    public static string? KebabKodlariCevir(string? metin, string dil)
    {
        if (string.IsNullOrEmpty(metin)) return metin;
        var s = metin;
        foreach (var (kalip, tr, en) in KebabAdlari) s = kalip.Replace(s, dil == "en" ? en : tr);
        return KebabKod.Replace(s, m => m.Value.Replace('-', ' '));
    }

    // BELGE-KALAN (hakem tur 2, karar 2): arayüz tercihi kaydı "Arayüz tercihi güncellendi:
    // menu-acik-gruplar" iki modda da "menu acik gruplar" (ş/ı'sız ham anahtar) basıyordu.
    // Anahtar kümesi sunucudaki beyaz listedir (TercihAnahtarlari.Tumu) — tam ad eşleşmesiyle.
    private static readonly (Regex Kalip, string Tr, string En)[] TercihAdlari =
    [
        (new(@"(?<![\p{L}-])menu-acik-gruplar(?![\p{L}-])", RegexOptions.CultureInvariant), "menüde açık bölümler", "open menu sections"),
        (new(@"(?<![\p{L}-])menu-favoriler(?![\p{L}-])", RegexOptions.CultureInvariant), "menüdeki sık kullanılanlar", "menu favourites"),
        (new(@"(?<![\p{L}-])menu-sirasi(?![\p{L}-])", RegexOptions.CultureInvariant), "menü sırası", "menu order"),
        (new(@"(?<![\p{L}-])panel-duzeni(?![\p{L}-])", RegexOptions.CultureInvariant), "ana sayfa düzeni", "home layout"),
        (new(@"(?<![\p{L}-])gorunum-subesi(?![\p{L}-])", RegexOptions.CultureInvariant), "görüntülenen şube", "viewed branch"),
    ];

    /// <summary>BELGE-KALAN — arayüz tercihi anahtarlarını okunur ada çevirir (İKİ MODDA;
    /// ham kayıt title'da). Tanınmayan metin DEĞİŞMEZ.</summary>
    public static string? TercihAnahtarlariniCevir(string? metin, string dil)
    {
        if (string.IsNullOrEmpty(metin)) return metin;
        var s = metin;
        foreach (var (kalip, tr, en) in TercihAdlari) s = kalip.Replace(s, dil == "en" ? en : tr);
        return s;
    }

    /// <summary>BASİT: "carisi/cariye/…" → "müşterisi/müşteriye/…" (Q10-5 çevirisi ekli
    /// biçimi tanımıyordu — #54).</summary>
    public static string? CariEkleriniCevir(string? metin, string dil)
    {
        if (string.IsNullOrEmpty(metin) || dil == "en") return metin;
        return CariEkli.Replace(metin, m => (m.Groups[1].Value == "C" ? "Müşteri" : "müşteri") + m.Groups[2].Value);
    }
}
