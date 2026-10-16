using Kuyumcu.Api.Common;
using MKur = Kuyumcu.Api.Modules.Kur;

namespace Kuyumcu.Api.Modules.Pano;

// ═════════════════════════════════════════════════════════════════════════════
// Vitrin panosu — fiyat hesabı. (Karar: PANO-KARAR-2026-09-11.md §1, §2, §7)
//
// Bu sınıf BİLİNÇLİ olarak saftır: veritabanı, ağ, saat okuma yok. Girdi
// verilir, çıktı alınır. Sebebi Mehmet'in gereksinimi: marj ticari sırdır ve
// asla panoya inmez — hesap SUNUCUDA biter, panoya yalnız sonuç gider. Saf
// olduğu için de marj/yuvarlama davranışı gerçek kur çekmeden test edilebilir.
//
// Faz 0 §11 mührü korunur: bu hesap ÜRÜN KARTINA dokunmaz. Pano kalemi bir stok
// kartı değildir; urunler.parite_kodu burada ne okunur ne yazılır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kuyumcunun bir pano satırı için yaptığı ayar (marj + yuvarlama).</summary>
public sealed record PanoKalemAyari
{
    /// <summary>Kanonik parite kodu — <see cref="MKur.PariteKodlari"/>.</summary>
    public required string PariteKodu { get; init; }

    /// <summary>Panodaki sıra (küçük olan üstte).</summary>
    public int Sira { get; init; }

    /// <summary>Kuyumcunun kendi yazdığı etiket; boşsa parite adı kullanılır.</summary>
    public string? Etiket { get; init; }

    /// <summary>
    /// Alış tarafına eklenen SABİT TL fark. Kuyumcu toptancıdan aşağı alacağı
    /// için genelde NEGATİFTİR — eksi değer kasıtlıdır, hata değildir.
    /// </summary>
    public decimal AlisMarjTl { get; init; }

    /// <summary>Satış tarafına eklenen sabit TL fark (genelde pozitif).</summary>
    public decimal SatisMarjTl { get; init; }

    /// <summary>Yuvarlama adımı: 0 (kapalı), 1, 5, 10, 50, 100. Varsayılan 10.</summary>
    public decimal YuvarlamaAdimi { get; init; } = PanoFiyatServisi.VarsayilanYuvarlama;

    /// <summary>Kapalıysa satır panoda hiç görünmez.</summary>
    public bool Aktif { get; init; } = true;
}

/// <summary>Panoya giden tek satır. Marj ve ham kotasyon BİLİNÇLİ olarak yok.</summary>
public sealed record PanoSatir(string Kod, string Etiket, decimal? Alis, decimal? Satis, string Birim);

/// <summary>Panoya çıkmayan kalem ve sebebi — yalnız yönetim ekranına gider.</summary>
public sealed record PanoAtlanan(string Kod, string Sebep);

/// <summary>
/// Hesap sonucu. <see cref="Satirlar"/> vitrine gider; <see cref="Atlananlar"/>
/// yalnız kuyumcunun yönetim ekranında görünür ki satırın neden kaybolduğunu
/// bilsin (sessizce yutmak Madde 1'e aykırı olurdu).
/// </summary>
public sealed record PanoSonuc(IReadOnlyList<PanoSatir> Satirlar, IReadOnlyList<PanoAtlanan> Atlananlar);

/// <summary>Bir kalemin panoya çıkmama sebepleri.</summary>
public static class AtlamaSebebi
{
    /// <summary>Kaynak bu kalemi hiç vermedi.</summary>
    public const string KotasyonYok = "KOTASYON_YOK";

    /// <summary>Kalem geldi ama alış da satış da boştu.</summary>
    public const string FiyatYok = "FIYAT_YOK";

    /// <summary>Alış ≥ satış. Vitrinde imkânsız; kaynak hatası ya da marj hatası.</summary>
    public const string TersMakas = "TERS_MAKAS";

    /// <summary>Satır tavanı doldu.</summary>
    public const string TavanAsildi = "TAVAN_ASILDI";

