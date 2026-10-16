using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-4 — KARMA ÖDEMEDE ALTIN BACAĞI (D3) · KARAR-HAS-DEFTERI-2026-09-23 § 5
//
// Müşterinin ödeme olarak verdiği eski altın HER İKİ DEFTERE BİRDEN işlenir:
// satışla AYNI transaction'da, AYNI kur kaydıyla yazılan AYRI bir HURDA_ALIS
// hareketi (has +, TL −) + gider pusulası. Satış BRÜT kalır; altın kasaya
// girmez, cariye has yazmaz, nakde sayılmaz; tevsik ve MASAK fişin BRÜTÜNE
// bakar. Test adları şartnamedeki T-1..T-25 ile birebir aynıdır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf (veritabanısız) kurallar — sözleşme, hesap, yapılandırma.</summary>
public sealed class B4AltinBacagiSafTestleri
{
    private static readonly MHurda.GiderPusulasiKapisi Hurda = MHurda.GiderPusulasiKapisi.Varsayilan();

    internal static MHurda.HurdaSaticiDto Satici(string tip = SaticiTipi.NihaiTuketici) => new()
    {
        SaticiTipi = tip, AdSoyad = "Ali Veli", Tckn = "10000000146", Adres = "Ulus, Ankara",
    };

    internal static MSatis.OdemeBacagiDto Altin(
        decimal gram, string ayar, decimal fiyat, decimal tutar,
        MHurda.HurdaSaticiDto? satici = null, bool saticiYok = false, string? kasa = null) => new()
    {
        Arac = "ALTIN", AyarId = ayar, GramAgirlik = gram, BirimFiyatTlHasGram = fiyat,
        TutarTl = tutar, Satici = saticiYok ? null : satici ?? Satici(), KasaId = kasa,
    };

    private static MSatis.OdemeBacagiDto B(string arac, decimal tutar) =>
        new() { Arac = arac, TutarTl = tutar, KasaId = arac == "VERESIYE" ? null : "ks1" };

    // T-9 (saf yarısı) — NakitToplam yalnız NAKIT + DOVIZ taşır.
    [Fact]
    public void AltinBacagi_Nakit_Toplamina_Katilmaz_saf()
    {
        // K22 5 g → 4,58 has × 2.000 = 9.160,00 (nihai tüketici: stopaj 0)
        var s = MSatis.OdemeSepeti.Coz(
            [B("NAKIT", 10_840m), Altin(5m, "K22", 2_000m, 9_160m)], 20_000m, "c1",
            MSatis.DovizKurlari.Yok, hurda: Hurda);

        Assert.Equal(10_840m, s.NakitToplam);
        Assert.Equal(0m, s.VeresiyeToplam);
        Assert.NotNull(s.Altin);
        Assert.Equal(9_160m, s.Altin!.Deger.NetTl);
    }

