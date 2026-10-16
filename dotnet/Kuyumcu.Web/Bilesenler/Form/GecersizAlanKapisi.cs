namespace Kuyumcu.Web.Bilesenler.Form;

// ═════════════════════════════════════════════════════════════════════════════
// GEÇERSİZ ALAN KAPISI — "ekranda görünen ≠ gönderilen" HATA SINIFININ ortak kapısı
// (SATIS-3 t17, orkestratör 29.09 12:10 + hasım doğrulayıcı KRİTİK ×2).
//
// SORUN: SayisalAlan (Sayi/Para/Gram) çözülemeyen ya da BELİRSİZ metinde ("1.500",
// tuş tuş yazımın ara değerleri "1.5" → "1.500") alan altına hata yazıyor ama
// çağırana hiçbir şey bildirmiyordu → sayfa ESKİ değeri (teklif fiyatı, kur, ara
// değer 1,5 g) tutuyor, Kaydet açık kalıyor ve kullanıcının görmediği sayı POST
// ediliyordu. Kutu kutu bayrak açmak (t16 GecersizdeBosalt) sınıfı kapatmadı.
//
// ÇÖZÜM (EKLEMELİ, geriye uyumlu): bir form bu kapıyı CascadingValue ile verirse
// o formun İÇİNDEKİ HER SayisalAlan kendiliğinden:
//   (a) alan hatasını gösterir (zaten gösteriyordu),
//   (b) kendini kapıya "geçersiz" diye yazar → form Kaydet'i PASİF yapar ve
//       sebebi ALANIN ETİKETİYLE söyler (UX-S30),
//   (c) çağırana DegerChanged(null) bildirir → eski/varsayılan/kur/teklif değeri
//       gövdeye GİTMEZ,
//   (d) değer null olduğu için OnParametersSet kullanıcının yazdığı metni EZMEZ
//       (kur/teklif yansıtması alanın üstüne yazamaz).
// Kapıyı VERMEYEN formlarda davranış BİREBİR eskisi gibidir.
// Alan DOM'dan kalkınca (Katlanir kapanır, tür değişir) kaydı silinir (Dispose).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir formdaki sayısı anlaşılamayan alanların kaydı (ElemanId → etiket).</summary>
public sealed class GecersizAlanKapisi
{
    private readonly List<(string Id, string Etiket)> gecersiz = [];

    /// <summary>
    /// GIRDI-AILESI-2: anlamsal girdi bileşenlerinin (Iban, Telefon, Tarih …) alan altı metni.
    /// Kilit sebebi bu metni BİREBİR söyler (kurucu 08.10: "Kaydet kilidi aynı metin").
    /// </summary>
    private readonly Dictionary<string, string> mesajlar = new(StringComparer.Ordinal);
    private readonly Action? degisti;

    /// <param name="degisti">Kayıt DEĞİŞİNCE çağrılır — formun sahibi yeniden çizilir (alanın çağıranı
    /// yalnız alt bileşen olsa bile Kaydet'in kilidi tazelenir, ör. para üstü "Alınan").</param>
    public GecersizAlanKapisi(Action? degisti = null) => this.degisti = degisti;

    /// <summary>Formda geçersiz/belirsiz sayı taşıyan en az bir alan var mı?</summary>
    public bool Var => gecersiz.Count > 0;

    /// <summary>Verilen ön ekle başlayan (ör. "avans-") geçersiz alan var mı?</summary>
    public bool OnekleVar(string onek) => gecersiz.Exists(g => g.Id.StartsWith(onek, StringComparison.Ordinal));

    /// <summary>İlk (en eski) geçersiz alan — kilit sebebi bunu söyler.</summary>
    public (string Id, string Etiket)? Ilk => gecersiz.Count > 0 ? gecersiz[0] : null;

