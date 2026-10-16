using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SON-KALANLAR — PATCH-5 hasım/hakem kalanları.
// Yeni sınır testleri (HTTP + servis + saf) ve gider akordeon yardımcısı.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// SON-KALANLAR — /gider'de geçmiş kayıtlar KAPALI akordeondur (kurucu kuralı "geçmiş kayıtlar katlanır").
/// Liste içeriğini sınayan ekran testleri önce akordeonu açar (kullanıcının yaptığı gibi başlığa dokunarak).
/// O sekmede çizili hangi akordeon varsa (giderler / para aktarımları) kapalıysa açılır.
/// </summary>
public static class GiderAkordeon
{
    public static void Ac<T>(IRenderedComponent<T> e) where T : Microsoft.AspNetCore.Components.IComponent
    {
        e.WaitForState(() => e.FindAll("#gd-gecmis-ac, #gd-virman-gecmis-ac").Count > 0, TimeSpan.FromSeconds(10));
        foreach (var id in new[] { "#gd-gecmis-ac", "#gd-virman-gecmis-ac" })
            if (e.FindAll(id) is { Count: > 0 } d && d[0].GetAttribute("aria-expanded") == "false")
                d[0].Click();
    }
}

// ═══════════ HTTP + servis + SQL ═══════════
[Collection("http")]
public sealed class SonKalanlarHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"SONKALAN {ad}");
    }

    private AsyncServiceScope Kapsam(Kiraci k)
    {
        var s = f.Ortam.Services.CreateAsyncScope();
        s.ServiceProvider.GetRequiredService<KapsamBaglamAyari>()
            .Ayarla(new SabitKiraciBaglami(k.TenantId, k.SubeId, tumSubeler: true));
        return s;
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    /// <summary>Kodu ve alanı sınar; gövde AYRIŞTIRILIR (Türkçe kaçışlar). Dönen: alanın ilk iletisi.</summary>
    private static async Task<string> Red(HttpResponseMessage r, HttpStatusCode kod, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == kod, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.True(j.TryGetProperty("alanlar", out var alanlar), "gövdede 'alanlar' yok: " + govde);
        var p = alanlar.EnumerateObject().FirstOrDefault(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        Assert.True(p.Value.ValueKind == JsonValueKind.Array, $"'{alan}' alanına işaretli değil: {govde}");
        return p.Value.EnumerateArray().First().GetString() ?? "";
    }

    private static async Task Tamam(HttpResponseMessage r)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {govde}");
    }

    private (string ParaBirimi, decimal Bakiye, int Hareket) Banka(Kiraci k, string id)
    {
        using var db = f.Vt.OlusturSistem();
        var h = db.BankaHesaplari.AsNoTracking().Single(x => x.Id == id);
        return (h.ParaBirimi, h.Bakiye, db.BankaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId && x.BankaHesapId == id));
    }

    private async Task<string> BankaAc(Kiraci k, HttpClient c, string paraBirimi)
    {
        var iban = TestIban.Tamamla(string.Concat(Enumerable.Range(0, 22).Select(i => (char)('0' + (k.TenantId[(i * 7) % k.TenantId.Length] % 10)))));
        await Tamam(await c.PostJson("/api/v1/bankalar", new { bankaAdi = "SK Banka", hesapAdi = "SK Vadesiz", iban, paraBirimi }));
        using var db = f.Vt.OlusturSistem();
        return db.BankaHesaplari.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;
    }

    // ── D-P5-1: banka hesabının para birimi (Kasa GecmisiBosMu eşi) ──

    [Fact]
    public async Task SK01_banka_BAKIYELI_hesapta_para_birimi_DEGISMEZ_409_alan_paraBirimi_SQL_USD_100()
    {
        var k = Kur("banka pb bakiye");
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaAc(k, c, "USD");
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{hesap}/hareket", """{"yon":"GELEN","tutar":100}"""));
        Assert.Equal(("USD", 100m, 1), Banka(k, hesap));
        var m = await Red(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{hesap}", """{"paraBirimi":"TRY"}"""),
            HttpStatusCode.Conflict, "paraBirimi");
        Assert.StartsWith("Bakiyesi sıfır olmayan banka hesabının para birimi değiştirilemez", m);
        Assert.Equal(("USD", 100m, 1), Banka(k, hesap));   // 100 USD sessizce 100 TL OLMAZ
    }

    [Fact]
    public async Task SK02_banka_bakiyesi_SIFIRA_donmus_ama_HAREKETLI_hesapta_da_DEGISMEZ_409()
    {
        var k = Kur("banka pb hareket");
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaAc(k, c, "USD");
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{hesap}/hareket", """{"yon":"GELEN","tutar":100}"""));
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{hesap}/hareket", """{"yon":"GIDEN","tutar":100}"""));
        Assert.Equal(("USD", 0m, 2), Banka(k, hesap));
        var m = await Red(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{hesap}", """{"paraBirimi":"EUR"}"""),
            HttpStatusCode.Conflict, "paraBirimi");
        Assert.Contains("Hareket görmüş", m);
        Assert.Equal(("USD", 0m, 2), Banka(k, hesap));
    }

    [Fact]
    public async Task SK03_banka_GECMISI_BOS_hesapta_para_birimi_degisir_200_ayni_deger_PATCH_kilide_takilmaz()
    {
        var k = Kur("banka pb bos");
        var c = f.Ortam.Istemci(k);
        var hesap = await BankaAc(k, c, "USD");
        await Tamam(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{hesap}", """{"paraBirimi":"EUR"}"""));
        Assert.Equal(("EUR", 0m, 0), Banka(k, hesap));
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{hesap}/hareket", """{"yon":"GELEN","tutar":5}"""));
        // Aynı para birimini göndermek değişiklik değildir (ekran tüm alanları gönderebilir) → 200.
        await Tamam(await Ham(c, HttpMethod.Patch, $"/api/v1/bankalar/{hesap}", """{"paraBirimi":"EUR","hesapAdi":"SK Vadesiz 2"}"""));
        Assert.Equal(("EUR", 5m, 1), Banka(k, hesap));
    }

    [Fact]
    public async Task SK04_kasa_GECMISLI_para_birimi_409_artik_ALANLI_paraBirimi_SQL_DEGISMEZ()
    {
        var k = Kur("kasa pb");
        var m = await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/kasalar/{k.KasaId}", """{"paraBirimi":"USD"}"""),
            HttpStatusCode.Conflict, "paraBirimi");
        Assert.Contains("para birimi", m);
        using var db = f.Vt.OlusturSistem();
        var kasa = db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId);
        Assert.Equal(("TRY", TestKurulum.KasaAcilis), (kasa.ParaBirimi, kasa.Bakiye));
    }

    // ── Gün Kapat: gerekçe alanı · yarım kuruş eksi · tek tavan cümlesi · Strict ileti ──

    [Fact]
    public async Task SK05_gun_kapat_FARK_gerekcesiz_400_alan_aciklama_ileti_sade_yonlu_kapanis_YOK()
    {
        var k = Kur("gun gerekce");
        var m = await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", """{"sayilanNakit":0}"""),
            HttpStatusCode.BadRequest, "aciklama");
        Assert.Contains("250.000,00 ₺ eksik", m);
        Assert.DoesNotContain(" TL", m);
        Assert.DoesNotContain("ZORUNLU", m);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    [Theory]
    [InlineData("-0.004")]
    [InlineData("-0.0049")]
    [InlineData("-0.01")]
    public async Task SK06_gun_kapat_SERVIS_yolunda_yarim_kurusluk_EKSI_sayim_SIFIR_sayilmaz_400_alan_sayilanNakit(string ham)
    {
        var k = Kur("gun eksi " + ham);
        await using var kapsam = Kapsam(k);
        var servis = kapsam.ServiceProvider.GetRequiredService<Kuyumcu.Api.Modules.Kasa.KasaServisi>();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => servis.GunKapat(k.KasaId,
            new Kuyumcu.Api.Modules.Kasa.GunKapanisiDto
            {
                SayilanNakit = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture),
                Aciklama = "eksi sayım denemesi — fark gerekçesi yazıldı",
            }, k.Islem));
        Assert.Equal("sayilanNakit", AlanIsareti.Alan(ex));
        // PAKET GOC-A: ileti sözlükten (K9a — ekranın alan altı metniyle AYNI); -0,004/-0,0049 ondalıktan ÖNCE eksi.
        Assert.Equal(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiEksi), "tr"), ex.Message);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task SK07_tavan_kurali_TEK_cumle_gun_kapat_ile_kasa_hareketi_ayni_kalip()
    {
        var k = Kur("tek tavan");
        var c = f.Ortam.Istemci(k);
        var gun = await Red(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":1e14,"aciklama":"tavan denemesi — kolon sınırı"}"""), HttpStatusCode.BadRequest, "sayilanNakit");
        var hareket = await Red(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"GIRIS","tutar":1e14,"odemeTuru":"NAKIT"}"""), HttpStatusCode.BadRequest, "tutar");
        // PAKET GOC-A: TEK cümle artık sözlüğün K9 tavan iletisi — iki uçta BİREBİR aynı ve ekranın alan altı metni.
        var kalip = Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiTavan, Kuyumcu.Core.Domain.Girdi.TutarKurali.Tavan), "tr");
        Assert.Equal(kalip, gun);
        Assert.Equal(kalip, hareket);
        Assert.DoesNotContain("arasında olmalıdır", gun);
        // HTTP yolunda eksi sayım da aynı Core kuralından (tek ileti) gelir.
        var eksi = await Red(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":-0.01,"aciklama":"eksi denemesi — fark gerekçesi"}"""), HttpStatusCode.BadRequest, "sayilanNakit");
        Assert.Equal(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiEksi), "tr"), eksi);
    }

    [Fact]
    public async Task SK08_Strict_sayi_iletisi_Turkce_yazimli_teknik_ayrinti_YOK()
    {
        var k = Kur("strict");
        var m = await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            """{"sayilanNakit":"1000"}"""), HttpStatusCode.BadRequest, "sayilanNakit");
        Assert.StartsWith("Sayılan nakit geçerli bir sayı değil", m);
        Assert.Contains("ör. 1500,50", m);
        Assert.DoesNotContain("Sayilan", m);
        Assert.DoesNotContain("tırnak", m);
        Assert.DoesNotContain("ondalık ayırıcı", m);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.GunKapanislari.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }

    // ── Kapanmış güne yazma iletisi + kasa kartı "bugün kapandı" bilgisi ──

    [Fact]
    public async Task SK09_kapanmis_gune_hareket_iletisi_SADE_buyuk_harf_ve_rol_kodu_YOK_TL_simgesi_ve_nakit_ayriminda_BugunKapali()
    {
        var k = Kur("kapali gun");
        var c = f.Ortam.Istemci(k);
        var onceAyrim = JsonDocument.Parse(await c.GetStringAsync("/api/v1/kasalar/nakit-ayrimi")).RootElement;
        Assert.False(onceAyrim.EnumerateArray().Single(x => x.GetProperty("kasaId").GetString() == k.KasaId).GetProperty("bugunKapali").GetBoolean());
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", """{"sayilanNakit":250000}"""));

        int once;
        using (var db = f.Vt.OlusturSistem()) once = db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId);
        var r = await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket", """{"yon":"GIRIS","tutar":10,"odemeTuru":"NAKIT"}""");
        var govde = await r.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var m = JsonDocument.Parse(govde).RootElement.GetProperty("message").GetString()!;
        Assert.Contains("günü kapandı", m);
        Assert.Contains("250.000,00 ₺", m);
        Assert.Contains("işletme sahibine başvurun", m);
        Assert.DoesNotContain("KAPATILMIŞ", m);
        Assert.DoesNotContain("SAHİP", m);
        Assert.DoesNotContain(" TL", m);
        using (var db = f.Vt.OlusturSistem()) Assert.Equal(once, db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId));

        var ayrim = JsonDocument.Parse(await c.GetStringAsync("/api/v1/kasalar/nakit-ayrimi")).RootElement;
        Assert.True(ayrim.EnumerateArray().Single(x => x.GetProperty("kasaId").GetString() == k.KasaId).GetProperty("bugunKapali").GetBoolean());
    }

    // ── "≤ 0" iletisi kullanıcının yazdığıyla çelişmez ──

    // PAKET GOC-A: 0,004 üç ondalıklıdır → K9 ondalık iletisi (yuvarlanıp "en az 0,01"e düşmez); literal metin.
    [Fact]
    public async Task SK10_kasa_hareketi_0_004_iletisi_ONDALIK_sifirdan_buyuk_DEMEZ_satir_YOK()
    {
        var k = Kur("enaz kasa");
        var m = await Red(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"GIRIS","tutar":0.004,"odemeTuru":"NAKIT"}"""), HttpStatusCode.BadRequest, "tutar");
        // Tur 2 (doğrulayıcı DÜŞÜK): beklenen metin LİTERAL — üretim sabitinden okunmaz (kendini doğrulamaz).
        Assert.Equal("Virgülden sonra en çok 2 hane yazılır.", m);
        Assert.DoesNotContain("sıfırdan büyük", m);
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId && x.Tutar == 0m));
    }

    [Fact]
    public async Task SK11_tahsilat_0_001_iletisi_EN_AZ_0_01_bos_tahsilat_sade_has_gram_YOK_hareket_YAZILMAZ()
    {
        var k = Kur("enaz tahsilat");
        var c = f.Ortam.Istemci(k);
        int once;
        using (var db = f.Vt.OlusturSistem()) once = db.Hareketler.AsNoTracking().Count(x => x.TenantId == k.TenantId);
        var kucuk = await Red(await Ham(c, HttpMethod.Post, $"/api/v1/cari/{k.CariId}/tahsilat",
            $$"""{"tlTutari":0.001,"odemeTuru":"HAVALE","kasaId":"{{k.KasaId}}","kurSnapshotId":"{{k.KurId}}"}"""), HttpStatusCode.BadRequest, "tlTutari");
        Assert.Equal("Tutar en az 0,01 olmalıdır — daha küçük tutar kaydedilemez", kucuk);   // tur 2: literal
        var bos = await Red(await Ham(c, HttpMethod.Post, $"/api/v1/cari/{k.CariId}/tahsilat",
            $$"""{"kurSnapshotId":"{{k.KurId}}"}"""), HttpStatusCode.BadRequest, "tlTutari");
        Assert.Equal("Tutar ya da altın girin.", bos);
        foreach (var m in new[] { kucuk, bos })
        {
            Assert.DoesNotContain("has gram", m, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("en az bir tutar", m);
        }
        using (var db = f.Vt.OlusturSistem()) Assert.Equal(once, db.Hareketler.AsNoTracking().Count(x => x.TenantId == k.TenantId));
    }
    // ── Tur 2 (doğrulayıcı DÜŞÜK): 0,005 için TEK KURAL — HTTP yolu = servis yolu = tahsilat ──
    [Fact]
    public async Task SK12_yarim_kurus_TEK_KURAL_HTTP_kasa_banka_yuvarlamaz_400_tahsilat_eski_kural()
    {
        var k = Kur("tek kural 0005");
        var c = f.Ortam.Istemci(k);
        // PAKET GOC-A (K9): kasa da banka gibi 0,005'i YUVARLAMAZ (eski: 0,01 yazılırdı) — 400 alan:tutar, satır YOK.
        var ondalik = Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiOndalik, 2), "tr");
        Assert.Equal(ondalik, await Red(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
            """{"yon":"GIRIS","tutar":0.005,"odemeTuru":"NAKIT","aciklama":"sk12"}"""), HttpStatusCode.BadRequest, "tutar"));
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0, db.KasaHareketleri.AsNoTracking().Count(x => x.TenantId == k.TenantId && x.Aciklama == "sk12"));
        // Banka — GIRDI-AILESI-2 tur 3 (doğrulayıcı YÜKSEK · K9): banka hareketi 2'den fazla ondalığı
        // YUVARLAMAZ, ekrandaki Para bileşeniyle aynı Core kuralı ve metniyle 400 verir (alan: tutar).
        // Kasa ve tahsilat uçları (eski TEK KURAL: 0,005 → 0,01) K9 göçünde (GOC) aynı kurala iner.
        var hesap = await BankaAc(k, c, "TRY");
        var ondalikMesaji = Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiOndalik, 2), "tr");
        foreach (var t in new[] { "0.005", "0.0049" })
            Assert.Equal(ondalikMesaji, await Red(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{hesap}/hareket",
                "{\"yon\":\"GELEN\",\"tutar\":" + t + "}"), HttpStatusCode.BadRequest, "tutar"));
        Assert.Equal(("TRY", 0m, 0), Banka(k, hesap));
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{hesap}/hareket", """{"yon":"GELEN","tutar":0.01}"""));
        Assert.Equal(("TRY", 0.01m, 1), Banka(k, hesap));
        // Kasa: 0,0049 → K9 ondalık iletisi (literal; GOC-A — yuvarlama YOK)
        Assert.Equal("Virgülden sonra en çok 2 hane yazılır.",
            await Red(await Ham(c, HttpMethod.Post, $"/api/v1/kasalar/{k.KasaId}/hareket",
                """{"yon":"GIRIS","tutar":0.0049,"odemeTuru":"NAKIT"}"""), HttpStatusCode.BadRequest, "tutar"));
        Assert.Equal(("TRY", 0.01m, 1), Banka(k, hesap));
        // Tahsilat (Cari modülü — GOC-A dosya alanı DIŞI, GOC-B'nin) bugün 0,005'i 0,01 kabul ediyor → değişmedi
        int once;
        using (var db = f.Vt.OlusturSistem()) once = db.Hareketler.AsNoTracking().Count(x => x.TenantId == k.TenantId);
        await Tamam(await Ham(c, HttpMethod.Post, $"/api/v1/cari/{k.CariId}/tahsilat",
            $$"""{"tlTutari":0.005,"odemeTuru":"HAVALE","kasaId":"{{k.KasaId}}","kurSnapshotId":"{{k.KurId}}"}"""));
        using (var db = f.Vt.OlusturSistem()) Assert.True(db.Hareketler.AsNoTracking().Count(x => x.TenantId == k.TenantId) > once);
    }
}

// ═══════════ Saf ═══════════
public sealed class SonKalanlarSafTestleri
{
    [Fact]
    public void SK20_PozitifTutar_iletisi_tek_kalip_en_az_0_01()
    {
        var ex = Assert.Throws<BadRequestException>(() => Kuyumcu.Api.Modules.KolonTavani.PozitifTutar(0.004m, "tutar", "Tutar"));
        Assert.Equal("Tutar en az 0,01 olmalıdır — daha küçük tutar kaydedilemez", ex.Message);   // tur 2: literal
        Assert.Equal("tutar", AlanIsareti.Alan(ex));
    }

    [Fact]
    public void SK21_JsonHataMesaji_Turkce_alan_adi_ve_ornek_teknik_ayrinti_YOK()
    {
        var m = Program.JsonHataMesaji("sayilanNakit",
            "The JSON value could not be converted to System.Nullable`1[System.Decimal]. Path: $.sayilanNakit | LineNumber: 0 | BytePositionInLine: 22.", null);
        Assert.Equal("Sayılan nakit geçerli bir sayı değil — sayıyı rakamla yazın, ör. 1500,50", m);
    }

    [Theory]
    [InlineData(185000, "TRY", "185.000,00 ₺")]
    [InlineData(0.5, "USD", "0,50 $")]
    [InlineData(12.3, "EUR", "12,30 €")]
    [InlineData(1, "GBP", "1,00 £")]
    public void SK22_sunucu_iletisi_para_bicimi_tr_TR_ve_simge(decimal tutar, string pb, string beklenen) =>
        Assert.Equal(beklenen, Kuyumcu.Api.Modules.Kasa.GunKapanisiKapisi.Para(tutar, pb));
}

// ═══════════ Ekran (bUnit) — Kasalar ═══════════
public sealed class SonKalanlarKasaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool bugunKapali;
    /// <summary>POST yanıtı (yol sonu → (kod, gövde)); yoksa 200 "{}".</summary>
    private readonly Dictionary<string, (int Kod, string Govde)> postYanit = [];

    public SonKalanlarKasaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(SonKalanlarKasaEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            var get = istek.Method == HttpMethod.Get;
            if (!get && t.postYanit.FirstOrDefault(p => yol.EndsWith(p.Key)) is { Key: not null } py)
                return new HttpResponseMessage((HttpStatusCode)py.Value.Kod)
                { Content = new StringContent(py.Value.Govde, Encoding.UTF8, "application/json") };
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube"}]""",
                var v when get && v.Contains("/kasalar/nakit-ayrimi") =>
                    $$"""[{"kasaId":"k1","paraBirimi":"TRY","toplam":1000,"nakit":1000,"kart":0,"havale":0,"bugunKapali":{{(t.bugunKapali ? "true" : "false")}}}]""",
                var v when get && v.Contains("/gun-kapanisi/onizleme") =>
                    """{"toplamBakiye":1000,"kartToplam":0,"havaleToplam":0,"nakitDefter":1000}""",
                var v when get && v.Contains("/kasalar/") && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when get && v.EndsWith("/kasalar") =>
                    """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1000,"aktif":true,"subeId":"s1"}]""",
                var v when v.EndsWith("/hareket") => """{"id":"h1","bakiye":1010}""",
                var v when v.Contains("/kasalar/") => """{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1000,"aktif":true,"subeId":"s1"}""",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> Ciz(string mod = ArayuzModuServisi.Basit)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(new OturumKullanici(
            "u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Kasalar>();
        e.WaitForElement("#kasa-gun-kapat-k1");
        return e;
    }

    private static string Hata400(string alan, string mesaj) =>
        JsonSerializer.Serialize(new { message = new[] { mesaj }, alanlar = new Dictionary<string, string[]> { [alan] = [mesaj] }, error = "Bad Request", statusCode = 400 });

    private static int Kez(string metin, string aranan) =>
        System.Text.RegularExpressions.Regex.Matches(metin, System.Text.RegularExpressions.Regex.Escape(aranan)).Count;

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void SKE01_bugunu_KAPANMIS_kasada_rozet_sebep_ve_hareket_gunu_kapat_KILITLI_istek_YOK(string mod)
    {
        bugunKapali = true;
        var e = Ciz(mod);
        e.WaitForElement("#kasa-gun-kapali-k1");
        Assert.Contains(SonKalanlarSozlugu.Tr.GunKapandiRozet, e.Find("#kasa-gun-kapali-k1").TextContent);
        Assert.Equal(SonKalanlarSozlugu.Tr.GunKapandiSebep, e.Find("#kasa-gun-kapali-not-k1").TextContent.Trim());
        Assert.True(e.Find("#kasa-hareket-k1").HasAttribute("disabled"));
        Assert.True(e.Find("#kasa-gun-kapat-k1").HasAttribute("disabled"));
        // Kilitli düğmede tıklama işleyicisi YOK (istek atılamaz); sebep düğmenin title'ında ve kartta yazılı.
        Assert.Equal(SonKalanlarSozlugu.Tr.GunKapandiSebep, e.Find("#kasa-hareket-k1").GetAttribute("title"));
        Assert.Throws<Bunit.MissingEventHandlerException>(() => e.Find("#kasa-gun-kapat-k1").Click());
    }

    [Fact]
    public void SKE02_gunu_ACIK_kasada_rozet_YOK_dugmeler_ACIK()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#kasa-gun-kapali-k1"));
        Assert.False(e.Find("#kasa-hareket-k1").HasAttribute("disabled"));
        Assert.False(e.Find("#kasa-gun-kapat-k1").HasAttribute("disabled"));
    }

    [Fact]
    public void SKE03_hareket_cekmecesi_Kaydet_ALTLIKTA_sunucu_alan_hatasi_YALNIZ_alan_altinda_bantta_TEKRAR_YOK()
    {
        const string mesaj = "Tutar çok büyük — kaydedilebilecek en büyük değeri aşıyor";
        postYanit["/hareket"] = (400, Hata400("tutar", mesaj));
        var e = Ciz();
        e.Find("#kasa-hareket-k1").Click();
        var kaydet = e.Find("#h-kaydet");
        Assert.NotNull(kaydet.Closest("footer"));                       // Gün Kapat ile AYNI düzen
        Assert.Single(e.FindAll("#cekmece-kasa-hareket #h-kaydet"));     // gövdede ikinci Kaydet yok
        e.Find("#h-tutar").Input("5000");
        e.Find("#h-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(mesaj, e.Find("#alan-hata-h-tutar").TextContent));
        Assert.Equal(1, Kez(e.Find("#cekmece-kasa-hareket").TextContent, mesaj));
        Assert.Contains("hatali", e.Find("#h-tutar").ClassName ?? "");
    }

    [Fact]
    public void SKE04_hareket_alan_DISI_red_BANTTA_kalir_yutulmaz()
    {
        const string mesaj = "Bu kasanın 03.10.2026 günü kapandı (sayılan 1.000,00 ₺). Kapanmış güne kayıt yazılamaz; yeniden açmak için işletme sahibine başvurun.";
        postYanit["/hareket"] = (400, JsonSerializer.Serialize(new { message = mesaj, error = "Bad Request", statusCode = 400 }));
        var e = Ciz();
        e.Find("#kasa-hareket-k1").Click();
        e.Find("#h-tutar").Input("10");
        e.Find("#h-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Kez(e.Find("#cekmece-kasa-hareket").TextContent, mesaj)));
    }

    [Theory]
    [InlineData("1200", 200, true)]
    [InlineData("900", 100, false)]
    public void SKE05_gun_kapat_fark_YONUYLE_yazilir(string sayilan, int fark, bool fazla)
    {
        var tutar = Bicim.Tl(fark);
        var e = Ciz();
        e.Find("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#gun-nakit-defter");
        e.Find("#gun-sayilan").Input(sayilan);
        var beklenen = string.Format(fazla ? SonKalanlarSozlugu.Tr.FarkFazla : SonKalanlarSozlugu.Tr.FarkEksik, tutar);
        Assert.Equal(N(beklenen), N(e.Find("#gun-fark").TextContent));
        Assert.DoesNotContain("-", e.Find("#gun-fark").TextContent);
    }

    [Fact]
    public void SKE06_gun_kapat_sunucu_gerekce_reddi_ALAN_altinda_bantta_TEKRAR_YOK()
    {
        const string mesaj = "Sayılan nakit beklenenden 100,00 ₺ eksik (beklenen nakit 1.000,00 ₺). Farkın nedenini yazın.";
        postYanit["/gun-kapanisi"] = (400, Hata400("aciklama", mesaj));
        var e = Ciz();
        e.Find("#kasa-gun-kapat-k1").Click();
        e.WaitForElement("#gun-nakit-defter");
        e.Find("#gun-sayilan").Input("900");
        // PAKET GOC-A: K30 (en az 20) ekranda da uygulanır — sunucu reddinin alan altında görünmesi için ekran kuralını
        // geçen bir gerekçe yazılır (sunucunun kendi sebebi — burada sahte 400 — yine alan altında, bantta tekrar yok).
        e.Find("#gun-aciklama").Input("sayımda yüz lira eksik çıktı");
        e.Find("#gun-kapat-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains(mesaj, e.Find("#alan-hata-gun-aciklama").TextContent));
        Assert.Equal(1, Kez(e.Find("#cekmece-kasa-gun").TextContent, mesaj));
    }

    private static string N(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"[\s\u00A0\u202F]+", " ").Trim();
}

// ═══════════ Ekran (bUnit) — Cariler tahsilat çekmecesi ═══════════
public sealed class SonKalanlarTahsilatEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string kasaYaniti = """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""";
    private (int Kod, string Govde)? tahsilatYaniti;

    public SonKalanlarTahsilatEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(SonKalanlarTahsilatEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            if (istek.Method == HttpMethod.Post && yol.EndsWith("/tahsilat") && t.tahsilatYaniti is { } ty)
                return new HttpResponseMessage((HttpStatusCode)ty.Kod) { Content = new StringContent(ty.Govde, Encoding.UTF8, "application/json") };
            var g = yol switch
            {
                var y when y.EndsWith("/kasalar") => t.kasaYaniti,
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/kur/gecmis") => "[]",
                var y when y.Contains("/kur/pariteler") => """{"pariteler":[]}""",
                var y when y.EndsWith("/kur/kaynaklar") => "[]",
                var y when y.Contains("/tevsik/durum") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/cari/c1") =>
                    """{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":null,"email":null,"vergiNo":null,"adres":null}""",
                var y when y.Contains("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","tlBakiye":1500,"hasGramBakiye":0}]""",
                var y when y.Contains("/belgeler") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> TahsilatAc(string mod = ArayuzModuServisi.Basit)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("button");
        e.WaitForAssertion(() => Assert.Contains(e.FindAll("button"), b => b.TextContent.Contains("Tahsilat")));
        e.FindAll("button").First(b => b.TextContent.Contains("Tahsilat")).Click();
        e.WaitForElement("#th-tutar");
        return e;
    }

    private static int Kez(string metin, string aranan) =>
        System.Text.RegularExpressions.Regex.Matches(metin, System.Text.RegularExpressions.Regex.Escape(aranan)).Count;

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void SKC01_tahsilat_CEKMECE_Kaydet_altlikta_tutar_kutusu_TAM_genislik(string mod)
    {
        var e = TahsilatAc(mod);
        Assert.NotNull(e.Find("#cekmece-tahsilat"));
        Assert.NotNull(e.Find("#th-kaydet").Closest("footer"));
        Assert.Null(e.Find("#th-tutar").Closest(".form-grid-2"));   // iki sütunlu ızgarada kesilmez
        Assert.Empty(e.FindAll(".modal #th-tutar"));
    }

    [Fact]
    public void SKC02_kasa_secin_KILIT_sebebi_TEK_yerde_altlikta()
    {
        kasaYaniti = """
            [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true},
             {"id":"ks3","ad":"Yedek TL","paraBirimi":"TRY","aktif":true}]
            """;
        var e = TahsilatAc();
        e.Find("#th-tutar").Input("1500");
        e.WaitForElement("#th-kasa");
        Assert.True(e.Find("#th-kaydet").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#th-kasa-gerekli"));
        // Tur 2 (doğrulayıcı DÜŞÜK, kasaSecinKez=2): altlık yer tutucunun cümlesini TEKRAR ETMEZ; ne yapılacağını söyler.
        Assert.Equal("Kaydetmek için yukarıdan kasayı seçin", e.Find("#th-kaydet-kilit").TextContent.Trim());
        var cekmece = e.Find("#cekmece-tahsilat").TextContent;
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(cekmece, "kasa seçin",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant).Count);   // yalnız yer tutucu
        Assert.NotNull(e.Find("#th-kaydet-kilit").Closest("footer"));   // sebep altlıkta, gövdede ayrı satır yok
    }

    [Fact]
    public void SKC03_sunucu_tlTutari_reddi_KUTU_altinda_kirmizi_bantta_TEKRAR_YOK_yazinca_silinir()
    {
        const string mesaj = "Tutar en az 0,01 olmalıdır — daha küçük tutar kaydedilemez";
        tahsilatYaniti = (400, JsonSerializer.Serialize(new { message = new[] { mesaj }, alanlar = new Dictionary<string, string[]> { ["tlTutari"] = [mesaj] }, error = "Bad Request", statusCode = 400 }));
        var e = TahsilatAc();
        // GIRDI-AILESI-2 tur 3 (K9): "0,001" artık ekranda kilitlenir (2 ondalık) — sunucu reddi yolu
        // iki ondalıklı değerle sınanır (sahte sunucu her tutarı reddeder).
        e.Find("#th-tutar").Input("0,01");
        e.Find("#th-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(mesaj, e.Find("#th-tutar-hata").TextContent));
        Assert.Contains("hatali", e.Find("#th-tutar").ClassName ?? "");
        Assert.Equal(1, Kez(e.Find("#cekmece-tahsilat").TextContent, mesaj));
        e.Find("#th-tutar").Input("5");
        Assert.Empty(e.FindAll("#th-tutar-hata"));
    }

    [Fact]
    public void SKC04_alan_DISI_tahsilat_reddi_BANTTA_kalir()
    {
        const string mesaj = "Bu kasanın 03.10.2026 günü kapandı (sayılan 0,00 ₺). Kapanmış güne kayıt yazılamaz; yeniden açmak için işletme sahibine başvurun.";
        tahsilatYaniti = (400, JsonSerializer.Serialize(new { message = mesaj, error = "Bad Request", statusCode = 400 }));
        var e = TahsilatAc();
        e.Find("#th-tutar").Input("100");
        e.Find("#th-kaydet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Kez(e.Find("#cekmece-tahsilat").TextContent, mesaj)));
    }
}

// ═══════════ Ekran (bUnit) — Gider geçmişi kapalı akordeon ═══════════
public sealed class SonKalanlarGiderEkranTestleri : BunitContext
{
    public SonKalanlarGiderEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var g = yol switch
            {
                var y when y.EndsWith("/kasalar") => """[{"id":"k1","ad":"Ana Kasa","paraBirimi":"TRY","bakiye":1000,"aktif":true,"subeId":"s1"}]""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") => """[{"kasaId":"k1","paraBirimi":"TRY","toplam":1000,"nakit":1000,"kart":0,"havale":0}]""",
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[{"id":"bh1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,"iban":"TR11","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}""",
                var y when y.EndsWith("/gider-kategorileri") => """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":1}""",
                var y when y.EndsWith("/giderler") =>
                    """{"satirlar":[{"id":"g1","kasaId":"k1","kasaAdi":"Ana Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":500,"paraBirimi":"TRY","aciklama":"Ekim kirası","tarih":"2026-10-01T06:00:00Z","stornoSatiri":false,"stornolandi":false}],"toplamAdet":1,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"x"}""",
                var y when y.EndsWith("/virman") =>
                    """{"satirlar":[{"cikisBacakId":"a1","girisBacakId":"b1","kaynakTur":"KASA","kaynakAdi":"Ana Kasa","hedefTur":"BANKA","hedefAdi":"Ziraat","tutar":100,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-10-01T06:00:00Z","stornoSatiri":false}],"toplam":1,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"x"}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, Encoding.UTF8, "application/json") });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> Ciz(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var modu = Services.GetRequiredService<ArayuzModuServisi>();
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => modu.BasitMi);
        var e = Render<Kuyumcu.Web.Pages.Gider>();
        e.WaitForElement("#gd-gecmis-ac", TimeSpan.FromSeconds(10));
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void SKG01_gider_gecmisi_ACILISTA_KAPALI_sayi_rozeti_gercek_acinca_liste_ve_degismez_notu(string mod)
    {
        var e = Ciz(mod);
        e.WaitForAssertion(() => Assert.Equal("1", e.Find("#gd-gecmis-sayi").TextContent.Trim()));
        Assert.Equal("false", e.Find("#gd-gecmis-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#gd-storno-g1"));
        Assert.Empty(e.FindAll("#degismez-ipucu-Gider"));
        Assert.Empty(e.FindAll("#gd-toplam"));
        Assert.DoesNotContain("Ekim kirası", e.Markup);
        e.Find("#gd-gecmis-ac").Click();
        e.WaitForElement("#gd-storno-g1");
        Assert.NotNull(e.Find("#degismez-ipucu-Gider"));
        Assert.NotNull(e.Find("#gd-toplam"));
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void SKG02_aciklama_ACILISTA_KAPALI_Not_ekle_ile_acilir(string mod)
    {
        var e = Ciz(mod);
        Assert.Empty(e.FindAll("#gd-aciklama"));
        Assert.Equal(ParaDemo2Sozlugu.Tr.NotEkle, e.Find("#gd-not-ekle").TextContent.Trim());
        e.Find("#gd-not-ekle").Click();
        Assert.NotNull(e.Find("#gd-aciklama"));
        Assert.Empty(e.FindAll("#gd-not-ekle"));
    }

    [Fact]
    public void SKG03_virman_gecmisi_ACILISTA_KAPALI_acinca_satir()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        e.Find("#gd-sekme-virman").Click();
        e.WaitForElement("#gd-virman-gecmis-ac");
        Assert.Equal("false", e.Find("#gd-virman-gecmis-ac").GetAttribute("aria-expanded"));
        Assert.Empty(e.FindAll("#gd-v-storno-a1"));
        e.Find("#gd-virman-gecmis-ac").Click();
        e.WaitForElement("#gd-v-storno-a1");
    }
}

// ═══════════ Ekran (bUnit) — Tur 2: banka hesabı para birimi kilidi (tasarım hakemi KRİTİK) ═══════════
// Kök: 409 alan:paraBirimi d-doviz'e eşleniyordu ama Secim alan hatası çizmiyordu → bant da boş →
// "Kaydet'e basınca hiçbir şey olmuyor". Kural: kilit KAYDETMEDEN önce okunur ve yazılır; sunucu yine
// reddederse mesaj alanın ALTINDA görünür (hiçbir durumda yutulmaz).
public sealed class SonKalanlarBankaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private decimal bakiye;
    private string hareketYaniti = """{"hareketler":[]}""";
    private int hareketKodu = 200;
    private (int Kod, string Govde)? patchYaniti;

    public SonKalanlarBankaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private sealed class SahteApi(SonKalanlarBankaEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            t.istekler.Add((istek.Method.Method, yol, govde));
            if (istek.Method == HttpMethod.Patch && yol.Contains("/bankalar/b1") && t.patchYaniti is { } py)
                return new HttpResponseMessage((HttpStatusCode)py.Kod) { Content = new StringContent(py.Govde, Encoding.UTF8, "application/json") };
            if (istek.Method == HttpMethod.Get && yol.Contains("/bankalar/b1/hareketler") && t.hareketKodu != 200)
                return new HttpResponseMessage((HttpStatusCode)t.hareketKodu)
                { Content = new StringContent("""{"message":"Sunucu hatası","statusCode":500}""", Encoding.UTF8, "application/json") };
            var g = yol switch
            {
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur1","altinHasFiyati":4100,"gramAltinFiyati":4300,"usdTry":34,"eurTry":37,"gbpTry":43,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-10-03T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"amacBeyaniEsigi":200000,"beyanFormuEsigi":2000000,"ekBelgeEsigi":20000000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                var y when y.Contains("/bankalar/") && y.Contains("/hareketler") => t.hareketYaniti,
                var y when y.Contains("/bankalar") && istek.Method == HttpMethod.Get =>
                    $$"""{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Döviz Hesabı","hesapSahibi":null,"iban":"TR200000000000000000000001","paraBirimi":"USD","bakiye":{{t.bakiye.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"aktif":true}]}""",
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube"}]""",
                var y when y.Contains("/pos/cihazlar") => "[]",
                var y when y.Contains("/cari") => "[]",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> DuzenleAc(string mod = ArayuzModuServisi.Basit)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Banka>();
        e.WaitForElement("#bh-duzenle-b1");
        istekler.Clear();
        e.Find("#bh-duzenle-b1").Click();
        e.WaitForElement("#cekmece-banka-hesap");
        return e;
    }

    private static bool SecenekKilitli(IRenderedComponent<Kuyumcu.Web.Pages.Banka> e) =>
        e.FindAll("#d-doviz input[type=radio]").All(r => r.HasAttribute("disabled"));

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void SKB01_BAKIYELI_hesapta_doviz_KILITLI_sebep_kaydetmeden_ONCE_yazar(string mod)
    {
        bakiye = 100m;
        var e = DuzenleAc(mod);
        e.WaitForAssertion(() => Assert.Contains("Bakiyesi olan hesabın para birimi değişmez — yeni hesap açın.",
            e.Find("#degismez-ipucu-banka-hesap-doviz").TextContent));
        Assert.True(SecenekKilitli(e));
        Assert.NotEmpty(e.FindAll("#d-doviz input[type=radio]"));
    }

    [Fact]
    public void SKB02_bakiyesi_SIFIR_ama_HAREKETLI_hesapta_KILITLI_hareket_sorusu_mevcut_uctan()
    {
        bakiye = 0m;
        hareketYaniti = """{"hareketler":[{"id":"bh1","yon":"GELEN","tutar":100,"karsiTarafAd":null,"aciklama":null,"durum":"BEKLIYOR","tarih":"2026-10-03T10:00:00Z"}]}""";
        var e = DuzenleAc();
        e.WaitForAssertion(() => Assert.Contains("Hareketi olan hesabın para birimi değişmez — yeni hesap açın.",
            e.Find("#degismez-ipucu-banka-hesap-doviz").TextContent));
        Assert.True(SecenekKilitli(e));
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/bankalar/b1/hareketler") && i.Yol.Contains("limit=1"));
    }

    [Fact]
    public void SKB03_GECMISSIZ_hesapta_doviz_ACIK_sunucu_409_paraBirimi_reddi_ALAN_ALTINDA_gorunur_bantta_tekrar_YOK()
    {
        bakiye = 0m;
        const string mesaj = "Hareket görmüş banka hesabının para birimi değiştirilemez — yeni hesap açın";
        patchYaniti = (409, JsonSerializer.Serialize(new { message = mesaj, alanlar = new Dictionary<string, string[]> { ["paraBirimi"] = [mesaj] }, error = "Conflict", statusCode = 409 }));
        var e = DuzenleAc();
        e.WaitForAssertion(() => Assert.False(SecenekKilitli(e)));
        Assert.Empty(e.FindAll("#degismez-ipucu-banka-hesap-doviz"));
        e.Find("#d-doviz input[value='TRY']").Change("TRY");
        e.Find("#cekmece-banka-hesap-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(mesaj, e.Find("#alan-hata-d-doviz").TextContent));
        Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Govde.Contains("\"paraBirimi\":\"TRY\""));
        Assert.NotNull(e.Find("#cekmece-banka-hesap"));                                   // çekmece açık, sessiz değil
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(
            e.Find("#cekmece-banka-hesap").TextContent, System.Text.RegularExpressions.Regex.Escape(mesaj)).Count);
    }

    [Fact]
    public void SKB04_hareket_sorusu_OKUNAMAZSA_kilitli_kalir_hata_serbest_SAYILMAZ()
    {
        bakiye = 0m;
        hareketKodu = 500;
        var e = DuzenleAc();
        e.WaitForAssertion(() => Assert.Contains("hesabın geçmişi okunamadı",
            e.Find("#degismez-ipucu-banka-hesap-doviz").TextContent));
        Assert.True(SecenekKilitli(e));
    }

    /// <summary>
    /// Hakem: "eşlenen ama çizilmeyen alan kalmasın" — Banka'nın sunucu alan eşlemelerindeki HER ekran alanı
    /// sayfada alan hatası çizen bir bileşene (HataAlani / AlanHatasi Alan) bağlı olmalı; yoksa
    /// FormDurumu.BantMesaji bandı null'a çevirir ve ileti hiçbir yerde görünmez.
    /// </summary>
    [Fact]
    public void SKB05_banka_sunucu_alan_eslemesindeki_HER_alan_ekranda_CIZILI()
    {
        var kaynak = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "Pages", "Banka.razor"));
        foreach (var sozluk in new[] { "SunucuAlanHesapYeni", "SunucuAlanHesapDuzenle", "SunucuAlanDekont" })
        {
            var m = System.Text.RegularExpressions.Regex.Match(kaynak,
                sozluk + @"\s*=\s*new\(\)\s*\{(?<g>.*?)\};", System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.True(m.Success, sozluk + " bulunamadı");
            var alanlar = System.Text.RegularExpressions.Regex.Matches(m.Groups["g"].Value, "\\]\\s*=\\s*\"([^\"]+)\"")
                .Select(x => x.Groups[1].Value).ToList();
            Assert.NotEmpty(alanlar);
            foreach (var a in alanlar)
                Assert.True(kaynak.Contains($"HataAlani=\"{a}\"") || kaynak.Contains($"AlanHatasi Alan=\"{a}\""),
                    $"{sozluk}: '{a}' eşleniyor ama ekranda alan hatası ÇİZİLMİYOR (ileti yutulur)");
        }
    }
}
