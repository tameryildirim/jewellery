using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Bilesenler.Form;

// ═════════════════════════════════════════════════════════════════════════════
// SAYISAL ALAN TABANI — Sayi · Para · Gram bileşenlerinin ORTAK beyni
// (paket P-36b · talimat 20/a).
//
// ÜÇ BİLEŞEN, TEK DAVRANIŞ:
//   • Kullanıcı "12,5" de yazar "12.5" de — ayrıştırma TEK yerde (FormAlani).
//   • Metin sayıya çevrilemiyorsa alan SESSİZ KALMAZ: alan altına Türkçe
//     mesaj yazılır (DoD EK md.3). Eski davranış (0'a düşmek) Anayasa md.1
//     ihlaliydi: bilinmeyen tahmin edilmez.
//   • Girilen değerin BİÇİMLİ hâli anında yanında gösterilir ve o biçim
//     YALNIZ Servisler/Bicim.cs'ten gelir (denetim.sh: GIRDI_AILESI_DISI_BICIM).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Ondalık değer taşıyan girdilerin ortak tabanı.</summary>
public abstract class SayisalAlan : FormAlani, IDisposable
{
    [Parameter] public decimal? Deger { get; set; }
    [Parameter] public EventCallback<decimal?> DegerChanged { get; set; }

    /// <summary>
    /// Alt sınır — sunucu kuralının ekran karşılığı. null ise sınır yok.
    /// GIRDI-AILESI-2: artık ÖLÜ DEĞİL (envanter açık #4: 30 çağrı koruma var sanıyordu):
    /// altındaki değer alan altında anında hata + kapıda Kaydet kilidi; EnAz ≥ 0 iken "-" tuşu
    /// yazılamaz (girdi-maske.js 'sayi'). Kural Core SayiKurali.Aralik'tadır.
    /// </summary>
    [Parameter] public decimal? EnAz { get; set; }
    /// <summary>Üst sınır — EnAz ile aynı şekilde UYGULANIR (GIRDI-AILESI-2).</summary>
    [Parameter] public decimal? EnCok { get; set; }

    /// <summary>
    /// GIRDI-AILESI-2 — alanın iş kuralı (Core'daki TEK sınıf). Varsayılan: EnAz/EnCok aralığı.
    /// Anlamsal alt bileşenler (Adet, Milyem, Yuzde, KurAlani, GunSayisi, Has, Para Tur=…) kendi
    /// Core kuralını ekler ve sonra tabanı çağırır.
    /// </summary>
    protected virtual GirdiHatasi? KuralDenetle(decimal d) => SayiKurali.Aralik(d, EnAz, EnCok);

    /// <summary>
    /// PAKET GOC-C (en küçük geriye uyumlu ekleme): tam sayı ihlalinin iletisi Core kuralından (GirdiSozlugu) mı gelsin?
    /// Varsayılan false = eski davranış (KESIF-P1 metni). Adet true döner: sunucu AdetKurali ile aynı cümle.
    /// </summary>
    protected virtual bool TamSayiCoreKuralinda => false;

    /// <summary>
    /// GOC-C-2 VERI-2: tam sayı alanında ondalık tuşu engellendiğinde görünen ipucu (girdi-maske.js · &lt;id&gt;-kesir).
    /// Adet (Core kuralı): "Adet küsuratlı yazılamaz…"; öbür tam sayı alanları genel cümle (ipucu ≠ alan altı HATASI:
    /// gizli ipucu hata metniyle aynı olsaydı "hata yok" denetimleri yanılırdı).
    /// </summary>
    protected string KesirTusuIpucu => TamSayiCoreKuralinda
        ? Kuyumcu.Core.I18n.GocC2Sozlugu.Al(Dil.Dil).KesirTusuEngeli
        : Kuyumcu.Core.I18n.GocC2Sozlugu.Al(Dil.Dil).KesirTusuEngeliGenel;

    /// <summary>"-" tuşu yazılabilir mi? (girdi-maske.js data-eksi) — alt sınır yoksa ya da eksiyse.</summary>
    protected virtual bool EksiYazilabilir => EnAz is not { } a || a < 0;

    /// <summary>Maske özniteliği: eksi serbestse "1", değilse çizilmez.</summary>
    protected string? EksiOzniteligi => EksiYazilabilir ? "1" : null;

    /// <summary>Maske özniteliği: tam sayı alanında "1".</summary>
    protected string? TamOzniteligi => TamSayi ? "1" : null;

