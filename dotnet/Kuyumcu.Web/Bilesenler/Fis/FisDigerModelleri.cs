using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Core.Sozlesmeler;

namespace Kuyumcu.Web.Bilesenler.Fis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-6 — PRO MODDA "DİĞER ▾" (PLAN-SADELESTIRME § S-2 madde 2.9)
//
// Sunucuda P-21 ile doğan uçların (rezerv · avans · konsinye · değişim ·
// siparişten satış · ciro/rücu) ekrandaki modeli. UX-S25: "uç var düğme yok"
// yasağı bu modelle kapanır.
//
// ⚠ TEK POST YOLU (PLAN § 3 md.5): bu dosya ve Fis/FisDiger*.razor bileşenleri
//   İSTEK ATMAZ, ApiIstemci enjekte etmez. Bileşen kullanıcının niyetini
//   <see cref="DigerIstek"/> olarak sayfaya bildirir; isteği sayfanın
//   Islemler.Diger.cs parçası atar (POS fişinin Kaydet()'ine DOKUNMADAN).
// ⚠ Yetki ≠ mod: rol kontrolü burada yalnız DÜĞMEYİ açıklamak içindir (UX-S30);
//   kapının kendisi sunucudaki [RolGerekli]/[ModulYetkisi]'dir.
// ⚠ Eşik/mevzuat sayısı YOK: gerekçe asgarileri (10 / 20) sunucu DTO'sunun
//   kendi sabitleridir ve sunucu da reddeder; ekran boş gönderimi durdurur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Diğer menüsünün işlem grupları (panel anahtarları).</summary>
public static class DigerTurleri
{
    public const string Rezerv = "REZERV";
    public const string Avans = "AVANS";
    public const string Konsinye = "KONSINYE";
    public const string Degisim = "DEGISIM";
    public const string Siparis = "SIPARIS";
    public const string Evrak = "EVRAK";

    /// <summary>Menüdeki sıra (taslak Profesyonel.dc.html, not n5).</summary>
    public static readonly string[] Tumu = [Rezerv, Avans, Konsinye, Degisim, Siparis, Evrak];

    /// <summary>Menü etiketi — tek kaynak (UX-S31).</summary>
    public static string Etiket(FisDigerMetinleri m, string tur) => tur switch
    {
        Rezerv => m.RezervBaslik,
        Avans => m.AvansBaslik,
        Konsinye => m.KonsinyeBaslik,
        Degisim => m.DegisimBaslik,
        Siparis => m.SiparisBaslik,
        _ => m.EvrakBaslik,
    };

    /// <summary>Menü açıklaması.</summary>
    public static string Aciklama(FisDigerMetinleri m, string tur) => tur switch
    {
        Rezerv => m.RezervAciklama,
        Avans => m.AvansAciklama,
        Konsinye => m.KonsinyeAciklama,
        Degisim => m.DegisimAciklama,
        Siparis => m.SiparisAciklama,
        _ => m.EvrakAciklama,
    };

    /// <summary>
    /// Grubun TAMAMINI kilitleyen rol sebebi (UX-S30). null = en az bir eylem açık.
    /// Sunucu kapısının AYNASIDIR (TicaretController / EvrakController [RolGerekli]).
    /// </summary>
    public static string? KilitSebebi(FisDigerMetinleri m, string tur, string? rol) => tur switch
    {
        Konsinye when !DigerYetki.SahipMudur(rol) => m.YetkiSahipMudur,
        Evrak when !DigerYetki.SahipMudurMuhasebe(rol) => m.YetkiSahipMudurMuhasebe,
        Rezerv or Degisim or Siparis when !DigerYetki.Tezgah(rol) => m.YetkiTezgah,
        _ => null,
    };
}

