using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <summary>
    /// PAKET B-1 (dalga S-B) — KULLANICI GÖRÜNÜM TERCİHİ + ESKİ DEĞER GÖÇÜ.
    ///
    /// <para>1) <c>kullanicilar</c> tablosuna üç <b>nullable</b> kolon:
    /// <c>tema_modu</c>, <c>sema</c>, <c>yazi_boyutu</c>. NULL = "bilinmiyor"
    /// değil, <b>"dükkân varsayılanını devral"</b> demektir; bu yüzden mevcut
    /// hiçbir satır doldurulmaz — hiçbir kiracının ekranı sessizce değişmez.</para>
    ///
    /// <para>2) ESKİ DEĞER ÖLMEZ: kiracı ayarlarında kayıtlı
    /// <c>altin</c>/<c>kurumsal</c> şema kodları <c>platin</c>'e,
    /// <c>yogun</c>/<c>okunakli</c> ölçek kodları <c>16</c>/<c>20</c> puntoya
    /// GÖÇ eder (harita: <c>GorunumSecenekleri.SemaTakmaAdlari</c> /
    /// <c>OlcekTakmaAdlari</c>). <c>okunakli → 20</c>'dir (belgedeki 18 değil):
    /// canlı CSS <c>[data-yazi="20"], [data-olcek="okunakli"]</c> yazdığı için
    /// göç, kullanıcının BUGÜN GÖRDÜĞÜ boyutu korur.</para>
    ///
    /// <para>⚠ GERİ ALMA DÜRÜSTLÜĞÜ (Anayasa md. 1): <c>Down</c> kolonları
    /// düşürür ama göç etmiş şema/ölçek kodlarını ESKİ ADA ÇEVİRMEZ —
    /// <c>platin</c>'in kaynağı <c>altin</c> mı <c>kurumsal</c> mı olduğu
    /// bilgisi kaybolmuştur ve TAHMİN EDİLMEZ. Geri alınan sürümde eski kod
    /// tanımadığı değeri reddedip kendi varsayılanına düşer (satır silinmez).</para>
    /// </summary>
    public partial class B1KullaniciGorunumTercihi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sema",
                table: "kullanicilar",
                type: "varchar(24)",
                maxLength: 24,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "tema_modu",
                table: "kullanicilar",
                type: "varchar(12)",
                maxLength: 12,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "yazi_boyutu",
                table: "kullanicilar",
                type: "int",
                nullable: true);

            // ── ESKİ DEĞER GÖÇÜ (kiraci_ayarlari) ────────────────────────────
            // Tercih KAYBOLMAZ: bugün kayıtlı eski kodlar yeni slug'a çevrilir.
            // Yeni kodlar (platin/zumrut/16/18/20) zaten beyaz listede olduğu
            // için WHERE yan tümceleri onlara DOKUNMAZ (yeniden çalıştırılabilir).
            migrationBuilder.Sql(
                "UPDATE kiraci_ayarlari SET deger = 'platin' " +
                "WHERE anahtar = 'gorunum_semasi' AND deger IN ('altin','kurumsal','platin-mavi');");

            migrationBuilder.Sql(
                "UPDATE kiraci_ayarlari SET deger = '16' " +
                "WHERE anahtar = 'gorunum_yazi_olcegi' AND deger = 'yogun';");

            migrationBuilder.Sql(
                "UPDATE kiraci_ayarlari SET deger = '20' " +
                "WHERE anahtar = 'gorunum_yazi_olcegi' AND deger = 'okunakli';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sema",
                table: "kullanicilar");

            migrationBuilder.DropColumn(
                name: "tema_modu",
                table: "kullanicilar");

            migrationBuilder.DropColumn(
                name: "yazi_boyutu",
                table: "kullanicilar");
        }
    }
}
