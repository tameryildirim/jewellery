namespace Kuyumcu.Core.Domain;

/// <summary>
/// Kıymetli maden cinsi.
///
/// <para>
/// ⚠ NEDEN VAR: "has gram" bir <b>ALTIN</b> kavramıdır — 24 ayar saf altın
/// karşılığı demektir. 925 gümüşün "has gramı" ile 22 ayar altının has gramını
/// toplamak anlamsızdır; 1 gram gümüş ile 1 gram altın aynı şey değildir.
/// Bu alan gelmeden önce her ayar tek bir <c>HasGrami</c> alanına indiriliyor
/// ve raporlar bunları AYIRMADAN topluyordu. Artık maden bilgisi taşınıyor ve
/// toplamlar maden bazında kırılabiliyor.
/// </para>
/// </summary>
public static class Maden
{
    public const string Altin = "ALTIN";
    public const string Gumus = "GUMUS";
    public const string Platin = "PLATIN";
    public const string Paladyum = "PALADYUM";

    /// <summary>
    /// Paslanmaz çelik — KIYMETSİZ maden (paket 20).
    ///
    /// <para>
    /// ⚠ Çelik saat/bileklik vitrinde satılıyordu ama çeliğin AYARI yoktu:
    /// ürün K18 gibi bir ALTIN ayarıyla kaydedilmek zorunda kalıyor ve
    /// <c>has_grami</c> sahte üretiliyordu — 40 g çelik saat defterde
    /// 30 g has altın gibi görünüyordu. Çelikte has hesabı YOKTUR
    /// (<see cref="Ayar.HasHesaplanir"/> = false) ve ürün yalnız SABİT
    /// satış fiyatıyla satılır (K20-5).
    /// </para>
    /// </summary>
    public const string Celik = "CELIK";

    /// <summary>
    /// Rodyum — <b>STOK MADENİ DEĞİLDİR</b> (K20-7).
    ///
    /// <para>
    /// Rodyum beyaz altın/platin yüzeye vurulan KAPLAMADIR; gramı tartılmaz,
    /// has hesabına girmez, ayarı yoktur. Bu sabit yalnız etiket/vitrin
    /// ibaresi için vardır ve <see cref="Hepsi"/> dizisinde BİLEREK yoktur —
    /// aksi hâlde "rodyum stoğu" diye var olmayan bir bakiye doğardı.
    /// </para>
    /// </summary>
    public const string Rodyum = "RODYUM";

    /// <summary>
    /// Stok madenleri — ayarı olan, tartılan, defterde bacağı olan madenler.
    /// <see cref="Rodyum"/> BİLEREK YOKTUR (kaplamadır, stok değildir).
    /// </summary>
    public static readonly string[] Hepsi = [Altin, Gumus, Platin, Paladyum, Celik];

    /// <summary>
    /// Rodyum kaplama işareti bu madenlerde anlamlıdır. Çelik yüzeye kuyumcu
    /// rodyum vurmaz; gümüşte de ticari teamül yoktur (ayrı bir kaplama
    /// kavramıdır). Fail-closed: liste dışı madende işaret REDDEDİLİR.
    /// </summary>
    public static readonly string[] RodyumKaplanabilir = [Altin, Platin, Paladyum];

    /// <summary>Türkçe görünen ad.</summary>
    public static string Ad(string maden) => maden switch
    {
        Altin => "Altın",
        Gumus => "Gümüş",
        Platin => "Platin",
        Paladyum => "Paladyum",
        Celik => "Çelik",
        Rodyum => "Rodyum",
        _ => maden,
    };
}

/// <summary>
/// Maden ayarı — milyem: 1000 üzerinden saflık.
/// </summary>
/// <param name="Kiymetli">
/// Kıymetli maden mi? Kıymetsiz ayarda (çelik) ürün has×kur ile FİYATLANAMAZ;
/// sabit satış fiyatı ZORUNLUDUR (K20-5).
/// </param>
/// <param name="HasHesaplanir">
/// Bu ayarda has gram hesabı anlamlı mı? <c>false</c> ise
/// <see cref="HasHesap.ToHasGram"/> 0 döner — sahte has üretilmez.
/// </param>
public sealed record Ayar(
    string Id, string Ad, decimal Milyem, string Maden = Domain.Maden.Altin,
    bool Kiymetli = true, bool HasHesaplanir = true);

