using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-5B — Banka "Yıldız" yüzü (tuval Y_Banka · BİREBİR · boş durum kalıbı; dolu liste
// tuvalde çizilmedi → liste kalıbı, belirsiz eşleşme). Yalnız GÖRÜNÜM + OKUMA: satırlar sayfanın
// MEVCUT hesap listesidir (GET /banka/hesaplar). "Hesap ekle" MEVCUT yeni hesap penceresi,
// "Hareketler" MEVCUT HesapSec (özgün ekranın hareket tablosu); dekont, ekstre, storno ve
// düzenleme "Tüm ayrıntılar"da. ⚠ Banka.razor davranış kodu başka şeridindir — yalnız Yıldız dalı.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Banka
{
    [Inject] private IServiceProvider Hizmetler { get; set; } = default!;

    private bool YildizOzgunCiz => yildizAyrinti;
    private string? YildizOzgunIsaret => "banka";
    private bool yildizAyrinti;

    private bool YildizBosMu => !yildizAyrinti && !yukleniyor && listeHata is null && hesaplar.Count == 0;

    private async Task YildizHareketler(string id)
    {
        yildizAyrinti = true;
        await HesapSec(id);
    }

    private YildizBosBaglami YildizBosBaglami => new()
    {
        Kok = "yildiz-banka",
        Ust = YildizBankaSozlugu.Ust(Dil.Dil),
        Baslik = YildizListeBicim.EkranAdi("/banka", Dil),
        BosBaslik = YildizBankaSozlugu.BosBaslik(Dil.Dil),
        BosMetin = YildizBankaSozlugu.BosMetin(Dil.Dil),
        B1Etiket = YildizBankaSozlugu.HesapEkle(Dil.Dil), B1Id = "yildiz-banka-b1",
        B1Tikla = EventCallback.Factory.Create(this, YeniHesapModalAc),
        Bantlar = YildizBant.Ciz(listeHata, basari, basariJeton, 3000),
        AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
    };

    private YildizListeBaglami YildizBaglami
    {
        get
        {
            var dil = Dil.Dil;
            return new YildizListeBaglami
            {
                Kok = "yildiz-banka", Tur = "banka",
                Ust = YildizBankaSozlugu.Ust(dil),
                Baslik = YildizListeBicim.EkranAdi("/banka", Dil),
                B1Etiket = YildizBankaSozlugu.HesapEkle(dil), B1Id = "yildiz-banka-b1",
                B1Tikla = EventCallback.Factory.Create(this, YeniHesapModalAc),
                Sutunlar = [new(YildizBankaSozlugu.Hesap(dil)), new(YildizBankaSozlugu.Iban(dil)), new(YildizBankaSozlugu.Bakiye(dil), Sag: true)],
                EylemSutunu = true,
                Satirlar = [.. hesaplar.Select(h => new YildizListeSatiri(h.Id,
                [
                    new(string.IsNullOrWhiteSpace(h.HesapAdi) ? h.BankaAdi : $"{h.BankaAdi} · {h.HesapAdi}", YildizHucreTuru.Ad, $"yildiz-banka-ad-{h.Id}"),
                    new((h.Aktif ? h.Iban : $"{h.Iban} · {YildizBankaSozlugu.KullanimdaDegil(dil)}")
                        + (h.SaglayiciKodu == "SAHTE" ? $" · {GelenOdemeSozlugu.DemoBanka(dil)}" : "")),
                    new(YildizParaBicim.Tutar(h.Bakiye, h.ParaBirimi), YildizHucreTuru.Deger, $"yildiz-banka-bakiye-{h.Id}"),
                ],
                new YildizEylem(YildizBankaSozlugu.Hareketler(dil), $"yildiz-banka-hareket-{h.Id}", EventCallback.Factory.Create(this, () => YildizHareketler(h.Id))),
                Soluk: !h.Aktif))],
                Yukleniyor = yukleniyor,
                Bantlar = YildizBantlari,
                AyrintiAc = EventCallback.Factory.Create(this, () => yildizAyrinti = true),
                AyrintiAcik = yildizAyrinti,
                AyrintiKapat = EventCallback.Factory.Create(this, () => yildizAyrinti = false),
                // M7: demo bankasına bağlı hesap varsa "Havale gönder (demo)".
                EkEtiket = SahteHesaplar.Count > 0 ? GelenOdemeSozlugu.HavaleGonder(dil) : null,
                EkId = "yildiz-banka-havale",
                EkTikla = EventCallback.Factory.Create(this, () => sahteHavaleAcik = true),
            };
        }
    }
}
