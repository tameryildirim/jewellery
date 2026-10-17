using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M7a — gelen banka ödemesi (kurucu 17.10):
//   • "müşteri listesinden otomatik bulsun, öyle bir müşteri yoksa otomatik kaydetsin"
//   • "borçtan düşme işini müşteriye sor… müşteri karar versin"
//   • "müşteri bilgilerine bir IBAN listesi gelsin, birden fazla IBAN'la işlem yapabilir"
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class M7aGelenOdemeHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "M7a-" + ad);
    }

    /// <summary>Algoritmaya uygun TCKN (d10, d11 kontrol haneleri).</summary>
    private static string Tckn(string tohum)
    {
        var h = Math.Abs(tohum.GetHashCode());
        var d = new int[11];
        d[0] = 1 + h % 9;
        for (var i = 1; i < 9; i++) { h = h / 7 + 13 * i; d[i] = h % 10; }
        d[9] = (((d[0] + d[2] + d[4] + d[6] + d[8]) * 7 - (d[1] + d[3] + d[5] + d[7])) % 10 + 10) % 10;
        d[10] = d.Take(10).Sum() % 10;
        return string.Concat(d);
    }

    private async Task<string> HesapAc(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/bankalar", new
        { bankaAdi = "Ziraat", hesapAdi = "Ticari", iban = TestIban.Uret(), paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private async Task<JsonElement> Dekont(Kiraci k, string hesap, object govde)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync($"/api/v1/bankalar/{hesap}/hareket", govde);
        var y = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(c.StatusCode == HttpStatusCode.OK, y.ToString());
        return y;
    }

    private async Task<string> CariAc(Kiraci k, string ad, string? tc = null)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/cari", new { tur = "MUSTERI", ad, vergiNo = tc });
        var y = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(c.StatusCode == HttpStatusCode.Created, y.ToString());
        return y.GetProperty("id").GetString()!;
    }

    private decimal TlBakiye(Kiraci k, string cariId)
    {
        using var db = f.Vt.OlusturSistem();
        return db.CariBakiyeler.AsNoTracking().Where(b => b.TenantId == k.TenantId && b.CariId == cariId)
            .Select(b => b.TlBakiye).FirstOrDefault();
    }

    [Fact]
    public async Task TC_ile_mevcut_musteri_bulunur_ve_IBAN_listesine_ogrenilir_ikinci_odeme_IBAN_ile_bulunur()
    {
        var k = YeniKiraci("tc");
        var hesap = await HesapAc(k);
        var tc = Tckn(k.TenantId);
        var cari = await CariAc(k, "Yunus Kaçkar", tc);
        var iban = TestIban.Uret();

        var y1 = await Dekont(k, hesap, new { yon = "GELEN", tutar = 1000m, karsiTarafAd = "YUNUS KACKAR", karsiIban = iban, karsiKimlikNo = tc });
        Assert.Equal("KIMLIK", y1.GetProperty("cari").GetProperty("yol").GetString());
        Assert.Equal(cari, y1.GetProperty("cari").GetProperty("cariId").GetString());

        var ibanlar = await f.Ortam.Istemci(k, "SAHIP").GetFromJsonAsync<JsonElement>($"/api/v1/cari/{cari}/ibanlar");
        Assert.Equal(iban, ibanlar.EnumerateArray().Single().GetProperty("iban").GetString());

        var y2 = await Dekont(k, hesap, new { yon = "GELEN", tutar = 500m, karsiTarafAd = "Y KACKAR", karsiIban = iban });
        Assert.Equal("IBAN", y2.GetProperty("cari").GetProperty("yol").GetString());
        Assert.Equal(cari, y2.GetProperty("cari").GetProperty("cariId").GetString());
    }

    [Fact]
    public async Task Bulunamayan_kisi_TC_ya_da_IBAN_ile_YENI_musteri_olarak_kaydedilir()
    {
        var k = YeniKiraci("yeni");
        var hesap = await HesapAc(k);
        var iban = TestIban.Uret();
        var y = await Dekont(k, hesap, new { yon = "GELEN", tutar = 750m, karsiTarafAd = "Nermin Tuzcuoğlu", karsiIban = iban });

        Assert.Equal("YENI", y.GetProperty("cari").GetProperty("yol").GetString());
        var cariId = y.GetProperty("cari").GetProperty("cariId").GetString()!;
        using var db = f.Vt.OlusturSistem();
        var cari = await db.Cariler.AsNoTracking().SingleAsync(c => c.Id == cariId && c.TenantId == k.TenantId);
        Assert.Equal("Nermin Tuzcuoğlu", cari.Ad);
        Assert.Equal("MUSTERI", cari.Tur);
        Assert.Equal(1, await db.CariIbanlari.CountAsync(c => c.TenantId == k.TenantId && c.CariId == cariId));
        var dekont = await db.BankaHareketleri.AsNoTracking().SingleAsync(b => b.TenantId == k.TenantId);
        Assert.Equal("ESLESTI", dekont.Durum);
        Assert.Equal(cariId, dekont.CariId);
    }

    [Fact]
    public async Task Yalniz_ad_varsa_musteri_ACILMAZ_POS_faiz_gibi_satirlar_musteri_sanilmaz()
    {
        var k = YeniKiraci("adyalniz");
        var hesap = await HesapAc(k);
        var y = await Dekont(k, hesap, new { yon = "GELEN", tutar = 300m, karsiTarafAd = "POS BLOKE COZUM" });
        Assert.Equal("YOK", y.GetProperty("cari").GetProperty("yol").GetString());
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.Cariler.AnyAsync(c => c.TenantId == k.TenantId && c.Ad == "POS BLOKE COZUM"));
        Assert.Equal("BEKLIYOR", (await db.BankaHareketleri.AsNoTracking().SingleAsync(b => b.TenantId == k.TenantId)).Durum);
    }

    [Fact]
    public async Task Adi_benzeyen_musteri_varsa_BAGLANMAZ_oneri_doner()
    {
        var k = YeniKiraci("benzer");
        var hesap = await HesapAc(k);
        var mevcut = await CariAc(k, "Mehmet Ali Yıldırım");
        var y = await Dekont(k, hesap, new { yon = "GELEN", tutar = 400m, karsiTarafAd = "MEHMET ALİ YILDIRIM", karsiIban = TestIban.Uret() });

        Assert.Equal("ONERI", y.GetProperty("cari").GetProperty("yol").GetString());
        Assert.Contains(y.GetProperty("cari").GetProperty("oneriler").EnumerateArray(), o => o.GetProperty("id").GetString() == mevcut);
        using var db = f.Vt.OlusturSistem();
        Assert.Null((await db.BankaHareketleri.AsNoTracking().SingleAsync(b => b.TenantId == k.TenantId)).CariId);
        Assert.Equal(0, await db.CariIbanlari.CountAsync(c => c.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Borcu_olan_musteride_karar_sorulur_BORC_borcu_dusurur_geri_al_borcu_geri_getirir()
    {
        var k = YeniKiraci("borc");
        var hesap = await HesapAc(k);
        var tc = Tckn(k.TenantId + "b");
        var cari = await CariAc(k, "Ayşe Borçlu", tc);
        var s = f.Ortam.Istemci(k, "SAHIP");
        var v = await s.PostAsJsonAsync($"/api/v1/cari/{cari}/veresiye", new { tlTutari = 10000m, hasGrami = 0m, kurSnapshotId = k.KurId, vadeGun = 30 });
        Assert.True(v.IsSuccessStatusCode, await v.Content.ReadAsStringAsync());

        var y = await Dekont(k, hesap, new { yon = "GELEN", tutar = 4000m, karsiTarafAd = "AYSE BORCLU", karsiKimlikNo = tc });
        var dekontId = y.GetProperty("hareketId").GetString()!;
        Assert.Equal(10000m, TlBakiye(k, cari));                 // bağlandı ama borç KENDİLİĞİNDEN düşmedi

        var bekleyen = await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar/karar-bekleyen");
        var satir = bekleyen.EnumerateArray().Single(x => x.GetProperty("id").GetString() == dekontId);
        Assert.Equal(10000m, satir.GetProperty("borcTl").GetDecimal());

        var kr = await s.PostAsJsonAsync($"/api/v1/bankalar/hareketler/{dekontId}/karar", new { karar = "BORC" });
        Assert.True(kr.IsSuccessStatusCode, await kr.Content.ReadAsStringAsync());
        Assert.Equal(6000m, TlBakiye(k, cari));
        var tekrar = await s.PostAsJsonAsync($"/api/v1/bankalar/hareketler/{dekontId}/karar", new { karar = "BORC" });
        Assert.Equal(HttpStatusCode.Conflict, tekrar.StatusCode);

        using (var db = f.Vt.OlusturSistem())
        {
            var b = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == dekontId);
            Assert.Null(b.HareketId);                              // MASAK: para dekontla sayılmaya devam eder
            var t = await db.Hareketler.AsNoTracking().SingleAsync(x => x.Id == b.KararHareketId);
            Assert.Equal("TAHSILAT", t.Tur);
            Assert.Equal(-4000m, t.TlTutari);
            Assert.Equal(0m, t.MasakOlcuTl);                        // ve tahsilat satırı ikinci kez saymaz
            Assert.Equal(0, await db.KasaHareketleri.CountAsync(x => x.TenantId == k.TenantId && x.HareketId == t.Id));
        }
        Assert.Empty((await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar/karar-bekleyen")).EnumerateArray());

        // Borçtan düşülmüş dekont doğrudan stornolanamaz
        var st = await s.PostAsJsonAsync($"/api/v1/bankalar/{hesap}/hareketler/{dekontId}/storno", new { gerekce = "yanlış dekont girildi" });
        Assert.Equal(HttpStatusCode.Conflict, st.StatusCode);

        var geri = await s.PostAsync($"/api/v1/bankalar/hareketler/{dekontId}/karar/geri-al", null);
        Assert.True(geri.IsSuccessStatusCode, await geri.Content.ReadAsStringAsync());
        Assert.Equal(10000m, TlBakiye(k, cari));
        Assert.Single((await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar/karar-bekleyen")).EnumerateArray());
    }

    [Fact]
    public async Task SATIS_karari_borca_dokunmaz_ve_listeden_cikar()
    {
        var k = YeniKiraci("satis");
        var hesap = await HesapAc(k);
        var cari = await CariAc(k, "Cemil Peşin");
        var s = f.Ortam.Istemci(k, "SAHIP");
        await s.PostAsJsonAsync($"/api/v1/cari/{cari}/veresiye", new { tlTutari = 2000m, hasGrami = 0m, kurSnapshotId = k.KurId, vadeGun = 30 });
        var y = await Dekont(k, hesap, new { yon = "GELEN", tutar = 900m, cariId = cari });
        var id = y.GetProperty("hareketId").GetString()!;

        var kr = await s.PostAsJsonAsync($"/api/v1/bankalar/hareketler/{id}/karar", new { karar = "SATIS" });
        Assert.True(kr.IsSuccessStatusCode, await kr.Content.ReadAsStringAsync());
        Assert.Equal(2000m, TlBakiye(k, cari));
        Assert.Empty((await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar/karar-bekleyen")).EnumerateArray());
        var gecersiz = await s.PostAsJsonAsync($"/api/v1/bankalar/hareketler/{id}/karar", new { karar = "HEPSI" });
        Assert.Equal(HttpStatusCode.BadRequest, gecersiz.StatusCode);
    }

    [Fact]
    public async Task Tezgahtar_karar_veremez_ve_listeyi_goremez()
    {
        var k = YeniKiraci("rol");
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.Forbidden, (await t.GetAsync("/api/v1/bankalar/karar-bekleyen")).StatusCode);
    }

    [Fact]
    public async Task Musteri_IBAN_listesi_coklu_IBAN_bir_IBAN_tek_musteriye_gecersiz_IBAN_400()
    {
        var k = YeniKiraci("iban");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var a = await CariAc(k, "İki Hesaplı Müşteri");
        var b = await CariAc(k, "Başka Müşteri");
        var i1 = TestIban.Uret();
        var i2 = TestIban.Uret();

        Assert.True((await s.PostAsJsonAsync($"/api/v1/cari/{a}/ibanlar", new { iban = i1 })).IsSuccessStatusCode);
        var r2 = await s.PostAsJsonAsync($"/api/v1/cari/{a}/ibanlar", new { iban = i2 });
        var liste = await r2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, liste.GetArrayLength());

        var cakisma = await s.PostAsJsonAsync($"/api/v1/cari/{b}/ibanlar", new { iban = i1 });
        Assert.Equal(HttpStatusCode.Conflict, cakisma.StatusCode);
        Assert.Contains("İki Hesaplı Müşteri", (await cakisma.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());

        var bozuk = await s.PostAsJsonAsync($"/api/v1/cari/{a}/ibanlar", new { iban = "TR000000000000000000000001" });
        Assert.Equal(HttpStatusCode.BadRequest, bozuk.StatusCode);

        var silinen = liste.EnumerateArray().First();
        var sil = await s.DeleteAsync($"/api/v1/cari/{a}/ibanlar/{silinen.GetProperty("id").GetString()}");
        Assert.Equal(1, (await sil.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
        // Kaldırılan IBAN artık başka müşteriye eklenebilir
        var tasi = await s.PostAsJsonAsync($"/api/v1/cari/{b}/ibanlar", new { iban = silinen.GetProperty("iban").GetString() });
        Assert.True(tasi.IsSuccessStatusCode, await tasi.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Hesabi_baska_belgeyle_islenmis_para_KAPALI_listeye_dusmez_karar_409()
    {
        var k = YeniKiraci("kapali");
        var hesap = await HesapAc(k);
        var id = Kuyumcu.Api.Common.Kimlik.Ulid();
        using (var db = f.Vt.OlusturSistem())
        {
            db.BankaHareketleri.Add(new BankaHareket
            {
                Id = id, TenantId = k.TenantId, BankaHesapId = hesap, Yon = "GELEN", Tutar = 500m, ParaBirimi = "TRY",
                CariId = k.CariId, Durum = "ESLESTI", CariKarari = Kuyumcu.Api.Modules.Banka.CariKarari.Kapali,
                Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.Empty((await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar/karar-bekleyen")).EnumerateArray());
        var kr = await s.PostAsJsonAsync($"/api/v1/bankalar/hareketler/{id}/karar", new { karar = "BORC" });
        Assert.Equal(HttpStatusCode.Conflict, kr.StatusCode);
    }

    // ── 17.10 kısmi ödeme / borçlu satış seçimi (KISMI-ODEME-ARASTIRMASI § 5) ──────────────────
    private async Task<string> VeresiyeSatis(Kiraci k, decimal tutar)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId, fisIskontoYuzde = 0m,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = tutar, hediye = false } },
            odemeler = new[] { new { arac = "VERESIYE", tutarTl = tutar, kasaId = (string?)null } },
        });
        var y = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(c.IsSuccessStatusCode, y.ToString());
        using var db = f.Vt.OlusturSistem();
        var fisId = y.GetProperty("fisId").GetString();
        return db.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.FisId == fisId && h.Tur == "SATIS").Id;
    }

    private async Task<Dictionary<string, decimal>> AcikSatislar(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var (borclar, _) = await Kuyumcu.Api.Modules.Vade.VadeKapisi.AcikBorclar(db, k.TenantId, k.CariId, DateTime.UtcNow);
        return borclar.ToDictionary(b => b.HareketId, b => b.AcikTutar);
    }

    [Fact]
    public async Task Borclu_satis_listesi_TBK102_onerisiyle_gelir_secilen_satis_kapanir_en_eski_acik_kalir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "M7a-mahsup", urunGram: 1m);
        var hesap = await HesapAc(k);
        var s1 = await VeresiyeSatis(k, 10_000m);
        var s2 = await VeresiyeSatis(k, 15_000m);
        var d = (await Dekont(k, hesap, new { yon = "GELEN", tutar = 12_000m, cariId = k.CariId })).GetProperty("hareketId").GetString()!;
        var s = f.Ortam.Istemci(k, "SAHIP");

        var satir = (await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar/karar-bekleyen")).EnumerateArray().Single(x => x.GetProperty("id").GetString() == d);
        var liste = satir.GetProperty("borcluSatislar").EnumerateArray().ToList();
        Assert.Equal([s1, s2], liste.Select(x => x.GetProperty("hareketId").GetString()));       // en eski önce
        Assert.Equal(10_000m, liste[0].GetProperty("oneriTl").GetDecimal());                     // öneri en eskiden
        Assert.Equal(2_000m, liste[1].GetProperty("oneriTl").GetDecimal());
        Assert.False(liste[0].GetProperty("faturaSuresiGecti").GetBoolean());

        // Müşteri "yeni olanı ödüyorum" dedi (TBK 101/1): 12.000 → ikinci satış
        var kr = await s.PostAsJsonAsync($"/api/v1/bankalar/hareketler/{d}/karar",
            new { karar = "BORC", mahsuplar = new[] { new { borcHareketId = s2, tutarTl = 12_000m } } });
        Assert.True(kr.IsSuccessStatusCode, await kr.Content.ReadAsStringAsync());
        var acik = await AcikSatislar(k);
        Assert.Equal(10_000m, acik[s1]);          // en eski DOKUNULMADI (FIFO olsaydı kapanırdı)
        Assert.Equal(3_000m, acik[s2]);

        // Geri al → mahsup yok sayılır, borç geri gelir
        Assert.True((await s.PostAsync($"/api/v1/bankalar/hareketler/{d}/karar/geri-al", null)).IsSuccessStatusCode);
        // Storno çifti yürüyüşte birbirini götürür: iki satış da TAM borcuyla açık (vadesiz parti doğmaz).
        var sonra = await AcikSatislar(k);
        Assert.Equal(10_000m, sonra[s1]);
        Assert.Equal(15_000m, sonra[s2]);
    }

    [Fact]
    public async Task Hatali_secimler_400_ve_hicbir_sey_yazilmaz()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "M7a-mahsup-red", urunGram: 1m);
        var hesap = await HesapAc(k);
        var s1 = await VeresiyeSatis(k, 10_000m);
        var d = (await Dekont(k, hesap, new { yon = "GELEN", tutar = 5_000m, cariId = k.CariId })).GetProperty("hareketId").GetString()!;
        var s = f.Ortam.Istemci(k, "SAHIP");

        foreach (var govde in new object[]
        {
            new { karar = "BORC", mahsuplar = new[] { new { borcHareketId = s1, tutarTl = 6_000m } } },        // gelen paradan fazla
            new { karar = "BORC", mahsuplar = new[] { new { borcHareketId = "baskasi", tutarTl = 1_000m } } }, // açık borç değil
            new { karar = "BORC", mahsuplar = new[] { new { borcHareketId = s1, tutarTl = 1_000.001m } } },    // 3 ondalık
        })
        {
            var r = await s.PostAsJsonAsync($"/api/v1/bankalar/hareketler/{d}/karar", govde);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }
        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, kontrol.CariMahsuplar.Count(m => m.TenantId == k.TenantId));
        Assert.Equal(0, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
        Assert.Null(kontrol.BankaHareketleri.Single(b => b.Id == d).CariKarari);
    }
}
