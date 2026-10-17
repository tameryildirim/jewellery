using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Tevsik;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 (TB-15) — BÜYÜK İŞLEMİN TAKSİTİ NAKİT ALINAMAZ
//
// DAYANAK (şartname § 3.1 M-1, birincil): 459 s. VUK GT §4.1.2 — haddi aşan
// işlemde bedel kısım kısım ödenirse "işlemin toplam tutarı dikkate alınacak ve
// her bir tahsilat ve ödeme" aracı finansal kurum kanalıyla yapılır.
//
// FİKSTÜR: veresiye borç ya gerçek satış fişi ucuyla (K-1 çok kalem) ya da deftere
// doğrudan satır + cari bakiye ile kurulur (ŞK-A deseni) — tarihli kaynak (K-5)
// yalnız böyle kurulabilir. Tahsilat HER ZAMAN gerçek HTTP ucundan geçer.
// ⚠ Had testte SABİT DEĞİL: YASAL-1 tablosundan (/tevsik/gunluk) okunur.
// ═════════════════════════════════════════════════════════════════════════════

internal static class TaksitFikstur
{
    /// <summary>Deftere veresiye kaynak satır + cari bakiye (üretimdeki alanlarla).</summary>
    public static string Borc(KuyumcuDbContext db, Kiraci k, decimal tutar, DateTime tarihUtc,
        string tur = "SATIS", string? fisId = null, decimal? veresiye = null, string? cariId = null)
    {
        cariId ??= k.CariId;
        var id = Kimlik.Ulid();
        var isaret = tur is "ALIS" or "HURDA_ALIS" ? -1m : 1m;
        db.Hareketler.Add(new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur,
            Tarih = tarihUtc, TlTutari = isaret * tutar, HasGrami = 0m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, CariId = cariId, KullaniciId = k.KullaniciId,
            FisId = fisId ?? Kimlik.Ulid(),
            OdemeTuru = tur == "ACILIS" ? null : "VERESIYE",
            VeresiyeTutar = tur == "ACILIS" ? null : (veresiye ?? tutar),
            Aciklama = "YASAL-2 fikstür", Olusturuldu = tarihUtc,
        });
        var b = db.CariBakiyeler.First(x => x.CariId == cariId && x.TenantId == k.TenantId);
        b.TlBakiye += isaret * (veresiye ?? tutar);
        db.SaveChanges();
        return id;
    }

    public static int TahsilatSayisi(KuyumcuDbContext db, Kiraci k) =>
        db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT");
}

