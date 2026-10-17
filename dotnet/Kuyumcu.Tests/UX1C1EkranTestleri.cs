using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-1-C1 — "her veri girilen yer düzeltilebilir", sayfa grubu 1
//   /giris · / (Panel) · /islemler (POS) · /cariler · /urunler ·
//   /urunler/agac · /urunler/etiketler
//
// ÖLÇÜLEN ŞEY "metin var mı" DEĞİL:
//   • Düzenle → hangi uca HANGİ GÖVDEYLE gidiliyor
//   • Boş/geçersiz gönderimde SESSİZ ETKİSİZLİK var mı (istek yok + mesaj yok)
//   • Düzenlenemeyen kayıtta düğme yerine GEREKÇE çiziliyor mu (UX-S25)
//   • Kilitli düğme SEBEBİNİ söylüyor mu (UX-S30)
//
// Bu dosya MEVCUT testlerin hiçbirini değiştirmez; yalnız EKLER (K4).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX1C1EkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    /// <summary>Testten teste değişen cari listesi (aktif/pasif senaryoları).</summary>
    private string cariListesi = """
        [{"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567",
          "email":"ayse@ornek.com","tlBakiye":-1500.00,"hasGramBakiye":2.5,"aktif":true},
         {"id":"c2","ad":"Pasif Toptancı","tur":"TEDARIKCI","telefon":null,
          "email":null,"tlBakiye":0,"hasGramBakiye":0,"aktif":false}]
        """;

    /// <summary>Reyon listesi — QA turu 2'nin "satırda 0 düğme" bulgusunun fikstürü.</summary>
    private string reyonListesi = """
        [{"id":"r1","subeId":"s1","ad":"Vitrin 1","tip":"VITRIN","aktif":true,"urunSayisi":0},
         {"id":"r2","subeId":"s1","ad":"Eski Vitrin","tip":"DEPO","aktif":false,"urunSayisi":0}]
        """;

    /// <summary>Son satışlar (POS) — iade-adayları ucu; boş dizi de sınanır.</summary>
    /// <summary>Stok yanıtı boş olsun mu (UX-S25 gerekçe senaryosu)?</summary>
    private bool stokBos;

    private string iadeAdaylari = """
        [{"id":"h1","tarih":"2026-09-17T09:00:00Z","tlTutari":11500.00,"urunId":"u1",
          "urunAdi":"22 Ayar Çeyrek","toplamAdet":1,"iadeEdilenAdet":0,"kalanAdet":1,"cariId":"c1"}]
        """;

    public UX1C1EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => cariListesi, () => reyonListesi, () => iadeAdaylari, () => stokBos))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();          // P-37b: karşılama "son işlemler" kutucuğu
        Services.AddScoped<MenuTercihServisi>();     // P-37b: kullanıcı bazlı kutucuk düzeni
        Services.AddScoped<Microsoft.Extensions.Configuration.IConfiguration>(
            _ => new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();           // P-37b: hava KAPALI (boş yapılandırma)
    }

    // ⚠ Sahte API'nin '{}' varsayılanı BIRAKILMADI: bu paketin dokunduğu her uca
    //   GERÇEK yanıt şeklinde cevap verilir (S-2'nin Stok modalı NRE'si tam da
    //   '{}' varsayılanından doğmuştu).
    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string> cariler,
        Func<string> reyonlar,
        Func<string> iadeler,
        Func<bool> stokBos) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, tam, govde));

            var g = yol switch
            {
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-17T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/kur/kaynaklar") =>
                    """{"kaynaklar":["ALTINKAYNAK"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK"}""",
                var y when y.Contains("/islemler/iade-adaylari") => iadeler(),
                // P-23 FAZ 2 — ödeme kırılımı ucu: '{}' varsayılanı BIRAKILMADI,
                // gerçek yanıt şeklinde (kalem + kapsam bayrağı) cevap verilir.
                var y when y.Contains("/islemler/fis/") => """
                    {"fisId":"h1","kalemler":[],"bacaklar":[],"veresiyeToplam":0,
                     "odemeKalemleri":[
                        {"id":"ok1","sira":0,"arac":"NAKIT","tutarTl":6000,"paraBirimi":"TRY",
                         "dovizTutar":null,"kasaId":"ks1","kasaHareketId":"kh1","posTahsilatId":null,
                         "taksitAdedi":null,"bilinmiyor":false,"geriDolduruldu":false,"aktif":true,
                         "stornoHareketId":null},
                        {"id":"ok2","sira":1,"arac":"VERESIYE","tutarTl":4000,"paraBirimi":"TRY",
                         "dovizTutar":null,"kasaId":null,"kasaHareketId":null,"posTahsilatId":null,
                         "taksitAdedi":null,"bilinmiyor":false,"geriDolduruldu":true,"aktif":true,
                         "stornoHareketId":null}],
                     "odemeKalemToplam":10000,"kalemKapsami":"KAYITLI"}
                    """,
                var y when y.Contains("/ekstre") =>
                    """{"bakiye":{"tlBakiye":-1500,"hasGramBakiye":2.5},"hareketler":[]}""",
                var y when y.EndsWith("/cari/c1") => """
                    {"id":"c1","ad":"Ayşe Demir","tur":"MUSTERI","telefon":"05551234567",
                     "email":"ayse@ornek.com","vergiNo":"1234567890","adres":"Şişli, İstanbul","aktif":true}
                    """,
                var y when y.EndsWith("/cari") => cariler(),
                var y when y.Contains("/cari?") => cariler(),
                var y when y.EndsWith("/reyonlar") => reyonlar(),
                var y when y.Contains("/reyonlar/") => """{"id":"r1","subeId":"s1","ad":"Vitrin 1","tip":"VITRIN","aktif":true,"urunSayisi":0}""",
                var y when y.Contains("/urun-kategorileri") =>
                    """[{"id":"k1","ad":"Ziynet","sira":0,"aktif":true,"urunSayisi":2,"altlar":[]}]""",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":100,"aktif":true}]""",
                var y when y.EndsWith("/urunler/u1") => """
                    {"id":"u1","tip":"ZIYNET","kod":"K-1","ad":"Burma Bilezik","ayarId":"K22",
                     "gramAgirlik":13.2,"tasAgirlik":0,"iscilikTl":500,"barkod":"8690000000001",
                     "tasKokeni":"TAS_YOK","tasIbaresi":null,"kategoriId":"k1","reyonId":"r1",
                     "satisFiyati":null,"gercekMilyem":916.0,"birim":"ADET","aktif":true,
                     "konsinye":false,"konsinyeCariId":null,"konsinyeBirimBedel":null}
                    """,
                // PAKET 20 — GET /urunler/madenler: maden + saflık tablosu ve
                // etiket ibare politikası (ADR-0005: rodyum ibaresi varsayılan KAPALI).
                // ⚠ Bu satır '/urunler' genel dalından ÖNCE gelmek ZORUNDA: aksi
                // hâlde ürün DİZİSİ dönerdi ve istemci JsonException ile kırılırdı.
                var y when y.EndsWith("/urunler/madenler") => """
                    {"madenler":[
                       {"maden":"ALTIN","madenAdi":"Altın","rodyumKaplanabilir":true,
                        "ayarlar":[{"id":"K22","ad":"22 Ayar","milyem":916,"kiymetli":true,"hasHesaplanir":true}]},
                       {"maden":"CELIK","madenAdi":"Çelik","rodyumKaplanabilir":false,
                        "ayarlar":[{"id":"CELIK","ad":"Paslanmaz Çelik","milyem":0,"kiymetli":false,"hasHesaplanir":false}]}],
                     "etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
                    """,
                // PAKET ISTEK-1 — toplu stok adedi (UrunStokOzetController şekli).
                var y when y.Contains("/urunler/stok-adetleri") => stokBos()
                    ? """{"urunler":[{"urunId":"u1","toplamAdet":0,"toplamHasGram":0},{"urunId":"u2","toplamAdet":0,"toplamHasGram":0}]}"""
                    : """{"urunler":[{"urunId":"u1","toplamAdet":3,"toplamHasGram":36.6},{"urunId":"u2","toplamAdet":0,"toplamHasGram":0}]}""",
                var y when y.EndsWith("/stok") => stokBos()
                    ? """{"subeStoklar":[]}"""
                    : """{"subeStoklar":[{"subeId":"s1","adet":3,"toplamHasGram":36.6}]}""",
                var y when y.Contains("/urunler") => """
                    [{"id":"u1","tip":"ZIYNET","kod":"K-1","ad":"Burma Bilezik","ayarId":"K22",
                      "gramAgirlik":13.2,"tasAgirlik":0,"hasGrami":12.1,"barkod":"8690000000001","aktif":true},
                     {"id":"u2","tip":"ZIYNET","kod":"K-2","ad":"Pasif Kolye","ayarId":"K14",
                      "gramAgirlik":5,"tasAgirlik":0,"hasGrami":2.9,"barkod":null,"aktif":false}]
                    """,
                var y when y.Contains("/masak/esikler") =>
                    """{"kimlikEsigi":185000,"beyanEsigi":185000,"paraBirimi":"TRY"}""",
                var y when y.Contains("/raporlar/gunluk") =>
                    """{"tarih":"2026-09-17","satis":{"adet":1,"toplamTl":1000,"toplamHas":1},"alis":null,"hurda":null,"netHasGramHareketi":1,"hareketSayisi":1,"kasalar":[]}""",
                var y when y.Contains("/raporlar/aylik") => """{"gunler":[]}""",
                var y when y.Contains("/fiyat/listeler") =>
                    """[{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","aktif":true}]""",
                var y when y.Contains("/fiyat/teklif") =>
                    """{"teklifVar":true,"sebep":null,"listeFiyati":11500,"hasDegeri":null,"marjTl":null,"iscilikTl":null,"indirimTutari":null,"satisFiyati":11500,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var y when y.Contains("/tevsik/gunluk") =>
                    """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""",
                var y when y.Contains("/masak/ozet") => """{"suresiGecmis":0,"yaklasan":0}""",
                var y when y.Contains("/gider-pusulalari/sure-takibi") => """{"gecikmisSayisi":0}""",
                // P-10: Urunler ekrani parite kodlarini TEK KAYNAKTAN okur (UX-S31).
                var p10p when p10p.Contains("/kur/pariteler") =>
                    """{"kaynak":"TEST","pariteler":[{"kod":"BILEZIK_22","ad":"22 Ayar Bilezik","alis":6800,"satis":6900,"birim":"GRAM"}]}""",
                // P-10: KART bacagi POS cihazi listesini okur.
                var p10c when p10c.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private void OturumAc(string rol = "SAHIP") =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private static bool AlanGonderildi(string govde, string ad) =>
        System.Text.Json.JsonDocument.Parse(govde).RootElement.TryGetProperty(ad, out _);

    // ═════════════════════════════════════════════════════════════════════════
    // C0 — ORTAK FORM ALTYAPISI (saf testler; ekran çizmeden)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void C0_sunucunun_alan_sozlugu_EKRAN_ID_altina_baglanir()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        var form = new FormDurumu();
        var hata = new ApiHata(400, "Doğrulama hatası")
        {
            Alanlar = new Dictionary<string, string[]>
            {
                ["email"] = ["Geçerli bir e-posta girin"],
                ["krediLimitiTl"] = ["Kredi limiti 0 veya daha büyük olmalıdır"],
            },
        };

        form.Sunucudan(hata, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["email"] = "cari-eposta", ["krediLimitiTl"] = "cari-limit",
        });

        Assert.False(form.Gecerli);
        Assert.Equal("Geçerli bir e-posta girin", form.Mesaj("cari-eposta"));
        Assert.Equal("Kredi limiti 0 veya daha büyük olmalıdır", form.Mesaj("cari-limit"));
        // Odak İLK hatalı alana gider.
        Assert.Equal("cari-eposta", form.IlkHatali);
        _ = dil;
    }

    [Fact]
    public void C0_403_yetki_ile_409_is_kurali_AYRI_metin_alir()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        var yetki = FormDurumu.SunucuMesaji(new ApiHata(403, "Bu işlem için gerekli rol: SAHIP veya MUDUR"), dil);
        var kural = FormDurumu.SunucuMesaji(new ApiHata(409, "Bu reyonda 2 ürün var"), dil);

        Assert.StartsWith(dil.T.Kabuk.YetkiYok, yetki);
        Assert.StartsWith(dil.T.Kabuk.IsKurali, kural);
        Assert.NotEqual(yetki, kural);
    }

    [Fact]
    public void C0_bos_ve_gecersiz_deger_TURKCE_alan_alti_mesaj_uretir()
    {
        var dil = Services.GetRequiredService<DilServisi>();
        var form = new FormDurumu();

        form.Zorunlu("ad", "   ", dil);
        form.Pozitif("gram", -5m, dil);
        form.Eposta("eposta", "gecersiz-eposta", dil);
        form.Telefon("telefon", "abc", dil);

        Assert.Equal(4, form.HataSayisi);
        Assert.Equal(dil.T.Kabuk.ZorunluAlan, form.Mesaj("ad"));
        Assert.Equal(dil.T.Kabuk.SifirdanBuyuk, form.Mesaj("gram"));
        Assert.Equal(dil.T.Kabuk.EpostaGecersiz, form.Mesaj("eposta"));
        Assert.Equal(dil.T.Kabuk.TelefonGecersiz, form.Mesaj("telefon"));
        Assert.Equal("hatali", form.Sinif("ad"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // /giris — QA turu 1+2 ORTA: "yalnız tarayıcı balonu, Türkçe mesaj yok"
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Giris_SAYFA_BASI_tasir_ve_baslik_bos_degil()
    {
        // TEMA TEMİZLİĞİ 17.10: Yıldız giriş düzeninde (tuval Giris4) giriş sekmesinde SayfaBasi çizilmez; sayfanın
        // başlığı amblem h1'idir (#giris-amblem) — "başlık boş olamaz" sözleşmesi oraya taşındı (erişilebilir ad + yazım).
        var e = Render<Kuyumcu.Web.Pages.Giris>();
        var baslik = e.Find("h1#giris-amblem");
        Assert.False(string.IsNullOrWhiteSpace(baslik.GetAttribute("aria-label")));
        Assert.False(string.IsNullOrWhiteSpace(baslik.TextContent));
    }

    [Fact]
    public void Giris_bos_formda_ALAN_ALTI_mesaj_cikar_ve_ISTEK_ATILMAZ()
    {
        var e = Render<Kuyumcu.Web.Pages.Giris>();
        istekler.Clear();

        e.FindAll("button").First(b => b.TextContent.Contains(Kuyumcu.Core.I18n.EkranlarGirisTuvalSozlugu.GirisYap("tr"))   // T-K4: tuval yazımı "Giriş yap"
                                       && b.GetAttribute("type") == "submit").Click();

        // Sessiz etkisizlik YASAK: hem mesaj var hem istek yok.
        Assert.NotNull(e.Find("#alan-hata-giris-eposta"));
        Assert.NotNull(e.Find("#alan-hata-giris-sifre"));
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/auth/giris"));
    }

    [Fact]
    public void Giris_gecersiz_eposta_TURKCE_mesaj_alir()
    {
        var e = Render<Kuyumcu.Web.Pages.Giris>();
        e.Find("#giris-eposta").Input("gecersiz-eposta");
        e.Find("#giris-sifre").Input("Parola123!");
        istekler.Clear();

        e.FindAll("button").First(b => b.GetAttribute("type") == "submit").Click();

        var mesaj = e.Find("#alan-hata-giris-eposta").TextContent;
        Assert.Contains("e-posta", mesaj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(istekler, i => i.Yol.Contains("/auth/giris"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // / (Panel)
    // ═════════════════════════════════════════════════════════════════════════

    // TEMA TEMİZLİĞİ 17.10: Panel_KARSILAMA_BASLIGI_tasir_ve_baslik_bos_degil kaldırıldı — eski karşılama gövdesi (selam #panel-selam + #panel-tarih) bilerek kaldırıldı, Panel yalnız YildizBugun; üst satırın (tarih · şube) dolu olduğu Yildiz4ATestleri'nde (#yildiz-bugun-ust-satir) sınanır.
    // TEMA TEMİZLİĞİ 17.10: Panel_kasa_ozeti_DONEMDEN_BAGIMSIZ_oldugunu_yazar kaldırıldı — eski kutucuk ızgarasının kasa kartı (#panel-kasa-donem) bilerek kaldırıldı; Yıldız Bugün yüzünde (tuval) dönem seçicisi ve bu kart yok.

    // ═════════════════════════════════════════════════════════════════════════
    // /cariler — QA turu 2 KRİTİK: pasif cari "aktif" görünüyor, dönüşü yok
    // ═════════════════════════════════════════════════════════════════════════

    // TEMA TEMİZLİĞİ 17.10: Cariler'in eski özgün listesi (SayfaBasi + tablo + satır eylemleri + süzgeç kutusu) bilerek
    // kaldırıldı; liste artık YildizMusteriler (#yildiz-musteri). Satır eylemleri satıra dokununca açılan KARTTA aynı
    // kimliklerle durur (YildizKartEylemleri: #c-aktife-al-{id}, #c-tahsilat-{id} …); tür/pasif süzgeci (#cari-tur-filtre,
    // #cari-pasifleri-goster) bağlantıdan (?tur=, ?aktif=false) gelince görünen şerittedir. Testler bu yolları sürer.
    private IRenderedComponent<Kuyumcu.Web.Pages.Cariler> CariEkrani(string? baglanti = null)
    {
        OturumAc();
        if (baglanti is not null)
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo(baglanti);
        var e = Render<Kuyumcu.Web.Pages.Cariler>();
        e.WaitForElement("#yildiz-musteri-liste");
        return e;
    }

    /// <summary>Satıra dokunur → müşteri kartı (düzenleme çekmecesi + kart eylemleri) açılır.</summary>
    private static void KartAc(IRenderedComponent<Kuyumcu.Web.Pages.Cariler> e, string cariId)
    {
        e.Find($"#yildiz-musteri-{cariId}").Click();
        e.WaitForElement("#yildiz-kart-eylemler");
    }

    [Fact]
    public void Cariler_liste_VARSAYILAN_olarak_yalniz_AKTIFLERI_ister()
    {
        var e = CariEkrani();
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/cari?") && i.Yol.Contains("aktif=true"));
        _ = e;
    }

    [Fact]
    public void Cariler_pasifleri_goster_anahtari_SUZGECI_KALDIRIR()
    {
        // Yıldız: süzgeç şeridi bağlantıdan gelen süzgeçle görünür (Cariler.Yildiz.cs YildizSuzgecEtkin).
        var e = CariEkrani("/cariler?tur=MUSTERI");
        e.WaitForElement("#yildiz-musteri-suzgec #cari-pasifleri-goster");
        istekler.Clear();

        e.Find("#cari-pasifleri-goster").Change(true);

        e.WaitForAssertion(() =>
        {
            var g = istekler.Where(i => i.Metot == "GET" && i.Yol.Contains("/cari")).ToList();
            Assert.NotEmpty(g);
            Assert.All(g, i => Assert.DoesNotContain("aktif=true", i.Yol));
        });
    }

    [Fact]
    public void Cariler_pasif_satirda_AKTIFE_AL_dugmesi_PATCH_aktif_true_gonderir()
    {
        var e = CariEkrani();
        KartAc(e, "c2");
        istekler.Clear();

        e.Find("#c-aktife-al-c2").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/cari/c2", istek.Yol);
        Assert.Equal("True", Alan(istek.Govde, "aktif"));
        // PATCH sözleşmesi: gönderilmeyen alan KORUNUR — ad/telefon taşınmaz.
        Assert.False(AlanGonderildi(istek.Govde, "ad"));
    }

    [Fact]
    public void Cariler_bos_ad_ile_kaydet_ISTEK_ATMAZ_ve_ALAN_ALTI_mesaj_verir()
    {
        var e = CariEkrani();
        e.Find("#cari-yeni-ac").Click();
        e.WaitForElement("#cari-ad");
        istekler.Clear();

        e.Find("#cari-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.NotNull(e.Find("#alan-hata-cari-ad"));
    }

    [Fact]
    public void Cariler_gecersiz_telefon_ALAN_ALTI_mesaj_verir_ve_POST_ATILMAZ()
    {
        var e = CariEkrani();
        e.Find("#cari-yeni-ac").Click();
        e.WaitForElement("#cari-ad");
        e.Find("#cari-ad").Input("Test Cari");
        // GIRDI-AILESI-2 (kurucu 08.10): telefona harf YAZILAMAZ (maske) — "abc" kutuda kalmaz;
        // geçersiz numara rakamla denenir (3 hane: sayfanın kuralı alan altında söyler).
        e.Find("#cari-telefon").Input("abc");
        Assert.Equal("", e.Find("#cari-telefon").GetAttribute("value"));
        e.Find("#cari-telefon").Input("123");
        istekler.Clear();

        e.Find("#cari-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.NotNull(e.Find("#alan-hata-cari-telefon"));
    }

    [Fact]
    public void Cariler_satir_ici_telefon_duzenlemesi_PATCH_telefon_gonderir()
    {
        // TEMA TEMİZLİĞİ 17.10: eski listenin satır içi telefon hücresi (#si-cari-telefon-c1) bilerek kaldırıldı; telefonun
        // Yıldız'daki düzeltme yolu satıra dokununca açılan karttır (#duz-telefon) — aynı uç, aynı PATCH sözleşmesi.
        var e = CariEkrani();
        KartAc(e, "c1");
        e.WaitForAssertion(() => Assert.False(e.Find("#duz-telefon").HasAttribute("disabled")));
        istekler.Clear();

        e.Find("#duz-telefon").Input("05559998877");
        e.WaitForAssertion(() => Assert.False(e.Find("#cekmece-cari-kaydet").HasAttribute("disabled")));
        e.Find("#cekmece-cari-kaydet").Click();

        e.WaitForAssertion(() =>
        {
            var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
            Assert.EndsWith("/cari/c1", istek.Yol);
            Assert.Equal(TelefonBicimi.Sunucuya("05559998877"), Alan(istek.Govde, "telefon"));
        });
    }

    [Fact]
    public void Cariler_tur_sutunu_HAM_ENUM_basmaz_ve_suzgec_BES_turu_kapsar()
    {
        var e = CariEkrani("/cariler?tur=MUSTERI");
        // UX-S31: "TEDARIKCI" ham kodu ekranda görünmez (fikstürde c2 TEDARIKCI — satırı çizilir).
        Assert.NotNull(e.Find("#yildiz-musteri-c2"));
        Assert.DoesNotContain("TEDARIKCI", e.Find("#yildiz-musteri-liste").TextContent);
        // Süzgeç: "Tümü" + beş tür.
        e.WaitForElement("#yildiz-musteri-suzgec #cari-tur-filtre");
        Assert.Equal(6, SeciciYardim.Secenekler(e, "cari-tur-filtre").Count);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // /urunler — QA turu 1+2 YÜKSEK
    // ═════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> UrunEkrani()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForElement("#sayfa-basi-urunler-baslik");
        return e;
    }

    [Fact]
    public void Urunler_pasif_satirda_AKTIFE_AL_PATCH_aktif_true_gonderir()
    {
        var e = UrunEkrani();
        istekler.Clear();

        e.Find("#u-aktife-al-u2").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/urunler/u2", istek.Yol);
        Assert.Equal("True", Alan(istek.Govde, "aktif"));
        Assert.False(AlanGonderildi(istek.Govde, "ad"));   // koru
    }

    [Fact]
    public void Urunler_duzenleme_formu_GERCEK_MILYEM_ve_BIRIM_alanlarini_TASIR()
    {
        var e = UrunEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-gercek-milyem");

        // UX-S26 paritesi: oluşturma formundaki iki alan düzenlemede de VAR.
        Assert.StartsWith("916", e.Find("#d-gercek-milyem").GetAttribute("value"));
        Assert.Equal("ADET", e.Find("#d-birim").Deger());
    }

    [Fact]
    public void Urunler_duzenleme_PATCH_govdesi_gercekMilyem_ve_birim_TASIR()
    {
        var e = UrunEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-gercek-milyem");
        e.Find("#d-gercek-milyem").Input("900");
        istekler.Clear();

        e.Find("#cekmece-urun-kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/urunler/u1", istek.Yol);
        Assert.Equal("900", Alan(istek.Govde, "gercekMilyem"));
        Assert.Equal("ADET", Alan(istek.Govde, "birim"));
    }

    /// <summary>
    /// UX-1-B devri (a): reyon seçicisi boşaltılınca gövdede `reyonId` GÖNDERİLİR
    /// ve NULL'dur — sunucu üç durumlu sözleşmeyle bunu "SİL" diye okur.
    /// </summary>
    [Fact]
    public void Urunler_reyon_secimi_bosaltilinca_govdede_reyonId_NULL_GIDER()
    {
        var e = UrunEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Düzenle")).Click();
        e.WaitForElement("#d-reyon");
        SeciciYardim.Sec(e, "d-reyon", "");
        istekler.Clear();

        e.Find("#cekmece-urun-kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.True(AlanGonderildi(istek.Govde, "reyonId"));   // alan GÖVDEDE
        Assert.Null(Alan(istek.Govde, "reyonId"));             // ve değeri NULL
    }

    [Fact]
    public void Urunler_bos_ad_ile_kaydet_ISTEK_ATMAZ_ve_ALAN_ALTI_mesaj_verir()
    {
        var e = UrunEkrani();
        e.Find("#urun-yeni-ac").Click();
        e.WaitForElement("#u-ad");
        istekler.Clear();

        e.Find("#u-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.NotNull(e.Find("#alan-hata-u-ad"));
        Assert.NotNull(e.Find("#alan-hata-u-kod"));
        Assert.NotNull(e.Find("#alan-hata-u-gram"));
    }

    /// <summary>
    /// S-2 regresyonu: Stok modalı GERÇEK yanıt şekliyle ("subeStoklar") açılır
    /// ve NRE atmaz — satır çizilir, "Beklenmeyen bir hata" bandı çıkmaz.
    /// </summary>
    [Fact]
    public void Urunler_stok_modali_GERCEK_yanitla_satiri_cizer_hata_vermez()
    {
        var e = UrunEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Stok")).Click();
        e.WaitForAssertion(() => Assert.Contains("s1", e.Markup));

        Assert.DoesNotContain("Beklenmeyen", e.Markup);
        Assert.Empty(e.FindAll("#degismez-ipucu-UrunStok"));
    }

    /// <summary>
    /// QA turu 2 ORTA: "Stok modalı salt okunur, stok girişi yolu yok".
    /// UX-S25: düğme yoksa NEDEN yok ve DOĞRU YOL ne olduğu YAZILIR.
    /// </summary>
    [Fact]
    public void Urunler_stok_BOSKEN_dugme_yerine_GEREKCE_cizilir()
    {
        stokBos = true;
        var e = UrunEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Stok")).Click();

        e.WaitForAssertion(() =>
        {
            var ipucu = e.Find("#degismez-ipucu-UrunStok");
            Assert.False(string.IsNullOrWhiteSpace(ipucu.TextContent));
            // İç paket/dalga numarası kullanıcıya GÖSTERİLMEZ (UX-S31).
            Assert.DoesNotContain("paket", ipucu.TextContent, StringComparison.OrdinalIgnoreCase);
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // /urunler/agac — QA turu 1+2 KRİTİK: reyon düzenlenemiyor
    // ═════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.UrunAgaci> AgacEkrani()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>().AyrintiAc();
        e.WaitForElement("#sayfa-basi-urun-agaci-baslik");
        return e;
    }

    [Fact]
    public void Agac_reyon_satirinda_DUZENLE_dugmesi_VAR_ve_PATCH_gonderir()
    {
        var e = AgacEkrani();
        e.Find("#r-duzenle-r1").Click();
        e.WaitForElement("#r-ad");
        e.Find("#r-ad").Input("Vitrin 1 (yeni)");
        istekler.Clear();

        e.Find("#cekmece-reyon-kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/reyonlar/r1", istek.Yol);
        Assert.Equal("Vitrin 1 (yeni)", Alan(istek.Govde, "ad"));
        Assert.Equal("VITRIN", Alan(istek.Govde, "tip"));
        // Şube gövdede GİTMEZ: sunucu bu alanı kabul etmiyor, ekran da sormuyor.
        Assert.False(AlanGonderildi(istek.Govde, "subeId"));
    }

    [Fact]
    public void Agac_reyon_pasife_alma_ONAY_ISTER_ve_onaydan_sonra_DELETE_gonderir()
    {
        var e = AgacEkrani();
        istekler.Clear();

        e.Find("#r-pasife-al-r1").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "DELETE");   // onaysız istek YOK

        e.Find("#r-pasif-onayla").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "DELETE");
        Assert.EndsWith("/reyonlar/r1", istek.Yol);
    }

    [Fact]
    public void Agac_pasif_reyonda_AKTIFE_AL_PATCH_aktif_true_gonderir()
    {
        var e = AgacEkrani();
        istekler.Clear();

        e.Find("#r-aktife-al-r2").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/reyonlar/r2", istek.Yol);
        Assert.Equal("True", Alan(istek.Govde, "aktif"));
    }

    [Fact]
    public void Agac_reyon_duzenlemede_SUBE_kilitli_ve_SEBEBI_yazar()
    {
        var e = AgacEkrani();
        e.Find("#r-duzenle-r1").Click();
        e.WaitForElement("#r-sube");

        Assert.True(e.Find("#r-sube").HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#r-sube-kilit").TextContent));
    }

    [Fact]
    public void Agac_bos_adla_kategori_kaydet_ISTEK_ATMAZ_ve_MESAJ_verir()
    {
        var e = AgacEkrani();
        e.Find("#agac-yeni-kategori").Click();
        e.WaitForElement("#k-ad");
        istekler.Clear();

        e.Find("#cekmece-kategori-kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot is "POST" or "PATCH");
        Assert.NotNull(e.Find("#alan-hata-k-ad"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // /urunler/etiketler — QA turu 2 ORTA: adet=0 ile boş sayfa basılıyordu
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// QA turu 2 ORTA: "Adet=0 ile ÜRÜN SEÇİLİYKEN 'Etiketleri Yazdır (0)' aktif
    /// ve boş sayfa yazdırıyor". Kilit hem seçim hem adet koşulunu taşımalıdır —
    /// yalnız "seçim var mı" bakan bir kilit bu bulguyu kapatmaz (M-C1-10).
    /// </summary>
    [Fact]
    public void Etiketler_URUN_SECILIYKEN_adet_sifirsa_YAZDIR_kilitli_ve_SEBEBI_yazar()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.UrunEtiketleri>().AyrintiAc();
        e.WaitForElement("#et-adet");

        // Önce ürün SEÇİLİR: kilit yalnız seçimden gelmiyor olmalı.
        e.FindAll("tbody input[type=checkbox]").First().Change(true);
        Assert.False(e.Find("#et-yazdir").HasAttribute("disabled"));   // seçim var, adet 1

        e.Find("#et-adet").Input("0");

        Assert.True(e.Find("#et-yazdir").HasAttribute("disabled"));
        Assert.Contains("(0)", e.Find("#et-yazdir").TextContent);
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#et-yazdir-kilit").TextContent));
    }

    [Fact]
    public void Etiketler_sunucu_SINIRINI_ekranda_soyler()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.UrunEtiketleri>().AyrintiAc();
        e.WaitForElement("#et-sinir");
        // TOPLU-6: sınırın altında sınır sayacın ipucunda (title); sınıra değince metinde (UX-S29).
        Assert.Contains("500", e.Find("#et-sinir").InnerHtml);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // /islemler (POS) — QA turu 1+2 KRİTİK
    // ═════════════════════════════════════════════════════════════════════════

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> PosEkrani()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-son-satislar");
        return e;
    }

    /// <summary>SATIS-2: son satışlar kartı KATLI gelir (kurucu 25.09) — liste ilk açılışta yüklenir.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> PosSonAcik()
    {
        var e = PosEkrani();
        Assert.Empty(e.FindAll("#islem-son-satislar-govde"));
        e.Find("#islem-son-satislar-ac").Click();
        e.WaitForElement("#islem-iade-et-h1");
        return e;
    }

    [Fact]
    public void Pos_kaydedilen_satislar_AYNI_EKRANDA_listelenir()
    {
        var e = PosSonAcik();
        var tablo = e.Find("#islem-son-satislar").TextContent;
        Assert.Contains("22 Ayar Çeyrek", tablo);
    }

    [Fact]
    public void Pos_storno_dugmesi_GORUNUR_ama_KILITLI_ve_GERCEK_gerekce_yazar()
    {
        var e = PosSonAcik();
        var dugme = e.Find("#islem-storno-h1");

        // Kapsam dışı türde düğme GİZLENMEZ; devre dışıdır (UX-S25/S30).
        Assert.True(dugme.HasAttribute("disabled"));

        var ipucu = e.Find("#degismez-ipucu-SatisFisi").TextContent;
        Assert.False(string.IsNullOrWhiteSpace(ipucu));
        // 48-A doğrulayıcı devri: iç paket/dalga numarası KULLANICIYA GÖSTERİLMEZ.
        Assert.DoesNotContain("paket 48", ipucu, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dalga", ipucu, StringComparison.OrdinalIgnoreCase);
    }

    // ═══ PAKET P-23 FAZ 2 (R-17) — ÖDEME KIRILIMI EKRANI ════════════════════
    // Hasım doğrulayıcı (2026-09-22): "FisOku'nun döndürdüğü odemeKalemleri
    // hiçbir sayfada gösterilmiyor" — sunucuda uç var, ekranda düğme yok
    // (UX-S25 ihlali). Bu üç test o boşluğu bekçiye bağlar.

    [Fact]
    public void P23_odeme_kirilimi_dugmesi_GERCEK_istek_atar()
    {
        // DoD EK md.1: tıklanınca hiçbir şey olmayan düğme = paket geçmez.
        var e = PosSonAcik();
        istekler.Clear();

        e.Find("#islem-kirilim-h1").Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Item1 == "GET" && i.Item2.Contains("/islemler/fis/h1")));
    }

    [Fact]
    public void P23_kirilim_kalemleri_ARAC_ADIYLA_ve_para_birimiyle_yazilir()
    {
        var e = PosSonAcik();
        e.Find("#islem-kirilim-h1").Click();

        e.WaitForAssertion(() =>
        {
            var govde = e.Find("#islem-kirilim-govde-h1").TextContent;
            // UX-S31: ham kod ekrana BASILMAZ, sözlükten çevrilir.
            Assert.DoesNotContain("NAKIT", govde);
            Assert.DoesNotContain("VERESIYE", govde);
            Assert.Contains("Nakit", govde);
            Assert.Contains("Veresiye", govde);
            // Geri doldurulmuş satır rozetle söylenir (kaynağı gizlenmez).
            Assert.Contains("Geçmiş kayıttan türetildi", govde);
        });
    }

    [Fact]
    public void P23_kapsam_bayragi_ekranda_TURKCE_yazar_ham_kod_gorunmez()
    {
        var e = PosSonAcik();
        e.Find("#islem-kirilim-h1").Click();

        e.WaitForAssertion(() =>
        {
            var kapsam = e.Find("#islem-kirilim-kapsam").TextContent;
            Assert.DoesNotContain("KAYITLI", kapsam);
            Assert.Contains("Kırılım kayıtlı", kapsam);
        });
    }

    [Fact]
    public void Pos_satirdan_IADE_ET_iade_kipine_gecirir_ve_kaynagi_secer()
    {
        var e = PosSonAcik();
        istekler.Clear();

        e.Find("#islem-iade-et-h1").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#iade-kaynak")));
        Assert.Equal("h1", SeciciYardim.SeciliDeger(e, "iade-kaynak"));
    }

    [Fact]
    public void Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir()
    {
        var e = PosEkrani();
        e.Find("#islem-adet").Input("0");
        e.Find("#islem-birimfiyat").Input("-5");
        istekler.Clear();

        e.Find("#islem-kalem-ekle").Click();

        // ⚠ KARARSIZLIK (S-3c ölçümü): Ekle işleyicisi ASENKRONDUR; tam süit
        // yükü altında çizim tıklamadan hemen sonra tamamlanmıyor ve bu iki
        // satır bekleme olmadan kırılıyordu (tek başına 35/35 yeşil, süitte
        // kırmızı). Ürün davranışı değil, testin yarışı — bekleme eklendi.
        e.WaitForAssertion(() =>
        {
            Assert.NotNull(e.Find("#alan-hata-islem-adet"));
            Assert.NotNull(e.Find("#alan-hata-islem-birimfiyat"));
        });
        Assert.DoesNotContain(e.FindAll("tbody tr td button"), b => b.TextContent.Contains("Sil"));
    }

    [Fact]
    public void Pos_kilitli_KAYDET_dugmesi_SEBEBINI_soyler()
    {
        var e = PosEkrani();
        // TEMA TEMİZLİĞİ 17.10: satış fişi Yıldız tezgâhıdır — Kaydet #yildiz-tezgah-kaydet (özgün #islem-kaydet satışta yok).
        // Boş formda Kaydet kilitli — ama artık NEDEN kilitli olduğu yazıyor (düğmenin title'ı = KaydetKilitSebebi; boş fişte
        // kalem alanının "boş" cümlesi görünür).
        var kaydet = e.WaitForElement("#yildiz-tezgah-kaydet");
        Assert.True(kaydet.HasAttribute("disabled"));
        Assert.False(string.IsNullOrWhiteSpace(kaydet.GetAttribute("title")));
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#yildiz-tezgah-bos").TextContent));
    }

    [Fact]
    public void Pos_kalem_satirinda_ADET_satir_ici_duzenlenir()
    {
        // TEMA TEMİZLİĞİ 17.10: satış fişi Yıldız tezgâhıdır — eski kalem tablosunun satır içi tetikleyicisi
        // (#si-kalem-adet-u1-ac) satışta yok; kalem satırına dokununca (#yildiz-kalem-0) açılan düzenlemede (PRO) adet
        // aynı satırda düzeltilir (YildizKalemYaz — FisKalemTablosu.AdetDegistir'in aynası; alan anahtarı si-kalem-adet-u1).
        // Müşteri seçimi bu iddianın parçası değil (Yıldız'da "Değiştir" katmanında) — yalnız kalem girilir.
        var e = PosEkrani();
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.WaitForAssertion(() => Assert.NotEqual("", e.Find("#islem-birimfiyat").GetAttribute("value")));
        e.Find("#islem-adet").Input("1");
        e.Find("#islem-kalem-ekle").Click();

        // Kalem ekleme asenkron: satır çizilmeden tetikleyici aranırsa paralel yük altında
        // yarış oluşur (doğrulama koşumu 2026-09-18). Satır beklenir, sonra tıklanır.
        e.WaitForElement("#yildiz-kalem-0").Click();
        e.WaitForElement("#yildiz-kalem-0-adet").Change("3");

        e.WaitForAssertion(() =>
        {
            Assert.StartsWith(Kuyumcu.Core.I18n.YildizTezgahSozlugu.Adet("3", "tr"),
                e.Find("#yildiz-kalem-0 .yz-tezgah-ayrinti").TextContent.Trim());
            Assert.Equal("3", e.Find("#yildiz-kalem-0-adet").GetAttribute("value"));
        });
        Assert.Empty(e.FindAll("#alan-hata-si-kalem-adet-u1"));
    }
}
