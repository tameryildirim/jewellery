using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-4 — EKRANIN ALTIN BACAĞI MODELİ (KARAR-HAS-DEFTERI § 4.5, B4-31..35)
//
// ⚠ HESAP TEK KAYNAKTAN: has / brüt / kesinti / net Kuyumcu.Core.Domain
//   .AltinBacagiHesap ile hesaplanır — sunucunun "kuruşu kuruşuna" kapısıyla
//   (OdemeSepeti.AltinBacaginiCoz) AYNI formül. Ekran ayrı formül YAZMAZ.
// ⚠ ORAN KODA GÖMÜLMEZ: stopaj oranları GET /islemler/altin-bacagi/ayarlar'dan
//   gelir; oran bilinmiyorsa net ÜRETİLMEZ (fail-closed) ve kayıt kilitlenir.
// ⚠ TL YAZILMAZ (B4-31): modelde TL alanı YOKTUR; yalnız türetilir.
// ⚠ Bu model İSTEK ATMAZ. Satış fişine bacağı ekleyen ve POST eden sayfadır
//   (S-2 kuralı: "TEK POST YOLU — Kaydet() Islemler.razor'da").
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sunucunun yapılandırmadan okuduğu satıcı tipi → stopaj oranı.</summary>
public sealed record AltinBacagiStopaj(string SaticiTipi, decimal Oran, bool PusulaGerekir);

/// <summary>GET /islemler/altin-bacagi/ayarlar yanıtı.</summary>
public sealed record AltinBacagiAyarlari(
    List<AltinBacagiStopaj> StopajOranlari, decimal? AlisIskontoOran, bool SaticiAdresiZorunlu);

/// <summary>Panelin her değişiklikte sayfaya bildirdiği durum.</summary>
/// <param name="Kaydedilebilir">Tüm alanlar tam VE fark 0,00 (B4-32 · B4-33).</param>
/// <param name="Eksikler">Kilidin sebepleri — ekranda yazılır (UX-S30).</param>
/// <param name="Fark">Bacak toplamı − fiş toplamı (bu bacak dahil).</param>
/// <param name="Deger">Türetilen has/brüt/kesinti/net; hesaplanamıyorsa null.</param>
/// <param name="Bacak">satis-fisi gövdesine konacak bacak; eksik varsa null.</param>
public sealed record AltinBacagiDurumu(
    bool Kaydedilebilir, IReadOnlyList<string> Eksikler, decimal Fark,
    AltinBacagiDegeri? Deger, object? Bacak);

/// <summary>Altın bacağının kullanıcı girdileri — HER ALAN kayıttan önce serbestçe değişir (B4-34).</summary>
public sealed class AltinBacagiGirisi
{
    public const string NihaiTuketici = "NIHAI_TUKETICI";
    public const string EsnafMuaf = "ESNAF_MUAF";
    public const string Mukellef = "MUKELLEF";

    public string? AyarId { get; set; }
    /// <summary>PAKET ISLEM-BANT — bant dışı has fiyatının SAHİP/MÜDÜR onayı (fiş gövdesinde bantOnay/bantGerekce).</summary>
    public Kuyumcu.Web.Bilesenler.Fis.IslemBantOnayDurumu Bant { get; } = new();
    public decimal? Gram { get; set; }
    public decimal? Fiyat { get; set; }
    /// <summary>Fiyat kullanıcı tarafından yazıldı mı (rozet: elle / önerildi).</summary>
    public bool FiyatElle { get; set; }
    /// <summary>SATIS-3 t6: fiyat "Güncel fiyatı kullan" ile KURDAN yazıldı (rozet "Kurdan geldi"); elle yazınca düşer.
    /// Girişin kendisinde tutulur — bileşen BASİT↔PRO geçişinde yeniden kurulsa da kaynak bilgisi kaybolmaz (UX-S21).</summary>
    public bool FiyatKurdan { get; set; }
    /// <summary>Perakendede satıcı çoğunlukla bireysel müşteridir; değiştirilebilir.</summary>
    public string SaticiTipi { get; set; } = NihaiTuketici;
    public string? AdSoyad { get; set; }
    public string? Tckn { get; set; }
    public string? Adres { get; set; }
    public string? TedarikciFaturaNo { get; set; }

    private string TcknRakam => new((Tckn ?? "").Where(char.IsAsciiDigit).ToArray());

    // ═══ k2 (tasarım hakemi, 29.09): satıcı kimliği SEÇİLİ MÜŞTERİDEN dolar (hurda alışındaki gibi — UX-S21/S24) ═══
    // Yalnız BOŞ alan doldurulur; kullanıcının yazdığı ezilmez. Bayraklar gövdeye GİRMEZ (Bacak() değişmedi):
    // ekran "Müşteriden geldi" rozetini ve BASİT'te yalnız EKSİK alanı göstermeyi bunlarla bilir.
    /// <summary>Son ön-dolgunun yapıldığı müşteri (aynı müşteri için tekrar doldurulmaz).</summary>
    public string? OnDolguCariId { get; set; }
    public bool AdCariden { get; set; }
    public bool TcknCariden { get; set; }
    public bool AdresCariden { get; set; }

