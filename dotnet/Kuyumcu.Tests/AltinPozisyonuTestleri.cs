using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Pozisyon;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-2 — ALTIN POZİSYONU (servis düzeyi, gerçek MySQL)
//
// Kilitlenen kurallar:
//   • NET = VİTRİN + HURDA + CARİ ALACAK − CARİ BORÇ (maden başına)
//   • Altın ve gümüş has gramı TOPLANMAZ (K20-2 · P-45)
//   • Konsinye mal ve müşteri emaneti pozisyona GİRMEZ (bize ait değil / nötr)
//   • Kiracı izolasyonu — başka kiracının stoğu/carisi sayıya karışmaz
//   • cari_bakiyeler ↔ defter tutarsızlığı GİZLENMEZ (DefterBakiyeFarki)
//   • Kırılım listesi: yön, sıra, sınır, "daha fazlası var", 400'ler, KVKK izi
//
// Beklenen değerler test içinde ELLE yazılmış sabitlerden gelir; servis
// çıktısı kendisiyle kıyaslanmaz.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class AltinPozisyonuTestleri(TestVeritabani vt)
{
    // KiraciKur varsayılanı: K22 · 10 g · 100 adet → has/adet = 9.16 → stok 916 g has.
    private const decimal VarsayilanVitrin = 916m;

    private static PozisyonServisi Servis(KuyumcuDbContext db) => new(db, TestKurulum.Denetim(db));

    private static void Defter(KuyumcuDbContext db, Kiraci k, string tur, decimal has,
                               string? cariId, string maden = Maden.Altin)
    {
        var now = DateTime.UtcNow;
        HareketFabrikasi.Ekle(db, OdemeIzi.Yok, new Hareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            Tur = tur, Tarih = now, TlTutari = 0m, HasGrami = has, Maden = maden,
            ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = cariId,
            KullaniciId = k.KullaniciId, Olusturuldu = now,
        });
        db.SaveChanges();
    }

    private static string CariEkle(KuyumcuDbContext db, Kiraci k, string ad, decimal altinBakiye = 0m)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Cariler.Add(new Cari { Id = id, TenantId = k.TenantId, Tur = "MUSTERI", Ad = ad, Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.CariBakiyeler.Add(new CariBakiye { CariId = id, TenantId = k.TenantId, TlBakiye = 0m, HasGramBakiye = altinBakiye, Guncellendi = now });
        db.SaveChanges();
        return id;
    }

    private static void UrunEkle(KuyumcuDbContext db, Kiraci k, string ayarId, decimal hasBirim, decimal adet, bool konsinye = false)
    {
        var id = Kimlik.Ulid();
        var now = DateTime.UtcNow;
        db.Urunler.Add(new Urun
        {
            Id = id, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"B2-{id[..8]}", Ad = "B2 ürünü",
            AyarId = ayarId, Milyem = Ayarlar.Bul(ayarId)!.Milyem, GramAgirlik = 10m, TasAgirlik = 0m,
            HasGrami = hasBirim, IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
            Konsinye = konsinye, KonsinyeCariId = konsinye ? k.CariId : null,
            KonsinyeBirimBedel = konsinye ? 1000m : null,
        });
        db.Stoklar.Add(new Stok
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = id,
            Adet = adet, ToplamHasGram = adet * hasBirim, Guncellendi = now,
        });
        db.SaveChanges();
    }

    // ── B2_01: yalnız stok varken net = vitrin; kart dönemden bağımsızdır ─────
    [Fact]
    public async Task B2_01_yalniz_stok_varken_net_vitrine_esittir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-stok");
        using var db = vt.Olustur(k);
        var p = await Servis(db).Hesapla(k.Islem);

        Assert.Equal(Maden.Altin, p.AnaMaden);
        Assert.Equal(VarsayilanVitrin, p.Altin.VitrinGram);
        Assert.Equal(VarsayilanVitrin, p.Altin.NetGram);
        Assert.Equal(0m, p.Altin.CariAlacakGram);
        Assert.Equal(0m, p.Altin.CariBorcGram);
        Assert.Empty(p.DigerMadenler);
        Assert.False(p.MadenlerToplanmaz);
        Assert.True(p.DonemdenBagimsiz);
        Assert.Equal(0m, p.DefterBakiyeFarki);
    }

    // ── B2_02: cari alacak + borç ayrı ayrı sayılır, net = vitrin + A − B ──────
    [Fact]
    public async Task B2_02_cari_alacak_ve_borc_ayri_sayilir_ve_nete_isaretle_girer()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-cari");
        using (var s = vt.OlusturSistem())
        {
            var borclu = CariEkle(s, k, "Bize borçlu", altinBakiye: 14.4m);
            var alacakli = CariEkle(s, k, "Biz borçluyuz", altinBakiye: -22m);
            // K4 işaret sözleşmesi: VERESIYE HasGrami = −borç; düzeltme de aynı yön.
            Defter(s, k, "VERESIYE", -10m, borclu);
            Defter(s, k, "CARI_DUZELTME", -4.4m, borclu);
            Defter(s, k, "CARI_DUZELTME", 22m, alacakli);
            // Tahsilat bakiye azaltır: 2 g geldi, 2 g ek borç yazıldı → net etki 0.
            Defter(s, k, "VERESIYE", -2m, borclu);
            Defter(s, k, "TAHSILAT", 2m, borclu);
        }
        using var db = vt.Olustur(k);
        var p = await Servis(db).Hesapla(k.Islem);

        Assert.Equal(14.4m, p.Altin.CariAlacakGram);
        Assert.Equal(22m, p.Altin.CariBorcGram);
        Assert.Equal(1, p.Altin.AlacakliCariSayisi);
        Assert.Equal(1, p.Altin.BorcluOlduguCariSayisi);
        Assert.Equal(VarsayilanVitrin + 14.4m - 22m, p.Altin.NetGram);
        Assert.Equal(0m, p.DefterBakiyeFarki);
    }

    // ── B2_03: altın ve gümüş TOPLANMAZ ────────────────────────────────────────
    [Fact]
    public async Task B2_03_gumus_has_grami_altina_eklenmez_ayri_satirda_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-maden");
        using (var s = vt.OlusturSistem())
        {
            UrunEkle(s, k, "G925", 9.25m, 20m);                 // 185 g gümüş
            var c = CariEkle(s, k, "Gümüş borçlusu");
            Defter(s, k, "VERESIYE", -5m, c, Maden.Gumus);       // 5 g gümüş alacak
        }
        using var db = vt.Olustur(k);
        var p = await Servis(db).Hesapla(k.Islem);

        Assert.Equal(VarsayilanVitrin, p.Altin.NetGram);           // gümüş KARIŞMADI
        Assert.Equal(0m, p.Altin.CariAlacakGram);
        Assert.True(p.MadenlerToplanmaz);
        var g = Assert.Single(p.DigerMadenler);
        Assert.Equal(Maden.Gumus, g.Maden);
        Assert.Equal(185m, g.VitrinGram);
        Assert.Equal(5m, g.CariAlacakGram);
        Assert.Equal(190m, g.NetGram);
    }

    // ── B2_04: emanet nötrdür; konsinye bize ait değildir ─────────────────────
    [Fact]
    public async Task B2_04_emanet_ve_konsinye_pozisyona_girmez_bilgi_olarak_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-emanet");
        using (var s = vt.OlusturSistem())
        {
            // Emanet: 7 g geldi, 2 g iade → elde 5 g; cari_bakiyeler −5 (biz borçluyuz).
            var c = CariEkle(s, k, "Emanetçi", altinBakiye: -5m);
            Defter(s, k, "EMANET_GIRIS", 7m, c);
            Defter(s, k, "EMANET_CIKIS", -2m, c);
            UrunEkle(s, k, "K22", 9.16m, 5m, konsinye: true);    // 45,8 g konsinye
        }
        using var db = vt.Olustur(k);
        var p = await Servis(db).Hesapla(k.Islem);

        Assert.Equal(VarsayilanVitrin, p.Altin.NetGram);
        Assert.Equal(VarsayilanVitrin, p.Altin.VitrinGram);
        Assert.Equal(5m, p.Altin.EmanetGram);
        Assert.Equal(45.8m, p.Altin.KonsinyeGram);
        Assert.Equal(0m, p.Altin.CariBorcGram);       // emanet cari borcu olarak SAYILMAZ
        Assert.Equal(0m, p.DefterBakiyeFarki);        // emanet dahil tablo = defter
    }

    // ── B2_05: hurda elde duran metaldir, pozisyona girer ─────────────────────
    [Fact]
    public async Task B2_05_hurda_has_grami_pozisyona_eklenir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-hurda");
        using (var s = vt.OlusturSistem())
        {
            Defter(s, k, "HURDA_ALIS", 4m, k.CariId);
            Defter(s, k, "HURDA_SATIS", -1.5m, null);
        }
        using var db = vt.Olustur(k);
        var p = await Servis(db).Hesapla(k.Islem);

        Assert.Equal(2.5m, p.Altin.HurdaGram);
        Assert.Equal(VarsayilanVitrin + 2.5m, p.Altin.NetGram);
        Assert.Equal(0m, p.Altin.CariAlacakGram);    // hurda alışı cari has bakiyesi DEĞİLDİR
    }

    // ── B2_06: kiracı izolasyonu ──────────────────────────────────────────────
    [Fact]
    public async Task B2_06_baska_kiracinin_stogu_ve_carisi_sayiya_karismaz()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-izo-a");
        var yabanci = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-izo-b", stokAdet: 999m);
        using (var s = vt.OlusturSistem())
        {
            var c = CariEkle(s, yabanci, "Yabancı borçlu", altinBakiye: 50m);
            Defter(s, yabanci, "VERESIYE", -50m, c);
            Defter(s, yabanci, "HURDA_ALIS", 30m, null);
        }
        using var db = vt.Olustur(k);
        var p = await Servis(db).Hesapla(k.Islem);

        Assert.Equal(VarsayilanVitrin, p.Altin.NetGram);
        Assert.Equal(0m, p.Altin.CariAlacakGram);
        Assert.Equal(0m, p.Altin.HurdaGram);
        Assert.Equal(0m, p.DefterBakiyeFarki);

        var liste = await Servis(db).Cariler(k.Islem, "ALTIN", "ALACAK", 50, 0);
        Assert.Empty(liste.Satirlar);
        Assert.Equal(0, liste.ToplamSatir);
    }

    // ── B2_11: global süzgeç OLMADAN da izolasyon (savunma derinliği) ────────
    // Sistem bağlamında HasQueryFilter devre dışıdır; servisin kendi açık
    // tenant_id koşulları tek başına yabancı kiracıyı dışarıda tutmalıdır.
    [Fact]
    public async Task B2_11_sistem_baglaminda_da_acik_tenant_kosulu_izolasyonu_saglar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-derin-a");
        var yabanci = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-derin-b");
        using (var s = vt.OlusturSistem())
        {
            var c = CariEkle(s, yabanci, "Yabancı", altinBakiye: 3m);
            Defter(s, yabanci, "VERESIYE", -3m, c);
            Defter(s, yabanci, "HURDA_ALIS", 2m, null);
            Defter(s, yabanci, "EMANET_GIRIS", 1m, c);
        }
        using var db = vt.OlusturSistem();
        var p = await Servis(db).Hesapla(k.Islem);

        Assert.Equal(VarsayilanVitrin, p.Altin.VitrinGram);
        Assert.Equal(0m, p.Altin.CariAlacakGram);
        Assert.Equal(0m, p.Altin.HurdaGram);
        Assert.Equal(0m, p.Altin.EmanetGram);
        Assert.Equal(0m, p.DefterBakiyeFarki);

        var liste = await Servis(db).Cariler(k.Islem, "ALTIN", "ALACAK", 50, 0);
        Assert.Equal(0, liste.ToplamSatir);
    }

    // ── B2_07: tablo ↔ defter ayrışması gizlenmez ─────────────────────────────
    [Fact]
    public async Task B2_07_cari_bakiye_tablosu_defterden_ayrisirsa_fark_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-fark");
        using (var s = vt.OlusturSistem())
        {
            var c = CariEkle(s, k, "Ayrışık", altinBakiye: 12m);  // defterde 10
            Defter(s, k, "VERESIYE", -10m, c);
        }
        using var db = vt.Olustur(k);
        var p = await Servis(db).Hesapla(k.Islem);

        Assert.Equal(10m, p.Altin.CariAlacakGram);   // pozisyon DEFTERDEN
        Assert.Equal(2m, p.DefterBakiyeFarki);        // tablo 12 − defter 10
    }

    // ── B2_08: kırılım listesi — yön, sıra, sınır, daha fazlası, KVKK izi ─────
    [Fact]
    public async Task B2_08_cari_kirilimi_yon_sira_sinir_ve_denetim_izi()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-liste");
        string c1, c2, c3, b1;
        using (var s = vt.OlusturSistem())
        {
            c1 = CariEkle(s, k, "Az borçlu"); Defter(s, k, "VERESIYE", -1m, c1);
            c2 = CariEkle(s, k, "Çok borçlu"); Defter(s, k, "VERESIYE", -9m, c2);
            c3 = CariEkle(s, k, "Orta borçlu"); Defter(s, k, "VERESIYE", -4m, c3);
            b1 = CariEkle(s, k, "Alacaklı toptancı"); Defter(s, k, "CARI_DUZELTME", 6m, b1);
        }
        using var db = vt.Olustur(k);
        var once = await db.DenetimKayitlari.CountAsync(d => d.TenantId == k.TenantId);

        var sayfa = await Servis(db).Cariler(k.Islem, null, null, 2, 0);   // varsayılan ALTIN/ALACAK
        Assert.Equal("ALTIN", sayfa.Maden);
        Assert.Equal("ALACAK", sayfa.Yon);
        Assert.Equal(3, sayfa.ToplamSatir);
        Assert.True(sayfa.DahaFazlasiVar);
        Assert.Equal(new[] { c2, c3 }, sayfa.Satirlar.Select(x => x.CariId).ToArray());
        Assert.Equal(9m, sayfa.Satirlar[0].HasGram);
        Assert.Equal("Çok borçlu", sayfa.Satirlar[0].CariAd);

        var borc = await Servis(db).Cariler(k.Islem, "altin", "borc", 50, 0);
        var satir = Assert.Single(borc.Satirlar);
        Assert.Equal(b1, satir.CariId);
        Assert.Equal(-6m, satir.HasGram);
        Assert.False(borc.DahaFazlasiVar);

        var sonra = await db.DenetimKayitlari.CountAsync(d => d.TenantId == k.TenantId);
        Assert.Equal(once + 2, sonra);   // her liste okuması iz bırakır (M115)
    }

    // ── B2_09: geçersiz girdi 400 (sessiz varsayılan YOK) ─────────────────────
    [Theory]
    [InlineData("BAKIR", "ALACAK", 50, 0)]
    [InlineData("ALTIN", "YANLIS", 50, 0)]
    [InlineData("ALTIN", "ALACAK", 0, 0)]
    [InlineData("ALTIN", "ALACAK", 501, 0)]
    [InlineData("ALTIN", "ALACAK", 50, -1)]
    public async Task B2_09_kirilim_gecersiz_girdiyi_400_ile_reddeder(string maden, string yon, int limit, int ofset)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-400");
        using var db = vt.Olustur(k);
        await Assert.ThrowsAsync<BadRequestException>(() => Servis(db).Cariler(k.Islem, maden, yon, limit, ofset));
    }

    // ── B2_10: storno satırı (aynı tür, ters has) bakiyeyi sıfırlar ───────────
    [Fact]
    public async Task B2_10_stornolanan_veresiye_pozisyondan_duser()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKETB2-TEST-storno");
        using (var s = vt.OlusturSistem())
        {
            var c = CariEkle(s, k, "Stornolu");
            Defter(s, k, "VERESIYE", -8m, c);
            Defter(s, k, "VERESIYE", 8m, c);   // StornoServisi: tür korunur, HasGrami ters
        }
        using var db = vt.Olustur(k);
        var p = await Servis(db).Hesapla(k.Islem);
        Assert.Equal(0m, p.Altin.CariAlacakGram);
        Assert.Equal(0, p.Altin.AlacakliCariSayisi);
        Assert.Equal(VarsayilanVitrin, p.Altin.NetGram);
    }

    // ── PAKET-37 (20.10): TL net varlık — kasa+banka para birimi bazında, altın has değeri, cari TL ──
    [Fact]
    public async Task NetVarlik_kasa_banka_para_birimi_bazinda_toplanir_ve_TL_servet_sunucuda_hesaplanir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P37-net-varlik");
        using var db = vt.Olustur(k);
        var now = DateTime.UtcNow;
        db.Kasalar.Add(new Kasa { Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Döviz kasası", ParaBirimi = "USD", Bakiye = 1_000m, Aktif = true, Guncellendi = now });
        db.Kasalar.Add(new Kasa { Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Kapalı kasa", ParaBirimi = "TRY", Bakiye = 99_999m, Aktif = false, Guncellendi = now });   // pasif: sayılmaz
        db.BankaHesaplari.Add(new BankaHesap { Id = Kimlik.Ulid(), TenantId = k.TenantId, BankaAdi = "Ziraat", HesapAdi = "Vadesiz", Iban = TestIban.Tamamla(new string('1', 22)), ParaBirimi = "TRY", Bakiye = 50_000m, Aktif = true, Olusturuldu = now, Guncellendi = now });
        db.BankaHesaplari.Add(new BankaHesap { Id = Kimlik.Ulid(), TenantId = k.TenantId, BankaAdi = "Ziraat", HesapAdi = "USD", Iban = TestIban.Tamamla(new string('2', 22)), ParaBirimi = "USD", Bakiye = 200m, Aktif = true, Olusturuldu = now, Guncellendi = now });
        var kur = await db.KurSnapshots.SingleAsync(x => x.Id == k.KurId);
        kur.UsdTry = 35m;
        (await db.CariBakiyeler.SingleAsync(x => x.CariId == k.CariId)).TlBakiye = 1_500m;   // müşteri bize 1.500 TL borçlu
        await db.SaveChangesAsync();

        var p = await Servis(db).Hesapla(k.Islem);
        var nv = p.NetVarlik!;

        var kasaTry = await db.Kasalar.Where(x => x.TenantId == k.TenantId && x.Aktif && x.ParaBirimi == "TRY").SumAsync(x => x.Bakiye);
        var usd = Assert.Single(nv.ParaBirimleri, x => x.ParaBirimi == "USD");
        Assert.Equal(1_000m, usd.Kasa); Assert.Equal(200m, usd.Banka); Assert.Equal(1_200m, usd.Toplam); Assert.Equal(35m, usd.Kur); Assert.Equal(42_000m, usd.TlKarsiligi);
        var tr = Assert.Single(nv.ParaBirimleri, x => x.ParaBirimi == "TRY");
        Assert.Equal(kasaTry, tr.Kasa); Assert.Equal(50_000m, tr.Banka);
        Assert.Equal(kasaTry + 50_000m + 42_000m, nv.NakitTl);
        Assert.Equal(6_827.20m, nv.HasFiyati);
        Assert.Equal(Math.Round(p.Altin.NetGram * 6_827.20m, 2, MidpointRounding.AwayFromZero), nv.AltinTl);
        var cariTl = await db.CariBakiyeler.Where(x => x.TenantId == k.TenantId).SumAsync(x => x.TlBakiye);
        Assert.Equal(1_500m, cariTl);
        Assert.Equal(cariTl, nv.CariTl);
        Assert.Equal(nv.NakitTl + nv.AltinTl + nv.CariTl, nv.NetVarlikTl);
        Assert.Empty(nv.Eksikler);
    }

    [Fact]
    public async Task NetVarlik_kuru_olmayan_para_birimi_toplami_NULL_yapar_ve_sebebi_yazar()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "P37-kursuz");
        using var db = vt.Olustur(k);
        db.Kasalar.Add(new Kasa { Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Sterlin", ParaBirimi = "GBP", Bakiye = 10m, Aktif = true, Guncellendi = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var nv = (await Servis(db).Hesapla(k.Islem)).NetVarlik!;
        var gbp = Assert.Single(nv.ParaBirimleri, x => x.ParaBirimi == "GBP");
        Assert.Null(gbp.Kur); Assert.Null(gbp.TlKarsiligi);
        Assert.Null(nv.NakitTl); Assert.Null(nv.NetVarlikTl);
        Assert.Contains("KUR_GBP", nv.Eksikler);
        Assert.NotNull(nv.AltinTl);   // has fiyatı var: altın kalemi yine hesaplanır
    }
}