/// <summary>Sunucu [RolGerekli] listelerinin ekrandaki aynası (yalnız açıklama için).</summary>
public static class DigerYetki
{
    public static bool SahipMudur(string? rol) => rol is "SAHIP" or "MUDUR";
    public static bool SahipMudurMuhasebe(string? rol) => rol is "SAHIP" or "MUDUR" or "MUHASEBE";
    public static bool Tezgah(string? rol) => rol is "SAHIP" or "MUDUR" or "TEZGAHTAR";
    public static bool Avans(string? rol) => rol is "SAHIP" or "MUDUR" or "TEZGAHTAR" or "MUHASEBE";
}

/// <summary>Bileşenin sayfaya bildirdiği niyet. İsteği SAYFA atar.</summary>
/// <param name="Eylem"><see cref="DigerEylemler"/> sabitlerinden biri.</param>
/// <param name="Id">Satır kimliği (rezerv, konsinye çıkışı, sipariş, evrak).</param>
public sealed record DigerIstek(string Eylem, string? Id = null);

/// <summary>Sayfanın karşıladığı eylemler — her biri TEK bir sunucu ucudur.</summary>
public static class DigerEylemler
{
    public const string Yukle = "YUKLE";                   // seçili panelin listeleri (GET)
    public const string Kapat = "KAPAT";                 // istek yok — panel kapandı, sayfa fişi geri çizer
    public const string Ciz = "CIZ";                       // istek yok: onay modalı açıldı, üst bileşen çizilir
    public const string RezervAc = "REZERV_AC";            // POST   /islemler/rezervler
    public const string RezervDuzenle = "REZERV_DUZENLE";  // PATCH  /islemler/rezervler/{id}
    public const string RezervIptal = "REZERV_IPTAL";      // POST   /islemler/rezervler/{id}/iptal
    public const string AvansSorgula = "AVANS_SORGULA";    // GET    /islemler/avans/{cariId}
    public const string AvansAl = "AVANS_AL";              // POST   /islemler/avans
    public const string AvansMahsup = "AVANS_MAHSUP";      // POST   /islemler/avans/mahsup
    public const string AvansIade = "AVANS_IADE";          // POST   /islemler/avans/iade
    public const string KonsinyeCikis = "KONSINYE_CIKIS";  // POST   /islemler/konsinye-cikis
    public const string KonsinyeIade = "KONSINYE_IADE";    // POST   /islemler/konsinye-iade
    public const string Degisim = "DEGISIM";               // POST   /islemler/degisim
    public const string SiparisSec = "SIPARIS_SEC";        // GET    /siparisler/{id}
    public const string SiparisCevir = "SIPARIS_CEVIR";    // POST   /siparisler/{id}/satisa-cevir
    public const string EvrakCiro = "EVRAK_CIRO";          // POST   /evraklar/{id}/ciro
    public const string EvrakRucu = "EVRAK_RUCU";          // POST   /evraklar/{id}/rucu
}

/// <summary>Bileşenlerin ortak bağlamı — sayfanın ZATEN yüklediği veriler (yeni istek yok).</summary>
public sealed record DigerBaglam(
    IReadOnlyList<CariVeri> Cariler,
    IReadOnlyList<UrunVeri> Urunler,
    IReadOnlyList<KasaVeri> Kasalar,
    IReadOnlyList<IadeAdayi> SonSatislar,
    bool KurVar,
    string? Rol,
    string? OturumSubeId = null,
    decimal? NakitHad = null)   // SATIS-3 t22: sunucunun nakit tevsik haddi (/tevsik/gunluk) — sayı istemciye gömülmez; bilinmezse ön denetim yok, kapı sunucuda
{
    /// <summary>SATIS-3 t6: fişle aynı kural — oturumun şubesindeki ilk TL kasası; yoksa null (seçici zorunlu).</summary>
    public string? VarsayilanKasaId => FisHesap.VarsayilanKasa(Kasalar, OturumSubeId);
    /// <summary>UX-S22: tek TL kasası varsa sorulmaz.</summary>
    public string? TekKasaId => Kasalar.Count == 1 ? Kasalar[0].Id : null;
    public string CariAdi(string? id) => Cariler.FirstOrDefault(c => c.Id == id)?.Ad ?? "—";
    public string UrunAdi(string? id) => Urunler.FirstOrDefault(u => u.Id == id)?.Ad ?? "—";
}