    /// <summary>İç hata bir İŞ KURALI hatası mı? (kilit sebebi o zaman alan altı metninin AYNISI)</summary>
    private bool icHataKural;

    /// <summary>
    /// SATIS-3 t16 (doğrulayıcı KRİTİK 29.09) — İSTEĞE BAĞLI, varsayılan KAPALI: metin sayıya çevrilemezse
    /// (geçersiz ya da "1.500" gibi belirsiz) çağırana <c>DegerChanged(null)</c> bildirilir. Böylece sayfa
    /// "görünen ≠ giden" durumuna düşmez: kutuda hata yazarken eski/kurdan gelen değer POST edilmez ve
    /// Kaydet kilitlenir. Ham metin ve iç hata korunur (Deger null ⇒ OnParametersSet kutuyu ezmez).
    /// Açmayan bileşenlerin davranışı DEĞİŞMEZ.
    /// </summary>
    [Parameter] public bool GecersizdeBosalt { get; set; }

    /// <summary>
    /// KESIF-P1 (#5 · #31): alan YALNIZ TAM SAYI kabul eder (valör günü, taksit adedi).
    /// Kesirli metin ('1,5') sessizce 1'e YUVARLANMAZ (yerli type=number böyle yapıyordu):
    /// alan altında Türkçe hata, kapı açıksa çağırana null bildirilir → Kaydet pasif, istek 0.
    /// Varsayılan KAPALI: açmayan bileşenlerin davranışı DEĞİŞMEZ.
    /// </summary>
    // ⚠ `new`: FormAlani.TamSayi(string?) statik ayrıştırıcısını bu türde gizler (çağıranlar
    //   onu sınıf adıyla — FormAlani.TamSayi(...) — çağırır; etkilenmez).
    [Parameter] public new bool TamSayi { get; set; }

    /// <summary>
    /// KESIF-P1 tur 2 (doğrulayıcı KRİTİK): alan en çok bu kadar ondalık hane kabul eder (null = sınırsız).
    /// Fazlası sessizce YUVARLANMAZ (POS komisyonu: '1,855' % → 0,01855 kesir, kolon decimal(5,4)
    /// 0,0186 yazıyordu — girilen ≠ kaydedilen): alan altında Türkçe hata, kapı açıksa çağırana null.
    /// Varsayılan KAPALI: açmayan bileşenlerin davranışı DEĞİŞMEZ.
    /// </summary>
    [Parameter] public int? EnCokOndalik { get; set; }

    /// <summary>Tam sayı kuralını bozan değer mi?</summary>
    private bool TamSayiIhlali(decimal d) => TamSayi && d != decimal.Truncate(d);

    /// <summary>Ondalık hane sınırını bozan değer mi?</summary>
    private bool OndalikIhlali(decimal d) => EnCokOndalik is { } n && n >= 0 && decimal.Round(d, n) != d;

    /// <summary>
    /// SATIS-3 t17 (orkestratör 29.09 12:10 — "görünen ≠ giden" HATA SINIFI): formun ortak geçersiz alan
    /// kapısı. Form bunu CascadingValue ile verirse bu alan <see cref="GecersizdeBosalt"/> açıkmış gibi
    /// davranır VE geçersiz/belirsiz durumunu kapıya yazar (Kaydet pasif + sebep alanın etiketi).
    /// Vermeyen formlarda davranış DEĞİŞMEZ (geriye uyumlu).
    /// </summary>
    [CascadingParameter] public GecersizAlanKapisi? GecersizKapi { get; set; }

    /// <summary>Geçersiz metinde çağırana null bildirilsin mi? (bayrak ya da kapı)</summary>
    private bool NullBildir => GecersizdeBosalt || GecersizKapi is not null;

    /// <summary>Kapıya bu alanın güncel durumunu yazar (kapı yoksa hiçbir şey yapmaz).</summary>
    private void KapiyaYaz() => GecersizKapi?.Bildir(ElemanId, Etiket, icHata is not null, icHataKural ? icHata : null);

    /// <summary>Alan DOM'dan kalkınca (katlanır kapandı, tür değişti) kapıdaki kaydı silinir.</summary>
    public void Dispose() => GecersizKapi?.Bildir(ElemanId, Etiket, false);

    [Inject] protected DilServisi Dil { get; set; } = default!;

    /// <summary>Kullanıcının o an yazdığı ham metin (sayıya çevrilmemiş hâli).</summary>
    protected string ham = "";

    /// <summary>Metin sayıya çevrilemediğinde doğan İÇ hata — çağıranın Hata'sını ezmez.</summary>
    protected string? icHata;

