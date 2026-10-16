using System.Text.RegularExpressions;

namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET Q10-5 — ORTAK FORM HATASI + KALAN JARGON + TABLO/MOBİL + BANKA EKSTRE
// (QA turu 10, 24.09 — gerçek tarayıcı bulguları)
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin Sozlukler.<Alan>.cs). Burada YALNIZ bu paketin yeni metinleri durur.
// ⚠ Basit mod metinleri PROFESYONEL metnin yerine GEÇMEZ; sayfa, basit modda
//   bu metni okur (D10 · K8). Profesyonel mod muhasebe dilini korur.
// ⚠ Mevzuat hükmü DEĞİŞMEZ: basit metinler yalnız aynı kuralı jargonsuz
//   söyler (özel matrah "HARİÇ" beyanı, açılış kuralı, storno yolu). Hiçbir
//   eşik/oran/kod bu dosyada yazılı DEĞİLDİR.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Q10-5 metinleri (bir dilde). Konumsal kayıt: eksik dil = derleme hatası.</summary>
public sealed record Q105Metinleri(
    // ── /veri-disa-aktarim kapsam adları (QA 10: kebab-case kod adı sızıntısı) ──
    string KapsamPosEslesmeleri,
    string KapsamSatinalma,
    string KapsamIrsaliye,
    string KapsamMuhasebeFisi,
    string KisiselVeriNotuBasit,
    // ── /vezne ──
    string VezneDefterKapilariBasit,
    string VezneKalemSayiHatasi,
    // ── /faturalar özel matrah beyanı (basit modda katlanır tek satır) ──
    string OzelMatrahOzetBasit,
    string OzelMatrahKapaliBasit,
    // ── /kurulum ──
    string KurulumNegatifAcilisBasit,
    string KurulumMuhurOnayBasit,
    // ── /kilavuz ──
    string KilavuzFaturaBolumuBasit,
    // ── /banka dekont değişmezlik metinleri (basit) ──
    string DekontStornoYokBasit,
    string DekontKilitliAlanlarBasit,
    string DekontDegismezYoluBasit,
    string BakiyeliHesapUyarisi,
    // ── /banka/ekstre ──
    string EkstreAciklamaBasit,
    string EkstreOnaylananYoluBasit,
    string EkstreAtlandi,
    string EkstreSatirAtlandi,
    string EkstreSatirGeriAlindi,
    string EkstreCariBaglandi,
    string EkstreCariKaldirildi,
    string EkstreKilitOnaylandi,
    string EkstreKilitAtlandi,
    string EkstreKilitHatali,
    string EkstreSahneDegismez,
    string EkstreSahneYolu,
    string IthalatProva,
    string IthalatOnaylandi,
    string IthalatIptal,
    string GerekceIpucuBasit,
    string GerekceKisaBasit);

