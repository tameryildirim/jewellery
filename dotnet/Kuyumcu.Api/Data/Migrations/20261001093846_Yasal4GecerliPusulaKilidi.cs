using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Yasal4GecerliPusulaKilidi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gecerli_hareket_id",
                table: "gider_pusulalari",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true,
                computedColumnSql: "(case when `iptal_edildi` = 0 then `hareket_id` end)",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UQ_gider_pusulalari_tenant_gecerli_hareket",
                table: "gider_pusulalari",
                columns: new[] { "tenant_id", "gecerli_hareket_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_gider_pusulalari_tenant_gecerli_hareket",
                table: "gider_pusulalari");

            migrationBuilder.DropColumn(
                name: "gecerli_hareket_id",
                table: "gider_pusulalari");
        }
    }
}
