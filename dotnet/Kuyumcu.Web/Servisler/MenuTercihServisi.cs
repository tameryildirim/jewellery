using System.Text.Json;

namespace Kuyumcu.Web.Servisler;

/// <summary>
/// PAKET P-37 — KULLANICIYA ÖZEL MENÜ TERCİHLERİ (favoriler · grup içi sıra ·
/// karşılama ekranı kutucuk düzeni).
///
/// ⚠ NEDEN SUNUCUDA, NEDEN localStorage DEĞİL: kurucu "kullanıcıya özel" dedi.
///   Tarayıcı deposu CİHAZA özeldir: aynı kullanıcı ikinci bilgisayarda
///   favorilerini kaybeder, aynı cihazı paylaşan iki tezgâhtar ise birbirinin
///   favorisini görür. İkisi de istenenin tersidir. Kaynak: /tercihler/benim
///   (kullanıcı kimliği JETONDAN gelir, gövdeden değil).
/// ⚠ Yazma başarısız olursa SESSİZ KALINMAZ: <see cref="Hata"/> dolar, ekran
///   bant çizer ve yerel değişiklik GERİ ALINIR (md.1 — "oldu" ancak kanıtla).
/// </summary>
public sealed class MenuTercihServisi(ApiIstemci api, OturumServisi oturum)
{
    public const string AnahtarFavoriler = "menu-favoriler";
    public const string AnahtarSira      = "menu-sirasi";
    public const string AnahtarPanel     = "panel-duzeni";
    /// <summary>PAKET S1-b — kenar çubuğunda AÇIK olan grupların slug listesi.</summary>
    public const string AnahtarAcikGruplar = "menu-acik-gruplar";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private string? yuklenenKullaniciId;
    private HashSet<string> favoriler = [];
    private Dictionary<string, List<string>> sira = [];
    private PanelDuzeni panel = new([], []);
    /// <summary>
    /// PAKET S1-b — açık grupların slug'ları. <c>null</c> = kullanıcı HİÇ
    /// dokunmamış; o zaman varsayılanı EKRAN seçer (aktif rotanın grubu açılır).
    /// Boş küme ise "hepsini kapattım" demektir ve null'dan FARKLIDIR.
    /// </summary>
    private HashSet<string>? acikGruplar;

    public event Action? Degisti;

    /// <summary>Son yazma/okuma hatası — ekranda bant olarak görünür (UX-S28).</summary>
    public string? Hata { get; private set; }

    /// <summary>
    /// PAKET S1-b — son başarısız yazmanın ANAHTARI. Kenar çubuğu bandı buna
    /// bakarak "sıra kaydedilemedi" ile "grup katlaması kaydedilemedi"yi ayırır;
    /// katlama hatasına "sıra" demek kullanıcıya yanlış şey söylemektir.
    /// </summary>
    public string? HataAnahtari { get; private set; }

    public bool Yuklendi => yuklenenKullaniciId is not null;

    public IReadOnlyCollection<string> Favoriler => favoriler;
    public bool Favori(string yol) => favoriler.Contains(yol);

    /// <summary>Karşılama ekranı kutucuk düzeni: sıra + gizlenenler.</summary>
    public PanelDuzeni Panel => panel;

    /// <summary>
    /// PAKET S1-b — açık grupların slug kümesi; kullanıcı hiç katlama yapmadıysa
    /// <c>null</c>. Ekran null'ı "aktif rotanın grubu açık" diye yorumlar.
    /// </summary>
    public IReadOnlyCollection<string>? AcikGruplar => acikGruplar;

    /// <summary>
    /// PAKET S1-g — kullanıcının kayıtlı bir menü SIRASI var mı? Kenar çubuğu
    /// "Sırayı sıfırla" düğmesini YALNIZ o zaman çizer: silinecek sıra yokken
    /// düğme etkisizdir (DoD EK md.1) ve tuvalde menüde böyle bir düğme yoktur.
    /// </summary>
    public bool OzelSiraVar => sira.Values.Any(l => l.Count > 0);

    // ── Okuma ────────────────────────────────────────────────────────────────

