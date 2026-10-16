using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ICSharpCode.SharpZipLib.Zip;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Disaaktarim;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// VERİ DIŞA AKTARIMI TESTLERİ — PLAN-2 Adım 4
//
// Mehmet'in VETO-1'i altı şart koydu. Her şart için EN AZ BİR test vardır ve
// testin adı hangi şartı sınadığını söyler. Şartlardan biri kodda gevşetilirse
// ilgili test kırmızıya döner — bu, vetonun yaşamaya devam etmesinin tek yolu.
//
// ⚠ BERTAN'IN UYARISI mutasyonla sınanır: "kiracı filtresini BİR KEZ yanlış
//    yaparsanız tüm müşterilerin verisi tek dosyada dışarı çıkar."
//    `Kiraci_A_export_dosyasinda_Kiraci_B_verisi_BULUNMAZ` tam bunu ölçer.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CsvYaziciTestleri
{
    [Theory]
    [InlineData("basit", "basit")]
    [InlineData("virgül,içerir", "\"virgül,içerir\"")]
    [InlineData("tırnak\"içerir", "\"tırnak\"\"içerir\"")]
    [InlineData("satır\nsonu", "\"satır\nsonu\"")]
    [InlineData("", "")]
    public void Kacis_RFC_4180_e_uyar(string girdi, string beklenen)
        => Assert.Equal(beklenen, CsvYazici.Hucre(girdi));

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+GEÇ()")]
    [InlineData("-2+3")]
    [InlineData("@SUM(A1)")]
    public void FORMUL_ile_baslayan_hucre_etkisizlestirilir(string girdi)
    {
        // CSV enjeksiyonu: müşteri adı "=cmd|..." olabilir. Excel bunu ÇALIŞTIRIR.
        var h = CsvYazici.Hucre(girdi);
        Assert.StartsWith("'", h.TrimStart('"'));
    }

    [Fact]
    public void Ondalik_ayraci_NOKTA_dir_kulturden_bagimsiz()
    {
        // Başka bir programa aktarılacak veri Türkçe virgüllü ondalıkla taşınmaz.
        Assert.Equal("1234.56", CsvYazici.Sayi(1234.56m));
    }

    [Fact]
    public void Tarih_ISO_8601_UTC_dir()
        => Assert.Equal("2026-09-14T08:30:00Z",
            CsvYazici.Tarih(new DateTime(2026, 9, 14, 8, 30, 0, DateTimeKind.Utc)));

    [Fact]
    public void Excel_BOM_u_yazilir()
    {
        // BOM olmadan Excel UTF-8'i tanımaz; "Şişli" → "Ã?iÅ?li".
        Assert.Equal([0xEF, 0xBB, 0xBF], CsvYazici.Bom);
    }
}

public sealed class MaskeTestleri
{
    [Fact]
    public void Kimlik_son_dort_hane_haric_maskelenir()
        => Assert.Equal("*******0146", Maske.Kimlik("10000000146", maskeli: true));

    [Fact]
    public void Maskesiz_modda_deger_aynen_gecer()
        => Assert.Equal("10000000146", Maske.Kimlik("10000000146", maskeli: false));

    [Fact]
    public void Kisa_deger_tamamen_maskelenir()
        => Assert.Equal("****", Maske.Kimlik("1234", maskeli: true));

    [Fact]
    public void Eposta_alan_adi_korunur_yerel_kisim_gizlenir()
        => Assert.Equal("a***@ornek.com", Maske.Eposta("ayse@ornek.com", maskeli: true));

