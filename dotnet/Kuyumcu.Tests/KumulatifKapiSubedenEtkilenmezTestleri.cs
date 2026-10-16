using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ŞUBE-KAPSAM-A § 2.6 / K-10 / K-11 — YASAL KÜMÜLATİF ŞUBEYE BÖLÜNMEZ
//                                           [HTTP, gerçek MySQL]
//
// Tevsik (459 s. VUK GT §4.1.1 "aynı günde aynı kişi") ve MASAK (Tedbirler Yön.
// md.5/1-b "bağlantılı işlemlerin toplam tutarı") kümülatifi MÜKELLEF/YÜKÜMLÜ
// (kiracı) genelidir; MASAK GT 34 md.3/1-c şubeyi yükümlünün parçası sayar.
//
// Hata (paket öncesi, bu dosya ağaçta koşunca KIRMIZI idi): şubeye kilitli
// TEZGÂHTAR oturumunda P-04 global şube süzgeci `db.Hareketler`'i oturum
// şubesine daraltıyordu → B şubesindeki nakit A şubesindeki kapıya görünmüyordu.
// Düzeltme: GunlukNakitToplam / GunlukIslemToplam şube süzgeci açık bağlamda
// SistemKapsami.Kiraci (madde 10) ile kiracı geneli, SALT OKUMA sorgulanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KumulatifKapiSubedenEtkilenmezTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Kiracıya ikinci (B) şube ekler.</summary>
    private string IkinciSube(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var subeId = Kimlik.Ulid();
        db.Subeler.Add(new Sube
        {
            Id = subeId, TenantId = k.TenantId, Ad = "Şube B", Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return subeId;
    }

    /// <summary>B şubesinde BUGÜN aynı cariye yapılmış bir satış satırı (başka tezgâhtarın işlemi).</summary>
    private async Task BSubesindeSatis(Kiraci k, string subeB, decimal tutar, string odemeTuru, decimal? nakit)
    {
        await using var db = f.Vt.OlusturSistem();
        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = subeB, Tur = "SATIS",
            Tarih = DateTime.UtcNow, TlTutari = tutar, HasGrami = 0m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            FisId = Kimlik.Ulid(), OdemeTuru = odemeTuru, NakitTutar = nakit,
            Aciklama = "ŞK-A B şubesi satışı", Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task<int> HareketSayisi(Kiraci k)
    {
        await using var db = f.Vt.OlusturSistem();
        return await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId);
    }

    private HttpClient TezgahtarA(Kiraci k) =>
        f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR");

    // ── K-10 (tevsik) ───────────────────────────────────────────────────────

    [Fact]
    public async Task K10_Tevsik_B_subesindeki_nakit_A_subesindeki_TEZGAHTAR_kapisina_sayilir_ikinci_satis_400()
    {
        var k = YeniKiraci("SK-A-TEST K10 tevsik");
        var subeB = IkinciSube(k);
        await BSubesindeSatis(k, subeB, 20_000m, "NAKIT", 20_000m);
        var once = await HareketSayisi(k);

        var c = await TezgahtarA(k).PostJson("/api/v1/islemler/satis", new
        {
            urunId = k.UrunId, adet = 1m, birimFiyatTl = 15_000m, odemeTuru = "NAKIT", kasaId = k.KasaId,
            cariId = k.CariId, kurSnapshotId = k.KurId,
        });

        var govde = await c.Metin();
        Assert.True(c.StatusCode == HttpStatusCode.BadRequest, $"beklenen 400, gelen {(int)c.StatusCode}: {govde}");
        Assert.Contains("kartla ya da havaleyle", govde, StringComparison.Ordinal);
        Assert.Equal(once, await HareketSayisi(k));   // yeni satır 0
    }

    [Fact]
    public async Task K10_Tevsik_onizleme_ucu_TEZGAHTAR_A_icin_B_subesi_nakdini_toplar()
    {
        var k = YeniKiraci("SK-A-TEST K10 onizleme");
        var subeB = IkinciSube(k);
        await BSubesindeSatis(k, subeB, 20_000m, "NAKIT", 20_000m);

        var c = await TezgahtarA(k).GetAsync($"/api/v1/tevsik/gunluk?cariId={k.CariId}");

        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        Assert.Equal(20_000m, (await c.Json()).GetProperty("bugunkuNakitToplam").GetDecimal());
    }

    /// <summary>Kontrol: eşik altı toplam (20.000 + 5.000) A şubesinde GEÇER — kapı şubesiz bloklamıyor.</summary>
    [Fact]
    public async Task K10_Kontrol_esik_alti_kumulatifte_A_subesi_satisi_gecer()
    {
        var k = YeniKiraci("SK-A-TEST K10 kontrol");
        var subeB = IkinciSube(k);
        await BSubesindeSatis(k, subeB, 20_000m, "NAKIT", 20_000m);

        var c = await TezgahtarA(k).PostJson("/api/v1/islemler/satis", new
        {
            urunId = k.UrunId, adet = 1m, birimFiyatTl = 5_000m, odemeTuru = "NAKIT", kasaId = k.KasaId,
            cariId = k.CariId, kurSnapshotId = k.KurId,
        });

        Assert.True(c.IsSuccessStatusCode, await c.Metin());
    }

    // ── K-10 (MASAK) ────────────────────────────────────────────────────────

    [Fact]
    public async Task K10_Masak_B_subesi_100bin_arti_A_subesi_100bin_kimliksiz_400()
    {
        var k = YeniKiraci("SK-A-TEST K10 masak");
        var subeB = IkinciSube(k);
        await BSubesindeSatis(k, subeB, 100_000m, "VERESIYE", 0m);
        var once = await HareketSayisi(k);

        var c = await TezgahtarA(k).PostJson("/api/v1/islemler/satis", new
        {
            // MASAK-370: 100.000 + 100.000 = 200.000 — bugün kimliği fiilen tetikleyen en düşük tutar
            // (amaç beyanı, TEYİTSİZ); eski hâli 90.000 → 190.000 ≥ 185.000 idi.
            urunId = k.UrunId, adet = 1m, birimFiyatTl = 100_000m, odemeTuru = "VERESIYE",
            cariId = k.CariId, kurSnapshotId = k.KurId,
        });

        var govde = await c.Metin();
        Assert.True(c.StatusCode == HttpStatusCode.BadRequest, $"beklenen 400, gelen {(int)c.StatusCode}: {govde}");
        Assert.Contains("MASAK", govde, StringComparison.Ordinal);
        Assert.Equal(once, await HareketSayisi(k));
    }

    // ── K-11 — görünüm şubesi kapıya SIZMAZ ────────────────────────────────

    [Fact]
    public async Task K11_SAHIP_gorunum_subesi_Merkez_iken_tevsik_kumulatifi_yine_kiraci_geneli()
    {
        var k = YeniKiraci("SK-A-TEST K11");
        var subeB = IkinciSube(k);
        await BSubesindeSatis(k, subeB, 20_000m, "NAKIT", 20_000m);

        // SAHİP "Merkez"i seçmiş: istemci subeId gönderse bile kiracı geneli uç yok sayar.
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/tevsik/gunluk?cariId={k.CariId}&subeId={k.SubeId}");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        Assert.Equal(20_000m, (await c.Json()).GetProperty("bugunkuNakitToplam").GetDecimal());

        // Ve yazma kapısı da: SAHİP Merkez'de 15.000 nakit → 400.
        var s = await f.Ortam.Istemci(k, "SAHIP").PostJson($"/api/v1/islemler/satis?subeId={k.SubeId}", new
        {
            urunId = k.UrunId, adet = 1m, birimFiyatTl = 15_000m, odemeTuru = "NAKIT", kasaId = k.KasaId,
            cariId = k.CariId, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, s.StatusCode);
        Assert.Contains("kartla ya da havaleyle", await s.Metin(), StringComparison.Ordinal);
    }

    // ── Güvenlik (Mehmet koşulu): kiracı-geneli okuma başka kiracıya AÇILMAZ ──

    [Fact]
    public async Task Kiraci_geneli_okuma_baska_kiracinin_satirini_saymaz()
    {
        var k = YeniKiraci("SK-A-TEST izolasyon 1");
        var diger = YeniKiraci("SK-A-TEST izolasyon 2");
        var digerB = IkinciSube(diger);
        // Başka kiracıda AYNI cari kimliğiyle satır yazılamaz (FK); yine de diğer kiracıda
        // büyük nakit var — bizim kiracının toplamına girmemeli.
        await BSubesindeSatis(diger, digerB, 25_000m, "NAKIT", 25_000m);

        var c = await TezgahtarA(k).GetAsync($"/api/v1/tevsik/gunluk?cariId={k.CariId}");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        Assert.Equal(0m, (await c.Json()).GetProperty("bugunkuNakitToplam").GetDecimal());
    }
    // ── Doğrulayıcı bulgusu (tur 2): ORTAM TRANSACTION'I içindeki commit edilmemiş satır ──
    // Şubeye kilitli bağlamda okuyucu ayrı bağlantı açsaydı, aynı transaction'da
    // kapıdan ÖNCE yazılmış nakit satırını görmezdi (fail-open). Okuyucu ortam
    // transaction'ına katılır: B şubesi 20.000 (commit) + A şubesi 7.000 (commit
    // EDİLMEMİŞ, aynı tx) = 27.000; geri alınınca 20.000.

    [Fact]
    public async Task Ortam_transactioni_icinde_commit_edilmemis_nakit_kilitli_rolde_kumulatife_sayilir()
    {
        var k = YeniKiraci("SK-A-TEST ortam tx");
        var subeB = IkinciSube(k);
        await BSubesindeSatis(k, subeB, 20_000m, "NAKIT", 20_000m);

        await using var kapsam = f.Ortam.Services.CreateAsyncScope();
        kapsam.ServiceProvider.GetRequiredService<KapsamBaglamAyari>()
            .Ayarla(new SabitKiraciBaglami(k.TenantId, k.SubeId, tumSubeler: false));
        var db = kapsam.ServiceProvider.GetRequiredService<KuyumcuDbContext>();

        // Ön koşul: bağlam gerçekten şubeye kilitli (B şubesinin satırı doğrudan görünmez).
        Assert.Equal(0, await db.Hareketler.CountAsync(h => h.SubeId == subeB));

        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            db.Hareketler.Add(new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS",
                Tarih = DateTime.UtcNow, TlTutari = 7_000m, HasGrami = 0m, ParaBirimi = "TRY",
                KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
                FisId = Kimlik.Ulid(), OdemeTuru = "NAKIT", NakitTutar = 7_000m,
                Aciklama = "ŞK-A ortam tx satırı", Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var icerde = await Kuyumcu.Api.Modules.Tevsik.TevsikKapisi.GunlukNakitToplam(
                db, k.TenantId, k.CariId, DateTime.UtcNow);
            Assert.Equal(27_000m, icerde);

            await tx.RollbackAsync();
        }

        db.ChangeTracker.Clear();
        var sonra = await Kuyumcu.Api.Modules.Tevsik.TevsikKapisi.GunlukNakitToplam(
            db, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.Equal(20_000m, sonra);
    }
}
