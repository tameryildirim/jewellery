using System.Globalization;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Hurda;

namespace Kuyumcu.Api.Modules.Isler.Isler;

/// <summary>
/// PAKET YASAL-4 / Y4-B — STOPAJ_HATIRLATMA. Kalanı olan dönemin son ödeme gününe
/// <see cref="StopajServisi.UyariGunu"/> gün ya da daha az kalınca UYARI, son gün
/// geçmişse KRİTİK bildirim üretir. Hesap YENİDEN YAZILMAZ: StopajServisi.Ozet
/// (Panel ile aynı türetme). Kişisel veri taşımaz (dönem ve tutar).
/// Son gün TEYİTSİZ bir yasal parametreden gelir (STOPAJ_BEYAN_GUNU, T-3) —
/// parametre yoksa dönem hatırlatılmaz (gün uydurulmaz).
/// </summary>
public sealed class StopajHatirlatmaIsi : IZamanlanmisIs
{
    public const string IsKodu = "STOPAJ_HATIRLATMA";
    public const string Rota = "/gider-pusulalari?sekme=stopaj";
    public const string Roller = "SAHIP,MUHASEBE";

    public string Kod => IsKodu;
    public string Ad => "Stopaj ödeme hatırlatması";

    public bool Etkin(IConfiguration y) => y.GetValue("Isler:Stopaj:Etkin", true);

    public string CalismaAnahtari(DateOnly yerelGun) => IsAnahtarlari.Gunluk(yerelGun);

    public async Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        var servis = baglam.Kapsam.GetRequiredService<StopajServisi>();
        var ozet = await servis.Ozet(baglam.TenantId, baglam.AnUtc);
        var tr = CultureInfo.GetCultureInfo("tr-TR");

        var taslaklar = new List<BildirimTaslagi>();
        foreach (var d in ozet.Gecikmis)
            taslaklar.Add(new BildirimTaslagi(IsKodu, BildirimOnem.Kritik,
                "Stopaj ödeme günü GEÇTİ",
                $"{d.Donem} dönemi stopajı {d.KalanTl.ToString("N2", tr)} TL; son gün {d.SonGun!.Value.ToString("dd.MM.yyyy", tr)} geçti.",
                Rota, "StopajDonemi", d.Donem, IsAnahtarlari.Tekillik("STOPAJ", d.Donem, baglam.YerelGun), Roller));
        foreach (var d in ozet.Yaklasan)
            taslaklar.Add(new BildirimTaslagi(IsKodu, BildirimOnem.Uyari,
                "Stopaj ödeme günü yaklaşıyor",
                $"{d.Donem} dönemi stopajı {d.KalanTl.ToString("N2", tr)} TL; son gün {d.SonGun!.Value.ToString("dd.MM.yyyy", tr)}.",
                Rota, "StopajDonemi", d.Donem, IsAnahtarlari.Tekillik("STOPAJ", d.Donem, baglam.YerelGun), Roller));

        return new IsCikti(taslaklar, ozet.Gecikmis.Count + ozet.Yaklasan.Count);
    }
}
