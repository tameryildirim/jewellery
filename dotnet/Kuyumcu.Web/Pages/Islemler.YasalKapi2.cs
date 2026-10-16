using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-2 (tasarım hakemi: "aynı uyarı 2-3 kez") — kimlik formu açıkken eşik cümlesi form başlığında
// ("Kimlik ve işlem amacı gerekli — 200.000 ₺ ve üstünde") ZATEN yazıyor; Kaydet kilidinin sebebi
// (#islem-kaydet-kilit / #islem-yasal-kapi / düğme ipucu) aynı eşiği ikinci kez söylemez, ne yapılacağını söyler.
//   · BASİT: "Kaydetmek için kimlik bilgilerini doldurun." (ölçüldü: eşik cümlesi 2 blok → 1 blok).
//   · PRO (tur 2, doğrulayıcı KRİTİK: PRO /islemler 220.000 ₺'de eşik 4 blokta — başlık, #masak-kural, kilit,
//     panel): kademe "Yasal kapılar" panelinde (#islem-pro-kimlik-etiket MASAK, #islem-pro-amac-etiket amaç —
//     YASAL-PARAM-EKRAN tur 3 "kademe söylensin" kararının TEK yeri). Kilit yalnız neyin eksik olduğunu (kimlik /
//     kimlik + amaç) söyler, sayı yok; MasakFormu kural satırı yalnız saklama notunu taşır (EsikPanelde).
// Panel çizilmeyen PRO (Yıldız tezgâh) ve form çizilmiyorken (gereksinim yok) eski cümle kalır: eşiği söyleyen
// başka yer yoktur.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>PRO "Yasal kapılar" paneli (FisProYanSutun) çiziliyor mu — çiziliyorsa eşik kademeleri orada yazar.</summary>
    private bool MasakEsikPanelde => Modu.ProfesyonelMi && !YildizTezgahMi;

    private string MasakEksikMetniTekYer =>
        KimlikEksikCumlesi is { } eksik ? eksik   // P-TEZGAH-TUR-KIMLIK (denetim K4): eksik ALANLARIN ADI (MasakFormu.Eksiksiz ile aynı koşullar)
        : masakGereksinim is null || masakEsik is null ? MasakEksikMetni
        : Modu.BasitMi ? ZoIslemSozlugu.Al(Dil.Dil).MasakKilitSebepBasit
        : MasakEsikPanelde
            ? (masakGereksinim.AmacGerekli ? YasalKapi2Sozlugu.Al(Dil.Dil).MasakKilitProAmac : YasalKapi2Sozlugu.Al(Dil.Dil).MasakKilitProKimlik)
            : MasakEksikMetni;
}
