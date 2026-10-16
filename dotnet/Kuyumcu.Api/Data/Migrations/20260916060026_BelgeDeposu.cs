using Kuyumcu.Api.Data.GeriDoldurma;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <summary>
    /// PAKET 4 — <c>urun_belgeleri</c> → <c>belgeler</c> (genel belge deposu).
    ///
    /// ⚠ EF'in ürettiği taslak DropTable + CreateTable idi — mevcut sertifika
    /// satırlarını SİLERDİ. Gövde elle, VERİ KORUYAN işlemlere çevrildi:
    /// RenameTable → yeni kolonlar → geri doldurma → indeks değişimi.
    ///
    /// ⚠ <c>urun_id</c> Faz A'da DÜŞÜRÜLMEZ (Aşkın'ın koşulu): nullable kalır,
    /// geri-doldurma testinin fikstürü ancak böyle kurulabilir ve Down geri
    /// dönülemez veri üretmez. Sonraki bir migration'da düşecek.
    /// </summary>
    public partial class BelgeDeposu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "urun_belgeleri", newName: "belgeler");

            migrationBuilder.AddColumn<string>(
                name: "sahip_turu", table: "belgeler", type: "varchar(20)", maxLength: 20,
                nullable: false, defaultValue: "URUN")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "sahip_id", table: "belgeler", type: "varchar(36)", maxLength: 36,
                nullable: false, defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            // urun_id artık yazılmaz → nullable.
            migrationBuilder.AlterColumn<string>(
                name: "urun_id", table: "belgeler", type: "varchar(36)", maxLength: 36,
                nullable: true, oldClrType: typeof(string), oldType: "varchar(36)",
                oldMaxLength: 36, oldNullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            // GERİ DOLDURMA — metin testle ORTAK (BelgeSahipGeriDoldurma).
            foreach (var sql in BelgeSahipGeriDoldurma.Sirayla)
                migrationBuilder.Sql(sql);

            migrationBuilder.DropIndex(
                name: "IX_urun_belgeleri_tenant_id_urun_id", table: "belgeler");

            migrationBuilder.CreateIndex(
                name: "IX_belgeler_tenant_id_sahip_turu_sahip_id",
                table: "belgeler",
                columns: new[] { "tenant_id", "sahip_turu", "sahip_id" });

            migrationBuilder.CreateIndex(
                name: "IX_belgeler_tenant_id_sha256",
                table: "belgeler",
                columns: new[] { "tenant_id", "sha256" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Bertan'ın koşulu: Down SESSİZ VERİ KAYBI YAPMAZ ──────────────
            // urun_id eski şemada NOT NULL'du. URUN dışı sahip_turu taşıyan bir
            // satır varsa geri dönüş o satırların sahibini KAYBEDER. Yorum yazmak
            // çalışmayı durdurmaz — bu yüzden AÇIK BİR HATA fırlatılır:
            // CHECK kısıtlı geçici tabloya sayım yazılır, sayım 0 değilse MySQL
            // kısıt ihlali verir ve migration DURUR.
            // ⚠ Kapı tetiklenince INSERT patlar ve DROP satırına sıra gelmez;
            // tablo ortada kalır. Bir sonraki denemede CREATE'in "already exists"
            // ile yanlış hata vermemesi için önce temizlenir.
            migrationBuilder.Sql("DROP TABLE IF EXISTS belge_down_kapisi");
            migrationBuilder.Sql("""
                CREATE TABLE belge_down_kapisi (
                    urun_disi_satir INT NOT NULL,
                    CONSTRAINT ck_belge_down_urun_disi CHECK (urun_disi_satir = 0)
                )
                """);
            migrationBuilder.Sql("""
                INSERT INTO belge_down_kapisi (urun_disi_satir)
                SELECT COUNT(*) FROM belgeler WHERE sahip_turu <> 'URUN'
                """);
            migrationBuilder.Sql("DROP TABLE belge_down_kapisi");

            // URUN sahipli satırların urun_id'si (varsa) geri yazılır.
            migrationBuilder.Sql("""
                UPDATE belgeler
                SET urun_id = sahip_id
                WHERE sahip_turu = 'URUN' AND (urun_id IS NULL OR urun_id = '')
                """);

            migrationBuilder.DropIndex(
                name: "IX_belgeler_tenant_id_sahip_turu_sahip_id", table: "belgeler");
            migrationBuilder.DropIndex(
                name: "IX_belgeler_tenant_id_sha256", table: "belgeler");

            migrationBuilder.AlterColumn<string>(
                name: "urun_id", table: "belgeler", type: "varchar(36)", maxLength: 36,
                nullable: false, oldClrType: typeof(string), oldType: "varchar(36)",
                oldMaxLength: 36, oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.DropColumn(name: "sahip_id", table: "belgeler");
            migrationBuilder.DropColumn(name: "sahip_turu", table: "belgeler");

            migrationBuilder.CreateIndex(
                name: "IX_urun_belgeleri_tenant_id_urun_id",
                table: "belgeler",
                columns: new[] { "tenant_id", "urun_id" });

            migrationBuilder.RenameTable(name: "belgeler", newName: "urun_belgeleri");
        }
    }
}
