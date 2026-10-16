using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET MADEN-AYRIMI — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT LİTERALDİR (üretim
// kodundan türetilmez). Paketin iddiaları: (1) stok değerlemesi toplamHasGram
// YALNIZ altın hası, diğer madenler madenKirilimi'nde; (2) hiçbir adedin maliyeti
// bilinmiyorsa stokMaliyetiTl/Has null ("0 ₺" değil); (3) rapor ekranlarında
// maden miktarı tek kural: altın "has" 4 hane, gümüş kendi biriminde (g, 2 hane).
// Proje yuvarlama kuralı: MidpointRounding.AwayFromZero.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class MADENAYRIMISinirTestleri(TestVeritabani vt)
{
    private static System.Text.Json.JsonElement J(object o) =>
        System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o)).RootElement;
    private static decimal D(System.Text.Json.JsonElement e, string ad) => e.GetProperty(ad).GetDecimal();
    private static bool Bos(System.Text.Json.JsonElement e, string ad) =>
        e.GetProperty(ad).ValueKind == System.Text.Json.JsonValueKind.Null;
    private static System.Text.Json.JsonElement Maden(System.Text.Json.JsonElement liste, string maden) =>
        liste.EnumerateArray().Single(m => m.GetProperty("maden").GetString() == maden);

    private Kiraci Kur(KuyumcuDbContext kurulum, string ad) =>
        TestKurulum.KiraciKur(kurulum, "PAKETMADEN-AYRIMI-TEST D " + ad);

    private static async Task<string> Gumus(KuyumcuDbContext db, Kiraci k)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"DG-{id[..8]}", Ad = "Dogrulayici gumus",
            AyarId = "G925", Milyem = 925m, GramAgirlik = 20m, TasAgirlik = 0m, HasGrami = 18.5m,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<object> Degerle(KuyumcuDbContext db, Kiraci k) =>
        await new KarZararServisi(db).StokDegerlemesi(k.Islem, null);

    // ── SIFIR: stokta hiç mal yok → bilinmeyen yok; tutar 0 (null DEĞİL), has 0, kırılım boş
    [Fact]
    public async Task MAS01_Sifir_bos_stok_maliyet_sifir_null_degil()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "sifir");
        using var db = vt.Olustur(k);
        var r = J(await Degerle(db, k));
        Assert.Equal(0m, D(r, "toplamAdet"));
        Assert.Equal(0m, D(r, "toplamHasGram"));
        Assert.Equal(0m, D(r, "stokMaliyetiTl"));
        Assert.False(r.GetProperty("stokMaliyetiKismi").GetBoolean());
        Assert.Equal(0, r.GetProperty("madenKirilimi").GetArrayLength());
    }

    // ── YALNIZ GÜMÜŞ: altın hası 0 (gümüş 3 × 18,5 = 55,5 saf KATILMAZ), maliyet bilinmiyor → null
    [Fact]
    public async Task MAS02_Yalniz_gumus_has_sifir_maliyet_null()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "gumus");
        using var db = vt.Olustur(k);
        var g = await Gumus(db, k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, g, PartiKaynagi.SayimFazla, 3m, 18.5m, DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();
        var r = J(await Degerle(db, k));
        Assert.Equal(0m, D(r, "toplamHasGram"));
        Assert.Equal(0m, D(r, "altinHasGram"));
        var gm = Maden(r.GetProperty("madenKirilimi"), "GUMUS");
        Assert.Equal(55.5m, D(gm, "toplamGram"));
        Assert.Equal(60m, D(gm, "fizikselGram"));
        Assert.True(Bos(r, "stokMaliyetiTl"));
        Assert.True(Bos(r, "stokMaliyetiHas"));
    }

    // ── KURUŞ: birim 2,345 × 1 → 2,35 (AwayFromZero; banker's 2,34 verirdi)
    [Fact]
    public async Task MAS03_Kurus_yuvarlamasi_AwayFromZero()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "kurus");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m, 9.16m,
            DateTime.UtcNow.AddDays(-1), birimMaliyetTl: 2.345m);
        await db.SaveChangesAsync();
        var r = J(await Degerle(db, k));
        Assert.Equal(2.35m, D(r, "stokMaliyetiTl"));
    }

    // ── HAS 4 HANE: altın parti 0,0001 has → toplamHasGram 0,0001; ekranda "0,0001 has"
    [Fact]
    public async Task MAS04_Has_dort_hane_sinir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "has4");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m, 0.0001m,
            DateTime.UtcNow.AddDays(-1), birimMaliyetTl: 1m);
        await db.SaveChangesAsync();
        var r = J(await Degerle(db, k));
        Assert.Equal(0.0001m, D(r, "toplamHasGram"));
        Assert.Equal("0,0001 has", Bicim.Has(D(r, "toplamHasGram")));
    }

    // ── ÇOK BÜYÜK: 2 × 999.999.999,99 → 1.999.999.999,98 (taşma yok, kuruş korunur)
    [Fact]
    public async Task MAS05_Cok_buyuk_tutar()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "buyuk");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 2m, 9.16m,
            DateTime.UtcNow.AddDays(-1), birimMaliyetTl: 999_999_999.99m);
        await db.SaveChangesAsync();
        var r = J(await Degerle(db, k));
        Assert.Equal(1_999_999_999.98m, D(r, "stokMaliyetiTl"));
        Assert.Equal(18.32m, D(r, "toplamHasGram"));
    }

    // ── BOŞ/NULL: ürünü bulunamayan parti → maden bilinmez ⇒ ALTIN sayılır, tartı gramı null (0 DEĞİL)
    [Fact]
    public async Task MAS06_Urunsuz_parti_altin_fiziksel_null()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "urunsuz");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, Kimlik.Ulid(), PartiKaynagi.SayimFazla, 1m, 5m,
            DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();
        var r = J(await Degerle(db, k));
        Assert.Equal(5m, D(r, "toplamHasGram"));
        var al = Maden(r.GetProperty("madenKirilimi"), "ALTIN");
        Assert.True(Bos(al, "fizikselGram"));
        Assert.True(Bos(r, "stokMaliyetiTl"));
    }

    // ═══ DOĞRULAYICI TUR (03.10, Aşkın + Mehmet) — dönem has bacağı sınırları ═══
    private static DateOnly BugunD => DateOnly.FromDateTime(Zaman.UtcdenYerele(DateTime.UtcNow));
    private static Task SatD(KuyumcuDbContext db, Kiraci k, string urunId, decimal adet, decimal birim) =>
        new Kuyumcu.Api.Modules.Satis.SatisServisi(db,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Kuyumcu.Api.Modules.Satis.SatisServisi>.Instance)
            .Satis(new Kuyumcu.Api.Modules.Satis.SatisOlusturDto
            {
                UrunId = urunId, Adet = adet, BirimFiyatTl = birim,
                OdemeTuru = "VERESIYE", KurSnapshotId = k.KurId, CariId = k.CariId,
            }, k.Islem);

    // ── BİLİNMEYEN HAS MALİYETİ: altın satışının TL maliyeti BİLİNİYOR ama partinin has maliyeti
    //    YOK (alış günü has fiyatı verilmemiş → BirimMaliyetHas null). Stok değerlemesi bu durumu
    //    (MA09) "0 sayma" diye düzeltti; dönem has kârı da AYNI kuralla bilinmeyen maliyeti 0 SAYMAMALI:
    //    karHas = hasılatHas − 0 (tüm hasılat kâr) basılırsa bilinmeyen sıfır diye kâra girer.
    [Fact]
    public async Task MAS20_Donem_has_maliyeti_bilinmeyen_altin_satisi_has_kari_tum_hasilat_olmaz()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "hasmaliyetsiz");
        using var db = vt.Olustur(k);
        new MaliyetMotoru(db).PartiAc(k.TenantId, k.SubeId, k.UrunId, PartiKaynagi.SayimFazla, 1m,
            k.UrunHasGrami, DateTime.UtcNow.AddDays(-1), birimMaliyetTl: 1_000m);
        await db.SaveChangesAsync();
        await SatD(db, k, k.UrunId, 1m, 1_500m);

        var r = J(await new KarZararServisi(db).Donem(k.Islem, BugunD, BugunD, null));
        Assert.Equal(500m, D(r, "karTl"));                       // TL bacağı biliniyor: 1.500 − 1.000
        // Has bacağı: maliyetHas bilinmiyor → karHas null OLMALI (tam hasılat kadar has kârı DEĞİL).
        // MADEN-2: ön koşul TOPLAM has hasılatından okunur — has maliyeti bilinmeyen satış artık
        // bilinenHasilatHas'a GİRMEZ (ayrı alan maliyetiEksikHasilatHas); niyet aynı: karHas ≠ tüm hasılat.
        var hasilatHas = D(r, "hasilatHas");
        Assert.True(hasilatHas > 0m);
        Assert.True(Bos(r, "karHas") || D(r, "karHas") != hasilatHas,
            $"has maliyeti bilinmeyen satış has kârına SIFIR maliyetle girdi: karHas={r.GetProperty("karHas")} = bilinenHasilatHas={hasilatHas}");
    }

    // ── SIFIR: dönemde hiç satış yok → TL ve has kârı 0/null ama gümüş/altın karışmaz, has bacağı ALTIN
    [Fact]
    public async Task MAS21_Donem_satissiz_has_bacagi_altin_karDurumu_veri_yok()
    {
        using var kurulum = vt.OlusturSistem();
        var k = Kur(kurulum, "satissiz");
        using var db = vt.Olustur(k);
        var r = J(await new KarZararServisi(db).Donem(k.Islem, BugunD, BugunD, null));
        Assert.Equal("ALTIN", r.GetProperty("hasBacagiMadeni").GetString());
        Assert.Equal(0m, D(r, "hasilatHas"));
    }
}

