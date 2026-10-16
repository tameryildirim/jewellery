namespace Kuyumcu.Web.Servisler;

/// <summary>
/// Kabuk durumu — dar ekranda (≤768px) kenar çubuğu çekmecesinin açık/kapalı
/// hâli. UstBar'daki hamburger (<c>#ust-menu-ac</c>), AnaYerlesim'deki örtü
/// (<c>#kenar-ortu</c>) ve KenarCubugu aynı durumu paylaşır.
/// <para>
/// ⚠ Bu bir TERCİH/GÖRÜNÜM durumudur; hiçbir yetki kapısına bağlı değildir.
/// Menü durumu sunucuya gitmez, kalıcı değildir (gezinince kapanır). Görünüm
/// şubesi (ŞUBE-KAPSAM-B) ise kullanıcı tercihi olarak sunucuya yazılır.
/// </para>
/// </summary>
public sealed class KabukServisi
{
    /// <summary>
    /// Paylaşılan tek örnek.
    /// <para>
    /// ⚠ NEDEN DI DEĞİL: UstBar ve KenarCubugu'na <c>@inject</c> ile girseydi,
    /// bu iki bileşeni çizen MEVCUT bUnit testleri (RotaBaslikTestleri,
    /// BasitModMenuTestleri, BildirimEkranTestleri) kayıt eksikliğinden
    /// kırılırdı. A2'nin kabul ölçütü ise "mevcut testler DEĞİŞTİRİLMEDEN
    /// yeşil"dir (K8-a). Durum yalnız bir görünüm bayrağıdır: sunucuya
    /// gitmez, yetkiye dokunmaz, kalıcı değildir.
    /// </para>
    /// </summary>
    public static KabukServisi Ortak { get; } = new();

    public bool MenuAcik { get; private set; }

    // ── PAKET ŞUBE-KAPSAM-B (§ 4.3) — GÖRÜNÜM ŞUBESİ ─────────────────────────
    //
    // P-15'in "AramaSubesi"si GorunumSubesi olarak YENİDEN ADLANDIRILDI ve artık
    // yalnız aramayı değil BÜTÜN EKRANI yönetir: Panel, Kasalar, Hurda, POS,
    // Emanet, stok/kâr-zarar/işlem defteri/kasa raporu çağrıları bu değeri
    // SubeliSorgu ile `?subeId=` olarak sunucuya geçirir (tek kaynak — arama
    // da aynı değeri okur).
    //
    // ⚠ BU BİR YETKİ DEĞİLDİR. Kapsamın tabanı P-04 global sorgu filtresidir;
    //   sunucu her uçta GorunumSubesi.Coz ile doğrular (kilitli rol başka şube
    //   isterse 403, pasif/yabancı şube 404). Değer yalnız DARALTIR.
    //
    // KALICILIK: seçim `PATCH /tercihler/benim {"gorunum-subesi": …}` ile
    //   SUNUCUYA yazılır (sayfa yenilenince ve başka bilgisayarda kalır — K-7/K-8).
    //   Açılışta `GET /tercihler/benim` → `gorunumSubesi` okunur. Yazma
    //   başarısızsa yerel değer GERİ ALINIR ve sebep döner — "görünen ≠
    //   kaydedilen" olamaz (K-9).
    //
    // SAHİPLİK: durum, onu kuran OturumServisi örneğine + kullanıcı kimliğine
    //   bağlıdır. Tarayıcıda tek oturum vardır; bUnit'te ise her test bağlamının
    //   kendi OturumServisi'si olduğundan paralel test sınıfları birbirinin
    //   görünüm şubesini GÖREMEZ (statik tekilin sızıntı kapısı). Başka
    //   kullanıcıyla giriş yapılınca eski durum kullanılmaz (anahtar değişir).
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<OturumServisi, GorunumDurumu> durumlar = new();

    /// <summary>En son kurulan/değişen durum — arama bileşenlerinin okuduğu genel değer.</summary>
    private GorunumDurumu? son;

