using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Api.Common.Dogrulama;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET GOC-A — SERVİS YOLU GİRDİ KAPISI.
// DataAnnotations (GirdiOznitelikleri) yalnız controller yolunda koşar (DEĞİŞMEZ
// kuralı); servis içinden çağrılan yolda (ekstre onayı, toplu işlem, testlerin
// servis çağrısı) AYNI Core kuralı burada elle çağrılır. Mesaj GirdiSozlugu.Tr —
// ekranın alan altı metniyle AYNI cümle; alan adı gövdeye 'alanlar' olarak gider.
// Sessiz yuvarlama YOK: fazla ondalık 400'dür (envanter ortak kök 3).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Core girdi kuralını servis yolunda uygular; bozuksa 400 + sözlük metni + alan.</summary>
public static class GirdiKapisi
{
    /// <summary>Kural sonucu doluysa 400 fırlatır. <paramref name="onek"/> verilirse metnin başına "onek: " eklenir (kalem sırası).</summary>
    public static void Denetle(GirdiHatasi? hata, string alan, string? onek = null)
    {
        if (hata is not { } h) return;
        var mesaj = GirdiSozlugu.Mesaj(h, Kuyumcu.Core.I18n.IstekDili.Gecerli);
        throw new BadRequestException(onek is { Length: > 0 } ? $"{onek}: {mesaj}" : mesaj).AlanIle(alan);
    }

    /// <summary>K9 — tutar (Pozitif / SifirOlabilir / Isaretli): eksi, sıfır, 2'den fazla ondalık, decimal(15,2) tavanı.</summary>
    public static decimal Tutar(decimal d, string alan, TutarTuru tur = TutarTuru.Pozitif, string? onek = null)
    {
        Denetle(TutarKurali.Denetle(d, tur), alan, onek);
        return d;
    }

    /// <summary>K9 — boş (null) geçer; dolu ise <see cref="Tutar(decimal,string,TutarTuru,string?)"/>.</summary>
    public static decimal? Tutar(decimal? d, string alan, TutarTuru tur = TutarTuru.Pozitif, string? onek = null)
    {
        if (d is { } x) Tutar(x, alan, tur, onek);
        return d;
    }

    /// <summary>K10 — gram: &gt; 0, en çok 3 ondalık, decimal(12,3) tavanı.</summary>
    public static decimal Gram(decimal d, string alan, string? onek = null)
    {
        Denetle(AgirlikKurali.Gram(d), alan, onek);
        return d;
    }

    /// <summary>K11 — has: &gt; 0, en çok 6 ondalık, decimal(15,6) tavanı.</summary>
    public static decimal Has(decimal d, string alan, string? onek = null)
    {
        Denetle(AgirlikKurali.Has(d), alan, onek);
        return d;
    }

    /// <summary>
    /// K11 + K9b — işaretli has (tahlil farkı): en çok 6 ondalık, mutlak değer tavanı. Sıfır reddi ÇAĞIRANINDIR
    /// (sözlükteki "0'dan büyük olmalı" cümlesi eksi değeri de kabul eden alanda yanıltıcı olurdu).
    /// </summary>
    public static decimal IsaretliHas(decimal d, string alan, string? onek = null)
    {
        Denetle(SayiKurali.Ondalik(d, AgirlikKurali.HasOndalik) ?? SayiKurali.Tavan(d, AgirlikKurali.HasTavan), alan, onek);
        return d;
    }

    /// <summary>K14 — adet: tam sayı ≥ 1.</summary>
    public static decimal Adet(decimal d, string alan, string? onek = null)
    {
        Denetle(AdetKurali.Denetle(d), alan, onek);
        return d;
    }

    /// <summary>K16 — kur: &gt; 0, en çok <paramref name="ondalik"/> ondalık (kolon), tavan.</summary>
    public static decimal? Kur(decimal? d, string alan, int ondalik = KurKurali.OndalikHane)
    {
        if (d is not { } x) return d;
        Denetle(KurKurali.Denetle(x) ?? SayiKurali.Ondalik(x, ondalik), alan);
        return d;
    }

    /// <summary>K8a — işlem tarihi: bugün dahil ileri yok, en çok 1 yıl geri (TarihPenceresi.Islem).</summary>
    public static void IslemTarihi(DateOnly? gun, string alan)
    {
        if (gun is { } g) Denetle(TarihPenceresi.Denetle(g, TarihTuru.Islem, Zaman.YerelBugun(DateTime.UtcNow)), alan);
    }

    /// <summary>K8e — tarih aralığı: başlangıç ≤ bitiş, en çok 5 yıl.</summary>
    public static void Aralik(DateOnly? bas, DateOnly? bit, string alan = "bitis") =>
        Denetle(TarihPenceresi.AralikDenetle(bas, bit), alan);

    /// <summary>K30 — gerekçe: kırpılmış en az 20; üst sınır kolon (verilmezse 1000).</summary>
    public static string Gerekce(string? s, string alan, int enCok = AlanSinirlari.GerekceEnCok)
    {
        var t = (s ?? "").Trim();
        if (t.Length < AlanSinirlari.GerekceEnAz)
            Denetle(new GirdiHatasi(GirdiKodu.GerekceKisa, AlanSinirlari.GerekceEnAz, t.Length), alan);
        if (t.Length > enCok)
            Denetle(new GirdiHatasi(GirdiKodu.GerekceUzun, enCok, t.Length), alan);
        return t;
    }

    /// <summary>Metin uzunluğu (kırpılmış): fazlası SESSİZCE KESİLMEZ — 400 (UX-S29).</summary>
    // GOC-A tur 2 (hasım 08.10): ileti sözlükten — ekranın SatirIciDuzenle EnFazla iletisiyle AYNI anahtar
    // (GirdiKodu.AdUzun "En çok {0} karakter yazılır."); çıplak Türkçe metin ve sabit etiket yok.
    public static string? Uzunluk(string? s, int enCok, string alan)
    {
        if (s is null) return null;
        var t = s.Trim();
        if (t.Length > enCok) Denetle(new GirdiHatasi(GirdiKodu.AdUzun, enCok), alan);
        return t;
    }

    /// <summary>Eski imza (doğrulayıcı testleri çağırır) — etiket artık iletiye GİRMEZ; ileti sözlükten.</summary>
    public static string? Uzunluk(string? s, int enCok, string alan, string etiket) => Uzunluk(s, enCok, alan);
}
