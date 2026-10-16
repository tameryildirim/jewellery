using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PATCH-5 (03.10) — Kasa / Cari / Virman / Gider / Banka / POS yazma uçlarında
// GİRDİ doğrulaması: "önce yuvarla, sonra ≤ 0" (sıfır tutarlı satır doğmaz), kolon
// tavanı (500 değil 400 + alan), PATCH sözleşmesi (zorunlu alan null = KORU, "" = 400;
// isteğe bağlı "" = SİL) ve Gün Kapat'ta sayılan nakdin SUNUCUDA zorunlu olması.
// PARA KURALLARI (nakit/araç payı kapısı, çift bacak) bu pakette DEĞİŞMEDİ.
// Her HTTP vakası: kod + alan (gövde AYRIŞTIRILARAK) ve veritabanında satır önce/sonra.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PatchBesHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCH5 {ad}");
    }

    /// <summary>Servis yolu (DataAnnotations ÇALIŞMAZ) — kiracı bağlamı kurulmuş DI kapsamı.</summary>
    private AsyncServiceScope Kapsam(Kiraci k)
    {
        var s = f.Ortam.Services.CreateAsyncScope();
        s.ServiceProvider.GetRequiredService<KapsamBaglamAyari>()
            .Ayarla(new SabitKiraciBaglami(k.TenantId, k.SubeId, tumSubeler: true));
        return s;
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    private static async Task Alan400(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "400 gövdesinde 'alanlar' yok: " + govde);
        var anahtarlar = alanlar.EnumerateObject().Select(p => p.Name).ToList();
        Assert.True(anahtarlar.Any(a => a.Equals(alan, StringComparison.OrdinalIgnoreCase)),
            $"400 '{alan}' alanına işaretli değil: {string.Join(",", anahtarlar)} · {govde}");
    }

    private static async Task Tamam(HttpResponseMessage r)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {govde}");
    }

    /// <summary>Kasanın bakiyesi ve hareket/kapanış sayısı — "satır yazılmaz, bakiye değişmez" kanıtı.</summary>
    private (decimal Bakiye, int Hareket, int Kapanis, int Defter) KasaDurum(Kiraci k, string? kasaId = null)
    {
        using var db = f.Vt.OlusturSistem();
        var id = kasaId ?? k.KasaId;
        return (db.Kasalar.AsNoTracking().Single(x => x.Id == id).Bakiye,
                db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId),
                db.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId),
                db.Hareketler.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    // ═══════════ GÜN KAPAT — sayılan nakit SUNUCUDA zorunlu ═══════════

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"aciklama":"sayım yapılmadı ama kapatıyorum"}""")]
    [InlineData("""{"sayilanNakit":null,"aciklama":"boş gönderim"}""")]
    public async Task P5_gun_kapat_sayilanNakit_GONDERILMEZSE_400_alan_sayilanNakit_kapanis_YAZILMAZ(string json)
    {
        var k = Kur("gun kapat zorunlu");
        var once = KasaDurum(k);
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post,
            $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", json), "sayilanNakit");
        Assert.Equal(once, KasaDurum(k));
        Assert.Equal(0, once.Kapanis);
    }

    [Fact]
    public async Task P5_gun_kapat_sayilanNakit_kolon_tavanini_asarsa_400_alan_sayilanNakit()
    {
        var k = Kur("gun kapat tavan");
        var once = KasaDurum(k);
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post,
            $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", """{"sayilanNakit":1e14,"aciklama":"tavan denemesi"}"""), "sayilanNakit");
        Assert.Equal(once, KasaDurum(k));
    }

    [Fact]
    public async Task P5_gun_kapat_SERVIS_yolunda_da_sayilanNakit_zorunlu_alan_sayilanNakit()
    {
        var k = Kur("gun kapat servis");
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Kasa.KasaServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            servis.GunKapat(k.KasaId, new Kuyumcu.Api.Modules.Kasa.GunKapanisiDto(), k.Islem));
        Assert.Equal("sayilanNakit", AlanIsareti.Alan(ex));
        Assert.Equal(0, KasaDurum(k).Kapanis);
    }

    [Fact]
    public async Task P5_gun_kapat_sayilanNakit_DEGERLE_200_kapanis_yazilir_fark_sifir()
    {
        var k = Kur("gun kapat olumlu");
        await Tamam(await Ham(f.Ortam.Istemci(k), HttpMethod.Post,
            $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", """{"sayilanNakit":250000}"""));
        using var db = f.Vt.OlusturSistem();
        var g = db.GunKapanislari.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(250_000m, g.SayilanNakit);
        Assert.Equal(0m, g.Fark);
    }

    /// <summary>
    /// Günü yeniden açma gerekçesi ≥ 20 karakter — KIRPILMIŞ metne. DTO MinLength(20) boşlukları
    /// da sayıyordu: "a" + 19 boşluk geçip kayda "a" yazılıyordu.
    /// </summary>
    [Fact]
    public async Task P5_gun_ac_gerekce_BOSLUKLA_doldurulmus_400_alan_gerekce_gun_KAPALI_kalir()
    {
        var k = Kur("gun ac gerekce");
        var c = f.Ortam.Istemci(k);
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", """{"sayilanNakit":250000}"""));
        string gun;
        using (var db0 = f.Vt.OlusturSistem())
            gun = db0.GunKapanislari.AsNoTracking().Single(x => x.TenantId == k.TenantId).YerelGun.ToString("yyyy-MM-dd");
        await Alan400(await c.PostJson($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi/{gun}/ac",
            new { gerekce = "a" + new string(' ', 19) }), "gerekce");
        using var db = f.Vt.OlusturSistem();
        var g = db.GunKapanislari.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Null(g.AcilisZamani);
        Assert.Null(g.AcilisGerekcesi);
    }

    // ═══════════ KASA — elle hareket, oluştur, PATCH ═══════════

    [Theory]
    [InlineData("komisyonOran", """{"yon":"GIRIS","tutar":100,"odemeTuru":"KART","komisyonOran":0.12345}""")]
    [InlineData("komisyonOran", """{"yon":"GIRIS","tutar":100,"odemeTuru":"KART","komisyonOran":1}""")]
    [InlineData("komisyonOran", """{"yon":"GIRIS","tutar":0.01,"odemeTuru":"KART","komisyonOran":0.6}""")]
    [InlineData("tutar", """{"yon":"GIRIS","tutar":1e14,"odemeTuru":"NAKIT"}""")]
    [InlineData("tutar", """{"yon":"GIRIS","tutar":9999999999999,"odemeTuru":"NAKIT"}""")]
    [InlineData("aciklama", """{"yon":"GIRIS","tutar":10,"odemeTuru":"NAKIT","aciklama":"__501__"}""")]
    public async Task P5_kasa_elle_hareket_girdi_kapisi_400_ALANLI_hareket_YAZILMAZ_bakiye_DEGISMEZ(string alan, string json)
    {
        var k = Kur("kasa hareket " + alan);
        var once = KasaDurum(k);
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post,
            $"/api/v1/kasalar/{k.KasaId}/hareket", json.Replace("__501__", new string('a', 501))), alan);
        Assert.Equal(once, KasaDurum(k));
    }

    [Fact]
    public async Task P5_kasa_elle_hareket_SERVIS_yolunda_0_004_tutar_400_alan_tutar_satir_YOK()
    {
        var k = Kur("kasa hareket servis");
        var once = KasaDurum(k);
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Kasa.KasaServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.HareketGir(k.KasaId,
            new Kuyumcu.Api.Modules.Kasa.KasaHareketDto { Yon = "GIRIS", Tutar = 0.004m, OdemeTuru = "NAKIT" }, k.Islem));
        Assert.Equal("tutar", AlanIsareti.Alan(ex));
        Assert.Equal(once, KasaDurum(k));
    }

    [Fact]
    public async Task P5_kasa_PATCH_paraBirimi_BOS_ve_GECERSIZ_400_alan_SQL_DEGISMEZ_null_KORUR()
    {
        var k = Kur("kasa patch para");
        var c = f.Ortam.Istemci(k);
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/kasalar/{k.KasaId}", """{"paraBirimi":""}"""), "paraBirimi");
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/kasalar/{k.KasaId}", """{"ad":"  "}"""), "ad");
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/kasalar/{k.KasaId}", "{\"ad\":\"" + new string('k', 101) + "\"}"), "ad");
        await Tamam(await Ham(c, HttpMethod.Patch, $"/api/v1/kasalar/{k.KasaId}", """{"ad":null,"paraBirimi":null}"""));
        using var db = f.Vt.OlusturSistem();
        var kasa = db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId);
        Assert.Equal("TRY", kasa.ParaBirimi);
        Assert.Equal("Kasa", kasa.Ad);
    }

    [Fact]
    public async Task P5_kasa_PATCH_servis_yolunda_paraBirimi_BOS_400_gecmisi_bos_kasada_da_YAZILMAZ()
    {
        var k = Kur("kasa patch servis");
        using (var db0 = f.Vt.OlusturSistem())
        {
            var kasa0 = db0.Kasalar.Single(x => x.Id == k.KasaId);
            kasa0.Bakiye = 0m;   // geçmişi ve bakiyesi boş kasa: para birimi kilidi devrede DEĞİL
            db0.SaveChanges();
        }
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Kasa.KasaServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.Guncelle(k.KasaId,
            new Kuyumcu.Api.Modules.Kasa.KasaGuncelleDto { ParaBirimi = "" }, k.Islem));
        Assert.Equal("paraBirimi", AlanIsareti.Alan(ex));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal("TRY", db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).ParaBirimi);
    }

    [Fact]
    public async Task P5_kasa_OLUSTUR_ad_101_ve_paraBirimi_XYZ_400_alan_kasa_ACILMAZ_bos_para_birimi_TRY()
    {
        var k = Kur("kasa olustur");
        var c = f.Ortam.Istemci(k);
        int Say() { using var db = f.Vt.OlusturSistem(); return db.Kasalar.AsNoTracking().Count(x => x.TenantId == k.TenantId); }
        var once = Say();
        await Alan400(await c.PostJson("/api/v1/kasalar", new { ad = new string('k', 101), subeId = k.SubeId }), "ad");
        await Alan400(await c.PostJson("/api/v1/kasalar", new { ad = "P5 XYZ", subeId = k.SubeId, paraBirimi = "XYZ" }), "paraBirimi");
        Assert.Equal(once, Say());
        await Tamam(await c.PostJson("/api/v1/kasalar", new { ad = "P5 Bos PB", subeId = k.SubeId, paraBirimi = "" }));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal("TRY", db.Kasalar.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Ad == "P5 Bos PB").ParaBirimi);
    }

    // ═══════════ VİRMAN ═══════════

    private string IkinciKasa(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kimlik.Ulid();
        db.Kasalar.Add(new Kasa
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Ad = new string('H', 100),
            ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });
        db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task P5_virman_kasa_bacagi_aciklama_ADLARLA_500_asarsa_400_alan_aciklama_500_DEGIL_bacak_YOK()
    {
        var k = Kur("virman aciklama");
        var hedef = IkinciKasa(k);
        var once = KasaDurum(k);
        await Alan400(await f.Ortam.Istemci(k).PostJson("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "KASA", hedefId = hedef,
            tutar = 10m, aciklama = new string('v', 500),
        }), "aciklama");
        Assert.Equal(once, KasaDurum(k));
        Assert.Equal(0m, KasaDurum(k, hedef).Bakiye);
    }

    [Fact]
    public async Task P5_virman_SERVIS_yolunda_0_004_tutar_400_alan_tutar_bacak_YOK()
    {
        var k = Kur("virman servis");
        var hedef = IkinciKasa(k);
        var once = KasaDurum(k);
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Gider.VirmanServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.Olustur(new Kuyumcu.Api.Modules.Gider.VirmanDto
        {
            KaynakTur = "KASA", KaynakId = k.KasaId, HedefTur = "KASA", HedefId = hedef, Tutar = 0.004m,
        }, k.Islem));
        Assert.Equal("tutar", AlanIsareti.Alan(ex));
        Assert.Equal(once, KasaDurum(k));
    }

    // ═══════════ GİDER ═══════════

    private async Task<string> GiderKategorisi(Kiraci k, HttpClient c)
    {
        await Tamam(await c.PostJson("/api/v1/gider-kategorileri", new { ad = "P5 Kira" }));
        using var db = f.Vt.OlusturSistem();
        return db.GiderKategorileri.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Ad == "P5 Kira").Id;
    }

    [Fact]
    public async Task P5_gider_SERVIS_yolunda_0_004_tutar_400_alan_tutar_satir_YOK()
    {
        var k = Kur("gider servis");
        var kat = await GiderKategorisi(k, f.Ortam.Istemci(k));
        var once = KasaDurum(k);
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Gider.GiderServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.Kaydet(new Kuyumcu.Api.Modules.Gider.GiderKaydetDto
        {
            KasaId = k.KasaId, GiderKategoriId = kat, Tutar = 0.004m,
        }, k.Islem));
        Assert.Equal("tutar", AlanIsareti.Alan(ex));
        Assert.Equal(once, KasaDurum(k));
    }

    [Fact]
    public async Task P5_gider_kategori_PATCH_ad_null_ve_aktif_null_KORUR_bos_ad_400_alan_ad()
    {
        var k = Kur("gider kategori patch");
        var c = f.Ortam.Istemci(k);
        var kat = await GiderKategorisi(k, c);
        await Tamam(await Ham(c, HttpMethod.Patch, $"/api/v1/gider-kategorileri/{kat}", """{"ad":null,"aktif":null,"aciklama":"not"}"""));
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/gider-kategorileri/{kat}", """{"ad":" "}"""), "ad");
        using var db = f.Vt.OlusturSistem();
        var g = db.GiderKategorileri.AsNoTracking().Single(x => x.Id == kat);
        Assert.Equal("P5 Kira", g.Ad);
        Assert.True(g.Aktif);
        Assert.Equal("not", g.Aciklama);
    }

    // ═══════════ CARİ ═══════════

    private (decimal Tl, decimal Has, int Defter, int KasaSatir) CariDurum(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var b = db.CariBakiyeler.AsNoTracking().Single(x => x.CariId == k.CariId);
        return (b.TlBakiye, b.HasGramBakiye,
                db.Hareketler.AsNoTracking().Count(x => x.TenantId == k.TenantId),
                db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    [Theory]
    [InlineData("tlTutari", """{"tlTutari":0.001,"hasGrami":0,"odemeTuru":"HAVALE","kasaId":"__KASA__","kurSnapshotId":"__KUR__"}""")]
    [InlineData("tlTutari", """{"tlTutari":0.004,"odemeTuru":"HAVALE","kasaId":"__KASA__","kurSnapshotId":"__KUR__"}""")]
    // SON-KALANLAR: kullanıcı ALTIN yazdıysa red altın alanına bağlanır (eskiden tlTutari; ileti "en az bir tutar" çelişiyordu).
    [InlineData("hasGrami", """{"hasGrami":0.0000004,"kurSnapshotId":"__KUR__"}""")]
    [InlineData("hasGrami", """{"hasGrami":1e10,"kurSnapshotId":"__KUR__"}""")]
    public async Task P5_cari_tahsilat_ONCE_yuvarla_sonra_sifir_400_ALANLI_hareket_YAZILMAZ(string alan, string json)
    {
        var k = Kur("tahsilat " + alan);
        var once = CariDurum(k);
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/cari/{k.CariId}/tahsilat",
            json.Replace("__KASA__", k.KasaId).Replace("__KUR__", k.KurId)), alan);
        Assert.Equal(once, CariDurum(k));
    }

    [Theory]
    [InlineData("tlDelta", """{"tlDelta":0.001,"hasDelta":0}""")]
    [InlineData("tlDelta", """{"tlDelta":-0.004,"hasDelta":0.0000001}""")]
    [InlineData("tlDelta", """{"tlDelta":1e14,"hasDelta":0}""")]
    [InlineData("hasDelta", """{"tlDelta":0,"hasDelta":1e10}""")]
    [InlineData("dovizDelta", """{"tlDelta":0,"hasDelta":0,"dovizParaBirimi":"USD","dovizDelta":0.004}""")]
    public async Task P5_cari_duzeltme_ONCE_yuvarla_sonra_sifir_ve_tavan_400_ALANLI_fis_YAZILMAZ(string alan, string fark)
    {
        var k = Kur("duzeltme " + alan);
        var once = CariDurum(k);
        var json = fark.TrimEnd('}') + ",\"gerekce\":\"PATCH-5 sınır denemesi gerekçesi uzun\",\"kurSnapshotId\":\"" + k.KurId + "\"}";
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/cari/{k.CariId}/duzeltme", json), alan);
        Assert.Equal(once, CariDurum(k));
    }

    [Fact]
    public async Task P5_cari_veresiye_SERVIS_yolunda_0_004_TL_400_alan_tlTutari_hareket_YOK()
    {
        var k = Kur("veresiye servis");
        var once = CariDurum(k);
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Cari.CariServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.VeresiyeGir(k.CariId,
            new Kuyumcu.Api.Modules.Cari.VeresiyeGirisDto { TlTutari = 0.004m, KurSnapshotId = k.KurId }, k.Islem));
        Assert.Equal("tlTutari", AlanIsareti.Alan(ex));
        Assert.Equal(once, CariDurum(k));
    }

    [Fact]
    public async Task P5_cari_doviz_veresiye_SERVIS_yolunda_0_004_400_alan_tutar_hareket_YOK()
    {
        var k = Kur("doviz veresiye servis");
        using (var db0 = f.Vt.OlusturSistem()) TestKurulum.KurDoviz(db0, k.KurId);
        var once = CariDurum(k);
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Cari.CariServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.DovizVeresiyeGir(k.CariId,
            new Kuyumcu.Api.Modules.Cari.DovizVeresiyeDto { ParaBirimi = "USD", Tutar = 0.004m, KurSnapshotId = k.KurId }, k.Islem));
        Assert.Equal("tutar", AlanIsareti.Alan(ex));
        Assert.Equal(once, CariDurum(k));
    }

    [Fact]
    public async Task P5_cari_PATCH_bos_ad_ve_bos_tur_400_alan_null_KORUR_iskonto_3_ondalik_400_SQL_DEGISMEZ()
    {
        var k = Kur("cari patch");
        var c = f.Ortam.Istemci(k);
        (string Ad, string Tur, decimal? Isk) Durum()
        {
            using var db = f.Vt.OlusturSistem();
            var x = db.Cariler.AsNoTracking().Single(y => y.Id == k.CariId);
            return (x.Ad, x.Tur, x.IskontoYuzde);
        }
        var once = Durum();
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", """{"ad":""}"""), "ad");
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", """{"tur":""}"""), "tur");
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", """{"iskontoYuzde":12.345}"""), "iskontoYuzde");
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", """{"krediLimitiTl":1e14}"""), "krediLimitiTl");
        Assert.Equal(once, Durum());
        await Tamam(await Ham(c, HttpMethod.Patch, $"/api/v1/cari/{k.CariId}", """{"ad":null,"tur":null,"iskontoYuzde":12.34}"""));
        Assert.Equal((once.Ad, once.Tur, (decimal?)12.34m), Durum());
    }

    // ═══════════ BANKA ═══════════

    private async Task<string> BankaHesabi(Kiraci k, HttpClient c)
    {
        var iban = TestIban.Tamamla(string.Concat(Enumerable.Range(0, 22).Select(i => (char)('0' + (k.TenantId[i % k.TenantId.Length] % 10)))));
        await Tamam(await c.PostJson("/api/v1/bankalar", new { bankaAdi = "P5 Banka", hesapAdi = "P5 Vadesiz", iban, paraBirimi = "" }));
        using var db = f.Vt.OlusturSistem();
        return db.BankaHesaplari.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;
    }

    private (decimal Bakiye, int Satir, string ParaBirimi, string BankaAdi) BankaDurum(Kiraci k, string hesapId)
    {
        using var db = f.Vt.OlusturSistem();
        var h = db.BankaHesaplari.AsNoTracking().Single(x => x.Id == hesapId);
        return (h.Bakiye, db.BankaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId), h.ParaBirimi, h.BankaAdi);
    }

    [Theory]
    [InlineData("tarih", """{"yon":"GELEN","tutar":100,"tarih":"dün"}""")]
    [InlineData("karsiTarafAd", """{"yon":"GELEN","tutar":100,"karsiTarafAd":"__151__"}""")]
    [InlineData("tutar", """{"yon":"GELEN","tutar":1e14}""")]
    public async Task P5_banka_hareket_girdi_kapisi_400_ALANLI_500_DEGIL_dekont_YAZILMAZ(string alan, string json)
    {
        var k = Kur("banka hareket " + alan);
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaHesabi(k, c);
        var once = BankaDurum(k, hesap);
        Assert.Equal("TRY", once.ParaBirimi);   // POST'ta "" para birimi → varsayılan TRY ("" YAZILMAZ)
        await Alan400(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{hesap}/hareket",
            json.Replace("__151__", new string('b', 151))), alan);
        Assert.Equal(once, BankaDurum(k, hesap));
    }

    [Fact]
    public async Task P5_banka_hareket_SERVIS_yolunda_0_004_tutar_400_alan_tutar_dekont_YOK()
    {
        var k = Kur("banka servis");
        var hesap = await BankaHesabi(k, f.Ortam.Istemci(k));
        var once = BankaDurum(k, hesap);
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Banka.BankaServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.HareketEkleIc(hesap,
            new Kuyumcu.Api.Modules.Banka.HareketEkleDto { Yon = "GELEN", Tutar = 0.004m }, k.Islem));
        Assert.Equal("tutar", AlanIsareti.Alan(ex));
        Assert.Equal(once, BankaDurum(k, hesap));
    }

    /// <summary>
    /// Ekstre onayı HareketEkleIc'e DataAnnotations'sız girer: 151 karakterlik karşı taraf adı
    /// MySQL 1406 → DbUpdateException (ApiException değil) idi; satır bazlı hata yakalanmaz,
    /// toplu onay 500 olurdu. Servis kapısı 400 alan:karsiTarafAd, dekont yazılmaz.
    /// </summary>
    [Fact]
    public async Task P5_banka_hareket_SERVIS_ekstre_yolu_karsiTarafAd_151_400_alan_dekont_YOK()
    {
        var k = Kur("banka servis karsi");
        var hesap = await BankaHesabi(k, f.Ortam.Istemci(k));
        var once = BankaDurum(k, hesap);
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Banka.BankaServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.HareketEkleIc(hesap,
            new Kuyumcu.Api.Modules.Banka.HareketEkleDto { Yon = "GELEN", Tutar = 10m, KarsiTarafAd = new string('b', 151) }, k.Islem));
        Assert.Equal("karsiTarafAd", AlanIsareti.Alan(ex));
        Assert.Equal(once, BankaDurum(k, hesap));
    }

    [Fact]
    public async Task P5_banka_faturaya_donustur_kdvOran_4ten_fazla_ondalik_400_alan_kdvOran_fatura_YOK()
    {
        var k = Kur("banka fatura kdv");
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaHesabi(k, c);
        await Tamam(await c.PostJson($"/api/v1/bankalar/{hesap}/hareket", new { yon = "GELEN", tutar = 120m, cariId = k.CariId }));
        string hareketId;
        using (var db0 = f.Vt.OlusturSistem())
            hareketId = db0.BankaHareketleri.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;
        await Alan400(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/hareketler/{hareketId}/faturaya-donustur",
            """{"kdvOran":0.12345}"""), "kdvOran");
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.Faturalar.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task P5_banka_PATCH_paraBirimi_BOS_iban_BOS_banka_adi_BOSLUK_400_alan_SQL_DEGISMEZ()
    {
        var k = Kur("banka patch");
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaHesabi(k, c);
        var once = BankaDurum(k, hesap);
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{hesap}", """{"paraBirimi":""}"""), "paraBirimi");
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{hesap}", """{"iban":""}"""), "iban");
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{hesap}", """{"bankaAdi":"   "}"""), "bankaAdi");
        Assert.Equal(once, BankaDurum(k, hesap));
    }

    // ═══════════ POS ═══════════

    [Fact]
    public async Task P5_pos_eslestir_ONCE_yuvarla_0_004_400_alan_satir_tutari_esleme_YAZILMAZ()
    {
        var k = Kur("pos eslestir");
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Pos.PosServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.Eslestir(new Kuyumcu.Api.Modules.Pos.PosEslestirDto
        {
            BankaHareketId = Kimlik.Ulid(),
            Satirlar = [new() { PosTahsilatId = Kimlik.Ulid(), Tutar = 0.004m }],
        }, k.Islem));
        Assert.Equal("satirlar[0].tutar", AlanIsareti.Alan(ex));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.PosYatmaEslesmeleri.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }
}

/// <summary>PATCH-5 — KolonTavani girdi yardımcıları SAF sınır tablosu.</summary>
public sealed class PatchBesKolonTavaniTestleri
{
    [Theory]
    [InlineData("0.004", false, "0")]
    [InlineData("0.005", true, "0.01")]
    [InlineData("0.01", true, "0.01")]
    [InlineData("-1", false, "0")]
    [InlineData("9999999999999.99", true, "9999999999999.99")]
    [InlineData("9999999999999.995", false, "0")]
    public void P5_PozitifTutar_once_yuvarla_sonra_sifir_ve_tavan(string deger, bool gecer, string beklenen)
    {
        var d = decimal.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
        if (gecer)
            Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture),
                Kuyumcu.Api.Modules.KolonTavani.PozitifTutar(d, "tutar", "Tutar"));
        else
            Assert.Equal("tutar", AlanIsareti.Alan(Assert.Throws<BadRequestException>(
                () => Kuyumcu.Api.Modules.KolonTavani.PozitifTutar(d, "tutar", "Tutar"))));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("TRY", true)]
    [InlineData("GBP", true)]
    [InlineData("", false)]
    [InlineData("try", false)]
    [InlineData("XAU", false)]
    public void P5_ParaBirimi_bos_dize_ve_liste_disi_400(string? deger, bool gecer)
    {
        if (gecer) Assert.Equal(deger, Kuyumcu.Api.Modules.KolonTavani.ParaBirimi(deger, "paraBirimi"));
        else Assert.Equal("paraBirimi", AlanIsareti.Alan(Assert.Throws<BadRequestException>(
            () => Kuyumcu.Api.Modules.KolonTavani.ParaBirimi(deger, "paraBirimi"))));
    }
}