    public async Task HazirlaAsync()
    {
        var kid = oturum.Kullanici?.Id;
        if (string.IsNullOrEmpty(kid) || kid == yuklenenKullaniciId) return;
        try
        {
            var cevap = await api.GetAsync<TercihCevap>("/tercihler/benim");
            var t = cevap.Tercihler ?? [];
            favoriler = [.. Coz<string[]>(t, AnahtarFavoriler) ?? []];
            sira = Coz<Dictionary<string, List<string>>>(t, AnahtarSira) ?? [];
            panel = Coz<PanelDuzeni>(t, AnahtarPanel) ?? new([], []);
            var acik = Coz<string[]>(t, AnahtarAcikGruplar);
            acikGruplar = acik is null ? null : [.. acik];
            sunucudakiAcikGruplar = acik is null ? null : [.. acik];
            yuklenenKullaniciId = kid;
            Hata = null;
            HataAnahtari = null;
        }
        catch (Exception ex)
        {
            // Tercih okunamadıysa VARSAYILAN düzen çizilir — menü BOŞALMAZ.
            Hata = ex.Message;
        }
        Degisti?.Invoke();
    }

    private static T? Coz<T>(Dictionary<string, string> t, string anahtar)
    {
        if (!t.TryGetValue(anahtar, out var ham) || string.IsNullOrWhiteSpace(ham)) return default;
        try { return JsonSerializer.Deserialize<T>(ham, Json); }
        catch (JsonException) { return default; }   // bozuk tercih → varsayılan
    }

    // ── Sıralama ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Bir grubun öğelerini kullanıcının sırasına göre dizer.
    /// ⚠ Kayıtlı sırada OLMAYAN öğe (yeni açılan ekran) KAYBOLMAZ — sonda kalır.
    /// ⚠ Kayıtlı sırada olup artık görünmeyen öğe sessizce atlanır.
    /// </summary>
    public List<T> SiraUygula<T>(string grupSlug, List<T> ogeler, Func<T, string> anahtar)
    {
        if (!sira.TryGetValue(grupSlug, out var kayitli) || kayitli.Count == 0) return ogeler;
        var indeks = kayitli
            .Select((y, i) => (y, i))
            .ToDictionary(p => p.y, p => p.i);
        return [.. ogeler.OrderBy(o => indeks.TryGetValue(anahtar(o), out var i) ? i : int.MaxValue)];
    }

    /// <summary>Grup içinde bir öğeyi kaydır (klavye Alt+↑/↓ ve sürükle-bırak ortak yolu).</summary>
    public Task TasiAsync(string grupSlug, IReadOnlyList<string> gorunenSira, string yol, int yon)
    {
        var liste = gorunenSira.ToList();
        var i = liste.IndexOf(yol);
        if (i < 0) return Task.CompletedTask;
        var hedef = i + yon;
        if (hedef < 0 || hedef >= liste.Count) return Task.CompletedTask;   // sınırda: sessiz dur
        (liste[i], liste[hedef]) = (liste[hedef], liste[i]);
        return SirayiYazAsync(grupSlug, liste);
    }

    /// <summary>Sürükle-bırak bırakma noktası: kaynağı hedefin yerine yerleştirir.</summary>
    public Task BirakAsync(string grupSlug, IReadOnlyList<string> gorunenSira, string kaynak, string hedef)
    {
        if (kaynak == hedef) return Task.CompletedTask;
        var liste = gorunenSira.ToList();
        var i = liste.IndexOf(kaynak);
        var j = liste.IndexOf(hedef);
        if (i < 0 || j < 0) return Task.CompletedTask;
        liste.RemoveAt(i);
        liste.Insert(j, kaynak);
        return SirayiYazAsync(grupSlug, liste);
    }

    private async Task SirayiYazAsync(string grupSlug, List<string> liste)
    {
        var onceki = sira.TryGetValue(grupSlug, out var e) ? new List<string>(e) : null;
        sira[grupSlug] = liste;
        Degisti?.Invoke();
        await YazAsync(AnahtarSira, JsonSerializer.Serialize(sira, Json), () =>
        {
            if (onceki is null) sira.Remove(grupSlug); else sira[grupSlug] = onceki;
        });
    }

    /// <summary>"Sırayı sıfırla" — sunucudaki tercihi SİLER (varsayılana döner).</summary>
    public async Task SirayiSifirlaAsync()
    {
        var onceki = sira;
        sira = [];
        Degisti?.Invoke();
        await SilAsync(AnahtarSira, () => sira = onceki);
    }

