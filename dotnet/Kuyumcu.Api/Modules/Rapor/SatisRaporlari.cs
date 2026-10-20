using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kuyumcu.Contracts.Sayfalar.Raporlar;

namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// SATIŞ RAPORLARI — paket 34 Faz A (R10, R12, R13, R14).
//
// ÜÇ DEĞİŞMEZ KURAL (spesifikasyon § iş kuralları):
//  1. İŞARET KORUNUR. İşlem defteri satırı TlTutari'yi HAM işaretiyle döner;
//     Math.Abs defterde yön bilgisini yok eder ve tediye ile tahsilat ayırt
//     edilemez hâle gelir. Toplamlar ayrı alanlarda (tlGiris/tlCikis) verilir.
//  2. ÖDEME ARACI KIRILIMI hareketler.odeme_turu'ndan OKUNMAZ. Karma fişte o
//     kolon yalnız "KARMA" özet etiketidir; gerçek kırılım kasa_hareketleri
//     bacak satırlarındadır. nakit_tutar NULL = BİLİNMİYOR, sıfır DEĞİL.
//  3. PERSONEL RAPORU "KAYDEDEN" der, "SATAN" demez. hareketler.kullanici_id
//     işlemi KAYDEDEN kullanıcıdır; vezne modeli satan ile tahsil edeni
//     ayrıştırana kadar prim kararı bu sayıya dayandırılamaz.
//     ⚠ P-08 / Y-13 GÜNCELLEMESİ: hareketler.personel_id kolonu açıldı ve
//     anlamı "SATAN" olarak KİLİTLENDİ (Sabitler.HareketPersonelRolu). Ancak
//     R13 personel raporunun ÖLÇÜTÜ bu pakette DEĞİŞMEDİ ve "KAYDEDEN"
//     kalmaya devam ediyor: kolon yeni olduğu için geçmiş satırlarda NULL'dur
//     ve ölçütü bugün değiştirmek raporu sessizce boşaltırdı. Prim/satan
//     raporu 1.1'dedir (D-2). İşlem defteri satırı personelId + satanBilinmiyor
//     alanlarını ŞİMDİDEN döner — veri görünür, iddia üretilmez.
//
// STORNO (paket 48 devri): storno ÇİFTİ toplamlardan StornoSuzgeci ile
// dışlanır ve AYRI sayılır. "Ters iade" ile storno taklidi yasaktır (Eray).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SatisRaporServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    /// <summary>TRANSFER ciroya KATILMAZ: aynı mal çıkış ve giriş şubesinde iki satır doğurur.</summary>
    public const string Transfer = "TRANSFER";

    // ─── R10 — İşlem defteri ─────────────────────────────────────────────────
    /// <summary>
    /// Genel hareket dökümü. Satırlar HAM işaretiyle döner; özet toplamları
    /// storno çifti DIŞLANARAK hesaplanır ve storno ayrı blokta sayılır.
    /// </summary>
    public async Task<object> IslemDefteri(
        AktifKullanici islem, DateOnly? baslangic, DateOnly? bitis, RaporFiltresi filtre)
    {
        var d = RaporAraligi.Coz(baslangic, bitis);
        var f = filtre.Dogrula();
        await KimlikleriDogrula(islem, f);

        var temel = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= d.Bas && h.Tarih < d.Bit);

        temel = temel.Daralt(f.SubeId);   // PAKET ŞUBE-KAPSAM-A: tek daraltma yolu
        if (!string.IsNullOrEmpty(f.CariId))      temel = temel.Where(h => h.CariId == f.CariId);
        if (!string.IsNullOrEmpty(f.KullaniciId)) temel = temel.Where(h => h.KullaniciId == f.KullaniciId);
        if (!string.IsNullOrEmpty(f.Tur))         temel = temel.Where(h => h.Tur == f.Tur);
        if (!string.IsNullOrEmpty(f.OdemeTuru))   temel = temel.Where(h => h.OdemeTuru == f.OdemeTuru);

        // TB-19: toplam satır sayısı SUNUCUDA sayılır, dönen listeden türetilmez.
        var toplamSatir = await temel.CountAsync();

        // Özet: storno ÇİFTİ dışlanır → yanlış girilip düzeltilmiş işlem ciroyu şişirmez.
        var netSorgu = temel.StornoCiftiniDisla(db, islem.TenantId);
        var ozet = new
        {
            tlGiris   = await netSorgu.Where(h => h.TlTutari > 0m).SumAsync(h => (decimal?)h.TlTutari) ?? 0m,
            tlCikis   = -(await netSorgu.Where(h => h.TlTutari < 0m).SumAsync(h => (decimal?)h.TlTutari) ?? 0m),
            hasGiris  = await netSorgu.Where(h => h.HasGrami > 0m).SumAsync(h => (decimal?)h.HasGrami) ?? 0m,
            hasCikis  = -(await netSorgu.Where(h => h.HasGrami < 0m).SumAsync(h => (decimal?)h.HasGrami) ?? 0m),
            satirSayisi = await netSorgu.CountAsync(),
        };

        // Storno AYRI görünür (Eray kuralı): kaç çift, ne kadar tutar hükümsüz.
        var stornoSatirlari = temel.Where(h => h.StornoKaynakId != null);
        var storno = new
        {
            adet = await stornoSatirlari.CountAsync(),
            tutarTl = await stornoSatirlari.SumAsync(h => (decimal?)Math.Abs(h.TlTutari)) ?? 0m,
        };

        var sayfa = await temel
            .OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id)
            .Skip(f.Ofset).Take(f.Limit)
            .ToListAsync();

        var stornolananlar = await db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.StornoKaynakId != null)
            .Select(h => h.StornoKaynakId!)
            .ToListAsync();
        var stornoKume = stornolananlar.ToHashSet(StringComparer.Ordinal);

        var cariAdlari = await AdHaritasi(sayfa.Select(h => h.CariId), islem.TenantId, "cari");
        var urunAdlari = await AdHaritasi(sayfa.Select(h => h.UrunId), islem.TenantId, "urun");
        var subeAdlari = await AdHaritasi(sayfa.Select(h => h.SubeId), islem.TenantId, "sube");
        var kullaniciAdlari = await AdHaritasi(sayfa.Select(h => h.KullaniciId), islem.TenantId, "kullanici");

        var satirlar = sayfa.Select(h => new
        {
            id = h.Id,
            tarih = h.Tarih,
            tur = h.Tur,
            // Yön AYRI alandır: tutar ham işaretiyle gider, ekran yönü koddan değil
            // bu alandan okur (UX-S31: ham kod ekrana basılmaz, sözlükten çevrilir).
            // P-48: yön İKİ BACAKTAN türetilir (IslemYonu.Turet) — TL=0 iken has
            // gramı yönü belirler; zıt işaret = KARISIK; ikisi sıfır = null.
            // Tür adı (EVRAK_GIRIS, EMANET_GIRIS…) yön kaynağı DEĞİLDİR.
            yon = IslemYonu.Turet(h.TlTutari, h.HasGrami),
            yonTl = IslemYonu.Isaret(h.TlTutari),
            yonHas = IslemYonu.Isaret(h.HasGrami),
            tlTutari = h.TlTutari,
            hasGrami = h.HasGrami,
            maden = h.Maden,
            paraBirimi = h.ParaBirimi,
            odemeTuru = h.OdemeTuru,
            nakitTutar = h.NakitTutar,
            veresiyeTutar = h.VeresiyeTutar,
            fisId = h.FisId,
            faturaNo = h.FaturaNo,
            // P-08 / Y-14 — adet NULL = BİLİNMİYOR, 0 DEĞİL. İki ayrı alan
            // döner ki ekran "0 adet" ile "bilinmiyor"u karıştırmasın; geçmiş
            // satırlar için adet TÜRETİLMEZ (Anayasa md.1).
            adet = h.Adet,
            adetBilinmiyor = h.Adet is null,
            // P-08 / Y-13 — SATAN. kullaniciAd KAYDEDEN'dir; ikisi ayrı alandır.
            personelId = h.PersonelId,
            satanBilinmiyor = h.PersonelId is null,
            cariAd = h.CariId is null ? null : cariAdlari.GetValueOrDefault(h.CariId),
            urunAd = h.UrunId is null ? null : urunAdlari.GetValueOrDefault(h.UrunId),
            subeAd = subeAdlari.GetValueOrDefault(h.SubeId),
            kullaniciAd = kullaniciAdlari.GetValueOrDefault(h.KullaniciId),
            // İki ayrı bayrak: bu satır bir storno MU, yoksa stornolanmış MI.
            stornoSatiri = h.StornoKaynakId != null,
            stornolandi = stornoKume.Contains(h.Id),
        }).ToList();

        // KVKK md.12 / ADR-0004 — cari adı dönen liste okuması iz bırakır.
        // ⚠ Faz B'ye ERTELENMEZ (Mehmet K34-2): Cari hassas varlıktır.
        await denetim.ListeOkumaKaydet(islem, nameof(Cari), satirlar.Count, "islem-defteri");

        return new
        {
            baslangic = d.YerelBas.ToString("yyyy-MM-dd"),
            bitis = d.YerelBit.ToString("yyyy-MM-dd"),
            toplamSatir,
            ofset = f.Ofset,
            limit = f.Limit,
            dahaFazlasiVar = f.Ofset + satirlar.Count < toplamSatir,
            ozet,
            storno,
            satirlar,
        };
    }

    // ─── R12 — Ödeme aracı kırılımı ──────────────────────────────────────────
    /// <summary>
    /// NAKIT/KART/HAVALE/DOVIZ/ALTIN kırılımı kasa_hareketleri BACAK satırlarından,
    /// veresiye payı hareketler.veresiye_tutar'dan gelir.
    /// <para>
    /// ⚠ hareketler.odeme_turu KULLANILMAZ: karma fişte o kolon "KARMA" özet
    /// etiketidir ve tek etikete indirgemek kırılımı YANILTIR (A6/A7 kanıtı).
    /// </para>
    /// </summary>
    public async Task<object> OdemeAraci(
        AktifKullanici islem, DateOnly? baslangic, DateOnly? bitis, string? subeId)
    {
        var d = RaporAraligi.Coz(baslangic, bitis);
        await SubeDogrula(islem, subeId);

        // Kasa bacakları — şube süzgeci kasanın şubesinden gelir.
        var kasaSorgu = db.KasaHareketleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.Tarih >= d.Bas && k.Tarih < d.Bit);
        if (!string.IsNullOrEmpty(subeId))
        {
            var kasaIdleri = await db.Kasalar.AsNoTracking()
                .Daralt(subeId).Where(k => k.TenantId == islem.TenantId)   // PAKET ŞUBE-KAPSAM-A
                .Select(k => k.Id).ToListAsync();
            kasaSorgu = kasaSorgu.Where(k => kasaIdleri.Contains(k.KasaId));
        }

        // Sunucu tarafı gruplama (TB-19): satırlar çekilmez, toplam çekilir.
        var bacaklar = await kasaSorgu
            .GroupBy(k => new { k.OdemeTuru, k.ParaBirimi })
            .Select(g => new
            {
                g.Key.OdemeTuru,
                g.Key.ParaBirimi,
                girisTutar = g.Where(x => x.Tutar > 0m).Sum(x => x.Tutar),
                cikisTutar = g.Where(x => x.Tutar < 0m).Sum(x => x.Tutar),
                adet = g.Count(),
            })
            .ToListAsync();

        var araclar = bacaklar
            .GroupBy(b => string.IsNullOrEmpty(b.OdemeTuru) ? "BELIRSIZ" : b.OdemeTuru)
            .Select(g => new
            {
                arac = g.Key,
                // UX-S32: para birimi tutarla BİRLİKTE taşınır; TL ile USD toplanmaz.
                paraBirimleri = g.Select(x => new
                {
                    paraBirimi = x.ParaBirimi,
                    girisTutar = Yuvarla2(x.girisTutar),
                    cikisTutar = Yuvarla2(-x.cikisTutar),
                    adet = x.adet,
                }).OrderBy(x => x.paraBirimi).ToList(),
                girisTlToplam = Yuvarla2(g.Where(x => x.ParaBirimi == "TRY").Sum(x => x.girisTutar)),
                adet = g.Sum(x => x.adet),
            })
            .OrderByDescending(x => x.girisTlToplam)
            .ToList();

        // ── Veresiye payı: kasa bacağı YOKTUR, defterden okunur ──────────────
        var hareketler = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= d.Bas && h.Tarih < d.Bit);
        hareketler = hareketler.Daralt(subeId);   // PAKET ŞUBE-KAPSAM-A
        var net = hareketler.StornoCiftiniDisla(db, islem.TenantId);

        // Borç DOĞURAN yön: TlTutari > 0 (alacak). Borç KAPATAN yön ayrı sayılır.
        var veresiyeDogan = await net.Where(h => h.TlTutari > 0m && h.VeresiyeTutar != null)
            .SumAsync(h => (decimal?)h.VeresiyeTutar) ?? 0m;
        var veresiyeKapanan = await net.Where(h => h.TlTutari < 0m && h.VeresiyeTutar != null)
            .SumAsync(h => (decimal?)h.VeresiyeTutar) ?? 0m;

        // ── BİLİNMİYOR sütunu: nakit payı NULL olan ESKİ satırlar ────────────
        // ⚠ 0 SAYILMAZ (Anayasa md.1). Bu satırların nakit payı geri kazanılamaz;
        //   raporda ayrı sütunda ve sayısıyla beyan edilir.
        var bilinmeyenNakit = await net.Where(h => h.NakitTutar == null).CountAsync();
        var bilinenNakit = await net.Where(h => h.NakitTutar != null).CountAsync();

        var stornoSatirlari = hareketler.Where(h => h.StornoKaynakId != null);
        // G-5: ekranın ayrıştırdığı zarf (Contracts tipi) — baslangic/bitis/satisKirilimi buradan.
        var zarf = SatisOdemeKirilimi.Zarf(d, await SatisOdemeKirilimiHesapla(islem, d, subeId));

        return new
        {
            baslangic = zarf.Baslangic,
            bitis = zarf.Bitis,
            kaynak = "KASA_HAREKETLERI",
            araclar,
            veresiye = new
            {
                doganTl = Yuvarla2(veresiyeDogan),
                kapananTl = Yuvarla2(veresiyeKapanan),
            },
            bilinmeyen = new
            {
                nakitPayiBilinmeyenSatir = bilinmeyenNakit,
                nakitPayiBilinenSatir = bilinenNakit,
            },
            storno = new
            {
                adet = await stornoSatirlari.CountAsync(),
                tutarTl = Yuvarla2(await stornoSatirlari.SumAsync(h => (decimal?)Math.Abs(h.TlTutari)) ?? 0m),
            },
            guvenilirlik = RaporGuvenilirligi.Hesapla(bilinenNakit, bilinmeyenNakit),
            // G-5 (B-4 devri): SATIŞ fişlerinin ödeme kalemlerinden kırılım —
            // "Eski altın (mahsup)" satırı BURADA doğar (kasa satırı yoktur).
            satisKirilimi = zarf.SatisKirilimi,
        };
    }

    // ─── G-5 — SATIŞ ödeme kırılımı (odeme_kalemleri) ────────────────────────
    /// <summary>
    /// Dönemin SATIŞ fişleri neyle ödendi — kaynak <c>odeme_kalemleri</c>.
    /// <para>
    /// ⚠ Yukarıdaki <c>araclar</c> kasa defterinin giriş/çıkışıdır; eski altın
    /// (ALTIN bacağı) kasaya YAZILMAZ (B-4: kasa_id NULL, hareket_id → HURDA_ALIS)
    /// ve orada hiç görünmez. Satış kırılımı bu yüzden fişin kalemlerinden
    /// okunur: ALTIN kalemi ayrı satır olur, <c>kasayaGirenTl</c>'ye KATILMAZ.
    /// </para>
    /// <para>
    /// Satış BRÜT yazılıdır (KARAR-HAS-DEFTERI 2026-09-23): kırılım toplamı
    /// brüt satış toplamına eşit olmalıdır; değilse fark UYDURULMAZ, sayı olarak
    /// döner. Kalemi hiç olmayan (eski) fiş "BILINMIYOR" satırında beyan edilir.
    /// </para>
    /// </summary>
    private async Task<SatisKirilimi> SatisOdemeKirilimiHesapla(
        AktifKullanici islem, RaporDonemi d, string? subeId)
    {
        var satislar = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= d.Bas && h.Tarih < d.Bit
                        && h.Tur == "SATIS");
        satislar = satislar.Daralt(subeId);   // PAKET ŞUBE-KAPSAM-A
        satislar = satislar.StornoCiftiniDisla(db, islem.TenantId);

        // Fiş anahtarı OdemeKalemFabrikasi.Turet ile AYNI: fis_id boşsa hareketin kendisi.
        var fisBrutleri = await satislar
            .Select(h => new { fis = (h.FisId == null || h.FisId == "") ? h.Id : h.FisId, h.TlTutari })
            .GroupBy(x => x.fis)
            .Select(g => new { fis = g.Key, brut = g.Sum(x => x.TlTutari) })
            .ToListAsync();

        var fisAnahtarlari = satislar
            .Select(h => (h.FisId == null || h.FisId == "") ? h.Id : h.FisId!);

        // Sunucu tarafı gruplama: kalem satırları çekilmez, fiş × araç toplamı çekilir.
        var kalemler = await db.OdemeKalemleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.Aktif && fisAnahtarlari.Contains(k.FisId))
            .GroupBy(k => new { k.FisId, k.Arac })
            .Select(g => new { g.Key.FisId, g.Key.Arac, tutar = g.Sum(x => x.TutarTl) })
            .ToListAsync();

        return SatisOdemeKirilimi.Olustur(
            fisBrutleri.Select(f => (f.fis, f.brut)).ToList(),
            kalemler.Select(k => (k.FisId, k.Arac, k.tutar)).ToList());
    }

    // ─── R13 — Kaydeden kullanıcı raporu ─────────────────────────────────────
    /// <summary>
    /// ⚠ KAYDEDEN kullanıcı — SATAN DEĞİL. hareketler.kullanici_id işlemi
    /// sisteme GİREN kişidir; vezne modeli (paket 19) satan ile tahsil edeni
    /// ayrıştırana kadar bu sayı prim/performans kararına dayanak OLAMAZ.
    /// Bu ayrım rapor başlığında, sütun adında ve sözlükte yazılıdır (M114).
    /// </summary>
    public async Task<object> Personel(
        AktifKullanici islem, DateOnly? baslangic, DateOnly? bitis, string? subeId)
    {
        var d = RaporAraligi.Coz(baslangic, bitis);
        await SubeDogrula(islem, subeId);

        var temel = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= d.Bas && h.Tarih < d.Bit);
        temel = temel.Daralt(subeId);   // PAKET ŞUBE-KAPSAM-A
        var net = temel.StornoCiftiniDisla(db, islem.TenantId);

        var gruplar = await net
            .Where(h => h.Tur == "SATIS" || h.Tur == "SATIS_IADE")
            .GroupBy(h => h.KullaniciId)
            .Select(g => new
            {
                KullaniciId = g.Key,
                satisAdet = g.Count(x => x.Tur == "SATIS"),
                satisTl = g.Where(x => x.Tur == "SATIS").Sum(x => (decimal?)x.TlTutari) ?? 0m,
                iadeAdet = g.Count(x => x.Tur == "SATIS_IADE"),
                iadeTl = g.Where(x => x.Tur == "SATIS_IADE").Sum(x => (decimal?)x.TlTutari) ?? 0m,
            })
            .ToListAsync();

        var adlar = await AdHaritasi(gruplar.Select(g => (string?)g.KullaniciId), islem.TenantId, "kullanici");

        var satirlar = gruplar.Select(g => new
        {
            kullaniciId = g.KullaniciId,
            // Alan adı KASITLI: "kaydedenAd". "satanAd" yazılırsa M114 kırmızı olur.
            kaydedenAd = adlar.GetValueOrDefault(g.KullaniciId) ?? g.KullaniciId,
            islemSayisi = g.satisAdet,
            ciroTl = Yuvarla2(g.satisTl),
            ortalamaSepetTl = g.satisAdet == 0 ? 0m : Yuvarla2(g.satisTl / g.satisAdet),
            iadeAdet = g.iadeAdet,
            iadeTl = Yuvarla2(Math.Abs(g.iadeTl)),
            iadeOrani = g.satisAdet == 0 ? (decimal?)null
                : Math.Round((decimal)g.iadeAdet / g.satisAdet, 4, MidpointRounding.AwayFromZero),
        }).OrderByDescending(x => x.ciroTl).ToList();

        return new
        {
            baslangic = d.YerelBas.ToString("yyyy-MM-dd"),
            bitis = d.YerelBit.ToString("yyyy-MM-dd"),
            olcut = "KAYDEDEN_KULLANICI",
            uyari = "SATAN_DEGIL_KAYDEDEN",
            toplamSatir = satirlar.Count,
            satirlar,
        };
    }

    // ─── R14 — Şube karşılaştırma ────────────────────────────────────────────
    /// <summary>
    /// Şube bazında satış/alış/hurda/mal farkı.
    /// <para>
    /// ⚠ TRANSFER ciroya KATILMAZ: transfer hem çıkış hem giriş şubesinde satır
    /// doğurur; ciroya katılsa aynı mal iki şubede sayılır ve toplam şişer.
    /// </para>
    /// </summary>
    public async Task<object> SubeKarsilastirma(AktifKullanici islem, DateOnly? baslangic, DateOnly? bitis)
    {
        var d = RaporAraligi.Coz(baslangic, bitis);

        var net = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= d.Bas && h.Tarih < d.Bit)
            .StornoCiftiniDisla(db, islem.TenantId);

        var gruplar = await net
            .GroupBy(h => h.SubeId)
            .Select(g => new
            {
                SubeId = g.Key,
                satisTl = g.Where(x => x.Tur == "SATIS").Sum(x => (decimal?)x.TlTutari) ?? 0m,
                satisAdet = g.Count(x => x.Tur == "SATIS"),
                alisTl = g.Where(x => x.Tur == "ALIS").Sum(x => (decimal?)x.TlTutari) ?? 0m,
                hurdaTl = g.Where(x => x.Tur == "HURDA_ALIS").Sum(x => (decimal?)x.TlTutari) ?? 0m,
                hurdaHas = g.Where(x => x.Tur == "HURDA_ALIS").Sum(x => (decimal?)x.HasGrami) ?? 0m,
                transferAdet = g.Count(x => x.Tur == Transfer),
                transferTl = g.Where(x => x.Tur == Transfer).Sum(x => (decimal?)x.TlTutari) ?? 0m,
            })
            .ToListAsync();

        var adlar = await AdHaritasi(gruplar.Select(g => (string?)g.SubeId), islem.TenantId, "sube");

        var satirlar = gruplar.Select(g => new
        {
            subeId = g.SubeId,
            subeAd = adlar.GetValueOrDefault(g.SubeId) ?? g.SubeId,
            satisAdet = g.satisAdet,
            // CİRO = yalnız SATIS. Transfer AYRI sütunda görünür, ciroya eklenmez.
            ciroTl = Yuvarla2(g.satisTl),
            alisTl = Yuvarla2(Math.Abs(g.alisTl)),
            hurdaTl = Yuvarla2(Math.Abs(g.hurdaTl)),
            hurdaHasGram = Math.Round(g.hurdaHas, 3, MidpointRounding.AwayFromZero),
            malFarkiTl = Yuvarla2(g.satisTl - Math.Abs(g.alisTl)),
            transferAdet = g.transferAdet,
            transferTl = Yuvarla2(Math.Abs(g.transferTl)),
        }).OrderByDescending(x => x.ciroTl).ToList();

        return new
        {
            baslangic = d.YerelBas.ToString("yyyy-MM-dd"),
            bitis = d.YerelBit.ToString("yyyy-MM-dd"),
            transferCiroyaKatilir = false,
            toplamSatir = satirlar.Count,
            toplamCiroTl = Yuvarla2(satirlar.Sum(x => x.ciroTl)),
            satirlar,
        };
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────────

    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Süzgeç kimlikleri KİRACI SINIRINDA doğrulanır (Mehmet K34-4): yabancı
    /// kiracının kimliğiyle süzme BOŞ SONUÇ değil 404 döner — aksi hâlde "boş
    /// mu, yoksa benim değil mi" ayrımından varlık varlığı sızdırılır.
    /// </summary>
    private async Task KimlikleriDogrula(AktifKullanici islem, RaporFiltresi f)
    {
        await SubeDogrula(islem, f.SubeId);
        if (!string.IsNullOrEmpty(f.CariId)
            && !await db.Cariler.AsNoTracking().AnyAsync(c => c.Id == f.CariId && c.TenantId == islem.TenantId))
            throw new NotFoundException("Cari bulunamadı");
        if (!string.IsNullOrEmpty(f.KullaniciId)
            && !await db.Kullanicilar.AsNoTracking().AnyAsync(k => k.Id == f.KullaniciId && k.TenantId == islem.TenantId))
            throw new NotFoundException("Kullanıcı bulunamadı");
    }

    private async Task SubeDogrula(AktifKullanici islem, string? subeId)
    {
        if (!string.IsNullOrEmpty(subeId)
            && !await db.Subeler.AsNoTracking().AnyAsync(s => s.Id == subeId && s.TenantId == islem.TenantId))
            throw new NotFoundException("Şube bulunamadı");
    }

    /// <summary>Kimlik → görünen ad haritası; yalnız sayfadaki kimlikler için.</summary>
    private async Task<Dictionary<string, string>> AdHaritasi(
        IEnumerable<string?> idler, string tenantId, string tur)
    {
        var kume = idler.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
        if (kume.Count == 0) return [];
        return tur switch
        {
            "cari" => await db.Cariler.AsNoTracking()
                .Where(c => c.TenantId == tenantId && kume.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Ad),
            "urun" => await db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == tenantId && kume.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Ad),
            "sube" => await db.Subeler.AsNoTracking()
                .Where(s => s.TenantId == tenantId && kume.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Ad),
            _ => await db.Kullanicilar.AsNoTracking()
                .Where(k => k.TenantId == tenantId && kume.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id, k => (k.Ad + " " + k.Soyad).Trim()),
        };
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ⚠ SINIF NİTELİĞİ MİRAS ALINMAZ (Bertan/Mehmet K34-3): RaporController'ın
//   kapıları bu sınıfa geçmez. Üç nitelik de AÇIKÇA yazılır.
// ═════════════════════════════════════════════════════════════════════════════