    /// <summary>
    /// Görünüm şubesi (null = tüm şubeler / daraltma yok). Arama bileşenleri
    /// (GlobalArama, KayitSecici, Secici) bunu `/arama?sube=` olarak geçirir.
    /// </summary>
    public string? GorunumSubesi => son?.SubeId;

    /// <summary>Görünüm şubesi DEĞİŞTİ (sahip = durumun OturumServisi örneği).</summary>
    public event Action<object>? GorunumDegisti;

    /// <summary>
    /// Görünüm DURUMU değişti (okundu, şube listesi geldi, seçim/geri alma) — yalnız
    /// yeniden çizim için. ⚠ Ortak <see cref="Degisti"/> olayı KULLANILMAZ: o olay
    /// statik tekilde bütün kabuk bileşenlerini çizdirir; paralel bUnit bağlamlarında
    /// başka testin bileşenini yeniden çizip olay kimliklerini bayatlatırdı.
    /// </summary>
    public event Action<object>? GorunumDurumuDegisti;

    /// <summary>Bu oturuma ait görünüm durumu; yoksa (henüz okunmadı / başka oturum) null.</summary>
    public GorunumDurumu? GorunumIcin(OturumServisi oturum)
        => durumlar.TryGetValue(oturum, out var g) && g.Anahtar == Anahtar(oturum) ? g : null;

    /// <summary>Sunucuya gidecek görünüm şubesi — yalnız tüm-şube yetkili kullanıcıda dolu.</summary>
    public string? GorunumSubesiIcin(OturumServisi oturum)
        => GorunumIcin(oturum) is { TumSubeler: true } g ? g.SubeId : null;

    private static string? Anahtar(OturumServisi oturum)
        => oturum.Kullanici is { } k ? $"{k.Id}|{k.Rol}|{k.SubeId}" : null;

    /// <summary>
    /// Tercihi okur (oturum başına BİR kez; uçuştaki okuma paylaşılır). Kullanıcı
    /// henüz yoksa hiçbir şey yapmaz. Okuma düşerse seçici GİZLİ kalır (tutucu
    /// taraf: sunucunun "tüm şube" dediğini görmeden seçim vaat edilmez) ve
    /// sebep <see cref="GorunumDurumu.Hata"/>'da durur.
    /// </summary>
    public Task GorunumHazirla(ApiIstemci api, OturumServisi oturum)
    {
        var anahtar = Anahtar(oturum);
        if (anahtar is null) return Task.CompletedTask;
        if (GorunumIcin(oturum) is { } mevcut) return mevcut.Yukleme;
        var yeni = new GorunumDurumu(oturum, anahtar);
        durumlar.AddOrUpdate(oturum, yeni);
        son = yeni;
        yeni.Yukleme = GorunumOku(api, yeni);
        return yeni.Yukleme;
    }

    /// <summary>
    /// Sayfa yenilemede profil (<c>/auth/ben</c>) gelmeden açılan sayfanın en çok
    /// bu kadar bekleyeceği süre — sonra daraltmasız devam eder (sunucunun varsayılanı).
    /// </summary>
    public static readonly TimeSpan ProfilBeklemeSiniri = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Uçuştaki okuma (varsa) — sayfalar ilk isteklerinden önce bekler.
    /// <para>
    /// ⚠ SAYFA YENİLEME (canlı ölçüm, 01.10): jeton var ama profil henüz okunmamışken
    /// (AnaYerlesim <c>/auth/ben</c>) sayfa ilk isteğini atarsa tercih okuması daha
    /// BAŞLAMAMIŞTIR → istek daraltmasız gider, tercih gelince ikinci kez seçili şubeyle
    /// gider ("önce birleşik, sonra şube" titremesi). Bu yüzden önce profil beklenir;
    /// profil gelince SubeSecici aynı olayda okumayı başlatır, burada o okuma beklenir.
    /// Jeton yoksa (bUnit, çıkış) BEKLEME YOK.
    /// </para>
    /// </summary>
    public async Task GorunumBekle(OturumServisi oturum)
    {
        if (oturum.GirisYapildi && oturum.Kullanici is null)
        {
            var geldi = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Dinle() { if (oturum.Kullanici is not null) geldi.TrySetResult(); }
            oturum.Degisti += Dinle;
            try
            {
                if (oturum.Kullanici is null)
                    await Task.WhenAny(geldi.Task, Task.Delay(ProfilBeklemeSiniri));
            }
            finally { oturum.Degisti -= Dinle; }
        }
        if (GorunumIcin(oturum) is { } g) await g.Yukleme;
    }

