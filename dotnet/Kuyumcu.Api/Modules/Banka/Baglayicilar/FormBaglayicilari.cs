using System.Text;
using System.Xml.Linq;
using static Kuyumcu.Api.Modules.Banka.Baglayicilar.Xml;

namespace Kuyumcu.Api.Modules.Banka.Baglayicilar;

/// <summary>
/// Akbank — Extre_InterfaceService.GetExtreWithParams (ASMX SOAP 1.1, HTTP Basic, IP kısıtlı). § 4.
/// Tarih 20 hane (yyyyMMddHHmmss + 6 sıfır), günü gününe çekilir; ondalık virgül; yön "+"/"-".
/// </summary>
public sealed class AkbankBaglayicisi : IBankaBaglayicisi
{
    public const string KodSabit = "AKBANK";
    public const string Adres = "https://firmahizmetleri.akbank.com/Extre_InterfaceService/Service.asmx";
    private static readonly string Ad = Kuyumcu.Core.Domain.BankaBaglayiciKatalogu.Bul(KodSabit)!.BankaAdi;
    public string Kod => KodSabit;

    public async Task<IReadOnlyList<SaglayiciHareketi>> Cek(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal)
    {
        var k = istek.Kimlik;
        var sonuc = new List<SaglayiciHareketi>();
        foreach (var (bas, bit) in Tarih.Dilimle(Tarih.Yerel(istek.Baslangic), Tarih.Yerel(istek.Bitis), 1))
        {
            using var m = Http.Soap11(Adres, "http://tempuri.org/GetExtreWithParams",
                Zarf(Giris.Istege(k, "urf") ?? "", Giris.Zorunlu(k, "hesapNo"), bas, bit));
            Http.Basic(m, Giris.Zorunlu(k, "kullanici"), Giris.Zorunlu(k, "sifre"));
            sonuc.AddRange(Ayristir(await Http.Gonder(http, m, Ad, iptal), istek.Iban));
        }
        return sonuc;
    }

    public static string Zarf(string urf, string hesapNo, DateTime bas, DateTime bit) =>
        $"""<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><GetExtreWithParams xmlns="http://tempuri.org/"><urf>{Kac(urf)}</urf><hesapNo>{Kac(hesapNo)}</hesapNo><dovizKodu></dovizKodu><subeKodu></subeKodu><baslangicTarihi>{bas:yyyyMMddHHmmss}000000</baslangicTarihi><bitisTarihi>{bit:yyyyMMddHHmmss}000000</bitisTarihi></GetExtreWithParams></soap:Body></soap:Envelope>""";

