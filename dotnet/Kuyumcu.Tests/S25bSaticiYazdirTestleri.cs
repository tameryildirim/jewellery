using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-5b — SATICI PERSONEL (hareketler.personel_id = SATAN) + OTOMATİK YAZDIR
//
// HTTP (gerçek boru hattı, kuyumcu_test):
//   H1 personelId'siz satış → oturum kullanıcısı          (KABUL 1)
//   H2 SAHIP başka aktif kullanıcıyı seçer → o yazılır     (KABUL 2)
//   H3 başka kiracının kullanıcısı → 400, hareket 0        (KABUL 3)
//   H4 pasif kullanıcı → 400, hareket 0                    (KABUL 4)
//   H5 TEZGAHTAR başkasını seçer → 403, hareket 0; kendini → 200 (KABUL 5)
//   H6 satis-fisi: tüm SATIŞ kalemlerine aynı satan; alansız → oturum
//   H7 GET /islemler/personeller: SAHIP tümü (pasif/yabancı yok), TEZGAHTAR yalnız kendi
//   H8 boş dizge personelId → 400
// bUnit (Islemler):
//   E1 pro + seçim serbest + 2 kişi → #islem-personel; seçim gövdeye personelId (KABUL 6)
//   E2 basit → seçici YOK, "Satan: <ad>" var, gövdede personelId YOK (KABUL 6)
//   E3 tek kullanıcı (pro) → seçici YOK, salt okunur satır (KABUL 7)
//   E4 liste alınamadı → hata bandı, seçici yok, satış yine gider (UX-S28)
//   E5 pro + varsayılan (dokunulmadı) → gövdede personelId yok (sunucu oturumu yazar)
// bUnit (Cikti): E6 ?yazdir=1 → print BİR KEZ; E7 sorgusuz → print YOK (KABUL 8)
// Saf: S1 FisYazdir.YazdirAdresi · S2 SaticiKapisi · S3 sözlük TR+EN
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class S25bSaticiHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private string KullaniciEkle(Kiraci k, string ad, bool aktif = true, string rol = "TEZGAHTAR")
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Kullanicilar.Add(new Kullanici
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Ad = ad, Soyad = "Personel",
            Email = $"{id}@test.local", SifreHash = "x", Rol = rol, Aktif = aktif,
            Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private List<(string Id, string? PersonelId, string KullaniciId)> Satislar(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS")
            .Select(h => new { h.Id, h.PersonelId, h.KullaniciId }).ToList()
            .Select(x => (x.Id, x.PersonelId, x.KullaniciId)).ToList();
    }

    private static object SatisGovde(Kiraci k, string? personelId, bool alanVar = true) => alanVar
        ? new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000m,
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId, personelId,
        }
        : new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000m,
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        };

    private static async Task<string> Mesaj(HttpResponseMessage c) =>
        JsonDocument.Parse(await c.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString()!;

    [Fact]
    public async Task S25b_H1_personelId_gonderilmezse_oturum_kullanicisi_yazilir()
    {
        var k = YeniKiraci("S25b H1");
        var c = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/satis", SatisGovde(k, null, alanVar: false));
        Assert.True(c.StatusCode == HttpStatusCode.OK, await c.Content.ReadAsStringAsync());
        var s = Assert.Single(Satislar(k));
        Assert.Equal(k.KullaniciId, s.PersonelId);
        Assert.Equal(k.KullaniciId, s.KullaniciId);

        // null açıkça gönderilse de aynı anlam: oturum.
        var c2 = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/satis", SatisGovde(k, null));
        Assert.True(c2.StatusCode == HttpStatusCode.OK, await c2.Content.ReadAsStringAsync());
        Assert.All(Satislar(k), x => Assert.Equal(k.KullaniciId, x.PersonelId));
    }

    [Fact]
    public async Task S25b_H2_SAHIP_baska_aktif_personeli_secer_o_yazilir_kaydi_giren_degismez()
    {
        var k = YeniKiraci("S25b H2");
        var ayse = KullaniciEkle(k, "Ayşe");
        var c = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/satis", SatisGovde(k, ayse));
        Assert.True(c.StatusCode == HttpStatusCode.OK, await c.Content.ReadAsStringAsync());
        var s = Assert.Single(Satislar(k));
        Assert.Equal(ayse, s.PersonelId);          // SATAN
        Assert.Equal(k.KullaniciId, s.KullaniciId); // kaydı GİREN
    }

    [Fact]
    public async Task S25b_H3_baska_kiracinin_kullanicisi_400_hareket_yazilmaz()
    {
        var k = YeniKiraci("S25b H3 A");
        var yabanci = YeniKiraci("S25b H3 B");
        var c = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/satis", SatisGovde(k, yabanci.KullaniciId));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Equal(SaticiKapisi.BulunamadiMesaji, await Mesaj(c));
        Assert.Empty(Satislar(k));

        // Fiş ucunda da aynı kapı.
        var cf = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/satis-fisi", FisGovde(k, yabanci.KullaniciId));
        Assert.Equal(HttpStatusCode.BadRequest, cf.StatusCode);
        Assert.Empty(Satislar(k));
    }

    [Fact]
    public async Task S25b_H4_pasif_personel_400_hareket_yazilmaz()
    {
        var k = YeniKiraci("S25b H4");
        var pasif = KullaniciEkle(k, "Pasif", aktif: false);
        var c = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/satis", SatisGovde(k, pasif));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Equal(SaticiKapisi.PasifMesaji, await Mesaj(c));
        Assert.Empty(Satislar(k));
    }

    [Fact]
    public async Task S25b_H5_TEZGAHTAR_baskasini_secemez_403_kendini_secebilir()
    {
        var k = YeniKiraci("S25b H5");
        var ayse = KullaniciEkle(k, "Ayşe");
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");

        var c = await tezgahtar.PostAsJsonAsync("/api/v1/islemler/satis", SatisGovde(k, ayse));
        Assert.Equal(HttpStatusCode.Forbidden, c.StatusCode);
        Assert.Equal(SaticiKapisi.YetkiMesaji, await Mesaj(c));
        Assert.Empty(Satislar(k));

        var cf = await tezgahtar.PostAsJsonAsync("/api/v1/islemler/satis-fisi", FisGovde(k, ayse));
        Assert.Equal(HttpStatusCode.Forbidden, cf.StatusCode);
        Assert.Empty(Satislar(k));

        var ok = await tezgahtar.PostAsJsonAsync("/api/v1/islemler/satis", SatisGovde(k, k.KullaniciId));
        Assert.True(ok.StatusCode == HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());
        Assert.Equal(k.KullaniciId, Assert.Single(Satislar(k)).PersonelId);
    }

    private static object FisGovde(Kiraci k, string? personelId) => new
    {
        cariId = k.CariId, kurSnapshotId = k.KurId, personelId,
        kalemler = new[]
        {
            new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 600m },
            new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 400m },
        },
        odemeler = new object[] { new { arac = "NAKIT", tutarTl = 1_000m, kasaId = k.KasaId } },
    };

    [Fact]
    public async Task S25b_H6_satis_fisi_tum_kalemlere_ayni_satan_alansiz_oturum()
    {
        var k = YeniKiraci("S25b H6");
        var ayse = KullaniciEkle(k, "Ayşe");
        var c = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/satis-fisi", FisGovde(k, ayse));
        Assert.True(c.StatusCode == HttpStatusCode.OK, await c.Content.ReadAsStringAsync());
        var ilk = Satislar(k);
        Assert.Equal(2, ilk.Count);
        Assert.All(ilk, x => Assert.Equal(ayse, x.PersonelId));

        var c2 = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/satis-fisi", FisGovde(k, null));
        Assert.True(c2.StatusCode == HttpStatusCode.OK, await c2.Content.ReadAsStringAsync());
        var yeni = Satislar(k).Where(x => ilk.All(i => i.Id != x.Id)).ToList();
        Assert.Equal(2, yeni.Count);
        Assert.All(yeni, x => Assert.Equal(k.KullaniciId, x.PersonelId));
    }

    [Fact]
    public async Task S25b_H7_personeller_ucu_SAHIP_tumunu_TEZGAHTAR_yalniz_kendini_gorur()
    {
        var k = YeniKiraci("S25b H7");
        var ayse = KullaniciEkle(k, "Ayşe");
        var pasif = KullaniciEkle(k, "Pasif", aktif: false);
        var yabanci = YeniKiraci("S25b H7 B");

        var s = await f.Ortam.Istemci(k).GetAsync("/api/v1/islemler/personeller");
        Assert.Equal(HttpStatusCode.OK, s.StatusCode);
        var j = JsonDocument.Parse(await s.Content.ReadAsStringAsync()).RootElement;
        Assert.True(j.GetProperty("secimSerbest").GetBoolean());
        Assert.Equal(k.KullaniciId, j.GetProperty("oturumId").GetString());
        var idler = j.GetProperty("personeller").EnumerateArray().Select(p => p.GetProperty("id").GetString()).ToList();
        Assert.Contains(k.KullaniciId, idler);
        Assert.Contains(ayse, idler);
        Assert.DoesNotContain(pasif, idler);
        Assert.DoesNotContain(yabanci.KullaniciId, idler);
        // Kişisel veri en az: e-posta/rol taşınmaz.
        Assert.All(j.GetProperty("personeller").EnumerateArray(), p =>
        {
            Assert.False(p.TryGetProperty("email", out _));
            Assert.False(p.TryGetProperty("rol", out _));
        });

        var t = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/islemler/personeller");
        Assert.Equal(HttpStatusCode.OK, t.StatusCode);
        var tj = JsonDocument.Parse(await t.Content.ReadAsStringAsync()).RootElement;
        Assert.False(tj.GetProperty("secimSerbest").GetBoolean());
        var tek = Assert.Single(tj.GetProperty("personeller").EnumerateArray());
        Assert.Equal(k.KullaniciId, tek.GetProperty("id").GetString());
    }

    [Fact]
    public async Task S25b_H8_bos_dizge_personelId_400()
    {
        var k = YeniKiraci("S25b H8");
        var c = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/satis", SatisGovde(k, " "));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Empty(Satislar(k));
    }
}

