using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Disaaktarim;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// Y-10 (fişte işlem öncesi/sonrası bakiye) ve Y-15 (mali müşavir hesap kodları) — 20.10 rakip taraması § 4.1.
[Collection("http")]
public sealed class Y10Y15FisBakiyeHesapKoduTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Satis_fisinde_onceki_ve_yeni_bakiye_ekstreyle_ayni_kuralla_yazilir_pesin_musteride_yazilmaz()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "Y10-bakiye");
        var s = f.Ortam.Istemci(k, "SAHIP");
        string veresiyeId, pesinId;
        var pesinCariId = (await s.GetFromJsonAsync<JsonElement>("/api/v1/cari/sistem-carisi")).GetProperty("id").GetString()!;   // Peşin Müşteri ilk istekte açılır
        using (var db = f.Vt.Olustur(k))
        {
            var now = DateTime.UtcNow;
            // Veresiye satış: müşteri 10.000 TL borçlandı → kayıtlı bakiye 10.000 (kural: CariBakiyeKurali.TlEtkisi)
            veresiyeId = Kimlik.Ulid();
            HareketFabrikasi.Ekle(db, OdemeIzi.Veresiyeden(10_000m, DateOnly.FromDateTime(now).AddDays(30)), new Hareket
            {
                Id = veresiyeId, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now.AddMinutes(-10), TlTutari = 10_000m,
                HasGrami = 1m, Maden = Maden.Altin, ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = k.CariId,
                UrunId = k.UrunId, KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
            var etki = CariBakiyeKurali.TlEtkisi("SATIS", 10_000m, 10_000m, false) ?? 0m;
            var cb = await db.CariBakiyeler.SingleAsync(x => x.CariId == k.CariId);
            cb.TlBakiye = etki; cb.HasGramBakiye = Kuyumcu.Api.Modules.Cari.CariServisi.BakiyeHasEtkisi("SATIS", 1m, Maden.Altin, false);
            // Peşin müşteri (sistem carisi) fişi
            pesinId = Kimlik.Ulid();
            HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(5_000m), new Hareket
            {
                Id = pesinId, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now, TlTutari = 5_000m, HasGrami = 0.5m,
                Maden = Maden.Altin, ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = pesinCariId, UrunId = k.UrunId, KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
            await db.SaveChangesAsync();
        }
        var dokum = await s.GetStringAsync($"/api/v1/cikti/SATIS_FISI/{veresiyeId}");
        Assert.Contains("Önceki bakiye", dokum);
        Assert.Contains("Yeni bakiye", dokum);
        var j = JsonSerializer.Deserialize<JsonElement>(dokum);
        var tutarlar = j.GetProperty("tutarlar").EnumerateArray().ToDictionary(t => t.GetProperty("etiket").GetString()!, t => t.GetProperty("deger").GetDecimal());
        var etkiTl = CariBakiyeKurali.TlEtkisi("SATIS", 10_000m, 10_000m, false) ?? 0m;
        Assert.Equal(0m, tutarlar["Önceki bakiye"]);
        Assert.Equal(etkiTl, tutarlar["Yeni bakiye"]);
        var pesinDokum = await s.GetStringAsync($"/api/v1/cikti/SATIS_FISI/{pesinId}");
        Assert.DoesNotContain("Önceki bakiye", pesinDokum);
    }

    [Fact]
    public async Task Hesap_kodlari_yazilir_okunur_bicim_kapisi_400_ve_Luca_fisi_yeni_kodlarla_cikar()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "Y15-hesap");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var ilk = await s.GetFromJsonAsync<JsonElement>("/api/v1/muhasebe-hesaplari");
        Assert.Equal("600", ilk.GetProperty("satisHesabi").GetString());
        Assert.Equal("391", ilk.GetProperty("kdvHesabi").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync("/api/v1/muhasebe-hesaplari", new { satisHesabi = "60A" })).StatusCode);
        var y = await s.PutAsJsonAsync("/api/v1/muhasebe-hesaplari", new { satisHesabi = "600.01", kdvHesabi = "391.001" });
        Assert.True(y.IsSuccessStatusCode, await y.Content.ReadAsStringAsync());
        var son = await s.GetFromJsonAsync<JsonElement>("/api/v1/muhasebe-hesaplari");
        Assert.Equal("600.01", son.GetProperty("satisHesabi").GetString());

        using (var db = f.Vt.OlusturSistem())
        {
            var cari = await db.Cariler.SingleAsync(c => c.Id == k.CariId);
            cari.HesapKodu = "120.001";
            db.Faturalar.Add(new Fatura
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId, FaturaNo = "FTR-Y15", Tur = "SATIS", Durum = "KESILDI",
                AraToplam = 1_000m, KdvOran = 0.2m, KdvTutar = 200m, Toplam = 1_200m, Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
            var (fisler, _) = await new LucaFisKaynagi().Fisler(db, k.TenantId);
            var fis = fisler.Single(x => x[0][3] == "FTR-Y15");
            Assert.Equal("600.01", fis[1][2]);
            Assert.Equal("391.001", fis[2][2]);
        }

        // Boş gönderince varsayılana döner
        await s.PutAsJsonAsync("/api/v1/muhasebe-hesaplari", new { });
        var geri = await s.GetFromJsonAsync<JsonElement>("/api/v1/muhasebe-hesaplari");
        Assert.Equal("600", geri.GetProperty("satisHesabi").GetString());
    }
}
