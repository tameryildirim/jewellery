using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-PARAM-EKRAN (MASAK-370 tasarım hakemi KRİTİK) — satış ekranının kimlik
// eksik kilit metni TETİKLEYEN KADEMEYİ söyler. Eskiden her durumda "MASAK eşiği
// aşıldı" yazıyordu; 07.10.2026 sonrası 200.000–369.999,99 bandında kimliği md.5/1-b
// eşiği değil amaç beyanı kademesi istiyor ve kullanıcı 370.000'i görüp şaşırıyordu.
// Sayılar SUNUCUDAN (/masak/esikler, /masak/gereksinim); istemciye eşik gömülmez.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>
    /// Kimlik eksik kilit metni: (1) karar bugünkü bağlantılı toplamdan geldiyse toplamı ve
    /// kimliği isteyen en düşük tutarı; (2) tutar md.5/1-b eşiğinin altında ama amaç beyanı
    /// kademesindeyse "X ve üstünde kimlik ve işlem amacı alınır"; (3) aksi hâlde kimlik eşiği.
    /// Eşik bilinmiyorsa (sunucu yanıtı yok) eski genel cümle — fail-closed, sayı uydurulmaz.
    /// </summary>
    private string MasakEksikMetni
    {
        get
        {
            if (masakEsik is not { } e) return Dil.T.Islem.MasakKimlikEksik;
            var m = YasalParamSozlugu.Al(Dil.Dil);
            // Tur 2: kademe TEK seçiciden (MasakKademe) — form başlığı ve PRO paneli aynı kuralı kullanır.
            // Sunucu kimlik istedi ama ölçüyü göndermediyse (eski sunucu) toplam UYDURULMAZ — genel cümle.
            return MasakKademe.Sec(e, masakGereksinim, MasakTutar) switch
            {
                // Tur 3 (hasım KRİTİK): toplamın AŞTIĞI kademe söylenir — 370.000 kimlik (MASAK) ile
                // 200.000 amaç kademesi ayrı; eskiden her zaman en düşük kademe (200.000) basılıyordu.
                MasakTetik.Toplam => MasakKademe.ToplamKademesi(e, masakGereksinim) switch
                {
                    MasakTetik.Kimlik => string.Format(m.HataToplam, Bicim.Tl(masakGereksinim!.OlcuTutarTl), Bicim.Tl(e.KimlikEsigi)),
                    MasakTetik.Amac => string.Format(m.HataToplamAmac, Bicim.Tl(masakGereksinim!.OlcuTutarTl), Bicim.Tl(e.AmacBeyaniEsigi)),
                    _ => Dil.T.Islem.MasakKimlikEksik,
                },
                MasakTetik.Kimlik => string.Format(m.HataKimlik, Bicim.Tl(e.KimlikEsigi)),
                MasakTetik.Amac => string.Format(m.HataAmac, Bicim.Tl(e.AmacBeyaniEsigi)),
                _ => Dil.T.Islem.MasakKimlikEksik,
            };
        }
    }

    /// <summary>Tur 2: PRO "Yasal kapılar" panelinin ölçüsü — kilit metniyle AYNI kural (MasakKademe).</summary>
    private decimal ProMasakOlcu => MasakKademe.Olcu(masakGereksinim, MasakTutar);

    /// <summary>Tur 2: amaç beyanı kademesi ayrı satır olarak gösterilecekse eşiği (sunucudan); yoksa null.</summary>
    private decimal? ProAmacEsigi => masakEsik is { } e && MasakKademe.AmacKademesiVar(e) ? e.AmacBeyaniEsigi : null;
}
