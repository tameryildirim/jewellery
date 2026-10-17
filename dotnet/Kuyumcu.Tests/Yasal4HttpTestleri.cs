using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-4 — Y4-A (gider pusulası süresi + yeniden düzenleme), Y4-B (stopaj
// borcu), Y4-C (KMTS deseni) HTTP + SQL kanıtları. Kabul ölçütleri K-1…K-9, K-14.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class Yasal4HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "PAKETYASAL4-TEST-" + ad));
    }

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>Hurda alışı servis yolundan (gerçek hareket + pusula aynı commit).</summary>
    private async Task<string> HurdaAl(Kiraci k, string tip = SaticiTipi.NihaiTuketici)
    {
        using var db = f.Vt.Olustur(k);
        await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).HurdaAlis(new MSatis.HurdaAlisDto
        {
            // 1 g: aynı cariye art arda nakit alışlar tevsik haddine (30.000 TL) takılmasın.
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 1m,
            BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Satici = TestKurulum.HurdaSatici(tip),
        }, k.Islem);
        using var kontrol = f.Vt.OlusturSistem();
        return kontrol.GiderPusulalari.Where(g => g.TenantId == k.TenantId)
            .OrderByDescending(g => g.Olusturuldu).ThenByDescending(g => g.Id).First().Id;
    }

    /// <summary>Pusulayı test için düzenler (teslim günü / iptal / stopaj) — doğrudan SQL değil, EF.</summary>
    private void Duzelt(Kiraci k, string id, Action<GiderPusulasi> a)
    {
        using var db = f.Vt.Olustur(k);
        var p = db.GiderPusulalari.Single(g => g.Id == id);
        a(p);
        db.SaveChanges();
    }

    private static void Iptal(GiderPusulasi p)
    {
        p.IptalEdildi = true;
        p.IptalGerekce = "test: satıcının adı yanlış yazılmıştı, düzeltilecek";
        p.IptalTarihi = DateTime.UtcNow;
    }

    private static DateTime GunOnce(int gun) => DateTime.UtcNow.AddDays(-gun);

    private static object Satici => new { adSoyad = "Doğru Satıcı", tckn = "10000000146", adres = "Kızılay, Ankara" };

    // ── Y4-A ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task K1_on_gun_once_duzenlenmis_iptal_edilmemis_pusula_sure_takibinde_GECIKMIS_DEGIL()
    {
        var k = YeniKiraci("K1");
        var id = await HurdaAl(k);
        Duzelt(k, id, p => { p.TeslimTarihi = GunOnce(10); p.DuzenlemeSonTarihi = GunOnce(3); });

        var c = f.Ortam.Istemci(k);
        var r = await Govde(await c.GetAsync("/api/v1/gider-pusulalari/sure-takibi"));
        Assert.Equal(0, r.GetProperty("gecikmisSayisi").GetInt32());
        Assert.Equal(0, r.GetProperty("yaklasanSayisi").GetInt32());

        // Liste: süre karşılanmış belgede gecikti/kalanGun DÖNMEZ (null).
        var liste = await Govde(await c.GetAsync("/api/v1/gider-pusulalari"));
        var s = liste.EnumerateArray().Single(x => x.GetProperty("id").GetString() == id);
        Assert.Equal(JsonValueKind.Null, s.GetProperty("gecikti").ValueKind);
        Assert.Equal(JsonValueKind.Null, s.GetProperty("kalanGun").ValueKind);
        Assert.False(s.GetProperty("yenidenDuzenlenebilir").GetBoolean());
    }

    [Fact]
    public async Task K2_iptal_edilmis_yenisiz_teslimi_5_gun_once_YAKLASAN_1_son_gun_teslim_arti_7()
    {
        var k = YeniKiraci("K2");
        var id = await HurdaAl(k);
        var teslim = GunOnce(5);
        Duzelt(k, id, p => { p.TeslimTarihi = teslim; Iptal(p); });

        var r = await Govde(await f.Ortam.Istemci(k).GetAsync("/api/v1/gider-pusulalari/sure-takibi"));
        Assert.Equal(1, r.GetProperty("yaklasanSayisi").GetInt32());
        Assert.Equal(0, r.GetProperty("gecikmisSayisi").GetInt32());
        var beklenen = YasalParametreServisi.IslemGunu(teslim).AddDays(7);
        var kalem = r.GetProperty("yaklasan")[0];
        Assert.Equal(beklenen.ToString("yyyy-MM-dd"), kalem.GetProperty("sonGun").GetString());
        Assert.Equal(id, kalem.GetProperty("id").GetString());
        Assert.Equal(beklenen.ToString("yyyy-MM-dd"), r.GetProperty("enYakinSonGun").GetString());
    }

    [Fact]
    public async Task K3_yeniden_duzenle_ayni_hareket_duzeltilen_eski_hareket_ve_kasa_ve_hurda_bakiyesi_DEGISMEZ()
    {
        var k = YeniKiraci("K3");
        var id = await HurdaAl(k);
        Duzelt(k, id, p => { p.TeslimTarihi = GunOnce(2); Iptal(p); });
        var c = f.Ortam.Istemci(k);

        int hareket, kasaSatir; string hareketId;
        using (var db = f.Vt.OlusturSistem())
        {
            hareket = db.Hareketler.Count(h => h.TenantId == k.TenantId);
            kasaSatir = db.KasaHareketleri.Count(h => h.TenantId == k.TenantId);
            hareketId = db.GiderPusulalari.Single(g => g.Id == id).HareketId;
        }
        var bakiyeOnce = (await Govde(await c.GetAsync("/api/v1/hurda/bakiye"))).ToString();

        var y = await c.PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var yeni = await Govde(y);
        Assert.Equal(id, yeni.GetProperty("duzeltilenId").GetString());
        Assert.Equal(hareketId, yeni.GetProperty("hareketId").GetString());
        Assert.Equal("Doğru Satıcı", yeni.GetProperty("saticiAdSoyad").GetString());
        Assert.NotEqual(id, yeni.GetProperty("id").GetString());

        using (var db = f.Vt.OlusturSistem())
        {
            Assert.Equal(hareket, db.Hareketler.Count(h => h.TenantId == k.TenantId));
            Assert.Equal(kasaSatir, db.KasaHareketleri.Count(h => h.TenantId == k.TenantId));
            var hepsi = db.GiderPusulalari.Where(g => g.TenantId == k.TenantId).ToList();
            Assert.Equal(2, hepsi.Count);
            Assert.All(hepsi, g => Assert.Equal(hareketId, g.HareketId));
            var eski = hepsi.Single(g => g.Id == id);
            var yeniP = hepsi.Single(g => g.Id != id);
            Assert.Equal(eski.TeslimTarihi, yeniP.TeslimTarihi);   // son gün ORİJİNAL teslimden
            Assert.Equal(eski.BrutTutar, yeniP.BrutTutar);
            Assert.Equal(eski.StopajTutar, yeniP.StopajTutar);
        }
        Assert.Equal(bakiyeOnce, (await Govde(await c.GetAsync("/api/v1/hurda/bakiye"))).ToString());

        // Süre takibi temizlendi; aynı alış için ikinci kez → 409.
        var r = await Govde(await c.GetAsync("/api/v1/gider-pusulalari/sure-takibi"));
        Assert.Equal(0, r.GetProperty("yaklasanSayisi").GetInt32());
        var tekrar = await c.PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.Conflict, tekrar.StatusCode);
    }

    [Fact]
    public async Task K4_teslimden_9_gun_sonra_409_yeni_pusula_yok_belgesiz_1_ve_6_gun_sonra_200()
    {
        var k = YeniKiraci("K4");
        var id = await HurdaAl(k);
        Duzelt(k, id, p => { p.TeslimTarihi = GunOnce(9); Iptal(p); });
        var c = f.Ortam.Istemci(k);

        var y = await c.PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.Conflict, y.StatusCode);
        Assert.Contains("artık gider pusulası düzenlenemez", (await Govde(y)).GetProperty("message").ToString());
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(1, db.GiderPusulalari.Count(g => g.TenantId == k.TenantId));

        var r = await Govde(await c.GetAsync("/api/v1/gider-pusulalari/sure-takibi"));
        Assert.Equal(1, r.GetProperty("gecikmisSayisi").GetInt32());   // belgesiz alış kaydı

        // 6 gün → hâlâ süre içinde → 200.
        var k2 = YeniKiraci("K4b");
        var id2 = await HurdaAl(k2);
        Duzelt(k2, id2, p => { p.TeslimTarihi = GunOnce(6); Iptal(p); });
        var y2 = await f.Ortam.Istemci(k2).PostAsJsonAsync($"/api/v1/gider-pusulalari/{id2}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.OK, y2.StatusCode);
    }

    [Fact]
    public async Task K5_iptal_edilmemis_pusulada_yeniden_duzenle_409_ve_TEZGAHTAR_403()
    {
        var k = YeniKiraci("K5");
        var id = await HurdaAl(k);
        var y = await f.Ortam.Istemci(k).PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.Conflict, y.StatusCode);

        var t = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/yeniden-duzenle", Satici);
        Assert.Equal(HttpStatusCode.Forbidden, t.StatusCode);
    }

    // ── YASAL-4 tur 4 (hasım SINIR2): bir alış için EN ÇOK BİR geçerli pusula — veritabanı kilidi ──
    [Fact]
    public async Task Y4A_tur4_ayni_alis_icin_ikinci_GECERLI_pusula_YAZILAMAZ_iptal_edilmis_engellemez()
    {
        var k = YeniKiraci("TUR4-KILIT");
        var id = await HurdaAl(k);
        GiderPusulasi Kopya(GiderPusulasi o, string no) => new()
        {
            Id = Kimlik.Ulid(), TenantId = o.TenantId, PusulaNo = no, HareketId = o.HareketId, SubeId = o.SubeId,
            CariId = o.CariId, SaticiTipi = o.SaticiTipi, SaticiAdSoyad = o.SaticiAdSoyad, SaticiTckn = o.SaticiTckn,
            SaticiTcknHash = o.SaticiTcknHash, SifreSurumu = o.SifreSurumu, SaticiAdres = o.SaticiAdres,
            AyarId = o.AyarId, GramAgirlik = o.GramAgirlik, HasGrami = o.HasGrami, BrutTutar = o.BrutTutar,
            StopajOran = o.StopajOran, StopajTutar = o.StopajTutar, NetOdenen = o.NetOdenen,
            TeslimTarihi = o.TeslimTarihi, DuzenlemeSonTarihi = o.DuzenlemeSonTarihi, DuzeltilenId = o.Id,
            Olusturuldu = DateTime.UtcNow,
        };
        using (var db = f.Vt.Olustur(k))
        {
            var o = db.GiderPusulalari.AsNoTracking().Single(g => g.Id == id);
            Assert.Equal(o.HareketId, o.GecerliHareketId);              // geçerli belgede kolon dolu
            db.GiderPusulalari.Add(Kopya(o, "GP-TEST-000901"));
            var ex = Assert.ThrowsAny<DbUpdateException>(() => db.SaveChanges());
            Assert.Contains("gecerli_hareket", ex.InnerException?.Message ?? "", StringComparison.OrdinalIgnoreCase);
        }
        Duzelt(k, id, Iptal);
        using (var db = f.Vt.Olustur(k))
        {
            var o = db.GiderPusulalari.AsNoTracking().Single(g => g.Id == id);
            Assert.Null(o.GecerliHareketId);                            // iptal edilmişte NULL → engellemez
            db.GiderPusulalari.Add(Kopya(o, "GP-TEST-000902"));
            db.SaveChanges();
        }
        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(1, kontrol.GiderPusulalari.Count(g => g.TenantId == k.TenantId && !g.IptalEdildi));
    }

    [Fact]
    public async Task Y4A_tur4_eszamanli_12_yeniden_duzenle_TEK_gecerli_pusula_digerleri_409_Turkce()
    {
        var k = YeniKiraci("TUR4-YARIS");
        var id = await HurdaAl(k);
        Duzelt(k, id, p => { p.TeslimTarihi = GunOnce(1); Iptal(p); });
        var yanitlar = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ =>
            f.Ortam.Istemci(k).PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/yeniden-duzenle", Satici)));
        var kodlar = yanitlar.Select(y => (int)y.StatusCode).ToList();
        Assert.Equal(1, kodlar.Count(x => x == 200));
        Assert.All(kodlar.Where(x => x != 200), x => Assert.Equal(409, x));
        foreach (var y in yanitlar.Where(y => y.StatusCode == HttpStatusCode.Conflict))
        {
            var m = (await Govde(y)).GetProperty("message").GetString() ?? "";
            // Ön kontrol ("zaten var") ya da kilit ("aynı anda … YAZILMADI") — genel "Bu kayıt zaten var" DEĞİL.
            Assert.True(m.Contains("YAZILMADI", StringComparison.Ordinal) || m.Contains("ikinci pusula düzenlenmez", StringComparison.Ordinal), m);
            Assert.DoesNotContain("Duplicate", m, StringComparison.OrdinalIgnoreCase);
        }
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1, db.GiderPusulalari.Count(g => g.TenantId == k.TenantId && !g.IptalEdildi));
        Assert.Equal(2, db.GiderPusulalari.Count(g => g.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Iptal_yaniti_yeniden_duzenle_yolunu_soyler_yeni_alis_DEMEZ()
    {
        var k = YeniKiraci("Iptal");
        var id = await HurdaAl(k);
        var r = await f.Ortam.Istemci(k).PostAsJsonAsync($"/api/v1/gider-pusulalari/{id}/iptal",
            new { gerekce = "test: satıcının adı yanlış yazılmıştı, düzeltilecek" });
        var m = (await Govde(r)).GetProperty("duzeltmeYolu").GetString()!;
        Assert.Contains("Yeniden düzenle", m);
        Assert.DoesNotContain("YENİ bir hurda alışı girin", m);
    }

    // ── Hasım bulgusu 01.10 (ORTA): gün sonu mutabakatı süre takibiyle AYNI türetme ──

    private void HareketTarihi(Kiraci k, string pusulaId, DateTime tarih)
    {
        using var db = f.Vt.Olustur(k);
        var p = db.GiderPusulalari.Single(g => g.Id == pusulaId);
        p.TeslimTarihi = tarih;
        db.SaveChanges();
        // Defter satırı EF ile güncellenemez (append-only kapanı); test kurgusu için doğrudan tarih kaydırılır.
        db.Hareketler.Where(h => h.Id == p.HareketId).ExecuteUpdate(x => x.SetProperty(h => h.Tarih, tarih));
    }

    private async Task<JsonElement> Mutabakat(Kiraci k, DateTime gunUtc) =>
        await Govde(await f.Ortam.Istemci(k).GetAsync(
            $"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gunUtc:yyyy-MM-dd}"));

    [Fact]
    public async Task Mutabakat_10_gun_once_GECERLI_pusulada_sure_alarmi_YOK_iptal_yenisiz_gecmis_BELGESIZ()
    {
        var k = YeniKiraci("MUT1");
        var gun = DateTime.UtcNow.Date.AddDays(-10).AddHours(9);
        var id = await HurdaAl(k);
        HareketTarihi(k, id, gun);

        var m = await Mutabakat(k, gun);
        Assert.Equal(0, m.GetProperty("hurda").GetProperty("gecikmisPusulaSayisi").GetInt32());
        Assert.Equal(0, m.GetProperty("hurda").GetProperty("belgesizSayisi").GetInt32());
        Assert.DoesNotContain(m.GetProperty("uyarilar").EnumerateArray(), u => u.GetString()!.Contains("pusula", StringComparison.OrdinalIgnoreCase));

        // Aynı alışın pusulası iptal edildi, yenisi yok ve son gün (teslim+7) geçti → BELGESİZ alış.
        Duzelt(k, id, Iptal);
        m = await Mutabakat(k, gun);
        Assert.Equal(1, m.GetProperty("hurda").GetProperty("belgesizSayisi").GetInt32());
        Assert.Equal(0, m.GetProperty("hurda").GetProperty("gecikmisPusulaSayisi").GetInt32());
        Assert.False(m.GetProperty("mutabik").GetBoolean());
    }

    [Fact]
    public async Task Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil()
    {
        var k = YeniKiraci("MUT2");
        var id = await HurdaAl(k);
        Duzelt(k, id, Iptal);
        // "Bugün" İŞLETME GÜNÜDÜR (Europe/Istanbul): 00:00–03:00 arası UTC günü bir gün geride kalır ve rapor dünü
        // sorardı (18.10 00:03'te ölçülen kırmızı) — sunucunun gün kuralıyla aynı gün sorulur.
        var m = await Mutabakat(k, Kuyumcu.Api.Common.Zaman.YerelBugun().ToDateTime(TimeOnly.MinValue));
        Assert.Equal(1, m.GetProperty("hurda").GetProperty("gecikmisPusulaSayisi").GetInt32());
        Assert.Equal(0, m.GetProperty("hurda").GetProperty("belgesizSayisi").GetInt32());
        Assert.Contains(m.GetProperty("uyarilar").EnumerateArray(), u => u.GetString()!.Contains("yeniden düzenlenmeli", StringComparison.Ordinal));
        Assert.DoesNotContain(m.GetProperty("uyarilar").EnumerateArray(), u => u.GetString()!.Contains("süresi dolmuş", StringComparison.Ordinal));
    }

    // ── Hasım bulgusu 01.10 (ORTA): § 4.1 madde 2 — MÜKELLEF satıcı, fatura no boş ──

    [Fact]
    public async Task Sure_takibi_mukellef_alisi_fatura_no_bossa_FATURA_BEKLENIYOR_sayar_doluysa_saymaz()
    {
        var k = YeniKiraci("MUKF");
        string hareketId;
        using (var db = f.Vt.Olustur(k))
        {
            var satici = TestKurulum.HurdaSatici(SaticiTipi.Mukellef);
            satici.TedarikciFaturaNo = "TDR-2026-77";
            await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).HurdaAlis(new MSatis.HurdaAlisDto
            {
                CariId = k.CariId, AyarId = "K22", GramAgirlik = 1m,
                BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId, Satici = satici,
            }, k.Islem);
            hareketId = db.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS").Select(h => h.Id).Single();
        }
        var c = f.Ortam.Istemci(k);
        var st = await Govde(await c.GetAsync("/api/v1/gider-pusulalari/sure-takibi"));
        Assert.Equal(0, st.GetProperty("faturaBekleyenSayisi").GetInt32());

        using (var db = f.Vt.Olustur(k))
            db.Hareketler.Where(h => h.Id == hareketId).ExecuteUpdate(x => x.SetProperty(h => h.FaturaNo, (string?)null));
        st = await Govde(await c.GetAsync("/api/v1/gider-pusulalari/sure-takibi"));
        Assert.Equal(1, st.GetProperty("faturaBekleyenSayisi").GetInt32());
        // Geri sayım YOK (süre teyitsiz): yalnız bilgi; pusula süre kümelerine girmez.
        Assert.Equal(0, st.GetProperty("gecikmisSayisi").GetInt32());
        Assert.Equal(0, st.GetProperty("yaklasanSayisi").GetInt32());
        Assert.False(st.GetProperty("faturaBekleyen")[0].TryGetProperty("sonGun", out _));
    }

    // ── Y4-B ────────────────────────────────────────────────────────────────

    /// <summary>K-6 senaryosu: Ağustos 2026'da 3 ESNAF pusulası (200+300+740) + 1 iptal-yeniden düzenlenen (100).</summary>
    private async Task<(Kiraci K, string IptalId)> StopajSenaryosu(string ad)
    {
        var k = YeniKiraci(ad);
        var teslim = new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);
        foreach (var tutar in new[] { 200m, 300m, 740m })
        {
            var id = await HurdaAl(k, SaticiTipi.EsnafMuaf);
            Duzelt(k, id, p => { p.TeslimTarihi = teslim; p.StopajTutar = tutar; });
        }
        var iptalId = await HurdaAl(k, SaticiTipi.EsnafMuaf);
        Duzelt(k, iptalId, p => { p.TeslimTarihi = teslim; p.StopajTutar = 100m; Iptal(p); });
        using (var db = f.Vt.Olustur(k))
        {
            // Yeniden düzenlenmiş hâli (aynı alış, aynı stopaj) — süre geçtiği için servis
            // yolu 409 verir; senaryo belgesi EF ile kurulur (kapı yolu YenidenDuzenle aynı alanları kopyalar).
            var eski = db.GiderPusulalari.AsNoTracking().Single(g => g.Id == iptalId);
            var kapi = Kuyumcu.Api.Modules.Hurda.GiderPusulasiKapisi.Varsayilan();
            kapi.YenidenDuzenle(db, eski, TestKurulum.HurdaSatici(SaticiTipi.EsnafMuaf), "GP-2026-990001", k.Islem, DateTime.UtcNow);
            await db.SaveChangesAsync();
        }
        return (k, iptalId);
    }

    private static JsonElement Donem(JsonElement y, string d) =>
        y.GetProperty("donemler").EnumerateArray().Single(x => x.GetProperty("donem").GetString() == d);

    [Fact]
    public async Task K6_K7_stopaj_donemi_iptal_edilen_sayilmaz_yenisi_sayilir_odeme_kalan_sifir_geri_al()
    {
        var (k, _) = await StopajSenaryosu("K6");
        var c = f.Ortam.Istemci(k);

        var d = Donem(await Govde(await c.GetAsync("/api/v1/stopaj/donemler?yil=2026")), "2026-08");
        Assert.Equal(1340m, d.GetProperty("kesilenTl").GetDecimal());
        Assert.Equal(1340m, d.GetProperty("kalanTl").GetDecimal());
        Assert.Equal(4, d.GetProperty("pusulaSayisi").GetInt32());
        Assert.Equal("2026-09-26", d.GetProperty("sonGun").GetString());   // izleyen ayın 26'sı (YASAL-1, TEYİTSİZ)
        Assert.True(d.GetProperty("sonGunGecti").GetBoolean());

        int kasaOnce;
        using (var db = f.Vt.OlusturSistem()) kasaOnce = db.KasaHareketleri.Count(h => h.TenantId == k.TenantId);

        // Fazla tutar → 400 (alan: tutar).
        var fazla = await c.PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId, tutar = 2000m });
        Assert.Equal(HttpStatusCode.BadRequest, fazla.StatusCode);

        var o = await c.PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId });
        Assert.Equal(HttpStatusCode.OK, o.StatusCode);
        var od = await Govde(o);
        Assert.Equal(1340m, od.GetProperty("tutarTl").GetDecimal());
        Assert.Equal(0m, od.GetProperty("kalanTl").GetDecimal());

        d = Donem(await Govde(await c.GetAsync("/api/v1/stopaj/donemler?yil=2026")), "2026-08");
        Assert.Equal(0m, d.GetProperty("kalanTl").GetDecimal());
        Assert.False(d.GetProperty("sonGunGecti").GetBoolean());

        string odemeId;
        using (var db = f.Vt.OlusturSistem())
        {
            Assert.Equal(kasaOnce + 1, db.KasaHareketleri.Count(h => h.TenantId == k.TenantId));
            var satir = db.StopajOdemeleri.Single(x => x.TenantId == k.TenantId);
            odemeId = satir.Id;
            var kasa = db.KasaHareketleri.Single(h => h.Id == satir.HareketId);
            Assert.Equal(-1340m, kasa.Tutar);
            Assert.Null(kasa.HareketId);   // deftere değil, gider yolundan kasa satırı
            Assert.Equal("Vergi — stopaj", db.GiderKategorileri.Single(g => g.Id == kasa.GiderKategoriId).Ad);
        }

        // Kalan 0 iken yeni ödeme → 409.
        Assert.Equal(HttpStatusCode.Conflict,
            (await c.PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId })).StatusCode);

        // Geri al = kasa stornosu → kalan yeniden 1340; ödeme satırı SİLİNMEZ.
        var g = await c.PostAsJsonAsync($"/api/v1/stopaj/odemeler/{odemeId}/geri-al",
            new { gerekce = "test: yanlış kasadan ödendi olarak kaydedildi" });
        Assert.Equal(HttpStatusCode.OK, g.StatusCode);
        d = Donem(await Govde(await c.GetAsync("/api/v1/stopaj/donemler?yil=2026")), "2026-08");
        Assert.Equal(1340m, d.GetProperty("kalanTl").GetDecimal());
        Assert.True(d.GetProperty("odemeler")[0].GetProperty("geriAlindi").GetBoolean());
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(1, db.StopajOdemeleri.Count(x => x.TenantId == k.TenantId));
    }

    // ── Hasım bulgusu 01.10 (KRİTİK): eşzamanlı ödeme kesilenin üstüne çıkamaz ──

    [Fact]
    public async Task Stopaj_ardisik_iki_kismi_odeme_sira_1_ve_2_alir_kalan_dogru()
    {
        var (k, _) = await StopajSenaryosu("SIRA");
        var c = f.Ortam.Istemci(k);
        var a = await c.PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId, tutar = 340m });
        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        var b = await c.PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId, tutar = 1000m });
        Assert.Equal(HttpStatusCode.OK, b.StatusCode);
        Assert.Equal(0m, (await Govde(b)).GetProperty("kalanTl").GetDecimal());
        using var db = f.Vt.OlusturSistem();
        var siralar = db.StopajOdemeleri.Where(x => x.TenantId == k.TenantId).OrderBy(x => x.SiraNo).Select(x => x.SiraNo).ToList();
        Assert.Equal([1, 2], siralar);
    }

    [Fact]
    public async Task Stopaj_ayni_donemde_ayni_sira_ikinci_kez_YAZILAMAZ_veritabani_kilidi()
    {
        var (k, _) = await StopajSenaryosu("KILIT");
        using var db = f.Vt.Olustur(k);
        StopajOdemesi Satir(string hid) => new()
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, Donem = "2026-08", Tutar = 1m, OdemeTarihi = DateTime.UtcNow,
            HareketId = hid, KasaId = k.KasaId, KullaniciId = k.Islem.Id, Olusturuldu = DateTime.UtcNow, SiraNo = 1,
        };
        db.StopajOdemeleri.Add(Satir(Kimlik.Ulid()));
        db.SaveChanges();
        db.StopajOdemeleri.Add(Satir(Kimlik.Ulid()));
        var ex = Assert.ThrowsAny<DbUpdateException>(() => db.SaveChanges());
        Assert.Contains("sira_no", ex.InnerException?.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stopaj_sira_cakismasi_409_Turkce_mesaj_kasa_satiri_ve_odeme_YAZILMAZ()
    {
        // Eşzamanlı ödemenin izi: okuma anından SONRA işlenmiş bir ödeme sırası 2'yi almış
        // (sayım 1 → bu istek de 2'yi dener). Veritabanı ikincisini reddeder; işlem bütünüyle geri alınır.
        var (k, _) = await StopajSenaryosu("CAKISMA");
        using (var db = f.Vt.Olustur(k))
        {
            db.StopajOdemeleri.Add(new StopajOdemesi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, Donem = "2026-08", Tutar = 0.01m, OdemeTarihi = DateTime.UtcNow,
                HareketId = Kimlik.Ulid(), KasaId = k.KasaId, KullaniciId = k.Islem.Id, Olusturuldu = DateTime.UtcNow, SiraNo = 2,
            });
            db.SaveChanges();
        }
        int kasaOnce;
        using (var db = f.Vt.OlusturSistem()) kasaOnce = db.KasaHareketleri.Count(h => h.TenantId == k.TenantId);
        var r = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId, tutar = 100m });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Contains("aynı anda başka bir stopaj ödemesi", (await Govde(r)).GetProperty("message").ToString());
        using (var db = f.Vt.OlusturSistem())
        {
            Assert.Equal(kasaOnce, db.KasaHareketleri.Count(h => h.TenantId == k.TenantId));
            Assert.Equal(1, db.StopajOdemeleri.Count(o => o.TenantId == k.TenantId));
        }
    }

    [Fact]
    public async Task Stopaj_eszamanli_12_tam_odeme_yalniz_biri_yazilir_digerleri_409_kasa_tek_cikis()
    {
        var (k, _) = await StopajSenaryosu("YARIS");
        int kasaOnce;
        using (var db = f.Vt.OlusturSistem()) kasaOnce = db.KasaHareketleri.Count(h => h.TenantId == k.TenantId);
        var yanitlar = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ =>
            f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId })));
        // 500 YOK: çakışan istek ya kalan 0 (409) ya sıra çakışması (409) alır.
        Assert.All(yanitlar, y => Assert.True(y.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict, $"beklenmeyen {(int)y.StatusCode}"));
        Assert.Equal(1, yanitlar.Count(y => y.StatusCode == HttpStatusCode.OK));
        // İlk ödemede sistem kategorisi de aynı anda açılır: kullanıcıya genel "kayıt zaten var" GİTMEZ.
        foreach (var y in yanitlar.Where(y => y.StatusCode == HttpStatusCode.Conflict))
        {
            var m = (await Govde(y)).GetProperty("message").ToString();
            Assert.True(m.Contains("kalan 0,00", StringComparison.Ordinal) || m.Contains("aynı anda", StringComparison.Ordinal), m);
        }
        using (var db = f.Vt.OlusturSistem())
        {
            var toplam = db.StopajOdemeleri.Where(o => o.TenantId == k.TenantId).Select(o => o.Tutar).ToList().Sum();
            Assert.Equal(1340m, toplam);
            Assert.Equal(kasaOnce + 1, db.KasaHareketleri.Count(h => h.TenantId == k.TenantId));
        }
        var d = Donem(await Govde(await f.Ortam.Istemci(k).GetAsync("/api/v1/stopaj/donemler?yil=2026")), "2026-08");
        Assert.Equal(0m, d.GetProperty("kalanTl").GetDecimal());
    }

    [Fact]
    public async Task Stopaj_fazla_odeme_kalani_EKSI_gosterir_sifira_kirpilmaz()
    {
        var (k, _) = await StopajSenaryosu("FAZLA");
        var c = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK,
            (await c.PostAsJsonAsync("/api/v1/stopaj/odemeler", new { donem = "2026-08", kasaId = k.KasaId })).StatusCode);
        // Ödemeden SONRA bir pusula iptal edildi → kesilen düşer, ödenen aynı → fazla ödeme görünür.
        using (var db = f.Vt.Olustur(k))
        {
            var p = db.GiderPusulalari.Where(g => g.TenantId == k.TenantId && !g.IptalEdildi && g.StopajTutar > 0m)
                .OrderBy(g => g.Id).First();
            var dusen = p.StopajTutar;
            Iptal(p);
            db.SaveChanges();
            var d = Donem(await Govde(await c.GetAsync("/api/v1/stopaj/donemler?yil=2026")), "2026-08");
            Assert.Equal(-dusen, d.GetProperty("kalanTl").GetDecimal());
        }
    }

    [Fact]
    public async Task K9_csv_satir_sayisi_iptal_edilmemis_stopajli_pusula_sayisi_okuma_izi_TEZGAHTAR_403()
    {
        var (k, _) = await StopajSenaryosu("K9");
        var r = await f.Ortam.Istemci(k).GetAsync("/api/v1/stopaj/donemler/2026-08/liste.csv");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var metin = (await r.Content.ReadAsStringAsync()).TrimStart('﻿');
        var satirlar = metin.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(1 + 4, satirlar.Length);   // başlık + 4 (iptal edilen yok, yenisi var)
        Assert.Contains("10000000146", metin);   // mali müşavir için TAM TCKN
        Assert.Contains("1340.00", (satirlar.Skip(1).Sum(s => decimal.Parse(s.Split(';')[6], System.Globalization.CultureInfo.InvariantCulture))).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

        using (var db = f.Vt.OlusturSistem())
            Assert.Contains(db.DenetimKayitlari.Where(d => d.TenantId == k.TenantId).ToList(),
                d => (d.Detay ?? "").Contains("stopaj listesi indirildi") && (d.Detay ?? "").Contains("4 satır"));

        var t = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/stopaj/donemler/2026-08/liste.csv");
        Assert.Equal(HttpStatusCode.Forbidden, t.StatusCode);
    }

    [Fact]
    public async Task Stopaj_ozeti_bu_ay_kesileni_ve_son_gunu_gecen_donemi_doner()
    {
        var (k, _) = await StopajSenaryosu("Ozet");
        var o = await Govde(await f.Ortam.Istemci(k).GetAsync("/api/v1/stopaj/ozet"));
        // 2026-08 dönemi bugün (≥ 2026-10-01) son günü (26.09) geçmiş ve ödenmemiş.
        Assert.Contains(o.GetProperty("gecikmis").EnumerateArray(), d => d.GetProperty("donem").GetString() == "2026-08");
    }

    [Fact]
    public async Task STOPAJ_HATIRLATMA_isi_son_gunu_gecen_donem_icin_KRITIK_bildirim_uretir_TEZGAHTAR_gormez()
    {
        var (k, _) = await StopajSenaryosu("Is");
        var kosum = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/isler/{Kuyumcu.Api.Modules.Isler.Isler.StopajHatirlatmaIsi.IsKodu}/calistir", new { });
        Assert.Equal(HttpStatusCode.OK, kosum.StatusCode);
        var j = await kosum.Json();
        Assert.True(j.TryGetProperty("uretilenBildirim", out var ub) && ub.GetInt32() >= 1, j.ToString());
        using (var db = f.Vt.OlusturSistem())
        {
            var b = db.Bildirimler.Where(x => x.TenantId == k.TenantId && x.Tur == "STOPAJ_HATIRLATMA").ToList();
            Assert.Contains(b, x => x.Onem == BildirimOnem.Kritik && x.IlgiliVarlikId == "2026-08");
        }
        // Görünürlük SAHİP/MUHASEBE: TEZGÂHTAR bu bildirimi görmez (süzme sunucuda).
        var tezgah = await (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/bildirimler")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("STOPAJ_HATIRLATMA", tezgah);
    }

    // ── Y4-C: KMTS ──────────────────────────────────────────────────────────

    [Fact]
    public async Task K14_KMTS_deseni_doluyken_uymayan_kod_400_urun_ve_satis_yolu_bosken_serbest()
    {
        var k = YeniKiraci("K14");
        var c = f.Ortam.Istemci(k);
        var yasal = f.Ortam.Services.GetRequiredService<YasalParametreServisi>();

        object Urun(string kod, string kmts) => new
        {
            tip = "ZIYNET", kod, ad = "KMTS test ürünü", ayarId = "K22", gramAgirlik = 5m, kmtsKodu = kmts,
        };

        // Desen BOŞ (KAPALI satır) → serbest.
        var serbest = await c.PostAsJsonAsync("/api/v1/urunler", Urun("KMTS-S1", "herhangi bir kod"));
        Assert.True(serbest.IsSuccessStatusCode, await serbest.Content.ReadAsStringAsync());

        using (var db = f.Vt.OlusturSistem())
            db.Database.ExecuteSqlRaw("UPDATE yasal_parametreler SET deger_metin = {0} WHERE anahtar = 'KMTS_KOD_DESENI'", "KM[0-9]{4}");
        yasal.Tazele();
        try
        {
            var kotu = await c.PostAsJsonAsync("/api/v1/urunler", Urun("KMTS-K1", "ABC"));
            Assert.Equal(HttpStatusCode.BadRequest, kotu.StatusCode);
            Assert.Contains("resmî biçime uymuyor", (await Govde(kotu)).GetProperty("message").ToString());
            var iyi = await c.PostAsJsonAsync("/api/v1/urunler", Urun("KMTS-I1", "KM1234"));
            Assert.True(iyi.IsSuccessStatusCode, await iyi.Content.ReadAsStringAsync());

            // Satış yolu (hurda alışı): AYNI yardımcı.
            var h = await c.PostAsJsonAsync("/api/v1/islemler/hurda-alis", new
            {
                cariId = k.CariId, ayarId = "K22", gramAgirlik = 1m, birimFiyatTlHasGram = 2800m,
                kasaId = k.KasaId, kurSnapshotId = k.KurId, odemeTuru = "NAKIT", kmtsKodu = "ABC",
                satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Hurda Satıcısı", tckn = "10000000146", adres = "Ulus" },
            });
            Assert.Equal(HttpStatusCode.BadRequest, h.StatusCode);
            Assert.Contains("resmî biçime uymuyor", (await Govde(h)).GetProperty("message").ToString());
        }
        finally
        {
            using var db = f.Vt.OlusturSistem();
            db.Database.ExecuteSqlRaw("UPDATE yasal_parametreler SET deger_metin = NULL WHERE anahtar = 'KMTS_KOD_DESENI'");
            yasal.Tazele();
        }
    }

    [Fact]
    public async Task KMTS_mevcut_kayit_taramasi_desen_bosken_KAPALI_doluyken_uymayani_listeler_silmez()
    {
        var k = YeniKiraci("KMTSTARA");
        var c = f.Ortam.Istemci(k);
        var yasal = f.Ortam.Services.GetRequiredService<YasalParametreServisi>();
        // Desen BOŞKEN serbest kodla kaydedilmiş ESKİ ürün.
        var eski = await c.PostAsJsonAsync("/api/v1/urunler", new
        {
            tip = "ZIYNET", kod = "KMTS-ESKI", ad = "Eski kodlu ürün", ayarId = "K22", gramAgirlik = 5m, kmtsKodu = "eski-serbest-kod",
        });
        Assert.True(eski.IsSuccessStatusCode, await eski.Content.ReadAsStringAsync());

        var a = await Govde(await c.GetAsync("/api/v1/kmts/uymayanlar"));
        Assert.False(a.GetProperty("bicimDenetimi").GetBoolean());
        Assert.Equal(0, a.GetProperty("sayi").GetInt32());

        using (var db = f.Vt.OlusturSistem())
            db.Database.ExecuteSqlRaw("UPDATE yasal_parametreler SET deger_metin = {0} WHERE anahtar = 'KMTS_KOD_DESENI'", "KM[0-9]{4}");
        yasal.Tazele();
        try
        {
            a = await Govde(await c.GetAsync("/api/v1/kmts/uymayanlar"));
            Assert.True(a.GetProperty("bicimDenetimi").GetBoolean());
            Assert.Equal(1, a.GetProperty("sayi").GetInt32());
            Assert.Equal("eski-serbest-kod", a.GetProperty("urunler")[0].GetProperty("kmtsKodu").GetString());
            Assert.Equal(1, (await Govde(await c.GetAsync("/api/v1/kmts/ayar"))).GetProperty("uymayanSayisi").GetInt32());
            using (var db = f.Vt.Olustur(k))
                Assert.Equal(1, db.Urunler.Count(x => x.TenantId == k.TenantId && x.KmtsKodu == "eski-serbest-kod"));   // silinmez
            Assert.Equal(HttpStatusCode.Forbidden,
                (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/kmts/uymayanlar")).StatusCode);
        }
        finally
        {
            using var db = f.Vt.OlusturSistem();
            db.Database.ExecuteSqlRaw("UPDATE yasal_parametreler SET deger_metin = NULL WHERE anahtar = 'KMTS_KOD_DESENI'");
            yasal.Tazele();
        }
    }

    [Fact]
    public async Task KMTS_ayari_varsayilan_kapali_SAHIP_acar_TEZGAHTAR_yazamaz()
    {
        var k = YeniKiraci("KmtsAyar");
        var c = f.Ortam.Istemci(k);
        Assert.False((await Govde(await c.GetAsync("/api/v1/kmts/ayar"))).GetProperty("acik").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Ortam.Istemci(k, "TEZGAHTAR").PutAsJsonAsync("/api/v1/kmts/ayar", new { acik = true })).StatusCode);
        var p = await c.PutAsJsonAsync("/api/v1/kmts/ayar", new { acik = true });
        Assert.True((await Govde(p)).GetProperty("acik").GetBoolean());
        Assert.True((await Govde(await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/kmts/ayar"))).GetProperty("acik").GetBoolean());
    }

    [Fact]
    public async Task Ozel_matrah_ayari_kuyum_fatura_varligini_soyler()
    {
        var k = YeniKiraci("OmAyar");
        var a = await Govde(await f.Ortam.Istemci(k).GetAsync("/api/v1/faturalar/ozel-matrah-ayari"));
        Assert.False(a.GetProperty("kuyumFaturaVar").GetBoolean());
    }
}
