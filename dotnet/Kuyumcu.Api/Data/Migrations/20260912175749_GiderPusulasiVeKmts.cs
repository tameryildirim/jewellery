using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class GiderPusulasiVeKmts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kmts_kodu",
                table: "urunler",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "kmts_kodu",
                table: "hareketler",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "gider_pusulalari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pusula_no = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    satici_tipi = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    satici_ad_soyad = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    satici_tckn = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    satici_tckn_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sifre_surumu = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    satici_adres = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ayar_id = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gram_agirlik = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    has_grami = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
                    mal_aciklamasi = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    brut_tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    stopaj_oran = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    stopaj_tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    net_odenen = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    teslim_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    duzenleme_son_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    duzeltilen_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gider_pusulalari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_gider_pusulalari_hareket_id",
                table: "gider_pusulalari",
                column: "hareket_id");

            migrationBuilder.CreateIndex(
                name: "IX_gider_pusulalari_tenant_id_pusula_no",
                table: "gider_pusulalari",
                columns: new[] { "tenant_id", "pusula_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gider_pusulalari_tenant_id_satici_tckn_hash",
                table: "gider_pusulalari",
                columns: new[] { "tenant_id", "satici_tckn_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_gider_pusulalari_tenant_id_teslim_tarihi",
                table: "gider_pusulalari",
                columns: new[] { "tenant_id", "teslim_tarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gider_pusulalari");

            migrationBuilder.DropColumn(
                name: "kmts_kodu",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "kmts_kodu",
                table: "hareketler");
        }
    }
}
