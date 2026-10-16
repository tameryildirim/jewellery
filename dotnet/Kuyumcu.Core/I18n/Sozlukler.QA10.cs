namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET QA10-KRITIK — /gider geri alma (storno) SONRASI BANT METNİ, BASİT MOD
//
// Tasarım hakemi (30.09): BASİT modda geri alma sonrası yeşil bant "Gider
// stornolandı" diyordu; aynı tablodaki rozet ise "Geri alındı" (KAT-1 D10).
// Tezgâhtar KRİTİK-1 senaryosunun tam ortasında "storno" jargonunu görüyordu.
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs HUB ve Sozlukler.Katalog.cs (BasitDilSozlugu)
//   P8 şeridinin alanıdır, bu pakette DOKUNULMAZ. Basit mod karşılığı burada
//   tutulur ve Gider.razor Dil.BasitMi ile seçer. PROFESYONEL modda hub metni
//   ("Gider stornolandı") olduğu gibi kalır (muhasebe dili).
// ⚠ Kavram tek kaynaktan (UX-S31): "Geri alındı" sözcüğü KatalogSozlugu.GeriAlindi
//   ile aynıdır; buradaki metin yalnız cümlenin öznesini ("Gider", "Virman") ekler.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>QA10-KRITIK ile eklenen metinler (bir dilde). Konumsal: eksik dil = derleme hatası.</summary>
public sealed record QA10Metinleri(
    // BASİT mod: gider geri alma sonrası başarı bandı
    string GiderGeriAlindiBasit,
    // BASİT mod: virman geri alma sonrası başarı bandı (bacak sayısı yazılmaz)
    string VirmanGeriAlindiBasit,
    // PROFESYONEL mod: virman stornosu bandının bacak sayısı eki ({0} = adet); eskiden çıplak "bacak" metniydi
    string VirmanBacakSayisi);

/// <summary>/gider geri alma bandının mod ve dile göre metni.</summary>
public static class QA10Sozlugu
{
    public static readonly QA10Metinleri Tr = new(
        GiderGeriAlindiBasit: "Gider geri alındı",
        VirmanGeriAlindiBasit: "Virman geri alındı (iki taraf birlikte)",
        VirmanBacakSayisi: "{0} bacak");

    public static readonly QA10Metinleri En = new(
        GiderGeriAlindiBasit: "Expense undone",
        VirmanGeriAlindiBasit: "Transfer undone (both sides together)",
        VirmanBacakSayisi: "{0} legs");

    public static QA10Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
