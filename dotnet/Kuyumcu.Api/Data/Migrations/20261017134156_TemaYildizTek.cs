using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <summary>
    /// TEMA TEMİZLİĞİ (kurucu kararı 17.10): "tema sadece Yıldız, karanlık ve açık
    /// mod kalacak; diğer temalar gidecek." Kayıtlı ESKİ şema kodları "yildiz"e göç eder.
    ///
    /// <para>İki yer: kiracı varsayılanı (<c>kiraci_ayarlari</c>, anahtar
    /// <c>gorunum_semasi</c>) ve kullanıcının kendi satırı (<c>kullanicilar.sema</c>).
    /// NULL kullanıcı satırına DOKUNULMAZ (NULL = "dükkân varsayılanını devral").
    /// Açık/koyu (<c>tema_modu</c>) ve yazı boyutu bu göçten ETKİLENMEZ.</para>
    ///
    /// <para>Liste <c>GorunumSecenekleri.EskiSemalar</c> ile AYNIDIR; ama göç
    /// "deger &lt;&gt; 'yildiz'" yazar: tanınmayan bozuk değer de Yıldız'a çekilir
    /// (sunucu zaten onu varsayılana çözerdi; varsayılan artık Yıldız'dır).
    /// Yeniden çalıştırılabilir: ikinci koşuda değişen satır yoktur.</para>
    ///
    /// <para>⚠ GERİ ALMA DÜRÜSTLÜĞÜ (Anayasa md. 1): <c>Down</c> eski şema adlarını
    /// GERİ YAZMAZ — hangi satırın hangi şemadan geldiği bilgisi kaybolmuştur ve
    /// TAHMİN EDİLMEZ. Geri alınan sürüm "yildiz"i zaten tanır (9. şemaydı).</para>
    /// </summary>
    public partial class TemaYildizTek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE kiraci_ayarlari SET deger = 'yildiz' " +
                "WHERE anahtar = 'gorunum_semasi' AND deger <> 'yildiz';");

            migrationBuilder.Sql(
                "UPDATE kullanicilar SET sema = 'yildiz' " +
                "WHERE sema IS NOT NULL AND sema <> 'yildiz';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Bilinçli olarak boş — bkz. sınıf belgesi "GERİ ALMA DÜRÜSTLÜĞÜ".
        }
    }
}