    // ── Favoriler ────────────────────────────────────────────────────────────

    public async Task FavoriDegistirAsync(string yol)
    {
        var onceki = new HashSet<string>(favoriler);
        if (!favoriler.Remove(yol)) favoriler.Add(yol);
        Degisti?.Invoke();
        await YazAsync(AnahtarFavoriler, JsonSerializer.Serialize(favoriler.ToArray(), Json),
            () => favoriler = onceki);
    }

    // ── Grup katlama (PAKET S1-b) ────────────────────────────────────────────

    /// <summary>
    /// Bir grubun açık olup olmadığı. Kayıtlı tercih YOKSA <c>null</c> döner —
    /// ekran o zaman varsayılanı (aktif rotanın grubu) uygular. Tahmin YAPILMAZ.
    /// </summary>
    public bool? GrupAcik(string grupSlug) => acikGruplar?.Contains(grupSlug);

    /// <summary>
    /// Grubu açar/kapatır ve sunucuya yazar. İlk katlamada kayıtlı küme
    /// <paramref name="varsayilanAcik"/> ile TOHUMLANIR: böylece kullanıcı tek
    /// bir grubu kapattığında o ana kadar ekranda AÇIK duran gruplar açık kalır
    /// (aksi hâlde ilk tıklamada bütün menü kapanırdı).
    /// </summary>
    public Task GrupKatlamaDegistirAsync(string grupSlug, IEnumerable<string> varsayilanAcik)
    {
        var yeni = acikGruplar is null ? [.. varsayilanAcik] : new HashSet<string>(acikGruplar);
        if (!yeni.Remove(grupSlug)) yeni.Add(grupSlug);
        acikGruplar = yeni;
        katlamaSurumu++;
        Degisti?.Invoke();
        // PAKET KATMAN-2 — tek yazıcı: bir yazma sürerken gelen tıklama yeni istek
        // AÇMAZ, yalnız sürümü artırır; süren yazma bitince SON durum yazılır.
        // Alan YALNIZ tamamlanmamış görevi tutar: yazma SENKRON biterse (istek
        // fırtınası / oturum düştü hatası ilk await'ten önce fırlar) finally alanı
        // önce null'lar; tamamlanmış görevi alana yazmak sonraki her tıklamayı
        // o bitmiş göreve bağlar ve katlama bir daha HİÇ yazılmazdı (hasım bulgusu).
        if (katlamaYazici is { IsCompleted: false } suren) return suren;
        var yazici = KatlamayiYazAsync();
        if (!yazici.IsCompleted) katlamaYazici = yazici;
        return yazici;
    }

    // ── PAKET KATMAN-2 — katlama yazımı: TEK YAZICI + BİRLEŞTİRME ─────────────
    //
    // 25.09 turundaki kalıcı kırmızı bandın ölçülen kaynağı: istemci her tıklamada
    // AYRI ve EŞANLI bir PATCH açıyordu; satır yokken iki eşanlı PATCH sunucuda
    // benzersiz indekse çarpıp 409/500 dönüyor, ekran da "kaydedilemedi, geri
    // alındı" bandını çiziyordu (menü aslında kaydedilmişti). Sunucu yarışı artık
    // kendisi çözüyor (TercihServisi.Yama); istemci de yükü doğurmaz:
    //   • aynı anda EN ÇOK BİR katlama PATCH'i uçuştadır,
    //   • ilk tıklama BEKLEMEDEN yazılır (tek tıklama anında kalıcı),
    //   • yazma sürerken gelen tıklamalar BİRLEŞİR; yazma bitince tıklamalar
    //     <see cref="KatlamaSukunetMs"/> ms durulana kadar beklenir (debounce),
    //     sonra YALNIZ SON durum yazılır — 10 hızlı tıklama 10 istek değildir.
    //   • başarısızlıkta yerel durum SUNUCUNUN BİLDİĞİ son duruma döner ve bant
    //     çizilir (md.1 — "oldu" ancak kanıtla); 429 zaten ApiIstemci'de geri
    //     çekilmeyle sessizce yeniden denenir, bant yalnız kalıcı hatada çıkar.

