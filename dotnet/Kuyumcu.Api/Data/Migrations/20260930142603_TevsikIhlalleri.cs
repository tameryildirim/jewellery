using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TevsikIhlalleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tevsik_ihlalleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tur = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    yon = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ihlal_gunu = table.Column<DateOnly>(type: "date", nullable: false),
                    nakit_toplam_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    had_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    hareket_idleri = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bildirim_son_gunu = table.Column<DateOnly>(type: "date", nullable: false),
                    durum = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kapatma_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    kapatan_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kapatma_gerekcesi = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bildirim_evrak_no = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bildirim_tarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    gec = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tevsik_ihlalleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_tevsik_ihlalleri_tenant_id_cari_id_ihlal_gunu_tur",
                table: "tevsik_ihlalleri",
                columns: new[] { "tenant_id", "cari_id", "ihlal_gunu", "tur" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tevsik_ihlalleri_tenant_id_durum_bildirim_son_gunu",
                table: "tevsik_ihlalleri",
                columns: new[] { "tenant_id", "durum", "bildirim_son_gunu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tevsik_ihlalleri");
        }
    }
}
