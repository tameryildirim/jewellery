using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Kurulum;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KURULUŞ SİHİRBAZI — paket T-2 Faz A [HTTP]
//
// Servis testi göremeyenler: rol kapısı (403), modül yetkisi, model bağlama
// (400 biçimi), JSON sözleşmesi (camelCase), 409 hata ADI.
//
// ⚠ Tavan/asgari rakamları SABİTLENMEZ: /kurulum/durum ucunun döndürdüğü
//   gerekceAsgari okunur ve "asgari − 1 → 400" biçiminde sınanır (Aşkın koşulu).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KurulumHttpTestleri(HttpFikstur f)
{
    private const string Gerekce = "Eski programdan devreden acilis bakiyesi";

    private static object ProvaGovde(string tur, string hedefId, decimal tl = 1_000m) => new
    {
        tur,
        satirlar = new[] { new { hedefId, tlTutar = tl, hasGram = 0m } },
    };

    private static object UygulaGovde(
        string tur, string hedefId, string jeton, string kurId,
        decimal tl = 1_000m, string gerekce = Gerekce) => new
    {
        tur,
        satirlar = new[] { new { hedefId, tlTutar = tl, hasGram = 0m } },
        provaJetonu = jeton,
        gerekce,
        kurSnapshotId = kurId,
    };

    private static Kiraci Kiraci(HttpFikstur f, string ad) =>
        TestKurulum.KiraciKur(f.Vt.OlusturSistem(), $"PAKETT-2-TEST {ad}");

    private static async Task<string> Jeton(HttpClient c, string tur, string hedefId, decimal tl = 1_000m)
    {
        var y = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova", ProvaGovde(tur, hedefId, tl));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var j = await y.Json();
        return j.GetProperty("provaJetonu").GetString()!;
    }

    // ── Yetki kapıları ───────────────────────────────────────────────────────

    [Fact]
    public async Task Token_yoksa_401()
    {
        var c = f.Ortam.AnonimIstemci();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await c.GetAsync("/api/v1/kurulum/durum")).StatusCode);
    }

    [Fact]
    public async Task TEZGAHTAR_acilis_uygula_403()
    {
        var k = Kiraci(f, "tezgahtar");
        var c = f.Ortam.Istemci(k, "TEZGAHTAR");

        // Modül yetkisi kapısı: TEZGÂHTAR kurulum ekranını hiç göremez.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.GetAsync("/api/v1/kurulum/durum")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula",
                UygulaGovde(AcilisTuru.Kasa, k.KasaId, "x", k.KurId))).StatusCode);
    }

    [Fact]
    public async Task MUDUR_durumu_GORUR_ama_acilis_uygulayamaz_403()
    {
        var k = Kiraci(f, "mudur");
        var c = f.Ortam.Istemci(k, "MUDUR");

        // Canlı doğrulama listesindeki "MUDUR ile → 403" maddesinin testi.
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/v1/kurulum/durum")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova",
                ProvaGovde(AcilisTuru.Kasa, k.KasaId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.PostAsJsonAsync("/api/v1/kurulum/muhurle",
                new { gerekce = Gerekce, onay = true })).StatusCode);

        // ⚠ Her YAZMA ucu tek tek sınanır: sınıf düzeyindeki
        //   [RolGerekli("SAHIP","MUDUR")] MÜDÜR'ü geçirir; uçları kapatan
        //   METOT düzeyindeki [RolGerekli("SAHIP")]'tir. Yalnız prova/mühür
        //   sınansaydı, uygula ucundan o niteliğin silinmesi SESSİZ geçerdi.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula",
                UygulaGovde(AcilisTuru.Kasa, k.KasaId, "x", k.KurId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.PostAsJsonAsync("/api/v1/kurulum/acilis/geri-al",
                new { oturumId = "o1", gerekce = Gerekce, kurSnapshotId = k.KurId })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await c.PatchAsJsonAsync("/api/v1/kurulum/durum",
                new { atlananAdimlar = new[] { KurulumAdimlari.StokAcilis } })).StatusCode);
    }

    // ── Kuru prova ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Kuru_prova_HICBIR_satir_yazmaz_200_doner()
    {
        var k = Kiraci(f, "prova");
        var c = f.Ortam.Istemci(k);

        var y = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova",
            ProvaGovde(AcilisTuru.Kasa, k.KasaId, 4_000m));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var j = await y.Json();
        Assert.True(j.GetProperty("temiz").GetBoolean());
        Assert.Equal(1, j.GetProperty("uygulanacakSatir").GetInt32());
        Assert.Equal(0, j.GetProperty("hataliSatir").GetInt32());

        // Sunucu "hiçbir şey yazmadım" diyor — veritabanı da öyle diyor.
        using var db = f.Vt.OlusturSistem();
        Assert.Empty(db.AcilisKalemleri.AsNoTracking().Where(a => a.TenantId == k.TenantId));
        Assert.Equal(TestKurulum.KasaAcilis, TestKurulum.KasaBakiye(db, k.KasaId));
    }

    [Fact]
    public async Task Hatali_satirda_prova_JETON_VERMEZ_ve_hatayi_satir_no_ile_doner()
    {
        var k = Kiraci(f, "hatali");
        var c = f.Ortam.Istemci(k);

        var y = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova", new
        {
            tur = AcilisTuru.Kasa,
            satirlar = new[]
            {
                new { hedefId = k.KasaId, tlTutar = 0m, hasGram = 0m },      // SIFIR_SATIR
                new { hedefId = "olmayan-kasa-kimligi", tlTutar = 5m, hasGram = 0m },  // HEDEF_BULUNAMADI
            },
        });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var j = await y.Json();
        Assert.False(j.GetProperty("temiz").GetBoolean());
        Assert.Equal(JsonValueKind.Null, j.GetProperty("provaJetonu").ValueKind);
        Assert.Equal(2, j.GetProperty("hataliSatir").GetInt32());
        var kodlar = j.GetProperty("hatalar").EnumerateArray()
            .Select(h => h.GetProperty("hataKodu").GetString()).ToList();
        Assert.Contains(AcilisHataKodu.SifirSatir, kodlar);
        Assert.Contains(AcilisHataKodu.HedefBulunamadi, kodlar);
        // Satır numarası KULLANICININ gördüğü sıradır (1'den başlar).
        Assert.Equal([1, 2], j.GetProperty("hatalar").EnumerateArray()
            .Select(h => h.GetProperty("satirNo").GetInt32()).Order().ToArray());
    }

    [Fact]
    public async Task Bos_hedefli_satir_MODEL_BAGLAMADA_400()
    {
        // ⚠ DTO DataAnnotations YALNIZ denetleyici yolunda çalışır; servis
        //   içinde aynı kural AcilisKaynaklari.SafDogrula (HEDEF_YOK) ile
        //   TEKRAR uygulanır. Bu test HTTP yolundaki kapının varlığını ölçer.
        var k = Kiraci(f, "bos-hedef");
        var c = f.Ortam.Istemci(k);

        var y = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova", new
        {
            tur = AcilisTuru.Kasa,
            satirlar = new[] { new { hedefId = "", tlTutar = 5m, hasGram = 0m } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }

    // ── Uygula kapıları ──────────────────────────────────────────────────────

    [Fact]
    public async Task Prova_jetonu_olmadan_uygula_400()
    {
        var k = Kiraci(f, "jetonsuz");
        var c = f.Ortam.Istemci(k);

        var y = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula",
            UygulaGovde(AcilisTuru.Kasa, k.KasaId, "uydurma-jeton", k.KurId));
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Empty(db.AcilisKalemleri.AsNoTracking().Where(a => a.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Gerekce_asgarinin_bir_altinda_400()
    {
        var k = Kiraci(f, "gerekce");
        var c = f.Ortam.Istemci(k);

        // ⚠ Rakam SABİTLENMEZ: asgari sunucudan okunur (Aşkın koşulu).
        var asgari = (await (await c.GetAsync("/api/v1/kurulum/durum")).Json())
            .GetProperty("gerekceAsgari").GetInt32();
        var kisa = new string('a', asgari - 1);

        var jeton = await Jeton(c, AcilisTuru.Kasa, k.KasaId);
        var y = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula",
            UygulaGovde(AcilisTuru.Kasa, k.KasaId, jeton, k.KurId, gerekce: kisa));

        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }

    [Fact]
    public async Task Baska_kiracinin_carisine_acilis_404()
    {
        var a = Kiraci(f, "kiraci-a");
        var b = Kiraci(f, "kiraci-b");
        var c = f.Ortam.Istemci(a);

        // A'nın jetonu A'nın kendi carisi için alınır, gövdede B'nin carisi
        // gönderilse jeton tutmaz; bu yüzden B'nin carisi için prova denenir.
        var prova = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/prova",
            ProvaGovde(AcilisTuru.Cari, b.CariId));
        var pj = await prova.Json();
        Assert.False(pj.GetProperty("temiz").GetBoolean());
        Assert.Contains(AcilisHataKodu.HedefBulunamadi,
            pj.GetProperty("hatalar").EnumerateArray()
              .Select(h => h.GetProperty("hataKodu").GetString()));

        // Uygulama yolunda kiracı sınırı 404'tür (varlık sızdırma yok).
        var jeton = Jetonlu(a, AcilisTuru.Cari, b.CariId);
        var y = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula",
            UygulaGovde(AcilisTuru.Cari, b.CariId, jeton, a.KurId));
        Assert.Equal(HttpStatusCode.NotFound, y.StatusCode);
    }

    /// <summary>
    /// Sunucunun ürettiği jetonun AYNISINI üretir — "jeton doğruyken bile kiracı
    /// kapısı çalışıyor mu" sorusunu cevaplamak için. Üretim istemcisi bunu
    /// YAPAMAZ (HMAC anahtarı sunucudadır); test anahtarı bilir.
    /// </summary>
    private static string Jetonlu(Kiraci k, string tur, string hedefId, decimal tl = 1_000m)
    {
        var kanonik = AcilisKaynaklari.KanonikMetin(tur,
            [new AcilisSatirDto { HedefId = hedefId, TlTutar = tl, HasGram = 0m }]);
        return TestVeritabani.TestSifreleyici.Hashle(
            $"ACILIS|{k.TenantId}|{tur}|{AcilisKaynaklari.Ozet(kanonik)}");
    }

    // ── Mühür ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Muhurlu_kiracida_acilis_uygula_409()
    {
        var k = Kiraci(f, "muhur");
        var c = f.Ortam.Istemci(k);

        var jeton = await Jeton(c, AcilisTuru.Kasa, k.KasaId);

        // P-49: gövde işletme adını BİREBİR taşır (ikinci kapı); ad kiracıya
        // KiraciKur'da verilen addır — Kiraci() yardımcısıyla aynı biçim.
        var muhur = await c.PostAsJsonAsync("/api/v1/kurulum/muhurle",
            new
            {
                gerekce = "Kurulum tamamlandi, acilis kapatiliyor", onay = true,
                isletmeAdiOnayi = "PAKETT-2-TEST muhur",
            });
        Assert.Equal(HttpStatusCode.OK, muhur.StatusCode);
        Assert.True((await muhur.Json()).GetProperty("muhurlendi").GetBoolean());

        var y = await c.PostAsJsonAsync("/api/v1/kurulum/acilis/uygula",
            UygulaGovde(AcilisTuru.Kasa, k.KasaId, jeton, k.KurId));
        Assert.Equal(HttpStatusCode.Conflict, y.StatusCode);
        // 409 ADI T-1'in hazırlık 409'undan AYRIDIR (Bertan koşulu).
        Assert.Equal(KurulumHataAdi.Muhurlu, (await y.Json()).GetProperty("error").GetString());
    }

    [Fact]
    public async Task Muhur_ONAYSIZ_400()
    {
        var k = Kiraci(f, "muhur-onay");
        var c = f.Ortam.Istemci(k);

        // P-49: işletme adı DOĞRU verilir ki 400'ü YALNIZ onay bayrağı üretsin
        // (bayrak kapısı silinirse bu çağrı 200 döner → test kırmızı).
        var y = await c.PostAsJsonAsync("/api/v1/kurulum/muhurle",
            new
            {
                gerekce = "Kurulum tamamlandi, acilis kapatiliyor", onay = false,
                isletmeAdiOnayi = "PAKETT-2-TEST muhur-onay",
            });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        var d = await (await c.GetAsync("/api/v1/kurulum/durum")).Json();
        Assert.False(d.GetProperty("muhurlendi").GetBoolean());
    }

    // ── P-49 — İKİNCİ KAPI: işletme adı birebir yazılmadan mühür yok ────────

    [Fact]
    public async Task Muhur_isletme_adi_onayi_yanlissa_400()
    {
        const string ad = "PAKETT-2-TEST muhur-ad";
        var k = Kiraci(f, "muhur-ad");
        var c = f.Ortam.Istemci(k);

        // Ekranın yazdıracağı ad sunucudan gelir (UX-S21) — istemci uydurmaz.
        var durum = await (await c.GetAsync("/api/v1/kurulum/durum")).Json();
        Assert.Equal(ad, durum.GetProperty("isletmeAdi").GetString());

        // Ekran atlanıp uç doğrudan çağrılsa bile kapı ayakta: yok / boş /
        // harf büyüklüğü farklı / eksik / başka ad → 400 ve kiracı mühürsüz kalır.
        foreach (var yanlis in new object[]
        {
            new { gerekce = "Kurulum tamamlandi, acilis kapatiliyor", onay = true },
            new { gerekce = "Kurulum tamamlandi, acilis kapatiliyor", onay = true, isletmeAdiOnayi = "" },
            new { gerekce = "Kurulum tamamlandi, acilis kapatiliyor", onay = true, isletmeAdiOnayi = "   " },
            new { gerekce = "Kurulum tamamlandi, acilis kapatiliyor", onay = true, isletmeAdiOnayi = ad.ToLowerInvariant() },
            new { gerekce = "Kurulum tamamlandi, acilis kapatiliyor", onay = true, isletmeAdiOnayi = ad[..^1] },
            new { gerekce = "Kurulum tamamlandi, acilis kapatiliyor", onay = true, isletmeAdiOnayi = "Baska Kuyumcu" },
        })
        {
            var y = await c.PostAsJsonAsync("/api/v1/kurulum/muhurle", yanlis);
            Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
            Assert.Contains("işletme adını", (await y.Json()).GetProperty("message").GetString()!);
        }
        var sonra = await (await c.GetAsync("/api/v1/kurulum/durum")).Json();
        Assert.False(sonra.GetProperty("muhurlendi").GetBoolean());

        // Ad BİREBİR (baş/son boşluk hariç) yazılınca kapı açılır → kapıyı
        // ADIN kendisinin tuttuğu kanıtlanır.
        var dogru = await c.PostAsJsonAsync("/api/v1/kurulum/muhurle", new
        {
            gerekce = "Kurulum tamamlandi, acilis kapatiliyor", onay = true,
            isletmeAdiOnayi = "  " + ad + " ",
        });
        Assert.Equal(HttpStatusCode.OK, dogru.StatusCode);
        Assert.True((await dogru.Json()).GetProperty("muhurlendi").GetBoolean());
    }

    // ── Durum ucu ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Kurulum_durum_ucu_eksik_adimlari_listeler()
    {
        var k = Kiraci(f, "durum");
        var c = f.Ortam.Istemci(k);

        var j = await (await c.GetAsync("/api/v1/kurulum/durum")).Json();

        // KiraciKur şube+kullanıcı+kur+kasa kurar: zorunlu adım kalmaz.
        Assert.Empty(j.GetProperty("eksikAdimlar").EnumerateArray());
        Assert.True(j.GetProperty("muhurlenebilir").GetBoolean());
        Assert.False(j.GetProperty("muhurlendi").GetBoolean());
        Assert.Equal(6, j.GetProperty("adimlar").GetArrayLength());
        Assert.Equal(1, j.GetProperty("sayilar").GetProperty("kasa").GetInt32());
    }

    [Fact]
    public async Task Durum_PATCH_i_gonderilmeyen_alani_KORUR_null_ise_SILER()
    {
        var k = Kiraci(f, "patch");
        var c = f.Ortam.Istemci(k);

        var y1 = await c.PatchAsJsonAsync("/api/v1/kurulum/durum",
            new { atlananAdimlar = new[] { KurulumAdimlari.StokAcilis } });
        Assert.Equal(HttpStatusCode.OK, y1.StatusCode);
        Assert.Contains(KurulumAdimlari.StokAcilis,
            (await y1.Json()).GetProperty("atlananAdimlar").EnumerateArray().Select(x => x.GetString()));

        // Gövdede YOKSA korunur.
        var y2 = await c.PatchAsJsonAsync("/api/v1/kurulum/durum", new { });
        Assert.Contains(KurulumAdimlari.StokAcilis,
            (await y2.Json()).GetProperty("atlananAdimlar").EnumerateArray().Select(x => x.GetString()));

        // Boş dizi = SİL.
        var y3 = await c.PatchAsJsonAsync("/api/v1/kurulum/durum",
            new { atlananAdimlar = Array.Empty<string>() });
        Assert.Empty((await y3.Json()).GetProperty("atlananAdimlar").EnumerateArray());
    }

    [Fact]
    public async Task Bilinmeyen_adim_anahtari_400()
    {
        var k = Kiraci(f, "adim");
        var c = f.Ortam.Istemci(k);

        var y = await c.PatchAsJsonAsync("/api/v1/kurulum/durum",
            new { atlananAdimlar = new[] { "UYDURMA_ADIM" } });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }

    // ── P-68 — açılışın kabul ettiği para birimleri TEK KAYNAKTAN ───────────

    [Fact]
    public async Task Durum_acilis_para_birimlerini_AcilisServisi_ile_ayni_kaynaktan_doner()
    {
        var k = Kiraci(f, "parabirimi");
        var c = f.Ortam.Istemci(k, "SAHIP");

        var y = await c.GetAsync("/api/v1/kurulum/durum");
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var j = await y.Json();
        var liste = j.GetProperty("acilisParaBirimleri").EnumerateArray()
            .Select(x => x.GetString()!).ToList();
        Assert.NotEmpty(liste);
        Assert.Equal(AcilisServisi.KabulEdilenParaBirimleri, liste);
    }
}
