using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİŞ OKUMA UCU — paket 1 Faz A [DB]
//
// FİŞ İÇİN AYRI TABLO YOKTUR: fiş = aynı fis_id'yi taşıyan hareket kümesidir.
// Bugüne kadar bu kümeyi OKUYAN uç yoktu; kesilen fiş bir daha görüntülenemiyordu.
//
// ⚠ Ödeme bacakları fişin İLK hareketine bağlanır (SatisModulu.SatisFisi);
//   sorgu fişin TÜM hareket kimlikleri üzerinden kurulur.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class FisOkumaTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Servis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static JsonElement Oku(object cevap) =>
        JsonDocument.Parse(JsonSerializer.Serialize(cevap)).RootElement;

    /// <summary>3 kalemli bir fiş + iki ödeme bacağı yazar; fis_id döner.</summary>
    private static string FisYaz(KuyumcuDbContext kurulum, Kiraci k)
    {
        var fisId = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        var ilk = "";
        foreach (var tl in new[] { 1_000m, 2_500m, 500m })
        {
            var id = Kimlik.Ulid();
            if (ilk == "") ilk = id;
            kurulum.Hareketler.Add(new Hareket
            {
                Id = id, TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now,
                TlTutari = tl, HasGrami = -1m, OdemeTuru = "KARMA", NakitTutar = 0m,
                VeresiyeTutar = 0m, FisId = fisId, KurSnapshotId = k.KurId, CariId = k.CariId,
                UrunId = k.UrunId, KullaniciId = k.KullaniciId, Olusturuldu = now,
            });
        }
        foreach (var (arac, tutar) in new[] { ("NAKIT", 2_000m), ("KART", 2_000m) })
            kurulum.KasaHareketleri.Add(new KasaHareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId, HareketId = ilk,
                Tutar = tutar, ParaBirimi = "TRY", OdemeTuru = arac, Tarih = now, Olusturuldu = now,
            });
        kurulum.SaveChanges();
        return fisId;
    }

    [Fact]
    public async Task Uc_kalemli_fis_okunur_bacaklar_AYRI_satir_gelir_toplamlar_defterle_birebir()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 fiş okuma");
        var fisId = FisYaz(kurulum, k);

        using var db = vt.Olustur(k);
        var y = Oku(await Servis(db).FisOku(fisId, k.Islem));

        Assert.Equal(3, y.GetProperty("kalemSayisi").GetInt32());
        Assert.Equal(3, y.GetProperty("kalemler").GetArrayLength());
        Assert.Equal(4_000m, y.GetProperty("toplamTl").GetDecimal());
        Assert.Equal(k.CariId, y.GetProperty("cariId").GetString());

        var bacaklar = y.GetProperty("bacaklar").EnumerateArray().ToList();
        Assert.Equal(2, bacaklar.Count);
        Assert.Contains(bacaklar, b => b.GetProperty("OdemeTuru").GetString() == "NAKIT");
        Assert.Contains(bacaklar, b => b.GetProperty("OdemeTuru").GetString() == "KART");
        Assert.Equal(4_000m, bacaklar.Sum(b => b.GetProperty("Tutar").GetDecimal()));

        // Kur kaydı fişle birlikte döner (belge bunu tüketir).
        Assert.Equal(k.KurId, y.GetProperty("kur").GetProperty("Id").GetString());
    }

    /// <summary>Mehmet K1-5: yanıt cari ADI taşır; telefon/adres/vergi no DÖNMEZ.</summary>
    [Fact]
    public async Task Fis_yaniti_cari_adini_tasir_ama_kisisel_iletisim_bilgisi_TASIMAZ()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 fiş cari");
        var fisId = FisYaz(kurulum, k);

        using var db = vt.Olustur(k);
        var ham = JsonSerializer.Serialize(await Servis(db).FisOku(fisId, k.Islem));

        Assert.Contains("cariAd", ham);
        Assert.DoesNotContain("telefon", ham, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vergiNo", ham, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adres", ham, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A kiracısının işlemiyle B'nin fis_id'si istenirse 404.
    /// ⚠ Bağlam FİLTRESİZ kurulur: global sorgu filtresi devredeyken bu testin
    /// mutasyonu (TenantId şartını kaldırmak) hiçbir şey yakalamazdı.
    /// </summary>
    [Fact]
    public async Task Yabanci_kiracinin_fis_id_si_404()
    {
        using var kurulum = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kurulum, "PAKET1 fiş A");
        var b = TestKurulum.KiraciKur(kurulum, "PAKET1 fiş B");
        var fisId = FisYaz(kurulum, b);

        using var filtresiz = vt.OlusturSistem();
        await Assert.ThrowsAsync<NotFoundException>(
            () => Servis(filtresiz).FisOku(fisId, a.Islem));
    }

    [Fact]
    public async Task Olmayan_fis_id_404()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 fiş yok");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<NotFoundException>(
            () => Servis(db).FisOku(Kimlik.Ulid(), k.Islem));
    }

    [Fact]
    public async Task Faturalanmis_fisin_fatura_numarasi_yanitta_gorunur()
    {
        using var kurulum = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kurulum, "PAKET1 fiş fatura");
        var fisId = FisYaz(kurulum, k);
        foreach (var h in kurulum.Hareketler.Where(h => h.FisId == fisId).ToList())
            h.FaturaNo = "FTR-2026-000042";
        kurulum.SaveChanges();

        using var db = vt.Olustur(k);
        var y = Oku(await Servis(db).FisOku(fisId, k.Islem));
        Assert.Equal("FTR-2026-000042", y.GetProperty("faturaNo").GetString());
    }
}
