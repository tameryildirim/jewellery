namespace Kuyumcu.Web.Bilesenler.Fis;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-2 — TEZGÂH FİŞİNİN FARK KURALI (Pusula formülü, PLAN § S-2 madde 2.3)
//
//   FARK = VERDİM (fişin NET toplamı) − ALDIM (ödeme bacaklarının toplamı)
//
//   • FARK = 0  → Kaydet açılır.
//   • FARK > 0  → ödeme eksik. Gerçek müşteri seçiliyse kalan "Açık hesap"
//                 (mevcut VERESİYE bacağı) olarak yazılabilir. Peşin Müşteri'de
//                 YAZILAMAZ; sunucu da reddeder (B-3 K3, OdemeSepeti).
//   • FARK < 0  → ödeme fazla, kaydedilmez.
//
// ⚠ Bu bir ÖNİZLEMEDİR, son söz sunucunundur: OdemeSepeti.Coz bacak toplamı
//   ≠ fiş tutarı ise 400 döner. Ekran kuralı kullanıcıyı o 400'e sürüklememek
//   içindir; kural gevşerse sunucu yine reddeder (canlı kanıt raporda).
// ⚠ Saf sınıf: istek atmaz, ApiIstemci bilmez (PLAN § 3 md.5 — tek POST yolu).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>FARK'ın üç hâli.</summary>
public enum FarkDurumu
{
    /// <summary>Verilen = alınan.</summary>
    Tamam,
    /// <summary>Alınan eksik (müşteri borçlu kalır).</summary>
    Eksik,
    /// <summary>Alınan fazla.</summary>
    Fazla,
}

/// <summary>Tezgâh fişinin VERDİM/ALDIM farkı — tek kural kaynağı.</summary>
public static class FisFark
{
    /// <summary>
    /// VERDİM − ALDIM. Tek ödemeli fişte (karma kapalı) ödeme fişin tamamıdır,
    /// fark 0'dır: <paramref name="bacaklar"/> null verilir.
    /// </summary>
    public static decimal Hesapla(decimal verdim, IEnumerable<FisBacagi>? bacaklar) =>
        bacaklar is null ? 0m : verdim - FisHesap.BacakToplami(bacaklar);

    /// <summary>Farkın hâli.</summary>
    public static FarkDurumu Durum(decimal fark) =>
        fark == 0m ? FarkDurumu.Tamam : fark > 0m ? FarkDurumu.Eksik : FarkDurumu.Fazla;

    /// <summary>Fark kuralı Kaydet'e izin veriyor mu? Yalnız TAM sıfır.</summary>
    public static bool KaydetAcik(decimal fark) => fark == 0m;

    /// <summary>
    /// "Açık hesap olarak kaydet" neden kapalı? null = açık. Sıra: fark yoksa
    /// eylem yok; müşteri yoksa müşteri iste; Peşin Müşteri'ye borç yazılmaz;
    /// ikinci VERESİYE bacağı açılmaz (sunucu en çok bir tane kabul eder);
    /// bacak sınırı dolduysa eklenemez.
    /// </summary>
    public static string? AcikHesapKilidi(
        decimal fark, string? cariId, string? pesinCariId, IReadOnlyCollection<FisBacagi> bacaklar,
        Kuyumcu.Core.I18n.TezgahFisiMetinleri m)
    {
        if (Durum(fark) != FarkDurumu.Eksik) return m.FarkTamam;
        if (string.IsNullOrEmpty(cariId)) return m.AcikHesapCariYok;
        if (!string.IsNullOrEmpty(pesinCariId) && cariId == pesinCariId) return m.AcikHesapPesin;
        if (bacaklar.Any(b => b.Arac == "VERESIYE")) return m.AcikHesapVarZaten;
        if (bacaklar.Count >= EnCokBacak) return m.FarkEksik;
        return null;
    }