    [Fact]
    public void Adres_TAMAMEN_gizlenir()
    {
        // Kısmi adres de kimliklendiricidir: "Şişli/İstanbul" + ad = kişi.
        Assert.Equal("[MASKELİ]", Maske.Adres("Şişli, İstanbul", maskeli: true));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SERVİS — kiracı sınırı ve içerik
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class DisaAktarimServisTestleri(TestVeritabani vt)
{
    private static DisaAktarimServisi Servis(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new SahteErisim()));

    private sealed class SahteErisim : Microsoft.AspNetCore.Http.IHttpContextAccessor
    {
        public Microsoft.AspNetCore.Http.HttpContext? HttpContext { get; set; }
    }

    private static DisaAktarimTalepDto Dto(string? kapsam = null, string? onay = null) => new()
    {
        Gerekce = "Mali müşavire yıl sonu devir dosyası hazırlanıyor",
        Kapsam = kapsam is null ? null : [kapsam],
        TamKimlikOnayi = onay,
    };

    /// <summary>Şifreli ZIP'i çözer, içindeki dosyaları ad → metin olarak verir.</summary>
    private static async Task<Dictionary<string, string>> Ac(
        DisaAktarimServisi s, DisaAktarimTalebi talep, string parola)
    {
        using var bellek = new MemoryStream();
        await s.Yaz(bellek, talep, parola);
        bellek.Position = 0;

        var sonuc = new Dictionary<string, string>();
        using var zip = new ZipFile(bellek) { Password = parola };
        foreach (ZipEntry giris in zip)
        {
            if (!giris.IsFile) continue;
            using var akis = zip.GetInputStream(giris);
            using var oku = new StreamReader(akis, Encoding.UTF8);
            sonuc[giris.Name] = await oku.ReadToEndAsync();
        }
        return sonuc;
    }

    private static async Task<(DisaAktarimTalebi Talep, string Parola)> TalepKur(
        KuyumcuDbContext db, Kiraci k, DisaAktarimTalepDto dto)
    {
        var s = Servis(db);
        var yanit = await s.Talep(dto, k.Islem);
        var json = JsonDocument.Parse(JsonSerializer.Serialize(yanit)).RootElement;
        var id = json.GetProperty("talepId").GetString()!;
        var parola = json.GetProperty("parola").GetString()!;
        var talep = await db.Set<DisaAktarimTalebi>().SingleAsync(t => t.Id == id);
        return (talep, parola);
    }

    // ── ŞART 1: gerekçe ─────────────────────────────────────────────────────

    [Fact]
    public async Task SART_1_Gerekcesiz_aktarim_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gerekçesiz");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).Talep(new DisaAktarimTalepDto { Gerekce = "lazım" }, k.Islem));

        Assert.Contains("gerekçesi", hata.Message);
    }

    [Fact]
    public async Task SART_1_Gerekce_denetim_izine_AYNEN_yazilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Gerekçe izi");
        using var db = vt.Olustur(k.TenantId);

        await Servis(db).Talep(Dto(), k.Islem);

