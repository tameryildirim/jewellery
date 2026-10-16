using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-11 (B-24) — ZAMANLANMIŞ İŞ AYARLARI, GERÇEK HTTP BORU HATTI.
//
// B-24: "zamanlanmış iş ayarları düzenlenemiyor" — saat/eşik/açık-kapalı yalnız
// appsettings.json'daydı. Bu dosya ucun VAR olduğunu değil, SÖZLEŞMESİNE
// uyduğunu ölçer: PATCH üç durumlu (yok/null/değer), sınırlar sunucuda
// zorlanır, rol kapısı kapalı, kiracı ayarı YALNIZ DARALTIR ve denetim izi düşer.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P11IsAyarHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static Task<HttpResponseMessage> Patch(HttpClient c, string json) =>
        c.PatchAsync("/api/v1/isler/ayarlar", new StringContent(json, Encoding.UTF8, "application/json"));

    /// <summary>Hata gövdesindeki insan mesajı — hangi alanda olursa olsun.</summary>
    private static string Mesaj(JsonElement g) =>
        g.TryGetProperty("message", out var msg) ? msg.GetString() ?? ""
        : g.TryGetProperty("mesaj", out var m) ? m.GetString() ?? ""
        : g.TryGetProperty("hata", out var h) ? h.GetString() ?? ""
        : g.ToString();

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        JsonDocument.Parse(await c.Content.ReadAsStringAsync()).RootElement.Clone();

    // ── 1) Ayar OKUNUR: /isler ayarlar bloğunu ve sınırları döner ───────────

    [Fact]
    public async Task Isler_ucu_AYARLARI_ve_SINIRLARI_dondurur()
    {
        var k = YeniKiraci("P11-TEST-oku");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/isler");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var g = await Govde(cevap);
        var a = g.GetProperty("ayarlar");
        // UX-S29/S30: sınır ve varsayılan SUNUCUDAN gelir; ekran kendi kopyasını tutmaz.
        Assert.Equal(0, a.GetProperty("baslangicSaat").GetInt32());
        Assert.Equal(0, a.GetProperty("saatEnAz").GetInt32());
        Assert.Equal(23, a.GetProperty("saatEnCok").GetInt32());
        Assert.True(a.GetProperty("vadeUfukGun").GetInt32() > 0);
        Assert.Equal(0, a.GetProperty("kapaliIsler").GetArrayLength());

        // Her iş satırı iki AYRI kapı taşır (UX-S30: "neden kapalı?").
        foreach (var i in g.GetProperty("isler").EnumerateArray())
        {
            Assert.True(i.TryGetProperty("yapilandirmaEtkin", out _));
            Assert.True(i.TryGetProperty("kiraciKapatti", out _));
        }
    }

    // ── 2) PATCH gerçekten yazar ve GET aynısını döner ──────────────────────

    [Fact]
    public async Task Patch_saat_ve_esikleri_YAZAR_ve_GET_ayni_degeri_doner()
    {
        var k = YeniKiraci("P11-TEST-yaz");
        var c = f.Ortam.Istemci(k, "SAHIP");

        var p = await Patch(c, """{"baslangicSaat":9,"vadeUfukGun":21,"masakUyariGun":5}""");
        Assert.Equal(HttpStatusCode.OK, p.StatusCode);
        var pg = await Govde(p);
        Assert.Equal(9, pg.GetProperty("baslangicSaat").GetInt32());
        Assert.Equal(21, pg.GetProperty("vadeUfukGun").GetInt32());

        var g = await Govde(await c.GetAsync("/api/v1/isler"));
        var a = g.GetProperty("ayarlar");
        Assert.Equal(9, a.GetProperty("baslangicSaat").GetInt32());
        Assert.Equal(21, a.GetProperty("vadeUfukGun").GetInt32());
        Assert.Equal(5, a.GetProperty("masakUyariGun").GetInt32());

        // Ayar MEVCUT kiraci_ayarlari tablosuna düştü (yeni tablo yok, migration yok).
        using var db = f.Vt.Olustur(k.TenantId);
        var satirlar = await db.KiraciAyarlari
            .Where(x => x.Anahtar.StartsWith("is_")).Select(x => x.Anahtar).ToListAsync();
        Assert.Contains(IsAyarAnahtari.BaslangicSaat, satirlar);
        Assert.Contains(IsAyarAnahtari.VadeUfukGun, satirlar);
    }

    // ── 3) PATCH SÖZLEŞMESİ: gövdede yoksa KORU, null ise SİL ───────────────

    [Fact]
    public async Task Patch_govdede_OLMAYAN_alan_DEGISMEZ()
    {
        var k = YeniKiraci("P11-TEST-kismi");
        var c = f.Ortam.Istemci(k, "SAHIP");

        Assert.Equal(HttpStatusCode.OK,
            (await Patch(c, """{"baslangicSaat":8,"vadeUfukGun":30}""")).StatusCode);
        // Yalnız saat gönderilir; vade ufku KORUNMALI (tam gövdeyle veri imhası yok).
        Assert.Equal(HttpStatusCode.OK, (await Patch(c, """{"baslangicSaat":11}""")).StatusCode);

        var a = (await Govde(await c.GetAsync("/api/v1/isler"))).GetProperty("ayarlar");
        Assert.Equal(11, a.GetProperty("baslangicSaat").GetInt32());
        Assert.Equal(30, a.GetProperty("vadeUfukGun").GetInt32());
    }

    [Fact]
    public async Task Patch_null_alani_SILER_ve_VARSAYILANA_dondurur()
    {
        var k = YeniKiraci("P11-TEST-null");
        var c = f.Ortam.Istemci(k, "SAHIP");

        Assert.Equal(HttpStatusCode.OK, (await Patch(c, """{"vadeUfukGun":45}""")).StatusCode);
        var once = (await Govde(await c.GetAsync("/api/v1/isler"))).GetProperty("ayarlar");
        Assert.Equal(45, once.GetProperty("vadeUfukGun").GetInt32());
        var varsayilan = once.GetProperty("varsayilanVadeUfukGun").GetInt32();
        Assert.NotEqual(45, varsayilan);

        Assert.Equal(HttpStatusCode.OK, (await Patch(c, """{"vadeUfukGun":null}""")).StatusCode);
        var sonra = (await Govde(await c.GetAsync("/api/v1/isler"))).GetProperty("ayarlar");
        Assert.Equal(varsayilan, sonra.GetProperty("vadeUfukGun").GetInt32());

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.False(await db.KiraciAyarlari.AnyAsync(x => x.Anahtar == IsAyarAnahtari.VadeUfukGun));
    }

    [Fact]
    public async Task Patch_BOS_govde_400_doner_sessiz_etkisizlik_YOK()
    {
        var k = YeniKiraci("P11-TEST-bos");
        var cevap = await Patch(f.Ortam.Istemci(k, "SAHIP"), "{}");
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        // ⚠ JSON gövdesi AYRIŞTIRILARAK sınanır (Türkçe karakter \uXXXX kaçışlanır).
        Assert.Contains("Güncellenecek alan", Mesaj(await Govde(cevap)));
    }

    // ── 4) SINIRLAR SUNUCUDA zorlanır (ekran kapısı yeterli değil) ──────────

    [Theory]
    [InlineData("""{"baslangicSaat":24}""")]
    [InlineData("""{"baslangicSaat":-1}""")]
    [InlineData("""{"vadeUfukGun":0}""")]
    [InlineData("""{"vadeUfukGun":400}""")]
    [InlineData("""{"masakUyariGun":61}""")]
    public async Task Aralik_disi_deger_400_doner(string json)
    {
        var k = YeniKiraci("P11-TEST-aralik");
        var cevap = await Patch(f.Ortam.Istemci(k, "SAHIP"), json);
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    // ── 5) ROL KAPISI ───────────────────────────────────────────────────────

    [Fact]
    public async Task Tezgahtar_ayar_degistiremez()
    {
        var k = YeniKiraci("P11-TEST-rol");
        var cevap = await Patch(f.Ortam.Istemci(k, "TEZGAHTAR"), """{"baslangicSaat":9}""");
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    // ── 6) KİRACI AYARI YALNIZ DARALTIR ─────────────────────────────────────

    [Fact]
    public async Task Kiracinin_kapattigi_is_ELLE_de_calismaz()
    {
        var k = YeniKiraci("P11-TEST-kapat");
        var c = f.Ortam.Istemci(k, "SAHIP");

        // Önce açıkken koşabildiğini GÖSTER (yoksa test hiçbir şey kanıtlamaz).
        var acikken = await c.PostAsJsonAsync("/api/v1/isler/VADE_HATIRLATMA/calistir", new { });
        Assert.Equal(HttpStatusCode.OK, acikken.StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await Patch(c, """{"kapaliIsler":["VADE_HATIRLATMA"]}""")).StatusCode);

        var kapaliyken = await c.PostAsJsonAsync("/api/v1/isler/VADE_HATIRLATMA/calistir", new { });
        Assert.Equal(HttpStatusCode.BadRequest, kapaliyken.StatusCode);
        Assert.Contains("ayarlarında kapalı", Mesaj(await Govde(kapaliyken)));

        var i = (await Govde(await c.GetAsync("/api/v1/isler"))).GetProperty("isler")
            .EnumerateArray().First(x => x.GetProperty("kod").GetString() == "VADE_HATIRLATMA");
        Assert.True(i.GetProperty("kiraciKapatti").GetBoolean());
        Assert.True(i.GetProperty("yapilandirmaEtkin").GetBoolean());
        Assert.False(i.GetProperty("etkin").GetBoolean());

        // Geri dönüş yolu VAR (UX-S25): boş liste = hiçbiri kapalı değil.
        Assert.Equal(HttpStatusCode.OK, (await Patch(c, """{"kapaliIsler":[]}""")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await c.PostAsJsonAsync("/api/v1/isler/VADE_HATIRLATMA/calistir", new { })).StatusCode);
    }

    // ⚠ "Yapılandırmada kapalı iş kiracı ayarıyla AÇILAMAZ" kuralı burada
    //   ÖLÇÜLMEZ: test ortamında kayıtlı üç işin üçü de yapılandırmada açıktır,
    //   koşulsuz geçen bir HTTP testi hiçbir şey kanıtlamazdı (Anayasa md.1).
    //   Kural P11IsAyarTestleri.Yapilandirmada_KAPALI_is_kiraci_ayariyla_ACILAMAZ
    //   içinde yapılandırması KAPALI bir işle ölçülür.

    // ── 7) DENETİM İZİ ──────────────────────────────────────────────────────

    [Fact]
    public async Task Ayar_degisikligi_DENETIM_IZINE_duser()
    {
        var k = YeniKiraci("P11-TEST-denetim");
        Assert.Equal(HttpStatusCode.OK,
            (await Patch(f.Ortam.Istemci(k, "SAHIP"), """{"baslangicSaat":7}""")).StatusCode);

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.True(await db.DenetimKayitlari.AnyAsync(d => d.VarlikTuru == "IsAyari"));
    }
}
