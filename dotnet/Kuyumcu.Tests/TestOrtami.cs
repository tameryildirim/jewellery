using System.Text.Json;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Test ortamı — kararı: Aşkın (QA), 2026-09-10 oyu.
// Gerçek MySQL'e karşı çalışır (EF InMemory transaction desteklemez, SQLite
// decimal hassasiyetini uygulamaz — has gram testleri yanlış güven verirdi).
// YALNIZ "kuyumcu_test" veritabanını kurar/siler; geliştirme veritabanı
// (kuyumcu_net) hiç açılmaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TestVeritabani
{
    public string Baglanti { get; }

    public TestVeritabani()
    {
        var vt = Environment.GetEnvironmentVariable("KUYUMCU_TEST_DB") ?? "kuyumcu_test";
        if (vt is "kuyumcu" or "kuyumcu_net")
            throw new InvalidOperationException("Testler geliştirme veritabanında çalıştırılamaz.");
        // KAPORA-ESZAMAN tur 2 (doğrulayıcı DÜŞÜK, sır hijyeni): parola KODDA DURMAZ. Önce ortam
        // değişkeni; yoksa yerel docker-compose.yml'deki MYSQL_ROOT_PASSWORD SATIRI (yorum satırı değil).
        // İkisi de yoksa açık hata — gömülü yedek değer YOK.
        var sifre = Environment.GetEnvironmentVariable("KUYUMCU_TEST_MYSQL_SIFRE")
            ?? ComposeParolasi()
            ?? throw new InvalidOperationException(
                "Test MySQL parolası bulunamadı: KUYUMCU_TEST_MYSQL_SIFRE ortam değişkenini verin ya da docker-compose.yml'de MYSQL_ROOT_PASSWORD tanımlı olsun.");
        Baglanti = $"server=localhost;port=3306;database={vt};user=root;password={sifre}";

        SemayiBirKezKur();
    }

    /// <summary>Çalışma dizininden yukarı doğru docker-compose.yml arar; "MYSQL_ROOT_PASSWORD:" ile BAŞLAYAN satırı okur.</summary>
    private static string? ComposeParolasi()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            var yol = Path.Combine(d.FullName, "docker-compose.yml");
            if (!File.Exists(yol)) continue;
            foreach (var satir in File.ReadLines(yol))
            {
                var m = System.Text.RegularExpressions.Regex.Match(satir, @"^\s*MYSQL_ROOT_PASSWORD:\s*(\S.*?)\s*$");
                if (m.Success) return m.Groups[1].Value.Trim('"', '\'');
            }
            return null;
        }
        return null;
    }

    // ── SÜREÇTE BİR KEZ ──────────────────────────────────────────────────────
    // Şema kurulumu (EnsureDeleted + Migrate) artık süreç başına BİR kez çalışır.
    //
    // NEDEN: PLAN-2 Adım 0a ile ikinci bir fikstür (HttpFikstur) eklendi. Her
    // fikstür kendi TestVeritabani'sini kurunca, İKİNCİSİNİN EnsureDeleted'i
    // birincisinin koşan testlerinin altından veritabanını siliyordu —
    // 132 test birden kırmızıya döndü. Kanıt: 2026-09-13 koşumu.
    //
    // Tek fikstür varken davranış aynıdır (zaten bir kez çalışıyordu); artık
    // kaç fikstür eklenirse eklensin aynı kalır.
    private static readonly object kilit = new();
    private static bool kuruldu;

    private void SemayiBirKezKur()
    {
        lock (kilit)
        {
            if (kuruldu) return;
            using var db = OlusturSistem();
            db.Database.EnsureDeleted();
            // Aşkın'ın koşulu: testler MIGRATION ile kurulan şemaya karşı çalışır,
            // EnsureCreated ile değil — aksi halde test şeması üretimdekinden sapabilir.
            db.Database.Migrate();
            kuruldu = true;
        }
    }

    // ── ADIM1-KARAR §3/Ç3 ────────────────────────────────────────────────────
    // Parametresiz Olustur() BİLİNÇLİ OLARAK SİLİNDİ. Bağlamsız bir test
    // DbContext'i artık derlenmez; aksi hâlde testler üretimden FARKLI bir
    // yolda sessizce yeşil kalırdı (Aşkın'ın "test şeması üretimden sapmasın"
    // ilkesinin bağlam karşılığı).
    //
    //   OlusturSistem()  → fikstür kurulumu ve yan etki doğrulaması (filtresiz)
    //   Olustur(kiraci)  → servis testi; üretimdeki istek bağlamının aynısı
    /// <summary>Testlerde sabit anahtar: şifreli çıktı okunabilir kalsın, ama GERÇEK şifreleme çalışsın.</summary>
    public static readonly IAlanSifreleyici TestSifreleyici =
        new AesGcmAlanSifreleyici(Enumerable.Range(1, AesGcmAlanSifreleyici.AnahtarBayt).Select(i => (byte)i).ToArray());

    private KuyumcuDbContext Kur(IKiraciBaglami baglam, IHttpContextAccessor? erisim = null)
    {
        var ayar = new DbContextOptionsBuilder<KuyumcuDbContext>()
            .UseMySql(Baglanti, ServerVersion.Parse("8.4.0-mysql"))
            .ReplaceService<IModelCacheKeyFactory, KiraciModelOnbellegi>()
            // Üretimle AYNI boru hattı — ikisi birden. Aşkın'ın tespiti (2026-09-11):
            // denetim dinleyicisi burada YOKTU, dolayısıyla hiçbir test onu görmüyordu.
            .AddInterceptors(
                new KiraciYazmaDenetcisi(NullLogger<KiraciYazmaDenetcisi>.Instance),
                new DenetimYazmaDinleyicisi(erisim ?? new HttpContextAccessor()),
                new MasakHashDinleyicisi())
            .Options;
        return new KuyumcuDbContext(ayar, baglam, TestSifreleyici);
    }

    /// <summary>Filtresiz bağlam — kurulum ve doğrulama için.</summary>
    public KuyumcuDbContext OlusturSistem() => Kur(new SistemKiraciBaglami());

    /// <summary>Kiracıya sabitlenmiş bağlam — üretimdeki istek bağlamının aynısı.</summary>
    public KuyumcuDbContext Olustur(Kiraci kiraci) => Kur(new SabitKiraciBaglami(kiraci.TenantId));

    /// <summary>
    /// Kiracıya sabitlenmiş bağlam + VERİLEN HttpContext erişimi — bağlam damgası
    /// (paket 48) oturumun "subeId" claim'ini okuduğu için testin o claim'i
    /// kurabilmesi gerekir.
    /// </summary>
    public KuyumcuDbContext Olustur(Kiraci kiraci, IHttpContextAccessor erisim)
        => Kur(new SabitKiraciBaglami(kiraci.TenantId), erisim);

    /// <summary>Kiracı kimliğiyle bağlam.</summary>
    public KuyumcuDbContext Olustur(string kiraciId) => Kur(new SabitKiraciBaglami(kiraciId));

    /// <summary>
    /// PAKET P-04 (Y-1): ŞUBE SÜZGECİ AÇIK bağlam — "tüm şubeler" ayrıcalığı
    /// olmayan bir oturumun (ör. TEZGAHTAR) gördüğü kapsamın aynısı.
    /// <paramref name="subeId"/> boş verilirse bağlam FAIL-CLOSED patlar; sözleşmenin
    /// 3. maddesi bu yolla sınanır.
    /// </summary>
    public KuyumcuDbContext OlusturSubeli(string kiraciId, string? subeId)
        => Kur(new SabitKiraciBaglami(kiraciId, subeId, tumSubeler: false));
}

