using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Yasal4StopajOdemeleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stopaj_odemeleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    donem = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    odeme_tarihi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    hareket_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kasa_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tahakkuk_fis_no = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stopaj_odemeleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_stopaj_odemeleri_tenant_id_donem",
                table: "stopaj_odemeleri",
                columns: new[] { "tenant_id", "donem" });

            migrationBuilder.CreateIndex(
                name: "IX_stopaj_odemeleri_tenant_id_hareket_id",
                table: "stopaj_odemeleri",
                columns: new[] { "tenant_id", "hareket_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stopaj_odemeleri");
        }
    }
}
