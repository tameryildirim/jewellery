using System.Globalization;
using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET BELGE-KALAN — KESIF-P3 tasarım hakemi tur 2 kalanları.
//   (1) VERİ/YASAL: taslak fatura Düzenle çekmecesinde tarih listeden bir gün geri
//       (kayıt UTC anı; liste yerel gün, çekmece UTC günü) — dokunmadan Kaydet belgeyi
//       bir gün geri yazıyordu. Sunucu: gün aynıysa kayıt değişmez; seri yılı işletme yılı.
//   (2) Düzenle çekmecesinde defter bağlı kalemin adedi salt okunur.
//   (3) PRO düzenlenebilir kalem tablosu her genişlikte kart (1440'ta kap dışı hücre).
//   (4) Stok değeri / reyon stok: maden değerleri ayrı satır (yapışık metin).
//   (5) Gün sonu faturasız işlemler: ödeme kodu okunur ad.
//   (6) Denetim izi: arayüz tercihi anahtarı okunur ad (iki modda).
// ⚠ Sahte API her uca GERÇEKÇİ yanıt döndürür.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class BelgeKalanSunucuTestleri(TestVeritabani vt)
{
    private static MFatura.FaturaServisi Fatura(Kuyumcu.Api.Data.KuyumcuDbContext db) => new(db);
    private static T Prop<T>(object o, string ad) => (T)o.GetType().GetProperty(ad)!.GetValue(o)!;
    private static JsonElement Govde(string s) => JsonDocument.Parse(s).RootElement;

    /// <summary>01:30 TSI kaydı (= önceki gün 22:30 UTC): işletme günü 1.10. Aynı günü PATCH
    /// etmek kaydı DEĞİŞTİRMEZ (eski yol 30.09'a, yani bir gün GERİ yazıyordu); başka gün
    /// o güne yazar ve işletme günü o gün olur.</summary>
    [Fact]
    public async Task BK_Taslak_tarih_PATCH_isletme_gunu_ayniysa_kayit_degismez()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "BK-tarih");
        using var db = vt.Olustur(k);
        var an = new DateTime(2026, 9, 30, 22, 30, 0, DateTimeKind.Utc);
        var t = await Fatura(db).Olustur(new MFatura.FaturaOlusturDto
            { CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m, Tarih = an }, k.Islem);
        var id = Prop<string>(t, "Id");

        await Fatura(db).TaslakGuncelle(id, Govde("""{"tarih":"2026-10-01"}"""), k.Islem);
        using (var oku = vt.OlusturSistem())
            Assert.Equal(an, DateTime.SpecifyKind(oku.Faturalar.Single(f => f.Id == id).Tarih, DateTimeKind.Utc));

        await Fatura(db).TaslakGuncelle(id, Govde("""{"tarih":"2026-09-29"}"""), k.Islem);
        using (var oku = vt.OlusturSistem())
        {
            var yeni = oku.Faturalar.Single(f => f.Id == id).Tarih;
            Assert.Equal(new DateOnly(2026, 9, 29), DateOnly.FromDateTime(Kuyumcu.Api.Common.Zaman.UtcdenYerele(yeni)));
        }
    }

    /// <summary>23:30 TSI kaydı (20:30 UTC): UTC ve yerel gün aynı — aynı günü PATCH etmek
    /// kaydı değiştirmez.</summary>
    [Fact]
    public async Task BK_Taslak_tarih_PATCH_2330_TSI_ayni_gun_korunur()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "BK-2330");
        using var db = vt.Olustur(k);
        var an = new DateTime(2026, 9, 30, 20, 30, 0, DateTimeKind.Utc);
        var id = Prop<string>(await Fatura(db).Olustur(new MFatura.FaturaOlusturDto
            { CariId = k.CariId, Toplam = 1_300m, KdvOran = 0.20m, Tarih = an }, k.Islem), "Id");
        await Fatura(db).TaslakGuncelle(id, Govde("""{"tarih":"2026-09-30"}"""), k.Islem);
        using var oku = vt.OlusturSistem();
        Assert.Equal(an, DateTime.SpecifyKind(oku.Faturalar.Single(f => f.Id == id).Tarih, DateTimeKind.Utc));
    }

    /// <summary>1 Ocak 2027 01:30 TSI (= 31.12.2026 22:30 UTC) kesilen belge 2027 serisinden
    /// numara alır (UTC yılı 2026 idi).</summary>
    [Fact]
    public async Task BK_Seri_yili_isletme_yilidir()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "BK-seri");
        using var db = vt.Olustur(k);
        var id = Prop<string>(await Fatura(db).Olustur(new MFatura.FaturaOlusturDto
            { CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m,
              // PAKET GOC-A (envanter açık #9): fatura tarihi İLERİ OLAMAZ (K8a) — sınır geçmiş yılbaşıyla sınanır:
              // 31.12.2025 22:30Z = 01.01.2026 01:30 TSI → 2026 serisi (UTC yılı 2025 DEĞİL).
              Tarih = new DateTime(2025, 12, 31, 22, 30, 0, DateTimeKind.Utc) }, k.Islem), "Id");
        await Fatura(db).DurumGuncelle(id, new MFatura.DurumGuncelleDto { Durum = "KESILDI" }, k.Islem);
        using var oku = vt.OlusturSistem();
        Assert.Contains("-2026-", oku.Faturalar.Single(f => f.Id == id).FaturaNo);
    }
}