public sealed class MADENAYRIMIBicimSinirTestleri
{
    private static string M(string? maden, decimal saf, decimal? fiz)
    {
        var kr = KarRaporSozlugu.Al("tr");
        return Bicim.MadenMiktari(maden, saf, fiz, kr.MadenGramFizikselSaf, kr.MadenGramSaf);
    }

    // ── SIFIR / EKSİ / YUVARLAMA: biçim tr-TR sabit, AwayFromZero
    [Fact]
    public void MAS10_Has_ve_gumus_bicim_sinirlari()
    {
        var eski = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        try
        {
            Assert.Equal("0,0000 has", Bicim.Has(0m));
            Assert.Equal("0,0001 has", Bicim.Has(0.00005m));          // AwayFromZero
            Assert.Equal("-0,5000 has", Bicim.Has(-0.5m));            // işaret korunur
            Assert.Equal("0,0000 has", M("ALTIN", 0m, null));
            Assert.Equal("saf 0,01 g", M("GUMUS", 0.005m, null));     // 2 hane AwayFromZero
            Assert.Equal("saf 0,00 g", M("GUMUS", 0m, 0m));
            Assert.Equal("1,9160 has", M(null, 1.916m, 2m));          // maden yok → altın, tartı yok sayılır
            Assert.DoesNotContain("has", M("GUMUS", 999_999_999.99m, 1_000_000_000m));
            // MADEN-AYRIMI tur 2 (tasarım hakemi KRİTİK): has ve maden gramı binlik gruplu (tek sayı dili).
            Assert.Equal("1.000.000.000,00 g (saf 999.999.999,99 g)", M("GUMUS", 999_999_999.99m, 1_000_000_000m));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = eski; }
    }

