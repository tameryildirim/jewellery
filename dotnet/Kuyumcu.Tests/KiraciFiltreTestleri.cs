using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ADIM 1 — kiracı izolasyonunun MUTASYON kanıtı. ADIM1-KARAR-2026-09-11.md §5.
//
// Mevcut KiraciIzolasyonTestleri SERVİS katmanını sınar: servis elle
// `TenantId ==` yazdığı için o testler filtre olmasa da yeşil kalır — yani
// filtrenin çalıştığını KANITLAMAZLAR (Aşkın'ın tespiti).
//
// Buradaki testler servisi TAMAMEN ATLAR, çıplak DbContext üzerinden gider ve
// hiçbir yerde elle `TenantId ==` yazmaz. Bu yüzden:
//   • filtre/denetçi kaldırılırsa → KIRMIZI
//   • yerinde durursa            → YEŞİL
// Filtrenin gerçekten taşıdığının tek kanıtı budur.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class KiraciFiltreTestleri(TestVeritabani vt)
{
    /// <summary>Faz 0'da kanıtlanan açığın rakamları — regresyon izi olarak korunur.</summary>
    private const decimal Faz0KasaTutari = 99_999m;
    private const decimal Faz0CariTutari = 77_777m;

    // ── M1: OKUMA filtresi ───────────────────────────────────────────────────

    [Fact]
    public void M1_YabanciKasaCiplakSorguyla_Gorunmez()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "M1-A");
        var b = TestKurulum.KiraciKur(kurulum, "M1-B");

        using var bDb = vt.Olustur(b);
        // DİKKAT: burada bilinçli olarak TenantId şartı YOK. Koruma yalnız filtreden gelir.
        var kasa = bDb.Kasalar.FirstOrDefault(k => k.Id == a.KasaId);

        Assert.Null(kasa);
    }

    [Fact]
    public void M1_YabanciCariVeUrunDeGorunmez()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "M1b-A");
        var b = TestKurulum.KiraciKur(kurulum, "M1b-B");

        using var bDb = vt.Olustur(b);
        Assert.Null(bDb.Cariler.FirstOrDefault(c => c.Id == a.CariId));
        Assert.Null(bDb.Urunler.FirstOrDefault(u => u.Id == a.UrunId));
        Assert.Null(bDb.KurSnapshots.FirstOrDefault(k => k.Id == a.KurId));
        Assert.Null(bDb.Subeler.FirstOrDefault(s => s.Id == a.SubeId));
    }

    [Fact]
    public void M1_ListelemeYalnizKendiKiracisiniDondurur()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "M1c-A");
        var b = TestKurulum.KiraciKur(kurulum, "M1c-B");

        using var bDb = vt.Olustur(b);
        var kasalar = bDb.Kasalar.ToList();

        Assert.NotEmpty(kasalar);
        Assert.All(kasalar, k => Assert.Equal(b.TenantId, k.TenantId));
        Assert.DoesNotContain(kasalar, k => k.Id == a.KasaId);
    }

    [Fact]
    public void M1_TenantTablosuKendisiDeFiltrelenir()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "M1d-A");
        var b = TestKurulum.KiraciKur(kurulum, "M1d-B");

        using var bDb = vt.Olustur(b);
        // Tenant'ta TenantId yok; filtre Id üzerinden kurulur.
        Assert.Null(bDb.Tenants.FirstOrDefault(t => t.Id == a.TenantId));
        Assert.NotNull(bDb.Tenants.FirstOrDefault(t => t.Id == b.TenantId));
    }

    // ── M2: YAZMA denetçisi ──────────────────────────────────────────────────
    // Query filter yalnız OKUMAyı korur. Aşağıdakiler filtreyle TAMAMEN serbesttir;
    // onları kapatan şey KiraciYazmaDenetcisi'dir.

    [Fact]
    public async Task M2_BaskaKiraciyaKayitEklenemez()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "M2-A");
        var b = TestKurulum.KiraciKur(kurulum, "M2-B");

        using var bDb = vt.Olustur(b);
        bDb.Kasalar.Add(new Kasa
        {
            Id = Kimlik.Ulid(), TenantId = a.TenantId, SubeId = a.SubeId,
            Ad = "Sızıntı kasası", ParaBirimi = "TRY", Bakiye = Faz0KasaTutari,
            Aktif = true, Guncellendi = DateTime.UtcNow,
        });

        await Assert.ThrowsAsync<KiraciIhlaliException>(() => bDb.SaveChangesAsync());

        // Yan etki sıfır mı — A'nın defterine hiçbir şey geçmemiş olmalı
        using var kontrol = vt.OlusturSistem();
        Assert.DoesNotContain(kontrol.Kasalar.Where(k => k.TenantId == a.TenantId),
            k => k.Ad == "Sızıntı kasası");
    }

    [Fact]
    public async Task M2_TenantIdBossaAktifKiraciylaDamgalanir()
    {
        using var kurulum = vt.OlusturSistem();
        var b = TestKurulum.KiraciKur(kurulum, "M2b-B");

        using var bDb = vt.Olustur(b);
        var id = Kimlik.Ulid();
        bDb.Kasalar.Add(new Kasa
        {
            Id = id, SubeId = b.SubeId, Ad = "Damga testi",
            ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
            // TenantId bilinçli olarak BOŞ
        });
        await bDb.SaveChangesAsync();

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(b.TenantId, kontrol.Kasalar.Single(k => k.Id == id).TenantId);
    }

    // ── M3: kiracı taşıma ────────────────────────────────────────────────────

    [Fact]
    public async Task M3_KaydinKiracisiDegistirilemez()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "M3-A");
        var b = TestKurulum.KiraciKur(kurulum, "M3-B");

        using var bDb = vt.Olustur(b);
        var kendiKasasi = bDb.Kasalar.Single(k => k.Id == b.KasaId);
        kendiKasasi.TenantId = a.TenantId;   // kendi kaydını A'ya taşımaya çalışıyor

        await Assert.ThrowsAsync<KiraciIhlaliException>(() => bDb.SaveChangesAsync());

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(b.TenantId, kontrol.Kasalar.Single(k => k.Id == b.KasaId).TenantId);
    }

    [Fact]
    public async Task M3_SistemBaglamiyla_CekilenYabanciKayit_KiraciBaglamindaKaydedilemez()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "M3b-A");
        var b = TestKurulum.KiraciKur(kurulum, "M3b-B");

        // Filtreyi atlatan bir yol kalmış olsaydı son savunma bu:
        // yabancı satır B bağlamında izlenmeye başlarsa kayıt reddedilir.
        using var bDb = vt.Olustur(b);
        var yabanci = kurulum.Kasalar.Single(k => k.Id == a.KasaId);
        kurulum.Entry(yabanci).State = EntityState.Detached;

        bDb.Attach(yabanci);
        yabanci.Bakiye = Faz0KasaTutari;
        bDb.Entry(yabanci).Property(k => k.Bakiye).IsModified = true;

        await Assert.ThrowsAsync<KiraciIhlaliException>(() => bDb.SaveChangesAsync());

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, a.KasaId));
    }

    // ── Faz 0 açığının birebir regresyonu (Aşkın md.6) ───────────────────────

    [Fact]
    public async Task Faz0_KasaSizintisi_ServisAtlanarakDaEngellenir()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "Faz0-A");
        var b = TestKurulum.KiraciKur(kurulum, "Faz0-B");

        using var bDb = vt.Olustur(b);

        // Faz 0'da bu HTTP 200 dönüyordu: B, A'nın kasasına 99.999 TL yazıyordu.
        // Artık kasayı GÖREMİYOR (okuma filtresi)…
        Assert.Null(bDb.Kasalar.FirstOrDefault(k => k.Id == a.KasaId));

        // …ve körlemesine yazmayı denerse de reddediliyor (yazma denetçisi).
        bDb.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(), TenantId = a.TenantId, KasaId = a.KasaId,
            Tutar = Faz0KasaTutari, ParaBirimi = "TRY", OdemeTuru = "NAKIT",
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
        });
        await Assert.ThrowsAsync<KiraciIhlaliException>(() => bDb.SaveChangesAsync());

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(kontrol, a.KasaId));
    }

    [Fact]
    public async Task Faz0_CariSizintisi_ServisAtlanarakDaEngellenir()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "Faz0c-A");
        var b = TestKurulum.KiraciKur(kurulum, "Faz0c-B");

        using var bDb = vt.Olustur(b);
        Assert.Null(bDb.Cariler.FirstOrDefault(c => c.Id == a.CariId));

        bDb.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = a.TenantId, SubeId = a.SubeId,
            CariId = a.CariId, KurSnapshotId = a.KurId, Tur = "SATIS",
            TlTutari = Faz0CariTutari, HasGrami = 0m,
            Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
        });
        await Assert.ThrowsAsync<KiraciIhlaliException>(() => bDb.SaveChangesAsync());

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, a.CariId).Tl);
    }

    // ── Fail-closed: bağlamsız DbContext ─────────────────────────────────────

    [Fact]
    public void BaglamsizDbContext_Kurulamaz()
    {
        // Kiracısı boş, sistem modu da kapalı bir bağlam → ctor patlamalı.
        // Sessizce "0 satır dönen" bir bağlam, yanlış veriyi doğru gösterirdi.
        var ex = Assert.Throws<InvalidOperationException>(() => vt.Olustur(""));
        Assert.Contains("Kiracı bağlamı çözülemedi", ex.Message);
    }

    // ── Sistem bağlamı kasten filtresiz ──────────────────────────────────────

    [Fact]
    public void SistemBaglami_TumKiracilariGorur()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "Sis-A");
        var b = TestKurulum.KiraciKur(kurulum, "Sis-B");

        Assert.NotNull(kurulum.Kasalar.FirstOrDefault(k => k.Id == a.KasaId));
        Assert.NotNull(kurulum.Kasalar.FirstOrDefault(k => k.Id == b.KasaId));
    }

    [Fact]
    public async Task SistemBaglami_TenantIdVerilmemisKaydiReddeder()
    {
        // Sistem bağlamı damgalamaz: arka plan işleri kiracısız satır üretemez.
        using var kurulum = vt.OlusturSistem();
        var b = TestKurulum.KiraciKur(kurulum, "Sis-b");

        using var sistem = vt.OlusturSistem();
        sistem.Kasalar.Add(new Kasa
        {
            Id = Kimlik.Ulid(), SubeId = b.SubeId, Ad = "Kiracısız",
            ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });

        await Assert.ThrowsAsync<KiraciIhlaliException>(() => sistem.SaveChangesAsync());
    }
}
