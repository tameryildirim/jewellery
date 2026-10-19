using Kuyumcu.Web.Bilesenler.Fis;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-S1 — "KAYDET VE YAZDIR" ETİKETİ DAVRANIŞLA AYNI ŞEYİ SÖYLER (Islemler sayfasının parçası)
//
// Doğrulayıcı bulgusu (24.09, ORTA): tuvalin "Kaydet ve yazdır" düğmesi yalnız
// kaydediyordu; yazdırmak için tezgâhtar iki kez daha tıklıyordu.
//   • Kayıt TEK belge doğuruyorsa (FisYazdir.TekBelge) başarıdan SONRA yazdırma
//     sayfası (/cikti/SATIS_FISI/{id}?yazdir=1 → Cikti.razor bir kez print()) yeni
//     sekmede kendiliğinden açılır. Satış ekranı yerinde kalır (barkod odağı).
//   • Tarayıcı açılır pencereyi engellerse "Fiş yazdır" bağlantısı zaten görünür
//     ve bir sonraki Kaydet'e kadar durur (S2-5b) — yol kaybolmaz.
//   • PAKET L-1 (tevsik, KRİTİK — 25.09): çok kalemli satış ARTIK kalem başına ayrı
//     POST EDİLMEZ; karma/hediye/iskonto fişiyle aynı TEK POST'la /islemler/satis-fisi'ye
//     gider (FisUcunaGider). Eskiden 42.065,89 ₺'lik nakit fiş 28.711,64 + 13.354,25
//     diye bölünüyor, sunucunun tevsik kapısı (459/575 s. VUK GT, fiş toplamı) haddi
//     hiç görmüyordu. Artık satışta her kayıt TEK belgedir → "Kaydet ve yazdır" her
//     zaman doğrudur; T-S1'in "N ayrı fiş" notu kalktı.
//   • PAKET L-1b (tevsik, KRİTİK — 25.09): çok kalemli ALIŞ da artık TEK POST
//     /islemler/alis-fisi'dir (AlisFisUcunaGider); tek kalemli alış eski /islemler/alis
//     yolunda. ALIS tuval yüzü değildir (etiket ve kendiliğinden yazdırma değişmedi).
// ⚠ Bu dosya istek ATMAZ; tek POST yolu Kaydet()'tir. Yalnız sonuç sayfası açılır.
// ═════════════════════════════════════════════════════════════════════════════

public partial class Islemler
{
    private bool otoYazdir, otoYazdirAcildi;

    /// <summary>
    /// PAKET L-1 — satış TEK fiş ucuna (/islemler/satis-fisi) mı gider? Karma · hediye · iskonto
    /// ya da ≥2 kalem → EVET. Kaydet() dalı ve "tek belge" kararı AYNI kuraldan okunur.
    /// </summary>
    private bool FisUcunaGider => tur == "SATIS" && (FisYazdir.FisUcuMu(kalemler.Count, karmaOdeme || HediyeVar || IskontoVar)
                                                     || FaturaSecili || HavaleModu);   // M6: faturalı satış fiş ucundan · 17.10 havaleden satış

    /// <summary>
    /// PAKET L-1b — alış TEK fiş ucuna (/islemler/alis-fisi) mı gider? ≥2 kalem → EVET (satışla aynı
    /// saf kural, eşik rakamı YOK). Tek kalemli alış bugünkü /islemler/alis yolunda kalır.
    /// </summary>
    private bool AlisFisUcunaGider => tur == "ALIS" && FisYazdir.FisUcuMu(kalemler.Count, ozelFis: false);

    /// <summary>Bu kayıt tek yazdırılabilir belge mi doğurur? (etiket ve kendiliğinden yazdırma bu karara bağlı)</summary>
    private bool TekBelge => FisYazdir.TekBelge(IadeKipi, kalemler.Count, FisUcunaGider);

    /// <summary>Kaydet düğmesinin etiketi: satış fişinde (BASİT tezgâh · T-S2: PRO tuval) tek belge → "Kaydet ve yazdır", çok belge → "Kaydet".</summary>
    private string KaydetEtiketi =>
        // t3 hakemi (tür adları tutarsız: karo "Değişim" → düğme "Takas Kaydet"): BASİT'te karo adı tuvalin sade adıdır,
        // düğme tür adı TAŞIMAZ ("Kaydet"); PRO'da sekme adı = düğmedeki ad (aynı kaynak FisTurleri).
        // PARA-DEMO-2 (madde 8): Değişim/Takas düğmesi iki modda AYNI yazı ("Kaydet") — karo/sekme adı zaten başlıkta.
        !SatisTuvali ? (Modu.BasitMi || tur == "TAKAS" ? ST.KaydetTek : $"{TurEtiket(tur)} {Dil.T.Islem.Kaydet}")
        : TekBelge && !FaturaSecili ? ST.KaydetYazdir : ST.KaydetTek;   // 19.10 kurucu: faturalı satış yazdırma vaat etmez

    /// <summary>Başarılı kayıttan sonra: etiket yazdırma vaat ettiyse ve tek belge çıktıysa yazdırma sayfasını açar.</summary>
    private async Task OtomatikYazdir()
    {
        otoYazdirAcildi = false;
        if (!otoYazdir || yazdirIdler.Count != 1 || FisYazdir.YazdirAdresi(yazdirIdler[0]) is not { } adres) return;
        try
        {
            await JS.InvokeVoidAsync("open", adres, "_blank", "noopener");
            otoYazdirAcildi = true;
        }
        catch (JSException ex) { yazdirHata = ex.Message; }   // UX-S28: yutulmaz; bağlantı yine görünür
    }
}
