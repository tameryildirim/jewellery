using System.Net;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Abonelik;
using Kuyumcu.Api.Modules.Sistem;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-27 — F-1 ABONELİK + F-3 PLAN KAPISI (kimlik: R-1)
//
// DoD'un üç cümlesi, üç test ailesi:
//   (1) "Plan değişimi yetki kapısından geçer, kiracı kendi planını
//        YÜKSELTEMEZ"                       → Yukseltme_* testleri
//   (2) "plan değişimi CANLI DENETİM İZİ üretir ve bu izin varlığı
//        kanıtlanır"                         → Iz_* testleri
//   (3) "abonelik durumu ürün davranışını FAIL-CLOSED etkiler (süresi dolan
//        kiracı yazma yapamaz, VERİ KAYBETMEZ)" → FailClosed_* testleri
//
// NEDEN GERÇEK HTTP: yetki kapısı bir DENETLEYİCİ niteliğidir ([RolGerekli],
// SistemAnahtariKapisi, KiraciDurumKapisi) — servis testi onları GÖRMEZ.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P27AbonelikTestleri(HttpFikstur fikstur)
{
    /// <summary>
    /// Kiracı yolunu sınayan servis örneği.
    /// <para>⚠ Sistem yolu AYRI SINIFTADIR (AbonelikSistemServisi): kiracı
    /// servisi SistemKapsami'ni hiç görmez, bu yüzden burada da verilmez.</para>
    /// </summary>
    public static AbonelikServisi KiraciServisi(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()), new KiraciDurumOnbellegi());

    private Kiraci Kur(string ad)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"P27-{ad}-{Guid.NewGuid():N}"[..24]);
    }

    private string PlanOku(string tenantId)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return db.Tenants.AsNoTracking().Single(t => t.Id == tenantId).Plan;
    }

    private List<KiraciAbonelik> Donemler(string tenantId)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return [.. db.Set<KiraciAbonelik>().AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Olusturuldu)];
    }

    private int DenetimSay(string tenantId, string parca)
    {
        using var db = fikstur.Vt.OlusturSistem();
        return db.DenetimKayitlari.AsNoTracking()
            .Count(d => d.TenantId == tenantId && d.Detay != null && d.Detay.Contains(parca));
    }

    /// <summary>Kiracıya SÜRESİ DOLMUŞ bir abonelik dönemi yazar (fail-closed fikstürü).</summary>
    private void SuresiDolmusDonemYaz(string tenantId, DateTime bitis)
    {
        using var db = fikstur.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        db.Set<KiraciAbonelik>().Add(new KiraciAbonelik
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            PlanKodu = "PRO",
            Baslangic = bitis.AddDays(-30),
            Bitis = bitis,
            Durum = AbonelikDurumlari.Aktif,
            Kaynak = AbonelikKaynaklari.Sistem,
            Gerekce = "P27 testi: süresi dolmuş dönem fikstürü (fail-closed).",
            OperatorAdi = "test",
            Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (1) YETKİ KAPISI — KİRACI KENDİ PLANINI YÜKSELTEMEZ
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SAHİP rolü bile olsa yükseltme 403'tür: plan bir tercih değil, ücret
    /// doğuran bir HAKTIR. Plan kolonu DEĞİŞMEZ.
    /// </summary>
    [Fact]
    public async Task Yukseltme_kiraci_tarafindan_REDDEDILIR_403()
    {
        var kiraci = Kur("yuks");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PatchJson("/api/v1/admin/plan", new { plan = "KURUMSAL" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        Assert.Equal("PRO", PlanOku(kiraci.TenantId));
        Assert.Empty(Donemler(kiraci.TenantId));   // reddedilen istek DÖNEM DE AÇMAZ
    }

    /// <summary>Reddedilen yükseltme girişimi İZSİZ kalmaz (saldırı sinyali).</summary>
    [Fact]
    public async Task Yukseltme_reddi_denetim_izine_yazilir()
    {
        var kiraci = Kur("yukiz");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        await istemci.PatchJson("/api/v1/admin/plan", new { plan = "KURUMSAL" });

        Assert.Equal(1, DenetimSay(kiraci.TenantId, $"{AbonelikServisi.RedDamgasi}: PRO → KURUMSAL"));
    }

    /// <summary>Yeni ucun kendisi de aynı kapıyı taşır: /abonelik/plan yükseltmeyi reddeder.</summary>
    [Fact]
    public async Task Yukseltme_abonelik_ucunden_de_REDDEDILIR()
    {
        var kiraci = Kur("yuks2");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PatchJson("/api/v1/abonelik/plan", new { plan = "KURUMSAL" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        Assert.Equal("PRO", PlanOku(kiraci.TenantId));
    }

    /// <summary>Abonelik uçları SAHİP kapısı arkasındadır — MÜDÜR geçemez.</summary>
    [Fact]
    public async Task Abonelik_uclari_MUDURu_reddeder()
    {
        var kiraci = Kur("rol");
        var istemci = fikstur.Ortam.Istemci(kiraci, "MUDUR");

        Assert.Equal(HttpStatusCode.Forbidden, (await istemci.GetAsync("/api/v1/abonelik")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await istemci.PatchJson("/api/v1/abonelik/plan", new { plan = "BASLANGIC" })).StatusCode);
    }

    /// <summary>İNDİRME açıktır: hak daralması kiracının kendi kararıdır.</summary>
    [Fact]
    public async Task Indirme_kiraci_tarafindan_YAPILABILIR_ve_donem_acar()
    {
        var kiraci = Kur("indir");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PatchJson("/api/v1/abonelik/plan", new { plan = "BASLANGIC" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Equal("BASLANGIC", PlanOku(kiraci.TenantId));
        var donem = Assert.Single(Donemler(kiraci.TenantId));
        Assert.Equal("BASLANGIC", donem.PlanKodu);
        Assert.Equal(AbonelikDurumlari.Aktif, donem.Durum);
        Assert.Equal(AbonelikKaynaklari.Kiraci, donem.Kaynak);
    }

    /// <summary>
    /// Mevcut plan KATALOGDA YOKSA seviye kıyaslanamaz → fail-closed: değişim
    /// yükseltme sayılır ve reddedilir (sessizce geçirilmez).
    /// </summary>
    [Fact]
    public async Task Katalogda_olmayan_plandan_degisim_FAIL_CLOSED_reddedilir()
    {
        var kiraci = Kur("bilinmez");
        using (var db = fikstur.Vt.OlusturSistem())
        {
            db.Tenants.Single(t => t.Id == kiraci.TenantId).Plan = "EFSANE";
            db.SaveChanges();
        }
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PatchJson("/api/v1/abonelik/plan", new { plan = "BASLANGIC" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        Assert.Equal("EFSANE", PlanOku(kiraci.TenantId));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (2) DENETİM İZİ
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Her gerçek plan değişimi TAM BİR iz satırı üretir.</summary>
    [Fact]
    public async Task Iz_indirmede_yazilir()
    {
        var kiraci = Kur("iz");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        await istemci.PatchJson("/api/v1/abonelik/plan", new { plan = "BASLANGIC" });

        Assert.Equal(1, DenetimSay(kiraci.TenantId, $"{AbonelikServisi.IzDamgasi}: PRO → BASLANGIC"));
    }

    /// <summary>Aynı plan gönderilirse ne satır ne iz doğar (gürültü üretilmez).</summary>
    [Fact]
    public async Task Iz_ayni_plan_gonderilince_YAZILMAZ()
    {
        var kiraci = Kur("izayni");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PatchJson("/api/v1/abonelik/plan", new { plan = "PRO" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Empty(Donemler(kiraci.TenantId));
        Assert.Equal(0, DenetimSay(kiraci.TenantId, AbonelikServisi.IzDamgasi));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SİSTEM UCU — TEK YÜKSELTME YOLU
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Sistem_ucu_plani_YUKSELTIR_iz_yazar_ve_eski_donemi_KAPATIR()
    {
        var kiraci = Kur("sis");
        var kiraciIstemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");
        await kiraciIstemci.PatchJson("/api/v1/abonelik/plan", new { plan = "BASLANGIC" });

        var sistem = fikstur.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "asikin-qa");
        var bitis = DateTime.UtcNow.AddDays(30);
        var cevap = await sistem.PostJson($"/api/v1/sistem/kiracilar/{kiraci.TenantId}/abonelik",
            new { plan = "KURUMSAL", bitis, gerekce = "Pilot sözleşmesi kapsamında plan yükseltmesi." });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();
        Assert.Equal("KURUMSAL", g.GetProperty("plan").GetString());
        Assert.Equal("KURUMSAL", PlanOku(kiraci.TenantId));

        var donemler = Donemler(kiraci.TenantId);
        Assert.Equal(2, donemler.Count);
        Assert.Equal(AbonelikDurumlari.Sonlandi, donemler[0].Durum);
        Assert.NotNull(donemler[0].Bitis);                       // kapanış anı YAZILDI
        Assert.Equal(AbonelikDurumlari.Aktif, donemler[1].Durum);
        Assert.Equal(AbonelikKaynaklari.Sistem, donemler[1].Kaynak);
        Assert.Equal("asikin-qa", donemler[1].OperatorAdi);
        Assert.Equal(1, DenetimSay(kiraci.TenantId,
            $"{AbonelikServisi.IzDamgasi}: BASLANGIC → KURUMSAL"));
    }

    /// <summary>Sistem ucu JWT ile AÇILMAZ: SAHİP token'ı yetmez.</summary>
    [Fact]
    public async Task Sistem_ucu_SAHIP_jetonuyla_acilmaz()
    {
        var kiraci = Kur("sisjwt");
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PostJson($"/api/v1/sistem/kiracilar/{kiraci.TenantId}/abonelik",
            new { plan = "KURUMSAL", gerekce = "Bu istek sistem anahtarı olmadan gelmektedir." });

        Assert.True(cevap.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Beklenen 401/403, gelen {(int)cevap.StatusCode}");
        Assert.Equal("PRO", PlanOku(kiraci.TenantId));
    }

    /// <summary>Gerekçesiz (≤20 karakter) yükseltme 400'dür — kim/neden boş kalamaz.</summary>
    [Fact]
    public async Task Sistem_ucu_GEREKCESIZ_istegi_reddeder()
    {
        var kiraci = Kur("sisger");
        var sistem = fikstur.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "asikin-qa");

        var cevap = await sistem.PostJson($"/api/v1/sistem/kiracilar/{kiraci.TenantId}/abonelik",
            new { plan = "KURUMSAL", gerekce = "kısa" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("PRO", PlanOku(kiraci.TenantId));
    }

    /// <summary>Geçmiş tarihli bitiş kabul edilmez (doğarken ölü abonelik yazılmaz).</summary>
    [Fact]
    public async Task Sistem_ucu_GECMIS_bitisi_reddeder()
    {
        var kiraci = Kur("sisbit");
        var sistem = fikstur.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "asikin-qa");

        var cevap = await sistem.PostJson($"/api/v1/sistem/kiracilar/{kiraci.TenantId}/abonelik",
            new
            {
                plan = "KURUMSAL",
                bitis = DateTime.UtcNow.AddDays(-1),
                gerekce = "Geçmiş tarihli bitiş denemesi — reddedilmelidir.",
            });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("PRO", PlanOku(kiraci.TenantId));
    }

    /// <summary>
    /// Kapı SERVİSTEDİR, DTO'da değil: DataAnnotations yalnız controller
    /// yolunda koşar. Servis DOĞRUDAN çağrıldığında da gerekçesiz ve
    /// operatörsüz değişim reddedilmelidir.
    /// <para>⚠ Bu test bir MUTASYON AÇIĞINI kapatır (M27-4): servisteki
    /// <c>gerekce.Length &lt; 20</c> kapısı silindiğinde HTTP testleri yeşil
    /// kalıyordu, çünkü isteği DTO reddediyordu.</para>
    /// </summary>
    [Fact]
    public async Task Sistem_SERVISI_gerekcesiz_ve_operatorsuz_istegi_REDDEDER()
    {
        var kiraci = Kur("sissrv");
        using var kapsam = fikstur.Ortam.Services.CreateScope();
        var servis = kapsam.ServiceProvider.GetRequiredService<AbonelikSistemServisi>();

        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(
            () => servis.SistemPlanDegistir(kiraci.TenantId,
                new SistemAbonelikDto { Plan = "KURUMSAL", Gerekce = "kısa" }, "asikin-qa"));
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(
            () => servis.SistemPlanDegistir(kiraci.TenantId,
                new SistemAbonelikDto
                {
                    Plan = "KURUMSAL",
                    Gerekce = "Operatör kimliği olmadan yapılan yükseltme denemesi.",
                }, "ab"));

        Assert.Equal("PRO", PlanOku(kiraci.TenantId));
        Assert.Empty(Donemler(kiraci.TenantId));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // (3) FAIL-CLOSED — SÜRESİ DOLAN KİRACI YAZAMAZ, VERİSİNİ KAYBETMEZ
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FailClosed_suresi_dolan_kiraci_YAZAMAZ()
    {
        var kiraci = Kur("fc1");
        SuresiDolmusDonemYaz(kiraci.TenantId, DateTime.UtcNow.AddDays(-1));
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        var cevap = await istemci.PostJson("/api/v1/cari",
            new { ad = "Abonelik Testi", tur = "MUSTERI" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        var g = await cevap.Json();
        // Mesaj DENEME değil ABONELİK dilindedir (UX-S31 · Anayasa md.1).
        Assert.Contains("Abonelik", g.GetProperty("message").GetString());
    }

    [Fact]
    public async Task FailClosed_suresi_dolan_kiraci_OKUYABILIR_veri_rehin_alinmaz()
    {
        var kiraci = Kur("fc2");
        SuresiDolmusDonemYaz(kiraci.TenantId, DateTime.UtcNow.AddDays(-1));
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");

        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync("/api/v1/cari")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await istemci.GetAsync("/api/v1/abonelik")).StatusCode);
        // Kayıtlar DURUYOR: kiracının satırları silinmedi.
        using var db = fikstur.Vt.OlusturSistem();
        Assert.True(db.Tenants.AsNoTracking().Any(t => t.Id == kiraci.TenantId));
    }

    [Fact]
    public async Task FailClosed_operator_donemi_uzatinca_yazma_GERI_ACILIR()
    {
        var kiraci = Kur("fc3");
        SuresiDolmusDonemYaz(kiraci.TenantId, DateTime.UtcNow.AddDays(-1));
        var istemci = fikstur.Ortam.Istemci(kiraci, "SAHIP");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await istemci.PostJson("/api/v1/cari", new { ad = "Kapali", tur = "MUSTERI" })).StatusCode);

        var sistem = fikstur.Ortam.SistemIstemci(HttpOrtami.TestSistemAnahtari, "asikin-qa");
        var uzat = await sistem.PostJson($"/api/v1/sistem/kiracilar/{kiraci.TenantId}/abonelik",
            new
            {
                plan = "PRO",
                bitis = DateTime.UtcNow.AddDays(30),
                gerekce = "Abonelik yenilendi; yazma yetkisi geri açılmalıdır.",
            });
        Assert.Equal(HttpStatusCode.OK, uzat.StatusCode);

        // Önbellek AÇIKÇA boşaltıldığı için uzatma GECİKMELİ etki etmez.
        var sonra = await istemci.PostJson("/api/v1/cari", new { ad = "Acildi", tur = "MUSTERI" });
        Assert.Equal(HttpStatusCode.Created, sonra.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SAF KURAL TESTLERİ (veritabanısız)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Durum_abonelik_bitisi_gecmisse_YAZAMAZ()
    {
        var now = new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);
        var kapali = new KiraciDurumu(KiraciDurumlari.Aktif, null, now.AddSeconds(-1));
        var acik = new KiraciDurumu(KiraciDurumlari.Aktif, null, now.AddDays(1));
        var suresiz = new KiraciDurumu(KiraciDurumlari.Aktif, null, null);

        Assert.False(kapali.YazabilirMi(now));
        Assert.True(acik.YazabilirMi(now));
        Assert.True(suresiz.YazabilirMi(now));
        Assert.Contains("Abonelik", kapali.RedSebebi(now));
    }

    /// <summary>
    /// SALT_OKUNUR kiracıda gerekçe ABONELİK mesajına dönüşmez: iki farklı
    /// sebep iki farklı cümledir (UX-S31).
    /// </summary>
    [Fact]
    public void Durum_salt_okunur_gerekcesi_ABONELIGE_donusmez()
    {
        var now = DateTime.UtcNow;
        var d = new KiraciDurumu(KiraciDurumlari.SaltOkunur, null, now.AddSeconds(-1));

        Assert.False(d.YazabilirMi(now));
        Assert.Contains("SALT OKUNUR", d.RedSebebi(now));
    }

    /// <summary>
    /// İstemci sözleşmesi sunucu DTO'suyla AD ve TİP olarak eşleşir
    /// (PLAN § 1 md.9 — decimal→int gibi sessiz daralma olamaz).
    /// </summary>
    [Fact]
    public void Sozlesme_istemci_ve_sunucu_alanlari_AD_ve_TIP_olarak_eslesir()
    {
        static Dictionary<string, string> Alanlar(Type t) =>
            t.GetProperties().ToDictionary(p => p.Name, p => p.PropertyType.Name);

        var sunucu = Alanlar(typeof(AbonelikDurumDto));
        var istemci = Alanlar(typeof(Kuyumcu.Contracts.Sayfalar.Isletme.AbonelikVeri));

        foreach (var (ad, tip) in istemci)
        {
            Assert.True(sunucu.ContainsKey(ad), $"Sunucuda olmayan istemci alanı: {ad}");
            // Koleksiyon ve kayıt tipleri ad olarak farklıdır (AbonelikPlanDto ↔
            // AbonelikPlanVeri); yalnız İLKEL alanlarda tip birebir aranır.
            if (tip is "String" or "Int32" or "Nullable`1" or "Boolean")
                Assert.Equal(sunucu[ad], tip);
        }
    }

    /// <summary>Migration tohumu: katalog üç planla ve ARTAN seviyelerle doğar.</summary>
    [Fact]
    public void Katalog_migration_tohumuyla_UC_planla_dogar()
    {
        using var db = fikstur.Vt.OlusturSistem();
        var planlar = db.Set<AbonelikPlani>().AsNoTracking().OrderBy(p => p.Seviye).ToList();

        Assert.Equal(["BASLANGIC", "PRO", "KURUMSAL"], planlar.Select(p => p.Kod));
        Assert.Equal(planlar.Select(p => p.Seviye).Distinct().Count(), planlar.Count);
        Assert.All(planlar, p => Assert.True(p.Aktif));
    }
}
