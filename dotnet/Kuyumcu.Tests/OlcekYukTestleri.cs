using System.Data.Common;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Arama;
using Kuyumcu.Api.Modules.Maliyet;
using Kuyumcu.Api.Modules.Yetki;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET OLCEK-YUK — çok kiracılı yük testi + TB-19 sınırsız liste düzeltmeleri.
//
// Kaynak: HAZIRLIK-KURULU-5.md madde 13. Yük altında (100 kiracı; büyük kiracıda
// 50.000 ürün · 5.000 cari · 180.000 hareket) ölçülen üç uç, kiracının TÜM
// ürün/cari kataloğunu her istekte belleğe alıyordu:
//   · GET /raporlar/kar-zarar/urun  → tüm ürün adları (sayfada 50 satır gösterilirken)
//   · GET /raporlar/vade-takibi     → tüm cari kartları (yalnız borçlular gerekirken)
//   · GET /arama?tur=FIS            → tüm cari adları (yalnız fişlerdeki cariler gerekirken)
// Düzeltme: sözlükler kimlik kümesine daraltıldı; yanıt sözleşmesi AYNI.
//
// Kanıt biçimi: (a) davranış — satır adları doğru ürün/cariyle eşleşir;
// (b) SQL — ad/kart sorgusu `IN (...)` ile daraltılmış çıkar (komut yakalayıcı).
// Yakalayıcı yalnız bu testlerin kendi bağlamına takılır; üretim boru hattının
// üç dinleyicisi (yazma denetçisi, denetim, MASAK hash) AYNEN kurulur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Yürütülen SELECT komutlarının metnini toplar (yalnız test bağlamında).</summary>
internal sealed class SqlKaydedici : DbCommandInterceptor
{
    public List<string> Komutlar { get; } = [];

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        lock (Komutlar) Komutlar.Add(command.CommandText);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        lock (Komutlar) Komutlar.Add(command.CommandText);
        return ValueTask.FromResult(result);
    }

    /// <summary>`FROM `tablo`` okuyan ve verilen sütunu seçen komutlar.</summary>
    public List<string> Okuyan(string tablo, string sutun) =>
        Komutlar.Where(k => k.Contains($"FROM `{tablo}`", StringComparison.Ordinal)
                         && k.Contains($".`{sutun}`", StringComparison.Ordinal)).ToList();
}