/// <summary>Q10-5 sözlüğü + basit mod gösterim yardımcıları.</summary>
public static class Q105Sozlugu
{
    public static readonly Q105Metinleri Tr = new(
        KapsamPosEslesmeleri: "POS–banka eşleşmeleri",
        KapsamSatinalma: "Satın alma siparişleri",
        KapsamIrsaliye: "İrsaliyeler",
        KapsamMuhasebeFisi: "Muhasebeciye fatura dökümü",
        KisiselVeriNotuBasit: "Seçtiğiniz bölümlerde kişisel bilgi var (müşteriler, gider pusulaları). Hepsi seçili gelir; gerekmiyorsa işareti kaldırın. Her dışa aktarma kayda geçer.",
        VezneDefterKapilariBasit: "Tahsil ederken yasal kontroller kendiliğinden yapılır.",
        VezneKalemSayiHatasi: "Ürün seçtiğiniz satırda adet ve birim fiyat sıfırdan büyük olmalı.",
        OzelMatrahOzetBasit: "Özel matrah kodu faturaya basılmıyor (ayrıntı için tıklayın)",
        OzelMatrahKapaliBasit: "Bu kurulumda faturaya özel matrah kodu yazılmaz. Mali müşavir onay verince ayarlardan tek adımda açılır.",
        KurulumNegatifAcilisBasit: "Kasa ve banka açılışı eksi olamaz: kasada eksi para bulunmaz. Eksi bakiye bir borçtur; borcu müşteri bakiyesi olarak girin.",
        KurulumMuhurOnayBasit: "Bu işlem geri alınamaz. Mühürden sonra açılış bakiyesi bir daha girilemez ve değiştirilemez; düzeltme ancak ters kayıt ya da müşteri düzeltme fişiyle yapılır.",
        KilavuzFaturaBolumuBasit: "Faturalar ve kuyum KDV'si",
        DekontStornoYokBasit: "Bu dekont geri alınamaz: ya zaten geri alınmıştır, ya faturaya dönüştürülmüştür, ya da banka ekstresinden gelmiştir.",
        DekontKilitliAlanlarBasit: "Tutar, yön ve para birimi değiştirilemez; yanlışsa dekont geri alınır.",
        DekontDegismezYoluBasit: "Tutar ya da yön yanlışsa dekontu \"Geri al (düzeltme fişi)\" ile geri alın ve doğrusunu yeni dekont olarak girin; açıklama, karşı taraf ve tarih doğrudan düzeltilebilir.",
        BakiyeliHesapUyarisi: "Bakiyesi sıfır olmayan banka hesabı kullanımdan kaldırılamaz — para nereye gitti sorusu cevapsız kalır.",
        EkstreAciklamaBasit: "Yüklenen ekstre satırlarını gözden geçirin, müşteriye bağlayın ve seçtiklerinizi banka hareketine çevirin.",
        EkstreOnaylananYoluBasit: "Yanlışsa, bu satırdan oluşan banka hareketini Banka ekranında \"Geri al (düzeltme fişi)\" ile geri alın.",
        EkstreAtlandi: "Atlandı",
        EkstreSatirAtlandi: "{0}. satır atlandı; banka hareketine çevrilmeyecek.",
        EkstreSatirGeriAlindi: "{0}. satırın atlanması geri alındı; satır yeniden onaylanabilir.",
        EkstreCariBaglandi: "{0}. satıra müşteri bağlandı.",
        EkstreCariKaldirildi: "{0}. satırın müşteri bağı kaldırıldı.",
        EkstreKilitOnaylandi: "Onaylandı: bu satırdan banka hareketi oluştu, yeniden seçilemez.",
        EkstreKilitAtlandi: "Atlandı: seçmek için önce \"Atlamayı geri al\".",
        EkstreKilitHatali: "Hatalı satır okunamadı; onaylanamaz.",
        EkstreSahneDegismez: "Ekstre satırının tarihi, tutarı, yönü, açıklaması ve karşı tarafı bankanın dosyasından gelir; burada değiştirilirse ekran bankanın ekstresini tutmaz.",
        EkstreSahneYolu: "Satır yanlışsa \"Atla\" ile dışarıda bırakın ve doğrusunu Banka ekranından elle dekont olarak girin; ya da bankadan düzeltilmiş dosyayı alıp yeniden yükleyin.",
        IthalatProva: "Önizlemede (deftere henüz yazılmadı)",
        IthalatOnaylandi: "Onaylandı",
        IthalatIptal: "İptal edildi",
        // Doğrulama turu (24.09, tr-TR küçük harfle K8): BASİT modda "denetim izine" yerine menü adı.
        GerekceIpucuBasit: "En az 20 karakter. Bu metin \"Kim ne yaptı\" kaydına yazılır ve silinemez.",
        GerekceKisaBasit: "Gerekçe en az {0} karakter olmalıdır — bu işlem \"Kim ne yaptı\" kaydına bu gerekçeyle yazılır.");

