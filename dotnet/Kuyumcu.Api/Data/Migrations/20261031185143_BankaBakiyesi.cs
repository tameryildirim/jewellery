using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BankaBakiyesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "banka_bakiye",
                table: "banka_hesaplari",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "banka_bakiye_zamani",
                table: "banka_hesaplari",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "banka_kullanilabilir",
                table: "banka_hesaplari",
                type: "decimal(15,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "banka_bakiye",
                table: "banka_hesaplari");

            migrationBuilder.DropColumn(
                name: "banka_bakiye_zamani",
                table: "banka_hesaplari");

            migrationBuilder.DropColumn(
                name: "banka_kullanilabilir",
                table: "banka_hesaplari");
        }
    }
}