// ── Sunucu yanıt modelleri (camelCase JSON; ApiIstemci Web varsayılanı) ─────

public sealed record RezervSatiri(
    string Id, string UrunId, string CariId, string? SiparisId, decimal Adet, string Durum,
    DateOnly? BitisGunu, string? Aciklama, string? KapanisGerekcesi, bool SuresiDoldu, bool Duzenlenebilir);

public sealed record RezervListesi(List<RezervSatiri>? Satirlar, int Toplam, int Limit, bool SinirAsildi);

public sealed record KonsinyeSatiri(
    string Id, string? CariId, string? UrunId, DateTime Tarih, string? Aciklama,
    decimal CikanAdet, decimal IadeEdilenAdet, decimal AcikAdet);

public sealed record KonsinyeListesi(List<KonsinyeSatiri>? Satirlar, int Toplam, int Limit, bool SinirAsildi);

public sealed record SiparisOzeti(string Id, string No, string Durum, string? CariId, string? CariAd);

public sealed record SiparisListesi(List<SiparisOzeti>? Siparisler, int Toplam, int Limit, bool DahaFazlasiVar);

public sealed record SiparisKalemi(string Id, string? UrunId, string? UrunAd, string? Tanim, decimal? GramAgirlik, decimal Adet);

public sealed record SiparisDetayi(string Id, string No, string Durum, string? CariId, List<SiparisKalemi>? Kalemler);

// ── Form girişleri (durumun sahibi SAYFADIR; bileşen yalnız doldurur) ────────

/// <summary>Alan → Türkçe hata; boşsa form gönderilebilir (DoD EK md.3).</summary>
public static class DigerDogrula
{
    public static Dictionary<string, string> Yeni() => new(StringComparer.Ordinal);

    public static void Zorunlu(Dictionary<string, string> h, string alan, string? deger, FisDigerMetinleri m)
    {
        if (string.IsNullOrWhiteSpace(deger)) h[alan] = m.ZorunluAlan;
    }

    public static void Pozitif(Dictionary<string, string> h, string alan, decimal? deger, FisDigerMetinleri m)
    {
        if (deger is null) h[alan] = m.ZorunluAlan;
        else if (deger <= 0m) h[alan] = m.SifirdanBuyuk;
    }

    public static void Gerekce(Dictionary<string, string> h, string alan, string? deger, int asgari, FisDigerMetinleri m)
    {
        if ((deger ?? "").Trim().Length < asgari) h[alan] = string.Format(m.GerekceKisa, asgari);
    }
}

public sealed class RezervGirisi
{
    public string? UrunId { get; set; }
    public string? CariId { get; set; }
    public decimal? Adet { get; set; } = 1m;
    public DateOnly? BitisGunu { get; set; }
    public string Aciklama { get; set; } = "";

    public Dictionary<string, string> Hatalar(FisDigerMetinleri m)
    {
        var h = DigerDogrula.Yeni();
        DigerDogrula.Zorunlu(h, "urun", UrunId, m);
        DigerDogrula.Zorunlu(h, "cari", CariId, m);
        DigerDogrula.Pozitif(h, "adet", Adet, m);
        return h;
    }

    public object Govde() => new
    {
        urunId = UrunId,
        cariId = CariId,
        adet = Adet,
        bitisGunu = Kuyumcu.Web.Servisler.Bicim.Makine(BitisGunu),
        aciklama = string.IsNullOrWhiteSpace(Aciklama) ? null : Aciklama.Trim(),
    };
}

