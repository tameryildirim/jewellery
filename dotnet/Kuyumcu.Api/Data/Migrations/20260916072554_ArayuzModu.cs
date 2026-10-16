using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArayuzModu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "varsayilan_arayuz_modu",
                table: "tenants",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "PROFESYONEL")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "arayuz_modu",
                table: "kullanicilar",
                type: "varchar(12)",
                maxLength: 12,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "varsayilan_arayuz_modu",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "arayuz_modu",
                table: "kullanicilar");
        }
    }
}
