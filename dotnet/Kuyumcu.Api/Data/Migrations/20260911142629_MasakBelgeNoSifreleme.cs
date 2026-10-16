using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MasakBelgeNoSifreleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "belge_no",
                table: "masak_kimlik_tespitleri",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "belge_no_hash",
                table: "masak_kimlik_tespitleri",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte>(
                name: "sifre_surumu",
                table: "masak_kimlik_tespitleri",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "IX_masak_kimlik_tespitleri_tenant_id_belge_no_hash",
                table: "masak_kimlik_tespitleri",
                columns: new[] { "tenant_id", "belge_no_hash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_masak_kimlik_tespitleri_tenant_id_belge_no_hash",
                table: "masak_kimlik_tespitleri");

            migrationBuilder.DropColumn(
                name: "belge_no_hash",
                table: "masak_kimlik_tespitleri");

            migrationBuilder.DropColumn(
                name: "sifre_surumu",
                table: "masak_kimlik_tespitleri");

            migrationBuilder.AlterColumn<string>(
                name: "belge_no",
                table: "masak_kimlik_tespitleri",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
