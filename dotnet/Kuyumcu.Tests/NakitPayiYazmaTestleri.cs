using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MEmanet = Kuyumcu.Api.Modules.Emanet;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// NAKİT PAYI YAZMA — her yazma yolu ödeme izini doğru doldurur mu?
//
// `hareketler.nakit_tutar` 459 s. VUK tevsik kümülatifinin KAYNAĞIDIR. Bir yol
// onu yanlış doldurursa (kart payını nakit sayar / nakit payını 0 yazar) had ya
// haksız yere kapanır ya sessizce delinir. Bu dosya 11 yazma yolunun
// erişilebilenlerini TEK TEK sınar; erişilemeyenler (TRANSFER: ikinci şube,
// SAYIM_DUZELTME: ürün servisi) HareketFabrikasi'nin değişmezleriyle korunur.
//
// Ayrıca 2026-09-14'te kapatılan iki fail-open açığı sabitlenir:
//   • TAKAS ve İŞÇİLİK tevsik kapısını HİÇ çağırmıyordu (ama kümülatifi besliyordu)
//   • TAKAS'ta fark NEGATİF (tediye) iken nakit payı mutlak değerle sayılır
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class NakitPayiYazmaTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MEmanet.EmanetServisi Emanet(KuyumcuDbContext db) =>
        new(db, NullLogger<MEmanet.EmanetServisi>.Instance);

    /// <summary>Kiracının TEK hareketi (türüne göre) — filtresiz bağlamdan okunur.</summary>
    private Hareket Tek(Kiraci k, string tur)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == tur);
    }

    /// <summary>Günün öncesine NAKİT satış düşer — kümülatif kapı testleri için zemin.</summary>
    private static void OncekiNakit(KuyumcuDbContext db, Kiraci k, decimal tutar)
    {
        var an = DateTime.UtcNow;
        HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(tutar), new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = "SATIS", Tarih = an, TlTutari = tutar, HasGrami = 0m,
            ParaBirimi = "TRY", OdemeTuru = "NAKIT",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = an,
        });
        db.SaveChanges();
    }

    private static MSatis.SatisOlusturDto SatisDto(Kiraci k, decimal fiyat, string odeme, bool kasa = true) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = fiyat,
        OdemeTuru = odeme, KasaId = kasa ? k.KasaId : null, KurSnapshotId = k.KurId,
    };

    // ── SATIŞ ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("NAKIT",    20_000, 0)]
    [InlineData("KART",     0,      0)]       // kart tevsik edilmiş sayılır — nakit DEĞİL
    [InlineData("HAVALE",   0,      0)]
    [InlineData("VERESIYE", 0,      20_000)]
    public async Task Satis_odeme_izini_etikete_gore_yazar(string odeme, decimal nakit, decimal veresiye)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), $"Nakit payı satış {odeme}");
        using var db = vt.Olustur(k);

        await Satis(db).Satis(SatisDto(k, 20_000m, odeme, kasa: odeme != "VERESIYE"), k.Islem);

        var h = Tek(k, "SATIS");
        Assert.Equal(nakit, h.NakitTutar);
        Assert.Equal(veresiye, h.VeresiyeTutar);
        Assert.NotNull(h.NakitTutar);   // yeni kod ASLA null yazmaz
    }

    // ── ALIŞ — kısmi ödeme (KARMA) ───────────────────────────────────────────

    [Fact]
    public async Task Kismi_odemeli_alista_nakit_payi_ODENEN_veresiye_payi_KALANDIR()
    {
        // 25.000 TL alış, 10.000 nakit ödendi → nakit 10.000, veresiye 15.000, etiket KARMA.
        // Bu, TevsikKapisi'nin eskiden GÖREMEDİĞİ satırın ta kendisidir.
        //
        // ⚠ Tutar BİLEREK haddin (30.000) ALTINDA: mevzuat araştırması
        // (2026-09-15) haddi aşan bir işlemin kısmen nakit ödenmesinin de ihlal
        // olduğunu gösterdi (§4.1.2). Yani 50.000'lik bir alışın 10.000'ini
        // nakit vermek artık REDDEDİLİR — bir alt testte sabitlenmiştir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Nakit payı kısmi alış");
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 25_000m,
            OdemeTuru = "NAKIT", OdenenTl = 10_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        var h = Tek(k, "ALIS");
        Assert.Equal("KARMA", h.OdemeTuru);
        Assert.Equal(10_000m, h.NakitTutar);
        Assert.Equal(15_000m, h.VeresiyeTutar);
    }

    [Fact]
    public async Task Haddi_asan_alista_KISMI_NAKIT_odeme_REDDEDILIR()
    {
        // 459 s. VUK GT §4.1.2: işlemin TOPLAM tutarı haddi aşıyorsa ödemenin
        // her bir parçası banka kanalıyla yapılmalıdır. 50.000 TL'lik alışın
        // 10.000 TL'sini nakit vermek ihlaldir ve ceza ALICIYA DA SATICIYA DA
        // ayrı ayrı kesilir. Kapı 2 olmadan bu işlem GEÇİYORDU: eski kural
        // yalnız 10.000 TL'lik nakit dilime bakıyor, o da haddin altında kalıyordu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kapı 2 alış");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).Alis(new MSatis.AlisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 50_000m,
                OdemeTuru = "NAKIT", OdenenTl = 10_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));

        Assert.Contains("İşlemin toplamı", hata.Message);
        Assert.Contains("kartla ya da havaleyle", hata.Message);
    }

    [Fact]
    public async Task Haddi_asan_alis_TAMAMEN_HAVALE_ise_gecer()
    {
        // Kapı 2 nakdi kapatır, işlemi değil. Aynı 50.000 TL havaleyle ödenirse
        // sorun yoktur — aksi hâlde kural meşru ticareti durdururdu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kapı 2 havale");
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 50_000m,
            OdemeTuru = "HAVALE", OdenenTl = 50_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        var h = Tek(k, "ALIS");
        Assert.Equal(0m, h.NakitTutar);
    }

    [Fact]
    public async Task Havaleli_kismi_alista_nakit_payi_SIFIRDIR()
    {
        // Aynı bölünme ama nakit yerine havale: nakit payı 0, veresiye yine 40.000.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Nakit payı havaleli kısmi alış");
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 50_000m,
            OdemeTuru = "HAVALE", OdenenTl = 10_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        var h = Tek(k, "ALIS");
        Assert.Equal(0m, h.NakitTutar);
        Assert.Equal(40_000m, h.VeresiyeTutar);
    }

    // ── HURDA ALIŞ ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Pesin_nakit_hurda_alisinda_nakit_payi_NET_odemeye_esittir()
    {
        // Kapı bugün stopaj SONRASI net ödemeye bakar (kod davranışı korunur;
        // brüt/net sorusu mevzuat araştırmasında). Nakit payı = |TL bacağı|.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Nakit payı hurda");
        using var db = vt.Olustur(k);

        await Satis(db).HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 5m, BirimFiyatTlHasGram = 5_000m,
            OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Satici = TestKurulum.HurdaSatici(),   // VUK md.234 gider pusulası kapısı satıcı kimliği ister
        }, k.Islem);

        var h = Tek(k, "HURDA_ALIS");
        Assert.True(h.TlTutari < 0m);                       // kasadan çıkış
        Assert.Equal(Math.Abs(h.TlTutari), h.NakitTutar);   // tamamı nakit ödendi
        Assert.Equal(0m, h.VeresiyeTutar);
    }

    // ── TAKAS — mutlak değer ve EKSİK KAPI ───────────────────────────────────

    private static MSatis.TakasDto TakasDto(Kiraci k, decimal eskiGram, decimal yeniFiyat, string odeme, bool kasa = true) => new()
    {
        CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = eskiGram, EskiUrunBirimFiyatTlHasGram = 5_000m,
        YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = yeniFiyat,
        KasaId = kasa ? k.KasaId : null, OdemeTuru = odeme, KurSnapshotId = k.KurId,
        Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
    };

    [Fact]
    public async Task Takasta_NEGATIF_fark_tediyedir_nakit_payi_MUTLAK_degerdir()
    {
        // Eski 10 g K22 × 5.000 = 45.800 TL; yeni 20.000 TL → fark −25.800 (BİZ öderiz).
        // TlTutari negatif kalır (defter işareti), nakit payı 25.800 (işaretsiz).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Nakit payı takas tediye");
        using var db = vt.Olustur(k);

        await Satis(db).Takas(TakasDto(k, 10m, 20_000m, "NAKIT"), k.Islem);

        var h = Tek(k, "TAKAS");
        Assert.Equal(-25_800m, h.TlTutari);
        Assert.Equal(25_800m, h.NakitTutar);
        Assert.Equal("NAKIT", h.OdemeTuru);
    }

    [Fact]
    public async Task Takas_nakit_farki_tevsik_kapisina_TAKILIR()
    {
        // ESKİ DAVRANIŞ: takas kapıyı hiç çağırmıyordu. 10.000 önceki nakit +
        // 25.800 tediye = 35.800 > 30.000 → artık DURUR (fail-closed).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Takas tevsik kapısı");
        using var db = vt.Olustur(k);
        OncekiNakit(db, k, 10_000m);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Takas(TakasDto(k, 10m, 20_000m, "NAKIT"), k.Islem));
        Assert.Contains("nakit sınırı", hata.Message);
    }

    [Fact]
    public async Task Takas_farki_KARTLA_odenirse_kapi_calismaz_ve_nakit_payi_sifirdir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Takas kart");
        using var db = vt.Olustur(k);
        OncekiNakit(db, k, 25_000m);   // kümülatif zaten yüksek — kart etkilenmemeli

        await Satis(db).Takas(TakasDto(k, 10m, 20_000m, "KART"), k.Islem);

        var h = Tek(k, "TAKAS");
        Assert.Equal(0m, h.NakitTutar);
        Assert.Equal("KART", h.OdemeTuru);                  // eskiden koşulsuz "NAKIT" yazılıyordu
        using var kontrol = vt.OlusturSistem();
        var kasaHareketi = kontrol.KasaHareketleri.Single(x => x.HareketId == h.Id);
        Assert.Equal("KART", kasaHareketi.OdemeTuru);       // kasa defteri de doğru etiketi taşır
    }

    [Fact]
    public async Task Kasasiz_takasta_fark_cariye_yazilir_veresiye_payi_dolar()
    {
        // Eski 1 g × 5.000 = 4.580; yeni 20.000 → fark +15.420 müşteri borçlanır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Takas veresiye");
        using var db = vt.Olustur(k);

        await Satis(db).Takas(TakasDto(k, 1m, 20_000m, "NAKIT", kasa: false), k.Islem);

        var h = Tek(k, "TAKAS");
        Assert.Equal(0m, h.NakitTutar);
        Assert.Equal(15_420m, h.VeresiyeTutar);
        Assert.Equal("VERESIYE", h.OdemeTuru);
    }

    // ── İŞÇİLİK — EKSİK KAPI ─────────────────────────────────────────────────

    private static MSatis.IscilikDto IscilikDto(Kiraci k, decimal tutar, string odeme, bool kasa = true) => new()
    {
        CariId = k.CariId, IscilikTl = tutar, KasaId = kasa ? k.KasaId : null,
        OdemeTuru = odeme, KurSnapshotId = k.KurId,
    };

    [Fact]
    public async Task Iscilik_nakit_tahsilati_tevsik_kapisina_TAKILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İşçilik tevsik kapısı");
        using var db = vt.Olustur(k);
        OncekiNakit(db, k, 25_000m);

        // 25.000 + 6.000 = 31.000 > 30.000 → DURUR. Eskiden geçiyordu.
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Iscilik(IscilikDto(k, 6_000m, "NAKIT"), k.Islem));
        Assert.Contains("nakit sınırı", hata.Message);
    }

    [Fact]
    public async Task Iscilik_havaleyle_tahsil_edilirse_kapi_calismaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İşçilik havale");
        using var db = vt.Olustur(k);
        OncekiNakit(db, k, 25_000m);

        await Satis(db).Iscilik(IscilikDto(k, 6_000m, "HAVALE"), k.Islem);

        var h = Tek(k, "ISCILIK");
        Assert.Equal(0m, h.NakitTutar);
        Assert.Equal("HAVALE", h.OdemeTuru);
    }

    [Fact]
    public async Task Kasasiz_iscilik_veresiyedir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İşçilik veresiye");
        using var db = vt.Olustur(k);

        await Satis(db).Iscilik(IscilikDto(k, 6_000m, "NAKIT", kasa: false), k.Islem);

        var h = Tek(k, "ISCILIK");
        Assert.Equal(0m, h.NakitTutar);
        Assert.Equal(6_000m, h.VeresiyeTutar);
    }

    // ── CARİ: VERESİYE ve TAHSİLAT ───────────────────────────────────────────

    [Fact]
    public async Task Veresiye_girisi_veresiye_payini_yazar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Nakit payı veresiye girişi");
        using var db = vt.Olustur(k);

        await Cari(db).VeresiyeGir(k.CariId,
            new MCari.VeresiyeGirisDto { TlTutari = 1_000m, KurSnapshotId = k.KurId }, k.Islem);

        var h = Tek(k, "VERESIYE");
        Assert.Equal(0m, h.NakitTutar);
        Assert.Equal(1_000m, h.VeresiyeTutar);
    }

    [Theory]
    [InlineData("NAKIT",  500)]
    [InlineData("HAVALE", 0)]
    public async Task Tahsilat_nakit_payini_odeme_aracina_gore_yazar(string odeme, decimal nakit)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), $"Nakit payı tahsilat {odeme}");
        using var db = vt.Olustur(k);

        await Cari(db).TahsilatGir(k.CariId,
            new MCari.TahsilatGirisDto { TlTutari = 500m, OdemeTuru = odeme, KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem);

        var h = Tek(k, "TAHSILAT");
        Assert.Equal(nakit, h.NakitTutar);
        Assert.Equal(0m, h.VeresiyeTutar);
    }

    // ── EMANET: para el değiştirmez ──────────────────────────────────────────

    [Fact]
    public async Task Emanet_girisinde_iki_pay_da_sifirdir_null_degil()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Nakit payı emanet");
        using var db = vt.Olustur(k);

        await Emanet(db).Giris(new MEmanet.EmanetGirisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m, KurSnapshotId = k.KurId,
        }, k.Islem);

        var h = Tek(k, "EMANET_GIRIS");
        Assert.Equal(0m, h.NakitTutar);       // 0, NULL DEĞİL — null "bilinmiyor" demektir
        Assert.Equal(0m, h.VeresiyeTutar);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// FABRİKA DEĞİŞMEZLERİ — yanlış izi deftere hiç sokmaz
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class HareketFabrikasiTestleri(TestVeritabani vt)
{
    private static Hareket Taslak(Kiraci k, decimal tl) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
        Tur = "SATIS", Tarih = DateTime.UtcNow, TlTutari = tl, HasGrami = 0m,
        ParaBirimi = "TRY", OdemeTuru = "NAKIT",
        KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
        Olusturuldu = DateTime.UtcNow,
    };

    [Fact]
    public void Negatif_iz_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fabrika negatif iz");
        using var db = vt.Olustur(k);

        var ex = Assert.Throws<InvalidOperationException>(
            () => HareketFabrikasi.Ekle(db, new OdemeIzi(-1m, 0m, null), Taslak(k, 100m)));
        Assert.Contains("işaretsiz", ex.Message);
    }

    [Fact]
    public void TL_bacagini_asan_iz_REDDEDILIR()
    {
        // 100 TL'lik harekete 60 nakit + 60 veresiye yazılamaz: bir yerde çift sayım var.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fabrika aşım");
        using var db = vt.Olustur(k);

        var ex = Assert.Throws<InvalidOperationException>(
            () => HareketFabrikasi.Ekle(db, new OdemeIzi(60m, 60m, null), Taslak(k, 100m)));
        Assert.Contains("aşıyor", ex.Message);
    }

    [Fact]
    public void Taslakta_elle_doldurulmus_pay_REDDEDILIR()
    {
        // Payı yalnız iz belirler; taslakta doldurulursa iki kaynak çelişebilir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fabrika elle pay");
        using var db = vt.Olustur(k);
        var t = Taslak(k, 100m); t.NakitTutar = 100m;

        Assert.Throws<InvalidOperationException>(
            () => HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(100m), t));
    }

    [Fact]
    public void Tediye_isaretli_TL_bacaginda_mutlak_deger_esasi_gecerlidir()
    {
        // TlTutari −100 (çıkış), nakit 100 → geçerli. İşaret pay hesabına karışmaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fabrika tediye");
        using var db = vt.Olustur(k);

        var h = HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(-100m), Taslak(k, -100m));
        Assert.Equal(100m, h.NakitTutar);
        Assert.Equal(0m, h.VeresiyeTutar);
    }

    [Fact]
    public void Etiketten_kart_ve_havaleyi_nakit_saymaz()
    {
        var vade = Zaman.YerelBugun().AddDays(30);
        Assert.Equal(new OdemeIzi(0m, 0m, null), OdemeIzi.Etiketten("KART", 5_000m, vade));
        Assert.Equal(new OdemeIzi(0m, 0m, null), OdemeIzi.Etiketten("HAVALE", 5_000m, vade));
        Assert.Equal(new OdemeIzi(5_000m, 0m, null), OdemeIzi.Etiketten("NAKIT", 5_000m, vade));
        Assert.Equal(new OdemeIzi(0m, 5_000m, vade), OdemeIzi.Etiketten("VERESIYE", -5_000m, vade));
        // Vade verilmezse veresiye payı BORÇ KAPANIŞI sayılır (iade/tahsilat).
        Assert.Equal(new OdemeIzi(0m, 5_000m, null), OdemeIzi.Etiketten("VERESIYE", -5_000m, null));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DIŞA AKTARIM — ödeme kırılımı taşınabilir olmalı (ADR-0014 / KVKK)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class NakitPayiDisaAktarimTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Hareket_disa_aktariminda_nakit_ve_veresiye_paylari_vardir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Dışa aktarım nakit payı");
        using var db = vt.Olustur(k);

        await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).Satis(
            new MSatis.SatisOlusturDto
            {
                CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 1_234.50m,
                OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);

        var kaynak = new Kuyumcu.Api.Modules.Disaaktarim.HareketKaynagi();
        var basliklar = kaynak.Basliklar.ToList();
        Assert.Contains("nakitTutar", basliklar);
        Assert.Contains("veresiyeTutar", basliklar);

        using var kontrol = vt.OlusturSistem();
        var satirlar = new List<string[]>();
        await foreach (var r in kaynak.Satirlar(kontrol, k.TenantId, maskeli: false)) satirlar.Add(r);

        var satir = Assert.Single(satirlar);
        Assert.Equal("1234.50", satir[basliklar.IndexOf("nakitTutar")]);
        Assert.Equal("0.00", satir[basliklar.IndexOf("veresiyeTutar")]);
        // Her satır başlık sayısı kadar hücre taşımalı — aksi hâlde CSV kayar.
        Assert.Equal(basliklar.Count, satir.Length);
    }
}
