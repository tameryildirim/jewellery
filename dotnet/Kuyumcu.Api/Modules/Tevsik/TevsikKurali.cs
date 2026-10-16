namespace Kuyumcu.Api.Modules.Tevsik;

// ═════════════════════════════════════════════════════════════════════════════
// TEVSİK ZORUNLULUĞU — 459 s. VUK Genel Tebliği · PLAN-2 Adım 2
//
// DAYANAK (birincil kaynak doğrulandı):
//   459 s. VUK Genel Tebliği, 575 sıra no'lu Genel Tebliğ ile değiştirildi.
//   Resmî Gazete 30.11.2024 / 32738 — birebir: "'7.000' ibareleri '30.000'
//   şeklinde ... değiştirilmiştir."
//
// KURAL: Tahsilat ve tediyelerin belirlenen haddi aşması hâlinde, ödeme banka
// veya finans kurumu aracılığıyla yapılmalı ve TEVSİK EDİLMELİDİR. Nakit
// yapılamaz.
//
// ⚠ KÜMÜLATİF: Had, tek işlem için değil, AYNI GÜNDE AYNI KİŞİYLE yapılan
// işlemlerin TOPLAMI için geçerlidir. Bu yüzden 3 × 12.000 TL nakit, tek bir
// 36.000 TL kadar ihlaldir. Bu kural olmadan sistem, işlemi bölerek haddin
// atlatılmasına sessizce izin verirdi.
//
// ⚠ CEZASI İKİ TARAFA BİRDEN kesilir (alıcı ve satıcı). Bu yüzden fail-closed:
// eşik aşıldığında işlem KAPANMAZ — uyarıp geçmez (Mehmet'in K8 koşulu:
// "sessiz loglama kabul değil").
//
// GÜN SINIRI: Europe/Istanbul yerel günü (K5). UTC gün sınırı kullanılsaydı
// İstanbul'da 00:00–03:00 arası yapılan nakit "dünkü" güne düşer ve kümülatif
// toplam yanlış hesaplanırdı.
//
// EŞİK YAPILANDIRMADAN OKUNUR (ADR-0005): mevzuat rakamı değişince kod değişmez.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>459 s. VUK Genel Tebliği tevsik eşikleri — <c>appsettings:Tevsik</c>.</summary>
public sealed class TevsikEsikleri
{
    /// <summary>
    /// Tevsik haddi. 30.000 TL — 459 s. VUK Genel Tebliği, 575 s. Tebliğ (RG 30.11.2024/32738).
    /// Önceki değer 7.000 TL idi.
    /// </summary>
    public decimal Had { get; init; } = 30_000m;

    /// <summary>
    /// Kural etkin mi. Kapatılırsa <b>yalnız uyarı</b> üretir, işlemi durdurmaz.
    /// ⚠ Bu bir kaçış kapısıdır ve varsayılan AÇIKTIR; kapatmak mevzuata
    /// aykırı davranmayı seçmektir ve bilinçli bir karardır.
    /// </summary>
    public bool Etkin { get; init; } = true;

    /// <summary>
    /// İŞLEM TUTARI KURALI (459 s. VUK GT §4.1.2). Açıkken: işlemin TOPLAM
    /// tutarı haddi aşıyorsa, ödemenin bir kısmının nakit yapılması da ihlaldir.
    ///
    /// Dayanak (mevzuat araştırması, 2026-09-15, iki hasım mercekten doğrulandı):
    /// tebliğ "işlemin toplam tutarı dikkate alınacak ve HER BİR tahsilat ve
    /// ödeme ... aracı finansal kurumlar kanalıyla gerçekleştirilecektir" der.
    /// GİB'in "30 Bin TL Üzeri Tahsilat ve Ödemelerde Tevsik Zorunluluğu"
    /// rehberindeki (Aralık 2024, Yayın No: 533) Örnek 2, 50.000 TL'lik bir
    /// satışın peşin nakit alınıp sonradan bankaya yatırılmasını dahi tevsik
    /// saymaz. Ceza ALICIYA VE SATICIYA AYRI AYRI kesilir.
    ///
    /// ⚠ Varsayılan AÇIKTIR. Kapatmak, kısmi nakit ödemeli büyük işlemleri
    /// geçirmek demektir ve bilinçli bir karardır. Mali müşavir aksi yönde
    /// görüş verirse burası kapatılır; KOD DEĞİŞMEZ (ADR-0005).
    ///
    /// ⚠ TAKASA UYGULANMAZ. Trampa mevzuatta hiç geçmiyor; araştırma bu
    /// başlıkta DÜŞÜK güven verdi ve doğrulama mercekleri aksi yönde bir
    /// özelge buldu. Doğrulanmamış bir yorum yüzünden her altın takasını
    /// bloke etmeyiz; sınır kodda ve belgede açıkça yazılıdır.
    /// </summary>
    public bool IslemTutariKuraliEtkin { get; init; } = true;
}

