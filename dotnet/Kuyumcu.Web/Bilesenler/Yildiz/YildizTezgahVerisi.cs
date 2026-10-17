using System.Globalization;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Bilesenler.Yildiz;

/// <summary>PAKET YILDIZ-4B — Tezgâh kalem satırı (tuval Tezgah4: [ayar 40 px] [ad / soluk ayrıntı] [tutar]).</summary>
public sealed record YildizTezgahKalem(int Sira, string Ayar, string Ad, string Ayrinti, string Tutar);

/// <summary>Ödeme satırı (tuval "Nakit · Ana Kasa · 15.000,00 ₺").</summary>
public sealed record YildizTezgahOdeme(int Sira, string Ad, string? Kasa, string Tutar);

/// <summary>
/// PAKET YILDIZ-4C — satıra dokununca açılan kalem düzenlemesinin değerleri (FisKalemTablosu PRO satır
/// düzenlemesiyle AYNI alanlar: adet · birim fiyat · satır indirimi · hediye; BASİT'te yalnız "Çıkar").
/// </summary>
public sealed record YildizKalemDuzenVerisi(int Sira, string UrunId, string AdetMetni, string FiyatMetni,
    string IskontoMetni, bool Hediye, string Birim);

/// <summary>YILDIZ-4C — kalem alanı yazıldı (Alan: "adet" | "fiyat" | "iskonto").</summary>
public sealed record YildizKalemGirdi(int Sira, string Alan, string Metin);

/// <summary>YILDIZ-4C — hediye işareti değişti.</summary>
public sealed record YildizKalemHediye(int Sira, bool Deger);

/// <summary>YILDIZ-4C — müşteri seçim listesinin satırı (tuval "Seni bekleyen" kalıbı: ad · soluk durum · b3).</summary>
public sealed record YildizMusteriSecenegi(string Id, string Ad, string? Durum);

/// <summary>
/// PAKET YILDIZ-4B — Tezgâh ekranının bağlamı: Islemler.Yildiz.cs doldurur, YildizTezgah çizer.
/// ⚠ YALNIZ GÖRÜNÜM: bütün değerler sayfanın MEVCUT durumundan okunur (ToplamTl, bacaklar,
/// FisFark, kilit sebebi, sunucunun nakit haddi); her eylem sayfanın MEVCUT yöntemidir
/// (Kaydet, Vazgec, OdemeyiBol, TekOdemeyeDonAracla, AcikHesapKaydet, IndirimDegistir).
/// Bileşen İSTEK ATMAZ; satış davranışı (karma ödeme, MASAK, nakit sınırı) DEĞİŞMEZ.
/// </summary>
public sealed class YildizTezgahBaglami
{
    public string TurAdi { get; init; } = "";
    public string? MusteriAdi { get; init; }
    public string? MusteriDurumu { get; init; }
    public bool MusteriAcik { get; init; }
    public bool TurlerAcik { get; init; }
    public IReadOnlyList<YildizTezgahKalem> Kalemler { get; init; } = [];
    public string ToplamAyrinti { get; init; } = "";
    public string ToplamMetni { get; init; } = "";
    public IReadOnlyList<YildizTezgahOdeme> Odemeler { get; init; } = [];
    public bool Karma { get; init; }
    /// <summary>TEZGAH-ODEME: karmada tek satır kaldı ve aracı tek ödemede seçilebilir (Nakit/Kart/Havale) → "− Tek ödeme" yoluna inilir.</summary>
    public bool TekeIndirilebilir { get; init; }
    /// <summary>BASİT tek ödeme: Nakit / Kart / Havale seçimi (sayfanın BasitOdemeSecenekleri; null = seçici özgün ödeme alanında).</summary>
    public IReadOnlyList<Kuyumcu.Web.Bilesenler.SecenekOgesi>? OdemeSecenekleri { get; init; }
    public string OdemeTuru { get; init; } = "NAKIT";
    /// <summary>M6 (16.10) — satış belgesi seçimi (Fiş | Fatura). Yalnız satışta görünür.</summary>
    public bool BelgeGorunur { get; init; }
    public string BelgeTuru { get; init; } = "FIS";
    /// <summary>Kuyumcu fatura haddi aşıldı: Fatura kilitli seçili.</summary>
    public bool BelgeZorunlu { get; init; }
    public string? BelgeNotu { get; init; }
    public EventCallback<string> BelgeSec { get; init; }
    /// <summary>17.10 havaleden satış — tezgâhın seçebileceği bekleyen gelen havaleler (yalnız satışta).</summary>
    public IReadOnlyList<YildizHavaleOgesi> Havaleler { get; init; } = [];
    /// <summary>Seçili havale (tezgâh havaleyi satıyor); null = normal satış.</summary>
    public string? SeciliHavaleId { get; init; }
    public bool HavalelerAcik { get; init; }
    public EventCallback HavalelerAcKapa { get; init; }
    public EventCallback<string> HavaleSec { get; init; }
    public EventCallback HavaleBirak { get; init; }
    public EventCallback<string> OdemeTuruSec { get; init; }
    public string FarkMetni { get; init; } = "";
    public bool FarkTamam { get; init; }
    /// <summary>Fark eksikken "Açık hesap olarak kaydet" görünür; kilitliyse sebebi.</summary>
    public bool AcikHesapGorunur { get; init; }
    public string? AcikHesapKilidi { get; init; }
    public string AcikHesapEtiketi { get; init; } = "";
    /// <summary>Fark bloğunun altındaki tek satır: yasal durum ya da Kaydet'in kilit sebebi (UX-S30).</summary>
    public string? Not { get; init; }
    public bool NotKotu { get; init; }
    public string KaydetEtiketi { get; init; } = "";
    public bool KaydetKilitli { get; init; }
    public string? KaydetKilitSebebi { get; init; }
    public bool Kaydediliyor { get; init; }
    public bool FisDolu { get; init; }
    public bool KalemVar { get; init; }
    public bool ParaUstuKullanilir { get; init; }
    public decimal NakitTutar { get; init; }
    public RenderFragment? Giris { get; init; }
    public RenderFragment? KalemDuzen { get; init; }
    public RenderFragment? OdemeDuzen { get; init; }
    public EventCallback Kaydet { get; init; }
    public EventCallback Vazgec { get; init; }
    public EventCallback BaskaOdeme { get; init; }
    public EventCallback TekOdeme { get; init; }
    public EventCallback Indirim { get; init; }
    public EventCallback AcikHesap { get; init; }
    public EventCallback MusteriDegistir { get; init; }
    public EventCallback TurDegistir { get; init; }

