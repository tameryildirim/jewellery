using System.Net;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-04 (kimlik Y-1) — ŞUBE BAĞLAMI + GLOBAL SORGU FİLTRESİ
//                           [HTTP + servis, gerçek MySQL]
//
// Bu dosya, şube NULL sözleşmesinin ÜÇ maddesini ve yazma tarafını kilitler.
// Sözleşme metni: Data/KiraciBaglami.cs → IKiraciBaglami başlığı.
//
//   md.1  TumSubeler = true              → süzgeç kurulmaz (rapor süzgeçleri yaşar)
//   md.2  TumSubeler = false + şube dolu → kiracı AND şube, tek ifade
//   md.3  TumSubeler = false + şube boş  → FAIL-CLOSED (ne fail-open ne fail-blind)
//
// Negatif erişim testi (A şubesi jetonuyla B şubesi kaydı → 404) Aşkın'ın
// koşuludur ve GERÇEK HTTP isteğiyle koşar.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class SubeIzolasyonTestleri(HttpFikstur f)
{
    /// <summary>Kiracıya ikinci bir şube ve o şubede bir kasa ekler.</summary>
    private (string SubeId, string KasaId) IkinciSube(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var subeId = Kimlik.Ulid();
        var kasaId = Kimlik.Ulid();
        db.Subeler.Add(new Sube
        {
            Id = subeId, TenantId = k.TenantId, Ad = "Şube B", Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        db.Kasalar.Add(new Kasa
        {
            Id = kasaId, TenantId = k.TenantId, SubeId = subeId, Ad = "B Kasası",
            ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = now,
        });
        db.SaveChanges();
        return (subeId, kasaId);
    }

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    // ── Sözleşme md.1 — tüm şubeler ayrıcalığı ──────────────────────────────

    [Fact]
    public async Task Md1_TumSubeler_rolu_ikinci_subenin_kasasini_gorur()
    {
        var k = YeniKiraci("P-04-MD1");
        var (_, kasaB) = IkinciSube(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync($"/api/v1/kasalar/{kasaB}");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Equal(kasaB, (await cevap.Json()).GetProperty("id").GetString());
    }

    [Fact]
    public async Task Md1_TumSubeler_rolu_listede_iki_subenin_kasasini_birden_gorur()
    {
        var k = YeniKiraci("P-04-MD1-LISTE");
        var (_, kasaB) = IkinciSube(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/kasalar");
        var idler = (await cevap.Json()).EnumerateArray()
            .Select(x => x.GetProperty("id").GetString()).ToList();

        Assert.Contains(k.KasaId, idler);
        Assert.Contains(kasaB, idler);
    }

    // ── Sözleşme md.2 — şubeye kilitli rol ──────────────────────────────────

    [Fact]
    public async Task Md2_Sube_kilitli_rol_yalniz_kendi_subesinin_kasasini_listeler()
    {
        var k = YeniKiraci("P-04-MD2");
        var (_, kasaB) = IkinciSube(k);

        var cevap = await f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR")
            .GetAsync("/api/v1/kasalar");
        var idler = (await cevap.Json()).EnumerateArray()
            .Select(x => x.GetProperty("id").GetString()).ToList();

        Assert.Contains(k.KasaId, idler);
        Assert.DoesNotContain(kasaB, idler);
    }

    /// <summary>Aşkın koşulu: A şubesi jetonuyla B şubesi kaydı → 404 (varlık sızdırılmaz).</summary>
    [Fact]
    public async Task Md2_Negatif_A_subesi_jetonuyla_B_subesi_kaydi_404()
    {
        var k = YeniKiraci("P-04-NEGATIF");
        var (_, kasaB) = IkinciSube(k);

        var cevap = await f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR")
            .GetAsync($"/api/v1/kasalar/{kasaB}");

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
    }

    // ── Sözleşme md.3 — fail-closed ─────────────────────────────────────────

    [Fact]
    public async Task Md3_Subesiz_kilitli_rol_fail_closed_200_donmez()
    {
        var k = YeniKiraci("P-04-MD3");
        IkinciSube(k);

        var cevap = await f.Ortam.Istemci(k.KullaniciId, k.TenantId, null, "TEZGAHTAR")
            .GetAsync("/api/v1/kasalar");

        // Ne fail-open (200 + tüm şubeler) ne fail-blind (200 + boş liste).
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        Assert.Contains("Şube bağlamı çözülemedi",
            (await cevap.Json()).GetProperty("message").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public void Md3_Subesiz_kilitli_baglam_DbContext_insa_edilemez()
    {
        var k = YeniKiraci("P-04-MD3-BAGLAM");

        var hata = Assert.Throws<SubeIhlaliException>(() => f.Vt.OlusturSubeli(k.TenantId, null));

        Assert.Equal(403, hata.StatusCode);
        Assert.Contains("Şube bağlamı çözülemedi", hata.Message, StringComparison.Ordinal);
    }

    // ── Yazma tarafı — okuma süzgeci yazmayı durdurmaz ──────────────────────

    [Fact]
    public async Task Yazma_capraz_sube_kaydi_403()
    {
        var k = YeniKiraci("P-04-YAZMA");
        var (subeB, _) = IkinciSube(k);

        await using var db = f.Vt.OlusturSubeli(k.TenantId, k.SubeId);
        db.Kasalar.Add(new Kasa
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = subeB, Ad = "Sızma kasası",
            ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
        });

        var hata = await Assert.ThrowsAsync<SubeIhlaliException>(() => db.SaveChangesAsync());
        Assert.Equal(403, hata.StatusCode);
    }

    [Fact]
    public async Task Yazma_subesiz_yeni_kayit_aktif_sube_ile_damgalanir()
    {
        var k = YeniKiraci("P-04-DAMGA");
        var kasaId = Kimlik.Ulid();

        await using (var db = f.Vt.OlusturSubeli(k.TenantId, k.SubeId))
        {
            db.Kasalar.Add(new Kasa
            {
                Id = kasaId, TenantId = k.TenantId, Ad = "Damgasız kasa",
                ParaBirimi = "TRY", Bakiye = 0m, Aktif = true, Guncellendi = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await using var sistem = f.Vt.OlusturSistem();
        Assert.Equal(k.SubeId, (await sistem.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasaId)).SubeId);
    }

    [Fact]
    public async Task Yazma_yabanci_sube_kaydinin_guncellenmesi_403()
    {
        var k = YeniKiraci("P-04-GUNCELLE");
        var (subeB, kasaB) = IkinciSube(k);

        // Süzgeci atlatan bir yol kalsaydı son savunma burasıdır: kaydı SİSTEM
        // bağlamında okuyup şubeye kilitli bağlamda güncellemeyi dene.
        Kasa yabanci;
        await using (var sistem = f.Vt.OlusturSistem())
            yabanci = await sistem.Kasalar.AsNoTracking().SingleAsync(x => x.Id == kasaB);

        await using var db = f.Vt.OlusturSubeli(k.TenantId, k.SubeId);
        db.Kasalar.Attach(yabanci);
        yabanci.Bakiye = 99_999m;

        var hata = await Assert.ThrowsAsync<SubeIhlaliException>(() => db.SaveChangesAsync());
        Assert.Equal(403, hata.StatusCode);
        Assert.Contains(subeB, hata.Message, StringComparison.Ordinal);
    }

    // ── Şubesiz (kiracı geneli) kayıt gizlenmez ─────────────────────────────

    [Fact]
    public async Task Subesiz_kayit_sube_kilitli_baglamda_gorunur()
    {
        var k = YeniKiraci("P-04-SUBESIZ");
        var siparisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;

        await using (var sistem = f.Vt.OlusturSistem())
        {
            sistem.Siparisler.Add(new Siparis
            {
                Id = siparisId, TenantId = k.TenantId, No = $"S-{siparisId[..8]}", Tur = "MUSTERI",
                Durum = "ACIK", CariId = k.CariId, SubeId = null, OlusturanKullaniciId = k.KullaniciId,
                Olusturuldu = now, Guncellendi = now,
            });
            await sistem.SaveChangesAsync();
        }

        await using var db = f.Vt.OlusturSubeli(k.TenantId, k.SubeId);
        Assert.True(await db.Siparisler.AsNoTracking().AnyAsync(s => s.Id == siparisId));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-04 — MEKANİZMA TESTLERİ (saf, veritabanısız)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SubeMekanizmaTestleri
{
    [Theory]
    [InlineData("SAHIP", true)]
    [InlineData("MUDUR", true)]
    [InlineData("MUHASEBE", true)]
    [InlineData("TEZGAHTAR", false)]
    [InlineData("UYDURMA_ROL", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Tum_subeler_ayricaligi_tek_kaynaktan_ve_bilinmeyen_rol_kapali(string? rol, bool beklenen)
        => Assert.Equal(beklenen, SubeYetkisi.TumSubeler(rol));

    /// <summary>
    /// sube_id taşıyan HER varlık ISubeVarlik uygulamalı; uygulamayan yalnız
    /// DbContext'teki gerekçeli beyaz listede olabilir. (Aynı kural uygulama
    /// açılışında da denetlenir; bu test onu derleme hattına taşır.)
    /// </summary>
    [Fact]
    public void Sube_id_tasiyan_her_varlik_ISubeVarlik_uygular()
    {
        var eksik = typeof(KuyumcuDbContext).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IKiraciVarlik).IsAssignableFrom(t)
                        && t.GetProperty("SubeId") is not null
                        && !typeof(ISubeVarlik).IsAssignableFrom(t)
                        && t != typeof(Kullanici))
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(eksik);
    }

    /// <summary>Model önbellek anahtarı şube süzgecinin durumunu AYIRMALI.</summary>
    [Fact]
    public void Model_onbellek_anahtari_sube_suzgecini_ayirir()
    {
        var secenekler = new DbContextOptionsBuilder<KuyumcuDbContext>()
            .UseMySql("server=localhost;database=yok;user=x;password=y", ServerVersion.Parse("8.4.0-mysql"))
            .Options;
        var sifreleyici = TestVeritabani.TestSifreleyici;

        using var suzgecsiz = new KuyumcuDbContext(secenekler, new SabitKiraciBaglami("t1"), sifreleyici);
        using var suzgecli = new KuyumcuDbContext(
            secenekler, new SabitKiraciBaglami("t1", "s1", tumSubeler: false), sifreleyici);

        var fabrika = new KiraciModelOnbellegi();
        Assert.NotEqual(fabrika.Create(suzgecsiz, false), fabrika.Create(suzgecli, false));
    }
}
