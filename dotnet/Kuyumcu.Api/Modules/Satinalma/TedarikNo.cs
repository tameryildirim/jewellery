using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Satinalma;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-26 — TEDARİK BELGE NUMARASI (satınalma siparişi + irsaliye).
//
// ⚠ İKİNCİ BİR NUMARA MOTORU DEĞİLDİR: SiparisNo ile AYNI sözleşmeyi uygular
//   (kiracı bazlı, veritabanı UNIQUE indeksi tekilliği verir, çakışmada sınırlı
//   döngü, döngü tükenirse 409 — sessiz ikinci ULID üretilmez). SiparisNo
//   doğrudan `db.Siparisler`e bağlı olduğu için yeniden kullanılamadı; kural
//   kopyalanmadı, ÖN EKİ PARAMETRE olan tek gövde yazıldı ve her iki tedarik
//   belgesi bunu çağırır.
//
// ⚠ SAYAÇ TABLOSU AÇILMADI: "önce SELECT et sonra yaz" yarış hâlinde iki kayda
//   aynı numarayı verir; kural veritabanındadır.
// ═════════════════════════════════════════════════════════════════════════════

public static class TedarikNo
{
    /// <summary>Çakışmada yeniden deneme sayısı. Tükenirse 409.</summary>
    public const int AzamiDeneme = 5;

    /// <summary>Satınalma siparişi ön eki: SAS-2026-</summary>
    public const string SatinalmaOnek = "SAS";

    /// <summary>İrsaliye ön eki: IRS-2026-</summary>
    public const string IrsaliyeOnek = "IRS";

    public static string Onek(string tur, int yil) => $"{tur}-{yil}-";

    /// <summary>Mevcut en büyük numaranın sıra kısmı + 1. Boşluk oluşabilir —
    /// numaranın kesintisiz olduğu İDDİA EDİLMEZ (iptal numarasını korur).</summary>
    public static async Task<string> Uret(
        KuyumcuDbContext db, string tenantId, string tur, int yil)
    {
        var onek = Onek(tur, yil);
        var enBuyuk = tur == SatinalmaOnek
            ? await db.SatinalmaSiparisleri.AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.No.StartsWith(onek))
                .OrderByDescending(s => s.No).Select(s => s.No).FirstOrDefaultAsync()
            : await db.Irsaliyeler.AsNoTracking()
                .Where(i => i.TenantId == tenantId && i.No.StartsWith(onek))
                .OrderByDescending(i => i.No).Select(i => i.No).FirstOrDefaultAsync();

        var sonraki = 1;
        if (enBuyuk is not null && int.TryParse(enBuyuk[onek.Length..], out var mevcut))
            sonraki = mevcut + 1;

        return Bicimle(tur, yil, sonraki);
    }

    public static string Bicimle(string tur, int yil, int sira) =>
        $"{Onek(tur, yil)}{sira.ToString("D5", System.Globalization.CultureInfo.InvariantCulture)}";

    /// <summary>Numaranın sıra kısmını bir artırır (yeniden denemede).</summary>
    public static string Artir(string no)
    {
        var tire = no.LastIndexOf('-');
        if (tire < 0 || !int.TryParse(no[(tire + 1)..], out var sira)) return no;
        return $"{no[..tire]}-{(sira + 1).ToString("D5", System.Globalization.CultureInfo.InvariantCulture)}";
    }
}
