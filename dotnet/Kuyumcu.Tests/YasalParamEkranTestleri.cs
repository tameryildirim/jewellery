using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-PARAM-EKRAN — yasal sınırlar tek kaynak (tablo) + bugünden geçerli
// değişiklik (RG künyesi zorunlu, gerekçe, iki adımlı onay, denetim izi) + ByGatech
// sistem yöneticisi formu / kuyumcu salt okunur.
//
// ⚠ Paylaşılan test veritabanına bugünden geçerli satır yazan test MASAK_EK_BELGE_ESIGI_TL
//   anahtarını kullanır (kimlik eşiği satır sayısını sınayan Masak370Testleri ile çakışmasın)
//   ve finally'de satırı siler, önceki satırı yeniden açar, önbelleği tazeler.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class YasalParamHttpTestleri(HttpFikstur f)
{
    private const string Uc = "/api/v1/sistem/yasal-parametreler";
    private DateOnly Bugun => YasalParametreServisi.IslemGunu(DateTime.UtcNow);
    private static string G(DateOnly g) => g.ToString("yyyy-MM-dd");

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    private Dictionary<string, object?> Govdesi(string anahtar, decimal deger, DateOnly bas) => new()
    {
        ["anahtar"] = anahtar, ["degerSayi"] = deger, ["baslangic"] = G(bas),
        ["kaynakUrl"] = "https://www.resmigazete.gov.tr/test", ["dayanak"] = "YASAL-PARAM-EKRAN test satırı (silinecek)",
        ["gerekce"] = "YASAL-PARAM-EKRAN HTTP testi bugünden geçerli değişiklik",
        ["resmiGazeteTarihi"] = G(Bugun), ["resmiGazeteSayisi"] = "33393",
    };

    private static Dictionary<string, object?> Ile(Dictionary<string, object?> g, string a, object? d) =>
        new(g) { [a] = d };

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETYASALPARAM-TEST-" + ad);
    }

    /// <summary>Ek belge eşiğinin bugünden geçerli test satırını siler, önceki satırı yeniden açar.</summary>
    private void EkBelgeGeriYukle(string? acilacakId)
    {
        using var db = f.Vt.OlusturSistem();
        db.Database.ExecuteSqlRaw(
            "DELETE FROM yasal_parametreler WHERE anahtar = 'MASAK_EK_BELGE_ESIGI_TL' AND olusturan LIKE 'sistem:%'");
        if (acilacakId is not null)
            db.Database.ExecuteSqlRaw("UPDATE yasal_parametreler SET gecerlilik_bitis = NULL WHERE id = {0}", acilacakId);
        f.Ortam.Services.GetRequiredService<YasalParametreServisi>().Tazele();
    }

    [Fact]
    public async Task Bugunden_gecerli_RG_ve_onayla_201_ayni_gun_sonraki_islem_yeni_esikle_dun_eski_esikle_denetim_izi()
    {
        var servis = f.Ortam.Services.GetRequiredService<YasalParametreServisi>();
        var oncekiSatir = servis.SatirBul(YasalAnahtar.MasakEkBelgeEsigiTl, Bugun)!;
        var k = YeniKiraci("bugun");
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        try
        {
            // Önce: 15.000.000 TL ek belge İSTEMEZ (eşik 20.000.000,01).
            var once = await Govde(await sahip.GetAsync("/api/v1/masak/gereksinim?tutar=15000000"));
            Assert.False(once.GetProperty("ekBelgeGerekli").GetBoolean());

            var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
            var govde = Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, 10_000_000m, Bugun);
            var on = await sis.PostAsJsonAsync(Uc + "/onizleme", govde);
            Assert.True(on.StatusCode == HttpStatusCode.OK, await on.Content.ReadAsStringAsync());
            var o = await Govde(on);
            Assert.True(o.GetProperty("bugunMu").GetBoolean());
            Assert.Equal("SIKILASTIRMA", o.GetProperty("yon").GetString());
            Assert.Equal(oncekiSatir.DegerSayi, o.GetProperty("eskiDeger").GetDecimal());
            Assert.Equal(10_000_000m, o.GetProperty("yeniDeger").GetDecimal());
            Assert.Equal("33393", o.GetProperty("resmiGazeteSayisi").GetString());
            // Önizleme HİÇBİR ŞEY yazmaz.
            Assert.Equal(oncekiSatir.DegerSayi, servis.SatirBul(YasalAnahtar.MasakEkBelgeEsigiTl, Bugun)!.DegerSayi);

            var kayit = await sis.PostAsJsonAsync(Uc, Ile(govde, "onayKodu", o.GetProperty("onayKodu").GetString()));
            Assert.Equal(HttpStatusCode.Created, kayit.StatusCode);
            var kg = await Govde(kayit);
            Assert.Equal(oncekiSatir.Id, kg.GetProperty("kapananSatirId").GetString());
            var yeniId = kg.GetProperty("id").GetString()!;

            // Aynı gün SONRAKİ işlem yeni eşikle (yeniden başlatmasız).
            var sonra = await Govde(await sahip.GetAsync("/api/v1/masak/gereksinim?tutar=15000000"));
            Assert.True(sonra.GetProperty("ekBelgeGerekli").GetBoolean());
            // Dün (önceki dönem) eski eşikle.
            var dun = await Govde(await sahip.GetAsync($"/api/v1/masak/esikler?tarih={G(Bugun.AddDays(-1))}"));
            Assert.Equal(oncekiSatir.DegerSayi, dun.GetProperty("ekBelgeEsigi").GetDecimal());

            using var db = f.Vt.OlusturSistem();
            var eski = db.YasalParametreler.AsNoTracking().Single(p => p.Id == oncekiSatir.Id);
            Assert.Equal(Bugun, eski.GecerlilikBitis);                       // geçmiş yeniden yazılmadı, yalnız bitiş
            var yeni = db.YasalParametreler.AsNoTracking().Single(p => p.Id == yeniId);
            Assert.Equal(Bugun, yeni.ResmiGazeteTarihi);
            Assert.Equal("33393", yeni.ResmiGazeteSayisi);
            Assert.Equal("sistem:asikin-qa", yeni.Olusturan);
            var iz = db.DenetimKayitlari.AsNoTracking().Where(d => d.VarlikId == yeniId).Select(d => d.Detay ?? "").ToList();
            var tek = Assert.Single(iz);
            Assert.Contains("BUGÜNDEN GEÇERLİ", tek);
            Assert.Contains("yön SIKILASTIRMA", tek);
            Assert.Contains("RG " + G(Bugun) + "/33393", tek);
            Assert.Contains("operatör: asikin-qa", tek);
        }
        finally
        {
            EkBelgeGeriYukle(oncekiSatir.Id);
        }
        Assert.Equal(oncekiSatir.DegerSayi, servis.SatirBul(YasalAnahtar.MasakEkBelgeEsigiTl, Bugun)!.DegerSayi);
    }

    [Fact]
    public async Task RG_tarihi_yok_400_RG_sayisi_yok_400_RG_tarihi_ileri_400_gecmis_400_gerekce_kisa_400_alanli()
    {
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var g = Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, 10_000_000m, Bugun.AddDays(3));
        foreach (var (govde, alan) in new[]
                 {
                     (Ile(g, "resmiGazeteTarihi", null), "resmiGazeteTarihi"),
                     (Ile(g, "resmiGazeteSayisi", null), "resmiGazeteSayisi"),
                     (Ile(g, "resmiGazeteSayisi", "otuz"), "resmiGazeteSayisi"),
                     (Ile(g, "resmiGazeteTarihi", G(Bugun.AddDays(1))), "resmiGazeteTarihi"),
                     (Ile(g, "gerekce", "kısa"), "gerekce"),
                 })
        {
            foreach (var yol in new[] { Uc + "/onizleme", Uc })
            {
                var c = await sis.PostAsJsonAsync(yol, Ile(govde, "onayKodu", "X"));
                Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
                var j = await Govde(c);
                Assert.True(j.TryGetProperty("alanlar", out var a) && a.TryGetProperty(alan, out _), $"{yol} {alan}: {j}");
            }
        }
        // YASAL-PARAM-2 (maskelenmiş assert yalıtıldı): "geçmiş başlangıç" durumu eskiden RG tarihi = BUGÜN ile
        // sınanıyordu; RG > başlangıç kuralı da aynı alanla (baslangic) 400 verdiği için geçmiş kuralı
        // kaldırılsa test yine yeşildi. Artık RG tarihi başlangıçtan ÖNCE (yalnız geçmiş kuralı tetiklenir)
        // ve mesaj da sınanır.
        var gecmis = Ile(Ile(g, "baslangic", G(Bugun.AddDays(-1))), "resmiGazeteTarihi", G(Bugun.AddDays(-2)));
        foreach (var yol in new[] { Uc + "/onizleme", Uc })
        {
            var c = await sis.PostAsJsonAsync(yol, Ile(gecmis, "onayKodu", "X"));
            Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
            var j = await Govde(c);
            Assert.True(j.GetProperty("alanlar").TryGetProperty("baslangic", out _), $"{yol}: {j}");
            Assert.StartsWith("Geçmiş tarihli yasal değer yazılamaz", j.GetProperty("message").GetString());
        }
        // Künye eksikse mesaj eksikliği söyler (başlangıç gününden TÜRETİLMEZ — bugün başlayanda da).
        foreach (var bas in new[] { Bugun, Bugun.AddDays(3) })
        {
            var yok = await Govde(await sis.PostAsJsonAsync(Uc + "/onizleme",
                Ile(Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, 10_000_000m, bas), "resmiGazeteTarihi", null)));
            Assert.Equal("Resmî Gazete tarihi zorunludur.", yok.GetProperty("message").GetString());
        }
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.YasalParametreler.Count(p => p.DegerSayi == 10_000_000m));
    }

    [Fact]
    public async Task Bugunku_satirin_dayanagi_BOSSA_dayanak_zorunlu_400_alanli_turetilmez()
    {
        // Ek belge eşiğinin tohum satırında dayanak boş: sunucu dayanağı UYDURMAZ (fail-closed).
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var c = await sis.PostAsJsonAsync(Uc + "/onizleme",
            Ile(Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, 10_000_000m, Bugun.AddDays(3)), "dayanak", null));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.True((await Govde(c)).GetProperty("alanlar").TryGetProperty("dayanak", out _));
        // Dayanağı dolu satırda (MASAK kimlik eşiği) boş dayanak bugünkü dayanaktan türetilir.
        var d = await sis.PostAsJsonAsync(Uc + "/onizleme",
            Ile(Govdesi(YasalAnahtar.MasakKimlikEsigiTl, 250_000m, Bugun.AddDays(3)), "dayanak", null));
        Assert.True(d.StatusCode == HttpStatusCode.OK, await d.Content.ReadAsStringAsync());
        Assert.Contains("md.5/1-b", (await Govde(d)).GetProperty("dayanak").GetString());
    }

    [Fact]
    public async Task Tur2_AYNI_deger_409_akla_yatkin_disi_400_buyuk_degisim_isaretli_Turkce_operator_cozulur_denetime_yazilir()
    {
        var servis = f.Ortam.Services.GetRequiredService<YasalParametreServisi>();
        var onceki = servis.SatirBul(YasalAnahtar.MasakEkBelgeEsigiTl, Bugun)!;
        var bugunku = onceki.DegerSayi!.Value;
        var ileri = Bugun.AddDays(3);
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);

        // AYNI değer yeni satır olarak yazılmaz → 409 (alan: degerSayi).
        var ayni = await sis.PostAsJsonAsync(Uc + "/onizleme", Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, bugunku, ileri));
        Assert.Equal(HttpStatusCode.Conflict, ayni.StatusCode);
        Assert.True((await Govde(ayni)).GetProperty("alanlar").TryGetProperty("degerSayi", out _));

        // Akla yatkınlık bandı dışı → 400 (sıfır, 10 kattan büyük, onda birden küçük).
        foreach (var d in new[] { 0m, bugunku * 10m + 1m, Math.Round(bugunku / 10m, 2) - 1m })
        {
            var c = await sis.PostAsJsonAsync(Uc + "/onizleme", Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, d, ileri));
            Assert.True(c.StatusCode == HttpStatusCode.BadRequest, $"{d}: {await c.Content.ReadAsStringAsync()}");
            Assert.True((await Govde(c)).GetProperty("alanlar").TryGetProperty("degerSayi", out _), d.ToString());
        }
        // Saklama yılı 0 → 400.
        var sak = await sis.PostAsJsonAsync(Uc + "/onizleme", Govdesi(YasalAnahtar.MasakSaklamaYil, 0m, ileri));
        Assert.Equal(HttpStatusCode.BadRequest, sak.StatusCode);

        // Bant içi: 1,5 kat → büyük değişim YOK; yarıya iniş → büyük değişim VAR.
        var kucuk = await Govde(await sis.PostAsJsonAsync(Uc + "/onizleme", Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, Math.Round(bugunku * 1.5m, 2), ileri)));
        Assert.False(kucuk.GetProperty("buyukDegisim").GetBoolean());

        // Türkçe harfli operatör adı RFC 5987 ile gelir; sunucu çözer, denetim izine ve oluşturana DÜZ yazılır.
        var tr = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, ApiIstemci.OperatorBasligiDegeri("Tasarım Hakemi"));
        var g = Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, Math.Round(bugunku / 2m, 2), ileri);
        try
        {
            var on = await tr.PostAsJsonAsync(Uc + "/onizleme", g);
            Assert.True(on.StatusCode == HttpStatusCode.OK, await on.Content.ReadAsStringAsync());
            var o = await Govde(on);
            Assert.True(o.GetProperty("buyukDegisim").GetBoolean());
            var kayit = await tr.PostAsJsonAsync(Uc, Ile(g, "onayKodu", o.GetProperty("onayKodu").GetString()));
            Assert.Equal(HttpStatusCode.Created, kayit.StatusCode);
            var yeniId = (await Govde(kayit)).GetProperty("id").GetString()!;
            using var db = f.Vt.OlusturSistem();
            Assert.Equal("sistem:Tasarım Hakemi", db.YasalParametreler.AsNoTracking().Single(p => p.Id == yeniId).Olusturan);
            Assert.Contains("operatör: Tasarım Hakemi", db.DenetimKayitlari.AsNoTracking().Single(d => d.VarlikId == yeniId).Detay);
        }
        finally
        {
            EkBelgeGeriYukle(onceki.Id);
        }
        // Kodlanmış ama çözülünce kısa ad → kapı 400 (en az 3 karakter kuralı çözülmüş değere uygulanır).
        var kisa = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "UTF-8''%C4%B0z");
        Assert.Equal(HttpStatusCode.BadRequest, (await kisa.PostAsJsonAsync(Uc + "/onizleme", g)).StatusCode);
        Assert.Equal(bugunku, servis.SatirBul(YasalAnahtar.MasakEkBelgeEsigiTl, Bugun)!.DegerSayi);
    }

    [Fact]
    public async Task Onay_kodu_yok_400_onay_kodu_uyusmaz_409_satir_yazilmaz()
    {
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var g = Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, 10_000_000m, Bugun.AddDays(3));
        var yok = await sis.PostAsJsonAsync(Uc, g);
        Assert.Equal(HttpStatusCode.BadRequest, yok.StatusCode);
        var yj = await Govde(yok);
        Assert.True(yj.GetProperty("alanlar").TryGetProperty("onayKodu", out _), yj.ToString());

        // Önizlenen 10.000.000; gönderilen 11.000.000 → kod tutmaz.
        var on = await Govde(await sis.PostAsJsonAsync(Uc + "/onizleme", g));
        var farkli = await sis.PostAsJsonAsync(Uc,
            Ile(Ile(g, "degerSayi", 11_000_000m), "onayKodu", on.GetProperty("onayKodu").GetString()));
        Assert.Equal(HttpStatusCode.Conflict, farkli.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.YasalParametreler.Count(p => p.DegerSayi == 10_000_000m || p.DegerSayi == 11_000_000m));
    }

    [Fact]
    public async Task Mehmet_SAHIP_JWT_onizleme_ve_kayit_403_sistem_anahtari_200()
    {
        var k = YeniKiraci("yetki");
        var g = Govdesi(YasalAnahtar.MasakEkBelgeEsigiTl, 10_000_000m, Bugun.AddDays(3));
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(HttpStatusCode.Forbidden, (await sahip.PostAsJsonAsync(Uc + "/onizleme", g)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sahip.PostAsJsonAsync(Uc, Ile(g, "onayKodu", "X"))).StatusCode);
        var yanlis = f.Ortam.SistemIstemci("yanlis-anahtar-yanlis-anahtar-yanlis-anahtar");
        Assert.Equal(HttpStatusCode.Forbidden, (await yanlis.PostAsJsonAsync(Uc + "/onizleme", g)).StatusCode);
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        var ok = await sis.PostAsJsonAsync(Uc + "/onizleme", g);
        Assert.True(ok.StatusCode == HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Onizleme_acik_siparis_sayisi_eski_yeni_bandindaki_acik_siparisi_sayar_teslim_edileni_saymaz()
    {
        var sis = f.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari);
        // 370.000 → 250.000 (yalnız önizleme; hiçbir şey yazılmaz).
        var g = Govdesi(YasalAnahtar.MasakKimlikEsigiTl, 250_000m, Bugun.AddDays(2));
        async Task<int> Say() => (await Govde(await sis.PostAsJsonAsync(Uc + "/onizleme", g)))
            .GetProperty("acikIslemSayisi").GetInt32();
        var once = await Say();

        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "PAKETYASALPARAM-TEST-siparis");
        var simdi = DateTime.UtcNow;
        foreach (var (durum, tutar) in new[] { (SiparisDurumu.Onaylandi, 300_000m), (SiparisDurumu.TeslimEdildi, 300_000m), (SiparisDurumu.Onaylandi, 100_000m) })
        {
            var id = Kimlik.Ulid();
            db.Siparisler.Add(new Siparis
            {
                Id = id, TenantId = k.TenantId, No = $"S-{id[^8..]}", Tur = "MUSTERI", Durum = durum,
                CariId = k.CariId, TahminiTutarTl = tutar, KurSnapshotId = k.KurId,
                OlusturanKullaniciId = k.KullaniciId, Olusturuldu = simdi, Guncellendi = simdi,
            });
        }
        await db.SaveChangesAsync();
        Assert.Equal(once + 1, await Say());

        var on = await Govde(await sis.PostAsJsonAsync(Uc + "/onizleme",
            Govdesi(YasalAnahtar.MasakSibIsGunu, 12m, Bugun.AddDays(2))));
        Assert.Equal(JsonValueKind.Null, on.GetProperty("acikIslemSayisi").ValueKind);   // TL değil → sayılmaz
        Assert.Equal("GEVSETME", on.GetProperty("yon").GetString());
    }

    [Fact]
    public async Task Masak_gereksinim_baglantili_toplami_olcu_olarak_doner()
    {
        var k = YeniKiraci("olcu");
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        var j = await Govde(await sahip.GetAsync($"/api/v1/masak/gereksinim?tutar=185000&cariId={k.CariId}"));
        Assert.Equal(185_000m, j.GetProperty("islemTutarTl").GetDecimal());
        Assert.Equal(185_000m, j.GetProperty("olcuTutarTl").GetDecimal());   // bugün önceki işlem yok
        var tek = await Govde(await sahip.GetAsync("/api/v1/masak/gereksinim?tutar=-1000"));
        Assert.Equal(1_000m, tek.GetProperty("olcuTutarTl").GetDecimal());
    }
}

