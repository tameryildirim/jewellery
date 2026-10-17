using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET ŞUBE-KAPSAM-A FİNANSAL SINIR TESTLERİ
//
// Uygulayıcının testlerinden BAĞIMSIZ yazıldı. Beklenen değerler SABİTTİR,
// üretim kodundan hesaplanmaz.
//   S1 sıfır       — B şubesinde 0 nakit → A tezgâhtarının kümülatifi 0
//   S2 eksi/karışık— A, B ve şubesiz satırlarda işaretli tutarlar: TEZGÂHTAR(A)
//                    kümülatifi = SAHİP kümülatifi (şube süzgeci kapıya sızmaz)
//   S3 kuruş sınırı— B 20.000 + A 10.000,00 = 30.000 tam had → GEÇER;
//                    B 20.000 + A 10.000,01 → 400 (şubeler arası kuruş)
//   S4 has 4 hane  — hurda 0,0001 (A) + 0,0002 (B) → birleşik 0,0003, kırılım eşit
//   S5 büyük tutar — günlük rapor 999.999.999,99 × 2 şube → birleşik 1.999.999.999,98
//   S6 boş/null    — subeId="" ve "   " boş sayılır; kilitli rolde "tumu" 403
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class SUBEKAPSAMASinirTestleri(HttpFikstur f)
{
    private Kiraci Kiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private string SubeEkle(Kiraci k, string ad = "Şube B")
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.Subeler.Add(new Sube { Id = id, TenantId = k.TenantId, Ad = ad, Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.SaveChanges();
        return id;
    }

    private async Task HareketYaz(Kiraci k, string? sube, string tur, decimal tl, decimal has,
        string odeme, decimal? nakit, string maden = "ALTIN")
    {
        await using var db = f.Vt.OlusturSistem();
        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = sube, Tur = tur, Tarih = DateTime.UtcNow,
            TlTutari = tl, HasGrami = has, ParaBirimi = "TRY", Maden = maden,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            FisId = Kimlik.Ulid(), OdemeTuru = odeme, NakitTutar = nakit,
            Aciklama = "DOGRULAYICI SK-A sinir", Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private HttpClient TezA(Kiraci k) => f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR");
    private HttpClient Sahip(Kiraci k) => f.Ortam.Istemci(k, "SAHIP");

    private static async Task<JsonElement> Ok(HttpResponseMessage c)
    {
        var g = await c.Metin();
        Assert.True(c.StatusCode == HttpStatusCode.OK, $"beklenen 200, gelen {(int)c.StatusCode}: {g}");
        return JsonDocument.Parse(g).RootElement;
    }

    private async Task<decimal> Kumulatif(HttpClient c, Kiraci k) =>
        (await Ok(await c.GetAsync($"/api/v1/tevsik/gunluk?cariId={k.CariId}")))
            .GetProperty("bugunkuNakitToplam").GetDecimal();

    private async Task<int> HareketSayisi(Kiraci k)
    {
        await using var db = f.Vt.OlusturSistem();
        return await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId);
    }

    private Task<HttpResponseMessage> NakitSatis(HttpClient c, Kiraci k, decimal fiyat) =>
        c.PostJson("/api/v1/islemler/satis", new
        {
            urunId = k.UrunId, adet = 1m, birimFiyatTl = fiyat, odemeTuru = "NAKIT", kasaId = k.KasaId,
            cariId = k.CariId, kurSnapshotId = k.KurId,
        });

    // ── S1 — sıfır ──────────────────────────────────────────────────────────
    [Fact]
    public async Task S1_Sifir_B_subesinde_sifir_nakit_A_kumulatifi_sifir()
    {
        var k = Kiraci("DOG-SKA sifir");
        var b = SubeEkle(k);
        await HareketYaz(k, b, "SATIS", 0m, 0m, "NAKIT", 0m);
        Assert.Equal(0m, await Kumulatif(TezA(k), k));
        Assert.Equal(0m, await Kumulatif(Sahip(k), k));
    }

    // ── S2 — işaretli / karışık / şubesiz: kilitli rol = tüm-şube rolü ─────
    [Fact]
    public async Task S2_Eksi_ve_karisik_satirlarda_TEZGAHTAR_A_kumulatifi_SAHIP_ile_ayni()
    {
        var k = Kiraci("DOG-SKA eksi");
        var b = SubeEkle(k);
        await HareketYaz(k, b, "SATIS", 12_345.67m, 0m, "NAKIT", 12_345.67m);
        await HareketYaz(k, b, "ALIS", -2_000.01m, 0m, "NAKIT", -2_000.01m);
        await HareketYaz(k, k.SubeId, "SATIS", 1_000.10m, 0m, "NAKIT", 1_000.10m);
        await HareketYaz(k, b, "SATIS", 500.05m, 0m, "NAKIT", null);   // eski kural: |TlTutari| (sube_id NOT NULL)
        await HareketYaz(k, b, "SATIS", 7_777m, 0m, "KART", 0m);           // kart payı sayılmaz

        var sahip = await Kumulatif(Sahip(k), k);
        var tez = await Kumulatif(TezA(k), k);
        // Sabit beklenen: 12.345,67 − 2.000,01 + 1.000,10 + 500,05 = 11.845,81
        Assert.Equal(11_845.81m, sahip);
        Assert.Equal(sahip, tez);
    }

    // ── S3 — kuruş sınırı, şubeler arası ───────────────────────────────────
    [Fact]
    public async Task S3_Kurus_siniri_tam_had_gecer()
    {
        var k = Kiraci("DOG-SKA had tam");
        var b = SubeEkle(k);
        await HareketYaz(k, b, "SATIS", 20_000m, 0m, "NAKIT", 20_000m);
        var c = await NakitSatis(TezA(k), k, 10_000.00m);
        Assert.True(c.IsSuccessStatusCode, await c.Metin());
    }

    [Fact]
    public async Task S3_Kurus_siniri_bir_kurus_asim_400_ve_satir_yazilmaz()
    {
        var k = Kiraci("DOG-SKA had kurus");
        var b = SubeEkle(k);
        await HareketYaz(k, b, "SATIS", 20_000m, 0m, "NAKIT", 20_000m);
        var once = await HareketSayisi(k);
        var c = await NakitSatis(TezA(k), k, 10_000.01m);
        var g = await c.Metin();
        Assert.True(c.StatusCode == HttpStatusCode.BadRequest, $"beklenen 400, gelen {(int)c.StatusCode}: {g}");
        Assert.Contains("kartla ya da havaleyle", g, StringComparison.Ordinal);
        Assert.Equal(once, await HareketSayisi(k));
    }

    // ── S4 — has 4 hane, birleşik hurda ────────────────────────────────────
    [Fact]
    public async Task S4_Has_dort_hane_birlesik_hurda_bakiyesi_ve_kirilim()
    {
        var k = Kiraci("DOG-SKA has");
        var b = SubeEkle(k);
        await HareketYaz(k, k.SubeId, "HURDA_ALIS", -1m, 0.0001m, "NAKIT", null);
        await HareketYaz(k, b, "HURDA_ALIS", -1m, 0.0002m, "NAKIT", null);

        var j = await Ok(await Sahip(k).GetAsync("/api/v1/hurda/bakiye"));
        static decimal Altin(JsonElement e) => e.GetProperty("satirlar").EnumerateArray()
            .Where(x => x.GetProperty("maden").GetString() == "ALTIN").Sum(x => x.GetProperty("hasGram").GetDecimal());
        Assert.Equal(0.0003m, Altin(j));
        Assert.Equal(0.0003m, j.GetProperty("subeler").EnumerateArray().Sum(Altin));

        var a = await Ok(await Sahip(k).GetAsync($"/api/v1/hurda/bakiye?subeId={k.SubeId}"));
        Assert.Equal(0.0001m, Altin(a));
        var tb = await Ok(await TezA(k).GetAsync("/api/v1/hurda/bakiye"));
        Assert.Equal(0.0001m, Altin(tb));
    }

    // ── S5 — çok büyük tutar ───────────────────────────────────────────────
    [Fact]
    public async Task S5_Buyuk_tutar_gunluk_rapor_birlesik_toplami_kurus_kaybetmez()
    {
        var k = Kiraci("DOG-SKA buyuk");
        var b = SubeEkle(k);
        await HareketYaz(k, k.SubeId, "SATIS", 999_999_999.99m, 0m, "VERESIYE", 0m);
        await HareketYaz(k, b, "SATIS", 999_999_999.99m, 0m, "VERESIYE", 0m);

        var m = await Ok(await Sahip(k).GetAsync($"/api/v1/raporlar/gunluk?subeId={k.SubeId}"));
        var t = await Ok(await Sahip(k).GetAsync("/api/v1/raporlar/gunluk"));
        Assert.Equal(999_999_999.99m, m.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
        Assert.Equal(1_999_999_999.98m, t.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
    }

    // ── S6 — boş / null / büyük-küçük harf ────────────────────────────────
    [Fact]
    public async Task S6_Bos_ve_bosluk_subeId_bos_sayilir_kilitli_rolde_tumu_403()
    {
        var k = Kiraci("DOG-SKA bos");
        var b = SubeEkle(k);
        await HareketYaz(k, k.SubeId, "SATIS", 10m, 0m, "VERESIYE", 0m);
        await HareketYaz(k, b, "SATIS", 20m, 0m, "VERESIYE", 0m);

        foreach (var q in new[] { "", "%20%20%20" })
        {
            var t = await Ok(await Sahip(k).GetAsync($"/api/v1/raporlar/gunluk?subeId={q}"));
            Assert.Equal(30m, t.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
            Assert.Equal("TUM_SUBELER", t.GetProperty("kapsam").GetString());
            var kasa = await Ok(await TezA(k).GetAsync($"/api/v1/kasalar?subeId={q}"));
            Assert.Equal(new[] { k.KasaId }, kasa.EnumerateArray().Select(x => x.GetProperty("id").GetString()).ToArray());
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await TezA(k).GetAsync("/api/v1/kasalar?subeId=tumu")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await TezA(k).GetAsync($"/api/v1/kasalar?subeId={b.ToLowerInvariant()}")).StatusCode);
        // Baştaki/sondaki boşluk kırpılır: " B " → yalnız B (20).
        var kirp = await Ok(await Sahip(k).GetAsync($"/api/v1/raporlar/gunluk?subeId=%20{b}%20"));
        Assert.Equal(20m, kirp.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
    }

    // ── S7 — MASAK kuruş sınırı ŞUBELER ARASI (tur 2 doğrulayıcısı) ─────────
    // MASAK-370: bugün (07.10.2026 ve sonrası) kimliği fiilen tetikleyen en düşük tutar
    // ">= 200.000" (amaç beyanı, TEYİTSİZ; md.5/1-b kimlik eşiği 370.000). B 100.000 + A 99.999,99 =
    // 199.999,99 → eşik altı GEÇER; B 100.000 + A 100.000,00 = 200.000,00 → 400.
    // (Eski hâli 185.000 dönemi: A 84.999,99 / 85.000,00.)

    private Task<HttpResponseMessage> VeresiyeSatis(HttpClient c, Kiraci k, decimal fiyat) =>
        c.PostJson("/api/v1/islemler/satis", new
        {
            urunId = k.UrunId, adet = 1m, birimFiyatTl = fiyat, odemeTuru = "VERESIYE",
            cariId = k.CariId, kurSnapshotId = k.KurId,
        });

    [Fact]
    public async Task S7_Masak_subeler_arasi_kurus_alti_gecer()
    {
        var k = Kiraci("DOGR-SK-A S7 alt");
        var b = SubeEkle(k);
        await HareketYaz(k, b, "SATIS", 100_000m, 0m, "VERESIYE", 0m);
        // 17.10 P0-4: 99.999,99 TL > kuyumcu fatura haddi → faturalı satış fişi + kimlikli cari.
        using (var db = f.Vt.OlusturSistem()) TestKurulum.CariyeTcknYaz(db, k);
        var c = await TezA(k).PostJson("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, belgeTuru = "FATURA", kurSnapshotId = k.KurId, fisIskontoYuzde = 0m,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 99_999.99m, hediye = false } },
            odemeler = new[] { new { arac = "VERESIYE", tutarTl = 99_999.99m, kasaId = (string?)null } },
        });
        Assert.True(c.IsSuccessStatusCode, await c.Metin());
    }

    [Fact]
    public async Task S7_Masak_subeler_arasi_tam_esik_kimliksiz_400_satir_yazilmaz()
    {
        var k = Kiraci("DOGR-SK-A S7 tam");
        var b = SubeEkle(k);
        await HareketYaz(k, b, "SATIS", 100_000m, 0m, "VERESIYE", 0m);
        var once = await HareketSayisi(k);
        var c = await VeresiyeSatis(TezA(k), k, 100_000.00m);
        var g = await c.Metin();
        Assert.True(c.StatusCode == HttpStatusCode.BadRequest, $"beklenen 400, gelen {(int)c.StatusCode}: {g}");
        Assert.Contains("MASAK", g, StringComparison.Ordinal);
        Assert.Equal(once, await HareketSayisi(k));
    }

    // ── S8 — MASAK gereksinim önizlemesi (TEZGÂHTAR A) B şubesini sayar ─────
    [Fact]
    public async Task S8_Masak_gereksinim_onizlemesi_TEZGAHTAR_A_icin_B_subesini_sayar()
    {
        var k = Kiraci("DOGR-SK-A S8");
        var b = SubeEkle(k);
        await HareketYaz(k, b, "SATIS", 100_000m, 0m, "VERESIYE", 0m);
        var j = await Ok(await TezA(k).GetAsync(
            $"/api/v1/masak/gereksinim?tutar=100000&cariId={k.CariId}"));
        Assert.True(j.GetProperty("kimlikGerekli").GetBoolean());
        var alt = await Ok(await TezA(k).GetAsync(
            $"/api/v1/masak/gereksinim?tutar=99999.99&cariId={k.CariId}"));
        Assert.False(alt.GetProperty("kimlikGerekli").GetBoolean());
    }
}
