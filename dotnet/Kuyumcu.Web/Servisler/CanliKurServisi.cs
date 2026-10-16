using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Canlı kur durumu. İki kanal birlikte çalışır:
///   • SignalR push — sunucu yeni snapshot yazdığı anda haber verir (anlık).
///   • Kendi yoklaması (varsayılan 10 sn, <c>Kur:YoklamaAraligiSaniye</c>) —
///     WebSocket kurulamasa (proxy/güvenlik duvarı) bile çubuk güncel kalır.
/// PAKET P-70 (QA turu 8: 6 rotalık turda 154 /kur/guncel isteği): push BAĞLIYKEN
/// ve sekme GİZLİYKEN yoklama ağ isteği atmaz; bileşenler kendi isteği yerine
/// <see cref="AlAsync"/> tek okuma noktasını kullanır.
/// WASM'da scoped == tekil: tüm uygulama için TEK HubConnection paylaşılır.
/// Bileşenler <see cref="Degisti"/> olayına abone olur, <see cref="Son"/>'u okur.
/// </summary>
public sealed class CanliKurServisi(ApiIstemci api, OturumServisi oturum, NavigationManager nav, IServiceProvider sp)
    : IAsyncDisposable, IDisposable
{
    /// <summary>Çubuk/işlem ekranı için gereken kur alanları (JSON camelCase eşleşir).</summary>
    /// <remarks>
    /// PAKET S1-c: <paramref name="Kaynak"/> (profesyonel yüzde "14:32 · Altınkaynak")
    /// ve <paramref name="AyarFiyatlari"/> (tuvaldeki "Gram 22") EKLENDİ. İkisi de
    /// sunucunun /kur/guncel yanıtında (KurSnapshotCevap) zaten vardı; istemci
    /// okumuyordu. Varsayılanlı oldukları için eski yanıt/sahte API kırılmaz.
    /// </remarks>
    public sealed record KurAnlik(
        string Id, decimal AltinHasFiyati, decimal? GramAltinFiyati,
        decimal? UsdTry, decimal? EurTry, decimal? GbpTry,
        string? Kaynak = null, IReadOnlyList<AyarFiyatiAnlik>? AyarFiyatlari = null)
    {
        /// <summary>
        /// Verilen ayar kimliğinin (K22 …) SUNUCUDA hesaplanmış gram fiyatı;
        /// yanıtta yoksa null — istemci has × milyem ÇARPIMI YAPMAZ (UX-S21).
        /// </summary>
        public decimal? AyarGramFiyati(string ayarId) =>
            AyarFiyatlari?.FirstOrDefault(a => a.AyarId == ayarId)?.GramFiyati;
    }

    /// <summary>/kur/guncel → ayarFiyatlari[] satırı (sunucu: AyarFiyati).</summary>
    public sealed record AyarFiyatiAnlik(string AyarId, int Milyem, decimal GramFiyati);

    /// <summary>
    /// VARSAYILAN yoklama aralığı (Kurucu isteği: 10 saniye). Gerçek aralık
    /// <see cref="Aralik"/>'tır: <c>Kur:YoklamaAraligiSaniye</c> yapılandırma
    /// anahtarından okunur (PAKET P-70), geçersiz/eksikse bu değer kullanılır.
    /// </summary>
    public static readonly TimeSpan YoklamaAraligi = TimeSpan.FromSeconds(10);

    /// <summary>PAKET P-70 — yoklama aralığının yapılandırma anahtarı (saniye, 2–300).</summary>
    public const string AralikAnahtari = "Kur:YoklamaAraligiSaniye";

    /// <summary>
    /// PAKET P-70 — saat kaynağı. DI'da <see cref="TimeProvider"/> kayıtlıysa o
    /// (testler sahte saatle 60 sn'yi anında "yaşar"), yoksa sistem saati.
    /// </summary>
    private readonly TimeProvider zaman = sp.GetService<TimeProvider>() ?? TimeProvider.System;

    /// <summary>
    /// PAKET P-70 — yürürlükteki yoklama aralığı (yapılandırılabilir; varsayılan
    /// <see cref="YoklamaAraligi"/>). Push bağlıyken yoklama bu aralıkta TİKLER
    /// ama ağ isteği ATMAZ; aynı değer <see cref="AlAsync"/>'in tazelik penceresidir.
    /// </summary>
    public TimeSpan Aralik { get; } = AralikOku(sp.GetService<IConfiguration>());

    /// <summary>
    /// Yapılandırmadan aralığı okur. 2 sn altı (sunucuyu döver) ve 300 sn üstü
    /// (push koparsa çubuk 5 dakikadan uzun bayat kalır) REDDEDİLİR → varsayılan.
    /// </summary>
    public static TimeSpan AralikOku(IConfiguration? yapilandirma)
    {
        var ham = yapilandirma?[AralikAnahtari];
        return int.TryParse(ham, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sn)
               && sn is >= 2 and <= 300
            ? TimeSpan.FromSeconds(sn)
            : YoklamaAraligi;
    }

    private HubConnection? hub;
    private Task? baslatmaGorevi;
    private CancellationTokenSource? yoklamaIptal;
    private IJSObjectReference? gorunurlukModulu;
    private DotNetObjectReference<CanliKurServisi>? gorunurlukRef;

    /// <summary>PAKET P-70 — son BAŞARILI okumanın (REST ya da push) saat-kaynağı zamanı.</summary>
    private DateTimeOffset? sonBasariliOkuma;

    /// <summary>PAKET P-70 — push kanalının olaylarla izlenen durumu (bkz. <see cref="BaglantiDurumunuIsle"/>).</summary>
    private bool pushBagli;

    /// <summary>En son bilinen kur (push veya yoklamadan).</summary>
    public KurAnlik? Son { get; private set; }

    /// <summary>
    /// Sunucuya en son ne zaman sorulduğu ya da (push bağlıyken) kurun en son ne
    /// zaman CANLI OLARAK TEYİT EDİLDİĞİ (yerel saat) — çubuğun yanında gösterilir.
    /// PAKET P-70: push bağlıyken yoklama ağ isteği atmaz; açık bir WebSocket,
    /// sunucu yeni kur yazarsa haber vereceği anlamına gelir, bu yüzden saat yine
    /// her aralıkta ilerler (çubuk "donmuş" görünmez).
    /// </summary>
    public DateTime? SonSorgu { get; private set; }

    /// <summary>Son sorgu başarısız olduysa sebebi; başarılıysa null.</summary>
    public string? SonHata { get; private set; }

    /// <summary>Canlı bağlantı kurulu mu (WebSocket açık)?</summary>
    public bool Bagli => pushBagli;

    /// <summary>PAKET P-70 — tarayıcı sekmesi görünür mü (visibilitychange)? Gizliyken yoklama duraklar.</summary>
    public bool Gorunur { get; private set; } = true;

    /// <summary>PAKET P-70 — eldeki kur ağ isteği atmadan verilebilir mi?</summary>
    private bool Taze =>
        Son is not null
        && (Bagli || (sonBasariliOkuma is { } t && zaman.GetUtcNow() - t < Aralik));

    public event Action? Degisti;

    /// <summary>Tekil başlatma — birden çok bileşen çağırsa da bir kez çalışır.</summary>
    public Task BaslatAsync() => baslatmaGorevi ??= BaslatIcAsync();

    private async Task BaslatIcAsync()
    {
        // 1) Anında göstermek için son kuru REST'ten al (push beklemeden)
        await TazeleAsync();

        // 2) Kendi yoklama döngüsü — push ÇALIŞMAZSA çubuk ilerler.
        //    PAKET P-70: push bağlıyken döngü ağ isteği ATMAZ (bkz. YoklamaDongusu).
        yoklamaIptal = new CancellationTokenSource();
        _ = YoklamaDongusu(yoklamaIptal.Token);

        // 3) Canlı push kanalına bağlan
        //
        // ⚠ BEKLENMEZ. WebSocket el sıkışması saniyeler sürebilir (proxy,
        // güvenlik duvarı, kopuk hat). Bu görev beklenirse BaslatAsync'i
        // await eden SATIŞ EKRANI o süre boyunca "Yükleniyor"da kalır —
        // tezgâhta müşteri beklerken kabul edilemez. Kur zaten (1)'de
        // REST'ten alındı; push yalnız bir hızlandırmadır, kurulamazsa
        // yoklama işi görür.
        _ = PushKanaliniBaglaAsync();

        // 4) PAKET P-70 — sekme görünürlüğü (ateşle-unut; JS yoksa sekme görünür sayılır).
        _ = GorunurlukIzleAsync();
    }

    private async Task PushKanaliniBaglaAsync()
    {
        try
        {
            hub = new HubConnectionBuilder()
                .WithUrl(nav.ToAbsoluteUri("/hubs/kur"),
                    o => o.AccessTokenProvider = () => Task.FromResult(oturum.AccessToken))
                .WithAutomaticReconnect()
                .Build();

            hub.On<KurAnlik>("KurGuncellendi", PushUygula);

            // PAKET P-70: bağlantı durumu OLAYLARLA izlenir — koptuğu an yoklama devrede.
            hub.Reconnecting += _ => { BaglantiDurumunuIsle(false); return Task.CompletedTask; };
            hub.Closed += _ => { BaglantiDurumunuIsle(false); return Task.CompletedTask; };
            // Yeniden bağlanınca aradaki kaçırılan güncellemeyi REST ile telafi et
            hub.Reconnected += async _ =>
            {
                await TazeleAsync();
                BaglantiDurumunuIsle(true);
            };

            await hub.StartAsync();
            BaglantiDurumunuIsle(hub.State == HubConnectionState.Connected);
        }
        catch { /* bağlanamadıysa yoklama devreye girer; reconnect denemeye devam eder */ }
    }

    /// <summary>
    /// PAKET P-70 — push kanalı durumu değişti. Hub olayları (StartAsync sonrası,
    /// Reconnecting, Reconnected, Closed) çağırır. Kopuşta son kur o an REST'ten
    /// yenilenir (aradaki kaçırılmış güncelleme) ve yoklama bir sonraki tikten
    /// itibaren ağ isteği atmaya başlar.
    /// </summary>
    public void BaglantiDurumunuIsle(bool bagli)
    {
        if (pushBagli == bagli) return;
        pushBagli = bagli;
        api.Onbellek.KurCanliBagli = bagli;   // ISTEK-1: push bağlıyken sayfalar ek çekim yapmaz
        // Kopunca kayıt düşer (artık kimse itmiyor). Bağlanınca kayıt yalnız
        // yoklama aralığından eskiyse düşer: bağlantı kurulmadan hemen önce
        // çekilmiş kur, P-70'in "taze" tanımıyla (Aralik) zaten tazedir.
        if (bagli) api.Onbellek.EskiyseDusur(ListeOnbellegi.KurYolu, Aralik);
        else api.Onbellek.Dusur(ListeOnbellegi.KurYolu);
        if (!bagli && oturum.GirisYapildi && Gorunur) _ = TazeleAsync();
        Degisti?.Invoke();
    }

    /// <summary>PAKET P-70 — js/kur-gorunurluk.js modülüyle visibilitychange olayına abone olur.</summary>
    private async Task GorunurlukIzleAsync()
    {
        try
        {
            var js = sp.GetService<IJSRuntime>();
            if (js is null) return;
            gorunurlukModulu = await js.InvokeAsync<IJSObjectReference>("import", "./js/kur-gorunurluk.js");
            gorunurlukRef = DotNetObjectReference.Create(this);
            await gorunurlukModulu.InvokeVoidAsync("izle", gorunurlukRef);
        }
        catch { /* JS yoksa (ön-işleme/test) sekme görünür sayılır: eski davranış */ }
    }

    /// <summary>
    /// PAKET P-70 — sekme görünürlüğü değişti (JS'ten). Gizliyken yoklama ağ
    /// isteği atmaz; görünür olunca eldeki kur bayatsa ve push yoksa hemen tazelenir.
    /// </summary>
    [JSInvokable]
    public void GorunurlukDegisti(bool gorunur)
    {
        Gorunur = gorunur;
        if (gorunur && !Taze && oturum.GirisYapildi) _ = TazeleAsync();
    }

    /// <summary>
    /// Aralıkta bir tikler (oturum kapanınca durur). PAKET P-70: push bağlıyken ya
    /// da sekme gizliyken AĞ İSTEĞİ ATMAZ; yalnız bağlantı kopuk/yeniden
    /// bağlanıyorken /kur/guncel okur.
    /// </summary>
    private async Task YoklamaDongusu(CancellationToken iptal)
    {
        using var zamanlayici = new PeriodicTimer(Aralik, zaman);
        try
        {
            while (await zamanlayici.WaitForNextTickAsync(iptal))
            {
                if (!oturum.GirisYapildi) continue;   // çıkış yapıldıysa boşa sorma
                if (!Gorunur) continue;               // P-70: sekme gizli → duraklat
                if (Bagli)                             // P-70: push canlı → istek YOK
                {
                    SonSorgu = DateTime.Now;           // canlı kanal teyidi (çubuk donmaz)
                    Degisti?.Invoke();
                    continue;
                }
                await TazeleAsync();
            }
        }
        catch (OperationCanceledException) { /* sayfa kapandı */ }
    }

    /// <summary>
    /// PAKET P-70 — TEK PAYLAŞILAN OKUMA NOKTASI. Bileşenler kendi /kur/guncel
    /// isteklerini atmak yerine bunu çağırır.
    /// <list type="bullet">
    /// <item><c>zorla=false</c>: eldeki kur tazeyse (push bağlı ya da son başarılı
    /// okuma <see cref="Aralik"/>'tan genç) AĞ İSTEĞİ ATMAZ.</item>
    /// <item><c>zorla=true</c> ("Yenile"): HER ÇAĞRIDA gerçek istek atar — Yenile yalancı olmaz.</item>
    /// </list>
    /// Hata YUTULMAZ (UX-S28): istek başarısızsa <see cref="ApiHata"/> fırlar,
    /// <see cref="SonHata"/> da güncellenir.
    /// </summary>
    public async Task<KurAnlik> AlAsync(bool zorla = false)
    {
        if (!zorla && Taze) return Son!;
        return await CekAsync();
    }

    /// <summary>Son kuru sunucudan çek (ilk yükleme + yoklama + reconnect telafisi). Hata yutulur, <see cref="SonHata"/>'ya yazılır.</summary>
    public async Task TazeleAsync()
    {
        try { await CekAsync(); }
        catch { /* CekAsync SonHata'yı yazdı ve aboneleri bilgilendirdi */ }
    }

    /// <summary>Tek ağ yolu: /kur/guncel okur; başarısızlıkta SonHata'yı yazar ve FIRLATIR.</summary>
    private async Task<KurAnlik> CekAsync()
    {
        try
        {
            // PAKET ISTEK-1: bu servis kurun TEK yetkili okuyucusudur; karar
            // verip çektiğinde (ilk yükleme, push kopukken yoklama, "Yenile")
            // liste önbelleğini ATLAR — "Yenile" yalancı olmaz.
            var kur = await api.TazeGetAsync<KurAnlik>(ListeOnbellegi.KurYolu)
                      ?? throw new ApiHata(404, "Kur alınamadı");
            SonHata = null;
            Uygula(kur);
            return kur;
        }
        catch (ApiHata ex)
        {
            // Kur hiç yoksa (404) ya da yetki/ağ hatasında son bilinen değer korunur
            SonHata = ex.Message;
            SonSorgu = DateTime.Now;
            Degisti?.Invoke();
            throw;
        }
        catch (Exception)
        {
            SonHata = "Kur alınamadı";
            SonSorgu = DateTime.Now;
            Degisti?.Invoke();
            throw new ApiHata(0, SonHata);
        }
    }

    /// <summary>
    /// PAKET ISTEK-1 — canlı kanaldan (SignalR) yeni kur geldi: sayfaların
    /// /kur/guncel önbellek kaydı düşer, bir sonraki okuma tazedir. Push
    /// bağlıyken sayfalar kısa süre önbellekten okur, ek çekim yapmaz.
    /// </summary>
    public void PushUygula(KurAnlik kur)
    {
        api.Onbellek.Dusur(ListeOnbellegi.KurYolu);
        Uygula(kur);
    }

    /// <summary>
    /// PAKET ISTEK-1 (GOZ #19/#48/#54) — saat rozetinin kırmızı olması için
    /// kurun GERÇEKTEN bayat olması gerekir: son sorgu hatalı VE elde taze kur
    /// yok (push kopuk ve son başarılı okuma <see cref="BayatEsigi"/>'nden eski
    /// ya da hiç kur yok). Tek bir geçici hata (ör. yoğunluk) varken kanal
    /// canlıysa ya da kur hâlâ tazeyse saat kırmızıya dönmez; sebep yine
    /// ipucunda yazar (UX-S28 — hata gizlenmez, telaş da yaratılmaz).
    /// </summary>
    public bool Bayat =>
        SonHata is not null
        && (Son is null
            || !(Bagli || (sonBasariliOkuma is { } t && zaman.GetUtcNow() - t < BayatEsigi)));

    /// <summary>Kur bu süreden eskiyse (ve son sorgu hatalıysa) bayat sayılır: üç yoklama aralığı.</summary>
    public TimeSpan BayatEsigi => Aralik * 3;

    /// <summary>Yeni kuru uygula ve aboneleri bilgilendir (push, yoklama veya elle çekim).</summary>
    public void Uygula(KurAnlik kur)
    {
        Son = kur;
        SonSorgu = DateTime.Now;
        sonBasariliOkuma = zaman.GetUtcNow();
        Degisti?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (yoklamaIptal is not null)
        {
            await yoklamaIptal.CancelAsync();
            yoklamaIptal.Dispose();
        }
        if (hub is not null) await hub.DisposeAsync();
        await GorunurlukBirakAsync();
    }

    /// <summary>PAKET P-70 — visibilitychange aboneliğini bırakır (hata yutulur: sayfa kapanıyor olabilir).</summary>
    private async Task GorunurlukBirakAsync()
    {
        try
        {
            if (gorunurlukModulu is not null)
            {
                await gorunurlukModulu.InvokeVoidAsync("birak");
                await gorunurlukModulu.DisposeAsync();
            }
        }
        catch { /* JS bağlamı gitmiş olabilir (sayfa kapanışı) */ }
        gorunurlukModulu = null;
        gorunurlukRef?.Dispose();
        gorunurlukRef = null;
    }

    /// <summary>
    /// Senkron kapatma. Yalnız IAsyncDisposable uygulayan bir tür, senkron
    /// kapatılan bir DI kapsabına konduğunda InvalidOperationException atar.
    /// Yoklama burada kesin durdurulur; hub kapatması ateşle-unut.
    /// </summary>
    public void Dispose()
    {
        yoklamaIptal?.Cancel();
        yoklamaIptal?.Dispose();
        yoklamaIptal = null;
        if (hub is not null) _ = hub.DisposeAsync();
        hub = null;
        _ = GorunurlukBirakAsync();
    }
}