/// <summary>
/// Rezerv düzenleme (Cekmece). PATCH SÖZLEŞMESİ: yalnız DEĞİŞEN alan gövdeye
/// girer (gönderilmeyen = korunur), boşaltılan son gün / açıklama açık null
/// olarak gider (null = sil). Adet boşaltılamaz (sunucu reddeder).
/// </summary>
public sealed class RezervDuzenlemesi
{
    public RezervSatiri? Asil { get; private set; }
    public decimal? Adet { get; set; }
    public DateOnly? BitisGunu { get; set; }
    public string Aciklama { get; set; } = "";

    public void Ac(RezervSatiri r)
    {
        Asil = r;
        Adet = r.Adet;
        BitisGunu = r.BitisGunu;
        Aciklama = r.Aciklama ?? "";
    }

    public void Kapat() => Asil = null;

    public Dictionary<string, string> Hatalar(FisDigerMetinleri m)
    {
        var h = DigerDogrula.Yeni();
        DigerDogrula.Pozitif(h, "adet", Adet, m);
        return h;
    }

    /// <summary>Yalnız değişen alanlar. Hiçbir şey değişmediyse boş sözlük.</summary>
    public Dictionary<string, object?> Govde()
    {
        var g = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (Asil is null) return g;
        if (Adet is { } a && a != Asil.Adet) g["adet"] = a;
        if (BitisGunu != Asil.BitisGunu)
            g["bitisGunu"] = Kuyumcu.Web.Servisler.Bicim.Makine(BitisGunu);
        var yeniAciklama = string.IsNullOrWhiteSpace(Aciklama) ? null : Aciklama.Trim();
        if (yeniAciklama != (Asil.Aciklama ?? null)) g["aciklama"] = yeniAciklama;
        return g;
    }
}

public sealed class AvansGirisi
{
    /// <summary>Sunucu DTO'larının kendi asgarisi (AvansIadeDto MinimumLength = 10).</summary>
    public const int IadeGerekceAsgari = 10;

    public string? CariId { get; set; }
    public decimal? Tutar { get; set; }
    public string OdemeTuru { get; set; } = "NAKIT";
    public string? KasaId { get; set; }
    public string Aciklama { get; set; } = "";
    public string? MahsupSatisId { get; set; }
    public decimal? MahsupTutar { get; set; }
    public decimal? IadeTutar { get; set; }
    public string IadeGerekce { get; set; } = "";

    /// <summary>Seçili carinin açık avansı (sunucudan; null = sorulmadı).</summary>
    public decimal? AcikAvans { get; set; }

    public Dictionary<string, string> AlHatalari(FisDigerMetinleri m, string? kasaId)
    {
        var h = DigerDogrula.Yeni();
        DigerDogrula.Zorunlu(h, "cari", CariId, m);
        DigerDogrula.Pozitif(h, "tutar", Tutar, m);
        DigerDogrula.Zorunlu(h, "kasa", kasaId, m);
        return h;
    }

    public Dictionary<string, string> MahsupHatalari(FisDigerMetinleri m)
    {
        var h = DigerDogrula.Yeni();
        DigerDogrula.Zorunlu(h, "satis", MahsupSatisId, m);
        DigerDogrula.Pozitif(h, "mahsup-tutar", MahsupTutar, m);
        return h;
    }

    public Dictionary<string, string> IadeHatalari(FisDigerMetinleri m, string? kasaId)
    {
        var h = DigerDogrula.Yeni();
        DigerDogrula.Zorunlu(h, "cari", CariId, m);
        DigerDogrula.Pozitif(h, "iade-tutar", IadeTutar, m);
        DigerDogrula.Zorunlu(h, "kasa", kasaId, m);
        DigerDogrula.Gerekce(h, "iade-gerekce", IadeGerekce, IadeGerekceAsgari, m);
        return h;
    }
}

public sealed class KonsinyeGirisi
{
    public string? CariId { get; set; }
    public string? UrunId { get; set; }
    public decimal? Adet { get; set; } = 1m;
    public string Aciklama { get; set; } = "";

