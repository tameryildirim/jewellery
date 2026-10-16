using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HareketIadeKaynagi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "iade_kaynak_hareket_id",
                table: "hareketler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_iade_kaynak_hareket_id",
                table: "hareketler",
                columns: new[] { "tenant_id", "iade_kaynak_hareket_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_iade_kaynak_hareket_id",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "iade_kaynak_hareket_id",
                table: "hareketler");
        }
    }
}
