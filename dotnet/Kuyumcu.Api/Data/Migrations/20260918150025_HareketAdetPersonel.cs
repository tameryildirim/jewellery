using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HareketAdetPersonel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "adet",
                table: "hareketler",
                type: "decimal(12,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "personel_id",
                table: "hareketler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_personel_id_tarih",
                table: "hareketler",
                columns: new[] { "tenant_id", "personel_id", "tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_personel_id_tarih",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "adet",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "personel_id",
                table: "hareketler");
        }
    }
}
