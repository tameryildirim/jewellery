using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatinalma = Kuyumcu.Api.Modules.Satinalma;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EKSIK-B1-SATINALMA — Satın alma (B1-1) + İrsaliye (B1-2) uçları: akış, hasım, kâğıt.
//
// Sınanan iş kuralları (her biri hatayı yeniden üreten test; mutasyon kodları raporda):
//   EB01 sipariş → onay → KISMİ kabul → TAM kabul: stok, irsaliye, kalem→hareket bağı, durum (SQL)
//   EB02 irsaliye kâğıt dökümü: fiyat YOK, miktar tr-TR, yasal uyarı, yön adı, sipariş dayanağı
//   EB03 hasım mal kabul: fazla / eksi / sıfır / ADET'e kesir / aynı kalem iki kez → 400, 0 satır
//   EB04 hasım durum: TASLAK ve KAPANDI siparişe kabul 409, 0 satır
//   EB05 hasım kiracı: başka kiracının siparişi / irsaliyesi / belgesi 404
//   EB06 hasım sipariş formu: ADET'e kesir, geçmiş beklenen tarih, aşırı uzun açıklama → 400, sipariş DOĞMAZ
//   EB07 hasım sevk irsaliyesi: ileri tarih, ADET'e kesir, aşırı uzun → 400, irsaliye DOĞMAZ
//   EB08 TEZGÂHTAR: satınalma/irsaliye uçları ve irsaliye belgesi 403
//   EB09 ÇİFT KABUL (eşzamanlı): kilit beklerken başka kabul kalanı bitirirse 409, stok bir kez
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class EksikB1SatinalmaHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "PAKETEKSIK-B1-SATINALMA-TEST " + ad);
        db.SaveChanges();
        return k;
    }

    private static async Task<JsonElement> Tamam(HttpResponseMessage r)
    {
        var govde = await r.Metin();
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode}: {govde}");
        return JsonDocument.Parse(govde).RootElement;
    }

    private static async Task<string> SiparisAc(HttpClient c, Kiraci k, decimal adet = 5m, decimal fiyat = 1_000m)
    {
        var y = await Tamam(await c.PostJson("/api/v1/satinalma", new
        {
            tedarikciCariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet, birimFiyatTl = fiyat } },
        }));
        Assert.Equal("TASLAK", y.GetProperty("durum").GetString());
        return y.GetProperty("id").GetString()!;
    }

    private static async Task<string> KalemId(HttpClient c, string siparisId) =>
        (await Tamam(await c.GetAsync($"/api/v1/satinalma/{siparisId}")))
            .GetProperty("kalemler")[0].GetProperty("id").GetString()!;

    private (decimal Stok, int Irsaliye, int IrsKalem, int Alis, decimal Kabul) Sayim(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return (
            db.Stoklar.AsNoTracking().Where(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Select(s => s.Adet).FirstOrDefault(),
            db.Irsaliyeler.AsNoTracking().Count(i => i.TenantId == k.TenantId),
            db.IrsaliyeKalemleri.AsNoTracking().Count(i => i.TenantId == k.TenantId),
            db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == "ALIS"),
            db.SatinalmaKalemleri.AsNoTracking().Where(x => x.TenantId == k.TenantId).Sum(x => (decimal?)x.KabulEdilenAdet) ?? 0m);
    }

    // ── EB01 ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task EB01_siparis_onay_kismi_ve_tam_kabul_stok_irsaliye_hareket_bagi_ve_durum()
    {
        var k = Kur("EB01");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var id = await SiparisAc(c, k, adet: 5m, fiyat: 2_000m);
        var kalem = await KalemId(c, id);
        var once = Sayim(k);

        var onay = await Tamam(await c.PostAsync($"/api/v1/satinalma/{id}/onayla", null));
        Assert.Equal("ONAYLANDI", onay.GetProperty("durum").GetString());
        Assert.True(onay.GetProperty("malKabulEdilebilir").GetBoolean());
        // Ekranın tedarikçi/ürün adı sunucudan gelir (UX-S21).
        Assert.False(string.IsNullOrWhiteSpace(onay.GetProperty("tedarikciAdi").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(onay.GetProperty("kalemler")[0].GetProperty("urunAdi").GetString()));
        Assert.Equal("ADET", onay.GetProperty("kalemler")[0].GetProperty("birim").GetString());

        // KISMİ kabul: 2 / 5
        var kismi = await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 2m } } }));
        Assert.Equal("KISMI_KABUL", kismi.GetProperty("yeniDurum").GetString());
        Assert.StartsWith("IRS-", kismi.GetProperty("irsaliyeNo").GetString());
        var irs1 = kismi.GetProperty("irsaliyeId").GetString()!;
        var ara = Sayim(k);
        Assert.Equal(once.Stok + 2m, ara.Stok);
        Assert.Equal(once.Irsaliye + 1, ara.Irsaliye);
        Assert.Equal(once.Alis + 1, ara.Alis);
        Assert.Equal(2m, ara.Kabul);

        // TAM kabul: kalan 3
        var tam = await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 3m } } }));
        Assert.Equal("KAPANDI", tam.GetProperty("yeniDurum").GetString());
        var son = Sayim(k);
        Assert.Equal(once.Stok + 5m, son.Stok);
        Assert.Equal(once.Irsaliye + 2, son.Irsaliye);
        Assert.Equal(once.Alis + 2, son.Alis);
        Assert.Equal(5m, son.Kabul);

        using var db = f.Vt.OlusturSistem();
        // Her irsaliye kalemi doğan ALIŞ hareketine bağlı (bağsız kalem YOK).
        var kalemler = await db.IrsaliyeKalemleri.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToListAsync();
        Assert.All(kalemler, x => Assert.NotNull(x.HareketId));
        var irs = await db.Irsaliyeler.AsNoTracking().FirstAsync(i => i.Id == irs1);
        Assert.Equal(IrsaliyeYonu.Gelen, irs.Yon);
        Assert.Equal(id, irs.SatinalmaSiparisId);

        // Liste: irsaliye cari adı ile döner, bağsız sayısı 2.
        var liste = await Tamam(await c.GetAsync("/api/v1/irsaliyeler?limit=200"));
        Assert.Equal(2, liste.GetProperty("bagsizToplam").GetInt32());
        Assert.All(liste.GetProperty("satirlar").EnumerateArray(),
            s => Assert.False(string.IsNullOrWhiteSpace(s.GetProperty("cariAdi").GetString())));
    }

    // ── EB02 ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task EB02_irsaliye_kagidi_fiyatsiz_miktarli_yasal_uyarili_ve_dayanakli()
    {
        var k = Kur("EB02");
        var c = f.Ortam.Istemci(k, "MUDUR");
        var id = await SiparisAc(c, k, adet: 4m, fiyat: 12_345.67m);
        var kalem = await KalemId(c, id);
        await Tamam(await c.PostAsync($"/api/v1/satinalma/{id}/onayla", null));
        var kabul = await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 3m } } }));
        var irsId = kabul.GetProperty("irsaliyeId").GetString()!;
        var siparisNo = kabul.GetProperty("siparis").GetProperty("no").GetString()!;

        var b = await Tamam(await c.GetAsync($"/api/v1/cikti/{BelgeTurleri.Irsaliye}/{irsId}"));
        Assert.Equal(BelgeTurleri.Irsaliye, b.GetProperty("tur").GetString());
        Assert.Equal("MAL KABUL İRSALİYESİ", b.GetProperty("belgeAdi").GetString());
        Assert.Equal(kabul.GetProperty("irsaliyeNo").GetString(), b.GetProperty("belgeNo").GetString());
        Assert.Contains(siparisNo, b.GetProperty("dayanak").GetString());
        Assert.Contains("BİLGİ AMAÇLI", b.GetProperty("uyari").GetString());
        Assert.Equal(0, b.GetProperty("tutarlar").GetArrayLength());           // VUK 230/5: fiyat/bedel yok
        var bolum = b.GetProperty("bolumler")[0];
        var basliklar = bolum.GetProperty("sutunBasliklari").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal(["Sıra", "Ürün / tanım", "Kod", "Miktar"], basliklar);
        var hucreler = bolum.GetProperty("satirlar")[0].GetProperty("hucreler").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal("3 adet", hucreler[3]);
        var tumMetin = b.GetRawText();
        Assert.DoesNotContain("12345", tumMetin);                              // birim fiyat kâğıda SIZMAZ
        Assert.DoesNotContain("12.345", tumMetin);
        Assert.Equal("Gönderen (tedarikçi)", b.GetProperty("karsi").GetProperty("baslik").GetString());
    }

    // ── EB03 ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task EB03_hasim_mal_kabul_fazla_eksi_sifir_kesir_cift_kalem_400_ve_sifir_satir()
    {
        var k = Kur("EB03");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var id = await SiparisAc(c, k, adet: 5m);
        var kalem = await KalemId(c, id);
        await Tamam(await c.PostAsync($"/api/v1/satinalma/{id}/onayla", null));
        var once = Sayim(k);

        object[] gecersizler =
        [
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 6m } } },      // fazla
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = -1m } } },     // eksi
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 0m } } },      // sıfır
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1.5m } } },    // ADET'e kesir
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 3m }, new { kalemId = kalem, adet = 3m } } }, // aynı kalem iki kez
            new { kurSnapshotId = (string?)null, kalemler = new[] { new { kalemId = kalem, adet = 1m } } }, // kur yok
            new { kurSnapshotId = k.KurId, aciklama = new string('x', 501), kalemler = new[] { new { kalemId = kalem, adet = 1m } } }, // aşırı uzun
        ];
        foreach (var g in gecersizler)
        {
            var r = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul", g);
            Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode}: {await r.Metin()} · {JsonSerializer.Serialize(g)}");
        }
        Assert.Equal(once, Sayim(k));                                           // 0 satır: stok/irsaliye/hareket/kabul aynı

        // TUR 2 (mutasyon T2-M1-2 açığı): ADET'e kesir mal kabulün KENDİ kapısında, kalem sırasıyla ve
        // "kalemler" alanına bağlı reddedilir (ekran alan altında gösterir) — alış servisinin geç reddine bırakılmaz.
        var kesir = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1.5m } } });
        var kesirGovde = JsonDocument.Parse(await kesir.Metin()).RootElement;
        Assert.True(kesirGovde.TryGetProperty("alanlar", out var alanlar) && alanlar.TryGetProperty("kalemler", out _),
            kesirGovde.ToString());
        Assert.Contains("Kalem 1", kesirGovde.GetProperty("message").GetString());
    }

    // ── EB04 ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task EB04_hasim_taslak_ve_kapanmis_siparise_kabul_409_sifir_satir()
    {
        var k = Kur("EB04");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var id = await SiparisAc(c, k, adet: 2m);
        var kalem = await KalemId(c, id);
        var once = Sayim(k);

        var taslak = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1m } } });
        Assert.Equal(HttpStatusCode.Conflict, taslak.StatusCode);
        Assert.Equal(once, Sayim(k));

        await Tamam(await c.PostAsync($"/api/v1/satinalma/{id}/onayla", null));
        await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 2m } } }));
        var kapali = Sayim(k);
        var tekrar = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1m } } });
        Assert.Equal(HttpStatusCode.Conflict, tekrar.StatusCode);
        Assert.Equal(kapali, Sayim(k));

        // Kabul yapılmış sipariş iptal edilemez (deftere yazıldı) → 409; durum KAPANDI kalır.
        var iptal = await c.PostJson($"/api/v1/satinalma/{id}/iptal", new { gerekce = "tedarikçi yanlış mal yolladı" });
        Assert.Equal(HttpStatusCode.Conflict, iptal.StatusCode);
    }

    // ── EB05 ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task EB05_hasim_baska_kiracinin_siparisi_irsaliyesi_ve_belgesi_404()
    {
        var a = Kur("EB05-A");
        var b = Kur("EB05-B");
        var ca = f.Ortam.Istemci(a, "SAHIP");
        var cb = f.Ortam.Istemci(b, "SAHIP");
        var idB = await SiparisAc(cb, b, adet: 2m);
        var kalemB = await KalemId(cb, idB);
        await Tamam(await cb.PostAsync($"/api/v1/satinalma/{idB}/onayla", null));
        var kabulB = await Tamam(await cb.PostJson($"/api/v1/satinalma/{idB}/mal-kabul",
            new { kurSnapshotId = b.KurId, kalemler = new[] { new { kalemId = kalemB, adet = 1m } } }));
        var irsB = kabulB.GetProperty("irsaliyeId").GetString()!;
        var onceB = Sayim(b);

        Assert.Equal(HttpStatusCode.NotFound, (await ca.GetAsync($"/api/v1/satinalma/{idB}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ca.PostJson($"/api/v1/satinalma/{idB}/mal-kabul",
            new { kurSnapshotId = a.KurId, kalemler = new[] { new { kalemId = kalemB, adet = 1m } } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ca.GetAsync($"/api/v1/irsaliyeler/{irsB}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ca.GetAsync($"/api/v1/cikti/{BelgeTurleri.Irsaliye}/{irsB}")).StatusCode);
        Assert.Equal(onceB, Sayim(b));
    }

    // ── EB06 ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task EB06_hasim_siparis_formu_kesir_gecmis_tarih_uzun_metin_400_siparis_dogmaz()
    {
        var k = Kur("EB06");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var dun = DateTime.UtcNow.AddDays(-3).ToString("yyyy-MM-dd");
        object[] gecersizler =
        [
            new { tedarikciCariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 2.5m, birimFiyatTl = 10m } } },
            new { tedarikciCariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = -2m, birimFiyatTl = 10m } } },
            new { tedarikciCariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 2m, birimFiyatTl = -10m } } },
            new { tedarikciCariId = k.CariId, beklenenTarih = dun, kalemler = new[] { new { urunId = k.UrunId, adet = 2m, birimFiyatTl = 10m } } },
            new { tedarikciCariId = k.CariId, beklenenTarih = "2999-01-01", kalemler = new[] { new { urunId = k.UrunId, adet = 2m, birimFiyatTl = 10m } } },
            new { tedarikciCariId = k.CariId, aciklama = new string('a', 501), kalemler = new[] { new { urunId = k.UrunId, adet = 2m, birimFiyatTl = 10m } } },
            new { tedarikciCariId = k.CariId, hesapKodu = new string('1', 31), kalemler = new[] { new { urunId = k.UrunId, adet = 2m, birimFiyatTl = 10m } } },
            new { tedarikciCariId = "01HZZZZZZZZZZZZZZZZZZZZZZZ", kalemler = new[] { new { urunId = k.UrunId, adet = 2m, birimFiyatTl = 10m } } },
        ];
        foreach (var g in gecersizler)
        {
            var r = await c.PostJson("/api/v1/satinalma", g);
            Assert.True(r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound,
                $"{(int)r.StatusCode}: {await r.Metin()} · {JsonSerializer.Serialize(g)}");
        }
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.SatinalmaSiparisleri.CountAsync(s => s.TenantId == k.TenantId));

        // PATCH de aynı kuralı uygular: geçmiş beklenen tarih 400, gönderilmeyen alan korunur.
        var id = await SiparisAc(c, k);
        var patch = await c.PatchJson($"/api/v1/satinalma/{id}", new { beklenenTarih = dun });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
        var yarin = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd");
        var iyi = await Tamam(await c.PatchJson($"/api/v1/satinalma/{id}", new { beklenenTarih = yarin }));
        Assert.Equal(yarin, iyi.GetProperty("beklenenTarih").GetString());
    }

    // ── EB07 ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task EB07_hasim_sevk_irsaliyesi_ileri_tarih_kesir_uzun_400_irsaliye_dogmaz_gecerli_200()
    {
        var k = Kur("EB07");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var yarin = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd");
        var eski = DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-dd");
        object[] gecersizler =
        [
            new { yon = "GIDEN", cariId = k.CariId, tarih = yarin, kalemler = new[] { new { urunId = k.UrunId, adet = 1m } } },
            new { yon = "GIDEN", cariId = k.CariId, tarih = eski, kalemler = new[] { new { urunId = k.UrunId, adet = 1m } } },
            new { yon = "GIDEN", cariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 0.5m } } },
            new { yon = "GIDEN", cariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = -1m } } },
            new { yon = "GIDEN", cariId = k.CariId, aciklama = new string('z', 501), kalemler = new[] { new { urunId = k.UrunId, adet = 1m } } },
            new { yon = "GELEN", cariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 1m } } },
        ];
        foreach (var g in gecersizler)
        {
            var r = await c.PostJson("/api/v1/irsaliyeler", g);
            Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode}: {await r.Metin()} · {JsonSerializer.Serialize(g)}");
        }
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0, await db.Irsaliyeler.CountAsync(s => s.TenantId == k.TenantId));

        var iyi = await Tamam(await c.PostJson("/api/v1/irsaliyeler", new
        {
            yon = "GIDEN", cariId = k.CariId, kalemler = new[] { new { urunId = k.UrunId, adet = 2m } },
        }));
        var irsId = iyi.GetProperty("id").GetString()!;
        Assert.Equal("ACIK", iyi.GetProperty("durum").GetString());
        // İrsaliye deftere YAZMAZ: stok değişmez.
        var b = await Tamam(await c.GetAsync($"/api/v1/cikti/{BelgeTurleri.Irsaliye}/{irsId}"));
        Assert.Equal("SEVK İRSALİYESİ", b.GetProperty("belgeAdi").GetString());
        Assert.Equal("Teslim alan", b.GetProperty("karsi").GetProperty("baslik").GetString());
        // PATCH tarih: ileri tarih 400; kayıt korunur.
        var patch = await c.PatchJson($"/api/v1/irsaliyeler/{irsId}", new { tarih = yarin });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
    }

    // ── EB08 ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task EB08_tezgahtar_satinalma_irsaliye_ve_irsaliye_belgesi_403()
    {
        var k = Kur("EB08");
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        var id = await SiparisAc(sahip, k, adet: 1m);
        var kalem = await KalemId(sahip, id);
        await Tamam(await sahip.PostAsync($"/api/v1/satinalma/{id}/onayla", null));
        var kabul = await Tamam(await sahip.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1m } } }));
        var irsId = kabul.GetProperty("irsaliyeId").GetString()!;

        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.Forbidden, (await t.GetAsync("/api/v1/satinalma")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await t.GetAsync($"/api/v1/satinalma/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await t.PostJson("/api/v1/satinalma", new { tedarikciCariId = k.CariId })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await t.GetAsync("/api/v1/irsaliyeler")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await t.GetAsync($"/api/v1/cikti/{BelgeTurleri.Irsaliye}/{irsId}")).StatusCode);
        // MUHASEBE okur ama yazamaz (sunucu kapısı).
        var m = f.Ortam.Istemci(k, "MUHASEBE");
        Assert.Equal(HttpStatusCode.OK, (await m.GetAsync($"/api/v1/cikti/{BelgeTurleri.Irsaliye}/{irsId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await m.PostAsync($"/api/v1/satinalma/{id}/onayla", null)).StatusCode);
    }
}

// ── EB10 (tur 3 — hasım KRİTİK): eşik üstü mal kabul MASAK beyanıyla yapılabilir ──
[Collection("http")]
public sealed class EksikB1SatinalmaMasakTestleri(HttpFikstur f)
{
    private static object Beyan() => new
    {
        belgeTuru = "TC_KIMLIK", belgeNo = "10000000146", adSoyad = "Ayşe Demir", uyruk = "TUR",
        islemAmaci = "Ticari mal alımı", aciklama = "Toptancıdan işletme stoğu için bilezik alımı",
        fonKaynagi = "Ticari gelir", ekBelgeReferansi = "EK-1",
    };

    /// <summary>
    /// Tedbirler Yön. md.5/1-b: kimlik tespiti işlem tutarına YA DA bağlantılı işlemler toplamına bağlıdır
    /// (ödeme şekline değil). İlk teslimat 150.000 (eşik altı) beyansız geçer; aynı gün aynı tedarikçiden
    /// ikinci teslimat (+300.000 → toplam 450.000) beyansız 400 + 0 satır (atomik: irsaliye de doğmaz);
    /// beyanla 200, stok/hareket yazılır ve MASAK kimlik tespiti kaydı doğar.
    /// </summary>
    [Fact]
    public async Task EB10_esik_ustu_mal_kabul_beyansiz_400_sifir_satir_beyanla_200_ve_kimlik_kaydi()
    {
        Kiraci k;
        using (var db0 = f.Vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(db0, "PAKETEKSIK-B1-SATINALMA-TEST EB10");
            db0.SaveChanges();
        }
        var c = f.Ortam.Istemci(k, "SAHIP");
        async Task<JsonElement> Tamam(HttpResponseMessage r)
        {
            var g = await r.Metin();
            Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode}: {g}");
            return JsonDocument.Parse(g).RootElement;
        }
        var y = await Tamam(await c.PostJson("/api/v1/satinalma", new
        {
            tedarikciCariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 5m, birimFiyatTl = 150_000m } },
        }));
        var id = y.GetProperty("id").GetString()!;
        var d = await Tamam(await c.PostAsync($"/api/v1/satinalma/{id}/onayla", null));
        var kalem = d.GetProperty("kalemler")[0].GetProperty("id").GetString()!;

        (int Irs, int Alis, int Kimlik, decimal Kabul) Say()
        {
            using var db = f.Vt.OlusturSistem();
            return (db.Irsaliyeler.AsNoTracking().Count(i => i.TenantId == k.TenantId),
                db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.Tur == "ALIS"),
                db.MasakKimlikTespitleri.AsNoTracking().Count(m => m.TenantId == k.TenantId),
                db.SatinalmaKalemleri.AsNoTracking().Where(x => x.TenantId == k.TenantId).Sum(x => (decimal?)x.KabulEdilenAdet) ?? 0m);
        }

        // (1) 150.000 — eşik altı, beyansız geçer.
        await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1m } } }));
        var once = Say();
        Assert.Equal((1, 1, 0, 1m), once);

        // (2) +300.000 (bağlantılı toplam 450.000) beyansız → 400, MASAK iletisi, 0 satır.
        var r = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 2m } } });
        var govde = await r.Metin();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode}: {govde}");
        Assert.Contains("kimlik", govde, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(once, Say());

        // (3) Aynı teslimat beyanla → 200, irsaliye + alış + kimlik tespiti kaydı.
        var ok = await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, masak = Beyan(), kalemler = new[] { new { kalemId = kalem, adet = 2m } } }));
        Assert.Equal("KISMI_KABUL", ok.GetProperty("yeniDurum").GetString());
        var son = Say();
        Assert.Equal(2, son.Irs);
        Assert.Equal(2, son.Alis);
        Assert.True(son.Kimlik >= 1, $"MASAK kimlik tespiti kaydı doğmadı: {son}");
        Assert.Equal(3m, son.Kabul);
    }
}

