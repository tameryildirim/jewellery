using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MTevsik = Kuyumcu.Api.Modules.Tevsik;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-1 — HTTP boru hattı testleri.
// K-3 (tablo boş → 503, hareket 0) · K-4 (ileri tarihli satır: bugün 400,
// ertesi gün 200, yeniden başlatma yok) · K-5 (geçmiş/bugün 409, JWT 403) ·
// K-6 (aynı başlangıç 409) · K-7 (appsettings ≠ tablo → sunucu açılmaz) ·
// K-9 (yaklaşan uç) · rol kapıları.
// ⚠ Paylaşılan test veritabanına ileri tarihli satır yazan test, finally'de
//   satırı siler ve önceki satırı yeniden açar (tohum bozulmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class YasalParametreHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETYASAL1-TEST-" + ad);
    }

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    private static HttpClient TokenliIstemci(HttpClient c, Kiraci k, string rol)
    {
        var token = new TokenServisi(new JwtAyarlar
        {
            Secret = HttpOrtami.TestSirri, AccessDakika = 15, RefreshGun = 7,
        }).AccessUret(k.KullaniciId, k.TenantId, k.SubeId, rol);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private static object NakitSatis(Kiraci k, decimal tutar) => new
    {
        cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = tutar,
        odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
    };

    private DateOnly Bugun => YasalParametreServisi.IslemGunu(DateTime.UtcNow);

    private static string G(DateOnly g) => g.ToString("yyyy-MM-dd");

    // YASAL-PARAM-EKRAN: Resmî Gazete künyesi zorunlu (tarih ≤ bugün ≤ başlangıç).
    private Dictionary<string, object?> Govdesi(string anahtar, decimal deger, DateOnly bas) => new()
    {
        ["anahtar"] = anahtar, ["degerSayi"] = deger, ["baslangic"] = G(bas),
        ["dayanak"] = "YASAL-1 test satırı (silinecek)", ["kaynakUrl"] = "https://www.resmigazete.gov.tr/test",
        ["teyit"] = "TEYITSIZ", ["aciklamaSade"] = "Test", ["gerekce"] = "YASAL-1 HTTP testi ileri tarihli satır denemesi",
        ["resmiGazeteTarihi"] = G(Bugun), ["resmiGazeteSayisi"] = "33393",
    };

    /// <summary>
    /// YASAL-PARAM-EKRAN — iki adımlı yazma: önizleme (hiçbir şey yazmaz) → onay koduyla kayıt.
    /// Önizleme 200 değilse onun yanıtı döner (doğrulama kuralları iki adımda AYNIDIR).
    /// </summary>
    private static async Task<HttpResponseMessage> Yaz(HttpClient c, Dictionary<string, object?> govde)
    {
        var on = await c.PostAsJsonAsync("/api/v1/sistem/yasal-parametreler/onizleme", govde);
        if (on.StatusCode != HttpStatusCode.OK) return on;
        var kod = (await Govde(on)).GetProperty("onayKodu").GetString();
        return await c.PostAsJsonAsync("/api/v1/sistem/yasal-parametreler",
            new Dictionary<string, object?>(govde) { ["onayKodu"] = kod });
    }

    /// <summary>İleri tarihli test satırını siler, önceki satırı yeniden açar, önbelleği tazeler.</summary>
    private void TevsikTohumunuGeriYukle()
    {
        using var db = f.Vt.OlusturSistem();
        db.Database.ExecuteSqlRaw(
            "DELETE FROM yasal_parametreler WHERE anahtar = 'TEVSIK_HADDI_TL' AND id NOT LIKE 'yasal1-tohum-%'");
        db.Database.ExecuteSqlRaw(
            "UPDATE yasal_parametreler SET gecerlilik_bitis = NULL WHERE id = 'yasal1-tohum-02'");
        f.Ortam.Services.GetRequiredService<YasalParametreServisi>().Tazele();
    }

    // ── K-3 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task K3_tablo_bosken_tevsik_kapisi_503_doner_ve_hareket_YAZILMAZ()
    {
        var k = YeniKiraci("bos");
        // Tevsik satırı OLMAYAN tablo (MASAK satırları var: 503'ün kaynağı tevsik kapısıdır).
        using var ortam = f.Ortam.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<YasalParametreServisi>();
            s.AddSingleton(YasalParametreServisi.Yukleyicili(() =>
            {
                using var db = f.Vt.OlusturSistem();
                return db.YasalParametreler.AsNoTracking()
                    .Where(p => p.Anahtar != YasalAnahtar.TevsikHaddiTl)
                    .AsEnumerable().Select(YasalParametreSatiri.Kaynaktan).ToList();
            }));
        }));
        var c = TokenliIstemci(ortam.CreateClient(), k, "SAHIP");

        var yanit = await c.PostAsJsonAsync("/api/v1/islemler/satis", NakitSatis(k, 1_000m));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, yanit.StatusCode);
        var g = await Govde(yanit);
        Assert.Equal("Yasal sınır tanımlı değil; destek ile görüşün.", g.GetProperty("message").GetString());
        Assert.Equal(503, g.GetProperty("statusCode").GetInt32());

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId));
    }

    // ── K-4 + K-9 ────────────────────────────────────────────────────────────

    [Fact]
    public async Task K4_K9_yarin_baslayan_40000_bugun_30000_01_nakit_400_yarin_35000_gecer_yeniden_baslatmasiz()
    {
        var k = YeniKiraci("ileri");
        var yarin = Bugun.AddDays(1);
        try
        {
            var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
            var ekle = await Yaz(sis,
                Govdesi(YasalAnahtar.TevsikHaddiTl, 40_000m, yarin));
            Assert.Equal(HttpStatusCode.Created, ekle.StatusCode);
            var eg = await Govde(ekle);
            Assert.Equal("yasal1-tohum-02", eg.GetProperty("kapananSatirId").GetString());

            // Bugün hâlâ 30.000: 30.000,01 nakit REDDEDİLİR.
            var sahip = f.Ortam.Istemci(k, "SAHIP");
            var red = await sahip.PostAsJsonAsync("/api/v1/islemler/satis", NakitSatis(k, 30_000.01m));
            Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
            Assert.Contains("nakit sınırı", (await Govde(red)).GetProperty("message").GetString());

            // Önizleme ucu: bugün 30.000, yarın 40.000.
            var bugunHad = await Govde(await sahip.GetAsync("/api/v1/tevsik/gunluk"));
            Assert.Equal(30_000m, bugunHad.GetProperty("had").GetDecimal());
            var yarinHad = await Govde(await sahip.GetAsync($"/api/v1/tevsik/gunluk?tarih={G(yarin)}"));
            Assert.Equal(40_000m, yarinHad.GetProperty("had").GetDecimal());

            // K-9: yaklaşan uç yarınki değişikliği döner (TEZGÂHTAR da çağırabilir — kişisel veri yok).
            var yak = await Govde(await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/yasal-parametreler/yaklasan"));
            var d = yak.GetProperty("degisiklikler").EnumerateArray()
                .Single(x => x.GetProperty("anahtar").GetString() == YasalAnahtar.TevsikHaddiTl);
            Assert.Equal(1, d.GetProperty("kalanGun").GetInt32());
            Assert.Equal(40_000m, d.GetProperty("yeniSayi").GetDecimal());
            Assert.Equal(30_000m, d.GetProperty("eskiSayi").GetDecimal());
            Assert.Equal(G(yarin), d.GetProperty("baslangic").GetString());

            // GECE YARISI (yeniden başlatma YOK): aynı süreçteki aynı singleton kural,
            // yerel 00:00'ı geçen bir işlem anında YENİ haddi uygular.
            var kural = f.Ortam.Services.GetRequiredService<MTevsik.TevsikKurali>();
            var kapi = new MTevsik.TevsikKapisi(kural);
            using var db = f.Vt.Olustur(k);
            var geceYarisiSonrasi = Zaman.YerelGunBasiUtc(yarin).AddMinutes(1);
            var sonuc = await kapi.Dogrula(db, k.TenantId, k.CariId, 35_000m, geceYarisiSonrasi);
            Assert.False(sonuc.Gerekli);
            Assert.Equal(40_000m, sonuc.Had);
            // Aynı tutar bugün hâlâ reddedilir.
            await Assert.ThrowsAsync<BadRequestException>(() =>
                kapi.Dogrula(db, k.TenantId, k.CariId, 35_000m, Zaman.YerelGunBasiUtc(yarin).AddMinutes(-1)));
        }
        finally
        {
            TevsikTohumunuGeriYukle();
        }
    }

    // ── K-5 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task K5_gecmis_baslangic_400_JWT_ile_403_anahtarsiz_401()
    {
        // YASAL-PARAM-EKRAN: geçmiş 409 → 400 (alan işaretli); BUGÜN başlayan artık YAZILABİLİR —
        // o yol YasalParamEkranTestleri'nde (temizlikli) sınanır.
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var dun = await Yaz(sis,
            Govdesi(YasalAnahtar.TevsikHaddiTl, 50_000m, Bugun.AddDays(-1)));
        Assert.Equal(HttpStatusCode.BadRequest, dun.StatusCode);
        var dg = await Govde(dun);
        Assert.Contains("Geçmiş", dg.GetProperty("message").GetString());
        Assert.True(dg.GetProperty("alanlar").TryGetProperty("baslangic", out _));

        var k = YeniKiraci("jwt");
        var jwt = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/sistem/yasal-parametreler",
            Govdesi(YasalAnahtar.TevsikHaddiTl, 50_000m, Bugun.AddDays(3)));
        Assert.True(jwt.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden, jwt.StatusCode.ToString());

        var anahtarsiz = await f.Ortam.SistemIstemci(null).PostAsJsonAsync("/api/v1/sistem/yasal-parametreler",
            Govdesi(YasalAnahtar.TevsikHaddiTl, 50_000m, Bugun.AddDays(3)));
        Assert.True(anahtarsiz.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden, anahtarsiz.StatusCode.ToString());

        // Hiçbiri yazmadı.
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.YasalParametreler.Count(p => p.Anahtar == YasalAnahtar.TevsikHaddiTl && p.DegerSayi == 50_000m));
    }

    [Fact]
    public async Task K5_beyaz_liste_disi_anahtar_400_alan_isaretli()
    {
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var c = await Yaz(sis,
            Govdesi("UYDURMA_ANAHTAR", 1m, Bugun.AddDays(5)));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.True((await Govde(c)).GetProperty("alanlar").TryGetProperty("anahtar", out _));
    }

    // ── Tur 2 (hasım ORTA-3): kodun henüz tarihli OKUMADIĞI anahtara yazma yok ──

    [Fact]
    public async Task Tur2_tablodan_OKUNMAYAN_anahtara_ileri_tarihli_yazma_409_ve_satir_yazilmaz()
    {
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        foreach (var anahtar in new[] { YasalAnahtar.GiderPusulasiGun, YasalAnahtar.StopajEsnafMuafHurdaOran,
                     YasalAnahtar.StopajBeyanGunu, YasalAnahtar.KdvGenelOran, YasalAnahtar.FaturaHaddiTl })
        {
            var c = await Yaz(sis, Govdesi(anahtar, 9m, Bugun.AddDays(5)));
            Assert.Equal(HttpStatusCode.Conflict, c.StatusCode);
            Assert.Contains("henüz tarihli okumuyor", (await Govde(c)).GetProperty("message").GetString());
        }
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.YasalParametreler.Count(p => p.Dayanak == "YASAL-1 test satırı (silinecek)"));
    }

    // ── Tur 2 (tasarım hakemi KRİTİK): dayanak yalnız resmî atıf, iç not API'de yok ──

    [Fact]
    public async Task Tur2_liste_dayanakta_ic_not_YOK_ic_not_alani_donmez_ayin_gunu_ve_kullanilmayan_isaretli()
    {
        var k = YeniKiraci("icnot");
        var g = await Govde(await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/yasal-parametreler"));
        var satirlar = g.GetProperty("satirlar").EnumerateArray().ToList();
        foreach (var x in satirlar)
        {
            var d = x.GetProperty("dayanak").GetString() ?? "";
            foreach (var ic in new[] { "şartname", "pakette", "doğrulanamadı", "okunmadı", "KAPALI", "kodlanmadı", "bulunamadı", "UBL" })
                Assert.DoesNotContain(ic, d, StringComparison.OrdinalIgnoreCase);
            Assert.False(x.TryGetProperty("icNot", out _));
        }
        JsonElement Bul(string a) => satirlar.Single(x => x.GetProperty("anahtar").GetString() == a);
        Assert.Equal("AYIN_GUNU", Bul(YasalAnahtar.StopajBeyanGunu).GetProperty("birim").GetString());
        Assert.False(Bul(YasalAnahtar.FaturaHaddiTl).GetProperty("kullaniliyor").GetBoolean());
        Assert.False(Bul(YasalAnahtar.StopajBeyanGunu).GetProperty("kullaniliyor").GetBoolean());
        Assert.True(Bul(YasalAnahtar.TevsikHaddiTl).GetProperty("kullaniliyor").GetBoolean());
        Assert.True(Bul(YasalAnahtar.GiderPusulasiGun).GetProperty("kullaniliyor").GetBoolean());
        // MASAK-370: bugünkü (07.10.2026 ve sonrası) satır — dayanak yalnız resmî atıf; iç not ayrı.
        Assert.Equal("Tedbirler Yönetmeliği md.5/1-b (11845 sayılı CK, RG 07.10.2026/33393)",
            Bul(YasalAnahtar.MasakKimlikEsigiTl).GetProperty("dayanak").GetString());
        // İç not veritabanında durur (silinmedi, taşındı).
        using var db = f.Vt.OlusturSistem();
        Assert.Contains("kodlanmadı", db.YasalParametreler.Single(p => p.Id == "yasal1-tohum-20").IcNot);
    }

    // ── Tur 3 (orkestratör 14:50) ────────────────────────────────────────────

    [Fact]
    public void Tur3_esik_gosterimi_ic_karsilastirma_kurusu_gostermez_BuyukEsit_olmayan_ham_kalir()
    {
        // ≥ 2.000.000,01 ≡ > 2.000.000 → "2.000.000 üzeri".
        Assert.Equal((YasalAnahtar.EsikUstu, 2_000_000m), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakBeyanFormuEsigiTl, 2_000_000.01m));
        Assert.Equal((YasalAnahtar.EsikUstu, 20_000_000m), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakEkBelgeEsigiTl, 20_000_000.01m));
        // ≥ 185.000 (Tedbirler Yön. md.5/1-b "veya üzerinde") → "185.000 ve üzeri".
        Assert.Equal((YasalAnahtar.EsikVeUstu, 185_000m), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakKimlikEsigiTl, 185_000m));
        Assert.Equal((YasalAnahtar.EsikVeUstu, 200_000m), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakAmacBeyaniEsigiTl, 200_000m));
        // Tevsik kapısı "> Had" — adı zaten "en yüksek tutar"; ham değer yazılır.
        Assert.Equal(((string?)null, (decimal?)null), YasalAnahtar.EsikGosterimi(YasalAnahtar.TevsikHaddiTl, 30_000m));
        Assert.Equal(((string?)null, (decimal?)null), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakKimlikEsigiTl, null));
        Assert.Equal(((string?)null, (decimal?)null), YasalAnahtar.EsikGosterimi(YasalAnahtar.MasakKimlikEsigiTl, 185_000.5m));
    }

    [Fact]
    public async Task Tur3_liste_esik_turu_ve_tutari_doner_ileri_tarihli_satir_Sonrakilerde_bugunku_satirlarda_DEGIL()
    {
        var k = YeniKiraci("sonraki");
        var bas = Bugun.AddDays(4);
        try
        {
            var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
            var ekle = await Yaz(sis,
                Govdesi(YasalAnahtar.TevsikHaddiTl, 40_000m, bas));
            Assert.Equal(HttpStatusCode.Created, ekle.StatusCode);

            var g = await Govde(await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/yasal-parametreler"));
            var satirlar = g.GetProperty("satirlar").EnumerateArray().ToList();
            JsonElement Bul(string a) => satirlar.Single(x => x.GetProperty("anahtar").GetString() == a);
            var beyan = Bul(YasalAnahtar.MasakBeyanFormuEsigiTl);
            Assert.Equal(2_000_000.01m, beyan.GetProperty("degerSayi").GetDecimal());
            Assert.Equal("USTU", beyan.GetProperty("esikTuru").GetString());
            Assert.Equal(2_000_000m, beyan.GetProperty("esikTutari").GetDecimal());
            var kimlik = Bul(YasalAnahtar.MasakKimlikEsigiTl);
            Assert.Equal("VE_USTU", kimlik.GetProperty("esikTuru").GetString());
            Assert.Equal(370_000m, kimlik.GetProperty("esikTutari").GetDecimal());   // MASAK-370: RG 07.10.2026/33393
            Assert.Equal(JsonValueKind.Null, Bul(YasalAnahtar.TevsikHaddiTl).GetProperty("esikTuru").ValueKind);

            // Bugünkü satırlar: tevsik TEK ve 30.000; ileri tarihli satır yalnız Sonrakiler'de.
            Assert.Equal(30_000m, Bul(YasalAnahtar.TevsikHaddiTl).GetProperty("degerSayi").GetDecimal());
            var sonraki = g.GetProperty("sonrakiler").EnumerateArray()
                .Single(x => x.GetProperty("anahtar").GetString() == YasalAnahtar.TevsikHaddiTl);
            Assert.Equal(40_000m, sonraki.GetProperty("degerSayi").GetDecimal());
            Assert.Equal(G(bas), sonraki.GetProperty("baslangic").GetString());
            Assert.All(g.GetProperty("sonrakiler").EnumerateArray(),
                x => Assert.True(string.CompareOrdinal(x.GetProperty("baslangic").GetString(), G(Bugun)) > 0));
        }
        finally
        {
            TevsikTohumunuGeriYukle();
        }
    }

    // ── K-6 (HTTP yolu) ──────────────────────────────────────────────────────

    [Fact]
    public async Task K6_ayni_anahtara_ayni_ya_da_daha_eski_ileri_tarih_ikinci_kez_409()
    {
        try
        {
            var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
            var ilk = await Yaz(sis,
                Govdesi(YasalAnahtar.TevsikHaddiTl, 45_000m, Bugun.AddDays(20)));
            Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);

            var ayni = await Yaz(sis,
                Govdesi(YasalAnahtar.TevsikHaddiTl, 46_000m, Bugun.AddDays(20)));
            Assert.Equal(HttpStatusCode.Conflict, ayni.StatusCode);

            var once = await Yaz(sis,
                Govdesi(YasalAnahtar.TevsikHaddiTl, 47_000m, Bugun.AddDays(10)));
            Assert.Equal(HttpStatusCode.Conflict, once.StatusCode);

            using var db = f.Vt.OlusturSistem();
            Assert.Equal(1, db.YasalParametreler.Count(p => p.Anahtar == YasalAnahtar.TevsikHaddiTl && p.GecerlilikBitis == null));
        }
        finally
        {
            TevsikTohumunuGeriYukle();
        }
    }

    // ── K-7 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task K7_YASAL_PARAM_EKRAN_tablodan_okunan_tevsik_haddi_appsettingsten_farkliysa_sunucu_ACILIR_tablo_esas()
    {
        // YASAL-PARAM-EKRAN — TEK KAYNAK: tevsik haddi işlem anında TABLODAN okunur; yapılandırma
        // kopyası farklı diye sunucu açılmaz duruma DÜŞMEZ (eski K-7: açılmıyordu). Kapı tablodaki
        // 30.000'i uygular (yapılandırmadaki 25.000'i DEĞİL).
        await using var ortam = f.Ortam.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<MTevsik.TevsikEsikleri>();
            s.AddSingleton(new MTevsik.TevsikEsikleri { Had = 25_000m });
        }));
        var c = ortam.CreateClient();
        var k = YeniKiraci("k7tablo");
        var had = await Govde(await TokenliIstemci(c, k, "SAHIP").GetAsync("/api/v1/tevsik/gunluk"));
        Assert.Equal(30_000m, had.GetProperty("had").GetDecimal());
    }

    [Fact]
    public void K7_YASAL_PARAM_EKRAN_YAPILANDIRMADAN_okunan_anahtar_tablodan_farkliysa_sunucu_ACILMAZ()
    {
        // Kodun hâlâ yapılandırmadan okuduğu anahtar (KDV genel oranı) ayrışırsa iki kaynak gerçekten
        // ayrışmıştır: program yapılandırmayı uygular, ekran tabloyu gösterir → sunucu açılmaz.
        var tablo = f.Ortam.Services.GetRequiredService<YasalParametreServisi>()
            .SatirBul(YasalAnahtar.KdvGenelOran, Bugun)?.DegerSayi;
        Assert.NotNull(tablo);
        using var ortam = f.Ortam.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<Kuyumcu.Api.Modules.Fatura.KdvAyarlari>();
            s.AddSingleton(new Kuyumcu.Api.Modules.Fatura.KdvAyarlari { VarsayilanOran = tablo!.Value + 0.01m });
        }));
        var ex = Assert.ThrowsAny<Exception>(() => ortam.CreateClient());
        var mesaj = ex.ToString();
        Assert.Contains("YASAL_PARAM_AYRISMA", mesaj);
        Assert.Contains("KDV_GENEL_ORAN", mesaj);
    }

    // ── Rol kapıları + okuma uçları ─────────────────────────────────────────

    [Fact]
    public async Task Liste_SAHIP_MUDUR_MUHASEBE_200_TEZGAHTAR_403_bugunku_tevsik_30000()
    {
        var k = YeniKiraci("rol");
        foreach (var rol in new[] { "SAHIP", "MUDUR", "MUHASEBE" })
        {
            var c = await f.Ortam.Istemci(k, rol).GetAsync("/api/v1/yasal-parametreler");
            Assert.Equal(HttpStatusCode.OK, c.StatusCode);
            var g = await Govde(c);
            var tevsik = g.GetProperty("satirlar").EnumerateArray()
                .Single(x => x.GetProperty("anahtar").GetString() == YasalAnahtar.TevsikHaddiTl);
            Assert.Equal(30_000m, tevsik.GetProperty("degerSayi").GetDecimal());
            Assert.Equal("DOGRULANDI", tevsik.GetProperty("teyit").GetString());
        }
        var t = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/yasal-parametreler");
        Assert.Equal(HttpStatusCode.Forbidden, t.StatusCode);

        var gecmis = await Govde(await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/yasal-parametreler/gecmis?anahtar={YasalAnahtar.TevsikHaddiTl}"));
        var degerler = gecmis.GetProperty("satirlar").EnumerateArray()
            .Select(x => x.GetProperty("degerSayi").GetDecimal()).ToList();
        Assert.Equal(new[] { 30_000m, 7_000m }, degerler);

        var eski = await Govde(await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/yasal-parametreler?tarih=2024-11-29"));
        Assert.Equal(7_000m, eski.GetProperty("satirlar").EnumerateArray()
            .Single(x => x.GetProperty("anahtar").GetString() == YasalAnahtar.TevsikHaddiTl)
            .GetProperty("degerSayi").GetDecimal());

        var masakEski = await Govde(await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/masak/esikler?tarih=2022-07-01"));
        Assert.Equal(85_000m, masakEski.GetProperty("kimlikEsigi").GetDecimal());
    }

    [Fact]
    public async Task Saglik_ucu_onbellek_ile_veritabani_esit()
    {
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        f.Ortam.Services.GetRequiredService<YasalParametreServisi>().Tazele();
        var g = await Govde(await sis.GetAsync("/api/v1/sistem/yasal-parametreler/saglik"));
        Assert.True(g.GetProperty("esit").GetBoolean());
        // Tur 2: bayram eksikliği operatöre bu uçta görünür (Panel'de bant yok).
        Assert.True(g.GetProperty("bayramEksik").ValueKind is JsonValueKind.True or JsonValueKind.False);
        Assert.True(g.GetProperty("bayramYili").GetInt32() >= 2026);
    }

    [Fact]
    public void Tur2_bayram_eksik_TABLO_satiriyla_KAPANMAZ_yalniz_sure_hesabinin_okudugu_yapilandirmaya_bakar()
    {
        // Hasım ORTA-2: tabloya RESMI_TATIL girilince uyarı kalkıyor ama MASAK süre hesabı
        // (ResmiTatiller.Aralik → yapılandırma) bayramı yine iş günü sayıyordu.
        var yil = 2031;
        var yasal = YasalParametreServisi.Yukleyicili(() =>
        [
            new YasalParametreSatiri("t1", YasalAnahtar.ResmiTatil, null, $"{yil}-01-20", YasalBirim.Tarih,
                new DateOnly(yil, 1, 20), new DateOnly(yil, 1, 21), "d", "https://x", YasalTeyit.Dogrulandi, "t"),
        ]);
        Assert.Single(yasal.Tatiller(yil));
        var bos = new TakvimAyarlari();
        Assert.Equal(ResmiTatiller.DiniBayramEksik(yil, bos), yasal.BayramEksik(yil, bos));
        Assert.True(yasal.BayramEksik(yil, bos));
    }
}
