using System.Text.Json;
using Kuyumcu.Api.Data;
using MMasak = Kuyumcu.Api.Modules.Masak;
using MPos = Kuyumcu.Api.Modules.Pos;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A 29.09 (16. tur — tasarım hakemi KRİTİK + YÜKSEK) [DB]
//
// (1) MASAK gereksinimi müşteriyle sorulabilir: kademe, kaydın geçtiği kümülatif
//     kapıyla (MasakKapisi.GunlukIslemToplam) AYNI ölçüyle hesaplanır. Eskiden ekran
//     yalnız işlem tutarını soruyordu (185.000 → YALNIZ_KIMLIK), kayıt ise günlük
//     toplamla reddediliyordu → amaç/açıklama/fon alanları hiç çizilmiyordu (döngü).
// (2) POS "Öneri getir": gün kümesi tutmasa da kalanı hareketin eşleşmemiş tutarına
//     BİREBİR eşit TEK tahsilat önerilir; birden çok eşit aday → öneri YOK.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class ZOISLEMA6Testleri(TestVeritabani vt)
{
    private static JsonElement Json(object o) =>
        JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    private static MMasak.MasakServisi Masak(KuyumcuDbContext db) =>
        new(db, new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri()), TestKurulum.Denetim(db));

    [Fact]
    public async Task ZO29z_MASAK_gereksinimi_MUSTERIYLE_gunluk_toplam_uzerinden_kapiyla_ayni()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO6 masak kümülatif");
        var diger = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO6 masak diğer");
        // Aynı gün aynı müşteriyle 60.000 ₺ elle dekont (MASAK kümülatifine sayılır).
        PosYardim.BankaHareketiYaz(vt, k, 60_000m, cariId: k.CariId);

        using var db = vt.Olustur(k);
        var s = Masak(db);
        // Müşterisiz: yalnız bu işlem (150.000 < kimlik eşiği) → hiçbir şey istenmez.
        var tek = await s.Gereksinim(150_000m, null, k.TenantId);
        Assert.False(tek.KimlikGerekli);
        // Müşteriyle: 60.000 + 150.000 = 210.000 → kimlik + amaç/açıklama.
        var kum = await s.Gereksinim(150_000m, k.CariId, k.TenantId);
        Assert.True(kum.KimlikGerekli);
        Assert.True(kum.AmacGerekli);
        // Tek kaynak: kapının kendi ölçüsüyle aynı sonuç.
        var toplam = await MMasak.MasakKapisi.GunlukIslemToplam(db, k.TenantId, k.CariId, DateTime.UtcNow);
        Assert.Equal(new MMasak.MasakKuralMotoru(new MMasak.MasakEsikleri()).Gereksinim(toplam + 150_000m), kum);

        // Başka kiracının müşterisi: toplam okunmaz (0) → tek işlem kuralı.
        using var db2 = vt.Olustur(diger);
        var yabanci = await Masak(db2).Gereksinim(150_000m, k.CariId, diger.TenantId);
        Assert.False(yabanci.KimlikGerekli);
    }

    [Fact]
    public async Task ZO29z_oneri_gun_kumesi_tutmasa_da_BIREBIR_tutan_TEK_tahsilati_onerir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO6 öneri birebir");
        var cihaz = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihaz, 20_000m);                 // net 19.630 — aynı gün
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);         // net 9.815 — aynı gün
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 9_815m);

        using var db = vt.Olustur(k);
        var j = Json(await PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = hareket }, k.Islem));
        Assert.True(j.GetProperty("bulundu").GetBoolean());
        var satirlar = j.GetProperty("satirlar").EnumerateArray().ToList();
        Assert.Single(satirlar);
        Assert.Equal(t, satirlar[0].GetProperty("posTahsilatId").GetString());
        Assert.Equal(9_815m, satirlar[0].GetProperty("tutar").GetDecimal());
        Assert.Equal(9_815m, j.GetProperty("toplam").GetDecimal());
        // Öneri YAZMAZ.
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, kontrol.PosYatmaEslesmeleri.Count(e => e.TenantId == k.TenantId));
    }

    [Fact]
    public async Task ZO29z_oneri_BIREBIR_iki_aday_varsa_varsayim_yapmaz_BOS_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO6 öneri iki aday");
        var cihaz = PosYardim.CihazYaz(vt, k);
        PosYardim.TahsilatYaz(vt, k, cihaz, 20_000m);
        PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 9_815m);

        using var db = vt.Olustur(k);
        var j = Json(await PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = hareket }, k.Islem));
        Assert.False(j.GetProperty("bulundu").GetBoolean());
        Assert.Empty(j.GetProperty("satirlar").EnumerateArray());
    }

    [Fact]
    public async Task ZO29z_oneri_BIREBIR_kismi_eslesmis_harekette_KALANA_bakar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO6 öneri kalan");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var ilk = PosYardim.TahsilatYaz(vt, k, cihaz, 20_000m);       // net 19.630
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 10_000m);         // net 9.815
        PosYardim.TahsilatYaz(vt, k, cihaz, 5_000m);                  // net 4.907,50 — kümeyi bozar
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 19_630m + 9_815m);
        using (var db0 = vt.Olustur(k))
            await PosYardim.Servis(db0).Eslestir(new MPos.PosEslestirDto
            {
                BankaHareketId = hareket,
                Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = ilk, Tutar = 19_630m }],
            }, k.Islem);

        using var db = vt.Olustur(k);
        var j = Json(await PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = hareket }, k.Islem));
        Assert.True(j.GetProperty("bulundu").GetBoolean());
        Assert.Equal(9_815m, j.GetProperty("hareketKalan").GetDecimal());
        Assert.Equal(t, j.GetProperty("satirlar")[0].GetProperty("posTahsilatId").GetString());
    }
}
