using System.Net;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yetki;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-09 — KİMLİK, ERİŞİM, MFA VE YETKİ MATRİSİ
// Kimlikler: B-6 · B-20 · B-35 · B-36 · B-37 · Y-39 + Ramazan K3 dilimi.
//
// NEDEN GERÇEK HTTP: [RolGerekli] ve [ModulYetkisi] birer DENETLEYİCİ
// niteliğidir; servis testi onları GÖRMEZ. Ayrıca B-35'in konusu doğrudan
// DURUM KODUDUR (404 mü 401 mi) — durum kodu yalnız boru hattında doğar.
//
// ⚠ Bu dosya yalnız P-09 kapsamındaki kuralları sınar. MFA'nın kendi uç
//   davranışları MfaHttpTestleri/MfaTestleri/UX1C4MfaDevirTestleri'ndedir;
//   burada P-09 DoD'unun EK istedikleri ölçülür: kurtarma izinin DB'de SATIR
//   olması ve yarım kalan kurulumun /ayarlar'da kilit yaratmaması.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P09KimlikErisimTestleri(HttpFikstur fikstur)
{
    private const string Sifre = "Parola12345";

    /// <summary>SAHİP'in parolası bilinen bir kiracı kurar.</summary>
    private Kiraci Kur(string ad)
    {
        using var db = fikstur.Vt.OlusturSistem();
        var kiraci = TestKurulum.KiraciKur(db, $"P09-{ad}-{Guid.NewGuid():N}"[..24]);
        var sahip = db.Kullanicilar.Single(k => k.Id == kiraci.KullaniciId);
        sahip.SifreHash = BCrypt.Net.BCrypt.HashPassword(Sifre, 12);
        db.SaveChanges();
        return kiraci;
    }

    /// <summary>Kiracıya ikinci bir kullanıcı ekler ve kimliğini döner.</summary>
    private string KullaniciEkle(Kiraci kiraci, string rol, bool aktif = true)
    {
        using var db = fikstur.Vt.OlusturSistem();
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Kullanicilar.Add(new Kullanici
        {
            Id = id, TenantId = kiraci.TenantId, SubeId = kiraci.SubeId,
            Ad = "İkinci", Soyad = "Kişi", Email = $"{id}@test.local",
            SifreHash = BCrypt.Net.BCrypt.HashPassword(Sifre, 12),
            Rol = rol, Aktif = aktif, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private string RolOku(string kullaniciId)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return db.Kullanicilar.AsNoTracking().Single(k => k.Id == kullaniciId).Rol;
    }

    private int DenetimSay(string tenantId, string parca)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return db.DenetimKayitlari.AsNoTracking()
            .Count(d => d.TenantId == tenantId && d.Detay != null && d.Detay.Contains(parca));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B-35 — BAŞARISIZ GİRİŞTE KULLANICI VARLIĞI İFŞA EDİLMEZ
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Olmayan e-posta ile yanlış parola AYNI durum kodunu ve AYNI metni döner.
    /// Eskiden olmayan kullanıcı 404 "Kullanıcı bulunamadı" alıyordu; saldırgan
    /// tek istekle e-posta sayımı yapabiliyordu.
    /// </summary>
    [Fact]
    public async Task B35_Olmayan_eposta_ve_yanlis_sifre_AYNI_401_ve_AYNI_metni_doner()
    {
        var kiraci = Kur("b35");
        string eposta;
        using (var db = fikstur.Vt.OlusturSistem())
            eposta = db.Kullanicilar.AsNoTracking().Single(k => k.Id == kiraci.KullaniciId).Email;

        var istemci = fikstur.Ortam.AnonimIstemci();

        var yok = await istemci.PostJson("/api/v1/auth/giris",
            new { email = $"hic-yok-{Guid.NewGuid():N}@test.local", sifre = Sifre, tenantId = kiraci.TenantId });
        var yanlis = await istemci.PostJson("/api/v1/auth/giris",
            new { email = eposta, sifre = "BambaskaParola99", tenantId = kiraci.TenantId });

        Assert.Equal(HttpStatusCode.Unauthorized, yok.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, yanlis.StatusCode);

        var yokMesaj = (await yok.Json()).GetProperty("message").GetString();
        var yanlisMesaj = (await yanlis.Json()).GetProperty("message").GetString();
        Assert.Equal(yanlisMesaj, yokMesaj);
        Assert.Equal("E-posta veya şifre hatalı", yokMesaj);
        Assert.DoesNotContain("bulunamadı", yokMesaj!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Geçerli imzalı jeton + artık var olmayan kimlik: 404 DEĞİL generic 401.
    /// 404 "bu kimlik vardı ama artık yok" bilgisini sızdırıyordu.
    /// </summary>
    [Fact]
    public async Task B35_Ben_ucu_silinmis_kimlikte_404_DEGIL_401_doner()
    {
        var kiraci = Kur("b35ben");
        var yokId = Kimlik.Ulid();
        var istemci = fikstur.Ortam.Istemci(yokId, kiraci.TenantId, kiraci.SubeId, "SAHIP");

        var cevap = await istemci.GetAsync("/api/v1/auth/ben");

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        var mesaj = (await cevap.Json()).GetProperty("message").GetString();
        Assert.Equal("E-posta veya şifre hatalı", mesaj);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B-36 — TEK SAHİP ÇIKMAZI + DEVİR
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Son aktif SAHİP pasife ALINAMAZ ve mesaj doğru çıkış yolunu söyler.</summary>
    [Fact]
    public async Task B36_Son_SAHIP_pasife_alinamaz_ve_mesaj_DEVRI_soyler()
    {
        var kiraci = Kur("b36kilit");
        var mudurId = KullaniciEkle(kiraci, "MUDUR");
        // MÜDÜR, SAHİP'i pasife almayı denerse önce rol kapısına takılır; bu yüzden
        // kapıyı SAHİP'in KENDİ yetkisiyle değil, İKİNCİ bir SAHİP üzerinden ölçeriz:
        // tek aktif SAHİP olan kiracıda SAHİP'i pasife almak isteyen yine SAHİP'tir.
        var istemci = fikstur.Ortam.Istemci(mudurId, kiraci.TenantId, kiraci.SubeId, "SAHIP");

        var cevap = await istemci.PatchJson(
            $"/api/v1/kullanicilar/{kiraci.KullaniciId}/aktif", new { aktif = false });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        var mesaj = (await cevap.Json()).GetProperty("message").GetString()!;
        Assert.Contains("tek SAHİBİ", mesaj);
        Assert.Contains("Sahipliği devret", mesaj);

        Assert.True(RolOku(kiraci.KullaniciId) == "SAHIP");
        using var db = fikstur.Vt.OlusturSistem();
        Assert.True(db.Kullanicilar.AsNoTracking().Single(k => k.Id == kiraci.KullaniciId).Aktif);
    }

    /// <summary>Son aktif SAHİP'in ROLÜ de düşürülemez — aynı kapı, ikinci yol.</summary>
    [Fact]
    public async Task B36_Son_SAHIBIN_rolu_de_dusurulemez()
    {
        var kiraci = Kur("b36rol");
        var ikinciSahip = KullaniciEkle(kiraci, "SAHIP");
        // ikinciSahip'i önce pasife al: aktif SAHİP sayısı 1'e iner.
        using (var db = fikstur.Vt.OlusturSistem())
        {
            var k = db.Kullanicilar.Single(x => x.Id == ikinciSahip);
            k.Aktif = false;
            db.SaveChanges();
        }
        var istemci = fikstur.Ortam.Istemci(ikinciSahip, kiraci.TenantId, kiraci.SubeId, "SAHIP");

        var cevap = await istemci.PatchJson(
            $"/api/v1/kullanicilar/{kiraci.KullaniciId}/rol", new { rol = "MUDUR" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        // ⚠ Yalnız durum koduna bakmak YETMEZ: rütbe kapısı ("eşit rol") de 403
        //   döner. Reddin TEK SAHİP kapısından geldiğini ve çıkış yolunu (devir)
        //   söylediğini metinle kilitleriz — yoksa kapı boşaltılınca test yeşil kalır.
        var mesaj = (await cevap.Json()).GetProperty("message").GetString()!;
        Assert.Contains("tek SAHİBİ", mesaj);
        Assert.Contains("Sahipliği devret", mesaj);
        Assert.Equal("SAHIP", RolOku(kiraci.KullaniciId));
    }

    /// <summary>
    /// Devir TEK İŞLEMDİR: hedef SAHİP olur, devreden düşer, kiracıda hiçbir an
    /// "sıfır SAHİP" ya da "iki SAHİP" hâli kalmaz ve iz yazılır.
    /// </summary>
    [Fact]
    public async Task B36_Sahiplik_devri_hedefi_SAHIP_yapar_devredeni_dusurur_ve_iz_yazar()
    {
        var kiraci = Kur("b36devir");
        var hedefId = KullaniciEkle(kiraci, "TEZGAHTAR");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PostJson(
            $"/api/v1/kullanicilar/{hedefId}/sahiplik-devret",
            new { sifre = Sifre, yeniRolum = "MUDUR" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var govde = await cevap.Json();
        Assert.Equal(hedefId, govde.GetProperty("yeniSahipId").GetString());
        Assert.Equal("MUDUR", govde.GetProperty("yeniRolunuz").GetString());

        Assert.Equal("SAHIP", RolOku(hedefId));
        Assert.Equal("MUDUR", RolOku(kiraci.KullaniciId));

        using var db = fikstur.Vt.OlusturSistem();
        Assert.Equal(1, db.Kullanicilar.AsNoTracking()
            .Count(k => k.TenantId == kiraci.TenantId && k.Rol == "SAHIP" && k.Aktif));
        Assert.Equal(1, DenetimSay(kiraci.TenantId, "sahiplik-devredildi"));
    }

    /// <summary>Çalınmış oturum sahipliği devredemez: parola yanlışsa roller DEĞİŞMEZ.</summary>
    [Fact]
    public async Task B36_Devir_yanlis_parolayla_reddedilir_ve_roller_DEGISMEZ()
    {
        var kiraci = Kur("b36parola");
        var hedefId = KullaniciEkle(kiraci, "TEZGAHTAR");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PostJson(
            $"/api/v1/kullanicilar/{hedefId}/sahiplik-devret",
            new { sifre = "YanlisParola123", yeniRolum = "MUDUR" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("TEZGAHTAR", RolOku(hedefId));
        Assert.Equal("SAHIP", RolOku(kiraci.KullaniciId));
        Assert.Equal(0, DenetimSay(kiraci.TenantId, "sahiplik-devredildi"));
    }

    /// <summary>Devirden sonraki rolüm SAHİP olamaz — iki SAHİP hâli kapıdan geçmez.</summary>
    [Fact]
    public async Task B36_Devirden_sonraki_rolum_SAHIP_olamaz()
    {
        var kiraci = Kur("b36cift");
        var hedefId = KullaniciEkle(kiraci, "TEZGAHTAR");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PostJson(
            $"/api/v1/kullanicilar/{hedefId}/sahiplik-devret",
            new { sifre = Sifre, yeniRolum = "SAHIP" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("TEZGAHTAR", RolOku(hedefId));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B-37 — KULLANICI SİLİNMEZ, PASİFE ALINIR VE GERİ ALINABİLİR
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// DELETE ucu HARD DELETE YAPMAZ: satır DB'de kalır, yalnız aktif=false olur.
    /// Kullanıcı satırı denetim izinin ve geçmiş hareketlerin sahibidir.
    /// </summary>
    [Fact]
    public async Task B37_DELETE_kaydi_SILMEZ_pasife_alir_satir_DB_de_KALIR()
    {
        var kiraci = Kur("b37sil");
        var hedefId = KullaniciEkle(kiraci, "TEZGAHTAR");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.DeleteAsync($"/api/v1/kullanicilar/{hedefId}");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = fikstur.Vt.OlusturSistem();
        var satir = db.Kullanicilar.AsNoTracking().SingleOrDefault(k => k.Id == hedefId);
        Assert.NotNull(satir);
        Assert.False(satir!.Aktif);
        Assert.Equal(1, DenetimSay(kiraci.TenantId, "kullanici-pasife-alindi"));
    }

    /// <summary>Pasife alma GERİ ALINABİLİR: PATCH /aktif ile kullanıcı geri döner.</summary>
    [Fact]
    public async Task B37_Pasif_kullanici_GERI_aktiflestirilebilir_ve_iz_yazar()
    {
        var kiraci = Kur("b37geri");
        var hedefId = KullaniciEkle(kiraci, "TEZGAHTAR", aktif: false);
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PatchJson($"/api/v1/kullanicilar/{hedefId}/aktif", new { aktif = true });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = fikstur.Vt.OlusturSistem();
        Assert.True(db.Kullanicilar.AsNoTracking().Single(k => k.Id == hedefId).Aktif);
        Assert.Equal(1, DenetimSay(kiraci.TenantId, "kullanici-aktiflestirildi"));
    }

    /// <summary>Pasif kullanıcıya sahiplik devredilemez — sessiz kilit doğmaz.</summary>
    [Fact]
    public async Task B37_Pasif_kullaniciya_sahiplik_devredilemez()
    {
        var kiraci = Kur("b37devir");
        var hedefId = KullaniciEkle(kiraci, "TEZGAHTAR", aktif: false);
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PostJson(
            $"/api/v1/kullanicilar/{hedefId}/sahiplik-devret", new { sifre = Sifre });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("SAHIP", RolOku(kiraci.KullaniciId));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Y-39 — YETKİ MATRİSİ ROTA HARİTASINDAN OTOMATİK ÜRETİLİR
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Matris ELLE yazılmaz: Moduller.Tumu tam olarak haritanın türevidir.</summary>
    [Fact]
    public void Y39_Moduller_Tumu_rota_haritasindan_TURETILIR()
    {
        var beklenen = Kuyumcu.Core.Erisim.RotaModulleri.Harita.Values
            .Where(v => v is not null).Select(v => v!).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Equal(beklenen, Moduller.Tumu);
        Assert.NotEmpty(Moduller.Tumu);
    }

    /// <summary>
    /// Matriste karşılığı OLMAYAN ekran kalmadı: P-09'un eklediği on anahtar
    /// (emanet · kur · vezne · pos · sayim · uyum · yedek · veri-aktarim ·
    /// denetim-izi · fiyat-listesi) matriste görünür.
    /// </summary>
    [Theory]
    [InlineData("emanet")]
    [InlineData("kur")]
    [InlineData("vezne")]
    [InlineData("pos")]
    [InlineData("sayim")]
    [InlineData("uyum")]
    [InlineData("yedek")]
    [InlineData("veri-aktarim")]
    [InlineData("denetim-izi")]
    [InlineData("fiyat-listesi")]
    public void Y39_Yeni_modul_anahtarlari_matriste_VAR(string anahtar)
        => Assert.Contains(anahtar, Moduller.Tumu);

    /// <summary>
    /// İKİ YÖNLÜ KİLİT: haritadaki her anahtar GERÇEK bir rotadır ve ekrandaki
    /// her rota haritadadır. Tek yön yeterli değildir — ölü anahtar matrisi
    /// şişirir, eksik anahtar ekranı fail-open bırakır.
    /// </summary>
    [Fact]
    public void Y39_Harita_ile_RotaBasliklari_IKI_YONLU_eslesir()
    {
        var haritaRotalari = Kuyumcu.Core.Erisim.RotaModulleri.Harita.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var baslikRotalari = Kuyumcu.Web.Layout.RotaBasliklari.Harita.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Equal(baslikRotalari, haritaRotalari);
    }

    /// <summary>
    /// GENİŞLEME YOK: yeni anahtarlar TEZGÂHTAR'a topluca açılmadı. "uyum" ve
    /// "denetim-izi" kişisel veri rejimindedir, varsayılanda KAPALI kalır.
    /// </summary>
    [Fact]
    public void Y39_Kisisel_veri_ekranlari_TEZGAHTAR_varsayilaninda_KAPALI()
    {
        var tezgahtar = Moduller.Varsayilan["TEZGAHTAR"];
        Assert.DoesNotContain("uyum", tezgahtar);
        Assert.DoesNotContain("denetim-izi", tezgahtar);
        Assert.DoesNotContain("yedek", tezgahtar);
        Assert.DoesNotContain("veri-aktarim", tezgahtar);
    }

    /// <summary>
    /// Varsayılanlar BUGÜNKÜ erişimi korur: "kasa" anahtarı olan her rol "pos"u,
    /// "urun" anahtarı olan her rol "sayim"ı da alır (sessiz erişim kaybı yok).
    /// </summary>
    [Theory]
    [InlineData("MUDUR")]
    [InlineData("MUHASEBE")]
    [InlineData("TEZGAHTAR")]
    public void Y39_Varsayilanlar_bugunku_erisimi_KORUR(string rol)
    {
        var izinler = Moduller.Varsayilan[rol];
        if (izinler.Contains("kasa")) Assert.Contains("pos", izinler);
        if (izinler.Contains("urun")) Assert.Contains("sayim", izinler);
        if (izinler.Contains("urun")) Assert.Contains("fiyat-listesi", izinler);
    }

    /// <summary>Yetki matrisi ucu SAHİP kapısındadır; MÜDÜR matrisi okuyamaz.</summary>
    [Fact]
    public async Task Y39_Matris_ucu_SAHIP_kapisindadir()
    {
        var kiraci = Kur("y39kapi");
        var mudur = fikstur.Ortam.Istemci(kiraci, "MUDUR");
        var sahip = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        Assert.Equal(HttpStatusCode.Forbidden, (await mudur.GetAsync("/api/v1/yetkiler")).StatusCode);

        var cevap = await sahip.GetAsync("/api/v1/yetkiler");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var moduller = (await cevap.Json()).GetProperty("moduller").EnumerateArray()
            .Select(x => x.GetString()!).ToArray();
        Assert.Equal(Moduller.Tumu, moduller);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // K3 DİLİMİ (Ramazan · BK-13 / BK-35) — YETKİSİZ PlanGuncelle
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Plan değiştirme SAHİP kapısı arkasındadır — MÜDÜR geçemez.</summary>
    [Fact]
    public async Task K3_Plan_ucu_MUDURu_reddeder()
    {
        var kiraci = Kur("k3rol");
        var istemci = fikstur.Ortam.Istemci(kiraci, "MUDUR");

        var cevap = await istemci.PatchJson("/api/v1/admin/plan", new { plan = "KURUMSAL" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        using var db = fikstur.Vt.OlusturSistem();
        Assert.Equal("PRO", db.Tenants.AsNoTracking().Single(t => t.Id == kiraci.TenantId).Plan);
    }

    /// <summary>
    /// Plan değişimi İZSİZ kalamaz: denetim izinde SATIR doğar.
    /// <para>
    /// ⚠ PAKET P-27 UYARLAMASI: eskiden bu test PRO → KURUMSAL (YÜKSELTME)
    /// gönderiyordu ve 200 bekliyordu. F-3 plan kapısı ile yükseltme kiracıya
    /// KAPANDI (403); iz kuralı değişmedi, yön değişti. İz damgası da
    /// AbonelikServisi.IzDamgasi ile tek kaynaktan gelir.
    /// </para>
    /// </summary>
    [Fact]
    public async Task K3_Plan_degisimi_denetim_izine_SATIR_yazar()
    {
        var kiraci = Kur("k3iz");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PatchJson("/api/v1/admin/plan", new { plan = "BASLANGIC" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = fikstur.Vt.OlusturSistem();
        Assert.Equal("BASLANGIC", db.Tenants.AsNoTracking().Single(t => t.Id == kiraci.TenantId).Plan);
        Assert.Equal(1, DenetimSay(kiraci.TenantId,
            $"{Kuyumcu.Api.Modules.Abonelik.AbonelikServisi.IzDamgasi}: PRO → BASLANGIC"));
    }

    /// <summary>
    /// Kapı SERVİSTEDİR: DTO DataAnnotations yalnız controller yolunda çalışır,
    /// servis doğrudan çağrıldığında uydurma plan yazılamaz.
    /// <para>⚠ PAKET P-27: plan yazan servis artık AbonelikServisi'dir
    /// (AdminServisi.PlanGuncelle kaldırıldı — tek yazma yolu).</para>
    /// </summary>
    [Fact]
    public async Task K3_Servis_uydurma_plani_REDDEDER_DTO_ya_guvenmez()
    {
        var kiraci = Kur("k3servis");
        using var db = fikstur.Vt.Olustur(kiraci);
        var servis = P27AbonelikTestleri.KiraciServisi(db);
        var islem = new Kuyumcu.Api.Auth.AktifKullanici(
            kiraci.KullaniciId, kiraci.TenantId, kiraci.SubeId, "SAHIP");

        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(
            () => servis.KiraciPlanDegistir("KURUMSAL ", islem));
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(
            () => servis.KiraciPlanDegistir("BEDAVA", islem));

        using var oku = fikstur.Vt.OlusturSistem();
        Assert.Equal("PRO", oku.Tenants.AsNoTracking().Single(t => t.Id == kiraci.TenantId).Plan);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B-6 / B-20 — MFA: KURTARMA İZİ DB SATIRI + YARIM KURULUM KİLİT YARATMAZ
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Yarım kalan MFA kurulumu /ayarlar'da KİLİT YARATMAZ: sıfırlanır ve
    /// kullanıcı yeniden kurulum başlatabilir. Etkin MFA bu uçla KAPANMAZ.
    /// </summary>
    [Fact]
    public async Task B6_Yarim_kalan_MFA_kurulumu_SIFIRLANABILIR()
    {
        var kiraci = Kur("b6yarim");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var kur = await istemci.PostAsync("/api/v1/auth/mfa/kur", null);
        Assert.Equal(HttpStatusCode.OK, kur.StatusCode);
        using (var db = fikstur.Vt.OlusturSistem())
            Assert.Equal(1, db.KullaniciMfalari.AsNoTracking().Count(m => m.KullaniciId == kiraci.KullaniciId));

        var sifirla = await istemci.PostAsync("/api/v1/auth/mfa/sifirla", null);
        Assert.Equal(HttpStatusCode.OK, sifirla.StatusCode);

        using (var db = fikstur.Vt.OlusturSistem())
            Assert.Equal(0, db.KullaniciMfalari.AsNoTracking()
                .Count(m => m.KullaniciId == kiraci.KullaniciId && m.Durum != MfaDurumu.Etkin));

        // Kilit yok: yeniden kurulum açılabiliyor.
        Assert.Equal(HttpStatusCode.OK, (await istemci.PostAsync("/api/v1/auth/mfa/kur", null)).StatusCode);
        Assert.Equal(1, DenetimSay(kiraci.TenantId, "mfa-kurulum-sifirlandi"));
    }

    /// <summary>
    /// B-20 — kurtarma kodu tüketimi denetim izinde SATIR olarak durur ve
    /// kodun KENDİSİ yazılmaz (sır hijyeni).
    /// </summary>
    [Fact]
    public async Task B20_Kurtarma_kodu_kullanimi_izde_SATIR_birakir_kod_YAZILMAZ()
    {
        var kiraci = Kur("b20iz");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        // Kurulum → doğrulama (etkinleşir, kurtarma kodları BİR KEZ döner).
        var kur = await (await istemci.PostAsync("/api/v1/auth/mfa/kur", null)).Json();
        var giz = kur.GetProperty("gizBase32").GetString()!;
        var dogrula = await istemci.PostJson("/api/v1/auth/mfa/dogrula",
            new { kod = Totp.Uret(Base32.Coz(giz), Totp.Adim(DateTime.UtcNow, 30)) });
        Assert.Equal(HttpStatusCode.OK, dogrula.StatusCode);
        var kodlar = (await dogrula.Json()).GetProperty("kurtarmaKodlari")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.NotEmpty(kodlar);

        // Kurtarma koduyla MFA'yı kapat → kod TÜKETİLİR ve iz düşer.
        var kapat = await istemci.PostJson("/api/v1/auth/mfa/kaldir",
            new { sifre = Sifre, kod = kodlar[0] });
        Assert.Equal(HttpStatusCode.OK, kapat.StatusCode);

        using var db = fikstur.Vt.OlusturSistem();
        var izler = db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == kiraci.TenantId && d.Detay != null
                     && d.Detay.Contains("mfa-kurtarma-kullanildi"))
            .ToList();
        Assert.Single(izler);
        Assert.DoesNotContain(kodlar[0], izler[0].Detay!);
        Assert.True(izler[0].Hassas);
    }
}
