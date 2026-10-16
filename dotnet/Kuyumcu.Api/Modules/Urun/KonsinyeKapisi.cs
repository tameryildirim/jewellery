using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Urun;

// ═════════════════════════════════════════════════════════════════════════════
// KONSİNYE KAPISI
//
// Konsinye mal bize AİT DEĞİLDİR. Vitrinde durur, satılınca bedeli sahibine
// borçlanılır. Kendi malımızla karıştırılırsa iki şey birden bozulur:
//   • STOK DEĞERİ şişer — başkasının malını kendi varlığımız sanırız
//   • KÂR şişer — maliyetsiz mal satmış gibi görünürüz
//
// Bu yüzden konsinye ürün, sahibi ve birim bedeli bildirilmeden kaydedilemez:
// bedel yoksa satışta borç doğmaz ve kâr yalan söyler. Fail-closed.
// ═════════════════════════════════════════════════════════════════════════════

public static class KonsinyeKapisi
{
    /// <summary>Ürün kartı kaydedilirken/güncellenirken konsinye tutarlılığı.</summary>
    public static void Dogrula(bool konsinye, string? cariId, decimal? birimBedel)
    {
        if (!konsinye)
        {
            // Konsinye olmayan üründe konsinye alanları dolu olamaz — tutarsız kayıt.
            if (!string.IsNullOrEmpty(cariId) || birimBedel is not null)
                throw new BadRequestException(
                    "Konsinye olmayan ürün için konsinye sahibi veya bedeli girilemez. " +
                    "Ürünü konsinye işaretleyin ya da bu alanları boşaltın.");
            return;
        }

        if (string.IsNullOrEmpty(cariId))
            throw new BadRequestException(
                "Konsinye ürünün sahibi (cari) belirtilmelidir — mal satılınca " +
                "bedelin kime borçlanılacağı bilinmelidir.");

        if (birimBedel is null or <= 0m)
            throw new BadRequestException(
                "Konsinye ürünün birim bedeli belirtilmelidir. Bedel olmadan satışta " +
                "mal sahibine borç doğmaz ve kâr olduğundan yüksek görünür.");
    }

    /// <summary>
    /// Satış anında son kontrol. Göç öncesi ya da elle bozulmuş kayıtlar burada yakalanır.
    /// </summary>
    public static void SatisaUygunMu(Data.Urun urun)
    {
        if (!urun.Konsinye) return;
        if (string.IsNullOrEmpty(urun.KonsinyeCariId) || urun.KonsinyeBirimBedel is null or <= 0m)
            throw new BadRequestException(
                $"'{urun.Ad}' konsinye bir üründür ancak sahibi veya birim bedeli eksik. " +
                "Satıştan önce ürün kartı tamamlanmalıdır — aksi hâlde mal sahibine " +
                "borç doğmaz ve kâr yanlış hesaplanır.");
    }
}