// ── EB09 — çift kabul (eşzamanlı): satır kilidi + kilit sonrası yeniden okuma ──
[Collection("veritabani")]
public sealed class EksikB1SatinalmaEszamanTestleri(TestVeritabani vt)
{
    private MSatinalma.SatinalmaServisi Servis(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db), new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance),
            NullLogger<MSatinalma.SatinalmaServisi>.Instance);

    private static string Kimlik(object yanit, string alan) =>
        JsonSerializer.SerializeToNode(yanit, yanit.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!
            .AsObject()[alan]!.GetValue<string>();

    [Fact]
    public async Task EB09_kilit_beklerken_kalan_baskasinca_kabul_edilirse_409_stok_ve_irsaliye_bir_kez()
    {
        Kiraci k;
        using (var kur = vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(kur, "PAKETEKSIK-B1-SATINALMA-TEST EB09");
            await kur.SaveChangesAsync();
        }
        string id, kalemId;
        using (var db0 = vt.OlusturSistem())
        {
            id = Kimlik(await Servis(db0).Olustur(new MSatinalma.SatinalmaOlusturDto
            {
                TedarikciCariId = k.CariId,
                Kalemler = [new MSatinalma.SatinalmaKalemDto { UrunId = k.UrunId, Adet = 3m, BirimFiyatTl = 500m }],
            }, k.Islem), "id");
            await Servis(db0).Onayla(id, k.Islem);
            kalemId = await db0.SatinalmaKalemleri.AsNoTracking().Where(x => x.SatinalmaSiparisId == id).Select(x => x.Id).FirstAsync();
        }
        decimal Stok()
        {
            using var db = vt.OlusturSistem();
            return db.Stoklar.AsNoTracking().Where(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Select(s => s.Adet).FirstOrDefault();
        }
        var stokOnce = Stok();

        // Tutucu: sipariş satırını kilitler (başka bir mal kabul sürüyor gibi).
        using var tutucu = vt.OlusturSistem();
        await using var tx = await tutucu.Database.BeginTransactionAsync();
        await tutucu.Database.Kilitle(KilitTablosu.SatinalmaSiparisleri, id, k.TenantId);

        using var db1 = vt.OlusturSistem();
        var gorev = Task.Run(() => Servis(db1).MalKabul(id, new MSatinalma.MalKabulDto
        {
            KurSnapshotId = k.KurId,
            Kalemler = [new MSatinalma.MalKabulKalemDto { KalemId = kalemId, Adet = 3m }],
        }, k.Islem));
        await Task.Delay(1_500);
        Assert.False(gorev.IsCompleted, "Mal kabul sipariş satır kilidini beklemeliydi.");

        // Kilit sahibi kalanın tamamını kabul etmiş gibi kalemi kapatır ve bırakır.
        var kalem = await tutucu.SatinalmaKalemleri.FirstAsync(x => x.Id == kalemId);
        kalem.KabulEdilenAdet = 3m;
        await tutucu.SaveChangesAsync();
        await tx.CommitAsync();

        await Assert.ThrowsAsync<ConflictException>(() => gorev);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Irsaliyeler.CountAsync(i => i.TenantId == k.TenantId));
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "ALIS"));
        Assert.Equal(stokOnce, Stok());
        Assert.Equal(3m, (await kontrol.SatinalmaKalemleri.AsNoTracking().FirstAsync(x => x.Id == kalemId)).KabulEdilenAdet);
    }
}
