using System.Text.Json;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SİPARİŞ EKRANLARI — bUnit (paket 3 Faz A).
//
// ⚠ KARARSIZLIK KORUMASI: zamanlayıcı yok, yoklama yok, ateşle-unut yan kanal
//   beklenmiyor. Ölçüm ilk yükleme + AÇIK kullanıcı eylemleriyle yapılır.
//
// ⚠ SAAT: "geciken" rozeti ekranda HESAPLANMAZ — sunucunun döndürdüğü `geciken`
//   bayrağı çizilir. Bu yüzden gece yarısı tuzağı (K5) bUnit'te sahte saat
//   GEREKTİRMEZ; kararın kendisi SiparisGecikme saf testleriyle ölçülür.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SiparisEkranTestleri : BunitContext
{
    /// <summary>PAKET GOC-C: söz günü bugünden önce olamaz — testler ileri bir gün kullanır (makine biçimi).</summary>
    private static readonly string GocCIleriGun = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private readonly List<(string Metot, string Yol, string Sorgu, string Govde)> istekler = [];
    private string listeYaniti = BosListe;
    private string detayYaniti = TaslakDetay;
    // PAKET P-52: kur anlık görüntüsü testten test'e değişir — "kur yok"
    // hâli UYDURULMAZ, sahte sunucunun GERÇEK yanıtı değiştirilerek ölçülür.
    private string kurYaniti = """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""";

    private const string BosListe = """
        {"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":0,"dahaFazlasiVar":false,
         "gecikenSayisi":0,"durumKirilimi":[],"siparisler":[]}
        """;

    private const string IkiSiparis = """
        {"istenenLimit":200,"limit":200,"azamiLimit":200,"toplam":250,"dahaFazlasiVar":true,
         "gecikenSayisi":1,"durumKirilimi":[{"durum":"TASLAK","adet":1},{"durum":"HAZIR","adet":1}],
         "siparisler":[
           {"id":"s1","no":"S-2026-000001","tur":"MUSTERI_SIPARISI","durum":"TASLAK","cariId":"c1",
            "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-09-10","tahminiTutarTl":25000.00,
            "aciklama":"bilezik","kalemSayisi":1,"geciken":true,
            "izinliGecisler":["ONAYLANDI","IPTAL"],"duzenlenebilir":true,
            "olusturuldu":"2026-09-16T08:00:00Z","guncellendi":"2026-09-16T08:00:00Z"},
           {"id":"s2","no":"S-2026-000002","tur":"TAMIR","durum":"HAZIR","cariId":"c1",
            "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-12-01","tahminiTutarTl":null,
            "aciklama":null,"kalemSayisi":2,"geciken":false,
            "izinliGecisler":["TESLIM_EDILDI","IPTAL"],"duzenlenebilir":false,
            "olusturuldu":"2026-09-16T09:00:00Z","guncellendi":"2026-09-16T09:00:00Z"}]}
        """;

    private const string TaslakDetay = """
        {"id":"s1","no":"S-2026-000001","tur":"MUSTERI_SIPARISI","durum":"TASLAK","cariId":"c1",
         "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-09-10","aciklama":"bilezik",
         "tahminiTutarTl":25000.00,"kurSnapshotId":"kur-1","emanetHareketId":null,
         "geciken":true,"duzenlenebilir":true,"terminal":false,
         "izinliGecisler":["ONAYLANDI","IPTAL"],"gerekceIsteyenGecisler":["IPTAL"],"gerekceAsgari":10,
         "olusturanKullaniciId":"u1","olusturuldu":"2026-09-16T08:00:00Z","guncellendi":"2026-09-16T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":"u-1","urunAd":"Bilezik","tanim":null,"ayarId":"K22",
                      "gramAgirlik":10.0,"adet":1.0,"sira":0}],
         "gecisler":[{"id":"g1","oncekiDurum":null,"yeniDurum":"TASLAK","gerekce":null,
                      "kullaniciId":"u1","kullaniciAd":"Test Kullanıcı","olusturuldu":"2026-09-16T08:00:00Z"}]}
        """;

    // PAKET P-52 — kur anlık görüntüsü OLMAYAN düzenlenebilir taslak.
    private const string KursuzTaslakDetay = """
        {"id":"s4","no":"S-2026-000004","tur":"MUSTERI_SIPARISI","durum":"TASLAK","cariId":"c1",
         "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":null,"aciklama":null,
         "tahminiTutarTl":null,"kurSnapshotId":null,"emanetHareketId":null,
         "geciken":false,"duzenlenebilir":true,"terminal":false,
         "izinliGecisler":["ONAYLANDI","IPTAL"],"gerekceIsteyenGecisler":["IPTAL"],"gerekceAsgari":10,
         "olusturanKullaniciId":"u1","olusturuldu":"2026-09-16T08:00:00Z","guncellendi":"2026-09-16T08:00:00Z",
         "kalemler":[{"id":"k9","urunId":null,"urunAd":null,"tanim":"künye","ayarId":null,
                      "gramAgirlik":null,"adet":1.0,"sira":0}],
         "gecisler":[{"id":"g1","oncekiDurum":null,"yeniDurum":"TASLAK","gerekce":null,
                      "kullaniciId":"u1","kullaniciAd":"Test","olusturuldu":"2026-09-16T08:00:00Z"}]}
        """;

    private const string TeslimDetay = """
        {"id":"s3","no":"S-2026-000003","tur":"MUSTERI_SIPARISI","durum":"TESLIM_EDILDI","cariId":"c1",
         "cariAd":"Ayşe Yılmaz","subeId":"sb1","sozVerilenGun":"2026-09-10","aciklama":null,
         "tahminiTutarTl":null,"kurSnapshotId":null,"emanetHareketId":null,
         "geciken":false,"duzenlenebilir":false,"terminal":true,
         "izinliGecisler":[],"gerekceIsteyenGecisler":[],"gerekceAsgari":10,
         "olusturanKullaniciId":"u1","olusturuldu":"2026-09-16T08:00:00Z","guncellendi":"2026-09-16T08:00:00Z",
         "kalemler":[{"id":"k1","urunId":null,"urunAd":null,"tanim":"künye","ayarId":null,
                      "gramAgirlik":null,"adet":1.0,"sira":0}],
         "gecisler":[{"id":"g1","oncekiDurum":null,"yeniDurum":"TASLAK","gerekce":null,
                      "kullaniciId":"u1","kullaniciAd":"Test","olusturuldu":"2026-09-16T08:00:00Z"}]}
        """;

    public SiparisEkranTestleri()
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

    private sealed class SahteApi(SiparisEkranTestleri sahip) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var sorgu = istek.RequestUri!.Query;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            sahip.istekler.Add((istek.Method.Method, yol, sorgu, govde));

            var g = yol switch
            {
                var y when y.Contains("/siparisler/") && y.EndsWith("/gecis") =>
                    """{"id":"s1","no":"S-2026-000001","durum":"ONAYLANDI"}""",
                var y when y.Contains("/siparisler/") => sahip.detayYaniti,
                var y when y.EndsWith("/siparisler") => sahip.listeYaniti,
                var y when y.EndsWith("/subeler") => """[{"id":"sb1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") =>
                    """[{"id":"c1","ad":"Ayşe Yılmaz","tur":"MUSTERI"}]""",
                var y when y.EndsWith("/urunler") =>
                    """
                    [{"id":"u-1","ad":"Bilezik","ayarId":"K22","gramAgirlik":10.0},
                     {"id":"u-2","ad":"Künye","ayarId":"K14","gramAgirlik":22.5}]
                    """,
                var y when y.Contains("/fiyat/teklif") =>
                    """{"urunId":"u-1","fiyat":31500.00,"hasDeger":28000.00,"iscilik":3500.00}""",
                var y when y.Contains("/kur/guncel") => sahip.kurYaniti,
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

    private IRenderedComponent<Kuyumcu.Web.Pages.Siparisler> Liste(string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "sb1"));
        return Render<Kuyumcu.Web.Pages.Siparisler>();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.SiparisDetay> Detay(string rol = "SAHIP", string id = "s1")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "sb1"));
        return Render<Kuyumcu.Web.Pages.SiparisDetay>(p => p.Add(x => x.Id, id));
    }

    // ── Liste ────────────────────────────────────────────────────────────────

    [Fact]
    public void Ekran_acilinca_siparis_listesi_cekilir()
    {
        Liste();
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/siparisler"));
    }

    [Fact]
    public void Bos_liste_BOS_DURUM_cizer()
    {
        var e = Liste();
        Assert.Single(e.FindAll(".bos-durum"));
        Assert.Empty(e.FindAll("#sip-liste"));
    }

    [Fact]
    public void Sunucu_siniri_ekranda_yazilir_sessiz_kirpma_yok()
    {
        listeYaniti = IkiSiparis;
        var e = Liste();
        Assert.Single(e.FindAll("#sip-sinir-uyarisi"));
        Assert.Contains("200", e.Find("#sip-sinir-uyarisi").TextContent);
    }

    [Fact]
    public void Geciken_rozeti_SUNUCUNUN_bayragindan_cizilir()
    {
        listeYaniti = IkiSiparis;
        var e = Liste();
        // s1 geciken=true → rozet var; s2 geciken=false → yok.
        Assert.Contains("Gecikmiş", e.Find("#sip-satir-s1").TextContent);
        Assert.DoesNotContain("Gecikmiş", e.Find("#sip-satir-s2").TextContent);
        Assert.Equal("1", e.Find("#sip-ozet-geciken").TextContent.Trim());
    }

    [Fact]
    public void Ham_durum_kodu_ekrana_basilmaz_sozlukten_cevrilir()
    {
        listeYaniti = IkiSiparis;
        var e = Liste();
        var satir = e.Find("#sip-satir-s1").TextContent;
        Assert.Contains("Taslak", satir);
        Assert.DoesNotContain("MUSTERI_SIPARISI", satir);
        Assert.Contains("Müşteri siparişi", satir);
    }

    [Fact]
    public void Tek_subede_sube_secici_cizilmez_deger_salt_okunur_yazilir()
    {
        var e = Liste();
        e.Find("#sip-yeni-ac").Click();
        Assert.Empty(e.FindAll("#sip-f-sube"));
        Assert.Single(e.FindAll("#sip-f-sube-sabit"));
        Assert.Contains("Merkez", e.Find("#sip-f-sube-sabit").TextContent);
    }

    [Fact]
    public void Cari_secilmeden_kaydet_istek_ATMAZ_ve_alan_alti_mesaj_cikar()
    {
        var e = Liste();
        e.Find("#sip-yeni-ac").Click();
        var oncekiPost = istekler.Count(i => i.Metot == "POST");
        e.Find("#sip-f-kaydet").Click();

        Assert.Equal(oncekiPost, istekler.Count(i => i.Metot == "POST"));
        Assert.Single(e.FindAll("#sip-f-cari-hata"));
    }

    [Fact]
    public void POST_govdesi_kalem_dizisi_ve_soz_verilen_gun_ile_kurulur()
    {
        var e = Liste();
        e.Find("#sip-yeni-ac").Click();
        SeciciYardim.Sec(e, "sip-f-cari", "c1");
        e.Find("#sip-f-detay-ac").Click(); // KESIF-P6 tur 2: isteğe bağlı alanlar "Daha fazla"da
        e.Find("#sip-f-gun").Change(GocCIleriGun);   // PAKET GOC-C: söz günü geçmiş olamaz (K8c) — ileri gün
        SeciciYardim.Sec(e, "sip-f-kalem-urun-0", "u-1");
        e.Find("#sip-f-kaydet").Click();

        var post = istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/siparisler"));
        // ⚠ Türkçe JSON gövdesi AYRIŞTIRILARAK sınanır (kaçış tuzağı).
        var j = JsonDocument.Parse(post.Govde).RootElement;
        Assert.Equal("MUSTERI_SIPARISI", j.GetProperty("tur").GetString());
        Assert.Equal("c1", j.GetProperty("cariId").GetString());
        Assert.Equal(GocCIleriGun, j.GetProperty("sozVerilenGun").GetString());
        var kalemler = j.GetProperty("kalemler").EnumerateArray().ToList();
        Assert.Single(kalemler);
        Assert.Equal("u-1", kalemler[0].GetProperty("urunId").GetString());
    }

    // ── PAKET P-78 — Siparisler gram/adet tr-TR (eskiden Invariant: "1,5" = 15) ──
    [Fact]
    public void P78_yeni_sipariste_1_5_gram_ve_2_5_adet_TURKCE_okunur()
    {
        var e = Liste();
        e.Find("#sip-yeni-ac").Click();
        SeciciYardim.Sec(e, "sip-f-cari", "c1");
        e.Find("#sip-f-detay-ac").Click(); // KESIF-P6 tur 2: isteğe bağlı alanlar "Daha fazla"da
        e.Find("#sip-f-gun").Change(GocCIleriGun);   // PAKET GOC-C: söz günü geçmiş olamaz (K8c) — ileri gün
        SeciciYardim.Sec(e, "sip-f-kalem-urun-0", "u-1");
        // PAKET GOC-C: kutular ortak Gram/Adet bileşeni (yazarken 'input'); adet K14 tam sayı → 2 (2,5 artık alan altı hata, GocC testinde).
        e.Find("#sip-f-kalem-gram-0").Input("1,5");
        e.Find("#sip-f-kalem-adet-0").Input("2");
        e.Find("#sip-f-kaydet").Click();

        var post = istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/siparisler"));
        var k = JsonDocument.Parse(post.Govde).RootElement.GetProperty("kalemler").EnumerateArray().Single();
        Assert.Equal(1.5m, k.GetProperty("gramAgirlik").GetDecimal());
        Assert.Equal(2m, k.GetProperty("adet").GetDecimal());
    }

    [Fact]
    public void P78_yeni_sipariste_belirsiz_gram_POST_ATMAZ_ve_sebebini_soyler()
    {
        var e = Liste();
        e.Find("#sip-yeni-ac").Click();
        SeciciYardim.Sec(e, "sip-f-cari", "c1");
        e.Find("#sip-f-detay-ac").Click(); // KESIF-P6 tur 2: isteğe bağlı alanlar "Daha fazla"da
        e.Find("#sip-f-gun").Change(GocCIleriGun);   // PAKET GOC-C: söz günü geçmiş olamaz (K8c) — ileri gün
        SeciciYardim.Sec(e, "sip-f-kalem-urun-0", "u-1");
        e.Find("#sip-f-kalem-gram-0").Input("1.500");
        var once = istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith("/siparisler"));
        e.Find("#sip-f-kaydet").Click();

        Assert.Equal(once, istekler.Count(i => i.Metot == "POST" && i.Yol.EndsWith("/siparisler")));
        // PAKET GOC-C: alan altında belirsizlik metni; form sebebi ortak kapının "«Gram» kutusunu düzeltin" cümlesi.
        Assert.Contains(Kuyumcu.Core.I18n.SayiGirisiSozlugu.Tr.Belirsiz, e.Find("#sip-f-kalem-gram-0-hata").TextContent);
        Assert.Equal(string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Al("tr").AlanDuzeltin, Kuyumcu.Core.I18n.Sozlukler.Getir("tr").Siparis.Gram),
            e.Find("#sip-f-kalem-hata").TextContent.Trim());
    }

    [Fact]
    public void Fiyat_ekranda_hesaplanmaz_teklif_ucu_kurSnapshotId_ile_cagrilir()
    {
        var e = Liste();
        e.Find("#sip-yeni-ac").Click();
        SeciciYardim.Sec(e, "sip-f-kalem-urun-0", "u-1");
        e.Find("#sip-f-detay-ac").Click(); // KESIF-P6 tur 2: tahmini tutar "Daha fazla"da
        e.Find("#sip-f-teklif").Click();

        var teklif = istekler.Last(i => i.Yol.Contains("/fiyat/teklif"));
        Assert.Equal("GET", teklif.Metot);
        Assert.Contains("kurSnapshotId=kur-1", teklif.Sorgu);
        Assert.Contains("urunId=u-1", teklif.Sorgu);
        // Sunucunun döndürdüğü fiyat ekranda gösterilir (istemci hesaplamaz).
        Assert.Contains("31.500", e.Find("#sip-f-tahmini").TextContent);
    }

    [Fact]
    public void Urun_secilmeden_teklif_dugmesi_kilitlidir_ve_sebebini_soyler()
    {
        var e = Liste();
        e.Find("#sip-yeni-ac").Click();
        e.Find("#sip-f-detay-ac").Click(); // KESIF-P6 tur 2: tahmini tutar "Daha fazla"da
        Assert.True(e.Find("#sip-f-teklif").HasAttribute("disabled"));
        Assert.Single(e.FindAll("#sip-f-teklif-sebep"));
    }

    // ── Detay ────────────────────────────────────────────────────────────────

    [Fact]
    public void Durum_makinesinin_izin_vermedigi_gecis_dugmesi_hic_render_edilmez()
    {
        var e = Detay();
        Assert.Single(e.FindAll("#sipd-gecis-ONAYLANDI"));
        Assert.Single(e.FindAll("#sipd-gecis-IPTAL"));
        // TASLAK'tan gidilemeyen durumlar için DÜĞME YOK.
        Assert.Empty(e.FindAll("#sipd-gecis-TESLIM_EDILDI"));
        Assert.Empty(e.FindAll("#sipd-gecis-HAZIR"));
        Assert.Empty(e.FindAll("#sipd-gecis-ATOLYEDE"));
    }

    [Fact]
    public void Gecis_dugmesi_gercek_istek_atar_ve_iyimser_kilit_damgasi_gonderir()
    {
        var e = Detay();
        // QA turu 5 (YÜKSEK): geçiş artık ONAY KİPİNDEN geçer — tek tıklama
        // istek ATMAZ. Onay verildikten sonra istek gider ve damga taşınır.
        e.Find("#sipd-gecis-ONAYLANDI").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/gecis"));
        e.Find("#sipd-gecis-onayla").Click();

        var post = istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/gecis"));
        var j = JsonDocument.Parse(post.Govde).RootElement;
        Assert.Equal("ONAYLANDI", j.GetProperty("yeniDurum").GetString());
        Assert.True(j.TryGetProperty("guncellendi", out var damga));
        Assert.NotEqual(JsonValueKind.Null, damga.ValueKind);
    }

    [Fact]
    public void Iptal_dugmesi_gerekce_bosken_istegi_GONDERMEZ()
    {
        var e = Detay();
        e.Find("#sipd-gecis-IPTAL").Click();   // modalı açar, istek ATMAZ
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/gecis"));

        var onay = e.Find("#sipd-iptal-onay");
        Assert.True(onay.HasAttribute("disabled"));
        onay.Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/gecis"));

        // Gerekçe yeterince uzun olunca düğme açılır ve istek gider.
        e.Find("#sipd-iptal-gerekce").Input("Müşteri vazgeçti, kayıt kapatıldı");
        e.Find("#sipd-iptal-onay").Click();
        var post = istekler.Last(i => i.Metot == "POST" && i.Yol.EndsWith("/gecis"));
        var j = JsonDocument.Parse(post.Govde).RootElement;
        Assert.Equal("IPTAL", j.GetProperty("yeniDurum").GetString());
        Assert.Contains("vazgeçti", j.GetProperty("gerekce").GetString()!);
    }

    [Fact]
    public void Tezgahtar_icin_iptal_dugmesi_kilitli_ve_sebebi_yazili()
    {
        var e = Detay("TEZGAHTAR");
        Assert.True(e.Find("#sipd-gecis-IPTAL").HasAttribute("disabled"));
        Assert.Single(e.FindAll("#sipd-gecis-IPTAL-sebep"));
    }

    [Fact]
    public void TASLAK_duzenlenir_terminal_durumda_DegismezIpucu_cizilir()
    {
        var taslak = Detay();
        Assert.Single(taslak.FindAll("#sipd-duzenle"));
        Assert.Empty(taslak.FindAll("#degismez-ipucu-Siparis"));

        detayYaniti = TeslimDetay;
        var teslim = Detay(id: "s3");
        Assert.Empty(teslim.FindAll("#sipd-duzenle"));
        Assert.Single(teslim.FindAll("#degismez-ipucu-Siparis"));
        // UX-S25: devre dışı düğme DE çizilmez — gerekçe + alternatif yol yazılır.
        Assert.Empty(teslim.FindAll("#sipd-gecis-IPTAL"));
        Assert.Single(teslim.FindAll("#sipd-gecis-yok"));
    }

    [Fact]
    public void Duzenleme_PATCH_govdesi_damga_ve_kalemlerle_kurulur()
    {
        var e = Detay();
        e.Find("#sipd-duzenle").Click();
        e.Find("#sipd-d-aciklama").Input("yeni not");
        e.Find("#sipd-d-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        var j = JsonDocument.Parse(patch.Govde).RootElement;
        Assert.Equal("yeni not", j.GetProperty("aciklama").GetString());
        Assert.True(j.TryGetProperty("guncellendi", out _));
        Assert.Equal(1, j.GetProperty("kalemler").GetArrayLength());
    }

    // ═══ PAKET P-52 — ürün seçici + tahmini tutar (düzenleme formu) ═════════
    // QA turu 8 / YÜKSEK: sunucu HER İKİSİNİ de kabul ediyordu (KalemBirlestir
    // urunId'yi korur, TahminDogrula tahminiTutarTl+kurSnapshotId alır); eksik
    // olan YALNIZ ekrandı. Ölçüm ekranın GÖVDESİ üzerinden yapılır.

    [Fact]
    public void DuzenlemeFormundaUrunSeciciGorunur()
    {
        var e = Detay();
        e.Find("#sipd-duzenle").Click();

        // Mevcut ürün ÖN SEÇİLİ gelir (UX-S21 — sistemin bildiği yeniden sorulmaz).
        Assert.Equal("u-1", e.Find("#sipd-d-kalem-urun-0").GetAttribute("data-deger"));
        // Ayar artık serbest metin DEĞİL, sözlükten seçimdir (UX-S31).
        Assert.Equal("K22", e.Find("#sipd-d-kalem-ayar-0").GetAttribute("data-deger"));
        // UX-S29: sunucu sınırı ekranda yazar.
        Assert.Single(e.FindAll("#sipd-d-urun-sinir"));
    }

    [Fact]
    public void UrunDegisiminceKalemKimligiKorunur()
    {
        var e = Detay();
        e.Find("#sipd-duzenle").Click();
        e.Find("#sipd-d-kalem-urun-0").Click();                 // seçiciyi aç
        e.Find("#sipd-d-kalem-urun-0-secenek-1").Click();       // 2. ürün: u-2
        e.Find("#sipd-d-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        var kalemler = JsonDocument.Parse(patch.Govde).RootElement.GetProperty("kalemler");
        // Kaydet sonrası kalem SAYISI değişmez — çoğaltma yok.
        Assert.Equal(1, kalemler.GetArrayLength());
        // KRİTİK: gövde kalem `id`sini TAŞIR → sunucu kalemi günceller, yenisini doğurmaz.
        Assert.Equal("k1", kalemler[0].GetProperty("id").GetString());
        Assert.Equal("u-2", kalemler[0].GetProperty("urunId").GetString());
        // UX-S21: ürün kartından türeyen ayar da gövdeye yansır.
        Assert.Equal("K14", kalemler[0].GetProperty("ayarId").GetString());
    }

    [Fact]
    public void TahminiTutarEkrandanKaydedilir()
    {
        var e = Detay();
        e.Find("#sipd-duzenle").Click();
        e.Find("#sipd-d-tahmini").Input("31500");
        e.Find("#sipd-d-kaydet").Click();

        var patch = istekler.Last(i => i.Metot == "PATCH");
        var j = JsonDocument.Parse(patch.Govde).RootElement;
        Assert.Equal(31500m, j.GetProperty("tahminiTutarTl").GetDecimal());
        // Kur anlık görüntüsü kullanıcıdan istenmez; ekran birlikte gönderir.
        Assert.Equal("kur-1", j.GetProperty("kurSnapshotId").GetString());
    }

    [Fact]
    public void TahminiTutarKursuzIseIstekGitmez()
    {
        kurYaniti = """{"id":null}""";
        detayYaniti = KursuzTaslakDetay;
        var e = Detay(id: "s4");
        e.Find("#sipd-duzenle").Click();
        // UX-S30: eksik koşul alanın yanında YAZAR.
        Assert.Single(e.FindAll("#sipd-d-tahmini-kur-yok"));

        e.Find("#sipd-d-tahmini").Input("31500");
        e.Find("#sipd-d-kaydet").Click();

        // DoD EK md.3: sessiz etkisizlik yok — alan altı mesaj var, ağ isteği YOK.
        Assert.Single(e.FindAll("#sipd-d-tahmini-hata"));
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }

    // KESIF-P6 tur 2 (hakem 01.10, K15 + K17): durum geçmişi KAPALI akordeon; "yalnız eklenir"
    // açıklama satırı kaldırıldı. Açılınca satırlar yine kartla gelen veriden çizilir (istek yok).
    [Fact]
    public void Gecis_dokumu_kapali_katlanirda_aciklama_satiri_yok()
    {
        var e = Detay();
        Assert.Empty(e.FindAll("#sipd-appendonly"));
        Assert.Empty(e.FindAll("#sipd-gecis-satir-g1"));
        Assert.Equal("false", e.Find("#sipd-gecmis-ac").GetAttribute("aria-expanded"));
        var once = istekler.Count;
        e.Find("#sipd-gecmis-ac").Click();
        Assert.Single(e.FindAll("#sipd-gecis-satir-g1"));
        Assert.Equal(once, istekler.Count);
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — sipariş formları: "1.500" ═════════════════════════════
    [Fact]
    public void OK1_K4_yeni_sipariste_belirsiz_gram_Olustur_PASIF_sebep_ve_kutu_hatali()
    {
        var e = Liste();
        e.Find("#sip-yeni-ac").Click();
        SeciciYardim.Sec(e, "sip-f-cari", "c1");
        SeciciYardim.Sec(e, "sip-f-kalem-urun-0", "u-1");
        e.Find("#sip-f-kalem-gram-0").Input("1.500");
        Assert.Equal("1.500", e.Find("#sip-f-kalem-gram-0").GetAttribute("value"));   // yazılan KALIR
        Assert.Equal("true", e.Find("#sip-f-kalem-gram-0").GetAttribute("aria-invalid"));
        Assert.True(e.Find("#sip-f-kaydet").HasAttribute("disabled"));
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Getir("tr").Siparis.Gram, e.Find("#sip-f-sayi-kilit").TextContent);
        e.Find("#sip-f-kalem-gram-0").Input("1500");
        Assert.False(e.Find("#sip-f-kaydet").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#sip-f-sayi-kilit"));
        Assert.Equal("false", e.Find("#sip-f-kalem-gram-0").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void OK1_K4_detay_tahmini_tutar_belirsiz_1_500_PATCH_YOK_sebep_yazar()
    {
        var e = Detay();
        e.Find("#sipd-duzenle").Click();
        foreach (var a in new[] { "1", "1.", "1.5", "1.50", "1.500" }) e.Find("#sipd-d-tahmini").Input(a);
        e.Find("#sipd-d-tahmini").Blur();
        Assert.Equal("1.500", e.Find("#sipd-d-tahmini").GetAttribute("value"));
        Assert.True(e.Find("#sipd-d-kaydet").HasAttribute("disabled"));
        Assert.NotEmpty(e.Find("#sipd-d-sayi-kilit").TextContent);
        var once = istekler.Count(i => i.Metot == "PATCH");
        e.Find("#sipd-d-kaydet").Click();
        Assert.Equal(once, istekler.Count(i => i.Metot == "PATCH"));
    }
}
