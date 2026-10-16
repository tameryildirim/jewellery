using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class YasalParamAyniGunAn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_yasal_parametreler_anahtar_gecerlilik_baslangic",
                table: "yasal_parametreler");

            migrationBuilder.AddColumn<DateTime>(
                name: "gecerlilik_baslangic_ani",
                table: "yasal_parametreler",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_yasal_parametreler_anahtar_gecerlilik_baslangic_gecerlilik_b~",
                table: "yasal_parametreler",
                columns: new[] { "anahtar", "gecerlilik_baslangic", "gecerlilik_baslangic_ani" },
                unique: true);

            // PAKET YASAL-PARAM-2: anı BOŞ satırlarda (tohum, ileri tarihli) eski güvence aynen
            // sürer — bir anahtarın bir başlangıç gününde anısız TEK satır. MySQL UNIQUE indeks
            // NULL'ları çakıştırmaz; işlevsel parça yalnız anısız satırı 1'e eşler (ux_yasal_param_acik_uc kalıbı).
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_yasal_param_gun_tekil ON yasal_parametreler "
                + "(anahtar, gecerlilik_baslangic, ((CASE WHEN gecerlilik_baslangic_ani IS NULL THEN 1 ELSE NULL END)));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ux_yasal_param_gun_tekil ON yasal_parametreler;");

            migrationBuilder.DropIndex(
                name: "IX_yasal_parametreler_anahtar_gecerlilik_baslangic_gecerlilik_b~",
                table: "yasal_parametreler");

            migrationBuilder.DropColumn(
                name: "gecerlilik_baslangic_ani",
                table: "yasal_parametreler");

            migrationBuilder.CreateIndex(
                name: "IX_yasal_parametreler_anahtar_gecerlilik_baslangic",
                table: "yasal_parametreler",
                columns: new[] { "anahtar", "gecerlilik_baslangic" },
                unique: true);
        }
    }
}
