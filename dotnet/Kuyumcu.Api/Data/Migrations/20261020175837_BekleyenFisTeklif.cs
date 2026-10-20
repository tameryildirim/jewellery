using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BekleyenFisTeklif : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kur_kilidi_id",
                table: "bekleyen_fisler",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "teklif_bitis",
                table: "bekleyen_fisler",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "teklif_no",
                table: "bekleyen_fisler",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "kur_kilidi_id",
                table: "bekleyen_fisler");

            migrationBuilder.DropColumn(
                name: "teklif_bitis",
                table: "bekleyen_fisler");

            migrationBuilder.DropColumn(
                name: "teklif_no",
                table: "bekleyen_fisler");
        }
    }
}
