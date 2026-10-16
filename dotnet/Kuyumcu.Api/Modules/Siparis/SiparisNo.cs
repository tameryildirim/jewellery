using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Siparis;

// ═════════════════════════════════════════════════════════════════════════════
// SİPARİŞ NUMARASI — kiracı içinde tekil, insan okunur (S-YYYY-NNNNNN).
//
// ⚠ SAYAÇ TABLOSU AÇILMADI (Bertan kararı): tekilliği VERİTABANI verir —
//   (tenant_id, no) UNIQUE indeks. Uygulama "önce SELECT et, sonra yaz" ile
//   karar vermez; yarış hâlinde INSERT patlar ve SINIRLI bir döngüyle yeniden
//   denenir. Döngü tükenirse 409 döner — SESSİZ İKİNCİ ULID ÜRETİLMEZ.
//
// ⚠ KİRACI BAZINDA artar, şube bazında değil: şube bazlı numara talebi
//   DOĞRULANMADI (piyasa araştırması dosyalarında birebir kanıt yok), bu yüzden
//   varsayım yapılmadı — tutucu olan kiracı bazlıdır.
// ═════════════════════════════════════════════════════════════════════════════

public static class SiparisNo
{
    /// <summary>Çakışmada yeniden deneme sayısı. Tükenirse 409 (Bertan koşulu).</summary>
    public const int AzamiDeneme = 5;

    /// <summary>Numara ön eki: S-2026-</summary>
    public static string Onek(int yil) => $"S-{yil}-";

    /// <summary>
    /// Kiracının o yıl için bir SONRAKİ numarasını üretir.
    /// Kaynak: mevcut en büyük numaranın sıra kısmı + 1 (boşluk oluşabilir; numara
    /// kesintisiz olduğunu İDDİA ETMEZ — iptal edilen sipariş numarasını korur).
    /// </summary>
    public static async Task<string> Uret(KuyumcuDbContext db, string tenantId, int yil)
    {
        var onek = Onek(yil);
        var enBuyuk = await db.Siparisler.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.No.StartsWith(onek))
            .OrderByDescending(s => s.No)
            .Select(s => s.No)
            .FirstOrDefaultAsync();

        var sonraki = 1;
        if (enBuyuk is not null && int.TryParse(enBuyuk[onek.Length..], out var mevcut))
            sonraki = mevcut + 1;

        return Bicimle(yil, sonraki);
    }

    /// <summary>Sıra numarasını biçimler (altı hane, taşarsa daha uzun).</summary>
    public static string Bicimle(int yil, int sira) =>
        $"{Onek(yil)}{sira.ToString("D6", System.Globalization.CultureInfo.InvariantCulture)}";

    /// <summary>Numaranın sıra kısmını bir artırır (yeniden denemede).</summary>
    public static string Artir(string no)
    {
        var tire = no.LastIndexOf('-');
        if (tire < 0 || !int.TryParse(no[(tire + 1)..], out var sira)) return no;
        var yilKismi = no[..tire];
        return $"{yilKismi}-{(sira + 1).ToString("D6", System.Globalization.CultureInfo.InvariantCulture)}";
    }
}