[Collection("http")]
public sealed class TevsikTaksitHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETYASAL2-TEST-" + ad);
    }

    private string Borc(Kiraci k, decimal tutar, DateTime? tarih = null, string tur = "SATIS", string? fisId = null)
    {
        using var db = f.Vt.OlusturSistem();
        return TaksitFikstur.Borc(db, k, tutar, tarih ?? DateTime.UtcNow.AddDays(-3), tur, fisId);
    }

    private async Task<HttpResponseMessage> Tahsilat(Kiraci k, decimal tutar, string odeme = "NAKIT", string rol = "SAHIP") =>
        await f.Ortam.Istemci(k, rol).PostAsJsonAsync($"/api/v1/cari/{k.CariId}/tahsilat", new
        {
            tlTutari = tutar, odemeTuru = odeme, kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });

    private int TahsilatSayisi(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return TaksitFikstur.TahsilatSayisi(db, k);
    }

    private async Task<decimal> Had(Kiraci k)
    {
        var g = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/tevsik/gunluk")).Json();
        return g.GetProperty("had").GetDecimal();
    }

    // ── K-1 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task K1_62000_veresiye_tek_fis_uc_kez_20000_nakit_tahsilat_UCU_DE_400_yeni_tahsilat_0()
    {
        var k = YeniKiraci("K1");
        Borc(k, 62_000m);
        for (var i = 0; i < 3; i++)
        {
            var c = await Tahsilat(k, 20_000m);
            Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
            var mesaj = (await c.Json()).GetProperty("message").GetString()!;
            Assert.Contains("kartla ya da havaleyle", mesaj, StringComparison.Ordinal);
            // Tur 3 (orkestratör 22:20): "en çok 0,00 TL" YAZILMAZ; ham mevzuat kodu görünür metinde yok.
            Assert.Contains("nakit alınamaz", mesaj, StringComparison.Ordinal);
            Assert.DoesNotMatch(@"(?<![\d.])0,00 TL", mesaj);   // "en çok 0,00 TL" yok (30.000,00 TL had rakamı olabilir)
            TaksitMesajDenetimi.KodsuzTekCumle(mesaj);
        }
        Assert.Equal(0, TahsilatSayisi(k));
    }

    /// <summary>
    /// K-1 çok kalemli varyant (M2): hiçbir kalem tek başına haddi aşmaz, FİŞ aşar.
    /// Borç GERÇEK satış fişi ucuyla doğar.
    /// </summary>
    [Fact]
    public async Task K1_cok_kalemli_fis_kalemleri_had_altinda_FIS_haddi_asiyor_nakit_taksit_400()
    {
        // 17.10 P0-4: fiş toplamı 62.000 > kuyumcu fatura haddi → FATURA + kimlikli cari; 2 g ürün
        // (has ≈ 12.500 TL) — 20.000'lik kalem has değerinin altına düşmesin (özel matrah pozitif).
        Kiraci k;
        using (var kdb = f.Vt.OlusturSistem()) { k = TestKurulum.KiraciKur(kdb, "PAKETYASAL2-TEST-K1 cok kalem", urunGram: 2m); TestKurulum.CariyeTcknYaz(kdb, k); }
        var fis = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId,
            belgeTuru = "FATURA",
            kalemler = new[] { 20_000m, 21_000m, 21_000m }
                .Select(b => new { urunId = k.UrunId, adet = 1m, birimFiyatTl = b, hediye = false }),
            odemeler = new[] { new { arac = "VERESIYE", tutarTl = 62_000m, kasaId = (string?)null } },
            kurSnapshotId = k.KurId,
            fisIskontoYuzde = 0m,
        });
        Assert.True(fis.IsSuccessStatusCode, await fis.Content.ReadAsStringAsync());
        using (var db = f.Vt.OlusturSistem())
        {
            var satirlar = db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS").ToList();
            Assert.Equal(3, satirlar.Count);
            Assert.All(satirlar, h => Assert.True(Math.Abs(h.TlTutari) <= 30_000m));
            Assert.Single(satirlar.Select(h => h.FisId).Distinct());
        }

        var c = await Tahsilat(k, 20_000m);
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Equal(0, TahsilatSayisi(k));
    }

    // ── K-2 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task K2_ayni_borca_20000_KART_tahsilati_200()
    {
        var k = YeniKiraci("K2");
        Borc(k, 62_000m);
        var c = await Tahsilat(k, 20_000m, "KART");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        Assert.Equal(1, TahsilatSayisi(k));
    }

    // ── K-3 + K-8 ───────────────────────────────────────────────────────────

    [Fact]
    public async Task K3_FIFO_10000_kucuk_sonra_50000_buyuk_15000_nakit_400_en_cok_10000_sonra_10000_nakit_200()
    {
        var k = YeniKiraci("K3");
        Borc(k, 10_000m, DateTime.UtcNow.AddDays(-10));
        Borc(k, 50_000m, DateTime.UtcNow.AddDays(-5));

        var red = await Tahsilat(k, 15_000m);
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
        var mesaj = (await red.Json()).GetProperty("message").GetString()!;
        Assert.Contains("en çok 10.000,00 TL nakit", mesaj, StringComparison.Ordinal);
        TaksitMesajDenetimi.KodsuzTekCumle(mesaj);
        Assert.Equal(0, TahsilatSayisi(k));

        var ok = await Tahsilat(k, 10_000m);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(1, TahsilatSayisi(k));

        // Küçük parti kapandı: artık nakit HİÇ alınamaz.
        var sonra = await Tahsilat(k, 1_000m);
        Assert.Equal(HttpStatusCode.BadRequest, sonra.StatusCode);
    }

    [Fact]
    public async Task K8_onizleme_K3_carisinde_taksit_nakitAlinabilir_10000_kural_devrede()
    {
        var k = YeniKiraci("K8");
        Borc(k, 10_000m, DateTime.UtcNow.AddDays(-10));
        Borc(k, 50_000m, DateTime.UtcNow.AddDays(-5));

        var g = await (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync($"/api/v1/tevsik/gunluk?cariId={k.CariId}")).Json();
        var t = g.GetProperty("taksit");
        Assert.True(t.GetProperty("kuralDevrede").GetBoolean());
        Assert.Equal(10_000m, t.GetProperty("nakitAlinabilir").GetDecimal());
        Assert.Equal(50_000m, t.GetProperty("buyukIslemTutari").GetDecimal());
        Assert.Contains("işlemden kalan borç", t.GetProperty("sebep").GetString(), StringComparison.Ordinal);   // tur 2: elle veresiye de olabilir → nötr
    }

    [Fact]
    public async Task K8_kucuk_borclu_cari_onizlemede_kural_DEVREDE_DEGIL()
    {
        var k = YeniKiraci("K8 kucuk");
        Borc(k, 12_000m);
        var g = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync($"/api/v1/tevsik/gunluk?cariId={k.CariId}")).Json();
        var t = g.GetProperty("taksit");
        Assert.False(t.GetProperty("kuralDevrede").GetBoolean());
        Assert.Equal(JsonValueKind.Null, t.GetProperty("nakitAlinabilir").ValueKind);
    }

    // ── K-4 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task K4_kaynak_tam_had_nakit_taksit_SERBEST_had_arti_1_kurus_400()
    {
        var k = YeniKiraci("K4 tam");
        var had = await Had(k);
        Borc(k, had);
        Assert.Equal(HttpStatusCode.OK, (await Tahsilat(k, 5_000m)).StatusCode);

        var k2 = YeniKiraci("K4 kurus");
        Borc(k2, had + 0.01m);
        Assert.Equal(HttpStatusCode.BadRequest, (await Tahsilat(k2, 5_000m)).StatusCode);
        Assert.Equal(0, TahsilatSayisi(k2));
    }

    // ── K-6 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task K6_acilis_bakiyesinden_nakit_tahsilat_200_ve_DOGRULANAMADI_izi_1_satir_kisisel_veri_yok()
    {
        var k = YeniKiraci("K6");
        Borc(k, 50_000m, tur: "ACILIS");

        var c = await Tahsilat(k, 20_000m);
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var iz = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "TEVSIK_TAKSIT_DOGRULANAMADI").ToListAsync();
        var tek = Assert.Single(iz);
        Assert.Contains("20000.00", tek.Detay ?? "", StringComparison.Ordinal);
        var cariAd = db.Cariler.AsNoTracking().First(x => x.Id == k.CariId).Ad;
        Assert.DoesNotContain(cariAd, tek.Detay ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task K6_defterde_karsiligi_olmayan_bakiye_de_KAYNAKSIZ_gecer_ve_iz_birakir()
    {
        var k = YeniKiraci("K6 fark");
        using (var db = f.Vt.OlusturSistem())
        {
            db.CariBakiyeler.First(b => b.CariId == k.CariId).TlBakiye = 40_000m;
            db.SaveChanges();
        }
        Assert.Equal(HttpStatusCode.OK, (await Tahsilat(k, 25_000m)).StatusCode);
        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(1, await kontrol.DenetimKayitlari.CountAsync(
            d => d.TenantId == k.TenantId && d.VarlikTuru == "TEVSIK_TAKSIT_DOGRULANAMADI"));
    }

    /// <summary>Kapı sıradan tahsilatta iz BIRAKMAZ (yalnız kaynaksız paya düşen nakitte).</summary>
    [Fact]
    public async Task Kucuk_satis_borcundan_nakit_tahsilat_200_iz_YOK()
    {
        var k = YeniKiraci("kucuk");
        Borc(k, 8_000m);
        Assert.Equal(HttpStatusCode.OK, (await Tahsilat(k, 8_000m)).StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.DenetimKayitlari.CountAsync(
            d => d.TenantId == k.TenantId && d.VarlikTuru == "TEVSIK_TAKSIT_DOGRULANAMADI"));
    }

    /// <summary>Döviz tahsilatı da aynı kapıdan geçer (efektif döviz NAKİTTİR).</summary>
    [Fact]
    public async Task Doviz_nakit_tahsilat_buyuk_satis_borcuna_400()
    {
        var k = YeniKiraci("doviz");
        using (var db = f.Vt.OlusturSistem())
        {
            TestKurulum.KurDoviz(db, k.KurId);
        }
        string usdKasa;
        using (var db = f.Vt.OlusturSistem())
            usdKasa = TestKurulum.KasaEkle(db, k.TenantId, k.SubeId, "USD");
        Borc(k, 62_000m);
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync($"/api/v1/cari/{k.CariId}/doviz-tahsilat", new
        {
            paraBirimi = "USD", tutar = 100m, kasaId = usdKasa, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var dm = (await c.Json()).GetProperty("message").GetString()!;
        Assert.Contains("kartla ya da havaleyle", dm, StringComparison.Ordinal);
        // Tur 3: döviz tahsilatında da "0,00 TL" yok; kod yok; tek cümle.
        Assert.Contains("nakit alınamaz", dm, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"(?<![\d.])0,00 TL", dm);   // "en çok 0,00 TL" yok (30.000,00 TL had rakamı olabilir)
        TaksitMesajDenetimi.KodsuzTekCumle(dm);
    }

    /// <summary>
    /// Tur 3 (orkestratör 22:20): döviz tahsilatında kısmen izinli nakit rakamı TL KARŞILIĞIDIR
    /// ve metin bunu söyler (10.000 küçük + 50.000 büyük borç; 1.000 USD ≈ 34.000 TL nakit → 400).
    /// </summary>
    [Fact]
    public async Task Doviz_kismen_izinli_red_TL_KARSILIGI_der()
    {
        var k = YeniKiraci("doviz2");
        using (var db = f.Vt.OlusturSistem())
        {
            TestKurulum.KurDoviz(db, k.KurId);
        }
        string usdKasa;
        using (var db = f.Vt.OlusturSistem())
            usdKasa = TestKurulum.KasaEkle(db, k.TenantId, k.SubeId, "USD");
        Borc(k, 10_000m, DateTime.UtcNow.AddDays(-10));
        Borc(k, 50_000m, DateTime.UtcNow.AddDays(-5));
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync($"/api/v1/cari/{k.CariId}/doviz-tahsilat", new
        {
            paraBirimi = "USD", tutar = 700m, kasaId = usdKasa, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var m = (await c.Json()).GetProperty("message").GetString()!;
        Assert.Contains("en çok 10.000,00 TL karşılığı nakit alınabilir", m, StringComparison.Ordinal);
        TaksitMesajDenetimi.KodsuzTekCumle(m);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// Saf kural + DB kapı testleri (K-4 saf, K-5 kaynak günün haddi, K-7 tediye).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tur 3 (orkestratör 22:20): red metni denetimi — kod yok, tek cümle.</summary>
internal static class TaksitMesajDenetimi
{
    public static void KodsuzTekCumle(string m)
    {
        foreach (var kod in new[] { "459", "VUK", "§", "Tebliğ", "md.", "tevsik" })
            Assert.DoesNotContain(kod, m, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".", m, StringComparison.Ordinal);
        // Tek cümle: sondaki nokta dışında cümle sonu yok (tarih/tutar noktaları rakam arasında).
        var govde = m[..^1];
        Assert.DoesNotMatch(@"[.!?](\s|$)", govde);
    }
}

public sealed class TevsikTaksitSafTestleri
{
    private static decimal Had(DateOnly _) => 30_000m;

    [Theory]
    [InlineData(true, false, "Bu borç 27.09.2026 tarihli, 30.000,00 TL'yi aşan bir işlemden kalıyor; nakit alınamaz, kartla ya da havaleyle alın.")]
    [InlineData(false, false, "Bu borç 27.09.2026 tarihli, 30.000,00 TL'yi aşan bir işlemden kalıyor; nakit ödenemez, kartla ya da havaleyle ödeyin.")]
    [InlineData(true, true, "Bu borç 27.09.2026 tarihli, 30.000,00 TL'yi aşan bir işlemden kalıyor; nakit alınamaz, kartla ya da havaleyle alın.")]
    public void Tur3_red_metni_nakit_SIFIRSA_tutar_yazmaz_kodsuz_tek_cumle(bool alacak, bool doviz, string beklenen)
    {
        var s = new TaksitSonuc(true, 0m, 0m, 62_000m, new DateOnly(2026, 9, 27), 30_000m);
        var m = TevsikKapisi.TaksitMesaji(s, alacak, doviz);
        Assert.Equal(beklenen, m);
        TaksitMesajDenetimi.KodsuzTekCumle(m);
    }

    [Theory]
    [InlineData(true, false, "Bu ödemede en çok 10.000,00 TL nakit alınabilir; kalanı kartla ya da havaleyle alın.")]
    [InlineData(true, true, "Bu ödemede en çok 10.000,00 TL karşılığı nakit alınabilir; kalanı kartla ya da havaleyle alın.")]
    [InlineData(false, false, "Bu ödemede en çok 10.000,00 TL nakit ödenebilir; kalanı kartla ya da havaleyle ödeyin.")]
    public void Tur3_red_metni_kismen_izinli_tutar_ve_para_birimi_dogru(bool alacak, bool doviz, string beklenen)
    {
        var s = new TaksitSonuc(true, 10_000m, 0m, 50_000m, new DateOnly(2026, 9, 25), 30_000m);
        var m = TevsikKapisi.TaksitMesaji(s, alacak, doviz);
        Assert.Equal(beklenen, m);
        TaksitMesajDenetimi.KodsuzTekCumle(m);
    }

    [Theory]
    [InlineData(30_000.00, false)]
    [InlineData(30_000.01, true)]
    public void K4_saf_kaynak_tam_had_serbest_bir_kurus_ustu_red(double kaynak, bool red)
    {
        var dagilim = TaksitKurali.FifoDagit(5_000m, 0m,
            [new TaksitKaynagi("p1", (decimal)kaynak, TaksitKaynak.Islem, (decimal)kaynak, new DateOnly(2026, 9, 1))]);
        var s = TaksitKurali.TaksitDegerlendir(5_000m, dagilim, Had);
        Assert.Equal(red, s.Red);
    }

    [Fact]
    public void FIFO_once_kaynaksiz_sonra_eskiden_yeniye_artan_FAZLA()
    {
        var d = TaksitKurali.FifoDagit(70_000m, 5_000m,
        [
            new TaksitKaynagi("eski", 10_000m, TaksitKaynak.Islem, 10_000m, new DateOnly(2026, 9, 1)),
            new TaksitKaynagi("yeni", 50_000m, TaksitKaynak.Islem, 50_000m, new DateOnly(2026, 9, 5)),
        ]);
        Assert.Equal([TaksitKaynak.Kaynaksiz, TaksitKaynak.Islem, TaksitKaynak.Islem, TaksitKaynak.Fazla], d.Select(x => x.Tur));
        Assert.Equal([5_000m, 10_000m, 50_000m, 5_000m], d.Select(x => x.Tuketilen));
        Assert.Equal("eski", d[1].PartiId);
    }

    [Fact]
    public void Takas_kaynakli_parti_kural_DISI()
    {
        var d = TaksitKurali.FifoDagit(20_000m, 0m,
            [new TaksitKaynagi("t", 60_000m, TaksitKaynak.Muaf, 60_000m, new DateOnly(2026, 9, 1))]);
        Assert.False(TaksitKurali.TaksitDegerlendir(20_000m, d, Had).Red);
    }

    [Fact]
    public void NakitAlinabilir_ilk_buyuk_partiye_kadarki_acik_tutar_buyuk_yoksa_null()
    {
        TaksitKaynagi P(string id, decimal t) => new(id, t, TaksitKaynak.Islem, t, new DateOnly(2026, 9, 1));
        Assert.Equal(12_000m, TaksitKurali.NakitAlinabilir(2_000m, [P("a", 10_000m), P("b", 40_000m), P("c", 5_000m)], Had));
        Assert.Null(TaksitKurali.NakitAlinabilir(0m, [P("a", 10_000m)], Had));
    }
}

[Collection("veritabani")]
public sealed class TevsikTaksitDbTestleri(TestVeritabani vt)
{
    private static Kuyumcu.Api.Common.YasalParametreServisi Yasal(KuyumcuDbContext db) =>
        Kuyumcu.Api.Common.YasalParametreServisi.Yukleyicili(() =>
            db.YasalParametreler.AsNoTracking().Where(p => p.Id.StartsWith("yasal1-tohum-"))
              .AsEnumerable().Select(Kuyumcu.Api.Common.YasalParametreSatiri.Kaynaktan).ToList());

    private static TevsikKapisi Kapi(KuyumcuDbContext db) =>
        new(new TevsikKurali(new TevsikEsikleri(), Yasal(db)));

    private static DateTime YerelOgle(int y, int a, int g) =>
        Zaman.YerelGunAraligiUtc(new DateOnly(y, a, g)).Item1.AddHours(12);

    // ── K-5 — KAYNAK GÜNÜN haddi (YASAL-1) ──

    [Fact]
    public async Task K5_20_11_2024_tarihli_25000_veresiye_o_gun_had_7000_bugunku_5000_nakit_taksit_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETYASAL2-TEST-K5 eski had");
        using (var db = vt.OlusturSistem()) TaksitFikstur.Borc(db, k, 25_000m, YerelOgle(2024, 11, 20));

        using var d = vt.Olustur(k.TenantId);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Kapi(d).TaksitDogrula(d, k.TenantId, k.CariId, 5_000m, 5_000m, DateTime.UtcNow));
        Assert.Contains("20.11.2024 tarihli, 7.000,00 TL'yi aşan", ex.Message, StringComparison.Ordinal);   // tur 3 kalıbı: gün önce, sonra had
    }

    [Fact]
    public async Task K5_05_12_2024_tarihli_25000_veresiye_o_gun_had_30000_taksit_nakit_SERBEST()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETYASAL2-TEST-K5 yeni had");
        using (var db = vt.OlusturSistem()) TaksitFikstur.Borc(db, k, 25_000m, YerelOgle(2024, 12, 5));

        using var d = vt.Olustur(k.TenantId);
        var s = await Kapi(d).TaksitDogrula(d, k.TenantId, k.CariId, 5_000m, 5_000m, DateTime.UtcNow);
        Assert.False(s.Red);
    }

    // ── K-7 — tediye yönü ──

    [Fact]
    public async Task K7_40000_veresiye_alis_iki_kez_10000_nakit_odeme_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETYASAL2-TEST-K7 tediye");
        using (var db = vt.OlusturSistem()) TaksitFikstur.Borc(db, k, 40_000m, DateTime.UtcNow.AddDays(-2), tur: "ALIS");

        using var d = vt.Olustur(k.TenantId);
        for (var i = 0; i < 2; i++)
        {
            var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
                Kapi(d).TaksitDogrula(d, k.TenantId, k.CariId, 10_000m, 10_000m, DateTime.UtcNow, alacakYonu: false));
            Assert.Contains("nakit ödenemez, kartla ya da havaleyle ödeyin", ex.Message, StringComparison.Ordinal);   // ödeme yönü cümlesi (tur 3)
            TaksitMesajDenetimi.KodsuzTekCumle(ex.Message);
            Assert.DoesNotContain("satıştan", ex.Message, StringComparison.Ordinal);       // tur 2: kaynak türü uydurulmaz
        }
        // Tahsilat yönünde (müşteri borcu yok) kapı devreye girmez.
        var s = await Kapi(d).TaksitDogrula(d, k.TenantId, k.CariId, 10_000m, 10_000m, DateTime.UtcNow, alacakYonu: true);
        Assert.False(s.Red);
    }

    /// <summary>
    /// Evrak yolu (EvrakModulu nakit tahsil): 50.000 TL'lik TEK veresiyeye karşılık alınan
    /// senedin KASAYA nakit tahsili, büyük işlemin taksitini nakit almaktır → 400, yazma yok.
    /// </summary>
    [Fact]
    public async Task Evrak_buyuk_veresiyeden_senedin_NAKIT_tahsili_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETYASAL2-TEST-evrak");
        using var db = vt.Olustur(k);
        await new Kuyumcu.Api.Modules.Cari.CariServisi(db,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance,
                TestKurulum.Denetim(db))
            .VeresiyeGir(k.CariId, new Kuyumcu.Api.Modules.Cari.VeresiyeGirisDto
                { TlTutari = 50_000m, KurSnapshotId = k.KurId, VadeGun = 30 }, k.Islem);
        var evrak = new Kuyumcu.Api.Modules.Evrak.EvrakServisi(db,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Kuyumcu.Api.Modules.Evrak.EvrakServisi>.Instance,
            TestKurulum.Denetim(db), Kapi(db));
        var e = await evrak.Gir(new Kuyumcu.Api.Modules.Evrak.EvrakGirisDto
        {
            EvrakTuru = "SENET", Tutar = 30_000m, VadeGunu = Zaman.YerelBugun().AddDays(30), EvrakNo = "S-1",
            KaynakCariId = k.CariId, KurSnapshotId = k.KurId,
        }, k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(() => evrak.TahsilEdildi(e.Evrak.Id,
            new Kuyumcu.Api.Modules.Evrak.TahsilEdildiDto { Hedef = "KASA", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
    }

    /// <summary>Kural kapalıyken (Tevsik:Etkin=false) karar hesaplanır ama işlem DURMAZ — iş sonradan bulur.</summary>
    [Fact]
    public async Task Kural_kapaliyken_red_hesaplanir_istisna_ATILMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETYASAL2-TEST-kapali");
        using (var db = vt.OlusturSistem()) TaksitFikstur.Borc(db, k, 62_000m, DateTime.UtcNow.AddDays(-1));
        using var d = vt.Olustur(k.TenantId);
        var kapi = new TevsikKapisi(new TevsikKurali(new TevsikEsikleri { Etkin = false }, Yasal(d)));
        var s = await kapi.TaksitDogrula(d, k.TenantId, k.CariId, 20_000m, 20_000m, DateTime.UtcNow);
        Assert.True(s.Red);
    }
}
