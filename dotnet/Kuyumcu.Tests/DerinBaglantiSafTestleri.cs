using Kuyumcu.Contracts.Arama;
using Kuyumcu.Web.Bilesenler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-15 — DERİN BAĞLANTI AYRIŞTIRICISI + ARAMA TÜR HARİTASI (saf testler)
// (kimlikler B-29 · Y-5)
//
// Bu iki mekanizma veritabanı ya da HTTP gerektirmez; kural kırıldığında
// KIRMIZI olan en ucuz test katmanı burasıdır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DerinBaglantiSafTestleri
{
    [Fact]
    public void Sorgusuz_adreste_suzgec_YOKTUR()
        => Assert.Empty(DerinBaglanti.Suzgecler("https://x.local/cariler"));

    [Fact]
    public void Coklu_suzgec_ayristirilir_ve_yuzde_kodlama_cozulur()
    {
        var s = DerinBaglanti.Suzgecler("https://x.local/urunler?q=Burma%20Bilezik&maden=ALTIN");

        Assert.Equal("Burma Bilezik", s["q"]);
        Assert.Equal("ALTIN", s["maden"]);
    }

    /// <summary>
    /// Aynı ad iki kez gelirse İLKİ kazanır. Son gelenin sessizce ezmesi,
    /// bağlantıyı üreten ekranın niyetini görünmez biçimde değiştirirdi.
    /// </summary>
    [Fact]
    public void Ayni_ad_iki_kez_gelirse_ILKI_kazanir()
        => Assert.Equal("bir", DerinBaglanti.Suzgecler("/x?ad=bir&ad=iki")["ad"]);

    /// <summary>Boş değer bir süzgeç DEĞİLDİR; kutuyu boşaltıp yalan söylemez.</summary>
    [Fact]
    public void Bos_degerli_parametre_suzgec_sayilmaz()
        => Assert.Empty(DerinBaglanti.Suzgecler("/x?q="));

    [Fact]
    public void Deger_icinde_esittir_varsa_KIRPILMAZ()
        => Assert.Equal("a=b", DerinBaglanti.Suzgecler("/x?imza=a=b")["imza"]);

    [Fact]
    public void Kare_isaretinden_sonrasi_sorguya_KARISMAZ()
        => Assert.Equal("ayse", DerinBaglanti.Suzgecler("/cariler?q=ayse#bolum")["q"]);

    [Fact]
    public void Oku_bulunmayan_suzgecte_null_doner()
        => Assert.Null(DerinBaglanti.Oku("/cariler?q=ayse", "tur"));

    [Fact]
    public void Suzgec_adi_buyuk_kucuk_harf_ayirmaz()
        => Assert.Equal("ayse", DerinBaglanti.Oku("/cariler?Q=ayse", "q"));

    // ── Arama tür haritası (UX-S31: tek kaynak, fail-closed) ────────────────

    [Fact]
    public void Her_arama_turunun_bir_yetki_modulu_VARDIR()
        => Assert.All(AramaTurleri.Tumu, t => Assert.False(
            string.IsNullOrWhiteSpace(AramaTurleri.Modul(t)),
            $"{t} türünün yetki modülü tanımsız — yetki kapısı sessizce açılırdı"));

    /// <summary>Bilinmeyen tür → modül YOK. Fail-closed: yetki varsayılmaz.</summary>
    [Fact]
    public void Bilinmeyen_turun_modulu_NULL_olur()
        => Assert.Null(AramaTurleri.Modul("UYDURMA"));

    [Fact]
    public void Genel_arama_turleri_Tumu_kumesinin_ALT_kumesidir()
        => Assert.All(AramaTurleri.Genel, t => Assert.Contains(t, AramaTurleri.Tumu));
}
