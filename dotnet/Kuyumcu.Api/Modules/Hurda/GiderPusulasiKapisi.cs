using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Hurda;

// ═════════════════════════════════════════════════════════════════════════════
// HURDA ALIŞ KAPISI — PLAN Adım 6. MASAK kapısıyla AYNI iki fazlı desen.
//
// MASAK kapısından FARKI: tutar eşiği YOK. VUK md.234 gereği vergi mükellefi
// olmayan kişiden yapılan her hurda alımında kimlik ve gider pusulası gerekir —
// 1.000 TL'de de, 1.000.000 TL'de de.
//
//   FAZ 1  Dogrula(satici)  → transaction öncesi. Eksikse BadRequestException.
//   FAZ 2  Kaydet(...)      → transaction içinde, hareket eklendikten sonra.
//                             Pusula ile hareket AYNI commit'te.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Hurda satıcısının kimliği — gider pusulasının zorunlu alanları.</summary>
public sealed class HurdaSaticiDto
{
    /// <summary>NIHAI_TUKETICI · ESNAF_MUAF · MUKELLEF</summary>
    [MaxLength(20)] public string? SaticiTipi { get; set; }

    [MaxLength(200)] public string? AdSoyad { get; set; }

    /// <summary>TCKN — 11 hane. Diske şifreli iner.</summary>
    [MaxLength(50)] public string? Tckn { get; set; }

    [MaxLength(500)] public string? Adres { get; set; }

    [MaxLength(500)] public string? MalAciklamasi { get; set; }

    /// <summary>Satıcı MÜKELLEF ise onun kestiği faturanın numarası (pusula düzenlenmez).</summary>
    [MaxLength(50)] public string? TedarikciFaturaNo { get; set; }
}

/// <summary>Stopaj oranları — mevzuat değişirse yapılandırmadan değişir, kod değişmez.</summary>
public sealed class HurdaAyarlari
{
    /// <summary>Esnaf muaflığından yararlanandan hurda alımı (GVK md.94). Varsayılan %2.</summary>
    public decimal EsnafMuafStopajOran { get; init; } = 0.02m;

    /// <summary>Vergi mükellefi olmayan nihai tüketici — tevkifat yok.</summary>
    public decimal NihaiTuketiciStopajOran { get; init; } = 0m;

    /// <summary>
    /// PAKET B-4 (B4-27 · K-KUR-4) — eski altın ALIŞ fiyatının ön-dolgu iskontosu.
    /// <para>
    /// VARSAYILAN <c>null</c> = ÖN-DOLGU YOK: TL/has-gram alanı boş açılır ve
    /// elle girilmesi ZORUNLUDUR. Sistem kur kaydındaki has fiyatından
    /// kendiliğinden bir alış fiyatı (spread) UYDURMAZ (K-KUR-3) — o sayı
    /// dükkânın ticari kararıdır, sistemin bilgisi değildir (Anayasa md.1).
    /// Set edilirse ön-dolgu <c>has_fiyat × (1 − oran)</c> olur ve düzenlenebilir
    /// kalır. Sunucu bu oranla ASLA fiyat türetmez; yalnız ekrana bildirir.
    /// </para>
    /// </summary>
    public decimal? AlisIskontoOran { get; init; } = null;

    /// <summary>VUK: teslimden itibaren gider pusulası düzenleme süresi (gün).</summary>
    public int PusulaDuzenlemeGunu { get; init; } = 7;

    /// <summary>
    /// PAKET P-13 (B-49) — satıcı ADRESİ gider pusulasının zorunlu unsuru mu?
    /// <para>
    /// VUK md.234 belgede satıcının adını, soyadını ve ADRESİNİ sayar; PATCH
    /// yolu (HurdaServisi.Guncelle) bu okumaya göre adresi zaten SİLDİRMİYOR.
    /// Oluşturmada ise alan isteğe bağlıydı: aynı belge bir yoldan adressiz
    /// doğabiliyor, diğer yoldan adressiz bırakılamıyordu (UX-S26 ihlali).
    /// </para>
    /// <para>
    /// ⚠ Kural KODA GÖMÜLÜ DEĞİL, yapılandırmadan okunur (ADR-0005): mali
    /// müşavir teyidi gelene kadar rakam/hüküm tek satırdan değişebilsin.
    /// Yalnız YENİ kayıtları bağlar; geçmiş adressiz kayıtlar zorlanmaz (K-25).
    /// </para>
    /// </summary>
    public bool SaticiAdresiZorunlu { get; init; } = true;

