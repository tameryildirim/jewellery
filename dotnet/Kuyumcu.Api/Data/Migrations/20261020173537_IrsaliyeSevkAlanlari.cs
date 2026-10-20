using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IrsaliyeSevkAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fiili_sevk_tarihi",
                table: "irsaliyeler",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plaka",
                table: "irsaliyeler",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "tasiyan_ad",
                table: "irsaliyeler",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "teslim_alan_ad",
                table: "irsaliyeler",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "teslim_tarihi",
                table: "irsaliyeler",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fiili_sevk_tarihi",
                table: "irsaliyeler");

            migrationBuilder.DropColumn(
                name: "plaka",
                table: "irsaliyeler");

            migrationBuilder.DropColumn(
                name: "tasiyan_ad",
                table: "irsaliyeler");

            migrationBuilder.DropColumn(
                name: "teslim_alan_ad",
                table: "irsaliyeler");

            migrationBuilder.DropColumn(
                name: "teslim_tarihi",
                table: "irsaliyeler");
        }
    }
}
