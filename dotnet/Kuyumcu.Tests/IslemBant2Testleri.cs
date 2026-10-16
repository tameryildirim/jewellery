using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT-2 (10.10) — ISLEM-BANT kapanış devri (tasarım hakemi bulguları):
//   KRİTİK  eksi/0 has fiyatında alan altı ileti yok, ekranda "-22,90 TL" → Tur=Pozitif + tutar "—".
//   YÜKSEK  hurda alışı TEZGAHTAR'a açık, sunucu 403'ü sonda → rol kümesi tek kaynak (IslemRolleri), Kaydet kilitli + cümle.
//   YÜKSEK  bant dışında Hurda'da Kaydet etkin (Takas'ta pasif) → dört ekranda AYNI kilit (IslemBantGosterim.KaydetKilidi).
//   ORTA    "Has Gram Fiyatı (₺)" ↔ "Has fiyatı (1 gram)" → tek ad, Title Case yok.
//   DÜŞÜK   GET /kullanicilar/ben TEZGAHTAR 403 → her rol 200, yalnız kendi kaydı.
// Sayfa düzeyindeki testler (Islemler) SATIS3EkranTestleri düzeneğini kullanır: IslemBant2EkranTestleri.cs.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Saf: kilit sırası, tutar kuralı, rol kümesi sözleşmesi, sözlük, etiket.</summary>
public sealed class IslemBant2SafTestleri
{
    private static readonly GirdiHatasi Oran = new(GirdiKodu.IslemFiyatBantDisi, 3_413.60m, 7_509.92m);
    private static readonly GirdiHatasi Mutlak = new(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m);
    private const string UzunGerekce = "Bozuk hurda, ayarı düşük çıktı; ustayla teyit ettim.";

