using Kuyumcu.Web.Bilesenler;

namespace Kuyumcu.Web.Pages;

// D2-05b (19.10) — hesap çekmecesi › banka bağlantısı formu açıkken çekmecenin birincil düğmesi
// "Kaydet ve dene" olur; Vazgeç/kapat önce formu kapatır (çekmece açık kalır).
public partial class Banka
{
    private BankaBaglantiBolumu? bbBolum;
    private bool BbFormAcik => bbBolum?.FormAcik == true;

    private Task CekmeceKaydet() => BbFormAcik ? bbBolum!.Kaydet() : HesapGuncelle();

    private Task CekmeceKapat()
    {
        if (BbFormAcik) return bbBolum!.FormKapat();
        duzenleHesap = null;
        bbBolum = null;   // kapanan çekmecenin form durumu bir sonraki açılışa taşınmaz
        return Task.CompletedTask;
    }
}
