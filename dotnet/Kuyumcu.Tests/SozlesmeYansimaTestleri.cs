using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// P-03 — SÖZLEŞME-1 (R-10): Kuyumcu.Contracts + tip/ad sözleşmesi
//
// NEDEN YANSIMA: istemci tipiyle sunucu DTO'su arasındaki alan adı / TİP kayması
// DERLEME HATASI VERMEZ. JSON ayrıştırıcısı eşleşmeyen alanı sessizce atlar,
// ekran `null`/0 gösterir (QA turu 2 dersi). Bu yüzden eşleşme çalışma zamanında
// yansımayla kilitlenir; kayma testi KIRMIZIYA döndürür.
//
// KAPSAM (dürüstlük notu, Anayasa md.1): bu testler P-03'ün sahiplendiği yüzeyi
// kilitler — `Kuyumcu.Contracts`, `Pages/`, `Bilesenler/`, `Layout/`.
// `Kuyumcu.Web/Servisler/` PLAN § 7'ye göre P-07'nin TEK EL alanıdır; oradaki
// 6 çift tanım (MasakGereksinim, PosCihazSatir, PosTahsilatSatir,
// PosTaksitOranSatir, PosValorSatir, SurumKunyesi) bu pakette DEVREDİLDİ olarak
// raporlanır, gizlenmez.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SozlesmeYansimaTestleri
{
    private static Assembly Sozlesmeler => typeof(Kuyumcu.Contracts.Fiyat.TopluSonuc).Assembly;
    private static Assembly Sunucu => typeof(Kuyumcu.Api.Modules.Fiyat.FiyatModulu).Assembly;
    private static Assembly Istemci => typeof(Kuyumcu.Web.Servisler.ApiIstemci).Assembly;

    /// <summary>Kuyumcu.sln'i barındıran `dotnet/` klasörü.</summary>
    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.True(d is not null, "Kuyumcu.sln bulunamadı — test kaynak ağacının dışında koşuyor.");
        return d!.FullName;
    }

    private static bool KayitTipi(Type t) =>
        t.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    private static List<(string Ad, string Tip)> Imza(Type t) =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
         .Where(p => p.Name != "EqualityContract")
         .Select(p => (p.Name, TipAdi(p.PropertyType)))
         .OrderBy(x => x.Name, StringComparer.Ordinal)
         .ToList();

    /// <summary>Koleksiyonun kabı (List / IReadOnlyList / dizi) sözleşme farkı değildir; ELEMANI farktır.</summary>
    private static string TipAdi(Type t)
    {
        var alt = Nullable.GetUnderlyingType(t);
        if (alt is not null) return TipAdi(alt) + "?";
        if (t.IsArray) return "koleksiyon<" + TipAdi(t.GetElementType()!) + ">";
        if (t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t) && t.IsGenericType)
            return "koleksiyon<" + TipAdi(t.GetGenericArguments()[0]) + ">";
        return t.Name;
    }

    // ── 1. Ortak tip GERÇEKTEN ortak mı: aynı ad iki ayrı tip olarak yaşamıyor ──
    [Fact]
    public void Contracts_tipi_sunucuda_ikinci_kez_tanimlanmiyor()
    {
        var sozlesme = Sozlesmeler.GetTypes().Where(KayitTipi).ToList();
        var sunucu = Sunucu.GetTypes().Where(KayitTipi)
            .Where(t => t.Namespace?.StartsWith("Kuyumcu.Api", StringComparison.Ordinal) == true)
            .ToList();

        var cakisan = (from s in sozlesme
                       join a in sunucu on s.Name equals a.Name
                       where s != a
                       select $"{s.FullName} ⇄ {a.FullName}").ToList();

        Assert.True(cakisan.Count == 0,
            "CONTRACTS_DISI_MODEL: aynı adın iki ayrı tanımı var (alan kayarsa derleme sessiz kalır, ekran null gösterir):\n  "
            + string.Join("\n  ", cakisan));
    }

    // ── 2. Toplu fiyat uygulaması: sunucu ile ekran BİREBİR aynı türü kullanıyor ──
    [Fact]
    public void Toplu_fiyat_yaniti_sunucuda_ve_ekranda_ayni_turdur()
    {
        var uc = typeof(Kuyumcu.Api.Modules.Fiyat.FiyatModulu)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(m => new[] { m.ReturnType }.Concat(m.GetParameters().Select(p => p.ParameterType)))
            .SelectMany(t => t.IsGenericType ? t.GetGenericArguments().Append(t) : new[] { t })
            .ToHashSet();

        Assert.Contains(typeof(Kuyumcu.Contracts.Fiyat.TopluSonuc), uc);
        // Tek tanım: ekranın okuduğu tip, sunucunun döndürdüğü tipin ta kendisidir.
        Assert.Equal("Kuyumcu.Contracts", typeof(Kuyumcu.Contracts.Fiyat.TopluSonuc).Assembly.GetName().Name);
        Assert.Equal(typeof(Kuyumcu.Contracts.Fiyat.TopluAtlanan),
            typeof(Kuyumcu.Contracts.Fiyat.TopluSonuc).GetProperty("Atlandi")!.PropertyType.GetGenericArguments()[0]);
        Assert.Equal(typeof(Kuyumcu.Contracts.Fiyat.TopluSatir),
            typeof(Kuyumcu.Contracts.Fiyat.TopluSonuc).GetProperty("Ornekler")!.PropertyType.GetGenericArguments()[0]);
    }

    // ── 3. Ad farklı, sözleşme aynı olmak ZORUNDA olan çiftler ────────────────
    //    (istemci tipi, sunucu DTO'su) — alan ADI ve TİPİ birebir eşleşir.
    public static TheoryData<Type, Type> Ciftler() => new()
    {
        { typeof(Kuyumcu.Contracts.Bilesenler.FiyatListesiUrunPaneli.Satir),
          typeof(Kuyumcu.Api.Modules.Fiyat.OnizlemeSatiri) },
        // PAKET P-27 (F-1 abonelik + F-3 plan kapısı): /abonelik ucunun üç
        // sözleşme tipi KİLİTSİZ BIRAKILMADI — ad ve tip yansımayla eşlenir.
        { typeof(Kuyumcu.Contracts.Sayfalar.Isletme.AbonelikVeri),
          typeof(Kuyumcu.Api.Modules.Abonelik.AbonelikDurumDto) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Isletme.AbonelikPlanVeri),
          typeof(Kuyumcu.Api.Modules.Abonelik.AbonelikPlanDto) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Isletme.AbonelikDonemVeri),
          typeof(Kuyumcu.Api.Modules.Abonelik.AbonelikDonemDto) },
        { typeof(Kuyumcu.Contracts.Bilesenler.FiyatListesiUrunPaneli.Sonuc),
          typeof(Kuyumcu.Api.Modules.Fiyat.OnizlemeSonucu) },
        // PAKET P-19 (Y-11/Y-12): /gider ekranının YEDİ ayrıştırma hedefi
        // KİLİTSİZ BIRAKILMADI (SozlesmeKapsamTestleri cırcırı yeni kilitsiz tip
        // kabul etmez). Sunucu artık anonim nesne DEĞİL bu kayıtları döndürür.
        { typeof(Kuyumcu.Contracts.Sayfalar.Gider.GiderKategoriVeri),
          typeof(Kuyumcu.Api.Modules.Gider.GiderKategoriCiktisi) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Gider.GiderKategoriYaniti),
          typeof(Kuyumcu.Api.Modules.Gider.GiderKategoriListesi) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Gider.GiderSatiri),
          typeof(Kuyumcu.Api.Modules.Gider.GiderSatirCiktisi) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Gider.GiderYaniti),
          typeof(Kuyumcu.Api.Modules.Gider.GiderListesi) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Gider.GiderSonucu),
          typeof(Kuyumcu.Api.Modules.Gider.GiderKaydiCiktisi) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Gider.VirmanSatiri),
          typeof(Kuyumcu.Api.Modules.Gider.VirmanSatirCiktisi) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Gider.VirmanYaniti),
          typeof(Kuyumcu.Api.Modules.Gider.VirmanListesi) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Gider.VirmanSonucu),
          typeof(Kuyumcu.Api.Modules.Gider.VirmanCiktisi) },
        { typeof(Kuyumcu.Contracts.Sayfalar.Gider.VirmanStornoSonucu),
          typeof(Kuyumcu.Api.Modules.Gider.VirmanStornoCiktisi) },
    };

    [Theory, MemberData(nameof(Ciftler))]
    public void Istemci_tipi_sunucu_dtosuyla_ad_ve_tip_olarak_eslesir(Type istemci, Type sunucu)
    {
        var i = Imza(istemci);
        var s = Imza(sunucu);
        // Koleksiyon elemanı ad olarak farklı olabilir (Satir ⇄ OnizlemeSatiri):
        // eleman tipleri bu testin kendisiyle ayrıca kilitlendiği için normalize edilir.
        // ⚠ PAKET P-27: aynı normalleştirme İÇ İÇE KAYIT tipleri için de gerekir
        //   (Donem alanı: AbonelikDonemVeri ⇄ AbonelikDonemDto). O çiftin
        //   kendisi bu tabloda AYRICA kilitlidir; burada yalnız istemci/sunucu
        //   son eki ("Veri"/"Dto") düşürülür — alan ADI ve ilkel TİPLER
        //   birebir karşılaştırılmaya DEVAM EDER.
        static List<(string, string)> Duz(List<(string Ad, string Tip)> x) =>
            x.Select(p => (p.Ad,
                Regex.Replace(
                    Regex.Replace(p.Tip, @"^koleksiyon<.*>$", "koleksiyon"),
                    @"(Veri|Dto)$", ""))).ToList();

        Assert.Equal(Duz(s), Duz(i));
    }

    // ── 4. Razor @code bloğunda yeni istemci record'u AÇILMIYOR ──────────────
    //    Kalan 3 istisna gerekçelidir; dördüncüsü testi kırar (PLAN § 1 md.9).
    [Fact]
    public void Razor_code_blogunda_istemci_record_tanimi_acilmiyor()
    {
        var web = Path.Combine(Kok(), "Kuyumcu.Web");
        var izinli = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Kuyumcu.Web/Servisler tipine (OturumKullanici) bağlı — Servisler P-07'nin tek elidir.
            ["AuthYaniti"] = "Giris.razor",
            // Kuyumcu.Web/Servisler/KarZararModelleri.cs tiplerine bağlı — P-07.
            ["OzetVeri"] = "KarZarar.razor",
            // Kuyumcu.Web.Servisler.SubeSatiri ile ad çakışması — P-07 birleştirecek.
            ["SubeSatiri"] = "Siparisler.razor",
        };
        var desen = new Regex(@"^\s*(private|public|internal)\s+(sealed\s+)?record\s+([A-Za-z0-9_]+)", RegexOptions.Multiline);
        var ihlal = new List<string>();
        foreach (var klasor in new[] { "Pages", "Bilesenler", "Layout" })
        {
            var dizin = Path.Combine(web, klasor);
            if (!Directory.Exists(dizin)) continue;
            foreach (var dosya in Directory.GetFiles(dizin, "*.razor"))
                foreach (Match m in desen.Matches(File.ReadAllText(dosya)))
                {
                    var ad = m.Groups[3].Value;
                    if (izinli.TryGetValue(ad, out var beklenen) && Path.GetFileName(dosya) == beklenen) continue;
                    ihlal.Add($"{klasor}/{Path.GetFileName(dosya)}: record {ad}");
                }
        }
        Assert.True(ihlal.Count == 0,
            "Yeni istemci record'u razor içine yazılmaz; Kuyumcu.Contracts'a yazılır (PLAN § 1 md.9):\n  "
            + string.Join("\n  ", ihlal));
    }

    // ── 5. Taşıma SAF YER DEĞİŞTİRMEDİR: taşınan tipler gerçekten Contracts'ta ──
    [Fact]
    public void Tasinan_tipler_Contracts_projesinde_ve_public()
    {
        var tipler = Sozlesmeler.GetTypes().Where(KayitTipi).ToList();
        Assert.True(tipler.Count >= 100, $"Kuyumcu.Contracts'ta beklenenden az kayıt tipi var: {tipler.Count}");
        Assert.All(tipler, t => Assert.True(t.IsPublic || t.IsNestedPublic, $"{t.FullName} public değil"));
        Assert.All(tipler, t => Assert.StartsWith("Kuyumcu.Contracts", t.Namespace ?? "", StringComparison.Ordinal));
        // Ekran derlemesinde artık @code record'u kalmadığının ikinci (yansıma) kanıtı:
        var ekranKayit = Istemci.GetTypes()
            .Where(t => KayitTipi(t) && t.DeclaringType is not null
                        && t.DeclaringType.Namespace?.StartsWith("Kuyumcu.Web.Pages", StringComparison.Ordinal) == true)
            .Select(t => t.DeclaringType!.Name + "." + t.Name)
            .ToList();
        Assert.True(ekranKayit.Count <= 3, "Pages bileşenlerinde beklenenden çok iç record var: " + string.Join(", ", ekranKayit));
    }
}
