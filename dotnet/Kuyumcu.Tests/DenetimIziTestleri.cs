using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Denetim izi — SIRADAKI-ADIM-KARAR §2/Ö1 (Aşkın + Mehmet şartı).
//
// Bu dosya yazılmadan önce denetim izinin TEK kanıtı canlı elle doğrulamaydı;
// dinleyici test boru hattında bile yoktu. Madde 1: "oldu" ancak otomatik
// kanıtla. Aşkın'ın şart koştuğu asgari 9 senaryo burada.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class DenetimIziTestleri(TestVeritabani vt)
{
    private static List<DenetimKaydi> Izler(KuyumcuDbContext sistem, string tenantId, string? varlikTuru = null) =>
        sistem.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == tenantId && (varlikTuru == null || d.VarlikTuru == varlikTuru))
            .OrderBy(d => d.Tarih).ToList();

    // ── YAZMA izi: otomatik, her durumda ────────────────────────────────────

    [Fact]
    public async Task Ekleme_OtomatikIzDusurur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Iz-ekleme");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        db.Kasalar.Add(new Kasa
        {
            Id = id, SubeId = k.SubeId, Ad = "İz kasası", ParaBirimi = "TRY",
            Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        var iz = Izler(kontrol, k.TenantId, nameof(Kasa)).Single(d => d.VarlikId == id);
        Assert.Equal(DenetimEylemi.Ekleme, iz.Eylem);
        Assert.False(iz.Hassas);            // Kasa KVKK kapsamında değil
        Assert.Null(iz.Detay);              // eklemede alan listesi yok
    }

    [Fact]
    public async Task Guncelleme_YalnizAlanAdlariniYazar_DegerYazmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Iz-guncelleme");

        const string gizliTelefon = "5551234567";
        const string gizliAdres = "Ulus Kuyumcular Çarşısı 12";

        using var db = vt.Olustur(k);
        var cari = db.Cariler.Single(c => c.Id == k.CariId);
        cari.Telefon = gizliTelefon;
        cari.Adres = gizliAdres;
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        var iz = Izler(kontrol, k.TenantId, nameof(Cari)).Single(d => d.Eylem == DenetimEylemi.Guncelleme);

        Assert.Contains("Telefon", iz.Detay);
        Assert.Contains("Adres", iz.Detay);
        // KVKK veri asgariliği: DEĞER izde ASLA geçmez
        Assert.DoesNotContain(gizliTelefon, iz.Detay);
        Assert.DoesNotContain(gizliAdres, iz.Detay);
        Assert.DoesNotContain("Ulus", iz.Detay);
    }

    [Fact]
    public async Task Silme_IzDusurur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Iz-silme");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        db.Kasalar.Add(new Kasa
        {
            Id = id, SubeId = k.SubeId, Ad = "Silinecek", ParaBirimi = "TRY",
            Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        db.Kasalar.Remove(db.Kasalar.Single(x => x.Id == id));
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        var izler = Izler(kontrol, k.TenantId, nameof(Kasa)).Where(d => d.VarlikId == id).ToList();
        Assert.Contains(izler, d => d.Eylem == DenetimEylemi.Ekleme);
        Assert.Contains(izler, d => d.Eylem == DenetimEylemi.Silme);
    }

    [Fact]
    public async Task HassasVarlik_HassasBayragiylaIsaretlenir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Iz-hassas");

        using var db = vt.Olustur(k);
        var id = Kimlik.Ulid();
        db.Cariler.Add(new Cari
        {
            Id = id, Tur = "MUSTERI", Ad = "Hassas Müşteri", Aktif = true,
            Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        Assert.True(Izler(kontrol, k.TenantId, nameof(Cari)).Single(d => d.VarlikId == id).Hassas);
    }

    [Fact]
    public async Task DenetimKaydininKendisiDenetlenmez_SonsuzDonguYok()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Iz-dongu");

        using var db = vt.Olustur(k);
        db.Kasalar.Add(new Kasa
        {
            Id = Kimlik.Ulid(), SubeId = k.SubeId, Ad = "Tek", ParaBirimi = "TRY",
            Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        // DenetimKaydi türü için iz OLMAMALI
        Assert.Empty(Izler(kontrol, k.TenantId, nameof(DenetimKaydi)));
    }

    [Fact]
    public async Task IzKiraciyaBaglidir_BaskaKiraciGoremez()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "Iz-A");
        var b = TestKurulum.KiraciKur(kurulum, "Iz-B");

        using var aDb = vt.Olustur(a);
        aDb.Kasalar.Add(new Kasa
        {
            Id = Kimlik.Ulid(), SubeId = a.SubeId, Ad = "A kasa", ParaBirimi = "TRY",
            Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });
        await aDb.SaveChangesAsync();

        using var bDb = vt.Olustur(b);
        // B, A'nın izlerini çıplak sorguyla bile göremez (Adım 1 filtresi)
        Assert.DoesNotContain(bDb.DenetimKayitlari.ToList(), d => d.TenantId == a.TenantId);
    }

    [Fact]
    public async Task SistemBaglamindaYazma_KullanicisizIzDusurur()
    {
        // Arka plan işleri kullanıcısız yazar; iz yine düşmeli.
        // ⚠ P-72 SAPMASI: bu test önceden KurSnapshot eklerdi; P-72 kullanıcısız
        // KurSnapshot EKLEMESİNİ izden ayırdı (teknik günlüğe). Testin niyeti
        // ("kullanıcısız yazma izi düşer") dışlanmayan bir türle (Kasa) korunur.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Iz-sistem");

        using var sistem = vt.OlusturSistem();
        var id = Kimlik.Ulid();
        sistem.Kasalar.Add(new Kasa
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Sistem kasası",
            ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });
        await sistem.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        var iz = Izler(kontrol, k.TenantId, nameof(Kasa)).Single(d => d.VarlikId == id);
        Assert.Equal(DenetimEylemi.Ekleme, iz.Eylem);
        Assert.Null(iz.KullaniciId);
    }

    // ── PAKET P-72: makine üretimi KurSnapshot denetim izinden ayrılır ───────

    /// <summary>Günlüğe düşen satırları yakalayan test günlüğü.</summary>
    private sealed class YakalayanGunluk : ILogger<DenetimYazmaDinleyicisi>
    {
        public List<(LogLevel Duzey, string Metin)> Satirlar { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (Satirlar) Satirlar.Add((logLevel, formatter(state, exception)));
        }
    }

    /// <summary>
    /// Üretim boru hattıyla aynı dinleyiciler, ama denetim dinleyicisine VERİLEN
    /// günlük ve (isteğe bağlı) HttpContext erişimi takılı — TestOrtami'nin
    /// Kur() yöntemiyle aynı kurgu.
    /// </summary>
    private KuyumcuDbContext Bagla(IKiraciBaglami baglam, ILogger<DenetimYazmaDinleyicisi> gunluk,
        IHttpContextAccessor? erisim = null)
    {
        var ayar = new DbContextOptionsBuilder<KuyumcuDbContext>()
            .UseMySql(vt.Baglanti, ServerVersion.Parse("8.4.0-mysql"))
            .ReplaceService<IModelCacheKeyFactory, KiraciModelOnbellegi>()
            .AddInterceptors(
                new KiraciYazmaDenetcisi(NullLogger<KiraciYazmaDenetcisi>.Instance),
                new DenetimYazmaDinleyicisi(erisim ?? new HttpContextAccessor(), gunluk),
                new MasakHashDinleyicisi())
            .Options;
        return new KuyumcuDbContext(ayar, baglam, TestVeritabani.TestSifreleyici);
    }

    private static HttpContextAccessor OturumluErisim(Kiraci k)
    {
        var erisim = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        erisim.HttpContext!.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity([
                new System.Security.Claims.Claim("sub", k.KullaniciId),
                new System.Security.Claims.Claim("subeId", k.SubeId),
            ], "test"));
        return erisim;
    }

    private static KurSnapshot YeniKur(string tenantId, string kaynak) => new()
    {
        Id = Kimlik.Ulid(), TenantId = tenantId, AltinHasFiyati = 7_000m,
        Kaynak = kaynak, Olusturuldu = DateTime.UtcNow,
    };

    [Fact]
    public async Task DenetimIzi_ZamanlayiciKurSnapshotu_IzeYazilmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P72-zamanlayici");

        using var once = vt.OlusturSistem();
        var onceSayi = Izler(once, k.TenantId).Count;

        // Zamanlayıcı bağlamı: HttpContext YOK → kullanıcı yok.
        using var db = Bagla(new SabitKiraciBaglami(k.TenantId), new YakalayanGunluk());
        var kur = YeniKur(k.TenantId, "ALTINKAYNAK");
        db.KurSnapshots.Add(kur);
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        Assert.NotNull(kontrol.KurSnapshots.AsNoTracking().SingleOrDefault(s => s.Id == kur.Id)); // kayıt YAZILDI
        Assert.Equal(onceSayi, Izler(kontrol, k.TenantId).Count);                                  // iz ARTMADI
        Assert.DoesNotContain(Izler(kontrol, k.TenantId, nameof(KurSnapshot)), d => d.VarlikId == kur.Id);
    }

    [Fact]
    public async Task DenetimIzi_ManuelKurGirisi_IzdeKalir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P72-manuel");
        var gunluk = new YakalayanGunluk();

        // (1) MANUEL giriş — oturumlu kullanıcı → iz KALIR, kullanıcı damgalı.
        var kur = YeniKur(k.TenantId, "MANUAL");
        using (var db = Bagla(new SabitKiraciBaglami(k.TenantId), gunluk, OturumluErisim(k)))
        {
            db.KurSnapshots.Add(kur);
            await db.SaveChangesAsync();
        }

        // (2) Manuel kur İPTALİ (Modified) — iz KALIR.
        using (var db = Bagla(new SabitKiraciBaglami(k.TenantId), gunluk, OturumluErisim(k)))
        {
            var s = db.KurSnapshots.Single(x => x.Id == kur.Id);
            s.IptalEdildi = true;
            s.IptalGerekce = "P-72 testi: yanlış girilen kur iptal edildi";
            s.IptalTarihi = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // (3) Kullanıcısız GÜNCELLEME de dışlanmaz — dışlama yalnız EKLEME içindir.
        // ⚠ HttpContextAccessor AsyncLocal paylaşır: yukarıda kurulan oturum bu
        // akışta hâlâ görünür; açıkça boşaltılmazsa "kullanıcısız" olmaz.
        using (var db = Bagla(new SistemKiraciBaglami(), gunluk, new HttpContextAccessor { HttpContext = null }))
        {
            var s = db.KurSnapshots.Single(x => x.Id == kur.Id);
            s.IptalGerekce = "P-72 testi: sistem bağlamında düzeltilen gerekçe metni";
            await db.SaveChangesAsync();
        }

        using var kontrol = vt.OlusturSistem();
        var izler = Izler(kontrol, k.TenantId, nameof(KurSnapshot)).Where(d => d.VarlikId == kur.Id).ToList();
        var ekleme = Assert.Single(izler, d => d.Eylem == DenetimEylemi.Ekleme);
        Assert.Equal(k.KullaniciId, ekleme.KullaniciId);
        Assert.Contains(izler, d => d.Eylem == DenetimEylemi.Guncelleme && d.KullaniciId == k.KullaniciId
                                    && d.Detay!.Contains(nameof(KurSnapshot.IptalEdildi)));
        Assert.Contains(izler, d => d.Eylem == DenetimEylemi.Guncelleme && d.KullaniciId == null);
        Assert.Empty(gunluk.Satirlar); // hiçbiri dışlanmadı
    }

    [Fact]
    public async Task DenetimIzi_KurDisiVarliklar_HalaYazilir()
    {
        // Liste TAM OLARAK tek tür içerir: KurSnapshot.
        Assert.Equal([typeof(KurSnapshot)], DenetimYazmaDinleyicisi.MakineUretimiDislananTurler);

        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P72-kurdisi");
        var gunluk = new YakalayanGunluk();

        // Kullanıcısız (zamanlayıcı benzeri) bağlamda BAŞKA türler eklenir → iz DÜŞER.
        using var db = Bagla(new SabitKiraciBaglami(k.TenantId), gunluk);
        var kasaId = Kimlik.Ulid();
        db.Kasalar.Add(new Kasa
        {
            Id = kasaId, SubeId = k.SubeId, Ad = "P72 kasası", ParaBirimi = "TRY",
            Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });
        var khId = Kimlik.Ulid();
        db.KasaHareketleri.Add(new KasaHareket
        {
            Id = khId, TenantId = k.TenantId, KasaId = k.KasaId, HareketId = null,
            Tutar = 1m, ParaBirimi = "TRY", OdemeTuru = "NAKIT",
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
        });
        var hId = Kimlik.Ulid();
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = hId, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "EMANET_GIRIS",
            Tarih = DateTime.UtcNow, TlTutari = 0m, HasGrami = 1m,
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        var izler = Izler(kontrol, k.TenantId);
        Assert.Contains(izler, d => d.VarlikTuru == nameof(Kasa) && d.VarlikId == kasaId && d.Eylem == DenetimEylemi.Ekleme);
        Assert.Contains(izler, d => d.VarlikTuru == nameof(KasaHareket) && d.VarlikId == khId && d.Eylem == DenetimEylemi.Ekleme);
        Assert.Contains(izler, d => d.VarlikTuru == nameof(Hareket) && d.VarlikId == hId && d.Eylem == DenetimEylemi.Ekleme);
        Assert.Empty(gunluk.Satirlar);
    }

    [Fact]
    public async Task DenetimIzi_DislananOlay_TeknikGunlugeDuser()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "P72-gunluk");
        var gunluk = new YakalayanGunluk();

        using var db = Bagla(new SabitKiraciBaglami(k.TenantId), gunluk);
        var kur1 = YeniKur(k.TenantId, "ALTINKAYNAK");
        var kur2 = YeniKur(k.TenantId, "TRUNCGIL");
        db.KurSnapshots.AddRange(kur1, kur2);
        await db.SaveChangesAsync();

        // Dışlanan HER olay için en az bir Information satırı — kimliğiyle.
        foreach (var kur in new[] { kur1, kur2 })
            Assert.Contains(gunluk.Satirlar, s => s.Duzey == LogLevel.Information
                && s.Metin.Contains(nameof(KurSnapshot)) && s.Metin.Contains(kur.Id)
                && s.Metin.Contains(k.TenantId));
    }

    // ── OKUMA izi: açık çağrı ────────────────────────────────────────────────

    [Fact]
    public async Task OkumaKaydet_HassasOkumayiKaydeder()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Iz-okuma");

        using var db = vt.Olustur(k);
        var servis = new DenetimServisi(db, new HttpContextAccessor());
        await servis.OkumaKaydet(k.Islem, nameof(Cari), k.CariId, "müşteri kartı açıldı");

        using var kontrol = vt.OlusturSistem();
        var iz = Izler(kontrol, k.TenantId, nameof(Cari)).Single(d => d.Eylem == DenetimEylemi.Okuma);
        Assert.Equal(k.CariId, iz.VarlikId);
        Assert.Equal(k.KullaniciId, iz.KullaniciId);
        Assert.True(iz.Hassas);
        Assert.Equal("müşteri kartı açıldı", iz.Detay);
    }

    [Fact]
    public async Task ListeOkumaKaydet_AdetYazar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "Iz-liste");

        using var db = vt.Olustur(k);
        await new DenetimServisi(db, new HttpContextAccessor())
            .ListeOkumaKaydet(k.Islem, "MasakKimlikTespiti", 42, "denetim ekranı");

        using var kontrol = vt.OlusturSistem();
        var iz = Izler(kontrol, k.TenantId, "MasakKimlikTespiti").Single();
        Assert.Contains("42 kayıt", iz.Detay);
        Assert.Contains("denetim ekranı", iz.Detay);
        Assert.True(iz.Hassas);
    }

    // ── Hassas varlık listesi ve kırpma ──────────────────────────────────────

    [Theory]
    [InlineData("Cari", true)]
    [InlineData("MasakKimlikTespiti", true)]
    [InlineData("MasakBeyani", true)]
    [InlineData("MasakSupheliBildirim", true)]
    [InlineData("Kasa", false)]
    [InlineData("Urun", false)]
    [InlineData("Hareket", false)]
    public void HassasVarlikListesiDogru(string tur, bool beklenen)
    {
        Assert.Equal(beklenen, HassasVarliklar.Mi(tur));
    }

    [Fact]
    public void Detay1000KarakterdeKirpilir()
    {
        var uzun = new string('x', 5_000);
        var iz = DenetimServisi.Olustur("t", "u", DenetimEylemi.Okuma, "Cari", null, uzun, null);
        Assert.Equal(1000, iz.Detay!.Length);
    }

    [Fact]
    public void HttpContextYoksaIpVeUserAgentNull()
    {
        var iz = DenetimServisi.Olustur("t", "u", DenetimEylemi.Okuma, "Cari", null, null, new HttpContextAccessor());
        Assert.Null(iz.IpAdresi);
        Assert.Null(iz.UserAgent);
    }
}
