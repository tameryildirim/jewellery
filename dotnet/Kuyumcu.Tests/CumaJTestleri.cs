using System.Text.RegularExpressions;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET CUMA-J — BASİT MOD JARGON + SIĞMA ARTIKLARI (saf sözlük testleri).
// Ekran (bUnit) testleri ilgili ekran sınıflarındadır (CJ_ önekli):
//   EvrakEkranTestleri, AltinPozisyonuKartiTestleri, IceAktarimEkranTestleri,
//   FaturaEkranTestleri, DenetimIziEkranTestleri.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>sikisma-olc.js K8 ile AYNI kalıp: büyük/küçük harf duyarsız, harf sınırlı.</summary>
internal static class CumaJJargon
{
    public static readonly Regex Re = new(
        @"(?<![\p{L}])(cari\p{L}*|storno|vezne|mutabakat|pasife al|denetim izi)(?![\p{L}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}

public sealed class CumaJTestleri
{
    [Fact]
    public void CJ_01_Basit_sozluk_evrak_ve_ice_aktarim_metinleri_jargonsuz_PRO_aynen()
    {
        var pro = Sozlukler.Getir("tr");
        var basit = BasitSozluk.Getir("tr");

        // Kaynak taramada bulunan üç ürün metni (+ aynı ekranların iki kardeş metni).
        Assert.Equal("Tutar ya da müşteri yanlış girildiyse evrakı karşılıksız işaretleyin (borç geri gelir) ya da girişi ters kayıtla geri alın; sonra doğru evrakı yeniden girin.", basit.Evrak.DegismezYol);
        Assert.Equal("Bu işlem müşterinin borcunu tutarı kadar GERİ GETİRİR ve deftere yeni bir satır yazar. Kayıt silinmez; geri dönmek için evrakı tahsil edin ya da ters kayıtla geri alın.", basit.Evrak.OnayMetin);
        Assert.Equal("Müşteri kartları", basit.IceAktarim.KaynakCari);
        Assert.Equal("Aktarım geri alındı; kayıtlar kullanımdan kaldırıldı", basit.IceAktarim.GeriAlindiMesaji);
        foreach (var m in new[] { basit.Evrak.DegismezYol, basit.Evrak.OnayMetin, basit.IceAktarim.KaynakCari, basit.IceAktarim.GeriAlindiMesaji })
        {
            Assert.DoesNotMatch(CumaJJargon.Re, m);
            Assert.DoesNotContain("pasife", m, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("ters kayıtla geri alın", basit.Evrak.DegismezYol, StringComparison.Ordinal);

        // D10: PRO sözlük DEĞİŞMEDİ — muhasebeci teknik terimi okumaya devam eder.
        Assert.Contains("storno edin", pro.Evrak.DegismezYol, StringComparison.Ordinal);
        Assert.Contains("storno'dur", pro.Evrak.OnayMetin, StringComparison.Ordinal);
        Assert.Equal("Müşteri / cari kartları", pro.IceAktarim.KaynakCari);
        Assert.NotSame(pro.Evrak, basit.Evrak);
        Assert.NotSame(pro.IceAktarim, basit.IceAktarim);
    }

    [Fact]
    public void CJ_01b_Ingilizce_basit_karsiliklar_dolu_ve_PRO_dan_farkli()
    {
        var pro = Sozlukler.Getir("en");
        var basit = BasitSozluk.Getir("en");
        Assert.Equal("If the amount or the customer is wrong, mark the cheque as bounced (the debt comes back) or undo the entry with a reverse record, then enter the correct cheque again.", basit.Evrak.DegismezYol);
        Assert.Equal("Customer cards", basit.IceAktarim.KaynakCari);
        Assert.NotEqual(pro.IceAktarim.KaynakCari, basit.IceAktarim.KaynakCari);
        Assert.NotEqual(pro.Evrak.DegismezYol, basit.Evrak.DegismezYol);
        Assert.Equal(4, CumaJSozlugu.AnahtarSayisi);
    }

    [Fact]
    public void CJ_02b_Pozisyon_basit_metinleri_jargonsuz_PRO_nesnesi_aynen()
    {
        var b = CumaJSozlugu.Pozisyon("tr", true);
        foreach (var m in new[] { b.Bos, b.DefterFarki, b.DegismezGerekce, b.DuzeltmeYolu })
            Assert.DoesNotMatch(CumaJJargon.Re, m);
        Assert.Same(PozisyonSozlugu.Tr, CumaJSozlugu.Pozisyon("tr", false));
        Assert.Same(PozisyonSozlugu.En, CumaJSozlugu.Pozisyon("en", false));
        Assert.Contains("cari için", PozisyonSozlugu.Tr.DuzeltmeYolu, StringComparison.Ordinal);
        // Diğer alanlar aynen taşınır (kopya, eksik alan yok).
        Assert.Equal(PozisyonSozlugu.Tr.Baslik, b.Baslik);
        Assert.NotEqual(PozisyonSozlugu.En.DuzeltmeYolu, CumaJSozlugu.Pozisyon("en", true).DuzeltmeYolu);
    }

    [Fact]
    public void CJ_03_Kisa_tablo_basliklari_ve_fatura_hucresi_iki_dilde_kisa()
    {
        foreach (var e in new[] { CumaJSozlugu.EkranTr, CumaJSozlugu.EkranEn, CumaJSozlugu.EkranProTr, CumaJSozlugu.EkranProEn })
        {
            // K3: kısa etiket tek kelime/kısa ifade — 1280 px'te iki satıra bölünmez.
            Assert.True(e.EvrakSutunTur.Length <= 6);
            Assert.True(e.EvrakSutunVeren.Length <= 10);
            Assert.True(e.EvrakSutunVade.Length <= 6);
            Assert.True(e.FaturaNoKesimdeKisa.Length <= 16);
        }
        Assert.Equal("Kesimde verilir", CumaJSozlugu.Ekran("tr").FaturaNoKesimdeKisa);
        // 4. tur: "Veren" sütunu moda göre; PRO'da "Müşteri" yazmaz.
        Assert.Equal("Kimden", CumaJSozlugu.Ekran("tr", basit: true).EvrakSutunVeren);
        Assert.Equal("Veren", CumaJSozlugu.Ekran("tr", basit: false).EvrakSutunVeren);
        Assert.Equal("Given by", CumaJSozlugu.Ekran("en", basit: false).EvrakSutunVeren);
        Assert.Equal("Dosyayı hazırlayın", CumaJSozlugu.EkranTr.IceAktarimHazirlikBaslik);
    }

    [Fact]
    public void CJ_09_Jargon_kalibi_cekimli_cari_bicimlerini_yakalar()
    {
        // Doğrulayıcı bulgusu (25.09): dar kalıp 'carilerde/carilerin' çekimlerini kaçırıyordu.
        foreach (var m in new[] { "cari", "Cari", "carilerde", "carilerin", "cariden", "Carilerin", "storno" })
            Assert.Matches(CumaJJargon.Re, m);
        foreach (var m in new[] { "Müşteri kartları", "ters kayıtla geri alın", "Kesimde verilir" })
            Assert.DoesNotMatch(CumaJJargon.Re, m);
        // PRO pozisyon metinleri çekimli 'cari' taşır — geniş kalıp onları yakalar; BASİT kopya temiz kalır.
        Assert.Matches(CumaJJargon.Re, PozisyonSozlugu.Tr.Bos);
        Assert.Matches(CumaJJargon.Re, PozisyonSozlugu.Tr.DefterFarki);
        var b = CumaJSozlugu.Pozisyon("tr", true);
        Assert.Equal("Henüz stokta, hurdada ya da müşteri hesaplarında altın yok.", b.Bos);
        Assert.DoesNotMatch(CumaJJargon.Re, b.DefterFarki);
    }
}