    // ── YILDIZ-4B tur 2 (tasarım hakemi KRİTİK) ─────────────────────────────────
    /// <summary>Müşteri satırı (sayfanın FisMusteriSatiri parçası — aynı id'ler); "Değiştir" katmanında çizilir.</summary>
    public RenderFragment? Musteri { get; init; }
    /// <summary>Kimlik/MASAK bloğu (sayfanın MASAK parçası — aynı kapı); null = gerekmiyor.</summary>
    public RenderFragment? Kimlik { get; init; }
    /// <summary>Kimlik bloğu çizilecek mi (sayfanın MASAK koşuluyla AYNI).</summary>
    public bool KimlikGorunur { get; init; }

    // ── P-TEZGAH-TUR-KIMLIK (denetim K4 · Y4) ───────────────────────────────────
    /// <summary>Görünür işlem türü sekmeleri (sayfanın FisTurSecici parçası — Satış · Alış · İade · Tahsilat); null = çizilmez.</summary>
    public RenderFragment? TurSecici { get; init; }
    /// <summary>Kimlik formu eksiksiz mi (sayfanın MasakTamam'ı — Kaydet kilidiyle AYNI koşul).</summary>
    public bool KimlikEksiksiz { get; init; }
    /// <summary>Çekmecenin "Tamam"ı eksik formla basıldı: sayfa eksik alanların altına mesaj yazar ve ilk eksiğe odaklanır.</summary>
    public EventCallback KimlikEksikGoster { get; init; }
    /// <summary>Kaydet'in son hatası (sayfanın <c>hata</c> alanı) — Fark not satırında, --kotu.</summary>
    public string? Hata { get; init; }
    /// <summary>Sunucunun ham reddi (dayanak) — hata satırının ipucu.</summary>
    public string? HataAyrinti { get; init; }
    /// <summary>Son kaydedilen fişin satırı ("Son fiş: …"); null = yok.</summary>
    public string? SonFis { get; init; }
    /// <summary>Yazdırma adresleri (sayfanın yazdirIdler'i, FisYazdir.YazdirAdresi ile).</summary>
    public IReadOnlyList<string> YazdirAdresleri { get; init; } = [];
    /// <summary>Yazdırma bağlantısı kurulamadıysa sebebi (UX-S28).</summary>
    public string? YazdirHata { get; init; }
    /// <summary>Kayıttan sonra sunucunun söylediği uyarılar (yerleşim, taksit) — soluk not satırı.</summary>
    public IReadOnlyList<string> Uyarilar { get; init; } = [];

