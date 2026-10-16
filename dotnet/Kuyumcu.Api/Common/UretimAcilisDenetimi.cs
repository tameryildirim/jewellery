using System.Security.Cryptography;
using System.Text;

namespace Kuyumcu.Api.Common;

// ═════════════════════════════════════════════════════════════════════════════
// ÜRETİM AÇILIŞ DENETÇİSİ — Paket T-1(+F-6), Faz A.
//
// NEDEN VAR: bugüne kadar `appsettings.json` içindeki ÖRNEK sırlarla
// (`Jwt:Secret = DEGISTIR-...`, `password=DEGISTIR`) uygulama sorunsuz
// açılıyordu. Program.cs yalnız "null mı" diye bakıyordu; değerin ÖRNEK
// olması bir hata sayılmıyordu. Sonuç: her müşteride AYNI JWT sırrı →
// bir kurulumdan alınan token diğerinde geçerli olurdu.
//
// KALIP: Common/AlanSifreleme.Yukle'nin BİREBİR İKİZİ.
//   • TEMBEL DEĞİL — `builder.Build()` çağrılmadan ÖNCE çalışır.
//     (Canlıda yakalanan hata: `AddSingleton(_ => Yukle(...))` tembeldi ve
//      anahtarsız uygulama /saglik'a 200 döndü. Aynı hataya düşülmez.)
//   • İhlalde `InvalidOperationException` → süreç sıfırdan FARKLI kodla biter.
//   • Mesaj ASLA sır DEĞERİ içermez; yalnız hangi kuralın tetiklendiğini söyler.
//
// ORTAM FARKI (Bertan + Mehmet koşulu):
//   Kurallar YALNIZ Production'da uygulanır — aksi hâlde tüm HTTP testleri
//   (WebApplicationFactory, ortam "Testing") ve geliştirme koşusu topluca
//   kırılırdı. "Development'ta test edilen davranış Production'da farklı
//   olmasın" şartı GÖRÜNMEZ bırakılmadı: `Uretim:DenetimiZorla=true`
//   yapılandırmasıyla AYNI kurallar her ortamda zorlanabilir; testler de
//   üretim davranışını tam olarak bu anahtarla sınar.
//
// KARA LİSTE DEĞERLERİ NEDEN AÇIK YAZILMIYOR:
//   `DEGISTIR` şablon değerleri zaten herkese açık yer tutuculardır, açık
//   yazılır. Geliştirme ortamının GERÇEK sırları ise kaynak koda İKİNCİ kez
//   kopyalanmaz — SHA-256 parmak izleriyle karşılaştırılır. Parmak izinden
//   değer geri üretilemez; kural yine TAM DİZGE eşleşmesidir (alt dizge
//   eşleşmesi yok → meşru bir sır tesadüfen kilitlenmez).
// ═════════════════════════════════════════════════════════════════════════════

public static class UretimAcilisDenetimi
{
    /// <summary>Şablon sırların ortak ön eki (appsettings.json yer tutucuları).</summary>
    public const string DegistirOneki = "DEGISTIR";

    /// <summary>Jwt:Secret için asgari uzunluk (UTF-8 bayt).</summary>
    public const int AsgariSirBayt = 32;

    public const string ZorlaAnahtari = "Uretim:DenetimiZorla";
    public const string TersVekilAnahtari = "Uretim:TersVekilArkasinda";
    public const string VeritabaniAdiOrtamDegiskeni = "KUYUMCU_VT";
    public const string YedekAnahtari = "Yedek:SifrelemeAnahtari";

    /// <summary>
    /// Kara listedeki GERÇEK (yer tutucu olmayan) örnek sırların SHA-256 parmak izleri.
    /// Sıra: [0] appsettings.Development.json Jwt:Secret, [1] geliştirme MySQL parolası.
    /// </summary>
    private static readonly string[] YasakParmakIzleri =
    [
        "ZeNMAtUsbXmCmgNAqt7z1EHp2zFa+SvYZlmrWBAeo4M=",
        "v1hqiPJlSxxNL8oKhlKzoZ3cFdD65Cds5+zG9SwiBNc=",
    ];

