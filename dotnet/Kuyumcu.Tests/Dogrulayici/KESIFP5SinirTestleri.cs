using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P5 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT metin/sayıdır
// (üretim kodundan hesaplanmaz). KESIF-P5'in para gösteren/hesaplayan yolları:
// FisKalemTablosu.BirimFiyatMetni (satır içi birim fiyat, tr-TR binlikli),
// geri ayrıştırma FormAlani.SayiCoz, SATIS-4 (a) nakit sınırı eşleşmesi
// TevsikOneri.Gerekli ("aşan" — tam hadde nakit serbest), GET /fiyat/teklifler
// sınırları (25 ürün, tekrar eden kimlik tekilleşir).
// ════════════════════════════════════════════════════════════════════════
public sealed class KESIFP5SinirTestleri
{
    [Fact]
    public void Sifir_birim_fiyat_kurusla_yazilir()
        => Assert.Equal("0,00", FisKalemTablosu.BirimFiyatMetni(0m));

    [Fact]
    public void Eksi_birim_fiyat_isaret_korunur_binlikli()
        => Assert.Equal("-1.500,00", FisKalemTablosu.BirimFiyatMetni(-1500m));

    [Fact]
    public void Kurus_alti_0_005_YUVARLANMAZ_ve_geri_ayni_deger_cozulur()
    {
        Assert.Equal("10,005", FisKalemTablosu.BirimFiyatMetni(10.005m));
        Assert.Equal(10.005m, FormAlani.SayiCoz("10,005").Deger);
    }

    [Fact]
    public void Dort_hane_0_0001_kaybolmaz()
    {
        Assert.Equal("0,0001", FisKalemTablosu.BirimFiyatMetni(0.0001m));
        Assert.Equal(0.0001m, FormAlani.SayiCoz(FisKalemTablosu.BirimFiyatMetni(0.0001m)).Deger);
    }

    [Fact]
    public void Cok_buyuk_tutar_binlikli_ve_gidis_donus_ayni()
    {
        Assert.Equal("999.999.999,99", FisKalemTablosu.BirimFiyatMetni(999_999_999.99m));
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
    }

    [Fact]
    public void Binlikli_metin_gidis_donus_11052_67()
    {
        Assert.Equal("11.052,67", FisKalemTablosu.BirimFiyatMetni(11052.67m));
        Assert.Equal(11052.67m, FormAlani.SayiCoz("11.052,67").Deger);
        // gösterilen "1.500,00" belirsiz değildir (kuruş yazılı) → 1500
        Assert.Equal(1500m, FormAlani.SayiCoz("1.500,00").Deger);
    }

    [Fact]
    public void Bos_null_ve_Turkce_giris()
    {
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
    }

    [Fact]
    public void Nakit_siniri_tam_hadde_SERBEST_bir_kurus_ustu_ASILIR()
    {
        Assert.False(TevsikOneri.Gerekli("NAKIT", "SATIS", 30_000m, 30_000m));
        Assert.True(TevsikOneri.Gerekli("NAKIT", "SATIS", 30_000.01m, 30_000m));
        Assert.True(TevsikOneri.Gerekli("NAKIT", "HURDA_ALIS", 30_000.01m, 30_000m));
        Assert.False(TevsikOneri.Gerekli("KART", "SATIS", 999_999_999.99m, 30_000m));
    }

    [Fact]
    public void Nakit_siniri_had_sifir_ya_da_eksi_ise_istemci_esik_UYDURMAZ()
    {
        Assert.False(TevsikOneri.Gerekli("NAKIT", "SATIS", 999_999_999.99m, 0m));
        Assert.False(TevsikOneri.Gerekli("NAKIT", "SATIS", 50_000m, -1m));
        Assert.False(TevsikOneri.Gerekli("NAKIT", "SATIS", -50_000m, 30_000m));
        Assert.False(TevsikOneri.Gerekli("NAKIT", "SATIS", 0m, 30_000m));
    }

    [Fact]
    public void Sozluk_metinlerinde_esik_sayisi_GOMULU_DEGIL()
    {
        foreach (var m in new[] { KesifP5Sozlugu.Tr, KesifP5Sozlugu.En })
        {
            Assert.DoesNotContain("30", m.OdemeOtomatikKart);
            Assert.DoesNotContain("30", m.OdemeOtomatikHavale);
            Assert.Contains("{0}", m.OdemeOtomatikKart);
            Assert.Contains("{0}", m.OdemeOtomatikHavale);
        }
    }
}

