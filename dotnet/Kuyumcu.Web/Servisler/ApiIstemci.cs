using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Core.I18n;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Servisler;

/// <summary>API'nin döndürdüğü Nest formatlı hata gövdesi.</summary>
public class ApiHata : Exception
{
    public ApiHata(int statusCode, string mesaj) : base(mesaj) => StatusCode = statusCode;
    public int StatusCode { get; }

    /// <summary>
    /// Sunucunun alan→mesaj sözlüğü (UX-4d madde 8). Ekran hatayı ilgili
    /// giriş alanının ALTINA yazabilsin diye taşınır; yoksa boştur.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Alanlar { get; init; } =
        new Dictionary<string, string[]>();

    /// <summary>
    /// Sunucunun bu isteğe verdiği korelasyon kimliği (paket T-4a).
    /// <para>
    /// Kullanıcı destek çağrısında TEK bu kodu söyler; destek aynı kodu sunucu
    /// günlüğünde arar. Sunucu göndermediyse boştur — ekran rozeti o zaman HİÇ
    /// çizilmez (uydurma kod gösterilmez, Anayasa md.1).
    /// </para>
    /// <para>⚠ Mevcut kurucu imzası DEĞİŞMEDİ: init-only alan, çağıran kırılmaz.</para>
    /// </summary>
    public string? IstekKimligi { get; init; }

    /// <summary>
    /// PAKET YASAL-3: reddin makine kodu (ör. PESIN_ART_ARDA_SORU, NAKIT_SINIRI) — ekran
    /// soru/karar penceresini METNE bakmadan açar. Sunucu göndermediyse null.
    /// </summary>
    public string? Kod { get; init; }
}

/// <summary>
/// UX-4d madde 15 — oturum düştü. ApiHata'dan türer ki mevcut
/// <c>catch (ApiHata)</c> blokları bunu da yakalasın ve Blazor'un
/// "Beklenmeyen bir hata oluştu" bandı ÇIKMASIN.
/// </summary>
public sealed class OturumDustuHatasi(string mesaj) : ApiHata(401, mesaj);

/// <summary>
/// PAKET P-07 (kimlik B-10) — sunucunun oran sınırı (429) üstel geri çekilmeli
/// yeniden denemelere rağmen açılmadı.
///
/// <para>
/// ⚠ <see cref="ApiHata"/>'dan TÜRER: sayfalardaki mevcut
/// <c>catch (ApiHata)</c> blokları bunu da yakalar, ekran sessizce BOŞALMAZ
/// (UX-S28) ve mesaj kullanıcıya kaç saniye kaldığını + "Yeniden dene"
/// yolunu söyler (UX-S30).
/// </para>
/// </summary>
public sealed class OranSinirHatasi(int kalanSaniye, string mesaj) : ApiHata(429, mesaj)
{
    /// <summary>Sunucunun <c>Retry-After</c> başlığı ya da son geri çekilme süresi (saniye).</summary>
    public int KalanSaniye { get; } = kalanSaniye;
}

/// <summary>
/// PAKET P-07 (kimlik B-57) — tek sayfa ömründe aynı uca istek FIRTINASI.
///
/// <para>
/// QA turu 5 kanıtı: <c>kur/guncel</c> ucuna 130'dan fazla istek atılıyor, arada
/// 401 alınıyordu. Kısa ömürlü GET önbelleği fırtınayı normalde soğurur; buna
/// RAĞMEN bir ekran aynı uca pencere içinde tavanı aşarsa bu bir kod hatasıdır
/// ve <b>gizlenmez</b> (Anayasa md.1): istek atılmaz, hata görünür olur.
/// </para>
/// </summary>
public sealed class IstekFirtinasiHatasi(string yol, int sayi, int tavan, string mesaj)
    : ApiHata(429, mesaj)
{
    public string Yol { get; } = yol;
    public int Sayi { get; } = sayi;
    public int Tavan { get; } = tavan;
}

/// <summary>
/// Axios client.ts karşılığı: Bearer ekler, 401'de refresh → isteği yineler,
/// olmuyorsa oturumu kapatıp /giris'e yönlendirir.
/// </summary>
public sealed class ApiIstemci(HttpClient http, OturumServisi oturum, NavigationManager nav)
{
    private static readonly JsonSerializerOptions JsonAyar = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// GET — PAKET P-07 (B-57): TEK UÇUŞ (single-flight).
    /// <para>
    /// Aynı yola AYNI ANDA düşen çağrılar TEK ağ isteğine iner; hepsi aynı
    /// yanıtı paylaşır. QA turu 5'te ölçülen <c>kur/guncel</c> yığılması
    /// (sayfa açılışında aynı uca üst üste binen istekler) burada söner.
    /// </para>
    /// <para>
    /// ⚠ ZAMAN PENCERELİ ÖNBELLEK YOKTUR (bilinçli karar, uygulama raporu § sapma):
    /// süre bazlı önbellek "Yenile" düğmesini yalancı yapar — kullanıcı tazeleme
    /// ister, ekran bayat veriyi geri verirdi (UX-S27/S28 ve mevcut ekran
    /// testleri bunu kilitliyor). Tek uçuş ise yalnız eşzamanlı KOPYA isteği
    /// söndürür; ardışık her çağrı gerçekten sunucuya gider.
    /// </para>
    /// </summary>
    /// <remarks>
    /// ⚠ PAKET ISTEK-1 SAPMASI (yukarıdaki "zaman pencereli önbellek yoktur"
    /// kararını DARALTIR): yalnız <see cref="ListeOnbellegi.Kaynaklar"/> beyaz
    /// listesindeki az değişen listeler (şubeler, reyonlar, kategoriler, kasa
    /// listesi, kur kaynakları, görünüm, canlı kur) kısa süre önbellekten
    /// karşılanır. "Yenile" yalancı olmasın diye: bu istemciden giden HER yazma
    /// önbelleği düşürür ve gerçek tazeleme isteyen çağıran
    /// <see cref="TazeGetAsync{T}"/> kullanır. Beyaz liste dışındaki her GET
    /// eskisi gibi her çağrıda sunucuya gider.
    /// </remarks>
    public async Task<T> GetAsync<T>(string yol)
    {
        var govde = await GetGovdeAsync(yol, taze: false);
        if (string.IsNullOrWhiteSpace(govde)) return default!;
        return JsonSerializer.Deserialize<T>(govde, JsonAyar)!;
    }