// ── Saf testler (veritabanısız) ─────────────────────────────────────────────

public sealed class YasalParamSafTestleri
{
    private static readonly DateOnly Gecis = new(2026, 10, 7);

    private static YasalParametreSatiri S(string anahtar, decimal deger, DateOnly bas, DateOnly? bit = null) =>
        new("t-" + anahtar + bas.DayNumber, anahtar, deger, null, "TL", bas, bit, "d", "https://x", "DOGRULANDI", "a");

    private static YasalParametreServisi Servis(TimeProvider saat) => YasalParametreServisi.Yukleyicili(() =>
    [
        S(YasalAnahtar.MasakKimlikEsigiTl, 185_000m, new(2023, 1, 14), Gecis),
        S(YasalAnahtar.MasakKimlikEsigiTl, 370_000m, Gecis),
        S(YasalAnahtar.MasakAmacBeyaniEsigiTl, 200_000m, new(2008, 4, 1)),
        S(YasalAnahtar.MasakBeyanFormuEsigiTl, 2_000_000.01m, new(2008, 4, 1)),
        S(YasalAnahtar.MasakEkBelgeEsigiTl, 20_000_000.01m, new(2008, 4, 1)),
        S(YasalAnahtar.MasakSibIsGunu, 10m, new(2008, 4, 1)),
        S(YasalAnahtar.MasakSaklamaYil, 8m, new(2008, 4, 1)),
        S(YasalAnahtar.KdvGenelOran, 0.20m, new(2008, 4, 1)),
    ], saat);