    /// <summary>
    /// P-38 · B-18: kotasyon GELDİ ama marj fiyatı sıfıra/altına indirdi.
    /// Eskiden bu satır ₺0,00 olarak VİTRİNE ÇIKIYORDU; artık çıkmaz.
    /// </summary>
    public const string MarjGecersiz = "MARJ_GECERSIZ";
}

public static class PanoFiyatServisi
{
    /// <summary>Varsayılan yuvarlama adımı (karar §2).</summary>
    public const decimal VarsayilanYuvarlama = 10m;

    /// <summary>Seçilebilir yuvarlama adımları; 0 = yuvarlama yok.</summary>
    public static readonly IReadOnlyList<decimal> YuvarlamaAdimlari = [0m, 1m, 5m, 10m, 50m, 100m];

    /// <summary>
    /// Panoda gösterilebilecek en fazla satır. Kaynak: Kuyumcular Odası'nın
    /// "Kuyumcu Ekranı" listesi 16 kalem içeriyor (PANO-KARAR §7, bulgu #5).
    /// </summary>
    public const int TavanSatir = 16;

    /// <summary>
    /// Bu sayının üstünde arayüz okunabilirlik uyarısı gösterir.
    /// DİKKAT: bu eşik ÖLÇÜLMEDİ — fiziksel LED panoların 6 satırda durmasından
    /// çıkarılmış tahmindir (PANO-KARAR §7). Pilotta gerçek TV'de doğrulanacak.
    /// </summary>
    public const int OkunabilirlikUyariEsigi = 12;

    /// <summary>
    /// Varsayılan pano kümesi — 7 kalem (PANO-KARAR §1, ölçüm §7 bulgu #3).
    /// Piyasada "dükkânda gösterilen temel ürün seti" diye adlandırılan kümenin
    /// birebir kendisidir; bizim tahminimiz değildir. USD/EUR kasıtlı olarak
    /// YOK — dükkân setinde geçmiyor, isteyen kuyumcu tek tıkla açar.
    /// </summary>
    public static readonly IReadOnlyList<string> VarsayilanKalemler =
    [
        MKur.PariteKodlari.Gram,
        MKur.PariteKodlari.CeyrekYeni,
        MKur.PariteKodlari.YarimYeni,
        MKur.PariteKodlari.TamYeni,
        MKur.PariteKodlari.AtaYeni,
        MKur.PariteKodlari.Bilezik22,
        MKur.PariteKodlari.Gumus,
    ];

    /// <summary>
    /// Marjın MUTLAK tavanı. Kotasyon bilinmediğinde tek dayanak budur; kotasyon
    /// varsa asıl kural <see cref="Dogrula(PanoKalemAyari, MKur.Parite?)"/>
    /// içindeki "fiyat sıfırın altına inemez" kuralıdır.
    /// </summary>
    public const decimal MarjMutlakTavan = 1_000_000m;