public sealed class BelgeKalanEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string faturaDurum = "TASLAK";
    private string faturaTarih = "2026-09-30T22:30:00";
    private bool hareketBagli = true;
    private string denetim = DenetimTercih;

    public BelgeKalanEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kuyumcu.Web.Bilesenler.KayitSecici.BeklemeMs = 0;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private void Mod(string mod)
    {
        var m = Services.GetRequiredService<ArayuzModuServisi>();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => m.BasitMi);
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
    }

    private string Liste => $$"""
        {"faturalar":[{"id":"f9","faturaNo":{{(faturaDurum == "TASLAK" ? "null" : "\"FTR-2026-000009\"")}},
          "cariAd":"Mehmet Kaya","tur":"SATIS","araToplam":0,"kdvOran":0.2,"kdvTutar":0,"toplam":1620,
          "durum":"{{faturaDurum}}","bankaHareketId":null,"tarih":"{{faturaTarih}}",
          "kalemKaynagi":"KALEM","hasDegerKaynagi":"KALEM","kdvTuru":"STANDART","cariId":"c2","aciklama":null}]}
        """;

    private string Kalemler => $$"""
        {"faturaId":"f9","faturaNo":null,"kalemKaynagi":"KALEM","hasDegerKaynagi":"KALEM","toplam":1620,
         "kalemler":[
           {"id":"ka","sira":1,"aciklama":"Burma Bilezik","miktar":1,"birim":"ADET",{{(hareketBagli ? "\"hareketId\":\"h1\"," : "")}}
            "ayarId":"K22","milyem":916,"gramAgirlik":10,"hasGrami":9.16,"tasIbaresi":null,
            "birimFiyat":1000,"iscilikTl":120,"indirimTutari":null,"kdvTuru":"STANDART",
            "kdvOran":0.2,"hasDeger":0,"kdvMatrah":0,"kdvTutar":0,"satirToplam":1120},
           {"id":"kb","sira":2,"aciklama":"Çeyrek altın","miktar":1,"birim":"ADET",
            "ayarId":"K22","milyem":916,"gramAgirlik":1.75,"hasGrami":1.6,"tasIbaresi":null,
            "birimFiyat":500,"iscilikTl":null,"indirimTutari":null,"kdvTuru":"STANDART",
            "kdvOran":0.2,"hasDeger":0,"kdvMatrah":0,"kdvTutar":0,"satirToplam":500}]}
        """;

    private const string DenetimTercih = """
        {"toplam":1,"limit":7,"offset":0,"kayitlar":[
          {"id":"d1","tarih":"2026-09-30T10:00:00Z","eylem":"EKLEME","varlikTuru":"KullaniciTercihi","varlikId":"u1","hassas":false,
           "detay":"Arayüz tercihi güncellendi: menu-acik-gruplar","ipAdresi":null,"kullaniciId":"u1","kullanici":"Ayşe Yılmaz","rol":"SAHIP",
           "subeId":null,"subeAd":null,"kasaId":null,"kasaAd":null}]}
        """;

    private const string Mutabakat = """
        {"tarih":"2026-09-30","tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},"satis":{"sayisi":3,"toplamTl":3000,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":3,"faturasizTl":3000},"alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},"hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},"fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},"fark":3000,"mutabik":false,"uyarilar":[],"faturaEslesme":null,
        "faturasizIslemler":[{"id":"h1","tarih":"2026-09-30T08:00:00Z","tlTutari":1000,"odemeTuru":"VERESIYE","cariId":"c1"},
                             {"id":"h2","tarih":"2026-09-30T09:00:00Z","tlTutari":1500,"odemeTuru":"KART","cariId":null},
                             {"id":"h3","tarih":"2026-09-30T10:00:00Z","tlTutari":500,"odemeTuru":"NAKIT","cariId":null}]}
        """;

    private const string ReyonStok = """
        {"reyonlar":[{"reyonId":"r1","reyonAdi":"Vitrin 1","tip":"VITRIN","aktif":true,"sorumluKullaniciId":null,
                      "subeId":"s1","subeAdi":"Merkez","adet":5,"urunSayisi":2,"altinHasGram":159.2652,
                      "madenler":[{"maden":"ALTIN","madenAdi":"Altın","adet":3,"toplamGram":159.2652},
                                  {"maden":"GUMUS","madenAdi":"Gümüş","adet":2,"toplamGram":83.25}]}],
         "atanmamis":[{"subeId":"s1","subeAdi":"Merkez","adet":1,
                       "madenler":[{"maden":"ALTIN","madenAdi":"Altın","adet":1,"toplamGram":9.16},
                                   {"maden":"GUMUS","madenAdi":"Gümüş","adet":1,"toplamGram":46.25}]}],
         "toplamAltinHasGram":168.4252,"uyari":null}
        """;

    private sealed class SahteApi(BelgeKalanEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            var g = yol switch
            {
                var y when y.Contains("/kalemler") => t.Kalemler,
                var y when y.Contains("/ozel-matrah-ayari") =>
                    """{"kod":null,"acik":false,"aralikta":false,"aralik":"801-812","varsayilanOran":0.20,"kaynakBeyani":"Kaynak beyanı (test).","maliMusavirTeyidi":false}""",
                var y when y.Contains("/faturalanabilir-islemler") => """{"gun":30,"limit":50,"dahaVar":false,"islemler":[]}""",
                var y when y.EndsWith("/faturalar") && get => t.Liste,
                var y when y.StartsWith("/api/v1/faturalar/", StringComparison.Ordinal) && !get =>
                    """{"id":"f9","durum":"TASLAK"}""",
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """
                    {"yil":2026,"ay":9,"satis":{"toplamTl":0,"adet":0},"alis":{"toplamTl":0,"adet":0},
                     "tahsilat":{"toplamTl":0},"veresiye":{"toplamTl":0},"karTl":null,"karKaynagi":"HESAPLANAMADI",
                     "satisAlisFarkiTl":0,"netHasGramHareketi":0,"madenDagilimi":[],"madenUyarisi":null,
                     "hareketSayisi":0,"gunler":[]}
                    """,
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") =>
                    """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""",
                var y when y.EndsWith("/raporlar/reyon-stok") => ReyonStok,
                var y when y.EndsWith("/raporlar/stok-yaslandirma") =>
                    """{"olculduAn":"2026-09-22T06:00:00Z","subeId":null,"kovalar":[],"toplamAdet":0,"altinHasGram":0,"madenDagilimi":[],"yasiBilinmeyenAdet":0,"devir":null,"oluStok":null,"guvenilirlik":"YUKSEK","uyarilar":[]}""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => Mutabakat,
                var y when y.EndsWith("/denetim/ozet") =>
                    """{"toplam":1,"hassasAdet":0,"enEskiKayit":"2026-09-01T08:00:00Z","eylemler":[{"eylem":"EKLEME","adet":1}],"turler":[{"varlikTuru":"KullaniciTercihi","adet":1}]}""",
                var y when y.EndsWith("/denetim") => t.denetim,
                var y when y.EndsWith("/kullanicilar") => """[{"id":"u1","ad":"Ayşe","soyad":"Yılmaz"}]""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik"}]""",
                var y when y.Contains("/raporlar/") => "{}",
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"},{"id":"c2","ad":"Mehmet Kaya"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Faturalar> CekmeceAc()
    {
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-duzenle-f9");
        e.Find("#fk-duzenle-f9").Click();
        e.WaitForElement("#fd-duzenle-tarih");
        return e;
    }

    /// <summary>(1) Liste ve çekmece AYNI günü gösterir (yerel gün); dokunmadan Kaydet PATCH
    /// gövdesine tarih KOYMAZ (sözleşme: gövdede yoksa koru); gün değişince koyar.</summary>
    [Fact]
    public void BK_Fatura_cekmece_tarihi_liste_gunuyle_ayni_ve_dokunulmazsa_gonderilmez()
    {
        Mod("PROFESYONEL");
        var e = CekmeceAc();
        var an = DateTime.Parse(faturaTarih, CultureInfo.InvariantCulture);
        // Tur 2: gün İŞLETME günüdür (Europe/Istanbul) — tarayıcının saat dilimi değil.
        var yerelGun = Bicim.IsletmeZamani(DateTime.SpecifyKind(an, DateTimeKind.Utc)).Date;
        // Liste hücresi (Bicim.Tarih) ile çekmece değeri aynı gün.
        Assert.Contains(Bicim.Tarih(an, "tr-TR"), e.Find("#fk-tablo").TextContent);
        // PAKET GOC-A: çekmece tarihi ortak Tarih (gg.aa.yyyy; K8a işlem penceresi) — gün AYNI.
        Assert.Equal(yerelGun.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                     e.Find("#fd-duzenle-tarih").GetAttribute("value"));
        Assert.NotEqual(an.Date, yerelGun);   // test gerçekten gün sınırını sınıyor (22:30Z = ertesi gün 01:30 TSI)

        e.Find("#cekmece-fatura-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/faturalar/f9")));
        var g1 = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.False(g1.TryGetProperty("tarih", out _));

        var e2 = CekmeceAc();
        e2.Find("#fd-duzenle-tarih").Change("28.09.2026");
        e2.Find("#cekmece-fatura-kaydet").Click();
        e2.WaitForAssertion(() => Assert.Equal(2, istekler.Count(i => i.Metot == "PATCH" && i.Yol.EndsWith("/faturalar/f9"))));
        var g2 = JsonDocument.Parse(istekler.Last(i => i.Metot == "PATCH").Govde).RootElement;
        Assert.Equal("2026-09-28", g2.GetProperty("tarih").GetString());
    }

    /// <summary>(2) Düzenle çekmecesi: defter bağlı kalemin adedi salt okunur + sebep + fişe bağlantı;
    /// bağsız kalemin adedi düzenlenebilir; fiyat alanları serbest.</summary>
    [Fact]
    public void BK_Fatura_cekmece_defter_bagli_kalem_adedi_salt_okunur()
    {
        Mod("BASIT");
        var e = CekmeceAc();
        e.WaitForElement("#fd-cekmece-kalem-ka");
        Assert.Empty(e.FindAll("#si-fatura-kalem-miktar-ka-ac"));
        Assert.Equal(Bicim.Miktar(1m, "ADET"), e.Find("#fd-cekmece-kalem-adet-kilit-ka").TextContent.Trim());
        Assert.Contains(KesifP3Sozlugu.Tr.AdetFistenGelir, e.Find("#fd-cekmece-kalem-adet-sebep-ka").TextContent);
        var bag = e.Find("#fd-cekmece-kalem-adet-fis-ka");
        Assert.Equal("/islemler", bag.GetAttribute("href"));
        Assert.Contains("hucre-tek-satir", bag.ClassList);   // "Fişten değiştir" iki satıra kırılmaz (K3)
        Assert.Equal(KesifP3Sozlugu.Tr.AdetFistenDegistir, bag.TextContent.Trim());
        Assert.NotNull(e.Find("#si-fatura-kalem-birimFiyat-ka-ac"));
        Assert.NotNull(e.Find("#si-fatura-kalem-miktar-kb-ac"));
        Assert.Empty(e.FindAll("#fd-cekmece-kalem-adet-kilit-kb"));
    }

    /// <summary>(3) PRO + düzenlenebilir (TASLAK) kalem tablosu zo-kalem-duzenlenir taşır (CSS: her
    /// genişlikte kart); BASİT ve kesilmiş belgede taşımaz.</summary>
    [Fact]
    public void BK_Fatura_PRO_taslak_kalem_tablosu_duzenlenir_sinifi()
    {
        Mod("PROFESYONEL");
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-goster-0");
        e.Find("#fk-goster-0").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("tr.fd-kalem-satir").Count));
        Assert.Contains("zo-kalem-duzenlenir", e.Find("#fd-kalem-tablosu").ClassList);

        faturaDurum = "KESILDI";
        var k = Render<Kuyumcu.Web.Pages.Faturalar>();
        k.WaitForElement("#fk-goster-0");
        k.Find("#fk-goster-0").Click();
        k.WaitForAssertion(() => Assert.Equal(2, k.FindAll("tr.fd-kalem-satir").Count));
        Assert.DoesNotContain("zo-kalem-duzenlenir", k.Find("#fd-kalem-tablosu").ClassList);
    }

    /// <summary>Tur 2 (DÜŞÜK): kalem penceresinin para hücreleri kuruş İKİ HANE — tam liralık
    /// "500" de "500,00 ₺" basılır (yanında "1.120,00 ₺" varken "500 ₺" olmaz); genel toplam da.</summary>
    [Fact]
    public void BK2_Fatura_kalem_para_hucreleri_iki_hane()
    {
        Mod("PROFESYONEL");
        faturaDurum = "KESILDI";
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-goster-0");
        e.Find("#fk-goster-0").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("tr.fd-kalem-satir").Count));
        var toplamlar = e.FindAll($"tr.fd-kalem-satir td[data-etiket=\"{Sozlukler.Tr.Fatura.SatirToplam}\"]")
            .Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal([Bicim.TlKurus(1120m), Bicim.TlKurus(500m)], toplamlar);
        Assert.EndsWith(",00\u00a0\u20ba", toplamlar[1]);
        // Tablonun BÜTÜN para hücreleri (birim fiyat, işçilik, has değer, KDV matrahı, KDV, satır) iki hane.
        var para = e.FindAll("#fd-kalem-tablosu td").Select(h => h.TextContent.Trim()).Where(x => x.EndsWith("\u20ba")).ToList();
        Assert.True(para.Count >= 8, $"para hücresi {para.Count}");
        Assert.All(para, x => Assert.Matches(@",\d{2}\u00a0\u20ba$", x));
        Assert.Equal(Bicim.TlKurus(1620m), e.Find("#fd-toplam").TextContent.Trim());
    }

    [Fact]
    public void BK_Fatura_BASIT_taslak_kalem_tablosu_duzenlenir_sinifi_yok()
    {
        Mod("BASIT");
        var e = Render<Kuyumcu.Web.Pages.Faturalar>();
        e.WaitForElement("#fk-goster-0");
        e.Find("#fk-goster-0").Click();
        e.WaitForAssertion(() => Assert.Equal(2, e.FindAll("tr.fd-kalem-satir").Count));
        Assert.DoesNotContain("zo-kalem-duzenlenir", e.Find("#fd-kalem-tablosu").ClassList);
    }

    /// <summary>(4) Reyon stok: her maden kendi satır öğesinde (zo-maden-satir → CSS blok);
    /// "has" ile sonraki maden adı bitişik tek metin düğümünde değil.</summary>
    [Fact]
    public void BK_Raporlar_reyon_stok_maden_ayri_satir()
    {
        Mod("PROFESYONEL");
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Equal(4, e.FindAll("#reyon-stok-kart .zo-maden-satir").Count));
        var satirlar = e.FindAll("#reyon-stok-kart .zo-maden-satir").Select(s => s.TextContent.Trim()).ToList();
        Assert.Equal($"Altın: {Bicim.Has(159.2652m)}", satirlar[0]);
        // MADEN-AYRIMI (ekip kararı 5): "has" yalnız altın; gümüş kendi biriminde (saf gram) — eski "83,2500 has" yanlıştı.
        Assert.Equal("Gümüş: saf 83,25 g", satirlar[1]);
        foreach (var hucre in e.FindAll("#reyon-stok-kart td.metin-amber"))
            Assert.All(hucre.Children, c => Assert.Contains("zo-maden-satir", c.ClassList));
    }

    /// <summary>(5) Gün sonu faturasız işlemler: ödeme kodu (VERESIYE/KART/NAKIT) görünmez,
    /// okunur ad; ham kod title'da.</summary>
    [Fact]
    public void BK_Raporlar_gun_sonu_faturasiz_odeme_okunur_ad()
    {
        Mod("PROFESYONEL");
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Equal(3, e.FindAll(".gs-faturasiz-odeme").Count));
        var hucreler = e.FindAll(".gs-faturasiz-odeme");
        var K = KatalogSozlugu.Al("tr");
        Assert.Equal([K.OdemeVeresiye, K.OdemeKart, K.OdemeNakit], hucreler.Select(h => h.TextContent.Trim()).ToArray());
        Assert.Equal(["VERESIYE", "KART", "NAKIT"], hucreler.Select(h => h.GetAttribute("title") ?? "").ToArray());
    }

    /// <summary>(6) Denetim izi: "menu-acik-gruplar" tercih anahtarı İKİ modda okunur ada çevrilir;
    /// ham anahtar ya da ş/ı'sız "menu acik gruplar" görünmez. PRO'da ham kayıt title'da.</summary>
    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void BK_DenetimIzi_tercih_anahtari_okunur(string mod)
    {
        Mod(mod);
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForAssertion(() => Assert.Single(c.FindAll("#dz-tablo tbody tr")));
        var tablo = c.Find("#dz-tablo").TextContent;
        Assert.Contains("menüde açık bölümler", tablo);
        Assert.DoesNotContain("menu-acik-gruplar", tablo);
        Assert.DoesNotContain("menu acik gruplar", tablo);
        if (mod == "PROFESYONEL")
            // PAKET GOC-D: dz-tablo ortak ızgarada; ham kayıt ipucu hücrenin İÇ öğesinde (td özniteliği satıra göre değişmez).
            Assert.Contains(c.FindAll("#dz-tablo td [title]"),
                td => td.GetAttribute("title") == "Arayüz tercihi güncellendi: menu-acik-gruplar");
    }

    /// <summary>Sözlük: beyaz listedeki her tercih anahtarının okunur adı var (TR+EN); tanınmayan metin değişmez.</summary>
    [Fact]
    public void BK_Tercih_anahtarlari_sozlukte_tam()
    {
        foreach (var a in Kuyumcu.Api.Modules.Tercih.TercihAnahtarlari.Tumu)
        {
            Assert.DoesNotContain(a, KesifP3Sozlugu.TercihAnahtarlariniCevir($"x: {a}", "tr"));
            Assert.DoesNotContain(a, KesifP3Sozlugu.TercihAnahtarlariniCevir($"x: {a}", "en"));
        }
        Assert.Equal("serbest metin", KesifP3Sozlugu.TercihAnahtarlariniCevir("serbest metin", "tr"));
    }
}