    /// <summary>Alanın gösterdiği hata: önce çağıranınki, sonra ayrıştırma hatası.</summary>
    /// SATIS-3 t17: kapı verilmiş formda ayrıştırma hatası ÖNCE gelir — çağıran null'u "zorunlu" diye
    /// raporlasa bile kullanıcı yazdığının NEDEN anlaşılmadığını görür. Kapısız formda eski sıra.
    protected string? EtkinHata =>
        GecersizKapi is not null && icHata is { Length: > 0 } ? icHata
        : GorunenHata is { Length: > 0 } ? GorunenHata : icHata;

    protected bool EtkinHatali => EtkinHata is { Length: > 0 };

    /// <summary>
    /// BICIM-2: artık kutunun ODAKTAKİ (ham) değeri ve <c>data-deger</c> özniteliği —
    /// odak dışında kutu <see cref="KutuDegeri"/> ile biçimli görünür; değerin
    /// düzenlenebilir hâli <c>data-deger</c>de kalır (Secici ile aynı sözleşme).
    /// P-78: eskiden Makine (Invariant, "1.234") idi;
    /// tr-TR çözücü "1.234"ü BELİRSİZ sayar, kutu kendi değerini geri okuyamazdı.
    /// Artık tr-TR gruplamasız ("1,234") — gidiş-dönüş birebir.
    /// </summary>
    protected string MakineDeger => Deger is { } d ? Bicim.Girdi(d) : "";

    // ═══ PAKET BICIM-2 — ODAK DIŞINDA BİÇİMLİ, ODAKTA HAM ═══════════════
    // Kurucu 25.09: Satış karma ödemede tablo "113.363,16 ₺", ödeme kutuları
    // "50000" / "63363,16". CUMA-M TelefonBicimli kalıbı:
    //   • ODAK DIŞI: kutu değeri Bicim.*Girdi ile gruplu ("50.000", "63.363,16").
    //   • ODAKTA: ham düzenlenebilir metin (Bicim.Girdi, gruplamasız, virgül
    //     ondalık) — yazarken değer YENİDEN YAZILMAZ (imleç atlamaz).
    //   • ODAKTAN ÇIKINCA: metin SayiCoz ile yeniden çözülür, ham normal
    //     yazıma getirilir ve kutu biçimlenir.
    // ⚠ Biçimli metin SayiCoz'a HİÇ gitmez: Deger decimal olarak tutulur;
    //   P-78 belirsizlik kuralı ("1.500" yazan kullanıcıya hata) KORUNUR.
    //   Metin çözülemiyorsa (hata) kutu kullanıcının yazdığını AYNEN gösterir.

    /// <summary>Kutu şu an odakta mı? (onfocus/onblur ile izlenir.)</summary>
    protected bool odakta;

    /// <summary>Odak dışı gösterim — alt bileşen verir (Bicim.ParaGirdi / GramGirdi / SayiGirdi).</summary>
    protected abstract string OdakDisiBicim(decimal d);

    /// <summary>
    /// Kutunun value özniteliği. Odakta ya da metin geçerli bir sayı değilse
    /// HAM metin; aksi hâlde biçimli gösterim.
    /// </summary>
    protected string KutuDegeri =>
        !odakta && icHata is null && Deger is { } d && Ondalik(ham) == d
            ? OdakDisiBicim(d)
            : ham;

    protected void OdakGeldi() => odakta = true;

    protected async Task OdakGitti()
    {
        odakta = false;
        var cozum = SayiCoz(ham);
        if (cozum.Deger is not { } d) return;   // hata/boş: Degisti'nin kararı geçerli
        if (TamSayiIhlali(d)) return;           // KESIF-P1: kesirli metin kabul edilmez (Degisti hatayı yazdı)
        if (OndalikIhlali(d)) return;           // KESIF-P1 tur 2: fazla ondalık kabul edilmez (Degisti hatayı yazdı)
        if (KuralDenetle(d) is not null) return; // GIRDI-AILESI-2: kural hatası Degisti'de yazıldı
        ham = Bicim.Girdi(d);                    // "50.000" → "50000" (odağa dönünce ham)
        if (d != Deger)
        {
            icHata = null;
            KapiyaYaz();
            Deger = d;
            await DegerChanged.InvokeAsync(d);
        }
    }

