using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BankaEkstresiIceAktarma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "banka_referans",
                table: "banka_hareketleri",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "dedupe_anahtar",
                table: "banka_hareketleri",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ekstre_satir_id",
                table: "banka_hareketleri",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cari_ibanlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    iban = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    iban_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unvan = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cari_ibanlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ekstre_ithalatlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    banka_hesap_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dosya_adi = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    format = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    satir_sayisi = table.Column<int>(type: "int", nullable: false),
                    yeni_sayisi = table.Column<int>(type: "int", nullable: false),
                    mukerrer_sayisi = table.Column<int>(type: "int", nullable: false),
                    hatali_sayisi = table.Column<int>(type: "int", nullable: false),
                    onaylanan_sayisi = table.Column<int>(type: "int", nullable: false),
                    durum = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    acilis_bakiye = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    kapanis_bakiye = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    yukleyen_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ekstre_ithalatlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ekstre_satirlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ithalat_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    satir_no = table.Column<int>(type: "int", nullable: false),
                    ham_satir = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tarih = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    yon = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    para_birimi = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    karsi_taraf_ad = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    karsi_iban = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    karsi_iban_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    banka_referans = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aciklama = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dedupe_anahtar = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    durum = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hata_mesaji = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    eslesen_cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    eslesme_kaynagi = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    banka_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ekstre_satirlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_banka_hareketleri_tenant_id_banka_hesap_id_dedupe_anahtar",
                table: "banka_hareketleri",
                columns: new[] { "tenant_id", "banka_hesap_id", "dedupe_anahtar" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cari_ibanlari_tenant_id_cari_id",
                table: "cari_ibanlari",
                columns: new[] { "tenant_id", "cari_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cari_ibanlari_tenant_id_iban_hash",
                table: "cari_ibanlari",
                columns: new[] { "tenant_id", "iban_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ekstre_ithalatlari_tenant_id_banka_hesap_id_sha256",
                table: "ekstre_ithalatlari",
                columns: new[] { "tenant_id", "banka_hesap_id", "sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_ekstre_ithalatlari_tenant_id_olusturuldu",
                table: "ekstre_ithalatlari",
                columns: new[] { "tenant_id", "olusturuldu" });

            migrationBuilder.CreateIndex(
                name: "IX_ekstre_satirlari_tenant_id_ithalat_id_durum",
                table: "ekstre_satirlari",
                columns: new[] { "tenant_id", "ithalat_id", "durum" });

            migrationBuilder.CreateIndex(
                name: "IX_ekstre_satirlari_tenant_id_ithalat_id_satir_no",
                table: "ekstre_satirlari",
                columns: new[] { "tenant_id", "ithalat_id", "satir_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ekstre_satirlari_tenant_id_karsi_iban_hash",
                table: "ekstre_satirlari",
                columns: new[] { "tenant_id", "karsi_iban_hash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cari_ibanlari");

            migrationBuilder.DropTable(
                name: "ekstre_ithalatlari");

            migrationBuilder.DropTable(
                name: "ekstre_satirlari");

            migrationBuilder.DropIndex(
                name: "IX_banka_hareketleri_tenant_id_banka_hesap_id_dedupe_anahtar",
                table: "banka_hareketleri");

            migrationBuilder.DropColumn(
                name: "banka_referans",
                table: "banka_hareketleri");

            migrationBuilder.DropColumn(
                name: "dedupe_anahtar",
                table: "banka_hareketleri");

            migrationBuilder.DropColumn(
                name: "ekstre_satir_id",
                table: "banka_hareketleri");
        }
    }
}
