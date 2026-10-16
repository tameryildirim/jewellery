using System.Globalization;
using System.Text.RegularExpressions;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 — doğrulama bulguları (28.09): ödeme bölme ve mod değişimi.
// • "+ Başka ödeme ekle" ilk tıkta İKİ satır açar (mevcut ödeme + boş ikinci ödeme).
// • PRO→BASİT geçişi girilen ödemeleri SİLMEZ (BASİT'te de "+ Başka ödeme ekle" var).
// ⚠ Kaydet() / POST gövdeleri DEĞİŞMEDİ. Islemler.razor ≤ 1.400 satır (S21).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    /// <summary>"+ Başka ödeme ekle": tek ödeme İLK bacak olur ve İKİNCİ (boş) ödeme satırı hemen açılır
    /// (SATIS-3 doğrulama bulgusu: ilk tıkta yalnız 1 satır açılıyordu). Elle yazılmayan ilk satır kalanı üstlenir.</summary>
    private async Task OdemeyiBol() { await OdemeyiBolIlk(); bacaklar.Add(FisFark.IkinciBacak(kasalar, kasaId, bacaklar, posCihazlari, YeniOdemeNakitsiz)); }

    /// <summary>k2: seçili GERÇEK müşteri (Peşin Müşteri / sistem carisi değil) — eski altın satıcı kimliği ondan dolar.</summary>
    private Kuyumcu.Contracts.Sayfalar.Islemler.CariVeri? GercekMusteri =>
        cariler.FirstOrDefault(c => c.Id == cariId && !c.SistemCarisi && c.Id != pesinCariId);

    /// <summary>k2 (tasarım hakemi K19, 29.09): bu fişte nakit kullanılamıyor mu — yeni ödeme satırı Nakit açılmaz.
    /// Yalnız SUNUCUNUN bildirdiği bilgiyle: nakit kilidi ya da bilinen had aşıldı. Had bilinmiyorsa false
    /// (eşik sayısı istemciye gömülmez; son söz sunucuda).</summary>
    internal bool YeniOdemeNakitsiz =>
        tevsikNakitKapali
        || (tevsikHaddi is { } h && h > 0m && TevsikOneri.Gerekli("NAKIT", IadeKipi ? "ALIS" : tur, TevsikTutar, h));

    /// <summary>Tek ödemeyi İLK bacağa çevirir (karma açılır, başka satır eklemez — eski altın yolu kendi satırını ekler).</summary>
    private async Task OdemeyiBolIlk()
    {
        bacaklar.Clear();
        var ilk = FisFark.IlkBacak(odemeTuru, kasaId, ToplamTl);
        if (string.IsNullOrEmpty(ilk.KasaId)) FisHesap.BacakKasaSabitle(ilk, kasalar);
        bacaklar.Add(ilk);
        await KarmaAnahtari(true);
    }


    /// <summary>
    /// SATIS-3 doğrulama bulgusu: BASİT'te de "+ Başka ödeme ekle" var → mod değişimi girilen ödemeleri
    /// SİLMEZ (PRO→BASİT'te Kart + Eski altın sessizce tek Nakit'e dönüyordu). Tek ödemeli KART/HAVALE
    /// satışı da Nakit'e çevrilmez: tek satırlık ödeme listesi olarak korunur.
    /// </summary>
    /// <remarks>
    /// PARA-DEMO-2 (madde 1, VERİ/YÜKSEK): tek ödemeli KART/HAVALE artık "tek satırlık ödeme listesi"ne
    /// (karma, /islemler/satis-fisi) ÇEVRİLMEZ — BASİT tezgâh fişinin ALDIM satırı Kart/Havale'yi kendisi
    /// gösterir ve seçtirir (#islem-aldim-odeme). Eskiden bu çevrim tek bacaklı fişi "KARMA" diye
    /// kaydettiriyordu (ekstre "Karma ödeme", gün sonu "Karışık"). Girilen ödeme yine SİLİNMEZ.
    /// </remarks>
    private void TekOdemeyiBasitteKoru()
    {
        // Bilinçli olarak boş: tek ödeme olduğu gibi kalır (BasitModuUygula da KART/HAVALE'yi Nakit'e çevirmez).
    }

    /// <summary>
    /// SATIS-3 doğrulama (28.09, canlı ölçüm — UX-S30): Kaydet kilidinin sebebi yalnız FisDolu (kalem/karma/açıklama)
    /// iken görünüyordu; kalemsiz türlerde (Hurda alış, Değişim, İade) kullanıcı gramı yazsa da düğme SEBEPSİZ
    /// kilitli kalıyordu. Boş açılışta gizli kalır (gürültü yok); kullanıcı o türün ilk alanını doldurunca görünür.
    /// </summary>
    /// P-TEZGAH-TUR-KIMLIK tur 2 (tasarım hakemi KRİTİK "Alış/İade'de Kaydet kilitli, nedeni 1 px"): paket kuralı "Kaydet kilitliyse
    /// sebebi TEK cümle Kaydet'in yanında" — Satış dışındaki türlerde (Alış, İade, Takas, Hurda alış …) boş açılışta da görünür.
    /// Satış'ta boş fiş (ürün okutulmadı) eskisi gibi sessiz: tuvalin "Satışa başlamak için barkodu okut" satırı aynı şeyi söyler.
    internal bool KilitGorunur => FisDolu || tur != "SATIS";

    /// <summary>SATIS-3 t6 (hakem ORTA "tevsik çelişkisi"): ödemede nakit var mı — "nakit alınamaz" bandı yalnız o zaman;
    /// kartla ödenen fişte asıl kapı (Peşin Müşteri kimliği) kendi bandında (#islem-pesin-esik) söylenir, üstte ikinci bant yok.</summary>
    private bool NakitOdemede => karmaOdeme ? bacaklar.Any(b => b.Arac == "NAKIT" && b.TutarTl > 0m) : odemeTuru == "NAKIT";

    /// <summary>t11: nakit kuralı bu fişte tetiklendi ve ödemede nakit var → kırmızı bant/kilit sebebi "tamamını kart ya da
    /// havaleyle alın" der; sağdaki not (FisNakitUyari) aynı şeyi ikinci kez söylemez. Kural (tetiklenme) sunucudan/haddden.</summary>
    internal bool NakitBandiVar => NakitOdemede && (tevsikNakitKapali || TevsikOnerisiGerekli);

    /// <summary>t11 (tasarım hakemi YÜKSEK "iade bandı ters yönde"): nakit kuralı cümlesi paranın YÖNÜNE göre — satış iadesinde
    /// para müşteriye geri verilir ("iade edilemez — geri ödeyin"), hurda/ürün alışında satıcıya ödenir ("ödenemez — ödeyin"),
    /// öbür türlerde müşteriden alınır ("alınamaz — tamamını … alın"). Kural aynı (sunucunun tevsik kapısı), yalnız dil.</summary>
    internal string NakitBantMetni => tur switch
    {
        "SATIS_IADE" => Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).NakitIadeEdilemez,
        "HURDA_ALIS" or "ALIS" => Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).NakitOdenemez,
        "TAKAS" when takas.Fark is < 0m => Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).NakitOdenemez,   // t15: fark müşteriye ödenir
        _ => Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).NakitAlinamaz,
    };

    /// <summary>t6: BASİT yasal kapı bandı — tetikleyen kural gerçekten bu fişe uyuyorsa: MASAK · nakit kuralı (ödemede nakit
    /// varken) · sunucunun 400'ü (Peşin Müşteri kimlik bandı zaten söylüyorsa ikinci bant yok). Ödeme seçicisini açan
    /// OdemeKapisiAcik DEĞİŞMEDİ (kullanıcı karta geçebilsin); tezgâh fişinde tevsik önerisi ALDIM satırında (eski kural).</summary>
    /// <summary>SATIS-3 t16 (doğrulayıcı DÜŞÜK "aynı cümle iki kez"): BASİT'te üst bant (BasitSatisAkisi) kilit sebebiyle
    /// AYNI cümleyi söylüyorsa Kaydet'in altındaki satır görsel olarak gizlenir (tek bant kuralı); metin DOM'da ve
    /// düğmenin title'ında kalır (ekran okuyucu + UX-S30).</summary>
    /// <summary>SATIS-3 t16 (doğrulayıcı DÜŞÜK) — takas farkı kartla ödenirken seçilen POS cihazı; yalnız birden çok aktif cihaz
    /// varsa sorulur (TakasDto.PosCihazId, sunucu fail-closed). Tek cihazda/nakitte gövdeye null gider → sunucu seçer.</summary>
    private string? takasPosId;
    private bool TakasPosEksik => odemeTuru == "KART" && posCihazlari.Count > 1 && string.IsNullOrEmpty(takasPosId);
    private string? TakasPosId => odemeTuru == "KART" && !string.IsNullOrEmpty(takasPosId) ? takasPosId : null;

    internal bool KilitBantlaAyni(string kilit) =>
        Modu.BasitMi && BasitBantAcik && BasitBantMesaji is { } m && kilit == m;

    // ═══ t24 (orkestratör 02:30 · tasarım hakemi KRİTİK) — yasal kapı hakkında ekranda AYNI ANDA TEK, tutarlı mesaj ═══
    // Hakem: Peşin Müşteri + 304.872 ₺'de "Peşin satış: müşteri adı gerekmez." ile "Bu tutarda Peşin Müşteri olmaz" aynı
    // anda; 129 bin ₺ nakitte önce "Müşteri seçin", müşteri seçilince "Nakit alınmaz" (engeller tek tek). Kural TEK
    // KAYNAK: sunucunun Peşin K2 kapısı (TevsikKapisi.Dogrula: fiş ölçütü > had · MasakKapisi.DogrulaKumulatif: kademe ≠ Yok,
    // yani tutar ≥ kimlik eşiği) ve nakit kuralı; eşik SAYILARI sunucudan (/tevsik/gunluk, /masak/esikler), bilinmeyen
    // eşik kilitlemez (son söz sunucuda). POST gövdeleri DEĞİŞMEDİ.

    /// <summary>t24: Peşin Müşteri bu fişte kullanılamaz — sunucunun bildirdiği tevsik haddi ya da MASAK kimlik eşiğiyle.</summary>
    internal bool PesinEsikUstu => PesinSecili && (
        (tevsikHaddi is { } h && h > 0m && TevsikTutar > h && (tur != "TAKAS" || odemeTuru == "NAKIT"))
        || (masakEsik is { } me && me.KimlikIstenenTutar() > 0m && MasakTutar >= me.KimlikIstenenTutar()));

    /// <summary>t24: Peşin engeli varsa TEK cümle — nakit kuralı da tetikliyse iki engel birlikte ve sırayla
    /// ("Müşteri seçin ve ödemeyi kart ya da havaleyle alın."); yoksa null.</summary>
    internal string? YasalEngelMetni
    {
        get
        {
            var s3 = Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil);
            var al = NakitBantMetni == s3.NakitAlinamaz;
            if (PesinEsikUstu)
                return !NakitBandiVar ? PesinKilitMetni : al ? s3.PesinVeNakitAl : s3.PesinVeNakitOde;
            // gerçek müşteri: MASAK kimliği eksik + nakit kuralı → iki engel tek cümlede (kural alınamadıysa eski yol: hata kartı)
            if (MasakGerekli && NakitBandiVar)
                return NakitKilitCumlesi;   // YASAL-KAPI-3: nakit kuralı tetikliyken kimlik formu açılmaz — önce ödeme türü (tek engel cümlesi)
            return null;
        }
    }

    /// <summary>t24: BASİT üst bant — Peşin engeli de bandı açar; bant metni engellerin TEK cümlesi. MASAK tamam ama nakit
    /// kuralı sürüyorsa bant "gereken alanlar açıldı" demez, Kaydet'in sebebiyle AYNI nakit cümlesini söyler.</summary>
    internal bool BasitBantAcik => YasalBantAcik || PesinEsikUstu;
    internal string? BasitBantMesaji => YasalEngelMetni
        ?? (!MasakGerekli && NakitBandiVar ? NakitKilitCumlesi   // YASAL-KAPI-3: bant ile Kaydet sebebi AYNI cümle (sayılı had)
        : MasakGerekli && MasakTamam && NakitBandiVar ? (YasalKilitSebebi ?? NakitBantMetni) : null);

    /// <summary>t24: ödeme sütunundaki kısa durum ("Müşteri seçin"/"Nakit alınmaz") aynı engeli sayfa TEK cümleyle söylüyorsa görsel gizli.</summary>
    internal bool OdemeDurumuGizli => YasalEngelMetni is not null
        || (Modu.BasitMi && BasitBantAcik && NakitBandiVar && BasitBantMesaji is not null);

    /// <summary>t21 (hasım doğrulayıcı ORTA): MASAK kuralı ALINAMADIYSA (masakHata) BASİT üst bandı "gereken alanlar açıldı"
    /// demez — doldurulacak alan yok; gerçek engel MASAK hata kartında (Yeniden dene) ve Kaydet sebebinde yazar.</summary>
    private bool YasalBantAcik =>
        YasalKapiAcik && !(masakHata && MasakGerekli) && !(TezgahFisi && TevsikOnerisiGerekli && !MasakGerekli)
        && (MasakGerekli || (NakitOdemede && (tevsikNakitKapali || TevsikOnerisiGerekli)) || (kapiSunucudanAcildi && !pesinKimlikGerekli));

    /// <summary>t21 — gönderilecek bir satırın kasası boşsa sebep: uyan kasa yoksa hangi para biriminin kasası eksik, varsa "kasa seçin".</summary>
    private string? KasasizSatirSebebi() =>
        FisFark.Gonderilecek(bacaklar).FirstOrDefault(b => FisAltinBacagi.KasaAlir(b) && string.IsNullOrEmpty(b.KasaId)) is not { } kb ? null
        : FisHesap.BacakKasalari(kb, kasalar).Any() ? Dil.T.Islem.KasaSecin
        : string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).OdemeKasasiYok, kb.Arac == "DOVIZ" ? kb.ParaBirimi : "TL");

    /// <summary>SATIS-3 (tur.mjs 28.09): karma kilit sebebi "Ödeme bacakları" diye başlık basıyordu (UX-S30 — kilitli düğme
    /// EKSİK koşulu söyler). Önce eski altının ilk eksiği, sonra tutarı boş ödeme satırı; ikisi de yoksa eski metin.</summary>
    private string KarmaKilitSebebi() =>
        (FisFark.DovizKuruEksik(bacaklar) ? Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).DovizKuruYok : null)   // t21: kur yokken döviz satırı düşmez, fiş kilitlenir
        ?? KasasizSatirSebebi()   // t21 (canlı ölçüm): kasası olmayan satır "Ödemeyi tamamlayın" diye belirsiz kalmaz (UX-S30)
        ?? FisAltinBacagi.KilitSebebi(bacaklar, Dil.Dil)
        ?? (FisFark.Gonderilecek(bacaklar).Count == 0 ? Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).OdemeTutariGirin : null)   // k2: boş satır tek başına kilit değil
        ?? (NakitKapali ? Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).NakitAlinamaz : null)
        ?? (FisFark.Durum(ToplamTl - FisHesap.BacakToplami(bacaklar)) switch
        {
            FarkDurumu.Eksik => string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).FarkEksik, Bicim.Tl(ToplamTl - FisHesap.BacakToplami(bacaklar))),
            FarkDurumu.Fazla => string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).FarkFazla, Bicim.Tl(FisHesap.BacakToplami(bacaklar) - ToplamTl)),
            _ => Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).OdemeyiTamamlayin,
        });   // t3 hakemi: "Ödeme bacakları" başlığı kilit sebebi olamaz (jargon + koşul söylemiyor)

    // ═══ t10 (tasarım hakemi KRİTİK, 28.09 gece) — istemcinin BİLDİĞİ yasal engel + sunucu mesajının dili ══════════
    // ⚠ Eşik SAYISI koda gömülmez: kilit yalnız sunucunun bildirdiği had (tevsikHaddi, /tevsik/gunluk) BİLİNİRKEN çalışır;
    //   bilinmiyorsa istemci kilitlemez ve son söz sunucudadır (fail-closed kapı sunucuda — değişmedi). POST gövdeleri aynı.

    /// <summary>MASAK reddi sunucudan geldi (kümülatif eşik istemcide bilinmez) → MASAK alanları açılır.</summary>
    // masakSunucudanGerekli: YASAL-KAPI-GERI ile reddin İZİ (Islemler.YasalKapiGeri.cs) — fiş tutarı reddin altına inince düşer.

    // ═══ t19 (orkestratör 15:40 + tasarım hakemi KRİTİK) — MASAK kapısı TEK KAYNAKTAN ══════════════════════════
    // Kademe (hangi alanlar) ve bağlantılı işlem kümülatifi SUNUCUNUN /masak/gereksinim ucundan gelir: istemci
    // fişin TOPLAMINI (MasakTutar — sunucunun kapıya verdiği tutar) ve gerçek müşteriyi sorar; sunucu aynı gün aynı
    // müşteriyle yapılan işlemleri ekleyip kademeyi döndürür (MasakKapisi.DogrulaKumulatif ile aynı ölçü).
    // Peşin Müşteri kümülatife girmez (sunucu kuralı) → müşteri parametresi gönderilmez.

    /// <summary>Gereksinim sorgusunda gönderilecek müşteri: yalnız Peşin Müşteri dışındaki gerçek müşteri.</summary>
    private string? MasakSorguCari => string.IsNullOrEmpty(cariId) || PesinSecili ? null : cariId;

    /// <summary>Sunucuya gereksinim sorulur mu: tutar eşikte/üstünde, sunucu reddetmiş ya da gerçek müşteriyle tutarlı fiş var.</summary>
    private bool MasakSorulur => masakEsik is { } me
        && (masakSunucudanGerekli || MasakTutar >= me.KimlikIstenenTutar() || (MasakTutar > 0m && MasakSorguCari is not null));

    /// <summary>Aynı soru iki kez sorulmaz: tutar + müşteri + red durumu değişince yeniden sorulur.</summary>
    private string MasakSorguAnahtari =>
        $"{MasakTutar.ToString(CultureInfo.InvariantCulture)}|{MasakSorguCari}|{masakSunucudanGerekli}";

    /// <summary>t19: MASAK formu her değişimde sayfayı yeniden çizdirir — Kaydet kilidi (MasakTamam) formun GÜNCEL
    /// hâlini okur. Eskiden yalnız klavye olayı sayfaya kabarcıklanınca (ProKisayol) güncelleniyordu; fareyle
    /// yapıştırma/otomatik doldurma sonrası Kaydet kilitli kalıyordu (bUnit ile yakalandı).</summary>
    private void MasakDegisti() { }

    /// <summary>
    /// Sunucuya fiş toplamı + müşteriyle sorar. Sunucu reddettiği hâlde (kümülatif) cevap kimlik istemiyorsa
    /// (ör. müşteri değişti, eski sunucu) güvenli yöne düşülür: en az kimlik eşiğiyle yeniden sorulur — kullanıcı
    /// alanları görmeden yeniden reddedilip çıkmaza girmez.
    /// </summary>
    /// <summary>t20: gereksinim ucu düştü → kart açık kalır, Türkçe bant + "Yeniden dene"; anahtar "soruldu" sayılır (istek fırtınası yok).</summary>
    private bool masakHata;


    /// <summary>
    /// Tutar değişince sunucudan gereksinim çeker. Aynı tutar için tekrar
    /// çağrılmaz — kullanıcı yazarken her tuşta istek atılmaz.
    /// </summary>
    private async Task GereksinimTazele()
    {
        if (!MasakSorulur) { masakGereksinim = null; gereksinimAnahtari = null; masakHata = false; return; }
        if (MasakSorguAnahtari == gereksinimAnahtari) return;
        // t20 (doğrulayıcı KRİTİK): anahtar İSTEKTEN ÖNCE "soruldu" işaretlenir — uç hata verse de aynı soru
        // OnAfterRenderAsync döngüsünde yeniden sorulmaz. Yeniden deneme yalnız anahtar değişince ya da
        // "Yeniden dene" düğmesiyle. Cevap yokken MasakGerekli ise Kaydet kilitli kalır (MasakTamam, fail-closed).
        gereksinimAnahtari = MasakSorguAnahtari;
        try { masakGereksinim = await MasakGereksinimSor(); masakHata = false; }   // t19: fiş toplamı + müşteri
        catch { masakGereksinim = null; masakHata = true; }
    }

    /// <summary>t20: gereksinim ucu düştükten sonra kullanıcının elle yeniden sorması (tek istek).</summary>
    private async Task MasakYenidenDene()
    {
        gereksinimAnahtari = null;
        await GereksinimTazele();
    }
    private async Task<MasakGereksinim> MasakGereksinimSor()
    {
        var g = await Uyum.Gereksinim(MasakTutar, MasakSorguCari);
        if (masakSunucudanGerekli && !g.KimlikGerekli && masakEsik is { } me)
            g = await Uyum.Gereksinim(Math.Max(MasakTutar, me.KimlikIstenenTutar()));
        return g;
    }

    /// <summary>Peşin Müşteri seçili mi.</summary>
    private bool PesinSecili => !string.IsNullOrEmpty(pesinCariId) && cariId == pesinCariId;

    /// <summary>İade edilecek tutar — FisIade.OrantiliTutar ve sunucuyla aynı hesap (orijinal tutarın adede düşen payı).</summary>
    private decimal IadeTutar => iadeSecili is null || iadeSecili.ToplamAdet <= 0m ? 0m
        : Math.Round(Math.Abs(iadeSecili.TlTutari) * iadeAdet / iadeSecili.ToplamAdet, 2, MidpointRounding.AwayFromZero);

    /// <summary>Tevsik kapısına giden tutar — sunucunun kapıya verdiği: satış/alış fiş toplamı, hurda has×fiyat, iade tutarı.</summary>
    private decimal TevsikTutar => tur switch
    {
        "HURDA_ALIS" => MasakTutar,
        "SATIS_IADE" or "ALIS_IADE" => IadeTutar,
        "TAKAS" => TakasTevsikTutari(takas.Fark),   // DEGISIM-GUNSONU t2: sunucu gibi kuruşa yuvarlanmış |fark|   // t15 (tasarım hakemi KRİTİK): sunucu takasta NAKİT FARKI tevsik kapısına verir (SatisModulu.Takas)
        _ => ToplamTl,
    };

    /// <summary>
    /// Kaydet'i kilitleyen, istemcinin sunucu eşiğiyle BİLDİĞİ yasal engel (null = yok). Hakem: "Peşin + 127.937 ₺ Nakit'te
    /// Kaydet etkin, tıklayınca aynı şey iki bantla söyleniyor" · "Hurda 61.429 ₺'de 'Nakit — sistem belirledi', Kaydet etkin".
    /// </summary>
    private string? YasalKilitSebebi =>
        YaptirimKilidi is { } yk ? yk   // PAKET YASAL-3 (§ 4.4): yasal liste eşleşmesi — müşteri değişene kadar kilit
        : YasalEngelMetni is { } engel ? engel   // t24: Peşin eşiği (+ nakit kuralı) TEK cümle
        : tevsikHaddi is not { } h || h <= 0m ? null
        : !KarmaAcik && TevsikOnerisiGerekli && (IadeKipi || tur == "TAKAS") ? NakitBantMetni   // t15 (hakem YÜKSEK "iki metin"): iade/takasta kilit sebebi bantla AYNI cümle
        : !KarmaAcik && TevsikOnerisiGerekli ? string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil).NakitHaddiKilit, Bicim.Tl(h))
        : null;

    /// <summary>Son sunucu reddinin ham metni — BASİT'te kısa cümlenin altında katlı "Bilgi" içinde (mevzuat dayanağı (i)'de kalır).</summary>
    private string? sunucuHamMesaj;

    /// <summary>Katlı ayrıntı yalnız bant hâlâ o reddi gösterirken ve kısa cümle ham metinden farklıyken çizilir.</summary>
    private string? SunucuAyrinti => hata is not null && hata == sunucuSadeMesaj && sunucuHamMesaj is { } ham && TlSembolu(ham) != hata ? ham : null;
    private string? sunucuSadeMesaj;

    private const string PesinReddi = "Peşin Müşteri ile bu tutarda";

    /// <summary>Sunucunun reddi MASAK alanlarını mı istiyor (alan anahtarı "masak…" ya da metinde MASAK; Peşin reddi hariç).</summary>
    private static bool MasakReddi(ApiHata ex) =>
        !ex.Message.Contains(PesinReddi, StringComparison.Ordinal)
        && (ex.Alanlar.Keys.Any(k => k.StartsWith("masak", StringComparison.OrdinalIgnoreCase))
            || ex.Message.Contains("MASAK", StringComparison.Ordinal));

    /// <summary>
    /// Sunucu mesajı ekrana: BASİT'te tezgâhtar diliyle tek kısa cümle (mevzuat dayanağı, madde no, ham alan anahtarı,
    /// büyük harf vurgusu yok — hakem KRİTİK); PRO'da mesaj aynen kalır. İkisinde de "127.937,79 TL" → Bicim.Tl ("₺").
    /// Anayasa md.1: hata yutulmaz — tanınmayan mesaj parantez içi dayanaklar çıkarılarak AYNEN gösterilir.
    /// </summary>
    private string SunucuMesaji(ApiHata ex)
    {
        sunucuHamMesaj = ex.Message ?? "";
        return sunucuSadeMesaj = SadeMesaj(ex);
    }

    private string SadeMesaj(ApiHata ex)
    {
        var m = ex.Message ?? "";
        // TOPLU-1 (keşif P5 01.10, YÜKSEK): nakit kasa kapısı reddi ("13407.25 TRY var, 21984.00 TRY isteniyor") iki
        // modda da ham basılmaz — sayılar ayrıştırılıp tr-TR biçimli sözlük cümlesiyle (Gider ekranıyla aynı kural).
        if (KasaBakiyeMesaji.Bicimle(m, Dil) is { } kasaMesaji) return kasaMesaji;
        var s3 = Kuyumcu.Core.I18n.Satis3Sozlugu.Al(Dil.Dil);
        if (Modu.BasitMi)
        {
            // Peşin reddi: "gerçek müşteri seçin" bandı (#islem-pesin-esik) eylemi söyler; sunucu cümlesi aynen kalır (S23E-05).
            if (m.Contains(PesinReddi, StringComparison.Ordinal)) return TlSembolu(m);
            if (MasakReddi(ex)) return s3.SunucuKimlikGerekli;
            if (ex.Kod == "NAKIT_SINIRI" || m.Contains("evsik", StringComparison.OrdinalIgnoreCase))   // YASAL-3: kodla tanınır
                return tevsikHaddi is { } h && h > 0m ? string.Format(s3.NakitHaddiKilit, Bicim.Tl(h)) : NakitBantMetni;
            if (m.Contains("md.234", StringComparison.OrdinalIgnoreCase) || m.Contains("gider pusula", StringComparison.OrdinalIgnoreCase))
                return s3.SunucuSaticiKimlik;
            m = Regex.Replace(m, @"\s*\([^)]*\)", "");
        }
        return TlSembolu(Regex.Replace(m, @"\s*\((?:masak|hurda|altin|odeme)\.[\w.\[\]]+\)", ""));   // ham alan anahtarı ekrana basılmaz (iki modda)
    }

    /// <summary>Sunucu metnindeki "127.937,79 TL" tutarları ekranın tek para biçimine (Bicim.Tl) çevrilir.</summary>
    public static string TlSembolu(string m) =>
        Regex.Replace(m, @"(\d{1,3}(?:\.\d{3})*(?:,\d{2})?) TL\b", x =>
            decimal.TryParse(x.Groups[1].Value, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out var d) ? Bicim.Tl(d) : x.Value);
}
