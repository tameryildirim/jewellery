using System.Globalization;

namespace Kuyumcu.Core.Domain.Girdi;

// ═══════════════════════════════════════════════════════════════════════════
// K8–K8e — TARİH PENCERESİ (kurucu 08.10: "tarih elle yazılınca otomatik biçim
// 08102026 → 08.10.2026, mantıksız yıl (3000/1900) yazılamaz").
//   K8  ortak     : gg.aa.yyyy; yıl 1900..2100 dışı HİÇ yazılamaz (İŞ KURALI).
//   K8a İşlem     : bugün dâhil, ileri tarih yok; geri en çok 1 yıl (İŞ KURALI,
//                   öneri; kapanmış güne yazmayı "gün kilidi" ayrıca keser).
//   K8b Doğum     : gelecek olamaz; en çok 120 yıl önce; en az 18 yıl önce.
//                   BİRİNCİL — 4721 s. TMK md.11 "Erginlik onsekiz yaşın
//                   doldurulmasıyla başlar" (https://www.mevzuat.gov.tr/MevzuatMetin/1.5.4721.pdf);
//                   120 yıl alt sınırı İŞ KURALI.
//   K8c Vade      : bugün ya da sonrası, en çok 10 yıl (İŞ KURALI; türe özel daha
//                   dar üst sınır çağıranın EnGec parametresiyle verilir).
//   K8d Keşide    : ⚠ TEYİTSİZ — ileri tarihli çek (5941 s. Çek Kanunu) birincil
//                   metinden okunmadı → BUGÜNKÜ davranış korunur (yalnız ortak K8
//                   penceresi). Mali müşavir/hukuk teyidi gerekir.
//   K8e Rapor     : süzgeç aralığı; yıl 2000..bugün+1, bas ≤ bit, en çok 5 yıl.
//   K8f KisiDogum : (PAKET GOC-B tur 2) müşteri/MASAK kimlik kaydı doğum tarihi — gelecek
//                   olamaz, en çok 120 yıl önce; 18 yaş ENGELİ YOK. TMK md.11 erginliği
//                   tanımlar, küçüğün müşteri/kimlik kaydını YASAKLAMAZ → mevzuat kuralı
//                   gereği BUGÜNKÜ (paket öncesi) davranış korunur; 18 altı yalnız ekranda
//                   UYARI (ResitDegil). Engele çevirmek kurucu/hukuk kararıdır (TEYİTSİZ).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Tarih alanının anlamı (pencereyi belirler).</summary>
public enum TarihTuru { Serbest, Islem, Dogum, Vade, Keside, Rapor, KisiDogum }

/// <summary>Tarih kuralının tek kaynağı.</summary>
public static class TarihPenceresi
{
    public const int EnErkenYil = 1900;
    public const int EnGecYil = 2100;
    public const int IslemGeriYil = 1;
    public const int ResitYas = 18;
    public const int DogumEnCokYil = 120;
    public const int VadeEnCokYil = 10;
    public const int RaporEnErkenYil = 2000;
    public const int AralikEnCokYil = 5;

    /// <summary>Ekranda ve elle yazımda kullanılan TEK biçim.</summary>
    public const string Bicim = "dd.MM.yyyy";

    /// <summary>Türün [en erken, en geç] penceresi (işletme günü <paramref name="bugun"/>).</summary>
    public static (DateOnly EnErken, DateOnly EnGec) Pencere(TarihTuru tur, DateOnly bugun) => tur switch
    {
        TarihTuru.Islem => (bugun.AddYears(-IslemGeriYil), bugun),
        TarihTuru.Dogum => (bugun.AddYears(-DogumEnCokYil), bugun.AddYears(-ResitYas)),
        TarihTuru.KisiDogum => (bugun.AddYears(-DogumEnCokYil), bugun),
        TarihTuru.Vade => (bugun, bugun.AddYears(VadeEnCokYil)),
        TarihTuru.Rapor => (new DateOnly(RaporEnErkenYil, 1, 1), new DateOnly(bugun.Year + 1, 12, 31)),
        _ => (new DateOnly(EnErkenYil, 1, 1), new DateOnly(EnGecYil, 12, 31)),
    };

    /// <summary>Tür penceresi ile çağıranın ek sınırlarının kesişimi.</summary>
    public static (DateOnly EnErken, DateOnly EnGec) EtkinPencere(TarihTuru tur, DateOnly bugun, DateOnly? enErken, DateOnly? enGec)
    {
        var (e, g) = Pencere(tur, bugun);
        if (enErken is { } x && x > e) e = x;
        if (enGec is { } y && y < g) g = y;
        return (e, g);
    }

