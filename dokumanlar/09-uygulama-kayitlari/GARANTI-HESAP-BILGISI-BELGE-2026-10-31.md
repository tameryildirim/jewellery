# Garanti BBVA — Hesap Bilgisi (Account Information) resmi belge özeti · 31.10.2026

Kaynak: developers.garantibbva.com.tr portalı (giriş gerekli), API id `6b902397-0948-4284-a703-f6cc41556e5d`,
belge `/api/developers/document-service/1.0/docs/api/6b902397-…/Account_Information?locale=en-US`, swagger `/api/developers/api-management/1.0/apis/6b902397-…/assets/swagger`.
ByGatech uygulamasına atanmış (Account Information + Account Transactions; başka API yok).

- **Uç nokta:** POST `https://apis.garantibbva.com.tr:443/balancesandmovements/accountinformation/account/v1/getaccountinformation` (belge tablosu).
  Swagger yolu `.../account/v1/` — **ÇELİŞKİ**; kod önce belge URI'sini, 404 dönerse swagger yolunu dener. Hangisinin doğru olduğu canlı onay numarası olmadan **doğrulanamadı**.
- **Kimlik:** OAuth 2.0 client credentials, `/auth/oauth/v2/token`, scope `oob`. Token **tek kullanımlık**: her istekte yeni.
- **İstek:** `consentId` (zorunlu), `IBAN` / `unitNum` / `accountNum` (isteğe bağlı; şube+hesap birlikte gönderilmeli). ByGatech yalnız consentId + IBAN yollar.
- **Yanıt:** `accounts[].balances[] = {type, Amount}`; türler `Balance`, `BlockedAmount`, `AvailableBalance`, `CreditLimit`, `AvailableBalanceWithCredit`.
  Şema küçük harf (`amount`, `balance`, `availableBalance`) yazıyor → ByGatech harf duyarsız okur. Ayrıca `IBAN`, `currencyCode` ("TL "), `lastActivityDate`, `status`.
- **Hatalar (400, result.messageText):** kod 2/3 consentId zorunlu · 4 clientId-consentId hatalı · 5 şube ve hesap birlikte · 6 hesap hatalı · 13 hesap/IBAN uyumsuz. 401 Invalid Credentials, 429 plan sınırı, 500.
- **Deneme ortamı:** belge "DEVELOPMENT SANDBOX SERVICE" başlığı altında yine canlı taban adresi veriyor; ayrı sandbox adresi yok.
- **Hareketler (Account Transactions):** her harekette `balanceAfterTransaction` (hareket sonrası bakiye) de var; ByGatech şu an kullanmıyor (bakiye Hesap Bilgisi'nden).
