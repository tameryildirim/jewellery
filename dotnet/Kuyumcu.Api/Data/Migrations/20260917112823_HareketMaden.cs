using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// PAKET 20 Faz A — hareketler.maden + urunler.rodyum_kaplama.
    ///
    /// <para>
    /// ⚠ <b>GERİ DOLDURMA BİR VARSAYIMDIR</b> (Anayasa md.1 · K20-10). Mevcut
    /// satırların madeni ÜRÜNÜN BUGÜNKÜ AYARINDAN türetilir; ürünü olmayan
    /// satır 'ALTIN' bırakılır. Ürünün ayarı satıştan SONRA değiştiyse geçmiş
    /// satır YANLIŞ etiketlenir — bu bilinen ve ölçülmüş bir sapmadır, gizlenmez.
    /// </para>
    /// <para>
    /// SEVK ÖNCESİ ÖLÇÜM (kuyumcu_net, 2026-09-17, Up çalıştırılmadan ÖNCE):
    ///   • hareketler toplam .......................... 128
    ///   • ürünsüz satır .............................. 34 (bunların 21'inin has bacağı ≠ 0)
    ///       – HURDA_ALIS 7 · EMANET_GIRIS 11 · EMANET_CIKIS 3
    ///         (hurda ve emanetin ayarı harekette SAKLANMIYOR; bu satırlar
    ///          ALTIN varsayılır — hepsi altın akışıydı, doğrulandı)
    ///   • altın dışı ayarlı ürün ..................... 4 (hepsi G925)
    ///   • altın dışı ayarlı üründen doğan hareket .... 3 → GUMUS olarak etiketlenir
    ///   • ayarı bir satıştan SONRA güncellenmiş ürün . 7 (ÜST SINIR: "guncellendi"
    ///     damgası ayar dışı bir alan değiştiğinde de ilerler; kaçının ayarının
    ///     fiilen değiştiği veri modelinde SAKLANMIYOR — tahmin edilmedi)
    /// </para>
    /// <para>
    /// Bu migration DenetimKaydi DÜŞÜRMEZ (SaveChanges dışıdır); varsayım ve
    /// sayılar sevk raporunda (PAKET-20-uygulama-raporu.md) yazılıdır.
    /// </para>
    /// </summary>
    public partial class HareketMaden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "rodyum_kaplama",
                table: "urunler",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "maden",
                table: "hareketler",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                // Varsayılan ALTIN: kolon eklendiği anda mevcut 128 satırın
                // hepsi ALTIN olur; aşağıdaki geri doldurma yalnız ÜRÜNÜ OLAN
                // satırları ürünün ayarına göre düzeltir.
                defaultValue: "ALTIN")
                .Annotation("MySql:CharSet", "utf8mb4");

            // ── GERİ DOLDURMA (K20-10) ───────────────────────────────────────
            // Ürünü olan satırın madeni ürünün BUGÜNKÜ ayarından türetilir.
            // Ayar kodu öneki maden demektir: G* gümüş, PT* platin, PD* paladyum,
            // CELIK çelik, kalanı altın. Ürünü olmayan satır ALTIN kalır.
            migrationBuilder.Sql(@"
                UPDATE hareketler h
                JOIN urunler u ON u.id = h.urun_id
                SET h.maden = CASE
                    WHEN u.ayar_id LIKE 'G%'  THEN 'GUMUS'
                    WHEN u.ayar_id LIKE 'PT%' THEN 'PLATIN'
                    WHEN u.ayar_id LIKE 'PD%' THEN 'PALADYUM'
                    WHEN u.ayar_id = 'CELIK'  THEN 'CELIK'
                    ELSE 'ALTIN'
                END;");

            migrationBuilder.CreateIndex(
                name: "IX_hareketler_tenant_id_maden_tarih",
                table: "hareketler",
                columns: new[] { "tenant_id", "maden", "tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hareketler_tenant_id_maden_tarih",
                table: "hareketler");

            migrationBuilder.DropColumn(
                name: "rodyum_kaplama",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "maden",
                table: "hareketler");
        }
    }
}
