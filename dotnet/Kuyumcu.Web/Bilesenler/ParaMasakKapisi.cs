using System.Text.RegularExpressions;
using Kuyumcu.Contracts.Bilesenler.MasakFormu;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Web.Bilesenler;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-KAPI-2 — SATIŞ DIŞI PARA FORMLARININ ORTAK MASAK KAPISI (tek kural, tek bileşen).
//
// Tasarım hakemi KRİTİK (10.10): /cariler Tahsilat çekmecesinde 250.000 ₺ nakit → sunucu MASAK 400
// ("MASAK beyan açıklaması (masak.aciklama) en az 20 karakter …"); çekmecede kimlik/amaç alanı YOKTU,
// tezgâhtar çıkışsız kalıyordu. Tutar 25.000'e inince eski hata kutusu da yerinde duruyordu.
//
// SINIF: sunucuda MASAK kapısına tabi her para formu (cari tahsilat, veresiye, döviz tahsilat/veresiye,
// siparişten satış …) aynı üç kuralı taşır:
//   (a) Eşik üstünde ortak MasakFormu açılır (kimlik + amaç; alanları SUNUCU söyler — /masak/gereksinim),
//       Kaydet kilitlenir ve sebebini yazar (UX-S30). Eşik SAYISI istemciye gömülmez (/masak/esikler).
//   (b) Tutar ya da müşteri değişince kapı YENİDEN değerlendirilir; sunucu reddi YasalKapiIzi deseniyle
//       tutulur — tutar reddin ölçüsünün altına inerse ya da müşteri değişirse form ve ret metni kalkar,
//       tutar geri çıkınca geri gelir. Girilen beyan (Model) silinmez; gövdeye yalnız kapı açıkken gider.
//   (c) Sunucu reddinde ham "masak.aciklama" metni BASILMAZ: ret alanları açar; form zaten doluysa
//       sunucu cümlesi alan anahtarlarından arındırılarak tek cümle yazılır.
// Bağlantılı işlem toplamı yalnız sunucuda bilinir: müşteri biliniyorsa eşik altında da sorulur
// (Banka dekontu ile aynı kural); karar sunucunun /masak/gereksinim yanıtıdır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir para formunun MASAK kapı durumu (form başına bir örnek; formu açan Ac çağırır).</summary>
public sealed class ParaMasakKapisi(
    Func<Task<Esikler?>> esikSor,
    Func<decimal, string?, Task<MasakGereksinim>> gereksinimSor)
{
    /// <summary>Sayfanın API istemcisiyle kurulur (Hurda/Banka/Kapora ile AYNI uçlar).</summary>
    public static ParaMasakKapisi Olustur(ApiIstemci api) => new(
        async () => await api.GetAsync<Esikler>("/masak/esikler") is { KimlikEsigi: > 0m } e ? e : null,
        (t, c) => new UyumServisi(api).Gereksinim(t, c)) { SoruGecikmesi = TimeSpan.FromMilliseconds(250) };

    /// <summary>Formdaki kimlik/beyan bilgisi (kişisel veri: form açılınca sıfırlanır).</summary>
    public MasakFormu.MasakBilgi Model { get; private set; } = new();
    public Esikler? Esik { get; private set; }
    public MasakGereksinim? Gereksinim { get; private set; }
    /// <summary>Eşik sorusu başarısız oldu (null eşik = bilinmiyor; karar sunucuda kalır).</summary>
    public bool EsikAlinamadi { get; private set; }
    /// <summary>Gereksinim sorusu başarısız oldu (fail-closed: eşik üstünde kilit kalkmaz).</summary>
    public bool SorguHatasi { get; private set; }
    /// <summary>Kapıya giden ölçü (TL, mutlak).</summary>
    public decimal Tutar { get; private set; }
    public string? CariId { get; private set; }
    /// <summary>Tur 2 (doğrulayıcı DÜŞÜK: her tuş vuruşunda /masak/gereksinim): soru bu kadar bekler; bu sürede tutar yine
    /// değişirse eski soru HİÇ gönderilmez (nesil). Beklerken Gereksinim null → kilit "hazırlanıyor" (fail-closed).</summary>
    public TimeSpan SoruGecikmesi { get; init; }

    private YasalKapiIzi? redIzi;
    private string? redMetni;
    private decimal sorulanTutar = -1m;
    private string? sorulanCari;
    private int nesil;

    /// <summary>Form açılırken: önceki formun beyanı ve izi kalmaz; eşik bir kez alınır.</summary>
    public async Task Ac(string? cariId)
    {
        Sifirla();
        CariId = Bos(cariId);
        await EsikYukle();
    }

    public void Sifirla()
    {
        Model = new();
        redIzi = null; redMetni = null;
        Gereksinim = null; SorguHatasi = false;
        sorulanTutar = -1m; sorulanCari = null;
        Tutar = 0m; CariId = null;
        nesil++;
    }

    private async Task EsikYukle()
    {
        if (Esik is not null) return;
        try { Esik = await esikSor(); }
        catch (ApiHata) { Esik = null; }
        catch (System.Text.Json.JsonException) { Esik = null; }
        EsikAlinamadi = Esik is null;
    }

    /// <summary>Sunucu reddinin izi hâlâ geçerli mi (aynı müşteri, tutar reddin ölçüsünden aşağı inmedi).</summary>
    public bool RedGecerli => YasalKapiIzi.Gecerli(redIzi, Tutar, CariId);

    /// <summary>Tutar sunucudan gelen "kimliği isteyen en düşük tutar"a ulaştı mı.</summary>
    public bool EsikUstu => Esik is not null && Tutar > 0m && Tutar >= Esik.KimlikIstenenTutar();

    /// <summary>Kimlik alanları gerekli mi: eşik üstü, ya da sunucu (bugünkü toplamla) istiyor, ya da reddetti.</summary>
    public bool Gerekli => Tutar > 0m && (EsikUstu || RedGecerli || Gereksinim?.KimlikGerekli == true);

    /// <summary>MasakFormu çizilebilir mi (alanları sunucunun gereksinimi söyler).</summary>
    public bool FormCizilir => Gerekli && Esik is not null && Gereksinim is { KimlikGerekli: true };

    /// <summary>POST gövdesinin <c>masak</c> alanı: yalnız kapı açıkken (eşik altında beyan gövdeye gitmez).</summary>
    public MasakFormu.MasakBilgi? Govde => Gerekli ? Model : null;

    /// <summary>Sunucu reddinin ekrandaki tek cümlesi — iz geçersizleşince (tutar indi / müşteri değişti) kalkar.</summary>
    public string? RedBandi => RedGecerli ? redMetni : null;

    /// <summary>UX-S30: Kaydet kilitliyse sebebi (null = kapı kilitlemiyor).</summary>
    public string? KilitSebebi(string dil)
    {
        if (!Gerekli) return null;
        var m = YasalKapi2Sozlugu.Al(dil);
        if (Esik is null) return m.MasakEsikYok;
        if (Gereksinim is null) return SorguHatasi ? m.MasakSorguHata : m.MasakHazirlaniyor;
        if (!Gereksinim.KimlikGerekli) return null;
        if (MasakFormu.Eksiksiz(Model, Gereksinim)) return null;
        return MasakFormu.TcknKilitMetni(Model, dil) ?? ZoIslemSozlugu.Al(dil).MasakKilitSebepBasit;
    }

    /// <summary>
    /// Tutar ya da müşteri değişince (ve form açılışında) çağrılır. Gereksinim sunucudan sorulur:
    /// tutar eşikteyse, müşteri biliniyorsa (bağlantılı toplam) ya da ret izi geçerliyse. Aynı tutar/müşteri
    /// için tekrar sorulmaz; geç gelen eski yanıt yeni tutarın üstüne yazılmaz. Dönüş: ekran yeniden çizilmeli mi.
    /// </summary>
    public async Task<bool> Tazele(decimal tutarTl, string? cariId)
    {
        var t = Math.Abs(tutarTl);
        var c = Bos(cariId);
        var degisti = t != Tutar || c != CariId;
        Tutar = t; CariId = c;
        var sorulmali = t > 0m && Esik is not null && (c is not null || EsikUstu || RedGecerli);
        if (!sorulmali)
        {
            var vardi = Gereksinim is not null || sorulanTutar >= 0m;
            Gereksinim = null; SorguHatasi = false; sorulanTutar = -1m; sorulanCari = null; nesil++;
            return degisti || vardi;
        }
        if (t == sorulanTutar && c == sorulanCari) return degisti;
        // Eski tutarın gereksinimi yenisinde geçerli değildir: sorudan ÖNCE düşer.
        Gereksinim = null; SorguHatasi = false; sorulanTutar = t; sorulanCari = c;
        var n = ++nesil;
        if (SoruGecikmesi > TimeSpan.Zero)
        {
            await Task.Delay(SoruGecikmesi);
            if (n != nesil) return true;   // arada yeni tutar yazıldı: bu soru gönderilmez
        }
        try
        {
            var g = await gereksinimSor(t, c);
            if (n == nesil) Gereksinim = g;
        }
        catch (ApiHata) { if (n == nesil) SorguHatasi = true; }
        catch (System.Text.Json.JsonException) { if (n == nesil) SorguHatasi = true; }
        return true;
    }

    /// <summary>
    /// Sunucu reddi MASAK kaynaklı mı? Kaynaklıysa iz alınır, alanlar açılır ve ekranın tek cümlesi hazırlanır;
    /// dönüş true (çağıran ham metni BASMAZ). Değilse false (çağıranın kendi hata yolu).
    /// </summary>
    public async Task<bool> Reddet(ApiHata ex, string dil)
    {
        if (!MasakReddi(ex)) return false;
        var tamamdi = Gereksinim is { KimlikGerekli: true } g && MasakFormu.Eksiksiz(Model, g);
        redIzi = YasalKapiIzi.Al(Tutar, CariId);
        await EsikYukle();   // açılışta alınamadıysa yeniden dene
        sorulanTutar = -1m;  // ret sonrası gereksinim yeniden sorulur (bugünkü toplam değişmiş olabilir)
        await Tazele(Tutar, CariId);
        var m = YasalKapi2Sozlugu.Al(dil);
        redMetni = Esik is null ? m.MasakEsikYok
            : tamamdi || !FormCizilir ? string.Format(m.MasakKabulEdilmedi, SadeMetin(MasakOneksiz(ex.Message)))
            : null;   // alanlar açıldı: neyin eksik olduğunu kilit sebebi söyler (tek yer)
        return true;
    }

    /// <summary>Sunucunun reddi MASAK alanlarını mı istiyor (Hurda / Kapora / Satış ile aynı ölçüt).</summary>
    public static bool MasakReddi(ApiHata e) =>
        e.StatusCode == 400
        && (e.Message.StartsWith("MASAK", StringComparison.OrdinalIgnoreCase)
            || e.Alanlar.Keys.Any(k => k.StartsWith("masak", StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Sunucu cümlesinden ham alan anahtarlarını ("(masak.aciklama)") ve içe gömülü ham MASAK reddini
    /// ("… (MASAK: … (masak.aciklama) …)") çıkarır; kalan cümle aynen gösterilir (Anayasa md.1: yutulmaz).
    /// </summary>
    public static string SadeMetin(string? mesaj)
    {
        var m = mesaj ?? "";
        m = IcMasak.Replace(m, "");
        m = AlanAnahtari.Replace(m, "");
        return Regex.Replace(m, @"\s{2,}", " ").Trim();
    }

    private static string MasakOneksiz(string m) =>
        m.StartsWith("MASAK:", StringComparison.OrdinalIgnoreCase) ? m[6..].TrimStart() : m;

    private static readonly Regex AlanAnahtari = new(@"\s*\((?:masak|hurda|altin|odeme)\.[\w.\[\]]+\)", RegexOptions.Compiled);
    private static readonly Regex IcMasak = new(@"\s*\(MASAK:(?:[^()]|\([^()]*\))*\)", RegexOptions.Compiled);

    private static string? Bos(string? c) => string.IsNullOrEmpty(c) ? null : c;
}
