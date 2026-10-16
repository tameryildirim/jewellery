using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MfaTotp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kullanici_mfa",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    yontem = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gizli_sifreli = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    etkinlesti = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    son_kullanilan_adim = table.Column<long>(type: "bigint", nullable: true),
                    yanlis_deneme = table.Column<int>(type: "int", nullable: false),
                    kilit_bitis = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    kurtarma_uretildi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kullanici_mfa", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mfa_kurtarma_kodlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kod_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanildi = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    kullanildi_tarih = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mfa_kurtarma_kodlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_kullanici_mfa_tenant_id_kullanici_id",
                table: "kullanici_mfa",
                columns: new[] { "tenant_id", "kullanici_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mfa_kurtarma_kodlari_tenant_id_kod_hash",
                table: "mfa_kurtarma_kodlari",
                columns: new[] { "tenant_id", "kod_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mfa_kurtarma_kodlari_tenant_id_kullanici_id_kullanildi",
                table: "mfa_kurtarma_kodlari",
                columns: new[] { "tenant_id", "kullanici_id", "kullanildi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kullanici_mfa");

            migrationBuilder.DropTable(
                name: "mfa_kurtarma_kodlari");
        }
    }
}