public sealed class S25bSaticiEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi api;

    public S25bSaticiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        api = new SahteApi(istekler);
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(api) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    /// <summary>Mod geçiş testi de aynı gerçekçi yanıtları kullanır.</summary>
    internal sealed class PaylasilanSahte(List<(string, string, string)> kayit) : SahteApi(kayit);

    internal class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        public string Personeller { get; set; } =
            """{"personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"},{"id":"u2","ad":"Ayşe","soyad":"Demir"}],"oturumId":"u1","secimSerbest":true}""";
        public bool PersonelHatali { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, yol, govde));

            if (PersonelHatali && yol.Contains("/islemler/personeller"))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"statusCode":500,"message":"Personel okunamadı (sahte)","error":"Internal Server Error"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var y = yol switch
            {
                var v when v.Contains("/islemler/personeller") => Personeller,
                var v when v.EndsWith("/cari/sistem-carisi") =>
                    """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/gereksinim") => """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/pos/cihazlar") => "[]",
                var v when v.Contains("/islemler/satis") => """{"id":"h1","fisId":"h1","tur":"SATIS","tlTutari":10000,"cariId":"pm1"}""",
                var v when v.Contains("/reyon") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    // TEMA TEMİZLİĞİ 17.10: SATIŞ fişi artık hep Yıldız tezgâhıdır. PRO'da satıcı seçicisi (FisSatici — #islem-personel,
    // #islem-satan, #islem-personel-ipucu/-sinir/-hata AYNI) müşteri "Değiştir" katmanının "+ Satıcı ve fiyat listesi"
    // ekindedir (#yildiz-musteri-pro-ac → #yildiz-musteri-pro). BASİT Yıldız tezgâhında "Satan" salt okunur satırı
    // BİLEREK çizilmez (tuvalde yok — Islemler.razor OdemeParcasi notu); gövdeye personelId yine girmez.
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "SAHIP", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().YildizHazir();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("/islemler/personeller")));
        return e;
    }

    /// <summary>PRO: "Değiştir" katmanında satıcı/fiyat listesi ekini açar (hata varsa ek zaten açık gelir).</summary>
    internal static void SaticiAc(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        if (e.FindAll("#yildiz-musteri-secici").Count == 0) e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement("#yildiz-musteri-secici");
        if (e.FindAll("#yildiz-musteri-pro").Count == 0) e.Find("#yildiz-musteri-pro-ac").Click();
        e.WaitForElement("#yildiz-musteri-pro");
    }

    private static void KalemVeKaydet(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        e.YildizKalemEkle(beklenenFiyat: "10000");
        e.YildizKaydet();
    }

    private JsonElement SatisPostu()
    {
        var p = istekler.Where(i => i.Metot == "POST" && (i.Yol.EndsWith("/islemler/satis") || i.Yol.EndsWith("/islemler/satis-fisi"))).ToList();
        return JsonDocument.Parse(Assert.Single(p).Govde).RootElement;
    }

    [Fact]
    public void S25b_E1_pro_modda_secici_var_secim_govdeye_personelId_olarak_gider()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SaticiAc(e);
        e.WaitForElement("#yildiz-musteri-pro #islem-personel");
        Assert.Equal(2, SeciciYardim.Secenekler(e, "islem-personel").Count);
        Assert.Equal("u1", SeciciYardim.SeciliDeger(e, "islem-personel"));   // varsayılan oturum
        SeciciYardim.Sec(e, "islem-personel", "u2");
        KalemVeKaydet(e);
        e.WaitForAssertion(() => Assert.Equal("u2", SatisPostu().GetProperty("personelId").GetString()));
        // Fiş bileşeni istek atmadı: personeller yalnız bir kez (sayfa yüklemesi).
        Assert.Single(istekler, i => i.Yol.Contains("/islemler/personeller"));
    }

    [Fact]
    public void S25b_E2_basit_modda_secici_yok_Satan_satiri_var_govdede_personelId_yok()
    {
        // TEMA TEMİZLİĞİ 17.10: "Satan" satırı Yıldız BASİT tezgâhında bilerek yok (tuval) — o iddia kaldırıldı;
        // seçicinin olmadığı ve gövdeye personelId girmediği güvencesi AYNEN.
        var e = Ekran(ArayuzModuServisi.Basit);
        e.Find("#yildiz-tezgah-degistir").Click();
        e.WaitForElement("#yildiz-musteri-secici");                 // katman açık — boş DOM değil
        Assert.Empty(e.FindAll("#yildiz-musteri-pro-ac, #islem-personel"));
        e.Find("#yildiz-tezgah-degistir").Click();
        KalemVeKaydet(e);
        e.WaitForAssertion(() => Assert.False(SatisPostu().TryGetProperty("personelId", out var v) && v.ValueKind != JsonValueKind.Null));
    }

    [Fact]
    public void S25b_E3_tek_kullanicida_secici_gizli_salt_okunur_satir()
    {
        api.Personeller = """{"personeller":[{"id":"u1","ad":"Tek","soyad":"Kişi"}],"oturumId":"u1","secimSerbest":true}""";
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SaticiAc(e);
        e.WaitForAssertion(() => Assert.Contains("Tek Kişi", e.Find("#islem-satan").TextContent));
        Assert.Empty(e.FindAll("#islem-personel"));
    }

    [Fact]
    public void S25b_E3b_secim_serbest_degilse_secici_gizli_sebep_yazar()
    {
        api.Personeller = """{"personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"}],"oturumId":"u1","secimSerbest":false}""";
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SaticiAc(e);
        e.WaitForElement("#islem-personel-ipucu");
        Assert.Empty(e.FindAll("#islem-personel"));
        Assert.Equal(FisSaticiSozlugu.Tr.KendinizIpucu, e.Find("#islem-personel-ipucu").TextContent.Trim());
    }

    [Fact]
    public void S25b_E9_liste_tavana_degdiyse_ekran_soyler_UX_S29()
    {
        api.Personeller = """{"personeller":[{"id":"u1","ad":"Test","soyad":"Tezgâhtar"},{"id":"u2","ad":"Ayşe","soyad":"Demir"}],"oturumId":"u1","secimSerbest":true,"dahaFazlasiVar":true}""";
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SaticiAc(e);
        e.WaitForElement("#islem-personel");
        Assert.Equal(FisSaticiSozlugu.Tr.DahaFazlasiVar, e.Find("#islem-personel-sinir").TextContent.Trim());
    }

    [Fact]
    public void S25b_E4_liste_alinamazsa_hata_bandi_secici_yok_satis_yine_gider()
    {
        api.PersonelHatali = true;
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        e.Find("#yildiz-tezgah-degistir").Click();
        // UX-S28: hata gizlenmez — katman açılınca satıcı eki KENDİLİĞİNDEN açık gelir (ProUstAcikZorunlu)
        e.WaitForElement("#yildiz-musteri-pro");
        e.WaitForAssertion(() => Assert.Contains("Personel okunamadı (sahte)", e.Find("#islem-personel-hata").TextContent));
        Assert.Empty(e.FindAll("#islem-personel"));
        Assert.Contains("Test Tezgâhtar", e.Find("#islem-satan").TextContent);
        KalemVeKaydet(e);
        e.WaitForAssertion(() => Assert.False(SatisPostu().TryGetProperty("personelId", out var v) && v.ValueKind != JsonValueKind.Null));
    }

    [Fact]
    public void S25b_E5_pro_varsayilan_dokunulmadiysa_gövdede_personelId_yok()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        SaticiAc(e);
        e.WaitForElement("#islem-personel");
        KalemVeKaydet(e);
        e.WaitForAssertion(() => Assert.False(SatisPostu().TryGetProperty("personelId", out var v) && v.ValueKind != JsonValueKind.Null));
    }
}
public sealed class S25bModGecisTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public S25bModGecisTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new S25bSaticiEkranTestleri.PaylasilanSahte(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    /// <summary>
    /// Pro modda başkası seçilip BASİT moda geçilirse: basit mod seçimi taşımaz —
    /// "Satan" satırı oturum kullanıcısını yazar ve gövdeye personelId KONMAZ
    /// (ekranda görünen ile deftere yazılan aynı olmalı — UX-S22).
    /// </summary>
    [Fact]
    public void S25b_E8_pro_secimi_basit_moda_tasinmaz_gorunen_ile_yazilan_ayni()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Tezgâhtar", "t@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        // TEMA TEMİZLİĞİ 17.10: Yıldız tezgâhı — PRO seçici "Değiştir" katmanının satıcı ekinde; BASİT'te seçici ve
        // "Satan" satırı yok (tuval). Güvence AYNI: PRO'daki seçim BASİT'e taşınmaz, gövdede personelId yok.
        var e = Render<Kuyumcu.Web.Pages.Islemler>().YildizHazir();
        S25bSaticiEkranTestleri.SaticiAc(e);
        e.WaitForElement("#islem-personel");
        SeciciYardim.Sec(e, "islem-personel", "u2");
        var modu = Services.GetRequiredService<ArayuzModuServisi>();
        e.InvokeAsync(() => modu.Uygula(ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-personel, #yildiz-musteri-pro-ac")));
        Assert.Single(e.FindAll("#yildiz-musteri-secici"));   // katman açık — boş DOM değil

        e.YildizKalemEkle(beklenenFiyat: "10000");
        e.YildizKaydet();
        e.WaitForAssertion(() =>
        {
            var p = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/islemler/satis"));
            var g = JsonDocument.Parse(p.Govde).RootElement;
            Assert.False(g.TryGetProperty("personelId", out var v) && v.ValueKind != JsonValueKind.Null);
        });
    }
}

public sealed class S25bOtomatikYazdirTestleri : BunitContext
{
    public S25bOtomatikYazdirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Belge()) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Belge : HttpMessageHandler
    {
        public bool Hata { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            const string y = """
            {"tur":"SATIS_FISI","belgeAdi":"SATIŞ FİŞİ","belgeNo":"01ABCDEF","dayanak":null,
             "duzenlemeTarihi":"2026-09-21T08:00:00Z",
             "duzenleyen":{"unvan":"Test Kuyumcu","sube":"Merkez","adres":"Çarşı 1","vergiDairesi":"Beyazıt","vergiNo":"1234567890","telefon":"02120000000"},
             "karsi":{"baslik":"Müşteri","ad":"Ayşe Yılmaz","vergiNo":null,"adres":null,"telefon":null},
             "bolumler":[{"baslik":"Kalemler","sutunBasliklari":["Tür","Tutar"],
               "satirlar":[{"hucreler":["Satış","1000.00"],"vurgu":false}],"not":null}],
             "tutarlar":[{"etiket":"Toplam tutar","deger":1000.00,"paraBirimi":"TRY","toplam":true}],
             "imzaBasliklari":["Satıcı","Alıcı"],"not":null,"uyari":null}
            """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private int PrintSayisi() => JSInterop.Invocations.Count(i => i.Identifier == "window.print");

    private IRenderedComponent<Kuyumcu.Web.Pages.Cikti> Ac(string adres)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(adres);
        var e = Render<Kuyumcu.Web.Pages.Cikti>(p => p.Add(x => x.Tur, BelgeTurleri.SatisFisi).Add(x => x.Id, "01ABCDEF"));
        e.WaitForElement("#cikti-belge");
        return e;
    }

    [Fact]
    public void S25b_E6_yazdir_sorgusuyla_acilinca_belge_cizildikten_sonra_BIR_KEZ_yazdirir()
    {
        var e = Ac("/cikti/SATIS_FISI/01ABCDEF?" + FisYazdir.OtomatikYazdirSorgusu);
        e.WaitForAssertion(() => Assert.Equal(1, PrintSayisi()));
        // Yeniden çizim ikinci kez yazdırmaz.
        e.Render();
        e.Render();
        Assert.Equal(1, PrintSayisi());
    }

    [Fact]
    public void S25b_E7_sorgusuz_acilista_yazdirma_cagrilmaz_dugme_yine_calisir()
    {
        var e = Ac("/cikti/SATIS_FISI/01ABCDEF");
        e.Render();
        Assert.Equal(0, PrintSayisi());
        e.Find("#cikti-yazdir").Click();
        Assert.Equal(1, PrintSayisi());
    }
}

public sealed class S25bSafTestleri
{
    [Fact]
    public void S25b_S1_YazdirAdresi_otomatik_sorguyu_ekler_bos_kimlikte_null()
    {
        Assert.Equal("/cikti/SATIS_FISI/h%2F1?yazdir=1", FisYazdir.YazdirAdresi("h/1"));
        Assert.Null(FisYazdir.YazdirAdresi(" "));
        Assert.Null(FisYazdir.YazdirAdresi(null));
    }

    [Theory]
    [InlineData("SAHIP", true)]
    [InlineData("MUDUR", true)]
    [InlineData("TEZGAHTAR", false)]
    [InlineData("MUHASEBE", false)]
    [InlineData("sahip", false)]   // TAM eşleşme
    [InlineData(null, false)]
    public void S25b_S2_SaticiKapisi_yalniz_SAHIP_MUDUR_baskasini_secer(string? rol, bool beklenen) =>
        Assert.Equal(beklenen, SaticiKapisi.BaskasiniSecebilir(rol));

    [Fact]
    public void S25b_S3_sozluk_TR_EN_dolu_ham_kod_yok()
    {
        foreach (var m in new[] { FisSaticiSozlugu.Tr, FisSaticiSozlugu.En })
            foreach (var p in typeof(FisSaticiMetinleri).GetProperties())
            {
                var v = (string)p.GetValue(m)!;
                Assert.False(string.IsNullOrWhiteSpace(v), p.Name);
                foreach (var ham in new[] { "SAHIP", "MUDUR", "TEZGAHTAR", "personel_id" })
                    Assert.DoesNotContain(ham, v, StringComparison.Ordinal);
            }
        Assert.True(FisSaticiSozlugu.Tr.Satan.Split(' ').Length <= 3);
    }
}
