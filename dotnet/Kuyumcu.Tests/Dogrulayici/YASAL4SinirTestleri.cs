using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET YASAL-4 FİNANSAL SINIR TESTLERİ.
// Stopaj ödeme tutarı (sıfır, eksi, 0,005 yuvarlama, çok büyük, boş), dönem ve
// son ödeme günü sınırları (kısa ay / artık yıl), KMTS kod uzunluk/kontrol/ReDoS,
// stopaj ödeme formunun Türkçe sayı girişi (FormAlani.SayiCoz).
// Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YASAL4SafSinirTestleri
{
    private static YasalParametreSatiri Satir(string anahtar, decimal? sayi, string? metin, DateOnly bas) =>
        new("t-" + anahtar, anahtar, sayi, metin, "X", bas, null, "test", "https://ornek.test", "TEYITSIZ", "test");

    private static YasalParametreServisi Yasal(params YasalParametreSatiri[] s) =>
        YasalParametreServisi.Yukleyicili(() => s);

    [Fact]
    public void SINIR_stopaj_son_gunu_beyan_gunu_31_iken_subat_son_gunune_kirpilir_artik_yil_29()
    {
        var y = Yasal(Satir(YasalAnahtar.StopajBeyanGunu, 31m, null, new DateOnly(2020, 1, 1)));
        var s = new MHurda.StopajServisi(null!, null!, null!, null!, y);
        Assert.Equal(new DateOnly(2027, 2, 28), s.SonGun(2027, 1, new DateOnly(2026, 10, 1)).Gun);
        Assert.Equal(new DateOnly(2028, 2, 29), s.SonGun(2028, 1, new DateOnly(2026, 10, 1)).Gun);
        // Aralık dönemi → izleyen yılın Ocak'ı.
        Assert.Equal(new DateOnly(2027, 1, 31), s.SonGun(2026, 12, new DateOnly(2026, 10, 1)).Gun);
    }

    [Fact]
    public void SINIR_stopaj_son_gunu_sifir_ya_da_eksi_parametrede_gun_UYDURULMAZ()
    {
        var sifir = new MHurda.StopajServisi(null!, null!, null!, null!,
            Yasal(Satir(YasalAnahtar.StopajBeyanGunu, 0m, null, new DateOnly(2020, 1, 1))));
        Assert.Null(sifir.SonGun(2026, 10, new DateOnly(2026, 10, 1)).Gun);
        var eksi = new MHurda.StopajServisi(null!, null!, null!, null!,
            Yasal(Satir(YasalAnahtar.StopajBeyanGunu, -3m, null, new DateOnly(2020, 1, 1))));
        Assert.Null(eksi.SonGun(2026, 10, new DateOnly(2026, 10, 1)).Gun);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-00")]
    [InlineData("2026-13")]
    [InlineData("2026/08")]
    [InlineData("2026-8")]
    [InlineData("+026-08")]
    [InlineData("2026-+8")]
    [InlineData("1999-12")]
    [InlineData("2101-01")]
    [InlineData("٢٠٢٦-08")]
    public void SINIR_stopaj_donem_gecersiz_bicimler_reddedilir(string? donem)
    {
        Assert.False(MHurda.StopajServisi.DonemGecerli(donem, out _, out _));
    }

    [Fact]
    public void SINIR_kmts_kodu_100_karakter_kabul_101_red_kontrol_karakteri_red_ReDoS_desen_fail_closed()
    {
        Assert.Equal(new string('A', 100), KmtsKodu.Dogrula(new string('A', 100), Yasal()));
        Assert.Throws<BadRequestException>(() => KmtsKodu.Dogrula(new string('A', 101), Yasal()));
        Assert.Throws<BadRequestException>(() => KmtsKodu.Dogrula("KM\u00071234", Yasal()));
        // Felaket geri izlemeli desen: 100 ms zaman aşımı → kabul EDİLMEZ (fail-closed).
        Assert.False(KmtsKodu.Uyar(new string('a', 40) + "!", "(a+)+"));
    }

    [Theory]
    [InlineData("45.000", "Tamam", "45000")]
    [InlineData("1,5", "Tamam", "1.5")]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("0,005", "Tamam", "0.005")]
    [InlineData("999.999.999,99", "Tamam", "999999999.99")]
    [InlineData("", "Bos", null)]
    [InlineData("abc", "Gecersiz", null)]
    public void SINIR_stopaj_odeme_formu_Turkce_sayi_girisi(string ham, string sonuc, string? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : decimal.Parse(deger, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }
}

[Collection("http")]
public sealed class YASAL4HttpSinirTestleri(HttpFikstur f)
{
    private async Task<Kiraci> StopajliKiraci(string ad, decimal stopaj)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "PAKETYASAL4-TEST-DOG-" + ad));
        using (var db = f.Vt.Olustur(k))
        {
            await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).HurdaAlis(new MSatis.HurdaAlisDto
            {
                CariId = k.CariId, AyarId = "K22", GramAgirlik = 1m,
                BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
                Satici = TestKurulum.HurdaSatici(SaticiTipi.EsnafMuaf),
            }, k.Islem);
        }
        using (var db = f.Vt.Olustur(k))
        {
            var p = db.GiderPusulalari.Single(g => g.TenantId == k.TenantId);
            p.TeslimTarihi = new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);
            p.StopajTutar = stopaj;
            db.SaveChanges();
        }
        return k;
    }

    private static async Task<JsonElement> Donem(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/v1/stopaj/donemler?yil=2026"))
            .GetProperty("donemler").EnumerateArray().Single(x => x.GetProperty("donem").GetString() == "2026-08");

    private static Task<HttpResponseMessage> Ode(HttpClient c, object govde) =>
        c.PostAsJsonAsync("/api/v1/stopaj/odemeler", govde);

    [Fact]
    public async Task SINIR_stopaj_odeme_sifir_eksi_kucuk_buyuk_bos_tutar()
    {
        var k = await StopajliKiraci("TUTAR", 100m);
        var c = f.Ortam.Istemci(k);

        // Sıfır ve eksi → 400, kasa satırı yazılmaz.
        Assert.Equal(HttpStatusCode.BadRequest, (await Ode(c, new { donem = "2026-08", kasaId = k.KasaId, tutar = 0m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Ode(c, new { donem = "2026-08", kasaId = k.KasaId, tutar = -5m })).StatusCode);
        // 0,004 → kuruş altı hane (PAKET GOC-A K9: yuvarlanmaz) → reddedilir (sıfır ödeme belgesi doğmaz).
        Assert.Equal(HttpStatusCode.BadRequest, (await Ode(c, new { donem = "2026-08", kasaId = k.KasaId, tutar = 0.004m })).StatusCode);
        // Çok büyük tutar → kalandan büyük → 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await Ode(c, new { donem = "2026-08", kasaId = k.KasaId, tutar = 999_999_999.99m })).StatusCode);
        // Boş kasa / boş dönem → 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await Ode(c, new { donem = "2026-08", tutar = 1m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Ode(c, new { kasaId = k.KasaId, tutar = 1m })).StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0, db.StopajOdemeleri.Count(o => o.TenantId == k.TenantId));

        // PAKET GOC-A (K9): 0,005 YUVARLANMAZ (eski: AwayFromZero 0,01) — 400, satır yok; 0,01 ödenir; kalan 99,99.
        Assert.Equal(HttpStatusCode.BadRequest, (await Ode(c, new { donem = "2026-08", kasaId = k.KasaId, tutar = 0.005m })).StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0, db.StopajOdemeleri.Count(o => o.TenantId == k.TenantId));
        var r = await Ode(c, new { donem = "2026-08", kasaId = k.KasaId, tutar = 0.01m });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(0.01m, (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tutarTl").GetDecimal());
        Assert.Equal(99.99m, (await Donem(c)).GetProperty("kalanTl").GetDecimal());

        // Tutar boş → kalanın TAMAMI (99,99) ödenir; kalan 0.
        var tam = await Ode(c, new { donem = "2026-08", kasaId = k.KasaId });
        Assert.Equal(HttpStatusCode.OK, tam.StatusCode);
        Assert.Equal(99.99m, (await tam.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tutarTl").GetDecimal());
        var d = await Donem(c);
        Assert.Equal(0m, d.GetProperty("kalanTl").GetDecimal());
        Assert.Equal(100m, d.GetProperty("odenenTl").GetDecimal());
    }

    [Fact]
    public async Task SINIR_stopaj_kurus_toplami_ve_kalan_ustu_kurus_alti_400_tam_kalan_kabul()
    {
        var k = await StopajliKiraci("KURUS", 0.01m);
        var c = f.Ortam.Istemci(k);
        Assert.Equal(0.01m, (await Donem(c)).GetProperty("kesilenTl").GetDecimal());
        // PAKET GOC-A (K9): 0,014 YUVARLANMAZ (eski: 0,01'e yuvarlanıp kalanla kabul) — 400; 0,01 (kalan) kabul.
        Assert.Equal(HttpStatusCode.BadRequest, (await Ode(c, new { donem = "2026-08", kasaId = k.KasaId, tutar = 0.014m })).StatusCode);
        var r = await Ode(c, new { donem = "2026-08", kasaId = k.KasaId, tutar = 0.01m });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(0m, (await Donem(c)).GetProperty("kalanTl").GetDecimal());
    }

    [Fact]
    public async Task SINIR_esZamanli_iki_tam_odeme_kesilenin_ustune_cikmamali()
    {
        var k = await StopajliKiraci("YARIS", 500m);
        var g = new { donem = "2026-08", kasaId = k.KasaId };
        // 6 eş zamanlı tam ödeme (çift tık / iki sekme / ağ yeniden denemesi).
        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Ode(f.Ortam.Istemci(k), g)));
        using var db = f.Vt.OlusturSistem();
        var toplam = db.StopajOdemeleri.Where(o => o.TenantId == k.TenantId).Select(o => o.Tutar).ToList().Sum();
        Assert.True(toplam <= 500m, $"Eş zamanlı ödemeler toplamı {toplam} TL > kesilen 500 TL");
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — TUR 2 (01.10): yeniden düzenleme sınırları (son gün tam
// eşik, has/gram 4 hane + çok büyük tutar birebir kopya), yerel ay sınırı (UTC
// gece yarısı kayması), çift geri al, fazla ödenmiş dönemde ödeme, eşzamanlı
// yeniden düzenleme. Beklenen değerler SABİTTİR.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class YASAL4Tur2SinirTestleri(HttpFikstur f)
{
    private static object Satici => new { adSoyad = "Doğru Satıcı", tckn = "10000000146", adres = "Kızılay, Ankara" };

    private async Task<(Kiraci K, string PusulaId)> Pusulali(string ad, Action<GiderPusulasi> duzelt)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "PAKETYASAL4-TEST-DOG2-" + ad));
        using (var db = f.Vt.Olustur(k))
        {
            await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).HurdaAlis(new MSatis.HurdaAlisDto
            {
                CariId = k.CariId, AyarId = "K22", GramAgirlik = 1m,
                BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
                Satici = TestKurulum.HurdaSatici(SaticiTipi.EsnafMuaf),
            }, k.Islem);
        }
        string id;
        using (var db = f.Vt.Olustur(k))
        {
            var p = db.GiderPusulalari.Single(g => g.TenantId == k.TenantId);
            duzelt(p);
            db.SaveChanges();
            id = p.Id;
        }
        return (k, id);
    }

    private static void Iptal(GiderPusulasi p)
    {
        p.IptalEdildi = true;
        p.IptalGerekce = "dogrulayici: satıcı adı yanlış yazılmıştı";
        p.IptalTarihi = DateTime.UtcNow;
    }

    [Fact]
    public async Task SINIR2_yeniden_duzenle_son_gun_TAM_esik_200_bir_gun_sonra_409()
    {
        // Teslim yerel günü = bugün − 7 → son gün = bugün → 200 (o gün dahil).
        var bugunYerel = Zaman.UtcdenYerele(DateTime.UtcNow).Date;
        var teslim7 = Zaman.YerelGunBasiUtc(DateOnly.FromDateTime(bugunYerel.AddDays(-7))).AddHours(12);
        var (k, id) = await Pusulali("ESIK7", p => { p.TeslimTarihi = teslim7; Iptal(p); });
        var r = await f.Ortam.Istemci(k).PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        // Teslim yerel günü = bugün − 8 → son gün dün → 409, yeni pusula yok.
        var teslim8 = Zaman.YerelGunBasiUtc(DateOnly.FromDateTime(bugunYerel.AddDays(-8))).AddHours(12);
        var (k2, id2) = await Pusulali("ESIK8", p => { p.TeslimTarihi = teslim8; Iptal(p); });
        var r2 = await f.Ortam.Istemci(k2).PostAsJsonAsync($"/api/v1/gider-pusulalari/{id2}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1, db.GiderPusulalari.Count(g => g.TenantId == k2.TenantId));
    }

    [Fact]
    public async Task SINIR2_yeniden_duzenle_has_gram_4_hane_ve_cok_buyuk_tutar_BIREBIR_kopyalanir()
    {
        var (k, id) = await Pusulali("HASGRAM", p =>
        {
            p.TeslimTarihi = DateTime.UtcNow.AddDays(-1);
            p.GramAgirlik = 0.001m; p.HasGrami = 0.0001m;   // gider_pusulalari.gram_agirlik decimal(12,3) (önceden var; bkz. rapor DUSUK)
            p.BrutTutar = 999_999_999.99m; p.StopajOran = 0.02m; p.StopajTutar = 19_999_999.99m; p.NetOdenen = 980_000_000.00m;
            Iptal(p);
        });
        var r = await f.Ortam.Istemci(k).PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var yeni = db.GiderPusulalari.Single(g => g.TenantId == k.TenantId && !g.IptalEdildi);
        Assert.Equal(0.001m, yeni.GramAgirlik);
        Assert.Equal(0.0001m, yeni.HasGrami);
        Assert.Equal(999_999_999.99m, yeni.BrutTutar);
        Assert.Equal(19_999_999.99m, yeni.StopajTutar);
        Assert.Equal(980_000_000.00m, yeni.NetOdenen);
        Assert.Equal(0.02m, yeni.StopajOran);
    }

    [Fact]
    public async Task SINIR2_stopaj_donemi_YEREL_ay_UTC_31_Agustos_21_30_Eylul_donemine_duser()
    {
        // 31.08.2026 21:30 UTC = 01.09.2026 00:30 İstanbul → dönem 2026-09 (08 DEĞİL).
        var (k, _) = await Pusulali("YERELAY", p =>
        {
            p.TeslimTarihi = new DateTime(2026, 8, 31, 21, 30, 0, DateTimeKind.Utc);
            p.StopajTutar = 123.45m;
        });
        var y = await f.Ortam.Istemci(k).GetFromJsonAsync<JsonElement>("/api/v1/stopaj/donemler?yil=2026");
        var donemler = y.GetProperty("donemler").EnumerateArray().ToList();
        Assert.Single(donemler);
        Assert.Equal("2026-09", donemler[0].GetProperty("donem").GetString());
        Assert.Equal(123.45m, donemler[0].GetProperty("kesilenTl").GetDecimal());
    }

    [Fact]
    public async Task SINIR2_cift_GERI_AL_ikincisi_reddedilir_kalan_bir_kez_artar()
    {
        var (k, _) = await Pusulali("CIFTGERI", p =>
        {
            p.TeslimTarihi = new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);
            p.StopajTutar = 50m;
        });
        var c = f.Ortam.Istemci(k);
        var o = await c.PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId });
        Assert.Equal(HttpStatusCode.OK, o.StatusCode);
        var odemeId = (await o.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        decimal bakiyeOdemeSonrasi;
        using (var db = f.Vt.OlusturSistem()) bakiyeOdemeSonrasi = db.Kasalar.Single(x => x.Id == k.KasaId).Bakiye;

        var g1 = await c.PostAsJsonAsync($"/api/v1/stopaj/odemeler/{odemeId}/geri-al", new { gerekce = "dogrulayici: yanlış kasa seçildi" });
        Assert.Equal(HttpStatusCode.OK, g1.StatusCode);
        var g2 = await c.PostAsJsonAsync($"/api/v1/stopaj/odemeler/{odemeId}/geri-al", new { gerekce = "dogrulayici: ikinci kez geri al" });
        Assert.True(g2.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest, $"ikinci geri al {(int)g2.StatusCode}");

        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(bakiyeOdemeSonrasi + 50m, db.Kasalar.Single(x => x.Id == k.KasaId).Bakiye);   // TEK iade
        var d = (await c.GetFromJsonAsync<JsonElement>("/api/v1/stopaj/donemler?yil=2026"))
            .GetProperty("donemler").EnumerateArray().Single(x => x.GetProperty("donem").GetString() == "2026-08");
        Assert.Equal(50m, d.GetProperty("kalanTl").GetDecimal());
        Assert.Equal(0m, d.GetProperty("odenenTl").GetDecimal());
    }

    [Fact]
    public async Task SINIR2_fazla_odenmis_EKSI_kalanli_donemde_yeni_odeme_409()
    {
        var (k, id) = await Pusulali("EKSI", p =>
        {
            p.TeslimTarihi = new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);
            p.StopajTutar = 80m;
        });
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK,
            (await c.PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId })).StatusCode);
        using (var db = f.Vt.Olustur(k))
        {
            var p = db.GiderPusulalari.Single(g => g.Id == id);
            Iptal(p);
            db.SaveChanges();
        }
        var d = (await c.GetFromJsonAsync<JsonElement>("/api/v1/stopaj/donemler?yil=2026"))
            .GetProperty("donemler").EnumerateArray().Single(x => x.GetProperty("donem").GetString() == "2026-08");
        Assert.Equal(-80m, d.GetProperty("kalanTl").GetDecimal());
        Assert.False(d.GetProperty("sonGunGecti").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict,
            (await c.PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId, tutar = 1m })).StatusCode);
    }

    [Fact]
    public async Task SINIR2_eszamanli_yeniden_duzenle_ayni_alis_icin_EN_COK_bir_gecerli_pusula()
    {
        var (k, id) = await Pusulali("YARIS", p => { p.TeslimTarihi = DateTime.UtcNow.AddDays(-1); p.StopajTutar = 60m; Iptal(p); });
        var yanitlar = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            f.Ortam.Istemci(k).PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/yeniden-duzenle", Satici)));
        using var db = f.Vt.OlusturSistem();
        var gecerli = db.GiderPusulalari.Count(g => g.TenantId == k.TenantId && !g.IptalEdildi);
        Assert.True(gecerli <= 1, $"aynı alış için {gecerli} geçerli pusula (stopaj {gecerli}× sayılır); durumlar: "
            + string.Join(",", yanitlar.Select(y => (int)y.StatusCode)));
        Assert.Equal(1, gecerli);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — YASAL-4 TUR 3 (01.10): geçerli pusula kilidi
// (UQ tenant_id + gecerli_hareket_id, iptal edilende NULL). Zincir yeniden
// düzenleme, DB düzeyi kilit, stopajın tek sayılması. Beklenenler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class YASAL4Tur3SinirTestleri(HttpFikstur f)
{
    private static object Satici => new { adSoyad = "Zincir Satıcı", tckn = "10000000146", adres = "Ulus, Ankara" };
    private const string Gerekce = "dogrulayici tur3: satıcı adresi eksik yazılmıştı";

    private async Task<(Kiraci K, string PusulaId)> Pusulali(string ad, decimal stopaj, DateTime teslim)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "PAKETYASAL4-TEST-DOG3-" + ad));
        using (var db = f.Vt.Olustur(k))
        {
            await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).HurdaAlis(new MSatis.HurdaAlisDto
            {
                CariId = k.CariId, AyarId = "K22", GramAgirlik = 1m,
                BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
                Satici = TestKurulum.HurdaSatici(SaticiTipi.EsnafMuaf),
            }, k.Islem);
        }
        string id;
        using (var db = f.Vt.Olustur(k))
        {
            var p = db.GiderPusulalari.Single(g => g.TenantId == k.TenantId);
            p.TeslimTarihi = teslim;
            p.StopajTutar = stopaj;
            db.SaveChanges();
            id = p.Id;
        }
        return (k, id);
    }

    [Fact]
    public async Task SINIR3_zincir_iptal_yeniden_iptal_yeniden_stopaj_TEK_sayilir_hareket_sayisi_degismez()
    {
        // Teslim: dün; stopaj 77,77. A iptal → B; B iptal → C. Dönem kesileni 77,77 (3× değil).
        var teslim = DateTime.UtcNow.AddDays(-1);
        var (k, aId) = await Pusulali("ZINCIR", 77.77m, teslim);
        var c = f.Ortam.Istemci(k);
        int hareketOnce;
        using (var db = f.Vt.OlusturSistem()) hareketOnce = db.Hareketler.Count(h => h.TenantId == k.TenantId);

        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/v1/gider-pusulalari/{aId}/iptal", new { gerekce = Gerekce })).StatusCode);
        var rb = await c.PostAsJsonAsync($"/api/v1/gider-pusulalari/{aId}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.OK, rb.StatusCode);
        var bId = (await rb.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
        // Geçerli B varken A'dan ikinci yeniden düzenleme → 409.
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/v1/gider-pusulalari/{aId}/yeniden-duzenle", Satici)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/v1/gider-pusulalari/{bId}/iptal", new { gerekce = Gerekce })).StatusCode);
        var rc = await c.PostAsJsonAsync($"/api/v1/gider-pusulalari/{bId}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.OK, rc.StatusCode);
        var cJson = await rc.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(bId, cJson.GetProperty("duzeltilenId").GetString());
        Assert.Equal(77.77m, cJson.GetProperty("stopajTutar").GetDecimal());

        using (var db = f.Vt.OlusturSistem())
        {
            Assert.Equal(hareketOnce, db.Hareketler.Count(h => h.TenantId == k.TenantId));
            Assert.Equal(3, db.GiderPusulalari.Count(g => g.TenantId == k.TenantId));
            Assert.Equal(1, db.GiderPusulalari.Count(g => g.TenantId == k.TenantId && !g.IptalEdildi));
        }
        var donem = Zaman.UtcdenYerele(teslim).ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        var d = (await c.GetFromJsonAsync<JsonElement>($"/api/v1/stopaj/donemler?yil={donem[..4]}"))
            .GetProperty("donemler").EnumerateArray().Single(x => x.GetProperty("donem").GetString() == donem);
        Assert.Equal(77.77m, d.GetProperty("kesilenTl").GetDecimal());
        Assert.Equal(1, d.GetProperty("pusulaSayisi").GetInt32());

        // Süre takibi: geçerli C var → bekleyen 0.
        var st = await c.GetFromJsonAsync<JsonElement>("/api/v1/gider-pusulalari/sure-takibi");
        Assert.Equal(0, st.GetProperty("yaklasanSayisi").GetInt32());
        Assert.Equal(0, st.GetProperty("gecikmisSayisi").GetInt32());
    }

    [Fact]
    public async Task SINIR3_DB_kilidi_ayni_alisa_ikinci_GECERLI_pusula_reddedilir_iptal_edilmis_kopya_kabul()
    {
        var (k, aId) = await Pusulali("DBKILIT", 10m, DateTime.UtcNow.AddDays(-1));
        GiderPusulasi Kopya(GiderPusulasi a, string no, bool iptal) => new()
        {
            Id = Kimlik.Ulid(), TenantId = a.TenantId, PusulaNo = no, HareketId = a.HareketId, SubeId = a.SubeId,
            CariId = a.CariId, SaticiTipi = a.SaticiTipi, SaticiAdSoyad = a.SaticiAdSoyad, SaticiTckn = a.SaticiTckn,
            SaticiTcknHash = a.SaticiTcknHash, SifreSurumu = a.SifreSurumu, SaticiAdres = a.SaticiAdres, AyarId = a.AyarId,
            GramAgirlik = a.GramAgirlik, HasGrami = a.HasGrami, BrutTutar = a.BrutTutar, StopajOran = a.StopajOran,
            StopajTutar = a.StopajTutar, NetOdenen = a.NetOdenen, TeslimTarihi = a.TeslimTarihi,
            DuzenlemeSonTarihi = a.DuzenlemeSonTarihi, Olusturuldu = DateTime.UtcNow,
            IptalEdildi = iptal, IptalGerekce = iptal ? Gerekce : null, IptalTarihi = iptal ? DateTime.UtcNow : null,
        };
        using (var db = f.Vt.Olustur(k))
        {
            var a = db.GiderPusulalari.Single(g => g.Id == aId);
            db.GiderPusulalari.Add(Kopya(a, "DOG3-GECERLI-2", iptal: false));
            var ex = Assert.ThrowsAny<Microsoft.EntityFrameworkCore.DbUpdateException>(() => db.SaveChanges());
            Assert.Contains("UQ_gider_pusulalari_tenant_gecerli_hareket", ex.InnerException?.Message ?? "", StringComparison.Ordinal);
        }
        using (var db = f.Vt.Olustur(k))
        {
            var a = db.GiderPusulalari.Single(g => g.Id == aId);
            db.GiderPusulalari.Add(Kopya(a, "DOG3-IPTAL-2", iptal: true));
            db.GiderPusulalari.Add(Kopya(a, "DOG3-IPTAL-3", iptal: true));
            db.SaveChanges();   // iptal edilenlerde gecerli_hareket_id NULL → iki kopya kabul
        }
        using (var db = f.Vt.OlusturSistem())
        {
            Assert.Equal(3, db.GiderPusulalari.Count(g => g.TenantId == k.TenantId));
            Assert.Equal(1, db.GiderPusulalari.Count(g => g.TenantId == k.TenantId && !g.IptalEdildi));
            Assert.Equal(1, db.GiderPusulalari.Count(g => g.TenantId == k.TenantId && g.GecerliHareketId != null));
        }
    }
}
