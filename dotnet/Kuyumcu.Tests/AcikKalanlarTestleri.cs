using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Tamamlama raporlarında "AÇIK KALAN" diye bırakılan maddelerin kapanış testleri:
//   • ADIM-6-7-8 §6/2  gider pusulası yazdırılabilir belge
//   • ADIM-6-7-8 §6/3  7 günlük düzenleme süresi takibi
//   • ADIM-A     §5/2  resmî tatil takvimi (10 iş günü bayramları atlamıyordu)
//   • ADIM-A     §5/5  saklama süresi dolunca maskeleme
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TutarYaziTestleri
{
    [Theory]
    [InlineData(0, "SIFIR TL")]
    [InlineData(1, "BİR TL")]
    [InlineData(11, "ONBİR TL")]
    [InlineData(100, "YÜZ TL")]                       // "BİRYÜZ" değil
    [InlineData(1_000, "BİN TL")]                     // "BİRBİN" değil
    [InlineData(2_000, "İKİBİN TL")]
    [InlineData(1_000_000, "BİRMİLYON TL")]           // ama "BİRMİLYON" doğru
    [InlineData(62_288, "ALTMIŞİKİBİNİKİYÜZSEKSENSEKİZ TL")]
    [InlineData(61_042.24, "ALTMIŞBİRBİNKIRKİKİ TL YİRMİDÖRT KR")]
    public void TutarYaziyaDonusur(decimal tutar, string beklenen)
    {
        Assert.Equal(beklenen, TutarYazi.Yaz(tutar));
    }

    [Fact]
    public void NegatifTutarEksiIleYazilir() =>
        Assert.StartsWith("EKSİ ", TutarYazi.Yaz(-500m));

    [Fact]
    public void SinirUstuRakamlaYazilir() =>
        Assert.Contains("1.000.000.000.000", TutarYazi.Yaz(1_000_000_000_000m));
}

public sealed class ResmiTatilTestleri
{
    [Fact]
    public void SabitTatillerTanimli()
    {
        var t = ResmiTatiller.Sabit(2026).ToList();
        Assert.Contains(new DateOnly(2026, 1, 1), t);     // yılbaşı
        Assert.Contains(new DateOnly(2026, 4, 23), t);
        Assert.Contains(new DateOnly(2026, 10, 29), t);
        Assert.Equal(7, t.Count);
    }

    [Fact]
    public void YapilandirmadakiDiniBayramEklenir()
    {
        var ayar = new TakvimAyarlari { ResmiTatiller = ["2026-03-20", "2026-03-21"] };
        var kume = ResmiTatiller.Tumu(2026, ayar);

        Assert.Contains(new DateOnly(2026, 3, 20), kume);
        Assert.Contains(new DateOnly(2026, 1, 1), kume);   // sabitler de duruyor
    }

    [Fact]
    public void BozukBiciminKayitSessizceYutulmaz()
    {
        var ayar = new TakvimAyarlari { ResmiTatiller = ["2026-03-20", "bayram", "20/03/2026"] };

        Assert.Equal(2, ResmiTatiller.Gecersiz(ayar).Count);
        Assert.Contains(new DateOnly(2026, 3, 20), ResmiTatiller.Tumu(2026, ayar));
    }

    [Fact]
    public void DiniBayramiOlmayanYilBildirilir()
    {
        // Madde 1: eksikliği sessizce geçme — süre erken dolar.
        Assert.True(ResmiTatiller.DiniBayramEksik(2026, new TakvimAyarlari()));
        Assert.False(ResmiTatiller.DiniBayramEksik(
            2026, new TakvimAyarlari { ResmiTatiller = ["2026-03-20"] }));
    }

    [Fact]
    public void TatilMasakSuresiniUzatir()
    {
        var motor = new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri { SupheliBildirimIsGunu = 3 });
        var carsamba = new DateTime(2026, 4, 22, 9, 0, 0, DateTimeKind.Utc);