    [Fact]
    public void AltinBacagi_Kiymetsiz_Ayarda_Has_Sifir()
    {
        // (1) Çelikte has YOK — sahte has üretilmez.
        var celik = Ayarlar.Bul("CELIK")!;
        Assert.Equal(0m, AltinBacagiHesap.Hesapla(40m, celik, 1_000m, 0m).HasGrami);

        // (2) "Yarın çeliğe sıfırdan farklı milyem yazan biri" — HasHesaplanir=false
        //     kapısı milyemden BAĞIMSIZ 0 döndürmeli (K20-5).
        var sahte = new Ayar("X500", "Deneme kıymetsiz", 500m, Maden.Celik, Kiymetli: false, HasHesaplanir: false);
        Assert.Equal(0m, AltinBacagiHesap.Hesapla(10m, sahte, 1_000m, 0m).HasGrami);

        // (3) Tek kaynak: has, HasHesap.ToHasGram ile KURUŞU KURUŞUNA aynı (6 hane).
        var k22 = Ayarlar.Bul("K22")!;
        var d = AltinBacagiHesap.Hesapla(1.2345671m, k22, 1_000m, 0m);
        Assert.Equal(HasHesap.ToHasGram(1.2345671m, k22), d.HasGrami);
        Assert.Equal(1.130863m, d.HasGrami);

        // (4) Kıymetsiz ayarla ödeme bacağı KURULAMAZ.
        var ex = Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz(
            [B("NAKIT", 100m), Altin(40m, "CELIK", 1_000m, 1m)], 101m, "c1",
            MSatis.DovizKurlari.Yok, hurda: Hurda));
        Assert.Contains("kıymetli maden değildir", ex.Message);
    }

    [Fact]
    public void AltinBacagi_Birden_Fazla_Bacak_Reddedilir()
    {
        var ex = Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz(
            [Altin(5m, "K22", 2_000m, 9_160m), Altin(5m, "K22", 2_000m, 9_160m), B("NAKIT", 1_680m)],
            20_000m, "c1", MSatis.DovizKurlari.Yok, hurda: Hurda));
        Assert.Contains("yalnız BİR eski altın", ex.Message);
    }

    [Fact]
    public void AltinBacagi_KasaId_Verilemez()
    {
        var ex = Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz(
            [B("NAKIT", 10_840m), Altin(5m, "K22", 2_000m, 9_160m, kasa: "ks1")], 20_000m, "c1",
            MSatis.DovizKurlari.Yok, hurda: Hurda));
        Assert.Contains("kasa verilemez", ex.Message);
    }

    [Fact]
    public void Altin_Disi_Bacakta_Ayar_Gram_Alanlari_Reddedilir()
    {
        foreach (var bozuk in new[]
        {
            new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 100m, KasaId = "ks1", GramAgirlik = 5m },
            new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 100m, KasaId = "ks1", AyarId = "K22" },
            new MSatis.OdemeBacagiDto { Arac = "KART", TutarTl = 100m, KasaId = "ks1", BirimFiyatTlHasGram = 1m },
            new MSatis.OdemeBacagiDto { Arac = "VERESIYE", TutarTl = 100m, Satici = Satici() },
        })
        {
            var ex = Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz(
                [bozuk], 100m, "c1", MSatis.DovizKurlari.Yok, hurda: Hurda));
            Assert.Contains("yalnız ALTIN bacağına aittir", ex.Message);
        }
    }

    [Fact]
    public void AltinBacagi_Istemci_TL_si_Sunucununkine_Esit_Degilse_400()
    {
        // Doğru net 9.160,00; istemci 9.160,01 der, toplamı NAKIT 1 kuruş eksikle tutturur.
        var ex = Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz(
            [B("NAKIT", 10_839.99m), Altin(5m, "K22", 2_000m, 9_160.01m)], 20_000m, "c1",
            MSatis.DovizKurlari.Yok, hurda: Hurda));
        Assert.Contains("sunucu hesabıyla uyuşmuyor", ex.Message);
        Assert.Contains("9.160,00", ex.Message.Replace("9,160.00", "9.160,00"));
    }

    [Fact]
    public void OdemeKalemi_Altin_Araci_Yazilabilir_saf()
    {
        Assert.True(OdemeAraci.Gecerli("ALTIN"));
        Assert.Contains(OdemeAraci.Altin, OdemeAraci.Tumu);
        // Kasaya GİRMEZ (B4-19): kasa satırı doğmaz.
        Assert.False(OdemeAraci.KasayaGirer(OdemeAraci.Altin));
        // B4-1/B4-2 aynı anda: DTO regex'i ile kapalı liste AYNI.
        var desen = typeof(MSatis.OdemeBacagiDto).GetProperty(nameof(MSatis.OdemeBacagiDto.Arac))!
            .GetCustomAttributes(typeof(RegularExpressionAttribute), false)
            .Cast<RegularExpressionAttribute>().Single().Pattern;
        Assert.Equal(OdemeAraci.Tumu.OrderBy(x => x), desen.Split('|').OrderBy(x => x));
        // B4-7: bacak sınırı değişmez.
        Assert.Equal(5, MSatis.OdemeSepeti.EnFazlaBacak);
    }

    [Fact]
    public void Sabitler_OdemeTurleri_ALTIN_Icerir()
    {
        // G-2: geçmiş satırlar okunabilir kalır — liste "ALTIN"ı KORUR.
        Assert.Contains("ALTIN", Sabitler.OdemeTurleri);
    }

    [Fact]
    public void Tek_Etiketli_ALTIN_OdemeTuru_Artik_Reddedilir_dto()
    {
        // G-1: DTO kapısı (controller yolu) ALTIN'ı reddeder ve metin altın BACAĞINA yönlendirir.
        var dto = new MSatis.SatisOlusturDto
        {
            UrunId = "u1", Adet = 1m, BirimFiyatTl = 100m, OdemeTuru = "ALTIN",
            KasaId = "ks1", KurSnapshotId = "kur1",
        };
        var sonuclar = new List<ValidationResult>();
        var gecerli = Validator.TryValidateObject(dto, new ValidationContext(dto), sonuclar, validateAllProperties: true);
        Assert.False(gecerli);
        var mesaj = Assert.Single(sonuclar, r => r.MemberNames.Contains(nameof(MSatis.SatisOlusturDto.OdemeTuru))).ErrorMessage;
        Assert.Contains("'Eski altın' ödeme bacağı", mesaj);

        // Diğer dört tür hâlâ geçerli (mevcut hâl bozulmaz).
        foreach (var tur in new[] { "NAKIT", "KART", "HAVALE", "VERESIYE" })
        {
            dto.OdemeTuru = tur;
            sonuclar.Clear();
            Assert.True(Validator.TryValidateObject(dto, new ValidationContext(dto), sonuclar, true), tur);
        }
    }

    [Fact]
    public void AltinBacagi_Yapilandirma_Varsayilani_Tutucu()
    {
        // K-KUR-4: varsayılan null = ön-dolgu YOK.
        Assert.Null(new MHurda.HurdaAyarlari().AlisIskontoOran);
        Assert.Null(AltinBacagiHesap.OnDolgu(6_827.20m, null));
        // Oran set edilirse ön-dolgu has × (1 − oran), düzenlenebilir kalır (ekran).
        Assert.Equal(6_690.66m, AltinBacagiHesap.OnDolgu(6_827.20m, 0.02m));
        // Sunucu fiyat TÜRETMEZ: has fiyatı boş gelen altın bacağı 400.
        var bacak = Altin(5m, "K22", 0m, 9_160m);
        bacak.BirimFiyatTlHasGram = null;
        var ex = Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz(
            [B("NAKIT", 10_840m), bacak], 20_000m, "c1", MSatis.DovizKurlari.Yok, hurda: Hurda));
        Assert.Contains("TÜRETMEZ", ex.Message);
        // Stopaj oranları yapılandırmadan (B4-25/B4-26 varsayılanları korunur).
        Assert.Equal(0.02m, new MHurda.HurdaAyarlari().EsnafMuafStopajOran);
        Assert.Equal(0m, new MHurda.HurdaAyarlari().NihaiTuketiciStopajOran);
    }

    [Fact]
    public void AltinBacagi_Satici_Kimligi_Yoksa_400_saf()
    {
        // Tutar ne olursa olsun: 0,1 g da olsa kimliksiz alım yok (VUK md.234).
        var ex = Assert.Throws<BadRequestException>(() => MSatis.OdemeSepeti.Coz(
            [B("NAKIT", 99.82m), Altin(0.1m, "K22", 2m, 0.18m, saticiYok: true)], 100m, "c1",
            MSatis.DovizKurlari.Yok, hurda: Hurda));
        Assert.Contains("VUK md.234", ex.Message);
    }
}