    [Fact]
    public void IB2_01_Kaydet_kilidi_sirasi_mutlak_yetki_onay_gerekce()
    {
        var bos = new IslemBantOnayDurumu();
        Assert.Null(IslemBantGosterim.KaydetKilidi(null, bos, "TEZGAHTAR", "tr"));                 // bant içinde kilit yok
        Assert.Equal(GirdiSozlugu.Mesaj(Mutlak, "tr"), IslemBantGosterim.KaydetKilidi(Mutlak, new() { Onay = true, Gerekce = UzunGerekce }, "SAHIP", "tr"));   // onay mutlakı aşamaz
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok, IslemBantGosterim.KaydetKilidi(Oran, new() { Onay = true, Gerekce = UzunGerekce }, "TEZGAHTAR", "tr"));
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok, IslemBantGosterim.KaydetKilidi(Oran, bos, "MUHASEBE", "tr"));
        Assert.Equal(string.Format(IslemBant2Sozlugu.Tr.BantOnayBekliyor, KurBantSozlugu.Tr.OnayEtiket),
            IslemBantGosterim.KaydetKilidi(Oran, bos, "SAHIP", "tr"));
        Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.GerekceKisa, 20m, 4m), "tr"),
            IslemBantGosterim.KaydetKilidi(Oran, new() { Onay = true, Gerekce = "kısa" }, "MUDUR", "tr"));
        Assert.Null(IslemBantGosterim.KaydetKilidi(Oran, new() { Onay = true, Gerekce = UzunGerekce }, "MUDUR", "tr"));
        Assert.Contains("tick", IslemBantGosterim.KaydetKilidi(Oran, bos, "SAHIP", "en"));
    }

    [Theory]
    [InlineData(-5.0, false)]
    [InlineData(0.0, false)]
    [InlineData(0.001, true)]        // mutlak dışı: model/fark hesabına girer (kuruş testleri), kutu altı tutar satırı ÇİZİLMEZ (aşağıda), Kaydet KİLİTLİ
    [InlineData(99_999_999.0, true)]
    [InlineData(2_000.0, true)]      // oran dışı ama onaylanabilir: tutar görünür (SAHİP onaylayıp kaydeder)
    [InlineData(6_800.0, true)]
    public void IB2_02_Fiyat_tutar_gosterimine_girer_mi_eksi_sifir_HAYIR(double fiyat, bool beklenen)
    {
        Assert.Equal(beklenen, IslemBantGosterim.FiyatHesabaGirerAyar((decimal)fiyat, "K22"));
        // Kutu altı tutar satırı: mutlak dışında da çizilmez (oran dışı onaylanabilir fiyatta çizilir).
        Assert.Equal(fiyat is >= 500 and <= 50_000, IslemBantGosterim.TutarGosterilirAyar((decimal)fiyat, "K22"));
        Assert.Equal(fiyat > 0, IslemBantGosterim.TutarGosterilirAyar((decimal)fiyat, "G925"));   // gümüşte bant yok
        // Gümüşte bant yok (ISLEM-BANT § 12/1): yalnız 0/eksi reddedilir.
        Assert.Equal(beklenen, IslemBantGosterim.FiyatHesabaGirerAyar((decimal)fiyat, "G925"));
    }

    [Fact]
    public void IB2_03_Takas_modeli_gecersiz_fiyati_isaretler_hesap_formulu_degismez()
    {
        // Model formülü (sunucuyla aynı) DEĞİŞMEZ — kuruş sınırı testleri absürt fiyatla aritmetik sınar.
        // Gösterim kararı EskiFiyatHesabaGirer'den: ALDIM tutarı, fark ve PRO fark kartı "—".
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 5m, YeniUrunId = "u1", YeniFiyat = 10_000m };
        t.FiyatGirildi(0m);
        Assert.False(t.EskiFiyatHesabaGirer);
        t.FiyatGirildi(-5m);
        Assert.False(t.EskiFiyatHesabaGirer);
        Assert.Null(t.Fark);
        t.FiyatGirildi(3_000m);
        Assert.True(t.EskiFiyatHesabaGirer);
        Assert.NotNull(t.Fark);
    }

    [Theory]
    [InlineData("TEZGAHTAR", true)]
    [InlineData("SAHIP", false)]
    [InlineData("MUDUR", false)]
    [InlineData("MUHASEBE", false)]
    [InlineData(null, false)]   // oturum bilinmiyor: ekran kilit uydurmaz, son söz sunucunun
    public void IB2_04_Hurda_alis_kilidi_sunucu_rol_kumesiyle_ayni(string? rol, bool kilitli) =>
        Assert.Equal(kilitli, IslemBantGosterim.HurdaAlisKilitli(rol));

    /// <summary>TEK KAYNAK sözleşmesi: sunucunun [RolGerekli] özniteliği == IslemRolleri.HurdaAlis (sıra dahil).</summary>
    [Fact]
    public void IB2_05_RolGerekli_hurda_alis_ozniteligi_IslemRolleri_ile_birebir()
    {
        var m = typeof(SatisController).GetMethods().Single(x =>
            x.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpPostAttribute>().Any(a => a.Template == "hurda-alis"));
        var oz = m.CustomAttributes.Single(c => c.AttributeType == typeof(RolGerekliAttribute));
        var roller = ((IReadOnlyCollection<CustomAttributeTypedArgument>)oz.ConstructorArguments[0].Value!)
            .Select(x => (string)x.Value!).ToArray();
        Assert.Equal(IslemRolleri.HurdaAlis.ToArray(), roller);
    }

    [Fact]
    public void IB2_06_Yetki_cumlesi_TR_EN_rol_adlari_sozlukten_ham_kod_yok()
    {
        var tr = IslemBant2Sozlugu.HurdaAlisYetkiMetni("tr");
        var en = IslemBant2Sozlugu.HurdaAlisYetkiMetni("en");
        foreach (var kod in IslemRolleri.HurdaAlis)
        {
            Assert.Contains(KesifP1Sozlugu.RolAdi(kod, "tr"), tr);
            Assert.Contains(KesifP1Sozlugu.RolAdi(kod, "en"), en);
            Assert.DoesNotContain(kod, tr);
        }
        Assert.NotEqual(tr, en);
    }

    [Fact]
    public void IB2_07_Has_fiyati_etiketi_tek_ad_Title_Case_ve_birim_eki_yok()
    {
        var tr = Sozlukler.Getir("tr").Islem.HasGramFiyati;
        Assert.Equal(DegisimGunSonuSozlugu.Al("tr").EskiHasFiyati, tr);   // BASİT Değişim ile aynı ad
        Assert.Equal("Has fiyatı (1 gram)", tr);
        Assert.Equal("Fine gold price (1 gram)", Sozlukler.Getir("en").Islem.HasGramFiyati);
        Assert.DoesNotContain("₺", tr);
    }
}

