using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO-2 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT LİTERALDİR (üretim
// kodundan hesaplanmaz). Paket kasanın NAKİT payını (bakiye − Σkart − Σhavale)
// çıkış kapısına bağladı ve gün sonu tahsilat tanımını değiştirdi; bu dosya o
// yolların sınırlarını sınar: sıfır · eksi · kuruş yuvarlaması (AwayFromZero,
// 0,005) · has/gram 0,0001 · çok büyük tutar · null araç · Türkçe giriş ·
// kapsam boşlukları (gider nakit çıkışı, elle DIGER girişi).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PARADEMO2SinirTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "PAKETPARA-DEMO-2-TEST " + ad), 2_000m);
    }

    private static async Task<string> KasaAc(HttpClient c, Kiraci k, string ad = "DOGR kasa")
    {
        var r = await c.PostJson("/api/v1/kasalar", new { ad, subeId = k.SubeId, paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Json()).GetProperty("id").GetString()!;
    }

    private static Task<HttpResponseMessage> Hareket(HttpClient c, string kasa, string yon, decimal tutar, string? arac) =>
        c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new { yon, tutar, odemeTuru = arac, aciklama = "dogrulayici" });

    private static async Task Giris(HttpClient c, string kasa, string arac, decimal tutar)
    {
        var r = await Hareket(c, kasa, "GIRIS", tutar, arac);
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    private async Task<decimal> Bakiye(string kasa)
    {
        using var db = f.Vt.OlusturSistem();
        return (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasa)).Bakiye;
    }

    private async Task<string> Nakit1000Kart500(HttpClient c, Kiraci k)
    {
        var kasa = await KasaAc(c, k);
        await Giris(c, kasa, "NAKIT", 1_000m);
        await Giris(c, kasa, "KART", 500m);
        return kasa;
    }

    // ── sıfır ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task S1_sifir_tutarli_nakit_cikis_400_bakiye_degismez()
    {
        var k = YeniKiraci("sifir");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", 0m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    // ── eksi ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task S2_eksi_tutarli_cikis_400_kapi_isaret_cevirip_girise_donusmez()
    {
        var k = YeniKiraci("eksi");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", -2_000m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    // ── kuruş: PAKET GOC-A (K9 · envanter ortak kök 3) — 2'den fazla ondalık SESSİZCE YUVARLANMAZ ──
    // Eski sözleşme (1.000,005 → 1.000,01 yuvarlanıp nakit payını aşar / 1.000,004 → 1.000,00 geçer) kalktı:
    // kuruş altı hane artık 400 + sözlük metni (ekranın alan altı metniyle AYNI) ve bakiye DEĞİŞMEZ.
    // Nakit payı sınırının iki yanı tam kuruşla aynen sınanır (asıl iddia korunur).
    private static string OndalikMetni => Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(
        new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiOndalik, 2), "tr");

    [Fact]
    public async Task S3_kurus_1000_005_yuvarlanmaz_400_ondalik_1000_01_nakit_payini_asar_400()
    {
        var k = YeniKiraci("kurus asar");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", 1_000.005m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains(OndalikMetni, await r.Content.ReadAsStringAsync());
        Assert.Equal(1_500m, await Bakiye(kasa));
        var r2 = await Hareket(c, kasa, "CIKIS", 1_000.01m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        Assert.Contains("nakit yetersiz", await r2.Content.ReadAsStringAsync());
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    [Fact]
    public async Task S4_kurus_1000_004_yuvarlanmaz_400_tam_nakit_1000_00_200_bakiye_500()
    {
        var k = YeniKiraci("kurus icinde");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", 1_000.004m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains(OndalikMetni, await r.Content.ReadAsStringAsync());
        Assert.Equal(1_500m, await Bakiye(kasa));
        var r2 = await Hareket(c, kasa, "CIKIS", 1_000.00m, "NAKIT");
        Assert.True(r2.IsSuccessStatusCode, await r2.Content.ReadAsStringAsync());
        Assert.Equal(500m, await Bakiye(kasa));
    }

    // ── has / gram: 0,0001 g alan sınırının altında (alan mesajı), 0,001 g küçük nakit çıkışı kapıdan geçer ───
    [Fact]
    public async Task S5a_hurda_0_0001_gram_alan_mesajiyla_400_kasa_degismez()
    {
        var k = YeniKiraci("gram4");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await c.PostAsJsonAsync("/api/v1/islemler/hurda-alis", new
        {
            cariId = k.CariId, ayarId = "K22", gramAgirlik = 0.0001m, birimFiyatTlHasGram = 1_200m,
            kasaId = kasa, kurSnapshotId = k.KurId, odemeTuru = "NAKIT",
            satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Hurda Satıcısı", tckn = "10000000146", adres = "Ulus" },
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("gramAgirlik", await r.Content.ReadAsStringAsync());
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    [Fact]
    public async Task S5_hurda_0_001_gram_kucuk_nakit_cikisi_kapidan_gecer()
    {
        var k = YeniKiraci("gram");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await c.PostAsJsonAsync("/api/v1/islemler/hurda-alis", new
        {
            cariId = k.CariId, ayarId = "K22", gramAgirlik = 0.001m, birimFiyatTlHasGram = 1_200m,
            kasaId = kasa, kurSnapshotId = k.KurId, odemeTuru = "NAKIT",
            satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Hurda Satıcısı", tckn = "10000000146", adres = "Ulus" },
        });
        var govde = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("nakit yetersiz", govde);
        Assert.True(r.IsSuccessStatusCode, govde);
        // 0,001 g × 0,916 × 1.200 = 1,0992 → 1,10 ₺ (AwayFromZero)
        Assert.Equal(1_498.90m, await Bakiye(kasa));
    }

    // ── çok büyük tutar ──────────────────────────────────────────────────
    [Fact]
    public async Task S6_cok_buyuk_999_999_999_99_tam_nakit_cikar_bir_kurus_fazlasi_400()
    {
        var k = YeniKiraci("buyuk");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await KasaAc(c, k);
        await Giris(c, kasa, "NAKIT", 999_999_999.99m);
        await Giris(c, kasa, "HAVALE", 500m);
        var fazla = await Hareket(c, kasa, "CIKIS", 1_000_000_000.00m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, fazla.StatusCode);
        var tam = await Hareket(c, kasa, "CIKIS", 999_999_999.99m, "NAKIT");
        Assert.True(tam.IsSuccessStatusCode, await tam.Content.ReadAsStringAsync());
        Assert.Equal(500m, await Bakiye(kasa));
    }

    // ── boş/null araç: sunucu reddeder (nakit kapısı sessizce atlanmaz) ──
    [Fact]
    public async Task S7_null_odeme_turu_400_bakiye_degismez()
    {
        var k = YeniKiraci("null");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", 1_200m, null);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    // ── bilinmeyen/nakit sayılan araç (ALTIN) fail-closed: nakit kapısına tabi ──
    [Fact]
    public async Task S8_ALTIN_etiketli_cikis_nakit_sayilir_1200_400()
    {
        var k = YeniKiraci("altin arac");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", 1_200m, "ALTIN");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    // ── Türkçe giriş (FormAlani.SayiCoz) — Günü Kapat sayım kutusunun ayrıştırıcısı ──
    [Fact]
    public void S9_turkce_giris_45_000_ve_1_5_dogru_belirsiz_1_500_reddedilir()
    {
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Null(FormAlani.SayiCoz(null).Deger);
    }

    // ── kapsam boşluğu adayı: gider NAKİT ödemesi de çekmeceden çıkar ──
    [Fact]
    public async Task S10_gider_nakit_odemesi_nakit_1000_kart_500_iken_1200_reddedilmeli()
    {
        var k = YeniKiraci("gider");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var kat = await c.PostJson("/api/v1/gider-kategorileri", new { ad = "Kira" });
        Assert.True(kat.IsSuccessStatusCode, await kat.Content.ReadAsStringAsync());
        var katId = (await kat.Json()).GetProperty("id").GetString()!;
        var r = await c.PostJson("/api/v1/giderler", new { kasaId = kasa, giderKategoriId = katId, tutar = 1_200m });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
    }

    // ── tanım sınırı: elle "DIGER" girişi açılış devri DEĞİLDİR ──
    [Fact]
    public async Task S11_elle_DIGER_girisi_gun_sonunda_acilis_devri_diye_gorunmemeli()
    {
        var k = YeniKiraci("diger");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await KasaAc(c, k);
        await Giris(c, kasa, "DIGER", 250m);
        var gun = Zaman.YerelBugun().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var t = (await (await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}")).Json()).GetProperty("tahsilat");
        Assert.Equal(0m, t.GetProperty("acilisDevri").GetDecimal());
    }

    // ═════════════════════════════════════════════════════════════════════
    // DOĞRULAYICI TUR 5 (02.10 öğle) — ek sınırlar. Beklenenler SABİT literal.
    // ═════════════════════════════════════════════════════════════════════

    private static Task<HttpResponseMessage> Virman(HttpClient c, string kaynak, string hedef, decimal tutar, string? arac) =>
        c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kaynak, hedefTur = "KASA", hedefId = hedef,
            tutar, aciklama = "dogrulayici", kasaAraci = arac,
        });

    // ── gider: tam nakit payı sınırı DAHİL (1000,00 geçer), 1 kuruş fazlası 400 ──
    [Fact]
    public async Task S12_gider_tam_nakit_1000_gecer_1000_01_400()
    {
        var k = YeniKiraci("gider sinir");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var kat = await c.PostJson("/api/v1/gider-kategorileri", new { ad = "Kira" });
        var katId = (await kat.Json()).GetProperty("id").GetString()!;
        var fazla = await c.PostJson("/api/v1/giderler", new { kasaId = kasa, giderKategoriId = katId, tutar = 1_000.01m });
        Assert.Equal(HttpStatusCode.BadRequest, fazla.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
        var tam = await c.PostJson("/api/v1/giderler", new { kasaId = kasa, giderKategoriId = katId, tutar = 1_000m });
        Assert.True(tam.IsSuccessStatusCode, await tam.Content.ReadAsStringAsync());
        Assert.Equal(500m, await Bakiye(kasa));
    }

    // ── NAKİT virman (kasa→kasa): nakit 1000 + kart 500 iken 1200 → 400, iki kasa da değişmez ──
    [Fact]
    public async Task S13_nakit_virman_1200_nakit_payini_asar_400()
    {
        var k = YeniKiraci("virman nakit");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var hedef = await KasaAc(c, k, "DOGR hedef");
        var r = await Virman(c, kasa, hedef, 1_200m, null);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
        Assert.Equal(0m, await Bakiye(hedef));
    }

    // ── KART virman payı tam 500 geçer; ardından nakit 1000 çıkışı da geçer (kart payı tüketildi, nakit bozulmadı) ──
    [Fact]
    public async Task S14_kart_virman_tam_pay_sonra_nakit_1000_cikis_bakiye_sifir()
    {
        var k = YeniKiraci("virman kart");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var hedef = await KasaAc(c, k, "DOGR hedef");
        var fazla = await Virman(c, kasa, hedef, 500.01m, "KART");
        Assert.Equal(HttpStatusCode.BadRequest, fazla.StatusCode);
        var r = await Virman(c, kasa, hedef, 500m, "KART");
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var cik = await Hareket(c, kasa, "CIKIS", 1_000m, "NAKIT");
        Assert.True(cik.IsSuccessStatusCode, await cik.Content.ReadAsStringAsync());
        Assert.Equal(0m, await Bakiye(kasa));
        // Hedef kasaya giren KART payı orada nakit SAYILMAZ: 1 TL nakit çıkışı bile 400.
        var hedefNakit = await Hareket(c, hedef, "CIKIS", 1m, "NAKIT");
        Assert.Equal(HttpStatusCode.BadRequest, hedefNakit.StatusCode);
        Assert.Equal(500m, await Bakiye(hedef));
    }

    // ── gün sonu tanımı: virman girişi TOPLAM TAHSİLAT'a girmez, ayrı satırda döner ──
    [Fact]
    public async Task S15_virman_girisi_toplam_tahsilata_girmez_ayri_satirda()
    {
        var k = YeniKiraci("tanim virman");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kaynak = await KasaAc(c, k);
        var hedef = await KasaAc(c, k, "DOGR hedef");
        await Giris(c, kaynak, "NAKIT", 1_000m);
        var v = await Virman(c, kaynak, hedef, 300m, null);
        Assert.True(v.IsSuccessStatusCode, await v.Content.ReadAsStringAsync());
        var gun = Zaman.YerelBugun().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var t = (await (await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}")).Json()).GetProperty("tahsilat");
        Assert.Equal(1_000m, t.GetProperty("toplam").GetDecimal());
        Assert.Equal(1_000m, t.GetProperty("nakit").GetDecimal());
        Assert.Equal(300m, t.GetProperty("virmanGiris").GetDecimal());
    }

    // ── kapsam sınırı (ADVERSARİAL): elle KART çıkışı kasadaki kart payını aşarsa fark NAKİT payından
    //    düşmüş olur ama nakit defteri bunu bilmez → nakit 1000 + kart 500 iken 1200 KART çıkışı. ──
    [Fact]
    public async Task S16_elle_kart_cikisi_kart_payini_asamaz_1200_400()
    {
        var k = YeniKiraci("kart asim");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await Hareket(c, kasa, "CIKIS", 1_200m, "KART");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(1_500m, await Bakiye(kasa));
    }
}
