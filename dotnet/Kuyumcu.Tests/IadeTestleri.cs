using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MKasa = Kuyumcu.Api.Modules.Kasa;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// İADE — gelen (satış iadesi) ve çıkan (alış iadesi)
//
// İade bağımsız bir işlem DEĞİLDİR; satışın aynasıdır. Orijinal işlem
// bulunmadan iade alınabilseydi, hiç satılmamış mal "iade" diye stoğa
// sokulur ve kasadan para çıkarılabilirdi.
//
// ⚠ MALİYET: iade edilen mal ÇIKTIĞI maliyetle geri girer. Yeni parti
// açılsaydı altın yükselirken satılmamış malda sahte kâr/zarar doğardı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class IadeTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static MKasa.KasaServisi Kasa(KuyumcuDbContext db) =>
        new(db, NullLogger<MKasa.KasaServisi>.Instance);

    /// <summary>Maliyetli stok kurar: 10 adet × 1.000 TL alış.</summary>
    private static async Task<string> AlisYap(
        KuyumcuDbContext db, Kiraci k, decimal adet = 10m, decimal birim = 1_000m)
    {
        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = adet, BirimFiyatTl = birim,
            OdemeTuru = "HAVALE", OdenenTl = adet * birim, KasaId = k.KasaId,
            KurSnapshotId = k.KurId,
        }, k.Islem);

        using var kontrol = new KuyumcuDbContext(
            new DbContextOptionsBuilder<KuyumcuDbContext>().Options, null!, null!);
        return "";
    }

    private string SonHareketId(Kiraci k, string tur)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == k.TenantId && h.Tur == tur)
            .OrderByDescending(h => h.Olusturuldu).ThenByDescending(h => h.Id)
            .First().Id;
    }

    private Hareket Hareket(Kiraci k, string tur)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Hareketler.AsNoTracking()
            .Single(h => h.TenantId == k.TenantId && h.Tur == tur);
    }

    private decimal StokAdet(Kiraci k)
    {
        using var kontrol = vt.OlusturSistem();
        return kontrol.Stoklar.AsNoTracking()
            .Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet;
    }

    private static MSatis.SatisOlusturDto SatisDto(Kiraci k, decimal adet, decimal birim, string odeme = "KART") => new()
    {
        CariId = k.CariId, UrunId = k.UrunId, Adet = adet, BirimFiyatTl = birim,
        OdemeTuru = odeme, KasaId = odeme == "VERESIYE" ? null : k.KasaId,
        KurSnapshotId = k.KurId,
    };

    private static MSatis.IadeDto IadeDto(string kaynak, decimal adet, string odeme, Kiraci k) => new()
    {
        KaynakHareketId = kaynak, Adet = adet, OdemeTuru = odeme,
        KasaId = odeme == "VERESIYE" ? null : k.KasaId, KurSnapshotId = k.KurId,
    };

    // ── Satış iadesi ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Satis_iadesi_MALI_stoga_PARAYI_kasadan_geri_verir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade temel", stokAdet: 10m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(SatisDto(k, 2m, 5_000m), k.Islem);
        Assert.Equal(8m, StokAdet(k));
        var satisId = SonHareketId(k, "SATIS");

        using (var oku = vt.OlusturSistem())
        {
            var kasaOnce = oku.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye;
            Assert.Equal(TestKurulum.KasaAcilis + 10_000m, kasaOnce);
        }

        await Satis(db).SatisIade(IadeDto(satisId, 2m, "KART", k), k.Islem);

        Assert.Equal(10m, StokAdet(k));                       // mal geri geldi
        var iade = Hareket(k, "SATIS_IADE");
        Assert.Equal(-10_000m, iade.TlTutari);                // para geri gitti
        Assert.Equal(satisId, iade.IadeKaynakHareketId);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis,
            kontrol.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye);
    }

    [Fact]
    public async Task Kismi_iadede_tutar_ORANTILIDIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade kısmi", stokAdet: 10m);
        using var db = vt.Olustur(k);

        await Satis(db).Satis(SatisDto(k, 4m, 2_500m), k.Islem);   // 10.000 TL
        var satisId = SonHareketId(k, "SATIS");

        await Satis(db).SatisIade(IadeDto(satisId, 1m, "KART", k), k.Islem);

        var iade = Hareket(k, "SATIS_IADE");
        Assert.Equal(-2_500m, iade.TlTutari);   // 4'te 1'i
        Assert.Equal(7m, StokAdet(k));          // 10 − 4 + 1
    }

    [Fact]
    public async Task Iade_tutari_orantiliyi_ASAMAZ()
    {
        // Aşabilseydi iade, kasadan para çıkarmanın denetimsiz bir yolu olurdu.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade fazla tutar", stokAdet: 10m);
        using var db = vt.Olustur(k);
        await Satis(db).Satis(SatisDto(k, 2m, 5_000m), k.Islem);
        var satisId = SonHareketId(k, "SATIS");

        var dto = IadeDto(satisId, 1m, "KART", k);
        dto.IadeTutariTl = 9_000m;   // orantılı 5.000

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).SatisIade(dto, k.Islem));
        Assert.Contains("aşamaz", hata.Message);
    }

    [Fact]
    public async Task Satilandan_FAZLASI_iade_edilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade fazla adet", stokAdet: 10m);
        using var db = vt.Olustur(k);
        await Satis(db).Satis(SatisDto(k, 2m, 5_000m), k.Islem);
        var satisId = SonHareketId(k, "SATIS");

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).SatisIade(IadeDto(satisId, 3m, "KART", k), k.Islem));
        Assert.Contains("kalan iade hakkını aşıyor", hata.Message);
    }

    [Fact]
    public async Task Ayni_satis_IKI_KEZ_tam_iade_edilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade çift", stokAdet: 10m);
        using var db = vt.Olustur(k);
        await Satis(db).Satis(SatisDto(k, 2m, 5_000m), k.Islem);
        var satisId = SonHareketId(k, "SATIS");

        await Satis(db).SatisIade(IadeDto(satisId, 2m, "KART", k), k.Islem);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).SatisIade(IadeDto(satisId, 1m, "KART", k), k.Islem));
        Assert.Contains("tamamı zaten iade edilmiş", hata.Message);
    }

    [Fact]
    public async Task Parca_parca_iade_kalan_hakki_DOGRU_azaltir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade parçalı", stokAdet: 10m);
        using var db = vt.Olustur(k);
        await Satis(db).Satis(SatisDto(k, 3m, 1_000m), k.Islem);
        var satisId = SonHareketId(k, "SATIS");

        await Satis(db).SatisIade(IadeDto(satisId, 1m, "KART", k), k.Islem);
        await Satis(db).SatisIade(IadeDto(satisId, 1m, "KART", k), k.Islem);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).SatisIade(IadeDto(satisId, 2m, "KART", k), k.Islem));
        Assert.Contains("kalan 1", hata.Message);
    }

    [Fact]
    public async Task Satis_OLMAYAN_hareket_iade_edilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade yanlış tür", stokAdet: 10m);
        using var db = vt.Olustur(k);
        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 1_000m,
            OdemeTuru = "HAVALE", OdenenTl = 1_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        var alisId = SonHareketId(k, "ALIS");

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).SatisIade(IadeDto(alisId, 1m, "KART", k), k.Islem));
        Assert.Contains("iade edilemez", hata.Message);
    }

    [Fact]
    public async Task Veresiye_satisin_iadesi_MUSTERI_BORCUNU_azaltir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade veresiye", stokAdet: 10m);
        using var db = vt.Olustur(k);
        await Satis(db).Satis(SatisDto(k, 2m, 5_000m, "VERESIYE"), k.Islem);
        var satisId = SonHareketId(k, "SATIS");

        using (var oku = vt.OlusturSistem())
            Assert.Equal(10_000m, TestKurulum.CariBakiye(oku, k.CariId).Tl);

        await Satis(db).SatisIade(IadeDto(satisId, 2m, "VERESIYE", k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(0m, TestKurulum.CariBakiye(kontrol, k.CariId).Tl);
    }

    // ── Maliyet ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Iade_maliyeti_CIKTIGI_fiyatla_geri_koyar()
    {
        // 10 adet × 1.000 TL alındı, 2 adet satıldı, 2 adet iade edildi.
        // Parti kalanı BAŞLANGIÇTAKİ 10'a dönmeli ve yeni parti AÇILMAMALI.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade maliyet", stokAdet: 0m);
        using var db = vt.Olustur(k);
        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 10m, BirimFiyatTl = 1_000m,
            OdemeTuru = "HAVALE", OdenenTl = 10_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

        await Satis(db).Satis(SatisDto(k, 2m, 5_000m), k.Islem);
        var satisId = SonHareketId(k, "SATIS");

        using (var ara = vt.OlusturSistem())
            Assert.Equal(8m, ara.Set<StokPartisi>().AsNoTracking()
                .Single(x => x.TenantId == k.TenantId).AdetKalan);

        await Satis(db).SatisIade(IadeDto(satisId, 2m, "KART", k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var partiler = kontrol.Set<StokPartisi>().AsNoTracking()
            .Where(x => x.TenantId == k.TenantId).ToList();
        Assert.Single(partiler);                    // YENİ parti açılmadı
        Assert.Equal(10m, partiler[0].AdetKalan);   // çıktığı yere geri kondu
    }

    [Fact]
    public async Task Iade_tuketim_satirini_SILMEZ_ters_kayit_ekler()
    {
        // Silinseydi "bu parti ne zaman tüketildi, ne zaman geri geldi"
        // tarihçesi yok olurdu (append-only defter).
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade ters kayıt", stokAdet: 0m);
        using var db = vt.Olustur(k);
        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 5m, BirimFiyatTl = 1_000m,
            OdemeTuru = "HAVALE", OdenenTl = 5_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        await Satis(db).Satis(SatisDto(k, 2m, 5_000m), k.Islem);
        await Satis(db).SatisIade(IadeDto(SonHareketId(k, "SATIS"), 2m, "KART", k), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var tuketimler = kontrol.Set<StokPartiTuketimi>().AsNoTracking()
            .Where(t => t.TenantId == k.TenantId).ToList();
        Assert.Equal(2, tuketimler.Count);                       // biri +2, biri −2
        Assert.Contains(tuketimler, t => t.Adet == 2m);
        Assert.Contains(tuketimler, t => t.Adet == -2m);
        Assert.Equal(0m, tuketimler.Sum(t => t.Adet));           // net sıfır
    }

    // ── Kapılar ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Kapanmis_gune_iade_yazilamaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade gün kilidi", stokAdet: 10m);
        using var db = vt.Olustur(k);
        await Satis(db).Satis(SatisDto(k, 1m, 1_000m), k.Islem);
        var satisId = SonHareketId(k, "SATIS");

        // PARA-DEMO (madde 1): satış KARTLA — kart çekmecede sayılmaz; sayılan nakit = açılış nakdi.
        await Kasa(db).GunKapat(k.KasaId, new MKasa.GunKapanisiDto
        {
            SayilanNakit = TestKurulum.KasaAcilis,
        }, k.Islem);

        await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).SatisIade(IadeDto(satisId, 1m, "KART", k), k.Islem));
    }

    [Fact]
    public async Task Nakit_iade_TEVSIK_kapisina_tabidir()
    {
        // İadede para kasadan ÇIKAR; tediye de hadde tabidir.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "İade tevsik", stokAdet: 10m);
        using var db = vt.Olustur(k);
        await Satis(db).Satis(SatisDto(k, 1m, 40_000m), k.Islem);   // kartla satıldı
        var satisId = SonHareketId(k, "SATIS");

        // 40.000 TL nakit iade: işlem tutarı haddi aşıyor → §4.1.2 reddeder.
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).SatisIade(IadeDto(satisId, 1m, "NAKIT", k), k.Islem));
        Assert.Contains("nakit sınırı", hata.Message);
    }

    // ── Alış iadesi ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Alis_iadesi_MALI_stoktan_cikarir_PARAYI_kasaya_koyar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Alış iadesi", stokAdet: 0m);
        using var db = vt.Olustur(k);
        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 5m, BirimFiyatTl = 1_000m,
            OdemeTuru = "HAVALE", OdenenTl = 5_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        Assert.Equal(5m, StokAdet(k));
        var alisId = SonHareketId(k, "ALIS");

        await Satis(db).AlisIade(IadeDto(alisId, 2m, "HAVALE", k), k.Islem);

        Assert.Equal(3m, StokAdet(k));                    // mal gitti
        var iade = Hareket(k, "ALIS_IADE");
        Assert.Equal(2_000m, iade.TlTutari);              // para geldi
        Assert.True(iade.HasGrami < 0m);
    }

    [Fact]
    public async Task Elimizde_olmayan_mal_tedarikciye_iade_edilemez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Alış iadesi stoksuz", stokAdet: 0m);
        using var db = vt.Olustur(k);
        await Satis(db).Alis(new MSatis.AlisOlusturDto
        {
            CariId = k.CariId, UrunId = k.UrunId, Adet = 2m, BirimFiyatTl = 1_000m,
            OdemeTuru = "HAVALE", OdenenTl = 2_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);
        var alisId = SonHareketId(k, "ALIS");

        // Aldığımız 2 adedi sattık; elimizde kalmadı.
        await Satis(db).Satis(SatisDto(k, 2m, 5_000m), k.Islem);

        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => Satis(db).AlisIade(IadeDto(alisId, 2m, "HAVALE", k), k.Islem));
        // ⚠ PAKET P-21 (Y-23) ile MESAJ DEĞİŞTİ, KURAL DEĞİŞMEDİ: kontrol artık
        //   BlokeKapisi.SatilabilirDogrula'dan geçer ve üç sayıyı da söyler
        //   (stok / rezerve / satılabilir) — UX-S30. Test SUSTURULMADI, kuralın
        //   yeni metnine hizalandı; hâlâ BadRequestException ve hâlâ yazma YOK.
        Assert.Contains("yetersiz satılabilir stok", hata.Message);
        Assert.Contains("Alış iadesi", hata.Message);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// İADE — EKRAN