    /// <summary>
    /// PAKET P-20 (Y-10) — FİRE / RAMAT FARKI KAYDI AÇIK MI?
    /// <para>
    /// Rafineriye giden has ile dönen has arasındaki eksilme (fire), imalat
    /// (Katman 1) sınırındadır ve kapsam kararı KURUCUDADIR (K-13). Karar
    /// gelmeden bu alan KODA GÖMÜLMEZ; yapılandırmadan okunur ve VARSAYILANI
    /// KAPALIDIR (fail-closed / tutucu davranış — Anayasa md.1).
    /// </para>
    /// <para>
    /// Açıldığında bile fire hesabının nasıl muhasebeleşeceği (gider mi,
    /// maliyet mi) mali müşavir teyidine bağlıdır; bu sürüm o hesabı KURMAZ.
    /// </para>
    /// </summary>
    public bool FireKaydiAcik { get; init; } = false;
}

public sealed class GiderPusulasiKapisi(HurdaAyarlari ayarlar)
{
    /// <summary>DI'sız kurulumlar (testler) için varsayılan oranlar.</summary>
    public static GiderPusulasiKapisi Varsayilan() => new(new HurdaAyarlari());

    public HurdaAyarlari Ayarlar => ayarlar;

    /// <summary>Satıcı tipine göre stopaj oranı.</summary>
    public decimal StopajOran(string saticiTipi) => saticiTipi switch
    {
        SaticiTipi.EsnafMuaf => ayarlar.EsnafMuafStopajOran,
        SaticiTipi.NihaiTuketici => ayarlar.NihaiTuketiciStopajOran,
        _ => 0m,
    };

    /// <summary>
    /// FAZ 1 — kimlik ve satıcı tipi doğrulaması. FAIL-CLOSED: eksikse hurda alışı
    /// hiç başlamaz. Tüm eksikler tek seferde bildirilir.
    /// </summary>
    public string Dogrula(HurdaSaticiDto? satici)
    {
        if (satici is null)
            throw new BadRequestException(
                "Hurda alışında satıcı kimliği zorunludur (VUK md.234 — gider pusulası). " +
                $"Satıcı tipi seçin: {string.Join(", ", Data.SaticiTipi.Hepsi)}");

        var eksikler = new List<string>();
        var tcknGecersiz = false;

        if (!Data.SaticiTipi.Gecerli(satici.SaticiTipi))
            eksikler.Add($"Geçerli satıcı tipi zorunludur: {string.Join(", ", Data.SaticiTipi.Hepsi)}");

        if (satici.SaticiTipi == Data.SaticiTipi.Mukellef)
        {
            // Mükelleften alımda pusula düzenlenmez; onun faturası belgedir.
            if (string.IsNullOrWhiteSpace(satici.TedarikciFaturaNo))
                eksikler.Add("Mükellef satıcıdan alımda tedarikçi fatura numarası zorunludur");
        }
        else if (Data.SaticiTipi.Gecerli(satici.SaticiTipi))
        {
            if (string.IsNullOrWhiteSpace(satici.AdSoyad))
                eksikler.Add("Satıcı ad soyad zorunludur (gider pusulası)");
            // PAKET GOC-A (envanter C · gp-yd-ad "VUK md.234 belgesinde satıcı adı '1' ya da '.' olabilir"): K19 ad
            // kuralı (en az 2, en az bir harf) — ekran AdSoyad bileşeniyle AYNI Core kuralı ve sözlük metni.
            else if (Kuyumcu.Core.Domain.Girdi.AdKurali.Denetle(satici.AdSoyad) is { } adHatasi)
                eksikler.Add($"Satıcı ad soyad: {Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(adHatasi, Kuyumcu.Core.I18n.IstekDili.Gecerli)}");

            var tckn = Normalize(satici.Tckn);
            if (tckn.Length == 0)
                eksikler.Add("Satıcı TCKN zorunludur (gider pusulası)");
            else if (tckn.Length != 11 || !tckn.All(char.IsAsciiDigit))
                eksikler.Add("TCKN 11 haneli rakam olmalıdır");
            else if (!TcknGecerli(tckn))
            {
                // PAKET TCKN-DOGRULAMA: tek kaynak + sade Türkçe mesaj (ekran alan altıyla aynı metin).
                eksikler.Add(Kuyumcu.Core.Domain.KimlikNoDogrulayici.TcknGecersizMesaj);
                tcknGecersiz = true;
            }

            // PAKET P-13 (B-49) — VUK md.234 zorunlu unsuru; yapılandırmadan okunur.
            if (ayarlar.SaticiAdresiZorunlu && string.IsNullOrWhiteSpace(satici.Adres))
                eksikler.Add("Satıcı adresi zorunludur (VUK md.234 — gider pusulası)");
        }

        if (eksikler.Count > 0)
        {
            var ex = new BadRequestException($"Hurda alış: {string.Join(" · ", eksikler)}");
            // PAKET TCKN-DOGRULAMA: tek eksik geçersiz TCKN ise red o alana işaretlenir (alanlar.tckn).
            throw tcknGecersiz && eksikler.Count == 1 ? ex.AlanIle("tckn") : ex;
        }

        return satici.SaticiTipi!;
    }

