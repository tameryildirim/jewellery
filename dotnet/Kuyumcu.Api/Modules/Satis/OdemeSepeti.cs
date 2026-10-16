using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Common;

namespace Kuyumcu.Api.Modules.Satis;

/// <summary>Bir fişin tek ödeme bacağı — "30.000'in 12.000'i nakit" gibi.</summary>
public sealed class OdemeBacagiDto
{
    /// <summary>
    /// Ödeme aracı: NAKIT · KART · HAVALE · VERESIYE · DOVIZ · ALTIN.
    ///
    /// <para>
    /// PAKET B-4 (KARAR-HAS-DEFTERI-2026-09-23, B4-1): ALTIN artık bir BACAKTIR.
    /// Eski "ALTIN BİLEREK YOKTUR" notunun beklediği belge üretildi: müşterinin
    /// verdiği eski altın, satışla AYNI transaction'da ve AYNI kur kaydıyla
    /// yazılan AYRI bir HURDA_ALIS hareketidir (trampa iki ayrı teslimdir,
    /// KDVK md. 2/5). Kasaya GİRMEZ, cariye has YAZMAZ, gider pusulası DOĞURUR.
    /// Bu liste <c>Data.OdemeAraci.Tumu</c> ile AYNIDIR (B4-2) — ayrışırsa
    /// sunucunun kabul ettiği bacak kalem olarak yazılamaz.
    /// </para>
    /// </summary>
    [Required, RegularExpression("NAKIT|KART|HAVALE|VERESIYE|DOVIZ|ALTIN")]
    public string Arac { get; set; } = null!;

    /// <summary>
    /// Bu bacakla kapatılan TL. Sıfır ve negatif bacak REDDEDİLİR.
    /// DOVIZ bacağında bu değer İSTEMCİNİN HESABIDIR; sunucu aynı kur kaydından
    /// kendi hesabını yapar ve bir kuruş bile tutmuyorsa fişi reddeder.
    /// </summary>
    // PAKET PATCH-4: kolon tavanı (decimal(15,2)) — taşma ağdan alansız 400 değil, alan:odemeler[i].tutarTl.
    [Range(0.01, KolonTavani.UstTutar)]
    public decimal TutarTl { get; set; }

    /// <summary>NAKIT/KART/HAVALE/DOVIZ için ZORUNLU; VERESIYE'de boş olmalıdır.</summary>
    [MaxLength(36)] public string? KasaId { get; set; }

    /// <summary>
    /// YALNIZ DOVIZ bacağı: efektifin para birimi (USD · EUR · GBP). Kasanın para
    /// birimiyle sunucuda eşlenir; TRY döviz bacağı olamaz.
    /// </summary>
    [RegularExpression("USD|EUR|GBP")] public string? ParaBirimi { get; set; }

    /// <summary>
    /// YALNIZ DOVIZ bacağı: kasaya giren EFEKTİF miktar (döviz cinsinden, ≥ 0,01).
    ///
    /// ⚠ NEDEN TL DEĞİL: kasaya giren şey dolardır, TL değil. Döviz kasasına TL
    /// yazılsaydı kasa bakiyesi hangi para biriminde olduğu belirsiz bir sayı olur,
    /// gün sonu sayımı hiçbir zaman tutmazdı (K6'nın varlık sebebi).
    /// </summary>
    [Range(0.01, KolonTavani.UstTutar)] public decimal? DovizTutar { get; set; }   // PATCH-4: kolon tavanı

    /// <summary>
    /// PAKET 31 — YALNIZ KART bacağı: tahsilatın geçtiği POS cihazı. Boşsa
    /// şubede TEK aktif cihaz varsa o seçilir; birden çok varsa istek REDDEDİLİR
    /// (ilk cihaz SEÇİLMEZ — fail-closed). Hiç cihaz yoksa Pos.CihazZorunlu
    /// kapalıyken POS satırı yazılmaz, bugünkü davranış aynen sürer.
    /// </summary>
    [MaxLength(36)] public string? PosCihazId { get; set; }

    /// <summary>PAKET 31 — kart taksit adedi (1 = tek çekim). Taksit oranı varsa komisyon ondan hesaplanır.</summary>
    [Range(1, 12)] public int? PosTaksitAdedi { get; set; }

