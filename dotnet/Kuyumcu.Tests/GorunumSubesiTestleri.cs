using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ŞUBE-KAPSAM-A — GÖRÜNÜM ŞUBESİ (sunucu) [HTTP, gerçek MySQL]
//
// K-1  ?subeId=M → yalnız M; toplam = SQL SUM (kuruş farkı 0)
// K-2  subeId yok (tüm şubeler) → M + B
// K-3  aynı ölçüm 7 uçta × 2 durum
// K-4  şubesiz SAHİP /hurda/bakiye → 200 + subeler[] kırılım toplamı = birleşik
// K-5  TEZGÂHTAR (A) ?subeId=B → 403; boş → yalnız A
// K-6  pasif şube / başka kiracının şubesi → 404
// Tercih: gorunum-subesi yaz/oku; geçersiz tercih oturum şubesine düşer.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class GorunumSubesiTestleri(HttpFikstur f)
{
    private sealed record Kurulum(Kiraci K, string SubeB, string KasaB, string SubePasif);

    private const decimal SatisM = 1_000.50m, SatisB = 2_000.25m;
    private const decimal HurdaM = 3.5m, HurdaB = 1.25m;
    private const decimal EmanetM = 2m, EmanetB = 5m;
    private const decimal StokB = 7m;

    private Kurulum Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, ad);
        var now = DateTime.UtcNow;
        var subeB = Kimlik.Ulid();
        var kasaB = Kimlik.Ulid();
        var pasif = Kimlik.Ulid();
        db.Subeler.Add(new Sube { Id = subeB, TenantId = k.TenantId, Ad = "Şube B", Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.Subeler.Add(new Sube { Id = pasif, TenantId = k.TenantId, Ad = "Kapalı şube", Aktif = false, Olusturuldu = now, Guncellendi = now });
        db.Kasalar.Add(new Kasa { Id = kasaB, TenantId = k.TenantId, SubeId = subeB, Ad = "B Kasası", ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = now });
        db.Stoklar.Add(new Stok { Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = subeB, UrunId = k.UrunId, Adet = StokB, ToplamHasGram = StokB * k.UrunHasGrami, Guncellendi = now });

        void H(string sube, string tur, decimal tl, decimal has) => db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = sube, Tur = tur, Tarih = now,
            TlTutari = tl, HasGrami = has, ParaBirimi = "TRY", Maden = "ALTIN",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            OdemeTuru = "VERESIYE", Aciklama = "SK-A fikstür", Olusturuldu = now,
        });
        H(k.SubeId, "SATIS", SatisM, 0m);
        H(subeB, "SATIS", SatisB, 0m);
        H(k.SubeId, "HURDA_ALIS", -10m, HurdaM);
        H(subeB, "HURDA_ALIS", -10m, HurdaB);
        H(k.SubeId, "EMANET_GIRIS", 0m, EmanetM);
        H(subeB, "EMANET_GIRIS", 0m, EmanetB);

        foreach (var (sube, kasa) in new[] { (k.SubeId, k.KasaId), (subeB, kasaB) })
        {
            var cihaz = Kimlik.Ulid();
            db.PosCihazlari.Add(new PosCihaz
            {
                Id = cihaz, TenantId = k.TenantId, SubeId = sube, Ad = sube == k.SubeId ? "POS M" : "POS B", BankaAdi = "Test Bankası",
                KasaId = kasa, ParaBirimi = "TRY", ValorGun = 1, Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
            db.PosTahsilatlari.Add(new PosTahsilat
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = sube, PosCihazId = cihaz, KurSnapshotId = k.KurId,
                BrutTutar = 100m, KomisyonOran = 0m, KomisyonTutar = 0m, NetTutar = 100m, YatanTutar = 0m,
                IslemTarihi = now, ValorTarihi = now.AddDays(1), Durum = PosTahsilatDurumu.Bekliyor, Olusturuldu = now,
            });
        }
        db.SaveChanges();
        return new Kurulum(k, subeB, kasaB, pasif);
    }

    private HttpClient Sahip(Kiraci k) => f.Ortam.Istemci(k, "SAHIP");
    private HttpClient Tezgahtar(Kiraci k) => f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR");

    private static async Task<JsonElement> Ok(HttpResponseMessage c)
    {
        var govde = await c.Metin();
        Assert.True(c.StatusCode == HttpStatusCode.OK, $"beklenen 200, gelen {(int)c.StatusCode}: {govde}");
        return JsonDocument.Parse(govde).RootElement;
    }

    private static string Q(string yol, string? sube) =>
        sube is null ? yol : yol + (yol.Contains('?') ? "&" : "?") + "subeId=" + sube;

    private static decimal HurdaAltin(JsonElement j) =>
        j.GetProperty("satirlar").EnumerateArray()
            .Where(x => x.GetProperty("maden").GetString() == "ALTIN")
            .Sum(x => x.GetProperty("hasGram").GetDecimal());

    // ── K-1 / K-2 — günlük rapor + SQL kanıtı ──────────────────────────────

    [Fact]
    public async Task K1_Gunluk_secili_sube_toplami_SQL_ile_ayni_K2_tum_subeler_toplami()
    {
        var s = Kur("SK-A-TEST K1");
        var m = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/raporlar/gunluk", s.K.SubeId)));
        var tumu = await Ok(await Sahip(s.K).GetAsync("/api/v1/raporlar/gunluk"));

        await using var db = f.Vt.OlusturSistem();
        var (bas, bit) = Zaman.YerelGunAraligiUtc(Zaman.YerelBugun());
        var sqlM = await db.Hareketler.Where(h => h.TenantId == s.K.TenantId && h.SubeId == s.K.SubeId
            && h.Tur == "SATIS" && h.Tarih >= bas && h.Tarih < bit).SumAsync(h => h.TlTutari);

        Assert.Equal(sqlM, m.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
        Assert.Equal(SatisM, m.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
        Assert.Equal("SUBE", m.GetProperty("kapsam").GetString());
        Assert.Single(m.GetProperty("kasalar").EnumerateArray());
        Assert.Equal(SatisM + SatisB, tumu.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
        Assert.Equal("TUM_SUBELER", tumu.GetProperty("kapsam").GetString());
    }

    // ── K-3 — yedi uç × iki durum ──────────────────────────────────────────

    [Fact]
    public async Task K3_Aylik_secili_sube_ve_tum_subeler()
    {
        var s = Kur("SK-A-TEST K3 aylik");
        var m = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/raporlar/aylik", s.K.SubeId)));
        var b = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/raporlar/aylik", s.SubeB)));
        var t = await Ok(await Sahip(s.K).GetAsync("/api/v1/raporlar/aylik"));
        Assert.Equal(SatisM, m.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
        Assert.Equal(SatisB, b.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
        Assert.Equal(SatisM + SatisB, t.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
    }

    // ── P35 ay sınırı (01.10.2026) — K3_Aylik gece 00:00–03:00 TSİ kırmızıydı ──
    // Kök: /raporlar/aylik yil/ay verilmezse ayı DateTime.UtcNow'dan alıyordu; ayın 1'i
    // 00:00–03:00 TSİ arası UTC hâlâ önceki ay → fikstür satışı (yerel: yeni ay) dışarıda kaldı.

    [Theory]
    [InlineData("2026-09-30T22:30:00Z", "2026-10-01")] // 01.10 01:30 TSİ — UTC'de hâlâ Eylül
    [InlineData("2026-10-15T09:00:00Z", "2026-10-15")]
    [InlineData("2026-10-31T21:00:00Z", "2026-11-01")] // tam yerel gece yarısı
    [InlineData("2026-10-31T20:59:59Z", "2026-10-31")]
    public void K3_Ay_siniri_yerel_bugun_isletme_saatine_gore(string utc, string beklenen)
    {
        var an = DateTime.Parse(utc, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
        Assert.Equal(DateTimeKind.Utc, an.Kind);
        Assert.Equal(DateOnly.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), Zaman.YerelBugun(an));
    }

    [Fact]
    public async Task K3_Aylik_gece_satisi_yerel_aya_duser_varsayilan_ay_yerel()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(db, "P35 ay siniri aylik");
            var gece = new DateTime(2026, 9, 30, 22, 30, 0, DateTimeKind.Utc);   // 01.10 01:30 TSİ
            db.Hareketler.Add(new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = gece,
                TlTutari = SatisM, HasGrami = 0m, ParaBirimi = "TRY", Maden = "ALTIN",
                KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
                OdemeTuru = "VERESIYE", Aciklama = "P35 gece satışı", Olusturuldu = gece,
            });
            db.SaveChanges();
        }
        var eylul = await Ok(await Sahip(k).GetAsync("/api/v1/raporlar/aylik?yil=2026&ay=9"));
        var ekim = await Ok(await Sahip(k).GetAsync("/api/v1/raporlar/aylik?yil=2026&ay=10"));
        Assert.Equal(0m, eylul.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
        Assert.Equal(SatisM, ekim.GetProperty("satis").GetProperty("toplamTl").GetDecimal());

        // Varsayılan ay = işletme saatine göre bu ay (UTC ayı değil).
        var bugun = Zaman.YerelBugun();
        var v = await Ok(await Sahip(k).GetAsync("/api/v1/raporlar/aylik"));
        Assert.Equal(bugun.Year, v.GetProperty("yil").GetInt32());
        Assert.Equal(bugun.Month, v.GetProperty("ay").GetInt32());
    }

    [Fact]
    public async Task K3_Hurda_bakiye_secili_sube_ve_tum_subeler()
    {
        var s = Kur("SK-A-TEST K3 hurda");
        var m = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/hurda/bakiye", s.K.SubeId)));
        var t = await Ok(await Sahip(s.K).GetAsync("/api/v1/hurda/bakiye"));
        Assert.Equal(HurdaM, HurdaAltin(m));
        Assert.Equal(HurdaM + HurdaB, HurdaAltin(t));
    }

    [Fact]
    public async Task K3_Hurda_defter_secili_sube_ve_tum_subeler()
    {
        var s = Kur("SK-A-TEST K3 defter");
        var m = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/hurda/defter", s.SubeB)));
        var t = await Ok(await Sahip(s.K).GetAsync("/api/v1/hurda/defter"));
        Assert.Equal(1, m.GetProperty("toplamAdet").GetInt32());
        Assert.Equal(HurdaB, m.GetProperty("satirlar")[0].GetProperty("hasGrami").GetDecimal());
        Assert.Equal(2, t.GetProperty("toplamAdet").GetInt32());
    }

    [Fact]
    public async Task K3_Pos_tahsilatlar_ve_valor_takvimi_secili_sube_ve_tum_subeler()
    {
        var s = Kur("SK-A-TEST K3 pos");
        var m = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/pos/tahsilatlar", s.K.SubeId)));
        var t = await Ok(await Sahip(s.K).GetAsync("/api/v1/pos/tahsilatlar"));
        Assert.Equal(1, m.GetProperty("toplamSatir").GetInt32());
        Assert.Equal("POS M", m.GetProperty("satirlar")[0].GetProperty("cihazAd").GetString());
        Assert.Equal(2, t.GetProperty("toplamSatir").GetInt32());

        var vm = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/pos/valor-takvimi", s.K.SubeId)));
        var vt = await Ok(await Sahip(s.K).GetAsync("/api/v1/pos/valor-takvimi"));
        Assert.Equal("POS M", Assert.Single(vm.EnumerateArray()).GetProperty("cihazAd").GetString());
        Assert.Equal(2, vt.GetArrayLength());

        var cm = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/pos/cihazlar", s.SubeB)));
        Assert.Equal("POS B", Assert.Single(cm.EnumerateArray()).GetProperty("ad").GetString());
    }

    [Fact]
    public async Task K3_Kasalar_secili_sube_ve_tum_subeler()
    {
        var s = Kur("SK-A-TEST K3 kasa");
        var m = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/kasalar", s.SubeB)));
        var t = await Ok(await Sahip(s.K).GetAsync("/api/v1/kasalar"));
        Assert.Equal(new[] { s.KasaB }, m.EnumerateArray().Select(x => x.GetProperty("id").GetString()).ToArray());
        Assert.Equal(2, t.GetArrayLength());
    }

    [Fact]
    public async Task K3_Urun_stok_adetleri_secili_sube_ve_tum_subeler()
    {
        var s = Kur("SK-A-TEST K3 stok");
        var m = await Ok(await Sahip(s.K).GetAsync(Q($"/api/v1/urunler/stok-adetleri?ids={s.K.UrunId}", s.SubeB)));
        var t = await Ok(await Sahip(s.K).GetAsync($"/api/v1/urunler/stok-adetleri?ids={s.K.UrunId}"));
        Assert.Equal(StokB, m.GetProperty("urunler")[0].GetProperty("toplamAdet").GetDecimal());
        Assert.Equal(100m + StokB, t.GetProperty("urunler")[0].GetProperty("toplamAdet").GetDecimal());
    }

    [Fact]
    public async Task K3_Emanet_secili_sube_ve_tum_subeler()
    {
        var s = Kur("SK-A-TEST K3 emanet");
        var m = await Ok(await Sahip(s.K).GetAsync(Q($"/api/v1/emanet/{s.K.CariId}", s.K.SubeId)));
        var t = await Ok(await Sahip(s.K).GetAsync($"/api/v1/emanet/{s.K.CariId}"));
        Assert.Equal(EmanetM, m.GetProperty("mevcutHasGramBakiye").GetDecimal());
        Assert.Single(m.GetProperty("hareketler").EnumerateArray());
        Assert.Equal(EmanetM + EmanetB, t.GetProperty("mevcutHasGramBakiye").GetDecimal());
    }

    [Fact]
    public async Task K3_Pos_fatura_mutabakati_ve_islem_defteri_secili_sube()
    {
        var s = Kur("SK-A-TEST K3 mutabakat");
        var m = await Ok(await Sahip(s.K).GetAsync(Q("/api/v1/raporlar/pos-fatura-mutabakati", s.K.SubeId)));
        var t = await Ok(await Sahip(s.K).GetAsync("/api/v1/raporlar/pos-fatura-mutabakati"));
        Assert.Equal(SatisM, m.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
        Assert.Equal(1, m.GetProperty("pos").GetProperty("adet").GetInt32());
        Assert.Equal(SatisM + SatisB, t.GetProperty("satis").GetProperty("toplamTl").GetDecimal());
        Assert.Equal(2, t.GetProperty("pos").GetProperty("adet").GetInt32());
        Assert.Equal(GorunumSubesi.KiraciGeneli, m.GetProperty("faturaKapsami").GetString());
    }

    // ── K-4 — şubesiz SAHİP, çok şubeli kiracı, birleşik hurda ─────────────

    [Fact]
    public async Task K4_Subesiz_SAHIP_hurda_bakiye_200_ve_kirilim_toplami_birlesik_toplama_esit()
    {
        var s = Kur("SK-A-TEST K4");
        var c = await f.Ortam.Istemci(s.K.KullaniciId, s.K.TenantId, null, "SAHIP").GetAsync("/api/v1/hurda/bakiye");
        var j = await Ok(c);

        Assert.Equal("TUM_SUBELER", j.GetProperty("kapsam").GetString());
        var kirilim = j.GetProperty("subeler").EnumerateArray().ToList();
        Assert.Equal(2, kirilim.Count);
        var kirilimToplami = kirilim.Sum(HurdaAltin);
        Assert.Equal(HurdaAltin(j), kirilimToplami);
        Assert.Equal(HurdaM + HurdaB, kirilimToplami);
    }

    // ── K-5 — kilitli rol başka şube isteyemez (tablo güdümlü) ─────────────

    public static TheoryData<string> KilitliUclar => new()
    {
        "/api/v1/kasalar",
        "/api/v1/hurda/bakiye",
        "/api/v1/hurda/defter",
        "/api/v1/pos/tahsilatlar",
        "/api/v1/pos/valor-takvimi",
        "/api/v1/pos/cihazlar",
        "/api/v1/emanet/{cari}",
        "/api/v1/urunler/stok-adetleri?ids={urun}",
    };

    [Theory]
    [MemberData(nameof(KilitliUclar))]
    public async Task K5_TEZGAHTAR_baska_subeyi_isterse_403_bos_ise_200(string sablon)
    {
        var s = Kur("SK-A-TEST K5");
        var yol = sablon.Replace("{cari}", s.K.CariId).Replace("{urun}", s.K.UrunId);

        var baska = await Tezgahtar(s.K).GetAsync(Q(yol, s.SubeB));
        Assert.True(baska.StatusCode == HttpStatusCode.Forbidden,
            $"{yol} ?subeId=B → beklenen 403, gelen {(int)baska.StatusCode}: {await baska.Metin()}");
        Assert.Contains("yetkiniz yok", await baska.Metin(), StringComparison.OrdinalIgnoreCase);

        var tumu = await Tezgahtar(s.K).GetAsync(Q(yol, GorunumSubesi.Tumu));
        Assert.Equal(HttpStatusCode.Forbidden, tumu.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Tezgahtar(s.K).GetAsync(yol)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Tezgahtar(s.K).GetAsync(Q(yol, s.K.SubeId))).StatusCode);
    }

    [Fact]
    public async Task K5_TEZGAHTAR_bos_subeId_ile_yalniz_kendi_subesini_gorur()
    {
        var s = Kur("SK-A-TEST K5 kendi");
        var kasa = await Ok(await Tezgahtar(s.K).GetAsync("/api/v1/kasalar"));
        Assert.Equal(new[] { s.K.KasaId }, kasa.EnumerateArray().Select(x => x.GetProperty("id").GetString()).ToArray());
        var hurda = await Ok(await Tezgahtar(s.K).GetAsync("/api/v1/hurda/bakiye"));
        Assert.Equal(HurdaM, HurdaAltin(hurda));
        var pos = await Ok(await Tezgahtar(s.K).GetAsync("/api/v1/pos/tahsilatlar"));
        Assert.Equal(1, pos.GetProperty("toplamSatir").GetInt32());
        Assert.Equal("POS M", pos.GetProperty("satirlar")[0].GetProperty("cihazAd").GetString());
    }

    // ── K-6 — pasif şube ve başka kiracının şubesi → 404 ───────────────────

    [Fact]
    public async Task K6_Pasif_sube_ve_baska_kiracinin_subesi_404()
    {
        var s = Kur("SK-A-TEST K6");
        var diger = Kur("SK-A-TEST K6 diger");
        foreach (var yol in new[] { "/api/v1/raporlar/gunluk", "/api/v1/kasalar", "/api/v1/hurda/bakiye", "/api/v1/pos/tahsilatlar" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await Sahip(s.K).GetAsync(Q(yol, s.SubePasif))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Sahip(s.K).GetAsync(Q(yol, diger.SubeB))).StatusCode);
        }
    }

    // ── Kiracı geneli uçlar subeId'yi yok sayar ve bunu söyler ─────────────

    [Fact]
    public async Task Kiraci_geneli_uclar_subeId_yok_sayar_ve_KIRACI_GENELI_yazar()
    {
        var s = Kur("SK-A-TEST kiraci geneli");
        foreach (var yol in new[] { "/api/v1/raporlar/cari-yaslandirma", "/api/v1/raporlar/vade-takibi" })
        {
            var j = await Ok(await Sahip(s.K).GetAsync(Q(yol, s.SubeB)));
            Assert.Equal(GorunumSubesi.KiraciGeneli, j.GetProperty("kapsam").GetString());
        }
    }

    // ── Tercih: gorunum-subesi ─────────────────────────────────────────────

    private static object Yama(string? deger) =>
        new { tercihler = new Dictionary<string, string?> { ["gorunum-subesi"] = deger } };

    [Fact]
    public async Task Tercih_gorunum_subesi_yazilir_okunur_ve_TUMU_kabul_edilir()
    {
        var s = Kur("SK-A-TEST tercih");
        var y = await Ok(await Sahip(s.K).PatchJson("/api/v1/tercihler/benim", Yama(s.SubeB)));
        Assert.Equal(s.SubeB, y.GetProperty("gorunumSubesi").GetProperty("subeId").GetString());

        var g = await Ok(await Sahip(s.K).GetAsync("/api/v1/tercihler/benim"));
        var gs = g.GetProperty("gorunumSubesi");
        Assert.Equal(s.SubeB, gs.GetProperty("subeId").GetString());
        Assert.True(gs.GetProperty("tumSubeler").GetBoolean());
        Assert.False(gs.GetProperty("gecersizTercih").GetBoolean());

        var t = await Ok(await Sahip(s.K).PatchJson("/api/v1/tercihler/benim", Yama("tumu")));
        Assert.Equal(JsonValueKind.Null, t.GetProperty("gorunumSubesi").GetProperty("subeId").ValueKind);
        Assert.Equal("TUMU", t.GetProperty("tercihler").GetProperty("gorunum-subesi").GetString());
    }

    [Fact]
    public async Task Tercih_pasife_alinan_sube_TUMU_ya_degil_oturum_subesine_duser()
    {
        var s = Kur("SK-A-TEST tercih pasif");
        await Ok(await Sahip(s.K).PatchJson("/api/v1/tercihler/benim", Yama(s.SubeB)));
        await using (var db = f.Vt.OlusturSistem())
        {
            var b = await db.Subeler.FirstAsync(x => x.Id == s.SubeB);
            b.Aktif = false;
            await db.SaveChangesAsync();
        }

        var gs = (await Ok(await Sahip(s.K).GetAsync("/api/v1/tercihler/benim"))).GetProperty("gorunumSubesi");
        Assert.True(gs.GetProperty("gecersizTercih").GetBoolean());
        Assert.Equal(s.K.SubeId, gs.GetProperty("subeId").GetString());
        Assert.NotEqual("TUMU", gs.GetProperty("deger").GetString());
    }

    [Fact]
    public async Task Tercih_gecersiz_degerler_yazilmaz()
    {
        var s = Kur("SK-A-TEST tercih red");
        var diger = Kur("SK-A-TEST tercih red diger");
        Assert.Equal(HttpStatusCode.NotFound, (await Sahip(s.K).PatchJson("/api/v1/tercihler/benim", Yama(s.SubePasif))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Sahip(s.K).PatchJson("/api/v1/tercihler/benim", Yama(diger.SubeB))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Tezgahtar(s.K).PatchJson("/api/v1/tercihler/benim", Yama("TUMU"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Tezgahtar(s.K).PatchJson("/api/v1/tercihler/benim", Yama(s.SubeB))).StatusCode);

        // Kilitli rol için: tercih yok → kendi şubesi, tumSubeler=false (seçici gizli).
        var gs = (await Ok(await Tezgahtar(s.K).GetAsync("/api/v1/tercihler/benim"))).GetProperty("gorunumSubesi");
        Assert.False(gs.GetProperty("tumSubeler").GetBoolean());
        Assert.Equal(s.K.SubeId, gs.GetProperty("subeId").GetString());

        // null = sil (PATCH sözleşmesi) → tüm-şube rolünde varsayılan "tüm şubeler".
        await Ok(await Sahip(s.K).PatchJson("/api/v1/tercihler/benim", Yama(s.SubeB)));
        var sil = await Ok(await Sahip(s.K).PatchJson("/api/v1/tercihler/benim", Yama(null)));
        Assert.False(sil.GetProperty("tercihler").TryGetProperty("gorunum-subesi", out _));
        Assert.Equal(JsonValueKind.Null, sil.GetProperty("gorunumSubesi").GetProperty("subeId").ValueKind);
    }
}
