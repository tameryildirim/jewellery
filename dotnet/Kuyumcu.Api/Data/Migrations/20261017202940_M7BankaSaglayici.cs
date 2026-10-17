using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class M7BankaSaglayici : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "saglayici_kodu",
                table: "banka_hesaplari",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "son_cekim",
                table: "banka_hesaplari",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "saglayici_referans",
                table: "banka_hareketleri",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sahte_banka_hareketleri",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    banka_hesap_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tutar = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    gonderen_ad = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gonderen_iban = table.Column<string>(type: "varchar(34)", maxLength: 34, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gonderen_kimlik_no = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    aciklama = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    olusturuldu = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sahte_banka_hareketleri", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_banka_hareketleri_tenant_id_banka_hesap_id_saglayici_referans",
                table: "banka_hareketleri",
                columns: new[] { "tenant_id", "banka_hesap_id", "saglayici_referans" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sahte_banka_hareketleri_tenant_id_banka_hesap_id_olusturuldu",
                table: "sahte_banka_hareketleri",
                columns: new[] { "tenant_id", "banka_hesap_id", "olusturuldu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sahte_banka_hareketleri");

            migrationBuilder.DropIndex(
                name: "IX_banka_hareketleri_tenant_id_banka_hesap_id_saglayici_referans",
                table: "banka_hareketleri");

            migrationBuilder.DropColumn(
                name: "saglayici_kodu",
                table: "banka_hesaplari");

            migrationBuilder.DropColumn(
                name: "son_cekim",
                table: "banka_hesaplari");

            migrationBuilder.DropColumn(
                name: "saglayici_referans",
                table: "banka_hareketleri");
        }
    }
}
