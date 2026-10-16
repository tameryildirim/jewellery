using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4d — GERÇEK HTTP BORU HATTI
//   madde 7  /api/** altında bilinmeyen yol 404 JSON (HTML SIZDIRMAZ)
//   madde 8  400 gövdesi Türkçe + alan→mesaj sözlüğü
//   madde 3  MASAK belge türü DTO düzeyinde reddedilir, mesaj değerleri sayar
//   madde 6  kasa/banka yön sözlüğü: karşı değer sessizce EŞLENMEZ
//   madde 19 şube telefonu biçim doğrulaması
//   madde 20 pusula görüntüleme ≠ yazdırma (ayrı denetim olayı)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class UX4dHttpTestleri(HttpFikstur f)
{
    // ── madde 7 ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task M7_Api_onekinde_bilinmeyen_yol_404_JSON_doner_index_html_DEGIL()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d 404");
        var c = f.Ortam.Istemci(k);

        var yanit = await c.GetAsync("/api/v1/olmayan");

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        Assert.Contains("application/json", yanit.Content.Headers.ContentType!.ToString());

        var metin = await yanit.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<!DOCTYPE", metin, StringComparison.OrdinalIgnoreCase);

        var govde = JsonDocument.Parse(metin).RootElement;
        Assert.Equal("uç bulunamadı", govde.GetProperty("hata").GetString());
        Assert.Equal(404, govde.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task M7_Token_SUZ_bilinmeyen_api_yolu_da_HTML_sizdirmaz()
    {
        var yanit = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/gizli/olmayan");
        var metin = await yanit.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", metin, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task M7_Api_DISINDAKI_yollar_API_404_govdesi_almaz()
    {
        // ⚠ Test barındırıcısında Blazor yayın çıktısı bulunmadığı için /kur
        // 200 dönmez; sınanan şey budur DEĞİL: yeni geri dönüş kuralının
        // YALNIZ /api önekine dokunduğu, SPA yollarını ele geçirmediğidir.
        var yanit = await f.Ortam.AnonimIstemci().GetAsync("/kur");
        var metin = await yanit.Content.ReadAsStringAsync();
        Assert.DoesNotContain("uç bulunamadı", metin);
    }

    // ── madde 8 ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task M8_Gecersiz_eposta_400_TURKCE_mesaj_ve_alan_sozlugu_doner()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d dogrulama");
        var c = f.Ortam.Istemci(k);

        var yanit = await c.PostAsJsonAsync("/api/v1/cari",
            new { tur = "MUSTERI", ad = "Hatalı", email = "bu-eposta-degil" });

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
        var govde = await yanit.Content.ReadFromJsonAsync<JsonElement>();

        var mesajlar = govde.GetProperty("message").EnumerateArray()
            .Select(x => x.GetString()!).ToList();
        Assert.Contains(mesajlar, m => m.Contains("E-posta"));
        Assert.DoesNotContain(mesajlar, m => m.Contains("field is not a valid"));

        // Ekran alan altına yazabilsin diye alan→mesaj sözlüğü.
        var alanlar = govde.GetProperty("alanlar");
        Assert.True(alanlar.TryGetProperty("email", out var email));
        Assert.Contains("E-posta", email[0].GetString());
    }

    [Fact]
    public async Task M8_Negatif_kredi_limiti_ANLAMLI_Turkce_mesaj_verir_1_79E_308_DEGIL()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d kredi limiti");
        var c = f.Ortam.Istemci(k);

        var yanit = await c.PostAsJsonAsync("/api/v1/cari",
            new { tur = "MUSTERI", ad = "Limit", krediLimitiTl = -5 });

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
        var metin = await yanit.Content.ReadAsStringAsync();
        Assert.Contains("Kredi limiti 0 veya daha büyük olmalıdır", metin);
        Assert.DoesNotContain("E+308", metin);
        Assert.DoesNotContain("must be between", metin);
    }

    [Fact]
    public async Task M8_Kategori_SIRASI_negatif_olamaz_Turkce_mesaj()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d kategori sira");
        var c = f.Ortam.Istemci(k);

        var yanit = await c.PostAsJsonAsync("/api/v1/urun-kategorileri", new { ad = "Ziynet", sira = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
        Assert.Contains("Sıra 0 veya daha büyük olmalıdır", await yanit.Content.ReadAsStringAsync());
    }

    // ── madde 19 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task M19_Sube_telefonu_abc_ARTIK_400_doner()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d sube tel");
        var c = f.Ortam.Istemci(k);

        var yanit = await c.PostAsJsonAsync("/api/v1/subeler", new { ad = "Şube X", telefon = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
        Assert.Contains("Telefon", await yanit.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task M19_Gecerli_sube_telefonu_kabul_edilir_davranis_bozulmaz()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d sube tel ok");
        var c = f.Ortam.Istemci(k);

        var yanit = await c.PostAsJsonAsync("/api/v1/subeler",
            new { ad = "Şube Y", telefon = "+90 212 555 12 34" });

        Assert.True(yanit.IsSuccessStatusCode, await yanit.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task M19_Cari_telefonu_sube_ile_AYNI_kurala_tabidir()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d cari tel");
        var c = f.Ortam.Istemci(k);

        var yanit = await c.PostAsJsonAsync("/api/v1/cari",
            new { tur = "MUSTERI", ad = "Telefonsuz", telefon = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
    }

    // ── madde 18 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task M18_Belge_no_arama_ucu_LISTE_UCUYLA_AYNI_alan_kumesini_dondurur()
    {
        var sistem = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "UX4d arama sozlesmesi");
        var now = DateTime.UtcNow;
        sistem.MasakKimlikTespitleri.Add(new MasakKimlikTespiti
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, IslemTutariTl = 250_000m,
            BelgeTuru = "TC_KIMLIK", BelgeNo = "10000000146", BelgeNoHash = "",
            AdSoyad = "Ayşe Demir", TespitEdenKullaniciId = k.KullaniciId,
            Tarih = now, SaklamaBitis = now.AddYears(8), Olusturuldu = now,
        });
        await sistem.SaveChangesAsync();

        var yanit = await f.Ortam.Istemci(k)
            .GetAsync("/api/v1/masak/kimlik-tespitleri/ara?belgeNo=10000000146");

        Assert.True(yanit.IsSuccessStatusCode);
        var govde = await yanit.Content.ReadFromJsonAsync<JsonElement>();
        var kayit = govde.GetProperty("kayitlar")[0];

        // Ekran bu sonucu LİSTE satırı olarak çizebilmelidir (UX-S26).
        foreach (var alan in new[] { "id", "cariId", "islemTutariTl", "belgeTuru", "belgeNo",
                                     "adSoyad", "tarih", "saklamaBitis", "tespitEdenKullaniciId" })
            Assert.True(kayit.TryGetProperty(alan, out _), $"'{alan}' alanı eksik");

        // Listede belge no MASKELİ kalır (Mehmet md.2).
        Assert.StartsWith("*", kayit.GetProperty("belgeNo").GetString());
    }

    // ── madde 6 ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task M6_Kasaya_GELEN_gonderilince_400_doner_ve_dogru_degerler_listelenir()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d kasa yon");
        var c = f.Ortam.Istemci(k);

        var yanit = await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket",
            new { yon = "GELEN", tutar = 100, aciklama = "yanlış sözlük" });

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
        var metin = await yanit.Content.ReadAsStringAsync();
        Assert.Contains("GIRIS", metin);
        Assert.Contains("CIKIS", metin);
    }

    [Fact]
    public async Task M6_Kasaya_GELEN_gonderilince_SESSIZ_ESLEME_yapilmaz_hareket_DOGMAZ()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d kasa sessiz");
        var c = f.Ortam.Istemci(k);

        await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/hareket",
            new { yon = "GELEN", tutar = 100, aciklama = "x" });

        var sayi = await f.Vt.OlusturSistem().KasaHareketleri
            .CountAsync(x => x.TenantId == k.TenantId);
        Assert.Equal(0, sayi);
    }

    // ── madde 3 ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task M3_MASAK_belge_turu_DTO_duzeyinde_reddedilir_ve_degerleri_sayar()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "UX4d masak belge");
        var c = f.Ortam.Istemci(k);

        var yanit = await c.PostAsJsonAsync("/api/v1/kasalar/" + k.KasaId + "/hareket", new
        {
            yon = "GIRIS", tutar = 10, aciklama = "x",
        });
        // Bu uç MASAK taşımaz; asıl sınama satış DTO'sunda (aşağıdaki gövde).
        Assert.True(yanit.IsSuccessStatusCode || yanit.StatusCode == HttpStatusCode.BadRequest);

        var satis = await c.PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            odemeTuru = "NAKIT",
            kasaId = k.KasaId,
            kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1000m } },
            masak = new { belgeTuru = "EHLIYET", belgeNo = "1", adSoyad = "X" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, satis.StatusCode);
        var metin = await satis.Content.ReadAsStringAsync();
        Assert.Contains("TC_KIMLIK", metin);
        Assert.Contains("PASAPORT", metin);
    }

    // ── madde 20 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task M20_Pusula_belgesi_GORUNTULEME_ile_YAZDIRMA_ayri_denetim_olayidir()
    {
        var sistem = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "UX4d pusula");
        var now = DateTime.UtcNow;
        var pusulaId = Kimlik.Ulid();
        var hareketId = Kimlik.Ulid();

        sistem.Hareketler.Add(new Hareket
        {
            Id = hareketId, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "HURDA_ALIS",
            Tarih = now, TlTutari = -1000m, HasGrami = 1m, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        sistem.GiderPusulalari.Add(new GiderPusulasi
        {
            Id = pusulaId, TenantId = k.TenantId, SubeId = k.SubeId, HareketId = hareketId,
            PusulaNo = "GP-UX4D-1", SaticiTipi = "VATANDAS", SaticiAdSoyad = "Satıcı",
            SaticiTckn = "10000000146", SaticiTcknHash = "",
            AyarId = "K22", GramAgirlik = 1m, HasGrami = 0.916m,
            BrutTutar = 1000m, StopajOran = 0m, StopajTutar = 0m, NetOdenen = 1000m,
            TeslimTarihi = now, DuzenlemeSonTarihi = now.AddDays(7),
            Olusturuldu = now,
        });
        await sistem.SaveChangesAsync();

        var c = f.Ortam.Istemci(k);

        Assert.True((await c.GetAsync($"/api/v1/gider-pusulalari/{pusulaId}/belge")).IsSuccessStatusCode);
        var izlerGoruntuleme = await Izler(k.TenantId, pusulaId);
        Assert.Contains(izlerGoruntuleme, d => (d.Detay ?? "").Contains("görüntülendi"));
        Assert.DoesNotContain(izlerGoruntuleme, d => (d.Detay ?? "").Contains("YAZDIRILDI"));

        Assert.True((await c.PostAsJsonAsync($"/api/v1/gider-pusulalari/{pusulaId}/yazdirildi", new { }))
            .IsSuccessStatusCode);
        Assert.Contains(await Izler(k.TenantId, pusulaId), d => (d.Detay ?? "").Contains("YAZDIRILDI"));
    }

    private async Task<List<DenetimKaydi>> Izler(string tenantId, string varlikId) =>
        await f.Vt.OlusturSistem().DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.VarlikId == varlikId)
            .ToListAsync();
}
