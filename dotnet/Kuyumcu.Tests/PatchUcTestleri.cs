using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PATCH-3 (02.10) — PATCH-2 / PATCH-SOZLESME açık kalanları
//   (1) Satış alış fişi kalemi + rezerv oluşturma: ÖNCE 3 haneye yuvarla, SONRA ≤ 0.
//   (2) Rezerv PATCH: sayı dize / decimal'e sığmayan / metin alanına sayı → 500 değil 400 alan.
//   (3) Satış modülünün TÜM yazma uçlarında kolon tavanı → 500 yok (400 + alan).
//   (4) Ürün/kategori isteğe bağlı alanları ÜÇ DURUMLU (yok=koru · null=SİL · ""=SİL geriye
//       uyum); zorunlu alanda null = KORU her yerde (şube adı dahil).
//   (5) Tek 6 hane yuvarlama yardımcısı · ürün POST kolon tavanı · MySQL 1264 / taşma → 400
//       son savunma hattı · Range iletisi (AralikMesaji) tek ve iki yönde doğru ileti.
// Her HTTP vakası: kod + alan (gövde AYRIŞTIRILARAK) ve veritabanında satır önce/sonra.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PatchUcHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCH3 {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    /// <summary>400 + alanlar içinde son parçası <paramref name="alan"/> olan anahtar (büyük/küçük harf duyarsız).</summary>
    private static async Task Alan400(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "400 gövdesinde 'alanlar' yok: " + govde);
        var anahtarlar = alanlar.EnumerateObject().Select(p => p.Name).ToList();
        Assert.True(anahtarlar.Any(a => a.Split('.')[^1].Equals(alan, StringComparison.OrdinalIgnoreCase)
                                        || a.Equals(alan, StringComparison.OrdinalIgnoreCase)),
            $"400 '{alan}' alanına işaretli değil: {string.Join(",", anahtarlar)}");
    }

    // ═══════════ (1) ALIŞ FİŞİ — 1,0004 adet kolona 1,000 yazılır (yuvarla → kontrol → yaz) ═══════════

    [Fact]
    public async Task P3_alis_fisi_kalem_adet_1_0004_yuvarlanmis_1_000_yazilir()
    {
        var k = Kur("alis fisi yuvarla");
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/alis-fisi", new
        {
            cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1.0004m, birimFiyatTl = 100m } },
            odemeTuru = "NAKIT",
            kasaId = k.KasaId,
            kurSnapshotId = k.KurId,
        });
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        using var db = f.Vt.OlusturSistem();
        var h = db.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "ALIS");
        Assert.Equal(1.000m, h.Adet);
        Assert.Equal(100.00m, Math.Abs(h.TlTutari));   // tutar yuvarlanmış adetle: 1,000 × 100 (alış eksi yönlü)
    }

    // ═══════════ (2) REZERV PATCH — tür / taşma / yuvarlama / null = KORU ═══════════

    private async Task<(string Id, decimal Bloke)> RezervKur(Kiraci k)
    {
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/islemler/rezervler",
            new { urunId = k.UrunId, cariId = k.CariId, adet = 2m });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var id = db.Rezervasyonlar.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;
        return (id, db.Stoklar.AsNoTracking().Single(s => s.TenantId == k.TenantId).BlokeAdet);
    }

    private (decimal Adet, string? Aciklama, DateOnly? Bitis, decimal Bloke) RezervDurum(Kiraci k, string id)
    {
        using var db = f.Vt.OlusturSistem();
        var z = db.Rezervasyonlar.AsNoTracking().Single(x => x.Id == id);
        return (z.Adet, z.Aciklama, z.BitisGunu, db.Stoklar.AsNoTracking().Single(s => s.TenantId == k.TenantId).BlokeAdet);
    }

    [Theory]
    [InlineData("""{"adet":"1.500"}""", "adet")]
    [InlineData("""{"adet":"3"}""", "adet")]
    [InlineData("""{"adet":1e30}""", "adet")]
    [InlineData("""{"adet":0.0004}""", "adet")]
    [InlineData("""{"adet":true}""", "adet")]
    [InlineData("""{"adet":100001}""", "adet")]   // tur 2: oluşturmayla TEK üst sınır (100.000)
    [InlineData("""{"aciklama":5}""", "aciklama")]
    [InlineData("""{"bitisGunu":20261231}""", "bitisGunu")]
    [InlineData("""{"bitisGunu":"31.12"}""", "bitisGunu")]
    public async Task P3_rezerv_PATCH_hatali_tur_ve_tasma_400_alan_500_DEGIL_SQL_DEGISMEZ(string json, string alan)
    {
        var k = Kur("rezerv patch tur");
        var (id, _) = await RezervKur(k);
        var once = RezervDurum(k, id);
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/islemler/rezervler/{id}", json), alan);
        Assert.Equal(once, RezervDurum(k, id));
    }

    [Fact]
    public async Task P3_rezerv_PATCH_adet_null_KORUR_200_zorunlu_alan_silinemez()
    {
        var k = Kur("rezerv null");
        var (id, bloke) = await RezervKur(k);
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/islemler/rezervler/{id}", """{"adet":null}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var s = RezervDurum(k, id);
        Assert.Equal(2m, s.Adet);
        Assert.Equal(bloke, s.Bloke);
    }

    [Fact]
    public async Task P3_rezerv_PATCH_adet_0_0005_sinir_0_001_olur_bloke_FARK_kadar()
    {
        var k = Kur("rezerv sinir");
        using (var g = f.Vt.OlusturSistem()) TestKurulum.UrunuGramBirimiYap(g, k.UrunId);   // PAKET GOC-C (K14): kesir yalnız GRAM birimde — yuvarlama sınırı orada ölçülür
        var (id, bloke) = await RezervKur(k);
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/islemler/rezervler/{id}", """{"adet":0.0005}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var s = RezervDurum(k, id);
        Assert.Equal(0.001m, s.Adet);
        Assert.Equal(bloke - 2m + 0.001m, s.Bloke);
    }

    // ═══════════ (3) SATIŞ MODÜLÜ — TÜM YAZMA UÇLARI kolon tavanı (500 yok) ═══════════

    [Theory]
    [InlineData("satis", """{"urunId":"x","adet":1e20,"birimFiyatTl":1,"kurSnapshotId":"x"}""", "adet")]
    [InlineData("satis", """{"urunId":"x","adet":1,"birimFiyatTl":1e20,"kurSnapshotId":"x"}""", "birimFiyatTl")]
    [InlineData("satis", """{"urunId":"x","adet":1,"birimFiyatTl":1,"iscilikTl":1e20,"kurSnapshotId":"x"}""", "iscilikTl")]
    [InlineData("alis", """{"urunId":"x","adet":1e20,"birimFiyatTl":1,"kurSnapshotId":"x"}""", "adet")]
    [InlineData("alis", """{"urunId":"x","adet":1,"birimFiyatTl":1,"odenenTl":1e20,"kurSnapshotId":"x"}""", "odenenTl")]
    [InlineData("alis-fisi", """{"kalemler":[{"urunId":"x","adet":1e20,"birimFiyatTl":1}],"odemeTuru":"NAKIT","kurSnapshotId":"x"}""", "adet")]
    [InlineData("alis-fisi", """{"kalemler":[{"urunId":"x","adet":1,"birimFiyatTl":1e20}],"odemeTuru":"NAKIT","kurSnapshotId":"x"}""", "birimFiyatTl")]
    [InlineData("hurda-alis", """{"ayarId":"K22","gramAgirlik":1e20,"birimFiyatTlHasGram":1,"kurSnapshotId":"x"}""", "gramAgirlik")]
    [InlineData("hurda-alis", """{"ayarId":"K22","gramAgirlik":1,"birimFiyatTlHasGram":1e20,"kurSnapshotId":"x"}""", "birimFiyatTlHasGram")]
    [InlineData("hurda-satis", """{"hasGrami":1e20,"birimFiyatTlHasGram":1,"kurSnapshotId":"x"}""", "hasGrami")]
    [InlineData("hurda-satis", """{"hasGrami":1,"birimFiyatTlHasGram":1,"tahsilEdilenTl":1e20,"kurSnapshotId":"x"}""", "tahsilEdilenTl")]
    [InlineData("hurda-satis", """{"hasGrami":1,"birimFiyatTlHasGram":1,"fireGrami":1e20,"kurSnapshotId":"x"}""", "fireGrami")]
    [InlineData("transfer", """{"kaynakSubeId":"a","hedefSubeId":"b","urunId":"x","adet":1e20}""", "adet")]
    [InlineData("satis-fisi", """{"kalemler":[{"urunId":"x","adet":1,"birimFiyatTl":1e20}],"kurSnapshotId":"x"}""", "birimFiyatTl")]
    [InlineData("satis-iade", """{"orijinalHareketId":"x","adet":1e20,"kurSnapshotId":"x"}""", "adet")]
    [InlineData("alis-iade", """{"orijinalHareketId":"x","adet":1,"iadeTutariTl":1e20,"kurSnapshotId":"x"}""", "iadeTutariTl")]
    [InlineData("takas", """{"eskiUrunAyarId":"K22","eskiUrunGram":1e20,"eskiUrunBirimFiyatTlHasGram":1,"yeniUrunId":"x","yeniUrunAdet":1,"yeniUrunBirimFiyatTl":1,"kurSnapshotId":"x"}""", "eskiUrunGram")]
    [InlineData("takas", """{"eskiUrunAyarId":"K22","eskiUrunGram":-5,"eskiUrunBirimFiyatTlHasGram":1,"yeniUrunId":"x","yeniUrunAdet":1,"yeniUrunBirimFiyatTl":1,"kurSnapshotId":"x"}""", "eskiUrunGram")]
    [InlineData("takas", """{"eskiUrunAyarId":"K22","eskiUrunGram":1,"eskiUrunBirimFiyatTlHasGram":1e20,"yeniUrunId":"x","yeniUrunAdet":1,"yeniUrunBirimFiyatTl":1,"kurSnapshotId":"x"}""", "eskiUrunBirimFiyatTlHasGram")]
    [InlineData("iscilik", """{"cariId":"x","iscilikTl":1e20,"kurSnapshotId":"x"}""", "iscilikTl")]
    [InlineData("avans", """{"cariId":"x","tutar":1e20,"kurSnapshotId":"x"}""", "tutar")]
    [InlineData("avans/mahsup", """{"satisHareketId":"x","tutar":1e20,"kurSnapshotId":"x"}""", "tutar")]
    [InlineData("avans/iade", """{"cariId":"x","tutar":1e20,"kasaId":"x","kurSnapshotId":"x"}""", "tutar")]
    [InlineData("degisim", """{"kaynakHareketId":"x","geriAlinanAdet":1,"yeniUrunId":"x","yeniAdet":1,"yeniBirimFiyatTl":1e20,"kurSnapshotId":"x"}""", "yeniBirimFiyatTl")]
    [InlineData("rezervler", """{"urunId":"x","cariId":"x","adet":1e20}""", "adet")]
    [InlineData("konsinye-cikis", """{"cariId":"x","urunId":"x","adet":1e20,"kurSnapshotId":"x"}""", "adet")]
    public async Task P3_satis_yazma_ucu_kolon_tavanini_asan_sayi_400_alan_500_DEGIL(string uc, string json, string alan)
    {
        var k = Kur("satis tavan " + uc);
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/islemler/{uc}", json), alan);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.Hareketler.Count(h => h.TenantId == k.TenantId));
    }

    // ═══════════ (5a) SON SAVUNMA HATTI — gerçek MySQL 1264 → 400 (500 değil) ═══════════

    [Fact]
    public async Task P3_MySQL_1264_kolon_tasmasi_genel_agda_400_alan_ile_satir_YAZILMAZ()
    {
        // Emanet girişinde gram kolonu için uç düzeyinde kolon kapısı YOKTU (bu paketin dosya
        // alanı dışı) — tam da son savunma hattının koruması gereken yol.
        // PAKET GOC-A: artık uçta K10 [Gram] kapısı VAR (tavan) — ileti sözlükten ve dizi biçiminde
        // (ModelState) döner; iddia aynı: 400 + alan + satır YAZILMAZ (500 değil).
        var k = Kur("1264 agi");
        var r = await f.Ortam.Istemci(k).PostJson("/api/v1/emanet/giris", new
        {
            cariId = k.CariId, ayarId = "K22", gramAgirlik = 1_000_000_000_000_000m, kurSnapshotId = k.KurId,
        });
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        var mesaj = j.GetProperty("message") is { ValueKind: JsonValueKind.Array } dizi
            ? string.Join(" ", dizi.EnumerateArray().Select(x => x.GetString()))
            : j.GetProperty("message").GetString();
        Assert.Contains("çok büyük", mesaj);
        Assert.True(j.TryGetProperty("alanlar", out var alanlar) && alanlar.EnumerateObject().Any(), govde);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.Hareketler.Count(h => h.TenantId == k.TenantId));
    }

    // ═══════════ (5b) ÜRÜN POST — kolon tavanı (PATCH ile ortak kapı) ═══════════

    [Theory]
    [InlineData("satisFiyati", 100_000_000_000_000)]
    [InlineData("iscilikTl", 100_000_000_000)]
    [InlineData("alisMaliyeti", 100_000_000_000_000)]
    [InlineData("minStok", 10_000_000_000)]
    [InlineData("gramAgirlik", 1_000_000_000)]
    public async Task P3_urun_POST_kolon_tavanini_asan_deger_400_alan_urun_ACILMAZ(string alan, double deger)
    {
        var k = Kur("urun post tavan " + alan);
        var govde = new Dictionary<string, object?>
        {
            ["tip"] = "ZIYNET", ["kod"] = "P3-" + alan, ["ad"] = "Tavan", ["ayarId"] = "K22",
            ["gramAgirlik"] = 5m, ["birim"] = "ADET", ["tasKokeni"] = "TAS_YOK",
        };
        govde[alan] = (decimal)deger;
        await Alan400(await f.Ortam.Istemci(k).PostJson("/api/v1/urunler", govde), alan);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1, db.Urunler.Count(u => u.TenantId == k.TenantId));
    }

    // ═══════════ (4) ÜRÜN PATCH — isteğe bağlı alan ÜÇ DURUMLU ═══════════

    private void UrunDoldur(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var u = db.Urunler.Single(x => x.Id == k.UrunId);
        u.Aciklama = "Vitrin 1"; u.PariteKodu = "CEYREK_YENI"; u.KmtsKodu = "KMTS-1"; u.Barkod = "B-P3";
        u.Rfid = "RF-1"; u.GorselUrl = "https://ornek.test/a.jpg"; u.TasIbaresi = "Doğal";
        u.SatisFiyati = 5000m; u.SeriNo = "SN-1"; u.Olcu = "17";
        db.SaveChanges();
    }

    private static object? UrunAlani(Urun u, string alan) => alan switch
    {
        "aciklama" => u.Aciklama, "pariteKodu" => u.PariteKodu, "kmtsKodu" => u.KmtsKodu,
        "barkod" => u.Barkod, "rfid" => u.Rfid, "gorselUrl" => u.GorselUrl,
        "tasIbaresi" => u.TasIbaresi, "satisFiyati" => u.SatisFiyati, "seriNo" => u.SeriNo, "olcu" => u.Olcu,
        _ => throw new ArgumentException(alan),
    };

    private Urun UrunOku(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Urunler.AsNoTracking().Single(x => x.Id == k.UrunId);
    }

    private static readonly string[] Alanlar =
        ["aciklama", "pariteKodu", "kmtsKodu", "barkod", "rfid", "gorselUrl", "tasIbaresi", "satisFiyati", "seriNo", "olcu"];

    public static TheoryData<string> IstegeBagliAlanlar() => new(Alanlar);

    [Theory]
    [MemberData(nameof(IstegeBagliAlanlar))]
    public async Task P3_urun_PATCH_istege_bagli_alan_null_SILER_SQL_NULL_digerleri_AYNI(string alan)
    {
        var k = Kur("urun null " + alan);
        UrunDoldur(k);
        var once = UrunOku(k);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/urunler/{k.UrunId}",
            new Dictionary<string, object?> { [alan] = null });
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        var sonra = UrunOku(k);
        Assert.Null(UrunAlani(sonra, alan));
        foreach (var diger in Alanlar.Where(x => x != alan))
            Assert.Equal(UrunAlani(once, diger), UrunAlani(sonra, diger));
        Assert.Equal(once.Ad, sonra.Ad);
    }

    [Theory]
    [InlineData("aciklama")]
    [InlineData("pariteKodu")]
    [InlineData("barkod")]
    [InlineData("tasIbaresi")]
    [InlineData("gorselUrl")]
    public async Task P3_urun_PATCH_bos_dize_GERIYE_UYUM_ile_de_SILER(string alan)
    {
        var k = Kur("urun bos " + alan);
        UrunDoldur(k);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/urunler/{k.UrunId}",
            new Dictionary<string, object?> { [alan] = "" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Null(UrunAlani(UrunOku(k), alan));
    }

    [Fact]
    public async Task P3_urun_PATCH_gonderilmeyen_istege_bagli_alanlar_KORUNUR_zorunlu_null_KORUNUR()
    {
        var k = Kur("urun koru");
        UrunDoldur(k);
        var once = UrunOku(k);
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/urunler/{k.UrunId}",
            new Dictionary<string, object?> { ["ad"] = null, ["kod"] = null, ["iscilikTl"] = 75m });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var sonra = UrunOku(k);
        Assert.Equal(75m, sonra.IscilikTl);
        Assert.Equal((once.Ad, once.Kod), (sonra.Ad, sonra.Kod));
        foreach (var alan in Alanlar)
            Assert.Equal(UrunAlani(once, alan), UrunAlani(sonra, alan));
    }

    [Fact]
    public async Task P3_pirlanta_4C_renk_null_SILER_karat_KORUNUR()
    {
        var k = Kur("pirlanta 4c");
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.Tip = "PIRLANTA"; u.PirlantaKarat = 0.5m; u.PirlantaRenk = "G"; u.SertifikaNo = "GIA-1";
            db.SaveChanges();
        }
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/urunler/{k.UrunId}",
            new Dictionary<string, object?> { ["pirlantaRenk"] = null, ["sertifikaNo"] = "" });
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        var s = UrunOku(k);
        Assert.Null(s.PirlantaRenk);
        Assert.Null(s.SertifikaNo);
        Assert.Equal(0.5m, s.PirlantaKarat);
    }

    // ═══════════ (4) KATEGORİ PATCH — üst kategori ÜÇ DURUMLU · ad null = KORU ═══════════

    private (string Ust, string Alt) KategoriKur(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var ust = new UrunKategorisi { Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Bilezik", Aktif = true, Olusturuldu = now, Guncellendi = now };
        var alt = new UrunKategorisi { Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = "Burma", UstId = ust.Id, Sira = 3, Aktif = true, Olusturuldu = now, Guncellendi = now };
        db.UrunKategorileri.AddRange(ust, alt);
        db.SaveChanges();
        return (ust.Id, alt.Id);
    }

    private UrunKategorisi KategoriOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return db.UrunKategorileri.AsNoTracking().Single(x => x.Id == id);
    }

    [Theory]
    [InlineData("""{"ustId":null}""")]
    [InlineData("""{"ustId":""}""")]
    public async Task P3_kategori_PATCH_ustId_null_ve_bos_KOKE_tasir_SQL_NULL(string json)
    {
        var k = Kur("kategori kok");
        var (_, alt) = KategoriKur(k);
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urun-kategorileri/{alt}", json);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var s = KategoriOku(alt);
        Assert.Null(s.UstId);
        Assert.Equal(("Burma", 3), (s.Ad, s.Sira));
    }

    [Fact]
    public async Task P3_kategori_PATCH_ustId_gonderilmezse_ve_ad_null_ise_KORUNUR()
    {
        var k = Kur("kategori koru");
        var (ust, alt) = KategoriKur(k);
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urun-kategorileri/{alt}", """{"ad":null,"sira":7}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var s = KategoriOku(alt);
        Assert.Equal((ust, "Burma", 7), (s.UstId, s.Ad, s.Sira));
    }

    // ═══════════ (4) ŞUBE — zorunlu ad null = KORU; isteğe bağlı "" = SİL (kolona "" yazılmaz) ═══════════

    [Fact]
    public async Task P3_sube_PATCH_ad_null_KORUR_adres_bos_dize_NULL_olur()
    {
        var k = Kur("sube tutarlilik");
        using (var db = f.Vt.OlusturSistem())
        {
            var s = db.Subeler.Single(x => x.Id == k.SubeId);
            s.Ad = "Merkez P3"; s.Adres = "Çarşı 1"; s.Telefon = "02125550000";
            db.SaveChanges();
        }
        var r = await f.Ortam.Istemci(k).PatchJson($"/api/v1/subeler/{k.SubeId}",
            new Dictionary<string, object?> { ["ad"] = null, ["adres"] = "", ["telefon"] = "" });
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        using var o = f.Vt.OlusturSistem();
        var sube = o.Subeler.AsNoTracking().Single(x => x.Id == k.SubeId);
        Assert.Equal("Merkez P3", sube.Ad);
        Assert.Null(sube.Adres);
        Assert.Null(sube.Telefon);
    }
}

