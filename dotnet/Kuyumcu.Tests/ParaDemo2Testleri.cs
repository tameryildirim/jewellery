using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO-2 — tasarım hakemi t2 + PARA-DEMO açık kalanları (HTTP + bağımsız DB okuması)
//
//   (1) VERİ: tek bacaklı satış fişi hareketler.odeme_turu = o bacağın aracı (KARMA yalnız ≥ 2 farklı araç);
//       gerçek karmada ekstre bacak türlerini yazar ("Kart + Havale").
//   (3) PARA: nakit çıkış kapısı kasanın NAKİT payına bakar (hurda alış · iade · elle çıkış · NAKİT virman).
//       Kabul: nakit 1000 + kart 500 iken 1200 nakit çıkışı 400; 1000 nakit çıkışı 200 (sınır).
//   (4) GET /kasalar/nakit-ayrimi — kasa kartı alt satırı.
//   (5) Gün sonu: toplam tahsilat virman girişini ve açılış devrini DIŞARIDA bırakır, ayrı satırda döner;
//       uyarılarda "TL" yok, "₺".
//   (9) Kâğıt ekstre: has etkisi olmayan satırda has hücresi boş.
// Beklenen değerler SABİT ve elle hesaplanmıştır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class ParaDemo2Testleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, ad), 2_000m);
    }

    /// <summary>Boş (0 bakiyeli) yeni TL kasası.</summary>
    private static async Task<string> KasaAc(HttpClient c, Kiraci k, string ad)
    {
        var r = await c.PostJson("/api/v1/kasalar", new { ad, subeId = k.SubeId, paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Json()).GetProperty("id").GetString()!;
    }

    private static async Task Giris(HttpClient c, string kasaId, string arac, decimal tutar)
    {
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasaId}/hareket", new
        {
            yon = "GIRIS", tutar, odemeTuru = arac, aciklama = "PARA-DEMO-2 test girişi",
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    /// <summary>Kabul senaryosunun kasası: nakit 1000 + kart 500 (toplam 1500).</summary>
    private static async Task<string> Nakit1000Kart500(HttpClient c, Kiraci k)
    {
        var kasa = await KasaAc(c, k, "PD2 kasa");
        await Giris(c, kasa, "NAKIT", 1_000m);
        await Giris(c, kasa, "KART", 500m);
        return kasa;
    }

    private async Task<decimal> KasaBakiyesi(string kasaId)
    {
        using var db = f.Vt.OlusturSistem();
        return (await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasaId)).Bakiye;
    }

    private static object HurdaGovde(Kiraci k, string kasaId, decimal fiyat) => new
    {
        cariId = k.CariId, ayarId = "K22", gramAgirlik = 1m, birimFiyatTlHasGram = fiyat,
        kasaId, kurSnapshotId = k.KurId, odemeTuru = "NAKIT",
        satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Hurda Satıcısı", tckn = "10000000146", adres = "Ulus" },
    };

    private static async Task<string> FisSatis(HttpClient c, Kiraci k, string kasaId, params (string Arac, decimal Tutar)[] bacaklar)
    {
        var r = await c.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = bacaklar.Sum(b => b.Tutar) } },
            odemeler = bacaklar.Select(b => new { arac = b.Arac, tutarTl = b.Tutar, kasaId }).ToArray(),
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Json()).GetProperty("fisId").GetString()!;
    }

    private async Task<List<string?>> FisEtiketleri(string fisId)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.Hareketler.AsNoTracking().Where(h => h.FisId == fisId).Select(h => h.OdemeTuru).ToListAsync();
    }

    // ═══ (1) tek bacak = o bacağın aracı ═════════════════════════════════════

    [Fact]
    public async Task PD2_1_tek_bacak_HAVALE_fisi_hareket_HAVALE_ekstre_Havale_gun_sonu_havale()
    {
        var k = YeniKiraci("PD2-1 havale");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var fis = await FisSatis(c, k, k.KasaId, ("HAVALE", 10_000m));

        Assert.Equal(["HAVALE"], await FisEtiketleri(fis));   // eskiden "KARMA"

        var ekstre = await (await c.GetAsync($"/api/v1/cari/{k.CariId}/ekstre")).Json();
        var satir = ekstre.GetProperty("hareketler").EnumerateArray().First(h => h.GetProperty("fisId").GetString() == fis);
        Assert.Equal("HAVALE", satir.GetProperty("odemeTuru").GetString());
        Assert.Equal(JsonValueKind.Null, satir.GetProperty("odemeAraclari").ValueKind);

        var kagit = await (await c.GetAsync($"/api/v1/cikti/{BelgeTurleri.CariEkstre}/{k.CariId}")).Json();
        var hucreler = kagit.GetProperty("bolumler")[0].GetProperty("satirlar").EnumerateArray()
            .Select(s => s.GetProperty("hucreler").EnumerateArray().Select(x => x.GetString()!).ToArray()).ToList();
        Assert.Contains(hucreler, h => h[1] == "Satış · Havale");
        Assert.DoesNotContain(hucreler, h => h[1].Contains("Karma"));

        var gun = Zaman.YerelBugun().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var m = await (await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}")).Json();
        Assert.Equal(10_000m, m.GetProperty("tahsilat").GetProperty("havale").GetDecimal());
    }

    [Fact]
    public async Task PD2_1_tek_bacak_KART_ve_iki_KART_bacagi_KART_etiketi()
    {
        var k = YeniKiraci("PD2-1 kart");
        var c = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(["KART"], await FisEtiketleri(await FisSatis(c, k, k.KasaId, ("KART", 5_000m))));
        // Aynı araç iki satırda (iki ayrı kart) karışık DEĞİLDİR.
        Assert.Equal(["KART"], await FisEtiketleri(await FisSatis(c, k, k.KasaId, ("KART", 3_000m), ("KART", 2_000m))));
    }

    [Fact]
    public async Task PD2_1_gercek_karma_KARMA_kalir_ekstre_Kart_arti_Havale_yazar()
    {
        var k = YeniKiraci("PD2-1 karma");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var fis = await FisSatis(c, k, k.KasaId, ("KART", 6_000m), ("HAVALE", 4_000m));
        Assert.Equal(["KARMA"], await FisEtiketleri(fis));

        var ekstre = await (await c.GetAsync($"/api/v1/cari/{k.CariId}/ekstre")).Json();
        var satir = ekstre.GetProperty("hareketler").EnumerateArray().First(h => h.GetProperty("fisId").GetString() == fis);
        Assert.Equal(["KART", "HAVALE"], satir.GetProperty("odemeAraclari").EnumerateArray().Select(x => x.GetString()!).ToArray());

        var kagit = await (await c.GetAsync($"/api/v1/cikti/{BelgeTurleri.CariEkstre}/{k.CariId}")).Json();
        var islemler = kagit.GetProperty("bolumler")[0].GetProperty("satirlar").EnumerateArray()
            .Select(s => s.GetProperty("hucreler")[1].GetString()!).ToList();
        Assert.Contains("Satış · Kart + Havale", islemler);
    }

    // ═══ (3) nakit çıkış kapısı ══════════════════════════════════════════════

    [Fact]
    public async Task PD2_3_nakit_1000_kart_500_iken_1200_nakit_ELLE_CIKIS_400_hicbir_sey_yazilmaz()
    {
        var k = YeniKiraci("PD2-3 elle");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 1_200m, odemeTuru = "NAKIT", aciklama = "nakit çıkış sınırı",
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var govde = await r.Content.ReadAsStringAsync();
        Assert.Contains("nakit yetersiz", govde);
        // Tur 3 (UX-S32 / ölçer K5): hata bandındaki tutar ekran biçiminde — "1000.00 TRY" kalıbı yok.
        var mesaj = System.Text.Json.JsonDocument.Parse(govde).RootElement.GetProperty("message").GetString()!;
        Assert.Contains("1.000,00\u00a0\u20ba nakit var", mesaj);
        Assert.Contains("1.200,00\u00a0\u20ba nakit isteniyor", mesaj);
        Assert.Contains("kart (500,00\u00a0\u20ba)", mesaj);
        Assert.DoesNotContain("TRY", mesaj);
        Assert.Equal(1_500m, await KasaBakiyesi(kasa));   // toplam yetiyordu; eskiden 200 dönerdi
    }

    [Fact]
    public async Task PD2_3_sinir_tam_nakit_kadar_cikis_200_bir_kurus_fazlasi_400()
    {
        var k = YeniKiraci("PD2-3 sınır");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var fazla = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 1_000.01m, odemeTuru = "NAKIT", aciklama = "bir kuruş fazla",
        });
        Assert.Equal(HttpStatusCode.BadRequest, fazla.StatusCode);
        var tam = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 1_000m, odemeTuru = "NAKIT", aciklama = "tam nakit",
        });
        Assert.True(tam.IsSuccessStatusCode, await tam.Content.ReadAsStringAsync());
        Assert.Equal(500m, await KasaBakiyesi(kasa));
    }

    [Fact]
    public async Task PD2_3_kart_cikisi_KART_PAYINI_asamaz_1200_400_500_gecer_havale_payi_0()
    {
        // Tur 5 (doğrulayıcı S16): KART/HAVALE çıkışı kendi aracının payına bakar — nakit 1000 + kart 500
        // iken 1200 KART çıkışı geçseydi kart payı −700, nakit defteri 1000 olur, çekmecede 300 varken
        // gün sonu 700 sahte açık verirdi.
        var k = YeniKiraci("PD2-3 kart çıkış");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var asan = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 1_200m, odemeTuru = "KART", aciklama = "kart iadesi",
        });
        Assert.Equal(HttpStatusCode.BadRequest, asan.StatusCode);
        var govde = System.Text.Json.JsonDocument.Parse(await asan.Content.ReadAsStringAsync())
            .RootElement.GetProperty("message").GetString()!;
        Assert.Contains("kart tahsilatı 500,00", govde);
        Assert.Contains("1.200,00", govde);
        Assert.DoesNotContain("TRY", govde);
        Assert.Equal(1_500m, await KasaBakiyesi(kasa));
        var havale = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 1m, odemeTuru = "HAVALE", aciklama = "havale payı 0",
        });
        Assert.Equal(HttpStatusCode.BadRequest, havale.StatusCode);
        var sinir = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 500m, odemeTuru = "KART", aciklama = "tam kart payı",
        });
        Assert.True(sinir.IsSuccessStatusCode, await sinir.Content.ReadAsStringAsync());
        Assert.Equal(1_000m, await KasaBakiyesi(kasa));
        var kartBitti = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 0.01m, odemeTuru = "KART", aciklama = "kart payı bitti",
        });
        Assert.Equal(HttpStatusCode.BadRequest, kartBitti.StatusCode);
        // Nakit payı dokunulmadı: 1000 nakit çıkışı hâlâ geçer.
        var nakit = await c.PostAsJsonAsync($"/api/v1/kasalar/{kasa}/hareket", new
        {
            yon = "CIKIS", tutar = 1_000m, odemeTuru = "NAKIT", aciklama = "tam nakit",
        });
        Assert.True(nakit.IsSuccessStatusCode, await nakit.Content.ReadAsStringAsync());
        Assert.Equal(0m, await KasaBakiyesi(kasa));
    }

    [Fact]
    public async Task PD2_3_nakit_1000_kart_500_iken_1099_TL_nakit_HURDA_ALISI_400()
    {
        var k = YeniKiraci("PD2-3 hurda");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        // 1 g 22 ayar × 1.200 ₺/has = 1.099,20 ₺ > nakit 1.000 (toplam 1.500 yeterdi).
        var r = await c.PostAsJsonAsync("/api/v1/islemler/hurda-alis", HurdaGovde(k, kasa, 1_200m));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("nakit yetersiz", await r.Content.ReadAsStringAsync());
        Assert.Equal(1_500m, await KasaBakiyesi(kasa));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.Hareketler.AsNoTracking().CountAsync(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS"));
    }

    [Fact]
    public async Task PD2_3_hurda_alisi_nakit_payi_icinde_200()
    {
        var k = YeniKiraci("PD2-3 hurda sınır içi");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var r = await c.PostAsJsonAsync("/api/v1/islemler/hurda-alis", HurdaGovde(k, kasa, 1_000m));   // 916,60 ₺
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PD2_3_kartli_satisin_NAKIT_IADESI_nakit_payini_asarsa_400()
    {
        var k = YeniKiraci("PD2-3 iade");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var s = await c.PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1, birimFiyatTl = 1_200m,
            odemeTuru = "KART", kasaId = kasa, kurSnapshotId = k.KurId,
        });
        Assert.True(s.IsSuccessStatusCode, await s.Content.ReadAsStringAsync());
        string satisId;
        using (var db = f.Vt.OlusturSistem())
            satisId = (await db.Hareketler.AsNoTracking().SingleAsync(h => h.TenantId == k.TenantId && h.Tur == "SATIS")).Id;
        // kasa: nakit 1.000 · kart 1.700 → 1.200 NAKİT iade çekmecede yok.
        var r = await c.PostAsJsonAsync("/api/v1/islemler/satis-iade", new
        {
            kaynakHareketId = satisId, adet = 1m, odemeTuru = "NAKIT", kasaId = kasa, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("nakit yetersiz", await r.Content.ReadAsStringAsync());
        Assert.Equal(2_700m, await KasaBakiyesi(kasa));
    }

    /// <summary>
    /// Tur 4 (doğrulayıcı bulgusu S10): gider NAKİT ödenir; nakit 1000 + kart 500 iken 1200 gider 400,
    /// hiçbir şey yazılmaz; tam nakit kadar (1000) gider 200. Eskiden toplam kapısına bakıp 200 dönüyor,
    /// nakit payı −200'e düşüyordu.
    /// </summary>
    [Fact]
    public async Task PD2_3_nakit_1000_kart_500_iken_1200_NAKIT_GIDER_400_tam_nakit_200()
    {
        var k = YeniKiraci("PD2-3 gider");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var kat = await c.PostJson("/api/v1/gider-kategorileri", new { ad = "PD2 kira" });
        Assert.Equal(HttpStatusCode.OK, kat.StatusCode);
        var katId = (await kat.Json()).GetProperty("id").GetString()!;

        var r = await c.PostJson("/api/v1/giderler", new { kasaId = kasa, giderKategoriId = katId, tutar = 1_200m });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var mesaj = (await r.Json()).GetProperty("message").GetString()!;
        Assert.Contains("nakit yetersiz", mesaj);
        Assert.Contains("1.000,00\u00a0\u20ba nakit var", mesaj);
        Assert.Equal(1_500m, await KasaBakiyesi(kasa));
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0, await db.KasaHareketleri.AsNoTracking()
                .CountAsync(x => x.KasaId == kasa && x.GiderKategoriId != null));

        var tam = await c.PostJson("/api/v1/giderler", new { kasaId = kasa, giderKategoriId = katId, tutar = 1_000m });
        Assert.Equal(HttpStatusCode.OK, tam.StatusCode);
        Assert.Equal(500m, await KasaBakiyesi(kasa));
    }

    [Fact]
    public async Task PD2_3_NAKIT_virman_nakit_payini_asarsa_400_KART_virman_kendi_payindan_200()
    {
        var k = YeniKiraci("PD2-3 virman");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        var hedef = await KasaAc(c, k, "PD2 hedef kasa");
        var nakit = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kasa, hedefTur = "KASA", hedefId = hedef, tutar = 1_200m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, nakit.StatusCode);
        Assert.Contains("nakit yetersiz", await nakit.Content.ReadAsStringAsync());
        // Tur 4 (UX-S32): kart payı aşımı mesajı ekran biçiminde — "500.00 TRY" kalıbı yok.
        var kartFazla = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kasa, hedefTur = "KASA", hedefId = hedef, tutar = 600m, kasaAraci = "KART",
        });
        Assert.Equal(HttpStatusCode.BadRequest, kartFazla.StatusCode);
        var kartMesaj = (await kartFazla.Json()).GetProperty("message").GetString()!;
        Assert.Contains("500,00\u00a0\u20ba; 600,00\u00a0\u20ba aktarılamaz", kartMesaj);
        Assert.DoesNotContain("TRY", kartMesaj);
        var kart = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = kasa, hedefTur = "KASA", hedefId = hedef, tutar = 500m, kasaAraci = "KART",
        });
        Assert.True(kart.IsSuccessStatusCode, await kart.Content.ReadAsStringAsync());
        Assert.Equal(1_000m, await KasaBakiyesi(kasa));
    }

    [Fact]
    public async Task PD2_3_ayrim_ayni_istekte_EKLENEN_kart_satirini_sayar_kaydedilmis_satiri_iki_kez_saymaz()
    {
        var k = YeniKiraci("PD2-3 bekleyen");
        using var db = f.Vt.OlusturSistem();
        // Kaydedilmiş KART 200 (DB toplamında) + aynı istekte eklenmiş, henüz kaydedilmemiş KART 500.
        db.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId, Tutar = 200m, ParaBirimi = "TRY",
            OdemeTuru = "KART", Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        db.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId, Tutar = 500m, ParaBirimi = "TRY",
            OdemeTuru = "KART", Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
        });
        var a = await Kuyumcu.Api.Modules.Kasa.KasaNakitAyrimi.Hesapla(db, k.TenantId, k.KasaId, TestKurulum.KasaAcilis + 700m);
        Assert.Equal(700m, a.Kart);
        Assert.Equal(TestKurulum.KasaAcilis, a.Nakit);
    }

    // ═══ (4) GET /kasalar/nakit-ayrimi ═══════════════════════════════════════

    [Fact]
    public async Task PD2_4_nakit_ayrimi_ucu_kasa_basina_nakit_kart_havale_doner()
    {
        var k = YeniKiraci("PD2-4 ayrım");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa = await Nakit1000Kart500(c, k);
        await Giris(c, kasa, "HAVALE", 300m);
        var r = await c.GetAsync("/api/v1/kasalar/nakit-ayrimi");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var satir = (await r.Json()).EnumerateArray().Single(x => x.GetProperty("kasaId").GetString() == kasa);
        Assert.Equal(1_800m, satir.GetProperty("toplam").GetDecimal());
        Assert.Equal(1_000m, satir.GetProperty("nakit").GetDecimal());
        Assert.Equal(500m, satir.GetProperty("kart").GetDecimal());
        Assert.Equal(300m, satir.GetProperty("havale").GetDecimal());
    }

    // ═══ (5) gün sonu tahsilat tanımı ════════════════════════════════════════

    [Fact]
    public async Task PD2_5_gun_sonu_toplam_tahsilat_virman_ve_acilis_devrini_DISARIDA_birakir()
    {
        var k = YeniKiraci("PD2-5 gün sonu");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var kasa2 = await KasaAc(c, k, "PD2 ikinci kasa");
        await Giris(c, k.KasaId, "NAKIT", 100m);   // gerçek tahsilat
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        {
            kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "KASA", hedefId = kasa2, tutar = 300m,
        });
        Assert.True(v.IsSuccessStatusCode, await v.Content.ReadAsStringAsync());
        // Açılış devri: GERÇEK kurulum ucu (AcilisServisi) — tur 4: ayırt etme ödeme türüyle değil,
        // ACILIS hareketine bağlı kasa bacağıyla yapılır.
        await AcilisDevri(c, k, kasa2, 700m);

        var gun = Zaman.YerelBugun().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var r = await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var t = (await r.Json()).GetProperty("tahsilat");
        Assert.Equal(100m, t.GetProperty("toplam").GetDecimal());     // eskiden 100 + 300 + 700 = 1.100
        Assert.Equal(100m, t.GetProperty("nakit").GetDecimal());
        Assert.Equal(0m, t.GetProperty("diger").GetDecimal());
        Assert.Equal(300m, t.GetProperty("virmanGiris").GetDecimal());
        Assert.Equal(700m, t.GetProperty("acilisDevri").GetDecimal());
        // Tur 4: tanım cümlesi API'den GELMEZ (istemci sözlüğü TR+EN) — API yalnız sayı döner.
        Assert.False(t.TryGetProperty("tanim", out _));
    }

    /// <summary>Kurulum açılış ucu: prova → uygula (SAHIP), kasa satırı.</summary>
    private static async Task AcilisDevri(HttpClient c, Kiraci k, string kasaId, decimal tl)
    {
        var satirlar = new[] { new { hedefId = kasaId, tlTutar = tl, hasGram = 0m } };
        var p = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova", new { tur = "KASA", satirlar });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        var jeton = (await p.Json()).GetProperty("provaJetonu").GetString()!;
        var u = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula", new
        {
            tur = "KASA", satirlar, provaJetonu = jeton,
            gerekce = "Eski programdan devreden acilis bakiyesi", kurSnapshotId = k.KurId,
        });
        Assert.True(u.IsSuccessStatusCode, await u.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Tur 4 (doğrulayıcı bulgusu S11): kasaya ELLE girilen DIGER tutarı açılış devri DEĞİLDİR —
    /// tahsilatın "diğer" kaleminde ve toplamda kalır; açılış devri satırı 0. Aynı gün gerçek açılış
    /// devri de varsa yalnız o ayrılır.
    /// </summary>
    [Fact]
    public async Task PD2_5_elle_DIGER_girisi_acilis_devri_DEGIL_diger_kaleminde_kalir()
    {
        var k = YeniKiraci("PD2-5 elle diğer");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Giris(c, k.KasaId, "DIGER", 250m);
        var gun = Zaman.YerelBugun().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var t = (await (await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}")).Json())
            .GetProperty("tahsilat");
        Assert.Equal(0m, t.GetProperty("acilisDevri").GetDecimal());
        Assert.Equal(250m, t.GetProperty("diger").GetDecimal());
        Assert.Equal(250m, t.GetProperty("toplam").GetDecimal());

        var kasa2 = await KasaAc(c, k, "PD2 açılış kasası");
        await AcilisDevri(c, k, kasa2, 400m);
        t = (await (await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}")).Json())
            .GetProperty("tahsilat");
        Assert.Equal(400m, t.GetProperty("acilisDevri").GetDecimal());
        Assert.Equal(250m, t.GetProperty("diger").GetDecimal());
        Assert.Equal(250m, t.GetProperty("toplam").GetDecimal());
    }

    [Fact]
    public async Task PD2_5_gun_sonu_uyarilarinda_TL_kodu_YOK_tutar_TL_isaretli()
    {
        var k = YeniKiraci("PD2-5 uyarı");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var s = await c.PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1, birimFiyatTl = 1_234.5m,
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.True(s.IsSuccessStatusCode, await s.Content.ReadAsStringAsync());
        var gun = Zaman.YerelBugun().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var uyarilar = (await (await c.GetAsync($"/api/v1/raporlar/pos-fatura-mutabakati?tarih={gun}")).Json())
            .GetProperty("uyarilar").EnumerateArray().Select(x => x.GetString()!).ToList();
        var faturasiz = Assert.Single(uyarilar, u => u.Contains("faturalandırılmamış"));
        Assert.Contains("1.234,50 ₺", faturasiz);
        Assert.DoesNotContain(uyarilar, u => u.EndsWith(" TL)") || u.Contains(" TL "));
    }

    // ═══ saf: etiket kuralı ══════════════════════════════════════════════════

    [Fact]
    public void PD2_saf_fis_etiketi_tek_arac_kendisi_iki_farkli_arac_KARMA()
    {
        static Kuyumcu.Api.Modules.Satis.OdemeBacagiDto B(string a) => new() { Arac = a, TutarTl = 1m };
        Assert.Equal("HAVALE", Kuyumcu.Api.Modules.Satis.OdemeSepeti.FisEtiketi([B("HAVALE")]));
        Assert.Equal("ALTIN", Kuyumcu.Api.Modules.Satis.OdemeSepeti.FisEtiketi([B("ALTIN")]));
        Assert.Equal("KART", Kuyumcu.Api.Modules.Satis.OdemeSepeti.FisEtiketi([B("KART"), B("KART")]));
        Assert.Equal("KARMA", Kuyumcu.Api.Modules.Satis.OdemeSepeti.FisEtiketi([B("NAKIT"), B("KART")]));
    }

    [Fact]
    public void PD2_sozluk_TR_ve_EN_dolu_ve_farkli()
    {
        var tr = typeof(Kuyumcu.Core.I18n.ParaDemo2Metinleri).GetProperties();
        foreach (var p in tr)
        {
            var t = (string)p.GetValue(Kuyumcu.Core.I18n.ParaDemo2Sozlugu.Tr)!;
            var e = (string)p.GetValue(Kuyumcu.Core.I18n.ParaDemo2Sozlugu.En)!;
            Assert.False(string.IsNullOrWhiteSpace(t), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(e), p.Name);
            Assert.NotEqual(t, e);
        }
    }
}