    private static SahteSaat Saat(int gun) => new(new DateTimeOffset(2026, 10, gun, 9, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(YasalAnahtar.MasakKimlikEsigiTl, 370_000, 250_000, "SIKILASTIRMA")]
    [InlineData(YasalAnahtar.MasakKimlikEsigiTl, 370_000, 400_000, "GEVSETME")]
    [InlineData(YasalAnahtar.TevsikHaddiTl, 30_000, 30_000, "AYNI")]
    [InlineData(YasalAnahtar.MasakSibIsGunu, 10, 7, "SIKILASTIRMA")]
    [InlineData(YasalAnahtar.MasakSaklamaYil, 8, 10, "SIKILASTIRMA")]
    [InlineData(YasalAnahtar.MasakSaklamaYil, 8, 5, "GEVSETME")]
    [InlineData(YasalAnahtar.KdvGenelOran, 0.20, 0.18, "DEGISIKLIK")]
    public void Yon_sikilastirma_gevsetme_dogru_okunur(string anahtar, double eski, double yeni, string beklenen) =>
        Assert.Equal(beklenen, YasalParametreServisi.Yon(anahtar, (decimal)eski, (decimal)yeni));

    [Theory]
    [InlineData(370_000, 370_001, true)]
    [InlineData(370_000, 3_700_000, true)]
    [InlineData(370_000, 3_700_001, false)]
    [InlineData(370_000, 37_000, true)]
    [InlineData(370_000, 36_999, false)]
    [InlineData(8, 0, false)]
    [InlineData(370_000, 999_999_999.99, false)]
    public void Tur2_akla_yatkinlik_bandi(double eski, double yeni, bool beklenen) =>
        Assert.Equal(beklenen, YasalParametreServisi.AklaYatkin((decimal)eski, (decimal)yeni));

    [Theory]
    [InlineData(370_000, 740_000, true)]
    [InlineData(370_000, 739_999, false)]
    [InlineData(370_000, 185_000, true)]
    [InlineData(370_000, 185_001, false)]
    public void Tur2_buyuk_degisim_isareti(double eski, double yeni, bool beklenen) =>
        Assert.Equal(beklenen, YasalParametreServisi.BuyukDegisim((decimal)eski, (decimal)yeni));

    [Theory]
    [InlineData("UTF-8''Tasar%C4%B1m%20Hakemi", "Tasarım Hakemi")]
    [InlineData("utf-8''%C3%87a%C4%9Flar", "Çağlar")]
    [InlineData("asikin-qa", "asikin-qa")]
    [InlineData("  ramazan  ", "ramazan")]
    [InlineData("UTF-8''%ZZbozuk", "%ZZbozuk")]
    public void Tur2_operator_basligi_cozulur(string ham, string beklenen) =>
        Assert.Equal(beklenen, Kuyumcu.Api.Modules.Sistem.SistemAnahtariKapisi.OperatorCoz(ham));

    [Fact]
    public void Acilis_tablodan_okunan_fark_UYARI_yapilandirmadan_okunan_fark_ENGEL()
    {
        var s = Servis(Saat(8));
        var (engel, uyari) = s.AcilisDegerlendir(new Dictionary<string, decimal>
        {
            [YasalAnahtar.MasakKimlikEsigiTl] = 185_000m,     // tablo 370.000 → tablo esas, uyarı
            [YasalAnahtar.MasakAmacBeyaniEsigiTl] = 200_000m, // eşit → hiçbiri
            [YasalAnahtar.KdvGenelOran] = 0.18m,              // yapılandırmadan okunur → engel
        });
        Assert.Contains("MASAK_KIMLIK_ESIGI_TL", Assert.Single(uyari));
        Assert.Contains("KDV_GENEL_ORAN", Assert.Single(engel));
    }

    private static MMasak.MasakKapisi Kapi(TimeProvider saat) =>
        new(new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri(), Servis(saat)));

