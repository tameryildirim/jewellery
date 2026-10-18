using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Bilesenler.Yildiz;

/// <summary>PAKET YILDIZ-5A — Etiketler formundaki bir ürün (tuval "22 Ayar Burma Bilezik · 8698256000046").</summary>
/// <param name="Milyem">Etiketteki ayar ("916"); bilinmiyorsa ayar kodu.</param>
/// <param name="Gram">Gram ("15,20").</param>
public sealed record YildizEtiketUrunu(string Id, string Ad, string? Barkod, string Milyem, string Gram);

/// <summary>
/// PAKET YILDIZ-5A — Etiketler ekranının bağlamı (tuval Y_Etiketler): UrunEtiketleri.razor doldurur,
/// YildizEtiketler çizer. Bileşen İSTEK ATMAZ; seçim, adet, yazdırma ve vazgeç sayfanın MEVCUT yollarıdır.
/// </summary>
public sealed class YildizEtiketBaglami
{
    public string Ust { get; init; } = "";
    public string Baslik { get; init; } = "";
    /// <summary>Seçilebilir ürünler (sayfanın etkin ürün listesi).</summary>
    public IReadOnlyList<YildizEtiketUrunu> Urunler { get; init; } = [];
    /// <summary>Seçili ürünler, seçim sırasıyla değil liste sırasıyla.</summary>
    public IReadOnlyList<YildizEtiketUrunu> Secili { get; init; } = [];
    public EventCallback<string> Ekle { get; init; }
    public EventCallback<string> Cikar { get; init; }
    public int Kopya { get; init; }
    public int KopyaEnCok { get; init; }
    public EventCallback<decimal?> KopyaGirildi { get; init; }
    /// <summary>Adet alanının alan altı hata metni (sayfanın form durumu).</summary>
    public string? KopyaHata { get; init; }
    public EventCallback Yazdir { get; init; }
    /// <summary>Yazdır kilitliyse sebebi (UX-S30).</summary>
    public string? YazdirKilit { get; init; }
    public EventCallback Vazgec { get; init; }
    public bool Yukleniyor { get; init; }
    public RenderFragment? Bantlar { get; init; }
    public bool AyrintiAcik { get; init; }
    public EventCallback AyrintiAc { get; init; }
    public EventCallback AyrintiKapat { get; init; }
    /// <summary>D1-02 — şablon: boy ("50 × 30 mm"), alanlar özeti, fiyat alanı açık mı, eylemler.</summary>
    public string? BoyMetni { get; init; }
    public string? IcerikMetni { get; init; }
    public bool FiyatVar { get; init; }
    public string? FiyatHata { get; init; }
    public EventCallback SablonAc { get; init; }
    public EventCallback ZplIndir { get; init; }
}

/// <summary>
/// PAKET YILDIZ-5A — Sayım ekranının bağlamı (tuval Y_Sayim · "özet" kalıbı): Sayim.razor doldurur,
/// YildizSayim çizer. Bileşen İSTEK ATMAZ; düğmeler ve satırlar sayfanın MEVCUT geri çağrılarıdır.
/// </summary>
public sealed class YildizSayimBaglami
{
    public string Ust { get; init; } = "";
    public string Baslik { get; init; } = "";
    /// <summary>Büyük rakam (sayılan ürün); null = çizilmez (açık sayım yok).</summary>
    public int? Rakam { get; init; }
    /// <summary>Rakamın soluk eki (tuval " / 13 ürün").</summary>
    public string RakamEki { get; init; } = "";
    public string Cumle { get; init; } = "";
    public string? B1Etiket { get; init; }
    public EventCallback B1Tikla { get; init; }
    public string? B1Kilit { get; init; }
    public string? B2Etiket { get; init; }
    public EventCallback B2Tikla { get; init; }
    public IReadOnlyList<YildizSutun> Sutunlar { get; init; } = [];
    public IReadOnlyList<YildizListeSatiri> Satirlar { get; init; } = [];
    /// <summary>Sağdaki liste önceki sayımlardır (iki sütun ızgarası).</summary>
    public bool Gecmis { get; init; }
    public IReadOnlyList<(string Etiket, string Deger)> Bilgi { get; init; } = [];
    public bool Yukleniyor { get; init; }
    public string? BosMetni { get; init; }
    /// <summary>YILDIZ-5A tur 2: okutma alanları (sayfanın MEVCUT sy-barkod · sy-adet · hareketli onayı) cümlenin altında, tuval .k kalıbında.</summary>
    public bool OkutAcik { get; init; }
    public RenderFragment? Bantlar { get; init; }
    public bool AyrintiAcik { get; init; }
    /// <summary>"+ …" bağlantısının metni (boşsa "Tüm ayrıntılar").</summary>
    public string? AyrintiEtiketi { get; init; }
    public EventCallback AyrintiAc { get; init; }
    public EventCallback AyrintiKapat { get; init; }
}