public sealed class BelgeKalanCssTestleri
{
    private static string Kod()
    {
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        return System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
    }

    [Fact]
    public void BK_css_kurallari()
    {
        var kod = Kod();
        Assert.Contains(".zo-maden-satir { display: block; }", kod);
        Assert.Contains("@container zokalem (max-width: 1600px) {\n  #fd-kalem-tablosu.zo-kalem-genis.zo-kalem-duzenlenir,", kod);
        Assert.Contains("#fd-kalem-tablosu.zo-kalem-genis.zo-kalem-duzenlenir > thead { display: none; }", kod);
        Assert.Contains("#fd-kalem-tablosu.zo-kalem-genis.zo-kalem-duzenlenir > tbody > tr { grid-template-columns: minmax(0, 1fr); }", kod);
        Assert.Contains("#fd-kalem-tablosu > tbody > tr > td:nth-child(2) .satir-ici { white-space: normal; min-width: 0; max-width: 100%; }", kod);
        Assert.Contains("#fd-kalem-tablosu > tbody > tr > td:nth-child(2) .si-tetik { white-space: normal; overflow-wrap: anywhere; max-width: 100%; }", kod);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// BELGE-KALAN TUR 2 — hasım doğrulayıcının kalan bulguları:
//   (ORTA)   Arama ucu 00:00–03:00 TSI belgesini UTC gününde (bir gün geri) basıyordu.
//   (DÜŞÜK)  İstemci günü tarayıcı saat dilimine bağlıydı → işletme günü (Europe/Istanbul).
//   (DÜŞÜK)  Kalem penceresi para hücreleri iki hane (45.751,50 ₺ yanında 3.200 ₺ olmaz).
//   (KRİTİK) Gider pusulası detay çekmecesi: tek birincil (Kaydet), Yazdır ikincil, iptal
//            ikincil + tehlike yazısı, madde numarası PRO title'da, gram listeyle 2 hane,
//            açıklama ve okuma izi notu kapalı ipucunda.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class BelgeKalanAramaHttpTestleri(HttpFikstur f)
{
    /// <summary>01:30 TSI'de kesilen belge (30.09 22:30 UTC) aramada İŞLETME günüyle (01.10.2026)
    /// görünür — liste ve çekmeceyle aynı gün (eski: 30.09.2026, bir gün geri).</summary>
    [Fact]
    public async Task BK2_Arama_fatura_gunu_isletme_gunudur()
    {
        Kiraci k;
        using (var kur = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(kur, "BK2-arama");
        string no;
        using (var db = f.Vt.Olustur(k))
        {
            var fs = new MFatura.FaturaServisi(db);
            var t = await fs.Olustur(new MFatura.FaturaOlusturDto
            {
                CariId = k.CariId, Toplam = 1_200m, KdvOran = 0.20m,
                Tarih = new DateTime(2026, 9, 30, 22, 30, 0, DateTimeKind.Utc),
            }, k.Islem);
            var id = (string)t.GetType().GetProperty("Id")!.GetValue(t)!;
            await fs.DurumGuncelle(id, new MFatura.DurumGuncelleDto { Durum = "KESILDI" }, k.Islem);
            no = db.Faturalar.AsNoTracking().Single(x => x.Id == id).FaturaNo!;
        }
        var cevap = await f.Ortam.Istemci(k).GetAsync($"/api/v1/arama?q={no}&tur=FATURA");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var kok = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement;
        var sonuc = kok.GetProperty("sonuclar").EnumerateArray().Single();
        var alt = sonuc.GetProperty("altBilgi").GetString()!;
        Assert.StartsWith("01.10.2026 ·", alt);
    }
}

public sealed class BelgeKalanTur2SafTestleri
{
    /// <summary>İşletme günü: UTC ve Unspecified (API "…T22:30:00") anı İstanbul'a çevrilir.</summary>
    [Fact]
    public void BK2_Bicim_isletme_gunu_Istanbul()
    {
        var utc = new DateTime(2026, 9, 30, 22, 30, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 10, 1, 1, 30, 0), Bicim.IsletmeZamani(utc));
        Assert.Equal(new DateTime(2026, 10, 1, 1, 30, 0), Bicim.IsletmeZamani(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)));
        Assert.Equal(new DateTime(2026, 9, 30, 23, 30, 0), Bicim.IsletmeZamani(utc.AddHours(-2)));
        // TOPLU-1 tur 2 (TARIH-TEK, tasarım hakemi KRİTİK): tek kalıp dd.MM.yyyy — "1.10.2026" (kültürün "d"si) değil.
        Assert.Equal("01.10.2026", Bicim.Tarih(utc, "tr-TR"));
    }

    /// <summary>Kaynak sözleşmesi: gün dönüşümü tarayıcının yerel saatine (ToLocalTime) değil
    /// işletme saat dilimine bağlıdır — test makinesi TSI'de olduğu için davranış testi bunu
    /// tek başına ayırt edemez; kaynak ölçülür.</summary>
    [Fact]
    public void BK2_istemci_gunu_isletme_dilimine_sabit()
    {
        var kok = CssKaynak.Kok();
        var bicim = File.ReadAllText(Path.Combine(kok, "Kuyumcu.Web", "Servisler", "Bicim.cs"));
        Assert.Contains("TimeZoneInfo.FindSystemTimeZoneById(\"Europe/Istanbul\")", bicim);
        Assert.Contains("IsletmeDilimi is { } dilim ? TimeZoneInfo.ConvertTimeFromUtc(utc, dilim)", bicim);
        // TOPLU-1 tur 2: Tarih tek kalıba (TarihTam = işletme günü "dd.MM.yyyy") bağlı.
        Assert.Contains("public static string Tarih(DateTime t, string locale) => TarihTam(t);", bicim);
        Assert.Contains("IsletmeZamani(an).ToString(\"dd'.'MM'.'yyyy\", CultureInfo.InvariantCulture)", bicim);
        var fatura = File.ReadAllText(Path.Combine(kok, "Kuyumcu.Web", "Pages", "Faturalar.razor"));
        Assert.Contains("internal static DateTime BelgeGunu(DateTime t) => Bicim.IsletmeZamani(t).Date;", fatura);
    }

    [Fact]
    public void BK2_css_ikincil_tehlike()
    {
        var css = File.ReadAllText(Path.Combine(CssKaynak.Kok(), "Kuyumcu.Web", "wwwroot", "css", "zo-belge.css"));
        Assert.Contains("html[data-mod] .cekmece .dugme.dugme-ikincil.zo-ikincil-tehlike { color: var(--tehlike); border-color: var(--tehlike-kenar); }", css);
    }
}

public sealed class BelgeKalanPusulaEkranTestleri : BunitContext
{
    private const string Liste = """
        [{"id":"gp1","pusulaNo":"GP-2026-000001","hareketId":null,"saticiTipi":"NIHAI_TUKETICI",
          "saticiAdSoyad":"Tuna Akarsu","saticiTckn":"*******0146","ayarId":"K14","gramAgirlik":3.1,
          "hasGrami":1.8073,"brutTutar":11875.47,"stopajOran":0.0,"stopajTutar":0.00,
          "netOdenen":11875.47,"teslimTarihi":"2099-09-01T00:00:00Z",
          "duzenlemeSonTarihi":"2099-09-08T00:00:00Z","gecikti":false,"kalanGun":30,
          "iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}]
        """;