/// <summary>bUnit: bileşen düzeyi (FisHurdaAlis · FisTakas · FisTakasTuval · AltinBacagiPaneli).</summary>
public sealed class IslemBant2BilesenTestleri : BunitContext
{
    public IslemBant2BilesenTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new Bos()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.GetRequiredService<CanliKurServisi>().Uygula(new CanliKurServisi.KurAnlik("kur-1", 6_827.20m, null, null, null, null));
    }

    private sealed class Bos : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });
    }

    private void Rol(string rol) => Services.GetRequiredService<OturumServisi>().KullaniciAta(
        new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    private static string Eksi => GirdiSozlugu.Mesaj(TutarKurali.Denetle(-5m, TutarTuru.Pozitif)!.Value, "tr");
    private static string Sifir => GirdiSozlugu.Mesaj(TutarKurali.Denetle(0m, TutarTuru.Pozitif)!.Value, "tr");

    [Theory]
    [InlineData("-5")]
    [InlineData("0")]
    public void IB2_10_Hurda_fiyati_eksi_sifir_alan_alti_ileti_deger_gitmez_tutar_yok(string metin)
    {
        Rol("SAHIP");
        var hurda = new HurdaGirisi { Ayar = "K22", Gram = 5m };
        hurda.FiyatGirildi(6_000m);
        var e = Render<FisHurdaAlis>(p => p.Add(x => x.Hurda, hurda).Add(x => x.Ayarlar, new[] { "K22" }));
        Assert.NotEqual("—", e.Find("#hurda-hesap-tutar").TextContent.Trim());
        e.Find("#hurda-fiyat").Input(metin);
        Assert.Contains(metin == "0" ? Sifir : Eksi, e.Markup);
        Assert.Null(hurda.Fiyat);                                         // görünen ≠ giden olmaz
        Assert.Equal("true", e.Find("#hurda-fiyat").GetAttribute("aria-invalid"));
        e.Render(p => p.Add(x => x.Hurda, hurda));
        Assert.Equal("—", e.Find("#hurda-hesap-tutar").TextContent.Trim());
        Assert.Null(e.Find("#hurda-fiyat").GetAttribute("data-eksi"));   // maske "-" tuşunu yazdırmaz
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("0")]
    [InlineData("5")]   // tarayıcıda "-5" maskeden "5" olarak geçer: mutlak sınır cümlesi + tutar yine YOK
    public void IB2_11_Takas_eski_fiyat_eksi_sifir_alan_alti_ileti_ALDIM_tutari_yok(string metin)
    {
        var takas = new TakasGirisi { EskiAyar = "K22", EskiGram = 5m };
        takas.FiyatGirildi(6_000m);
        var e = Render<FisTakasTuval>(p => p.Add(x => x.Takas, takas)
            .Add(x => x.Aldim, (RenderFragment)(b =>
            {
                b.OpenComponent<FisTakas>(0);
                b.AddAttribute(1, nameof(FisTakas.Takas), takas);
                b.AddAttribute(2, nameof(FisTakas.Ayarlar), new[] { "K22" });
                b.AddAttribute(3, nameof(FisTakas.UrunSecenekleri), Array.Empty<Kuyumcu.Web.Bilesenler.SecenekOgesi>());
                b.AddAttribute(4, nameof(FisTakas.Bolum), "ESKI");
                b.AddAttribute(5, nameof(FisTakas.Sade), true);
                b.CloseComponent();
            })));
        Assert.NotEqual("—", e.Find("#takas-aldim-tutar").TextContent.Trim());
        e.Find("#takas-eski-fiyat").Input(metin);
        Assert.Contains(metin switch { "0" => Sifir, "5" => GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.FiyatMutlakDisi, 500m, 50_000m), "tr"), _ => Eksi }, e.Markup);
        if (metin != "5") Assert.Null(takas.EskiFiyat);
        e.Render(p => p.Add(x => x.Takas, takas));
        Assert.Equal("—", e.Find("#takas-aldim-tutar").TextContent.Trim());
        Assert.DoesNotContain("-22,90", e.Markup);
        Assert.Equal("Has fiyatı (1 gram)", e.Find("label[for=takas-eski-fiyat]").TextContent.Replace(e.Find("#takas-eski-fiyat-kaynak").TextContent, "").Trim().TrimEnd('*').Trim());
    }

    [Fact]
    public void IB2_12_Eski_altin_paneli_eksi_fiyat_isaretli_tutar_yok_bant_kilidi_ilk_eksik()
    {
        Rol("TEZGAHTAR");
        AltinBacagiDurumu? son = null;
        var giris = new AltinBacagiGirisi { AyarId = "K22", Gram = 5m, Fiyat = 2_000m, FiyatElle = true };
        var e = Render<Kuyumcu.Web.Bilesenler.AltinBacagiPaneli>(p => p.Add(x => x.Giris, giris).Add(x => x.HasFiyati, 6_827.20m)
            .Add(x => x.Degisti, (AltinBacagiDurumu d) => son = d));
        e.Find("#altin-gram").Input("5");   // bildirim tetikle
        Assert.NotNull(son);
        Assert.False(son!.Kaydedilebilir);
        Assert.Equal(KurBantSozlugu.Tr.YetkiYok, son.Eksikler[0]);   // oran dışı + TEZGAHTAR → yetki cümlesi İLK

        e.Find("#altin-fiyat").Input("-5");
        Assert.Equal("true", e.Find("#altin-fiyat").GetAttribute("aria-invalid"));
        Assert.Null(son.Deger);
        Assert.False(son.Kaydedilebilir);
    }

    [Fact]
    public void IB2_13_Hurda_alis_TEZGAHTAR_formun_basinda_yetki_cumlesi_MUHASEBE_gormez()
    {
        Rol("TEZGAHTAR");
        var hurda = new HurdaGirisi { Ayar = "K22" };
        var e = Render<FisHurdaAlis>(p => p.Add(x => x.Hurda, hurda).Add(x => x.Ayarlar, new[] { "K22" }));
        Assert.Equal(IslemBant2Sozlugu.HurdaAlisYetkiMetni("tr"), e.Find("#hurda-yetki-yok").TextContent.Trim());
        Assert.Equal("Has fiyatı (1 gram)", e.Find("label[for=hurda-fiyat]").TextContent.Replace(e.Find("#hurda-fiyat-kaynak").TextContent, "").Trim().TrimEnd('*').Trim());

        Rol("MUHASEBE");
        e.Render(p => p.Add(x => x.Hurda, hurda));
        Assert.Empty(e.FindAll("#hurda-yetki-yok"));
    }
}

