// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Emanet.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Emanet;
// Tasarım hakemi 28.09 (EMANET-1): telefon müşteri seçicide ikinci satır ve arama anahtarı
// (GET /cari zaten `telefon` döndürüyordu; alan eklendi, eski yanıtta yoksa null).
public sealed record CariOzet(string Id, string Ad, string? Telefon = null);
// UX-1-C2: stornoKaynakId defterden gelir — ters kaydın hangi satırı
// hükümsüz kıldığını EKRAN da bilir; fisId çok bacaklı fişi işaretler.
public sealed record HareketVeri(string Id, string Tur, DateTime Tarih,
                                  decimal HasGrami, string? Aciklama,
                                  string? StornoKaynakId, string? FisId,
                                  // PAKET S3-5: emanetin madeni (defterdeki `maden` kolonu, sunucu
                                  // zaten gönderiyordu). Ayrıntı çekmecesinde KİLİTLİ alan olarak
                                  // yazılır. Eski yanıtta yoksa null → ekranda "—" (uydurulmaz).
                                  string? Maden = null,
                                  // P-DIL-BORC-KASA (Y18): bırakıldığı gibi iz (defter kolonları; eski satırda null).
                                  decimal? EmanetGram = null, decimal? EmanetTas = null, string? EmanetAyarId = null);
// ⚠ PAKET 20 — SÖZLEŞME DARALMASI: MevcutHasGramBakiye artık YALNIZ ALTIN
// emanetidir. Eskiden bütün madenlerin gramını topluyordu; 100 g gümüş ile
// 100 g altın aynı rakama karışıyordu. Ekran o yüzden tek rakam yerine
// MADEN BAZLI SATIRLAR gösterir ve çıkışta maden seçtirir (K20-9).
public sealed record EmanetYaniti(string CariId, decimal MevcutHasGramBakiye,
                                   List<HareketVeri>? Hareketler,
                                   List<Kuyumcu.Core.Sozlesmeler.MadenBakiyeSatiri>? MadenBakiyeleri = null,
                                   bool SinirDegdi = false, int Limit = 200,
                                   // P-DIL-BORC-KASA (Y18): altın emanetinin bırakıldığı gibi kalanı; null = bilinmiyor.
                                   List<Kuyumcu.Core.Domain.EmanetAyarSatiri>? BirakildigiGibi = null);
public sealed record KurVeri(string Id, decimal AltinHasFiyati);
