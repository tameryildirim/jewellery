using System.Net;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Contracts.Bilesenler.MasakFormu;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using MHurda = Kuyumcu.Api.Modules.Hurda;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET TCKN-DOGRULAMA (08.10, kurucu bulgusu: Değişim satıcı TCKN'ye 11111111111
// yazılabiliyordu). Tek kaynak Kuyumcu.Core KimlikNoDogrulayici; sunucu uçları
// 400 + alanlar.<ad> döner ve satır YAZMAZ; ekran alan altında anında söyler,
// Kaydet kilidinin sebebi aynı metindir.
// ⚠ Test numaraları gerçek kişiye ait olmayan, kuraldan geçen/geçmeyen sentetik
//   örneklerdir (10000000146, 19090909018, 99000000042 …). KVKK: gerçek numara yok.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf: doğrulayıcının sınırları.</summary>
public sealed class TcknDogrulayiciTestleri
{
    [Theory]
    [InlineData("10000000146", true)]
    [InlineData("12345678950", true)]
    [InlineData("20000000046", true)]
    [InlineData("19090909018", true)]    // 7×tek − çift < 0: eski '%' kopyası GEÇERLİ numarayı reddediyordu
    [InlineData("99000000042", true)]    // YKN (99 ile başlar) — aynı kural
    [InlineData("11111111111", false)]   // kurucu bulgusu
    [InlineData("12345678901", false)]
    [InlineData("99999999999", false)]
    [InlineData("99000000041", false)]   // YKN, 11. hane yanlış
    [InlineData("10000000156", false)]   // 10. hane yanlış
    [InlineData("10000000157", false)]   // YALNIZ 10. hane yanlış (11. hane ilk 10'un toplamıyla tutarlı) — M1-1 açığını kapatır
    [InlineData("01000000146", false)]   // ilk hane 0
    [InlineData("00000000000", false)]
    [InlineData("1000000014", false)]    // 10 hane
    [InlineData("100000001466", false)]  // 12 hane
    [InlineData("1000000014A", false)]   // harf
    [InlineData("١٠٠٠٠٠٠٠١٤٦", false)]   // Arapça-Hint rakamları (Unicode \d) — ASCII değil
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TcknGecerli_sinirlari(string? no, bool beklenen) =>
        Assert.Equal(beklenen, KimlikNoDogrulayici.TcknGecerli(no));

    [Fact]
    public void Gider_pusulasi_kapisi_ayni_tek_kaynagi_kullanir() =>
        // Eski yerel kopya negatif kalanda 19090909018'i reddediyordu; artık Core'a bağlı.
        Assert.True(MHurda.GiderPusulasiKapisi.TcknGecerli("19090909018"));

    [Theory]
    [InlineData("99000000042", true)]
    [InlineData("10000000146", false)]
    [InlineData("99000000041", false)]
    public void YknMi(string no, bool beklenen) => Assert.Equal(beklenen, KimlikNoDogrulayici.YknMi(no));

    [Theory]
    [InlineData("1234567890", true)]
    [InlineData("123456789", false)]
    [InlineData("12345678901", false)]
    [InlineData("123456789A", false)]
    public void Vkn_yalniz_bicim_TEYITSIZ(string no, bool beklenen) =>
        Assert.Equal(beklenen, KimlikNoDogrulayici.VknBicimGecerli(no));

    [Theory]
    [InlineData(null, KimlikNoHatasi.Yok)]
    [InlineData("  ", KimlikNoHatasi.Yok)]
    [InlineData("1234567890", KimlikNoHatasi.Yok)]
    [InlineData("10000000146", KimlikNoHatasi.Yok)]
    [InlineData("11111111111", KimlikNoHatasi.TcknGecersiz)]
    [InlineData("123", KimlikNoHatasi.HaneSayisi)]
    [InlineData("12a4567890", KimlikNoHatasi.RakamDisi)]
    public void TcknVeyaVknDenetle(string? no, KimlikNoHatasi beklenen) =>
        Assert.Equal(beklenen, KimlikNoDogrulayici.TcknVeyaVknDenetle(no));

    [Theory]
    [InlineData("11111111111", KimlikNoHatasi.TcknGecersiz)]
    [InlineData("1111111111", KimlikNoHatasi.HaneSayisi)]
    [InlineData("10000000146", KimlikNoHatasi.Yok)]
    public void TcknDenetle(string no, KimlikNoHatasi beklenen) =>
        Assert.Equal(beklenen, KimlikNoDogrulayici.TcknDenetle(no));

    [Fact]
    public void Cari_kurali_KimlikNo_Dogrula_TCKN_algoritmasini_uygular()
    {
        Assert.Equal("TC kimlik numarası geçerli değil", KimlikNo.Dogrula("11111111111", KimlikNo.Tckn));
        Assert.Equal("TC kimlik numarası geçerli değil", KimlikNo.Dogrula("11111111111", null));   // tür uzunluktan
        Assert.Null(KimlikNo.Dogrula("10000000146", KimlikNo.Tckn));
        Assert.Null(KimlikNo.Dogrula("1111111111", KimlikNo.Vkn));   // VKN: yalnız hane (TEYİTSİZ)
    }

    [Fact]
    public void Isletme_kurali_VergiKimlik_11_hanede_algoritma()
    {
        Assert.Equal(VergiKimlikHatasi.TcknGecersiz, VergiKimlik.Denetle("11111111111"));
        Assert.Equal("TC kimlik numarası geçerli değil", VergiKimlik.Mesaj("11111111111"));
        Assert.Null(VergiKimlik.Mesaj("10000000146"));
        Assert.Null(VergiKimlik.Mesaj("1111111111"));
        Assert.Equal("TC kimlik numarası geçerli değil.", IsletmeKimlikSozlugu.VergiNoMesaji("tr", "11111111111"));
        Assert.Equal("The national ID number is not valid.", IsletmeKimlikSozlugu.VergiNoMesaji("en", "11111111111"));
    }

    [Fact]
    public void Gider_pusulasi_kapisi_400_alan_tckn()
    {
        var k = MHurda.GiderPusulasiKapisi.Varsayilan();
        var ex = Assert.Throws<BadRequestException>(() => k.Dogrula(new MHurda.HurdaSaticiDto
        {
            SaticiTipi = "NIHAI_TUKETICI", AdSoyad = "Ayşe Yılmaz", Tckn = "11111111111", Adres = "Kadıköy",
        }));
        Assert.Contains("TC kimlik numarası geçerli değil", ex.Message);
        Assert.Equal("tckn", AlanIsareti.Alan(ex));

        // Geçerli numara: kapı açılır.
        Assert.Equal("NIHAI_TUKETICI", k.Dogrula(new MHurda.HurdaSaticiDto
        {
            SaticiTipi = "NIHAI_TUKETICI", AdSoyad = "Ayşe Yılmaz", Tckn = "10000000146", Adres = "Kadıköy",
        }));
    }
}

/// <summary>İstemci modelleri: alan altı metin ve kilit sebebi aynı tek kaynaktan.</summary>
public sealed class TcknIstemciModelTestleri
{
    private static readonly SozlukIslem T = Sozlukler.Tr.Islem;

    [Fact]
    public void Satici_TCKN_11_hane_gecersiz_anında_hata_ve_kilit()
    {
        var s = new HurdaGirisi { SaticiTipi = "NIHAI_TUKETICI", SaticiAd = "Ayşe", SaticiTckn = "11111111111", SaticiAdres = "Kadıköy" };
        Assert.Equal("TC kimlik numarası geçerli değil.", s.TcknHata(T));
        Assert.True(s.TcknAlgoritmaHatasi);
        Assert.False(s.SaticiTamam(T));
        Assert.Equal("TC kimlik numarası geçerli değil.", FisTakasSatici.EksikMetni(s, "tr", true, T));
        Assert.Equal("The national ID number is not valid.", s.TcknHata(Sozlukler.En.Islem));

        s.SaticiTckn = "10000000146";
        Assert.Null(s.TcknHata(T));
        Assert.False(s.TcknAlgoritmaHatasi);
        Assert.True(s.SaticiTamam(T));
    }

    [Fact]
    public void Altin_bacagi_satici_TCKN_gecersiz_alan_alti_ve_eksik_listesi()
    {
        var ayar = new AltinBacagiAyarlari([new(AltinBacagiGirisi.NihaiTuketici, 0m, true)], null, false);
        var g = new AltinBacagiGirisi
        {
            SaticiTipi = AltinBacagiGirisi.NihaiTuketici, AyarId = "K22", Gram = 10m, Fiyat = 3000m,
            AdSoyad = "Ayşe", Tckn = "11111111111",
        };
        Assert.Equal("TC kimlik numarası geçerli değil.", g.TcknHata(ayar, T));
        g.Tckn = "10000000146";
        Assert.Null(g.TcknHata(ayar, T));
    }

    [Fact]
    public void Mukellef_saticida_TCKN_sorulmaz()
    {
        var s = new HurdaGirisi { SaticiTipi = "MUKELLEF", SaticiTckn = "11111111111", TedarikciFaturaNo = "A1" };
        Assert.Null(s.TcknHata(T));
        Assert.False(s.TcknAlgoritmaHatasi);
    }

    [Fact]
    public void Masak_TC_kimlik_belge_no_kilit_ve_alan_hatasi()
    {
        var g = new MasakGereksinim("YALNIZ_KIMLIK", true, false, false, false, 20);
        var m = new MasakFormu.MasakBilgi { BelgeTuru = "TC_KIMLIK", BelgeNo = "11111111111", AdSoyad = "Ali Veli" };
        Assert.False(MasakFormu.Eksiksiz(m, g));
        Assert.Equal("TC kimlik numarası geçerli değil.", MasakFormu.TcknKilitMetni(m, "tr"));
        Assert.Equal("TC kimlik numarası geçerli değil.", MasakFormu.TcknAlanHatasi(m, "tr"));

        // Yazarken (10 hane) alan altı kırmızı yok; kilit yine kapalı.
        m.BelgeNo = "1111111111";
        Assert.Null(MasakFormu.TcknAlanHatasi(m, "tr"));
        Assert.False(MasakFormu.Eksiksiz(m, g));

        m.BelgeNo = "10000000146";
        Assert.True(MasakFormu.Eksiksiz(m, g));
        Assert.Null(MasakFormu.TcknKilitMetni(m, "tr"));

        // Pasaport bu kurala TABİ DEĞİL.
        m.BelgeTuru = "PASAPORT"; m.BelgeNo = "11111111111";
        Assert.True(MasakFormu.Eksiksiz(m, g));
        Assert.Null(MasakFormu.TcknKilitMetni(m, "tr"));
    }
}

/// <summary>bUnit: MASAK kimlik formunda 11111111111 yazınca alan altında ANINDA hata.</summary>
public sealed class TcknMasakFormuEkranTestleri : BunitContext
{
    public TcknMasakFormuEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    [Fact]
    public void TC_kimlik_belge_no_gecersiz_alan_altinda_aninda()
    {
        var model = new MasakFormu.MasakBilgi { BelgeTuru = "TC_KIMLIK" };
        var e = Render<MasakFormu>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Tutar, 400_000m)
            .Add(c => c.Esik, new Esikler(370_000m, 370_000m, 2_000_000.01m, 20_000_000.01m, 20, ["TC_KIMLIK", "PASAPORT"]))
            .Add(c => c.Gereksinim, new MasakGereksinim("YALNIZ_KIMLIK", true, false, false, false, 20)));

        Assert.Empty(e.FindAll("#masak-belge-no-hata"));
        e.Find("#masak-belge-no").Input("11111111111");
        e.WaitForAssertion(() => Assert.Equal("TC kimlik numarası geçerli değil.", e.Find("#masak-belge-no-hata").TextContent.Trim()));

        e.Find("#masak-belge-no").Input("10000000146");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#masak-belge-no-hata")));
    }
}

