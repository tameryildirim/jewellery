namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT (09.10) — metinler (TR + EN). Bant iletisinin KENDİSİ
// GirdiSozlugu'ndadır (FiyatMutlakDisi / IslemFiyatBantDisi); onay yolu metinleri
// KurBantSozlugu'ndadır (aynı kalıp, aynı cümle). Burada yalnız PATCH /kullanicilar/ben
// alan beyaz listesinin reddi var. Konumsal kayıt: eksik dil = DERLEME HATASI.
// ═══════════════════════════════════════════════════════════════════════════

public sealed record IslemBantMetinleri(
    string YalnizModDegisir);   // "kullanici" modülü olmayan rol PATCH /ben'de mod dışı alan gönderdi (403)

public static class IslemBantSozlugu
{
    public static readonly IslemBantMetinleri Tr = new(
        YalnizModDegisir: "Bu bilgiyi değiştirme yetkiniz yok. Kendiniz için yalnız ekran görünümünü (basit / profesyonel) değiştirebilirsiniz.");

    public static readonly IslemBantMetinleri En = new(
        YalnizModDegisir: "You are not allowed to change this information. For yourself you can only change the screen view (simple / professional).");

    public static IslemBantMetinleri Al(string dil) => dil == "en" ? En : Tr;
}
