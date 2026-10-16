using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PosCihazVeTahsilat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pos_cihazlari",
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
                    banka_adi = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    uye_isyeri_no = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    terminal_no = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kasa_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    banka_hesap_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    para_birimi = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pesin_komisyon_oran = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    valor_gun = table.Column<int>(type: "int", nullable: false),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_cihazlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pos_tahsilatlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pos_cihaz_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kasa_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kur_snapshot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    brut_tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    komisyon_oran = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    komisyon_tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    net_tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    yatan_tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    taksit_adedi = table.Column<int>(type: "int", nullable: false),
                    para_birimi = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    islem_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    valor_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    durum = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    iptal_gerekcesi = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_tahsilatlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_pos_cihazlari_tenant_id_ad",
                table: "pos_cihazlari",
                columns: new[] { "tenant_id", "ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_cihazlari_tenant_id_aktif",
                table: "pos_cihazlari",
                columns: new[] { "tenant_id", "aktif" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_cihazlari_tenant_id_sube_id",
                table: "pos_cihazlari",
                columns: new[] { "tenant_id", "sube_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_tahsilatlari_tenant_id_durum_valor_tarihi",
                table: "pos_tahsilatlari",
                columns: new[] { "tenant_id", "durum", "valor_tarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_tahsilatlari_tenant_id_hareket_id",
                table: "pos_tahsilatlari",
                columns: new[] { "tenant_id", "hareket_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_tahsilatlari_tenant_id_kasa_hareket_id",
                table: "pos_tahsilatlari",
                columns: new[] { "tenant_id", "kasa_hareket_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_tahsilatlari_tenant_id_pos_cihaz_id_islem_tarihi",
                table: "pos_tahsilatlari",
                columns: new[] { "tenant_id", "pos_cihaz_id", "islem_tarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_tahsilatlari_tenant_id_valor_tarihi",
                table: "pos_tahsilatlari",
                columns: new[] { "tenant_id", "valor_tarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pos_cihazlari");

            migrationBuilder.DropTable(
                name: "pos_tahsilatlari");
        }
    }
}
