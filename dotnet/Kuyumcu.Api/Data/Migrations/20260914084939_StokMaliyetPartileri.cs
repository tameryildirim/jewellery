using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StokMaliyetPartileri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "maliyet_yontemi",
                table: "tenants",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stok_parti_tuketimleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    parti_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cikis_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urun_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    adet = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    birim_maliyet_tl = table.Column<decimal>(type: "decimal(15,4)", nullable: true),
                    birim_maliyet_has = table.Column<decimal>(type: "decimal(15,6)", nullable: true),
                    tarih = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stok_parti_tuketimleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stok_partileri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sube_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urun_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    giris_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    adet_giren = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    adet_kalan = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    has_gram_birim = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
                    birim_maliyet_tl = table.Column<decimal>(type: "decimal(15,4)", nullable: true),
                    birim_maliyet_has = table.Column<decimal>(type: "decimal(15,6)", nullable: true),
                    kur_snapshot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    giris_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stok_partileri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_stok_parti_tuketimleri_cikis_hareket_id",
                table: "stok_parti_tuketimleri",
                column: "cikis_hareket_id");

            migrationBuilder.CreateIndex(
                name: "IX_stok_parti_tuketimleri_parti_id",
                table: "stok_parti_tuketimleri",
                column: "parti_id");

            migrationBuilder.CreateIndex(
                name: "IX_stok_parti_tuketimleri_tenant_id_tarih",
                table: "stok_parti_tuketimleri",
                columns: new[] { "tenant_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_stok_partileri_giris_hareket_id",
                table: "stok_partileri",
                column: "giris_hareket_id");

            migrationBuilder.CreateIndex(
                name: "IX_stok_partileri_tenant_id_sube_id_urun_id_giris_tarihi",
                table: "stok_partileri",
                columns: new[] { "tenant_id", "sube_id", "urun_id", "giris_tarihi" });

            // ── AÇILIŞ (DEVIR) PARTİLERİ ────────────────────────────────────
            // Bu özellikten ÖNCE girmiş stokların maliyeti bilinmiyor.
            // Her mevcut stok satırı için maliyeti NULL bir devir partisi açılır.
            //
            // ⚠ SIFIR MALİYET YAZILMAZ. Sıfır yazmak "bedava alınmış" demek olur
            // ve o malın satışı kârı olduğundan yüksek gösterirdi. NULL, raporda
            // "maliyeti bilinmeyen" olarak AYRI sayılır (Anayasa md.1).
            migrationBuilder.Sql(@"
                INSERT INTO stok_partileri
                    (id, tenant_id, sube_id, urun_id, giris_hareket_id, kaynak,
                     adet_giren, adet_kalan, has_gram_birim,
                     birim_maliyet_tl, birim_maliyet_has, kur_snapshot_id,
                     giris_tarihi, olusturuldu)
                SELECT
                    LOWER(REPLACE(UUID(), '-', '')),
                    s.tenant_id, s.sube_id, s.urun_id, NULL, 'DEVIR',
                    s.adet, s.adet,
                    CASE WHEN s.adet > 0 THEN ROUND(s.toplam_has_gram / s.adet, 6) ELSE 0 END,
                    NULL, NULL, NULL,
                    s.guncellendi, s.guncellendi
                FROM stoklar s
                WHERE s.adet > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stok_parti_tuketimleri");

            migrationBuilder.DropTable(
                name: "stok_partileri");

            migrationBuilder.DropColumn(
                name: "maliyet_yontemi",
                table: "tenants");
        }
    }
}