    private async Task GorunumOku(ApiIstemci api, GorunumDurumu d)
    {
        var onceki = d.SubeId;
        try
        {
            var y = await api.GetAsync<System.Text.Json.JsonElement>("/tercihler/benim");
            GorunumYanitiUygula(d, y);
            d.Hata = null;
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException or HttpRequestException)
        {
            // UX-S28: okuma hatası "seçim yok"a ÇEVRİLMEZ — sebep durur, seçici
            // gizli kalır, istekler daraltmasız (sunucunun kendi varsayılanı) gider.
            d.Hata = ex.Message;
        }
        d.Yuklendi = true;
        if (d.Sahip is OturumServisi o && ReferenceEquals(GorunumIcin(o), d))
        {
            GorunumDurumuDegisti?.Invoke(d.Sahip);
            if (onceki != d.SubeId) GorunumDegisti?.Invoke(d.Sahip);
        }
        if (d.GecersizTercih) await GecersizTercihiTemizle(api, d);
    }

    /// <summary>
    /// ŞUBE-KAPSAM-B tur 3 (doğrulayıcı YÜKSEK — "kapanan şube bandı her ekranda kalıcı"):
    /// kayıtlı tercih kapanmış bir şubeyi gösteriyorsa sunucunun ÇÖZDÜĞÜ değer
    /// (kendi şube ya da tüm şubeler) tercihe BİR KEZ yazılır → bant yalnız bu
    /// açılışta görünür, bir sonraki açılışta çıkmaz. Yazım düşerse bant yine
    /// kullanıcının "Kapat" düğmesiyle kapanır; hata sessiz değildir — sebep
    /// <see cref="GorunumDurumu.TemizlemeHatasi"/>'nda durur ve bantta yazar.
    /// Yerel bayrak burada İNDİRİLMEZ: kullanıcı neden şube değiştiğini görmelidir.
    /// </summary>
    private async Task GecersizTercihiTemizle(ApiIstemci api, GorunumDurumu d)
    {
        try
        {
            await api.PatchAsync<System.Text.Json.JsonElement>("/tercihler/benim",
                new { tercihler = new Dictionary<string, string?> { ["gorunum-subesi"] = d.SubeId ?? TumSubelerDegeri } });
            d.TemizlemeHatasi = null;
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException or HttpRequestException)
        {
            d.TemizlemeHatasi = ex.Message;
            GorunumDurumuDegisti?.Invoke(d.Sahip);
        }
    }

    /// <summary>
    /// ŞUBE-KAPSAM-B tur 3: kapanmış-şube bandının "Kapat" düğmesi — yalnız yerel
    /// bayrağı indirir (tercih zaten açılışta temizlendi). Bant bütün sayfalarda kalkar.
    /// </summary>
    public void GecersizBandiKapat(OturumServisi oturum)
    {
        if (GorunumIcin(oturum) is not { GecersizTercih: true } d) return;
        d.GecersizTercih = false;
        GorunumDurumuDegisti?.Invoke(d.Sahip);
    }

