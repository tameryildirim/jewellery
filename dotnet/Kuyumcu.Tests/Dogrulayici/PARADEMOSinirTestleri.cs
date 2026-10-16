using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MKasa = Kuyumcu.Api.Modules.Kasa;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT ve ELLE hesaplanmıştır
// (üretim kodundan türetilmez). Paketin dokunduğu para yolu: gün sonu sayımında
// NAKİT defteri = kasa bakiyesi − Σ kart − Σ havale (KasaNakitAyrimi), Günü Kapat
// farkı (MidpointRounding.AwayFromZero, 2 hane), has ile tahsilat (TL'ye çevrilmez).
// Sınırlar: 0 · eksi · kuruş 0,005 · has 0,0001 · 999.999.999,99 · boş/null ·
// Türkçe giriş ('45.000', '1,5', belirsiz '1.500' reddi — FormAlani.SayiCoz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class PARADEMOSinirDbTestleri(TestVeritabani vt)
{
    private const decimal Acilis = 250_000m;   // TestKurulum.KasaAcilis (hareketsiz devir) — sabit

    private static MKasa.KasaServisi Kasa(KuyumcuDbContext db) => new(db, NullLogger<MKasa.KasaServisi>.Instance);
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) => new(db, NullLogger<MSatis.SatisServisi>.Instance);
    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, TestKurulum.Denetim(db));

    private Kiraci YeniKiraci(string ad)
    {
        using var db = vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private async Task SatisYap(Kiraci k, string odeme, decimal tutar)
    {
        using var db = vt.Olustur(k);
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
            OdemeTuru = odeme, KasaId = odeme == "VERESIYE" ? null : k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
    }

    private async Task ElleHareket(Kiraci k, string yon, decimal tutar, string odeme, decimal? komisyon = null)
    {
        using var db = vt.Olustur(k);
        await Kasa(db).HareketGir(k.KasaId, new MKasa.KasaHareketDto
        {
            Yon = yon, Tutar = tutar, OdemeTuru = odeme, KomisyonOran = komisyon, Aciklama = "sınır testi",
        }, k.Islem);
    }

    private async Task<MKasa.KasaNakitAyrimiSonuc> Ayrim(Kiraci k)
    {
        using var db = vt.Olustur(k);
        return await MKasa.KasaNakitAyrimi.Hesapla(db, k.TenantId,
            db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId));
    }

    // ── SIFIR ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PDS_sifir_hareketsiz_kasada_nakit_esittir_toplam_fark_sifir()
    {
        var k = YeniKiraci("PDS sıfır");
        var a = await Ayrim(k);
        Assert.Equal(0m, a.Kart);
        Assert.Equal(0m, a.Havale);
        Assert.Equal(250_000m, a.Nakit);
        Assert.Equal(250_000m, a.ToplamBakiye);

        using var db = vt.Olustur(k);
        await Kasa(db).GunKapat(k.KasaId, new MKasa.GunKapanisiDto { SayilanNakit = 250_000m }, k.Islem);
        using var kontrol = vt.OlusturSistem();
        var g = kontrol.GunKapanislari.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0m, g.Fark);
        Assert.Equal(250_000m, g.DefterBakiye);
    }

    // ── EKSİ: kart çıkışı (POS iadesi gibi) nakdi DEĞİŞTİRMEZ ─────────────────
    [Fact]
    public async Task PDS_eksi_kart_cikisi_nakit_defterini_degistirmez()
    {
        var k = YeniKiraci("PDS eksi kart");
        await SatisYap(k, "KART", 500m);
        await ElleHareket(k, "CIKIS", 200m, "KART");
        var a = await Ayrim(k);
        Assert.Equal(300m, a.Kart);                 // 500 − 200
        Assert.Equal(250_300m, a.ToplamBakiye);     // 250.000 + 500 − 200
        Assert.Equal(250_000m, a.Nakit);            // çekmecedeki para değişmedi
    }

    [Fact]   // PARA-DEMO tur 2: Skip kaldırıldı (GunKapat servis içi eksi kontrolü)
    public async Task PDS_eksi_sayilan_nakit_servis_yolunda_da_reddedilmeli()
    {
        // DTO [Range(0,…)] yalnız controller yolunda çalışır; servis içi kontrol kuralı.
        var k = YeniKiraci("PDS eksi sayım");
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = -1m, Aciklama = "eksi sayım denemesi" }, k.Islem));
    }

    // ── KURUŞ: PAKET GOC-A (K9a) — sayılan nakitte kuruş altı hane YUVARLANMAZ ────
    // Eski sözleşme (250.000,005 → 0,01 fark + gerekçe / 250.000,004 → fark 0 kapanır) kalktı: kuruş altı
    // hane 400 alan:sayilanNakit + sözlük metni, kapanış YAZILMAZ. Fark/gerekçe kuralı tam kuruşla aynen.
    [Fact]
    public async Task PDS_kurus_0_005_yuvarlanmaz_400_tam_kurus_0_01_fark_gerekce_ister()
    {
        var k = YeniKiraci("PDS kuruş 005");
        await SatisYap(k, "KART", 500m);
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = 250_000.005m }, k.Islem));
        Assert.Contains("Virgülden sonra en çok 2 hane", hata.Message);
        Assert.Equal("sayilanNakit", Kuyumcu.Api.Common.AlanIsareti.Alan(hata));
        // SON-KALANLAR: ileti sade (₺ + farkın yönü) ve alan:aciklama — kural aynı (tam kuruşla).
        var fark = await Assert.ThrowsAsync<BadRequestException>(() => Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = 250_000.01m }, k.Islem));
        Assert.Contains("0,01 ₺ fazla", fark.Message);
        Assert.Equal("aciklama", Kuyumcu.Api.Common.AlanIsareti.Alan(fark));
    }

    [Fact]
    public async Task PDS_kurus_0_004_yuvarlanmaz_400_kapanis_YOK_tam_sayim_fark_sifir_kaydedilir()
    {
        var k = YeniKiraci("PDS kuruş 004");
        await SatisYap(k, "HAVALE", 333.33m);
        using (var db = vt.Olustur(k))
        {
            var hata = await Assert.ThrowsAsync<BadRequestException>(() => Kasa(db).GunKapat(k.KasaId,
                new MKasa.GunKapanisiDto { SayilanNakit = 250_000.004m }, k.Islem));
            Assert.Equal("sayilanNakit", Kuyumcu.Api.Common.AlanIsareti.Alan(hata));
        }
        using (var bos = vt.OlusturSistem())
            Assert.False(bos.GunKapanislari.AsNoTracking().Any(x => x.TenantId == k.TenantId));
        using (var db = vt.Olustur(k))
            await Kasa(db).GunKapat(k.KasaId, new MKasa.GunKapanisiDto { SayilanNakit = 250_000m }, k.Islem);
        using var kontrol = vt.OlusturSistem();
        var g = kontrol.GunKapanislari.AsNoTracking().Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0m, g.Fark);
        Assert.Equal(250_000m, g.DefterBakiye);
    }

    // ── HAS 4 HANE: 0,0001 has tahsilatı TL borcunu değiştirmez ──────────────
    [Fact]
    public async Task PDS_has_0_0001_tahsilat_TL_ayni_has_eksi_0_0001_etiket_yok()
    {
        var k = YeniKiraci("PDS has 4 hane");
        await SatisYap(k, "VERESIYE", 1_000m);
        using (var db = vt.Olustur(k))
            await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
            {
                HasGrami = 0.0001m, OdemeTuru = "NAKIT", KurSnapshotId = k.KurId,
            }, k.Islem);
        using var kontrol = vt.OlusturSistem();
        var b = kontrol.CariBakiyeler.AsNoTracking().Single(x => x.CariId == k.CariId);
        Assert.Equal(1_000m, b.TlBakiye);
        Assert.Equal(-0.0001m, b.HasGramBakiye);
        Assert.Null(kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT").OdemeTuru);
        Assert.Equal(250_000m, kontrol.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye);
    }

    // ── ÇOK BÜYÜK TUTAR ──────────────────────────────────────────────────────
    [Fact]
    public async Task PDS_buyuk_999_999_999_99_kart_nakde_karismaz()
    {
        var k = YeniKiraci("PDS büyük");
        await ElleHareket(k, "GIRIS", 999_999_999.99m, "KART");
        var a = await Ayrim(k);
        Assert.Equal(999_999_999.99m, a.Kart);
        Assert.Equal(1_000_249_999.99m, a.ToplamBakiye);
        Assert.Equal(250_000m, a.Nakit);
    }

    // ── BOŞ / NULL ───────────────────────────────────────────────────────────
    [Fact]
    public async Task PDS_bos_olmayan_kasa_onizlemesi_404()
    {
        var k = YeniKiraci("PDS boş kasa");
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<NotFoundException>(() => Kasa(db).GunKapanisiOnizleme("YOK-BOYLE-KASA", k.Islem));
    }

    // ── HASIM: komisyonlu elle KART girişi (bakiye NET, hareket BRÜT yazılır) ──
    // HareketGir: kasa.Bakiye += Tutar × (1 − komisyon), KasaHareket.Tutar = Tutar (brüt).
    // nakit = bakiye − Σkart(brüt) ⇒ çekmece değişmediği hâlde nakit defteri komisyon kadar düşer.
    [Fact]   // PARA-DEMO tur 2: Skip kaldırıldı (HareketGir hareketi NET yazar)
    public async Task PDS_hasim_komisyonlu_elle_kart_girisi_nakdi_degistirmez()
    {
        var k = YeniKiraci("PDS komisyon");
        await ElleHareket(k, "GIRIS", 1_000m, "KART", komisyon: 0.02m);
        var a = await Ayrim(k);
        Assert.Equal(250_980m, a.ToplamBakiye);   // 250.000 + 1.000 × 0,98
        Assert.Equal(250_000m, a.Nakit);          // çekmeceye nakit girmedi
    }
}

