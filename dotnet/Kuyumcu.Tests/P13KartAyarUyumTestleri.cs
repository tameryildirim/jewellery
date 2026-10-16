using System.Net;
using System.Reflection;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Sozlesmeler;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-13 — KART / AYAR / UYUM EKRAN BORÇLARI
// [HTTP, gerçek MySQL + SAF] · Kimlikler: B-39 · B-40 · B-47 · B-48 · B-12 ·
//                                         B-49 · B-17 · B-27 · B-61
//
// SÖZLEŞME (PLAN § 2 P-13 DoD):
//   • Hard DELETE yok: pasife alma + BAĞLI KAYIT SAYISI uyarısı (cari, kategori)
//   • Kur kaynağı ve maliyet yöntemi BOŞKEN KİLİTLİ KALMAZ
//   • /uyum GONDERILDI kaydında DÜZELTME YOLU tanımlıdır (ek beyan)
//   • Gider pusulasında satıcı ADRESİ boş bırakılamaz (VUK md.234)
//   • Ham iç kod (DOVIZ_HESAP_FAZ_B) kullanıcıya gösterilmez
//
// ⚠ Her test BİR İŞ KURALINI yeniden üretir; kural bozulduğunda ADIYLA
//   kırmızıya döner (mutasyon turu raporda).
// ═════════════════════════════════════════════════════════════════════════════

// ── SAF testler (veritabanı yok) ────────────────────────────────────────────

public sealed class P13SafTestleri
{
    private static readonly Kuyumcu.Api.Modules.Hurda.GiderPusulasiKapisi Kapi =
        Kuyumcu.Api.Modules.Hurda.GiderPusulasiKapisi.Varsayilan();

    private static Kuyumcu.Api.Modules.Hurda.HurdaSaticiDto Satici(string? adres) => new()
    {
        SaticiTipi = SaticiTipi.NihaiTuketici,
        AdSoyad = "Ali Veli",
        Tckn = "10000000146",
        Adres = adres,
    };

    /// <summary>
    /// B-49 · VUK md.234: pusula düzenlenen satıcıda ADRES zorunludur.
    /// Düzenleme yolu (HurdaServisi.Guncelle) adresi zaten SİLDİRMİYORDU;
    /// oluşturmada isteğe bağlıydı → aynı belge bir yoldan adressiz doğabiliyordu.
    /// </summary>
    [Fact]
    public void HurdaSaticiAdresiZorunludur()
    {
        var ex = Assert.Throws<BadRequestException>(() => Kapi.Dogrula(Satici(null)));
        Assert.Contains("adresi zorunludur", ex.Message);
        Assert.Contains("VUK md.234", ex.Message);

        var bos = Assert.Throws<BadRequestException>(() => Kapi.Dogrula(Satici("   ")));
        Assert.Contains("adresi zorunludur", bos.Message);

        // Adres doluyken kapı açılır.
        Assert.Equal(SaticiTipi.NihaiTuketici, Kapi.Dogrula(Satici("Ulus, Ankara")));
    }

    /// <summary>
    /// Mevzuat hükmü KODA GÖMÜLMEZ (ADR-0005): mali müşavir teyidi farklı
    /// çıkarsa kural tek satırlık yapılandırmadan gevşer, kod değişmez.
    /// </summary>
    [Fact]
    public void HurdaSaticiAdresZorunlulugu_YapilandirmadanKapanir()
    {
        var gevsek = new Kuyumcu.Api.Modules.Hurda.GiderPusulasiKapisi(
            new Kuyumcu.Api.Modules.Hurda.HurdaAyarlari { SaticiAdresiZorunlu = false });
        Assert.Equal(SaticiTipi.NihaiTuketici, gevsek.Dogrula(Satici(null)));
    }

    /// <summary>
    /// MÜKELLEF satıcıda pusula düzenlenmez (onun faturası belgedir) → adres
    /// kapısı orada ÇALIŞMAZ. Aksi hâlde kural belgesi olmayan bir işlemi
    /// bloklardı.
    /// </summary>
    [Fact]
    public void MukellefteAdresAranmaz()
    {
        var tip = Kapi.Dogrula(new Kuyumcu.Api.Modules.Hurda.HurdaSaticiDto
        {
            SaticiTipi = SaticiTipi.Mukellef, TedarikciFaturaNo = "A-2026-9",
        });
        Assert.Equal(SaticiTipi.Mukellef, tip);
    }

