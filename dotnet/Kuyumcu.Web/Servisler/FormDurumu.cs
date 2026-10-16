using System.Text.RegularExpressions;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Web.Servisler;

// ═════════════════════════════════════════════════════════════════════════════
// FORM DURUMU — paket UX-1 Faz C, adım C0 (ortak form altyapısı).
//
// NEDEN VAR (QA turu 1 ve 2'nin aynı bulgusu, 6 ekranda tekrar eden):
//   "Boş adla Kaydet sessizce etkisiz; dialog içinde mesaj yok, istek yok."
// Bugüne kadar her sayfa bunu `disabled="@(string.IsNullOrWhiteSpace(ad))"`
// ile çözüyordu: düğme kilitleniyor ama NEDEN kilitli olduğu hiçbir yerde
// yazmıyordu (UX-S30 ihlali) ve sunucunun alan→mesaj sözlüğü
// (ApiHata.Alanlar, UX-4d madde 8) ekrana HİÇ bağlanmıyordu.
//
// ⚠ EditForm KULLANILMADI — bilinçli sapma, raporda gerekçeli:
//   (1) MÜHÜR 5 KOŞUL 3 "record dondurma": EditForm her form için bir model
//       tipi ister; bu tipler Pages/ altında doğar ve denetim.sh'ın
//       "Pages içinde istemci record" sayacını (tavan 122) kırardı.
//   (2) EditForm'a geçiş 7 sayfadaki HER @bind'ı yeniden yazmayı gerektirir;
//       brif mührü K4 "mevcut element id'leri DEĞİŞMEZ" der, tek dokunuşta
//       bu ölçekte bir yeniden yazım id sözleşmesini riske atar.
//   Hedef (Türkçe alan altı mesaj · sessiz etkisizlik yasağı · odak ilk
//   hatalı alana · sunucu sözlüğünün ekrana bağlanması) BİREBİR karşılanır;
//   denetim.sh sayaç 8 (EditForm ≤ ValidationMessage) etkilenmez.
//
// KULLANIM (tek kalıp — ikinci kalıp AÇILMAZ):
//     form.Temizle();
//     form.Zorunlu("ad", yeniAd, Dil);
//     form.Pozitif("gram", yeniGram, Dil);
//     if (!form.Gecerli) { /* gönderme — ekran alan altı mesajı çizer */ return; }
//     try { ... } catch (ApiHata ex) { form.Sunucudan(ex); hata = ex.Message; }
//
// ⚠ Bu sınıf ekranda HİÇBİR ŞEY çizmez ve HTTP bilmez; çizimi AlanHatasi
//   bileşeni, isteği sayfa yapar. Sunucudaki hiçbir kapı buna bakmaz.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tek bir formun alan→Türkçe mesaj defteri (istemci + sunucu birleşik).</summary>
public sealed class FormDurumu
{
    private readonly Dictionary<string, string> alanlar = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> sira = [];

    /// <summary>Hiç hata yoksa true. Boş formda da true'dur — doğrulama ÇAĞRILMALIDIR.</summary>
    public bool Gecerli => alanlar.Count == 0;

    /// <summary>Hata sayısı (bUnit ve mutasyon testleri bunu okur).</summary>
    public int HataSayisi => alanlar.Count;

    /// <summary>İlk hatalı alanın anahtarı — odak buraya taşınır (C0-d).</summary>
    public string? IlkHatali => sira.Count > 0 ? sira[0] : null;

    public bool Var(string alan) => alanlar.ContainsKey(alan);

    public string? Mesaj(string alan) => alanlar.TryGetValue(alan, out var m) ? m : null;

    /// <summary>
    /// Girdiye eklenecek sınıf — hatalı alan kenarlıkla işaretlenir.
    /// Sınıf app.css'te ZATEN VARDI (<c>.girdi.hatali</c>, satır 223); yeni bir
    /// stil adı açılmaz.
    /// </summary>
    public string? Sinif(string alan) => alanlar.ContainsKey(alan) ? "hatali" : null;

    public void Temizle()
    {
        alanlar.Clear();
        sira.Clear();
    }

    /// <summary>
    /// PAKET Q10-5 (QA 10 bulgusu: "alan hata mesajı alan düzeltildikten sonra
    /// ekranda kalıyor" — /fiyatlandirma, /vezne, /banka, /urunler, /hurda):
    /// kullanıcı alanın değerini DEĞİŞTİRDİĞİ anda o alanın eski mesajı düşer.
    /// Yeni değer hâlâ hatalıysa bir sonraki gönderimde doğrulama yeniden
    /// yazar; sunucu da son sözü söyler. Girdi ailesi (FormAlani) bunu kendi
    /// değişim işleyicisinde çağırır — sayfalar ikinci bir kalıp açmaz.
    /// </summary>
    /// <returns>Alanın silinecek bir mesajı varsa true.</returns>
    public bool Sil(string? alan)
    {
        if (string.IsNullOrWhiteSpace(alan) || !alanlar.Remove(alan)) return false;
        sira.RemoveAll(a => string.Equals(a, alan, StringComparison.OrdinalIgnoreCase));
        return true;
    }