[Collection("veritabani")]
public sealed class OlcekYukTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private static JsonElement J(object o) =>
        JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    /// <summary>
    /// Sorgu birincil anahtarla, bir KİMLİK KÜMESİYLE daraltılmış mı? EF tek elemanlı
    /// kümeyi `= 'x'`, çok elemanlıyı `IN ('x', …)` olarak yazar; ikisi de sınırlıdır.
    /// Kiracının tamamını okuyan sorguda `id` süzgeci YOKTUR (yalnız tenant_id).
    /// </summary>
    private static bool KimlikleDaraltilmis(string sql) =>
        System.Text.RegularExpressions.Regex.IsMatch(sql, @"`\w+`\.`id` (IN \(|= )");

    /// <summary>Üretimle aynı boru hattı + SQL yakalayıcı; kiracıya sabitlenmiş bağlam.</summary>
    private KuyumcuDbContext Kaydeden(Kiraci k, SqlKaydedici kaydedici)
    {
        var ayar = new DbContextOptionsBuilder<KuyumcuDbContext>()
            .UseMySql(vt.Baglanti, ServerVersion.Parse("8.4.0-mysql"))
            .ReplaceService<IModelCacheKeyFactory, KiraciModelOnbellegi>()
            .AddInterceptors(
                new KiraciYazmaDenetcisi(NullLogger<KiraciYazmaDenetcisi>.Instance),
                new DenetimYazmaDinleyicisi(new HttpContextAccessor()),
                new MasakHashDinleyicisi(),
                kaydedici)
            .Options;
        return new KuyumcuDbContext(ayar, new SabitKiraciBaglami(k.TenantId), TestVeritabani.TestSifreleyici);
    }

    /// <summary>Kiracıya satışı olmayan ek ürünler ekler (katalog büyüklüğü taklidi).</summary>
    private static List<string> EkUrunler(KuyumcuDbContext db, Kiraci k, int adet, string onEk)
    {
        var now = DateTime.UtcNow;
        var idler = new List<string>();
        for (var i = 0; i < adet; i++)
        {
            var id = Kimlik.Ulid();
            idler.Add(id);
            db.Urunler.Add(new Urun
            {
                Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"{onEk}-{i}-{id[..6]}", Ad = $"{onEk} ürünü {i}",
                AyarId = "K22", Milyem = 916m, GramAgirlik = 5m, TasAgirlik = 0m, HasGrami = 4.58m,
                IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
            db.Stoklar.Add(new Stok
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = id,
                Adet = 10m, ToplamHasGram = 45.8m, Guncellendi = now,
            });
        }
        db.SaveChanges();
        return idler;
    }

    private static List<string> EkCariler(KuyumcuDbContext db, Kiraci k, int adet, string onEk)
    {
        var now = DateTime.UtcNow;
        var idler = new List<string>();
        for (var i = 0; i < adet; i++)
        {
            var id = Kimlik.Ulid();
            idler.Add(id);
            db.Cariler.Add(new Cari
            {
                Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = $"{onEk} carisi {i}",
                Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
        }
        db.SaveChanges();
        return idler;
    }

    private static Task VeresiyeSat(KuyumcuDbContext db, Kiraci k, string urunId, decimal birim) =>
        Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = urunId, Adet = 1m, BirimFiyatTl = birim,
            OdemeTuru = "VERESIYE", KasaId = null, KurSnapshotId = k.KurId, VadeGun = 0,
        }, k.Islem);

    // ── 1) Ürün bazlı kâr: adlar yalnız SAYFADAKİ ürünler için ──────────────

    [Fact]
    public async Task OY01_Urun_bazli_kar_urun_adlarini_yalniz_sayfadaki_urunlerden_okur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "OY01-urun-adi", stokAdet: 20m);
        EkUrunler(kurulum, k, 6, "OY01-satilmayan");
        using (var db = vt.Olustur(k)) await VeresiyeSat(db, k, k.UrunId, 4_000m);

        var kaydedici = new SqlKaydedici();
        using var olcum = Kaydeden(k, kaydedici);
        var r = J(await new KarZararServisi(olcum).UrunBazli(k.Islem, Bugun, Bugun, null, 50));

        var satir = Assert.Single(r.GetProperty("satirlar").EnumerateArray());
        var urun = kurulum.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId);
        Assert.Equal(k.UrunId, satir.GetProperty("urunId").GetString());
        Assert.Equal(urun.Kod, satir.GetProperty("kod").GetString());
        Assert.Equal(urun.Ad, satir.GetProperty("ad").GetString());

        // Ad sorgusu kimlik kümesiyle daraltılmış olmalı — tüm katalog okunmaz.
        var adSorgulari = kaydedici.Okuyan("urunler", "kod");
        Assert.NotEmpty(adSorgulari);
        Assert.All(adSorgulari, s => Assert.True(KimlikleDaraltilmis(s), s));
    }

    [Fact]
    public async Task OY02_Urun_bazli_kar_limit_sonrasi_adlar_dogru_urunle_eslesir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "OY02-esles", stokAdet: 20m);
        var ikinci = EkUrunler(kurulum, k, 1, "OY02-ikinci")[0];
        using (var db = vt.Olustur(k))
        {
            await VeresiyeSat(db, k, k.UrunId, 4_000m);
            await VeresiyeSat(db, k, ikinci, 6_000m);
        }

        using var olcum = vt.Olustur(k);
        var r = J(await new KarZararServisi(olcum).UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var satirlar = r.GetProperty("satirlar").EnumerateArray().ToList();
        Assert.Equal(2, satirlar.Count);
        var adlar = kurulum.Urunler.AsNoTracking()
            .Where(u => u.TenantId == k.TenantId).ToDictionary(u => u.Id, u => (u.Kod, u.Ad));
        foreach (var s in satirlar)
        {
            var id = s.GetProperty("urunId").GetString()!;
            Assert.Equal(adlar[id].Kod, s.GetProperty("kod").GetString());
            Assert.Equal(adlar[id].Ad, s.GetProperty("ad").GetString());
        }

        // limit=1: tek satır döner, adı KENDİ ürününün adıdır; dahaFazlasiVar sözleşmesi korunur.
        var bir = J(await new KarZararServisi(olcum).UrunBazli(k.Islem, Bugun, Bugun, null, 1));
        var tek = Assert.Single(bir.GetProperty("satirlar").EnumerateArray());
        var tekId = tek.GetProperty("urunId").GetString()!;
        Assert.Equal(adlar[tekId].Ad, tek.GetProperty("ad").GetString());
        Assert.True(bir.GetProperty("dahaFazlasiVar").GetBoolean());
    }

    // ── 2) Vade takibi: yalnız borçlu carilerin kartı ───────────────────────

    [Fact]
    public async Task OY03_Vade_takibi_yalniz_borclu_carilerin_kartini_okur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "OY03-vade", stokAdet: 20m);
        EkCariler(kurulum, k, 5, "OY03-borcsuz");
        using (var db = vt.Olustur(k)) await VeresiyeSat(db, k, k.UrunId, 3_000m);

        var kaydedici = new SqlKaydedici();
        using var olcum = Kaydeden(k, kaydedici);
        var yanit = J(await TestKurulum.RaporServisi(olcum).VadeTakibi(k.Islem, 7));

        var satir = Assert.Single(yanit.GetProperty("cariler").EnumerateArray());
        Assert.Equal(k.CariId, satir.GetProperty("cariId").GetString());
        Assert.Equal("OY03-vade müşterisi", satir.GetProperty("cariAd").GetString());

        var kartSorgulari = kaydedici.Okuyan("cariler", "ad");
        Assert.NotEmpty(kartSorgulari);
        Assert.All(kartSorgulari, s => Assert.True(KimlikleDaraltilmis(s), s));
    }

    [Fact]
    public async Task OY04_Vade_takibi_borclu_yoksa_cari_tablosuna_hic_gitmez()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "OY04-vade-bos", stokAdet: 20m);
        EkCariler(kurulum, k, 3, "OY04-borcsuz");

        var kaydedici = new SqlKaydedici();
        using var olcum = Kaydeden(k, kaydedici);
        var yanit = J(await TestKurulum.RaporServisi(olcum).VadeTakibi(k.Islem, 7));

        Assert.Empty(yanit.GetProperty("cariler").EnumerateArray());
        Assert.Empty(kaydedici.Okuyan("cariler", "ad"));
    }

    // ── 3) Fiş araması: yalnız fişlerdeki carilerin adı ─────────────────────

    [Fact]
    public async Task OY05_Fis_aramasi_cari_adini_yalniz_fislerdeki_carilerden_okur_ve_bulur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "OY05-fis", stokAdet: 20m);
        // Aynı adı taşıyan ama fişi OLMAYAN cari: aramada fiş üretmemeli.
        EkCariler(kurulum, k, 4, "OY05-fis müşterisi");
        using (var db = vt.Olustur(k))
            await Satis(db).SatisFisi(new MSatis.SatisFisiDto
            {
                CariId = k.CariId, KurSnapshotId = k.KurId,
                Kalemler = [new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 2_500m }],
                Odemeler = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 2_500m, KasaId = k.KasaId }],
            }, k.Islem);

        var kaydedici = new SqlKaydedici();
        using var olcum = Kaydeden(k, kaydedici);
        var servis = new AramaServisi(olcum, new YetkiServisi(olcum, new MemoryCache(new MemoryCacheOptions())));
        var yanit = await servis.Ara("OY05-fis müşterisi", "FIS", 20, 0, null, k.Islem);

        var sonuc = Assert.Single(yanit.Sonuclar);
        Assert.StartsWith("OY05-fis müşterisi ·", sonuc.Ad);

        // Ad SÖZLÜĞÜ (yalnız cariler tablosundan okuyan sorgu) kimlik kümesiyle daraltılmış olmalı.
        // Hareket sorgusunun içindeki ad-eşleşme ALT SORGUSU sözlük değildir, burada sayılmaz (OY07 ona bakar).
        var adSorgulari = kaydedici.Okuyan("cariler", "ad")
            .Where(s => !s.Contains("FROM `hareketler`", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(adSorgulari);
        Assert.All(adSorgulari, s => Assert.True(KimlikleDaraltilmis(s), s));
    }

    // ── 3b) Fiş araması: eşleşme ve sayfalama SUNUCUDA (180.000 fişte ~500 ms ölçüldü) ──

    private static async Task<string> FisKes(KuyumcuDbContext db, Kiraci k, string? cariId, decimal tutar)
    {
        var r = J(await Satis(db).SatisFisi(new MSatis.SatisFisiDto
        {
            CariId = cariId, KurSnapshotId = k.KurId,
            Kalemler = [new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar }],
            Odemeler = [new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = tutar, KasaId = k.KasaId }],
        }, k.Islem));
        var p = r.EnumerateObject().Single(x => string.Equals(x.Name, "fisId", StringComparison.OrdinalIgnoreCase));
        return p.Value.GetString()!;
    }

    [Fact]
    public async Task OY07_Fis_aramasi_eslesme_ve_sayfalama_sunucuda_yapilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "OY07-sayfa", stokAdet: 20m);
        var baska = EkCariler(kurulum, k, 1, "OY07-baska")[0];
        var kendi = new List<string>();
        string baskaFis;
        using (var db = vt.Olustur(k))
        {
            for (var i = 0; i < 3; i++) kendi.Add(await FisKes(db, k, k.CariId, 1_000m + i));
            baskaFis = await FisKes(db, k, baska, 2_000m);
            await FisKes(db, k, null, 3_000m);   // carisiz fiş: ad eşleşmesine girmez
        }
        // PAKET OLCEK-YUK t2 (doğrulayıcı ORTA, MX1): "en yeni fiş önce" sırası SQL'e taşındı; sabit beklenen
        // sırayla korunur. Tarihler kesim sırasından, fiş kimliği sırasından ve tersinden FARKLI bir sıra verir:
        // en yeni kendi[1] (−1 gün) · kendi[2] (−2 gün) · kendi[0] (−3 gün).
        var simdi = DateTime.UtcNow;
        foreach (var (fis, gunOnce) in new[] { (kendi[1], 1), (kendi[2], 2), (kendi[0], 3) })
            await kurulum.Database.ExecuteSqlRawAsync(
                "UPDATE hareketler SET tarih = {0} WHERE tenant_id = {1} AND fis_id = {2}",
                simdi.AddDays(-gunOnce), k.Islem.TenantId, fis);

        var kaydedici = new SqlKaydedici();
        using var olcum = Kaydeden(k, kaydedici);
        var servis = new AramaServisi(olcum, new YetkiServisi(olcum, new MemoryCache(new MemoryCacheOptions())));

        // Cari adıyla: yalnız KENDİ carisinin 3 fişi; 2'lik sayfalar örtüşmez, 'daha var' sayarak söylenir.
        var s1 = await servis.Ara("OY07-sayfa müşterisi", "FIS", 2, 0, null, k.Islem);
        var s2 = await servis.Ara("OY07-sayfa müşterisi", "FIS", 2, 2, null, k.Islem);
        Assert.Equal(2, s1.Sonuclar.Count);
        Assert.True(s1.DahaVar);
        Assert.Single(s2.Sonuclar);
        Assert.False(s2.DahaVar);
        var gelen = s1.Sonuclar.Concat(s2.Sonuclar).Select(x => x.Kimlik).ToList();
        Assert.Equal(kendi.OrderBy(x => x), gelen.OrderBy(x => x));
        // Sıra: en yeni fiş önce, sayfalar arasında da (sabit beklenen liste).
        Assert.Equal(new[] { kendi[1], kendi[2], kendi[0] }, gelen);
        Assert.DoesNotContain(baskaFis, gelen);
        Assert.All(s1.Sonuclar.Concat(s2.Sonuclar), x => Assert.StartsWith("1 kalem ·", x.AltBilgi));

        // Fiş kimliğiyle: diğer carinin fişi bulunur, adı KENDİ carisinin adıdır.
        var f = Assert.Single((await servis.Ara(baskaFis, "FIS", 20, 0, null, k.Islem)).Sonuclar);
        Assert.Equal(baskaFis, f.Kimlik);
        Assert.StartsWith("OY07-baska carisi 0 ·", f.Ad);

        // Yok: boş sonuç (hata değil).
        Assert.Empty((await servis.Ara("OY07-hicbiryerde-yok", "FIS", 20, 0, null, k.Islem)).Sonuclar);

        // SQL: fişleri gruplayan sorgu sayfayı SUNUCUDA keser (LIMIT) — tüm fişler belleğe gelmez.
        var gruplama = kaydedici.Komutlar
            .Where(c => c.Contains("FROM `hareketler`", StringComparison.Ordinal)
                     && c.Contains("GROUP BY", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(gruplama);
        Assert.Contains(gruplama, c => c.Contains("LIMIT", StringComparison.Ordinal)
                                     && (c.Contains("LIKE", StringComparison.Ordinal) || c.Contains("LOCATE", StringComparison.Ordinal)));
        Assert.All(gruplama, c => Assert.True(
            c.Contains("LIMIT", StringComparison.Ordinal) || c.Contains("`fis_id` IN (", StringComparison.Ordinal)
                || c.Contains("`fis_id` = ", StringComparison.Ordinal), c));
    }

    // ── PAKET OLCEK-YUK t2 (tasarım hakemi KRİTİK, UX-S29): ürün listesi sunucu sınırı ──
    // Sunucu dönemde satılan TOPLAM ürün sayısını döner (ekran türetmez); ofset ile sonraki
    // sayfa istenir, sayfalar örtüşmez ve tek seferde alınan listeyle AYNI sıradadır.
    [Fact]
    public async Task OY08_Urun_bazli_kar_toplam_sayi_ve_ofset_sayfasi_sunucudan()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "OY08-sayfa", stokAdet: 20m);
        var ek = EkUrunler(kurulum, k, 2, "OY08-ek");
        using (var db = vt.Olustur(k))
        {
            await VeresiyeSat(db, k, k.UrunId, 4_000m);
            await VeresiyeSat(db, k, ek[0], 5_000m);
            await VeresiyeSat(db, k, ek[1], 6_000m);
        }

        using var olcum = vt.Olustur(k);
        var servis = new KarZararServisi(olcum);
        var tum = J(await servis.UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var tumIdler = tum.GetProperty("satirlar").EnumerateArray().Select(s => s.GetProperty("urunId").GetString()!).ToList();
        Assert.Equal(3, tumIdler.Count);
        Assert.Equal(3, tum.GetProperty("toplamUrunSayisi").GetInt32());
        Assert.False(tum.GetProperty("dahaFazlasiVar").GetBoolean());

        var s1 = J(await servis.UrunBazli(k.Islem, Bugun, Bugun, null, 2, 0));
        Assert.Equal(2, s1.GetProperty("urunSayisi").GetInt32());
        Assert.Equal(3, s1.GetProperty("toplamUrunSayisi").GetInt32());
        Assert.True(s1.GetProperty("dahaFazlasiVar").GetBoolean());
        Assert.Equal(0, s1.GetProperty("ofset").GetInt32());

        var s2 = J(await servis.UrunBazli(k.Islem, Bugun, Bugun, null, 2, 2));
        Assert.Equal(1, s2.GetProperty("urunSayisi").GetInt32());
        Assert.Equal(3, s2.GetProperty("toplamUrunSayisi").GetInt32());
        Assert.False(s2.GetProperty("dahaFazlasiVar").GetBoolean());
        Assert.Equal(2, s2.GetProperty("ofset").GetInt32());

        var sayfali = s1.GetProperty("satirlar").EnumerateArray().Concat(s2.GetProperty("satirlar").EnumerateArray())
            .Select(s => s.GetProperty("urunId").GetString()!).ToList();
        Assert.Equal(tumIdler, sayfali);   // örtüşme 0, eksik 0, sıra aynı

        var bos = J(await servis.UrunBazli(k.Islem, Bugun, Bugun, null, 2, 3));
        Assert.Empty(bos.GetProperty("satirlar").EnumerateArray());
        Assert.False(bos.GetProperty("dahaFazlasiVar").GetBoolean());

        await Assert.ThrowsAsync<BadRequestException>(() => servis.UrunBazli(k.Islem, Bugun, Bugun, null, 2, -1));
    }

    // ── 4) Yük betiği sözleşmesi (saf): kabul eşikleri gevşetilemez ─────────

    [Fact]
    public void OY06_Yuk_betigi_kabul_esiklerini_tasir_ve_oran_sinirini_gevsetmez()
    {
        var kok = YayinDosyaSozlesmesiTestleri.ProjeKoku();
        var yol = Path.Combine(kok, "dokumanlar", "09-uygulama-kayitlari", "betikler", "yuk", "k6-cok-kiraci.js");
        Assert.True(File.Exists(yol), $"yük betiği yok: {yol}");
        var metin = File.ReadAllText(yol);
        Assert.Contains("'http_req_duration{faz:kararli}': ['p(95)<800']", metin);
        Assert.Contains("'http_req_failed{faz:kararli}': ['rate<0.01']", metin);
        Assert.Contains("'sayi_429': ['count<1']", metin);
        // Her kiracı ayrı hat; oran sınırı kapatılmaz (Program.cs'e dokunan bayrak yok).
        Assert.Contains("X-Forwarded-For", metin);
        Assert.DoesNotContain("RateLimit", metin);
    }
}
