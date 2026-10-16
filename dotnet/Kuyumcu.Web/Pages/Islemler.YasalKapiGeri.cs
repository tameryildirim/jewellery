using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-GERI — eşiğe bağlı ekran durumu her fiş değişiminde YENİDEN değerlendirilir.
// (ayrıntı ve kök neden: Bilesenler/Fis/YasalKapiIzi.cs)
//
// Eskiden üç DÜZ bool idi (Islemler.Odeme.cs / Islemler.razor) ve yalnız Kaydet/müşteri/tür
// değişimiyle iniyordu; artık her biri sunucu reddinin İZİDİR — fişin yasal ölçüsü reddin
// ölçüsünün altına inince (kalem sil, adet azalt, iskonto, eski altın/takas tutarı) ya da müşteri
// değişince kendiliğinden düşer, tutar geri çıkınca geri gelir:
//   • masakSunucudanGerekli — MASAK reddi formu zorla açıyordu (ölçü: MasakTutar)
//   • kapiSunucudanAcildi   — BASİT "yasal kapı" bandı + ödeme seçicisi (ölçü: YasalOlcu)
//   • pesinKimlikGerekli    — Peşin reddi bandı "gerçek müşteri seçin" (ölçü: YasalOlcu)
// Sunucu hata bandı (yalnız sunucu reddinin kendi cümlesi) ve Peşin "aynı kişi mi?" sorusu da
// kapı izi geçersizleşince iner (OnAfterRenderAsync → YasalIzleriTazele). Girilen MASAK beyanı
// (masak modeli) SİLİNMEZ — gövdeye yalnız MasakGerekli iken gider (MasakGovdesi, değişmedi).
// Kaydet()/POST gövdeleri DEĞİŞMEDİ; atamalar (= true / = false / |=) aynen çalışır.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private YasalKapiIzi? masakRedIzi;
    private YasalKapiIzi? kapiRedIzi;
    private YasalKapiIzi? pesinRedIzi;
    /// <summary>Kapı izi bir sunucu reddinden geldi ve henüz "geçersizleşti" işlenmedi.</summary>
    private bool kapiIziIslenmedi;

    /// <summary>Fişin yasal kapılara giden ölçüsü: MASAK tutarı ile tevsik tutarının büyüğü (sunucunun iki kapısı).</summary>
    private decimal YasalOlcu => Math.Max(Math.Abs(MasakTutar), Math.Abs(TevsikTutar));

    /// <summary>MASAK reddi sunucudan geldi VE fiş hâlâ o tutarda/üstünde, aynı müşteride.</summary>
    private bool masakSunucudanGerekli
    {
        get => YasalKapiIzi.Gecerli(masakRedIzi, MasakTutar, cariId);
        set => masakRedIzi = value ? YasalKapiIzi.Al(MasakTutar, cariId) : null;
    }

    /// <summary>Son POST bir kapıdan döndü VE fiş hâlâ o ölçüde (BASİT bant + ödeme seçicisi).</summary>
    private bool kapiSunucudanAcildi
    {
        get => YasalKapiIzi.Gecerli(kapiRedIzi, YasalOlcu, cariId);
        set { kapiRedIzi = value ? YasalKapiIzi.Al(YasalOlcu, cariId) : null; kapiIziIslenmedi = value; }
    }

    /// <summary>Peşin reddi (gerçek müşteri gerekir) VE fiş hâlâ o ölçüde.</summary>
    private bool pesinKimlikGerekli
    {
        get => YasalKapiIzi.Gecerli(pesinRedIzi, YasalOlcu, cariId);
        set => pesinRedIzi = value ? YasalKapiIzi.Al(YasalOlcu, cariId) : null;
    }

    /// <summary>
    /// OnAfterRenderAsync'te: kapı izi az önce geçersizleştiyse sunucunun ret cümlesi (hata bandı) ve
    /// Peşin "aynı kişi mi?" sorusu iner — artık o fişe ait değildir. Değişiklik olduysa true (yeniden çiz).
    /// Kullanıcının kendi hatası / başka bant (sunucu cümlesi değilse) dokunulmaz.
    /// </summary>
    private bool YasalIzleriTazele()
    {
        if (!kapiIziIslenmedi || kapiRedIzi is null || kapiRedIzi.Gecerli(YasalOlcu, cariId)) return false;
        kapiIziIslenmedi = false;
        var degisti = false;
        if (hata is not null && hata == sunucuSadeMesaj) { hata = null; degisti = true; }
        if (pesinSoruAcik) { pesinSoruAcik = false; degisti = true; }
        return degisti;
    }

    // ── nakit sınırı (tevsik haddi) önizlemesi: toplam HER değişimde yeniden sorulur ──
    /// <summary>Son /tevsik/gunluk önizlemesinin sorulduğu fiş toplamı (TevsikOnizle yazar); null = hiç sorulmadı.</summary>
    private decimal? tevsikSorulanToplam;

    /// <summary>
    /// Sunucunun "işlem tutarı kuralı devrede" önizlemesi yalnız kalem ekle/sil/satır düzeltmesinde soruluyordu; fiş
    /// iskontosu (ve Yıldız kalem düzeltmesi gibi başka yollar) toplamı değiştirince bayat kalıyor, ödeme otomatik Kart'ta ve
    /// "nakit alınamaz" kilidi yerinde kalıyordu. Satışta toplam son sorulandan farklıysa önizleme yeniden istenir (tek istek;
    /// TevsikOnizle sorduğu toplamı yazar → döngü yok). Değiştiyse true.
    /// </summary>
    private async Task<bool> TevsikToplamTazele()
    {
        if (kalemler.Count == 0) { tevsikSorulanToplam = null; return false; }   // boş fiş (kayıt/Vazgeç sonrası): istek YOK (YASAL-2) — ilk kalem zaten sorar
        if (tur != "SATIS" || tevsikSorulanToplam is not { } t || t == ToplamTl) return false;
        await TevsikOnizle();
        return true;
    }

    // ── Peşin kilidinin cümlesi: HANGİ eşik tutuyor (sunucunun sırası: MASAK, sonra tevsik) ──
    private Kuyumcu.Core.I18n.YasalKapiGeriMetinleri YKG => Kuyumcu.Core.I18n.YasalKapiGeriSozlugu.Al(Dil.Dil);

    /// <summary>Peşin + tutar MASAK'ın kimlik istediği en düşük tutarda/üstünde mi (eşik sunucudan).</summary>
    private bool PesinMasakUstu => masakEsik is { } me && me.KimlikIstenenTutar() > 0m && MasakTutar >= me.KimlikIstenenTutar();

    /// <summary>
    /// Peşin engelinin TEK cümlesi — tutar değişince cümle de değişir (kimlik kademesinden tevsik kademesine inince
    /// "kimlik gerekir" yerine "had üstü müşteri adına"). Eşik bilinmiyorsa eski genel cümle (sayı uydurulmaz).
    /// </summary>
    internal string PesinKilitMetni =>
        PesinMasakUstu && masakEsik is { } me ? string.Format(YKG.PesinKimlikKilit, Bicim.Tl(me.KimlikIstenenTutar()))
        : tevsikHaddi is { } h && h > 0m ? string.Format(YKG.PesinTevsikKilit, Bicim.Tl(h))
        : Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).PesinTutarKilit;
}
