using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-23c — VEZNE FİŞİ BAŞLIĞININ GENELLEŞTİRİLMESİ (şema)
    //
    // ⚠ VARSAYILAN 'SATIS'tır ve bu bir TAHMİN DEĞİLDİR: paket 19'dan bu yana
    //   vezne fişinin TEK türü satıştı (Data/VezneFisi.cs başlığı). EF'in ürettiği
    //   boş dizge varsayılanı ELLE 'SATIS' yapıldı; boş bırakılsaydı mevcut
    //   satırlar kapalı listenin DIŞINDA bir türle kalır ve tür kapısı onları
    //   reddederdi.
    // ⚠ talep_tutar_tl NULL'dur ve SATIS fişinde NULL kalır: tutarı kalemler
    //   belirler, iki kaynak olamaz (UX-S22).
    // ═════════════════════════════════════════════════════════════════════════
    /// <inheritdoc />
    public partial class P23cVezneFisTuru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fis_turu",
                table: "vezne_fisleri",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "SATIS")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "talep_tutar_tl",
                table: "vezne_fisleri",
                type: "decimal(15,2)",
                nullable: true);

            // Varsayılan yalnız YENİ satırlara işler; mevcut satırlar açıkça
            // damgalanır (boş dizge kalmış olma ihtimali kapatılır).
            migrationBuilder.Sql(
                "UPDATE vezne_fisleri SET fis_turu = 'SATIS' WHERE fis_turu IS NULL OR fis_turu = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fis_turu",
                table: "vezne_fisleri");

            migrationBuilder.DropColumn(
                name: "talep_tutar_tl",
                table: "vezne_fisleri");
        }
    }
}