    public static List<SaglayiciHareketi> Ayristir(string xml, string iban)
    {
        var d = Oku(xml);
        var liste = new List<SaglayiciHareketi>();
        foreach (var hesap in Hepsi(d, "Hesap"))
        {
            var hIban = Al(hesap, "IBAN");
            if (hIban is not null && !string.Equals(hIban.Replace(" ", ""), iban, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var h in hesap.Elements().Where(e => e.Name.LocalName == "Detay"))
            {
                var gun = Tarih.Oku(Al(h, "IslemTarihi")?[..Math.Min(8, Al(h, "IslemTarihi")!.Length)], "yyyyMMdd");
                // Saat: TimeStamp[8..14] (yyyyMMddHHmmss…).
                var ts = Al(h, "TimeStamp");
                var tarih = ts is { Length: >= 14 } ? Tarih.Oku(ts[..14], "yyyyMMddHHmmss") ?? gun : gun;
                var tutar = Sayi.Oku(Al(h, "Tutar"));
                var ba = Al(h, "TutarBorcAlacak");
                if (tarih is null || tutar is null) continue;
                var gelen = ba is not null ? ba == "+" || ba == "A" : tutar > 0;
                var aciklama = string.Join(" ", new[] { Al(h, "Aciklama"), gelen ? Al(h, "DekontAlacakAciklama") : Al(h, "DekontBorcAciklama") }
                    .Where(x => x is not null).Distinct());
                var t = HareketTemizleyici.Temizle(KodSabit, Al(h, "FisNo") is { } fn ? $"{fn}-{Al(h, "ReferansNo")}" : Al(h, "ReferansNo"),
                    tarih.Value, gelen, tutar.Value, null,
                    gelen ? Al(h, "BorcluIBAN") ?? Al(h, "KarsiHesapIBAN") : Al(h, "AlacakliIBAN") ?? Al(h, "KarsiHesapIBAN"),
                    gelen ? Al(h, "AmirTCKN") ?? Al(h, "AmirVKN") ?? Al(h, "VKN") : Al(h, "LehdarTCKN") ?? Al(h, "LehdarVKN") ?? Al(h, "VKN"),
                    aciklama, yedekAnahtar: ts);
                if (t is not null) liste.Add(t);
            }
        }
        return liste;
    }
}

/// <summary>
/// Ziraat — HEK_NKYWS SorgulaHesapHareketZamanIle (ASMX HTTP POST form). § 7. KISMEN: SOAP ad alanı
/// TEYİTSİZ olduğundan ASMX'in form yolu kullanılır (topluluk kodu aynı serviste HesapEkstre'yi böyle çağırıyor);
/// yanıt alan adları harf duyarsız okunur. Aralık en çok 21 gün (topluluk yorumu) → 1 günlük dilim.
/// </summary>
public sealed class ZiraatBaglayicisi : IBankaBaglayicisi
{
    public const string KodSabit = "ZIRAAT";
    public const string Adres = "https://hesap.ziraatbank.com.tr/HEK_NKYWS/HesapHareketleri.asmx/SorgulaHesapHareketZamanIle";
    private static readonly string Ad = Kuyumcu.Core.Domain.BankaBaglayiciKatalogu.Bul(KodSabit)!.BankaAdi;
    public string Kod => KodSabit;

    public async Task<IReadOnlyList<SaglayiciHareketi>> Cek(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal)
    {
        var k = istek.Kimlik;
        var sonuc = new List<SaglayiciHareketi>();
        foreach (var (bas, bit) in Tarih.Dilimle(Tarih.Yerel(istek.Baslangic), Tarih.Yerel(istek.Bitis), 1))
        {
            using var m = new HttpRequestMessage(HttpMethod.Post, Adres) { Content = new FormUrlEncodedContent(Form(k, bas, bit)) };
            sonuc.AddRange(Ayristir(await Http.Gonder(http, m, Ad, iptal)));
        }
        return sonuc;
    }

    public static Dictionary<string, string> Form(IReadOnlyDictionary<string, string?> k, DateTime bas, DateTime bit) => new()
    {
        ["musteriNo"] = Giris.Zorunlu(k, "musteriNo"), ["ekNo"] = Giris.Zorunlu(k, "ekNo"),
        ["baslangicZamani"] = bas.ToString("yyyy-MM-ddTHH:mm:ss"), ["bitisZamani"] = bit.ToString("yyyy-MM-ddTHH:mm:ss"),
        ["kurumKod"] = Giris.Zorunlu(k, "kullanici"), ["sifre"] = Giris.Zorunlu(k, "sifre"), ["iptalFisGetir"] = "H",
    };

