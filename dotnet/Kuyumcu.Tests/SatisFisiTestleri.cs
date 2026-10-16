using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ÖDEME SEPETİ — saf doğrulama, veritabanı yok
// ═════════════════════════════════════════════════════════════════════════════

public sealed class OdemeSepetiTestleri
{
    private static MSatis.OdemeBacagiDto B(string arac, decimal tutar, string? kasa = "ks1") =>
        new() { Arac = arac, TutarTl = tutar, KasaId = arac == "VERESIYE" ? null : kasa };

    [Fact]
    public void Bacak_toplami_fis_tutarina_TAM_esit_olmalidir()
    {
        // Bir kuruş bile tutmuyorsa para ya kaybolur ya iki kez sayılır.
        var ex = Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([B("NAKIT", 9_999.99m)], 10_000m, "c1", MSatis.DovizKurlari.Yok));
        Assert.Contains("eşit olmalıdır", ex.Message);
    }

    [Fact]
    public void Tam_esitlikte_gecer_ve_paylar_ayrisir()
    {
        var s = MSatis.OdemeSepeti.Coz(
            [B("NAKIT", 15_000m), B("KART", 10_000m), B("VERESIYE", 5_000m)], 30_000m, "c1", MSatis.DovizKurlari.Yok);

        Assert.Equal(15_000m, s.NakitToplam);
        Assert.Equal(5_000m, s.VeresiyeToplam);   // kart ne nakittir ne alacak
    }

    [Fact]
    public void Bacaksiz_fis_REDDEDILIR()
        => Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz([], 100m, "c1", MSatis.DovizKurlari.Yok));

    [Fact]
    public void Sifir_ve_negatif_bacak_REDDEDILIR()
    {
        Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([B("NAKIT", 100m), B("KART", 0m)], 100m, "c1", MSatis.DovizKurlari.Yok));
        Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([B("NAKIT", 200m), B("KART", -100m)], 100m, "c1", MSatis.DovizKurlari.Yok));
    }

    [Fact]
    public void Iki_veresiye_bacagi_REDDEDILIR()
    {
        var ex = Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([B("VERESIYE", 50m), B("VERESIYE", 50m)], 100m, "c1", MSatis.DovizKurlari.Yok));
        Assert.Contains("yalnız BİR veresiye", ex.Message);
    }

    [Fact]
    public void Carisiz_veresiye_REDDEDILIR()
    {
        // Borç kime yazılacağı bilinmeden kaydedilen alacak tahsil edilemez.
        var ex = Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([B("VERESIYE", 100m)], 100m, cariId: null, MSatis.DovizKurlari.Yok));
        Assert.Contains("müşteri (cari) seçilmelidir", ex.Message);
    }

    [Fact]
    public void Kasasiz_nakit_bacagi_REDDEDILIR()
    {
        var ex = Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([B("NAKIT", 100m, kasa: null)], 100m, "c1", MSatis.DovizKurlari.Yok));
        Assert.Contains("kasa seçilmelidir", ex.Message);
    }

    [Fact]
    public void Veresiye_bacagina_kasa_verilemez()
    {
        var b = new MSatis.OdemeBacagiDto { Arac = "VERESIYE", TutarTl = 100m, KasaId = "ks1" };
        Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz([b], 100m, "c1", MSatis.DovizKurlari.Yok));
    }

    [Fact]
    public void Bes_bacaktan_fazlasi_REDDEDILIR()
    {
        var bacaklar = Enumerable.Range(0, 6).Select(_ => B("NAKIT", 10m)).ToList();
        Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz(bacaklar, 60m, "c1", MSatis.DovizKurlari.Yok));
    }

    // ── Pay dağıtımı ────────────────────────────────────────────────────────

    [Fact]
    public void Paylar_SIRALI_doldurulur_ve_toplam_TAM_kapanir()
    {
        // 3 kalem (10.000 + 10.000 + 10.000), 15.000 nakit + 5.000 veresiye.
        // Sıralı: 1. kalem 10.000 nakit; 2. kalem 5.000 nakit + 5.000 veresiye;
        // 3. kalem 0/0 (kalanı kart karşılıyor).
        var paylar = MSatis.OdemeSepeti.PaylariDagit([10_000m, 10_000m, 10_000m], 15_000m, 5_000m);

        Assert.Equal((10_000m, 0m), paylar[0]);
        Assert.Equal((5_000m, 5_000m), paylar[1]);
        Assert.Equal((0m, 0m), paylar[2]);
        Assert.Equal(15_000m, paylar.Sum(x => x.Nakit));
        Assert.Equal(5_000m, paylar.Sum(x => x.Veresiye));
    }

    [Fact]
    public void Kurusu_bolunen_tutarda_bile_pay_toplami_SAPMAZ()
    {
        // Orantılı dağıtım burada kuruş kaybederdi; sıralı doldurmada yalnız
        // çıkarma var, toplam TANIM GEREĞİ kapanır.
        var kalemler = new[] { 33.33m, 33.33m, 33.34m };
        var paylar = MSatis.OdemeSepeti.PaylariDagit(kalemler, 50m, 50m);

        Assert.Equal(50m, paylar.Sum(x => x.Nakit));
        Assert.Equal(50m, paylar.Sum(x => x.Veresiye));
        for (var i = 0; i < kalemler.Length; i++)
            Assert.True(paylar[i].Nakit + paylar[i].Veresiye <= kalemler[i],
                "kalem payı kendi tutarını aşamaz");
    }

    // ── DÖVİZ BACAĞI — paket 15 (yasal biçim: dövizle TAHSİLAT) ──────────────

    private static readonly MSatis.DovizKurlari Kur = new(UsdTry: 40m, EurTry: 44m, GbpTry: null);

    private static MSatis.OdemeBacagiDto D(string birim, decimal miktar, decimal tl, string? kasa = "kUSD") =>
        new() { Arac = "DOVIZ", ParaBirimi = birim, DovizTutar = miktar, TutarTl = tl, KasaId = kasa };

    /// <summary>
    /// TL karşılığını SUNUCU hesaplar. İstemcinin TL'sine güvenilseydi kasiyer
    /// kendi kurunu uygular — yetki belgesiz kuyumcu için döviz ticareti (yasak).
    /// </summary>
    [Fact]
    public void Doviz_bacaginin_TL_karsiligi_kur_kaydiyla_KURUSUNA_esit_olmalidir()
    {
        var ex = Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([D("USD", 100m, 4_001m)], 4_001m, "c1", Kur));
        Assert.Contains("4.000,00 TL", ex.Message.Replace("4,000.00", "4.000,00"));
        Assert.Contains("uyuşmuyor", ex.Message);
    }

    [Fact]
    public void Doviz_bacagi_dogru_TL_ile_gecer_ve_NAKIT_sayilir()
    {
        var s = MSatis.OdemeSepeti.Coz([D("USD", 100m, 4_000m), B("KART", 1_000m)], 5_000m, "c1", Kur);

        Assert.Equal(4_000m, s.DovizToplamTl);
        Assert.Equal(4_000m, s.NakitToplam);   // efektif döviz = nakit (tevsik fail-closed)
        Assert.Equal(0m, s.VeresiyeToplam);
    }

    /// <summary>Kur çekilememişse döviz ALINMAZ — "yaklaşık kur" yok.</summary>
    [Fact]
    public void Kuru_olmayan_dovizle_tahsilat_REDDEDILIR()
    {
        var ex = Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([D("GBP", 10m, 650m)], 650m, "c1", Kur));
        Assert.Contains("GBP kuru", ex.Message);
    }

    [Fact]
    public void Kur_kaydi_YOKSA_hic_doviz_bacagi_kabul_edilmez()
    {
        Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([D("USD", 100m, 4_000m)], 4_000m, "c1", MSatis.DovizKurlari.Yok));
    }

    [Fact]
    public void Doviz_bacaginda_para_birimi_ve_miktar_ZORUNLU()
    {
        var birimsiz = new MSatis.OdemeBacagiDto { Arac = "DOVIZ", DovizTutar = 100m, TutarTl = 4_000m, KasaId = "kUSD" };
        Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz([birimsiz], 4_000m, "c1", Kur));

        var miktarsiz = new MSatis.OdemeBacagiDto { Arac = "DOVIZ", ParaBirimi = "USD", TutarTl = 4_000m, KasaId = "kUSD" };
        Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz([miktarsiz], 4_000m, "c1", Kur));
    }

    [Fact]
    public void Kasasiz_doviz_bacagi_REDDEDILIR()
    {
        Assert.Throws<BadRequestException>(() =>
            MSatis.OdemeSepeti.Coz([D("USD", 100m, 4_000m, kasa: null)], 4_000m, "c1", Kur));
    }

    /// <summary>TL bacağına döviz alanı: yanlış araç ya da dolar TL diye yazılmış.</summary>
    [Fact]
    public void TL_bacaginda_doviz_alanlari_REDDEDILIR()
    {
        var karisik = new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 100m, KasaId = "ks1", DovizTutar = 100m };
        var ex = Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz([karisik], 100m, "c1", Kur));
        Assert.Contains("yalnız DOVIZ", ex.Message);
    }

    [Fact]
    public void Doviz_yuvarlamasi_yarim_kurusu_YUKARI_atar()
    {
        // 12,345 USD × 40 = 493,80 — ama 1,2345 × 40 = 49,38; kuruş tam. Yarım kuruş
        // örneği: 0,125 EUR × 44 = 5,50 → 5,50 (tam). 0,1125 × 44 = 4,95. Sunucu ile
        // istemci AYNI kuralı (AwayFromZero) kullanmalı; bu test kuralı sabitler.
        var s = MSatis.OdemeSepeti.Coz([D("EUR", 0.1125m, 4.95m)], 4.95m, "c1", Kur);
        Assert.Equal(4.95m, s.DovizToplamTl);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SATIŞ FİŞİ UCU — kapı paritesi ve defter etkisi
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class SatisFisiTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSatis.OdemeBacagiDto B(Kiraci k, string arac, decimal tutar) =>
        new() { Arac = arac, TutarTl = tutar, KasaId = arac == "VERESIYE" ? null : k.KasaId };

    private static MSatis.SatisFisiDto Fis(
        Kiraci k, decimal birimFiyat, List<MSatis.OdemeBacagiDto> odemeler, int kalemAdet = 1) => new()
    {
        CariId = k.CariId,
        Kalemler = Enumerable.Range(0, kalemAdet)
            .Select(_ => new MSatis.SatisFisKalemDto
            {
                UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = birimFiyat,
            }).ToList(),
        Odemeler = odemeler,
        KurSnapshotId = k.KurId,
    };

    private List<Hareket> FisSatirlari(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId != null)
            .OrderBy(h => h.Id).ToList();
    }

    [Fact]
    public async Task Fisin_tum_kalemleri_AYNI_fis_id_tasir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş grup kimliği");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 5_000m, [B(k, "NAKIT", 6_000m), B(k, "KART", 9_000m)], kalemAdet: 3), k.Islem);

        var satirlar = FisSatirlari(k);
        Assert.Equal(3, satirlar.Count);
        Assert.Single(satirlar.Select(h => h.FisId).Distinct());
        Assert.All(satirlar, h => Assert.Equal("KARMA", h.OdemeTuru));
    }

    [Fact]
    public async Task Nakit_ve_veresiye_paylari_defterde_TOPLAMI_verir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş pay toplamı");
        using var db = vt.Olustur(k);

        // 3 × 5.000 = 15.000 → 6.000 nakit + 4.000 kart + 5.000 veresiye
        await Satis(db).SatisFisi(
            Fis(k, 5_000m, [B(k, "NAKIT", 6_000m), B(k, "KART", 4_000m), B(k, "VERESIYE", 5_000m)],
                kalemAdet: 3), k.Islem);

        var satirlar = FisSatirlari(k);
        Assert.Equal(6_000m, satirlar.Sum(h => h.NakitTutar));
        Assert.Equal(5_000m, satirlar.Sum(h => h.VeresiyeTutar));
        Assert.Equal(15_000m, satirlar.Sum(h => h.TlTutari));
    }

    [Fact]
    public async Task Her_bacak_KENDI_kasa_hareketini_yazar_ozet_satir_YOK()
    {
        // Gün sonu raporu kasa hareketlerini ödeme aracına göre gruplar; tek
        // özet satır yazılsaydı kart ile nakit ayrımı kaybolurdu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş kasa bacakları");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 15_000m, [B(k, "NAKIT", 6_000m), B(k, "KART", 9_000m)]), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kasaSatirlari = kontrol.KasaHareketleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId).ToList();

        Assert.Equal(2, kasaSatirlari.Count);
        Assert.Equal(6_000m, kasaSatirlari.Single(x => x.OdemeTuru == "NAKIT").Tutar);
        Assert.Equal(9_000m, kasaSatirlari.Single(x => x.OdemeTuru == "KART").Tutar);
    }

    [Fact]
    public async Task Veresiye_bacagi_cari_bakiyesine_yazilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş veresiye bakiye");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 10_000m, [B(k, "KART", 6_000m), B(k, "VERESIYE", 4_000m)]), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var (tl, _) = TestKurulum.CariBakiye(kontrol, k.CariId);
        Assert.Equal(4_000m, tl);
    }

    [Fact]
    public async Task Haddi_asan_fiste_NAKIT_bacak_REDDEDILIR()
    {
        // §4.1.2 — karma ödemenin en kritik kapısı. 40.000 TL'lik fişin
        // 5.000 TL'sini nakit almak ihlaldir; Kapı 2 olmadan geçerdi.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş kapı 2");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).SatisFisi(
                Fis(k, 40_000m, [B(k, "NAKIT", 5_000m), B(k, "KART", 35_000m)]), k.Islem));

        Assert.Contains("İşlemin toplamı", hata.Message);
    }

    [Fact]
    public async Task Haddi_asan_fis_NAKITSIZ_ise_gecer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş kapı 2 nakitsiz");
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 40_000m, [B(k, "KART", 20_000m), B(k, "HAVALE", 20_000m)]), k.Islem);

        var satirlar = FisSatirlari(k);
        Assert.Equal(0m, satirlar.Sum(h => h.NakitTutar));
    }

    [Fact]
    public async Task Fisin_nakit_payi_KUMULATIF_toplama_katilir()
    {
        // Fişten sonra yapılan saf nakit satış, fişin nakit payını görmeli.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş kümülatif");
        using var db = vt.Olustur(k);

        // 20.000'lik fiş: 18.000 nakit + 2.000 kart (had aşılmıyor)
        await Satis(db).SatisFisi(
            Fis(k, 20_000m, [B(k, "NAKIT", 18_000m), B(k, "KART", 2_000m)]), k.Islem);

        // 18.000 + 13.000 = 31.000 > 30.000 → durmalı
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).Satis(new MSatis.SatisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 13_000m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("31.000,00", hata.Message);
    }

    [Fact]
    public async Task Stok_yetersizse_fis_TAMAMEN_reddedilir()
    {
        // Kapı parite testi: kalem doğrulaması transaction'dan ÖNCE olmalı,
        // yoksa ilk kalem yazılıp ikincide patlardı.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş stok", stokAdet: 1m);
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).SatisFisi(
                Fis(k, 1_000m, [B(k, "KART", 2_000m)], kalemAdet: 2), k.Islem));

        Assert.Empty(FisSatirlari(k));
    }

    [Fact]
    public async Task Doviz_kasasina_TL_bacagi_yazilamaz()
    {
        // K6 paritesi: bacak başına kasa para birimi doğrulanır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş döviz kasa", kasaDoviz: "USD");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).SatisFisi(Fis(k, 5_000m, [B(k, "NAKIT", 5_000m)]), k.Islem));
    }

    [Fact]
    public async Task Fis_stogu_dusurur_ve_maliyet_partisini_tuketir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş stok düşümü", stokAdet: 10m);
        using var db = vt.Olustur(k);

        await Satis(db).SatisFisi(
            Fis(k, 5_000m, [B(k, "KART", 10_000m)], kalemAdet: 2), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var stok = kontrol.Stoklar.AsNoTracking()
            .Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId);
        Assert.Equal(8m, stok.Adet);
    }

    // ── DÖVİZLE TAHSİLAT — paket 15 (yasal biçim) ────────────────────────────

    /// <summary>USD kasası açar ve kur kaydına USD kuru yazar (TestKurulum'da yok).</summary>
    private string DovizHazirla(Kiraci k, string birim = "USD", decimal? kur = 40m)
    {
        using var db = vt.OlusturSistem();
        var kasaId = Kimlik.Ulid();
        db.Kasalar.Add(new Kasa
        {
            Id = kasaId, TenantId = k.TenantId, SubeId = k.SubeId,
            Ad = $"{birim} Kasa", ParaBirimi = birim, Bakiye = 0m, Aktif = true,
            Guncellendi = DateTime.UtcNow,
        });
        var snap = db.KurSnapshots.Single(s => s.Id == k.KurId);
        if (birim == "USD") snap.UsdTry = kur; else if (birim == "EUR") snap.EurTry = kur;
        db.SaveChanges();
        return kasaId;
    }

    private static MSatis.OdemeBacagiDto D(string kasaId, string birim, decimal miktar, decimal tl) =>
        new() { Arac = "DOVIZ", ParaBirimi = birim, DovizTutar = miktar, TutarTl = tl, KasaId = kasaId };

    [Fact]
    public async Task Doviz_bacagi_DOVIZ_kasasina_EFEKTIF_miktari_yazar_TL_degil()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Döviz tahsilat", stokAdet: 10m);
        var usdKasa = DovizHazirla(k);
        using var db = vt.Olustur(k.TenantId);

        // 5.000 TL fiş: 100 USD (= 4.000 TL) + 1.000 TL kart
        await Satis(db).SatisFisi(
            Fis(k, 5_000m, [D(usdKasa, "USD", 100m, 4_000m), B(k, "KART", 1_000m)]), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kasa = kontrol.Kasalar.AsNoTracking().Single(x => x.Id == usdKasa);
        Assert.Equal(100m, kasa.Bakiye);                         // dolar, TL değil
        var kh = kontrol.KasaHareketleri.AsNoTracking().Single(x => x.KasaId == usdKasa);
        Assert.Equal(100m, kh.Tutar);
        Assert.Equal("USD", kh.ParaBirimi);
        Assert.Equal("DOVIZ", kh.OdemeTuru);

        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS");
        Assert.Equal(5_000m, h.TlTutari);                        // TL bacağı TL
        Assert.Equal(4_000m, h.NakitTutar);                      // döviz = nakit payı (tevsik)
        Assert.Equal(k.KurId, h.KurSnapshotId);                  // kur ispatı satırda
    }

    /// <summary>Dolar avro kasasına giremez (K6'nın döviz karşılığı).</summary>
    [Fact]
    public async Task Doviz_bacagi_FARKLI_para_biriminin_kasasina_yazilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Döviz yanlış kasa", stokAdet: 10m);
        DovizHazirla(k, "USD", 40m);
        var eurKasa = DovizHazirla(k, "EUR", 44m);
        using var db = vt.Olustur(k.TenantId);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(
            Fis(k, 4_000m, [D(eurKasa, "USD", 100m, 4_000m)]), k.Islem));
        Assert.Contains("USD kasası gerektirir", ex.Message);
    }

    [Fact]
    public async Task Doviz_bacagi_TL_kasasina_yazilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Döviz TL kasa", stokAdet: 10m);
        DovizHazirla(k);
        using var db = vt.Olustur(k.TenantId);

        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(
            Fis(k, 4_000m, [D(k.KasaId, "USD", 100m, 4_000m)]), k.Islem));
    }

    /// <summary>
    /// 459 s. VUK: efektif döviz NAKİTTİR. 800 USD × 40 = 32.000 TL > 30.000 had.
    /// Döviz nakit sayılmasaydı had, dolarla ödetilerek delinirdi.
    /// </summary>
    [Fact]
    public async Task Doviz_efektif_TEVSIK_haddinde_nakit_sayilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Döviz tevsik", stokAdet: 10m);
        var usdKasa = DovizHazirla(k);
        using var db = vt.Olustur(k.TenantId);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(
            Fis(k, 32_000m, [D(usdKasa, "USD", 800m, 32_000m)]), k.Islem));
        Assert.Contains("30.000", ex.Message.Replace("30,000", "30.000"));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, kontrol.Kasalar.AsNoTracking().Single(x => x.Id == usdKasa).Bakiye);
    }

    [Fact]
    public async Task Kur_kaydinda_USD_yoksa_dovizle_tahsilat_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Döviz kursuz", stokAdet: 10m);
        var usdKasa = DovizHazirla(k, "USD", kur: null);
        using var db = vt.Olustur(k.TenantId);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(
            Fis(k, 4_000m, [D(usdKasa, "USD", 100m, 4_000m)]), k.Islem));
        Assert.Contains("USD kuru", ex.Message);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// FATURA DAMGASI — çok kalemli fişin TAMAMI damgalanır