// ═════════════════════════════════════════════════════════════════════════════

public sealed class IadeEkranTestleri : Bunit.BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Adaylar = """
        [{"id":"h1","tarih":"2026-09-15T09:00:00Z","tlTutari":10000,"urunId":"u1",
          "urunAdi":"Burma Bilezik","toplamAdet":4,"iadeEdilenAdet":1,"kalanAdet":3,"cariId":"c1"}]
        """;

    public IadeEkranTestleri()
    {
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Services.AddScoped<Kuyumcu.Web.Servisler.DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<Kuyumcu.Web.Servisler.OturumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.ArayuzModuServisi>();   // UX-3 Faz B
        Services.AddScoped<Kuyumcu.Web.Servisler.ApiIstemci>();
        Services.AddScoped<Kuyumcu.Web.Servisler.UyumServisi>();
        Services.AddScoped<Kuyumcu.Web.Servisler.CanliKurServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                var v when v.Contains("/islemler/iade-adaylari") => Adaylar,
                var v when v.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") =>
                    """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """
                    {"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,
                     "ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,
                     "belgeTurleri":["TC_KIMLIK","PASAPORT"]}
                    """,
                var v when v.Contains("/tevsik/gunluk") => """
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,
                     "bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}
                    """,
                // P-10: KART bacagi POS cihazi listesini okur.
                var p10c when p10c.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Islemler> IadeEkrani()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.FindAll("button").First(b => b.TextContent.Contains("Satış İadesi")).Click();
        e.WaitForElement("#iade-kaynak");
        istekler.Clear();
        return e;
    }

    /// <summary>
    /// İade DIŞINDAKİ her koşulu sağlar (cari + kasa). Böylece Kaydet'in açık
    /// mı kapalı mı olduğu YALNIZ iade kapısına bağlı kalır.
    ///
    /// ⚠ Bu ayrım olmadan test yanlış sebepten geçer: mutasyon iade kapısını
    /// kaldırdığında Kaydet yine kapalı kalır (kasa seçilmemiştir) ve test
    /// kırmızıya dönmez. 2026-09-15'te bu tam olarak yaşandı.
    /// </summary>
    private static void DigerKosullariSagla(Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
    {
        SeciciYardim.Sec(e, "islem-cari", "c1");
        if (e.FindAll("#islem-kasa").Count > 0) SeciciYardim.Sec(e, "islem-kasa", "ks1");   // SATIS-3: tek kasada seçici yok (otomatik)
    }

    private static AngleSharp.Dom.IElement Kaydet(
        Bunit.IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
        => e.FindAll("button").First(b => b.TextContent.Contains("Kaydet"));

    [Fact]
    public void Iade_sekmesi_adaylari_SUNUCUDAN_ceker()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.FindAll("button").First(b => b.TextContent.Contains("Satış İadesi")).Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Yol.Contains("/islemler/iade-adaylari?tur=SATIS")));
    }

    [Fact]
    public void Kaynak_secilmeden_KAYDET_kapali()
    {
        // İade bağımsız işlem değildir; kaynak yoksa tutar bile hesaplanamaz.
        var e = IadeEkrani();
        DigerKosullariSagla(e);

        Assert.True(Kaydet(e).HasAttribute("disabled"));
    }

    [Fact]
    public void Kaynak_secilince_KALAN_hak_ve_ORANTILI_tutar_gosterilir()
    {
        var e = IadeEkrani();
        SeciciYardim.Sec(e, "iade-kaynak", "h1");
        e.WaitForElement("#iade-adet");

        // kalan 3 adet; varsayılan iade adedi 3 → 10.000 × 3/4 = 7.500
        e.WaitForAssertion(() => Assert.Equal("3", e.Find("#iade-adet").GetAttribute("value")));
        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Tl(7500m), e.Markup));
    }

    [Fact]
    public void Kismi_adette_orantili_tutar_DUSER()
    {
        var e = IadeEkrani();
        SeciciYardim.Sec(e, "iade-kaynak", "h1");
        e.WaitForElement("#iade-adet");
        e.Find("#iade-adet").Input("1");

        e.WaitForAssertion(() => Assert.Contains(Kuyumcu.Web.Servisler.Bicim.Tl(2500m), e.Markup));   // 10.000 × 1/4
    }

    [Fact]
    public void Kalan_hakki_asan_adette_KAYDET_kapanir()
    {
        var e = IadeEkrani();
        DigerKosullariSagla(e);
        SeciciYardim.Sec(e, "iade-kaynak", "h1");
        e.WaitForElement("#iade-adet");

        // Kalan 3: bu adette Kaydet AÇIK olmalı — testin diğer koşullardan
        // değil, yalnız iade kapısından geçtiğini bu satır kanıtlar.
        // Adet değişimi @bind:after ile eşzamansız yeniden çizim tetikler;
        // düğmenin durumu beklenerek okunur (sabit uyku değil, koşul beklenir).
        e.Find("#iade-adet").Input("3");
        e.WaitForAssertion(() => Assert.False(Kaydet(e).HasAttribute("disabled")));

        e.Find("#iade-adet").Input("4");        // kalan hakkı aşıyor
        e.WaitForAssertion(() => Assert.True(Kaydet(e).HasAttribute("disabled")));
    }

    [Fact]
    public void Iade_dogru_uca_KAYNAK_ve_ADET_ile_gonderilir()
    {
        var e = IadeEkrani();
        DigerKosullariSagla(e);
        SeciciYardim.Sec(e, "iade-kaynak", "h1");
        e.WaitForElement("#iade-adet");
        e.Find("#iade-adet").Input("2");
        Kaydet(e).Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Yol.EndsWith("/islemler/satis-iade")));
        var istek = istekler.First(i => i.Metot == "POST");
        Assert.Equal("h1", Alan(istek.Govde, "kaynakHareketId"));
        Assert.Equal("2", Alan(istek.Govde, "adet"));
    }

    [Fact]
    public void Iade_odeme_secicisinde_ALTIN_YOKTUR()
    {
        // IadeDto yalnız NAKIT|KART|HAVALE|VERESIYE kabul eder.
        var e = IadeEkrani();
        var degerler = SeciciYardim.Secenekler(e, "islem-odeme")
            .Select(o => o.Deger()).ToList();
        Assert.DoesNotContain("ALTIN", degerler);
        Assert.Contains("VERESIYE", degerler);
    }
}
