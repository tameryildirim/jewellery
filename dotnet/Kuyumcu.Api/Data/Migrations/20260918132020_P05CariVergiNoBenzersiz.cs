using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <summary>
    /// PAKET P-05 (Y-2) — cariler(tenant_id, vergi_no) benzersizliği.
    ///
    /// PLAN "filtreli/NULL toleranslı" diyor. MySQL'de WHERE'li (partial) indeks
    /// YOKTUR; karşılığı 8.0.13+ ile gelen FONKSİYONEL anahtar parçasıdır.
    /// Bu yüzden indeks düz CreateIndex ile değil, ham SQL ile kurulur:
    ///
    ///   UNIQUE (tenant_id, (CASE WHEN aktif = 1 THEN vergi_no END))
    ///
    /// Anlamı: AYNI kiracıda AYNI vergi/TC numarası İKİ AKTİF caride olamaz.
    /// PASİF (arşivlenmiş) cari ifadeyi NULL'a düşürür, UNIQUE birden çok NULL
    /// kabul eder → arşiv kayıtları yeni kaydı ENGELLEMEZ.
    ///
    /// Bu tasarım K-1 çakışmasını VERİYE DOKUNMADAN çözer: canlı kuyumcu_net'te
    /// tek çakışma QA-TEST-1 kiracısındaki (aktif=1) + (aktif=0) çiftiydi; kalıcı
    /// QA fikstürü olduğu için birleştirilmesi YASAK. Filtreli kısıt bu çifti
    /// meşru sayar, iki AKTİF kaydı ise reddeder.
    /// </summary>
    public partial class P05CariVergiNoBenzersiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX `UQ_cariler_tenant_vergi_no` ON `cariler` " +
                "(`tenant_id`, ((CASE WHEN `aktif` = 1 THEN `vergi_no` END)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX `UQ_cariler_tenant_vergi_no` ON `cariler`");
        }
    }
}
