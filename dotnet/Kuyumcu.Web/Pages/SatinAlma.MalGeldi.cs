using Kuyumcu.Contracts.Bilesenler.MasakFormu;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Contracts.Sayfalar.Fiyatlandirma;
using Kuyumcu.Contracts.Sayfalar.SatinAlma;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// 17.10 kurucu: stok girişi tek adımlı "Mal geldi". Sipariş formunun AYNISI (tek düzenleme kalıbı) "Mal geldi"
// kipinde açılır; Stoğa al → POST /satinalma/mal-geldi (sipariş + onay + teslim alma tek istekte, sunucuda).
//   • Kur kullanıcıdan istenmez (UX-S21) — sunucunun güncel kuru.
//   • MASAK: tutar için kimlik gerekiyorsa SUNUCU söyler (/masak/gereksinim, tedarikçinin bugünkü toplamıyla);
//     ilk Stoğa al'da sorulur, gerekiyorsa ortak MasakFormu aynı çekmecede açılır ve eksiksiz olmadan gitmez.
// ═════════════════════════════════════════════════════════════════════════════
public partial class SatinAlma
{
    private bool malGeldiKipi;
    private MasakGereksinim? fMasakGereksinim;
    private decimal fGereksinimTutari = -1m;

    private decimal FormTutari => satirlar.Sum(r => r.Adet is > 0m && r.BirimFiyatTl is >= 0m
        ? Math.Round(r.Adet!.Value * r.BirimFiyatTl!.Value, 2, MidpointRounding.AwayFromZero) : 0m);

    /// <summary>Bu tutar için beyan formu açık mı (sunucu "kimlik gerekli" dedi).</summary>
    private bool FormMasakAcik => malGeldiKipi && fMasakGereksinim is { KimlikGerekli: true } && masakEsik is not null
        && fGereksinimTutari == FormTutari;

    private string? FormMasakKilidi =>
        !FormMasakAcik ? null
        : MasakFormu.Eksiksiz(masak, fMasakGereksinim!) ? null
        : MasakFormu.TcknKilitMetni(masak, Dil.Dil) ?? M.MasakKabulKilit;

    private void MalGeldiAc()
    {
        YeniAc();
        if (!formAcik) return;
        malGeldiKipi = true;
        fMasakGereksinim = null; fGereksinimTutari = -1m; masak = new();
    }

    private async Task MalGeldiKaydet()
    {
        formDenendi = true;
        if (FormKilidi is not null || kaydediliyor) return;
        kaydediliyor = true; formHata = null;
        try
        {
            var t = FormTutari;
            if (t != fGereksinimTutari || fMasakGereksinim is null)
            {
                masakEsik ??= await Api.GetAsync<Esikler>("/masak/esikler");
                fMasakGereksinim = await Uyum.Gereksinim(t, fTedarikciId);
                fGereksinimTutari = t;
                if (fMasakGereksinim.KimlikGerekli) { formHata = MalGeldiSozlugu.MasakDoldurun(Dil.Dil); kaydediliyor = false; return; }
            }
            var kurId = (await Api.GetAsync<KurOzet>("/kur/guncel")).Id;
            if (string.IsNullOrEmpty(kurId)) { formHata = M.KurYok; kaydediliyor = false; return; }
            var sonuc = await Api.PostAsync<MalKabulSonucu>("/satinalma/mal-geldi", new
            {
                tedarikciCariId = fTedarikciId, kurSnapshotId = kurId, aciklama = Bos(fAciklama),
                kalemler = KalemGovdesi(), masak = FormMasakAcik ? masak : null,
            });
            basari = MalGeldiSozlugu.Bitti(sonuc.IrsaliyeNo, Dil.Dil);
            formAcik = false; malGeldiKipi = false; masak = new();   // kişisel veri formda kalmaz
            detay = sonuc.Siparis;   // UX-S27: sonuç aynı ekranda — kapanan sipariş ve irsaliyesi açılır
            kabulAdetleri.Clear();
            await Yukle();
        }
        catch (Exception ex) when (ex is ApiHata or HttpRequestException or System.Text.Json.JsonException) { formHata = ex.Message; }
        kaydediliyor = false;
    }
}