    /// <summary>
    /// Ham iç kod ekrana basılmaz: her hata/uyarı kodunun kullanıcıya
    /// gösterilecek KISA kimliği vardır ve "AC-00" (bilinmeyen) DÖNMEZ.
    /// </summary>
    [Fact]
    public void AcilisHataKimligi_TumIcKodlariKapsar()
    {
        var kodlar = Sabitler(typeof(Kuyumcu.Api.Modules.Kurulum.AcilisHataKodu))
            .Concat(Sabitler(typeof(Kuyumcu.Api.Modules.Kurulum.AcilisUyariKodu)))
            .ToList();

        Assert.NotEmpty(kodlar);
        var eksik = kodlar.Where(k => AcilisHataKimligi.Kimlik(k) == AcilisHataKimligi.Bilinmeyen).ToList();
        Assert.True(eksik.Count == 0,
            "Haritaya yazılmamış iç kod(lar): " + string.Join(", ", eksik));

        // Kimlikler benzersizdir: iki farklı kod aynı kimliği taşıyamaz.
        var kimlikler = kodlar.Select(AcilisHataKimligi.Kimlik).ToList();
        Assert.Equal(kimlikler.Count, kimlikler.Distinct().Count());
    }

    /// <summary>Kimlik HAM KODU tekrar etmez — sızıntının kendisi bu olurdu.</summary>
    [Fact]
    public void AcilisHataKimligi_HamKodDondurmez()
    {
        Assert.Equal("AC-10", AcilisHataKimligi.Kimlik("NEGATIF_ACILIS"));
        Assert.Equal("AC-07", AcilisHataKimligi.Kimlik("DOVIZ_HESAP_FAZ_B"));
        Assert.DoesNotContain("_", AcilisHataKimligi.Kimlik("DOVIZ_HESAP_FAZ_B"));

        var sonuc = new AcilisSatirSonucu(3, "DOVIZ_HESAP_FAZ_B", "Döviz hesabı bu fazda değil");
        Assert.Equal("AC-07", sonuc.Kimlik);
    }

    /// <summary>
    /// B-27 — S-3f'te "kapandı" denen ama TARAYICIDA ölçülmemiş bulgunun
    /// yeniden ölçümü: tek dilimli (%100) pastada yay yozlaşıyor ve hiçbir
    /// dolgu çizilmiyordu. DOM ölçümü (getBBox) hâlâ tarayıcı turuna aittir;
    /// burada üretilen YOLUN kapalı tam daire olduğu ölçülür — bu, bulgunun
    /// "kapandı" iddiasının kod tarafındaki tek doğrulanabilir parçasıdır.
    /// </summary>
    [Fact]
    public void B27_TekDilimliPasta_KapaliTamDaireCizer()
    {
        using var baglam = new Bunit.BunitContext();
        var svg = baglam.Render<Kuyumcu.Web.Bilesenler.PastaGrafik>(p => p.Add(
            x => x.Noktalar,
            new[] { new Kuyumcu.Web.Bilesenler.GrafikNokta("Tek", 100m) })).Markup;

        var yollar = System.Text.RegularExpressions.Regex.Matches(svg, @"<path d=""([^""]+)""");
        var d = Assert.Single(yollar).Groups[1].Value;
        // İKİ yarım yay + kapanış: yozlaşan tek yay ("A … Z" tek parça) DEĞİL.
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(d, @"\bA ").Count);
        Assert.EndsWith("Z", d);
        Assert.DoesNotContain(",", d, StringComparison.Ordinal);   // InvariantCulture
    }

    private static IEnumerable<string> Sabitler(Type t) =>
        t.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);
}

// ── HTTP testleri (gerçek MySQL) ────────────────────────────────────────────

[Collection("http")]
public sealed class P13KartAyarUyumTestleri(HttpFikstur f)
{
    /// <summary>
    /// Hata gövdesi ANLAM DÜZEYİNDE okunur: System.Text.Json Türkçe harfleri
    /// \u015F gibi kaçırır, ham metinde Assert.Contains tutmaz.
    /// </summary>
    private static async Task<string> Mesaj(HttpResponseMessage r) =>
        (await r.Json()).GetProperty("message").GetString() ?? "";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    // ═══ B-40 — KATEGORİ PASİFE ALMA ════════════════════════════════════════

