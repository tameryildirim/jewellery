using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class YasalParametreIcNot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ic_not",
                table: "yasal_parametreler",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // ── YASAL-1 tur 2 (tasarım hakemi KRİTİK): dayanak alanı YALNIZ resmî atıf
            //    taşır (ekranda basılır). Teyit durumu, şartname atfı ve "kural kodlanmadı"
            //    gibi iç notlar ic_not'a taşınır (ekrana/API'ye basılmaz). Resmî atfı
            //    doğrulanamayan satırda dayanak BOŞ kalır → ekran "henüz doğrulanmadı" yazar.
            //    Tohum dışı satırlara DOKUNULMAZ (WHERE id = tohum). Append-only kuralının
            //    istisnası: geçmiş düzeltmesi YALNIZ migration ile (ADR-0005).
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '459 sıra no.lu VUK Genel Tebliği (RG 24.12.2015/29572)', ic_not = 'yürürlük 01.01.2016 (Tebliğin yürürlük maddesi)' WHERE id = 'yasal1-tohum-01';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '575 sıra no.lu VUK Genel Tebliği (RG 30.11.2024/32738)', ic_not = 'md.5: yayımı tarihinde yürürlük' WHERE id = 'yasal1-tohum-02';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'VUK mükerrer md.355 (7524 sayılı Kanun md.13, RG 02.08.2024/32620)', ic_not = NULL WHERE id = 'yasal1-tohum-03';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.5/1-b (ilk metin)', ic_not = 'yürürlük 01.04.2008; bitiş 3580 s. CK, RG 24.02.2021/31405 — CK yürürlük maddesi ayrıca okunmadı' WHERE id = 'yasal1-tohum-04';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.5/1-b (3580 sayılı CK, RG 24.02.2021/31405)', ic_not = 'CK yürürlük maddesi ayrıca okunmadı' WHERE id = 'yasal1-tohum-05';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.5/1-b (5739 sayılı CK, RG 16.06.2022/31868)', ic_not = 'CK yürürlük maddesi ayrıca okunmadı' WHERE id = 'yasal1-tohum-06';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.5/1-b (6702 sayılı CK, RG 14.01.2023/32073)', ic_not = NULL WHERE id = 'yasal1-tohum-07';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '', ic_not = 'Dayanağı bulunamadı (şartname § 3.2) — bugünkü davranışı korumak için tutulur; başlangıç = yönetmelik yürürlüğü' WHERE id = 'yasal1-tohum-08';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '', ic_not = 'Nakit İşlem Beyan Formu tebliğinin RG metni doğrulanamadı (şartname § 3.2, M-06) — bugünkü davranış korunur' WHERE id = 'yasal1-tohum-09';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '', ic_not = 'Nakit İşlem Beyan Formu tebliğinin RG metni doğrulanamadı (şartname § 3.2, M-06) — bugünkü davranış korunur' WHERE id = 'yasal1-tohum-10';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.28/2', ic_not = 'ilk metin, değişiklik dipnotu yok; yürürlük md.51: 01.04.2008' WHERE id = 'yasal1-tohum-11';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.46/1', ic_not = 'ilk metin, değişiklik dipnotu yok; yürürlük md.51: 01.04.2008' WHERE id = 'yasal1-tohum-12';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'VUK md.234 (7338 sayılı Kanun md.23, RG 26.10.2021/31640)', ic_not = 'yürürlük: yayımı takip eden ay başı' WHERE id = 'yasal1-tohum-13';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'GVK md.94/13 ve 2009/14592 sayılı BKK (RG 03.02.2009/27130)', ic_not = 'sonraki değişiklik doğrulanamadı' WHERE id = 'yasal1-tohum-14';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '', ic_not = 'GVK md.94 — mükellef olmayan kişiden alıma özgü bent bulunamadı (şartname § 3.2)' WHERE id = 'yasal1-tohum-15';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '', ic_not = 'GİB beyan ve ödeme süreleri belgesi — dayanak tebliğ doğrulanamadı (şartname § 3.2)' WHERE id = 'yasal1-tohum-16';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '', ic_not = 'KDV genel oranı — bu pakette birincil kaynak okunmadı; bugünkü yapılandırma değeri (Kdv:VarsayilanOran)' WHERE id = 'yasal1-tohum-17';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '', ic_not = 'GİB UBL-TR kod listesi (özel matrah) — kural K-10 teyidine kadar KAPALI; ekranda teknik terim basılmaz' WHERE id = 'yasal1-tohum-18';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'VUK md.232; 577 sıra no.lu VUK Genel Tebliği (RG 30.12.2024/32768 2. Mükerrer)', ic_not = 'kural bu pakette kodlanmadı' WHERE id = 'yasal1-tohum-19';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'VUK md.232; 588 sıra no.lu VUK Genel Tebliği (RG 31.12.2025/33124 5. Mükerrer)', ic_not = 'kural bu pakette kodlanmadı' WHERE id = 'yasal1-tohum-20';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '', ic_not = 'KMTS resmî kod biçimi yayımlanmadı — KAPALI yer tutucu (YASAL-4 § 4.4)' WHERE id = 'yasal1-tohum-21';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET birim = 'AYIN_GUNU' WHERE anahtar = 'STOPAJ_BEYAN_GUNU';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '459 s. VUK Genel Tebliği, RG 24.12.2015/29572 (yürürlük 01.01.2016)' WHERE id = 'yasal1-tohum-01';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = '575 s. VUK Genel Tebliği, RG 30.11.2024/32738 (yayımı tarihinde yürürlük)' WHERE id = 'yasal1-tohum-02';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'VUK mükerrer md.355 (7524 s. K. md.13, RG 02.08.2024/32620)' WHERE id = 'yasal1-tohum-03';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.5/1-b ilk metin (yürürlük 01.04.2008); bitiş 3580 s. CK, RG 24.02.2021/31405 — CK yürürlük maddesi ayrıca okunmadı' WHERE id = 'yasal1-tohum-04';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.5/1-b (3580 s. CK, RG 24.02.2021/31405) — CK yürürlük maddesi ayrıca okunmadı' WHERE id = 'yasal1-tohum-05';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.5/1-b (5739 s. CK, RG 16.06.2022/31868) — CK yürürlük maddesi ayrıca okunmadı' WHERE id = 'yasal1-tohum-06';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.5/1-b (6702 s. CK, RG 14.01.2023/32073)' WHERE id = 'yasal1-tohum-07';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Dayanağı bulunamadı (şartname § 3.2) — bugünkü davranışı korumak için tutulur; başlangıç = yönetmelik yürürlüğü' WHERE id = 'yasal1-tohum-08';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Nakit İşlem Beyan Formu tebliğinin RG metni doğrulanamadı (şartname § 3.2, M-06) — bugünkü davranış korunur' WHERE id = 'yasal1-tohum-09';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Nakit İşlem Beyan Formu tebliğinin RG metni doğrulanamadı (şartname § 3.2, M-06) — bugünkü davranış korunur' WHERE id = 'yasal1-tohum-10';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.28/2 (ilk metin, değişiklik dipnotu yok; yürürlük md.51: 01.04.2008)' WHERE id = 'yasal1-tohum-11';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'Tedbirler Yönetmeliği md.46/1 (ilk metin, değişiklik dipnotu yok; yürürlük md.51: 01.04.2008)' WHERE id = 'yasal1-tohum-12';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'VUK md.234 (7338 s. K. md.23, RG 26.10.2021/31640; yürürlük: yayımı takip eden ay başı)' WHERE id = 'yasal1-tohum-13';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'GVK md.94/13 + 2009/14592 BKK (RG 03.02.2009/27130) — sonraki değişiklik doğrulanamadı' WHERE id = 'yasal1-tohum-14';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'GVK md.94 — mükellef olmayan kişiden alıma özgü bent bulunamadı (şartname § 3.2)' WHERE id = 'yasal1-tohum-15';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'GİB beyan ve ödeme süreleri belgesi — dayanak tebliğ doğrulanamadı (şartname § 3.2)' WHERE id = 'yasal1-tohum-16';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'KDV genel oranı — bu pakette birincil kaynak okunmadı; bugünkü yapılandırma değeri (Kdv:VarsayilanOran)' WHERE id = 'yasal1-tohum-17';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'GİB UBL-TR kod listesi (özel matrah) — kural K-10 teyidine kadar KAPALI' WHERE id = 'yasal1-tohum-18';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'VUK md.232; 577 s. VUK GT (RG 30.12.2024/32768 2. Mük.) — kural bu pakette kodlanmadı' WHERE id = 'yasal1-tohum-19';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'VUK md.232; 588 s. VUK GT (RG 31.12.2025/33124 5. Mük.) — kural bu pakette kodlanmadı' WHERE id = 'yasal1-tohum-20';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET dayanak = 'KMTS resmî kod biçimi yayımlanmadı — KAPALI yer tutucu (YASAL-4 § 4.4)' WHERE id = 'yasal1-tohum-21';");
            migrationBuilder.Sql(@"UPDATE yasal_parametreler SET birim = 'GUN' WHERE anahtar = 'STOPAJ_BEYAN_GUNU';");
            migrationBuilder.DropColumn(
                name: "ic_not",
                table: "yasal_parametreler");
        }
    }
}
