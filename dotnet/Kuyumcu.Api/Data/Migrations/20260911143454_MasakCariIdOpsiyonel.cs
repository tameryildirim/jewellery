using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MasakCariIdOpsiyonel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "cari_id",
                table: "masak_kimlik_tespitleri",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldMaxLength: 36)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "masak_kimlik_tespitleri",
                keyColumn: "cari_id",
                keyValue: null,
                column: "cari_id",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "cari_id",
                table: "masak_kimlik_tespitleri",
                type: "varchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldMaxLength: 36,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
