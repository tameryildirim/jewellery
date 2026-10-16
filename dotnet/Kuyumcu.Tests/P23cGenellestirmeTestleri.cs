using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MVezne = Kuyumcu.Api.Modules.Vezne;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-23c (FAZ 3) — VEZNE FİŞİNİN GENELLEŞTİRİLMESİ + ÖDEME KALEMİNİN
// TÜM YOLLARA YAYILMASI (R-17 / R-18 "tümü" dalı)
//
// Sınanan çekirdek:
//   • ODEME_KALEMSIZ_YOL = 0 — ödeme yazan HER yol kalem de yazar
//     (tahsilat · işçilik · takas · alış · evrak · tek bacaklı satış)
//   • FIS_TOPLAM_ODEME_KALEMI_FARKI = 0 — kalem toplamı = fiş toplamı,
//     GERÇEKTEN ölçülür (kapının varlığı değil)
//   • Storno YENİ kalem yazmaz; kaynağın kalemleri pasifleşir
//   • MASAK kümülatifi ve 459 s. VUK tevsik haddi kalem tablosundan
//     ETKİLENMEZ (gerileme testi — plan DoD şartı)
//   • Vezne fişi TÜR taşır: SATIS · TAHSILAT · ISCILIK; tür sözleşmesi
//     fail-closed'dur ve TAKAS/EMANET gerekçesiyle reddedilir
// ═════════════════════════════════════════════════════════════════════════════

// ── Saf testler: veritabanı yok ──────────────────────────────────────────────
public sealed class OdemeKalemFabrikasiSafTestleri
{
    private static Hareket H(string? odemeTuru, decimal tl, string? fisId = null) => new()
    {
        Id = "H1", TenantId = "T1", SubeId = "S1", Tur = "SATIS",
        Tarih = DateTime.UtcNow, TlTutari = tl, HasGrami = 0m,
        OdemeTuru = odemeTuru, FisId = fisId, KurSnapshotId = "K1",
        KullaniciId = "U1", Olusturuldu = DateTime.UtcNow,
    };

    [Fact]
    public void Tek_arac_etiketi_TEK_kalem_uretir_ve_tutar_ISARETSIZDIR()
    {
        var kalemler = OdemeKalemFabrikasi.Turet(H("NAKIT", -1_234.56m), OdemeIzi.Nakitten(1_234.56m));
        Assert.Single(kalemler);
        Assert.Equal(OdemeAraci.Nakit, kalemler[0].Arac);
        Assert.Equal(1_234.56m, kalemler[0].TutarTl);
        // Fiş kimliği yoksa fiş HAREKETİN KENDİSİDİR.
        Assert.Equal("H1", kalemler[0].FisId);
    }

    [Fact]
    public void KARMA_etiketi_paylardan_IKI_kalem_uretir_toplami_korunur()
    {
        // Kısmi ödeme: 4.000 peşin + 6.000 veresiye = 10.000 fiş toplamı.
        var kalemler = OdemeKalemFabrikasi.Turet(
            H("KARMA", -10_000m), new OdemeIzi(4_000m, 6_000m, null));
        Assert.Equal(2, kalemler.Count);
        Assert.Equal(OdemeAraci.Nakit, kalemler[0].Arac);
        Assert.Equal(OdemeAraci.Veresiye, kalemler[1].Arac);
        Assert.Equal(10_000m, kalemler.Sum(x => x.TutarTl));
    }

    [Fact]
    public void Para_el_degistirmeyen_hareket_kalem_URETMEZ()
    {
        Assert.Empty(OdemeKalemFabrikasi.Turet(H(null, 0m), OdemeIzi.Yok));
        // "DIGER" (açılış devri) bir ödeme aracı DEĞİLDİR.
        Assert.Empty(OdemeKalemFabrikasi.Turet(H("DIGER", 5_000m), OdemeIzi.Yok));
    }

    [Fact]
    public void Fis_kimligi_varsa_kalem_ONA_baglanir()
    {
        var kalemler = OdemeKalemFabrikasi.Turet(H("KART", 900m, fisId: "F9"), OdemeIzi.Yok);
        Assert.Equal("F9", Assert.Single(kalemler).FisId);
    }