    /// <summary>
    /// Seçili müşterinin kayıtlı ad/TCKN/adresini BOŞ alanlara yazar (müşteri değişince önceki müşteriden gelenler
    /// yenisiyle değişir; elle yazılana dokunulmaz). TCKN yalnız kayıttaki numara TCKN ise (tür TCKN ya da tür beyansız
    /// 11 hane) — vergi kimlik numarası TCKN kutusuna konmaz. Müşteri yoksa (Peşin) hiçbir şey yapmaz.
    /// </summary>
    public bool MusteridenDoldur(string? cariId, string? ad, string? vergiNo, string? kimlikTuru, string? adres)
    {
        if (cariId == OnDolguCariId) return false;
        OnDolguCariId = cariId;
        if (string.IsNullOrEmpty(cariId))
        {
            // Müşteri kaldırıldı (Peşin): önceki müşteriden gelenler silinir, elle yazılan kalır.
            if (AdCariden) AdSoyad = null;
            if (TcknCariden) Tckn = null;
            if (AdresCariden) Adres = null;
            AdCariden = TcknCariden = AdresCariden = false;
            return true;
        }
        var no = new string((vergiNo ?? "").Where(char.IsAsciiDigit).ToArray());
        var tcknMi = no.Length == 11 && (kimlikTuru is null or "" or "TCKN");
        if (AdCariden || string.IsNullOrWhiteSpace(AdSoyad)) { AdSoyad = string.IsNullOrWhiteSpace(ad) ? null : ad.Trim(); AdCariden = AdSoyad is not null; }
        if (TcknCariden || string.IsNullOrWhiteSpace(Tckn)) { Tckn = tcknMi ? no : null; TcknCariden = tcknMi; }
        if (AdresCariden || string.IsNullOrWhiteSpace(Adres)) { Adres = string.IsNullOrWhiteSpace(adres) ? null : adres.Trim(); AdresCariden = Adres is not null; }
        return true;
    }

    /// <summary>k2: alan altı mesajlar ("Kaydetmek için" listesinin yerine). Satıcı alanları yalnız altın tutarı hesaplanınca
    /// (kullanıcı ayar/gram/fiyatı bitirince) söylenir — açılışta kırmızı duvar yok; ayar/gram/fiyat, üçünden biri yazılınca.</summary>
    public bool HesapBasladi => AyarId is not null || Gram is > 0m;
    public string? AyarHata(AltinBacagiMetinleri m) => HesapBasladi && (AyarId is null || Ayarlar.Bul(AyarId) is not { HasHesaplanir: true }) ? m.AyarSecin : null;
    public string? GramHata(AltinBacagiMetinleri m) => HesapBasladi && Gram is not > 0m ? m.GramGirin : null;
    public string? FiyatHata(AltinBacagiMetinleri m) => HesapBasladi && Fiyat is not > 0m ? m.FiyatGirin : null;
    public string? AdHata(AltinBacagiAyarlari? a, SozlukIslem t) => Deger(a) is not null && SaticiTipi != Mukellef && string.IsNullOrWhiteSpace(AdSoyad) ? t.SaticiAdZorunlu : null;
    public string? TcknHata(AltinBacagiAyarlari? a, SozlukIslem t) => Deger(a) is null || SaticiTipi == Mukellef ? null
        : TcknRakam.Length == 0 ? t.SaticiTcknZorunlu : TcknRakam.Length != 11 ? t.SaticiTcknGecersiz
        : !KimlikNoDogrulayici.TcknGecerli(TcknRakam) ? KimlikNoSozlugu.Icin(t).TcknGecersiz : null;   // TCKN-DOGRULAMA
    public string? AdresHata(AltinBacagiAyarlari? a, AltinBacagiMetinleri m) =>
        Deger(a) is not null && SaticiTipi != Mukellef && a is { SaticiAdresiZorunlu: true } && string.IsNullOrWhiteSpace(Adres) ? m.AdresGirin : null;
    public string? FaturaHata(AltinBacagiAyarlari? a, SozlukIslem t) =>
        Deger(a) is not null && SaticiTipi == Mukellef && string.IsNullOrWhiteSpace(TedarikciFaturaNo) ? t.TedarikciFaturaNoZorunlu : null;

    /// <summary>Seçili satıcı tipinin YAPILANDIRILMIŞ oranı; bilinmiyorsa null (net üretilmez).</summary>
    public decimal? Oran(AltinBacagiAyarlari? a) =>
        a?.StopajOranlari.FirstOrDefault(s => s.SaticiTipi == SaticiTipi)?.Oran;

