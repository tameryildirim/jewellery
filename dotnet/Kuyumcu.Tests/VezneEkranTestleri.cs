using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// VEZNE EKRANI — bUnit (paket 19 Faz A).
//
// ⚠ KARARSIZLIK KORUMASI (Aşkın koşulu): ekranda YOKLAMA ZAMANLAYICISI YOKTUR;
//   kuyruk yalnız ilk yüklemede ve "Yenile" düğmesiyle tazelenir. Testler
//   zamanlayıcı beklemez, WaitForAssertion gerekmez.
//
// ⚠ Sahte API camelCase döner (sunucunun gerçek şekli); anonim alanlar
//   OrdinalIgnoreCase okunur.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class VezneEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string kuyrukYaniti = BosKuyruk;
    private string detayYaniti = VeznedeDetay;
    private string kasaYaniti = TekKasa;

    private const string BosKuyruk = """
        {"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,
         "dahaFazlasiVar":false,"vezneZorunlu":false,"gerekceAsgari":10,"subeId":"sb1",
         "durumKirilimi":[],"fisler":[]}
        """;

    private const string DoluKuyruk = """
        {"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":250,
         "dahaFazlasiVar":true,"vezneZorunlu":false,"gerekceAsgari":10,"subeId":"sb1",
         "durumKirilimi":[{"durum":"VEZNEDE","adet":2}],
         "fisler":[
           {"id":"v1","durum":"VEZNEDE","toplamTl":5000.00,"damgaliToplamTl":5000.00,
            "toplamTahmini":false,"cariId":"c1","cariAd":"Ayşe Yılmaz",
            "aciklama":"bilezik","hazirlayanKullaniciId":"u2","hazirlayanAd":"Ali Tezgâhtar",
            "kalemSayisi":1,"hareketFisId":null,"duzenlenebilir":false,"mutabakatGerekli":false,
            "olusturuldu":"2026-09-17T08:00:00Z","veznelendi":"2026-09-17T08:05:00Z",
            "guncellendi":"2026-09-17T08:05:00Z"},
           {"id":"v2","durum":"ACIK","toplamTl":2150.00,"damgaliToplamTl":0.00,
            "toplamTahmini":true,"cariId":null,"cariAd":null,
            "aciklama":null,"hazirlayanKullaniciId":"u1","hazirlayanAd":"Test Kullanıcı",
            "kalemSayisi":2,"hareketFisId":null,"duzenlenebilir":true,"mutabakatGerekli":false,
            "olusturuldu":"2026-09-17T09:00:00Z","veznelendi":null,
            "guncellendi":"2026-09-17T09:00:00Z"}]}
        """;

    private const string KendiFisiKuyruk = """
        {"durum":"VEZNEDE","istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":1,
         "dahaFazlasiVar":false,"vezneZorunlu":true,"gerekceAsgari":10,"subeId":"sb1",
         "durumKirilimi":[],"fisler":[
           {"id":"v1","durum":"VEZNEDE","toplamTl":5000.00,"cariId":"c1","cariAd":"Ayşe Yılmaz",
            "aciklama":null,"hazirlayanKullaniciId":"u1","hazirlayanAd":"Test Kullanıcı",
            "kalemSayisi":1,"hareketFisId":null,"duzenlenebilir":false,"mutabakatGerekli":false,
            "olusturuldu":"2026-09-17T08:00:00Z","veznelendi":"2026-09-17T08:05:00Z",
            "guncellendi":"2026-09-17T08:05:00Z"}]}
        """;

    private const string VeznedeDetay = """
        {"id":"v1","durum":"VEZNEDE","cariId":"c1","cariAd":"Ayşe Yılmaz","aciklama":"bilezik",
         "kurSnapshotId":"kur-1","toplamTl":5000.00,"damgaliToplamTl":5000.00,"hareketFisId":null,
         "hazirlayanKullaniciId":"u2","hazirlayanAd":"Ali Tezgâhtar","tahsilEdenKullaniciId":null,
         "tahsilEdenAd":null,"iptalGerekce":null,"vezneZorunlu":false,"gerekceAsgari":10,
         "duzenlenebilir":false,"veznelenebilir":false,"tahsilEdilebilir":true,
         "iptalEdilebilir":true,"terminal":false,"mutabakatGerekli":false,
         "olusturuldu":"2026-09-17T08:00:00Z","guncellendi":"2026-09-17T08:05:00Z",
         "veznelendi":"2026-09-17T08:05:00Z","tahsilEdildi":null,"iptalEdildi":null,
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"Bilezik","adet":1.0,
                      "birimFiyatTl":4900.00,"iscilikTl":100.00,"sira":0,"tutarTl":5000.00}]}
        """;

    private const string KendiFisiDetay = """
        {"id":"v1","durum":"VEZNEDE","cariId":"c1","cariAd":"Ayşe Yılmaz","aciklama":null,
         "kurSnapshotId":"kur-1","toplamTl":5000.00,"damgaliToplamTl":5000.00,"hareketFisId":null,
         "hazirlayanKullaniciId":"u1","hazirlayanAd":"Test Kullanıcı","tahsilEdenKullaniciId":null,
         "tahsilEdenAd":null,"iptalGerekce":null,"vezneZorunlu":true,"gerekceAsgari":10,
         "duzenlenebilir":false,"veznelenebilir":false,"tahsilEdilebilir":true,
         "iptalEdilebilir":true,"terminal":false,"mutabakatGerekli":false,
         "olusturuldu":"2026-09-17T08:00:00Z","guncellendi":"2026-09-17T08:05:00Z",
         "veznelendi":"2026-09-17T08:05:00Z","tahsilEdildi":null,"iptalEdildi":null,
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"Bilezik","adet":1.0,
                      "birimFiyatTl":5000.00,"iscilikTl":0.00,"sira":0,"tutarTl":5000.00}]}
        """;

    private const string TahsilEdilmisDetay = """
        {"id":"v3","durum":"TAHSIL","cariId":"c1","cariAd":"Ayşe Yılmaz","aciklama":null,
         "kurSnapshotId":"kur-1","toplamTl":5000.00,"damgaliToplamTl":5000.00,"hareketFisId":"f-9",
         "hazirlayanKullaniciId":"u2","hazirlayanAd":"Ali Tezgâhtar","tahsilEdenKullaniciId":"u1",
         "tahsilEdenAd":"Test Kullanıcı","iptalGerekce":null,"vezneZorunlu":false,"gerekceAsgari":10,
         "duzenlenebilir":false,"veznelenebilir":false,"tahsilEdilebilir":false,
         "iptalEdilebilir":false,"terminal":true,"mutabakatGerekli":false,
         "olusturuldu":"2026-09-17T08:00:00Z","guncellendi":"2026-09-17T08:30:00Z",
         "veznelendi":"2026-09-17T08:05:00Z","tahsilEdildi":"2026-09-17T08:30:00Z","iptalEdildi":null,
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"Bilezik","adet":1.0,
                      "birimFiyatTl":5000.00,"iscilikTl":0.00,"sira":0,"tutarTl":5000.00}]}
        """;

    private const string TekKasa = """[{"id":"ks1","ad":"Merkez kasa","paraBirimi":"TRY"}]""";
    private const string IkiKasa = """
        [{"id":"ks1","ad":"Merkez kasa","paraBirimi":"TRY"},
         {"id":"ks2","ad":"Dolar kasası","paraBirimi":"USD"}]
        """;

    public VezneEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(VezneEkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                var y when y.EndsWith("/tahsil") =>
                    """{"vezneFisId":"v1","durum":"TAHSIL","hareketFisId":"f-42","toplamTl":5000.00}""",
                var y when y.EndsWith("/veznele") => sahip.detayYaniti,
                var y when y.EndsWith("/iptal") => sahip.detayYaniti,
                var y when y.Contains("/vezne/fisler/") => sahip.detayYaniti,
                var y when y.EndsWith("/vezne/fisler") => sahip.detayYaniti,
                var y when y.EndsWith("/vezne/kuyruk") => sahip.kuyrukYaniti,
                var y when y.EndsWith("/kasalar") => sahip.kasaYaniti,
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Yılmaz"}]""",
                var y when y.EndsWith("/urunler") => """[{"id":"u-1","ad":"Bilezik"}]""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":"sb1"}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Vezne> Ekran(string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "sb1"));
        return Render<Kuyumcu.Web.Pages.Vezne>();
    }

    // ── Kuyruk ───────────────────────────────────────────────────────────────

    [Fact]
    public void Ekran_acilinca_kuyruk_cekilir_ve_zamanlayici_kurulmaz()
    {
        Ekran();
        Assert.Single(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/vezne/kuyruk"));
    }

    [Fact]
    public void Bos_kuyruk_BOS_DURUM_cizer_tahsil_paneli_cizilmez()
    {
        var e = Ekran();
        Assert.Single(e.FindAll(".bos-durum"));
        Assert.Empty(e.FindAll("#vz-liste"));
        Assert.Empty(e.FindAll("#vz-tahsil-panel"));
    }

    [Fact]
    public void Elle_yenile_dugmesi_kuyrugu_YENIDEN_ceker()
    {
        var e = Ekran();
        var once = istekler.Count(i => i.Yol.EndsWith("/vezne/kuyruk"));
        e.Find("#vz-yenile").Click();
        Assert.Equal(once + 1, istekler.Count(i => i.Yol.EndsWith("/vezne/kuyruk")));
    }

    [Fact]
    public void Sunucu_siniri_ekranda_yazilir_sessiz_kirpma_yok()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        Assert.Single(e.FindAll("#vz-sinir-uyarisi"));
        Assert.Contains("200", e.Find("#vz-sinir-uyarisi").TextContent);
    }

    [Fact]
    public void Ham_durum_kodu_ekrana_basilmaz_sozlukten_cevrilir()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        var satir = e.Find("#vz-satir-v1").TextContent;
        Assert.Contains("Veznede", satir);
        Assert.DoesNotContain("VEZNEDE", satir);
    }

    [Fact]
    public void ACIK_fiste_duzenle_ve_vezneye_gonder_VAR_VEZNEDE_fiste_YOK()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        Assert.Single(e.FindAll("#vz-duzenle-v2"));
        Assert.Single(e.FindAll("#vz-veznele-v2"));
        Assert.Empty(e.FindAll("#vz-duzenle-v1"));
        Assert.Empty(e.FindAll("#vz-veznele-v1"));
    }

    // ── Tahsil paneli ────────────────────────────────────────────────────────

    [Fact]
    public void Fis_secilince_kalemler_ve_toplam_gorunur()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        e.Find("#vz-sec-v1").Click();

        Assert.Single(e.FindAll("#vz-detay-kalemler"));
        Assert.Contains("Bilezik", e.Find("#vz-detay-kalemler").TextContent);
        Assert.Contains(Bicim.Tl(5000m), e.Find("#vz-detay-toplam").TextContent);
    }

    [Fact]
    public void Tek_kasada_kasa_secici_CIZILMEZ_deger_salt_okunur_yazilir()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        e.Find("#vz-sec-v1").Click();

        Assert.Empty(e.FindAll("#vz-bacak-kasa-0"));
        Assert.Single(e.FindAll("#vz-kasa-sabit"));
        Assert.Contains("Merkez kasa", e.Find("#vz-kasa-sabit").TextContent);
    }

    [Fact]
    public void Iki_kasada_secici_CIZILIR()
    {
        kuyrukYaniti = DoluKuyruk;
        kasaYaniti = IkiKasa;
        var e = Ekran();
        e.Find("#vz-sec-v1").Click();

        Assert.Single(e.FindAll("#vz-bacak-kasa-0"));
        Assert.Empty(e.FindAll("#vz-kasa-sabit"));
    }

    [Fact]
    public void Bacak_toplami_fis_tutarina_esit_degilken_Tahsil_PASIF_ve_sebep_yazar()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        e.Find("#vz-sec-v1").Click();

        e.Find("#vz-bacak-tutar-0").Input("4000");
        Assert.True(e.Find("#vz-tahsil").HasAttribute("disabled"));
        Assert.Single(e.FindAll("#vz-tahsil-kilit"));
        Assert.Contains("eşit", e.Find("#vz-tahsil-kilit").TextContent);
    }

    [Fact]
    public void Kalani_ata_bacagi_fis_tutarina_tamamlar()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        e.Find("#vz-sec-v1").Click();
        e.Find("#vz-bacak-tutar-0").Input("4000");

        e.Find("#vz-kalani-ata").Click();
        Assert.False(e.Find("#vz-tahsil").HasAttribute("disabled"));
    }

    [Fact]
    public void Tahsil_TEK_POST_uretir_ve_defter_fisi_basari_bandina_yazilir()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        e.Find("#vz-sec-v1").Click();
        e.Find("#vz-tahsil").Click();

        var tahsiller = istekler.Where(i => i.Metot == "POST" && i.Yol.EndsWith("/tahsil")).ToList();
        Assert.Single(tahsiller);
        Assert.Contains("\"bacaklar\"", tahsiller[0].Govde);
        Assert.Contains("\"kurSnapshotId\":\"kur-1\"", tahsiller[0].Govde);
        // UX-S27: sunucunun döndürdüğü defter fişi banda YAZILIR.
        Assert.Contains("f-42", e.Find("#vz-basari-bandi").TextContent);
    }

    [Fact]
    public void Vezne_ayrimi_acikken_KENDI_fisinde_Tahsil_PASIF_ve_sebep_yazar()
    {
        kuyrukYaniti = KendiFisiKuyruk;
        detayYaniti = KendiFisiDetay;
        var e = Ekran();
        e.Find("#vz-sec-v1").Click();

        Assert.True(e.Find("#vz-tahsil").HasAttribute("disabled"));
        Assert.Contains("tahsil edemezsiniz", e.Find("#vz-tahsil-kilit").TextContent);
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/tahsil"));
    }

    [Fact]
    public void TAHSIL_edilmis_fiste_tahsil_paneli_yerine_DEGISMEZ_IPUCU_cizilir()
    {
        kuyrukYaniti = DoluKuyruk;
        detayYaniti = TahsilEdilmisDetay;
        var e = Ekran();
        e.Find("#vz-sec-v1").Click();

        Assert.Empty(e.FindAll("#vz-tahsil-panel"));
        Assert.Single(e.FindAll("#degismez-ipucu-VezneFisi"));
    }

    // ── Yeni fiş formu ───────────────────────────────────────────────────────

    [Fact]
    public void Kalemsiz_kaydet_istek_ATMAZ_ve_alan_alti_mesaj_cikar()
    {
        var e = Ekran();
        e.Find("#vz-yeni-ac").Click();
        var once = istekler.Count(i => i.Metot == "POST");

        e.Find("#vz-f-kaydet").Click();

        Assert.Equal(once, istekler.Count(i => i.Metot == "POST"));
        Assert.Single(e.FindAll("#vz-f-kalem-hata"));
    }

    [Fact]
    public void Gecerli_kalemle_POST_govdesi_kur_ve_kalem_dizisiyle_kurulur()
    {
        var e = Ekran();
        e.Find("#vz-yeni-ac").Click();
        SeciciYardim.Sec(e, "vz-f-urun-0", "u-1");
        e.Find("#vz-f-fiyat-0").Input("1500.50");
        e.Find("#vz-f-kaydet").Click();

        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/vezne/fisler"));
        Assert.Contains("\"kurSnapshotId\":\"kur-1\"", post.Govde);
        Assert.Contains("\"birimFiyatTl\":1500.50", post.Govde);
    }

    // ── İptal ────────────────────────────────────────────────────────────────

    [Fact]
    public void Iptalde_gerekce_kisayken_dugme_PASIF_uzayinca_ACILIR()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        e.Find("#vz-iptal-v1").Click();

        Assert.True(e.Find("#vz-iptal-onayla").HasAttribute("disabled"));
        Assert.Single(e.FindAll("#vz-iptal-kilit"));

        e.Find("#vz-iptal-gerekce").Input("müşteri vazgeçti, mal vitrine döndü");
        Assert.False(e.Find("#vz-iptal-onayla").HasAttribute("disabled"));
    }

    [Fact]
    public void Iptal_onaylaninca_gerekceli_POST_gider()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        e.Find("#vz-iptal-v1").Click();
        e.Find("#vz-iptal-gerekce").Input("müşteri vazgeçti, mal vitrine döndü");
        e.Find("#vz-iptal-onayla").Click();

        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/iptal"));
        // Türkçe gövde AYRIŞTIRILARAK sınanır (JSON \u kaçışı ham metinde eşleşmez).
        var j = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement;
        Assert.Contains("vazgeçti", j.GetProperty("gerekce").GetString());
    }

    // ══════════════════════════════════════════════════════════════════════
    // PAKET UX-1-D — VEZNEDE fişinin kilidi GEREKÇELİ olur (QA turu 4, grup 1)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void UX1D_VEZNEDE_fiste_DUZENLE_YOK_ama_GEREKCE_ve_ALTERNATIF_YOL_VAR()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();

        // v1 = VEZNEDE: düzenleme düğmesi yok (sunucu 409 verirdi)...
        Assert.Empty(e.FindAll("#vz-duzenle-v1"));
        // ...ama SESSİZ değil: gerekçe kutusu satırın kendisinde.
        var ipucu = e.Find("#degismez-ipucu-VezneFisi-v1");
        Assert.False(string.IsNullOrWhiteSpace(ipucu.TextContent));
        // UX-S25: devre dışı düğme ÇİZİLMEZ.
        Assert.Empty(ipucu.QuerySelectorAll("button"));
        // Doğru düzeltme yolu gerçekten AÇIKTIR: iptal düğmesi duruyor.
        Assert.NotNull(e.Find("#vz-iptal-v1"));
    }

    [Fact]
    public void UX1D_ACIK_fiste_DUZENLE_VAR_ve_gerekce_kutusu_CIZILMEZ()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();

        // v2 = ACIK: yol yerinde düzenlemedir; gerekçe kutusu GÖSTERİLMEZ
        // (aksi hâlde açık bir yolun yanında "değiştirilemez" yazardı).
        Assert.NotNull(e.Find("#vz-duzenle-v2"));
        Assert.Empty(e.FindAll("#degismez-ipucu-VezneFisi-v2"));
    }

    // ── PAKET P-23c — FİŞ TÜRÜ (vezne fişinin genelleştirilmesi) ─────────────

    [Fact]
    public void Tur_secicisi_UC_turu_gosterir_TAKAS_ve_EMANET_YOKTUR()
    {
        var e = Ekran();
        e.Find("#vz-yeni-ac").Click();
        var secenekler = SeciciYardim.Secenekler(e, "vz-f-tur")
            .Select(o => o.Deger()).Where(d => d.Length > 0).ToArray();
        Assert.Equal(["SATIS", "TAHSILAT", "ISCILIK"], secenekler);
        // Kapsam kararı EKRANDA da yazılıdır (görünmez kural olmaz).
        Assert.Contains("Takas", e.Find("#vz-f-tur-kapsam").TextContent);
    }

    [Fact]
    public void TAHSILAT_secilince_kalem_satirlari_CIZILMEZ_tutar_alani_CIKAR()
    {
        var e = Ekran();
        e.Find("#vz-yeni-ac").Click();
        Assert.NotNull(e.Find("#vz-f-kalem-0"));      // SATIS: kalem satırı var

        SeciciYardim.Sec(e, "vz-f-tur", "TAHSILAT");

        Assert.Empty(e.FindAll("#vz-f-kalem-0"));     // kalemsiz türde YOK
        Assert.NotNull(e.Find("#vz-f-tutar"));
    }

    [Fact]
    public void TAHSILAT_fisi_POST_govdesi_tutarTl_tasir_kalemler_TASIMAZ()
    {
        var e = Ekran();
        e.Find("#vz-yeni-ac").Click();
        SeciciYardim.Sec(e, "vz-f-tur", "TAHSILAT");
        SeciciYardim.Sec(e, "vz-f-cari", "c1");
        e.Find("#vz-f-tutar").Input("2750.25");
        e.Find("#vz-f-kaydet").Click();

        var post = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/vezne/fisler"));
        Assert.Contains("\"fisTuru\":\"TAHSILAT\"", post.Govde);
        Assert.Contains("\"tutarTl\":2750.25", post.Govde);
        Assert.DoesNotContain("\"kalemler\"", post.Govde);
    }

    [Fact]
    public void TAHSILAT_fisinde_tutar_bos_ise_istek_ATILMAZ_alan_alti_mesaj_cikar()
    {
        var e = Ekran();
        e.Find("#vz-yeni-ac").Click();
        SeciciYardim.Sec(e, "vz-f-tur", "TAHSILAT");
        SeciciYardim.Sec(e, "vz-f-cari", "c1");
        var once = istekler.Count(i => i.Metot == "POST");

        e.Find("#vz-f-kaydet").Click();

        Assert.Equal(once, istekler.Count(i => i.Metot == "POST"));
        Assert.Single(e.FindAll("#vz-f-kalem-hata"));
    }

    [Fact]
    public void TAHSILAT_fisinde_cari_secilmezse_istek_ATILMAZ()
    {
        var e = Ekran();
        e.Find("#vz-yeni-ac").Click();
        SeciciYardim.Sec(e, "vz-f-tur", "TAHSILAT");
        e.Find("#vz-f-tutar").Input("500");
        var once = istekler.Count(i => i.Metot == "POST");

        e.Find("#vz-f-kaydet").Click();

        Assert.Equal(once, istekler.Count(i => i.Metot == "POST"));
        Assert.Single(e.FindAll("#vz-f-kalem-hata"));
    }

    // ── P-46: kuyrukta gerçek fiş toplamı (kuyruk = detay) ───────────────────

    [Fact]
    public void P46_Ekran_ACIK_fiste_hesaplanan_tutari_TAHMINI_rozetiyle_gosterir()
    {
        // QA tur 8: ACIK kalemli fiş kuyrukta "0,00 TL" görünüyordu. Sunucu artık
        // kalem toplamını döndürür; ekran onu basar ve damgalanmadığını SÖYLER.
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        var hucre = e.Find("#vz-toplam-v2").TextContent;
        Assert.Contains(Bicim.Tl(2150m), hucre);
        Assert.DoesNotContain("0,00 ", hucre.Replace("2.150,00", ""));
        var rozet = e.Find("#vz-toplam-rozet-v2");
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Vezne.ToplamTahmini, rozet.TextContent);
        Assert.NotNull(rozet.QuerySelector(".rozet-sari"));
    }

    [Fact]
    public void P46_Ekran_veznelenmis_fiste_DAMGALI_tutari_KESIN_rozetiyle_gosterir()
    {
        kuyrukYaniti = DoluKuyruk;
        var e = Ekran();
        Assert.Contains(Bicim.Tl(5000m), e.Find("#vz-toplam-v1").TextContent);
        var rozet = e.Find("#vz-toplam-rozet-v1");
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Vezne.ToplamDamgali, rozet.TextContent);
        Assert.DoesNotContain(Kuyumcu.Core.I18n.Sozlukler.Tr.Vezne.ToplamTahmini, rozet.TextContent);
    }

    // ── PAKET Q10-5 (QA turu 10 /vezne) ──────────────────────────────────────

    [Fact]
    public void Q105_Negatif_fiyatta_dogru_sebep_ve_fiyat_duzelince_mesaj_duser()
    {
        var e = Ekran();
        e.Find("#vz-yeni-ac").Click();
        SeciciYardim.Sec(e, "vz-f-urun-0", "u-1");
        e.Find("#vz-f-fiyat-0").Input("-5");
        e.Find("#vz-f-kaydet").Click();

        // Sebep "kalem yok" DEĞİL: ürün seçili satırda fiyat sıfırdan büyük olmalı.
        Assert.Equal(Kuyumcu.Core.I18n.Q105Sozlugu.Tr.VezneKalemSayiHatasi,
            e.Find("#vz-f-kalem-hata").TextContent.Trim());
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/vezne/fisler"));

        // Kullanıcı fiyatı düzeltir → eski mesaj ANINDA düşer (gönderim beklenmez).
        e.Find("#vz-f-fiyat-0").Input("1500");
        Assert.Empty(e.FindAll("#vz-f-kalem-hata"));
    }

    [Fact]
    public void Q105_Urunsuz_kaydette_kalem_gerekli_mesaji_korunur()
    {
        var e = Ekran();
        e.Find("#vz-yeni-ac").Click();
        e.Find("#vz-f-kaydet").Click();
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Vezne.KalemGerekli,
            e.Find("#vz-f-kalem-hata").TextContent.Trim());
    }

    [Fact]
    public void Q105_Basit_modda_defter_notu_ic_terim_tasimaz_PRO_degismez()
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => true);
        var basit = Ekran();
        var metin = basit.Find("#vz-defter-notu").TextContent.Trim();
        Assert.Equal(Kuyumcu.Core.I18n.Q105Sozlugu.Tr.VezneDefterKapilariBasit, metin);
        Assert.DoesNotContain("MASAK", metin);
        Assert.DoesNotContain("tevsik", metin);
    }

    [Fact]
    public void Q105_Profesyonel_modda_defter_notu_ayni_kalir()
    {
        var e = Ekran();
        Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.Vezne.DefterKapilari,
            e.Find("#vz-defter-notu").TextContent.Trim());
    }
}
