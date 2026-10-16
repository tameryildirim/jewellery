using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;

namespace Kuyumcu.Web.Pages;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT-2 (10.10) — tasarım hakemi bulguları (ISLEM-BANT kapanış devri):
//   • YÜKSEK: hurda alışı TEZGAHTAR'a açılıyor, Kaydet etkin, sunucu 403'ü SONDA söylüyordu.
//     Artık rol sunucunun [RolGerekli] kümesinde değilse (IslemRolleri.HurdaAlis — TEK KAYNAK)
//     Kaydet kilitli ve sebebi formun başındaki cümlenin AYNISI → canlıda 403 hiç tetiklenmez.
//   • YÜKSEK: bant dışı has fiyatında Kaydet takas ve hurda alışta AYNI kuralla kilitli
//     (IslemBantGosterim.KaydetKilidi): mutlak dışı · yetkisiz · onaysız · gerekçe kısa.
//     Eski altınla ödeme (karma) aynı kuralı AltinBacagiPaneli eksik listesinden alır.
// Islemler.razor 1.400 satır tavanında: buraya taşındı, razor'da yalnız MEVCUT iki satıra birer koşul eklendi.
// ═══════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>ISLEM-BANT-2 Kaydet kilidi (sebep cümlesi) ya da null. Gonderilebilir ve KaydetKilitSebebi AYNI değeri okur.</summary>
    private string? IslemBant2Kilidi => tur switch
    {
        "HURDA_ALIS" => IslemBantGosterim.HurdaAlisKilitli(Oturum.Kullanici?.Rol)
            ? IslemBant2Sozlugu.HurdaAlisYetkiMetni(Dil.Dil)
            : IslemBantGosterim.KaydetKilidi(
                FiyatBandi.IslemDenetle(hurda.Fiyat, IslemFiyatBaglami.HurdaAlis, IslemBantGosterim.Referans(Hizmetler),
                    Kuyumcu.Core.Domain.Ayarlar.MadenBul(hurda.Ayar)),
                hurda.Bant, Oturum.Kullanici?.Rol, Dil.Dil),
        "TAKAS" => IslemBantGosterim.KaydetKilidi(
            FiyatBandi.IslemDenetle(takas.EskiFiyat, IslemFiyatBaglami.Takas, IslemBantGosterim.Referans(Hizmetler),
                Kuyumcu.Core.Domain.Ayarlar.MadenBul(takas.EskiAyar)),
            takas.Bant, Oturum.Kullanici?.Rol, Dil.Dil),
        _ => null,
    };
}
