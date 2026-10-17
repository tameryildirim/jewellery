using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Fiyat;

/// <summary>Bir ürün için hesaplanmış fiyat teklifi — kırılımıyla.</summary>
/// <param name="ListeFiyati">İndirim ÖNCESİ birim fiyat (liste).</param>
/// <param name="HasDegeri">has_grami × has kuru (marjsız).</param>
/// <param name="MarjTl">Has değerine eklenen liste marjı.</param>
/// <param name="IscilikTl">Listede uygulanan işçilik (çarpan sonrası).</param>
/// <param name="IndirimTutari">Birim indirim (≥ 0); kampanya yoksa 0.</param>
/// <param name="SatisFiyati">İndirim SONRASI birim fiyat = Liste − İndirim.</param>
/// <param name="KampanyaId">Uygulanan kampanya; yoksa null.</param>
/// <param name="SabitFiyat">Sabit satış fiyatı kullanıldı (marj/işçilik uygulanmadı) — ürün kartından ya da geçersiz kılmadan.</param>
/// <param name="Kaynak">Fiyatın NEREDEN geldiği: <see cref="FiyatKaynagi"/> (LISTE · URUN_GECERSIZ_KILMA · URUN_SABIT_FIYAT).</param>
/// <param name="OfsetTl">M1 — liste fiyatına eklenen elle ofset (± TL); yoksa 0.</param>
/// <param name="SatisMilyemi">M1 — fiyat satış milyemiyle hesaplandıysa o milyem; değilse null.</param>
/// <param name="MaliyetTl">M1b — alış (maliyet) milyemi biliniyorsa birim maliyet (net gram × alış milyemi × has); değilse null. Kâr = satış − maliyet.</param>
/// <param name="KdvPayiTl">KDV payı (müşteri 16.10, 04:59: "binde bir gir… bunun üzerine ekle, fiyatın içinde onu ekle"):
/// ürünün KDV'ye giren payı (binde) varsa fiyatın ÜSTÜNE eklenen KDV; yoksa 0.</param>
public sealed record FiyatTeklifi(
    decimal ListeFiyati, decimal HasDegeri, decimal MarjTl, decimal IscilikTl,
    decimal IndirimTutari, decimal SatisFiyati, string? KampanyaId, bool SabitFiyat,
    string Kaynak = FiyatKaynagi.Liste, decimal OfsetTl = 0m, decimal? SatisMilyemi = null,
    decimal? MaliyetTl = null, decimal KdvPayiTl = 0m);

/// <summary>
/// Fiyatın kaynağı — ekranda rozet, denetimde cevap ("bu fiyat nereden geldi?").
/// Uçlarda dizge olarak döner; POS bu alanı GÖRMEZDEN GELEBİLİR (geriye dönük uyum).
/// </summary>
public static class FiyatKaynagi
{
    /// <summary>Listenin marjı/işçilik çarpanı uygulandı.</summary>
    public const string Liste = "LISTE";
    /// <summary>Bu liste × ürün kesişimine tanımlı geçersiz kılma uygulandı.</summary>
    public const string UrunGecersizKilma = "URUN_GECERSIZ_KILMA";
    /// <summary>Ürün kartındaki sabit satış fiyatı kullanıldı.</summary>
    public const string UrunSabitFiyat = "URUN_SABIT_FIYAT";
}

