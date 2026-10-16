namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-DIL-BORC-KASA (denetim 10.10 · Y7, Y8, Y18, O21) — borç/alacak TEK KALIP.
// Denetim: aynı tedarikçi Tezgâh'ta "alacağı 3.840 ₺", Müşteriler'de "biz borçluyuz",
// Bugün'de "borcun"; Müşteriler başlığındaki "alacak" Tezgâh'takinin TERSİ anlamdaydı.
// Ekip oyu: her ekran DÜKKÂN BAKIŞINDAN iki sabit kalıp kullanır —
//   "Müşteri bize borçlu: X ₺"   (bakiye ARTI)
//   "Biz müşteriye borçluyuz: X ₺" (bakiye EKSİ)
// Bütün ekranlar (Bugün, Müşteriler, Tezgâh üst satırı, müşteri kartı, Raporlar) bu sınıftan
// okur; "alacağı / alacaklı / alacağın / borcun / N alacak" gibi bakışı belirsiz sözler
// PDilBorcKasaTestleri.MetinTaramasi ile kaynakta 0 tutulur. YALNIZ GÖSTERİM: hiçbir
// hesap/işaret değişmez (artı = müşteri bize borçlu, cari_bakiyeler sözleşmesi).
// ═════════════════════════════════════════════════════════════════════════════
public static class BorcDiliSozlugu
{
    private static bool En(string dil) => dil == "en";

    /// <summary>Kalıp (string.Format, {0} = biçimli tutar): bakiye ARTI.</summary>
    public static string BizeBorcluKalip(string dil) => En(dil) ? "Customer owes us: {0}" : "Müşteri bize borçlu: {0}";
    /// <summary>Kalıp (string.Format, {0} = biçimli tutar): bakiye EKSİ.</summary>
    public static string BizBorcluyuzKalip(string dil) => En(dil) ? "We owe the customer: {0}" : "Biz müşteriye borçluyuz: {0}";

    /// <summary>Kalıbın tutarsız başı (rozet/etiket: "Müşteri bize borçlu").</summary>
    public static string BizeBorcluEtiket(string dil) => En(dil) ? "Customer owes us" : "Müşteri bize borçlu";
    public static string BizBorcluyuzEtiket(string dil) => En(dil) ? "We owe the customer" : "Biz müşteriye borçluyuz";

    public static string BizeBorclu(string tutar, string dil) => string.Format(BizeBorcluKalip(dil), tutar);
    public static string BizBorcluyuz(string tutar, string dil) => string.Format(BizBorcluyuzKalip(dil), tutar);

    /// <summary>Bakiye sıfır.</summary>
    public static string BorcYok(string dil) => En(dil) ? "No balance" : "Borç yok";

    /// <summary>
    /// İşaretten kalıp seçer: artı → bize borçlu, eksi → biz borçluyuz, sıfır → borç yok.
    /// <paramref name="tutar"/> MUTLAK değerin biçimli hâlidir (işaret kalıpta söylenir).
    /// </summary>
    public static string Durum(decimal bakiye, string tutar, string dil) =>
        Yon(bakiye, 6) switch { > 0 => BizeBorclu(tutar, dil), < 0 => BizBorcluyuz(tutar, dil), _ => BorcYok(dil) };

    /// <summary>
    /// TEK İŞARET KURALI (doğrulayıcı tur 1, DÜŞÜK): yön GÖSTERİM HASSASİYETİNE yuvarlanmış değerden
    /// okunur (TL 2 hane, has 3–4 hane) — ekranda "0,000 g has" yazıp "bize borçlu" demek çelişkidir.
    /// +1 = müşteri bize borçlu · −1 = biz müşteriye borçluyuz · 0 = borç yok. Bütün çağrı yerleri
    /// (Tezgâh üst satırı, Platin Tezgâh rozeti, PRO yan sütun, müşteri kartı) bunu çağırır.
    /// </summary>
    public static int Yon(decimal bakiye, int hane) =>
        Math.Sign(Math.Round(bakiye, hane, MidpointRounding.AwayFromZero));

    /// <summary>
    /// Kalıbın tamamı: bakiye <paramref name="hane"/> haneye yuvarlanır, yön ondan seçilir,
    /// MUTLAK değer <paramref name="bicim"/> ile yazılır.
    /// </summary>
    public static string Durum(decimal bakiye, int hane, Func<decimal, string> bicim, string dil)
    {
        var y = Math.Round(bakiye, hane, MidpointRounding.AwayFromZero);
        return y > 0m ? BizeBorclu(bicim(y), dil) : y < 0m ? BizBorcluyuz(bicim(-y), dil) : BorcYok(dil);
    }

