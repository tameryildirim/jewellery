using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <summary>
    /// Paket 28 — vade takibi. ⚠ Bu dosya 2026-09-15'te `dotnet ef migrations remove --force`
    /// kazasıyla silindi ve ürettiği anda kaydedilen içerikle (A11) elle geri yazıldı;
    /// kuyumcu_net'te aynı kimlikle zaten uygulanmıştır. Designer dosyası yalnız
    /// öznitelikleri taşır (BuildTargetModel gerekmez).
    /// </summary>
    public partial class VadeTakibi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "vade_tarihi",
                table: "hareketler",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "varsayilan_vade_gun",
                table: "cariler",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "vade_tarihi", table: "hareketler");
            migrationBuilder.DropColumn(name: "varsayilan_vade_gun", table: "cariler");
        }
    }
}
