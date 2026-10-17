using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Rapor;
using Kuyumcu.Api.Modules.Satis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Maliyet;

// ═════════════════════════════════════════════════════════════════════════════
// KÂR-ZARAR — iki bacaklı
//
// ⚠ KUYUMCUDA TEK BAŞINA TL KÂRI YALAN SÖYLER.
// Örnek (gerçek rakamlarla):
//   Ocak: 10 g has mal alındı, has 6.800 TL → maliyet 68.000 TL = 10,000 g has
//   Mart: aynı mal 75.000 TL'ye satıldı, has 8.000 TL → hasılat = 9,375 g has
//   TL kârı : +7.000 TL   → "kâr ettim"
//   HAS kârı: −0,625 g    → sattığı malın yerine aynı gramı KOYAMAZ
// Kuyumcu dükkânını gramla döndürür. Yalnız TL gösteren bir rapor, ona
// eriyerek büyüdüğünü fark ettirmez. Bu yüzden iki bacak da zorunludur.
//
// ⚠ MALİYETİ BİLİNMEYEN SATIŞ AYRI SAYILIR.
// Bu özellik gelmeden önce girmiş stokların (DEVIR) ve sayım fazlalarının
// maliyeti yoktur. Onları sıfır maliyetli saymak kârı şişirirdi; toplama
// katmayıp AYRI raporlarız — kaç adedin maliyetinin bilinmediği açıkça yazılır.
//
// ═════════════════════════════════════════════════════════════════════════════
// PAKET 35 FAZ A — ÜÇ KANITLI ÖLÇÜM HATASI KAPATILDI (2026-09-17)
//
// Paket ÖNCESİ ölçülen davranış (karakterizasyon, KarZararIadeTestleri):
//   • 4 adet satış (6.000 TL) + 2 adet iade (3.000 TL) + 500 TL işçilik
//       → hasilatTl 6.000 · maliyetTl 4.000 · karTl 2.000   (İADE HİÇ YOK)
//   • Takas farkı 2.000 TL, verilen malın maliyeti 1.000 TL
//       → hasilatTl 2.000 · maliyetTl 1.000                 (FARK = CİRO sayılmış)
//
// 1) İADE HEM HASILATTAN HEM MALİYETTEN DÜŞER. SATIS_IADE satırının TlTutari
//    zaten NEGATİFTİR (SatisModulu.SatisIade) ve maliyet tarafında ters tüketim
//    satırı ÖLÇÜLDÜ: MaliyetMotoru.PartiIade, CikisHareketId = İADE hareketinin
//    kimliği, Adet = −geri (NEGATİF), BirimMaliyetTl = ORİJİNAL partininki
//    (Bertan'ın ZORUNLU ÖLÇÜM koşulu — iddia doğrulandı, kod ona göre yazıldı).
//    Bu yüzden iade kimlikleri de "satış kimlikleri" kümesine alınır; ters satır
//    negatif maliyet olarak toplanır → iade edilen malın maliyeti geri ÇIKAR.
//    İade bugünkü maliyetle yeniden DEĞERLENMEZ; motorun yazdığı birim maliyet
//    aynen okunur.
//
// 2) TAKAS FARKI HASILATA KATILMAZ. Takas satırının TlTutari = farkTl'dir ve
//    NEGATİF olabilir; el değiştiren brüt değer deftere YAZILMAZ. Brüt değeri
//    uydurmak yerine kalem ayrı raporlanır (takasFarkTl) ve ekranda ne olduğu
//    yazılır. Takasın maliyet bacağı da aynı sebeple SMM'ye girmez — hasılatı
//    sayılmayan malın maliyeti sayılırsa kâr ikinci kez bozulurdu.
//
// 3) İŞÇİLİK GELİRİ AYRI KALEMDİR (iscilikTl): malın maliyeti yoktur, hasılata
//    katılırsa marj yüzdesi bozulur. Kâr köprüsünde brüt kârdan SONRA gelir.
//
// AYRICA: storno ÇİFTİ (kaynak + ters satır) StornoSuzgeci ile dışlanır ve
// AYRI blokta sayılır (paket 48-A devri); güvenilirlik artık ÜÇ ucun üçünde de
// döner ve RaporGuvenilirligi ortak ölçeğinden gelir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KarZararServisi(KuyumcuDbContext db)
{
    /// <summary>Karşılaştırma seçenekleri — KAPALI LİSTE (Mehmet K35-1 deseni).</summary>
    public static readonly string[] KarsilastirmaSecenekleri = ["ONCEKI_DONEM", "GECEN_YIL"];

    /// <summary>
    /// Dönem kâr-zararı. Hasılat satış hareketlerinden, maliyet parti
    /// tüketimlerinden gelir — ikisi de deftere yazılmış gerçek kayıtlardır.
    /// </summary>
    /// <param name="karsilastir">
    /// Boş = karşılaştırma YOK (varsayılan kapalı: ikinci sorgu turu demektir).
    /// ONCEKI_DONEM / GECEN_YIL — her ikisi de AYNI GÜN SAYISI kadar alınır.
    /// </param>
    public async Task<object> Donem(
        AktifKullanici islem, DateOnly baslangic, DateOnly bitis, string? subeId,
        string? karsilastir = null)
    {
        // PAKET GOC-D (envanter E · rapor tarih aralıkları): pencere + bas ≤ bit + en çok 400 gün —
        // Core RaporDonemKurali (ekranın alan altı metniyle AYNI sözlük anahtarı). Eskiden yalnız ters
        // aralık reddediliyordu; 3000 yılı ya da 10 yıllık dönem sunucuyu boşuna yoruyordu.
        DonemDogrula(baslangic, bitis);

        var olcum = await Olc(islem, baslangic, bitis, subeId);
        var maliyetBilinmiyor = olcum.KarDurumu == KarDurumlari.Hesaplanamadi;

        object? onceki = null;
        object? degisim = null;
        if (!string.IsNullOrWhiteSpace(karsilastir))
        {
            var kod = karsilastir.Trim().ToUpperInvariant();
            if (!KarsilastirmaSecenekleri.Contains(kod))
                throw new BadRequestException(
                    "karsilastir yalnız ONCEKI_DONEM veya GECEN_YIL olabilir");

            // ⚠ EŞİT UZUNLUK: 15 günlük dönem 30 günlük ayla kıyaslanmaz.
            var gun = bitis.DayNumber - baslangic.DayNumber + 1;
            var (oBas, oBit) = kod == "ONCEKI_DONEM"
                ? (baslangic.AddDays(-gun), baslangic.AddDays(-1))
                : (baslangic.AddYears(-1), baslangic.AddYears(-1).AddDays(gun - 1));

            var o = await Olc(islem, oBas, oBit, subeId);
            onceki = new
            {
                baslangic = oBas.ToString("yyyy-MM-dd"),
                bitis     = oBit.ToString("yyyy-MM-dd"),
                gunSayisi = oBit.DayNumber - oBas.DayNumber + 1,
                hasilatTl = Math.Round(o.HasilatTl, 2, MidpointRounding.AwayFromZero),
                // PAKET RAPOR-GORUNUM (veri d): grafik çubuğu "maliyeti bilinen satış − maliyet = kâr"
                // ilişkisini önceki dönemde de kurabilsin diye bilinen hasılat AYRI döner (seçili dönemle aynı alan).
                bilinenHasilatTl = Math.Round(o.BilinenHasilatTl, 2, MidpointRounding.AwayFromZero),
                maliyetTl = o.KarDurumu == KarDurumlari.Hesaplanamadi ? (decimal?)null : o.MaliyetTl,
                karTl     = R(o.KarTl, 2),
                donemKariTl = R(o.DonemKariTl, 2),
                karHas    = R(o.KarHas, 4),
                karDurumu = o.KarDurumu,
                iscilikTl = Math.Round(o.IscilikTl, 2, MidpointRounding.AwayFromZero),
                iadeTl    = Math.Round(o.IadeTl, 2, MidpointRounding.AwayFromZero),
            };
            degisim = new
            {
                hasilatTlYuzde   = Yuzde(o.HasilatTl, olcum.HasilatTl),
                karTlYuzde       = Yuzde(o.KarTl, olcum.KarTl),
                donemKariTlYuzde = Yuzde(o.DonemKariTl, olcum.DonemKariTl),
                karHasYuzde      = Yuzde(o.KarHas, olcum.KarHas),
            };
        }

        return new
        {
            baslangic = baslangic.ToString("yyyy-MM-dd"),
            bitis     = bitis.ToString("yyyy-MM-dd"),
            gunSayisi = bitis.DayNumber - baslangic.DayNumber + 1,
            subeId,

            satisAdedi = olcum.SatisAdedi,
            iadeAdedi  = olcum.IadeAdedi,
            // Adet de storno çiftinden ARINDIRILMIŞ sayılır: tutar toplamı
            // sıfırlansa bile "kaç fiş kesildi" sorusunun cevabı değişmemeli.
            iscilikAdedi = olcum.IscilikAdedi,

            // ── TL bacağı — muhasebenin gördüğü
            // ⚠ hasilatTl artık NET'tir (brüt satış − iade). Alan ADI ve TÜRÜ
            //   korunur (eski istemciler kırılmaz), tanımı DÜZELİR.
            brutHasilatTl = Math.Round(olcum.BrutHasilatTl, 2, MidpointRounding.AwayFromZero),
            iadeTl        = Math.Round(olcum.IadeTl, 2, MidpointRounding.AwayFromZero),
            hasilatTl     = Math.Round(olcum.HasilatTl, 2, MidpointRounding.AwayFromZero),
            // KAR-RAPOR-2: hiçbir satışın maliyeti bilinmiyorsa (HESAPLANAMADI) maliyet
            // "0" DEĞİL null döner — bilinmeyen sıfır diye basılmaz (fail-closed ekrana da uzanır).
            // KISMI'de maliyet YALNIZ bilinen satışlarındır ve bilinenHasilatTl ile eşleşir:
            // bilinenHasilatTl − maliyetTl = karTl (kuruşu kuruşuna, yuvarlanmış bileşenler).
            brutMaliyetTl = maliyetBilinmiyor ? (decimal?)null : olcum.BrutMaliyetTl,
            iadeMaliyetTl = maliyetBilinmiyor ? (decimal?)null : olcum.IadeMaliyetTl,
            maliyetTl     = maliyetBilinmiyor ? (decimal?)null : olcum.MaliyetTl,
            // PAKET KAR-RAPOR: kâr YALNIZ maliyeti bilinen satışlardan; hiçbiri
            // bilinmiyorsa null (karDurumu = HESAPLANAMADI) — satış kâr diye GÖSTERİLMEZ.
            karTl         = R(olcum.KarTl, 2),
            karDurumu     = olcum.KarDurumu,
            bilinenHasilatTl = Math.Round(olcum.BilinenHasilatTl, 2, MidpointRounding.AwayFromZero),
            maliyetiEksikSatisAdedi = olcum.MaliyetiEksikSatisAdedi,
            maliyetiEksikIadeAdedi  = olcum.MaliyetiEksikIadeAdedi,
            maliyetiEksikTutarTl    = Math.Round(olcum.MaliyetiEksikTutarTl, 2, MidpointRounding.AwayFromZero),
            karMarjiYuzde = olcum.KarTl is { } kt && olcum.BilinenHasilatTl > 0m
                ? Math.Round(kt / olcum.BilinenHasilatTl * 100m, 2, MidpointRounding.AwayFromZero) : (decimal?)null,

            // ── Hasılata KATILMAYAN kalemler — ayrı, açık, gerekçeli
            iscilikTl   = Math.Round(olcum.IscilikTl, 2, MidpointRounding.AwayFromZero),
            takasFarkTl = Math.Round(olcum.TakasFarkTl, 2, MidpointRounding.AwayFromZero),
            takasAdedi  = olcum.TakasAdedi,
            takasNotu   = olcum.TakasAdedi > 0
                ? "Takas satırının tutarı el değiştiren malın brüt değeri DEĞİL, yalnız "
                + "FARK TL'sidir (negatif de olabilir). Brüt değer deftere yazılmadığı için "
                + "takas cirosu ölçülemez; bu yüzden takas farkı hasılata KATILMAZ, ayrı gösterilir."
                : null,

            // ── PAKET P-19 (Y-12) — GİDER: dönem kârından DÜŞÜLÜR, ayrı satırdır
            giderTl      = Math.Round(olcum.GiderTl, 2, MidpointRounding.AwayFromZero),
            giderKirilimi = olcum.GiderKirilimi,
            giderNotu    = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Gider, nakit kasasından çıkan ve bir gider kategorisine yazılan "
                         + "tutarların toplamıdır (kira, elektrik, personel…). Malın maliyeti "
                         + "DEĞİLDİR: brüt kârdan SONRA düşülür. Stornolanan gider toplamdan çıkar."),

            donemKariTl = R(olcum.DonemKariTl, 2),

            // ── HAS bacağı — kuyumcunun gördüğü
            hasilatHas = Math.Round(olcum.HasilatHas, 4, MidpointRounding.AwayFromZero),
            iadeHas    = Math.Round(olcum.IadeHas, 4, MidpointRounding.AwayFromZero),
            maliyetHas = olcum.KarHas is null ? (decimal?)null : olcum.MaliyetHas,
            // MADEN-AYRIMI: has bacağının (hasilatHas, maliyetHas, karHas, bilinenHasilatHas) madeni — yalnız altın.
            hasBacagiMadeni = Kuyumcu.Core.Domain.Maden.Altin,
            karHas     = R(olcum.KarHas, 4),
            // KAR-RAPOR-2 tur 2: BASİT 2 haneli has kartı bu alanı basar (görünen bileşenlerin farkı).
            karHasIkiHane = olcum.KarHasIkiHane,
            // M9: altın kârı = makas (işçilik) kârı + altın fiyat farkı kârı (toplamı kuruşu kuruşuna).
            makasKariTl = olcum.MakasKariTl,
            altinFiyatKariTl = olcum.AltinFiyatKariTl,
            // KAR-RAPOR-2: has bacağı da AYNI kümeden — bilinenHasilatHas − maliyetHas = karHas.
            bilinenHasilatHas = olcum.BilinenHasilatHas,
            maliyetiEksikHasilatHas = olcum.HasilatHas - olcum.BilinenHasilatHas,
            // MADEN-2: has bacağının kendi kâr durumu (TAM/KISMI/HESAPLANAMADI/VERI_YOK) ve eksik sayımı.
            //   yalnizHasMaliyetiEksikAdedi = TL maliyeti bilinen ama has maliyeti bilinmeyen altın hareketi.
            hasKarDurumu = olcum.HasKarDurumu,
            hasMaliyetiEksikSatisAdedi = olcum.HasMaliyetiEksikSatisAdedi,
            hasMaliyetiEksikIadeAdedi  = olcum.HasMaliyetiEksikIadeAdedi,
            yalnizHasMaliyetiEksikAdedi = olcum.YalnizHasMaliyetiEksikAdedi,

            // QA turu 3 bulgusu: /raporlar aylık özeti ile bu ekranın has rakamı
            // birbirini tutmuyordu. İKİ FARKLI BÜYÜKLÜK ölçülüyor ve artık İKİSİ
            // DE burada, adıyla dönüyor:
            //   hasilatHas           = TL hasılatın satış GÜNÜNÜN kuruyla has karşılığı
            //                          (kaç gram has ALABİLİRİM — kâr bunun üzerinden)
            //   hasilatHasDefterGram = fiilen el değiştiren malın has grami
            //                          (/raporlar aylık özetindeki satis.toplamHas)
            // ⚠ P-45: bu alan ARTIK YALNIZ ALTIN taşır (eskiden altın+gümüş
            //   toplamıydı). Ad geriye dönük sözleşme gereği KORUNUR; ne
            //   ölçtüğü karışmasın diye aynı değer `altinHasilatHasDefterGram`
            //   adıyla da yayınlanır ve maden kırılımı ayrıca döner.
            hasilatHasDefterGram      = Math.Round(olcum.HasilatHasDefterGram, 6, MidpointRounding.AwayFromZero),
            altinHasilatHasDefterGram = Math.Round(olcum.HasilatHasDefterGram, 6, MidpointRounding.AwayFromZero),
            hasDefterMadenDagilimi    = olcum.HasDefterMadenDagilimi,
            madenUyarisi = HareketToplami.MadenUyarisi(
                olcum.HasDefterMadenDagilimi, "altinHasilatHasDefterGram"),
            hasTanimNotu =
                "HAS hasılatı iki farklı büyüklüktür: hasilatHas, TL hasılatın satış günü "
              + "kuruyla HAS KARŞILIĞIDIR (kâr bu bacaktan ölçülür); hasilatHasDefterGram ise "
              + "fiilen el değiştiren malın DEFTERDEKİ has gramıdır (aylık özetteki değer). "
              + "İkisi eşit değildir ve olmak zorunda değildir. Defter gramı YALNIZ ALTIN "
              + "taşır (P-45): gümüş/platin gramları hasDefterMadenDagilimi altında ayrı "
              + "durur, farklı madenlerin gramları toplanmaz.",

            // ── Kâr köprüsü — hasılattan dönem kârına giden yol tek tabloda
            karKoprusu = olcum.KarKoprusu,

            // ── Storno (paket 48-A devri): çift DIŞLANDI, ayrıca sayılır
            storno = new
            {
                adet = olcum.StornoAdet,
                tutarTl = Math.Round(olcum.StornoTutarTl, 2, MidpointRounding.AwayFromZero),
                notu = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Storno çifti (hükümsüz kılınan kayıt + ters kaydı) ciro, adet ve "
                     + "kâr toplamlarına KATILMAZ; burada ayrıca sayılır."),
            },

            // ── Karşılaştırma (isteğe bağlı, varsayılan KAPALI)
            karsilastir = string.IsNullOrWhiteSpace(karsilastir)
                ? null : karsilastir.Trim().ToUpperInvariant(),
            onceki,
            degisimYuzde = degisim,

            // ── Dürüstlük alanları: neyin hesaplanamadığı GİZLENMEZ
            maliyetiBilinenAdet    = Math.Round(olcum.MaliyetliAdet, 3, MidpointRounding.AwayFromZero),
            maliyetiBilinmeyenAdet = Math.Round(olcum.MaliyetsizAdet, 3, MidpointRounding.AwayFromZero),
            kuruBulunamayanSatis   = olcum.KursuzSatis,
            // RAPOR-4: maliyeti parti yerine ÜRÜN KARTINDAKİ alış maliyetinden gelen adet (kaynak etiketi "kart maliyeti").
            kartMaliyetliAdet      = Math.Round(olcum.KartMaliyetliAdet, 3, MidpointRounding.AwayFromZero),
            guvenilirlik = RaporGuvenilirligi.Hesapla(
                olcum.MaliyetliAdet, olcum.MaliyetsizAdet, olcum.KursuzSatis),

            // PAKET KAR-RAPOR: eski metin "kâr olduğundan yüksek görünüyor olabilir"
            // diyordu — artık maliyeti eksik satış kâra HİÇ girmez, metin bunu söyler.
            uyari = olcum.MaliyetiEksikSatisAdedi + olcum.MaliyetiEksikIadeAdedi > 0
                ? $"{olcum.MaliyetiEksikSatisAdedi} satışın alış maliyeti bilinmiyor (açılış stoğu, sayım " +
                  "fazlası ya da maliyeti girilmemiş mal). Bu satışlar kâra KATILMADI; tutarları ayrı " +
                  "gösterilir."
                : olcum.MaliyetsizAdet > 0m
                ? $"{olcum.MaliyetsizAdet:0.###} adet ürünün alış maliyeti bilinmiyor; bu adetler kâra KATILMADI."
                : null,
        };
    }

    // ── Dönem ölçümü (karşılaştırma aynı hesabı ikinci kez koşar) ────────────

    private sealed record DonemOlcumu(
        int SatisAdedi, int IadeAdedi, int TakasAdedi, int IscilikAdedi,
        decimal BrutHasilatTl, decimal IadeTl, decimal HasilatTl,
        decimal BrutMaliyetTl, decimal IadeMaliyetTl, decimal MaliyetTl,
        decimal? KarTl, decimal IscilikTl, decimal TakasFarkTl, decimal? DonemKariTl,
        decimal HasilatHas, decimal IadeHas, decimal MaliyetHas, decimal? KarHas,
        decimal HasilatHasDefterGram,
        IReadOnlyList<object> HasDefterMadenDagilimi,
        decimal MaliyetliAdet, decimal MaliyetsizAdet, int KursuzSatis,
        int StornoAdet, decimal StornoTutarTl,
        decimal GiderTl, IReadOnlyList<object> GiderKirilimi,
        IReadOnlyList<object> KarKoprusu,
        string KarDurumu, decimal BilinenHasilatTl,
        int MaliyetiEksikSatisAdedi, int MaliyetiEksikIadeAdedi, decimal MaliyetiEksikTutarTl,
        decimal BilinenHasilatHas,
        // KAR-RAPOR-2 tur 2: BASİT gösterim (2 hane) için kâr = R2(bilinen has hasılat) − R2(maliyet has).
        //   Sunucu hesaplar; istemci kârı YENİDEN HESAPLAMAZ (tek kural, tek kaynak).
        decimal? KarHasIkiHane,
        // MADEN-2: has bacağının KENDİ kâr durumu ve eksik sayımı (TL kümesinden bağımsız, yalnız altın).
        string HasKarDurumu = KarDurumlari.VeriYok, int HasMaliyetiEksikSatisAdedi = 0,
        int HasMaliyetiEksikIadeAdedi = 0, int YalnizHasMaliyetiEksikAdedi = 0,
        // RAPOR-4: maliyeti ürün kartından alınan (parti maliyeti olmayan) adet — kaynak etiketi için.
        decimal KartMaliyetliAdet = 0m,
        // M9: altın kârının iki parçası (KarAyrimi) — hesaplanamazsa null.
        decimal? MakasKariTl = null, decimal? AltinFiyatKariTl = null);

    private async Task<DonemOlcumu> Olc(
        AktifKullanici islem, DateOnly baslangic, DateOnly bitis, string? subeId)
    {
        var (bas, _) = Zaman.YerelGunAraligiUtc(baslangic);
        var (_, sonBit) = Zaman.YerelGunAraligiUtc(bitis);

        var temel = db.Hareketler.AsNoTracking()
            .Where(h => h.TenantId == islem.TenantId
                     && h.Tarih >= bas && h.Tarih < sonBit
                     && (h.Tur == "SATIS" || h.Tur == "SATIS_IADE"
                      || h.Tur == "TAKAS" || h.Tur == "ISCILIK"));
        if (!string.IsNullOrEmpty(subeId)) temel = temel.Where(h => h.SubeId == subeId);

        // Storno çifti toplamlardan DIŞLANIR (paket 48-A süzgeci; ikinci süzgeç yazılmaz).
        var net = temel.StornoCiftiniDisla(db, islem.TenantId);

        var satirlar = await net
            .Select(h => new { h.Id, h.Tur, h.TlTutari, h.HasGrami, h.KurSnapshotId, h.Maden, h.Adet })
            .ToListAsync();

        var stornoSatirlari = temel.Where(h => h.StornoKaynakId != null);
        var stornoAdet = await stornoSatirlari.CountAsync();
        var stornoTutar = await stornoSatirlari.SumAsync(h => (decimal?)Math.Abs(h.TlTutari)) ?? 0m;

        var satislar = satirlar.Where(h => h.Tur == "SATIS").ToList();
        var iadeler  = satirlar.Where(h => h.Tur == "SATIS_IADE").ToList();
        var takaslar = satirlar.Where(h => h.Tur == "TAKAS").ToList();
        var iscilik  = satirlar.Where(h => h.Tur == "ISCILIK").ToList();

        var brutHasilatTl = satislar.Sum(h => h.TlTutari);
        var iadeTl        = iadeler.Sum(h => Math.Abs(h.TlTutari));
        var hasilatTl     = brutHasilatTl - iadeTl;
        var takasFarkTl   = takaslar.Sum(h => h.TlTutari);
        var iscilikTl     = iscilik.Sum(h => h.TlTutari);

        // Hasılatın has karşılığı: her satışın KENDİ günündeki kurla çevrilir.
        // Dönem sonu kuruyla çevirmek, dönem içi kur hareketini kâra karıştırırdı.
        var kurlar = await KurHaritasi(
            islem.TenantId, satislar.Concat(iadeler).Select(h => h.KurSnapshotId));
        var kursuzSatis = 0;
        // ⚠ PAKET MADEN-AYRIMI (tasarım hakemi t1 KRİTİK): HAS BACAĞI ("Altın kârı (has)")
        //   YALNIZ ALTIN satışlarından ölçülür. Eskiden gümüş satışının TL'si de altın
        //   kuruyla "has"a çevrilip altın kârına ekleniyordu (gümüş kolye 0,1359 has).
        //   'has' yalnız altın içindir (ekip kararı 5); gümüşün kârı TL bacağındadır.
        //   kursuzSatis sayımı (güvenilirlik) TÜM satışlar için korunur.
        var kursuzTum = 0;
        HasCevir(satislar.Concat(iadeler).Select(h => (Math.Abs(h.TlTutari), h.KurSnapshotId)), kurlar, ref kursuzTum);
        var brutHasilatHas = HasCevir(satislar.Where(h => AltinMaden(h.Maden)).Select(h => (h.TlTutari, h.KurSnapshotId)), kurlar, ref kursuzSatis);
        var iadeHas = HasCevir(iadeler.Where(h => AltinMaden(h.Maden)).Select(h => (Math.Abs(h.TlTutari), h.KurSnapshotId)), kurlar, ref kursuzSatis);
        kursuzSatis = kursuzTum;
        var hasilatHas = brutHasilatHas - iadeHas;

        // ── Defterdeki fiilî has gramı (aylık özetin saydığı büyüklük) ──────
        // ⚠ PAKET P-45: bu rakam MADEN AYRIMI YAPMADAN toplanıyordu. 22 ayar
        //   altının has gramı ile 925 gümüşün has gramı aynı torbaya giriyor,
        //   ekranda "44,4154 g" gibi HİÇBİR ŞEYİN KARŞILIĞI OLMAYAN bir sayı
        //   çıkıyordu (ölçüm: 32,7604 g ALTIN + 11,6550 g GÜMÜŞ). Artık defter
        //   gramı MADEN BAZINDA kırılır; `HasilatHasDefterGram` YALNIZ ALTIN
        //   taşır (K20-2 ile aynı kural, aylık özetle birebir aynı büyüklük).
        //
        // Kırılım, aylık özetin kullandığı yardımcılarla ÜRETİLİR (ikinci bir
        // gruplama yazılmaz): satış satırı |has| ile, iade satırı −|has| ile
        // kırılıma konur; net gram böylece maden bazında düşülür.
        var defterKirilimi = satislar
            .Select(h => new HareketKirilimi(h.Tur, h.Maden, 1, 0m, 0m, Math.Abs(h.HasGrami), Math.Abs(h.HasGrami)))
            .Concat(iadeler.Select(h => new HareketKirilimi(
                h.Tur, h.Maden, 1, 0m, 0m, -Math.Abs(h.HasGrami), Math.Abs(h.HasGrami))))
            .ToList();
        var defterMadenDagilimi = HareketToplami.MadenDagilimi(defterKirilimi);
        var defterGram = HareketToplami.AltinHas(defterKirilimi);

        // ── Maliyet: parti tüketimleri ──────────────────────────────────────
        // ⚠ İADE kimlikleri de kümededir: ters tüketim satırı (Adet<0) İADE
        //   hareketine bağlıdır (MaliyetMotoru.PartiIade — ölçüldü).
        //   TAKAS kimliği kümede DEĞİLDİR: hasılatı sayılmayan malın maliyeti de
        //   sayılmaz.
        var maliyetKimlikleri = satislar.Select(h => h.Id).Concat(iadeler.Select(h => h.Id)).ToHashSet();

        var tuketimSorgu = db.Set<StokPartiTuketimi>().AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId
                     && t.Tarih >= bas && t.Tarih < sonBit
                     && t.CikisHareketId != null);
        if (!string.IsNullOrEmpty(subeId)) tuketimSorgu = tuketimSorgu.Where(t => t.SubeId == subeId);

        var hamTuketimler = (await tuketimSorgu
            .Select(t => new { t.CikisHareketId, t.UrunId, t.Adet, t.BirimMaliyetTl, t.BirimMaliyetHas })
            .ToListAsync())
            .Where(t => maliyetKimlikleri.Contains(t.CikisHareketId!))
            .ToList();
        // PAKET RAPOR-4 (VERİ 1 — ekip oyu Bertan + Mustafa): parti satırında maliyet yoksa ÜRÜN KARTINDAKİ
        // alış maliyeti (> 0) kullanılır ve "kart maliyeti" diye AYRI sayılır. Has maliyeti kartla
        // BİLİNMEZ (alış günü has fiyatı yok) → has bacağı o satırda fail-closed kalır.
        var kartMaliyetleri = await KartMaliyetleri(islem.TenantId,
            hamTuketimler.Where(t => t.BirimMaliyetTl is null).Select(t => t.UrunId));
        // M1b — kart TL maliyeti de yoksa alış milyeminden (has) maliyet; TL'si satışın KENDİ kuruyla.
        var milyemHas = await MilyemHasMaliyetleri(islem.TenantId,
            hamTuketimler.Where(t => t.BirimMaliyetTl is null && KartMaliyeti(kartMaliyetleri, t.UrunId) is null).Select(t => t.UrunId));
        var hareketKuru = satislar.Concat(iadeler)
            .Where(h => !string.IsNullOrEmpty(h.KurSnapshotId) && kurlar.ContainsKey(h.KurSnapshotId!))
            .ToDictionary(h => h.Id, h => kurlar[h.KurSnapshotId!]);
        var tuketimler = hamTuketimler
            .Select(t =>
            {
                var (tl, has, kart) = BirimMaliyetSec(t.BirimMaliyetTl, t.BirimMaliyetHas, t.UrunId, t.CikisHareketId,
                    kartMaliyetleri, milyemHas, hareketKuru);
                return new { t.CikisHareketId, t.Adet, BirimMaliyetTl = tl, BirimMaliyetHas = has, Kart = kart };
            })
            .ToList();

        // ═══ PAKET KAR-RAPOR (02.10) — FAIL-CLOSED KÂR ════════════════════════
        // KÖK NEDEN (ölçüldü): eski hesap `karTl = hasilatTl − maliyetTl` idi;
        // hasilatTl TÜM satışları taşırken maliyetTl yalnız maliyeti BİLİNEN parti
        // satırlarını topluyordu. Maliyeti bilinmeyen satış (açılış/sayım fazlası
        // partisinden ya da partisi hiç olmayan stoktan çıkan mal) böylece kâra
        // SIFIR MALİYETLE giriyordu: demo dükkânında satışların tamamı açılış
        // stoğundan çıktığı için "KÂR" aylık satış toplamına BİREBİR eşitti.
        // KURAL: bir satış (ya da iade) ancak TÜM adedi maliyeti bilinen parti
        // satırlarıyla karşılanmışsa kâra girer. Eksik olanlar AYRI sayılır
        // (adet + tutar); hiçbiri bilinmiyorsa kâr HESAPLANMAZ (null — 0 değil).
        var bilinenKimlikler = MaliyetiBilinenler(
            satislar.Select(h => (h.Id, h.Adet)).Concat(iadeler.Select(h => (h.Id, h.Adet))),
            tuketimler.Select(t => (t.CikisHareketId, t.Adet, t.BirimMaliyetTl)));
        var bilinenSatislar = satislar.Where(h => bilinenKimlikler.Contains(h.Id)).ToList();
        var bilinenIadeler  = iadeler.Where(h => bilinenKimlikler.Contains(h.Id)).ToList();
        var eksikSatislar   = satislar.Where(h => !bilinenKimlikler.Contains(h.Id)).ToList();
        var eksikIadeler    = iadeler.Where(h => !bilinenKimlikler.Contains(h.Id)).ToList();
        var eksikTutarTl    = eksikSatislar.Sum(h => h.TlTutari) - eksikIadeler.Sum(h => Math.Abs(h.TlTutari));
        var bilinenHasilatTl = hasilatTl - eksikTutarTl;

        // Parti defteri stoktan geride kaldıysa (satırı hiç yazılmamış adet)
        // o adet de "maliyeti bilinmeyen"dir — eskiden hiç sayılmıyordu.
        var satirsizAdet = SatirsizAdet(
            satislar.Select(h => (h.Id, h.Adet)).Concat(iadeler.Select(h => (h.Id, h.Adet))),
            tuketimler.Select(t => (t.CikisHareketId, t.Adet)));

        // Maliyet YALNIZ kâra giren (bilinen) hareketlerin satırlarından toplanır.
        var bilinenTuketim = tuketimler.Where(t => bilinenKimlikler.Contains(t.CikisHareketId!)).ToList();

        // İade maliyeti ÇIKTIĞI maliyetle geri alınır: satırın BirimMaliyetTl'si
        // orijinal partininkidir ve BURADA YENİDEN DEĞERLENMEZ.
        var cikan = bilinenTuketim.Where(t => t.Adet > 0m).ToList();
        var geri  = bilinenTuketim.Where(t => t.Adet < 0m).ToList();

        var brutMaliyetTl = cikan.Where(t => t.BirimMaliyetTl is not null).Sum(t => t.BirimMaliyetTl!.Value * t.Adet);
        var iadeMaliyetTl = -geri.Where(t => t.BirimMaliyetTl is not null).Sum(t => t.BirimMaliyetTl!.Value * t.Adet);
        var maliyetTl = brutMaliyetTl - iadeMaliyetTl;

        // ═══ PAKET MADEN-2 (hasım KRİTİK MAS20) — HAS BACAĞI KENDİ "BİLİNEN" KÜMESİYLE ═══
        // KÖK NEDEN (ölçüldü): has bacağı TL'nin bilinen kümesini kullanıyordu. TL maliyeti bilinen
        // ama alış günü has fiyatı olmayan parti (BirimMaliyetHas null — SatisModulu alışında kurun
        // AltinHasFiyati yoksa, AcilisYazici'da kur.AltinHasFiyati null) has maliyetine 0 katkı
        // veriyor, satışın has hasılatı ise bilinenHasilatHas'a TAM giriyordu → karHas = tüm has
        // hasılatı (sıfır maliyetle kâr). KURAL (TL bacağıyla AYNI yardımcı, birim = has): bir altın
        // satışı ancak TÜM parti satırlarında has maliyeti (ve TL maliyeti) biliniyorsa has kârına girer;
        // eksik olanın has hasılatı ayrı alandadır (maliyetiEksikHasilatHas); hiçbiri bilinmiyorsa null.
        var altinHareketler = satislar.Concat(iadeler).Where(h => AltinMaden(h.Maden)).ToList();
        var hasBilinenKimlikler = HasMaliyetiBilinenler(
            altinHareketler.Select(h => (h.Id, h.Adet)),
            tuketimler.Select(t => (t.CikisHareketId, t.Adet, t.BirimMaliyetTl, t.BirimMaliyetHas)));
        var hasBilinenSatislar = satislar.Where(h => hasBilinenKimlikler.Contains(h.Id)).ToList();
        var hasBilinenIadeler  = iadeler.Where(h => hasBilinenKimlikler.Contains(h.Id)).ToList();
        var hasEksikSatisAdedi = satislar.Count(h => AltinMaden(h.Maden) && !hasBilinenKimlikler.Contains(h.Id));
        var hasEksikIadeAdedi  = iadeler.Count(h => AltinMaden(h.Maden) && !hasBilinenKimlikler.Contains(h.Id));
        // TL'si bilinen ama has'ı bilinmeyen altın hareketi — ekran yalnız bu farkı ayrıca söyler
        // (TL eksiği zaten "maliyeti eksik" kutusunda; aynı satış iki kez anlatılmaz).
        var yalnizHasEksikAdedi = altinHareketler.Count(h => bilinenKimlikler.Contains(h.Id) && !hasBilinenKimlikler.Contains(h.Id));

        // Has maliyeti YALNIZ has-bilinen (altın) hareketlerin tüketim satırlarından — hasılatla AYNI küme.
        var hasTuketim = tuketimler.Where(t => hasBilinenKimlikler.Contains(t.CikisHareketId!)).ToList();
        var brutMaliyetHas = hasTuketim.Where(t => t.Adet > 0m).Sum(t => t.BirimMaliyetHas!.Value * t.Adet);
        var iadeMaliyetHas = -hasTuketim.Where(t => t.Adet < 0m).Sum(t => t.BirimMaliyetHas!.Value * t.Adet);
        var maliyetHas = brutMaliyetHas - iadeMaliyetHas;

        // Güvenilirlik MUTLAK hacim üzerinden ölçülür: tamamı iade edilmiş bir
        // dönemde net adet 0'dır ama veri VARDIR (VERI_YOK demek yanlış olurdu).
        var maliyetliAdet  = tuketimler.Where(t => t.BirimMaliyetTl is not null).Sum(t => Math.Abs(t.Adet));
        var kartMaliyetliAdet = tuketimler.Where(t => t.Kart).Sum(t => Math.Abs(t.Adet));
        var maliyetsizAdet = tuketimler.Where(t => t.BirimMaliyetTl is null).Sum(t => Math.Abs(t.Adet))
                           + satirsizAdet;

        // ── PAKET P-19 (Y-12): GİDER — kâr-zararda AYRI SATIR ───────────────
        // ⚠ NEDEN BURADA DEĞİL, GİDER MODÜLÜNDE: gider `hareketler` defterinde
        //   DEĞİL, kasa_hareketleri tablosundadır ve ölçümü giderin kendi
        //   modülünün işidir (tek kaynak — ekranla kâr-zarar aynı rakamı okur).
        //   Kâr-zarar onu ÇAĞIRIR; gider düşülmezse ekran "kâr ettim" der ama
        //   kira ödenmemiş gibi sayar.
        var gider = await Modules.Gider.GiderOlcumu.Donem(db, islem.TenantId, bas, sonBit, subeId);
        var giderTl = gider.ToplamTl;
        var giderKirilimi = gider.Kirilim;

        // HAS bacağı da AYNI kümeyle: bilinen satışların has karşılığı − maliyeti.
        var kursuzYoksay = 0;
        var bilinenHasilatHas =
            HasCevir(hasBilinenSatislar.Select(h => (h.TlTutari, h.KurSnapshotId)), kurlar, ref kursuzYoksay)
          - HasCevir(hasBilinenIadeler.Select(h => (Math.Abs(h.TlTutari), h.KurSnapshotId)), kurlar, ref kursuzYoksay);
        // MADEN-AYRIMI + MADEN-2: altın kârının fail-closed durumu HAS-BİLİNEN altın kümesinden —
        // hiçbir altın satışının has maliyeti bilinmiyorsa has kârı null (gümüşün ya da yalnız TL'nin
        // bilinen maliyeti onu "hesaplanmış" yapmaz).
        var altinKarDurumu = KarDurumuBelirle(
            hasBilinenSatislar.Count + hasBilinenIadeler.Count,
            hasEksikSatisAdedi + hasEksikIadeAdedi);

        var eksikAdet = eksikSatislar.Count + eksikIadeler.Count;
        var bilinenAdet = bilinenSatislar.Count + bilinenIadeler.Count;
        var karDurumu = KarDurumuBelirle(bilinenAdet, eksikAdet);

        // ═══ PAKET KAR-RAPOR-2 — KURUŞ KURALI (ekip oyu: Bertan + Mustafa, 02.10) ═══
        // Ekranda yan yana duran kartlar TOPLANABİLİR olmalı: kullanıcı "hasılat −
        // maliyet" çıkarmasını kendisi yapar. Eski hesap kârı KESİN değerden
        // yuvarlıyordu (1.501 − 1.000,875 = 500,125 → 500,13) ama maliyet kartı
        // ayrıca 1.000,88'e yuvarlanıyordu → ekranda 1.501,00 − 1.000,88 = 500,12
        // ≠ 500,13. KURAL: her TEMEL bileşen BİR KEZ yuvarlanır (TL 2, has 4 hane,
        // AwayFromZero); türetilen her tutar (net hasılat, bilinen hasılat, maliyet,
        // kâr, dönem kârı, köprü) YUVARLANMIŞ bileşenlerin farkıdır. Böylece kâr =
        // görünen hasılat − görünen maliyet, köprü kalemleri dönem kârına kuruşu
        // kuruşuna eşittir ve TÜM ekranlar (Kâr/Zarar, Rapor merkezi, Panel, aylık
        // özet) bu tek rakamı okur. Mevzuat hükmü DEĞİL, proje gösterim kuralıdır.
        var brutHasilatR   = R2(brutHasilatTl);
        var iadeR          = R2(iadeTl);
        var hasilatR       = brutHasilatR - iadeR;
        var eksikTutarR    = R2(eksikTutarTl);
        var bilinenHasilatR = hasilatR - eksikTutarR;
        var brutMaliyetR   = R2(brutMaliyetTl);
        var iadeMaliyetR   = R2(iadeMaliyetTl);
        var maliyetR       = brutMaliyetR - iadeMaliyetR;
        var iscilikR       = R2(iscilikTl);
        var giderR         = R2(giderTl);
        var hasilatHasR    = R4(hasilatHas);
        var bilinenHasilatHasR = R4(bilinenHasilatHas);
        var maliyetHasR    = R4(maliyetHas);

        var hesaplanamadi = karDurumu == KarDurumlari.Hesaplanamadi;
        decimal? karTl = hesaplanamadi ? null : bilinenHasilatR - maliyetR;
        var altinHesaplanamadi = hesaplanamadi || altinKarDurumu == KarDurumlari.Hesaplanamadi;
        decimal? karHas = altinHesaplanamadi ? null : bilinenHasilatHasR - maliyetHasR;
        decimal? karHasIkiHane = altinHesaplanamadi ? null : R2(bilinenHasilatHasR) - R2(maliyetHasR);
        decimal? donemKariTl = karTl is null ? null : karTl + iscilikR - giderR;
        // M9 (müşteri 16.10: "3.600 kâr etmiş görürsem üstüne vergi alır"): altın kârı iki parçaya ayrılır.
        var (makasKari, altinFiyatKari) = altinHesaplanamadi ? ((decimal?)null, (decimal?)null) : KarAyrimi(
            hasBilinenSatislar.Select(h => h.TlTutari).Concat(hasBilinenIadeler.Select(h => -Math.Abs(h.TlTutari))),
            hasTuketim.Select(t => (t.CikisHareketId, t.Adet, t.BirimMaliyetTl, t.BirimMaliyetHas)),
            hareketKuru);

        // ── Kâr köprüsü: işaretli kalemlerin TOPLAMI dönem kârına EŞİTTİR.
        var koprü = new List<object>
        {
            Kalem("BRUT_HASILAT", brutHasilatR, false),
            Kalem("IADE", -iadeR, false),
            Kalem("NET_HASILAT", hasilatR, true),
        };
        // Maliyeti eksik satışlar kâra girmez → köprüde AYRI kalem (yalnız varsa).
        if (eksikAdet > 0) koprü.Add(Kalem("MALIYETI_EKSIK_SATIS", -eksikTutarR, false));
        koprü.AddRange(new List<object>
        {
            // KAR-RAPOR-2: hiçbir satışın maliyeti bilinmiyorsa maliyet "0 ₺" DEĞİL, bilinmiyor (null).
            Kalem("MALIYET", hesaplanamadi ? null : -brutMaliyetR, false),
            Kalem("IADE_MALIYETI", hesaplanamadi ? null : iadeMaliyetR, false),
            Kalem("BRUT_KAR", karTl, true),
            Kalem("ISCILIK", iscilikR, false),
            Kalem("GIDER", -giderR, false),
            Kalem("DONEM_KARI", donemKariTl, true),
        });

        return new DonemOlcumu(
            satislar.Count, iadeler.Count, takaslar.Count, iscilik.Count,
            brutHasilatR, iadeR, hasilatR,
            brutMaliyetR, iadeMaliyetR, maliyetR,
            karTl, iscilikR, takasFarkTl, donemKariTl,
            hasilatHasR, iadeHas, maliyetHasR, karHas, defterGram, defterMadenDagilimi,
            maliyetliAdet, maliyetsizAdet, kursuzSatis,
            stornoAdet, stornoTutar,
            giderR, giderKirilimi, koprü,
            karDurumu, bilinenHasilatR, eksikSatislar.Count, eksikIadeler.Count, eksikTutarR,
            bilinenHasilatHasR, karHasIkiHane,
            altinKarDurumu, hasEksikSatisAdedi, hasEksikIadeAdedi, yalnizHasEksikAdedi,
            kartMaliyetliAdet, makasKari, altinFiyatKari);
    }

    /// <summary>
    /// M9 — ALTIN ürün TL kârının iki parçası (müşteri 16.10: has değeri kâr gibi görünmesin).
    /// <list type="bullet">
    ///   <item><b>Makas (işçilik) kârı</b> = hasılat − satılan malın has maliyetinin SATIŞ anı has fiyatıyla
    ///   değeri; has kârı (gram) × satış kuru — kuyumcunun alıp sattığı "makas".</item>
    ///   <item><b>Altın fiyat farkı kârı</b> = has maliyetinin satış kuruyla değeri − ödenen TL maliyet;
    ///   stoktaki altının alıştan satışa değer kazanması (düşüşte eksi).</item>
    /// </list>
    /// Toplamları = R2(hasılat) − R2(maliyet). Bir satırın kuru ya da TL/has maliyeti eksikse UYDURULMAZ: (null, null).
    /// İade hasılata EKSİ, tüketimi eksi adetle girer (Olc ile aynı işaret düzeni).
    /// </summary>
    public static (decimal? MakasTl, decimal? AltinFiyatTl) KarAyrimi(
        IEnumerable<decimal> hasilatIsaretli,
        IEnumerable<(string? CikisHareketId, decimal Adet, decimal? BirimTl, decimal? BirimHas)> tuketim,
        IReadOnlyDictionary<string, decimal> hareketKuru)
    {
        var hasilatlar = hasilatIsaretli.ToList();
        if (hasilatlar.Count == 0) return (null, null);
        decimal hasSatisKuruyla = 0m, maliyetTl = 0m;
        foreach (var t in tuketim)
        {
            if (t.BirimHas is not { } h || t.BirimTl is not { } tl || t.CikisHareketId is null
                || !hareketKuru.TryGetValue(t.CikisHareketId, out var kur) || kur <= 0m)
                return (null, null);
            hasSatisKuruyla += h * t.Adet * kur;
            maliyetTl += tl * t.Adet;
        }
        var hasDegeriR = R2(hasSatisKuruyla);
        return (R2(hasilatlar.Sum()) - hasDegeriR, hasDegeriR - R2(maliyetTl));
    }

    /// <summary>MADEN-AYRIMI: hareket ALTIN mı (boş/NULL ALTIN sayılır — HareketToplami.MadenKodu tek kaynak).</summary>
    internal static bool AltinMaden(string? maden) =>
        HareketToplami.MadenKodu(maden) == Kuyumcu.Core.Domain.Maden.Altin;

    /// <summary>KAR-RAPOR-2 kuruş kuralı: temel bileşen TEK KEZ yuvarlanır (TL 2 hane, AwayFromZero).</summary>
    internal static decimal R2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
    /// <summary>KAR-RAPOR-2 kuruş kuralı: has bileşeni TEK KEZ yuvarlanır (4 hane, AwayFromZero).</summary>
    internal static decimal R4(decimal d) => Math.Round(d, 4, MidpointRounding.AwayFromZero);

    private static object Kalem(string kod, decimal? tutarTl, bool araToplam)
        => new { kod, tutarTl = tutarTl is { } t ? Math.Round(t, 2, MidpointRounding.AwayFromZero) : (decimal?)null, araToplam };

    /// <summary>Kâr durumu kodları (ekrana basılmaz, sözlükten çevrilir).</summary>
    public static class KarDurumlari
    {
        public const string VeriYok = "VERI_YOK";
        public const string Tam = "TAM";
        public const string Kismi = "KISMI";
        public const string Hesaplanamadi = "HESAPLANAMADI";
    }

    /// <summary>
    /// PAKET KAR-RAPOR — fail-closed: hiç satış yoksa VERI_YOK; hepsinin maliyeti
    /// biliniyorsa TAM; bir kısmı biliniyorsa KISMI (kâr yalnız bilinenlerden);
    /// hiçbiri bilinmiyorsa HESAPLANAMADI (kâr null — satış kâr diye GÖSTERİLMEZ).
    /// </summary>
    public static string KarDurumuBelirle(int bilinenAdet, int eksikAdet) =>
        bilinenAdet == 0 && eksikAdet == 0 ? KarDurumlari.VeriYok
        : eksikAdet == 0 ? KarDurumlari.Tam
        : bilinenAdet > 0 ? KarDurumlari.Kismi
        : KarDurumlari.Hesaplanamadi;

    /// <summary>
    /// PAKET KAR-RAPOR — maliyeti TAM bilinen hareketlerin kimlikleri. Bir hareket
    /// ancak (1) en az bir parti tüketim satırı varsa, (2) satırlarının HEPSİNDE
    /// birim TL maliyeti doluysa ve (3) satırları hareketin adedini tamamen
    /// karşılıyorsa (parti defteri geride kalmamışsa) "bilinen" sayılır. Yarım
    /// bilinen satış da eksiktir: yarısını sıfır maliyetle saymak kârı şişirirdi.
    /// </summary>
    /// <summary>
    /// KAR-RAPOR-2 — parti satırı hiç yazılmamış ("satırsız") adet. MaliyetiBilinenler ile
    /// AYNI tolerans: orada "bilinen" sayılan küçük açık (≤ 0,0005 adet) burada "bilinmeyen"
    /// sayılmaz; aksi hâlde kâr TAM iken Panel "maliyet eksik" yazıyordu (hasım bulgusu).
    /// </summary>
    public static decimal SatirsizAdet(
        IEnumerable<(string Id, decimal? Adet)> hareketler,
        IEnumerable<(string? CikisId, decimal Adet)> tuketimler)
    {
        var karsilanan = tuketimler.Where(t => t.CikisId != null)
            .GroupBy(t => t.CikisId!)
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.Adet)));
        return hareketler.Sum(h =>
        {
            var acik = Math.Abs(h.Adet ?? 0m) - karsilanan.GetValueOrDefault(h.Id);
            return acik <= KarsilamaToleransi ? 0m : acik;
        });
    }

    /// <summary>Adet karşılama toleransı — MaliyetiBilinenler ve satırsız adet AYNI eşiği kullanır.</summary>
    public const decimal KarsilamaToleransi = 0.0005m;

    public static HashSet<string> MaliyetiBilinenler(
        IEnumerable<(string Id, decimal? Adet)> hareketler,
        IEnumerable<(string? CikisId, decimal Adet, decimal? BirimTl)> tuketimler)
    {
        var gruplar = tuketimler.Where(t => t.CikisId != null)
            .GroupBy(t => t.CikisId!)
            .ToDictionary(g => g.Key, g => g.ToList());
        var sonuc = new HashSet<string>();
        foreach (var (id, adet) in hareketler)
        {
            if (!gruplar.TryGetValue(id, out var satirlar) || satirlar.Count == 0) continue;
            if (satirlar.Any(x => x.BirimTl is null)) continue;
            var karsilanan = satirlar.Sum(x => Math.Abs(x.Adet));
            if (karsilanan + KarsilamaToleransi < Math.Abs(adet ?? 0m)) continue;
            sonuc.Add(id);
        }
        return sonuc;
    }

    /// <summary>
    /// PAKET MADEN-2 — HAS bacağının "bilinen" kümesi. TL bacağıyla AYNI kural ve AYNI yardımcı
    /// (<see cref="MaliyetiBilinenler"/>; iki bacak ayrı kural ÜRETMEZ): birim maliyet olarak
    /// satırın has maliyeti verilir — TL maliyeti olmayan satırın has maliyeti de bilinmiyor sayılır.
    /// Bir altın hareketi ancak TÜM parti satırlarında has maliyeti doluysa ve satırlar adedi
    /// karşılıyorsa has kârına girer. Çağıran yalnız ALTIN hareketlerini verir.
    /// </summary>
    public static HashSet<string> HasMaliyetiBilinenler(
        IEnumerable<(string Id, decimal? Adet)> altinHareketler,
        IEnumerable<(string? CikisId, decimal Adet, decimal? BirimTl, decimal? BirimHas)> tuketimler)
        => MaliyetiBilinenler(altinHareketler,
            tuketimler.Select(t => (t.CikisId, t.Adet, t.BirimTl is null ? null : t.BirimHas)));

    public static decimal HasCevir(
        IEnumerable<(decimal Tutar, string KurId)> satirlar,
        IReadOnlyDictionary<string, decimal> kurlar, ref int kursuz)
    {
        decimal toplam = 0m;
        foreach (var (tutar, kurId) in satirlar)
        {
            if (kurlar.TryGetValue(kurId ?? "", out var fiyat) && fiyat > 0m) toplam += tutar / fiyat;
            else kursuz++;
        }
        return toplam;
    }

    /// <summary>Değişim yüzdesi. Önceki dönem sıfırsa yüzde ANLAMSIZDIR → null.</summary>
    private static decimal? Yuzde(decimal? onceki, decimal? simdi)
        => onceki is not { } o || simdi is not { } s || o == 0m
            ? null : Math.Round((s - o) / Math.Abs(o) * 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>PAKET GOC-D — kâr-zarar dönemi: Rapor penceresi, bas ≤ bit, en çok 400 gün (tek kural).</summary>
    internal static void DonemDogrula(DateOnly baslangic, DateOnly bitis)
    {
        if (Kuyumcu.Core.Domain.Girdi.RaporDonemKurali.Denetle(baslangic, bitis, Zaman.YerelBugun(), Kuyumcu.Core.I18n.IstekDili.Gecerli) is { } h)
            throw new BadRequestException(h.Mesaj).AlanIle(h.Alan);
    }

    /// <summary>Boş (hesaplanamadı) değer null kalır — 0'a çevrilmez.</summary>
    private static decimal? R(decimal? d, int ondalik) => d is { } x ? Math.Round(x, ondalik, MidpointRounding.AwayFromZero) : null;

    /// <summary>Ürün bazlı kârlılık — hangi mal kazandırıyor, hangisi rafta duruyor.</summary>
    public async Task<object> UrunBazli(
        AktifKullanici islem, DateOnly baslangic, DateOnly bitis, string? subeId, int limit, int ofset = 0)
    {
        if (limit < 1 || limit > 500)
            throw new BadRequestException("limit 1 ile 500 arasında olmalı");
        // PAKET OLCEK-YUK t2 (tasarım hakemi KRİTİK, UX-S29): ekran "daha fazla göster" ile sonraki sayfayı ister.
        if (ofset < 0)
            throw new BadRequestException("ofset 0 ya da daha büyük olmalı");
        DonemDogrula(baslangic, bitis);   // PAKET GOC-D: ürün kırılımında dönem HİÇ denetlenmiyordu

        var (bas, _) = Zaman.YerelGunAraligiUtc(baslangic);
        var (_, sonBit) = Zaman.YerelGunAraligiUtc(bitis);

        // ═══ PAKET RAPOR-4 (KAR-HIZ) — SIRALAMA AY KOVALARINDAN, AYRINTI YALNIZ SAYFA İÇİN ══════════════
        // KÖK NEDEN (OLCEK-YUK + RAPOR-4 ölçümü): dönemdeki TÜM satış ve parti satırları (büyük kiracıda
        // 3 yılda 180.000 + 180.000) her istekte belleğe alınıyordu → p95 1,4–4,4 sn. SQL'de iki katlı
        // toplama da 2 sn sürdü (ölçüldü). YENİ YOL (ayrıntı: KarUrunOnToplam.cs):
        //   (1) ürün başına kova = kapanmış tam aylar (doğrulamalı bellek önbelleği) + canlı parçalar;
        //       kâr kovadan, kart maliyeti İSTEK ANINDA uygulanarak; eski sırayla sıralanır, sayfa kesilir;
        //   (2) sayfadaki ürünlerin satırları eskisiyle AYNI bellek hesabından (UrunSatirlari) geçer.
        // Eşdeğerlik: RaporDortTestleri RD-KH* (eski yol test projesinde AYNEN tutulur ve karşılaştırılır).
        var kovalar = await KarUrunOnToplam.DonemKovalari(db, islem.TenantId, baslangic, bitis, subeId);
        var kartAdaylari = kovalar.Where(kv => kv.Value.NAdet > 0 || kv.Value.AbsBos > 0m).Select(kv => kv.Key).ToList();
        var kartlar = await KartMaliyetleriToplu(islem.TenantId, kartAdaylari);
        // Eski sıra BİREBİR: kârı olan önce, kâr azalan, eşitlikte ürün kimliği (ordinal).
        // KAR-HIZ: tüm ürünleri sıralamak yerine yalnız ilk (ofset + limit) tutulur (öbek; aynı karşılaştırıcı) —
        // sıra ve sayfa tam sıralamayla AYNIDIR (eşitlik ürün kimliğiyle kırıldığı için toplam sıra).
        static int Sira((string Urun, decimal? Kar) a, (string Urun, decimal? Kar) b)
        {
            if (a.Kar.HasValue != b.Kar.HasValue) return a.Kar.HasValue ? -1 : 1;
            if (a.Kar is { } ka && b.Kar is { } kb && ka != kb) return kb.CompareTo(ka);
            return string.CompareOrdinal(a.Urun, b.Urun);
        }
        var gerekli = (int)Math.Min((long)ofset + limit, int.MaxValue);
        var oncelik = new PriorityQueue<(string Urun, decimal? Kar), (string Urun, decimal? Kar)>(
            Comparer<(string Urun, decimal? Kar)>.Create(static (a, b) => Sira(b, a)));   // kök = tutulanların EN SONUNCUSU
        decimal bilinen = 0m, bilinmeyen = 0m;
        var kursuzToplam = 0;
        foreach (var (urun, k) in kovalar)
        {
            var kart = KartMaliyeti(kartlar, urun);
            var kartVar = kart is not null;
            bilinen += k.AbsBilinen + (kartVar ? k.AbsBos : 0m);
            bilinmeyen += kartVar ? 0m : k.AbsBos;
            kursuzToplam += k.Kursuz;
            var oge = (Urun: urun, Kar: k.Kar(kart));
            if (oncelik.Count < gerekli) oncelik.Enqueue(oge, oge);
            else if (gerekli > 0 && Sira(oge, oncelik.Peek()) < 0) oncelik.DequeueEnqueue(oge, oge);
        }
        var toplamUrunSayisi = kovalar.Count;
        var ilkler = new List<(string Urun, decimal? Kar)>(oncelik.Count);
        while (oncelik.Count > 0) ilkler.Add(oncelik.Dequeue());
        ilkler.Sort(static (a, b) => Sira(a, b));
        var sayfaUrunIdleri = ilkler.Skip(ofset).Take(limit).Select(x => x.Urun).ToList();

        // (2) Sayfa ayrıntısı — eski bellek hesabı, YALNIZ sayfadaki ürünler, eski pencere ve süzgeçlerle.
        var sayfaNet = KarUrunOnToplam.NetSatislar(db, islem.TenantId, bas, sonBit, subeId)
            .Where(h => sayfaUrunIdleri.Contains(h.UrunId!));
        var sayfaSatislar = sayfaUrunIdleri.Count == 0 ? [] : (await KarUrunOnToplam.HareketleriOku(sayfaNet))
            .Select(h => new UrunSatisSatiri(h.Id, h.Tur, h.UrunId, h.TlTutari, h.HasGrami, h.KurSnapshotId, h.Adet, h.Maden))
            .ToList();
        var sayfaHareketIdleri = sayfaNet.Select(h => h.Id);
        var tuketimSorgu = db.Set<StokPartiTuketimi>().AsNoTracking()
            .Where(t => t.TenantId == islem.TenantId && t.Tarih >= bas && t.Tarih < sonBit
                     && t.CikisHareketId != null && sayfaHareketIdleri.Contains(t.CikisHareketId));
        if (!string.IsNullOrEmpty(subeId)) tuketimSorgu = tuketimSorgu.Where(t => t.SubeId == subeId);
        var hamSayfaTuketim = sayfaUrunIdleri.Count == 0 ? [] : await KarUrunOnToplam.TuketimleriOku(tuketimSorgu);
        var kartMaliyetleri = await KartMaliyetleri(islem.TenantId,
            hamSayfaTuketim.Where(t => t.BirimMaliyetTl is null).Select(t => t.UrunId));
        var kurlar = await KurHaritasi(islem.TenantId, sayfaSatislar.Select(h => h.KurSnapshotId));
        // M1b — kart TL maliyeti yoksa alış milyeminden; TL'si satışın KENDİ kuruyla (ana rapor yoluyla AYNI seçim).
        var milyemHas = await MilyemHasMaliyetleri(islem.TenantId,
            hamSayfaTuketim.Where(t => t.BirimMaliyetTl is null && KartMaliyeti(kartMaliyetleri, t.UrunId) is null).Select(t => t.UrunId));
        var hareketKuru = sayfaSatislar
            .Where(h => !string.IsNullOrEmpty(h.KurSnapshotId) && kurlar.ContainsKey(h.KurSnapshotId!))
            .ToDictionary(h => h.Id, h => kurlar[h.KurSnapshotId!]);
        var sayfaTuketim = hamSayfaTuketim
            .Select(t =>
            {
                var (tl, has, kart) = BirimMaliyetSec(t.BirimMaliyetTl, t.BirimMaliyetHas, t.UrunId, t.CikisHareketId,
                    kartMaliyetleri, milyemHas, hareketKuru);
                return new UrunTuketimSatiri(t.CikisHareketId, t.UrunId, t.Adet, tl, has, kart);
            })
            .ToList();

        var urunAdlari = new Dictionary<string, (string? Kod, string? Ad)>();
        if (sayfaUrunIdleri.Count > 0)
            foreach (var u in await db.Urunler.AsNoTracking()
                         .Where(u => u.TenantId == islem.TenantId && sayfaUrunIdleri.Contains(u.Id))
                         .Select(u => new { u.Id, u.Kod, u.Ad })
                         .Take(sayfaUrunIdleri.Count)
                         .ToListAsync())
                urunAdlari[u.Id] = (u.Kod, u.Ad);

        var satirlar = UrunSatirlari(sayfaSatislar, sayfaTuketim, kurlar)
            .Select(x => urunAdlari.TryGetValue(x.urunId, out var ka) ? x with { kod = ka.Kod, ad = ka.Ad } : x)
            .ToList();

        return new
        {
            baslangic = baslangic.ToString("yyyy-MM-dd"),
            bitis = bitis.ToString("yyyy-MM-dd"),
            subeId,
            urunSayisi = satirlar.Count,
            limit,
            ofset,
            // UX-S29: dönemde satılan TOPLAM ürün sayısı sunucudan gelir (ekran türetmez);
            // sunucu sınırına değildiyse ekran "daha fazlası var" der ve sonraki sayfayı ister.
            toplamUrunSayisi,
            dahaFazlasiVar = toplamUrunSayisi > ofset + satirlar.Count,
            maliyetiBilinenAdet = Math.Round(bilinen, 3, MidpointRounding.AwayFromZero),
            maliyetiBilinmeyenAdet = Math.Round(bilinmeyen, 3, MidpointRounding.AwayFromZero),
            kuruBulunamayanSatis = kursuzToplam,
            guvenilirlik = RaporGuvenilirligi.Hesapla(bilinen, bilinmeyen, kursuzToplam),
            // RAPOR-4 (VERİ 1): SAYFADAKİ satırlarda maliyeti ürün kartından gelen adet toplamı.
            kartMaliyetliAdet = satirlar.Sum(x => x.kartMaliyetliAdet),
            satirlar,
        };
    }


    // ── PAKET RAPOR-4 (KAR-HIZ): ürün satırının bellek hesabı — YALNIZ sayfadaki ürünlerin satırlarıyla çağrılır.
    //    Gövde eski UrunBazli'nin ürün hesabıdır (değiştirilmeden taşındı); eşdeğerlik testi bunu kanıtlar.
    public sealed record UrunSatisSatiri(string Id, string Tur, string UrunId, decimal TlTutari, decimal HasGrami,
        string KurSnapshotId, decimal? Adet, string? Maden);
    public sealed record UrunTuketimSatiri(string? CikisHareketId, string UrunId, decimal Adet,
        decimal? BirimMaliyetTl, decimal? BirimMaliyetHas, bool Kart);
#pragma warning disable IDE1006 // JSON alan adları yanıt sözleşmesiyle birebir (camelCase) kalır
    public sealed record UrunKarSatiri
    {
        public string urunId { get; init; } = "";
        public string? kod { get; init; }
        public string? ad { get; init; }
        public int satisAdedi { get; init; }
        public int iadeAdedi { get; init; }
        public decimal brutHasilatTl { get; init; }
        public decimal iadeTl { get; init; }
        public decimal hasilatTl { get; init; }
        public decimal? maliyetTl { get; init; }
        public decimal bilinenHasilatTl { get; init; }
        public decimal? karTl { get; init; }
        public string karDurumu { get; init; } = "";
        public int maliyetiEksikSatisAdedi { get; init; }
        public decimal maliyetiEksikTutarTl { get; init; }
        public string maden { get; init; } = "";
        public decimal? hasilatHas { get; init; }
        public decimal? maliyetHas { get; init; }
        public decimal? karHas { get; init; }
        public string? hasKarDurumu { get; init; }
        public int hasMaliyetiEksikSatisAdedi { get; init; }
        public decimal? karMarjiYuzde { get; init; }
        public decimal maliyetiBilinmeyenAdet { get; init; }
        public string guvenilirlik { get; init; } = "";
        public decimal kartMaliyetliAdet { get; init; }
        /// <summary>M9: makas (işçilik) kârı — yalnız altın, hesaplanamazsa null.</summary>
        public decimal? makasKariTl { get; init; }
        /// <summary>M9: altın fiyat farkı kârı — yalnız altın, hesaplanamazsa null.</summary>
        public decimal? altinFiyatKariTl { get; init; }
    }
#pragma warning restore IDE1006

    public static List<UrunKarSatiri> UrunSatirlari(
        IReadOnlyList<UrunSatisSatiri> satislar, IReadOnlyList<UrunTuketimSatiri> tuketimler,
        IReadOnlyDictionary<string, decimal> kurlar)
    {
        var kimlikler = satislar.Select(s => s.Id).ToHashSet();
        // PAKET KAR-RAPOR: ürün satırında da kâr YALNIZ maliyeti tam bilinen satışlardan.
        var bilinenKimlikler = MaliyetiBilinenler(
            satislar.Select(h => (h.Id, h.Adet)),
            tuketimler.Where(t => t.CikisHareketId != null && kimlikler.Contains(t.CikisHareketId))
                      .Select(t => (t.CikisHareketId, t.Adet, t.BirimMaliyetTl)));
        // MADEN-2: ürün satırının has bacağı da dönemle AYNI has-bilinen kuralıyla (tek yardımcı).
        var hasBilinenKimlikler = HasMaliyetiBilinenler(
            satislar.Where(h => AltinMaden(h.Maden)).Select(h => (h.Id, h.Adet)),
            tuketimler.Where(t => t.CikisHareketId != null && kimlikler.Contains(t.CikisHareketId))
                      .Select(t => (t.CikisHareketId, t.Adet, t.BirimMaliyetTl, t.BirimMaliyetHas)));
        var maliyetler = tuketimler
            .Where(t => t.CikisHareketId != null && kimlikler.Contains(t.CikisHareketId))
            .GroupBy(t => t.UrunId)
            .ToDictionary(g => g.Key, g => new
            {
                Tl  = g.Where(x => x.BirimMaliyetTl is not null && bilinenKimlikler.Contains(x.CikisHareketId!))
                       .Sum(x => x.BirimMaliyetTl!.Value * x.Adet),
                Has = g.Where(x => x.BirimMaliyetHas is not null && hasBilinenKimlikler.Contains(x.CikisHareketId!))
                       .Sum(x => x.BirimMaliyetHas!.Value * x.Adet),
                BilinenAdet    = g.Where(x => x.BirimMaliyetTl is not null).Sum(x => Math.Abs(x.Adet)),
                BilinmeyenAdet = g.Where(x => x.BirimMaliyetTl is null).Sum(x => Math.Abs(x.Adet)),
                KartAdet       = g.Where(x => x.Kart).Sum(x => Math.Abs(x.Adet)),
            });


        // M9: satışın kendi has fiyatı (KarAyrimi) — kuru bulunamayan satış sözlükte yoktur.
        var hareketKuru = satislar
            .Where(h => !string.IsNullOrEmpty(h.KurSnapshotId) && kurlar.ContainsKey(h.KurSnapshotId))
            .ToDictionary(h => h.Id, h => kurlar[h.KurSnapshotId]);
        var tuketimHarekette = tuketimler.Where(t => t.CikisHareketId != null && kimlikler.Contains(t.CikisHareketId)).ToList();

        return satislar
            .GroupBy(s => s.UrunId)
            .Select(g =>
            {
                var brut = g.Where(x => x.Tur == "SATIS").Sum(x => x.TlTutari);
                var iade = g.Where(x => x.Tur == "SATIS_IADE").Sum(x => Math.Abs(x.TlTutari));
                var hasilat = brut - iade;

                var kursuz = 0;
                var hasilatHas =
                    HasCevir(g.Where(x => x.Tur == "SATIS").Select(x => (x.TlTutari, x.KurSnapshotId)), kurlar, ref kursuz)
                  - HasCevir(g.Where(x => x.Tur == "SATIS_IADE").Select(x => (Math.Abs(x.TlTutari), x.KurSnapshotId)), kurlar, ref kursuz);

                var m = maliyetler.GetValueOrDefault(g.Key);
                var maliyet = m?.Tl ?? 0m;
                var maliyetHas = m?.Has ?? 0m;

                var bilinen = g.Where(x => bilinenKimlikler.Contains(x.Id)).ToList();
                var eksik   = g.Where(x => !bilinenKimlikler.Contains(x.Id)).ToList();
                var bilinenHasilat = bilinen.Where(x => x.Tur == "SATIS").Sum(x => x.TlTutari)
                                   - bilinen.Where(x => x.Tur == "SATIS_IADE").Sum(x => Math.Abs(x.TlTutari));
                // MADEN-2: has bacağı kendi bilinen kümesinden (has maliyeti eksik satış has kârına 0 maliyetle girmez).
                var hasBilinen = g.Where(x => hasBilinenKimlikler.Contains(x.Id)).ToList();
                var hasEksikAdet = g.Count(x => !hasBilinenKimlikler.Contains(x.Id));
                var yoksay = 0;
                var bilinenHasilatHas =
                    HasCevir(hasBilinen.Where(x => x.Tur == "SATIS").Select(x => (x.TlTutari, x.KurSnapshotId)), kurlar, ref yoksay)
                  - HasCevir(hasBilinen.Where(x => x.Tur == "SATIS_IADE").Select(x => (Math.Abs(x.TlTutari), x.KurSnapshotId)), kurlar, ref yoksay);
                var hasDurum = KarDurumuBelirle(hasBilinen.Count, hasEksikAdet);
                var durum = KarDurumuBelirle(bilinen.Count, eksik.Count);
                // KAR-RAPOR-2 kuruş kuralı (dönemle AYNI): kâr = yuvarlanmış bilinen hasılat − yuvarlanmış maliyet.
                var urunHesaplanamadi = durum == KarDurumlari.Hesaplanamadi;
                var hasilatR = R2(hasilat);
                var bilinenHasilatR = R2(bilinenHasilat);
                var maliyetR = R2(maliyet);
                decimal? kar = urunHesaplanamadi ? null : bilinenHasilatR - maliyetR;
                // MADEN-AYRIMI (tasarım hakemi t1 KRİTİK): has bacağı yalnız ALTIN ürünü taşır.
                // Gümüş/platin satırında hasilatHas/maliyetHas/karHas null — ekran "—" basar,
                // satır dönem "Altın kârı (has)" toplamına da katılmaz (Olc ile aynı küme).
                var urunMadeni = HareketToplami.MadenKodu(g.First().Maden);
                var altinUrun = AltinMaden(urunMadeni);
                var hasHesaplanamadi = urunHesaplanamadi || !altinUrun || hasDurum == KarDurumlari.Hesaplanamadi;
                decimal? karHasU = hasHesaplanamadi ? null : R4(bilinenHasilatHas) - R4(maliyetHas);
                var hasKimlik = hasBilinen.Select(x => x.Id).ToHashSet();
                var (makasU, altinFiyatU) = hasHesaplanamadi ? ((decimal?)null, (decimal?)null) : KarAyrimi(
                    hasBilinen.Select(x => x.Tur == "SATIS_IADE" ? -Math.Abs(x.TlTutari) : x.TlTutari),
                    tuketimHarekette.Where(t => hasKimlik.Contains(t.CikisHareketId!))
                        .Select(t => (t.CikisHareketId, t.Adet, t.BirimMaliyetTl, t.BirimMaliyetHas)),
                    hareketKuru);
                return new UrunKarSatiri
                {
                    urunId = g.Key,
                    kod = (string?)null,
                    ad  = (string?)null,
                    satisAdedi = g.Count(x => x.Tur == "SATIS"),
                    iadeAdedi  = g.Count(x => x.Tur == "SATIS_IADE"),
                    brutHasilatTl = Math.Round(brut, 2, MidpointRounding.AwayFromZero),
                    iadeTl = Math.Round(iade, 2, MidpointRounding.AwayFromZero),
                    hasilatTl = hasilatR,
                    // KAR-RAPOR-2: maliyeti hiç bilinmeyen satırda maliyet "0" DEĞİL null (bilinmiyor);
                    // KISMI satırda YALNIZ bilinen satışların maliyeti — bilinenHasilatTl ile eşleşir.
                    maliyetTl = urunHesaplanamadi ? (decimal?)null : maliyetR,
                    bilinenHasilatTl = bilinenHasilatR,
                    karTl = kar,
                    karDurumu = durum,
                    maliyetiEksikSatisAdedi = eksik.Count(x => x.Tur == "SATIS"),
                    maliyetiEksikTutarTl = hasilatR - bilinenHasilatR,
                    // HAS BACAĞI ASLA GİZLENMEZ: her ALTIN satırı TL kârıyla birlikte HAS kârını taşır (MADEN-AYRIMI: diğer madenlerde null).
                    maden = urunMadeni,
                    hasilatHas = altinUrun ? Math.Round(hasilatHas, 4, MidpointRounding.AwayFromZero) : (decimal?)null,
                    maliyetHas = hasHesaplanamadi ? (decimal?)null : R4(maliyetHas),
                    karHas = karHasU,
                    // MADEN-2: has bacağının kendi durumu (altın dışı satırda null) ve has maliyeti eksik satış adedi.
                    hasKarDurumu = altinUrun ? hasDurum : null,
                    hasMaliyetiEksikSatisAdedi = altinUrun ? g.Count(x => x.Tur == "SATIS" && !hasBilinenKimlikler.Contains(x.Id)) : 0,
                    karMarjiYuzde = kar is { } kk && bilinenHasilatR > 0m ? Math.Round(kk / bilinenHasilatR * 100m, 2, MidpointRounding.AwayFromZero) : (decimal?)null,
                    maliyetiBilinmeyenAdet = Math.Round(m?.BilinmeyenAdet ?? 0m, 3, MidpointRounding.AwayFromZero),
                    guvenilirlik = RaporGuvenilirligi.Hesapla(
                        m?.BilinenAdet ?? 0m, m?.BilinmeyenAdet ?? 0m, kursuz),
                    // RAPOR-4 (VERİ 1): maliyeti ürün kartından alınan adet — ekran "kart maliyeti" rozeti basar.
                    kartMaliyetliAdet = Math.Round(m?.KartAdet ?? 0m, 3, MidpointRounding.AwayFromZero),
                    makasKariTl = makasU,
                    altinFiyatKariTl = altinFiyatU,
                };
            })
            // Kârı hesaplanamayan ürün sona (null büyük değildir); kararlı sıra: ürün kimliği (ordinal).
            .OrderByDescending(x => x.karTl.HasValue).ThenByDescending(x => x.karTl)
            .ThenBy(x => x.urunId, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Stok değerlemesi — elde kalan malın maliyeti. Stok değeri raporu bugüne
    /// kadar yalnız has gram veriyordu; maliyet bacağı burada geliyor.
    /// </summary>
    public async Task<object> StokDegerlemesi(AktifKullanici islem, string? subeId)
    {
        // ⚠ KONSİNYE MAL VARLIĞIMIZ DEĞİLDİR — stok değerine katılmaz.
        // Katılırsa başkasının malını kendi varlığımız sanırız.
        // RAPOR-4: kiracı kimliği yerel değişkende — denetim.sh beyaz liste ölçümü bu deyimi (fonksiyonun ilk
        // deyimi) önceki fonksiyona atfediyordu; artık StokDegerlemesi'ne atfedilir (listede zaten var).
        var tenantId = islem.TenantId;
        var konsinyeUrunler = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Konsinye)
            .Select(u => u.Id).ToListAsync();

        var sorgu = db.Set<StokPartisi>().AsNoTracking()
            .Where(p => p.TenantId == islem.TenantId && p.AdetKalan > 0m
                     && !konsinyeUrunler.Contains(p.UrunId));
        if (!string.IsNullOrEmpty(subeId)) sorgu = sorgu.Where(p => p.SubeId == subeId);

        // ⚠ PAKET MADEN-AYRIMI (KAR-RAPOR-2 hasım t2 YÜKSEK): `toplamHasGram` altın hasıyla
        //   gümüş saf gramını TOPLUYORDU (H kiracısı: tüm stok gümüş → "66,60 g has"; Stok yaşı
        //   aynı stokta "0,0000 has" diyordu). Parti ürüne LEFT JOIN ile bağlanır; maden ayardan
        //   türetilir (Stok yaşı ile AYNI kural: ürünsüz parti → ayar yok → ALTIN). `toplamHasGram`
        //   YALNIZ ALTIN hasıdır; diğer madenler `madenKirilimi`nde kendi saf ve fiziksel gramıyla.
        var partiler = await (
            from p in sorgu
            join u in db.Urunler.AsNoTracking().Where(u => u.TenantId == islem.TenantId)
                on p.UrunId equals u.Id into uj
            from u in uj.DefaultIfEmpty()
            select new { p.UrunId, p.SubeId, p.AdetKalan, p.HasGramBirim,
                         p.BirimMaliyetTl, p.BirimMaliyetHas, p.Kaynak,
                         AyarId = u == null ? null : u.AyarId,
                         GramAgirlik = u == null ? (decimal?)null : u.GramAgirlik })
            .ToListAsync();

        string MadenOf(string? ayarId) =>
            Kuyumcu.Core.Domain.Ayarlar.MadenBul(ayarId ?? "") ?? Kuyumcu.Core.Domain.Maden.Altin;

        var madenKirilimi = partiler
            .GroupBy(p => MadenOf(p.AyarId))
            .Select(g => new
            {
                maden = g.Key,
                madenAdi = Kuyumcu.Core.Domain.Maden.Ad(g.Key),
                adet = Math.Round(g.Sum(x => x.AdetKalan), 3, MidpointRounding.AwayFromZero),
                // Saf gram (altında = has gram). Farklı madenlerin gramı TOPLANMAZ.
                toplamGram = Math.Round(g.Sum(x => x.AdetKalan * x.HasGramBirim), 6, MidpointRounding.AwayFromZero),
                // Fiziksel (tartı) gram: adet × ürün gramı; ürünü bulunamayan parti için bilinmez → null.
                fizikselGram = g.Any(x => x.GramAgirlik is null)
                    ? (decimal?)null
                    : Math.Round(g.Sum(x => x.AdetKalan * x.GramAgirlik!.Value), 4, MidpointRounding.AwayFromZero),
            })
            .OrderBy(m => m.maden == Kuyumcu.Core.Domain.Maden.Altin ? 0 : 1)
            .ThenByDescending(m => m.toplamGram)
            .ToList();
        var altinHasGram = madenKirilimi
            .Where(m => m.maden == Kuyumcu.Core.Domain.Maden.Altin).Sum(m => m.toplamGram);

        // PAKET RAPOR-4 tur 2 (hasım DÜŞÜK, VERİ 1): kâr hesabıyla AYNI kural — partide TL maliyeti yoksa ve ürün
        // kartındaki alış maliyeti > 0 ise o adet kart maliyetiyle bilinir; kaynak AYRI sayılır (kartMaliyetliAdet).
        // Has bacağı kartla HESAPLANMAZ (alış günü altın fiyatı bilinmiyor) — fail-closed, kâr motoruyla aynı.
        var kartlar = await KartMaliyetleriToplu(tenantId,
            partiler.Where(p => p.BirimMaliyetTl is null && !string.IsNullOrEmpty(p.UrunId))
                    .Select(p => p.UrunId!).Distinct().ToList());
        decimal? EtkinMaliyet(decimal? partiMaliyeti, string? urunId) =>
            partiMaliyeti ?? (urunId is not null && kartlar.TryGetValue(urunId, out var km) ? km : null);
        var kartMaliyetliAdet = partiler.Where(p => p.BirimMaliyetTl is null && EtkinMaliyet(null, p.UrunId) is not null)
                                        .Sum(p => p.AdetKalan);
        var kartMaliyetliTl = partiler.Where(p => p.BirimMaliyetTl is null && EtkinMaliyet(null, p.UrunId) is not null)
                                      .Sum(p => EtkinMaliyet(null, p.UrunId)!.Value * p.AdetKalan);

        var maliyetliTl  = partiler.Where(p => EtkinMaliyet(p.BirimMaliyetTl, p.UrunId) is not null)
                                   .Sum(p => EtkinMaliyet(p.BirimMaliyetTl, p.UrunId)!.Value * p.AdetKalan);
        // MADEN-AYRIMI (hasım t1 DÜŞÜK): has maliyeti AYRI sayılır — TL maliyeti bilinen ama
        // alış günü kuru olmayan parti (BirimMaliyetHas null) has tutarına 0 olarak girmez.
        // Has bacağı yalnız ALTIN partilerinden (dönem has kârıyla aynı küme).
        var altinPartiler = partiler.Where(p => MadenOf(p.AyarId) == Kuyumcu.Core.Domain.Maden.Altin).ToList();
        var maliyetliHas = altinPartiler.Where(p => p.BirimMaliyetHas is not null)
                                   .Sum(p => p.BirimMaliyetHas!.Value * p.AdetKalan);
        var hasMaliyetliAdet  = altinPartiler.Where(p => p.BirimMaliyetHas is not null).Sum(p => p.AdetKalan);
        var hasMaliyetsizAdet = altinPartiler.Where(p => p.BirimMaliyetHas is null).Sum(p => p.AdetKalan);

        var maliyetliAdet  = partiler.Where(p => EtkinMaliyet(p.BirimMaliyetTl, p.UrunId) is not null).Sum(p => p.AdetKalan);
        var maliyetsizAdet = partiler.Where(p => EtkinMaliyet(p.BirimMaliyetTl, p.UrunId) is null).Sum(p => p.AdetKalan);

        return new
        {
            subeId,
            toplamAdet = Math.Round(partiler.Sum(p => p.AdetKalan), 3, MidpointRounding.AwayFromZero),
            // MADEN-AYRIMI: YALNIZ ALTIN hası (aynı değer adıyla da); diğer madenler madenKirilimi'nde.
            toplamHasGram = Math.Round(altinHasGram, 6, MidpointRounding.AwayFromZero),
            altinHasGram  = Math.Round(altinHasGram, 6, MidpointRounding.AwayFromZero),
            madenKirilimi,

            maliyetliAdet    = Math.Round(maliyetliAdet, 3, MidpointRounding.AwayFromZero),
            maliyetsizAdet   = Math.Round(maliyetsizAdet, 3, MidpointRounding.AwayFromZero),
            // MADEN-AYRIMI (hasım t2 YÜKSEK): stokta mal var ama HİÇBİRİNİN maliyeti bilinmiyorsa
            //   stok maliyeti "0 ₺" DEĞİL, BİLİNMİYOR'dur → null (Anayasa md.1, fail-closed).
            //   Kısmen biliniyorsa tutar yalnız bilinen adetleri toplar; `stokMaliyetiKismi` = true.
            stokMaliyetiTl   = StokMaliyetiBilinmiyor(maliyetliAdet, maliyetsizAdet)
                ? (decimal?)null : Math.Round(maliyetliTl, 2, MidpointRounding.AwayFromZero),
            // TL maliyeti hiç bilinmiyorsa has karşılığı da bilinmez (yalnız gümüş stoğu dahil).
            stokMaliyetiHas  = StokMaliyetiBilinmiyor(maliyetliAdet, maliyetsizAdet)
                            || StokMaliyetiBilinmiyor(hasMaliyetliAdet, hasMaliyetsizAdet)
                ? (decimal?)null : Math.Round(maliyetliHas, 4, MidpointRounding.AwayFromZero),
            stokMaliyetiKismi = maliyetliAdet > 0m && maliyetsizAdet > 0m,
            // RAPOR-4 tur 2: maliyetli adedin kart maliyetiyle bilinen kısmı (ekranda "kart maliyeti" rozeti).
            kartMaliyetliAdet = Math.Round(kartMaliyetliAdet, 3, MidpointRounding.AwayFromZero),
            kartMaliyetliTl   = Math.Round(kartMaliyetliTl, 2, MidpointRounding.AwayFromZero),
            // MADEN-AYRIMI: has maliyetinin kendi kısmilik işareti ve sayımı (yalnız altın partileri).
            stokMaliyetiHasKismi = hasMaliyetliAdet > 0m && hasMaliyetsizAdet > 0m,
            hasMaliyetliAdet  = Math.Round(hasMaliyetliAdet, 3, MidpointRounding.AwayFromZero),
            hasMaliyetsizAdet = Math.Round(hasMaliyetsizAdet, 3, MidpointRounding.AwayFromZero),

            // Paket 35: güvenilirlik artık ÜÇ ucun üçünde de döner.
            guvenilirlik = RaporGuvenilirligi.Hesapla(maliyetliAdet, maliyetsizAdet),

            kaynakDagilimi = partiler.GroupBy(p => p.Kaynak)
                .Select(g => new { kaynak = g.Key, adet = Math.Round(g.Sum(x => x.AdetKalan), 3, MidpointRounding.AwayFromZero) })
                .OrderByDescending(x => x.adet).ToList(),

            konsinyeUrunSayisi = konsinyeUrunler.Count,

            uyari = maliyetsizAdet > 0m
                ? "Bir kısım stoğun alış maliyeti bilinmiyor; stok maliyeti bu adetleri İÇERMEZ."
                : konsinyeUrunler.Count > 0
                    ? $"{konsinyeUrunler.Count} konsinye ürün stok değerine DAHİL EDİLMEDİ — bu mallar işletmenin varlığı değildir."
                    : null,
        };
    }

    /// <summary>MADEN-AYRIMI: stokta mal var ve hiçbir adedin maliyeti bilinmiyor → tutar bilinmez (null).</summary>
    public static bool StokMaliyetiBilinmiyor(decimal maliyetliAdet, decimal maliyetsizAdet) =>
        maliyetliAdet == 0m && maliyetsizAdet > 0m;

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    /// <summary>
    /// PAKET RAPOR-4 (VERİ 1): ürün kartındaki alış maliyeti — YALNIZ parti satırında maliyet yoksa ve kart
    /// değeri sıfırdan BÜYÜKSE (0 ya da boş = bilinmiyor; fail-closed). Kimlik kümesiyle daraltılır (TB-19).
    /// </summary>
    internal async Task<Dictionary<string, decimal>> KartMaliyetleri(string tenantId, IEnumerable<string?> urunIdler)
    {
        var kume = urunIdler.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
        if (kume.Count == 0) return [];
        return await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == tenantId && kume.Contains(u.Id) && u.AlisMaliyeti != null && u.AlisMaliyeti > 0m)
            .Take(kume.Count)   // kimlik kümesi kadar (TB-19 sayfalı)
            .ToDictionaryAsync(u => u.Id, u => u.AlisMaliyeti!.Value);
    }

    /// <summary>
    /// RAPOR-4: sıralama için kart maliyetleri. Aday az ise kimlik kümesiyle, çoksa kiracının kart maliyeti
    /// girilmiş ürünleri sayfalı okunur (50.000 kimlikli IN listesi kurulmaz).
    /// </summary>
    /// <summary>Kart maliyeti okuma sayfası (iki sütun; 100.000 satır ≈ 3 MB) — büyük kiracıda tek gidiş-dönüş.</summary>
    private const int KartSayfasi = 100_000;
    /// <summary>RAPOR-4 tur 2: yalnız test — sayfa sınırını küçültüp (maliyet, id) sayfa anahtarını sınamak için (akış yerel).</summary>
    public static readonly AsyncLocal<int?> KartSayfasiTest = new();

    public async Task<Dictionary<string, decimal>> KartMaliyetleriToplu(string tenantId, IReadOnlyList<string> adaylar)
    {
        if (adaylar.Count == 0) return [];
        if (adaylar.Count <= 2_000) return await KartMaliyetleri(tenantId, adaylar);
        var sonuc = new Dictionary<string, decimal>();
        // RAPOR-4 tur 2 (KAR-HIZ ölçümü): sıra (alis_maliyeti, id) — IX_urunler_tenant_id_alis_maliyeti indeksinin
        // KENDİ sırası (InnoDB ikincil indeksi id'yi taşır) → kapsayan aralık okuması, sıralama yok. "id" sırası
        // eniyileyiciyi kiracı indeksiyle 50.000 satırlık tarama + birincil anahtar aramasına itiyordu (344–745 ms
        // → 23–29 ms, büyük kiracı, ölçüldü). Sayfa anahtarı (maliyet, id) çifti.
        decimal? sonMaliyet = null; string? sonId = null;
        while (true)
        {
            var q = db.Urunler.AsNoTracking()
                .Where(u => u.TenantId == tenantId && u.AlisMaliyeti != null && u.AlisMaliyeti > 0m);
            if (sonMaliyet is { } sm && sonId is not null)
                q = q.Where(u => u.AlisMaliyeti > sm || (u.AlisMaliyeti == sm && string.Compare(u.Id, sonId) > 0));
            var sayfa = await q.OrderBy(u => u.AlisMaliyeti).ThenBy(u => u.Id)
                .Select(u => new { u.Id, Maliyet = u.AlisMaliyeti!.Value })
                .Take(KartSayfasiTest.Value ?? KartSayfasi)
                .ToListAsync();
            foreach (var u in sayfa) sonuc[u.Id] = u.Maliyet;
            if (sayfa.Count < (KartSayfasiTest.Value ?? KartSayfasi)) return sonuc;
            (sonMaliyet, sonId) = (sayfa[^1].Maliyet, sayfa[^1].Id);
        }
    }

    /// <summary>
    /// M1b (müşteri 16.10: "925'den maliyetim varsa 925 yazacağım", "has değeri maliyet olarak
    /// gözüküyor" — sahte kâr) — ürün kartındaki ALIŞ milyeminden birim HAS maliyeti:
    /// net gram × alış milyemi/1000 (= has gram × alış milyemi / ayar milyemi). Yalnız ALTIN ürün
    /// (K20-4); parti ve kart TL maliyeti yoksa kullanılır. TL karşılığı satışın KENDİ kuruyla bulunur.
    /// </summary>
    internal async Task<Dictionary<string, decimal>> MilyemHasMaliyetleri(string tenantId, IEnumerable<string?> urunIdler)
    {
        var kume = urunIdler.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
        if (kume.Count == 0) return [];
        var satirlar = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == tenantId && kume.Contains(u.Id) && u.AlisMilyemi != null && u.AlisMilyemi > 0m && u.Milyem > 0m)
            .Select(u => new { u.Id, u.AyarId, u.HasGrami, u.Milyem, Alis = u.AlisMilyemi!.Value })
            .Take(kume.Count)
            .ToListAsync();
        return satirlar
            .Where(u => (Kuyumcu.Core.Domain.Ayarlar.MadenBul(u.AyarId) ?? Kuyumcu.Core.Domain.Maden.Altin) == Kuyumcu.Core.Domain.Maden.Altin)
            .ToDictionary(u => u.Id, u => Math.Round(u.HasGrami * u.Alis / u.Milyem, 6, MidpointRounding.AwayFromZero));
    }

    /// <summary>M1b — birim maliyet yedeği (parti → kart TL → alış milyemi × satış kuru).</summary>
    public static (decimal? Tl, decimal? Has, bool Kart) BirimMaliyetSec(
        decimal? partiTl, decimal? partiHas, string? urunId, string? cikisHareketId,
        IReadOnlyDictionary<string, decimal> kart, IReadOnlyDictionary<string, decimal> milyemHas,
        IReadOnlyDictionary<string, decimal> hareketKuru)
    {
        if (partiTl is not null) return (partiTl, partiHas, false);
        if (KartMaliyeti(kart, urunId) is { } k) return (k, null, true);
        if (urunId is not null && milyemHas.TryGetValue(urunId, out var h)
            && cikisHareketId is not null && hareketKuru.TryGetValue(cikisHareketId, out var kur) && kur > 0m)
            return (Math.Round(h * kur, 2, MidpointRounding.AwayFromZero), h, true);
        return (null, null, false);
    }

    internal static decimal? KartMaliyeti(IReadOnlyDictionary<string, decimal> kart, string? urunId) =>
        urunId is not null && kart.TryGetValue(urunId, out var m) ? m : null;

    private async Task<Dictionary<string, decimal>> KurHaritasi(string tenantId, IEnumerable<string> idler)
    {
        var kume = idler.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        if (kume.Count == 0) return [];
        return await db.KurSnapshots.AsNoTracking()
            .Where(k => k.TenantId == tenantId && kume.Contains(k.Id))
            .ToDictionaryAsync(k => k.Id, k => k.AltinHasFiyati);
    }
}

