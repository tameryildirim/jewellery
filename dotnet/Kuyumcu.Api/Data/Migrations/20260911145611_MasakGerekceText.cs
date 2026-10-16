using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MasakGerekceText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "gerekce",
                table: "masak_supheli_bildirimler",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(2000)",
                oldMaxLength: 2000)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "gerekce",
                table: "masak_supheli_bildirimler",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
