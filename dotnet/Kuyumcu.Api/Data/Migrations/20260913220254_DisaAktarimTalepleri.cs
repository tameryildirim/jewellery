using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DisaAktarimTalepleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disa_aktarim_talepleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gerekce = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kapsam = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    maskeli = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    jeton_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    parola_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    son_kullanma = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    kullanildi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    satir_sayisi = table.Column<int>(type: "int", nullable: true),
                    ip_adresi = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disa_aktarim_talepleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_disa_aktarim_talepleri_jeton_hash",
                table: "disa_aktarim_talepleri",
                column: "jeton_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disa_aktarim_talepleri_tenant_id_olusturuldu",
                table: "disa_aktarim_talepleri",
                columns: new[] { "tenant_id", "olusturuldu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disa_aktarim_talepleri");
        }
    }
}
