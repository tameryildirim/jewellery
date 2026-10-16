using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class P77YedekTalepTuru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tur",
                table: "yedek_talepleri",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "YEDEK")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tur",
                table: "yedek_talepleri");
        }
    }
}
