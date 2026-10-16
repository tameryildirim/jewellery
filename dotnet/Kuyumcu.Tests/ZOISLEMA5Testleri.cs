using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using MPos = Kuyumcu.Api.Modules.Pos;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A 29.09 (tasarım hakemi t3 KRİTİK) — POS YATMASI KAPISI [DB + HTTP]
//
// Olay: Garanti POS cihazının 11.950 ₺'lik tahsilatı, ZİRAAT hesabındaki bir
// MÜŞTERİ havalesiyle eşleştirilebildi; tahsilat "Yattı" oldu.
// Kural (tek kural, üç yol — aday listesi, öneri, eşleştirme):
//   • hareket POS yatması olabilecek satır olmalı (GELEN, müşteriye/faturaya bağlı değil,
//     ters kayıt/virman değil, hesabında en az bir POS cihazı var);
//   • tahsilatın cihazının hesabı = hareketin hesabı (tanımsızsa eşleşmez).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class ZOISLEMA5PosHesapTestleri(TestVeritabani vt)
{
    private static JsonElement Json(object o) =>
        JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    private static Task<object> Eslestir(KuyumcuDbContext db, Kiraci k, string hareket, string t, decimal tutar) =>
        PosYardim.Servis(db).Eslestir(new MPos.PosEslestirDto
        {
            BankaHareketId = hareket,
            Satirlar = [new MPos.PosEslestirSatirDto { PosTahsilatId = t, Tutar = tutar }],
        }, k.Islem);

    private int EslesmeSayisi(Kiraci k)
    {
        using var db = vt.OlusturSistem();
        return db.PosYatmaEslesmeleri.AsNoTracking().Count(e => e.TenantId == k.TenantId);
    }

    [Fact]
    public async Task ZO29g_POS_cihazi_olmayan_hesaptaki_musteri_havalesi_POS_tahsilatini_KAPATAMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 başka hesap");
        var garanti = PosYardim.CihazYaz(vt, k);                       // Garanti · Ana
        var t = PosYardim.TahsilatYaz(vt, k, garanti, 12_000m);
        var ziraat = PosYardim.HesapYaz(vt, k, "Ziraat", "Vadesiz");
        var havale = PosYardim.BankaHareketiYaz(vt, k, 2_456_789.45m, hesapId: ziraat);

        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Eslestir(db, k, havale, t, 11_000m));
        Assert.Contains("bağlı POS cihazı yok", ex.Message);
        Assert.Equal(0, EslesmeSayisi(k));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(PosTahsilatDurumu.Bekliyor, kontrol.PosTahsilatlari.AsNoTracking().Single(x => x.Id == t).Durum);
    }

    [Fact]
    public async Task ZO29g_baska_cihazin_hesabina_yatan_para_bu_cihazin_tahsilatini_KAPATAMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 iki cihaz");
        var garanti = PosYardim.CihazYaz(vt, k);
        var ziraatHesap = PosYardim.HesapYaz(vt, k, "Ziraat", "POS");
        PosYardim.CihazYaz(vt, k, "Ziraat POS", bankaHesapId: ziraatHesap);
        var t = PosYardim.TahsilatYaz(vt, k, garanti, 12_000m);
        var ziraatYatan = PosYardim.BankaHareketiYaz(vt, k, 26_950m, hesapId: ziraatHesap);

        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Eslestir(db, k, ziraatYatan, t, 11_000m));
        Assert.Contains("başka bir banka hesabına yatar", ex.Message);
        Assert.Contains("Garanti POS", ex.Message);
        Assert.Equal(0, EslesmeSayisi(k));
    }

    [Fact]
    public async Task ZO29h_musteriyle_eslesmis_hareket_POS_hesabinda_olsa_da_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 müşteri havalesi");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);
        var havale = PosYardim.BankaHareketiYaz(vt, k, 5_000m, cariId: k.CariId);

        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Eslestir(db, k, havale, t, 500m));
        Assert.Contains("müşteriyle eşleşmiş", ex.Message);
        var ex2 = await Assert.ThrowsAsync<BadRequestException>(() => PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = havale }, k.Islem));
        Assert.Contains("müşteriyle eşleşmiş", ex2.Message);
        Assert.Equal(0, EslesmeSayisi(k));
    }

    [Fact]
    public async Task ZO29h_ters_kaydi_yapilmis_ve_virman_hareketi_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 ters virman");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 1_000m);
        var kaynak = PosYardim.BankaHareketiYaz(vt, k, 900m);
        var virman = PosYardim.BankaHareketiYaz(vt, k, 900m);
        var tersId = PosYardim.BankaHareketiYaz(vt, k, 900m, "GIDEN");
        using (var db = vt.OlusturSistem())
        {
            db.BankaHareketleri.Single(h => h.Id == tersId).StornoKaynakId = kaynak;
            db.BankaHareketleri.Single(h => h.Id == virman).KarsiBacakId = Kimlik.Ulid();
            db.SaveChanges();
        }

        using var db2 = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Eslestir(db2, k, kaynak, t, 900m));
        Assert.Contains("ters kayıtla kapatılmış", ex.Message);
        var ex2 = await Assert.ThrowsAsync<BadRequestException>(() => Eslestir(db2, k, virman, t, 900m));
        Assert.Contains("hesaplar arası aktarım", ex2.Message);
        Assert.Equal(0, EslesmeSayisi(k));
    }

    [Fact]
    public async Task ZO29i_cihazin_hesabi_TANIMSIZSA_eslesmez_400()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 hesapsız cihaz");
        PosYardim.CihazYaz(vt, k);                                       // hesap POS hesabı olsun
        var hesapsiz = PosYardim.CihazYaz(vt, k, "Eski POS", hesapsiz: true);
        var t = PosYardim.TahsilatYaz(vt, k, hesapsiz, 1_000m);
        var hareket = PosYardim.BankaHareketiYaz(vt, k, 981.50m);

        using var db = vt.Olustur(k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Eslestir(db, k, hareket, t, 981.50m));
        Assert.Contains("Eski POS", ex.Message);
        Assert.Contains("hesabı tanımlı değil", ex.Message);
        Assert.Equal(0, EslesmeSayisi(k));
    }

    [Fact]
    public async Task ZO29j_oneri_YALNIZ_hareketin_hesabindaki_cihazlari_sunar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 öneri hesap");
        var garanti = PosYardim.CihazYaz(vt, k);
        var ziraatHesap = PosYardim.HesapYaz(vt, k, "Ziraat", "POS");
        var ziraat = PosYardim.CihazYaz(vt, k, "Ziraat POS", bankaHesapId: ziraatHesap);
        PosYardim.TahsilatYaz(vt, k, garanti, 1_000m);                  // net 981,50 — Garanti
        var tz = PosYardim.TahsilatYaz(vt, k, ziraat, 1_000m);          // net 981,50 — Ziraat
        var ziraatYatan = PosYardim.BankaHareketiYaz(vt, k, 981.50m, hesapId: ziraatHesap);

        using var db = vt.Olustur(k);
        var j = Json(await PosYardim.Servis(db)
            .OtoEslestirOnerisi(new MPos.PosOtoOneriDto { BankaHareketId = ziraatYatan }, k.Islem));
        Assert.True(j.GetProperty("bulundu").GetBoolean());
        var satirlar = j.GetProperty("satirlar").EnumerateArray().ToList();
        Assert.Single(satirlar);
        Assert.Equal(tz, satirlar[0].GetProperty("posTahsilatId").GetString());
        var uygun = j.GetProperty("uygunCihazlar").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal([ziraat], uygun);
    }

    [Fact]
    public async Task ZO29k_yatma_adaylari_yalniz_POS_hesabindaki_ACIK_hareketler_hesap_adi_ve_KALAN_ile()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "ZO5 adaylar");
        var cihaz = PosYardim.CihazYaz(vt, k);
        var t = PosYardim.TahsilatYaz(vt, k, cihaz, 20_000m);           // net 19.630
        var ziraat = PosYardim.HesapYaz(vt, k, "Ziraat", "Vadesiz");
        var acik = PosYardim.BankaHareketiYaz(vt, k, 26_950m);
        var kismi = PosYardim.BankaHareketiYaz(vt, k, 15_000m);
        var musteri = PosYardim.BankaHareketiYaz(vt, k, 26_000m, cariId: k.CariId);
        var giden = PosYardim.BankaHareketiYaz(vt, k, 26_100m, "GIDEN");
        var baskaHesap = PosYardim.BankaHareketiYaz(vt, k, 2_600m, hesapId: ziraat);
        using (var db = vt.Olustur(k)) await Eslestir(db, k, kismi, t, 3_050m);   // kalan 11.950

        using var db2 = vt.Olustur(k);
        var y = await PosYardim.Servis(db2).YatmaAdaylari("", 10, "tr", k.TenantId);
        var kimlikler = y.Sonuclar.Select(s => s.Kimlik).ToHashSet();
        Assert.Contains(acik, kimlikler);
        Assert.Contains(kismi, kimlikler);
        Assert.DoesNotContain(musteri, kimlikler);
        Assert.DoesNotContain(giden, kimlikler);
        Assert.DoesNotContain(baskaHesap, kimlikler);
        Assert.True(y.Gozatma);
        var ks = y.Sonuclar.Single(s => s.Kimlik == kismi);
        // 29.09 (hakem YÜKSEK): TUTAR birincil (ad); hesap · tarih ikincil; kalan yalnız kısmi harekette (ikincil satır sonu).
        Assert.Equal("+" + Kuyumcu.Api.Modules.Arama.AramaServisi.EkranPara(15_000m, "TRY"), ks.Ad);
        Assert.StartsWith("Garanti · Ana · ", ks.AltBilgi);
        Assert.EndsWith(" · kalan " + Kuyumcu.Api.Modules.Arama.AramaServisi.EkranPara(11_950m, "TRY"), ks.AltBilgi);
        var ac = y.Sonuclar.Single(s => s.Kimlik == acik);
        Assert.Equal("+" + Kuyumcu.Api.Modules.Arama.AramaServisi.EkranPara(26_950m, "TRY"), ac.Ad);
        Assert.DoesNotContain("kalan", ac.AltBilgi);

        // Sorgu: tutar öneki ve hesap adı; EN dil anahtarı.
        var y2 = await PosYardim.Servis(db2).YatmaAdaylari("26", 10, "en", k.TenantId);
        Assert.Equal([acik], y2.Sonuclar.Select(s => s.Kimlik).ToList());
        Assert.Equal("+" + Kuyumcu.Api.Modules.Arama.AramaServisi.EkranPara(26_950m, "TRY"), y2.Sonuclar[0].Ad);
        var y2k = await PosYardim.Servis(db2).YatmaAdaylari("15", 10, "en", k.TenantId);
        Assert.Equal([kismi], y2k.Sonuclar.Select(s => s.Kimlik).ToList());
        Assert.Contains(" · remaining ", y2k.Sonuclar[0].AltBilgi);
        var y3 = await PosYardim.Servis(db2).YatmaAdaylari("Ziraat", 10, "tr", k.TenantId);
        Assert.Empty(y3.Sonuclar);

        // Tam eşleşen hareket listeden düşer.
        using (var db3 = vt.Olustur(k)) await Eslestir(db3, k, kismi, t, 11_950m);
        using var db4 = vt.Olustur(k);
        var y4 = await PosYardim.Servis(db4).YatmaAdaylari("", 10, "tr", k.TenantId);
        Assert.DoesNotContain(kismi, y4.Sonuclar.Select(s => s.Kimlik));
    }
}

