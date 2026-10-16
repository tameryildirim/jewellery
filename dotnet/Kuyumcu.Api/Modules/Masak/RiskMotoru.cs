using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Masak;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 40-A — RİSK MOTORU.
//
// BİLİNÇLİ OLARAK SAF: veritabanı, ağ, SAAT OKUMA yok (MasakKuralMotoru.cs:9-11
// ilkesi). Girdi verilir, karar alınır. Girdiyi TOPLAMAK RiskServisi'nin işidir.
//
// ⚠ TEK BİR SABİT RAKAM YOK. Puan, eşik, band oranı, sayı — hepsi
//   MasakRiskAyarlari'ndan gelir (ADR-0005). Bir rakamı buraya gömmek
//   "OrtaEsik değiştirilince seviye değişir" testini KIRMIZIYA döndürür.
//
// ⚠ SEBEPLER YALNIZ KOD SABİTİDİR (KVKK, Mehmet K40-3): ad, TCKN, belge no,
//   uyruk, adres bu listeye ASLA yazılmaz. Sebep metni ekranda sözlükten çevrilir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sebep kodları — <see cref="RiskSonuc.Sebepler"/> yalnız bunları taşır.</summary>
public static class RiskSebebi
{
    public const string PepBeyani = "PEP_BEYANI";
    public const string NakitAgirlik = "NAKIT_AGIRLIK";
    public const string EsigeYakin = "ESIGE_YAKIN";
    public const string SupheliGecmis = "SUPHELI_GECMIS";
    public const string KimlikEksik = "KIMLIK_EKSIK";
    public const string YuksekRiskliUlke = "YUKSEK_RISKLI_ULKE";
    public const string YaptirimEslesmesi = "YAPTIRIM_ESLESMESI";

    public static readonly IReadOnlyList<string> Hepsi =
        [PepBeyani, NakitAgirlik, EsigeYakin, SupheliGecmis, KimlikEksik, YuksekRiskliUlke, YaptirimEslesmesi];
}

/// <summary>
/// Motorun girdisi. Hepsi SAYISAL ya da mantıksal özettir; kişisel veri
/// (ad, belge no, adres) motora HİÇ girmez.
/// </summary>
/// <param name="CariTuru">MUSTERI / TEDARIKCI / … — Faz A'da puana girmez, sebep bağlamı için taşınır.</param>
/// <param name="PepBeyani">Müşterinin kendi beyanı.</param>
/// <param name="Uyruk">ISO 3166-1 alpha-3. Liste boşsa hiç kullanılmaz.</param>
/// <param name="SonDonemNakitOran">Pencere içindeki nakit tutarın toplam tutara oranı (0–1).</param>
/// <param name="SonDonemToplamTl">Pencere içindeki toplam TL hacmi — 0 ise oran anlamsızdır.</param>
/// <param name="EsigeYakinIslemSayisi">Kimlik eşiğinin bandında kalan işlem sayısı.</param>
/// <param name="KimlikEksikSayisi">Eşik üstü olup kimlik tespiti bulunmayan işlem sayısı.</param>
/// <param name="SupheliBildirimSayisi">Bu cari için açılmış şüpheli işlem bildirimi sayısı.</param>
/// <param name="YaptirimEslesmesiVar">FAZ B girdisi. Faz A'da HER ZAMAN false verilir.</param>
public sealed record RiskGirdi(
    string CariTuru,
    bool PepBeyani,
    string? Uyruk,
    decimal SonDonemNakitOran,
    decimal SonDonemToplamTl,
    int EsigeYakinIslemSayisi,
    int KimlikEksikSayisi,
    int SupheliBildirimSayisi,
    bool YaptirimEslesmesiVar);

/// <summary>Motorun kararı.</summary>
public sealed record RiskSonuc(int Puan, string Seviye, IReadOnlyList<string> Sebepler);

public sealed class RiskMotoru(MasakRiskAyarlari ayarlar)
{
    public MasakRiskAyarlari Ayarlar => ayarlar;

    /// <summary>
    /// Puanı, seviyeyi ve sebep KODLARINI üretir.
    /// </summary>
    /// <param name="girdi">Toplanmış sayısal özet.</param>
    public RiskSonuc Degerlendir(RiskGirdi girdi)
    {
        var puan = 0;
        var sebepler = new List<string>();

        if (girdi.YaptirimEslesmesiVar)
        {
            puan += ayarlar.YaptirimPuan;
            sebepler.Add(RiskSebebi.YaptirimEslesmesi);
        }

        if (girdi.PepBeyani)
        {
            puan += ayarlar.PepPuan;
            sebepler.Add(RiskSebebi.PepBeyani);
        }

        // Oran ancak GERÇEK bir hacim varken anlamlıdır: 0 TL'lik dönemde
        // "nakit oranı %100" demek uydurma olurdu.
        if (girdi.SonDonemToplamTl > 0m && girdi.SonDonemNakitOran > ayarlar.NakitAgirlikOrani)
        {
            puan += ayarlar.NakitAgirlikPuan;
            sebepler.Add(RiskSebebi.NakitAgirlik);
        }

        if (ayarlar.StructuringAsgariIslem > 0
            && girdi.EsigeYakinIslemSayisi >= ayarlar.StructuringAsgariIslem)
        {
            puan += ayarlar.StructuringPuan;
            sebepler.Add(RiskSebebi.EsigeYakin);
        }

        if (girdi.SupheliBildirimSayisi > 0)
        {
            puan += ayarlar.SupheliGecmisPuan;
            sebepler.Add(RiskSebebi.SupheliGecmis);
        }

        // Katsayı 0 ise sebep de yazılmaz: puana etki etmeyen bir gerekçe
        // ekranda "neden yüksek" sorusunu YANLIŞ cevaplardı.
        if (ayarlar.KimlikEksikPuan > 0 && girdi.KimlikEksikSayisi > 0)
        {
            puan += ayarlar.KimlikEksikPuan * girdi.KimlikEksikSayisi;
            sebepler.Add(RiskSebebi.KimlikEksik);
        }

        // Liste BOŞKEN uyruk hiç puan üretmez — listeyi biz uyduramayız.
        if (girdi.Uyruk is { Length: > 0 } uyruk
            && ayarlar.YuksekRiskliUlkeler.Contains(uyruk, StringComparer.OrdinalIgnoreCase))
        {
            puan += ayarlar.UlkeRiskPuan;
            sebepler.Add(RiskSebebi.YuksekRiskliUlke);
        }

        return new RiskSonuc(puan, Seviye(puan), sebepler);
    }

    /// <summary>Puanı banda çevirir. Bandın SINIRI yapılandırmadan gelir.</summary>
    public string Seviye(int puan) =>
        puan >= ayarlar.YuksekEsik ? RiskSeviyesi.Yuksek
        : puan >= ayarlar.OrtaEsik ? RiskSeviyesi.Orta
        : RiskSeviyesi.Dusuk;

    /// <summary>
    /// Structuring bandının alt sınırı — "eşiğe yakın" sayımının SQL tarafı
    /// bu iki değeri kullanır; oranı iki yere yazmamak için burada durur.
    /// </summary>
    public (decimal Alt, decimal Ust) Band(decimal kimlikEsigi) =>
        (kimlikEsigi * ayarlar.BandAltOran, kimlikEsigi * ayarlar.BandUstOran);
}
