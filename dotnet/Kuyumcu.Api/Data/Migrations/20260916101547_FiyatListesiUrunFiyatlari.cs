using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FiyatListesiUrunFiyatlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiyat_listesi_urun_fiyatlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    liste_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urun_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sabit_fiyat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    has_marj_yuzde = table.Column<decimal>(type: "decimal(7,3)", nullable: true),
                    iscilik_carpan_yuzde = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    not = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    guncellendi = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiyat_listesi_urun_fiyatlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_fiyat_listesi_urun_fiyatlari_tenant_id_liste_id",
                table: "fiyat_listesi_urun_fiyatlari",
                columns: new[] { "tenant_id", "liste_id" });

            migrationBuilder.CreateIndex(
                name: "IX_fiyat_listesi_urun_fiyatlari_tenant_id_liste_id_urun_id",
                table: "fiyat_listesi_urun_fiyatlari",
                columns: new[] { "tenant_id", "liste_id", "urun_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiyat_listesi_urun_fiyatlari");
        }
    }
}