    /// <summary>Art arda tıklamalar durulduktan kaç ms sonra son durum yazılır.</summary>
    public const int KatlamaSukunetMs = 350;

    /// <summary>Katlama yazımı 429'da (ApiIstemci geri çekilmesinden sonra) kaç kez sessizce yeniden denenir.</summary>
    public const int KatlamaOranYenidenDeneme = 3;

    /// <summary>Test kancası — gerçek saat yerine anında dönen bekleme verilebilir.</summary>
    public Func<TimeSpan, Task> Bekle { get; set; } = sure => Task.Delay(sure);

    private int katlamaSurumu;
    private Task? katlamaYazici;
    /// <summary>Sunucunun doğruladığı son katlama kümesi (geri alma hedefi).</summary>
    private HashSet<string>? sunucudakiAcikGruplar;

    private async Task KatlamayiYazAsync()
    {
        try
        {
            while (true)
            {
                var yazilanSurum = katlamaSurumu;
                if (acikGruplar is null) return;   // arada "Grupları sıfırla" geldi: DELETE onun işi
                var yazilan = new HashSet<string>(acikGruplar);
                var tamam = await YazAsync(AnahtarAcikGruplar,
                    JsonSerializer.Serialize(yazilan.ToArray(), Json),
                    () =>
                    {
                        acikGruplar = sunucudakiAcikGruplar is null ? null : new HashSet<string>(sunucudakiAcikGruplar);
                        katlamaSurumu++;
                    },
                    KatlamaOranYenidenDeneme);
                if (!tamam) return;
                sunucudakiAcikGruplar = yazilan;
                if (yazilanSurum == katlamaSurumu) return;   // arada tıklama yok: son durum yazıldı

                // Yazma sürerken tıklandı → tıklamalar durulana kadar bekle, sonra SON durumu yaz.
                int gorulen;
                do
                {
                    gorulen = katlamaSurumu;
                    await Bekle(TimeSpan.FromMilliseconds(KatlamaSukunetMs));
                } while (gorulen != katlamaSurumu);
            }
        }
        finally
        {
            katlamaYazici = null;
        }
    }

    /// <summary>
    /// Bandın "kapat" düğmesi: hatayı ekrandan kaldırır. Veri değişmez — yerel
    /// durum zaten sunucunun bildiğine geri alınmıştı.
    /// </summary>
    public void HatayiKapat()
    {
        Hata = null;
        HataAnahtari = null;
        Degisti?.Invoke();
    }

    /// <summary>Katlama tercihini SİLER — varsayılana (aktif grubun açık olması) döner.</summary>
    public async Task KatlamayiSifirlaAsync()
    {
        var onceki = acikGruplar;
        acikGruplar = null;
        katlamaSurumu++;
        Degisti?.Invoke();
        // Uçuştaki katlama PATCH'i DELETE'ten SONRA işlenirse sunucuda değer kalır,
        // istemci null sanırdı: önce süren yazıcı biter (acikGruplar null gördüğü
        // için yeni PATCH açmaz), sonra DELETE atılır.
        if (katlamaYazici is { IsCompleted: false } suren)
        {
            await suren;
            onceki = acikGruplar ?? onceki;   // yazıcı hata verip geri aldıysa geri dönülecek durum o
            acikGruplar = null;
            katlamaSurumu++;
            Degisti?.Invoke();
        }
        if (await SilAsync(AnahtarAcikGruplar, () => acikGruplar = onceki))
            sunucudakiAcikGruplar = null;
    }

    // ── Karşılama ekranı kutucuk düzeni ──────────────────────────────────────

    public bool KutucukGizli(string kod) => panel.Gizli.Contains(kod);

    public List<T> PanelSirasi<T>(List<T> kutucuklar, Func<T, string> anahtar)
    {
        if (panel.Sira.Count == 0) return kutucuklar;
        var indeks = panel.Sira.Select((k, i) => (k, i)).ToDictionary(p => p.k, p => p.i);
        return [.. kutucuklar.OrderBy(k => indeks.TryGetValue(anahtar(k), out var i) ? i : int.MaxValue)];
    }

