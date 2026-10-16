using Kuyumcu.Api.Common;
using Microsoft.AspNetCore.Http;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// MASAK'ın işlem akışına GERÇEKTEN bağlandığının kanıtı — SIRADAKI-ADIM-KARAR §2/A.
// Bertan: "Bugün bu sistemde 5 milyon TL'lik nakit satış tek bir kontrol görmeden
// kapanıyor." Bu dosya yazılmadan önce bunu çürüten tek test yoktu.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MasakEntegrasyonTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);            // motor verilmedi → VARSAYILAN eşikler

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MMasak.MasakBilgiDto TamKimlik(string? aciklama = null) => new()
    {
        BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = "10000000146", AdSoyad = "Ayşe Demir",
        DogumTarihi = new DateOnly(1985, 4, 12), Uyruk = "TUR",
        IslemAmaci = "Yatırım", Aciklama = aciklama ?? "Birikim değerlendirmek için altın alımı",
        FonKaynagi = "Maaş birikimi", EkBelgeReferansi = "BELGE-2026-001",
    };

    private static MSatis.SatisOlusturDto SatisDto(Kiraci k, decimal birimFiyat, MMasak.MasakBilgiDto? masak = null) => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = birimFiyat,
        // ⚠ HAVALE, NAKİT değil: MASAK eşiği 185.000 TL'dir ve 459 s. VUK Genel Tebliği
        // tevsik haddi 30.000 TL. Bu büyüklükte bir işlem yasal olarak nakit
        // yapılamaz; testin gerçekçi olması için banka üzerinden kurgulanır.
        OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId, Masak = masak,
    };

    // ── FAIL-CLOSED: eşik üstü + kimlik yok → işlem HİÇ başlamaz ────────────

    [Fact]
    public async Task EsikUstuSatis_KimlikYoksa_Reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MASAK-red");
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).Satis(SatisDto(k, 200_000m), k.Islem));   // 200.000 ≥ 185.000

        Assert.Contains("MASAK", ex.Message);
        Assert.Contains("kimlik tespiti zorunludur", ex.Message);

        // Faz 1 tx ÖNCESİ: hiçbir yan etki yok — stok, kasa, hareket dokunulmamış
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Empty(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.Empty(kontrol.MasakKimlikTespitleri.Where(m => m.TenantId == k.TenantId));
    }

    [Fact]
    public async Task EsikUstuSatis_KimlikVarAmaAciklamaKisa_TumEksiklerBirden()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MASAK-eksik");
        using var db = vt.Olustur(k);

        var bilgi = TamKimlik(aciklama: "kısa") ;
        bilgi.IslemAmaci = null;

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(db).Satis(SatisDto(k, 250_000m, bilgi), k.Islem));

        Assert.Contains("İşlem amacı", ex.Message);
        Assert.Contains("en az 20 karakter", ex.Message);   // ikisi de tek mesajda
    }

    // ── Eşik üstü + tam kimlik → geçer, tespit + beyan hareketle ATOMİK ─────

    [Fact]
    public async Task EsikUstuSatis_TamKimlikle_TespitVeBeyanHareketeBagli()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MASAK-tam");
        using var db = vt.Olustur(k);

        await Satis(db).Satis(SatisDto(k, 250_000m, TamKimlik()), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var hareket = kontrol.Hareketler.Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS");
        var tespit = kontrol.MasakKimlikTespitleri.Single(m => m.TenantId == k.TenantId);
        var beyan = kontrol.MasakBeyanlari.Single(b => b.TenantId == k.TenantId);

        Assert.Equal(hareket.Id, tespit.HareketId);           // aynı commit'te bağlandı
        Assert.Equal(k.CariId, tespit.CariId);
        Assert.Equal(250_000m, tespit.IslemTutariTl);
        Assert.Equal("10000000146", tespit.BelgeNo);           // EF üzerinden çözülmüş
        Assert.Equal(k.KullaniciId, tespit.TespitEdenKullaniciId);
        Assert.Equal(tespit.Tarih.AddYears(8), tespit.SaklamaBitis);

        Assert.Equal(tespit.Id, beyan.KimlikTespitiId);
        Assert.Equal(hareket.Id, beyan.HareketId);
        Assert.Equal(BeyanKademesi.AmacBeyani, beyan.Kademe);   // 250.000 → amaç kademesi
        Assert.Equal("Yatırım", beyan.IslemAmaci);

        // Diskte TCKN düz metin DEĞİL (şifreleme kapıdan da geçiyor)
        var ham = await kontrol.Database.SqlQuery<string>(
            $"SELECT belge_no AS Value FROM masak_kimlik_tespitleri WHERE id = {tespit.Id}").SingleAsync();
        Assert.DoesNotContain("10000000146", ham);
    }

    [Fact]
    public async Task YalnizKimlikKademesinde_BeyanYazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MASAK-yalnizkimlik");
        using var db = vt.Olustur(k);

        // MASAK-370: YALNIZ_KIMLIK kademesi yalnız kimlik eşiği amaç beyanı eşiğinin ALTINDAYKEN
        // oluşur — 06.10.2026'ya dek yürürlükteki 185.000 dönemi. 07.10.2026'dan itibaren (370.000 >
        // 200.000) bu kademe yoktur; davranış dönem eşiği açıkça verilerek sınanır.
        var donem185 = new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri { KimlikEsigi = 185_000m });
        var bilgi = TamKimlik(); bilgi.IslemAmaci = null; bilgi.Aciklama = null;   // 190.000: yalnız kimlik yeter
        await new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance, donem185)
            .Satis(SatisDto(k, 190_000m, bilgi), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Single(kontrol.MasakKimlikTespitleri.Where(m => m.TenantId == k.TenantId));
        Assert.Empty(kontrol.MasakBeyanlari.Where(b => b.TenantId == k.TenantId));
    }

    // ── Eşik altı: hiçbir şey değişmez (mevcut 21 test bu yüzden yeşil kaldı) ──

    [Fact]
    public async Task EsikAltiSatis_KimliksizGecer_TespitYazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MASAK-alti");
        using var db = vt.Olustur(k);

        await Satis(db).Satis(SatisDto(k, 100_000m), k.Islem);   // 100.000 < 200.000 (07.10.2026'dan itibaren kimlik isteyen en düşük tutar)

        using var kontrol = vt.OlusturSistem();
        Assert.Single(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.Empty(kontrol.MasakKimlikTespitleri.Where(m => m.TenantId == k.TenantId));
    }

    // ── TAKAS TUZAĞI (Bertan): fark küçük, el değiştiren değer büyük ─────────

    [Fact]
    public async Task Takas_FarkKucukAmaDegerBuyuk_EsigeTakilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "MASAK-takas"), 45_000m);
        using var db = vt.Olustur(k);

        // Eski: 20 g K22 × 45.000 TL/has → 18,32 has × 45.000 = 824.400 TL (ISLEM-BANT: 90.000 mutlak sınır dışı)
        // Yeni: 1 adet × 830.000 TL → fark 5.600 TL (eşik ALTI), Max 830.000 (eşik ÜSTÜ)
        var dto = new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = 20m, EskiUrunBirimFiyatTlHasGram = 45_000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 830_000m,
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        };

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Takas(dto, k.Islem));
        Assert.Contains("MASAK", ex.Message);

        dto.Masak = TamKimlik();
        await Satis(db).Takas(dto, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var tespit = kontrol.MasakKimlikTespitleri.Single(m => m.TenantId == k.TenantId);
        Assert.Equal(830_000m, tespit.IslemTutariTl);   // fark (5.600) DEĞİL, el değiştiren değer
        Assert.Equal(MasakKaynakTuru.Hareket, tespit.KaynakTur);   // paket 41 regresyonu
    }

    // ── Cari akışları da kapıdan geçer ───────────────────────────────────────

    [Fact]
    public async Task Veresiye_EsikUstu_KimlikYoksa_Reddedilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MASAK-veresiye");
        using var db = vt.Olustur(k);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Cari(db).VeresiyeGir(k.CariId, new MCari.VeresiyeGirisDto { TlTutari = 500_000m, KurSnapshotId = k.KurId }, k.Islem));
        Assert.Contains("MASAK", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);   // yan etki yok
    }

    [Fact]
    public async Task Tahsilat_EsikUstu_TamKimlikle_TespitHareketeBagli()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MASAK-tahsilat");
        using var db = vt.Olustur(k);

        await Cari(db).TahsilatGir(k.CariId,
            new MCari.TahsilatGirisDto { TlTutari = 300_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId, Masak = TamKimlik() }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var hareket = kontrol.Hareketler.Single(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT");
        var tespit = kontrol.MasakKimlikTespitleri.Single(m => m.TenantId == k.TenantId);
        Assert.Equal(hareket.Id, tespit.HareketId);
        Assert.Equal(300_000m, tespit.IslemTutariTl);
        Assert.Equal(MasakKaynakTuru.Hareket, tespit.KaynakTur);   // paket 41 regresyonu
    }

    // ── PAKET 41 REGRESYONU: defter akışları kaynak_tur='HAREKET' yazar ─────

    /// <summary>
    /// ⚠ PAKET 41 / K41-2: MasakKapisi.Kaydet'e ZORUNLU <c>kaynakTur</c>
    /// parametresi eklendi ve defterden geçen 12 çağrı noktasının (SatisModulu
    /// ×8 + CariModulu ×4) hepsine açıkça "HAREKET" yazıldı. Bu test o kararı
    /// KİLİTLER: varsayılan BANKA_HAREKET yapılsa ya da bir çağrı noktası
    /// yanlış kaynak yazsa bu test KIRMIZI olur. (Tasarım belgesindeki "8"
    /// çağrı noktası eksik sayımdı; ölçülen gerçek sayı 12'dir.)
    /// </summary>
    [Fact]
    public async Task Defter_akislari_kaynak_tur_HAREKET_yazar_banka_degil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MASAK-kaynak-tur");
        using var db = vt.Olustur(k);

        await Satis(db).Satis(SatisDto(k, 250_000m, TamKimlik()), k.Islem);
        await Cari(db).VeresiyeGir(k.CariId,
            new MCari.VeresiyeGirisDto { TlTutari = 500_000m, KurSnapshotId = k.KurId, Masak = TamKimlik() }, k.Islem);
        await Cari(db).TahsilatGir(k.CariId,
            new MCari.TahsilatGirisDto
            {
                TlTutari = 300_000m, OdemeTuru = "HAVALE", KasaId = k.KasaId,
                KurSnapshotId = k.KurId, Masak = TamKimlik(),
            }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var tespitler = kontrol.MasakKimlikTespitleri.Where(m => m.TenantId == k.TenantId).ToList();
        var beyanlar = kontrol.MasakBeyanlari.Where(b => b.TenantId == k.TenantId).ToList();

        Assert.Equal(3, tespitler.Count);
        Assert.All(tespitler, t => Assert.Equal(MasakKaynakTuru.Hareket, t.KaynakTur));
        Assert.NotEmpty(beyanlar);
        Assert.All(beyanlar, b => Assert.Equal(MasakKaynakTuru.Hareket, b.KaynakTur));
    }

    // ── Eşikler yapılandırmadan: DI motoru verilirse onun eşiği geçer ────────

    [Fact]
    public async Task DiMotoruVerilirse_OnunEsigiKullanilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "MASAK-esik");
        using var db = vt.Olustur(k);

        var yumusak = new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri { KimlikEsigi = 1_000_000m, AmacBeyaniEsigi = 1_100_000m });
        var servis = new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance, yumusak);

        await servis.Satis(SatisDto(k, 250_000m), k.Islem);   // varsayılanda reddedilirdi, bu eşikte geçer

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.MasakKimlikTespitleri.Where(m => m.TenantId == k.TenantId));
    }
}
