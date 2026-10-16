using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MCari = Kuyumcu.Api.Modules.Cari;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MSiparis = Kuyumcu.Api.Modules.Siparis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET EKSIK-B1-YAZDIR — B1-3 (cari ekstresi yazdır) · B1-4 (emanet belgesi yazdır)
// · B1-5 (siparişe kapora + usta) · B1-6 (süreç şeridi).
//
// Bu dosya: SAF kurallar + VERİTABANI (kapora sunucu kuralları, hasım denemeleri,
// satışta düşüm, geri verme = 48-A storno). Ekran testleri: EksikB1YazdirEkranTestleri.cs.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EksikB1YazdirSafTestleri
{
    [Theory]
    [InlineData("TASLAK", 0)]
    [InlineData("ONAYLANDI", 0)]
    [InlineData("ATOLYEDE", 1)]
    [InlineData("HAZIR", 2)]
    [InlineData("TESLIM_EDILDI", 3)]
    public void B16_durum_seritte_dogru_asamaya_duser(string durum, int beklenen) =>
        Assert.Equal(beklenen, Kuyumcu.Web.Pages.SiparisDetay.AdimSirasi(durum));

    [Theory]
    [InlineData("IPTAL")]
    [InlineData("BILINMEYEN")]
    [InlineData(null)]
    public void B16_iptal_ve_bilinmeyen_durumda_serit_cizilmez(string? durum) =>
        Assert.Null(Kuyumcu.Web.Pages.SiparisDetay.AdimSirasi(durum));

    [Theory]
    [InlineData("/emanet", "/emanet")]
    [InlineData("/siparisler/01ABC", "/siparisler/01ABC")]
    [InlineData("//kotu.example.com", null)]
    [InlineData("/\\kotu.example.com", null)]
    [InlineData("https://kotu.example.com", null)]
    [InlineData("/x:y", null)]
    [InlineData("emanet", null)]
    [InlineData("/", null)]
    [InlineData(null, null)]
    public void B14_cikti_donus_yolu_yalniz_uygulama_ici(string? girdi, string? beklenen) =>
        Assert.Equal(beklenen, Kuyumcu.Web.Pages.Cikti.GuvenliDonus(girdi));

    [Fact]
    public void B15_kapora_kilidi_iptal_teslim_canli_kapora()
    {
        Assert.Null(MSiparis.SiparisServisi.KaporaKilidi(SiparisDurumu.Taslak, null));
        Assert.Null(MSiparis.SiparisServisi.KaporaKilidi(SiparisDurumu.Hazir, null));
        Assert.Contains("İptal", MSiparis.SiparisServisi.KaporaKilidi(SiparisDurumu.Iptal, null));
        Assert.Contains("Teslim", MSiparis.SiparisServisi.KaporaKilidi(SiparisDurumu.TeslimEdildi, null));
        var canli = new MSiparis.SiparisServisi.KaporaBilgisi(100m, DateTime.UtcNow, "k", Stornolu: false);
        Assert.Contains("zaten alınmış", MSiparis.SiparisServisi.KaporaKilidi(SiparisDurumu.Onaylandi, canli));
        // Stornolanmış kapora "alınmış" sayılmaz → doğrusu yeniden alınabilir.
        Assert.Null(MSiparis.SiparisServisi.KaporaKilidi(SiparisDurumu.Onaylandi, canli with { Stornolu = true }));
    }
}

[Collection("veritabani")]
public sealed class EksikB1YazdirTestleri(TestVeritabani vt)
{
    private static MCari.CariServisi Cari(KuyumcuDbContext db) =>
        new(db, NullLogger<MCari.CariServisi>.Instance, new DenetimServisi(db, new HttpContextAccessor()));