    /// <summary>Kalem ayarını doğrular; geçersizse kaydetmeden önce patlar.</summary>
    /// <param name="ayar">Kaydedilecek kalem ayarı.</param>
    /// <param name="kotasyon">
    /// P-38 · B-18 — KALEMİN CANLI KOTASYONU (biliniyorsa). QA turu 7 bulgu 112:
    /// −999.999 TL marj sunucudan <b>200 ile kabul ediliyordu</b>; mutlak tavan
    /// (1.000.000) bu değeri geçiriyor, hesap da fiyatı sessizce 0'a çekiyordu →
    /// müşteriye dönük panoda "gram altın alış ₺0,00". Ekran kapısı TEK kapı
    /// sayılmaz; son sözü sunucu söyler. Kotasyon biliniyorsa marj artık
    /// fiyatı sıfıra/altına indiremez ve alış ≥ satış yapamaz.
    /// Kotasyon yoksa (kaynak henüz çekmedi) yalnız mutlak tavan uygulanır —
    /// bu durum raporda açıkça yazılıdır, uydurma bir sınır konmaz.
    /// </param>
    public static void Dogrula(PanoKalemAyari ayar, MKur.Parite? kotasyon = null)
    {
        if (!MKur.PariteKodlari.Gecerli(ayar.PariteKodu))
            throw new BadRequestException($"Geçersiz parite kodu: {ayar.PariteKodu}");

        if (!YuvarlamaAdimlari.Contains(ayar.YuvarlamaAdimi))
            throw new BadRequestException(
                $"Geçersiz yuvarlama adımı: {ayar.YuvarlamaAdimi}. " +
                $"Seçilebilir: {string.Join(", ", YuvarlamaAdimlari)}");

        // Marja üst sınır koymuyoruz (kuyumcunun ticari kararı) ama saçma
        // büyüklükler yazım hatasıdır — pano yanlış fiyat göstermesin.
        if (Math.Abs(ayar.AlisMarjTl) > MarjMutlakTavan || Math.Abs(ayar.SatisMarjTl) > MarjMutlakTavan)
            throw new BadRequestException("Marj değeri makul aralığın dışında");

        if (kotasyon is null) return;

        // ── P-38 · B-18 — MARJ FİYATI SIFIRA İNDİREMEZ ─────────────────────
        // Kural kotasyona bağlıdır, uydurulmuş bir üst sınıra değil: panoya
        // basılacak fiyat pozitif olmak ZORUNDADIR. Kaydetmeden önce patlar ki
        // vitrin hiç bozulmasın (hesap tarafındaki MARJ_GECERSIZ ikinci kapıdır).
        var yeniAlis = kotasyon.Alis + ayar.AlisMarjTl;
        if (yeniAlis is { } ya && ya <= 0m)
            throw new BadRequestException(
                "Alış marjı fiyatı sıfıra ya da altına indiriyor: " +
                $"kotasyon {Yaz(kotasyon.Alis)} + marj {Yaz(ayar.AlisMarjTl)} = {Yaz(ya)}. " +
                "Vitrine ₺0,00 basılamaz; marjı küçültün.");

        var yeniSatis = kotasyon.Satis + ayar.SatisMarjTl;
        if (yeniSatis is { } ys && ys <= 0m)
            throw new BadRequestException(
                "Satış marjı fiyatı sıfıra ya da altına indiriyor: " +
                $"kotasyon {Yaz(kotasyon.Satis)} + marj {Yaz(ayar.SatisMarjTl)} = {Yaz(ys)}. " +
                "Vitrine ₺0,00 basılamaz; marjı küçültün.");

        // Ters makas KAYIT ANINDA da reddedilir: hesap tarafı bu satırı zaten
        // atlıyordu, ama kuyumcu ayarı kaydedip panonun neden boşaldığını
        // anlamıyordu (UX-S27 — sonuç görünür olmalı).
        if (yeniAlis is { } a2 && yeniSatis is { } s2 && a2 >= s2)
            throw new BadRequestException(
                $"Marjlar ters makas üretiyor: alış {Yaz(a2)} ≥ satış {Yaz(s2)}. " +
                "Bu satır vitrinde gösterilemezdi; marjları düzeltin.");
    }

    private static string Yaz(decimal? d) =>
        d is { } v ? v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "—";

    /// <summary>
    /// Kaynak kotasyonları + kuyumcunun ayarları → panoya gidecek satırlar.
    /// Kotasyonu gelmeyen kalem ATLANIR (ekranda boş satır bırakmaktansa hiç
    /// gösterme — karar §2 "bayat fiyat ekranda kalmaz" ilkesinin aynısı).
    /// Atlanan her kalem sebebiyle birlikte döner; sessizce yutulmaz.
    /// </summary>
    public static PanoSonuc Hesapla(
        IReadOnlyList<MKur.Parite> pariteler,
        IReadOnlyList<PanoKalemAyari> ayarlar)
    {
        var kaynak = new Dictionary<string, MKur.Parite>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in pariteler) kaynak[p.Kod] = p;   // son gelen kazanır

        var satirlar = new List<PanoSatir>();
        var atlananlar = new List<PanoAtlanan>();

