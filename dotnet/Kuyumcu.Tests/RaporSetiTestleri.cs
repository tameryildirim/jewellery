using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Rapor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 34 Faz A — RAPOR SETİ [veritabanı testleri]
//
// Sınanan değişmezler:
//   • işaret korunur (defter satırında yön kaybolmaz)
//   • limit kapısı SERVİSTE durur ([Range] servis yolunda çalışmaz)
//   • ödeme aracı kırılımı kasa bacaklarından okunur, "KARMA" etiketinden DEĞİL
//   • nakit payı NULL = BİLİNMİYOR, sıfır DEĞİL
//   • TRANSFER şube cirosuna katılmaz
//   • personel raporu KAYDEDEN der
//   • kapanmamış gün AÇIK'tır, farkı NULL'dur
//   • TB-19: toplamSatir sayfadan türetilmez
//   • KVKK: cari adı dönen rapor okuma izi yazar
//   • storno çifti ciroyu şişirmez ama AYRI görünür (paket 48 devri)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class RaporSetiTestleri(TestVeritabani vt)
{
    private static object? Alan(object kaynak, string ad) =>
        kaynak.GetType().GetProperty(ad)?.GetValue(kaynak);

    private static IEnumerable<object> Liste(object kaynak, string ad)
        => ((System.Collections.IEnumerable)Alan(kaynak, ad)!).Cast<object>();

    private SatisRaporServisi Satis(Kiraci k)
    {
        var db = vt.Olustur(k);
        return new SatisRaporServisi(db, new DenetimServisi(db, new HttpContextAccessor()));
    }

    private FinansRaporServisi Finans(Kiraci k) => new(vt.Olustur(k));

    private static DateOnly Bugun => Zaman.YerelBugun();

    private static Hareket Satir(
        Kiraci k, string tur, decimal tl, decimal has = 0m, string? odemeTuru = null,
        decimal? nakit = null, decimal? veresiye = null, string? kullaniciId = null,
        string? subeId = null, string? cariId = null, string? fisId = null,
        string? stornoKaynakId = null, DateTime? tarih = null)
        => new()
        {
            Id = Kimlik.Ulid(),
            TenantId = k.TenantId,
            SubeId = subeId ?? k.SubeId,
            Tur = tur,
            Tarih = tarih ?? DateTime.UtcNow,
            TlTutari = tl,
            HasGrami = has,
            ParaBirimi = "TRY",
            OdemeTuru = odemeTuru,
            NakitTutar = nakit,
            VeresiyeTutar = veresiye,
            FisId = fisId,
            StornoKaynakId = stornoKaynakId,
            CariId = cariId,
            KurSnapshotId = k.KurId,
            KullaniciId = kullaniciId ?? k.KullaniciId,
            Olusturuldu = DateTime.UtcNow,
        };

    // ── İşlem defteri (R10) ──────────────────────────────────────────────────

    [Fact]
    public async Task Islem_defteri_TL_isaretini_korur()
    {
        // M122 — TlTutari'ye Math.Abs uygulanırsa bu test KIRMIZI olur.
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST isaret");
        sistem.Hareketler.Add(Satir(k, "SATIS", 5_000m, -1m, "NAKIT", nakit: 5_000m, veresiye: 0m, cariId: k.CariId));
        sistem.Hareketler.Add(Satir(k, "ALIS", -3_000m, 2m, "NAKIT", nakit: 3_000m, veresiye: 0m));
        await sistem.SaveChangesAsync();

        var r = await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 50));
        var satirlar = Liste(r, "satirlar").ToList();

        var satis = satirlar.Single(s => (string)Alan(s, "tur")! == "SATIS");
        var alis = satirlar.Single(s => (string)Alan(s, "tur")! == "ALIS");
        Assert.Equal(5_000m, Alan(satis, "tlTutari"));
        Assert.Equal(-3_000m, Alan(alis, "tlTutari"));   // NEGATİF kalır
        // P-48: satış (+TL, -has) ve alış (-TL, +has) iki bacaklıdır → KARISIK;
        // bacak yönleri ayrı alanlarda gelir (eski tek-bacak "GIRIS/CIKIS" beklentisi
        // has gramını yok sayıyordu).
        Assert.Equal("KARISIK", Alan(satis, "yon"));
        Assert.Equal("GIRIS", Alan(satis, "yonTl"));
        Assert.Equal("KARISIK", Alan(alis, "yon"));
        Assert.Equal("CIKIS", Alan(alis, "yonTl"));

        var ozet = Alan(r, "ozet")!;
        Assert.Equal(5_000m, Alan(ozet, "tlGiris"));
        Assert.Equal(3_000m, Alan(ozet, "tlCikis"));
    }

    // ── P-48 — İşlem defteri YÖN türetimi (has gramı ve sıfır TL dâhil) ──────
    // Ölçülen hata: yon = TlTutari >= 0 ? GIRIS : CIKIS — has gramı yöne girmiyordu.
    // Mutasyon M48-1 (eski formüle dönüş) → P48_01, P48_02, P48_03 KIRMIZI.

    private static object DefterSatiri(object r, string tur)
        => Liste(r, "satirlar").Single(s => (string)Alan(s, "tur")! == tur);

    [Fact]
    public async Task P48_01_TL_sifir_has_negatif_satir_CIKIS_doner()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET48-TEST tl-sifir");
        // QA tur 8 journal'ı: "EMANET_GIRISStorno | Giris | 0,00 TL | -5.496 g"
        sistem.Hareketler.Add(Satir(k, "EMANET_GIRIS", 0m, -5.496m));
        // "EVRAK_GIRIS | Cikis | -3.250,40 TL | 0.000 g" — tür adı yönü BELİRLEMEZ.
        sistem.Hareketler.Add(Satir(k, "EVRAK_GIRIS", -3_250.40m, 0m));
        sistem.Hareketler.Add(Satir(k, "SAYIM_DUZELTME", 0m, 1.25m));
        await sistem.SaveChangesAsync();

        var r = await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 50));

        var emanet = DefterSatiri(r, "EMANET_GIRIS");
        Assert.Equal("CIKIS", Alan(emanet, "yon"));
        Assert.Null(Alan(emanet, "yonTl"));
        Assert.Equal("CIKIS", Alan(emanet, "yonHas"));
        Assert.Equal(0m, Alan(emanet, "tlTutari"));
        Assert.Equal(-5.496m, Alan(emanet, "hasGrami"));   // işaret korunur

        Assert.Equal("CIKIS", Alan(DefterSatiri(r, "EVRAK_GIRIS"), "yon"));
        Assert.Equal("GIRIS", Alan(DefterSatiri(r, "SAYIM_DUZELTME"), "yon"));

        // Saf fonksiyon — aynı kural tek kaynaktan.
        Assert.Equal(IslemYonu.Cikis, IslemYonu.Turet(0m, -5.496m));
        Assert.Equal(IslemYonu.Giris, IslemYonu.Turet(0m, 0.001m));
        Assert.Equal(IslemYonu.Cikis, IslemYonu.Turet(-3_250.40m, 0m));
        Assert.Equal(IslemYonu.Giris, IslemYonu.Turet(10m, 0m));
    }

    [Fact]
    public async Task P48_02_Zit_isaretli_satir_KARISIK_doner()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET48-TEST karisik");
        sistem.Hareketler.Add(Satir(k, "SATIS", 5_000m, -1.5m));      // para girer, has çıkar
        sistem.Hareketler.Add(Satir(k, "ALIS", -3_000m, 2m));         // para çıkar, has girer
        sistem.Hareketler.Add(Satir(k, "TAKAS", 100m, 0.5m));         // aynı işaret → KARISIK DEĞİL
        await sistem.SaveChangesAsync();

        var r = await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 50));

        var satis = DefterSatiri(r, "SATIS");
        Assert.Equal("KARISIK", Alan(satis, "yon"));
        Assert.Equal("GIRIS", Alan(satis, "yonTl"));
        Assert.Equal("CIKIS", Alan(satis, "yonHas"));

        var alis = DefterSatiri(r, "ALIS");
        Assert.Equal("KARISIK", Alan(alis, "yon"));
        Assert.Equal("CIKIS", Alan(alis, "yonTl"));
        Assert.Equal("GIRIS", Alan(alis, "yonHas"));

        Assert.Equal("GIRIS", Alan(DefterSatiri(r, "TAKAS"), "yon"));

        Assert.Equal(IslemYonu.Karisik, IslemYonu.Turet(5_000m, -1.5m));
        Assert.Equal(IslemYonu.Karisik, IslemYonu.Turet(-0.01m, 0.001m));
        Assert.Equal(IslemYonu.Cikis, IslemYonu.Turet(-5m, -1m));
    }

    [Fact]
    public async Task P48_03_Iki_bacak_sifirsa_yon_null_doner()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET48-TEST sifir");
        sistem.Hareketler.Add(Satir(k, "ISCILIK", 0m, 0m));
        await sistem.SaveChangesAsync();

        var r = await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 50));

        var s = DefterSatiri(r, "ISCILIK");
        Assert.Null(Alan(s, "yon"));          // "GIRIS" TAHMİN EDİLMEZ
        Assert.Null(Alan(s, "yonTl"));
        Assert.Null(Alan(s, "yonHas"));
        Assert.Null(IslemYonu.Turet(0m, 0m));
    }

    [Fact]
    public async Task Islem_defteri_limitsiz_cagrilamaz()
    {
        // M110 — limit kapısı RaporFiltresi.Dogrula()'da; servis yolunda da durur.
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PAKET34-TEST limit");
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 0)));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 501)));
    }

    [Fact]
    public async Task Islem_defteri_tur_suzgeci_SATIS_IADE_bulur()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST tur");
        sistem.Hareketler.Add(Satir(k, "SATIS", 9_000m, -2m, "NAKIT", nakit: 9_000m, veresiye: 0m));
        sistem.Hareketler.Add(Satir(k, "SATIS_IADE", -1_500m, 0.5m, "NAKIT", nakit: 1_500m, veresiye: 0m));
        await sistem.SaveChangesAsync();

        var r = await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Tur: "SATIS_IADE", Limit: 50));
        var satirlar = Liste(r, "satirlar").ToList();
        Assert.Single(satirlar);
        Assert.Equal("SATIS_IADE", Alan(satirlar[0], "tur"));
        Assert.Equal(1, Alan(r, "toplamSatir"));
    }

    [Fact]
    public async Task Islem_defteri_sayfalar_ve_toplamSatir_gercektir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST sayfa");
        for (var i = 0; i < 5; i++)
            sistem.Hareketler.Add(Satir(k, "SATIS", 1_000m + i, -1m, "NAKIT", nakit: 1_000m, veresiye: 0m));
        await sistem.SaveChangesAsync();

        var r = await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 2));
        Assert.Equal(5, Alan(r, "toplamSatir"));     // sayfadan TÜRETİLMEZ
        Assert.Equal(2, Liste(r, "satirlar").Count());
        Assert.Equal(true, Alan(r, "dahaFazlasiVar"));

        var son = await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 2, Ofset: 4));
        Assert.Single(Liste(son, "satirlar"));
        Assert.Equal(false, Alan(son, "dahaFazlasiVar"));
    }

    [Fact]
    public async Task Yabanci_kiracinin_kimligiyle_suzme_404_doner()
    {
        // Mehmet K34-4: boş sonuç DEĞİL 404 — varlığın varlığı sızdırılmaz.
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST sinir A");
        var yabanci = TestKurulum.KiraciKur(sistem, "PAKET34-TEST sinir B");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(CariId: yabanci.CariId, Limit: 10)));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(SubeId: yabanci.SubeId, Limit: 10)));
    }

    [Fact]
    public async Task Yabanci_kiracinin_hareketi_hicbir_raporda_gorunmez()
    {
        // ⚠ REGRESYON testi — mutasyon kanıtı DEĞİL (global HasQueryFilter zaten
        //   koruyor; "TenantId şartı kaldırılsın" mutasyonu çok katmanlı korumaya
        //   takılır ve testi kırmaz — Aşkın'ın tuzak uyarısı).
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST izole A");
        var yabanci = TestKurulum.KiraciKur(sistem, "PAKET34-TEST izole B");
        sistem.Hareketler.Add(Satir(yabanci, "SATIS", 77_777m, -9m, "NAKIT", nakit: 77_777m, veresiye: 0m));
        await sistem.SaveChangesAsync();

        var r = await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 50));
        Assert.Equal(0, Alan(r, "toplamSatir"));
        Assert.Empty(Liste(r, "satirlar"));
    }

    // ── Ödeme aracı (R12) ────────────────────────────────────────────────────

    [Fact]
    public async Task Karma_fis_odeme_aracina_bacaklara_bolunur()
    {
        // M111 — kırılım hareketler.odeme_turu'ndan okunursa tek "KARMA" satırı
        // çıkar ve bu test KIRMIZI olur.
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST karma");
        var now = DateTime.UtcNow;
        var fisId = Kimlik.Ulid();

        var h = Satir(k, "SATIS", 20_000m, -3m, "KARMA", nakit: 12_000m, veresiye: 3_000m,
                      cariId: k.CariId, fisId: fisId);
        sistem.Hareketler.Add(h);
        sistem.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId, HareketId = h.Id,
            Tutar = 12_000m, ParaBirimi = "TRY", OdemeTuru = "NAKIT", Tarih = now, Olusturuldu = now,
        });
        sistem.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId, HareketId = h.Id,
            Tutar = 5_000m, ParaBirimi = "TRY", OdemeTuru = "KART", Tarih = now, Olusturuldu = now,
        });
        await sistem.SaveChangesAsync();

        var r = await Satis(k).OdemeAraci(k.Islem, Bugun, Bugun, null);
        var araclar = Liste(r, "araclar").ToList();
        var adlar = araclar.Select(a => (string)Alan(a, "arac")!).ToList();

        Assert.Contains("NAKIT", adlar);
        Assert.Contains("KART", adlar);
        Assert.DoesNotContain("KARMA", adlar);   // özet etiket KIRILIM DEĞİLDİR

        Assert.Equal(12_000m, Alan(araclar.Single(a => (string)Alan(a, "arac")! == "NAKIT"), "girisTlToplam"));
        Assert.Equal(5_000m, Alan(araclar.Single(a => (string)Alan(a, "arac")! == "KART"), "girisTlToplam"));

        var veresiye = Alan(r, "veresiye")!;
        Assert.Equal(3_000m, Alan(veresiye, "doganTl"));
    }

    [Fact]
    public async Task Bilinmeyen_nakit_payi_sifira_yazilmaz()
    {
        // M117 — NULL nakit payı 0 sayılırsa bilinmeyen sayacı 0 olur ve KIRMIZI.
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST bilinmeyen");
        sistem.Hareketler.Add(Satir(k, "SATIS", 4_000m, -1m, "NAKIT", nakit: null, veresiye: null));
        sistem.Hareketler.Add(Satir(k, "SATIS", 6_000m, -1m, "NAKIT", nakit: 6_000m, veresiye: 0m));
        await sistem.SaveChangesAsync();

        var r = await Satis(k).OdemeAraci(k.Islem, Bugun, Bugun, null);
        var bilinmeyen = Alan(r, "bilinmeyen")!;
        Assert.Equal(1, Alan(bilinmeyen, "nakitPayiBilinmeyenSatir"));
        Assert.Equal(1, Alan(bilinmeyen, "nakitPayiBilinenSatir"));
        Assert.NotEqual(RaporGuvenilirligi.Tam, Alan(r, "guvenilirlik"));
    }

    // ── Personel (R13) ───────────────────────────────────────────────────────

    [Fact]
    public async Task Kaydeden_kullanici_raporu_iki_kullaniciyi_ayri_sayar()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST personel");
        var now = DateTime.UtcNow;
        var ikinciId = Kimlik.Ulid();
        sistem.Kullanicilar.Add(new Kullanici
        {
            Id = ikinciId, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Zeynep", Soyad = "Yılmaz",
            Email = $"{ikinciId}@test.local", SifreHash = "x", Rol = "TEZGAHTAR", Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        sistem.Hareketler.Add(Satir(k, "SATIS", 10_000m, -1m, "NAKIT", nakit: 10_000m, veresiye: 0m));
        sistem.Hareketler.Add(Satir(k, "SATIS", 4_000m, -1m, "NAKIT", nakit: 4_000m, veresiye: 0m, kullaniciId: ikinciId));
        sistem.Hareketler.Add(Satir(k, "SATIS_IADE", -1_000m, 0.2m, "NAKIT", nakit: 1_000m, veresiye: 0m, kullaniciId: ikinciId));
        await sistem.SaveChangesAsync();

        var r = await Satis(k).Personel(k.Islem, Bugun, Bugun, null);
        var satirlar = Liste(r, "satirlar").ToList();
        Assert.Equal(2, satirlar.Count);

        var zeynep = satirlar.Single(s => ((string)Alan(s, "kaydedenAd")!).StartsWith("Zeynep", StringComparison.Ordinal));
        Assert.Equal(1, Alan(zeynep, "islemSayisi"));
        Assert.Equal(4_000m, Alan(zeynep, "ciroTl"));
        Assert.Equal(1, Alan(zeynep, "iadeAdet"));
    }

    [Fact]
    public void Personel_tablosu_KAYDEDEN_der()
    {
        // M114 — sütun "Satan" olursa KIRMIZI. Metin sözlükte TEK kaynaktan gelir.
        var tr = Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti;
        var en = Kuyumcu.Core.I18n.Sozlukler.En.RaporSeti;
        Assert.Contains("Kaydeden", tr.KaydedenKullanici, StringComparison.Ordinal);
        Assert.DoesNotContain("Satan", tr.KaydedenKullanici, StringComparison.Ordinal);
        Assert.DoesNotContain("Tezgâhtar", tr.KaydedenKullanici, StringComparison.Ordinal);
        Assert.Contains("Recording", en.KaydedenKullanici, StringComparison.Ordinal);
        Assert.DoesNotContain("Seller", en.KaydedenKullanici, StringComparison.Ordinal);
        Assert.Equal(tr.KaydedenKullanici, tr.AdPersonel);
    }

    // ── Şube karşılaştırma (R14) ─────────────────────────────────────────────

    [Fact]
    public async Task Transfer_sube_cirosuna_KATILMAZ()
    {
        // M112 — transfer ciroya katılırsa KIRMIZI.
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST transfer");
        var now = DateTime.UtcNow;
        var ikinciSube = Kimlik.Ulid();
        sistem.Subeler.Add(new Sube
        {
            Id = ikinciSube, TenantId = k.TenantId, Ad = "Şube 2", Aktif = true,
            Olusturuldu = now, Guncellendi = now,
        });
        sistem.Hareketler.Add(Satir(k, "SATIS", 30_000m, -4m, "NAKIT", nakit: 30_000m, veresiye: 0m));
        sistem.Hareketler.Add(Satir(k, "TRANSFER", 50_000m, -6m, subeId: k.SubeId));
        sistem.Hareketler.Add(Satir(k, "TRANSFER", -50_000m, 6m, subeId: ikinciSube));
        await sistem.SaveChangesAsync();

        var r = await Satis(k).SubeKarsilastirma(k.Islem, Bugun, Bugun);
        var satirlar = Liste(r, "satirlar").ToList();

        Assert.Equal(30_000m, Alan(r, "toplamCiroTl"));          // transfer YOK
        var merkez = satirlar.Single(s => (string)Alan(s, "subeId")! == k.SubeId);
        Assert.Equal(30_000m, Alan(merkez, "ciroTl"));
        Assert.Equal(1, Alan(merkez, "transferAdet"));           // ama GÖRÜNÜR
        Assert.Equal(false, Alan(r, "transferCiroyaKatilir"));
    }

    // ── Storno devri (paket 48 → 34) ─────────────────────────────────────────

    [Fact]
    public async Task Storno_cifti_ciroyu_sismez_ama_AYRI_gorunur()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST storno");
        var temiz = Satir(k, "SATIS", 10_000m, -1m, "NAKIT", nakit: 10_000m, veresiye: 0m);
        var hatali = Satir(k, "SATIS", 99_000m, -9m, "NAKIT", nakit: 99_000m, veresiye: 0m);
        sistem.Hareketler.Add(temiz);
        sistem.Hareketler.Add(hatali);
        await sistem.SaveChangesAsync();

        var ters = Satir(k, "SATIS", -99_000m, 9m, "NAKIT", nakit: 99_000m, veresiye: 0m,
                         stornoKaynakId: hatali.Id);
        sistem.Hareketler.Add(ters);
        await sistem.SaveChangesAsync();

        var r = await Satis(k).SubeKarsilastirma(k.Islem, Bugun, Bugun);
        // Ciro yalnız TEMİZ satıştır; stornolanan ve ters satır DIŞLANIR.
        Assert.Equal(10_000m, Alan(r, "toplamCiroTl"));
        // ⚠ TUTAR TEK BAŞINA YETMEZ: storno çifti TL'de net SIFIRDIR, süzgeç
        //   kaldırılsa bile toplam 10.000 kalırdı (ölçüldü — M-34-S ilk denemede
        //   hiçbir testi kırmadı = TEST AÇIĞI). ADET süzgeci gerçekten sınar:
        //   süzgeçsiz hâlde 3 satış sayılır.
        var merkezSatir = Liste(r, "satirlar").Single(x => (string)Alan(x, "subeId")! == k.SubeId);
        Assert.Equal(1, Alan(merkezSatir, "satisAdet"));

        var defter = await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 50));
        var ozet = Alan(defter, "ozet")!;
        Assert.Equal(10_000m, Alan(ozet, "tlGiris"));
        // ...ama defterde ÜÇ satır da durur (append-only) ve storno AYRI sayılır.
        Assert.Equal(3, Alan(defter, "toplamSatir"));
        var storno = Alan(defter, "storno")!;
        Assert.Equal(1, Alan(storno, "adet"));
        Assert.Equal(99_000m, Alan(storno, "tutarTl"));
    }

    // ── Kasa defteri (R11) ───────────────────────────────────────────────────

    [Fact]
    public async Task Kasa_defteri_gun_kapanisi_farkini_gosterir_kapanmamis_gunu_ACIK_isaretler()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST kasa");
        var now = DateTime.UtcNow;
        sistem.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId,
            Tutar = 1_500m, ParaBirimi = "TRY", OdemeTuru = "NAKIT", Tarih = now, Olusturuldu = now,
        });
        // BUGÜNÜN yerel 00:30'u — UTC'de dünün 21:30'u (İstanbul UTC+3).
        var geceYarisiUtc = Zaman.YerelGunBasiUtc(Bugun).AddMinutes(30);
        sistem.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId,
            Tutar = 1_000m, ParaBirimi = "TRY", OdemeTuru = "NAKIT",
            Tarih = geceYarisiUtc, Olusturuldu = geceYarisiUtc,
        });
        // ⚠ Üretimde kasa bacağı ile bakiye AYNI transaction'da yazılır; fikstür de
        //   öyle kurulmalı — yoksa devir çözümü (bakiye − sonraki hareketler) doğru
        //   çalışır ama fikstür kendi içinde tutarsız olur.
        var kasaKaydi = await sistem.Kasalar.FirstAsync(x => x.Id == k.KasaId);
        kasaKaydi.Bakiye += 2_500m;
        sistem.GunKapanislari.Add(new GunKapanisi
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId, SubeId = k.SubeId,
            YerelGun = Bugun, SayilanNakit = 252_450m, DefterBakiye = 252_500m, Fark = -50m,
            Aciklama = "Eksik", KapatanKullaniciId = k.KullaniciId, KapanisZamani = now, Olusturuldu = now,
        });
        await sistem.SaveChangesAsync();

        var r = await Finans(k).KasaDefteri(k.Islem, Bugun.AddDays(-1), Bugun, k.KasaId);
        var kasa = Liste(r, "kasalar").Single();
        var gunler = Liste(kasa, "gunler").ToList();
        Assert.Equal(2, gunler.Count);

        var dun = gunler[0];
        Assert.Equal("ACIK", Alan(dun, "durum"));
        Assert.Null(Alan(dun, "fark"));           // kapanmamış günün farkı BİLİNMİYOR
        Assert.Equal(0m, Alan(dun, "giris"));

        var bugun = gunler[1];
        Assert.Equal("KAPALI", Alan(bugun, "durum"));
        // ⚠ K5 KAPANI: gece 00:30'da (yerel) yazılan kasa bacağı UTC'de BİR ÖNCEKİ
        //   güne düşer. Gün kovası UTC'ye göre kurulursa bu 1.000 TL dünün
        //   satırına gider ve bugünün girişi 1.500 kalır. Aşağıdaki 2.500 ancak
        //   kova YEREL güne göre kurulduğunda çıkar (ölçüldü: M-34-K5 ilk
        //   denemede hiçbir testi kırmıyordu = TEST AÇIĞI).
        Assert.Equal(-50m, Alan(bugun, "fark"));
        Assert.Equal(2_500m, Alan(bugun, "giris"));   // 1.500 + gece yarısı 1.000
        Assert.Equal(2, Alan(bugun, "hareketSayisi"));
        Assert.Equal(0m, Alan(dun, "giris"));          // gece yarısı satırı DÜNE düşmez
        // Kapanış, kasanın GERÇEK bakiyesiyle tutar (devir geriye doğru çözülür).
        Assert.Equal(TestKurulum.KasaAcilis + 2_500m, Alan(bugun, "kapanis"));
        Assert.Equal("TRY", Alan(kasa, "paraBirimi"));
    }

    // ── TB-19 / MÜHÜR 5 ──────────────────────────────────────────────────────

    [Fact]
    public async Task Yaslandirma_toplamSatir_gercek_sayidir()
    {
        // M121 — toplamSatir limitlenmiş satır sayısından üretilirse KIRMIZI.
        // ⚠ 500 carilik fikstür YOK: limit parametrik (Aşkın koşulu, test süresi).
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST yaslandirma");
        var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            var cariId = Kimlik.Ulid();
            sistem.Cariler.Add(new Cari
            {
                Id = cariId, TenantId = k.TenantId, Tur = "MUSTERI", Ad = $"Borçlu {i}",
                Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
            sistem.CariBakiyeler.Add(new CariBakiye
            {
                CariId = cariId, TenantId = k.TenantId, TlBakiye = 1_000m + i,
                HasGramBakiye = 0m, SonIslemTarihi = now, Guncellendi = now,
            });
        }
        await sistem.SaveChangesAsync();

        var db = vt.Olustur(k);
        var servis = TestKurulum.RaporServisi(db);
        var r = await servis.CariYaslandirma(k.Islem, limit: 2);

        Assert.Equal(5, Alan(r, "toplamSatir"));
        Assert.Equal(2, Liste(r, "yaslananCariler").Count());
        Assert.Equal(true, Alan(r, "dahaFazlasiVar"));

        // ozetTl SAYFANIN DEĞİL, TÜM cari kümesinin toplamıdır.
        var ozet = (Dictionary<string, decimal>)Alan(r, "ozetTl")!;
        Assert.Equal(5_010m, ozet["0-30"]);   // 1000+1001+1002+1003+1004
    }

    [Fact]
    public async Task Stok_degeri_sayfalanir_ve_toplamlar_tum_urunlerden_gelir()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST stok");
        var now = DateTime.UtcNow;
        for (var i = 0; i < 4; i++)
        {
            var urunId = Kimlik.Ulid();
            sistem.Urunler.Add(new Urun
            {
                Id = urunId, TenantId = k.TenantId, Tip = "ZIYNET", Kod = $"P34-{i}", Ad = $"Ek ürün {i}",
                AyarId = "K22", Milyem = 0.916m, GramAgirlik = 5m, TasAgirlik = 0m, HasGrami = 4.58m,
                IscilikTl = 0m, Aktif = true, Olusturuldu = now, Guncellendi = now,
            });
            sistem.Stoklar.Add(new Stok
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, UrunId = urunId,
                Adet = 2m, ToplamHasGram = 9.16m, Guncellendi = now,
            });
        }
        await sistem.SaveChangesAsync();

        var servis = TestKurulum.RaporServisi(vt.Olustur(k));
        var tam = await servis.StokDegeri(k.Islem, null, limit: 500);
        var sayfali = await servis.StokDegeri(k.Islem, null, limit: 2);

        Assert.Equal(5, Alan(tam, "toplamSatir"));
        Assert.Equal(5, Alan(sayfali, "toplamSatir"));      // sayfadan türetilmez
        Assert.Equal(2, Liste(sayfali, "urunler").Count());
        // toplamHas TÜM ürünlerden gelir; sayfa değişince DEĞİŞMEZ.
        Assert.Equal(Alan(tam, "toplamHas"), Alan(sayfali, "toplamHas"));
    }

    // ── KVKK okuma izi ───────────────────────────────────────────────────────

    [Fact]
    public async Task Kisisel_veri_donen_rapor_okuma_izi_yazar()
    {
        // M115 — CariYaslandirma / islem-defteri okuma izi kaldırılırsa KIRMIZI.
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST kvkk");
        sistem.Hareketler.Add(Satir(k, "SATIS", 2_000m, -1m, "NAKIT", nakit: 2_000m, veresiye: 0m, cariId: k.CariId));
        await sistem.SaveChangesAsync();

        await Satis(k).IslemDefteri(k.Islem, Bugun, Bugun, new RaporFiltresi(Limit: 10));
        await TestKurulum.RaporServisi(vt.Olustur(k)).CariYaslandirma(k.Islem);

        var izler = await vt.OlusturSistem().DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.Eylem == DenetimEylemi.Okuma
                     && d.VarlikTuru == nameof(Cari))
            .ToListAsync();

        Assert.Contains(izler, i => i.Detay!.Contains("islem-defteri", StringComparison.Ordinal));
        Assert.Contains(izler, i => i.Detay!.Contains("cari-yaslandirma", StringComparison.Ordinal));
    }

    // ── QA turu 3 — aylık özet çelişkisi ─────────────────────────────────────

    [Fact]
    public async Task Aylik_veresiye_ve_tahsilat_GUN_SONU_ile_ayni_kaynaktan_gelir()
    {
        // QA turu 3 bulgusu: aylık özet veresiye ve tahsilatı 0 gösteriyordu;
        // aynı ekranda VERESIYE satış ve gün sonu tahsilatı DOLU idi.
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKET34-TEST aylik");
        var now = DateTime.UtcNow;

        // Veresiye SATIŞ: ayrı "VERESIYE" hareketi DOĞMAZ, pay SATIS satırındadır.
        sistem.Hareketler.Add(Satir(k, "SATIS", 4_000m, -1m, "VERESIYE", nakit: 0m, veresiye: 4_000m,
                                    cariId: k.CariId));
        sistem.KasaHareketleri.Add(new KasaHareket
        {
            Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = k.KasaId,
            Tutar = 150m, ParaBirimi = "TRY", OdemeTuru = "NAKIT", Tarih = now, Olusturuldu = now,
        });
        await sistem.SaveChangesAsync();

        var yerel = Zaman.UtcdenYerele(now);
        var r = await TestKurulum.RaporServisi(vt.Olustur(k)).AylikOzet(yerel.Year, yerel.Month, k.Islem);

        Assert.Equal(4_000m, Alan(Alan(r, "veresiye")!, "toplamTl"));
        Assert.Equal(150m, Alan(Alan(r, "tahsilat")!, "toplamTl"));
        Assert.Equal("KASA_HAREKETLERI", Alan(Alan(r, "tahsilat")!, "kaynak"));
    }
}
