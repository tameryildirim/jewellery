using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain.Girdi;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-C — ADET TAM SAYI KAPISI (envanter açık #6 · katalog K14).
//
// ADET birimli üründe miktar parça sayısıdır: 1,5 yüzük satılamaz, taşınamaz,
// rezerve edilemez, sayılamaz. GRAM birimli üründe miktar GRAMDIR (K14: "gramlı
// ürünlerde miktar gramdır, adet alanı değil") — orada kesir serbesttir, kolon
// decimal(12,3) sınırı mevcut kapılarda (KolonTavani) kalır.
//
// Kural Core AdetKurali'dir (ekran <Adet> bileşeniyle AYNI sınıf); ileti
// GirdiSozlugu'ndan gelir ("Küsuratlı yazılamaz; tam sayı girin.") — ekranın
// alan altı metniyle aynı cümle. Sessiz kırpma YOK (Anayasa md.1).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Ürünün birimine göre miktar kuralı: ADET → tam sayı ≥ 1, GRAM → serbest kesir.</summary>
public static class UrunAdetKapisi
{
    /// <summary>Gram birimi sabiti (urunler.birim).</summary>
    public const string GramBirimi = "GRAM";

    /// <summary>Miktar ADET birimli üründe tam sayı ≥ 1 değilse 400 (alan + sözlük metni).</summary>
    public static void Dogrula(string? birim, decimal miktar, string alan, string? onek = null)
    {
        if (string.Equals(birim, GramBirimi, StringComparison.Ordinal)) return;
        GirdiKapisi.Denetle(AdetKurali.Denetle(miktar), alan, onek);
    }

    /// <summary>Sıfır olabilen sayım/eşik değeri (sayılan adet, kritik stok): ADET birimde yalnız tam sayı ≥ 0.</summary>
    public static void SifirOlabilirDogrula(string? birim, decimal? miktar, string alan, string? onek = null)
    {
        if (miktar is not { } m || string.Equals(birim, GramBirimi, StringComparison.Ordinal)) return;
        if (m < 0) GirdiKapisi.Denetle(new GirdiHatasi(GirdiKodu.SayiEksi), alan, onek);
        if (m != decimal.Truncate(m)) GirdiKapisi.Denetle(new GirdiHatasi(GirdiKodu.SayiTam), alan, onek);
    }
}