    protected override void OnParametersSet()
    {
        // Dışarıdan gelen değer kullanıcının yazdığından FARKLIYSA kutu tazelenir;
        // aynıysa dokunulmaz (yazarken imleç sıfırlanmasın).
        // SATIS-3 t17 (d): kapı/bayrak açıkken alan GEÇERSİZ metin taşıyorsa dışarıdan gelen değer
        // (çağıranın null→0 dönüşümü, kur, teklif, otomatik kalan) kullanıcının yazdığının ÜSTÜNE YAZMAZ;
        // kutu kullanıcı düzeltene dek onun metnini ve hatasını gösterir, kapı Kaydet'i kilitli tutar.
        if (NullBildir && icHata is not null) return;
        // t17 canlı ölçüm: kullanıcı kutuyu SİLDİ (odakta, boş) ve çağıran null'u 0'a çevirip geri itti (ödeme satırı
        // TutarTl decimal) → kutuya "0" yazılıyor, ardından yazılan "1,5" "01,5" görünüyordu. Kapıda boş kutu boş kalır.
        if (NullBildir && odakta && string.IsNullOrWhiteSpace(ham) && Deger == 0m) return;
        if (Ondalik(ham) != Deger)
        {
            ham = MakineDeger;
            icHata = null;
            KapiyaYaz();
        }
    }

    // ═══ PAKET P-39 · K7 — MÜKERRER ÖNİZLEME ═══════════════════════════════
    // TASARIM-DENETIMI-2026-09-21 § 5.1: aynı değer hem girdinin İÇİNDE hem
    // hemen ALTINDA yazıyordu (8 alan / 5 ekran). En kötüsü `/fiyatlandirma`:
    // önizleme girdiyle BİREBİR aynıydı ("100" / "100").
    // KURAL (§ 9 K7): önizleme yalnız BİLGİ EKLİYORSA gösterilir.
    // Ölçüt, denetçinin ölçüm betiğindeki "girdi-alti-tekrar" ile aynı SORUYU
    // sorar (§ 10.3) ama rakam dizisi yerine SAYISAL DEĞER üzerinden — sebebi
    // aşağıda, <see cref="BicimBilgiEkliyor"/> başlığındaki kurucu bulgusu.

    /// <summary>Önizlemenin biçimlediği metin — alt bileşen verir (Bicim.*).</summary>
    protected abstract string OnizlemeBicimi { get; }

    /// <summary>Biçimin ötesinde ek bilgi var mı (ör. has karşılığı)? Varsayılan: yok.</summary>
    protected virtual bool OnizlemeEkBilgiVar => false;

    /// <summary>Biçimli metin girdinin kendi değerinden FAZLASINI söylüyor mu?</summary>
    // ─── KURUCU BULGUSU 2026-09-22: "adet kısmında ne yazarsam altta da o
    //     görünüyor" ────────────────────────────────────────────────────────
    // K7 ölçütü RAKAM DİZİSİ karşılaştırmasıydı ve SIZDIRIYORDU. `/islemler`
    // Adet kutusunda 5198'de ÖLÇÜLDÜ (kutu → önizleme):
    //     "1,0"   → "1"      ·  "15,20" → "15.2"
    //     "2,50"  → "2.5"    ·  "007"   → "7"
    // Sebep: sondaki/baştaki sıfır rakam dizisini değiştiriyor ("1520" ⊄
    // "152"), ölçüt "bilgi eklendi" sanıyor ve mükerrer önizlemeyi yeniden
    // çiziyordu — üstelik Türkçe ekranda NOKTALI ("15.2"), yani kullanıcının
    // yazdığından daha da kötü bir yazımla. Yazım farkı BİLGİ DEĞİLDİR.
    //
    // DOĞRU ÖLÇÜT: kutudaki metin bu sayıyı ZATEN söylüyor mu? Söylüyorsa
    // önizlemenin tek yaptığı aynı sayıyı başka bir yazımla tekrarlamaktır →
    // çizilmez. Gerçekten YENİ olan bilgi (has karşılığı gibi) bu ölçütten
    // geçmez; onu <see cref="OnizlemeEkBilgiVar"/> ayakta tutar.
    // Ölçüt sayısal DEĞER üzerinden işler: decimal karşılaştırması ölçeği
    // yok sayar (15,20 == 15,2), yazım oyunlarına kapalıdır.
    protected bool BicimBilgiEkliyor
    {
        get
        {
            if (Deger is null) return false;
            if (OnizlemeBicimi.Length == 0) return false;
            // Metin sayıya çevrilemiyorsa (kutuda çöp var, Deger son geçerli
            // değeri tutuyor) sistemin hâlâ bildiği sayı BİLGİDİR: gösterilir.
            return Ondalik(ham) != Deger;
        }
    }

