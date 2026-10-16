// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/Kasalar.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.

namespace Kuyumcu.Contracts.Sayfalar.Kasalar;
// ── veri modelleri (sayfaya özel, JSON camelCase otomatik eşleşir) ──
public sealed record KasaVeri(string Id, string Ad, string ParaBirimi, decimal Bakiye, bool Aktif, string? SubeId);
// UX-1-C2: hareketId defter bağıdır — storno YALNIZ bu bağ varken üretilebilir.
// PAKET S-3b — alan adı VE tipi sunucuyla doğrulandı:
//   Data/Entities.cs KasaHareket: Tutar decimal · Aciklama string? · StornoKaynakId string?
// PAKET P-19 (Y-11) — sapma S1 kapanışı: virman BACAĞI ekranda tanınır.
//   karsiBacakId dolu satır = virmanın yarısıdır; TEK BAŞINA stornolanamaz
//   (sunucu kapısı: StornoServisi.KasaHareketiStorno). Ekran bu satırın storno
//   düğmesini kilitler ve DOĞRU YOLU (/gider → Virman sekmesi) gösterir — UX-S25/S30.
//   Alan adı VE tipi sunucuyla doğrulandı (Data/Entities.cs:920 KarsiBacakId string?).
public sealed record HareketVeri(string Id, decimal Tutar, string OdemeTuru, DateTime Tarih,
                                  string? HareketId, string? Aciklama, string? StornoKaynakId,
                                  string? KarsiBacakId = null);
public sealed record HareketYaniti(List<HareketVeri> Hareketler);

// PAKET PARA-DEMO (madde 1) — GET /kasalar/{id}/gun-kapanisi/onizleme yanıtı. TEK TİP:
// Kasalar.razor (Günü Kapat) ve KasaRaporu.razor (gün sonu sayımı) aynı record'u okur
// (istemci sayfalarına yeni record yazılmaz — denetim P-10). Sayım NAKİT defterle karşılaştırılır;
// kasaya yazılmış kart ve havale ayrı satırda "sayıma girmez".
/// <summary>Gün sonu sayımı önizlemesi: toplam = nakit defteri + kart + havale.</summary>
public sealed record GunKapanisiOnizleme(decimal ToplamBakiye, decimal KartToplam, decimal HavaleToplam, decimal NakitDefter);

// PAKET PARA-DEMO-2 (madde 4) — kasa kartı / "Kasada ne var?" alt satırı: "Nakit X · Kart/Havale Y".
/// <summary>Bir kasanın nakit / kart / havale ayrımı (toplam = nakit + kart + havale; sunucu KasaNakitAyrimi).</summary>
/// <param name="BugunKapali">PAKET SON-KALANLAR (hasım YÜKSEK, B1440__14): kasanın BUGÜNKÜ (yerel gün) kapanışı var ve
/// yeniden açılmamış — kart "gün kapandı" rozeti gösterir, "+ Hareket" ve "Günü kapat" kilitlenir (sebep yazılı).
/// Sunucu kilidi <c>GunKapanisiKapisi</c>dır; bu alan yalnız ekranın önceden söylemesi içindir.</param>
public sealed record KasaNakitAyrimiOzeti(string KasaId, string ParaBirimi, decimal Toplam, decimal Nakit, decimal Kart, decimal Havale,
    bool BugunKapali = false);
