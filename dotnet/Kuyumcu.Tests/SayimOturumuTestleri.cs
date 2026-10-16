using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Sayim;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SAYIM OTURUMU — paket 23. Kurallar: açılışta sistem adedi dondurulur ·
// sayılmayan SIFIRLANMAZ · fark deftere SAYIM_DUZELTME olur · hareketli satır
// onaysız uygulanmaz · aynı şubede ikinci açık oturum yok.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class SayimOturumuTestleri(TestVeritabani vt)
{
    private static SayimServisi Servis(KuyumcuDbContext db) =>
        new(db, new MUrun.UrunServisi(db));

    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) => new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static dynamic D(object o) => o;
    private static T Prop<T>(object o, string ad) => (T)o.GetType().GetProperty(ad)!.GetValue(o)!;
    private static System.Collections.IList Satirlar(object detay) => (System.Collections.IList)Prop<object>(detay, "satirlar");
    private static T SatirProp<T>(object satir, string ad) { var v = satir.GetType().GetProperty(ad)!.GetValue(satir); return (T)v!; }

    private async Task<string> Ac(Kiraci k, string kapsam = "TUMU", string? kapsamId = null)
    {
        using var db = vt.Olustur(k.TenantId);
        var d = await Servis(db).Ac(new SayimAcDto { Ad = "Vitrin", SubeId = k.SubeId, KapsamTur = kapsam, KapsamId = kapsamId }, k.Islem);
        return Prop<string>(d, "Id");
    }

    [Fact]
    public async Task Acilista_sistem_adedi_DONDURULUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım açılış", stokAdet: 7m);
        var id = await Ac(k);
        using var db = vt.Olustur(k.TenantId);
        var d = await Servis(db).Detay(id, k.Islem);
        var s = Satirlar(d).Cast<object>().Single(x => SatirProp<string>(x, "urunId") == k.UrunId);
        Assert.Equal(7m, SatirProp<decimal>(s, "sistemAdet"));
        Assert.Null(s.GetType().GetProperty("sayilanAdet")!.GetValue(s));   // henüz sayılmadı
        Assert.Equal(1, Prop<int>(d, "sayilmayan"));
    }

    [Fact]
    public async Task Ayni_subede_ikinci_ACIK_oturum_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım çift", stokAdet: 1m);
        await Ac(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Ac(k));
    }

    [Fact]
    public async Task Say_barkodla_bulur_ve_ikinci_sayim_USTUNE_yazar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım barkod", stokAdet: 5m);
        using (var db = vt.OlusturSistem()) { db.Urunler.Single(u => u.Id == k.UrunId).Barkod = "869SAY1"; db.SaveChanges(); }
        var id = await Ac(k);
        using var kontrol = vt.Olustur(k.TenantId);
        await Servis(kontrol).Say(id, new SayimSayDto { Barkod = "869SAY1", SayilanAdet = 4m }, k.Islem);
        var d = await Servis(kontrol).Say(id, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 3m }, k.Islem);
        var s = Satirlar(d).Cast<object>().Single();
        Assert.Equal(3m, SatirProp<decimal>(s, "sayilanAdet"));
        Assert.Equal(-2m, SatirProp<decimal>(s, "fark"));
        await Assert.ThrowsAsync<NotFoundException>(() => Servis(kontrol).Say(id, new SayimSayDto { Barkod = "YOK", SayilanAdet = 1m }, k.Islem));
    }

    /// <summary>Kapanış: fark deftere SAYIM_DUZELTME olur, stok sayılana eşitlenir, satıra hareket bağlanır.</summary>
    [Fact]
    public async Task Kapanis_farki_DEFTERE_yazar_ve_stogu_esitler()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım kapanış", stokAdet: 5m);
        var id = await Ac(k);
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 3m }, k.Islem);
        var y = await Servis(db).Kapat(id, new SayimKapatDto(), k.Islem);
        Assert.Equal(1, Prop<int>(y, "uygulanan"));

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(3m, kontrol.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet);
        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SAYIM_DUZELTME");
        Assert.Equal(0m, h.TlTutari);
        Assert.True(h.HasGrami < 0m);
        Assert.Equal(h.Id, kontrol.SayimSatirlari.AsNoTracking().Single(s => s.OturumId == id).DuzeltmeHareketId);
        Assert.Equal("KAPANDI", kontrol.SayimOturumlari.AsNoTracking().Single(o => o.Id == id).Durum);
    }

    /// <summary>Sayılmayan satır SIFIRLANMAZ: bilinmeyen ≠ sıfır.</summary>
    [Fact]
    public async Task Sayilmayan_satir_kapanista_SIFIRLANMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım sayılmayan", stokAdet: 5m);
        var id = await Ac(k);
        using var db = vt.Olustur(k.TenantId);
        // hiçbir satır sayılmadı → kapatınca hiçbir şey yazılmaz
        var y = await Servis(db).Kapat(id, new SayimKapatDto(), k.Islem);
        Assert.Equal(0, Prop<int>(y, "uygulanan"));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(5m, kontrol.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet);
        Assert.False(kontrol.Hareketler.AsNoTracking().Any(x => x.TenantId == k.TenantId && x.Tur == "SAYIM_DUZELTME"));
    }

    /// <summary>Oturum sırasında satılan ürünün satırı işaretlenir ve onaysız UYGULANMAZ.</summary>
    [Fact]
    public async Task Oturumda_hareket_goren_satir_ONAYSIZ_uygulanmaz_onayla_uygulanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım hareketli", stokAdet: 5m);
        var id = await Ac(k);
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 5m }, k.Islem);
        // sayımdan sonra 1 adet satıldı → güncel 4, sayılan 5 (satış öncesi gerçek)
        await Satis(db).Satis(new MSatis.SatisOlusturDto { CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 1_000m, OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem);
        await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 3m }, k.Islem);   // fark −2 (sisteme göre)

        var d = await Servis(db).Detay(id, k.Islem);
        Assert.Equal(1, Prop<int>(d, "hareketli"));
        Assert.True(SatirProp<bool>(Satirlar(d).Cast<object>().Single(), "oturumdaHareketVar"));

        var y = await Servis(db).Kapat(id, new SayimKapatDto { HareketliSatirlariUygula = false }, k.Islem);
        Assert.Equal(0, Prop<int>(y, "uygulanan")); Assert.Equal(1, Prop<int>(y, "atlanan"));
        using (var kontrol = vt.OlusturSistem())
            Assert.Equal(4m, kontrol.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet);   // dokunulmadı

        // Onaylı ikinci oturum: sayılan (3) güncel stoğun (4) üzerine yazılır
        var id2 = await Ac(k);
        await Servis(db).Say(id2, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 3m }, k.Islem);
        await Satis(db).Satis(new MSatis.SatisOlusturDto { CariId = k.CariId, UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 1_000m, OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem);
        var y2 = await Servis(db).Kapat(id2, new SayimKapatDto { HareketliSatirlariUygula = true }, k.Islem);
        Assert.Equal(1, Prop<int>(y2, "uygulanan"));
        using (var kontrol = vt.OlusturSistem())
            Assert.Equal(3m, kontrol.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet);
    }

    [Fact]
    public async Task Kapanmis_oturumda_sayim_ve_kapanis_REDDEDILIR_iptal_defter_yazmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım kapanmış", stokAdet: 5m);
        var id = await Ac(k);
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 1m }, k.Islem);
        await Servis(db).Iptal(id, k.Islem);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 2m }, k.Islem));
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Kapat(id, new SayimKapatDto(), k.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(5m, kontrol.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet);
        Assert.False(kontrol.Hareketler.AsNoTracking().Any(x => x.TenantId == k.TenantId && x.Tur == "SAYIM_DUZELTME"));
    }

    [Fact]
    public async Task Kapsam_disi_urun_SAYILAMAZ_ve_kapsam_hedefi_dogrulanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Sayım kapsam", stokAdet: 5m);
        await Assert.ThrowsAsync<BadRequestException>(() => Ac(k, "REYON", "yok"));
        await Assert.ThrowsAsync<BadRequestException>(() => Ac(k, "KATEGORI", null));
        var id = await Ac(k);
        using var db = vt.Olustur(k.TenantId);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Say(id, new SayimSayDto { UrunId = "baska-urun", SayilanAdet = 1m }, k.Islem));
    }
}