    /// <summary>Sunucunun ve bacak ekranının sınırı (FisOdemeBacaklari ile aynı).</summary>
    public const int EnCokBacak = 5;

    /// <summary>Farkı kapatan açık hesap (VERESİYE) bacağı — kasası yoktur.</summary>
    public static FisBacagi AcikHesapBacagi(decimal fark) =>
        new() { Arac = "VERESIYE", TutarTl = fark, KasaId = "" };

    /// <summary>
    /// "Ödemeyi böl": tek ödemenin kendisi İLK bacak olur (araç, kasa, fişin
    /// tamamı). Tek ödemede yalnız bacağın tanıdığı araçlar taşınır; tanımadığı
    /// (ALTIN, VERESİYE) NAKİT'e düşer ve kasası boş kalır (fail-closed).
    /// </summary>
    public static FisBacagi IlkBacak(string odemeTuru, string kasaId, decimal toplam) =>
        odemeTuru is "NAKIT" or "KART" or "HAVALE"
            ? new() { Arac = odemeTuru, TutarTl = toplam, KasaId = kasaId }
            : new() { Arac = "NAKIT", TutarTl = toplam, KasaId = "" };

    /// <summary>
    /// SATIS-3: "+ Başka ödeme ekle" ile açılan İKİNCİ ödeme satırı — Nakit, tutar 0 (kullanıcı yazar;
    /// elle yazılmayan ilk satır kalanı üstlenir). TL kasası tekse otomatik, değilse fişin kasası önerilir.
    /// </summary>
    public static FisBacagi IkinciBacak(IReadOnlyList<Kuyumcu.Contracts.Sayfalar.Islemler.KasaVeri> kasalar, string? fisKasaId,
        IEnumerable<FisBacagi>? mevcut = null, IReadOnlyList<Kuyumcu.Web.Servisler.PosCihazSatir>? pos = null,
        bool nakitsiz = false)
    {
        // t10 (tasarım hakemi KRİTİK): yeni satır mevcut satırlarla AYNI araçla ("Nakit · 0") açılmaz.
        // k2 (tasarım hakemi K19): bu fişte nakit kullanılamıyorsa yeni satır Nakit AÇILMAZ.
        var yeni = new FisBacagi { Arac = YeniBacakAraci(mevcut ?? [], nakitsiz), TutarTl = 0m };
        if (pos is not null) FisHesap.BacakPosSabitle(yeni, pos);
        FisHesap.BacakKasaSabitle(yeni, kasalar);
        if (string.IsNullOrEmpty(yeni.KasaId) && !string.IsNullOrEmpty(fisKasaId)
            && FisHesap.BacakKasalari(yeni, kasalar).Any(k => k.Id == fisKasaId)) yeni.KasaId = fisKasaId;
        return yeni;
    }

    /// <summary>
    /// t10 — "+ Başka ödeme ekle" ile açılan satırın aracı: fişte henüz olmayan ilk araç (Nakit → Kart → Havale);
    /// üçü de varsa Nakit. Tek Nakit ödemeden bölününce yeni satır Kart olur. Tezgâhta ikinci ödeme çoğunlukla karttır (hakem ölçümü: yeni satır "Nakit · 0" açılıyordu).
    /// </summary>
    /// <remarks>k2 (tasarım hakemi K19, 29.09): fişte nakit KULLANILAMIYORSA (<paramref name="nakitsiz"/> — sunucunun
    /// bildirdiği had/kalan nakit hakkı; eşik sayısı burada yok) Nakit atlanır: "Başka ödeme" kullanılamayan
    /// "Nakit · 0" satırıyla açılıp Kaydet'i kilitlemesin.</remarks>
    public static string YeniBacakAraci(IEnumerable<FisBacagi> mevcut, bool nakitsiz = false)
    {
        var var_ = mevcut.Select(b => b.Arac).ToHashSet();
        foreach (var a in (string[])["NAKIT", "KART", "HAVALE"])
            if (!var_.Contains(a) && !(nakitsiz && a == "NAKIT")) return a;
        return nakitsiz ? "KART" : "NAKIT";
    }

