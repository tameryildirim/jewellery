using System.Linq.Expressions;
using System.Reflection;

namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KAT-1 — KURUCU ŞİKÂYET KATALOĞU: TRY→₺ (K5), BASİT MOD JARGONU (K8),
// HAM KODLAR (UX-S31) — TR + EN METİNLER
//
// Kurucu (24.09): "ekranlar hakkında sana ne şikâyet ettiysem tüm sayfalarda
// kontrol edilsin" + "TRY yerine Türk lirası işaretini (₺) koyalım".
//
// ⚠ NEDEN AYRI DOSYA: Sozlukler.cs ORTAK HUB'dır (orkestratör koşulu: yeni
//   metin Sozlukler.<Alan>.cs). HUB'daki PRO metinlerin HİÇBİRİ değişmez.
// ⚠ EKSİK ANAHTAR = DERLEME HATASI (UX-S31): metinler KONUMSAL record'dadır;
//   Tr ve En aynı kurucudan geçer.
// ⚠ D10 (PLAN § 5B sözlük göçü): basit modda Cari→Müşteri (tedarikçide
//   Toptancı), Storno→Geri al (düzeltme fişi), Vezne→Kasa kuyruğu,
//   Mutabakat→Karşılaştırma, Pasife al→Kullanımdan kaldır, Denetim izi→Kim ne
//   yaptı. PROFESYONEL modda "Cari" KALIR (muhasebe dili). Mevzuat adları
//   (gider pusulası, MASAK, tevsik) iki modda da korunur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>KAT-1 metinleri (bir dilde).</summary>
public sealed record KatalogMetinleri(
    // ── K5: para birimi seçicilerinde TRY'nin okunur adı ──
    string TurkLirasiSecenek,
    // ── K8: D10 sade karşılıklar (basit mod) ──
    string Musteri,
    string Musteriler,
    string MusteriAdi,
    string Toptanci,
    string GeriAl,
    string GeriAlindi,
    string KasaKuyrugu,
    string Karsilastirma,
    string KullanimdanKaldir,
    string KullanimdanKaldirilsinMi,
    string KimNeYapti,
    // ── UX-S31: işlem defteri hareket türleri (ham kod ekrana basılmaz) ──
    string TurSatis,
    string TurSatisIade,
    string TurAlis,
    string TurAlisIade,
    string TurHurdaAlis,
    string TurTakas,
    string TurIscilik,
    string TurVeresiye,
    string TurTahsilat,
    string TurTransfer,
    string TurEmanetGiris,
    string TurEmanetCikis,
    string TurCariDuzeltme,
    string TurMusteriDuzeltme,
    string TurAcilis,
    string TurEvrakGiris,
    string TurEvrakCiro,
    string TurEvrakKarsiliksiz,
    string TurEvrakRucu,
    string TurHurdaSatis,
    string TurSayimDuzeltme,
    string TurTahlilFarki,
    string TurAvansAlindi,
    string TurAvansIade,
    string TurAvansMahsup,
    string TurDovizTahsilat,
    string TurDovizVeresiye,
    string TurKonsinyeCikis,
    string TurKonsinyeIade,
    // ── UX-S31: ödeme araçları ──
    string OdemeNakit,
    string OdemeKart,
    string OdemeHavale,
    string OdemeAltin,
    string OdemeVeresiye,
    string OdemeKarma,
    string OdemeDoviz,
    string OdemeDiger,
    // ── Cariler döviz tahsilatı: "{0} kasası yok" (çıplak metin sözlüğe taşındı) ──
    string DovizKasasiYok,
    // ── K8 (basit): ayrı sözlük dosyalarındaki iki jargon metninin basit hâli ──
    string BankaEslesmisYoluBasit,
    string PozisyonDefterFarkiBasit,
    // ── K4 (tren): /raporlar profesyonel yüz sekmeleri ──
    string RaporSekmeSerit,
    string RaporSekmeOzet,
    string RaporSekmeGrafik,
    string RaporSekmeCari,
    string RaporSekmeStok,
    string RaporSekmeYaslandirma,
    string RaporSekmeGunSonu,
    // ── K4 (tren): /islemler son satışlar kartı sınırı (UX-S29) ──
    string SonSatisSinir,
    string SonSatisHepsi,
    string SonSatisIlkleri,
    string SonSatisDefter);

public static class KatalogSozlugu
{
    public static readonly KatalogMetinleri Tr = new(
        TurkLirasiSecenek: "₺ Türk lirası",
        Musteri: "Müşteri",
        Musteriler: "Müşteriler",
        MusteriAdi: "Müşteri adı",
        Toptanci: "Toptancı",
        GeriAl: "Geri al (düzeltme fişi)",
        GeriAlindi: "Geri alındı",
        KasaKuyrugu: "Kasa kuyruğu",
        Karsilastirma: "Karşılaştırma",
        KullanimdanKaldir: "Kullanımdan kaldır",
        KullanimdanKaldirilsinMi: "Kullanımdan kaldırılsın mı?",
        KimNeYapti: "Kim ne yaptı",
        TurSatis: "Satış",
        TurSatisIade: "Satış iadesi",
        TurAlis: "Alış",
        TurAlisIade: "Alış iadesi",
        TurHurdaAlis: "Hurda alışı",
        TurTakas: "Takas",
        TurIscilik: "İşçilik",
        TurVeresiye: "Veresiye",
        TurTahsilat: "Tahsilat",
        TurTransfer: "Transfer",
        TurEmanetGiris: "Emanet girişi",
        TurEmanetCikis: "Emanet çıkışı",
        TurCariDuzeltme: "Cari düzeltme",
        TurMusteriDuzeltme: "Bakiye düzeltme",
        TurAcilis: "Açılış bakiyesi",
        TurEvrakGiris: "Çek/senet girişi",
        TurEvrakCiro: "Çek/senet devri",
        TurEvrakKarsiliksiz: "Karşılıksız çek/senet",
        TurEvrakRucu: "Çek/senet geri döndü",
        TurHurdaSatis: "Hurda satışı",
        TurSayimDuzeltme: "Sayım düzeltmesi",
        TurTahlilFarki: "Tahlil farkı",
        TurAvansAlindi: "Avans alındı",
        TurAvansIade: "Avans iadesi",
        TurAvansMahsup: "Avans mahsubu",
        TurDovizTahsilat: "Döviz tahsilatı",
        TurDovizVeresiye: "Döviz veresiye",
        TurKonsinyeCikis: "Emanet mal çıkışı",
        TurKonsinyeIade: "Emanet mal iadesi",
        OdemeNakit: "Nakit",
        OdemeKart: "Kart",
        OdemeHavale: "Havale",
        OdemeAltin: "Altın",
        OdemeVeresiye: "Veresiye",
        OdemeKarma: "Karışık",
        OdemeDoviz: "Döviz",
        OdemeDiger: "Diğer",
        DovizKasasiYok: "{0} kasası yok — önce {0} kasası açın.",
        BankaEslesmisYoluBasit: "Müşteri yanlışsa dekontu geri alın (düzeltme fişi) ve doğru müşteriyle yeni dekont girin.",
        PozisyonDefterFarkiBasit: "Uyarı: müşterilerin altın bakiyesi ile defter {0} g farklı. Müşteri hesap dökümlerini kontrol edin.",
        RaporSekmeSerit: "Rapor bölümleri",
        RaporSekmeOzet: "Özet",
        RaporSekmeGrafik: "Grafikler",
        RaporSekmeCari: "Cari yaşlandırma",
        RaporSekmeStok: "Stok değeri",
        RaporSekmeYaslandirma: "Stok yaşı",
        RaporSekmeGunSonu: "Gün sonu",
        SonSatisSinir: "{0} / {1} fiş gösteriliyor",
        SonSatisHepsi: "Hepsini göster ({0})",
        SonSatisIlkleri: "İlk {0} fişi göster",
        SonSatisDefter: "Tüm geçmiş: İşlem defteri →");

    public static readonly KatalogMetinleri En = new(
        TurkLirasiSecenek: "₺ Turkish lira",
        Musteri: "Customer",
        Musteriler: "Customers",
        MusteriAdi: "Customer name",
        Toptanci: "Wholesaler",
        GeriAl: "Undo (correction slip)",
        GeriAlindi: "Undone",
        KasaKuyrugu: "Till queue",
        Karsilastirma: "Comparison",
        KullanimdanKaldir: "Retire",
        KullanimdanKaldirilsinMi: "Retire this record?",
        KimNeYapti: "Who did what",
        TurSatis: "Sale",
        TurSatisIade: "Sale return",
        TurAlis: "Purchase",
        TurAlisIade: "Purchase return",
        TurHurdaAlis: "Scrap purchase",
        TurTakas: "Exchange",
        TurIscilik: "Labour",
        TurVeresiye: "On credit",
        TurTahsilat: "Collection",
        TurTransfer: "Transfer",
        TurEmanetGiris: "Custody in",
        TurEmanetCikis: "Custody out",
        TurCariDuzeltme: "Account adjustment",
        TurMusteriDuzeltme: "Balance adjustment",
        TurAcilis: "Opening balance",
        TurEvrakGiris: "Cheque/note in",
        TurEvrakCiro: "Cheque/note endorsed",
        TurEvrakKarsiliksiz: "Bounced cheque/note",
        TurEvrakRucu: "Cheque/note returned",
        TurHurdaSatis: "Scrap sale",
        TurSayimDuzeltme: "Stock count adjustment",
        TurTahlilFarki: "Assay difference",
        TurAvansAlindi: "Advance received",
        TurAvansIade: "Advance refund",
        TurAvansMahsup: "Advance offset",
        TurDovizTahsilat: "Currency collection",
        TurDovizVeresiye: "Currency on credit",
        TurKonsinyeCikis: "Consignment out",
        TurKonsinyeIade: "Consignment return",
        OdemeNakit: "Cash",
        OdemeKart: "Card",
        OdemeHavale: "Bank transfer",
        OdemeAltin: "Gold",
        OdemeVeresiye: "On credit",
        OdemeKarma: "Mixed",
        OdemeDoviz: "Foreign currency",
        OdemeDiger: "Other",
        DovizKasasiYok: "No {0} till — open a {0} till first.",
        BankaEslesmisYoluBasit: "If the customer is wrong, undo the slip (correction slip) and enter a new one with the right customer.",
        PozisyonDefterFarkiBasit: "Warning: customers' gold balance differs from the ledger by {0} g. Check the customer statements.",
        RaporSekmeSerit: "Report sections",
        RaporSekmeOzet: "Summary",
        RaporSekmeGrafik: "Charts",
        RaporSekmeCari: "Account ageing",
        RaporSekmeStok: "Stock value",
        RaporSekmeYaslandirma: "Stock age",
        RaporSekmeGunSonu: "End of day",
        SonSatisSinir: "Showing {0} of {1} receipts",
        SonSatisHepsi: "Show all ({0})",
        SonSatisIlkleri: "Show first {0}",
        SonSatisDefter: "Full history: Transaction ledger →");