/// <summary>HTTP: her giriş ucunda 11111111111 → 400 alanlı, satır yazılmaz; geçerli numara geçer.</summary>
[Collection("http")]
public sealed class TcknUcTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "TCKN " + ad));
    }

    private static async Task Alan400(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        var mesaj = j.GetProperty("message");
        var metin = mesaj.ValueKind == System.Text.Json.JsonValueKind.Array
            ? string.Join(" ", mesaj.EnumerateArray().Select(x => x.GetString())) : mesaj.GetString();
        Assert.Contains("TC kimlik numarası geçerli değil", metin);
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "400 gövdesinde 'alanlar' yok: " + govde);
        Assert.True(alanlar.TryGetProperty(alan, out _), $"alanlar.{alan} yok: " + govde);
    }

    [Fact]
    public async Task Cari_POST_gecersiz_400_satir_yok_gecerli_basarili()
    {
        var k = Kur("cari post");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Cariler.Count(x => x.TenantId == k.TenantId); }
        var once = Sayi();
        await Alan400(await c.PostJson("/api/v1/cari", new { ad = "Ali Veli", tur = "MUSTERI", vergiNo = "11111111111" }), "vergiNo");
        Assert.Equal(once, Sayi());

        var ok = await c.PostJson("/api/v1/cari", new { ad = "Ali Veli", tur = "MUSTERI", vergiNo = "10000000146", kimlikTuru = "TCKN" });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        Assert.Equal(once + 1, Sayi());
    }

    [Fact]
    public async Task Cari_PATCH_gecersiz_400_deger_korunur()
    {
        var k = Kur("cari patch");
        var c = f.Ortam.Istemci(k);
        await Alan400(await c.PatchJson($"/api/v1/cari/{k.CariId}", new { vergiNo = "11111111111" }), "vergiNo");
        using var db = f.Vt.OlusturSistem();
        Assert.NotEqual("11111111111", db.Cariler.Single(x => x.Id == k.CariId && x.TenantId == k.TenantId).VergiNo);
    }

    [Fact]
    public async Task Isletme_kimlik_PATCH_gecersiz_400()
    {
        var k = Kur("isletme");
        var c = f.Ortam.Istemci(k);
        await Alan400(await c.PatchJson("/api/v1/admin/tenant/kimlik", new { vergiNo = "11111111111" }), "vergiNo");
        var ok = await c.PatchJson("/api/v1/admin/tenant/kimlik", new { vergiNo = "10000000146" });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Hurda_alis_satici_gecersiz_400_hareket_yok()
    {
        var k = Kur("hurda");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Hareketler.Count(x => x.TenantId == k.TenantId); }
        var once = Sayi();
        object Govde(string tckn) => new
        {
            cariId = k.CariId, ayarId = "K22", gramAgirlik = 10m,
            birimFiyatTlHasGram = 2_800m, kasaId = k.KasaId, kurSnapshotId = k.KurId,
            satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ayşe Yılmaz", tckn, adres = "Kadıköy" },
        };
        await Alan400(await c.PostJson("/api/v1/islemler/hurda-alis", Govde("11111111111")), "tckn");
        Assert.Equal(once, Sayi());
        var ok = await c.PostJson("/api/v1/islemler/hurda-alis", Govde("10000000146"));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Degisim_takas_satici_gecersiz_400()
    {
        var k = Kur("takas");
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/takas", new
        {
            cariId = k.CariId, eskiUrunAyarId = "K22", eskiUrunGram = 3, eskiUrunBirimFiyatTlHasGram = 3000,
            yeniUrunId = k.UrunId, yeniUrunAdet = 1, yeniUrunBirimFiyatTl = 20000,
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
            satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ayşe Yılmaz", tckn = "11111111111", adres = "Kadıköy" },
        });
        await Alan400(r, "tckn");
    }

    [Fact]
    public async Task Evrak_borclu_kimlik_gecersiz_400_satir_yok()
    {
        var k = Kur("evrak");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.KiymetliEvraklar.Count(x => x.TenantId == k.TenantId); }
        object Govde(string kimlik, string no) => new
        {
            evrakTuru = "CEK", tutar = 10_000m, vadeGunu = Zaman.YerelBugun().AddDays(20).ToString("yyyy-MM-dd"),
            evrakNo = no, bankaAdi = "Ziraat", kaynakCariId = k.CariId, kurSnapshotId = k.KurId,
            borcluKimlikNo = kimlik,
        };
        await Alan400(await c.PostJson("/api/v1/evraklar", Govde("11111111111", "CEK-T1")), "borcluKimlikNo");
        Assert.Equal(0, Sayi());
        var ok = await c.PostJson("/api/v1/evraklar", Govde("10000000146", "CEK-T2"));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        Assert.Equal(1, Sayi());
    }

    [Fact]
    public async Task Masak_TC_kimlik_belge_no_gecersiz_400_hareket_yok_pasaport_tabi_degil()
    {
        var k = Kur("masak");
        var c = f.Ortam.Istemci(k);
        int Sayi() { using var db = f.Vt.OlusturSistem(); return db.Hareketler.Count(x => x.TenantId == k.TenantId); }
        var once = Sayi();
        object Govde(string tur) => new
        {
            odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId, cariId = k.CariId,
            urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000_000m,
            masak = new { belgeTuru = tur, belgeNo = "11111111111", adSoyad = "Ali Veli" },
        };
        await Alan400(await c.PostJson("/api/v1/islemler/satis", Govde("TC_KIMLIK")), "belgeNo");
        Assert.Equal(once, Sayi());

        // Pasaport: aynı numara TCKN kuralına tabi değil → red (varsa) belgeNo alanına işaretlenmez.
        var p = await c.PostJson("/api/v1/islemler/satis", Govde("PASAPORT"));
        var pg = await p.Content.ReadAsStringAsync();
        Assert.DoesNotContain("TC kimlik numarası geçerli değil", pg);
    }
}
