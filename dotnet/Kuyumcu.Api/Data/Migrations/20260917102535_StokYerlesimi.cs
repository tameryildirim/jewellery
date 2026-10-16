using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StokYerlesimi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stok_yerlesimleri",
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
                    reyon_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    adet = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stok_yerlesimleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_reyonlar_tenant_id_sube_id_ad",
                table: "reyonlar",
                columns: new[] { "tenant_id", "sube_id", "ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stok_yerlesimleri_tenant_id_reyon_id",
                table: "stok_yerlesimleri",
                columns: new[] { "tenant_id", "reyon_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stok_yerlesimleri_tenant_id_sube_id_urun_id",
                table: "stok_yerlesimleri",
                columns: new[] { "tenant_id", "sube_id", "urun_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stok_yerlesimleri_tenant_id_sube_id_urun_id_reyon_id",
                table: "stok_yerlesimleri",
                columns: new[] { "tenant_id", "sube_id", "urun_id", "reyon_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stok_yerlesimleri");

            migrationBuilder.DropIndex(
                name: "IX_reyonlar_tenant_id_sube_id_ad",
                table: "reyonlar");
        }
    }
}