    /// <summary>SHA-256 parmak izi (base64). Değer geri üretilemez.</summary>
    public static string ParmakIzi(string deger) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(deger)));

    /// <summary>Değer şablon yer tutucusu mu ya da bilinen örnek sır mı?</summary>
    public static bool KaraListede(string? deger) =>
        !string.IsNullOrEmpty(deger)
        && (deger.StartsWith(DegistirOneki, StringComparison.Ordinal)
            || YasakParmakIzleri.Contains(ParmakIzi(deger)));

    /// <summary>Kuralların bu ortamda uygulanıp uygulanmayacağı.</summary>
    public static bool Uygulanir(IConfiguration yapilandirma, IHostEnvironment ortam) =>
        ortam.IsProduction()
        || string.Equals(yapilandirma[ZorlaAnahtari], "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Üretim açılış kapısı. İhlalde <see cref="InvalidOperationException"/> atar;
    /// çağıran Program.cs olduğu için süreç açılmadan biter (fail-closed).
    /// </summary>
    public static void Dogrula(IConfiguration yapilandirma, IHostEnvironment ortam)
    {
        if (!Uygulanir(yapilandirma, ortam)) return;

        // ── FAIL-CLOSED 1: JWT sırrı ────────────────────────────────────────
        var sir = yapilandirma["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(sir))
            throw new InvalidOperationException(
                "Jwt:Secret tanımlı değil. Üretimde Jwt__Secret ortam değişkeni zorunludur.");
        if (KaraListede(sir))
            throw new InvalidOperationException(
                "Jwt:Secret varsayılan/örnek değerde. dotnet/kurulum.sh ile kuruluma özgü bir sır üretin " +
                "ve Jwt__Secret ortam değişkeninden verin (uygulama açılmıyor — fail-closed).");
        if (Encoding.UTF8.GetByteCount(sir) < AsgariSirBayt)
            throw new InvalidOperationException(
                $"Jwt:Secret {AsgariSirBayt} bayttan kısa. Daha uzun bir sır üretin (değer loglanmaz).");

        // ── FAIL-CLOSED 2: veritabanı parolası ──────────────────────────────
        var baglanti = yapilandirma.GetConnectionString("Kuyumcu");
        if (string.IsNullOrWhiteSpace(baglanti))
            throw new InvalidOperationException(
                "ConnectionStrings:Kuyumcu tanımlı değil. Üretimde ConnectionStrings__Kuyumcu zorunludur.");
        var parola = BaglantidanAl(baglanti, "password", "pwd");
        if (KaraListede(parola))
            throw new InvalidOperationException(
                "Veritabanı parolası varsayılan/örnek değerde. Her kurulum kendi parolasını üretmelidir " +
                "(dotnet/kurulum.sh). Uygulama açılmıyor — fail-closed.");

        // ── Tek kaynaktan veritabanı adı (KUYUMCU_VT) ───────────────────────
        // `docker-compose.yml` "kuyumcu", `appsettings.Development.json` "kuyumcu_net"
        // diyordu. Üretimde iki farklı ad üreten yapılandırma birleşimi SESSİZCE
        // boş bir şemaya bağlanmaya yol açar — burada durdurulur.
        var beklenenVt = Environment.GetEnvironmentVariable(VeritabaniAdiOrtamDegiskeni);
        var baglantiVt = BaglantidanAl(baglanti, "database", "initial catalog");
        if (!string.IsNullOrWhiteSpace(beklenenVt)
            && !string.Equals(beklenenVt, baglantiVt, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"{VeritabaniAdiOrtamDegiskeni} ortam değişkeni ile bağlantı dizgesindeki veritabanı adı " +
                "birbirini tutmuyor. Tek kaynak kuralı (T-1): ikisi aynı olmalı.");

        // ── Sır disiplini: yedek anahtarı alan şifreleme anahtarından AYRI ───
        // (MÜHÜR 3). Yedek anahtarının VARLIĞI burada zorunlu kılınmaz — yedek
        // otomasyonu T-3 paketidir; ama tanımlıysa AYNI olamaz.
        var yedekAnahtari = yapilandirma[YedekAnahtari];
        if (!string.IsNullOrWhiteSpace(yedekAnahtari)
            && yedekAnahtari == yapilandirma[AlanSifreleme.YapilandirmaAnahtari])
            throw new InvalidOperationException(
                $"{YedekAnahtari} ile {AlanSifreleme.YapilandirmaAnahtari} aynı olamaz " +
                "(yedek anahtarı alan şifreleme anahtarından ayrı tutulur).");

        // ── FAIL-CLOSED 4: TLS ──────────────────────────────────────────────
        // Ya Kestrel doğrudan HTTPS dinler, ya da ters vekil arkasında olduğu
        // AÇIKÇA beyan edilir. İkisi de yoksa üretimde açılış durur.
        if (!TlsBeyanEdilmis(yapilandirma))
            throw new InvalidOperationException(
                "TLS beyan edilmedi. Ya Kestrel'e https uç noktası verin (Urls / Kestrel:Endpoints) " +
                $"ya da ters vekil arkasındaysanız {TersVekilAnahtari}=true yapın. " +
                "TLS'siz üretim kurulumu onaylanmaz — fail-closed.");
    }

    /// <summary>Kestrel https uç noktası ya da açık ters vekil beyanı var mı?</summary>
    public static bool TlsBeyanEdilmis(IConfiguration yapilandirma)
    {
        if (string.Equals(yapilandirma[TersVekilAnahtari], "true", StringComparison.OrdinalIgnoreCase))
            return true;

        var urls = yapilandirma["Urls"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrWhiteSpace(urls)
            && urls.Contains("https://", StringComparison.OrdinalIgnoreCase))
            return true;

        return yapilandirma.GetSection("Kestrel:Endpoints").GetChildren()
            .Any(e => (e["Url"] ?? string.Empty).Contains("https://", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Bağlantı dizgesinden anahtar değeri çeker (değer DÖNDÜRÜLÜR, loglanmaz).</summary>
    private static string? BaglantidanAl(string baglanti, params string[] anahtarlar)
    {
        foreach (var parca in baglanti.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var esit = parca.IndexOf('=');
            if (esit <= 0) continue;
            var ad = parca[..esit].Trim();
            if (anahtarlar.Any(a => string.Equals(a, ad, StringComparison.OrdinalIgnoreCase)))
                return parca[(esit + 1)..].Trim();
        }
        return null;
    }
}