    // ── YILDIZ-4C — etkileşim hâlleri tuval dilinde (eski form/tablo Yıldız'da ÇİZİLMEZ) ──────────────
    /// <summary>PRO mu (kalem düzenlemesinde adet/fiyat/indirim/hediye; müşteri katmanında satıcı/fiyat listesi).</summary>
    public bool Pro { get; init; }
    /// <summary>Kalem düzenleme değerleri (Kalemler ile aynı sıra).</summary>
    public IReadOnlyList<YildizKalemDuzenVerisi> KalemDuzenleri { get; init; } = [];
    /// <summary>Sayfanın form durumu (alan altı Türkçe ileti — id'ler FisKalemTablosu ile AYNI: si-kalem-*-{urunId}).</summary>
    public Kuyumcu.Web.Servisler.FormDurumu? Form { get; init; }
    public EventCallback<YildizKalemGirdi> KalemYaz { get; init; }
    public EventCallback<YildizKalemHediye> KalemHediye { get; init; }
    public EventCallback<int> KalemCikar { get; init; }
    /// <summary>Kalem düzenlemesinin altındaki fiş alanları (reyon · açıklama — sayfanın özgün parçaları).</summary>
    public RenderFragment? KalemEk { get; init; }
    /// <summary>Fiş indirimi açık mı (sayfanın indirimAcik'i ya da girilmiş oran) ve alanı (FisIskontoSatiri — id'ler aynı).</summary>
    public bool IndirimGorunur { get; init; }
    public RenderFragment? IndirimAlani { get; init; }
    /// <summary>Para üstünün hesaplanacağı nakit (tek ödemede fiş toplamı, karmada NAKİT satırlarının toplamı).</summary>
    public decimal ParaUstuNakit { get; init; }
    /// <summary>Karmada "+ Başka ödeme": yeni ödeme satırı (FisOdemeBacaklari "+ Başka ödeme ekle" ile AYNI kural).</summary>
    public EventCallback BacakEkle { get; init; }
    /// <summary>Karmada satır sayısı tavana (5) ulaştı mı.</summary>
    public bool BacakTavan { get; init; }
    /// <summary>Müşteri seçim listesi (sayfanın GET /cari listesi; sistem carisi dahil — Peşin Müşteri seçilebilir).</summary>
    public IReadOnlyList<YildizMusteriSecenegi> MusteriSecenekleri { get; init; } = [];
    public string MusteriSeciliId { get; init; } = "";
    /// <summary>Seçim: sayfa cariId'yi yazar ve MusteriDegisti'yi çağırır (FisMusteriSatiri.Sec ile AYNI sıra).</summary>
    public EventCallback<string> MusteriSec { get; init; }
    public string? PesinHata { get; init; }
    public bool PesinKimlikGerekli { get; init; }
    public EventCallback GercekMusteriSec { get; init; }
    /// <summary>PRO: satıcı + fiyat listesi alanları (sayfanın ProUstAlanlar parçası); BASİT'te null.</summary>
    public RenderFragment? ProUstAlanlar { get; init; }
    public bool ProUstAcikZorunlu { get; init; }
    /// <summary>Peşin Müşteri art arda sorusu (sayfanın PesinArtArdaSoru'su — aynı geri çağrılar); null = sorulmuyor.</summary>
    public RenderFragment? PesinSoru { get; init; }
}

