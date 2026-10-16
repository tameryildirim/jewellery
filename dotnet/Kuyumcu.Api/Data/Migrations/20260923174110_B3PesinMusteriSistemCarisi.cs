using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <summary>
    /// PAKET B-3 (karar D2 · K4) — "Peşin Müşteri" sistem carisi bayrağı.
    /// <para>
    /// KİRACI BAŞINA TEK: fonksiyonel anahtar parçalı UNIQUE indeks
    /// <c>(tenant_id, (CASE WHEN sistem_carisi = 1 THEN 1 END))</c>. Sıradan
    /// carilerde ifade NULL'dır ve MySQL UNIQUE indeksi birden çok NULL kabul
    /// eder; iki SİSTEM carisi ise reddedilir. Desen P05CariVergiNoBenzersiz ile
    /// aynıdır (Pomelo filtreli indeks modelleyemez → ham SQL; model bu indeksi
    /// BİLDİRMEZ, dolayısıyla model farkı üretmez).
    /// </para>
    /// <para>
    /// TOHUMLAMA bu migration'da YAPILMAZ: kart ilk ihtiyaç anında
    /// (GET /api/v1/cari/sistem-carisi) ADR-0027 "gerekiyorsa tohumla" deseniyle
    /// açılır — plan § S-2 madde 2.4. Mevcut kiracıların verisine dokunulmaz.
    /// </para>
    /// </summary>
    public partial class B3PesinMusteriSistemCarisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "sistem_carisi",
                table: "cariler",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX `UQ_cariler_tenant_sistem_carisi` ON `cariler` " +
                "(`tenant_id`, ((CASE WHEN `sistem_carisi` = 1 THEN 1 END)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX `UQ_cariler_tenant_sistem_carisi` ON `cariler`");

            migrationBuilder.DropColumn(
                name: "sistem_carisi",
                table: "cariler");
        }
    }
}
