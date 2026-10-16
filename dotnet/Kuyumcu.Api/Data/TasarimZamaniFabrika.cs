using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuyumcu.Api.Data;

/// <summary>
/// `dotnet ef` komutları için tasarım zamanı DbContext üreticisi.
/// Uygulamayı (Program.cs) başlatmaz, canlı veritabanına bağlanmaz — bu yüzden
/// migration üretmek için JWT secret'ı ya da ayakta bir MySQL gerekmez.
/// Bağlantı gerekirse KUYUMCU_BAGLANTI ortam değişkeninden alınır.
/// </summary>
public sealed class TasarimZamaniFabrika : IDesignTimeDbContextFactory<KuyumcuDbContext>
{
    public KuyumcuDbContext CreateDbContext(string[] args)
    {
        // ⚠ PAKET T-1: buradaki yedek dizgede ÖRNEK BİR PAROLA YOKTUR.
        // Eskiden "password=DEGISTIR" yazıyordu; bu, kara listeye alınan şablon
        // sırrın kod içindeki İKİNCİ kopyasıydı (denetim.sh T1-2 sayacı yakaladı).
        // Tasarım zamanında veritabanına BAĞLANILMAZ — yalnız sağlayıcının şema
        // üretmesi için bir dizge gerekir. Gerçek bağlantı KUYUMCU_BAGLANTI
        // ortam değişkeninden gelir (migration bundle / database update).
        var baglanti = Environment.GetEnvironmentVariable("KUYUMCU_BAGLANTI")
            ?? "server=localhost;port=3306;database=kuyumcu;user=root";

        var ayar = new DbContextOptionsBuilder<KuyumcuDbContext>()
            .UseMySql(baglanti, ServerVersion.Parse("8.4.0-mysql"))
            .Options;

        // Tasarım zamanı: kiracı kavramı yok — sistem bağlamı ile kurulur.
        // Tasarım zamanında veri okunmaz/yazılmaz; şema için gerçek anahtar gerekmez.
        return new KuyumcuDbContext(ayar, new SistemKiraciBaglami(),
            new Common.AesGcmAlanSifreleyici(new byte[Common.AesGcmAlanSifreleyici.AnahtarBayt]));
    }
}