        var tatilsiz = Zaman.UtcdenYerele(motor.SonBildirimTarihi(carsamba));
        // 23 Nisan tatil → bir gün daha kayar
        var tatilli = Zaman.UtcdenYerele(
            motor.SonBildirimTarihi(carsamba, ResmiTatiller.Sabit(2026).ToHashSet()));

        Assert.True(tatilli > tatilsiz, "resmî tatil süreyi uzatmalı");
    }
}

[Collection("veritabani")]
public sealed class PusulaBelgeTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MHurda.HurdaServisi Servis(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()), new MHurda.HurdaAyarlari());

    private static System.Text.Json.JsonElement Kok(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    private async Task<string> HurdaAl(
        KuyumcuDbContext db, Kiraci k, string tip = SaticiTipi.NihaiTuketici)
    {
        await Satis(db).HurdaAlis(new MSatis.HurdaAlisDto
        {
            CariId = k.CariId, AyarId = "K22", GramAgirlik = 10m,
            BirimFiyatTlHasGram = 2_800m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Satici = TestKurulum.HurdaSatici(tip),
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        return kontrol.GiderPusulalari.Where(g => g.TenantId == k.TenantId)
            .OrderByDescending(g => g.Olusturuldu).First().Id;
    }

    [Fact]
    public async Task Belge_VukAlanlariniIcerir_TutarYaziyla()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Belge-vuk"));
        using var db = vt.Olustur(k);
        var id = await HurdaAl(db, k);

        var b = Kok(await Servis(db).Belge(id, k.Islem));

        Assert.Equal("GİDER PUSULASI", b.GetProperty("belgeTuru").GetString());
        Assert.Contains("VUK md.234", b.GetProperty("dayanak").GetString());
        Assert.StartsWith("GP-", b.GetProperty("PusulaNo").GetString());
        Assert.Equal("10000000146", b.GetProperty("satici").GetProperty("tckn").GetString());
        Assert.Contains("Hurda altın", b.GetProperty("mal").GetProperty("cins").GetString());

        var yaziyla = b.GetProperty("tutarlar").GetProperty("yaziyla").GetString()!;
        Assert.Contains("YİRMİBEŞBİN", yaziyla);   // 25.648 TL
        Assert.EndsWith("TL", yaziyla);
    }

    [Fact]
    public async Task Belge_StopajsizdaDogruNotYazar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Belge-not1"));
        using var db = vt.Olustur(k);
        var id = await HurdaAl(db, k);

        var not = Kok(await Servis(db).Belge(id, k.Islem)).GetProperty("not").GetString()!;
        Assert.Contains("tevkifatı yapılmamıştır", not);
    }

    [Fact]
    public async Task Belge_StopajlidaVergiBorcuUyarisiVerir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Belge-not2"));
        using var db = vt.Olustur(k);
        var id = await HurdaAl(db, k, SaticiTipi.EsnafMuaf);

        var b = Kok(await Servis(db).Belge(id, k.Islem));
        Assert.Equal("%2", b.GetProperty("tutarlar").GetProperty("stopajOranYuzde").GetString());
        // PAKET YASAL-4 / Y4-B: stopaj artık dönem borcu olarak izleniyor; belge bunu söyler.
        Assert.Contains("vergi dairesine aittir", b.GetProperty("not").GetString());
    }

    [Fact]
    public async Task Belge_OkumaIziDuser()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Belge-iz"));
        using var db = vt.Olustur(k);
        var id = await HurdaAl(db, k);

        await Servis(db).Belge(id, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var iz = kontrol.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.Eylem == DenetimEylemi.Okuma
                     && d.VarlikTuru == nameof(GiderPusulasi))
            .ToList();
        // ⚠ UX-4d madde 20: GÖRÜNTÜLEME ile YAZDIRMA ayrıldı. Belgeyi ekranda
        // açmak "yazdırıldı" sayılmaz; basım ayrı uçla (POST {id}/yazdirildi)
        // kaydedilir. Beklenti bilerek güncellendi.
        Assert.Contains(iz, x => (x.Detay ?? "").Contains("belgesi görüntülendi"));
        Assert.DoesNotContain(iz, x => (x.Detay ?? "").Contains("YAZDIRILDI"));
    }

    [Fact]
    public async Task Liste_TcknMaskeli_TekilTam()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Belge-maske"));
        using var db = vt.Olustur(k);
        var id = await HurdaAl(db, k);

        var liste = System.Text.Json.JsonSerializer.Serialize(
            await Servis(db).Listele(k.Islem, null, null, 100));
        Assert.Contains("*******0146", liste);
        Assert.DoesNotContain("10000000146", liste);

        var tekil = Kok(await Servis(db).Getir(id, k.Islem));
        Assert.Equal("10000000146", tekil.GetProperty("SaticiTckn").GetString());
    }

    [Fact]
    public async Task SureTakibi_YeniPusulaGecikmisDegil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Sure-yeni"));
        using var db = vt.Olustur(k);
        await HurdaAl(db, k);

        var r = Kok(await Servis(db).SureTakibi(k.Islem));
        Assert.Equal(0, r.GetProperty("gecikmisSayisi").GetInt32());
        Assert.Equal(7, r.GetProperty("sure").GetProperty("gun").GetInt32());
    }

    /// <summary>
    /// PAKET YASAL-4 / Y4-A (K-1): eski test "8 gün önce teslim → gecikmiş 1" bekliyordu —
    /// bu YANLIŞ ALARMDI (pusula alışla aynı anda düzenlenir). İptal edilmemiş eski pusula
    /// süre takibine GİRMEZ; yalnız iptal edilip yerine yenisi düzenlenmeyen girer.
    /// </summary>
    [Fact]
    public async Task SureTakibi_SuresiDolaniYakalar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = IslemBantFikstur.ReferansKur(kurulum, TestKurulum.KiraciKur(kurulum, "Sure-gecmis"));
        using var db = vt.Olustur(k);
        var id = await HurdaAl(db, k);

        // Süreyi geçmişe çek (8 gün önce teslim alınmış gibi)
        using var duzelt = vt.Olustur(k);
        var p = duzelt.GiderPusulalari.Single(g => g.Id == id);
        p.TeslimTarihi = DateTime.UtcNow.AddDays(-8);
        p.DuzenlemeSonTarihi = DateTime.UtcNow.AddDays(-1);
        await duzelt.SaveChangesAsync();

        var r = Kok(await Servis(db).SureTakibi(k.Islem));
        Assert.Equal(0, r.GetProperty("gecikmisSayisi").GetInt32());   // süresinde düzenlenmiş belge

        // İptal edilir, yerine yenisi düzenlenmez → son günü (teslim + 7) geçmiş → BELGESİZ 1.
        using (var iptal = vt.Olustur(k))
        {
            var q = iptal.GiderPusulalari.Single(g => g.Id == id);
            q.IptalEdildi = true; q.IptalGerekce = "test iptali — yanlış satıcı adı yazılmıştı"; q.IptalTarihi = DateTime.UtcNow;
            await iptal.SaveChangesAsync();
        }
        r = Kok(await Servis(db).SureTakibi(k.Islem));
        Assert.Equal(1, r.GetProperty("gecikmisSayisi").GetInt32());
    }
}

