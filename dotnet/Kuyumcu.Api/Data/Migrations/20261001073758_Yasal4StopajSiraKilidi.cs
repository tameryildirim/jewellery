using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Yasal4StopajSiraKilidi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stopaj_odemeleri_tenant_id_donem",
                table: "stopaj_odemeleri");

            migrationBuilder.AddColumn<int>(
                name: "sira_no",
                table: "stopaj_odemeleri",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Var olan satırlar (varsa) dönem içinde 1, 2, 3 … diye numaralanır; aksi hâlde
            // aynı dönemdeki iki eski ödeme 0/0 ile benzersiz indeksi kurdurmazdı.
            migrationBuilder.Sql(
                "UPDATE stopaj_odemeleri s JOIN (SELECT id, ROW_NUMBER() OVER "
                + "(PARTITION BY tenant_id, donem ORDER BY olusturuldu, id) AS rn FROM stopaj_odemeleri) x "
                + "ON x.id = s.id SET s.sira_no = x.rn;");

            migrationBuilder.CreateIndex(
                name: "IX_stopaj_odemeleri_tenant_id_donem_sira_no",
                table: "stopaj_odemeleri",
                columns: new[] { "tenant_id", "donem", "sira_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stopaj_odemeleri_tenant_id_donem_sira_no",
                table: "stopaj_odemeleri");

            migrationBuilder.DropColumn(
                name: "sira_no",
                table: "stopaj_odemeleri");

            migrationBuilder.CreateIndex(
                name: "IX_stopaj_odemeleri_tenant_id_donem",
                table: "stopaj_odemeleri",
                columns: new[] { "tenant_id", "donem" });
        }
    }
}
