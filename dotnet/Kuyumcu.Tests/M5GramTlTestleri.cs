using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Contracts.Cikti;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M5 — gram + TL yan yana (ses kaydı 07:07: "kesmem için hem gramını görmem hem de değerini görmem lazım…
// hepsini has'a çeviremezsin"). Satış fişi kâğıdında ürünün GRAMI has'tan önce; has ek bilgi.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class M5GramTlTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Satis_fisi_ciktisinda_urun_grami_hasdan_ONCE_ve_degeri_dogru()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "M5-fis");
        decimal gram, birim;
        using (var db = f.Vt.OlusturSistem())
        {
            gram = db.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId).GramAgirlik;
            var kur = db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati;
            birim = Math.Ceiling(k.UrunHasGrami * kur * 1.10m);
        }
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.True((await s.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = k.CariId, kimlikNo = "10000000146" })).IsSuccessStatusCode);
        var r = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = birim, hediye = false } },
            odemeler = new[] { new { arac = "KART", tutarTl = birim, kasaId = k.KasaId } },
            kurSnapshotId = k.KurId, fisIskontoYuzde = 0m, belgeTuru = "FATURA",   // test ürünü kuyumcu fatura haddinin üstünde
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        string hareketId;
        using (var db = f.Vt.OlusturSistem())
            hareketId = db.Hareketler.AsNoTracking().First(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Id;

        var belge = JsonDocument.Parse(await s.GetStringAsync($"/api/v1/cikti/{BelgeTurleri.SatisFisi}/{hareketId}")).RootElement;
        var bolum = belge.GetProperty("bolumler")[0];
        var basliklar = bolum.GetProperty("sutunBasliklari").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal(["Tür", "Açıklama", "Gram", "Has gram", "Tutar"], basliklar);
        Assert.Equal(SutunBicimi.Gram, bolum.GetProperty("sutunBicimleri")[2].GetString());
        var hucre = bolum.GetProperty("satirlar")[0].GetProperty("hucreler")[2].GetString();
        Assert.Equal(decimal.Round(gram, 3), decimal.Parse(hucre!, System.Globalization.CultureInfo.InvariantCulture));
    }
}
