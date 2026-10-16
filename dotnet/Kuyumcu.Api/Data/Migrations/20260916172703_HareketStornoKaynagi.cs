using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HareketStornoKaynagi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "storno_kaynak_id",
                table: "hareketler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_storno_kaynak_id",
                table: "hareketler",
                columns: new[] { "tenant_id", "storno_kaynak_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_storno_kaynak_id",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "storno_kaynak_id",
                table: "hareketler");
        }
    }
}
