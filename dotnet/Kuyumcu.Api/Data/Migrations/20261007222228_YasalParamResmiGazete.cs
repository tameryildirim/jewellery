using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN — yasal_parametreler'e Resmî Gazete künyesi (tarih + sayı).
    /// <para>
    /// Sistem ekranından yazılan her yeni satırda ikisi ZORUNLU (YasalParametreServisi.Dogrula);
    /// kuyumcu ekranı kaynağı "RG tarih / sayı" olarak gösterir. Eski tohum satırlarında künye
    /// UYDURULMAZ: NULL kalır. Tek istisna bu sürümden bir önceki migration'ın satırı
    /// <c>masak370-01</c>: künyesi birincil kaynakla doğrulanmış ve ic_not alanında yazılıdır
    /// (RG 07.10.2026 sayı 33393 — MEVZUAT-KUYUMCU-BELGE-2026-10-07.md § 2;
    /// https://www.resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf). Değer, başlangıç ve
    /// damga DEĞİŞMEZ; yalnız yeni künye sütunları doldurulur (geçmiş yeniden yazılmaz).
    /// </para>
    /// </summary>
    public partial class YasalParamResmiGazete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "resmi_gazete_sayisi",
                table: "yasal_parametreler",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "resmi_gazete_tarihi",
                table: "yasal_parametreler",
                type: "date",
                nullable: true);

            // Künyesi birincil kaynakla doğrulanmış tek satır (yukarıdaki not).
            migrationBuilder.Sql(
                "UPDATE yasal_parametreler SET resmi_gazete_tarihi = '2026-10-07', resmi_gazete_sayisi = '33393' "
                + "WHERE id = 'masak370-01' AND resmi_gazete_tarihi IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "resmi_gazete_sayisi",
                table: "yasal_parametreler");

            migrationBuilder.DropColumn(
                name: "resmi_gazete_tarihi",
                table: "yasal_parametreler");
        }
    }
}
