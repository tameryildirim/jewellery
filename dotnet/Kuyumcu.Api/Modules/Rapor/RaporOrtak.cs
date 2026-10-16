using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Rapor;

// ═════════════════════════════════════════════════════════════════════════════
// RAPOR ORTAK ALTYAPISI — paket 34 Faz A.
//
// NEDEN VAR: dönem çözümü (K5 yerel gün → UTC) bugüne kadar her rapor ucunda
// TEKRAR yazılıyordu (RaporModulu.GunlukOzet, AylikOzet, PosFaturaMutabakati,
// KarZararServisi.Donem). Dört kopya demek, beşinci raporu yazan kişinin
// DateTime.Today'e dönmesi ve İstanbul'da 00:00–03:00 cirosunun yanlış güne
// düşmesi demektir. Dönem çözümü artık TEK yerdedir ve sınanır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Bir raporun çözülmüş dönemi. <see cref="Bas"/>/<see cref="Bit"/> UTC yarı
/// açık aralıktır ([Bas, Bit)); <see cref="YerelBas"/>/<see cref="YerelBit"/>
/// kullanıcının gördüğü İŞLETME (Europe/Istanbul) günleridir.
/// </summary>
public readonly record struct RaporDonemi(
    DateTime Bas, DateTime Bit, DateOnly YerelBas, DateOnly YerelBit);

public static class RaporAraligi
{
    /// <summary>En geniş dönem — büyük kiracıda bir yıllık döküm sunucuyu kilitler.</summary>
    public const int EnFazlaGun = Kuyumcu.Core.Domain.Girdi.RaporDonemKurali.EnCokGun;

    /// <summary>
    /// Dönem çözümü. Her iki uç da yerel (Europe/Istanbul) GÜN'dür ve aralık
    /// KAPALI yorumlanır: 01.09–01.09 tek günü kapsar.
    /// <para>Varsayılan (ikisi de null): BUGÜN.</para>
    /// </summary>
    /// <exception cref="BadRequestException">bitiş &lt; başlangıç ya da dönem çok uzun.</exception>
    public static RaporDonemi Coz(DateOnly? baslangic, DateOnly? bitis)
    {
        var bugun = Zaman.YerelBugun();
        var bas = baslangic ?? bitis ?? bugun;
        var bit = bitis ?? baslangic ?? bugun;

        // PAKET GOC-D (envanter E · "rapor tarih aralıkları 400 + bas ≤ bit"): kural Core'da TEK yerde
        // (RaporDonemKurali — ekranın alan altı metniyle AYNI sözlük anahtarı). Eskiden 3000 yılı
        // gibi pencere dışı gün sessizce boş rapor döndürüyordu (UX-S28); artık 400 + alan.
        if (Kuyumcu.Core.Domain.Girdi.RaporDonemKurali.Denetle(bas, bit, bugun, Kuyumcu.Core.I18n.IstekDili.Gecerli, EnFazlaGun) is { } h)
            throw new BadRequestException(h.Mesaj).AlanIle(h.Alan);

        // K5: yerel gün sınırı UTC'ye çevrilir. Bit, bitiş gününün SONU (dahil).
        var (basUtc, _) = Zaman.YerelGunAraligiUtc(bas);
        var (_, bitUtc) = Zaman.YerelGunAraligiUtc(bit);
        return new RaporDonemi(basUtc, bitUtc, bas, bit);
    }
}

