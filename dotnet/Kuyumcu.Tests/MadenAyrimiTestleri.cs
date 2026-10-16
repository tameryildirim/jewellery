using System.Net;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MADEN-AYRIMI (02.10) — rapor ekranlarında altın/gümüş ayrımı + bilinmeyen
// maliyet + has birimi (Kâr-zarar › Stok, Stok değeri, Stok yaşı) + BASİT jargonu.
//
// Kaynak: KAR-RAPOR-2 hasım t2 + tasarım t2 bulguları. İlke (ekip kararı 5):
//   • "has" YALNIZ ALTIN içindir; gümüş kendi biriminde (saf gram + tartı gramı), has'a KARIŞMAZ.
//   • Bilinmeyen maliyet "0 ₺" basılmaz (null → "Bilinmiyor").
//   • Has tek kural: Bicim.Has (4 hane, "has"); BASİT ve PRO AYNI rakam.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MadenAyrimiDbTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;
    private static decimal D(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetDecimal();
    private static bool Bos(System.Text.Json.JsonElement e, string ad) =>
        e.GetProperty(ad).ValueKind == System.Text.Json.JsonValueKind.Null;
    private static System.Text.Json.JsonElement Maden(System.Text.Json.JsonElement liste, string maden) =>
        liste.EnumerateArray().Single(m => m.GetProperty("maden").GetString() == maden);

    /// <summary>925 ayar gümüş yüzük: 30 g tartı, 27,75 g saf gümüş.</summary>
    private static async Task<string> GumusUrun(KuyumcuDbContext db, Kiraci k, decimal stokAdet = 0m)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"G-{id[..8]}", Ad = "Gümüş yüzük",
            AyarId = "G925", Milyem = 925m, GramAgirlik = 30m, TasAgirlik = 0m, HasGrami = 27.75m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        if (stokAdet > 0m)
            db.Stoklar.Add(new Stok
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = id,
                Adet = stokAdet, ToplamHasGram = stokAdet * 27.75m, Guncellendi = now,
            });
        await db.SaveChangesAsync();
        return id;
    }

    // ── Kâr-zarar › Stok: toplamHasGram YALNIZ altın; gümüş madenKirilimi'nde (saf 138,75 · tartı 150)
    [Fact]
    public async Task MA01_Stok_degerlemesi_has_toplami_yalniz_altin_gumus_ayri_kirilimda()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST MA01");
        using var db = vt.Olustur(k);
        var gumusId = await GumusUrun(db, k);
        var motor = new MaliyetMotoru(db);
        var an = DateTime.UtcNow.AddDays(-1);
        motor.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 2m, k.UrunHasGrami, an, birimMaliyetTl: 1000m);
        motor.PartiAc(k.TenantId, k.SubeId, gumusId, PartiKaynagi.SayimFazla, 5m, 27.75m, an, birimMaliyetTl: 100m);
        await db.SaveChangesAsync();

        var r = J(await new KarZararServisi(db).StokDegerlemesi(k.Islem, null));
        var altinHas = 2m * k.UrunHasGrami;
        Assert.Equal(altinHas, D(r, "toplamHasGram"));                 // 138,75 g gümüş KATILMAZ
        Assert.Equal(altinHas, D(r, "altinHasGram"));
        var kirilim = r.GetProperty("madenKirilimi");
        var gumus = Maden(kirilim, "GUMUS");
        Assert.Equal(138.75m, D(gumus, "toplamGram"));
        Assert.Equal(150m, D(gumus, "fizikselGram"));
        Assert.Equal(5m, D(gumus, "adet"));
        Assert.Equal(altinHas, D(Maden(kirilim, "ALTIN"), "toplamGram"));
        Assert.Equal(20m, D(Maden(kirilim, "ALTIN"), "fizikselGram"));   // 2 × 10 g
        Assert.Equal(7m, D(r, "toplamAdet"));
    }

    // ── Hiçbir adedin maliyeti bilinmiyor → stok maliyeti null (0 ₺ DEĞİL), kısmi değil
    [Fact]
    public async Task MA02_Maliyeti_hic_bilinmeyen_stok_maliyeti_null_sifir_degil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST MA02");
        using var db = vt.Olustur(k);
        var gumusId = await GumusUrun(db, k);
        var motor = new MaliyetMotoru(db);
        var an = DateTime.UtcNow.AddDays(-1);
        motor.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 3m, k.UrunHasGrami, an);
        motor.PartiAc(k.TenantId, k.SubeId, gumusId, PartiKaynagi.SayimFazla, 4m, 27.75m, an);
        await db.SaveChangesAsync();

        var r = J(await new KarZararServisi(db).StokDegerlemesi(k.Islem, null));
        Assert.True(Bos(r, "stokMaliyetiTl"));
        Assert.True(Bos(r, "stokMaliyetiHas"));
        Assert.False(r.GetProperty("stokMaliyetiKismi").GetBoolean());
        Assert.Equal(0m, D(r, "maliyetliAdet"));
        Assert.Equal(7m, D(r, "maliyetsizAdet"));
    }

    // ── Kısmen bilinen: tutar yalnız bilinen adet; kısmi = true
    [Fact]
    public async Task MA03_Kismen_bilinen_stok_maliyeti_yalniz_bilinen_ve_kismi_isaretli()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST MA03");
        using var db = vt.Olustur(k);
        var motor = new MaliyetMotoru(db);
        var an = DateTime.UtcNow.AddDays(-1);
        motor.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 2m, k.UrunHasGrami, an, birimMaliyetTl: 1500.25m);
        motor.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m, k.UrunHasGrami, an);
        await db.SaveChangesAsync();

        var r = J(await new KarZararServisi(db).StokDegerlemesi(k.Islem, null));
        Assert.Equal(3000.50m, D(r, "stokMaliyetiTl"));
        Assert.True(r.GetProperty("stokMaliyetiKismi").GetBoolean());
    }

    // ── Stok yaşı kovası: gümüşün tartı gramı ayrı alan (150 g; saf 138,75 g)
    [Fact]
    public async Task MA04_Stok_yasi_kova_gumus_fiziksel_ve_saf_gram()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST MA04");
        using var db = vt.Olustur(k);
        var gumusId = await GumusUrun(db, k);
        var motor = new MaliyetMotoru(db);
        var an = DateTime.UtcNow.AddDays(-1);
        motor.PartiAc(k.TenantId, k.SubeId, gumusId, PartiKaynagi.SayimFazla, 5m, 27.75m, an);
        motor.PartiAc(k.TenantId, k.SubeId, Kimlik.Ulid(), PartiKaynagi.SayimFazla, 1m, 5m, an);   // ürünü yok
        await db.SaveChangesAsync();

        var servis = new Kuyumcu.Api.Modules.Rapor.StokYaslandirmaServisi(db,
            new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        var kova = J(await servis.Yaslandirma(k.Islem, null)).GetProperty("kovalar")[0];
        var gumus = Maden(kova.GetProperty("madenKirilimi"), "GUMUS");
        Assert.Equal(138.75m, D(gumus, "toplamGram"));
        Assert.Equal(150m, D(gumus, "fizikselGram"));
        // Ürünsüz parti ALTIN sayılır; tartı gramı bilinmez → null (0 BASILMAZ).
        Assert.True(Bos(Maden(kova.GetProperty("madenKirilimi"), "ALTIN"), "fizikselGram"));
        Assert.Equal(5m, D(kova, "hasGram"));
    }

    // ── Rapor merkezi › Stok değeri: satırda maden + tartı gramı; toplamHas yalnız altın
    [Fact]
    public async Task MA05_Stok_degeri_satirinda_maden_ve_fiziksel_gram_toplam_yalniz_altin()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST MA05", stokAdet: 2m);
        using var db = vt.Olustur(k);
        var gumusId = await GumusUrun(db, k, stokAdet: 5m);

        var r = J(await TestKurulum.RaporServisi(db).StokDegeri(k.Islem, null, 50, 0));
        var satir = r.GetProperty("urunler").EnumerateArray().Single(u => u.GetProperty("urunId").GetString() == gumusId);
        Assert.Equal("GUMUS", satir.GetProperty("maden").GetString());
        Assert.Equal(138.75m, D(satir, "toplamHas"));
        Assert.Equal(150m, D(satir, "fizikselGram"));
        Assert.Equal(2m * k.UrunHasGrami, D(r, "toplamHas"));
    }

    // ═══ TUR 2 (tasarım hakemi KRİTİK): "Altın kârı (has)" YALNIZ altın satışlarından ═══
    private static MSatis.SatisServisi SatisSrv(KuyumcuDbContext db) =>
        new(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<MSatis.SatisServisi>.Instance);
    private static DateOnly Bugun => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));

    private static Task Alis(KuyumcuDbContext db, Kiraci k, string urunId, decimal adet, decimal birim) =>
        SatisSrv(db).Alis(new MSatis.AlisOlusturDto
        {
            UrunId = urunId, Adet = adet, BirimFiyatTl = birim, KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

    private static Task Sat(KuyumcuDbContext db, Kiraci k, string urunId, decimal adet, decimal birim) =>
        SatisSrv(db).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = urunId, Adet = adet, BirimFiyatTl = birim,
            OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
        }, k.Islem);

    [Fact]
    public async Task MA07_Altin_kari_has_yalniz_altin_satislarindan_gumus_satiri_has_tasimaz()
    {
        using var kurulum = vt.OlusturSistem();
        // Referans: YALNIZ altın satan kiracı (aynı altın işlemleri).
        var a = TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST MA07a");
        using (var dbA = vt.Olustur(a))
        {
            await Alis(dbA, a, a.UrunId, 1m, 1_000m);
            await Sat(dbA, a, a.UrunId, 1m, 1_500m);
        }
        // Altın + gümüş satan kiracı: altın işlemleri birebir aynı, üstüne maliyetli gümüş satışı.
        var k = TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST MA07b");
        using var db = vt.Olustur(k);
        var gumusId = await GumusUrun(db, k);
        await Alis(db, k, k.UrunId, 1m, 1_000m);
        await Sat(db, k, k.UrunId, 1m, 1_500m);
        await Alis(db, k, gumusId, 1m, 100m);
        await Sat(db, k, gumusId, 1m, 900m);

        System.Text.Json.JsonElement refR;
        using (var dbA = vt.Olustur(a))
            refR = J(await new KarZararServisi(dbA).Donem(a.Islem, Bugun, Bugun, null));
        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));

        // TL bacağı gümüşü TAŞIR (para kârı): 500 + 800.
        Assert.Equal(1_300m, D(r, "karTl"));
        // Has bacağı gümüşü TAŞIMAZ: altın-yalnız kiracıyla birebir aynı.
        Assert.Equal(D(refR, "hasilatHas"), D(r, "hasilatHas"));
        Assert.Equal(D(refR, "maliyetHas"), D(r, "maliyetHas"));
        Assert.Equal(D(refR, "karHas"), D(r, "karHas"));
        Assert.Equal(D(refR, "bilinenHasilatHas"), D(r, "bilinenHasilatHas"));
        Assert.True(D(r, "karHas") > 0m);
        Assert.Equal("ALTIN", r.GetProperty("hasBacagiMadeni").GetString());

        // Ürün satırı: gümüşte has alanları null (ekran "—"), altında dolu.
        var u = J(await new KarZararServisi(db).UrunBazli(k.Islem, Bugun, Bugun, null, 100));
        var satirlar = u.GetProperty("satirlar").EnumerateArray().ToList();
        var g = satirlar.Single(x => x.GetProperty("urunId").GetString() == gumusId);
        var al = satirlar.Single(x => x.GetProperty("urunId").GetString() == k.UrunId);
        Assert.Equal("GUMUS", g.GetProperty("maden").GetString());
        Assert.True(Bos(g, "karHas"));
        Assert.True(Bos(g, "hasilatHas"));
        Assert.True(Bos(g, "maliyetHas"));
        Assert.Equal(800m, D(g, "karTl"));
        Assert.Equal("ALTIN", al.GetProperty("maden").GetString());
        Assert.Equal(D(r, "karHas"), D(al, "karHas"));          // altın satırı = dönem altın kârı
    }

    [Fact]
    public async Task MA08_Altin_maliyeti_bilinmiyorsa_has_kari_null_gumusun_bilinen_maliyeti_onu_hesaplatmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST MA08");
        using var db = vt.Olustur(k);
        var gumusId = await GumusUrun(db, k);
        // Altın: maliyetsiz açılış partisi → altın satışının maliyeti bilinmez.
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m,
            k.UrunHasGrami, DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();
        await Sat(db, k, k.UrunId, 1m, 1_500m);
        await Alis(db, k, gumusId, 1m, 100m);
        await Sat(db, k, gumusId, 1m, 900m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("KISMI", r.GetProperty("karDurumu").GetString());
        Assert.Equal(800m, D(r, "karTl"));                  // TL kârı bilinen gümüşten
        Assert.True(Bos(r, "karHas"));                       // altın kârı BİLİNMİYOR — 0 değil
        Assert.True(Bos(r, "maliyetHas"));
    }

    [Fact]
    public async Task MA09_Stok_has_maliyeti_kursuz_partiyi_sifir_saymaz_ayri_sayilir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST MA09");
        using var db = vt.Olustur(k);
        var motor = new MaliyetMotoru(db);
        var an = DateTime.UtcNow.AddDays(-1);
        // TL maliyeti bilinen ama alış günü has fiyatı verilmemiş parti → BirimMaliyetHas null.
        motor.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 2m, k.UrunHasGrami, an, birimMaliyetTl: 1000m);
        await db.SaveChangesAsync();
        var r = J(await new KarZararServisi(db).StokDegerlemesi(k.Islem, null));
        Assert.Equal(2000m, D(r, "stokMaliyetiTl"));
        Assert.True(Bos(r, "stokMaliyetiHas"));              // eskiden 0 dönüyordu
        Assert.Equal(0m, D(r, "hasMaliyetliAdet"));
        Assert.Equal(2m, D(r, "hasMaliyetsizAdet"));

        // Has fiyatıyla açılan ikinci parti → has maliyeti kısmi.
        motor.PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m, k.UrunHasGrami, an,
            birimMaliyetTl: 1000m, altinHasFiyati: 4000m);
        await db.SaveChangesAsync();
        var r2 = J(await new KarZararServisi(db).StokDegerlemesi(k.Islem, null));
        Assert.False(Bos(r2, "stokMaliyetiHas"));
        Assert.True(r2.GetProperty("stokMaliyetiHasKismi").GetBoolean());
        Assert.False(r2.GetProperty("stokMaliyetiKismi").GetBoolean());   // TL'de hepsi biliniyor
    }

    [Theory]
    [InlineData("0", "0", false)]     // stok yok → bilinen 0 ₺ (gerçekten sıfır)
    [InlineData("0", "5", true)]      // hiçbiri bilinmiyor → null
    [InlineData("2", "3", false)]     // kısmen → bilinen kısım
    [InlineData("4", "0", false)]
    public void MA06_StokMaliyetiBilinmiyor_kurali(string maliyetli, string maliyetsiz, bool beklenen) =>
        Assert.Equal(beklenen, KarZararServisi.StokMaliyetiBilinmiyor(decimal.Parse(maliyetli), decimal.Parse(maliyetsiz)));
}

