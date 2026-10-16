namespace Kuyumcu.Core.Sozlesmeler;

// ═════════════════════════════════════════════════════════════════════════════
// BANKA EKSTRESİ İÇE AKTARMA — İSTEMCİ SÖZLEŞMELERİ (paket 32 Faz A).
//
// ⚠ NEDEN PAGES İÇİNDE DEĞİL (S-2 / MÜHÜR 5 KOŞUL 3): Pages altına elle yazılan
//   yanıt modelleri DONDURULDU (denetim.sh "Pages içinde istemci record tanımı"
//   sayacı, tavan 129). Önceki iki turda bu sınırın delinmesi çalışma zamanı tip
//   hatası üretti (StokYaniti alan adı, sonra FormatInt32).
//
// ⚠ TİP EŞLEŞMESİ SUNUCUYLA BİREBİRDİR (Modules/Banka/EkstreModulu.cs):
//   tutar / acilisBakiye / kapanisBakiye → decimal
//   satirNo / sayaçlar / limit / offset / toplam → int
//   tarih / olusturuldu → DateTime
//   Alan ADI camelCase JSON ile otomatik eşleşir.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Sahne tablosundaki TEK satır (GET /bankalar/ekstre/{id} · PATCH yanıtı).</summary>
public sealed record EkstreSatirVeri(
    string Id,
    int SatirNo,
    string? HamSatir,
    DateTime Tarih,
    string Yon,
    decimal Tutar,
    string? ParaBirimi,
    string? KarsiTarafAd,
    string? KarsiIban,
    string? BankaReferans,
    string? Aciklama,
    string Durum,
    string? HataMesaji,
    string? EslesenCariId,
    string? EslesenCariAd,
    string? EslesmeKaynagi,
    string? BankaHareketId);

/// <summary>PATCH /bankalar/ekstre/satir/{id} yanıt zarfı.</summary>
public sealed record EkstreSatirYaniti(EkstreSatirVeri? Satir);

/// <summary>
/// POST /bankalar/{id}/ekstre/yukle yanıtı.
/// <para>⚠ <c>UretilenHareketSayisi</c> yüklemede DAİMA 0'dır ve ekranda da
/// böyle yazar — "hiçbir şey yazılmadı" iddiası SUNUCUDAN gelir.</para>
/// </summary>
public sealed record EkstreYuklemeYaniti(
    string IthalatId,
    string Durum,
    string? DosyaAdi,
    int SatirSayisi,
    int YeniSayisi,
    int MukerrerSayisi,
    int HataliSayisi,
    int OnaylananSayisi,
    int UretilenHareketSayisi,
    int MaksOnaySatir);

/// <summary>GET /bankalar/ekstre/{id} — başlık + SAYFALI satır listesi (TB-19).</summary>
public sealed record EkstreDetayYaniti(
    string IthalatId,
    string BankaHesapId,
    string? HesapAdi,
    string? ParaBirimi,
    string? DosyaAdi,
    string? Format,
    string Durum,
    int SatirSayisi,
    int YeniSayisi,
    int MukerrerSayisi,
    int HataliSayisi,
    int OnaylananSayisi,
    decimal? AcilisBakiye,
    decimal? KapanisBakiye,
    DateTime Olusturuldu,
    int MaksOnaySatir,
    int Limit,
    int Offset,
    int Toplam,
    List<EkstreSatirVeri>? Satirlar)
{
    public List<EkstreSatirVeri> SatirListesi => Satirlar ?? [];
}

/// <summary>POST /onayla — satır bazında hata.</summary>
public sealed record EkstreOnayHatasi(int SatirNo, string? Hata);

/// <summary>POST /bankalar/ekstre/{id}/onayla yanıtı.</summary>
public sealed record EkstreOnayYaniti(
    string IthalatId,
    int Secilen,
    int Basarili,
    int Basarisiz,
    int UretilenHareketSayisi,
    List<EkstreOnayHatasi>? Hatalar,
    /// <summary>P-38 · B-18 — cari seçilmediği için ONAYLANMAYAN satır sayısı.</summary>
    int CarisizAtlanan = 0,
    /// <summary>P-38 · B-18 — bilerek carisiz yazılan banka hareketi sayısı.</summary>
    int CarisizUretilen = 0)
{
    public List<EkstreOnayHatasi> HataListesi => Hatalar ?? [];
}

/// <summary>GET /oneriler — tek öneri. ⚠ ASLA otomatik uygulanmaz (K4-OK-5).</summary>
public sealed record EkstreOneri(string CariId, string Ad, decimal Skor);

/// <summary>GET /bankalar/ekstre/satir/{id}/oneriler yanıtı.</summary>
public sealed record EkstreOneriYaniti(
    string SatirId,
    string? KarsiTarafAd,
    decimal Esik,
    int TarananCari,
    int OtomatikUygulanan,
    List<EkstreOneri>? Oneriler)
{
    public List<EkstreOneri> OneriListesi => Oneriler ?? [];
}

/// <summary>
/// GET /cari?limit=200 — eşleştirme ekranındaki cari seçici.
/// ⚠ Pages altına yeni record yazılmaması için burada durur (S-2).
/// </summary>
public sealed record EkstreCariSecenek(string Id, string Ad);