    [Fact]
    public void Kumulatif_notu_toplam_kimlik_esigine_ulasinca_KIMLIK_esigini_yazar_ulasmazsa_amac_kademesini()
    {
        var kapi = Kapi(Saat(8));
        var ust = Assert.Throws<BadRequestException>(() => kapi.Dogrula(185_000m, null, 370_000m)).Message;
        Assert.Contains("370.000 TL ve üstü", ust);
        Assert.Contains("eşik 370.000 TL", ust);
        Assert.DoesNotContain("eşik 200.000", ust);

        var alt = Assert.Throws<BadRequestException>(() => kapi.Dogrula(125_000m, null, 250_000m)).Message;
        Assert.Contains("200.000 TL ve üstü", alt);
        Assert.Contains("eşik 200.000 TL", alt);
    }

    [Fact]
    public void Esik_gunu_verilirse_ISLEM_GUNUNUN_esigi_okunur_verilmezse_bugunun()
    {
        var kapi = Kapi(Saat(8));
        // Bugün (08.10) 190.000 kimliksiz geçer (370.000 / 200.000).
        Assert.Equal(BeyanKademesi.Yok, kapi.Dogrula(190_000m, null).Kademe);
        // 06.10 tarihli işlem (geç eşleştirilen dekont): o günün 185.000 eşiği → kimlik zorunlu.
        var ex = Assert.Throws<BadRequestException>(() => kapi.Dogrula(190_000m, null, esikGunu: new DateOnly(2026, 10, 6)));
        Assert.Contains("185.000 TL ve üstü", ex.Message);
    }
}

// ── bUnit ────────────────────────────────────────────────────────────────────

public sealed class YasalParamEkranTestleri : BunitContext
{
    private readonly List<(string Yol, string? Anahtar, string? Govde)> istekler = [];
    private readonly List<string> operatorlar = [];
    private int onizlemeKodu = 200;
    private string onizlemeJson = Onizleme;
    private bool agHatasi;
    /// <summary>Tur 3: sunucu bu oturumun ByGatech yönetim kiracısı olduğunu söylüyor mu (liste yanıtı).</summary>
    private bool yonetimAcik = true;

    private const string Liste = """
        {"tarih":"2026-10-08","bugun":"2026-10-08","bayramEksik":false,"tatiller":[],
         "satirlar":[
          {"id":"m","anahtar":"MASAK_KIMLIK_ESIGI_TL","degerSayi":370000,"degerMetin":null,"birim":"TL",
           "baslangic":"2026-10-07","bitis":null,"dayanak":"Tedbirler Yönetmeliği md.5/1-b",
           "kaynakUrl":"https://www.resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf","teyit":"DOGRULANDI","aciklamaSade":"x","kullaniliyor":true,
           "esikTuru":"VE_USTU","esikTutari":370000,"resmiGazeteTarihi":"2026-10-07","resmiGazeteSayisi":"33393"},
          {"id":"a","anahtar":"MASAK_AMAC_BEYANI_ESIGI_TL","degerSayi":200000,"degerMetin":null,"birim":"TL",
           "baslangic":"2008-04-01","bitis":null,"dayanak":"md.x","kaynakUrl":"https://www.mevzuat.gov.tr/x",
           "teyit":"TEYITSIZ","aciklamaSade":"x","kullaniliyor":true,"esikTuru":"VE_USTU","esikTutari":200000}]}
        """;