// ═══════════ Servis yolu — DTO Range yalnız controller'da koşar: servis içi kapı ═══════════

[Collection("veritabani")]
public sealed class PatchUcServisTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Servis(KuyumcuDbContext db) => new(db, NullLogger<MSatis.SatisServisi>.Instance);

    [Theory]
    [InlineData(0.0004)]
    [InlineData(0.0001)]
    [InlineData(-0.0004)]
    public async Task P3_servis_alis_fisi_kalem_adet_yuvarlaninca_sifir_RET_alan_kalemler(double adet)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCH3 servis alis");
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).AlisFisi(new MSatis.AlisFisiDto
        {
            CariId = k.CariId, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.AlisFisKalemDto { UrunId = k.UrunId, Adet = (decimal)adet, BirimFiyatTl = 100m }],
        }, k.Islem));
        Assert.Equal("kalemler", AlanIsareti.Alan(hata));
        using var o = vt.OlusturSistem();
        Assert.Equal(0, o.Hareketler.Count(h => h.TenantId == k.TenantId));
    }

    [Fact]
    public async Task P3_servis_alis_fisi_kalem_birim_fiyat_kolon_tavani_RET()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCH3 servis alis tavan");
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).AlisFisi(new MSatis.AlisFisiDto
        {
            CariId = k.CariId, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.AlisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 10_000_000_000_000m }],
        }, k.Islem));
        Assert.Equal("kalemler", AlanIsareti.Alan(hata));
    }

    [Theory]
    [InlineData(0.0004)]
    [InlineData(0.0001)]
    public async Task P3_servis_rezerv_OLUSTUR_adet_yuvarlaninca_sifir_RET_bloke_DEGISMEZ(double adet)
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCH3 servis rezerv");
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).RezervOlustur(
            new MSatis.RezervOlusturDto { UrunId = k.UrunId, CariId = k.CariId, Adet = (decimal)adet }, k.Islem));
        Assert.Equal("adet", AlanIsareti.Alan(hata));
        using var o = vt.OlusturSistem();
        Assert.Equal(0, o.Rezervasyonlar.Count(r => r.TenantId == k.TenantId));
        Assert.Equal(0m, o.Stoklar.AsNoTracking().Single(s => s.TenantId == k.TenantId).BlokeAdet);
    }

    [Theory]
    [InlineData("satisFiyati")]
    [InlineData("iscilikTl")]
    [InlineData("minStok")]
    public async Task P3_servis_urun_OLUSTUR_kolon_tavani_UC_KAPISINDA_400_alan_veritabanina_GITMEZ(string alan)
    {
        // HTTP yolunda son savunma hattı (1264 → 400) da aynı alanı verir; uç kapısının
        // kendisi ancak servis yolunda (ağ yok) ayrıca kanıtlanır: kapı yoksa DbUpdateException.
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCH3 servis urun " + alan);
        using var db = vt.Olustur(k);
        var dto = new Kuyumcu.Api.Modules.Urun.UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = "P3S-" + alan, Ad = "Tavan", AyarId = "K22", GramAgirlik = 5m,
            Birim = "ADET", TasKokeni = "TAS_YOK",
        };
        switch (alan)
        {
            case "satisFiyati": dto.SatisFiyati = 100_000_000_000_000m; break;
            case "iscilikTl": dto.IscilikTl = 100_000_000_000m; break;
            default: dto.MinStok = 10_000_000_000m; break;
        }
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => new Kuyumcu.Api.Modules.Urun.UrunServisi(db, TestKurulum.Denetim(db)).Olustur(dto, k.Islem));
        Assert.Equal(alan, AlanIsareti.Alan(hata));
        using var o = vt.OlusturSistem();
        Assert.Equal(1, o.Urunler.Count(u => u.TenantId == k.TenantId));
    }

    [Fact]
    public async Task P3_servis_rezerv_OLUSTUR_adet_0_0005_sinir_0_001_yazilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PATCH3 servis rezerv sinir");
        TestKurulum.UrunuGramBirimiYap(kurulum, k.UrunId);   // PAKET GOC-C (K14): kesir yalnız GRAM birimde — yuvarlama sınırı orada ölçülür
        using var db = vt.Olustur(k);
        await Servis(db).RezervOlustur(new MSatis.RezervOlusturDto { UrunId = k.UrunId, CariId = k.CariId, Adet = 0.0005m }, k.Islem);
        using var o = vt.OlusturSistem();
        Assert.Equal(0.001m, o.Rezervasyonlar.AsNoTracking().Single(r => r.TenantId == k.TenantId).Adet);
        Assert.Equal(0.001m, o.Stoklar.AsNoTracking().Single(s => s.TenantId == k.TenantId).BlokeAdet);
    }
}

