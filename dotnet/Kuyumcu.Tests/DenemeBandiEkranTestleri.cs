using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// DENEME / SALT-OKUNUR BANDI — paket F-2-min Faz A [bUnit]
//
// Sunucu SON SÖZÜ söyler: ekran kalan günü ve salt-okunurluğu HESAPLAMAZ,
// GET /admin/tenant yanıtından okur (UX-S21). AKTIF kiracıda band çizilmez.
//
// DoD EK md.1-3: salt-okunur iken kaydeden her düğme disabled'dır ve NEDENİ
// ekranda yazılıdır (UX-S30).
//
// ⚠ FAZ B (/sistem/kiracilar yönetici konsolu) BU DALGADA YOKTUR (MÜHÜR 4/K5,
//   dalga 6). Bu yüzden onun bUnit testleri de burada YOKTUR — tasarım
//   metnindeki SistemKiracilar_* testleri Faz B ile birlikte gelecektir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DenemeBandiEkranTestleri : BunitContext
{
    private string tenantJson =
        """{"id":"t1","ad":"Demo Kuyumcu","plan":"BASLANGIC","aiEtkin":false,"durum":"AKTIF","kalanGun":null,"saltOkunurMu":false}""";

    public DenemeBandiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(() => tenantJson))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(Func<string> tenant) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var g = yol switch
            {
                var y when y.EndsWith("/admin/tenant") => tenant(),
                // PAKET P-27: abonelik ucu GERÇEKÇİ yanıt döner ("{}" varsayılanı
                // ekranı yanlış duruma düşürürdü). Kiracı EN DÜŞÜK plandadır
                // (BASLANGIC) — bu yüzden indirilecek plan YOKTUR ve seçici
                // hiç çizilmez (UX-S22); yerine gerekçe yazılır.
                var y when y.Contains("/abonelik") => """
                    {"planKodu":"BASLANGIC","planAdi":"Başlangıç","seviye":10,
                     "donem":null,"kalanGun":null,"suresiDolduMu":false,"yazabilirMi":true,
                     "yukseltmeKiraciyaKapali":true,
                     "yukseltmeKapaliSebebi":"Plan yükseltme işletme hesabından yapılamaz.",
                     "planlar":[{"kod":"BASLANGIC","ad":"Başlangıç","seviye":10,"aktif":true},
                                {"kod":"PRO","ad":"Pro","seviye":20,"aktif":true}],
                     "indirilebilirPlanlar":[],"gecmis":[],
                     "gecmisSiniri":50,"gecmisSinirinaDegdi":false}
                    """,
                var y when y.EndsWith("/subeler") =>
                    """[{"id":"s1","ad":"Merkez","adres":null,"telefon":null,"aktif":true},{"id":"s2","ad":"Sube 2","adres":null,"telefon":null,"aktif":true}]""",
                var y when y.Contains("/admin") => tenant(),
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private void Deneme(int kalanGun, bool saltOkunur = false) => tenantJson =
        $$"""{"id":"t1","ad":"Demo Kuyumcu","plan":"BASLANGIC","aiEtkin":false,"durum":"DENEME","kalanGun":{{kalanGun}},"saltOkunurMu":{{(saltOkunur ? "true" : "false")}}}""";

    private void SaltOkunur() => tenantJson =
        """{"id":"t1","ad":"Demo Kuyumcu","plan":"BASLANGIC","aiEtkin":false,"durum":"SALT_OKUNUR","kalanGun":null,"saltOkunurMu":true}""";

    private IRenderedComponent<Kuyumcu.Web.Pages.Isletme> Ciz()
        => Render<Kuyumcu.Web.Pages.Isletme>();

    // ── Band görünürlüğü ─────────────────────────────────────────────────────

    [Fact]
    public void Isletme_Deneme_Kalan_Gun_Rozeti_Gorunur()
    {
        Deneme(kalanGun: 9);
        var e = Ciz();

        Assert.NotNull(e.Find("#i-durum-bandi"));
        Assert.Contains("Deneme", e.Find("#i-durum-rozeti").TextContent);
        Assert.Contains("9", e.Find("#i-kalan-gun").TextContent);
        Assert.Contains("kalan gün", e.Find("#i-kalan-gun").TextContent);
    }

    [Fact]
    public void AKTIF_Kiracida_Band_Gorunmez()
    {
        var e = Ciz();
        Assert.Empty(e.FindAll("#i-durum-bandi"));
        Assert.Empty(e.FindAll("#i-yazma-kilidi"));
    }

    [Fact]
    public void SaltOkunur_Bandi_DisaAktarim_Baglantisi_Icerir()
    {
        SaltOkunur();
        var e = Ciz();

        var bag = e.Find("#i-disa-aktarim-bag");
        // KVKK/VUK gerekçesi: veri rehin alınmaz, indirme yolu bandın içindedir.
        Assert.Equal("/veri-disa-aktarim", bag.GetAttribute("href"));
        Assert.Contains("Verilerinizi dışa aktarın", bag.TextContent);
    }

    // ── Yazma kilidi ─────────────────────────────────────────────────────────

    [Fact]
    public void SaltOkunur_Iken_Kaydet_Dugmesi_Disabled_Ve_Neden_Yazili()
    {
        SaltOkunur();
        var e = Ciz();

        Assert.True(e.Find("#i-kimlik-duzenle").HasAttribute("disabled"));
        Assert.True(e.Find("#s-yeni-sube-ac").HasAttribute("disabled"));
        Assert.True(e.Find("#i-vezne-zorunlu").HasAttribute("disabled"));

        // PAKET P-27: en düşük planda plan SEÇİCİSİ hiç çizilmez (UX-S22);
        // yerine değişmezlik gerekçesi durur (UX-S25) — "tıklanınca hiçbir şey
        // olmayan" bir düğme bırakılmaz (DoD EK md.1).
        Assert.Empty(e.FindAll("#i-plan"));
        Assert.NotNull(e.Find("#degismez-ipucu-Abonelik"));

        // UX-S30: kilidin SEBEBİ ekranda yazılı.
        Assert.Contains("salt okunur", e.Find("#i-yazma-kilidi").TextContent,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deneme_suresi_dolmus_kiracida_da_yazma_kilitlidir()
    {
        // durum DENEME ama sunucu saltOkunurMu=true diyor (süre doldu).
        Deneme(kalanGun: 0, saltOkunur: true);
        var e = Ciz();

        Assert.True(e.Find("#i-kimlik-duzenle").HasAttribute("disabled"));
        Assert.Contains("Deneme süreniz doldu", e.Find("#i-deneme-bitti").TextContent);
        Assert.NotNull(e.Find("#i-disa-aktarim-bag"));
    }

    [Fact]
    public void Deneme_iken_yazma_ACIK_kalir()
    {
        Deneme(kalanGun: 3);
        var e = Ciz();

        Assert.False(e.Find("#i-kimlik-duzenle").HasAttribute("disabled"));
        Assert.False(e.Find("#s-yeni-sube-ac").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#i-yazma-kilidi"));
    }
}
