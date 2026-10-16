using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SiparisCatisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "siparis_gecisleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    siparis_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    onceki_durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    yeni_durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_siparis_gecisleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "siparis_kalemleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    siparis_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urun_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tanim = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ayar_id = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gram_agirlik = table.Column<decimal>(type: "decimal(12,3)", nullable: true),
                    adet = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    sira = table.Column<int>(type: "int", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_siparis_kalemleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "siparisler",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    no = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tur = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    soz_verilen_gun = table.Column<DateOnly>(type: "date", nullable: true),
                    aciklama = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tahmini_tutar_tl = table.Column<decimal>(type: "decimal(14,2)", nullable: true),
                    kur_snapshot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    emanet_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_siparisler", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_siparis_gecisleri_tenant_id_siparis_id_olusturuldu",
                table: "siparis_gecisleri",
                columns: new[] { "tenant_id", "siparis_id", "olusturuldu" });

            migrationBuilder.CreateIndex(
                name: "IX_siparis_kalemleri_tenant_id_siparis_id",
                table: "siparis_kalemleri",
                columns: new[] { "tenant_id", "siparis_id" });

            migrationBuilder.CreateIndex(
                name: "IX_siparisler_tenant_id_cari_id",
                table: "siparisler",
                columns: new[] { "tenant_id", "cari_id" });

            migrationBuilder.CreateIndex(
                name: "IX_siparisler_tenant_id_durum_soz_verilen_gun",
                table: "siparisler",
                columns: new[] { "tenant_id", "durum", "soz_verilen_gun" });

            migrationBuilder.CreateIndex(
                name: "IX_siparisler_tenant_id_no",
                table: "siparisler",
                columns: new[] { "tenant_id", "no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_siparisler_tenant_id_tur_durum",
                table: "siparisler",
                columns: new[] { "tenant_id", "tur", "durum" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "siparis_gecisleri");

            migrationBuilder.DropTable(
                name: "siparis_kalemleri");

            migrationBuilder.DropTable(
                name: "siparisler");
        }
    }
}