/// <summary>Tevsik değerlendirmesinin sonucu.</summary>
/// <param name="Gerekli">Bu ödeme tevsik edilmeli mi (banka üzerinden).</param>
/// <param name="GunlukNakitToplam">Bu işlem DAHİL, aynı gün aynı kişiyle yapılan nakit toplamı.</param>
/// <param name="Had">Uygulanan had.</param>
/// <param name="Asim">Haddi aşan tutar.</param>
/// <param name="Mesaj">Kullanıcıya gösterilecek açıklama; gerekli değilse null.</param>
public sealed record TevsikSonuc(
    bool Gerekli, decimal GunlukNakitToplam, decimal Had, decimal Asim, string? Mesaj)
{
    public static readonly TevsikSonuc Gerekmez = new(false, 0m, 0m, 0m, null);
}

/// <summary>
/// Saf kural — I/O yok, tarih/veritabanı bilmez. Günlük nakit toplamını
/// çağıran verir; bu sınıf yalnız karar üretir. Böylece tam sınır testleri
/// (29.999 / 30.000 / 30.001) veritabanı olmadan koşturulabilir.
/// </summary>
public sealed class TevsikKurali(TevsikEsikleri esikler, Common.YasalParametreServisi? yasal = null)
{
    /// <summary>PAKET YASAL-3: red metni tr-TR biçimiyle (sunucu kültüründen bağımsız).</summary>
    private static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// PAKET YASAL-3: istemcinin nakit sınırı reddini METNE bakmadan tanıdığı kod (hata gövdesi `kod`).
    /// </summary>
    public const string RedKodu = "NAKIT_SINIRI";

    /// <summary>
    /// PAKET YASAL-1: BUGÜNÜN (yerel gün) eşikleri. Had tarihli tablodan okunur
    /// (<see cref="Common.YasalParametreServisi"/>); tablo tanımsızsa 503 (fail-closed).
    /// Veritabanısız kurulumlarda (testler, <c>Varsayilan()</c>) yapılandırma nesnesi döner.
    /// </summary>
    public TevsikEsikleri Esikler => yasal is null ? esikler : EsiklerGunu(yasal.Bugun);

    /// <summary>
    /// PAKET YASAL-1: VERİLEN YEREL GÜNÜN eşikleri — geçmiş işlem O GÜNÜN haddiyle
    /// değerlendirilir. Had yalnız tablodan gelir; <see cref="TevsikEsikleri.Etkin"/>
    /// ve <see cref="TevsikEsikleri.IslemTutariKuraliEtkin"/> politika anahtarlarıdır
    /// (mevzuat rakamı değil) ve yapılandırmada kalır.
    /// </summary>
    public TevsikEsikleri EsiklerGunu(DateOnly gun) => yasal is null
        ? esikler
        : new TevsikEsikleri
        {
            Had = yasal.Sayi(Kuyumcu.Api.Data.YasalAnahtar.TevsikHaddiTl, gun),
            Etkin = esikler.Etkin,
            IslemTutariKuraliEtkin = esikler.IslemTutariKuraliEtkin,
        };

    /// <summary>İşlem anının (UTC) yerel günündeki eşiklerle değerlendirir.</summary>
    public TevsikSonuc Degerlendir(
        decimal oncekiGunlukNakit, decimal buIslemNakit, decimal? islemToplamTutari, DateTime islemAniUtc) =>
        Degerlendir(oncekiGunlukNakit, buIslemNakit, islemToplamTutari,
            EsiklerGunu(Common.YasalParametreServisi.IslemGunu(islemAniUtc)));

