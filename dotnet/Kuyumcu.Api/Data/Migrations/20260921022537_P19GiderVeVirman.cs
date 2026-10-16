using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class P19GiderVeVirman : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gider_kategori_id",
                table: "kasa_hareketleri",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "karsi_bacak_id",
                table: "kasa_hareketleri",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "karsi_bacak_id",
                table: "banka_hareketleri",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "gider_kategorileri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aciklama = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gider_kategorileri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_kasa_hareketleri_tenant_id_gider_kategori_id_tarih",
                table: "kasa_hareketleri",
                columns: new[] { "tenant_id", "gider_kategori_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_kasa_hareketleri_tenant_id_karsi_bacak_id",
                table: "kasa_hareketleri",
                columns: new[] { "tenant_id", "karsi_bacak_id" });

            migrationBuilder.CreateIndex(
                name: "IX_banka_hareketleri_tenant_id_karsi_bacak_id",
                table: "banka_hareketleri",
                columns: new[] { "tenant_id", "karsi_bacak_id" });

            migrationBuilder.CreateIndex(
                name: "IX_gider_kategorileri_tenant_id_ad",
                table: "gider_kategorileri",
                columns: new[] { "tenant_id", "ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gider_kategorileri_tenant_id_aktif",
                table: "gider_kategorileri",
                columns: new[] { "tenant_id", "aktif" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gider_kategorileri");

            migrationBuilder.DropIndex(
                name: "IX_kasa_hareketleri_tenant_id_gider_kategori_id_tarih",
                table: "kasa_hareketleri");

            migrationBuilder.DropIndex(
                name: "IX_kasa_hareketleri_tenant_id_karsi_bacak_id",
                table: "kasa_hareketleri");

            migrationBuilder.DropIndex(
                name: "IX_banka_hareketleri_tenant_id_karsi_bacak_id",
                table: "banka_hareketleri");

            migrationBuilder.DropColumn(
                name: "gider_kategori_id",
                table: "kasa_hareketleri");

            migrationBuilder.DropColumn(
                name: "karsi_bacak_id",
                table: "kasa_hareketleri");

            migrationBuilder.DropColumn(
                name: "karsi_bacak_id",
                table: "banka_hareketleri");
        }
    }
}