    [Fact]
    public void VEZNE_FIS_TURU_kapali_listedir()
    {
        Assert.True(VezneFisTuru.Gecerli("SATIS"));
        Assert.True(VezneFisTuru.Gecerli("TAHSILAT"));
        Assert.True(VezneFisTuru.Gecerli("ISCILIK"));
        // Kapsam kararı: takas ve emanet vezne fişi TAŞIMAZ (gerekçesi
        // Data/VezneFisi.cs başlığında yazılıdır).
        Assert.False(VezneFisTuru.Gecerli("TAKAS"));
        Assert.False(VezneFisTuru.Gecerli("EMANET"));
        Assert.False(VezneFisTuru.Gecerli(null));
        Assert.True(VezneFisTuru.Kalemli("SATIS"));
        Assert.False(VezneFisTuru.Kalemli("TAHSILAT"));
        Assert.True(VezneFisTuru.CariZorunlu("ISCILIK"));
        Assert.False(VezneFisTuru.CariZorunlu("SATIS"));
    }
}

// ── Veritabanı testleri ──────────────────────────────────────────────────────
[Collection("veritabani")]
public sealed class P23cGenellestirmeTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MSatis.StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()),
            NullLogger<MSatis.StornoServisi>.Instance);

    private static MVezne.VezneServisi Vezne(KuyumcuDbContext db) =>
        new(db, NullLogger<MVezne.VezneServisi>.Instance,
            new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance),
            Cari(db));

    private List<OdemeKalemi> Kalemler(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return [.. kontrol.OdemeKalemleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId).OrderBy(x => x.Olusturuldu).ThenBy(x => x.Sira)];
    }

    private Hareket Tek(Kiraci k, string tur)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == tur && h.StornoKaynakId == null);
    }

    /// <summary>
    /// ODEME_KALEMSIZ_YOL sayacının TEST İÇİ karşılığı — denetim.sh'ın canlı
    /// sorgusuyla AYNI ölçüttür. Betikteki ölçüt değişirse bu test de kırılır
    /// ki iki taraf ayrışmasın.
    /// </summary>
    private int KalemsizYol(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        var hepsi = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId).ToList();
        var kalemHareketleri = kontrol.OdemeKalemleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Aktif).Select(x => x.HareketId).ToList();
        var stornolananlar = hepsi.Where(h => h.StornoKaynakId is not null)
            .Select(h => h.StornoKaynakId!).ToHashSet();
        return hepsi.Count(h =>
            ((h.NakitTutar ?? 0m) + (h.VeresiyeTutar ?? 0m) > 0m
             || OdemeAraci.Gecerli(h.OdemeTuru))
            && h.StornoKaynakId is null
            && !stornolananlar.Contains(h.Id)
            && !kalemHareketleri.Contains(h.Id));
    }

    /// <summary>FIS_TOPLAM_ODEME_KALEMI_FARKI sayacının test içi karşılığı.</summary>
    private int FarkliFis(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        var hepsi = kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId).ToList();
        var stornolananlar = hepsi.Where(h => h.StornoKaynakId is not null)
            .Select(h => h.StornoKaynakId!).ToHashSet();
        var canli = hepsi.Where(h =>
            ((h.NakitTutar ?? 0m) + (h.VeresiyeTutar ?? 0m) > 0m
             || OdemeAraci.Gecerli(h.OdemeTuru))
            && h.StornoKaynakId is null && !stornolananlar.Contains(h.Id));
        var fisToplam = canli
            .GroupBy(h => h.FisId ?? h.Id)
            .ToDictionary(g => g.Key, g => Math.Round(g.Sum(h => Math.Abs(h.TlTutari)), 2));
        var kalemToplam = kontrol.OdemeKalemleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Aktif).ToList()
            .GroupBy(x => x.FisId)
            .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.TutarTl), 2));
        return fisToplam.Count(p =>
            Math.Abs(p.Value - (kalemToplam.TryGetValue(p.Key, out var kt) ? kt : 0m)) > 0.005m);
    }

    // ── 1) TAHSİLAT YOLU ────────────────────────────────────────────────────

    [Fact]
    public async Task Tahsilat_yolu_odeme_kalemi_YAZAR_ve_fis_toplamina_esittir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c tahsilat");
        using var db = vt.Olustur(k);

        await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
        {
            TlTutari = 4_500m, OdemeTuru = "HAVALE", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Aciklama = "Borç tahsilatı",
        }, k.Islem);

        var kalem = Assert.Single(Kalemler(k));
        Assert.Equal(OdemeAraci.Havale, kalem.Arac);
        Assert.Equal(4_500m, kalem.TutarTl);
        Assert.Equal("TRY", kalem.ParaBirimi);
        Assert.False(kalem.GeriDolduruldu);
        Assert.False(kalem.Bilinmiyor);
        Assert.Equal(Tek(k, "TAHSILAT").Id, kalem.HareketId);
        Assert.Equal(0, KalemsizYol(k));
        Assert.Equal(0, FarkliFis(k));
    }

    // ── 2) İŞÇİLİK YOLU ─────────────────────────────────────────────────────

    [Fact]
    public async Task Iscilik_yolu_odeme_kalemi_YAZAR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c işçilik");
        using var db = vt.Olustur(k);

        await Satis(db).Iscilik(new MSatis.IscilikDto
        {
            CariId = k.CariId, IscilikTl = 750m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId, Aciklama = "Tamir",
        }, k.Islem);

        var kalem = Assert.Single(Kalemler(k));
        Assert.Equal(OdemeAraci.Nakit, kalem.Arac);
        Assert.Equal(750m, kalem.TutarTl);
        Assert.Equal(0, KalemsizYol(k));
    }

    [Fact]
    public async Task Iscilik_veresiye_kalirsa_VERESIYE_kalemi_yazilir()
    {
        // Kasa YOKSA bedel cariye borçlanır; ödeme aracı VERESIYE'dir ve
        // kalemi de yazılır — "kasaya girmeyen pay kayıtsız kalmaz".
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c işçilik veresiye");
        using var db = vt.Olustur(k);

        await Satis(db).Iscilik(new MSatis.IscilikDto
        {
            CariId = k.CariId, IscilikTl = 1_100m, OdemeTuru = "NAKIT",
            KasaId = null, KurSnapshotId = k.KurId, VadeGun = 30,
        }, k.Islem);

        var kalem = Assert.Single(Kalemler(k));
        Assert.Equal(OdemeAraci.Veresiye, kalem.Arac);
        Assert.Equal(1_100m, kalem.TutarTl);
        Assert.Null(kalem.KasaId);       // veresiyenin kasa satırı YOKTUR
        Assert.Equal(0, KalemsizYol(k));
    }

    // ── 3) TAKAS YOLU ───────────────────────────────────────────────────────

    [Fact]
    public async Task Takas_farki_odeme_kalemi_YAZAR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c takas");
        using var db = vt.Olustur(k);

        await Satis(db).Takas(new MSatis.TakasDto
        {
            Satici = ParaDemoTestVerisi.TakasSaticisi,   // PARA-DEMO md.3: takasta satıcı kimliği zorunlu
            CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = 10m,
            EskiUrunBirimFiyatTlHasGram = 5_000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 60_000m,
            KasaId = k.KasaId, OdemeTuru = "KART", KurSnapshotId = k.KurId,
        }, k.Islem);

        var h = Tek(k, "TAKAS");
        var kalem = Assert.Single(Kalemler(k));
        Assert.Equal(OdemeAraci.Kart, kalem.Arac);
        Assert.Equal(Math.Abs(h.TlTutari), kalem.TutarTl);   // fark İŞARETSİZ yazılır
        Assert.Equal(0, KalemsizYol(k));
        Assert.Equal(0, FarkliFis(k));
    }

    // ── 4) ALIŞ — KISMİ ÖDEME İKİ KALEM ─────────────────────────────────────

    [Fact]
    public async Task Alista_kismi_odeme_IKI_kalem_yazar_toplami_fis_toplamidir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c alış kısmi");
        using var db = vt.Olustur(k);

        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 20_000m,
            OdenenTl = 8_000m, OdemeTuru = "NAKIT", KasaId = k.KasaId,
            KurSnapshotId = k.KurId, VadeGun = 30,
        }, k.Islem);

        var kalemler = Kalemler(k);
        Assert.Equal(2, kalemler.Count);
        Assert.Equal(OdemeAraci.Nakit, kalemler[0].Arac);
        Assert.Equal(8_000m, kalemler[0].TutarTl);
        Assert.Equal(OdemeAraci.Veresiye, kalemler[1].Arac);
        Assert.Equal(12_000m, kalemler[1].TutarTl);
        Assert.Equal(0, KalemsizYol(k));
        Assert.Equal(0, FarkliFis(k));
    }

    // ── 5) STORNO — YENİ KALEM YAZMAZ, KAYNAĞI PASİFLEŞTİRİR ────────────────

    [Fact]
    public async Task Storno_YENI_kalem_yazmaz_kaynagi_pasiflestirir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c storno kalem");
        string kaynakId;
        using (var db = vt.Olustur(k))
        {
            await Satis(db).Iscilik(new MSatis.IscilikDto
            {
                CariId = k.CariId, IscilikTl = 2_000m, OdemeTuru = "NAKIT",
                KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);
            kaynakId = Tek(k, "ISCILIK").Id;
        }

        using (var db2 = vt.Olustur(k))
            await Storno(db2).Storno(kaynakId, new MSatis.StornoDto
            {
                Gerekce = "Kasiyer yanlış cariye işledi — mutabakatta düzeltildi",
                KurSnapshotId = k.KurId, KasaId = k.KasaId,
            }, k.Islem);

        var kalemler = Kalemler(k);
        // TEK kalem vardır (kaynağınki) ve PASİFTİR; ters kayıt kalem doğurmadı.
        var kalem = Assert.Single(kalemler);
        Assert.Equal(kaynakId, kalem.HareketId);
        Assert.False(kalem.Aktif);
        Assert.False(string.IsNullOrEmpty(kalem.StornoHareketId));
        Assert.Equal(0, KalemsizYol(k));    // stornolanan satır kapsam DIŞI
        Assert.Equal(0, FarkliFis(k));
    }

    // ── 6) MASAK + 459 s. VUK GERİLEME (plan DoD şartı) ─────────────────────

    [Fact]
    public async Task MASAK_ve_459_VUK_kumulatifi_kalem_tablosundan_ETKILENMEZ()
    {
        // PLAN DoD: "MASAK kümülatifi ve 459 s. VUK tevsik haddi yeni tablo
        // üzerinden AYNI sonucu verir (gerileme testi)."
        //
        // ⚠ BU TESTİN ÖLÇTÜĞÜ ŞEY: kapıların KAYNAĞI hâlâ eski alanlardır
        //   (hareketler.nakit_tutar). Kalem tablosu yanlarında durur; had ne
        //   erken kapanır ne geç. Ölçüm, aynı kiracıda kalem YAZILIYORKEN
        //   haddin TAM eşikte tetiklenmesidir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c MASAK tevsik");
        using var db = vt.Olustur(k);

        // 1. adım: 25.000 TL nakit işçilik — had (30.000) AŞILMADI, geçer.
        await Satis(db).Iscilik(new MSatis.IscilikDto
        {
            CariId = k.CariId, IscilikTl = 25_000m, OdemeTuru = "NAKIT",
            KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        var ilkKalem = Assert.Single(Kalemler(k));
        Assert.Equal(25_000m, ilkKalem.TutarTl);
        // Eski alan AYNEN yazılmış: tevsik kümülatifinin kaynağı değişmedi.
        var h1 = Tek(k, "ISCILIK");
        Assert.Equal(25_000m, h1.NakitTutar);

        // 2. adım: aynı gün 6.000 TL daha nakit → kümülatif 31.000 > 30.000.
        // Kapı, kalem tablosuna RAĞMEN eski kaynaktan okuyup DURDURMALIDIR.
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).Iscilik(new MSatis.IscilikDto
            {
                CariId = k.CariId, IscilikTl = 6_000m, OdemeTuru = "NAKIT",
                KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem));
        Assert.Contains("nakit sınırı", hata.Message);

        // Reddedilen işlem kalem DE yazmamıştır (transaction bütünlüğü).
        Assert.Single(Kalemler(k));
        Assert.Equal(0, KalemsizYol(k));
    }

    // ── 7) VEZNE FİŞİNİN GENELLEŞTİRİLMESİ ──────────────────────────────────

    private static MVezne.VezneFisOlusturDto TahsilatFisi(Kiraci k, decimal tutar) => new()
    {
        FisTuru = "TAHSILAT", CariId = k.CariId, KurSnapshotId = k.KurId,
        Aciklama = "vezne tahsilat fişi", TutarTl = tutar,
    };

    private static string Kimlik(object cevap) =>
        System.Text.Json.JsonSerializer.SerializeToElement(cevap).GetProperty("id").GetString()!;

    private static string Metin(object cevap, string alan) =>
        System.Text.Json.JsonSerializer.SerializeToElement(cevap).GetProperty(alan).GetString() ?? "";

    private static decimal Sayi(object cevap, string alan) =>
        System.Text.Json.JsonSerializer.SerializeToElement(cevap).GetProperty(alan).GetDecimal();

    [Fact]
    public async Task Vezne_TAHSILAT_fisi_acilir_veznelenir_tahsil_edilir_ve_kalem_yazilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c vezne tahsilat");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(TahsilatFisi(k, 3_200m), k.Islem));
        var veznelendi = await Vezne(db).Veznele(fisId, k.Islem);
        Assert.Equal(VezneDurum.Veznede, Metin(veznelendi, "durum"));
        Assert.Equal(3_200m, Sayi(veznelendi, "toplamTl"));

        var sonuc = await Vezne(db).Tahsil(fisId, new MVezne.VezneTahsilDto
        {
            KurSnapshotId = k.KurId,
            Bacaklar = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 3_200m, KasaId = k.KasaId }],
        }, k.Islem);

        var hareketFisId = Metin(sonuc, "hareketFisId");
        Assert.False(string.IsNullOrEmpty(hareketFisId));

        // Defterde TAHSILAT satırı doğdu ve vezne fişi ona BAĞLANDI.
        var h = Tek(k, "TAHSILAT");
        Assert.Equal(h.Id, hareketFisId);

        var kalem = Assert.Single(Kalemler(k));
        Assert.Equal(OdemeAraci.Nakit, kalem.Arac);
        Assert.Equal(3_200m, kalem.TutarTl);
        Assert.Equal(h.Id, kalem.FisId);
        Assert.Equal(0, KalemsizYol(k));
        Assert.Equal(0, FarkliFis(k));

        // R10 KORUNDU: vezne modülü defteri kendisi yazmadı — satırın sahibi
        // tahsil edendir ve tek satır vardır.
        using var kontrol = vt.OlusturSistem();
        Assert.Single(kontrol.Hareketler.AsNoTracking().Where(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Vezne_ISCILIK_fisi_tahsil_edilince_defterde_ISCILIK_satiri_dogar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c vezne işçilik");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(new MVezne.VezneFisOlusturDto
        {
            FisTuru = "ISCILIK", CariId = k.CariId, KurSnapshotId = k.KurId, TutarTl = 900m,
        }, k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);
        await Vezne(db).Tahsil(fisId, new MVezne.VezneTahsilDto
        {
            KurSnapshotId = k.KurId,
            Bacaklar = [new MSatis.OdemeBacagiDto { Arac = "KART", TutarTl = 900m, KasaId = k.KasaId }],
        }, k.Islem);

        var h = Tek(k, "ISCILIK");
        Assert.Equal(900m, h.TlTutari);
        var kalem = Assert.Single(Kalemler(k));
        Assert.Equal(OdemeAraci.Kart, kalem.Arac);
        Assert.Equal(0, KalemsizYol(k));
    }

    [Fact]
    public async Task Kalemsiz_fise_KALEM_gonderilirse_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c vezne kalem reddi");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Olustur(new MVezne.VezneFisOlusturDto
            {
                FisTuru = "TAHSILAT", CariId = k.CariId, KurSnapshotId = k.KurId, TutarTl = 100m,
                Kalemler = [new MVezne.VezneKalemDto
                {
                    UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 100m, IscilikTl = 0m,
                }],
            }, k.Islem));
        Assert.Contains("ürün kalemi taşımaz", hata.Message);
    }

    [Fact]
    public async Task Satis_fisine_TUTAR_gonderilirse_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c vezne tutar reddi");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Olustur(new MVezne.VezneFisOlusturDto
            {
                FisTuru = "SATIS", CariId = k.CariId, KurSnapshotId = k.KurId, TutarTl = 100m,
                Kalemler = [new MVezne.VezneKalemDto
                {
                    UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 100m, IscilikTl = 0m,
                }],
            }, k.Islem));
        Assert.Contains("kalemlerden hesaplanır", hata.Message);
    }

    [Fact]
    public async Task TAKAS_turu_vezne_fisi_olarak_REDDEDILIR_ve_gerekce_yazilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c vezne takas reddi");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Olustur(new MVezne.VezneFisOlusturDto
            {
                FisTuru = "TAKAS", CariId = k.CariId, KurSnapshotId = k.KurId, TutarTl = 100m,
            }, k.Islem));
        Assert.Contains("Takas ve emanet vezne fişi taşımaz", hata.Message);
    }

    [Fact]
    public async Task Kalemsiz_fisin_TUTARI_duzenlenebilir_turu_DEGISMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c vezne düzenleme");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(TahsilatFisi(k, 1_000m), k.Islem));

        var guncel = await Vezne(db).Guncelle(fisId,
            System.Text.Json.JsonDocument.Parse("""{"tutarTl":2_500.50}""".Replace("_", ""))
                .RootElement, k.Islem);
        Assert.Equal(2_500.50m, Sayi(guncel, "tutarTl"));

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Guncelle(fisId,
                System.Text.Json.JsonDocument.Parse("""{"fisTuru":"SATIS"}""").RootElement, k.Islem));
        Assert.Contains("türü değiştirilemez", hata.Message);
    }

    [Fact]
    public async Task Kalemsiz_fis_TEK_bacakla_tahsil_edilir_ikinci_bacak_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c vezne çok bacak");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(TahsilatFisi(k, 1_000m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Tahsil(fisId, new MVezne.VezneTahsilDto
            {
                KurSnapshotId = k.KurId,
                Bacaklar =
                [
                    new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 400m, KasaId = k.KasaId },
                    new MSatis.OdemeBacagiDto { Arac = "KART", TutarTl = 600m, KasaId = k.KasaId },
                ],
            }, k.Islem));
        Assert.Contains("tek ödeme aracıyla", hata.Message);

        // R9: reddedilen tahsilde fiş VEZNEDE kalır, defter BOŞ kalır.
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(VezneDurum.Veznede,
            kontrol.VezneFisleri.AsNoTracking().Single(f => f.Id == fisId).Durum);
        Assert.Empty(kontrol.Hareketler.AsNoTracking().Where(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Tahsilat_fisi_VERESIYE_bacagiyla_kapatilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c vezne veresiye reddi");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(TahsilatFisi(k, 1_000m), k.Islem));
        await Vezne(db).Veznele(fisId, k.Islem);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Vezne(db).Tahsil(fisId, new MVezne.VezneTahsilDto
            {
                KurSnapshotId = k.KurId,
                Bacaklar = [new MSatis.OdemeBacagiDto { Arac = "VERESIYE", TutarTl = 1_000m }],
            }, k.Islem));
        Assert.Contains("veresiyeyle ödenmiş olmaz", hata.Message);
    }

    [Fact]
    public async Task Eski_SATIS_fisi_davranisi_BIREBIR_korunur()
    {
        // Gerileme: tür göndermeyen istemci paket 19 davranışını AYNEN alır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P23c vezne gerileme");
        using var db = vt.Olustur(k);

        var fisId = Kimlik(await Vezne(db).Olustur(new MVezne.VezneFisOlusturDto
        {
            CariId = k.CariId, KurSnapshotId = k.KurId,
            Kalemler = [new MVezne.VezneKalemDto
            {
                UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 5_000m, IscilikTl = 0m,
            }],
        }, k.Islem));

        using (var kontrol = vt.OlusturSistem())
            Assert.Equal(VezneFisTuru.Satis,
                kontrol.VezneFisleri.AsNoTracking().Single(f => f.Id == fisId).FisTuru);

        await Vezne(db).Veznele(fisId, k.Islem);
        var sonuc = await Vezne(db).Tahsil(fisId, new MVezne.VezneTahsilDto
        {
            KurSnapshotId = k.KurId,
            Bacaklar = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 5_000m, KasaId = k.KasaId }],
        }, k.Islem);

        Assert.False(string.IsNullOrEmpty(Metin(sonuc, "hareketFisId")));
        Assert.Equal(0, KalemsizYol(k));
        Assert.Equal(0, FarkliFis(k));
    }
}