/// <summary>
/// Rapor süzgeçleri. Kimlik alanları ÇAĞIRAN servis tarafından kiracı sınırında
/// ayrıca doğrulanır (Mehmet K34-4: yabancı kiracının kimliği 404 döner).
/// </summary>
public sealed record RaporFiltresi(
    string? SubeId = null,
    string? CariId = null,
    string? KullaniciId = null,
    string? KategoriId = null,
    string? ReyonId = null,
    string? Tur = null,
    string? OdemeTuru = null,
    int Limit = 100,
    int Ofset = 0)
{
    public const int EnFazlaLimit = 500;

    /// <summary>
    /// SERVİS İÇİ doğrulama.
    /// <para>
    /// ⚠ Neden burada: <c>[Range(1,500)]</c> yalnız controller model bağlamada
    /// çalışır (Aşkın'ın kuralı — DataAnnotations servis yolunda ÇALIŞMAZ).
    /// Servisi doğrudan çağıran her yol (test, iç çağrı) da limitsiz döküm
    /// yapamasın diye kapı buradadır.
    /// </para>
    /// </summary>
    public RaporFiltresi Dogrula()
    {
        // PAKET GOC-D (K32 süzgeç beyaz listesi): bilinmeyen tür/ödeme aracı eskiden sorguya
        // aynen gidiyor ve "kayıt yok" dönüyordu (UX-S28). Tür listesi deftere yazılan türlerin
        // belge listesi (Entities.HareketTurleri), ödeme aracı Core RaporSuzgecKurali.OdemeAraclari.
        if (!Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.ListedeMi(Tur, Kuyumcu.Api.Data.Sabitler.HareketTurleri))
            throw new BadRequestException(Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.Mesaj(Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle("tur");
        if (!Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.ListedeMi(OdemeTuru, Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.OdemeAraclari))
            throw new BadRequestException(Kuyumcu.Core.Domain.Girdi.RaporSuzgecKurali.Mesaj(Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle("odemeTuru");
        if (Limit < 1 || Limit > EnFazlaLimit)
            throw new BadRequestException($"limit 1 ile {EnFazlaLimit} arasında olmalı");
        if (Ofset < 0)
            throw new BadRequestException("ofset negatif olamaz");
        return this;
    }
}

/// <summary>
/// Sayfalı rapor zarfı — TB-19 sözleşmesi.
/// <para>
/// <see cref="ToplamSatir"/> SUNUCUDA <c>CountAsync</c> ile ölçülür; dönen
/// satır sayısından TÜRETİLMEZ (aksi hâlde limit = toplam olur ve ekran
/// "daha fazlası var" uyarısını hiç basmaz).
/// </para>
/// </summary>
public sealed record SayfaliSonuc<T>(int ToplamSatir, int Ofset, int Limit, IReadOnlyList<T> Satirlar)
{
    /// <summary>Sunucu sınırına değildi mi (UX-S29: sessiz kırpma yok).</summary>
    public bool DahaFazlasiVar => Ofset + Satirlar.Count < ToplamSatir;
}

/// <summary>
/// Rapora ne kadar güvenilebileceğinin açık beyanı (Anayasa md.1: bilinmeyen
/// sıfır değildir). KarZararServisi'nin özel yardımcısı buraya taşındı ki
/// bütün raporlar AYNI ölçeği kullansın.
/// </summary>
public static class RaporGuvenilirligi
{
    public const string VeriYok = "VERI_YOK";
    public const string Tam = "TAM";
    public const string Yuksek = "YUKSEK";
    public const string Orta = "ORTA";
    public const string Dusuk = "DUSUK";

    /// <param name="bilinen">Değeri ölçülebilen kısım.</param>
    /// <param name="bilinmeyen">Değeri ölçülemeyen kısım (maliyetsiz, kursuz…).</param>
    /// <param name="ekKusur">Sayısal olmayan ek eksiklik adedi (kur fotoğrafı yok vb.).</param>
    public static string Hesapla(decimal bilinen, decimal bilinmeyen, int ekKusur = 0)
    {
        if (bilinen + bilinmeyen == 0m) return VeriYok;
        if (bilinmeyen == 0m && ekKusur == 0) return Tam;
        var oran = bilinen / (bilinen + bilinmeyen);
        return oran >= 0.9m ? Yuksek : oran >= 0.5m ? Orta : Dusuk;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-25 / B-8 — SUNUCUDA TOPLAMA (ölçek borcu).
//
// NEDEN VAR: GunlukOzet / AylikOzet / PosFaturaMutabakati dönemin BÜTÜN
// hareketlerini `ToListAsync()` ile belleğe alıp LINQ-to-Objects ile
// topluyordu. Dönem (gün/ay) sınırlıydı ama SATIR SAYISI değildi: 3 yıllık
// veriyle koşan bir dükkânın yoğun bir gününde bu, on binlerce satırın
// uygulama belleğine taşınması demektir (TB-19 / 34-Ö açık borcu).
//
// ÇÖZÜM: hareket toplamları veritabanında `GROUP BY tur, maden` ile alınır.
// Dönen satır sayısı |tür| × |maden| ile SINIRLIDIR (onlarca), hareket
// sayısıyla DEĞİL. Rapor gövdeleri birebir aynı alanları üretmeye devam eder;
// yalnız kaynak değişir.
//
// ⚠ Mutlak toplamlar da SUNUCUDA alınır (`Math.Abs` → SQL `ABS`). Bellekte
// alınsaydı satırları çekmek zorunda kalırdık — bütün mesele buydu.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Bir (tür, maden) çiftinin SUNUCUDA hesaplanmış toplamı. Satır listesi
/// DEĞİLDİR: rapor gövdesinin ihtiyaç duyduğu beş toplamı taşır.
/// </summary>
/// <param name="Tur">Hareket türü (SATIS, ALIS, HURDA_ALIS…).</param>
/// <param name="Maden">Ham maden kodu; boş/NULL ALTIN sayılır (K20-8).</param>
/// <param name="Adet">Satır adedi (eski <c>liste.Count</c>).</param>
/// <param name="TlToplam">İşaretli TL toplamı.</param>
/// <param name="TlMutlak">|TL| toplamı — alış/hurda tarafı bunu kullanır.</param>
/// <param name="HasToplam">İşaretli has gram toplamı.</param>
/// <param name="HasMutlak">|has gram| toplamı — satış tarafı bunu kullanır.</param>
public sealed record HareketKirilimi(
    string Tur, string? Maden, int Adet,
    decimal TlToplam, decimal TlMutlak,
    decimal HasToplam, decimal HasMutlak);

public static class HareketToplami
{
    /// <summary>
    /// Verilen hareket sorgusunu SUNUCUDA (tür, maden) kırılımına toplar.
    /// Çağıran sorguyu kiracı/dönem ile SINIRLAMIŞ olmalıdır.
    /// </summary>
    public static Task<List<HareketKirilimi>> Coz(IQueryable<Data.Hareket> sorgu) =>
        Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            sorgu.GroupBy(h => new { h.Tur, h.Maden })
                 .Select(g => new HareketKirilimi(
                     g.Key.Tur,
                     g.Key.Maden,
                     g.Count(),
                     g.Sum(x => x.TlTutari),
                     g.Sum(x => Math.Abs(x.TlTutari)),
                     g.Sum(x => x.HasGrami),
                     g.Sum(x => Math.Abs(x.HasGrami)))));

    /// <summary>Kırılımı türe göre süzer (eski <c>liste.Where(h =&gt; h.Tur == …)</c>).</summary>
    public static List<HareketKirilimi> Tur(IEnumerable<HareketKirilimi> hepsi, string tur)
        => [.. hepsi.Where(x => string.Equals(x.Tur, tur, StringComparison.Ordinal))];

    /// <summary>Satır adedi toplamı.</summary>
    public static int Adet(IEnumerable<HareketKirilimi> k) => k.Sum(x => x.Adet);

    /// <summary>Madenin normalize hâli — boş/NULL ALTIN'dır (K20-8, tek kaynak).</summary>
    public static string MadenKodu(string? ham) =>
        string.IsNullOrWhiteSpace(ham) ? Kuyumcu.Core.Domain.Maden.Altin : ham;

    /// <summary>
    /// YALNIZ ALTIN has toplamı (K20-2). <paramref name="mutlak"/> true ise
    /// |has| toplanır — satış tarafının eski <c>Math.Abs(h.HasGrami)</c> davranışı.
    /// </summary>
    public static decimal AltinHas(IEnumerable<HareketKirilimi> k, bool mutlak = false) =>
        Math.Round(k.Where(x => MadenKodu(x.Maden) == Kuyumcu.Core.Domain.Maden.Altin)
                    .Sum(x => mutlak ? x.HasMutlak : x.HasToplam), 6);

    /// <summary>
    /// Birden çok maden varsa ekranda/yanıtta görünecek uyarı; tek madende null.
    /// <para>
    /// ⚠ TEK KAYNAK (UX-S31): bu metin daha önce yalnız RaporModulu'nun içinde
    /// özel bir yardımcıdaydı. P-45 ile kâr-zarar raporu da aynı uyarıyı basmak
    /// zorunda kaldı; metnin ikinci kopyası yazılmadı, buraya taşındı.
    /// </para>
    /// </summary>
    /// <param name="dagilim"><see cref="MadenDagilimi"/> çıktısı.</param>
    /// <param name="alanAdi">Yalnız ALTIN taşıyan alanın yanıttaki adı.</param>
    public static string? MadenUyarisi(IReadOnlyCollection<object> dagilim, string alanAdi = "toplamHas") =>
        dagilim.Count > 1
            ? $"Bu dönemde birden çok maden var. `{alanAdi}` yalnız ALTIN has gramıdır; " +
              "diğer madenler `madenDagilimi` altında ayrı verilir — farklı madenlerin " +
              "gramları toplanmaz."
            : null;

    /// <summary>Maden kırılımı — eski <c>MadenDagilimi</c> ile aynı şekil ve sıra.</summary>
    public static List<object> MadenDagilimi(IEnumerable<HareketKirilimi> k, bool mutlak = false) =>
        [.. k.GroupBy(x => MadenKodu(x.Maden))
             .Select(g => new
             {
                 maden = g.Key,
                 madenAdi = Kuyumcu.Core.Domain.Maden.Ad(g.Key),
                 toplamHas = Math.Round(g.Sum(x => mutlak ? x.HasMutlak : x.HasToplam), 6),
                 satirSayisi = g.Sum(x => x.Adet),
             })
             .Where(x => x.toplamHas != 0m)
             .OrderByDescending(x => x.toplamHas)
             .Cast<object>()];
}
