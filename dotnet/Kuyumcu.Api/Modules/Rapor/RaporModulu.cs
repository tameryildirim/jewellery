using System.Linq.Expressions;
using System.Globalization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// Rapor modülü — kaynak: apps/api/src/modules/rapor/*
// Salt-okur raporlama uçları; DTO gerektirmez (tüm girdiler query paramı).
// Dönüş şekilleri NestJS RaporService ile birebir aynıdır.
// ═════════════════════════════════════════════════════════════════════════════

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class RaporServisi(
    KuyumcuDbContext db, DenetimServisi denetim, Maliyet.KarZararServisi? karZarar = null,
    Hurda.HurdaAyarlari? hurdaAyarlari = null, YasalParametreServisi? yasal = null)
{
    // YASAL-4 (hasım bulgusu 01.10): gider pusulası belge durumu süre takibiyle AYNI türetmeden.
    private readonly Hurda.HurdaAyarlari hurdaAyar = hurdaAyarlari ?? new Hurda.HurdaAyarlari();

    // ─── Günlük özet ──────────────────────────────────────────────────────────
    /// <summary>
    /// Belirli bir gün için:
    /// - Satış adedi + toplam TL
    /// - Alış adedi + toplam TL
    /// - Net has gram hareketi (satış+alış)
    /// - Kasa bakiyeleri
    /// </summary>
    public async Task<object> GunlukOzet(string tarih, AktifKullanici islem, string? subeId = null)
    {
        RaporGunuDogrula(tarih);   // PAKET GOC-D: 3000 yılı / bozuk gün sessiz boş özet DEĞİL, 400
        // K5: gün sınırı İŞLETME saatine (Europe/Istanbul) göre hesaplanır.
        // Kayıtlar UTC tutulduğu için yerel gün 00:00–24:00 aralığı UTC'ye çevrilir;
        // aksi halde İstanbul'da gece 00:00–03:00 arasındaki ciro rapora düşmüyordu.
        // Geçersiz tarihte hiçbir hareket eşleşmez (Nest'teki Invalid Date davranışı).
        var gecerli = DateOnly.TryParse(tarih, CultureInfo.InvariantCulture, DateTimeStyles.None, out var yerelGun);
        var (gun, gunSonu) = gecerli
            ? Zaman.YerelGunAraligiUtc(yerelGun)
            : (DateTime.MinValue, DateTime.MinValue);

        // ─ P-25 / B-8 — SUNUCUDA TOPLANIR ───────────────────────────────────
        // Eski hâl günün BÜTÜN hareketlerini `ToListAsync()` ile belleğe alıp
        // LINQ-to-Objects ile topluyordu. Gün sınırı satır sayısını SINIRLAMAZ:
        // yoğun bir günde on binlerce satır uygulamaya taşınıyordu (TB-19).
        // Artık toplam SQL'de `GROUP BY tur, maden` ile alınır; dönen satır
        // sayısı |tür| × |maden| kadardır, hareket sayısıyla değil.
        // ⚠ `OrderByDescending(h => h.Tarih)` KALDIRILDI: toplama için anlamsızdı
        // (sıralı okumanın tek etkisi bütün satırları sıralamaktı).
        // PAKET ŞUBE-KAPSAM-A: görünüm şubesi yalnız DARALTIR (null = tüm şubeler).
        var kirilim = await HareketToplami.Coz(db.Hareketler.AsNoTracking().Daralt(subeId)
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= gun && h.Tarih < gunSonu));

        var satislar = HareketToplami.Tur(kirilim, "SATIS");
        var alislar  = HareketToplami.Tur(kirilim, "ALIS");
        var hurdalar = HareketToplami.Tur(kirilim, "HURDA_ALIS");

        // ═══ PAKET RAPOR-2 tur 2 (tasarım hakemi KRİTİK) — NET SATIŞ ════════════
        // "Bugün ne sattım" (Panel + Rapor merkezi) iadeyi düşmüyordu; aynı gün
        // Kâr-zarar "Bugün" HASILAT'ı ve "Defterdeki altın" NET'ti (259.152,14 ↔
        // 252.277,99 ₺; 37,2340 ↔ 36,2340 has). TEK TANIM (Bertan + Mustafa oyu):
        // KarZararServisi.Olc ile BİREBİR aynı küme — SATIS + SATIS_IADE, storno
        // çifti DIŞLANIR (48-A süzgeci; ikinci süzgeç yazılmaz); TL = Σ satış − Σ|iade|,
        // has = YALNIZ ALTIN Σ|satış has| − Σ|iade has|. Eski `satis` bloğu ŞEKİL
        // sözleşmesi gereği aynen kalır (P69 bekçisi); ekran yeni `netSatis`i okur.
        var netKirilim = await HareketToplami.Coz(db.Hareketler.AsNoTracking().Daralt(subeId)
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= gun && h.Tarih < gunSonu
                     && (h.Tur == "SATIS" || h.Tur == "SATIS_IADE"))
            .StornoCiftiniDisla(db, islem.TenantId));
        var netSatislar = HareketToplami.Tur(netKirilim, "SATIS");
        var netIadeler  = HareketToplami.Tur(netKirilim, "SATIS_IADE");
        var brutSatisTl = netSatislar.Sum(x => x.TlToplam);
        var iadeTl      = netIadeler.Sum(x => x.TlMutlak);
        var brutAltinHas = HareketToplami.AltinHas(netSatislar, mutlak: true);
        var iadeAltinHas = HareketToplami.AltinHas(netIadeler, mutlak: true);

        // Kasa bakiyeleri — kiracı başına kasa sayısı ONLARCADIR, sınırsız
        // döküm değildir (P-25 beyaz listesi: sayaç bunu saymaz).
        var kasaBakiyeleri = await db.Kasalar.AsNoTracking().Daralt(subeId)
            .Where(k => k.TenantId == islem.TenantId)
            .OrderBy(k => k.Ad)
            .Take(RaporFiltresi.EnFazlaLimit)
            .ToListAsync();

        return new
        {
            tarih,
            // PAKET ŞUBE-KAPSAM-A — yanıt hangi kapsamı topladığını SÖYLER.
            gorunumSubesi = subeId,
            kapsam = GorunumSubesi.Kapsam(subeId),
            satis = new
            {
                adet      = HareketToplami.Adet(satislar),
                toplamTl  = satislar.Sum(x => x.TlToplam),
                // K20-2: yalnız ALTIN. Diğer madenler madenDagilimi altında.
                // P-69: `toplamHas` ADI yanıltıcıdır (tüm madenlerin toplamı DEĞİL,
                // yalnız ALTIN) — şekil sözleşmesi gereği KALDIRILMAZ; aynı değer
                // doğru adıyla `altinToplamHas` olarak da döner. Yeni istemci onu okur.
                toplamHas = HareketToplami.AltinHas(satislar, mutlak: true),
                altinToplamHas = HareketToplami.AltinHas(satislar, mutlak: true),
                madenDagilimi = HareketToplami.MadenDagilimi(satislar, mutlak: true),
            },
            // PAKET RAPOR-2 tur 2: Kâr-zarar hasılatıyla AYNI tanım (yukarıdaki not).
            netSatis = new
            {
                satisAdet      = HareketToplami.Adet(netSatislar),
                iadeAdet       = HareketToplami.Adet(netIadeler),
                brutTl         = brutSatisTl,
                iadeTl,
                toplamTl       = brutSatisTl - iadeTl,
                brutAltinHas,
                iadeAltinHas,
                altinToplamHas = Math.Round(brutAltinHas - iadeAltinHas, 6),
            },
            alis = new
            {
                adet      = HareketToplami.Adet(alislar),
                toplamTl  = alislar.Sum(x => x.TlMutlak),
                toplamHas = HareketToplami.AltinHas(alislar),        // P-69: YALNIZ ALTIN (eski ad)
                altinToplamHas = HareketToplami.AltinHas(alislar),
                madenDagilimi = HareketToplami.MadenDagilimi(alislar),
            },
            hurda = new
            {
                toplamHas = HareketToplami.AltinHas(hurdalar),       // P-69: YALNIZ ALTIN (eski ad)
                altinToplamHas = HareketToplami.AltinHas(hurdalar),
                madenDagilimi = HareketToplami.MadenDagilimi(hurdalar),
                odenenTl  = hurdalar.Sum(x => x.TlMutlak),
            },
            netHasGramHareketi = HareketToplami.AltinHas(kirilim),
            // Dönemin TAMAMININ maden kırılımı — ekran uyarıyı buradan basar.
            madenDagilimi = HareketToplami.MadenDagilimi(kirilim),
            madenUyarisi  = MadenUyarisi(HareketToplami.MadenDagilimi(kirilim)),
            hareketSayisi      = HareketToplami.Adet(kirilim),
            kasalar = kasaBakiyeleri.Select(k => new
            {
                id         = k.Id,
                ad         = k.Ad,
                paraBirimi = k.ParaBirimi,
                bakiye     = k.Bakiye,
            }).ToList(),
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET 20 — MADEN KIRILIMI (K20-3).
    //
    // ⚠ NEDEN VAR: gün sonu ve aylık raporlar has gramları maden AYIRMADAN
    // topluyordu: 100 g altın + 500 g gümüş "600 g has" oluyordu. Bu sayı
    // hiçbir şeyin karşılığı değildir. Artık `toplamHas` YALNIZ ALTINDIR ve
    // diğer madenler ayrı satırlarda döner — stok raporundaki desenin aynısı.
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Verilen satırların madenlere göre has gram dağılımı.</summary>
    private static List<object> MadenDagilimi(
        IEnumerable<Data.Hareket> hareketler, Func<Data.Hareket, decimal> has) =>
        [.. hareketler
            .GroupBy(h => string.IsNullOrWhiteSpace(h.Maden)
                ? Kuyumcu.Core.Domain.Maden.Altin : h.Maden)
            .Select(g => new
            {
                maden    = g.Key,
                madenAdi = Kuyumcu.Core.Domain.Maden.Ad(g.Key),
                toplamHas = Math.Round(g.Sum(has), 6),
                satirSayisi = g.Count(),
            })
            .Where(x => x.toplamHas != 0m)
            .OrderByDescending(x => x.toplamHas)
            .Cast<object>()];

    /// <summary>Yalnız ALTIN satırlarının has toplamı (K20-2).</summary>
    private static decimal AltinHas(
        IEnumerable<Data.Hareket> hareketler, Func<Data.Hareket, decimal> has) =>
        Math.Round(hareketler
            .Where(h => string.IsNullOrWhiteSpace(h.Maden)
                     || h.Maden == Kuyumcu.Core.Domain.Maden.Altin)
            .Sum(has), 6);

    /// <summary>Birden çok maden varsa ekranda görünecek uyarı; yoksa null.</summary>
    // ⚠ P-45: metin artık HareketToplami.MadenUyarisi'nde TEK kaynaktır
    //   (kâr-zarar raporu da aynı uyarıyı basıyor; ikinci kopya yazılmadı).
    private static string? MadenUyarisi(List<object> dagilim) =>
        HareketToplami.MadenUyarisi(dagilim);

    /// <summary>
    /// PAKET GOC-D — tek günlük raporun günü: makine biçimi (yyyy-MM-dd; eski uçla aynı TryParse) çözülmeli ve Rapor
    /// penceresinde olmalı. Eskiden çözülemeyen gün "hiçbir hareket eşleşmez" diye SESSİZ boş
    /// rapor döndürüyordu (UX-S28: hata "kayıt yok"a çevrilmez). Metin GirdiSozlugu'ndan.
    /// </summary>
    internal static void RaporGunuDogrula(string? tarih)
    {
        if (!DateOnly.TryParse((tarih ?? "").Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var gun))
            throw new BadRequestException(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(
                new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(Kuyumcu.Core.Domain.Girdi.GirdiKodu.TarihBicim), Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle("tarih");
        if (Kuyumcu.Core.Domain.Girdi.RaporDonemKurali.GunDenetle(gun, Zaman.YerelBugun(), Kuyumcu.Core.I18n.IstekDili.Gecerli) is { } m)
            throw new BadRequestException(m).AlanIle("tarih");
    }

    // ─── Aylık özet ───────────────────────────────────────────────────────────

    public async Task<object> AylikOzet(int yil, int ay, AktifKullanici islem, string? subeId = null)
    {
        // PAKET GOC-D: yıl Rapor penceresinde (2000..gelecek yıl). Ay taşması (ay=13 → ertesi Ocak) KORUNUR.
        if (Kuyumcu.Core.Domain.Girdi.RaporDonemKurali.GunDenetle(new DateOnly(Math.Clamp(yil, 1, 9998), 1, 1), Zaman.YerelBugun(), Kuyumcu.Core.I18n.IstekDili.Gecerli) is { } yilHata)
            throw new BadRequestException(yilHata).AlanIle("yil");
        // K5: ay sınırı da işletme saatine göre (ay taşması korunur: ay=13 → ertesi yıl Ocak)
        var (baslangic, bitis) = Zaman.YerelAyAraligiUtc(yil, ay);

        // ─ P-25 / B-8 — SUNUCUDA TOPLANIR (GunlukOzet ile AYNI yol) ─────────
        // Aylık rapor ayın BÜTÜN hareketlerini belleğe alıyordu; bir ay
        // milyonlarca satır olabilir. Toplam artık SQL'de alınır.
        // PAKET Q10-1: storno ÇİFTİ (hükümsüz kaynak + ters satırı) dönem
        // toplamlarına GİRMEZ. İşaretli toplamlar zaten değişmez (çift net 0);
        // MUTLAK toplamlar (cari tahsilatı Σ|tl|) çifti 2 × tutar sayıyordu.
        var donemSorgu = db.Hareketler.AsNoTracking().Daralt(subeId)
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= baslangic && h.Tarih < bitis)
            .StornoCiftiniDisla(db, islem.TenantId);

        var kirilim = await HareketToplami.Coz(donemSorgu);

        var satislar    = HareketToplami.Tur(kirilim, "SATIS");
        var alislar     = HareketToplami.Tur(kirilim, "ALIS");
        var tahsilatlar = HareketToplami.Tur(kirilim, "TAHSILAT");

        // ═════════════════════════════════════════════════════════════════════
        // QA TURU 3 (2026-09-17) — "aylık özet veresiye ve tahsilatı SIFIR
        // gösteriyor, aynı sayfadaki VERESIYE satış ve gün sonu tahsilatıyla
        // ÇELİŞİYOR". Ölçülen kök neden İKİ TANE:
        //
        //  (1) VERESİYE: rapor yalnız `Tur == "VERESIYE"` satırlarını sayıyordu.
        //      Oysa veresiye SATIŞ ayrı bir hareket doğurmaz — borç, SATIS
        //      satırının `veresiye_tutar` payındadır (HareketFabrikasi.OdemeIzi).
        //      Yani 4.000 TL'lik veresiye satış defterde duruyor ama raporda
        //      yoktu. Tanım artık ÖDEME ARACI RAPORUYLA (R12) AYNI: borç doğuran
        //      yönde (tlTutari > 0) yazılan veresiye payı toplamı.
        //
        //  (2) TAHSİLAT: rapor `Tur == "TAHSILAT"` (yalnız CARİ tahsilatı)
        //      sayıyordu; gün sonu mutabakatı ise KASAYA GİREN parayı sayıyor.
        //      Aynı ekranda iki farklı cevap olamaz (UX-S22). Aylık kart artık
        //      gün sonuyla AYNI kaynaktan beslenir: kasa_hareketleri girişleri.
        //      Cari tahsilatı bilgisi SAKLANMAZ, ayrı alanda durur.
        // ═════════════════════════════════════════════════════════════════════
        // P-25: veresiye payları da SUNUCUDA toplanır — iki skaler sorgu.
        var veresiyeDoganTl = await donemSorgu
            .Where(h => h.TlTutari > 0m && h.VeresiyeTutar != null)
            .SumAsync(h => (decimal?)h.VeresiyeTutar!.Value) ?? 0m;
        var veresiyeKapananTl = await donemSorgu
            .Where(h => h.TlTutari < 0m && h.VeresiyeTutar != null)
            .SumAsync(h => (decimal?)h.VeresiyeTutar!.Value) ?? 0m;

        // PAKET Q10-1: storno çifti kasaya giren TL'ye girmez (gider stornosu
        // tahsilat DEĞİLDİR; stornolanan tahsilat da hükümsüzdür).
        // PAKET ŞUBE-KAPSAM-A: kasa_hareketleri şube taşımaz; şube KASADAN türetilir.
        var aylikKasaIdleri = subeId is null ? null
            : await db.Kasalar.AsNoTracking().Daralt(subeId).Select(k => k.Id)
                .Take(RaporFiltresi.EnFazlaLimit).ToListAsync();   // kasa sayısı onlarcadır (TB-19 sınırı)
        var aylikKasaGirisSorgu = db.KasaHareketleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.Tarih >= baslangic && k.Tarih < bitis
                     && k.Tutar > 0m && k.ParaBirimi == "TRY"
                     && (aylikKasaIdleri == null || aylikKasaIdleri.Contains(k.KasaId)))
            .StornoCiftiniDisla(db, islem.TenantId);
        // PAKET RAPOR-3 (hasım VERİ, 150.000 ₺ farkı): aylık "Tahsilat" GÜN SONUYLA AYNI TANIM (PARA-DEMO-2 madde 5) —
        // virman girişi (KarsiBacakId dolu) ve açılış devri (Tur="ACILIS" hareketine bağlı kasa bacağı) tahsilata
        // GİRMEZ, ayrı alanda döner. Eskiden ikisi de toplama giriyordu: Özet 632.975,08 ↔ Gün sonu 482.975,08.
        var aylikVirmanGiris = await aylikKasaGirisSorgu.Where(k => k.KarsiBacakId != null)
            .SumAsync(k => (decimal?)k.Tutar) ?? 0m;
        var aylikAcilisDevri = await aylikKasaGirisSorgu
            .Where(k => k.KarsiBacakId == null && k.HareketId != null)
            .Join(db.Hareketler.AsNoTracking().Where(h => h.TenantId == islem.TenantId && h.Tur == "ACILIS"),
                  k => k.HareketId, h => h.Id, (k, h) => k.Tutar)
            .SumAsync(t => (decimal?)t) ?? 0m;
        var kasaGirisleri = (await aylikKasaGirisSorgu.Where(k => k.KarsiBacakId == null)
            .SumAsync(k => (decimal?)k.Tutar) ?? 0m) - aylikAcilisDevri;

        var satisTl = satislar.Sum(x => x.TlToplam);
        var alisTl  = alislar.Sum(x => x.TlMutlak);
        // PAKET RAPOR-2 tur 2: satış iadesi (storno çifti zaten dışlı — donemSorgu). Kâr-zarar
        // "Bu ay" hasılatı NET'tir; aylık satış kartı aynı sayıyı netSatisTl ile gösterir.
        var aylikIadeler = HareketToplami.Tur(kirilim, "SATIS_IADE");
        var aylikIadeTl  = aylikIadeler.Sum(x => x.TlMutlak);
        // PAKET RAPOR-4 (YÜKSEK, tasarım hakemi): "Alış-satış nakit farkı" BRÜT satıştan hesaplanıyordu
        // (658.028,20 − 80.050 = 577.978,20) ama aynı ekrandaki satış kartı NET (635.528,20) → patron çıkarma
        // yapınca 22.500 ₺ tutmuyordu; Panel "Günün nakit akışı" ise net satış − alış − hurda diyordu.
        // TEK TANIM (Panel ile aynı): net satış − alış − hurda alışı.
        var aylikHurdaTl = HareketToplami.Tur(kirilim, "HURDA_ALIS").Sum(x => x.TlMutlak);

        // ── Günlük kırılım (SIRADAKI-ADIM-KARAR §2/Ö3) ────────────────────────
        // Panel.razor:230 ve Raporlar.razor:187 bu alanı bekliyordu, hiç dönmüyordu →
        // iki sayfada grafik kalıcı boştu. Gün sınırı işletme saatine göre (K5 ile
        // aynı ilke); ay içindeki her gün sıfır dolgulu döner, bugünden sonrası dönmez.
        var ilkGun = new DateOnly(yil, 1, 1).AddMonths(ay - 1);
        var sonGun = ilkGun.AddMonths(1).AddDays(-1);
        var bugun  = Zaman.YerelBugun();
        if (sonGun > bugun) sonGun = bugun;

        // ─ P-25 / B-8 — GÜNLÜK KIRILIM DE SUNUCUDA ──────────────────────────
        // Eski hâl bellekteki satırları gruplayabiliyordu çünkü satırlar ZATEN
        // bellekteydi. Artık SQL gruplar. Yerel güne çevrim: Türkiye 2016'dan
        // beri kalıcı UTC+3'tür (yaz saati yok), bu yüzden dönemin başındaki
        // ofset ay boyunca sabittir ve `Tarih + ofset` ile DATE() alınabilir.
        // Ofset SABİT YAZILMAZ, Zaman.Isletme'den OKUNUR (tek kaynak).
        var ofsetSaat = Zaman.Isletme.GetUtcOffset(baslangic).TotalHours;
        var gunlukHam = await donemSorgu
            .Where(h => h.Tur == "SATIS" || h.Tur == "ALIS" || h.Tur == "SATIS_IADE")
            .GroupBy(h => new { h.Tur, Gun = h.Tarih.AddHours(ofsetSaat).Date })
            .Select(g => new
            {
                g.Key.Tur,
                g.Key.Gun,
                Tl = g.Sum(x => x.TlTutari),
                TlMutlak = g.Sum(x => Math.Abs(x.TlTutari)),
            })
            .ToListAsync();

        var gunlukSatis = gunlukHam.Where(x => x.Tur == "SATIS")
            .ToDictionary(x => DateOnly.FromDateTime(x.Gun), x => x.Tl);
        var gunlukAlis = gunlukHam.Where(x => x.Tur == "ALIS")
            .ToDictionary(x => DateOnly.FromDateTime(x.Gun), x => x.TlMutlak);
        // PAKET RAPOR-3 (hasım DÜŞÜK "grafik brüt, kart net"): günün NET satışı = satış − |iade| (storno çifti
        // donemSorgu'da zaten dışlı) — Panel "Son 7 günlük satış" grafiği kartla AYNI tanımı çizer.
        var gunlukIade = gunlukHam.Where(x => x.Tur == "SATIS_IADE")
            .ToDictionary(x => DateOnly.FromDateTime(x.Gun), x => x.TlMutlak);

        var gunler = new List<object>();
        for (var g = ilkGun; g <= sonGun; g = g.AddDays(1))
            gunler.Add(new
            {
                tarih   = g.ToString("yyyy-MM-dd"),
                satisTl = gunlukSatis.GetValueOrDefault(g, 0m),
                alisTl  = gunlukAlis.GetValueOrDefault(g, 0m),
                netSatisTl = gunlukSatis.GetValueOrDefault(g, 0m) - gunlukIade.GetValueOrDefault(g, 0m),
            });

        // ═════════════════════════════════════════════════════════════════════
        // UX-4d madde 12 — NET HAS GRAM TEK TANIM.
        // Aylık rapor "net has" diye satış/alış has farkını gösteriyordu; günlük
        // panel ise TÜM hareketlerin has toplamını (hurda, emanet, takas dâhil).
        // QA turu 1: aylık 8,12 < aynı ayın bir günündeki 53,04 — imkânsız görünen
        // bir çelişki. Tanım artık GünlükÖzet ile BİREBİR aynı ifadedir:
        //     netHasGramHareketi = o dönemdeki tüm hareketlerin HasGrami toplamı
        // (aynı formül GunlukOzet içinde de kullanılır; ikisi bilerek tek satır).
        // ═════════════════════════════════════════════════════════════════════
        // K20-2: "net has" ALTIN has gramıdır; diğer madenler ayrı kırılımdadır.
        var netHasGramHareketi = HareketToplami.AltinHas(kirilim);
        var aylikMadenDagilimi = HareketToplami.MadenDagilimi(kirilim);

        // ═════════════════════════════════════════════════════════════════════
        // UX-4d madde 21 — KÂR TEK TANIM (KarZararServisi).
        // Buradaki "karTl = satış − alış" bir KÂR değil, dönemin mal alım/satım
        // NAKİT farkıdır; satılan malın maliyetini (parti tüketimi) hiç görmez.
        // Aynı ay için /raporlar −31.800, /raporlar/kar-zarar +20.200 gösteriyordu.
        // Tek yetkili tanım KarZararServisi'dir; eski sayı ADIYLA (satisAlisFarkiTl)
        // korunur ki rapor ekranı onu "mal alım-satım farkı" diye yazabilsin.
        // KarZararServisi verilmemişse (DI'sız test kurulumları) kâr alanı NULL
        // döner — sıfır YAZILMAZ (Anayasa md.1: bilinmeyen sıfır değildir).
        // ═════════════════════════════════════════════════════════════════════
        decimal? gercekKarTl = null;
        object? karZararOzeti = null;
        // PAKET KAR-RAPOR: kâr durumu ve maliyeti eksik satışlar AYNI kaynaktan
        // (KarZararServisi) üst düzeye taşınır — ekran "KÂR = satış" basmaz.
        string karDurumu = "HESAPLANAMADI";
        int maliyetiEksikSatisAdedi = 0;
        int maliyetiEksikIadeAdedi = 0;   // MADEN-2: Rapor merkezi bandı maliyetsiz iadeyi de anar
        decimal maliyetiEksikTutarTl = 0m;
        if (karZarar is not null)
        {
            var ilk = new DateOnly(yil, 1, 1).AddMonths(ay - 1);
            var son = ilk.AddMonths(1).AddDays(-1);
            var kz = await karZarar.Donem(islem, ilk, son, subeId);
            karZararOzeti = kz;
            var alan = kz.GetType().GetProperty("karTl")?.GetValue(kz);
            if (alan is decimal d) gercekKarTl = d;
            if (kz.GetType().GetProperty("karDurumu")?.GetValue(kz) is string kd) karDurumu = kd;
            if (kz.GetType().GetProperty("maliyetiEksikSatisAdedi")?.GetValue(kz) is int ea) maliyetiEksikSatisAdedi = ea;
            if (kz.GetType().GetProperty("maliyetiEksikIadeAdedi")?.GetValue(kz) is int ei) maliyetiEksikIadeAdedi = ei;
            if (kz.GetType().GetProperty("maliyetiEksikTutarTl")?.GetValue(kz) is decimal et) maliyetiEksikTutarTl = et;
        }

        return new
        {
            yil, ay,
            gorunumSubesi = subeId,
            kapsam = GorunumSubesi.Kapsam(subeId),
            // ═══ P-69 — ADLANDIRMA (QA turu 8) ═════════════════════════════════
            // `toplamHas` ölçüldüğünde YALNIZ ALTINDIR (Paket 20) ama adı "toplam"
            // der; kullanıcı 32,76 g'yi tüm madenlerin toplamı sanıyordu. Veri
            // doğru, ad yanlış. Eski alan ŞEKİL SÖZLEŞMESİ gereği KALDIRILMAZ
            // (P69_02 bekçisi); aynı değer doğru adıyla `altinToplamHas` olarak
            // da döner ve `madenDagilimi`'nin ALTIN satırına eşittir (P69_01).
            // ══════════════════════════════════════════════════════════════════
            satis    = new { sayisi = HareketToplami.Adet(satislar), toplamTl = satisTl, toplamHas = HareketToplami.AltinHas(satislar, mutlak: true), altinToplamHas = HareketToplami.AltinHas(satislar, mutlak: true), madenDagilimi = HareketToplami.MadenDagilimi(satislar, mutlak: true) },
            iade     = new { sayisi = HareketToplami.Adet(aylikIadeler), toplamTl = aylikIadeTl },
            netSatisTl = satisTl - aylikIadeTl,
            alis     = new { sayisi = HareketToplami.Adet(alislar),  toplamTl = alisTl,  toplamHas = HareketToplami.AltinHas(alislar), altinToplamHas = HareketToplami.AltinHas(alislar), madenDagilimi = HareketToplami.MadenDagilimi(alislar) },
            // QA turu 3: kaynak gün sonu mutabakatıyla AYNI — kasaya giren TL.
            tahsilat = new
            {
                toplamTl = Yuvarla2(kasaGirisleri),
                kaynak = "KASA_HAREKETLERI",
                cariTahsilatiTl = Yuvarla2(tahsilatlar.Sum(x => x.TlMutlak)),
                // RAPOR-3: gün sonundaki ayrı satırların aylık karşılığı (tahsilata girmez).
                virmanGiris = Yuvarla2(aylikVirmanGiris),
                acilisDevri = Yuvarla2(aylikAcilisDevri),
            },
            // QA turu 3: veresiye payı ödeme aracı raporuyla (R12) AYNI tanım.
            veresiye = new
            {
                toplamTl = Yuvarla2(veresiyeDoganTl),
                kaynak = "VERESIYE_PAYI",
                kapananTl = Yuvarla2(veresiyeKapananTl),
            },
            // UX-4d madde 21: TEK kâr tanımı — KarZararServisi.
            karTl    = gercekKarTl,
            karKaynagi = gercekKarTl is null ? "HESAPLANAMADI" : "KAR_ZARAR_SERVISI",
            karDurumu,
            maliyetiEksikSatisAdedi,
            maliyetiEksikIadeAdedi,
            maliyetiEksikTutarTl,
            satisAlisFarkiTl = satisTl - aylikIadeTl - alisTl - aylikHurdaTl,
            // RAPOR-4: farkın bileşenleri ekranda yazılabilsin diye hurda alışı da döner.
            hurdaAlisTl = aylikHurdaTl,
            karZarar = karZararOzeti,
            netHasGramHareketi,
            madenDagilimi = aylikMadenDagilimi,
            madenUyarisi  = MadenUyarisi(aylikMadenDagilimi),
            hareketSayisi = HareketToplami.Adet(kirilim),
            gunler,
        };
    }

    /// <summary>TL yuvarlama — para alanları 2 ondalık (bankacı yuvarlaması DEĞİL).</summary>
    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    /// <summary>PAKET RAPOR-GORUNUM (veri b): mutabakat hurda has gramı — YALNIZ ALTIN (boş maden = altın, K20-8).</summary>
    internal static decimal MutabakatAltinHas(IEnumerable<Hareket> hurdalar) =>
        Math.Round(hurdalar.Where(h => HareketToplami.MadenKodu(h.Maden) == Kuyumcu.Core.Domain.Maden.Altin)
                           .Sum(h => h.HasGrami), 6, MidpointRounding.AwayFromZero);

    /// <summary>PAKET RAPOR-GORUNUM (veri b): mutabakat hurdasının maden kırılımı — günlük özetle AYNI şekil
    /// (maden, madenAdi, toplamHas = o madenin saf gramı, satirSayisi); farklı madenler toplanmaz.</summary>
    internal static List<object> MutabakatMadenDagilimi(IEnumerable<Hareket> hurdalar) =>
        [.. hurdalar.GroupBy(h => HareketToplami.MadenKodu(h.Maden))
             .Select(g => new
             {
                 maden = g.Key,
                 madenAdi = Kuyumcu.Core.Domain.Maden.Ad(g.Key),
                 toplamHas = Math.Round(g.Sum(h => h.HasGrami), 6, MidpointRounding.AwayFromZero),
                 satirSayisi = g.Count(),
             })
             .Where(x => x.toplamHas != 0m)
             .OrderBy(x => x.maden == Kuyumcu.Core.Domain.Maden.Altin ? 0 : 1)
             .ThenByDescending(x => x.toplamHas)
             .Cast<object>()];

    /// <summary>
    /// PARA-DEMO-2 (madde 5) — gün sonu uyarı metnindeki tutar: ekranın Bicim.Tl biçimiyle aynı
    /// (tr-TR, kuruş 2 hane, eksi U+2212, "₺"). "TL" kodu ve sunucu kültürüne bağlı "N2" artık yok.
    /// </summary>
    internal static string TlMetni(decimal n)
    {
        var y = Yuvarla2(n);
        var govde = Math.Abs(y).ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
        return y < 0 ? $"\u2212{govde}\u00a0\u20ba" : $"{govde}\u00a0\u20ba";
    }

    // ─── Günlük POS–fatura mutabakatı (PLAN Adım 7) ───────────────────────────
    /// <summary>
    /// Gün sonu kapanışı: tahsilatlar ödeme türüne göre, satışlar faturalı/faturasız,
    /// kesilen faturalar ve aradaki FARK. KDV beyanının dayanağı budur.
    ///
    /// Eşleştirme `hareketler.fatura_no` üzerinden yapılır: faturalandırılan satışa
    /// fatura numarası damgalanır (FaturaServisi.Olustur, hareketId verildiğinde).
    /// Damgasız satış "faturasız" sayılır — bu bir SUÇLAMA değil, gün sonunda
    /// kuyumcunun bakması gereken listedir.
    ///
    /// Gün sınırı K5 kuralına uyar: Europe/Istanbul yerel günü UTC'ye çevrilir.
    /// </summary>
    /// <summary>
    /// P-25 / B-8 — satır satır EŞLEŞTİRİLEN kümelerin okuma tavanı.
    /// Toplamlar bu tavandan BAĞIMSIZDIR (sunucuda alınır); tavan yalnız
    /// fatura↔fiş bağı ve belge denetimi gibi eşleştirme adımlarını sınırlar.
    /// Tavana değildiyse rapor uyarı basar (UX-S29).
    /// </summary>
    public const int MutabakatSatirTavani = 2000;

    public async Task<object> PosFaturaMutabakati(string tarih, AktifKullanici islem, string? subeId = null)
    {
        RaporGunuDogrula(tarih);   // PAKET GOC-D: mutabakat günü Rapor penceresinde (ekran Tarih Tur=Rapor ile aynı kural)
        var gecerli = DateOnly.TryParse(tarih, CultureInfo.InvariantCulture, DateTimeStyles.None, out var yerelGun);
        var (gun, gunSonu) = gecerli
            ? Zaman.YerelGunAraligiUtc(yerelGun)
            : (DateTime.MinValue, DateTime.MinValue);

        // ═════════════════════════════════════════════════════════════════════
        // P-25 / B-8 — SUNUCUDA TOPLAMA (mutabakat ayağı).
        //
        // Eski hâl günün TÜM hareketlerini, TÜM kasa hareketlerini ve TÜM
        // faturalarını belleğe alıyordu. Üçü de artık ihtiyaca göre ayrışır:
        //
        //   • yalnız TOPLAM gereken yerde  → SQL `GROUP BY` / `SUM`
        //   • satır satır EŞLEŞTİRME gereken yerde (fatura↔fiş bağı, hurda
        //     belge denetimi) → satırlar okunur ama SAYFA SINIRIYLA.
        //
        // ⚠ DÜRÜSTLÜK (Anayasa md.1): satış satırları ve satış faturaları hâlâ
        //   okunur — `FaturaHareketBagi` bir EŞLEŞTİRMEDİR, toplam değildir ve
        //   SQL'de tek ifadeyle yazılamaz. Okuma artık SINIRLIDIR
        //   (`MutabakatSatirTavani`) ve sınıra değildiyse rapor bunu YAZAR
        //   (UX-S29: sessiz kırpma yok). Bu, kalan borcun gizlenmesi değil
        //   ölçülmesidir; P-25 beyaz listesinde ADIYLA durur.
        // ═════════════════════════════════════════════════════════════════════
        var hareketSorgu = db.Hareketler.AsNoTracking().Daralt(subeId)
            .Where(h => h.TenantId == islem.TenantId && h.Tarih >= gun && h.Tarih < gunSonu);
        // PAKET ŞUBE-KAPSAM-A: kasa_hareketleri şube taşımaz → şube KASADAN türetilir.
        var mutabakatKasaIdleri = subeId is null ? null
            : await db.Kasalar.AsNoTracking().Daralt(subeId).Select(k => k.Id)
                .Take(RaporFiltresi.EnFazlaLimit).ToListAsync();   // kasa sayısı onlarcadır (TB-19 sınırı)

        // Toplamlar: SUNUCUDA, satır okunmadan.
        var kirilim = await HareketToplami.Coz(hareketSorgu);
        var alisKirilim  = HareketToplami.Tur(kirilim, "ALIS");
        var satisKirilim = HareketToplami.Tur(kirilim, "SATIS");

        // Eşleştirme gereken satırlar: SATIS + HURDA_ALIS, sayfa sınırıyla.
        var satislar = await hareketSorgu.Where(h => h.Tur == "SATIS")
            .OrderByDescending(h => h.TlTutari).ThenBy(h => h.Id)
            .Take(MutabakatSatirTavani).ToListAsync();
        var hurdalar = await hareketSorgu.Where(h => h.Tur == "HURDA_ALIS")
            .OrderBy(h => h.Id).Take(MutabakatSatirTavani).ToListAsync();

        // Kasa girişleri: ödeme türü kırılımı SUNUCUDA toplanır.
        // PAKET Q10-1: storno çifti gün sonu tahsilatına girmez (aylık özetle AYNI kural).
        var kasaGirisKirilim = await db.KasaHareketleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.Tarih >= gun && k.Tarih < gunSonu
                     && k.Tutar > 0m
                     && (mutabakatKasaIdleri == null || mutabakatKasaIdleri.Contains(k.KasaId)))
            .StornoCiftiniDisla(db, islem.TenantId)
            .GroupBy(k => new { k.OdemeTuru, Virman = k.KarsiBacakId != null, k.ParaBirimi })
            .Select(g => new { g.Key.OdemeTuru, g.Key.Virman, g.Key.ParaBirimi, Tutar = g.Sum(x => x.Tutar) })
            .ToListAsync();

        // PARA-DEMO-2 (madde 5) — "TOPLAM TAHSİLAT" TANIMI: o gün MÜŞTERİDEN/İŞLEMDEN kasaya giren para.
        // DIŞARIDA kalır (ayrı satırda döner): (a) virman girişi — kasalar/banka arası aktarım, yeni para
        // değil (KarsiBacakId dolu bacak); (b) açılış devri — kurulumda kasaya yazılan devir. Eskiden ikisi
        // de toplama giriyor, tahsilatı şişiriyordu.
        // PARA-DEMO-2 tur 4 (doğrulayıcı bulgusu S11): açılış devri ödeme türüyle (DIGER) DEĞİL, yazan
        // kaydın kimliğiyle ayırt edilir — AcilisServisi kasa bacağını Tur="ACILIS" hareketine bağlar.
        // Kasaya elle girilen DIGER tutarı açılış devri DEĞİLDİR; tahsilatın "diğer" kaleminde kalır.
        // PAKET RAPOR-3 tur 2 (hasım ORTA "dövizli kasa"): TL tahsilatı YALNIZ TRY kasa girişidir — aylık Özet
        // (AylikOzet, ParaBirimi=="TRY") ile AYNI tanım. Eskiden 100 USD girişi "Nakit 100 ₺" diye TL toplamına
        // giriyordu (UX-S32). Dövizli girişler para birimiyle AYRI alanda döner (dovizGiris), asla toplanmaz.
        var virmanGiris = Yuvarla2(kasaGirisKirilim.Where(x => x.Virman && x.ParaBirimi == "TRY").Sum(x => x.Tutar));
        var acilisKirilim = await db.KasaHareketleri.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId && k.Tarih >= gun && k.Tarih < gunSonu
                     && k.Tutar > 0m && k.KarsiBacakId == null && k.HareketId != null
                     && (mutabakatKasaIdleri == null || mutabakatKasaIdleri.Contains(k.KasaId)))
            .StornoCiftiniDisla(db, islem.TenantId)
            .Join(db.Hareketler.AsNoTracking().Where(h => h.TenantId == islem.TenantId && h.Tur == "ACILIS"),
                  k => k.HareketId, h => h.Id, (k, h) => new { k.ParaBirimi, k.Tutar })
            .GroupBy(x => x.ParaBirimi)
            .Select(g => new { ParaBirimi = g.Key, Tutar = g.Sum(x => x.Tutar) })
            .ToListAsync();
        var acilisDevri = Yuvarla2(acilisKirilim.Where(x => x.ParaBirimi == "TRY").Sum(x => x.Tutar));
        var tahsilatKirilim = kasaGirisKirilim.Where(x => !x.Virman && x.ParaBirimi == "TRY").ToList();
        // Dövizli kasaya müşteriden giren para (virman ve açılış devri dışı), para birimi başına; TL'ye çevrilmez.
        var dovizGiris = kasaGirisKirilim.Where(x => !x.Virman && x.ParaBirimi != "TRY")
            .GroupBy(x => x.ParaBirimi)
            .Select(g => new
            {
                paraBirimi = g.Key,
                tutar = Yuvarla2(g.Sum(x => x.Tutar) - acilisKirilim.Where(a => a.ParaBirimi == g.Key).Sum(a => a.Tutar)),
            })
            .Where(x => x.tutar > 0m)
            .OrderBy(x => x.paraBirimi, StringComparer.Ordinal)
            .ToList();

        // İptal edilen fatura KDV beyanına girmez.
        var faturaSorgu = db.Faturalar.AsNoTracking()
            .Where(f => f.TenantId == islem.TenantId && f.Tarih >= gun && f.Tarih < gunSonu
                     && f.Durum != "IPTAL");

        // Fatura toplamları: SUNUCUDA, türe göre.
        var faturaKirilim = await faturaSorgu
            .GroupBy(f => f.Tur)
            .Select(g => new
            {
                Tur = g.Key,
                Adet = g.Count(),
                Toplam = g.Sum(x => x.Toplam),
                Kdv = g.Sum(x => x.KdvTutar),
            })
            .ToListAsync();

        // Yalnız SATIŞ faturaları satır satır okunur (fiş bağı için), sayfa sınırıyla.
        var faturalar = await faturaSorgu.Where(f => f.Tur == "SATIS")
            .OrderByDescending(f => f.Toplam).ThenBy(f => f.Id)
            .Take(MutabakatSatirTavani).ToListAsync();

        // ── Tahsilat: kasaya GİREN para, ödeme türüne göre (SUNUCU toplamı) ──
        var tahsilat = tahsilatKirilim
            .GroupBy(x => string.IsNullOrEmpty(x.OdemeTuru) ? "BELIRSIZ" : x.OdemeTuru)
            .ToDictionary(g => g.Key, g => Yuvarla2(g.Sum(x => x.Tutar)));
        // Açılış devri (yukarıda kimlikle bulundu) kendi ödeme türü kaleminden düşülür; toplama girmez.
        if (acilisDevri > 0m && tahsilat.TryGetValue("DIGER", out var digerHam))
        {
            var kalan = Yuvarla2(digerHam - acilisDevri);
            if (kalan > 0m) tahsilat["DIGER"] = kalan; else tahsilat.Remove("DIGER");
        }

        decimal Tur(string t) => tahsilat.TryGetValue(t, out var v) ? v : 0m;

        // ── Satışlar: faturalı / faturasız ───────────────────────────────────

        // ⚠ UX-4d madde 11: "faturalı" sayılmak için damga YETMEZ — damganın
        // işaret ettiği faturanın o gün İPTAL EDİLMEMİŞ olarak durması gerekir.
        // İptal edilen faturanın damgası hareketin üzerinde kalır; eskiden bu
        // satış "faturalı" görünüyor ama beyana giren faturası olmuyordu.
        var satisFaturalari = faturalar;

        // P-08 / Y-25 — BAĞ TEK YOLDAN OKUNUR (FaturaHareketBagi).
        // ⚠ Eski hâl `hareketler.fatura_no` metnini fatura numarasıyla
        // karşılaştırıyordu; otorite artık `fatura_kalemleri.hareket_id`'dir,
        // damga yalnız eski satırlar için o sınıfın İÇİNDE okunur.
        // Küme "o günün, iptal edilmemiş SATIŞ faturaları" ile SINIRLI kalır —
        // ertesi gün kesilen fatura bu günün raporunu değiştirmez.
        var faturaBagi = await FaturaHareketBagi.Coz(db, islem.TenantId, satislar, satisFaturalari);

        var faturali = satislar.Where(h => faturaBagi.ContainsKey(h.Id)).ToList();
        var faturasiz = satislar.Except(faturali).ToList();
        // PARA-DEMO-3 (hakem ORTA): gün sonu "Karışık" değil bacak türleri ("Kart + Havale") — CariModulu ekstresiyle aynı sorgu.
        var faturasizKarmaFis = faturasiz.Where(h => h.OdemeTuru == "KARMA").Select(h => h.FisId ?? h.Id)
            .Distinct(StringComparer.Ordinal).ToList();
        var faturasizKarma = faturasizKarmaFis.Count == 0
            ? new Dictionary<string, List<string>>(StringComparer.Ordinal)
            : (await db.OdemeKalemleri.AsNoTracking()
                    .Where(k => k.TenantId == islem.TenantId && faturasizKarmaFis.Contains(k.FisId) && !k.Bilinmiyor)
                    .Select(k => new { k.FisId, k.Arac, k.Sira })
                    .ToListAsync())
                .GroupBy(k => k.FisId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Sira).Select(x => x.Arac).Distinct(StringComparer.Ordinal).ToList(),
                    StringComparer.Ordinal);

        // ⚠ P-25: satış TOPLAMI sayfadan DEĞİL, SUNUCU kırılımından gelir —
        // tavana değen bir günde "satış toplamı" sayfa toplamına düşseydi FARK
        // sessizce sıfırlanırdı (TB-19 sözleşmesi: toplam sayfadan türetilmez).
        var satisToplam = Yuvarla2(satisKirilim.Sum(x => x.TlToplam));
        var faturaToplam = Yuvarla2(satisFaturalari.Sum(f => f.Toplam));

        // ── Alış tarafı (ADIM-6-7-8 §6/4: yalnız SATIS sayılıyordu) ──────────
        var alisFaturaOzet = faturaKirilim.FirstOrDefault(x => x.Tur == "ALIS");

        var pusulalar = await db.GiderPusulalari.AsNoTracking().Daralt(subeId)
            .Where(g => g.TenantId == islem.TenantId && g.TeslimTarihi >= gun && g.TeslimTarihi < gunSonu)
            .OrderBy(g => g.Id)
            .Take(MutabakatSatirTavani)
            .ToListAsync();

        // Belgesiz alım: mükelleften alımda tedarikçi fatura no, diğerinde pusula olmalı.
        // ⚠ P-08 BEYAZ LİSTE — bu satır fatura BAĞI kurmaz. HURDA_ALIS
        // hareketinde `fatura_no` bizim belgemiz değil TEDARİKÇİNİN fatura
        // numarasıdır (SatisModulu: `FaturaNo = dto.Satici.TedarikciFaturaNo`).
        // Sorulan soru "bu alımın BİR belgesi var mı"dır, "hangi faturamıza
        // bağlı"dır değil; bu yüzden FaturaHareketBagi'ye taşınmaz.
        // ⚠ YASAL-4 / Y4-A (hasım bulgusu 01.10): belge durumu HurdaServisi.SureTakibi ile
        //   AYNI türetmeden (GiderPusulasiSuresi.Durum). İptal edilmiş pusula BELGE SAYILMAZ;
        //   iptal edilmemiş pusula süresinde düzenlenmiştir → "7 gün doldu" alarmı ÜRETMEZ.
        //   Son günü geçmiş iptal+yenisiz = belgesiz alış; günü geçmemiş = yeniden düzenlenecek.
        var bugunYerel = YasalParametreServisi.IslemGunu(DateTime.UtcNow);
        var pusulaByHareket = pusulalar.ToLookup(p => p.HareketId, StringComparer.Ordinal);
        var hurdaDurumlari = hurdalar
            .Where(x => Hurda.TedarikciFaturasi.YokMu(x.FaturaNo))
            .Select(h => (Hareket: h, Durum: Hurda.GiderPusulasiSuresi.Durum(pusulaByHareket[h.Id], hurdaAyar, yasal, bugunYerel)))
            .ToList();
        var belgesizHurda = hurdaDurumlari.Where(x => x.Durum == Hurda.PusulaBelgeDurumu.Belgesiz).Select(x => x.Hareket).ToList();

        var uyarilar = new List<string>();
        if (belgesizHurda.Count > 0)
            uyarilar.Add($"{belgesizHurda.Count} hurda alımının belgesi yok (ne geçerli gider pusulası ne tedarikçi faturası)");

        // Alan adı (gecikmisPusulaSayisi) istemci sözleşmesi için korunur; anlamı artık
        // "iptal edilmiş, yerine yenisi düzenlenmemiş, son günü GEÇMEMİŞ pusula"dır.
        var gecikmisPusula = hurdaDurumlari.Where(x => x.Durum == Hurda.PusulaBelgeDurumu.YenidenDuzenlenecek).ToList();
        if (gecikmisPusula.Count > 0)
            uyarilar.Add($"{gecikmisPusula.Count} iptal edilmiş gider pusulası yeniden düzenlenmeli (7 günlük süre bitmeden)");

        if (faturasiz.Count > 0)
            uyarilar.Add($"{faturasiz.Count} satış faturalandırılmamış (toplam {TlMetni(faturasiz.Sum(h => h.TlTutari))})");


        // ═════════════════════════════════════════════════════════════════════
        // UX-4d madde 11 — FATURA EŞLEMESİ ve FARK'IN TEK TANIMI.
        //
        // QA turu 1 kanıtı: aynı ekranda "SATIŞ FATURASI ₺68.000" yazarken
        // "Faturalı 0 · ₺0 / Faturasız 4 · ₺173.200" yazıyordu ve FARK 105.200
        // görünüyordu. Çelişki değil, EKSİK ANLATIMDI: `hareketler.fatura_no`
        // yalnız fatura FİŞTEN kesildiğinde damgalanır; elle (hareketsiz) kesilen
        // fatura hiçbir satışa bağlanmaz. Rapor bu iki kümeyi karşılaştırıyor,
        // ama bağsız fatura kümesini HİÇ göstermiyordu.
        //
        // TEK TANIM (bu satır belgedeki tanımdır, rapor da aynısını yazar):
        //     FARK = satış toplamı
        //            − satış fişlerine BAĞLI (ve iptal edilmemiş) faturalar
        //            − hiçbir fişe bağlanmamış satış faturaları
        // Her satışın geçerli bir faturası varsa, faturaların tutarı satışlarla
        // örtüşüyorsa ve bağsız fatura yoksa FARK sıfırdır. Üç sapma türünün
        // (faturasız satış · tutar sapması · bağsız fatura) HEPSİ bu tek
        // formülde görünür; hiçbiri sessizce yutulmaz.
        // ═════════════════════════════════════════════════════════════════════
        // P-08 / Y-25 — bağlı/bağsız ayrımı da AYNI tek yoldan okunur;
        // ikinci bir eşleştirme kuralı yazılmaz (iki paralel bağ = çifte sayma).
        var bagliFaturaIdler = faturaBagi.Values.Select(b => b.FaturaId).ToHashSet(StringComparer.Ordinal);

        var bagliFaturalar = satisFaturalari.Where(f => bagliFaturaIdler.Contains(f.Id)).ToList();
        var bagimsizFaturalar = satisFaturalari.Where(f => !bagliFaturaIdler.Contains(f.Id)).ToList();

        // PAKET ŞUBE-KAPSAM-A: fatura kiracı genelidir (şube taşımaz — mimari red
        // korunur). Şube görünümünde yalnız o şubenin FİŞLERİNE bağlı faturalar
        // şubeye atanabilir; bağsız fatura hiçbir şubeye ait değildir → FARK
        // hesabına girmez ve bu SÖYLENİR (sessiz düşürme yok).
        var subeyeAtanamayanFaturaAdet = 0;
        if (subeId is not null && bagimsizFaturalar.Count > 0)
        {
            subeyeAtanamayanFaturaAdet = bagimsizFaturalar.Count;
            uyarilar.Add(
                $"{bagimsizFaturalar.Count} satış faturası hiçbir fişe bağlı olmadığı için bir şubeye "
                + "atanamadı; şube görünümünde fark hesabına girmedi (Tüm şubeler görünümünde görünür).");
            bagimsizFaturalar = [];
        }

        var faturasizTl = Yuvarla2(faturasiz.Sum(h => h.TlTutari));
        var bagliFaturaTl = Yuvarla2(bagliFaturalar.Sum(f => f.Toplam));
        var bagimsizFaturaTl = Yuvarla2(bagimsizFaturalar.Sum(f => f.Toplam));

        var fark = Yuvarla2(satisToplam - bagliFaturaTl - bagimsizFaturaTl);
        if (bagimsizFaturalar.Count > 0)
            uyarilar.Add(
                $"{bagimsizFaturalar.Count} satış faturası hiçbir satış fişine bağlı değil "
                + $"(toplam {TlMetni(bagimsizFaturaTl)}) — elle kesilmiş olabilir");
        // PAKET KAR-RAPOR tur 2 (PARA-DEMO-2 hakemi "aynı tutar için iki sarı uyarı"):
        // fark TAMAMEN faturalanmamış satıştan geliyorsa (bağsız fatura yok, fark =
        // faturasız tutar) yukarıdaki "N satış faturalandırılmamış (toplam X)" uyarısı
        // aynı tutarı zaten söyler — ikinci uyarı basılmaz. Farkın başka bileşeni
        // varsa (bağsız fatura, kısmi faturalama) uyarı aynen kalır.
        var farkYalnizFaturasizdan = bagimsizFaturalar.Count == 0 && faturasiz.Count > 0 && fark == faturasizTl;
        if (fark != 0m && !farkYalnizFaturasizdan)
            uyarilar.Add(
                $"Faturalanmamış satış ile fişe bağlanmamış fatura arasında {TlMetni(Math.Abs(fark))} fark var");

        if (tahsilat.ContainsKey("BELIRSIZ"))
            uyarilar.Add("Ödeme türü boş kasa hareketi var — POS/nakit ayrımı yapılamıyor");

        // UX-S29 / Anayasa md.1: satır satır eşleştirilen küme tavana değdiyse
        // rapor bunu SÖYLER. Sessiz kırpma "mutabık" yalanı üretirdi.
        if (satislar.Count >= MutabakatSatirTavani || satisFaturalari.Count >= MutabakatSatirTavani
            || hurdalar.Count >= MutabakatSatirTavani || pusulalar.Count >= MutabakatSatirTavani)
            uyarilar.Add(
                $"Bu günde eşleştirilen satır sayısı {MutabakatSatirTavani} sınırına değdi; "
                + "fatura–fiş eşlemesi bu sınıra kadar okundu. Toplamlar TAM, eşleşme listesi kırpılmıştır.");

        // ═════════════════════════════════════════════════════════════════════
        // PAKET 31 Faz B — POS BLOĞU. Mevcut tahsilat kırılımı (yukarısı)
        // DEĞİŞMEDİ; bu blok YANINA eklendi. Kaynak pos_tahsilatlari YAN
        // DEFTERİDİR: kasa bakiyesine dokunmaz, yalnız "bankadan ne zaman ne
        // kadar yatacak" sorusunu cevaplar.
        // ═════════════════════════════════════════════════════════════════════
        var posGunluk = await db.PosTahsilatlari.AsNoTracking().Daralt(subeId)
            .Where(p => p.TenantId == islem.TenantId && p.IslemTarihi >= gun && p.IslemTarihi < gunSonu
                     && p.Durum != Data.PosTahsilatDurumu.Iptal)
            .GroupBy(p => 1)
            .Select(g => new
            {
                Adet = g.Count(),
                Brut = g.Sum(x => x.BrutTutar),
                Komisyon = g.Sum(x => x.KomisyonTutar),
                Net = g.Sum(x => x.NetTutar),
                Yatan = g.Sum(x => x.YatanTutar),
            })
            .FirstOrDefaultAsync();

        // Valörü GEÇMİŞ ama hâlâ yatmamış tutar — gün süzgecinden BAĞIMSIZDIR
        // (UX-S33: "bugün itibarıyla"). Sunucuda toplanır, satır listesi dönmez.
        var simdi = DateTime.UtcNow;
        var gecikmis = await db.PosTahsilatlari.AsNoTracking().Daralt(subeId)
            .Where(p => p.TenantId == islem.TenantId && p.ValorTarihi < simdi
                     && (p.Durum == Data.PosTahsilatDurumu.Bekliyor
                         || p.Durum == Data.PosTahsilatDurumu.Kismi))
            .GroupBy(p => 1)
            .Select(g => new { Adet = g.Count(), Kalan = g.Sum(x => x.NetTutar - x.YatanTutar) })
            .FirstOrDefaultAsync();

        if (gecikmis is { Adet: > 0 })
            uyarilar.Add(
                $"{gecikmis.Adet} POS tahsilatının valörü geçti, banka yatması görünmüyor "
                + $"(toplam {TlMetni(gecikmis.Kalan)})");

        return new
        {
            tarih = gecerli ? yerelGun.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : tarih,
            gorunumSubesi = subeId,
            kapsam = GorunumSubesi.Kapsam(subeId),
            // Fatura bloğu (sayı/toplam/KDV) KİRACI GENELİDİR — fatura şube taşımaz.
            // Şube görünümünde FARK yalnız şubenin fişlerine bağlı faturalarla hesaplanır.
            faturaKapsami = GorunumSubesi.KiraciGeneli,
            subeyeAtanamayanFaturaAdet,
            // PAKET 31 Faz B — POS mutabakat bloğu (yan defter).
            pos = new
            {
                adet = posGunluk?.Adet ?? 0,
                brutKart = Yuvarla2(posGunluk?.Brut ?? 0m),
                komisyon = Yuvarla2(posGunluk?.Komisyon ?? 0m),
                beklenenNet = Yuvarla2(posGunluk?.Net ?? 0m),
                yatan = Yuvarla2(posGunluk?.Yatan ?? 0m),
                valoruGecmisBekleyenAdet = gecikmis?.Adet ?? 0,
                valoruGecmisBekleyenTutar = Yuvarla2(gecikmis?.Kalan ?? 0m),
            },
            tahsilat = new
            {
                nakit = Tur("NAKIT"),
                kart = Tur("KART"),          // POS
                havale = Tur("HAVALE"),
                diger = Yuvarla2(tahsilat.Where(x => x.Key is not ("NAKIT" or "KART" or "HAVALE"))
                                          .Sum(x => x.Value)),
                toplam = Yuvarla2(tahsilat.Values.Sum()),
                // PARA-DEMO-2 (madde 5): toplama GİRMEYEN kasa girişleri, ayrı satır.
                virmanGiris,
                acilisDevri,
                dovizGiris,   // RAPOR-3 tur 2: dövizli kasa girişleri (TL toplamına GİRMEZ)
            },
            satis = new
            {
                sayisi = HareketToplami.Adet(satisKirilim),
                toplamTl = satisToplam,
                faturaliSayisi = faturali.Count,
                faturaliTl = Yuvarla2(faturali.Sum(h => h.TlTutari)),
                faturasizSayisi = faturasiz.Count,
                faturasizTl,
            },
            // UX-4d madde 11: fatura↔fiş eşlemesinin İKİ yönü de görünür.
            faturaEslesme = new
            {
                bagliSayisi = bagliFaturalar.Count,
                bagliTl = bagliFaturaTl,
                bagimsizSayisi = bagimsizFaturalar.Count,
                bagimsizTl = bagimsizFaturaTl,
                faturasizTl,
                farkTanimi = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar"),
                bagimsizFaturalar = bagimsizFaturalar
                    .OrderByDescending(f => f.Toplam)
                    .Select(f => new { f.Id, f.FaturaNo, f.Tarih, f.Toplam, f.CariId, f.Durum })
                    .ToList(),
            },
            alis = new
            {
                sayisi = HareketToplami.Adet(alisKirilim),
                toplamTl = Yuvarla2(alisKirilim.Sum(x => x.TlMutlak)),
                faturaSayisi = alisFaturaOzet?.Adet ?? 0,
                faturaToplamTl = Yuvarla2(alisFaturaOzet?.Toplam ?? 0m),
            },
            hurda = new
            {
                sayisi = hurdalar.Count,
                odenenTl = Yuvarla2(hurdalar.Sum(h => Math.Abs(h.TlTutari))),
                // PAKET RAPOR-GORUNUM (veri b): `hasGrami` TÜM madenleri topluyordu (altın has + gümüş saf
                // gram tek sayı). "Has" YALNIZ ALTINDIR (K20-2 / P-69): alan adı korunur (eski istemci),
                // değeri yalnız altın; aynı değer doğru adıyla `altinHasGrami`; diğer madenler
                // `madenDagilimi` altında AYRI (gramları toplanmaz) — gunluk ozetle aynı kalıp.
                hasGrami = MutabakatAltinHas(hurdalar),
                altinHasGrami = MutabakatAltinHas(hurdalar),
                madenDagilimi = MutabakatMadenDagilimi(hurdalar),
                pusulaSayisi = pusulalar.Count,
                pusulaBrutTl = Yuvarla2(pusulalar.Sum(p => p.BrutTutar)),
                stopajToplamTl = Yuvarla2(pusulalar.Sum(p => p.StopajTutar)),
                belgesizSayisi = belgesizHurda.Count,
                gecikmisPusulaSayisi = gecikmisPusula.Count,
            },
            fatura = new
            {
                // P-25: TÜM fatura toplamı SUNUCUDAN — sayfadan türetilmez.
                sayisi = faturaKirilim.Sum(x => x.Adet),
                toplamTl = Yuvarla2(faturaKirilim.Sum(x => x.Toplam)),
                kdvToplam = Yuvarla2(faturaKirilim.Sum(x => x.Kdv)),
                satisFaturasiTl = faturaToplam,
            },
            // UX-S29 — SUNUCU SINIRI EKRANDA SÖYLENİR. Satır satır eşleştirilen
            // kümelerin tavanı ve tavana değilip değilmediği AÇIKÇA döner.
            satirTavani = new
            {
                tavan = MutabakatSatirTavani,
                satisTavanaDegdi = satislar.Count >= MutabakatSatirTavani,
                faturaTavanaDegdi = satisFaturalari.Count >= MutabakatSatirTavani,
                hurdaTavanaDegdi = hurdalar.Count >= MutabakatSatirTavani,
                pusulaTavanaDegdi = pusulalar.Count >= MutabakatSatirTavani,
            },
            fark,
            mutabik = fark == 0m && faturasiz.Count == 0
                      && bagimsizFaturalar.Count == 0 && belgesizHurda.Count == 0,
            // PAKET DIL-SUNUCU: uyarı cümleleri istek diline göre sözlükten (TR istekte AYNEN).
            uyarilar = uyarilar.Select(u => Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(u)).ToList(),
            faturasizIslemler = faturasiz
                .OrderByDescending(h => h.TlTutari)
                .Select(h => new
                {
                    h.Id, h.Tarih, h.TlTutari, h.OdemeTuru, h.CariId,
                    // PARA-DEMO-3 (hakem ORTA): gerçek karma satırın bacak araçları — ekstreyle AYNI kaynak
                    // (odeme_kalemleri, sıra korunur, bilinmeyen kalem araç sayılmaz). Yalnız KARMA'da dolu.
                    odemeAraclari = h.OdemeTuru == "KARMA" && faturasizKarma.TryGetValue(h.FisId ?? h.Id, out var ka) ? ka : null,
                })
                .ToList(),
        };
    }

    // ─── Cari yaşlandırma (aging) ─────────────────────────────────────────────
    /// <summary>
    /// Açık bakiyeli cariler:
    /// 0-30 gün, 31-60 gün, 61-90 gün, 90+ gün dilimlerine ve "vadesi bilinmiyor"
    /// (BELIRSIZ) dilimine göre gruplandırır.
    /// <para>
    /// PAKET RAPOR-YASLANDIRMA (GOC-47B § 0.7): açılış devri (<c>ACILIS</c>) neti
    /// bakiyedeyse ve ondan sonra başka yazım yoksa carinin yaşlanma tarihi
    /// BİLİNMİYOR sayılır (<see cref="CariYaslandirmaKurali"/>). Eski hâl açılış anını "son
    /// işlem" sayıp önceki programdan devralınmış yaşlı borcu 0-30 gün gösteriyordu.
    /// </para>
    /// </summary>
    /// <param name="limit">Sayfa boyu — TB-19 / MÜHÜR 5 kapısı; sınırsız döküm YOK.</param>
    /// <param name="ofset">Kaçıncı satırdan başlanacak.</param>
    public async Task<object> CariYaslandirma(AktifKullanici islem, int limit = 500, int ofset = 0)
    {
        // TB-19 / MÜHÜR 5 (34-Ö): bu uç "tüm carileri" çekiyordu; 50.000 carili
        // kiracıda hem sunucu belleği hem yanıt boyu doğrusal büyüyordu.
        // ⚠ YANIT ŞEKLİ DEĞİŞMEZ (Raporlar.razor:259-264 kayıtları kırılmasın):
        //   yaslananCariler + ozetTl AYNEN durur, yanına sayfalama alanları eklenir.
        // ⚠ ozetTl SAYFANIN DEĞİL, TÜM CARİLERİN toplamıdır — sunucuda 5 ayrı
        //   toplama sorgusuyla hesaplanır (M121: sayfadan türetilirse kırmızı).
        new RaporFiltresi(Limit: limit, Ofset: ofset).Dogrula();
        var simdi = DateTime.UtcNow;
        var tenantId = islem.TenantId;

        // Paket 27: döviz bakiyesi olan cari de "bakiyesi olan"dır. Süzgeçten
        // düşseydi 1.000 USD borçlu bir müşteri yaşlandırmada HİÇ görünmezdi.
        // PAKET CARI-BAKIYE (tek tanım): has bakiyesi EMANETSİZDİR (liste/detay/ekstreyle aynı
        // fonksiyon, CariBakiyeKurali.Gorunum). Yalnız emaneti olan müşteri "bakiyesi olan" sayılmaz.
        // PAKET RAPOR-YASLANDIRMA: döviz kolonları ELLE SAYILMAZ — DovizBirimleri tek
        // kaynağından türer (yeni birim kolonu eklenince süzgeç onu kendiliğinden kapsar).
        var acik = CariBakiyeKurali.Gorunum(db, tenantId).Where(AcikSuzgeci());

        var toplamSatir = await acik.CountAsync();

        // Yaşlanma tarihi (tur 2 — hasım KRİTİK + ORTA):
        //  · açılışı olmayan cari  → son işlem tarihi (eski model aynen);
        //  · açılış NET ≠ 0 (TL, has ya da HERHANGİ bir döviz birimi kendi içinde) VE
        //    son açılıştan sonra açılış olmayan yazım YOK → null (BELIRSIZ, neden ACILIS);
        //  · öteki durumlar → açılış OLMAYAN son hareketin tarihi (ters açılış anı
        //    "son işlem" sayılmaz: açılış geri alınınca kalan yeni borç 0-30'da kalır).
        // Döviz neti (CariId, ParaBirimi) ikilisine göre — farklı birimler toplanıp sıfırlanmaz.
        var ara = acik.Select(g => new
        {
            G = g,
            AcilisMax = db.Hareketler
                .Where(h => h.TenantId == tenantId && h.CariId == g.Bakiye.CariId
                         && h.Tur == CariYaslandirmaKurali.AcilisTuru)
                .Max(h => (DateTime?)h.Tarih),
            DigerMax = db.Hareketler
                .Where(h => h.TenantId == tenantId && h.CariId == g.Bakiye.CariId
                         && h.Tur != CariYaslandirmaKurali.AcilisTuru)
                .Max(h => (DateTime?)h.Tarih),
            AcilisNetVar =
                db.Hareketler
                    .Where(h => h.TenantId == tenantId && h.CariId == g.Bakiye.CariId
                             && h.Tur == CariYaslandirmaKurali.AcilisTuru)
                    .Sum(h => (decimal?)h.TlTutari) != 0m
             || db.Hareketler
                    .Where(h => h.TenantId == tenantId && h.CariId == g.Bakiye.CariId
                             && h.Tur == CariYaslandirmaKurali.AcilisTuru)
                    .Sum(h => (decimal?)h.HasGrami) != 0m
             || db.Hareketler
                    .Where(h => h.TenantId == tenantId && h.CariId == g.Bakiye.CariId
                             && h.Tur == CariYaslandirmaKurali.AcilisTuru && h.DovizTutar != null)
                    .GroupBy(h => h.ParaBirimi)
                    .Any(gr => gr.Sum(h => h.DovizTutar ?? 0m) != 0m),
        });
        var tarihli = ara.Select(x => new CariYaslandirmaKurali.Satir
        {
            G = x.G,
            AcilisDahil = x.AcilisNetVar,
            YasTarihi = x.AcilisMax == null
                ? x.G.Bakiye.SonIslemTarihi
                : x.AcilisNetVar && (x.DigerMax == null || x.DigerMax <= x.AcilisMax)
                    ? null
                    : x.DigerMax ?? x.G.Bakiye.SonIslemTarihi,
        });

        // Dilim eşikleri: floor((simdi−sonIslem).TotalDays) <= 30  ⟺  sonIslem > simdi−31g
        var e31 = simdi.AddDays(-31);
        var e61 = simdi.AddDays(-61);
        var e91 = simdi.AddDays(-91);
        var ozetTl = new Dictionary<string, decimal>
        {
            ["0-30"]  = await tarihli.Where(x => x.YasTarihi != null && x.YasTarihi > e31)
                                     .SumAsync(x => (decimal?)x.G.Bakiye.TlBakiye) ?? 0m,
            ["31-60"] = await tarihli.Where(x => x.YasTarihi != null && x.YasTarihi <= e31 && x.YasTarihi > e61)
                                     .SumAsync(x => (decimal?)x.G.Bakiye.TlBakiye) ?? 0m,
            ["61-90"] = await tarihli.Where(x => x.YasTarihi != null && x.YasTarihi <= e61 && x.YasTarihi > e91)
                                     .SumAsync(x => (decimal?)x.G.Bakiye.TlBakiye) ?? 0m,
            ["90+"]   = await tarihli.Where(x => x.YasTarihi != null && x.YasTarihi <= e91)
                                     .SumAsync(x => (decimal?)x.G.Bakiye.TlBakiye) ?? 0m,
            [CariYaslandirmaKurali.Belirsiz] = await tarihli.Where(x => x.YasTarihi == null)
                                     .SumAsync(x => (decimal?)x.G.Bakiye.TlBakiye) ?? 0m,
        };

        // Döviz toplamları (TÜM cariler, sunucuda) — vadelendirilmez, TL'ye karışmaz.
        // Kodlar DovizBirimleri'nden gelir: GBP (ve ileride eklenecek birim) kaçmaz.
        var ozetDoviz = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var kolon in DovizBirimleri.Kolonlar)
            ozetDoviz[DovizBirimleri.Kod(kolon)] =
                await acik.SumAsync(DovizBirimleri.ToplamSecici<CariBakiyeGorunumu>(g => g.Bakiye, kolon)) ?? 0m;

        var aktifBakiyeler = await tarihli
            .OrderByDescending(x => x.G.Bakiye.TlBakiye).ThenBy(x => x.G.Bakiye.CariId)
            .Skip(ofset).Take(limit)
            .ToListAsync();

        // Cari adları YALNIZ sayfadaki kimlikler için okunur (eski hâl tüm
        // carileri sözlüğe alıyordu — ikinci sınırsız okuma).
        var sayfaIdleri = aktifBakiyeler.Select(x => x.G.Bakiye.CariId).ToList();
        var cariMap = sayfaIdleri.Count > 0
            ? await db.Cariler.AsNoTracking()
                .Where(c => c.TenantId == tenantId && sayfaIdleri.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id)
            : new Dictionary<string, Data.Cari>();

        // Açılış devri hâlâ bakiyede mi (net ≠ 0)? — artık AYNI SQL projeksiyonundan
        // (Satir.AcilisDahil): dilim ile bayrak tek kuraldan beslenir, birimler ayrı.

        var yaslandirma = aktifBakiyeler.Select(x =>
        {
            var b = x.G.Bakiye;
            var gun = CariYaslandirmaKurali.GunSayisi(x.YasTarihi, simdi);
            var cari = cariMap.GetValueOrDefault(b.CariId);
            var dovizler = DovizBirimleri.Bakiyeler(b);
            var acilisli = x.AcilisDahil;

            return new
            {
                cariId        = b.CariId,
                cariAd        = cari?.Ad ?? "Bilinmiyor",
                cariTur       = cari?.Tur ?? "MUSTERI",
                tlBakiye      = b.TlBakiye,
                hasGramBakiye = x.G.EmanetsizHas,
                // Paket 27 Faz A: döviz bakiyesi GÖSTERİLİR ama VADELENDİRİLMEZ.
                // Bilgi saklanmaz (Anayasa md.1) — dilim TL'ye göre hesaplanır,
                // ekran "döviz bakiyesi var (vadelendirilmedi)" rozeti basar.
                // usd/eur/gbp alanları YANIT ŞEKLİ uyumu için durur; tek kaynak
                // dovizBakiyeleri'dir (DovizBirimleri — yeni birim burada kendiliğinden çıkar).
                usdBakiye     = b.UsdBakiye,
                eurBakiye     = b.EurBakiye,
                gbpBakiye     = b.GbpBakiye,
                dovizBakiyeleri  = dovizler,
                dovizBakiyesiVar = dovizler.Count > 0,
                gunSayisi     = gun,
                dilim         = CariYaslandirmaKurali.Dilim(gun),
                // BELIRSIZ nedeni: ACILIS (son yazım açılış devri) · TARIH_YOK (hiç işlem tarihi yok).
                belirsizNedeni = CariYaslandirmaKurali.BelirsizNedeni(gun, acilisli),
                acilisDahil   = acilisli,
            };
        }).ToList();

        // KVKK md.12 / ADR-0004 — cari adı dönen liste okuması iz bırakır (M115).
        await denetim.ListeOkumaKaydet(islem, nameof(Cari), yaslandirma.Count, "cari-yaslandirma");

        // Özet — sözlük anahtarları JSON'a aynen geçer ("0-30", ..., "BELIRSIZ")
        // ⚠ ozetTl yukarıda SUNUCUDA hesaplandı; sayfadan TÜRETİLMEZ.
        return new
        {
            // PAKET ŞUBE-KAPSAM-A § 4.2.3: cari şubeye bölünmez; subeId yok sayılır.
            kapsam = GorunumSubesi.KiraciGeneli,
            yaslananCariler = yaslandirma,
            ozetTl,
            ozetDoviz,
            toplamSatir,
            ofset,
            limit,
            dahaFazlasiVar = ofset + yaslandirma.Count < toplamSatir,
        };
    }

    /// <summary>
    /// Yaşlandırmaya giren "açık bakiyeli" cari süzgeci: TL, emanetsiz has ya da
    /// HERHANGİ bir döviz kolonu (DovizBirimleri — elle liste yok) sıfırdan farklı.
    /// </summary>
    internal static Expression<Func<CariBakiyeGorunumu, bool>> AcikSuzgeci()
    {
        Expression<Func<CariBakiyeGorunumu, bool>> tlHas = g =>
            g.Bakiye.TlBakiye > 0.01m || g.Bakiye.TlBakiye < -0.01m
            || g.EmanetsizHas > 0.000001m || g.EmanetsizHas < -0.000001m;
        var p = tlHas.Parameters[0];
        var doviz = DovizBirimleri.AcikIfadesi(Expression.Property(p, nameof(CariBakiyeGorunumu.Bakiye)), 0.01m);
        return Expression.Lambda<Func<CariBakiyeGorunumu, bool>>(Expression.OrElse(tlHas.Body, doviz), p);
    }


    // ─── Vade takibi ──────────────────────────────────────────────────────────
    /// <summary>
    /// Vadesi GEÇMİŞ ve YAKLAŞAN açık borçlar — müşteri müşteri.
    ///
    /// Yaşlandırma raporundan (<see cref="CariYaslandirma"/>) FARKI: orası
    /// borcun KAÇ GÜNDÜR durduğuna bakar (son işlem tarihi), burası SÖZ VERİLEN
    /// GÜNE bakar. 90 gün vadeli 60 günlük bir borç yaşlandırmada "61-90"
    /// diliminde görünür ama gecikmiş DEĞİLDİR; kasiyer ikisini karıştırırsa
    /// vadesi gelmemiş müşteriyi arar, geciken müşteriyi aramaz.
    /// </summary>
    /// <param name="ufukGun">Kaç gün ilerisine kadarki vadeler "yaklaşan" sayılsın.</param>
    public async Task<object> VadeTakibi(AktifKullanici islem, int ufukGun)
    {
        if (ufukGun < 0) throw new BadRequestException("Ufuk gün sayısı negatif olamaz");

        var now = DateTime.UtcNow;

        // YALNIZ BİZE BORÇLU cariler. Negatif bakiye bizim tedarikçiye borcumuzdur;
        // aynı tabloya konsaydı "kimden alacağım var" ekranı, kime borçlu olduğumuzu
        // da alacak gibi gösterirdi. Tedarikçi vadesi ayrı bir iştir, burada değildir.
        var bakiyeliler = await db.CariBakiyeler.AsNoTracking()
            .Where(b => b.TenantId == islem.TenantId && b.TlBakiye > 0m)
            .Select(b => b.CariId)
            .ToListAsync();

        // PAKET OLCEK-YUK (TB-19): yalnız BORÇLU carilerin kartı okunur. Eskiden kiracının
        // TÜM carileri (5.000 carili kiracıda 5.000 tam kart) her istekte belleğe alınıyordu;
        // sözlük yalnız `bakiyeliler` için sorgulandığından yanıt aynıdır.
        var cariMap = bakiyeliler.Count == 0
            ? new Dictionary<string, Data.Cari>()
            : await db.Cariler.AsNoTracking()
                .Where(c => c.TenantId == islem.TenantId && bakiyeliler.Contains(c.Id))
                .Take(bakiyeliler.Count)
                .ToDictionaryAsync(c => c.Id);

        var satirlar = new List<object>();
        decimal gecikenToplam = 0m, yaklasanToplam = 0m, vadesizToplam = 0m;
        // ⚠ UX-4d madde 16: UFKUN ÖTESİNDEKİ BORÇ GÖRÜNMÜYORDU.
        // Üç kart (geciken · yaklaşan · vadesiz) 7 günlük varsayılan ufukta
        // 76.000 TL açık alacak varken ₺0,00 gösteriyordu: vadesi 7 günden
        // uzak borç hiçbir kovaya düşmüyor, hiçbir kartta toplanmıyordu.
        // Artık ufkun ötesi AYRI toplanır ve dönemden bağımsız GENEL TOPLAM
        // her zaman döner (UX-S33: ufuk seçicisi bu kartı etkilemez).
        decimal ufukOtesiToplam = 0m;

        foreach (var cariId in bakiyeliler)
        {
            var (borclar, vadesiz) = await Modules.Vade.VadeKapisi.AcikBorclar(
                db, islem.TenantId, cariId, now);

            vadesizToplam += vadesiz;
            if (borclar.Count == 0 && vadesiz == 0m) continue;

            var cari = cariMap.GetValueOrDefault(cariId);
            var geciken = borclar.Where(b => b.GecikmeGun > 0).ToList();
            var yaklasan = borclar.Where(b => b.GecikmeGun <= 0 && -b.GecikmeGun <= ufukGun).ToList();

            var ufukOtesi = borclar.Where(b => b.GecikmeGun <= 0 && -b.GecikmeGun > ufukGun).ToList();

            gecikenToplam   += geciken.Sum(b => b.AcikTutar);
            yaklasanToplam  += yaklasan.Sum(b => b.AcikTutar);
            ufukOtesiToplam += ufukOtesi.Sum(b => b.AcikTutar);

            satirlar.Add(new
            {
                cariId,
                cariAd  = cari?.Ad ?? "Bilinmiyor",
                cariTur = cari?.Tur ?? "MUSTERI",
                telefon = cari?.Telefon,
                gecikenTutar  = geciken.Sum(b => b.AcikTutar),
                yaklasanTutar = yaklasan.Sum(b => b.AcikTutar),
                // UX-4d madde 16: vadesi ufkun ötesinde olan açık borç.
                ufukOtesiTutar = ufukOtesi.Sum(b => b.AcikTutar),
                acikToplamTutar = geciken.Sum(b => b.AcikTutar)
                                + yaklasan.Sum(b => b.AcikTutar)
                                + ufukOtesi.Sum(b => b.AcikTutar)
                                + vadesiz,
                // Vadesi bilinmeyen kısım AYRI kalemdir; bugüne yazılmaz.
                vadesizTutar  = vadesiz,
                enEskiGecikmeGun = geciken.Count == 0 ? (int?)null : geciken.Max(b => b.GecikmeGun),
                enYakinVade = borclar.Count == 0 ? (DateOnly?)null : borclar.Min(b => b.Vade),
                borclar = borclar.Select(b => new
                {
                    hareketId = b.HareketId,
                    tarih     = b.Tarih,
                    vade      = b.Vade,
                    acikTutar = b.AcikTutar,
                    gecikmeGun = b.GecikmeGun,
                    durum     = b.GecikmeGun > 0 ? "GECIKTI"
                              : -b.GecikmeGun <= ufukGun ? "YAKLASIYOR"
                              : "VADESINDE",
                }).ToList(),
            });
        }

        // KVKK md.12 — cari adı VE TELEFON dönen liste okuması iz bırakır (paket 34).
        await denetim.ListeOkumaKaydet(islem, nameof(Cari), satirlar.Count, "vade-takibi");

        return new
        {
            // PAKET ŞUBE-KAPSAM-A § 4.2.3: cari şubeye bölünmez; subeId yok sayılır.
            kapsam = GorunumSubesi.KiraciGeneli,
            ufukGun,
            gecikenToplam,
            yaklasanToplam,
            vadesizToplam,
            // UX-4d madde 16 — ufuk seçicisinden BAĞIMSIZ kalemler (UX-S33).
            ufukOtesiToplam,
            acikToplam = gecikenToplam + yaklasanToplam + ufukOtesiToplam + vadesizToplam,
            cariler = satirlar,
        };
    }

    // ─── Stok değer raporu ────────────────────────────────────────────────────
    /// <summary>
    /// Tüm ürünler için güncel stok adeti + toplam has gram.
    /// </summary>
    /// <param name="limit">Sayfa boyu — TB-19 / MÜHÜR 5; 50.000 ürünlü kiracıda sınırsız döküm YOK.</param>
    public async Task<object> StokDegeri(AktifKullanici islem, string? subeId, int limit = 500, int ofset = 0)
    {
        // TB-19 / MÜHÜR 5 (34-Ö): eski hâl hem TÜM stok satırlarını hem TÜM
        // ürünleri belleğe alıyordu. Artık gruplama ve toplamlar SUNUCUDA yapılır;
        // yalnız SAYFADAKİ ürünlerin kartı okunur.
        // ⚠ YANIT ŞEKLİ DEĞİŞMEZ: urunler + toplamHas + madenDagilimi + uyari.
        new RaporFiltresi(Limit: limit, Ofset: ofset).Dogrula();

        var q = db.Stoklar.AsNoTracking().Where(s => s.TenantId == islem.TenantId);
        q = q.Daralt(subeId);   // PAKET ŞUBE-KAPSAM-A: tek daraltma yolu

        // Ürün bazlı toplama — SUNUCUDA.
        var gruplu = q.GroupBy(s => s.UrunId)
            .Select(g => new
            {
                UrunId = g.Key,
                adet = g.Sum(x => x.Adet),
                // PAKET P-21 (Y-23): rezerve adet AYRI GÖSTERİLİR, adetten
                // DÜŞÜLMEZ. Düşülseydi sayım raporu raftaki malı eksik sayar,
                // sayan kişi "mal kayıp" derdi. Ayrı sütun hem fiziksel gerçeği
                // hem satılabilirliği söyler (plan DoD: "sayım raporu blokeyi
                // ayrı gösterir").
                blokeAdet = g.Sum(x => x.BlokeAdet),
                toplamHas = g.Sum(x => x.ToplamHasGram),
            });

        var toplamSatir = await gruplu.CountAsync();

        // ── Maden dağılımı TÜM ürünler üzerinden (sayfadan DEĞİL) ───────────
        // Maden, ayarId'den TÜRETİLİR (Ayarlar.MadenBul bellek içi tablodur);
        // bu yüzden SQL'de ayarId'ye göre gruplanır, maden eşlemesi sonra yapılır.
        // Ayar sayısı ONLARCADIR — sınırsız okuma değildir.
        var ayarToplam = await (
            from s in q
            join u in db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId)
                on s.UrunId equals u.Id
            // RAPOR-GORUNUM: tartı gramı (adet × ürün gramı) — altın dışı madenin toplam satırı
            // "90,00 g (saf 83,25 g)" yazabilsin (istemci toplamaz; reyon-stok fizikselGram ile aynı tanım).
            group new { s.Adet, s.ToplamHasGram, u.GramAgirlik } by u.AyarId into g
            select new { AyarId = g.Key, adet = g.Sum(x => x.Adet), gram = g.Sum(x => x.ToplamHasGram),
                         fiziksel = g.Sum(x => x.Adet * x.GramAgirlik) })
            .ToListAsync();

        // ⚠ FARKLI MADENLER TOPLANMAZ (2026-09-14 düzeltmesi).
        // "Has" bir ALTIN kavramıdır; toplam yalnız altını sayar, diğer madenler
        // ayrı kırılımda verilir (100 g altın + 500 g gümüş = "562,5 g has" DEĞİL).
        var madenDagilimi = ayarToplam
            .GroupBy(x => Kuyumcu.Core.Domain.Ayarlar.MadenBul(x.AyarId ?? "")
                          ?? Kuyumcu.Core.Domain.Maden.Altin)
            .Select(g => new
            {
                maden = g.Key,
                madenAdi = Kuyumcu.Core.Domain.Maden.Ad(g.Key),
                adet = g.Sum(x => x.adet),
                toplamGram = Math.Round(g.Sum(x => x.gram), 6),
                fizikselGram = Math.Round(g.Sum(x => x.fiziksel), 4, MidpointRounding.AwayFromZero),
            })
            .OrderByDescending(x => x.toplamGram)
            .ToList();

        var altinHas = madenDagilimi
            .Where(x => x.maden == Kuyumcu.Core.Domain.Maden.Altin)
            .Sum(x => x.toplamGram);

        // ── Sayfa: ALTIN önce, sonra diğer madenler (Maden.Hepsi sırası), maden içinde miktara göre ──
        // RAPOR-GORUNUM tur 2 (hasım DÜŞÜK): istemci sayfayı kendi içinde "altın önce" diye yeniden
        // sıralıyordu → 2. sayfada genel sıra bozulurdu. Sıra artık SAYFALAMADAN ÖNCE, SQL'de: maden
        // ayarId'den türetildiği için madenin ayar kimlikleri (bellek içi Ayarlar tablosu) IN listesi olur;
        // ayarı bilinmeyen/boş ürün ALTIN sayılır (satır eşlemesiyle aynı kural: MadenBul ?? Altin).
        // (yerel işlev değil lambda: denetim.sh sınırsız-okuma ölçümü metot adını bildirimden okur)
        Func<string, string[]> ayarlariOf = maden =>
            [.. Kuyumcu.Core.Domain.Ayarlar.Tumu.Where(kv => kv.Value.Maden == maden).Select(kv => kv.Key)];
        var gumusAyar = ayarlariOf(Kuyumcu.Core.Domain.Maden.Gumus);
        var platinAyar = ayarlariOf(Kuyumcu.Core.Domain.Maden.Platin);
        var paladyumAyar = ayarlariOf(Kuyumcu.Core.Domain.Maden.Paladyum);
        var celikAyar = ayarlariOf(Kuyumcu.Core.Domain.Maden.Celik);
        var sirali =
            from g in gruplu
            join u in db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId)
                on g.UrunId equals u.Id into uj
            from u in uj.DefaultIfEmpty()
            select new
            {
                g.UrunId, g.adet, g.blokeAdet, g.toplamHas,
                madenSira = u == null ? 0
                    : gumusAyar.Contains(u.AyarId) ? 1
                    : platinAyar.Contains(u.AyarId) ? 2
                    : paladyumAyar.Contains(u.AyarId) ? 3
                    : celikAyar.Contains(u.AyarId) ? 4 : 0,
            };
        var sayfa = await sirali
            .OrderBy(x => x.madenSira).ThenByDescending(x => x.toplamHas).ThenBy(x => x.UrunId)
            .Skip(ofset).Take(limit)
            .Select(x => new { x.UrunId, x.adet, x.blokeAdet, x.toplamHas })
            .ToListAsync();

        var sayfaIdleri = sayfa.Select(x => x.UrunId).ToList();
        var urunMap = sayfaIdleri.Count == 0
            ? new Dictionary<string, Data.Urun>()
            : await db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == islem.TenantId && sayfaIdleri.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

        var satirlar = sayfa.Select(x =>
        {
            var urun = urunMap.GetValueOrDefault(x.UrunId);
            var ayarId = urun?.AyarId ?? "";
            return new
            {
                urunId    = x.UrunId,
                kod       = urun?.Kod ?? "",
                ad        = urun?.Ad ?? "",
                ayarId,
                maden     = Kuyumcu.Core.Domain.Ayarlar.MadenBul(ayarId)
                            ?? Kuyumcu.Core.Domain.Maden.Altin,
                adet      = x.adet,
                blokeAdet = x.blokeAdet,
                satilabilirAdet = Math.Round(x.adet - x.blokeAdet, 3, MidpointRounding.AwayFromZero),
                // ⚠ ALAN ADI KORUNDU: `toplamHas` bu satırda ürünün KENDİ madeninin saf gramıdır
                //   (gümüşte saf gümüş, has DEĞİL). MADEN-AYRIMI: ekran birimi `maden`den seçer;
                //   tartı gramı ayrı alan (ürün kartı bulunamazsa bilinmez → null).
                toplamHas = x.toplamHas,
                fizikselGram = urun is null ? (decimal?)null
                    : Math.Round(x.adet * urun.GramAgirlik, 4, MidpointRounding.AwayFromZero),
            };
        }).ToList();

        return new
        {
            urunler = satirlar,
            toplamHas = Math.Round(altinHas, 6),   // YALNIZ ALTIN
            madenDagilimi,
            // P-21 (Y-23): sayfadaki rezerve toplamı — ekran "şu kadarı ayrılmış"
            // diyebilsin ve o sayı rezervasyon listesine bağlansın (UX-S23).
            toplamBlokeAdet = Math.Round(satirlar.Sum(x => x.blokeAdet), 3, MidpointRounding.AwayFromZero),
            toplamSatir,
            ofset,
            limit,
            dahaFazlasiVar = ofset + satirlar.Count < toplamSatir,
            uyari = madenDagilimi.Count > 1
                ? "Stokta birden çok maden var. `toplamHas` yalnız ALTIN has gramıdır; " +
                  "diğer madenler `madenDagilimi` altında ayrı verilir — farklı madenlerin " +
                  "gramları toplanmaz."
                : null,
        };
    }

    // ─── Reyon stok raporu (paket 25 Faz A) ──────────────────────────────────
    /// <summary>
    /// Reyon bazında adet ve has gram. İki dürüstlük kuralı:
    ///  1. FARKLI MADENLER TOPLANMAZ (StokDegeri'ndeki 2026-09-14 kararı):
    ///     toplam yalnız ALTIN has gramıdır, diğer madenler ayrı satırdadır.
    ///  2. ATANMAMIŞ mal AYRI SATIRDIR ve gizlenmez: şube stoğu ile yerleşim
    ///     toplamı arasındaki fark "fiziksel yeri bildirilmemiş mal"dır.
    ///
    /// TB-19: gruplama SUNUCUDA yapılır (GroupBy → ToListAsync); StokDegeri'nin
    /// sınırsız ToListAsync deseni KOPYALANMAZ.
    /// </summary>
    public async Task<object> ReyonStok(AktifKullanici islem, string? subeId, string? reyonId,
                                        string? maden)
    {
        var q = db.StokYerlesimleri.AsNoTracking()
            .Where(y => y.TenantId == islem.TenantId);
        q = q.Daralt(subeId);   // PAKET ŞUBE-KAPSAM-A: tek daraltma yolu
        if (!string.IsNullOrEmpty(reyonId)) q = q.Where(y => y.ReyonId == reyonId);

        // GROUP BY reyon + ayar — toplama veritabanında biter, satırlar
        // uygulamaya TEK TEK taşınmaz.
        var gruplar = await q
            .Join(db.Urunler.Where(u => u.TenantId == islem.TenantId),
                  y => y.UrunId, u => u.Id, (y, u) => new { y, u })
            .GroupBy(x => new { x.y.ReyonId, x.y.SubeId, x.u.AyarId })
            .Select(g => new
            {
                g.Key.ReyonId,
                g.Key.SubeId,
                g.Key.AyarId,
                Adet = g.Sum(x => x.y.Adet),
                HasGram = g.Sum(x => x.y.Adet * x.u.HasGrami),
                // MADEN-2 (tasarım ORTA): reyon satırı da Stok değeri gibi "fiziksel (saf)" yazsın — tartı gramı.
                FizikselGram = g.Sum(x => x.y.Adet * x.u.GramAgirlik),
                UrunSayisi = g.Count(),
            })
            .ToListAsync();

        var reyonAdlari = await db.Set<Reyon>().AsNoTracking()
            .Where(r => r.TenantId == islem.TenantId)
            .Select(r => new { r.Id, r.Ad, r.Tip, r.Aktif, r.SorumluKullaniciId })
            .ToListAsync();
        var subeAdlari = await db.Subeler.AsNoTracking()
            .Where(x => x.TenantId == islem.TenantId)
            .ToDictionaryAsync(x => x.Id, x => x.Ad);

        var madenli = gruplar.Select(g => new
        {
            g.ReyonId, g.SubeId, g.Adet, g.HasGram, g.FizikselGram, g.UrunSayisi,
            Maden = Kuyumcu.Core.Domain.Ayarlar.MadenBul(g.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
        }).ToList();

        if (!string.IsNullOrEmpty(maden))
            madenli = madenli.Where(x => x.Maden == maden).ToList();

        var satirlar = madenli
            .GroupBy(x => x.ReyonId)
            .Select(g =>
            {
                var r = reyonAdlari.FirstOrDefault(x => x.Id == g.Key);
                return new
                {
                    reyonId = g.Key,
                    reyonAdi = r?.Ad ?? g.Key,
                    tip = r?.Tip ?? "",
                    aktif = r?.Aktif ?? false,
                    sorumluKullaniciId = r?.SorumluKullaniciId,
                    subeId = g.First().SubeId,
                    subeAdi = subeAdlari.GetValueOrDefault(g.First().SubeId, g.First().SubeId),
                    adet = Math.Round(g.Sum(x => x.Adet), 3),
                    urunSayisi = g.Sum(x => x.UrunSayisi),
                    madenler = g.GroupBy(x => x.Maden).Select(m => new
                    {
                        maden = m.Key,
                        madenAdi = Kuyumcu.Core.Domain.Maden.Ad(m.Key),
                        adet = Math.Round(m.Sum(x => x.Adet), 3),
                        toplamGram = Math.Round(m.Sum(x => x.HasGram), 6),
                        fizikselGram = Math.Round(m.Sum(x => x.FizikselGram), 4),
                    }).OrderByDescending(m => m.toplamGram).ToList(),
                    altinHasGram = Math.Round(
                        g.Where(x => x.Maden == Kuyumcu.Core.Domain.Maden.Altin).Sum(x => x.HasGram), 6),
                };
            })
            .OrderBy(x => x.subeAdi).ThenBy(x => x.reyonAdi)
            .ToList();

        // ── ATANMAMIŞ: şube stoğu − o şubedeki yerleşim toplamı ──
        var stokQ = db.Stoklar.AsNoTracking().Where(s => s.TenantId == islem.TenantId);
        stokQ = stokQ.Daralt(subeId);   // PAKET ŞUBE-KAPSAM-A: tek daraltma yolu

        var stokGrup = await stokQ
            .Join(db.Urunler.Where(u => u.TenantId == islem.TenantId),
                  s => s.UrunId, u => u.Id, (s, u) => new { s, u })
            .GroupBy(x => new { x.s.SubeId, x.u.AyarId })
            .Select(g => new
            {
                g.Key.SubeId, g.Key.AyarId,
                Adet = g.Sum(x => x.s.Adet),
                HasGram = g.Sum(x => x.s.Adet * x.u.HasGrami),
                FizikselGram = g.Sum(x => x.s.Adet * x.u.GramAgirlik),
            })
            .ToListAsync();

        var yerlesikGrup = await db.StokYerlesimleri.AsNoTracking().Daralt(subeId)   // PAKET ŞUBE-KAPSAM-A
            .Where(y => y.TenantId == islem.TenantId)
            .Join(db.Urunler.Where(u => u.TenantId == islem.TenantId),
                  y => y.UrunId, u => u.Id, (y, u) => new { y, u })
            .GroupBy(x => new { x.y.SubeId, x.u.AyarId })
            .Select(g => new
            {
                g.Key.SubeId, g.Key.AyarId,
                Adet = g.Sum(x => x.y.Adet),
                HasGram = g.Sum(x => x.y.Adet * x.u.HasGrami),
                FizikselGram = g.Sum(x => x.y.Adet * x.u.GramAgirlik),
            })
            .ToListAsync();

        var atanmamisSatirlar = stokGrup.Select(sg =>
        {
            var y = yerlesikGrup.FirstOrDefault(x => x.SubeId == sg.SubeId && x.AyarId == sg.AyarId);
            return new
            {
                subeId = sg.SubeId,
                madenKodu = Kuyumcu.Core.Domain.Ayarlar.MadenBul(sg.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin,
                adet = Math.Round(sg.Adet - (y?.Adet ?? 0m), 3),
                gram = Math.Round(sg.HasGram - (y?.HasGram ?? 0m), 6),
                fiziksel = Math.Round(sg.FizikselGram - (y?.FizikselGram ?? 0m), 4),
            };
        })
        .Where(x => x.adet != 0m || x.gram != 0m)
        .Where(x => string.IsNullOrEmpty(maden) || x.madenKodu == maden)
        .GroupBy(x => x.subeId)
        .Select(g => new
        {
            subeId = g.Key,
            subeAdi = subeAdlari.GetValueOrDefault(g.Key, g.Key),
            adet = Math.Round(g.Sum(x => x.adet), 3),
            madenler = g.GroupBy(x => x.madenKodu).Select(m => new
            {
                maden = m.Key,
                madenAdi = Kuyumcu.Core.Domain.Maden.Ad(m.Key),
                adet = Math.Round(m.Sum(x => x.adet), 3),
                toplamGram = Math.Round(m.Sum(x => x.gram), 6),
                fizikselGram = Math.Round(m.Sum(x => x.fiziksel), 4),
            }).OrderByDescending(m => m.toplamGram).ToList(),
        })
        .OrderBy(x => x.subeAdi)
        .ToList();

        var madenSayisi = satirlar.SelectMany(x => x.madenler).Select(m => m.maden)
            .Union(atanmamisSatirlar.SelectMany(x => x.madenler).Select(m => m.maden))
            .Distinct().Count();

        return new
        {
            reyonlar = satirlar,
            atanmamis = atanmamisSatirlar,
            toplamAltinHasGram = Math.Round(satirlar.Sum(x => x.altinHasGram), 6),
            uyari = madenSayisi > 1
                ? "Raporda birden çok maden var. `toplamAltinHasGram` YALNIZ altın has gramıdır; "
                  + "diğer madenler her satırın `madenler` kırılımında ayrı verilir — "
                  + "farklı madenlerin gramları toplanmaz."
                : null,
        };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
[Route("api/v1/raporlar")]
[Yetki.ModulYetkisi("rapor")]
public sealed class RaporController(RaporServisi servis, KuyumcuDbContext db) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/raporlar/gunluk?tarih=2026-07-02 — Günlük satış/alış/kasa özeti</summary>
    [HttpGet("gunluk")]
    public async Task<object> GunlukOzet([FromQuery] string? tarih, [FromQuery] string? subeId)
        => await servis.GunlukOzet(
            tarih ?? Zaman.YerelBugun().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Islem, await GorunumSubesi.Coz(db, Islem, subeId));

    /// <summary>GET /api/v1/raporlar/aylik?yil=2026&amp;ay=7 — Aylık toplamlar ve kâr özeti</summary>
    [HttpGet("aylik")]
    public async Task<object> AylikOzet([FromQuery] int? yil, [FromQuery] int? ay, [FromQuery] string? subeId)
    {
        // P35 ay sınırı (01.10): varsayılan ay İŞLETME saatine göre (UTC değil) — yoksa
        // ayın 1'i 00:00–03:00 TSİ arası "bu ay" önceki ayı döndürüyordu.
        var bugun = Zaman.YerelBugun();
        var sube = await GorunumSubesi.Coz(db, Islem, subeId);
        return await servis.AylikOzet(yil ?? bugun.Year, ay ?? bugun.Month, Islem, sube);
    }

    /// <summary>GET /api/v1/raporlar/cari-yaslandirma — Açık cari bakiyeleri (0-30, 31-60, 61-90, 90+ gün)</summary>
    [HttpGet("cari-yaslandirma")]
    public Task<object> CariYaslandirma(
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, RaporFiltresi.EnFazlaLimit)] int limit = 500,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)] int ofset = 0)
        => servis.CariYaslandirma(Islem, limit, ofset);


    /// <summary>GET /api/v1/raporlar/vade-takibi?ufukGun=7 — Vadesi geçen ve yaklaşan borçlar</summary>
    [HttpGet("vade-takibi")]
    public Task<object> VadeTakibi([FromQuery] int? ufukGun) => servis.VadeTakibi(Islem, ufukGun ?? 7);

    /// <summary>GET /api/v1/raporlar/pos-fatura-mutabakati?tarih=2026-09-12 — Gün sonu kapanışı (Adım 7)</summary>
    [HttpGet("pos-fatura-mutabakati")]
    public async Task<object> PosFaturaMutabakati([FromQuery] string? tarih, [FromQuery] string? subeId)
        => await servis.PosFaturaMutabakati(
            tarih ?? Zaman.YerelBugun().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Islem, await GorunumSubesi.Coz(db, Islem, subeId));

    /// <summary>GET /api/v1/raporlar/stok-degeri?subeId=... — Stok envanteri ve toplam has gram</summary>
    [HttpGet("stok-degeri")]
    public async Task<object> StokDegeri(
        [FromQuery] string? subeId,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, RaporFiltresi.EnFazlaLimit)] int limit = 500,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)] int ofset = 0)
        => await servis.StokDegeri(Islem, await GorunumSubesi.Coz(db, Islem, subeId), limit, ofset);

    /// <summary>GET /api/v1/raporlar/reyon-stok — Reyon bazlı stok (paket 25)</summary>
    [HttpGet("reyon-stok")]
    public async Task<object> ReyonStok([FromQuery] string? subeId, [FromQuery] string? reyonId,
                                  [FromQuery] string? maden)
        => !Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.ListedeMi(maden, Kuyumcu.Core.Domain.Maden.Hepsi)
            // PAKET GOC-D (K32): bilinmeyen maden süzgeci "kayıt yok" DEĞİL, 400 + alan.
            ? throw new BadRequestException(Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.Mesaj(Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle("maden")
            : await servis.ReyonStok(Islem, await GorunumSubesi.Coz(db, Islem, subeId), reyonId, maden);
}
