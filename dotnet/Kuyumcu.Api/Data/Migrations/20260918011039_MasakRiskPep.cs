using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MasakRiskPep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "pep_beyan_tarihi",
                table: "cariler",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "pep_beyani",
                table: "cariler",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pep_gorev",
                table: "cariler",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cari_risk_profilleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cari_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kaynak = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    puan = table.Column<int>(type: "int", nullable: false),
                    seviye = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sebepler = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hesaplanma_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cari_risk_profilleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_cariler_tenant_id_pep_beyani",
                table: "cariler",
                columns: new[] { "tenant_id", "pep_beyani" });

            migrationBuilder.CreateIndex(
                name: "IX_cari_risk_profilleri_tenant_id_cari_id_olusturuldu",
                table: "cari_risk_profilleri",
                columns: new[] { "tenant_id", "cari_id", "olusturuldu" });

            migrationBuilder.CreateIndex(
                name: "IX_cari_risk_profilleri_tenant_id_seviye",
                table: "cari_risk_profilleri",
                columns: new[] { "tenant_id", "seviye" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cari_risk_profilleri");

            migrationBuilder.DropIndex(
                name: "IX_cariler_tenant_id_pep_beyani",
                table: "cariler");

            migrationBuilder.DropColumn(
                name: "pep_beyan_tarihi",
                table: "cariler");

            migrationBuilder.DropColumn(
                name: "pep_beyani",
                table: "cariler");

            migrationBuilder.DropColumn(
                name: "pep_gorev",
                table: "cariler");
        }
    }
}
