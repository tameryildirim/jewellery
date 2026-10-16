using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Bilesenler.Grid;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GRID-1 — ORTAK IZGARANIN KOLON TANIMI (envanter § G1 "Kolon tanımı")
//
// Bir kolon: kalıcı ANAHTAR (tercihte saklanır — değiştirilirse kullanıcının
// düzeni o kolon için varsayılana döner; o yüzden Türkçe ad değil, sabit kod),
// sözlükten gelen BAŞLIK (çıplak metin yok — UX-S31), hücre içeriği (şablon ya
// da tür + değer), zorunlu görünürlük, varsayılan görünürlük ve sunucu sıralama adı.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Hücre biçim türü — biçim Servisler/Bicim.cs'ten gelir (tek kaynak).</summary>
public enum IzgaraHucreTuru { Metin, Para, Tl, Gram, Has, Tarih, Adet, Rozet, Eylem }

public sealed class IzgaraKolonu<T>
{
    /// <summary>Kalıcı kolon anahtarı (^[a-z0-9-]{1,40}$). Tercihte saklanır; DEĞİŞTİRME.</summary>
    public required string Anahtar { get; init; }

    /// <summary>Başlık — sözlükten (Dil.T…). Kart düzeninde hücre etiketi de budur.</summary>
    public required string Baslik { get; init; }

    /// <summary>Başlık görsel olarak gizli mi (eylem kolonu): ekran okuyucu yine okur.</summary>
    public bool BaslikGizli { get; init; }

    /// <summary>Hücre biçimi (Hucre şablonu yoksa Deger bu türe göre biçimlenir).</summary>
    public IzgaraHucreTuru Tur { get; init; } = IzgaraHucreTuru.Metin;

    /// <summary>Ham değer — Hucre şablonu verilmediyse Tur'a göre biçimlenip yazılır.</summary>
    public Func<T, object?>? Deger { get; init; }

    /// <summary>Hücre içeriği şablonu (rozet, satır içi düzenleme, eylemler…).</summary>
    public RenderFragment<T>? Hucre { get; init; }

    /// <summary>
    /// PAKET GOC-B (en küçük geriye uyumlu ekleme): başlık hücresinin id'si — göç edilen tablonun mevcut
    /// başlık id sözleşmesi korunsun diye (ör. "evrak-th-tur"). null = id yok (eski davranış).
    /// </summary>
    public string? BaslikId { get; init; }

    /// <summary>
    /// PAKET GOC-B: başlığın title'ı — kısa başlıkta TAM AD (ör. "Tür" → "Evrak türü"). null = klavye ipucu (eski davranış).
    /// </summary>
    public string? BaslikIpucu { get; init; }

    /// <summary>Başlık hücresinin ek sınıfı.</summary>
    public string? BaslikSinif { get; init; }

    /// <summary>Veri hücresinin sınıfı (satıra göre: eksi bakiye kırmızı vb.).</summary>
    public Func<T, string?>? HucreSinif { get; init; }

    /// <summary>
    /// Hücreye tıklanınca (satırı aç). Verilmezse hücrede tıklama işleyicisi YOKTUR.
    /// ⚠ EventCallback (Func DEĞİL): alıcısı ÇAĞIRAN sayfadır → tıklamadan sonra
    ///   sayfa yeniden çizilir (Func olsaydı yalnız ızgara çizilir, sayfanın açtığı
    ///   panel görünmezdi). Çağıran: EventCallback.Factory.Create&lt;T&gt;(this, Yontem).
    /// </summary>
    public EventCallback<T> HucreTikla { get; init; }

    /// <summary>Hücreye eklenecek öznitelikler (ör. data-duzenlenebilir).</summary>
    public IReadOnlyDictionary<string, object>? HucreOznitelikleri { get; init; }

    /// <summary>Gizlenemez (ad/ünvan, tutar, eylemler): panelde "Gizle" kilitli ve sebebi yazılı.</summary>
    public bool ZorunluGorunur { get; init; }

