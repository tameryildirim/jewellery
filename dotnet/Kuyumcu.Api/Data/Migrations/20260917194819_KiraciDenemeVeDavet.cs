using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class KiraciDenemeVeDavet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "davet_id",
                table: "tenants",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "deneme_bitis",
                table: "tenants",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "durum",
                table: "tenants",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "AKTIF")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kiraci_davetleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    jeton_hash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    isletme_adi = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    deneme_gun = table.Column<int>(type: "int", nullable: false),
                    gecerlilik = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    kullanildi_mi = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    kullanan_tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    iptal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturan_not = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kiraci_davetleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_durum_deneme_bitis",
                table: "tenants",
                columns: new[] { "durum", "deneme_bitis" });

            migrationBuilder.CreateIndex(
                name: "IX_kiraci_davetleri_gecerlilik_kullanildi_mi",
                table: "kiraci_davetleri",
                columns: new[] { "gecerlilik", "kullanildi_mi" });

            migrationBuilder.CreateIndex(
                name: "IX_kiraci_davetleri_jeton_hash",
                table: "kiraci_davetleri",
                column: "jeton_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kiraci_davetleri");

            migrationBuilder.DropIndex(
                name: "IX_tenants_durum_deneme_bitis",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "davet_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "deneme_bitis",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "durum",
                table: "tenants");
        }
    }
}
