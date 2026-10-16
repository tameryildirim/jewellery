using Kuyumcu.Api.Data;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 40-A — RİSK MOTORU (SAF). Veritabanı, ağ, saat yok.
//
// İLK TEST BİLİNÇLİ OLARAK "eşik yapılandırmadan gelir"dir (Bertan koşulu):
// paketin en büyük riski puan/eşik rakamlarının koda gömülmesidir (ADR-0005).
// O test kırmızıya dönmeden hiçbir rakam koda yazılamaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RiskMotoruTestleri
{
    private static MMasak.RiskGirdi Girdi(
        bool pep = false, string? uyruk = null, decimal nakitOran = 0m, decimal toplam = 0m,
        int esigeYakin = 0, int kimlikEksik = 0, int supheli = 0, bool yaptirim = false)
        => new("MUSTERI", pep, uyruk, nakitOran, toplam, esigeYakin, kimlikEksik, supheli, yaptirim);

    // ── M40-1: eşikler YAPILANDIRMADAN ───────────────────────────────────────

    [Fact]
    public void OrtaEsik_degistirilince_seviye_degisir_esik_koda_gomulu_degildir()
    {
        // PEP puanı 30. Varsayılan OrtaEsik 40 → DUSUK.
        var varsayilan = new MMasak.RiskMotoru(new MMasak.MasakRiskAyarlari());
        var a = varsayilan.Degerlendir(Girdi(pep: true));
        Assert.Equal(30, a.Puan);
        Assert.Equal(RiskSeviyesi.Dusuk, a.Seviye);

        // AYNI girdi, YALNIZ eşik değişti → ORTA. Rakam koda gömülü olsaydı
        // bu test kırmızıya dönerdi.
        var sikilastirilmis = new MMasak.RiskMotoru(new MMasak.MasakRiskAyarlari { OrtaEsik = 25 });
        var b = sikilastirilmis.Degerlendir(Girdi(pep: true));
        Assert.Equal(30, b.Puan);
        Assert.Equal(RiskSeviyesi.Orta, b.Seviye);
    }

    [Fact]
    public void PepPuani_yapilandirmadan_gelir()
    {
        var motor = new MMasak.RiskMotoru(new MMasak.MasakRiskAyarlari { PepPuan = 7 });
        Assert.Equal(7, motor.Degerlendir(Girdi(pep: true)).Puan);
    }

    // ── Temel davranış ───────────────────────────────────────────────────────

    [Fact]
    public void Sifir_girdi_DUSUK_puan_sifir_sebep_bos()
    {
        var sonuc = new MMasak.RiskMotoru(new MMasak.MasakRiskAyarlari()).Degerlendir(Girdi());

        Assert.Equal(0, sonuc.Puan);
        Assert.Equal(RiskSeviyesi.Dusuk, sonuc.Seviye);
        Assert.Empty(sonuc.Sebepler);
    }

    [Fact]
    public void Pep_beyani_puan_ekler_ve_sebep_listesinde_kod_gecer()
    {
        var ayar = new MMasak.MasakRiskAyarlari();
        var sonuc = new MMasak.RiskMotoru(ayar).Degerlendir(Girdi(pep: true));

        Assert.Equal(ayar.PepPuan, sonuc.Puan);
        Assert.Contains(MMasak.RiskSebebi.PepBeyani, sonuc.Sebepler);
    }

    [Fact]
    public void Yaptirim_eslesmesi_tek_basina_YUKSEK_yapar()
    {
        // ⚠ FAZ A'da servis bu alanı HER ZAMAN false verir; motor sözleşmesi
        //   yine de sınanır (Faz B bu davranışa yaslanacak).
        var sonuc = new MMasak.RiskMotoru(new MMasak.MasakRiskAyarlari())
            .Degerlendir(Girdi(yaptirim: true));

        Assert.Equal(RiskSeviyesi.Yuksek, sonuc.Seviye);
        Assert.Contains(MMasak.RiskSebebi.YaptirimEslesmesi, sonuc.Sebepler);
    }

    [Fact]
    public void Nakit_agirlik_ve_structuring_birlikte_ORTAdan_YUKSEGE_gecirir()
    {
        var motor = new MMasak.RiskMotoru(new MMasak.MasakRiskAyarlari());

        // PEP (30) + nakit (15) = 45 → ORTA
        var orta = motor.Degerlendir(Girdi(pep: true, nakitOran: 0.95m, toplam: 500_000m));
        Assert.Equal(45, orta.Puan);
        Assert.Equal(RiskSeviyesi.Orta, orta.Seviye);

        // + structuring (25) = 70 → YUKSEK
        var yuksek = motor.Degerlendir(
            Girdi(pep: true, nakitOran: 0.95m, toplam: 500_000m, esigeYakin: 2));
        Assert.Equal(70, yuksek.Puan);
        Assert.Equal(RiskSeviyesi.Yuksek, yuksek.Seviye);
        Assert.Contains(MMasak.RiskSebebi.NakitAgirlik, yuksek.Sebepler);
        Assert.Contains(MMasak.RiskSebebi.EsigeYakin, yuksek.Sebepler);
    }

    [Fact]
    public void Hacim_sifirken_nakit_orani_puan_uretmez()
    {
        // 0 TL'lik dönemde "nakit oranı %100" demek uydurma olurdu.
        var sonuc = new MMasak.RiskMotoru(new MMasak.MasakRiskAyarlari())
            .Degerlendir(Girdi(nakitOran: 1m, toplam: 0m));

        Assert.Equal(0, sonuc.Puan);
        Assert.DoesNotContain(MMasak.RiskSebebi.NakitAgirlik, sonuc.Sebepler);
    }

    // ── M40-2: yüksek riskli ülke listesi BOŞ ────────────────────────────────

    [Fact]
    public void Ulke_listesi_BOSken_uyruk_hic_puan_uretmez()
    {
        var ayar = new MMasak.MasakRiskAyarlari();
        Assert.Empty(ayar.YuksekRiskliUlkeler);

        var sonuc = new MMasak.RiskMotoru(ayar).Degerlendir(Girdi(uyruk: "IRN"));

        Assert.Equal(0, sonuc.Puan);
        Assert.DoesNotContain(MMasak.RiskSebebi.YuksekRiskliUlke, sonuc.Sebepler);
    }

    [Fact]
    public void Ulke_listesi_DOLDURULUNCA_uyruk_puan_uretir()
    {
        var ayar = new MMasak.MasakRiskAyarlari { YuksekRiskliUlkeler = ["IRN"] };
        var sonuc = new MMasak.RiskMotoru(ayar).Degerlendir(Girdi(uyruk: "IRN"));

        Assert.Equal(ayar.UlkeRiskPuan, sonuc.Puan);
        Assert.Contains(MMasak.RiskSebebi.YuksekRiskliUlke, sonuc.Sebepler);
    }

    [Fact]
    public void KimlikEksik_katsayisi_SIFIRKEN_puan_uretmez()
    {
        // Uydurulmuş katsayı yerine varsayılan 0 (Anayasa md.1): kuyumcu açıkça
        // değer vermeden bu kalem skoru sessizce şişirmez.
        var ayar = new MMasak.MasakRiskAyarlari();
        Assert.Equal(0, ayar.KimlikEksikPuan);

        var sonuc = new MMasak.RiskMotoru(ayar).Degerlendir(Girdi(kimlikEksik: 5));
        Assert.Equal(0, sonuc.Puan);
        Assert.DoesNotContain(MMasak.RiskSebebi.KimlikEksik, sonuc.Sebepler);
    }

    [Fact]
    public void Supheli_gecmisi_puan_ekler()
    {
        var ayar = new MMasak.MasakRiskAyarlari();
        var sonuc = new MMasak.RiskMotoru(ayar).Degerlendir(Girdi(supheli: 1));

        Assert.Equal(ayar.SupheliGecmisPuan, sonuc.Puan);
        Assert.Contains(MMasak.RiskSebebi.SupheliGecmis, sonuc.Sebepler);
    }

    // ── M40-3: sebepler YALNIZ kod sabiti (KVKK, Mehmet K40-3) ───────────────

    [Fact]
    public void Sebepler_YALNIZ_kod_sabitidir_kisisel_veri_tasimaz()
    {
        var motor = new MMasak.RiskMotoru(new MMasak.MasakRiskAyarlari
        {
            YuksekRiskliUlkeler = ["IRN"],
            KimlikEksikPuan = 10,
        });
        var sonuc = motor.Degerlendir(new MMasak.RiskGirdi(
            "MUSTERI", true, "IRN", 0.99m, 900_000m, 3, 2, 1, true));

        Assert.All(sonuc.Sebepler, s => Assert.Contains(s, MMasak.RiskSebebi.Hepsi));
        // Kimlik verisi hiçbir sebebin içine sızmamalı.
        Assert.All(sonuc.Sebepler, s =>
        {
            Assert.DoesNotContain("IRN", s);
            Assert.DoesNotContain(" ", s);
        });
    }

    [Fact]
    public void Structuring_asgari_islem_sayisi_altinda_puan_uretmez()
    {
        var ayar = new MMasak.MasakRiskAyarlari();   // StructuringAsgariIslem = 2
        var motor = new MMasak.RiskMotoru(ayar);

        Assert.Equal(0, motor.Degerlendir(Girdi(esigeYakin: 1)).Puan);
        Assert.Equal(ayar.StructuringPuan, motor.Degerlendir(Girdi(esigeYakin: 2)).Puan);
    }

    [Fact]
    public void Band_sinirlari_esikten_ve_oranlardan_hesaplanir()
    {
        // Eşiğin KENDİSİ motorda değişmez; yalnız bandı hesaplanır.
        var motor = new MMasak.RiskMotoru(new MMasak.MasakRiskAyarlari());
        var (alt, ust) = motor.Band(185_000m);

        Assert.Equal(148_000m, alt);
        Assert.Equal(185_000m, ust);
    }
}