    /// <summary>Tutar ayrı sütundaki liste satırı (Yıldız tuval küçük harf): "bize borçlu".</summary>
    public static string KisaBizeBorclu(string dil) => En(dil) ? "owes us" : "bize borçlu";
    /// <summary>Tutar ayrı sütundaki liste satırı: "biz borçluyuz".</summary>
    public static string KisaBizBorcluyuz(string dil) => En(dil) ? "we owe" : "biz borçluyuz";

    /// <summary>Müşteriler üst satırı: "7 müşteri · 3 bize borçlu · 1 biz borçluyuz".</summary>
    public static string Sayilar(int bize, int biz, string dil) => $"{bize} {KisaBizeBorclu(dil)} · {biz} {KisaBizBorcluyuz(dil)}";

    /// <summary>Bugün cümlesindeki toplamlar (bütün müşteriler): aynı kalıbın çoğulu.</summary>
    public static string Toplam(string bize, string biz, string dil) => En(dil)
        ? $"Customers owe us: {bize}. We owe customers: {biz}."
        : $"Müşteriler bize borçlu: {bize}. Biz müşterilere borçluyuz: {biz}.";

    // ── O21 (BASİT mod düz karşılıklar — yalnız müşteri bakiyesi eylemleri) ──
    /// <summary>"Düzeltme Fişi" BASİT karşılığı.</summary>
    public static string BakiyeDuzelt(string dil) => En(dil) ? "Correct the balance" : "Bakiyeyi düzelt";
    /// <summary>"Döviz Veresiye" BASİT karşılığı.</summary>
    public static string DovizleVeresiye(string dil) => En(dil) ? "Sell on credit in currency" : "Dövizle veresiye yaz";
    /// <summary>"Döviz Tahsilat" BASİT karşılığı.</summary>
    public static string DovizleTahsilat(string dil) => En(dil) ? "Collect in currency" : "Dövizle tahsilat al";
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-DIL-BORC-KASA (Y8) — Kasa ekranının büyük rakamı ÇEKMECEDEKİ NAKİT.
// Denetim: "Ana Kasa 312.999,49 ₺" kart (22.846) ve havaleyi (96.408) içeriyordu; çekmecede
// 193.744 vardı. Hesap formülü DEĞİŞMEZ (GET /kasalar/nakit-ayrimi = sunucu KasaNakitAyrimi);
// yalnız hangi toplamın büyük yazıldığı ve etiketi değişir. Ayrım okunamazsa büyük rakam kasa
// toplamıdır ve etiketi bunu SÖYLER ("nakit" diye basılmaz — UX-S28).
// ═════════════════════════════════════════════════════════════════════════════
public static class KasaNakitSozlugu
{
    private static bool En(string dil) => dil == "en";
    public static string CekmecedekiNakit(string dil) => En(dil) ? "Cash in the drawer" : "Çekmecedeki nakit";
    public static string KasaToplami(string dil) => En(dil) ? "Till total (cash, card and transfer)" : "Kasa toplamı (nakit, kart, havale)";
    /// <summary>Kartın alt satırı: "Kart X · Havale Y · Toplam Z".</summary>
    public static string AyrimSatiri(string kart, string havale, string toplam, string dil) => En(dil)
        ? $"Card {kart} · Transfer {havale} · Total {toplam}"
        : $"Kart {kart} · Havale {havale} · Toplam {toplam}";
    /// <summary>Üst toplam kartı (TL kasalar).</summary>
    public static string CekmecedekiNakitTl(string dil) => En(dil) ? "Cash in the drawer (TRY tills)" : "Çekmecedeki nakit (TL kasalar)";
    public static string ToplamAlt(string toplam, string dil) => En(dil) ? $"With card and transfer: {toplam}" : $"Kart ve havale dahil: {toplam}";
    /// <summary>Yıldız özet cümlesi (nakit okunduysa).</summary>
    public static string YildizCumleNakit(string toplam, string dil) => En(dil)
        ? $"Cash in the drawer. Card and transfer are on their own rows; all together {toplam}."
        : $"Çekmecedeki nakit. Kart ve havale ayrı satırda; hepsi birlikte {toplam}.";
    /// <summary>Yıldız özet cümlesi (nakit ayrımı okunamadı — büyük rakam kasa toplamı).</summary>
    public static string YildizCumleToplam(string dil) => En(dil)
        ? "Till total: cash, card and transfer together. The cash split could not be read."
        : "Kasa toplamı: nakit, kart ve havale birlikte. Nakit ayrımı okunamadı.";
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-DIL-BORC-KASA (Y18) — EMANET "BIRAKILDIĞI GİBİ" DİLİ (tek kaynak).
// "22 ayar · 12,50 g" (tuval Y_Emanet "Ne bıraktı" sütunu); has yalnız ikincil.
// Satır: "22 ayar · 12,50 g · taş 0,50 g". Ayar adı altında "N ayar" (EN "NK"),
// diğer madenlerde ayarın kendi adı (ör. "925 Gümüş").
// ═════════════════════════════════════════════════════════════════════════════
public static class EmanetDiliSozlugu
{
    private static bool En(string dil) => dil == "en";
    private static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
    private static readonly System.Globalization.CultureInfo Ing = System.Globalization.CultureInfo.GetCultureInfo("en-GB");

