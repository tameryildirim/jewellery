using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Pozisyon;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// 20.10 — raporsuz paket kalanları (KONTROL-RAPORSUZ-PAKETLER-2026-10-20.md): PAKET-03 siparişe belge,
// PAKET-01 hurda alışı fiş kimliği, PAKET-04 işletme logosu çıktıda, PAKET-06 WhatsApp bağlantısı, zekât hesaplayıcı.
public sealed class WhatsAppBaglantisiTestleri
{
    [Theory]
    [InlineData("0532 123 45 67", "https://wa.me/905321234567")]
    [InlineData("+90 (532) 123-45-67", "https://wa.me/905321234567")]
    [InlineData("5321234567", "https://wa.me/905321234567")]
    [InlineData("0212 555 00 41", null)]      // sabit hat: WhatsApp yok
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Cep_numarasi_wa_me_adresine_cevrilir_sabit_hat_ve_bos_bosluk(string? tel, string? beklenen) =>
        Assert.Equal(beklenen, WhatsAppBaglantisi.Adres(tel));

    [Fact]
    public void Hazir_metin_kaciriilir() =>
        Assert.Equal("https://wa.me/905321234567?text=Vadeniz%20geldi%2C%20%C3%B6deme%20bekliyoruz.", WhatsAppBaglantisi.Adres("05321234567", "Vadeniz geldi, ödeme bekliyoruz."));
}

[Collection("http")]
public sealed class RaporsuzPaketKalanHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Siparise_belge_baglanir_baska_kiracinin_siparisi_404()
    {
        Kiraci k, baska;
        using (var db = f.Vt.OlusturSistem()) { k = TestKurulum.KiraciKur(db, "RP-siparis"); baska = TestKurulum.KiraciKur(db, "RP-baska"); }
        var s = f.Ortam.Istemci(k, "SAHIP");
        var sp = await s.PostAsJsonAsync("/api/v1/siparisler", new
        {
            tur = "MUSTERI_SIPARISI", cariId = k.CariId, aciklama = "Alyans çifti", sozVerilenGun = DateOnly.FromDateTime(DateTime.Today.AddDays(10)).ToString("yyyy-MM-dd"),
            kalemler = new[] { new { tanim = "Alyans çifti", adet = 1m, gramAgirlik = 5m } },
        });
        var sy = await sp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(sp.IsSuccessStatusCode, sy.ToString());
        var siparisId = sy.GetProperty("id").GetString()!;
        var png = Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0]);
        var b = await s.PostAsJsonAsync("/api/v1/belgeler", new { sahipTuru = "SIPARIS", sahipId = siparisId, tur = "GORSEL", ad = "cizim.png", icerikBase64 = png });
        Assert.True(b.IsSuccessStatusCode, await b.Content.ReadAsStringAsync());
        var liste = await s.GetFromJsonAsync<JsonElement>($"/api/v1/belgeler?sahipTuru=SIPARIS&sahipId={siparisId}");
        Assert.Single(liste.EnumerateArray());
        var yabanci = await f.Ortam.Istemci(baska, "SAHIP").PostAsJsonAsync("/api/v1/belgeler", new { sahipTuru = "SIPARIS", sahipId = siparisId, tur = "GORSEL", ad = "x.png", icerikBase64 = png });
        Assert.Equal(HttpStatusCode.NotFound, yabanci.StatusCode);
    }

    [Fact]
    public async Task Hurda_alisi_fis_kimligi_alir_ve_fis_ucu_okur()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "RP-hurda");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var r = await s.PostAsJsonAsync("/api/v1/islemler/hurda-alis", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, kasaId = k.KasaId, odemeTuru = "NAKIT",
            ayarId = "K22", gramAgirlik = 1m, birimFiyatTlHasGram = 6_000m, satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ali Veli", tckn = "10000000146", adres = "Ulus" },
        });
        var y = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(r.IsSuccessStatusCode, y.ToString());
        using var db2 = f.Vt.OlusturSistem();
        var h = await db2.Hareketler.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId && x.Tur == "HURDA_ALIS");
        Assert.False(string.IsNullOrEmpty(h.FisId));
        var fis = await s.GetAsync($"/api/v1/islemler/fis/{h.FisId}");
        Assert.True(fis.IsSuccessStatusCode, await fis.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Isletme_logosu_yuklenince_ciktida_logo_belge_kimligi_doner_yoksa_null()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "RP-logo");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var sat = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, fisIskontoYuzde = 0m,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000m, hediye = false } },
            odemeler = new[] { new { arac = "NAKIT", tutarTl = 1_000m, kasaId = k.KasaId } },
        });
        Assert.True(sat.IsSuccessStatusCode, await sat.Content.ReadAsStringAsync());
        string hareketId;
        using (var db = f.Vt.OlusturSistem())
            hareketId = (await db.Hareketler.AsNoTracking().FirstAsync(x => x.TenantId == k.TenantId && x.Tur == "SATIS")).Id;
        var once = await s.GetFromJsonAsync<JsonElement>($"/api/v1/cikti/SATIS_FISI/{hareketId}");
        Assert.Equal(JsonValueKind.Null, once.GetProperty("duzenleyen").GetProperty("logoBelgeId").ValueKind);
        var png = Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0]);
        var b = await s.PostAsJsonAsync("/api/v1/belgeler", new { sahipTuru = "ISLETME", sahipId = k.TenantId, tur = "GORSEL", ad = "logo.png", icerikBase64 = png });
        Assert.True(b.IsSuccessStatusCode, await b.Content.ReadAsStringAsync());
        var sonra = await s.GetFromJsonAsync<JsonElement>($"/api/v1/cikti/SATIS_FISI/{hareketId}");
        Assert.Equal(JsonValueKind.String, sonra.GetProperty("duzenleyen").GetProperty("logoBelgeId").ValueKind);
    }
}

[Collection("http")]
public sealed class ZekatHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Pozisyon_zekat_ozeti_doner_nisap_80_18_kirkta_bir_TL_guncel_kurdan()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "RP-zekat", urunGram: 100m, stokAdet: 1m);
        var y = await f.Ortam.Istemci(k, "SAHIP").GetFromJsonAsync<JsonElement>("/api/v1/pozisyon");
        var z = y.GetProperty("zekat");
        Assert.Equal(80.18m, z.GetProperty("nisapGram").GetDecimal());
        var net = y.GetProperty("altin").GetProperty("netGram").GetDecimal();
        Assert.Equal(net >= 80.18m, z.GetProperty("nisapUstu").GetBoolean());
        if (net >= 80.18m)
        {
            Assert.Equal(Math.Round(net / 40m, 6), z.GetProperty("zekatHasGram").GetDecimal());
            Assert.Equal(JsonValueKind.Number, z.GetProperty("zekatTl").ValueKind);
        }
        else Assert.Equal(0m, z.GetProperty("zekatHasGram").GetDecimal());
    }
}
