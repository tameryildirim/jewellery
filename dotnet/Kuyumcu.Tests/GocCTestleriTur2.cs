using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Iceaktarim;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-C · TUR 2 — hasım bulgularının kapanışı
//  KRİTİK: içe aktarım açılış stoğu ADET birimli ürüne kesirli adet yazıyordu (#6 · K14).
//          → AcilisStokKaynagi.TopluDogrula birimi okur (kuru provada satır hatası),
//            AcilisYazici.StokYaz son savunma UrunAdetKapisi.
//  DÜŞÜK : ürün kartı içe aktarım kuru provası taş ≥ brüt / eksi tutar kuralını göstermiyordu
//          → UrunKaynagi.Dogrula UrunServisi.GocCKapisi'ni çağırır (tek kaynak).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GocCTur2SafTestleri
{
    private static Dictionary<string, string> Kart(string gram, string tas = "0", string? satis = null) => new()
    {
        ["tip"] = "ZIYNET", ["kod"] = "GT2-01", ["ad"] = "GocC tur2 ürün", ["ayarId"] = "K22",
        ["gramAgirlik"] = gram, ["tasAgirlik"] = tas, ["tasKokeni"] = tas == "0" ? "TAS_YOK" : "DOGAL", ["satisFiyati"] = satis ?? "",
    };

    private static string M(GirdiKodu kod) => GirdiSozlugu.Mesaj(new GirdiHatasi(kod), "tr");

    [Fact]
    public void GCT2_S01_urun_karti_provasi_tas_brute_esit_ya_da_buyukse_satir_hatasi_sozluk_metniyle()
    {
        var k = new UrunKaynagi();
        foreach (var tas in new[] { "5", "6.5" })
        {
            var s = k.Dogrula(Kart("5", tas));
            Assert.Null(s.Dto);
            Assert.Equal("ALAN_GECERSIZ", s.HataKodu);
            Assert.Equal(M(GirdiKodu.TasBrut), s.HataMesaji);
        }
        // Sınırın hemen altı geçerli (taş 4,99 < brüt 5).
        foreach (var gecerli in new[] { k.Dogrula(Kart("5", "4.99")), k.Dogrula(Kart("5")) })
            Assert.True(gecerli.Dto is not null, $"{gecerli.HataKodu}: {gecerli.HataMesaji}");
    }

    [Fact]
    public void GCT2_S02_urun_karti_provasi_eksi_satis_fiyati_satir_hatasi()
    {
        var s = new UrunKaynagi().Dogrula(Kart("5", "0", "-1"));
        Assert.Null(s.Dto);
        Assert.Equal("ALAN_GECERSIZ", s.HataKodu);
    }
}

[Collection("veritabani")]
public sealed class GocCTur2VtTestleri(TestVeritabani vt)
{
    private const string Gerekce = "GOC-C tur 2 açılış stoğu birim testi — yeterince uzun gerekçe";

    private static IceAktarimServisi Servis(KuyumcuDbContext db)
    {
        var denetim = TestKurulum.Denetim(db);
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance, denetim);
        var urun = new Kuyumcu.Api.Modules.Urun.UrunServisi(db, denetim);
        var kurulum = new Kuyumcu.Api.Modules.Kurulum.KurulumServisi(db, denetim,
            NullLogger<Kuyumcu.Api.Modules.Kurulum.KurulumServisi>.Instance, new ConfigurationBuilder().Build());
        return new IceAktarimServisi(db, denetim, cari, urun, null, kurulum);
    }

    private static IceAktarimProvaDto Dto(string csv) => new()
    {
        Kaynak = "acilis-stok", DosyaAdi = "acilis-stok-gct2.csv", Gerekce = Gerekce,
        IcerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv)),
    };

    private static string UrunEkle(KuyumcuDbContext db, string tenantId, string kod, string birim)
    {
        var now = DateTime.UtcNow; var id = Kimlik.Ulid();
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = tenantId, Tip = "ZIYNET", Kod = kod, Ad = $"GocC tur2 {kod}", AyarId = "K22",
            Milyem = 0.916m, GramAgirlik = 2m, TasAgirlik = 0m, HasGrami = 1.832m, Birim = birim,
            IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task GCT2_V01_acilis_stogu_provasi_ADET_birimde_kesir_SATIR_HATASI_GRAM_birimde_gecer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-C-TEST gct2 prova");
        using var db = vt.Olustur(k.TenantId);
        UrunEkle(db, k.TenantId, "GT2-ADET", "ADET");
        UrunEkle(db, k.TenantId, "GT2-GRAM", "GRAM");
        UrunEkle(db, k.TenantId, "GT2-TAM", "ADET");

        var p = await Servis(db).Prova(Dto("kod,adet,birimMaliyetTl\nGT2-ADET,1.5,100\nGT2-GRAM,1.5,100\nGT2-TAM,2,100\n"), k.Islem);
        var j = JsonDocument.Parse(JsonSerializer.Serialize(p)).RootElement;
        Assert.True(j.GetProperty("hataliSatir").GetInt32() == 1, j.GetProperty("ilkHatalar").GetRawText());
        var h = Assert.Single(j.GetProperty("ilkHatalar").EnumerateArray());
        Assert.Equal(2, h.GetProperty("satirNo").GetInt32());   // başlık 1. satır → ilk veri satırı 2
        Assert.Equal("ADET_GECERSIZ", h.GetProperty("hataKodu").GetString());
        Assert.Contains(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.SayiTam), "tr"), h.GetProperty("hataMesaji").GetString());
    }

    [Fact]
    public async Task GCT2_V02_StokYaz_son_savunma_ADET_birimde_kesirli_acilis_400_hicbir_satir_yazilmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETGOC-C-TEST gct2 yazici");
        using var db = vt.Olustur(k.TenantId);
        var uid = UrunEkle(db, k.TenantId, "GT2-SON", "ADET");
        var urun = await db.Urunler.SingleAsync(u => u.Id == uid);
        var cari = new Kuyumcu.Api.Modules.Cari.CariServisi(db, NullLogger<Kuyumcu.Api.Modules.Cari.CariServisi>.Instance,
            TestKurulum.Denetim(db));
        var yazici = new Kuyumcu.Api.Modules.Kurulum.AcilisYazici(db, cari);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => yazici.StokYaz(urun, k.SubeId, 1.5m, null,
            null!, "gerekçe", k.Islem, Kimlik.Ulid(), DateTime.UtcNow));
        Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.SayiTam), "tr"), ex.Message);
        await db.SaveChangesAsync();

        using var oku = vt.Olustur(k.TenantId);
        Assert.False(await oku.Stoklar.AnyAsync(x => x.UrunId == uid));
        Assert.False(await oku.Hareketler.AnyAsync(x => x.UrunId == uid));
    }
}