    /// <summary>Gram: en az 2, en çok 3 ondalık, birim "g" ("12,50 g"; sayı ile birim arası bölünmez boşluk).</summary>
    public static string Gram(decimal gram, string dil) =>
        $"{gram.ToString("#,0.00#", En(dil) ? Ing : Tr)}\u00A0g";   // birim sayıdan ayrı satıra düşmez

    /// <summary>Ayarın tezgâh adı: K22 → "22 ayar" / "22K"; diğerleri kendi adı.</summary>
    public static string AyarAdi(string ayarId, string dil)
    {
        var a = Kuyumcu.Core.Domain.Ayarlar.Bul(ayarId);
        if (a is null) return ayarId;
        if (a.Maden == Kuyumcu.Core.Domain.Maden.Altin && ayarId.Length <= 3 && ayarId.StartsWith('K'))
            return En(dil) ? $"{ayarId[1..]}K" : $"{ayarId[1..]}\u00A0ayar";   // "22 ayar" bölünmez
        return a.Ad;
    }

    /// <summary>Tek ayar: "22 ayar · 12,50 g".</summary>
    public static string AyarGram(string ayarId, decimal gram, string dil) => $"{AyarAdi(ayarId, dil)} · {Gram(gram, dil)}";

    /// <summary>Hareket satırı (bırakıldığı gibi): "22 ayar · 12,50 g · taş 0,50 g".</summary>
    public static string Satir(string ayarId, decimal gram, decimal? tas, string dil) =>
        tas is > 0m ? $"{AyarGram(ayarId, gram, dil)} · {(En(dil) ? "stone" : "taş")} {Gram(tas.Value, dil)}" : AyarGram(ayarId, gram, dil);

    /// <summary>Kalan emanet parçaları (her ayar ayrı parça; ekran " · " ile değil satır/blok olarak dizer).</summary>
    public static IReadOnlyList<string> Parcalar(IEnumerable<Kuyumcu.Core.Domain.EmanetAyarSatiri> satirlar, string dil) =>
        satirlar.Select(s => AyarGram(s.AyarId, s.Gram, dil)).ToList();

    /// <summary>Müşteri satırı: "Emanette 22 ayar · 12,50 g" (çok ayarda " + " ile).</summary>
    public static string Emanette(IEnumerable<Kuyumcu.Core.Domain.EmanetAyarSatiri> satirlar, string dil) =>
        $"{(En(dil) ? "In safekeeping" : "Emanette")} {string.Join(" + ", Parcalar(satirlar, dil))}";

    /// <summary>
    /// Yıldız müşteri satırı durum parçası (satır " · " ile birleştiği için iç ayırıcı YOK):
    /// "emanette 22 ayar 12,50 g".
    /// </summary>
    public static string EmanetteDuz(IEnumerable<Kuyumcu.Core.Domain.EmanetAyarSatiri> satirlar, string dil) =>
        $"{(En(dil) ? "in safekeeping" : "emanette")} {string.Join(" + ", satirlar.Select(x => $"{AyarAdi(x.AyarId, dil)} {Gram(x.Gram, dil)}"))}";

    /// <summary>İade alanı etiketi (bırakıldığı gibi girilir).</summary>
    public static string IadeGram(string dil) => En(dil) ? "Weight returned (g)" : "Geri verilen gram";
    public static string IadeAyar(string dil) => En(dil) ? "Karat" : "Ayar";
    /// <summary>İkincil has satırı (yalnız PRO): "= 11,4500 g has".</summary>
    public static string HasIkincil(string has, string dil) => $"= {has}";
}