    // ── PAKET B-4 (B4-4) — YALNIZ ALTIN BACAĞI ───────────────────────────────
    // Bu dört alan YALNIZ ALTIN bacağında verilebilir; başka bacakta gelirse
    // 400 (DOVIZ'in paraBirimi/dovizTutar kuralıyla aynı sertlik). ALTIN'da
    // dördü de ZORUNLUDUR. TutarTl istemcinin hesabıdır; sunucu aynı formülle
    // (Core.AltinBacagiHesap) kendi netini üretir ve kuruş şaşarsa reddeder.

    /// <summary>YALNIZ ALTIN: eski altının ayarı (K22, K14, G925 …). Maden bu ayardan türetilir.</summary>
    [MaxLength(10)] public string? AyarId { get; set; }

    /// <summary>YALNIZ ALTIN: tartılan brüt gram (&gt; 0).</summary>
    // PAKET PATCH-4: yalnız ÜST kolon tavanı (decimal(12,3)); alt sınır 0 — "> 0" kuralı ve
    // iletisi Coz içinde kalır (mevcut davranış korunur).
    [Range(0, KolonTavani.UstAdet)] public decimal? GramAgirlik { get; set; }

    /// <summary>
    /// YALNIZ ALTIN: dükkânın HAS GRAM başına alış fiyatı (&gt; 0). Sunucu bu
    /// fiyatı kur kaydından TÜRETMEZ (K-KUR-3/4) — dükkânın ticari kararıdır.
    /// </summary>
    [Range(0, KolonTavani.UstTutar)] public decimal? BirimFiyatTlHasGram { get; set; }   // PATCH-4: kolon tavanı

    /// <summary>YALNIZ ALTIN: satıcı kimliği — gider pusulası için ZORUNLU, tutar eşiği YOK (VUK md.234).</summary>
    public Kuyumcu.Api.Modules.Hurda.HurdaSaticiDto? Satici { get; set; }
}

/// <summary>
/// İşlem anındaki kur kaydının döviz kurları (1 birim döviz = ? TL). Boş değer
/// "o gün o kur çekilememiş" demektir; DOVIZ bacağı için FAIL-CLOSED redde yol açar.
/// </summary>
public readonly record struct DovizKurlari(decimal? UsdTry, decimal? EurTry, decimal? GbpTry)
{
    /// <summary>Döviz bacağı olmayan çağrılar için — herhangi bir DOVIZ bacağını reddeder.</summary>
    public static readonly DovizKurlari Yok = new(null, null, null);

    public decimal? Kur(string? paraBirimi) => paraBirimi switch
    {
        "USD" => UsdTry, "EUR" => EurTry, "GBP" => GbpTry, _ => null,
    };
}

/// <summary>Çözülmüş sepet — tutarlar doğrulanmış, paylar hesaplanmıştır.</summary>
/// <param name="Bacaklar">Girdi bacakları, sırası korunmuş.</param>
/// <param name="NakitToplam">Tevsik kapısına verilecek NAKİT toplamı.</param>
/// <param name="VeresiyeToplam">Cariye borçlanacak toplam.</param>
/// <param name="DovizToplamTl">
/// Döviz bacaklarının TL karşılığı toplamı. <see cref="NakitToplam"/>'a ZATEN
/// DAHİLDİR; ayrıca verilir ki fiş yanıtı ve günlük özet kırılımı gösterebilsin.
/// </param>
/// <param name="Altin">
/// PAKET B-4 — fişin ALTIN bacağının sunucuda türetilmiş değerleri; bacak
/// yoksa <c>null</c>. NakitToplam'a da VeresiyeToplam'a da KATILMAZ (B4-13).
/// </param>
public sealed record CozulmusSepet(
    IReadOnlyList<OdemeBacagiDto> Bacaklar, decimal NakitToplam, decimal VeresiyeToplam,
    decimal DovizToplamTl, AltinBacagiCozumu? Altin = null);

/// <summary>PAKET B-4 — ALTIN bacağının doğrulanmış ve türetilmiş hâli.</summary>
/// <param name="Bacak">Girdi bacağı.</param>
/// <param name="Ayar">Eski altının ayarı (maden buradan).</param>
/// <param name="SaticiTipi">hurda.Dogrula'nın döndürdüğü satıcı tipi.</param>
/// <param name="Deger">Has · brüt · stopaj · net (tek kaynak: Core.AltinBacagiHesap).</param>
public sealed record AltinBacagiCozumu(
    OdemeBacagiDto Bacak, Kuyumcu.Core.Domain.Ayar Ayar, string SaticiTipi,
    Kuyumcu.Core.Domain.AltinBacagiDegeri Deger);