[Collection("http")]
public sealed class PARADEMOSinirHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task PDS_HTTP_eksi_sayilan_nakit_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PDS http eksi");
        var c = f.Ortam.Istemci(k);
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            new { sayilanNakit = -1m, aciklama = "eksi sayım denemesi" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}

public sealed class PARADEMOSinirGirisTestleri
{
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("250.000,01", "250000.01")]
    [InlineData("999.999.999,99", "999999999.99")]
    [InlineData("0,0001", "0.0001")]
    public void PDS_Turkce_giris_SayiCoz(string metin, string beklenen)
    {
        var c = FormAlani.SayiCoz(metin);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Fact]
    public void PDS_belirsiz_1_500_reddedilir()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
    }

    [Fact]
    public void PDS_bos_giris_null()
    {
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Null(FormAlani.SayiCoz(null).Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO tur 3 — HASIM DOĞRULAYICI EK SINIR TESTLERİ (tur 2 değişiklikleri:
// HareketGir NET hareket, virman kasaAraci). Beklenen değerler SABİT, elle hesaplı.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class PARADEMOSinirTur3Testleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<(decimal Nakit, decimal Kart, decimal Havale, decimal Toplam)> On(HttpClient c, string kasaId)
    {
        var r = await c.GetAsync($"/api/v1/kasalar/{kasaId}/gun-kapanisi/onizleme");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        return (j.GetProperty("nakitDefter").GetDecimal(), j.GetProperty("kartToplam").GetDecimal(),
                j.GetProperty("havaleToplam").GetDecimal(), j.GetProperty("toplamBakiye").GetDecimal());
    }

    private async Task<decimal> HareketToplami(Kiraci k, string kasaId)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.KasaId == kasaId).SumAsync(h => h.Tutar);
    }

    private async Task<string> Banka(Kiraci k)
    {
        var r = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Sınır Bankası", hesapAdi = "PDS3 hesap",
            iban = TestIban.Uret(),
            paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Json()).GetProperty("id").GetString()!;
    }

    // KURUŞ: komisyon 0,005 → 1 × 0,995 = 0,995 → AwayFromZero 1,00; hareket = bakiye farkı.
    [Fact]
    public async Task PDS3_kurus_komisyon_0_995_AwayFromZero_hareket_bakiye_ayni()
    {
        var k = YeniKiraci("PDS3 kuruş komisyon");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket",
            new { yon = "GIRIS", tutar = 1m, odemeTuru = "KART", komisyonOran = 0.005m });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var o = await On(c, k.KasaId);
        Assert.Equal(250_001.00m, o.Toplam);
        Assert.Equal(1.00m, o.Kart);
        Assert.Equal(250_000m, o.Nakit);
        Assert.Equal(1.00m, await HareketToplami(k, k.KasaId));
    }

    // KURUŞ — PAKET GOC-A (K9): kuruş altı virman tutarı (500,005 / 500,004) YUVARLANMAZ → 400.
    // Kart payı sınırı tam kuruşla: 500,01 > kart payı 500,00 → 400; 500,00 geçer.
    [Fact]
    public async Task PDS3_virman_kart_payi_kurus_alti_400_tam_kurus_siniri()
    {
        var k = YeniKiraci("PDS3 virman kuruş");
        var c = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket",
            new { yon = "GIRIS", tutar = 500m, odemeTuru = "KART" })).StatusCode);
        var b = await Banka(k);
        var asan = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = b, tutar = 500.005m, kasaAraci = "KART" });
        Assert.Equal(HttpStatusCode.BadRequest, asan.StatusCode);
        Assert.Contains("Virgülden sonra en çok 2 hane", await asan.Content.ReadAsStringAsync());
        var alti = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = b, tutar = 500.004m, kasaAraci = "KART" });
        Assert.Equal(HttpStatusCode.BadRequest, alti.StatusCode);
        var payiAsan = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = b, tutar = 500.01m, kasaAraci = "KART" });
        Assert.Equal(HttpStatusCode.BadRequest, payiAsan.StatusCode);
        var tam = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = b, tutar = 500.00m, kasaAraci = "KART" });
        Assert.Equal(HttpStatusCode.OK, tam.StatusCode);
        var o = await On(c, k.KasaId);
        Assert.Equal(0m, o.Kart);
        Assert.Equal(250_000m, o.Nakit);
        Assert.Equal(250_000m, o.Toplam);
    }

    // BÜYÜK: 999.999.999,99 kart → tamamı bankaya KART virmanı → kart 0, nakit 250.000.
    [Fact]
    public async Task PDS3_buyuk_tutar_kart_virmani_nakde_dokunmaz()
    {
        var k = YeniKiraci("PDS3 büyük virman");
        var c = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket",
            new { yon = "GIRIS", tutar = 999_999_999.99m, odemeTuru = "KART" })).StatusCode);
        var b = await Banka(k);
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = b, tutar = 999_999_999.99m, kasaAraci = "KART" });
        Assert.Equal(HttpStatusCode.OK, v.StatusCode);
        var o = await On(c, k.KasaId);
        Assert.Equal(0m, o.Kart);
        Assert.Equal(250_000m, o.Nakit);
    }

    // KASA→KASA: kart payı hedefte de KART kalır; hedefin nakit defteri değişmez.
    [Fact]
    public async Task PDS3_kasadan_kasaya_kart_virmani_hedef_nakdi_degismez()
    {
        var k = YeniKiraci("PDS3 kasa kasa");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var y = await c.PostJson("/api/v1/kasalar", new { ad = "PDS3 ikinci", subeId = k.SubeId, paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var ikinci = (await y.Json()).GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket",
            new { yon = "GIRIS", tutar = 700m, odemeTuru = "HAVALE" })).StatusCode);
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "KASA", hedefId = ikinci, tutar = 700m, kasaAraci = "HAVALE" });
        Assert.Equal(HttpStatusCode.OK, v.StatusCode);
        var kaynak = await On(c, k.KasaId);
        var hedef = await On(c, ikinci);
        Assert.Equal(0m, kaynak.Havale);
        Assert.Equal(250_000m, kaynak.Nakit);
        Assert.Equal(700m, hedef.Havale);
        Assert.Equal(0m, hedef.Nakit);
        Assert.Equal(700m, hedef.Toplam);
    }

    // SIFIR / BOŞ: kart payı 0 iken KART virmanı (0,01) reddedilir; boş araç = NAKIT.
    [Fact]
    public async Task PDS3_sifir_kart_payinda_kart_virmani_400_bos_arac_nakit()
    {
        var k = YeniKiraci("PDS3 sıfır pay");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var b = await Banka(k);
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = b, tutar = 0.01m, kasaAraci = "KART" });
        Assert.Equal(HttpStatusCode.BadRequest, v.StatusCode);
        var bos = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = b, tutar = 0.01m, kasaAraci = "" });
        Assert.Equal(HttpStatusCode.OK, bos.StatusCode);
        Assert.Equal(249_999.99m, (await On(c, k.KasaId)).Nakit);
    }

    // EKSİ / geçersiz araç: ALTIN, küçük harf 'kart' (controller desen kapısı) → 400, yazım yok.
    [Theory]
    [InlineData("ALTIN")]
    [InlineData("DOVIZ")]
    [InlineData("kart")]
    public async Task PDS3_gecersiz_arac_400_hicbir_sey_yazilmaz(string arac)
    {
        var k = YeniKiraci("PDS3 geçersiz " + arac);
        var c = f.Ortam.Istemci(k, "SAHIP");
        var b = await Banka(k);
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = b, tutar = 1m, kasaAraci = arac });
        Assert.Equal(HttpStatusCode.BadRequest, v.StatusCode);
        Assert.Equal(0m, await HareketToplami(k, k.KasaId));
    }

    // EKSİ: virmanda eksi tutar reddedilir (Range kapısı), kasa değişmez.
    [Fact]
    public async Task PDS3_eksi_virman_tutari_400()
    {
        var k = YeniKiraci("PDS3 eksi virman");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var b = await Banka(k);
        var v = await c.PostAsJsonAsync("/api/v1/virman", new
        { kaynakTur = "KASA", kaynakId = k.KasaId, hedefTur = "BANKA", hedefId = b, tutar = -5m, kasaAraci = "KART" });
        Assert.Equal(HttpStatusCode.BadRequest, v.StatusCode);
        Assert.Equal(250_000m, (await On(c, k.KasaId)).Toplam);
    }
}
