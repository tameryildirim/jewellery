using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Gorunum;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-1 (dalga S-B) — KULLANICI GÖRÜNÜM TERCİHİ [saf + gerçek HTTP]
//
// Kurucu, 23 Eylül: "o rapor ne diyorsa onu yapacaksın, backend de ona göre
// şekillensin, eğer mevcut hali bozulmuyorsa."
// Tuval notu n4: "her kullanıcı kendi temasını seçer; SAHİP dükkân
// varsayılanını belirler."
//
// Bu dosyanın kilitlediği sözleşmeler:
//   • 8 şema · 3 punto · 3 tema modu — liste ve varsayılanlar (TEMA-SISTEMI)
//   • ESKİ DEĞER ÖLMEZ: altin/kurumsal/platin-mavi → platin, yogun→16,
//     okunakli→20, otomatik→sistem
//   • üç katman: kullanıcı satırı → dükkân varsayılanı → sistem (NULL = devral)
//   • PATCH sözleşmesi: gövdede yoksa KORU, null ise DEVRET (satırı temizle)
//   • YETKİ ≠ TERCİH: tezgâhtar KENDİ görünümünü yazar, dükkânınkini YAZAMAZ
//   • kullanıcılar ve kiracılar arası SIZINTI YOK
//   • denetim kaydı yazılır (DoD sözleşmesi)
//   • KIRMIZI ÇİZGİ: KÂĞIT DAİMA BEYAZ — yazdırma kaynakları temaya bağlanamaz
//
// ⚠ Test kiracısı adı "PAKETB-1-TEST-" önekiyle açılır ve iş sonunda silinir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Saf testler — veritabanı/HTTP gerekmez, liste ve göç haritası.</summary>
public sealed class B1GorunumSecenekleriTestleri
{
    // ── B1-1) TEK şema (tema temizliği 17.10), ÜÇ punto, ÜÇ tema modu ───────
    [Fact]
    public void B1_1_Listeler_1_3_3_ve_varsayilanlar_dogrudur()
    {
        // Kurucu 17.10: "tema sadece Yıldız, karanlık ve açık mod kalacak".
        Assert.Equal(["yildiz"], GorunumSecenekleri.Semalar);
        Assert.Equal(3, GorunumSecenekleri.Olcekler.Length);
        Assert.Equal(3, GorunumSecenekleri.TemaModlari.Length);
        Assert.Equal(["16", "18", "20"], GorunumSecenekleri.Olcekler);

        Assert.Equal("yildiz", GorunumSecenekleri.SemaVarsayilan);
        Assert.Equal("18", GorunumSecenekleri.OlcekVarsayilan);
        Assert.Equal("sistem", GorunumSecenekleri.TemaModuVarsayilan);

        // ⚠ ALTIN/AMBER YASAĞI (TEMA-SISTEMI § 2): "altin" artık seçilebilir
        //   bir şema DEĞİLDİR — yalnız göç edilen eski bir koddur.
        Assert.DoesNotContain("altin", GorunumSecenekleri.Semalar);
    }

    // ── B1-2) ESKİ DEĞER ÖLMEZ — göç haritası ───────────────────────────────
    [Fact]
    public void B1_2_Eski_kodlar_yildiza_gocer()
    {
        // Tema temizliği (17.10): kaldırılan 8 şema + 3 eski kod → "yildiz" (migration TemaYildizTek ile AYNI liste).
        foreach (var eski in new[] { "platin", "zumrut", "grafit", "bordo", "okyanus", "menekse", "zeytin", "fildisi",
                                     "altin", "kurumsal", "platin-mavi" })
            Assert.Equal("yildiz", GorunumSecenekleri.SemaNormalle(eski));
        Assert.Equal("yildiz", GorunumSecenekleri.SemaNormalle("yildiz"));
        Assert.Equal(11, GorunumSecenekleri.EskiSemalar.Count);

        Assert.Equal("16", GorunumSecenekleri.OlcekNormalle("yogun"));
        Assert.Equal("20", GorunumSecenekleri.OlcekNormalle("okunakli"));

        Assert.Equal("sistem", GorunumSecenekleri.TemaModuNormalle("otomatik"));
        Assert.Equal("karanlik", GorunumSecenekleri.TemaModuNormalle("karanlik"));
    }

