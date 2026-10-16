using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DenetimIziVeMasak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "denetim_kayitlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    eylem = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    varlik_turu = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    varlik_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hassas = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    detay = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_adresi = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_agent = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tarih = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_denetim_kayitlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "masak_beyanlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kimlik_tespiti_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    islem_tutari_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    kademe = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    islem_amaci = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aciklama = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fon_kaynagi = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ek_belge_referansi = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    alan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tarih = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    saklama_bitis = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    duzeltilen_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_masak_beyanlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "masak_kimlik_tespitleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    islem_tutari_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    belge_turu = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    belge_no = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ad_soyad = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dogum_tarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    uyruk = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    adres = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tespit_eden_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tarih = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    saklama_bitis = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    duzeltilen_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_masak_kimlik_tespitleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "masak_supheli_bildirimler",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kimlik_tespiti_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    islem_tutari_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    gerekce = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tespit_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    son_bildirim_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gonderim_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    gonderim_referansi = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kapatma_gerekcesi = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bildiren_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    saklama_bitis = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_masak_supheli_bildirimler", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_denetim_kayitlari_tenant_id_hassas_tarih",
                table: "denetim_kayitlari",
                columns: new[] { "tenant_id", "hassas", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_denetim_kayitlari_tenant_id_tarih",
                table: "denetim_kayitlari",
                columns: new[] { "tenant_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_denetim_kayitlari_tenant_id_varlik_turu_varlik_id",
                table: "denetim_kayitlari",
                columns: new[] { "tenant_id", "varlik_turu", "varlik_id" });

            migrationBuilder.CreateIndex(
                name: "IX_masak_beyanlari_tenant_id_kimlik_tespiti_id",
                table: "masak_beyanlari",
                columns: new[] { "tenant_id", "kimlik_tespiti_id" });

            migrationBuilder.CreateIndex(
                name: "IX_masak_beyanlari_tenant_id_tarih",
                table: "masak_beyanlari",
                columns: new[] { "tenant_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_masak_kimlik_tespitleri_tenant_id_cari_id",
                table: "masak_kimlik_tespitleri",
                columns: new[] { "tenant_id", "cari_id" });

            migrationBuilder.CreateIndex(
                name: "IX_masak_kimlik_tespitleri_tenant_id_saklama_bitis",
                table: "masak_kimlik_tespitleri",
                columns: new[] { "tenant_id", "saklama_bitis" });

            migrationBuilder.CreateIndex(
                name: "IX_masak_kimlik_tespitleri_tenant_id_tarih",
                table: "masak_kimlik_tespitleri",
                columns: new[] { "tenant_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_masak_supheli_bildirimler_tenant_id_durum_son_bildirim_tarihi",
                table: "masak_supheli_bildirimler",
                columns: new[] { "tenant_id", "durum", "son_bildirim_tarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_masak_supheli_bildirimler_tenant_id_tespit_tarihi",
                table: "masak_supheli_bildirimler",
                columns: new[] { "tenant_id", "tespit_tarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "denetim_kayitlari");

            migrationBuilder.DropTable(
                name: "masak_beyanlari");

            migrationBuilder.DropTable(
                name: "masak_kimlik_tespitleri");

            migrationBuilder.DropTable(
                name: "masak_supheli_bildirimler");
        }
    }
}