    private const string Onizleme = """
        {"anahtar":"MASAK_KIMLIK_ESIGI_TL","birim":"TL","eskiDeger":370000,"eskiMetin":null,"eskiBaslangic":"2026-10-07",
         "yeniDeger":250000,"yeniMetin":null,"baslangic":"2026-10-08","bugunMu":true,"yon":"SIKILASTIRMA",
         "acikIslemSayisi":3,"resmiGazeteTarihi":"2026-10-08","resmiGazeteSayisi":"33400","teyit":"DOGRULANDI",
         "dayanak":"Tedbirler Yönetmeliği md.5/1-b","onayKodu":"ABCDEF0123456789"}
        """;

    private const string Sonuc = """
        {"id":"yeni","anahtar":"MASAK_KIMLIK_ESIGI_TL","degerSayi":250000,"degerMetin":null,"eskiDeger":370000,"birim":"TL",
         "baslangic":"2026-10-08","bugunMu":true,"yon":"SIKILASTIRMA","teyit":"DOGRULANDI",
         "resmiGazeteTarihi":"2026-10-08","resmiGazeteSayisi":"33400","kapananSatirId":"m"}
        """;

    public YasalParamEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi(YasalParamEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? null : await istek.Content.ReadAsStringAsync(iptal);
            var anahtar = istek.Headers.TryGetValues("X-Sistem-Anahtari", out var d) ? d.First() : null;
            if (istek.Headers.TryGetValues("X-Sistem-Operator", out var op)) lock (t.operatorlar) t.operatorlar.Add(op.First());
            lock (t.istekler) t.istekler.Add((yol, anahtar, govde));
            // Tur 2: tarayıcının ASCII dışı başlığı reddetmesi / ağ kopması → HttpRequestException.
            if (t.agHatasi && yol.Contains("/sistem/")) throw new HttpRequestException("ağ yok (test)");
            var (kod, cevap) = yol switch
            {
                var y when y.EndsWith("/sistem/yasal-parametreler/onizleme") => (t.onizlemeKodu, t.onizlemeKodu == 200 ? t.onizlemeJson
                    : """{"message":"Resmî Gazete tarihi zorunludur.","statusCode":400,"alanlar":{"resmiGazeteTarihi":["Resmî Gazete tarihi zorunludur."]}}"""),
                var y when y.EndsWith("/sistem/yasal-parametreler") => (201, Sonuc),
                var y when y.Contains("/yasal-parametreler/gecmis") => (200, """{"anahtar":"MASAK_KIMLIK_ESIGI_TL","satirlar":[]}"""),
                var y when y.Contains("/yasal-parametreler") => (200, t.yonetimAcik
                    ? Liste.Replace("\"bayramEksik\":false", "\"bayramEksik\":false,\"yonetimAcik\":true") : Liste),
                _ => (200, "{}"),
            };
            return new HttpResponseMessage((HttpStatusCode)kod)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.YasalSinirlar> Ac(bool yonetim)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(yonetim ? "/yasal-sinirlar?yonetim=1" : "/yasal-sinirlar");
        var e = Render<Kuyumcu.Web.Pages.YasalSinirlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ys-liste")));
        return e;
    }

    [Fact]
    public void Kuyumcu_salt_okunur_form_YOK_kaynak_RG_gorunur_not_ByGatech_gunceller_T1_yorumu_rozet_yaninda_DEGIL()
    {
        var e = Ac(false);
        Assert.Empty(e.FindAll("#ysy-bolum"));
        Assert.Empty(e.FindAll("button#ysy-onizle"));
        Assert.Contains("Resmî Gazete 7 Ekim 2026, sayı 33393", e.Find("#ys-rg-masak-kimlik-esigi-tl").TextContent);
        Assert.Contains("ByGatech günceller", e.Find("#ys-degismez").TextContent);
        // T1 yorumu kartın üst bölümünde değil, "Dayanak ve geçmiş" katlanırının İÇİNDE.
        var kart = e.Find("#ys-satir-masak-kimlik-esigi-tl");
        Assert.Null(kart.QuerySelector(":scope > #ys-yorum-masak-kimlik-esigi-tl"));
        Assert.Contains("kimlik alınır", e.Find("#ys-ad-masak-amac-beyani-esigi-tl").TextContent);
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/sistem/"));
    }

    [Fact]
    public void Yonetim_bos_onizle_alan_alti_hatalar_istek_ATILMAZ()
    {
        var e = Ac(true);
        e.Find("#ysy-onizle").Click();
        var m = e.Markup;
        Assert.Contains("Sınır seçin.", m);
        Assert.Contains("Yeni değeri yazın.", m);
        Assert.Contains("Resmî Gazete tarihini girin.", m);
        Assert.Contains("Resmî Gazete sayısını girin.", m);
        Assert.Contains("Gerekçe en az 20 karakter olmalı (0/20).", m);
        Assert.Contains("Sistem anahtarını girin.", m);
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/sistem/"));
    }

    private const string YeniRgAdresi = "https://www.resmigazete.gov.tr/eskiler/2026/10/20261008-1.pdf";
    private const string EskiRgAdresi = "https://www.resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf";

    private void Doldur(IRenderedComponent<Kuyumcu.Web.Pages.YasalSinirlar> e)
    {
        e.Find("#ysy-sinir").Click();
        e.FindAll("li.secici-secenek").First(l => l.GetAttribute("data-deger") == "MASAK_KIMLIK_ESIGI_TL").Click();
        e.Find("#ysy-deger").Input("250000");
        e.Find("#ysy-rg-tarihi").Change("2026-10-08");
        // Tur 3: yeni RG tarihi önerilen ESKİ belge adresini temizler → yönetici yeni belgenin adresini yapıştırır.
        e.Find("#ysy-kaynak").Input(YeniRgAdresi);
        e.Find("#ysy-rg-sayisi").Input("33400");
        e.Find("#ysy-gerekce").Input("Resmî Gazete 33400 ile kimlik eşiği değişti (test).");
        e.Find("#ysy-anahtar").Input("gizli-test-anahtari-gizli-test-anahtari");
        e.Find("#ysy-operator").Input("Ramazan");
    }

