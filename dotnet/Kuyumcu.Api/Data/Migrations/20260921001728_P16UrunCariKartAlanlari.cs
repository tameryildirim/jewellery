using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class P16UrunCariKartAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "alis_maliyeti",
                table: "urunler",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "hedef_marj",
                table: "urunler",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "kdv_orani",
                table: "urunler",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_stok",
                table: "urunler",
                type: "decimal(12,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "olcu",
                table: "urunler",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "seri_no",
                table: "urunler",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "tedarikci_id",
                table: "urunler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "dogum_tarihi",
                table: "cariler",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "iskonto_yuzde",
                table: "cariler",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kimlik_turu",
                table: "cariler",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "segment",
                table: "cariler",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "unvan",
                table: "cariler",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "uyruk",
                table: "cariler",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alis_maliyeti",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "hedef_marj",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "kdv_orani",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "min_stok",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "olcu",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "seri_no",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "tedarikci_id",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "dogum_tarihi",
                table: "cariler");

            migrationBuilder.DropColumn(
                name: "iskonto_yuzde",
                table: "cariler");

            migrationBuilder.DropColumn(
                name: "kimlik_turu",
                table: "cariler");

            migrationBuilder.DropColumn(
                name: "segment",
                table: "cariler");

            migrationBuilder.DropColumn(
                name: "unvan",
                table: "cariler");

            migrationBuilder.DropColumn(
                name: "uyruk",
                table: "cariler");
        }
    }
}