[Collection("http")]
public sealed class KESIFP5HttpSinirTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Toplu_teklif_TAM_25_kabul_tekrarlar_tekillesir_bilinmeyen_satir_uretmez()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETKESIF-P5-DOG sinir");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/fiyat/listeler",
            new { ad = "Perakende", tur = "PERAKENDE", hasMarjYuzde = 5m, iscilikCarpanYuzde = 100m, varsayilan = true });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var tezgah = f.Ortam.Istemci(k, "TEZGAHTAR");

        // tam 25 benzersiz kimlik (24 bilinmeyen + 1 gerçek) → 200, yalnız gerçek ürün satır üretir
        var yirmiBes = string.Join(',', Enumerable.Range(0, 24).Select(i => $"yok{i}").Append(k.UrunId));
        var r = await tezgah.GetAsync($"/api/v1/fiyat/teklifler?urunIds={yirmiBes}&kurSnapshotId={k.KurId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(1, (await r.Json()).GetProperty("satirlar").GetArrayLength());

        // 30 kez aynı kimlik → tekilleşir (25 sınırını aşmaz), tek satır
        var tekrar = string.Join(',', Enumerable.Repeat(k.UrunId, 30));
        var r2 = await tezgah.GetAsync($"/api/v1/fiyat/teklifler?urunIds={tekrar}&kurSnapshotId={k.KurId}");
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var s = (await r2.Json()).GetProperty("satirlar");
        Assert.Equal(1, s.GetArrayLength());
        Assert.True(s[0].GetProperty("satisFiyati").GetDecimal() > 0m);

        // 26 benzersiz → 400 (sessiz kırpma yok)
        var yirmiAlti = string.Join(',', Enumerable.Range(0, 26).Select(i => $"z{i}"));
        Assert.Equal(HttpStatusCode.BadRequest,
            (await tezgah.GetAsync($"/api/v1/fiyat/teklifler?urunIds={yirmiAlti}&kurSnapshotId={k.KurId}")).StatusCode);
    }
}

// ════════════════════════════════════════════════════════════════════════
// KESIF-P5 tur 2 doğrulayıcısı (Aşkın + Mehmet, 01.10) — satır içi KUTU yolu
// (FisKalemTablosu → SatirIciDuzenle → FiyatDegistir): Türkçe giriş, eksi/boş/
// belirsiz reddi, çok büyük tutar, gösterilen binlikli metnin aynen kaydı.
// Beklenen değerler SABİT.
// ════════════════════════════════════════════════════════════════════════
public sealed class KESIFP5SinirKutuTestleri : Bunit.BunitContext
{
    private readonly FormDurumu form = new();

    public KESIFP5SinirKutuTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<DilServisi>(Services);
        GocC2Ortak.IzgaraServisleri(Services);   // GOC-C-2: kalem tablosu ortak ızgarada (ApiIstemci ister)
    }

    private (Bunit.IRenderedComponent<FisKalemTablosu> T, FisKalemi K) Tablo(decimal fiyat)
    {
        var k = new FisKalemi { UrunId = "u1", UrunAdi = "Burma Bilezik", Adet = 1m, BirimFiyat = fiyat };
        var t = Render<FisKalemTablosu>(p => p
            .Add(x => x.Kalemler, [k])
            .Add(x => x.Urunler, Array.Empty<Kuyumcu.Contracts.Sayfalar.Islemler.UrunVeri>())
            .Add(x => x.Form, form)
            .Add(x => x.Toplam, k.BirimFiyat)
            .Add(x => x.Degisti, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<bool>(this, _ => { })));
        t.Find("#islem-kalem-duzenle-0").Click();
        return (t, k);
    }

    private static void Yaz(Bunit.IRenderedComponent<FisKalemTablosu> t, string metin)
    {
        t.Find("#si-kalem-fiyat-u1-ac").Click();
        t.Find("#si-kalem-fiyat-u1").Input(metin);
        t.Find("#si-kalem-fiyat-u1-kaydet").Click();
    }

    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0", "0")]
    [InlineData("0,005", "0.005")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    public void Kutu_Turkce_giris_kabul(string metin, string beklenen)
    {
        var (t, k) = Tablo(100m);
        Yaz(t, metin);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), k.BirimFiyat);
    }

    [Theory]
    [InlineData("1.500")]     // belirsiz
    [InlineData("-5")]        // eksi
    [InlineData("abc")]       // geçersiz
    public void Kutu_red_deger_DEGISMEZ_alan_hatasi_gorunur(string metin)
    {
        var (t, k) = Tablo(100m);
        Yaz(t, metin);
        Assert.Equal(100m, k.BirimFiyat);
        Assert.True(form.Var("si-kalem-fiyat-u1"), "alan altı hata yok: " + metin);
        Assert.False(string.IsNullOrWhiteSpace(form.Mesaj("si-kalem-fiyat-u1")));
    }

    [Fact]
    public void Kutu_bos_REDDEDILIR_kutu_altinda_bos_birakilamaz()
    {
        var (t, k) = Tablo(100m);
        Yaz(t, "   ");
        Assert.Equal(100m, k.BirimFiyat);
        Assert.Contains("Bu alan boş bırakılamaz.", t.Markup);
    }

    [Fact]
    public void Kutu_gosterilen_binlikli_buyuk_tutar_aynen_kaydedilince_degismez()
    {
        var (t, k) = Tablo(999_999_999.99m);
        Assert.Equal("999.999.999,99", t.Find("#si-kalem-fiyat-u1-ac").TextContent.Trim());
        t.Find("#si-kalem-fiyat-u1-ac").Click();
        t.Find("#si-kalem-fiyat-u1-kaydet").Click();
        Assert.Equal(999_999_999.99m, k.BirimFiyat);
    }
}