    /// <summary>Ön ekle başlayan geçersiz alanların etiketleri (sıralı, tekrarsız).</summary>
    public IReadOnlyList<string> Etiketler(string onek) =>
        [.. gecersiz.Where(g => g.Id.StartsWith(onek, StringComparison.Ordinal)).Select(g => g.Etiket).Distinct()];

    /// <summary>Alanın durumunu yazar: geçersizse kayda ekler, değilse kayıttan siler.</summary>
    public void Bildir(string id, string etiket, bool gecersizMi) => Bildir(id, etiket, gecersizMi, null);

    /// <summary>
    /// GIRDI-AILESI-2: alan durumunu alan altı METNİYLE yazar; geçersizken <see cref="Sebep"/> bu metni aynen döndürür.
    /// </summary>
    public void Bildir(string id, string etiket, bool gecersizMi, string? mesaj)
    {
        var mesajDegisti = false;
        if (gecersizMi && mesaj is { Length: > 0 })
        {
            mesajDegisti = !mesajlar.TryGetValue(id, out var eski) || eski != mesaj;
            mesajlar[id] = mesaj;
        }
        else mesajDegisti = mesajlar.Remove(id);
        var ix = gecersiz.FindIndex(g => g.Id == id);
        if (gecersizMi && ix < 0) gecersiz.Add((id, etiket));
        else if (!gecersizMi && ix >= 0) gecersiz.RemoveAt(ix);
        else if (!mesajDegisti) return;   // değişiklik yok → yeniden çizim yok
        degisti?.Invoke();
    }

    // ═══ ORTAK-1 (KEŞİF #4, 30.09) — fiş DIŞI formlar için EKLEMELİ yardımcılar ═══════════════
    // /urunler "İşçilik Tutarı" = "1.500" → Kaydet → gövdede 0 (kutu hatasız "0"a dönüyordu: kapısız
    // formda OnParametersSet eski değeri geri yazıp iç hatayı siliyordu). Fiş dışı her form aynı kapıyı
    // aynı üç satırla kullanır: CascadingValue + kilit sebebi (Sebep) + kaydet koruması (FormaIsle).

    /// <summary>Sayfanın yeniden çizimini güvenle isteyen kurucu (sayfa kapanırken yok sayılır).</summary>
    public GecersizAlanKapisi(Func<Task> tazele) : this(() =>
    {
        try { _ = tazele(); }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    })
    { }

    /// <summary>
    /// UX-S30 kilit sebebi: ön ekle (ör. "u-") başlayan ilk geçersiz alanı ETİKETİYLE söyler
    /// ("«İşçilik Tutarı» kutusundaki sayıyı düzeltin …"); geçersiz alan yoksa null.
    /// </summary>
    public string? Sebep(string dil, string onek = "")
    {
        var g = gecersiz.Find(x => x.Id.StartsWith(onek, StringComparison.Ordinal));
        if (g.Id is null) return null;
        if (mesajlar.TryGetValue(g.Id, out var m)) return m;   // GIRDI-AILESI-2: alan altı metniyle AYNI
        return string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Al(dil).AlanDuzeltin, g.Etiket);
    }

    /// <summary>
    /// Kaydet koruması (düğme kilidinin arkasındaki ikinci kat — Enter ile gönderim, yarış):
    /// ön ekli her geçersiz alanı forma alan hatası olarak işler; en az biri varsa true → çağıran POST ETMEZ.
    /// </summary>
    public bool FormaIsle(Kuyumcu.Web.Servisler.FormDurumu form, string onek, string mesaj)
    {
        var var_ = false;
        foreach (var g in gecersiz)
        {
            if (!g.Id.StartsWith(onek, StringComparison.Ordinal)) continue;
            form.Ekle(g.Id, mesaj);
            var_ = true;
        }
        return var_;
    }

    /// <summary>Tüm kaydı siler (form sıfırlanınca).</summary>
    public void Temizle()
    {
        if (gecersiz.Count == 0) return;
        gecersiz.Clear();
        mesajlar.Clear();
        degisti?.Invoke();
    }
}