[ApiController]
[Authorize]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
[Route("api/v1/raporlar")]
[Yetki.ModulYetkisi("rapor")]
public sealed class SatisRaporController(SatisRaporServisi servis, KuyumcuDbContext db) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/raporlar/katalog — Sevk edilmiş raporların TEK sayım kaynağı</summary>
    [HttpGet("katalog")]
    public object Katalog() => new
    {
        toplam = RaporKatalogu.Hepsi.Length,
        faz = "A",
        raporlar = RaporKatalogu.Hepsi.Select(r => new
        {
            kod = r.Kod,
            adAnahtari = r.Kod,
            ad = Kuyumcu.Core.I18n.Sozlukler.Tr.RaporSeti.Ad(r.Kod),
            rota = r.Rota,
            uc = "/api/v1/" + r.Uc,
            roller = r.Roller,
            faz = r.Faz,
            grup = r.Grup,
        }).ToList(),
    };

    /// <summary>GET /api/v1/raporlar/islem-defteri — R10 genel hareket dökümü (sayfalı)</summary>
    [HttpGet("islem-defteri")]
    public async Task<object> IslemDefteri(
        [FromQuery] DateOnly? baslangic, [FromQuery] DateOnly? bitis,
        [FromQuery] string? tur, [FromQuery] string? subeId, [FromQuery] string? cariId,
        [FromQuery] string? kullaniciId, [FromQuery] string? odemeTuru,
        [FromQuery, Range(1, RaporFiltresi.EnFazlaLimit)] int limit = 100,
        [FromQuery, Range(0, int.MaxValue)] int ofset = 0)
        => await servis.IslemDefteri(Islem, baslangic, bitis,
            new RaporFiltresi(await GorunumSubesi.Coz(db, Islem, subeId), cariId, kullaniciId, null, null, tur, odemeTuru, limit, ofset));

    /// <summary>GET /api/v1/raporlar/odeme-araci — R12 ödeme aracı kırılımı</summary>
    [HttpGet("odeme-araci")]
    public async Task<object> OdemeAraci(
        [FromQuery] DateOnly? baslangic, [FromQuery] DateOnly? bitis, [FromQuery] string? subeId)
        => await servis.OdemeAraci(Islem, baslangic, bitis, await GorunumSubesi.Coz(db, Islem, subeId));

    /// <summary>GET /api/v1/raporlar/personel — R13 KAYDEDEN kullanıcı bazlı özet</summary>
    [HttpGet("personel")]
    public async Task<object> Personel(
        [FromQuery] DateOnly? baslangic, [FromQuery] DateOnly? bitis, [FromQuery] string? subeId)
        => await servis.Personel(Islem, baslangic, bitis, await GorunumSubesi.Coz(db, Islem, subeId));

    /// <summary>GET /api/v1/raporlar/prim — D1-20 SATAN bazlı prim ve hedef (sahip/müdür/muhasebe)</summary>
    [HttpGet("prim")]
    public async Task<PrimRaporYaniti> Prim(
        [FromQuery] DateOnly? baslangic, [FromQuery] DateOnly? bitis, [FromQuery] string? subeId,
        [FromServices] DenetimServisi denetim, [FromQuery] decimal? oran = null)   // D1-20 dene-gör: bu oranla ne öderdim
        => await PrimRaporu.Hesapla(db, denetim, Islem, baslangic, bitis, await GorunumSubesi.Coz(db, Islem, subeId), oran);

    /// <summary>GET /api/v1/raporlar/sube-karsilastirma — R14 şube bazlı karşılaştırma</summary>
    [HttpGet("sube-karsilastirma")]
    public Task<object> SubeKarsilastirma(
        [FromQuery] DateOnly? baslangic, [FromQuery] DateOnly? bitis)
        => servis.SubeKarsilastirma(Islem, baslangic, bitis);
}

