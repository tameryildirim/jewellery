using System.Globalization;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Bilesenler.Form;

// ═════════════════════════════════════════════════════════════════════════════
// GİRDİ AİLESİ — ORTAK SÖZLEŞME (paket P-36b · kurucu talimatı 20/a)
//
// NEDEN: 21 Eylül ölçümünde ekranlarda 365 ham <input>/<textarea> vardı ve her
// biri kendi etiketini, zorunluluk işaretini, ipucunu, hata yerini ve dokunma
// hedefini kendi bildiği gibi çiziyordu. Kurucunun şartı tek cümleydi:
// "beğenmediğimiz bir yer olursa TEK SEFERDE değiştiririz." Bu ancak tek
// sözleşme + tek kabuk ile tutar.
//
// SÖZLEŞME, <Secici>'nin (P-36) kurduğunun BİREBİR AYNISIDIR — ikinci bir
// parametre dili açılmaz:
//   ElemanId · Etiket · Deger/DegerChanged · Zorunlu · Devredisi +
//   DevredisiSebebi · Hata · Ipucu · Yukleniyor
// Türetilen kimlikler: <ElemanId>-sar · -ipucu · -durum · -hata · -onizleme
//
// ⚠ BİÇİMLENDİRME TEK YERDEN: para/gram/sayı gösterimi YALNIZ Servisler/Bicim.cs
//   üzerinden yapılır. Bu dosyada ToString("N2"/"C2"/"F3") YOKTUR; yalnız
//   MAKİNE biçimi (Bicim.Makine) vardır, o da <input value> içindir.
//
// ⚠ SATIR İÇİ RENK/PX YOK: görsel değerlerin tamamı belirtecler.css'ten gelir
//   (denetim.sh: GIRDI_AILESI_TEK_KAYNAK).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Girdi ailesinin ortak parametre tabanı. Kendi başına hiçbir şey çizmez.</summary>
public abstract class FormAlani : ComponentBase
{
    /// <summary>Mevcut girdi id'si BİREBİR korunur; türetilenler &lt;ElemanId&gt;-&lt;alan&gt;.</summary>
    [Parameter, EditorRequired] public string ElemanId { get; set; } = "";

    /// <summary>Görünür etiket. Boş bırakılamaz — etiketsiz alan yasaktır (UX-S31).</summary>
    [Parameter, EditorRequired] public string Etiket { get; set; } = "";

    /// <summary>true ise yıldız + aria-required; false ise etiket yanında "(isteğe bağlı)".</summary>
    [Parameter] public bool Zorunlu { get; set; }

    [Parameter] public bool Devredisi { get; set; }

    /// <summary>
    /// <see cref="Devredisi"/> ise GÖRÜNÜR sebep (DoD EK md.1 · UX-S30).
    /// Kilitli alanın yanında hangi koşulun eksik olduğu yazılmak zorundadır.
    /// </summary>
    [Parameter] public string? DevredisiSebebi { get; set; }

    /// <summary>Alan altı Türkçe hata mesajı. Doluysa .hatali + aria-invalid.</summary>
    [Parameter] public string? Hata { get; set; }

    /// <summary>Alan altı yardım metni (biçim örneği, kaynak rozeti açıklaması).</summary>
    [Parameter] public string? Ipucu { get; set; }

    /// <summary>Değer sunucudan gelene dek alan kilitli ve "yükleniyor" der.</summary>
    [Parameter] public bool Yukleniyor { get; set; }

    /// <summary>Kapalıyken görünen yer tutucu.</summary>
    [Parameter] public string? YerTutucu { get; set; }

    /// <summary>
    /// UX-S21 — sistemin bildiği değerin NEREDEN geldiğini söyleyen rozet
    /// ("kasadan", "cariden", "oturumdan"). Boşsa rozet çizilmez.
    /// </summary>
    [Parameter] public string? KaynakRozeti { get; set; }

    // ── FORM DEFTERİ KÖPRÜSÜ (paket P-36c/3 — ham girdi göçü) ───────────────
    // NEDEN: göç edilen ekranlarda hata mesajı <AlanHatasi Alan="x" Durum="form"/>
    // ile ÇİZİLİYORDU ve o bileşen id="alan-hata-x" üretiyordu. Girdi ailesi
    // kendi kabuğunda hata çizdiği için o satır kaldırılırsa MEVCUT ELEMAN
    // KİMLİĞİ KAYBOLURDU (testler ve QA turları o kimliği kullanıyor).
    // Çözüm: alanın hata defterini ve alan anahtarını bileşene veriyoruz;
    // kabuk aynı kimliği (alan-hata-x) BİREBİR üretiyor. İkinci bir hata
    // çizim yolu AÇILMIYOR — AlanHatasi satırı göç edilen alanda silinir.

    /// <summary>Formun hata defteri (FormDurumu). Verilirse mesaj buradan okunur.</summary>
    [Parameter] public FormDurumu? Durum { get; set; }

    /// <summary>
    /// <see cref="Durum"/> içindeki alan anahtarı. Verilirse hata kimliği
    /// <c>alan-hata-{HataAlani}</c> olur (AlanHatasi ile BİREBİR aynı).
    /// </summary>
    [Parameter] public string? HataAlani { get; set; }

