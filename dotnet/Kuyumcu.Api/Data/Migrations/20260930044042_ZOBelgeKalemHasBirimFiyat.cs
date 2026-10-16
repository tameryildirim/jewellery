using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ZOBelgeKalemHasBirimFiyat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "has_birim_fiyat",
                table: "fatura_kalemleri",
                type: "decimal(15,6)",
                nullable: true);

            // GERİ DOLDURMA — yalnız TASLAK faturaların KUYUM + ALTIN kalemleri (kesilmiş/ödenmiş/
            // iptal belge yasal belgedir, satırına dokunulmaz; PATCH yalnız taslakta çalışır).
            // (1) Fişten kalem: bağlı hareketin KUR KAYDI (kur_snapshots.altin_has_fiyati) —
            //     oluşturmada motorun kullandığı değerin AYNISI.
            migrationBuilder.Sql(
                "UPDATE fatura_kalemleri k " +
                "JOIN faturalar f ON f.id = k.fatura_id AND f.tenant_id = k.tenant_id AND f.durum = 'TASLAK' " +
                "JOIN hareketler h ON h.id = k.hareket_id AND h.tenant_id = k.tenant_id " +
                "JOIN kur_snapshots s ON s.id = h.kur_snapshot_id AND s.tenant_id = k.tenant_id " +
                "SET k.has_birim_fiyat = s.altin_has_fiyati " +
                "WHERE k.has_birim_fiyat IS NULL AND k.kdv_turu = 'KUYUM' AND k.maden = 'ALTIN' " +
                "AND k.has_grami > 0 AND s.altin_has_fiyati > 0;");
            // (2) Diğerleri (ürün yolları; kur kaydı bulunamayan fişten kalem): ESKİ KAYIT İÇİN
            //     tek çıkar yol olarak has_deger ÷ has_grami (6 hane) GERİ ÇÖZÜLÜR. Bu değer
            //     kur kaydının kendisi değil, yuvarlanmış bedelden türetilmiş YAKLAŞIKTIR; yalnız
            //     bu migration öncesi kalemler için kullanılır (yeni kalemde kolon oluşurken
            //     kur kaydından yazılır). Has bedeli ya da has gramı olmayan kalem NULL kalır →
            //     düzeltmede özel matrah uygulanmaz (fail-closed).
            migrationBuilder.Sql(
                "UPDATE fatura_kalemleri k " +
                "JOIN faturalar f ON f.id = k.fatura_id AND f.tenant_id = k.tenant_id AND f.durum = 'TASLAK' " +
                "SET k.has_birim_fiyat = ROUND(k.has_deger / k.has_grami, 6) " +
                "WHERE k.has_birim_fiyat IS NULL AND k.kdv_turu = 'KUYUM' AND k.maden = 'ALTIN' " +
                "AND k.has_grami > 0 AND k.has_deger > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_birim_fiyat",
                table: "fatura_kalemleri");
        }
    }
}
