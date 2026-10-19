# Araştırma — Banka hesap hareketi servisleri: C# bağlayıcı için teknik ayrıntı · 19.10.2026

> Önceki belge: `ARASTIRMA-BANKA-DOGRUDAN-ENTEGRASYON-2026-10-18.md` (hukuk, başvuru yolu, ücret).
> Bu belge yalnız **tel üstündeki biçimi** toplar: uç nokta, kimlik, istek/yanıt alan adları.
> Yöntem: hesap açılmadı, giriş yapılmadı, form doldurulmadı. Yalnız herkese açık sayfalar ve açık kaynak kod okundu.
> Bankaların sunucularına bu ortamdan erişilemedi (curl: bağlantı yok), bu yüzden **hiçbir WSDL canlıdan indirilmedi.** Alan adları açık kaynak koddan çıkarıldı.

## 0. Özet tablo

| Banka | Protokol | Uç nokta (kaynakta görülen) | Kimlik | Bağlayıcı yazmaya yeter mi |
|---|---|---|---|---|
| Garanti BBVA | REST/JSON | `https://apis.garantibbva.com.tr/balancesandmovements/accountinformation/transaction/v1/gettransactions` | OAuth2 client_credentials (scope `oob`) + `consentId` | **EVET** |
| VakıfBank | SOAP 1.2 + WS-Addressing (WCF wsHttpBinding) | `https://vbservice.vakifbank.com.tr/HesapHareketleri.OnlineEkstre/SOnlineEkstreServis.svc` | Gövde içinde `MusteriNo` + `KurumKullanici` + `Sifre` | **EVET** |
| QNB (Finansbank) | SOAP 1.1 (Axis2) | `https://fbmaestro.qnb.com.tr/MaestroCoreEkstre/services/TeknikBaglantiService.TeknikBaglantiServiceHttpSoap11Endpoint/` (+ `http://…:9086/…`) | Gövde içinde `userName` + `password` | **EVET** |
| Akbank | SOAP 1.1 / ASMX | `https://firmahizmetleri.akbank.com/Extre_InterfaceService/Service.asmx` | HTTP Basic (kullanıcı/şifre) + `urf` parametresi + IP kısıtı | **EVET** (yanıt kök adı TEYİTSİZ) |
| Kuveyt Türk | SOAP 1.1 (WCF basicHttpBinding, BOA) | `https://boa.kuveytturk.com.tr/BOA.Integration.WCFService/BOA.Integration.AccountStatement/AccountStatementService.svc/Basic` | Gövde içinde `ExtUName` + `ExtUPassword` | **EVET** |
| Albaraka Türk | SOAP 1.1 / ASMX | `https://eservice.albarakaturk.com.tr:10214/invoiceincomingsite/HesapBilgileriService.asmx` | Gövde içinde `pId` + `pIdPass` | **EVET** (ad alanı çelişkisi var, aşağıda) |
| Ziraat | SOAP 1.1 / ASMX (+ HTTP POST form) | `https://hesap.ziraatbank.com.tr/HEK_NKYWS/HesapHareketleri.asmx` | Gövde içinde `KurumKod`/`kurumKod`/`kullaniciKod` + `Sifre`/`sifre` | **KISMEN** (istek tam, yanıt alan adı büyük/küçük harfi ve tarih biçimi kısmen TEYİTSİZ) |
| İş Bankası | HTTP POST form → XML (Posmatik) | `https://posmatik2.isbank.com.tr/Authenticate.aspx` / `AuthenticateSpecific.aspx` | Form alanı `uid` + `pwd` (bankanın verdiği servis kullanıcısı) | **KISMEN** (iki uç nokta, tarih parametresi yalnız birinde; resmi belge yok) |
| Yapı Kredi | SOAP (`sorgula`, `arg0`) | **TEYİTSİZ** | **TEYİTSİZ** | **KISMEN** (yalnız alan adları) |
| Halkbank | SOAP/JSON? (`BagliMusteriEkstreSorgulama`) | **TEYİTSİZ** | **TEYİTSİZ** | **KISMEN** (yalnız alan adları) |
| DenizBank | REST/JSON + JWS imza | Canlı `https://apigw.denizbank.com/api/v2/{ApiName}`, UAT `https://ssgtest.denizbank.com:9443/apiuatv2/{ApiName}` | `AppKey` + `Channel` (gövde başlığı) + RS256 JWS `Signature` başlığı + statik IP | **KISMEN** (`ApiName` TEYİTSİZ) |
| Enpara | QNB ile aynı servis olduğu söyleniyor | **TEYİTSİZ** | — | **HAYIR** (ayrı teknik kaynak yok) |

