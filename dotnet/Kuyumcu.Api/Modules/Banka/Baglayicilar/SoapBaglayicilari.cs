using System.Globalization;
using System.Text;
using System.Xml.Linq;
using static Kuyumcu.Api.Modules.Banka.Baglayicilar.Xml;

namespace Kuyumcu.Api.Modules.Banka.Baglayicilar;

// ═════════════════════════════════════════════════════════════════════════════
// D2-05b — SOAP/XML bankaları. Zarflar ve alan adları ARASTIRMA-BANKA-API-TEKNIK-2026-10-19.md'den
// BİREBİR (bölüm numarası her sınıfta). Ayrıştırıcılar saf fonksiyondur, belgedeki örneklerle test edilir.
// ⚠ Hiçbiri bankanın canlı servisinde denenmedi (pilot işletmenin servis kullanıcısıyla doğrulanacak).
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>VakıfBank — SOnlineEkstreServis.GetirHareket (SOAP 1.2 + WS-Addressing). § 2.</summary>
public sealed class VakifbankBaglayicisi : IBankaBaglayicisi
{
    public const string KodSabit = "VAKIF";
    public const string Adres = "https://vbservice.vakifbank.com.tr/HesapHareketleri.OnlineEkstre/SOnlineEkstreServis.svc";
    private static readonly string Ad = Kuyumcu.Core.Domain.BankaBaglayiciKatalogu.Bul(KodSabit)!.BankaAdi;
    public string Kod => KodSabit;

    public async Task<IReadOnlyList<SaglayiciHareketi>> Cek(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal)
    {
        var k = istek.Kimlik;
        var sonuc = new List<SaglayiciHareketi>();
        foreach (var (bas, bit) in Tarih.Dilimle(Tarih.Yerel(istek.Baslangic), Tarih.Yerel(istek.Bitis), 1))
        {
            using var m = new HttpRequestMessage(HttpMethod.Post, Adres)
            {
                Content = new StringContent(Zarf(Giris.Zorunlu(k, "musteriNo"), Giris.Zorunlu(k, "kullanici"), Giris.Zorunlu(k, "sifre"),
                    Giris.Zorunlu(k, "hesapNo"), bas, bit), Encoding.UTF8, "application/soap+xml"),
            };
            sonuc.AddRange(Ayristir(await Http.Gonder(http, m, Ad, iptal), istek.Iban));
        }
        return sonuc;
    }

    public static string Zarf(string musteriNo, string kullanici, string sifre, string hesapNo, DateTime bas, DateTime bit) =>
        $"""<soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope" xmlns:peak="Peak.Integration.ExternalInbound.Ekstre" xmlns:peak1="http://schemas.datacontract.org/2004/07/Peak.Integration.ExternalInbound.Ekstre.DataTransferObjects"><soap:Header xmlns:wsa="http://www.w3.org/2005/08/addressing"><wsa:Action>Peak.Integration.ExternalInbound.Ekstre/ISOnlineEkstreServis/GetirHareket</wsa:Action><wsa:To>{Adres}</wsa:To></soap:Header><soap:Body><peak:GetirHareket><peak:sorgu><peak1:MusteriNo>{Kac(musteriNo)}</peak1:MusteriNo><peak1:KurumKullanici>{Kac(kullanici)}</peak1:KurumKullanici><peak1:Sifre>{Kac(sifre)}</peak1:Sifre><peak1:SorguBaslangicTarihi>{bas:yyyy-MM-dd HH:mm}</peak1:SorguBaslangicTarihi><peak1:SorguBitisTarihi>{bit:yyyy-MM-dd HH:mm}</peak1:SorguBitisTarihi><peak1:HesapNo>{Kac(hesapNo)}</peak1:HesapNo><peak1:HareketTipi xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/></peak:sorgu></peak:GetirHareket></soap:Body></soap:Envelope>""";

