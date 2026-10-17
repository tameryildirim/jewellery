using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Data.Migrations;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MCari = Kuyumcu.Api.Modules.Cari;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P2 · MÜŞTERİ
//   #8  cari detay: satır has'ı toplamı ≠ bakiye + ham hareket kodu ("SATIS")
//   #22 uyruk iki standart (cari 2 harf ↔ MASAK 3 harf)
//   #5  vade '1,5' sessizce 1'e kırpılıyordu
//   #21 alt madde: düzeltme fişi penceresinde iki sayaç
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KesifP2HttpTestleri(HttpFikstur f)
{
    private const string Gerekce = "KESIF-P2 test düzeltmesi — yeterince uzun gerekçe";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PAKETKESIF-P2-TEST-{ad}-{Guid.NewGuid():N}"[..40]);
    }

    private static async Task Tamam(HttpResponseMessage r, string ne)
    {
        var g = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, $"{ne}: {(int)r.StatusCode} {g}");
    }

    private static decimal Dec(JsonElement e) => e.GetDecimal();

    /// <summary>
    /// Kabul #8: satış, tahsilat, emanet (altın + gümüş), düzeltme ve storno karışık tohumda
    /// kiracının HER carisi için satırların cari has etkisi toplamı = Has Gram bakiyesi (±0,0001).
    /// </summary>
    [Fact]
    public async Task CariEkstre_satir_cari_has_toplami_bakiyeye_esit()
    {
        var k = YeniKiraci("ekstre");
        var c = f.Ortam.Istemci(k, "SAHIP");

        // İkinci cari (her cari sınansın).
        var yeni = await c.PostJson("/api/v1/cari", new { ad = "KESIF-P2 ikinci müşteri", tur = "MUSTERI" });
        await Tamam(yeni, "cari oluştur");
        var ikinciId = (await yeni.Json()).GetProperty("id").GetString()!;

        foreach (var cariId in new[] { k.CariId, ikinciId })
        {
            var v = await c.PostJson($"/api/v1/cari/{cariId}/veresiye",
                new { tlTutari = 1_000m, hasGrami = 2.5m, aciklama = "veresiye", kurSnapshotId = k.KurId });
            await Tamam(v, "veresiye");
            await Tamam(await c.PostJson($"/api/v1/cari/{cariId}/tahsilat",
                new { tlTutari = 100m, hasGrami = 1.2345m, odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId }), "tahsilat");
            await Tamam(await c.PostJson($"/api/v1/cari/{cariId}/duzeltme",
                new { tlDelta = 0m, hasDelta = -0.3333m, gerekce = Gerekce, kurSnapshotId = k.KurId }), "düzeltme");
            await Tamam(await c.PostJson("/api/v1/emanet/giris",
                new { cariId, ayarId = "K22", gramAgirlik = 10m, aciklama = "altın emanet", kurSnapshotId = k.KurId }), "emanet altın");
            await Tamam(await c.PostJson("/api/v1/emanet/giris",
                new { cariId, ayarId = "G925", gramAgirlik = 100m, aciklama = "gümüş emanet", kurSnapshotId = k.KurId }), "emanet gümüş");
            await Tamam(await c.PostJson($"/api/v1/emanet/{cariId}/cikis",
                new { ayarId = "K22", hasGrami = 3m, aciklama = "altın emanet iadesi", kurSnapshotId = k.KurId }), "emanet çıkış");
        }

        // Satış (has'ı olan SATIS satırı: bakiye has'ına DOKUNMAZ) — ilk cari.
        await Tamam(await c.PostJson("/api/v1/islemler/satis", new
        {
            urunId = k.UrunId, adet = 1m, birimFiyatTl = 5_000m, odemeTuru = "NAKIT", kasaId = k.KasaId,
            cariId = k.CariId, kurSnapshotId = k.KurId,
        }), "satış");

        // Veresiye STORNOSU — ikinci cari (storno satırı da bakiyeyi yazar).
        using (var db = f.Vt.OlusturSistem())
        {
            var vid = await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.CariId == ikinciId && h.Tur == "VERESIYE")
                .Select(h => h.Id).FirstAsync();
            await Tamam(await c.PostJson($"/api/v1/islemler/{vid}/storno",
                new { gerekce = Gerekce, kurSnapshotId = k.KurId }), "storno");

            // Gümüş emanet STORNOSU: StornoServisi.CariEtkisi madenden bağımsız −hasGrami tersler
            // (bakiye gerçekten oynar) — ekstre aynası bunu da toplamalıdır.
            var gid = await db.Hareketler.AsNoTracking()
                .Where(h => h.TenantId == k.TenantId && h.CariId == ikinciId && h.Tur == "EMANET_GIRIS" && h.Maden == "GUMUS")
                .Select(h => h.Id).FirstAsync();
            await Tamam(await c.PostJson($"/api/v1/islemler/{gid}/storno",
                new { gerekce = Gerekce, kurSnapshotId = k.KurId }), "gümüş emanet stornosu");
        }

        var liste = await (await c.GetAsync("/api/v1/cari?limit=500")).Json();
        var idler = liste.EnumerateArray().Select(x => x.GetProperty("id").GetString()!).ToList();
        Assert.Contains(k.CariId, idler);
        Assert.Contains(ikinciId, idler);

        var sinandi = 0;
        foreach (var id in idler)
        {
            var r = await c.GetAsync($"/api/v1/cari/{id}/ekstre?limit=1000");
            await Tamam(r, "ekstre");
            var j = await r.Json();
            // PAKET CARI-BAKIYE (tek tanım): bakiye.hasGramBakiye artık EMANETSİZ; kayıtlı kolon ayrı alanda.
            var kayitli = Dec(j.GetProperty("bakiye").GetProperty("kayitliHasGramBakiye"));
            // Tur 3 (ekip kararı 3): üstteki bakiye EMANETSİZ; satırların bakiye etkisi toplamı ona eşit,
            // emanet ayrı "Emanette" ve kayıtlı = emanetsiz − emanette (tanım değişmedi).
            var bakiye = Dec(j.GetProperty("bakiyeEmanetsizHas"));
            var emanette = Dec(j.GetProperty("emanetteHas"));
            var satirlar = j.GetProperty("hareketler").EnumerateArray().ToList();
            var toplam = satirlar.Sum(s => Dec(s.GetProperty("bakiyeHasEtkisi")));
            Assert.True(Math.Abs(toplam - bakiye) <= 0.0001m, $"cari {id}: satırlar {toplam} ≠ bakiye {bakiye}");
            Assert.True(Math.Abs(bakiye - emanette - kayitli) <= 0.0001m, $"cari {id}: emanetsiz {bakiye} − emanette {emanette} ≠ kayıtlı {kayitli}");
            Assert.True(Math.Abs(satirlar.Sum(s => Dec(s.GetProperty("emanetHas"))) - emanette) <= 0.0001m, $"cari {id}: emanet satırları ≠ Emanette");
            Assert.All(satirlar.Where(s => s.GetProperty("emanet").GetBoolean()), s =>
            {
                Assert.Equal(0m, Dec(s.GetProperty("bakiyeHasEtkisi")));
                Assert.False(s.GetProperty("hasBakiyesineGirer").GetBoolean());
            });
            Assert.True(Math.Abs(Dec(j.GetProperty("sayfaDisiHasEtkisi"))) <= 0.0001m);
            Assert.True(Math.Abs(Dec(j.GetProperty("bakiyeUyusmazligiHas"))) <= 0.0001m, $"cari {id}: uyuşmazlık");
            Assert.False(j.GetProperty("dahaFazlaVar").GetBoolean());
            sinandi++;
        }
        Assert.True(sinandi >= 2);

        // Anlamlılık: ilk carinin bakiyesi SIFIR DEĞİL ve eski yol (satır has'ı toplamı) BAKİYEYİ VERMEZ.
        var ilk = await (await c.GetAsync($"/api/v1/cari/{k.CariId}/ekstre?limit=1000")).Json();
        var ilkSatirlar = ilk.GetProperty("hareketler").EnumerateArray().ToList();
        var ilkBakiye = Dec(ilk.GetProperty("bakiyeEmanetsizHas"));
        Assert.NotEqual(0m, ilkBakiye);
        // Altın emanet (10 g K22 giriş, 3 has çıkış) kayıtlı bakiyede; üstte değil, "Emanette"de.
        Assert.NotEqual(0m, Dec(ilk.GetProperty("emanetteHas")));
        // PAKET CARI-BAKIYE: kayıtlı kolon (emanet dahil) artık ayrı alanda; bakiye.hasGramBakiye emanetsizdir.
        Assert.NotEqual(Dec(ilk.GetProperty("bakiye").GetProperty("kayitliHasGramBakiye")), ilkBakiye);
        Assert.Equal(Dec(ilk.GetProperty("bakiye").GetProperty("hasGramBakiye")), ilkBakiye);
        Assert.True(Math.Abs(ilkSatirlar.Sum(s => Dec(s.GetProperty("hasGrami"))) - ilkBakiye) > 0.0001m);

        var satis = Assert.Single(ilkSatirlar, s => s.GetProperty("tur").GetString() == "SATIS");
        Assert.NotEqual(0m, Dec(satis.GetProperty("hasGrami")));
        Assert.Equal(0m, Dec(satis.GetProperty("cariHasEtkisi")));
        Assert.False(satis.GetProperty("hasBakiyesineGirer").GetBoolean());

        var gumus = Assert.Single(ilkSatirlar, s => s.GetProperty("tur").GetString() == "EMANET_GIRIS"
                                                     && s.GetProperty("maden").GetString() == "GUMUS");
        Assert.True(gumus.GetProperty("emanet").GetBoolean());
        Assert.False(gumus.GetProperty("hasBakiyesineGirer").GetBoolean());
        Assert.Equal(0m, Dec(gumus.GetProperty("cariHasEtkisi")));

        var altin = Assert.Single(ilkSatirlar, s => s.GetProperty("tur").GetString() == "EMANET_GIRIS"
                                                     && s.GetProperty("maden").GetString() == "ALTIN");
        // Karar 3: altın emanet de bakiyeye GİRMEZ (ekran etkisi 0); kayıtlı bakiye aynası (cariHasEtkisi) korunur.
        Assert.False(altin.GetProperty("hasBakiyesineGirer").GetBoolean());
        Assert.Equal(0m, Dec(altin.GetProperty("bakiyeHasEtkisi")));
        Assert.Equal(-Dec(altin.GetProperty("hasGrami")), Dec(altin.GetProperty("cariHasEtkisi")));
        Assert.Equal(Dec(altin.GetProperty("hasGrami")), Dec(altin.GetProperty("emanetHas")));

        // Sayfalı okuma: satırlar + sayfa dışı = bakiye, sınır söylenir (UX-S29).
        var sayfa = await (await c.GetAsync($"/api/v1/cari/{k.CariId}/ekstre?limit=2")).Json();
        var sayfaSatir = sayfa.GetProperty("hareketler").EnumerateArray().ToList();
        Assert.Equal(2, sayfaSatir.Count);
        Assert.True(sayfa.GetProperty("dahaFazlaVar").GetBoolean());
        var sayfaToplam = sayfaSatir.Sum(s => Dec(s.GetProperty("bakiyeHasEtkisi"))) + Dec(sayfa.GetProperty("sayfaDisiHasEtkisi"));
        Assert.True(Math.Abs(sayfaToplam - ilkBakiye) <= 0.0001m);
    }

    /// <summary>
    /// Tur 2 (hakem, #8 — demo kiracısı): kayıtlı bakiye işlemlerle TUTMUYORSA (09.09 tohum verisi gibi)
    /// fark "sayfa dışı" sayılmaz; ayrı `bakiyeUyusmazligiHas` alanında söylenir. Sayfalı okumada da
    /// satırlar + sayfa dışı + uyuşmazlık = bakiye.
    /// </summary>
    [Fact]
    public async Task CariEkstre_bakiye_uyusmazligi_ayri_soylenir()
    {
        var k = YeniKiraci("uyusmazlik");
        var c = f.Ortam.Istemci(k, "SAHIP");
        await Tamam(await c.PostJson($"/api/v1/cari/{k.CariId}/veresiye",
            new { tlTutari = 1_000m, hasGrami = 2.5m, aciklama = "veresiye", kurSnapshotId = k.KurId }), "veresiye");
        await Tamam(await c.PostJson($"/api/v1/cari/{k.CariId}/tahsilat",
            new { tlTutari = 100m, hasGrami = 1.2345m, odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId }), "tahsilat");

        var temiz = await (await c.GetAsync($"/api/v1/cari/{k.CariId}/ekstre")).Json();
        Assert.Equal(0m, Dec(temiz.GetProperty("bakiyeUyusmazligiHas")));

        // Tohum verisindeki gibi bozuk kayıtlı bakiye (defter dışı yazılmış +77,86).
        using (var db = f.Vt.OlusturSistem())
        {
            var b = await db.CariBakiyeler.FirstAsync(x => x.TenantId == k.TenantId && x.CariId == k.CariId);
            b.HasGramBakiye += 77.86m;
            await db.SaveChangesAsync();
        }

        var j = await (await c.GetAsync($"/api/v1/cari/{k.CariId}/ekstre")).Json();
        Assert.False(j.GetProperty("dahaFazlaVar").GetBoolean());
        Assert.Equal(0m, Dec(j.GetProperty("sayfaDisiHasEtkisi")));          // "eski işlem" DEĞİL
        Assert.Equal(77.86m, Dec(j.GetProperty("bakiyeUyusmazligiHas")));

        var s = await (await c.GetAsync($"/api/v1/cari/{k.CariId}/ekstre?limit=1")).Json();
        Assert.True(s.GetProperty("dahaFazlaVar").GetBoolean());
        var sayfaSatir = s.GetProperty("hareketler").EnumerateArray().ToList();
        Assert.Single(sayfaSatir);
        Assert.NotEqual(0m, Dec(s.GetProperty("sayfaDisiHasEtkisi")));
        Assert.Equal(77.86m, Dec(s.GetProperty("bakiyeUyusmazligiHas")));
        var bakiye = Dec(s.GetProperty("bakiye").GetProperty("hasGramBakiye"));
        Assert.Equal(Dec(s.GetProperty("bakiyeEmanetsizHas")), bakiye);   // emanet yok → emanetsiz = kayıtlı
        Assert.Equal(bakiye, sayfaSatir.Sum(x => Dec(x.GetProperty("bakiyeHasEtkisi")))
                             + Dec(s.GetProperty("sayfaDisiHasEtkisi")) + Dec(s.GetProperty("bakiyeUyusmazligiHas")));
    }

    /// <summary>Kabul #22: 'TR' → 400 + Türkçe mesaj; 'TUR' → 200; eşlenemeyen eski değer değişmeden korunur.</summary>
    [Fact]
    public async Task Cari_uyruk_2_harf_400_3_harf_200()
    {
        var k = YeniKiraci("uyruk");
        var c = f.Ortam.Istemci(k, "SAHIP");

        var iki = await c.PostJson("/api/v1/cari", new { ad = "Uyruk iki harf", tur = "MUSTERI", uyruk = "TR" });
        Assert.Equal(HttpStatusCode.BadRequest, iki.StatusCode);
        var mesaj = await iki.Metin();
        Assert.Contains("3 harfli", mesaj, StringComparison.Ordinal);

        var uc = await c.PostJson("/api/v1/cari", new { ad = "Uyruk üç harf", tur = "MUSTERI", uyruk = "tur" });
        Assert.Equal(HttpStatusCode.Created, uc.StatusCode);
        var id = (await uc.Json()).GetProperty("id").GetString()!;
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal("TUR", db.Cariler.AsNoTracking().Single(x => x.Id == id).Uyruk);   // büyük harfe normallenir

        Assert.Equal(HttpStatusCode.BadRequest, (await c.PatchJson($"/api/v1/cari/{id}", new { uyruk = "DE" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PatchJson($"/api/v1/cari/{id}", new { uyruk = "ZZZ" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson($"/api/v1/cari/{id}", new { uyruk = "DEU" })).StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal("DEU", db.Cariler.AsNoTracking().Single(x => x.Id == id).Uyruk);

        // Eşlenemeyen eski değer (migration'ın bıraktığı 'ZZ'): DEĞİŞMEDEN gönderilirse kart kaydedilir.
        using (var db = f.Vt.OlusturSistem())
            await db.Database.ExecuteSqlRawAsync("UPDATE cariler SET uyruk = 'ZZ' WHERE id = {0}", id);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchJson($"/api/v1/cari/{id}", new { uyruk = "ZZ", ad = "Uyruk eski" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PatchJson($"/api/v1/cari/{id}", new { uyruk = "YY" })).StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal("ZZ", db.Cariler.AsNoTracking().Single(x => x.Id == id).Uyruk);   // uydurulmadı

        // Servis yolu (DataAnnotations koşmaz) da aynı kuralı taşır.
        var hata = Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() => MCari.CariServisi.UyrukNormalle("TR", null));
        Assert.Contains("3 harfli", hata.Message, StringComparison.Ordinal);
    }
}

[Collection("veritabani")]
public sealed class KesifP2MigrationTestleri(TestVeritabani vt)
{
    [Fact]
    public void Uyruk_eslemesi_249_satir_ve_her_hedef_ISO_alpha3()
    {
        Assert.Equal(249, UyrukEslemesi.Alpha2Alpha3.Count);
        Assert.All(UyrukEslemesi.Alpha2Alpha3.Values, a3 => Assert.Contains(a3, UlkeListesi.IsoKodlari));
        Assert.Equal("TUR", UyrukEslemesi.Esle(" tr "));
        Assert.Equal("ESH", UyrukEslemesi.Esle("EH"));
        Assert.Null(UyrukEslemesi.Esle("ZZ"));
    }

    /// <summary>
    /// Tur 2 (doğrulayıcı DÜŞÜK): migration eşlemesi DONDURULMUŞTUR — çalışma anındaki sözlükten
    /// üretilmez. Donuk liste 249 çift, sözlükle birebir aynı; sözlük değişirse bu test kırmızı olur
    /// (yeni migration yazılır, UyrukAlpha3 değişmez). SQL metni sözlüğe bağlı değildir.
    /// </summary>
    [Fact]
    public void Uyruk_eslemesi_migrationda_donuk_ve_sozlukle_ayni()
    {
        var donuk = UyrukAlpha3.DonukCiftler();
        Assert.Equal(249, donuk.Count);
        Assert.Equal(249, donuk.Select(c => c.A2).Distinct().Count());
        Assert.Equal(UyrukEslemesi.Alpha2Alpha3.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => (kv.Key, kv.Value)),
                     donuk.OrderBy(c => c.A2, StringComparer.Ordinal).Select(c => (c.A2, c.A3)));
        var sql = UyrukAlpha3.EslemeSql();
        Assert.Contains("WHEN 'TR' THEN 'TUR'", sql);
        Assert.Contains("WHEN 'EH' THEN 'ESH'", sql);
        Assert.Equal(249, Regex.Matches(sql, @"WHEN '[A-Z]{2}' THEN '[A-Z]{3}'").Count);
        Assert.Equal(249, Regex.Matches(UyrukAlpha3.GeriSql(), @"WHEN '[A-Z]{3}' THEN '[A-Z]{2}'").Count);
        // Kaynak metinde sözlüğe çalışma anı bağı yok.
        var kaynak = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Api", "Data", "Migrations", "20260930115854_UyrukAlpha3.cs"));
        Assert.DoesNotContain("UyrukEslemesi.Alpha2Alpha3.", kaynak);
    }

    /// <summary>Kabul #22: migration TR→TUR eşler; eşlenemeyen değer uydurulmaz, korunur ve günlüğe yazılır.</summary>
    [Fact]
    public async Task Migration_uyruk_TR_TUR_eslenemeyen_korunur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETKESIF-P2-TEST migration");
        var idTr = Kimlik.Ulid();
        var idDe = Kimlik.Ulid();
        var idZz = Kimlik.Ulid();
        var idTur = Kimlik.Ulid();
        using (var db = vt.Olustur(k))
        {
            var now = DateTime.UtcNow;
            foreach (var (id, u) in new[] { (idTr, "TR"), (idDe, "de"), (idZz, "ZZ"), (idTur, "TUR") })
                db.Cariler.Add(new Cari
                {
                    Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = $"göç {u}", Aktif = true,
                    Uyruk = u, Olusturuldu = now, Guncellendi = now,
                });
            await db.SaveChangesAsync();
        }

        using (var db = vt.OlusturSistem())
            await db.Database.ExecuteSqlRawAsync(UyrukAlpha3.EslemeSql());

        using var kontrol = vt.OlusturSistem();
        string? U(string id) => kontrol.Cariler.AsNoTracking().Single(x => x.Id == id).Uyruk;
        Assert.Equal("TUR", U(idTr));
        Assert.Equal("DEU", U(idDe));
        Assert.Equal("ZZ", U(idZz));      // eşlenemedi → UYDURULMADI
        Assert.Equal("TUR", U(idTur));    // zaten 3 harf → dokunulmadı

        var gunluk = kontrol.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "CariUyrukGocu").ToList();
        Assert.Equal(3, gunluk.Count);   // TR, de, ZZ (TUR günlüğe girmez)
        var zz = Assert.Single(gunluk, d => d.VarlikId == idZz);
        Assert.Contains("EŞLENEMEDİ", zz.Detay, StringComparison.Ordinal);
        Assert.DoesNotContain("ZZ", zz.Detay, StringComparison.Ordinal);   // kişisel değer günlüğe yazılmaz
        Assert.Contains(gunluk, d => d.VarlikId == idTr && d.Detay!.Contains("eşlendi"));
    }
}

public sealed class KesifP2EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string CariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567",
          "email":"ayse@ornek.com","tlBakiye":-1500.00,"hasGramBakiye":-114.958,
          "usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false}]
        """;

    // Keşif #8'in verisi + tur 3 (karar 3): altın emanet 114,958 has kayıtlı bakiyede (−114,958),
    // veresiye +13,4999 has. Kayıtlı −101,4581; ekranda altın bakiyesi EMANETSİZ 13,4999, Emanette 114,958.
    private const string Ekstre = """
        {"bakiye":{"tlBakiye":-1500,"hasGramBakiye":-101.4581,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0},
         "hareketler":[
          {"id":"h1","tur":"SATIS","tlTutari":1500,"hasGrami":-250,"aciklama":null,"olusturuldu":"2026-09-30T09:00:00Z","cariHasEtkisi":0,"bakiyeHasEtkisi":0,"emanetHas":0,"hasBakiyesineGirer":false,"emanet":false,"maden":"ALTIN"},
          {"id":"h2","tur":"EMANET_GIRIS","tlTutari":0,"hasGrami":114.958,"aciklama":"bilezik","olusturuldu":"2026-09-30T08:00:00Z","cariHasEtkisi":-114.958,"bakiyeHasEtkisi":0,"emanetHas":114.958,"hasBakiyesineGirer":false,"emanet":true,"maden":"ALTIN"},
          {"id":"h3","tur":"EMANET_GIRIS","tlTutari":0,"hasGrami":92.5,"aciklama":"tepsi","olusturuldu":"2026-09-30T07:00:00Z","cariHasEtkisi":0,"bakiyeHasEtkisi":0,"emanetHas":0,"hasBakiyesineGirer":false,"emanet":true,"maden":"GUMUS"},
          {"id":"h4","tur":"CARI_DUZELTME","tlTutari":0,"hasGrami":0,"aciklama":null,"olusturuldu":"2026-09-30T06:00:00Z","cariHasEtkisi":0,"bakiyeHasEtkisi":0,"emanetHas":0,"hasBakiyesineGirer":true,"emanet":false,"maden":"ALTIN"},
          {"id":"h5","tur":"VERESIYE","tlTutari":0,"hasGrami":-13.4999,"aciklama":null,"olusturuldu":"2026-09-30T05:00:00Z","cariHasEtkisi":13.4999,"bakiyeHasEtkisi":13.4999,"emanetHas":0,"hasBakiyesineGirer":true,"emanet":false,"maden":"ALTIN"}],
         "sayfaDisiHasEtkisi":0,"dahaFazlaVar":false,"limit":100,
         "bakiyeEmanetsizHas":13.4999,"emanetteHas":114.958}
        """;

    private static string H(decimal n) => Kuyumcu.Web.Pages.Cariler.HasBicim(n);
    /// <summary>Karar 2: görünür metinde ham kod (CARI_DUZELTME, SATIS, TRY …) yok.</summary>
    private static readonly Regex HamKod = new(@"\b[A-Z]{2,}(_[A-Z]+)*\b");

    public KesifP2EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => ekstreYaniti)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>Testin değiştirebildiği ekstre yanıtı (tur 2: uyuşmazlık senaryosu).</summary>
    private string ekstreYaniti = Ekstre;

    private sealed class SahteApi(List<(string, string, string)> kayit, Func<string> ekstre) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"usdTry":40,"eurTry":44,"gbpTry":null}""",
                var y when y.Contains("/ekstre") => ekstre(),
                var y when y.EndsWith("/cari/c1") => """
                    {"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567","email":null,
                     "vergiNo":null,"adres":null,"krediLimitiTl":null,"varsayilanVadeGun":30,"fiyatListesiId":null,
                     "pepBeyani":false,"pepGorev":null,"pepBeyanTarihi":null,"uyruk":"ZZ"}
                    """,
                var y when y.EndsWith("/cari") => CariListesi,
                var y when y.EndsWith("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/fiyat/listeler") => """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> Ciz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        istekler.Clear();
        return e;
    }

    private static KesifP2Metinleri P2 => KesifP2Sozlugu.Tr;

    private static AngleSharp.Dom.IElement Dugme(IRenderedComponent<Kuyumcu.Web.Pages.Cariler> e, string metin)
        => e.FindAll("button").First(b => b.TextContent.Contains(metin));

    [Fact]
    public void CariDetay_ham_hareket_kodu_yok()
    {
        var e = Ciz();
        e.KartEylemi("c1", "detay");   // TEMA TEMİZLİĞİ 17.10: Detay kartın eylemi
        e.WaitForElement("#cari-detay-hareketler");
        var metin = e.Find("#cari-detay-hareketler").TextContent;

        // Görünür metinde hareket kodu 0 ("SATIS" dâhil).
        Assert.DoesNotMatch(new Regex(@"\b[A-Z]{2,}(_[A-Z]+)*\b"), metin);
        Assert.Contains(KatalogSozlugu.HareketTuru("SATIS", "tr"), metin);

        // Has sütunu = bakiye etkisi: SATIS satırında has YOK; veresiye +13,4999.
        // PARA-DEMO-2 (madde 9): has etkisi olmayan satırda boş işaret ("—") BASILMAZ — hücre hiç çizilmez.
        Assert.Empty(e.FindAll("#cari-detay-has-h1"));
        Assert.Equal(H(13.4999m), e.Find("#cari-detay-has-h5").TextContent.Trim());

        // Tur 3 — ekip kararı 3: HER emanet satırı (altın dahil) "Emanet — bakiyeye girmez", has'ı toplamda YOK.
        foreach (var hid in new[] { "h2", "h3" })
        {
            Assert.Equal(P2.EmanetBakiyeyeGirmez, e.Find($"#cari-detay-emanet-{hid}").TextContent.Trim());
            Assert.Empty(e.FindAll($"#cari-detay-has-{hid}"));   // PARA-DEMO-2 (madde 9)
        }
        Assert.Equal(string.Format(P2.EmanetSatir, H(114.958m)), e.Find("#cari-detay-emanet-has-h2").TextContent.Trim());

        // Üstteki altın bakiyesi EMANETSİZ (13,4999) = satır etkileri toplamı; emanet ayrı "Emanette" kutusunda.
        var kutular = e.Find("#cari-detay-bakiyeler").TextContent;
        Assert.Contains(P2.AltinBakiye, kutular);
        Assert.Contains(H(13.4999m), kutular);
        Assert.Contains(P2.Emanette, kutular);
        Assert.Contains(H(114.958m), kutular);
        Assert.Contains(P2.EmanetteAlt, kutular);
        Assert.DoesNotContain(H(-101.4581m), kutular);   // kayıtlı (emanet dahil) üstte basılmaz
        Assert.DoesNotContain(H(-114.958m), e.Find("#cari-detay").TextContent);
        // Tek bakış açısı: TL −1.500 → "Biz borçluyuz", altın +13,4999 → "Bize borçlu"; muhasebe kelimesi yok.
        Assert.Contains(P2.YonBizBorcluyuz, kutular);
        Assert.Contains(P2.YonBizeBorclu, kutular);
        Assert.DoesNotContain(Sozlukler.Tr.Cari.Alacak, kutular);
        // "TL" para birimi adıdır (kod değil); geri kalan büyük harfli kalıp 0.
        Assert.Empty(HamKod.Matches(e.Find("#cari-detay").TextContent).Select(m => m.Value).Where(v => v != "TL"));

        // Kural BİR KEZ ve kapalı katlanırda (açılınca görünür).
        Assert.Single(e.FindAll("#cari-detay-kural"));
        Assert.Empty(e.FindAll("#cari-detay-kural-metin"));
        e.Find("#cari-detay-kural-ac").Click();
        Assert.Contains(P2.HasEtkisiKural, e.Find("#cari-detay-kural-metin").TextContent);

        // Boş döviz kutusu çizilmez (zorunlu önce).
        Assert.Empty(e.FindAll("#cari-detay-doviz"));
    }

    /// <summary>
    /// Tur 2 (hakem, #8): tüm satırlar listede (dahaFazlaVar=false, süzgeç yok) ama kayıtlı bakiye
    /// işlemlerle tutmuyor (demo kiracısı: Zeynep Koç fark 77,86). Ekran bunu "listede görünmeyen eski
    /// işlemlerin etkisi" diye AÇIKLAMAZ; ayrı uyarı bandı "uyuşmuyor" der.
    /// </summary>
    [Fact]
    public void CariDetay_uyusmazlik_eski_islem_diye_aciklanmaz()
    {
        ekstreYaniti = Ekstre.Replace("\"sayfaDisiHasEtkisi\":0,", "\"sayfaDisiHasEtkisi\":0,\"bakiyeUyusmazligiHas\":77.86,");
        Assert.Contains("bakiyeUyusmazligiHas", ekstreYaniti);
        var e = Ciz();
        e.KartEylemi("c1", "detay");
        e.WaitForElement("#cari-detay-hareketler");

        var bant = e.Find("#cari-detay-uyusmazlik");
        // TOPLU-1: tek uyarı; altın farkı kimlikli parça (#cari-detay-uyusmazlik-has).
        Assert.Equal(string.Format(Toplu1Sozlugu.Tr.UyusmazlikAltinFark, H(77.86m)), e.Find("#cari-detay-uyusmazlik-has").TextContent.Trim());
        Assert.Contains("bant-uyari", bant.ClassName);
        Assert.Empty(e.FindAll("#cari-detay-sayfa-disi"));
        Assert.DoesNotContain("eski işlem", e.Find("#cari-detay").TextContent);

        // Sayfa dışı gerçekten varsa (sınır) o metin çıkar, uyuşmazlık bandı çıkmaz.
        ekstreYaniti = Ekstre.Replace("\"sayfaDisiHasEtkisi\":0,\"dahaFazlaVar\":false", "\"sayfaDisiHasEtkisi\":-2.5,\"dahaFazlaVar\":true");
        var e2 = Ciz();
        e2.KartEylemi("c1", "detay");
        e2.WaitForElement("#cari-detay-hareketler");
        Assert.Empty(e2.FindAll("#cari-detay-uyusmazlik"));
        Assert.Contains(H(-2.5m), e2.Find("#cari-detay-sayfa-disi").TextContent);
    }

    [Fact]
    public void Cari_vade_kesirli_reddedilir()
    {
        var e = Ciz();
        e.KartAc("c1");   // TEMA TEMİZLİĞİ 17.10: Yıldız — satıra dokununca kart (düzenleme)
        e.WaitForAssertion(() => Assert.Equal("30", e.Find("#duz-vade").GetAttribute("value")));

        e.Find("#duz-vade").Input("1,5");
        e.WaitForAssertion(() => Assert.Contains(P2.VadeTamSayi, e.Find("#cekmece-cari-govde").TextContent));
        Assert.True(e.Find("#cekmece-cari-kaydet").HasAttribute("disabled"));
        e.Find("#cekmece-cari-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot is "PATCH" or "POST");

        e.Find("#duz-vade").Input("15");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-cari-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var p = Assert.Single(istekler, i => i.Metot == "PATCH");
            Assert.Equal(15, JsonDocument.Parse(p.Govde).RootElement.GetProperty("varsayilanVadeGun").GetInt32());
            // Eşlenemeyen eski uyruk ('ZZ') değişmeden gider (uydurulmaz, kart kaydedilebilir).
            Assert.Equal("ZZ", JsonDocument.Parse(p.Govde).RootElement.GetProperty("uyruk").GetString());
        });
    }

    [Fact]
    public void Cari_uyruk_MASAK_ile_ayni_ulke_secicisi_ve_duzeltin_rozeti()
    {
        var e = Ciz();
        e.KartAc("c1");   // TEMA TEMİZLİĞİ 17.10: Yıldız — satıra dokununca kart (düzenleme)
        e.WaitForElement("#duz-uyruk-duzeltin");
        Assert.Contains(P2.UyrukDuzeltin, e.Find("#duz-uyruk-duzeltin").TextContent);
        var secenekler = SeciciYardim.Secenekler(e, "duz-uyruk").Select(o => o.Deger()).ToList();
        foreach (var u in UlkeListesi.Tumu) Assert.Contains(u.Kod, secenekler);
        Assert.Contains(UlkeListesi.Diger, secenekler);

        // Tur 2 (#20, hakem): "(isteğe bağlı)" satırı olan/olmayan etiketler aynı üst çizgide —
        // ızgara ortak `.form-satiri` kuralını taşır (tarayıcıda etiket METNİ ölçüldü: rapor § 8).
        Assert.Contains("form-satiri", e.Find("#duz-p16").ClassList);

        SeciciYardim.Sec(e, "duz-uyruk", "TUR");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#duz-uyruk-duzeltin")));
        e.Find("#cekmece-cari-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var p = Assert.Single(istekler, i => i.Metot == "PATCH");
            Assert.Equal("TUR", JsonDocument.Parse(p.Govde).RootElement.GetProperty("uyruk").GetString());
        });
    }

    [Fact]
    public void CariDuzeltme_tek_sayac()
    {
        var e = Ciz();
        e.KartEylemi("c1", "duzeltme");
        e.WaitForElement("#dz-gerekce");
        Assert.Empty(e.FindAll("#dz-gerekce-sayac"));   // CokSatir'in "0 / 500" sayacı YOK
        var govde = e.Find("#dz-gerekce").ParentElement!.ParentElement!.TextContent;
        Assert.Contains(string.Format(P2.GerekceSayac, 0, 20), e.Markup);
        Assert.DoesNotContain("/ 500", e.Markup);
        Assert.DoesNotContain("0/20", e.Markup);
        Assert.NotNull(govde);

        // Tur 3 (hakem KRİTİK, karar 2): bantta ham hareket kodu YOK ("CARI_DUZELTME"), "Pozitif = borç artar" yok;
        // yön her alanın altında sade ifadeyle. Has kutusu birimi "has" (g değil), 4 ondalık.
        var uyari = e.Find("#dz-uyari").TextContent;
        Assert.Equal(P2.DuzeltmeUyari, uyari.Trim());
        Assert.DoesNotMatch(HamKod, uyari);
        Assert.DoesNotContain("CARI_DUZELTME", e.Markup);
        Assert.DoesNotContain("Pozitif", e.Markup);
        Assert.Equal(P2.DuzeltmeYon, e.Find("#dz-tl-ipucu").TextContent.Trim());
        Assert.Equal(P2.DuzeltmeYon, e.Find("#dz-has-ipucu").TextContent.Trim());
        var hasSatir = e.Find("#dz-has").ParentElement!;
        Assert.Equal(P2.BirimHas, hasSatir.QuerySelector(".alan-son-ek")!.TextContent.Trim());
        // Gerekçe ÖNCE yazılır: kilidin tek sebebi has kutusunun 5. ondalığı olsun (mutasyon M5-7 açığı).
        e.Find("#dz-gerekce").Input("Sayım farkı düzeltmesi — yeterince uzun gerekçe");
        e.Find("#dz-has").Input("1,23456");
        e.WaitForAssertion(() => Assert.True(e.Find("#dz-kaydet").HasAttribute("disabled")));
        e.Find("#dz-has").Input("-1,2345");
        e.WaitForAssertion(() => Assert.False(e.Find("#dz-kaydet").HasAttribute("disabled")));
    }

    /// <summary>
    /// Tur 3 (hakem, karar 5 BICIM-TEK cari kısmı): has binlik gruplu, 4 ondalık, tam sıfırda "0 has",
    /// eksi işareti TL ile aynı (U+2212). Liste BASİT ve PRO'da tek bakış açısı: "Bize borçlu"/"Biz borçluyuz".
    /// </summary>
    [Fact]
    public void Cari_has_bicimi_tek_ve_yon_kelimesi_liste_detay_ayni()
    {
        Assert.Equal("\u22121.127.787,6456\u00a0has", H(-1_127_787.6456m));
        Assert.Equal("0\u00a0has", H(0m));
        Assert.Equal("0\u00a0has", H(0.00004m));
        Assert.Equal("0,0001\u00a0has", H(0.00005m));
        Assert.StartsWith("\u2212", H(-2.0001m));
        Assert.StartsWith("\u2212", Bicim.Tl(-1500.5m));   // TL ile aynı eksi işareti

        // TEMA TEMİZLİĞİ 17.10: liste = Yıldız satırı (iki modda aynı yüz). Satır has'ı tuval kalıbıyla yazar
        // ("−114,96 g has" — YildizMusteriBicim.Satir); detaydaki 4 ondalıklı HasBicim ile AYNI DEĞİL → raporda ÜRÜN BULGUSU.
        // Korunan güvence: tek eksi işareti (U+2212, ASCII tire yok), tek yön kelimesi, muhasebe "Alacak/Borç" sözü yok.
        // Eski BASİT tablonun "Bakiye" sütun başlığı (th.musteri-sutun-borc) Yıldız satırında yok — kaldırıldı.
        foreach (var mod in new[] { Kuyumcu.Web.Servisler.ArayuzModuServisi.Basit, Kuyumcu.Web.Servisler.ArayuzModuServisi.Profesyonel })
        {
            var e = Ciz();
            Services.GetRequiredService<Kuyumcu.Web.Servisler.ArayuzModuServisi>().Uygula(mod, mod);
            e.Render();
            e.WaitForElement(TemaTemizligiGGYardimcisi.Satir("c1"));
            var satir = Assert.Single(e.FindAll("#yildiz-musteri-liste > .yz-musteri-sat"));
            var metin = satir.TextContent;
            Assert.Contains("\u2212114,96", metin);               // has eksi işareti TL ile aynı (U+2212)
            Assert.DoesNotContain("-114", metin);                  // ASCII tire yok
            Assert.Contains(Bicim.Tl(-1500m), metin);
            Assert.Contains(YildizBugunSozlugu.BizBorcluyuz("tr"), metin);   // TL eksi → tek yön kelimesi (BorcDiliSozlugu — tek kaynak)
            Assert.DoesNotContain(Sozlukler.Tr.Cari.Alacak, metin);
            Assert.DoesNotContain(">" + Sozlukler.Tr.Cari.Borc + "<", satir.InnerHtml);
        }
    }

    [Fact]
    public void Duzenle_bos_ad_alan_altinda_etiketle_ayni_mesaj()
    {
        var e = Ciz();
        e.KartAc("c1");   // TEMA TEMİZLİĞİ 17.10: Yıldız — satıra dokununca kart (düzenleme)
        e.WaitForAssertion(() => Assert.Equal("Ayşe Demir", e.Find("#duz-ad").GetAttribute("value")));
        e.Find("#duz-ad").Input("");
        var etiket = Sozlukler.Tr.Cari.AdFirma;
        e.WaitForAssertion(() => Assert.Contains(string.Format(P2.AdZorunlu, etiket), e.Find("#cekmece-cari-govde").TextContent));
        Assert.True(e.Find("#cekmece-cari-kaydet").HasAttribute("disabled"));
    }
}
