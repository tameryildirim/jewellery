using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Microsoft.EntityFrameworkCore;
using MCikti = Kuyumcu.Api.Modules.Cikti;
using MPano = Kuyumcu.Api.Modules.Pano;
using MKur = Kuyumcu.Api.Modules.Kur;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-17 — ORTAK A4 ÇIKTI (Y-6) + VİTRİN PANOSU (Y-7) · SAF + HTTP
//
// Her iş kuralı için hatayı YENİDEN ÜRETEN test:
//   Y-6/1  bilinmeyen belge türü → 400 (sessiz boş sayfa YOK)
//   Y-6/2  tür ↔ hareket türü uyuşmazlığı → 400
//   Y-6/3  yetkisiz modül → 403 (fail-closed, tür başına kapı)
//   Y-6/4  kiracı izolasyonu: başka kiracının hareketi → 404
//   Y-6/5  storno edilmiş kayıt kâğıtta UYARI taşır
//   Y-6/6  dört tür de TEK sözleşmeden çıkar; imza/para birimi tür başına doğru
//   Y-6/7  cari ekstresi sunucu sınırını YAZAR (UX-S29)
//   Y-6/8  belge görüntüleme DENETİM İZİNE düşer
//   Y-7/1  pano ayarı kiraci_ayarlari'na yazılır, YENİ TABLO açılmaz
//   Y-7/2  PATCH sözleşmesi: gönderilmeyen alan DEĞİŞMEZ, null = varsayılan
//   Y-7/3  boş gövde → 400 (sessiz etkisizlik yasak)
//   Y-7/4  geçersiz yuvarlama/parite → 400
//   Y-7/5  marj TEZGAHTAR'a kapalı (403), pano AÇIK (200)
//   Y-7/6  varsayılana dön: ayar satırı silinir, kalem varsayılana döner
//   Y-7/7  ayar değişikliği DENETİM İZİNE düşer
//   Y-7/8  CSV dizme/çözme kültürden bağımsız (InvariantCulture)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P17SafTestleri
{
    // ── Y-7/8 — ayar satırı kültürden bağımsız dizilir/çözülür ─────────────

    [Fact]
    public void Ayar_CSV_gidis_donus_ayni_degeri_verir()
    {
        var ayar = new MPano.PanoKalemAyari
        {
            PariteKodu = MKur.PariteKodlari.Gram,
            Sira = 3,
            Etiket = "Gram Altın",
            AlisMarjTl = -12.5m,
            SatisMarjTl = 27.75m,
            YuvarlamaAdimi = 5m,
            Aktif = false,
        };

        var ham = MPano.PanoAyarServisi.Diz(ayar);
        // Kültür kaymazsa ondalık ayırıcı NOKTADIR; virgül görürsek tr-TR sızmış.
        Assert.DoesNotContain(',', ham);
        Assert.True(ham.Length <= 200, "kiraci_ayarlari.deger 200 karakterdir");

        var geri = MPano.PanoAyarServisi.Coz(MKur.PariteKodlari.Gram, ham, Varsayilan());
        Assert.Equal(ayar.Sira, geri.Sira);
        Assert.Equal(ayar.Etiket, geri.Etiket);
        Assert.Equal(ayar.AlisMarjTl, geri.AlisMarjTl);
        Assert.Equal(ayar.SatisMarjTl, geri.SatisMarjTl);
        Assert.Equal(ayar.YuvarlamaAdimi, geri.YuvarlamaAdimi);
        Assert.False(geri.Aktif);
    }

    [Fact]
    public void Bozuk_ayar_satiri_VARSAYILANA_duser_patlamaz()
    {
        // Anayasa md.1: bozuk veri sessizce yutulmaz ama ekranı da kilitlemez;
        // kuyumcu varsayılanı görür ve düzeltebilir (UX-S25).
        var geri = MPano.PanoAyarServisi.Coz(MKur.PariteKodlari.Gram, "abc|def", Varsayilan());
        Assert.Equal(7, geri.Sira);
        Assert.Equal(4m, geri.AlisMarjTl);
    }

    [Fact]
    public void Etiketteki_ayirici_temizlenir_satir_bozulmaz()
    {
        var ham = MPano.PanoAyarServisi.Diz(Varsayilan() with { Etiket = "A|B|C" });
        var geri = MPano.PanoAyarServisi.Coz(MKur.PariteKodlari.Gram, ham, Varsayilan());
        Assert.Equal("A B C", geri.Etiket);
    }

    // ── Y-6/6 — dört belge türü, tek kapalı liste ──────────────────────────

    [Fact]
    public void Dort_belge_turu_vardir_ve_hepsinin_modul_kapisi_yazilidir()
    {
        // 4 → 5: PAKET EKSIK-B1-SATINALMA irsaliye dökümü (IRSALIYE) eklendi. 5 → 6: D1-01 fatura dökümü (FATURA). 6 → 7: D1-10 mutabakat formu. 7 → 8: D1-19 KVKK formu. 8 → 10: D1-13 kimlik tespit formu + ürün belgesi.
        Assert.Equal(10, BelgeTurleri.Tumu.Length);
        foreach (var t in BelgeTurleri.Tumu)
        {
            Assert.True(BelgeTurleri.Gecerli(t));
            Assert.False(string.IsNullOrWhiteSpace(MCikti.CiktiServisi.Modul(t)));
            Assert.False(string.IsNullOrWhiteSpace(MCikti.CiktiServisi.BelgeAdi(t)));
        }
        Assert.False(BelgeTurleri.Gecerli("SATIS_FISI_X"));
        Assert.False(BelgeTurleri.Gecerli(null));
    }

    [Fact]
    public void Belge_turu_modul_kapisi_TUR_BASINA_farklidir()
    {
        // Tek bir [ModulYetkisi] bu ucu ifade EDEMEZ — testin var oluş sebebi bu.
        Assert.Equal("islem", MCikti.CiktiServisi.Modul(BelgeTurleri.SatisFisi));
        Assert.Equal("cari", MCikti.CiktiServisi.Modul(BelgeTurleri.TahsilatMakbuzu));
        Assert.Equal("emanet", MCikti.CiktiServisi.Modul(BelgeTurleri.EmanetTeslim));
        Assert.Equal("cari", MCikti.CiktiServisi.Modul(BelgeTurleri.CariEkstre));
        Assert.Equal("fatura", MCikti.CiktiServisi.Modul(BelgeTurleri.Irsaliye));   // + rol kapısı (EKSIK-B1-SATINALMA)
        Assert.Equal("fatura", MCikti.CiktiServisi.Modul(BelgeTurleri.Fatura));     // D1-01
        Assert.Equal("cari", MCikti.CiktiServisi.Modul(BelgeTurleri.CariMutabakat)); // D1-10
        Assert.Equal("cari", MCikti.CiktiServisi.Modul(BelgeTurleri.KvkkAydinlatma)); // D1-19
        Assert.Equal("uyum", MCikti.CiktiServisi.Modul(BelgeTurleri.KimlikTespit));   // D1-13
        Assert.Equal("islem", MCikti.CiktiServisi.Modul(BelgeTurleri.GarantiKarti));  // D1-13
    }

    [Fact]
    public void Islem_defteri_satiri_dogru_belge_turune_baglanir()
    {
        var f = typeof(Kuyumcu.Web.Pages.IslemDefteri);
        Assert.NotNull(f);
        Assert.Equal(BelgeTurleri.SatisFisi, Kuyumcu.Web.Pages.IslemDefteri.BelgeTuru("SATIS"));
        Assert.Equal(BelgeTurleri.TahsilatMakbuzu, Kuyumcu.Web.Pages.IslemDefteri.BelgeTuru("TAHSILAT"));
        Assert.Equal(BelgeTurleri.EmanetTeslim, Kuyumcu.Web.Pages.IslemDefteri.BelgeTuru("EMANET_CIKIS"));
        // Karşılığı olmayan tür için düğme ÇİZİLMEZ (null), uydurulmaz.
        Assert.Null(Kuyumcu.Web.Pages.IslemDefteri.BelgeTuru("TRANSFER"));
    }

    // ── Sözlük: TR ve EN eksiksiz (çıplak metin yok) ───────────────────────

    [Fact]
    public void Cikti_ve_Pano_sozlukleri_TR_ve_EN_dolu()
    {
        foreach (var s in new object[]
                 {
                     Kuyumcu.Core.I18n.Sozlukler.Tr.Cikti, Kuyumcu.Core.I18n.Sozlukler.En.Cikti,
                     Kuyumcu.Core.I18n.Sozlukler.Tr.Pano, Kuyumcu.Core.I18n.Sozlukler.En.Pano,
                 })
        {
            foreach (var p in s.GetType().GetProperties())
                Assert.False(string.IsNullOrWhiteSpace((string)p.GetValue(s)!),
                    $"{s.GetType().Name}.{p.Name} boş");
        }
    }

    private static MPano.PanoKalemAyari Varsayilan() => new()
    {
        PariteKodu = MKur.PariteKodlari.Gram,
        Sira = 7,
        AlisMarjTl = 4m,
        SatisMarjTl = 6m,
        YuvarlamaAdimi = 10m,
        Aktif = true,
    };
}