/// <summary>
/// Ödeme bacaklarının TEK doğrulama merkezi.
///
/// Tüm kapılar transaction AÇILMADAN ÖNCE işler (MASAK ve tevsik ile aynı desen):
/// hadde/kurala takılan bir fişte hiçbir yazma başlamasın.
/// </summary>
public static class OdemeSepeti
{
    /// <summary>En fazla bacak sayısı — kazara üretilmiş devasa istekleri keser.</summary>
    public const int EnFazlaBacak = 5;

    /// <summary>
    /// PARA-DEMO-2 (madde 1) — çok bacaklı fiş ucunun hareket etiketi. "KARMA" YALNIZ iki ve
    /// daha çok bacakta; tek bacakta o bacağın aracı (NAKIT/KART/HAVALE/VERESIYE/DOVIZ/ALTIN).
    /// Aynı araç iki kez girilmişse (2 × KART) karışık değildir → o araç.
    /// </summary>
    public static string FisEtiketi(IReadOnlyList<OdemeBacagiDto> bacaklar)
    {
        var araclar = bacaklar.Select(b => b.Arac).Distinct(StringComparer.Ordinal).ToList();
        return araclar.Count == 1 ? araclar[0] : "KARMA";
    }

    /// <summary>
    /// Bacakları doğrular ve paylarını toplar.
    /// </summary>
    /// <param name="bacaklar">Fişin ödeme bacakları.</param>
    /// <param name="fisToplamTl">Fiş kalemlerinin TL toplamı.</param>
    /// <param name="cariId">Cari — VERESIYE bacağı varsa zorunludur.</param>
    /// <param name="kurlar">
    /// İşlem anındaki kur kaydının döviz kurları. ZORUNLU konumsal parametre,
    /// varsayılanı yok: DOVIZ bacağının TL karşılığını kasiyer değil SUNUCU
    /// hesaplar. İstemcinin TL'sine güvenilseydi kasiyer her fişte kendi kurunu
    /// uygular, dükkân fiilen döviz alım-satımı yapmış olurdu — yetki belgesiz
    /// kuyumcu için 1567 s. Kanun md.3/(5) kapsamında ceza ve kapatma sebebi
    /// (bkz. PAKET-15-DOVIZ-KARAR-2026-09-15.md).
    /// </param>
    /// <param name="sistemCarisi">
    /// PAKET B-3 (D2 · K3): cari kiracının "Peşin Müşteri" kartı mı? Çağıran
    /// sunucuda ölçer (<c>SistemCarisi.MiAsync</c>); istemciden ALINMAZ.
    /// Doğruysa fiş farkı açık hesaba (VERESIYE) YAZILAMAZ.
    /// </param>
    /// <param name="hurda">
    /// PAKET B-4 — ALTIN bacağının stopaj oranı ve satıcı kimliği kapısı
    /// (yapılandırmadan, ADR-0005). ALTIN bacağı varken <c>null</c> verilirse
    /// fiş REDDEDİLİR (fail-closed): oran bilinmeden net TL üretilmez.
    /// </param>
    public static CozulmusSepet Coz(
        IReadOnlyList<OdemeBacagiDto>? bacaklar, decimal fisToplamTl, string? cariId,
        DovizKurlari kurlar, bool sistemCarisi = false,
        Kuyumcu.Api.Modules.Hurda.GiderPusulasiKapisi? hurda = null)
    {
        if (bacaklar is null || bacaklar.Count == 0)
            throw new BadRequestException("Fiş en az bir ödeme bacağı içermelidir");

        if (bacaklar.Count > EnFazlaBacak)
            throw new BadRequestException(
                $"Bir fişte en fazla {EnFazlaBacak} ödeme bacağı olabilir (gelen: {bacaklar.Count})");

        foreach (var b in bacaklar)
            if (b.TutarTl <= 0m)
                throw new BadRequestException($"Ödeme bacağı tutarı sıfırdan büyük olmalıdır ({b.Arac})");

        var veresiyeler = bacaklar.Where(b => b.Arac == "VERESIYE").ToList();

        if (veresiyeler.Count > 1)
            throw new BadRequestException("Bir fişte yalnız BİR veresiye bacağı olabilir");

        // ⚠ Cari olmadan veresiye yazılamaz: borç kime yazılacağı bilinmeden
        // kaydedilen alacak, hiç kimsenin borcu olur ve tahsil edilemez.
        // Bugünkü tek ödemeli yolda cari boşsa bakiye SESSİZCE düşüyor; yeni
        // uçta bu davranış tekrarlanmaz.
        if (veresiyeler.Count == 1 && string.IsNullOrEmpty(cariId))
            throw new BadRequestException("Veresiye bacağı için müşteri (cari) seçilmelidir");

        // ── PAKET B-3 (D2 · K3) — PEŞİN MÜŞTERİ'DE FARK ≠ 0 KAYDEDİLEMEZ ─────
        // Fiş farkı ya ödeme bacaklarıyla kapanır ya da gerçek bir müşteriye
        // açık hesap (VERESIYE) olarak yazılır. Kimliksiz karta yazılan borç
        // kimsenin borcu değildir ve tahsil edilemez. Bacak toplamı eşitliği
        // (aşağıda, TAM EŞİTLİK) bu kuralla birlikte "fark = 0" demektir.
        if (sistemCarisi && veresiyeler.Count > 0)
            throw new BadRequestException(Kuyumcu.Api.Modules.Cari.SistemCarisi.VeresiyeYasakMesaji);

        // ── PAKET B-4 (B4-6) — BİR FİŞTE EN FAZLA BİR ALTIN BACAĞI ──────────
        // VERESIYE kuralıyla simetrik. İki ayrı ayarın hası tek gider pusulasına
        // sığmaz; iki parça eski altın iki ayrı fiş ya da hurda alış ucudur.
        var altinBacaklari = bacaklar.Where(b => b.Arac == AltinArac).ToList();
        if (altinBacaklari.Count > 1)
            throw new BadRequestException(
                "Bir fişte yalnız BİR eski altın bacağı olabilir. İkinci parça için ayrı fiş " +
                "ya da Hurda alış ekranını kullanın (her parçanın kendi gider pusulası olur).");

        AltinBacagiCozumu? altin = null;
        foreach (var b in bacaklar)
        {
            if (b.Arac == "VERESIYE")
            {
                if (!string.IsNullOrEmpty(b.KasaId))
                    throw new BadRequestException("Veresiye bacağına kasa verilemez — para kasadan geçmez");
            }
            else if (b.Arac == AltinArac)
            {
                // B4-5: eski altın kasadan GEÇMEZ — TL kasasına yazılsaydı kasada
                // olmayan TL varmış gibi görünürdü (tek etiketli ALTIN hatası).
                if (!string.IsNullOrEmpty(b.KasaId))
                    throw new BadRequestException(
                        "Eski altın bacağına kasa verilemez — altın kasaya TL olarak girmez");
            }
            else if (string.IsNullOrEmpty(b.KasaId))
            {
                throw new BadRequestException($"{b.Arac} bacağı için kasa seçilmelidir");
            }

            if (b.Arac == AltinArac)
            {
                altin ??= AltinBacaginiCoz(b, hurda);
            }
            else if (b.AyarId is not null || b.GramAgirlik is not null
                     || b.BirimFiyatTlHasGram is not null || b.Satici is not null)
            {
                // B4-4: gram/ayar/fiyat/satıcı YALNIZ altın bacağına aittir.
                throw new BadRequestException(
                    $"{b.Arac} bacağında ayar/gram/has fiyatı/satıcı verilemez — bunlar yalnız ALTIN bacağına aittir");
            }

            if (b.Arac == "DOVIZ")
            {
                DovizBacaginiDogrula(b, kurlar);
            }
            else if (b.ParaBirimi is not null || b.DovizTutar is not null)
            {
                // TL bacağına döviz alanı: ya istemci yanlış aracı seçti ya da
                // döviz miktarını TL diye yazıyor. İkisi de sessiz geçilmez.
                throw new BadRequestException(
                    $"{b.Arac} bacağında paraBirimi/dovizTutar verilemez — bunlar yalnız DOVIZ bacağına aittir");
            }
        }

        // ⚠ TAM EŞİTLİK, tolerans YOK. Bacak toplamı fiş toplamını bir kuruş bile
        // tutmuyorsa para ya kaybolur ya iki kez sayılır; "yakın" yeterli değildir.
        var bacakToplam = bacaklar.Sum(b => b.TutarTl);
        if (bacakToplam != fisToplamTl)
            throw new BadRequestException(
                $"Ödeme bacaklarının toplamı fiş tutarına eşit olmalıdır: " +
                $"bacaklar {bacakToplam:N2} TL, fiş {fisToplamTl:N2} TL " +
                $"(fark {bacakToplam - fisToplamTl:N2} TL)");

        var dovizToplamTl = bacaklar.Where(b => b.Arac == "DOVIZ").Sum(b => b.TutarTl);

        return new CozulmusSepet(
            bacaklar,
            // DÖVİZ EFEKTİF = NAKİT (459 s. VUK GT). Tebliğ tevsiki "aracı finansal
            // kurum kanalıyla" tanımlar; elden alınan dolar hiçbir kanaldan geçmez.
            // Yabancı paranın hadde hangi kurla çevrileceğine dair fıkra birincil
            // metinden teyit EDİLEMEDİ (arama, 2026-09-15); bu yüzden işlem anındaki
            // kur kaydı kullanılır ve döviz payı DAİMA nakit sayılır — kapı yalnız
            // SIKILAŞIR, gevşemez (fail-closed).
            //
            // PAKET B-4 (B4-13): ALTIN payı nakde KATILMAZ — altın nakit değildir.
            // Tevsik yine FİŞİN BRÜTÜNE bakar (SatisFisi, islemToplamTutari), yani
            // "yarısını altın verelim" ile had delinemez (B4-22).
            NakitToplam:    bacaklar.Where(b => b.Arac == "NAKIT").Sum(b => b.TutarTl) + dovizToplamTl,
            VeresiyeToplam: veresiyeler.Sum(b => b.TutarTl),
            DovizToplamTl:  dovizToplamTl,
            Altin:          altin);
    }

