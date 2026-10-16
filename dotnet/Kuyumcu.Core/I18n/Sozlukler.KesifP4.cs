namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P4 · ÜRÜN — açıklama alanının kendi etiketi (#40), ayar seçeneği
// biçimi (#79/#84/#85: "22 Ayar", milyem yalnız PRO'da parantez içinde).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır ve yalnız P8'indir (orkestratör
//   kuralı: yeni metin AYRI dosyaya). HUB'daki `Urun.Aciklama` SAYFA ALT
//   BAŞLIĞIDIR ("Stoktaki ürün kartları; …"); alan etiketi olarak okunuyordu.
// ⚠ Ayar ADI buraya YAZILMAZ: tek kaynak Kuyumcu.Core.Domain.Ayarlar (ekranda
//   KurKaynakAdi.AyarAdi). Burada yalnız milyemli biçimin kalıbı var.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>KESIF-P4 metinleri (bir dilde).</summary>
public sealed record KesifP4Metinleri(
    string AciklamaEtiketi,
    string AyarMilyemKalibi,
    string PariteEtiketi,
    string PariteIpucu,
    string DosyaSecilmedi);

public static class KesifP4Sozlugu
{
    public static readonly KesifP4Metinleri Tr = new(
        AciklamaEtiketi: "Açıklama",
        AyarMilyemKalibi: "{0} ({1}‰)",
        // KESIF-P4 tur 2 (#37): HUB'daki "Parite kodu" etiketi ve ham kodlu ipucu
        // ("ÇEYREK_YENİ, REŞAT…") yerine sade ad ve kodsuz ipucu.
        PariteEtiketi: "Altın cinsi",
        PariteIpucu: "Çeyrek, yarım, Reşat gibi sikke ve ziynetlerde fiyat bu cinsin kurundan gelir. Boş bırakılırsa fiyat gram ve işçilikten hesaplanır.",
        // KESIF-P4 tur 2 (#16): tarayıcının ham "Dosya seçilmedi" kontrolü gizlendi.
        DosyaSecilmedi: "Henüz dosya seçilmedi");

    public static readonly KesifP4Metinleri En = new(
        AciklamaEtiketi: "Description",
        AyarMilyemKalibi: "{0} ({1}‰)",
        PariteEtiketi: "Gold type",
        PariteIpucu: "For coins and bullion such as quarter, half or Reşat, the price comes from this type's rate. Leave empty to price by grams and labour.",
        DosyaSecilmedi: "No file selected yet");

    public static KesifP4Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
