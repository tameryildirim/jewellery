using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UrunAgaciReyonKonsinye : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kategori_id",
                table: "urunler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "konsinye",
                table: "urunler",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "konsinye_birim_bedel",
                table: "urunler",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "konsinye_cari_id",
                table: "urunler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "reyon_id",
                table: "urunler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "kategori_id",
                table: "hareketler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "konsinye",
                table: "hareketler",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "konsinye_birim_bedel",
                table: "hareketler",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "konsinye_cari_id",
                table: "hareketler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "reyon_id",
                table: "hareketler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reyonlar",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tip = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sorumlu_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reyonlar", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "urun_kategorileri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ad = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ust_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sira = table.Column<int>(type: "int", nullable: false),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_urun_kategorileri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_urunler_tenant_id_kategori_id",
                table: "urunler",
                columns: new[] { "tenant_id", "kategori_id" });

            migrationBuilder.CreateIndex(
                name: "IX_urunler_tenant_id_konsinye",
                table: "urunler",
                columns: new[] { "tenant_id", "konsinye" });

            migrationBuilder.CreateIndex(
                name: "IX_urunler_tenant_id_reyon_id",
                table: "urunler",
                columns: new[] { "tenant_id", "reyon_id" });

            migrationBuilder.CreateIndex(
                name: "IX_reyonlar_tenant_id_sube_id",
                table: "reyonlar",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_urun_kategorileri_tenant_id_ust_id",
                table: "urun_kategorileri",
                columns: new[] { "tenant_id", "ust_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reyonlar");

            migrationBuilder.DropTable(
                name: "urun_kategorileri");

            migrationBuilder.DropIndex(
                name: "IX_urunler_tenant_id_kategori_id",
                table: "urunler");

            migrationBuilder.DropIndex(
                name: "IX_urunler_tenant_id_konsinye",
                table: "urunler");

            migrationBuilder.DropIndex(
                name: "IX_urunler_tenant_id_reyon_id",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "kategori_id",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "konsinye",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "konsinye_birim_bedel",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "konsinye_cari_id",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "reyon_id",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "kategori_id",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "konsinye",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "konsinye_birim_bedel",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "konsinye_cari_id",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "reyon_id",
                table: "hareketler");
        }
    }
}
