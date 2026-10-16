using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3b — DÜZELTİLEBİLİRLİK (HTTP kapısı).
//
// KURUL TUR 3 / ENGEL 1 (KRİTİK): "25 veri girişi düzeltilemiyor; gider
// pusulası ve kasa hareketinde storno yolu hiç yok."
//
// Bu dosya ÜÇ düzeltme yolunu gerçek HTTP isteğiyle kanıtlar:
//   1. elle girilen KASA HAREKETİ  → gerekçeli ters kayıt (tek motor)
//   2. MANUEL KUR kaydı            → gerekçeli iptal (geçersiz damgası)
//   3. GİDER PUSULASI              → VUK md.234 iptal (+ yeni pusula yolu)
// ve bir düzeltme yolunu daha: pasif ŞUBENİN aktife alınması (PATCH aktif).
//
// [RolGerekli] birer DENETLEYİCİ niteliğidir; servis testi onları GÖRMEZ.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class S3bDuzeltilebilirlikHttpTestleri(HttpFikstur f)
{
    private const string Gerekce = "Kira ödemesi yanlış kasaya ve yanlış yönde girildi";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Elle kasa hareketi girer; oluşan kasa_hareketleri satırının kimliğini döner.</summary>
    private async Task<string> ElleHareket(Kiraci k, string yon, decimal tutar, string? aciklama)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/kasalar/{k.KasaId}/hareket",
                new { yon, tutar, odemeTuru = "NAKIT", aciklama });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        return await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.KasaId == k.KasaId
                        && h.HareketId == null && h.StornoKaynakId == null)
            .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).FirstAsync();
    }

    // ── 1. ELLE KASA HAREKETİ ────────────────────────────────────────────────

    [Fact]
    public async Task Elle_kasa_hareketinin_aciklamasi_KAYDEDILIR()
    {
        // QA turu 3: ekran açıklamayı gönderiyor, sunucu SESSİZCE ATIYORDU.
        var k = YeniKiraci("S3b açıklama");
        var id = await ElleHareket(k, "CIKIS", 150m, "İşletme kirası — Eylül");

        using var db = f.Vt.OlusturSistem();
        var satir = await db.KasaHareketleri.AsNoTracking().FirstAsync(h => h.Id == id);
        Assert.Equal("İşletme kirası — Eylül", satir.Aciklama);

        // Geçmiş ucu da açıklamayı DÖNDÜRÜR (ekran onu basabilsin) — camelCase.
        var liste = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/kasalar/{k.KasaId}/hareketler");
        Assert.Equal(HttpStatusCode.OK, liste.StatusCode);
        var ham = await liste.Metin();
        Assert.Contains("\"aciklama\"", ham);
        // Türkçe metin JSON'da kaçışlı gelebilir; ayrıştırılarak sınanır.
        Assert.Contains("İşletme kirası — Eylül",
            System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(ham)
                .GetProperty("hareketler").EnumerateArray()
                .Select(x => x.GetProperty("aciklama").GetString() ?? "").ToList());
    }

    [Fact]
    public async Task Elle_kasa_hareketi_stornolanir_ve_bakiye_geri_doner()
    {
        var k = YeniKiraci("S3b kasa storno");
        decimal once;
        using (var db = f.Vt.OlusturSistem())
            once = await db.Kasalar.AsNoTracking().Where(x => x.Id == k.KasaId)
                .Select(x => x.Bakiye).FirstAsync();

        var id = await ElleHareket(k, "CIKIS", 150m, "Yanlış yön");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/kasalar/{k.KasaId}/hareket/{id}/storno", new { gerekce = Gerekce });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var json = await cevap.Json();
        Assert.Equal(id, json.GetProperty("kaynakKasaHareketId").GetString());
        Assert.Equal(150m, json.GetProperty("tutar").GetDecimal());   // -(-150)

        using var db2 = f.Vt.OlusturSistem();
        // Ters satır KAYNAĞA BAĞLI doğar (QA önerisi) ve kaynak satır DEĞİŞMEZ.
        var ters = await db2.KasaHareketleri.AsNoTracking()
            .SingleAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId == id);
        Assert.Equal(150m, ters.Tutar);
        Assert.StartsWith("STORNO: ", ters.Aciklama);

        var kaynak = await db2.KasaHareketleri.AsNoTracking().FirstAsync(h => h.Id == id);
        Assert.Equal(-150m, kaynak.Tutar);

        var sonra = await db2.Kasalar.AsNoTracking().Where(x => x.Id == k.KasaId)
            .Select(x => x.Bakiye).FirstAsync();
        Assert.Equal(once, sonra);
    }

    [Fact]
    public async Task Ayni_kasa_hareketi_IKINCI_KEZ_stornolanamaz_409()
    {
        var k = YeniKiraci("S3b çift storno");
        var id = await ElleHareket(k, "GIRIS", 90m, null);
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        Assert.Equal(HttpStatusCode.OK,
            (await istemci.PostJson($"/api/v1/kasalar/{k.KasaId}/hareket/{id}/storno",
                new { gerekce = Gerekce })).StatusCode);

        var ikinci = await istemci.PostJson(
            $"/api/v1/kasalar/{k.KasaId}/hareket/{id}/storno", new { gerekce = Gerekce });
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1, await db.KasaHareketleri.AsNoTracking()
            .CountAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId == id));
    }

    [Fact]
    public async Task STORNO_satirinin_KENDISI_stornolanamaz_409()
    {
        // ⚠ MUTASYON DERSİ (M-S3b-20): bu kapı hiçbir testte yoktu. Kaldırıldığında
        // ters kaydın tersi yazılabiliyor ve defterde sonsuz zincir doğuyordu.
        var k = YeniKiraci("S3b ters kaydın tersi");
        var id = await ElleHareket(k, "CIKIS", 120m, null);
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        Assert.Equal(HttpStatusCode.OK,
            (await istemci.PostJson($"/api/v1/kasalar/{k.KasaId}/hareket/{id}/storno",
                new { gerekce = Gerekce })).StatusCode);

        string tersId;
        using (var db = f.Vt.OlusturSistem())
            tersId = await db.KasaHareketleri.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.StornoKaynakId == id)
                .Select(h => h.Id).SingleAsync();

        var cevap = await istemci.PostJson(
            $"/api/v1/kasalar/{k.KasaId}/hareket/{tersId}/storno", new { gerekce = Gerekce });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        Assert.Contains("zaten bir STORNO kaydıdır",
            (await cevap.Json()).GetProperty("message").GetString());

        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(1, await db2.KasaHareketleri.AsNoTracking()
            .CountAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId != null));
    }

    [Fact]
    public async Task Gerekce_20_karakterden_kisaysa_400_ve_ters_kayit_YOK()
    {
        var k = YeniKiraci("S3b kısa gerekçe");
        var id = await ElleHareket(k, "GIRIS", 40m, null);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/kasalar/{k.KasaId}/hareket/{id}/storno", new { gerekce = "yanlış" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.KasaHareketleri.AsNoTracking()
            .AnyAsync(h => h.TenantId == k.TenantId && h.StornoKaynakId != null));
    }

    [Fact]
    public void Gerekce_kapisi_SERVIS_ICINDE_de_kapalidir_SAF()
    {
        // ⚠ MUTASYON DERSİ (M-S3b-03): HTTP testi tek başına YETMEZ — controller
        // yolundaki [MinLength(20)] de 400 döndürdüğü için servis içindeki elle
        // doğrulama kaldırılsa bile HTTP testi YEŞİL kalıyordu. DTO
        // DataAnnotations yalnız controller yolunda çalışır; servis doğrudan
        // çağrıldığında (iç çağrı, arka plan işi) kapı burada kapanmalıdır.
        var ex = Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(
            () => Kuyumcu.Api.Modules.Satis.StornoServisi.GerekceDogrula("yanlış"));
        Assert.Contains("20 karakter", ex.Message);

        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(
            () => Kuyumcu.Api.Modules.Satis.StornoServisi.GerekceDogrula(null));
        Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(
            () => Kuyumcu.Api.Modules.Satis.StornoServisi.GerekceDogrula(new string('a', 1001)));

        // Sınır: tam 20 karakter GEÇER (kırpılmış hâliyle ölçülür).
        Assert.Equal(new string('a', 20),
            Kuyumcu.Api.Modules.Satis.StornoServisi.GerekceDogrula("  " + new string('a', 20) + "  "));
    }

    [Fact]
    public async Task Defter_fisinin_kasa_bacagi_tek_basina_stornolanamaz_409()
    {
        // Fişin kasa bacağı ayrı ayrı geri alınırsa defter ile kasa ayrışır.
        var k = YeniKiraci("S3b defter bacağı");
        var iscilik = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/islemler/iscilik", new
        {
            cariId = k.CariId, iscilikTl = 500m, odemeTuru = "NAKIT",
            kasaId = k.KasaId, kurSnapshotId = k.KurId, aciklama = "İşçilik",
        });
        Assert.Equal(HttpStatusCode.OK, iscilik.StatusCode);

        string bacakId;
        using (var db = f.Vt.OlusturSistem())
            bacakId = await db.KasaHareketleri.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.HareketId != null)
                .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).FirstAsync();

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/kasalar/{k.KasaId}/hareket/{bacakId}/storno", new { gerekce = Gerekce });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        // ⚠ Türkçe gövde JSON'da kaçışlıdır — ham metinde aranmaz, AYRIŞTIRILIR.
        var mesaj = (await cevap.Json()).GetProperty("message").GetString();
        Assert.Contains("defter işleminin bacağıdır", mesaj);
    }

    [Fact]
    public async Task TEZGAHTAR_kasa_hareketi_stornolayamaz_403()
    {
        var k = YeniKiraci("S3b kasa rol");
        var id = await ElleHareket(k, "GIRIS", 60m, null);

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .PostJson($"/api/v1/kasalar/{k.KasaId}/hareket/{id}/storno", new { gerekce = Gerekce });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task Yabanci_kiracinin_kasa_hareketi_404()
    {
        var a = YeniKiraci("S3b kasa A");
        var b = YeniKiraci("S3b kasa B");
        var id = await ElleHareket(a, "GIRIS", 70m, null);

        var cevap = await f.Ortam.Istemci(b, "SAHIP")
            .PostJson($"/api/v1/kasalar/{b.KasaId}/hareket/{id}/storno", new { gerekce = Gerekce });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
    }

    // ── 2. MANUEL KUR ────────────────────────────────────────────────────────

    [Fact]
    public async Task Manuel_kur_iptal_edilir_ve_guncel_kur_ONA_DONMEZ()
    {
        var k = YeniKiraci("S3b kur iptal");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var kayit = await istemci.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 6_800m });
        Assert.Equal(HttpStatusCode.Created, kayit.StatusCode);
        var yeniKurId = (await kayit.Json()).GetProperty("id").GetString()!;

        var iptal = await istemci.PostJson(
            $"/api/v1/kur/manuel/{yeniKurId}/iptal",
            new { gerekce = "Virgül hatası: 6.800 yerine 68.000 girildi" });
        Assert.Equal(HttpStatusCode.OK, iptal.StatusCode);
        Assert.True((await iptal.Json()).GetProperty("iptalEdildi").GetBoolean());

        // Satır SİLİNMEZ (geçmişte 'geçersiz' rozetiyle kalır).
        using var db = f.Vt.OlusturSistem();
        var s = await db.KurSnapshots.AsNoTracking().FirstAsync(x => x.Id == yeniKurId);
        Assert.True(s.IptalEdildi);
        Assert.Equal(6_800m, s.AltinHasFiyati);

        // Güncel kur artık iptal edilene DÖNMEZ.
        var guncel = await istemci.GetAsync("/api/v1/kur/guncel");
        Assert.Equal(HttpStatusCode.OK, guncel.StatusCode);
        Assert.NotEqual(yeniKurId, (await guncel.Json()).GetProperty("id").GetString());
    }

    [Fact]
    public async Task Kura_bagli_islem_varsa_iptal_409_ve_kayit_GECERLI_kalir()
    {
        // Fail-closed: geçmiş bir işlemin kuru, o işlem dururken geçersiz olamaz.
        // ⚠ MUTASYON DERSİ (M-S3b-06): test önce "iki kez iptal dene" biçimindeydi;
        // ikinci çağrı zaten "zaten iptal edilmiş" 409'u alıyordu, bu yüzden BAĞLI
        // İŞLEM kapısı kaldırılsa bile test YEŞİL kalıyordu. Artık iptal YALNIZ BİR
        // KEZ ve İŞLEM YAZILDIKTAN SONRA denenir; mesaj da AYRIŞTIRILARAK sınanır.
        var k = YeniKiraci("S3b kur bağlı");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var kayit = await istemci.PostJson("/api/v1/kur/manuel", new { altinHasFiyati = 7_100m });
        Assert.Equal(HttpStatusCode.Created, kayit.StatusCode);
        var kurId = (await kayit.Json()).GetProperty("id").GetString()!;

        var iscilik = await istemci.PostJson("/api/v1/islemler/iscilik", new
        {
            cariId = k.CariId, iscilikTl = 100m, odemeTuru = "NAKIT",
            kasaId = k.KasaId, kurSnapshotId = kurId, aciklama = "İşçilik",
        });
        Assert.Equal(HttpStatusCode.OK, iscilik.StatusCode);

        var cevap = await istemci.PostJson($"/api/v1/kur/manuel/{kurId}/iptal",
            new { gerekce = "Bu kur kaydı hatalı girilmiş, düzeltilmesi gerekiyor" });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        var mesaj = (await cevap.Json()).GetProperty("message").GetString() ?? "";
        Assert.Contains("bağlı", mesaj);
        Assert.Contains("storno", mesaj, StringComparison.OrdinalIgnoreCase);

        // Kayıt GEÇERLİ kaldı (iptal yazılmadı).
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.KurSnapshots.AsNoTracking()
            .Where(x => x.Id == kurId).Select(x => x.IptalEdildi).FirstAsync());
    }

    [Fact]
    public async Task Dis_kaynakli_kur_kaydi_iptal_edilemez_409()
    {
        var k = YeniKiraci("S3b kur dış");
        string disId;
        using (var db = f.Vt.OlusturSistem())
        {
            disId = Kuyumcu.Api.Common.Kimlik.Ulid();
            db.KurSnapshots.Add(new KurSnapshot
            {
                Id = disId, TenantId = k.TenantId, AltinHasFiyati = 6_500m,
                Kaynak = "ALTINKAYNAK", Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/kur/manuel/{disId}/iptal",
            new { gerekce = "Dış kaynaktan gelen kur yanlış görünüyor" });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        using var db2 = f.Vt.OlusturSistem();
        Assert.False(await db2.KurSnapshots.AsNoTracking()
            .Where(x => x.Id == disId).Select(x => x.IptalEdildi).FirstAsync());
    }

    // ── 3. GİDER PUSULASI (VUK md.234) ───────────────────────────────────────

    private async Task<string> PusulaKur(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kuyumcu.Api.Common.Kimlik.Ulid();
        db.GiderPusulalari.Add(new GiderPusulasi
        {
            Id = id, TenantId = k.TenantId, PusulaNo = "GP-2026-000001",
            HareketId = Kuyumcu.Api.Common.Kimlik.Ulid(), SubeId = k.SubeId,
            SaticiTipi = SaticiTipi.NihaiTuketici, SaticiAdSoyad = "Test Satıcı",
            SaticiTckn = "11111111110", SaticiTcknHash = new string('a', 64), SifreSurumu = 0,
            AyarId = "22K", GramAgirlik = 10m, HasGrami = 9.17m,
            BrutTutar = 10_000m, StopajOran = 0m, StopajTutar = 0m, NetOdenen = 10_000m,
            TeslimTarihi = DateTime.UtcNow, DuzenlemeSonTarihi = DateTime.UtcNow.AddDays(7),
            Olusturuldu = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task Gider_pusulasi_gerekceyle_IPTAL_edilir_ve_SILINMEZ()
    {
        var k = YeniKiraci("S3b pusula iptal");
        var id = await PusulaKur(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/gider-pusulalari/{id}/iptal",
            new { gerekce = "Satıcının T.C. kimlik numarası hatalı yazılmış" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var json = await cevap.Json();
        Assert.True(json.GetProperty("iptalEdildi").GetBoolean());
        // PAKET YASAL-4 / Y4-A: doğru yol aynı alış için YENİDEN DÜZENLE (yeni alış stok/kasayı ikinci kez yazardı).
        Assert.Contains("Yeniden düzenle", json.GetProperty("duzeltmeYolu").GetString());
        Assert.DoesNotContain("YENİ bir hurda alışı girin", json.GetProperty("duzeltmeYolu").GetString());

        using var db = f.Vt.OlusturSistem();
        var g = await db.GiderPusulalari.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.True(g.IptalEdildi);
        Assert.Equal("GP-2026-000001", g.PusulaNo);   // belge SİLİNMEZ
    }

    [Fact]
    public async Task Ayni_pusula_ikinci_kez_iptal_edilemez_409()
    {
        var k = YeniKiraci("S3b pusula çift");
        var id = await PusulaKur(k);
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var govde = new { gerekce = "Satıcının T.C. kimlik numarası hatalı yazılmış" };

        Assert.Equal(HttpStatusCode.OK,
            (await istemci.PostJson($"/api/v1/gider-pusulalari/{id}/iptal", govde)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await istemci.PostJson($"/api/v1/gider-pusulalari/{id}/iptal", govde)).StatusCode);
    }

    [Fact]
    public async Task MUHASEBE_pusula_iptal_edemez_403()
    {
        var k = YeniKiraci("S3b pusula rol");
        var id = await PusulaKur(k);

        var cevap = await f.Ortam.Istemci(k, "MUHASEBE").PostJson(
            $"/api/v1/gider-pusulalari/{id}/iptal",
            new { gerekce = "Satıcının T.C. kimlik numarası hatalı yazılmış" });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task Pusula_detayi_okunmasi_HASSAS_denetim_kaydi_yazar()
    {
        // Kurul tur 3 (/denetim-izi): tam TCKN gösteren okuma "kişisel veri
        // erişimi" süzgecinde GÖRÜNMÜYORDU.
        var k = YeniKiraci("S3b pusula hassas");
        var id = await PusulaKur(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync($"/api/v1/gider-pusulalari/{id}");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.True(await db.DenetimKayitlari.AsNoTracking().AnyAsync(d =>
            d.TenantId == k.TenantId && d.VarlikId == id && d.Hassas));
    }

    // ── 4. PASİF ŞUBEYİ AKTİFE ALMA ──────────────────────────────────────────

    [Fact]
    public async Task Pasife_alinan_sube_PATCH_ile_geri_acilir()
    {
        var k = YeniKiraci("S3b şube aktif");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var yeni = await istemci.PostJson("/api/v1/subeler", new { ad = "S3b İkinci Şube" });
        Assert.Equal(HttpStatusCode.OK, yeni.StatusCode);
        var subeId = (await yeni.Json()).GetProperty("id").GetString()!;

        Assert.Equal(HttpStatusCode.OK, (await istemci.DeleteAsync($"/api/v1/subeler/{subeId}")).StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.False(await db.Subeler.AsNoTracking().Where(s => s.Id == subeId)
                .Select(s => s.Aktif).FirstAsync());

        var geri = await istemci.PatchAsJsonAsync($"/api/v1/subeler/{subeId}",
            new { ad = "S3b İkinci Şube", aktif = true });
        Assert.Equal(HttpStatusCode.OK, geri.StatusCode);

        using var db2 = f.Vt.OlusturSistem();
        Assert.True(await db2.Subeler.AsNoTracking().Where(s => s.Id == subeId)
            .Select(s => s.Aktif).FirstAsync());
    }

    [Fact]
    public async Task Sube_PATCH_gonderilmeyen_aktif_alanini_KORUR()
    {
        // PATCH sözleşmesi: gövdede yoksa "değişmedi" demektir.
        var k = YeniKiraci("S3b şube patch");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var yeni = await istemci.PostJson("/api/v1/subeler", new { ad = "S3b Koru" });
        var subeId = (await yeni.Json()).GetProperty("id").GetString()!;
        await istemci.DeleteAsync($"/api/v1/subeler/{subeId}");

        var cevap = await istemci.PatchAsJsonAsync($"/api/v1/subeler/{subeId}", new { ad = "S3b Koru 2" });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var sube = await db.Subeler.AsNoTracking().FirstAsync(s => s.Id == subeId);
        Assert.Equal("S3b Koru 2", sube.Ad);
        Assert.False(sube.Aktif);       // aktif gönderilmedi → KORUNDU
    }
}