    /// <summary>
    /// PAKET ISTEK-1 — önbelleği ATLAYAN GET ("Yenile" düğmeleri, "kuru şimdi
    /// sor"). Yanıt önbelleği de tazeler. Eşzamanlı aynı yol yine tek uçuşa iner.
    /// ⚠ 429'da son-iyi yedeğine DÜŞMEZ: "Yenile"/kur sorgusu eski gövdeyi
    /// "yeni sorgulandı" gibi alamaz — <see cref="OranSinirHatasi"/> çağırana
    /// ulaşır (doğrulayıcı bulgusu: kur saati yoğunlukta asla bayatlamıyordu).
    /// </summary>
    public async Task<T> TazeGetAsync<T>(string yol)
    {
        var govde = await GetGovdeAsync(yol, taze: true);
        if (string.IsNullOrWhiteSpace(govde)) return default!;
        return JsonSerializer.Deserialize<T>(govde, JsonAyar)!;
    }

    public async Task<T> PostAsync<T>(string yol, object? govde = null) =>
        await OkuAsync<T>(await YazAsync(() => Istek(HttpMethod.Post, yol, govde)));

    public async Task<T> PatchAsync<T>(string yol, object? govde = null) =>
        await OkuAsync<T>(await YazAsync(() => Istek(HttpMethod.Patch, yol, govde)));

    public async Task<T> PutAsync<T>(string yol, object? govde = null) =>
        await OkuAsync<T>(await YazAsync(() => Istek(HttpMethod.Put, yol, govde)));

    public async Task<T> DeleteAsync<T>(string yol) =>
        await OkuAsync<T>(await YazAsync(() => Istek(HttpMethod.Delete, yol)));

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET ISTEK-1 — LİSTE ÖNBELLEĞİ + YOĞUNLUK DURUMU
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Az değişen listelerin önbelleği (oturum kimliğine bağlı). Oturum olayı
    /// (giriş, jeton yenileme, kullanıcı/mod ataması, çıkış) önbelleği boşaltır.
    /// </summary>
    public ListeOnbellegi Onbellek => onbellek ??= OnbellekKur();
    private ListeOnbellegi? onbellek;

    private ListeOnbellegi OnbellekKur()
    {
        var o = new ListeOnbellegi();
        // Oturumun İLK kullanıcı ataması (açılışta /auth/ben → KullaniciAta,
        // yok → var) aynı oturumdur, önbelleği düşürmez; sonraki her oturum
        // olayı (yeniden atama, mod/profil değişimi, jeton yenileme, çıkış,
        // yeni giriş) düşürür.
        var kullaniciVardi = oturum.Kullanici is not null;
        oturum.Degisti += () =>
        {
            var simdi = oturum.Kullanici is not null;
            if (kullaniciVardi || !simdi) o.YazmaOldu();
            kullaniciVardi = simdi;
        };
        return o;
    }

    private string? OturumKimligi => ListeOnbellegi.KimlikCoz(oturum.AccessToken);

    /// <summary>
    /// Yazma: önbellek isteği göndermeden ÖNCE ve yanıt geldikten SONRA düşer.
    /// Önce: yazma sürerken başlayan okuma eski hâli önbelleğe yazamasın (nesil).
    /// Sonra: yazma sürerken tamamlanan okuma da düşsün.
    /// </summary>
    private async Task<HttpResponseMessage> YazAsync(Func<HttpRequestMessage> istekUret)
    {
        Onbellek.YazmaOldu();
        UcuslariBirak();
        try { return await GonderAsync(istekUret); }
        finally
        {
            Onbellek.YazmaOldu();
            // Yazmadan ÖNCE başlamış bir GET uçuşuna yazmadan SONRA gelen çağrı
            // katılmasın: eski hâli alırdı. Süren uçuş kendi bekleyenlerine döner.
            UcuslariBirak();
        }
    }

