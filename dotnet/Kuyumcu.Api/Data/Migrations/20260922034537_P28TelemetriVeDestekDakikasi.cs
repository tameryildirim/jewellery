using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class P28TelemetriVeDestekDakikasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "destek_dakikalari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tarih = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    dakika = table.Column<int>(type: "int", nullable: false),
                    kanal = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    konu = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aciklama = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    operator_adi = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_destek_dakikalari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "telemetri_olcumleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olcum_saati = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    istek_sayisi = table.Column<int>(type: "int", nullable: false),
                    hata_sayisi = table.Column<int>(type: "int", nullable: false),
                    islem_ms = table.Column<long>(type: "bigint", nullable: false),
                    yanit_bayt = table.Column<long>(type: "bigint", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetri_olcumleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_destek_dakikalari_tenant_id_aktif_tarih",
                table: "destek_dakikalari",
                columns: new[] { "tenant_id", "aktif", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_destek_dakikalari_tenant_id_tarih",
                table: "destek_dakikalari",
                columns: new[] { "tenant_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_telemetri_olcumleri_tenant_id_olcum_saati",
                table: "telemetri_olcumleri",
                columns: new[] { "tenant_id", "olcum_saati" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "destek_dakikalari");

            migrationBuilder.DropTable(
                name: "telemetri_olcumleri");
        }
    }
}