// ═════════════════════════════════════════════════════════════════════════════
// P-48 — İŞLEM DEFTERİ YÖN TÜRETİMİ (saf fonksiyon, tek kaynak).
//
// ÖLÇÜLEN HATA (QA tur 8): yön yalnız TlTutari >= 0'dan türetiliyordu; bu yüzden
//   · "EMANET_GIRIS storno | 0,00 TL | -5,496 g" satırı GİRİŞ görünüyordu
//     (TL sıfır → ">= 0" → GIRIS; has gramı hiç bakılmıyordu),
//   · iki bacağı da sıfır olan satır da GİRİŞ yazıyordu (tahmin).
//
// KURAL:
//   1. TL ve has İKİSİ DE sıfırdan farklı ve işaretleri ZIT → KARISIK
//      (satış: para girer, has çıkar; alış: tersi). Tek yöne indirgenmez;
//      ekran iki bacağı (yonTl / yonHas) ayrı basar — Anayasa md.1.
//   2. TL ≠ 0 → TL işaretinden.
//   3. TL = 0 ve has ≠ 0 → HAS işaretinden.
//   4. İkisi de 0 → null (yön YOK; ekran "—" basar, "Giriş" YAZMAZ).
//
// ⚠ TÜR ADI YÖN KAYNAĞI DEĞİLDİR: "EVRAK_GIRIS" adlı satır TL'si -3.250,40 ise
//   ÇIKIŞ'tır. Tür adı işlemin cinsini söyler, paranın/malın yönünü söylemez;
//   yön yalnız defterdeki işaretlerden okunur.
// ═════════════════════════════════════════════════════════════════════════════
public static class IslemYonu
{
    public const string Giris = "GIRIS";
    public const string Cikis = "CIKIS";
    public const string Karisik = "KARISIK";

