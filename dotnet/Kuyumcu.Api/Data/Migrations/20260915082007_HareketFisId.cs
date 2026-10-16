using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HareketFisId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fis_id",
                table: "hareketler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_fis_id",
                table: "hareketler",
                columns: new[] { "tenant_id", "fis_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_fis_id",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "fis_id",
                table: "hareketler");
        }
    }
}
