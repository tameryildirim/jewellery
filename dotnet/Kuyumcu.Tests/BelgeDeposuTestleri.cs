using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BELGE DEPOSU — paket 4 Faz A [DB]
//
// ⚠ KİRACI SIZINTISI TESTLERİ vt.OlusturSistem() (FİLTRESİZ) bağlamında koşar:
// vt.Olustur(kiraci) ile koşsalardı global HasQueryFilter 404'ü ZATEN üretirdi
// ve servisteki açık TenantId şartının kaldırılması yakalanmazdı (Aşkın'ın
// koşulu — ADR-0010'un İKİNCİ katmanı ancak böyle sınanır).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class BelgeDeposuTestleri(TestVeritabani vt)
{
    private static T Prop<T>(object o, string ad) =>
        (T)o.GetType().GetProperties().Single(p => string.Equals(p.Name, ad, StringComparison.OrdinalIgnoreCase)).GetValue(o)!;

    private static string PdfB64(string ek = "") =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("%PDF-1.7 " + ek));

    private static string JpegB64(int boyut = 64)
    {
        var b = new byte[boyut];
        b[0] = 0xFF; b[1] = 0xD8; b[2] = 0xFF;
        return Convert.ToBase64String(b);
    }

    private static BelgeYukleDto Dto(string sahipTuru, string sahipId, string tur, string icerik, string ad = "d.pdf") =>
        new() { SahipTuru = sahipTuru, SahipId = sahipId, Tur = tur, Ad = ad, IcerikBase64 = icerik };

    // ── Mutlu yol ────────────────────────────────────────────────────────────

    [Fact]
    public async Task URUN_sahibine_yukleme_satiri_SAHIP_TURU_ve_SAHIP_ID_ile_yazar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge urun");
        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db);

        var y = await s.Ekle(Dto("URUN", k.UrunId, "SERTIFIKA", PdfB64("sertifika")), k.Islem);
        var id = Prop<string>(y, "Id");

        using var kontrol = vt.OlusturSistem();
        var satir = kontrol.Belgeler.AsNoTracking().Single(b => b.Id == id);
        Assert.Equal("URUN", satir.SahipTuru);
        Assert.Equal(k.UrunId, satir.SahipId);
        Assert.Equal("application/pdf", satir.Mime);
        Assert.Equal(
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("%PDF-1.7 sertifika"))).ToLowerInvariant(),
            satir.Sha256);
    }

    [Fact]
    public async Task ISLETME_sahibi_KENDI_kiracisidir_baska_kiracinin_kimligi_404()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge isletme");
        var k2 = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge isletme yabanci");
        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db);

        await s.Ekle(Dto("ISLETME", k.TenantId, "GORSEL", JpegB64(), "logo.jpg"), k.Islem);

        // ⚠ Mehmet K4-2: başka kiracının kimliği verilince VARLIK SIZDIRILMAZ.
        await Assert.ThrowsAsync<NotFoundException>(
            () => s.Ekle(Dto("ISLETME", k2.TenantId, "GORSEL", JpegB64(), "logo.jpg"), k.Islem));
    }

    // ── Sahip kapısı (fail-closed) ───────────────────────────────────────────

    [Fact]
    public async Task Olmayan_sahip_404_ve_SATIR_YAZILMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge olmayan sahip");
        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db);

        await Assert.ThrowsAsync<NotFoundException>(
            () => s.Ekle(Dto("URUN", "YOK-BOYLE-URUN", "SERTIFIKA", PdfB64()), k.Islem));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.Belgeler.Count(b => b.SahipId == "YOK-BOYLE-URUN"));
    }

    [Fact]
    public async Task Yabanci_kiracinin_urunune_yukleme_404_FILTRESIZ_baglamda()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge yabanci A");
        var k2 = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge yabanci B");

        // ⚠ FİLTRESİZ bağlam: global filtre devrede değil, servis kapısı sınanıyor.
        using var db = vt.OlusturSistem();
        var s = new BelgeServisi(db);

        await Assert.ThrowsAsync<NotFoundException>(
            () => s.Ekle(Dto("URUN", k2.UrunId, "SERTIFIKA", PdfB64()), k.Islem));
    }

    [Fact]
    public async Task Yabanci_kiracinin_belgesinin_icerigi_404_FILTRESIZ_baglamda()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge icerik A");
        var k2 = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge icerik B");

        using var dbB = vt.Olustur(k2.TenantId);
        var id = Prop<string>(
            await new BelgeServisi(dbB).Ekle(Dto("URUN", k2.UrunId, "SERTIFIKA", PdfB64("B")), k2.Islem), "Id");

        using var db = vt.OlusturSistem();
        await Assert.ThrowsAsync<NotFoundException>(() => new BelgeServisi(db).Icerik(id, k.Islem));
        await Assert.ThrowsAsync<NotFoundException>(() => new BelgeServisi(db).PasifeAl(id, k.Islem));
    }

    [Fact]
    public async Task Kapali_liste_disi_sahip_turu_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge sahip turu");
        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db);
        Assert.Contains("sahip türü",
            (await Assert.ThrowsAsync<BadRequestException>(
                () => s.Ekle(Dto("CARI", k.CariId, "EK", PdfB64()), k.Islem))).Message);
    }

    /// <summary>
    /// ⚠ Bertan'ın koşulu: SIPARIS dalı kapalı listede KALIR ama uygulanmadığı
    /// AÇIKÇA söylenir. Test olmasaydı paket 3 geldiğinde dalın hiç yazılmadığı
    /// fark edilmezdi.
    /// </summary>
    [Fact]
    public async Task SIPARIS_dali_ACIKCA_reddeder_siparis_modulu_henuz_yok()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge siparis");
        using var db = vt.Olustur(k.TenantId);
        Assert.Contains("Sipariş modülü henüz yok",
            (await Assert.ThrowsAsync<BadRequestException>(
                () => new BelgeServisi(db).Ekle(Dto("SIPARIS", "S-1", "EK", PdfB64()), k.Islem))).Message);
    }

    /// <summary>
    /// ⚠ Mehmet K4-3 (VETO koşulu): gider pusulası eki KAPALIDIR — imzalı pusula
    /// fotoğrafı satıcının TCKN'sini görüntü olarak taşır (ADR-0008 gerilimi).
    /// </summary>
    [Fact]
    public async Task GIDER_PUSULASI_dali_KAPALIDIR_ayri_karara_baglandi()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge pusula");
        using var db = vt.Olustur(k.TenantId);
        Assert.Contains("ADR-0008",
            (await Assert.ThrowsAsync<BadRequestException>(
                () => new BelgeServisi(db).Ekle(Dto("GIDER_PUSULASI", "G-1", "FOTOGRAF", JpegB64()), k.Islem))).Message);
    }

    // ── Tavanlar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sahip_basina_aktif_belge_tavani_ASILAMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge sahip tavan");
        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db, new BelgeAyarlari { SahipBasinaAzami = 2 });

        await s.Ekle(Dto("URUN", k.UrunId, "SERTIFIKA", PdfB64("1")), k.Islem);
        await s.Ekle(Dto("URUN", k.UrunId, "SERTIFIKA", PdfB64("2")), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => s.Ekle(Dto("URUN", k.UrunId, "SERTIFIKA", PdfB64("3")), k.Islem));
        Assert.Contains("2 aktif belge", ex.Message);
        Assert.Contains("mevcut: 2", ex.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(2, kontrol.Belgeler.Count(b => b.TenantId == k.TenantId && b.SahipId == k.UrunId));
    }

    [Fact]
    public async Task Kiraci_toplam_tavani_asilinca_400_ve_mesaj_MEVCUT_kullanimi_yazar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge toplam tavan");
        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db, new BelgeAyarlari { KiraciToplamMb = 1 });

        await s.Ekle(Dto("URUN", k.UrunId, "GORSEL", JpegB64(600 * 1024), "a.jpg"), k.Islem);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => s.Ekle(Dto("URUN", k.UrunId, "GORSEL", JpegB64(600 * 1024), "b.jpg"), k.Islem));
        Assert.Contains("mevcut 600 KB", ex.Message);
        Assert.Contains("tavan 1 MB", ex.Message);
    }

    // ── Liste / pasife alma ──────────────────────────────────────────────────

    /// <summary>
    /// M-B8: liste ucu İÇERİK DÖNDÜRMEZ. 20 belgelik bir liste 40 MB JSON
    /// üretemez. ⚠ Anonim tip PascalCase üretir → OrdinalIgnoreCase ile aranır.
    /// </summary>
    [Fact]
    public async Task Liste_yanitinda_ICERIK_alani_YOKTUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge liste icerik");
        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db);
        await s.Ekle(Dto("URUN", k.UrunId, "SERTIFIKA", PdfB64("liste")), k.Islem);

        var liste = await s.ListeGenel("URUN", k.UrunId, k.Islem);
        var satir = Assert.Single(liste);

        var adlar = satir.GetType().GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(adlar, a => a.Equals("icerik", StringComparison.OrdinalIgnoreCase));

        var json = System.Text.Json.JsonSerializer.Serialize(satir);
        using var belge = System.Text.Json.JsonDocument.Parse(json);
        Assert.DoesNotContain(belge.RootElement.EnumerateObject(),
            p => p.Name.Equals("icerik", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DELETE_sonrasi_liste_bos_satir_AKTIF_0_ve_SILINMEMIS()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge pasife");
        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db);
        var id = Prop<string>(await s.Ekle(Dto("URUN", k.UrunId, "SERTIFIKA", PdfB64("p")), k.Islem), "Id");

        await s.PasifeAl(id, k.Islem);
        Assert.Empty(await s.ListeGenel("URUN", k.UrunId, k.Islem));

        using var kontrol = vt.OlusturSistem();
        var satir = kontrol.Belgeler.AsNoTracking().Single(b => b.Id == id);
        Assert.False(satir.Aktif);
    }

    [Fact]
    public async Task Ayni_sha256_IKI_FARKLI_urune_baglanabilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge ayni sha");
        using var sistem = vt.OlusturSistem();
        var urun2 = Kimlik.Ulid();
        sistem.Urunler.Add(new Urun
        {
            Id = urun2, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"K2-{urun2[..8]}", Ad = "İkinci",
            AyarId = "K22", Milyem = 916m, GramAgirlik = 5m, TasAgirlik = 0m, HasGrami = 4.58m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
        });
        sistem.SaveChanges();

        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db);
        var a = Prop<string>(await s.Ekle(Dto("URUN", k.UrunId, "SERTIFIKA", PdfB64("ayni")), k.Islem), "Sha256");
        var b = Prop<string>(await s.Ekle(Dto("URUN", urun2, "SERTIFIKA", PdfB64("ayni")), k.Islem), "Sha256");
        Assert.Equal(a, b);   // indeks BENZERSİZ DEĞİL — ikinci bağ meşrudur
    }

    // ── Eski ürün yolu korunuyor ─────────────────────────────────────────────

    [Fact]
    public async Task Eski_urun_yolu_YENI_servise_delege_eder_ve_URUN_sahibi_yazar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Belge eski yol");
        using var db = vt.Olustur(k.TenantId);
        var s = new BelgeServisi(db);

        var y = await s.Yukle(k.UrunId, new BelgeYukleDto { Ad = "GIA.pdf", IcerikBase64 = PdfB64("eski") }, k.Islem);
        var id = Prop<string>(y, "Id");

        using var kontrol = vt.OlusturSistem();
        var satir = kontrol.Belgeler.AsNoTracking().Single(b => b.Id == id);
        Assert.Equal("URUN", satir.SahipTuru);
        Assert.Equal(k.UrunId, satir.SahipId);
        Assert.Equal("SERTIFIKA", satir.Tur);
        Assert.Single(await s.Liste(k.UrunId, k.Islem));
    }
}
