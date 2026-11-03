using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Kullanici;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// 02.11 — KURUCU KARARI: "AYNI E-POSTA İLE KAYIT YAPILAMASIN"
//
// Bir e-posta aynı anda YALNIZ BİR işletmede aktif olabilir. Kapılar:
//   (1) kullanıcı ekleme (POST /kullanicilar)            → 409, alan=email
//   (2) e-posta değiştirme (PATCH /kullanicilar/{id})    → 409
//   (3) pasiften aktife alma (PATCH {id}/aktif, PATCH {id} aktif:true) → 409
//   (4) işletme kaydı (/auth/kayit) — AuthTestleri.Ayni_eposta_ikinci_kez_kayit_edilemez
// Pasif kayıt ENGEL DEĞİLDİR (ayrılan çalışan başka işletmede çalışabilir).
// Kendi işletmesi içindeki tekillik ayrı mesajla korunur ("zaten kayıtlı").
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KullaniciEpostaTekilligiTestleri(HttpFikstur f)
{
    private const string Sifre = "Parola123";

    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Kiracıda verilen e-postayla (aktif/pasif) çalışan açar, kimliğini döner.</summary>
    private async Task<string> CalisanAc(Kiraci k, string eposta, bool aktif = true)
    {
        var c = f.Ortam.Istemci(k);
        var r = await c.PostJson("/api/v1/kullanicilar", new { ad = "Deneme", soyad = "Çalışan", email = eposta, sifre = Sifre, rol = "TEZGAHTAR" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var id = (await r.Json()).GetProperty("id").GetString()!;
        if (!aktif)
            Assert.Equal(HttpStatusCode.OK, (await Patch(c, $"/api/v1/kullanicilar/{id}/aktif", new { aktif = false })).StatusCode);
        return id;
    }

    private static Task<HttpResponseMessage> Patch(HttpClient c, string yol, object govde) =>
        c.SendAsync(new HttpRequestMessage(HttpMethod.Patch, yol) { Content = JsonContent.Create(govde) });

    private static async Task MesajDogrula(HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        var json = await r.Json();
        Assert.Equal(KullaniciServisi.EpostaBaskaIsletmedeMesaji, json.GetProperty("message").GetString());
        Assert.DoesNotContain("tenant", json.GetRawText(), StringComparison.OrdinalIgnoreCase);   // jargon yok
    }

    [Fact]
    public async Task ET01_Baska_isletmede_AKTIF_eposta_ile_kullanici_ACILAMAZ_409_alan_email()
    {
        var a = Kur("Tekillik A"); var b = Kur("Tekillik B");
        var eposta = $"tekil-{Kimlik.Ulid()}@test.local";
        await CalisanAc(a, eposta);

        var r = await f.Ortam.Istemci(b).PostJson("/api/v1/kullanicilar",
            new { ad = "İkinci", soyad = "Kayıt", email = eposta, sifre = Sifre, rol = "TEZGAHTAR" });

        await MesajDogrula(r);
        using var db = f.Vt.OlusturSistem();
        Assert.Single(db.Kullanicilar.Where(k => k.Email == eposta));   // B'de satır oluşmadı
    }

    [Fact]
    public async Task ET02_Baska_isletmede_PASIF_eposta_engel_degil_201()
    {
        var a = Kur("Pasif A"); var b = Kur("Pasif B");
        var eposta = $"pasif-{Kimlik.Ulid()}@test.local";
        await CalisanAc(a, eposta, aktif: false);

        var r = await f.Ortam.Istemci(b).PostJson("/api/v1/kullanicilar",
            new { ad = "Yeni", soyad = "İşletme", email = eposta, sifre = Sifre, rol = "TEZGAHTAR" });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task ET03_Pasif_kaydi_aktife_alma_baska_isletmede_aktifse_409_her_iki_ucta()
    {
        var a = Kur("Aktife A"); var b = Kur("Aktife B");
        var eposta = $"aktife-{Kimlik.Ulid()}@test.local";
        var aId = await CalisanAc(a, eposta, aktif: false);
        await CalisanAc(b, eposta);                                   // B'de aktif
        var c = f.Ortam.Istemci(a);

        await MesajDogrula(await Patch(c, $"/api/v1/kullanicilar/{aId}/aktif", new { aktif = true }));
        await MesajDogrula(await Patch(c, $"/api/v1/kullanicilar/{aId}", new { aktif = true }));

        using var db = f.Vt.OlusturSistem();
        Assert.False(db.Kullanicilar.Single(k => k.Id == aId).Aktif);   // pasif kaldı
    }

    [Fact]
    public async Task ET04_Eposta_degistirme_baska_isletmede_aktif_adrese_409_eski_adres_kalir()
    {
        var a = Kur("Değiştir A"); var b = Kur("Değiştir B");
        var bEposta = $"b-{Kimlik.Ulid()}@test.local";
        await CalisanAc(b, bEposta);
        var aEposta = $"a-{Kimlik.Ulid()}@test.local";
        var aId = await CalisanAc(a, aEposta);

        await MesajDogrula(await Patch(f.Ortam.Istemci(a), $"/api/v1/kullanicilar/{aId}", new { email = bEposta }));

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(aEposta, db.Kullanicilar.Single(k => k.Id == aId).Email);
    }

    [Fact]
    public async Task ET05_Kendi_isletmesinde_ayni_eposta_eski_mesajla_409()
    {
        var a = Kur("Kendi A");
        var eposta = $"kendi-{Kimlik.Ulid()}@test.local";
        await CalisanAc(a, eposta);

        var r = await f.Ortam.Istemci(a).PostJson("/api/v1/kullanicilar",
            new { ad = "Tekrar", soyad = "Aynı", email = eposta, sifre = Sifre, rol = "TEZGAHTAR" });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("Bu e-posta adresi zaten kayıtlı", (await r.Json()).GetProperty("message").GetString());
    }
}
