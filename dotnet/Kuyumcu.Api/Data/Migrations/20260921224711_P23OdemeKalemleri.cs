using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class P23OdemeKalemleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "odeme_kalemleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fis_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sira = table.Column<int>(type: "int", nullable: false),
                    arac = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tutar_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    para_birimi = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    doviz_tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    kasa_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kasa_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pos_tahsilat_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    taksit_adedi = table.Column<int>(type: "int", nullable: true),
                    evrak_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kur_snapshot_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bilinmiyor = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    storno_hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_odeme_kalemleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_odeme_kalemleri_tenant_id_arac_olusturuldu",
                table: "odeme_kalemleri",
                columns: new[] { "tenant_id", "arac", "olusturuldu" });

            migrationBuilder.CreateIndex(
                name: "IX_odeme_kalemleri_tenant_id_fis_id",
                table: "odeme_kalemleri",
                columns: new[] { "tenant_id", "fis_id" });

            migrationBuilder.CreateIndex(
                name: "IX_odeme_kalemleri_tenant_id_hareket_id",
                table: "odeme_kalemleri",
                columns: new[] { "tenant_id", "hareket_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "odeme_kalemleri");
        }
    }
}
