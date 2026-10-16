using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Arama;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-BELGE 29.09 (tasarım hakemi k3, KRİTİK · K5 "TRY yerine ₺"):
// "Fişten Fatura Kes" penceresindeki fiş aramasının alt satırı ham biçimdeydi
// ("2 kalem · 2100.50 TRY"). Sözleşme: tutar ekranın para biçimiyle (tr-TR, bölünmez
// boşluk, ₺) — beklenen metin SABİT yazılır, üretim kodundan hesaplanmaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class ZoBelgeAramaFisTestleri(HttpFikstur f)
{
    private void Satir(Kiraci k, string fisId, decimal tl)
    {
        using var db = f.Vt.Olustur(k.TenantId);
        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = DateTime.UtcNow, Aciklama = "zb63 arama",
            TlTutari = tl, HasGrami = 0m, ParaBirimi = "TRY",
            CariId = k.CariId, FisId = fisId, KullaniciId = k.KullaniciId, KurSnapshotId = k.KurId,
            Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task ZB63_fis_aramasi_alt_satiri_TR_bicimli_ve_TL_simgeli_TRY_yok()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "ZB63 fiş arama");
        Satir(k, "FIS-ZB63-A", 1200.50m);
        Satir(k, "FIS-ZB63-A", 900m);
        Satir(k, "FIS-ZB63-B", 48790m);

        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/arama?q=FIS-ZB63&tur=FIS");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var y = (await cevap.Content.ReadFromJsonAsync<AramaYaniti>(
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)))!;

        var a = Assert.Single(y.Sonuclar, s => s.Kimlik == "FIS-ZB63-A");
        Assert.Equal("2 kalem · 2.100,50 ₺", a.AltBilgi);
        var b = Assert.Single(y.Sonuclar, s => s.Kimlik == "FIS-ZB63-B");
        // Kuruş sıfırsa yazılmaz (Bicim.Para kuralı).
        Assert.Equal("1 kalem · 48.790 ₺", b.AltBilgi);
        foreach (var s in y.Sonuclar)
        {
            Assert.DoesNotContain("TRY", s.AltBilgi);
            Assert.DoesNotContain(".00", s.AltBilgi);
        }
    }
}