    public static KatalogMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>
    /// K5 — para birimi SEÇİCİSİNDE seçeneğin okunur adı: TRY → "₺ Türk lirası";
    /// USD/EUR/GBP kodu AYNEN kalır (kurucu yalnız TRY dedi). Seçeneğin DEĞERİ
    /// (gövdeye giden kod) çağıranda "TRY" kalır — bu yalnız görünen metindir.
    /// </summary>
    public static string ParaSecenegi(string? kod, string dil) => kod switch
    {
        null or "" or "TRY" => Al(dil).TurkLirasiSecenek,
        _ => kod,
    };

    /// <summary>
    /// UX-S31 — işlem defteri hareket türünün okunur adı. Ham kod (SATIS_IADE,
    /// EMANET_GIRIS …) ekrana basılmaz. <paramref name="basit"/> = basit modda
    /// "Cari düzeltme" yerine jargonsuz ad. Bilinmeyen kod ad UYDURULMADAN
    /// kendisiyle döner (Anayasa md.1) — ölçüm (ham enum taraması) onu yakalar.
    /// </summary>
    public static string HareketTuru(string? kod, string dil, bool basit = false)
    {
        var m = Al(dil);
        return kod switch
        {
            "SATIS" => m.TurSatis,
            "SATIS_IADE" => m.TurSatisIade,
            "ALIS" => m.TurAlis,
            "ALIS_IADE" => m.TurAlisIade,
            "HURDA_ALIS" => m.TurHurdaAlis,
            "TAKAS" => m.TurTakas,
            "ISCILIK" => m.TurIscilik,
            "VERESIYE" => m.TurVeresiye,
            "TAHSILAT" => m.TurTahsilat,
            "TRANSFER" => m.TurTransfer,
            "EMANET_GIRIS" => m.TurEmanetGiris,
            "EMANET_CIKIS" => m.TurEmanetCikis,
            "CARI_DUZELTME" => basit ? m.TurMusteriDuzeltme : m.TurCariDuzeltme,
            "ACILIS" => m.TurAcilis,
            "EVRAK_GIRIS" => m.TurEvrakGiris,
            "EVRAK_CIRO" => m.TurEvrakCiro,
            "EVRAK_KARSILIKSIZ" => m.TurEvrakKarsiliksiz,
            "EVRAK_RUCU" => m.TurEvrakRucu,
            "HURDA_SATIS" => m.TurHurdaSatis,
            "SAYIM_DUZELTME" => m.TurSayimDuzeltme,
            "TAHLIL_FARKI" => m.TurTahlilFarki,
            "AVANS_ALINDI" => m.TurAvansAlindi,
            "AVANS_IADE" => m.TurAvansIade,
            "AVANS_MAHSUP" => m.TurAvansMahsup,
            "DOVIZ_TAHSILAT" => m.TurDovizTahsilat,
            "DOVIZ_VERESIYE" => m.TurDovizVeresiye,
            "KONSINYE_CIKIS" => m.TurKonsinyeCikis,
            "KONSINYE_IADE" => m.TurKonsinyeIade,
            null or "" => "—",
            _ => kod,
        };
    }

    /// <summary>UX-S31 — ödeme aracının okunur adı (NAKIT, KART … ekrana basılmaz).</summary>
    public static string OdemeAraci(string? kod, string dil)
    {
        var m = Al(dil);
        return kod switch
        {
            "NAKIT" => m.OdemeNakit,
            "KART" => m.OdemeKart,
            "HAVALE" or "EFT" => m.OdemeHavale,
            "ALTIN" => m.OdemeAltin,
            "VERESIYE" => m.OdemeVeresiye,
            "KARMA" => m.OdemeKarma,
            "DOVIZ" => m.OdemeDoviz,
            "DIGER" => m.OdemeDiger,   // PAKET DIL-SUNUCU: ham kod ekranda görünmez (açılış devri satırı DIGER yazar)
            null or "" => "—",
            _ => kod,
        };
    }
}

/// <summary>
/// PAKET KAT-1 (K8) — BASİT MOD SÖZLÜK KATMANI.
///
/// <para>
/// Basit modda <c>DilServisi.T</c> bu katmanı döndürür: PRO sözlüğün KOPYASI,
/// üzerinde yalnız D10 jargon anahtarları <see cref="KatalogMetinleri"/>'deki
/// AYRI basit anahtarlarla değiştirilmiş hâlde. PRO sözlük nesnesine DOKUNULMAZ
/// (kopya üzerinde çalışılır); profesyonel mod "Cari"yi görmeye devam eder.
/// </para>
/// <para>
/// ⚠ Anahtar yolları LAMBDA ile verilir (<c>s =&gt; s.Cari.Baslik</c>):
/// yanlış/silinmiş anahtar DERLEME hatasıdır, dizge yolu yoktur (UX-S31).
/// </para>
/// </summary>
public static class BasitSozluk
{
    private static readonly Lazy<Sozluk> tr = new(() => Kur(Sozlukler.Getir("tr"), "tr"));
    private static readonly Lazy<Sozluk> en = new(() => Kur(Sozlukler.Getir("en"), "en"));

    /// <summary>Basit mod sözlüğü (dil başına bir kez kurulur).</summary>
    public static Sozluk Getir(string dil) => dil == "en" ? en.Value : tr.Value;

    /// <summary>Katmanın değiştirdiği anahtar sayısı (test/denetim için).</summary>
    public static int AnahtarSayisi => Degisikliklerini("tr").Count + BasitDilSozlugu.SozlukEsleme(BasitDilSozlugu.Tr).Count;

    private static Sozluk Kur(Sozluk pro, string dil)
    {
        var katman = new Katman(pro);
        foreach (var (yol, deger) in Degisikliklerini(dil)) katman.Ata(yol, deger);
        // PAKET CUMA-J: ek basit karşılıklar ayrı dosyada (Sozlukler.CumaJ.cs).
        foreach (var (yol, deger) in CumaJSozlugu.Esleme(CumaJSozlugu.Sade(dil))) katman.Ata(yol, deger);
        foreach (var (yol, anahtar, deger) in BasitDilSozlugu.SozlukEsleme(BasitDilSozlugu.Al(dil)))
            katman.SozlukAta(yol, anahtar, deger);
        // PAKET KESIF-P8: sözlük taramasının BASİT karşılıkları (cari/pasif/sunucu/storno/append-only).
        foreach (var (yol, deger) in KesifP8BasitSozlugu.Esleme(dil)) katman.Ata(yol, deger);
        return katman.Kok;
    }

    /// <summary>
    /// D10 eşlemesi: PRO anahtar → BASİT değer. Liste YALNIZ basit modda
    /// görünen jargonu değiştirir; mevzuat adı (gider pusulası, MASAK) yoktur.
    /// </summary>
    internal static List<(Expression<Func<Sozluk, string>> Yol, string Deger)> Degisikliklerini(string dil) =>
        BasitDilSozlugu.Esleme(BasitDilSozlugu.Al(dil));

    /// <summary>Kopyala-yaz katmanı: yol üzerindeki her ara nesne BİR KEZ kopyalanır.</summary>
    private sealed class Katman(Sozluk pro)
    {
        private static readonly MethodInfo Kopyala =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Orijinal nesne → kopyası (aynı alt sözlük iki kez kopyalanmasın).
        private readonly Dictionary<object, object> kopyalar = new(ReferenceEqualityComparer.Instance);

        public Sozluk Kok { get; } = (Sozluk)Kopyala.Invoke(pro, null)!;

        /// <summary>Sözlük girdisi: sözlük KOPYALANIR (PRO sözlük değişmez), girdi kopyada değişir.</summary>
        public void SozlukAta(Expression<Func<Sozluk, Dictionary<string, string>>> yol, string anahtar, string deger)
        {
            var (sahip, ozellik) = Yuru(yol.Body);
            var mevcut = (Dictionary<string, string>)ozellik.GetValue(sahip)!;
            if (!mevcut.ContainsKey(anahtar))
                throw new InvalidOperationException($"BasitSozluk: {ozellik.Name} içinde '{anahtar}' yok");
            if (!kopyalar.ContainsKey(mevcut))
            {
                var kopya = new Dictionary<string, string>(mevcut, mevcut.Comparer);
                kopyalar[mevcut] = kopya;
                kopyalar[kopya] = kopya;
                ozellik.SetValue(sahip, kopya);
                mevcut = kopya;
            }
            mevcut[anahtar] = deger;
        }