    public static List<SaglayiciHareketi> Ayristir(string xml, string iban)
    {
        var d = Oku(xml);
        // Cevap düzeyindeki IslemKodu (hareketlerin de IslemKodu alanı var → yalnız sonuç elemanının çocuğu).
        var cevap = Ilk(d, "GetirHareketResult");
        var kod = cevap is null ? null : Al(cevap, "IslemKodu");
        if (kod is not null && kod != "VBB0001")
            throw new BankaBaglantiHatasi($"{Ad}: {Al(cevap!, "IslemAciklamasi") ?? "istek reddedildi"} ({kod}).");
        var liste = new List<SaglayiciHareketi>();
        foreach (var hesap in Hepsi(d, "DtoEkstreHesap"))
        {
            var hIban = Al(hesap, "HesapNoIban");
            if (hIban is not null && !string.Equals(hIban.Replace(" ", ""), iban, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var h in Hepsi(hesap, "DtoEkstreHareket"))
            {
                var tarih = Tarih.Oku(Al(h, "IslemTarihZamani", "IslemTarihi"), "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "dd.MM.yyyy HH:mm:ss", "yyyy-MM-dd");
                var tutar = Sayi.Oku(Al(h, "Tutar"));
                var ba = Al(h, "BorcAlacak");
                if (tarih is null || tutar is null) continue;
                var gelen = ba is not null ? ba == "A" : tutar > 0;
                var detay = Detaylar(h);
                detay.TryGetValue(gelen ? "GonderenIbanKumarasi" : "AliciIbanKumarasi", out var karsiIban);
                detay.TryGetValue(gelen ? "GonderenKimlikNumarasi" : "AliciKimlikNumarasi", out var karsiKimlik);
                detay.TryGetValue(gelen ? "GonderenAdi" : "AliciAdi", out var karsiAd);
                var t = HareketTemizleyici.Temizle(KodSabit, Al(h, "Id") ?? Al(h, "IslemNo"), tarih.Value, gelen, tutar.Value,
                    karsiAd, karsiIban, karsiKimlik, string.Join(" ", new[] { Al(h, "IslemAdi"), Al(h, "Aciklama") }.Where(x => x is not null)));
                if (t is not null) liste.Add(t);
            }
        }
        return liste;
    }

    /// <summary>"Detaylar" anahtar–değer listesi (KeyValueOfstringstring: Key/Value).</summary>
    private static Dictionary<string, string> Detaylar(XElement h)
    {
        var s = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var d = h.Elements().FirstOrDefault(e => e.Name.LocalName == "Detaylar");
        if (d is null) return s;
        foreach (var kv in d.Elements())
            if (Al(kv, "Key") is { } a && Al(kv, "Value") is { } v) s[a] = v;
        return s;
    }
}

/// <summary>QNB — Maestro TeknikBaglantiService.getTransactionInfo (SOAP 1.1). § 3.</summary>
public sealed class QnbBaglayicisi : IBankaBaglayicisi
{
    public const string KodSabit = "QNB";
    public const string Adres = "https://fbmaestro.qnb.com.tr/MaestroCoreEkstre/services/TeknikBaglantiService.TeknikBaglantiServiceHttpSoap11Endpoint/";
    private static readonly string Ad = Kuyumcu.Core.Domain.BankaBaglayiciKatalogu.Bul(KodSabit)!.BankaAdi;
    public string Kod => KodSabit;

    public async Task<IReadOnlyList<SaglayiciHareketi>> Cek(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal)
    {
        var k = istek.Kimlik;
        var sonuc = new List<SaglayiciHareketi>();
        foreach (var (bas, bit) in Tarih.Dilimle(Tarih.Yerel(istek.Baslangic), Tarih.Yerel(istek.Bitis), 1))
        {
            using var m = Http.Soap11(Adres, "urn:getTransactionInfo",
                Zarf(Giris.Zorunlu(k, "kullanici"), Giris.Zorunlu(k, "sifre"), Giris.Zorunlu(k, "hesapNo"), istek.Iban, bas, bit));
            sonuc.AddRange(Ayristir(await Http.Gonder(http, m, Ad, iptal), istek.Iban));
        }
        return sonuc;
    }

    // WSDL sırası (alfabetik): password, transactionInfoInputType{accountNo, endDate, iban, startDate}, userName.
    public static string Zarf(string kullanici, string sifre, string hesapNo, string iban, DateTime bas, DateTime bit) =>
        $"""<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ns="http://teknikbaglantiekstre.genericekstrenew2.driver.maestro.ibtech.com"><soapenv:Header/><soapenv:Body><ns:getTransactionInfo><transactionInfo><password>{Kac(sifre)}</password><transactionInfoInputType><accountNo>{Kac(hesapNo)}</accountNo><endDate>{bit:yyyy-MM-ddTHH:mm:ss}</endDate><iban>{Kac(iban)}</iban><startDate>{bas:yyyy-MM-ddTHH:mm:ss}</startDate></transactionInfoInputType><userName>{Kac(kullanici)}</userName></transactionInfo></ns:getTransactionInfo></soapenv:Body></soapenv:Envelope>""";

    public static List<SaglayiciHareketi> Ayristir(string xml, string iban)
    {
        var d = Oku(xml);
        var donus = Ilk(d, "return");
        if (donus is not null && Al(donus, "errorCode") is { } hk && hk != "0")
            throw new BankaBaglantiHatasi($"{Ad}: {Al(donus, "errorDescription") ?? "istek reddedildi"} ({hk}).");
        var liste = new List<SaglayiciHareketi>();
        // Yerel ad TEYİTSİZ (Reference.cs "accountInfoReturnType", SAP "accountInfos") → ikisi de.
        var hesaplar = Hepsi(d, "accountInfoReturnType").Concat(Hepsi(d, "accountInfos")).ToList();
        foreach (var hesap in hesaplar)
        {
            var hIban = Al(hesap, "iban");
            if (hIban is not null && !string.Equals(hIban.Replace(" ", ""), iban, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var h in hesap.Elements().Where(e => e.Name.LocalName == "transactions"))
            {
                var tarih = Tarih.Oku(Al(h, "transactionDate"), "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fffK", "yyyy-MM-dd");
                var tutar = Sayi.Oku(Al(h, "transactionAmount"));
                var ba = Al(h, "debitOrCreditCode");
                if (tarih is null || tutar is null || ba is not ("A" or "B")) continue;
                // Yön: A = alacak (hesaba giriş). SAP'ın ters çevirmesi Mihenk için TEYİTSİZ → pilotla doğrulanacak.
                var t = HareketTemizleyici.Temizle(KodSabit, Al(h, "transactionId") ?? Al(h, "muhasebeFisNo"), tarih.Value, ba == "A", tutar.Value,
                    null, Al(h, "opponentIBAN"), Al(h, "opponentTAXNoPIDNo"),
                    Al(h, "transactionDescription") ?? Al(h, "eftReturnDescription") ?? Al(h, "vheTrxDescription"));
                if (t is not null) liste.Add(t);
            }
        }
        return liste;
    }
}

/// <summary>Kuveyt Türk — BOA AccountStatementService.GetAccountStatement (SOAP 1.1). § 5.</summary>
public sealed class KuveytTurkBaglayicisi : IBankaBaglayicisi
{
    public const string KodSabit = "KUVEYT";
    public const string Adres = "https://boa.kuveytturk.com.tr/BOA.Integration.WCFService/BOA.Integration.AccountStatement/AccountStatementService.svc/Basic";
    private const string Eylem = "http://boa.net/BOA.Integration.CoreBanking.Teller/Service/IAccountStatementService/GetAccountStatement";
    private static readonly string Ad = Kuyumcu.Core.Domain.BankaBaglayiciKatalogu.Bul(KodSabit)!.BankaAdi;
    public string Kod => KodSabit;

    public async Task<IReadOnlyList<SaglayiciHareketi>> Cek(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal)
    {
        var k = istek.Kimlik;
        var (no, ek) = Giris.Bol(Giris.Zorunlu(k, "hesapNo"));
        var sonuc = new List<SaglayiciHareketi>();
        foreach (var (bas, bit) in Tarih.Dilimle(Tarih.Yerel(istek.Baslangic), Tarih.Yerel(istek.Bitis), 1))
        {
            using var m = Http.Soap11(Adres, Eylem, Zarf(Giris.Zorunlu(k, "kullanici"), Giris.Zorunlu(k, "sifre"), no, ek ?? "", istek.Iban, bas, bit));
            sonuc.AddRange(Ayristir(await Http.Gonder(http, m, Ad, iptal), istek.Iban));
        }
        return sonuc;
    }

    // DataContract alfabetik sırası: temel (ExtUName, ExtUPassword) sonra model (AccountNumber, AccountSuffix, BeginDate, EndDate, Iban).
    public static string Zarf(string kullanici, string sifre, string musteriNo, string ekNo, string iban, DateTime bas, DateTime bit) =>
        $"""<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ser="http://boa.net/BOA.Integration.CoreBanking.Teller/Service" xmlns:boa="http://schemas.datacontract.org/2004/07/BOA.Integration.Base" xmlns:boa1="http://schemas.datacontract.org/2004/07/BOA.Integration.Model.CoreBanking.Teller"><soapenv:Header/><soapenv:Body><ser:GetAccountStatement><ser:request><boa:ExtUName>{Kac(kullanici)}</boa:ExtUName><boa:ExtUPassword>{Kac(sifre)}</boa:ExtUPassword><boa1:AccountNumber>{Kac(musteriNo)}</boa1:AccountNumber><boa1:AccountSuffix>{Kac(ekNo)}</boa1:AccountSuffix><boa1:BeginDate>{bas:yyyy-MM-ddTHH:mm:ss}</boa1:BeginDate><boa1:EndDate>{bit:yyyy-MM-ddTHH:mm:ss}</boa1:EndDate><boa1:Iban>{Kac(iban)}</boa1:Iban></ser:request></ser:GetAccountStatement></soapenv:Body></soapenv:Envelope>""";

    public static List<SaglayiciHareketi> Ayristir(string xml, string iban)
    {
        var d = Oku(xml);
        var sonucE = Ilk(d, "GetAccountStatementResult");
        if (sonucE is not null && Al(sonucE, "Success") is "false")
            throw new BankaBaglantiHatasi($"{Ad}: {Al(sonucE, "ErrorMessage") ?? Hepsi(sonucE, "ErrorMessage").Select(e => e.Value).FirstOrDefault(v => v.Length > 0) ?? "istek reddedildi"}.");
        var liste = new List<SaglayiciHareketi>();
        foreach (var h in Hepsi(d, "TransactionDetailContract"))
        {
            var tarih = Tarih.Oku(Al(h, "SystemDate", "TranDate"), "yyyy-MM-ddTHH:mm:ss.FFFFFFF", "yyyy-MM-ddTHH:mm:ss");
            var alacak = Sayi.Oku(Al(h, "Credit")) ?? 0m;
            var borc = Sayi.Oku(Al(h, "Debit")) ?? 0m;
            if (tarih is null || (alacak == 0m && borc == 0m)) continue;
            var gelen = alacak > 0m;
            var t = HareketTemizleyici.Temizle(KodSabit, Al(h, "BusinessKey") ?? Al(h, "TranRef"), tarih.Value, gelen, gelen ? alacak : borc,
                gelen ? Al(h, "SenderName") : Al(h, "ReceiverName"),
                // Iban'ın anlamı TEYİTSİZ (karşı mı, kendi hesabımız mı): kendi IBAN'ımızsa karşı IBAN sayılmaz.
                Al(h, "Iban") is { } ki && !string.Equals(ki.Replace(" ", ""), iban, StringComparison.OrdinalIgnoreCase) ? ki : null,
                gelen ? Al(h, "SenderIdentityNumber") : Al(h, "ReceiverIdentityNumber"), Al(h, "Description"));
            if (t is not null) liste.Add(t);
        }
        return liste;
    }
}

/// <summary>Albaraka Türk — HesapBilgileriService.getHesapHareketleri (ASMX SOAP 1.1). § 6.</summary>
public sealed class AlbarakaBaglayicisi : IBankaBaglayicisi
{
    public const string KodSabit = "ALBARAKA";
    public const string Adres = "https://eservice.albarakaturk.com.tr:10214/invoiceincomingsite/HesapBilgileriService.asmx";
    private static readonly string Ad = Kuyumcu.Core.Domain.BankaBaglayiciKatalogu.Bul(KodSabit)!.BankaAdi;
    public string Kod => KodSabit;

    public async Task<IReadOnlyList<SaglayiciHareketi>> Cek(HttpClient http, BankaCekimIstegi istek, CancellationToken iptal)
    {
        var k = istek.Kimlik;
        var (_, ek) = Giris.Bol(Giris.Zorunlu(k, "hesapNo"));
        var hesapNo = ek ?? Giris.Zorunlu(k, "hesapNo");
        var sonuc = new List<SaglayiciHareketi>();
        foreach (var (bas, bit) in Tarih.Dilimle(Tarih.Yerel(istek.Baslangic).Date, Tarih.Yerel(istek.Bitis), 1))
        {
            // WSDL: SOAPAction boş; ad alanı services.albaraka.com (topluluk kodunun tempuri.org'u değil).
            using var m = Http.Soap11(Adres, "", Zarf(Giris.Zorunlu(k, "kullanici"), Giris.Zorunlu(k, "sifre"), Giris.Zorunlu(k, "musteriNo"), hesapNo, bas, bit));
            sonuc.AddRange(Ayristir(await Http.Gonder(http, m, Ad, iptal), istek.Iban));
        }
        return sonuc;
    }

    public static string Zarf(string kullanici, string sifre, string musteriNo, string hesapNo, DateTime bas, DateTime bit) =>
        $"""<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:alb="http://services.albaraka.com/"><soap:Body><alb:getHesapHareketleri><alb:pId>{Kac(kullanici)}</alb:pId><alb:pIdPass>{Kac(sifre)}</alb:pIdPass><alb:pParams><musteriNo>{Kac(musteriNo)}</musteriNo><hesapNo>{Kac(hesapNo)}</hesapNo><basTarih>{bas:yyyyMMdd}</basTarih><sonTarih>{bit:yyyyMMdd}</sonTarih></alb:pParams></alb:getHesapHareketleri></soap:Body></soap:Envelope>""";

    public static List<SaglayiciHareketi> Ayristir(string xml, string iban)
    {
        var d = Oku(xml);
        var sonucE = Ilk(d, "result");
        if (sonucE is not null && Al(sonucE, "code") is { } kod && kod is not ("0" or "00" or "000"))
            throw new BankaBaglantiHatasi($"{Ad}: {Al(sonucE, "message") ?? "istek reddedildi"} ({kod}).");
        var liste = new List<SaglayiciHareketi>();
        foreach (var hesap in Hepsi(d, "hesapHareketleri"))
        {
            var hIban = Al(hesap, "hesapIban");
            if (hIban is not null && !string.Equals(hIban.Replace(" ", ""), iban, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var h in hesap.Elements().Where(e => string.Equals(e.Name.LocalName, "hesapHareket", StringComparison.OrdinalIgnoreCase)))
            {
                var gun = Tarih.Oku(Al(h, "tarih"), "yyyyMMdd", "dd.MM.yyyy", "yyyy-MM-dd");
                var saat = Al(h, "saat") is { } s && TimeSpan.TryParseExact(s.Length == 6 ? s : s.Replace(":", "").PadRight(6, '0'), "hhmmss", CultureInfo.InvariantCulture, out var ts) ? ts : TimeSpan.Zero;
                var tutar = Sayi.Oku(Al(h, "islemTutari"));
                var ba = Al(h, "borcAlacak");
                if (gun is null || tutar is null) continue;
                var gelen = ba is not null ? ba.StartsWith('A') || ba == "+" : tutar > 0;
                var t = HareketTemizleyici.Temizle(KodSabit, Al(h, "MuhRefNo") is { } mr ? $"{Al(h, "fisNo")}-{mr}" : Al(h, "fisNo"),
                    gun.Value.Date + saat, gelen, tutar.Value, null, Al(h, "karsiHesapIban"), Al(h, "karsiHesapTcknVkn"), Al(h, "aciklama"));
                if (t is not null) liste.Add(t);
            }
        }
        return liste;
    }
}
