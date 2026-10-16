using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Bilesenler.Form;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — ANLAMSAL METİN GİRDİSİNİN ORTAK BEYNİ
// (Iban · Telefon · Eposta · Pasaport · BarkodAlani · AdSoyad · Unvan · Adres).
//
// Kurucu 08.10: "Sık kullanılan input için TEK kendi bileşenimiz; TC kontrolü bir
// yerde var diğerinde yok olmasın." Her anlamsal bileşen dört şeyi İÇİNDE taşır:
//   (1) yazarken davranış — harf engeli, yapıştırma temizliği, maske.
//       Tarayıcıda wwwroot/js/girdi-maske.js tuşu/yapıştırmayı keser; AYNI maske
//       burada Temizle() ile de uygulanır (JS'siz ortam, bUnit, otomatik doldurma).
//       İkisi aynı çıktıyı verir (idempotent) → Blazor kutuyu yeniden yazmaz,
//       imleç atlamaz.
//   (2) uzunluk dolunca / alandan çıkınca kural — Core'daki TEK sınıf.
//   (3) alan altı anında hata (sözlük: Kuyumcu.Core/I18n/Sozlukler.Girdi.cs).
//   (4) GecersizAlanKapisi.Bildir(… mesaj) — Kaydet kilidinin sebebi alan altı
//       metniyle BİREBİR aynı.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Kurallı metin girdilerinin ortak tabanı.</summary>
public abstract class KuralliMetinAlani : FormAlani, IDisposable
{
    [Parameter] public string? Deger { get; set; }
    [Parameter] public EventCallback<string?> DegerChanged { get; set; }

    /// <summary>Formun ortak geçersiz alan kapısı (varsa Kaydet kilidi buradan).</summary>
    [CascadingParameter] public GecersizAlanKapisi? GecersizKapi { get; set; }

    [Inject] protected DilServisi Dil { get; set; } = default!;

    /// <summary>Kutuda görünen (maskeli) metin.</summary>
    protected string ham = "";

    /// <summary>Kuralın ürettiği alan altı metni (null = geçerli).</summary>
    protected string? icHata;

    /// <summary>Kullanıcı alandan en az bir kez çıktı mı? (sonra hata canlı izlenir)</summary>
    protected bool dokunuldu;

    private string? sonGelen;
    private bool ilk = true;

    /// <summary>Yazarken maske (JS ile aynı, idempotent): harf engeli + biçim.</summary>
    protected abstract string Temizle(string? girdi);

    /// <summary>Tam kural (alandan çıkınca ve dokunulduktan sonra her tuşta).</summary>
    protected abstract GirdiHatasi? TamDenetle(string temiz);

    /// <summary>Yazarken kural — yalnız uzunluk dolunca anlamlıysa döner. Varsayılan: yok.</summary>
    protected virtual GirdiHatasi? YazarkenDenetle(string temiz) => null;

    /// <summary>Çağırana giden değer (ör. IBAN boşluksuz).</summary>
    protected virtual string Kayit(string temiz) => temiz;

    /// <summary>Alandan çıkınca son biçim (ör. e-posta küçük harf).</summary>
    protected virtual string CikisBicimi(string temiz) => temiz;

    /// <summary>Dışarıdan gelen değerin kutudaki gösterimi.</summary>
    protected virtual string Goster(string? deger) => Temizle(deger);

    /// <summary>Alan altında görünen: önce kural (kilit sebebiyle aynı), sonra sunucu/sayfa hatası.</summary>
    protected string? EtkinHata => icHata is { Length: > 0 } ? icHata : GorunenHata;
    protected bool EtkinHatali => EtkinHata is { Length: > 0 };

    /// <summary>aria-describedby zinciri.</summary>
    protected string? EtkinAciklayan
    {
        get
        {
            var p = new List<string>(3);
            if (IpucuMetni is { Length: > 0 }) p.Add($"{ElemanId}-ipucu");
            if (Kilitli && DevredisiSebebi is { Length: > 0 }) p.Add($"{ElemanId}-durum");
            if (EtkinHatali) p.Add(HataElemanId);
            return p.Count == 0 ? null : string.Join(' ', p);
        }
    }

    /// <summary>Bileşenin varsayılan ipucu (çağıran Ipucu verirse o).</summary>
    protected virtual string? IpucuMetni => Ipucu;

    private string? Mesaj(GirdiHatasi? h) => h is { } x ? GirdiSozlugu.Mesaj(x, Dil.Dil) : null;

    private void KapiyaYaz() => GecersizKapi?.Bildir(ElemanId, Etiket, icHata is not null, icHata);

    public void Dispose() => GecersizKapi?.Bildir(ElemanId, Etiket, false, null);

    protected override void OnParametersSet()
    {
        if (!ilk && Deger == sonGelen) return;
        ilk = false;
        sonGelen = Deger;
        if (Kayit(ham) == (Deger ?? "")) return;   // kullanıcının yazdığı zaten bu → kutuya dokunma
        ham = Goster(Deger);
        // Kayıtlı değer de aynı kuraldan geçer: bozuk eski veri sessizce kaydedilmez.
        icHata = ham.Length == 0 ? null : Mesaj(TamDenetle(ham));
        KapiyaYaz();
    }

    /// <summary>@oninput — yazarken.</summary>
    protected async Task Yazildi(ChangeEventArgs e)
    {
        DefterdenSil();
        ham = Temizle(e.Value?.ToString());
        icHata = ham.Length == 0 ? null : Mesaj(dokunuldu ? TamDenetle(ham) : YazarkenDenetle(ham));
        KapiyaYaz();
        await Bildir();
    }

    /// <summary>@onchange — alandan çıkınca (ya da Enter).</summary>
    protected async Task Bitti(ChangeEventArgs e)
    {
        DefterdenSil();
        dokunuldu = true;
        ham = CikisBicimi(Temizle(e.Value?.ToString()));
        icHata = ham.Length == 0 ? null : Mesaj(TamDenetle(ham));
        KapiyaYaz();
        await Bildir();
    }

    private Task Bildir()
    {
        var yeni = ham.Length == 0 ? null : Kayit(ham);
        sonGelen = yeni;
        Deger = yeni;
        return DegerChanged.InvokeAsync(yeni);
    }

    // ── maske yardımcıları (girdi-maske.js ile aynı algoritma) ─────────────

    /// <summary>Rakamları verilen grup boylarıyla boşlukla ayırır (eksik son grup olduğu gibi).</summary>
    protected static string Grupla(string s, params int[] gruplar)
    {
        var b = new System.Text.StringBuilder(s.Length + gruplar.Length);
        var i = 0;
        foreach (var g in gruplar)
        {
            if (i >= s.Length) break;
            if (b.Length > 0) b.Append(' ');
            var n = Math.Min(g, s.Length - i);
            b.Append(s, i, n);
            i += n;
        }
        if (i < s.Length) { if (b.Length > 0) b.Append(' '); b.Append(s, i, s.Length - i); }
        return b.ToString();
    }
}
