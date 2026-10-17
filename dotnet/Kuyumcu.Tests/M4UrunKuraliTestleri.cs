using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M4 — ürün kuralları (müşteri 16.10: "içinde bilezik geçiyorsa binde bir… bilezik ise
// şu kadar, kolyeyse bu kadar, tek tek yapma").
// ═════════════════════════════════════════════════════════════════════════════

public sealed class M4UrunKuraliSafTestleri
{
    private static UrunKurali K(string? ad = "bilezik", string? kat = null) => new()
    {
        Id = "k", TenantId = "t", Ad = "Bilezikler", AdIcerir = ad, KategoriId = kat,
        AlisMilyemi = 925m, SatisMilyemi = 940m, KdvMatrahBinde = 1m, KdvOrani = 20m,
    };

    [Theory]
    [InlineData("22 Ayar Burma Bilezik", true)]
    [InlineData("BİLEZİK KELEPÇE", true)]           // Türkçe büyük İ
    [InlineData("22 ayar bilezik", true)]
    [InlineData("Zincir Kolye", false)]
    public void Ad_kurali_Turkce_harf_duyarsiz_icerir(string ad, bool beklenen) =>
        Assert.Equal(beklenen, UrunKuraliServisi.Uyar(K(), ad, null));

    [Fact]
    public void Kategori_ve_ad_birlikteyse_ikisi_de_uymali()
    {
        var k = K("bilezik", "kat1");
        Assert.True(UrunKuraliServisi.Uyar(k, "Burma Bilezik", "kat1"));
        Assert.False(UrunKuraliServisi.Uyar(k, "Burma Bilezik", "kat2"));
        Assert.False(UrunKuraliServisi.Uyar(k, "Kolye", "kat1"));
    }

    [Fact]
    public void Kosulsuz_kural_hicbir_seye_uymaz() =>
        Assert.False(UrunKuraliServisi.Uyar(K(null, null), "Bilezik", "kat"));

    [Fact]
    public void Doldur_yalniz_bos_alanlari_doldurur_elle_girilen_kazanir()
    {
        var u = new Urun { Id = "u", TenantId = "t", Tip = "ZIYNET", Kod = "B", Ad = "Bilezik", AyarId = "K22", AlisMilyemi = 930m };
        var alanlar = UrunKuraliServisi.Doldur(u, K());
        Assert.Equal(930m, u.AlisMilyemi);                 // elle girilen korunur
        Assert.Equal(940m, u.SatisMilyemi);
        Assert.Equal(1m, u.KdvMatrahBinde);
        Assert.Equal(20m, u.KdvOrani);
        Assert.DoesNotContain("alisMilyemi", alanlar);
    }

    [Fact]
    public void Ustune_yaz_modunda_mevcut_degerler_degisir()
    {
        var u = new Urun { Id = "u", TenantId = "t", Tip = "ZIYNET", Kod = "B", Ad = "Bilezik", AyarId = "K22", AlisMilyemi = 930m };
        UrunKuraliServisi.Doldur(u, K(), yalnizBos: false);
        Assert.Equal(925m, u.AlisMilyemi);
    }

    [Fact]
    public void Gumus_urunde_milyem_ve_binde_UYGULANMAZ_KDV_orani_uygulanir()
    {
        var u = new Urun { Id = "u", TenantId = "t", Tip = "ZIYNET", Kod = "G", Ad = "Gümüş Bilezik", AyarId = "G925" };
        UrunKuraliServisi.Doldur(u, K());
        Assert.Null(u.AlisMilyemi);
        Assert.Null(u.SatisMilyemi);
        Assert.Null(u.KdvMatrahBinde);
        Assert.Equal(20m, u.KdvOrani);
    }
}

[Collection("veritabani")]
public sealed class M4UrunKuraliVtTestleri(TestVeritabani vt)
{
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    [Fact]
    public async Task Yeni_urun_bos_alanlarini_uyan_kuraldan_alir_elle_girilen_kazanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "M4-yeni");
        using var db = vt.Olustur(k);
        await new UrunKuraliServisi(db).Olustur(new UrunKuraliDto
        {
            Ad = "Bilezikler", AdIcerir = "bilezik", AlisMilyemi = 925m, KdvMatrahBinde = 1m,
        }, k.Islem);

        await new UrunServisi(db).Olustur(new UrunOlusturDto
        { Tip = "ZIYNET", Kod = "M4-A", Ad = "Kelepçe BİLEZİK", AyarId = "K22", GramAgirlik = 10m }, k.Islem);
        await new UrunServisi(db).Olustur(new UrunOlusturDto
        { Tip = "ZIYNET", Kod = "M4-B", Ad = "Burma Bilezik", AyarId = "K22", GramAgirlik = 10m, AlisMilyemi = 930m }, k.Islem);
        await new UrunServisi(db).Olustur(new UrunOlusturDto
        { Tip = "ZIYNET", Kod = "M4-C", Ad = "Zincir Kolye", AyarId = "K22", GramAgirlik = 10m }, k.Islem);

        var u = await db.Urunler.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToDictionaryAsync(x => x.Kod);
        Assert.Equal(925m, u["M4-A"].AlisMilyemi);
        Assert.Equal(1m, u["M4-A"].KdvMatrahBinde);
        Assert.Equal(930m, u["M4-B"].AlisMilyemi);       // elle girilen kazanır
        Assert.Equal(1m, u["M4-B"].KdvMatrahBinde);
        Assert.Null(u["M4-C"].AlisMilyemi);              // kurala uymayan dokunulmaz
    }

    [Fact]
    public async Task Onizleme_ve_uygula_mevcut_urunlerin_bos_alanlarini_doldurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "M4-uygula");
        using var db = vt.Olustur(k);
        foreach (var (kod, ad) in new[] { ("E1", "Hasır M4BİLEZİK"), ("E2", "Burma m4bilezik"), ("E3", "Künye") })
            await new UrunServisi(db).Olustur(new UrunOlusturDto
            { Tip = "ZIYNET", Kod = kod, Ad = ad, AyarId = "K22", GramAgirlik = 5m }, k.Islem);

        var kural = J(await new UrunKuraliServisi(db).Olustur(new UrunKuraliDto
        { Ad = "Bilezik binde", AdIcerir = "m4bilezik", KdvMatrahBinde = 1m }, k.Islem));
        var id = kural.GetProperty("Id").GetString()!;

        var o = J(await new UrunKuraliServisi(db).Onizleme(id, true, k.Islem));
        Assert.Equal(2, o.GetProperty("uyanSayisi").GetInt32());
        Assert.Equal(2, o.GetProperty("degisecekSayisi").GetInt32());

        var r = await new UrunKuraliServisi(db).Uygula(id, true, k.Islem);
        Assert.Equal(2, r.DegisenSayisi);
        var u = await db.Urunler.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToDictionaryAsync(x => x.Kod);
        Assert.Equal(1m, u["E1"].KdvMatrahBinde);
        Assert.Equal(1m, u["E2"].KdvMatrahBinde);
        Assert.Null(u["E3"].KdvMatrahBinde);

        var tekrar = J(await new UrunKuraliServisi(db).Onizleme(id, true, k.Islem));
        Assert.Equal(0, tekrar.GetProperty("degisecekSayisi").GetInt32());   // ikinci uygulama boşa
    }

    [Fact]
    public async Task Kosulsuz_ya_da_degersiz_kural_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "M4-ret");
        using var db = vt.Olustur(k);
        var s = new UrunKuraliServisi(db);
        var e1 = await Assert.ThrowsAsync<BadRequestException>(() => s.Olustur(new UrunKuraliDto { Ad = "x", KdvMatrahBinde = 1m }, k.Islem));
        Assert.Equal("adIcerir", AlanIsareti.Alan(e1));
        var e2 = await Assert.ThrowsAsync<BadRequestException>(() => s.Olustur(new UrunKuraliDto { Ad = "x", AdIcerir = "bilezik" }, k.Islem));
        Assert.Equal("alisMilyemi", AlanIsareti.Alan(e2));
        var e3 = await Assert.ThrowsAsync<BadRequestException>(() => s.Olustur(new UrunKuraliDto { Ad = "x", AdIcerir = "b", KdvMatrahBinde = 0m }, k.Islem));
        Assert.Equal("kdvMatrahBinde", AlanIsareti.Alan(e3));
    }
}
