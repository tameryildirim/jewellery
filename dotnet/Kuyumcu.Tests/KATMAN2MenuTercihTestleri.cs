using System.Net;
using System.Net.Http.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Tercih;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KATMAN-2 — "Grubun açık/kapalı durumu kaydedilemedi, geri alındı"
// kalıcı kırmızı bandı (25.09 tam tur + göz incelemesi, 5 ekranda).
//
// ÖLÇÜLEN KÖK NEDEN (30.09, canlı API 5166, kuyumcu_net, kendi test kiracısı):
//   • 4 rol × sıralı aç-kapa → hepsi 200 (yetki sorunu YOK)
//   • aynı kullanıcı, aynı anahtar, satır YOKKEN 2 eşanlı PATCH → 10 turun
//     10'unda biri 409 ("Bu kayıt zaten var", MySQL 1062) ya da 500 (MySQL 1213
//     kilitlenme) — iki istek de "satır yok" görüp INSERT ediyor.
//   • satır VARKEN 10 eşanlı PATCH → 10×200; satır YOKKEN → 5×409.
//   İstemci her tıklamada AYRI ve EŞANLI PATCH açtığı için (tek yazıcı yoktu)
//   iki sekme / hızlı çift tıklama ilk katlamada bu yarışa giriyordu.
//
// DÜZELTME: (1) sunucu yarışı kaybeden yazmayı baştan okuyup yeniden dener
// (TercihServisi.Yama, sınırlı döngü, sonunda hata GİZLENMEZ); (2) istemci
// katlamada TEK YAZICI + birleştirme (+ durulma beklemesi) kullanır; (3) bant
// yalnız kalıcı hatada çıkar ve kapatılabilir.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KATMAN2TercihHttpTestleri(HttpFikstur f)
{
    private const string Yol = "/api/v1/tercihler/benim";

    private (Kiraci K, Dictionary<string, string> RolKullanici) RolluKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, ad);
        var roller = new Dictionary<string, string> { ["SAHIP"] = k.KullaniciId };
        var now = DateTime.UtcNow;
        foreach (var rol in new[] { "MUDUR", "TEZGAHTAR", "MUHASEBE" })
        {
            var id = Kimlik.Ulid();
            db.Kullanicilar.Add(new Kullanici
            {
                Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Ad = rol, Soyad = "Test",
                Email = $"{id}@test.local", SifreHash = "x", Rol = rol, Aktif = true,
                Olusturuldu = now, Guncellendi = now,
            });
            roller[rol] = id;
        }
        db.SaveChanges();
        return (k, roller);
    }

    private static object Govde(string anahtar, string? deger) =>
        new { tercihler = new Dictionary<string, string?> { [anahtar] = deger } };

    [Theory]
    [InlineData("SAHIP")]
    [InlineData("MUDUR")]
    [InlineData("TEZGAHTAR")]
    [InlineData("MUHASEBE")]
    public async Task Her_rol_KENDI_katlamasini_ac_kapa_yazar_200(string rol)
    {
        var (k, roller) = RolluKiraci("KATMAN2-ROL-" + rol);
        var istemci = f.Ortam.Istemci(roller[rol], k.TenantId, k.SubeId, rol);

        var ac = await istemci.PatchAsJsonAsync(Yol, Govde(TercihAnahtarlari.MenuAcikGruplar, """["rapor"]"""));
        var kapa = await istemci.PatchAsJsonAsync(Yol, Govde(TercihAnahtarlari.MenuAcikGruplar, "[]"));

        Assert.Equal(HttpStatusCode.OK, ac.StatusCode);
        Assert.Equal(HttpStatusCode.OK, kapa.StatusCode);
        using var db = f.Vt.Olustur(k.TenantId);
        var satir = await db.KullaniciTercihleri.AsNoTracking().SingleAsync(t =>
            t.KullaniciId == roller[rol] && t.Anahtar == TercihAnahtarlari.MenuAcikGruplar);
        Assert.Equal("[]", satir.Deger);   // kimlik JETONDAN: satır o rolün kullanıcısına yazıldı
    }

    [Fact]
    public async Task Satir_YOKKEN_ayni_anahtara_ESANLI_on_PATCH_hepsi_200_ve_TEK_satir()
    {
        // Ölçülen kök neden: bu senaryoda düzeltmeden önce 10 isteğin ~5'i 409/500.
        // Üç tur koşulur; yarışın hiç oluşmaması olasılığı ihmal edilebilir.
        var (k, roller) = RolluKiraci("KATMAN2-ESANLI-AYNI");
        var istemci = f.Ortam.Istemci(roller["MUDUR"], k.TenantId, k.SubeId, "MUDUR");

        for (var tur = 0; tur < 3; tur++)
        {
            using (var db = f.Vt.Olustur(k.TenantId))
            {
                db.KullaniciTercihleri.RemoveRange(
                    db.KullaniciTercihleri.Where(t => t.KullaniciId == roller["MUDUR"]));
                await db.SaveChangesAsync();
            }

            var istekler = Enumerable.Range(0, 10).Select(i => istemci.PatchAsJsonAsync(Yol,
                Govde(TercihAnahtarlari.MenuAcikGruplar, $"[\"g{i}\"]"))).ToArray();
            var yanitlar = await Task.WhenAll(istekler);

            foreach (var y in yanitlar)
                Assert.True(y.StatusCode == HttpStatusCode.OK,
                    $"tur {tur}: {(int)y.StatusCode} {await y.Content.ReadAsStringAsync()}");
            using var db2 = f.Vt.Olustur(k.TenantId);
            var satirlar = await db2.KullaniciTercihleri.AsNoTracking()
                .Where(t => t.KullaniciId == roller["MUDUR"] && t.Anahtar == TercihAnahtarlari.MenuAcikGruplar)
                .ToListAsync();
            Assert.Single(satirlar);
            Assert.Matches("""^\["g[0-9]"\]$""", satirlar[0].Deger);
        }
    }

    [Fact]
    public async Task Esanli_iki_PATCH_FARKLI_anahtar_ikisi_de_KALICI()
    {
        var (k, roller) = RolluKiraci("KATMAN2-ESANLI-FARKLI");
        var istemci = f.Ortam.Istemci(roller["TEZGAHTAR"], k.TenantId, k.SubeId, "TEZGAHTAR");

        var yanitlar = await Task.WhenAll(
            istemci.PatchAsJsonAsync(Yol, Govde(TercihAnahtarlari.MenuAcikGruplar, """["rapor"]""")),
            istemci.PatchAsJsonAsync(Yol, Govde(TercihAnahtarlari.MenuFavoriler, """["/kasalar"]""")));

        Assert.All(yanitlar, y => Assert.Equal(HttpStatusCode.OK, y.StatusCode));
        using var db = f.Vt.Olustur(k.TenantId);
        var t = await db.KullaniciTercihleri.AsNoTracking()
            .Where(x => x.KullaniciId == roller["TEZGAHTAR"]).ToDictionaryAsync(x => x.Anahtar, x => x.Deger);
        Assert.Equal("""["rapor"]""", t[TercihAnahtarlari.MenuAcikGruplar]);
        Assert.Equal("""["/kasalar"]""", t[TercihAnahtarlari.MenuFavoriler]);
    }

    [Fact]
    public async Task Yeniden_deneme_DOGRULAMA_hatasini_yutmaz_bilinmeyen_anahtar_yine_400()
    {
        // Döngü yalnız eşanlı yazma çakışmasını (DbUpdateException) yeniden dener;
        // beyaz liste ihlali 400 kalır (sessiz kabul yok).
        var (k, roller) = RolluKiraci("KATMAN2-400");
        var y = await f.Ortam.Istemci(roller["MUHASEBE"], k.TenantId, k.SubeId, "MUHASEBE")
            .PatchAsJsonAsync(Yol, Govde("menu-bilinmeyen", "[]"));
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }
}