[Collection("veritabani")]
public sealed class SaklamaSuresiTestleri(TestVeritabani vt)
{
    private static MMasak.SaklamaSuresiIsi Is(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()));

    private static System.Text.Json.JsonElement Kok(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;

    private static MasakKimlikTespiti Tespit(Kiraci k, DateTime saklamaBitis) => new()
    {
        Id = Kimlik.Ulid(), TenantId = k.TenantId, CariId = k.CariId, IslemTutariTl = 250_000m,
        BelgeTuru = KimlikBelgeTuru.TcKimlik, BelgeNo = "10000000146", AdSoyad = "Eski Müşteri",
        Adres = "Ulus", Uyruk = "TUR", DogumTarihi = new DateOnly(1980, 1, 1),
        TespitEdenKullaniciId = k.KullaniciId, Tarih = DateTime.UtcNow.AddYears(-9),
        SaklamaBitis = saklamaBitis, Olusturuldu = DateTime.UtcNow.AddYears(-9),
    };

    [Fact]
    public async Task SuresiDolmamisKayitMaskelenmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Sak-taze");
        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(Tespit(k, DateTime.UtcNow.AddYears(1)));
        await db.SaveChangesAsync();

        Assert.Equal(0, Kok(await Is(db).Onizleme(k.Islem))
            .GetProperty("suresiDolmus").GetProperty("kimlikTespiti").GetInt32());

        await Is(db).Uygula(k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal("Eski Müşteri",
            kontrol.MasakKimlikTespitleri.Single(m => m.TenantId == k.TenantId).AdSoyad);
    }

    [Fact]
    public async Task SuresiDolmusKayit_KimlikMaskelenir_TutarKorunur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Sak-dolmus");
        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(Tespit(k, DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync();

        var onizleme = Kok(await Is(db).Onizleme(k.Islem));
        Assert.Equal(1, onizleme.GetProperty("suresiDolmus").GetProperty("kimlikTespiti").GetInt32());

        var sonuc = Kok(await Is(db).Uygula(k.Islem));
        Assert.Equal(1, sonuc.GetProperty("maskelenen").GetProperty("kimlikTespiti").GetInt32());

        using var kontrol = vt.OlusturSistem();
        var m = kontrol.MasakKimlikTespitleri.Single(x => x.TenantId == k.TenantId);

        // Kimlik gitti
        Assert.Equal(MMasak.SaklamaSuresiIsi.Maskelenmis, m.AdSoyad);
        Assert.Equal(MMasak.SaklamaSuresiIsi.Maskelenmis, m.BelgeNo);
        Assert.Null(m.Adres);
        Assert.Null(m.DogumTarihi);
        Assert.Null(m.Uyruk);

        // Denetim izi KORUNDU — kayıt silinmedi
        Assert.Equal(250_000m, m.IslemTutariTl);
        Assert.NotEqual(default, m.Tarih);
    }

    [Fact]
    public async Task MaskelemeIkinciKezCalisincaBirSeyDegismez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Sak-idempotent");
        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(Tespit(k, DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync();

        await Is(db).Uygula(k.Islem);
        var ikinci = Kok(await Is(db).Uygula(k.Islem));

        Assert.Equal(0, ikinci.GetProperty("maskelenen").GetProperty("kimlikTespiti").GetInt32());
    }

    [Fact]
    public async Task MaskelemeDenetimIzineYazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Sak-iz");
        using var db = vt.Olustur(k);
        db.MasakKimlikTespitleri.Add(Tespit(k, DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync();

        await Is(db).Uygula(k.Islem);

        using var kontrol = vt.OlusturSistem();
        var iz = kontrol.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && (d.Detay ?? "").Contains("saklama süresi maskelemesi"))
            .ToList();
        Assert.Single(iz);
    }

    [Fact]
    public async Task BaskaKiracininSuresiDolmusKaydinaDokunmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "Sak-A");
        var b = TestKurulum.KiraciKur(kurulum, "Sak-B");

        using var aDb = vt.Olustur(a);
        aDb.MasakKimlikTespitleri.Add(Tespit(a, DateTime.UtcNow.AddDays(-1)));
        await aDb.SaveChangesAsync();

        using var bDb = vt.Olustur(b);
        var sonuc = Kok(await Is(bDb).Uygula(b.Islem));
        Assert.Equal(0, sonuc.GetProperty("maskelenen").GetProperty("kimlikTespiti").GetInt32());

        using var kontrol = vt.OlusturSistem();
        Assert.Equal("Eski Müşteri",
            kontrol.MasakKimlikTespitleri.Single(m => m.TenantId == a.TenantId).AdSoyad);
    }
}
