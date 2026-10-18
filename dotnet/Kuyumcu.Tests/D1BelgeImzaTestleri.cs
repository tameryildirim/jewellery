using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-13 (18.10, rakip eşitleme) — ürün belgesi (garanti kartı) · kimlik tespit formu · ekranda imza.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1BelgeImzaTestleri(HttpFikstur f)
{
    private static string PngB64()
    {
        var b = new byte[80];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(b, 0);
        return Convert.ToBase64String(b);
    }

    private static string JpegB64()
    {
        var b = new byte[64];
        b[0] = 0xFF; b[1] = 0xD8; b[2] = 0xFF;
        return Convert.ToBase64String(b);
    }

    [Fact]
    public async Task Urun_belgesi_satisin_urunlerini_basar_ekran_imzasi_yuklenince_belgede_gorunur()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-urun-belgesi");
        decimal birim;
        using (var db = f.Vt.OlusturSistem())
        {
            var kur = db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati;
            birim = Math.Ceiling(k.UrunHasGrami * kur * 1.10m);
        }
        var s = f.Ortam.Istemci(k, "SAHIP");
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.True((await s.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = k.CariId, kimlikNo = "10000000146" })).IsSuccessStatusCode);
        var r = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = birim, hediye = false } },
            odemeler = new[] { new { arac = "KART", tutarTl = birim, kasaId = k.KasaId } },
            kurSnapshotId = k.KurId, fisIskontoYuzde = 0m, belgeTuru = "FATURA",
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        string satisId;
        using (var db = f.Vt.OlusturSistem())
            satisId = db.Hareketler.AsNoTracking().First(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Id;

        var b1 = await t.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.GarantiKarti}/{satisId}");
        Assert.Equal("ÜRÜN BELGESİ", b1!.BelgeAdi);
        Assert.Single(b1.Bolumler[0].Satirlar);
        Assert.Null(b1.ImzaGorseli);
        Assert.Contains("Garanti koşulları", b1.Not);   // koşul UYDURULMAZ — işletmeye yönlendirir

        // İmza yalnız PNG; sahip satış satırı.
        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync("/api/v1/belgeler",
            new { sahipTuru = "SATIS", sahipId = satisId, tur = "IMZA", ad = "imza.jpg", icerikBase64 = JpegB64() })).StatusCode);
        var y = await t.PostAsJsonAsync("/api/v1/belgeler", new { sahipTuru = "SATIS", sahipId = satisId, tur = "IMZA", ad = "imza.png", icerikBase64 = PngB64() });
        Assert.True(y.IsSuccessStatusCode, await y.Content.ReadAsStringAsync());
        var b2 = await t.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.GarantiKarti}/{satisId}");
        Assert.Equal(PngB64(), b2!.ImzaGorseli);

        // Satış olmayan satır sahip olamaz; satış olmayan hareketten ürün belgesi düzenlenmez.
        Assert.Equal(HttpStatusCode.NotFound, (await t.PostAsJsonAsync("/api/v1/belgeler",
            new { sahipTuru = "SATIS", sahipId = k.UrunId, tur = "IMZA", ad = "imza.png", icerikBase64 = PngB64() })).StatusCode);
    }

    [Fact]
    public async Task Kimlik_tespit_formu_tam_kimligi_basar_tezgahtar_403_okuma_izi_duser()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-kimlik-formu");
        var id = Kimlik.Ulid();
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            db.MasakKimlikTespitleri.Add(new MasakKimlikTespiti
            {
                Id = id, TenantId = k.TenantId, CariId = k.CariId, IslemTutariTl = 250_000m,
                BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = "10000000146", AdSoyad = "Ayşe Demir",
                Adres = "Ulus", Uyruk = "TUR", DogumTarihi = new DateOnly(1980, 1, 1),
                TespitEdenKullaniciId = k.KullaniciId, Tarih = DateTime.UtcNow,
                SaklamaBitis = DateTime.UtcNow.AddYears(8), Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync($"/api/v1/cikti/{BelgeTurleri.KimlikTespit}/{id}")).StatusCode);
        var b = await f.Ortam.Istemci(k, "SAHIP").GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.KimlikTespit}/{id}");
        Assert.Equal("KİMLİK TESPİT FORMU", b!.BelgeAdi);
        var alanlar = b.Bolumler[0].Satirlar.ToDictionary(s => s.Hucreler[0], s => s.Hucreler[1]);
        Assert.Equal("Ayşe Demir", alanlar["Ad soyad"]);
        Assert.Equal("10000000146", alanlar["Belge numarası"]);   // form tam numarayı basar (imzalatılıp saklanır)
        Assert.Equal(2, b.ImzaBasliklari.Length);
        using (var db = f.Vt.OlusturSistem())
            Assert.True(db.DenetimKayitlari.AsNoTracking().Any(d => d.TenantId == k.TenantId && d.VarlikId == id));
    }
}
