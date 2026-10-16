using System.Globalization;
using Kuyumcu.Api.Modules.Banka;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 32 — ÜNVAN EŞLEŞTİRME [SAF]
//
// ⚠ BU DOSYANIN ASIL İDDİASI: ünvan benzerliği HİÇBİR KOŞULDA otomatik
//   bağlamaz. Skor yalnız ÖNERİ SIRALAR; otomatik bağın tek yolu IBAN'dır
//   (bunun DB kanıtı EkstreIceAktarmaTestleri'ndedir).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CariEslestirmeTestleri
{
    [Fact]
    public void Sirket_ekleri_atilinca_ayni_kisi_yuksek_skor_verir()
    {
        var skor = CariEslestirici.Skor("AYŞE DEMİR LTD. ŞTİ.", "Ayşe Demir");

        Assert.Equal(1m, skor);
    }

    [Fact]
    public void Ayse_Demir_ile_Ayse_Demirci_ESIGIN_ALTINDA_kalir()
    {
        // "Ayşe Demir" ∩ "Ayşe Demirci" = {AYŞE} · birleşim = 3 → 0,333
        var skor = CariEslestirici.Skor("Ayşe Demir", "Ayşe Demirci");

        Assert.True(skor < new EkstreAyarlari().OneriEsigi,
            $"Beklenen: eşiğin (0,60) altında; ölçülen {skor}");
    }

    [Fact]
    public void Tr_TR_kulturu_ilker_ILKER_olur_INVARIANT_KULTURDE_BILE()
    {
        // ⚠ TUZAK SABİTLENİYOR: invariant kültürde 'i'.ToUpper() = 'I' olurdu
        //   ve "İLKER" adlı cari ile hiç eşleşmezdi. TestOrtami/HttpOrtami
        //   kültür ayarlamadığı için iş parçacığı kültürü BURADA zorlanır.
        var onceki = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Assert.Equal("İLKER", CariEslestirici.UnvanNormalle("ilker"));
            Assert.Equal(1m, CariEslestirici.Skor("ilker", "İLKER"));
        }
        finally { Thread.CurrentThread.CurrentCulture = onceki; }
    }

    [Fact]
    public void Noktalama_ve_coklu_bosluk_temizlenir()
        => Assert.Equal("ALİ VELİ", CariEslestirici.UnvanNormalle("  Ali,  Veli.  "));

    [Fact]
    public void Bos_unvan_sifir_skor_verir()
    {
        Assert.Equal(0m, CariEslestirici.Skor(null, "Ali"));
        Assert.Equal(0m, CariEslestirici.Skor("Ali", ""));
    }

    [Fact]
    public void Tek_kelimelik_unvanda_sirket_eki_SILINMEZ()
        // "LTD" tek başına kalan ünvanı boşaltmaz — aksi hâlde skor patlardı.
        => Assert.Equal("LTD", CariEslestirici.UnvanNormalle("Ltd"));
}