public sealed class KATMAN2MenuTercihEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Govde)> patchler = [];
    private TaskCompletionSource<HttpStatusCode>? kapi;   // null = PATCH anında 200
    private readonly List<TimeSpan> beklemeler = [];
    private string sunucuTercihleri = "{}";
    private int ilkKacPatch429;   // ilk N PATCH 429 (Retry-After: 1) döner
    private readonly List<string> yazmaSirasi = [];   // PATCH/DELETE isteklerinin GÖNDERİLİŞ sırası

    private sealed class SahteApi(KATMAN2MenuTercihEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            var kod = HttpStatusCode.OK;
            if (istek.Method != HttpMethod.Get)
                lock (t.yazmaSirasi) t.yazmaSirasi.Add(istek.Method.Method);
            if (istek.Method == HttpMethod.Patch)
            {
                int sira;
                lock (t.patchler) { t.patchler.Add((istek.Method.Method, govde)); sira = t.patchler.Count; }
                if (sira <= t.ilkKacPatch429)
                {
                    var r = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("""{"message":"çok fazla istek","statusCode":429}""",
                            System.Text.Encoding.UTF8, "application/json"),
                    };
                    r.Headers.TryAddWithoutValidation("Retry-After", "1");
                    return r;
                }
                if (t.kapi is { } k) kod = await k.Task;
            }
            if (kod != HttpStatusCode.OK)
                return new HttpResponseMessage(kod)
                {
                    Content = new StringContent("""{"message":"kalıcı hata"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };
            var cevap = yol switch
            {
                var y when y.Contains("/tercihler/benim", StringComparison.Ordinal) =>
                    $$"""{"tercihler":{{t.sunucuTercihleri}},"mesaj":"Tercihler kaydedildi"}""",
                var y when y.Contains("/yetkiler/benim", StringComparison.Ordinal) =>
                    """{"moduller":["cari","urun","islem","kasa","banka","fatura","rapor","kullanici","evrak","siparis","bildirim"]}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cevap, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    public KATMAN2MenuTercihEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
    }

    private IRenderedComponent<KenarCubugu> Ciz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "MUDUR", "s1", null,
                                ArayuzModuServisi.Profesyonel));
        // Durulma beklemesi gerçek saatle değil, kayıtla sınanır.
        Services.GetRequiredService<MenuTercihServisi>().Bekle = sure =>
        {
            beklemeler.Add(sure);
            return Task.CompletedTask;
        };
        // ApiIstemci'nin kendi 429 geri çekilmesi de gerçek saatle beklemesin.
        Services.GetRequiredService<ApiIstemci>().Uyu = _ => Task.CompletedTask;
        var e = Render<KenarCubugu>();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".kenar-nav a")));
        return e;
    }

    private static bool Katli(IRenderedComponent<KenarCubugu> e, string slug) =>
        e.Find($"#kenar-grup-dugme-{slug}").GetAttribute("aria-expanded") == "false";

    // Find + Click aynı dağıtıcı turunda. Arka plandan (HTTP devamı) gelen yeniden
    // çizim olay kimliğini yine de eskitebilir: o zaman DOM yeniden bulunur ve tık
    // TEKRARLANIR — tık hiç dağıtılmadığı için çift tıklama oluşmaz.
    private static void Tikla(IRenderedComponent<KenarCubugu> e, string secici)
    {
        for (var deneme = 1; ; deneme++)
        {
            try
            {
                e.InvokeAsync(() => e.Find(secici).Click()).GetAwaiter().GetResult();
                return;
            }
            catch (Bunit.Rendering.UnknownEventHandlerIdException) when (deneme < 5)
            {
                e.Render();   // DOM'u güncel çizim ağacına eşitle, sonra yeniden bul
            }
        }
    }

    private int PatchSayisi { get { lock (patchler) return patchler.Count; } }

    [Fact]
    public void On_hizli_tiklama_ESANLI_PATCH_acmaz_SON_durum_yazilir_bant_YOK()
    {
        kapi = new TaskCompletionSource<HttpStatusCode>(TaskCreationOptions.RunContinuationsAsynchronously);
        var e = Ciz();

        for (var i = 0; i < 10; i++) Tikla(e, "#kenar-grup-dugme-rapor");   // çift sayı → son durum KAPALI

        e.WaitForAssertion(() => Assert.Equal(1, PatchSayisi));
        Assert.True(Katli(e, "rapor"));            // ekran anında son tıklamayı gösterir
        Assert.Equal(1, PatchSayisi);              // yazma sürerken 9 tıklama YENİ istek açmadı

        kapi.SetResult(HttpStatusCode.OK);         // ilk yazma döndü → birleşik SON durum yazılır

        e.WaitForAssertion(() => Assert.Equal(2, PatchSayisi));
        string son;
        lock (patchler) son = patchler[^1].Govde;
        Assert.Contains("menu-acik-gruplar", son, StringComparison.Ordinal);
        Assert.DoesNotContain("rapor", son, StringComparison.Ordinal);   // son durum: rapor KAPALI
        Assert.Empty(e.FindAll("#kenar-sira-hatasi"));
        Assert.True(Katli(e, "rapor"));
    }

    [Fact]
    public void Yazma_surerken_gelen_tiklamalar_DURULMA_beklemesinden_sonra_yazilir()
    {
        kapi = new TaskCompletionSource<HttpStatusCode>(TaskCreationOptions.RunContinuationsAsynchronously);
        var e = Ciz();

        Tikla(e, "#kenar-grup-dugme-rapor");
        e.WaitForAssertion(() => Assert.Equal(1, PatchSayisi));
        Assert.Empty(beklemeler);                  // ilk tıklama BEKLEMEDEN yazılır
        Tikla(e, "#kenar-grup-dugme-yonetim");
        kapi.SetResult(HttpStatusCode.OK);

        e.WaitForAssertion(() => Assert.Equal(2, PatchSayisi));
        Assert.Contains(TimeSpan.FromMilliseconds(MenuTercihServisi.KatlamaSukunetMs), beklemeler);
        Assert.InRange(MenuTercihServisi.KatlamaSukunetMs, 300, 500);
        string son;
        lock (patchler) son = patchler[^1].Govde;
        Assert.Contains("rapor", son, StringComparison.Ordinal);
        Assert.Contains("yonetim", son, StringComparison.Ordinal);
    }

    [Fact]
    public void Tek_tiklama_TEK_PATCH_bekleme_yok()
    {
        var e = Ciz();

        Tikla(e, "#kenar-grup-dugme-rapor");

        e.WaitForAssertion(() => Assert.Equal(1, PatchSayisi));
        Assert.Empty(beklemeler);
        Assert.False(Katli(e, "rapor"));
        Assert.Empty(e.FindAll("#kenar-sira-hatasi"));
    }

    [Fact]
    public void Kalici_hatada_SUNUCUNUN_bildigi_duruma_doner_bant_TEK_ve_KAPATILABILIR()
    {
        // Sunucunun bildiği durum: yalnız para-banka açık.
        sunucuTercihleri = $$"""{"{{MenuTercihServisi.AnahtarAcikGruplar}}":"[\"para-banka\"]"}""";
        kapi = new TaskCompletionSource<HttpStatusCode>(TaskCreationOptions.RunContinuationsAsynchronously);
        var e = Ciz();
        e.WaitForAssertion(() => Assert.False(Katli(e, "para-banka")));
        Tikla(e, "#kenar-grup-dugme-rapor");
        Tikla(e, "#kenar-grup-dugme-yonetim");

        kapi.SetResult(HttpStatusCode.InternalServerError);

        e.WaitForAssertion(() => Assert.Single(e.FindAll("#kenar-sira-hatasi")));
        Assert.True(Katli(e, "rapor"));            // sunucu ikisini de bilmiyordu → ikisi de geri
        Assert.True(Katli(e, "yonetim"));
        Assert.False(Katli(e, "para-banka"));      // sunucunun bildiği açık grup açık kaldı
        Assert.Equal(1, PatchSayisi);              // hatadan sonra birikmiş durum YAZILMAZ
        Assert.Contains(MenuSozlugu.KatlamaKaydedilemedi("tr"),
            e.Find("#kenar-sira-hatasi").TextContent, StringComparison.Ordinal);

        Tikla(e, "#kenar-sira-hatasi-kapat");

        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#kenar-sira-hatasi")));
    }

    [Fact]
    public void Gecici_429_SESSIZCE_yeniden_denenir_bant_YOK_durum_KORUNUR()
    {
        // ApiIstemci 1 + 3 geri çekilme = 4 istek sonra OranSinirHatasi atar;
        // katlama yazıcısı bunu kalıcı hata saymaz, Retry-After kadar bekleyip yeniden yazar.
        ilkKacPatch429 = 4;
        var e = Ciz();

        Tikla(e, "#kenar-grup-dugme-rapor");

        e.WaitForAssertion(() => Assert.Equal(5, PatchSayisi));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#kenar-sira-hatasi")));
        Assert.False(Katli(e, "rapor"));           // geri alınmadı
        Assert.Contains(TimeSpan.FromSeconds(1), beklemeler);
    }

    [Fact]
    public void Suren_429_denemeler_BITINCE_bant_cizilir_sessiz_kalinmaz()
    {
        ilkKacPatch429 = int.MaxValue;
        var e = Ciz();

        Tikla(e, "#kenar-grup-dugme-rapor");

        e.WaitForAssertion(() => Assert.Single(e.FindAll("#kenar-sira-hatasi")));
        Assert.True(Katli(e, "rapor"));
        Assert.Equal(MenuTercihServisi.KatlamaOranYenidenDeneme,
            beklemeler.Count(b => b == TimeSpan.FromSeconds(1)));
    }

    // ── Hasım bulgusu (KRİTİK) — tek yazıcı alanı tamamlanmış göreve takılmamalı ──

    [Fact]
    public void Ilk_yazma_BITTIKTEN_sonra_ikinci_tiklama_da_yazilir()
    {
        var e = Ciz();

        Tikla(e, "#kenar-grup-dugme-rapor");
        e.WaitForAssertion(() => Assert.Equal(1, PatchSayisi));
        Tikla(e, "#kenar-grup-dugme-yonetim");

        e.WaitForAssertion(() => Assert.Equal(2, PatchSayisi));
        string son;
        lock (patchler) son = patchler[^1].Govde;
        Assert.Contains("yonetim", son, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir()
    {
        var saat = DateTimeOffset.Parse("2026-09-30T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        // Sahte saat Ciz'den ÖNCE bağlanır: açılıştaki GET /tercihler/benim (MenuTercihServisi.HazirlaAsync)
        // de aynı uçtur ve pencereye sayılır. Saat sonradan bağlanınca o GET GERÇEK saatle pencere
        // açıyordu; gerçek saat sabit tarihi geçince (30.09 10:00Z sonrası) pencere hiç sıfırlanmadı
        // ve doldurma döngüsünün 60. isteği 61. sayıldı → test duvar saatine bağlı kırmızıydı.
        var api = Services.GetRequiredService<ApiIstemci>();
        api.Simdi = () => saat;
        var e = Ciz();
        saat = saat.AddSeconds(61);                  // açılış GET'inin penceresi kapandı; doldurma temiz pencerede
        // Aynı uca (favori/sıra/panel ile ORTAK /tercihler/benim) pencere dolduruluyor.
        for (var i = 0; i < ApiIstemci.IstekTekrarTavani; i++)
            await api.PatchAsync<object>("/tercihler/benim", new { tercihler = new Dictionary<string, string?>() });
        var dolu = PatchSayisi;

        Tikla(e, "#kenar-grup-dugme-rapor");   // FirtinaKontrol ilk await'ten ÖNCE fırlar → yazma senkron biter

        e.WaitForAssertion(() => Assert.Single(e.FindAll("#kenar-sira-hatasi")));   // sessiz kalınmaz
        Assert.Equal(dolu, PatchSayisi);             // istek ATILMADI
        Tikla(e, "#kenar-sira-hatasi-kapat");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#kenar-sira-hatasi")));

        saat = saat.AddSeconds(61);                  // pencere boşaldı
        Tikla(e, "#kenar-grup-dugme-rapor");

        e.WaitForAssertion(() => Assert.Equal(dolu + 1, PatchSayisi));
        Assert.Empty(e.FindAll("#kenar-sira-hatasi"));
        string son;
        lock (patchler) son = patchler[^1].Govde;
        Assert.Contains("menu-acik-gruplar", son, StringComparison.Ordinal);
    }

    [Fact]
    public void Grup_sifirla_ucustaki_PATCH_bitmeden_DELETE_atmaz()
    {
        kapi = new TaskCompletionSource<HttpStatusCode>(TaskCreationOptions.RunContinuationsAsynchronously);
        var e = Ciz();

        Tikla(e, "#kenar-grup-dugme-rapor");
        e.WaitForAssertion(() => Assert.Equal(1, PatchSayisi));
        e.WaitForAssertion(() => Assert.Single(e.FindAll("#kenar-katlama-sifirla")));
        Tikla(e, "#kenar-katlama-sifirla");

        Thread.Sleep(100);
        lock (yazmaSirasi) Assert.DoesNotContain("DELETE", yazmaSirasi);   // PATCH uçuştayken DELETE YOK

        kapi.SetResult(HttpStatusCode.OK);

        e.WaitForAssertion(() => { lock (yazmaSirasi) Assert.Contains("DELETE", yazmaSirasi); });
        lock (yazmaSirasi) Assert.Equal(["PATCH", "DELETE"], yazmaSirasi);   // son söz DELETE'in
        Assert.Equal(1, PatchSayisi);                                          // sıfırlanan durum PATCH'lenmedi
        Assert.Empty(e.FindAll("#kenar-katlama-sifirla"));                    // kayıtlı tercih yok
        Assert.Empty(e.FindAll("#kenar-sira-hatasi"));
    }
}
