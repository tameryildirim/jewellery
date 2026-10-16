using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Fiyat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// FİYAT LİSTESİ / KAMPANYA / HEDİYE — veritabanlı (paket 17)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class FiyatListesiTestleri(TestVeritabani vt)
{
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) =>
        new(db, NullLogger<MSatis.SatisServisi>.Instance);

    private static AktifKullanici Kul(Kiraci k) => k.Islem;

    private async Task<FiyatListesi> ListeAc(Kiraci k, decimal marj = 5m, bool varsayilan = true, decimal carpan = 100m)
    {
        using var db = vt.Olustur(k.TenantId);
        return await new FiyatServisi(db).ListeOlustur(new FiyatListesiDto
        {
            Ad = $"Liste {marj}", HasMarjYuzde = marj, IscilikCarpanYuzde = carpan, Varsayilan = varsayilan,
        }, Kul(k));
    }

    private async Task<Kampanya> KampanyaAc(Kiraci k, string tur, decimal deger, int gun = 7, string hedefTur = "TUMU", string? hedefId = null)
    {
        using var db = vt.Olustur(k.TenantId);
        var bugun = Zaman.YerelBugun();
        return await new FiyatServisi(db).KampanyaOlustur(new KampanyaDto
        {
            Ad = "K", Baslangic = bugun.AddDays(-1), Bitis = bugun.AddDays(gun), HedefTur = hedefTur, HedefId = hedefId,
            IndirimTuru = tur, Deger = deger,
        }, Kul(k));
    }

    private Urun Urun(Kiraci k) { using var db = vt.OlusturSistem(); return db.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId); }
    private decimal HasKuru(Kiraci k) { using var db = vt.OlusturSistem(); return db.KurSnapshots.AsNoTracking().Single(s => s.Id == k.KurId).AltinHasFiyati; }

    private static MSatis.SatisFisiDto Fis(Kiraci k, params MSatis.SatisFisKalemDto[] kalemler) => new()
    {
        CariId = k.CariId, Kalemler = kalemler.ToList(), KurSnapshotId = k.KurId,
        Odemeler = [new() { Arac = "KART", TutarTl = kalemler.Where(x => !x.Hediye).Sum(x => x.Adet * x.BirimFiyatTl), KasaId = k.KasaId }],
    };

    // ── Liste ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ilk_liste_otomatik_VARSAYILAN_olur_ve_tek_varsayilan_kalir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Liste varsayılan");
        var a = await ListeAc(k, 5m, varsayilan: false);
        Assert.True(a.Varsayilan);                                  // kiracının ilk listesi
        var b = await ListeAc(k, 2m, varsayilan: true);
        using var db = vt.OlusturSistem();
        Assert.Single(db.FiyatListeleri.Where(f => f.TenantId == k.TenantId && f.Varsayilan));
        Assert.Equal(b.Id, db.FiyatListeleri.Single(f => f.TenantId == k.TenantId && f.Varsayilan).Id);
    }

    [Fact]
    public async Task Cariye_atanan_liste_varsayilani_EZER()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Liste cari");
        await ListeAc(k, 8m, varsayilan: true);
        var toptan = await ListeAc(k, 1m, varsayilan: false);
        using (var db = vt.OlusturSistem()) { db.Cariler.Single(c => c.Id == k.CariId).FiyatListesiId = toptan.Id; db.SaveChanges(); }

        using var kontrol = vt.Olustur(k.TenantId);
        var servis = new FiyatServisi(kontrol);
        var u = Urun(k); var kur = HasKuru(k);
        var cariTeklif = await servis.Teklif(u, k.CariId, kur, Zaman.YerelBugun());
        var tezgahTeklif = await servis.Teklif(u, null, kur, Zaman.YerelBugun());
        Assert.Equal(FiyatMotoru.Yuvarla(u.HasGrami * kur * 0.01m), cariTeklif!.MarjTl);
        Assert.Equal(FiyatMotoru.Yuvarla(u.HasGrami * kur * 0.08m), tezgahTeklif!.MarjTl);
    }

    [Fact]
    public async Task Liste_yoksa_teklif_YOK_uydurulmaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Liste yok");
        using var db = vt.Olustur(k.TenantId);
        Assert.Null(await new FiyatServisi(db).Teklif(Urun(k), k.CariId, HasKuru(k), Zaman.YerelBugun()));
    }

    // ── Kampanya kuralları ───────────────────────────────────────────────────

    /// <summary>FEY md.11/(3): indirimli satış en fazla üç ay.</summary>
    [Fact]
    public async Task Uc_aydan_uzun_kampanya_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kampanya süre");
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => KampanyaAc(k, "TUTAR_TL", 100m, gun: 95));
        Assert.Contains("üç ay", ex.Message);
        await KampanyaAc(k, "TUTAR_TL", 100m, gun: 90);             // sınır içi geçer
    }

    [Fact]
    public async Task Hedefi_bulunmayan_kampanya_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kampanya hedef");
        await Assert.ThrowsAsync<BadRequestException>(() => KampanyaAc(k, "TUTAR_TL", 100m, hedefTur: "URUN", hedefId: "yok"));
        await Assert.ThrowsAsync<BadRequestException>(() => KampanyaAc(k, "TUTAR_TL", 100m, hedefTur: "KATEGORI", hedefId: null));
    }

    [Fact]
    public async Task Kategori_kampanyasi_ALT_kategoriyi_kapsar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Kampanya kategori");
        await ListeAc(k);
        string ust, alt;
        using (var db = vt.OlusturSistem())
        {
            var now = DateTime.UtcNow;
            ust = Kimlik.Ulid(); alt = Kimlik.Ulid();
            db.UrunKategorileri.Add(new UrunKategorisi { Id = ust, TenantId = k.TenantId, Ad = "Bilezik", Olusturuldu = now, Guncellendi = now });
            db.UrunKategorileri.Add(new UrunKategorisi { Id = alt, TenantId = k.TenantId, Ad = "Burma", UstId = ust, Olusturuldu = now, Guncellendi = now });
            db.Urunler.Single(u => u.Id == k.UrunId).KategoriId = alt;
            db.SaveChanges();
        }
        await KampanyaAc(k, "TUTAR_TL", 250m, hedefTur: "KATEGORI", hedefId: ust);

        using var kontrol = vt.Olustur(k.TenantId);
        var t = await new FiyatServisi(kontrol).Teklif(Urun(k), k.CariId, HasKuru(k), Zaman.YerelBugun());
        Assert.Equal(250m, t!.IndirimTutari);
    }

    // ── Satış satırına yazım (FEY md.11) ─────────────────────────────────────

    [Fact]
    public async Task Fis_kalemine_liste_fiyati_indirim_ve_kampanya_YAZILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Fiş liste fiyatı", stokAdet: 10m);
        await ListeAc(k, marj: 0m, carpan: 100m);
        var kamp = await KampanyaAc(k, "TUTAR_TL", 300m);
        using var db = vt.Olustur(k.TenantId);
        var u = Urun(k); var liste = FiyatMotoru.Yuvarla(u.HasGrami * HasKuru(k) + (u.IscilikTl ?? 0m));
        var satis = liste - 300m;

        await Satis(db).SatisFisi(Fis(k, new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = satis }), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS");
        Assert.Equal(liste, h.ListeFiyati);
        Assert.Equal(300m, h.IndirimTutari);
        Assert.Equal(kamp.Id, h.KampanyaId);
        Assert.False(h.Hediye);
    }

    /// <summary>Kasiyer liste ÜSTÜNDE satarsa indirim 0'dır — negatif indirim yazılmaz.</summary>
    [Fact]
    public async Task Liste_ustunde_satista_indirim_SIFIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Liste üstü", stokAdet: 10m);
        await ListeAc(k, marj: 0m);
        using var db = vt.Olustur(k.TenantId);
        var u = Urun(k); var liste = FiyatMotoru.Yuvarla(u.HasGrami * HasKuru(k) + (u.IscilikTl ?? 0m));

        await Satis(db).SatisFisi(Fis(k, new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = liste + 500m }), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS");
        Assert.Equal(0m, h.IndirimTutari);
        Assert.Null(h.KampanyaId);
    }

    /// <summary>TRHTUY md.14: indirim öncesi fiyat = son 30 günün EN DÜŞÜK liste fiyatı.</summary>
    [Fact]
    public async Task Indirim_oncesi_fiyat_son_30_gunun_EN_DUSUGUDUR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "30 gün", stokAdet: 10m);
        await ListeAc(k, marj: 0m);
        var now = DateTime.UtcNow;
        using (var db = vt.OlusturSistem())
        {
            // 20 gün önce 9.000'e, 40 gün önce 7.000'e satılmış (40 gün pencerenin DIŞINDA)
            foreach (var (gun, fiyat) in new[] { (20, 9_000m), (40, 7_000m) })
                HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(fiyat), new Hareket
                {
                    Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now.AddDays(-gun),
                    TlTutari = fiyat, HasGrami = -1m, OdemeTuru = "NAKIT", KurSnapshotId = k.KurId, UrunId = k.UrunId,
                    KullaniciId = k.KullaniciId, Olusturuldu = now.AddDays(-gun), ListeFiyati = fiyat,
                });
            db.SaveChanges();
        }
        using var kontrol = vt.Olustur(k.TenantId);
        Assert.Equal(9_000m, await new FiyatServisi(kontrol).IndirimOncesiFiyat(k.TenantId, k.UrunId, 10_000m, now));
        Assert.Equal(8_500m, await new FiyatServisi(kontrol).IndirimOncesiFiyat(k.TenantId, k.UrunId, 8_500m, now)); // bugünkü liste daha düşükse o
    }

    // ── Hediye ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Hediye_kalemi_bedelli_kalemin_YANINDA_sifir_fiyatla_gecer_stoktan_duser()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Hediye", stokAdet: 10m);
        await ListeAc(k, marj: 0m);
        await KampanyaAc(k, "TUTAR_TL", 100m);   // aktif kampanya varken hediye damgalanmamalı
        using var db = vt.Olustur(k.TenantId);
        var u = Urun(k); var liste = FiyatMotoru.Yuvarla(u.HasGrami * HasKuru(k) + (u.IscilikTl ?? 0m));

        // İstemci hediyeye fiyat gönderse bile sunucu 0'a zorlar.
        await Satis(db).SatisFisi(Fis(k,
            new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = liste - 100m },
            new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = liste, Hediye = true }), k.Islem);   // istemci fiyat göndermiş; sunucu 0'a zorlar

        using var kontrol = vt.OlusturSistem();
        var hediye = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS" && x.Hediye);
        Assert.Equal(0m, hediye.TlTutari);
        Assert.Equal(liste, hediye.IndirimTutari);                // tutar iskontosu = liste fiyatı
        Assert.Null(hediye.KampanyaId);                           // hediye kampanya indirimi DEĞİL
        Assert.Equal(8m, kontrol.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet);
    }

    /// <summary>KDV GUT I/B-10.1 / KDVK md.27: tek başına hediye bedelsiz teslimdir → kabul edilmez.</summary>
    [Fact]
    public async Task Tek_basina_hediye_REDDEDILIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Hediye tek", stokAdet: 10m);
        await ListeAc(k);
        using var db = vt.Olustur(k.TenantId);
        var dto = Fis(k, new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = 0m, Hediye = true });
        dto.Odemeler = [];

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Satis(db).SatisFisi(dto, k.Islem));
        Assert.Contains("emsal bedel", ex.Message);
        using var kontrol = vt.OlusturSistem();
        Assert.Equal(10m, kontrol.Stoklar.AsNoTracking().Single(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId).Adet);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET 49 — liste × ürün görünümü (önizleme · geçersiz kılma · toplu uygulama)
    // ═════════════════════════════════════════════════════════════════════════

    private static FiyatServisi Servis(KuyumcuDbContext db) =>
        new(db, new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

    /// <summary>Kiracıya ek ürün — gümüş / sabit fiyatlı / kategorili senaryolar için.</summary>
    private string UrunEkle(Kiraci k, string kod, string ayarId = "K22", decimal gram = 10m,
                            decimal? iscilik = 0m, decimal? sabitFiyat = null, string? kategoriId = null)
    {
        using var db = vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        var milyem = Kuyumcu.Core.Domain.Ayarlar.Bul(ayarId)!.Milyem;
        var has = Kuyumcu.Core.Domain.HasHesap.HasGramHesapla(gram, 0m, ayarId);
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = kod, Ad = $"Ürün {kod}",
            AyarId = ayarId, Milyem = milyem, GramAgirlik = gram, TasAgirlik = 0m, HasGrami = has,
            IscilikTl = iscilik, SatisFiyati = sabitFiyat, KategoriId = kategoriId, Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        db.SaveChanges();
        return id;
    }

    private (string Ust, string Alt) KategoriAgaci(Kiraci k)
    {
        using var db = vt.OlusturSistem();
        var now = DateTime.UtcNow;
        var ust = Kimlik.Ulid(); var alt = Kimlik.Ulid();
        db.UrunKategorileri.Add(new UrunKategorisi { Id = ust, TenantId = k.TenantId, Ad = "Bilezik", Olusturuldu = now, Guncellendi = now });
        db.UrunKategorileri.Add(new UrunKategorisi { Id = alt, TenantId = k.TenantId, Ad = "Burma", UstId = ust, Olusturuldu = now, Guncellendi = now });
        db.SaveChanges();
        return (ust, alt);
    }

    /// <summary>M49-6 — önizleme toplamı ve her satırın KAYNAK rozeti.</summary>
    [Fact]
    public async Task Onizleme_toplam_ve_her_satirda_dogru_KAYNAK_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 önizleme");
        var liste = await ListeAc(k, marj: 8m);
        var sabitli = UrunEkle(k, "A-SABIT", sabitFiyat: 12_345m);
        var gumus = UrunEkle(k, "A-GUMUS", ayarId: "G925");
        var ozel = UrunEkle(k, "A-OZEL");

        using var db = vt.Olustur(k.TenantId);
        var servis = Servis(db);
        await servis.UrunFiyatiEkle(liste.Id, new UrunFiyatiDto { UrunId = ozel, HasMarjYuzde = 1m }, k.Islem);

        var sonuc = await servis.ListeOnizleme(liste.Id, k.KurId, k.Islem, limit: 50);
        Assert.Equal(4, sonuc.Toplam);                                   // kurulum ürünü + 3 ek
        Assert.Equal(50, sonuc.Limit);

        var s = sonuc.Satirlar.ToDictionary(x => x.Kod);
        Assert.Equal("URUN_SABIT_FIYAT", s["A-SABIT"].Kaynak);
        Assert.Equal(12_345m, s["A-SABIT"].ListeFiyati);
        Assert.False(s["A-GUMUS"].TeklifVar);
        Assert.Equal("GUMUS", s["A-GUMUS"].Sebep);
        Assert.Equal("URUN_GECERSIZ_KILMA", s["A-OZEL"].Kaynak);
        Assert.Equal(1m, s["A-OZEL"].GecersizKilma!.HasMarjYuzde);

        // Kesişim kuralı: geçersiz kılınmamış ürün listenin marjıyla kalır.
        var normal = sonuc.Satirlar.Single(x => x.UrunId == k.UrunId);
        Assert.Equal("LISTE", normal.Kaynak);
        Assert.Equal(FiyatMotoru.Yuvarla(normal.HasDegeri * 0.08m), normal.MarjTl);
    }

    /// <summary>Önizleme SALT HESAPTIR: hiçbir geçersiz kılma satırı yazmaz.</summary>
    [Fact]
    public async Task Onizleme_hicbir_satir_YAZMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 önizleme yazmaz");
        var liste = await ListeAc(k);
        using (var db = vt.Olustur(k.TenantId))
            await Servis(db).ListeOnizleme(liste.Id, k.KurId, k.Islem);
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.FiyatListesiUrunFiyatlari.Where(x => x.TenantId == k.TenantId));
    }

    /// <summary>M49-7 — aynı (liste, ürün) için ikinci ekleme 409.</summary>
    [Fact]
    public async Task Ayni_liste_ve_urun_icin_ikinci_gecersiz_kilma_409()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 çift");
        var liste = await ListeAc(k);
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).UrunFiyatiEkle(liste.Id, new UrunFiyatiDto { UrunId = k.UrunId, SabitFiyat = 100m }, k.Islem);
        await Assert.ThrowsAsync<ConflictException>(() =>
            Servis(db).UrunFiyatiEkle(liste.Id, new UrunFiyatiDto { UrunId = k.UrunId, SabitFiyat = 200m }, k.Islem));
        using var kontrol = vt.OlusturSistem();
        Assert.Single(kontrol.FiyatListesiUrunFiyatlari.Where(x => x.TenantId == k.TenantId));
    }

    /// <summary>M49-8 — BAŞKA kiracının listesi "yoktur" (kiracı izolasyonu).</summary>
    [Fact]
    public async Task Baska_kiracinin_listesine_gecersiz_kilma_404()
    {
        var a = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 kiracı A");
        var b = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 kiracı B");
        var listeA = await ListeAc(a);

        using var db = vt.Olustur(b.TenantId);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Servis(db).UrunFiyatiEkle(listeA.Id, new UrunFiyatiDto { UrunId = b.UrunId, SabitFiyat = 100m }, b.Islem));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Servis(db).ListeOnizleme(listeA.Id, b.KurId, b.Islem));

        // ⚠ İKİNCİ KALKAN AYRI SINANIR: global sorgu filtresi devre dışıyken
        // (sistem kapsamı) servisin KENDİ tenant_id koşulu tek başına durmalı.
        // Aksi halde "filtre zaten var" diye yazılan kod sessizce silinebilirdi.
        using var sistem = vt.OlusturSistem();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new FiyatServisi(sistem).ListeOnizleme(listeA.Id, b.KurId, b.Islem));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new FiyatServisi(sistem).TopluUygula(listeA.Id, new TopluUygulaDto
            {
                HedefTur = "SECILI", UrunIdler = [a.UrunId], HasMarjYuzde = 1m, KurSnapshotId = b.KurId,
            }, onizleme: true, b.Islem));
    }

    /// <summary>Gümüş üründe yalnız SABİT fiyat (Q3 mührü) — marj geçersiz kılması 400.</summary>
    [Fact]
    public async Task Gumus_urunde_marj_gecersiz_kilmasi_REDDEDILIR_sabit_fiyat_KABUL()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 gümüş");
        var liste = await ListeAc(k);
        var gumus = UrunEkle(k, "G-1", ayarId: "G925");
        using var db = vt.Olustur(k.TenantId);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).UrunFiyatiEkle(liste.Id, new UrunFiyatiDto { UrunId = gumus, HasMarjYuzde = 5m }, k.Islem));
        await Servis(db).UrunFiyatiEkle(liste.Id, new UrunFiyatiDto { UrunId = gumus, SabitFiyat = 1_200m }, k.Islem);

        var sonuc = await Servis(db).ListeOnizleme(liste.Id, k.KurId, k.Islem);
        var satir = sonuc.Satirlar.Single(x => x.UrunId == gumus);
        Assert.True(satir.TeklifVar);
        Assert.Equal(1_200m, satir.ListeFiyati);
    }

    /// <summary>M49-9 — toplu uygulama ÖNİZLEMESİ hiçbir satır yazmaz; özet doğrudur.</summary>
    [Fact]
    public async Task Toplu_uygula_onizlemesi_HICBIR_SATIR_YAZMAZ()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 toplu kuru");
        var liste = await ListeAc(k, marj: 8m);
        using var db = vt.Olustur(k.TenantId);
        var dto = new TopluUygulaDto
        {
            HedefTur = "SECILI", UrunIdler = [k.UrunId], HasMarjYuzde = 1m, KurSnapshotId = k.KurId,
        };
        var ozet = await Servis(db).TopluUygula(liste.Id, dto, onizleme: true, k.Islem);

        Assert.False(ozet.Uygulandi);
        Assert.Equal(1, ozet.Uygulanacak);
        Assert.True(ozet.ToplamFark < 0m);                               // %8 → %1 fiyatı DÜŞÜRÜR
        Assert.NotEmpty(ozet.ParmakIzi);
        using var kontrol = vt.OlusturSistem();
        Assert.Empty(kontrol.FiyatListesiUrunFiyatlari.Where(x => x.TenantId == k.TenantId));
    }

    /// <summary>M49-10 — onay damgası olmadan yazma 409; damgayla yazma geçer.</summary>
    [Fact]
    public async Task Toplu_uygula_PARMAK_IZI_olmadan_409_damgayla_YAZAR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 damga");
        var liste = await ListeAc(k, marj: 8m);
        using var db = vt.Olustur(k.TenantId);
        var dto = new TopluUygulaDto
        {
            HedefTur = "SECILI", UrunIdler = [k.UrunId], HasMarjYuzde = 1m, KurSnapshotId = k.KurId,
        };
        var ozet = await Servis(db).TopluUygula(liste.Id, dto, onizleme: true, k.Islem);

        await Assert.ThrowsAsync<ConflictException>(() =>
            Servis(db).TopluUygula(liste.Id, dto, onizleme: false, k.Islem));
        dto.ParmakIzi = "bozuk-damga";
        await Assert.ThrowsAsync<ConflictException>(() =>
            Servis(db).TopluUygula(liste.Id, dto, onizleme: false, k.Islem));

        dto.ParmakIzi = ozet.ParmakIzi;
        var sonuc = await Servis(db).TopluUygula(liste.Id, dto, onizleme: false, k.Islem);
        Assert.True(sonuc.Uygulandi);

        using var kontrol = vt.OlusturSistem();
        var kayit = kontrol.FiyatListesiUrunFiyatlari.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(1m, kayit.HasMarjYuzde);
        Assert.True(kayit.Aktif);
        // Denetim izi: "fiyatı kim, ne zaman değiştirdi" cevaplanabilir.
        Assert.Contains(kontrol.DenetimKayitlari.Where(d => d.TenantId == k.TenantId),
            d => d.VarlikTuru == "FiyatListesiTopluUygulama");
    }

    /// <summary>M49-11 — kategori hedefi ALT kategorileri de kapsar; gümüş ATLANIR.</summary>
    [Fact]
    public async Task Toplu_uygula_KATEGORI_hedefinde_alt_dallar_kapsanir_gumus_ATLANIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 kategori");
        var liste = await ListeAc(k, marj: 8m);
        var (ust, alt) = KategoriAgaci(k);
        var altUrun = UrunEkle(k, "B-ALT", kategoriId: alt);
        var gumus = UrunEkle(k, "B-GUMUS", ayarId: "G925", kategoriId: alt);
        var sabitli = UrunEkle(k, "B-SABIT", sabitFiyat: 5_000m, kategoriId: ust);

        using var db = vt.Olustur(k.TenantId);
        var dto = new TopluUygulaDto
        {
            HedefTur = "KATEGORI", KategoriId = ust, IscilikCarpanYuzde = 50m, KurSnapshotId = k.KurId,
        };
        var ozet = await Servis(db).TopluUygula(liste.Id, dto, onizleme: true, k.Islem);

        Assert.Equal(3, ozet.Hedeflenen);                                // üst + alt daldaki ürünler
        Assert.Equal(1, ozet.Uygulanacak);                               // yalnız B-ALT
        Assert.Contains(ozet.Atlandi, a => a.UrunId == gumus && a.Sebep == "GUMUS");
        Assert.Contains(ozet.Atlandi, a => a.UrunId == sabitli && a.Sebep == "URUN_SABIT_FIYAT");

        dto.ParmakIzi = ozet.ParmakIzi;
        await Servis(db).TopluUygula(liste.Id, dto, onizleme: false, k.Islem);
        using var kontrol = vt.OlusturSistem();
        var kayit = Assert.Single(kontrol.FiyatListesiUrunFiyatlari.Where(x => x.TenantId == k.TenantId));
        Assert.Equal(altUrun, kayit.UrunId);
        Assert.Equal(50m, kayit.IscilikCarpanYuzde);
    }

    /// <summary>PATCH sözleşmesi: gönderilmeyen alan KORUNUR, açık null SİLER; aktif=false pasife alır.</summary>
    [Fact]
    public async Task Gecersiz_kilma_PATCH_sozlesmesi_ve_pasife_alma()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 patch");
        var liste = await ListeAc(k, marj: 8m);
        using var db = vt.Olustur(k.TenantId);
        var servis = Servis(db);
        await servis.UrunFiyatiEkle(liste.Id,
            new UrunFiyatiDto { UrunId = k.UrunId, HasMarjYuzde = 1m, IscilikCarpanYuzde = 40m, Not = "toptancı" }, k.Islem);

        // Yalnız çarpan gönderilir → marj ve not KORUNUR.
        var g1 = await servis.UrunFiyatiGuncelle(liste.Id, k.UrunId,
            new UrunFiyatiDto { IscilikCarpanYuzde = 70m }, k.Islem);
        Assert.Equal(1m, g1.HasMarjYuzde);
        Assert.Equal(70m, g1.IscilikCarpanYuzde);
        Assert.Equal("toptancı", g1.Not);

        // Açık null → o alan SİLİNİR (listeden gelmeye başlar).
        var g2 = await servis.UrunFiyatiGuncelle(liste.Id, k.UrunId,
            new UrunFiyatiDto { HasMarjYuzde = null }, k.Islem);
        Assert.Null(g2.HasMarjYuzde);
        Assert.Equal(70m, g2.IscilikCarpanYuzde);

        // Pasife alma → fiyat LİSTEYE döner (kayıt silinmez).
        await servis.UrunFiyatiGuncelle(liste.Id, k.UrunId, new UrunFiyatiDto { Aktif = false }, k.Islem);
        var sonuc = await servis.ListeOnizleme(liste.Id, k.KurId, k.Islem);
        var satir = sonuc.Satirlar.Single(x => x.UrunId == k.UrunId);
        Assert.Equal("LISTE", satir.Kaynak);
        using var kontrol = vt.OlusturSistem();
        Assert.False(kontrol.FiyatListesiUrunFiyatlari.Single(x => x.TenantId == k.TenantId).Aktif);
    }

    /// <summary>
    /// M49-12 MEVZUAT (FEY md.11 / TRHTUY md.14): geçersiz kılma da "liste fiyatı"dır.
    /// Hareket.ListeFiyati geçersiz kılma UYGULANDIKTAN sonraki fiyattır; 30 günlük
    /// "indirim öncesi fiyat" taraması bu düşük fiyatı görür.
    /// </summary>
    [Fact]
    public async Task Gecersiz_kilmayla_dusen_fiyat_HAREKETE_yazilir_ve_30_gun_tabanini_dusurur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P49 mevzuat", stokAdet: 10m);
        var liste = await ListeAc(k, marj: 10m);
        var u = Urun(k); var kur = HasKuru(k);
        var listeFiyati = FiyatMotoru.Yuvarla(FiyatMotoru.Yuvarla(u.HasGrami * kur) * 1.10m + (u.IscilikTl ?? 0m));

        using var db = vt.Olustur(k.TenantId);
        await Servis(db).UrunFiyatiEkle(liste.Id,
            new UrunFiyatiDto { UrunId = k.UrunId, HasMarjYuzde = 0m }, k.Islem);
        var dusukFiyat = FiyatMotoru.Yuvarla(FiyatMotoru.Yuvarla(u.HasGrami * kur) + (u.IscilikTl ?? 0m));
        Assert.True(dusukFiyat < listeFiyati);

        await Satis(db).SatisFisi(Fis(k, new MSatis.SatisFisKalemDto { UrunId = k.UrunId, Adet = 1m, BirimFiyatTl = dusukFiyat }), k.Islem);

        using var kontrol = vt.OlusturSistem();
        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "SATIS");
        Assert.Equal(dusukFiyat, h.ListeFiyati);                         // geçersiz kılma SONRASI fiyat
        Assert.Equal(0m, h.IndirimTutari);                               // yeni liste fiyatı — indirim İLANI değil

        using var oku = vt.Olustur(k.TenantId);
        var oncesi = await new FiyatServisi(oku).IndirimOncesiFiyat(k.TenantId, k.UrunId, listeFiyati, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(dusukFiyat, oncesi);                                // 30 günün EN DÜŞÜĞÜ
    }
}
