using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ŞUBE-KAPSAM-B — SEÇİLİ ŞUBE BÜTÜN EKRANI YÖNETİR (istemci)
//
// Sözleşme (şartname § 4.3 / § 4.4, kabul K-7…K-9, K-14; mutasyon M7, M8):
//   • Seçim SUNUCUYA tercih olarak yazılır (PATCH /tercihler/benim
//     {"gorunum-subesi": ULID | "TUMU"}); açılışta GET ile okunur.
//   • Yazma düşerse seçici ESKİ değere döner + HataBandi (K-9 — görünen =
//     kaydedilen).
//   • Şubeli ekranlar (Panel, Kasalar, …) istekleri SubeliSorgu ile
//     `subeId=` taşır; seçim değişince YENİDEN yükler.
//   • "Tüm şubeler" yetkisi istemcide rol listesinden DEĞİL, sunucunun tercih
//     yanıtından (gorunumSubesi.tumSubeler) okunur (§ 4.3.5).
//   • Tek şubeli dükkânda ve şubeye kilitli rolde rozet + seçici YOK (K-14).
//
// Sahte sunucu tercihi GERÇEK uç gibi tutar: PATCH değeri saklar, GET onu
// çözülmüş hâliyle döner (TercihModulu.GorunumSubesiCoz ile aynı alanlar).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GorunumSubesiEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly Durum d = new();

    private sealed class Durum
    {
        public int SubeSayisi = 2;
        public string Rol = "SAHIP";
        public string? Kayitli;                 // sunucudaki tercih (ULID | "TUMU" | null)
        public bool KayitliPasif;               // kayıtlı şube kapandı → gecersizTercih
        public HttpStatusCode PatchKodu = HttpStatusCode.OK;
        public HttpStatusCode TercihGetKodu = HttpStatusCode.OK;
        /// <summary>Teste özgü gerçekçi yanıtlar (yol parçası → gövde; en uzun parça önce).</summary>
        public Dictionary<string, string> Ek = new();
        /// <summary>Tur 2: yol parçası → hata kodu (Ek'ten önce bakılır).</summary>
        public Dictionary<string, HttpStatusCode> Hata = new();
        /// <summary>Tur 2 (yarış): yol parçası → kapı; eşleşen istek kapı açılana dek bekler (bir kez).</summary>
        public Dictionary<string, TaskCompletionSource> Kapi = new();
        /// <summary>Tur 2 (yarış): İLK PATCH bu kapıyı bekler ve bu kodla döner.</summary>
        public TaskCompletionSource? IlkPatchKapisi;
        public HttpStatusCode IlkPatchKodu = HttpStatusCode.InternalServerError;
    }

    private sealed class SahteApi(List<(string, string, string)> kayit, Durum d) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((istek.Method.Method, tam, govde));

            TaskCompletionSource? kapi = null;
            lock (d.Kapi)
            {
                var k = d.Kapi.FirstOrDefault(x => tam.Contains(x.Key, StringComparison.Ordinal));
                if (k.Key is not null) { kapi = k.Value; d.Kapi.Remove(k.Key); }
            }
            if (kapi is not null) await kapi.Task;

            if (yol.EndsWith("/tercihler/benim", StringComparison.Ordinal))
            {
                if (istek.Method == HttpMethod.Patch)
                {
                    var ilk = Interlocked.Exchange(ref d.IlkPatchKapisi, null);
                    if (ilk is not null)
                    {
                        await ilk.Task;
                        if (d.IlkPatchKodu != HttpStatusCode.OK)
                            return Json(d.IlkPatchKodu, """{"hata":"Tercih kaydedilemedi (geç yanıt)"}""");
                    }
                    if (d.PatchKodu != HttpStatusCode.OK)
                        return Json(d.PatchKodu, """{"hata":"Tercih kaydedilemedi (sunucu hatası)"}""");
                    using var b = JsonDocument.Parse(govde);
                    d.Kayitli = b.RootElement.GetProperty("tercihler").GetProperty("gorunum-subesi").GetString();
                    d.KayitliPasif = false;
                }
                else if (d.TercihGetKodu != HttpStatusCode.OK)
                {
                    return Json(d.TercihGetKodu, """{"hata":"Tercihler okunamadı"}""");
                }
                return Json(HttpStatusCode.OK, TercihYaniti());
            }

            var hk = d.Hata.Where(k => tam.Contains(k.Key, StringComparison.Ordinal))
                           .OrderByDescending(k => k.Key.Length).FirstOrDefault();
            if (hk.Key is not null) return Json(hk.Value, $$"""{"hata":"{{hk.Key}} okunamadı (SKB)","message":"{{hk.Key}} okunamadı (SKB)"}""");

            var ek = d.Ek.Where(k => tam.Contains(k.Key, StringComparison.Ordinal))
                         .OrderByDescending(k => k.Key.Length).FirstOrDefault();
            if (ek.Key is not null) return Json(HttpStatusCode.OK, ek.Value);

            // Basit Panel'in altın pozisyonu kartı bu testin konusu değil: yetkisiz (403) notunu çizer.
            if (yol.EndsWith("/pozisyon", StringComparison.Ordinal))
                return Json(HttpStatusCode.Forbidden, """{"hata":"Yetkiniz yok"}""");

            var cevap = yol switch
            {
                var y when y.EndsWith("/subeler", StringComparison.Ordinal) =>
                    "[" + string.Join(",", Enumerable.Range(1, d.SubeSayisi).Select(i =>
                        $$"""{"id":"s{{i}}","ad":"{{(i == 1 ? "Merkez" : $"Şube {i}")}}","aktif":true}""")) + "]",
                var y when y.EndsWith("/yetkiler/benim", StringComparison.Ordinal) =>
                    """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor"]}""",
                var y when y.EndsWith("/raporlar/gunluk", StringComparison.Ordinal) => """
                    {"satis":{"toplamTl":12500.00},"alis":{"toplamTl":3000.00},
                     "hurda":{"odenenTl":500.00},"netHasGramHareketi":4.25,
                     "kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":9000.00,"paraBirimi":"TRY"}]}
                    """,
                var y when y.EndsWith("/raporlar/aylik", StringComparison.Ordinal) => """
                    {"gunler":[{"tarih":"2026-09-20","satisTl":1000.00},
                               {"tarih":"2026-09-21","satisTl":12500.00}]}
                    """,
                var y when y.Contains("/raporlar/islem-defteri", StringComparison.Ordinal) => """
                    {"baslangic":"2026-09-21","bitis":"2026-09-21","toplamSatir":0,"ofset":0,
                     "limit":5,"dahaFazlasiVar":false,
                     "ozet":{"tlGiris":0,"tlCikis":0,"hasGiris":0,"hasCikis":0,"satirSayisi":0},
                     "storno":{"adet":0,"tutarTl":0},"satirlar":[]}
                    """,
                var y when y.EndsWith("/raporlar/kar-zarar", StringComparison.Ordinal) =>
                    """{"karTl":1000.00,"maliyetiBilinmeyenAdet":0}""",
                var y when y.EndsWith("/kasalar", StringComparison.Ordinal) => """
                    [{"id":"k1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":9000.00,"aktif":true,"subeId":"s1"},
                     {"id":"k2","ad":"Şube 2 Kasası","paraBirimi":"TRY","bakiye":4000.00,"aktif":true,"subeId":"s2"}]
                    """,
                var y when y.EndsWith("/kur/guncel", StringComparison.Ordinal) =>
                    """{"id":"kur-1","tenantId":"t1","altinHasFiyati":6877,"gramAltinFiyati":6912.40,"usdTry":41.95,"eurTry":49.10,"gbpTry":56.30,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-23T09:02:00Z","ayarFiyatlari":[]}""",
                var y when y.EndsWith("/kur/kaynaklar", StringComparison.Ordinal) =>
                    """{"kaynaklar":["ALTINKAYNAK"],"secili":"ALTINKAYNAK","varsayilan":"ALTINKAYNAK"}""",
                var y when y.Contains("/uyum/", StringComparison.Ordinal) =>
                    """{"suresiGecmis":0,"yaklasan":0,"gecikmisSayisi":0,"yuksek":0}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return Json(HttpStatusCode.OK, cevap);
        }

        /// <summary>Gerçek ucun çözümü: kilitli rol → kendi şubesi; pasif → oturum şubesi + gecersiz.</summary>
        private string TercihYaniti()
        {
            var tum = d.Rol is "SAHIP" or "MUDUR" or "MUHASEBE";
            string? sube;
            var gecersiz = false;
            if (!tum) sube = "s1";
            else if (d.Kayitli is null or "TUMU") sube = null;
            else if (d.KayitliPasif) { sube = "s1"; gecersiz = true; }
            else sube = d.Kayitli;
            var subeJson = sube is null ? "null" : $"\"{sube}\"";
            var kayitliJson = d.Kayitli is null ? "null" : $"\"{d.Kayitli}\"";
            var tercihler = d.Kayitli is null ? "{}" : $$"""{"gorunum-subesi":"{{d.Kayitli}}"}""";
            return $$"""
                {"tercihler":{{tercihler}},
                 "gorunumSubesi":{"kayitli":{{kayitliJson}},"subeId":{{subeJson}},"tumSubeler":{{(tum ? "true" : "false")}},
                                  "gecersizTercih":{{(gecersiz ? "true" : "false")}},"deger":"{{sube ?? "TUMU"}}"},
                 "mesaj":"Tercihler kaydedildi"}
                """;
        }

        private static HttpResponseMessage Json(HttpStatusCode kod, string govde) => new(kod)
        {
            Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    public GorunumSubesiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, d))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
        Services.AddScoped<HavaServisi>();
    }

    private OturumServisi Oturum => Services.GetRequiredService<OturumServisi>();

    private void OturumAc(string rol = "SAHIP", string mod = ArayuzModuServisi.Profesyonel)
    {
        d.Rol = rol;
        Oturum.KullaniciAta(new OturumKullanici("u1", "Ahmet", "Yılmaz", "a@ornek.com", rol, "s1", null, mod));
    }

    /// <summary>Kabuk seçicisini çizer ve tercih + şube listesi okunana dek bekler.</summary>
    private IRenderedComponent<SubeSecici> SeciciHazir()
    {
        var s = Render<SubeSecici>();
        s.WaitForAssertion(() =>
        {
            var g = KabukServisi.Ortak.GorunumIcin(Oturum);
            Assert.NotNull(g);
            Assert.True(g!.Yuklendi);
            Assert.Equal(d.SubeSayisi, g.Subeler.Count);
        });
        return s;
    }

    /// <summary>
    /// Seçicide bir şubeyi seçer. Aynı bağlamdaki başka bileşen (rozet, sayfa) kabuk
    /// olayıyla yeniden çizdirebildiği için bulunan öğenin olay kimliği bayatlayabilir —
    /// bayatsa yeniden bulunup tıklanır (bUnit UnknownEventHandlerIdException).
    /// </summary>
    private static void Sec(IRenderedComponent<SubeSecici> s, string metin)
    {
        for (var deneme = 0; ; deneme++)
        {
            try
            {
                if (s.FindAll("#ust-sube-secim-liste").Count == 0) s.Find("#ust-sube-secim").Click();
                s.WaitForAssertion(() => Assert.NotNull(s.Find("#ust-sube-secim-liste")));
                s.FindAll("#ust-sube-secim-liste li[role=option]")
                 .First(o => o.TextContent.Trim() == metin).Click();
                return;
            }
            catch (Bunit.Rendering.UnknownEventHandlerIdException) when (deneme < 5) { }
        }
    }

    private List<string> Getler(string parca)
    {
        lock (istekler)
            return istekler.Where(i => i.Metot == "GET" && i.Yol.Contains(parca, StringComparison.Ordinal))
                           .Select(i => i.Yol).ToList();
    }

    // ── SubeliSorgu (saf) ────────────────────────────────────────────────────

    [Fact]
    public void SubeliSorgu_secim_yokken_yolu_AYNEN_doner_secimde_subeId_ekler()
    {
        OturumAc();
        // Tercih okunmadan: daraltma yok.
        Assert.Equal("/kasalar", SubeliSorgu.Ekle("/kasalar", Oturum));

        d.Kayitli = "s2";
        SeciciHazir();
        Assert.Equal("/kasalar?subeId=s2", SubeliSorgu.Ekle("/kasalar", Oturum));
        Assert.Equal("/raporlar/aylik?yil=2026&ay=9&subeId=s2", SubeliSorgu.Ekle("/raporlar/aylik?yil=2026&ay=9", Oturum));
        // Sayfanın kendi süzgeci varsa dokunulmaz (iki subeId olmaz).
        Assert.Equal("/x?subeId=s1", SubeliSorgu.Ekle("/x?subeId=s1", Oturum));
        Assert.Equal("s2", SubeliSorgu.Sube(Oturum));
    }

    [Fact]
    public void SubeliSorgu_baska_oturumun_secimini_GORMEZ_paralel_sizinti_yok()
    {
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        // Aynı statik kabuk, BAŞKA bir OturumServisi örneği (başka test bağlamı / başka kullanıcı).
        var yabanci = new OturumServisi(JSInterop.JSRuntime, Services);
        yabanci.KullaniciAta(new OturumKullanici("u1", "Ahmet", "Yılmaz", "a@ornek.com", "SAHIP", "s1"));
        Assert.Null(SubeliSorgu.Sube(yabanci));
        Assert.Equal("/kasalar", SubeliSorgu.Ekle("/kasalar", yabanci));
    }

    [Fact]
    public void Sube_kilitli_rolde_istemci_subeId_GONDERMEZ_sunucu_oturum_subesini_uygular()
    {
        OturumAc("TEZGAHTAR");
        d.Kayitli = "s2";
        var s = SeciciHazir();
        Assert.Null(SubeliSorgu.Sube(Oturum));
        // Durumun kendisi de daraltma TUTMAZ (arama bileşenleri genel değeri okur).
        Assert.Null(KabukServisi.Ortak.GorunumIcin(Oturum)!.SubeId);
        Assert.Empty(s.FindAll("#ust-sube-secim"));
    }

    // ── Kalıcılık (K-7 / K-8 istemci yüzü) ───────────────────────────────────

    [Fact]
    public void K7_Kayitli_tercih_acilista_okunur_secici_o_subeyle_cizilir()
    {
        OturumAc();
        d.Kayitli = "s2";
        var s = SeciciHazir();
        s.WaitForAssertion(() => Assert.Contains("Şube 2", s.Find("#ust-sube-secim").TextContent, StringComparison.Ordinal));
        Assert.Equal("s2", KabukServisi.Ortak.GorunumSubesiIcin(Oturum));
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/tercihler/benim", StringComparison.Ordinal));
    }

    [Fact]
    public void Secim_SUNUCUYA_yazilir_sube_ULID_tumu_TUMU()
    {
        OturumAc();
        var s = SeciciHazir();
        Sec(s, "Şube 2");
        s.WaitForAssertion(() => Assert.Equal("s2", d.Kayitli));
        var yama = istekler.Last(i => i.Metot == "PATCH");
        Assert.EndsWith("/tercihler/benim", yama.Yol, StringComparison.Ordinal);
        using (var b = JsonDocument.Parse(yama.Govde))
            Assert.Equal("s2", b.RootElement.GetProperty("tercihler").GetProperty("gorunum-subesi").GetString());
        Assert.Equal("s2", KabukServisi.Ortak.GorunumSubesiIcin(Oturum));

        Sec(s, Sozlukler.Tr.Kabuk.SubeTumu);
        s.WaitForAssertion(() => Assert.Equal("TUMU", d.Kayitli));
        Assert.Null(KabukServisi.Ortak.GorunumSubesiIcin(Oturum));
    }

    [Fact]
    public void K9_Tercih_yazimi_500_donerse_secici_ESKI_degere_doner_ve_HataBandi_gorunur()
    {
        OturumAc();
        d.Kayitli = "s1";
        var s = SeciciHazir();
        d.PatchKodu = HttpStatusCode.InternalServerError;

        Sec(s, "Şube 2");

        s.WaitForAssertion(() => Assert.NotNull(s.Find("#ust-sube-kayit-hata")));
        var onEk = SubeKapsamBSozlugu.Tr.KayitHatasi[..SubeKapsamBSozlugu.Tr.KayitHatasi.IndexOf("{0}", StringComparison.Ordinal)];
        Assert.StartsWith(onEk, s.Find("#ust-sube-kayit-hata").TextContent.Trim(), StringComparison.Ordinal);
        s.WaitForAssertion(() => Assert.Equal("s1", KabukServisi.Ortak.GorunumSubesiIcin(Oturum)));
        // Görünen = kaydedilen: seçim ESKİ değere döndü, ekran da eskiyi gösteriyor.
        Assert.Equal("s1", KabukServisi.Ortak.GorunumSubesiIcin(Oturum));
        Assert.Equal("s1", d.Kayitli);
        Assert.Contains("Merkez", s.Find("#ust-sube-secim").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Şube 2", s.Find("#ust-sube-secim").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Tercih_okunamazsa_secici_GIZLI_sebep_bantta_istekler_daraltmasiz()
    {
        OturumAc();
        d.TercihGetKodu = HttpStatusCode.InternalServerError;
        var s = Render<SubeSecici>();
        s.WaitForAssertion(() => Assert.NotNull(s.Find("#ust-sube-tercih-hata")));
        Assert.Empty(s.FindAll("#ust-sube-secim"));
        Assert.Null(SubeliSorgu.Sube(Oturum));
    }

    // ── § 4.3.5 — tüm-şube yetkisi SUNUCUDAN ─────────────────────────────────

    [Fact]
    public void Tum_sube_yetkisi_istemci_rol_listesinden_DEGIL_sunucudan_okunur()
    {
        // Rol SAHİP ama sunucu (tercih yanıtı) tumSubeler=false diyorsa seçici YOK:
        // istemci sunucunun reddedeceği bir seçimi vaat etmez.
        OturumAc();
        d.Rol = "TEZGAHTAR";   // sahte sunucu bu kullanıcıya tumSubeler=false döner
        var s = SeciciHazir();
        Assert.False(KabukServisi.Ortak.GorunumIcin(Oturum)!.TumSubeler);
        Assert.Empty(s.FindAll("#ust-sube-secim"));
    }

    // ── M7 — Panel isteği seçili şubeyi taşır ────────────────────────────────

    [Fact]
    public void M7_Panel_sube_secili_iken_gunluk_aylik_defter_istekleri_subeId_tasir_rozet_yazar()
    {
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        lock (istekler) istekler.Clear();

        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForAssertion(() =>
        {
            Assert.Contains(Getler("/raporlar/gunluk"), y => y.Contains("subeId=s2", StringComparison.Ordinal));
            Assert.Contains(Getler("/raporlar/aylik"), y => y.Contains("subeId=s2", StringComparison.Ordinal));
            Assert.Contains(Getler("/raporlar/islem-defteri"), y => y.Contains("subeId=s2", StringComparison.Ordinal));
        }, TimeSpan.FromSeconds(10));
        Assert.All(Getler("/raporlar/gunluk"), y => Assert.Contains("subeId=s2", y, StringComparison.Ordinal));
        Assert.Equal(string.Format(SubeKapsamBSozlugu.Tr.RozetSube, "Şube 2"),
            e.Find("#gorunum-rozeti-metin").TextContent.Trim());
        // Kiracı geneli kartlar (cari yaşlandırma) şubeye BÖLÜNMEZ.
        Assert.All(Getler("/raporlar/cari-yaslandirma"), y => Assert.DoesNotContain("subeId", y, StringComparison.Ordinal));
    }

    [Fact]
    public void Panel_secim_degisince_subeli_kartlari_YENIDEN_yukler()
    {
        OturumAc();
        var s = SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForAssertion(() => Assert.NotEmpty(Getler("/raporlar/gunluk")), TimeSpan.FromSeconds(10));
        Assert.All(Getler("/raporlar/gunluk"), y => Assert.DoesNotContain("subeId", y, StringComparison.Ordinal));
        Assert.Equal(SubeKapsamBSozlugu.Tr.RozetTumu, e.Find("#gorunum-rozeti-metin").TextContent.Trim());

        Sec(s, "Merkez");

        e.WaitForAssertion(() =>
            Assert.Contains(Getler("/raporlar/gunluk"), y => y.Contains("subeId=s1", StringComparison.Ordinal)),
            TimeSpan.FromSeconds(10));
        e.WaitForAssertion(() => Assert.Equal(string.Format(SubeKapsamBSozlugu.Tr.RozetSube, "Merkez"),
            e.Find("#gorunum-rozeti-metin").TextContent.Trim()));
    }

    [Fact]
    public void Panel_basit_yuzde_kar_istegi_de_subeId_tasir()
    {
        OturumAc(mod: ArayuzModuServisi.Basit);
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForAssertion(() =>
            Assert.Contains(Getler("/raporlar/kar-zarar"), y => y.Contains("subeId=s2", StringComparison.Ordinal)),
            TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Sayfa yenileme (canlı ölçümde bulundu, 01.10): jeton var, profil henüz gelmemişken
    /// Panel açılırsa İLK günlük isteği bile seçili şubeyle gider — daraltmasız ara istek
    /// ("önce birleşik, sonra şube" titremesi) YOK.
    /// </summary>
    [Fact]
    public async Task Sayfa_yenilemede_ilk_istek_bile_secili_subeyle_gider_titreme_yok()
    {
        d.Rol = "SAHIP";
        d.Kayitli = "s2";
        await Oturum.TokenKaydetAsync("jeton-yok.gecersiz.imza", "yenileme");
        Assert.True(Oturum.GirisYapildi);
        Assert.Null(Oturum.Kullanici);
        Render<SubeSecici>();
        var p = Render<Kuyumcu.Web.Pages.Panel>();
        await Task.Delay(300);   // profil gelmeden Panel İSTEK ATMAMALI
        Assert.Empty(Getler("/raporlar/gunluk"));
        await p.InvokeAsync(() => Oturum.KullaniciAta(new OturumKullanici("u1", "Ahmet", "Yılmaz", "a@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel)));
        p.WaitForAssertion(() => Assert.NotEmpty(Getler("/raporlar/gunluk")), TimeSpan.FromSeconds(10));
        Assert.All(Getler("/raporlar/gunluk"), y => Assert.Contains("subeId=s2", y, StringComparison.Ordinal));
    }

    [Fact]
    public void Raporlar_basit_yuzde_bugun_ozeti_ve_gun_sonu_subeId_tasir()
    {
        d.Ek["/raporlar/pos-fatura-mutabakati"] = Fikstur<RaporlarEkranTestleri>("Mutabakat");
        OturumAc(mod: ArayuzModuServisi.Basit);
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() =>
        {
            Assert.Contains(Getler("/raporlar/gunluk"), y => y.Contains("subeId=s2", StringComparison.Ordinal));
            Assert.Contains(Getler("/raporlar/pos-fatura-mutabakati"), y => y.Contains("subeId=s2", StringComparison.Ordinal));
        }, TimeSpan.FromSeconds(10));
        Assert.DoesNotContain(Getler("/raporlar/pos-fatura-mutabakati"), y => !y.Contains("subeId=", StringComparison.Ordinal));
    }

    // ── Kasalar ──────────────────────────────────────────────────────────────

    [Fact]
    public void Kasalar_listesi_secili_subeyle_istenir_onbellek_anahtari_subeye_gore_ayri()
    {
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains(Getler("/kasalar"), y => y.EndsWith("/kasalar?subeId=s2", StringComparison.Ordinal)),
            TimeSpan.FromSeconds(10));
        Assert.NotNull(e.Find("#gorunum-rozeti"));

        // ListeOnbellegi: /kasalar kaydı /kasalar?subeId=… için KULLANILMAZ (şube değişince eski liste yok).
        var o = new ListeOnbellegi();
        var nesil = o.NesilAl("k");
        o.Koy("/kasalar", "k", "[1]", nesil);
        Assert.False(o.TryAl("/kasalar?subeId=s2", "k", out _));
        o.Koy("/kasalar?subeId=s2", "k", "[2]", nesil);
        Assert.True(o.TryAl("/kasalar?subeId=s2", "k", out var g2));
        Assert.Equal("[2]", g2);
        Assert.True(o.TryAl("/kasalar", "k", out var g1));
        Assert.Equal("[1]", g1);
    }

    [Fact]
    public void Kasalar_tum_subeler_gorunumunde_kasa_kartinda_sube_adi_yazar()
    {
        OturumAc();
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#kasa-sube-k2")), TimeSpan.FromSeconds(10));
        Assert.Contains("Şube 2", e.Find("#kasa-sube-k2").TextContent, StringComparison.Ordinal);
        Assert.Contains("Merkez", e.Find("#kasa-sube-k1").TextContent, StringComparison.Ordinal);
    }

    // ── K-14 — sade yüz: tek şube / kilitli rol ──────────────────────────────

    [Fact]
    public void K14_Tek_subeli_kiracida_Panel_ve_Kasalarda_rozet_ve_secici_YOK()
    {
        d.SubeSayisi = 1;
        OturumAc();
        var s = SeciciHazir();
        Assert.Empty(s.FindAll("#ust-sube-secim"));

        var p = Render<Kuyumcu.Web.Pages.Panel>();
        p.WaitForAssertion(() => Assert.NotEmpty(Getler("/raporlar/gunluk")), TimeSpan.FromSeconds(10));
        Assert.Empty(p.FindAll("#gorunum-rozeti"));

        var k = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        k.WaitForAssertion(() => Assert.NotEmpty(Getler("/kasalar")), TimeSpan.FromSeconds(10));
        Assert.Empty(k.FindAll("#gorunum-rozeti"));
        Assert.All(Getler("/kasalar"), y => Assert.DoesNotContain("subeId", y, StringComparison.Ordinal));
    }

    [Fact]
    public void K14_Sube_kilitli_tezgahtarda_cok_subede_bile_rozet_ve_secici_YOK()
    {
        OturumAc("TEZGAHTAR");
        d.Kayitli = "s2";
        var s = SeciciHazir();
        Assert.Empty(s.FindAll("#ust-sube-secim"));
        var p = Render<Kuyumcu.Web.Pages.Panel>();
        p.WaitForAssertion(() => Assert.NotEmpty(Getler("/raporlar/gunluk")), TimeSpan.FromSeconds(10));
        Assert.Empty(p.FindAll("#gorunum-rozeti"));
        Assert.All(Getler("/raporlar/gunluk"), y => Assert.DoesNotContain("subeId", y, StringComparison.Ordinal));
    }

    // ── § 4.4 — kapanmış şube tercihi ────────────────────────────────────────

    [Fact]
    public void Kayitli_sube_kapandiysa_sari_bant_kendi_subenize_donuldu()
    {
        OturumAc();
        d.Kayitli = "s2";
        d.KayitliPasif = true;
        SeciciHazir();
        var p = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#gorunum-gecersiz")), TimeSpan.FromSeconds(10));
        Assert.Equal(SubeKapsamBSozlugu.Tr.GecersizTercihSubeye, p.Find("#gorunum-gecersiz-metin").TextContent.Trim());
        Assert.Equal("s1", SubeliSorgu.Sube(Oturum));
    }

    // ── Tur 3 (doğrulayıcı YÜKSEK): kapanan şube bandı kalıcı değil ─────────

    /// <summary>
    /// Kayıtlı tercih kapanmış şubeyi gösteriyorsa açılışta sunucunun çözdüğü değer
    /// (kendi şube "s1") tercihe BİR KEZ yazılır → bir sonraki açılışta bant çıkmaz.
    /// </summary>
    [Fact]
    public void Kapanan_sube_tercihi_acilista_SUNUCUDA_temizlenir_bant_bir_kez_gorunur()
    {
        OturumAc();
        d.Kayitli = "s2";
        d.KayitliPasif = true;
        SeciciHazir();
        var p = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#gorunum-gecersiz")), TimeSpan.FromSeconds(10));
        p.WaitForAssertion(() =>
        {
            lock (istekler)
                Assert.Contains(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("/tercihler/benim", StringComparison.Ordinal)
                    && i.Govde.Contains("\"gorunum-subesi\":\"s1\"", StringComparison.Ordinal));
        }, TimeSpan.FromSeconds(10));
        Assert.Equal("s1", d.Kayitli);
        Assert.False(d.KayitliPasif);
        Assert.Empty(p.FindAll("#gorunum-gecersiz-hata"));
    }

    /// <summary>"Tamam" bandı kapatır; başka bir sayfada da (aynı oturum) tekrar çıkmaz.</summary>
    [Fact]
    public void Kapanan_sube_bandi_Tamam_ile_kapanir_baska_sayfada_da_cikmaz()
    {
        OturumAc();
        d.Kayitli = "s2";
        d.KayitliPasif = true;
        SeciciHazir();
        var p = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#gorunum-gecersiz-kapat")), TimeSpan.FromSeconds(10));
        p.Find("#gorunum-gecersiz-kapat").Click();
        p.WaitForAssertion(() => Assert.Empty(p.FindAll("#gorunum-gecersiz")));
        var h = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        h.WaitForAssertion(() => Assert.NotNull(h.Find("#gorunum-rozeti")), TimeSpan.FromSeconds(10));
        Assert.Empty(h.FindAll("#gorunum-gecersiz"));
    }

    /// <summary>Temizleme yazımı düşerse sessiz değil: bant + sebep görünür, "Tamam" yine kapatır.</summary>
    [Fact]
    public void Kapanan_sube_temizligi_duserse_sebep_gorunur()
    {
        OturumAc();
        d.Kayitli = "s2";
        d.KayitliPasif = true;
        d.PatchKodu = HttpStatusCode.InternalServerError;
        SeciciHazir();
        var p = Render<Kuyumcu.Web.Pages.Kasalar>().AyrintiAc();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#gorunum-gecersiz-hata")), TimeSpan.FromSeconds(10));
        Assert.Contains(SubeKapsamBSozlugu.Tr.GecersizTemizlenemedi.Split("{0}")[0].Trim(), p.Find("#gorunum-gecersiz-hata").TextContent, StringComparison.Ordinal);
        p.Find("#gorunum-gecersiz-kapat").Click();
        p.WaitForAssertion(() => Assert.Empty(p.FindAll("#gorunum-gecersiz")));
    }

    // ── § 4.3.3 — diğer şubeli ekranlar (tablo güdümlü) ─────────────────────

    /// <summary>
    /// Seçili şube "s2" iken her şubeli ekranın şubeli çağrısı `subeId=s2` taşır ve
    /// başlık altında rozet çizilir. Bir ekran SubeliSorgu'yu unutursa o satır KIRMIZI.
    /// </summary>
    [Theory]
    [InlineData("Hurda", "/hurda/bakiye")]
    [InlineData("Hurda", "/hurda/defter")]
    [InlineData("PosTahsilatlar", "/pos/tahsilatlar")]
    [InlineData("PosTahsilatlar", "/pos/valor-takvimi")]
    [InlineData("KarZarar", "/raporlar/kar-zarar?")]
    [InlineData("KarZarar", "/raporlar/stok-degerlemesi")]
    [InlineData("KasaRaporu", "/kasalar")]
    [InlineData("IslemDefteri", "/raporlar/islem-defteri")]
    [InlineData("Raporlar", "/raporlar/aylik")]
    [InlineData("Raporlar", "/raporlar/stok-degeri")]
    [InlineData("Raporlar", "/raporlar/stok-yaslandirma")]
    [InlineData("Raporlar", "/raporlar/pos-fatura-mutabakati")]
    public void Subeli_ekran_cagrisi_secili_subeyi_tasir_ve_rozet_cizer(string sayfa, string parca)
    {
        Services.AddScoped<Kuyumcu.Web.Servisler.PosServisi>();
        // Gerçekçi gövdeler — sahibi olan ekran testlerinin TEK kaynağından (kopya yok).
        // POS gövdeleri PosEkranTestleri'ndeki gerçekçi yanıtlarla aynı şekil (orada satır içi).
        d.Ek["/pos/valor-takvimi"] =
            """[{"valorTarihi":"2026-09-19T00:00:00Z","cihazId":"p1","cihazAd":"Garanti POS","bankaAdi":"Garanti","brut":10000.00,"komisyon":185.00,"net":9815.00,"yatan":0.00,"kalan":9815.00}]""";
        d.Ek["/pos/tahsilatlar"] =
            """{"satirlar":[],"toplamSatir":0,"limit":50,"offset":0,"azamiSatir":200,"sinirDegdi":false}""";
        d.Ek["/pos/cihazlar"] =
            """[{"id":"p1","ad":"Garanti POS","bankaAdi":"Garanti","subeId":"s2","kasaId":"k2","bankaHesapId":null,"uyeIsyeriNo":null,"terminalNo":null,"pesinKomisyonOran":0.0185,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":9815.00,"bugunYatacakNet":0.00}]""";
        if (sayfa == "KarZarar")
        {
            d.Ek["/raporlar/kar-zarar/urun"] = Fikstur<KarZararEkranTestleri>("UrunYaniti");
            d.Ek["/raporlar/kar-zarar"] = Fikstur<KarZararEkranTestleri>("KarZararYaniti");
            d.Ek["/raporlar/stok-degerlemesi"] = Fikstur<KarZararEkranTestleri>("StokYaniti");
        }
        d.Ek["/raporlar/katalog"] = Fikstur<RaporlarEkranTestleri>("Katalog");
        d.Ek["/raporlar/stok-yaslandirma"] = Fikstur<RaporlarEkranTestleri>("Yaslandirma");
        d.Ek["/raporlar/pos-fatura-mutabakati"] = Fikstur<RaporlarEkranTestleri>("Mutabakat");
        d.Ek["/raporlar/aylik"] = """{"yil":2026,"ay":9,"gunler":[]}""";
        d.Ek["/raporlar/stok-degeri"] =
            """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""";
        d.Ek["/raporlar/reyon-stok"] = """{"reyonlar":[],"subeler":[]}""";
        d.Ek["/reyonlar"] = "[]";
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var tur = typeof(Kuyumcu.Web.Pages.Panel).Assembly.GetType("Kuyumcu.Web.Pages." + sayfa)!;
        var e = Render(b => { b.OpenComponent(0, tur); b.CloseComponent(); });
        e.WaitForAssertion(() => Assert.NotEmpty(Getler(parca)), TimeSpan.FromSeconds(10));
        // Hurda formu YAZMA (oturum) şubesinin bakiyesini ayrıca okur (?subeId=s1) — bilinçli;
        // görünüm çağrısı yine s2 taşımalı.
        Assert.Contains(Getler(parca), y => y.Contains("subeId=s2", StringComparison.Ordinal));
        Assert.DoesNotContain(Getler(parca), y => !y.Contains("subeId=", StringComparison.Ordinal));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#gorunum-rozeti")), TimeSpan.FromSeconds(10));
        Assert.Contains("Şube 2", e.Find("#gorunum-rozeti").TextContent, StringComparison.Ordinal);
    }

    /// <summary>Başka test sınıfının özel sabit fikstürü (yansıma ile — tek kaynak).</summary>
    private static string Fikstur<T>(string ad) =>
        (string)(typeof(T).GetField(ad, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                 ?.GetValue(null) ?? throw new InvalidOperationException($"{typeof(T).Name}.{ad} fikstürü yok"));

    // ── Sözlük (UX-S31) ──────────────────────────────────────────────────────

    [Fact]
    public void Sozluk_TR_EN_dolu_ve_farkli()
    {
        foreach (var p in typeof(SubeKapsamBMetinleri).GetProperties())
        {
            var tr = (string)p.GetValue(SubeKapsamBSozlugu.Tr)!;
            var en = (string)p.GetValue(SubeKapsamBSozlugu.En)!;
            Assert.False(string.IsNullOrWhiteSpace(tr), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(en), p.Name);
            Assert.NotEqual(tr, en);
        }
    }

    // ══ TUR 2 (doğrulayıcı bulguları) ═══════════════════════════════════════

    private DilServisi Dil => Services.GetRequiredService<DilServisi>();

    private const string KasaDefteri2 = """
        {"baslangic":"2026-09-08","bitis":"2026-09-21","saatDilimi":"Europe/Istanbul","utcOfsetSaat":3,"yazSaatiRiski":false,"toplamSatir":2,"kasalar":[
         {"kasaId":"k1","kasaAd":"Merkez Kasa","paraBirimi":"TRY","acilisDeviri":0,"kapanisBakiye":1200.00,"guncelBakiye":1200.00,"gunler":[]},
         {"kasaId":"k2","kasaAd":"Şube 2 Kasası","paraBirimi":"TRY","acilisDeviri":0,"kapanisBakiye":4000.00,"guncelBakiye":4000.00,"gunler":[]}]}
        """;

    /// <summary>KRİTİK (UX-S28): kasa seçenekleri düşerse sebep görünür; defter "kayıt yok" DEMEZ, başka şubenin kasası da görünmez.</summary>
    [Fact]
    public void KasaRaporu_sube_seciliyken_kasalar_duserse_HATA_gorunur_bos_durum_ve_baska_sube_kasasi_YOK()
    {
        d.Hata["/kasalar"] = HttpStatusCode.InternalServerError;
        d.Ek["/raporlar/kasa-defteri"] = KasaDefteri2;
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotEmpty(Getler("/raporlar/kasa-defteri")), TimeSpan.FromSeconds(10));
        e.WaitForAssertion(() => Assert.Contains("/kasalar okunamadı (SKB)", e.Markup, StringComparison.Ordinal),
            TimeSpan.FromSeconds(5));
        Assert.DoesNotContain(Dil.T.RaporSeti.BosDurum, e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Merkez Kasa", e.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void KasaRaporu_sube_seciliyken_defter_YALNIZ_o_subenin_kasalarini_gosterir()
    {
        d.Ek["/kasalar?subeId=s2"] =
            """[{"id":"k2","ad":"Şube 2 Kasası","paraBirimi":"TRY","bakiye":4000.00,"aktif":true,"subeId":"s2"}]""";
        d.Ek["/raporlar/kasa-defteri"] = KasaDefteri2;
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains("Şube 2 Kasası", e.Markup, StringComparison.Ordinal),
            TimeSpan.FromSeconds(10));
        Assert.DoesNotContain("Merkez Kasa", e.Markup, StringComparison.Ordinal);
    }

    /// <summary>ORTA (test açığı): Emanet hareketleri seçili şubeyle istenir.</summary>
    [Fact]
    public void Emanet_cari_secilince_emanet_istegi_subeId_tasir_ve_rozet_cizer()
    {
        d.Ek["/cari?"] = Fikstur<Istek1EkranTestleri>("Cariler");
        d.Ek["/emanet/c1"] = """{"cariId":"c1","mevcutHasGramBakiye":0,"hareketler":[],"madenBakiyeleri":[]}""";
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Emanet>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#e-cari")), TimeSpan.FromSeconds(10));
        SeciciYardim.Sec(e, "e-cari", "c1");
        e.WaitForAssertion(() => Assert.NotEmpty(Getler("/emanet/c1")), TimeSpan.FromSeconds(10));
        Assert.All(Getler("/emanet/c1"), y => Assert.Contains("subeId=s2", y, StringComparison.Ordinal));
        Assert.Contains("Şube 2", e.Find("#gorunum-rozeti").TextContent, StringComparison.Ordinal);
    }

    /// <summary>ORTA (test açığı): ürün listesi kiracı geneli, STOK ADETLERİ seçili şubeden.</summary>
    [Fact]
    public void Urunler_stok_adetleri_subeId_tasir_urun_listesi_tasimaz_rozet_cizer()
    {
        d.Ek["/urunler/stok-adetleri"] =
            """{"urunler":[{"urunId":"u1","toplamAdet":3,"toplamHasGram":42.6},{"urunId":"u2","toplamAdet":1,"toplamHasGram":2.4}]}""";
        d.Ek["/urunler/madenler"] = """{"madenler":[],"etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":""}}""";
        d.Ek["/urun-kategorileri"] = "[]";
        d.Ek["/reyonlar"] = "[]";
        d.Ek["/kur/pariteler"] = """{"pariteler":[]}""";
        d.Ek["/urunler?"] = Fikstur<Istek1EkranTestleri>("Urunler");
        d.Ek["/cari"] = Fikstur<Istek1EkranTestleri>("Cariler");
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotEmpty(Getler("/urunler/stok-adetleri")), TimeSpan.FromSeconds(10));
        Assert.All(Getler("/urunler/stok-adetleri"), y => Assert.Contains("subeId=s2", y, StringComparison.Ordinal));
        Assert.All(Getler("/urunler?"), y => Assert.DoesNotContain("subeId", y, StringComparison.Ordinal));
        e.WaitForAssertion(() => Assert.Equal("3 adet", e.Find("#u-stok-adet-u1").TextContent.Trim()));
        Assert.Contains("Şube 2", e.Find("#gorunum-rozeti").TextContent, StringComparison.Ordinal);
    }

    private const string HurdaBirlesik = """
        {"subeId":"","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":2.748,"girenHas":2.748,"cikanHas":0,"tahlilFarkiHas":0}],
         "fireKaydiAcik":false,"fireNotu":null,"kapsam":"TUM_SUBELER",
         "subeler":[{"subeId":"s1","ad":"Merkez","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":1.832,"girenHas":1.832,"cikanHas":0,"tahlilFarkiHas":0}]},
                    {"subeId":"s2","ad":"Şube 2","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":0.916,"girenHas":0.916,"cikanHas":0,"tahlilFarkiHas":0}]}]}
        """;
    private const string HurdaTekSube = """
        {"subeId":"s1","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":1.832,"girenHas":1.832,"cikanHas":0,"tahlilFarkiHas":0}],
         "fireKaydiAcik":false,"fireNotu":null,"kapsam":"SUBE","subeler":null}
        """;

    private IRenderedComponent<Kuyumcu.Web.Pages.Hurda> HurdaCiz(string? kayitli, string mod)
    {
        d.Ek["/hurda/bakiye"] = HurdaBirlesik;
        d.Ek["/hurda/bakiye?subeId="] = HurdaTekSube;
        d.Ek["/hurda/defter"] = """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":null}""";
        OturumAc(mod: mod);
        d.Kayitli = kayitli;
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hrd-bakiye-listesi")), TimeSpan.FromSeconds(10));
        return e;
    }

    /// <summary>ORTA (§ 4.4): "Tüm şubeler"de Hurda şube kırılımı — PRO açık tablo.</summary>
    [Fact]
    public void Hurda_tum_subelerde_PRO_sube_kirilimi_ACIK_tabloda_her_sube_satiri()
    {
        var e = HurdaCiz("TUMU", ArayuzModuServisi.Profesyonel);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hrd-sube-kirilimi")));
        Assert.Contains("Merkez", e.Find("#hrd-sube-s1-ALTIN").TextContent, StringComparison.Ordinal);
        Assert.Contains("Şube 2", e.Find("#hrd-sube-s2-ALTIN").TextContent, StringComparison.Ordinal);
        Assert.Contains("0,916", e.Find("#hrd-sube-s2-ALTIN").TextContent, StringComparison.Ordinal);
        Assert.Contains(SubeKapsamBSozlugu.Tr.SubeSutunu, e.Find("#hrd-sube-kirilimi thead").TextContent, StringComparison.Ordinal);
    }

    /// <summary>ORTA (§ 4.4 + kurucu "isteğe bağlı kapalı"): BASİT'te kırılım KAPALI katlanır, açınca aynı tablo.</summary>
    [Fact]
    public void Hurda_tum_subelerde_BASIT_sube_kirilimi_KAPALI_katlanir_acinca_gorunur()
    {
        var e = HurdaCiz("TUMU", ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hrd-sube-kirilimi-katlanir")));
        Assert.Empty(e.FindAll("#hrd-sube-kirilimi"));
        e.Find("#hrd-sube-kirilimi-katlanir-ac").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#hrd-sube-s1-ALTIN")));
    }

    [Fact]
    public void Hurda_tek_sube_gorunumunde_sube_kirilimi_YOK()
    {
        var e = HurdaCiz("s1", ArayuzModuServisi.Profesyonel);
        Assert.Empty(e.FindAll("#hrd-sube-kirilimi"));
        Assert.Empty(e.FindAll("#hrd-sube-kirilimi-katlanir"));
    }

    /// <summary>DÜŞÜK: şube seçiliyken kiracı geneli Panel kartları "tüm şubeler birlikte" der; tüm şubelerde demez.</summary>
    [Fact]
    public void Panel_sube_seciliyken_kiraci_geneli_kartlar_TUM_SUBELER_notu_tasir()
    {
        OturumAc(mod: ArayuzModuServisi.Basit);
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-soru-borclu-kapsam")), TimeSpan.FromSeconds(10));
        Assert.Equal(SubeKapsamBSozlugu.Tr.KartTumSubelerNotu, e.Find("#panel-soru-borclu-kapsam").TextContent.Trim());
        Assert.Equal(SubeKapsamBSozlugu.Tr.KartTumSubelerNotu, e.Find("#panel-soru-bekleyen-kapsam").TextContent.Trim());
    }

    [Fact]
    public void Panel_tum_subelerde_kiraci_geneli_not_CIZILMEZ()
    {
        OturumAc(mod: ArayuzModuServisi.Basit);
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#panel-soru-borclu")), TimeSpan.FromSeconds(10));
        e.WaitForAssertion(() => Assert.Equal(SubeKapsamBSozlugu.Tr.RozetTumu, e.Find("#gorunum-rozeti-metin").TextContent.Trim()));
        Assert.Empty(e.FindAll("#panel-soru-borclu-kapsam"));
        Assert.Empty(e.FindAll("#panel-soru-bekleyen-kapsam"));
    }

    /// <summary>DÜŞÜK: Raporlar cari yaşlandırma sekmesi kiracı geneli rozetini taşır; kimlik çakışması yok.</summary>
    [Fact]
    public void Raporlar_cari_sekmesi_KIRACI_GENELI_rozeti_tasir_sayfa_rozeti_tek()
    {
        Services.AddScoped<Kuyumcu.Web.Servisler.PosServisi>();
        d.Ek["/raporlar/katalog"] = Fikstur<RaporlarEkranTestleri>("Katalog");
        d.Ek["/raporlar/stok-yaslandirma"] = Fikstur<RaporlarEkranTestleri>("Yaslandirma");
        d.Ek["/raporlar/pos-fatura-mutabakati"] = Fikstur<RaporlarEkranTestleri>("Mutabakat");
        d.Ek["/raporlar/aylik"] = """{"yil":2026,"ay":9,"gunler":[]}""";
        d.Ek["/raporlar/stok-degeri"] =
            """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""";
        d.Ek["/raporlar/reyon-stok"] = """{"reyonlar":[],"subeler":[]}""";
        d.Ek["/reyonlar"] = "[]";
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yas-kapsam-rozeti-metin")), TimeSpan.FromSeconds(10));
        Assert.Equal(SubeKapsamBSozlugu.Tr.RozetMusteriHesaplari, e.Find("#yas-kapsam-rozeti-metin").TextContent.Trim());
        Assert.Single(e.FindAll("#gorunum-rozeti"));
        Assert.Contains("Şube 2", e.Find("#gorunum-rozeti").TextContent, StringComparison.Ordinal);
    }

    /// <summary>DÜŞÜK (yarış): hızlı A→B geçişinde A'nın GEÇ gelen hatası B seçimini geri ALMAZ.</summary>
    [Fact]
    public async Task Hizli_A_B_gecisinde_A_nin_GEC_hatasi_B_secimini_geri_ALMAZ()
    {
        OturumAc();
        SeciciHazir();
        Assert.Null(KabukServisi.Ortak.GorunumSubesiIcin(Oturum));
        var kapi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        d.IlkPatchKapisi = kapi;
        var api = Services.GetRequiredService<ApiIstemci>();

        var a = KabukServisi.Ortak.GorunumSubesiAtaAsync(api, Oturum, "s1");
        var b = await KabukServisi.Ortak.GorunumSubesiAtaAsync(api, Oturum, "s2");
        Assert.Null(b);
        Assert.Equal("s2", KabukServisi.Ortak.GorunumSubesiIcin(Oturum));

        kapi.SetResult();
        var aSonuc = await a;

        Assert.Null(aSonuc);   // eskimiş yazımın hatası kullanıcıya bant olarak da dönmez
        Assert.Equal("s2", KabukServisi.Ortak.GorunumSubesiIcin(Oturum));
    }

    private static string Gunluk(decimal satis) => $$"""
        {"satis":{"toplamTl":{{satis.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},"alis":{"toplamTl":0},
         "hurda":{"odenenTl":0},"netHasGramHareketi":0,
         "kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":9000.00,"paraBirimi":"TRY"}]}
        """;

    /// <summary>DÜŞÜK (yarış): eski şubenin GEÇ gelen günlük yanıtı yeni seçimin kartını EZMEZ.</summary>
    [Fact]
    public async Task Panel_eski_subenin_GEC_yaniti_yeni_secimin_kartini_EZMEZ()
    {
        var gun = Kuyumcu.Web.Servisler.Bicim.Makine(Kuyumcu.Web.Servisler.Bicim.IsletmeBugun);
        OturumAc();
        var s = SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Panel>();
        e.WaitForAssertion(() => Assert.NotEmpty(Getler("/raporlar/gunluk")), TimeSpan.FromSeconds(10));

        var kapi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (d.Kapi) d.Kapi[$"gunluk?tarih={gun}&subeId=s1"] = kapi;
        d.Ek[$"/raporlar/gunluk?tarih={gun}&subeId=s1"] = Gunluk(11111m);
        d.Ek[$"/raporlar/gunluk?tarih={gun}&subeId=s2"] = Gunluk(22222m);

        Sec(s, "Merkez");
        e.WaitForAssertion(() =>
            Assert.Contains(Getler("/raporlar/gunluk"), y => y.Contains("subeId=s1", StringComparison.Ordinal)),
            TimeSpan.FromSeconds(10));
        Sec(s, "Şube 2");
        e.WaitForAssertion(() => Assert.Contains("22.222", e.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(10));

        kapi.SetResult();
        // Geç yanıtın işlenmesi için zaman tanınır; ardından kart HÂLÂ yeni şubeyi gösterir.
        await Task.Delay(600);
        e.Render();
        Assert.Contains("22.222", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("11.111", e.Markup, StringComparison.Ordinal);
    }

    /// <summary>DÜŞÜK (yarış): Kasa raporunda eski şubenin GEÇ gelen defter yanıtı yeni seçimin tablosunu EZMEZ.</summary>
    [Fact]
    public async Task KasaRaporu_eski_subenin_GEC_defter_yaniti_yeni_secimi_EZMEZ()
    {
        d.Ek["/kasalar?subeId=s2"] =
            """[{"id":"k2","ad":"Şube 2 Kasası","paraBirimi":"TRY","bakiye":4000.00,"aktif":true,"subeId":"s2"}]""";
        d.Ek["/raporlar/kasa-defteri"] = KasaDefteri2;
        OturumAc();
        var s = SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.KasaRaporu>().AyrintiAc();
        e.WaitForAssertion(() => Assert.Contains("Merkez Kasa", e.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(10));

        var kapi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (d.Kapi) d.Kapi["/raporlar/kasa-defteri"] = kapi;   // Merkez'in defter isteği bekler
        var once = Getler("/raporlar/kasa-defteri").Count;
        Sec(s, "Merkez");
        e.WaitForAssertion(() => Assert.True(Getler("/raporlar/kasa-defteri").Count > once), TimeSpan.FromSeconds(10));

        Sec(s, "Şube 2");
        e.WaitForAssertion(() =>
        {
            Assert.Contains("Şube 2 Kasası", e.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Merkez Kasa", e.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(10));

        // Geç yanıt farklı içerikle döner: işlenirse tabloda görünür.
        d.Ek["/raporlar/kasa-defteri"] = KasaDefteri2.Replace("Şube 2 Kasası", "ESKI-YANIT", StringComparison.Ordinal);
        kapi.SetResult();
        await Task.Delay(600);
        e.Render();
        Assert.Contains("Şube 2 Kasası", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ESKI-YANIT", e.Markup, StringComparison.Ordinal);
    }

    // ═══ TUR 3 (doğrulayıcı bulguları) ═══════════════════════════════════════

    private static string KaynakDizini([System.Runtime.CompilerServices.CallerFilePath] string yol = "") =>
        Path.GetDirectoryName(yol)!;

    private static string PozisyonGovdesi(decimal vitrin, decimal hurda, string kapsam) => $$"""
        {"anaMaden":"ALTIN",
         "altin":{"maden":"ALTIN","vitrinGram":{{vitrin.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"hurdaGram":{{hurda.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
                  "cariAlacakGram":2.5,"cariBorcGram":0,"netGram":{{(vitrin + hurda + 2.5m).ToString(System.Globalization.CultureInfo.InvariantCulture)}},
                  "alacakliCariSayisi":1,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},
         "digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,
         "hesaplanmaAni":"2026-10-01T10:00:00Z","donemdenBagimsiz":true,
         "kapsam":"{{kapsam}}","cariKapsam":"KIRACI_GENELI"}
        """;

    /// <summary>
    /// [KRİTİK] Altın pozisyonu kartı seçili şubeyi gösterir: istek `subeId` taşır,
    /// büyük sayı o şubenin yanıtıdır, alacak/borç "tüm şubeler birlikte" notunu taşır;
    /// seçim "Tüm şubeler"e dönünce kart YENİDEN çekilir ve not kalkar.
    /// </summary>
    [Fact]
    public void Pozisyon_karti_secili_subeyi_gosterir_subeId_tasir_secim_degisince_yeniden_ceker()
    {
        OturumAc();
        d.Ek["/pozisyon?subeId=s2"] = PozisyonGovdesi(3.2m, 1.9m, "SUBE");
        d.Ek["/pozisyon"] = PozisyonGovdesi(107.0m, 11.3m, "TUM_SUBELER");
        var s = SeciciHazir();
        var k = Render<AltinPozisyonuKarti>();
        k.WaitForAssertion(() => Assert.Contains("+120,8", k.Find("#pozisyon-net").TextContent, StringComparison.Ordinal));
        Assert.Empty(k.FindAll("#pozisyon-cari-kapsam"));

        Sec(s, "Şube 2");
        k.WaitForAssertion(() => Assert.Contains("+7,6", k.Find("#pozisyon-net").TextContent, StringComparison.Ordinal),
            TimeSpan.FromSeconds(10));
        Assert.Contains(Getler("/pozisyon"), y => y.EndsWith("/pozisyon?subeId=s2", StringComparison.Ordinal));
        Assert.Contains("3,2", k.Find("#pozisyon-vitrin").TextContent, StringComparison.Ordinal);
        Assert.Contains(SubeKapsamBSozlugu.Tr.KartTumSubelerNotu.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")), k.Find("#pozisyon-cari-kapsam").TextContent, StringComparison.Ordinal);

        Sec(s, Sozlukler.Tr.Kabuk.SubeTumu);
        k.WaitForAssertion(() => Assert.Contains("+120,8", k.Find("#pozisyon-net").TextContent, StringComparison.Ordinal),
            TimeSpan.FromSeconds(10));
        Assert.Empty(k.FindAll("#pozisyon-cari-kapsam"));
    }

    /// <summary>Panel'deki kart da seçili şubeyle istenir (sayfa yerleşimi bileşeni şubesiz çağırmasın).</summary>
    [Fact]
    public void Panel_pozisyon_karti_ilk_istekte_bile_secili_subeyle_gider()
    {
        OturumAc(mod: ArayuzModuServisi.Basit);
        d.Kayitli = "s2";
        d.Ek["/pozisyon?subeId=s2"] = PozisyonGovdesi(3.2m, 1.9m, "SUBE");
        d.Ek["/pozisyon"] = PozisyonGovdesi(107.0m, 11.3m, "TUM_SUBELER");
        SeciciHazir();
        var p = Render<Kuyumcu.Web.Pages.Panel>();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#pozisyon-cari-kapsam")), TimeSpan.FromSeconds(10));
        var poz = Getler("/pozisyon");
        Assert.NotEmpty(poz);
        Assert.All(poz, y => Assert.EndsWith("/pozisyon?subeId=s2", y, StringComparison.Ordinal));
    }

    /// <summary>[YÜKSEK] Rozet 12 px hap değil: gövde ölçeğinde düz satır sınıfı (≥ 14 px — CSS belirteci).</summary>
    [Fact]
    public void Rozet_kucuk_hap_DEGIL_govde_olcekli_satir_ve_css_bagli()
    {
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var r = Render<GorunumRozeti>();
        r.WaitForAssertion(() => Assert.NotNull(r.Find("#gorunum-rozeti-metin")));
        Assert.Contains("gorunum-rozeti", r.Find("#gorunum-rozeti").ClassList);
        Assert.DoesNotContain("rozet", r.Find("#gorunum-rozeti-metin").ClassList);

        // Kaynak ağacından (derleme çıktı dizininden bağımsız — mutasyon koşumu -o ile başka dizine derler).
        var kok = Path.Combine(KaynakDizini(), "..", "Kuyumcu.Web", "wwwroot");
        var css = File.ReadAllText(Path.Combine(kok, "css", "sube-kapsam-b.css"));
        var blok = System.Text.RegularExpressions.Regex.Match(css, @"\.gorunum-rozeti\s*\{([^}]*)\}").Groups[1].Value;
        Assert.Contains("font-size: var(--yazi-md)", blok, StringComparison.Ordinal);
        var html = File.ReadAllText(Path.Combine(kok, "index.html"));
        var bizim = html.IndexOf("css/sube-kapsam-b.css", StringComparison.Ordinal);
        Assert.True(bizim > 0 && bizim < html.IndexOf("css/ortak.css", StringComparison.Ordinal));
    }

    /// <summary>[ORTA] Menü seçicisi: etiket "Hangi şube?", "Tüm şubeler" gri yer-tutucu DEĞİL gerçek değer.</summary>
    [Fact]
    public void Secici_etiketi_HANGI_SUBE_tum_subeler_yer_tutucu_degil_gercek_secim()
    {
        OturumAc();
        var s = SeciciHazir();
        s.WaitForAssertion(() => Assert.NotNull(s.Find("#ust-sube-secim")));
        Assert.Contains(SubeKapsamBSozlugu.Tr.HangiSube, s.Find("#ust-sube-secici label").TextContent, StringComparison.Ordinal);
        var metin = s.Find("#ust-sube-secim .secici-metin");
        Assert.Equal(Sozlukler.Tr.Kabuk.SubeTumu, metin.TextContent.Trim());
        Assert.DoesNotContain("yer-tutucu", metin.ClassList);
        Assert.Equal(KabukServisi.TumSubelerDegeri, s.Find("#ust-sube-secim").GetAttribute("data-deger"));
    }

    /// <summary>[YÜKSEK] İşlem defteri: süzgeç başka şubeye çevrilince rozet SÜZGEÇTEKİ şubeyi yazar.</summary>
    [Fact]
    public void IslemDefteri_suzgec_baska_subeye_cevrilince_rozet_gosterilen_subeyi_yazar()
    {
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var p = Render<Kuyumcu.Web.Pages.IslemDefteri>();
        p.WaitForAssertion(() => Assert.Equal(string.Format(SubeKapsamBSozlugu.Tr.RozetSube, "Şube 2"),
            p.Find("#gorunum-rozeti-metin").TextContent.Trim()), TimeSpan.FromSeconds(10));

        p.Find("#id-suzgec-ac").Click();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#id-sube")));
        p.Find("#id-sube").Click();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#id-sube-liste")));
        p.FindAll("#id-sube-liste li[role=option]").First(o => o.TextContent.Trim() == "Merkez").Click();

        p.WaitForAssertion(() => Assert.Equal(string.Format(SubeKapsamBSozlugu.Tr.RozetSube, "Merkez"),
            p.Find("#gorunum-rozeti-metin").TextContent.Trim()), TimeSpan.FromSeconds(10));
        Assert.Contains(Getler("/raporlar/islem-defteri"), y => y.Contains("subeId=s1", StringComparison.Ordinal));
        Assert.Equal("s2", SubeliSorgu.Sube(Oturum));   // görünüm değişmedi — yalnız bu sayfanın süzgeci
    }

    /// <summary>Rozet bileşeni: süzgeç kipi boş değerde "tüm şubeler" yazar, kart rozeti bandı tekrar etmez.</summary>
    [Fact]
    public void Rozet_suzgec_kipi_bos_degerde_tum_subeler_kart_rozeti_bandi_tekrarlamaz()
    {
        OturumAc();
        d.Kayitli = "s2";
        d.KayitliPasif = true;
        SeciciHazir();
        var r = Render<GorunumRozeti>(x => x.Add(p => p.Id, "rs-kapsam-rozeti")
                                           .Add(p => p.SuzgecKullan, true).Add(p => p.SuzgecSubeId, ""));
        r.WaitForAssertion(() => Assert.Equal(SubeKapsamBSozlugu.Tr.RozetTumu, r.Find("#rs-kapsam-rozeti-metin").TextContent.Trim()));
        Assert.Empty(r.FindAll("#gorunum-gecersiz"));
    }

    /// <summary>
    /// [YÜKSEK] Hurda: görünüm kendi şubeden başkayken form İKİNCİ bakiye çizmez (tek rakam);
    /// "Kendi şubeme geç" görünümü oturum şubesine çeker, form o zaman açılır.
    /// </summary>
    [Fact]
    public void Hurda_baska_sube_gorunurken_form_ikinci_bakiye_CIZMEZ_kendi_subeme_gec_duzeltir()
    {
        OturumAc();
        d.Kayitli = "s2";
        d.Ek["/hurda/bakiye?subeId=s2"] = """{"subeId":"s2","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":1.9236,"girenHas":1.9236,"cikanHas":0,"tahlilFarkiHas":0}],"fireKaydiAcik":false,"kapsam":"SUBE"}""";
        d.Ek["/hurda/bakiye?subeId=s1"] = """{"subeId":"s1","satirlar":[{"maden":"ALTIN","madenAdi":"Altın","hasGram":9.389,"girenHas":9.389,"cikanHas":0,"tahlilFarkiHas":0}],"fireKaydiAcik":false,"kapsam":"SUBE"}""";
        d.Ek["/hurda/defter"] = """{"satirlar":[],"toplamAdet":0}""";
        SeciciHazir();
        var p = Render<Kuyumcu.Web.Pages.Hurda>().AyrintiAc();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#hrd-kendi-subeye-gec")), TimeSpan.FromSeconds(10));
        Assert.DoesNotContain("9,389", p.Markup, StringComparison.Ordinal);
        Assert.Empty(p.FindAll("#hrd-form-alan"));
        Assert.DoesNotContain(Getler("/hurda/bakiye"), y => y.Contains("subeId=s1", StringComparison.Ordinal));

        p.Find("#hrd-kendi-subeye-gec").Click();
        p.WaitForAssertion(() => Assert.NotNull(p.Find("#hrd-form-alan")), TimeSpan.FromSeconds(10));
        Assert.Equal("s1", d.Kayitli);
        Assert.Empty(p.FindAll("#hrd-form-sube-notu"));
        p.WaitForAssertion(() => Assert.Contains("9,389", p.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("1,9236", p.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// [YÜKSEK] Rapor merkezi → Reyon stok: kartın şube kutusu görünüm şubesinden başka
    /// şubeye çevrilince kart KENDİ rozetini taşır (süzgeçteki şube); aynıyken ikinci rozet yok.
    /// </summary>
    [Fact]
    public void Raporlar_reyon_stok_sube_kutusu_baska_subede_kart_rozeti_suzgec_subesini_yazar()
    {
        Services.AddScoped<Kuyumcu.Web.Servisler.PosServisi>();
        d.Ek["/raporlar/katalog"] = Fikstur<RaporlarEkranTestleri>("Katalog");
        d.Ek["/raporlar/stok-yaslandirma"] = Fikstur<RaporlarEkranTestleri>("Yaslandirma");
        d.Ek["/raporlar/pos-fatura-mutabakati"] = Fikstur<RaporlarEkranTestleri>("Mutabakat");
        d.Ek["/raporlar/aylik"] = """{"yil":2026,"ay":9,"gunler":[]}""";
        d.Ek["/raporlar/stok-degeri"] =
            """{"urunler":[],"toplamHas":0,"madenDagilimi":[],"toplamSatir":0,"ofset":0,"limit":500,"dahaFazlasiVar":false,"uyari":null}""";
        d.Ek["/raporlar/reyon-stok"] = """{"reyonlar":[],"subeler":[]}""";
        d.Ek["/reyonlar"] = "[]";
        OturumAc();
        d.Kayitli = "s2";
        SeciciHazir();
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rs-sube")), TimeSpan.FromSeconds(10));
        Assert.Empty(e.FindAll("#rs-kapsam-rozeti"));

        e.Find("#rs-sube").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#rs-sube-liste")));
        e.FindAll("#rs-sube-liste li[role=option]").First(o => o.TextContent.Trim() == "Merkez").Click();

        e.WaitForAssertion(() => Assert.Equal(string.Format(SubeKapsamBSozlugu.Tr.RozetSube, "Merkez"),
            e.Find("#rs-kapsam-rozeti-metin").TextContent.Trim()), TimeSpan.FromSeconds(10));
        Assert.Contains(Getler("/raporlar/reyon-stok"), y => y.Contains("subeId=s1", StringComparison.Ordinal));
        Assert.Single(e.FindAll("#gorunum-rozeti"));
        Assert.Contains("Şube 2", e.Find("#gorunum-rozeti").TextContent, StringComparison.Ordinal);
    }
}
