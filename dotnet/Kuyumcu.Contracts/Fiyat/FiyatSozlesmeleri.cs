// P-03 SÖZLEŞME-1 — SUNUCU ⇄ İSTEMCİ ORTAK TİP (R-10).
// Kaynak: Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs (sunucu sürümü kanoniktir) +
//         Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor (aynı şeklin ikinci kopyası).
// Bu dört tip iki yerde AYRI AYRI tanımlıydı: aynı adın iki tanımı derleme hatası
// vermez, alan/tip kayarsa çalışma zamanında sessizce null döner (QA turu 2 dersi).
// Artık tek tanım vardır; sunucu ve ekran birebir aynı türü kullanır.
namespace Kuyumcu.Contracts.Fiyat;

/// <summary>Bu liste × ürün kesişimindeki geçersiz kılmanın ekrana giden özeti.</summary>
public sealed record GecersizKilmaOzeti(
    decimal? SabitFiyat, decimal? HasMarjYuzde, decimal? IscilikCarpanYuzde, string? Not, bool Aktif);

public sealed record TopluSatir(string UrunId, string Kod, string Ad, decimal EskiFiyat, decimal YeniFiyat, decimal Fark);

public sealed record TopluAtlanan(string UrunId, string Kod, string Sebep);

/// <summary>Toplu uygulamanın etki özeti; <c>Uygulandi=false</c> iken hiçbir satır yazılmamıştır.</summary>
public sealed record TopluSonuc(
    bool Uygulandi, int Hedeflenen, int Uygulanacak, decimal ToplamFark, string ParmakIzi,
    IReadOnlyList<TopluSatir> Ornekler, IReadOnlyList<TopluAtlanan> Atlandi);
