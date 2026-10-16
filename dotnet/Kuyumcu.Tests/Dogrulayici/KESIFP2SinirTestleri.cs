using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using MCari = Kuyumcu.Api.Modules.Cari;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET KESIF-P2 FİNANSAL SINIR TESTLERİ.
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT (üretim kodundan
// hesaplanmaz). Kapsam: cari ekstre satır has etkisi (CariHasEtkisi /
// HasBakiyesineGirer), has gösterimi (Bicim.Has, 4 hane), uyruk normalleme
// (UyrukNormalle), vade girişinin Türkçe sayı çözümü (FormAlani.SayiCoz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KESIFP2SinirTestleri
{
    private static readonly string[] Turler =
    [
        "VERESIYE", "TAHSILAT", "CARI_DUZELTME", "ACILIS", "EMANET_GIRIS", "EMANET_CIKIS",
        "SATIS", "ALIS", "SATIS_IADE", "ALIS_IADE", "HURDA_ALIS", "TAKAS", "ISCILIK", "EVRAK_GIRIS",
    ];
    private static readonly string?[] Madenler = ["ALTIN", "GUMUS", "PLATIN", null];

    [Fact]
    public void Sinir_sifir_has_etkisi_sifir()
    {
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("TAHSILAT", 0m, "ALTIN", false));
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", 0m, "ALTIN", false));
    }

    [Fact]
    public void Sinir_eksi_isaret_K4_terslenir()
    {
        // VERESIYE: hareket HasGrami −2,5 (altın bizden gitti) → cari has bakiyesi +2,5.
        Assert.Equal(2.5m, MCari.CariServisi.CariHasEtkisi("VERESIYE", -2.5m, "ALTIN", false));
        // Altın emanet girişi +9,16 → bakiye −9,16 (müşteriye borcumuz).
        Assert.Equal(-9.16m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", 9.16m, "ALTIN", false));
        // Altın emanet çıkışı −3 → bakiye +3.
        Assert.Equal(3m, MCari.CariServisi.CariHasEtkisi("EMANET_CIKIS", -3m, "ALTIN", false));
    }

    [Fact]
    public void Sinir_has_dort_hane_kirpilmaz()
    {
        Assert.Equal(-0.0001m, MCari.CariServisi.CariHasEtkisi("CARI_DUZELTME", 0.0001m, "ALTIN", false));
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal("-0,3333 has", Bicim.Has(-0.3333m));
    }

    [Fact]
    public void Sinir_yuvarlama_besinci_hane_AwayFromZero()
    {
        // 0,00005 has → 4 haneye AwayFromZero: 0,0001 (sıfırda saklanmaz).
        Assert.Equal("0,0001 has", Bicim.Has(0.00005m));
        Assert.Equal("-0,0001 has", Bicim.Has(-0.00005m));
        Assert.Equal("0,0000 has", Bicim.Has(0.00004m));
    }

    [Fact]
    public void Sinir_cok_buyuk_tutar_tasmaz()
    {
        Assert.Equal(-999_999_999.9999m, MCari.CariServisi.CariHasEtkisi("TAHSILAT", 999_999_999.9999m, "ALTIN", false));
        Assert.Equal(999_999_999.99m, MCari.CariServisi.CariHasEtkisi("VERESIYE", -999_999_999.99m, "ALTIN", false));
    }

    [Fact]
    public void Sinir_has_bakiyesine_girmeyen_satir_toplama_katilmaz()
    {
        // Değişmez: "girmez" denen satırın etkisi 0 olmalı (rozet ile sayı çelişmez).
        foreach (var t in Turler)
        foreach (var m in Madenler)
        foreach (var s in new[] { false, true })
        {
            var etki = MCari.CariServisi.CariHasEtkisi(t, 12.3456m, m, s);
            var girer = MCari.CariServisi.HasBakiyesineGirer(t, m, s);
            if (!girer) Assert.True(etki == 0m, $"{t}/{m ?? "null"}/storno={s}: girmez ama etki {etki}");
            else Assert.Equal(-12.3456m, etki);
        }
        // Satış/alış/hurda has taşısa da has bakiyesine dokunmaz.
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("SATIS", -6.412m, "ALTIN", false));
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("HURDA_ALIS", 10.764m, "ALTIN", false));
        // Gümüş emanet (storno değil) altın hesabına girmez.
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", 92.5m, "GUMUS", false));
        Assert.False(MCari.CariServisi.HasBakiyesineGirer("EMANET_GIRIS", "GUMUS", false));
    }

    [Fact]
    public void Sinir_uyruk_bos_null_ve_normalleme()
    {
        Assert.Null(MCari.CariServisi.UyrukNormalle(null, null));
        Assert.Null(MCari.CariServisi.UyrukNormalle("   ", "TUR"));
        Assert.Equal("TUR", MCari.CariServisi.UyrukNormalle(" tur ", null));
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("TR", null));
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("TURK", null));
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("ZZZ", null));
        // Kayıttaki eşlenemeyen eski değer DEĞİŞMEDEN gelirse aynen korunur (uydurulmaz).
        Assert.Equal("ZZ", MCari.CariServisi.UyrukNormalle("zz", "ZZ"));
        // Ama eski değerden FARKLI geçersiz değer yine reddedilir.
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("YY", "ZZ"));
    }

    [Fact]
    public void Sinir_vade_turkce_giris()
    {
        var kesir = FormAlani.SayiCoz("1,5");
        Assert.Equal(FormAlani.SayiSonucu.Tamam, kesir.Sonuc);
        Assert.Equal(1.5m, kesir.Deger);
        Assert.NotEqual(decimal.Truncate(kesir.Deger!.Value), kesir.Deger!.Value);   // tam değil → vade reddi

        Assert.Equal(15m, FormAlani.SayiCoz("15").Deger);
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
    }

    // ── Doğrulayıcı tur 3 (2026-09-30 18:00) — ek sınırlar, beklenenler SABİT ──

    [Fact]
    public void Sinir3_tahsilat_eksi_has_isareti_terslenir()
    {
        // Eksi has'lı tahsilat satırı (defterde −1,2345) cari has bakiyesini +1,2345 oynatır.
        Assert.Equal(1.2345m, MCari.CariServisi.CariHasEtkisi("TAHSILAT", -1.2345m, "ALTIN", false));
        // PAKET CIFT-BACAK (ekip oyu 30.09, HATA-1): storno satırı da KAYNAĞIN madenine bağlıdır —
        // gümüş emanet stornosu altın has kolonuna yazmaz (eski "madenden bağımsız" kural hataydı).
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", 92.5m, "GUMUS", true));
        Assert.False(MCari.CariServisi.HasBakiyesineGirer("EMANET_GIRIS", "GUMUS", true));
        // Maden NULL (K20 öncesi eski emanet satırı) → etki 0, girmez.
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", 5m, null, false));
    }

    [Fact]
    public void Sinir3_has_gosterimi_buyuk_ve_4_hane()
    {
        Assert.Equal("999.999.999,9999 has", Bicim.Has(999_999_999.9999m));
        Assert.Equal("-114,9580 has", Bicim.Has(-114.958m));
        Assert.Equal("0,0000 has", Bicim.Has(0m));
        // 0,005 sınırı: kuruş biçimi AwayFromZero (0,005 → 0,01).
        Assert.Equal("0,0050 has", Bicim.Has(0.005m));
    }

    [Fact]
    public void Sinir3_uyruk_tek_harf_kucuk_ve_bosluk()
    {
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("T", null));
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("tr", null));
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("T R", null));
        Assert.Equal("DEU", MCari.CariServisi.UyrukNormalle("deu", "ZZ"));
        Assert.Equal("TUR", MCari.CariServisi.UyrukNormalle("TUR", "TUR"));
    }

    [Fact]
    public void Sinir3_migration_eslemesi_TR_TUR_ZZ_yok_ve_tekil()
    {
        var ciftler = Kuyumcu.Api.Data.Migrations.UyrukAlpha3.DonukCiftler();
        Assert.Equal(249, ciftler.Count);
        Assert.Equal(249, ciftler.Select(c => c.A2).Distinct().Count());
        Assert.Equal(249, ciftler.Select(c => c.A3).Distinct().Count());
        Assert.Contains(("TR", "TUR"), ciftler);
        Assert.Contains(("DE", "DEU"), ciftler);
        Assert.Contains(("GB", "GBR"), ciftler);
        Assert.DoesNotContain(ciftler, c => c.A2 == "ZZ" || c.A2 == "UK" || c.A2 == "EU");
        var sql = Kuyumcu.Api.Data.Migrations.UyrukAlpha3.EslemeSql();
        Assert.Contains("WHEN 'TR' THEN 'TUR'", sql);
        Assert.DoesNotContain("'ZZ'", sql);
        // Günlük satırı kişisel değer (uyruk) taşımaz: detay metni sabit iki cümleden biri.
        Assert.DoesNotContain("CONCAT", sql, StringComparison.OrdinalIgnoreCase);
    }

    // ── Doğrulayıcı tur 4 (2026-09-30 19:50, pKESIF-P2) — karar 3 (emanet bakiyeye girmez) ayrışımı,
    //    storno aynası, uyruk kapısı. Beklenenler SABİT, elle hesaplandı. ──

    [Fact]
    public void Sinir4_emanet_ekran_etkisi_her_madende_sifir_kayitli_ayna_korunur()
    {
        foreach (var m in new string?[] { "ALTIN", "GUMUS", "PLATIN", null })
        foreach (var st in new[] { false, true })
        {
            Assert.Equal(0m, MCari.CariServisi.BakiyeHasEtkisi("EMANET_GIRIS", 9.16m, m, st));
            Assert.Equal(0m, MCari.CariServisi.BakiyeHasEtkisi("EMANET_CIKIS", -3m, m, st));
        }
        // Kayıtlı ayna: altın emanet −9,16; gümüş emanet 0; gümüş emanet STORNOSU (satır −100) de 0
        // (PAKET CIFT-BACAK HATA-1: storno kaynağın madenine göre tersler — EmanetKurali ortak kural).
        Assert.Equal(-9.16m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", 9.16m, "ALTIN", false));
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", 100m, "GUMUS", false));
        Assert.Equal(0m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", -100m, "GUMUS", true));
        Assert.Equal(9.16m, MCari.CariServisi.CariHasEtkisi("EMANET_GIRIS", -9.16m, "ALTIN", true));
        // Emanet dışı türde iki fonksiyon aynı.
        Assert.Equal(-1.2345m, MCari.CariServisi.BakiyeHasEtkisi("TAHSILAT", 1.2345m, "ALTIN", false));
        Assert.Equal(0m, MCari.CariServisi.BakiyeHasEtkisi("SATIS", -10m, "ALTIN", false));
    }

    [Fact]
    public void Sinir4_karisik_tohum_ayrisimi_elle_hesaplanan_sabitlerle()
    {
        // (tür, satır hasGrami, maden, storno) — veresiye 2,5 · tahsilat 1,2345 · düzeltme −0,3333 ·
        // altın emanet giriş 9,16 · altın emanet çıkış 3 · gümüş emanet 100 · satış (has 9,16, etkisiz).
        var satirlar = new (string T, decimal H, string? M, bool S)[]
        {
            ("VERESIYE", -2.5m, "ALTIN", false), ("TAHSILAT", 1.2345m, "ALTIN", false),
            ("CARI_DUZELTME", 0.3333m, "ALTIN", false), ("EMANET_GIRIS", 9.16m, "ALTIN", false),
            ("EMANET_CIKIS", -3m, "ALTIN", false), ("EMANET_GIRIS", 100m, "GUMUS", false),
            ("SATIS", -9.16m, "ALTIN", false),
        };
        var kayitli = satirlar.Sum(x => MCari.CariServisi.CariHasEtkisi(x.T, x.H, x.M, x.S));
        var emanetsiz = satirlar.Sum(x => MCari.CariServisi.BakiyeHasEtkisi(x.T, x.H, x.M, x.S));
        Assert.Equal(-5.2278m, kayitli);        // 2,5 − 1,2345 − 0,3333 − 9,16 + 3
        Assert.Equal(0.9322m, emanetsiz);       // 2,5 − 1,2345 − 0,3333
        Assert.Equal(6.16m, emanetsiz - kayitli); // "Emanette" = 9,16 − 3
    }

    [Fact]
    public void Sinir4_uyruk_iki_harf_red_uc_harf_kabul_eski_deger_korunur()
    {
        var ex = Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("TR", null));
        Assert.Contains("3 harfli", ex.Message);
        Assert.Equal("TUR", MCari.CariServisi.UyrukNormalle(" tur ", null));
        Assert.Null(MCari.CariServisi.UyrukNormalle("   ", "ZZ"));
        Assert.Equal("ZZ", MCari.CariServisi.UyrukNormalle("zz", "ZZ"));      // eşlenemeyen eski değer uydurulmaz
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("DE", "ZZ"));
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("TURK", null));
    }

    [Fact]
    public void Sinir4_vade_sifir_ve_belirsiz_binlik()
    {
        Assert.Equal(0m, FormAlani.SayiCoz("0").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
        Assert.Equal(0.005m, FormAlani.SayiCoz("0,005").Deger);
    }
}
