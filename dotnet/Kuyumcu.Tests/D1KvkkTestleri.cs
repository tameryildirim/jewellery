using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Contracts.Sayfalar.Kvkk;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-19 (18.10, rakip eşitleme) — KVKK aydınlatma ve rıza kaydı.
//   • Metin yokken aydınlatma 400 ve form 400 (metin uydurulmaz).
//   • Metin sürümlenir (aynı metin sürüm artırmaz); tezgâhtar metni değiştiremez (403).
//   • Beyan eklenir; izin geri alınınca durum "yok" olur, geçmişte iki satır kalır.
//   • Form işletmenin metnini ve sürümünü basar; Peşin Müşteri 400; başka işletme 404.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1KvkkTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Metin_surumlenir_beyan_eklenir_izin_geri_alinir_form_metni_basar()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-kvkk");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");

        Assert.Null((await s.GetFromJsonAsync<KvkkMetinDurumu>("/api/v1/kvkk/metin"))!.Surum);
        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/kvkk", new { tur = "AYDINLATMA", deger = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.GetAsync($"/api/v1/cikti/{BelgeTurleri.KvkkAydinlatma}/{k.CariId}")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await t.PutAsJsonAsync("/api/v1/kvkk/metin", new { metin = "x" })).StatusCode);
        var m1 = await (await s.PutAsJsonAsync("/api/v1/kvkk/metin", new { metin = "Veri sorumlusu: Test Kuyumcu.\n\nAmaç: satış ve fatura." })).Content.ReadFromJsonAsync<KvkkMetinDurumu>();
        Assert.Equal(1, m1!.Surum);
        var ayni = await (await s.PutAsJsonAsync("/api/v1/kvkk/metin", new { metin = "Veri sorumlusu: Test Kuyumcu.\n\nAmaç: satış ve fatura." })).Content.ReadFromJsonAsync<KvkkMetinDurumu>();
        Assert.Equal(1, ayni!.Surum);   // aynı metin sürüm artırmaz

        var d1 = await (await t.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/kvkk", new { tur = "AYDINLATMA", deger = true })).Content.ReadFromJsonAsync<KvkkCariDurumu>();
        Assert.Equal(1, d1!.AydinlatmaSurumu);
        Assert.NotNull(d1.AydinlatmaTarihi);
        var d2 = await (await t.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/kvkk", new { tur = "PAZARLAMA_IZNI", deger = true, kanal = "IMZALI_FORM" })).Content.ReadFromJsonAsync<KvkkCariDurumu>();
        Assert.True(d2!.PazarlamaIzni);
        var d3 = await (await t.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/kvkk", new { tur = "PAZARLAMA_IZNI", deger = false })).Content.ReadFromJsonAsync<KvkkCariDurumu>();
        Assert.False(d3!.PazarlamaIzni);
        Assert.Equal(3, d3.Gecmis.Count);   // geri alma yeni satır; geçmiş silinmez

        var m2 = await (await s.PutAsJsonAsync("/api/v1/kvkk/metin", new { metin = "Yeni metin." })).Content.ReadFromJsonAsync<KvkkMetinDurumu>();
        Assert.Equal(2, m2!.Surum);
        Assert.Equal(1, m2.AydinlatilanMusteri);
        Assert.Equal(0, m2.PazarlamaIzniOlan);
        var d4 = await t.GetFromJsonAsync<KvkkCariDurumu>($"/api/v1/cari/{k.CariId}/kvkk");
        Assert.Equal(1, d4!.AydinlatmaSurumu);
        Assert.Equal(2, d4.GuncelSurum);   // metin değişti → ekran "yeniden aydınlatın" der

        var b = await s.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.KvkkAydinlatma}/{k.CariId}");
        Assert.Contains("sürümü 2", b!.Dayanak);
        Assert.Equal("Yeni metin.", b.Bolumler[0].Satirlar.Single().Hucreler[0]);
        Assert.Equal(2, b.Bolumler[1].Satirlar.Count);

        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/kvkk", new { tur = "AYDINLATMA", deger = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/kvkk", new { tur = "BASKA", deger = true })).StatusCode);
    }

    [Fact]
    public async Task Pesin_musteri_400_baska_isletme_404()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-kvkk-k");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var pesin = (await (await s.GetAsync("/api/v1/cari/sistem-carisi")).Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.BadRequest, (await s.GetAsync($"/api/v1/cari/{pesin}/kvkk")).StatusCode);
        Kiraci baska;
        using (var db = f.Vt.OlusturSistem()) baska = TestKurulum.KiraciKur(db, "D1-kvkk-b");
        Assert.Equal(HttpStatusCode.NotFound, (await f.Ortam.Istemci(baska, "SAHIP").GetAsync($"/api/v1/cari/{k.CariId}/kvkk")).StatusCode);
    }
}