        /// <summary>s =&gt; s.A.B.C : A ve B'yi kopyalayarak yürür; (C'nin sahibi, C) döner.</summary>
        private (object Sahip, PropertyInfo Ozellik) Yuru(Expression govde)
        {
            var zincir = new List<PropertyInfo>();
            var ifade = govde;
            while (ifade is MemberExpression me && me.Member is PropertyInfo pi)
            {
                zincir.Insert(0, pi);
                ifade = me.Expression!;
            }
            if (ifade is not ParameterExpression || zincir.Count == 0)
                throw new InvalidOperationException($"BasitSozluk: desteklenmeyen yol {govde}");
            object hedef = Kok;
            for (var i = 0; i < zincir.Count - 1; i++)
            {
                var ozellik = zincir[i];
                var alt = ozellik.GetValue(hedef)
                    ?? throw new InvalidOperationException($"BasitSozluk: {ozellik.Name} boş");
                if (!kopyalar.TryGetValue(alt, out var altKopya))
                {
                    altKopya = Kopyala.Invoke(alt, null)!;
                    kopyalar[alt] = altKopya;
                    kopyalar[altKopya] = altKopya;
                }
                ozellik.SetValue(hedef, altKopya);
                hedef = altKopya;
            }
            return (hedef, zincir[^1]);
        }

        public void Ata(Expression<Func<Sozluk, string>> yol, string deger)
        {
            // s => s.A.B.C  →  [A, B, C]
            var zincir = new List<PropertyInfo>();
            var ifade = yol.Body;
            while (ifade is MemberExpression me && me.Member is PropertyInfo pi)
            {
                zincir.Insert(0, pi);
                ifade = me.Expression!;
            }
            if (ifade is not ParameterExpression || zincir.Count == 0)
                throw new InvalidOperationException($"BasitSozluk: desteklenmeyen yol {yol}");

            object hedef = Kok;
            for (var i = 0; i < zincir.Count - 1; i++)
            {
                var ozellik = zincir[i];
                var alt = ozellik.GetValue(hedef)
                    ?? throw new InvalidOperationException($"BasitSozluk: {ozellik.Name} boş");
                if (!kopyalar.TryGetValue(alt, out var altKopya))
                {
                    altKopya = Kopyala.Invoke(alt, null)!;
                    kopyalar[alt] = altKopya;
                    kopyalar[altKopya] = altKopya;
                }
                ozellik.SetValue(hedef, altKopya);
                hedef = altKopya;
            }
            zincir[^1].SetValue(hedef, deger);
        }
    }
}

/// <summary>
/// KAT-1 (K8 · D10) — BASİT MOD karşılıkları. Her alan PRO sözlükteki bir
/// anahtarın basit hâlidir; alan adı PRO anahtarın yoludur (Nokta → alt çizgi).
/// PRO değer DEĞİŞMEZ; bu değerler yalnız basit modda <see cref="BasitSozluk"/>
/// katmanından okunur. Konumsal record: eksik anahtar = derleme hatası.
/// </summary>
public sealed record BasitDilMetinleri(
    string Nav_Cariler,
    string Nav_Vezne,
    string Cari_Baslik,
    string Cari_CariDuzenle,
    string Cari_PasifeAlOnay,
    string Cari_YeniCari,
    string Cari_CariBulunamadi,
    string Cari_Olusturulamadi,
    string Cari_CariDetay,
    string Cari_CariSecin,
    string Cari_CariAra,
    string Islem_CariSecin,
    string Islem_CaridenGeldi,
    string Islem_PasifCariGizlendi,
    string Pos_PasifeAl,
    string Rapor_CariYaslandirma,
    string Rapor_CariBasligi,
    string RaporSeti_AdPosMutabakat,
    string RaporSeti_AdCariYaslandirma,
    string RaporSeti_CariAd,
    string RaporSeti_Storno,
    string RaporSeti_StornoAdet,
    string RaporSeti_StornoAciklama,
    string Banka_DekontCariIpucu,
    string Banka_MasakCariUyari,
    string Banka_CariBagla,
    string Banka_CarisizSatirUyari,
    string Kabuk_TurCari,
    string Kabuk_Storno,
    string Kabuk_StornoGerekce,
    string Kabuk_StornoYapildi,
    string Kabuk_PasifeAl,
    string Denetim_DegistirilemezNot,
    string Denetim_Storno,
    string Denetim_StornoGerekce,
    string Denetim_StornoYapildi,
    string Gider_KategoriPasifeAl,
    string Gider_Storno,
    string Gider_StornoGerekcesi,
    string Gider_StornoSatiri,
    string Uyum_RiskCariSec,
    string Uyum_RiskCariSecilmedi,
    string Fiyat_PasifeAl,
    string Mutabakat_Baslik,
    string Belge_PasifeAl,
    string Duzeltme_DenetimGerekce,
    string SayfaDuzeltme_CariKartinaGit,
    string UrunAgaci_KategoriPasifeAl,
    string KarZarar_Storno,
    string KarZarar_StornoNotu,
    string Vezne_Baslik,
    string Vezne_AyrimZorunlu,
    string Siparis_Cari,
    string Siparis_CariGerekli,
    string Yonetim_SubeAktifeAlmaGerekce,
    // ── Sözlük (Dictionary) girdileri: alan adı = yol + "__" + anahtar ──
    string SayfaDuzeltme_VarlikAdlari__Cari,
    string SayfaDuzeltme_VarlikAdlari__CariBakiye);

public static class BasitDilSozlugu
{
    public static readonly BasitDilMetinleri Tr = new(
        Nav_Cariler: "Müşteriler",
        Nav_Vezne: "Kasa kuyruğu",
        Cari_Baslik: "Müşteriler",
        Cari_CariDuzenle: "Müşteriyi düzenle",
        Cari_PasifeAlOnay: "Müşteri kullanımdan kaldırılacak. Geçmişi ve bakiyesi korunur, yeni işlemde seçilemez. Onaylıyor musunuz?",
        Cari_YeniCari: "Yeni müşteri",
        Cari_CariBulunamadi: "Müşteri bulunamadı",
        Cari_Olusturulamadi: "Müşteri eklenemedi",
        Cari_CariDetay: "Müşteri ayrıntısı",
        Cari_CariSecin: "Müşteri seçin...",
        Cari_CariAra: "Müşteri ara",
        Islem_CariSecin: "Müşteri seçin",
        Islem_CaridenGeldi: "Müşteriden geldi",
        Islem_PasifCariGizlendi: "Kullanımdan kaldırılan müşteriler burada görünmez; Müşteriler ekranından geri açın.",
        Pos_PasifeAl: "Kullanımdan kaldır",
        Rapor_CariYaslandirma: "Müşteri borç yaşı raporu",
        Rapor_CariBasligi: "Müşteri",
        RaporSeti_AdPosMutabakat: "Gün sonu karşılaştırması",
        RaporSeti_AdCariYaslandirma: "Müşteri borç yaşı",
        RaporSeti_CariAd: "Müşteri",
        RaporSeti_Storno: "Geri alınan",
        RaporSeti_StornoAdet: "Geri alınan adet",
        RaporSeti_StornoAciklama: "Geri alınan işlemler toplamlara katılmaz; iptal edilen satış ciroyu şişirmez.",
        Banka_DekontCariIpucu: "Müşteri seçilirse aynı gün aynı müşteriyle yapılan işlemler MASAK eşiğinde birlikte değerlendirilir.",
        Banka_MasakCariUyari: "Müşteri seçilmediği için bu tutar tek başına değerlendirilir; bugün aynı kişiyle yapılan diğer işlemler toplanamaz. Kişiyi bağlamak için müşteri seçin.",
        Banka_CariBagla: "Müşteriye bağla",
        Banka_CarisizSatirUyari: "Seçili {0} satırda müşteri eşleşmesi yok. Müşteri seçmeden onaylarsanız müşterisiz banka hareketi oluşur.",
        Kabuk_TurCari: "Müşteri",
        Kabuk_Storno: "Geri al (düzeltme fişi)",
        Kabuk_StornoGerekce: "Geri alma sebebi",
        Kabuk_StornoYapildi: "Geri alındı. Düzeltme fişi no: {0}",
        Kabuk_PasifeAl: "Kullanımdan kaldır",
        Denetim_DegistirilemezNot: "Kim ne yaptı kaydı yalnız eklenir: kayıt asla silinmez ya da değiştirilmez.",
        Denetim_Storno: "Geri al (düzeltme fişi)",
        Denetim_StornoGerekce: "Geri alma sebebi (en az 20 karakter)",
        Denetim_StornoYapildi: "Geri alındı: düzeltme fişi yazıldı, asıl satıra dokunulmadı.",
        Gider_KategoriPasifeAl: "Kullanımdan kaldır",
        Gider_Storno: "Geri al (düzeltme fişi)",
        Gider_StornoGerekcesi: "Geri alma sebebi (en az 20 karakter)",
        Gider_StornoSatiri: "Düzeltme fişi satırı",
        Uyum_RiskCariSec: "Müşteri seç",
        Uyum_RiskCariSecilmedi: "Geçmişi görmek için bir müşteri seçin. Seçmeden kayıt açılmaz: her açılış kayıt altına alınır (KVKK md.12).",
        Fiyat_PasifeAl: "Kullanımdan kaldır",
        Mutabakat_Baslik: "Gün sonu karşılaştırması",
        Belge_PasifeAl: "Kullanımdan kaldır",
        Duzeltme_DenetimGerekce: "Kim ne yaptı kaydı yalnız eklenir, hiç değiştirilmez — kim neye baktı, ne yazdı sorusunun cevabı budur.",
        SayfaDuzeltme_CariKartinaGit: "Müşteri kartını aç",
        UrunAgaci_KategoriPasifeAl: "Kullanımdan kaldır",
        KarZarar_Storno: "Geri alınan",
        KarZarar_StornoNotu: "Geri alınan işlemler (iptal edilen kayıt ve düzeltme fişi) ciro, adet ve kâra katılmaz; burada ayrıca sayılır.",
        Vezne_Baslik: "Kasa kuyruğu",
        Vezne_AyrimZorunlu: "Kasa kuyruğu ayrımı zorunlu",
        Siparis_Cari: "Müşteri",
        Siparis_CariGerekli: "Müşteri seçimi zorunlu.",
        Yonetim_SubeAktifeAlmaGerekce: "Kullanımdan kaldırılan şube bu ekrandan geri açılamıyor: bunun yolu henüz yok. Etkisiz düğme koymak yerine durumu olduğu gibi yazıyoruz.",
        SayfaDuzeltme_VarlikAdlari__Cari: "Müşteri",
        SayfaDuzeltme_VarlikAdlari__CariBakiye: "Müşteri bakiyesi");

