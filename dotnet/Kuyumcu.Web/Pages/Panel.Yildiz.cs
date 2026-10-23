using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4C — Yıldız Bugün'ün iki tuval satırı (Bugun4 "Seni bekleyen"):
//   • "Dünkü gün sonu kapatılmadı · Kasa · Kapat" — GET /raporlar/kasa-defteri (dün, tek gün):
//     dün hareketi olan ve kapanışı KAPALI olmayan kasa varsa satır; "Kapat" → Kasalar (gün sonu orada).
//   • "X'in emaneti · N g has · 30 günü aştı · Aç" — GET /cari (EmanetteHas) + GET /emanet/{id}:
//     altın emaneti eşikten (30 gün, ayar sabiti) uzun duranlar.
// YALNIZ OKUMA, YALNIZ Yıldız (öteki 8 şemada bu yükleyici HİÇ çalışmaz). Veri yoksa / yetki yoksa
// satır ÇIKMAZ (uydurma yok); yükleme hatası "yok"a çevrilmez — kendi bandında söylenir (UX-S28).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Panel
{
    private bool yildizEkYuklendi;
    private IReadOnlyList<string> yildizDunKasalar = [];
    private List<YildizBekleyenSatir> yildizEmanetSatirlari = [];
    private string? yildizGunSonuHata;
    private string? yildizEmanetHata;
    private bool yildizEmanetSinirda;

    /// <summary>YuzeGoreYukle'den bir kez (yalnız Yıldız).</summary>
    private void YildizEkYukleBaslat()
    {
        if (yildizEkYuklendi) return;
        yildizEkYuklendi = true;
        Yuklemeler.AddRange([YildizGunSonuYukle(), YildizEmanetYukle()]);
    }

    private async Task YildizGunSonuYukle()
    {
        if (!await Oturum.RaporRoluBekle()) { yildizDunKasalar = []; return; }   // Görsel denetim 2: rapor rolü yoksa istek yok
        try
        {
            var dun = DateOnly.FromDateTime(Bicim.IsletmeBugun).AddDays(-1);
            var y = await Rapor.KasaDefteri(dun, dun);
            yildizDunKasalar = YildizBugunBicim.DunKapanmayanKasalar(y, dun);
        }
        // Rapor/kasa yetkisi olmayan rol (TEZGAHTAR) gün sonu kapatamaz → satır yok, bant yok.
        catch (ApiHata ex) when (ex.StatusCode == 403) { yildizDunKasalar = []; }
        catch (Exception ex) when (YukleHatasi(ex)) { yildizGunSonuHata = HataMetni(ex); }
        await InvokeAsync(StateHasChanged);
    }

    private async Task YildizEmanetYukle()
    {
        try
        {
            const int CariSiniri = 200;
            var cariler = await Api.GetAsync<List<Kuyumcu.Contracts.Sayfalar.Cariler.CariVeri>>($"/cari?aktif=true&limit={CariSiniri}");
            var emanetli = cariler.Where(c => c.EmanetteHas > 0m).OrderByDescending(c => c.EmanetteHas).ToList();
            yildizEmanetSinirda = cariler.Count >= CariSiniri || emanetli.Count > YildizBugunBicim.EmanetOkumaSiniri;
            await SubeliSorgu.HazirAsync(Oturum);
            var bugun = DateOnly.FromDateTime(Bicim.IsletmeBugun);
            var satirlar = new List<YildizBekleyenSatir>();
            foreach (var c in emanetli.Take(YildizBugunBicim.EmanetOkumaSiniri))
            {
                var e = await Api.GetAsync<Kuyumcu.Contracts.Sayfalar.Emanet.EmanetYaniti>(
                    SubeliSorgu.Ekle($"/emanet/{Uri.EscapeDataString(c.Id)}", Oturum));
                var bas = YildizBugunBicim.EmanetBaslangici(
                    (e.Hareketler ?? []).Select(h => (h.Tarih, h.HasGrami, h.Maden)), e.SinirDegdi);
                var gun = YildizBugunBicim.EmanetGunu(bas, bugun);
                if (!YildizBugunBicim.EmanetSatiriCikar(gun)) continue;
                satirlar.Add(new($"emanet-{c.Id}",
                    YildizBugunSozlugu.Emaneti(c.Ad, YildizBugunBicim.Iyelik(c.Ad), Dil.Dil),
                    YildizBugunBicim.HasIki(e.MevcutHasGramBakiye, YildizBugunSozlugu.GramHas(Dil.Dil)),
                    YildizBugunSozlugu.EmanetAsti(YildizBugunBicim.EmanetEsikGun, Dil.Dil), false,
                    YildizBugunSozlugu.Ac(Dil.Dil), "/emanet",
                    YildizBugunSozlugu.EmanetIpucu(gun!.Value, Dil.Dil)));
            }
            yildizEmanetSatirlari = satirlar;
        }
        // Cari ya da emanet modülü yetkisi yoksa satır yok (uydurma yok), bant yok.
        catch (ApiHata ex) when (ex.StatusCode == 403) { yildizEmanetSatirlari = []; }
        catch (Exception ex) when (YukleHatasi(ex)) { yildizEmanetHata = HataMetni(ex); }
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>"Dünkü gün sonu kapatılmadı" satırı (yoksa boş liste).</summary>
    private IEnumerable<YildizBekleyenSatir> YildizGunSonuSatiri() => yildizDunKasalar.Count == 0 ? []
        : [new("gun-sonu-dun", YildizBugunSozlugu.DunGunSonu(Dil.Dil), null, YildizBugunSozlugu.KaynakKasa(Dil.Dil), false,
               YildizBugunSozlugu.Kapat(Dil.Dil), "/kasalar", YildizBugunSozlugu.DunGunSonuIpucu(string.Join(", ", yildizDunKasalar), Dil.Dil))];
}