    /// <summary>ALTIN bacağının araç kodu — <c>Data.OdemeAraci.Altin</c> ile aynı.</summary>
    public const string AltinArac = Data.OdemeAraci.Altin;

    /// <summary>
    /// G-1 — tek etiketli <c>OdemeTuru = "ALTIN"</c> reddinin metni: kullanıcıyı
    /// altın BACAĞINA yönlendirir (sessiz 400 yok, UX-S30).
    /// </summary>
    public const string TekEtiketliAltinMesaji =
        "Altınla ödeme artık tek başına ödeme türü değildir: satış fişine 'Eski altın' " +
        "ödeme bacağı ekleyin (ayar, gram, has fiyatı ve satıcı kimliği ile). " +
        "Eski altın kasaya TL olarak yazılmaz; gider pusulası ile ayrı bir hurda alışı olarak kaydedilir.";

    /// <summary>
    /// PAKET B-4 — ALTIN bacağını doğrular ve değerlerini TEK KAYNAKTAN türetir.
    /// <list type="bullet">
    ///   <item>B4-4: ayar + gram + has fiyatı + satıcı ZORUNLU.</item>
    ///   <item>K-KUR-3/4: has fiyatı boşsa sunucu fiyat TÜRETMEZ — 400.</item>
    ///   <item>B4-20: satıcı kimliği tutar eşiği OLMADAN zorunlu (hurda.Dogrula).</item>
    ///   <item>B4-8..10: has/brüt/stopaj/net = Core.AltinBacagiHesap.</item>
    ///   <item>B4-11: istemcinin TutarTl'si sunucunun netine KURUŞU KURUŞUNA eşit.</item>
    /// </list>
    /// </summary>
    public static AltinBacagiCozumu AltinBacaginiCoz(
        OdemeBacagiDto b, Kuyumcu.Api.Modules.Hurda.GiderPusulasiKapisi? hurda)
    {
        if (hurda is null)
            throw new InvalidOperationException(
                "ALTIN bacağı gider pusulası kapısı olmadan çözülemez (stopaj oranı bilinmiyor).");

        if (string.IsNullOrWhiteSpace(b.AyarId))
            throw new BadRequestException("Eski altın bacağı için ayar seçilmelidir");
        var ayar = Kuyumcu.Core.Domain.Ayarlar.Bul(b.AyarId)
            ?? throw new BadRequestException($"Eski altın bacağında geçersiz ayar: {b.AyarId}");

        // K20-5: kıymetsiz ayarda has YOKTUR; sahte has ile ödeme alınmaz.
        if (!ayar.HasHesaplanir)
            throw new BadRequestException(
                $"{ayar.Ad} kıymetli maden değildir; has karşılığı olmadığı için ödeme bacağı olamaz");

        if (b.GramAgirlik is not { } gram || gram <= 0m)
            throw new BadRequestException("Eski altın bacağı için gram sıfırdan büyük olmalıdır");

        if (b.BirimFiyatTlHasGram is not { } fiyat || fiyat <= 0m)
            throw new BadRequestException(
                "Eski altın bacağı için has gram başına alış fiyatı girilmelidir. " +
                "Sistem alış fiyatını kur kaydından TÜRETMEZ — fiyat işletmenin kararıdır.");

        var saticiTipi = hurda.Dogrula(b.Satici);

        var deger = Kuyumcu.Core.Domain.AltinBacagiHesap.Hesapla(
            gram, ayar, fiyat, hurda.StopajOran(saticiTipi));

        if (deger.NetTl != b.TutarTl)
            throw new BadRequestException(
                $"Eski altın bacağının TL karşılığı sunucu hesabıyla uyuşmuyor: " +
                $"{deger.HasGrami:N6} g has × {fiyat:N2} TL = {deger.BrutTl:N2} TL brüt, " +
                $"stopaj {deger.StopajTutar:N2} TL, net {deger.NetTl:N2} TL; gönderilen {b.TutarTl:N2} TL");

        return new AltinBacagiCozumu(b, ayar, saticiTipi, deger);
    }