    /// <summary>Satır başına geri gelen adet (çıkış hareketi kimliği → adet).</summary>
    public Dictionary<string, decimal?> GeriAdet { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> Hatalar(FisDigerMetinleri m)
    {
        var h = DigerDogrula.Yeni();
        DigerDogrula.Zorunlu(h, "cari", CariId, m);
        DigerDogrula.Zorunlu(h, "urun", UrunId, m);
        DigerDogrula.Pozitif(h, "adet", Adet, m);
        return h;
    }
}

public sealed class DegisimGirisi
{
    /// <summary>
    /// Farkın ödeme türleri. KART YOKTUR: sunucu DegisimDto yalnız NAKIT|VERESIYE
    /// kabul eder (POS yan defteri kararı P-21 kapsamı dışında) — ekran seçeneği
    /// hiç sunmaz ve sebebini yazar (UX-S30).
    /// </summary>
    public static readonly string[] FarkTurleri = ["NAKIT", "VERESIYE"];

    public string? KaynakHareketId { get; set; }
    public decimal? GeriAlinanAdet { get; set; } = 1m;
    public string? YeniUrunId { get; set; }
    public decimal? YeniAdet { get; set; } = 1m;
    public decimal? YeniBirimFiyat { get; set; }
    public string FarkOdemeTuru { get; set; } = "NAKIT";
    public string Aciklama { get; set; } = "";

    public Dictionary<string, string> Hatalar(FisDigerMetinleri m, string? kasaId)
    {
        var h = DigerDogrula.Yeni();
        DigerDogrula.Zorunlu(h, "kaynak", KaynakHareketId, m);
        DigerDogrula.Pozitif(h, "geri-adet", GeriAlinanAdet, m);
        DigerDogrula.Zorunlu(h, "yeni-urun", YeniUrunId, m);
        DigerDogrula.Pozitif(h, "yeni-adet", YeniAdet, m);
        if (YeniBirimFiyat is null) h["yeni-fiyat"] = m.ZorunluAlan;
        else if (YeniBirimFiyat < 0m) h["yeni-fiyat"] = m.SifirdanBuyuk;
        if (FarkOdemeTuru == "NAKIT") DigerDogrula.Zorunlu(h, "kasa", kasaId, m);
        return h;
    }
}

public sealed class SiparisGirisi
{
    public SiparisDetayi? Secili { get; set; }

    /// <summary>
    /// PAKET YASAL-KAPI-2: siparişten satış sunucuda SatisFisi'nin MASAK kapısından geçer (SiparisSatisaCevirDto.Masak);
    /// ekran kimlik alanı göstermiyordu → eşik üstü sipariş teslimi çıkışsızdı. Sayfa kurar (API'li), bileşen çizer.
    /// </summary>
    public Kuyumcu.Web.Bilesenler.ParaMasakKapisi? Masak { get; set; }
    public Dictionary<string, decimal?> Fiyat { get; } = new(StringComparer.Ordinal);
    public string Arac { get; set; } = "NAKIT";
    public decimal? AvansMahsup { get; set; }

    /// <summary>Satır toplamı = Σ adet × birim fiyat (önizleme; tutarı SUNUCU kesinleştirir).</summary>
    public decimal Toplam => Secili?.Kalemler?.Sum(k => k.Adet * (Fiyat.GetValueOrDefault(k.Id) ?? 0m)) ?? 0m;

    public Dictionary<string, string> Hatalar(FisDigerMetinleri m, string? kasaId)
    {
        var h = DigerDogrula.Yeni();
        if (Secili is null) { h["siparis"] = m.ZorunluAlan; return h; }
        foreach (var k in Secili.Kalemler ?? [])
            if (Fiyat.GetValueOrDefault(k.Id) is not { } f || f <= 0m) h[$"fiyat-{k.Id}"] = m.SifirdanBuyuk;
        if (Arac != "VERESIYE") DigerDogrula.Zorunlu(h, "kasa", kasaId, m);
        if (AvansMahsup is < 0m) h["mahsup"] = m.SifirdanBuyuk;
        return h;
    }
}

public sealed class EvrakGirisi
{
    /// <summary>Sunucunun rücu gerekçe asgarisi (EvrakServisi.GerekceAsgari = 20).</summary>
    public const int RucuGerekceAsgari = 20;

