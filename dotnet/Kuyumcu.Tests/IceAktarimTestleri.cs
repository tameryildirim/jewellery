using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Iceaktarim;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// VERİ İÇE AKTARIMI — paket 47 Faz A [DB]
//
// Bu paketin riski SIZDIRMA değil ZEHİRLEME'dir. Her testin adı hangi kapıyı
// sınadığını söyler; kapı gevşetilirse ilgili test kırmızıya döner.
//   • Kuru prova hiçbir şey yazmaz          → PROVA_hicbir_kayit_YAZMAZ…
//   • Taş kökeni kapısı yutulmaz            → Tas_agirligi_var_kokeni_yok…
//   • Kendi Add'i yok, servis yolu kullanır → Uygulanan_cari_SERVIS_yolundan…
//   • Tek transaction, kısmi göç yok        → Bir_satir_patlarsa…
//   • Sahne tablosu diskte şifreli          → Ham_satir_DISKTE_sifreli…
//   • İki katmanlı kiracı koruması          → Baska_kiracinin_oturumu…
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class IceAktarimTestleri(TestVeritabani vt)
{
    private sealed class SahteErisim : Microsoft.AspNetCore.Http.IHttpContextAccessor
    {
        public Microsoft.AspNetCore.Http.HttpContext? HttpContext { get; set; }
    }

    private static IceAktarimServisi Servis(KuyumcuDbContext db, IceAktarimAyarlari? ayar = null)
    {
        var denetim = new DenetimServisi(db, new SahteErisim());
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(
            db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance, denetim);
        var urun = new Kuyumcu.Api.Modules.Urun.UrunServisi(db, denetim);
        return new IceAktarimServisi(db, denetim, cari, urun, ayar);
    }

    private const string Gerekce = "Eski programdan müşteri kartları taşınıyor";

    private static string B64(string csv) => Convert.ToBase64String(Encoding.UTF8.GetBytes(csv));

    private static IceAktarimProvaDto Dto(
        string kaynak, string csv, string? politika = null, string gerekce = Gerekce) => new()
    {
        Kaynak = kaynak,
        DosyaAdi = $"{kaynak}.csv",
        IcerikBase64 = B64(csv),
        Gerekce = gerekce,
        YinelenenPolitikasi = politika,
    };

    private static string Alan(object cevap, params string[] yol)
    {
        var oge = JsonDocument.Parse(JsonSerializer.Serialize(cevap)).RootElement;
        foreach (var a in yol) oge = oge.GetProperty(a);
        return oge.ToString();
    }

    private const string UcCari = """
        tur,ad,telefon,vergiNo,adres
        MUSTERI,Ayşe Yılmaz,05321112233,1234567890,"Şişli, İstanbul"
        MUSTERI,Ali Demir,05321112244,,Ankara
        TEDARIKCI,Mehmet Toptan,05321112255,9876543210,İzmir
        """;

    // ── Kuru prova ───────────────────────────────────────────────────────────

    [Fact]
    public async Task PROVA_hicbir_kayit_YAZMAZ_cari_sayisi_degismez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST prova");
        using var db = vt.Olustur(k.TenantId);

        var once = await db.Cariler.CountAsync(c => c.TenantId == k.TenantId);
        var cevap = await Servis(db).Prova(Dto("cari", UcCari), k.Islem);
        var sonra = await db.Cariler.CountAsync(c => c.TenantId == k.TenantId);

        Assert.Equal(once, sonra);                       // ⛔ TEK GERÇEK KANIT
        Assert.Equal("3", Alan(cevap, "gecerliSatir"));
        Assert.Equal("0", Alan(cevap, "yazilanKayit"));
        Assert.Equal(IceAktarimDurumu.Prova, Alan(cevap, "durum"));

        // Sahne tablosu doldu, hedef tablo boş kaldı.
        var oturumId = Alan(cevap, "oturumId");
        Assert.Equal(3, await db.Set<IceAktarimSatiri>().CountAsync(s => s.OturumId == oturumId));
    }

    [Fact]
    public async Task Tas_agirligi_var_kokeni_yok_satir_HATA_ADR0013()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST taş");
        using var db = vt.Olustur(k.TenantId);

        var csv = """
            tip,kod,ad,ayarId,gramAgirlik,tasAgirlik,tasKokeni
            ZIYNET,P47-T1,Taşsız bilezik,K22,10,0,
            ZIYNET,P47-T2,Taşlı yüzük,K22,5,1.5,
            """;

        var cevap = await Servis(db).Prova(Dto("urun", csv), k.Islem);

        Assert.Equal("1", Alan(cevap, "gecerliSatir"));
        Assert.Equal("1", Alan(cevap, "hataliSatir"));

        var oturumId = Alan(cevap, "oturumId");
        var hatali = await db.Set<IceAktarimSatiri>()
            .SingleAsync(s => s.OturumId == oturumId && s.Sonuc == IceAktarimSatirSonucu.Hata);
        Assert.Equal(3, hatali.SatirNo);                 // başlık 1, ilk veri 2
        Assert.Equal("TAS_KOKENI", hatali.HataKodu);
        Assert.Contains("md.11", hatali.HataMesaji);
    }

    [Fact]
    public async Task Gecersiz_ayar_kodu_satiri_HATA_diger_satirlar_GECERLI()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST ayar");
        using var db = vt.Olustur(k.TenantId);

        var csv = """
            tip,kod,ad,ayarId,gramAgirlik
            ZIYNET,P47-A1,Bilezik,K22,10
            ZIYNET,P47-A2,Uydurma ayar,K99,10
            ZIYNET,P47-A3,Kolye,K14,8
            """;

        var cevap = await Servis(db).Prova(Dto("urun", csv), k.Islem);

        Assert.Equal("2", Alan(cevap, "gecerliSatir"));
        Assert.Equal("1", Alan(cevap, "hataliSatir"));

        var oturumId = Alan(cevap, "oturumId");
        var hatali = await db.Set<IceAktarimSatiri>()
            .SingleAsync(s => s.OturumId == oturumId && s.Sonuc == IceAktarimSatirSonucu.Hata);
        Assert.Equal(3, hatali.SatirNo);
        Assert.Equal("ALAN_GECERSIZ", hatali.HataKodu);
    }

    [Fact]
    public async Task Yinelenen_kod_politikasi_HATA_ise_satir_reddedilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST yinelenen");
        using var db = vt.Olustur(k.TenantId);

        var mevcutKod = await db.Urunler.Where(u => u.TenantId == k.TenantId)
            .Select(u => u.Kod).FirstAsync();

        var csv = $"""
            tip,kod,ad,ayarId,gramAgirlik
            ZIYNET,{mevcutKod},Zaten var,K22,10
            ZIYNET,P47-Y1,Yeni ürün,K22,10
            """;

        var hataPolitikasi = await Servis(db).Prova(Dto("urun", csv, YinelenenPolitikasi.Hata), k.Islem);
        Assert.Equal("1", Alan(hataPolitikasi, "hataliSatir"));
        Assert.Equal("1", Alan(hataPolitikasi, "gecerliSatir"));

        var atlaPolitikasi = await Servis(db).Prova(Dto("urun", csv, YinelenenPolitikasi.Atla), k.Islem);
        Assert.Equal("0", Alan(atlaPolitikasi, "hataliSatir"));
        Assert.Equal("1", Alan(atlaPolitikasi, "atlananSatir"));
        Assert.Equal("1", Alan(atlaPolitikasi, "gecerliSatir"));
    }

    [Fact]
    public async Task GUNCELLE_politikasi_FAZ_A_da_KABUL_EDILMEZ()
    {
        // Bertan + Mehmet ortak koşulu: üzerine yazma geri almayı imkânsızlaştırır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST guncelle");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).Prova(Dto("cari", UcCari, "GUNCELLE"), k.Islem));

        Assert.Contains("GUNCELLE", hata.Message);
    }

    [Fact]
    public async Task Gerekcesiz_oturum_ACILMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST gerekçe");
        using var db = vt.Olustur(k.TenantId);

        await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).Prova(Dto("cari", UcCari, gerekce: "lazım"), k.Islem));

        Assert.Equal(0, await db.Set<IceAktarimOturumu>().CountAsync(o => o.TenantId == k.TenantId));
    }

    // ── Uygulama ─────────────────────────────────────────────────────────────

    private static IceAktarimUygulaDto Onay(IceAktarimServisi s) =>
        new() { Onay = s.Ayarlar.UygulaOnayMetni };

    [Fact]
    public async Task Uygulanan_cari_SERVIS_yolundan_gecer_cari_bakiye_satiri_dogar()
    {
        // ⚠ FAIL-CLOSED 3 KANITI: `db.Cariler.Add` ile yazılsaydı cari_bakiyeler
        //   satırı HİÇ doğmazdı (o satırı CariServisi.Olustur açar) ve cari
        //   ekstresi ilk bakışta "bakiyesi yok" derdi.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST servis yolu");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var prova = await s.Prova(Dto("cari", UcCari), k.Islem);
        var oturumId = Alan(prova, "oturumId");

        var sonuc = await s.Uygula(oturumId, Onay(s), k.Islem);
        Assert.Equal("3", Alan(sonuc, "uygulananSatir"));

        var yeniler = await db.Cariler.AsNoTracking()
            .Where(c => c.TenantId == k.TenantId && c.Ad == "Ayşe Yılmaz").ToListAsync();
        var yeni = Assert.Single(yeniler);

        Assert.True(await db.CariBakiyeler.AnyAsync(b => b.CariId == yeni.Id));
        Assert.True(yeni.Aktif);
        Assert.Equal("05321112233", yeni.Telefon);       // maskeli DEĞİL, gerçek değer
        Assert.Equal("1234567890", yeni.VergiNo);
    }

    [Fact]
    public async Task Cari_uygulamasi_DIS_transaction_icinde_calisir_ic_ice_HATA_YOK()
    {
        // Aşkın'ın koşulu: CariServisi.Olustur kendi transaction'ını açıyor.
        // Dış transaction ile iç içe davranış MySQL'de doğrulanır.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST iç içe");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var prova = await s.Prova(Dto("cari", UcCari), k.Islem);
        var oturumId = Alan(prova, "oturumId");

        var sonuc = await s.Uygula(oturumId, Onay(s), k.Islem);

        Assert.Equal("3", Alan(sonuc, "uygulananSatir"));
        Assert.Equal(IceAktarimDurumu.Uygulandi, Alan(sonuc, "durum"));
        Assert.Null(db.Database.CurrentTransaction);     // dış transaction kapandı
    }

    [Fact]
    public async Task Bir_satir_patlarsa_TUM_oturum_geri_alinir_kismi_goc_YOK()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST kısmi");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var csv = """
            tip,kod,ad,ayarId,gramAgirlik
            ZIYNET,P47-K1,Birinci,K22,10
            ZIYNET,P47-K2,İkinci,K22,10
            ZIYNET,P47-K3,Üçüncü,K22,10
            """;

        var prova = await s.Prova(Dto("urun", csv), k.Islem);
        var oturumId = Alan(prova, "oturumId");
        Assert.Equal("3", Alan(prova, "gecerliSatir"));

        // ⚠ GERÇEK BİR İSTİSNA: prova ile uygulama arasında 3. satırın kodu
        //   başka bir yoldan yazılır → UrunServisi.Olustur ConflictException atar.
        using (var yan = vt.Olustur(k.TenantId))
        {
            var yanServis = Servis(yan);
            var yanProva = await yanServis.Prova(Dto("urun", """
                tip,kod,ad,ayarId,gramAgirlik
                ZIYNET,P47-K3,Araya giren,K22,10
                """), k.Islem);
            await yanServis.Uygula(Alan(yanProva, "oturumId"), Onay(yanServis), k.Islem);
        }

        var oncekiSayi = await db.Urunler.CountAsync(u => u.TenantId == k.TenantId);

        // P-05 (Y-2): kod çakışması artık ALAN ALTI 409 (AlanCakismasiException);
        // davranış AYNI — içe aktarım oturumu bütünüyle geri alınır.
        var kodHatasi = await Assert.ThrowsAsync<AlanCakismasiException>(
            () => s.Uygula(oturumId, Onay(s), k.Islem));
        Assert.Equal("kod", kodHatasi.Alan);
        Assert.Equal(409, kodHatasi.StatusCode);

        using var dogrula = vt.Olustur(k.TenantId);
        // KISMİ GÖÇ YOK: P47-K1 ve P47-K2 de yazılmadı.
        Assert.Equal(oncekiSayi, await dogrula.Urunler.CountAsync(u => u.TenantId == k.TenantId));
        Assert.False(await dogrula.Urunler.AnyAsync(u => u.TenantId == k.TenantId && u.Kod == "P47-K1"));

        var oturum = await dogrula.Set<IceAktarimOturumu>().SingleAsync(o => o.Id == oturumId);
        Assert.Equal(IceAktarimDurumu.Iptal, oturum.Durum);
    }

    [Fact]
    public async Task Uygulanan_oturum_IKINCI_kez_uygulanamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST iki kez");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var prova = await s.Prova(Dto("cari", UcCari), k.Islem);
        var oturumId = Alan(prova, "oturumId");
        await s.Uygula(oturumId, Onay(s), k.Islem);

        await Assert.ThrowsAsync<ConflictException>(() => s.Uygula(oturumId, Onay(s), k.Islem));
        Assert.Equal(3, await db.Cariler.CountAsync(c => c.TenantId == k.TenantId && c.Ad != $"PAKET47-TEST iki kez müşterisi"));
    }

    // ── Kişisel veri ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Ham_satir_DISKTE_sifreli_saklanir_ADR0003()
    {
        // Mehmet'in koşulu: sahne tablosunda telefon/vergi no DÜZ METİN duramaz.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST şifre");
        using var db = vt.Olustur(k.TenantId);

        var cevap = await Servis(db).Prova(Dto("cari", UcCari), k.Islem);
        var oturumId = Alan(cevap, "oturumId");

        // Ham SQL: dönüştürücü devre dışı — diskteki gerçek değer okunur.
        var diskteki = await db.Database
            .SqlQuery<string>($"SELECT ham_json AS Value FROM ice_aktarim_satirlari WHERE oturum_id = {oturumId} LIMIT 1")
            .SingleAsync();

        Assert.DoesNotContain("05321112233", diskteki);
        Assert.DoesNotContain("1234567890", diskteki);
        Assert.StartsWith("v", diskteki);                // "v{sürüm}:base64" biçimi
    }

    [Fact]
    public async Task Satir_listesinde_kimlik_alanlari_MASKELI_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST maske");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var cevap = await s.Prova(Dto("cari", UcCari), k.Islem);
        var oturumId = Alan(cevap, "oturumId");

        var liste = JsonSerializer.Serialize(await s.Satirlar(oturumId, null, 50, 0, k.Islem));

        Assert.DoesNotContain("05321112233", liste);
        Assert.Contains("2233", liste);                  // son 4 hane görünür
        Assert.Contains("MASKEL", liste);                // adres tamamen gizli
    }

    // ── Geri alma ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Geri_al_yalniz_bu_oturumun_kartlarini_deaktif_eder()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST geri al");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var prova = await s.Prova(Dto("cari", UcCari), k.Islem);
        var oturumId = Alan(prova, "oturumId");
        await s.Uygula(oturumId, Onay(s), k.Islem);

        var sonuc = await s.GeriAl(oturumId, k.Islem);
        Assert.Equal("3", Alan(sonuc, "pasifeAlinan"));

        using var dogrula = vt.Olustur(k.TenantId);
        // Oturumun kartları PASİF; SİLİNMEDİ.
        Assert.Equal(3, await dogrula.Cariler.CountAsync(c => c.TenantId == k.TenantId && !c.Aktif));
        // Fikstürün kendi carisi DOKUNULMADAN kaldı.
        Assert.True(await dogrula.Cariler.AsNoTracking().Where(c => c.Id == k.CariId).Select(c => c.Aktif).SingleAsync());
    }

    [Fact]
    public async Task Uygulanmamis_oturum_geri_alinamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST geri al kapı");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var prova = await s.Prova(Dto("cari", UcCari), k.Islem);
        await Assert.ThrowsAsync<ConflictException>(
            () => s.GeriAl(Alan(prova, "oturumId"), k.Islem));
    }

    // ── Düzenleme yolu (MÜHÜR 2 / DoD) ───────────────────────────────────────

    [Fact]
    public async Task PATCH_gonderilmeyen_alani_KORUR_uygulanan_oturum_DUZENLENEMEZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST patch");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var prova = await s.Prova(Dto("cari", UcCari, YinelenenPolitikasi.Atla), k.Islem);
        var oturumId = Alan(prova, "oturumId");

        // Yalnız gerekçe gönderilir → politika KORUNUR.
        await s.Guncelle(oturumId, new IceAktarimGuncelleDto
        {
            Gerekce = "Mali müşavir listesinden düzeltilmiş kart göçü",
        }, k.Islem);

        var oturum = await db.Set<IceAktarimOturumu>().SingleAsync(o => o.Id == oturumId);
        Assert.Equal("Mali müşavir listesinden düzeltilmiş kart göçü", oturum.Gerekce);
        Assert.Equal(YinelenenPolitikasi.Atla, oturum.YinelenenPolitikasi);

        await s.Uygula(oturumId, Onay(s), k.Islem);
        // Uygulanmış oturumun gerekçesi denetim izinin parçasıdır (ADR-0004).
        await Assert.ThrowsAsync<ConflictException>(() => s.Guncelle(oturumId, new IceAktarimGuncelleDto
        {
            Gerekce = "sonradan değiştirilen gerekçe metni buraya",
        }, k.Islem));
    }

    [Fact]
    public async Task Iptal_edilen_oturum_uygulanamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST iptal");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var prova = await s.Prova(Dto("cari", UcCari), k.Islem);
        var oturumId = Alan(prova, "oturumId");

        await s.Iptal(oturumId, k.Islem);
        await Assert.ThrowsAsync<ConflictException>(() => s.Uygula(oturumId, Onay(s), k.Islem));
    }

    // ── Denetim izi ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Her_adim_denetim_izine_GEREKCEYLE_yazilir_hassas()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST iz");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var prova = await s.Prova(Dto("cari", UcCari), k.Islem);
        await s.Uygula(Alan(prova, "oturumId"), Onay(s), k.Islem);

        var izler = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.VarlikTuru == IceAktarimServisi.DenetimVarligi)
            .OrderBy(d => d.Tarih).ToListAsync();

        Assert.True(izler.Count >= 2);
        Assert.All(izler, i => Assert.True(i.Hassas));
        Assert.Contains(izler, i => i.Detay!.Contains("kuru prova"));
        Assert.Contains(izler, i => i.Detay!.Contains("uygulandı"));
        Assert.All(izler, i => Assert.Contains(Gerekce, i.Detay!));
    }

    // ── Kiracı izolasyonu ────────────────────────────────────────────────────

    [Fact]
    public async Task Baska_kiracinin_oturumu_okunamaz_404()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST kiracı A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST kiracı B");

        string oturumId;
        using (var dbA = vt.Olustur(a.TenantId))
            oturumId = Alan(await Servis(dbA).Prova(Dto("cari", UcCari), a.Islem), "oturumId");

        using var dbB = vt.Olustur(b.TenantId);
        await Assert.ThrowsAsync<NotFoundException>(
            () => Servis(dbB).Satirlar(oturumId, null, 50, 0, b.Islem));
        await Assert.ThrowsAsync<NotFoundException>(
            () => Servis(dbB).GeriAl(oturumId, b.Islem));
    }

    // ── TB-19 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Satir_listesi_limitsiz_cagrilamaz_TB19()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST limit");
        using var db = vt.Olustur(k.TenantId);
        var s = Servis(db);

        var prova = await s.Prova(Dto("cari", UcCari), k.Islem);
        var oturumId = Alan(prova, "oturumId");

        // Tavan YAPILANDIRMADAN okunur; test rakam sabitlemez (Aşkın koşulu).
        var tavan = s.Ayarlar.SatirListesiTavan;
        await Assert.ThrowsAsync<BadRequestException>(
            () => s.Satirlar(oturumId, null, tavan + 1, 0, k.Islem));
        await Assert.ThrowsAsync<BadRequestException>(
            () => s.Satirlar(oturumId, null, 0, 0, k.Islem));

        var sonuc = await s.Satirlar(oturumId, null, tavan, 0, k.Islem);
        Assert.Equal("3", Alan(sonuc, "toplamSatir"));
    }

    // ── Dosya kapısı (tavanlar yapılandırmadan) ──────────────────────────────

    [Fact]
    public async Task Satir_tavani_asilinca_oturum_ACILMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST tavan");
        using var db = vt.Olustur(k.TenantId);
        var ayar = new IceAktarimAyarlari { MaksSatir = 2 };
        var s = Servis(db, ayar);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => s.Prova(Dto("cari", UcCari), k.Islem));        // 3 satır, tavan 2

        Assert.Contains($"{ayar.MaksSatir}", hata.Message);
        Assert.Equal(0, await db.Set<IceAktarimOturumu>().CountAsync(o => o.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Ikili_dosya_CSV_sanilmaz_ve_oturum_ACILMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST ikili");
        using var db = vt.Olustur(k.TenantId);

        // %PDF- imzası: kullanıcı "CSV UTF-8" yerine PDF seçmiş.
        var pdf = Convert.ToBase64String("%PDF-1.7 sahte"u8.ToArray());
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Prova(new IceAktarimProvaDto
        {
            Kaynak = "cari", DosyaAdi = "liste.pdf", IcerikBase64 = pdf, Gerekce = Gerekce,
        }, k.Islem));

        Assert.Contains("PDF", hata.Message);
        Assert.Equal(0, await db.Set<IceAktarimOturumu>().CountAsync(o => o.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Zorunlu_kolon_eksik_dosya_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST kolon");
        using var db = vt.Olustur(k.TenantId);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Servis(db).Prova(Dto("cari", "ad,telefon\nAyşe,05321112233\n"), k.Islem));

        Assert.Contains("tur", hata.Message);
    }

    [Fact]
    public async Task Gecmis_hareket_kaynagi_YOKTUR_seceneK3_VETO()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET47-TEST kapsam");
        using var db = vt.Olustur(k.TenantId);

        // Mehmet'in VETO'su: geçmiş satış/kasa/fatura içe aktarılamaz.
        // GOC-47B: açılış bakiyesi/stoku artık Faz B kaynağıdır (Goc47BTestleri) — listeden çıktı.
        foreach (var yasak in new[] { "satis", "kasa", "fatura", "hareket", "gecmis-satis" })
        {
            var hata = await Assert.ThrowsAsync<BadRequestException>(
                () => Servis(db).Prova(Dto(yasak, UcCari), k.Islem));
            Assert.Contains("Bilinmeyen içe aktarım kaynağı", hata.Message);
        }
    }
}
