using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ZOIslemPosEslesmeCiftSira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pos_yatma_eslesmeleri_tenant_id_pos_tahsilat_id_banka_hareke~",
                table: "pos_yatma_eslesmeleri");

            migrationBuilder.AddColumn<int>(
                name: "cift_sira",
                table: "pos_yatma_eslesmeleri",
                type: "int",
                nullable: false,
                // mevcut satırlar kendi çiftinin İLK eşleşme olayıdır (eski tekil indeks
                // çift başına tek satıra izin veriyordu)
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_pos_yatma_eslesmeleri_tenant_id_pos_tahsilat_id_banka_hareke~",
                table: "pos_yatma_eslesmeleri",
                columns: new[] { "tenant_id", "pos_tahsilat_id", "banka_hareket_id", "cift_sira" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pos_yatma_eslesmeleri_tenant_id_pos_tahsilat_id_banka_hareke~",
                table: "pos_yatma_eslesmeleri");

            migrationBuilder.DropColumn(
                name: "cift_sira",
                table: "pos_yatma_eslesmeleri");

            migrationBuilder.CreateIndex(
                name: "IX_pos_yatma_eslesmeleri_tenant_id_pos_tahsilat_id_banka_hareke~",
                table: "pos_yatma_eslesmeleri",
                columns: new[] { "tenant_id", "pos_tahsilat_id", "banka_hareket_id" },
                unique: true);
        }
    }
}