    /// <summary>Alan mesajını ekler. Aynı alana ikinci mesaj YAZILMAZ (ilk sebep kalır).</summary>
    public void Ekle(string alan, string mesaj)
    {
        if (string.IsNullOrWhiteSpace(alan) || string.IsNullOrWhiteSpace(mesaj)) return;
        if (alanlar.ContainsKey(alan)) return;
        alanlar[alan] = mesaj;
        sira.Add(alan);
    }

    // ── İstemci kuralları ────────────────────────────────────────────────────
    // Hepsi sunucudaki kuralın AYNISIDIR ve onu TEKRARLAMAZ: son sözü sunucu
    // söyler, buradaki kontrol kullanıcıyı 400'e sürüklememek içindir.

    public bool Zorunlu(string alan, string? deger, DilServisi dil)
    {
        if (!string.IsNullOrWhiteSpace(deger)) return true;
        Ekle(alan, dil.T.Kabuk.ZorunluAlan);
        return false;
    }

    public bool Pozitif(string alan, decimal? deger, DilServisi dil)
    {
        if (deger is > 0m) return true;
        Ekle(alan, dil.T.Kabuk.SifirdanBuyuk);
        return false;
    }

    public bool NegatifDegil(string alan, decimal? deger, DilServisi dil)
    {
        if (deger is null || deger >= 0m) return true;
        Ekle(alan, dil.T.Kabuk.NegatifOlamaz);
        return false;
    }

    /// <summary>Boşsa geçer (alan isteğe bağlı); doluysa e-posta biçimi aranır.</summary>
    public bool Eposta(string alan, string? deger, DilServisi dil)
    {
        if (string.IsNullOrWhiteSpace(deger)) return true;
        if (EpostaKalibi.IsMatch(deger.Trim())) return true;
        Ekle(alan, dil.T.Kabuk.EpostaGecersiz);
        return false;
    }

    /// <summary>
    /// Boşsa geçer. Doluysa sunucunun kuralının TA KENDİSİ çağrılır
    /// (Kuyumcu.Core.Domain.Iletisim.TelefonGecerli) — ikinci kural yazılmaz.
    /// </summary>
    public bool Telefon(string alan, string? deger, DilServisi dil)
    {
        if (Kuyumcu.Core.Domain.Iletisim.TelefonGecerli(deger)) return true;
        Ekle(alan, dil.T.Kabuk.TelefonGecersiz);
        return false;
    }

    public bool EnAzKarakter(string alan, string? deger, int enAz, DilServisi dil)
    {
        if ((deger ?? "").Trim().Length >= enAz) return true;
        Ekle(alan, string.Format(dil.T.Duzeltme.EnAzKarakter, enAz));
        return false;
    }

    // ── Sunucu sözlüğü (UX-4d madde 8) ───────────────────────────────────────

    /// <summary>
    /// 400 yanıtındaki alan→mesaj sözlüğünü ekrana taşır. Sunucu alan adını
    /// PascalCase verir (KrediLimitiTl); ekran id'leri kendi adlarını kullanır,
    /// bu yüzden <paramref name="esleme"/> ile çevrilebilir.
    /// </summary>
    public void Sunucudan(ApiHata hata, IReadOnlyDictionary<string, string>? esleme = null)
    {
        foreach (var (sunucuAlan, mesajlar) in hata.Alanlar)
        {
            if (mesajlar.Length == 0) continue;
            Ekle(EslemeAdi(sunucuAlan, esleme), string.Join(" ", mesajlar));
        }
    }

    /// <summary>
    /// PAKET Q10-5 (QA 10 /banka: "geçersiz IBAN'da sunucu 400 döndükten sonra
    /// bile alan 'hatali' işaretlenmiyor"). KÖK: sunucu alan adını camelCase
    /// döndürür (<c>"iban"</c>), sayfaların eşleme sözlükleri PascalCase yazılmış
    /// (<c>["Iban"] = "hesap-iban"</c>) ve varsayılan sözlük karşılaştırıcısı
    /// büyük/küçük harfe DUYARLIDIR → eşleme hiç tutmuyor, mesaj ekranda
    /// olmayan "iban" anahtarına yazılıyordu. Eşleme artık harf duyarsızdır;
    /// sayfaların sözlükleri tek tek düzeltilmez (tek yer).
    /// </summary>
    public static string EslemeAdi(string sunucuAlan, IReadOnlyDictionary<string, string>? esleme)
    {
        if (esleme is null) return sunucuAlan;
        if (esleme.TryGetValue(sunucuAlan, out var tam)) return tam;
        foreach (var (anahtar, deger) in esleme)
            if (string.Equals(anahtar, sunucuAlan, StringComparison.OrdinalIgnoreCase)) return deger;
        return sunucuAlan;
    }