    /// <summary>
    /// k2 (tasarım hakemi K19): tutarı 0 olan TL satırı (altın hariç) "boş satırdır" — Kaydet'i KİLİTLEMEZ ve
    /// sunucuya GÖNDERİLMEZ (kullanıcı silmiş gibi). Gönderilen satırlar yalnız bunlardır; fark kuralı bunlarla aynıdır
    /// (0 satır toplama bir şey katmaz). Altın satırı her zaman gönderilir (kendi kilidi FisAltinBacagi.Tamam).
    /// </summary>
    /// <remarks>t21 (hasım doğrulayıcı KRİTİK): DÖVİZ satırı müşterinin verdiği döviz MİKTARIYLA vardır — kur yokken TL'si 0
    /// olsa da (<see cref="DovizKuruEksik"/>) DÜŞMEZ; gövdede kalır ve Kaydet "kur alınamadı" sebebiyle kilitlenir (fail-closed).
    /// Döviz miktarı 0 ise boş satırdır, düşer.</remarks>
    public static List<FisBacagi> Gonderilecek(IEnumerable<FisBacagi> bacaklar) =>
        [.. bacaklar.Where(b => FisAltinBacagi.AltinMi(b) || b.TutarTl != 0m || (b.Arac == "DOVIZ" && b.DovizTutar > 0m))];

    /// <summary>t21 — döviz miktarı girilmiş ama TL karşılığı kurdan türetilemedi (kur kaydında o birim yok ya da kur gelmedi).
    /// İstemci kur UYDURMAZ: böyle bir satır varken fiş kaydedilemez.</summary>
    public static bool DovizKuruEksik(IEnumerable<FisBacagi> bacaklar) =>
        bacaklar.Any(b => b.Arac == "DOVIZ" && b.DovizTutar > 0m && b.TutarTl <= 0m);

    /// <summary>
    /// t10 — otomatik kalan bir satırı 0'a indirdiyse o satır düşer (hakem: "0 ₺'lik nakit satırını kullanıcı elle
    /// silmek zorunda"). Yalnız ELLE yazılmamış, türetilmemiş (altın/döviz değil) ve düzenlenen satır olmayan; en az bir
    /// satır kalır. Gövdeye yeni alan girmez; 0 ₺'lik satır zaten gönderilemezdi (Kaydet kilidi "Ödeme tutarını girin").
    /// </summary>
    /// <remarks>⚠ GOC-C-2 VERI-1 (tasarım hakemi, VERİ): ödeme ekranı bunu ARTIK ÇAĞIRMAZ — rakam rakam yazımda ilk satır
    /// kullanıcı sormadan siliniyordu. 0 ₺'lik satır k2'den beri Kaydet'i kilitlemez ve gönderilmez (<see cref="Gonderilecek"/>),
    /// yani silmeye gerek yoktur. Saf kural (eski sözleşme testleri) için yerinde bırakıldı; yeni çağrı açılmaz.</remarks>
    [System.Obsolete("GOC-C-2: ödeme ekranı bu kuralı çağırmaz (VERI-1 — satır kullanıcı sormadan silinmez). Yeni çağrı açma; yalnız eski saf kural sözleşme testleri için duruyor.")]
    public static bool SifirSatirlariDusur(IList<FisBacagi> bacaklar, FisBacagi? duzenlenen)
    {
        var dus = bacaklar.Where(b => !ReferenceEquals(b, duzenlenen) && !b.ElleYazildi && b.TutarTl == 0m
            && b.Arac is not ("DOVIZ" or FisAltinBacagi.Arac)).ToList();
        if (dus.Count == 0 || dus.Count >= bacaklar.Count) return false;
        foreach (var b in dus) bacaklar.Remove(b);
        return true;
    }
}
