using System.Reflection;
using System.Text.Json;
using Kuyumcu.Web.Servisler;
using MFatura = Kuyumcu.Api.Modules.Fatura;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET BELGE-KALAN sınır testleri (01.10.2026).
// Paket fatura TARİHİ (işletme günü) + seri yılı + reyon stok has gösterimine dokundu.
// Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class BELGEKALANSunucuSinirTestleri(TestVeritabani vt)
{
    private static T Prop<T>(object o, string ad) => (T)o.GetType().GetProperty(ad)!.GetValue(o)!;
    private static JsonElement G(string s) => JsonDocument.Parse(s).RootElement;

    private async Task<(string id, MFatura.FaturaServisi s, Kuyumcu.Api.Data.KuyumcuDbContext db, Kiraci k)> Taslak(string ad, DateTime an)
    {
        var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, ad);
        var db = vt.Olustur(k);
        var s = new MFatura.FaturaServisi(db);
        var id = Prop<string>(await s.Olustur(new MFatura.FaturaOlusturDto
            { CariId = k.CariId, Toplam = 1_000m, KdvOran = 0.20m, Tarih = an }, k.Islem), "Id");
        return (id, s, db, k);
    }

    private DateTime Oku(string id)
    {
        using var o = vt.OlusturSistem();
        return DateTime.SpecifyKind(o.Faturalar.Single(f => f.Id == id).Tarih, DateTimeKind.Utc);
    }

    /// <summary>Sınır: 20:59:59Z (23:59:59 TSI) → işletme günü 30.09; aynı gün PATCH kaydı değiştirmez.</summary>
    [Fact]
    public async Task DGR_BK_2359_TSI_ayni_gun_kayit_degismez()
    {
        var an = new DateTime(2026, 9, 30, 20, 59, 59, DateTimeKind.Utc);
        var (id, s, _, k) = await Taslak("DGR-BK-2359", an);
        await s.TaslakGuncelle(id, G("""{"tarih":"2026-09-30"}"""), k.Islem);
        Assert.Equal(an, Oku(id));
    }

    /// <summary>Sınır: 21:00:00Z (00:00 TSI) → işletme günü 01.10; "2026-10-01" kaydı değiştirmez, "2026-09-30" ise 30.09 00:00Z yazar.</summary>
    [Fact]
    public async Task DGR_BK_0000_TSI_gun_sinirinda_yerel_gun()
    {
        var an = new DateTime(2026, 9, 30, 21, 0, 0, DateTimeKind.Utc);
        var (id, s, _, k) = await Taslak("DGR-BK-0000", an);
        await s.TaslakGuncelle(id, G("""{"tarih":"2026-10-01"}"""), k.Islem);
        Assert.Equal(an, Oku(id));
        await s.TaslakGuncelle(id, G("""{"tarih":"2026-09-30"}"""), k.Islem);
        Assert.Equal(new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc), Oku(id));
    }

    /// <summary>Boş/null/geçersiz tarih: 400, kayıt korunur (null = silme YASAK).</summary>
    [Theory]
    [InlineData("""{"tarih":null}""")]
    [InlineData("""{"tarih":""}""")]
    [InlineData("""{"tarih":"30.09.2026"}""")]
    [InlineData("""{"tarih":"2026-02-30"}""")]
    [InlineData("""{"tarih":20260930}""")]
    public async Task DGR_BK_gecersiz_tarih_reddedilir_kayit_korunur(string govde)
    {
        var an = new DateTime(2026, 9, 30, 22, 30, 0, DateTimeKind.Utc);
        var (id, s, _, k) = await Taslak("DGR-BK-gecersiz", an);
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => s.TaslakGuncelle(id, G(govde), k.Islem));
        Assert.Equal(an, Oku(id));
    }

    /// <summary>Seri yılı sınırı: 31.12 20:59:59Z (23:59:59 TSI) → o yılın serisi (yanlış yöne kayma yok).</summary>
    /// <remarks>PAKET GOC-A (envanter açık #9): fatura tarihi artık İLERİ OLAMAZ (K8a) — sınır, geçmiş yılbaşı
    /// (31.12.2025, bugünden 1 yıl içinde) ile aynen sınanır; 2026-12-31 bugünden ileri olduğu için 400 alırdı.</remarks>
    [Fact]
    public async Task DGR_BK_seri_yili_yilbasi_oncesi_2025()
    {
        var (id, s, _, k) = await Taslak("DGR-BK-seri", new DateTime(2025, 12, 31, 20, 59, 59, DateTimeKind.Utc));
        await s.DurumGuncelle(id, new MFatura.DurumGuncelleDto { Durum = "KESILDI" }, k.Islem);
        using var o = vt.OlusturSistem();
        Assert.Contains("-2025-", o.Faturalar.Single(f => f.Id == id).FaturaNo);
    }
}

public sealed class BELGEKALANIstemciSinirTestleri
{
    private static DateTime BelgeGunu(DateTime t) =>
        (DateTime)typeof(Kuyumcu.Web.Pages.Faturalar)
            .GetMethod("BelgeGunu", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
            .Invoke(null, [t])!;

    /// <summary>İstemci günü = Bicim.Tarih ile aynı kural (ToLocalTime). Unspecified (API'nin 'Z'siz
    /// JSON'u) UTC sayılır — liste ile çekmece aynı günü gösterir.</summary>
    [Theory]
    [InlineData("2026-09-30T20:59:59")]
    [InlineData("2026-09-30T21:00:00")]
    [InlineData("2026-09-30T22:30:00")]
    [InlineData("2026-12-31T22:30:00")]
    public void DGR_BK_istemci_gunu_liste_gunuyle_ayni(string ham)
    {
        var t = DateTime.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(DateTimeKind.Unspecified, t.Kind);
        // TOPLU-1 tur 2 (TARIH-TEK): tek kalıp dd.MM.yyyy.
        Assert.Equal(Bicim.Tarih(t, "tr-TR"), BelgeGunu(t).ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture));
        if (TimeZoneInfo.Local.Id is "Europe/Istanbul" or "Turkey Standard Time")
        {
            var beklenen = ham switch
            {
                "2026-09-30T20:59:59" => new DateTime(2026, 9, 30),
                "2026-12-31T22:30:00" => new DateTime(2027, 1, 1),
                _ => new DateTime(2026, 10, 1),
            };
            Assert.Equal(beklenen, BelgeGunu(t));
        }
    }