    public static readonly Q105Metinleri En = new(
        KapsamPosEslesmeleri: "POS–bank matches",
        KapsamSatinalma: "Purchase orders",
        KapsamIrsaliye: "Delivery notes",
        KapsamMuhasebeFisi: "Invoice listing for the accountant",
        KisiselVeriNotuBasit: "The selected sections contain personal data (customers, expense receipts). All are selected by default; untick what you do not need. Every export is recorded.",
        VezneDefterKapilariBasit: "Legal checks run automatically when you collect payment.",
        VezneKalemSayiHatasi: "On a line with a product, quantity and unit price must be greater than zero.",
        OzelMatrahOzetBasit: "The special VAT base code is not printed on invoices (click for details)",
        OzelMatrahKapaliBasit: "This installation prints no special VAT base code on invoices. Once the accountant approves, it is enabled in settings in one step.",
        KurulumNegatifAcilisBasit: "Cash and bank openings cannot be negative: a cash drawer cannot hold negative money. A negative balance is a debt; enter it as a customer balance.",
        KurulumMuhurOnayBasit: "This cannot be undone. After sealing, opening balances can no longer be entered or changed; corrections are made only with a reversal or a customer correction slip.",
        KilavuzFaturaBolumuBasit: "Invoices and jewellery VAT",
        DekontStornoYokBasit: "This bank slip cannot be undone: it is already undone, has been turned into an invoice, or came from a bank statement.",
        DekontKilitliAlanlarBasit: "Amount, direction and currency cannot be changed; if wrong, the slip is undone.",
        DekontDegismezYoluBasit: "If the amount or direction is wrong, use \"Undo (correction slip)\" and enter the correct slip again; description, counterparty and date can be corrected directly.",
        BakiyeliHesapUyarisi: "A bank account with a non-zero balance cannot be deactivated — the question of where the money went would go unanswered.",
        EkstreAciklamaBasit: "Review the uploaded statement lines, link them to customers and turn the selected ones into bank transactions.",
        EkstreOnaylananYoluBasit: "If it is wrong, undo the bank transaction created from this line on the Bank screen with \"Undo (correction slip)\".",
        EkstreAtlandi: "Skipped",
        EkstreSatirAtlandi: "Line {0} skipped; it will not become a bank transaction.",
        EkstreSatirGeriAlindi: "Line {0} is no longer skipped; it can be approved again.",
        EkstreCariBaglandi: "Customer linked to line {0}.",
        EkstreCariKaldirildi: "Customer link removed from line {0}.",
        EkstreKilitOnaylandi: "Approved: a bank transaction was created from this line; it cannot be selected again.",
        EkstreKilitAtlandi: "Skipped: use \"Undo skip\" first to select it.",
        EkstreKilitHatali: "The faulty line could not be read; it cannot be approved.",
        EkstreSahneDegismez: "A statement line's date, amount, direction, description and counterparty come from the bank's file; changing them here would make the screen disagree with the bank statement.",
        EkstreSahneYolu: "If a line is wrong, leave it out with \"Skip\" and enter the correct slip manually on the Bank screen, or get a corrected file from the bank and upload it again.",
        IthalatProva: "In preview (not written to the ledger yet)",
        IthalatOnaylandi: "Approved",
        IthalatIptal: "Cancelled",
        GerekceIpucuBasit: "At least 20 characters. This text is written to the \"Who did what\" log and cannot be deleted.",
        GerekceKisaBasit: "Reason must be at least {0} characters — this action is written to the \"Who did what\" log with this reason.");

    public static Q105Metinleri Al(string dil) => dil == "en" ? En : Tr;

    // ── /denetim-izi — basit modda Detay sütunu (QA 10 · K8) ─────────────────
    // Detay metnini SUNUCU yazar ("liste: 5 kayıt · islem-defteri",
    // "arayuz-modu: BASIT"). Bu DEFTER VERİSİDİR ve DEĞİŞMEZ; yalnız GÖSTERİMDE,
    // basit modda bilinen kod adları okunur sözcüklere çevrilir. Profesyonel
    // modda metin olduğu gibi kalır (denetçi ham kaydı görür).

    // SIRA ÖNEMLİ (24.09 canlı ölçümü): önce bilinen kod adları, sonra kalan
    // kebab-case kodlar boşluklu sözcüğe, EN SON "cari" sözcükleri. Ters sırada
    // "cari-yaslandirma" → "müşteri-yaslandirma" olup yine kod adı gibi
    // okunuyordu (K8 ölçümü "teri-yaslandirma" yakaladı).
    private static readonly (Regex Kalip, string Tr, string En)[] KodAdlari =
    [
        (new(@"(?<![\p{L}-])islem-defteri(?![\p{L}-])", RegexOptions.CultureInvariant), "işlem defteri", "transaction ledger"),
        (new(@"(?<![\p{L}-])arayuz-modu(?![\p{L}-])", RegexOptions.CultureInvariant), "görünüm", "view mode"),
        (new(@"(?<![\p{L}-])cari-yaslandirma(?![\p{L}-])", RegexOptions.CultureInvariant), "müşteri yaşlandırma", "customer ageing"),
    ];

    private static readonly (Regex Kalip, string Tr, string En)[] DenetimSozcukleri =
    [
        (new(@"(?<![\p{L}])denetim izi(?![\p{L}])", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), "kim ne yaptı kaydı", "activity log"),
        (new(@"(?<![\p{L}])cari listesi(?![\p{L}])", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), "müşteri listesi", "customer list"),
        (new(@"(?<![\p{L}])cariler(?![\p{L}])", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), "müşteriler", "customers"),
        (new(@"(?<![\p{L}])cari(?![\p{L}])", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), "müşteri", "customer"),
        (new(@"(?<![\p{L}])storno(?![\p{L}])", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), "geri alma", "undo"),
    ];