// ═════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/v1/raporlar")]
[Authorize]
[Yetki.ModulYetkisi("rapor")]
public sealed class KarZararController(KarZararServisi servis, KuyumcuDbContext db) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/raporlar/kar-zarar — Dönem kâr-zararı (TL + has) — MUHASEBE+</summary>
    [HttpGet("kar-zarar")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<object> KarZarar(
        [FromQuery, Required] DateOnly baslangic,
        [FromQuery, Required] DateOnly bitis,
        [FromQuery] string? subeId,
        [FromQuery, RegularExpression("ONCEKI_DONEM|GECEN_YIL")] string? karsilastir = null)
        => await servis.Donem(Islem, baslangic, bitis, await GorunumSubesi.Coz(db, Islem, subeId), karsilastir);   // PAKET ŞUBE-KAPSAM-A

    /// <summary>GET /api/v1/raporlar/kar-zarar/urun — Ürün bazlı kârlılık — MUHASEBE+</summary>
    [HttpGet("kar-zarar/urun")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<object> UrunBazli(
        [FromQuery, Required] DateOnly baslangic,
        [FromQuery, Required] DateOnly bitis,
        [FromQuery] string? subeId,
        [FromQuery, Range(1, 500)] int limit = 50,
        [FromQuery, Range(0, 100_000)] int ofset = 0)   // PAKET OLCEK-YUK t2: "daha fazla göster" sayfası
        => await servis.UrunBazli(Islem, baslangic, bitis, await GorunumSubesi.Coz(db, Islem, subeId), limit, ofset);   // PAKET ŞUBE-KAPSAM-A

    /// <summary>GET /api/v1/raporlar/stok-degerlemesi — Elde kalan malın maliyeti — MUHASEBE+</summary>
    [HttpGet("stok-degerlemesi")]
    [RolGerekli("SAHIP", "MUDUR", "MUHASEBE")]
    public async Task<object> StokDegerlemesi([FromQuery] string? subeId)
        => await servis.StokDegerlemesi(Islem, await GorunumSubesi.Coz(db, Islem, subeId));   // PAKET ŞUBE-KAPSAM-A
}
