using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// P-03 — SÖZLEŞME KAPSAM SAYACI (R-10, hasım doğrulaması KRİTİK bulgusu)
//
// DÜRÜSTLÜK NOTU (Anayasa md.1): P-03'ün ilk turunda "sunucu DTO ⇄ istemci tip
// ad + TİP eşleşmesi yansımayla kilitlendi" denmişti; gerçekte 75 ayrıştırma
// hedefinin yalnız birkaçı kilitliydi. Bu dosya o oranı ÖLÇER ve dondurur.
//
// NEDEN TAM KAPSAM BU PAKETTE MÜMKÜN DEĞİL: sunucu uçlarının büyük bölümü
// `Task<object>` döndürür (anonim nesne). Anonim nesnenin yansımayla
// eşleştirilebileceği bir DTO TİPİ YOKTUR. Böyle uçlarda tek gerçek kilit
// çalışma zamanı JSON şekil testidir (SozlesmeSekilTestleri) ya da sunucunun
// Contracts tipini döndürmeye geçirilmesidir — ikincisi sunucu modüllerini
// açar, P-03'ün "saf yer değiştirme" DoD'unu aşar → P-07'ye DEVREDİLDİ.
//
// MEKANİZMA: aşağıdaki KilitsizBeklenen listesi bir CIRCIRDIR.
//   • Listeye yeni ad eklenemez (yeni kilitsiz tip = test KIRMIZI).
//   • Listeden ad silmek için o tipin gerçekten kilitlenmesi gerekir.
//   • denetim.sh `SOZLESME_KILITSIZ_TIP` sayacı bu listenin uzunluğunu okur
//     ve tavanla karşılaştırır; tavan yalnız AZALIR.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SozlesmeKapsamTestleri
{
    private static Assembly Sunucu => typeof(Kuyumcu.Api.Modules.Fiyat.FiyatModulu).Assembly;
    private static Assembly Sozlesmeler => typeof(Kuyumcu.Contracts.Fiyat.TopluSonuc).Assembly;
    private static Assembly Cekirdek => typeof(Kuyumcu.Core.Sozlesmeler.AcilisProvaYaniti).Assembly;

    /// <summary>Tip adı olmayan / sözleşmesi bulunmayan ayrıştırma hedefleri.</summary>
    private static readonly HashSet<string> Yoksayilan = new(StringComparer.Ordinal)
    { "JsonElement", "object", "List", "string", "bool", "int", "decimal" };

    // ── CIRCIR: bugün hâlâ kilitsiz olan ayrıştırma hedefleri ────────────────
    // Ölçüm komutu (taban belgeden kopyalanmadı, testin kendisi ölçer):
    //   grep -rhoE "Api\.(Get|Post|Patch|Put|Delete)Async<[A-Za-z0-9_.]+" --include='*.razor' \
    //     Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler | sed -E 's/.*<//' | sed -E 's/.*\.//' | sort -u
    public static readonly string[] KilitsizBeklenen = KilitsizListesi();

    private static string[] KilitsizListesi() => new[]
    {
        "AcilisGeriAlYaniti",
        "AcilisProvaYaniti",
        "AcilisUygulaYaniti",
        "AuthYaniti",
        "AylikYanit",
        "CariDetayVeri",
        "Detay",
        "EkstreDetayYaniti",
        "EkstreOnayYaniti",
        "EkstreSatirYaniti",
        "EkstreYuklemeYaniti",
        "EmanetYaniti",
        "Esikler",
        "EtiketVeri",
        "FaturaYaniti",
        "FaturalanabilirYanit",
        "FisYaniti",
        "GecmisYanit",
        "GunlukOzet",
        "HareketYaniti",
        "IceAktarimGecmisYaniti",
        "IceAktarimGeriAlYaniti",
        "IceAktarimProvaYaniti",
        "IceAktarimSablonYaniti",
        "IceAktarimUygulaYaniti",
        "IsYaniti",
        "KalemYaniti",
        "KapatYaniti",
        "Kart",
        "KaynakDurumu",
        // "KaynakSonuc" — KESIF-P6 tur 3: Panel kur kaynağı PATCH yolu kaldırıldı (kaynak yalnız İşletme'de), hedef yok.
        "KaynakYaniti",
        "KosuYanit",
        "KurAnlik",
        "KurSatiri",
        "MadenTablosuYaniti",
        "OzelMatrahOnizleme",
        "Ozet",
        "OzetVeri",
        "PariteYaniti",
        "ReyonIcerikYaniti",
        "ReyonStokYaniti",
        "Sayfa",
        "StokVeri",
        "StokYaniti",
        "SubeVeri",
        "TalepYanit",
        "TatbikatYanit",
        "Teklif",
        "TevsikOnizleme",
        "UrunTamKart",
        "UrunVeri",
        "UrunYanit",
        "UrunYerlesimYaniti",
        "VezneAyarYaniti",
        "VezneDetayYaniti",
        "VezneKurSatiri",
        "VezneKuyrukYaniti",
        "Yanit",
    };

    private static string Kok()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.True(d is not null, "Kuyumcu.sln bulunamadı.");
        return d!.FullName;
    }

    /// <summary>Razor dosyalarındaki `Api.XAsync&lt;T&gt;` ayrıştırma hedeflerinin KISA adları.</summary>
    internal static List<string> AyristirmaHedefleri()
    {
        var web = Path.Combine(Kok(), "Kuyumcu.Web");
        var desen = new Regex(@"Api\.(?:Get|Post|Patch|Put|Delete)Async<([A-Za-z0-9_.]+)");
        var adlar = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var klasor in new[] { "Pages", "Bilesenler", "Layout" })
        {
            var dizin = Path.Combine(web, klasor);
            if (!Directory.Exists(dizin)) continue;
            foreach (var dosya in Directory.GetFiles(dizin, "*.razor"))
                foreach (Match m in desen.Matches(File.ReadAllText(dosya)))
                {
                    var tam = m.Groups[1].Value;
                    var kisa = tam.Contains('.') ? tam[(tam.LastIndexOf('.') + 1)..] : tam;
                    if (!Yoksayilan.Contains(kisa)) adlar.Add(kisa);
                }
        }
        return adlar.ToList();
    }

    /// <summary>Sunucunun İMZALARINDA geçen tipler: anonim (`Task&lt;object&gt;`) uçlar buraya GİRMEZ.</summary>
    private static HashSet<Type> SunucuYuzeyi()
    {
        var küme = new HashSet<Type>();
        void Ekle(Type? t, int derinlik)
        {
            if (t is null || derinlik > 3 || !küme.Add(t)) return;
            if (t.IsGenericType) foreach (var g in t.GetGenericArguments()) Ekle(g, derinlik + 1);
            if (t.IsArray) Ekle(t.GetElementType(), derinlik + 1);
            if (t.Assembly == Sozlesmeler || t.Assembly == Cekirdek)
                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    Ekle(p.PropertyType, derinlik + 1);
        }

        foreach (var tip in Sunucu.GetTypes())
        {
            if (tip.Namespace?.StartsWith("Kuyumcu.Api", StringComparison.Ordinal) != true) continue;
            foreach (var m in tip.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                             | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Ekle(m.ReturnType, 0);
                foreach (var p in m.GetParameters()) Ekle(p.ParameterType, 0);
            }
        }
        return küme;
    }

    /// <summary>Kısa ada karşılık gelen tüm sözleşme tipleri (aynı ad birden çok ad alanında olabilir).</summary>
    private static List<Type> Coz(string kisaAd) =>
        Sozlesmeler.GetTypes().Concat(Cekirdek.GetTypes())
            .Where(t => t.Name == kisaAd && (t.IsPublic || t.IsNestedPublic))
            .ToList();

    /// <summary>Kilitsiz = sunucu imza yüzeyinde HİÇ görünmeyen (ya da hiç çözülemeyen) ayrıştırma hedefi.</summary>
    internal static List<string> KilitsizOlcum()
    {
        var yuzey = SunucuYuzeyi();
        var sekil = SozlesmeSekilTestleri.SekilKilitliTipler.ToHashSet();
        var cift = SozlesmeYansimaTestleri.Ciftler().Select(x => (Type)x[0]).ToHashSet();
        var sonuc = new List<string>();
        foreach (var ad in AyristirmaHedefleri())
        {
            var tipler = Coz(ad);
            // Kilitli sayılmak için AYNI ADI taşıyan TÜM tipler kilitli olmalı:
            //   • sunucu aynı tipi imzasında kullanıyor (tek tanım → derleyici kilidi), ya da
            //   • SozlesmeSekilTestleri canlı JSON şekliyle kilitliyor, ya da
            //   • SozlesmeYansimaTestleri.Ciftler() tablosunda yansımayla eşleniyor.
            if (tipler.Count > 0 && tipler.All(t => yuzey.Contains(t) || sekil.Contains(t) || cift.Contains(t)))
                continue;
            sonuc.Add(ad);
        }
        return sonuc;
    }

    // ── 1. CIRCIR: kilitsiz tip listesi birebir donduruldu ───────────────────
    [Fact]
    public void Kilitsiz_tip_listesi_circir_ile_donduruldu()
    {
        var olculen = KilitsizOlcum();
        var beklenen = KilitsizBeklenen.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var yeni = olculen.Except(beklenen, StringComparer.Ordinal).ToList();
        var kapanan = beklenen.Except(olculen, StringComparer.Ordinal).ToList();

        Assert.True(yeni.Count == 0,
            "SOZLESME_KILITSIZ_TIP CIRCIRI KIRILDI — sunucu DTO'suyla kilitlenmemiş YENİ istemci tipi:\n  "
            + string.Join("\n  ", yeni)
            + "\n(Yeni uç önce Kuyumcu.Contracts tipini alır; sunucu `Task<object>` yerine o tipi döndürür.)");

        Assert.True(kapanan.Count == 0,
            "Bu tipler artık kilitli — listeden SİLİN ve denetim.sh tavanını düşürün:\n  "
            + string.Join("\n  ", kapanan));
    }

    // ── 2. Ölçülen oran raporla tutarlı: kilitli/toplam ──────────────────────
    [Fact]
    public void Kapsam_orani_beyan_edilen_degerle_ayni()
    {
        var toplam = AyristirmaHedefleri().Count;
        var kilitsiz = KilitsizOlcum().Count;
        Assert.True(toplam > 0, "Ayrıştırma hedefi bulunamadı — regex ya da klasör yolu bozulmuş.");
        Assert.Equal(KilitsizBeklenen.Length, kilitsiz);
        Assert.True(kilitsiz < toplam, "Hiçbir tip kilitli değil — kilit mekanizması çalışmıyor.");
    }
}
