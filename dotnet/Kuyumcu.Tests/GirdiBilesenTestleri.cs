using Bunit;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya kuralın SINIRLARINI sınar; geçersiz değerler bilinçlidir
// (denetim.sh TEST_GECERSIZ_FIKSTUR bu dosyayı saymaz).
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — anlamsal girdi bileşenleri [bUnit].
// Tarayıcıdaki tuş engeli girdi-maske.js'tedir (gerçek Chrome'da ölçüldü — rapor);
// burada AYNI maskenin C# karşılığı + alan altı hata + Kaydet kilidi (kapı sebebi
// alan altı metniyle BİREBİR aynı) sınanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GirdiBilesenTestleri : BunitContext
{
    private readonly GecersizAlanKapisi kapi = new();
    private object? son;

    public GirdiBilesenTestleri()
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

    private static string Tr(GirdiKodu k, decimal? a = null, decimal? b2 = null, DateOnly? g = null) =>
        GirdiSozlugu.Mesaj(new GirdiHatasi(k, a, b2, g), "tr");

    private void KilitAyni(IRenderedComponent<CascadingValue<GecersizAlanKapisi>> e, string beklenen)
    {
        Assert.Equal(beklenen, e.Find("#t-hata").TextContent.Trim());
        Assert.True(kapi.Var);
        Assert.Equal(beklenen, kapi.Sebep("tr"));
    }

    // ── IBAN ────────────────────────────────────────────────────────────────
    [Fact]
    public void Iban_yazarken_TR_oneki_4lu_grup_banka_kodunda_harf_yazilmaz()
    {
        var e = Ciz<Iban, string?>();
        e.Find("#t").Input("330006100519");
        Assert.Equal("TR33 0006 1005 19", e.Find("#t").GetAttribute("value"));
        e.Find("#t").Input("tr33ABC0006");
        Assert.Equal("TR33 0006", e.Find("#t").GetAttribute("value"));
        Assert.Empty(e.FindAll("#t-hata"));        // eksik yazımda hata yok
        Assert.False(kapi.Var);
        Assert.Equal("off", e.Find("#t").GetAttribute("autocomplete"));
        Assert.Equal("iban", e.Find("#t").GetAttribute("data-maske"));
    }

    [Fact]
    public void Iban_26_karakterde_mod97_hatasi_alan_altinda_ve_kilit_sebebi_ayni_metin()
    {
        var e = Ciz<Iban, string?>();
        e.Find("#t").Input("TR34 0006 1005 1978 6457 8413 26");
        KilitAyni(e, GirdiSozlugu.Tr.IbanKontrol);
        Assert.Equal("true", e.Find("#t").GetAttribute("aria-invalid"));

        e.Find("#t").Input("TR330006100519786457841326");
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.False(kapi.Var);
        Assert.Equal("TR330006100519786457841326", son);   // çağırana BOŞLUKSUZ gider
        Assert.Equal("TR33 0006 1005 1978 6457 8413 26", e.Find("#t").GetAttribute("value"));
    }

    [Fact]
    public void Iban_eksik_yazim_cikista_uzunluk_hatasi_ve_kilit()
    {
        var e = Ciz<Iban, string?>();
        e.Find("#t").Change("TR33 0006 1005 1978 6457 8413 2");
        KilitAyni(e, Tr(GirdiKodu.IbanUzunluk, 25, 26));
    }

    // ── TELEFON (Telefon + TelefonBicimli tek bileşen) ─────────────────────
    [Fact]
    public void Telefon_harf_yazilmaz_yapistirma_temizlenir_ve_gruplanir()
    {
        var e = Ciz<Telefon, string?>();
        e.Find("#t").Input("0532abc4112233");
        Assert.Equal("0532 411 22 33", e.Find("#t").GetAttribute("value"));
        e.Find("#t").Input("+90 (532) 411-22-33");
        Assert.Equal("+90 532 411 22 33", e.Find("#t").GetAttribute("value"));
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.Equal("telefon", e.Find("#t").GetAttribute("data-maske"));
    }

    [Fact]
    public void Telefon_10_hane_dolunca_aralik_hatasi_ve_kilit()
    {
        var e = Ciz<Telefon, string?>();
        e.Find("#t").Input("0500111223");                   // 9 ulusal hane: henüz hata yok
        Assert.Empty(e.FindAll("#t-hata"));
        e.Find("#t").Input("05001112233");
        KilitAyni(e, GirdiSozlugu.Tr.TelefonAralik);
        e.Find("#t").Input("05321112233");
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.False(kapi.Var);
    }

    [Fact]
    public void TelefonBicimli_artik_Telefon_ile_ayni_bilesen()
    {
        var e = Ciz<TelefonBicimli, string?>();
        e.Find("#t").Input("0532x41122y33");
        Assert.Equal("0532 411 22 33", e.Find("#t").GetAttribute("value"));
        e.Find("#t").Change("5324112233");
        Assert.Equal("0532 411 22 33", e.Find("#t").GetAttribute("value"));   // CUMA-M çıkış biçimi korunur
        e.Find("#t").Input("05001112233");
        KilitAyni(e, GirdiSozlugu.Tr.TelefonAralik);
    }

    // ── E-POSTA ─────────────────────────────────────────────────────────────
    [Fact]
    public void Eposta_bosluk_yazilmaz_cikista_kucuk_harf_ve_kural()
    {
        var e = Ciz<Eposta, string?>();
        e.Find("#t").Input("Ali Veli@Ornek.com");
        Assert.Equal("AliVeli@Ornek.com", e.Find("#t").GetAttribute("value"));
        e.Find("#t").Change("Ali@Ornek.COM");
        Assert.Equal("ali@ornek.com", son);
        Assert.Empty(e.FindAll("#t-hata"));
        e.Find("#t").Change("ali@ornek");
        KilitAyni(e, GirdiSozlugu.Tr.EpostaBicim);
    }

    // ── TARİH ───────────────────────────────────────────────────────────────
    [Fact]
    public void Tarih_08102026_yazilinca_08_10_2026_olur_ve_deger_gider()
    {
        var e = Ciz<Tarih, DateOnly?>();
        e.Find("#t").Input("08102026");
        Assert.Equal("08.10.2026", e.Find("#t").GetAttribute("value"));
        Assert.Equal(new DateOnly(2026, 10, 8), son);
        Assert.Equal("text", e.Find("#t").GetAttribute("type"));
        Assert.Equal("tarih", e.Find("#t").GetAttribute("data-maske"));
        Assert.Equal("1900", e.Find("#t").GetAttribute("data-yil-en-az"));
        Assert.Equal("2100", e.Find("#t").GetAttribute("data-yil-en-cok"));
    }

    [Fact]
    public void Tarih_makine_bicimi_change_geri_uyum()
    {
        var e = Ciz<Tarih, DateOnly?>();
        e.Find("#t").Change("2026-05-01");
        Assert.Equal(new DateOnly(2026, 5, 1), son);
        Assert.Equal("01.05.2026", e.Find("#t").GetAttribute("value"));
    }

    [Fact]
    public void Tarih_olmayan_gun_ve_pencere_disi_yil_hata_ve_kilit()
    {
        var e = Ciz<Tarih, DateOnly?>();
        e.Find("#t").Input("31022026");
        KilitAyni(e, GirdiSozlugu.Tr.TarihGecersiz);
        Assert.Null(son);
        e.Find("#t").Input("01013000");
        KilitAyni(e, Tr(GirdiKodu.TarihGec, g: new DateOnly(2100, 12, 31)));
        e.Find("#t").Input("01011899");
        KilitAyni(e, Tr(GirdiKodu.TarihErken, g: new DateOnly(1900, 1, 1)));
    }

    [Fact]
    public void Tarih_eksik_yazim_cikista_bicim_hatasi()
    {
        var e = Ciz<Tarih, DateOnly?>();
        e.Find("#t").Input("0810");
        Assert.Equal("08.10", e.Find("#t").GetAttribute("value"));
        Assert.Empty(e.FindAll("#t-hata"));
        e.Find("#t").Change("08.10");
        KilitAyni(e, GirdiSozlugu.Tr.TarihBicim);
    }

    [Fact]
    public void Tarih_Islem_turunde_ileri_tarih_ve_Dogum_turunde_18_yas()
    {
        var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
        var e = Ciz<Tarih, DateOnly?>(new() { ["Tur"] = TarihTuru.Islem });
        e.Find("#t").Input(TarihPenceresi.Yaz(bugun.AddDays(1)));
        KilitAyni(e, GirdiSozlugu.Tr.TarihIleri);
        Assert.Equal(bugun.Year.ToString(), e.Find("#t").GetAttribute("data-yil-en-cok"));

        var d = Ciz<Tarih, DateOnly?>(new() { ["Tur"] = TarihTuru.Dogum });
        d.Find("#t").Input(TarihPenceresi.Yaz(bugun.AddYears(-17)));
        Assert.Equal(Tr(GirdiKodu.DogumResit, 18), d.Find("#t-hata").TextContent.Trim());
    }

    [Fact]
    public void Tarih_EnGec_artik_yalniz_gorsel_degil_uygulanir()
    {
        var sinir = new DateOnly(2026, 1, 31);
        var e = Ciz<Tarih, DateOnly?>(new() { ["EnGec"] = (DateOnly?)sinir });
        e.Find("#t").Input("01022026");
        KilitAyni(e, Tr(GirdiKodu.TarihGec, g: sinir));
    }

    [Fact]
    public void Tarih_takvim_yerli_secici_ayni_yoldan_ve_dugme_degil()
    {
        var e = Ciz<Tarih, DateOnly?>(new() { ["Tur"] = TarihTuru.Islem });
        var yerli = e.Find("#t-takvim-yerli");
        Assert.Equal("date", yerli.GetAttribute("type"));
        Assert.Equal(Bicim.Makine(DateOnly.FromDateTime(Bicim.IsletmeBugun)), yerli.GetAttribute("max"));
        Assert.Empty(e.FindAll("button"));      // DoD EK md.1: işlevsiz düğme yok
        var gun = DateOnly.FromDateTime(Bicim.IsletmeBugun).AddDays(-3);
        yerli.Change(Bicim.Makine(gun));
        Assert.Equal(TarihPenceresi.Yaz(gun), e.Find("#t").GetAttribute("value"));
        Assert.Equal(gun, son);
    }

    // ── SAYISAL: EnAz/EnCok ARTIK UYGULANIR (envanter açık #4) ──────────────
    [Fact]
    public void Sayi_EnAz_0_eksi_hata_kilit_ve_null()
    {
        var e = Ciz<Sayi, decimal?>(new() { ["EnAz"] = (decimal?)0m });
        e.Find("#t").Input("-5");
        KilitAyni(e, GirdiSozlugu.Tr.SayiEksi);
        Assert.Null(son);                                   // kapılı formda geçersiz değer GİTMEZ
        Assert.Null(e.Find("#t").GetAttribute("data-eksi"));   // "-" tuşu maskede kapalı
        e.Find("#t").Input("5");
        Assert.False(kapi.Var);
        Assert.Equal(5m, son);
    }

    [Fact]
    public void Sayi_EnCok_ve_EnAz_ondalik()
    {
        var e = Ciz<Sayi, decimal?>(new() { ["EnCok"] = (decimal?)100m });
        e.Find("#t").Input("100,5");
        KilitAyni(e, Tr(GirdiKodu.SayiEnCok, 100));
        Assert.Equal("1", e.Find("#t").GetAttribute("data-eksi"));   // alt sınır yok → eksi serbest

        var p = Ciz<Para, decimal?>(new() { ["EnAz"] = (decimal?)0.01m });
        p.Find("#t").Input("0");
        Assert.Equal(Tr(GirdiKodu.SayiEnAz, 0.01m), p.Find("#t-hata").TextContent.Trim());
    }

    [Fact]
    public void Para_Tur_Pozitif_sifir_ve_ondalik_hatasi()
    {
        var e = Ciz<Para, decimal?>(new() { ["Tur"] = (TutarTuru?)TutarTuru.Pozitif });
        e.Find("#t").Input("0");
        KilitAyni(e, GirdiSozlugu.Tr.SayiSifir);
        e.Find("#t").Input("10,005");
        KilitAyni(e, Tr(GirdiKodu.SayiOndalik, 2));
        e.Find("#t").Input("10,05");
        Assert.False(kapi.Var);
    }

    [Fact]
    public void Adet_Milyem_Yuzde_Kur_Has_GunSayisi_Core_kuralini_uygular()
    {
        var a = Ciz<Adet, decimal?>();
        a.Find("#t").Input("0");
        Assert.Equal(Tr(GirdiKodu.SayiEnAz, 1), a.Find("#t-hata").TextContent.Trim());
        Assert.Equal("1", a.Find("#t").GetAttribute("data-tam"));
        Assert.Null(a.Find("#t").GetAttribute("data-eksi"));

        var m = Ciz<Milyem, decimal?>();
        m.Find("#t").Input("1001");
        Assert.Equal(Tr(GirdiKodu.MilyemAralik, 1, 1000), m.Find("#t-hata").TextContent.Trim());

        var y = Ciz<Yuzde, decimal?>();
        y.Find("#t").Input("100,01");
        Assert.Equal(Tr(GirdiKodu.YuzdeAralik, 0, 100), y.Find("#t-hata").TextContent.Trim());

        var k = Ciz<KurAlani, decimal?>();
        k.Find("#t").Input("0");
        Assert.Equal(GirdiSozlugu.Tr.SayiSifir, k.Find("#t-hata").TextContent.Trim());

        var h = Ciz<Has, decimal?>();
        h.Find("#t").Input("0,0000001");
        Assert.Equal(Tr(GirdiKodu.SayiOndalik, 6), h.Find("#t-hata").TextContent.Trim());

        var g = Ciz<GunSayisi, decimal?>(new() { ["EnAz"] = (decimal?)2m, ["EnCok"] = (decimal?)12m });
        g.Find("#t").Input("13");
        Assert.Equal(Tr(GirdiKodu.GunAralik, 2, 12), g.Find("#t-hata").TextContent.Trim());
    }

    // ── TCKN / VKN bileşenleri (KimlikNoDogrulayici'yi sarar) ────────────────
    [Fact]
    public void TcKimlikNo_harf_yazilmaz_11_hanede_algoritma_ve_kilit()
    {
        var e = Ciz<TcKimlikNo, string?>();
        e.Find("#t").Input("1000a0000147999");
        Assert.Equal("10000000147", e.Find("#t").GetAttribute("value"));
        KilitAyni(e, KimlikNoSozlugu.Tr.TcknGecersiz);
        e.Find("#t").Input("10000000146");
        Assert.False(kapi.Var);
        Assert.Equal("10000000146", son);

        var v = Ciz<Vkn, string?>();
        v.Find("#t").Change("12345");
        Assert.Equal(Tr(GirdiKodu.KimlikHane, 10), v.Find("#t-hata").TextContent.Trim());
    }

    // ── METİN: pasaport, barkod, ad, adres ──────────────────────────────────
    [Fact]
    public void Pasaport_Barkod_AdSoyad_Adres_maske_ve_kural()
    {
        var p = Ciz<Pasaport, string?>();
        p.Find("#t").Input("u 12-345 678");
        Assert.Equal("U12345678", p.Find("#t").GetAttribute("value"));

        var b = Ciz<BarkodAlani, string?>();
        b.Find("#t").Input(" kçay 12");
        Assert.Equal("KAY12", b.Find("#t").GetAttribute("value"));
        b.Find("#t").Change("4006381333932");
        Assert.Equal(GirdiSozlugu.Tr.Ean13Kontrol, b.Find("#t-ipucu").TextContent.Trim());   // uyarı: kilit YOK
        Assert.False(kapi.Var);

        var a = Ciz<AdSoyad, string?>();
        a.Find("#t").Input("  Ayşe   Nur");
        Assert.Equal("Ayşe Nur", a.Find("#t").GetAttribute("value"));
        a.Find("#t").Change("12");
        Assert.Equal(GirdiSozlugu.Tr.AdHarf, a.Find("#t-hata").TextContent.Trim());

        var r = Ciz<Adres, string?>();
        r.Find("#t").Change("Ulus");
        Assert.Equal(Tr(GirdiKodu.AdresKisa, 10), r.Find("#t-hata").TextContent.Trim());
    }
}