[Collection("http")]
public sealed class P17HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Deftere doğrudan bir hareket satırı yazar (belge kaynağı).</summary>
    private string HareketYaz(Kiraci k, string tur, decimal tl, decimal has,
        string? fisId = null, string? stornoKaynakId = null)
    {
        using var db = f.Vt.OlusturSistem();
        var id = Kimlik.Ulid();
        db.Hareketler.Add(new Hareket
        {
            Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = tur,
            Tarih = DateTime.UtcNow, TlTutari = tl, HasGrami = has, ParaBirimi = "TRY",
            KurSnapshotId = k.KurId, CariId = k.CariId, KullaniciId = k.KullaniciId,
            FisId = fisId, StornoKaynakId = stornoKaynakId,
            Aciklama = "P-17 test satırı", Olusturuldu = DateTime.UtcNow,
        });
        db.SaveChanges();
        return id;
    }

    // ── Y-6/1 — bilinmeyen tür 400 ─────────────────────────────────────────

    [Fact]
    public async Task Bilinmeyen_belge_turu_400_doner()
    {
        var k = YeniKiraci("P17 tür");
        var c = await f.Ortam.Istemci(k).GetAsync("/api/v1/cikti/BILINMEYEN/x");
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("SATIS_FISI", await c.Metin(), StringComparison.Ordinal);
    }

    // ── Y-6/2 — tür ↔ hareket uyuşmazlığı 400 ──────────────────────────────

    [Fact]
    public async Task Tahsilat_hareketinden_SATIS_FISI_istenirse_400_doner()
    {
        var k = YeniKiraci("P17 uyuşmazlık");
        var id = HareketYaz(k, "TAHSILAT", -500m, 0m);

        var c = await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.SatisFisi}/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("TAHSILAT", await c.Metin(), StringComparison.Ordinal);
    }

    // ── Y-6/3 — tür başına yetki kapısı (fail-closed) ──────────────────────

    [Fact]
    public async Task Emanet_modulu_kapali_rolde_emanet_belgesi_403_doner()
    {
        var k = YeniKiraci("P17 yetki");
        var id = HareketYaz(k, "EMANET_GIRIS", 0m, 12.5m);
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");

        // Varsayılan matriste tezgâhtarda "emanet" VARDIR — önce belge açılır.
        Assert.Equal(HttpStatusCode.OK,
            (await tezgahtar.GetAsync($"/api/v1/cikti/{BelgeTurleri.EmanetTeslim}/{id}")).StatusCode);

        // SAHİP tezgâhtardan "emanet" modülünü kaldırır (yetki ekranının yolu).
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = Kuyumcu.Api.Modules.Yetki.Moduller.Varsayilan["MUDUR"],
            ["MUHASEBE"] = Kuyumcu.Api.Modules.Yetki.Moduller.Varsayilan["MUHASEBE"],
            ["TEZGAHTAR"] = [.. Kuyumcu.Api.Modules.Yetki.Moduller.Varsayilan["TEZGAHTAR"].Where(m => m != "emanet")],
        };
        Assert.Equal(HttpStatusCode.OK,
            (await f.Ortam.Istemci(k).PutAsJsonAsync("/api/v1/yetkiler", new { matris })).StatusCode);

        // Kapı TÜR BAŞINA kapanır: emanet belgesi 403, tahsilat makbuzu AÇIK.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tezgahtar.GetAsync($"/api/v1/cikti/{BelgeTurleri.EmanetTeslim}/{id}")).StatusCode);

        var tahsilat = HareketYaz(k, "TAHSILAT", -100m, 0m);
        Assert.Equal(HttpStatusCode.OK,
            (await tezgahtar.GetAsync($"/api/v1/cikti/{BelgeTurleri.TahsilatMakbuzu}/{tahsilat}")).StatusCode);
    }

    // ── Y-6/4 — kiracı izolasyonu ──────────────────────────────────────────

    [Fact]
    public async Task Baska_kiracinin_hareketinin_belgesi_404_doner()
    {
        var a = YeniKiraci("P17 kiracı A");
        var b = YeniKiraci("P17 kiracı B");
        var id = HareketYaz(a, "SATIS", 1_000m, 5m);

        var c = await f.Ortam.Istemci(b).GetAsync($"/api/v1/cikti/{BelgeTurleri.SatisFisi}/{id}");
        Assert.Equal(HttpStatusCode.NotFound, c.StatusCode);
    }

    // ── Y-6/5 — storno edilmiş kayıt kâğıtta uyarı taşır ───────────────────

    [Fact]
    public async Task Storno_edilmis_fis_belgesinde_UYARI_vardir()
    {
        var k = YeniKiraci("P17 storno");
        var asil = HareketYaz(k, "SATIS", 1_000m, 5m);
        HareketYaz(k, "SATIS", -1_000m, -5m, stornoKaynakId: asil);

        var c = await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.SatisFisi}/{asil}");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await c.Json();
        Assert.Contains("STORNO", j.GetProperty("uyari").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stornosuz_fis_belgesinde_uyari_YOKTUR()
    {
        var k = YeniKiraci("P17 temiz fiş");
        var asil = HareketYaz(k, "SATIS", 1_000m, 5m);

        var j = await (await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.SatisFisi}/{asil}")).Json();
        Assert.Equal(System.Text.Json.JsonValueKind.Null, j.GetProperty("uyari").ValueKind);
    }

    // ── Y-6/6 — dört tür de aynı sözleşmeyi doldurur ───────────────────────

    [Fact]
    public async Task Dort_belge_turu_de_AYNI_sozlesmeyi_doldurur()
    {
        var k = YeniKiraci("P17 dört tür");
        var istemci = f.Ortam.Istemci(k);

        var kaynaklar = new (string Tur, string Id, string Imza)[]
        {
            (BelgeTurleri.SatisFisi, HareketYaz(k, "SATIS", 1_000m, 5m), "Alıcı"),
            (BelgeTurleri.TahsilatMakbuzu, HareketYaz(k, "TAHSILAT", -400m, 0m), "Ödeyen"),
            (BelgeTurleri.EmanetTeslim, HareketYaz(k, "EMANET_CIKIS", 0m, -3m), "Teslim alan"),
            (BelgeTurleri.CariEkstre, k.CariId, "Düzenleyen"),
        };

        foreach (var (tur, id, imza) in kaynaklar)
        {
            var c = await istemci.GetAsync($"/api/v1/cikti/{tur}/{id}");
            Assert.Equal(HttpStatusCode.OK, c.StatusCode);
            var j = await c.Json();

            // JSON sözleşmesi camelCase ve TÜM alanlar dolu — dört türde de aynı.
            Assert.Equal(tur, j.GetProperty("tur").GetString());
            Assert.False(string.IsNullOrWhiteSpace(j.GetProperty("belgeAdi").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(j.GetProperty("belgeNo").GetString()));
            Assert.True(j.GetProperty("bolumler").GetArrayLength() >= 1);
            Assert.True(j.GetProperty("tutarlar").GetArrayLength() >= 1);
            Assert.Contains(imza,
                j.GetProperty("imzaBasliklari").EnumerateArray().Select(x => x.GetString()!));

            // UX-S32: her tutar KENDİ para birimini taşır — "varsayılan TL" yok.
            foreach (var t in j.GetProperty("tutarlar").EnumerateArray())
                Assert.False(string.IsNullOrWhiteSpace(t.GetProperty("paraBirimi").GetString()));
        }
    }

    // ── Y-6/7 — cari ekstresi sunucu sınırını YAZAR ────────────────────────

    [Fact]
    public async Task Cari_ekstresi_sunucu_sinirini_YAZAR()
    {
        var k = YeniKiraci("P17 ekstre");
        HareketYaz(k, "TAHSILAT", -100m, 0m);

        var j = await (await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.CariEkstre}/{k.CariId}")).Json();
        var not = j.GetProperty("bolumler")[0].GetProperty("not").GetString();
        // UX-S29: sessiz kırpma YOK — sınır her hâlde yazılır.
        Assert.Contains(MCikti.CiktiServisi.EkstreSatirTavani.ToString(), not!, StringComparison.Ordinal);
    }

    // ── Y-6/8 — belge görüntüleme denetim izine düşer ──────────────────────

    [Fact]
    public async Task Belge_goruntulemesi_DENETIM_IZINE_duser()
    {
        var k = YeniKiraci("P17 denetim");
        var id = HareketYaz(k, "SATIS", 1_000m, 5m);

        await f.Ortam.Istemci(k).GetAsync($"/api/v1/cikti/{BelgeTurleri.SatisFisi}/{id}");

        using var db = f.Vt.OlusturSistem();
        Assert.True(await db.DenetimKayitlari.AsNoTracking()
            .AnyAsync(d => d.TenantId == k.TenantId && d.VarlikTuru == "CiktiBelgesi" && d.VarlikId == id));
    }

    // ── Y-7/1 — pano ayarı kiraci_ayarlari'na yazılır ──────────────────────

    [Fact]
    public async Task Pano_ayari_kiraci_ayarlarina_yazilir_yeni_tablo_acilmaz()
    {
        var k = YeniKiraci("P17 pano yaz");
        var c = await f.Ortam.Istemci(k).PatchJson(
            $"/api/v1/pano/ayarlar/{MKur.PariteKodlari.Gram}", new { alisMarjTl = -25m, satisMarjTl = 35m });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var satir = await db.KiraciAyarlari.AsNoTracking().SingleAsync(
            a => a.TenantId == k.TenantId && a.Anahtar == MPano.PanoAyarAnahtari.Anahtar(MKur.PariteKodlari.Gram));
        Assert.Contains("-25", satir.Deger, StringComparison.Ordinal);
        Assert.True(satir.Deger.Length <= 200);
    }

    // ── Y-7/2 — PATCH sözleşmesi ───────────────────────────────────────────

    [Fact]
    public async Task PATCH_gonderilmeyen_alani_DEGISTIRMEZ()
    {
        var k = YeniKiraci("P17 patch koru");
        var kod = MKur.PariteKodlari.CeyrekYeni;
        var istemci = f.Ortam.Istemci(k);

        await istemci.PatchJson($"/api/v1/pano/ayarlar/{kod}", new { alisMarjTl = -50m, etiket = "Çeyrek" });
        // İkinci PATCH yalnız satış marjını gönderir; alış marjı ve etiket DURMALI.
        var j = await (await istemci.PatchJson($"/api/v1/pano/ayarlar/{kod}", new { satisMarjTl = 70m })).Json();

        var ayar = j.GetProperty("ayarlar").EnumerateArray().Single(a => a.GetProperty("kod").GetString() == kod);
        Assert.Equal(-50m, ayar.GetProperty("alisMarjTl").GetDecimal());
        Assert.Equal(70m, ayar.GetProperty("satisMarjTl").GetDecimal());
        Assert.Equal("Çeyrek", ayar.GetProperty("etiket").GetString());
    }

    [Fact]
    public async Task PATCH_acikca_null_gonderilen_alan_VARSAYILANA_doner()
    {
        var k = YeniKiraci("P17 patch null");
        var kod = MKur.PariteKodlari.CeyrekYeni;
        var istemci = f.Ortam.Istemci(k);

        await istemci.PatchJson($"/api/v1/pano/ayarlar/{kod}", new { alisMarjTl = -50m });
        var j = await (await istemci.PatchJson($"/api/v1/pano/ayarlar/{kod}",
            new { alisMarjTl = (decimal?)null })).Json();

        var ayar = j.GetProperty("ayarlar").EnumerateArray().Single(a => a.GetProperty("kod").GetString() == kod);
        Assert.Equal(0m, ayar.GetProperty("alisMarjTl").GetDecimal());
    }

    // ── Y-7/3 — boş gövde 400 (sessiz etkisizlik yasak) ────────────────────

    [Fact]
    public async Task Bos_PATCH_govdesi_400_doner()
    {
        var k = YeniKiraci("P17 boş patch");
        var c = await f.Ortam.Istemci(k).PatchJson($"/api/v1/pano/ayarlar/{MKur.PariteKodlari.Gram}", new { });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        // ⚠ Ham gövde \uXXXX kaçışlıdır; Türkçe mesaj AYRIŞTIRILARAK sınanır.
        Assert.Contains("Güncellenecek alan",
            (await c.Json()).GetProperty("message").GetString()!, StringComparison.Ordinal);
    }

    // ── Y-7/4 — geçersiz değerler 400 ──────────────────────────────────────

    [Fact]
    public async Task Gecersiz_yuvarlama_adimi_400_doner()
    {
        var k = YeniKiraci("P17 yuvarlama");
        var c = await f.Ortam.Istemci(k).PatchJson(
            $"/api/v1/pano/ayarlar/{MKur.PariteKodlari.Gram}", new { yuvarlamaAdimi = 7m });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("uvarlama",
            (await c.Json()).GetProperty("message").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Gecersiz_parite_kodu_400_doner()
    {
        var k = YeniKiraci("P17 parite");
        var c = await f.Ortam.Istemci(k).PatchJson("/api/v1/pano/ayarlar/YOKBOYLE", new { aktif = true });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
    }

    // ── Y-7/5 — marj ticari sırdır: ayar ucu MUDUR+, pano herkese ──────────

    [Fact]
    public async Task Marj_ayarlari_TEZGAHTARA_kapali_pano_ACIK()
    {
        var k = YeniKiraci("P17 marj kapısı");
        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");

        Assert.Equal(HttpStatusCode.Forbidden, (await tezgahtar.GetAsync("/api/v1/pano/ayarlar")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tezgahtar.PatchJson($"/api/v1/pano/ayarlar/{MKur.PariteKodlari.Gram}", new { aktif = false })).StatusCode);
    }

    [Fact]
    public async Task Pano_gorunumunde_MARJ_alani_YOKTUR()
    {
        var k = YeniKiraci("P17 marj sızıntı");
        var j = await (await f.Ortam.Istemci(k).GetAsync("/api/v1/pano/guncel")).Json();

        var ham = j.GetRawText();
        // Marj hiçbir biçimde vitrine inmez — hesap sunucuda biter.
        Assert.DoesNotContain("arj", ham, StringComparison.Ordinal);
        Assert.True(j.TryGetProperty("satirlar", out _));
        Assert.True(j.TryGetProperty("atlananlar", out _));
    }

    // ── Y-7/6 — varsayılana dön ────────────────────────────────────────────

    [Fact]
    public async Task Varsayilana_don_ayar_satirini_siler_ve_kalem_varsayilana_doner()
    {
        var k = YeniKiraci("P17 varsayılan");
        var kod = MKur.PariteKodlari.Gram;
        var istemci = f.Ortam.Istemci(k);

        await istemci.PatchJson($"/api/v1/pano/ayarlar/{kod}", new { alisMarjTl = -99m, aktif = false });
        var j = await (await istemci.PostJson($"/api/v1/pano/ayarlar/{kod}/varsayilana-don", new { })).Json();

        var ayar = j.GetProperty("ayarlar").EnumerateArray().Single(a => a.GetProperty("kod").GetString() == kod);
        Assert.Equal(0m, ayar.GetProperty("alisMarjTl").GetDecimal());
        Assert.True(ayar.GetProperty("aktif").GetBoolean());
        Assert.False(ayar.GetProperty("ozel").GetBoolean());

        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.KiraciAyarlari.AsNoTracking()
            .AnyAsync(a => a.TenantId == k.TenantId && a.Anahtar == MPano.PanoAyarAnahtari.Anahtar(kod)));
    }

    [Fact]
    public async Task Ayari_olmayan_kalemde_varsayilana_don_404_doner()
    {
        var k = YeniKiraci("P17 varsayılan yok");
        var c = await f.Ortam.Istemci(k).PostJson(
            $"/api/v1/pano/ayarlar/{MKur.PariteKodlari.Gumus}/varsayilana-don", new { });
        Assert.Equal(HttpStatusCode.NotFound, c.StatusCode);
    }

    // ── Y-7/7 — ayar değişikliği denetim izine düşer ───────────────────────

    [Fact]
    public async Task Pano_ayar_degisikligi_DENETIM_IZINE_duser()
    {
        var k = YeniKiraci("P17 pano denetim");
        await f.Ortam.Istemci(k).PatchJson(
            $"/api/v1/pano/ayarlar/{MKur.PariteKodlari.Gram}", new { satisMarjTl = 15m });

        using var db = f.Vt.OlusturSistem();
        Assert.True(await db.DenetimKayitlari.AsNoTracking().AnyAsync(
            d => d.TenantId == k.TenantId && d.VarlikTuru == "PanoKalemAyari"));
    }

    // ── Kiracı izolasyonu: bir kiracının ayarı diğerine sızmaz ─────────────

    [Fact]
    public async Task Bir_kiracinin_pano_ayari_digerine_SIZMAZ()
    {
        var a = YeniKiraci("P17 pano A");
        var b = YeniKiraci("P17 pano B");
        var kod = MKur.PariteKodlari.Gram;

        await f.Ortam.Istemci(a).PatchJson($"/api/v1/pano/ayarlar/{kod}", new { satisMarjTl = 123m });

        var j = await (await f.Ortam.Istemci(b).GetAsync("/api/v1/pano/ayarlar")).Json();
        var ayar = j.GetProperty("ayarlar").EnumerateArray().Single(x => x.GetProperty("kod").GetString() == kod);
        Assert.Equal(0m, ayar.GetProperty("satisMarjTl").GetDecimal());
        Assert.False(ayar.GetProperty("ozel").GetBoolean());
    }

    // ── Varsayılan pano kümesi: dükkân seti AÇIK, kalanlar kapalı ──────────

    [Fact]
    public async Task Varsayilan_pano_kumesi_dukkan_setidir()
    {
        var k = YeniKiraci("P17 varsayılan küme");
        var j = await (await f.Ortam.Istemci(k).GetAsync("/api/v1/pano/ayarlar")).Json();

        var aktifKodlar = j.GetProperty("ayarlar").EnumerateArray()
            .Where(a => a.GetProperty("aktif").GetBoolean())
            .Select(a => a.GetProperty("kod").GetString()!)
            .ToList();

        Assert.Equal(MPano.PanoFiyatServisi.VarsayilanKalemler.Count, aktifKodlar.Count);
        foreach (var kod in MPano.PanoFiyatServisi.VarsayilanKalemler)
            Assert.Contains(kod, aktifKodlar);
        Assert.Equal(aktifKodlar.Count, j.GetProperty("aktifSayisi").GetInt32());
    }
}