    public static readonly BasitDilMetinleri En = new(
        Nav_Cariler: "Customers",
        Nav_Vezne: "Till queue",
        Cari_Baslik: "Customers",
        Cari_CariDuzenle: "Edit customer",
        Cari_PasifeAlOnay: "The customer will be retired. History and balance are kept; it cannot be picked in new sales. Confirm?",
        Cari_YeniCari: "New customer",
        Cari_CariBulunamadi: "Customer not found",
        Cari_Olusturulamadi: "Customer could not be added",
        Cari_CariDetay: "Customer details",
        Cari_CariSecin: "Pick a customer...",
        Cari_CariAra: "Search customer",
        Islem_CariSecin: "Pick a customer",
        Islem_CaridenGeldi: "From the customer",
        Islem_PasifCariGizlendi: "Retired customers are hidden here; bring them back on the Customers screen.",
        Pos_PasifeAl: "Retire",
        Rapor_CariYaslandirma: "Customer debt age report",
        Rapor_CariBasligi: "Customer",
        RaporSeti_AdPosMutabakat: "End-of-day comparison",
        RaporSeti_AdCariYaslandirma: "Customer debt age",
        RaporSeti_CariAd: "Customer",
        RaporSeti_Storno: "Undone",
        RaporSeti_StornoAdet: "Undone count",
        RaporSeti_StornoAciklama: "Undone sales are left out of totals; a cancelled sale does not inflate turnover.",
        Banka_DekontCariIpucu: "If you pick a customer, today's deals with the same customer are counted together for the MASAK limit.",
        Banka_MasakCariUyari: "No customer picked, so this amount is judged alone; today's other deals with the same person cannot be added up. Pick a customer to link them.",
        Banka_CariBagla: "Link to customer",
        Banka_CarisizSatirUyari: "{0} selected rows have no customer. If you confirm without one, the bank entries stay unlinked.",
        Kabuk_TurCari: "Customer",
        Kabuk_Storno: "Undo (correction slip)",
        Kabuk_StornoGerekce: "Reason for undo",
        Kabuk_StornoYapildi: "Undone. Correction slip no: {0}",
        Kabuk_PasifeAl: "Retire",
        Denetim_DegistirilemezNot: "The who-did-what log only grows: entries are never deleted or changed.",
        Denetim_Storno: "Undo (correction slip)",
        Denetim_StornoGerekce: "Reason for undo (at least 20 characters)",
        Denetim_StornoYapildi: "Undone: a correction slip was written, the original row was not touched.",
        Gider_KategoriPasifeAl: "Retire",
        Gider_Storno: "Undo (correction slip)",
        Gider_StornoGerekcesi: "Reason for undo (at least 20 characters)",
        Gider_StornoSatiri: "Correction slip row",
        Uyum_RiskCariSec: "Pick customer",
        Uyum_RiskCariSecilmedi: "Pick a customer to see the history. Nothing opens before you pick: each opening is logged (KVKK art.12).",
        Fiyat_PasifeAl: "Retire",
        Mutabakat_Baslik: "End-of-day comparison",
        Belge_PasifeAl: "Retire",
        Duzeltme_DenetimGerekce: "The who-did-what log only grows and never changes — it answers who looked at or wrote what.",
        SayfaDuzeltme_CariKartinaGit: "Open customer card",
        UrunAgaci_KategoriPasifeAl: "Retire",
        KarZarar_Storno: "Undone",
        KarZarar_StornoNotu: "Undone deals (the cancelled entry and its correction slip) are left out of turnover, count and profit; they are counted here separately.",
        Vezne_Baslik: "Till queue",
        Vezne_AyrimZorunlu: "Till-queue separation required",
        Siparis_Cari: "Customer",
        Siparis_CariGerekli: "Picking a customer is required.",
        Yonetim_SubeAktifeAlmaGerekce: "A retired branch cannot be reopened here yet: there is no way to do it yet. Rather than a dead button, we say so plainly.",
        SayfaDuzeltme_VarlikAdlari__Cari: "Customer",
        SayfaDuzeltme_VarlikAdlari__CariBakiye: "Customer balance");

    public static BasitDilMetinleri Al(string dil) => dil == "en" ? En : Tr;

    /// <summary>PRO anahtar yolu → basit değer (lambda yol: yanlış anahtar derlenmez).</summary>
    /// <summary>Sözlük (Dictionary) girdileri: PRO sözlüğün yolu + anahtar → basit değer.</summary>
    internal static List<(System.Linq.Expressions.Expression<Func<Sozluk, Dictionary<string, string>>> Yol, string Anahtar, string Deger)> SozlukEsleme(BasitDilMetinleri b) =>
    [
        (s => s.SayfaDuzeltme.VarlikAdlari, "Cari", b.SayfaDuzeltme_VarlikAdlari__Cari),
        (s => s.SayfaDuzeltme.VarlikAdlari, "CariBakiye", b.SayfaDuzeltme_VarlikAdlari__CariBakiye),
    ];

    internal static List<(System.Linq.Expressions.Expression<Func<Sozluk, string>> Yol, string Deger)> Esleme(BasitDilMetinleri b) =>
    [
        (s => s.Nav.Cariler, b.Nav_Cariler),
        (s => s.Nav.Vezne, b.Nav_Vezne),
        (s => s.Cari.Baslik, b.Cari_Baslik),
        (s => s.Cari.CariDuzenle, b.Cari_CariDuzenle),
        (s => s.Cari.PasifeAlOnay, b.Cari_PasifeAlOnay),
        (s => s.Cari.YeniCari, b.Cari_YeniCari),
        (s => s.Cari.CariBulunamadi, b.Cari_CariBulunamadi),
        (s => s.Cari.Olusturulamadi, b.Cari_Olusturulamadi),
        (s => s.Cari.CariDetay, b.Cari_CariDetay),
        (s => s.Cari.CariSecin, b.Cari_CariSecin),
        (s => s.Cari.CariAra, b.Cari_CariAra),
        (s => s.Islem.CariSecin, b.Islem_CariSecin),
        (s => s.Islem.CaridenGeldi, b.Islem_CaridenGeldi),
        (s => s.Islem.PasifCariGizlendi, b.Islem_PasifCariGizlendi),
        (s => s.Pos.PasifeAl, b.Pos_PasifeAl),
        (s => s.Rapor.CariYaslandirma, b.Rapor_CariYaslandirma),
        (s => s.Rapor.CariBasligi, b.Rapor_CariBasligi),
        (s => s.RaporSeti.AdPosMutabakat, b.RaporSeti_AdPosMutabakat),
        (s => s.RaporSeti.AdCariYaslandirma, b.RaporSeti_AdCariYaslandirma),
        (s => s.RaporSeti.CariAd, b.RaporSeti_CariAd),
        (s => s.RaporSeti.Storno, b.RaporSeti_Storno),
        (s => s.RaporSeti.StornoAdet, b.RaporSeti_StornoAdet),
        (s => s.RaporSeti.StornoAciklama, b.RaporSeti_StornoAciklama),
        (s => s.Banka.DekontCariIpucu, b.Banka_DekontCariIpucu),
        (s => s.Banka.MasakCariUyari, b.Banka_MasakCariUyari),
        (s => s.Banka.CariBagla, b.Banka_CariBagla),
        (s => s.Banka.CarisizSatirUyari, b.Banka_CarisizSatirUyari),
        (s => s.Kabuk.TurCari, b.Kabuk_TurCari),
        (s => s.Kabuk.Storno, b.Kabuk_Storno),
        (s => s.Kabuk.StornoGerekce, b.Kabuk_StornoGerekce),
        (s => s.Kabuk.StornoYapildi, b.Kabuk_StornoYapildi),
        (s => s.Kabuk.PasifeAl, b.Kabuk_PasifeAl),
        (s => s.Denetim.DegistirilemezNot, b.Denetim_DegistirilemezNot),
        (s => s.Denetim.Storno, b.Denetim_Storno),
        (s => s.Denetim.StornoGerekce, b.Denetim_StornoGerekce),
        (s => s.Denetim.StornoYapildi, b.Denetim_StornoYapildi),
        (s => s.Gider.KategoriPasifeAl, b.Gider_KategoriPasifeAl),
        (s => s.Gider.Storno, b.Gider_Storno),
        (s => s.Gider.StornoGerekcesi, b.Gider_StornoGerekcesi),
        (s => s.Gider.StornoSatiri, b.Gider_StornoSatiri),
        (s => s.Uyum.RiskCariSec, b.Uyum_RiskCariSec),
        (s => s.Uyum.RiskCariSecilmedi, b.Uyum_RiskCariSecilmedi),
        (s => s.Fiyat.PasifeAl, b.Fiyat_PasifeAl),
        (s => s.Mutabakat.Baslik, b.Mutabakat_Baslik),
        (s => s.Belge.PasifeAl, b.Belge_PasifeAl),
        (s => s.Duzeltme.DenetimGerekce, b.Duzeltme_DenetimGerekce),
        (s => s.SayfaDuzeltme.CariKartinaGit, b.SayfaDuzeltme_CariKartinaGit),
        (s => s.UrunAgaci.KategoriPasifeAl, b.UrunAgaci_KategoriPasifeAl),
        (s => s.KarZarar.Storno, b.KarZarar_Storno),
        (s => s.KarZarar.StornoNotu, b.KarZarar_StornoNotu),
        (s => s.Vezne.Baslik, b.Vezne_Baslik),
        (s => s.Vezne.AyrimZorunlu, b.Vezne_AyrimZorunlu),
        (s => s.Siparis.Cari, b.Siparis_Cari),
        (s => s.Siparis.CariGerekli, b.Siparis_CariGerekli),
        (s => s.Yonetim.SubeAktifeAlmaGerekce, b.Yonetim_SubeAktifeAlmaGerekce),
    ];
}

