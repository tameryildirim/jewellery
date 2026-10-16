using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class S3bDuzeltilebilirlik : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "iptal_edildi",
                table: "kur_snapshots",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "iptal_gerekce",
                table: "kur_snapshots",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "iptal_tarihi",
                table: "kur_snapshots",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aciklama",
                table: "kasa_hareketleri",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "storno_kaynak_id",
                table: "kasa_hareketleri",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "iptal_edildi",
                table: "gider_pusulalari",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "iptal_gerekce",
                table: "gider_pusulalari",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "iptal_tarihi",
                table: "gider_pusulalari",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_kasa_hareketleri_tenant_id_storno_kaynak_id",
                table: "kasa_hareketleri",
                columns: new[] { "tenant_id", "storno_kaynak_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_kasa_hareketleri_tenant_id_storno_kaynak_id",
                table: "kasa_hareketleri");

            migrationBuilder.DropColumn(
                name: "iptal_edildi",
                table: "kur_snapshots");

            migrationBuilder.DropColumn(
                name: "iptal_gerekce",
                table: "kur_snapshots");

            migrationBuilder.DropColumn(
                name: "iptal_tarihi",
                table: "kur_snapshots");

            migrationBuilder.DropColumn(
                name: "aciklama",
                table: "kasa_hareketleri");

            migrationBuilder.DropColumn(
                name: "storno_kaynak_id",
                table: "kasa_hareketleri");

            migrationBuilder.DropColumn(
                name: "iptal_edildi",
                table: "gider_pusulalari");

            migrationBuilder.DropColumn(
                name: "iptal_gerekce",
                table: "gider_pusulalari");

            migrationBuilder.DropColumn(
                name: "iptal_tarihi",
                table: "gider_pusulalari");
        }
    }
}
