using System.Globalization;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Isler.Isler;

/// <summary>
/// D1-06 (18.10) — gecikmiş taksit hatırlatması: etkin ödeme planlarında vadesi geçmiş ve ödenmemiş taksit varsa
/// plan başına günde bir bildirim (müşteri adı bildirim GÖVDESİNE yazılmaz — kişisel veri; ilgili varlık cari).
/// Taksit durumu OdemePlaniServisi.Ozetle ile AYNI hesaptan gelir (tek tanım).
/// </summary>
public sealed class TaksitHatirlatmaIsi : IZamanlanmisIs
{
    public const string IsKodu = "TAKSIT_HATIRLATMA";
    public const string Rota = "/cariler";

    public string Kod => IsKodu;
    public string Ad => "Gecikmiş taksit";

    public bool Etkin(IConfiguration y) => y.GetValue("Isler:TaksitHatirlatma:Etkin", true);

    public string CalismaAnahtari(DateOnly yerelGun) => IsAnahtarlari.Gunluk(yerelGun);

    public async Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        var db = baglam.Db;
        var planlar = await db.OdemePlanlari.AsNoTracking()
            .Where(p => p.TenantId == baglam.TenantId && p.Durum == OdemePlani.Aktif)
            .OrderBy(p => p.Olusturuldu).Take(500).ToListAsync(iptal);
        if (planlar.Count == 0) return IsCikti.Bos;
        var servis = new Cari.OdemePlaniServisi(db, new DenetimServisi(db, new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        var ozetler = await servis.Ozetle(planlar, baglam.TenantId);
        var taslaklar = new List<BildirimTaslagi>();
        foreach (var o in ozetler)
        {
            var geciken = o.Taksitler.Where(t => t.Durum == "GECIKTI").ToList();
            if (geciken.Count == 0) continue;
            var tutar = geciken.Sum(t => t.TutarTl - t.OdenenTl);
            taslaklar.Add(new BildirimTaslagi(IsKodu, BildirimOnem.Uyari, "Gecikmiş taksit",
                string.Create(CultureInfo.GetCultureInfo("tr-TR"), $"{geciken.Count} taksit gecikti · {tutar:N2} TL. Ayrıntı müşterinin ödeme planında."),
                Rota, "Cari", o.CariId, IsAnahtarlari.Tekillik("TAKSIT", o.Id, baglam.YerelGun), null));
        }
        return new IsCikti(taslaklar, planlar.Count);
    }
}