    private const string Detay = """
        {"id":"gp1","pusulaNo":"GP-2026-000001","saticiTipi":"NIHAI_TUKETICI","saticiAdSoyad":"Tuna Akarsu",
         "saticiTckn":"10000000146","saticiAdres":"Kadıköy","ayarId":"K14","gramAgirlik":3.1,
         "hasGrami":1.8073,"malAciklamasi":"14 ayar hurda yüzük","brutTutar":11875.47,
         "stopajOran":0.0,"stopajTutar":0.00,"netOdenen":11875.47,
         "teslimTarihi":"2099-09-01T00:00:00Z","duzenlemeSonTarihi":"2099-09-08T00:00:00Z",
         "subeId":null,"cariId":null,"hareketId":null,"duzeltilenId":null,
         "olusturuldu":"2099-09-01T00:00:00Z","iptalEdildi":false,"iptalGerekce":null,"iptalTarihi":null}
        """;

    public BelgeKalanPusulaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/gider-pusulalari/sure-takibi") => """
                    {"sure":{"gun":7,"dayanak":"VUK md.234"},"gecikmisSayisi":0,"yaklasanSayisi":0,
                     "gecikmis":[],"yaklasan":[],"uyarilar":[]}
                    """,
                var y when y.Contains("/gider-pusulalari/") => Detay,
                var y when y.EndsWith("/gider-pusulalari") => Liste,
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6827.20,"gramAltinFiyati":6861.51}""",
                _ => "[]",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.GiderPusulalari> Ac(string mod)
    {
        var m = Services.GetRequiredService<ArayuzModuServisi>();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => m.BasitMi);
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "sb1", mod, mod));
        var c = Render<Kuyumcu.Web.Pages.GiderPusulalari>().AyrintiAc();
        c.WaitForState(() => c.FindAll("#gp-tablo tbody tr").Count == 1);
        c.Find("#gp-detay-gp1").Click();
        c.WaitForElement("#gp-detay-mal");
        return c;
    }

    /// <summary>Çekmecede TEK birincil düğme (alt çubuktaki Kaydet); Yazdır ikincil; yıkıcı iptal
    /// dolu kırmızı değil (ikincil + tehlike yazısı), adı sade, madde numarası PRO'da title'da;
    /// tıklanınca ortak onay modalı açılır.</summary>
    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void BK2_Pusula_detay_tek_birincil_iptal_ikincil(string mod)
    {
        var c = Ac(mod);
        var birinciller = c.FindAll("#cekmece-gider-pusulasi .dugme-birincil");
        Assert.Single(birinciller);
        Assert.Equal("cekmece-gider-pusulasi-kaydet", birinciller[0].Id);
        var yazdir = c.Find("#cekmece-gider-pusulasi #gp-yazdir-gp1");   // satır eylem menüsünde de aynı kimlik var (önceden kalma)
        Assert.Contains("dugme-ikincil", yazdir.ClassList);
        var iptal = c.Find("#cekmece-gider-pusulasi #gp-iptal-gp1");
        Assert.DoesNotContain("dugme-tehlike", iptal.ClassList);
        Assert.Contains("dugme-ikincil", iptal.ClassList);
        Assert.Contains("zo-ikincil-tehlike", iptal.ClassList);
        Assert.Equal(KesifP3Sozlugu.Tr.PusulaIptalEtSade, iptal.TextContent.Trim());
        if (mod == "BASIT") Assert.Null(iptal.GetAttribute("title"));
        else Assert.Equal(Sozlukler.Tr.Pusula.IptalEt, iptal.GetAttribute("title"));
        iptal.Click();
        c.WaitForElement("#gp-iptal-onay");
        Assert.NotNull(c.Find("#gp-iptal-onayla"));
    }

    /// <summary>Gram listeyle aynı 2 ondalık; "neyi düzeltebilirim" açıklaması ve PRO okuma izi
    /// notu çekmece gövdesinde değil, kapalı #gp-degismez ipucunda.</summary>
    [Theory]
    [InlineData("BASIT")]
    [InlineData("PROFESYONEL")]
    public void BK2_Pusula_detay_gram_iki_hane_aciklama_kapali(string mod)
    {
        var c = Ac(mod);
        Assert.Contains("· 3,10 g →", c.Find("#gp-detay-mal").TextContent);
        Assert.Contains("· 3,10 g", c.Find("#gp-mal-gp1").TextContent);
        var kapali = c.Find("#gp-degismez");
        Assert.False(kapali.HasAttribute("open"));
        Assert.NotNull(c.Find("#gp-degismez #gp-duzenlenebilir-aciklama"));
        Assert.Single(c.FindAll("#gp-duzenlenebilir-aciklama"));
        if (mod == "BASIT")
            Assert.Empty(c.FindAll("#gp-okuma-izi-notu"));
        else
            Assert.NotNull(c.Find("#gp-degismez #gp-okuma-izi-notu"));
        // Çekmece gövdesinde (kapalı ipucunun dışında) KVKK notu yok.
        var govde = c.Find("#cekmece-gider-pusulasi-govde").TextContent;
        var ipucu = kapali.TextContent;
        Assert.DoesNotContain("KVKK", govde.Replace(ipucu, ""));
    }
}
