using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yetki;
using Kuyumcu.Contracts.Arama;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-15 — KABUK ARAMA UCU, GERÇEK HTTP BORU HATTI
// (kimlikler Y-29 · Y-5 · B-25 · B-29)
//
// PLAN § 2 P-15 RİSKİ: "arama ucu şube filtresiyle çakışırsa çapraz şube
// sonucu döner; NEGATİF TEST ZORUNLU." Bu dosyanın çekirdeği o testtir:
//   • şubeye kilitli kullanıcı BAŞKA şubeyi isterse 403 (sessiz düşürme YOK),
//   • şubeye kilitli kullanıcı komşu şubenin fişini HİÇ göremez (P-04 filtresi).
//
// Servis testi [Authorize] / [RolGerekli] / yetki matrisini GÖRMEZ; bu yüzden
// yetki kapıları burada, üretimdeki boru hattında ölçülür.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class AramaHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Adı bilinen bir cari ekler (arama sorgusu bu ada vurur).</summary>
    private string CariEkle(Kiraci k, string ad, string? telefon = null, bool aktif = true)
    {
        using var db = f.Vt.Olustur(k.TenantId);
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari
        {
            Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = ad,
            Telefon = telefon, Aktif = aktif, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    /// <summary>
    /// Belirli bir şubede bir FİŞ satırı yazar. Hareket ISubeVarlik'tir;
    /// çapraz şube negatif testinin kaynağı budur.
    /// </summary>
    private string FisEkle(Kiraci k, string subeId, string cariId, string fisId)
    {
        using var db = f.Vt.Olustur(k.TenantId);
        db.Hareketler.Add(new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = subeId,
            Tur = "SATIS", Tarih = DateTime.UtcNow, Aciklama = "arama testi",
            TlTutari = 1000m, HasGrami = 0m, ParaBirimi = "TRY",
            CariId = cariId, FisId = fisId, KullaniciId = k.KullaniciId, KurSnapshotId = k.KurId,
            Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
        return fisId;
    }

    private static async Task<AramaYaniti> Oku(HttpResponseMessage cevap)
    {
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Content.ReadFromJsonAsync<AramaYaniti>(
            new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web)))!;
    }

    // ── Kimlik kapısı ────────────────────────────────────────────────────────

    [Fact]
    public async Task Jetonsuz_arama_401_doner()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/arama?q=ay");
        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    // ── Y-5: sunucuda arar ve sayfalar ───────────────────────────────────────

    [Fact]
    public async Task Cari_adiyla_aranir_ve_kimlik_ile_birlikte_AD_doner()
    {
        var k = YeniKiraci("P-15 arama");
        var id = CariEkle(k, "Zeynep Arama Testi", "05551112233");

        var yanit = await Oku(await f.Ortam.Istemci(k)
            .GetAsync("/api/v1/arama?q=Zeynep%20Arama&tur=CARI"));

        var sonuc = Assert.Single(yanit.Sonuclar);
        Assert.Equal(id, sonuc.Kimlik);
        Assert.Equal("Zeynep Arama Testi", sonuc.Ad);
        Assert.Equal(AramaTurleri.Cari, sonuc.Tur);
        // B-25: kullanıcıya gösterilen metinde ULID YOKTUR.
        Assert.DoesNotContain(id, sonuc.Ad, StringComparison.Ordinal);
        Assert.DoesNotContain(id, sonuc.AltBilgi ?? "", StringComparison.Ordinal);
        // B-29: derin bağlantı sorgu dizesi taşır.
        Assert.Equal($"/cariler?cariId={id}", sonuc.Rota);
    }

    [Fact]
    public async Task Kisa_sorgu_HATA_degil_BOS_yanittir_ve_tavani_bildirir()
    {
        var k = YeniKiraci("P-15 kısa");
        var yanit = await Oku(await f.Ortam.Istemci(k).GetAsync("/api/v1/arama?q=a"));

        Assert.Empty(yanit.Sonuclar);
        Assert.False(yanit.DahaVar);
        // UX-S29: ekranın yazacağı sunucu sınırı yanıtta GELİR, ekran uydurmaz.
        Assert.Equal(Kuyumcu.Api.Modules.Arama.AramaServisi.Tavan, yanit.Tavan);
    }

    [Fact]
    public async Task Sinira_deginde_dahaVar_TRUE_doner_sessiz_kirpma_yok()
    {
        var k = YeniKiraci("P-15 sınır");
        for (var i = 0; i < 4; i++) CariEkle(k, $"Sinir Testi {i}");

        var yanit = await Oku(await f.Ortam.Istemci(k)
            .GetAsync("/api/v1/arama?q=Sinir%20Testi&tur=CARI&limit=2"));

        Assert.Equal(2, yanit.Sonuclar.Count);
        Assert.True(yanit.DahaVar);
    }

    [Fact]
    public async Task Ofset_sonraki_sayfayi_getirir()
    {
        var k = YeniKiraci("P-15 ofset");
        for (var i = 0; i < 3; i++) CariEkle(k, $"Ofset Testi {i}");

        var ilk = await Oku(await f.Ortam.Istemci(k)
            .GetAsync("/api/v1/arama?q=Ofset%20Testi&tur=CARI&limit=1&ofset=0"));
        var ikinci = await Oku(await f.Ortam.Istemci(k)
            .GetAsync("/api/v1/arama?q=Ofset%20Testi&tur=CARI&limit=1&ofset=1"));

        Assert.NotEqual(ilk.Sonuclar[0].Kimlik, ikinci.Sonuclar[0].Kimlik);
    }

    [Fact]
    public async Task Bilinmeyen_tur_400_doner_sessizce_GENELE_dusmez()
    {
        var k = YeniKiraci("P-15 tür");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/arama?q=ayse&tur=UYDURMA");
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    // ── Kiracı izolasyonu ────────────────────────────────────────────────────

    [Fact]
    public async Task Baska_kiracinin_carisi_aramada_GORUNMEZ()
    {
        var a = YeniKiraci("P-15 kiracı A");
        var b = YeniKiraci("P-15 kiracı B");
        CariEkle(a, "Capraz Kiraci Musterisi");

        var yanit = await Oku(await f.Ortam.Istemci(b)
            .GetAsync("/api/v1/arama?q=Capraz%20Kiraci&tur=CARI"));

        Assert.Empty(yanit.Sonuclar);
    }

    // ── ŞUBE: PLAN'ın zorunlu kıldığı NEGATİF testler ────────────────────────

    /// <summary>
    /// PLAN P-15 RİSKİ (birebir): arama ucu şube filtresiyle çakışırsa çapraz
    /// şube sonucu döner. Şubeye kilitli rol (TEZGAHTAR) komşu şubenin fişini
    /// GÖREMEZ — ne sessizce, ne kısmen.
    /// </summary>
    [Fact]
    public async Task Sube_kilitli_rol_komsu_subenin_fisini_GORMEZ()
    {
        var k = YeniKiraci("P-15 şube");
        string komsuSube;
        using (var db = f.Vt.Olustur(k.TenantId))
            komsuSube = TestKurulum.SubeEkle(db, k.TenantId, "P-15 Komşu");

        FisEkle(k, k.SubeId, k.CariId, "FIS-KENDI-SUBE");
        FisEkle(k, komsuSube, k.CariId, "FIS-KOMSU-SUBE");

        var tezgahtar = f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR");
        var yanit = await Oku(await tezgahtar.GetAsync("/api/v1/arama?q=FIS-&tur=FIS"));

        Assert.Contains(yanit.Sonuclar, s => s.Kimlik == "FIS-KENDI-SUBE");
        Assert.DoesNotContain(yanit.Sonuclar, s => s.Kimlik == "FIS-KOMSU-SUBE");
    }

    /// <summary>
    /// Şubeye kilitli rol AÇIKÇA başka şubeyi isterse yanıt 403'tür.
    /// Sessizce kendi şubesine düşürülmek (fail-blind) YASAKTIR: kullanıcı
    /// "komşu şubede kayıt yok" sanırdı (Anayasa md.1).
    /// </summary>
    [Fact]
    public async Task Sube_kilitli_rol_BASKA_sube_isterse_403_alir()
    {
        var k = YeniKiraci("P-15 şube 403");
        string komsuSube;
        using (var db = f.Vt.Olustur(k.TenantId))
            komsuSube = TestKurulum.SubeEkle(db, k.TenantId, "P-15 Komşu 403");

        var tezgahtar = f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR");
        var cevap = await tezgahtar.GetAsync($"/api/v1/arama?q=FIS-&tur=FIS&sube={komsuSube}");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    /// <summary>
    /// TÜM ŞUBELER ayrıcalığı olan rol (SAHIP) şube seçiciyle DARALTABİLİR:
    /// seçtiği şubenin dışındaki fiş sonuçta yer almaz.
    /// </summary>
    [Fact]
    public async Task Tum_subeler_rolu_sube_secici_ile_DARALTIR()
    {
        var k = YeniKiraci("P-15 daraltma");
        string komsuSube;
        using (var db = f.Vt.Olustur(k.TenantId))
            komsuSube = TestKurulum.SubeEkle(db, k.TenantId, "P-15 Daraltma Komşu");

        FisEkle(k, k.SubeId, k.CariId, "FIS-DAR-KENDI");
        FisEkle(k, komsuSube, k.CariId, "FIS-DAR-KOMSU");

        var sahip = f.Ortam.Istemci(k, "SAHIP");

        var hepsi = await Oku(await sahip.GetAsync("/api/v1/arama?q=FIS-DAR&tur=FIS"));
        Assert.Equal(2, hepsi.Sonuclar.Count);

        var dar = await Oku(await sahip.GetAsync($"/api/v1/arama?q=FIS-DAR&tur=FIS&sube={komsuSube}"));
        Assert.Single(dar.Sonuclar);
        Assert.Equal("FIS-DAR-KOMSU", dar.Sonuclar[0].Kimlik);
    }

    // ── Modül yetkisi (tür başına) ───────────────────────────────────────────

    /// <summary>
    /// Tek tür istenip yetki yoksa 403 döner — sessiz boş liste DEĞİL.
    /// "fatura" anahtarı SAHİP eliyle TEZGAHTAR'dan alınır (yetki ekranının yolu).
    /// </summary>
    [Fact]
    public async Task Yetkisiz_turde_tek_tur_aramasi_403_doner()
    {
        var k = YeniKiraci("P-15 yetki");
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = [.. Moduller.Tumu],
            ["MUHASEBE"] = ["cari", "urun", "islem", "kasa", "banka", "fatura"],
            // "fatura" BİLEREK YOK:
            ["TEZGAHTAR"] = ["cari", "urun", "islem", "kasa", "banka"],
        };
        var kaydet = await f.Ortam.Istemci(k, "SAHIP")
            .PutAsJsonAsync("/api/v1/yetkiler", new { matris });
        Assert.Equal(HttpStatusCode.OK, kaydet.StatusCode);

        var tezgahtar = f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR");
        var cevap = await tezgahtar.GetAsync("/api/v1/arama?q=FTR&tur=FATURA");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    /// <summary>
    /// Genel aramada yetkisiz tür TARANMAZ ama istek DÜŞMEZ: kullanıcı
    /// görebildiği türlerin sonucunu alır.
    /// </summary>
    [Fact]
    public async Task Genel_aramada_yetkisiz_tur_taranmaz_istek_dusmez()
    {
        var k = YeniKiraci("P-15 genel yetki");
        CariEkle(k, "Genel Yetki Musterisi");
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = [.. Moduller.Tumu],
            ["MUHASEBE"] = ["cari"],
            ["TEZGAHTAR"] = ["cari"],
        };
        await f.Ortam.Istemci(k, "SAHIP").PutAsJsonAsync("/api/v1/yetkiler", new { matris });

        var tezgahtar = f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR");
        var yanit = await Oku(await tezgahtar.GetAsync("/api/v1/arama?q=Genel%20Yetki"));

        Assert.All(yanit.Sonuclar, s => Assert.Equal(AramaTurleri.Cari, s.Tur));
        Assert.Contains(yanit.Sonuclar, s => s.Ad == "Genel Yetki Musterisi");
    }

    // ── Sözleşme (camelCase + alan adları) ───────────────────────────────────

    [Fact]
    public async Task Yanit_camelCase_alan_adlariyla_doner()
    {
        var k = YeniKiraci("P-15 sözleşme");
        CariEkle(k, "Sozlesme Musterisi");

        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/arama?q=Sozlesme&tur=CARI");
        var kok = await cevap.Json();

        Assert.True(kok.TryGetProperty("sonuclar", out var sonuclar));
        Assert.True(kok.TryGetProperty("dahaVar", out _));
        Assert.True(kok.TryGetProperty("tavan", out _));
        var ilk = sonuclar.EnumerateArray().First();
        Assert.True(ilk.TryGetProperty("kimlik", out _));
        Assert.True(ilk.TryGetProperty("altBilgi", out _));
        Assert.True(ilk.TryGetProperty("rota", out _));
    }

    // ── B-25: ürün derin bağlantısı UYGULANABİLİR bir süzgeçtir ─────────────

    [Fact]
    public async Task Urun_rotasi_hedef_sayfanin_OKUDUGU_suzgeci_tasir()
    {
        var k = YeniKiraci("P-15 ürün rota");
        string kod;
        using (var db = f.Vt.Olustur(k.TenantId))
            kod = db.Urunler.Single(u => u.Id == k.UrunId).Kod;

        var yanit = await Oku(await f.Ortam.Istemci(k)
            .GetAsync($"/api/v1/arama?q={Uri.EscapeDataString(kod)}&tur=URUN"));

        var sonuc = Assert.Single(yanit.Sonuclar);
        // Urunler.razor `q` süzgecini OKUR (denetim.sh SORGU_SUZGECI_OKUMAYAN_SAYFA).
        Assert.Equal($"/urunler?q={Uri.EscapeDataString(kod)}", sonuc.Rota);
    }

    // ── KATMAN-1b (tasarım hakemi 29.09 KRİTİK): palet alt bilgisi tr-TR ─────────────
    // Palette "14 Ayar … · K14 · 4.1 g" ve telefon ham "05320000104" görünüyordu. Gram tr-TR
    // ("4,1 g"), telefon "0532 000 01 04" kalıbında döner; arama ham değerle yapılmaya devam eder.

    [Fact]
    public async Task KATMAN1b_urun_alt_bilgisinde_GRAM_tr_TR_4_virgul_1_g()
    {
        var k = YeniKiraci("P-15 ürün gram");
        string kod;
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.GramAgirlik = 4.1m;
            kod = u.Kod;
            db.SaveChanges();
        }

        var yanit = await Oku(await f.Ortam.Istemci(k)
            .GetAsync($"/api/v1/arama?q={Uri.EscapeDataString(kod)}&tur=URUN"));

        var sonuc = Assert.Single(yanit.Sonuclar);
        Assert.EndsWith(" · 4,1 g", sonuc.AltBilgi);
        Assert.DoesNotContain("4.1", sonuc.AltBilgi!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task KATMAN1b_cari_alt_bilgisinde_TELEFON_0532_000_01_04_kalibinda_ham_numarayla_ARANIR()
    {
        var k = YeniKiraci("P-15 telefon");
        CariEkle(k, "Kemal Telefon Testi", "05320000104");

        // Arama HAM rakamlarla yapılır (kullanıcı boşluksuz yazar).
        var yanit = await Oku(await f.Ortam.Istemci(k)
            .GetAsync("/api/v1/arama?q=05320000104&tur=CARI"));

        var sonuc = Assert.Single(yanit.Sonuclar);
        Assert.Equal("0532 000 01 04", sonuc.AltBilgi);
    }

    [Theory]
    [InlineData("05320000104", "0532 000 01 04")]
    [InlineData("+90 532 000 01 04", "0532 000 01 04")]
    [InlineData("5320000104", "0532 000 01 04")]
    [InlineData("0532 000 0104", "0532 000 01 04")]
    [InlineData("1234", "1234")]
    [InlineData("+44 20 7946 0958", "+44 20 7946 0958")]
    public void KATMAN1b_EkranTelefon_TR_kalibi_uymayani_DEGISTIRMEZ(string ham, string beklenen) =>
        Assert.Equal(beklenen, Kuyumcu.Api.Modules.Arama.AramaServisi.EkranTelefon(ham));

    [Theory]
    [InlineData("4.1", "4,1 g")]
    [InlineData("1.75", "1,75 g")]
    [InlineData("1250", "1.250 g")]
    [InlineData("0.125", "0,125 g")]
    public void KATMAN1b_EkranGram_tr_TR(string ham, string beklenen) =>
        Assert.Equal(beklenen, Kuyumcu.Api.Modules.Arama.AramaServisi.EkranGram(
            decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture)));
}
