using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3c — FORM VE ETKİLEŞİM SAĞLAMLIĞI (kurul tur 3 engel 20 · QA turu 3)
//
// Kapatılan borçlar:
//   1. /fiyatlandirma — eksi marj / %500 işçilik UYARISIZ kabul ediliyordu.
//      Artık: rahat aralık dışı değer ekranda uyarı + onay kutusu ister,
//      onaysız Kaydet KİLİTLİ ve istek ATILMAZ (UX-S30 · sessiz kabul yasak).
//   2. Satır içi düzenleme — odak kaybında değişiklik SESSİZCE kayboluyordu.
//      Artık: blur KAYDEDER; değişmemişse sessizce kapanır; Kaydet/İptal
//      düğmesine tıklarken ÇİFT gönderim olmaz (mousedown bayrağı).
//   3. /sayim — "Enter kaydetmiyor" bulgusunun SAYFA düzeyinde regresyonu.
//   4. ModalPencere — her açılışta gövde durumu sıfırlanır (@key + Acildi).
//
// ⚠ bUnit CSS uygulamaz: 375 px kanıtı burada DEĞİL, canlı turdadır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S3cFormSaglamligiTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public S3cFormSaglamligiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    // ── Sahte API — '{}' varsayılanı BIRAKILMADI ─────────────────────────────
    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                var v when v.Contains("/onizleme") => """{"toplam":0,"limit":50,"offset":0,"satirlar":[]}""",
                var v when v.Contains("/fiyat/listeler") && istek.Method == HttpMethod.Get =>
                    """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","hasMarjYuzde":5,"iscilikCarpanYuzde":100,"varsayilan":true,"aktif":true}]""",
                var v when v.Contains("/fiyat/kampanyalar") && istek.Method == HttpMethod.Get => "[]",
                var v when v.Contains("/urun-kategorileri/duz") => """[{"id":"kat1","ad":"Bilezik","ustId":null}]""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"A22","hasGrami":9.16}]""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez Şube"}]""",
                var v when v.Contains("/sayim/") =>
                    """{"id":"o1","ad":"Eylül sayımı","subeId":"s1","kapsamTur":"TUMU","durum":"ACIK","toplamSatir":1,"sayilan":0,"sayilmayan":1,"farkli":0,"eksikAdet":0,"fazlaAdet":0,"netFarkHas":0,"hareketli":0,"satirlar":[{"urunId":"u2","kod":"KL-1","ad":"Kolye KL1","barkod":null,"birim":"ADET","sistemAdet":1,"sayilanAdet":null,"guncelAdet":1,"fark":null,"oturumdaHareketVar":false}]}""",
                var v when v.Contains("/sayim") && istek.Method == HttpMethod.Get =>
                    """[{"id":"o1","ad":"Eylül sayımı","kapsamTur":"TUMU","durum":"ACIK"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null ? v.ToString() : null;
    }

    // ═══ 1. /fiyatlandirma — makul aralık kapısı ════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Fiyatlandirma> FiyatFormu()
    {
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>().AyrintiAc();
        e.WaitForElement("#fl-tablo tbody tr");
        e.Find("#fl-duzenle-fl1").Click();
        e.WaitForElement("#fl-form");
        istekler.Clear();
        return e;
    }

    /// <summary>S3c-1 — eksi marj artık SESSİZCE kabul edilmez: uyarı + kilitli Kaydet + istek yok.</summary>
    [Fact]
    public void Fiyat_EKSI_MARJ_uyari_cikarir_KAYDET_kilitlenir_ve_ISTEK_ATILMAZ()
    {
        var e = FiyatFormu();
        e.Find("#fl-marj").Input("-5");

        Assert.False(string.IsNullOrWhiteSpace(e.Find("#fl-risk-uyari").TextContent));
        Assert.True(e.Find("#fl-kaydet").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#fl-risk-not").TextContent));

        e.Find("#fl-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot is "POST" or "PATCH");
    }

    /// <summary>S3c-2 — %500 işçilik çarpanı da riskli sayılır (rahat üst sınır %200).</summary>
    [Fact]
    public void Fiyat_YUKSEK_ISCILIK_CARPANI_uyari_cikarir()
    {
        var e = FiyatFormu();
        e.Find("#fl-carpan").Input("500");

        Assert.False(string.IsNullOrWhiteSpace(e.Find("#fl-risk-uyari").TextContent));
        Assert.True(e.Find("#fl-kaydet").HasAttribute("disabled"));
    }

    /// <summary>S3c-3 — bilinçli onay verilince kayıt gider ve gövdede riskOnayi=true taşınır.</summary>
    [Fact]
    public void Fiyat_RISK_ONAYI_isaretlenince_PATCH_gider_ve_govdede_riskOnayi_true_olur()
    {
        var e = FiyatFormu();
        e.Find("#fl-marj").Input("-5");
        e.Find("#fl-risk-onay").Change(true);

        Assert.False(e.Find("#fl-kaydet").HasAttribute("disabled"));
        e.Find("#fl-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/fiyat/listeler/fl1"));
            Assert.Equal("-5", Alan(istek.Govde, "hasMarjYuzde"));
            Assert.Equal("True", Alan(istek.Govde, "riskOnayi"));
        });
    }

    /// <summary>S3c-4 — sert sınır dışı değer onayla BİLE geçmez, alan altı mesaj verir.</summary>
    [Fact]
    public void Fiyat_SERT_SINIR_disi_deger_ONAYLA_BILE_gonderilmez()
    {
        var e = FiyatFormu();
        e.Find("#fl-marj").Input("-500");
        e.Find("#fl-risk-onay").Change(true);
        e.Find("#fl-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot is "POST" or "PATCH");
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#alan-hata-fl-marj").TextContent));
    }

    /// <summary>S3c-5 — rahat aralıktaki değer eskisi gibi tek tıkla kaydedilir (kapı fazla kapatmıyor).</summary>
    [Fact]
    public void Fiyat_MAKUL_deger_uyari_CIKARMAZ_ve_dogrudan_kaydedilir()
    {
        var e = FiyatFormu();
        e.Find("#fl-marj").Input("12");

        Assert.Empty(e.FindAll("#fl-risk-uyari"));
        e.Find("#fl-kaydet").Click();

        e.WaitForAssertion(() => Assert.Single(istekler, i => i.Metot == "PATCH"));
    }

    // ═══ 2. SatirIciDuzenle — odak kaybı ════════════════════════════════════

    private IRenderedComponent<SatirIciDuzenle> SatirCiz(Action<string>? kaydedildi = null, string? deger = "Ahmet")
        => Render<SatirIciDuzenle>(p => p
            .Add(x => x.Varlik, "cari").Add(x => x.Alan, "aciklama").Add(x => x.Id, "42")
            .Add(x => x.Etiket, "Açıklama").Add(x => x.Deger, deger)
            .Add(x => x.Kaydedildi, EventCallback.Factory.Create<string>(this, v => kaydedildi?.Invoke(v))));

    /// <summary>S3c-6 — odak kaybı KAYDEDER (QA turu 3: değişiklik sessizce kayboluyordu).</summary>
    [Fact]
    public void SatirIci_ODAK_KAYBINDA_kaydeder()
    {
        var sayac = 0; string? yazilan = null;
        var e = SatirCiz(v => { yazilan = v; sayac++; });

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("Mehmet");
        e.Find("#si-cari-aciklama-42").Blur();

        Assert.Equal("Mehmet", yazilan);
        Assert.Equal(1, sayac);
    }

    /// <summary>S3c-7 — değer değişmediyse blur istek ATMAZ (gereksiz yazma yok).</summary>
    [Fact]
    public void SatirIci_DEGISMEYEN_degerde_odak_kaybi_istek_ATMAZ()
    {
        var sayac = 0;
        var e = SatirCiz(_ => sayac++);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Blur();

        Assert.Equal(0, sayac);
        Assert.NotNull(e.Find("#si-cari-aciklama-42-ac"));   // düzenleme kapandı
    }

    /// <summary>S3c-8 — Kaydet düğmesine tıklarken blur da olur: ÇİFT gönderim OLMAZ.</summary>
    [Fact]
    public void SatirIci_KAYDET_dugmesinde_blur_ile_CIFT_gonderim_olmaz()
    {
        var sayac = 0;
        var e = SatirCiz(_ => sayac++);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("Mehmet");
        // Gerçek tarayıcı sırası: düğmede mousedown → girdide blur → düğmede click
        e.Find("#si-cari-aciklama-42-kaydet").MouseDown();
        e.Find("#si-cari-aciklama-42").Blur();
        e.Find("#si-cari-aciklama-42-kaydet").Click();

        Assert.Equal(1, sayac);
    }

    /// <summary>S3c-9 — İptal düğmesine tıklarken blur KAYDETMEZ (iptal iptaldir).</summary>
    [Fact]
    public void SatirIci_IPTAL_dugmesinde_blur_KAYDETMEZ()
    {
        var sayac = 0;
        var e = SatirCiz(_ => sayac++);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("Mehmet");
        e.Find("#si-cari-aciklama-42-iptal").MouseDown();
        e.Find("#si-cari-aciklama-42").Blur();
        e.Find("#si-cari-aciklama-42-iptal").Click();

        Assert.Equal(0, sayac);
        Assert.Equal("Ahmet", e.Find("#si-cari-aciklama-42-ac").TextContent);
    }

    /// <summary>S3c-10 — geçersiz (boş) değerde blur SESSİZ kapanmaz: alan altı mesaj + açık kalır.</summary>
    [Fact]
    public void SatirIci_GECERSIZ_degerde_odak_kaybi_SESSIZ_kalmaz()
    {
        var sayac = 0;
        var e = SatirCiz(_ => sayac++);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("   ");
        e.Find("#si-cari-aciklama-42").Blur();

        Assert.Equal(0, sayac);
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#si-cari-aciklama-42-hata").TextContent));
    }

    // ═══ 3. /sayim — Enter ve blur SAYFA düzeyinde POST'a döner ═════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Sayim> SayimOturumu()
    {
        var e = Render<Kuyumcu.Web.Pages.Sayim>().SayimOzgunListe();
        e.WaitForElement("#sy-ac-o1");
        e.Find("#sy-ac-o1").Click();
        e.WaitForElement("#sy-satirlar tbody tr");
        istekler.Clear();
        return e;
    }

    /// <summary>S3c-11 — QA turu 3 bulgusu: satır içi "Sayılan"da ENTER gerçekten POST atar.</summary>
    [Fact]
    public void Sayim_satir_ici_ENTER_ile_POST_atilir()
    {
        var e = SayimOturumu();
        e.Find("#si-sayim-sayilan-u2-ac").Click();
        e.WaitForElement("#si-sayim-sayilan-u2");
        e.Find("#si-sayim-sayilan-u2").Input("5");
        e.Find("#si-sayim-sayilan-u2").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/sayim/o1/say"));
            Assert.Equal("u2", Alan(istek.Govde, "urunId"));
            Assert.Equal("5", Alan(istek.Govde, "sayilanAdet"));
        });
    }

    /// <summary>S3c-12 — aynı hücrede odak kaybı da POST atar (sessiz kayıp yok).</summary>
    [Fact]
    public void Sayim_satir_ici_ODAK_KAYBINDA_POST_atilir()
    {
        var e = SayimOturumu();
        e.Find("#si-sayim-sayilan-u2-ac").Click();
        e.WaitForElement("#si-sayim-sayilan-u2");
        e.Find("#si-sayim-sayilan-u2").Input("5");
        e.Find("#si-sayim-sayilan-u2").Blur();

        e.WaitForAssertion(() =>
            Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/sayim/o1/say")));
    }

    // ═══ 4. ModalPencere — her açılışta durum sıfırlanır ═══════════════════

    /// <summary>S3c-13 — modal KAPANIP AÇILINCA gövdesi yeniden kurulur ve Acildi çağrılır.</summary>
    [Fact]
    public void Modal_her_acilista_GOVDE_yeniden_kurulur_ve_Acildi_cagrilir()
    {
        var acilis = 0;
        var e = Render<ModalPencere>(p => p
            .Add(x => x.Acik, true)
            .Add(x => x.Baslik, "Kasa hareketi")
            .Add(x => x.Acildi, EventCallback.Factory.Create(this, () => acilis++))
            .AddChildContent<SatirIciDuzenle>(c => c
                .Add(x => x.Varlik, "kasa").Add(x => x.Alan, "aciklama").Add(x => x.Id, "1")
                .Add(x => x.Etiket, "Açıklama").Add(x => x.Deger, "Gider")));

        Assert.Equal(1, acilis);
        // İçeride durum bırak: düzenleme kutusu AÇIK kalsın.
        e.Find("#si-kasa-aciklama-1-ac").Click();
        Assert.NotNull(e.Find("#si-kasa-aciklama-1"));

        e.Render(p => p.Add(x => x.Acik, false));
        e.Render(p => p.Add(x => x.Acik, true));

        Assert.Equal(2, acilis);
        // Önceki açılışın durumu TAŞINMAZ: kutu yeniden kapalı.
        Assert.Empty(e.FindAll("#si-kasa-aciklama-1"));
        Assert.NotNull(e.Find("#si-kasa-aciklama-1-ac"));
    }
}
