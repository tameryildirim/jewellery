namespace Kuyumcu.Web.Bilesenler.Uyum;

// PAKET YASAL-3 — Uyum/Panel bileşenlerinin okuduğu yanıt şekilleri (camelCase JSON).
// Sunucu: Kuyumcu.Api/Modules/Masak/SaklamaYaptirimModulu.cs · YaptirimServisi.Ozet.

public sealed record UyumYasalOzet(
    int SaklamaYaklasan, int SaklamaGun, bool SaklamaKuru,
    DateOnly? ListeTarihi, bool ListeEski, int? ListeYasiGun, int AcikTam, int AcikBenzerlik);

public sealed record SaklamaBekletmeSatiri(
    string Id, string Kapsam, string? KimlikTespitiId, DateTime Baslangic, DateOnly BitisGunu,
    string Gerekce, DateTime Olusturuldu, DateTime? Kaldirildi, bool Aktif);

public sealed record SaklamaMaskelemeSatiri(DateTime Tarih, bool Otomatik, string? Ozet);

public sealed record SaklamaBekletmeListesi(
    bool Kuru, List<SaklamaBekletmeSatiri> Bekletmeler, List<SaklamaMaskelemeSatiri> Maskelemeler);

public sealed record YaptirimEslesmeSatiri(
    string Id, string Tur, string Nokta, string? CariId, string? CariAd, string Durum,
    DateOnly? BildirimSonGunu, DateOnly? MasakBildirimTarihi, string? MasakEvrakNo, string? Not, DateTime Olusturuldu);

public sealed record YaptirimOzeti(
    DateOnly? ListeTarihi, string? Kaynak, string? KaynakUrl, int? ListeYasiGun, bool ListeEski,
    int EskiListeGun, int YururlukteSatir, int AcikTam, int AcikBenzerlik, List<YaptirimEslesmeSatiri> Eslesmeler,
    EsikBolmeSinyali? EsikBolmeSinyali = null);

/// <summary>PAKET YASAL-3 tur 2 — art arda Peşin satışta "başka biri" beyanları (son N gün).</summary>
public sealed record EsikBolmeSinyali(int Gun, int Adet, int Sinir, List<EsikBolmeSinyalSatiri> Son);

public sealed record EsikBolmeSinyalSatiri(DateTime Tarih, string? Kullanici);

/// <summary>Bekletme çekmecesinde "tek kayıt" seçeneği için maskeli tespit özeti (/masak/kimlik-tespitleri).</summary>
public sealed record SaklamaTespitOzeti(string Id, string AdSoyad, DateTime Tarih, DateTime SaklamaBitis);
