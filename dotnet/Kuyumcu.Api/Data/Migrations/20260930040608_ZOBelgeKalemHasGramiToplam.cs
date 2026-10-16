using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ZOBelgeKalemHasGramiToplam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GERİ DOLDURMA (ZO-BELGE 30.09 — kalem.HasGrami TEK ANLAM = SATIRIN TOPLAM has gramı).
            // Şema değişmez. Eski kod ürün yollarında (kalemli / taslağa ekle) BİRİM has'ı
            // yazıyordu; fişten yolu zaten TOPLAMI (|hareket.has_grami|) yazıyordu.
            // Ölçüt: has gramı dolu, miktarı 1 olmayan, ürün kopyası taşıyan kalem — ve
            // bağlı defter satırının has gramına EŞİT DEĞİL (eşitse fişten yolunun toplamıdır).
            // Miktar 1 olan satırda birim = toplam, dokunulmaz.
            // ⚠ YALNIZ TASLAK faturaların kalemleri (orkestratör 30.09 07:50): kesilmiş/ödenmiş/
            // iptal belge YASAL BELGEDİR, satırına dokunulmaz. (kuyumcu_net'e ilk uygulamada
            // etkilenen satır 0 idi — düzeltme yalnız henüz uygulanmamış veritabanları içindir.)
            migrationBuilder.Sql(
                "CREATE TEMPORARY TABLE zob_hg_etkilenen (fatura_id varchar(36) NOT NULL PRIMARY KEY);");
            migrationBuilder.Sql(
                "INSERT IGNORE INTO zob_hg_etkilenen (fatura_id) " +
                "SELECT DISTINCT k.fatura_id FROM fatura_kalemleri k " +
                "JOIN faturalar f ON f.id = k.fatura_id AND f.tenant_id = k.tenant_id AND f.durum = 'TASLAK' " +
                "LEFT JOIN hareketler h ON h.id = k.hareket_id AND h.tenant_id = k.tenant_id " +
                "WHERE k.has_grami IS NOT NULL AND k.miktar <> 1 AND k.urun_id IS NOT NULL " +
                "AND (h.id IS NULL OR ROUND(ABS(h.has_grami), 6) <> k.has_grami);");
            migrationBuilder.Sql(
                "UPDATE fatura_kalemleri k " +
                "JOIN faturalar f ON f.id = k.fatura_id AND f.tenant_id = k.tenant_id AND f.durum = 'TASLAK' " +
                "LEFT JOIN hareketler h ON h.id = k.hareket_id AND h.tenant_id = k.tenant_id " +
                "SET k.has_grami = ROUND(k.has_grami * k.miktar, 6) " +
                "WHERE k.has_grami IS NOT NULL AND k.miktar <> 1 AND k.urun_id IS NOT NULL " +
                "AND (h.id IS NULL OR ROUND(ABS(h.has_grami), 6) <> k.has_grami);");
            // Yalnız TASLAK KUYUM başlıkları kalemlerden yeniden toplanır (kesilmiş belge
            // salt okunurdur; başlığına dokunulmaz). Başlık = altın kalemlerin toplam has'ı;
            // altın kalemi KALMAYAN (maden NULL / altın dışı) taslakta da başlık yeniden toplanır:
            // has_gram = 0, özet kur NULL (koddaki KalemHasGrami + OzetKur ile aynı kural).
            migrationBuilder.Sql(
                "UPDATE faturalar f JOIN zob_hg_etkilenen e ON e.fatura_id = f.id " +
                "LEFT JOIN (SELECT fatura_id, ROUND(SUM(ABS(has_grami)), 6) g FROM fatura_kalemleri " +
                "WHERE maden = 'ALTIN' GROUP BY fatura_id) x ON x.fatura_id = f.id " +
                "SET f.has_gram = COALESCE(x.g, 0), " +
                "f.has_birim_fiyat = CASE WHEN COALESCE(x.g, 0) > 0 THEN ROUND(f.has_deger / x.g, 6) ELSE NULL END " +
                "WHERE f.kdv_turu = 'KUYUM' AND f.durum = 'TASLAK';");
            migrationBuilder.Sql("DROP TEMPORARY TABLE zob_hg_etkilenen;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri dönüş YOK: toplam → birim dönüşümü hangi satırın dönüştürüldüğünü
            // bilmeden yapılamaz (fişten kalemleri zaten toplamdı). Veri kaybı yok —
            // birim has = has_grami ÷ miktar her an türetilebilir.
        }
    }
}