    /// <summary>
    /// 403 (yetki) ile 409 (iş kuralı) AYRI mesajlar alır — 48-A doğrulayıcı
    /// devri: ekran bu ayrımı kullanıcıya doğru göstermek zorundadır.
    /// Dönen metin sayfanın hata bandına yazılır.
    /// </summary>
    public static string SunucuMesaji(ApiHata hata, DilServisi dil) => hata.StatusCode switch
    {
        403 => $"{dil.T.Kabuk.YetkiYok} {hata.Message}".Trim(),
        409 => $"{dil.T.Kabuk.IsKurali} {hata.Message}".Trim(),
        _ => hata.Message,
    };

    /// <summary>
    /// PAKET Q10-5 (doğrulama bulgusu, /banka yeni hesap): alan hatası düzeltilince
    /// alan temizleniyor ama modal içindeki GENEL bant aynı mesajı bir sonraki
    /// gönderime dek taşıyordu. Kural: bant yalnız ALAN DIŞI hatayı gösterir.
    /// Sunucunun alan sözlüğündeki her anahtar ekrandaki bir alana eşleniyorsa
    /// (<paramref name="esleme"/>, harf duyarsız) mesaj zaten alanın altındadır →
    /// bant için null döner. Eşlenemeyen bir alan ya da alan sözlüğü yoksa
    /// <see cref="SunucuMesaji"/> aynen döner (hata yutulmaz — UX-S28).
    /// </summary>
    public static string? BantMesaji(ApiHata hata, DilServisi dil, IReadOnlyDictionary<string, string>? esleme)
    {
        var dolu = hata.Alanlar.Where(a => a.Value.Length > 0).Select(a => a.Key).ToList();
        if (esleme is not null && dolu.Count > 0 &&
            dolu.All(a => esleme.Keys.Any(k => string.Equals(k, a, StringComparison.OrdinalIgnoreCase))))
            return null;
        return SunucuMesaji(hata, dil);
    }

    /// <summary>
    /// PAKET SON-KALANLAR (hasım ORTA, B1440__06/Pk1440__06/B375__06): sunucu alan hatası hem alanın
    /// altında hem kırmızı bantta İKİ KEZ yazılıyordu (sayfa Sunucudan + SunucuMesaji'yi birlikte
    /// çağırıyordu). TEK KALIP: alan hataları deftere yazılır, bant yalnız EŞLENEMEYEN (alan dışı)
    /// hatayı gösterir — <see cref="BantMesaji"/> kuralı. Dönen değer bant metnidir (null = bant boş).
    /// <para>⚠ <paramref name="esleme"/> yalnız EKRANDA ÇİZİLİ alanları içermelidir; çizili olmayan
    /// alanın mesajı bantta kalmalıdır (hata yutulmaz — UX-S28).</para>
    /// </summary>
    public string? SunucuHatasi(ApiHata hata, DilServisi dil, IReadOnlyDictionary<string, string> esleme)
    {
        Sunucudan(hata, esleme);
        return BantMesaji(hata, dil, esleme);
    }

    /// <summary>
    /// TOPLU-7B — "aynı hata metni iki kez" (GOC-A kapanış devri): <see cref="SunucuHatasi"/>'nin bant
    /// metni null döndüğünde (her sunucu alan hatası ekrandaki bir alanın altına yazıldı) bant BOŞ kalmaz,
    /// KISA genel cümle taşır ("Kayıt yapılmadı. Kırmızı işaretli alanı düzeltin."); tam cümle yalnız
    /// alanın altındadır. Alan dışı bir ret varsa bant onu TAM taşır (hata yutulmaz, UX-S28).
    /// Para ekranlarının tüm "alan + bant" catch blokları bu TEK yoldan geçer.
    /// </summary>
    public string SunucuHatasiTek(ApiHata hata, DilServisi dil, IReadOnlyDictionary<string, string> esleme)
        => SunucuHatasi(hata, dil, esleme) ?? dil.T.Urun.FormAlanHatasiGenel;

    private static readonly Regex EpostaKalibi =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
}
