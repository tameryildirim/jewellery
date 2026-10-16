using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P8 — SÖZLÜK · HAM KOD · JARGON TARAMASI [saf + bUnit DilServisi]
//
//  P8_01  BASİT TR: bütün metin yapraklarında (yansımayla gezilir) cari/pasif/
//         sunucu/storno/append-only (sözcük başı, büyük/küçük harf duyarsız) ve
//         BÜYÜK_ALT_ÇİZGİ ham kod 0.
//  P8_02  PRO TR + EN: BÜYÜK_ALT_ÇİZGİ 0 (PRO'da "Cari" KALIR — D10).
//  P8_03  BASİT EN: BÜYÜK_ALT_ÇİZGİ + storno/append-only 0.
//  P8_04  Karşılık listesi: her BASİT değer PRO'dan farklı, PRO değer gerçekten
//         taramaya takılıyor (liste boşuna büyümez), {0} yer tutucuları aynı,
//         aynı yol iki listede yok.
//  P8_05  Bulgu metinleri (#21 #33 #82 + devirler RFID 'K-21', fiyat paneli 'Gram').
//  P8_06  DilServisi basit modda karşılığı, PRO modda HUB metnini verir (KAT_K8_03 kalıbı).
//  P8_07  Razor cırcırı: görünür metinde doğrudan ayar kodu basımı (title/value/Deger
//         özniteliği ve AyarAdi(...) dışı) yalnız azalabilir.
//
// İstisna listesi YALNIZ gerekçeli ekip kararıyla açılır (bugün BOŞ). Karar 1
// (KESIF-KARARLAR 30.09): basılı belgenin resmî adı "Cari Hesap Ekstresi" API'den
// gelir (CiktiModulu), sözlükte değildir → tarama kapsamı dışında, ekran metni "müşteri".
//
// Mutasyonlar (rapor § mutasyon):
//  M8-1 KesifP8BasitSozlugu Kasa.PasifeAl = "Pasife Al"        → P8_01, P8_04, P8_06 KIRMIZI
//  M8-2 HUB Sayim.KapatOnay geri "SAYIM_DUZELTME"               → P8_01, P8_02, P8_05 KIRMIZI
//  M8-3 HUB RfidIpucu geri "(K-21)"                             → P8_05 + P10 RFID testi KIRMIZI
//  M8-4 Kur() içinde P8 katman satırı yorumlanır                → P8_01, P8_06 KIRMIZI
//  M8-5 razor'a görünür @x.AyarId basımı eklenir                → P8_07 KIRMIZI
//  M8-6 HUB Fiyat.Gram geri "Gram"                              → P8_05 KIRMIZI
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KesifSozlukTaramaTestleri : BunitContext
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <summary>Sözcük BAŞI sınırı: "pasife", "cariye", "sunucudan", "stornolandı" da yakalanır.
    /// Metin önce tr-TR küçük harfe çevrilir (I→ı, İ→i) → iki i de sınıfta.</summary>
    private static readonly Regex Yasak = new(@"(?<![\p{L}\p{N}_])(car[iı]|pas[iı]f|sunucu|storno|append-only)");

    /// <summary>BÜYÜK_ALT_ÇİZGİ ham kod (triyaj kalıbı + Türkçe büyük harfler).</summary>
    private static readonly Regex Ham = new(@"(?<![\p{L}\p{N}_])[A-ZÇĞİÖŞÜ]{2,}(_[A-ZÇĞİÖŞÜ0-9]+)+(?![\p{L}\p{N}_])");

    private static readonly Regex YerTutucu = new(@"\{\d+(:[^}]*)?\}");

    /// <summary>Gerekçeli ekip kararıyla açılan istisnalar (yol → gerekçe). Bugün BOŞ.</summary>
    private static readonly Dictionary<string, string> Istisna = new();

    public KesifSozlukTaramaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    /// <summary>Sözlük ağacının bütün metin yaprakları (string özellik, sözlük değeri, liste öğesi).</summary>
    internal static List<(string Yol, string Deger)> Yapraklar(object kok)
    {
        var gorulen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var sonuc = new List<(string, string)>();
        void Gez(object o, string yol)
        {
            if (o is string s) { sonuc.Add((yol, s)); return; }
            if (!gorulen.Add(o)) return;
            if (o is IDictionary d)
            {
                foreach (DictionaryEntry e in d) if (e.Value != null) Gez(e.Value, $"{yol}[{e.Key}]");
                return;
            }
            if (o is IEnumerable en)
            {
                var i = 0;
                foreach (var x in en) { if (x != null) Gez(x, $"{yol}[{i}]"); i++; }
                return;
            }
            var t = o.GetType();
            if (t.IsPrimitive || t.IsEnum || t.Namespace?.StartsWith("Kuyumcu", StringComparison.Ordinal) != true) return;
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length > 0 || p.PropertyType.IsSubclassOf(typeof(Delegate))) continue;
                var v = p.GetValue(o);
                if (v != null) Gez(v, yol.Length == 0 ? p.Name : yol + "." + p.Name);
            }
        }
        Gez(kok, "");
        return sonuc;
    }

    private static bool YasakVar(string metin) => Yasak.IsMatch(metin.ToLower(Tr));

    private static string Rapor(IEnumerable<(string Yol, string Deger)> ihlal) =>
        string.Join("\n", ihlal.Select(x => $"  {x.Yol} = {x.Deger}"));

    // ── P8_01 BASİT TR ───────────────────────────────────────────────────────
    [Fact]
    public void P8_01_BASIT_TR_sozlukte_cari_pasif_sunucu_storno_append_only_ve_ham_kod_0()
    {
        var yapraklar = Yapraklar(BasitSozluk.Getir("tr"));
        Assert.True(yapraklar.Count > 2500, $"yaprak sayısı şüpheli az: {yapraklar.Count} (gezgin bozuldu mu?)");
        var ihlal = yapraklar
            .Where(x => !Istisna.ContainsKey(x.Yol) && (YasakVar(x.Deger) || Ham.IsMatch(x.Deger)))
            .ToList();
        Assert.True(ihlal.Count == 0, $"BASİT TR jargon/ham kod ({ihlal.Count}):\n{Rapor(ihlal)}");
    }

    // ── P8_02 PRO TR + EN ────────────────────────────────────────────────────
    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public void P8_02_PRO_sozlukte_BUYUK_ALT_CIZGI_ham_kod_0(string dil)
    {
        var ihlal = Yapraklar(Sozlukler.Getir(dil)).Where(x => Ham.IsMatch(x.Deger)).ToList();
        Assert.True(ihlal.Count == 0, $"PRO {dil} ham kod ({ihlal.Count}):\n{Rapor(ihlal)}");
        // #87 (tur 2): "append-only" PRO'da da yasak — iki dilde düz cümle (karar 2: BASİT ve PRO).
        var ingilizceJargon = Yapraklar(Sozlukler.Getir(dil))
            .Where(x => x.Deger.Contains("append-only", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(ingilizceJargon.Count == 0, $"PRO {dil} append-only ({ingilizceJargon.Count}):\n{Rapor(ingilizceJargon)}");
        // D10: PRO muhasebe dili korunur.
        if (dil == "tr")
        {
            Assert.Equal("Cariler", Sozlukler.Getir("tr").Cari.Baslik);
            Assert.Equal("Pasife Al", Sozlukler.Getir("tr").Kasa.PasifeAl);
        }
    }

    // ── P8_03 BASİT EN ───────────────────────────────────────────────────────
    [Fact]
    public void P8_03_BASIT_EN_sozlukte_ham_kod_storno_append_only_0()
    {
        var enYasak = new Regex(@"(?<![\p{L}\p{N}_])(storno|append-only)", RegexOptions.IgnoreCase);
        var ihlal = Yapraklar(BasitSozluk.Getir("en"))
            .Where(x => Ham.IsMatch(x.Deger) || enYasak.IsMatch(x.Deger)).ToList();
        Assert.True(ihlal.Count == 0, $"BASİT EN ({ihlal.Count}):\n{Rapor(ihlal)}");
    }

    // ── P8_04 karşılık listesinin sözleşmesi ─────────────────────────────────
    [Fact]
    public void P8_04_karsilik_listesi_PROdan_farkli_yer_tutucu_ayni_cift_yol_yok()
    {
        var satirlar = KesifP8BasitSozlugu.Satirlar;
        Assert.True(satirlar.Count >= 130, $"karşılık sayısı {satirlar.Count}");
        Assert.Equal(satirlar.Count, KesifP8BasitSozlugu.AnahtarSayisi);

        var proTr = Yapraklar(Sozlukler.Getir("tr")).ToDictionary(x => x.Yol, x => x.Deger);
        var proEn = Yapraklar(Sozlukler.Getir("en")).ToDictionary(x => x.Yol, x => x.Deger);
        foreach (var (yol, tr, en) in satirlar)
        {
            Assert.True(proTr.ContainsKey(yol), $"{yol} PRO sözlükte yok");
            Assert.False(YasakVar(tr) || Ham.IsMatch(tr), $"{yol} BASİT TR jargon taşıyor: {tr}");
            if (!Birlestirme.Contains(yol))
                Assert.True(YasakVar(proTr[yol]) || Ham.IsMatch(proTr[yol]),
                $"{yol} PRO metni taramaya takılmıyor — gereksiz karşılık: {proTr[yol]}");
            Assert.NotEqual(proTr[yol], tr);
            Assert.False(string.IsNullOrWhiteSpace(en), $"{yol} EN boş");
            Assert.Equal(YerTutucular(proTr[yol]), YerTutucular(tr));
            Assert.Equal(YerTutucular(proEn[yol]), YerTutucular(en));
        }

        // Aynı yol iki katman listesinde olmaz (son yazan kazanır → sessiz çakışma).
        Assert.Equal(satirlar.Count, satirlar.Select(x => x.Yol).Distinct().Count());
        var digerleri = KesifP8BasitSozlugu.DigerKatmanYollari.ToHashSet();
        Assert.True(digerleri.Count >= 50, $"diğer katman yolu {digerleri.Count}");
        var cift = satirlar.Select(x => x.Yol).Where(digerleri.Contains).ToList();
        Assert.True(cift.Count == 0, "iki listede birden: " + string.Join(", ", cift));
    }

    /// <summary>Triyaj "Birleştirme önerisi" maddeleri: PRO metni taramaya takılmaz ama tezgâhtara
    /// teknik (TOPLU ZO-BELGE "CSV"/"Hata say", TOPLU SATIS-3 BASİT MASAK jargonu).</summary>
    private static readonly HashSet<string> Birlestirme =
    [
        "IceAktarim.DosyaSec", "IceAktarim.PolitikaHata",
        "Islem.MasakBaslik", "Islem.MasakAciklama", "Islem.MasakKademeAmac",
        // #87 tur 2: PRO metni artık "append-only" taşımıyor (P8_02); BASİT karşılık daha kısa cümle olarak kalır.
        "Uyum.ProfilDegismezNot", "Siparis.AppendOnly",
    ];

    private static string YerTutucular(string s) =>
        string.Join(",", YerTutucu.Matches(s).Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal));

    // ── P8_05 bulgu metinleri + devirler ─────────────────────────────────────
    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public void P8_05_bulgu_metinleri_iki_modda_ham_kodsuz_ve_devirler_kapandi(string dil)
    {
        foreach (var s in new[] { Sozlukler.Getir(dil), BasitSozluk.Getir(dil) })
        {
            // #21 #82 #33: ham hareket kodu yok.
            Assert.DoesNotContain("CARI_DUZELTME", s.Cari.DuzeltmeIpucu);
            Assert.DoesNotContain("SAYIM_DUZELTME", s.Urun.SayimIpucu);
            Assert.DoesNotContain("SAYIM_DUZELTME", s.Sayim.KapatOnay);
            Assert.DoesNotContain("EMANET_GIRIS", s.Siparis.EmanetUyarisi);
            Assert.DoesNotMatch(Ham, s.Urun.PariteKoduIpucu);
            // Devir KESIF-P4: RFID ipucunda iç kalem kodu yok, düz cümle var.
            Assert.DoesNotContain("K-21", s.Urun.RfidIpucu);
            Assert.DoesNotContain("DIŞINDADIR", s.Urun.RfidIpucu);
            // #17 #35 metin: "sunucu" yok.
            Assert.DoesNotContain("sunucu", s.Vezne.KurTahsilAninda, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("server", s.Vezne.KurTahsilAninda, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sunucu", s.Siparis.TeklifAl, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("server", s.Siparis.TeklifAl, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sunucu", s.Siparis.TeklifKaynak, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("server", s.Siparis.TeklifKaynak, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sunucu", s.Kabuk.SiraKaydedilemedi, StringComparison.OrdinalIgnoreCase);
        }
        var pro = Sozlukler.Getir(dil);
        // Devir KESIF-P1: fiyat panelinin sütunu has gram gösterir, başlık da öyle der.
        Assert.Equal(dil == "en" ? "Fine grams" : "Has gram", pro.Fiyat.Gram);
        // #87 tur 2 (canlı ölçümde bulundu): denetim izi kayıt türü ham sınıf adı "CariRiskProfili" basmaz.
        foreach (var s in new[] { pro, BasitSozluk.Getir(dil) })
            Assert.Equal(dil == "en" ? "Risk profile" : "Risk profili", s.SayfaDuzeltme.VarlikAdi("CariRiskProfili"));
        Assert.Contains(dil == "en" ? "no RFID reader connection" : "RFID okuyucu bağlantısı yoktur", pro.Urun.RfidIpucu);
    }

    // ── P8_06 DilServisi (KAT_K8_03 kalıbı) ──────────────────────────────────
    [Fact]
    public void P8_06_DilServisi_basit_modda_P8_karsiligini_proda_HUB_metnini_verir()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        var basit = true;
        dil.BasitModuBagla(() => basit);
        Assert.Equal("Kullanımdan kaldır", dil.T.Kasa.PasifeAl);            // #74
        Assert.Equal("Kullanımdan kaldır", dil.T.Urun.PasifeAl);            // #83
        Assert.Equal("Müşteri", dil.T.Kurulum.TurCari);                     // #76
        Assert.Equal("Malı bize satan kişi ya da firma. Konsinye sahibinden farklıdır.", dil.T.Urun.TedarikciIpucu); // #81
        Assert.DoesNotContain("cari", dil.T.Cari.UnvanIpucu, StringComparison.OrdinalIgnoreCase);              // #18
        Assert.DoesNotContain("cari", dil.T.Cari.DovizFisBacagiUyari, StringComparison.OrdinalIgnoreCase);     // #18
        Assert.DoesNotContain("storno", dil.T.Duzeltme.StornoHenuzYok, StringComparison.OrdinalIgnoreCase);    // #25
        Assert.DoesNotContain("maliyet partisi", dil.T.Duzeltme.StornoHenuzYok);                               // #25
        Assert.DoesNotContain("append-only", dil.T.Uyum.ProfilDegismezNot, StringComparison.OrdinalIgnoreCase); // #87
        Assert.Contains("müşteri hesap dökümü", dil.T.Cikti.Aciklama);       // #50 #52 (karar 1)
        Assert.DoesNotContain("cari", dil.T.Cikti.KaynaktaDuzelt, StringComparison.OrdinalIgnoreCase);

        basit = false;
        Assert.Equal("Pasife Al", dil.T.Kasa.PasifeAl);                     // PRO değişmez (D10)
        Assert.Equal("Malı bize satan cari. Konsinye sahibinden farklıdır.", dil.T.Urun.TedarikciIpucu);
        Assert.DoesNotContain("append-only", dil.T.Siparis.AppendOnly, StringComparison.OrdinalIgnoreCase); // #87 tur 2: PRO da düz cümle
        Assert.Contains("yalnız eklenen", dil.T.Siparis.AppendOnly);
        Assert.DoesNotContain("append-only", dil.T.Uyum.ProfilDegismezNot, StringComparison.OrdinalIgnoreCase); // #87 PRO
        Assert.DoesNotContain("ezme", dil.T.Uyum.ProfilDegismezNot);
        Assert.StartsWith("Risk profili yalnız eklenen", dil.T.Uyum.ProfilDegismezNot);
    }

    // ── P8_07 Razor cırcırı ──────────────────────────────────────────────────
    /// <summary>
    /// Görünür metinde ayar kodunun DOĞRUDAN basımı: <c>@x.AyarId</c> ya da
    /// <c>@(… x.AyarId …)</c>; öznitelik değeri (<c>title="@x.AyarId"</c>, value, Deger —
    /// karar 2: ham değer yalnız title'da) ve <c>KurKaynakAdi.AyarAdi(…)</c> sayılmaz.
    /// Taban (01.10): 1 yer — SiparisDetay.razor kalem tablosu (sahibi KESIF-P6/sipariş
    /// detay). Yalnız AZALABİLİR; hedef 0.
    /// </summary>
    private static readonly Dictionary<string, int> AyarKoduTabani = new()
    {
        ["Pages/SiparisDetay.razor"] = 1,
    };

    [Fact]
    public void P8_07_razor_ayar_kodu_dogrudan_basimi_tabani_asmaz()
    {
        var web = Path.Combine(DotnetKoku(), "Kuyumcu.Web");
        var sayim = AyarKoduBasimlari(web);
        var fazla = sayim.Where(x => x.Value > AyarKoduTabani.GetValueOrDefault(x.Key)).ToList();
        Assert.True(fazla.Count == 0,
            "Görünür ham ayar kodu (KurKaynakAdi.AyarAdi kullanın, ham değer yalnız title):\n"
            + string.Join("\n", fazla.Select(x => $"  {x.Key}: {x.Value} (taban {AyarKoduTabani.GetValueOrDefault(x.Key)})")));
        Assert.True(sayim.Values.Sum() <= 1, $"toplam {sayim.Values.Sum()} > taban 1");
    }

    private static readonly Regex AyarDogrudan = new(@"(?<!=""\s*)@[\w.?]+\.AyarId\b");
    private static readonly Regex AyarIfade = new(@"(?<!=""\s*)@\((?![^\n]*AyarAdi)[^)]*\bAyarId\b");

    internal static Dictionary<string, int> AyarKoduBasimlari(string web)
    {
        var sonuc = new Dictionary<string, int>();
        foreach (var dosya in Directory.EnumerateFiles(web, "*.razor", SearchOption.AllDirectories))
        {
            var goreli = Path.GetRelativePath(web, dosya).Replace('\\', '/');
            if (goreli.StartsWith("bin/", StringComparison.Ordinal) || goreli.StartsWith("obj/", StringComparison.Ordinal)) continue;
            var metin = File.ReadAllText(dosya);
            var kod = metin.IndexOf("\n@code", StringComparison.Ordinal);
            if (kod >= 0) metin = metin[..kod];
            metin = Regex.Replace(metin, @"@\*.*?\*@", "", RegexOptions.Singleline);
            var n = AyarDogrudan.Matches(metin).Count + AyarIfade.Matches(metin).Count;
            if (n > 0) sonuc[goreli] = n;
        }
        return sonuc;
    }

    private static string DotnetKoku()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
