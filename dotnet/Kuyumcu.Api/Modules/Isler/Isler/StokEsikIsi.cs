using System.Globalization;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Isler.Isler;

/// <summary>
/// D1-15 (18.10, rakip eşitleme: "eşik altı stok bildirimi") — ürün kartındaki asgari stoğun (MinStok) altına
/// düşen aktif ürünler için günde bir bildirim. Stok TÜM şubelerin toplamıdır (ürün kartı şube ayırmaz).
/// ⚠ GÜRÜLTÜ SINIRI: ürün sayısı Isler:StokEsik:MaksUrun'u aşarsa tek tek değil TEK özet bildirim (TB-19).
/// </summary>
public sealed class StokEsikIsi : IZamanlanmisIs
{
    public const string IsKodu = "STOK_ESIK";
    public const string Rota = "/urunler";

    public string Kod => IsKodu;
    public string Ad => "Eşik altı stok";

    public bool Etkin(IConfiguration y) => y.GetValue("Isler:StokEsik:Etkin", true);

    public string CalismaAnahtari(DateOnly yerelGun) => IsAnahtarlari.Gunluk(yerelGun);

    public async Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        var y = baglam.Kapsam.GetRequiredService<IConfiguration>();
        var maks = Math.Clamp(y.GetValue("Isler:StokEsik:MaksUrun", 100), 1, 2000);
        var db = baglam.Db;
        var esikli = await db.Urunler.AsNoTracking()
            .Where(u => u.TenantId == baglam.TenantId && u.Aktif && u.MinStok != null && u.MinStok > 0)
            .Select(u => new { u.Id, u.Ad, u.Kod, Min = u.MinStok!.Value })
            .Take(5000)
            .ToListAsync(iptal);
        if (esikli.Count == 0) return IsCikti.Bos;
        var idler = esikli.Select(u => u.Id).ToList();
        var stoklar = await db.Stoklar.AsNoTracking()
            .Where(s => s.TenantId == baglam.TenantId && idler.Contains(s.UrunId))
            .GroupBy(s => s.UrunId)
            .Select(g => new { UrunId = g.Key, Adet = g.Sum(x => x.Adet) })
            .ToDictionaryAsync(x => x.UrunId, x => x.Adet, iptal);
        var altinda = esikli
            .Select(u => (u.Id, u.Ad, u.Kod, u.Min, Adet: stoklar.GetValueOrDefault(u.Id)))
            .Where(u => u.Adet < u.Min)
            .OrderBy(u => u.Adet - u.Min).ThenBy(u => u.Ad, StringComparer.Ordinal)
            .ToList();
        if (altinda.Count == 0) return new IsCikti([], esikli.Count);
        if (altinda.Count > maks)
            return new IsCikti(
            [
                new BildirimTaslagi(IsKodu, BildirimOnem.Uyari, "Asgari stoğun altında çok sayıda ürün var",
                    FormattableString.Invariant($"{altinda.Count} ürünün stoğu asgari stoğun altında. Tek tek bildirim üretilmedi; liste ürünler ekranındadır."),
                    Rota, null, null, IsAnahtarlari.Tekillik("STOK-ESIK-OZET", altinda.Count.ToString(CultureInfo.InvariantCulture), baglam.YerelGun), null),
            ], esikli.Count);
        return new IsCikti([.. altinda.Select(u => new BildirimTaslagi(
            IsKodu, u.Adet <= 0m ? BildirimOnem.Kritik : BildirimOnem.Uyari,
            u.Adet <= 0m ? "Stok bitti" : "Stok asgari seviyenin altında",
            string.Create(CultureInfo.GetCultureInfo("tr-TR"), $"{u.Ad} ({u.Kod}): stokta {u.Adet:0.###}, asgari {u.Min:0.###}."),
            Rota, "Urun", u.Id, IsAnahtarlari.Tekillik("STOK-ESIK", u.Id, baglam.YerelGun), null))], esikli.Count);
    }
}
