using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FaturaKalemBirimHas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "birim_has_grami",
                table: "fatura_kalemleri",
                type: "decimal(15,6)",
                nullable: true);

            // PAKET CARI-BAKIYE — GERİ DOLDURMA YALNIZ TASLAK faturalarda: birim has = satır toplamı ÷ miktar
            // (ürün yolu kalemleri; fişten kesilen, defter bağlı kalem — hareket_id dolu — adedi belgede
            // değişmediği için NULL kalır). KESILDI/IPTAL belgelere DOKUNULMAZ (değişmez belge).
            migrationBuilder.Sql(@"
UPDATE fatura_kalemleri k
  JOIN faturalar f ON f.id = k.fatura_id AND f.tenant_id = k.tenant_id
   SET k.birim_has_grami = ROUND(k.has_grami / k.miktar, 6)
 WHERE f.durum = 'TASLAK'
   AND k.hareket_id IS NULL
   AND k.has_grami IS NOT NULL
   AND k.miktar > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "birim_has_grami",
                table: "fatura_kalemleri");
        }
    }
}