/// <summary>
/// FİYAT MOTORU — saf hesap; veritabanı, saat, ağ yok (PanoFiyatServisi ile aynı
/// ilke). Girdi: ürün + has kuru + liste + (varsa) o an geçerli kampanya.
///
/// Kurallar:
///  1. Ürünün SABİT satış fiyatı varsa liste fiyatı odur; marj ve işçilik çarpanı
///     UYGULANMAZ (kuyumcu fiyatı elle koymuştur). Kampanya yine uygulanır; ancak
///     ISCILIK_YUZDE sabit fiyatta işçilik bileşeni bilinmediği için 0 indirim verir
///     — sessizce "tutar yüzdesi" sayılmaz.
///  2. ALTIN DIŞI maden (gümüş · platin · paladyum · çelik) ALTIN has kuruyla
///     fiyatlanmaz (12 g gümüş kolyeyi 76.972 TL, 5 g platin alyansı fahiş
///     rakam yapan hata — paket 20). Sabit fiyatı yoksa teklif YOK → null
///     (fail-closed; elle girilir). Maden kotasyonları Faz B'de gelir.
///     ⚠ Q3 mührü (2026-09-16, paket 49): "sabit fiyat" buraya liste × ürün
///     GEÇERSİZ KILMASINDAN da gelebilir — sabit fiyat has kuruna bağlı değildir,
///     bu yüzden gümüş ürün de fiyatlanır. Yalnız marj/işçilik taşıyan geçersiz
///     kılma gümüşte fail-closed null kalır (has dalına girmek zorunda olurdu).
///  3. Has kuru ≤ 0 ise teklif yok (kur çekilememiş).
///  4. İndirim liste fiyatını aşamaz; negatif satış fiyatı yoktur.
///  5. Yuvarlama 2 hane, AwayFromZero — ekran ve sunucu aynı kuralı paylaşır.
///  6. M1 (müşteri 16.10: "925 ise 925") — SATIŞ MİLYEMİ doluysa (yalnız altın, has dalı):
///     net gram = has gram × 1000 / ayar milyemi; altın+işçilik = net gram × satış milyemi/1000 × has kuru.
///     Has değeri (ayar milyemiyle) AYNI kalır; fark "milyem işçiliği" olarak İŞÇİLİK sütununa yazılır
///     (KDV özel matrahı ve kâr raporu işçiliği ayrı görür). Satış milyemi kuyumcunun kâr payını
///     zaten içerdiği için LİSTENİN genel marjı UYGULANMAZ (çift kâr olurdu); liste × ürün
///     geçersiz kılmasında AÇIKÇA verilmiş marj yine uygulanır (daha özel kural). Sabit TL işçilik
///     (× liste çarpanı) üstüne eklenir.
///  6b. M1b (müşteri 16.10: "925'den maliyetim varsa 925 yazacağım", "bileziği kârsız satıyoruz") —
///     ALIŞ (maliyet) milyemi: maliyet = net gram × alış milyemi/1000 × has kuru (teklifte MaliyetTl).
///     Satış milyemi BOŞSA satış alış milyemiyle yapılır (kârsız satış); kural 6 aynen işler.
///  7. M1 — FİYAT OFSETİ (± TL) yalnız hesaplanan (has) dalda liste fiyatına eklenir; sabit fiyatta
///     anlamsızdır (fiyat zaten elle konmuştur). Sonuç eksiye düşmez.
/// </summary>
public static class FiyatMotoru
{
    /// <param name="gecersizKilma">
    /// Bu liste × ürün kesişimine tanımlı geçersiz kılma (paket 49). Çağıran DB'den
    /// okur; motor SAF kalır. Pasif kayıt YOK sayılır — "pasife al" fiyatı listeye
    /// döndürmenin tek yoludur.
    /// </param>
    public static FiyatTeklifi? Hesapla(
        Data.Urun urun, decimal hasKuru, FiyatListesi liste, Kampanya? kampanya,
        FiyatListesiUrunFiyati? gecersizKilma = null, decimal varsayilanKdvOrani = 0.20m)
    {
        decimal kdvPayiTl = 0m;          // KDV payı (binde) — kural 8
        decimal listeFiyati, hasDegeri, marjTl, iscilikTl;
        bool sabit;
        string kaynak;
        decimal ofsetTl = 0m;            // M1 kural 7
        decimal? satisMilyemi = null;    // M1 kural 6
        decimal? maliyetTl = null;       // M1b kural 6b

        // Pasif ya da boş geçersiz kılma hiç yokmuş gibidir (fail-open değil:
        // "yok" hâli listenin kuralıdır, uydurma bir fiyat değil).
        var gk = gecersizKilma is { Aktif: true, Doldurulmus: true } ? gecersizKilma : null;

        if (gk?.SabitFiyat is > 0m)
        {
            // Kural 1a — liste × ürün kesişimindeki sabit fiyat, ürün kartındaki
            // GENEL sabit fiyattan daha özeldir ve onu EZER (paket 49).
            sabit = true; kaynak = FiyatKaynagi.UrunGecersizKilma;
            listeFiyati = Yuvarla(gk.SabitFiyat.Value);
            hasDegeri = 0m; marjTl = 0m; iscilikTl = 0m;
        }
        else if (urun.SatisFiyati is > 0m)
        {
            sabit = true; kaynak = FiyatKaynagi.UrunSabitFiyat;
            listeFiyati = Yuvarla(urun.SatisFiyati.Value);
            hasDegeri = 0m; marjTl = 0m; iscilikTl = 0m;
        }
        else
        {
            // Kural 2 (paket 20) — MADEN BAZLI FAIL-CLOSED.
            // ⚠ Eskiden burada `urun.AyarId.StartsWith('G')` yazıyordu: yalnız
            // GÜMÜŞ eleniyordu. PT950 platin alyans ve PD999 paladyum yüzük bu
            // kontrolden GEÇİYOR ve ALTIN has kuruyla çarpılıyordu — 5 g platin
            // alyans altın kuruyla fiyatlanıp fahiş bir rakam üretiyordu.
            // Artık ürünün MADENİ sorulur: ALTIN dışı madenin kotasyonu sistemde
            // YOKTUR (Faz B'de gelecek), o yüzden has dalına hiç girilmez ve
            // teklif ÜRETİLMEZ. Sabit fiyat yolu yukarıda açıktır — altın dışı
            // ürün elle girilen fiyatla satılır (K20-4: başka madenin kuru
            // ASLA ikame edilmez).
            var maden = Kuyumcu.Core.Domain.Ayarlar.MadenBul(urun.AyarId)
                        ?? Kuyumcu.Core.Domain.Maden.Altin;
            if (maden != Kuyumcu.Core.Domain.Maden.Altin) return null;
            if (hasKuru <= 0m) return null;                         // kur yok — kural 3

            sabit = false;
            // Geçersiz kılmanın yüzdesi listeninkinin YERİNE geçer — TOPLANMAZ.
            // Boş bırakılan alan listeden gelir (kısmi geçersiz kılma).
            var marjYuzde = gk?.HasMarjYuzde ?? liste.HasMarjYuzde;
            var carpanYuzde = gk?.IscilikCarpanYuzde ?? liste.IscilikCarpanYuzde;
            kaynak = gk is null ? FiyatKaynagi.Liste : FiyatKaynagi.UrunGecersizKilma;
            hasDegeri = Yuvarla(urun.HasGrami * hasKuru);
            if (urun.AlisMilyemi is > 0m && urun.Milyem > 0m)
                maliyetTl = Yuvarla(urun.HasGrami * 1000m / urun.Milyem * urun.AlisMilyemi.Value / 1000m * hasKuru);
            var etkinSatisMilyemi = urun.SatisMilyemi is > 0m ? urun.SatisMilyemi : urun.AlisMilyemi;   // 6b
            if (etkinSatisMilyemi is > 0m && urun.Milyem > 0m)
            {
                // Kural 6 — satış milyemi: işçilik milyemde, genel liste marjı yok.
                satisMilyemi = etkinSatisMilyemi.Value;
                var netGram = urun.HasGrami * 1000m / urun.Milyem;
                var altinVeIscilik = Yuvarla(netGram * satisMilyemi.Value / 1000m * hasKuru);
                marjTl = gk?.HasMarjYuzde is { } gkMarj ? Yuvarla(hasDegeri * gkMarj / 100m) : 0m;
                iscilikTl = Yuvarla(altinVeIscilik - hasDegeri
                                    + (urun.IscilikTl ?? 0m) * carpanYuzde / 100m);
            }
            else
            {
                marjTl = Yuvarla(hasDegeri * marjYuzde / 100m);
                iscilikTl = Yuvarla((urun.IscilikTl ?? 0m) * carpanYuzde / 100m);
            }
            ofsetTl = urun.FiyatOfsetTl ?? 0m;
            var oncesi = Math.Max(0m, Yuvarla(hasDegeri + marjTl + iscilikTl + ofsetTl));
            // Kural 8 — KDV PAYI FİYATIN ÜSTÜNE (müşteri 16.10, 04:59–05:07 ve 26:43–26:49): KDV'ye giren pay
            // binde b ise faturada KDV = toplam × b/1000 × oran (FaturaModulu → OzelMatrahMotoru.HesaplaBinde).
            // Kuyumcunun eline fiyattan önceki tutar TAM kalsın diye toplam = önce ÷ (1 − b/1000 × oran);
            // aradaki fark KDV payıdır. (Kârsız bilezikte KDV fiyattan kesilseydi her satış KDV kadar zarar olurdu.)
            if (urun.KdvMatrahBinde is { } binde && binde > 0m && binde <= 1000m)
            {
                var oran = urun.KdvOrani is { } y ? y / 100m : varsayilanKdvOrani;
                var pay = binde / 1000m * oran;
                if (oran > 0m && pay < 1m) kdvPayiTl = Yuvarla(oncesi / (1m - pay) - oncesi);
            }
            listeFiyati = oncesi + kdvPayiTl;
        }

        var indirim = kampanya is null ? 0m : Indirim(kampanya, listeFiyati, iscilikTl, sabit);
        indirim = Math.Min(Math.Max(indirim, 0m), listeFiyati);   // kural 4

        return new FiyatTeklifi(
            listeFiyati, hasDegeri, marjTl, iscilikTl,
            indirim, Yuvarla(listeFiyati - indirim),
            indirim > 0m ? kampanya!.Id : null, sabit, kaynak, ofsetTl, satisMilyemi, maliyetTl, kdvPayiTl);
    }

