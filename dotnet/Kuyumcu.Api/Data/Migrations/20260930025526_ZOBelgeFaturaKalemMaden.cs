using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ZOBelgeFaturaKalemMaden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "maden",
                table: "fatura_kalemleri",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // GERİ DOLDURMA (ZO-BELGE 30.09 — kalem madeninin tek kaynağı):
            // (1) defter satırına bağlı kalem → hareketin madeni (fişten kesme yolu
            //     özel matrahı ZATEN bu değerle hesaplamıştı);
            migrationBuilder.Sql(
                "UPDATE fatura_kalemleri k JOIN hareketler h " +
                "ON h.id = k.hareket_id AND h.tenant_id = k.tenant_id " +
                "SET k.maden = h.maden WHERE k.maden IS NULL;");
            // (2) ürün kartından eklenen kalem → oluşturma anında kullanılan kural
            //     (Urun.Maden = Ayarlar.MadenBul(ayar) ?? ALTIN) kalemdeki ayar kopyasıyla.
            //     Ürünü de hareketi de olmayan satır NULL kalır → özel matrah uygulanmaz.
            migrationBuilder.Sql(
                "UPDATE fatura_kalemleri SET maden = CASE " +
                "WHEN ayar_id IS NULL OR ayar_id IN ('K8','K14','K18','K21','K22','K24','K995') THEN 'ALTIN' " +
                "WHEN ayar_id IN ('G800','G925','G950','G999') THEN 'GUMUS' " +
                "WHEN ayar_id IN ('PT850','PT900','PT950','PT999') THEN 'PLATIN' " +
                "WHEN ayar_id IN ('PD500','PD950','PD999') THEN 'PALADYUM' " +
                "WHEN ayar_id = 'CELIK' THEN 'CELIK' ELSE NULL END " +
                "WHERE maden IS NULL AND urun_id IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "maden",
                table: "fatura_kalemleri");
        }
    }
}