//
// ⚠ NEDEN ÖNEMLİ: gün sonu raporu, FaturaNo'su boş SATIS satırlarını
// "faturalandırılmamış satış" sayar ve bu rakam KDV beyanının dayanağıdır.
// Fatura yalnız verilen satıra vurulsaydı 3 kalemli bir fişin 2 kalemi
// faturasız görünür, mutabakat her gün yanlış alarm verirdi.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class FisFaturaDamgasiTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private async Task<List<Hareket>> UcKalemliFis(Kiraci k, KuyumcuDbContext db)
    {
        await Satis(db).SatisFisi(new MSatis.SatisFisiDto
        {
            CariId = k.CariId,
            Kalemler = Enumerable.Range(0, 3).Select(_ => new MSatis.SatisFisKalemDto
            {
                UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 3_000m,
            }).ToList(),
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "KART", TutarTl = 9_000m, KasaId = k.KasaId }],
            KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId != null).ToList();
    }

    [Fact]
    public async Task Fatura_fisin_TUM_kalemlerini_damgalar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş fatura damgası", stokAdet: 10m);
        using var db = vt.Olustur(k);
        var satirlar = await UcKalemliFis(k, db);
        Assert.Equal(3, satirlar.Count);

        await new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db).Olustur(
            new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
            {
                CariId = k.CariId, Tur = "SATIS", Toplam = 9_000m, KdvOran = 0.20m,
                HareketId = satirlar[0].Id,
            }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var sonra = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.FisId != null).ToList();

        Assert.All(sonra, h => Assert.False(string.IsNullOrEmpty(h.FaturaNo)));
        Assert.Single(sonra.Select(h => h.FaturaNo).Distinct());   // hepsi AYNI numara
    }

    [Fact]
    public async Task Faturalanmis_fis_IKINCI_kez_faturalanamaz()
    {
        // İkinci kalemden tekrar fatura kesilmesi aynı malı iki kez
        // faturalandırmak olurdu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş çift fatura", stokAdet: 10m);
        using var db = vt.Olustur(k);
        var satirlar = await UcKalemliFis(k, db);

        var servis = new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db);
        await servis.Olustur(new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
        {
            CariId = k.CariId, Tur = "SATIS", Toplam = 9_000m, KdvOran = 0.20m,
            HareketId = satirlar[0].Id,
        }, k.Islem);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            servis.Olustur(new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
            {
                CariId = k.CariId, Tur = "SATIS", Toplam = 9_000m, KdvOran = 0.20m,
                HareketId = satirlar[1].Id,
            }, k.Islem));
        Assert.Contains("fişin kalemleri zaten", hata.Message);
    }

    [Fact]
    public async Task Fissiz_tek_hareket_davranisi_DEGISMEZ()
    {
        // Geriye uyum: fiş kimliği olmayan klasik satışta yalnız o satır damgalanır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fişsiz fatura damgası");
        using var db = vt.Olustur(k);

        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m,
            OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        using var oku = vt.OlusturSistem();
        var hareket = oku.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS");
        Assert.Null(hareket.FisId);

        await new Kuyumcu.Api.Modules.Fatura.FaturaServisi(db).Olustur(
            new Kuyumcu.Api.Modules.Fatura.FaturaOlusturDto
            {
                CariId = k.CariId, Tur = "SATIS", Toplam = 5_000m, KdvOran = 0.20m,
                HareketId = hareket.Id,
            }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.False(string.IsNullOrEmpty(kontrol.Hareketler.AsNoTracking()
            .Single(h => h.Id == hareket.Id).FaturaNo));
    }
}
