using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Isler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-11 (B-24) — ZAMANLANMIŞ İŞ AYARI KAPILARI (veritabanı düzeyi).
//
// HTTP testleri ucun sözleşmesini ölçer; bu dosya ÇATININ ayarları gerçekten
// UYGULADIĞINI ölçer: saat kapısı, kiracı kapaması ve "ayar yalnız daraltır"
// kuralı (yapılandırmada kapalı iş ekrandan açılamaz — fail-closed).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P11IsAyarTestleri(TestVeritabani vt)
{
    private static readonly IConfiguration Bos = IsCatisiKurulum.Yapilandirma();

    private Kiraci YeniKiraci(string ad)
    {
        using var db = vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static BildirimTaslagi Taslak(string anahtar) =>
        new("SAHTE", BildirimOnem.Uyari, "Sahte başlık", "Sahte gövde", "/uyum", null, null, anahtar, null);

    private static async Task AyarYaz(KuyumcuDbContext db, Kiraci k, string anahtar, string deger)
    {
        db.KiraciAyarlari.Add(new KiraciAyari
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId,
            Anahtar = anahtar, Deger = deger, Guncellendi = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Verilen YEREL saate denk gelen UTC anı — testi saat dilimine bağımlı bırakmaz.</summary>
    private static DateTime YerelSaatteUtc(int yerelSaat)
    {
        var an = DateTime.UtcNow.Date.AddHours(12);
        for (var i = 0; i < 48; i++, an = an.AddHours(1))
            if (Zaman.UtcdenYerele(an).Hour == yerelSaat) return an;
        throw new InvalidOperationException("Yerel saat bulunamadı");
    }

    // ── 1) SAAT KAPISI ──────────────────────────────────────────────────────

    [Fact]
    public async Task Tur_baslangic_saatinden_ONCE_hic_kosmaz()
    {
        var k = YeniKiraci("P11-TEST-saat-once");
        using var db = vt.Olustur(k);
        await AyarYaz(db, k, IsAyarAnahtari.BaslangicSaat, "10");

        var i = new SahteIs("SAAT_KAPISI", _ => new IsCikti([Taslak($"S:{k.TenantId}")], 1));
        var c = IsCatisiKurulum.Calistirici(db, Bos, i);

        var sonuclar = await c.TumEtkinIsleriCalistir(k.TenantId, YerelSaatteUtc(9), default);

        Assert.Empty(sonuclar);
        Assert.Equal(0, i.CagriSayisi);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.IsCalismalari.CountAsync(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Tur_baslangic_saatinde_ve_SONRASINDA_kosar()
    {
        var k = YeniKiraci("P11-TEST-saat-sonra");
        using var db = vt.Olustur(k);
        await AyarYaz(db, k, IsAyarAnahtari.BaslangicSaat, "10");

        var i = new SahteIs("SAAT_KAPISI_2", _ => new IsCikti([Taslak($"S2:{k.TenantId}")], 1));
        var c = IsCatisiKurulum.Calistirici(db, Bos, i);

        var sonuclar = await c.TumEtkinIsleriCalistir(k.TenantId, YerelSaatteUtc(10), default);

        Assert.Single(sonuclar);
        Assert.Equal(1, i.CagriSayisi);
    }

    [Fact]
    public async Task Saat_kapisi_ELLE_kosumu_ENGELLEMEZ()
    {
        // Kullanıcı düğmeye bilerek bastı; saat kuralı zamanlanmış tur içindir.
        var k = YeniKiraci("P11-TEST-saat-elle");
        using var db = vt.Olustur(k);
        await AyarYaz(db, k, IsAyarAnahtari.BaslangicSaat, "23");

        var i = new SahteIs("SAAT_ELLE", _ => new IsCikti([Taslak($"SE:{k.TenantId}")], 1));
        var c = IsCatisiKurulum.Calistirici(db, Bos, i);

        var s = await c.ElleCalistir(i, k.TenantId, YerelSaatteUtc(1), k.KullaniciId, default);

        Assert.False(s.Atlandi);
        Assert.Equal(1, i.CagriSayisi);
    }

    // ── 2) KİRACI KAPAMASI ──────────────────────────────────────────────────

    [Fact]
    public async Task Kiracinin_kapattigi_is_TURDA_kosmaz_digerleri_kosar()
    {
        var k = YeniKiraci("P11-TEST-kapali-is");
        using var db = vt.Olustur(k);
        await AyarYaz(db, k, IsAyarAnahtari.KapaliIsler, "KAPATILAN");

        var kapali = new SahteIs("KAPATILAN", _ => new IsCikti([Taslak($"K1:{k.TenantId}")], 1));
        var acik = new SahteIs("ACIK_IS", _ => new IsCikti([Taslak($"K2:{k.TenantId}")], 1));
        var c = IsCatisiKurulum.Calistirici(db, Bos, kapali, acik);

        var sonuclar = await c.TumEtkinIsleriCalistir(k.TenantId, DateTime.UtcNow, default);

        Assert.Single(sonuclar);
        Assert.Equal("ACIK_IS", sonuclar[0].IsKodu);
        Assert.Equal(0, kapali.CagriSayisi);
        Assert.Equal(1, acik.CagriSayisi);
    }

    [Fact]
    public async Task Kiraci_kapamasi_DIGER_KIRACIYI_etkilemez()
    {
        var a = YeniKiraci("P11-TEST-izole-a");
        var b = YeniKiraci("P11-TEST-izole-b");
        using (var dba = vt.Olustur(a)) await AyarYaz(dba, a, IsAyarAnahtari.KapaliIsler, "IZOLE");

        using var db = vt.Olustur(b);
        var i = new SahteIs("IZOLE", _ => new IsCikti([Taslak($"I:{b.TenantId}")], 1));
        var c = IsCatisiKurulum.Calistirici(db, Bos, i);

        var sonuclar = await c.TumEtkinIsleriCalistir(b.TenantId, DateTime.UtcNow, default);

        Assert.Single(sonuclar);
        Assert.Equal(1, i.CagriSayisi);
    }

    // ── 3) AYAR YALNIZ DARALTIR (fail-closed) ───────────────────────────────

    [Fact]
    public async Task Yapilandirmada_KAPALI_is_kiraci_ayariyla_ACILAMAZ()
    {
        var k = YeniKiraci("P11-TEST-failclosed");
        using var db = vt.Olustur(k);
        // Kiracı hiçbir işi kapatmamış (liste boş) — yine de açılmamalı.
        await AyarYaz(db, k, IsAyarAnahtari.KapaliIsler, "");

        var i = new SahteIs("YAP_KAPALI", _ => new IsCikti([Taslak($"Y:{k.TenantId}")], 1));
        var y = IsCatisiKurulum.Yapilandirma(("Isler:YAP_KAPALI:Etkin", "false"));
        var c = IsCatisiKurulum.Calistirici(db, y, i);

        Assert.False(await c.KiraciEtkinMi(i, k.TenantId));
        var sonuclar = await c.TumEtkinIsleriCalistir(k.TenantId, DateTime.UtcNow, default);

        Assert.Empty(sonuclar);
        Assert.Equal(0, i.CagriSayisi);
    }

    // ── 4) EŞİK EZME + PATCH SÖZLEŞMESİ (servis düzeyi) ─────────────────────

    [Fact]
    public async Task Esik_ayari_YAPILANDIRMA_varsayilanini_ezer()
    {
        var k = YeniKiraci("P11-TEST-esik");
        using var db = vt.Olustur(k);
        var y = IsCatisiKurulum.Yapilandirma(("Isler:Vade:UfukGun", "7"), ("Isler:Masak:UyariGun", "3"));
        var kapsam = IsCatisiKurulum.Kapsam(db, y);
        var servis = kapsam.GetRequiredService<IsAyarServisi>();

        Assert.Equal(7, (await servis.Oku(k.TenantId)).VadeUfukGun);

        await AyarYaz(db, k, IsAyarAnahtari.VadeUfukGun, "30");
        var okunan = await servis.Oku(k.TenantId);

        Assert.Equal(30, okunan.VadeUfukGun);
        Assert.Equal(3, okunan.MasakUyariGun);   // dokunulmayan alan varsayılanda kaldı
    }

    [Fact]
    public async Task Bozuk_ayar_degeri_VARSAYILANA_duser_cokmez()
    {
        var k = YeniKiraci("P11-TEST-bozuk");
        using var db = vt.Olustur(k);
        await AyarYaz(db, k, IsAyarAnahtari.VadeUfukGun, "abc");
        await AyarYaz(db, k, IsAyarAnahtari.BaslangicSaat, "99");

        var y = IsCatisiKurulum.Yapilandirma(("Isler:Vade:UfukGun", "7"));
        var servis = IsCatisiKurulum.Kapsam(db, y).GetRequiredService<IsAyarServisi>();
        var okunan = await servis.Oku(k.TenantId);

        Assert.Equal(7, okunan.VadeUfukGun);                        // ayrıştırılamadı → varsayılan
        Assert.Equal(IsAyarAnahtari.SaatEnCok, okunan.BaslangicSaat); // aralığa sıkıştırıldı
    }

    // ── 5) SERVİS İÇİ ELLE DOĞRULAMA ────────────────────────────────────────
    //
    // ⚠ DataAnnotations YALNIZ controller yolunda çalışır. Servis başka bir
    //   yerden (iş, göç betiği, ileride başka bir uç) çağrıldığında sınırı
    //   zorlayan tek şey IsAyarServisi.Dogrula'dır. M11-6 mutasyonu bu boşluğu
    //   açığa çıkardı: [Range] sökülünce HTTP testleri hâlâ yeşildi.

    private static AktifKullanici Sahip(Kiraci k) => new(k.KullaniciId, k.TenantId, null, "SAHIP");

    [Theory]
    [InlineData(24, null, null)]
    [InlineData(-1, null, null)]
    [InlineData(null, 0, null)]
    [InlineData(null, 400, null)]
    [InlineData(null, null, 61)]
    public async Task Servis_araligi_KENDI_zorlar_controller_niteligine_guvenmez(
        int? saat, int? vade, int? masak)
    {
        var k = YeniKiraci("P11-TEST-servis-aralik");
        using var db = vt.Olustur(k);
        var servis = IsCatisiKurulum.Kapsam(db, Bos).GetRequiredService<IsAyarServisi>();

        var dto = new IsAyarPatchDto();
        if (saat is not null) dto.BaslangicSaat = saat;
        if (vade is not null) dto.VadeUfukGun = vade;
        if (masak is not null) dto.MasakUyariGun = masak;

        await Assert.ThrowsAsync<BadRequestException>(() => servis.Guncelle(Sahip(k), dto));

        // Reddedilen istek HİÇBİR satır yazmadı.
        using var kontrol = vt.OlusturSistem();
        Assert.False(await kontrol.KiraciAyarlari
            .AnyAsync(a => a.TenantId == k.TenantId && a.Anahtar.StartsWith("is_")));
    }

    [Fact]
    public async Task Servis_gecerli_degeri_YAZAR_sinir_ucunda_reddetmez()
    {
        var k = YeniKiraci("P11-TEST-servis-sinir");
        using var db = vt.Olustur(k);
        var servis = IsCatisiKurulum.Kapsam(db, Bos).GetRequiredService<IsAyarServisi>();

        var dto = new IsAyarPatchDto { BaslangicSaat = IsAyarAnahtari.SaatEnCok };
        var sonuc = await servis.Guncelle(Sahip(k), dto);

        Assert.Equal(IsAyarAnahtari.SaatEnCok, sonuc.BaslangicSaat);
    }
}