    // ── DOĞRULAYICI TUR: has binlik + eksi + yarım sınır + çok büyük (tr-TR, kültürden bağımsız)
    [Fact]
    public void MAS12_Has_N4_binlik_eksi_yarim_buyuk()
    {
        var eski = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        try
        {
            Assert.Equal("1.447,3856 has", Bicim.Has(1447.3856m));
            Assert.Equal("-1.234,5679 has", Bicim.Has(-1234.56785m));       // AwayFromZero, işaret korunur
            Assert.Equal("999.999.999,9900 has", Bicim.Has(999_999_999.99m));
            Assert.Equal("1.000,0000 has", M("ALTIN", 999.99995m, null));    // yuvarlama binliği geçer
            Assert.Equal("saf 1.000,00 g", M("GUMUS", 999.995m, null));      // gümüş 2 hane AwayFromZero
            Assert.DoesNotContain("TRY", Bicim.Has(1m));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = eski; }
    }

    // ── TÜRKÇE GİRİŞ (FormAlani.SayiCoz): '45.000' binlik, '1,5' ondalık, '1.500' belirsiz → RED
    [Fact]
    public void MAS11_Turkce_giris()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
        Assert.Equal(0.0001m, FormAlani.SayiCoz("0,0001").Deger);
        Assert.Equal(-1.5m, FormAlani.SayiCoz("-1,5").Deger);
        Assert.Null(FormAlani.SayiCoz("").Deger);
        Assert.Null(FormAlani.SayiCoz(null).Deger);
    }
}
