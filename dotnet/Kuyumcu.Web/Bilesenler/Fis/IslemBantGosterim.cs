using Kuyumcu.Core.Domain;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Bilesenler.Fis;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET ISLEM-BANT (09.10) — işlem içi has gram fiyatı bandının EKRAN gösterimi.
// Sunucuyla AYNI Core kuralı (FiyatBandi.IslemDenetle) ve AYNI cümle (GirdiSozlugu.Mesaj):
// alan altında, POST'tan önce. Referans = canlı kur (sunucunun referansı da son geçerli
// kur kaydıdır). Bileşen istek ATMAZ; yalnız alan doğrulama gösterimidir.
// CanliKurServisi kayıtlı değilse (yalın bUnit kurulumu) referans yoktur → yalnız mutlak sınır.
// ═══════════════════════════════════════════════════════════════════════════
public static class IslemBantGosterim
{
    /// <summary>Canlı has fiyatı (yoksa null). Hizmet kayıtlı değilse de null — ekran çökmez.</summary>
    public static decimal? Referans(IServiceProvider hizmetler) =>
        hizmetler.GetService(typeof(CanliKurServisi)) is CanliKurServisi c && c.Son is { AltinHasFiyati: > 0m } s
            ? s.AltinHasFiyati : null;

    /// <summary>Bant dışıysa sunucunun 400 iletisiyle AYNI cümle; içindeyse null.</summary>
    public static string? Metin(decimal? fiyat, IslemFiyatBaglami baglam, decimal? referansHas, string? ayarId, string dil) =>
        MadenMetni(fiyat, baglam, referansHas, Ayarlar.MadenBul(ayarId), dil);

    /// <summary>Ayar değil maden seçilen ekran (hurda satışı) için aynı gösterim.</summary>
    public static string? MadenMetni(decimal? fiyat, IslemFiyatBaglami baglam, decimal? referansHas, string? maden, string dil) =>
        FiyatBandi.IslemDenetle(fiyat, baglam, referansHas, maden) is { } h
            ? GirdiSozlugu.Mesaj(h, dil) : null;

    // ═══ PAKET ISLEM-BANT-2 (10.10) — Kaydet kilidi + tutar gösterimi (dört ekranda AYNI kural) ═══

    /// <summary>Oturumdaki rol (OturumServisi kayıtlı değilse null — yalın bUnit kurulumu).</summary>
    public static string? Rol(IServiceProvider hizmetler) =>
        (hizmetler.GetService(typeof(OturumServisi)) as OturumServisi)?.Kullanici?.Rol;

    /// <summary>Bant dışı fiyatı onaylayabilen rol (sunucu KurServisi.BantOnayYetkili ile aynı: SAHİP/MÜDÜR).</summary>
    public static bool OnayYetkili(string? rol) => rol is "SAHIP" or "MUDUR";

    /// <summary>
    /// Hurda alışı kaydedemeyen rol mü? Küme TEK KAYNAK (IslemRolleri.HurdaAlis = sunucu [RolGerekli]).
    /// Rol BİLİNMİYORSA (oturum yok) kilit çizilmez — son söz sunucunun; bilinen ve kümede olmayan rol kilitlenir.
    /// </summary>
    public static bool HurdaAlisKilitli(string? rol) =>
        !string.IsNullOrEmpty(rol) && !IslemRolleri.Icinde(IslemRolleri.HurdaAlis, rol);

    /// <summary>
    /// Fiyat EKRANDAKİ tutar/fark gösterimine girer mi? 0/eksi/boş HAYIR (GIRDI-AILESI-2 Tur=Pozitif ile aynı) —
    /// "-22,90 TL" bir daha görünmez. Mutlak sınır dışı (has 500–50.000) fiyatta tutar KUTUDAKİ değerle tutarlı
    /// görünür (alan altında sınır cümlesi, Kaydet kilitli); maden parametresi ileride gümüş bandı için ayrıldı.
    /// (Tur 1 denemesi mutlak dışında da gizliyordu: SATIS3/ZOISLEMA kuruş ve büyük sayı biçim testlerini
    /// kırdığı ve kilit zaten kaydı engellediği için geri alındı — rapor § 1.)
    /// </summary>
    public static bool FiyatHesabaGirer(decimal? fiyat, string? maden) => fiyat is > 0m;

