using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class M7bEvrakTahsilKarariKapali : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // M7b (17.10): çek/senet tahsilinin banka satırı — carinin borcu evrak GİRİŞİNDE düşüldü; M7a karar
            // listesine düşüp "borcundan düş" ile ikinci kez düşülmesin. Yalnız kararı VERİLMEMİŞ satırlar işaretlenir.
            migrationBuilder.Sql(@"UPDATE banka_hareketleri b
  JOIN kiymetli_evraklar e ON e.tahsil_banka_hareket_id = b.id AND e.tenant_id = b.tenant_id
   SET b.cari_karari = 'KAPALI'
 WHERE b.cari_karari IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE banka_hareketleri b
  JOIN kiymetli_evraklar e ON e.tahsil_banka_hareket_id = b.id AND e.tenant_id = b.tenant_id
   SET b.cari_karari = NULL
 WHERE b.cari_karari = 'KAPALI';");
        }
    }
}