    /// <summary>
    /// FAZ 2 — gider pusulasını transaction içinde yazar. MÜKELLEF satıcıda pusula
    /// düzenlenmez (fatura alınır), null döner.
    /// </summary>
    public GiderPusulasi? Kaydet(
        KuyumcuDbContext db, string pusulaNo, HurdaSaticiDto satici, string hareketId,
        string subeId, string? cariId, string ayarId, decimal gramAgirlik, decimal hasGrami,
        decimal brutTutar, AktifKullanici islem, DateTime now)
    {
        var tip = satici.SaticiTipi!;
        if (!Data.SaticiTipi.PusulaGerekir(tip)) return null;

        var oran = StopajOran(tip);
        var stopaj = Math.Round(brutTutar * oran, 2, MidpointRounding.AwayFromZero);

        var pusula = new GiderPusulasi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            PusulaNo = pusulaNo,
            HareketId = hareketId,
            SubeId = subeId,
            CariId = cariId,
            SaticiTipi = tip,
            SaticiAdSoyad = satici.AdSoyad!.Trim(),
            SaticiTckn = Normalize(satici.Tckn),
            SaticiAdres = satici.Adres?.Trim(),
            AyarId = ayarId,
            GramAgirlik = gramAgirlik,
            HasGrami = hasGrami,
            MalAciklamasi = satici.MalAciklamasi?.Trim(),
            BrutTutar = brutTutar,
            StopajOran = oran,
            StopajTutar = stopaj,
            NetOdenen = Math.Round(brutTutar - stopaj, 2, MidpointRounding.AwayFromZero),
            TeslimTarihi = now,
            DuzenlemeSonTarihi = now.AddDays(ayarlar.PusulaDuzenlemeGunu),
            Olusturuldu = now,
        };
        db.GiderPusulalari.Add(pusula);
        return pusula;
    }

    /// <summary>
    /// PAKET YASAL-4 / Y4-A — iptal edilmiş pusulanın AYNI alış için yeniden
    /// düzenlenmesi. Pusula yazan yer yine bu kapıdır (ikinci pusula yazma yolu yok).
    /// Mali alanlar (ayar, gram, has, brüt, stopaj oranı/tutarı, net, teslim ve son
    /// tarih) ESKİ BELGEDEN kopyalanır — belge üzerindeki <c>stopaj_oran</c>
    /// otoritedir, yeniden hesaplanmaz. Yalnız satıcı kimliği/adresi ve mal açıklaması
    /// yeni gelir. <c>duzeltilen_id</c> = eski belge.
    /// </summary>
    public GiderPusulasi YenidenDuzenle(
        KuyumcuDbContext db, GiderPusulasi eski, HurdaSaticiDto satici, string pusulaNo,
        AktifKullanici islem, DateTime now)
    {
        var pusula = new GiderPusulasi
        {
            Id = Kimlik.Ulid(),
            TenantId = islem.TenantId,
            PusulaNo = pusulaNo,
            HareketId = eski.HareketId,
            SubeId = eski.SubeId,
            CariId = eski.CariId,
            SaticiTipi = eski.SaticiTipi,
            SaticiAdSoyad = satici.AdSoyad!.Trim(),
            SaticiTckn = Normalize(satici.Tckn),
            SaticiAdres = satici.Adres?.Trim(),
            AyarId = eski.AyarId,
            GramAgirlik = eski.GramAgirlik,
            HasGrami = eski.HasGrami,
            MalAciklamasi = satici.MalAciklamasi?.Trim(),
            BrutTutar = eski.BrutTutar,
            StopajOran = eski.StopajOran,
            StopajTutar = eski.StopajTutar,
            NetOdenen = eski.NetOdenen,
            TeslimTarihi = eski.TeslimTarihi,
            DuzenlemeSonTarihi = eski.DuzenlemeSonTarihi,
            DuzeltilenId = eski.Id,
            Olusturuldu = now,
        };
        db.GiderPusulalari.Add(pusula);
        return pusula;
    }

    private static string Normalize(string? s) =>
        new((s ?? "").Where(char.IsAsciiDigit).ToArray());

    /// <summary>
    /// TCKN doğrulama algoritması. Uydurma numara girilmesini engeller — gider
    /// pusulası vergi belgesidir, hatalı kimlikle düzenlenmesi kuyumcuyu riske atar.
    /// </summary>
    /// <para>PAKET TCKN-DOGRULAMA: kural artık TEK kaynakta (Kuyumcu.Core KimlikNoDogrulayici);
    /// eski yerel kopyadaki negatif kalan hatası (geçerli numarayı reddetme) orada giderildi.</para>
    public static bool TcknGecerli(string tckn) => Kuyumcu.Core.Domain.KimlikNoDogrulayici.TcknGecerli(tckn);
}
