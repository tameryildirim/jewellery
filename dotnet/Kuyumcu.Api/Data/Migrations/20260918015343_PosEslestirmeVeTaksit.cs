using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PosEslestirmeVeTaksit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pos_taksit_oranlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pos_cihaz_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    taksit_adedi = table.Column<int>(type: "int", nullable: false),
                    komisyon_oran = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    valor_gun = table.Column<int>(type: "int", nullable: true),
                    gecerli_baslangic = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_taksit_oranlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pos_yatma_eslesmeleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pos_tahsilat_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    banka_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    eslesen_tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    otomatik = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    eslestiren_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    geri_alindi = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    geri_alma_gerekcesi = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_yatma_eslesmeleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_pos_taksit_oranlari_tenant_id_pos_cihaz_id_aktif",
                table: "pos_taksit_oranlari",
                columns: new[] { "tenant_id", "pos_cihaz_id", "aktif" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_taksit_oranlari_tenant_id_pos_cihaz_id_taksit_adedi_gece~",
                table: "pos_taksit_oranlari",
                columns: new[] { "tenant_id", "pos_cihaz_id", "taksit_adedi", "gecerli_baslangic" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_yatma_eslesmeleri_tenant_id_banka_hareket_id",
                table: "pos_yatma_eslesmeleri",
                columns: new[] { "tenant_id", "banka_hareket_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_yatma_eslesmeleri_tenant_id_pos_tahsilat_id",
                table: "pos_yatma_eslesmeleri",
                columns: new[] { "tenant_id", "pos_tahsilat_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_yatma_eslesmeleri_tenant_id_pos_tahsilat_id_banka_hareke~",
                table: "pos_yatma_eslesmeleri",
                columns: new[] { "tenant_id", "pos_tahsilat_id", "banka_hareket_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pos_taksit_oranlari");

            migrationBuilder.DropTable(
                name: "pos_yatma_eslesmeleri");
        }
    }
}