    /// <summary>
    /// DOVIZ bacağı: para birimi + efektif miktar zorunlu, kur o gün çekilmiş
    /// olmalı, istemcinin TL'si sunucununkine KURUŞU KURUŞUNA eşit olmalı.
    /// </summary>
    private static void DovizBacaginiDogrula(OdemeBacagiDto b, DovizKurlari kurlar)
    {
        if (b.ParaBirimi is null)
            throw new BadRequestException("DOVIZ bacağı için para birimi (USD/EUR/GBP) seçilmelidir");
        if (b.DovizTutar is not { } miktar || miktar <= 0m)
            throw new BadRequestException("DOVIZ bacağı için efektif miktar sıfırdan büyük olmalıdır");

        // FAIL-CLOSED: kur yoksa döviz alınmaz. "Yaklaşık kur" diye bir şey yok;
        // kayıtsız kurla alınan dolar, denetimde TL karşılığı ispatlanamayan tahsilattır.
        if (kurlar.Kur(b.ParaBirimi) is not { } kur || kur <= 0m)
            throw new BadRequestException(
                $"{b.ParaBirimi} kuru işlem anındaki kur kaydında yok; döviz tahsilatı yapılamaz");

        var sunucuTl = Math.Round(miktar * kur, 2, MidpointRounding.AwayFromZero);
        if (sunucuTl != b.TutarTl)
            throw new BadRequestException(
                $"DOVIZ bacağının TL karşılığı kur kaydıyla uyuşmuyor: " +
                $"{miktar:N2} {b.ParaBirimi} × {kur:N4} = {sunucuTl:N2} TL, gönderilen {b.TutarTl:N2} TL");
    }

