using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-12 — DEFTER TARAFI DÜZENLENEBİLİRLİK + STORNO GÖRÜNÜRLÜĞÜ
// [HTTP, gerçek MySQL] · Kimlikler: B-41 · B-42 · B-43
//
// SÖZLEŞME (PLAN § 2 P-12 DoD):
//   • Finansal alan (tutar · has gram · ayar · yön) PATCH ile DEĞİŞMEZ;
//     tek düzeltme yolu STORNO + yeni kayıttır.
//   • Açıklama / karşı taraf / tarih düzenlenebilir olur.
//   • DenetimIzi her değişikliği ESKİ → YENİ değerle yazar.
//
// ⚠ Bu dosyadaki her test BİR İŞ KURALINI yeniden üretir; kural bozulduğunda
//   ADIYLA kırmızıya döner (mutasyon turu raporda).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P12DefterDuzenlenebilirlikTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    // ── yardımcılar ──────────────────────────────────────────────────────────

    private async Task<string> KasaHareketiGir(Kiraci k, string aciklama = "Kira ödemesi")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/kasalar/{k.KasaId}/hareket",
            new { yon = "GIRIS", tutar = 1500m, odemeTuru = "NAKIT", aciklama });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        return await db.KasaHareketleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.KasaId == k.KasaId && x.Aciklama == aciklama)
            .OrderByDescending(x => x.Olusturuldu).Select(x => x.Id).FirstAsync();
    }

    private async Task<(string HesapId, string HareketId)> BankaDekontuGir(
        Kiraci k, decimal tutar = 2500m, string aciklama = "Havale geldi")
    {
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var hesap = await istemci.PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ana Hesap",
            iban = TestIban.Tamamla(new string('1', 22)), paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.OK, hesap.StatusCode);
        var hesapId = (await hesap.Json()).GetProperty("id").GetString()!;

        var dekont = await istemci.PostJson($"/api/v1/bankalar/{hesapId}/hareket", new
        {
            yon = "GELEN", tutar, karsiTarafAd = "Ali Veli", aciklama,
        });
        Assert.Equal(HttpStatusCode.OK, dekont.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var id = await db.BankaHareketleri.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.BankaHesapId == hesapId)
            .OrderByDescending(x => x.Olusturuldu).Select(x => x.Id).FirstAsync();
        return (hesapId, id);
    }

    private async Task<string> EmanetGirisi(Kiraci k, string aciklama = "Bilezik emaneti")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/emanet/giris", new
        {
            cariId = k.CariId, ayarId = "K22", gramAgirlik = 10m,
            aciklama, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        return await db.Hareketler.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.Tur == "EMANET_GIRIS")
            .OrderByDescending(x => x.Olusturuldu).Select(x => x.Id).FirstAsync();
    }

    private const string Gerekce = "Musteri yanlis tutar soyledi, duzeltiliyor.";

    // ═════════════════════════════════════════════════════════════════════════
    // B-41 — KASA HAREKETİ DÜZENLENEBİLİR
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task B41_kasa_hareketinin_aciklamasi_DUZELTILIR()
    {
        var k = YeniKiraci("P12 kasa aciklama");
        var id = await KasaHareketiGir(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket/{id}",
                new { aciklama = "Dukkan kirasi — Eylul" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var h = await db.KasaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("Dukkan kirasi — Eylul", h.Aciklama);
    }

    /// <summary>
    /// PATCH SÖZLEŞMESİ: gövdede olmayan alan "değişmedi" demektir. Yalnız
    /// tarih gönderildiğinde açıklama SİLİNMEZ (veri imhası deseni).
    /// </summary>
    [Fact]
    public async Task B41_govdede_olmayan_alan_KORUNUR()
    {
        var k = YeniKiraci("P12 kasa patch korur");
        var id = await KasaHareketiGir(k, "Ilk aciklama");

        var yeniTarih = DateTime.UtcNow.AddHours(-3);
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket/{id}", new { tarih = yeniTarih });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var h = await db.KasaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("Ilk aciklama", h.Aciklama);
    }

    /// <summary>null gönderilen açıklama SİLİNİR (üç durumlu sözleşmenin 3. hâli).</summary>
    [Fact]
    public async Task B41_null_aciklama_SILINIR()
    {
        var k = YeniKiraci("P12 kasa null siler");
        var id = await KasaHareketiGir(k, "Silinecek not");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/kasalar/{k.KasaId}/hareket/{id}", new { aciklama = (string?)null });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Null((await db.KasaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id)).Aciklama);
    }

    /// <summary>Hiç alan gönderilmeyen PATCH sessizce başarılı olmaz — 400 (UX-S27).</summary>
    [Fact]
    public async Task B41_bos_govde_SESSIZ_GECMEZ()
    {
        var k = YeniKiraci("P12 kasa bos govde");
        var id = await KasaHareketiGir(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket/{id}", new { });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    /// <summary>
    /// STORNO GÖRÜNÜRLÜĞÜNÜN ÖTEKİ YÜZÜ: hükümsüz kılınmış kaydın bilgisi
    /// düzeltilmez — storno satırının açıklaması GEREKÇEDİR.
    /// </summary>
    [Fact]
    public async Task B41_stornolanmis_kasa_hareketi_DUZELTILEMEZ()
    {
        var k = YeniKiraci("P12 kasa storno kilit");
        var id = await KasaHareketiGir(k);
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var storno = await istemci.PostJson(
            $"/api/v1/kasalar/{k.KasaId}/hareket/{id}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, storno.StatusCode);

        var cevap = await istemci.PatchAsJsonAsync(
            $"/api/v1/kasalar/{k.KasaId}/hareket/{id}", new { aciklama = "sonradan degistir" });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
    }

    /// <summary>Defter fişinin kasa bacağının TARİHİ bacaktan kaydırılamaz.</summary>
    [Fact]
    public async Task B41_defter_bacaginin_tarihi_DEGISTIRILEMEZ()
    {
        var k = YeniKiraci("P12 kasa defter bacagi");
        using (var db = f.Vt.Olustur(k))
        {
            db.KasaHareketleri.Add(new KasaHareket
            {
                Id = Guid.NewGuid().ToString("N")[..26].ToUpperInvariant(),
                TenantId = k.TenantId, KasaId = k.KasaId,
                HareketId = Guid.NewGuid().ToString("N")[..26].ToUpperInvariant(),
                Tutar = 100m, ParaBirimi = "TRY", OdemeTuru = "NAKIT",
                Aciklama = "fis bacagi", Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        string id;
        using (var db = f.Vt.OlusturSistem())
            id = await db.KasaHareketleri.AsNoTracking()
                .Where(x => x.TenantId == k.TenantId && x.Aciklama == "fis bacagi")
                .Select(x => x.Id).SingleAsync();

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/kasalar/{k.KasaId}/hareket/{id}", new { tarih = DateTime.UtcNow.AddDays(-2) });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
    }

    /// <summary>
    /// SÖZLEŞME KİLİDİ (mekanizma): düzeltme DTO'su FİNANSAL ALAN TAŞIMAZ.
    /// Bir sonraki oturum "tutar da düzeltilsin" diye alan eklerse bu test
    /// ADIYLA kırmızıya döner — `STORNO_DISI_FINANSAL_PATCH` sayacının
    /// derleme içi karşılığıdır.
    /// </summary>
    [Fact]
    public void Duzeltme_DTOlari_FINANSAL_ALAN_TASIMAZ()
    {
        string[] yasak =
        [
            "Tutar", "Yon", "OdemeTuru", "KomisyonOran", "ParaBirimi",
            "HasGrami", "AyarId", "GramAgirlik", "Maden", "KasaId",
            "BankaHesapId", "CariId", "Bakiye",
        ];

        Type[] dtolar =
        [
            typeof(Kuyumcu.Api.Modules.Kasa.KasaHareketDuzeltDto),
            typeof(Kuyumcu.Api.Modules.Banka.HareketDuzeltDto),
            typeof(Kuyumcu.Api.Modules.Emanet.EmanetHareketDuzeltDto),
        ];

        foreach (var t in dtolar)
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            Assert.DoesNotContain(p.Name, yasak);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B-42 — BANKA DEKONTU: DÜZELTİLEBİLİR **VE** STORNOLANABİLİR
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task B42_dekontun_karsi_tarafi_ve_aciklamasi_DUZELTILIR()
    {
        var k = YeniKiraci("P12 banka duzelt");
        var (hesapId, id) = await BankaDekontuGir(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}",
            new { karsiTarafAd = "Veli Ali", aciklama = "Kira tahsilati" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var h = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("Veli Ali", h.KarsiTarafAd);
        Assert.Equal("Kira tahsilati", h.Aciklama);
    }

    /// <summary>Düzeltme ucu TUTARA DOKUNMAZ: gövdeye tutar konsa bile değişmez.</summary>
    [Fact]
    public async Task B42_dekont_PATCHi_TUTARI_DEGISTIRMEZ()
    {
        var k = YeniKiraci("P12 banka tutar kilit");
        var (hesapId, id) = await BankaDekontuGir(k, tutar: 2500m);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}",
            new { aciklama = "not", tutar = 999999m, yon = "GIDEN" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var h = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(2500m, h.Tutar);
        Assert.Equal("GELEN", h.Yon);
    }

    /// <summary>
    /// B-42'nin ASIL borcu: dekont artık STORNOLANIR. Ters satır KARŞI yönde,
    /// AYNI tutarla doğar; kaynak satıra DOKUNULMAZ (ADR-0004).
    /// </summary>
    [Fact]
    public async Task B42_dekont_STORNOLANIR_ve_ters_satir_dogar()
    {
        var k = YeniKiraci("P12 banka storno");
        var (hesapId, id) = await BankaDekontuGir(k, tutar: 4000m);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}/storno", new { gerekce = Gerekce });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var govde = await cevap.Json();
        Assert.Equal(id, govde.GetProperty("kaynakBankaHareketId").GetString());

        using var db = f.Vt.OlusturSistem();
        var ters = await db.BankaHareketleri.AsNoTracking()
            .SingleAsync(x => x.TenantId == k.TenantId && x.StornoKaynakId == id);
        Assert.Equal("GIDEN", ters.Yon);
        Assert.Equal(4000m, ters.Tutar);

        var kaynak = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(4000m, kaynak.Tutar);          // kaynak satır DEĞİŞMEDİ
        Assert.Equal("GELEN", kaynak.Yon);
    }

    /// <summary>Çift storno YASAK — ikinci deneme 409.</summary>
    [Fact]
    public async Task B42_dekont_IKINCI_KEZ_stornolanamaz()
    {
        var k = YeniKiraci("P12 banka cift storno");
        var (hesapId, id) = await BankaDekontuGir(k);
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var ilk = await istemci.PostJson(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);

        var ikinci = await istemci.PostJson(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
    }

    /// <summary>Gerekçe 20 karakterden kısaysa storno yazılmaz (servis içi kapı).</summary>
    [Fact]
    public async Task B42_kisa_gerekceyle_storno_REDDEDILIR()
    {
        var k = YeniKiraci("P12 banka kisa gerekce");
        var (hesapId, id) = await BankaDekontuGir(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/bankalar/{hesapId}/hareketler/{id}/storno", new { gerekce = "yanlis" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.BankaHareketleri.AsNoTracking()
            .AnyAsync(x => x.TenantId == k.TenantId && x.StornoKaynakId == id));
    }

    /// <summary>
    /// STORNO ÇİFTİ MASAK EŞİĞİNE SAYILMAZ (B-42'nin görünmeyen yüzü).
    /// Dekont stornolanabilir olur olmaz, hükümsüz kılınmış bir para hareketi
    /// müşterinin günlük kümülatifini İKİNCİ KEZ doldurmaya başlardı: 100.000
    /// TL'lik bir hatayı düzelten kuyumcu, aynı gün doğru dekontu girmek
    /// istediğinde 185.000 TL eşiğine takılır ve hatasını DÜZELTEMEZDİ.
    /// Süzgeç: StornoSuzgeci.StornoCiftiniDisla(IQueryable&lt;BankaHareket&gt;).
    /// </summary>
    [Fact]
    public async Task B42_storno_cifti_MASAK_esigine_SAYILMAZ()
    {
        var k = YeniKiraci("P12 banka masak suzgec");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var hesap = await istemci.PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ana Hesap",
            iban = TestIban.Tamamla(new string('2', 22)), paraBirimi = "TRY",
        });
        var hesapId = (await hesap.Json()).GetProperty("id").GetString()!;

        // 1) 100.000 TL dekont (eşik altı — MASAK alanı istenmez)
        var ilk = await istemci.PostJson($"/api/v1/bankalar/{hesapId}/hareket",
            new { yon = "GELEN", tutar = 100_000m, cariId = k.CariId, aciklama = "ilk" });
        Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);

        string ilkId;
        using (var db = f.Vt.OlusturSistem())
            ilkId = await db.BankaHareketleri.AsNoTracking()
                .Where(x => x.TenantId == k.TenantId && x.BankaHesapId == hesapId)
                .OrderByDescending(x => x.Olusturuldu).Select(x => x.Id).FirstAsync();

        // 2) hatalı dekont STORNO edilir
        var storno = await istemci.PostJson(
            $"/api/v1/bankalar/{hesapId}/hareketler/{ilkId}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.OK, storno.StatusCode);

        // 3) doğru dekont aynı gün girilir — süzgeç olmasaydı kümülatif
        //    300.000 TL sayılır ve istek MASAK alanları istenerek REDDEDİLİRDİ.
        var ikinci = await istemci.PostJson($"/api/v1/bankalar/{hesapId}/hareket",
            new { yon = "GELEN", tutar = 100_000m, cariId = k.CariId, aciklama = "duzeltilmis" });

        Assert.Equal(HttpStatusCode.OK, ikinci.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B-43 — EMANET HAREKETİ DÜZENLENEBİLİR
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task B43_emanet_hareketinin_aciklamasi_DUZELTILIR()
    {
        var k = YeniKiraci("P12 emanet duzelt");
        var id = await EmanetGirisi(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/emanet/hareketler/{id}", new { aciklama = "22 ayar bilezik" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal("22 ayar bilezik",
            (await db.Hareketler.AsNoTracking().SingleAsync(x => x.Id == id)).Aciklama);
    }

    /// <summary>Emanet düzeltmesi HAS GRAMA dokunmaz (bakiyeyi o oluşturur).</summary>
    [Fact]
    public async Task B43_emanet_PATCHi_HAS_GRAMI_DEGISTIRMEZ()
    {
        var k = YeniKiraci("P12 emanet has kilit");
        var id = await EmanetGirisi(k);

        decimal once;
        using (var db = f.Vt.OlusturSistem())
            once = (await db.Hareketler.AsNoTracking().SingleAsync(x => x.Id == id)).HasGrami;

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/emanet/hareketler/{id}", new { aciklama = "not", hasGrami = 999m });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(once, (await db2.Hareketler.AsNoTracking().SingleAsync(x => x.Id == id)).HasGrami);
    }

    /// <summary>Emanet ucu DEFTERİN tamamına açılmaz: başka tür 409.</summary>
    [Fact]
    public async Task B43_emanet_ucu_BASKA_TURU_duzeltemez()
    {
        var k = YeniKiraci("P12 emanet tur kapisi");
        string id;
        using (var db = f.Vt.Olustur(k))
        {
            id = Guid.NewGuid().ToString("N")[..26].ToUpperInvariant();
            db.Hareketler.Add(new Hareket
            {
                Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "TAHSILAT",
                Tarih = DateTime.UtcNow, TlTutari = 500m, HasGrami = 0m,
                KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
                Olusturuldu = DateTime.UtcNow, Aciklama = "tahsilat",
            });
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/emanet/hareketler/{id}", new { aciklama = "degistir" });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // DENETİM İZİ — plan DoD md.4: her değişiklik ESKİ → YENİ değerle yazılır
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Duzeltme_DENETIM_IZINE_eski_ve_yeni_degerle_yazilir()
    {
        var k = YeniKiraci("P12 denetim izi");
        var id = await KasaHareketiGir(k, "Eski not");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            $"/api/v1/kasalar/{k.KasaId}/hareket/{id}", new { aciklama = "Yeni not" });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var kayit = await db.DenetimKayitlari.AsNoTracking()
            .Where(x => x.TenantId == k.TenantId && x.VarlikTuru == "KasaHareketiDuzeltme")
            .OrderByDescending(x => x.Tarih).FirstAsync();

        Assert.Contains("Eski not", kayit.Detay);
        Assert.Contains("Yeni not", kayit.Detay);
    }
}