    /// <summary>Tek bacağın yönü: pozitif GIRIS, negatif CIKIS, sıfır null.</summary>
    public static string? Isaret(decimal deger)
        => deger > 0m ? Giris : deger < 0m ? Cikis : null;

    /// <summary>Satırın yönü: GIRIS / CIKIS / KARISIK / null (bkz. sınıf başı kuralı).</summary>
    public static string? Turet(decimal tlTutari, decimal hasGrami)
    {
        var tl = Isaret(tlTutari);
        var has = Isaret(hasGrami);
        if (tl is not null && has is not null && tl != has) return Karisik;
        return tl ?? has;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// G-5 (B-4 devri) — SATIŞ ÖDEME KIRILIMI (saf fonksiyon, tek kaynak).
//
// KURALLAR:
//   1. Satırlar fişin ödeme kalemlerinden gelir; ALTIN kalemi "Eski altın
//      (mahsup)" satırıdır. Kasaya girmez → kasayaGirenTl'ye KATILMAZ
//      (OdemeAraci.KasayaGirer tek kaynak).
//   2. Tutarı 0 olan araç için satır ÇİZİLMEZ (altınsız dönemde boş satır yok).
//   3. Kalemi olmayan fiş BILINMIYOR satırında brüt tutarıyla beyan edilir;
//      sıfır sayılmaz, başka araca yazılmaz (Anayasa md.1).
//   4. farkTl = brüt satış − kırılım toplamı. Uydurulmaz; sıfır değilse döner.
// ═════════════════════════════════════════════════════════════════════════════
public static class SatisOdemeKirilimi
{
    public const string Kaynak = "ODEME_KALEMLERI";

    /// <summary>
    /// Kırılımı Kuyumcu.Contracts tipiyle döndürür: ekranın ayrıştırdığı tip
    /// sunucunun imzasındadır (SozlesmeKapsamTestleri — alan kayarsa DERLEME kırılır).
    /// </summary>
    public static SatisKirilimi Olustur(
        IReadOnlyList<(string Fis, decimal Brut)> fisler,
        IReadOnlyList<(string Fis, string Arac, decimal Tutar)> kalemler)
    {
        var kalemliFisler = kalemler.Select(k => k.Fis).ToHashSet();
        var kalemsiz = fisler.Where(f => !kalemliFisler.Contains(f.Fis)).ToList();

        var satirlar = kalemler
            .GroupBy(k => k.Arac)
            .Select(g => new SatisKirilimiSatiri(g.Key, Yuvarla2(g.Sum(x => x.Tutar)),
                g.Select(x => x.Fis).Distinct().Count(), OdemeAraci.KasayaGirer(g.Key)))
            .Where(s => s.TutarTl > 0m)
            .ToList();

        var kalemsizTl = Yuvarla2(kalemsiz.Sum(f => f.Brut));
        if (kalemsizTl != 0m)
            satirlar.Add(new SatisKirilimiSatiri(OdemeAraci.Bilinmiyor, kalemsizTl, kalemsiz.Count, false));

        satirlar = satirlar.OrderByDescending(s => s.TutarTl).ToList();

        var brut = Yuvarla2(fisler.Sum(f => f.Brut));
        var toplam = Yuvarla2(satirlar.Sum(s => s.TutarTl));
        return new SatisKirilimi(
            Kaynak: Kaynak,
            Satirlar: satirlar,
            BrutSatisTl: brut,
            KirilimToplamTl: toplam,
            FarkTl: Yuvarla2(brut - toplam),
            // "Kasaya giren" yalnız kasaya giren araçlardır: eski altın ve veresiye HARİÇ.
            KasayaGirenTl: Yuvarla2(satirlar.Where(s => s.KasayaGirer).Sum(s => s.TutarTl)),
            MahsupAltinTl: Yuvarla2(satirlar.Where(s => s.Arac == OdemeAraci.Altin).Sum(s => s.TutarTl)),
            VeresiyeTl: Yuvarla2(satirlar.Where(s => s.Arac == OdemeAraci.Veresiye).Sum(s => s.TutarTl)),
            FisAdet: fisler.Count,
            KalemsizFisAdet: kalemsiz.Count,
            KalemsizTl: kalemsizTl);
    }

    /// <summary>
    /// Ekranın ayrıştırdığı zarf (GET /raporlar/odeme-araci'nin baslangic/bitis/satisKirilimi
    /// alt kümesi). OdemeAraci yanıtının bu üç alanı BU nesneden okunur.
    /// </summary>
    public static SatisOdemeKirilimiYaniti Zarf(RaporDonemi d, SatisKirilimi kirilim) =>
        new(d.YerelBas.ToString("yyyy-MM-dd"), d.YerelBit.ToString("yyyy-MM-dd"), kirilim);

    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
}
