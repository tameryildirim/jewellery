// PAKET P-30 (kimlik R-4) — DEMO KİRACISI KISIT BEYANI SÖZLEŞMESİ.
//
// NİÇİN KODDA: BK-47 "hariç listesi yalnız belgede değil, demo kiracısının
// GİRİŞ EKRANINDA okunur" der. Giriş ekranı oturum AÇILMADAN çizilir; belgeyi
// (docs/HARIC-LISTESI.md) tarayıcıya okutacak bir yol yoktur. Bu yüzden kısıt
// satırları sunucunun bildiği TEK dizide durur ve anonim bir uçla ekrana iner.
//
// ⚠ İKİNCİ KOPYA DEĞİLDİR: docs/HARIC-LISTESI.md hâlâ TEK KAYNAKTIR; bu dizi
//   onun makine okunur yansımasıdır ve dotnet/denetim.sh'ın P-30 bloğundaki
//   DEMO_KISIT_AYRISMA sayacı iki tarafın H kodlarını her koşumda karşılaştırır
//   (ayrışma = denetim KIRMIZI). Yani metin iki yerde yaşayamaz, kod + belge
//   birlikte değişmek zorundadır.
//
// ⚠ Durum kodları (HARIC / KAPSAM_DISI / SURUM_1_1) EKRANA HAM BASILMAZ
//   (UX-S31): Giris.razor bunları Sozlukler'den çevirir.

namespace Kuyumcu.Contracts.Kisit;

/// <summary>1.0'da bulunmayan bir yetenek — docs/HARIC-LISTESI.md'deki bir H satırı.</summary>
/// <param name="Kod">Belgedeki satır kimliği (H-1 … H-8).</param>
/// <param name="Baslik">Türkçe başlık (ekran dili TR).</param>
/// <param name="BaslikEn">İngilizce başlık (ekran dili EN) — çeviri ikinci dizide yaşamaz.</param>
/// <param name="Durum">HARIC · KAPSAM_DISI · SURUM_1_1 (ham kod; ekranda çevrilir).</param>
public sealed record KisitSatiri(string Kod, string Baslik, string BaslikEn, string Durum);

/// <summary>GET /api/v1/genel/kisitlar yanıtı.</summary>
/// <param name="Kaynak">Kısıt beyanının tek kaynağı (belge yolu) — ekranda gösterilir.</param>
/// <param name="Toplam">Satır sayısı (istemci sessizce kırpmasın diye açıkça döner — UX-S29).</param>
/// <param name="Kisitlar">H satırları.</param>
public sealed record KisitBeyaniYaniti(string Kaynak, int Toplam, IReadOnlyList<KisitSatiri> Kisitlar);

/// <summary>Kısıt beyanının makine okunur hâli — TEK DİZİ (UX-S31).</summary>
public static class KisitBeyani
{
    public const string DurumHaric = "HARIC";
    public const string DurumKapsamDisi = "KAPSAM_DISI";
    public const string DurumSurum11 = "SURUM_1_1";

    /// <summary>Kısıt beyanının tek kaynağı — ekranda da yazılır.</summary>
    public const string Kaynak = "docs/HARIC-LISTESI.md";

    public static readonly IReadOnlyList<KisitSatiri> Liste =
    [
        new("H-1", "e-Fatura / e-Arşiv / e-İrsaliye gönderimi (GİB'e iletim)",
            "e-Invoice / e-Archive / e-Waybill transmission (to the tax authority)", DurumHaric),
        new("H-2", "ÖKC / yazarkasa / Z raporu",
            "Fiscal cash register / Z report", DurumHaric),
        new("H-3", "SMS / WhatsApp gönderimi",
            "SMS / WhatsApp delivery", DurumHaric),
        new("H-4", "Banka canlı API'si (açık bankacılık)",
            "Live bank API (open banking)", DurumHaric),
        new("H-5", "İmalat / atölye",
            "Manufacturing / workshop", DurumKapsamDisi),
        new("H-6", "RFID sayım donanımı",
            "RFID stock-count hardware", DurumHaric),
        new("H-7", "Emanet parça kimliği (kalıcı emanet envanteri)",
            "Consignment item identity (persistent consignment inventory)", DurumSurum11),
        new("H-8", "Seri no / bandrol (KMTS)",
            "Serial number / banderole (KMTS)", DurumSurum11),
    ];
}
