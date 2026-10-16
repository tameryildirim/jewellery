using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MSiparis = Kuyumcu.Api.Modules.Siparis;

namespace Kuyumcu.Tests.Dogrulayici;

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET EKSIK-B1-YAZDIR finansal sınır testleri.
// Kapsam: POST /siparisler/{id}/kapora (B1-5) — para kasaya ve cariye girer.
// Beklenen değerler SABİT ve elle hesaplandı (üretim kodundan türetilmez).
// Yuvarlama kuralı: CariServisi.TahsilatGirHareket TL 2 hane, MidpointRounding.AwayFromZero.
// ═══════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class EKSIKB1YAZDIRSinirTestleri(TestVeritabani vt)
{
    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MSiparis.SiparisServisi Siparis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSiparis.SiparisServisi>.Instance, Cari(db));

    private static JsonElement J(object yanit) =>
        JsonSerializer.SerializeToElement(yanit, yanit.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private Kiraci Kur(string ad)
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETEKSIK-B1-YAZDIR-TEST dg " + ad);
        sistem.SaveChanges();
        return k;
    }

    private static async Task<string> SiparisAc(KuyumcuDbContext db, Kiraci k)
    {
        var y = J(await Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
        {
            Tur = SiparisTuru.MusteriSiparisi, CariId = k.CariId, SubeId = k.SubeId, Aciklama = "sinir",
            Kalemler = [new MSiparis.SiparisKalemDto { Tanim = "bilezik", Adet = 1m }],
        }, k.Islem));
        return y.GetProperty("id").GetString()!;
    }

    private static MSiparis.SiparisKaporaDto Govde(Kiraci k, decimal? tutar, string odeme = "HAVALE") => new()
    {
        TlTutari = tutar, OdemeTuru = odeme, KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    private async Task DefterBos(Kiraci k, string id)
    {
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
        Assert.Null((await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).KaporaHareketId);
    }

    // ── sıfır · eksi · null → 400, defter boş, sipariş bağı yok ──────────────
    [Theory]
    [InlineData("0", null)]
    [InlineData("-0.01", null)]
    [InlineData("null", null)]
    public async Task Sinir_sifir_eksi_null_kapora_reddedilir(string ham, string? _)
    {
        decimal? tutar = ham == "null" ? null : decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        var k = Kur("Z" + ham);
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        await Assert.ThrowsAsync<BadRequestException>(() => Siparis(db).Kapora(id, Govde(k, tutar), k.Islem));
        await DefterBos(k, id);
    }

    // ── kuruş yuvarlaması: 100,005 → 100,01 (AwayFromZero); 100,004 → 100,00 ──
    [Theory]
    [InlineData("100.005", "100.01")]
    [InlineData("100.004", "100.00")]
    public async Task Sinir_kurus_yuvarlamasi_kasa_ve_cari_ayni(string girdi, string beklenen)
    {
        var tutar = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        var hedef = decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture);
        var k = Kur("K" + girdi);
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var kasaOnce = TestKurulum.KasaBakiye(db, k.KasaId);
        await Siparis(db).Kapora(id, Govde(k, tutar), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(kasaOnce + hedef, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(-hedef, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
        var d = J(await Siparis(kontrol).Detay(id, k.Islem));
        Assert.Equal(hedef, d.GetProperty("kaporaTutari").GetDecimal());   // kart defterle aynı kuruşu gösterir
    }

    // ── 0,004 TL: kapora kapısından (>0) geçer ama yuvarlanınca 0 → 400, defter boş ──
    [Fact]
    public async Task Sinir_yuvarlaninca_sifir_olan_kapora_reddedilir()
    {
        var k = Kur("Y0004");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        await Assert.ThrowsAsync<BadRequestException>(() => Siparis(db).Kapora(id, Govde(k, 0.004m), k.Islem));
        await DefterBos(k, id);
    }

    // ── çok büyük tutar: 999.999.999,99 ya kuruşu kuruşuna yazılır ya da 400 (500 DEĞİL) ──
    [Fact]
    public async Task Sinir_cok_buyuk_kapora_kurusu_kaybolmaz_ya_da_400()
    {
        var k = Kur("BUYUK");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var kasaOnce = TestKurulum.KasaBakiye(db, k.KasaId);
        try
        {
            await Siparis(db).Kapora(id, Govde(k, 999_999_999.99m), k.Islem);
        }
        catch (ApiException ex) when (ex is BadRequestException or ConflictException)
        {
            await DefterBos(k, id);   // reddedildiyse YARIM kayıt yok
            return;
        }
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(kasaOnce + 999_999_999.99m, TestKurulum.KasaBakiye(kontrol, k.KasaId));
        Assert.Equal(-999_999_999.99m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
        Assert.Equal(999_999_999.99m, J(await Siparis(kontrol).Detay(id, k.Islem)).GetProperty("kaporaTutari").GetDecimal());
    }

    // ── Türkçe giriş (kapora formu Para bileşeni FormAlani.SayiCoz kullanır) ──
    [Theory]
    [InlineData("45.000", 45000.0)]
    [InlineData("1,5", 1.5)]
    [InlineData("999.999.999,99", 999999999.99)]
    public void Sinir_turkce_giris_cozulur(string ham, double beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.True(c.Tamam);
        Assert.Equal((decimal)beklenen, c.Deger);
    }

    [Fact]
    public void Sinir_turkce_giris_belirsiz_1500_reddedilir()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
    }

    // ── DİL: kapora kilidinin İngilizce karşılığı (409 iletisi + ekrandaki kilit sebebi) ──
    // BULGU (doğrulayıcı, 09.10): paket iletiyi değiştirdi, SunucuIletiSozlugu.Veri.cs'deki eski
    // karşılık (satır 183) artık eşleşmiyor; "Teslim edilmiş…" iletisinin karşılığı hiç yok.
    // Uygulayıcı sözlüğe TR/EN çiftini ekleyince Skip kaldırılır.
    [Fact] // Tur 2: TR/EN çiftleri SunucuIletiSozlugu.Veri.cs'de — Skip kaldırıldı.
    public void Dil_kapora_kilidi_iletileri_ingilizceye_cevrilir()
    {
        var canli = new MSiparis.SiparisServisi.KaporaBilgisi(1m, DateTime.UtcNow, "k", Stornolu: false);
        foreach (var tr in new[]
                 {
                     MSiparis.SiparisServisi.KaporaKilidi(SiparisDurumu.Iptal, null)!,
                     MSiparis.SiparisServisi.KaporaKilidi(SiparisDurumu.TeslimEdildi, null)!,
                     MSiparis.SiparisServisi.KaporaKilidi(SiparisDurumu.Onaylandi, canli)!,
                 })
            Assert.NotEqual(tr, Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(tr, "en"));
    }

    // ── TUR 3 (doğrulayıcı): EŞZAMANLI çift kapora — iki istek aynı anda gelirse
    // defterde TEK tahsilat olmalı (iki cihaz/sekme). Beklenen SABİT: 1 satır.
    // BULGU KRİTİK (doğrulayıcı tur 3, 09.10): iki eşzamanlı istek → 2 TAHSILAT, kasa +2×tutar,
    // biri siparişe bağsız (canlı 5166 SQL ile de görüldü). Uygulayıcı düzeltince Skip kaldırılır.
    [Fact]   // PAKET KAPORA-ESZAMAN: düzeltildi (sipariş satır kilidi + tek işlem) — Skip kaldırıldı
    public async Task Sinir_eszamanli_cift_kapora_tek_tahsilat()
    {
        var k = Kur("ESZ");
        string id;
        using (var db0 = vt.OlusturSistem()) id = await SiparisAc(db0, k);
        using var db1 = vt.OlusturSistem();
        using var db2 = vt.OlusturSistem();
        var t1 = Task.Run(() => Siparis(db1).Kapora(id, Govde(k, 1_000m), k.Islem));
        var t2 = Task.Run(() => Siparis(db2).Kapora(id, Govde(k, 1_000m), k.Islem));
        try { await Task.WhenAll(t1, t2); } catch (ConflictException) { }
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
    }

    // ── TUR 3 (doğrulayıcı): "Geri" dönüş yolu — tarayıcı URL ayrıştırıcısı sekme/satır
    // sonu karakterlerini SİLER ("/\t/x" → "//x" = dış site). Bunlar reddedilmeli.
    // BULGU ORTA (doğrulayıcı tur 3): gerçek Chrome'da "/%09/127.0.0.1" → Geri → kökenden çıkış (chrome-error). Düzeltilince Skip kaldırılır.
    [Theory]   // PAKET KAPORA-ESZAMAN: düzeltildi (Core GuvenliYol) — Skip kaldırıldı
    [InlineData("/\t/kotu.example.com")]
    [InlineData("/\n/kotu.example.com")]
    [InlineData("/\r/kotu.example.com")]
    public void Guvenlik_donus_kontrol_karakteri_dis_siteye_cikamaz(string kacisli)
    {
        var girdi = System.Text.RegularExpressions.Regex.Unescape(kacisli);
        Assert.Null(Kuyumcu.Web.Pages.Cikti.GuvenliDonus(girdi));
    }
}
