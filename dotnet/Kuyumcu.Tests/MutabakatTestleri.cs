using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Rapor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MMasak = Kuyumcu.Api.Modules.Masak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// MUTABAKAT TESTLERİ — PAKET P-24 (kimlik B-7 · B-62).
//
// NE SINIYOR: "özet" diye gösterilen her sayının, altındaki listenin toplamıyla
// AYNI sorgudan türediğini. Bu ürün bu kusurdan dört kez ısırıldı (vezne ₺0,00 ↔
// detay 2.000; evrak "portföy boş" ↔ 2 evrak; kâr-zarar 11,45 g ↔ 50,38 has;
// /denetim-izi kartı 312 ↔ liste 7). Her defasında iki sayı da "doğru"ydu ve
// kullanıcı hangisine güveneceğini bilmiyordu — Anayasa md.1'in tam karşıtı.
//
// ⚠ TEST ADI SÖZLEŞMEDİR: `Mutabakat_<Modul>_...`. denetim.sh P-24 bloğu
//   (MUTABAKAT_TESTI_OLMAYAN_OZET = 0) özet ucu olan HER modülde bu önekli en
//   az bir test arar. Adı değiştiren denetimi kırar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MutabakatTestleri(TestVeritabani vt)
{
    private static DenetimServisi Denetim(KuyumcuDbContext db) => new(db, new HttpContextAccessor());

    private static Kuyumcu.Api.Modules.Denetim.DenetimOkumaServisi DenetimServis(KuyumcuDbContext db)
        => new(db, Denetim(db));

    private static object? Alan(object kaynak, string ad) =>
        kaynak.GetType().GetProperty(ad)?.GetValue(kaynak);

    private static int Sayi(object kaynak, string ad) => Convert.ToInt32(Alan(kaynak, ad));

    /// <summary>Anonim kırılım listesinin "adet" alanlarını toplar.</summary>
    private static int KirilimToplami(object? kirilim)
    {
        var toplam = 0;
        foreach (var oge in (System.Collections.IEnumerable)kirilim!)
            toplam += Convert.ToInt32(Alan(oge, "adet"));
        return toplam;
    }

    /// <summary>
    /// ⚠ BU TESTİN KENDİ VARLIK TÜRÜ. Kiracı kurulumu (KiraciKur) zaten onlarca
    ///   denetim izi doğuruyor; "toplam" ile kendi satırlarımızı ayırt etmezsek
    ///   test kurulum gürültüsünü ölçer, mutabakatı DEĞİL.
    /// </summary>
    private const string Deney = "P24Deney";

    private static void Iz(KuyumcuDbContext db, Kiraci k, string eylem, DateTime tarih,
                           bool hassas = false, string varlikTuru = Deney)
        => db.DenetimKayitlari.Add(new DenetimKaydi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KullaniciId = k.KullaniciId,
            Eylem = eylem, VarlikTuru = varlikTuru, VarlikId = Kimlik.Ulid(),
            Hassas = hassas, Tarih = tarih,
        });

    // ═══ SAF — gün sonu kapsayıcı üst sınır (B-62 off-by-one) ═══════════════

    [Fact]
    public void Mutabakat_UstSinirKapsayici_GUN_verildiginde_ERTESI_GUNE_tasir()
    {
        var gun = new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc),
                     Mutabakat.UstSinirKapsayici(gun));
    }

    [Fact]
    public void Mutabakat_UstSinirKapsayici_SAATLI_deger_DEGISMEZ()
    {
        var an = new DateTime(2026, 9, 22, 13, 45, 0, DateTimeKind.Utc);
        Assert.Equal(an, Mutabakat.UstSinirKapsayici(an));
    }

    [Fact]
    public void Mutabakat_UstSinirKapsayici_NULL_kalir()
        => Assert.Null(Mutabakat.UstSinirKapsayici(null));

    [Fact]
    public void Mutabakat_Denk_ayrisinca_MutabakatIhlali_ATAR()
    {
        var hata = Assert.Throws<MutabakatIhlali>(() => Mutabakat.Denk("vezne.toplam", 0m, 2000m));
        Assert.Equal("vezne.toplam", hata.Alan);
        Assert.Contains("Mutabakat ihlali", hata.Message);
    }

    [Fact]
    public void Mutabakat_Denk_esitse_SESSIZ_gecer()
        => Mutabakat.Denk("vezne.toplam", 2000m, 2000m);

    // ═══ DENETİM — B-7 (özet ↔ liste) ve B-62 (bitiş günü) ══════════════════

    [Fact]
    public async Task Mutabakat_Denetim_ozet_toplami_SUZGECLI_liste_toplamina_ESITTIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P24 denetim ozet");
        using var db = vt.Olustur(k);

        var t = DateTime.UtcNow.AddHours(-2);
        for (var i = 0; i < 7; i++) Iz(db, k, DenetimEylemi.Silme, t.AddMinutes(i));
        for (var i = 0; i < 5; i++) Iz(db, k, DenetimEylemi.Okuma, t.AddMinutes(i));
        await db.SaveChangesAsync();

        var servis = DenetimServis(db);

        // SÜZGEÇLİ liste: yalnız SİLME.
        var liste = await servis.Listele(k.Islem, null, null, DenetimEylemi.Silme,
                                         Deney, null, null, null, 100, 0);
        var ozet = await servis.Ozet(k.Islem, null, null, DenetimEylemi.Silme, Deney);

        // ⚠ ÖNCEDEN: özet ucu eylem süzgecini HİÇ görmüyordu; kart tüm kayıtları
        //   sayıp 12+ diyor, liste 7 diyordu. Aynı ekranda iki gerçek.
        Assert.Equal(7, Sayi(liste, "toplam"));
        Assert.Equal(Sayi(liste, "toplam"), Sayi(ozet, "toplam"));
    }

    [Fact]
    public async Task Mutabakat_Denetim_ozet_KIRILIM_toplami_toplamla_DENKTIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P24 denetim kirilim");
        using var db = vt.Olustur(k);

        var t = DateTime.UtcNow.AddHours(-3);
        Iz(db, k, DenetimEylemi.Ekleme, t);
        Iz(db, k, DenetimEylemi.Guncelleme, t.AddMinutes(1));
        Iz(db, k, DenetimEylemi.Okuma, t.AddMinutes(2), hassas: true, varlikTuru: "Cari");
        await db.SaveChangesAsync();

        var ozet = await DenetimServis(db).Ozet(k.Islem, null, null);
        var toplam = Sayi(ozet, "toplam");

        Assert.Equal(toplam, KirilimToplami(Alan(ozet, "eylemler")));
        Assert.Equal(toplam, KirilimToplami(Alan(ozet, "turler")));
        Assert.Equal(toplam, KirilimToplami(Alan(ozet, "subeler")));
    }

    [Fact]
    public async Task Mutabakat_Denetim_BITIS_GUNU_kapsama_DAHILDIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P24 denetim bitis");
        using var db = vt.Olustur(k);

        // Bitiş gününün İÇİNDE, öğleden sonra yazılmış bir kayıt.
        var bugun = DateTime.UtcNow.Date;
        Iz(db, k, DenetimEylemi.Ekleme, bugun.AddHours(14));
        await db.SaveChangesAsync();

        var servis = DenetimServis(db);
        // Ekranın gönderdiği biçim: SAATSİZ gün (Tarih bileşeni ile).
        var liste = await servis.Listele(k.Islem, bugun, bugun, null, Deney, null, null, null, 100, 0);
        var ozet = await servis.Ozet(k.Islem, bugun, bugun, null, Deney);

        // ⚠ ÖNCEDEN: sunucu `Tarih < bitis` uyguluyordu; bitiş günü 00:00 olduğu
        //   için O GÜNÜN TAMAMI kapsam dışı kalıyor, ekran "kayıt yok" diyordu.
        Assert.Equal(1, Sayi(liste, "toplam"));
        Assert.Equal(1, Sayi(ozet, "toplam"));
    }

    [Fact]
    public async Task Mutabakat_Denetim_bitis_gununden_SONRAKI_kayit_KAPSAM_DISIDIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P24 denetim bitis ust");
        using var db = vt.Olustur(k);

        var bugun = DateTime.UtcNow.Date;
        Iz(db, k, DenetimEylemi.Ekleme, bugun.AddDays(1).AddHours(1));   // ertesi gün
        await db.SaveChangesAsync();

        // Kapsayıcı üst sınır, sınırı SINIRSIZ yapmaz: ertesi gün girmez.
        var liste = await DenetimServis(db)
            .Listele(k.Islem, bugun, bugun, null, Deney, null, null, null, 100, 0);
        Assert.Equal(0, Sayi(liste, "toplam"));
    }

    [Fact]
    public async Task Mutabakat_Denetim_HASSAS_suzgeci_ozete_de_DUSER()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P24 denetim hassas");
        using var db = vt.Olustur(k);

        var t = DateTime.UtcNow.AddHours(-1);
        Iz(db, k, DenetimEylemi.Okuma, t, hassas: true);
        Iz(db, k, DenetimEylemi.Okuma, t.AddMinutes(1), hassas: false);
        Iz(db, k, DenetimEylemi.Okuma, t.AddMinutes(2), hassas: false);
        await db.SaveChangesAsync();

        var servis = DenetimServis(db);
        var liste = await servis.Listele(k.Islem, null, null, null, Deney, null, true, null, 100, 0);
        var ozet = await servis.Ozet(k.Islem, null, null, null, Deney, null, true);

        Assert.Equal(1, Sayi(liste, "toplam"));
        Assert.Equal(Sayi(liste, "toplam"), Sayi(ozet, "toplam"));
    }

    // ═══ MASAK — özet kartı ↔ gerçek kayıtlar ═══════════════════════════════

    [Fact]
    public async Task Mutabakat_Masak_ozet_ACIK_bildirim_sayisi_kayitlarla_ESITTIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P24 masak ozet");
        using var db = vt.Olustur(k);

        var simdi = DateTime.UtcNow;
        db.MasakSupheliBildirimler.AddRange(
            Supheli(k, SupheliDurum.Acik, simdi.AddDays(3)),
            Supheli(k, SupheliDurum.Acik, simdi.AddDays(-1)),        // süresi geçmiş
            Supheli(k, SupheliDurum.Kapatildi, simdi.AddDays(5)));
        await db.SaveChangesAsync();

        var ozet = await new MMasak.MasakServisi(
            db, new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri()), Denetim(db)).Ozet(k.Islem);

        var gercekAcik = await db.MasakSupheliBildirimler
            .CountAsync(s => s.TenantId == k.TenantId && s.Durum == SupheliDurum.Acik);

        Assert.Equal(gercekAcik, Sayi(ozet, "acikBildirim"));
        // Alt kırılımlar üst sayıyı AŞAMAZ (kart kendi içinde de mutabık).
        Assert.True(Sayi(ozet, "suresiGecmis") + Sayi(ozet, "yaklasan") <= Sayi(ozet, "acikBildirim"));
        Assert.Equal(1, Sayi(ozet, "suresiGecmis"));
    }

    private static MasakSupheliBildirim Supheli(Kiraci k, string durum, DateTime sonBildirim) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId,
        Gerekce = new string('ş', 300), TespitTarihi = DateTime.UtcNow.AddDays(-2),
        SonBildirimTarihi = sonBildirim, Durum = durum,
        BildirenKullaniciId = k.KullaniciId, Olusturuldu = DateTime.UtcNow,
    };

    // ═══ RİSK — seviye dağılımı ↔ profil sayısı ═════════════════════════════

    [Fact]
    public async Task Mutabakat_Risk_ozet_seviye_dagilimi_PROFIL_sayisina_ESITTIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P24 risk ozet");
        using var db = vt.Olustur(k);

        var ikinciCari = Kimlik.Ulid();
        db.Cariler.Add(new Cari
        {
            Id = ikinciCari, TenantId = k.TenantId, Ad = "P24 Risk Cari", Tur = "MUSTERI",
            Aktif = true, Guncellendi = DateTime.UtcNow,
        });
        db.CariRiskProfilleri.AddRange(
            Profil(k, k.CariId, RiskSeviyesi.Dusuk),
            Profil(k, ikinciCari, RiskSeviyesi.Yuksek));
        await db.SaveChangesAsync();

        var ozet = await new MMasak.RiskServisi(db, new MMasak.MasakRiskAyarlari(),
            new MMasak.MasakEsikleri(), Denetim(db)).Ozet(k.Islem);

        var guncelProfil = await db.CariRiskProfilleri
            .CountAsync(p => p.TenantId == k.TenantId
                && !db.CariRiskProfilleri.Any(x => x.TenantId == k.TenantId
                    && x.CariId == p.CariId && x.Olusturuldu > p.Olusturuldu));

        // Özet kartı seviye seviye döner; ÜÇÜNÜN TOPLAMI güncel profil sayısına
        // eşit olmak zorundadır — bir seviye düşerse kart sessizce eksik sayar.
        var kartToplami = Sayi(ozet, "dusuk") + Sayi(ozet, "orta") + Sayi(ozet, "yuksek");
        Assert.Equal(guncelProfil, kartToplami);
        Assert.Equal(1, Sayi(ozet, "yuksek"));
    }

    private static CariRiskProfili Profil(Kiraci k, string cariId, string seviye) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = cariId,
        Kaynak = RiskKaynagi.Hesap, Puan = seviye == RiskSeviyesi.Yuksek ? 80 : 10,
        Seviye = seviye, Sebepler = "[]", KullaniciId = k.KullaniciId,
        HesaplanmaTarihi = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
    };

    // ═══ YEDEK — "saklanan koşu" kartı ↔ koşu kayıtları ═════════════════════

    [Fact]
    public async Task Mutabakat_Yedek_ozet_saklanan_kosu_KAYITLARLA_ESITTIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P24 yedek ozet");
        using var db = vt.Olustur(k);

        db.YedekKayitlari.AddRange(
            new YedekKaydi
            {
                Id = Kimlik.Ulid(), Tur = YedekTuru.Tam, Baslangic = DateTime.UtcNow.AddHours(-5),
                Durum = YedekDurumu.Basarili, Sifreli = true, DisKopya = DisKopyaDurumu.Kopyalandi,
                Dogrulama = YedekDogrulama.Dogrulandi, DogrulamaTarih = DateTime.UtcNow.AddHours(-4),
            },
            new YedekKaydi
            {
                Id = Kimlik.Ulid(), Tur = YedekTuru.Tam, Baslangic = DateTime.UtcNow.AddHours(-2),
                Durum = YedekDurumu.Basarisiz, Sifreli = true, DisKopya = DisKopyaDurumu.Yok,
            });
        await db.SaveChangesAsync();

        var yapilandirma = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Yedek:SaklamaGun"] = "30",
            ["Yedek:RpoDakika"] = "360",
            ["Yedek:RtoDakika"] = "120",
        }).Build();

        var ozet = await new Kuyumcu.Api.Modules.Yedek.YedekServisi(db, Denetim(db), yapilandirma)
            .Ozet(k.Islem);

        Assert.Equal(await db.YedekKayitlari.CountAsync(), Sayi(ozet, "saklananKosu"));
    }
}
