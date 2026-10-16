using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TakasEskiHas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "takas_eski_has_grami",
                table: "hareketler",
                type: "decimal(15,6)",
                nullable: true);

            // PAKET CIFT-BACAK tur 2 — GERİ DOLDURMA (yalnız ADETLİ takas satırı).
            // Adet kolonunu CIFT-BACAK tur 1 yazmaya başladı; o satırların eski has'ı
            // o zaman "net + adet × ürün has" diye türetiliyordu — aynı değer bir KEZ
            // donduruluyor (bundan sonra ürün kartı değişse de kaymaz). ADETSİZ eski
            // TAKAS satırı NULL kalır: eski has bilinmez, uydurulmaz (Anayasa md.1).
            // Ölçüm (30.09, kuyumcu_net): 3 TAKAS satırı, 0'ı adetli → etkilenen 0.
            migrationBuilder.Sql(
                "UPDATE hareketler h JOIN urunler u ON u.id = h.urun_id AND u.tenant_id = h.tenant_id " +
                "SET h.takas_eski_has_grami = ROUND(h.has_grami + h.adet * u.has_grami, 6) " +
                "WHERE h.tur = 'TAKAS' AND h.adet IS NOT NULL AND h.takas_eski_has_grami IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "takas_eski_has_grami",
                table: "hareketler");
        }
    }
}
