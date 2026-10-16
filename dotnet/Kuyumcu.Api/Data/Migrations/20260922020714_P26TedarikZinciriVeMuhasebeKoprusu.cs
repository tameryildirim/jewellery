using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class P26TedarikZinciriVeMuhasebeKoprusu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kapora_hareket_id",
                table: "siparisler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "usta_cari_id",
                table: "siparisler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "hesap_kodu",
                table: "cariler",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "irsaliye_kalemleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    irsaliye_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urun_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tanim = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    adet = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sira = table.Column<int>(type: "int", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irsaliye_kalemleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "irsaliyeler",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    no = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    yon = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tarih = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    satinalma_siparis_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fatura_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aciklama = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hesap_kodu = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    iptal_gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irsaliyeler", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "satinalma_kalemleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    satinalma_siparis_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urun_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tanim = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    adet = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    birim_fiyat_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    kabul_edilen_adet = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    sira = table.Column<int>(type: "int", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_satinalma_kalemleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "satinalma_siparisleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    no = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tedarikci_cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    beklenen_tarih = table.Column<DateOnly>(type: "date", nullable: true),
                    aciklama = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hesap_kodu = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    iptal_gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_satinalma_siparisleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_irsaliye_kalemleri_hareket_id",
                table: "irsaliye_kalemleri",
                column: "hareket_id");

            migrationBuilder.CreateIndex(
                name: "IX_irsaliye_kalemleri_tenant_id_irsaliye_id",
                table: "irsaliye_kalemleri",
                columns: new[] { "tenant_id", "irsaliye_id" });

            migrationBuilder.CreateIndex(
                name: "IX_irsaliyeler_tenant_id_durum_fatura_id",
                table: "irsaliyeler",
                columns: new[] { "tenant_id", "durum", "fatura_id" });

            migrationBuilder.CreateIndex(
                name: "IX_irsaliyeler_tenant_id_no",
                table: "irsaliyeler",
                columns: new[] { "tenant_id", "no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_irsaliyeler_tenant_id_satinalma_siparis_id",
                table: "irsaliyeler",
                columns: new[] { "tenant_id", "satinalma_siparis_id" });

            migrationBuilder.CreateIndex(
                name: "IX_irsaliyeler_tenant_id_sube_id_tarih",
                table: "irsaliyeler",
                columns: new[] { "tenant_id", "sube_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_satinalma_kalemleri_tenant_id_satinalma_siparis_id",
                table: "satinalma_kalemleri",
                columns: new[] { "tenant_id", "satinalma_siparis_id" });

            migrationBuilder.CreateIndex(
                name: "IX_satinalma_siparisleri_tenant_id_no",
                table: "satinalma_siparisleri",
                columns: new[] { "tenant_id", "no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_satinalma_siparisleri_tenant_id_sube_id_durum",
                table: "satinalma_siparisleri",
                columns: new[] { "tenant_id", "sube_id", "durum" });

            migrationBuilder.CreateIndex(
                name: "IX_satinalma_siparisleri_tenant_id_tedarikci_cari_id",
                table: "satinalma_siparisleri",
                columns: new[] { "tenant_id", "tedarikci_cari_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "irsaliye_kalemleri");

            migrationBuilder.DropTable(
                name: "irsaliyeler");

            migrationBuilder.DropTable(
                name: "satinalma_kalemleri");

            migrationBuilder.DropTable(
                name: "satinalma_siparisleri");

            migrationBuilder.DropColumn(
                name: "kapora_hareket_id",
                table: "siparisler");

            migrationBuilder.DropColumn(
                name: "usta_cari_id",
                table: "siparisler");

            migrationBuilder.DropColumn(
                name: "hesap_kodu",
                table: "cariler");
        }
    }
}