    /// <summary>
    /// Kutunun ALTINDAKİ tutar satırı (hurda alış hesap satırı, Değişim ALDIM tutarı, eski altın "Fişe sayılan") çizilsin mi?
    /// 0/eksi/boş HAYIR; onayla bile aşılamayan MUTLAK sınır dışı (has 500–50.000) da HAYIR — tarayıcıda "-5" yazılınca maske
    /// eksiyi yazdırmaz, kutuda "5" kalır: alanın altında sınır cümlesi varken "22,90 ₺" tutarı görünmez (hakem KRİTİK).
    /// Model formülü ve fark/özet kartları DEĞİŞMEZ (kuruş ve büyük sayı biçim testleri o yolları sınar).
    /// </summary>
    public static bool TutarGosterilir(decimal? fiyat, string? maden) =>
        fiyat is > 0m && !(FiyatBandi.IslemDenetle(fiyat, IslemFiyatBaglami.HurdaAlis, null, maden) is { } h && !FiyatBandi.OnaylanabilirMi(h));

    /// <summary>Ayar kodlu ekranlar için (hurda alış, takas, eski altın).</summary>
    public static bool TutarGosterilirAyar(decimal? fiyat, string? ayarId) => TutarGosterilir(fiyat, Ayarlar.MadenBul(ayarId));

    /// <summary>Ayar kodlu ekranlar için (hurda alış, takas, eski altın).</summary>
    public static bool FiyatHesabaGirerAyar(decimal? fiyat, string? ayarId) => FiyatHesabaGirer(fiyat, Ayarlar.MadenBul(ayarId));

    /// <summary>
    /// Bant yüzünden Kaydet kilitli mi — sebep cümlesi ya da null. Takas, hurda alış, ramat satışı ve eski altınla
    /// ödemede AYNI sıra: mutlak dışı → alanın cümlesi · oran dışı + yetkisiz rol → yetki cümlesi · onay yok →
    /// "fiyatı düzeltin ya da onaylayın" · gerekçe 20 karakterden kısa → gerekçe cümlesi (sunucuyla aynı K30).
    /// </summary>
    public static string? KaydetKilidi(GirdiHatasi? h, IslemBantOnayDurumu durum, string? rol, string dil)
    {
        if (h is not { } x) return null;
        if (!FiyatBandi.OnaylanabilirMi(x)) return GirdiSozlugu.Mesaj(x, dil);
        if (!OnayYetkili(rol)) return KurBantSozlugu.Al(dil).YetkiYok;
        if (!durum.Onay) return string.Format(IslemBant2Sozlugu.Al(dil).BantOnayBekliyor, KurBantSozlugu.Al(dil).OnayEtiket);
        var n = durum.Gerekce.Trim().Length;
        return n < AlanSinirlari.GerekceEnAz
            ? GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.GerekceKisa, AlanSinirlari.GerekceEnAz, n), dil)
            : null;
    }
}

/// <summary>
/// PAKET ISLEM-BANT — "fiyat doğru, yine de kaydet" onayının ekran durumu (SAHİP/MÜDÜR).
/// Gövdeye YALNIZ onay işaretliyken gider; sunucu bant aşımı yoksa bu alanlara bakmaz.
/// </summary>
public sealed class IslemBantOnayDurumu
{
    public bool Onay { get; set; }
    public string Gerekce { get; set; } = "";

    /// <summary>Gövde alanı bantOnay: işaretliyse true, değilse null (alan gönderilmez sayılır).</summary>
    public bool? GovdeOnay => Onay ? true : null;

    /// <summary>Gövde alanı bantGerekce: yalnız onay işaretliyken.</summary>
    public string? GovdeGerekce => Onay ? Gerekce.Trim() : null;
}