[CollectionDefinition("veritabani")]
public sealed class VeritabaniKoleksiyonu : ICollectionFixture<TestVeritabani>;

/// <summary>Bir test kiracısının (işletmenin) hazır kimlikleri.</summary>
public sealed class Kiraci
{
    public required string TenantId { get; init; }
    public required string SubeId { get; init; }
    public required string KullaniciId { get; init; }
    public required string KasaId { get; init; }
    public required string CariId { get; init; }
    public required string UrunId { get; init; }
    public required string KurId { get; init; }
    public required decimal UrunHasGrami { get; init; }
    public AktifKullanici Islem => new(KullaniciId, TenantId, SubeId, "SAHIP");
}

public static class TestKurulum
{
    public const decimal KasaAcilis = 250_000m;

    /// <summary>
    /// Denetim servisi — GERÇEK örnek (paket 34 / M115). Opsiyonel null parametre
    /// YASAKTIR: null'da iz düşmez ve sessiz bir KVKK açığı doğardı (Aşkın koşulu).
    /// </summary>
    public static DenetimServisi Denetim(KuyumcuDbContext db) => new(db, new HttpContextAccessor());

    /// <summary>Rapor servisi — denetim izi bağlı, üretimdeki DI ile aynı biçimde.</summary>
    public static Kuyumcu.Api.Modules.Rapor.RaporServisi RaporServisi(
        KuyumcuDbContext db, Kuyumcu.Api.Modules.Maliyet.KarZararServisi? karZarar = null)
        => new(db, Denetim(db), karZarar);

