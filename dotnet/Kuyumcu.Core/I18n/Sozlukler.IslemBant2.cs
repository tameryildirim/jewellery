namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT-2 (10.10) — Kaydet kilidinin sebep cümleleri (TR + EN).
// Konumsal kayıt: eksik dil = DERLEME HATASI. Rol adları KesifP1Sozlugu.RolListesi'nden
// (ham kod ekrana basılmaz — UX-S31).
// ═══════════════════════════════════════════════════════════════════════════

public sealed record IslemBant2Metinleri(
    string HurdaAlisYetkiYok,   // {0} = rol listesi ("işletme sahibi ya da müdür ya da muhasebe")
    string BantOnayBekliyor);   // {0} = onay kutusunun etiketi (KurBantSozlugu.OnayEtiket)

public static class IslemBant2Sozlugu
{
    public static readonly IslemBant2Metinleri Tr = new(
        HurdaAlisYetkiYok: "Eski altın alışını yalnız {0} kaydedebilir. Bu işlemi onlardan birine yaptırın.",
        BantOnayBekliyor: "Fiyatı düzeltin ya da \"{0}\" kutusunu işaretleyip gerekçe yazın.");

    public static readonly IslemBant2Metinleri En = new(
        HurdaAlisYetkiYok: "Only {0} can record a scrap gold purchase. Ask one of them to do it.",
        BantOnayBekliyor: "Fix the price or tick \"{0}\" and write a reason.");

    public static IslemBant2Metinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>Hurda alış yetki cümlesi — rol listesi IslemRolleri.HurdaAlis'ten (tek kaynak).</summary>
    public static string HurdaAlisYetkiMetni(string dil) =>
        string.Format(Al(dil).HurdaAlisYetkiYok, KesifP1Sozlugu.RolListesi(Kuyumcu.Core.Domain.IslemRolleri.HurdaAlis, dil));
}
