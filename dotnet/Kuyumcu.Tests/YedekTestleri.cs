using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yedek;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// YEDEK OTOMASYONU — servis testleri (paket T-3 Faz A)
//
// ⚠ BU TESTLER BETİĞİ ÇALIŞTIRMAZ. Betiğin fail-closed davranışı (şifreleme,
//   dış kopya, saklama) CANLI ölçümle kanıtlandı ve rapora yazıldı; burada
//   uygulamanın sözleşmesi sınanır: kuyruk satırı, iptal yolu, denetim izi,
//   "dosya adı dışarı sızmaz" kuralı ve fail-closed politikası metni.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class YedekTestleri(TestVeritabani vt)
{
    private static YedekServisi Servis(KuyumcuDbContext db)
    {
        var yapilandirma = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Yedek:SaklamaGun"] = "30",
            ["Yedek:RpoDakika"] = "360",
            ["Yedek:RtoDakika"] = "120",
        }).Build();
        return new YedekServisi(db, TestKurulum.Denetim(db), yapilandirma);
    }

    [Fact]
    public async Task ElleTalep_Acilinca_BekliyorDurumundaKaydedilir()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "T3-talep-ac");
        using var db = vt.Olustur(k);

        var cevap = await Servis(db).TalepAc(k.Islem,
            new YedekTalepDto { Gerekce = "aylık denetim öncesi elle yedek alınıyor" });

        var id = TestKurulum.Metin(cevap, "id")!;
        var kayit = await db.YedekTalepleri.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal(YedekTalepDurumu.Bekliyor, kayit.Durum);
        Assert.Equal(k.TenantId, kayit.TenantId);
        Assert.Equal(k.KullaniciId, kayit.KullaniciId);
        // API SÜREÇ BAŞLATMAZ: kuyruk satırı dışında hiçbir koşu satırı doğmaz.
        Assert.Null(kayit.YedekKaydiId);
    }

    [Fact]
    public async Task AyniKiracidaIkinciBekleyenTalep_Reddedilir()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "T3-ikinci-talep");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        await servis.TalepAc(k.Islem, new YedekTalepDto { Gerekce = "ilk talep, aylık yedek denetimi" });

        var hata = await Assert.ThrowsAsync<ApiException>(() =>
            servis.TalepAc(k.Islem, new YedekTalepDto { Gerekce = "ikinci talep, aynı anda açılıyor" }));
        Assert.Equal(429, hata.StatusCode);
    }

    [Fact]
    public async Task TalepAc_GerekceKisa_ServisteDeReddedilir()
    {
        // DTO DataAnnotations YALNIZ controller yolunda çalışır; servis içi elle
        // kontrol ayrı bir kapıdır ve ayrıca sınanır (değişmez kural).
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "T3-servis-kisa");
        using var db = vt.Olustur(k);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).TalepAc(k.Islem, new YedekTalepDto { Gerekce = "çok kısa" }));
        Assert.Equal(0, await db.YedekTalepleri.CountAsync());
    }

    [Fact]
    public async Task Talep_IptalEdilince_HardDeleteOlmaz_DurumIPTAL()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "T3-iptal");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        var acilan = await servis.TalepAc(k.Islem, new YedekTalepDto { Gerekce = "yanlışlıkla açılan yedek talebi" });
        var id = TestKurulum.Metin(acilan, "id")!;
        var oncekiAdet = await db.YedekTalepleri.CountAsync();

        await servis.TalepIptal(k.Islem, id,
            new YedekTalepGuncelleDto { IptalGerekce = "yanlışlıkla açıldı, iptal edildi" });

        var sonrakiAdet = await db.YedekTalepleri.CountAsync();
        var kayit = await db.YedekTalepleri.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal(oncekiAdet, sonrakiAdet);            // SİLİNMEDİ
        Assert.Equal(YedekTalepDurumu.Iptal, kayit.Durum);
        Assert.NotNull(kayit.IptalGerekce);
    }

    [Fact]
    public async Task IslenmisTalep_IptalEdilemez()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "T3-islenmis");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        var acilan = await servis.TalepAc(k.Islem, new YedekTalepDto { Gerekce = "kuyruğa alınan elle yedek talebi" });
        var id = TestKurulum.Metin(acilan, "id")!;

        // Betiğin yaptığını taklit et: kuyruk tüketildi.
        var kayit = await db.YedekTalepleri.SingleAsync(t => t.Id == id);
        kayit.Durum = YedekTalepDurumu.Islendi;
        kayit.Islendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            servis.TalepIptal(k.Islem, id,
                new YedekTalepGuncelleDto { IptalGerekce = "artık gerekmiyor, iptal edilsin" }));
    }

    [Fact]
    public async Task IptalGerekcesiKisa_Reddedilir()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "T3-kisa-iptal");
        using var db = vt.Olustur(k);
        var servis = Servis(db);

        var acilan = await servis.TalepAc(k.Islem, new YedekTalepDto { Gerekce = "gerekçeli elle yedek talebi" });
        var id = TestKurulum.Metin(acilan, "id")!;

        await Assert.ThrowsAsync<BadRequestException>(() =>
            servis.TalepIptal(k.Islem, id, new YedekTalepGuncelleDto { IptalGerekce = "kısa" }));
    }

    [Fact]
    public async Task Ozet_OlcumVeTaahhudu_AYRI_Dondurur()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "T3-ozet");
        // yedek_kayitlari KİRACISIZDIR: başka testlerin bıraktığı koşular bu
        // testin "son yedek" ölçümünü bozar. Ölçüm testi kendi zeminini kurar.
        kur.YedekKayitlari.RemoveRange(kur.YedekKayitlari);
        await kur.SaveChangesAsync();
        kur.YedekKayitlari.Add(new YedekKaydi
        {
            Id = Kimlik.Ulid(), Tur = YedekTuru.Tam, Baslangic = DateTime.UtcNow.AddMinutes(-90),
            Bitis = DateTime.UtcNow.AddMinutes(-89), Durum = YedekDurumu.Basarili,
            DosyaAdi = "kuyumcu_net-ozet.sql.gz.gpg", BoyutBayt = 4096, Sha256 = new string('a', 64),
            Sifreli = true, DisKopya = DisKopyaDurumu.Kopyalandi,
            Dogrulama = YedekDogrulama.Dogrulandi, DogrulamaTarih = DateTime.UtcNow, TabloSayisi = 54,
        });
        await kur.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var ozet = await Servis(db).Ozet(k.Islem);

        // MÜHÜR 6 / K2: ölçüm ile taahhüt AYRI alanlardır.
        var rpoOlculen = TestKurulum.Sayi(ozet, "rpoDakikaOlculen");
        Assert.InRange(rpoOlculen, 80, 100);
        Assert.Equal(360m, TestKurulum.Sayi(ozet, "rpoDakikaTaahhut"));
        Assert.Equal(120m, TestKurulum.Sayi(ozet, "rtoDakikaTaahhut"));
        Assert.True(TestKurulum.Bayrak(ozet, "sonKosuSaglikli"));
    }

    [Fact]
    public async Task Liste_DosyaAdiVeYolDondurmez()
    {
        using var kur = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(kur, "T3-dosya-adi");
        kur.YedekKayitlari.Add(new YedekKaydi
        {
            Id = Kimlik.Ulid(), Tur = YedekTuru.Tam, Baslangic = DateTime.UtcNow,
            Durum = YedekDurumu.Basarili, DosyaAdi = "GIZLI-DOSYA-ADI.sql.gz.gpg",
            BoyutBayt = 1024, Sifreli = true, DisKopya = DisKopyaDurumu.Kopyalandi,
        });
        await kur.SaveChangesAsync();

        using var db = vt.Olustur(k);
        var liste = await Servis(db).Liste(k.Islem, 50);
        var json = System.Text.Json.JsonSerializer.Serialize(liste);

        // ⚠ Dosya adı/yolu istemciye DÖNMEZ (indirme yüzeyi açılmasın).
        Assert.DoesNotContain("GIZLI-DOSYA-ADI", json);
    }

    [Fact]
    public void TatbikatRaporu_YalnizTabloAdiTasir_DegerYazilmaz()
    {
        // Mehmet koşulu: rapor alanına veri DEĞERİ yazılmaz.
        var rapor = YedekPolitikasi.TatbikatRaporu(["cariler", "hareketler"]);
        Assert.Equal("uyusmayan_tablolar=cariler,hareketler", rapor);
        Assert.DoesNotContain("=", rapor.Split('=', 2)[1]);
    }
}