/// <summary>
/// Ayar tanımları. Altın ve gümüş değerleri TypeScript @kuyum/core ile birebir
/// aynıdır — değiştirilmedi. Platin ve paladyum 2026-09-14'te eklendi.
///
/// Has gram = (gramAğırlık − taşAğırlık) × milyem / 1000
/// </summary>
public static class Ayarlar
{
    public static readonly IReadOnlyDictionary<string, Ayar> Altin = new Dictionary<string, Ayar>
    {
        ["K8"]  = new("K8",  "8 Ayar",  333m),
        ["K14"] = new("K14", "14 Ayar", 585m),
        ["K18"] = new("K18", "18 Ayar", 750m),
        ["K21"] = new("K21", "21 Ayar", 875m),
        ["K22"] = new("K22", "22 Ayar", 916m),
        ["K24"] = new("K24", "24 Ayar", 1000m),
        // Ticari has: rafineri teslimlerinde 995 saflık standarttır ve
        // 24 ayarla (1000) karıştırılmamalıdır.
        ["K995"] = new("K995", "995 Ticari Has", 995m),
    };

    public static readonly IReadOnlyDictionary<string, Ayar> Gumus = new Dictionary<string, Ayar>
    {
        ["G800"] = new("G800", "800 Gümüş", 800m, Maden.Gumus),
        ["G925"] = new("G925", "925 Gümüş", 925m, Maden.Gumus),
        ["G950"] = new("G950", "950 Gümüş", 950m, Maden.Gumus),
        ["G999"] = new("G999", "999 Gümüş", 999m, Maden.Gumus),
    };

    /// <summary>Platin — mücevherde 950 ve 900 standarttır.</summary>
    public static readonly IReadOnlyDictionary<string, Ayar> Platin = new Dictionary<string, Ayar>
    {
        ["PT850"] = new("PT850", "850 Platin", 850m, Maden.Platin),
        ["PT900"] = new("PT900", "900 Platin", 900m, Maden.Platin),
        ["PT950"] = new("PT950", "950 Platin", 950m, Maden.Platin),
        ["PT999"] = new("PT999", "999 Platin", 999m, Maden.Platin),
    };

    /// <summary>Paladyum — beyaz altın alaşımında ve yatırımda kullanılır.</summary>
    public static readonly IReadOnlyDictionary<string, Ayar> Paladyum = new Dictionary<string, Ayar>
    {
        ["PD500"] = new("PD500", "500 Paladyum", 500m, Maden.Paladyum),
        ["PD950"] = new("PD950", "950 Paladyum", 950m, Maden.Paladyum),
        ["PD999"] = new("PD999", "999 Paladyum", 999m, Maden.Paladyum),
    };

    /// <summary>
    /// Çelik — KIYMETSİZ. Milyem 0, has hesaplanmaz.
    ///
    /// <para>
    /// ⚠ AÇIK SORU (paket 20, kurucuya): perakende kuyumcuda çelik saat/bileklik
    /// için 316L / 904L gibi KALİTE KODU ayrımı yapılıp yapılmadığı doğrulanmadı.
    /// Tahmin edilmedi (Anayasa md.1): tek bir <c>CELIK</c> kodu açıldı; kalite
    /// kodu gerekiyorsa ayarlar buraya EKLENİR ve desen/ekran kendiliğinden
    /// genişler (K20-8).
    /// </para>
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Ayar> Celik = new Dictionary<string, Ayar>
    {
        ["CELIK"] = new("CELIK", "Paslanmaz Çelik", 0m, Maden.Celik,
                        Kiymetli: false, HasHesaplanir: false),
    };

    /// <summary>Bütün madenlerin birleşik arama tablosu.</summary>
    public static readonly IReadOnlyDictionary<string, Ayar> Tumu =
        Altin.Concat(Gumus).Concat(Platin).Concat(Paladyum).Concat(Celik)
             .ToDictionary(kv => kv.Key, kv => kv.Value);

    public static Ayar? Bul(string ayarId) => Tumu.TryGetValue(ayarId, out var a) ? a : null;

    /// <summary>Bir ayarın madeni. Bilinmeyen ayarda null.</summary>
    public static string? MadenBul(string? ayarId) =>
        ayarId is not null && Tumu.TryGetValue(ayarId, out var a) ? a.Maden : null;

    /// <summary>Bir madenin ayarları — ürün kartı seçim listesi için.</summary>
    public static IEnumerable<Ayar> MadeninAyarlari(string maden) =>
        Tumu.Values.Where(a => a.Maden == maden).OrderBy(a => a.Milyem);

    /// <summary>
    /// EKRAN SEÇİCİLERİNİN TEK KAYNAĞI (UX-4c madde 10 · UX-S31).
    ///
    /// <para>
    /// ⚠ NEDEN VAR: ayar listesi ÜÇ ekranda elle üç kez yazılmıştı ve içerikleri
    /// FARKLIYDI — Emanet.razor'da K995 ve G999 vardı, Urunler.razor ile
    /// Islemler.razor'daki kopyalarda yoktu. Sonuç: müşteri 995 külçe bırakabiliyor
    /// ama aynı külçe ürün olarak kaydedilemiyor, hurda alışta seçilemiyordu.
    /// Artık üç ekran da buradan beslenir; yeni bir ayar eklenince üçü birden
    /// güncellenir.
    /// </para>
    /// <para>
    /// Platin/paladyum BİLEREK dışarıda: bu üç ekran (emanet · ürün kartı · POS
    /// hurda/takas) altın-gümüş iş akışıdır ve mevcut davranış korunur. Platin
    /// ürünler <see cref="MadeninAyarlari"/> ile ayrı beslenir.
    /// </para>
    /// </summary>
    public static readonly string[] EkranSecenekleri =
        [.. Altin.Keys.Concat(Gumus.Keys)];