    public static List<SaglayiciHareketi> Ayristir(string xml)
    {
        var d = Oku(xml);
        var kok = d.Root!;
        var hk = Al(kok, "HataKodu", "CevapKodu", "hataKodu");
        if (hk is not null && hk.Trim('0').Length > 0)
            throw new BankaBaglantiHatasi($"{Ad}: {Al(kok, "HataAck", "CevapAciklama", "hataAciklamasi") ?? "istek reddedildi"} ({hk}).");
        var liste = new List<SaglayiciHareketi>();
        foreach (var h in Hepsi(d, "HareketlerDetay"))
        {
            if (Al(h, "KayitDurumu") is "I" or "IPTAL" || Al(h, "IptalZamani") is not null) continue;   // iptal fişi
            var tarih = Tarih.Oku(Al(h, "IslemTarihi"), "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.FFFFFFF", "dd.MM.yyyy HH:mm:ss", "yyMMdd", "dd.MM.yyyy");
            var tutar = Sayi.Oku(Al(h, "Tutar"));
            var ba = Al(h, "BorcAlacak");
            if (tarih is null || tutar is null) continue;
            var gelen = ba is not null ? ba == "A" : tutar > 0;
            var t = HareketTemizleyici.Temizle(KodSabit, Al(h, "DekontNo") ?? Al(h, "TimeStamp") ?? Al(h, "MuhRef"), tarih.Value, gelen, tutar.Value,
                Al(h, "AdUnvan"), Al(h, "IBAN"), Al(h, "TcknVkn"), Al(h, "Aciklama") ?? Al(h, "IslemAciklama"));
            if (t is not null) liste.Add(t);
        }
        return liste;
    }
}

/// <summary>
/// İş Bankası — Posmatik online XML ekstre (HTTP POST form, ISO-8859-9). § 8. KISMEN: resmi belge yok;
/// tarihli uç (AuthenticateSpecific.aspx). Kullanıcıya tanımlı TÜM hesaplar döner → IBAN ile süzülür.
/// </summary>
public sealed class IsBankasiBaglayicisi : IBankaBaglayicisi
{
    public const string KodSabit = "ISBANK";
    public const string Adres = "https://posmatik2.isbank.com.tr/AuthenticateSpecific.aspx";
    private static readonly string Ad = Kuyumcu.Core.Domain.BankaBaglayiciKatalogu.Bul(KodSabit)!.BankaAdi;
    public string Kod => KodSabit;

    static IsBankasiBaglayicisi() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public async Task<IReadOnlyList<SaglayiciHareketi>> Cek(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal)
    {
        var k = istek.Kimlik;
        var sonuc = new List<SaglayiciHareketi>();
        foreach (var (bas, bit) in Tarih.Dilimle(Tarih.Yerel(istek.Baslangic), Tarih.Yerel(istek.Bitis), 1))
        {
            using var m = new HttpRequestMessage(HttpMethod.Post, Adres)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["uid"] = Giris.Zorunlu(k, "kullanici"), ["pwd"] = Giris.Zorunlu(k, "sifre"),
                    ["BeginDate"] = bas.ToString("dd.MM.yyyy HH:mm:ss"), ["EndDate"] = bit.ToString("dd.MM.yyyy HH:mm:ss"),
                }),
            };
            sonuc.AddRange(Ayristir(await Http.Gonder(http, m, Ad, iptal, Encoding.GetEncoding("ISO-8859-9")), istek.Iban));
        }
        return sonuc;
    }

    public static List<SaglayiciHareketi> Ayristir(string xml, string iban)
    {
        if (!xml.TrimStart().StartsWith('<')) throw new BankaBaglantiHatasi($"{Ad} servis kullanıcı adını ya da şifresini kabul etmedi.");
        var d = Oku(xml);
        var liste = new List<SaglayiciHareketi>();
        foreach (var hesap in Hepsi(d, "Hesap"))
        {
            var tanim = hesap.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, "Tanimlamalar", StringComparison.OrdinalIgnoreCase));
            var hIban = tanim is null ? null : Al(tanim, "IBANNo");
            if (hIban is not null && !string.Equals(hIban.Replace(" ", ""), iban, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var h in Hepsi(hesap, "Hareket"))
            {
                var gun = Tarih.Oku(Al(h, "Tarih"), "dd.MM.yyyy", "dd/MM/yyyy", "yyyy-MM-dd");
                var saat = Al(h, "Saat") is { } s && TimeSpan.TryParse(s, out var ts) ? ts : TimeSpan.Zero;
                var tutar = Sayi.Oku(Al(h, "Miktar"));
                var ba = Al(h, "borcAlacak");
                if (gun is null || tutar is null) continue;
                var gelen = ba is not null ? ba == "A" : tutar > 0;
                var t = HareketTemizleyici.Temizle(KodSabit, Al(h, "HareketSirano") ?? Al(h, "DekontNo") ?? Al(h, "isl_id"),
                    gun.Value.Date + saat, gelen, tutar.Value, null, Al(h, "LehdarhIBAN", "LehdarIBAN"), Al(h, "KarsiHesapVKN"),
                    string.Join(" ", new[] { Al(h, "Aciklama"), Al(h, "MusteriAciklama") }.Where(x => x is not null)));
                if (t is not null) liste.Add(t);
            }
        }
        return liste;
    }
}