    private static MSiparis.SiparisServisi Siparis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSiparis.SiparisServisi>.Instance, Cari(db));

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MSatis.StornoServisi Storno(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new HttpContextAccessor()), NullLogger<MSatis.StornoServisi>.Instance);

    private static JsonElement J(object yanit) =>
        JsonSerializer.SerializeToElement(yanit, yanit.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private Kiraci Kur(string ad)
    {
        using var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, ad, stokAdet: 5m);
        sistem.SaveChanges();
        return k;
    }

    private static async Task<string> SiparisAc(KuyumcuDbContext db, Kiraci k, string tur = SiparisTuru.MusteriSiparisi,
        bool urunlu = false)
    {
        var y = J(await Siparis(db).Olustur(new MSiparis.SiparisOlusturDto
        {
            Tur = tur, CariId = k.CariId, SubeId = k.SubeId, Aciklama = "bilezik",
            Kalemler = [urunlu
                ? new MSiparis.SiparisKalemDto { UrunId = k.UrunId, Adet = 1m }
                : new MSiparis.SiparisKalemDto { Tanim = "bilezik", Adet = 1m }],
        }, k.Islem));
        return y.GetProperty("id").GetString()!;
    }

    private static MSiparis.SiparisKaporaDto KaporaGovde(Kiraci k, decimal tutar = 2_000m, string odeme = "NAKIT") => new()
    {
        TlTutari = tutar, OdemeTuru = odeme, KasaId = k.KasaId, KurSnapshotId = k.KurId,
    };

    // ── B1-5: kapora siparişe BAĞLANIR, kart defterden okur ────────────────────

    [Fact]
    public async Task B15_kapora_siparise_baglanir_detay_tutari_ve_kasayi_defterden_verir()
    {
        var k = Kur("PAKETEKSIK-B1-TEST-K1");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var kasaOnce = TestKurulum.KasaBakiye(db, k.KasaId);

        var cevap = J(await Siparis(db).Kapora(id, KaporaGovde(k), k.Islem));
        var hareketId = cevap.GetProperty("kaporaHareketId").GetString();

        using var kontrol = vt.OlusturSistem();
        var s = await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Equal(hareketId, s.KaporaHareketId);                                   // sipariş ↔ defter bağı
        var h = await kontrol.Hareketler.AsNoTracking().FirstAsync(x => x.Id == hareketId);
        Assert.Equal("TAHSILAT", h.Tur);
        Assert.Equal(k.CariId, h.CariId);                                              // siparişin carisi
        Assert.Equal(kasaOnce + 2_000m, TestKurulum.KasaBakiye(kontrol, k.KasaId));     // para kasaya girdi
        Assert.Equal(-2_000m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);            // müşteri alacaklı

        var d = J(await Siparis(kontrol).Detay(id, k.Islem));
        Assert.Equal(2_000m, d.GetProperty("kaporaTutari").GetDecimal());
        Assert.Equal(k.KasaId, d.GetProperty("kaporaKasaId").GetString());
        Assert.False(d.GetProperty("kaporaStornolu").GetBoolean());
        Assert.False(d.GetProperty("kaporaAlinabilir").GetBoolean());
        Assert.Contains("zaten alınmış", d.GetProperty("kaporaKilitSebebi").GetString());
        // UX-S27: POST yanıtı da güncel kartı taşır.
        Assert.Equal(2_000m, cevap.GetProperty("siparis").GetProperty("kaporaTutari").GetDecimal());
    }

    // ── HASIM: çift kapora · iptal · teslim · veresiye · sıfır/eksi · başka kiracı ─

    [Fact]
    public async Task B15_hasim_cift_kapora_409_ve_defter_tek_satir()
    {
        var k = Kur("PAKETEKSIK-B1-TEST-K2");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        await Siparis(db).Kapora(id, KaporaGovde(k), k.Islem);

        await Assert.ThrowsAsync<ConflictException>(() => Siparis(db).Kapora(id, KaporaGovde(k, 500m), k.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(1, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
    }

    [Fact]
    public async Task B15_hasim_iptal_siparise_kapora_409_defter_bos()
    {
        var k = Kur("PAKETEKSIK-B1-TEST-K3");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        await Siparis(db).Gecis(id, new MSiparis.SiparisGecisDto
        {
            YeniDurum = SiparisDurumu.Iptal, Gerekce = "Müşteri vazgeçti, sipariş kapandı",
        }, k.Islem);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => Siparis(db).Kapora(id, KaporaGovde(k), k.Islem));
        Assert.Contains("İptal", ex.Message);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
    }

    // Tur 2 (doğrulayıcı KRİTİK bulgusu): Detay'ın kilit sebebi istek dilinde döner;
    // EN istekte Türkçe sızmaz (ekran #sipd-kapora-kilit bunu ham basar).
    [Fact]
    public async Task B15_EN_istekte_detay_kilit_sebebi_ingilizce()
    {
        var k = Kur("PAKETEKSIK-B1-TEST-K3E");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        await Siparis(db).Gecis(id, new MSiparis.SiparisGecisDto
        {
            YeniDurum = SiparisDurumu.Iptal, Gerekce = "Müşteri vazgeçti, sipariş kapandı",
        }, k.Islem);

        string? en, tr;
        using (Kuyumcu.Core.I18n.IstekDili.Kapsam("en"))
            en = JsonDocument.Parse(JsonSerializer.Serialize(await Siparis(db).Detay(id, k.Islem)))
                .RootElement.GetProperty("kaporaKilitSebebi").GetString();
        using (Kuyumcu.Core.I18n.IstekDili.Kapsam("tr"))
            tr = JsonDocument.Parse(JsonSerializer.Serialize(await Siparis(db).Detay(id, k.Islem)))
                .RootElement.GetProperty("kaporaKilitSebebi").GetString();
        Assert.Equal("A deposit cannot be taken on a cancelled order.", en);
        Assert.Equal("İptal edilmiş siparişe kapora alınamaz.", tr);
    }

    [Theory]
    [InlineData("VERESIYE")]
    [InlineData("ALTIN")]
    [InlineData("nakit-degil")]
    public async Task B15_hasim_kapora_gercek_para_degilse_400(string odeme)
    {
        var k = Kur("PAKETEKSIK-B1-TEST-K4-" + odeme[..3]);
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Siparis(db).Kapora(id, KaporaGovde(k, 100m, odeme), k.Islem));
        Assert.Contains("açık hesaba kapora yazılmaz", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task B15_hasim_sifir_ve_eksi_kapora_400(int tutar)
    {
        var k = Kur("PAKETEKSIK-B1-TEST-K5" + (tutar < 0 ? "E" : "S"));
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        await Assert.ThrowsAsync<BadRequestException>(() => Siparis(db).Kapora(id, KaporaGovde(k, tutar), k.Islem));
    }

    [Fact]
    public async Task B15_hasim_baska_kiracinin_siparisine_kapora_404()
    {
        var a = Kur("PAKETEKSIK-B1-TEST-K6A");
        var b = Kur("PAKETEKSIK-B1-TEST-K6B");
        using var db = vt.OlusturSistem();
        var idA = await SiparisAc(db, a);
        await Assert.ThrowsAsync<NotFoundException>(() => Siparis(db).Kapora(idA, KaporaGovde(b), b.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Null((await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == idA)).KaporaHareketId);
    }

    // ── İptalde iade yolu: 48-A storno → kart "geri verildi", yeniden alınabilir ──

    [Fact]
    public async Task B15_kapora_geri_verilince_kasa_ve_cari_doner_dogrusu_yeniden_alinir()
    {
        var k = Kur("PAKETEKSIK-B1-TEST-K7");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k);
        var kasaOnce = TestKurulum.KasaBakiye(db, k.KasaId);
        var hareketId = J(await Siparis(db).Kapora(id, KaporaGovde(k), k.Islem)).GetProperty("kaporaHareketId").GetString()!;

        await Storno(db).Storno(hareketId, new MSatis.StornoDto
        {
            Gerekce = "Sipariş iptal edildi, kapora müşteriye geri verildi", KurSnapshotId = k.KurId, KasaId = k.KasaId,
        }, k.Islem);

        using (var kontrol = vt.OlusturSistem())
        {
            Assert.Equal(kasaOnce, TestKurulum.KasaBakiye(kontrol, k.KasaId));
            Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
            var d = J(await Siparis(kontrol).Detay(id, k.Islem));
            Assert.True(d.GetProperty("kaporaStornolu").GetBoolean());
            Assert.True(d.GetProperty("kaporaAlinabilir").GetBoolean());
        }

        // Doğru tutar yeniden alınabilir; bağ YENİ satıra geçer.
        var yeni = J(await Siparis(db).Kapora(id, KaporaGovde(k, 1_500m), k.Islem)).GetProperty("kaporaHareketId").GetString();
        Assert.NotEqual(hareketId, yeni);
        using var son = vt.OlusturSistem();
        Assert.Equal(yeni, (await son.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).KaporaHareketId);
        Assert.Equal(-1_500m, TestKurulum.CariBakiye(son, k.CariId).Tl);
    }

    // ── Satışta düşüm: teslimde kalan nakit + kapora kadarı açık hesap → cari 0 ──

    [Fact]
    public async Task B15_satista_kapora_dusulur_cari_bakiye_sifirlanir()
    {
        var k = Kur("PAKETEKSIK-B1-TEST-K8");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k, urunlu: true);
        await Siparis(db).Kapora(id, KaporaGovde(k, 2_000m), k.Islem);
        await Siparis(db).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Onaylandi }, k.Islem);
        await Siparis(db).Gecis(id, new MSiparis.SiparisGecisDto { YeniDurum = SiparisDurumu.Hazir }, k.Islem);
        var kalemId = await db.SiparisKalemleri.AsNoTracking().Where(x => x.SiparisId == id).Select(x => x.Id).FirstAsync();

        using var db2 = vt.OlusturSistem();
        await Satis(db2).SiparisiSatisaCevir(id, new MSatis.SiparisSatisaCevirDto
        {
            KurSnapshotId = k.KurId, VadeGun = 0,
            Kalemler = [new MSatis.SiparisSatisaCevirKalemDto { SiparisKalemId = kalemId, BirimFiyatTl = 10_000m }],
            Odemeler =
            [
                new MSatis.OdemeBacagiDto { Arac = "NAKIT", TutarTl = 8_000m, KasaId = k.KasaId },
                new MSatis.OdemeBacagiDto { Arac = "VERESIYE", TutarTl = 2_000m },
            ],
        }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(SiparisDurumu.TeslimEdildi, (await kontrol.Siparisler.AsNoTracking().FirstAsync(x => x.Id == id)).Durum);
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);   // kapora borçtan düştü
        var d = J(await Siparis(kontrol).Detay(id, k.Islem));
        Assert.False(d.GetProperty("kaporaAlinabilir").GetBoolean());    // teslim edilmiş → kapora yok
        Assert.Contains("Teslim", d.GetProperty("kaporaKilitSebebi").GetString());
    }

    // ── Usta: kartta ADIYLA ──────────────────────────────────────────────────

    [Fact]
    public async Task B15_usta_detayda_adiyla_gelir()
    {
        var k = Kur("PAKETEKSIK-B1-TEST-K9");
        using var db = vt.OlusturSistem();
        var id = await SiparisAc(db, k, SiparisTuru.Tamir);
        using (var sistem = vt.OlusturSistem())
        {
            var simdi = DateTime.UtcNow;
            sistem.Cariler.Add(new Cari
            {
                Id = "USTA-" + k.TenantId[..20], TenantId = k.TenantId, Tur = "TEDARIKCI", Ad = "Usta Hasan",
                Aktif = true, Olusturuldu = simdi, Guncellendi = simdi,
            });
            sistem.SaveChanges();
        }
        var ustaId = "USTA-" + k.TenantId[..20];
        var d0 = J(await Siparis(db).Detay(id, k.Islem));
        await Siparis(db).Guncelle(id, JsonDocument.Parse(
            "{\"ustaCariId\":\"" + ustaId + "\",\"guncellendi\":" + JsonSerializer.Serialize(d0.GetProperty("guncellendi").GetDateTime()) + "}").RootElement, k.Islem);

        using var kontrol = vt.OlusturSistem();
        var d = J(await Siparis(kontrol).Detay(id, k.Islem));
        Assert.Equal(ustaId, d.GetProperty("ustaCariId").GetString());
        Assert.Equal("Usta Hasan", d.GetProperty("ustaAd").GetString());
    }
}