    /// <summary>Doğrulama ifadelerinde kullanılan geçerli ayar listesi.</summary>
    public static readonly string[] GecerliIdler = [.. Tumu.Keys.OrderBy(x => x)];

    /// <summary>
    /// DTO doğrulaması için düzenli ifade deseni — <b>ÜRETİLİR, elle yazılmaz</b>
    /// (K20-8, paket 20).
    ///
    /// <para>
    /// ⚠ NEDEN DEĞİŞTİ: bu desen DÖRT dosyada elle kopyalanmıştı
    /// (UrunModulu ×2, EmanetModulu, SatisModulu) ve kaynağı da elle yazılı bir
    /// <c>const</c>'tu. Yeni bir ayar (çelik) eklendiğinde beş yerden biri
    /// unutulur ve "API'de var, ekranda/uçta yok" sapması doğardı — nitekim
    /// doğmuştu. Artık desen <see cref="Tumu"/> anahtarlarından üretilir;
    /// ayar eklemek TEK dosyada olur.
    /// </para>
    /// <para>
    /// Sıralama: UZUN anahtar ÖNCE. Düzenli ifade almaşıkları soldan sırayla
    /// denenir; "K2|K24" sırasında "K24" hiç eşleşmezdi. .NET'in
    /// <c>RegularExpressionAttribute</c>'u tam eşleşme arar ama
    /// <see cref="System.Text.RegularExpressions.Regex.IsMatch(string)"/>
    /// aramaz — bu yüzden desen doğrudan kullanılacaksa
    /// <see cref="DesenTam"/> tercih edilir.
    /// </para>
    /// </summary>
    public static readonly string Desen =
        string.Join('|', Tumu.Keys.OrderByDescending(k => k.Length).ThenBy(k => k, StringComparer.Ordinal));

    /// <summary>Çıpalı desen — <c>IsMatch</c> ile doğrudan kullanılabilir.</summary>
    public static readonly string DesenTam = $"^(?:{Desen})$";

    /// <summary>Ayar geçerli mi? Tek kaynak: <see cref="Tumu"/>.</summary>
    public static bool Gecerli(string? ayarId) =>
        ayarId is not null && Tumu.ContainsKey(ayarId);
}

/// <summary>Has gram hesapları.</summary>
public static class HasHesap
{
    /// <summary>
    /// Net gramı (taş düşülmüş) saf maden karşılığına çevirir.
    ///
    /// <para>
    /// ⚠ <b>KIYMETSİZ AYARDA 0 DÖNER</b> (K20-5, paket 20). Çelik saatin
    /// "has gramı" yoktur; milyem 0 olduğu için çarpım zaten 0 verirdi ama
    /// bu kontrol NİYETİ kodda görünür kılar: yarın çeliğe sıfırdan farklı bir
    /// milyem yazan biri, defterde sahte has altın üretmiş olmaz.
    /// </para>
    /// <para>
    /// PAKET PATCH-4 t2 (hasım ORTA): 6. hane yuvarlaması projenin TEK kuralına
    /// bağlandı — <c>MidpointRounding.AwayFromZero</c> (OzelMatrahMotoru.Yuvarla6 ile
    /// aynı). Eskiden varsayılan ToEven idi: stok has toplamı AwayFromZero, birim has
    /// ToEven ile yuvarlanıyor ve yarım sınırda (…5) 1 milyonda bir gram ayrışıyordu.
    /// </para>
    /// </summary>
    public static decimal ToHasGram(decimal netGram, Ayar ayar) =>
        ayar.HasHesaplanir ? Math.Round(netGram * ayar.Milyem / 1000m, 6, MidpointRounding.AwayFromZero) : 0m;

    /// <summary>Brüt gram + taş ağırlığından has gram.</summary>
    public static decimal HasGramHesapla(decimal gramAgirlik, decimal tasAgirlik, string ayarId)
    {
        var ayar = Bul(ayarId) ?? throw new ArgumentException($"Geçersiz ayar: {ayarId}", nameof(ayarId));
        return ToHasGram(gramAgirlik - tasAgirlik, ayar);
    }

    private static Ayar? Bul(string ayarId) => Ayarlar.Bul(ayarId);
}
