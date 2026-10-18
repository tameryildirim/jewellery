using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class D1SatisOnayi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "iskonto_siniri_yuzde",
                table: "kullanicilar",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "onay_talepleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    isteyen_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    parmak = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sebepler = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    toplam_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    liste_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    indirim_yuzde = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                    has_degeri_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    kalem_sayisi = table.Column<int>(type: "int", nullable: false),
                    durum = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    karar_veren_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    karar_zamani = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    karar_notu = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanildi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_onay_talepleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_onay_talepleri_tenant_id_durum_olusturuldu",
                table: "onay_talepleri",
                columns: new[] { "tenant_id", "durum", "olusturuldu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "onay_talepleri");

            migrationBuilder.DropColumn(
                name: "iskonto_siniri_yuzde",
                table: "kullanicilar");
        }
    }
}
