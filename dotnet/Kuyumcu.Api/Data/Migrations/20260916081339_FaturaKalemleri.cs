using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FaturaKalemleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "has_deger_kaynagi",
                table: "faturalar",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "ELLE")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "kalem_kaynagi",
                table: "faturalar",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "BASLIK")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "fatura_kalemleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fatura_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sira = table.Column<int>(type: "int", nullable: false),
                    hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urun_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aciklama = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    miktar = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    birim = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ayar_id = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    milyem = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    gram_agirlik = table.Column<decimal>(type: "decimal(12,4)", nullable: true),
                    has_grami = table.Column<decimal>(type: "decimal(15,6)", nullable: true),
                    tas_ibaresi = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    birim_fiyat = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    iscilik_tl = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    indirim_tutari = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    kdv_turu = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kdv_oran = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    has_deger = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    kdv_matrah = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    kdv_tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    satir_toplam = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fatura_kalemleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_fatura_kalemleri_tenant_id_fatura_id",
                table: "fatura_kalemleri",
                columns: new[] { "tenant_id", "fatura_id" });

            migrationBuilder.CreateIndex(
                name: "IX_fatura_kalemleri_tenant_id_hareket_id",
                table: "fatura_kalemleri",
                columns: new[] { "tenant_id", "hareket_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fatura_kalemleri");

            migrationBuilder.DropColumn(
                name: "has_deger_kaynagi",
                table: "faturalar");

            migrationBuilder.DropColumn(
                name: "kalem_kaynagi",
                table: "faturalar");
        }
    }
}