    /// <summary>
    /// Bu ödeme tevsik gerektiriyor mu?
    /// </summary>
    /// <param name="oncekiGunlukNakit">
    /// Aynı gün, aynı kişiyle YAPILMIŞ nakit işlemlerin toplamı (bu işlem hariç).
    /// </param>
    /// <param name="buIslemNakit">Bu işlemde nakit olarak ödenen/tahsil edilen tutar.</param>
    /// <param name="islemToplamTutari">
    /// İşlemin TOPLAM tutarı (nakit olmayan bacaklar dâhil). Verilirse
    /// <see cref="TevsikEsikleri.IslemTutariKuraliEtkin"/> kapısı da işler.
    /// Verilmezse yalnız kümülatif kural çalışır — eski davranış.
    /// </param>
    public TevsikSonuc Degerlendir(
        decimal oncekiGunlukNakit, decimal buIslemNakit, decimal? islemToplamTutari = null) =>
        Degerlendir(oncekiGunlukNakit, buIslemNakit, islemToplamTutari, Esikler);

    /// <summary>Verilen (tarihi çözülmüş) eşiklerle değerlendirir — saf çekirdek.</summary>
    public static TevsikSonuc Degerlendir(
        decimal oncekiGunlukNakit, decimal buIslemNakit, decimal? islemToplamTutari, TevsikEsikleri esikler)
    {
        // İşaret önemsizdir: tahsilat da tediye de hadde tabidir.
        var toplam = Math.Abs(oncekiGunlukNakit) + Math.Abs(buIslemNakit);

        if (Math.Abs(buIslemNakit) == 0m) return TevsikSonuc.Gerekmez;

        // ── KAPI 2: İŞLEM TUTARI KURALI (§4.1.2) ──
        // Kümülatiften ÖNCE bakılır: 50.000 TL'lik bir fişin 20.000'i nakitse,
        // günün toplamı haddin altında olsa bile bu ihlaldir.
        if (esikler.IslemTutariKuraliEtkin && islemToplamTutari is { } islemTutari
            && Math.Abs(islemTutari) > esikler.Had)
        {
            // PAKET YASAL-3 (YASAL-2 devri): görünür metinde mevzuat kodu YOK — tek sade
            // cümle + ne yapmalı (TaksitMesaji kalıbı). Dayanak yalnız ekranın ipucunda
            // (NakitSiniriDayanagi) durur; istemci reddi `kod` = NAKIT_SINIRI ile tanır.
            var mesaj2 = string.Format(Tr,
                "İşlemin toplamı {0:N2} TL, {1:N2} TL nakit sınırını aşıyor; {2:N2} TL nakit de alınamaz, tamamı kartla ya da havaleyle ödenmeli.",
                Math.Abs(islemTutari), esikler.Had, Math.Abs(buIslemNakit));
            return new TevsikSonuc(
                true, toplam, esikler.Had, Math.Abs(islemTutari) - esikler.Had, mesaj2);
        }

        // Haddi AŞMASI hâlinde — yani tam had (30.000) hâlâ serbesttir.
        if (toplam <= esikler.Had)
            return new TevsikSonuc(false, toplam, esikler.Had, 0m, null);

        var asim = toplam - esikler.Had;
        var kumulatif = Math.Abs(oncekiGunlukNakit) > 0m;

        // PAKET YASAL-3 (YASAL-2 devri): tek sade cümle, mevzuat kodu yok (bkz. mesaj2 notu).
        var mesaj = kumulatif
            ? string.Format(Tr,
                "Bu kişiyle bugün nakit toplamı {0:N2} TL oluyor (önceki {1:N2} TL, bu işlem {2:N2} TL) ve {3:N2} TL nakit sınırını {4:N2} TL aşıyor; kalanı kartla ya da havaleyle ödenmeli.",
                toplam, Math.Abs(oncekiGunlukNakit), Math.Abs(buIslemNakit), esikler.Had, asim)
            : string.Format(Tr,
                "{0:N2} TL nakit, {1:N2} TL nakit sınırını {2:N2} TL aşıyor; bu ödeme kartla ya da havaleyle yapılmalı.",
                Math.Abs(buIslemNakit), esikler.Had, asim);

        return new TevsikSonuc(true, toplam, esikler.Had, asim, mesaj);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 (TB-15) — "BÜYÜK İŞLEMİN TAKSİTİ" KURALI (saf)
//
// DAYANAK (birincil, şartname § 3.1 M-1): 459 s. VUK GT §4.1.2 — haddi aşan
// işlemde bedel farklı tarihlerde kısım kısım ödenirse "işlemin toplam tutarı
// dikkate alınacak ve her bir tahsilat ve ödeme" aracı finansal kurum kanalıyla
// yapılır. Yani 62.000 TL'lik veresiye satışın 20.000 TL'lik taksiti de nakit
// alınamaz — günlük kümülatif haddin altında kalsa bile.
//
// KARAR (şartname § 4.1): ödeme açık partilere FIFO ile dağılır (defterin tek
// doğruluk kuralı — VadeKapisi). Kaynak işlemi haddi AŞAN partiye düşen nakit
// reddedilir; had KAYNAK İŞLEMİN GÜNÜNÜN haddidir (YASAL-1, T-3/K-B önerisi —
// DOĞRULANAMADI, mali müşavir teyidi). Kaynağı bilinmeyen (açılış/devir) pay
// GEÇER ve iz bırakır (K-A önerisi). Takas kaynaklı parti kural dışıdır
// (T-4 — §4.1.2 kapısıyla tutarlı).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Taksit kuralında bir partinin kaynağı.</summary>
public enum TaksitKaynak
{
    /// <summary>Kaynak işlemi bilinen parti (satış/alış fişi, veresiye …).</summary>
    Islem,
    /// <summary>Kaynağı bilinmeyen bakiye (açılış, devir, eski satır) — geçer + iz.</summary>
    Kaynaksiz,
    /// <summary>Kural dışı kaynak (takas — § 3.2 T-4).</summary>
    Muaf,
    /// <summary>Açık borcu aşan ödeme (avans) — hiçbir işlemin taksiti değil.</summary>
    Fazla,
}

/// <summary>Bir ödemenin bir partiye düşen payı.</summary>
/// <param name="PartiId">Partiyi açan hareket (Kaynaksiz/Fazla için null).</param>
/// <param name="Tuketilen">Bu ödemeden bu partiye düşen TL.</param>
/// <param name="Tur">Partinin kaynağı.</param>
/// <param name="KaynakTutar">Kaynak işlemin toplam tutarı (yalnız Islem).</param>
/// <param name="KaynakGun">Kaynak işlemin yerel günü (yalnız Islem).</param>
public readonly record struct TaksitParti(
    string? PartiId, decimal Tuketilen, TaksitKaynak Tur, decimal KaynakTutar, DateOnly KaynakGun);

/// <summary>Açık parti + kaynak işlem bilgisi (FIFO dağıtımına girdi).</summary>
public readonly record struct TaksitKaynagi(
    string PartiId, decimal Kalan, TaksitKaynak Tur, decimal KaynakTutar, DateOnly KaynakGun);

/// <summary>Taksit kuralının kararı.</summary>
/// <param name="Red">Nakit payı izin verilenden büyük mü.</param>
/// <param name="IzinliNakit">Bu ödemede en çok alınabilecek nakit.</param>
/// <param name="Dogrulanamayan">Kaynağı bilinmeyen partiye düşen nakit (iz yazılır).</param>
/// <param name="BuyukKaynakTutar">Haddi aşan ilk kaynak işlemin tutarı (varsa).</param>
/// <param name="BuyukKaynakGun">Haddi aşan ilk kaynak işlemin günü (varsa).</param>
/// <param name="BuyukHad">O günün haddi (varsa).</param>
public sealed record TaksitSonuc(
    bool Red, decimal IzinliNakit, decimal Dogrulanamayan,
    decimal? BuyukKaynakTutar, DateOnly? BuyukKaynakGun, decimal? BuyukHad)
{
    public static readonly TaksitSonuc Serbest = new(false, decimal.MaxValue, 0m, null, null, null);

    /// <summary>Büyük işleme bağlı parti var mı (kural bu ödemede devrede mi).</summary>
    public bool KuralDevrede => BuyukKaynakTutar is not null;
}

/// <summary>Saf taksit kuralı — I/O yok.</summary>
public static class TaksitKurali
{
    /// <summary>
    /// Ödemeyi açık partilere FIFO dağıtır: önce kaynağı bilinmeyen bakiye (en eski
    /// sayılır), sonra partiler eskiden yeniye; artan ödeme <see cref="TaksitKaynak.Fazla"/>.
    /// </summary>
    public static List<TaksitParti> FifoDagit(
        decimal odeme, decimal kaynaksiz, IReadOnlyList<TaksitKaynagi> partiler)
    {
        var sonuc = new List<TaksitParti>();
        var kalan = Math.Abs(odeme);
        if (kalan == 0m) return sonuc;

        if (kaynaksiz > 0m)
        {
            var t = Math.Min(kaynaksiz, kalan);
            sonuc.Add(new TaksitParti(null, t, TaksitKaynak.Kaynaksiz, 0m, default));
            kalan -= t;
        }
        foreach (var p in partiler)
        {
            if (kalan <= 0m) break;
            if (p.Kalan <= 0m) continue;
            var t = Math.Min(p.Kalan, kalan);
            sonuc.Add(new TaksitParti(p.PartiId, t, p.Tur, p.KaynakTutar, p.KaynakGun));
            kalan -= t;
        }
        if (kalan > 0m) sonuc.Add(new TaksitParti(null, kalan, TaksitKaynak.Fazla, 0m, default));
        return sonuc;
    }

    /// <summary>Partinin kaynak işlemi, KAYNAK GÜNÜN haddini AŞIYOR mu (tam had serbest).</summary>
    public static bool Buyuk(TaksitParti p, Func<DateOnly, decimal> had) =>
        p.Tur == TaksitKaynak.Islem && p.KaynakTutar > had(p.KaynakGun);

    /// <summary>
    /// Kural: izinliNakit = Σ tüketilen (büyük OLMAYAN partiler); nakit &gt; izinli → RED.
    /// </summary>
    /// <param name="nakit">Bu ödemenin nakit payı.</param>
    /// <param name="tuketilen">Ödemenin FIFO dağılımı (<see cref="FifoDagit"/>).</param>
    /// <param name="had">Günün haddi (YASAL-1 tarihli tablo).</param>
    public static TaksitSonuc TaksitDegerlendir(
        decimal nakit, IReadOnlyList<TaksitParti> tuketilen, Func<DateOnly, decimal> had)
    {
        nakit = Math.Abs(nakit);
        decimal izinli = 0m, dogrulanamayan = 0m;
        TaksitParti? ilkBuyuk = null;
        foreach (var p in tuketilen)
        {
            if (Buyuk(p, had)) { ilkBuyuk ??= p; continue; }
            izinli += p.Tuketilen;
            if (p.Tur == TaksitKaynak.Kaynaksiz) dogrulanamayan += p.Tuketilen;
        }
        var red = nakit > izinli;
        // Kaynağı bilinmeyen paya düşen NAKİT: FIFO'da en önde olduğu için
        // nakdin ilk kısmı oraya düşer.
        var dogNakit = Math.Min(dogrulanamayan, nakit);
        return ilkBuyuk is { } b
            ? new TaksitSonuc(red, izinli, dogNakit, b.KaynakTutar, b.KaynakGun, had(b.KaynakGun))
            : new TaksitSonuc(red, izinli, dogNakit, null, null, null);
    }

    /// <summary>
    /// Önizleme: bu cariden şu an en çok ne kadar NAKİT alınabilir — FIFO'da ilk
    /// büyük partiye kadar olan açık tutar. Büyük parti yoksa null (sınır yok).
    /// </summary>
    public static decimal? NakitAlinabilir(
        decimal kaynaksiz, IReadOnlyList<TaksitKaynagi> partiler, Func<DateOnly, decimal> had)
    {
        var toplam = Math.Max(0m, kaynaksiz);
        foreach (var p in partiler)
        {
            if (p.Kalan <= 0m) continue;
            if (Buyuk(new TaksitParti(p.PartiId, p.Kalan, p.Tur, p.KaynakTutar, p.KaynakGun), had))
                return toplam;
            toplam += p.Kalan;
        }
        return null;
    }

    /// <summary>
    /// PAKET YASAL-2 (TB-16) — kendiliğinden bildirimin SON GÜNÜ: sayım ihlali
    /// İZLEYEN günden başlar; hafta sonu ve <paramref name="tatiller"/> atlanır.
    /// Arife yarım günü İŞ GÜNÜ sayılır (§ 3.2 T-2 — kısa hesap güvenli yöndür).
    /// Tatil takviminde bayram eksikse süre KISA çıkar (güvenli yön).
    /// </summary>
    public static DateOnly BildirimSonGunu(DateOnly ihlalGunu, int isGunu, IReadOnlySet<DateOnly> tatiller)
    {
        var gun = ihlalGunu;
        var kalan = isGunu;
        while (kalan > 0)
        {
            gun = gun.AddDays(1);
            if (gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (tatiller.Contains(gun)) continue;
            kalan--;
        }
        return gun;
    }
}
