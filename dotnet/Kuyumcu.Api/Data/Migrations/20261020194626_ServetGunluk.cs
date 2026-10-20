using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ServetGunluk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "servet_gunluk",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gun = table.Column<DateOnly>(type: "date", nullable: false),
                    nakit_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    altin_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    cari_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    net_tl = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    altin_net_has = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
                    has_fiyati = table.Column<decimal>(type: "decimal(15,4)", nullable: true),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servet_gunluk", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_servet_gunluk_tenant_id_gun",
                table: "servet_gunluk",
                columns: new[] { "tenant_id", "gun" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "servet_gunluk");
        }
    }
}