/// <summary>
/// PAKET KESIF-P8 — BASİT MOD SÖZLÜK TARAMASININ KARŞILIKLARI (#18 #25 #50 #52
/// #74 #76 #81 #83 #87 + birleştirilen TOPLU maddeleri).
///
/// <para>
/// <c>BasitSozluk.Getir("tr")</c> bütün metin yapraklarında <c>cari</c>, <c>pasif</c>,
/// <c>sunucu</c>, <c>storno</c>, <c>append-only</c> ve BÜYÜK_ALT_ÇİZGİ ham kod 0 olsun
/// diye (KesifSozlukTaramaTestleri) PRO metni bu sözcükleri taşıyan her anahtarın
/// BASİT karşılığı buradadır. PRO sözlük DEĞİŞMEZ (D10: muhasebe dili "Cari"/"Storno"
/// PRO'da kalır); yasal tam metin PRO'da yaşamaya devam eder (SADE-1: metin silinmez).
/// </para>
/// <para>
/// ⚠ NEDEN <see cref="BasitDilMetinleri"/> değil: o kaydın sözleşmesi (KAT-1 testi
/// KAT_K8_02) her PRO değerinin büyük harfli D10 jargon kalıbını ("Cari", "Pasife al")
/// taşımasını ister; buradaki PRO metinlerin çoğu küçük harfli ("cari", "pasife
/// alınacak") ya da başka sözcük ("sunucu") taşır. Ayrı liste, aynı katmana TEK satırla
/// bağlanır (<see cref="BasitSozluk"/> Kur()).
/// </para>
/// <para>
/// ⚠ EKSİK DİL = DERLEME HATASI: her satır (yol, TR, EN) üçlüsüdür; yol LAMBDA'dır
/// (yanlış/silinmiş anahtar derlenmez — UX-S31). Biçim yer tutucuları ({0}, {1}) PRO
/// metniyle birebir aynıdır (test).
/// </para>
/// </summary>
public static class KesifP8BasitSozlugu
{
    /// <summary>Karşılık sayısı (test/denetim için).</summary>
    public static int AnahtarSayisi => Liste.Count;

    /// <summary>PRO anahtar yolu → basit değer (dil seçili).</summary>
    internal static IEnumerable<(Expression<Func<Sozluk, string>> Yol, string Deger)> Esleme(string dil) =>
        Liste.Select(x => (x.Yol, dil == "en" ? x.En : x.Tr));

    /// <summary>Test için: yol metni ("Kasa.PasifeAl") → (TR, EN).</summary>
    public static IReadOnlyList<(string Yol, string Tr, string En)> Satirlar =>
        Liste.Select(x => (YolMetni(x.Yol), x.Tr, x.En)).ToList();

    /// <summary>Test için: aynı katmana yazan DİĞER listelerin (KAT-1 D10, CUMA-J) yolları — çift yol denetimi.</summary>
    public static IReadOnlyList<string> DigerKatmanYollari =>
        BasitDilSozlugu.Esleme(BasitDilSozlugu.Tr).Select(x => YolMetni(x.Yol))
            .Concat(CumaJSozlugu.Esleme(CumaJSozlugu.Sade("tr")).Select(x => YolMetni(x.Yol)))
            .ToList();

    private static string YolMetni(Expression<Func<Sozluk, string>> yol)
    {
        var parca = new List<string>();
        var ifade = yol.Body;
        while (ifade is MemberExpression me) { parca.Insert(0, me.Member.Name); ifade = me.Expression!; }
        return string.Join('.', parca);
    }