    /// <summary>
    /// Sunucunun çözdüğü tercih (<c>gorunumSubesi: {subeId, tumSubeler,
    /// gecersizTercih}</c>) duruma işlenir. Alan yoksa (eski sunucu) tutucu taraf:
    /// tüm-şube yetkisi YOK sayılır, seçici çizilmez.
    /// </summary>
    private static void GorunumYanitiUygula(GorunumDurumu d, System.Text.Json.JsonElement y)
    {
        if (y.ValueKind != System.Text.Json.JsonValueKind.Object
            || !y.TryGetProperty("gorunumSubesi", out var g)
            || g.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            d.TumSubeler = false;
            d.SubeId = null;
            d.GecersizTercih = false;
            return;
        }
        d.TumSubeler = g.TryGetProperty("tumSubeler", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.True;
        d.GecersizTercih = g.TryGetProperty("gecersizTercih", out var gt) && gt.ValueKind == System.Text.Json.JsonValueKind.True;
        var sube = g.TryGetProperty("subeId", out var s) && s.ValueKind == System.Text.Json.JsonValueKind.String
            ? s.GetString() : null;
        // Şubeye kilitli rolde istemci daraltma GÖNDERMEZ: sunucu oturum
        // şubesini zaten uygular (gönderilseydi aynı şube = 200, fark yok).
        d.SubeId = d.TumSubeler && !string.IsNullOrWhiteSpace(sube) ? sube : null;
    }

    /// <summary>
    /// Görünüm şubesini seçer ve SUNUCUYA yazar. Yerel değer HEMEN değişir (seçici
    /// beklemeden kapanır); yazma başarısızsa ESKİ değere geri dönülür, olay
    /// yeniden atılır ve hata metni döner (çağıran HataBandi'nda gösterir — K-9).
    /// Başarıda sunucunun normalize ettiği değer esas alınır. null/boş = tüm şubeler.
    /// </summary>
    public async Task<string?> GorunumSubesiAtaAsync(ApiIstemci api, OturumServisi oturum, string? subeId)
    {
        var yeni = string.IsNullOrWhiteSpace(subeId) ? null : subeId;
        var d = GorunumIcin(oturum);
        if (d is null) return null;               // tercih okunmadan seçim yok
        if (d.SubeId == yeni && !d.GecersizTercih) return null;
        var eski = d.SubeId;
        var eskiGecersiz = d.GecersizTercih;
        var nesil = ++d.Nesil;
        d.SubeId = yeni;
        d.GecersizTercih = false;
        son = d;
        GorunumDurumuDegisti?.Invoke(d.Sahip);
        GorunumDegisti?.Invoke(d.Sahip);
        try
        {
            var y = await api.PatchAsync<System.Text.Json.JsonElement>("/tercihler/benim",
                new { tercihler = new Dictionary<string, string?> { ["gorunum-subesi"] = yeni ?? TumSubelerDegeri } });
            if (nesil != d.Nesil) return null;   // daha yeni seçim var — eski yanıt atılır
            var once = d.SubeId;
            if (y.ValueKind == System.Text.Json.JsonValueKind.Object && y.TryGetProperty("gorunumSubesi", out _))
                GorunumYanitiUygula(d, y);
            if (once != d.SubeId) { GorunumDurumuDegisti?.Invoke(d.Sahip); GorunumDegisti?.Invoke(d.Sahip); }
            return null;
        }
        catch (Exception ex) when (ex is ApiHata or System.Text.Json.JsonException or HttpRequestException)
        {
            // Daha yeni bir seçim yazılıyorsa eski yazımın hatası onu geri ALMAZ.
            if (nesil != d.Nesil) return null;
            d.SubeId = eski;
            d.GecersizTercih = eskiGecersiz;
            GorunumDurumuDegisti?.Invoke(d.Sahip);
            GorunumDegisti?.Invoke(d.Sahip);
            return ex.Message;
        }
    }

    /// <summary>Sunucu tercih değeri: tüm şubeler (TercihModulu / GorunumSubesi.Tumu ile aynı).</summary>
    public const string TumSubelerDegeri = "TUMU";

    /// <summary>
    /// Yalnız YEREL atama (sunucuya yazılmaz) — kalıcılığı olmayan test/teşhis
    /// yolu. Ekran seçimi <see cref="GorunumSubesiAtaAsync"/>'i kullanır.
    /// null = durumu boşaltır.
    /// </summary>
    public void GorunumSubesiYerelAta(string? subeId)
    {
        var yeni = string.IsNullOrWhiteSpace(subeId) ? null : subeId;
        // ⚠ Yalnız GENEL (arama) değeri etkiler; oturum durumlarına DOKUNMAZ —
        //   paralel test sınıfının kurucusundaki sıfırlama başka testin durumunu ezemez.
        if (son?.SubeId == yeni) return;
        son = yeni is null ? null : new GorunumDurumu(new object(), null) { SubeId = yeni };
        // Olay ATILMAZ: genel değeri okuyan Secici her aramada değeri yeniden okur.
    }

    /// <summary>SubeSecici şube listesini okuyunca rozet adları buradan okur.</summary>
    public void SubeListesiAta(OturumServisi oturum, IReadOnlyList<Kuyumcu.Core.Sozlesmeler.SubeOzeti> subeler)
    {
        if (GorunumIcin(oturum) is not { } d) return;
        d.Subeler = subeler;
        GorunumDurumuDegisti?.Invoke(d.Sahip);
    }

    public event Action? Degisti;

    public void Ac() => Ata(true);
    public void Kapat() => Ata(false);
    public void Degistir() => Ata(!MenuAcik);

    private void Ata(bool acik)
    {
        if (MenuAcik == acik) return;
        MenuAcik = acik;
        Degisti?.Invoke();
    }
}

/// <summary>
/// PAKET ŞUBE-KAPSAM-B — görünüm şubesinin oturumdaki hâli. Sahip = durumu
/// kuran OturumServisi örneği (paralel bUnit sınıfları arasında sızıntı yok).
/// </summary>
public sealed class GorunumDurumu(object sahip, string? anahtar)
{
    internal object Sahip { get; } = sahip;
    internal string? Anahtar { get; } = anahtar;
    internal Task Yukleme { get; set; } = Task.CompletedTask;

