using System.Text;
using System.Text.Json;

namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ISTEK-1 — LİSTE ÖNBELLEĞİ (oturum boyunca az değişen listeler)
//
// KAYNAK (orkestratör ölçümü 25.09 10:40, TEK kullanıcı): 10 ekran 30 sn'de
// gezilince 67 /api/v1 isteği atılıyordu; /subeler ×7, /kur/guncel ×4,
// /reyonlar ×4, /gorunum ×2, /kasalar ×2, /urun-kategorileri ×2. Aynı liste
// her ekran açılışında yeniden çekiliyor, hızlı gezinen tek kullanıcı genel
// sınıra (o gün 100/dk) çarpıyor ve ekranlar kırmızı "çok yoğun" bandıyla
// boşalıyordu (GOZ-INCELEME #0 #34 #44 #45 #46 #53 #57 #66–#69).
//
// NE ÖNBELLEĞE GİRER (beyaz liste — başka hiçbir yol GİRMEZ):
//   /subeler · /reyonlar · /urun-kategorileri (+ /duz) · /kur/kaynaklar ·
//   /gorunum             → 60 sn tazelik
//   /kasalar             → 10 sn tazelik (BAKİYE taşır; başka tezgâhın satışı
//                          bu istemcinin yazması olmadığı için önbelleği
//                          düşürmez — doğrulayıcı bulgusu, kısa tazelik)
//   /kur/guncel          → YALNIZ canlı kur kanalı (SignalR) bağlıyken; her
//                          push'ta ve kanal kopunca/bağlanınca DÜŞER. Yani push
//                          bağlıyken sayfalar ek çekim yapmaz, kopukken her
//                          okuma eskisi gibi sunucuya gider.
//   Kök yolun TAM kendisi ya da sorgu dizeli hâli önbelleğe girer; alt kayıt
//   yolları (/kasalar/{id}/hareketler gibi) GİRMEZ.
//
// GEÇERSİZ KILMA — TUTUCU TARAF: bu istemciden atılan HER yazma isteği
// (POST/PATCH/PUT/DELETE, başarılı ya da değil) önbelleğin TAMAMINI düşürür.
// Neden hedefli değil: /kasalar yanıtı BAKİYE taşır ve bakiye satış, tahsilat,
// gider, virman... onlarca farklı uçtan değişir; reyon/kategori listeleri de
// ürün sayısı taşıyabilir. "Hangi yazma hangi listeyi etkiler" haritası
// eksik kalırsa ekran bayat bakiye gösterirdi — bir önbellek kazancı buna
// değmez. Yazmalar okumalardan çok seyrektir; tam düşürme ucuzdur.
// Uçuştaki bir çekim yazmadan ÖNCE başladıysa yanıtı önbelleğe YAZILMAZ
// (nesil sayacı) — yazma öncesi hâl yazma sonrasına sızmaz.
//
// KİMLİK: kayıtlar oturum kimliğine (jetondaki kullanıcı + kiracı + şube)
// bağlıdır. Çıkış, başka kullanıcıyla giriş ya da şube değişimi (yeni jeton,
// yeni şube) önbelleği boşaltır — kiracılar arası sızıntı olamaz.
//
// SON İYİ YANIT: 429'da ekran boşalmasın diye her başarılı GET gövdesi (yol
// başına, en fazla SonIyiTavani adet, ALINDIĞI ZAMANLA) saklanır; sunucu
// ısrarla 429 derse ApiIstemci bunu YALNIZ yaşı "Retry-After + SonIyiEkPayi"
// (≈70 sn) içindeyse döner ve yoğunluk bandını açar (UX-S28: hata gizlenmez,
// ama liste de silinmez). Daha eski gövde ASLA dönmez — 20 dakikalık bakiye
// "şimdiki" gibi görünemez; o durumda sayfa hatayı kendi bandında gösterir.
// Canlı kur (/kur/guncel) hiçbir zaman son-iyi yedeğinden verilmez: kur
// fotoğrafına bağlanan yazmalar bayat kurla yapılamaz, 429 kurda HATA kalır.
// "Yenile" (TazeGetAsync) da yedeğe düşmez. Yazmalar yedeği de düşürür.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ListeOnbellegi
{
    /// <summary>Ortak tazelik (kurucu kararı: 60 sn).</summary>
    public static readonly TimeSpan Tazelik = TimeSpan.FromSeconds(60);

    /// <summary>
    /// /kasalar tazeliği — liste BAKİYE taşır ve başka terminalin satışı bu
    /// istemcinin önbelleğini düşürmez; bu yüzden ortak 60 sn yerine 10 sn.
    /// </summary>
    public static readonly TimeSpan KasaTazelik = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Son-iyi yedeğinin yaş payı: 429 yedeği yalnız yaşı
    /// Retry-After + bu pay içindeki gövdeler için kullanılır.
    /// </summary>
    public static readonly TimeSpan SonIyiEkPayi = TimeSpan.FromSeconds(60);

    /// <summary>Önbelleğe giren kök yollar ve tazelikleri (tek kaynak).</summary>
    public static readonly IReadOnlyDictionary<string, TimeSpan> Kaynaklar = new Dictionary<string, TimeSpan>
    {
        ["/subeler"] = Tazelik,
        ["/reyonlar"] = Tazelik,
        ["/urun-kategorileri"] = Tazelik,
        ["/urun-kategorileri/duz"] = Tazelik,
        ["/kur/kaynaklar"] = Tazelik,
        ["/gorunum"] = Tazelik,
        ["/kasalar"] = KasaTazelik,
        ["/kur/guncel"] = Tazelik,   // yalnız push bağlıyken (TryAl)
    };

    /// <summary>
    /// CanliKurServisi'nin push kanalı (SignalR) bağlı mı? Bağlıyken /kur/guncel
    /// kaydı en fazla 60 sn tutulur ve her push'ta düşer; kopukken hiç verilmez.
    /// </summary>
    public bool KurCanliBagli { get; set; }

    /// <summary>Canlı kur yolu — SignalR ile yeni kur gelince düşürülür.</summary>
    public const string KurYolu = "/kur/guncel";

    /// <summary>429 yedeği için saklanan azami yol sayısı (bellek sınırı).</summary>
    public const int SonIyiTavani = 200;

    /// <summary>Test kancası — gerçek saat sınanabilir olsun diye.</summary>
    public Func<DateTimeOffset> Simdi { get; set; } = () => DateTimeOffset.UtcNow;

    /// <summary>
    /// Mutasyon/teşhis anahtarı: false iken önbellek HİÇ cevap vermez (her okuma
    /// ağa gider). Üretimde hep true; testler "önbellek kapalıyken istek sayısı
    /// artar" iddiasını bununla da sınar.
    /// </summary>
    public bool Etkin { get; set; } = true;

    private readonly object kilit = new();
    private readonly Dictionary<string, (DateTimeOffset Zaman, string Govde)> kayitlar = [];
    private readonly Dictionary<string, (DateTimeOffset Zaman, string Govde)> sonIyi = [];
    private readonly LinkedList<string> sonIyiSira = new();
    private string? kimlik;
    private long nesil;

    /// <summary>Kaç okumanın önbellekten karşılandığı (test/teşhis kancası).</summary>
    public int Isabet { get; private set; }

    /// <summary>Yolun önbellek tazeliği; beyaz listede değilse null.</summary>
    public static TimeSpan? TazelikBul(string yol)
    {
        var soru = yol.IndexOf('?');
        var kok = soru >= 0 ? yol[..soru] : yol;
        return Kaynaklar.TryGetValue(kok, out var t) ? t : null;
    }

    /// <summary>
    /// Şu anki nesil — çekim başlarken alınır, yazarken karşılaştırılır. Önce
    /// oturum kimliği işlenir ki ilk çekimin kaydı kimlik atamasıyla düşmesin.
    /// </summary>
    public long NesilAl(string? oturumKimligi)
    {
        lock (kilit)
        {
            KimlikDenetle(oturumKimligi);
            return nesil;
        }
    }

    /// <summary>Taze kayıt varsa gövdeyi döner.</summary>
    public bool TryAl(string yol, string? oturumKimligi, out string govde)
    {
        govde = "";
        if (!Etkin || TazelikBul(yol) is not { } tazelik) return false;
        // Canlı kur YALNIZ push kanalı bağlıyken önbellekten verilir: o sürede
        // yeni kur zaten İTİLİR ve kayıt o an düşer (Dusur) — önbellekteki kur
        // her an "son itilen kur"dur. Kanal kopuksa her okuma sunucuya gider
        // (kur fotoğrafına bağlanan yazmalar bayat kurla yapılamaz).
        if (yol.StartsWith(KurYolu, StringComparison.Ordinal) && !KurCanliBagli) return false;
        lock (kilit)
        {
            KimlikDenetle(oturumKimligi);
            if (!kayitlar.TryGetValue(yol, out var k)) return false;
            if (Simdi() - k.Zaman >= tazelik) { kayitlar.Remove(yol); return false; }
            govde = k.Govde;
            Isabet++;
            return true;
        }
    }

    /// <summary>
    /// Başarılı GET yanıtını yazar. <paramref name="baslangicNesli"/> çekim
    /// başlarken alınan nesildir; arada yazma olduysa yanıt ÖNBELLEĞE YAZILMAZ.
    /// </summary>
    public void Koy(string yol, string? oturumKimligi, string govde, long baslangicNesli)
    {
        lock (kilit)
        {
            KimlikDenetle(oturumKimligi);
            if (baslangicNesli != nesil) return;           // yazma araya girdi
            if (TazelikBul(yol) is not null) kayitlar[yol] = (Simdi(), govde);
            // 429 yedeği (her GET için — beyaz listeden bağımsız)
            if (sonIyi.ContainsKey(yol)) sonIyiSira.Remove(yol);
            sonIyi[yol] = (Simdi(), govde);
            sonIyiSira.AddLast(yol);
            while (sonIyiSira.Count > SonIyiTavani)
            {
                sonIyi.Remove(sonIyiSira.First!.Value);
                sonIyiSira.RemoveFirst();
            }
        }
    }

    /// <summary>
    /// 429 yedeği: bu oturumda bu yola en son başarıyla gelen gövde — YALNIZ
    /// yaşı <paramref name="azamiYas"/>'tan küçükse. Canlı kur yolu hiçbir zaman
    /// yedekten verilmez (bayat kur "taze" işaretlenemez).
    /// </summary>
    public bool SonIyiAl(string yol, string? oturumKimligi, TimeSpan azamiYas, out string govde)
    {
        govde = "";
        if (yol.StartsWith(KurYolu, StringComparison.Ordinal)) return false;
        lock (kilit)
        {
            KimlikDenetle(oturumKimligi);
            if (!sonIyi.TryGetValue(yol, out var k)) return false;
            if (Simdi() - k.Zaman > azamiYas) return false;
            govde = k.Govde;
            return true;
        }
    }

    /// <summary>Bu istemciden bir yazma gitti: TÜM kayıtlar düşer (tutucu taraf — yukarıdaki not).</summary>
    public void YazmaOldu()
    {
        lock (kilit)
        {
            nesil++;
            kayitlar.Clear();
            sonIyi.Clear();
            sonIyiSira.Clear();
        }
    }

    /// <summary>
    /// Yolu, kaydı <paramref name="yasSiniri"/>'ndan eskiyse düşürür (push kanalı
    /// yeni bağlandı: az önce çekilmiş kur, yoklama aralığı kadar tazedir).
    /// </summary>
    public void EskiyseDusur(string yol, TimeSpan yasSiniri)
    {
        lock (kilit)
        {
            if (kayitlar.TryGetValue(yol, out var k) && Simdi() - k.Zaman >= yasSiniri)
                kayitlar.Remove(yol);
        }
    }

    /// <summary>Tek yolu düşürür (canlı kurdan yeni kur geldi).</summary>
    public void Dusur(string yol)
    {
        lock (kilit)
        {
            kayitlar.Remove(yol);
        }
    }

    private void KimlikDenetle(string? oturumKimligi)
    {
        if (kimlik == oturumKimligi) return;
        kimlik = oturumKimligi;
        nesil++;
        kayitlar.Clear();
        sonIyi.Clear();
        sonIyiSira.Clear();
    }

    /// <summary>
    /// Oturum kimliği: erişim jetonunun gövdesindeki kullanıcı + kiracı + şube.
    /// Jeton yenilense de (15 dk) kimlik AYNI kalır → önbellek boşuna düşmez;
    /// şube değişince yeni jetonda şube farklıdır → önbellek düşer.
    /// Jeton çözülemezse jetonun kendisi kimliktir (tutucu taraf).
    /// </summary>
    public static string? KimlikCoz(string? jeton)
    {
        if (string.IsNullOrEmpty(jeton)) return null;
        var parca = jeton.Split('.');
        if (parca.Length < 2) return jeton;
        try
        {
            var b = parca[1].Replace('-', '+').Replace('_', '/');
            b = b.PadRight(b.Length + (4 - b.Length % 4) % 4, '=');
            using var belge = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(b)));
            var kok = belge.RootElement;
            string Oku(params string[] adlar)
            {
                foreach (var ad in adlar)
                    if (kok.TryGetProperty(ad, out var d) && d.ValueKind == JsonValueKind.String)
                        return d.GetString() ?? "";
                return "";
            }
            var sonuc = $"{Oku("sub", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")}|{Oku("tenantId", "tenant_id", "kiraciId")}|{Oku("subeId", "sube_id")}|{Oku("rol")}";
            return sonuc == "|||" ? jeton : sonuc;
        }
        catch (Exception e) when (e is FormatException or JsonException or ArgumentException)
        {
            return jeton;
        }
    }
}