    /// <summary>Tarih penceresi denetimi. null → null (zorunluluk çağıranındır).</summary>
    public static GirdiHatasi? Denetle(DateOnly? t, TarihTuru tur, DateOnly bugun, DateOnly? enErken = null, DateOnly? enGec = null)
    {
        if (t is not { } d) return null;
        var (e, g) = EtkinPencere(tur, bugun, enErken, enGec);
        if (d < e)
            return tur switch
            {
                TarihTuru.Islem => new GirdiHatasi(GirdiKodu.TarihCokEski, Gun: e),
                TarihTuru.Dogum or TarihTuru.KisiDogum => new GirdiHatasi(GirdiKodu.DogumCokEski, DogumEnCokYil),
                _ => new GirdiHatasi(GirdiKodu.TarihErken, Gun: e),
            };
        if (d > g)
            return tur switch
            {
                TarihTuru.KisiDogum when d > bugun => new GirdiHatasi(GirdiKodu.DogumGelecek),
                // Çağıranın üst sınırı BUGÜN ise (EnGec = bugün) sebep "ileri tarih"tir.
                not TarihTuru.Dogum when g == bugun && d > bugun => new GirdiHatasi(GirdiKodu.TarihIleri),
                TarihTuru.Islem when d > bugun => new GirdiHatasi(GirdiKodu.TarihIleri),
                TarihTuru.Dogum when d > bugun => new GirdiHatasi(GirdiKodu.DogumGelecek),
                TarihTuru.Dogum => new GirdiHatasi(GirdiKodu.DogumResit, ResitYas),
                _ => new GirdiHatasi(GirdiKodu.TarihGec, Gun: g),
            };
        return null;
    }

    /// <summary>
    /// K8f yardımcı (PAKET GOC-B tur 2): doğum tarihi geçerli ama kişi 18 yaşından küçük mü?
    /// Yalnız ekran UYARISI içindir — kaydı ENGELLEMEZ (bkz. üst not K8f).
    /// </summary>
    public static bool ResitDegil(DateOnly? dogum, DateOnly bugun) =>
        dogum is { } d && d <= bugun && d > bugun.AddYears(-ResitYas);

    /// <summary>Aralık denetimi (K8e): bas ≤ bit, en çok <paramref name="enCokYil"/> yıl.</summary>
    public static GirdiHatasi? AralikDenetle(DateOnly? bas, DateOnly? bit, int enCokYil = AralikEnCokYil)
    {
        if (bas is not { } b || bit is not { } s) return null;
        if (b > s) return new(GirdiKodu.AralikTers);
        if (s > b.AddYears(enCokYil)) return new(GirdiKodu.AralikUzun, enCokYil);
        return null;
    }

    /// <summary>
    /// Kullanıcı metnini çözer: "gg.aa.yyyy", bitişik "ggaayyyy" ve makine biçimi
    /// "yyyy-MM-dd" (yapıştırma/geri uyum). Boş → (null, null). Eksik ya da bozuk
    /// yazım → TarihBicim; 31.02 gibi olmayan gün → TarihGecersiz.
    /// </summary>
    public static (GirdiHatasi? Hata, DateOnly? Deger) Coz(string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham)) return (null, null);
        var t = ham.Trim();
        int gun, ay, yil;
        if (t.Length == 10 && t[4] == '-' && t[7] == '-')
        {
            if (!int.TryParse(t[..4], NumberStyles.None, CultureInfo.InvariantCulture, out yil)
                || !int.TryParse(t[5..7], NumberStyles.None, CultureInfo.InvariantCulture, out ay)
                || !int.TryParse(t[8..], NumberStyles.None, CultureInfo.InvariantCulture, out gun))
                return (new(GirdiKodu.TarihBicim), null);
        }
        else
        {
            var r = KimlikNoDogrulayici.Rakamlar(t);
            foreach (var c in t)
                if (!(c is >= '0' and <= '9' || c is '.' or '/' or '-' or ' ')) return (new(GirdiKodu.TarihBicim), null);
            if (r.Length != 8) return (new(GirdiKodu.TarihBicim), null);
            gun = (r[0] - '0') * 10 + (r[1] - '0');
            ay = (r[2] - '0') * 10 + (r[3] - '0');
            yil = int.Parse(r[4..], CultureInfo.InvariantCulture);
        }
        if (ay is < 1 or > 12 || gun < 1 || yil < 1 || gun > DateTime.DaysInMonth(yil, ay))
            return (new(GirdiKodu.TarihGecersiz), null);
        return (null, new DateOnly(yil, ay, gun));
    }

    /// <summary>Ekran biçimi (gg.aa.yyyy) — dil bağımsız tek maske.</summary>
    public static string Yaz(DateOnly d) => d.ToString(Bicim, CultureInfo.InvariantCulture);
}