    [Fact]
    public void Yonetim_onizle_onay_penceresi_eski_yeni_sikilastirma_acik_siparis_kaydet_onay_kodu_ve_basari_bandi()
    {
        var e = Ac(true);
        Doldur(e);
        e.Find("#ysy-onizle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-onay")));
        var on = istekler.Single(i => i.Yol.EndsWith("/onizleme"));
        Assert.Equal("gizli-test-anahtari-gizli-test-anahtari", on.Anahtar);
        var j = JsonDocument.Parse(on.Govde!).RootElement;
        Assert.Equal("MASAK_KIMLIK_ESIGI_TL", j.GetProperty("anahtar").GetString());
        Assert.Equal(250_000m, j.GetProperty("degerSayi").GetDecimal());
        Assert.Equal("2026-10-08", j.GetProperty("baslangic").GetString());     // varsayılan BUGÜN
        Assert.Equal("33400", j.GetProperty("resmiGazeteSayisi").GetString());
        Assert.Equal(JsonValueKind.Null, j.GetProperty("onayKodu").ValueKind);
        // Tur 3: önerilen eski RG adresi yeni RG tarihiyle temizlendi; gövdeye yazılan YENİ adres gider.
        Assert.Equal(YeniRgAdresi, j.GetProperty("kaynakUrl").GetString());

        Assert.Contains($"{Bicim.Tl(370_000m)} → {Bicim.Tl(250_000m)}", e.Find("#ysy-onay-eski-yeni").TextContent);
        Assert.Contains("Bugünden (8 Ekim 2026) geçerli", e.Find("#ysy-onay-gecerlilik").TextContent);
        Assert.Contains("sıkılaşıyor", e.Find("#ysy-onay-uyari").TextContent);
        Assert.Contains(": 3", e.Find("#ysy-onay-acik").TextContent);
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/sistem/yasal-parametreler"));   // onaysız kayıt YOK

        e.Find("#ysy-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("Kaydedildi", e.Find("#ysy-basari").TextContent));
        var kayit = istekler.Single(i => i.Yol.EndsWith("/sistem/yasal-parametreler"));
        Assert.Equal("ABCDEF0123456789", JsonDocument.Parse(kayit.Govde!).RootElement.GetProperty("onayKodu").GetString());
        Assert.Empty(e.FindAll("#ysy-onay"));
        // Liste yeniden okundu; anahtar alanı boşaltıldı.
        Assert.True(istekler.Count(i => i.Yol.EndsWith("/yasal-parametreler")) >= 2);
        Assert.True(string.IsNullOrEmpty(e.Find("#ysy-anahtar").GetAttribute("value")));
    }

    [Fact]
    public void Yonetim_vazgec_kayit_atmaz_sunucu_alan_hatasi_alanin_altina()
    {
        var e = Ac(true);
        Doldur(e);
        e.Find("#ysy-onizle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-vazgec")));
        e.Find("#ysy-vazgec").Click();
        Assert.Empty(e.FindAll("#ysy-onay"));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/sistem/yasal-parametreler"));

        onizlemeKodu = 400;
        e.Find("#ysy-onizle").Click();
        e.WaitForAssertion(() => Assert.Contains("Resmî Gazete tarihi zorunludur.", e.Markup));
        Assert.Empty(e.FindAll("#ysy-onay"));
    }

    // ── TUR 2 (tasarım hakemi KRİTİK + hasım DÜŞÜK) ───────────────────────────

    [Fact]
    public void Tur2_Turkce_harfli_ad_basliga_ASCII_kodlu_gider_sunucu_cozer_sayfa_COKMEZ()
    {
        var e = Ac(true);
        Doldur(e);
        e.Find("#ysy-operator").Input("Tasarım Hakemi");
        e.Find("#ysy-onizle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-onay")));
        var baslik = Assert.Single(operatorlar);
        // Tarayıcı ASCII dışı başlığı reddeder (HttpRequestException) → değer yalnız ASCII olmalı.
        Assert.True(baslik.All(c => c < 128), baslik);
        Assert.Equal("Tasarım Hakemi", Kuyumcu.Api.Modules.Sistem.SistemAnahtariKapisi.OperatorCoz(baslik));
    }

    [Fact]
    public void Tur2_ag_hatasi_ONIZLE_ve_KAYDET_sayfayi_dusurmez_hata_bandina_yazar()
    {
        var e = Ac(true);
        Doldur(e);
        agHatasi = true;
        e.Find("#ysy-onizle").Click();
        e.WaitForAssertion(() => Assert.Contains("Sunucuya ulaşılamadı", e.Find("#ysy-hata").TextContent));
        Assert.Empty(e.FindAll("#ysy-onay"));
        Assert.NotNull(e.Find("#ysy-bolum"));
        Assert.False(e.Find("#ysy-onizle").HasAttribute("disabled"));

        agHatasi = false;
        e.Find("#ysy-onizle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-onay")));
        agHatasi = true;
        e.Find("#ysy-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains("Sunucuya ulaşılamadı", e.Find("#ysy-onay-hata").TextContent));
        Assert.NotNull(e.Find("#ysy-onay"));
    }

    [Fact]
    public void Tur2_ileri_tarihte_uyari_BUGUN_demez_gunu_soyler_ayni_gun_ipucu_yok_buyuk_degisim_uyarisi()
    {
        onizlemeJson = Onizleme.Replace("\"bugunMu\":true", "\"bugunMu\":false")
            .Replace("\"baslangic\":\"2026-10-08\"", "\"baslangic\":\"2026-10-15\"")
            .Replace("\"onayKodu\":\"ABCDEF0123456789\"", "\"onayKodu\":\"ABCDEF0123456789\",\"buyukDegisim\":true");
        var e = Ac(true);
        Doldur(e);
        e.Find("#ysy-onizle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-onay")));
        var uyari = e.Find("#ysy-onay-uyari").TextContent;
        Assert.Contains("15 Ekim 2026 gününden itibaren", uyari);
        Assert.DoesNotContain("bugün", uyari, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("15 Ekim 2026 tarihinde kendiliğinden", e.Find("#ysy-onay-gecerlilik").TextContent);
        Assert.Empty(e.FindAll("#degismez-ipucu-YasalParametreBugun"));
        Assert.Contains("çok değişiyor", e.Find("#ysy-onay-buyuk").TextContent);
    }

    [Fact]
    public void Tur2_bugunden_gecerlide_ayni_gun_duzeltme_yolu_DegismezIpucu_ile_soylenir_buyuk_degisim_yoksa_uyari_yok()
    {
        var e = Ac(true);
        Doldur(e);
        e.Find("#ysy-onizle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-onay")));
        var ipucu = e.Find("#degismez-ipucu-YasalParametreBugun").TextContent;
        // YASAL-PARAM-2: aynı gün düzeltme yolu artık VAR — ipucu onu söyler, "yarından geçerli" demez.
        Assert.Contains("kaydedildiği andan geçerlidir", ipucu);
        Assert.Contains("Bugün girilen değeri düzelt", ipucu);
        Assert.DoesNotContain("yarından", ipucu);
        Assert.Contains("bugünün bundan sonraki işlemleri", e.Find("#ysy-onay-uyari").TextContent);
        Assert.Empty(e.FindAll("#ysy-onay-buyuk"));
    }

    // ── TUR 3 (hasım ORTA/DÜŞÜK) ──────────────────────────────────────────────

    [Fact]
    public void Tur3_bos_gonder_sonra_sinir_sec_kaynak_hatasi_BAYAT_kalmaz_yer_tutucu_sade_sinir_secici_hatali()
    {
        var e = Ac(true);
        e.Find("#ysy-onizle").Click();
        Assert.Contains("Adres https:// ile başlamalı.", e.Markup);
        // Sınır seçicisi hatalı işaretli (etiket rengi yasal.css: .yasal-yonetim .secici-alan:has(.secici-tetik.hatali)).
        Assert.Contains("hatali", e.Find("#ysy-sinir").ClassList);
        Assert.NotNull(e.Find("#ysy-bolum #ysy-sinir-sar"));
        Assert.Equal("Resmî Gazete bağlantısını yapıştırın", e.Find("#ysy-kaynak").GetAttribute("placeholder"));

        e.Find("#ysy-sinir").Click();
        e.FindAll("li.secici-secenek").First(l => l.GetAttribute("data-deger") == "MASAK_KIMLIK_ESIGI_TL").Click();
        Assert.Equal(EskiRgAdresi, e.Find("#ysy-kaynak").GetAttribute("value"));
        Assert.DoesNotContain("Adres https:// ile başlamalı.", e.Markup);
        Assert.Empty(e.FindAll("#ysy-kaynak-hata"));
        Assert.DoesNotContain("hatali", e.Find("#ysy-sinir").ClassList);
    }

    [Fact]
    public void Tur3_RG_tarihi_degisince_onerilen_eski_adres_temizlenir_ayni_RG_tarihinde_ve_elle_yazilan_adres_korunur()
    {
        var e = Ac(true);
        e.Find("#ysy-sinir").Click();
        e.FindAll("li.secici-secenek").First(l => l.GetAttribute("data-deger") == "MASAK_KIMLIK_ESIGI_TL").Click();
        e.Find("#ysy-rg-tarihi").Change("2026-10-07");          // önerilen satırın kendi RG tarihi → öneri kalır
        Assert.Equal(EskiRgAdresi, e.Find("#ysy-kaynak").GetAttribute("value"));
        e.Find("#ysy-rg-tarihi").Change("2026-10-08");          // başka gazete → eski belge adresi temizlenir
        Assert.True(string.IsNullOrEmpty(e.Find("#ysy-kaynak").GetAttribute("value")));
        e.Find("#ysy-kaynak").Input(YeniRgAdresi);
        e.Find("#ysy-rg-tarihi").Change("2026-10-06");          // elle yazılan adrese dokunulmaz
        Assert.Equal(YeniRgAdresi, e.Find("#ysy-kaynak").GetAttribute("value"));
    }

    [Fact]
    public void Tur3_onay_penceresi_kaynak_adresini_gosterir_metinler_sade()
    {
        var e = Ac(true);
        Doldur(e);
        e.Find("#ysy-onizle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ysy-onay")));
        Assert.Equal($"Kaynak adresi: {YeniRgAdresi}", e.Find("#ysy-onay-kaynak").TextContent.Trim());
        Assert.Equal("Etkilenecek açık sipariş: 3", e.Find("#ysy-onay-acik").TextContent.Trim());
        var ipucu = e.Find("#degismez-ipucu-YasalParametreBugun").TextContent;
        Assert.Contains("Yanlış girdiyseniz bugün \"Bugün girilen değeri düzelt\" bölümünden gerekçeyle düzeltin (yalnız giren kişi).", ipucu);
        Assert.DoesNotContain("veritabanı", ipucu);
        Assert.DoesNotContain("(", e.Find("#ysy-onay-acik").TextContent);
    }

    [Fact]
    public void Tur3_kuyumcu_SAHIP_yonetim_1_ile_formu_ve_sistem_anahtari_alanini_GORMEZ_not_gorur()
    {
        yonetimAcik = false;   // sunucu: bu kiracı ByGatech yönetim kiracısı DEĞİL
        var e = Ac(true);
        Assert.Empty(e.FindAll("#ysy-bolum"));
        Assert.Empty(e.FindAll("#ysy-anahtar"));
        Assert.Empty(e.FindAll("button#ysy-onizle"));
        Assert.Contains("Bu bölüm ByGatech içindir", e.Find("#ys-yonetim-kapali").TextContent);
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/sistem/"));
        // ?yonetim=1 olmadan not da çıkmaz (kuyumcunun olağan ekranı değişmedi).
        yonetimAcik = true;
        var olagan = Ac(false);
        Assert.Empty(olagan.FindAll("#ys-yonetim-kapali"));
        Assert.Empty(olagan.FindAll("#ysy-bolum"));
    }

    [Fact]
    public void Tur3_kartlar_teyitsizde_ne_zamandan_beri_satiri_YOK_baslikta_teyit_parantezi_YOK_toplama_notu()
    {
        var e = Ac(false);
        Assert.Empty(e.FindAll("#ys-baslangic-masak-amac-beyani-esigi-tl"));          // TEYITSIZ
        Assert.Contains("7 Ekim 2026", e.Find("#ys-baslangic-masak-kimlik-esigi-tl").TextContent);   // DOGRULANDI
        var ad = e.Find("#ys-ad-masak-amac-beyani-esigi-tl").TextContent;
        Assert.DoesNotContain("(", ad);
        Assert.DoesNotContain("teyid", ad);
        e.Find("#ys-ayrinti-masak-kimlik-esigi-tl-ac").Click();
        Assert.Equal(Kuyumcu.Core.I18n.YasalParamSozlugu.Tr.ToplamaKurali,
            e.Find("#ys-yorum-masak-kimlik-esigi-tl").TextContent.Trim());
    }
}

