using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuthSifirlaParolaJetonlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parola_sifirlama_jetonlari",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    jeton_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gecerlilik = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    kullanildi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    iptal_edildi = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ip_adresi = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parola_sifirlama_jetonlari", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_parola_sifirlama_jetonlari_jeton_hash",
                table: "parola_sifirlama_jetonlari",
                column: "jeton_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parola_sifirlama_jetonlari_tenant_id_kullanici_id",
                table: "parola_sifirlama_jetonlari",
                columns: new[] { "tenant_id", "kullanici_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parola_sifirlama_jetonlari");
        }
    }
}