    internal static readonly List<(Expression<Func<Sozluk, string>> Yol, string Tr, string En)> Liste =
    [
        // ── "Pasif / Pasife al" → "Kullanım dışı / Kullanımdan kaldır" (#74 #83 + K8) ──
        (s => s.Ortak.Pasif, "Kullanım dışı", "Retired"),
        (s => s.Auth.MfaPasif, "Kapalı", "Off"),
        (s => s.Cari.PasifeAl, "Kullanımdan kaldır", "Retire"),
        (s => s.Urun.PasifeAl, "Kullanımdan kaldır", "Retire"),
        (s => s.Urun.PasifeAlOnay, "Ürün kullanımdan kaldırılacak. Geçmiş hareketleri korunur, yeni satışta seçilemez. Onaylıyor musunuz?", "The product will be retired. Its history is kept; it cannot be picked in new sales. Confirm?"),
        (s => s.Kasa.PasifeAl, "Kullanımdan kaldır", "Retire"),
        (s => s.Kasa.PasifeAlOnay, "Kasa kullanımdan kaldırılacak. Geçmiş hareketleri korunur, yeni işlemde seçilemez. Onaylıyor musunuz?", "The till will be retired. Its history is kept; it cannot be picked in new deals. Confirm?"),
        (s => s.Kasa.BakiyeliKasaUyarisi, "Bakiyesi sıfır olmayan kasa kullanımdan kaldırılamaz.", "A till with a non-zero balance cannot be retired."),
        (s => s.Kasa.SayfaAciklama, "Kasalar ve bakiyeleri; kasa bilgileri düzenlenebilir, kaldırılan kasa geri açılabilir.", "Tills and their balances; details can be edited and a retired till can be reopened."),
        (s => s.Kasa.PasifKasaUyari, "Bu kasa kullanım dışı: yeni hareket girilemez. Geçmişi okunabilir; yeniden kullanmak için önce geri açın.", "This till is retired: no new entries. Its history can be read; reopen it first to use it again."),
        (s => s.Pos.PasifeAlOnay, "Bu POS cihazını kullanımdan kaldırmak istiyor musunuz?", "Retire this POS device?"),
        (s => s.Pos.AcikAlacakUyarisi, "Açık POS alacağı olan cihaz kullanımdan kaldırılamaz", "A device with open POS receivables cannot be retired"),
        (s => s.Pos.KasaKilitYolu, "Düzeltme yolu: cihazı kullanımdan kaldırın ve doğru kasaya bağlı yeni bir cihaz kurun. Henüz tahsilatı olmayan cihazda bu alan doğrudan düzenlenebilir.", "To fix it: retire the device and set up a new one linked to the right till. On a device with no payments yet this field can be edited directly."),
        (s => s.Banka.SayfaAciklama, "Banka hesapları ve dekontlar; hesap bilgileri düzenlenebilir, kaldırılan hesap geri açılabilir.", "Bank accounts and slips; details can be edited and a retired account can be reopened."),
        (s => s.Banka.PasifHesapUyari, "Bu hesap kullanım dışı: yeni dekont girilemez. Geçmişi okunabilir; yeniden kullanmak için önce geri açın.", "This account is retired: no new slips. Its history can be read; reopen it first to use it again."),
        (s => s.Gider.KategoriPasifNotu, "Kullanımdan kaldırılan kategoriye yeni gider yazılamaz; geçmiş giderlerin kategorisi yerinde kalır.", "No new expense can go to a retired category; past expenses keep their category."),
        (s => s.Gider.KategoriSilinmez, "Gider kategorisi silinmez, kullanımdan kaldırılır; geçmiş gider satırlarının sınıfı kaybolmasın diye.", "An expense category is never deleted, only retired, so past expense rows keep their class."),
        (s => s.Kabuk.PasifleriGoster, "Kullanım dışı olanları göster", "Show retired"),
        (s => s.Kabuk.PasifeAlindi, "Kayıt kullanımdan kaldırıldı.", "Record retired."),
        (s => s.Kabuk.PasifeAlOnay, "Bu kayıt kullanımdan kaldırılacak. Geçmiş kayıtlar korunur, yalnız yeni işlemlerde seçilemez. Onaylıyor musunuz?", "This record will be retired. Past records are kept; it just cannot be picked in new deals. Confirm?"),
        (s => s.Kullanici.PasifeAlOnay, "Kullanıcı kullanımdan kaldırılacak: bir daha giriş yapamaz. Geçmiş işlemleri ve kim ne yaptı kaydı korunur. Onaylıyor musunuz?", "The user will be retired and cannot sign in again. Past deals and the who-did-what log are kept. Confirm?"),
        (s => s.Kullanici.TekSahipUyari, "Bu işletmenin tek sahibi kullanımdan kaldırılamaz. Önce sahipliği devredin.", "The only owner of this business cannot be retired. Hand over ownership first."),
        (s => s.Kullanici.PasifEt, "Kullanımdan kaldır", "Retire"),
        (s => s.Kullanici.Pasif, "Kullanım dışı", "Retired"),
        (s => s.Kullanici.SilOnay, "Bu kullanıcı kullanımdan kaldırılsın mı?", "Retire this user?"),
        (s => s.Kullanici.KendiniSilemez, "Kendi hesabınızı kullanımdan kaldıramazsınız", "You cannot retire your own account"),
        (s => s.Fiyat.SayfaAciklama, "Fiyat listeleri ve kampanyalar; her satır düzenlenebilir, kullanımdan kaldırılabilir ve geri açılabilir.", "Price lists and campaigns; every row can be edited, retired and reopened."),
        (s => s.Fiyat.ListePasifeAlindi, "Fiyat listesi kullanımdan kaldırıldı.", "Price list retired."),
        (s => s.Fiyat.KampanyaPasifeAlindi, "Kampanya kullanımdan kaldırıldı.", "Campaign retired."),
        (s => s.Fiyat.GecersizKilmaKaldirildi, "Ürüne özel fiyat kaldırıldı — fiyat listeye döndü.", "Product-specific price removed — the price follows the list again."),
        (s => s.IceAktarim.GeriAlOnay, "Bu oturumda oluşan kayıtlar kullanımdan kaldırılacak (silinmeyecek). Devam edilsin mi?", "Records created in this session will be retired (not deleted). Continue?"),
        (s => s.UrunAgaci.ReyonPasifOnay, "Bu reyon kullanımdan kaldırılacak. Kayıt silinmez; üzerinde ürün varsa işlem reddedilir ve istediğinizde geri açabilirsiniz.", "This section will be retired. Nothing is deleted; if it still holds products the action is refused, and you can reopen it any time."),
        (s => s.UrunAgaci.KategoriPasifOnay, "Kategori silinmez, kullanımdan kaldırılır: ürün kartlarındaki kırılım korunur ve kategori ağaçta görünmez olur. Geri dönüş yolu aynı satırdaki “Aktife al” düğmesidir.", "The category is not deleted, only retired: product cards keep it and it disappears from the tree. To undo, use the “Activate” button on the same row."),
        (s => s.UrunAgaci.KategoriPasifRozet, "Kullanım dışı", "Retired"),
        (s => s.Isletme.PasifeAl, "Kullanımdan kaldır", "Retire"),
        (s => s.Isletme.PasifeAlOnay, "Şube kullanımdan kaldırılacak. Geçmiş hareketleri korunur, yeni işlemde seçilemez. Onaylıyor musunuz?", "The branch will be retired. Its history is kept; it cannot be picked in new deals. Confirm?"),
        (s => s.Isletme.SonSubeUyarisi, "Tek açık şube kullanımdan kaldırılamaz — işlem yapılacak şube kalmaz.", "The only open branch cannot be retired — no branch would be left to work in."),
        (s => s.Belge.PasifeAlindi, "Belge kullanımdan kaldırıldı (silinmedi, kayıt korunur).", "Document retired (not deleted; the record is kept)."),
        (s => s.Duzeltme.BelgeGerekce, "Belgenin içeriği değiştirilmez; yanlış belge kullanımdan kaldırılır ve doğrusu yüklenir.", "A document's content is never changed; the wrong one is retired and the right one uploaded."),

        // ── "cari" → "müşteri" (#18 #76 #81 + D10; karar 1: ekranda "Müşteri") ──
        (s => s.Cari.DuzeltmeIpucu, "Bakiyeyi elle düzeltir; deftere müşteri düzeltme fişi olarak yazılır ve geri alınamaz. Gerekçe zorunlu (en az 20 karakter). Artı tutar borcu artırır.", "Adjusts the balance by hand; it is posted as a customer correction slip and cannot be undone. A reason is required (at least 20 characters). A positive amount increases the debt."),
        (s => s.Cari.TlKarsiligiBilgiAmacli, "TL karşılığı (bilgi amaçlı — sistem hesaplar)", "TL equivalent (for information — the system calculates it)"),
        (s => s.Cari.DovizFisBacagiUyari, "Fişteki döviz bacağı bir ödemedir, müşteriye döviz borcu bırakmaz. Döviz borcu yalnız bu formdan doğar.", "An FX leg on a receipt is a payment; it leaves no FX debt on the customer. FX debt arises only from this form."),
        (s => s.Cari.NotlarIpucu, "Bu müşteriyle ilgili serbest not (en fazla 1000 karakter). Boş bırakılırsa mevcut not silinir.", "A free note about this customer (at most 1000 characters). Leaving it empty deletes the current note."),
        (s => s.Cari.UnvanIpucu, "Faturaya yazılan resmî ad; boşsa müşteri adı kullanılır.", "Legal name printed on invoices; the customer name is used when empty."),
        (s => s.Urun.TedarikciIpucu, "Malı bize satan kişi ya da firma. Konsinye sahibinden farklıdır.", "The person or firm that sold us the item. Different from the consignment owner."),
        (s => s.Hurda.Ramatci, "Ramatçı / rafineri", "Refiner"),
        (s => s.Hurda.DuzeltmeNotu, "Defter satırının üzerine yazılmaz: aynı hurda satışına ikinci bir fark bacağı eklenir, toplam fark bacakların toplamıdır. Tahsilat tutarı bu yolla değişmez; para tarafı için müşteri düzeltme fişini kullanın.", "A ledger row is never overwritten: a second difference leg is added to the same scrap sale and the total difference is the sum of the legs. The payment amount does not change this way; use the customer correction slip for the money side."),
        (s => s.Panel.YuksekRiskliCari, "müşteri yüksek riskli görünüyor (uyarıdır, engel değil)", "customers look high-risk (a warning, not a block)"),
        (s => s.Islem.SatisDuzeltmeYolu, "Müşteri malı geri getirdiyse \"Satış İadesi\" sekmesinden iade girin; yalnız açıklama/ödeme bilgisi yanlışsa düzeltme fişiyle müşteri bakiyesi düzeltilir.", "If the customer brought the goods back, enter a return on the \"Sales Return\" tab; if only the description or payment is wrong, fix the customer balance with a correction slip."),
        (s => s.Islem.IskontoCariden, "müşteri kartındaki orandan", "from the customer card rate"),
        (s => s.Rapor.YasHasNotu, "Has bakiyesi TL'ye çevrilmez, ayrı okunur: artı = müşteri bize has borçlu, eksi = biz müşteriye has borçluyuz.", "The fine-gold balance is not converted to TL: plus = the customer owes us fine gold, minus = we owe the customer."),
        (s => s.Rapor.YasListelenenToplam, "Listelenen müşterilerin toplamı", "Total of the listed customers"),
        (s => s.Rapor.YasSinirUyari, "Yalnız ilk {0} müşteri listelendi, toplam {1} müşteri var; alt toplam yalnız listelenenleri kapsar.", "Only the first {0} customers are listed out of {1}; the subtotal covers only those listed."),
        (s => s.Banka.CariSecInfo, "Fatura kesilecek müşteriyi seçin", "Pick the customer to invoice"),
        (s => s.Banka.DekontCari, "Müşteri", "Customer"),
        (s => s.Banka.EkstreAciklama, "Yüklenen ekstre satırlarını gözden geçirin, müşteriye bağlayın ve seçtiklerinizi banka hareketine çevirin.", "Review the uploaded statement rows, link them to customers and turn the selected ones into bank entries."),
        (s => s.Banka.IbanOgrenildi, "IBAN bu müşteriye öğretildi; sonraki ekstrelerde otomatik eşleşecek.", "The IBAN is now linked to this customer; future statements will match automatically."),
        (s => s.Banka.CarisizOnayla, "Müşterisiz onayla", "Confirm without customer"),
        (s => s.Banka.CarisizOnaylaIpucu, "İşaretlenmezse müşteri eşleşmesi olmayan satırlar onaylanmaz; beklemede kalır ve sonra müşteri seçilebilir.", "If unticked, rows without a customer are not confirmed; they wait and a customer can be picked later."),
        (s => s.Banka.CarisizUretilen, "müşterisiz hareket: {0}", "entries without customer: {0}"),
        (s => s.Banka.CarisizAtlanan, "müşteri yok diye beklemede kalan: {0}", "waiting for a customer: {0}"),
        (s => s.Fatura.FaturaDegismez, "Fatura bir belgedir: kesilen numara vergi dairesine karşı seriyi tüketir, bu yüzden başlığı, müşteri bilgisi, KDV'si ve kalemleri kaydedildikten sonra yerinde değiştirilmez.", "An invoice is a document: its number uses up the series filed with the tax office, so its header, customer details, VAT and lines cannot be changed in place once saved."),
        (s => s.Uyum.PepSayisi, "PEP beyanı veren müşteri", "Customers with a PEP declaration"),
        (s => s.Uyum.Profilsiz, "Profili hiç hesaplanmamış müşteri", "Customers never profiled"),
        (s => s.Uyum.YuksekRiskliler, "Yüksek riskli müşteriler", "High-risk customers"),
        (s => s.Uyum.YuksekRiskliYok, "Yüksek riskli müşteri yok.", "No high-risk customers."),
        (s => s.Uyum.ProfilTarihceYok, "Bu müşteri için henüz profil hesaplanmadı.", "No profile has been calculated for this customer yet."),
        (s => s.Uyum.RiskListeSiniri, "Liste en çok {0} müşteri gösterir; daraltmak için müşteri seçin.", "The list shows at most {0} customers; pick a customer to narrow it."),
        (s => s.Cikti.Aciklama, "Satış fişi, tahsilat makbuzu, emanet teslim belgesi ve müşteri hesap dökümü tek A4 şablonundan basılır.", "Sales slip, payment receipt, safekeeping delivery note and customer account summary all print from one A4 template."),
        (s => s.Cikti.KaynaktaDuzelt, "Kaydı düzeltmek için kaynağına gidin (işlem defteri / müşteri kartı) ve oradaki düzeltme yolunu kullanın.", "To fix the record, go to its source (deal log / customer card) and use the fix path there."),
        (s => s.Kurulum.TurCari, "Müşteri", "Customer"),
        (s => s.Kurulum.NegatifAcilisYok, "Kasa ve banka açılış bakiyesi eksi olamaz; eksi bakiye borçtur, müşteri ya da tedarikçi hesabına girilir.", "Till and bank opening balances cannot be negative; a negative balance is a debt and goes on a customer or supplier account."),
        (s => s.Kurulum.MuhurOnay, "Bu işlem geri alınamaz. Mühürden sonra açılış bakiyesi bir daha girilemez, değiştirilemez ve geri alınamaz; tek düzeltme yolu ters kayıt ya da müşteri düzeltme fişi olur.", "This cannot be undone. After sealing, opening balances can never be entered, changed or undone again; the only fix is a reversing entry or a customer correction slip."),
        (s => s.Kurulum.AcilisDegismez, "Açılış kaydı deftere işlenmiştir; yerinde düzeltilemez (VUK md.219). Mühürlenmeden önce bu oturumu geri alabilir, mühürden sonra müşteri düzeltme fişi kesebilirsiniz.", "The opening entry is in the ledger and cannot be edited in place (VUK art.219). Before sealing you can undo this session; after sealing you can issue a customer correction slip."),
        (s => s.Kurulum.NegatifAcilisGerekce, "Kasa ve banka açılışı neden eksi olamaz: kasada eksi para, banka mevduatında eksi bakiye fiziken bulunmaz. Eksi bakiye bir BORÇTUR ve borcun yeri müşteri ya da tedarikçi hesabıdır. Kredili mevduat (KMH) ayrı bir üründür; kararı beklemede olduğu için bu kural şimdilik tutucudur ve gevşetilmemiştir.", "Why till and bank openings cannot be negative: a till cannot physically hold negative cash, nor a deposit a negative balance. A negative balance is a DEBT and belongs on a customer or supplier account. An overdraft (KMH) is a separate product; its decision is pending, so this rule stays strict for now."),
        (s => s.Emanet.AramaDaraltin, "Liste sınırına (50 kayıt) değdi — aradığınız müşteri görünmüyorsa aramayı daraltın.", "The list hit its limit (50 records) — if the customer you want is missing, narrow the search."),
        (s => s.Emanet.AciklamaKilitliAlanlar, "Gram, ayar, maden ve müşteri kilitlidir (emanet bakiyesini oluştururlar) — düzeltmek için kaydı geri alın.", "Grams, carat, metal and customer are locked (they make up the safekeeping balance) — undo the record to fix them."),
        (s => s.SayfaDuzeltme.RaporAciklamasi, "Aylık özet, gün sonu karşılaştırması, müşteri borç yaşı ve stok değeri.", "Monthly summary, end-of-day comparison, customer debt age and stock value."),
        (s => s.Siparis.SinirUyarisi, "En çok {0} sipariş listelenir; daha fazlası var. Tür, durum ya da müşteri süzgeciyle daraltın.", "At most {0} orders are listed; there are more. Narrow by type, status or customer."),
        (s => s.Vezne.CariGerekli, "Bu fiş türü müşteriye yazılır: müşteri seçimi zorunlu", "This ticket type is posted to a customer: picking a customer is required"),
        (s => s.Vezne.VezneleDegismez, "Kasa kuyruğuna gönderilmiş fişin tutarı damgalanmıştır: kasada o tutar bekletildiği için kalemleri, müşteri ve açıklaması artık yerinde değiştirilmez.", "A ticket sent to the till queue has its amount stamped: since the till is holding that amount, its lines, customer and note can no longer be changed in place."),
        (s => s.Yonetim.KisiselVeriVarsayilanNotu, "Seçili kapsamlar KİŞİSEL VERİ içeriyor (müşteriler, gider pusulaları). Açılışta hepsi seçili gelir; gerekmiyorsa işareti kaldırın — dışa aktarılan her satır kim ne yaptı kaydına yazılır.", "The selected scopes contain PERSONAL DATA (customers, expense vouchers). All are ticked at first; untick what you do not need — every exported row is written to the who-did-what log."),

        // ── "storno" → "geri al" (#25 + K8) ──
        (s => s.Gider.Stornolandi, "Geri alındı", "Undone"),
        (s => s.Gider.StornoYapildi, "Gider geri alındı", "Expense undone"),
        (s => s.Gider.GiderDegismezNotu, "Gider satırının tutarı yerinde değişmez; düzeltmek için geri alıp doğrusunu yeni kayıt olarak girin.", "An expense amount is never edited in place; to fix it, undo it and enter the right one as a new record."),
        (s => s.Gider.VirmanStornoYapildi, "Virman geri alındı (iki bacak birlikte)", "Transfer undone (both legs together)"),
        (s => s.Gider.IkiBacakNotu, "Virmanın tek bacağı geri alınamaz; iki bacak birlikte döner.", "One leg of a transfer cannot be undone alone; both legs go back together."),
        (s => s.Hurda.DegismezNotu, "Hurda satışı yerinde değişmez ve bu sürümde geri alınamaz: hurdanın stok satırı yoktur, ters kayıt bu satışı tanımaz. Yanlış çıkış, ramatçıyla yapılan karşılaştırma kaydıyla düzeltilir; düzeltme yolu açılana kadar kayıt olduğu gibi durur.", "A scrap sale is not edited in place and cannot be undone in this version: scrap has no stock row and the reversal does not know this sale. A wrong sale is fixed with a comparison record with the refiner; until a fix path exists the record stays as it is."),
        (s => s.Kasa.HareketStornoYok, "Bu satır bir işlemin kasa bacağıdır; tek başına geri alınamaz.", "This row is the till leg of a deal; it cannot be undone on its own."),
        (s => s.Kasa.HareketStornoYolu, "Düzeltmek için işlemin kendisini geri alın: fişi doğduğu ekrandan (satış · tahsilat · emanet) geri alın; kasa bacağı da birlikte geri alınır.", "To fix it, undo the deal itself on the screen it came from (sale · payment · safekeeping); the till leg is undone with it."),
        (s => s.Banka.DekontDegismezYolu, "Tutar ya da yön yanlışsa dekontu geri alın (ters kayıt yazılır) ve doğrusunu yeni bir dekont olarak girin; açıklama, karşı taraf ve tarih doğrudan düzeltilebilir.", "If the amount or direction is wrong, undo the slip (a reversing entry is written) and enter the right one as a new slip; note, counterparty and date can be fixed directly."),
        (s => s.Banka.DekontStornolandi, "Geri alındı", "Undone"),
        (s => s.Banka.DekontStornoYok, "Bu dekont geri alınamaz: ya zaten geri alınmış ya da ters kayıt satırıdır, ya faturaya dönüştürülmüştür, ya da banka ekstresinden gelmiştir.", "This slip cannot be undone: it is already undone or is a reversing row, it was turned into an invoice, or it came from a bank statement."),
        (s => s.Banka.DekontKilitliAlanlar, "Tutar, yön ve para birimi kilitlidir — düzeltmek için dekontu geri alın.", "Amount, direction and currency are locked — undo the slip to fix them."),
        (s => s.Banka.OnaylananSatirYolu, "Yanlışsa düzeltme yolu, üretilen banka hareketinin düzeltme fişidir.", "If it is wrong, fix it with a correction slip on the bank entry created."),
        (s => s.Kabuk.StornoluKayit, "Geri alındı", "Undone"),
        (s => s.Kabuk.StornoKurYok, "Güncel kur kaydı olmadan geri alma yazılamaz: ters kayıt bugünün kuruna bağlanır.", "An undo cannot be written without a current rate: the reversing entry is tied to today's rate."),
        (s => s.Kabuk.VirmanBacagiGerekce, "Bu satır bir virmanın bacağıdır: tek başına geri alınamaz, yoksa paranın yalnız yarısı geri döner ve karşı taraf şişmiş kalır.", "This row is one leg of a transfer: it cannot be undone alone, or only half the money would come back and the other side would stay inflated."),
        (s => s.Kabuk.VirmanStornosunaGit, "Virmanı geri almaya git", "Go to undo the transfer"),
        (s => s.Denetim.StornoTurDesteklenmiyor, "Bu işlem türü geri alınamaz; ekranda yazılı düzeltme yolunu kullanın.", "This deal type cannot be undone; use the fix path written on screen."),
        (s => s.Emanet.HareketDegismez, "Bu satır geri alınamaz: ya zaten geri alınmıştır, ya kendisi bir geri alma kaydıdır, ya da çok bacaklı bir fişin parçasıdır.", "This row cannot be undone: it is already undone, it is itself an undo record, or it is part of a multi-leg ticket."),
        (s => s.Emanet.HareketDegismezYolu, "Çok bacaklı fişte fişin tamamı için düzeltme fişi kullanılır; zaten geri alınmış satır ikinci kez geri alınmaz.", "For a multi-leg ticket use a correction slip for the whole ticket; a row already undone is not undone twice."),
        (s => s.Duzeltme.StornoHenuzYok, "Bu işlem türü stoğu da geri çevirmeyi gerektirdiği için tek tuşla geri alınamaz. Doğru yol: satış/alış için iade işlemi girilir, diğer türlerde ters yönde yeni bir kayıt açılır ve gerekçesi açıklamaya yazılır.", "This deal type cannot be undone with one click because the stock would have to be turned back too. The right way: enter a return for sales/purchases; for other types open a new record in the opposite direction and write the reason in the note."),
        (s => s.Duzeltme.StornoYokGerekce, "Bu işlem türü stoğu da geri çevirmeyi gerektirdiği için tek tuşla geri alınamaz.", "This deal type cannot be undone with one click because the stock would have to be turned back too."),

        // ── "append-only" → "yalnız eklenir" (#87 + #33 döküm) ──
        (s => s.Uyum.ProfilDegismezNot, "Risk profili yalnız eklenen bir kayıttır, değiştirilmez: denetimde 'o gün hangi skorla işlem yaptın' sorusu sorulur. Düzeltmek için yeniden hesaplayın ya da gerekçe yazarak elle değiştirin — ikisi de YENİ satır yazar, eskisi durur.", "The risk profile only grows and is never changed: an auditor asks which score was in force on a given day. To correct it, recalculate or override with a reason — both write a NEW row and the old one stays."),
        (s => s.Siparis.AppendOnly, "Bu döküm yalnız eklenir: satırlar sonradan değiştirilmez, silinmez.", "This log only grows: rows are never edited or deleted."),

        // ── "sunucu" → "liste sınırı / sistem" (#17 #35 metin + TOPLU KATMAN-1b /vitrin) ──
        (s => s.Urun.KmtsKoduIpucu, "Kıymetli Madenler Takip Sistemi kodu. İsteğe bağlı; sistem doğrulamaz, olduğu gibi saklanır. Boş bırakılırsa kayıttan silinir.", "Precious Metals Tracking System code. Optional; the system does not check it and stores it as is. Leaving it empty removes it."),
        (s => s.Islem.IskontoIpucu, "Yüzde girilir; TL karşılığını sistem hesaplar.", "Enter a percentage; the system calculates the TL amount."),
        (s => s.Pos.SinirUyarisi, "Liste sınırına ulaşıldı — daha fazlası var, tarih veya cihaz süzgecini daraltın.", "List limit reached — there is more; narrow the date or device filter."),
        // TOPLU-4: (s => s.RaporSeti.SinirUyarisi) karşılığı kaldırıldı — PRO metni artık sade ("Sınıra ulaşıldı — …"); BASİT aynı metni okur.
        (s => s.Banka.MasakYukleniyorSebep, "MASAK gereksinimi alınıyor — birkaç saniye içinde alanlar açılacak.", "Loading the MASAK requirement — the fields open in a few seconds."),
        (s => s.Banka.SinirUyari, "Liste sayfa sayfa gelir; sınıra değdiyseniz süzgeçle daraltın.", "The list comes in pages; if you hit the limit, narrow with filters."),
        (s => s.Fatura.FisOnizlemeIpucu, "Fişin TÜM kalemleri faturaya girer; kalem seçilemez. Has bedelini sistem hesaplar.", "ALL lines of the ticket go on the invoice; lines cannot be picked. The system calculates the fine-gold value."),
        (s => s.Fatura.FaturalanabilirSinir, "Liste sınırına ulaşıldı — daha fazlası var; tarih aralığını daraltın.", "List limit reached — there is more; narrow the date range."),
        (s => s.Kabuk.SunucuOnayiBekleniyor, "Kayıt onayı bekleniyor — kaydedildi yazılmadan önce yanıt beklenir.", "Waiting for confirmation — \"saved\" shows only after the reply."),
        (s => s.Gorunum.Yuklenmedi, "Görünüm ayarı okunamadı; seçim kaydedilemez.", "The display setting could not be read; your choice cannot be saved."),
        (s => s.Kur.IptalOnay, "Bu kur kaydı GEÇERSİZ kılınacak: satır silinmez, geçmişte 'geçersiz' rozetiyle kalır ve güncel kur seçiminden çıkarılır. Kayda bağlı işlem varsa iptal reddedilir.", "This rate record will be VOIDED: the row is not deleted, it stays in history with a 'void' badge and leaves the current-rate choice. If any deal is tied to it, the void is refused."),
        (s => s.Pano.TavanNotu, "Panoda en fazla {0} satır gösterilir.", "The board shows at most {0} rows."),
        (s => s.Pano.AyarAciklama, "Marj ticari sırdır ve panoya inmez; fiyat sistemde hesaplanır, panoya yalnız sonuç gider.", "The margin is a trade secret and never reaches the board; the price is calculated in the system and only the result goes to the board."),
        (s => s.Fiyat.PanelIpucu, "Fiyatlar sistemde hesaplanır; sayfa başına en fazla 50 satır gelir. Tekil ve toplu geçersiz kılma bu panelden yapılır.", "Prices are calculated in the system; at most 50 rows per page. Single and bulk overrides are made from this panel."),
        (s => s.Kurulum.AcilisAciklama, "Önce KURU PROVA çalıştırın: sistem satırları kontrol eder ve hiçbir kayıt oluşturmaz. Sonuç temizse uygulayabilirsiniz.", "Run a DRY RUN first: the system checks the rows and creates nothing. If the result is clean you can apply it."),
        (s => s.Yerlesim.Sinirda, "Liste sınırına ulaşıldı — daha fazlası var; süzgeçle daraltın.", "List limit reached — there is more; narrow with filters."),
        (s => s.Duzeltme.ListeSiniri, "Bu listede en çok {0} kayıt gelir; hepsini görmek için süzgeçle daraltın.", "This list returns at most {0} records; narrow with filters to see them all."),
        (s => s.Bildirim.SinirUyarisi, "Bu liste en fazla {0} kayıtla gelir — daha fazlası var. Daraltmak için durum sekmesini değiştirin.", "This list returns at most {0} records — there are more. Switch the status tab to narrow it."),
        (s => s.Evrak.SinirUyarisi, "Bir seferde en çok {0} evrak listelenir; daha fazlası var, süzgeçle daraltın.", "At most {0} papers are listed at a time; there are more, narrow with filters."),
        (s => s.Evrak.GenisletmeSiniri, "Portföyün vade aralığı liste sınırından ({0} gün) uzun; tarihleri elle daraltın.", "The portfolio's due-date range is longer than the list limit ({0} days); narrow the dates by hand."),
        (s => s.Vezne.SinirUyarisi, "Bu liste en çok {0} satırla gelir; daha fazlası var. Durum süzgeciyle daraltın.", "This list returns at most {0} rows; there are more. Narrow with the status filter."),
        (s => s.Yonetim.YetkilerAciklama, "Hangi rolün hangi ekranı görebileceğini belirler. Son sözü sistem söyler; buradaki tablo kapının kendisidir.", "Sets which role can see which screen. The system has the last word; this table is the gate itself."),
        (s => s.Yedek.AnahtarUyari, "Yedek şifreleme anahtarı kaybolursa tüm yedekler açılamaz. Anahtarın ayrı ve güvenli bir yerde kopyası bulunmalıdır.", "If the backup encryption key is lost, no backup can be opened. Keep a copy of the key in a separate, safe place."),
        (s => s.Yedek.ElleYedekAciklama, "Bu düğme yedeği hemen almaz; sıraya bir talep yazar ve zamanlayıcı en geç 60 saniye içinde yedeği alır.", "This button does not take the backup at once; it queues a request and the scheduler takes the backup within 60 seconds."),
        (s => s.Yedek.IndirmeYok, "Yedek dosyası uygulamadan indirilemez: dosya tüm işletmelerin verisini taşır. Geri yükleme yalnız teknik ekip tarafından, yazılı yönergeyle yapılır.", "A backup file cannot be downloaded from the app: it carries every business's data. Restores are done only by the technical team, following the written procedure."),

        // ── TOPLU birleştirmeleri: Veri içe aktar (ZO-BELGE) + BASİT MASAK metni (SATIS-3) ──
        (s => s.IceAktarim.DosyaSec, "Excel'den kaydedilmiş dosya (.csv)", "File saved from Excel (.csv)"),
        (s => s.IceAktarim.PolitikaHata, "Aynı kayıt varsa: dur ve göster (önerilen)", "If the record exists: stop and show (recommended)"),
        (s => s.Islem.MasakBaslik, "Müşteri kimliği (MASAK)", "Customer ID (MASAK)"),
        (s => s.Islem.MasakAciklama, "Yasal kimlik sınırı aşıldığı için müşteri kimliği zorunludur. Bilgiler şifreli saklanır, 8 yıl korunur.", "The legal ID threshold is exceeded, so the customer's ID is required. Data is stored encrypted and kept for 8 years."),
        (s => s.Islem.MasakKademeAmac, "Kimlik ve işlemin amacı yazılmalı", "ID and the purpose of the deal are required"),
    ];
}
