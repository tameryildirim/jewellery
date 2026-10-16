using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FiyatListesiVeKampanya : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "hediye",
                table: "hareketler",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "indirim_tutari",
                table: "hareketler",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kampanya_id",
                table: "hareketler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "liste_fiyati",
                table: "hareketler",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fiyat_listesi_id",
                table: "cariler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "fiyat_listeleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tur = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    has_marj_yuzde = table.Column<decimal>(type: "decimal(7,3)", nullable: false),
                    iscilik_carpan_yuzde = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                    varsayilan = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiyat_listeleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kampanyalar",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ad = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    baslangic = table.Column<DateOnly>(type: "date", nullable: false),
                    bitis = table.Column<DateOnly>(type: "date", nullable: false),
                    hedef_tur = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hedef_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    indirim_turu = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    deger = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kampanyalar", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_fiyat_listeleri_tenant_id_varsayilan",
                table: "fiyat_listeleri",
                columns: new[] { "tenant_id", "varsayilan" });

            migrationBuilder.CreateIndex(
                name: "IX_kampanyalar_tenant_id_baslangic_bitis",
                table: "kampanyalar",
                columns: new[] { "tenant_id", "baslangic", "bitis" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiyat_listeleri");

            migrationBuilder.DropTable(
                name: "kampanyalar");

            migrationBuilder.DropColumn(
                name: "hediye",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "indirim_tutari",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "kampanya_id",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "liste_fiyati",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "fiyat_listesi_id",
                table: "cariler");
        }
    }
}
