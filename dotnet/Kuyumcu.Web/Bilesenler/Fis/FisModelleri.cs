using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Bilesenler.Fis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-1 — TEZGÂH FİŞİNİN VERİ MODELLERİ (Islemler.razor'dan TAŞINDI)
//
// ⚠ Bu dosya DAVRANIŞ DEĞİŞTİRMEZ: sınıflar ve hesaplar Islemler.razor'un
//   @code bloğundan BİREBİR taşındı (S-2 madde 2.1 — "fiş gövdesi bileşenlere
//   ayrılır"). Durumun SAHİBİ hâlâ Islemler.razor'dur; fiş bileşenleri bu
//   nesneleri parametre olarak alır, POST ETMEZ, API istemcisi enjekte etmez.
//   Tek POST yolu Islemler.razor Kaydet()'tir (plan § 3).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Fişin bir kalemi (VERDİM sütunu) — eskiden Islemler.razor içindeki <c>Kalem</c>.</summary>
public sealed class FisKalemi
{
    public required string UrunId { get; init; }
    public required string UrunAdi { get; init; }
    public decimal Adet { get; set; }
    public decimal BirimFiyat { get; set; }
    /// <summary>Hediye işaretlenince fiyat 0 olur; geri alınırsa buradan döner.</summary>
    public decimal OncekiFiyat { get; set; }
    /// <summary>Satışın yanında bedelsiz — sunucu da 0'a zorlar, tek başına reddeder.</summary>
    public bool Hediye { get; set; }
    /// <summary>Sunucu teklifinin kırılımı (varsa) — fişte "indirim öncesi" gösterimi için.</summary>
    public decimal? ListeFiyati { get; set; }
    public decimal? IndirimOncesi { get; set; }
    /// <summary>
    /// PAKET P-16 (Y-4) — SATIR İSKONTOSU YÜZDESİ.
    /// ⚠ TUTAR DEĞİL YÜZDE taşınır: TL karşılığını sunucu hesaplar
    /// (IskontoHesabi). Ekranda görünen rakam ÖNİZLEMEDİR.
    /// </summary>
    public decimal SatirIskontoYuzde { get; set; }

    /// <summary>
    /// PAKET T-S2 — sunucu teklifinin BİRİM işçiliği (GET /fiyat/teklif · iscilikTl). Yalnız
    /// kalem teklifin fiyatıyla eklendiyse dolar; fiyat elle yazıldıysa ya da sonradan
    /// düzeltildiyse null (ekran "bilinmiyor" der, rakam UYDURMAZ). Gövdeye GİTMEZ.
    /// </summary>
    public decimal? IscilikTl { get; set; }

    /// <summary>Hediye bayrağı değişince fiyatı sıfırlar ya da eski fiyatı geri getirir.</summary>
    public void HediyeUygula()
    {
        if (Hediye) { OncekiFiyat = BirimFiyat; BirimFiyat = 0m; }
        else BirimFiyat = OncekiFiyat;
    }
}

/// <summary>
/// Kalem giriş satırının durumu (barkod + ürün + adet + birim fiyat). Eskiden
/// Islemler.razor'da ayrı alanlardı (posBarkod, secilenUrunId, adet, birimFiyat,
/// sonTeklif); anlamları DEĞİŞMEDİ.
/// </summary>
public sealed class KalemGirisi
{
    public string Barkod { get; set; } = "";
    public string? BarkodHata { get; set; }
    public string UrunId { get; set; } = "";
    public decimal Adet { get; set; } = 1m;
    public decimal? BirimFiyat { get; set; }
    public Teklif? SonTeklif { get; set; }

    /// <summary>Kalem eklendikten sonra satır boşalır (eski KalemEkle sonu).</summary>
    public void Sifirla()
    {
        UrunId = "";
        BirimFiyat = null;
        SonTeklif = null;
        Adet = 1m;
    }
}

/// <summary>Karma ödemenin bir bacağı (ALDIM sütunu) — eskiden Islemler.razor içindeki <c>Bacak</c>.</summary>
public sealed class FisBacagi
{
    public string Arac { get; set; } = "NAKIT";
    public decimal TutarTl { get; set; }
    public string KasaId { get; set; } = "";
    /// <summary>DOVIZ bacağı: USD/EUR/GBP.</summary>
    public string ParaBirimi { get; set; } = "USD";
    /// <summary>DOVIZ bacağı: kasaya giren efektif miktar.</summary>
    public decimal DovizTutar { get; set; }

    /// <summary>PAKET P-10 (Y-3) — KART bacağı: tahsilatın geçtiği POS cihazı.</summary>
    public string PosCihazId { get; set; } = "";

    /// <summary>PAKET P-10 (Y-3) — KART bacağı: taksit adedi (1 = tek çekim).</summary>
    public int PosTaksitAdedi { get; set; } = 1;

    /// <summary>
    /// PAKET B-4 — ALTIN bacağının girdileri (ayar, gram, has fiyatı, satıcı).
    /// Yalnız Arac == "ALTIN" iken doludur; TutarTl bu girdilerden TÜRETİLİR
    /// (B4-31: kullanıcı TL yazmaz) — bkz. FisAltinBacagi.
    /// </summary>
    public AltinBacagiGirisi? Altin { get; set; }

    /// <summary>PAKET B-4 — panelin son bildirdiği durum (kilit + eksikler, B4-32/33).</summary>
    public AltinBacagiDurumu? AltinDurum { get; set; }

    /// <summary>PAKET SATIS-3 — kullanıcı tutarı elle yazdı mı (otomatik kalan bu bacağa dokunmaz). Gövdeye GİRMEZ.</summary>
    public bool ElleYazildi { get; set; }
}

/// <summary>
/// Hurda alışın girdileri + DP4-03 satıcı kimliği (VUK md.234). Doğrulama
/// mesajları eskiden Islemler.razor'daki HurdaSaticiTipiHata … HurdaSaticiTamam
/// özellikleriydi; kural BİREBİR aynıdır ve yalnız hurda alışta okunur.
/// </summary>
public sealed class HurdaGirisi
{
    /// <summary>PAKET ISLEM-BANT — bant dışı has fiyatının SAHİP/MÜDÜR onayı (gövde: bantOnay/bantGerekce).</summary>
    public IslemBantOnayDurumu Bant { get; } = new();
    public decimal? Gram { get; set; }
    public string Ayar { get; set; } = "K18";
    public decimal? Fiyat { get; set; }
    /// <summary>
    /// UX-4b madde 7(a) — has gram fiyatının KAYNAĞI. false: canlı kurdan türetildi,
    /// true: kullanıcı elle yazdı ve kur güncellemesi üzerine yazamaz.
    /// </summary>
    public bool FiyatElle { get; set; }

    public string SaticiTipi { get; set; } = "";
    public string? SaticiAd { get; set; }
    public string? SaticiTckn { get; set; }
    public string? SaticiAdres { get; set; }
    public string? TedarikciFaturaNo { get; set; }
    public bool SaticiAdCariden { get; set; }

    /// <summary>P-36c/3: değer ortak girdi ailesinden ÇÖZÜLMÜŞ gelir; kaynak "elle"ye döner.</summary>
    public void FiyatGirildi(decimal? deger)
    {
        Fiyat = deger;
        FiyatElle = true;
    }

    /// <summary>
    /// UX-S24 — bağımlı alanın bağını EKRAN kurar: satıcı tipi seçilince ad
    /// alanı seçili cariden ön dolar (kullanıcı yazdıysa ÜZERİNE YAZILMAZ).
    /// </summary>
    public void SaticiTipiSec(string? tip, string? cariAdi)
    {
        SaticiTipi = tip ?? "";
        if (SaticiTipi is "NIHAI_TUKETICI" or "ESNAF_MUAF"
            && string.IsNullOrWhiteSpace(SaticiAd)
            && cariAdi is not null)
        {
            SaticiAd = cariAdi;
            SaticiAdCariden = true;
        }
    }

    /// <summary>
    /// SATIS-3 doğrulama 4. tur (UX-S21/S27): kullanıcı ne alıp ne ödeyeceğini kaydetmeden görür.
    /// Sunucuyla AYNI formül (SatisModulu.HurdaAlis: HasHesap.ToHasGram × fiyat, 2 hane, AwayFromZero);
    /// gram/ayar/fiyat eksikse null. Stopaj oranı (satıcı tipine göre, sunucu yapılandırması) istemcide HESAPLANMAZ.
    /// </summary>
    public decimal? HasKarsiligi =>
        Gram is > 0 && Ayarlar.Bul(Ayar) is { HasHesaplanir: true } a ? HasHesap.ToHasGram(Gram.Value, a) : null;

    /// <summary>Hesaplanan brüt tutar (has × has gram fiyatı) — null: hesaplanamaz.</summary>
    public decimal? HesaplananTutar =>
        HasKarsiligi is { } h && Fiyat is > 0 ? Math.Round(h * Fiyat.Value, 2, MidpointRounding.AwayFromZero) : null;

    /// <summary>TCKN'nin yalnız rakamları — boşluk/tire kullanıcıyı engellemesin.</summary>
    public string TcknRakam => new([.. (SaticiTckn ?? "").Where(char.IsAsciiDigit)]);

    // Alan altı Türkçe mesajlar (DoD EK md.3) — DEĞER GİRİLDİKÇE temizlenir
    // (DP4-06), "denendi" bayrağına bağlanmaz: sessiz etkisizlik yasak.
    public string? TipiHata(SozlukIslem t) =>
        string.IsNullOrEmpty(SaticiTipi) ? t.SaticiTipiZorunlu : null;

    /// <summary>
    /// SATIS-3 doğrulama bulgusu: açılışta, kullanıcı hiçbir şeye dokunmadan "Satıcı tipi zorunludur."
    /// kırmızısı çıkıyordu. Alan altı mesaj formla ilk etkileşimden SONRA görünür (kilit sebebi Kaydet'in
    /// yanında zaten yazılıdır — sessiz etkisizlik yok). Gövdeye girmez.
    /// </summary>
    public bool Dokunuldu { get; set; }

    /// <summary>Ekrandaki alan altı mesaj: yalnız form ellendiyse.</summary>
    public string? TipiHataGorunur(SozlukIslem t) => Dokunuldu ? TipiHata(t) : null;

    public string? AdHata(SozlukIslem t) =>
        SaticiTipi is "NIHAI_TUKETICI" or "ESNAF_MUAF" && string.IsNullOrWhiteSpace(SaticiAd)
            ? t.SaticiAdZorunlu : null;

    public string? TcknHata(SozlukIslem t) =>
        SaticiTipi is not ("NIHAI_TUKETICI" or "ESNAF_MUAF") ? null
        : TcknRakam.Length == 0 ? t.SaticiTcknZorunlu
        : TcknRakam.Length != 11 ? t.SaticiTcknGecersiz
        : !KimlikNoDogrulayici.TcknGecerli(TcknRakam) ? KimlikNoSozlugu.Icin(t).TcknGecersiz   // TCKN-DOGRULAMA: anında, alan altında
        : null;

    /// <summary>PAKET TCKN-DOGRULAMA: 11 hane yazılmış ama TC kimlik no doğrulama kuralından geçmiyor (kilit sebebi bu metni söyler).</summary>
    public bool TcknAlgoritmaHatasi =>
        SaticiTipi is "NIHAI_TUKETICI" or "ESNAF_MUAF" && TcknRakam.Length == 11 && !KimlikNoDogrulayici.TcknGecerli(TcknRakam);

    public string? FaturaNoHata(SozlukIslem t) =>
        SaticiTipi == "MUKELLEF" && string.IsNullOrWhiteSpace(TedarikciFaturaNo)
            ? t.TedarikciFaturaNoZorunlu : null;

    /// <summary>
    /// PAKET SATIS-3 (KAYNAK 2 md.4) — satıcı adresi zorunlu mu? Sunucu yapılandırmasından
    /// (GET /islemler/altin-bacagi/ayarlar → saticiAdresiZorunlu = HurdaAyarlari, GiderPusulasiKapisi
    /// ile AYNI değer). Bilinmiyorken TUTUCU: zorunlu (sunucu varsayılanı da AÇIK).
    /// </summary>
    public bool AdresZorunlu { get; set; } = true;

    public string? AdresHata(string mesaj) =>
        AdresZorunlu && SaticiTipi is "NIHAI_TUKETICI" or "ESNAF_MUAF" && string.IsNullOrWhiteSpace(SaticiAdres)
            ? mesaj : null;

    /// <summary>Sunucunun FAIL-CLOSED kapısının istemci aynası (fazlası değil).</summary>
    public bool SaticiTamam(SozlukIslem t) =>
        TipiHata(t) is null && AdHata(t) is null && TcknHata(t) is null && FaturaNoHata(t) is null
        && AdresHata("x") is null;

    /// <summary>Başarılı kayıttan sonra form boşalır; has fiyatı yine kurdan gelir (UX-S21).</summary>
    public void Sifirla(decimal hasFiyati)
    {
        Gram = null;
        SaticiTipi = "";
        SaticiAd = SaticiTckn = SaticiAdres = TedarikciFaturaNo = null;
        SaticiAdCariden = false;
        Dokunuldu = false;
        FiyatElle = false;
        Fiyat = hasFiyati > 0m ? hasFiyati : null;
    }
}

/// <summary>Takas (değişim) girdileri — eski ürün (hurda gibi) + yeni ürün.</summary>
public sealed class TakasGirisi
{
    /// <summary>PAKET ISLEM-BANT — bant dışı eski altın has fiyatının SAHİP/MÜDÜR onayı.</summary>
    public IslemBantOnayDurumu Bant { get; } = new();
    public string EskiAyar { get; set; } = "K22";
    public decimal? EskiGram { get; set; }
    public decimal? EskiFiyat { get; set; }
    /// <summary>Has gram fiyatı elle mi girildi (UX-4b 7(a)) — rozet kaynağı.</summary>
    public bool FiyatElle { get; set; }
    public string YeniUrunId { get; set; } = "";
    public int YeniAdet { get; set; } = 1;
    public decimal? YeniFiyat { get; set; }

    public void FiyatGirildi(decimal? deger)
    {
        EskiFiyat = deger;
        FiyatElle = true;
    }

    // Takas fark önizlemesi — SatisModulu.Takas ile aynı formül
    public decimal EskiHas =>
        EskiGram is > 0 && Ayarlar.Bul(EskiAyar) is { } ayar
            ? HasHesap.ToHasGram(EskiGram.Value, ayar)
            : 0m;
    public decimal EskiDeger => EskiHas * (EskiFiyat ?? 0m);
    /// <summary>ISLEM-BANT-2: eski altın fiyatı tutar hesabına girer mi (0/eksi/mutlak dışı HAYIR — EKRANDA tutar yerine "—"; Fark/hesap modeli AYNEN, bkz. FisTakasTuval).</summary>
    public bool EskiFiyatHesabaGirer => IslemBantGosterim.FiyatHesabaGirerAyar(EskiFiyat, EskiAyar);
    public decimal YeniDeger => YeniAdet * (YeniFiyat ?? 0m);
    public decimal? Fark =>
        EskiGram is > 0 && EskiFiyat is > 0 && YeniFiyat is > 0
            ? YeniDeger - EskiDeger
            : null;

    public bool Tamam =>
        EskiGram is > 0 && EskiFiyat is > 0
        && Ayarlar.Bul(EskiAyar) is not null   // t9: bilinmeyen ayar has=0 → yanlış fark; Tamam değil
        && !string.IsNullOrEmpty(YeniUrunId)
        && YeniAdet > 0 && YeniFiyat is > 0;

    /// <summary>
    /// PAKET PARA-DEMO (madde 3) — müşteriden ALINAN eski altının satıcı kimliği (VUK md.234
    /// gider pusulası; TBK md.283: takasta her taraf verdiği şeyin satıcısıdır). Hurda alışının
    /// AYNI modeli ve doğrulamaları kullanılır (yalnız kimlik alanları; gram/fiyat burada değil).
    /// </summary>
    public HurdaGirisi Satici { get; } = new();

    public void Sifirla(decimal hasFiyati)
    {
        FiyatElle = false;
        EskiGram = null;
        EskiFiyat = hasFiyati > 0m ? hasFiyati : null;
        YeniUrunId = "";
        YeniAdet = 1;
        YeniFiyat = null;
        Satici.Sifirla(0m);   // PARA-DEMO: kimlik bir sonraki takasa taşınmaz
    }
}

/// <summary>
/// Fiş hesapları — SUNUCUNUN hesabının AYNISI (önizleme). Yuvarlama kuralı
/// sunucuyla aynı: 2 hane, AwayFromZero. SON SÖZ SUNUCUNUNDUR (UX-S27).
/// </summary>
public static class FisHesap
{
    public static decimal Yuvarla2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    private static readonly System.Globalization.CultureInfo TrKultur = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// PAKET T-S2 (doğrulayıcı bulgusu, KRİTİK) — HAS gram gösterimi: tuval iki hane
    /// gösterir ("5,66"), ama has DÖRT hane taşır (P-57: Bicim.Has = F4). Biçim
    /// "#,0.00##": en az iki, gerekirse dört hane → 0,0003 g "0,00" diye KAYBOLMAZ.
    /// Birim eki çağırana aittir (tablo hücresi yalın sayı, toplam " g" ekler).
    /// </summary>
    public static decimal HasYuvarla(decimal has) => Math.Round(has, 4, MidpointRounding.AwayFromZero);
    /// <inheritdoc cref="HasYuvarla(decimal)"/>
    public static string HasSayi(decimal has) => HasYuvarla(has).ToString("#,0.00##", TrKultur);

    /// <summary>PAKET T-S2 — GRAM gösterimi: en az iki, gerekirse üç hane (P-57: Bicim.Gram = F3).</summary>
    public static decimal GramYuvarla(decimal gram) => Math.Round(gram, 3, MidpointRounding.AwayFromZero);
    /// <inheritdoc cref="GramYuvarla(decimal)"/>
    public static string GramSayi(decimal gram) => GramYuvarla(gram).ToString("#,0.00#", TrKultur);

    /// <summary>
    /// PAKET T-S1 — fişin TOPLAM GRAMI (tuval "Toplam · 10,40 g"): her kalemin
    /// ürün kartındaki gramı × adedi. Kalem tablosunun Gram sütunuyla AYNI formül
    /// (KalemGram × Adet); ürün kartında gram yoksa 0 sayılır (uydurulmaz).
    /// </summary>
    public static decimal ToplamGram(IEnumerable<FisKalemi> kalemler, IReadOnlyList<UrunVeri> urunler) =>
        kalemler.Sum(k => (urunler.FirstOrDefault(u => u.Id == k.UrunId)?.GramAgirlik ?? 0m) * k.Adet);

    /// <summary>PAKET T-S2 — fişin toplam HAS gramı (ürün kartındaki has gramı × adet; UX-S21: kasiyerden istenmez).</summary>
    public static decimal ToplamHas(IEnumerable<FisKalemi> kalemler, IReadOnlyList<UrunVeri> urunler) =>
        kalemler.Sum(k => (urunler.FirstOrDefault(u => u.Id == k.UrunId)?.HasGrami ?? 0m) * k.Adet);

    /// <summary>PAKET T-S2 — satırın işçiliği (teklifin birim işçiliği × adet); hediye satırı 0; bilinmiyorsa null.</summary>
    /// <remarks>PAKET T-S2b: SATIR İSKONTOSU varsa tutar indirim sonrası, teklifin işçiliği indirim ÖNCESİdir;
    /// iskonto satırın bütününden düşer, işçilik payı ayrıştırılmaz → null ("—", uydurma yok).</remarks>
    public static decimal? SatirIscilik(FisKalemi k) =>
        k.Hediye ? 0m : k.SatirIskontoYuzde > 0m ? null : k.IscilikTl is { } i ? Yuvarla2(i * k.Adet) : null;

    /// <summary>PAKET T-S2 — bilinen satırların işçilik toplamı + en az bir satırın işçiliği bilinmiyor mu.</summary>
    /// <remarks>PAKET T-S2b: FİŞ İSKONTOSU varsa her satırın işçiliği indirim öncesidir → toplam bilinmez (0, Eksik) → "—".</remarks>
    public static (decimal Toplam, bool Eksik) ToplamIscilik(IReadOnlyCollection<FisKalemi> kalemler, decimal? fisIskontoYuzde = null) =>
        fisIskontoYuzde is > 0m && kalemler.Any(k => !k.Hediye)
            ? (0m, true)
            : (Yuvarla2(kalemler.Sum(k => SatirIscilik(k) ?? 0m)), kalemler.Any(k => SatirIscilik(k) is null));

    /// <summary>PAKET T-S2b — toplam has: bir kalemin ürün kartı listede yoksa has BİLİNMEZ → null ("—"; 0 sayılıp eksik toplam basılmaz).</summary>
    public static decimal? ToplamHasBilinen(IReadOnlyCollection<FisKalemi> kalemler, IReadOnlyList<UrunVeri> urunler) =>
        kalemler.All(k => urunler.Any(u => u.Id == k.UrunId)) ? ToplamHas(kalemler, urunler) : null;

    /// <summary>Satırın iskonto DÜŞÜLMÜŞ tutarı (önizleme).</summary>
    public static decimal SatirNet(FisKalemi k)
    {
        var brut = Yuvarla2(k.Adet * k.BirimFiyat);
        var isk = k.SatirIskontoYuzde <= 0m ? 0m : Yuvarla2(brut * k.SatirIskontoYuzde / 100m);
        if (isk > brut) isk = brut;
        return brut - isk;
    }

    public static decimal SatirToplamNet(IEnumerable<FisKalemi> kalemler) => Yuvarla2(kalemler.Sum(SatirNet));

    /// <summary>Fiş iskontosunun TL karşılığı (önizleme).</summary>
    public static decimal FisIskontoTutari(IReadOnlyCollection<FisKalemi> kalemler, decimal? yuzde)
    {
        if (yuzde is null or <= 0m) return 0m;
        var net = SatirToplamNet(kalemler);
        var t = Yuvarla2(net * yuzde.Value / 100m);
        return t > net ? net : t;
    }

    /// <summary>Satır + fiş iskontosunun toplam TL karşılığı (önizleme).</summary>
    public static decimal IskontoOnizleme(IReadOnlyCollection<FisKalemi> kalemler, decimal? yuzde) =>
        Yuvarla2(kalemler.Sum(k => Yuvarla2(k.Adet * k.BirimFiyat)) - SatirToplamNet(kalemler))
        + FisIskontoTutari(kalemler, yuzde);

    /// <summary>
    /// ⚠ TOPLAM NET: ödeme bacakları, tevsik önizlemesi ve nakit kilidi hepsi bu
    /// değeri okur. İskonto düşülmemiş toplam gönderilirse bacak toplamı sunucunun
    /// hesapladığı fiş toplamını tutmaz ve fiş 400 döner.
    /// </summary>
    public static decimal Toplam(IReadOnlyCollection<FisKalemi> kalemler, decimal? fisIskontoYuzde) =>
        Yuvarla2(SatirToplamNet(kalemler) - FisIskontoTutari(kalemler, fisIskontoYuzde));

    public static decimal BacakToplami(IEnumerable<FisBacagi> bacaklar) => bacaklar.Sum(b => b.TutarTl);

    /// <summary>
    /// PAKET SATIS-3 (kurucu 25.09 ~12:00, KAYNAK 2 md.5) — OTOMATİK KALAN. Kullanıcının
    /// ELLE yazmadığı son TL bacağı (ALTIN ve DÖVİZ türetilir, hedef olamaz) fişin kalanını
    /// üstlenir; eksiye düşmez (fazla varsa fark ekranda "Fazla" diye yazılır). Değişiklik
    /// olduysa true — çağıran sayfayı yeniden çizer. Gövdeye yeni alan girmez.
    /// </summary>
    public static bool OtoDengele(IList<FisBacagi> bacaklar, decimal fisToplami)
    {
        if (bacaklar.Count < 2) return false;
        // k2 (tasarım hakemi K19, 29.09): hedef, TUTARI OLAN son elle-yazılmamış TL satırıdır — "+ Başka ödeme ekle" ile
        // açılıp boş (0) duran satır kalanı çalmaz (Kart 127.947,77 + boş satır + Eski altın → Kart kalanda kalıyordu,
        // fark −63.101). Tutarlı aday yoksa son aday (eski davranış).
        var adaylar = bacaklar.Where(b => !b.ElleYazildi && b.Arac is not ("DOVIZ" or FisAltinBacagi.Arac)).ToList();
        var hedef = adaylar.LastOrDefault(b => b.TutarTl > 0m) ?? adaylar.LastOrDefault();
        if (hedef is null) return false;
        var digerleri = bacaklar.Where(b => !ReferenceEquals(b, hedef)).Sum(b => b.TutarTl);
        var yeni = Math.Max(0m, fisToplami - digerleri);
        if (yeni == hedef.TutarTl) return false;
        hedef.TutarTl = yeni;
        return true;
    }

    /// <summary>
    /// 459 s. VUK GT §4.1.2 — fiş toplamı haddi aşıyorsa hiçbir bacak nakit
    /// olamaz. Eşik SUNUCUDAN gelir (/tevsik/gunluk), ekrana gömülmez (ADR-0005).
    /// </summary>
    public static bool NakitKapali(bool tevsikNakitKapali, IEnumerable<FisBacagi> bacaklar) =>
        tevsikNakitKapali && bacaklar.Any(b => b.Arac == "NAKIT" && b.TutarTl > 0m);   // t10: 0 ₺ nakit satırı kilitlemez

    /// <summary>
    /// SATIS-3 t6 — varsayılan kasa OTURUMUN ŞUBESİNDEN seçilir (sunucu sırası: ULID = açılış sırası).
    /// Kullanıcının şubesi biliniyorsa yalnız o şubenin (ya da şubesi bilinmeyen) kasaları aday; aday yoksa
    /// null → ekran seçiciyi BASİT'te de gösterir (fail-closed). Şubesiz oturumda (ör. patron) kasalar birden çok
    /// şubeye dağılmışsa yine null; tek şubedeyse ilk kasa.
    /// </summary>
    public static string? VarsayilanKasa(IReadOnlyList<KasaVeri> kasalar, string? oturumSubeId, string? kullaniciId = null)
    {
        if (kasalar.Count == 0) return null;
        // D1-11 (18.10) çalışan kasası: oturumdaki çalışanın SORUMLU olduğu kasa (şubesi uyuyorsa) önce gelir.
        if (!string.IsNullOrEmpty(kullaniciId)
            && kasalar.FirstOrDefault(k => k.SorumluKullaniciId == kullaniciId
                && (string.IsNullOrEmpty(oturumSubeId) || string.IsNullOrEmpty(k.SubeId) || k.SubeId == oturumSubeId)) is { } benim)
            return benim.Id;
        if (!string.IsNullOrEmpty(oturumSubeId))
            return kasalar.FirstOrDefault(k => string.IsNullOrEmpty(k.SubeId) || k.SubeId == oturumSubeId)?.Id;
        var subeler = kasalar.Select(k => k.SubeId).Where(s => !string.IsNullOrEmpty(s)).Distinct().Count();
        return subeler > 1 ? null : kasalar[0].Id;
    }

    /// <summary>Bacağın para birimine uyan kasalar — dolar avro kasasına giremez.</summary>
    public static IEnumerable<KasaVeri> BacakKasalari(FisBacagi b, IEnumerable<KasaVeri> kasalar) =>
        kasalar.Where(k => k.ParaBirimi == (b.Arac == "DOVIZ" ? b.ParaBirimi : "TRY"));

    /// <summary>
    /// UX-4b madde 7(d) / UX-S22 — bacağın para birimine uyan TEK kasa varsa odur,
    /// yoksa null. Tek kasa durumunda seçici kilitlenir ve değer otomatik atanır.
    /// </summary>
    public static KasaVeri? BacakKasaTek(FisBacagi b, IEnumerable<KasaVeri> kasalar)
    {
        var uyanlar = BacakKasalari(b, kasalar).Take(2).ToList();
        return uyanlar.Count == 1 ? uyanlar[0] : null;
    }

    /// <summary>Bacağın kasasını tek uyan kasaya sabitler (yoksa boşaltır).</summary>
    public static void BacakKasaSabitle(FisBacagi b, IEnumerable<KasaVeri> kasalar) =>
        b.KasaId = BacakKasaTek(b, kasalar)?.Id ?? string.Empty;

    /// <summary>PAKET P-10 — KART bacağının POS cihazını tek aktif cihaza sabitler.</summary>
    public static void BacakPosSabitle(FisBacagi b, IReadOnlyList<PosCihazSatir> posCihazlari)
    {
        if (b.Arac != "KART") { b.PosCihazId = ""; b.PosTaksitAdedi = 1; return; }
        if (posCihazlari.Count == 1) b.PosCihazId = posCihazlari[0].Id;
    }

    // ── P-36c GÖÇ YARDIMCILARI (ham <select> → ortak <Secici>) ────────────
    // Seçenek dizileri kodda durur: Razor öznitelik değerinde iç içe çift tırnak
    // ayrıştırıcıyı bozar, ayrıca iki form AYNI diziyi kullanmak ZORUNDA (UX-S26).
    /// <summary>
    /// PAKET SATIS-3 (UX-S31) — ayar seçenekleri: değer KOD (gövde değişmez), etiket AD ("22 Ayar");
    /// ham "K22" ekrana basılmaz. Tek kaynak Core.Ayarlar.
    /// </summary>
    public static IReadOnlyList<SecenekOgesi> AyarSecenekleri(IEnumerable<string> kodlar) =>
        [.. kodlar.Select(a => new SecenekOgesi(a, Kuyumcu.Web.Bilesenler.KurKaynakAdi.AyarAdi(a)))];

    /// <summary>
    /// PAKET SATIS-3 (UX-S31) — ürün seçeneği etiketi: ad + ayarın GÖRÜNEN adı; ad ayarı zaten
    /// söylüyorsa ("22 Ayar Burma Bilezik") tekrar edilmez. Ham kod (K22) basılmaz.
    /// </summary>
    public static string UrunEtiketi(string ad, string? ayarId)
    {
        // DIL-EKRAN: görünen ayar adı etkin dilden (TR "22 Ayar" · EN "22k"); ürün adı (VERİ) Türkçe çekirdek adı
        // ("22 Ayar Burma Bilezik") zaten söylüyorsa EN'de de tekrar edilmez.
        var ayarAdi = string.IsNullOrEmpty(ayarId) ? null : Kuyumcu.Web.Bilesenler.KurKaynakAdi.AyarAdi(ayarId);
        var cekirdekAdi = !string.IsNullOrEmpty(ayarId) && Kuyumcu.Core.Domain.Ayarlar.Bul(ayarId) is { } cekirdek ? cekirdek.Ad : null;
        return ayarAdi is null || ad.Contains(ayarAdi, StringComparison.OrdinalIgnoreCase)
               || (cekirdekAdi is not null && ad.Contains(cekirdekAdi, StringComparison.OrdinalIgnoreCase)) ? ad : $"{ad} · {ayarAdi}";
    }

    public static IReadOnlyList<SecenekOgesi> KasaSecenekleri(IEnumerable<KasaVeri> kasalar) =>
        [.. kasalar.Select(k => new SecenekOgesi(k.Id, $"{k.Ad} ({Bicim.ParaIsareti(k.ParaBirimi)})"))];
}

/// <summary>
/// PAKET P-23 FAZ 2 (R-17) — son satışlar listesinde AÇIK olan fişin ödeme
/// kırılımı. Sunucu fişin ödeme paylarını KAYIT olarak tutar; hata, listeyi
/// boşaltıp "kayıt yok" göstermeye ÇEVRİLMEZ (UX-S28 / Anayasa md.1): bant
/// ayrıdır, boş durum yalnız istek BAŞARILI ve sonuç gerçekten boşken çıkar.
/// İsteği sayfa atar (GET /islemler/fis/{id}); burada yalnız durum ve ayrıştırma.
/// </summary>
public sealed class OdemeKirilimi
{
    public string? AcikId { get; set; }
    public bool Yukleniyor { get; set; }
    public string? Hata { get; set; }
    public string? Kapsam { get; set; }
    public List<OdemeKalemiSatiri> Kalemler { get; set; } = [];

    /// <summary>Yeni fiş için açılır: önceki sonucun hiçbir parçası taşınmaz.</summary>
    public void Ac(string hareketId)
    {
        AcikId = hareketId;
        Yukleniyor = true;
        Hata = null;
        Kapsam = null;
        Kalemler = [];
    }

    /// <summary>Sunucu yanıtını (kalemKapsami + odemeKalemleri) okur; bilinmeyen alan tutucu varsayılana düşer.</summary>
    public void Oku(System.Text.Json.JsonElement y)
    {
        if (y.ValueKind != System.Text.Json.JsonValueKind.Object) return;
        Kapsam = y.TryGetProperty("kalemKapsami", out var kk) && kk.ValueKind == System.Text.Json.JsonValueKind.String
            ? kk.GetString() : null;
        if (!y.TryGetProperty("odemeKalemleri", out var dizi) || dizi.ValueKind != System.Text.Json.JsonValueKind.Array)
            return;
        foreach (var e in dizi.EnumerateArray())
            Kalemler.Add(new OdemeKalemiSatiri(
                e.TryGetProperty("arac", out var a) && a.ValueKind == System.Text.Json.JsonValueKind.String
                    ? a.GetString()! : "BILINMIYOR",
                e.TryGetProperty("tutarTl", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? t.GetDecimal() : 0m,
                e.TryGetProperty("paraBirimi", out var pb) && pb.ValueKind == System.Text.Json.JsonValueKind.String
                    ? pb.GetString()! : "TRY",
                e.TryGetProperty("bilinmiyor", out var bi) && bi.ValueKind == System.Text.Json.JsonValueKind.True,
                e.TryGetProperty("geriDolduruldu", out var gd) && gd.ValueKind == System.Text.Json.JsonValueKind.True,
                !(e.TryGetProperty("aktif", out var ak) && ak.ValueKind == System.Text.Json.JsonValueKind.False)));
    }
}

/// <summary>
/// UX-S27 — POST yanıtı okunmadan ATILMAZ. Kaydet() yanıtlarından fiş no,
/// yerleşim uyarısı ve SUNUCUNUN uyguladığı iskonto okunur (eskiden
/// Islemler.razor'daki FisNoOku / UyariOku / IskontoBandiniOku).
/// </summary>
public static class FisYaniti
{
    /// <summary>Sunucunun döndürdüğü fiş/hareket numarası (fisId, yoksa id).</summary>
    public static string? FisNo(System.Text.Json.JsonElement y)
    {
        if (y.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
        foreach (var ad in new[] { "fisId", "id" })
            if (y.TryGetProperty(ad, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String)
                return v.GetString();
        return null;
    }

    /// <summary>Sunucunun döndürdüğü yerleşim uyarısı — okunmadan ATILMAZ.</summary>
    public static string? YerlesimUyarisi(System.Text.Json.JsonElement y)
    {
        if (y.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
        return y.TryGetProperty("yerlesimUyarisi", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
            ? v.GetString() : null;
    }

    /// <summary>
    /// PAKET P-16 (Y-4 / UX-S27) — UYGULANAN iskonto banda yazılır. ⚠ Ekranın
    /// önizlemesi DEĞİL, SUNUCUNUN döndürdüğü rakam; ikisi ayrışırsa kullanıcı
    /// sunucunun yazdığını görür (Anayasa md.1). İskonto yoksa null.
    /// </summary>
    public static string? IskontoNotu(System.Text.Json.JsonElement y, SozlukIslem t)
    {
        if (y.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
        var toplam = y.TryGetProperty("iskontoToplam", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Number
            ? v.GetDecimal() : 0m;
        if (toplam <= 0m) return null;
        var caridenMi = y.TryGetProperty("fisIskontoCaridenMi", out var c)
                        && c.ValueKind == System.Text.Json.JsonValueKind.True;
        return caridenMi
            ? $"{t.IskontoUygulandi}: {Bicim.Tl(toplam)} ({t.IskontoCariden})"
            : $"{t.IskontoUygulandi}: {Bicim.Tl(toplam)}";
    }
}

/// <summary>Şubeler arası transfer girdileri (eskiden Islemler.razor'daki transfer* alanları).</summary>
public sealed class TransferGirisi
{
    public string KaynakSubeId { get; set; } = "";
    public string HedefSubeId { get; set; } = "";
    public string UrunId { get; set; } = "";
    public int Adet { get; set; } = 1;

    /// <summary>Gönderilebilir mi: iki FARKLI şube, ürün ve pozitif adet (sunucu yeniden doğrular).</summary>
    public bool Tamam =>
        !string.IsNullOrEmpty(KaynakSubeId)
        && !string.IsNullOrEmpty(HedefSubeId)
        && KaynakSubeId != HedefSubeId
        && !string.IsNullOrEmpty(UrunId)
        && Adet > 0;

    public void Sifirla()
    {
        KaynakSubeId = "";
        HedefSubeId = "";
        UrunId = "";
        Adet = 1;
    }
}
