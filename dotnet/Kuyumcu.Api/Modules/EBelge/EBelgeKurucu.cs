using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.EBelge;

/// <summary>
/// E-3 — kesilmiş fatura → entegratörden bağımsız e-belge modeli. Eksik zorunlu bilgi gönderimi DURDURUR
/// (kalıcı hata, sade dille); hiçbir alan tahminle doldurulmaz:
///   • işletmenin VKN/TCKN'si ve unvanı (İşletme ayarları),
///   • alıcının VKN/TCKN'si (faturaya kesim anında dondurulan alıcı bilgisi),
///   • kuyum (özel matrah) satırı varsa özel matrah kodu (Kdv:OzelMatrahKodu — mali müşavir teyidi K-10).
/// Tutarlar faturadan AYNEN gelir: satır toplamı KDV dahildir; net = satır toplamı − KDV.
/// </summary>
public static class EBelgeKurucu
{
    public static async Task<EBelgeModeli> Kur(KuyumcuDbContext db, Data.Fatura f, Guid ettn, string senaryo, string? aliciEtiketi,
        string? seriOnEki, CancellationToken iptal = default, string? sablon = null)
    {
        var isletme = await db.Tenants.AsNoTracking().FirstAsync(t => t.Id == f.TenantId, iptal);
        var kalemler = await db.FaturaKalemleri.AsNoTracking()
            .Where(k => k.TenantId == f.TenantId && k.FaturaId == f.Id).OrderBy(k => k.Sira).Take(1000).ToListAsync(iptal);
        var cari = await db.Cariler.AsNoTracking().Where(c => c.Id == f.CariId && c.TenantId == f.TenantId)
            .Select(c => new { c.Email, c.Telefon }).FirstOrDefaultAsync(iptal);
        return Kur(f, kalemler, isletme, cari?.Email, cari?.Telefon, ettn, senaryo, aliciEtiketi, seriOnEki, sablon);
    }

    public static EBelgeModeli Kur(Data.Fatura f, IReadOnlyList<FaturaKalemi> kalemler, Tenant isletme, string? aliciEposta, string? aliciTelefon,
        Guid ettn, string senaryo, string? aliciEtiketi, string? seriOnEki, string? sablon = null)
    {
        if (string.IsNullOrWhiteSpace(isletme.VergiNo))
            throw new EBelgeHatasi("İşletmenin vergi numarası eksik. İşletme ayarlarına yazın, sonra yeniden deneyin.", false);
        if (string.IsNullOrWhiteSpace(f.AliciVergiNo))
            throw new EBelgeHatasi("Müşterinin TC ya da vergi numarası yok. Müşteri kartına yazın, sonra yeniden deneyin.", false);

        var satirlar = new List<EBelgeSatiri>();
        if (kalemler.Count > 0)
        {
            foreach (var k in kalemler)
            {
                var net = k.SatirToplam - k.KdvTutar;
                var ozel = k.KdvTuru == "KUYUM" && k.HasDeger > 0m;
                satirlar.Add(new(Ad(k.Aciklama, k.TasIbaresi), k.Miktar, Birim(k.Birim), k.Miktar == 0m ? net : Math.Round(net / k.Miktar, 8),
                    net, Math.Round(k.KdvOran * 100m, 2), k.KdvTutar, ozel ? k.KdvMatrah : null, KuyumAyrintisi(k)));
            }
        }
        else
        {
            // Kalemsiz (başlık) fatura: tek satır, faturanın toplamlarıyla.
            var net = f.Toplam - f.KdvTutar;
            var ozel = f.KdvTuru == "KUYUM" && f.HasDeger > 0m;
            satirlar.Add(new(string.IsNullOrWhiteSpace(f.Aciklama) ? "Satış" : Kisalt(f.Aciklama!, 200), 1m, "C62", net, net,
                Math.Round(f.KdvOran * 100m, 2), f.KdvTutar, ozel ? net - f.HasDeger : null));
        }
        var ozelVar = satirlar.Any(s => s.OzelMatrahTutar is not null);
        if (ozelVar && string.IsNullOrWhiteSpace(f.OzelMatrahKodu))
            throw new EBelgeHatasi("Kuyum satırının özel matrah kodu tanımlı değil (mali müşavir teyidi bekleniyor). Kod tanımlanınca yeniden deneyin.", false);

        var satici = new EBelgeTaraf(isletme.VergiNo!.Trim(), isletme.Ad, isletme.VergiDairesi, isletme.Adres, null, null, null, isletme.Telefon);
        var alici = new EBelgeTaraf(f.AliciVergiNo!.Trim(), f.AliciUnvan ?? "", null, f.AliciAdres, null, null,
            string.IsNullOrWhiteSpace(aliciEposta) ? null : aliciEposta.Trim(), string.IsNullOrWhiteSpace(aliciTelefon) ? null : aliciTelefon.Trim());
        var notlar = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.FaturaNo)) notlar.Add($"Mihenk belge no: {f.FaturaNo}");
        return new(ettn, senaryo, ozelVar ? "OZELMATRAH" : "SATIS", Zaman.UtcdenYerele(f.Tarih), f.ParaBirimi == "TL" ? "TRY" : f.ParaBirimi,
            string.IsNullOrWhiteSpace(seriOnEki) ? null : seriOnEki.Trim().ToUpperInvariant(), satici, alici,
            senaryo == EBelgeSenaryolari.EFatura ? aliciEtiketi : null, satirlar, ozelVar ? f.OzelMatrahKodu : null, notlar, sablon);
    }

    private static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>"22 ayar · 10,00 g · has 9,1600 g · işçilik 1.200,00 TL" — yalnız kalemde dolu olan parçalar; hiçbiri yoksa null.</summary>
    public static string? KuyumAyrintisi(FaturaKalemi k)
    {
        var p = new List<string>();
        if (!string.IsNullOrWhiteSpace(k.AyarId))
            p.Add(k.AyarId!.Trim().ToUpperInvariant() switch
            {
                "GUMUS" => "Gümüş",
                var a when a.Length > 1 && a[0] == 'K' && int.TryParse(a[1..], out var n) => $"{n} ayar",
                var a => a,
            });
        if (k.GramAgirlik is { } g && g > 0m) p.Add($"{g.ToString("N2", Tr)} g");
        if (k.HasGrami is { } h && h > 0m) p.Add($"has {h.ToString("N4", Tr)} g");
        if (k.IscilikTl is { } i && i > 0m) p.Add($"işçilik {i.ToString("N2", Tr)} TL");
        return p.Count == 0 ? null : string.Join(" · ", p);
    }

    private static string Ad(string aciklama, string? tas) =>
        Kisalt(string.IsNullOrWhiteSpace(tas) ? aciklama : $"{aciklama} ({tas})", 200);

    private static string Kisalt(string s, int n) => s.Length <= n ? s : s[..n];

    /// <summary>Birim → UBL-TR birim kodu (C62 adet, GRM gram, KGM kilogram). Bilinmeyen birim adet sayılmaz: olduğu gibi gider.</summary>
    public static string Birim(string? b) => (b ?? "").Trim().ToUpperInvariant() switch
    {
        "ADET" or "" => "C62",
        "GRAM" or "GR" or "G" => "GRM",
        "KG" or "KILOGRAM" => "KGM",
        var x => x,
    };
}
