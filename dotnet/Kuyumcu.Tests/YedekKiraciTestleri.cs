using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yedek;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// YEDEK — KİRACI İZOLASYONU (paket T-3 Faz A)
//
// Sınır: yedek KOŞUSU ve TATBİKATI sistem düzeyidir (tüm kiracıları kapsar,
// tenant_id taşımaz), elle yedek TALEBİ kiracıya aittir. Bir kiracının SAHİBİ
// başka kiracının TALEBİNİ göremez.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class YedekKiraciTestleri(TestVeritabani vt)
{
    private static YedekServisi Servis(KuyumcuDbContext db) =>
        new(db, TestKurulum.Denetim(db),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build());

    [Fact]
    public async Task BaskaKiracininTalebi_Listede_Gorunmez()
    {
        using var kur = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kur, "T3-kiraci-A");
        var b = TestKurulum.KiraciKur(kur, "T3-kiraci-B");

        using var dbA = vt.Olustur(a);
        var acilan = await Servis(dbA).TalepAc(a.Islem,
            new YedekTalepDto { Gerekce = "A kiracısının elle yedek talebi" });
        var idA = TestKurulum.Metin(acilan, "id")!;

        using var dbB = vt.Olustur(b);
        var listeB = await Servis(dbB).Talepler(b.Islem, 50);
        var json = System.Text.Json.JsonSerializer.Serialize(listeB);

        Assert.DoesNotContain(idA, json);
        Assert.DoesNotContain("A kiracısının", json);
    }

    [Fact]
    public async Task YedekTalebi_TenantId_KiraciYazmaDenetcisiTarafindanZorlanir()
    {
        using var kur = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kur, "T3-yazma-A");
        var b = TestKurulum.KiraciKur(kur, "T3-yazma-B");

        using var dbA = vt.Olustur(a);
        dbA.YedekTalepleri.Add(new YedekTalebi
        {
            Id = Kimlik.Ulid(),
            TenantId = b.TenantId,          // ← BAŞKA kiracı: denetçi patlamalı
            KullaniciId = a.KullaniciId,
            Gerekce = "başka kiracıya yazma denemesi, reddedilmeli",
            Durum = YedekTalepDurumu.Bekliyor,
            Olusturuldu = DateTime.UtcNow,
        });

        await Assert.ThrowsAsync<KiraciIhlaliException>(() => dbA.SaveChangesAsync());
    }

    [Fact]
    public async Task YedekKaydi_KiracisizVarliktir_TumKiracilardan_AyniGorunur()
    {
        using var kur = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(kur, "T3-sistem-A");
        var b = TestKurulum.KiraciKur(kur, "T3-sistem-B");
        var id = Kimlik.Ulid();
        kur.YedekKayitlari.Add(new YedekKaydi
        {
            Id = id, Tur = YedekTuru.Tam, Baslangic = DateTime.UtcNow,
            Durum = YedekDurumu.Basarili, Sifreli = true, DisKopya = DisKopyaDurumu.Kopyalandi,
        });
        await kur.SaveChangesAsync();

        using var dbA = vt.Olustur(a);
        using var dbB = vt.Olustur(b);
        Assert.True(await dbA.YedekKayitlari.AnyAsync(y => y.Id == id));
        Assert.True(await dbB.YedekKayitlari.AnyAsync(y => y.Id == id));
    }
}
