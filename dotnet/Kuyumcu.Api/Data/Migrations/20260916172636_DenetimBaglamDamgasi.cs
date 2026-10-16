using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DenetimBaglamDamgasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kasa_id",
                table: "denetim_kayitlari",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "sube_id",
                table: "denetim_kayitlari",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_denetim_kayitlari_tenant_id_sube_id_tarih",
                table: "denetim_kayitlari",
                columns: new[] { "tenant_id", "sube_id", "tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_denetim_kayitlari_tenant_id_sube_id_tarih",
                table: "denetim_kayitlari");

            migrationBuilder.DropColumn(
                name: "kasa_id",
                table: "denetim_kayitlari");

            migrationBuilder.DropColumn(
                name: "sube_id",
                table: "denetim_kayitlari");
        }
    }
}
