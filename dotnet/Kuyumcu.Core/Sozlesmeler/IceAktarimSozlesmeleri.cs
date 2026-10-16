namespace Kuyumcu.Core.Sozlesmeler;

// ═════════════════════════════════════════════════════════════════════════════
// VERİ İÇE AKTARIMI İSTEMCİ SÖZLEŞMELERİ (paket 47 Faz A).
//
// ⚠ NEDEN PAGES İÇİNDE DEĞİL (S-2 / MÜHÜR 5 KOŞUL 3): Pages altına elle yazılan
//   yanıt modelleri dondurulmuştur (denetim.sh "Pages içinde istemci record
//   tanımı" sayacı). Yeni uçların yanıt şekli burada, TEK kopya hâlinde durur.
//
// Alan adları sunucunun camelCase JSON'una birebir karşılık gelir:
//   Modules/Iceaktarim/IceAktarimModulu.cs → Sablonlar · Prova · Satirlar · Gecmis
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>GET /ice-aktarim/sablonlar — bir kaynağın kolonu.</summary>
public sealed record IceAktarimKolonBilgisi(string Ad, bool Zorunlu, string Ornek);

/// <summary>GET /ice-aktarim/sablonlar — desteklenen kaynak.</summary>
public sealed record IceAktarimKaynakBilgisi(string Ad, List<IceAktarimKolonBilgisi>? Kolonlar, bool Defter = false)
{
    public List<IceAktarimKolonBilgisi> Liste => Kolonlar ?? [];
}

/// <summary>GET /ice-aktarim/sablonlar zarfı — tavanlar ve onay metni de buradan gelir.</summary>
public sealed record IceAktarimSablonYaniti(
    string OnayMetni, int MaksSatir, int MaksDosyaBayt, int GerekceAsgari,
    List<string>? Politikalar, List<IceAktarimKaynakBilgisi>? Kaynaklar)
{
    public List<string> PolitikaListesi => Politikalar ?? [];
    public List<IceAktarimKaynakBilgisi> KaynakListesi => Kaynaklar ?? [];
}

/// <summary>POST /ice-aktarim/prova — ilk 100 hatadan biri.</summary>
public sealed record IceAktarimHataSatiri(int SatirNo, string? HataKodu, string? HataMesaji);

/// <summary>
/// POST /ice-aktarim/prova yanıtı. <c>YazilanKayit</c> kuru provada DAİMA 0'dır
/// ve ekranda da böyle yazar — "hiçbir şey yazılmadı" iddiası sunucudan gelir.
/// </summary>
public sealed record IceAktarimProvaYaniti(
    string OturumId, string Kaynak, string Durum, string YinelenenPolitikasi,
    int ToplamSatir, int GecerliSatir, int HataliSatir, int AtlananSatir,
    int YazilanKayit, string OnayMetni,
    List<string>? BilinmeyenKolonlar, List<IceAktarimHataSatiri>? IlkHatalar,
    IceAktarimDefterOzeti? Ozet = null, string? AyniDosyaOturumId = null)
{
    public List<string> BilinmeyenListe => BilinmeyenKolonlar ?? [];
    public List<IceAktarimHataSatiri> HataListesi => IlkHatalar ?? [];
}

/// <summary>POST /ice-aktarim/{id}/uygula yanıtı.</summary>
public sealed record IceAktarimUygulaYaniti(
    string OturumId, string Durum, int UygulananSatir, int AtlananSatir,
    int HataliSatir, bool GeriAlinabilir);

/// <summary>POST /ice-aktarim/{id}/geri-al yanıtı.</summary>
public sealed record IceAktarimGeriAlYaniti(
    string OturumId, string Durum, int PasifeAlinan, string Mesaj, int GeriAlinan = 0);

/// <summary>
/// GOC-47B — açılış (defter) kaynağının kuru prova özeti (UX-S23: etkisi uygulamadan
/// önce görünür). Bakiye kaynağında TL/has/döviz toplamları, stok kaynağında adet ve
/// maliyeti bilinmeyen adet dolar; diğerleri 0 gelir.
/// </summary>
public sealed record IceAktarimDefterOzeti(
    int SatirSayisi, decimal TlToplam, decimal HasToplam, Dictionary<string, decimal>? DovizToplam,
    decimal ToplamAdet, decimal MaliyetiBilinmeyenAdet, decimal ToplamMaliyetTl, decimal ToplamHasGram)
{
    public Dictionary<string, decimal> DovizListesi => DovizToplam ?? [];
}

/// <summary>GET /ice-aktarim/gecmis satırı.</summary>
public sealed record IceAktarimGecmisSatiri(
    string Id, string KullaniciId, string KaynakAd, string DosyaAdi, string Gerekce,
    string Durum, string YinelenenPolitikasi, int ToplamSatir, int GecerliSatir,
    int HataliSatir, int AtlananSatir, int UygulananSatir,
    DateTime Olusturuldu, DateTime? Uygulandi, DateTime? GeriAlindi);

/// <summary>GET /ice-aktarim/gecmis zarfı.</summary>
public sealed record IceAktarimGecmisYaniti(int Toplam, int Limit, List<IceAktarimGecmisSatiri>? Kayitlar)
{
    public List<IceAktarimGecmisSatiri> Liste => Kayitlar ?? [];
}
