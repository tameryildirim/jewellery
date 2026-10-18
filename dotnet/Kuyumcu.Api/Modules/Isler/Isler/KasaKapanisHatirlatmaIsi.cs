using System.Globalization;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Isler.Isler;

/// <summary>
/// D1-11 (18.10, rakip eşitleme: "kapanış hatırlatması") — akşam saatinden (yerel 19:00 — <see cref="Saat"/>)
/// sonra, BUGÜN hareketi olup gün kapanışı yapılmamış (ya da yeniden açılmış) aktif kasalar için günde bir bildirim.
/// Saat gelmeden çalışma anahtarı "-erken" ekiyle ayrı tutulur: erken tur boş döner, saat gelince günlük anahtar
/// bir kez koşar (iş durumsuzdur; anahtar tek kaynaktır). Saat yapılandırmadan OKUNMAZ: anahtar ile koşum aynı
/// saate bakmazsa günlük anahtar erken tüketilir ve o gün hatırlatma hiç çıkmaz.
/// </summary>
public sealed class KasaKapanisHatirlatmaIsi : IZamanlanmisIs
{
    public const string IsKodu = "KASA_KAPANIS";
    public const string Rota = "/kasalar";
    public const int Saat = 19;

    public string Kod => IsKodu;
    public string Ad => "Gün kapanışı hatırlatma";

    public bool Etkin(IConfiguration y) => y.GetValue("Isler:KasaKapanis:Etkin", true);

    /// <summary>Saat dolmadan "-erken" anahtarı (o tur boş döner); dolunca günlük anahtar.</summary>
    public string CalismaAnahtari(DateOnly yerelGun) =>
        SaatGeldi(DateTime.UtcNow, Saat) ? IsAnahtarlari.Gunluk(yerelGun) : IsAnahtarlari.Gunluk(yerelGun) + "-erken";

    internal static bool SaatGeldi(DateTime anUtc, int saat) => Zaman.UtcdenYerele(anUtc).Hour >= saat;

    public async Task<IsCikti> Calistir(IsBaglami baglam, CancellationToken iptal)
    {
        if (!SaatGeldi(baglam.AnUtc, Saat)) return IsCikti.Bos;

        var db = baglam.Db;
        var gun = baglam.YerelGun;
        var (bas, son) = Zaman.YerelGunAraligiUtc(gun);
        var kasalar = await db.Kasalar.AsNoTracking()
            .Where(k => k.TenantId == baglam.TenantId && k.Aktif)
            .Select(k => new { k.Id, k.Ad })
            .Take(500).ToListAsync(iptal);
        if (kasalar.Count == 0) return IsCikti.Bos;
        var idler = kasalar.Select(k => k.Id).ToList();
        var hareketli = await db.KasaHareketleri.AsNoTracking()
            .Where(h => h.TenantId == baglam.TenantId && idler.Contains(h.KasaId) && h.Tarih >= bas && h.Tarih < son)
            .Select(h => h.KasaId).Distinct().ToListAsync(iptal);
        var kapali = await db.GunKapanislari.AsNoTracking()
            .Where(g => g.TenantId == baglam.TenantId && g.YerelGun == gun && g.AcilisZamani == null)
            .Select(g => g.KasaId).ToListAsync(iptal);
        var acik = kasalar.Where(k => hareketli.Contains(k.Id) && !kapali.Contains(k.Id)).ToList();
        return new IsCikti([.. acik.Select(k => new BildirimTaslagi(
            IsKodu, BildirimOnem.Uyari, "Gün kapanışı yapılmadı",
            string.Create(CultureInfo.GetCultureInfo("tr-TR"), $"{k.Ad}: bugün hareket var, gün sonu sayımı ve kapanışı henüz yapılmadı."),
            Rota, "Kasa", k.Id, IsAnahtarlari.Tekillik("KASA-KAPANIS", k.Id, gun), null))], kasalar.Count);
    }
}