    public string? CiroEvrakId { get; set; }
    public string? CiroCariId { get; set; }
    public string? RucuEvrakId { get; set; }
    public string RucuAciklama { get; set; } = "";

    public Dictionary<string, string> CiroHatalari(FisDigerMetinleri m)
    {
        var h = DigerDogrula.Yeni();
        DigerDogrula.Zorunlu(h, "ciro-evrak", CiroEvrakId, m);
        DigerDogrula.Zorunlu(h, "ciro-cari", CiroCariId, m);
        return h;
    }

    public Dictionary<string, string> RucuHatalari(FisDigerMetinleri m)
    {
        var h = DigerDogrula.Yeni();
        DigerDogrula.Zorunlu(h, "rucu-evrak", RucuEvrakId, m);
        DigerDogrula.Gerekce(h, "rucu-aciklama", RucuAciklama, RucuGerekceAsgari, m);
        return h;
    }
}

/// <summary>
/// "Diğer" panelinin tüm durumu. Sahibi SAYFADIR (Islemler.Diger.cs);
/// bileşenler alanları doldurur ve niyeti <see cref="DigerIstek"/> ile bildirir.
/// </summary>
public sealed class DigerIslemDurumu
{
    /// <summary>Açık panel (<see cref="DigerTurleri"/>); null = POS fişi görünür.</summary>
    public string? Secim { get; set; }
    public bool Mesgul { get; set; }
    public string? Hata { get; set; }
    /// <summary>SATIS-3 t22: sunucunun nakit (tevsik) reddi — hangi eylemin düğmesinin yanında kısa metinle yazılacağı.</summary>
    public (string Eylem, string Metin)? Red { get; set; }
    public string? Basari { get; set; }
    public int Jeton { get; set; }

    /// <summary>Liste okunamadıysa sebebi (UX-S28: hata "kayıt yok"a çevrilmez).</summary>
    public string? ListeHata { get; set; }

    /// <summary>Gönderim denendi mi (alan altı mesajlar yalnız o zaman çıkar).</summary>
    public HashSet<string> Denenen { get; } = new(StringComparer.Ordinal);

    /// <summary>Yıkıcı/geri alınamaz eylem onayı bekleniyor (eylem kodu), null = yok.</summary>
    public string? Onay { get; set; }
    public string? OnayId { get; set; }

    public string? KasaId { get; set; }

    public RezervGirisi Rezerv { get; } = new();
    public RezervDuzenlemesi RezervDuzen { get; } = new();
    public string RezervIptalGerekce { get; set; } = "";
    public RezervListesi? Rezervler { get; set; }

    public AvansGirisi Avans { get; } = new();

    public KonsinyeGirisi Konsinye { get; } = new();
    public KonsinyeListesi? Konsinyeler { get; set; }

    public DegisimGirisi Degisim { get; } = new();

    public SiparisGirisi Siparis { get; } = new();
    public SiparisListesi? Siparisler { get; set; }

    public EvrakGirisi Evrak { get; } = new();
    public List<EvrakSatiri>? Devredilebilir { get; set; }
    public List<EvrakSatiri>? Devredilmis { get; set; }
    public bool EvrakDahaFazla { get; set; }

    /// <summary>Paneli değiştirir; bandlar ve onay sıfırlanır (eski sonuç yeni panelde kalmaz).</summary>
    public void Sec(string? tur)
    {
        Secim = tur;
        Hata = null;
        Basari = null;
        ListeHata = null;
        Onay = null;
        OnayId = null;
        Denenen.Clear();
        RezervDuzen.Kapat();
    }

    /// <summary>Etkin kasa: UX-S22 tek kasa → o, yoksa seçilen.</summary>
    public string? EtkinKasa(DigerBaglam b) => b.TekKasaId ?? KasaId;
}