// ═══════════ Saf testler — tek yuvarlama kuralı, son savunma hattı, Range iletisi ═══════════

public sealed class PatchUcSafTestleri
{
    [Theory]
    [InlineData("1.3750005", "1.375001")]
    [InlineData("-1.3750005", "-1.375001")]
    [InlineData("0.0000005", "0.000001")]
    [InlineData("2.0000025", "2.000003")]
    public void P3_tek_6_hane_kurali_AwayFromZero_fatura_motoruyla_BIREBIR(string girdi, string beklenen)
    {
        var d = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        var b = decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(b, Kuyumcu.Api.Modules.KolonTavani.Yuvarla6(d));
        Assert.Equal(Kuyumcu.Api.Modules.Fatura.OzelMatrahMotoru.Yuvarla6(d), Kuyumcu.Api.Modules.KolonTavani.Yuvarla6(d));
    }

    [Fact]
    public void P3_satis_modulu_6_hane_yuvarlamasi_TEK_yardimciya_baglidir()
    {
        // SatisModulu'nun özel Yuvarla6'sı KolonTavani.Yuvarla6'ya yönlenir — iki kural ayrışamaz.
        var m = typeof(MSatis.SatisServisi).GetMethod("Yuvarla6",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        Assert.Equal(1.375001m, (decimal)m.Invoke(null, [1.3750005m])!);
        Assert.Equal(-1.375001m, (decimal)m.Invoke(null, [-1.3750005m])!);
    }

    /// <summary>MySqlConnector MySqlException benzeri: yansımayla okunan Number özelliği.</summary>
    private sealed class SahteSqlHatasi(int numara, string ileti) : Exception(ileti)
    {
        public int Number { get; } = numara;
    }

    [Fact]
    public void P3_ag_MySQL_1264_alanli_400_olur()
    {
        var ex = new DbUpdateException("kaydedilemedi",
            new SahteSqlHatasi(1264, "Out of range value for column 'konsinye_birim_bedel' at row 1"));
        var red = Program.SayiTasmasiCevir(ex);
        Assert.NotNull(red);
        Assert.Equal(400, red!.StatusCode);
        Assert.Equal("konsinyeBirimBedel", AlanIsareti.Alan(red));
        Assert.Contains("çok büyük", red.Message);
    }

    [Fact]
    public void P3_ag_tasma_OverflowException_400_olur_baska_hata_CEVRILMEZ()
    {
        Assert.Equal(400, Program.SayiTasmasiCevir(new InvalidOperationException("x", new OverflowException()))!.StatusCode);
        Assert.Null(Program.SayiTasmasiCevir(new DbUpdateException("x", new SahteSqlHatasi(1062, "Duplicate entry"))));
        Assert.Null(Program.SayiTasmasiCevir(new InvalidOperationException("başka")));
    }

    [Theory]
    [InlineData("tl_tutari", "tlTutari")]
    [InlineData("gram", "gram")]
    [InlineData("toplam_has_gram", "toplamHasGram")]
    public void P3_kolon_adi_camelCase_alana_cevrilir(string kolon, string alan)
    {
        Assert.Equal(alan, Program.KolonAlani(kolon));
        Assert.Equal(kolon, Program.KolonAdi($"Out of range value for column '{kolon}' at row 1"));
    }

    [Theory]
    [InlineData("The field Tutar must be between 0,01 and 9999999999999,99.", "Tutar 0,01 ile 9.999.999.999.999,99 arasında olmalıdır")]
    // tur 2 (doğrulayıcı ORTA): kültürsüz (invariant) sunucu iletisinde KÜÇÜK sınır da tr-TR olur.
    [InlineData("The field Tutar must be between 0.01 and 9999999999999.99.", "Tutar 0,01 ile 9.999.999.999.999,99 arasında olmalıdır")]
    [InlineData("The field Adet must be between 0.001 and 100000.", "Adet 0,001 ile 100000 arasında olmalıdır")]
    [InlineData("The field Adet must be between 0,001 and 999999999,999.", "Adet 0,001 ile 999.999.999,999 arasında olmalıdır")]
    [InlineData("The field VadeGun must be between 0 and 3650.", "Vade (gün) 0 ile 3650 arasında olmalıdır")]
    public void P3_AralikMesaji_sonlu_ust_sinir_HER_ZAMAN_yazilir_tek_ileti_iki_yonde_dogru(string mesaj, string beklenen)
    {
        var alan = mesaj.Split(' ')[2];
        Assert.Equal(beklenen, Kuyumcu.Core.I18n.DogrulamaSozlugu.Cevir(alan, mesaj));
    }

    [Theory]
    [InlineData("The field IscilikTl must be between 0 exclusive and 9999999999999,99.", "İşçilik (TL) 0 değerinden büyük, en çok 9.999.999.999.999,99 olmalıdır")]
    [InlineData("The field IscilikTl must be between 0 exclusive and 1,79769313486232E+308.", "İşçilik (TL) 0 değerinden büyük olmalıdır")]
    [InlineData("The field HasGrami must be between 1E-06 and 999999999,999999.", "Has gram 0,000001 ile 999.999.999,999999 arasında olmalıdır")]
    [InlineData("The field IscilikTl must be between 0.5 exclusive and 9999999999999.99.", "İşçilik (TL) 0,5 değerinden büyük, en çok 9.999.999.999.999,99 olmalıdır")]
    [InlineData("The field HasGrami must be between 1E-06 and 999999999.999999.", "Has gram 0,000001 ile 999.999.999,999999 arasında olmalıdır")]
    [InlineData("The field IadeTutariTl must be between 0 and 9999999999999,99.", "İade tutarı (TL) 0 ile 9.999.999.999.999,99 arasında olmalıdır")]
    public void P3_Range_exclusive_ve_bilimsel_alt_sinir_TURKCE_ingilizce_SIZMAZ(string mesaj, string beklenen)
    {
        var alan = mesaj.Split(' ')[2];
        var sonuc = Kuyumcu.Core.I18n.DogrulamaSozlugu.Cevir(alan, mesaj);
        Assert.Equal(beklenen, sonuc);
        Assert.DoesNotContain("The field", sonuc);
    }

    [Fact]
    public void P3_AralikMesaji_sinirsiz_ust_sinir_E308_gizlenir()
        => Assert.Equal("Kredi limiti (TL) 0 veya daha büyük olmalıdır",
            Kuyumcu.Core.I18n.DogrulamaSozlugu.Cevir("KrediLimitiTl",
                "The field KrediLimitiTl must be between 0 and 1,79769313486232E+308."));

    // ═══ tur 2 (doğrulayıcı DÜŞÜK): rezerv adedinin TEK üst sınırı ═══
    [Theory]
    [InlineData("100000", "100000")]
    [InlineData("0.001", "0.001")]
    public void P3_rezerv_adet_kapisi_sinir_ICI_aynen_doner(string adet, string beklenen)
        => Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture),
            MSatis.SatisServisi.RezervAdetKapisi(decimal.Parse(adet, System.Globalization.CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("100000.001")]
    [InlineData("100001")]
    [InlineData("999999999.999")]
    [InlineData("0")]
    [InlineData("-1")]
    public void P3_rezerv_adet_kapisi_sinir_DISI_400_alan_adet(string adet)
    {
        var h = Assert.Throws<BadRequestException>(() =>
            MSatis.SatisServisi.RezervAdetKapisi(decimal.Parse(adet, System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Equal("adet", AlanIsareti.Alan(h));
    }

    [Fact]
    public void P3_rezerv_adet_ust_siniri_OLUSTURMA_DTO_Range_ile_TEK_kaynak()
    {
        var r = (System.ComponentModel.DataAnnotations.RangeAttribute)typeof(MSatis.RezervOlusturDto)
            .GetProperty(nameof(MSatis.RezervOlusturDto.Adet))!
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RangeAttribute), false).Single();
        Assert.Equal(MSatis.RezervOlusturDto.AdetUst, Convert.ToDouble(r.Maximum));
        Assert.Equal(100_000d, MSatis.RezervOlusturDto.AdetUst);
    }
}