// ── MASAK formu başlığı / kuralı (bUnit) ────────────────────────────────────

public sealed class YasalParamMasakFormuTestleri : BunitContext
{
    public YasalParamMasakFormuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private static Kuyumcu.Contracts.Bilesenler.MasakFormu.Esikler Esik() =>
        new(370_000m, 200_000m, 2_000_000.01m, 20_000_000.01m, 20, ["TC_KIMLIK"]) { KimlikIstenenEnDusukTutar = 200_000m };

    private IRenderedComponent<Kuyumcu.Web.Bilesenler.MasakFormu> Ciz(MasakGereksinim g, bool sade, decimal tutar = 185_000m) =>
        Render<Kuyumcu.Web.Bilesenler.MasakFormu>(p => p
            .Add(x => x.Model, new Kuyumcu.Web.Bilesenler.MasakFormu.MasakBilgi())
            .Add(x => x.Tutar, tutar)
            .Add(x => x.Esik, Esik())
            .Add(x => x.Gereksinim, g)
            .Add(x => x.Sade, sade));

    [Fact]
    public void Baglantili_toplamda_baslik_bugunku_toplami_soyler_BASIT_ve_PRO()
    {
        var g = new MasakGereksinim("YALNIZ_KIMLIK", true, false, false, false, 0) { OlcuTutarTl = 370_000m, IslemTutarTl = 185_000m };
        foreach (var sade in new[] { true, false })
        {
            var e = Ciz(g, sade);
            Assert.True(e.FindAll("#masak-baslik").Count == 1, e.Markup[..Math.Min(600, e.Markup.Length)]);
            Assert.Equal($"Kimlik gerekli — bu müşterinin bugünkü toplamı {Bicim.Tl(370_000m)}", e.Find("#masak-baslik").TextContent);
        }
    }