    /// <summary>Önizleme çizilecek mi? (aria-describedby zinciri de buna bakar.)</summary>
    protected bool OnizlemeGorunur => Deger is not null && (BicimBilgiEkliyor || OnizlemeEkBilgiVar)
        && !(NullBildir && icHata is not null);   // t17: geçersiz metnin altına "sistemin bildiği" eski sayı yazılmaz

    /// <summary>aria-describedby zinciri + iç hata kimliği.</summary>
    protected string? SayisalAciklayan
    {
        get
        {
            var p = new List<string>(4);
            if (Ipucu is { Length: > 0 }) p.Add($"{ElemanId}-ipucu");
            if (Kilitli && DevredisiSebebi is { Length: > 0 }) p.Add($"{ElemanId}-durum");
            if (EtkinHatali) p.Add(HataElemanId);
            if (OnizlemeGorunur) p.Add($"{ElemanId}-onizleme");
            return p.Count == 0 ? null : string.Join(' ', p);
        }
    }

    protected async Task Degisti(ChangeEventArgs e)
    {
        // BICIM-2: input olayı = kullanıcı kutuda YAZIYOR. focus olayı kaçsa
        // bile (tarayıcı otomatik doldurma, olay sırası) yazılan metin
        // biçimle ezilmez; biçim yalnız blur'da gelir.
        odakta = true;
        DefterdenSil();
        icHataKural = false;
        ham = e.Value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(ham))
        {
            icHata = null;
            KapiyaYaz();
            Deger = null;
            await DegerChanged.InvokeAsync(null);
            return;
        }

        var cozum = SayiCoz(ham);
        if (cozum.Deger is not { } d)
        {
            // Sessiz etkisizlik YASAK: kutu kırmızı olur ve sebebi altında yazar.
            // P-78: "1.500" gibi BELİRSİZ girdi ayrı mesajla söylenir (fail-closed).
            icHata = cozum.Sonuc == SayiSonucu.Belirsiz
                ? cozum.Mesaj(Dil.Dil, TamSayi)
                : Dil.T.Form.SayiAnlasilmadi;
            KapiyaYaz();            // t17: önce kapı (çağıranın yeniden çizimi kilidi görsün)
            if (NullBildir)         // Deger zaten null olsa da bildirilir: çağıran "kullanıcı yazdı" bilgisini alır (kur üzerine yazmasın)
            {
                Deger = null;
                await DegerChanged.InvokeAsync(null);
            }
            return;
        }

        if (TamSayiIhlali(d) && !(TamSayiCoreKuralinda && KuralDenetle(d) is not null))
        {
            // KESIF-P1 (#5): "1,5" gün/adet sessizce 1 olmaz — alan hatası + kapı.
            // PAKET GOC-C: anlamsal bileşen (Adet) tam sayı kuralını Core'da taşıyorsa bu dal ATLANIR ve aşağıdaki
            // kural dalı Core metnini yazar — ekran ile sunucu AYNI sözlük anahtarı ("Küsuratlı yazılamaz …").
            icHata = Kuyumcu.Core.I18n.KesifP1Sozlugu.Al(Dil.Dil).TamSayiHata;
            KapiyaYaz();
            if (NullBildir)
            {
                Deger = null;
                await DegerChanged.InvokeAsync(null);
            }
            return;
        }

        if (OndalikIhlali(d))
        {
            // KESIF-P1 tur 2: '1,855' % sessizce 1,86 olmaz — alan hatası + kapı.
            icHata = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                Kuyumcu.Core.I18n.KesifP1Sozlugu.Al(Dil.Dil).OndalikSiniri, EnCokOndalik);
            KapiyaYaz();
            if (NullBildir)
            {
                Deger = null;
                await DegerChanged.InvokeAsync(null);
            }
            return;
        }

        if (KuralDenetle(d) is { } kural)
        {
            // GIRDI-AILESI-2: EnAz/EnCok ve anlamsal kural (Core) — alan altı hata + kapı (sebep AYNI metin).
            // Kapısız formda değer çağırana yine gider (eski akış: sunucu da aynı kuralla reddeder);
            // kapılı formda null gider → Kaydet kilitli, görünen ≠ giden olmaz.
            icHata = Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(kural, Dil.Dil);
            icHataKural = true;
            KapiyaYaz();
            Deger = NullBildir ? null : d;
            await DegerChanged.InvokeAsync(Deger);
            return;
        }

        icHata = null;
        KapiyaYaz();
        Deger = d;
        await DegerChanged.InvokeAsync(d);
    }
}
