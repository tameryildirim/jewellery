using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IceAktarim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ice_aktarim_oturumlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak_ad = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dosya_adi = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dosya_sha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    yinelenen_politikasi = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    toplam_satir = table.Column<int>(type: "int", nullable: false),
                    gecerli_satir = table.Column<int>(type: "int", nullable: false),
                    hatali_satir = table.Column<int>(type: "int", nullable: false),
                    atlanan_satir = table.Column<int>(type: "int", nullable: false),
                    uygulanan_satir = table.Column<int>(type: "int", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    uygulandi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    geri_alindi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ip_adresi = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ice_aktarim_oturumlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ice_aktarim_satirlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    oturum_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    satir_no = table.Column<int>(type: "int", nullable: false),
                    ham_json = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sonuc = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hata_kodu = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hata_mesaji = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hedef_varlik_turu = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hedef_varlik_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ice_aktarim_satirlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ice_aktarim_oturumlari_tenant_id_durum",
                table: "ice_aktarim_oturumlari",
                columns: new[] { "tenant_id", "durum" });

            migrationBuilder.CreateIndex(
                name: "IX_ice_aktarim_oturumlari_tenant_id_olusturuldu",
                table: "ice_aktarim_oturumlari",
                columns: new[] { "tenant_id", "olusturuldu" });

            migrationBuilder.CreateIndex(
                name: "IX_ice_aktarim_satirlari_tenant_id_oturum_id_satir_no",
                table: "ice_aktarim_satirlari",
                columns: new[] { "tenant_id", "oturum_id", "satir_no" });

            migrationBuilder.CreateIndex(
                name: "IX_ice_aktarim_satirlari_tenant_id_oturum_id_sonuc",
                table: "ice_aktarim_satirlari",
                columns: new[] { "tenant_id", "oturum_id", "sonuc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ice_aktarim_oturumlari");

            migrationBuilder.DropTable(
                name: "ice_aktarim_satirlari");
        }
    }
}