    /// <summary>
    /// Sunucu yoğun (429) ve istemci kendiliğinden bekleyip yeniden deniyor ya da
    /// ekranda az önceki veriyi gösteriyor. Üst bar tek satırlık sade bandı
    /// bundan çizer; sonraki başarılı yanıtta kendiliğinden kapanır.
    /// </summary>
    public bool Yogun { get; private set; }

    /// <summary><see cref="Yogun"/> değişti (üst bar abone olur).</summary>
    public event Action? YogunlukDegisti;

    private void YogunlukBildir(bool yogun)
    {
        if (Yogun == yogun) return;
        Yogun = yogun;
        YogunlukDegisti?.Invoke();
    }

    /// <summary>
    /// 429'da, sunucunun <c>Retry-After</c> süresi bu tavanı aşmıyorsa istemci
    /// BİR KEZ o kadar bekleyip yeniden dener (kurucu kararı: "Retry-After
    /// süresine göre bir kez otomatik yeniden dene"). Daha uzunsa karar kullanıcıya kalır.
    /// </summary>
    public const int TekSeferlikBeklemeTavaniSaniye = 15;

    /// <summary>
    /// Oturum AÇILMADAN, elde tutulan KISA ÖMÜRLÜ bir jetonla tek POST atar —
    /// girişin ikinci adımı (45-UI · POST /auth/mfa/giris).
    /// <para>
    /// ⚠ Sunucu meydan okuma jetonunu <c>Authorization: Bearer</c> başlığında
    /// bekler, GÖVDEDE değil (AuthController.GirisMfa). Bu yüzden ayrı bir
    /// giriş noktası var: normal <see cref="Istek"/> oturumun access token'ını
    /// koyar, ikinci adımda ise ortada henüz bir oturum yoktur.
    /// </para>
    /// <para>
    /// ⚠ 401 YENİLEME akışına GİRMEZ: yenilenecek bir oturum yok. 401 doğrudan
    /// çağırana döner ve ekran "kod hatalı" mesajını gösterir — kullanıcı
    /// sessizce /giris'e atılmaz.
    /// </para>
    /// </summary>
    public async Task<T> PostJetonlaAsync<T>(string yol, string jeton, object? govde = null)
    {
        var istek = new HttpRequestMessage(HttpMethod.Post, $"api/v1{yol}");
        DilBasligiKoy(istek);
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jeton);
        if (govde is not null)
            istek.Content = JsonContent.Create(govde, options: JsonAyar);
        return await OkuAsync<T>(await http.SendAsync(istek));
    }

    /// <summary>
    /// PAKET YASAL-PARAM-EKRAN — ByGatech SİSTEM YÖNETİCİSİ uçları (<c>/sistem/*</c>). Kimlik
    /// kiracı JWT'si DEĞİL, sunucunun <c>SistemAnahtariKapisi</c>'nın beklediği başlıktır
    /// (yeni rol açılmadı). Anahtar yalnız bu isteğin başlığına konur — saklanmaz,
    /// günlüğe/depoya yazılmaz; JWT EKLENMEZ (iki kimlik karışmaz). Fırtına kapısından geçer
    /// (P-07: ikinci HTTP yolu yok); 401 yenileme akışına GİRMEZ (yenilenecek oturum yok).
    /// </summary>
    /// <summary>Operatör adını HTTP başlığına güvenle konacak biçime çevirir: <c>UTF-8''&lt;yüzde-kodlu&gt;</c>.</summary>
    public static string OperatorBasligiDegeri(string ad) => "UTF-8''" + Uri.EscapeDataString(ad.Trim());

    public async Task<T> SistemPostAsync<T>(string yol, string anahtar, string operatorAdi, object govde)
    {
        var istek = new HttpRequestMessage(HttpMethod.Post, $"api/v1{yol}");
        DilBasligiKoy(istek);
        istek.Headers.TryAddWithoutValidation("X-Sistem-Anahtari", anahtar);
        // TUR 2 (tasarım hakemi KRİTİK): başlık yalnız ASCII taşır; Türkçe harfli ad ("Tasarım
        // Hakemi") tarayıcıda HttpRequestException ile isteği düşürüyordu → RFC 5987 kodlaması
        // (sunucu SistemAnahtariKapisi.OperatorCoz ile çözer).
        istek.Headers.TryAddWithoutValidation("X-Sistem-Operator", OperatorBasligiDegeri(operatorAdi));
        istek.Content = JsonContent.Create(govde, options: JsonAyar);
        return await OkuAsync<T>(await HamGonderAsync(istek, UcAnahtari(istek)));
    }

    /// <summary>
    /// İkili yanıt indirir (şifreli ZIP gibi). 401 yenileme akışı aynen geçerlidir.
    /// Dosya adı Content-Disposition'dan okunur; yoksa çağıran karar verir.
    /// </summary>
    public async Task<(byte[] Veri, string? DosyaAdi)> PostBaytAsync(string yol, object? govde = null)
    {
        var yanit = await YazAsync(() => Istek(HttpMethod.Post, yol, govde));
        if (!yanit.IsSuccessStatusCode) await HataFirlat(yanit);
        return (await yanit.Content.ReadAsByteArrayAsync(),
                yanit.Content.Headers.ContentDisposition?.FileNameStar
                ?? yanit.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }

    /// <summary>
    /// İkili yanıt İNDİRİR (GET). Belge deposu içerik ucu için (paket 4);
    /// 401 yenileme akışı aynen geçerlidir.
    /// </summary>
    public async Task<(byte[] Veri, string? DosyaAdi)> GetBaytAsync(string yol)
    {
        var yanit = await GonderAsync(() => Istek(HttpMethod.Get, yol));
        if (!yanit.IsSuccessStatusCode) await HataFirlat(yanit);
        return (await yanit.Content.ReadAsByteArrayAsync(),
                yanit.Content.Headers.ContentDisposition?.FileNameStar
                ?? yanit.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }

    public Task<JsonElement> GetAsync(string yol) => GetAsync<JsonElement>(yol);
    public Task<JsonElement> PostAsync(string yol, object? govde = null) => PostAsync<JsonElement>(yol, govde);
    public Task<JsonElement> PatchAsync(string yol, object? govde = null) => PatchAsync<JsonElement>(yol, govde);
    public Task<JsonElement> PutAsync(string yol, object? govde = null) => PutAsync<JsonElement>(yol, govde);

    // ── iç işleyiş ───────────────────────────────────────────────────────────

    /// <summary>
    /// PAKET DIL-SUNUCU: arayüz dilini sunucuya SÖYLER (Accept-Language: en | tr). Tarayıcının kendi dil başlığı
    /// uygulamanın seçili dilini temsil etmez (TR uygulama + İngilizce tarayıcı olabilir) — başlık BİLEREK
    /// ezilir. Sunucu hata/uyarı/künye cümlelerini bu dile göre sözlükten seçer; veri ve tutarlar etkilenmez.
    /// </summary>
    public static void DilBasligiKoy(HttpRequestMessage istek)
    {
        istek.Headers.AcceptLanguage.Clear();
        istek.Headers.TryAddWithoutValidation("Accept-Language", Bicim.Dil == "en" ? "en" : "tr");
    }

    private HttpRequestMessage Istek(HttpMethod metot, string yol, object? govde = null)
    {
        var istek = new HttpRequestMessage(metot, $"api/v1{yol}");
        DilBasligiKoy(istek);
        if (!string.IsNullOrEmpty(oturum.AccessToken))
            istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oturum.AccessToken);
        if (govde is not null)
            istek.Content = JsonContent.Create(govde, options: JsonAyar);
        return istek;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // UX-4d madde 15 — SÜRESİ DOLMUŞ OTURUMDA 3× /auth/yenile + 3× /auth/cikis.
    //
    // Bir sayfa açılışta paralel 3 istek atıyor. Üçü de 401 alıyor, ÜÇÜ BİRDEN
    // yenilemeyi deniyor (3 × POST /auth/yenile → 401), üçü birden çıkış yapıp
    // üçü birden /giris'e yönlendiriyordu; ardından okunamayan yanıt
    // ApiHata olarak yukarı kaçıp Blazor'un "Beklenmeyen bir hata oluştu"
    // bandını açıyordu.
    //
    // Çözüm: yenileme TEK UÇUŞ (single-flight) — ilk çağıran denemeyi yapar,
    // diğerleri aynı Task'ı bekler; başarısızsa oturum TEK KEZ temizlenir,
    // TEK KEZ yönlendirme yapılır ve çağrılara Türkçe "Oturum süresi doldu"
    // hatası fırlatılır (hata bandı değil, giriş sayfası görünür).
    // ═════════════════════════════════════════════════════════════════════════
    private Task<bool>? yenilemeUcusu;
    private bool oturumDusuruldu;
    private readonly SemaphoreSlim kilit = new(1, 1);

    private async Task<HttpResponseMessage> GonderAsync(Func<HttpRequestMessage> istekUret)
    {
        var ilk = istekUret();
        if (oturumDusuruldu)
        {
            // Düşen sekmede yeniden giriş yapıldıysa yeni oturum başlamıştır; bayrak kalkar.
            // Giriş öncesi (anonim) uçlar düşmüş sekmeden de gider: yoksa /giris ekranı
            // "yeniden giriş yapın" derken giriş isteğinin kendisi engellenirdi.
            if (oturum.GirisYapildi) { oturumDusuruldu = false; baskaKullaniciAlgilandi = false; }
            else if (!GirisOncesiUc(ilk))
            {
                ilk.Dispose();
                throw new OturumDustuHatasi(OturumDustuMesaji);
            }
        }

        var uc = UcAnahtari(ilk);
        var yanit = await HamGonderAsync(ilk, uc);

        // ── PAKET P-07 / B-10: 429 → üstel geri çekilme + jitter ────────────
        var kalanSaniye = 0;
        var uzunBeklendi = false;
        for (var deneme = 1; yanit.StatusCode == HttpStatusCode.TooManyRequests; deneme++)
        {
            var sunucununSuresi = RetryAfterSaniye(yanit);
            kalanSaniye = sunucununSuresi ?? kalanSaniye;
            var uzun = sunucununSuresi is int s && s > OtomatikBeklemeTavaniSaniye;

            // Sunucu "çok uzun süre bekle" diyorsa kullanıcıyı ekranda bekletmeyiz:
            // karar kullanıcıya bırakılır ("Yeniden dene" + kalan süre).
            // PAKET ISTEK-1: Retry-After ≤ TekSeferlikBeklemeTavaniSaniye ise BİR KEZ
            // o kadar beklenir (üst barda sade yoğunluk bandı açık kalır) ve yeniden denenir.
            if (deneme > GeriCekilmeDenemeTavani
                || (uzun && (uzunBeklendi || sunucununSuresi > TekSeferlikBeklemeTavaniSaniye)))
                break;
            if (uzun)
            {
                uzunBeklendi = true;
                YogunlukBildir(true);
            }

            var bekleme = GeriCekilmeSuresi(sunucununSuresi, deneme);
            kalanSaniye = Math.Max(kalanSaniye, (int)Math.Ceiling(bekleme.TotalSeconds));
            GeriCekilmeSayisi++;
            yanit.Dispose();
            await Uyu(bekleme);
            yanit = await HamGonderAsync(istekUret(), uc);
        }

        if (yanit.StatusCode == HttpStatusCode.TooManyRequests)
        {
            yanit.Dispose();
            var kalan = kalanSaniye > 0 ? kalanSaniye : OtomatikBeklemeTavaniSaniye;
            throw new OranSinirHatasi(kalan, OranSinirMesaji(kalan, DilKaynagi()));
        }
        // Yoğunluk geçti: bant kendiliğinden kapanır.
        YogunlukBildir(false);

        if (yanit.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(oturum.RefreshToken))
        {
            if (await TekUcuslaYenileAsync())
                yanit = await HamGonderAsync(istekUret(), uc);   // taze token ile tekrar
            else
            {
                await OturumuDusurAsync();
                throw new OturumDustuHatasi(OturumDustuMesaji);
            }
        }
        return yanit;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PAKET P-07 — DAYANIKLILIK BANDI (kimlikler B-10 · B-57)
    //
    // Tek kapı: web istemcisinin ATTIĞI HER istek buradan geçer. Bu yüzden
    // `ApiIstemci` dışında HttpClient tüketicisi kalmamalıdır
    // (denetim.sh sayacı APIISTEMCI_DISI_HTTPCLIENT = 0).
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>429'da en fazla kaç kez otomatik yeniden denenir.</summary>
    public const int GeriCekilmeDenemeTavani = 3;

    /// <summary>İlk geri çekilme basamağı (ms); her denemede ikiye katlanır.</summary>
    public const int TabanGecikmeMs = 200;

    /// <summary>Jitter tavanı (ms) — eşzamanlı istemciler aynı anda dönmesin.</summary>
    public const int JitterMs = 250;

    /// <summary>Sunucu bundan uzun bekleme isterse otomatik denenmez, karar kullanıcıya bırakılır.</summary>
    public const int OtomatikBeklemeTavaniSaniye = 5;

    /// <summary>
    /// B-57 — tek sayfa ömründe aynı uca, <see cref="IstekPenceresi"/> içinde
    /// izin verilen azami istek. Bu tavan bir KULLANIM sınırı değil, KOD HATASI
    /// dedektörüdür; denetim.sh'ta cırcırdır (yalnız azalır).
    /// </summary>
    public const int IstekTekrarTavani = 60;

    public static readonly TimeSpan IstekPenceresi = TimeSpan.FromSeconds(60);

    /// <summary>Test kancaları — gerçek saat/uyku/rastgelelik sınanabilir olsun diye.</summary>
    public Func<DateTimeOffset> Simdi { get; set; } = () => DateTimeOffset.UtcNow;
    public Func<TimeSpan, Task> Uyu { get; set; } = sure => Task.Delay(sure);
    public Func<int, int> Jitter { get; set; } = tavan => Random.Shared.Next(0, tavan + 1);

    /// <summary>Ağa fiilen çıkan istek sayısı (test kancası).</summary>
    public int GonderilenIstek { get; private set; }

    /// <summary>429 yüzünden kaç kez geri çekilindiği (test kancası).</summary>
    public int GeriCekilmeSayisi { get; private set; }

    private readonly object bandKilidi = new();
    private readonly Dictionary<string, (DateTimeOffset Baslangic, int Sayi)> istekSayaci = [];
    private readonly Dictionary<string, Task<string>> getUcusu = [];

    /// <summary>Sorgu dizesi ATILIR: fırtına "aynı uç" üzerinden ölçülür.</summary>
    private static string UcAnahtari(HttpRequestMessage istek)
    {
        var yol = istek.RequestUri!.OriginalString;
        var soru = yol.IndexOf('?');
        return soru >= 0 ? yol[..soru] : yol;
    }

    private async Task<HttpResponseMessage> HamGonderAsync(HttpRequestMessage istek, string uc)
    {
        FirtinaKontrol(uc);
        GonderilenIstek++;
        return await http.SendAsync(istek);
    }

    /// <summary>B-57 — tavan aşılırsa istek ATILMAZ, hata GÖRÜNÜR olur (Anayasa md.1).</summary>
    private void FirtinaKontrol(string uc)
    {
        int sayi;
        lock (bandKilidi)
        {
            var simdi = Simdi();
            if (!istekSayaci.TryGetValue(uc, out var kayit) || simdi - kayit.Baslangic >= IstekPenceresi)
                kayit = (simdi, 0);
            sayi = kayit.Sayi + 1;
            istekSayaci[uc] = (kayit.Baslangic, sayi);
        }
        if (sayi > IstekTekrarTavani)
            throw new IstekFirtinasiHatasi(uc, sayi, IstekTekrarTavani, FirtinaMesaji(uc, sayi));
    }

    private TimeSpan GeriCekilmeSuresi(int? sunucununSuresi, int deneme)
    {
        var temel = sunucununSuresi is int s && s > 0
            ? TimeSpan.FromSeconds(s)
            : TimeSpan.FromMilliseconds(TabanGecikmeMs * Math.Pow(2, deneme - 1));
        return temel + TimeSpan.FromMilliseconds(Jitter(JitterMs));
    }

    private static int? RetryAfterSaniye(HttpResponseMessage yanit)
    {
        if (yanit.Headers.RetryAfter?.Delta is TimeSpan d) return (int)Math.Ceiling(d.TotalSeconds);
        if (yanit.Headers.TryGetValues("Retry-After", out var degerler)
            && int.TryParse(degerler.FirstOrDefault(), System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out var saniye))
            return saniye;
        return null;
    }

    /// <summary>UX-S30 + UX-S28: ne olduğu, ne kadar kaldığı ve NE YAPILACAĞI yazılı (TR+EN, UstBarSozlugu).</summary>
    public static string OranSinirMesaji(int kalanSaniye, string dil = "tr") =>
        UstBarSozlugu.OranSinir(dil, kalanSaniye);

    /// <summary>
    /// PAKET ISTEK-1 — hata metinlerinin dili. Program.cs <c>() =&gt; DilServisi.Dil</c>
    /// bağlar; ApiIstemci DilServisi'ne DI ile bağlanmaz (onlarca bUnit sınıfı
    /// ApiIstemci'yi tek başına kurar). Bağ yoksa Türkçe.
    /// </summary>
    public Func<string> DilKaynagi { get; set; } = () => "tr";

    public static string FirtinaMesaji(string uc, int sayi) =>
        $"Aynı adrese ({uc}) bir dakika içinde {sayi} istek atıldı; tavan {IstekTekrarTavani}. " +
        "İstek gönderilmedi. Sayfayı yenileyin; sorun sürerse bu mesajı destek kaydına yazın.";

    private async Task<string> GetGovdeAsync(string yol, bool taze)
    {
        var kimlik = OturumKimligi;
        try
        {
            return await GetGovdeUcusuAsync(yol, taze);
        }
        catch (OranSinirHatasi hata) when (!taze && Onbellek.SonIyiAl(
            yol, kimlik, TimeSpan.FromSeconds(hata.KalanSaniye) + ListeOnbellegi.SonIyiEkPayi, out var sonIyi))
        {
            // PAKET ISTEK-1 (UX-S28): sunucu ısrarla 429 dedi ama bu ekranın AZ
            // ÖNCEKİ (yaşı Retry-After + 60 sn içinde) verisi elde. Liste
            // BOŞALTILMAZ; üst bardaki sade yoğunluk bandı açılır ve sonraki
            // başarılı yanıtta kendiliğinden kapanır. Daha eski gövde, canlı kur
            // ve "Yenile" (taze) çağrıları yedeğe DÜŞMEZ — hata çağırana ulaşır.
            YogunlukBildir(true);
            return sonIyi;
        }
    }

    private Task<string> GetGovdeUcusuAsync(string yol, bool taze)
    {
        // PAKET ISTEK-1: beyaz listedeki az değişen liste tazeyse AĞA ÇIKILMAZ.
        if (!taze && Onbellek.TryAl(yol, OturumKimligi, out var onbellekten))
            return Task.FromResult(onbellekten);

        lock (bandKilidi)
        {
            if (getUcusu.TryGetValue(yol, out var ucusan)) return ucusan;

            // ⚠ Uçuş kaydı, çekim BAŞLAMADAN ÖNCE konur. Aksi hâlde eşzamanlı
            //   tamamlanan (sahte/anlık) bir yanıtta `finally` kaydı biz henüz
            //   yazmadan siler ve BİTMİŞ görev sözlükte ÖLÜMSÜZ kalır —
            //   önbellek süresi dolsa bile ikinci istek hiç atılmaz.
            var bayrak = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            getUcusu[yol] = bayrak.Task;
            _ = GetGovdeCekAsync(yol, bayrak);
            return bayrak.Task;
        }
    }

    /// <summary>Tek uçuş kayıtlarını bırakır (yeni çağrılar yeni istek başlatır).</summary>
    private void UcuslariBirak()
    {
        lock (bandKilidi) getUcusu.Clear();
    }

    /// <summary>
    /// PAKET ISTEK-1 (testte yakalandı): uçuş kaydı sonuç YAZILMADAN önce
    /// silinir. Eskiden `finally` içinde siliniyordu; sonuç yazıldıktan sonra
    /// ama kayıt silinmeden önce gelen yeni çağrı BİTMİŞ eski uçuşa katılıp
    /// eski yanıtı alıyordu (yazma sonrası "yeni reyon görünmüyor").
    /// Yalnız KENDİ kaydını siler — bu arada açılmış yeni uçuşa dokunmaz.
    /// </summary>
    private void UcusuKapat(string yol, TaskCompletionSource<string> bayrak)
    {
        lock (bandKilidi)
            if (getUcusu.TryGetValue(yol, out var kayit) && ReferenceEquals(kayit, bayrak.Task))
                getUcusu.Remove(yol);
    }

    private async Task GetGovdeCekAsync(string yol, TaskCompletionSource<string> bayrak)
    {
        var kimlik = OturumKimligi;
        var nesil = Onbellek.NesilAl(kimlik);
        try
        {
            var yanit = await GonderAsync(() => Istek(HttpMethod.Get, yol));
            if (!yanit.IsSuccessStatusCode) await HataFirlat(yanit);
            var govde = await yanit.Content.ReadAsStringAsync();
            Onbellek.Koy(yol, kimlik, govde, nesil);
            UcusuKapat(yol, bayrak);
            bayrak.SetResult(govde);
        }
        catch (Exception hata)
        {
            UcusuKapat(yol, bayrak);
            bayrak.SetException(hata);   // hata PAYLAŞILIR; bir sonraki çağrı yeniden dener
        }
        finally
        {
            UcusuKapat(yol, bayrak);
        }
    }

    /// <summary>
    /// Sunucudaki refresh token kaydını iptal eder (çıkış).
    /// <para>
    /// ⚠ 401 YENİLEME akışına GİRMEZ ve geri çekilmez: çıkış yolunda yenilenecek
    /// oturum yoktur, ağ hatası da yerel temizliği engellememelidir.
    /// <see cref="OturumServisi"/> bunu çağırır — böylece web tarafında
    /// <c>ApiIstemci</c> dışında HttpClient tüketicisi kalmaz (P-07 sayacı).
    /// </para>
    /// </summary>
    public async Task<bool> JetonIptalEtAsync(string refreshToken)
    {
        try
        {
            GonderilenIstek++;
            var yanit = await http.PostAsJsonAsync("api/v1/auth/cikis", new { refreshToken }, JsonAyar);
            return yanit.IsSuccessStatusCode;
        }
        catch (HttpRequestException) { return false; }   // ağ yok — yerel temizlik yine de yapılır
        catch (TaskCanceledException) { return false; }  // zaman aşımı — aynı gerekçe
    }

    /// <summary>Ekranların gösterdiği Türkçe metin — sözlükten bağımsız sabit.</summary>
    internal const string OturumDustuMesaji = "Oturum süresi doldu — lütfen tekrar giriş yapın.";

    /// <summary>Kaç kez yenileme İSTEĞİ atıldığı (test kancası).</summary>
    internal int YenilemeDenemesi { get; private set; }

    private async Task<bool> TekUcuslaYenileAsync()
    {
        if (oturumDusuruldu) return false;

        Task<bool> ucus;
        await kilit.WaitAsync();
        try
        {
            if (oturumDusuruldu) return false;
            ucus = yenilemeUcusu ??= YenileAsync();
        }
        finally { kilit.Release(); }

        var sonuc = await ucus;

        await kilit.WaitAsync();
        try { yenilemeUcusu = null; }
        finally { kilit.Release(); }

        return sonuc;
    }

    private async Task OturumuDusurAsync()
    {
        await kilit.WaitAsync();
        try
        {
            if (oturumDusuruldu) return;
            oturumDusuruldu = true;
        }
        finally { kilit.Release(); }

        if (baskaKullaniciAlgilandi)
        {
            // Öteki kullanıcının depodaki oturumuna dokunulmaz, sunucuya istek gitmez.
            oturum.BaskaKullaniciYuzundenBirak();
            nav.NavigateTo("/giris");
            return;
        }
        await oturum.CikisAsync();
        nav.NavigateTo("/giris");
    }

    /// <summary>Oturum gerektirmeyen giriş ekranı uçları (düşmüş sekmeden de gönderilir).</summary>
    private static bool GirisOncesiUc(HttpRequestMessage istek)
    {
        var yol = istek.RequestUri?.OriginalString ?? "";
        var soru = yol.IndexOf('?');
        if (soru >= 0) yol = yol[..soru];
        return yol.EndsWith("/auth/giris", StringComparison.Ordinal)
            || yol.EndsWith("/auth/kayit", StringComparison.Ordinal)
            || yol.EndsWith("/genel/kisitlar", StringComparison.Ordinal)
            // PAKET AUTH-SIFIRLA: "Parolamı unuttum" uçları oturumsuz çalışır.
            || yol.Contains("/auth/parola-sifirlama/", StringComparison.Ordinal);
    }

    /// <summary>Keşif B006 güvenlik kapısı: depoda başka kullanıcının çifti görüldü.</summary>
    private volatile bool baskaKullaniciAlgilandi;

    // ═════════════════════════════════════════════════════════════════════════
    // Keşif B006 (EMANET-1 son doğrulama, KRİTİK) — İKİ SEKME JETON YARIŞI.
    //
    // Yukarıdaki tek uçuş yalnız BU sekmenin isteklerini birleştirir. Aynı
    // kullanıcı iki sekmede çalışınca iki sekme aynı eski yenileme jetonunu
    // sunucuya götürüyor; ikincisi sunucuda "iptal edilmiş jetonun yeniden
    // kullanımı" sayılıp TÜM aile iptal ediliyordu (RFC 9700 §4.14.2 — bu
    // sunucu davranışı DOĞRUDUR ve korunur). Düzeltme istemcide:
    //   1) yenileme tarayıcı genelinde tek kilit altında yapılır (js/oturum-kilidi.js);
    //   2) kilidi alan sekme depoyu TAZE okur; başka sekme zaten döndürdüyse
    //      onun çiftini benimser ve sunucuya eski jetonla hiç gitmez.
    // ═════════════════════════════════════════════════════════════════════════
    private async Task<bool> YenileAsync()
    {
        var kilitKimligi = await oturum.SekmeKilidiAlAsync();
        try
        {
            switch (await oturum.BaskaSekmeninCiftiniBenimseAsync())
            {
                case CiftBenimseme.Benimsendi:
                    return true;
                case CiftBenimseme.BaskaKullanici:
                    // ⚠ GÜVENLİK (30.09 05:52): depodaki çift başka kullanıcı/kiracıya ait.
                    // Ne benimsenir ne de bu sekmenin jetonuyla sunucuya gidilir; oturum
                    // düşürülür ve giriş ekranı nedenini söyler (OturumuDusurAsync).
                    baskaKullaniciAlgilandi = true;
                    return false;
                default:
                    return await SunucudaYenileAsync();
            }
        }
        finally { await oturum.SekmeKilidiBirakAsync(kilitKimligi); }
    }

    private async Task<bool> SunucudaYenileAsync()
    {
        YenilemeDenemesi++;
        try
        {
            var yanit = await http.PostAsJsonAsync("api/v1/auth/yenile",
                new { refreshToken = oturum.RefreshToken }, JsonAyar);
            if (!yanit.IsSuccessStatusCode) return false;
            var veri = await yanit.Content.ReadFromJsonAsync<JsonElement>(JsonAyar);
            await oturum.TokenKaydetAsync(
                veri.GetProperty("accessToken").GetString()!,
                veri.GetProperty("refreshToken").GetString()!);
            return true;
        }
        catch { return false; }
    }

    private static async Task<T> OkuAsync<T>(HttpResponseMessage yanit)
    {
        if (yanit.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(JsonElement) || yanit.Content.Headers.ContentLength > 0)
                return (await yanit.Content.ReadFromJsonAsync<T>(JsonAyar))!;
            return default!;
        }

        await HataFirlat(yanit);
        return default!;   // HataFirlat her zaman fırlatır
    }

    /// <summary>Sunucunun hata gövdesini ApiHata'ya çevirir. Her zaman fırlatır.</summary>
    private static async Task HataFirlat(HttpResponseMessage yanit)
    {
        var mesaj = "İstek başarısız";
        var alanlar = new Dictionary<string, string[]>();
        string? kod = null;   // PAKET YASAL-3
        // PAKET T-4a: kimlik ÖNCE başlıktan okunur — gövde okunamasa bile
        // (bozuk/boş yanıt) destek kodu elde kalır.
        string? istekKimligi = yanit.Headers.TryGetValues("X-Istek-Kimligi", out var basliklar)
            ? basliklar.FirstOrDefault()
            : null;
        try
        {
            var hata = await yanit.Content.ReadFromJsonAsync<JsonElement>(JsonAyar);
            if (hata.TryGetProperty("message", out var m))
                mesaj = m.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", m.EnumerateArray().Select(x => x.GetString()))
                    : m.GetString() ?? mesaj;

            // UX-4d madde 8: sunucunun alan→mesaj sözlüğü taşınır.
            if (hata.TryGetProperty("alanlar", out var a) && a.ValueKind == JsonValueKind.Object)
                foreach (var alan in a.EnumerateObject())
                    alanlar[alan.Name] = alan.Value.ValueKind == JsonValueKind.Array
                        ? [.. alan.Value.EnumerateArray().Select(x => x.GetString() ?? "")]
                        : [alan.Value.GetString() ?? ""];

            if (hata.TryGetProperty("kod", out var kd) && kd.ValueKind == JsonValueKind.String)
                kod = kd.GetString();   // PAKET YASAL-3: yalnız işaretli redlerde gelir

            // Gövdedeki kimlik başlıktakiyle aynıdır; başlık okunamadıysa
            // (vekil sıyırmışsa) gövde yedektir.
            if (string.IsNullOrEmpty(istekKimligi)
                && hata.TryGetProperty("istekKimligi", out var k) && k.ValueKind == JsonValueKind.String)
                istekKimligi = k.GetString();
        }
        catch { /* gövde okunamadıysa genel mesaj */ }

        throw new ApiHata((int)yanit.StatusCode, mesaj)
        {
            Alanlar = alanlar,
            IstekKimligi = string.IsNullOrWhiteSpace(istekKimligi) ? null : istekKimligi,
            Kod = string.IsNullOrWhiteSpace(kod) ? null : kod,
        };
    }
}
