using System.Reflection;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using PanelSz = Kuyumcu.Contracts.Sayfalar.Panel;
using YaslandirmaSz = Kuyumcu.Contracts.Sayfalar.Raporlar;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-2 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT (üretim kodundan
// hesaplanmaz). Kapsam: Panel "Kim borçlu" has gösterim eşiği (0,00005),
// Rapor merkezi "Bugün ne aldım" has satırı / altın dışı maden satırı kararı,
// altın pozisyonu kırılım toplamı ve biçim sınırları; Türkçe giriş gerilemesi.
// ════════════════════════════════════════════════════════════════════════

public sealed class RAPOR2SinirTestleri
{
    private static readonly Type PanelTip = typeof(Kuyumcu.Web.Pages.Panel);
    private static readonly Type RaporlarTip = typeof(Kuyumcu.Web.Pages.Raporlar);

    private static T Cagir<T>(Type t, string ad, params object?[] args)
    {
        var m = t.GetMethod(ad, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException($"{t.Name}.{ad} bulunamadı");
        return (T)m.Invoke(null, args)!;
    }

    private static YaslandirmaSz.YaslananCari Cari(decimal tl, decimal has) =>
        new("c", "C", "MUSTERI", tl, has, 3, "0-30");

    // ── Kim borçlu: has eşiği (sıfır, eşik altı, tam eşik, eksi) ──
    [Fact]
    public void R2S01_Sifir_bakiye_listeye_girmez()
    {
        Assert.False(Cagir<bool>(PanelTip, "Alacak", Cari(0m, 0m)));
        Assert.False(Cagir<bool>(PanelTip, "BizBorcluyuz", Cari(0m, 0m)));
    }

    [Fact]
    public void R2S02_Esik_alti_has_0_000049_girmez_tam_esik_0_00005_girer_ve_0_0001_yazar()
    {
        Assert.False(Cagir<bool>(PanelTip, "Alacak", Cari(0m, 0.000049m)));
        Assert.True(Cagir<bool>(PanelTip, "Alacak", Cari(0m, 0.00005m)));
        Assert.Equal("0,0001 has", Bicim.HasIsaretli(0.00005m));      // AwayFromZero: 0,00005 → 0,0001
        Assert.Equal("0,0000 has", Bicim.HasIsaretli(0.000049m));
    }

    [Fact]
    public void R2S03_Eksi_has_esigi_isaret_dogru()
    {
        Assert.False(Cagir<bool>(PanelTip, "BizBorcluyuz", Cari(0m, -0.000049m)));
        Assert.True(Cagir<bool>(PanelTip, "BizBorcluyuz", Cari(0m, -0.00005m)));
        Assert.False(Cagir<bool>(PanelTip, "Alacak", Cari(0m, -0.00005m)));
        Assert.Equal("−0,0001 has", Bicim.HasIsaretli(-0.00005m));
    }

    // ── TL eşiği kuruş: 0,01 dahil değil, 0,011 dahil (değişmedi) ──
    [Fact]
    public void R2S04_Tl_kurus_esigi()
    {
        Assert.False(Cagir<bool>(PanelTip, "Alacak", Cari(0.01m, 0m)));
        Assert.True(Cagir<bool>(PanelTip, "Alacak", Cari(0.011m, 0m)));
        Assert.True(Cagir<bool>(PanelTip, "BizBorcluyuz", Cari(-0.011m, 0m)));
    }

    // ── Bugün ne aldım: yalnız gümüş → has satırı YOK; boş/null → has satırı (0) yazılır ──
    [Fact]
    public void R2S05_Yalniz_gumus_has_satiri_gereksiz_bos_listede_gerekli()
    {
        var gumus = new List<PanelSz.GunlukMadenPayi> { new("GUMUS", "Gümüş", 37.46m) };
        Assert.False(Cagir<bool>(RaporlarTip, "HasSatiriGerekli", 0m, gumus));
        Assert.True(Cagir<bool>(RaporlarTip, "HasSatiriGerekli", 0m, null));
        Assert.True(Cagir<bool>(RaporlarTip, "HasSatiriGerekli", 0m, new List<PanelSz.GunlukMadenPayi>()));
        Assert.True(Cagir<bool>(RaporlarTip, "HasSatiriGerekli", 0.0001m, gumus));   // 4 hane altın has
    }

    [Fact]
    public void R2S06_Altin_disi_madenler_altin_ve_sifir_ve_bos_maden_ayiklanir()
    {
        var l = new List<PanelSz.GunlukMadenPayi>
        {
            new("ALTIN", "Altın", 22.3046m), new("GUMUS", "Gümüş", 37.46m),
            new("PLATIN", "Platin", 0m), new(null, null, 5m), new("", "", 5m),
        };
        var s = Cagir<List<PanelSz.GunlukMadenPayi>>(RaporlarTip, "AltinDisiMadenler", l);
        Assert.Equal("GUMUS", Assert.Single(s).Maden);
        Assert.Empty(Cagir<List<PanelSz.GunlukMadenPayi>>(RaporlarTip, "AltinDisiMadenler", new object?[] { null }));
    }

    // ── Gümüş gram biçimi: kuruş benzeri 2 hane AwayFromZero sınırı ve çok büyük değer ──
    [Fact]
    public void R2S07_Gumus_saf_gram_2_hane_yuvarlama_ve_buyuk_deger()
    {
        Assert.Equal("saf 0,01 g", Bicim.MadenMiktari("GUMUS", 0.005m, null, KarRaporSozlugu.Tr.MadenGramFizikselSaf, KarRaporSozlugu.Tr.MadenGramSaf));
        Assert.Equal("saf 0,00 g", Bicim.MadenMiktari("GUMUS", 0.0049m, null, KarRaporSozlugu.Tr.MadenGramFizikselSaf, KarRaporSozlugu.Tr.MadenGramSaf));
        Assert.Equal("saf 999.999.999,99 g", Bicim.MadenMiktari("GUMUS", 999_999_999.99m, null, KarRaporSozlugu.Tr.MadenGramFizikselSaf, KarRaporSozlugu.Tr.MadenGramSaf));
    }

    // ── Has 4 hane ve çok büyük has ──
    [Fact]
    public void R2S08_Has_4_hane_ve_buyuk()
    {
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
        Assert.Equal("999.999.999,9999 has", Bicim.Has(999_999_999.9999m));
        Assert.Equal("+999.999.999,9900 has", "+" + Bicim.Has(999_999_999.99m));
    }

    // ── Panel kâr satırı kısa öbek: 0/0, büyük sayı, bölünmez boşluk ──
    [Fact]
    public void R2S09_Eksik_sayi_kisa_sinirlar()
    {
        Assert.Equal("1 satış", KarRaporSozlugu.EksikSayiKisa(KarRaporSozlugu.Tr, 1, 0));
        Assert.Equal("1000 satış, 2 iade", KarRaporSozlugu.EksikSayiKisa(KarRaporSozlugu.Tr, 1000, 2));   // adet sayısı, binlik ayıracı yok (para değil)
        Assert.Contains(' ', KarRaporSozlugu.Tr.PanelKarKismiKisa);
    }

    // ── Türkçe giriş gerilemesi (FormAlani.SayiCoz) ──
    [Fact]
    public void R2S10_Turkce_giris()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// RAPOR-2 TUR 2 — HASIM DOĞRULAYICI (Aşkın + Mehmet, ikinci doğrulama 03.10):
// /raporlar/gunluk `netSatis` ve /raporlar/aylik `netSatisTl` sınırları. Beklenen değerler
// ELLE yazılmış sabitlerdir; ayrıca Kâr-zarar (KarZararServisi) aynı günle çapraz kontrol edilir.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class RAPOR2NetSatisSinirTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    private static Kuyumcu.Api.Data.Hareket H(Kiraci k, string tur, decimal tl, decimal has, string maden = "ALTIN") => new()
    {
        Id = Kuyumcu.Api.Common.Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur,
        Tarih = DateTime.UtcNow, TlTutari = tl, HasGrami = has, Maden = maden,
        ParaBirimi = "TRY", KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
    };

    private async Task<(System.Text.Json.JsonElement n, System.Text.Json.JsonElement kz)> Olc(Kiraci k)
    {
        using var db = vt.Olustur(k);
        var bugun = Kuyumcu.Api.Common.Zaman.YerelBugun();
        var g = J(await TestKurulum.RaporServisi(db).GunlukOzet(bugun.ToString("yyyy-MM-dd"), k.Islem));
        var kz = J(await new Kuyumcu.Api.Modules.Maliyet.KarZararServisi(db).Donem(k.Islem, bugun, bugun, null));
        return (g.GetProperty("netSatis"), kz);
    }

    private static decimal D(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetDecimal();

    [Fact]
    public async Task DR2N01_Sifir_hareketsiz_gun_net_sifir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-2-TEST dg2 n01");
        await sistem.SaveChangesAsync();
        var (n, kz) = await Olc(k);
        Assert.Equal(0, n.GetProperty("satisAdet").GetInt32());
        Assert.Equal(0, n.GetProperty("iadeAdet").GetInt32());
        Assert.Equal(0m, D(n, "toplamTl"));
        Assert.Equal(0m, D(n, "altinToplamHas"));
        Assert.Equal(D(kz, "hasilatTl"), D(n, "toplamTl"));
    }

    [Fact]
    public async Task DR2N02_Eksi_yalniz_iade_gunu_net_eksi_isaret_dogru_ve_kar_zarar_ile_ayni()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-2-TEST dg2 n02");
        sistem.Add(H(k, "SATIS_IADE", -1_000m, 0.5m));
        await sistem.SaveChangesAsync();
        var (n, kz) = await Olc(k);
        Assert.Equal(1_000m, D(n, "iadeTl"));
        Assert.Equal(-1_000m, D(n, "toplamTl"));
        Assert.Equal(-0.5m, D(n, "altinToplamHas"));
        Assert.Equal(D(kz, "hasilatTl"), D(n, "toplamTl"));
        Assert.Equal(D(kz, "altinHasilatHasDefterGram"), D(n, "altinToplamHas"));
    }

    [Fact]
    public async Task DR2N03_Kurus_sinirlari_100_01_eksi_0_01_ve_0_005_kayit_yuvarlamasi()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-2-TEST dg2 n03");
        sistem.Add(H(k, "SATIS", 100.01m, -0.0001m));
        sistem.Add(H(k, "SATIS", 0.005m, 0m));          // kolon 2 hane: kayıtta 0,01'e yuvarlanır (MySQL yarım yukarı)
        sistem.Add(H(k, "SATIS_IADE", -0.01m, 0.0001m));
        await sistem.SaveChangesAsync();
        var (n, kz) = await Olc(k);
        Assert.Equal(100.02m, D(n, "brutTl"));
        Assert.Equal(0.01m, D(n, "iadeTl"));
        Assert.Equal(100.01m, D(n, "toplamTl"));
        Assert.Equal(0m, D(n, "altinToplamHas"));              // 0,0001 − 0,0001
        Assert.Equal(D(kz, "hasilatTl"), D(n, "toplamTl"));
    }

    [Fact]
    public async Task DR2N04_Has_4_hane_0_0001_kaybolmaz()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-2-TEST dg2 n04");
        sistem.Add(H(k, "SATIS", 10m, -0.0001m));
        sistem.Add(H(k, "SATIS", 10m, -5m, "GUMUS"));
        await sistem.SaveChangesAsync();
        var (n, kz) = await Olc(k);
        Assert.Equal(0.0001m, D(n, "altinToplamHas"));
        Assert.Contains("0,0001 has", Kuyumcu.Web.Servisler.Bicim.HasIsaretli(D(n, "altinToplamHas")), StringComparison.Ordinal);
        Assert.Equal(D(kz, "altinHasilatHasDefterGram"), D(n, "altinToplamHas"));
    }

    [Fact]
    public async Task DR2N05_Cok_buyuk_tutar_999_999_999_99_kurus_kaybi_yok()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-2-TEST dg2 n05");
        sistem.Add(H(k, "SATIS", 999_999_999.99m, -1_000m));
        sistem.Add(H(k, "SATIS_IADE", -0.01m, 0.0001m));
        await sistem.SaveChangesAsync();
        var (n, kz) = await Olc(k);
        Assert.Equal(999_999_999.98m, D(n, "toplamTl"));
        Assert.Equal(999.9999m, D(n, "altinToplamHas"));
        Assert.Equal(D(kz, "hasilatTl"), D(n, "toplamTl"));
    }

    [Fact]
    public async Task DR2N06_Iade_stornosu_dislanir_net_brute_doner()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETRAPOR-2-TEST dg2 n06");
        sistem.Add(H(k, "SATIS", 5_000m, -1m));
        var iade = H(k, "SATIS_IADE", -5_000m, 1m);
        var ters = H(k, "SATIS_IADE", 5_000m, -1m);
        ters.StornoKaynakId = iade.Id;
        sistem.Add(iade); sistem.Add(ters);
        await sistem.SaveChangesAsync();
        var (n, kz) = await Olc(k);
        Assert.Equal(0, n.GetProperty("iadeAdet").GetInt32());
        Assert.Equal(5_000m, D(n, "toplamTl"));
        Assert.Equal(1m, D(n, "altinToplamHas"));
        Assert.Equal(D(kz, "hasilatTl"), D(n, "toplamTl"));
    }

    [Fact]
    public void DR2N07_Bos_null_eski_yanitta_netSatis_null_ekran_brute_duser()
    {
        var o = System.Text.Json.JsonSerializer.Deserialize<PanelSz.GunlukOzet>(
            """{"tarih":"2026-10-03","satis":{"adet":1,"toplamTl":10,"toplamHas":0.5},"alis":null,"hurda":null,"netHasHareketi":0,"kasalar":[]}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(o);
        Assert.Null(o!.NetSatis);
        Assert.Null(o.Satis!.MadenDagilimi);
    }
}
