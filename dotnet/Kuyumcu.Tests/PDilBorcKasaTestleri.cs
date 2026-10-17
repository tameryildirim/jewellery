using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Contracts.Sayfalar.Kasalar;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-DIL-BORC-KASA (denetim 10.10 · Y7, Y8, Y18, O21) — YALNIZ GÖSTERİM.
//  Y7  borç/alacak TEK kalıp, dükkân bakışından: "Müşteri bize borçlu: X" / "Biz müşteriye
//      borçluyuz: X" — Bugün, Müşteriler, Tezgâh üst satırı, müşteri kartı, Raporlar aynı
//      sözlük anahtarından (BorcDiliSozlugu) okur; belirsiz sözler kaynakta 0.
//  Y8  Kasa ekranının büyük rakamı ÇEKMECEDEKİ NAKİT; kart/havale ayrı satırda adlarıyla.
//  Y18 emanet iade etiketi giriş diliyle (gram · altın), has ikincil.
//  O21 BASİT modda müşteri bakiyesi eylemleri düz adla.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class PDilBorcKasaSafTestleri
{
    // ── Y7: kalıp ve işaret ──
    [Fact]
    public void PDBK_01_tek_kalip_isaretten_secilir_artı_bize_borclu_eksi_biz_borcluyuz()
    {
        Assert.Equal("Müşteri bize borçlu: 3.840 ₺", BorcDiliSozlugu.Durum(3840m, "3.840 ₺", "tr"));
        Assert.Equal("Biz müşteriye borçluyuz: 3.840 ₺", BorcDiliSozlugu.Durum(-3840m, "3.840 ₺", "tr"));
        Assert.Equal("Borç yok", BorcDiliSozlugu.Durum(0m, "0 ₺", "tr"));
        Assert.Equal("Customer owes us: 5", BorcDiliSozlugu.Durum(1m, "5", "en"));
        Assert.Equal("We owe the customer: 5", BorcDiliSozlugu.Durum(-1m, "5", "en"));
    }

    [Fact]
    public void PDBK_02_Tezgah_ust_satiri_ayni_kalip_tedarikci_eksi_bakiye_biz_borcluyuz()
    {
        // Denetim Y7: aynı tedarikçi Tezgâh'ta "alacağı 3.840 ₺" yazıyordu.
        var tedarikci = new CariVeri("t1", "Anadolu Toptan", TlBakiye: -3840m);
        Assert.Equal(BorcDiliSozlugu.BizBorcluyuz(Bicim.Tl(3840m), "tr"), YildizTezgahBicim.MusteriDurumu(tedarikci, "tr"));
        var musteri = new CariVeri("m1", "Selin", TlBakiye: 1250m);
        Assert.Equal(BorcDiliSozlugu.BizeBorclu(Bicim.Tl(1250m), "tr"), YildizTezgahBicim.MusteriDurumu(musteri, "tr"));
        Assert.Equal(BorcDiliSozlugu.BorcYok("tr"), YildizTezgahBicim.MusteriDurumu(new CariVeri("m2", "Ali"), "tr"));
    }

    [Fact]
    public void PDBK_03_Musteriler_ust_satiri_ve_suzgec_bakis_acisini_soyler()
    {
        Assert.Equal("7 müşteri · 3 bize borçlu · 1 biz borçluyuz", YildizMusteriSozlugu.Ust(7, 3, 1, "tr"));
        Assert.Equal("100+ müşteri · 3 bize borçlu · 1 biz borçluyuz", YildizMusteriSozlugu.UstSinirli(100, 3, 1, "tr"));
        Assert.Equal("BORÇLU", YildizMusteriSozlugu.Borclu("tr"));   // süzgeç = bize borçlu olanlar (tuval adı)
        Assert.Equal(BorcDiliSozlugu.KisaBizeBorclu("tr"), YildizMusteriSozlugu.BizeBorclu("tr"));
        Assert.Equal(BorcDiliSozlugu.KisaBizBorcluyuz("tr"), YildizBugunSozlugu.BizBorcluyuz("tr"));
    }

    [Fact]
    public void PDBK_04_Bugun_cumlesi_toplamlari_ayni_kalipla_ve_buyuk_rakamin_etiketi()
    {
        var v = new YildizBugunVerisi { SatisAdet = 4, AlacakTl = 1000m, BorcTl = 250m };
        var cumle = YildizBugunBicim.Cumle(v, "tr");
        Assert.Equal($"Bugünkü satış · 4 işlem. Müşteriler bize borçlu: {Bicim.Tl(1000m)}. Biz müşterilere borçluyuz: {Bicim.Tl(250m)}.", cumle);
    }

    [Fact]
    public void PDBK_05_butun_ekran_anahtarlari_TEK_kaynaktan_okur()
    {
        foreach (var dil in new[] { "tr", "en" })
        {
            var en = dil == "en";
            var fm = en ? FisMusteriSozlugu.En : FisMusteriSozlugu.Tr;
            Assert.Equal(BorcDiliSozlugu.BizeBorcluEtiket(dil), fm.Borcu);
            Assert.Equal(BorcDiliSozlugu.BizBorcluyuzEtiket(dil), fm.Alacagi);
            Assert.Equal(BorcDiliSozlugu.BorcYok(dil), fm.BorcuYok);
            var pd2 = en ? ParaDemo2Sozlugu.En : ParaDemo2Sozlugu.Tr;
            Assert.Equal(BorcDiliSozlugu.BizeBorcluKalip(dil), pd2.GuncelBorc);
            Assert.Equal(BorcDiliSozlugu.BizBorcluyuzKalip(dil), pd2.GuncelAlacak);
            var t3 = en ? Toplu3Sozlugu.En : Toplu3Sozlugu.Tr;
            Assert.Equal(BorcDiliSozlugu.BizeBorcluKalip(dil), t3.YasDovizBizeBorclu);
            Assert.Equal(BorcDiliSozlugu.BizBorcluyuzKalip(dil), t3.YasDovizBizBorcluyuz);
            var p2 = en ? KesifP2Sozlugu.En : KesifP2Sozlugu.Tr;
            Assert.Equal(BorcDiliSozlugu.BizeBorcluEtiket(dil), p2.YonBizeBorclu);
            Assert.Equal(BorcDiliSozlugu.BizBorcluyuzEtiket(dil), p2.YonBizBorcluyuz);
        }
    }

    [Fact]
    public void PDBK_06_Tezgah_rozeti_eksi_bakiyede_biz_borcluyuz_kalibi()
    {
        var r = FisBakiyeRozeti.Olustur(new CariVeri("t1", "Anadolu", TlBakiye: -3840m), FisMusteriSozlugu.Tr);
        Assert.NotNull(r);
        Assert.Equal(BorcDiliSozlugu.BizBorcluyuz(Bicim.Para(3840m, "TRY"), "tr"), r!.Value.Metin);
        var b = FisBakiyeRozeti.Olustur(new CariVeri("m1", "Selin", TlBakiye: 500m), FisMusteriSozlugu.Tr);
        Assert.Equal(BorcDiliSozlugu.BizeBorclu(Bicim.Para(500m, "TRY"), "tr"), b!.Value.Metin);
    }

    // ── Metin taraması: bakışı belirsiz söz üretim kaynağında 0 ──
    private static readonly (string Ad, Regex Desen)[] Aykirilar =
    [
        ("alacağı {tutar}", new Regex(@"alacağı \{", RegexOptions.CultureInvariant)),
        ("\"Alacağı\"", new Regex("\"Alacağı\"", RegexOptions.CultureInvariant)),
        ("Alacağın X", new Regex(@"Alacağın ", RegexOptions.CultureInvariant)),
        ("borcun {tutar}", new Regex(@"borcun \{", RegexOptions.CultureInvariant)),
        ("N alacak · M borç", new Regex(@"\} alacak · ", RegexOptions.CultureInvariant)),
        ("X alacaklı", new Regex("alacaklı\"", RegexOptions.CultureInvariant)),
        ("X borçlu (yön yok)", new Regex("\\{0\\} borçlu\"", RegexOptions.CultureInvariant)),
        ("Müşteriye borcumuz", new Regex("Müşteriye borcumuz", RegexOptions.CultureInvariant)),
        ("borcu yok (eski)", new Regex("\"[Bb]orcu yok\"", RegexOptions.CultureInvariant)),
        ("receivable · payable", new Regex(@"receivable · ", RegexOptions.CultureInvariant)),
    ];

    /// <summary>Yorumlar (// … · /* … */ · @* … *@) atılır: yalnız ekrana çıkabilecek kod taranır.</summary>
    private static string YorumsuzKod(string metin)
    {
        metin = Regex.Replace(metin, @"/\*.*?\*/", "", RegexOptions.Singleline);
        metin = Regex.Replace(metin, @"@\*.*?\*@", "", RegexOptions.Singleline);
        return string.Join('\n', metin.Split('\n').Select(s =>
        {
            var i = s.IndexOf("//", StringComparison.Ordinal);
            return i >= 0 && (i == 0 || s[i - 1] != ':') ? s[..i] : s;
        }));
    }

    public static IReadOnlyList<string> AykiriSatirlar()
    {
        var kok = Toplu3Ortak.Kok();
        var dosyalar = Directory.EnumerateFiles(Path.Combine(kok, "Kuyumcu.Core", "I18n"), "*.cs")
            .Concat(Directory.EnumerateFiles(Path.Combine(kok, "Kuyumcu.Web"), "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".razor", StringComparison.Ordinal) || f.EndsWith(".cs", StringComparison.Ordinal)))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        var sonuc = new List<string>();
        foreach (var f in dosyalar)
        {
            var satirlar = YorumsuzKod(File.ReadAllText(f)).Split('\n');
            for (var i = 0; i < satirlar.Length; i++)
                foreach (var (ad, desen) in Aykirilar)
                    if (desen.IsMatch(satirlar[i])) sonuc.Add($"{Path.GetFileName(f)}:{i + 1} [{ad}] {satirlar[i].Trim()}");
        }
        return sonuc;
    }

    [Fact]
    public void PDBK_07_metin_taramasi_belirsiz_borc_alacak_sozu_SIFIR()
    {
        var aykiri = AykiriSatirlar();
        Assert.True(aykiri.Count == 0, "Aykırı borç/alacak sözü:\n" + string.Join("\n", aykiri));
    }

    // ── Y8: büyük rakam ──
    [Fact]
    public void PDBK_08_kasa_buyuk_rakami_cekmecedeki_nakit_ayrim_yoksa_toplam()
    {
        var ayrim = new KasaNakitAyrimiOzeti("k1", "TRY", 312999.49m, 193744.49m, 22846m, 96409m);
        Assert.Equal(193744.49m, Kasalar.BuyukRakam(312999.49m, ayrim));
        Assert.Equal(312999.49m, Kasalar.BuyukRakam(312999.49m, null));
    }

    // ── Y18 ──
    [Fact]
    public void PDBK_09_emanet_iade_etiketi_giris_diliyle_has_ikincil()
    {
        Assert.Equal("Geri verilecek altın (g has)", Sozlukler.Tr.Emanet.IadeEdilecekHas);
        Assert.DoesNotContain("has gram", Sozlukler.Tr.Emanet.IadeEdilecekHas, StringComparison.OrdinalIgnoreCase);
    }
}

// ── Y8 ekran: Platin Kasalar kartı + üst toplam ──
public sealed class PDilBorcKasaKasaEkranTestleri : BunitContext
{
    private bool ayrimHata;

    public PDilBorcKasaKasaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(PDilBorcKasaKasaEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var get = istek.Method == HttpMethod.Get;
            if (get && yol.Contains("/kasalar/nakit-ayrimi") && t.ayrimHata)
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("""{"hata":"Ayrım okunamadı"}""", System.Text.Encoding.UTF8, "application/json") });
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube"}]""",
                var v when get && v.Contains("/kasalar/nakit-ayrimi") =>
                    """[{"kasaId":"k1","paraBirimi":"TRY","toplam":312999.49,"nakit":193744.49,"kart":22846,"havale":96409}]""",
                var v when get && v.Contains("/kasalar/") && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when get && v.EndsWith("/kasalar") =>
                    """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":312999.49,"aktif":true,"subeId":"s1"}]""",
                _ => "[]",
            }));
        }
    }

    private IRenderedComponent<Kasalar> Ciz(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici(
            "u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kasalar>().AyrintiAc();
        e.WaitForElement("#kasa-bakiye-k1");
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void PDBK_10_kasa_karti_buyuk_rakam_nakit_kart_havale_adlariyla_ayri(string mod)
    {
        var e = Ciz(mod);
        e.WaitForAssertion(() => Assert.Equal(Bicim.Para(193744.49m, "TRY"), e.Find("#kasa-bakiye-k1").TextContent.Trim()));
        Assert.Equal(KasaNakitSozlugu.CekmecedekiNakit("tr"), e.Find("#kasa-buyuk-etiket-k1").TextContent.Trim());
        Assert.Equal(KasaNakitSozlugu.AyrimSatiri(Bicim.Para(22846m, "TRY"), Bicim.Para(96409m, "TRY"), Bicim.Para(312999.49m, "TRY"), "tr"),
            e.Find("#kasa-ayrim-k1").TextContent.Trim());
        // Üst toplam kartı da çekmecedeki nakit; kart+havale dahil toplam alt satırda.
        var ust = e.Find(".stat-izgara").TextContent;
        Assert.Contains(KasaNakitSozlugu.CekmecedekiNakitTl("tr"), ust);
        Assert.Contains(KasaNakitSozlugu.ToplamAlt(Bicim.Para(312999.49m, "TRY"), "tr"), ust);
        Assert.DoesNotContain(Sozlukler.Tr.Kasa.ToplamTlBakiye, ust);
    }

    [Fact]
    public void PDBK_11_ayrim_okunamazsa_buyuk_rakam_toplam_ve_etiketi_toplam_der_nakit_demez()
    {
        ayrimHata = true;
        var e = Ciz(ArayuzModuServisi.Basit);
        e.WaitForElement("#kasa-ayrim-hata-k1");
        Assert.Equal(Bicim.Para(312999.49m, "TRY"), e.Find("#kasa-bakiye-k1").TextContent.Trim());
        Assert.Equal(KasaNakitSozlugu.KasaToplami("tr"), e.Find("#kasa-buyuk-etiket-k1").TextContent.Trim());
        Assert.Contains(Sozlukler.Tr.Kasa.ToplamTlBakiye, e.Find(".stat-izgara").TextContent);
    }
}

// ── O21: BASİT modda müşteri bakiyesi eylemleri düz adla (PRO'da eski ad) ──
public sealed class PDilBorcKasaCariEkranTestleri : BunitContext
{
    public PDilBorcKasaCariEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var y = istek.RequestUri!.AbsolutePath;
            var g = y switch
            {
                _ when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                _ when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                _ when y.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Anadolu Toptan","tur":"TEDARIKCI","telefon":"05551234567","email":null,
                      "tlBakiye":-3840.00,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,
                      "riskSeviye":null,"pepBeyani":false,"aktif":true}]
                    """,
                _ when y.Contains("/fiyat/listeler") => """[]""",
                _ => "{}",
            };
            return Task.FromResult(Toplu5Ortak.Yanit(g));
        }
    }

    private IRenderedComponent<Cariler> Ciz(string mod)
    {
        Toplu5Ortak.Oturum(this, mod);
        var e = Render<Cariler>();
        e.WaitForState(() => e.FindAll("#c-doviz-veresiye-c1").Count > 0);
        return e;
    }

    [Fact]
    public void PDBK_12_BASIT_duz_ad_PRO_eski_ad()
    {
        var b = Ciz(ArayuzModuServisi.Basit);
        Assert.Equal(BorcDiliSozlugu.BakiyeDuzelt("tr"), b.FindAll("#c-duzeltme-c1")[0].TextContent.Trim());
        Assert.Equal(BorcDiliSozlugu.DovizleVeresiye("tr"), b.FindAll("#c-doviz-veresiye-c1")[0].TextContent.Trim());
        Assert.Equal(BorcDiliSozlugu.DovizleTahsilat("tr"), b.FindAll("#c-doviz-tahsilat-c1")[0].TextContent.Trim());
        // Kart: eksi bakiye = biz borçluyuz (dükkân bakışı) — "alacak" sözü yok.
        Assert.Contains(BorcDiliSozlugu.BizBorcluyuzEtiket("tr"), b.Markup);
    }

    [Fact]
    public void PDBK_13_PRO_eski_adlar_korunur()
    {
        var p = Ciz(ArayuzModuServisi.Profesyonel);
        Assert.Equal(Sozlukler.Tr.Cari.Duzeltme, p.FindAll("#c-duzeltme-c1")[0].TextContent.Trim());
        Assert.Equal(Sozlukler.Tr.Cari.DovizVeresiye, p.FindAll("#c-doviz-veresiye-c1")[0].TextContent.Trim());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// TUR 2 (doğrulayıcı + tasarım hakemi, 11.10) — Y18 emanet BIRAKILDIĞI GİBİ ("22\u00A0ayar · 12,50\u00A0g"),
// tek işaret kuralı (BorcDiliSozlugu.Yon — gösterim hassasiyetinden), Platin rozet küçük has sınırı.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class PDilBorcKasaTur2SafTestleri
{
    private static Kuyumcu.Core.Domain.EmanetIz G(string? ayar, decimal? gram, decimal has, decimal? tas = null) => new(ayar, gram, tas, has);

    [Fact]
    public void PDBK_14_birakildigi_gibi_ozet_ayar_bazinda_kalan_gram()
    {
        // 12,50 g 22 ayar bırakıldı (has 11,45); 5 g 22 ayar geri verildi → kalan 7,50 g.
        var o = Kuyumcu.Core.Domain.EmanetBirakildigi.Ozetle([G("K22", 12.5m, 11.45m), G("K22", -5m, -4.58m)]);
        var s = Assert.Single(o!);
        Assert.Equal(("K22", 7.5m, 6.87m), (s.AyarId, s.Gram, s.Has));
        // Taş: brüt 12,5 · taş 0,5 → kalan NET 12 g (has netten hesaplanır).
        Assert.Equal(12m, Assert.Single(Kuyumcu.Core.Domain.EmanetBirakildigi.Ozetle([G("K22", 12.5m, 10.992m, 0.5m)])!).Gram);
        // İki ayar: gramı büyük olan önde.
        var iki = Kuyumcu.Core.Domain.EmanetBirakildigi.Ozetle([G("K14", 6m, 3.51m), G("K22", 12.5m, 11.45m)])!;
        Assert.Equal(["K22", "K14"], iki.Select(x => x.AyarId));
        // Tam iade → boş (emanet yok).
        Assert.Empty(Kuyumcu.Core.Domain.EmanetBirakildigi.Ozetle([G("K22", 12.5m, 11.45m), G("K22", -12.5m, -11.45m)])!);
    }

    [Fact]
    public void PDBK_15_ayar_bilinmiyorsa_ya_da_baska_ayarla_iade_edildiyse_NULL_uydurulmaz()
    {
        // Eski satır (kolondan önce): ayar yok → bilinmiyor.
        Assert.Null(Kuyumcu.Core.Domain.EmanetBirakildigi.Ozetle([G(null, null, 11.45m)]));
        // Yeni giriş + eski (has'lı) iade: ayarsız has ≠ 0 → bilinmiyor.
        Assert.Null(Kuyumcu.Core.Domain.EmanetBirakildigi.Ozetle([G("K22", 12.5m, 11.45m), G(null, null, -2m)]));
        // 22 ayar bırakıldı, 14 ayar olarak geri verildi → 14 ayarın kalanı eksi → bilinmiyor.
        Assert.Null(Kuyumcu.Core.Domain.EmanetBirakildigi.Ozetle([G("K22", 12.5m, 11.45m), G("K14", -2m, -1.17m)]));
    }

    [Fact]
    public void PDBK_16_emanet_dili_ayar_gram_tasi_tek_kalip()
    {
        Assert.Equal("22\u00A0ayar · 12,50\u00A0g", EmanetDiliSozlugu.AyarGram("K22", 12.5m, "tr"));
        Assert.Equal("22K · 12.50\u00A0g", EmanetDiliSozlugu.AyarGram("K22", 12.5m, "en"));
        Assert.Equal("22\u00A0ayar · 12,50\u00A0g · taş 0,50\u00A0g", EmanetDiliSozlugu.Satir("K22", 12.5m, 0.5m, "tr"));
        Assert.Equal("925 Gümüş · 3,125\u00A0g", EmanetDiliSozlugu.AyarGram("G925", 3.125m, "tr"));
        var s = new[] { new Kuyumcu.Core.Domain.EmanetAyarSatiri("K22", "22 Ayar", 12.5m, 11.45m) };
        Assert.Equal("Emanette 22\u00A0ayar · 12,50\u00A0g", EmanetDiliSozlugu.Emanette(s, "tr"));
        Assert.Equal("emanette 22\u00A0ayar 12,50\u00A0g", EmanetDiliSozlugu.EmanetteDuz(s, "tr"));
        Assert.DoesNotContain("has", EmanetDiliSozlugu.Emanette(s, "tr"));
    }

    [Fact]
    public void PDBK_17_Yildiz_emanet_ne_birakti_sutunu_tuval_kalibi()
    {
        var k = new YildizEmanetKaydi("c1", "Derya", "h1", DateTime.UtcNow, 10.992m, null, "ALTIN", YildizEmanetDurum.Emanette, "K22", 12.5m, 0.5m);
        Assert.Equal("22\u00A0ayar · 12,50\u00A0g · taş 0,50\u00A0g", Kuyumcu.Web.Pages.Emanet.YildizNeBirakti(k, "tr"));
        Assert.Equal("tamir · 22\u00A0ayar · 12,50\u00A0g · taş 0,50\u00A0g", Kuyumcu.Web.Pages.Emanet.YildizNeBirakti(k with { Aciklama = "tamir" }, "tr"));
        // Eski satır: açıklama ya da maden adı (uydurma yok).
        Assert.Equal("Bilezik", Kuyumcu.Web.Pages.Emanet.YildizNeBirakti(k with { AyarId = null, Gram = null, Aciklama = "Bilezik" }, "tr"));
    }

    [Fact]
    public void PDBK_18_tek_isaret_kurali_gosterim_hassasiyetinden_ve_rozet_sinirlari()
    {
        // Yön yuvarlanmış değerden: 0,0004 g has (3 hane) = 0 → borç yok; 0,0005 → bize borçlu.
        Assert.Equal(0, BorcDiliSozlugu.Yon(0.0004m, 3));
        Assert.Equal(1, BorcDiliSozlugu.Yon(0.0005m, 3));
        Assert.Equal(-1, BorcDiliSozlugu.Yon(-0.005m, 2));
        Assert.Equal(0, BorcDiliSozlugu.Yon(-0.004m, 2));
        Assert.Equal("Borç yok", BorcDiliSozlugu.Durum(0.004m, 2, v => v.ToString(), "tr"));
        Assert.Equal("Biz müşteriye borçluyuz: 0,01", BorcDiliSozlugu.Durum(-0.005m, 2, v => v.ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")), "tr"));
        // Platin Tezgâh rozeti: 0,0004 g has yalnızken "Borç yok" (eskiden "bize borçlu: 0,000\u00A0g has").
        var m = FisMusteriSozlugu.Tr;
        var kucuk = FisBakiyeRozeti.Olustur(new CariVeri("c1", "Ayşe", HasGramBakiye: 0.0004m), m);
        Assert.Equal(m.BorcuYok, kucuk!.Value.Metin);
        var sinir = FisBakiyeRozeti.Olustur(new CariVeri("c1", "Ayşe", TlBakiye: -0.004m, HasGramBakiye: 0.0005m), m);
        Assert.Equal($"{m.Borcu}: 0,001 g {m.Has}", sinir!.Value.Metin);
        // Tezgâh üst satırı aynı kural: −0,004 ₺ → borç yok.
        Assert.Equal(BorcDiliSozlugu.BorcYok("tr"), YildizTezgahBicim.MusteriDurumu(new CariVeri("t1", "X", TlBakiye: -0.004m), "tr"));
    }
}

// ── Y18 servis: giriş gram/ayarı saklar, iade ayar+gramla, storno ters, özet uçlarda ──
[Collection("veritabani")]
public sealed class PDilBorcKasaEmanetDbTestleri(TestVeritabani vt)
{
    private static readonly System.Text.Json.JsonSerializerOptions Web = new(System.Text.Json.JsonSerializerDefaults.Web);
    private static System.Text.Json.JsonElement J(object o) => System.Text.Json.JsonSerializer.SerializeToElement(o, Web);
    private static Kuyumcu.Api.Modules.Emanet.EmanetServisi Emanet(Kuyumcu.Api.Data.KuyumcuDbContext db) =>
        new(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Kuyumcu.Api.Modules.Emanet.EmanetServisi>.Instance);

    private Kiraci YeniKiraci(string ad)
    {
        using var kurulum = vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, $"PAKETPDILBORC-TEST {ad}"), 2_000m);
    }

    private async Task<System.Text.Json.JsonElement> Ozet(Kiraci k)
    {
        using var db = vt.Olustur(k);
        return J(await Emanet(db).CariEmanetListesi(k.CariId, k.Islem));
    }

    private async Task Giris(Kiraci k, string ayar, decimal gram, decimal? tas = null)
    {
        using var db = vt.Olustur(k);
        await Emanet(db).Giris(new Kuyumcu.Api.Modules.Emanet.EmanetGirisDto
        { CariId = k.CariId, AyarId = ayar, GramAgirlik = gram, TasAgirlik = tas, KurSnapshotId = k.KurId }, k.Islem);
    }

    private async Task Cikis(Kiraci k, decimal has, string? ayar = null, decimal? gram = null)
    {
        using var db = vt.Olustur(k);
        await Emanet(db).Cikis(k.CariId, new Kuyumcu.Api.Modules.Emanet.EmanetCikisDto
        { HasGrami = has, AyarId = ayar, GramAgirlik = gram, KurSnapshotId = k.KurId }, k.Islem);
    }

    [Fact]
    public async Task PDBK_19_giris_birakildigi_gibi_saklanir_ozet_ayar_gram_doner_has_ayni()
    {
        var k = YeniKiraci("giris");
        await Giris(k, "K22", 12.5m);
        using (var db = vt.OlusturSistem())
        {
            var h = db.Hareketler.Single(x => x.TenantId == k.TenantId && x.Tur == "EMANET_GIRIS");
            Assert.Equal((12.5m, (decimal?)null, "K22", 11.45m), (h.EmanetGram!.Value, h.EmanetTas, h.EmanetAyarId!, h.HasGrami));
        }
        var o = await Ozet(k);
        Assert.Equal(11.45m, o.GetProperty("mevcutHasGramBakiye").GetDecimal());   // hesap DEĞİŞMEDİ
        var s = Assert.Single(o.GetProperty("birakildigiGibi").EnumerateArray());
        Assert.Equal(("K22", 12.5m, 11.45m), (s.GetProperty("ayarId").GetString(), s.GetProperty("gram").GetDecimal(), s.GetProperty("has").GetDecimal()));
        // Cari listesi aynı özeti taşır (Müşteriler satırı "Emanette 22\u00A0ayar · 12,50\u00A0g").
        using var cdb = vt.Olustur(k);
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(cdb,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance, TestKurulum.Denetim(cdb));
        var satir = J(await cari.Listele(k.Islem, null, null, null, 50, 0)).EnumerateArray().Single(x => x.GetProperty("id").GetString() == k.CariId);
        Assert.Equal(12.5m, Assert.Single(satir.GetProperty("emanetBirakildigi").EnumerateArray()).GetProperty("gram").GetDecimal());
        Assert.Equal(11.45m, satir.GetProperty("emanetteHas").GetDecimal());
    }

    [Fact]
    public async Task PDBK_20_iade_ayar_gramla_kalan_azalir_has_uyusmazsa_400_ayarsiz_iade_ozeti_bilinmez_yapar()
    {
        var k = YeniKiraci("iade");
        await Giris(k, "K22", 12.5m);
        await Cikis(k, 4.58m, "K22", 5m);                       // 5 g 22 ayar = 4,58 has
        var s = Assert.Single((await Ozet(k)).GetProperty("birakildigiGibi").EnumerateArray());
        Assert.Equal(7.5m, s.GetProperty("gram").GetDecimal());
        using (var db = vt.OlusturSistem())
        {
            var c = db.Hareketler.Single(x => x.TenantId == k.TenantId && x.Tur == "EMANET_CIKIS");
            Assert.Equal((-5m, "K22", -4.58m), (c.EmanetGram!.Value, c.EmanetAyarId!, c.HasGrami));
        }
        // Gövdedeki has ayar·gramla tutmuyor (5 g 22 ayar ≠ 4 has) → 400, deftere yazılmaz.
        var x = await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => Cikis(k, 4m, "K22", 5m));
        Assert.Contains("uyuşmuyor", x.Message);
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => Cikis(k, 4.58m, null, 5m));   // gram var, ayar yok
        // Eski yol (yalnız has) çalışır ama özet artık bilinmez → null (ekran has'a düşer).
        await Cikis(k, 1m);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await Ozet(k)).GetProperty("birakildigiGibi").ValueKind);
    }

    [Fact]
    public async Task PDBK_21_storno_birakildigi_gibi_izi_ters_isaretle_dondurur()
    {
        var k = YeniKiraci("storno");
        await Giris(k, "K22", 12.5m, 0.5m);
        string id;
        using (var db = vt.OlusturSistem())
            id = db.Hareketler.Single(x => x.TenantId == k.TenantId && x.Tur == "EMANET_GIRIS").Id;
        using (var db = vt.Olustur(k))
            await new Kuyumcu.Api.Modules.Satis.StornoServisi(db, TestKurulum.Denetim(db),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<Kuyumcu.Api.Modules.Satis.StornoServisi>.Instance)
                .Storno(id, new Kuyumcu.Api.Modules.Satis.StornoDto { Gerekce = "PDBK storno testi — yeterince uzun gerekçe", KurSnapshotId = k.KurId }, k.Islem);
        using (var db = vt.OlusturSistem())
        {
            var t = db.Hareketler.Single(x => x.TenantId == k.TenantId && x.StornoKaynakId == id);
            Assert.Equal((-12.5m, (decimal?)-0.5m, "K22"), (t.EmanetGram!.Value, t.EmanetTas, t.EmanetAyarId!));
        }
        Assert.Empty((await Ozet(k)).GetProperty("birakildigiGibi").EnumerateArray());
    }
}

// ── Y18 ekran: Platin Emanet özeti ve iade çekmecesi bırakıldığı gibi; Müşteriler satırı ──
public sealed class PDilBorcKasaEmanetEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool ayarsiz;

    private sealed class SahteApi(PDilBorcKasaEmanetEkranTestleri s) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (s.istekler) s.istekler.Add((istek.Method.Method, yol, govde));
            var bg = s.ayarsiz ? "null" : """[{"ayarId":"K22","ayarAdi":"22 Ayar","gram":12.5,"has":11.45}]""";
            var g = yol switch
            {
                var y when y.Contains("/kur/guncel") => """{"id":"kur-9","altinHasFiyati":6800}""",
                var y when y.Contains("/cikis") => """{"cariId":"c1","iadeEdilenHas":11.45,"maden":"ALTIN","madenAdi":"Altın"}""",
                var y when y.Contains("/emanet/") => $$"""
                    {"cariId":"c1","mevcutHasGramBakiye":11.45,
                     "madenBakiyeleri":[{"maden":"ALTIN","madenAdi":"Altın","gram":11.45}],
                     "birakildigiGibi":{{bg}},
                     "hareketler":[{"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-10-10T10:00:00Z","hasGrami":11.45,
                        "aciklama":null,"stornoKaynakId":null,"fisId":null,"maden":"ALTIN",
                        "emanetGram":12.5,"emanetTas":null,"emanetAyarId":"K22"}]}
                    """,
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    public PDilBorcKasaEmanetEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Emanet> Ciz(string mod)
    {
        Toplu5Ortak.Oturum(this, mod);
        var e = Render<Kuyumcu.Web.Pages.Emanet>().AyrintiAc();
        SeciciYardim.Sec(e, "e-cari", "c1");
        e.WaitForAssertion(() => Assert.Matches("(g|has)$", e.Find("#e-ozet-deger").TextContent.Trim()));
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void PDBK_22_ozet_birakildigi_gibi_has_yalniz_PRO_ikincil(string mod)
    {
        var e = Ciz(mod);
        e.WaitForAssertion(() => Assert.Equal("22\u00A0ayar · 12,50\u00A0g", e.Find("#e-ozet-deger").TextContent.Trim()));
        Assert.Equal(mod == ArayuzModuServisi.Profesyonel, e.FindAll("#e-ozet-has").Count == 1);
    }

    [Fact]
    public void PDBK_23_iade_cekmecesi_ayar_ve_gramla_acilir_POST_ayar_gram_has_tasir()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#e-cikis-ac").Click();
        Assert.Equal("22\u00A0ayar · 12,50\u00A0g", e.Find("#c-bakiye-altin, #c-maden-bakiyeleri strong").TextContent.Trim());
        Assert.Equal("22\u00A0ayar", e.Find("#c-ayar-sabit").TextContent.Trim());   // tek ayar → seçici yok (UX-S22)
        Assert.Empty(e.FindAll("#c-has"));                                    // has gizli hesap
        e.Find("#e-cikis-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(Istekler(), i => i.Metot == "POST");
            Assert.EndsWith("/emanet/c1/cikis", post.Yol);
            using var d = System.Text.Json.JsonDocument.Parse(post.Govde);
            Assert.Equal("K22", d.RootElement.GetProperty("ayarId").GetString());
            Assert.Equal(12.5m, d.RootElement.GetProperty("gramAgirlik").GetDecimal());
            Assert.Equal(11.45m, d.RootElement.GetProperty("hasGrami").GetDecimal());
        });
    }

    [Fact]
    public void PDBK_24_kalandan_fazla_gram_alan_altinda_uyari_ve_istek_yok()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        e.Find("#e-cikis-ac").Click();
        e.Find("#c-gram").Input("13");
        e.Find("#c-gram").Blur();
        e.WaitForElement("#c-gram-fazla");
        Assert.True(e.Find("#e-cikis-kaydet").HasAttribute("disabled"));
        Assert.DoesNotContain(Istekler(), i => i.Metot == "POST");
    }

    [Fact]
    public void PDBK_25_ayar_bilinmiyorsa_eski_has_ekrani_korunur()
    {
        ayarsiz = true;
        var e = Ciz(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Equal(Bicim.Has(11.45m), e.Find("#e-ozet-deger").TextContent.Trim()));
        e.Find("#e-cikis-ac").Click();
        Assert.NotNull(e.Find("#c-has"));
    }

    private List<(string Metot, string Yol, string Govde)> Istekler() { lock (istekler) return [.. istekler]; }
}

public sealed class PDilBorcKasaMusteriEmanetTestleri
{
    [Fact]
    public void PDBK_26_Yildiz_musteri_satiri_emanet_birakildigi_gibi_yoksa_has()
    {
        var bg = new[] { new Kuyumcu.Core.Domain.EmanetAyarSatiri("K22", "22 Ayar", 12.5m, 11.45m) };
        var c = new Kuyumcu.Contracts.Sayfalar.Cariler.CariVeri("c1", "Ayşe", "MUSTERI", null, null, 0m, 0m, EmanetteHas: 11.45m, EmanetBirakildigi: bg);
        Assert.Equal("emanette 22\u00A0ayar 12,50\u00A0g", YildizMusteriBicim.Satir(c, null, null, "", "tr").Durum);
        var eski = c with { EmanetBirakildigi = null };
        Assert.Equal(YildizMusteriSozlugu.Emanette("11,45", "tr"), YildizMusteriBicim.Satir(eski, null, null, "", "tr").Durum);
    }
}