        var iz = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.VarlikTuru == DisaAktarimServisi.DenetimVarligi)
            .OrderByDescending(d => d.Tarih).FirstAsync();

        Assert.Contains("Mali müşavire yıl sonu devir dosyası hazırlanıyor", iz.Detay);
        Assert.True(iz.Hassas);   // toplu kişisel veri çıkışı
    }

    // ── ŞART 2: varsayılan maskeli ──────────────────────────────────────────

    [Fact]
    public async Task SART_2_Varsayilan_MASKELIDIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Varsayılan maske");
        using var db = vt.Olustur(k.TenantId);

        var (talep, _) = await TalepKur(db, k, Dto());

        Assert.True(talep.Maskeli);
    }

    [Fact]
    public async Task SART_2_Tam_kimlik_YANLIS_onay_metniyle_alinamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Yanlış onay");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).Talep(Dto(onay: "evet tamam"), k.Islem));

        Assert.Contains("birebir", hata.Message);
    }

    [Fact]
    public async Task SART_2_Dogru_onay_metniyle_tam_kimlik_alinir_ve_IZE_ISLENIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Doğru onay");
        using var db = vt.Olustur(k.TenantId);

        var (talep, _) = await TalepKur(db, k,
            Dto(onay: "TAM KİMLİK BİLGİSİ DIŞA AKTARILACAKTIR"));

        Assert.False(talep.Maskeli);

        var iz = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.VarlikTuru == DisaAktarimServisi.DenetimVarligi)
            .OrderByDescending(d => d.Tarih).FirstAsync();
        Assert.Contains("TAM KİMLİK", iz.Detay);
    }

    [Fact]
    public async Task SART_2_Maskeli_dosyada_TCKN_duz_metin_GECMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Maskeli TCKN");
        using var db = vt.Olustur(k.TenantId);
        PusulaYaz(db, k, "10000000146");

        var s = Servis(db);
        var (talep, parola) = await TalepKur(db, k, Dto("gider-pusulasi"));
        var dosyalar = await Ac(s, talep, parola);

        var csv = dosyalar["gider-pusulasi.csv"];
        Assert.DoesNotContain("10000000146", csv);
        Assert.Contains("*******0146", csv);
    }

    [Fact]
    public async Task SART_2_Tam_kimlik_modunda_TCKN_COZULMUS_halde_gelir()
    {
        // Şifreli sütun EF converter'ı ile çözülür; dosyada gerçek TCKN olmalı.
        // Bu, maskesiz modun gerçekten çalıştığının kanıtıdır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tam TCKN");
        using var db = vt.Olustur(k.TenantId);
        PusulaYaz(db, k, "10000000146");

        var s = Servis(db);
        var (talep, parola) = await TalepKur(db, k,
            Dto("gider-pusulasi", "TAM KİMLİK BİLGİSİ DIŞA AKTARILACAKTIR"));
        var dosyalar = await Ac(s, talep, parola);

        Assert.Contains("10000000146", dosyalar["gider-pusulasi.csv"]);
    }

    // ── ŞART 5: şifreleme ───────────────────────────────────────────────────

    [Fact]
    public async Task SART_5_Arsiv_PAROLASIZ_acilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Parolasız");
        using var db = vt.Olustur(k.TenantId);

        var s = Servis(db);
        var (talep, parola) = await TalepKur(db, k, Dto("urun"));

        using var bellek = new MemoryStream();
        await s.Yaz(bellek, talep, parola);
        bellek.Position = 0;

        using var zip = new ZipFile(bellek);   // parola VERİLMEDİ
        var giris = zip.Cast<ZipEntry>().First(g => g.IsFile);
        Assert.True(giris.IsCrypted);
        Assert.Throws<ZipException>(() => zip.GetInputStream(giris));
    }

    [Fact]
    public async Task SART_5_Sifreleme_AES_256_dir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "AES 256");
        using var db = vt.Olustur(k.TenantId);

        var s = Servis(db);
        var (talep, parola) = await TalepKur(db, k, Dto("urun"));

        using var bellek = new MemoryStream();
        await s.Yaz(bellek, talep, parola);
        bellek.Position = 0;

        using var zip = new ZipFile(bellek) { Password = parola };
        // ZipCrypto (eski, kırılabilir) DEĞİL — AES-256 (WinZip AE-1).
        Assert.All(zip.Cast<ZipEntry>().Where(g => g.IsFile),
            g => Assert.Equal(256, g.AESKeySize));
    }

    [Fact]
    public async Task SART_5_Parola_veritabaninda_DUZ_METIN_tutulmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Parola saklanmaz");
        using var db = vt.Olustur(k.TenantId);

        var (talep, parola) = await TalepKur(db, k, Dto("urun"));

        Assert.DoesNotContain(parola, talep.ParolaHash);
        Assert.Equal(64, talep.ParolaHash.Length);   // SHA-256 hex
    }

    [Fact]
    public async Task SART_5_Jeton_da_veritabaninda_DUZ_METIN_tutulmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Jeton saklanmaz");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var yanit = await s.Talep(Dto("urun"), k.Islem);
        var json = JsonDocument.Parse(JsonSerializer.Serialize(yanit)).RootElement;
        var jeton = json.GetProperty("jeton").GetString()!;
        var id = json.GetProperty("talepId").GetString()!;

        var talep = await db.Set<DisaAktarimTalebi>().AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.DoesNotContain(jeton, talep.JetonHash);
    }

    // ── ŞART 6: tek kullanım + kısa ömür ────────────────────────────────────

    [Fact]
    public async Task SART_6_Jeton_IKINCI_kez_kullanilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Tek kullanım");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var yanit = await s.Talep(Dto("urun"), k.Islem);
        var json = JsonDocument.Parse(JsonSerializer.Serialize(yanit)).RootElement;
        var jeton = json.GetProperty("jeton").GetString()!;
        var parola = json.GetProperty("parola").GetString()!;

        await s.JetonuTuket(jeton, parola, "203.0.113.9");   // 1. kullanım: geçer

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => s.JetonuTuket(jeton, parola, "203.0.113.9"));
        Assert.Contains("daha önce kullanıldı", hata.Message);
    }

    [Fact]
    public async Task SART_6_Suresi_dolan_jeton_calismaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Süre doldu");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var yanit = await s.Talep(Dto("urun"), k.Islem);
        var json = JsonDocument.Parse(JsonSerializer.Serialize(yanit)).RootElement;
        var jeton = json.GetProperty("jeton").GetString()!;
        var parola = json.GetProperty("parola").GetString()!;
        var id = json.GetProperty("talepId").GetString()!;

        var talep = await db.Set<DisaAktarimTalebi>().SingleAsync(t => t.Id == id);
        talep.SonKullanma = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => s.JetonuTuket(jeton, parola, null));
        Assert.Contains("süresi doldu", hata.Message);
    }

    [Fact]
    public async Task SART_6_Yanlis_parola_jetonu_TUKETMEZ()
    {
        // Yanlış parola jetonu yakarsa, parolayı yanlış yazan kullanıcı
        // baştan talep açmak zorunda kalır — ve bu bir DoS yüzeyi olurdu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Yanlış parola");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var yanit = await s.Talep(Dto("urun"), k.Islem);
        var json = JsonDocument.Parse(JsonSerializer.Serialize(yanit)).RootElement;
        var jeton = json.GetProperty("jeton").GetString()!;
        var parola = json.GetProperty("parola").GetString()!;

        await Assert.ThrowsAsync<BadRequestException>(
            () => s.JetonuTuket(jeton, "yanlisparola", null));

        // Doğru parolayla hâlâ çalışmalı.
        var talep = await s.JetonuTuket(jeton, parola, null);
        Assert.NotNull(talep);
    }

    // ── KİRACI SINIRI — Bertan'ın uyarısı ───────────────────────────────────

    [Fact]
    public async Task Kiraci_A_export_dosyasinda_Kiraci_B_verisi_BULUNMAZ()
    {
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "Export A");
        var b = TestKurulum.KiraciKur(sistem, "Export B");

        // B'ye ayırt edici bir cari ekle.
        using (var dbB = vt.Olustur(b.TenantId))
        {
            dbB.Cariler.Add(new Cari
            {
                Id = Kimlik.Ulid(), TenantId = b.TenantId, Tur = "MUSTERI",
                Ad = "BASKA-KIRACI-MUSTERISI", VergiNo = "9999999999",
                Aktif = true, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            await dbB.SaveChangesAsync();
        }

        using var dbA = vt.Olustur(a.TenantId);
        var s = Servis(dbA);
        var (talep, parola) = await TalepKur(dbA, a, Dto("cari"));
        var dosyalar = await Ac(s, talep, parola);

        var csv = dosyalar["cari.csv"];
        Assert.DoesNotContain("BASKA-KIRACI-MUSTERISI", csv);
        Assert.DoesNotContain(b.TenantId, csv);
        Assert.Contains("Export A müşterisi", csv);
    }

    [Fact]
    public async Task Baska_kiracinin_jetonu_kendi_verisini_getirmez()
    {
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "Jeton A");
        var b = TestKurulum.KiraciKur(sistem, "Jeton B");

        string jeton, parola;
        using (var dbA = vt.Olustur(a.TenantId))
        {
            var yanit = await Servis(dbA).Talep(Dto("cari"), a.Islem);
            var json = JsonDocument.Parse(JsonSerializer.Serialize(yanit)).RootElement;
            jeton = json.GetProperty("jeton").GetString()!;
            parola = json.GetProperty("parola").GetString()!;
        }

        // B'nin bağlamında A'nın jetonu görünmez (global kiracı filtresi).
        using var dbB = vt.Olustur(b.TenantId);
        await Assert.ThrowsAsync<NotFoundException>(
            () => Servis(dbB).JetonuTuket(jeton, parola, null));
    }

    // ── İçerik ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Arsivde_her_kaynak_icin_bir_CSV_ve_OKUBENI_bulunur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Arşiv içeriği");
        using var db = vt.Olustur(k.TenantId);

        var s = Servis(db);
        var (talep, parola) = await TalepKur(db, k, new DisaAktarimTalepDto
        {
            Gerekce = "Tam yedek alınıyor, muhasebe devri için",
        });
        var dosyalar = await Ac(s, talep, parola);

        foreach (var ad in Kaynaklar.Adlar)
            Assert.True(dosyalar.ContainsKey($"{ad}.csv"), $"{ad}.csv arşivde yok");

        Assert.Contains("OKUBENI.txt", dosyalar.Keys);
        Assert.Contains("Gerekçe", dosyalar["OKUBENI.txt"]);
    }

    [Fact]
    public async Task Urun_dosyasi_TAS_KOKENINI_tasir()
    {
        // Adım 3'ün yasal alanı dışa aktarımda da bulunmalı — yoksa müşteri
        // veriyi başka bir programa taşıdığında yönetmelik bilgisi kaybolur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Export taş kökeni");
        using var db = vt.Olustur(k.TenantId);

        var urun = await db.Urunler.SingleAsync(u => u.Id == k.UrunId);
        urun.TasAgirlik = 0.5m;
        urun.TasKokeni = "SENTETIK";
        urun.TasIbaresi = "sentetik";
        await db.SaveChangesAsync();

        var s = Servis(db);
        var (talep, parola) = await TalepKur(db, k, Dto("urun"));
        var csv = (await Ac(s, talep, parola))["urun.csv"];

        Assert.Contains("tasKokeni", csv);
        Assert.Contains("SENTETIK", csv);
    }

    [Fact]
    public async Task Bilinmeyen_kaynak_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Bilinmeyen kaynak");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).Talep(Dto("kullanicilar"), k.Islem));

        Assert.Contains("Bilinmeyen dışa aktarım kaynağı", hata.Message);
    }

    [Fact]
    public async Task Kullanici_ve_parola_tablolari_kaynak_listesinde_DEGILDIR()
    {
        // Dışa aktarım bir veri taşınabilirliği aracıdır, veritabanı dökümü değil.
        // Kullanıcı/şifre/refresh token/denetim izi aktarılmaz (Batuhan'ın notu:
        // "tam SQL yedek mimari değil OPERASYON işidir").
        await Task.CompletedTask;
        Assert.DoesNotContain("kullanici", Kaynaklar.Adlar);
        Assert.DoesNotContain("denetim", Kaynaklar.Adlar);
        Assert.DoesNotContain("token", Kaynaklar.Adlar);
    }

    private static void PusulaYaz(KuyumcuDbContext db, Kiraci k, string tckn)
    {
        var now = DateTime.UtcNow;
        var hareketId = Kimlik.Ulid();
        db.Hareketler.Add(new Hareket
        {
            Id = hareketId, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "HURDA_ALIS",
            Tarih = now, TlTutari = -5_000m, HasGrami = 1m, ParaBirimi = "TRY",
            OdemeTuru = "NAKIT", KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId,
            Olusturuldu = now,
        });
        db.GiderPusulalari.Add(new GiderPusulasi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, PusulaNo = $"GP-2026-{Random.Shared.Next(100000, 999999)}",
            HareketId = hareketId, SubeId = k.SubeId,
            SaticiTipi = SaticiTipi.NihaiTuketici, SaticiAdSoyad = "Hurda Satıcısı",
            SaticiTckn = tckn, SaticiTcknHash = TestVeritabani.TestSifreleyici.Hashle(tckn),
            SifreSurumu = TestVeritabani.TestSifreleyici.Surum, SaticiAdres = "Ulus, Ankara",
            AyarId = "K22", GramAgirlik = 2m, HasGrami = 1m,
            BrutTutar = 5_000m, StopajOran = 0m, StopajTutar = 0m, NetOdenen = 5_000m,
            TeslimTarihi = now, DuzenlemeSonTarihi = now.AddDays(7), Olusturuldu = now,
        });
        db.SaveChanges();
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HTTP — yetki, oran sınırı, gerçek indirme
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class DisaAktarimHttpTestleri(HttpFikstur f)
{
    private static object Govde(string? onay = null) => new
    {
        gerekce = "Mali müşavire yıl sonu devir dosyası hazırlanıyor",
        kapsam = new[] { "urun" },
        tamKimlikOnayi = onay,
    };

    [Fact]
    public async Task Token_yoksa_401()
    {
        var c = f.Ortam.AnonimIstemci();
        var r = await c.PostAsJsonAsync("/api/v1/disa-aktarim", Govde());
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task SART_1_MUDUR_dahi_disa_aktaramaz_yalniz_SAHIP()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Rol kapısı");

        var mudur = f.Ortam.Istemci(k, "MUDUR");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await mudur.PostAsJsonAsync("/api/v1/disa-aktarim", Govde())).StatusCode);

        var muhasebe = f.Ortam.Istemci(k, "MUHASEBE");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await muhasebe.PostAsJsonAsync("/api/v1/disa-aktarim", Govde())).StatusCode);

        var sahip = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK,
            (await sahip.PostAsJsonAsync("/api/v1/disa-aktarim", Govde())).StatusCode);
    }

    [Fact]
    public async Task Gecersiz_govde_400_doner()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Geçersiz gövde");
        var c = f.Ortam.Istemci(k);

        var r = await c.PostAsJsonAsync("/api/v1/disa-aktarim", new { kapsam = new[] { "urun" } });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Ucdan_uca_indirme_GERCEK_sifreli_zip_doner()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Uçtan uca");
        var c = f.Ortam.Istemci(k);

        var talep = await (await c.PostAsJsonAsync("/api/v1/disa-aktarim", Govde())).Json();
        var jeton = talep.GetProperty("jeton").GetString()!;
        var parola = talep.GetProperty("parola").GetString()!;

        var r = await c.PostAsJsonAsync($"/api/v1/disa-aktarim/{jeton}/indir", new { parola });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/zip", r.Content.Headers.ContentType?.MediaType);
        Assert.Contains("attachment", r.Content.Headers.ContentDisposition?.ToString());
        Assert.Equal("no-store", r.Headers.CacheControl?.ToString());

        var bayt = await r.Content.ReadAsByteArrayAsync();
        Assert.True(bayt.Length > 0);

        using var zip = new ZipFile(new MemoryStream(bayt)) { Password = parola };
        var urun = zip.Cast<ZipEntry>().Single(g => g.Name == "urun.csv");
        using var oku = new StreamReader(zip.GetInputStream(urun), Encoding.UTF8);
        var csv = await oku.ReadToEndAsync();
        Assert.Contains("Test bilezik", csv);
    }

    [Fact]
    public async Task SART_6_Ayni_baglanti_ikinci_indirmede_400()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "İkinci indirme");
        var c = f.Ortam.Istemci(k);

        var talep = await (await c.PostAsJsonAsync("/api/v1/disa-aktarim", Govde())).Json();
        var jeton = talep.GetProperty("jeton").GetString()!;
        var parola = talep.GetProperty("parola").GetString()!;

        Assert.Equal(HttpStatusCode.OK,
            (await c.PostAsJsonAsync($"/api/v1/disa-aktarim/{jeton}/indir", new { parola })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PostAsJsonAsync($"/api/v1/disa-aktarim/{jeton}/indir", new { parola })).StatusCode);
    }

    [Fact]
    public async Task SART_3_Indirme_denetim_izine_satir_sayisiyla_yazilir()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "İz satır sayısı");
        var c = f.Ortam.Istemci(k);

        var talep = await (await c.PostAsJsonAsync("/api/v1/disa-aktarim", Govde())).Json();
        var jeton = talep.GetProperty("jeton").GetString()!;
        var parola = talep.GetProperty("parola").GetString()!;
        await c.PostAsJsonAsync($"/api/v1/disa-aktarim/{jeton}/indir", new { parola });

        var gecmis = await (await c.GetAsync("/api/v1/disa-aktarim/gecmis")).Json();
        var kayit = gecmis.GetProperty("kayitlar")[0];

        Assert.Equal(1, kayit.GetProperty("satirSayisi").GetInt32());   // 1 ürün
        Assert.False(kayit.GetProperty("kullanildi").ValueKind == JsonValueKind.Null);
        Assert.Contains("Mali müşavire", kayit.GetProperty("gerekce").GetString());
    }

    [Fact]
    public async Task Baska_kiracinin_jetonuyla_indirme_404()
    {
        var sistem = f.Vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "HTTP jeton A");
        var b = TestKurulum.KiraciKur(sistem, "HTTP jeton B");

        var ca = f.Ortam.Istemci(a);
        var talep = await (await ca.PostAsJsonAsync("/api/v1/disa-aktarim", Govde())).Json();
        var jeton = talep.GetProperty("jeton").GetString()!;
        var parola = talep.GetProperty("parola").GetString()!;

        var cb = f.Ortam.Istemci(b);
        var r = await cb.PostAsJsonAsync($"/api/v1/disa-aktarim/{jeton}/indir", new { parola });
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Kaynaklar_ucu_alan_listesini_verir()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Kaynak listesi");
        var c = f.Ortam.Istemci(k);

        var r = await (await c.GetAsync("/api/v1/disa-aktarim/kaynaklar")).Json();

        // 6 → 9: paket 31, ADR-0014 kapsamına pos-cihazlari · pos-tahsilatlari
        // · pos-eslesmeleri kaynaklarını ekledi (kişisel veri YOK, maskeleme gerekmez).
        // 9 → 12: PAKET P-26 (Y-34/Y-35/Y-37) satinalma · irsaliye · muhasebe-fisi.
        Assert.Equal(12, r.GetArrayLength());
        var cari = r.EnumerateArray().Single(x => x.GetProperty("ad").GetString() == "cari");
        Assert.True(cari.GetProperty("kisiselVeri").GetBoolean());
    }

    [Fact]
    public async Task SART_4_Oran_siniri_asilinca_429()
    {
        // Politika: saat başına 10 istek (talep + indirme ortak kota). 11.'si reddedilmeli.
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Oran sınırı");
        var c = f.Ortam.Istemci(k);

        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.OK,
                (await c.PostAsJsonAsync("/api/v1/disa-aktarim", Govde())).StatusCode);

        var r = await c.PostAsJsonAsync("/api/v1/disa-aktarim", Govde());
        Assert.Equal(HttpStatusCode.TooManyRequests, r.StatusCode);
    }
}
