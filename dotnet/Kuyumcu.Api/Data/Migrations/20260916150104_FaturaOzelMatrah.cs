using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FaturaOzelMatrah : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "has_birim_fiyat",
                table: "faturalar",
                type: "decimal(15,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "has_gram",
                table: "faturalar",
                type: "decimal(15,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ozel_matrah_kodu",
                table: "faturalar",
                type: "varchar(5)",
                maxLength: 5,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_faturalar_tenant_id_kdv_turu_tarih",
                table: "faturalar",
                columns: new[] { "tenant_id", "kdv_turu", "tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_faturalar_tenant_id_kdv_turu_tarih",
                table: "faturalar");

            migrationBuilder.DropColumn(
                name: "has_birim_fiyat",
                table: "faturalar");

            migrationBuilder.DropColumn(
                name: "has_gram",
                table: "faturalar");

            migrationBuilder.DropColumn(
                name: "ozel_matrah_kodu",
                table: "faturalar");
        }
    }
}