    /// <summary>Tam donanımlı bir kiracı kurar: şube, kullanıcı, kasa, cari, ürün+stok, kur.</summary>
    /// <summary>
    /// PAKET GOC-C (K14): kesirli miktar YALNIZ GRAM birimli üründe geçerlidir (ADET birimde tam sayı).
    /// Kesir/yuvarlama sınırını ölçen testler kiracının ürününü GRAM birimine çevirir; has gram AYNEN kalır.
    /// </summary>
    public static void UrunuGramBirimiYap(KuyumcuDbContext db, string urunId)
    {
        var u = db.Urunler.Single(x => x.Id == urunId);
        u.Birim = "GRAM";
        db.SaveChanges();
    }

    public static Kiraci KiraciKur(
        KuyumcuDbContext db, string ad,
        string kasaDoviz = "TRY", decimal urunGram = 10m, string ayarId = "K22", decimal stokAdet = 100m)
    {
        var now = DateTime.UtcNow;
        var tenantId = Kimlik.Ulid();
        var subeId = Kimlik.Ulid();
        var kullaniciId = Kimlik.Ulid();
        var kasaId = Kimlik.Ulid();
        var cariId = Kimlik.Ulid();
        var urunId = Kimlik.Ulid();
        var kurId = Kimlik.Ulid();

        var milyem = Kuyumcu.Core.Domain.Ayarlar.Bul(ayarId)!.Milyem;
        var hasGrami = Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(urunGram, 0m, ayarId);

        db.Tenants.Add(new Tenant { Id = tenantId, Ad = ad, Plan = "PRO", Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.Subeler.Add(new Sube { Id = subeId, TenantId = tenantId, Ad = "Merkez", Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.Kullanicilar.Add(new Kullanici
        {
            Id = kullaniciId, TenantId = tenantId, SubeId = subeId, Ad = "Test", Soyad = "Kullanici",
            Email = $"{kullaniciId}@test.local", SifreHash = "x", Rol = "SAHIP", Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        db.Kasalar.Add(new Kasa
        {
            Id = kasaId, TenantId = tenantId, SubeId = subeId, Ad = "Kasa",
            ParaBirimi = kasaDoviz, Bakiye = KasaAcilis, Aktif = true, Guncellendi = now,
        });
        db.Cariler.Add(new Cari
        {
            Id = cariId, TenantId = tenantId, Tur = "MUSTERI", Ad = $"{ad} müşterisi",
            Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.CariBakiyeler.Add(new CariBakiye
        {
            CariId = cariId, TenantId = tenantId, TlBakiye = 0m, HasGramBakiye = 0m, Guncellendi = now,
        });
        db.Urunler.Add(new Urun
        {
            Id = urunId, TenantId = tenantId, Tip = "ZIYNET", Kod = $"K-{urunId[..8]}", Ad = "Test bilezik",
            AyarId = ayarId, Milyem = milyem, GramAgirlik = urunGram, TasAgirlik = 0m, HasGrami = hasGrami,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = tenantId, SubeId = subeId, UrunId = urunId,
            Adet = stokAdet, ToplamHasGram = stokAdet * hasGrami, Guncellendi = now,
        });
        db.KurSnapshots.Add(new KurSnapshot
        {
            Id = kurId, TenantId = tenantId, AltinHasFiyati = 6_827.20m, GramAltinFiyati = 6_861.51m,
            Kaynak = "MANUAL", Olusturuldu = now,
        });
        db.SaveChanges();

        return new Kiraci
        {
            TenantId = tenantId, SubeId = subeId, KullaniciId = kullaniciId, KasaId = kasaId,
            CariId = cariId, UrunId = urunId, KurId = kurId, UrunHasGrami = hasGrami,
        };
    }

    /// <summary>
    /// Kiracının kur fotoğrafına döviz kurlarını yazar (paket 27).
    /// <para>
    /// ⚠ <c>KiraciKur</c> varsayılanı USD/EUR/GBP kurlarını BOŞ bırakır ve bu
    /// bilinçlidir: kur yokluğu bir HATA DEĞİL, fail-closed kapının sınandığı
    /// normal hâldir. Döviz işlemi yapacak test kuru AÇIKÇA kurar; böylece
    /// "GBP kuru yokken red" testi ayrıca kurulum yapmak zorunda kalmaz.
    /// </para>
    /// </summary>
    public static void KurDoviz(
        KuyumcuDbContext db, string kurId,
        decimal? usd = 40m, decimal? eur = 44m, decimal? gbp = null)
    {
        var s = db.KurSnapshots.Single(x => x.Id == kurId);
        s.UsdTry = usd; s.EurTry = eur; s.GbpTry = gbp;
        db.SaveChanges();
    }

    // ── PAKET F-2-min — DAVET ÜRETİMİ (MERKEZÎ) ──────────────────────────────
    //
    // ⚠ Kayıt ucu artık davet zorunlu kılıyor. Davet üretimi TEK yerde durur:
    //   her test kendi jetonunu elle kurarsa, kuralın bir sonraki değişiminde
    //   (ör. jeton biçimi) hepsi tek tek düzeltilmek zorunda kalırdı.

    /// <summary>
    /// Tek kullanımlık bir davet yazar ve HAM jetonu döndürür.
    /// </summary>
    /// <param name="gecerlilikSaat">Negatif verilirse SÜRESİ GEÇMİŞ davet üretir.</param>
    public static (string Jeton, string DavetId) DavetOlustur(
        KuyumcuDbContext db, int denemeGun = 14, double gecerlilikSaat = 72,
        bool iptal = false, bool kullanildi = false, string? not = null)
    {
        var now = DateTime.UtcNow;
        var jeton = Kuyumcu.Api.Modules.Sistem.SistemServisi.JetonUret();
        var davet = new KiraciDaveti
        {
            Id = Kimlik.Ulid(),
            JetonHash = Kuyumcu.Api.Modules.Sistem.SistemServisi.Ozet(jeton),
            Email = null,
            IsletmeAdi = null,
            DenemeGun = denemeGun,
            Gecerlilik = now.AddHours(gecerlilikSaat),
            KullanildiMi = kullanildi,
            Iptal = iptal,
            OlusturanNot = not,
            Olusturuldu = now,
            Guncellendi = now,
        };
        db.Davetler.Add(davet);
        db.SaveChanges();
        return (jeton, davet.Id);
    }

    /// <summary>Kiracının durumunu/deneme bitişini doğrudan yazar (kapı testleri).</summary>
    public static void KiraciDurumuYaz(
        KuyumcuDbContext db, string tenantId, string durum, DateTime? denemeBitis = null)
    {
        var t = db.Tenants.Single(x => x.Id == tenantId);
        t.Durum = durum;
        t.DenemeBitis = denemeBitis;
        db.SaveChanges();
    }

    /// <summary>Verilen para biriminde ikinci bir kasa ekler (döviz tahsilat testleri).</summary>
    public static string KasaEkle(
        KuyumcuDbContext db, string tenantId, string subeId,
        string paraBirimi, decimal acilis = 0m, string ad = "Döviz Kasa")
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.Kasalar.Add(new Kasa
        {
            Id = id, TenantId = tenantId, SubeId = subeId, Ad = ad,
            ParaBirimi = paraBirimi, Bakiye = acilis, Aktif = true, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    /// <summary>İkinci bir şube ekler (transfer testleri için).</summary>
    public static string SubeEkle(KuyumcuDbContext db, string tenantId, string ad = "Sube-2")
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.Subeler.Add(new Sube { Id = id, TenantId = tenantId, Ad = ad, Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.SaveChanges();
        return id;
    }

    /// <summary>
    /// Geçerli bir hurda satıcısı (Adım 6). TCKN 10000000146 doğrulama
    /// algoritmasından geçer — uydurma numara reddedildiği için gereklidir.
    /// </summary>
    public static Kuyumcu.Api.Modules.Hurda.HurdaSaticiDto HurdaSatici(
        string tip = Kuyumcu.Api.Data.SaticiTipi.NihaiTuketici) => new()
    {
        SaticiTipi = tip,
        AdSoyad = "Hurda Satıcısı",
        Tckn = "10000000146",
        Adres = "Ulus, Ankara",
    };

    // ── Okuma yardımcıları ───────────────────────────────────────────────────

    public static (decimal Tl, decimal Has) CariBakiye(KuyumcuDbContext db, string cariId)
    {
        var b = db.CariBakiyeler.AsNoTracking().Single(x => x.CariId == cariId);
        return (b.TlBakiye, b.HasGramBakiye);
    }

    public static decimal KasaBakiye(KuyumcuDbContext db, string kasaId) =>
        db.Kasalar.AsNoTracking().Single(k => k.Id == kasaId).Bakiye;

    /// <summary>Servis cevabındaki metin alanı; alan yoksa ya da null ise <c>null</c>.</summary>
    public static string? Metin(object cevap, params string[] yol)
    {
        var oge = JsonDocument.Parse(JsonSerializer.Serialize(cevap)).RootElement;
        foreach (var a in yol)
        {
            if (!oge.TryGetProperty(a, out oge)) return null;
        }
        return oge.ValueKind == JsonValueKind.String ? oge.GetString() : null;
    }

    /// <summary>Servis cevabındaki bool alan; alan yoksa <c>false</c>.</summary>
    public static bool Bayrak(object cevap, params string[] yol)
    {
        var oge = JsonDocument.Parse(JsonSerializer.Serialize(cevap)).RootElement;
        foreach (var a in yol)
        {
            if (!oge.TryGetProperty(a, out oge)) return false;
        }
        return oge.ValueKind == JsonValueKind.True;
    }

    /// <summary>Servislerin döndürdüğü anonim nesneden iç içe alan okur (JSON üzerinden).</summary>
    public static decimal Sayi(object cevap, params string[] yol)
    {
        var oge = JsonDocument.Parse(JsonSerializer.Serialize(cevap)).RootElement;
        foreach (var a in yol) oge = oge.GetProperty(a);
        return oge.GetDecimal();
    }
}