    /// <summary>Reyon stok satırındaki has: 4 hane, tr-TR, sıfır/eksi/yuvarlama/büyük.</summary>
    [Theory]
    [InlineData("0", "0,0000 has")]
    [InlineData("-1.5", "-1,5000 has")]
    [InlineData("0.00005", "0,0001 has")]
    [InlineData("0.00004", "0,0000 has")]
    [InlineData("159.2652", "159,2652 has")]
    [InlineData("999999999.9999", "999.999.999,9999 has")]
    public void DGR_BK_reyon_has_bicimi(string girdi, string beklenen)
        => Assert.Equal(beklenen, Bicim.Has(decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture)));
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI tur 2 (01.10.2026) — gram 2 hane (gider pusulası detay/liste),
// kalem penceresi kuruş iki hane (TlKurus), işletme günü sınırı (tarayıcıdan bağımsız),
// Türkçe sayı girişi (FormAlani.SayiCoz). Beklenen değerler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class BELGEKALANTur2SinirTestleri
{
    /// <summary>Gider pusulası gram gösterimi (BelgeSayisi 2 hane, tr-TR): sıfır, eksi, 0,005 sınırı
    /// (AwayFromZero), 4 haneli gram, çok büyük.</summary>
    [Theory]
    [InlineData("0", "0,00")]
    [InlineData("-1.5", "-1,50")]
    [InlineData("4.665", "4,67")]
    [InlineData("4.664", "4,66")]
    [InlineData("0.005", "0,01")]
    [InlineData("0.0049", "0,00")]
    [InlineData("4.6716", "4,67")]
    [InlineData("999999999.99", "999.999.999,99")]
    public void DGR_BK2_gram_iki_hane(string girdi, string beklenen)
        => Assert.Equal(beklenen, Bicim.BelgeSayisi(decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture), "tr-TR", 2));

    /// <summary>Kalem penceresi para hücresi (TlKurus): kuruş her zaman iki hane.</summary>
    [Theory]
    [InlineData("0", "0,00 ₺")]
    [InlineData("500", "500,00 ₺")]
    [InlineData("0.005", "0,01 ₺")]
    [InlineData("0.0049", "0,00 ₺")]
    [InlineData("999999999.99", "999.999.999,99 ₺")]
    public void DGR_BK2_kalem_para_kurus_iki_hane(string girdi, string beklenen)
        => Assert.Equal(beklenen, Bicim.TlKurus(decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture)));

    /// <summary>Eksi tutar: işaret korunur, kuruş iki hane (biçim içeriği sabit değil, işaret ve hane sınanır).</summary>
    [Fact]
    public void DGR_BK2_kalem_para_eksi_isaret()
    {
        var s = Bicim.TlKurus(-1234.5m);
        Assert.Contains("1.234,50", s);
        Assert.True(s.Contains('-') || s.Contains('−'), s);
    }

    /// <summary>İşletme günü sınırı makine saat diliminden BAĞIMSIZ: 20:59:59Z = 23:59:59 TSI (aynı gün),
    /// 21:00:00Z = 00:00 TSI (ertesi gün), yılbaşı. Kind Utc ve Unspecified aynı sonucu verir.</summary>
    [Theory]
    [InlineData("2026-09-30T20:59:59", "30.09.2026")]
    [InlineData("2026-09-30T21:00:00", "01.10.2026")]   // TOPLU-1 tur 2: tek kalıp (eskiden "1.10.2026")
    [InlineData("2026-12-31T20:59:59", "31.12.2026")]
    [InlineData("2026-12-31T21:00:00", "01.01.2027")]
    public void DGR_BK2_isletme_gunu_sinir(string ham, string beklenen)
    {
        var t = DateTime.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.Tarih(t, "tr-TR"));
        Assert.Equal(beklenen, Bicim.Tarih(DateTime.SpecifyKind(t, DateTimeKind.Utc), "tr-TR"));
    }

    /// <summary>Türkçe sayı girişi: '45.000' = 45000, '1,5' = 1,5, '1.500' belirsiz değil (binlik) ya da
    /// reddedilir; boş = Bos; '1,2,3' geçersiz.</summary>
    [Fact]
    public void DGR_BK2_turkce_sayi_girisi()
    {
        var a = Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("45.000");
        Assert.Equal(45000m, a.Deger);
        Assert.Equal(1.5m, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(0.0001m, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("0,0001").Deger);
        Assert.Equal(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Bos, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Bos, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(null).Sonuc);
        Assert.NotEqual(Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Tamam, Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("1,2,3").Sonuc);
        var b = Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz("1.500");
        // belirsiz '1.500': ya Belirsiz olarak reddedilir ya da (tr-TR binlik) 1500 olur — ASLA 1,5 değil.
        Assert.True(b.Sonuc == Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiSonucu.Belirsiz || b.Deger == 1500m, $"{b.Sonuc} {b.Deger}");
        Assert.NotEqual(1.5m, b.Deger);
    }
}
