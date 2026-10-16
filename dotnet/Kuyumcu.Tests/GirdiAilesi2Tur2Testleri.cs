using Bunit;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya doğrulayıcı tur 1 bulgularının SINIRLARINI sınar; geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — TUR 2 (hasım doğrulayıcı + tasarım hakemi bulguları):
//   (1) IBAN yapıştırma temizliği "IBAN: TR33-…" ön ekinde ülke kodunu IBAN sözcüğünden almasın
//   (2) yabancı IBAN yazılırken tamamlanana dek Kaydet kilitli + alan altı neden
//   (3) Telefon 444 + 4 hane (alan kodsuz ulusal tek numara)
//   (4) TCKN-veya-VKN hane mesajı "10 ya da 11"
//   (5) Gram Tur=Gram/Tas/Has → Core AgirlikKurali (K10/K11); Tur yoksa bugünkü davranış
//   (6) Kaydet kilit sebebi bağlantı mavisi değil, soluk ton (tasarım hakemi KRİTİK)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GirdiAilesi2Tur2Testleri : BunitContext
{
    private readonly GecersizAlanKapisi kapi = new();
    private object? son;

    public GirdiAilesi2Tur2Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private IRenderedComponent<CascadingValue<GecersizAlanKapisi>> Ciz<T, TDeger>(Dictionary<string, object?>? ek = null)
        where T : IComponent
    {
        return Render<CascadingValue<GecersizAlanKapisi>>(p => p
            .Add(x => x.Value, kapi)
            .Add(x => x.IsFixed, true)
            .Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<T>(0);
                b.AddAttribute(1, "ElemanId", "t");
                b.AddAttribute(2, "Etiket", "Alan");
                b.AddAttribute(3, "DegerChanged", EventCallback.Factory.Create<TDeger>(this, v => son = v));
                if (ek is not null) b.AddMultipleAttributes(4, ek.Select(x => new KeyValuePair<string, object>(x.Key, x.Value!)));
                b.CloseComponent();
            })));
    }

    // ── (1) IBAN ön ekli yapıştırma ─────────────────────────────────────────
    [Theory]
    [InlineData("iban: tr33-0006 1005/1978 6457 8413 26")]
    [InlineData("IBAN TR33 0006 1005 1978 6457 8413 26")]
    [InlineData("Hesap 1: TR330006100519786457841326 (Ziraat)")]
    [InlineData("Ref AB12 / IBAN TR33 0006 1005 1978 6457 8413 26")]   // TR, önceki 2 harf + 2 rakamdan ÖNCE gelir
    [InlineData("TR33 0006 1005 1978 6457 8413 26")]
    public void T2_Iban_on_ekli_yapistirma_IBAN_sozcugunu_ulke_kodu_saymaz(string yapistirilan)
    {
        Assert.Equal("TR33 0006 1005 1978 6457 8413 26", Iban.Maskele(yapistirilan));
        var e = Ciz<Iban, string?>();
        e.Find("#t").Input(yapistirilan);
        Assert.Equal("TR33 0006 1005 1978 6457 8413 26", e.Find("#t").GetAttribute("value"));
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.False(kapi.Var);
        Assert.Equal("TR330006100519786457841326", son);
    }

    [Fact]
    public void T2_Iban_on_ekli_bozuk_IBAN_26_karakterde_hata_ve_kilit()
    {
        var e = Ciz<Iban, string?>();
        e.Find("#t").Input("IBAN: TR34-0006 1005 1978 6457 8413 26");
        Assert.Equal("TR34 0006 1005 1978 6457 8413 26", e.Find("#t").GetAttribute("value"));
        Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, e.Find("#t-hata").TextContent.Trim());
        Assert.True(kapi.Var);
        Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, kapi.Sebep("tr"));
    }

    [Fact]
    public void T2_Iban_maske_idempotent_ve_yabanci_icinde_TR_rakam_basa_kesilmez()
    {
        // Yabancı IBAN'ın içinde "TR12" geçse bile başı zaten 2 harf + 2 rakam → olduğu gibi kalır.
        Assert.Equal("GB82 WEST 1234 5698 7654 32", Iban.Maskele("GB82 WEST 1234 5698 7654 32"));
        Assert.Equal("GB82 WETR 1234", Iban.Maskele("GB82WETR1234"));
        Assert.Equal(Iban.Maskele("IBAN: TR33 0006"), Iban.Maskele(Iban.Maskele("IBAN: TR33 0006")));
        Assert.Equal("TR33 0006 1", Iban.Maskele("3300061"));   // rakamla başlayan yazım → TR öneki (eski davranış)
    }

    // ── (2) yabancı IBAN yazarken kilit ─────────────────────────────────────
    [Fact]
    public void T2_Yabanci_IBAN_tamamlanana_dek_alan_altinda_neden_ve_Kaydet_kilitli()
    {
        var e = Ciz<Iban, string?>();
        e.Find("#t").Input("GB82 WEST 1234");
        var beklenen = GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.IbanEksik, 12), "tr");
        Assert.Equal(beklenen, e.Find("#t-hata").TextContent.Trim());
        Assert.True(kapi.Var);
        Assert.Equal(beklenen, kapi.Sebep("tr"));

        e.Find("#t").Input("GB82 WEST 1234 5698 7654 32");   // ISO 13616 örnek İngiliz IBAN'ı (geçerli)
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.False(kapi.Var);
        Assert.Equal("GB82WEST12345698765432", son);
    }

    [Fact]
    public void T2_Yabanci_IBAN_ulke_kodu_ve_kontrol_hanesinden_once_hata_yok()
    {
        var e = Ciz<Iban, string?>();
        e.Find("#t").Input("GB8");
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.False(kapi.Var);
    }

    // ── (3) Telefon 444 ─────────────────────────────────────────────────────
    [Theory]
    [InlineData("444 1 234")]
    [InlineData("4441234")]
    [InlineData("0444 1234")]
    [InlineData("+90 444 12 34")]
    public void T2_Telefon_444_dort_hane_gecerli(string tel) => Assert.Null(TelefonDogrulayici.Denetle(tel));

    [Theory]
    [InlineData("444 123")]        // 6 hane
    [InlineData("444 12345")]      // 8 hane
    [InlineData("445 1234")]       // 444 değil, 7 hane
    public void T2_Telefon_444_disi_kisa_numara_hane_hatasi(string tel) =>
        Assert.Equal(GirdiKodu.TelefonHane, TelefonDogrulayici.Denetle(tel)?.Kod);

    [Theory]
    [InlineData("4441234", "444 12 34")]
    [InlineData("444 1 234", "444 12 34")]
    [InlineData("04441234", "0444 12 34")]
    [InlineData("44412345", "444 123 45")]      // 8. hane → 444 değil, olağan gruplama
    [InlineData("4441", "444 1")]
    public void T2_Telefon_444_maskesi_okunur_gruplar(string girilen, string beklenen) =>
        Assert.Equal(beklenen, Telefon.Maskele(girilen));

    // ── (4) TCKN veya VKN hane mesajı ───────────────────────────────────────
    [Theory]
    [InlineData("123456789", 9)]
    [InlineData("123456789012", 12)]
    public void T2_TcknVeyaVkn_hane_mesaji_10_ya_da_11_der(string no, int hane)
    {
        var h = KimlikKurali.TcknVeyaVkn(no);
        Assert.Equal(GirdiKodu.KimlikHaneIkili, h?.Kod);
        var m = GirdiSozlugu.Mesaj(h!.Value, "tr");
        Assert.Contains("10", m);
        Assert.Contains("11", m);
        Assert.Contains($"şu an {hane}", m);
    }

    [Fact]
    public void T2_Tckn_tek_basina_hane_mesaji_11_kalir() =>
        Assert.Equal(GirdiKodu.KimlikHane, KimlikKurali.Tckn("123456789")?.Kod);

    // ── (5) Gram Tur ────────────────────────────────────────────────────────
    [Fact]
    public void T2_Gram_Tur_Gram_sifir_ve_dort_ondalik_hata_kilit_eksi_tusu_kapali()
    {
        var e = Ciz<Gram, decimal?>(new() { ["Tur"] = (AgirlikTuru?)AgirlikTuru.Gram });
        Assert.Null(e.Find("#t").GetAttribute("data-eksi"));
        e.Find("#t").Input("0");
        Assert.Equal(GirdiSozlugu.Tr.SayiSifir, e.Find("#t-hata").TextContent.Trim());
        Assert.True(kapi.Var);
        e.Find("#t").Input("1,2345");
        Assert.True(kapi.Var);
        e.Find("#t").Input("1,234");
        Assert.False(kapi.Var);
        Assert.Equal(1.234m, son);
    }

    [Fact]
    public void T2_Gram_Tur_Tas_brutten_kucuk_olmali()
    {
        var e = Ciz<Gram, decimal?>(new() { ["Tur"] = (AgirlikTuru?)AgirlikTuru.Tas, ["Brut"] = (decimal?)5m });
        e.Find("#t").Input("5");
        Assert.Equal(GirdiSozlugu.Tr.TasBrut, e.Find("#t-hata").TextContent.Trim());
        Assert.True(kapi.Var);
        e.Find("#t").Input("0");                 // taş 0 olabilir
        Assert.False(kapi.Var);
    }

    [Fact]
    public void T2_Gram_Tur_Has_alti_ondalik_gecer_yedisi_hata()
    {
        var e = Ciz<Gram, decimal?>(new() { ["Tur"] = (AgirlikTuru?)AgirlikTuru.Has });
        e.Find("#t").Input("0,123456");
        Assert.False(kapi.Var);
        e.Find("#t").Input("0,1234567");
        Assert.True(kapi.Var);
    }

    [Fact]
    public void T2_Gram_Tur_yoksa_bugunku_davranis_korunur()
    {
        var e = Ciz<Gram, decimal?>();
        e.Find("#t").Input("0");
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.False(kapi.Var);
        Assert.Equal(0m, son);
    }

    [Theory]
    [InlineData(AgirlikTuru.Gram, "0", GirdiKodu.SayiSifir)]
    [InlineData(AgirlikTuru.Gram, "-1", GirdiKodu.SayiEksi)]
    [InlineData(AgirlikTuru.Tas, "-0.5", GirdiKodu.SayiEksi)]
    [InlineData(AgirlikTuru.Has, "0", GirdiKodu.SayiSifir)]
    public void T2_AgirlikKurali_Denetle_ture_gore(AgirlikTuru tur, string d, GirdiKodu kod) =>
        Assert.Equal(kod, AgirlikKurali.Denetle(decimal.Parse(d, System.Globalization.CultureInfo.InvariantCulture), tur)?.Kod);

    // ── (6) kilit sebebi görünümü ───────────────────────────────────────────
    [Fact]
    public void T2_Kaydet_kilit_sebebi_soluk_tonda_baglanti_mavisi_degil()
    {
        var k = new GecersizAlanKapisi();
        k.Bildir("hesap-iban", "IBAN", true, GirdiSozlugu.Tr.IbanKontrol);
        var e = Render<SayiKapisiSebebi>(p => p
            .Add(x => x.ElemanId, "hesap-kilit-sebep")
            .Add(x => x.Onek, "hesap-")
            .Add(x => x.Kapi, k));
        var sinif = e.Find("#hesap-kilit-sebep").GetAttribute("class") ?? "";
        Assert.Contains("metin-soluk", sinif);
        Assert.DoesNotContain("metin-amber", sinif);
        Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, e.Find("#hesap-kilit-sebep").TextContent.Trim());
    }

    // ── sözlük: yeni kodların iki dilde metni ───────────────────────────────
    [Theory]
    [InlineData(GirdiKodu.IbanEksik)]
    [InlineData(GirdiKodu.KimlikHaneIkili)]
    public void T2_yeni_kodlar_iki_dilde(GirdiKodu kod)
    {
        var tr = GirdiSozlugu.Mesaj(new GirdiHatasi(kod, 9), "tr");
        var en = GirdiSozlugu.Mesaj(new GirdiHatasi(kod, 9), "en");
        Assert.False(string.IsNullOrWhiteSpace(tr));
        Assert.False(string.IsNullOrWhiteSpace(en));
        Assert.NotEqual(tr, en);
    }
}