**Kaynak güvenilirlik sırası** (bu belgede her alan için hangisi kullanıldı yazıyor):
1. **svcutil `Reference.cs`** (bankanın WSDL'inden .NET aracıyla üretilmiş C# sınıfları): alan adı, sıra, ad alanı, SOAPAction birebir. En güvenilir.
2. **Bankanın OpenAPI tanımı** (Garanti `api.json`, topluluk deposuna kopyalanmış).
3. **Çalışan topluluk kodu** (PHP/C#/Node, elle yazılmış SOAP zarfı).
4. **SAP ABAP deposu `xcersoylu/EHO20250605`** (20 Türk bankası için SAP EHÖ). Dikkat: bu kod bankaya doğrudan değil, araya konmuş SAP CPI ara katmanına JSON gönderiyor (`METH CALL_API.abap`: `ms_bankpass-cpi_url`, Basic auth). JSON'daki adlar CPI'nin girdisi; bankanın XML adlarıyla aynı olması **olası ama TEYİTSİZ**. Yanıt tiplerinde ABAP alan adlarını küçük harfe çeviriyor; **büyük/küçük harf TEYİTSİZ.**

**Güvenlik notu:** Açık kaynak depolardan ikisinde (`crazypsp/TurksoftApp` → `TurkSoft.BankWebUI/wwwroot/data/bankAccounts.json` ve `TestSeedData.cs`; `gokmenma/ersan_elk` → `test/posmatik_probe.php`) gerçek gibi görünen banka servis kullanıcı adı/şifreleri açıkta duruyor. Bu belgeye **hiçbiri aktarılmadı**, kullanılmadı.

---

## 1. Garanti BBVA — EHÖ Account Transactions

**Bağlayıcı yazmaya yeter mi: EVET.**

| Konu | Değer | Kaynak |
|---|---|---|
| Protokol | REST/JSON, `POST` | OpenAPI `api.json` |
| Taban URL | `https://apis.garantibbva.com.tr` (OpenAPI'de bozuk yazılmış: `[https://apis.garantibbva.com.tr:443`) | [garanti_deneme/docs/api.json](https://github.com/emrekartal99/garanti_deneme/blob/HEAD/docs/api.json) |
| Ayrı sandbox host | **TEYİTSİZ** (OpenAPI "Development Sandbox Service" diyor, ayrı adres vermiyor) | aynı |
| Token URL | `https://apis.garantibbva.com.tr/auth/oauth/v2/token` | OpenAPI `securitySchemes.OauthClientCredentials.flows.clientCredentials.tokenUrl` |
| Grant / scope | `client_credentials`, scope `oob` | OpenAPI + [Hasokeyk/garanti-bbva-php](https://github.com/Hasokeyk/garanti-bbva-php/blob/HEAD/src/GarantiBBVA/GarantiBBVA.php) |
| Token alanları | `client_id`, `client_secret`, `grant_type`, `redirect_uri` (portaldaki Callback URL ile birebir) | Hasokeyk (query string ile `GET`!), [dolibarr-garanti-api/docs/api-notlari.md](https://github.com/mbrksntrk/dolibarr-garanti-api/blob/HEAD/docs/api-notlari.md) (form gövdesi de çalışıyor) |
| Token ömrü | **Tek kullanımlık**: her istekten (her sayfadan) önce yeni token | OpenAPI açıklaması: "The access token can be used only one time." |
| Hareket yolu | `POST /balancesandmovements/accountinformation/transaction/v1/gettransactions` | OpenAPI, Hasokeyk |
| Bakiye yolu | `POST /balancesandmovements/accountinformation/account/v1/getaccountinformation` (gövde `{consentId}`) | Hasokeyk |
| Başlıklar | `Authorization: Bearer <token>`, `Content-Type: application/json`, `Accept: application/json` | Hasokeyk, api_spec.md |
| Tarih aralığı | En çok **30 gün** (reasonCode 18) | api_spec.md, api-notlari.md |
| Sayfalama | `pageIndex` (1'den), `pageSize` ≤ **500** (reasonCode 30). Toplam sayfa bilgisi yok: dönen kayıt < pageSize ise son sayfa | aynı. Not: SAP ABAP 1000 gönderiyor (`YCL_EHO_GET_RECEIPTS_GAR`), 500 sınırıyla çelişiyor → 500 kullan |
| Hız sınırı | HTTP 429 "API Plan Limit Exceeded". Sayısal sınır **TEYİTSİZ** | OpenAPI |
| Onay (consent) | İşletme Garanti internet şubesinde Başvur → EHÖ'ye client_id'yi girer (34 haneliyse başına 0 ile 36'ya tamamla), hesap seçer → `consentId` oluşur | Hasokeyk README |

İstek (OpenAPI örneği, birebir):
```json
{
  "consentId": "3e09da8a-ae9c-50bc-b34a-c61531729dbe",
  "unitNum": "295",
  "accountNum": "6291296",
  "IBAN": "TR620006200029500006291296",
  "startDate": "2020-12-25T12:53:07.867",
  "endDate": "2020-12-25T17:53:07.867",
  "transactionId": "",
  "pageIndex": 1,
  "pageSize": 100
}
```
- `consentId` zorunlu. `unitNum`+`accountNum`+`IBAN` isteğe bağlı; verilmezse consent'teki tüm hesaplar (api-notlari.md). Şube ve hesap no birlikte gönderilmeli (reasonCode 5).
- Tarih biçimi `yyyy-MM-ddTHH:mm:ss.fff`, saat dilimi yazılmıyor (api-notlari.md: Türkiye saati).
- Şemada `endtDate` yazım hatası var, gerçekte `endDate` (api-notlari.md).

Yanıt (OpenAPI `components/schemas/Transaction/example`, birebir; dış sarmal api_spec.md'den):
```json
{
  "result": { "returnCode": 200, "reasonCode": 1, "messageText": "Başarılı" },
  "transactions": [
    {
      "customerNum": 45245447,
      "customerName": "DURU LTD ",
      "VKN": 1194136989,
      "TCKN": null,
      "productId": "9d5de526-e32c-5cc7-81ce-545b93ade759",
      "unitNum": 295,
      "accountNum": 6291296,
      "currencyCode": "TL",
      "IBAN": "TR620006200029500006291296",
      "activityDate": "2020-12-25",
      "valueDate": "2020-12-28",
      "txnCreditDebitIndicator": "A",
      "amount": 1250.67,
      "balanceAfterTransaction": 9836180.2,
      "explanation": "00000",
      "transactionId": "WPDT",
      "clasificationCode": "NDTS",
      "corrVKN": "1518410029",
      "corrTCKN": null,
      "corrCustomerNum": 45271413,
      "transactionReferenceId": "2020-12-25T16:13:36.688516",
      "transactionInstanceId": "2020-12-25T16:13:36.862988",
      "enrichmentInformation": [
        { "enrichmentCode": "CORRACNT", "enrichmentValue": { "moduleName": "CORRACNT", "productType": "Account", "corrAccountNum": "6287369", "corrUnitNum": "295", "corrIBAN": "TR700006200029500006287369" } },
        { "enrichmentCode": "MUS", "enrichmentValue": { "moduleName": "MUS", "nameSurnameText": "DURU LTD ", "corrNameSurnameText": "DURU LTD " } },
        { "enrichmentCode": "HES", "enrichmentValue": { "moduleName": "HES", "accountMainType": "M", "accSubType": "S", "accType": "A", "productCode": "VDSTDUI", "subProductCode": "" } },
        { "enrichmentCode": "DTS", "enrichmentValue": { "moduleName": "DTS", "firmOriginalText": "", "relatedInvText1": "" } },
        { "enrichmentCode": "DEK", "enrichmentValue": { "moduleName": "DEK", "detailText": "" } }
      ]
    }
  ]
}
```

Mihenk alan eşlemesi:

| Mihenk | Garanti alanı |
|---|---|
| Tarih | `activityDate` (muhasebe), saat için `transactionInstanceId` (ISO zaman damgası) |
| Valör | `valueDate` |
| Tutar | `amount` (hep pozitif) |
| Yön | `txnCreditDebitIndicator`: `A` = alacak (hesaba giriş), `B` = borç (çıkış). (SAP kodu A/B'yi ters çeviriyor çünkü banka hesabını muhasebe gözüyle tutuyor; Mihenk için ters çevirme yok.) |
| Açıklama | `explanation` (+ `DEK.detailText`, `DTS.firmOriginalText`) |
| Karşı taraf adı | `enrichmentInformation[MUS].corrNameSurnameText` ya da `EFT.senderName/receiverName` |
| Karşı IBAN | `enrichmentInformation[CORRACNT].corrIBAN` |
| Karşı VKN/TCKN | `corrVKN` / `corrTCKN` |
| Bakiye | `balanceAfterTransaction` |
| Benzersiz no | **Yok.** `transactionId` kategori kodudur (WPDT), benzersiz değil. Öneri (api-notlari.md): `IBAN + transactionReferenceId + transactionInstanceId + activityDate + yön + tutar` özeti |

Hata kodları (HTTP 400, `result.reasonCode`): 2/3 consent zorunlu · 4 clientId–consentId uyuşmuyor · 5 şube+hesap birlikte · 6 hesap hatalı · 13 hesap–IBAN uyumsuz · 15 transactionId hatalı · 18 aralık > 30 gün · 19 tarih hatalı · 21 tarih biçimi hatalı · 30 pageSize > 500. Diğer: 401, 405, 429, 500. Onay bekleyen anahtar: HTTP 401, `x-ca-err: 3003201`, `{"error":"invalid_client"}`. Kaynak: api_spec.md, api-notlari.md.

---

## 2. VakıfBank — Online Ekstre (SOnlineEkstreServis)

**Bağlayıcı yazmaya yeter mi: EVET.**

| Konu | Değer | Kaynak |
|---|---|---|
| WSDL | `https://vbservice.vakifbank.com.tr/HesapHareketleri.OnlineEkstre/SOnlineEkstreServis.svc?wsdl` | [TurksoftApp VakıfSrv/ConnectedService.json](https://github.com/crazypsp/TurksoftApp/tree/HEAD/TurkSoft.Business/Connected%20Services) |
| Uç nokta | `https://vbservice.vakifbank.com.tr/HesapHareketleri.OnlineEkstre/SOnlineEkstreServis.svc` | Reference.cs `GetEndpointAddress` |
| Bağlama | WCF **WSHttpBinding**, `Security.Mode = Transport`, istemci kimliği `None` → SOAP 1.2 + WS-Addressing | Reference.cs `GetBindingForEndpoint` |
| Servis ad alanı | `Peak.Integration.ExternalInbound.Ekstre` | Reference.cs `ServiceContractAttribute` |
| Veri ad alanı | `http://schemas.datacontract.org/2004/07/Peak.Integration.ExternalInbound.Ekstre.DataTransferObjects` | Reference.cs |
| Action | `Peak.Integration.ExternalInbound.Ekstre/ISOnlineEkstreServis/GetirHareket` | Reference.cs |
| İşlemler | `GetirHareket`, `GetirHareketOzet`, `GetirDekont` (`DtoDekontSorgu{KurumKullanici,Sifre,MusteriNo,IslemNo}` → PDF bayt dizisi), `GetirYatirimHareket`, `GetirUyeIsyeriHareket` (POS) | Reference.cs |
| Kimlik | Gövdede `MusteriNo`, `KurumKullanici`, `Sifre` | Reference.cs `DtoEkstreSorgu` |
| Tarih biçimi | `yyyy-MM-dd HH:mm` (ör. `2026-10-01 00:00`) | [crazypsp/Finans VakifBankStatementProvider.cs](https://github.com/crazypsp/Finans/blob/HEAD/Finans.Infrastructure/Banking/Managers/VakifBankStatementProvider.cs) ve SAP `YCL_EHO_GET_RECEIPTS_VKB` (ikisi aynı) |
| Başarı kodu | `IslemKodu == "VBB0001"` | Finans VakifBankStatementProvider.cs |
| Tarih aralığı / sayfalama / hız | **TEYİTSİZ** | — |

İstek alanları (`DtoEkstreSorgu`, WSDL sırası): `MusteriNo`, `KurumKullanici`, `Sifre`, `SorguBaslangicTarihi`, `SorguBitisTarihi`, `HesapNo`, `HareketTipi`, `EnDusukTutar` (decimal), `EnYuksekTutar` (decimal), `HesapAcilisTarihi`, `HesapKapanisTarihi`.

SOAP zarfı (çalışan topluluk kodundan birebir, [selimhallac/vakifbank src/Vakifbank.php](https://github.com/selimhallac/vakifbank/blob/HEAD/src/Vakifbank.php); `Content-Type: application/soap+xml;`):
```xml
<soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope" xmlns:peak="Peak.Integration.ExternalInbound.Ekstre" xmlns:peak1="http://schemas.datacontract.org/2004/07/Peak.Integration.ExternalInbound.Ekstre.DataTransferObjects">
  <soap:Header xmlns:wsa="http://www.w3.org/2005/08/addressing">
    <wsa:Action>Peak.Integration.ExternalInbound.Ekstre/ISOnlineEkstreServis/GetirHareket</wsa:Action>
    <wsa:To>https://vbservice.vakifbank.com.tr/HesapHareketleri.OnlineEkstre/SOnlineEkstreServis.svc</wsa:To>
  </soap:Header>
  <soap:Body>
    <peak:GetirHareket>
      <peak:sorgu>
        <peak1:MusteriNo>{musteriNo}</peak1:MusteriNo>
        <peak1:KurumKullanici>{kullanici}</peak1:KurumKullanici>
        <peak1:Sifre>{sifre}</peak1:Sifre>
        <peak1:SorguBaslangicTarihi>{yyyy-MM-dd HH:mm}</peak1:SorguBaslangicTarihi>
        <peak1:SorguBitisTarihi>{yyyy-MM-dd HH:mm}</peak1:SorguBitisTarihi>
        <peak1:HesapNo>{hesapNo}</peak1:HesapNo>
        <peak1:HareketTipi xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"/>
      </peak:sorgu>
    </peak:GetirHareket>
  </soap:Body>
</soap:Envelope>
```

Yanıt zarfının başı (aynı PHP kodunun temizlediği metinden birebir):
```xml
<s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope" xmlns:a="http://www.w3.org/2005/08/addressing">
  <s:Header><a:Action s:mustUnderstand="1">Peak.Integration.ExternalInbound.Ekstre/ISOnlineEkstreServis/GetirHareketResponse</a:Action></s:Header>
  <s:Body>
    <GetirHareketResponse xmlns="Peak.Integration.ExternalInbound.Ekstre">
      <GetirHareketResult xmlns:b="http://schemas.datacontract.org/2004/07/Peak.Integration.ExternalInbound.Ekstre.DataTransferObjects" xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
        <b:IslemKodu>…</b:IslemKodu> <b:IslemAciklamasi>…</b:IslemAciklamasi>
        <b:Hesaplar><b:DtoEkstreHesap> … <b:Hareketler><b:DtoEkstreHareket> … </b:DtoEkstreHareket></b:Hareketler> … </b:DtoEkstreHesap></b:Hesaplar>
      </GetirHareketResult>
    </GetirHareketResponse>
  </s:Body>
</s:Envelope>
```
(`GetirHareketResult` adı SAP `YCL_EHO_GET_RECEIPTS_VKB` tipinden; iç yapı Reference.cs'den.)

Yanıt alanları (Reference.cs, birebir):
- `DtoEkstreCevap`: `BankaKodu, BankaAdi, BankaVergiDairesi, BankaVergiNumarasi, IslemKodu, IslemAciklamasi, Hesaplar[]`
- `DtoEkstreHesap`: `HesapNo, MusteriNo, MusteriUnvani, SubeKodu, SubeAdi, DovizTipi, AcilisBakiye, CariBakiye, KullanilabilirBakiye, KrediliBankomatLimiti, KrediBakiyesi, SonHareketTarihi, VergiKimlikNumarasi, Hareketler[], HesapTipi, VadeBaslangicTarihi, VadeBitisTarihi, VadeSonuBeklenenBakiye, VadeFaizOrani, VadeSonuOdenecekBrutFaizTutari, VadeSonuOdenecekNetFaizTutari, HesapNoIban, HesapDurumu, Stopaj, VadeSonuVergiTutari, HesapTemditTuru, HesapRumuzu, VadeliHesapVirmanHesapNo`
- `DtoEkstreHareket`: `IslemTarihi, IslemNo, IslemAdi, Tutar (decimal), BorcAlacak, Aciklama, IslemOncesiBakiye, IslemSonrasıBakiye` (**Türkçe ı ile**), `IslemYeri, IslemKanal, KartNo, IslemKodu, Detaylar, IslemTarihZamani, Id (long)`

Mihenk eşlemesi:

| Mihenk | VakıfBank |
|---|---|
| Tarih/saat | `IslemTarihi` (SAP `yyyy-MM-dd HH:mm:ss` gibi kesiyor) |
| Tutar / yön | `Tutar` (eksi olabilir, SAP işaretini atıyor) + `BorcAlacak` (`A`/`B`) |
| Açıklama | `Aciklama`, `IslemAdi` |
| Karşı taraf | `Detaylar` anahtar–değer listesi (`KeyValueOfstringstring`): `GonderenKimlikNumarasi`, `AliciKimlikNumarasi`, `GonderenIbanKumarasi`, `AliciIbanKumarasi` (bankanın yazımıyla "Kumarasi"), `GonderenSubeKodu`, `SwiftKodu` (SAP kodundan; anahtar listesi tam değil) |
| Bakiye | `IslemSonrasıBakiye` |
| Benzersiz no | `Id` (long) ve `IslemNo` |

---

## 3. QNB (Finansbank) — Maestro TeknikBaglantiService

**Bağlayıcı yazmaya yeter mi: EVET.**

| Konu | Değer | Kaynak |
|---|---|---|
| WSDL | `https://fbmaestro.qnb.com.tr/MaestroCoreEkstre/services/TeknikBaglantiService?wsdl` | [TurksoftApp QNBSrv/ConnectedService.json](https://github.com/crazypsp/TurksoftApp/tree/HEAD/TurkSoft.Business/Connected%20Services) |
| Uç noktalar | WSDL'deki: `http://fbmaestro.qnb.com.tr:9086/MaestroCoreEkstre/services/TeknikBaglantiService.TeknikBaglantiServiceHttpSoap11Endpoint/` (ve `…Soap12Endpoint/`). Topluluk kodunun HTTPS seçeneği: `https://fbmaestro.qnb.com.tr/MaestroCoreEkstre/services/TeknikBaglantiService.TeknikBaglantiServiceHttpSoap11Endpoint/` | QNBSrv/Reference.cs; [crazypsp/Finans QnbMaestroStatementProvider.cs](https://github.com/crazypsp/Finans/blob/HEAD/Finans.Infrastructure/Banking/Managers/QnbMaestroStatementProvider.cs) |
| SOAPAction | `"urn:getTransactionInfo"` | QnbMaestroStatementProvider.cs |
| Ad alanı | `http://teknikbaglantiekstre.genericekstrenew2.driver.maestro.ibtech.com` (işlem); veri tipleri `http://getextendeddetailtransactioninfo.model.genericekstrenew2.driver.maestro.ibtech.com/xsd`; iç elemanlar **unqualified** | Reference.cs |
| Kimlik | Gövdede `userName`, `password` (Paraşüt kılavuzu: banka WSDL adresi + kullanıcı + şifre verir) | Reference.cs |
| Tarih biçimi | `xs:dateTime` (ör. `2026-10-01T00:00:00`) | Reference.cs (`Nullable<DateTime>`), SAP `YCL_EHO_GET_RECEIPTS_QNB` |
| Hata | `errorCode` boş ya da `"0"` başarı sayılıyor (topluluk kodu); başarı değerinin kesin tanımı **TEYİTSİZ** | QnbMaestroStatementProvider.cs |
| Tarih aralığı / sayfalama / hız | **TEYİTSİZ** | — |

> Çelişki: SAP ABAP kodu `userName`'i `transactionInfo`'nun **dışına** koyuyor; WSDL'den üretilmiş Reference.cs ve C# kodu **içine** koyuyor. WSDL esas alınmalı → içine.

SOAP zarfı (Reference.cs sırası + C# kodundaki zarf; WSDL'e göre sıra `password, transactionInfoInputType, userName`):
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ns="http://teknikbaglantiekstre.genericekstrenew2.driver.maestro.ibtech.com">
  <soapenv:Header/>
  <soapenv:Body>
    <ns:getTransactionInfo>
      <transactionInfo>
        <password>{sifre}</password>
        <transactionInfoInputType>
          <accountNo>{hesapNo}</accountNo>
          <endDate>2026-10-19T23:59:59</endDate>
          <iban>{IBAN, isteğe bağlı}</iban>
          <startDate>2026-10-19T00:00:00</startDate>
        </transactionInfoInputType>
        <userName>{kullanici}</userName>
      </transactionInfo>
    </ns:getTransactionInfo>
  </soapenv:Body>
</soapenv:Envelope>
```
(`transactionInfoInputType` içi sıra WSDL'de alfabetik: `accountNo, endDate, iban, startDate`. C# topluluk kodu `accountNo, startDate, endDate, iban` sırasıyla gönderiyor ve çalıştığı belirtiliyor; sıra katılığı **TEYİTSİZ**, WSDL sırası güvenli.)

Yanıt: `ns:getTransactionInfoResponse` → `return` (`GetExtendedDetailAccountInfoResponse`):
- `return`: `accountInfoReturnType[]`, `errorCode`, `errorDescription`
- `accountInfoReturnType` (`ExtendedDetailAccountInfo`): `accountBalance, accountCurrencyCode, accountNo, accountTitle, accountType, branchCode, branchName, customerNo, iban, lastTrxClosingBalance, lastTrxClosingProcessDate, lastTrxClosingProcessTime, lastTrxDate, lastTrxDebitOrCreditCode, openingBalance, openingBalanceDate, previousTrxDebitOrCreditCode, previousTrxOpeningBalance, previousTrxOpeningBalanceDate, reserveField1..3, transactions[], usableOVDPLimit, usableTotalAmount`
- `transactions` (`ExtendedDetailTransactionInfo`): `currencyCode, ddFirmInvoiceNumber, ddFirmReferenceNumber, ddReferenceNumber, debitOrCreditCode, eftInquiryNumber, eftReturnDescription, muhasebeFisNo, operationName, opponentAccountNo, opponentBank, opponentBankCode, opponentBranch, opponentBranchCode, opponentCityCode, opponentIBAN, opponentTAXNoPIDNo, processCode, productOperationRefNo (long), reserveField1..3, statementTransactionOrder (long), transactionAmount (decimal), transactionBalance (decimal), transactionDate (dateTime), transactionDescription, transactionExchangeRate, transactionId (long), valueDate (dateTime), vheFirmDocumentNumber, vheTrxDescription`

> Dikkat: SAP ABAP deposundaki tip `accountInfos` adını kullanıyor, Reference.cs `accountInfoReturnType`. C# topluluk kodu `accountInfos` adını arıyor. Yerel ad **TEYİTSİZ** → ayrıştırıcı her ikisini de kabul etmeli.

Mihenk eşlemesi: tarih `transactionDate` · valör `valueDate` · tutar `transactionAmount` · yön `debitOrCreditCode` (`A`/`B`; SAP "bankada ters çalışıyor" notuyla çeviriyor, Mihenk açısından **TEYİTSİZ**, pilotla doğrulanmalı) · açıklama `transactionDescription` / `eftReturnDescription` / `vheTrxDescription` · karşı IBAN `opponentIBAN` · karşı VKN/TCKN `opponentTAXNoPIDNo` · karşı banka `opponentBank` · bakiye `transactionBalance` · benzersiz `transactionId` (+ `muhasebeFisNo`).

Enpara: önceki belgede Paraşüt'ün "3. Şablon"u QNB ile aynı servis diye geçiyor. Enpara'nın aynı uç noktayı kullandığına dair teknik kaynak **bulunamadı (TEYİTSİZ)**.

---

## 4. Akbank — Elektronik Ekstre (Extre_InterfaceService)

**Bağlayıcı yazmaya yeter mi: EVET** (yanıt kök eleman adı TEYİTSİZ, alan sırası iki kaynakta tutarlı).

| Konu | Değer | Kaynak |
|---|---|---|
| Uç nokta / WSDL | `https://firmahizmetleri.akbank.com/Extre_InterfaceService/Service.asmx` (`?wsdl`) | [crazypsp/Finans AkbankSrv](https://github.com/crazypsp/Finans/tree/HEAD/Finans.Infrastructure/Connected%20Services/AkbankSrv), [selimhallac/akbank-web-ekstre-hesap-hareketleri](https://github.com/selimhallac/akbank-web-ekstre-hesap-hareketleri/blob/HEAD/src/Akbank.php) |
| Protokol | ASMX, SOAP 1.1 ve 1.2 (Reference.cs iki uç tanımı), ad alanı `http://tempuri.org/`, SOAPAction `http://tempuri.org/GetExtreWithParams` | Reference.cs |
| İşlemler | `GetExtre()` (parametresiz) ve `GetExtreWithParams(urf, hesapNo, dovizKodu, subeKodu, baslangicTarihi, bitisTarihi)`; ikisi de serbest XML (`XmlNode`) döner | Reference.cs |
| Kimlik | **HTTP Basic** (`Authorization: Basic base64(kullanici:sifre)`) | selimhallac Akbank.php (çalışan GET `…/service.asmx/GetExtre`); Finans kodu da Basic kullanıyor ama yorumda "emin değiliz" diyor |
| `urf` | Şubenin verdiği Ürün Referans Kodu (önceki belge); boş gönderen kod da var | Finans / TurksoftApp |
| Tarih biçimi | 20 hane: `yyyyMMdd` + `HHmmss` + 6 hane (ör. `20261019000000000000`, `20261019235959000000`) | SAP `YCL_EHO_GET_RECEIPTS_AKB` ve Finans kodu (ikisi aynı biçim) |
| Tarih aralığı | Topluluk C# kodu **günü gününe** döngüyle çekiyor; banka sayfası "son 3 ay" (önceki belge). Gün başına sınır **TEYİTSİZ** | Finans AkbankStatementProvider.cs |
| IP kısıtı | Var (Akbank Direkt'te yazılımın IP'si tanımlanır) | önceki belge |

SOAP 1.1 zarfı (Reference.cs'ten türetildi, ASMX document/literal; **canlıda denenmedi**):
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <GetExtreWithParams xmlns="http://tempuri.org/">
      <urf>{URF}</urf>
      <hesapNo>{hesapNo}</hesapNo>
      <dovizKodu></dovizKodu>
      <subeKodu></subeKodu>
      <baslangicTarihi>20261019000000000000</baslangicTarihi>
      <bitisTarihi>20261019235959000000</bitisTarihi>
    </GetExtreWithParams>
  </soap:Body>
</soap:Envelope>
```
HTTP başlıkları: `Content-Type: text/xml; charset=utf-8`, `SOAPAction: "http://tempuri.org/GetExtreWithParams"`, `Authorization: Basic …`.

Yanıt yapısı (SAP tipinden; eleman sırası C# kodunun indeksleriyle **birebir tutarlı**, çapraz doğrulandı):
- Sarmal: `GetExtreWithParamsResponse/GetExtreWithParamsResult/…/Hesap` (aradaki kök adı SAP'ta `HesapHareketleriResult` olarak eşlenmiş; gerçek XML kök adı **TEYİTSİZ**. selimhallac kodu kökün altında doğrudan `Hesap` arıyor.)
- `Hesap` çocukları (sıra 0'dan): `SqlID, HesapTuruAdi, HesapNo, URF, SubeKodu, SubeAdi, DovizKodu, IBAN, AcilisIlkBakiye, AcilisGunBakiye, CariBakiye, Bakiye, BlokeMeblag, HesapAcilisTarihi, SonHareketTarihi, HesapTuruKodu, LastTmSt, AktifFlag, DetayFlag, DekontBilgiFlag, VadeliHareketlerIslensin, Detay[]`
- `Detay` çocukları (sıra 0'dan): `ValorTarihi, IslemTarihi, ReferansNo, KarsiHesapIBAN, Tutar, SonBakiye, Aciklama, VKN, TutarBorcAlacak, SonBakiyeBorcAlacak, FonksiyonKodu, MT940FonksiyonKodu, FisNo, OzelAlan1, OzelAlan2, AboneNo, FaturaNo, FaturaDonem, FaturaSonOdemeTarihi, LehdarVKN, LehdarTCKN, AmirVKN, AmirTCKN, BorcluIBAN, AlacakliIBAN, DekontBorcAciklama, DekontAlacakAciklama, IslemiYapanSube, HareketDurumu, TimeStamp`
- Döviz kodu TL için `YTL` (SAP kodu `TRY`→`YTL` çeviriyor).
- Ondalık ayırıcı **virgül** (`Tutar`, `SonBakiye`: SAP `,`→`.` çeviriyor).

Mihenk eşlemesi: tarih `IslemTarihi` (`yyyyMMdd`) + saat `TimeStamp[8..14]` · valör `ValorTarihi` · tutar `Tutar` · yön `TutarBorcAlacak` (`+` alacak, `-` borç) · açıklama `Aciklama` + `DekontAlacakAciklama`/`DekontBorcAciklama` · karşı IBAN: girişte `BorcluIBAN`, çıkışta `AlacakliIBAN` · karşı VKN `VKN` (ayrıca `AmirVKN/TCKN`, `LehdarVKN/TCKN`) · bakiye `SonBakiye` · benzersiz `FisNo` (+ `ReferansNo`).

---

## 5. Kuveyt Türk — BOA AccountStatementService

**Bağlayıcı yazmaya yeter mi: EVET.**

| Konu | Değer | Kaynak |
|---|---|---|
| WSDL | `https://boa.kuveytturk.com.tr/BOA.Integration.WCFService/BOA.Integration.AccountStatement/AccountStatementService.svc?wsdl` | [crazypsp/Finans KuveytTurkSrv](https://github.com/crazypsp/Finans/tree/HEAD/Finans.Infrastructure/Connected%20Services/KuveytTurkSrv) |
| Uç nokta | `…/AccountStatementService.svc/Basic` (WCF basicHttpBinding, SOAP 1.1) | TurksoftApp KuveytTurkSrv/Reference.cs |
| SOAPAction | `http://boa.net/BOA.Integration.CoreBanking.Teller/Service/IAccountStatementService/GetAccountStatement` | Reference.cs |
| Ad alanları | servis `http://boa.net/BOA.Integration.CoreBanking.Teller/Service`; temel `http://schemas.datacontract.org/2004/07/BOA.Integration.Base`; model `http://schemas.datacontract.org/2004/07/BOA.Integration.Model.CoreBanking.Teller` | Reference.cs |
| Kimlik | Gövdede `ExtUName`, `ExtUPassword` (temel sınıf `RequestModelBase`) | Reference.cs |
| İstek | `AccountStatetmentRequest` (bankanın yazımıyla "Statetment"): `AccountNumber` (int, müşteri no), `AccountSuffix` (string, ek no), `BeginDate`, `EndDate` (dateTime), `Iban` | Reference.cs |
| Hesap biçimi | Topluluk kodu `98886207-1` gibi `müşteriNo-ekNo` biçimini bölüyor | Finans KuveytTurkStatementProvider.cs |
| Tarih aralığı / sayfalama / hız | **TEYİTSİZ** | — |

SOAP zarfı (aynı BOA platformundaki Emlak Katılım için çalışan PHP kodundan birebir; Kuveyt Türk aynı WSDL ad alanlarını ve aynı işlemi kullanıyor, adres farklı. Kuveyt için `AccountNumber`/`AccountSuffix` eklendi, sıra **TEYİTSİZ**, DataContract alfabetik sırası önerilir):
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ser="http://boa.net/BOA.Integration.CoreBanking.Teller/Service" xmlns:boa="http://schemas.datacontract.org/2004/07/BOA.Integration.Base" xmlns:boa1="http://schemas.datacontract.org/2004/07/BOA.Integration.Model.CoreBanking.Teller">
  <soapenv:Header/>
  <soapenv:Body>
    <ser:GetAccountStatement>
      <ser:request>
        <boa:ExtUName>{kullanici}</boa:ExtUName>
        <boa:ExtUPassword>{sifre}</boa:ExtUPassword>
        <boa1:AccountNumber>{musteriNo}</boa1:AccountNumber>
        <boa1:AccountSuffix>{ekNo}</boa1:AccountSuffix>
        <boa1:BeginDate>2026-10-01</boa1:BeginDate>
        <boa1:EndDate>2026-10-19</boa1:EndDate>
      </ser:request>
    </ser:GetAccountStatement>
  </soapenv:Body>
</soapenv:Envelope>
```
Kaynak: [selimhallac/Emlak-Katilim-Online-Web-Ekstre](https://github.com/selimhallac/Emlak-Katilim-Online-Web-Ekstre). Başlık: `SOAPAction: http://boa.net/BOA.Integration.CoreBanking.Teller/Service/IAccountStatementService/GetAccountStatement`, `Content-Type: text/xml;charset=UTF-8`.

Yanıt (Reference.cs): `GetAccountStatementResponse/GetAccountStatementResult` →
- `Success` (bool), `Results[]` (`ErrorCode, ErrorMessage, Exception, IsFriendly, Params, Severity`), `ErrorCode`, `ErrorMessage`, `Value[]`
- `Value/AccountContract`: `AccountAlias, AccountNumber, AccountSuffix, Balance, BalanceDetails[] (BeginAmount, EndAmount, FEC, TranDate), BranchId, BranchName, CredentialError, Details[], DisplayName, FEC, FECLongName, FECName, IBAN, InternalPeriodEnd, LastRenewedDate, LastTranDate, MaturityEnd, OpenDate, ParentProductCode, ProductCode, TCKNorTaxNumber`
- `Details/TransactionDetailContract`: `Amount, BranchId, BranchName, BusinessKey (decimal), ChannelId, Credit, CurrentBalance, Debit, Description, FEC, FECName, Iban, ReceiverIdentityNumber, ReceiverName, ResourceCode, SenderIdentityNumber, SenderName, SwiftTransactionCode, SystemDate, TranDate, TranRef, TranType, ValueDate`

Mihenk eşlemesi: tarih `TranDate` · valör `ValueDate` · tutar/yön `Credit` > 0 alacak, `Debit` > 0 borç · açıklama `Description` · karşı ad `SenderName`/`ReceiverName` · karşı TCKN/VKN `SenderIdentityNumber`/`ReceiverIdentityNumber` · karşı IBAN `Iban` (anlamı **TEYİTSİZ**: işlem karşı IBAN'ı mı, hesap IBAN'ı mı) · bakiye `CurrentBalance` · benzersiz `BusinessKey` (+ `TranRef`).

---

## 6. Albaraka Türk — HesapBilgileriService

**Bağlayıcı yazmaya yeter mi: EVET** (ad alanı çelişkisi canlıda çözülmeli).

| Konu | Değer | Kaynak |
|---|---|---|
| WSDL | `https://eservice.albarakaturk.com.tr:10214/invoiceincomingsite/HesapBilgileriService.asmx?wsdl` | [TurksoftApp AlbarakaTurkSrv/ConnectedService.json](https://github.com/crazypsp/TurksoftApp/tree/HEAD/TurkSoft.Business/Connected%20Services) |
| Uç nokta | `https://eservice.albarakaturk.com.tr:10214/invoiceincomingsite/HesapBilgileriService.asmx` (WSDL içindeki adres iç ağ: `http://10.144.40.130/…`, kullanılamaz) | [crazypsp/Finans AlbarakaStatementProvider.cs](https://github.com/crazypsp/Finans/blob/HEAD/Finans.Infrastructure/Banking/Managers/AlbarakaStatementProvider.cs), Reference.cs |
| Ad alanı | WSDL: `http://services.albaraka.com/`, SOAPAction boş (`""`). Elle yazılmış C# zarfı `http://tempuri.org/` kullanıyor → **çelişki**, WSDL esas alınmalı | Reference.cs vs AlbarakaStatementProvider.cs |
| İşlemler | `getHesapDetaylari(pId, pIdPass, pParams{musteriNo})`, `getHesapHareketleri(pId, pIdPass, pParams{musteriNo, hesapNo, basTarih, sonTarih})` | Reference.cs |
| Kimlik | `pId` (kullanıcı), `pIdPass` (şifre) gövdede | Reference.cs |
| Tarih biçimi | `yyyyMMdd` | Finans kodu ve SAP `YCL_EHO_GET_RECEIPTS_ALB` (ikisi aynı) |
| `hesapNo` | `müşteriNo-ekNo` biçiminde girilirse tire sonrası (ek no) gönderiliyor | Finans kodu, SAP (`hesap_no = suffix`) |
| Tarih aralığı / sayfalama / hız | **TEYİTSİZ** | — |

SOAP zarfı (Reference.cs'e göre: sarmal ve `pId/pIdPass/pParams` `services.albaraka.com` ad alanında, `pParams` içi **unqualified**; canlıda denenmedi):
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:alb="http://services.albaraka.com/">
  <soap:Body>
    <alb:getHesapHareketleri>
      <alb:pId>{kullanici}</alb:pId>
      <alb:pIdPass>{sifre}</alb:pIdPass>
      <alb:pParams>
        <musteriNo>{musteriNo}</musteriNo>
        <hesapNo>{ekNo}</hesapNo>
        <basTarih>20261001</basTarih>
        <sonTarih>20261019</sonTarih>
      </alb:pParams>
    </alb:getHesapHareketleri>
  </soap:Body>
</soap:Envelope>
```

Yanıt (Reference.cs): `getHesapHareketleriResponse/responseData` (`hesapHareketleriResponse`, ayrıca `result{code, message}`) → `hesapHareketleri[]` (`hesapHareketleriHesap`: `musteriNo, hesapNo, hesapIban, hesapTcknVkn, hesapHareket[]`) → `hesapHareket`: `tarih, saat, aciklama, borcAlacak, islemTutari, bakiye, karsiHesapIban, karsiHesapTcknVkn, fisNo, Code, SeqNumber, BranchCode, ChannelCode, islemsonrasibakiye, MuhRefNo`. Hesap listesi (`getHesapDetaylari`): `musteriNo, hesapNo, sube, paraCinsi, hesapTipi, blokeBakiye, cekBakiye, senetBakiye, toplamBakiye, kullanabilirBakiye, sonHareketTarihi, vadeBaslangicTarih, vadeBitisTarihi, vadeSuresi, hesapEsnekVade, esnekVadeGunSayisi, iban, tcknVkn`.

Mihenk eşlemesi: tarih `tarih` + `saat` · tutar `islemTutari` · yön `borcAlacak` · açıklama `aciklama` · karşı IBAN `karsiHesapIban` · karşı VKN/TCKN `karsiHesapTcknVkn` · bakiye `islemsonrasibakiye` · benzersiz `fisNo` / `MuhRefNo`.

---

## 7. Ziraat Bankası — HEK_NKYWS HesapHareketleri

**Bağlayıcı yazmaya yeter mi: KISMEN** (istek tarafı tam; yanıt alan adlarının harf büyüklüğü ve bazı tarih biçimleri TEYİTSİZ).

| Konu | Değer | Kaynak |
|---|---|---|
| WSDL | `https://hesap.ziraatbank.com.tr/HEK_NKYWS/HesapHareketleri.asmx?WSDL` | [ravanbabayev/ziraat-bank-php](https://github.com/ravanbabayev/ziraat-bank-php/blob/HEAD/src/ZiraatBankService.php) |
| Protokol | ASMX, SOAP 1.1. Aynı servis ASMX **HTTP POST form** ile de çağrılıyor: `POST …/HesapHareketleri.asmx/HesapEkstre`, gövde `KurumKod=…&Sifre=…&HesapNo=…&BaslangicTarihi=…&BitisTarihi=…` | [selimhallac/ziraatbankasi-web-eksktre-hesap-hareketleri](https://github.com/selimhallac/ziraatbankasi-web-eksktre-hesap-hareketleri/blob/HEAD/ziraatbank.class.php), [alialaca/ziraatbank-web-ekstre](https://github.com/alialaca/ziraatbank-web-ekstre/blob/HEAD/lib/index.js) |
| SOAP ad alanı | **TEYİTSİZ** (kodlar WSDL'i PHP SoapClient'a veriyor, ad alanı yazılmamış; WSDL indirilip okunmalı) | — |
| Kimlik | Banka verdiği "kurum kodu" (kullanıcı adı) + şifre. Alan adı işleme göre değişiyor: `KurumKod`/`kurumKod`/`kullaniciKod` + `Sifre`/`sifre` (WSDL'de tutarsız, birebir korunmalı) | ravanbabayev (WSDL `__getTypes` ile teyit edildiği yazıyor) |
| Tarih aralığı | "en fazla **21 gün** geriye" (topluluk kodu yorumu; resmi kaynak değil) | selimhallac ziraatbank.class.php |
| Sayfalama | Yalnız `GetirPdfDekont*`, `SorgulaMutabakatDetayZamanIle`, `SorgulaMahsupDetayZamanIle` işlemlerinde (`suAnkiSayfa/sayfaBoyutu`, `sayfaNumarasi/sayfaSatirSayisi`) | ravanbabayev |

19 işlemin istek alanları (WSDL sırası, birebir; ravanbabayev):
```text
HesapEkstre                    { KurumKod; Sifre; HesapNo; BaslangicTarihi; BitisTarihi }                  (string)
HesapEkstreZamanIle            { kurumKod; sifre; hesapNo; baslangicZamani(dateTime); bitisZamani(dateTime) }
MusteriHesapHareket            { KurumKod; sifre; MusteriNo(int); EkNo(int); SubeKodu; BaslangicTarihi; BitisTarihi }
SorgulaDetayWS / SorgulaOzetWS { kullaniciKod; sifre; ibanNo; baslangicTarihi; bitisTarihi }
SorgulaDetayWSZamanIle / SorgulaOzetWSZamanIle { kullaniciKod; sifre; ibanNo; baslangicZamani(dateTime); bitisZamani(dateTime) }
HareketSorgulaVknIle           { kullaniciKod; sifre; vkn; muhasebeTarihi(dateTime) }
SorgulaHesapHareket            { musteriNo(int); ekNo(int); baslangicTr; bitisTr; kurumKod; sifre }
SorgulaHesapHareketZamanIle    { musteriNo(int); ekNo(int); baslangicZamani(dateTime); bitisZamani(dateTime); kurumKod; sifre; iptalFisGetir("H"/"E") }
SorgulaHesapHareketZamanIleHizli { musteriNo; ekNo; baslangicZamani; bitisZamani; kurumKod; sifre }
GetirTanimliHesapBilgileri     { kurumKod; sifre; tcknVkn }
GetirPdfDekont(ZamanIle)       { kurumKod; sifre; musteriNo; ekNo; baslangic…; bitis…; suAnkiSayfa; sayfaBoyutu; dosyaFormati }
GetirPdfDekontDekontNoIle      { kurumKod; sifre; musteriNo; ekNo; dekontNo }
SorgulaGelenParaTransfer       { kurumKod; sifre; musteriNo; ekNo; gonderenBankaKodu; islemTarihi; sorguNo; islemSubeKodu; fisReferansi }
SorgulaMutabakatOzetZamanIle   { kurumKod; sifre; musteriNo; ekNo; tarih }
SorgulaMutabakatDetayZamanIle  { kurumKod; sifre; musteriNo; ekNo; tarih; sayfaNumarasi; sayfaSatirSayisi; kayitDurumu }
SorgulaMahsupDetayZamanIle     { kurumKod; sifre; musteriNo; ekNo; baslangicZamani; bitisZamani; sayfaNumarasi; sayfaSatirSayisi; kayitDurumu }
```

Tarih biçimleri (kaynaklar çelişiyor → **TEYİTSİZ**, pilotla sabitlenmeli):
- `SorgulaHesapHareket.baslangicTr/bitisTr`: `dd.MM.yyyy` (ravanbabayev, canlı entegrasyondan).
- `*ZamanIle` dateTime: `yyyy-MM-ddTHH:mm:ss` (ravanbabayev ve SAP).
- `HesapEkstre.BaslangicTarihi`: selimhallac README `Y-m-d` diyor, alialaca README `YYYY.AA.GG` diyor.

Önerilen akış (ravanbabayev `getHesaplarVeHareketleri`): `GetirTanimliHesapBilgileri(tcknVkn)` → hesap listesi → her hesap için `SorgulaHesapHareketZamanIle(musteriNo, ekNo, …)`.

Yanıtlar:
- `GetirTanimliHesapBilgileriResult`: `CevapKodu` (0 = başarı), `CevapAciklama`, `KullaniciHesapBilgileri/KullaniciHesapBilgi[]` → `MusteriNo, EkNo, IBAN, SubeKodu, SubeAdi, DovizKodu` (ravanbabayev). Tek kayıtta dizi yerine tek nesne gelebiliyor.
- `SorgulaHesapHareketZamanIleResult` (SAP tipinden, harf büyüklüğü TEYİTSİZ): `HataKodu, HataAck, SubeKodu, SubeAdi, HesapNo, AcilisBakiye, CariBakiye, BlokeliBakiye, HareketDetay/HareketlerDetay[]` → `DovizTipi, IslemTarihi, Tutar, BorcAlacak, Aciklama, MuhasebeTarihi, ValorTarihi, TimeStamp, IslemAciklama, TcknVkn, AdUnvan, IBAN, MuhRef, ProgramKod, DekontNo, IslemTipi, KayitDurumu, IptalZamani, Bakiye`.
- `SorgulaHesapHareketResult` (ravanbabayev): `CevapKodu`, `hareketdetay/HareketlerDetay[]` → `islemTarihi` (ör. `2026-07-22T21:59:54`), `aciklama`, `tutar`, `borcAlacak` (`A`/`B`). Benzersiz alan adı kodda tahminle aranıyor (`referans`/`referansNo`/`entegrasyonReferansNo`) → **TEYİTSİZ**.
- `HesapEkstre` örnek yanıtı (selimhallac README, maskeli, birebir alan adları): kök `BankaKodu` (010), `BankaAdi`, `BankaVergiDairesi`, `BankaVergiNumarasi`, `hataKodu` (00), `hataAciklamasi`, `Bakiye{subeKodu, SubeAdi, HesapNo, DovizTipi, AcilisBakiye, CariBakiye, Detay{HareketlerDetay[]{IslemTarihi (yyMMdd, ör. 201221), Valor (yyMMdd), Referans, Aciklama, Tutar, BorcAlacak, IslemTipi (ör. 211M), TimeStamp (ör. 202012211572FNCC636), musteriVergiNumarasi, Bakiye}}}`.

```xml
<!-- HesapEkstre yanıt iskeleti (selimhallac README'deki SimpleXML dökümünden geri kuruldu; kök eleman adı TEYİTSİZ) -->
<…>
  <BankaKodu>010</BankaKodu>
  <hataKodu>00</hataKodu>
  <hataAciklamasi>Hata Yok</hataAciklamasi>
  <Bakiye>
    <subeKodu>…</subeKodu><HesapNo>…</HesapNo><DovizTipi>TRY</DovizTipi>
    <AcilisBakiye>…</AcilisBakiye><CariBakiye>…</CariBakiye>
    <Detay>
      <HareketlerDetay>
        <IslemTarihi>201221</IslemTarihi><Valor>201222</Valor><Referans>00000000</Referans>
        <Aciklama>Gönd: …</Aciklama><Tutar>8208.00</Tutar><BorcAlacak>A</BorcAlacak>
        <IslemTipi>211M</IslemTipi><TimeStamp>202012211572FNCC636</TimeStamp>
        <musteriVergiNumarasi>…</musteriVergiNumarasi><Bakiye>472417.99</Bakiye>
      </HareketlerDetay>
    </Detay>
  </Bakiye>
</…>
```
Not: Karşı taraf adı bazı işlemlerde yalnız açıklamada geçiyor (`Gönd: AD SOYAD 0157-Enpara…`); ravanbabayev bunu düzenli ifadeyle ayıklıyor. `*ZamanIle` yanıtında ayrı `AdUnvan`, `TcknVkn`, `IBAN` alanları var (SAP).

---

## 8. İş Bankası — Posmatik online XML ekstre

**Bağlayıcı yazmaya yeter mi: KISMEN.** Uç nokta ve yanıt yapısı topluluk kodlarıyla belli; resmi belge yok, iki farklı uç nokta var, tarih parametresinin hangi uçta geçerli olduğu kesin değil.

| Konu | Değer | Kaynak |
|---|---|---|
| Ürün | İş Bankası "Online XML (web servis) ekstre", şubeden başvuru | [isbank e-çözümler](https://www.isbank.com.tr/TR/ticari/e-cozumler/Sayfalar/e-cozumler.aspx) |
| Uç nokta A | `POST https://posmatik2.isbank.com.tr/Authenticate.aspx`, form: `uid`, `pwd` (tarih yok; dönen XML'de kök altında `Tarih` var) | [selimhallac/Isbank src/Isbank.php](https://github.com/selimhallac/Isbank/blob/HEAD/src/Isbank.php), [crazypsp/Finans IsBankStatementProvider.cs](https://github.com/crazypsp/Finans/blob/HEAD/Finans.Infrastructure/Banking/Managers/IsBankStatementProvider.cs) (önce `LoginPanel.html`'e GET ile çerez alıyor) |
| Uç nokta B | `POST https://posmatik2.isbank.com.tr/AuthenticateSpecific.aspx`, `application/x-www-form-urlencoded`: `uid`, `pwd`, `BeginDate`, `EndDate` | [gokmenma/ersan_elk test/posmatik_probe.php](https://github.com/gokmenma/ersan_elk/blob/HEAD/test/posmatik_probe.php) (deneme betiği; sonucu bilinmiyor) |
| Tarih biçimi | `dd.MM.yyyy HH:mm:ss` (ör. `20.11.2025 00:00:01`) | posmatik_probe.php ve SAP `YCL_EHO_GET_RECEIPTS_ISB` (`uid, pwd, BeginDate, EndDate`, aynı biçim) |
| Karakter kodlaması | **ISO-8859-9** (yedek Windows-1254) | Finans IsBankStatementProvider.cs |
| Kimlik | `uid`/`pwd` = bankanın verdiği **servis** kullanıcısı (internet şubesi parolası değil; önceki belgede Paraşüt kılavuzu) | — |
| Hesap seçimi | Yok; kullanıcıya tanımlı **tüm hesaplar** döner, IBAN ile süzülür | SAP ISB (IBAN ile `READ TABLE`) |
| API Portal "Hesap Hareketleri" API'si | `developer.isbank.com.tr` → `developer.sandbox.isbank.com.tr` yönlendiriyor; sayfa tek sayfa uygulaması, içerik okunamadı → uç nokta/alan **TEYİTSİZ** | WebFetch 19.10 |

Örnek istek (uç nokta B):
```http
POST /AuthenticateSpecific.aspx HTTP/1.1
Host: posmatik2.isbank.com.tr
Content-Type: application/x-www-form-urlencoded
Accept: application/xml,text/xml,*/*

uid={servisKullanici}&pwd={servisSifre}&BeginDate=01.10.2026%2000%3A00%3A01&EndDate=19.10.2026%2023%3A59%3A59
```

Yanıt yapısı (SAP tipi + C# XPath'i birleştirildi; harf büyüklüğü C# XPath'inde görülenler kesin, diğerleri **TEYİTSİZ**):
```text
XMLEXBAT
  Tarih
  Hesaplar
    Hesap[]
      Tanimlamalar: IBANNo, HesapTuru, HesapNo*, MusteriNo*, SubeKodu*, SubeAdi, DovizTuru, HesapAcilisTarihi, SonHareketTarihi*, Bakiye, KullanilabilirBakiye
      Hareketler
        Hareket[]: Tarih (dd.MM.yyyy), Saat, HareketSirano*, Miktar*, Bakiye*, Aciklama*, KarsiHesapVKN*, MusteriAciklama,
                   LehdarhIBAN (SAP adı "lehdarhiban"), borcAlacak* (SAP "BorcAlacak"), KarsiSube, isl_id, IslemTuru*,
                   timeStamp*, ParaBirimi*, Kaynak*, IslemTod*, DekontNo*
(* = C# kodunda birebir XPath olarak geçen ad)
```
Mihenk eşlemesi: tarih `Tarih` + saat (SAP: `HareketSirano[6..12]`) · tutar `Miktar` (borçta başında `-` var, SAP atıyor) · yön `borcAlacak` (`A`/`B`) · açıklama `Aciklama` (+ `MusteriAciklama`) · karşı IBAN `LehdarhIBAN` (**TEYİTSİZ** yazım) · karşı VKN `KarsiHesapVKN` · bakiye `Bakiye` · benzersiz `HareketSirano` (SAP `[12..18]`'i fiş no sayıyor; yoksa `isl_id`) ya da `DekontNo`.

---

## 9. Yapı Kredi — Online Hesap Özeti

**Bağlayıcı yazmaya yeter mi: KISMEN** (alan adları SAP kodundan; uç nokta, WSDL, kimlik alanları **TEYİTSİZ**).

- Ürün: "Online Hesap Özeti (gün içi)", web servisle gün içi hareket ([Yapı Kredi e-çözümler](https://www.yapikredi.com.tr/kurumsal/nakit-yonetimi/e-cozumler-ve-operasyonel-hizmetler/)). Herkese açık WSDL/uç nokta bulunamadı.
- SAP `YCL_EHO_GET_RECEIPTS_YKB` ([EHO20250605](https://github.com/xcersoylu/EHO20250605/tree/HEAD/objects/CLAS/YCL_EHO_GET_RECEIPTS_YKB)) CPI'ye şu JSON'u gönderiyor (yanıttaki `ns1:sorgulaResponse` adından JAX-WS tarzı SOAP olduğu anlaşılıyor; **çıkarım**):
```json
{ "sorgula": { "arg0": {
  "baslangicSaat": 0000, "baslangicTarih": 20261019,
  "bitisSaat": 2359,     "bitisTarih": 20261019,
  "dovizKodu": "TL", "firmaKodu": "{firmaKodu}", "hesapNo": {hesapNo}
} } }
```
  Kullanıcı adı/şifre bu JSON'da yok → CPI ekliyor olmalı (WS-Security mi, HTTP Basic mi **TEYİTSİZ**). Paraşüt kılavuzu "kullanıcı adı + şifre + firma kodu" diyor (önceki belge). `TRY` → `TL`.
- Yanıt: `ns1:sorgulaResponse/return` → `id, bankaAdi, bankaKodu, bankaVergiDairesi, bankaVergiNumarasi, hataAciklamasi, hataKodu, hesaplar/hesap{acilisBakiyesi, dovizTipi, hesapNo, kapanisBakiyesi, subeAdi, subeKodu, hareketler/hareket[]}`; `hareket`: `aciklama, alacakliVkn, anlikBakiye, borcAlacak, borcluVkn, dekontNo, fizikselIslemTarihi, gonderenAd, gonderenBanka, gonderenIbanNo, gonderenSube, hareketKey, islemTipi, karsiHesapVno, kontratNo, muhasebeTarihi, saat, siraNo, tutar, valor, uzunAciklama` (camelCase'e çevirme **TEYİTSİZ**; SAP küçük harf tutuyor). `muhasebeTarihi` ve `valor` yüzyılsız geliyor (SAP başına yılın ilk iki hanesini ekliyor).

---

## 10. Halkbank — Bağlı müşteri ekstre sorgulama

**Bağlayıcı yazmaya yeter mi: KISMEN** (alan adları SAP kodundan; uç nokta ve kimlik **TEYİTSİZ**).

- Ürün: "Web Servis Hizmetleri, XML mesajlaşma" ([Halkbank raporlama sistemleri](https://www.halkbank.com.tr/tr/ticari-kurumsal/nakit-yonetimi/raporlama-sistemleri)). WSDL/uç nokta herkese açık değil.
- SAP `YCL_EHO_GET_RECEIPTS_HLK` ([EHO20250605](https://github.com/xcersoylu/EHO20250605/tree/HEAD/objects/CLAS/YCL_EHO_GET_RECEIPTS_HLK)) istek:
```json
{ "BagliMusteriEkstreSorgulama": { "request": {
  "BagliMusteriNumarasi": "{musteriNo}",
  "BaslangicTarihi": "2026-10-19",
  "BitisTarihi": "2026-10-19"
} } }
```
  (SAP kodu bitiş tarihini de başlangıçtan alıyor, günlük çekiyor; kullanıcı/şifre JSON'da yok → **TEYİTSİZ**.)
- Yanıt: `BagliMusteriEkstreSorgulamaResponse/BagliMusteriEkstreSorgulamaResult` → `HataKodu` ("0" başarı), `HataAciklama`, `Hesaplar/Hesap[]{Bakiye, BlokeMeblag, FaizOrani, Hareketler/Hareket[], HesapAcilis_Tarihi, HesapAdi, HesapCinsi, HesapNo, HesapTuru, IbanNo, KrediLimit, KrediliKullanilabilirBakiye, KullanilabilirBakiye, MusteriNo, SonHareketTarihi, SubeAdi, SubeKodu, VadeTarihi}`; `Hareket`: `Aciklama, AtmNo, Bakiye, DekontNo, EkBilgi, EkstreAciklama, HareketTutari (işaretli: < 0 borç), Iptal ("E" = iptal), IslemKod, IslemYapanAdSoyad, IslemYapanKimlikNo, KarsiAdSoyad, KarsiBankaKod, KarsiHesapIBAN, KarsiKimlikNo, KarsiMusteriNo, KarsiSubeKod, ReferansNo, Saat (HH:mm:ss), SiraNo, Tarih (dd.MM.yyyy)`. Ondalık virgül. Harf büyüklüğü **TEYİTSİZ**.

---

## 11. DenizBank — API Portal

**Bağlayıcı yazmaya yeter mi: KISMEN** (zarf, imza ve taban adres belli; hesap hareketi `ApiName`'i ve yanıt alan adlarının kesin yazımı **TEYİTSİZ**).

| Konu | Değer | Kaynak |
|---|---|---|
| Canlı | `https://apigw.denizbank.com/api/v2/{ApiName}` | [Denizbank PROD API Entegrasyonuv2.pdf](https://api.intertech.com.tr/content/Denizbank%20PROD%20API%20Entegrasyonuv2.pdf) |
| UAT | `https://ssgtest.denizbank.com:9443/apiuatv2/{ApiName}` | [Denizbank UAT API Entegrasyonuv2.pdf](https://api.intertech.com.tr/content/Denizbank%20UAT%20API%20Entegrasyonuv2.pdf) |
| Ön koşul | Portal kaydı + **statik IP** yetkilendirmesi | [apiportal.denizbank.com](https://apiportal.denizbank.com/kaydol) |
| İmza | Gövde SHA-256 → RS256 (ya da internet şubesinde seçilen RS512) JWS → HTTP başlığı `Signature`. Açık anahtar (≥ 2048 bit) işletmenin internet şubesinde Ayarlar → API → Sertifika Ekle ekranından, `Channel`'a bağlı yüklenir | PROD PDF |
| Zaman damgası | Gövde `Header.RequestDate` (ör. `2024-01-17T09:32:52.7086442+03:00`) zorunlu | PROD PDF |

Gövde iskeleti (PROD PDF birebir; `Parameters` içi SAP `YCL_EHO_GET_RECEIPTS_DNZ`'den):
```json
{
  "Header": {
    "AppKey": "{{AppKey}}",
    "Channel": "{{Channel}}",
    "ChannelSessionId": "5CE7303B-9C0E-4628-A9E7-3F34D28FEC8D",
    "ChannelRequestId": "525F2F2D-B852-4B46-9FC3-9B765BC86AAA",
    "RequestDate": "2024-01-17T09:32:52.7086442+03:00"
  },
  "Parameters": [
    {
      "CustomerNo": "{musteriNo}",
      "AccountBranchCode": "{subeKodu}",
      "AccountSuffix": "{ekNo, isteğe bağlı}",
      "AssociationCode": "{kurumKodu}",
      "IBANNo": "{IBAN}",
      "QueryDate": "2026-10-19T00:00:00.000+03:00",
      "EndDate": "2026-10-19T23:59:59.000+03:00"
    }
  ]
}
```
Yanıt (SAP tipinden, harf büyüklüğü **TEYİTSİZ**): `Data.Accounts[]{MaturityDate, AvailableBalanceWithCredit, CreditLimit, AvailableBalance, BlockageAmount, AmountOfBalance, QueryInitialBalance, QueryFinishBalance, LastModifiedDate, InterestRate, AccountOpenningDate, AccountNumber, AccountBranchName, AccountBranchCode, AccountCurrencyCode, AccountName, AccountType, AccountSuffix, IBANNumber, Transactions[]}`; `Transactions`: `Valor, ReceiptNumber, DestinationAccount, DestinationAccountIdendityNumb… (SAP 30 harfe kesmiş), DestinationAccountTitle, ReferecenceNumber (bankanın yazımı), TransactionName, TransactionDetail1..5, AdditionalInfo, BankDescription, TransactionCode, SubTransactionCode, Description, Channel, BalanceBeforeTransaction, BalanceAfterTransaction, Amount, OrderNumber, Time, Date, CustomerNo, IslemDurumu, DebitCreditType`. SAP: `TransactionDetail1` 10–11 haneyse karşı VKN/TCKN, `TransactionDetail2` karşı IBAN, `TransactionDetail4` karşı şube.

---

## 12. C# bağlayıcı için ortak notlar

- **WCF mi elle zarf mı:** .NET 8'de `System.ServiceModel.Http` ile `dotnet-svcutil` istemcisi üretilebilir (topluluk projeleri böyle yapmış). VakıfBank `WSHttpBinding` (Transport) istiyor; elle zarf yazılırsa SOAP 1.2 + `wsa:Action` + `wsa:To` başlıkları şart. Diğerleri SOAP 1.1, elle `HttpClient` + `XDocument` yeterli.
- **Ayrıştırma:** Yerel ada (LocalName) göre, harf duyarsız okuyun; tek kayıtta dizi yerine tek eleman gelebiliyor (Ziraat). Ondalık ayırıcı bankaya göre virgül (Akbank, Halkbank) ya da nokta.
- **Kodlama:** İş Bankası ISO-8859-9 → `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)`.
- **Tarih aralığı sınırları (bilinenler):** Garanti 30 gün + sayfa ≤ 500 · Ziraat 21 gün (topluluk yorumu) · Akbank topluluk kodu günlük çekiyor · diğerleri **TEYİTSİZ** → bağlayıcı aralığı ayarlanabilir dilimlere bölmeli (varsayılan 1 gün).
- **Benzersizlik:** Garanti benzersiz no vermiyor (özet anahtar üret). VakıfBank `Id`, QNB `transactionId`, Kuveyt `BusinessKey`, Akbank `FisNo`, Albaraka `fisNo`, Ziraat `DekontNo`/`TimeStamp`, İş `HareketSirano`.
- **IP kısıtı:** Akbank ve DenizBank zorunlu; diğerleri için önceki belgedeki gibi sabit çıkış IP'si bildirilmeli.
- **Test:** Sandbox yalnız Garanti (portal) ve DenizBank (UAT adresi) için görüldü. Diğerlerinde pilot işletmenin servis kullanıcısı gerekir.

## Kaynaklar (bu belgede kullanılan)
- SAP EHÖ (20 banka, ABAP): https://github.com/xcersoylu/EHO20250605 (`objects/CLAS/YCL_EHO_GET_RECEIPTS_{GAR,ISB,AKB,QNB,YKB,VKB,ZIR,KUV,ALB,HLK,DNZ}`, `YCL_EHO_GET_RECEIPTS/METH CALL_API.abap`)
- C# (svcutil Reference.cs + sağlayıcılar): https://github.com/crazypsp/Finans · https://github.com/crazypsp/TurksoftApp
- Garanti: https://github.com/Hasokeyk/garanti-bbva-php · https://github.com/emrekartal99/garanti_deneme (`docs/api.json`, `docs/api_spec.md`) · https://github.com/mbrksntrk/dolibarr-garanti-api (`docs/api-notlari.md`)
- Ziraat: https://github.com/ravanbabayev/ziraat-bank-php · https://github.com/selimhallac/ziraatbankasi-web-eksktre-hesap-hareketleri · https://github.com/alialaca/ziraatbank-web-ekstre
- VakıfBank: https://github.com/selimhallac/vakifbank
- Akbank: https://github.com/selimhallac/akbank-web-ekstre-hesap-hareketleri
- İş Bankası: https://github.com/selimhallac/Isbank · https://github.com/gokmenma/ersan_elk (`test/posmatik_probe.php`) · https://www.isbank.com.tr/TR/ticari/e-cozumler/Sayfalar/e-cozumler.aspx
- Kuveyt Türk / BOA: https://github.com/selimhallac/Emlak-Katilim-Online-Web-Ekstre
- DenizBank: https://api.intertech.com.tr/content/Denizbank%20PROD%20API%20Entegrasyonuv2.pdf · https://api.intertech.com.tr/content/Denizbank%20UAT%20API%20Entegrasyonuv2.pdf · https://apiportal.denizbank.com/kaydol
- Yapı Kredi: https://www.yapikredi.com.tr/kurumsal/nakit-yonetimi/e-cozumler-ve-operasyonel-hizmetler/
- Halkbank: https://www.halkbank.com.tr/tr/ticari-kurumsal/nakit-yonetimi/raporlama-sistemleri
