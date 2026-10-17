using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ÜRÜN EKRANI — HER İŞLEV TEK TEK
//
// TESPİT (2026-09-14): ürün modülünün uçlarından yalnız 3'ü ekranda
// kullanılıyordu (listele · oluştur · şube stoku). Ekranı olmayanlar:
//   PATCH  /urunler/{id}                 — düzenleme
//   DELETE /urunler/{id}                 — pasife alma
//   GET    /urunler/{id}                 — tam kart (liste yalnız özet taşır)
//   GET    /urunler/barkod/{kod}         — POS'un günlük işi
//   GET    /urunler/{id}/etiket          — md.11/(e) zorunlu ibare
//   PATCH  /urunler/{id}/stok/{subeId}   — sayım düzeltme
//   GET/POST/PATCH /urun-kategorileri    — ağaç API'de vardı, girişi yoktu
//   GET/POST       /reyonlar             — aynı
//
// "Düğme var" yetmez: hangi uca, hangi gövdeyle gidildiği kontrol edilir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UrunEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string UrunListesi = """
        [{"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
          "gramAgirlik":15.5,"tasAgirlik":0,"hasGrami":14.19,"barkod":"869001","aktif":true},
         {"id":"u2","tip":"PIRLANTA","kod":"YZK-07","ad":"Tektaş Yüzük","ayarId":"K18",
          "gramAgirlik":3.2,"tasAgirlik":0.25,"hasGrami":2.4,"barkod":"869002","aktif":true,
          "tasKokeni":"SENTETIK","tasIbaresi":"sentetik"}]
        """;

    // ⚠ Tam kart listede OLMAYAN alanları taşır. Düzenleme bunları çekmezse,
    //   kaydet dendiğinde kategori/reyon bağları sessizce kopardı.
    private const string TamKart = """
        {"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
         "gramAgirlik":15.5,"tasAgirlik":0,"iscilikTl":250,"barkod":"869001",
         "tasKokeni":"TAS_YOK","tasIbaresi":null,
         "kategoriId":"k2","reyonId":"r1","satisFiyati":null}
        """;

    // ⚠ PAKET 4: PIRLANTA kartı 4C + sertifika taşır; düzenleme ekranında
    //   girişi YOKTU (A16 kalan açığı).
    private const string PirlantaTamKart = """
        {"id":"u2","tip":"PIRLANTA","kod":"YZK-07","ad":"Tektaş Yüzük","ayarId":"K18",
         "gramAgirlik":3.2,"tasAgirlik":0.25,"iscilikTl":500,"barkod":"869002",
         "tasKokeni":"SENTETIK","tasIbaresi":"sentetik",
         "kategoriId":null,"reyonId":null,"satisFiyati":null,
         "pirlantaKarat":0.52,"pirlantaRenk":"G","pirlantaBerraklik":"VS1",
         "pirlantaKesim":"EXCELLENT","sertifikaKurumu":"GIA","sertifikaNo":"GIA-77"}
        """;

    private const string Agac = """
        [{"id":"k1","ad":"Altın","sira":0,"aktif":true,"urunSayisi":2,
          "altlar":[{"id":"k2","ad":"Bilezik","sira":0,"aktif":true,"urunSayisi":1,"altlar":[]}]}]
        """;

    private const string Reyonlar = """
        [{"id":"r1","subeId":"s1","ad":"Ön Vitrin","tip":"VITRIN","aktif":true,"urunSayisi":3}]
        """;

    public UrunEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // S3-3: Urunler.razor basit/profesyonel mod okur
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            // Sorgu dizesi DE kaydedilir: süzgeçlerin gerçekten uca gittiği
            // ancak böyle kanıtlanır.
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                var y when y.Contains("/urunler/barkod/") => """
                    {"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
                     "gramAgirlik":15.5,"tasAgirlik":0,"hasGrami":14.19,"barkod":"869001","aktif":true}
                    """,
                var y when y.EndsWith("/etiket") => """
                    {"urunId":"u2","kod":"YZK-07","ad":"Tektaş Yüzük","ibare":"sentetik",
                     "ibareZorunlu":true,"etiketMetni":"Tektaş Yüzük (sentetik)","barkod":"869002",
                     "ayarId":"K18","milyem":750,"gramAgirlik":3.2,"hasGrami":2.4,
                     "dayanak":"Kuyum Ticareti Yönetmeliği md.11/(e)"}
                    """,
                // GERÇEK sunucu şekli (UrunModulu.StokListele): zarf + "subeStoklar".
                // Eskiden burada {"stok":[…]} vardı; sahte yanıt gerçeğe uymadığı için
                // Urunler.razor'daki alan adı uyumsuzluğu (NullReferenceException) testlerden kaçmıştı (S-2).
                // PAKET ISTEK-1 — liste sütunu TEK toplu istekle okunur (UrunStokOzetController şekli).
                var y when y.Contains("/urunler/stok-adetleri") => """
                    {"urunler":[{"urunId":"u1","toplamAdet":4.250,"toplamHasGram":56.7},
                                {"urunId":"u2","toplamAdet":0,"toplamHasGram":0}]}
                    """,
                var y when y.EndsWith("/stok") => """
                    {"urunId":"u1","toplamAdet":4.250,"toplamHasGram":56.7,
                     "subeStoklar":[{"subeId":"s1","adet":4.250,"toplamHasGram":56.7,"guncellendi":"2026-09-17T00:00:00"}]}
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
                var y when y.Contains("/urun-kategorileri") => Agac,
                var y when y.Contains("/reyonlar") => Reyonlar,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var y when y.EndsWith("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var y when y.EndsWith("/urunler/u1") => TamKart,
                var y when y.EndsWith("/urunler/u2") => PirlantaTamKart,
                var y when y.Contains("/belgeler") => "[]",
                var y when y.Contains("/urunler") => UrunListesi,
                // P-10: Urunler ekrani parite kodlarini TEK KAYNAKTAN okur (UX-S31).
                var p10p when p10p.Contains("/kur/pariteler") =>
                    """{"kaynak":"TEST","pariteler":[{"kod":"BILEZIK_22","ad":"22 Ayar Bilezik","alis":6800,"satis":6900,"birim":"GRAM"}]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        istekler.Clear();
        return e;
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private static AngleSharp.Dom.IElement Dugme(
        IRenderedComponent<Kuyumcu.Web.Pages.Urunler> e, string metin)
        => e.FindAll("button").First(b => b.TextContent.Contains(metin));

    // ── Düğmeler ─────────────────────────────────────────────────────────────

    [Fact]
    public void Her_urun_satirinda_DUZENLE_dugmesi_vardir()
    {
        var e = Ciz();
        Assert.Equal(2, e.FindAll("button").Count(b => b.TextContent.Contains("Düzenle")));
    }

    [Fact]
    public void Her_urun_satirinda_ETIKET_ve_PASIFE_AL_dugmesi_vardir()
    {
        var e = Ciz();
        Assert.Equal(2, e.FindAll("button").Count(b => b.TextContent.Contains("Etiket")));
        Assert.Equal(2, e.FindAll("button").Count(b => b.TextContent.Contains("Pasife Al")));
    }

    // ── Düzenleme ────────────────────────────────────────────────────────────

    [Fact]
    public void Duzenle_acilinca_TAM_KART_cekilir()
    {
        // Kategori ve reyon listede YOK. Tam kart çekilmezse bu bağlar silinir.
        var e = Ciz();
        Dugme(e, "Düzenle").Click();

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/urunler/u1"));
    }

    [Fact]
    public void Duzenle_kaydedince_PATCH_gonderilir_ve_kategori_reyon_KORUNUR()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        istekler.Clear();
        Dugme(e, "Kaydet").Click();

        // Kaydetmenin ardından liste tazelenir; ilgilendiğimiz PATCH isteği.
        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/urunler/u1", istek.Yol);
        Assert.Equal("k2", Alan(istek.Govde, "kategoriId"));
        Assert.Equal("r1", Alan(istek.Govde, "reyonId"));
    }

    [Fact]
    public void Duzenlemede_isim_degisikligi_govdeye_yansir()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        e.Find("#d-ad").Input("Burma Bilezik 22A");
        istekler.Clear();
        Dugme(e, "Kaydet").Click();

        Assert.Equal("Burma Bilezik 22A", Alan(istekler[0].Govde, "ad"));
    }

    // ── Barkod ───────────────────────────────────────────────────────────────

    [Fact]
    // ⚠ P-37 (kurucu 21 Eylül 16:30: "inputlar seçme yerleri dengesiz") —
    //   /urunler'deki AYRI barkod kutusu ve "Barkod okut" düğmesi KALDIRILDI;
    //   mükerrer arama yüzeyi 0. Barkod okutma ANA arama kutusunun Enter'ına
    //   indi (okutucu donanımı zaten Enter basar). Test silinmedi, TEK yüzeyin
    //   aynı işi yaptığını ölçecek şekilde çevrildi.
    public void Barkod_TEK_arama_kutusundan_okutulur()
    {
        var e = Ciz();
        // TEK arama yüzeyi: ekranda başka bir type=search kutusu YOKTUR.
        Assert.Single(e.FindAll("#urun-arama"));
        var kutu = e.Find("#urun-arama");
        kutu.Input("869001");
        kutu.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/urunler/barkod/869001"));
    }

    [Fact]
    public void Barkod_ENTER_ile_de_aranir()
    {
        // Okutucu donanımı okumayı Enter ile bitirir; düğmeye basılmaz.
        var e = Ciz();
        var kutu = e.Find("#urun-arama");
        kutu.Input("869002");
        kutu.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        Assert.Contains(istekler, i => i.Yol.EndsWith("/urunler/barkod/869002"));
    }

    // ── Etiket ───────────────────────────────────────────────────────────────

    [Fact]
    public void Etiket_ucu_cagrilir_ve_ZORUNLU_IBARE_gosterilir()
    {
        // md.11/(e): sentetik taşlı ürün ibaresi AÇIKÇA görünmeden satışa sunulamaz.
        var e = Ciz();
        e.FindAll("button").Where(b => b.TextContent.Contains("Etiket")).Last().Click();

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/etiket"));
        Assert.Contains("Tektaş Yüzük (sentetik)", e.Markup);
    }

    // ── Sayım düzeltme ───────────────────────────────────────────────────────

    [Fact]
    public void Stok_modalinda_SAYIM_DUZELT_dugmesi_vardir()
    {
        var e = Ciz();
        Dugme(e, "Stok").Click();

        Assert.Contains(e.FindAll("button"), b => b.TextContent.Contains("Sayım Düzelt"));
    }

    /// <summary>
    /// S-2: sunucu "subeStoklar" döndürürken istemci "stok" bekliyordu; alan null kalınca
    /// modal açılışında NullReferenceException fırlıyor ve kullanıcıya "Beklenmeyen bir hata
    /// oluştu" bandı çıkıyordu (QA turu 2, /urunler). Sahte API artık GERÇEK şekli döndürür.
    /// </summary>
    [Fact]
    public void Stok_modali_GERCEK_yanit_seklinde_satirlari_cizer_hata_vermez()
    {
        var e = Ciz();
        Dugme(e, "Stok").Click();

        var metin = e.Find("[role=dialog][aria-modal=true]").TextContent;   // S3-3: satır "⋯" menüsü de role=dialog (modal değil)
        // S3-3 / QA tur 8: şube satırı ADIYLA çizilir (GET /subeler), ham kimlik değil.
        Assert.Contains("Merkez", metin);                   // şube satırı çizildi
        Assert.Contains("4", metin);                        // adet
        Assert.DoesNotContain("Beklenmeyen", e.Markup);     // çalışma zamanı hatası bandı YOK
        Assert.Empty(e.FindAll(".bos-durum"));              // boş durum gösterilmedi
    }


    /// <summary>
    /// QA turu 3 (2026-09-17), /urunler — İKİ KRİTİK: sunucu adedi decimal(12,3) döndürür
    /// ("adet":5.000); istemci record'unda alan <c>int</c> olduğu için System.Text.Json
    /// çözümlemesi patlıyor, modal "stok yok" gösteriyor ve hata render ağacını öldürdüğü
    /// için sayfadaki sonraki kaydetmeler de sessizce çalışmıyordu. Sahte yanıt artık
    /// ondalık adet döndürür; tip sözleşmesi ad + TİP olarak bekçilenir.
    /// </summary>
    [Fact]
    public void Stok_modali_ONDALIK_adeti_cozer_ve_satiri_cizer()
    {
        var e = Ciz();
        Dugme(e, "Stok").Click();

        var metin = e.Find("[role=dialog][aria-modal=true]").TextContent;   // S3-3: satır "⋯" menüsü de role=dialog (modal değil)
        Assert.Contains("Merkez", metin);
        // Kültürden bağımsız: ayırıcı virgül de nokta da olabilir.
        Assert.True(metin.Contains("4,25") || metin.Contains("4.25"),
            $"Ondalık adet çizilmedi. Modal metni: {metin}");
        Assert.DoesNotContain("Beklenmeyen", e.Markup);
        Assert.Empty(e.FindAll(".bos-durum"));
    }

    /// <summary>
    /// Aynı kökün ikinci yüzü: modal açıldıktan SONRA sayfadaki düzenleme kaydı hâlâ
    /// çalışmalı (render ağacı ölmemeli). Çözümleme hatası geri gelirse bu test kırmızı olur.
    /// </summary>
    [Fact]
    public void Stok_modali_acildiktan_SONRA_urun_duzenleme_kaydi_ISTEK_gonderir()
    {
        var e = Ciz();
        Dugme(e, "Stok").Click();
        e.Find("[role=dialog][aria-modal=true] button[aria-label], [role=dialog][aria-modal=true] .modal-kapat").Click();
        istekler.Clear();

        Dugme(e, "Düzenle").Click();
        e.Find("#d-ad").Input("Yeni Ad");
        Dugme(e, "Kaydet").Click();

        Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("/urunler/u1"));
    }

    [Fact]
    public void Sayim_kaydedilince_dogru_SUBE_ucuna_PATCH_gider()
    {
        var e = Ciz();
        Dugme(e, "Stok").Click();
        Dugme(e, "Sayım Düzelt").Click();
        e.Find("#s-adet").Input("3");
        istekler.Clear();
        Dugme(e, "Kaydet").Click();

        var istek = istekler.First(i => i.Metot == "PATCH");
        Assert.EndsWith("/urunler/u1/stok/s1", istek.Yol);
        Assert.Equal("3", Alan(istek.Govde, "adet"));
    }

    // ── Pasife alma ──────────────────────────────────────────────────────────

    [Fact]
    public void Pasife_alma_ONAY_ister_once_istek_gitmez()
    {
        // Yanlışlıkla tıklanan bir düğme ürünü satıştan düşürmemeli.
        var e = Ciz();
        Dugme(e, "Pasife Al").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "DELETE");
    }

    [Fact]
    public void Pasife_alma_onaylaninca_DELETE_gonderilir()
    {
        var e = Ciz();
        Dugme(e, "Pasife Al").Click();
        e.FindAll("button").Where(b => b.TextContent.Contains("Pasife Al")).Last().Click();

        Assert.Contains(istekler, i => i.Metot == "DELETE" && i.Yol.EndsWith("/urunler/u1"));
    }

    // ── Kategori / reyon bağlama ─────────────────────────────────────────────

    [Fact]
    public void Ekran_acilinca_kategori_agaci_ve_reyonlar_yuklenir()
    {
        Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();

        Assert.Contains(istekler, i => i.Yol.EndsWith("/urun-kategorileri"));
        Assert.Contains(istekler, i => i.Yol.EndsWith("/reyonlar"));
    }

    [Fact]
    public void Yeni_urun_formunda_kategori_agaci_ic_ice_gosterilir()
    {
        var e = Ciz();
        Dugme(e, "Yeni ürün").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da

        var secenekler = SeciciYardim.Secenekler(e, "u-kategori")
            .Select(o => o.Ad()).ToList();
        Assert.Contains("Altın", secenekler);
        Assert.Contains("Altın › Bilezik", secenekler);   // seviye görünür olmalı
    }

    [Fact]
    public void Kategori_suzgeci_listeyi_KATEGORI_ID_ile_daraltir()
    {
        var e = Ciz();
        // P-36c: ham <select> → ortak <Secici>; süzgeç artık KİMLİKLE bulunur.
        SeciciYardim.Sec(e, "urun-kategori-filtre", "k2");

        Assert.Contains(istekler, i => i.Yol.Contains("kategoriId=k2"));
    }

    [Fact]
    public void Reyon_suzgeci_listeyi_REYON_ID_ile_daraltir()
    {
        var e = Ciz();
        SeciciYardim.Sec(e, "urun-reyon-filtre", "r1");

        Assert.Contains(istekler, i => i.Yol.Contains("reyonId=r1"));
    }

    [Fact]
    public void Konsinye_isaretlenince_SAHIP_ve_BEDEL_sorulur()
    {
        // Konsinye kapısı: sahibi/bedeli olmadan kaydedilemez (sunucu reddeder).
        var e = Ciz();
        Dugme(e, "Yeni ürün").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da
        Assert.Empty(e.FindAll("#u-kons-cari"));

        // Kimliğe bağlan: forma yeni onay kutuları (rodyum kaplama) eklenince
        // "ilk checkbox" varsayımı kırılıyordu.
        e.Find("#u-konsinye").Change(true);

        Assert.Single(e.FindAll("#u-kons-cari"));
        Assert.Single(e.FindAll("#u-kons-bedel"));
    }

    /// <summary>Paket 22: PIRLANTA seçilince 4C alanı çizilir; karatsız Kaydet KAPALI (sunucu da 400 verir).</summary>
    [Fact]
    public void PIRLANTA_tipinde_4C_alani_cizilir_ve_karatsiz_Kaydet_KAPALI()
    {
        var e = Ciz();
        Dugme(e, "Yeni ürün").Click();
        Assert.Empty(e.FindAll("#u-4c"));
        SeciciYardim.Sec(e, "u-tip", "PIRLANTA");
        e.WaitForElement("#u-4c");
        e.Find("#u-ad").Input("Tektaş"); e.Find("#u-kod").Input("P-1"); e.Find("#u-gram").Input("3");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
        Assert.True(Dugme(e, "Kaydet").HasAttribute("disabled"));
        e.Find("#u-karat").Input("0.52");
        e.WaitForAssertion(() => Assert.False(Dugme(e, "Kaydet").HasAttribute("disabled")));
    }
    // ── 4C / sertifika düzenleme (paket 4, A16 kalan açığı) ──────────────────

    [Fact]
    public void PIRLANTA_urununde_duzenleme_modalinda_4C_alanlari_DOLU_gelir()
    {
        var e = Ciz();
        e.FindAll("button").Where(b => b.TextContent.Contains("Düzenle")).ElementAt(1).Click();

        // P-78: girdi kutusu değeri tr-TR gruplamasız basılır (virgül ondalık); eski
        // Invariant yazım ("1.234") tr-TR çözücüde BELİRSİZ sayılıp kutu kendi değerini
        // geri okuyamazdı. Değer aynı, yazım değişti.
        e.WaitForAssertion(() => Assert.Equal("0,52", e.Find("#d-karat").GetAttribute("value")));
        Assert.Equal("G", e.Find("#d-renk").Deger());
        Assert.Equal("VS1", e.Find("#d-berraklik").Deger());
        Assert.Equal("GIA", e.Find("#d-sert-kurum").Deger());
        Assert.Equal("GIA-77", e.Find("#d-sert-no").GetAttribute("value"));
    }

    [Fact]
    public void PIRLANTA_4C_degisikligi_PATCH_govdesine_yansir()
    {
        var e = Ciz();
        e.FindAll("button").Where(b => b.TextContent.Contains("Düzenle")).ElementAt(1).Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-sert-no")));
        e.Find("#d-sert-no").Input("GIA-99");
        istekler.Clear();
        Dugme(e, "Kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        var istek = istekler.First(i => i.Metot == "PATCH");
        Assert.Equal("GIA-99", Alan(istek.Govde, "sertifikaNo"));
        Assert.Equal("0.52", Alan(istek.Govde, "pirlantaKarat"));
    }

    [Fact]
    public void ZIYNET_urununde_4C_alanlari_GORUNMEZ_ve_govdede_NULL_gider()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-ad")));
        Assert.Empty(e.FindAll("#d-karat"));

        istekler.Clear();
        Dugme(e, "Kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        Assert.Null(Alan(istekler.First(i => i.Metot == "PATCH").Govde, "pirlantaKarat"));
    }

    // ── Belge yükleyici (paket 4) ────────────────────────────────────────────

    [Fact]
    public void Duzenleme_modalinda_BELGE_YUKLEYICI_vardir_ve_urune_baglidir()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#belge-sec")));
        Assert.NotNull(e.Find("#belge-yukle"));
        Assert.Contains(istekler, i => i.Yol.Contains("/belgeler?sahipTuru=URUN&sahipId=u1"));
    }


    // ═════════════════════════════════════════════════════════════════════════
    // PAKET 20 — MADEN SEÇİMİ, SAFLIK TABLOSU VE ÇELİK KURALI
    //
    // Kanıtlanan şey: ekrandaki ayar/tip listeleri ELLE YAZILMAZ, Kuyumcu.Core
    // tek kaynağından gelir; maden seçilince YALNIZ o madenin ayarları listelenir
    // ve milyemleri GÖRÜNÜR; çelik seçilince fiyatsız kaydetme SESSİZCE
    // etkisiz kalmaz, alan altında Türkçe mesaj çıkar.
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Maden_secilince_YALNIZ_o_madenin_ayarlari_ve_milyemleri_listelenir()
    {
        var e = Ciz();
        Dugme(e, "Yeni ürün").Click();
        // ⚠ KARARSIZLIK ÖNLEMİ (P-36c/2): modal açılışı asenkron; yüklü makinede
        //   ilk Find hemen koşarsa "#u-ayar yok" der. Beklemek testi ZAYIFLATMAZ,
        //   yalnız yarışı kaldırır (Anayasa md.1: kararsızlık belgelenip geçilmez).
        e.WaitForElement("#u-ayar");

        // Varsayılan ALTIN: gümüş/platin ayarları listede OLMAMALI.
        // KESIF-P4 #79/#84: seçenek METNİ ad ("22 Ayar (916‰)"), DEĞERİ kod —
        // madene göre süzme kodla (değer) sınanır, ham kod metinde aranmaz.
        var altinSecenekleri = SeciciYardim.ListeMetni(e, "u-ayar");
        var altinDegerleri = SeciciYardim.Secenekler(e, "u-ayar").Select(o => o.Deger()).ToList();
        Assert.Contains("K22", altinDegerleri);
        Assert.Contains("916", altinSecenekleri);              // saflık tablosu görünür (PRO)
        Assert.DoesNotContain("G925", altinDegerleri);
        Assert.DoesNotContain("PT950", altinDegerleri);

        // Ürün grubu ALTIN dayatmayan bir gruba çekilir ki maden seçici açılsın.
        SeciciYardim.Sec(e, "u-tip", "SAAT");
        SeciciYardim.Sec(e, "u-maden", "PLATIN");

        var platinSecenekleri = SeciciYardim.ListeMetni(e, "u-ayar");
        var platinDegerleri = SeciciYardim.Secenekler(e, "u-ayar").Select(o => o.Deger()).ToList();
        Assert.Contains("PT950", platinDegerleri);
        Assert.Contains("950", platinSecenekleri);
        Assert.DoesNotContain("K22", platinDegerleri);
        // P-URUN (K6): + boş "Ayar seçin" seçeneği (varsayılan yok) — dolu seçenekler madenin ayarları kadar.
        Assert.Equal(
            Kuyumcu.Core.Domain.Ayarlar.MadeninAyarlari("PLATIN").Count(),
            SeciciYardim.Secenekler(e, "u-ayar").Count(o => o.Deger() != ""));
    }

    /// <summary>
    /// Ürün grubu bir maden DAYATIYORSA seçici kilitlenir ve sebebi yazılır
    /// (UX-S22 + UX-S30: görünmez kural olmaz).
    /// </summary>
    [Fact]
    public void Urun_grubu_madeni_dayatiyorsa_secici_KILITLI_ve_sebebi_YAZILI()
    {
        var e = Ciz();
        Dugme(e, "Yeni ürün").Click();
        SeciciYardim.Sec(e, "u-tip", "GUMUS");

        Assert.True(e.Find("#u-maden").HasAttribute("disabled"));
        Assert.Contains("gümüş", e.Find("#u-maden-kilit").TextContent);
        // Ayar da o madene çekilmiş olmalı — aksi hâlde sunucu 400 verirdi.
        var kilitliAyarlar = SeciciYardim.ListeMetni(e, "u-ayar");
        Assert.Contains("G", kilitliAyarlar);
        Assert.DoesNotContain("K22", kilitliAyarlar);
    }

    [Fact]
    public void Celik_secilince_has_hesaplanmaz_uyarisi_cikar_ve_fiyatsiz_kaydet_SESSIZ_KALMAZ()
    {
        var e = Ciz();
        Dugme(e, "Yeni ürün").Click();
        SeciciYardim.Sec(e, "u-tip", "CELIK");

        Assert.Single(e.FindAll("#u-kiymetsiz-uyari"));
        Assert.Contains("has gramı", e.Find("#u-kiymetsiz-uyari").TextContent);

        // Zorunlu alanlar dolu, YALNIZ satış fiyatı boş → istek ATILMAZ,
        // alan altında Türkçe mesaj çıkar (sessiz etkisizlik yasağı).
        e.Find("#u-ad").Input("Çelik saat");
        e.Find("#u-kod").Input("CLK-1");
        e.Find("#u-gram").Input("40");
        istekler.Clear();
        Dugme(e, "Kaydet").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        // P-36c/3: alan artık ortak kabukta (Bilesenler/Form/AlanKabugu) çizilir;
        // hata metni kutunun DOĞRUDAN ebeveyninde değil, alan sarmalayıcısında
        // (#<id>-sar) durur. Kimlik korundu, okunan yer sözleşmeye göre güncellendi.
        Assert.Contains("satış fiyatı", e.Find("#u-satis-fiyati-sar").TextContent,
                        StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Celik_fiyatiyla_KAYDEDILIR_ve_govde_ayari_CELIK_tasir()
    {
        var e = Ciz();
        Dugme(e, "Yeni ürün").Click();
        SeciciYardim.Sec(e, "u-tip", "CELIK");
        e.Find("#u-ad").Input("Çelik saat");
        e.Find("#u-kod").Input("CLK-1");
        e.Find("#u-gram").Input("40");
        e.Find("#u-satis-fiyati").Input("2500");

        istekler.Clear();
        Dugme(e, "Kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST"));

        var govde = istekler.First(i => i.Metot == "POST").Govde;
        Assert.Equal("CELIK", Alan(govde, "ayarId"));
        Assert.Equal("CELIK", Alan(govde, "tip"));
        Assert.Equal("2500", Alan(govde, "satisFiyati"));
    }

    [Fact]
    public void Rodyum_kutusu_kaplanamayan_madende_KILITLI_ve_govdeye_false_gider()
    {
        var e = Ciz();
        Dugme(e, "Yeni ürün").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da

        // Altında açık:
        Assert.False(e.Find("#u-rodyum").HasAttribute("disabled"));
        e.Find("#u-rodyum").Change(true);

        // Çeliğe geçince KİLİTLENİR ve işaret DÜŞER (sunucu da reddederdi).
        SeciciYardim.Sec(e, "u-tip", "CELIK");
        Assert.True(e.Find("#u-rodyum").HasAttribute("disabled"));

        e.Find("#u-ad").Input("Çelik saat");
        e.Find("#u-kod").Input("CLK-2");
        e.Find("#u-gram").Input("40");
        e.Find("#u-satis-fiyati").Input("2500");
        istekler.Clear();
        Dugme(e, "Kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST"));
        Assert.Equal("False", Alan(istekler.First(i => i.Metot == "POST").Govde, "rodyumKaplama"));
    }

    /// <summary>
    /// K20-8: listeler ELLE YAZILMAZ. Ürün grubu listesi API'nin kabul ettiği
    /// 11 grubun TAMAMINI gösterir — eskiden 7 tanesini gösteriyor, platin
    /// alyans ekrandan HİÇ kaydedilemiyordu.
    /// </summary>
    [Fact]
    public void Urun_grubu_ve_ayar_listeleri_TEK_KAYNAKTAN_gelir()
    {
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Urunler.razor");
        Assert.Contains("Kuyumcu.Core.Domain.UrunTipleri.Hepsi", kaynak);
        Assert.Contains("Kuyumcu.Core.Domain.Maden.Hepsi", kaynak);

        var e = Ciz();
        Dugme(e, "Yeni ürün").Click();
        Assert.Equal(Kuyumcu.Core.Domain.UrunTipleri.Hepsi.Length, SeciciYardim.Secenekler(e, "u-tip").Count);
        var uTipMetni = SeciciYardim.ListeMetni(e, "u-tip");
        // S3-3 (HAM_KOD_EKRANDA = 0, UX-S31): seçenek DEĞERİ kod, GÖRÜNEN adı sözlükten.
        Assert.Contains("Platin", uTipMetni);
        Assert.Contains("Çelik", uTipMetni);
        Assert.DoesNotContain("DEGERLI_TAS", uTipMetni);
        // P-36c: maden seçicisi ürün grubu maden DAYATTIĞINDA kilitlidir
        // (UX-S22); listeyi sayabilmek için önce serbest bir gruba geçilir.
        SeciciYardim.Sec(e, "u-tip", "SAAT");
        Assert.Equal(Kuyumcu.Core.Domain.Maden.Hepsi.Length, SeciciYardim.Secenekler(e, "u-maden").Count);
    }

    /// <summary>Maden filtresi gerçekten uca gider (UX-S24 · süzgeç sunucuda).</summary>
    [Fact]
    public void Maden_filtresi_uca_gider()
    {
        var e = Ciz();
        istekler.Clear();
        SeciciYardim.Sec(e, "urun-maden-filtre", "GUMUS");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.Contains("maden=GUMUS")));
    }

    /// <summary>UX-S26: düzenleme formu oluşturma formuyla AYNI alan kümesini taşır.</summary>
    [Fact]
    public void Duzenleme_formu_da_maden_saflik_ve_rodyum_tasir()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#d-ad")));

        Assert.NotNull(e.Find("#d-maden"));
        Assert.NotNull(e.Find("#d-rodyum"));
        Assert.Contains("916", e.Find("#d-ayar").InnerHtml);     // saflık görünür

        istekler.Clear();
        Dugme(e, "Kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "PATCH"));
        Assert.Equal("False", Alan(istekler.First(i => i.Metot == "PATCH").Govde, "rodyumKaplama"));
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — "görünen ≠ giden" fiş DIŞI formlarda ═══════════════
    // Canlı: /urunler + Yeni Ürün > İşçilik Tutarı = "1.500" > Kaydet → POST 201, gövdede "iscilikTl":0,
    // kutu hatasız "0". Tuş tuş yazım (gerçek klavye) ara değerleri de üretir: 1 · 1. · 1.5 · 1.50 · 1.500.
    private static readonly string[] OK1TusTus1500 = ["1", "1.", "1.5", "1.50", "1.500"];

    private static void OK1Yaz(IRenderedComponent<Kuyumcu.Web.Pages.Urunler> e, string id, IEnumerable<string> adimlar)
    {
        foreach (var a in adimlar) e.Find($"#{id}").Input(a);
        e.Find($"#{id}").Blur();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> OK1Ciz(string mod)
    {
        if (mod == ArayuzModuServisi.Basit)
            Services.GetRequiredService<OturumServisi>().KullaniciAta(
                new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        return Ciz();
    }

    private void OK1YeniUrunDoldur(IRenderedComponent<Kuyumcu.Web.Pages.Urunler> e, string mod)
    {
        Dugme(e, "Yeni ürün").Click();
        e.Find("#u-daha-fazla").Click();   // PAKET GOC-C (devir "zorunlu önce"): yeni ürün formu PRO'da da yalnız zorunlularla açılır — isteğe bağlılar "Daha fazla"da (eskiden yalnız BASİT)
        e.Find("#u-ad").Input("Burma Bilezik");
        e.Find("#u-kod").Input("BLZ-99");
        e.Find("#u-gram").Input("15");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit, "u-iscilik")]
    [InlineData(ArayuzModuServisi.Profesyonel, "u-iscilik")]
    [InlineData(ArayuzModuServisi.Profesyonel, "u-gram")]
    [InlineData(ArayuzModuServisi.Profesyonel, "u-tas")]
    public void OK1_K4_yeni_urun_belirsiz_1_500_Kaydet_PASIF_sebep_yazar_POST_YOK(string mod, string alan)
    {
        var e = OK1Ciz(mod);
        OK1YeniUrunDoldur(e, mod);
        OK1Yaz(e, alan, OK1TusTus1500);

        // Kutu kullanıcının yazdığını AYNEN gösterir, alan hatası görünür (sessiz "0" YOK).
        Assert.Equal("1.500", e.Find($"#{alan}").GetAttribute("value"));
        Assert.Equal("true", e.Find($"#{alan}").GetAttribute("aria-invalid"));
        Assert.Contains("1500", e.Find($"#{alan}-sar").TextContent);
        // Kaydet PASİF + sebep kutunun ETİKETİYLE (UX-S30).
        Assert.True(e.Find("#u-kaydet").HasAttribute("disabled"));
        var etiket = e.Find($"label[for='{alan}']").TextContent.Replace("*", "").Split("(isteğe")[0].Trim();
        Assert.Contains(etiket, e.Find("#u-kaydet-kilit").TextContent);

        istekler.Clear();
        e.Find("#u-kaydet").Click();   // bUnit disabled düğmeye de olay gönderir → kod içi koruma da sınanır
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        Assert.Equal("1.500", e.Find($"#{alan}").GetAttribute("value"));   // yeniden çizim kutuyu "0"a EZMEZ
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void OK1_K4_duzeltince_Kaydet_acilir_ve_govdede_YAZILAN_deger_gider(string mod)
    {
        var e = OK1Ciz(mod);
        OK1YeniUrunDoldur(e, mod);
        OK1Yaz(e, "u-iscilik", OK1TusTus1500);
        OK1Yaz(e, "u-iscilik", ["1500"]);
        Assert.False(e.Find("#u-kaydet").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#u-kaydet-kilit"));

        istekler.Clear();
        e.Find("#u-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST"));
        Assert.Equal("1500", Alan(istekler.First(i => i.Metot == "POST").Govde, "iscilikTl"));
    }

    [Theory]
    [InlineData("1,5", "1.5")]
    [InlineData("abc", null)]
    public void OK1_K4_virgul_ondalik_gecer_harf_POST_etmez(string yazilan, string? beklenen)
    {
        var e = OK1Ciz(ArayuzModuServisi.Profesyonel);
        OK1YeniUrunDoldur(e, ArayuzModuServisi.Profesyonel);
        OK1Yaz(e, "u-iscilik", [yazilan]);
        istekler.Clear();
        e.Find("#u-kaydet").Click();
        if (beklenen is null)
        {
            Assert.True(e.Find("#u-kaydet").HasAttribute("disabled"));
            Assert.DoesNotContain(istekler, i => i.Metot == "POST");
        }
        else
        {
            e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST"));
            Assert.Equal(beklenen, Alan(istekler.First(i => i.Metot == "POST").Govde, "iscilikTl"));
        }
    }

    [Theory]
    [InlineData("d-iscilik")]
    [InlineData("d-gram")]
    [InlineData("d-satis")]
    public void OK1_K4_duzenle_cekmecesi_belirsiz_1_500_PATCH_YOK_sebep_yazar(string alan)
    {
        var e = OK1Ciz(ArayuzModuServisi.Profesyonel);
        Dugme(e, "Düzenle").Click();
        e.WaitForElement($"#{alan}");
        OK1Yaz(e, alan, OK1TusTus1500);
        Assert.Equal("true", e.Find($"#{alan}").GetAttribute("aria-invalid"));
        var etiket = e.Find($"label[for='{alan}']").TextContent.Replace("*", "").Split("(isteğe")[0].Trim();
        Assert.Contains(etiket, e.Find("#cekmece-urun-kilit").TextContent);
        Assert.True(e.Find("#cekmece-urun-kaydet").HasAttribute("disabled"));

        istekler.Clear();
        e.Find("#cekmece-urun-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        Assert.Equal("1.500", e.Find($"#{alan}").GetAttribute("value"));
    }

    [Fact]
    public void OK1_K4_sayim_belirsiz_1_500_PATCH_YOK_sebep_yazar()
    {
        var e = OK1Ciz(ArayuzModuServisi.Profesyonel);
        Dugme(e, "Stok").Click();
        Dugme(e, "Sayım Düzelt").Click();
        OK1Yaz(e, "s-adet", OK1TusTus1500);
        Assert.Equal("true", e.Find("#s-adet").GetAttribute("aria-invalid"));
        Assert.NotEmpty(e.Find("#s-kaydet-kilit").TextContent);
        istekler.Clear();
        Dugme(e, "Kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
    }
}