    /// <summary>Pusula doğacak mı — sunucunun beyanından (mükellefte fatura belgedir).</summary>
    public bool PusulaGerekir(AltinBacagiAyarlari? a) =>
        a?.StopajOranlari.FirstOrDefault(s => s.SaticiTipi == SaticiTipi)?.PusulaGerekir ?? true;

    /// <summary>B4-31: gram + ayar → has → fiyat → TL. Eksik girdide null (uydurulmaz).</summary>
    public AltinBacagiDegeri? Deger(AltinBacagiAyarlari? a)
    {
        if (AyarId is null || Ayarlar.Bul(AyarId) is not { HasHesaplanir: true } ayar) return null;
        if (Gram is not { } g || g <= 0m || Fiyat is not { } f || f <= 0m) return null;
        if (Oran(a) is not { } oran) return null;
        return AltinBacagiHesap.Hesapla(g, ayar, f, oran);
    }

    /// <summary>K-KUR-4: ön-dolgu yalnız yapılandırılmış oranla; kullanıcı yazdıysa ezilmez.</summary>
    public void OnDoldur(decimal? hasFiyati, AltinBacagiAyarlari? a)
    {
        if (FiyatElle) return;
        Fiyat = AltinBacagiHesap.OnDolgu(hasFiyati, a?.AlisIskontoOran);
    }

    /// <summary>Kayıt kilidinin sebepleri (UX-S30), fark dahil (B4-33).</summary>
    public List<string> Eksikler(AltinBacagiAyarlari? a, SozlukIslem t, AltinBacagiMetinleri m, decimal fark)
    {
        var e = new List<string>();
        if (a is null) { e.Add(m.AyarlarYok); return e; }
        if (AyarId is null || Ayarlar.Bul(AyarId) is not { HasHesaplanir: true }) e.Add(m.AyarSecin);
        if (Gram is not > 0m) e.Add(m.GramGirin);
        if (Fiyat is not > 0m) e.Add(m.FiyatGirin);
        if (Oran(a) is null) e.Add(t.SaticiTipiZorunlu);
        if (SaticiTipi == Mukellef)
        {
            if (string.IsNullOrWhiteSpace(TedarikciFaturaNo)) e.Add(t.TedarikciFaturaNoZorunlu);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(AdSoyad)) e.Add(t.SaticiAdZorunlu);
            if (TcknRakam.Length == 0) e.Add(t.SaticiTcknZorunlu);
            else if (TcknRakam.Length != 11) e.Add(t.SaticiTcknGecersiz);
            else if (!KimlikNoDogrulayici.TcknGecerli(TcknRakam)) e.Add(KimlikNoSozlugu.Icin(t).TcknGecersiz);   // TCKN-DOGRULAMA
            if (a.SaticiAdresiZorunlu && string.IsNullOrWhiteSpace(Adres)) e.Add(m.AdresGirin);
        }
        if (fark != 0m) e.Add(m.FarkSifirOlmali);
        return e;
    }

    /// <summary>satis-fisi gövdesindeki ALTIN bacağı (camelCase alan adları sunucu sözleşmesiyle aynı).</summary>
    public object? Bacak(AltinBacagiDegeri? d) => d is null ? null : new
    {
        arac = "ALTIN",
        tutarTl = d.NetTl,
        ayarId = AyarId,
        gramAgirlik = Gram,
        birimFiyatTlHasGram = Fiyat,
        satici = new
        {
            saticiTipi = SaticiTipi,
            adSoyad = AdSoyad?.Trim(),
            tckn = TcknRakam,
            adres = Adres?.Trim(),
            tedarikciFaturaNo = TedarikciFaturaNo?.Trim(),
        },
    };
}

/// <summary>
/// PAKET B-4 — GET /islemler/altin-bacagi/ayarlar. Stopaj oranları ve ön-dolgu
/// oranı YAPILANDIRMADAN gelir (ADR-0005; oran ekrana gömülmez). Eksik yanıt ya
/// da hata null döner (fail-closed): panel #altin-ayarlar-yok bandını gösterir
/// ve kayıt kilitli kalır; sessiz varsayılan oran KULLANILMAZ (UX-S28).
/// Fiş bileşenleri istek atmadığı için (S-2 kuralı) çağıran SAYFADIR.
/// </summary>
public static class AltinBacagiAyarKaynagi
{
    public const string Yol = "/islemler/altin-bacagi/ayarlar";

    public static async Task<AltinBacagiAyarlari?> Yukle(ApiIstemci api)
    {
        try
        {
            var a = await api.GetAsync<AltinBacagiAyarlari>(Yol);
            return a?.StopajOranlari is { Count: > 0 } ? a : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
