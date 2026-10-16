using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PirlantaVeBelge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pirlanta_berraklik",
                table: "urunler",
                type: "varchar(5)",
                maxLength: 5,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "pirlanta_karat",
                table: "urunler",
                type: "decimal(8,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pirlanta_kesim",
                table: "urunler",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "pirlanta_renk",
                table: "urunler",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "sertifika_kurumu",
                table: "urunler",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "sertifika_no",
                table: "urunler",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "urun_belgeleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urun_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tur = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ad = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mime = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    boyut = table.Column<int>(type: "int", nullable: false),
                    sha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    icerik = table.Column<byte[]>(type: "longblob", nullable: false),
                    yukleyen_kullanici_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aktif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_urun_belgeleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_urun_belgeleri_tenant_id_urun_id",
                table: "urun_belgeleri",
                columns: new[] { "tenant_id", "urun_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "urun_belgeleri");

            migrationBuilder.DropColumn(
                name: "pirlanta_berraklik",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "pirlanta_karat",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "pirlanta_kesim",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "pirlanta_renk",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "sertifika_kurumu",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "sertifika_no",
                table: "urunler");
        }
    }
}
