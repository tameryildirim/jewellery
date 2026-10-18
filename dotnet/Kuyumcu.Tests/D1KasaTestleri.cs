using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Isler;
using Kuyumcu.Api.Modules.Isler.Isler;
using Kuyumcu.Contracts.Sayfalar.Kasalar;
using Kuyumcu.Web.Bilesenler.Fis;
using Microsoft.EntityFrameworkCore;
using IslemKasa = Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-11 (18.10, rakip eşitleme) — kör kasa sayımı · çalışan kasası · gün kapanışı hatırlatması.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1KasaTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Kor_sayimda_mudur_tutari_gormez_tutmayan_sayim_tutarsiz_reddedilir_iz_kalir_gerekceyle_kapanir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-kor");
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        var mudur = f.Ortam.Istemci(k, "MUDUR");

        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").PutAsJsonAsync("/api/v1/kasalar/kor-sayim", new { etkin = true })).StatusCode);
        Assert.False((await mudur.GetFromJsonAsync<GunKapanisiOnizleme>($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi/onizleme"))!.Gizli);
        Assert.True((await sahip.PutAsJsonAsync("/api/v1/kasalar/kor-sayim", new { etkin = true })).IsSuccessStatusCode);

        var gizli = await mudur.GetFromJsonAsync<GunKapanisiOnizleme>($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi/onizleme");
        Assert.True(gizli!.Gizli);
        Assert.Equal(0m, gizli.NakitDefter);   // tutar GÖNDERİLMEDİ
        var acik = await sahip.GetFromJsonAsync<GunKapanisiOnizleme>($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi/onizleme");
        Assert.False(acik!.Gizli);             // SAHİP görür
        Assert.NotEqual(0m, acik.NakitDefter);

        var red = await mudur.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", new { sayilanNakit = 999m });
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
        var mesaj = (await red.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;
        Assert.Contains("tutmadı", mesaj);
        Assert.DoesNotContain(acik.NakitDefter.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")), mesaj);
        using (var db = f.Vt.OlusturSistem())
            Assert.Contains(db.DenetimKayitlari.AsNoTracking().Where(d => d.TenantId == k.TenantId).Select(d => d.Detay).ToList(),
                d => d != null && d.Contains("kör sayım denemesi"));

        var ok = await mudur.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            new { sayilanNakit = 999m, aciklama = "Kör sayım denemesi, sayım tutmadı; yarın yeniden sayılacak." });
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        Assert.Equal(999m - acik.NakitDefter, (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("fark").GetDecimal());
    }

    [Fact]
    public async Task Kasaya_sorumlu_calisan_atanir_kaldirilir_yabanci_kimlik_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-sorumlu");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var r = await s.PatchAsJsonAsync($"/api/v1/kasalar/{k.KasaId}", new { sorumluKullaniciId = k.KullaniciId });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var l = await s.GetFromJsonAsync<List<KasaVeri>>("/api/v1/kasalar");
        Assert.Equal(k.KullaniciId, l!.Single(x => x.Id == k.KasaId).SorumluKullaniciId);

        Assert.Equal(HttpStatusCode.BadRequest, (await s.PatchAsJsonAsync($"/api/v1/kasalar/{k.KasaId}", new { sorumluKullaniciId = "01ZZZZZZZZZZZZZZZZZZZZZZZZ" })).StatusCode);
        Assert.True((await s.PatchAsJsonAsync($"/api/v1/kasalar/{k.KasaId}", new { sorumluKullaniciId = "" })).IsSuccessStatusCode);
        Assert.Null((await s.GetFromJsonAsync<List<KasaVeri>>("/api/v1/kasalar"))!.Single(x => x.Id == k.KasaId).SorumluKullaniciId);
    }

    [Fact]
    public async Task Aksam_hareketli_kapanmamis_kasa_icin_hatirlatma_kapaninca_yok_saat_dolmadan_yok()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-kapanis");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var h = await s.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket", new { yon = "GIRIS", tutar = 100m, odemeTuru = "NAKIT", aciklama = "test" });
        Assert.True(h.IsSuccessStatusCode, await h.Content.ReadAsStringAsync());

        var bugun = Kuyumcu.Api.Common.Zaman.YerelBugun();
        DateTime Yerel(int saat) => Kuyumcu.Api.Common.Zaman.YerelGunAraligiUtc(bugun).Item1.AddHours(saat);
        async Task<IsCikti> Kos(DateTime an)
        {
            using var db = f.Vt.Olustur(k.TenantId);
            return await new KasaKapanisHatirlatmaIsi().Calistir(new IsBaglami(IsCatisiKurulum.Kapsam(db, IsCatisiKurulum.Yapilandirma()), db, k.TenantId, an), default);
        }
        Assert.Empty((await Kos(Yerel(12))).Taslaklar);                  // 12:00 — saat dolmadı
        var aksam = await Kos(Yerel(20));
        var t = Assert.Single(aksam.Taslaklar);
        Assert.Equal(k.KasaId, t.IlgiliVarlikId);

        var kapat = await s.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", new { sayilanNakit = (await s.GetFromJsonAsync<GunKapanisiOnizleme>($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi/onizleme"))!.NakitDefter });
        Assert.True(kapat.IsSuccessStatusCode, await kapat.Content.ReadAsStringAsync());
        Assert.Empty((await Kos(Yerel(20))).Taslaklar);
    }
}

public sealed class D1KasaSafTestleri
{
    [Fact]
    public void Tezgahta_calisanin_sorumlu_oldugu_kasa_varsayilandir_yoksa_subenin_ilk_kasasi()
    {
        IslemKasa[] l = [new("k1", "Ana", "TRY", true, "s1"), new("k2", "Ayşe", "TRY", true, "s1", "u2"), new("k3", "Başka şube", "TRY", true, "s2", "u3")];
        Assert.Equal("k2", FisHesap.VarsayilanKasa(l, "s1", "u2"));
        Assert.Equal("k1", FisHesap.VarsayilanKasa(l, "s1", "u9"));
        Assert.Equal("k1", FisHesap.VarsayilanKasa(l, "s1", "u3"));   // sorumlu olduğu kasa başka şubede → kendi şubesinin ilki
        Assert.Equal("k1", FisHesap.VarsayilanKasa(l, "s1"));
    }
}
