using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET L-1b — TEVSİK AÇIĞI (ALIŞ): ÇOK KALEMLİ ALIŞ TEK FİŞ (GERÇEK HTTP BORU HATTI)
//
// Ekran çok kalemli ALIŞ'ı kalem başına ayrı POST /islemler/alis ile gönderiyordu:
// Peşin Müşteri'den iki parça alınırken toplam haddi aşan nakit ödeme iki kayda
// bölünüyor, tevsik kapısı fiş toplamını görmüyordu. Yeni uç /islemler/alis-fisi
// kapıları fişin BRÜT toplamıyla BİR KEZ çağırır; kalemler ortak fis_id taşır ve
// tek transaction'da ya hep ya hiç yazılır.
//
// ⚠ "Kural gevşetilmez/sıkılaştırılmaz": sınır testleri aynı toplamı TEK KALEMLİ
//   /islemler/alis'e de gönderir ve iki ucun AYNI sonucu verdiğini sınar.
// ⚠ Eşik testte SABİT DEĞİL: had sunucudan (/tevsik/gunluk) okunur (ADR-0005).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class L1bAlisFisiHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETL1b-TEST-" + ad);
    }

    private string IkinciUrun(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"K2-{id[..8]}", Ad = "Test kolye",
            AyarId = "K14", Milyem = Kuyumcu.Core.Domain.Ayarlar.Bul("K14")!.Milyem, GramAgirlik = 5m, TasAgirlik = 0m,
            HasGrami = Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(5m, 0m, "K14"),
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<string> PesinId(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/cari/sistem-carisi");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await Govde(c)).GetProperty("id").GetString()!;
    }

    private async Task<decimal> Had(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "MUHASEBE").GetAsync("/api/v1/tevsik/gunluk");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var had = (await Govde(c)).GetProperty("had").GetDecimal();
        Assert.True(had > 0m, "had sunucudan okunamadı");
        return had;
    }

    /// <summary>Ekranın L-1b gövdesiyle AYNI şekil (Islemler.razor AlisFisUcunaGider dalı).</summary>
    private static object Fis(Kiraci k, string? cariId, string odemeTuru, (string UrunId, decimal Birim)[] kalemler,
                              decimal? odenenTl = null, string? kasaId = "", bool? farkliKisi = null) => new
    {
        // PAKET YASAL-3 (§ 4.2): art arda Peşin işlemde "başka biri" beyanı (yalnız verilince).
        masak = farkliKisi == true ? new { ayniKisiBeyani = false } : null,
        cariId,
        kalemler = kalemler.Select(x => new { urunId = x.UrunId, adet = 1m, birimFiyatTl = x.Birim }),
        odemeTuru,
        kasaId = kasaId == "" ? k.KasaId : kasaId,
        odenenTl,
        kurSnapshotId = k.KurId,
    };

    private static object Tek(Kiraci k, string? cariId, string odemeTuru, decimal birim, bool? farkliKisi = null) => new
    {
        masak = farkliKisi == true ? new { ayniKisiBeyani = false } : null,   // PAKET YASAL-3 (§ 4.2)
        cariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = birim, odemeTuru, kasaId = k.KasaId, kurSnapshotId = k.KurId,
    };

    private async Task<HttpResponseMessage> FisGonder(Kiraci k, object govde, string rol = "MUHASEBE") =>
        await f.Ortam.Istemci(k, rol).PostAsJsonAsync("/api/v1/islemler/alis-fisi", govde);

    private async Task<HttpResponseMessage> TekGonder(Kiraci k, object govde, string rol = "MUHASEBE") =>
        await f.Ortam.Istemci(k, rol).PostAsJsonAsync("/api/v1/islemler/alis", govde);

    private List<Hareket> Alislar(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "ALIS").ToList();
    }

    private (decimal Stok, decimal Kasa, int Parti, int OdemeKalemi, int KasaHareketi) Durum(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return (
            db.Stoklar.AsNoTracking().Where(s => s.TenantId == k.TenantId).Sum(s => s.Adet),
            db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye,
            db.StokPartileri.AsNoTracking().Count(p => p.TenantId == k.TenantId),
            db.OdemeKalemleri.AsNoTracking().Count(p => p.TenantId == k.TenantId),
            db.KasaHareketleri.AsNoTracking().Count(p => p.TenantId == k.TenantId));
    }

    // ═══ KABUL — bölme kapandı: Peşin + 2 kalem NAKİT, toplam had üstü ════════

    [Fact]
    public async Task L1bH_01_Pesin_iki_kalem_NAKIT_toplam_had_ustu_400_tevsik_cariId_SQL_0_hareket_stok_kasa_degismez()
    {
        var k = YeniKiraci("pesin-nakit");
        var u2 = IkinciUrun(k);
        var pesin = await PesinId(k);
        var had = await Had(k);
        var a = Math.Round(had * 0.7m, 2); var b = Math.Round(had * 0.4m, 2);
        Assert.True(a < had && b < had && a + b > had);
        var once = Durum(k);

        var c = await FisGonder(k, Fis(k, pesin, "NAKIT", [(k.UrunId, a), (u2, b)]));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var j = await Govde(c);
        Assert.Contains("tevsik", j.GetProperty("message").GetString()!, StringComparison.OrdinalIgnoreCase);
        Assert.True(j.TryGetProperty("alanlar", out var e) && e.TryGetProperty("cariId", out _),
            "ret müşteri alanına bağlanmalı (ekran bandı): " + j);
        Assert.Empty(Alislar(k));
        Assert.Equal(once, Durum(k));   // stok · kasa · parti · ödeme kalemi · kasa hareketi AYNEN

        // Kanıt: aynı iki kalem ESKİ yoldan (kalem başına POST) ayrı ayrı geçiyordu — açığın kendisi.
        Assert.True((await TekGonder(k, Tek(k, pesin, "NAKIT", a))).IsSuccessStatusCode);
        Assert.True((await TekGonder(k, Tek(k, pesin, "NAKIT", b, farkliKisi: true))).IsSuccessStatusCode);   // YASAL-3: art arda → beyan
    }

    // ═══ SINIRLAR — çok kalemli fiş, tek kalemli alışla AYNI sonucu verir ═════

    [Theory]
    [InlineData(-0.01, true)]
    [InlineData(0.00, true)]
    [InlineData(0.01, false)]
    public async Task L1bH_02_Pesin_NAKIT_sinirlar_cok_kalemli_fis_TEK_kalemli_alisla_AYNI_sonuc(double sapma, bool gecmeli)
    {
        var k = YeniKiraci("sinir");
        var u2 = IkinciUrun(k);
        var pesin = await PesinId(k);
        var had = await Had(k);
        var toplam = had + (decimal)sapma;
        var a = Math.Round(toplam / 2m, 2); var b = toplam - a;

        var tek = await TekGonder(k, Tek(k, pesin, "NAKIT", toplam));
        var tekHareket = Alislar(k).Count;
        // YASAL-3 (§ 4.2): ikinci Peşin işlem art arda → "başka biri" beyanıyla (tek işlem kuralı kıyaslanıyor).
        var fis = await FisGonder(k, Fis(k, pesin, "NAKIT", [(k.UrunId, a), (u2, b)], farkliKisi: true));

        Assert.Equal(tek.StatusCode, fis.StatusCode);   // kural gevşemez, sıkılaşmaz
        Assert.Equal(gecmeli, fis.IsSuccessStatusCode);
        var yeni = Alislar(k).Count - tekHareket;
        if (gecmeli)
        {
            var j = await Govde(fis);
            var fisId = j.GetProperty("fisId").GetString();
            Assert.Equal(2, j.GetProperty("kalemSayisi").GetInt32());
            Assert.Equal(toplam, j.GetProperty("toplamTl").GetDecimal());
            var satirlar = Alislar(k).Where(h => h.FisId == fisId).ToList();
            Assert.Equal(2, satirlar.Count);
            Assert.Equal(-toplam, satirlar.Sum(s => s.TlTutari));
            Assert.Equal(toplam, satirlar.Sum(s => s.NakitTutar ?? 0m));
        }
        else Assert.Equal(0, yeni);
    }

    [Fact]
    public async Task L1bH_03_Pesin_KART_had_ustu_fis_TEK_kalemli_alisla_AYNI_sonuc_kural_degismedi()
    {
        // Tek kalemli alışta tevsik YALNIZ nakit ödemede çağrılır (kasadan çıkan nakit). Fiş bu kuralı
        // değiştirmez: KART'la ödenen alışta tek kalem ne dönüyorsa çok kalemli fiş de onu döner.
        var k = YeniKiraci("pesin-kart");
        var u2 = IkinciUrun(k);
        var pesin = await PesinId(k);
        var had = await Had(k);
        var tek = await TekGonder(k, Tek(k, pesin, "KART", had + 1m));
        var fis = await FisGonder(k, Fis(k, pesin, "KART", [(k.UrunId, had), (u2, 1m)]));
        Assert.Equal(tek.StatusCode, fis.StatusCode);
    }

    [Fact]
    public async Task L1bH_04_gercek_cari_had_ustu_HAVALE_200_TEK_fisId_NAKIT_400_islem_tutari_kurali()
    {
        var k = YeniKiraci("cari");
        var u2 = IkinciUrun(k);
        var had = await Had(k);
        var a = Math.Round(had * 0.6m, 2); var b = Math.Round(had * 0.6m, 2);

        var nakit = await FisGonder(k, Fis(k, k.CariId, "NAKIT", [(k.UrunId, a), (u2, b)]));
        Assert.Equal(HttpStatusCode.BadRequest, nakit.StatusCode);   // §4.1.2 fiş toplamıyla
        Assert.Empty(Alislar(k));

        var havale = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(k.UrunId, a), (u2, b)]));
        Assert.True(havale.IsSuccessStatusCode, $"{(int)havale.StatusCode}: {await havale.Content.ReadAsStringAsync()}");
        var j = await Govde(havale);
        var fisId = j.GetProperty("fisId").GetString();
        Assert.False(string.IsNullOrEmpty(fisId));
        var satirlar = Alislar(k);
        Assert.Equal(2, satirlar.Count);
        Assert.All(satirlar, s => Assert.Equal(fisId, s.FisId));
        Assert.All(satirlar, s => Assert.Equal("HAVALE", s.OdemeTuru));
        Assert.Contains(j.GetProperty("hareketId").GetString(), satirlar.Select(s => s.Id));   // yazdırma bağlantısı fişin bir kalemine
    }

    // ═══ YAZIM — kalem başına davranış tek kalemli alışla aynı ════════════════

    [Fact]
    public async Task L1bH_05_kalem_basina_stok_parti_kasa_hareketi_odeme_kalemi_ortak_fis_kismi_odeme_cariye_borc()
    {
        var k = YeniKiraci("yazim");
        var u2 = IkinciUrun(k);
        var once = Durum(k);
        // Gerçek cari, 12.000 + 8.000 = 20.000; 15.000 nakit ödendi → 5.000 tedarikçiye borç (had altı).
        // (Kısmi HAVALE/KART'ta ödeme kırılımı tek kalemli alışta da eksik yazılıyor — rapor § açık kalanlar.)
        var c = await FisGonder(k, Fis(k, k.CariId, "NAKIT", [(k.UrunId, 12_000m), (u2, 8_000m)], odenenTl: 15_000m));
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode}: {await c.Content.ReadAsStringAsync()}");
        var j = await Govde(c);
        Assert.Equal(15_000m, j.GetProperty("odenenTl").GetDecimal());
        Assert.Equal(5_000m, j.GetProperty("kalanTl").GetDecimal());
        var fisId = j.GetProperty("fisId").GetString()!;

        var sonra = Durum(k);
        Assert.Equal(once.Stok + 2m, sonra.Stok);
        Assert.Equal(once.Kasa - 15_000m, sonra.Kasa);
        Assert.Equal(once.Parti + 2, sonra.Parti);
        Assert.Equal(once.KasaHareketi + 2, sonra.KasaHareketi);   // kasa satırı KALEM başına (storno kalem başına doğru)

        using var db = f.Vt.OlusturSistem();
        var satirlar = db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.FisId == fisId).ToList();
        Assert.Equal(2, satirlar.Count);
        Assert.Equal(5_000m, satirlar.Sum(s => s.VeresiyeTutar ?? 0m));
        Assert.Equal(15_000m, satirlar.Sum(s => s.NakitTutar ?? 0m));   // tevsik kümülatifinin okuduğu pay
        var ids = satirlar.Select(s => s.Id).ToList();
        var kasaSatirlari = db.KasaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId && ids.Contains(x.HareketId!)).ToList();
        Assert.Equal(-15_000m, kasaSatirlari.Sum(x => x.Tutar));
        Assert.Equal(-5_000m, db.CariBakiyeler.AsNoTracking().Single(b => b.CariId == k.CariId).TlBakiye);
        var kalemler = db.OdemeKalemleri.AsNoTracking().Where(x => x.TenantId == k.TenantId && x.FisId == fisId).ToList();
        Assert.Equal(20_000m, kalemler.Sum(x => x.TutarTl));   // kırılım fiş toplamına eşit
        // Fiş okuma ucu fişi tek belge olarak döndürür.
        var fo = await f.Ortam.Istemci(k, "MUHASEBE").GetAsync($"/api/v1/islemler/fis/{fisId}");
        Assert.Equal(HttpStatusCode.OK, fo.StatusCode);
        Assert.Equal(2, (await Govde(fo)).GetProperty("kalemler").GetArrayLength());
    }

    // ═══ ATOMİKLİK — 2. kalem geçersizse 1. kalemden HİÇBİR iz kalmaz ════════

    [Fact]
    public async Task L1bH_06_ikinci_kalem_kasa_bakiyesine_takilirsa_ilk_kalemin_hareketi_stogu_partisi_YOK()
    {
        var k = YeniKiraci("atomik-kasa");
        var u2 = IkinciUrun(k);
        using (var db = f.Vt.OlusturSistem())
        {
            db.Kasalar.Single(x => x.Id == k.KasaId).Bakiye = 50_000m;
            db.SaveChanges();
        }
        var once = Durum(k);
        // 30.000 kasadan çıkar (1. kalem yazılır), 2. kalem 30.000 kasa bakiyesini aşar → yazım döngüsünde patlar.
        var c = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(k.UrunId, 30_000m), (u2, 30_000m)]));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("kasa", (await Govde(c)).GetProperty("message").GetString()!, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Alislar(k));
        Assert.Equal(once, Durum(k));
    }

    [Fact]
    public async Task L1bH_07_ikinci_kalem_BASKA_kiracinin_urunu_404_hic_bir_sey_yazilmaz_iki_kiracida()
    {
        var a = YeniKiraci("izolasyon-a");
        var b = YeniKiraci("izolasyon-b");
        var onceA = Durum(a); var onceB = Durum(b);
        var c = await FisGonder(a, Fis(a, a.CariId, "HAVALE", [(a.UrunId, 1_000m), (b.UrunId, 1_000m)]));
        Assert.Equal(HttpStatusCode.NotFound, c.StatusCode);
        Assert.Empty(Alislar(a)); Assert.Empty(Alislar(b));
        Assert.Equal(onceA, Durum(a)); Assert.Equal(onceB, Durum(b));

        // Başka kiracının carisi de kabul edilmez.
        var c2 = await FisGonder(a, Fis(a, b.CariId, "HAVALE", [(a.UrunId, 1_000m), (a.UrunId, 1_000m)]));
        Assert.False(c2.IsSuccessStatusCode);
        Assert.Empty(Alislar(a));
    }

    // ═══ YETKİ — tek kalemli alışla AYNI rol kümesi ═══════════════════════════

    [Fact]
    public async Task L1bH_08_TEZGAHTAR_403_yazim_yok_rol_kumesi_tek_kalemli_alisla_birebir()
    {
        var k = YeniKiraci("yetki");
        var u2 = IkinciUrun(k);
        var c = await FisGonder(k, Fis(k, k.CariId, "HAVALE", [(k.UrunId, 1_000m), (u2, 1_000m)]), rol: "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.Forbidden, c.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await TekGonder(k, Tek(k, k.CariId, "HAVALE", 1_000m), rol: "TEZGAHTAR")).StatusCode);
        Assert.Empty(Alislar(k));

        // Rol kümesi BİREBİR: her rol için iki uç aynı kararı verir (yetki filtresi gövde doğrulamasından ÖNCE
        // işler → boş gövde yetkili rolde 400, yetkisizde 403; hiçbir şey yazılmaz).
        foreach (var rol in new[] { "SAHIP", "MUDUR", "MUHASEBE", "TEZGAHTAR" })
        {
            var tek = await f.Ortam.Istemci(k, rol).PostAsJsonAsync("/api/v1/islemler/alis", new { });
            var fis = await f.Ortam.Istemci(k, rol).PostAsJsonAsync("/api/v1/islemler/alis-fisi", new { });
            Assert.True(tek.StatusCode == fis.StatusCode, $"{rol}: alis {(int)tek.StatusCode} ≠ alis-fisi {(int)fis.StatusCode}");
            Assert.Equal(rol == "TEZGAHTAR", fis.StatusCode == HttpStatusCode.Forbidden);
        }
        Assert.Empty(Alislar(k));
    }

    [Fact]
    public async Task L1bH_09_bos_kalem_ve_sifir_adet_400_Turkce_mesaj_yazim_yok()
    {
        var k = YeniKiraci("bos");
        var bos = await FisGonder(k, new { cariId = k.CariId, kalemler = Array.Empty<object>(), odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);
        // L-1b (canlı C14): alan altı ileti KALEM der — liste için "karakter" iletisi yanlış yönlendirir.
        var bosMetin = (await Govde(bos)).GetRawText();
        Assert.Contains("kalem", bosMetin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("karakter", bosMetin, StringComparison.OrdinalIgnoreCase);
        var sifir = await FisGonder(k, new
        {
            cariId = k.CariId, odemeTuru = "HAVALE", kasaId = k.KasaId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 10m }, new { urunId = k.UrunId, adet = 0m, birimFiyatTl = 10m } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, sifir.StatusCode);
        Assert.Empty(Alislar(k));
    }
}