    // Kalan küçük harfli kebab-case kod adı ("gider-pusulasi") → boşluklu sözcük.
    // "e-posta" Türkçe yazımdır (TDK), kod adı DEĞİLDİR — dokunulmaz.
    private static readonly Regex KebabKod =
        new(@"(?<![\p{L}\p{N}/.-])(?!e-posta(?![\p{L}]))[a-z]+(?:-[a-z]+)+(?![\p{L}\p{N}/.-])", RegexOptions.CultureInvariant);

    private static readonly Regex ListeBasi =
        new(@"(?<![\p{L}])liste: (\d+) kayıt(?![\p{L}])", RegexOptions.CultureInvariant);

    // Sunucunun ListeOkumaKaydet/OkumaKaydet çağrılarında yazdığı sabit gerekçeler (DenetimIzi.cs çağrı yerleri).
    private static readonly (string Tr, string En)[] DenetimCumleleri =
    [
        ("gider pusulası belgesi YAZDIRILDI (basım)", "expense voucher document PRINTED"),
        ("gider pusulası belgesi görüntülendi", "expense voucher document viewed"),
        ("gider pusulası görüntülendi", "expense voucher viewed"),
        ("gider pusulası listesi", "expense voucher list"),
        ("kimlik tespitleri listesi", "identity check list"),
        ("şüpheli bildirim listesi", "suspicious report list"),
        ("yüksek riskli cari listesi", "high-risk customer list"),
        ("risk profili görüntülendi", "risk profile viewed"),
        ("tam kimlik görüntülendi", "full identity viewed"),
        ("belge no ile arama", "search by document no."),
        ("nakit sınırı bildirim dilekçesi", "cash limit notification petition"),
        ("cari kartı görüntülendi", "customer card viewed"),
        ("satınalma listesi", "purchase list"),
        ("irsaliye listesi", "delivery note list"),
        ("denetim izi görüntülendi", "activity log viewed"),
        ("sürüm ayrıntısı", "version details"),
        ("yedek özeti", "backup summary"),
        ("açılış kuru provası", "opening rate dry run"),
    ];

    private static readonly Regex BelgeGoruntulendi =
        new(@"(?<![\p{L}])(\S+) belgesi görüntülendi", RegexOptions.CultureInvariant);

    private static readonly Regex StopajIndirildi =
        new(@"stopaj listesi indirildi \(mali müşavir\) · dönem (\S+) · (\d+) satır · tam TCKN", RegexOptions.CultureInvariant);

    /// <summary>
    /// Sunucunun sabit Türkçe denetim gerekçelerini EN GÖSTERİMDE çevirir (BASİT ve PROFESYONEL; ham kayıt
    /// title'da değişmez). TR dilinde ya da boş metinde metin AYNEN döner.
    /// </summary>
    public static string? DenetimCumleleriEn(string? s, string dil)
    {
        if (dil != "en" || string.IsNullOrEmpty(s)) return s;
        s = ListeBasi.Replace(s, m => $"list: {m.Groups[1].Value} records");
        s = StopajIndirildi.Replace(s, m => $"withholding list downloaded (accountant) · period {m.Groups[1].Value} · {m.Groups[2].Value} rows · full ID no.");
        foreach (var (tr, en) in DenetimCumleleri) s = s.Replace(tr, en, StringComparison.Ordinal);
        s = BelgeGoruntulendi.Replace(s, m => $"{m.Groups[1].Value} document viewed");
        return s;
    }

    /// <summary>
    /// Denetim kaydı detayını basit modda gösterilecek biçime çevirir.
    /// <paramref name="basit"/> false ise ya da metin boşsa metin DEĞİŞMEZ.
    /// </summary>
    public static string? DenetimDetayi(string? detay, string dil, bool basit)
    {
        if (!basit || string.IsNullOrEmpty(detay)) return detay;
        var s = detay;
        // PAKET DIL-EKRAN: sunucunun yazdığı SABİT Türkçe cümle başları/gerekçeleri yalnız EN
        // GÖSTERİMDE çevrilir (kayıt değişmez). Eşleşmeyen metin AYNEN kalır.
        s = DenetimCumleleriEn(s, dil)!;
        foreach (var (kalip, tr, en) in KodAdlari)
            s = kalip.Replace(s, dil == "en" ? en : tr);
        s = KebabKod.Replace(s, m => m.Value.Replace('-', ' '));
        foreach (var (kalip, tr, en) in DenetimSozcukleri)
            s = kalip.Replace(s, dil == "en" ? en : tr);
        return s;
    }
}
