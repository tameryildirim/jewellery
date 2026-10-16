// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/UrunAgaci.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using Kuyumcu.Core.Sozlesmeler;

namespace Kuyumcu.Contracts.Sayfalar.UrunAgaci;
/// <summary>
/// Kategori ağacı düğümü.
///
/// ⚠ <c>Altlar</c> NULL OLABİLİR: yaprak düğümde alan hiç gönderilmeyebilir
/// ve JSON'dan null gelir. Null gelince ağaç yürüyüşü NullReference atıp
/// EKRANIN TAMAMINI çökertiyordu — kategori isteğe bağlı bir yardımcıyken
/// ürün listesi bile açılmıyordu. Bu yüzden her erişim <c>Cocuklar</c>
/// üzerinden yapılır.
/// </summary>
public sealed record KategoriDugum(string Id, string Ad, int Sira, bool Aktif,
                                    int UrunSayisi, List<KategoriDugum>? Altlar)
{
    public List<KategoriDugum> Cocuklar => Altlar ?? [];
}
public sealed record DuzKategori(string Id, string Etiket);
// ⚠ Yanıt şekilleri Kuyumcu.Core/Sozlesmeler/YerlesimSozlesmeleri.cs
//   dosyasındadır (S-2 / MÜHÜR 5 KOŞUL 3): Pages altına ikinci bir kopya
//   yazılmaz, alan adı sapması çalışma zamanında patlamasın.
public sealed record SubeOzet(string Id, string Ad);

// ── PAKET P-13 (B-40) — kategori pasife alma öncesi bağlı kayıt özeti ────────
// TEK TİP: sunucu (KategoriServisi.BagliKayitlar) ve ekran (UrunAgaci.razor)
// AYNI record'u kullanır (PLAN § 1 md.9 — ad VE tip sözleşmesi).
/// <summary>Kategoriye bağlı kayıt sayıları — pasife alma kapısının gerekçesi.</summary>
public sealed record KategoriBagliKayit(
    string KategoriId, bool Aktif, int AltKategoriSayisi, int UrunSayisi);