    /// <summary>Defterden gelen mesaj (yalnız <see cref="HataAlani"/> verildiyse).</summary>
    protected string? DefterHatasi =>
        HataAlani is { Length: > 0 } a ? Durum?.Mesaj(a) : null;

    /// <summary>Gösterilecek hata: önce açıkça verilen, sonra defterden gelen.</summary>
    protected string? GorunenHata => Hata is { Length: > 0 } ? Hata : DefterHatasi;

    /// <summary>
    /// Göç edilen ekranda hata &lt;span&gt;'i BAŞKA bir kimlik taşıyorsa
    /// (ör. #sifre-hata) o kimlik burada verilir ve BİREBİR korunur.
    /// </summary>
    [Parameter] public string? HataKimligi { get; set; }

    /// <summary>Hata &lt;span&gt;'inin kimliği — göçte eski kimlik korunur.</summary>
    protected string HataElemanId =>
        HataKimligi is { Length: > 0 } k ? k
        : HataAlani is { Length: > 0 } a ? $"alan-hata-{a}"
        : $"{ElemanId}-hata";

    /// <summary>
    /// PAKET Q10-5 — ORTAK: kullanıcı alanın değerini değiştirdiğinde o alanın
    /// defterdeki (FormDurumu) eski mesajı TEK YERDEN silinir. Her girdi
    /// bileşeni değişim işleyicisinin başında bunu çağırır; sayfalar kendi
    /// "hatayı temizle" kodunu yazmaz (ikinci kalıp açılmaz).
    /// Açıkça verilen <see cref="Hata"/> parametresine dokunulmaz — o değer
    /// sayfanın hesapladığı canlı sonuçtur ve sayfa yeniden çizince tazelenir.
    /// </summary>
    protected void DefterdenSil()
    {
        if (HataAlani is { Length: > 0 } a) Durum?.Sil(a);
    }

    protected bool Hatali => GorunenHata is { Length: > 0 };

    protected bool Kilitli => Devredisi || Yukleniyor;

    /// <summary>aria-describedby zinciri: ipucu + kilit sebebi + hata.</summary>
    protected string? AciklayanId
    {
        get
        {
            var p = new List<string>(3);
            if (Ipucu is { Length: > 0 }) p.Add($"{ElemanId}-ipucu");
            if (Kilitli && DevredisiSebebi is { Length: > 0 }) p.Add($"{ElemanId}-durum");
            if (Hatali) p.Add(HataElemanId);
            return p.Count == 0 ? null : string.Join(' ', p);
        }
    }

    // ── SAYI ÇÖZÜCÜ — tek yer ────────────────────────────────────────────────
    // Kullanıcı tezgâhta "12,5" de yazar, barkod okuyucudan "12.5" de gelir.
    // İki ayrı ekranda iki ayrı ayrıştırma YAZILMAZ (UX-S31 "tek kaynak").
    // Boş/bozuk metin null döner — SESSİZCE 0 OLMAZ (Anayasa md.1: bilinmeyen
    // tahmin edilmez; çağıran "değer yok" ile "değer sıfır"ı ayırt edebilmeli).
    //
    // ── PAKET P-78 — tr-TR KURALLARI (demo düzeltmesi, 24.09 ölçümü) ───────
    // Eski çözücü noktayı HER ZAMAN ondalık sayıyordu: Türkçe "45.000" = 45,
    // ekranların kendi Invariant ayrıştırmaları da "1,5" = 15 okuyordu. Kurallar:
    //   • boşluk / NBSP / dar NBSP temizlenir; baştaki "-" korunur (negatif
    //     kuralı ÇAĞIRANIN işidir).
    //   • binlik kalıbı ^\d{1,3}(\.\d{3})+(,\d+)?$ → nokta binlik, virgül ondalık
    //     ("1.250.000,50" = 1250000,5). TEK grup ("1.500") yalnız grup "000"
    //     ise binliktir ("45.000" = 45000): eski makine biçimi (0.## / 0.###)
    //     sondaki sıfırları hiç yazmadığı için ".000" yalnız insan elinden,
    //     Türkçe binlik olarak çıkar. Aksi hâlde BELİRSİZ (aşağıda).
    //   • yalnız virgül → ondalık ("1,5" = 1,5).
    //   • ayırıcı yok → tam sayı.
    //   • TEK nokta ve noktadan sonra 3 hane DEĞİL ("22.5", "5.25", "45000.00")
    //     → ondalık nokta KABUL (eski alışkanlık / barkod okuyucu, geri uyum).
    //     Tam sayı kısmı "0" ise ("0.500") binlik olamaz → yine ondalık.
    //   • TEK nokta + tam 3 hane ("1.500") → BELİRSİZ → null (fail-closed;
    //     sessiz tahmin YASAK). Çağıran SayiCoz ile sebebi okur ve alan altına
    //     SayiGirisiSozlugu.Belirsiz yazar.
    //   • virgül + nokta ama binlik kalıbına uymuyor ("1,500.25") → geçersiz.