    private async Task<string> KategoriKur(Kiraci k, string ad, string? ustId = null)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson("/api/v1/urun-kategorileri", new { ad, ustId, sira = 0 });
        Assert.Equal(HttpStatusCode.Created, c.StatusCode);
        return (await c.Json()).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Kategori_TemizseSilinmez_PasifeAlinirVeGeriDonulur()
    {
        var k = YeniKiraci("PAKETP13-TEST-kat-temiz");
        var id = await KategoriKur(k, "Ziynet");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var sil = await istemci.DeleteAsync($"/api/v1/urun-kategorileri/{id}");
        Assert.Equal(HttpStatusCode.OK, sil.StatusCode);

        using (var db = f.Vt.OlusturSistem())
        {
            // HARD DELETE YOK: satır yerinde, yalnız aktif=false.
            var kayit = await db.UrunKategorileri.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.False(kayit.Aktif);
        }

        // Geri dönüş yolu: PATCH {aktif:true} (UX-S25).
        var geri = await istemci.PatchJson($"/api/v1/urun-kategorileri/{id}", new { aktif = true });
        Assert.Equal(HttpStatusCode.OK, geri.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.True((await db.UrunKategorileri.AsNoTracking().SingleAsync(x => x.Id == id)).Aktif);
    }

    [Fact]
    public async Task Kategori_UrunBagliykenPasifeAlinamaz_SayiMesajdaYazar()
    {
        var k = YeniKiraci("PAKETP13-TEST-kat-urun");
        var id = await KategoriKur(k, "Bilezik");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(HttpStatusCode.OK,
            (await istemci.PatchJson($"/api/v1/urunler/{k.UrunId}", new { kategoriId = id })).StatusCode);

        var sil = await istemci.DeleteAsync($"/api/v1/urun-kategorileri/{id}");
        Assert.Equal(HttpStatusCode.Conflict, sil.StatusCode);
        Assert.Contains("1 ürün", await Mesaj(sil));

        using var db = f.Vt.OlusturSistem();
        Assert.True((await db.UrunKategorileri.AsNoTracking().SingleAsync(x => x.Id == id)).Aktif);
    }

    [Fact]
    public async Task Kategori_AktifAltKategoriVarkenPasifeAlinamaz()
    {
        var k = YeniKiraci("PAKETP13-TEST-kat-alt");
        var ust = await KategoriKur(k, "Altın");
        await KategoriKur(k, "22 Ayar", ust);

        var sil = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/urun-kategorileri/{ust}");
        Assert.Equal(HttpStatusCode.Conflict, sil.StatusCode);
        Assert.Contains("1 aktif alt kategori", await Mesaj(sil));
    }

    /// <summary>
    /// ARKA KAPI KAPALI: kapı yalnız DELETE'e konsaydı PATCH {aktif:false}
    /// sessiz bir ikinci yol olurdu ve ürünlü dal görünmez olurdu.
    /// </summary>
    [Fact]
    public async Task Kategori_PatchAktifFalse_AyniKapidanGecer()
    {
        var k = YeniKiraci("PAKETP13-TEST-kat-patch");
        var id = await KategoriKur(k, "Set");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        await istemci.PatchJson($"/api/v1/urunler/{k.UrunId}", new { kategoriId = id });

        var patch = await istemci.PatchJson($"/api/v1/urun-kategorileri/{id}", new { aktif = false });
        Assert.Equal(HttpStatusCode.Conflict, patch.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.True((await db.UrunKategorileri.AsNoTracking().SingleAsync(x => x.Id == id)).Aktif);
    }

    [Fact]
    public async Task Kategori_BagliKayitlar_SayilariDoner()
    {
        var k = YeniKiraci("PAKETP13-TEST-kat-sayi");
        var ust = await KategoriKur(k, "Kolye");
        await KategoriKur(k, "İnce Kolye", ust);
        await f.Ortam.Istemci(k, "SAHIP").PatchJson($"/api/v1/urunler/{k.UrunId}", new { kategoriId = ust });

        var c = await f.Ortam.Istemci(k, "SAHIP").GetAsync($"/api/v1/urun-kategorileri/{ust}/bagli-kayitlar");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Json();
        Assert.Equal(1, j.GetProperty("urunSayisi").GetInt32());
        Assert.Equal(1, j.GetProperty("altKategoriSayisi").GetInt32());
        Assert.True(j.GetProperty("aktif").GetBoolean());
    }

    // ═══ B-39 — CARİ PASİFE ALMA ════════════════════════════════════════════

    [Fact]
    public async Task Cari_BagliKayitlar_HareketVeBakiyeSayar()
    {
        var k = YeniKiraci("PAKETP13-TEST-cari-bagli");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var v = await istemci.PostJson($"/api/v1/cari/{k.CariId}/veresiye",
            new { tlTutari = 900m, aciklama = "Veresiye satış", kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.OK, v.StatusCode);

        var c = await istemci.GetAsync($"/api/v1/cari/{k.CariId}/bagli-kayitlar");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Json();
        Assert.Equal(k.CariId, j.GetProperty("cariId").GetString());
        Assert.True(j.GetProperty("hareketSayisi").GetInt32() >= 1);
        Assert.Equal(900m, j.GetProperty("tlBakiye").GetDecimal());
        Assert.True(j.GetProperty("aktif").GetBoolean());
    }

    /// <summary>
    /// Pasife alma ENGELLENMEZ (borçlu müşterinin kartı da kapatılır) ama
    /// kaç hareketli / ne bakiyeli kartın kapandığı DENETİM İZİNE düşer.
    /// </summary>
    [Fact]
    public async Task Cari_PasifeAlinir_DenetimIziBagliKaytlariYazar()
    {
        var k = YeniKiraci("PAKETP13-TEST-cari-pasif");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(HttpStatusCode.OK, (await istemci.PostJson(
            $"/api/v1/cari/{k.CariId}/veresiye",
            new { tlTutari = 500m, aciklama = "Veresiye", kurSnapshotId = k.KurId })).StatusCode);

        var sil = await istemci.DeleteAsync($"/api/v1/cari/{k.CariId}");
        Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
        var j = await sil.Json();
        Assert.False(j.GetProperty("aktif").GetBoolean());
        Assert.Equal(500m, j.GetProperty("bagli").GetProperty("tlBakiye").GetDecimal());

        using var db = f.Vt.OlusturSistem();
        Assert.False((await db.Cariler.AsNoTracking().SingleAsync(x => x.Id == k.CariId)).Aktif);
        // ⚠ Otomatik denetim kesicisi her SaveChanges için detaysız satır da
        //   yazar; aranan GEREKÇELİ satırdır (detay dolu olan).
        var izler = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikId == k.CariId && d.Detay != null)
            .Select(d => d.Detay!).ToListAsync();
        var iz = Assert.Single(izler, x => x.Contains("pasife alındı"));
        Assert.Contains("TL bakiye: 500", iz);
    }

    // ═══ B-47 — MALİYET YÖNTEMİ AYARI ═══════════════════════════════════════

    [Fact]
    public async Task MaliyetYontemi_BoskenSecilebilir_VeDenetimeDuser()
    {
        var k = YeniKiraci("PAKETP13-TEST-maliyet");
        using (var db = f.Vt.OlusturSistem())
            Assert.Null((await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == k.TenantId)).MaliyetYontemi);

        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PatchJson("/api/v1/admin/tenant/maliyet-yontemi", new { yontem = "LIFO" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        Assert.Equal("LIFO", (await c.Json()).GetProperty("maliyetYontemi").GetString());

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal("LIFO",
            (await kontrol.Tenants.AsNoTracking().SingleAsync(t => t.Id == k.TenantId)).MaliyetYontemi);
        var izler = await kontrol.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikId == k.TenantId && d.Detay != null)
            .Select(d => d.Detay!).ToListAsync();
        var iz = Assert.Single(izler, x => x.Contains("maliyet-yontemi-degisti"));
        Assert.Contains("LIFO", iz);
    }

    [Fact]
    public async Task MaliyetYontemi_GecersizDegerReddedilir()
    {
        var k = YeniKiraci("PAKETP13-TEST-maliyet-red");
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PatchJson("/api/v1/admin/tenant/maliyet-yontemi", new { yontem = "KAFAMA_GORE" });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("FIFO", await Mesaj(c));

        using var db = f.Vt.OlusturSistem();
        Assert.Null((await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == k.TenantId)).MaliyetYontemi);
    }

    // ═══ B-12 — /uyum GONDERILDI KAYDINDA DÜZELTME YOLU (EK BEYAN) ══════════

    private static string Metin(int uzunluk) => new('ş', uzunluk);

    private async Task<string> SupheliKur(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/masak/supheli", new
        {
            islemTutariTl = 250_000m, gerekce = Metin(320),
        });
        Assert.Equal(HttpStatusCode.Created, c.StatusCode);
        return (await c.Json()).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task EkBeyan_YalnizGonderilmisBildirimeDuzenlenir()
    {
        var k = YeniKiraci("PAKETP13-TEST-ekbeyan-acik");
        var id = await SupheliKur(k);

        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/masak/supheli/{id}/ek-beyan", new { gerekce = Metin(330) });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("GÖNDERİLMİŞ", await Mesaj(c));
    }

    [Fact]
    public async Task EkBeyan_AsliDegistirmez_VeBaglar()
    {
        var k = YeniKiraci("PAKETP13-TEST-ekbeyan");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var id = await SupheliKur(k);
        Assert.Equal(HttpStatusCode.OK, (await istemci.PatchJson(
            $"/api/v1/masak/supheli/{id}/gonderildi", new { gonderimReferansi = "MSK-2026-1" })).StatusCode);

        var asilGerekce = Metin(320);
        var ekGerekce = Metin(400);
        var c = await istemci.PostJson($"/api/v1/masak/supheli/{id}/ek-beyan", new { gerekce = ekGerekce });
        Assert.Equal(HttpStatusCode.Created, c.StatusCode);
        var j = await c.Json();
        var yeniId = j.GetProperty("id").GetString()!;
        Assert.Equal(id, j.GetProperty("duzeltilenBildirimId").GetString());
        Assert.NotEqual(id, yeniId);
        // Bağlar DEVRALINIR: tutar aslından gelir (gövdede gönderilmedi).
        Assert.Equal(250_000m, j.GetProperty("islemTutariTl").GetDecimal());

        using var db = f.Vt.OlusturSistem();
        var asil = await db.MasakSupheliBildirimler.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(SupheliDurum.Gonderildi, asil.Durum);   // asıl GERİ ALINMAZ
        Assert.Equal(asilGerekce, asil.Gerekce);             // asıl ÜZERİNE YAZILMAZ
        Assert.Null(asil.DuzeltilenBildirimId);
        Assert.Equal(id, (await db.MasakSupheliBildirimler.AsNoTracking()
            .SingleAsync(x => x.Id == yeniId)).DuzeltilenBildirimId);

        var iz = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikId == yeniId).FirstAsync();
        Assert.Contains("ek beyan", iz.Detay!);
    }