    [Fact]
    public void Tek_islemde_baslik_tetikleyen_kademeyi_soyler_IKI_MODDA_AYNI_PRO_kural_tek_cumle_kalibi()
    {
        // YASAL-PARAM-EKRAN tur 2 (tasarım hakemi KRİTİK): 250.000 → amaç kademesi; başlık iki modda aynı.
        var g = new MasakGereksinim("AMAC_BEYANI", true, true, false, false, 20) { OlcuTutarTl = 250_000m, IslemTutarTl = 250_000m };
        var beklenen = string.Format(Kuyumcu.Core.I18n.YasalParamSozlugu.Tr.BaslikAmac, Bicim.Tl(200_000m));
        Assert.Equal(beklenen, Ciz(g, true, 250_000m).Find("#masak-baslik").TextContent);
        var pro = Ciz(g, false, 250_000m);
        Assert.Equal(beklenen, pro.Find("#masak-baslik").TextContent);
        Assert.Empty(pro.FindAll(".kart--masak .rozet"));
        Assert.Equal(
            $"Kimlik {Bicim.Tl(370_000m)} ve üstünde zorunlu (MASAK). {Bicim.Tl(200_000m)} ve üstünde işlem amacı beyanıyla birlikte de kimlik istenir (mali müşavir teyidi bekleniyor). Bilgiler şifreli saklanır.",
            pro.Find("#masak-kural").TextContent);

        // 400.000 → md.5/1-b kimlik eşiği (370.000) söylenir, iki modda aynı.
        var g4 = new MasakGereksinim("AMAC_BEYANI", true, true, false, false, 20) { OlcuTutarTl = 400_000m, IslemTutarTl = 400_000m };
        var b4 = string.Format(Kuyumcu.Core.I18n.YasalParamSozlugu.Tr.BaslikKimlik, Bicim.Tl(370_000m));
        Assert.Equal(b4, Ciz(g4, true, 400_000m).Find("#masak-baslik").TextContent);
        Assert.Equal(b4, Ciz(g4, false, 400_000m).Find("#masak-baslik").TextContent);
    }

    [Fact]
    public void Tur2_istege_bagli_alanlar_KAPALI_katlanirda_zorunlu_adres_disarida_PRO_etiket_TCKN_siz()
    {
        var g = new MasakGereksinim("AMAC_BEYANI", true, true, false, false, 20) { OlcuTutarTl = 250_000m, IslemTutarTl = 250_000m };
        foreach (var sade in new[] { true, false })
        {
            var e = Ciz(g, sade, 250_000m);
            Assert.Empty(e.FindAll("#masak-dogum"));
            Assert.Empty(e.FindAll("#masak-uyruk"));
            Assert.Empty(e.FindAll("#masak-adres"));
            Assert.Equal("Diğer bilgiler (isteğe bağlı)", e.Find("#masak-diger-ac").TextContent.Trim());
            Assert.Contains(Kuyumcu.Core.I18n.ZoIslemSozlugu.Tr.MasakBelgeNoTc, e.Find("label[for=masak-belge-no]").TextContent);
            Assert.DoesNotContain("TCKN", e.Find(".kart--masak").TextContent);
            e.Find("#masak-diger-ac").Click();
            Assert.NotNull(e.Find("#masak-dogum"));
            Assert.NotNull(e.Find("#masak-uyruk"));
            Assert.NotNull(e.Find("#masak-adres"));
        }
        // Gider pusulası adresi ZORUNLU ise katlanırda saklanmaz.
        var z = Render<Kuyumcu.Web.Bilesenler.MasakFormu>(p => p
            .Add(x => x.Model, new Kuyumcu.Web.Bilesenler.MasakFormu.MasakBilgi())
            .Add(x => x.Tutar, 250_000m).Add(x => x.Esik, Esik()).Add(x => x.Gereksinim, g)
            .Add(x => x.AdresZorunlu, true));
        Assert.Equal("true", z.Find("#masak-adres").GetAttribute("aria-required"));
        Assert.Empty(z.FindAll("#masak-dogum"));
        z.Find("#masak-diger-ac").Click();
        Assert.Single(z.FindAll("#masak-adres"));
    }

    [Theory]
    [InlineData(400_000, 0, "Kimlik")]
    [InlineData(370_000, 0, "Kimlik")]
    [InlineData(369_999.99, 0, "Amac")]
    [InlineData(250_000, 0, "Amac")]
    [InlineData(199_999.99, 0, "Yok")]
    [InlineData(185_000, 370_000, "Toplam")]
    public void Tur2_MasakKademe_tek_secici(double tutar, double toplam, string beklenen)
    {
        MasakGereksinim? g = toplam > 0
            ? new MasakGereksinim("YALNIZ_KIMLIK", true, false, false, false, 0) { OlcuTutarTl = (decimal)toplam, IslemTutarTl = (decimal)tutar }
            : (decimal)tutar >= 200_000m ? new MasakGereksinim("AMAC_BEYANI", true, true, false, false, 20) : null;
        Assert.Equal(beklenen, MasakKademe.Sec(Esik(), g, (decimal)tutar).ToString());
    }

    [Fact]
    public void Tur2_PRO_yasal_kapilar_kimlik_satiri_370_MASAK_amac_ayri_satir_kilitle_ayni_kural()
    {
        var P = Kuyumcu.Core.I18n.SatisProSozlugu.Tr;
        var YP = Kuyumcu.Core.I18n.YasalParamSozlugu.Tr;
        IRenderedComponent<Kuyumcu.Web.Bilesenler.Fis.FisProYanSutun> Panel(decimal olcu) =>
            Render<Kuyumcu.Web.Bilesenler.Fis.FisProYanSutun>(p => p
                .Add(x => x.KimlikEsigi, 200_000m).Add(x => x.MasakKimlikEsigi, 370_000m)
                .Add(x => x.AmacEsigi, 200_000m).Add(x => x.MasakOlcu, olcu).Add(x => x.KimlikAsildi, olcu >= 200_000m)
                .Add(x => x.Toplam, olcu).Add(x => x.NakitAsildi, false).Add(x => x.NakitHad, 30_000m));
        var p400 = Panel(400_000m);
        Assert.Equal(string.Format(P.KimlikEsigi, Bicim.Tl(370_000m)), p400.Find("#islem-pro-kimlik-etiket").TextContent.Trim());
        Assert.Equal(P.KimlikGerekir, p400.Find("#islem-pro-kimlik").TextContent.Trim());
        Assert.Equal(string.Format(YP.PanelAmac, Bicim.Tl(200_000m)), p400.Find("#islem-pro-amac-etiket").TextContent.Trim());
        Assert.Equal(YP.PanelAmacGerekir, p400.Find("#islem-pro-amac").TextContent.Trim());

        // 250.000: kimlik satırı md.5/1-b'nin ALTINDA; kimliği amaç kademesi istiyor (ayrı satır).
        var p250 = Panel(250_000m);
        Assert.Equal(P.Altinda, p250.Find("#islem-pro-kimlik").TextContent.Trim());
        Assert.Equal(YP.PanelAmacGerekir, p250.Find("#islem-pro-amac").TextContent.Trim());
        Assert.DoesNotContain(Bicim.Tl(200_000m), p250.Find("#islem-pro-kimlik-etiket").TextContent);

        var p100 = Panel(100_000m);
        Assert.Equal(P.Altinda, p100.Find("#islem-pro-kimlik").TextContent.Trim());
        Assert.Equal(P.Altinda, p100.Find("#islem-pro-amac").TextContent.Trim());
    }
}
