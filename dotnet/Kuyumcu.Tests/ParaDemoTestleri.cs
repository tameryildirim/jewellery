using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Core.I18n;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MHurda = Kuyumcu.Api.Modules.Hurda;
using MKasa = Kuyumcu.Api.Modules.Kasa;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PARA-DEMO — uzun demo kaydının (01.10) para / yasal bulguları
//   (1) Günü Kapat: sayılan nakit YALNIZ nakit defteriyle karşılaştırılır
//       (kart + havale kasaya yazılmış olsa da çekmecede yoktur).
//   (2) Has ile tahsilat TL borcunu DÜŞÜRMEZ (ekip kararı B: davranış aynı, ekran söyler).
//   (3) Takasta satıcı kimliği (VUK md.234 · TBK md.283) — fail-closed + gider pusulası.
//   (5) Cari ekstresi (kâğıt): işlem = tür · ödeme, borç etkisi, yürüyen bakiye.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class ParaDemoDbTestleri(TestVeritabani vt)
{
    private static MKasa.KasaServisi Kasa(KuyumcuDbContext db) => new(db, NullLogger<MKasa.KasaServisi>.Instance);
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) => new(db, NullLogger<MSatis.SatisServisi>.Instance);
    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, TestKurulum.Denetim(db));

    private Kiraci YeniKiraci(string ad)
    {
        using var db = vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, ad));
    }

    private async Task SatisYap(Kiraci k, string odeme, decimal tutar)
    {
        using var db = vt.Olustur(k);
        await Satis(db).Satis(new MSatis.SatisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = tutar,
            OdemeTuru = odeme, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
    }

    /// <summary>Kabul senaryosu: nakit 1000 + kart 500 + havale 300 → sayılan nakit 1000 (+ açılış nakdi).</summary>
    private async Task<Kiraci> UcAracliKiraci(string ad)
    {
        var k = YeniKiraci(ad);
        await SatisYap(k, "NAKIT", 1_000m);
        await SatisYap(k, "KART", 500m);
        await SatisYap(k, "HAVALE", 300m);
        return k;
    }

    // ── (1) Gün sonu nakit sayımı ────────────────────────────────────────────

    [Fact]
    public async Task PD1_nakit_1000_kart_500_havale_300_sayilan_nakitle_fark_SIFIR()
    {
        var k = await UcAracliKiraci("PD1 sayım tam");
        using var db = vt.Olustur(k);

        // Açılış nakdi (250.000, hareketsiz devir) + 1.000 nakit satış = çekmecedeki nakit.
        await Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis + 1_000m }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var kapanis = kontrol.GunKapanislari.AsNoTracking()
            .Single(g => g.TenantId == k.TenantId && g.KasaId == k.KasaId);
        Assert.Equal(0m, kapanis.Fark);
        Assert.Equal(TestKurulum.KasaAcilis + 1_000m, kapanis.DefterBakiye);   // karşılaştırılan NAKİT defteri
        Assert.Null(kapanis.Aciklama);
        // Defterin kendisi DEĞİŞMEDİ: kasa bakiyesi hâlâ nakit + kart + havale.
        Assert.Equal(TestKurulum.KasaAcilis + 1_800m,
            kontrol.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye);
    }

    [Fact]
    public async Task PD1_toplam_bakiye_sayilirsa_kart_havale_kadar_FAZLA_cikar_gerekce_ister()
    {
        // Sınır: tezgâhtar kart+havaleyi de saymış gibi toplamı yazarsa bu bir FAZLADIR (+800), açık değil.
        var k = await UcAracliKiraci("PD1 toplam sayıldı");
        using var db = vt.Olustur(k);

        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Kasa(db).GunKapat(k.KasaId,
            new MKasa.GunKapanisiDto { SayilanNakit = TestKurulum.KasaAcilis + 1_800m }, k.Islem));
        // SON-KALANLAR: ileti sade (büyük harf/"TL" yok, ₺ + farkın yönü) ve alan:aciklama — kural aynı.
        Assert.Contains("800,00 ₺ fazla", hata.Message);
        Assert.Contains("Farkın nedenini yazın", hata.Message);
        Assert.Contains("kart 500,00 ₺", hata.Message);
        Assert.Contains("havale 300,00 ₺", hata.Message);
    }

    [Fact]
    public async Task PD1_eksik_nakit_negatif_fark_olarak_kaydedilir()
    {
        var k = await UcAracliKiraci("PD1 eksik");
        using var db = vt.Olustur(k);
        await Kasa(db).GunKapat(k.KasaId, new MKasa.GunKapanisiDto
        {
            SayilanNakit = TestKurulum.KasaAcilis + 900m, Aciklama = "Yüz lira eksik çıktı",
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(-100m, kontrol.GunKapanislari.AsNoTracking()
            .Single(g => g.TenantId == k.TenantId && g.KasaId == k.KasaId).Fark);
    }

    [Fact]
    public async Task PD1_onizleme_toplam_esittir_nakit_arti_kart_arti_havale()
    {
        var k = await UcAracliKiraci("PD1 önizleme");
        using var db = vt.Olustur(k);
        var o = await Kasa(db).GunKapanisiOnizleme(k.KasaId, k.Islem);

        Assert.Equal(TestKurulum.KasaAcilis + 1_800m, o.ToplamBakiye);
        Assert.Equal(500m, o.KartToplam);
        Assert.Equal(300m, o.HavaleToplam);
        Assert.Equal(TestKurulum.KasaAcilis + 1_000m, o.NakitDefter);
        Assert.Equal(o.ToplamBakiye, o.NakitDefter + o.KartToplam + o.HavaleToplam);
    }

    [Fact]
    public async Task PD1_kartla_cari_tahsilati_da_sayima_girmez()
    {
        // Demo veresiye-03: kalan borç KARTLA tahsil edildi → kasaya KART yazıldı; sayımda nakit değil.
        var k = YeniKiraci("PD1 kart tahsilat");
        await SatisYap(k, "VERESIYE", 2_000m);
        using (var db = vt.Olustur(k))
            await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
            {
                TlTutari = 2_000m, OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId,
            }, k.Islem);

        using var db2 = vt.Olustur(k);
        var ayrim = await MKasa.KasaNakitAyrimi.Hesapla(db2, k.TenantId,
            db2.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId));
        Assert.Equal(2_000m, ayrim.Kart);
        Assert.Equal(TestKurulum.KasaAcilis, ayrim.Nakit);
    }

    [Fact]
    public void PD1_nakit_disi_araclar_YALNIZ_kart_ve_havale()
    {
        // Açılış devri (DIGER), gider/virman (NAKIT) ve döviz (DOVIZ) bugünkü gibi nakit sayılır.
        Assert.Equal(["KART", "HAVALE"], MKasa.KasaNakitAyrimi.NakitDisiAraclar);
    }

    // ── (2) Has ile tahsilat ─────────────────────────────────────────────────

    [Fact]
    public async Task PD2_has_ile_tahsilat_TL_borcu_DEGISTIRMEZ_altin_hesabina_yazar()
    {
        // Ekip kararı B (rapor § madde 2): has TL'ye ÇEVRİLMEZ (alış fiyatı dükkân kararıdır, K-KUR-3;
        // altını parayla almak gider pusulası ister). Ekran bunu söyler; defter iki ayrı bacak kalır.
        var k = YeniKiraci("PD2 has tahsilat");
        await SatisYap(k, "VERESIYE", 5_000m);
        using (var db = vt.Olustur(k))
            await Cari(db).TahsilatGir(k.CariId, new MCari.TahsilatGirisDto
            {
                HasGrami = 1m, OdemeTuru = "NAKIT", KurSnapshotId = k.KurId,
            }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var b = kontrol.CariBakiyeler.AsNoTracking().Single(x => x.CariId == k.CariId);
        Assert.Equal(5_000m, b.TlBakiye);
        Assert.Equal(-1m, b.HasGramBakiye);
        Assert.Equal(TestKurulum.KasaAcilis, kontrol.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye);
        // Para el değiştirmedi → ödeme etiketi YOK (P-23; formun "NAKIT" varsayılanı deftere yazılmaz).
        var th = kontrol.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT");
        Assert.Null(th.OdemeTuru);
        Assert.Empty(kontrol.KasaHareketleri.AsNoTracking().Where(x => x.TenantId == k.TenantId && x.HareketId == th.Id));
    }

    // ── (3) Takasta satıcı kimliği ───────────────────────────────────────────

    private static MSatis.TakasDto TakasDto(Kiraci k, MHurda.HurdaSaticiDto? satici) => new()
    {
        CariId = k.CariId, EskiUrunAyarId = "K22", EskiUrunGram = 3m, EskiUrunBirimFiyatTlHasGram = 3_000m,
        YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 20_000m,
        OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId, Satici = satici,
    };

    private static MHurda.HurdaSaticiDto Vatandas() => new()
    {
        SaticiTipi = "NIHAI_TUKETICI", AdSoyad = "Deneme Vatandaş", Tckn = "10000000146", Adres = "Çarşı Mah. 1",
    };

    [Fact]
    public async Task PD3_takas_kimliksiz_REDDEDILIR_hicbir_sey_yazilmaz()
    {
        var k = YeniKiraci("PD3 kimliksiz");
        using var db = vt.Olustur(k);
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Takas(TakasDto(k, null), k.Islem));
        Assert.Contains("satıcı kimliği zorunludur", hata.Message);

        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "TAKAS"));
        Assert.Equal(TestKurulum.KasaAcilis, kontrol.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye);
    }

    [Fact]
    public async Task PD3_takas_TCKNsiz_vatandas_REDDEDILIR()
    {
        var k = YeniKiraci("PD3 tcknsiz");
        using var db = vt.Olustur(k);
        var s = Vatandas(); s.Tckn = null;
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Takas(TakasDto(k, s), k.Islem));
        Assert.Contains("TCKN zorunludur", hata.Message);
    }

    [Fact]
    public async Task PD3_takas_vatandastan_GIDER_PUSULASI_ayni_committe_dogar()
    {
        var k = YeniKiraci("PD3 pusula");
        using (var db = vt.Olustur(k))
        {
            var j = JsonSerializer.SerializeToElement(await Satis(db).Takas(TakasDto(k, Vatandas()), k.Islem));
            Assert.Equal("NIHAI_TUKETICI", j.GetProperty("saticiTipi").GetString());
            Assert.False(string.IsNullOrEmpty(j.GetProperty("giderPusulasi").GetProperty("PusulaNo").GetString()));
        }

        using var kontrol = vt.OlusturSistem();
        var takas = kontrol.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "TAKAS");
        var pusula = kontrol.GiderPusulalari.AsNoTracking().Single(g => g.TenantId == k.TenantId);
        Assert.Equal(takas.Id, pusula.HareketId);
        Assert.Equal("10000000146", pusula.SaticiTckn);
        // Brüt = eski altının bedeli: 3 g × 0,916 × 3.000 = 8.244,00; stopaj 0 → net = brüt.
        Assert.Equal(8_244m, pusula.BrutTutar);
        Assert.Equal(pusula.BrutTutar, pusula.NetOdenen);
        Assert.Equal(2.748m, pusula.HasGrami);
    }

    [Fact]
    public async Task PD3_takas_esnaf_muaf_stopajli_REDDEDILIR()
    {
        var k = YeniKiraci("PD3 esnaf");
        using var db = vt.Olustur(k);
        var s = Vatandas(); s.SaticiTipi = "ESNAF_MUAF";
        var hata = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Takas(TakasDto(k, s), k.Islem));
        Assert.Contains("stopaj", hata.Message);
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.GiderPusulalari.AsNoTracking().Where(g => g.TenantId == k.TenantId));
    }

    [Fact]
    public async Task PD3_takas_mukellef_pusula_yok_fatura_no_aciklamada()
    {
        var k = YeniKiraci("PD3 mükellef");
        using (var db = vt.Olustur(k))
        {
            var j = JsonSerializer.SerializeToElement(await Satis(db).Takas(TakasDto(k,
                new MHurda.HurdaSaticiDto { SaticiTipi = "MUKELLEF", TedarikciFaturaNo = "ADK2026000457" }), k.Islem));
            Assert.Equal(JsonValueKind.Null, j.GetProperty("giderPusulasi").ValueKind);
        }
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.GiderPusulalari.AsNoTracking().Where(g => g.TenantId == k.TenantId));
        var takas = kontrol.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "TAKAS");
        Assert.Contains("ADK2026000457", takas.Aciklama);
    }

    [Fact]
    public async Task PD3_takas_mukellef_fatura_nosuz_REDDEDILIR()
    {
        var k = YeniKiraci("PD3 mükellef eksik");
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).Takas(TakasDto(k,
            new MHurda.HurdaSaticiDto { SaticiTipi = "MUKELLEF" }), k.Islem));
    }
}

