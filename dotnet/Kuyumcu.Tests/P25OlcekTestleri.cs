using System.Reflection;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Rapor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-25 — ÖLÇEK + KÜLTÜR + RAPOR TAMAMLAMA [veritabanı + saf testler]
//
// Kimlikler: B-8 (ölçek/kültür borcu) · Y-16 (16. rapor) · Y-31 (rapor sayısı
// tutarlılığı) · R-6 (yük testi) · B-64 (geçersiz süzgeçte bayat tablo).
//
// Sınanan değişmezler:
//   • GunlukOzet / AylikOzet / PosFaturaMutabakati SUNUCUDA toplar — dönemin
//     satırlarını belleğe ALMAZ (kaynak düzeyinde kanıt + sayısal denklik)
//   • Sunucuda toplama rakamları DEĞİŞTİRMEZ (maden ayrımı, işaret, mutlak)
//   • R15 yaş kovaları, devir hızı, ölü stok sayfası
//   • Partisi olmayan stok "0-30 gün"e atılmaz, AYRI durur (Anayasa md.1)
//   • Devir hızı payda sıfırken NULL — sıfır DEĞİL
//   • Rapor sayısı KATALOGDAN okunur; FazAAsgari katalogla birebir
//   • Personel raporu KAYDEDEN der (adı da, ölçütü de)
//   • Kiracı izolasyonu: yabancı kiracının partisi sayılmaz
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class P25OlcekTestleri(TestVeritabani vt)
{
    private static object? Alan(object kaynak, string ad) =>
        kaynak.GetType().GetProperty(ad)?.GetValue(kaynak);

    private static object Alt(object kaynak, string ad) => Alan(kaynak, ad)!;

    private static IEnumerable<object> Liste(object kaynak, string ad)
        => ((System.Collections.IEnumerable)Alan(kaynak, ad)!).Cast<object>();

    private static Hareket Satir(
        Kiraci k, string tur, decimal tl, decimal has, string? maden = null,
        DateTime? tarih = null, decimal? veresiye = null)
        => new()
        {
            Id = Kimlik.Ulid(),
            TenantId = k.TenantId,
            SubeId = k.SubeId,
            Tur = tur,
            Tarih = tarih ?? DateTime.UtcNow,
            TlTutari = tl,
            HasGrami = has,
            Maden = maden ?? "ALTIN",
            ParaBirimi = "TRY",
            VeresiyeTutar = veresiye,
            KurSnapshotId = k.KurId,
            KullaniciId = k.KullaniciId,
            Olusturuldu = DateTime.UtcNow,
        };

    private static StokPartisi Parti(
        Kiraci k, DateTime giris, decimal kalan, decimal hasBirim,
        decimal? maliyet = null, string? urunId = null)
        => new()
        {
            Id = Kimlik.Ulid(),
            TenantId = k.TenantId,
            SubeId = k.SubeId,
            UrunId = urunId ?? k.UrunId,
            Kaynak = PartiKaynagi.Alis,
            // ⚠ GİREN ≠ KALAN: yaşlandırma RAFTA KALANI ölçer, girmiş olanı DEĞİL.
            //   Eşit bırakılsaydı AdetKalan→AdetGiren mutasyonu testi kırmazdı.
            AdetGiren = kalan * 3m,
            AdetKalan = kalan,
            HasGramBirim = hasBirim,
            BirimMaliyetTl = maliyet,
            GirisTarihi = giris,
            Olusturuldu = giris,
        };

    // ═════════════════════════════════════════════════════════════════════════
    // B-8 — SUNUCUDA TOPLAMA
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// M25-1 — GunlukOzet maden ayrımını ve işaretleri SUNUCU toplamasından
    /// SONRA da doğru üretir. `HareketToplami.Coz` mutlak/işaretli toplamları
    /// karıştırırsa bu test KIRMIZI olur.
    /// </summary>
    [Fact]
    public async Task Gunluk_ozet_sunucuda_toplar_ve_madenleri_ayirir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST gunluk");
        var bugun = Zaman.YerelBugun();
        var an = Zaman.YerelGunBasiUtc(bugun).AddHours(10);

        // Satış: has NEGATİF yazılır (mal çıkar) — rapor MUTLAK toplar.
        sistem.Hareketler.Add(Satir(k, "SATIS", 10_000m, -2.5m, "ALTIN", an));
        sistem.Hareketler.Add(Satir(k, "SATIS", 4_000m, -1.5m, "ALTIN", an));
        // Gümüş satışı ALTIN has toplamına KATILMAZ.
        sistem.Hareketler.Add(Satir(k, "SATIS", 500m, -100m, "GUMUS", an));
        // Alış: TL negatif yazılır — rapor MUTLAK toplar.
        sistem.Hareketler.Add(Satir(k, "ALIS", -6_000m, 3m, "ALTIN", an));
        await sistem.SaveChangesAsync();

        var db = vt.Olustur(k);
        var o = await TestKurulum.RaporServisi(db).GunlukOzet(bugun.ToString("yyyy-MM-dd"), k.Islem);

        var satis = Alt(o, "satis");
        Assert.Equal(3, Alan(satis, "adet"));
        Assert.Equal(14_500m, Alan(satis, "toplamTl"));
        // K20-2: YALNIZ ALTIN — 100 g gümüş toplanmaz.
        Assert.Equal(4.0m, Alan(satis, "toplamHas"));

        var alis = Alt(o, "alis");
        Assert.Equal(6_000m, Alan(alis, "toplamTl"));   // mutlak
        Assert.Equal(3.0m, Alan(alis, "toplamHas"));

        // netHas = tüm hareketlerin İŞARETLİ altın has toplamı: -2,5 -1,5 +3 = -1
        Assert.Equal(-1.0m, Alan(o, "netHasGramHareketi"));
        Assert.Equal(4, Alan(o, "hareketSayisi"));
        Assert.NotNull(Alan(o, "madenUyarisi"));        // iki maden var
    }

    /// <summary>
    /// M25-2 — AylikOzet günlük kırılımı SUNUCUDA gruplar ve yerel güne
    /// doğru düşürür. Saat dilimi kayması olursa ciro yanlış güne düşer (K5).
    /// </summary>
    [Fact]
    public async Task Aylik_ozet_gunluk_kirilimi_yerel_gune_duser()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST aylik");
        var bugun = Zaman.YerelBugun();
        // Yerel günün İLK saniyesi: UTC'de bir ÖNCEKİ güne düşer.
        var yerelGunBasi = Zaman.YerelGunBasiUtc(bugun).AddMinutes(30);

        sistem.Hareketler.Add(Satir(k, "SATIS", 7_777m, -1m, "ALTIN", yerelGunBasi));
        sistem.Hareketler.Add(Satir(k, "ALIS", -2_222m, 1m, "ALTIN", yerelGunBasi));
        await sistem.SaveChangesAsync();

        var db = vt.Olustur(k);
        var o = await TestKurulum.RaporServisi(db).AylikOzet(bugun.Year, bugun.Month, k.Islem);

        var gunler = Liste(o, "gunler").ToList();
        var hedef = gunler.Single(g => (string)Alan(g, "tarih")! == bugun.ToString("yyyy-MM-dd"));
        Assert.Equal(7_777m, Alan(hedef, "satisTl"));
        Assert.Equal(2_222m, Alan(hedef, "alisTl"));   // mutlak
        Assert.Equal(2, Alan(o, "hareketSayisi"));
    }

    /// <summary>
    /// M25-3 — aylık veresiye payı sunucuda toplanır; borç DOĞURAN ve KAPATAN
    /// yön ayrı ayrı. İki sorgu birbirine karışırsa test KIRMIZI olur.
    /// </summary>
    [Fact]
    public async Task Aylik_ozet_veresiye_paylarini_sunucuda_ayirir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST veresiye");
        var bugun = Zaman.YerelBugun();
        var an = Zaman.YerelGunBasiUtc(bugun).AddHours(11);

        sistem.Hareketler.Add(Satir(k, "SATIS", 9_000m, -1m, "ALTIN", an, veresiye: 4_000m));
        sistem.Hareketler.Add(Satir(k, "TAHSILAT", -1_500m, 0m, null, an, veresiye: 1_500m));
        await sistem.SaveChangesAsync();

        var db = vt.Olustur(k);
        var o = await TestKurulum.RaporServisi(db).AylikOzet(bugun.Year, bugun.Month, k.Islem);

        var v = Alt(o, "veresiye");
        Assert.Equal(4_000m, Alan(v, "toplamTl"));
        Assert.Equal(1_500m, Alan(v, "kapananTl"));
        Assert.Equal(1_500m, Alan(Alt(o, "tahsilat"), "cariTahsilatiTl"));
    }

    /// <summary>
    /// M25-4 — MEKANİZMA TESTİ. Üç özet ucu dönemin satırlarını belleğe
    /// ALMAZ: kaynakta `HareketToplami.Coz` çağrısı vardır ve sınırsız
    /// `Hareketler…ToListAsync()` yoktur. Biri eski desene dönerse KIRMIZI.
    /// (denetim.sh SINIRSIZ_TOLIST sayacının kod içi ikizi.)
    /// </summary>
    [Fact]
    public void Ozet_uclari_sunucuda_toplar_kaynak_kanibi()
    {
        var yol = KaynakYolu("Kuyumcu.Api/Modules/Rapor/RaporModulu.cs");
        var metin = File.ReadAllText(yol);

        foreach (var uc in new[] { "GunlukOzet", "AylikOzet", "PosFaturaMutabakati" })
        {
            var govde = Govde(metin, uc);
            Assert.Contains("HareketToplami.Coz", govde);
            Assert.DoesNotContain("var tumHareketler", govde);
        }

        // PosFaturaMutabakati satır satır eşleştirdiği kümeleri TAVANLA okur.
        var pos = Govde(metin, "PosFaturaMutabakati");
        Assert.Contains("MutabakatSatirTavani", pos);
        Assert.DoesNotContain("var hareketler = await db.Hareketler", pos);
    }

    /// <summary>
    /// M25-5 — mutabakat tavanına değildiğinde rapor SUSMAZ (UX-S29).
    /// Sessiz kırpma "mutabık" yalanı üretirdi.
    /// </summary>
    [Fact]
    public void Mutabakat_tavani_makul_ve_uyari_baglidir()
    {
        Assert.True(RaporServisi.MutabakatSatirTavani > 0);
        var metin = File.ReadAllText(KaynakYolu("Kuyumcu.Api/Modules/Rapor/RaporModulu.cs"));
        var pos = Govde(metin, "PosFaturaMutabakati");
        Assert.Contains("satirTavani", pos);
        Assert.Contains("sınırına değdi", pos);
    }

    /// <summary>
    /// M25-6 — POS mutabakatında SATIŞ TOPLAMI sayfadan DEĞİL sunucudan gelir.
    /// Sayfadan türetilseydi tavana değen günde FARK sessizce sıfırlanırdı.
    /// </summary>
    [Fact]
    public async Task Mutabakat_satis_toplamini_sunucudan_alir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST mutabakat");
        var bugun = Zaman.YerelBugun();
        var an = Zaman.YerelGunBasiUtc(bugun).AddHours(12);

        sistem.Hareketler.Add(Satir(k, "SATIS", 12_000m, -2m, "ALTIN", an));
        sistem.Hareketler.Add(Satir(k, "SATIS", 8_000m, -1m, "ALTIN", an));
        sistem.Hareketler.Add(Satir(k, "ALIS", -5_000m, 2m, "ALTIN", an));
        await sistem.SaveChangesAsync();

        var db = vt.Olustur(k);
        var o = await TestKurulum.RaporServisi(db)
            .PosFaturaMutabakati(bugun.ToString("yyyy-MM-dd"), k.Islem);

        var satis = Alt(o, "satis");
        Assert.Equal(2, Alan(satis, "sayisi"));
        Assert.Equal(20_000m, Alan(satis, "toplamTl"));
        Assert.Equal(5_000m, Alan(Alt(o, "alis"), "toplamTl"));   // mutlak
        Assert.False((bool)Alan(Alt(o, "satirTavani"), "satisTavanaDegdi")!);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Y-16 — R15 STOK YAŞLANDIRMA / DEVİR HIZI
    // ═════════════════════════════════════════════════════════════════════════

    private StokYaslandirmaServisi Servis(Kiraci k)
    {
        var db = vt.Olustur(k);
        return new StokYaslandirmaServisi(db, new DenetimServisi(db, new HttpContextAccessor()));
    }

    /// <summary>M25-7 — yaş kovaları giriş tarihine göre doğru dolar.</summary>
    [Fact]
    public async Task Yaslandirma_kovalari_giris_tarihine_gore_dolar()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST kova", stokAdet: 0m);
        var simdi = DateTime.UtcNow;

        sistem.Add(Parti(k, simdi.AddDays(-5), 2m, 1m, 1_000m));      // G0_30
        sistem.Add(Parti(k, simdi.AddDays(-45), 3m, 1m, 1_000m));     // G31_90
        sistem.Add(Parti(k, simdi.AddDays(-200), 4m, 1m, 1_000m));    // G181_365
        sistem.Add(Parti(k, simdi.AddDays(-500), 5m, 1m, null));      // G365_USTU, maliyetsiz
        await sistem.SaveChangesAsync();

        var o = await Servis(k).Yaslandirma(k.Islem, null);
        var kovalar = Liste(o, "kovalar").ToDictionary(x => (string)Alan(x, "kod")!);

        Assert.Equal(2m, Alan(kovalar["G0_30"], "adet"));
        Assert.Equal(3m, Alan(kovalar["G31_90"], "adet"));
        Assert.Equal(0m, Alan(kovalar["G91_180"], "adet"));
        Assert.Equal(4m, Alan(kovalar["G181_365"], "adet"));
        Assert.Equal(5m, Alan(kovalar["G365_USTU"], "adet"));

        // Anayasa md.1: maliyeti bilinmeyen adet AYRI yazılır, sıfır maliyet SAYILMAZ.
        Assert.Equal(5m, Alan(kovalar["G365_USTU"], "maliyetiBilinmeyenAdet"));
        Assert.Equal(0m, Alan(kovalar["G365_USTU"], "maliyetTl"));
        Assert.Equal(14m, Alan(o, "toplamAdet"));
    }

    /// <summary>
    /// M25-8 — partisi OLMAYAN stok "0-30 gün" kovasına atılmaz; yaşı
    /// BİLİNMİYOR olarak ayrı durur ve uyarı basılır (Anayasa md.1).
    /// </summary>
    [Fact]
    public async Task Partisiz_stok_yasi_bilinmeyen_olarak_ayri_durur()
    {
        var sistem = vt.OlusturSistem();
        // KiraciKur 100 adet stok kurar ama PARTİ açmaz → yaşı bilinmez.
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST partisiz", stokAdet: 40m);
        sistem.Add(Parti(k, DateTime.UtcNow.AddDays(-10), 10m, 1m, 500m));
        await sistem.SaveChangesAsync();

        var o = await Servis(k).Yaslandirma(k.Islem, null);
        var kovalar = Liste(o, "kovalar").ToDictionary(x => (string)Alan(x, "kod")!);

        Assert.Equal(10m, Alan(kovalar["G0_30"], "adet"));   // 40 DEĞİL
        Assert.Equal(30m, Alan(o, "yasiBilinmeyenAdet"));
        Assert.Contains(Liste(o, "uyarilar").Cast<string>(), u => u.Contains("BİLİNMİYOR"));
    }

    /// <summary>
    /// M25-9 — rafta mal yokken devir hızı NULL döner; sıfır YAZILMAZ.
    /// `devirHizi = 0` yazan bir uygulama "mal dönmüyor" derdi, oysa mal YOK.
    /// </summary>
    [Fact]
    public async Task Devir_hizi_rafta_mal_yokken_null_doner()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST bosraf", stokAdet: 0m);
        await sistem.SaveChangesAsync();

        var o = await Servis(k).Yaslandirma(k.Islem, null);
        var d = Alt(o, "devir");

        Assert.Null(Alan(d, "devirHizi"));
        Assert.Null(Alan(d, "yillikTahmini"));
        Assert.Null(Alan(d, "ortalamaRafOmruGun"));
        Assert.Equal(RaporGuvenilirligi.VeriYok, Alan(o, "guvenilirlik"));
    }

    /// <summary>M25-10 — devir hızı tüketim/kalan oranıdır ve pencereyle ölçeklenir.</summary>
    [Fact]
    public async Task Devir_hizi_tuketimden_hesaplanir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST devir", stokAdet: 0m);
        var parti = Parti(k, DateTime.UtcNow.AddDays(-100), 20m, 1m, 1_000m);
        sistem.Add(parti);
        sistem.Add(new StokPartiTuketimi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            PartiId = parti.Id, UrunId = k.UrunId, Adet = 5m,
            BirimMaliyetTl = 1_000m, Tarih = DateTime.UtcNow.AddDays(-10),
        });
        // Pencere DIŞINDA kalan tüketim sayılmaz.
        sistem.Add(new StokPartiTuketimi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId,
            PartiId = parti.Id, UrunId = k.UrunId, Adet = 99m,
            BirimMaliyetTl = 1_000m, Tarih = DateTime.UtcNow.AddDays(-300),
        });
        await sistem.SaveChangesAsync();

        var o = await Servis(k).Yaslandirma(k.Islem, null, devirPenceresiGun: 90);
        var d = Alt(o, "devir");

        Assert.Equal(5m, Alan(d, "tuketilenAdet"));
        Assert.Equal(20m, Alan(d, "kalanAdet"));
        Assert.Equal(0.25m, Alan(d, "devirHizi"));
        Assert.Equal(360.0m, Alan(d, "ortalamaRafOmruGun"));   // 20 × 90 / 5
    }

    /// <summary>
    /// M25-11 — ölü stok sayfalıdır ve toplam satır SAYFADAN TÜRETİLMEZ
    /// (TB-19 sözleşmesi); sınıra değildiyse "daha fazlası var" der (UX-S29).
    /// </summary>
    [Fact]
    public async Task Olu_stok_sayfalidir_ve_toplam_sayfadan_turetilmez()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST olu", stokAdet: 0m);
        var eski = DateTime.UtcNow.AddDays(-400);
        for (var i = 0; i < 4; i++)
        {
            var urunId = Kimlik.Ulid();
            sistem.Urunler.Add(new Urun
            {
                Id = urunId, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"OLU-{i}",
                Ad = $"Ölü ürün {i}", AyarId = "K22", Milyem = 916m, GramAgirlik = 1m,
                HasGrami = 0.916m, Aktif = true,
                Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            sistem.Add(Parti(k, eski.AddDays(i), 1m, 0.916m, 100m, urunId));
        }
        await sistem.SaveChangesAsync();

        var o = await Servis(k).Yaslandirma(k.Islem, null, limit: 2);
        var olu = Alt(o, "oluStok");

        Assert.Equal(4, Alan(olu, "toplamSatir"));     // CountAsync — sayfadan DEĞİL
        Assert.Equal(2, Liste(olu, "urunler").Count());
        Assert.True((bool)Alan(olu, "dahaFazlasiVar")!);
        // En eski mal ÖNCE gelir.
        Assert.Equal("OLU-0", Alan(Liste(olu, "urunler").First(), "kod"));
    }

    /// <summary>M25-12 — yabancı kiracının partisi SAYILMAZ (izolasyon).</summary>
    [Fact]
    public async Task Yaslandirma_yabanci_kiracinin_partisini_saymaz()
    {
        var sistem = vt.OlusturSistem();
        var a = TestKurulum.KiraciKur(sistem, "PAKET25-TEST izolasyon A", stokAdet: 0m);
        var b = TestKurulum.KiraciKur(sistem, "PAKET25-TEST izolasyon B", stokAdet: 0m);
        sistem.Add(Parti(a, DateTime.UtcNow.AddDays(-5), 7m, 1m, 100m));
        sistem.Add(Parti(b, DateTime.UtcNow.AddDays(-5), 999m, 1m, 100m));
        await sistem.SaveChangesAsync();

        var o = await Servis(a).Yaslandirma(a.Islem, null);
        Assert.Equal(7m, Alan(o, "toplamAdet"));

        // ⚠ TEST AÇIĞI KAPATILDI (P-25 mutasyon M25-2, 2026-09-22):
        //   Yalnız yukarıdaki iddia varken servisteki AÇIK `p.TenantId ==
        //   islem.TenantId` süzgeci silinse bile test YEŞİL kalıyordu, çünkü
        //   global sorgu süzgeci zaten kapsıyordu. Yani ikinci savunma hattı
        //   SINANMIYORDU. Jetonun kiracısı ile bağlamın kiracısı AYRIŞIRSA
        //   (yanlış kapsamlı DbContext, devralınan bağlam) açık süzgeç tek
        //   koruyucudur.
        //
        //   ÖLÇÜLEN GERÇEK (md.1): bağlam A'ya, işlem B'ye bakarken çağrı
        //   BOŞ SONUÇ döndürmüyor, `KiraciIhlaliException` ile DURUYOR —
        //   denetim kaydı yazma denetçisi üçüncü hattı kapatıyor. Fail-closed
        //   davranış budur ve ADIYLA sınanır.
        await Assert.ThrowsAsync<KiraciIhlaliException>(
            () => Servis(a).Yaslandirma(b.Islem, null));

        //   Açık süzgecin KENDİSİ ise kaynak düzeyinde kilitlenir: yukarıdaki
        //   iki hat (global süzgeç + yazma denetçisi) onu davranışsal olarak
        //   gölgelediği için silinmesi hiçbir testi kırmıyordu (M25-2 açığı).
        //   Bu satır, ikinci savunma hattının kodda DURDUĞUNU sınar.
        var kaynak = File.ReadAllText(
            KaynakYolu("Kuyumcu.Api/Modules/Rapor/StokYaslandirmaRaporu.cs"));
        Assert.Contains("p.TenantId == islem.TenantId && p.AdetKalan > 0m", kaynak);
        Assert.Contains("s.TenantId == islem.TenantId", kaynak);
        Assert.Contains("t.TenantId == islem.TenantId", kaynak);
    }

    /// <summary>
    /// M25-13 — limit kapısı SERVİSTE durur ([Range] servis yolunda çalışmaz).
    /// Eşik sınırları da serviste doğrulanır (UX-S30).
    /// </summary>
    [Fact]
    public async Task Yaslandirma_limit_ve_esik_kapisi_serviste_durur()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET25-TEST kapi", stokAdet: 0m);
        await sistem.SaveChangesAsync();
        var s = Servis(k);

        await Assert.ThrowsAsync<BadRequestException>(
            () => s.Yaslandirma(k.Islem, null, limit: 5_000));
        await Assert.ThrowsAsync<BadRequestException>(
            () => s.Yaslandirma(k.Islem, null, oluEsikGun: 0));
        await Assert.ThrowsAsync<BadRequestException>(
            () => s.Yaslandirma(k.Islem, null, devirPenceresiGun: 99_999));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Y-31 — RAPOR SAYISI KATALOGDAN
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// M25-14 — rapor sayısı BELGEDEN değil KATALOGDAN okunur ve 16'nın altına
    /// düşemez. `FazAAsgari` katalogla birebir aynıdır.
    /// </summary>
    [Fact]
    public void Rapor_sayisi_katalogdan_okunur_ve_16dir()
    {
        Assert.Equal(RaporKatalogu.FazAAsgari, RaporKatalogu.Hepsi.Length);
        Assert.True(RaporKatalogu.FazAAsgari >= 16,
            $"Faz A rapor sayısı 16'nın altına düşemez; ölçülen {RaporKatalogu.FazAAsgari}");
        Assert.Contains(RaporKatalogu.Hepsi, r => r.Kod == "R15");
    }

    /// <summary>
    /// M25-15 — katalogdaki her uç GERÇEKTEN kayıtlıdır (kart tıklanınca 404
    /// veren rapor olmaz) ve her kodun sözlükte ADI vardır (UX-S31: ham kod
    /// ekrana basılmaz).
    /// </summary>
    [Fact]
    public void Katalogdaki_her_rapor_gercek_uca_ve_ada_sahiptir()
    {
        var uclar = typeof(RaporKatalogu).Assembly.GetTypes()
            .Where(t => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t))
            .SelectMany(t =>
            {
                var rota = t.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>()?.Template ?? "";
                return t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .SelectMany(m => m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpGetAttribute>()
                        .Select(a => $"{rota}/{a.Template}".Replace("api/v1/", "")));
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var r in RaporKatalogu.Hepsi)
        {
            Assert.True(uclar.Contains(r.Uc), $"{r.Kod} için kayıtlı [HttpGet] yok: {r.Uc}");

            var trAd = Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti.Ad(r.Kod);
            var enAd = Kuyumcu.Core.I18n.Sozlukler.En.RaporSeti.Ad(r.Kod);
            Assert.NotEqual(r.Kod, trAd);   // ham kod DÖNMEZ
            Assert.NotEqual(r.Kod, enAd);
        }
    }

    /// <summary>
    /// M25-16 — R15 yaş kovası kodları ekrana HAM basılmaz; sözlükte TR ve EN
    /// karşılığı vardır ve ikisi de kodun kendisi DEĞİLDİR.
    /// </summary>
    [Fact]
    public void Yas_kovasi_kodlari_sozlukte_cevrilir()
    {
        foreach (var kv in StokYaslandirmaServisi.Kovalar)
        {
            Assert.NotEqual(kv.Kod, Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti.Kova(kv.Kod));
            Assert.NotEqual(kv.Kod, Kuyumcu.Core.I18n.Sozlukler.En.RaporSeti.Kova(kv.Kod));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PERSONEL RAPORU — "KAYDEDEN", "SATAN" DEĞİL
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// M25-17 — R13'ün ADI da ÖLÇÜTÜ de KAYDEDEN der. "Satıcı bazlı" iddiası
    /// P-08'in `personel_id = SATAN` kilidi kanıtlanana dek KULLANILMAZ.
    /// </summary>
    [Fact]
    public void Personel_raporu_kaydeden_bazli_adiyla_cikar()
    {
        var tr = Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti;
        var en = Kuyumcu.Core.I18n.Sozlukler.En.RaporSeti;

        Assert.Contains("Kaydeden", tr.Ad("R13"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Satıcı", tr.Ad("R13"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Satan", tr.Ad("R13"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Record", en.Ad("R13"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Seller", en.Ad("R13"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Salesperson", en.Ad("R13"), StringComparison.OrdinalIgnoreCase);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B-8 KÜLTÜR AYAĞI
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// M25-18 — tiplenmiş rapor istemcisi sorgu dizesini MAKİNE biçiminde
    /// kurar. `ToString("yyyy-MM-dd")` tr-TR kültüründe koşarsa sunucuya
    /// giden dizge kültüre bağımlı olur; tek kaynak `Bicim.Makine`.
    /// (denetim.sh KULTURSUZ_TAVAN sayacının kod içi ikizi.)
    /// </summary>
    [Fact]
    public void Rapor_istemcisi_sorgu_dizesini_makine_biciminde_kurar()
    {
        foreach (var dosya in new[] { "Kuyumcu.Web/Servisler/RaporServisi.cs",
                                      "Kuyumcu.Web/Servisler/UyumServisi.cs" })
        {
            var metin = File.ReadAllText(KaynakYolu(dosya));
            Assert.DoesNotContain("ToString(\"yyyy-MM-dd\")", metin);
            Assert.Contains("Bicim.Makine", metin);
        }
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private static string KaynakYolu(string goreli)
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln")))
            d = d.Parent;
        Assert.NotNull(d);
        var yol = Path.Combine(d!.FullName, goreli);
        Assert.True(File.Exists(yol), $"kaynak bulunamadı: {yol}");
        return yol;
    }

    /// <summary>Bir metodun gövdesini kaba ama KARARLI biçimde keser.</summary>
    private static string Govde(string metin, string metotAdi)
    {
        var i = metin.IndexOf($" {metotAdi}(", StringComparison.Ordinal);
        Assert.True(i > 0, $"metot bulunamadı: {metotAdi}");
        var sonraki = metin.IndexOf("\n    public ", i + 1, StringComparison.Ordinal);
        if (sonraki < 0) sonraki = metin.Length;
        return metin[i..sonraki];
    }
}
