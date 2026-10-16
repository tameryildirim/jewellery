using Kuyumcu.Api.Modules.Meta;
using Kuyumcu.Api.Modules.Satis;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// UX-1-C3 DOĞRULAMA DÜZELTMESİ — "ekrandaki düzeltme yolu, sunucunun GERÇEKTEN
// yapabildiği şeyle uyuşur" sözleşmesi.
//
// NEDEN: doğrulayıcı KRİTİK bulgusu — /gider-pusulalari ekranı kullanıcıya
// "İşlemler ekranından alım işlemi ters kayıtla düzeltilir" diyordu; oysa
// StornoServisi.ErtelenenTurler içinde HURDA_ALIS var ve sunucu bu türü
// 409 ile reddediyor. 1798 test + denetim.sh + 14 mutasyon bunu göremedi,
// çünkü hiçbir test METNİN DOĞRULUĞUNU sunucu yeteneğiyle karşılaştırmıyordu.
//
// Bu dosya mevcut hiçbir testi değiştirmez; yalnız EKLER (K4).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class UX1C3DuzeltmeSozlesmeTestleri
{
    /// <summary>
    /// Kullanıcıya "şu yoldan düzeltebilirsin" diyen kalıplar. Ters kaydı
    /// OLMAYAN bir tür için bu kalıpların hiçbiri metne giremez.
    /// </summary>
    private static readonly string[] VaatKaliplariTr =
    [
        "ters kayıtla düzeltilir", "ters kayıtla düzeltin", "ters kayıt girilir",
        "storno edilir", "storno edin", "storno yapılır",
        "iade fişi", "düzeltme fişi", "yeni pusula düzenlenir",
        "İşlemler ekranından",
    ];

    private static readonly string[] VaatKaliplariEn =
    [
        "reverse the purchase", "reverse the transaction", "issue a new voucher",
        "void it", "void the transaction", "correct it on the",
    ];

    /// <summary>Metnin "bugün yapılamıyor" dediğini kanıtlayan işaretler.</summary>
    private static readonly string[] YokIsaretleriTr =
        ["bulunmuyor", "düzeltilemiyor", "yapılamıyor", "hükümsüz kılınamaz", "yoktur"];

    private static readonly string[] YokIsaretleriEn =
        ["cannot", "no reversing", "is not possible", "there is no"];

    private static void VaatEtmez(string metin, string[] vaatler, string neden)
    {
        Assert.False(string.IsNullOrWhiteSpace(metin), $"{neden}: metin boş.");
        foreach (var v in vaatler)
            Assert.False(metin.Contains(v, StringComparison.OrdinalIgnoreCase),
                $"{neden}: metin OLMAYAN bir düzeltme yolu vaat ediyor → \"{v}\"\n{metin}");
    }

    private static void YokDer(string metin, string[] isaretler, string neden)
        => Assert.True(isaretler.Any(i => metin.Contains(i, StringComparison.OrdinalIgnoreCase)),
            $"{neden}: metin, düzeltmenin bugün YAPILAMADIĞINI açıkça söylemiyor.\n{metin}");

    // ── 1. StornoServisi ile DuzeltmePolitikasi aynı gerçeği anlatır ─────────

    [Fact]
    public void Ertelenen_turler_STORNOLANABILIR_listesinde_DEGILDIR()
    {
        foreach (var tur in StornoServisi.ErtelenenTurler)
            Assert.DoesNotContain(tur, StornoServisi.StornolanabilirTurler);
    }

    [Fact]
    public void Ertelenen_her_tur_politikada_STORNO_YOK_damgasi_tasir()
    {
        foreach (var tur in StornoServisi.ErtelenenTurler)
        {
            Assert.True(DuzeltmePolitikasi.HareketTurleri.TryGetValue(tur, out var s),
                $"'{tur}' türü DuzeltmePolitikasi.HareketTurleri'nde damgasız.");
            Assert.Equal(GerekceAnahtarlari.StornoYok, s!.GerekceAnahtari);
        }
    }

    // ── 2. STORNO_YOK gerekçe metinleri bir yol VAAT ETMEZ ───────────────────

    [Fact]
    public void Storno_yok_gerekcesi_olmayan_bir_yol_vaat_etmez()
    {
        var tr = Sozlukler.Tr.Duzeltme.GerekceMetni(GerekceAnahtarlari.StornoYok);
        var en = Sozlukler.En.Duzeltme.GerekceMetni(GerekceAnahtarlari.StornoYok);
        VaatEtmez(tr, VaatKaliplariTr, "STORNO_YOK gerekçesi (TR)");
        VaatEtmez(en, VaatKaliplariEn, "STORNO_YOK gerekçesi (EN)");
        YokDer(tr, YokIsaretleriTr, "STORNO_YOK gerekçesi (TR)");
        YokDer(en, YokIsaretleriEn, "STORNO_YOK gerekçesi (EN)");
    }

    // ── 3. Gider pusulası: ekran metni sunucunun GERÇEKTEN yapabildiğiyle uyuşur
    //
    // ⚠ PAKET S-3b'DE SÖZLEŞME DEĞİŞTİ: eskiden "düzeltme yolu HİÇ YOK" doğruydu
    // ve testler metnin bir yol VAAT ETMEMESİNİ istiyordu. Artık VUK md.234
    // iptal yolu GERÇEKTEN VAR (POST /api/v1/gider-pusulalari/{id}/iptal), bu
    // yüzden aynı sözleşme TERSİNDEN uygulanır: metin vaat ettiği yolun sunucuda
    // BULUNDUĞUNU kanıtlamak zorundadır — ve kapsayamadığı şeyi (stok/maliyet
    // bacağı) açıkça SÖYLEMEK zorundadır (Anayasa md.1).

    [Fact]
    public void Gider_pusulasi_politikasi_GERCEK_iptal_ucunu_gosterir()
    {
        var s = DuzeltmePolitikasi.Varliklar["GiderPusulasi"];
        Assert.Equal(Politikalar.Storno, s.Politika);
        Assert.Equal(GerekceAnahtarlari.Pusula, s.GerekceAnahtari);
        Assert.Equal("api/v1/gider-pusulalari/{id}/iptal", s.HedefUc);
    }

    [Fact]
    public void Gider_pusulasi_metni_VAAT_ETTIGI_yolun_kapsamini_DURUSTCE_sinirlar()
    {
        // Sunucu gerçeği DEĞİŞMEDİ: hurda alışının stok/maliyet bacağı hâlâ
        // geri alınamıyor. Metin bunu söylemezse kullanıcı iptali "alım silindi"
        // sanır.
        Assert.Contains("HURDA_ALIS", StornoServisi.ErtelenenTurler);

        var tr = Sozlukler.Tr.SayfaDuzeltme.PusulaDuzeltmeYolu;
        var en = Sozlukler.En.SayfaDuzeltme.PusulaDuzeltmeYolu;

        Assert.Contains("iptal", tr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("void", en, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("DEĞİŞMEZ", tr);
        Assert.Contains("UNCHANGED", en);

        // "İşlemler ekranından ters kayıt" gibi OLMAYAN yol hâlâ vaat edilemez.
        VaatEtmez(tr, ["ters kayıtla düzeltilir", "storno edilir", "İşlemler ekranından"],
            "PusulaDuzeltmeYolu (TR)");
        VaatEtmez(en, ["reverse the purchase", "reverse the transaction"],
            "PusulaDuzeltmeYolu (EN)");
    }

    [Fact]
    public void Pusula_gerekcesi_YERINDE_duzenlemeyi_vaat_ETMEZ()
    {
        var tr = Sozlukler.Tr.Duzeltme.GerekceMetni(GerekceAnahtarlari.Pusula);
        var en = Sozlukler.En.Duzeltme.GerekceMetni(GerekceAnahtarlari.Pusula);
        Assert.False(string.IsNullOrWhiteSpace(tr));
        Assert.False(string.IsNullOrWhiteSpace(en));
        Assert.Contains("yerinde değiştirilmez", tr);
        Assert.Contains("not edited in place", en);
    }
}