        foreach (var ayar in ayarlar.Where(a => a.Aktif).OrderBy(a => a.Sira))
        {
            if (satirlar.Count >= TavanSatir)
            {
                atlananlar.Add(new PanoAtlanan(ayar.PariteKodu, AtlamaSebebi.TavanAsildi));
                continue;
            }

            if (!kaynak.TryGetValue(ayar.PariteKodu, out var p))
            {
                atlananlar.Add(new PanoAtlanan(ayar.PariteKodu, AtlamaSebebi.KotasyonYok));
                continue;
            }

            var alis = Uygula(p.Alis, ayar.AlisMarjTl, ayar.YuvarlamaAdimi, yukari: false);
            var satis = Uygula(p.Satis, ayar.SatisMarjTl, ayar.YuvarlamaAdimi, yukari: true);

            if (p.Alis is null && p.Satis is null)
            {
                atlananlar.Add(new PanoAtlanan(ayar.PariteKodu, AtlamaSebebi.FiyatYok));
                continue;
            }

            // ── P-38 · B-18 — ₺0,00 VİTRİNE ÇIKMAZ ─────────────────────────
            // Kotasyon geldiği hâlde marj fiyatı pozitif bırakmıyorsa satır
            // ATLANIR ve sebebi yönetim ekranında yazar. Eskiden `Uygula`
            // fiyatı sessizce 0'a çekiyordu: "gram altın alış ₺0,00" bulgusu
            // (QA turu 7 #112) tam olarak buradan doğuyordu.
            if ((p.Alis is not null && alis is null) || (p.Satis is not null && satis is null))
            {
                atlananlar.Add(new PanoAtlanan(ayar.PariteKodu, AtlamaSebebi.MarjGecersiz));
                continue;
            }

            // TERS MAKAS koruması. Vitrinde alış ≥ satış İMKÂNSIZDIR: müşteri
            // dükkâna satıp aynı anda ucuza geri alır. Gerçek vaka (11.09.2026,
            // üç ölçümde de aynı): Altınkaynak "Hamit Altın" alış 44.052,56 /
            // satış 43.250,00 yayınlıyor — kaynağın kendi verisi bozuk. Böyle
            // bir satır ekrana ÇIKMAZ; kuyumcu yönetim ekranında sebebini görür.
            if (alis is { } a && satis is { } s && a >= s)
            {
                atlananlar.Add(new PanoAtlanan(ayar.PariteKodu, AtlamaSebebi.TersMakas));
                continue;
            }

            var etiket = string.IsNullOrWhiteSpace(ayar.Etiket)
                ? MKur.PariteKodlari.Ad(ayar.PariteKodu)
                : ayar.Etiket.Trim();

            satirlar.Add(new PanoSatir(ayar.PariteKodu, etiket, alis, satis, p.Birim));
        }
        return new PanoSonuc(satirlar, atlananlar);
    }

    /// <summary>
    /// Kotasyona marjı ekler ve yuvarlar. Yön kuyumcunun lehinedir:
    /// satış YUKARI, alış AŞAĞI (karar §2).
    /// <para>
    /// P-38 · B-18: pozitif kalmayan fiyat artık <b>0'a çekilmez</b>, GEÇERSİZ
    /// sayılır (null). Çağıran `Hesapla` bu satırı MARJ_GECERSIZ sebebiyle
    /// atlar — vitrinde ₺0,00 diye bir fiyat olamaz.
    /// </para>
    /// </summary>
    private static decimal? Uygula(decimal? kotasyon, decimal marj, decimal adim, bool yukari)
    {
        if (kotasyon is not { } k) return null;
        var fiyat = k + marj;
        if (fiyat <= 0m) return null;

        if (adim <= 0m) return Math.Round(fiyat, 2, MidpointRounding.AwayFromZero);

        var bolum = fiyat / adim;
        var adimli = yukari ? Math.Ceiling(bolum) : Math.Floor(bolum);
        var sonuc = Math.Round(adimli * adim, 2, MidpointRounding.AwayFromZero);

        // Aşağı yuvarlama fiyatı sıfıra indirebilir (fiyat 3 ₺, adım 10 ₺).
        // ₺0,00 vitrine BASILMAZ — satır atlanır (B-18).
        return sonuc <= 0m ? null : sonuc;
    }
}
