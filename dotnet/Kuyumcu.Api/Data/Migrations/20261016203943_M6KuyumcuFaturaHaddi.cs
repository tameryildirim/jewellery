using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class M6KuyumcuFaturaHaddi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // M6 (16.10) — kuyumcu fatura haddi: genel haddin 3 KATI (VUK GT 514 md.4, RG 04.01.2020/30998; birincil
            // metin okundu 16.10.2026). Genel had satırları: 2025 = 9.900 (577 s. GT), 2026 = 12.000 (588 s. GT).
            migrationBuilder.Sql(@"INSERT INTO yasal_parametreler (id, anahtar, deger_sayi, deger_metin, birim, gecerlilik_baslangic, gecerlilik_bitis, dayanak, kaynak_url, teyit, aciklama_sade, olusturan, olusturuldu) VALUES
('m6-kuyumcu-had-2025', 'KUYUMCU_FATURA_HADDI_TL', 29700, NULL, 'TL', '2025-01-01', '2026-01-01', 'VUK GT 514 md.4 (RG 04.01.2020/30998): işlenmiş kıymetli maden satışında genel haddin 3 katı; genel had 9.900 (577 s. GT)', 'https://www.resmigazete.gov.tr/eskiler/2020/01/20200104-4.htm', 'DOGRULANDI', 'Kuyumcu satışında bu tutarı aşınca fatura zorunludur', 'migration:M6KuyumcuFaturaHaddi', '2026-10-16 00:00:00'),
('m6-kuyumcu-had-2026', 'KUYUMCU_FATURA_HADDI_TL', 36000, NULL, 'TL', '2026-01-01', NULL, 'VUK GT 514 md.4 (RG 04.01.2020/30998): işlenmiş kıymetli maden satışında genel haddin 3 katı; genel had 12.000 (588 s. GT)', 'https://www.resmigazete.gov.tr/eskiler/2020/01/20200104-4.htm', 'DOGRULANDI', 'Kuyumcu satışında bu tutarı aşınca fatura zorunludur', 'migration:M6KuyumcuFaturaHaddi', '2026-10-16 00:00:00');");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM yasal_parametreler WHERE id IN ('m6-kuyumcu-had-2025','m6-kuyumcu-had-2026');");

        }
    }
}
