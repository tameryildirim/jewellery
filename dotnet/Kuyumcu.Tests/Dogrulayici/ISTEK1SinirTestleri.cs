using System.Net;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — PAKET ISTEK-1 SINIR TESTLERİ (kalıcı gerileme testi)
//
// Paketin dokunduğu miktar yolu: GET /api/v1/urunler/stok-adetleri (ürün başına
// tüm şubelerin adet + has gram toplamı) ve ürün listesindeki gösterimi
// (Bicim.Miktar). Para/kur/kasa hesabı YOK; bu yüzden kuruş yuvarlaması ve
// Türkçe giriş ayrıştırması bu uçta uygulanmaz (uç kullanıcıdan sayı almaz).
// Beklenen değerler ELLE yazılmıştır, üretim kodundan hesaplanmaz.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class ISTEK1SinirTestleri(HttpFikstur f)
{
    private async Task<(Kiraci K, string Sube2)> KurAsync(string ad, decimal anaAdet, decimal anaHas, decimal ikinciAdet, decimal ikinciHas)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, ad, stokAdet: 0m);
        var sube2 = Kuyumcu.Api.Common.Kimlik.Ulid();
        using (var db = f.Vt.OlusturSistem())
        {
            var now = DateTime.UtcNow;
            var ana = db.Stoklar.Single(s => s.TenantId == k.TenantId && s.UrunId == k.UrunId);
            ana.Adet = anaAdet; ana.ToplamHasGram = anaHas;
            db.Subeler.Add(new Sube { Id = sube2, TenantId = k.TenantId, Ad = "Sınır Şube 2", Aktif = true, Olusturuldu = now, Guncellendi = now });
            db.Stoklar.Add(new Stok
            {
                Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = sube2, UrunId = k.UrunId,
                Adet = ikinciAdet, ToplamHasGram = ikinciHas, Guncellendi = now,
            });
            await db.SaveChangesAsync();
        }
        return (k, sube2);
    }

    private async Task<System.Text.Json.JsonElement> SatirAsync(Kiraci k)
    {
        var y = await f.Ortam.Istemci(k).GetAsync($"/api/v1/urunler/stok-adetleri?ids={k.UrunId}");
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        return Assert.Single((await y.Json()).GetProperty("urunler").EnumerateArray().ToList());
    }

    [Fact]
    public async Task S1_sifir_stok_iki_subede_0_doner()
    {
        var (k, _) = await KurAsync("ISTEK1 sinir sifir", 0m, 0m, 0m, 0m);
        var s = await SatirAsync(k);
        Assert.Equal(0m, s.GetProperty("toplamAdet").GetDecimal());
        Assert.Equal(0m, s.GetProperty("toplamHasGram").GetDecimal());
    }

    [Fact]
    public async Task S2_eksi_stok_isareti_korunur_toplam_dogru()
    {
        // Şube 1: -3 adet (eksiye düşmüş stok), şube 2: +1 → toplam -2; işaret yutulmaz.
        var (k, _) = await KurAsync("ISTEK1 sinir eksi", -3m, -2.748m, 1m, 0.916m);
        var s = await SatirAsync(k);
        Assert.Equal(-2m, s.GetProperty("toplamAdet").GetDecimal());
        Assert.Equal(-1.832m, s.GetProperty("toplamHasGram").GetDecimal());
    }

    [Fact]
    public async Task S3_has_gram_4_hane_0_0001_kaybolmaz()
    {
        var (k, _) = await KurAsync("ISTEK1 sinir has4", 0.001m, 0.0001m, 0.002m, 0.0002m);
        var s = await SatirAsync(k);
        Assert.Equal(0.003m, s.GetProperty("toplamAdet").GetDecimal());
        Assert.Equal(0.0003m, s.GetProperty("toplamHasGram").GetDecimal());
    }

    [Fact]
    public async Task S4_cok_buyuk_miktar_kolon_tavaninda_tasmadan_toplanir()
    {
        // adet decimal(12,3) tavanı 999.999.999,999; iki şube toplamı tavanı aşar
        // (1.999.999.999,998) — SUM sonucu taşmamalı, tam değer dönmeli.
        var (k, _) = await KurAsync("ISTEK1 sinir buyuk", 999_999_999.999m, 999_999_999.999999m, 999_999_999.999m, 0.000001m);
        var s = await SatirAsync(k);
        Assert.Equal(1_999_999_999.998m, s.GetProperty("toplamAdet").GetDecimal());
        Assert.Equal(1_000_000_000m, s.GetProperty("toplamHasGram").GetDecimal());
    }

    [Theory]
    [InlineData("")]
    [InlineData(",,,")]
    [InlineData(" , ")]
    public async Task S5_bos_null_kimlik_listesi_400(string ids)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "ISTEK1 sinir bos " + ids.Length);
        var yol = "/api/v1/urunler/stok-adetleri" + (ids.Length == 0 ? "" : "?ids=" + Uri.EscapeDataString(ids));
        var y = await f.Ortam.Istemci(k).GetAsync(yol);
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }

    [Fact]
    public async Task S6_tam_500_kimlik_kabul_501_red_sinir_tam_esik()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "ISTEK1 sinir 500");
        var ist = f.Ortam.Istemci(k);
        var tam = string.Join(",", new[] { k.UrunId }.Concat(Enumerable.Range(0, 499).Select(i => $"Z{i:D5}")));
        var y = await ist.GetAsync($"/api/v1/urunler/stok-adetleri?ids={tam}");
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        Assert.Single((await y.Json()).GetProperty("urunler").EnumerateArray());
        // Tekrarlayan kimlik sayılmaz: 500 benzersiz + 1 tekrar = kabul.
        var tekrarli = tam + "," + k.UrunId;
        Assert.Equal(HttpStatusCode.OK, (await ist.GetAsync($"/api/v1/urunler/stok-adetleri?ids={tekrarli}")).StatusCode);
    }

    [Theory]
    [InlineData(0, "ADET", "0 adet")]
    [InlineData(-2, "ADET", "-2 adet")]
    [InlineData(2.5, "ADET", "2,5 adet")]
    public void S7_liste_hucresi_tr_TR_bicimi(decimal adet, string birim, string beklenen)
    {
        Assert.Equal(beklenen, Bicim.Miktar(adet, birim));
    }

    // ── Tur 2 doğrulayıcı ekleri ────────────────────────────────────────────

    [Fact]
    public async Task S8_tam_esik_ustu_501_benzersiz_kimlik_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "ISTEK1 sinir 501");
        var fazla = string.Join(",", Enumerable.Range(0, 501).Select(i => $"Y{i:D5}"));
        var y = await f.Ortam.Istemci(k).GetAsync($"/api/v1/urunler/stok-adetleri?ids={fazla}");
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }

    /// <summary>
    /// Önbellek kimliği GERÇEK sunucu jetonundan çözülür: aynı kullanıcı + kiracı,
    /// farklı ŞUBE → farklı kimlik (şube değişince bakiye taşıyan /kasalar düşer);
    /// farklı KİRACI → farklı kimlik; aynı iddialarla yeniden üretilen jeton → AYNI kimlik.
    /// Beklenen değerler elle yazılmıştır.
    /// </summary>
    [Fact]
    public void S9_onbellek_kimligi_gercek_jetonda_sube_ve_kiraci_ayirir()
    {
        var ts = new Kuyumcu.Api.Auth.TokenServisi(new Kuyumcu.Api.Auth.JwtAyarlar
        {
            Secret = HttpOrtami.TestSirri, AccessDakika = 15, RefreshGun = 7,
        });
        var a1 = ListeOnbellegi.KimlikCoz(ts.AccessUret("U1", "T1", "S1", "SAHIP"));
        var a2 = ListeOnbellegi.KimlikCoz(ts.AccessUret("U1", "T1", "S1", "SAHIP"));
        var sube = ListeOnbellegi.KimlikCoz(ts.AccessUret("U1", "T1", "S2", "SAHIP"));
        var kiraci = ListeOnbellegi.KimlikCoz(ts.AccessUret("U1", "T2", "S1", "SAHIP"));

        Assert.Equal("U1|T1|S1|SAHIP", a1);
        Assert.Equal(a1, a2);
        Assert.Equal("U1|T1|S2|SAHIP", sube);
        Assert.Equal("U1|T2|S1|SAHIP", kiraci);
    }

    [Fact]
    public void S10_baska_kimlikle_onbellek_ve_429_yedegi_SIZMAZ()
    {
        var o = new ListeOnbellegi();
        var n = o.NesilAl("U1|T1|S1|SAHIP");
        o.Koy("/kasalar", "U1|T1|S1|SAHIP", "[{\"bakiye\":1000}]", n);
        Assert.True(o.TryAl("/kasalar", "U1|T1|S1|SAHIP", out _));

        Assert.False(o.TryAl("/kasalar", "U1|T1|S2|SAHIP", out _));
        Assert.False(o.SonIyiAl("/kasalar", "U1|T1|S2|SAHIP", TimeSpan.FromMinutes(5), out _));
        // Eski kimliğe dönülse bile kayıt geri gelmez (kimlik değişimi temizledi).
        Assert.False(o.TryAl("/kasalar", "U1|T1|S1|SAHIP", out _));
    }
}