[Collection("http")]
public sealed class ZOISLEMA5HttpTestleri(HttpFikstur fikstur)
{
    [Fact]
    public async Task ZO29k_yatma_adaylari_ucu_MUHASEBE_200_camelCase_TEZGAHTAR_403()
    {
        var k = TestKurulum.KiraciKur(fikstur.Vt.OlusturSistem(), "ZO5 HTTP adaylar");
        PosYardim.CihazYaz(fikstur.Vt, k, $"POS-{Guid.NewGuid():N}"[..12]);
        var hareket = PosYardim.BankaHareketiYaz(fikstur.Vt, k, 4_321m);

        var m = fikstur.Ortam.Istemci(k, "MUHASEBE");
        var y = await m.GetAsync("/api/v1/pos/yatma-adaylari?q=&dil=tr");
        var govde = await y.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var j = JsonDocument.Parse(govde).RootElement;
        var ilk = j.GetProperty("sonuclar").EnumerateArray().Single();
        Assert.Equal(hareket, ilk.GetProperty("kimlik").GetString());
        Assert.Equal("+" + Kuyumcu.Api.Modules.Arama.AramaServisi.EkranPara(4_321m, "TRY"), ilk.GetProperty("ad").GetString());
        Assert.StartsWith("Garanti · Ana · ", ilk.GetProperty("altBilgi").GetString());

        var tz = fikstur.Ortam.Istemci(k, "TEZGAHTAR");
        var y2 = await tz.GetAsync("/api/v1/pos/yatma-adaylari?q=");
        Assert.Equal(HttpStatusCode.Forbidden, y2.StatusCode);
    }
}
