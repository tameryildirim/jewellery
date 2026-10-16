using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Fiyat;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİYAT LİSTESİ — VARSAYILAN BAYRAĞI KORUMASI (paket UX-4a madde 1)
//
// NEDEN VAR: ListeGuncelle eskiden
//     liste.Varsayilan = dto.Varsayilan || liste.Varsayilan && dto.Varsayilan;
// yazıyordu. Gövdede varsayilan=false gelen HER PATCH (ekranın tam gövde
// göndermesi buna yol açar) kiracının varsayılan listesini SESSİZCE düşürüyor,
// FiyatServisi.ListeCoz null dönüyor ve POS hiçbir ürüne otomatik fiyat
// veremiyordu. Düzenle düğmesi bu hata kapatılmadan eklenemezdi (UX-4 sırası
// 1 → 2 bu yüzden bağlayıcıdır).
//
// Sözleşme (UX-S26): gönderilmeyen/false alan KORUNUR; yalnız true atar.
// Fail-closed: hiçbir güncelleme kiracıyı "aktif varsayılan listesiz" bırakamaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class FiyatVarsayilanKorumaTestleri(TestVeritabani vt)
{
    private static AktifKullanici Kul(Kiraci k) => k.Islem;

    private async Task<FiyatListesi> ListeAc(Kiraci k, string ad, decimal marj, bool varsayilan, bool aktif = true)
    {
        using var db = vt.Olustur(k.TenantId);
        return await new FiyatServisi(db).ListeOlustur(new FiyatListesiDto
        {
            Ad = ad, HasMarjYuzde = marj, IscilikCarpanYuzde = 100m, Varsayilan = varsayilan, Aktif = aktif,
        }, Kul(k));
    }

    private static FiyatListesiDto TamGovde(FiyatListesi l, bool varsayilan, bool? aktif = null) => new()
    {
        Ad = l.Ad, Tur = l.Tur, HasMarjYuzde = l.HasMarjYuzde,
        IscilikCarpanYuzde = l.IscilikCarpanYuzde, Varsayilan = varsayilan, Aktif = aktif ?? l.Aktif,
    };

    /// <summary>M1-1 — gövdede varsayilan=false gelse bile varsayılanlık DÜŞMEZ.</summary>
    [Fact]
    public async Task PATCH_varsayilan_FALSE_gelince_varsayilanlik_KORUNUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4a varsayılan koruma");
        var liste = await ListeAc(k, "Perakende", 5m, varsayilan: true);
        Assert.True(liste.Varsayilan);

        using (var db = vt.Olustur(k.TenantId))
        {
            var g = TamGovde(liste, varsayilan: false);
            g.Ad = "Perakende 2";                                   // yalnız ad değişiyor
            var sonuc = await new FiyatServisi(db).ListeGuncelle(liste.Id, g, Kul(k));
            Assert.True(sonuc.Varsayilan);                          // ← koruma
            Assert.Equal("Perakende 2", sonuc.Ad);
        }

        using var oku = vt.OlusturSistem();
        var kayit = await oku.FiyatListeleri.AsNoTracking().SingleAsync(f => f.Id == liste.Id);
        Assert.True(kayit.Varsayilan);

        // Ve POS yolu hâlâ çalışır: liste çözülüyor.
        using var db2 = vt.Olustur(k.TenantId);
        Assert.NotNull(await new FiyatServisi(db2).ListeCoz(k.TenantId, null));
    }

    /// <summary>Varsayılanlık yalnız BAŞKA bir listeye devredilerek kalkar.</summary>
    [Fact]
    public async Task Varsayilanlik_baska_listeye_DEVREDILIR_ve_tek_varsayilan_kalir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4a varsayılan devir");
        var a = await ListeAc(k, "A", 5m, varsayilan: true);
        var b = await ListeAc(k, "B", 8m, varsayilan: false);

        using (var db = vt.Olustur(k.TenantId))
            await new FiyatServisi(db).ListeGuncelle(b.Id, TamGovde(b, varsayilan: true), Kul(k));

        using var oku = vt.OlusturSistem();
        var hepsi = await oku.FiyatListeleri.AsNoTracking().Where(f => f.TenantId == k.TenantId).ToListAsync();
        Assert.Equal(b.Id, Assert.Single(hepsi, f => f.Varsayilan).Id);
        Assert.False(hepsi.Single(f => f.Id == a.Id).Varsayilan);
    }

    /// <summary>Fail-closed: kiracıyı aktif varsayılan listesiz bırakan güncelleme 400.</summary>
    [Fact]
    public async Task Varsayilansiz_birakan_guncelleme_400_verir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4a varsayılansız kapı");
        // İlk liste sunucuda zorla varsayılan olur; PASİF açılınca kiracıda
        // "varsayılan ama aktif değil" durumu doğar → ListeCoz null.
        var liste = await ListeAc(k, "Pasif varsayılan", 5m, varsayilan: true, aktif: false);
        Assert.True(liste.Varsayilan);
        Assert.False(liste.Aktif);

        using (var db = vt.Olustur(k.TenantId))
        {
            var g = TamGovde(liste, varsayilan: false);
            g.Ad = "Adı değişsin";
            g.Aktif = null;                                        // 'aktif' gönderilmiyor → koru
            var hata = await Assert.ThrowsAsync<BadRequestException>(
                () => new FiyatServisi(db).ListeGuncelle(liste.Id, g, Kul(k)));
            Assert.Contains("varsayılan fiyat listesiz", hata.Message);
        }

        // Kapı GERÇEKTEN kapandı: ad değişmedi.
        using var oku = vt.OlusturSistem();
        Assert.Equal("Pasif varsayılan", (await oku.FiyatListeleri.AsNoTracking().SingleAsync(f => f.Id == liste.Id)).Ad);

        // Aynı listeyi AKTİF etmek serbesttir — düzeltme yolu açıktır.
        using var db2 = vt.Olustur(k.TenantId);
        var duzelen = await new FiyatServisi(db2).ListeGuncelle(liste.Id, TamGovde(liste, varsayilan: false, aktif: true), Kul(k));
        Assert.True(duzelen.Aktif);
        Assert.True(duzelen.Varsayilan);
    }

    /// <summary>Varsayılan liste pasife alınamaz (mevcut kapı korunuyor).</summary>
    [Fact]
    public async Task Varsayilan_liste_PASIFE_ALINAMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4a varsayılan pasif");
        var liste = await ListeAc(k, "Perakende", 5m, varsayilan: true);

        using var db = vt.Olustur(k.TenantId);
        var hata = await Assert.ThrowsAsync<BadRequestException>(
            () => new FiyatServisi(db).ListeGuncelle(liste.Id, TamGovde(liste, varsayilan: true, aktif: false), Kul(k)));
        Assert.Contains("pasife alınamaz", hata.Message);
    }

    /// <summary>Madde 3 — panel "bu listeyle" derken O listenin marjını kullanır.</summary>
    [Fact]
    public async Task TeklifListeyle_SECILEN_listenin_marjini_uygular()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "UX4a liste bazlı teklif");
        var varsayilanListe = await ListeAc(k, "Varsayılan", 0m, varsayilan: true);
        var digerListe = await ListeAc(k, "Yüksek marj", 20m, varsayilan: false);

        using var db = vt.Olustur(k.TenantId);
        var servis = new FiyatServisi(db);
        var urun = await db.Urunler.AsNoTracking().SingleAsync(u => u.Id == k.UrunId);
        var kur = await db.KurSnapshots.AsNoTracking().Where(s => s.Id == k.KurId).Select(s => s.AltinHasFiyati).SingleAsync();
        var bugun = Zaman.YerelBugun();

        var varsayilanTeklif = await servis.Teklif(urun, null, kur, bugun);
        var secilenTeklif = await servis.TeklifListeyle(urun, (await servis.ListeGetir(k.TenantId, digerListe.Id))!, kur, bugun);

        Assert.NotNull(varsayilanTeklif);
        Assert.NotNull(secilenTeklif);
        Assert.Equal(0m, varsayilanTeklif!.MarjTl);
        Assert.True(secilenTeklif!.MarjTl > 0m);
        Assert.True(secilenTeklif.ListeFiyati > varsayilanTeklif.ListeFiyati);
        Assert.Equal(varsayilanListe.Id, (await servis.ListeCoz(k.TenantId, null))!.Id);
    }
}