    public async Task KutucukGizleDegistirAsync(string kod)
    {
        var onceki = panel;
        var gizli = panel.Gizli.Contains(kod)
            ? panel.Gizli.Where(g => g != kod).ToList()
            : [.. panel.Gizli, kod];
        panel = panel with { Gizli = gizli };
        Degisti?.Invoke();
        await YazAsync(AnahtarPanel, JsonSerializer.Serialize(panel, Json), () => panel = onceki);
    }

    public async Task KutucukTasiAsync(IReadOnlyList<string> gorunenSira, string kod, int yon)
    {
        var liste = gorunenSira.ToList();
        var i = liste.IndexOf(kod);
        if (i < 0) return;
        var hedef = i + yon;
        if (hedef < 0 || hedef >= liste.Count) return;
        (liste[i], liste[hedef]) = (liste[hedef], liste[i]);
        var onceki = panel;
        panel = panel with { Sira = liste };
        Degisti?.Invoke();
        await YazAsync(AnahtarPanel, JsonSerializer.Serialize(panel, Json), () => panel = onceki);
    }

    /// <summary>P-37b — karşılama kutucuğunu sürükle-bırakla hedefin yerine taşır.</summary>
    public Task KutucukBirakAsync(IReadOnlyList<string> gorunenSira, string kaynak, string hedef)
    {
        if (kaynak == hedef) return Task.CompletedTask;
        var liste = gorunenSira.ToList();
        var i = liste.IndexOf(kaynak);
        var j = liste.IndexOf(hedef);
        if (i < 0 || j < 0) return Task.CompletedTask;
        liste.RemoveAt(i);
        liste.Insert(j, kaynak);
        var onceki = panel;
        panel = panel with { Sira = liste };
        Degisti?.Invoke();
        return YazAsync(AnahtarPanel, JsonSerializer.Serialize(panel, Json), () => panel = onceki);
    }

    public async Task PanelDuzeniniSifirlaAsync()
    {
        var onceki = panel;
        panel = new([], []);
        Degisti?.Invoke();
        await SilAsync(AnahtarPanel, () => panel = onceki);
    }

    // ── Sunucu yazma (PATCH: gönderilmeyen anahtar KORUNUR) ──────────────────

    /// <param name="oranYenidenDeneme">
    /// KATMAN-2 — sunucu oran sınırı (<see cref="OranSinirHatasi"/>, ApiIstemci'nin kendi
    /// geri çekilmesinden SONRA da süren 429) bir kalıcı hata değildir: bu kadar kez
    /// sunucunun söylediği süre beklenip SESSİZCE yeniden yazılır; ancak sonra bant.
    /// İstek fırtınası (<see cref="IstekFirtinasiHatasi"/>) bir kod hatasıdır, beklenmez.
    /// </param>
    private async Task<bool> YazAsync(string anahtar, string deger, Action geriAl, int oranYenidenDeneme = 0)
    {
        var tamam = true;
        for (var deneme = 0; ; deneme++)
        {
            try
            {
                await api.PatchAsync<TercihCevap>("/tercihler/benim",
                    new { tercihler = new Dictionary<string, string?> { [anahtar] = deger } });
                Hata = null;
                HataAnahtari = null;
                break;
            }
            catch (OranSinirHatasi ex) when (deneme < oranYenidenDeneme)
            {
                await Bekle(TimeSpan.FromSeconds(Math.Max(1, ex.KalanSaniye)));
            }
            catch (Exception ex)
            {
                geriAl();               // yerel değişiklik GERİ ALINIR — yalan söylemez
                Hata = ex.Message;
                HataAnahtari = anahtar;
                tamam = false;
                break;
            }
        }
        Degisti?.Invoke();
        return tamam;
    }

    private async Task<bool> SilAsync(string anahtar, Action geriAl)
    {
        var tamam = true;
        try
        {
            await api.DeleteAsync<TercihCevap>($"/tercihler/benim/{anahtar}");
            Hata = null;
            HataAnahtari = null;
        }
        catch (Exception ex)
        {
            geriAl();
            Hata = ex.Message;
            HataAnahtari = anahtar;
            tamam = false;
        }
        Degisti?.Invoke();
        return tamam;
    }

    private sealed record TercihCevap(Dictionary<string, string>? Tercihler);
}

/// <summary>Karşılama ekranı kutucuk düzeni — sıra (kod listesi) + gizlenenler.</summary>
public sealed record PanelDuzeni(List<string> Sira, List<string> Gizli);