    [Fact]
    public async Task EkBeyanin_EkBeyani_Duzenlenemez()
    {
        var k = YeniKiraci("PAKETP13-TEST-ekbeyan-zincir");
        var istemci = f.Ortam.Istemci(k, "SAHIP");
        var id = await SupheliKur(k);
        await istemci.PatchJson($"/api/v1/masak/supheli/{id}/gonderildi", new { gonderimReferansi = "MSK-2026-2" });
        var ek = await istemci.PostJson($"/api/v1/masak/supheli/{id}/ek-beyan", new { gerekce = Metin(350) });
        var ekId = (await ek.Json()).GetProperty("id").GetString()!;
        await istemci.PatchJson($"/api/v1/masak/supheli/{ekId}/gonderildi", new { gonderimReferansi = "MSK-2026-3" });

        var c = await istemci.PostJson($"/api/v1/masak/supheli/{ekId}/ek-beyan", new { gerekce = Metin(360) });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("ek beyanın ek beyanı", (await Mesaj(c)).ToLowerInvariant());
    }

    // ═══ B-61 — /isletme TELEFON DOĞRULAMASI (gerileme kilidi) ══════════════

    [Fact]
    public async Task IsletmeTelefonu_SubeyleAyniKuraldanGecer()
    {
        var k = YeniKiraci("PAKETP13-TEST-telefon");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var kotu = await istemci.PatchJson("/api/v1/admin/tenant/kimlik", new { telefon = "abc" });
        Assert.Equal(HttpStatusCode.BadRequest, kotu.StatusCode);

        var iyi = await istemci.PatchJson("/api/v1/admin/tenant/kimlik", new { telefon = "05551112233" });
        Assert.Equal(HttpStatusCode.OK, iyi.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal("05551112233", (await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == k.TenantId)).Telefon);
    }
}