/// <summary>HTTP (gerçek MySQL): GET /kullanicilar/ben her rol için 200 ve YALNIZ kendi kaydı.</summary>
[Collection("http")]
public sealed class IslemBant2HttpTestleri(HttpFikstur f)
{
    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    [InlineData("MUHASEBE")]
    [InlineData("TEZGAHTAR")]
    public async Task IB2_20_GET_ben_her_rol_200_yalniz_kendi_kaydi_parola_ozeti_yok(string rol)
    {
        Kiraci k;
        var kendiId = Kimlik.Ulid();
        using (var db = f.Vt.OlusturSistem())
        {
            k = TestKurulum.KiraciKur(db, "PAKETISLEM-BANT-2-TEST ben " + rol);
            var now = DateTime.UtcNow;
            db.Kullanicilar.Add(new Kullanici
            {
                Id = kendiId, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Kendi", Soyad = rol,
                Email = $"{kendiId}@test.local", SifreHash = "gizli-ozet", Rol = rol, Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
            db.SaveChanges();
        }
        var c = f.Ortam.Istemci(kendiId, k.TenantId, k.SubeId, rol);
        var r = await c.GetAsync("/api/v1/kullanicilar/ben");
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode} {govde}");
        var j = JsonDocument.Parse(govde).RootElement;
        Assert.Equal(kendiId, j.GetProperty("id").GetString());                 // sahibin satırı DEĞİL, jetondaki kişi
        Assert.NotEqual(k.KullaniciId, j.GetProperty("id").GetString());
        Assert.Equal(rol, j.GetProperty("rol").GetString());
        Assert.DoesNotContain("gizli-ozet", govde);
        Assert.False(j.TryGetProperty("sifreHash", out _));

        // Başkasının satırı hâlâ kapalı (yetki genişlemesi yok): /kullanicilar/{id} SAHİP/MÜDÜR dışına 403.
        if (rol is "TEZGAHTAR" or "MUHASEBE")
        {
            var baska = await c.GetAsync($"/api/v1/kullanicilar/{k.KullaniciId}");
            Assert.Equal(HttpStatusCode.Forbidden, baska.StatusCode);
        }
    }

    [Fact]
    public async Task IB2_21_GET_ben_anonim_401()
    {
        var r = await f.Ortam.CreateClient().GetAsync("/api/v1/kullanicilar/ben");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
