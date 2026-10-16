namespace Kuyumcu.Core.I18n;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-5 — para ekranları görünüm kuyruğu (Cari/ekstre/Çıktı, Gider/Kasalar/
// Virman, İşlemler, Döviz Veresiye, gün sonu biçimi). YALNIZ GÖRÜNÜM: metinler
// bugünkü davranışı sade dille anlatır; hiçbir yasal kural, eşik, kapı ya da hesap
// DEĞİŞMEZ (MASAK kimlik kapısı aynen; yalnız BASİT başlık/etiket dili).
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör kuralı: yeni metin
//   paketin kendi dosyasına). Ekran bu dosyadaki karşılığı okur.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): konumsal record, Tr ve En aynı kurucu.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>TOPLU-5 metinleri (bir dilde).</summary>
public sealed record Toplu5Metinleri(
    // ── /cikti: değişmezlik notu TEK satır, gerekçe + düzeltme yolu ⓘ'de ──
    string CiktiDegismezKisa,
    string CiktiDegismezBilgi,
    // ── /cariler Döviz Veresiye: açıklamalar tek ⓘ; etiket sade; boş tutar alan altı mesajı ──
    string DovizBilgi,
    string DovizTlKarsiligi,
    string DovizTutarBos,
    // ── /gider: seçicide çekmecedeki nakit önde; nakit aşımında tek bilgi ──
    string GiderKasaNakitSecenek,
    string GiderKasaToplamSecenek,
    string GiderKartPayi,
    string GiderHavalePayi,
    string GiderNakitBant,
    // ── /gider?sekme=virman: BASİT dil + ipucu ──
    string VirmanBaslikBasit,
    string VirmanAciklamaBasit,
    string VirmanAracBilgi,
    string VirmanIkiBacakBasit,
    // ── /islemler: kilitteki alan adı = kimlik kutusundaki etiket ──
    string AlanHane,
    // ── gün sonu: tahsilat kırılımı (sıfır kalem yazılmaz) · durum cümle düzeninde ──
    string GunSonuTahsilat,
    string KirilimNakit,
    string KirilimKart,
    string KirilimHavale,
    string KirilimDiger,
    string GunSonuTutuyor,
    string GunSonuFarkVar,
    // ── /cikti tur 2 (hakem KRİTİK): fişte ham 26 haneli kayıt kimliği yerine kısa kayıt kodu ──
    string CiktiKayitKodu,
    string CiktiKayitKoduTam);

public static class Toplu5Sozlugu
{
    public static readonly Toplu5Metinleri Tr = new(
        CiktiDegismezKisa: "Belge kayıttan üretilir; burada düzeltilmez.",
        CiktiDegismezBilgi: "Nasıl düzeltilir?",
        DovizBilgi: "Döviz veresiye hakkında",
        DovizTlKarsiligi: "TL karşılığı",
        DovizTutarBos: "Tutarı girin.",
        GiderKasaNakitSecenek: "{0} · nakit {1}",
        GiderKasaToplamSecenek: "{0} · toplam {1}",
        GiderKartPayi: "kartla alınan {0}",
        GiderHavalePayi: "havaleyle gelen {0}",
        GiderNakitBant: "Kasadaki {0} çekmecede değil; gider yalnız nakitten ödenir.",
        VirmanBaslikBasit: "Para aktarımı (kasa ↔ banka)",
        VirmanAciklamaBasit: "Kasadan bankaya ya da bankadan kasaya para aktarır; satış ya da gider sayılmaz.",
        VirmanAracBilgi: "Neden soruluyor?",
        VirmanIkiBacakBasit: "Bir aktarım geri alınırsa iki tarafı birlikte geri döner.",
        AlanHane: "{0} (11 hane)",
        GunSonuTahsilat: "Tahsilat",
        KirilimNakit: "Nakit {0}",
        KirilimKart: "Kart {0}",
        KirilimHavale: "Havale {0}",
        KirilimDiger: "Diğer {0}",
        GunSonuTutuyor: "Tutuyor",
        GunSonuFarkVar: "Fark var",
        CiktiKayitKodu: "Kayıt kodu",
        CiktiKayitKoduTam: "Tam kayıt numarası: {0}");

    public static readonly Toplu5Metinleri En = new(
        CiktiDegismezKisa: "The document is produced from a record; it is not corrected here.",
        CiktiDegismezBilgi: "How to correct?",
        DovizBilgi: "About foreign-currency credit",
        DovizTlKarsiligi: "₺ equivalent",
        DovizTutarBos: "Enter the amount.",
        GiderKasaNakitSecenek: "{0} · cash {1}",
        GiderKasaToplamSecenek: "{0} · total {1}",
        GiderKartPayi: "{0} taken by card",
        GiderHavalePayi: "{0} received by transfer",
        GiderNakitBant: "The till's {0} is not in the drawer; expenses are paid from cash only.",
        VirmanBaslikBasit: "Money transfer (till ↔ bank)",
        VirmanAciklamaBasit: "Moves money from the till to the bank or back; it is not a sale or an expense.",
        VirmanAracBilgi: "Why is this asked?",
        VirmanIkiBacakBasit: "If a transfer is undone, both sides are reversed together.",
        AlanHane: "{0} (11 digits)",
        GunSonuTahsilat: "Collected",
        KirilimNakit: "Cash {0}",
        KirilimKart: "Card {0}",
        KirilimHavale: "Transfer {0}",
        KirilimDiger: "Other {0}",
        GunSonuTutuyor: "Matches",
        GunSonuFarkVar: "Difference",
        CiktiKayitKodu: "Record code",
        CiktiKayitKoduTam: "Full record number: {0}");

    public static Toplu5Metinleri Al(string dil) => dil == "en" ? En : Tr;
}
