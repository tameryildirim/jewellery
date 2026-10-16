using System.Globalization;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P5 + SATIS-4 (Kurul 5, 01.10) — satış ekranı davranışları.
// (a) Tutar nakit sınırını AŞINCA ödeme varsayılanı nakit dışına döner (satışta Kart,
//     para dışarı çıkan türlerde Havale) — form kendi reddedeceği seçenekle açılmaz.
//     Sınır SAYISI istemciye gömülmez: yalnız sunucunun bildirdiği had (tevsikHaddi,
//     /tevsik/gunluk) BİLİNİRKEN çalışır; had bilinmiyorsa davranış eskisi gibidir
//     (Nakit kalır, son söz sunucuda — fail-closed kapı sunucuda DEĞİŞMEDİ).
//     Kullanıcı ödemeyi ELLE seçtiyse (Nakit dahil) üzerine YAZILMAZ; tutar sınırın
//     altına inerse ve ödeme hâlâ otomatik seçilen değerse Nakit'e geri döner.
// (b) Tek kimlik formu: Hurda alışında MASAK kimlik kutusu açıldığında satıcının adı,
//     TCKN'si ve adresi O kutudan alınır (VUK md.234 alanları gövdede AYNEN gider —
//     satici bloğu doldurulur, yalnız ekranda ikinci kez sorulmaz). Belge türü TC kimlik
//     değilse (pasaport vb.) satıcı TCKN kutusu görünür kalır (değer uydurulmaz).
//     MASAK kutusu seçili GERÇEK müşteriden ve önceden yazılmış satıcı alanlarından
//     yalnız BOŞ alanlara ön dolar (kullanıcının yazdığı ezilmez).
// (d) Son kayıt satırı her türde ve her modda bir sonraki kayda kadar görünür.
// ⚠ Kaydet() / POST gövdeleri DEĞİŞMEDİ. Islemler.razor ≤ 1.400 satır (S21).
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private KesifP5Metinleri P5 => KesifP5Sozlugu.Al(Dil.Dil);

    // ── (a) ödeme varsayılanı ────────────────────────────────────────────────
    /// <summary>Kullanıcı ödeme türünü bu fişte elle seçti mi (seçiciden). Fiş boşalınca sıfırlanır.</summary>
    private bool odemeElleSecildi;
    /// <summary>Otomatik seçilen ödeme (KART/HAVALE) — null: otomatik seçim yok.</summary>
    private string? odemeOtomatik;

    /// <summary>Seçiciden ödeme değişti (FisOdemeKasa @bind-OdemeTuru:after) — elle seçim; otomatik not düşer.</summary>
    private void OdemeElleDegisti() { hata = null; odemeElleSecildi = true; odemeOtomatik = null; }

    /// <summary>
    /// SATIS-4 (a): bu fişte NAKİT sunucunun bildirdiği sınırı aşıyor mu — yalnız had BİLİNİRKEN
    /// (tevsikHaddi &gt; 0) ya da sunucu bugün nakdi kapattıysa (tevsikNakitKapali) ve fişte tutar varsa.
    /// Takas farkı satış gibi, iade alış gibi denetlenir (sunucu tevsik.Dogrula ile aynı eşleme).
    /// </summary>
    internal bool NakitSiniriAsildi =>
        TevsikTutar > 0m
        && ((!IadeKipi && tur == "TAKAS"
                ? TakasOdemeVarsayilani(takas.Fark, tevsikHaddi) is not null   // DEGISIM-GUNSONU t2: değişimin TEK kuralı (Islemler.Degisim.cs)
                : tevsikHaddi is { } h && h > 0m
                  && TevsikOneri.Gerekli("NAKIT", IadeKipi ? "ALIS" : tur, TevsikTutar, h))
            || tevsikNakitKapali);

    /// <summary>Nakit dışı varsayılan: para müşteriden ALINIYORSA Kart, müşteriye/satıcıya ÖDENİYORSA Havale.</summary>
    internal string? NakitDisiVarsayilan => tur switch
    {
        "SATIS" => "KART",
        "TAKAS" => TakasNakitDisiYon(takas.Fark),   // DEGISIM-GUNSONU: tek kaynak (Islemler.Degisim.cs)
        "HURDA_ALIS" or "ALIS" or "SATIS_IADE" or "ALIS_IADE" => "HAVALE",
        _ => null,
    };

    /// <summary>
    /// OnAfterRenderAsync'te her çizimden sonra çağrılır; durum değiştiyse true (sayfa yeniden çizilir).
    /// Döngü yok: yalnız NAKIT→hedef ya da otomatik hedef→NAKIT geçişinde true döner.
    /// </summary>
    internal bool OdemeVarsayilaniniUygula()
    {
        if (TevsikTutar <= 0m)
        {
            // boş fiş: sıradaki fişte seçim yeniden varsayılandan. Canlı ölçümde bulundu (01.10): büyük kartlı satış
            // kaydedilince önceki fişin sunucu önizlemesi ("işlem tutarı kuralı devrede") BOŞ fişte kalıyor, 0 ₺ ekranda
            // "Bu tutarda nakit alınamaz" bandı ve açık ödeme seçicisi çıkıyordu → boş fişte bu bayrak düşer
            // (ilk kalemde TevsikOnizle sunucudan yeniden sorar; kapı sunucuda DEĞİŞMEDİ).
            // Vazgeç ile boşalan fişte OTOMATİK seçilen ödeme de varsayılana (Nakit) döner — sıradaki fiş eşik altındaysa
            // önceki fişin "Kart"ı notsuz kalmasın (canlı ölçümde bulundu). Elle seçim bu dalda korunur (eski davranış).
            var degisti = false;
            if (odemeOtomatik is not null && odemeTuru == odemeOtomatik && !karmaOdeme) { odemeTuru = "NAKIT"; degisti = true; }
            odemeElleSecildi = false; odemeOtomatik = null;
            if (!tevsikNakitKapali || kalemler.Count > 0 || karmaOdeme) return degisti;
            tevsikNakitKapali = false;
            return true;
        }
        if (karmaOdeme || odemeElleSecildi || NakitDisiVarsayilan is not { } hedef) return false;
        if (NakitSiniriAsildi)
        {
            // DEGISIM-GUNSONU (canlı ölçümde bulundu): değişimde farkın YÖNÜ değişirse (müşteri öder ↔ müşteriye ödenir)
            // OTOMATİK seçilen araç da yön değiştirir (Kart ↔ Havale); elle seçim yine ezilmez (odemeElleSecildi yukarıda).
            if (odemeOtomatik is not null && odemeTuru == odemeOtomatik && hedef != odemeOtomatik)
            {
                odemeTuru = hedef; odemeOtomatik = hedef; hata = null;
                return true;
            }
            if (odemeTuru != "NAKIT") return false;
            odemeTuru = hedef; odemeOtomatik = hedef; hata = null;
            return true;
        }
        if (odemeOtomatik is not null && odemeTuru == odemeOtomatik)
        {
            odemeTuru = "NAKIT"; odemeOtomatik = null;
            return true;
        }
        return false;
    }

    /// <summary>
    /// KESIF-P5 t2 (tasarım hakemi KRİTİK, 01.10): kayıttan sonra boş fişte PRO "Yasal kapılar" paneli önceki fişin
    /// önizlemesini (işlem tutarı kuralı devrede) taşıyıp 0 ₺'de "Aşıldı" yazıyordu. Sunucunun kuralı fiş tutarına bağlıdır
    /// (TevsikController: devrede = |fisToplamTl| &gt; had) → boş fişte devrede DEĞİLDİR; bayrak düşer, had/kalan aynen kalır.
    /// İstek ATILMAZ (kayıt sonrası sorgu YASAL-2 sözleşmesi); ilk kalemde TevsikOnizle sunucudan yeniden sorar. Kapı sunucuda DEĞİŞMEDİ.
    /// </summary>
    private void ProYasalBosFis()
    {
        if (proTevsik is { IslemTutariKuraliDevrede: true } p) proTevsik = p with { IslemTutariKuraliDevrede = false };
    }

    /// <summary>Ödeme seçicisinin altındaki kısa not: "Tutar {had} nakit sınırını aştığı için ödeme Kart olarak seçildi." (had sunucudan)</summary>
    internal string? OdemeOtomatikNotu =>
        odemeOtomatik is null || odemeTuru != odemeOtomatik || karmaOdeme ? null
        : string.Format(odemeOtomatik == "KART" ? P5.OdemeOtomatikKart : P5.OdemeOtomatikHavale,
            tevsikHaddi is { } h && h > 0m ? Bicim.Tl(h) : "");

    // ── (b) tek kimlik formu ─────────────────────────────────────────────────
    /// <summary>MASAK kimlik kutusu ekranda mı (Islemler.razor'daki koşulun aynısı: gereksinim yüklendi).</summary>
    internal bool MasakKutusuAcik => MasakGerekli && !PesinEsikUstu && masakGereksinim is not null;

    /// <summary>Hurda alışında satıcı kimliği MASAK kutusundan alınır (bireysel/esnaf satıcı + MASAK kutusu açık).</summary>
    /// PARA-DEMO-2 tur 5 (doğrulayıcı YÜKSEK): DEĞİŞİMDE de aynı kural — vatandaştan alınan eski altının satıcısı
    /// müşterinin kendisidir; TCKN ve adres MASAK kutusunda BİR KEZ sorulur (esnaf satıcı takasta zaten kilitli).
    internal bool KimlikBirlesik => MasakKutusuAcik && (
        (tur == "HURDA_ALIS" && hurda.SaticiTipi is "NIHAI_TUKETICI" or "ESNAF_MUAF")
        || (tur == "TAKAS" && takas.Satici.SaticiTipi == "NIHAI_TUKETICI"));

    /// <summary>Tek kimlik formunda satıcı girdisinin sahibi: hurda alışında hurda, değişimde takas satıcısı.</summary>
    internal Kuyumcu.Web.Bilesenler.Fis.HurdaGirisi BirlesikSatici => tur == "TAKAS" ? takas.Satici : hurda;

    /// <summary>Son ön dolumun anahtarı (kimlik kutusu nesnesi · müşteri · tek-form durumu).</summary>
    private object? kimlikOnDolum;

    /// <summary>MASAK belgesi TC kimlik mi — öyleyse satıcı TCKN'si belge numarasıdır (ikinci kez sorulmaz).</summary>
    internal bool MasakBelgesiTckn => masak.BelgeTuru == "TC_KIMLIK";

    /// <summary>Yalnız boş alana yazar (kullanıcının yazdığı ezilmez).</summary>
    private static bool BosaYaz(string? mevcut, string? yeni, Action<string> yaz)
    {
        if (!string.IsNullOrWhiteSpace(mevcut) || string.IsNullOrWhiteSpace(yeni)) return false;
        yaz(yeni.Trim()); return true;
    }

    /// <summary>
    /// SATIS-4 (b): MASAK kutusu seçili GERÇEK müşteriden (ad · TCKN · adres) ve — hurdada — önceden yazılmış
    /// satıcı alanlarından BOŞ alanlara ön dolar; ardından hurdada satıcı alanları MASAK kutusundan eşitlenir.
    /// Değişiklik olduysa true.
    /// </summary>
    internal bool KimlikEsitle()
    {
        if (!MasakKutusuAcik) return false;
        var degisti = false;
        var tcknKabul = MasakBelgesiTckn;
        // Ön dolum BİR KEZ (aynı kimlik kutusu + aynı müşteri + aynı tek-form durumu için): kullanıcı dolan alanı
        // silerse yeniden doldurulmaz — değer onun kararıdır. Fiş sıfırlanınca kutu yeni nesnedir → yeniden ön dolar.
        var anahtar = (masak, cariId, KimlikBirlesik);
        if (!Equals(kimlikOnDolum, anahtar))
        {
            kimlikOnDolum = anahtar;
            if (GercekMusteri is { } m)
            {
                degisti |= BosaYaz(masak.AdSoyad, m.Ad, v => masak.AdSoyad = v);
                // yalnız müşteri kartındaki kimlik TÜRÜ TCKN ise (vergi kimlik no belge numarası sayılmaz)
                if (tcknKabul && m.KimlikTuru == "TCKN" && m.VergiNo is { Length: 11 } vn && vn.All(char.IsAsciiDigit))
                    degisti |= BosaYaz(masak.BelgeNo, vn, v => masak.BelgeNo = v);
                degisti |= BosaYaz(masak.Adres, m.Adres, v => masak.Adres = v);
            }
            if (KimlikBirlesik)
            {
                var sb = BirlesikSatici;
                degisti |= BosaYaz(masak.AdSoyad, sb.SaticiAd, v => masak.AdSoyad = v);
                if (tcknKabul) degisti |= BosaYaz(masak.BelgeNo, sb.TcknRakam, v => masak.BelgeNo = v);
                degisti |= BosaYaz(masak.Adres, sb.SaticiAdres, v => masak.Adres = v);
            }
        }
        if (!KimlikBirlesik) return degisti;
        // satıcı ← MASAK (tek kaynak): gövdedeki VUK md.234 alanları kutudan gelir
        var s = BirlesikSatici;
        if (s.SaticiAd != masak.AdSoyad) { s.SaticiAd = masak.AdSoyad; s.SaticiAdCariden = false; degisti = true; }
        if (s.SaticiAdres != masak.Adres) { s.SaticiAdres = masak.Adres; degisti = true; }
        if (tcknKabul && s.SaticiTckn != masak.BelgeNo) { s.SaticiTckn = masak.BelgeNo; degisti = true; }
        return degisti;
    }

    /// <summary>OnAfterRenderAsync kancası: (a) + (b). Değişiklik olduysa true (sayfa yeniden çizilir).</summary>
    internal bool Satis4Uygula()
    {
        var a = OdemeVarsayilaniniUygula();
        var c = TakasSaticiOnDolumEsitle();   // DEGISIM-GUNSONU t2: Peşin Müşteri adı satıcıya ön dolmaz
        var b = KimlikEsitle();
        return a || b || c;
    }

    // ── (d) kalıcı son kayıt ─────────────────────────────────────────────────
    /// <summary>
    /// SATIS-4 (d): kaydedilenin özeti — TÜR · SAAT · TUTAR — bir sonraki Kaydet'e kadar yazdır bağlantısının
    /// yanında durur (başarı bandı 3 sn'de kapanır; özet kapanmaz). Sunucu okunur fiş numarası döndürmüyor
    /// (fisId ULID — ekrana basılmaz); numara UYDURULMAZ. Tutar kayıttan ÖNCE ekranın gösterdiği işlem tutarıdır.
    /// </summary>
    private string? SonKayitMetni()
    {
        var tutar = TevsikTutar > 0m ? TevsikTutar : ToplamTl;
        if (tutar <= 0m && kalemler.Count == 0) return null;
        return string.Format(P5.SonKayit, SeciliTurAdi,
            Bicim.IsletmeSaati(DateTime.UtcNow), Bicim.Tl(tutar));
    }
}