    /// <summary>Kullanıcının tercihi yokken görünür mü.</summary>
    public bool VarsayilanGorunur { get; init; } = true;

    /// <summary>
    /// Sunucu sıralama adı (ör. "tl-bakiye"). Yoksa başlıkta sıralama düğmesi ÇIKMAZ:
    /// sayfalı/sınırlı listede istemci yalnız görünen sayfayı sıralar ve yanıltır.
    /// </summary>
    public string? SunucuSiralamaAdi { get; init; }

    /// <summary>Taşınamaz: hep en sonda durur (satır eylemleri). Sırası tercihe yazılmaz.</summary>
    public bool Tasinamaz { get; init; }

    /// <summary>Kart düzeninde (dar ekran) hücre etiketi gösterilmesin (ad hücresi, eylemler).</summary>
    public bool KartEtiketsiz { get; init; }

    /// <summary>Para kolonunun para birimi (UX-S32 — tutar "varsayılan TL" sayılmaz).</summary>
    public Func<T, string?>? ParaBirimi { get; init; }

    /// <summary>Kolon listesinin saf tanımı (düzen motoru girdisi) — ızgara ve ön okuma AYNI yoldan.</summary>
    internal static IReadOnlyList<IzgaraKolonTanimi> Tanimlar(IEnumerable<IzgaraKolonu<T>> kolonlar) =>
        [.. kolonlar.Select(k => new IzgaraKolonTanimi(
            k.Anahtar, k.ZorunluGorunur, k.VarsayilanGorunur, k.Tasinamaz, k.SunucuSiralamaAdi is not null))];

    /// <summary>Tercihteki sıralamanın sunucu karşılığı (sıralanamayan/bilinmeyen kolon → null).</summary>
    internal static IzgaraSiralama? SiralamaKarsiligi(IEnumerable<IzgaraKolonu<T>> kolonlar, Kuyumcu.Core.Domain.IzgaraSiralamaTercihi? s)
    {
        if (s is null || kolonlar.FirstOrDefault(k => k.Anahtar == s.A) is not { SunucuSiralamaAdi: { } ad }) return null;
        return new IzgaraSiralama(s.A, ad, s.Y == Kuyumcu.Core.Domain.IzgaraTercihKurali.YonAzalan);
    }

    internal string BicimliDeger(T satir)
    {
        var v = Deger?.Invoke(satir);
        return (Tur, v) switch
        {
            (_, null) => "—",
            (IzgaraHucreTuru.Tl, decimal d) => Bicim.Tl(d),
            (IzgaraHucreTuru.Para, decimal d) => Bicim.Para(d, ParaBirimi?.Invoke(satir)),
            (IzgaraHucreTuru.Gram, decimal d) => Bicim.Gram(d),
            (IzgaraHucreTuru.Has, decimal d) => Bicim.Has(d),
            (IzgaraHucreTuru.Adet, decimal d) => Bicim.GunSayisi(d),
            (IzgaraHucreTuru.Adet, int i) => Bicim.GunSayisi(i),
            (IzgaraHucreTuru.Tarih, DateTime t) => Bicim.Gun(t),
            (IzgaraHucreTuru.Tarih, DateOnly t) => Bicim.Gun(t),
            _ => Convert.ToString(v, System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) ?? "—",
        };
    }
}

/// <summary>Izgaranın o anki sıralaması (sunucu adı + yön). null = varsayılan sıra.</summary>
public sealed record IzgaraSiralama(string KolonAnahtari, string SunucuAdi, bool Azalan)
{
    /// <summary>Liste ucuna eklenecek sorgu parçası: sirala=…&amp;yon=artan|azalan.</summary>
    public string Sorgu => $"sirala={Uri.EscapeDataString(SunucuAdi)}&yon={(Azalan ? "azalan" : "artan")}";
}