/// <summary>PAKET YILDIZ-4B — Tezgâh yazımı (saf; tuval Tezgah4 sayı dili).</summary>
public static class YildizTezgahBicim
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Gram: en az iki, en çok üç hane ("1,75 g" — tuval; "0,001 g" — P-57: küçük gram KAYBOLMAZ, eskiden N2 "0,00 g"
    /// basıyordu — tema temizliği 17.10 GC bulgusu). Has: dört hane ("1,6030 g has").
    /// </summary>
    public static string Gram(decimal g) => FisHesap.GramSayi(g);
    public static string Has(decimal h) => FisHesap.HasYuvarla(h).ToString("N4", Tr);
    public static string Adet(decimal a) => a == decimal.Truncate(a) ? a.ToString("N0", Tr) : a.ToString("0.###", Tr);

    /// <summary>
    /// Kalem satırının soluk ayrıntısı ("2 adet · 1,00 g · 1,9900 g has"). Gram (tuval: tanesinin gramı) ve has (satır toplamı = kart has gramı × adet)
    /// ürün kartından türetilir (UX-S21); kart yoksa yalnız adet yazılır
    /// (uydurulmaz). Gram birimli üründe adet zaten gramdır → "adet" parçası yazılmaz.
    /// </summary>
    public static string KalemAyrinti(FisKalemi k, UrunVeri? u, string dil)
    {
        if (u is null) return YildizTezgahSozlugu.Adet(Adet(k.Adet), dil);
        var parca = new List<string>();
        if (u.Birim != "GRAM") parca.Add(YildizTezgahSozlugu.Adet(Adet(k.Adet), dil));
        parca.Add(YildizTezgahSozlugu.Gram(Gram(u.Birim == "GRAM" ? k.Adet : u.GramAgirlik)));   // tuval "2 adet · 1,00 g" = BİRİM gram; has = satır toplamı
        parca.Add(YildizTezgahSozlugu.Has(Has(u.HasGrami * k.Adet), dil));
        return string.Join(" · ", parca);
    }

    /// <summary>Fişin toplam ayrıntısı ("3,75 g · 3,5930 g has"); has bilinmiyorsa yalnız gram.</summary>
    public static string ToplamAyrinti(decimal gram, decimal? has, string dil) =>
        has is { } h ? $"{YildizTezgahSozlugu.Gram(Gram(gram))} · {YildizTezgahSozlugu.Has(Has(h), dil)}"
                     : YildizTezgahSozlugu.Gram(Gram(gram));

    /// <summary>Üst satır: "Satış · Selin Ergüven · borcu yok" (müşteri/durum yoksa o parça yazılmaz).</summary>
    public static string UstSatir(string tur, string? musteri, string? durum) =>
        string.Join(" · ", new[] { tur, musteri, durum }.Where(p => !string.IsNullOrWhiteSpace(p)));

    /// <summary>
    /// YILDIZ-4C — müşteri katmanının süzgeci: ad içinde arama (tr-TR, büyük/küçük harf duyarsız); seçili müşteri
    /// ilk sıraya alınır (hangi müşterinin seçili olduğu her zaman görünür), öteki sıra sayfanın listesi gibi.
    /// </summary>
    public static IReadOnlyList<YildizMusteriSecenegi> MusteriSuz(IReadOnlyList<YildizMusteriSecenegi> liste, string? arama, string? seciliId)
    {
        var a = (arama ?? "").Trim();
        var uyan = a.Length == 0 ? liste
            : [.. liste.Where(m => Tr.CompareInfo.IndexOf(m.Ad, a, CompareOptions.IgnoreCase) >= 0)];
        return [.. uyan.Where(m => m.Id == seciliId), .. uyan.Where(m => m.Id != seciliId)];
    }

    /// <summary>Gerçek müşterinin TL bakiye durumu (Peşin Müşteri'de yazılmaz). Artı = bize borçlu.</summary>
    public static string? MusteriDurumu(CariVeri? c, string dil)
    {
        if (c is null || c.SistemCarisi) return null;
        // P-DIL-BORC-KASA tur 2: tek işaret kuralı (yuvarlanmış değerden yön) — BorcDiliSozlugu.Durum.
        return Kuyumcu.Core.I18n.BorcDiliSozlugu.Durum(c.TlBakiye, 2, v => Servisler.Bicim.Tl(v), dil);
    }
}

/// <summary>17.10 — tezgâhtaki bekleyen havale satırı (ad · tutar · tarih; IBAN yok).</summary>
public sealed record YildizHavaleOgesi(string Id, string Ad, string Tutar, string Tarih);
