using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Core.Domain;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-02 — etiket şablonu (çözümleme, ibare zorunluluğu), ZPL çıktısı, QR matrisi; sunucu şablon ucu.
// QR'ın OKUNABİLİRLİĞİ tarayıcıda (BarcodeDetector) ölçüldü — .NET'te çözücü yok; burada yapı denetlenir.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class D1EtiketCekirdekTestleri
{
    [Fact]
    public void Sablon_yazilir_cozulur_ibare_her_zaman_eklenir_bozuk_metin_varsayilana_duser()
    {
        var s = new EtiketSablonu(58, 40, [EtiketSablonu.Ad, EtiketSablonu.Qr, EtiketSablonu.Fiyat]);
        var c = EtiketSablonu.Coz(s.Yaz());
        Assert.Equal(58, c.GenislikMm);
        Assert.Equal([EtiketSablonu.Ad, EtiketSablonu.Ibare, EtiketSablonu.Qr, EtiketSablonu.Fiyat], c.Alanlar);
        Assert.Equal(EtiketSablonu.Varsayilan, EtiketSablonu.Coz("bozuk"));
        Assert.Equal(EtiketSablonu.Varsayilan.Alanlar, EtiketSablonu.Coz("99x99|AD").Alanlar);   // hazır boy değil
        Assert.DoesNotContain("XYZ", EtiketSablonu.Coz("50x30|AD,XYZ,AD").Alanlar);
        Assert.True(EtiketSablonu.Varsayilan.Yaz().Length <= 200);
        Assert.True(new EtiketSablonu(50, 30, EtiketSablonu.TumAlanlar).Yaz().Length <= 200);
    }

    [Fact]
    public void Zpl_sablon_sirasiyla_barkod_qr_ve_temiz_metin_uretir()
    {
        var s = new EtiketSablonu(50, 30, [EtiketSablonu.Ad, EtiketSablonu.Ibare, EtiketSablonu.Barkod, EtiketSablonu.Qr, EtiketSablonu.Fiyat]);
        var z = EtiketZpl.Uret(s, [new EtiketVerisi("Gram ^Altın~", null, "Altın 995", "24 · 1 g", "8690000000111", "G1", "3.200,00 ₺")]);
        Assert.StartsWith("^XA^CI28^PW400^LL240", z);
        Assert.Contains("^BCN,", z);
        Assert.Contains("^FD8690000000111^FS", z);
        Assert.Contains("^BQN,2,3^FDMA,8690000000111^FS", z);
        Assert.Contains("Gram  Altın ", z);                    // ^ ~ veride komut sayılmaz
        Assert.Contains("3.200,00 ₺", z);
        Assert.True(z.IndexOf("Gram", StringComparison.Ordinal) < z.IndexOf("^BCN", StringComparison.Ordinal));
        Assert.EndsWith("^XZ\n", z);
    }

    [Fact]
    public void Qr_surum_boyutu_bulucu_desen_ve_sinir()
    {
        var m1 = QrKod.Matris("G1");
        Assert.Equal(21, m1.GetLength(0));                       // sürüm 1
        Assert.True(m1[0, 0] && m1[0, 6] && m1[6, 0] && !m1[1, 1] && m1[2, 2]);   // sol üst bulucu
        Assert.True(m1[13, 8]);                                  // her zaman koyu modül (8, n−8)
        Assert.Equal(49, QrKod.Matris(new string('x', 150)).GetLength(0));   // sürüm 8 (154 veri sözcüğü)
        Assert.Equal(57, QrKod.Matris(new string('x', 200)).GetLength(0));   // sürüm 10
        Assert.Throws<ArgumentException>(() => QrKod.Matris(new string('x', 214)));
        Assert.Equal(QrKod.Svg("8690000000111"), QrKod.Svg("8690000000111"));   // belirlenimci
    }
}

[Collection("http")]
public sealed class D1EtiketSablonuHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Sablon_varsayilan_gelir_sahip_yazar_tezgahtar_yazamaz_ibare_korunur()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1ETIKET");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var v = await s.GetFromJsonAsync<EtiketSablonu>("/api/v1/urunler/etiket-sablonu");
        Assert.Equal(EtiketSablonu.Varsayilan.Alanlar, v!.Alanlar);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync("/api/v1/urunler/etiket-sablonu", new { genislikMm = 99, yukseklikMm = 30, alanlar = new[] { "AD" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync("/api/v1/urunler/etiket-sablonu", new { genislikMm = 50, yukseklikMm = 30, alanlar = new[] { "AD", "LOGO" } })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").PutAsJsonAsync("/api/v1/urunler/etiket-sablonu", new { genislikMm = 58, yukseklikMm = 40, alanlar = new[] { "AD" } })).StatusCode);
        var r = await s.PutAsJsonAsync("/api/v1/urunler/etiket-sablonu", new { genislikMm = 58, yukseklikMm = 40, alanlar = new[] { "QR", "AD", "FIYAT" } });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var g = await f.Ortam.Istemci(k, "TEZGAHTAR").GetFromJsonAsync<EtiketSablonu>("/api/v1/urunler/etiket-sablonu");
        Assert.Equal(58, g!.GenislikMm);
        Assert.Equal(["QR", "IBARE", "AD", "FIYAT"], g.Alanlar);
    }
}