    /// <summary>Kampanyanın BİRİM indirimi.</summary>
    private static decimal Indirim(Kampanya k, decimal listeFiyati, decimal iscilikTl, bool sabitFiyat) => k.IndirimTuru switch
    {
        // Sabit fiyatlı üründe işçilik bileşeni yok → işçilik indirimi tanımsız → 0.
        "ISCILIK_YUZDE" => sabitFiyat ? 0m : Yuvarla(iscilikTl * k.Deger / 100m),
        "TUTAR_YUZDE"   => Yuvarla(listeFiyati * k.Deger / 100m),
        "TUTAR_TL"      => Yuvarla(k.Deger),
        _ => throw new InvalidOperationException($"Bilinmeyen indirim türü: {k.IndirimTuru}"),
    };

    /// <summary>
    /// Kampanya bu ürüne bu gün uygulanır mı? Kategori hedefi ALT kategorileri de
    /// kapsar; <paramref name="urunKategoriZinciri"/> ürünün kategorisinden köke
    /// kadar olan kimliklerdir (çağıran DB'den kurar).
    /// </summary>
    public static bool Uygulanir(Kampanya k, DateOnly bugun, Data.Urun urun, IReadOnlySet<string> urunKategoriZinciri)
    {
        if (!k.Aktif || bugun < k.Baslangic || bugun > k.Bitis) return false;
        return k.HedefTur switch
        {
            "TUMU"     => true,
            "URUN"     => k.HedefId == urun.Id,
            "KATEGORI" => k.HedefId is not null && urunKategoriZinciri.Contains(k.HedefId),
            _ => false,
        };
    }

    /// <summary>
    /// Birden fazla kampanya uygulanıyorsa müşteri lehine EN YÜKSEK indirim seçilir;
    /// kampanyalar toplanmaz (toplansaydı %60+%60 = bedava olurdu).
    /// </summary>
    public static Kampanya? EnIyi(
        IEnumerable<Kampanya> uygunlar, Data.Urun urun, decimal hasKuru, FiyatListesi liste,
        FiyatListesiUrunFiyati? gecersizKilma = null)
    {
        Kampanya? enIyi = null; var enIyiIndirim = 0m;
        foreach (var k in uygunlar)
        {
            // Geçersiz kılma İNDİRİMİ de etkiler (sabit fiyatta ISCILIK_YUZDE = 0);
            // "en iyi kampanya" seçimi bu yüzden aynı girdiyle yapılır.
            var t = Hesapla(urun, hasKuru, liste, k, gecersizKilma);
            if (t is not null && t.IndirimTutari > enIyiIndirim) { enIyi = k; enIyiIndirim = t.IndirimTutari; }
        }
        return enIyi;
    }

    public static decimal Yuvarla(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