[Collection("http")]
public sealed class ParaDemoHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, ad));
    }

    private static async Task Satis(HttpClient c, Kiraci k, string odeme, decimal tutar)
    {
        var r = await c.PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1, birimFiyatTl = tutar,
            odemeTuru = odeme, kasaId = odeme == "VERESIYE" ? null : k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PD1_HTTP_onizleme_ve_gun_kapat_kabul_senaryosu()
    {
        var k = YeniKiraci("PD1 http");
        var c = f.Ortam.Istemci(k);
        await Satis(c, k, "NAKIT", 1_000m);
        await Satis(c, k, "KART", 500m);
        await Satis(c, k, "HAVALE", 300m);

        var on = await c.GetAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi/onizleme");
        Assert.Equal(HttpStatusCode.OK, on.StatusCode);
        var oj = await on.Json();
        Assert.Equal(TestKurulum.KasaAcilis + 1_000m, oj.GetProperty("nakitDefter").GetDecimal());

        var r = await c.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi",
            new { sayilanNakit = TestKurulum.KasaAcilis + 1_000m });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Json();
        Assert.Equal(0m, j.GetProperty("fark").GetDecimal());
        Assert.Equal(500m, j.GetProperty("kartToplam").GetDecimal());
        Assert.Equal(300m, j.GetProperty("havaleToplam").GetDecimal());
        Assert.Equal(TestKurulum.KasaAcilis + 1_800m, j.GetProperty("toplamBakiye").GetDecimal());
    }

    [Fact]
    public async Task PD1_HTTP_onizleme_TEZGAHTAR_ROLUNE_kapali()
    {
        // Kapanışa yetkili rollerle aynı küme (SAHIP · MUDUR · MUHASEBE).
        var k = YeniKiraci("PD1 http rol");
        var r = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi/onizleme");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task PD1_HTTP_gun_sonu_mutabakati_alt_kalemler_toplami_esittir_toplam_HAVALE_dahil()
    {
        var k = YeniKiraci("PD1 mutabakat");
        var c = f.Ortam.Istemci(k);
        await Satis(c, k, "NAKIT", 1_000m);
        await Satis(c, k, "KART", 500m);
        await Satis(c, k, "HAVALE", 300m);

        var j = await (await c.GetAsync("/api/v1/raporlar/pos-fatura-mutabakati")).Json();
        var t = j.GetProperty("tahsilat");
        Assert.Equal(300m, t.GetProperty("havale").GetDecimal());
        Assert.Equal(t.GetProperty("toplam").GetDecimal(),
            t.GetProperty("nakit").GetDecimal() + t.GetProperty("kart").GetDecimal()
            + t.GetProperty("havale").GetDecimal() + t.GetProperty("diger").GetDecimal());
    }

    [Fact]
    public async Task PD3_HTTP_takas_kimliksiz_400()
    {
        var k = YeniKiraci("PD3 http");
        var r = await f.Ortam.Istemci(k).PostAsJsonAsync("/api/v1/islemler/takas", new
        {
            cariId = k.CariId, eskiUrunAyarId = "K22", eskiUrunGram = 3, eskiUrunBirimFiyatTlHasGram = 3000,
            yeniUrunId = k.UrunId, yeniUrunAdet = 1, yeniUrunBirimFiyatTl = 20000,
            odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("satıcı kimliği zorunludur", (await r.Json()).GetProperty("message").GetString());
    }

    // ── (5) Cari ekstresi (kâğıt) ────────────────────────────────────────────

    [Fact]
    public async Task PD5_ekstre_isleme_odeme_yazilir_borc_etkisi_ve_yuruyen_bakiye_TUTAR()
    {
        var k = YeniKiraci("PD5 ekstre");
        var c = f.Ortam.Istemci(k);
        await Satis(c, k, "NAKIT", 1_000m);      // peşin: borca yazılmaz
        await Satis(c, k, "VERESIYE", 2_000m);   // borç +2.000
        var th = await c.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/tahsilat", new
        {
            tlTutari = 500m, odemeTuru = "KART", kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.True(th.IsSuccessStatusCode, await th.Content.ReadAsStringAsync());

        var j = await (await c.GetAsync($"/api/v1/cikti/{BelgeTurleri.CariEkstre}/{k.CariId}")).Json();
        var bolum = j.GetProperty("bolumler")[0];
        Assert.Equal(["Tarih", "İşlem", "Açıklama", "Tutar", "Has etkisi", "Borç etkisi", "Bakiye"],
            bolum.GetProperty("sutunBasliklari").EnumerateArray().Select(x => x.GetString()!).ToArray());
        var satirlar = bolum.GetProperty("satirlar").EnumerateArray()
            .Select(s => s.GetProperty("hucreler").EnumerateArray().Select(x => x.GetString()!).ToArray()).ToList();
        Assert.Equal(3, satirlar.Count);

        Assert.Equal("Satış · Nakit", satirlar[0][1]);
        Assert.Equal("1000.00", satirlar[0][3]);
        Assert.Equal("0.00", satirlar[0][5]);       // peşin satış borca yazılmadı
        Assert.Equal("0.00", satirlar[0][6]);
        Assert.Equal("Satış · Veresiye", satirlar[1][1]);
        Assert.Equal("2000.00", satirlar[1][5]);
        Assert.Equal("2000.00", satirlar[1][6]);
        Assert.Equal("Tahsilat · Kart", satirlar[2][1]);
        Assert.Equal("-500.00", satirlar[2][5]);
        Assert.Equal("1500.00", satirlar[2][6]);   // önceki + satır

        // Son satırın bakiyesi = belgenin TL bakiyesi; borç etkileri toplamı = bakiye.
        var tlBakiye = j.GetProperty("tutarlar")[0].GetProperty("deger").GetDecimal();
        Assert.Equal(tlBakiye, decimal.Parse(satirlar[^1][6], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(tlBakiye, satirlar.Sum(s => decimal.Parse(s[5], System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public async Task PD5_ekstre_has_ile_tahsilat_Has_yazar_TL_etkisi_sifir()
    {
        var k = YeniKiraci("PD5 has");
        var c = f.Ortam.Istemci(k);
        var th = await c.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/tahsilat", new
        {
            hasGrami = 1m, odemeTuru = "NAKIT", kurSnapshotId = k.KurId,
        });
        Assert.True(th.IsSuccessStatusCode, await th.Content.ReadAsStringAsync());

        var j = await (await c.GetAsync($"/api/v1/cikti/{BelgeTurleri.CariEkstre}/{k.CariId}")).Json();
        var satir = j.GetProperty("bolumler")[0].GetProperty("satirlar")[0].GetProperty("hucreler")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Equal("Tahsilat · Altın", satir[1]);   // ekrandaki adla aynı (UX-S31)
        // PARA-DEMO-2 (madde 9): has hücresi sunucuda biçimlenir — eksi U+2212, tr-TR, birim "has" (ekranla tek tip).
        Assert.Equal("\u22121,0000\u00a0has", satir[4]);   // has bakiyeye etkisi: müşteriye 1 g alacak
        Assert.Equal("0.00", satir[5]);

    }

    [Fact]
    public void PD_sozluk_TR_ve_EN_dolu_ve_farkli()
    {
        foreach (var p in typeof(ParaDemoMetinleri).GetProperties())
        {
            var tr = (string?)p.GetValue(ParaDemoSozlugu.Tr);
            var en = (string?)p.GetValue(ParaDemoSozlugu.En);
            Assert.False(string.IsNullOrWhiteSpace(tr), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(en), p.Name);
        }
        Assert.DoesNotContain("Borcu azaltır", ParaDemoSozlugu.Tr.TahsilatIpucu);
        Assert.Null(ParaDemoSozlugu.Tr.OdemeAdi("SATIS", 1m, 0m, "BILINMEYEN"));
        Assert.Equal("Altın", ParaDemoSozlugu.Tr.OdemeAdi("TAHSILAT", 0m, 1m, "NAKIT"));
    }
}

/// <summary>
/// PAKET PARA-DEMO (madde 3) — takas artık satıcı kimliği ister (VUK md.234, fail-closed).
/// Takası başka bir kural için kuran mevcut testler bu satıcıyı verir: MÜKELLEF satıcıda
/// gider pusulası düzenlenmez ve takas satırının sayısal alanları (TL, has, ölçü) DEĞİŞMEZ —
/// testlerin sınadığı kural etkilenmez (yalnız açıklamaya satıcı fatura no eklenir).
/// </summary>
public static class ParaDemoTestVerisi
{
    public static MHurda.HurdaSaticiDto TakasSaticisi => new()
    {
        SaticiTipi = "MUKELLEF", TedarikciFaturaNo = "TEST-TAKAS-1",
    };

    /// <summary>HTTP gövdeleri için aynı satıcı (camelCase).</summary>
    public static object TakasSaticisiJson => new { saticiTipi = "MUKELLEF", tedarikciFaturaNo = "TEST-TAKAS-1" };
}