    /// <summary>
    /// ŞUBE-KAPSAM-B tur 2 (yarış): her seçim yazımı nesli bir artırır; geç gelen
    /// ESKİ yazımın yanıtı/hatası yeni seçimi ezmez (A→B hızlı geçişte A'nın hatası
    /// "A öncesine" dönmesin).
    /// </summary>
    internal int Nesil { get; set; }

    /// <summary>Seçili şube (null = tüm şubeler).</summary>
    public string? SubeId { get; internal set; }

    /// <summary>Sunucunun tek kaynağından (SubeYetkisi): kullanıcı tüm şubeleri görebilir mi?</summary>
    public bool TumSubeler { get; internal set; }

    /// <summary>Kayıtlı tercih geçersizdi (pasif/silinmiş şube) — oturum şubesine dönüldü.</summary>
    public bool GecersizTercih { get; internal set; }

    /// <summary>Geçersiz tercihin sunucuda temizlenememe sebebi (tur 3; bantta yazar).</summary>
    public string? TemizlemeHatasi { get; internal set; }

    /// <summary>Tercih okundu mu (başarılı ya da hatalı).</summary>
    public bool Yuklendi { get; internal set; }

    /// <summary>Tercih okunamadıysa sebep (UX-S28).</summary>
    public string? Hata { get; internal set; }

    /// <summary>Aktif şubeler (SubeSecici okur; rozet adı buradan).</summary>
    public IReadOnlyList<Kuyumcu.Core.Sozlesmeler.SubeOzeti> Subeler { get; internal set; } = [];

    /// <summary>Seçim anlamlı mı: tüm-şube yetkisi VE birden çok aktif şube (UX-S22).</summary>
    public bool CokluGorunum => TumSubeler && Subeler.Count > 1;
}
