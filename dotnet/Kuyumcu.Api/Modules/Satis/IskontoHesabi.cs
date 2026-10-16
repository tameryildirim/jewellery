using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Satis;

/// <summary>
/// PAKET P-16 (Y-4) — SATIR VE FİŞ İSKONTOSUNUN TEK HESAP YERİ.
///
/// <para>
/// ⚠ <b>NEDEN SUNUCUDA YENİDEN HESAPLANIR:</b> istemci iskonto TUTARINI
/// gönderseydi, "yüzde 10" yazan kasiyerin ekranında görünen rakam ile deftere
/// yazılan rakam ayrışabilirdi (yuvarlama, elle düzeltme, eski sürüm istemci,
/// doğrudan API çağrısı). Bu yüzden istemciden YALNIZ YÜZDE alınır; TL karşılığı
/// burada üretilir. Ekrandaki önizleme aynı kuralı uygular ama son sözü sunucu söyler.
/// </para>
///
/// <para>
/// ⚠ <b>FİŞ İSKONTOSU NEDEN SATIRLARA DAĞITILIR:</b> fiş toplamından tek
/// kalemde düşülseydi, satır tutarlarının toplamı fiş toplamına EŞİT OLMAZDI —
/// ödeme bacağı doğrulaması (OdemeSepeti), stok maliyeti, KDV matrahı ve
/// fatura kalemleri hepsi satır tutarlarını okur. Dağıtım oransaldır; kuruş
/// artığı EN BÜYÜK satıra yazılır, böylece Σ satır = fiş toplamı TAM tutar.
/// </para>
/// </summary>
public static class IskontoHesabi
{
    /// <summary>Kuruş yuvarlaması — sunucu ile istemci AYNI kuralı kullanır.</summary>
    private static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Yüzde 0-100 dışındaysa reddedilir (sessizce kelepçelenmez).
    /// PAKET GOC-C: kural Core <c>YuzdeKurali</c> (K15: 0–100, en çok 2 ondalık — ekran <c>Yuzde</c>
    /// bileşeniyle AYNI sınıf); ileti GirdiSozlugu'ndan, ekranın alan altı metniyle aynı cümle.
    /// Eksi iskonto eskiden tek kalemli satışta sessizce yok sayılıyordu; artık 400.
    /// </summary>
    public static void YuzdeDogrula(decimal? yuzde, string alanAdi, string alan = "fisIskontoYuzde")
    {
        if (yuzde is null) return;
        Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Denetle(
            Kuyumcu.Core.Domain.Girdi.YuzdeKurali.Denetle(yuzde), alan, alanAdi);
    }

    /// <summary>Brüt tutarın verilen yüzdesi — kuruşa yuvarlanmış iskonto TL'si.</summary>
    public static decimal Tutar(decimal brut, decimal? yuzde)
    {
        if (yuzde is null or <= 0m || brut <= 0m) return 0m;
        var t = Yuvarla2(brut * yuzde.Value / 100m);
        // İskonto brütü aşamaz: %100'de tutar tam brüttür, negatif satır doğmaz.
        return t > brut ? brut : t;
    }

    /// <summary>
    /// Fiş iskontosunu satırlara ORANSAL dağıtır.
    /// Dönen dizi <paramref name="satirTutarlari"/> ile aynı sıradadır ve
    /// elemanlarının toplamı <paramref name="fisIskontoTutari"/>'na TAM eşittir.
    /// </summary>
    public static decimal[] Dagit(IReadOnlyList<decimal> satirTutarlari, decimal fisIskontoTutari)
    {
        var n = satirTutarlari.Count;
        var pay = new decimal[n];
        if (n == 0 || fisIskontoTutari <= 0m) return pay;

        var toplam = satirTutarlari.Sum();
        if (toplam <= 0m) return pay;

        // Tutar toplamı aşamaz — aşarsa fiş negatife düşerdi.
        var dagitilacak = fisIskontoTutari > toplam ? toplam : fisIskontoTutari;

        decimal verilen = 0m;
        for (var i = 0; i < n; i++)
        {
            pay[i] = Yuvarla2(dagitilacak * satirTutarlari[i] / toplam);
            // Satırın kendi tutarını aşamaz.
            if (pay[i] > satirTutarlari[i]) pay[i] = satirTutarlari[i];
            verilen += pay[i];
        }

        // Kuruş artığı: EN BÜYÜK tutarlı satıra yazılır (deterministik).
        var fark = dagitilacak - verilen;
        if (fark != 0m)
        {
            var enBuyuk = 0;
            for (var i = 1; i < n; i++)
                if (satirTutarlari[i] > satirTutarlari[enBuyuk]) enBuyuk = i;

            var yeni = pay[enBuyuk] + fark;
            // Artık düzeltmesi satırı negatife ya da tutarının üstüne itmez.
            if (yeni < 0m) yeni = 0m;
            if (yeni > satirTutarlari[enBuyuk]) yeni = satirTutarlari[enBuyuk];
            pay[enBuyuk] = yeni;
        }

        return pay;
    }
}
