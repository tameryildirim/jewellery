using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİYATLANDIRMA EKRANI — paket UX-4a (madde 2-5)
//
// Sınananlar:
//   2 — Düzenle yolu: #fl-duzenle-{id} formu doldurur, Kaydet PATCH'e gider;
//       varsayılan listede "Pasife al" KAPALI ve sebebi yazılı.
//   3 — Liste satırından ürün paneli açılır, /fiyat/teklif listeId ile çağrılır;
//       gümüş üründe sebep rozeti çıkar (istek bile atılmaz).
//   4 — ISCILIK_YUZDE bilgi kutusu; gümüş hedefte Kaydet KİLİTLİ.
//   5 — ilk listede Varsayılan işaretli+kapalı; kayıt sonrası başarı bandı;
//       boş listede boş durum; SUNUCU HATASI "kayıt yok"a çevrilmez (UX-S28).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class FiyatlandirmaUX4aEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi sahte = new();

    public FiyatlandirmaUX4aEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        sahte.Kayit = istekler;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(sahte) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        public List<(string, string, string)> Kayit = [];
        /// <summary>Listeler boş dönsün (ilk liste senaryosu).</summary>
        public bool BosListe;
        /// <summary>GET /fiyat/listeler 500 dönsün (UX-S28 senaryosu).</summary>
        public bool SunucuHatasi;
        /// <summary>Ürünlerde gümüş kayıt da bulunsun.</summary>
        public bool GumusUrun;

        private const string Listeler =
            """
            [{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","hasMarjYuzde":5,"iscilikCarpanYuzde":100,"varsayilan":true,"aktif":true},
             {"id":"fl2","ad":"Toptan","tur":"TOPTAN","hasMarjYuzde":2,"iscilikCarpanYuzde":50,"varsayilan":false,"aktif":true}]
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            Kayit.Add((istek.Method.Method, yol, govde));

            if (SunucuHatasi && yol.Contains("/fiyat/listeler") && istek.Method == HttpMethod.Get)
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"mesaj":"Sunucu hatası"}""", System.Text.Encoding.UTF8, "application/json"),
                };

            var urunler = GumusUrun
                ? """[{"id":"u1","ad":"Burma Bilezik","ayarId":"A22","hasGrami":9.16},{"id":"u2","ad":"Gümüş Kolye","ayarId":"G925","hasGrami":0}]"""
                : """[{"id":"u1","ad":"Burma Bilezik","ayarId":"A22","hasGrami":9.16}]""";

            // ⚠ SIRA ÖNEMLİ: "/fiyat/listeler/{id}/onizleme" yolu "/fiyat/listeler"
            // dalına da uyar; önizleme dalı ÖNCE gelmeli (paket 49).
            // ⚠ SIRA ÖNEMLİ: "/fiyat/listeler/{id}/onizleme" yolu "/fiyat/listeler"
            // dalına da uyar; önizleme dalı ÖNCE gelmeli (paket 49).
            var onizleme = GumusUrun
                ? """{"toplam":2,"limit":50,"offset":0,"satirlar":[{"urunId":"u1","kod":"K-1","ad":"Burma Bilezik","ayarId":"A22","gram":10,"hasGrami":9.16,"iscilikTl":566.2,"teklifVar":true,"sebep":null,"hasDegeri":37556,"marjTl":1877.8,"iscilikTlUygulanan":566.2,"listeFiyati":40000,"indirimTutari":0,"satisFiyati":40000,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null},{"urunId":"u2","kod":"K-2","ad":"Gümüş Kolye","ayarId":"G925","gram":12,"hasGrami":0,"iscilikTl":0,"teklifVar":false,"sebep":"GUMUS","hasDegeri":0,"marjTl":0,"iscilikTlUygulanan":0,"listeFiyati":0,"indirimTutari":0,"satisFiyati":0,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null}]}"""
                : """{"toplam":1,"limit":50,"offset":0,"satirlar":[{"urunId":"u1","kod":"K-1","ad":"Burma Bilezik","ayarId":"A22","gram":10,"hasGrami":9.16,"iscilikTl":566.2,"teklifVar":true,"sebep":null,"hasDegeri":37556,"marjTl":1877.8,"iscilikTlUygulanan":566.2,"listeFiyati":40000,"indirimTutari":0,"satisFiyati":40000,"kampanyaId":null,"kaynak":"LISTE","gecersizKilma":null}]}""";

            var y = yol switch
            {
                var v when v.Contains("/onizleme") => onizleme,
                var v when v.Contains("/fiyat/teklif") =>
                    """{"teklifVar":true,"listeFiyati":40000,"hasDegeri":37556,"marjTl":1877.8,"iscilikTl":566.2,"indirimTutari":0,"satisFiyati":40000,"sabitFiyat":false}""",
                var v when v.Contains("/fiyat/listeler") && istek.Method == HttpMethod.Get =>
                    BosListe ? "[]" : Listeler,
                var v when v.Contains("/fiyat/kampanyalar") && istek.Method == HttpMethod.Get =>
                    """[{"id":"km1","ad":"Eylül","baslangic":"2026-09-01","bitis":"2026-09-20","hedefTur":"TUMU","hedefId":null,"indirimTuru":"TUTAR_YUZDE","deger":10,"aktif":true}]""",
                var v when v.Contains("/urun-kategorileri/duz") => """[{"id":"kat1","ad":"Bilezik","ustId":null}]""",
                var v when v.Contains("/urunler") => urunler,
                var v when v.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
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

    private IRenderedComponent<Kuyumcu.Web.Pages.Fiyatlandirma> Ciz(string beklenen = "#fl-tablo tbody tr")
    {
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>();
        e.WaitForElement(beklenen);
        istekler.Clear();
        return e;
    }

    // ── Madde 2 — düzenleme yolu ─────────────────────────────────────────────

    [Fact]
    public void Duzenle_formu_MEVCUT_degerlerle_dolar_ve_PATCH_e_gider()
    {
        var e = Ciz();
        e.Find("#fl-duzenle-fl2").Click();
        e.WaitForElement("#fl-form");

        Assert.Equal("Toptan", e.Find("#fl-ad").GetAttribute("value"));
        Assert.Equal("TOPTAN", e.Find("#fl-tur").Deger());
        Assert.Equal("2", e.Find("#fl-marj").GetAttribute("value"));
        Assert.Equal("50", e.Find("#fl-carpan").GetAttribute("value"));

        e.Find("#fl-marj").Input("3.5");
        e.Find("#fl-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/fiyat/listeler/fl2"));
            Assert.Equal("Toptan", Alan(istek.Govde, "ad"));
            Assert.Equal("3.5", Alan(istek.Govde, "hasMarjYuzde"));
            Assert.Equal("False", Alan(istek.Govde, "varsayilan"));
            Assert.Equal("True", Alan(istek.Govde, "aktif"));
        });
    }

    [Fact]
    public void Varsayilan_listede_PASIFE_AL_kapali_ve_sebebi_yazili()
    {
        var e = Ciz();
        Assert.True(e.Find("#fl-durum-fl1").HasAttribute("disabled"));     // varsayılan liste
        Assert.False(e.Find("#fl-durum-fl2").HasAttribute("disabled"));
        Assert.Contains("Varsayılan", e.Find("#fl-kilit-fl1").TextContent);
        Assert.Empty(e.FindAll("#fl-kilit-fl2"));
    }

    [Fact]
    public void Pasife_al_TAM_govdeyle_PATCH_atar()
    {
        var e = Ciz();
        e.Find("#fl-durum-fl2").Click();
        // PAKET P-65 (E49): pasife alma ORTAK ONAY KİPİNDEN geçer.
        e.Find("#fl-pasif-onayla").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/fiyat/listeler/fl2"));
            Assert.Equal("False", Alan(istek.Govde, "aktif"));
            Assert.Equal("Toptan", Alan(istek.Govde, "ad"));
            Assert.Equal("2", Alan(istek.Govde, "hasMarjYuzde"));       // marj SIFIRLANMIYOR
        });
    }

    [Fact]
    public void Kampanya_duzenleme_PATCH_e_gider_ve_aktif_tasir()
    {
        var e = Ciz();
        e.Find("#km-duzenle-km1").Click();
        e.WaitForElement("#km-form");
        Assert.Equal("Eylül", e.Find("#km-ad").GetAttribute("value"));
        // GIRDI-AILESI-2 (kurucu 08.10): ortak <Tarih> metin + gg.aa.yyyy maskesi; yerli seçici takvim simgesinde (#<id>-takvim-yerli).
        Assert.Equal("01.09.2026", e.Find("#km-bas").GetAttribute("value"));

        e.Find("#km-deger").Input("12");
        e.Find("#km-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH" && i.Yol.EndsWith("/fiyat/kampanyalar/km1"));
            Assert.Equal("12", Alan(istek.Govde, "deger"));
            Assert.Equal("True", Alan(istek.Govde, "aktif"));
        });
    }

    // ── Madde 3 — ürün paneli ────────────────────────────────────────────────

    /// <summary>
    /// Madde 3 (paket 49 ile güncellendi): panel ürün başına /fiyat/teklif çağırmaz;
    /// TEK önizleme ucunu seçilen listeyle çağırır (iki mekanizma bırakılmadı).
    /// </summary>
    [Fact]
    public void Liste_secilince_urun_paneli_acilir_ve_ONIZLEME_ucu_listeyle_cagrilir()
    {
        sahte.GumusUrun = true;
        var e = Ciz();
        e.Find("#fl-goster-fl2").Click();
        e.WaitForElement("#flp-tablo");

        var onizlemeler = istekler.Where(i => i.Yol.Contains("/onizleme")).ToList();
        Assert.Single(onizlemeler);                                      // ürün başına istek YOK
        Assert.Contains("/fiyat/listeler/fl2/onizleme", onizlemeler[0].Yol);
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/fiyat/teklif"));
        // KESIF-P1 (#10): tutar tr-TR + ₺ ("40,000" en-US biçimiydi).
        Assert.Contains("40.000\u00a0₺", e.Find("#flp-satir-u1").TextContent);
        Assert.DoesNotContain("40,000", e.Find("#flp-satir-u1").TextContent);
        Assert.Contains("Gümüş", e.Find("#flp-rozet-u2").TextContent);

        e.Find("#flp-kapat").Click();
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#flp-panel")));
    }

    /// <summary>
    /// Paket 49: eski istemci-tarafı "toplu marj önizlemesi" kutusu KALDIRILDI
    /// (aritmetiği ekranda tekrarlıyordu). Yerine sunucu önizlemesi + onay modalı
    /// geldi; marj girmek TEK BAŞINA hiçbir istek atmaz ve hiçbir şey yazmaz.
    /// </summary>
    [Fact]
    public void Toplu_marj_girmek_TEK_BASINA_istek_atmaz()
    {
        var e = Ciz();
        e.Find("#fl-goster-fl2").Click();
        e.WaitForElement("#flp-tablo");
        istekler.Clear();

        e.Find("#flp-toplu-marj").Input("10");

        Assert.Empty(istekler);                                          // hiçbir yazma/okuma yok
        Assert.True(e.Find("#flp-toplu").HasAttribute("disabled"));      // ürün seçilmeden açılmaz
    }

    // ── Madde 4 — sessiz etkisizlik uyarıları ────────────────────────────────

    [Fact]
    public void ISCILIK_YUZDE_secilince_bilgi_kutusu_gorunur()
    {
        var e = Ciz();
        e.Find("#km-yeni").Click();
        e.WaitForElement("#km-form");
        Assert.Contains("işçilik indirimi uygulanmaz", e.Find("#km-iscilik-uyari").TextContent);

        SeciciYardim.Sec(e, "km-tur", "TUTAR_YUZDE");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#km-iscilik-uyari")));
    }

    [Fact]
    public void Gumus_hedefli_kampanyada_KAYDET_kilitli_ve_uyari_var()
    {
        sahte.GumusUrun = true;
        var e = Ciz();
        e.Find("#km-yeni").Click();
        e.WaitForElement("#km-form");
        e.Find("#km-ad").Input("Gümüş günleri");
        e.Find("#km-deger").Input("10");
        SeciciYardim.Sec(e, "km-hedef", "URUN");
        e.WaitForElement("#km-hedefid");

        SeciciYardim.Sec(e, "km-hedefid", "u2");                               // gümüş ürün
        e.WaitForAssertion(() =>
        {
            Assert.True(e.Find("#km-kaydet").HasAttribute("disabled"));
            Assert.Contains("Gümüş ürünler altın has kuruyla otomatik fiyatlanmaz", e.Find("#km-gumus-uyari").TextContent);
        });

        SeciciYardim.Sec(e, "km-hedefid", "u1");                               // altın ürün
        e.WaitForAssertion(() =>
        {
            Assert.False(e.Find("#km-kaydet").HasAttribute("disabled"));
            Assert.Empty(e.FindAll("#km-gumus-uyari"));
        });
    }

    // ── Madde 5 — geri bildirim, ilk liste, hata yutmama ─────────────────────

    [Fact]
    public void Kayit_sonrasi_BASARI_bandi_cikar()
    {
        var e = Ciz();
        e.Find("#fl-duzenle-fl2").Click();
        e.WaitForElement("#fl-form");
        e.Find("#fl-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains("kaydedildi", e.Find("#fl-basari .bant-basari").TextContent));
    }

    [Fact]
    public void Hic_liste_yokken_VARSAYILAN_isaretli_ve_KAPALI_bos_durum_var()
    {
        sahte.BosListe = true;
        var e = Ciz("#fl-bos");

        Assert.Contains("POS otomatik fiyat veremez", e.Find("#fl-bos").TextContent);

        e.Find("#fl-yeni").Click();
        e.WaitForElement("#fl-form");
        var kutu = e.Find("#fl-varsayilan");
        Assert.True(kutu.HasAttribute("disabled"));
        Assert.True(((AngleSharp.Html.Dom.IHtmlInputElement)kutu).IsChecked);
        Assert.Contains("İlk liste", e.Find("#fl-varsayilan-not").TextContent);

        e.Find("#fl-ad").Input("İlk liste");
        e.Find("#fl-kaydet").Click();
        e.WaitForAssertion(() =>
            Assert.Equal("True", Alan(Assert.Single(istekler, i => i.Metot == "POST").Govde, "varsayilan")));
    }

    /// <summary>UX-S28 / Anayasa md.1 — sunucu hatası "kayıt yok" gibi gösterilmez.</summary>
    [Fact]
    public void Sunucu_hatasinda_HATA_bandi_cikar_BOS_DURUM_cikmaz()
    {
        sahte.SunucuHatasi = true;
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>();

        e.WaitForAssertion(() =>
        {
            Assert.NotEmpty(e.FindAll(".bant-hata"));
            Assert.Empty(e.FindAll("#fl-bos"));
            Assert.Empty(e.FindAll("#km-bos"));
        });
    }
}
