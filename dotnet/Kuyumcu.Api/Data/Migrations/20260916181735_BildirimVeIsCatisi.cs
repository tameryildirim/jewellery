using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BildirimVeIsCatisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bildirimler",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tur = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    onem = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    baslik = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    govde = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hedef_rota = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gorunur_roller = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ilgili_varlik_turu = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ilgili_varlik_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tekillik_anahtari = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_calismasi_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    kapatildi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    kapatan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kapatma_notu = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bildirimler", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "is_calismalari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_kodu = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    calisma_anahtari = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    baslangic = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    bitis = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    durum = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    uretilen_bildirim = table.Column<int>(type: "int", nullable: false),
                    incelenen_kayit = table.Column<int>(type: "int", nullable: false),
                    sure_ms = table.Column<int>(type: "int", nullable: true),
                    hata_ozeti = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tetikleyen_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_is_calismalari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_bildirimler_tenant_id_ilgili_varlik_turu_ilgili_varlik_id",
                table: "bildirimler",
                columns: new[] { "tenant_id", "ilgili_varlik_turu", "ilgili_varlik_id" });

            migrationBuilder.CreateIndex(
                name: "IX_bildirimler_tenant_id_kapatildi_olusturuldu",
                table: "bildirimler",
                columns: new[] { "tenant_id", "kapatildi", "olusturuldu" });

            migrationBuilder.CreateIndex(
                name: "IX_bildirimler_tenant_id_tekillik_anahtari",
                table: "bildirimler",
                columns: new[] { "tenant_id", "tekillik_anahtari" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_is_calismalari_tenant_id_baslangic",
                table: "is_calismalari",
                columns: new[] { "tenant_id", "baslangic" });

            migrationBuilder.CreateIndex(
                name: "IX_is_calismalari_tenant_id_is_kodu_calisma_anahtari",
                table: "is_calismalari",
                columns: new[] { "tenant_id", "is_kodu", "calisma_anahtari" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bildirimler");

            migrationBuilder.DropTable(
                name: "is_calismalari");
        }
    }
}