    /// <summary>
    /// Fişin nakit ve veresiye paylarını KALEMLERE dağıtır.
    ///
    /// SIRALI doldurma kullanılır, orantılı DEĞİL: orantılı dağıtım her kalemde
    /// yuvarlama yapar ve payların toplamı fiş toplamından bir kuruş sapabilir.
    /// Sıralı doldurmada yalnız çıkarma vardır; <c>Σ pay = toplam</c> TANIM GEREĞİ
    /// sağlanır. Nakit ilk kaleme yazılır — keyfîdir ama belirlenimcidir ve
    /// kümülatif toplamı etkilemez (kapı tüm satırları toplar).
    /// </summary>
    /// <param name="kalemTutarlari">Kalemlerin TL tutarları, fiş sırasıyla.</param>
    public static List<(decimal Nakit, decimal Veresiye)> PaylariDagit(
        IReadOnlyList<decimal> kalemTutarlari, decimal nakitToplam, decimal veresiyeToplam)
    {
        var kalanNakit = nakitToplam;
        var kalanVeresiye = veresiyeToplam;
        var paylar = new List<(decimal, decimal)>(kalemTutarlari.Count);

        foreach (var tutar in kalemTutarlari)
        {
            var nakit = Math.Min(kalanNakit, tutar);
            kalanNakit -= nakit;

            var veresiye = Math.Min(kalanVeresiye, tutar - nakit);
            kalanVeresiye -= veresiye;

            paylar.Add((nakit, veresiye));
        }

        // Bacak toplamı fiş toplamına EŞİT olduğu için buraya artık kalmaz.
        // Kalırsa bir değişmez kırılmıştır; sessiz geçilmez.
        if (kalanNakit != 0m || kalanVeresiye != 0m)
            throw new InvalidOperationException(
                $"Pay dağıtımı kapanmadı: kalan nakit {kalanNakit}, kalan veresiye {kalanVeresiye}.");

        return paylar;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// P-23 · ODEME_KALEMSIZ_YOL — TUTAR KAPISI (TEK KAYNAK)
//
// KÖK NEDEN (ölçüldü, 30.09.2026): tek etiketli yollar (satış, alış, hurda,
// iade, takas) hareketi bir ödeme aracıyla (NAKIT/KART/HAVALE/VERESIYE)
// ETİKETLİYOR; kalem fabrikası (Data/OdemeKalemFabrikasi.Turet) ise yalnız
// |TlTutari| > 0 iken kalem doğuruyor. BirimFiyatTl [Range(0,…)] 0'ı kabul
// ettiği için 0,00 TL "KART satışı" yazılıyor, kalemi doğmuyor ve denetim.sh
// P-23 ODEME_KALEMSIZ_YOL (canlı) kırmızıya dönüyordu (01M3QG1P6EVK9HM5CVRTCBKWXJ).
//
// KURAL (denetim.sh P-23 sayacının koddaki karşılığı):
//   • Ödeme etiketi taşıyacak işlemin tutarı 0 olamaz → 400 + Türkçe sebep.
//   • Para el DEĞİŞTİRMEYEN meşru işlem (fark 0 takas, bedelsiz — hediye —
//     malın iadesi, bedelsiz mal girişi = 0 TL alış) ödeme ETİKETİ TAŞIMAZ: etiket null yazılır; kalem
//     beklenmez, kasa/POS/cari satırı yazılmaz. Etiketli-ama-tutarsız satır
//     böylece hiçbir yoldan doğamaz.
// ═════════════════════════════════════════════════════════════════════════════
/// <summary>Ödeme etiketli işlemlerin tutar kapısı (P-23 · tek kaynak).</summary>
public static class OdemeTutarKapisi
{
    /// <summary>Tutar 2 haneye yuvarlanınca 0 ise para el değiştirmiyordur.</summary>
    public static bool ParaYok(decimal tutarTl) =>
        Math.Round(Math.Abs(tutarTl), 2, MidpointRounding.AwayFromZero) == 0m;

    /// <summary>
    /// Ödemeli işlemin tutarı 0 ise 400. <paramref name="islemAdi"/> cümlenin
    /// öznesidir ("Satış", "Alış"…); <paramref name="neYapilir"/> kullanıcıya
    /// düzeltme yolunu söyler (UX-S30).
    /// </summary>
    public static void SifirOlamaz(decimal tutarTl, string islemAdi, string neYapilir, string alan)
    {
        if (ParaYok(tutarTl))
            throw new BadRequestException(
                $"{islemAdi} tutarı 0 TL olamaz: ödemesi olmayan işlem kaydedilmez. {neYapilir}")
                .AlanIle(alan);
    }

    /// <summary>Tek etiketli satışın ödeme türü kapalı listeden olmalı (servis içi, DTO'ya güvenilmez).</summary>
    public static void EtiketGecerli(string? odemeTuru, string[] izinli)
    {
        if (odemeTuru is null || Array.IndexOf(izinli, odemeTuru) < 0)
            throw new BadRequestException(
                $"Ödeme türü seçilmedi ya da geçersiz: {string.Join(", ", izinli)} seçeneklerinden biri olmalıdır.")
                .AlanIle("odemeTuru");
    }

    /// <summary>Para el değiştirmiyorsa etiket YOK (null); aksi hâlde verilen etiket.</summary>
    public static string? Etiket(decimal tutarTl, string? etiket) => ParaYok(tutarTl) ? null : etiket;
}