    // ── B1-3) BİLİNMEYEN değer SESSİZCE varsayılana DÜŞMEZ, null döner ──────
    //       (Anayasa md. 1: bilinmeyen tahmin edilmez)
    [Fact]
    public void B1_3_Bilinmeyen_deger_null_doner()
    {
        Assert.Null(GorunumSecenekleri.SemaNormalle("pembe"));
        Assert.Null(GorunumSecenekleri.OlcekNormalle("14"));
        Assert.Null(GorunumSecenekleri.TemaModuNormalle("gece"));
        Assert.Null(GorunumSecenekleri.PuntoKodu(14));

        Assert.False(GorunumSecenekleri.SemaGecerli("pembe"));
        Assert.False(GorunumSecenekleri.OlcekGecerli("14"));
        Assert.False(GorunumSecenekleri.TemaModuGecerli("gece"));

        // Punto çevirisi: kod ↔ sayı tek yerde.
        Assert.Equal(20, GorunumSecenekleri.Punto("okunakli"));
        Assert.Equal("16", GorunumSecenekleri.PuntoKodu(16));
    }

    // ── B1-4) KIRMIZI ÇİZGİ: KÂĞIT DAİMA BEYAZ ──────────────────────────────
    //   Yazdırma kaynakları (Çıktı ekranı, A4/pusula belgesi, ürün etiketi,
    //   yazdır düğmesi) şema/tema/punto özniteliklerine ve görünüm
    //   servislerine BAŞVURAMAZ. Bir gün biri belgeyi temaya bağlarsa bu test
    //   kırmızı döner (mutasyon M-B1-4 ile kanıtlandı).
    [Fact]
    public void B1_4_Kagit_daima_beyaz_yazdirma_kaynaklari_temaya_baglanmaz()
    {
        string[] kagit =
        [
            Path.Combine("Kuyumcu.Web", "Pages", "Cikti.razor"),
            Path.Combine("Kuyumcu.Web", "Pages", "PusulaBelgesi.razor"),
            Path.Combine("Kuyumcu.Web", "Pages", "UrunEtiketleri.razor"),
            Path.Combine("Kuyumcu.Web", "Bilesenler", "Yazdirma", "A4Belge.razor"),
            Path.Combine("Kuyumcu.Web", "Bilesenler", "Yazdirma", "YazdirDugmesi.razor"),
        ];

        string[] yasak =
        [
            "data-sema", "data-yazi", "data-tema",
            "GorunumServisi", "KullaniciGorunumServisi", "GorunumSecenekleri",
        ];

        foreach (var dosya in kagit)
        {
            var metin = CssKaynak.Metin(dosya.Split(Path.DirectorySeparatorChar));
            foreach (var kelime in yasak)
                Assert.False(
                    metin.Contains(kelime, StringComparison.Ordinal),
                    $"KÂĞIT DAİMA BEYAZ ihlali: {dosya} içinde '{kelime}' geçiyor. " +
                    "Yazdırma çıktısı ekran temasından ETKİLENMEZ (B-1 kırmızı çizgisi).");
        }
    }
}

// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Gerçek HTTP boru hattı — [RolGerekli]/[HerkeseAcik] ve JSON sözleşmesi.</summary>
[Collection("http")]
public sealed class B1KullaniciGorunumHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Aynı kiracıda İKİNCİ bir kullanıcı — sızıntı testi için.</summary>
    private string IkinciKullanici(Kiraci k, string rol = "TEZGAHTAR")
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Kullanicilar.Add(new Kullanici
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "İkinci", Soyad = "Kişi",
            Email = $"{id}@test.local", SifreHash = "x", Rol = rol, Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private static async Task<System.Text.Json.JsonElement> Oku(HttpResponseMessage c)
    {
        var j = (await c.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())!;
        return j.TryGetProperty("gorunum", out var ic) ? ic : j;
    }

    // ── B1H-1) Jetonsuz istek 401 ───────────────────────────────────────────
    [Fact]
    public async Task B1H1_Jetonsuz_istek_401_doner()
    {
        var c = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/kullanici/gorunum");
        Assert.Equal(HttpStatusCode.Unauthorized, c.StatusCode);
    }

    // ── B1H-2) Hiçbir tercih yokken SİSTEM varsayılanı + listeler döner ─────
    [Fact]
    public async Task B1H2_Tercih_yokken_sistem_varsayilani_ve_listeler_doner()
    {
        var k = YeniKiraci("PAKETB-1-TEST-varsayilan");
        var c = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/kullanici/gorunum");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        var g = await Oku(c);
        Assert.Equal(GorunumSecenekleri.SemaVarsayilan, g.GetProperty("sema").GetString());
        Assert.Equal(GorunumSecenekleri.TemaModuVarsayilan, g.GetProperty("temaModu").GetString());
        Assert.Equal(18, g.GetProperty("yaziBoyutu").GetInt32());

        // Kaynak rozetleri (UX-S21): hiçbir katman yoksa "sistem".
        Assert.Equal("sistem", g.GetProperty("semaKaynagi").GetString());
        Assert.Equal("sistem", g.GetProperty("temaModuKaynagi").GetString());
        Assert.Equal("sistem", g.GetProperty("yaziBoyutuKaynagi").GetString());

        // Kendi satırı BOŞ: migration hiçbir kullanıcıyı doldurmadı.
        Assert.Equal(System.Text.Json.JsonValueKind.Null, g.GetProperty("ozelSema").ValueKind);

        // Seçenek listeleri SUNUCUDAN gelir — istemci kendi listesini uydurmaz.
        Assert.Equal(1, g.GetProperty("semalar").GetArrayLength());   // tema temizliği 17.10: yalnız "yildiz"
        Assert.Equal(3, g.GetProperty("temaModlari").GetArrayLength());
        Assert.Equal(3, g.GetProperty("yaziBoyutlari").GetArrayLength());
    }

    // ── B1H-3) YETKİ ≠ TERCİH: tezgâhtar KENDİ görünümünü YAZAR (200),
    //           dükkân varsayılanını YAZAMAZ (403). Tuval notu n4.
    [Fact]
    public async Task B1H3_Tezgahtar_kendi_gorunumunu_yazar_dukkaninkini_yazamaz()
    {
        var k = YeniKiraci("PAKETB-1-TEST-rol");
        var i = f.Ortam.Istemci(k, "TEZGAHTAR");

        var kendi = await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { sema = "bordo" });
        Assert.Equal(HttpStatusCode.OK, kendi.StatusCode);

        var dukkan = await i.PatchAsJsonAsync("/api/v1/gorunum", new { sema = "bordo" });
        Assert.Equal(HttpStatusCode.Forbidden, dukkan.StatusCode);
    }

    // ── B1H-4) ÜÇ KATMAN: kullanıcı satırı dükkân varsayılanını EZER ────────
    //   Tema temizliği (17.10): şema tek olduğu için katman tema modunda sınanır (aynı çözüm yolu).
    [Fact]
    public async Task B1H4_Kullanici_satiri_dukkan_varsayilanini_ezer()
    {
        var k = YeniKiraci("PAKETB-1-TEST-katman");
        var sahip = f.Ortam.Istemci(k, "SAHIP");

        await sahip.PatchAsJsonAsync("/api/v1/gorunum", new { temaModu = "karanlik" });

        // Dükkân "karanlik" derken kullanıcı kendi satırında "aydinlik" seçiyor.
        var c = await sahip.PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { temaModu = "aydinlik" });
        var g = await Oku(c);

        Assert.Equal("aydinlik", g.GetProperty("temaModu").GetString());
        Assert.Equal("kullanici", g.GetProperty("temaModuKaynagi").GetString());
        Assert.Equal("karanlik", g.GetProperty("kiraciTemaModu").GetString());   // dükkân DEĞİŞMEDİ

        using var db = f.Vt.Olustur(k.TenantId);
        var satir = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == k.KullaniciId);
        Assert.Equal("aydinlik", satir.TemaModu);
    }

    // ── B1H-5) PATCH SÖZLEŞMESİ: gövdede olmayan alan KORUNUR ───────────────
    [Fact]
    public async Task B1H5_Gonderilmeyen_alan_korunur()
    {
        var k = YeniKiraci("PAKETB-1-TEST-patch");
        var i = f.Ortam.Istemci(k, "SAHIP");

        await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { temaModu = "karanlik" });
        var c = await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { yaziBoyutu = 20 });

        var g = await Oku(c);
        Assert.Equal("karanlik", g.GetProperty("temaModu").GetString());   // ⚠ EZİLMEDİ
        Assert.Equal(20, g.GetProperty("yaziBoyutu").GetInt32());
    }

    // ── B1H-6) null = DÜKKÂN VARSAYILANINA DEVRET (satır temizlenir) ────────
    //   "Oluşturulan her kayıt düzeltilebilir" (UX-S25): bu, silme yoludur.
    [Fact]
    public async Task B1H6_Null_gonderilince_dukkan_varsayilanina_devredilir()
    {
        var k = YeniKiraci("PAKETB-1-TEST-devret");
        var i = f.Ortam.Istemci(k, "SAHIP");

        await i.PatchAsJsonAsync("/api/v1/gorunum", new { temaModu = "aydinlik" });
        await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { temaModu = "karanlik" });

        var c = await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { temaModu = (string?)null });
        var g = await Oku(c);

        Assert.Equal("aydinlik", g.GetProperty("temaModu").GetString());          // dükkâna DÜŞTÜ
        Assert.Equal("kiraci", g.GetProperty("temaModuKaynagi").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, g.GetProperty("ozelTemaModu").ValueKind);

        using var db = f.Vt.Olustur(k.TenantId);
        var satir = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == k.KullaniciId);
        Assert.Null(satir.TemaModu);
    }

    // ── B1H-7) BEYAZ LİSTE: liste dışı değer 400 — sessizce düşürülmez ──────
    [Theory]
    [InlineData("sema", "pembe")]
    [InlineData("temaModu", "gece")]
    public async Task B1H7_Beyaz_liste_disi_deger_400_doner(string alan, string deger)
    {
        var k = YeniKiraci($"PAKETB-1-TEST-400-{alan}");
        var i = f.Ortam.Istemci(k, "SAHIP");

        var govde = new Dictionary<string, object?> { [alan] = deger };
        var c = await i.PatchAsJsonAsync("/api/v1/kullanici/gorunum", govde);
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
    }

    // ── B1H-8) Beyaz liste dışı PUNTO 400 (14 px yok) ───────────────────────
    [Fact]
    public async Task B1H8_Gecersiz_punto_400_doner()
    {
        var k = YeniKiraci("PAKETB-1-TEST-punto");
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { yaziBoyutu = 14 });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
    }

    // ── B1H-9) Boş gövde 400 — "hiçbir şey olmayan" istek sessiz geçmez ─────
    [Fact]
    public async Task B1H9_Bos_govde_400_doner()
    {
        var k = YeniKiraci("PAKETB-1-TEST-bos");
        var c = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
    }

    // ── B1H-10) DENETİM KAYDI yazılır (DoD sözleşmesi) ──────────────────────
    [Fact]
    public async Task B1H10_Denetim_kaydi_yazilir()
    {
        var k = YeniKiraci("PAKETB-1-TEST-denetim");
        await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { temaModu = "karanlik" });

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.True(await db.DenetimKayitlari.AsNoTracking()
            .AnyAsync(d => d.VarlikTuru == KullaniciGorunumServisi.Varlik
                        && d.VarlikId == k.KullaniciId));
    }

    // ── B1H-11) KULLANICI SIZINTISI YOK: A'nın tercihi B'yi etkilemez ───────
    [Fact]
    public async Task B1H11_Kullanicilar_arasi_sizinti_yok()
    {
        var k = YeniKiraci("PAKETB-1-TEST-sizinti");
        var ikinciId = IkinciKullanici(k);

        await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { sema = "fildisi" });

        var c = await f.Ortam.Istemci(ikinciId, k.TenantId, k.SubeId, "TEZGAHTAR")
            .GetAsync("/api/v1/kullanici/gorunum");
        var g = await Oku(c);

        Assert.Equal(GorunumSecenekleri.SemaVarsayilan, g.GetProperty("sema").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, g.GetProperty("ozelSema").ValueKind);
    }

    // ── B1H-12) KİRACI SIZINTISI YOK ────────────────────────────────────────
    [Fact]
    public async Task B1H12_Kiraci_sizintisi_yok()
    {
        var a = YeniKiraci("PAKETB-1-TEST-kiraci-a");
        var b = YeniKiraci("PAKETB-1-TEST-kiraci-b");

        await f.Ortam.Istemci(a, "SAHIP")
            .PatchAsJsonAsync("/api/v1/kullanici/gorunum", new { sema = "zeytin" });

        var c = await f.Ortam.Istemci(b, "SAHIP").GetAsync("/api/v1/kullanici/gorunum");
        var g = await Oku(c);
        Assert.Equal(GorunumSecenekleri.SemaVarsayilan, g.GetProperty("sema").GetString());
    }

    // ── B1H-13) ESKİ KOD ÖLMEZ: takma ad kabul edilir, KANONİK yazılır ──────
    [Fact]
    public async Task B1H13_Eski_kodlar_kabul_edilir_ve_kanonige_gocer()
    {
        var k = YeniKiraci("PAKETB-1-TEST-takma");
        var c = await f.Ortam.Istemci(k, "SAHIP").PatchAsJsonAsync(
            "/api/v1/kullanici/gorunum", new { sema = "platin-mavi", temaModu = "otomatik" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        var g = await Oku(c);
        Assert.Equal(GorunumSecenekleri.SemaVarsayilan, g.GetProperty("sema").GetString());
        Assert.Equal(GorunumSecenekleri.TemaModuVarsayilan, g.GetProperty("temaModu").GetString());

        using var db = f.Vt.Olustur(k.TenantId);
        var satir = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == k.KullaniciId);
        Assert.Equal("yildiz", satir.Sema);          // ⚠ depoda ESKİ AD YAŞAMAZ (tema temizliği 17.10: tek şema)
        Assert.Equal("sistem", satir.TemaModu);
    }
}