// ═════════════════════════════════════════════════════════════════════════════
// Saf: tek biçim kuralı (Bicim.MadenMiktari)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class MadenAyrimiBicimTestleri
{
    private static string M(string? maden, decimal saf, decimal? fiz, string dil = "tr")
    {
        var kr = KarRaporSozlugu.Al(dil);
        return Bicim.MadenMiktari(maden, saf, fiz, kr.MadenGramFizikselSaf, kr.MadenGramSaf);
    }

    [Fact]
    public void MA10_Altin_has_gumus_kendi_biriminde()
    {
        var eski = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        try
        {
            Assert.Equal("3,2060 has", M("ALTIN", 3.206m, null));
            Assert.Equal("3,2060 has", M(null, 3.206m, 4m));               // maden bilinmiyor → altın varsayımı
            Assert.Equal("150,00 g (saf 138,75 g)", M("GUMUS", 138.75m, 150m));
            Assert.Equal("saf 138,75 g", M("GUMUS", 138.75m, null));
            Assert.Equal("saf 5,00 g", M("GUMUS", 5m, 5.004m));            // tartı = saf (2 hane) → tek rakam
            Assert.Equal("150,00 g (138,75 g pure)", M("GUMUS", 138.75m, 150m, "en"));
            Assert.DoesNotContain("has", M("PLATIN", 9.5m, 10m));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = eski; }
    }

    // TUR 2 (tasarım hakemi KRİTİK): has binlik gruplu, 4 ondalık, tarayıcı kültüründen bağımsız (tr-TR).
    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void MA11_Has_binlik_gruplu_dort_ondalik(string kultur)
    {
        var eski = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(kultur);
        try
        {
            Assert.Equal("1.447,3856 has", Bicim.Has(1447.3856m));
            Assert.Equal("1.400,0000 has", Bicim.Has(1400m));
            Assert.Equal("999,9999 has", Bicim.Has(999.9999m));
            Assert.Equal("-1.234,5000 has", Bicim.Has(-1234.5m));
            Assert.Equal("1.447,3856 has", M("ALTIN", 1447.3856m, null));
            Assert.Equal("1.500,00 g (saf 1.387,50 g)", M("GUMUS", 1387.5m, 1500m));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = eski; }
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// bUnit: Kâr-zarar ekranı
// ═════════════════════════════════════════════════════════════════════════════
public sealed class MadenAyrimiKarZararEkranTestleri : BunitContext
{
    internal const string OzetTam = """
    {"baslangic":"2026-10-01","bitis":"2026-10-02","gunSayisi":2,"subeId":null,
     "satisAdedi":2,"iadeAdedi":0,
     "brutHasilatTl":10000,"iadeTl":0,"hasilatTl":10000,
     "brutMaliyetTl":8000,"iadeMaliyetTl":0,"maliyetTl":8000,
     "karTl":2000,"karDurumu":"TAM","bilinenHasilatTl":10000,
     "maliyetiEksikSatisAdedi":0,"maliyetiEksikIadeAdedi":0,"maliyetiEksikTutarTl":0,
     "karMarjiYuzde":20,
     "iscilikTl":0,"takasFarkTl":0,"takasAdedi":0,"takasNotu":null,
     "giderTl":0,"giderKirilimi":[],"donemKariTl":2000,
     "hasilatHas":117.595,"iadeHas":0,"maliyetHas":98.443,"karHas":19.152,"karHasIkiHane":19.16,
     "bilinenHasilatHas":117.595,"maliyetiEksikHasilatHas":0,
     "hasilatHasDefterGram":159.37,"altinHasilatHasDefterGram":159.37,
     "hasDefterMadenDagilimi":[{"maden":"GUMUS","madenAdi":"Gümüş","toplamHas":27.75,"satirSayisi":1},
                               {"maden":"ALTIN","madenAdi":"Altın","toplamHas":159.37,"satirSayisi":1}],
     "madenUyarisi":"x",
     "karKoprusu":[{"kod":"BRUT_HASILAT","tutarTl":10000,"araToplam":false},
                   {"kod":"NET_HASILAT","tutarTl":10000,"araToplam":true},
                   {"kod":"MALIYET","tutarTl":-8000,"araToplam":false},
                   {"kod":"BRUT_KAR","tutarTl":2000,"araToplam":true},
                   {"kod":"ISCILIK","tutarTl":0,"araToplam":false},
                   {"kod":"DONEM_KARI","tutarTl":2000,"araToplam":true}],
     "storno":{"adet":0,"tutarTl":0,"notu":""},
     "karsilastir":null,"onceki":null,"degisimYuzde":null,
     "maliyetiBilinenAdet":2,"maliyetiBilinmeyenAdet":0,"kuruBulunamayanSatis":0,
     "guvenilirlik":"TAM","uyari":null}
    """;

    private const string UrunTam = """
    {"satirlar":[{"urunId":"u1","kod":"K-1","ad":"Bilezik","satisAdedi":2,"iadeAdedi":0,
                  "brutHasilatTl":10000,"iadeTl":0,"hasilatTl":10000,"maliyetTl":8000,"bilinenHasilatTl":10000,
                  "karTl":2000,"karDurumu":"TAM","maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,
                  "hasilatHas":117.595,"maliyetHas":98.443,"karHas":19.152,"karMarjiYuzde":20,
                  "maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM"}],
     "urunSayisi":1,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"TAM"}
    """;

    /// <summary>H kiracısının görüntüsü: stok altın 3,206 has + gümüş 138,75 saf (150 g); maliyet hiç bilinmiyor.</summary>
    internal const string StokBilinmiyor = """
    {"subeId":null,"toplamAdet":7,"toplamHasGram":3.206,"altinHasGram":3.206,
     "madenKirilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":2,"toplamGram":3.206,"fizikselGram":3.5},
                      {"maden":"GUMUS","madenAdi":"Gümüş","adet":5,"toplamGram":138.75,"fizikselGram":150}],
     "maliyetliAdet":0,"maliyetsizAdet":7,"stokMaliyetiTl":null,"stokMaliyetiHas":null,"stokMaliyetiKismi":false,
     "guvenilirlik":"DUSUK","kaynakDagilimi":[],"konsinyeUrunSayisi":0,"uyari":"Bir kısım stoğun alış maliyeti bilinmiyor; stok maliyeti bu adetleri İÇERMEZ."}
    """;

    private string ozet = OzetTam;
    private string urun = UrunTam;
    private string stok = StokBilinmiyor;

    public MadenAyrimiKarZararEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => ozet, () => stok, () => urun)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<RaporServisi>();
    }

    private sealed class SahteApi(Func<string> ozet, Func<string> stok, Func<string> urun) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.Contains("kar-zarar/urun") => urun(),
                var y when y.Contains("kar-zarar") => ozet(),
                var y when y.Contains("stok-degerlemesi") => stok(),
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> Ciz(bool basit)
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-panel-stok .stat-izgara")));
        return c;
    }

    private static Dictionary<string, (string Deger, string Alt)> Kartlar(IRenderedComponent<Kuyumcu.Web.Pages.KarZarar> c, string kap) =>
        c.FindAll($"{kap} .stat-kart").ToDictionary(
            k => k.QuerySelector(".stat-etiket")!.TextContent.Trim(),
            k => (k.QuerySelector(".stat-deger")!.TextContent.Trim(), k.QuerySelector(".stat-alt")?.TextContent.Trim() ?? ""));

    // [YÜKSEK] has kartı yalnız altın; gümüş alt satırda kendi biriminde
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MA20_Stok_karti_altin_has_gumus_alt_satirda_kendi_biriminde(bool basit)
    {
        var c = Ciz(basit);
        var k = Kartlar(c, "#kz-panel-stok");
        var kr = KarRaporSozlugu.Tr;
        Assert.Equal("3,2060 has", k[kr.StokAltinHas].Deger);
        Assert.Equal("Gümüş: 150,00 g (saf 138,75 g)", k[kr.StokAltinHas].Alt);
        var panel = c.Find("#kz-panel-stok").TextContent;
        Assert.DoesNotContain("141,9", panel, StringComparison.Ordinal);       // altın + gümüş toplamı YOK
        Assert.DoesNotContain("138,75 has", panel, StringComparison.Ordinal);
        Assert.DoesNotContain(" g has", panel, StringComparison.Ordinal);
    }

    // [YÜKSEK] maliyeti bilinmeyen stok "0 ₺" basılmaz; kısmen biliniyorsa etiket "Bilinen stok maliyeti"
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MA21_Stok_maliyeti_bilinmiyorsa_0_basilmaz_kismiyse_bilinen_etiketi(bool basit)
    {
        var kr = KarRaporSozlugu.Tr;
        var c = Ciz(basit);
        var k = Kartlar(c, "#kz-panel-stok");
        var etiket = Sozlukler.Getir("tr").KarZarar.StokMaliyeti;
        Assert.Equal(kr.MaliyetBilinmiyor, k[etiket].Deger);
        Assert.Equal(kr.StokMaliyetiBilinmiyorAlt, k[etiket].Alt);
        Assert.DoesNotContain(Bicim.Tl(0m), c.Find("#kz-panel-stok").TextContent, StringComparison.Ordinal);

        stok = StokBilinmiyor.Replace("\"maliyetliAdet\":0,\"maliyetsizAdet\":7,\"stokMaliyetiTl\":null,\"stokMaliyetiHas\":null,\"stokMaliyetiKismi\":false",
                                      "\"maliyetliAdet\":2,\"maliyetsizAdet\":5,\"stokMaliyetiTl\":3000.5,\"stokMaliyetiHas\":0.44,\"stokMaliyetiKismi\":true", StringComparison.Ordinal);
        Assert.Contains("\"stokMaliyetiKismi\":true", stok, StringComparison.Ordinal);
        var c2 = Ciz(basit);
        var k2 = Kartlar(c2, "#kz-panel-stok");
        Assert.Equal(Bicim.Tl(3000.5m), k2[kr.StokMaliyetiBilinen].Deger);
        Assert.False(k2.ContainsKey(etiket));
        Assert.Equal(string.Format(kr.StokMaliyetiKismiNot, "5"), c2.Find("#kz-stok-kismi").TextContent.Trim());
        Assert.Empty(c.FindAll("#kz-stok-kismi"));
        Assert.DoesNotContain("İÇERMEZ", c.Find("#kz-panel-stok").TextContent, StringComparison.Ordinal);
    }

    // [YÜKSEK] BASİT'te stok güvenilirlik rozeti açılışta görünmez (kapalı katlanır); PRO açık
    [Fact]
    public void MA22_BASIT_stok_guvenilirlik_kapali_katlanirda_PRO_acik()
    {
        var c = Ciz(true);
        Assert.Empty(c.FindAll("#kz-stok-guven"));
        c.Find("#kz-stok-nereden-ac").Click();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kz-stok-nereden-govde #kz-stok-guven")));

        var p = Ciz(false);
        Assert.NotNull(p.Find("#kz-stok-guven"));
        Assert.Empty(p.FindAll("#kz-stok-nereden"));
    }

    // [YÜKSEK] PRO'da "Bacağı" jargonu yok; teknik paragraf ⓘ arkasında
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MA23_Bacak_jargonu_iki_modda_yok_teknik_not_ipucunda(bool basit)
    {
        var c = Ciz(basit);
        Assert.DoesNotContain("Bacağı", c.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("BACAĞI", c.Markup, StringComparison.Ordinal);
        var basliklar = c.FindAll("#kz-panel-ozet h2.bolum-baslik").Select(h => h.TextContent.Trim()).ToArray();
        Assert.Equal([OrtakGorunumSozlugu.Tr.KarZararParaKariBasit, KarRaporSozlugu.Tr.AltinKariBaslik], basliklar);   // RAPOR-2
        // Teknik paragraf hiçbir modda açık <p> olarak kartların altına yapışmaz.
        foreach (var t in c.FindAll("#kz-has-tanim"))
            Assert.Equal("DETAILS", t.ParentElement!.TagName);
    }

    // [ORTA] Takas: BASİT'te 0 iken gizli; adet birimli; iç içe kart yok; paragraf ⓘ; işçilik kartı köprüde varsa tekrar etmez
    [Fact]
    public void MA24_Takas_ve_iscilik_kartlari()
    {
        var b = Ciz(true);
        Assert.Empty(b.FindAll("#kz-takas"));
        Assert.Empty(b.FindAll("#kz-iscilik"));                 // köprüde ISCILIK satırı var

        var p = Ciz(false);
        var takas = p.Find("#kz-takas");
        Assert.Empty(takas.QuerySelectorAll(".kart .stat-kart"));    // dış kart yok → kart içinde kart yok
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.TakasAdet, 0), takas.QuerySelector(".stat-alt")!.TextContent.Trim());
        Assert.Equal("DETAILS", p.Find("#kz-takas-notu").ParentElement!.TagName);
        Assert.Empty(p.FindAll("#kz-iscilik"));

        ozet = OzetTam.Replace("\"takasFarkTl\":0,\"takasAdedi\":0", "\"takasFarkTl\":-250,\"takasAdedi\":2", StringComparison.Ordinal);
        var b2 = Ciz(true);
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.TakasAdet, 2), b2.Find("#kz-takas .stat-alt").TextContent.Trim());
    }

    // [ORTA] has tek kural: BASİT ve PRO AYNI rakam (19,1520 has), BASİT 19,16 g basmaz; ürün satırı aynı
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MA25_Has_kari_iki_modda_ayni_rakam_ozet_ve_urun_satiri_ayni(bool basit)
    {
        var c = Ciz(basit);
        var hasKartlar = c.FindAll("#kz-panel-ozet .stat-izgara")[1].QuerySelectorAll(".stat-kart")
            .Select(x => x.QuerySelector(".stat-deger")!.TextContent.Trim()).ToList();
        Assert.Equal(["117,5950 has", "98,4430 has", "19,1520 has"], hasKartlar.Take(3));
        Assert.DoesNotContain("19,16", c.Markup, StringComparison.Ordinal);
        var urunSatiri = c.FindAll("#kz-panel-urunler tr").First(t => t.TextContent.Contains("Bilezik", StringComparison.Ordinal));
        Assert.Contains("19,1520 has", urunSatiri.TextContent, StringComparison.Ordinal);
    }

    // [ORTA] BASİT: defter gramı (159,37) üç kartlı köprünün yanında kart değil; ayrı satır + ⓘ; gümüş has yazılmaz
    [Fact]
    public void MA26_BASIT_defter_grami_kopruden_ayri_katlanirda_PRO_kartta_madenine_gore()
    {
        var b = Ciz(true);
        var izgara = b.FindAll("#kz-panel-ozet .stat-izgara")[1];
        Assert.Equal(3, izgara.QuerySelectorAll(".stat-kart").Length);
        // Açılışta görünmez (kapalı "Bu rakamlar nereden geliyor?"); açılınca altın önce, açıklamasıyla.
        Assert.Empty(b.FindAll("#kz-defter-ayri"));
        b.Find("#kz-nereden-ac").Click();
        b.WaitForAssertion(() => Assert.NotNull(b.Find("#kz-nereden-govde #kz-defter-ayri")));
        var satirlar = b.FindAll("#kz-defter-ayri .kz-defter-maden").Select(x => x.TextContent.Trim()).ToList();
        Assert.Equal(["Altın: 159,3700 has", "Gümüş: saf 27,75 g"], satirlar);
        Assert.Equal(KarRaporSozlugu.Tr.DefterAyriIpucu, b.Find("#kz-defter-aciklama").TextContent.Trim());
        Assert.DoesNotContain("27,7500 has", b.Markup, StringComparison.Ordinal);

        var p = Ciz(false);
        var k = p.FindAll("#kz-panel-ozet .stat-izgara")[1].QuerySelectorAll(".stat-kart").ToDictionary(
            x => x.QuerySelector(".stat-etiket")!.TextContent.Trim(),
            x => (Deger: x.QuerySelector(".stat-deger")!.TextContent.Trim(), Alt: ""));
        // TUR 2 (tasarım hakemi KRİTİK): kart etiketi 'has' kelimesini yalnız altına verir.
        var kr = KarRaporSozlugu.Tr;
        Assert.Equal("159,3700 has", k[kr.DefterAltinEtiketi].Deger);
        Assert.Equal("saf 27,75 g", k[string.Format(kr.DefterMadenEtiketi, "Gümüş")].Deger);
        Assert.DoesNotContain(k.Keys, e => e.Contains("Gümüş", StringComparison.Ordinal) && e.Contains("has", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(Sozlukler.Getir("tr").KarZarar.HasDefterGram, p.Markup, StringComparison.Ordinal);
        Assert.Equal(kr.HasTanimMetni, p.Find("#kz-has-tanim").TextContent.Trim());   // ⓘ metni 'has'ı altına bağlar
        Assert.Empty(p.FindAll("#kz-defter-ayri"));
    }

    // TUR 2 (tasarım hakemi KRİTİK): gümüş ürün satırı has kârı taşımaz ("—"); altın satırı has'ta
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MA28_Gumus_urun_satiri_has_kari_tire_altin_satiri_has(bool basit)
    {
        urun = """
        {"satirlar":[{"urunId":"u1","kod":"K-1","ad":"Bilezik","satisAdedi":1,"iadeAdedi":0,"maden":"ALTIN",
                      "brutHasilatTl":10000,"iadeTl":0,"hasilatTl":10000,"maliyetTl":8000,"bilinenHasilatTl":10000,
                      "karTl":2000,"karDurumu":"TAM","maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,
                      "hasilatHas":117.595,"maliyetHas":98.443,"karHas":19.152,"karMarjiYuzde":20,
                      "maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM"},
                     {"urunId":"u2","kod":"G-1","ad":"925 Gümüş Kolye","satisAdedi":1,"iadeAdedi":0,"maden":"GUMUS",
                      "brutHasilatTl":1000,"iadeTl":0,"hasilatTl":1000,"maliyetTl":100,"bilinenHasilatTl":1000,
                      "karTl":900,"karDurumu":"TAM","maliyetiEksikSatisAdedi":0,"maliyetiEksikTutarTl":0,
                      "hasilatHas":null,"maliyetHas":null,"karHas":null,"karMarjiYuzde":90,
                      "maliyetiBilinmeyenAdet":0,"guvenilirlik":"TAM"}],
         "urunSayisi":2,"limit":50,"dahaFazlasiVar":false,"guvenilirlik":"TAM"}
        """;
        var c = Ciz(basit);
        var satirlar = c.FindAll("#kz-panel-urunler tr");
        var gumus = satirlar.First(t => t.TextContent.Contains("Gümüş Kolye", StringComparison.Ordinal));
        var altin = satirlar.First(t => t.TextContent.Contains("Bilezik", StringComparison.Ordinal));
        Assert.DoesNotContain(" has", gumus.TextContent, StringComparison.Ordinal);
        Assert.Contains("—", gumus.TextContent, StringComparison.Ordinal);
        Assert.Contains("900", gumus.TextContent, StringComparison.Ordinal);   // TL kârı görünür
        Assert.Contains("19,1520 has", altin.TextContent, StringComparison.Ordinal);
    }

    // TUR 2 (tasarım hakemi KRİTİK): BASİT'te teknik maden bandı YOK; PRO'da tek cümle ⓘ arkasında, 'gümüşün has gramı' yok
    [Fact]
    public void MA29_Maden_bandi_BASITte_yok_PROda_ipucunda()
    {
        var kr = KarRaporSozlugu.Tr;
        var eskiBant = Sozlukler.Getir("tr").KarZarar.MadenToplanmaz;
        var b = Ciz(true);
        Assert.Empty(b.FindAll("#kz-maden-uyari"));
        Assert.DoesNotContain(eskiBant, b.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(kr.MadenAyriMetin, b.Markup, StringComparison.Ordinal);

        var p = Ciz(false);
        var bant = p.Find("#kz-maden-uyari");
        Assert.DoesNotContain("uyari-kutu", bant.ClassList);                 // sarı kutu değil (MADEN-2: xUnit2029)
        var ipucu = p.Find("#kz-maden-uyari-ipucu");
        Assert.Equal("DETAILS", ipucu.TagName);
        Assert.False(ipucu.HasAttribute("open"));                         // kapalı
        Assert.Equal(kr.MadenAyriMetin, p.Find("#kz-maden-uyari-metin").TextContent.Trim());
        Assert.DoesNotContain(eskiBant, p.Markup, StringComparison.Ordinal);
    }

    // TUR 2 (tasarım hakemi KRİTİK): 1.000 has üstü değer binlik gruplu basılır (iki modda aynı)
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MA32_Has_bin_ustu_binlik_gruplu(bool basit)
    {
        ozet = OzetTam.Replace("\"hasilatHas\":117.595", "\"hasilatHas\":1447.3856", StringComparison.Ordinal)
                      .Replace("\"bilinenHasilatHas\":117.595", "\"bilinenHasilatHas\":1447.3856", StringComparison.Ordinal);
        stok = StokBilinmiyor.Replace("\"toplamHasGram\":3.206", "\"toplamHasGram\":1400", StringComparison.Ordinal);
        var c = Ciz(basit);
        var hasKartlar = c.FindAll("#kz-panel-ozet .stat-izgara")[1].QuerySelectorAll(".stat-kart")
            .Select(x => x.QuerySelector(".stat-deger")!.TextContent.Trim()).ToList();
        Assert.Equal("1.447,3856 has", hasKartlar[0]);
        Assert.Equal("1.400,0000 has", Kartlar(c, "#kz-panel-stok")[KarRaporSozlugu.Tr.StokAltinHas].Deger);
        Assert.DoesNotContain("1447,3856", c.Markup, StringComparison.Ordinal);
    }

    // [DÜŞÜK] maliyeti eksik yalnız iade → kutu görünür; eksi tutar "−Y" olarak basılmaz
    [Fact]
    public void MA27_Maliyeti_eksik_yalniz_iade_kutuyu_acar_eksi_tutar_basilmaz()
    {
        ozet = OzetTam.Replace("\"maliyetiEksikSatisAdedi\":0,\"maliyetiEksikIadeAdedi\":0,\"maliyetiEksikTutarTl\":0",
                               "\"maliyetiEksikSatisAdedi\":0,\"maliyetiEksikIadeAdedi\":1,\"maliyetiEksikTutarTl\":-500", StringComparison.Ordinal);
        var c = Ciz(true);
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.MaliyetiEksikIadeSatir, 1), c.Find("#kz-maliyet-eksik-iade").TextContent.Trim());
        Assert.Empty(c.FindAll("#kz-maliyet-eksik-satir"));

        ozet = OzetTam.Replace("\"maliyetiEksikSatisAdedi\":0,\"maliyetiEksikIadeAdedi\":0,\"maliyetiEksikTutarTl\":0",
                               "\"maliyetiEksikSatisAdedi\":1,\"maliyetiEksikIadeAdedi\":2,\"maliyetiEksikTutarTl\":-500", StringComparison.Ordinal);
        var c2 = Ciz(false);
        var satir = c2.Find("#kz-maliyet-eksik-satir").TextContent.Trim();
        Assert.Equal(string.Format(KarRaporSozlugu.Tr.MaliyetiEksikSatisSayi, 1), satir);
        Assert.DoesNotContain("500", c2.Find("#kz-maliyet-eksik").TextContent, StringComparison.Ordinal);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// bUnit: Rapor merkezi (Stok değeri + Stok yaşı)
// ═════════════════════════════════════════════════════════════════════════════
public sealed class MadenAyrimiRaporlarEkranTestleri : BunitContext
{
    private const string StokDegeri = """
    {"urunler":[{"urunId":"g1","kod":"G-1","ad":"925 Ayar Gümüş Yüzük Seti","ayarId":"G925","maden":"GUMUS",
                 "adet":3,"blokeAdet":0,"satilabilirAdet":3,"toplamHas":66.6,"fizikselGram":72},
                {"urunId":"a1","kod":"A-1","ad":"22 Ayar Bilezik","ayarId":"K22","maden":"ALTIN",
                 "adet":1,"blokeAdet":0,"satilabilirAdet":1,"toplamHas":3.206,"fizikselGram":3.5}],
     "toplamHas":3.206,
     "madenDagilimi":[{"maden":"GUMUS","madenAdi":"Gümüş","adet":3,"toplamGram":66.6},{"maden":"ALTIN","madenAdi":"Altın","adet":1,"toplamGram":3.206}],
     "toplamBlokeAdet":0,"toplamSatir":2,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":"x"}
    """;

    private const string Yaslandirma = """
    {"olculduAn":"2026-10-02T06:00:00Z","subeId":null,
     "kovalar":[{"kod":"G0_30","altSinirGun":0,"ustSinirGun":30,"adet":7,"hasGram":3.206,"altinHasGram":3.206,"maliyetTl":0,"maliyetiBilinmeyenAdet":7,
                 "madenKirilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":2,"toplamGram":3.206,"fizikselGram":3.5},
                                  {"maden":"GUMUS","madenAdi":"Gümüş","adet":5,"toplamGram":138.75,"fizikselGram":150}]}],
     "toplamAdet":7,"altinHasGram":3.206,
     "madenDagilimi":[{"maden":"ALTIN","madenAdi":"Altın","adet":2,"toplamGram":3.206},{"maden":"GUMUS","madenAdi":"Gümüş","adet":5,"toplamGram":138.75}],
     "yasiBilinmeyenAdet":0,
     "devir":{"pencereGun":90,"tuketilenAdet":1,"kalanAdet":7,"devirHizi":0.14,"yillikTahmini":0.57,"ortalamaRafOmruGun":630,"yontem":"PARTI_TUKETIMI","tanim":"t"},
     "oluStok":{"esikGun":180,"toplamSatir":1,"toplamAdet":5,"ofset":0,"limit":100,"dahaFazlasiVar":false,
                "urunler":[{"urunId":"g1","kod":"G-1","ad":"Gümüş yüzük","ayarId":"G925","maden":"GUMUS","adet":5,"hasGram":138.75,"enEskiGiris":"2026-01-01T00:00:00Z","yasGun":274}]},
     "guvenilirlik":"DUSUK","uyarilar":[]}
    """;

    private const string Mutabakat = """
        {"tarih":"2026-10-02",
         "tahsilat":{"nakit":0,"kart":0,"havale":0,"diger":0,"toplam":0},
         "satis":{"sayisi":0,"toplamTl":0,"faturaliSayisi":0,"faturaliTl":0,"faturasizSayisi":0,"faturasizTl":0},
         "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
         "hurda":{"sayisi":0,"odenenTl":0,"hasGrami":0,"pusulaSayisi":0,"pusulaBrutTl":0,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
         "fatura":{"sayisi":0,"toplamTl":0,"kdvToplam":0,"satisFaturasiTl":0},
         "fark":0,"mutabik":true,"uyarilar":[],"faturasizIslemler":[],"faturaEslesme":null}
        """;

    public MadenAyrimiRaporlarEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => KarRaporRaporlarEkranTestleri.AylikHesaplanamadi,
                var y when y.EndsWith("/raporlar/cari-yaslandirma") =>
                    """{"yaslananCariler":[],"ozetTl":{},"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false}""",
                var y when y.EndsWith("/raporlar/stok-degeri") => StokDegeri,
                var y when y.EndsWith("/raporlar/reyon-stok") => """{"reyonlar":[],"subeler":[]}""",
                var y when y.EndsWith("/raporlar/stok-yaslandirma") => Yaslandirma,
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => Mutabakat,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/kullanicilar") => "[]",
                var y when y.EndsWith("/kasalar") => "[]",
                var y when y.EndsWith("/reyonlar") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    // [ORTA] Stok değeri: gümüş satırı "66,6000 has" yazmaz; tartı + saf gram
    [Fact]
    public void MA30_Stok_degeri_gumus_satiri_has_yazmaz_altin_has()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.Contains("925 Ayar Gümüş Yüzük Seti", e.Markup, StringComparison.Ordinal));
        var satirlar = e.FindAll("tr").Where(t => t.TextContent.Contains("Yüzük Seti", StringComparison.Ordinal)
                                              || t.TextContent.Contains("22 Ayar Bilezik", StringComparison.Ordinal)).ToList();
        var gumus = satirlar.Single(t => t.TextContent.Contains("Yüzük Seti", StringComparison.Ordinal));
        var altin = satirlar.Single(t => t.TextContent.Contains("22 Ayar Bilezik", StringComparison.Ordinal));
        Assert.Contains("72,00 g (saf 66,60 g)", gumus.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("66,6000 has", e.Markup, StringComparison.Ordinal);
        Assert.Contains("3,2060 has", altin.TextContent, StringComparison.Ordinal);
    }

    // [DÜŞÜK] Stok yaşı: kova gümüşü tartı + saf; devir formülü kartta açık değil, ⓘ arkasında; ölü stok gümüşü has yazmaz
    [Fact]
    public void MA31_Stok_yasi_gumus_birimi_ve_devir_tanimi_ipucunda()
    {
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#sy-devir-ipucu")));
        var kart = e.Find("#stok-yaslandirma-kart").QuerySelectorAll(".stat-kart")
            .First(k => k.QuerySelector(".stat-etiket")!.TextContent.Contains("30", StringComparison.Ordinal));
        Assert.Equal("3,2060 has", kart.QuerySelector(".stat-deger")!.TextContent.Trim());
        Assert.Contains("Gümüş: 150,00 g (saf 138,75 g)", kart.QuerySelector(".stat-alt")!.TextContent, StringComparison.Ordinal);
        var tanim = Sozlukler.Getir("tr").RaporSeti.DevirTanimi;
        Assert.Equal("DETAILS", e.Find("#sy-devir-tanim").ParentElement!.TagName);
        Assert.DoesNotContain(e.FindAll("#stok-yaslandirma-kart .stat-alt"), a => a.TextContent.Contains(tanim, StringComparison.Ordinal));
        Assert.DoesNotContain("138,7500 has", e.Markup, StringComparison.Ordinal);
        Assert.Contains("saf 138,75 g", e.Find("#stok-yaslandirma-kart table").TextContent, StringComparison.Ordinal);
    }
}
