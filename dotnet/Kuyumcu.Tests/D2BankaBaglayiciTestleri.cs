using Kuyumcu.Api.Modules.Banka;
using Kuyumcu.Api.Modules.Banka.Baglayicilar;
using Kuyumcu.Core.Domain;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D2-05b (19.10) — hazır banka bağlayıcıları: ayrıştırıcılar (saf). Örnek yanıtlar
// ARASTIRMA-BANKA-API-TEKNIK-2026-10-19.md'deki alan adlarıyla BİREBİR kuruldu.
// Her bankada: yön, tutar, tarih (Türkiye saati → UTC), referans, karşı IBAN/kimlik, hata kodu.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class D2BankaBaglayiciTestleri
{
    private static readonly string Iban = TestIban.Uret();
    private static readonly string KarsiIban = TestIban.Uret();
    private const string Tc = "10000000146";

    [Fact]
    public void Garanti_belgedeki_ornek_yanit_okunur_A_gelen_B_giden_karsi_IBAN_VKN_ad()
    {
        var json = $$"""
        { "result": { "returnCode": 200, "reasonCode": 1, "messageText": "Başarılı" },
          "transactions": [
            { "IBAN": "{{Iban}}", "activityDate": "2026-10-19", "txnCreditDebitIndicator": "A", "amount": 1250.67,
              "explanation": "bilezik", "corrVKN": null, "corrTCKN": "{{Tc}}",
              "transactionReferenceId": "2026-10-19T16:13:36.688516", "transactionInstanceId": "2026-10-19T16:13:36.862988",
              "enrichmentInformation": [
                { "enrichmentCode": "CORRACNT", "enrichmentValue": { "corrIBAN": "{{KarsiIban}}" } },
                { "enrichmentCode": "MUS", "enrichmentValue": { "corrNameSurnameText": "Hatice Demirkol" } } ] },
            { "IBAN": "{{Iban}}", "activityDate": "2026-10-19", "txnCreditDebitIndicator": "B", "amount": 40,
              "explanation": "00000", "transactionReferenceId": "x", "transactionInstanceId": "2026-10-19T09:00:00.000000" } ] }
        """;
        var (h, ham) = GarantiBaglayicisi.Ayristir(json, Iban);
        Assert.Equal(2, ham);
        Assert.Equal(2, h.Count);
        var g = h[0];
        Assert.Equal(HareketYonu.BankaGelen, g.Yon);
        Assert.Equal(1250.67m, g.Tutar);
        Assert.Equal("Hatice Demirkol", g.KarsiAd);
        Assert.Equal(KarsiIban, g.KarsiIban);
        Assert.Equal(Tc, g.KarsiKimlikNo);
        Assert.Equal(new DateTime(2026, 10, 19, 13, 13, 36, DateTimeKind.Utc).AddTicks(8_629_880), g.Tarih);   // 16:13 TR = 13:13 UTC
        Assert.StartsWith("GARANTI-", g.Referans);
        Assert.Equal(HareketYonu.BankaGiden, h[1].Yon);
        Assert.Null(h[1].Aciklama);   // "00000" açıklama sayılmaz
        // Banka benzersiz no vermez: aynı yanıt aynı referansı üretir (ikinci çekim YAZMAZ).
        Assert.Equal(g.Referans, GarantiBaglayicisi.Ayristir(json, Iban).Hareketler[0].Referans);
    }

    // 27.10 — portaldaki resmi belgeyle doğrulama: başka bankadan EFT'de karşı tarafın adı/IBAN'ı/kimliği yalnız
    // EFT zenginleştirmesinde gelir (MUS/CORRACNT yok, corrVKN null). Gelende GÖNDEREN okunur, gidende ALICI.
    [Fact]
    public void Garanti_baska_bankadan_EFT_gonderen_ad_IBAN_kimlik_EFT_blogundan_okunur()
    {
        var json = $$"""
        { "result": { "returnCode": 200, "reasonCode": 1, "messageText": "Başarılı" },
          "transactions": [
            { "IBAN": "{{Iban}}", "activityDate": "2026-10-27", "txnCreditDebitIndicator": "A", "amount": 3500,
              "explanation": "Para Transferi EFT", "corrVKN": null, "corrTCKN": null,
              "transactionReferenceId": "2026-10-27T14:15:14.008745", "transactionInstanceId": "2026-10-27T14:15:14.440521",
              "enrichmentInformation": [
                { "enrichmentCode": "EFT", "enrichmentValue": { "moduleName": "EFT", "msgType": "KRED",
                  "senderIBANEFT": "{{KarsiIban}}", "receiverIBANEFT": "{{Iban}}", "senderName": "Kemal Ardıç",
                  "receiverName": "Altın Çarşı", "senderVKN": "{{Tc}}" } } ] },
            { "IBAN": "{{Iban}}", "activityDate": "2026-10-27", "txtnCreditDebitIndicator": "B", "amount": 560.7,
              "explanation": "EFT", "transactionReferenceId": "y", "transactionInstanceId": "2026-10-27T15:00:00.000000",
              "enrichmentInformation": [
                { "enrichmentCode": "EFT", "enrichmentValue": { "senderIBANEFT": "{{Iban}}", "receiverIBANEFT": "{{KarsiIban}}",
                  "senderName": "Altın Çarşı", "receiverName": "Tuna Akarsu", "senderVKN": "{{Tc}}" } } ] } ] }
        """;
        var (h, ham) = GarantiBaglayicisi.Ayristir(json, Iban);
        Assert.Equal(2, ham);
        Assert.Equal(2, h.Count);
        var g = h[0];
        Assert.Equal(HareketYonu.BankaGelen, g.Yon);
        Assert.Equal("Kemal Ardıç", g.KarsiAd);
        Assert.Equal(KarsiIban, g.KarsiIban);
        Assert.Equal(Tc, g.KarsiKimlikNo);
        // Şemadaki yazım ("txtn…") da yön olarak okunur; gidende karşı taraf ALICIDIR, gönderen kimliği alınmaz.
        var c = h[1];
        Assert.Equal(HareketYonu.BankaGiden, c.Yon);
        Assert.Equal("Tuna Akarsu", c.KarsiAd);
        Assert.Equal(KarsiIban, c.KarsiIban);
        Assert.Null(c.KarsiKimlikNo);
    }

    [Fact]
    public void Garanti_MUS_ve_CORRACNT_varsa_EFT_blogunu_ezmez()
    {
        var baska = TestIban.Uret();
        var json = $$"""
        { "transactions": [ { "txnCreditDebitIndicator": "A", "amount": 10, "transactionInstanceId": "2026-10-27T10:00:00.000000",
            "enrichmentInformation": [
              { "enrichmentCode": "CORRACNT", "enrichmentValue": { "corrIBAN": "{{KarsiIban}}" } },
              { "enrichmentCode": "MUS", "enrichmentValue": { "corrNameSurnameText": "Hatice Demirkol" } },
              { "enrichmentCode": "EFT", "enrichmentValue": { "senderIBANEFT": "{{baska}}", "senderName": "Başka Ad" } } ] } ] }
        """;
        var g = GarantiBaglayicisi.Ayristir(json, Iban).Hareketler.Single();
        Assert.Equal("Hatice Demirkol", g.KarsiAd);
        Assert.Equal(KarsiIban, g.KarsiIban);
    }

    [Fact]
    public void Banka_400_JSON_govdesindeki_Turkce_aciklama_kullaniciya_iletilir()
    {
        Assert.Equal("Vermiş olduğunuz clientId-consentId bilgileri hatalıdır. (kod 4)", JsonBankaHatasi.Coz(
            """{ "result": { "returnCode": 400, "reasonCode": 4, "messageText": "Vermiş olduğunuz clientId-consentId bilgileri hatalıdır." } }"""));
        Assert.Equal("Invalid Credentials", JsonBankaHatasi.Coz("""{ "result": { "code": 401, "info": "Invalid Credentials" } }"""));
        Assert.Null(JsonBankaHatasi.Coz("<html>404</html>"));
        Assert.Null(JsonBankaHatasi.Coz("{ bozuk"));
        Assert.Null(JsonBankaHatasi.Coz("""{ "result": { "returnCode": 400 } }"""));
    }

    // ── Kurucu 31.10: Garanti Hesap Bilgisi (bakiye) — resmi belge örnek yanıtı + şemadaki küçük harf yazımı ──
    [Fact]
    public void Garanti_bakiye_belgedeki_ornek_yanit_Balance_ve_AvailableBalance_okunur_IBAN_ile_hesap_secilir()
    {
        var baska = TestIban.Uret();
        var json = $$"""
        { "result": { "returnCode": 200, "reasonCode": 1, "messageText": "Başarılı" },
          "accounts": [
            { "balances": [ { "type": "Balance", "Amount": "1.00" } ], "IBAN": "{{baska}}", "currencyCode": "TL " },
            { "balances": [
                { "type": "Balance", "Amount": "9995559.34" }, { "type": "BlockedAmount", "Amount": "250.00" },
                { "type": "AvailableBalance", "Amount": "9995309.34" }, { "type": "CreditLimit", "Amount": "0.00" },
                { "type": "AvailableBalanceWithCredit", "Amount": "9995309.34" } ],
              "IBAN": "{{Iban}}", "currencyCode": "TL ", "status": "A" } ] }
        """;
        var b = GarantiBaglayicisi.BakiyeAyristir(json, Iban);
        Assert.NotNull(b);
        Assert.Equal(9995559.34m, b!.Bakiye);
        Assert.Equal(9995309.34m, b.Kullanilabilir);
    }

    [Fact]
    public void Garanti_bakiye_kucuk_harf_type_amount_ve_sayi_tutar_okunur_tek_hesap_IBANsiz_secilir()
    {
        var json = """
        { "result": { "returnCode": 200 },
          "accounts": [ { "balances": [ { "type": "balance", "amount": 1250.5 }, { "TYPE": "availableBalance", "AMOUNT": "1000,25" } ] } ] }
        """;
        var b = GarantiBaglayicisi.BakiyeAyristir(json, Iban);
        Assert.Equal(1250.5m, b!.Bakiye);
        Assert.Equal(1000.25m, b.Kullanilabilir);
    }

    [Fact]
    public void Garanti_bakiye_yoksa_null_uydurulmaz_hata_govdesi_sade_mesajla_durur()
    {
        Assert.Null(GarantiBaglayicisi.BakiyeAyristir("""{ "result": { "returnCode": 200 }, "accounts": [] }""", Iban));
        Assert.Null(GarantiBaglayicisi.BakiyeAyristir("""{ "result": { "returnCode": 200 }, "accounts": [ { "balances": [ { "type": "BlockedAmount", "Amount": "5" } ] } ] }""", Iban));
        // İki hesap, ikisi de bizim IBAN değil → yanlış hesabın bakiyesi YAZILMAZ.
        Assert.Null(GarantiBaglayicisi.BakiyeAyristir($$"""{ "accounts": [ { "IBAN": "{{KarsiIban}}", "balances": [ { "type": "Balance", "Amount": "5" } ] }, { "IBAN": "{{TestIban.Uret()}}", "balances": [ { "type": "Balance", "Amount": "6" } ] } ] }""", Iban));
        var ex = Assert.Throws<BankaBaglantiHatasi>(() => GarantiBaglayicisi.BakiyeAyristir(
            """{ "result": { "returnCode": 400, "reasonCode": 13, "messageText": "Hesap bilgisi ile İban bilgisi uyumsuzdur." } }""", Iban));
        Assert.Contains("uyumsuz", ex.Message);
        Assert.Contains("13", ex.Message);
        Assert.Throws<BankaBaglantiHatasi>(() => GarantiBaglayicisi.BakiyeAyristir("<html/>", Iban));
    }

    [Fact]
    public void Garanti_hata_kodu_sade_mesajla_durur()
    {
        var ex = Assert.Throws<BankaBaglantiHatasi>(() => GarantiBaglayicisi.Ayristir(
            """{ "result": { "returnCode": 400, "reasonCode": 18, "messageText": "Tarih aralığı 30 günü geçemez" } }""", Iban));
        Assert.Contains("30 günü", ex.Message);
        Assert.Contains("18", ex.Message);
    }

    [Fact]
    public void Garanti_istekleri_belge_ornegindeki_gibi_sube_ve_hesap_no_IBANdan_yollar()
    {
        // Belge örneği (Account Information + Account Transactions swagger, 01.11): IBAN TR620006200029500006291296 → unitNum "295", accountNum "6291296".
        const string belgeIban = "TR620006200029500006291296";
        Assert.Equal(("295", "6291296"), GarantiBaglayicisi.SubeHesap(belgeIban));
        Assert.Equal(("295", "6291296"), GarantiBaglayicisi.SubeHesap("tr62 0006 2000 2950 0006 2912 96"));

        using var b = System.Text.Json.JsonDocument.Parse(GarantiBaglayicisi.BakiyeGovdesi("c-1", belgeIban));
        Assert.Equal("c-1", b.RootElement.GetProperty("consentId").GetString());
        Assert.Equal("295", b.RootElement.GetProperty("unitNum").GetString());
        Assert.Equal("6291296", b.RootElement.GetProperty("accountNum").GetString());
        Assert.Equal(belgeIban, b.RootElement.GetProperty("IBAN").GetString());

        using var h = System.Text.Json.JsonDocument.Parse(GarantiBaglayicisi.IstekGovdesi("c-1", belgeIban, new DateTime(2026, 11, 1, 9, 0, 0), new DateTime(2026, 11, 1, 10, 0, 0), 1));
        Assert.Equal("295", h.RootElement.GetProperty("unitNum").GetString());
        Assert.Equal("6291296", h.RootElement.GetProperty("accountNum").GetString());
        Assert.Equal("2026-11-01T09:00:00.000", h.RootElement.GetProperty("startDate").GetString());
    }

    [Fact]
    public void Garanti_IBANi_degilse_sube_hesap_no_uydurulmaz_yalniz_IBAN_yollanir()
    {
        var ziraat = TestIban.Tamamla("0001000000000000123456");   // banka kodu 00010 (Garanti değil)
        Assert.Null(GarantiBaglayicisi.SubeHesap(ziraat));
        Assert.Null(GarantiBaglayicisi.SubeHesap(null));
        Assert.Null(GarantiBaglayicisi.SubeHesap("TR62000620"));
        using var b = System.Text.Json.JsonDocument.Parse(GarantiBaglayicisi.BakiyeGovdesi("c-1", ziraat));
        Assert.False(b.RootElement.TryGetProperty("unitNum", out _));
        Assert.False(b.RootElement.TryGetProperty("accountNum", out _));
        Assert.Equal(ziraat, b.RootElement.GetProperty("IBAN").GetString());
    }

    [Fact]
    public void VakifBank_GetirHareket_yaniti_Id_referans_Detaylar_karsi_bilgi_ve_hata_kodu()
    {
        string Zarf(string kod) => $"""
        <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope"><s:Body>
          <GetirHareketResponse xmlns="Peak.Integration.ExternalInbound.Ekstre">
            <GetirHareketResult xmlns:b="http://schemas.datacontract.org/2004/07/Peak.Integration.ExternalInbound.Ekstre.DataTransferObjects" xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays">
              <b:IslemKodu>{kod}</b:IslemKodu><b:IslemAciklamasi>Kullanıcı bilgileri hatalı</b:IslemAciklamasi>
              <b:Hesaplar><b:DtoEkstreHesap><b:HesapNoIban>{Iban}</b:HesapNoIban><b:Hareketler>
                <b:DtoEkstreHareket><b:IslemTarihi>2026-10-19 10:15:00</b:IslemTarihi><b:IslemAdi>EFT</b:IslemAdi>
                  <b:Tutar>8208,50</b:Tutar><b:BorcAlacak>A</b:BorcAlacak><b:Aciklama>kolye</b:Aciklama><b:IslemKodu>X1</b:IslemKodu>
                  <b:Detaylar><a:KeyValueOfstringstring><a:Key>GonderenIbanKumarasi</a:Key><a:Value>{KarsiIban}</a:Value></a:KeyValueOfstringstring>
                    <a:KeyValueOfstringstring><a:Key>GonderenKimlikNumarasi</a:Key><a:Value>{Tc}</a:Value></a:KeyValueOfstringstring></b:Detaylar>
                  <b:Id>778899</b:Id></b:DtoEkstreHareket>
              </b:Hareketler></b:DtoEkstreHesap></b:Hesaplar>
            </GetirHareketResult></GetirHareketResponse></s:Body></s:Envelope>
        """;
        var h = Assert.Single(VakifbankBaglayicisi.Ayristir(Zarf("VBB0001"), Iban));
        Assert.Equal("VAKIF-778899", h.Referans);
        Assert.Equal(8208.50m, h.Tutar);
        Assert.Equal(HareketYonu.BankaGelen, h.Yon);
        Assert.Equal(KarsiIban, h.KarsiIban);
        Assert.Equal(Tc, h.KarsiKimlikNo);
        Assert.Equal("EFT kolye", h.Aciklama);
        // Hareketin kendi IslemKodu (X1) sonuç kodu sayılmaz; sonuç düzeyindeki hata kodu durdurur.
        var ex = Assert.Throws<BankaBaglantiHatasi>(() => VakifbankBaglayicisi.Ayristir(Zarf("VBB0101"), Iban));
        Assert.Contains("Kullanıcı bilgileri hatalı", ex.Message);
    }

    [Fact]
    public void VakifBank_zarfi_sifreyi_XML_kacisiyla_yazar_ve_WS_Addressing_tasir()
    {
        var z = VakifbankBaglayicisi.Zarf("123", "kul", "a<b&c", "999", new DateTime(2026, 10, 19), new DateTime(2026, 10, 19, 23, 59, 0));
        Assert.Contains("<peak1:Sifre>a&lt;b&amp;c</peak1:Sifre>", z);
        Assert.Contains("<wsa:Action>Peak.Integration.ExternalInbound.Ekstre/ISOnlineEkstreServis/GetirHareket</wsa:Action>", z);
        Assert.Contains("<peak1:SorguBitisTarihi>2026-10-19 23:59</peak1:SorguBitisTarihi>", z);
        System.Xml.Linq.XDocument.Parse(z);   // geçerli XML
    }

    [Fact]
    public void QNB_iki_yerel_ad_da_okunur_baska_IBAN_suzulur_hata_kodu_durdurur()
    {
        string Y(string sarmal, string iban, string hata = "0") => $"""
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"><soapenv:Body>
          <ns:getTransactionInfoResponse xmlns:ns="http://teknikbaglantiekstre.genericekstrenew2.driver.maestro.ibtech.com"><ns:return>
            <{sarmal}><iban>{iban}</iban>
              <transactions><debitOrCreditCode>B</debitOrCreditCode><transactionAmount>150.00</transactionAmount>
                <transactionDate>2026-10-19T11:00:00</transactionDate><transactionDescription>kira</transactionDescription>
                <transactionId>5551</transactionId><opponentIBAN>{KarsiIban}</opponentIBAN></transactions>
            </{sarmal}><errorCode>{hata}</errorCode><errorDescription>Şifre hatalı</errorDescription>
          </ns:return></ns:getTransactionInfoResponse></soapenv:Body></soapenv:Envelope>
        """;
        foreach (var ad in new[] { "accountInfoReturnType", "accountInfos" })
        {
            var h = Assert.Single(QnbBaglayicisi.Ayristir(Y(ad, Iban), Iban));
            Assert.Equal(("QNB-5551", HareketYonu.BankaGiden, 150m), (h.Referans, h.Yon, h.Tutar));
        }
        Assert.Empty(QnbBaglayicisi.Ayristir(Y("accountInfos", TestIban.Uret()), Iban));
        Assert.Contains("Şifre hatalı", Assert.Throws<BankaBaglantiHatasi>(() => QnbBaglayicisi.Ayristir(Y("accountInfos", Iban, "E101"), Iban)).Message);
    }

    [Fact]
    public void Akbank_virgullu_tutar_arti_eksi_yon_TimeStamp_saati_FisNo_referans()
    {
        var xml = $"""
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><GetExtreWithParamsResponse xmlns="http://tempuri.org/">
          <GetExtreWithParamsResult><HesapHareketleri><Hesap><IBAN>{Iban}</IBAN>
            <Detay><IslemTarihi>20261019</IslemTarihi><ReferansNo>R1</ReferansNo><Tutar>1.250,75</Tutar><Aciklama>HAVALE</Aciklama>
              <TutarBorcAlacak>+</TutarBorcAlacak><FisNo>F9</FisNo><AmirTCKN>{Tc}</AmirTCKN><BorcluIBAN>{KarsiIban}</BorcluIBAN>
              <DekontAlacakAciklama>yüzük</DekontAlacakAciklama><TimeStamp>20261019143000123456</TimeStamp></Detay>
            <Detay><IslemTarihi>20261019</IslemTarihi><Tutar>99,00</Tutar><TutarBorcAlacak>-</TutarBorcAlacak><FisNo>F10</FisNo></Detay>
          </Hesap></HesapHareketleri></GetExtreWithParamsResult></GetExtreWithParamsResponse></soap:Body></soap:Envelope>
        """;
        var h = AkbankBaglayicisi.Ayristir(xml, Iban);
        Assert.Equal(2, h.Count);
        Assert.Equal(("AKBANK-F9-R1", HareketYonu.BankaGelen, 1250.75m, KarsiIban, Tc), (h[0].Referans, h[0].Yon, h[0].Tutar, h[0].KarsiIban, h[0].KarsiKimlikNo));
        Assert.Equal(new DateTime(2026, 10, 19, 11, 30, 0, DateTimeKind.Utc), h[0].Tarih);
        Assert.Equal("HAVALE yüzük", h[0].Aciklama);
        Assert.Equal((HareketYonu.BankaGiden, 99m), (h[1].Yon, h[1].Tutar));
        Assert.Contains("<baslangicTarihi>20261019000000000000</baslangicTarihi>", AkbankBaglayicisi.Zarf("U", "1", new DateTime(2026, 10, 19), new DateTime(2026, 10, 19, 23, 59, 59)));
    }

    [Fact]
    public void KuveytTurk_Credit_Debit_BusinessKey_Success_false_durdurur_kendi_IBAN_karsi_sayilmaz()
    {
        string Y(string ok, string hIban) => $"""
        <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body><GetAccountStatementResponse xmlns="http://boa.net/BOA.Integration.CoreBanking.Teller/Service">
          <GetAccountStatementResult xmlns:a="http://schemas.datacontract.org/2004/07/BOA.Integration.Base" xmlns:b="http://schemas.datacontract.org/2004/07/BOA.Integration.Model.CoreBanking.Teller">
            <a:ErrorMessage>Kullanıcı bulunamadı</a:ErrorMessage><a:Success>{ok}</a:Success><a:Value><b:AccountContract><b:Details>
              <b:TransactionDetailContract><b:BusinessKey>4411</b:BusinessKey><b:Credit>500.00</b:Credit><b:Debit>0</b:Debit>
                <b:Description>çeyrek</b:Description><b:Iban>{hIban}</b:Iban><b:SenderName>Ali Veli</b:SenderName>
                <b:SenderIdentityNumber>{Tc}</b:SenderIdentityNumber><b:TranDate>2026-10-19T09:30:00</b:TranDate></b:TransactionDetailContract>
            </b:Details></b:AccountContract></a:Value></GetAccountStatementResult></GetAccountStatementResponse></s:Body></s:Envelope>
        """;
        var h = Assert.Single(KuveytTurkBaglayicisi.Ayristir(Y("true", KarsiIban), Iban));
        Assert.Equal(("KUVEYT-4411", HareketYonu.BankaGelen, 500m, "Ali Veli", KarsiIban, Tc), (h.Referans, h.Yon, h.Tutar, h.KarsiAd, h.KarsiIban, h.KarsiKimlikNo));
        Assert.Null(Assert.Single(KuveytTurkBaglayicisi.Ayristir(Y("true", Iban), Iban)).KarsiIban);
        Assert.Contains("Kullanıcı bulunamadı", Assert.Throws<BankaBaglantiHatasi>(() => KuveytTurkBaglayicisi.Ayristir(Y("false", Iban), Iban)).Message);
    }

    [Fact]
    public void Albaraka_tarih_saat_birlesir_fis_MuhRef_referans_result_kodu_durdurur()
    {
        string Y(string kod) => $"""
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><getHesapHareketleriResponse xmlns="http://services.albaraka.com/">
          <responseData><result><code>{kod}</code><message>Yetkisiz</message></result><hesapHareketleri><hesapIban>{Iban}</hesapIban>
            <hesapHareket><tarih>20261019</tarih><saat>101500</saat><aciklama>gelen havale</aciklama><borcAlacak>A</borcAlacak>
              <islemTutari>3000,00</islemTutari><karsiHesapIban>{KarsiIban}</karsiHesapIban><fisNo>77</fisNo><MuhRefNo>M1</MuhRefNo></hesapHareket>
          </hesapHareketleri></responseData></getHesapHareketleriResponse></soap:Body></soap:Envelope>
        """;
        var h = Assert.Single(AlbarakaBaglayicisi.Ayristir(Y("0"), Iban));
        Assert.Equal(("ALBARAKA-77-M1", 3000m, new DateTime(2026, 10, 19, 7, 15, 0, DateTimeKind.Utc)), (h.Referans, h.Tutar, h.Tarih));
        Assert.Throws<BankaBaglantiHatasi>(() => AlbarakaBaglayicisi.Ayristir(Y("99"), Iban));
    }

    [Fact]
    public void Ziraat_HareketlerDetay_harf_duyarsiz_iptal_fisi_atlanir_hata_kodu_durdurur()
    {
        string Y(string hk) => $"""
        <SorgulaHesapHareketZamanIleResult xmlns="http://tempuri.org/"><HataKodu>{hk}</HataKodu><HataAck>Şifre hatalı</HataAck>
          <HareketDetay><HareketlerDetay><islemTarihi>2026-10-19T12:00:00</islemTarihi><tutar>700.00</tutar><borcAlacak>A</borcAlacak>
            <AdUnvan>Hatice Demirkol</AdUnvan><TcknVkn>{Tc}</TcknVkn><IBAN>{KarsiIban}</IBAN><DekontNo>D1</DekontNo><Aciklama>borç ödemesi</Aciklama></HareketlerDetay>
          <HareketlerDetay><IslemTarihi>2026-10-19T12:05:00</IslemTarihi><Tutar>700.00</Tutar><BorcAlacak>B</BorcAlacak><IptalZamani>2026-10-19T12:06:00</IptalZamani></HareketlerDetay>
          </HareketDetay></SorgulaHesapHareketZamanIleResult>
        """;
        var h = Assert.Single(ZiraatBaglayicisi.Ayristir(Y("0")));
        Assert.Equal(("ZIRAAT-D1", "Hatice Demirkol", Tc), (h.Referans, h.KarsiAd, h.KarsiKimlikNo));
        Assert.Contains("Şifre hatalı", Assert.Throws<BankaBaglantiHatasi>(() => ZiraatBaglayicisi.Ayristir(Y("12"))).Message);
    }

    [Fact]
    public void IsBankasi_XMLEXBAT_IBAN_ile_suzulur_eksi_miktar_giden_XML_olmayan_yanit_kimlik_hatasi()
    {
        var xml = $"""
        <XMLEXBAT><Tarih>19.10.2026</Tarih><Hesaplar>
          <Hesap><Tanimlamalar><IBANNo>{Iban}</IBANNo></Tanimlamalar><Hareketler>
            <Hareket><Tarih>19.10.2026</Tarih><Saat>14:20:00</Saat><HareketSirano>H1</HareketSirano><Miktar>-250,00</Miktar>
              <Aciklama>kira</Aciklama><borcAlacak>B</borcAlacak></Hareket></Hareketler></Hesap>
          <Hesap><Tanimlamalar><IBANNo>{TestIban.Uret()}</IBANNo></Tanimlamalar><Hareketler>
            <Hareket><Tarih>19.10.2026</Tarih><HareketSirano>H2</HareketSirano><Miktar>10,00</Miktar><borcAlacak>A</borcAlacak></Hareket></Hareketler></Hesap>
        </Hesaplar></XMLEXBAT>
        """;
        var h = Assert.Single(IsBankasiBaglayicisi.Ayristir(xml, Iban));
        Assert.Equal(("ISBANK-H1", HareketYonu.BankaGiden, 250m), (h.Referans, h.Yon, h.Tutar));
        Assert.Throws<BankaBaglantiHatasi>(() => IsBankasiBaglayicisi.Ayristir("Kullanıcı adı veya şifre hatalı", Iban));
    }

    [Fact]
    public void Temizleyici_gecersiz_IBAN_TC_ad_hareketi_kilitlemez_alan_bos_kalir_sifir_tutar_atlanir()
    {
        var t = HareketTemizleyici.Temizle("X", "1", new DateTime(2026, 10, 19, 10, 0, 0), true, 100m,
            "12345", "TR000000000000000000000000", "11111111111", "a")!;   // olumsuz-fikstür: bankadan gelen bozuk veri
        Assert.Null(t.KarsiAd);
        Assert.Null(t.KarsiIban);
        Assert.Null(t.KarsiKimlikNo);
        Assert.Null(HareketTemizleyici.Temizle("X", "1", DateTime.Now, true, 0.001m, null, null, null, null));
        Assert.True(HareketTemizleyici.Temizle("X", new string('9', 200), DateTime.Now, true, 1m, null, null, null, null)!.Referans.Length <= 64);
    }

    [Fact]
    public void Katalogdaki_her_banka_icin_bagliyici_var_ve_zorunlu_alan_denetimi()
    {
        Assert.Equal(8, BankaBaglayiciKatalogu.Tumu.Count);
        Assert.All(BankaBaglayiciKatalogu.Tumu, t => Assert.NotNull(BankaBaglayicilari.Bul(t.Kod)));
        Assert.All(BankaBaglayiciKatalogu.Tumu, t => Assert.True(t.Kod.Length <= 20));
        var q = BankaBaglayiciKatalogu.Bul("QNB")!;
        Assert.Equal(("sifre", "Servis şifresi zorunludur"), BankaBaglayiciKatalogu.Denetle(q, new Dictionary<string, string?> { ["kullanici"] = "a", ["hesapNo"] = "1" }));
        Assert.Equal("x", BankaBaglayiciKatalogu.Denetle(q, new Dictionary<string, string?> { ["kullanici"] = "a", ["sifre"] = "b", ["hesapNo"] = "1", ["x"] = "y" })!.Value.Alan);
        Assert.Null(BankaBaglayiciKatalogu.Denetle(q, new Dictionary<string, string?> { ["kullanici"] = "a", ["sifre"] = "b", ["hesapNo"] = "1" }));
        Assert.Equal("Servis kullanıcı adı: ab•••• · Hesap numarası: 12••", BankaBaglayiciKatalogu.Ozet(q, new Dictionary<string, string?> { ["kullanici"] = "abcdef", ["sifre"] = "GIZLI", ["hesapNo"] = "1234" }));
    }

    [Fact]
    public void Dilimleme_aralik_parcalari_ortusmez_bosluk_birakmaz()
    {
        var d = Tarih.Dilimle(new DateTime(2026, 10, 1), new DateTime(2026, 10, 3, 12, 0, 0), 1).ToList();
        Assert.Equal(3, d.Count);
        Assert.Equal(new DateTime(2026, 10, 1, 23, 59, 59), d[0].Bit);
        Assert.Equal(new DateTime(2026, 10, 2), d[1].Bas);
        Assert.Equal(new DateTime(2026, 10, 3, 12, 0, 0), d[2].Bit);
    }
}