    /// <summary>P-78 — ayrıştırma sonucunun türü.</summary>
    public enum SayiSonucu { Tamam, Bos, Gecersiz, Belirsiz }

    /// <summary>P-78 — ayrıştırma sonucu: değer + (başarısızsa) sebep.</summary>
    public readonly record struct SayiCozumu(SayiSonucu Sonuc, decimal? Deger)
    {
        public bool Tamam => Sonuc == SayiSonucu.Tamam;

        /// <summary>
        /// Alan altına yazılacak Türkçe/İngilizce mesaj; Tamam/Bos için null
        /// (boşluk kuralı ÇAĞIRANIN kararıdır: bazı alanlar boş olabilir).
        /// </summary>
        public string? Mesaj(string dil, bool tamSayi = false) => Sonuc switch
        {
            SayiSonucu.Belirsiz => tamSayi
                ? Kuyumcu.Core.I18n.SayiGirisiSozlugu.Al(dil).BelirsizTam   // TOPLU-7A: tam sayı alanında "1,5" seçeneği sunulmaz
                : Kuyumcu.Core.I18n.SayiGirisiSozlugu.Al(dil).Belirsiz,
            SayiSonucu.Gecersiz => Kuyumcu.Core.I18n.SayiGirisiSozlugu.Al(dil).Gecersiz,
            _ => null,
        };
    }

    private static readonly System.Text.RegularExpressions.Regex BinlikKalibi =
        new(@"^\d{1,3}(\.\d{3})+(,\d+)?$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex VirgulluKalip =
        new(@"^(\d+(,\d*)?|,\d+)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex NoktaliKalip =
        new(@"^(\d*)\.(\d+)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex TamKalip =
        new(@"^\d+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>P-78 — kullanıcı metnini tr-TR kurallarıyla sayıya çevirir (TEK MERKEZ).</summary>
    public static SayiCozumu SayiCoz(string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham)) return new(SayiSonucu.Bos, null);
        var t = ham.Replace(" ", "").Replace(" ", "").Replace(" ", "")
                   .Replace("\t", "").Trim();
        if (t.Length == 0) return new(SayiSonucu.Bos, null);

        var eksi = t[0] == '-';
        var govde = eksi ? t[1..] : t;
        if (govde.Length == 0) return new(SayiSonucu.Gecersiz, null);

        string? makine = null;          // InvariantCulture ile çözülecek normal biçim
        if (BinlikKalibi.IsMatch(govde))
        {
            var noktaSayisi = govde.Count(c => c == '.');
            var virgulVar = govde.Contains(',');
            var tekGrupSifir = govde.EndsWith(".000", StringComparison.Ordinal);
            if (noktaSayisi == 1 && !virgulVar && !tekGrupSifir)
            {
                // "1.500": 1,5 mi 1500 mü? Tam kısım "0" ise ("0.500") binlik olamaz.
                if (govde.StartsWith("0.", StringComparison.Ordinal)) makine = govde;
                else return new(SayiSonucu.Belirsiz, null);
            }
            else
            {
                makine = govde.Replace(".", "").Replace(',', '.');
            }
        }
        else if (TamKalip.IsMatch(govde))
        {
            makine = govde;
        }
        else if (VirgulluKalip.IsMatch(govde))
        {
            makine = govde.Replace(',', '.');
            if (makine.EndsWith('.')) makine = makine[..^1];
        }
        else if (NoktaliKalip.Match(govde) is { Success: true } m)
        {
            // TEK nokta, 3 hane DEĞİL ya da tam kısım 3 haneden uzun → ondalık nokta.
            // (Tam 3 hane + ≤3 haneli tam kısım binlik kalıbına düşer, yukarıda.)
            if (m.Groups[1].Value.Length == 0) makine = "0" + govde;
            else makine = govde;
        }
        else
        {
            return new(SayiSonucu.Gecersiz, null);
        }

        if (!decimal.TryParse(makine, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d))
            return new(SayiSonucu.Gecersiz, null);
        return new(SayiSonucu.Tamam, eksi ? -d : d);
    }

    /// <summary>Ham metni ondalığa çevirir (P-78: tr-TR kuralları, <see cref="SayiCoz"/>).</summary>
    public static decimal? Ondalik(string? ham) => SayiCoz(ham).Deger;

    /// <summary>Ham metni tam sayıya çevirir.</summary>
    public static int? TamSayi(string? ham) =>
        int.TryParse((ham ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var d)
            ? d : null;

    /// <summary>
    /// Ham metni tarihe çevirir (ISO 8601 — input type=date bunu üretir).
    /// ⚠ Adı <c>TarihCoz</c>dur, <c>Tarih</c> DEĞİL: aynı ad alanında
    /// <see cref="Tarih"/> adlı bir BİLEŞEN var; ikisi çakışırsa Razor
    /// derleyicisi sessizce bileşeni seçer.
    /// </summary>
    public static DateOnly? TarihCoz(string? ham) =>
        DateOnly.TryParseExact((ham ?? "").Trim(), "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var g) ? g : null;
}