/// <summary>Defter etkisi — gerçek MySQL (kuyumcu_test_*), satış fişi ucu.</summary>
[Collection("veritabani")]
public sealed class B4AltinBacagiTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) => new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private Kiraci Kiraci(string ad) => IslemBantFikstur.ReferansKur(vt.OlusturSistem(), TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB4-TEST-" + ad), 3_500m);

    private static MSatis.OdemeBacagiDto B(Kiraci k, string arac, decimal tutar) =>
        new() { Arac = arac, TutarTl = tutar, KasaId = arac == "VERESIYE" ? null : k.KasaId };

    private static MSatis.OdemeBacagiDto Altin(
        decimal gram, string ayar, decimal fiyat, decimal tutar, string tip = SaticiTipi.NihaiTuketici) =>
        B4AltinBacagiSafTestleri.Altin(gram, ayar, fiyat, tutar, B4AltinBacagiSafTestleri.Satici(tip));

    private static MSatis.SatisFisiDto Fis(Kiraci k, decimal birimFiyat, List<MSatis.OdemeBacagiDto> odemeler) => new()
    {
        CariId = k.CariId,
        Kalemler = [new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = birimFiyat }],
        Odemeler = odemeler,
        KurSnapshotId = k.KurId,
    };

    // Standart fiş: 20.000 TL; eski altın K22 5 g → 4,58 has × 2.000 = 9.160,00 brüt.
    //   nihai tüketici: stopaj 0 → net 9.160,00 · esnaf muaf: stopaj 183,20 → net 8.976,80
    private const decimal FisTl = 20_000m;
    private const decimal EskiHas = 4.58m;

    private List<Hareket> Hareketler(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId).OrderBy(h => h.Tur).ToList();
    }

    private async Task<object> StandartFis(Kiraci k, string tip = SaticiTipi.NihaiTuketici)
    {
        using var db = vt.Olustur(k);
        var net = tip == SaticiTipi.EsnafMuaf ? 8_976.80m : 9_160m;
        return await Satis(db).SatisFisi(
            Fis(k, FisTl, [B(k, "NAKIT", FisTl - net), Altin(5m, "K22", 2_000m, net, tip)]), k.Islem);
    }

    [Fact]
    public async Task AltinBacagi_IkiDeftere_Birden_Yazar()
    {
        var k = Kiraci("iki defter");
        await StandartFis(k);

        var h = Hareketler(k);
        Assert.Equal(2, h.Count);
        var satis = h.Single(x => x.Tur == "SATIS");
        var hurda = h.Single(x => x.Tur == "HURDA_ALIS");
        Assert.Equal(FisTl, satis.TlTutari);
        Assert.Equal(-k.UrunHasGrami, satis.HasGrami);
        Assert.Equal(-9_160m, hurda.TlTutari);          // mali bacak (net)
        Assert.Equal(EskiHas, hurda.HasGrami);          // fiziki bacak (has)
        Assert.Null(hurda.Adet);                        // gramla alınır, adet uydurulmaz
        Assert.Equal(k.CariId, hurda.CariId);
        Assert.Null(hurda.FisId);                       // satış fiş kümesine KARIŞMAZ
    }

    [Fact]
    public async Task AltinBacagi_Satis_BRUT_Yazilir()
    {
        var k = Kiraci("brut");
        await StandartFis(k, SaticiTipi.EsnafMuaf);

        var satis = Hareketler(k).Single(x => x.Tur == "SATIS");
        Assert.Equal(FisTl, satis.TlTutari);                 // KDVK 23/e: mahsup DÜŞÜLMEZ
        Assert.NotEqual(FisTl - 8_976.80m, satis.TlTutari);
    }

    [Fact]
    public async Task AltinBacagi_Cariye_Has_Yazmaz()
    {
        var k = Kiraci("cari has");
        using (var db = vt.Olustur(k))
            await Satis(db).SatisFisi(Fis(k, FisTl,
                [B(k, "NAKIT", 5_840m), Altin(5m, "K22", 2_000m, 9_160m), B(k, "VERESIYE", 5_000m)]), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var (tl, has) = TestKurulum.CariBakiye(kontrol, k.CariId);
        Assert.Equal(0m, has);          // K3: satın alınan hurda müşterinin altın alacağı DEĞİLDİR
        Assert.Equal(5_000m, tl);       // cariye yalnız veresiye bacağı yazılır
    }

    [Fact]
    public async Task AltinBacagi_Kasaya_Yazmaz()
    {
        var k = Kiraci("kasa");
        await StandartFis(k);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis + 10_840m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        var kasaSatirlari = kontrol.KasaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToList();
        var tek = Assert.Single(kasaSatirlari);
        Assert.Equal("NAKIT", tek.OdemeTuru);
        Assert.Equal(10_840m, tek.Tutar);
    }

    [Fact]
    public async Task AltinBacagi_GiderPusulasi_Ayni_Committe_Dogar()
    {
        var k = Kiraci("pusula");
        var yanit = await StandartFis(k, SaticiTipi.EsnafMuaf);

        using var kontrol = vt.OlusturSistem();
        var hurda = kontrol.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS");
        var p = kontrol.GiderPusulalari.AsNoTracking().Single(g => g.TenantId == k.TenantId);
        Assert.Equal(hurda.Id, p.HareketId);
        Assert.False(string.IsNullOrWhiteSpace(p.PusulaNo));
        Assert.Equal(9_160m, p.BrutTutar);          // pusula BRÜT
        Assert.Equal(183.20m, p.StopajTutar);
        Assert.Equal(8_976.80m, p.NetOdenen);
        Assert.Equal(-8_976.80m, hurda.TlTutari);   // hareket NET
        Assert.Equal(EskiHas, p.HasGrami);
        Assert.Equal("K22", p.AyarId);
        // UX-S27: yanıt iki belgeyi ve pusula numarasını söyler.
        Assert.Equal(p.PusulaNo, TestKurulum.Metin(yanit, "altinBacagi", "giderPusulasi", "PusulaNo"));
        Assert.Equal(2m, Sayi(yanit, "altinBacagi", "belgeSayisi"));
    }

    [Fact]
    public async Task AltinBacagi_Satici_Kimligi_Yoksa_400()
    {
        var k = Kiraci("satici yok");
        using var db = vt.Olustur(k);
        var bacak = B4AltinBacagiSafTestleri.Altin(0.1m, "K22", 2m, 0.18m, saticiYok: true);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(
            Fis(k, 100m, [B(k, "NAKIT", 99.82m), bacak]), k.Islem));
        Assert.Contains("VUK md.234", ex.Message);
        Assert.Empty(Hareketler(k));
    }

    [Fact]
    public async Task AltinBacagi_Fis_Denkligi_Toleranssiz()
    {
        var k = Kiraci("denklik");
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(
            Fis(k, FisTl, [B(k, "NAKIT", 10_840.01m), Altin(5m, "K22", 2_000m, 9_160m)]), k.Islem));
        Assert.Contains("eşit olmalıdır", ex.Message);
        Assert.Empty(Hareketler(k));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    [Fact]
    public async Task AltinBacagi_Nakit_Toplamina_Katilmaz()
    {
        var k = Kiraci("nakit payi");
        var yanit = await StandartFis(k);

        var h = Hareketler(k);
        Assert.Equal(10_840m, h.Sum(x => x.NakitTutar ?? 0m));
        Assert.Equal(0m, h.Single(x => x.Tur == "HURDA_ALIS").NakitTutar);
        Assert.Equal(0m, h.Single(x => x.Tur == "HURDA_ALIS").VeresiyeTutar);
        Assert.Equal(10_840m, Sayi(yanit, "nakitToplam"));
    }

    [Fact]
    public async Task AltinBacagi_Tevsik_Fis_Brutu_Uzerinden_Tetiklenir()
    {
        // 40.000 TL fiş: 5.000 nakit + 35.000 eski altın (K24 10 g × 3.500).
        // Nakit payı haddin altında ama FİŞ BRÜTÜ hadde bakar → nakit bacak kabul edilmez.
        var k = Kiraci("tevsik");
        using (var db = vt.Olustur(k))
        {
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(
                Fis(k, 40_000m, [B(k, "NAKIT", 5_000m), Altin(10m, "K24", 3_500m, 35_000m)]), k.Islem));
            Assert.Contains("İşlemin toplamı", ex.Message);
        }
        Assert.Empty(Hareketler(k));

        // Nakitsiz aynı fiş geçer — kapı yalnız nakit bacak varken işler.
        using (var db = vt.Olustur(k))
            await Satis(db).SatisFisi(
                Fis(k, 40_000m, [B(k, "KART", 5_000m), Altin(10m, "K24", 3_500m, 35_000m)]), k.Islem);
        Assert.Equal(0m, Hareketler(k).Sum(x => x.NakitTutar ?? 0m));
    }

    [Fact]
    public async Task AltinBacagi_Masak_Esigi_Bruten_Olculur()
    {
        // 200.000 TL fiş: 20.000 kart + 180.000 eski altın (K24 20 g × 9.000). MASAK-370: bugün
        // kimliği fiilen tetikleyen en düşük tutar 200.000 (amaç beyanı, TEYİTSİZ; md.5/1-b 370.000).
        // Altın dışı pay eşiğin çok altında; fişin BRÜTÜ eşik üstü → kimlik tespiti istenir.
        var k = Kiraci("masak");
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(
            Fis(k, 200_000m, [B(k, "KART", 20_000m), Altin(48m, "K24", 3_750m, 180_000m)]), k.Islem));   // ISLEM-BANT: 20 g × 9.000 bant dışıydı; aynı brüt 48 g × 3.750
        Assert.StartsWith("MASAK", ex.Message);
        Assert.Empty(Hareketler(k));
    }

    [Fact]
    public async Task AltinBacagi_Maden_Ayardan_Gelir_Gumus_Altina_Karismaz()
    {
        // 925 gümüş 100 g → 92,5 has × 30 = 2.775,00
        var k = Kiraci("gumus");
        using (var db = vt.Olustur(k))
            await Satis(db).SatisFisi(
                Fis(k, FisTl, [B(k, "NAKIT", 17_225m), Altin(100m, "G925", 30m, 2_775m)]), k.Islem);

        var hurda = Hareketler(k).Single(x => x.Tur == "HURDA_ALIS");
        Assert.Equal(Maden.Gumus, hurda.Maden);
        using var oku = vt.Olustur(k);
        Assert.Equal(0m, await MHurda.HurdaStokKapisi.Bakiye(oku, k.Islem, Maden.Altin));
        Assert.Equal(92.5m, await MHurda.HurdaStokKapisi.Bakiye(oku, k.Islem, Maden.Gumus));
    }

    [Fact]
    public async Task AltinBacagi_Tek_KurSnapshot()
    {
        var k = Kiraci("tek kur");
        // Fişten SONRA değil ÖNCE daha yeni bir kur kaydı: yazım "en son kuru" değil FİŞİN kurunu almalı.
        using (var s = vt.OlusturSistem())
        {
            s.KurSnapshots.Add(new KurSnapshot
            {
                // ISLEM-BANT: daha yeni kayıt işlem bandının referansı olur; değer fikstür fiyatını (2.000) bant içinde tutar.
                Id = Kimlik.Ulid(), TenantId = k.TenantId, AltinHasFiyati = 3_600m, GramAltinFiyati = 7_050m,
                Kaynak = "MANUAL", Olusturuldu = DateTime.UtcNow.AddMinutes(5),
            });
            s.SaveChanges();
        }
        await StandartFis(k);

        var h = Hareketler(k);
        Assert.Equal(2, h.Count);
        Assert.All(h, x => Assert.Equal(k.KurId, x.KurSnapshotId));
    }

    [Fact]
    public async Task AltinBacagi_Iptal_Edilmis_Kur_Reddedilir()
    {
        var k = Kiraci("iptal kur");
        using (var s = vt.OlusturSistem())
        {
            s.KurSnapshots.Single(x => x.Id == k.KurId).IptalEdildi = true;
            s.SaveChanges();
        }
        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(
            Fis(k, FisTl, [B(k, "NAKIT", 10_840m), Altin(5m, "K22", 2_000m, 9_160m)]), k.Islem));
        Assert.Contains("iptal edilmiş", ex.Message);
        Assert.Empty(Hareketler(k));
    }

    [Fact]
    public async Task OdemeKalemi_Altin_Araci_Yazilabilir()
    {
        var k = Kiraci("kalem");
        var yanit = await StandartFis(k);

        using var kontrol = vt.OlusturSistem();
        var fisId = TestKurulum.Metin(yanit, "fisId")!;
        var kalemler = kontrol.OdemeKalemleri.AsNoTracking().Where(x => x.TenantId == k.TenantId && x.FisId == fisId).ToList();
        Assert.Equal(FisTl, kalemler.Sum(x => x.TutarTl));           // fiş toplamı = kalem toplamı
        var altin = kalemler.Single(x => x.Arac == OdemeAraci.Altin);
        Assert.Equal(9_160m, altin.TutarTl);
        Assert.Null(altin.KasaId);
        Assert.Null(altin.KasaHareketId);
        var hurdaId = kontrol.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "HURDA_ALIS").Id;
        Assert.Equal(hurdaId, altin.HareketId);
    }

    [Fact]
    public async Task Tek_Etiketli_ALTIN_OdemeTuru_Artik_Reddedilir()
    {
        // Servis yolu (controller'sız çağrı) da kapalı — DTO regex'ine güvenilmez.
        var k = Kiraci("tek etiket");
        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m, OdemeTuru = "ALTIN",
            KasaId = k.KasaId, KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem));
        Assert.Contains("'Eski altın' ödeme bacağı", ex.Message);
        Assert.Empty(Hareketler(k));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
    }

    [Fact]
    public async Task Sabitler_OdemeTurleri_ALTIN_Gecmis_Satir_Okunur()
    {
        // G-2: geçmişte yazılmış tek etiketli ALTIN satırı tür kapısından 400'e DÜŞMEZ.
        var k = Kiraci("gecmis altin");
        using var db = vt.Olustur(k);
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = DateTime.UtcNow,
            TlTutari = 100m, HasGrami = 0m, OdemeTuru = "ALTIN", KurSnapshotId = k.KurId,
            KullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        Assert.Contains(Hareketler(k), h => h.OdemeTuru == "ALTIN");
        Assert.Contains("ALTIN", Sabitler.OdemeTurleri);
    }

    [Fact]
    public async Task AltinBacagi_Tum_Yazmalar_Tek_Transactionda()
    {
        // Pusula yazımı veritabanında PATLAR (mal açıklaması kolonu 500 karakter):
        // hiçbir hareket / kasa / cari / pusula satırı kalmamalı.
        var k = Kiraci("tek tx");
        using (var db = vt.Olustur(k))
        {
            var satici = B4AltinBacagiSafTestleri.Satici();
            satici.MalAciklamasi = new string('x', 600);
            var bacak = B4AltinBacagiSafTestleri.Altin(5m, "K22", 2_000m, 9_160m, satici);
            await Assert.ThrowsAnyAsync<Exception>(() => Satis(db).SatisFisi(
                Fis(k, FisTl, [B(k, "NAKIT", 5_840m), bacak, B(k, "VERESIYE", 5_000m)]), k.Islem));
        }

        Assert.Empty(Hareketler(k));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Empty(kontrol.KasaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId));
        Assert.Empty(kontrol.GiderPusulalari.AsNoTracking().Where(x => x.TenantId == k.TenantId));
        Assert.Empty(kontrol.OdemeKalemleri.AsNoTracking().Where(x => x.TenantId == k.TenantId));
        Assert.Equal((0m, 0m), TestKurulum.CariBakiye(kontrol, k.CariId));
    }

    [Fact]
    public async Task AltinBacagi_Hurda_Has_Bakiyesine_Duser()
    {
        var k = Kiraci("hurda bakiye");
        using (var once = vt.Olustur(k))
            Assert.Equal(0m, await MHurda.HurdaStokKapisi.Bakiye(once, k.Islem, Maden.Altin));

        await StandartFis(k);

        using var sonra = vt.Olustur(k);
        Assert.Equal(EskiHas, await MHurda.HurdaStokKapisi.Bakiye(sonra, k.Islem, Maden.Altin));
    }

    [Fact]
    public async Task AltinBacagi_Storno_Simetrik()
    {
        // ŞARTNAMEDEN UYARLAMA (rapor § Sapmalar): 48-A storno motoru SATIS ve
        // HURDA_ALIS türlerini BİLEREK stornolamıyor (stok/maliyet partisi geri
        // alması — kısmi geri alma yapılmaz). Simetri bu yüzden REDDİN
        // simetrisidir: iki hareketin HİÇBİRİ tek başına geri alınamaz; yarım
        // geri alınmış bir fiş (satış duruyor, alış silinmiş ya da tersi) DOĞAMAZ.
        var k = Kiraci("storno");
        await StandartFis(k);
        var h = Hareketler(k);
        decimal kasaOnce, hurdaOnce;
        using (var kontrol = vt.OlusturSistem()) kasaOnce = TestKurulum.KasaBakiye(kontrol, k.KasaId);
        using (var oku = vt.Olustur(k)) hurdaOnce = await MHurda.HurdaStokKapisi.Bakiye(oku, k.Islem, Maden.Altin);

        foreach (var hareket in h)
        {
            using var db = vt.Olustur(k);
            var storno = new MSatis.StornoServisi(db, TestKurulum.Denetim(db), NullLogger<MSatis.StornoServisi>.Instance);
            await Assert.ThrowsAnyAsync<ApiException>(() => storno.Storno(hareket.Id, new MSatis.StornoDto
            {
                Gerekce = "Test: altın bacaklı fişin tek yarısını geri alma denemesi",
                KurSnapshotId = k.KurId, KasaId = k.KasaId,
            }, k.Islem));
        }

        Assert.Equal(2, Hareketler(k).Count);
        using (var kontrol = vt.OlusturSistem()) Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        using (var oku = vt.Olustur(k)) Assert.Equal(hurdaOnce, await MHurda.HurdaStokKapisi.Bakiye(oku, k.Islem, Maden.Altin));
    }

    [Fact]
    public void AltinBacagi_Yapilandirma_Varsayilani_Tutucu_uc()
    {
        // Ekranın okuduğu uç: oran yapılandırılmamışken null döner (ön-dolgu yok).
        using var db = vt.OlusturSistem();
        var ayarlar = Satis(db).AltinBacagiAyarlari();
        Assert.Null(Sayi(ayarlar, "alisIskontoOran"));
        Assert.True(System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(ayarlar))
            .RootElement.TryGetProperty("alisIskontoOran", out _), "alan yanıtta AÇIKÇA null olmalı");
    }

    /// <summary>Servis cevabındaki sayı alanı; alan yoksa ya da null ise <c>null</c>.</summary>
    internal static decimal? Sayi(object cevap, params string[] yol)
    {
        var oge = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(cevap)).RootElement;
        foreach (var a in yol)
            if (!oge.TryGetProperty(a, out oge)) return null;
        return oge.ValueKind == System.Text.Json.JsonValueKind.Number ? oge.GetDecimal() : null;
    }
}
