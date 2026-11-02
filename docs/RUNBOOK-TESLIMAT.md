# Runbook — Teslimat (kurulumdan ilk girişe) · PAKET P-33 · Köprü K-7

> **Amaç:** bir dükkâna BYGATECH kurulumunu baştan sona, **mevcut betiklerle**,
> tahmin yürütmeden yapmak. Bu belge olmadan **P-35 sürüm mührü atılmaz** (BK-32).
> Bu belgedeki tatbikat bir **YEREL KAP TATBİKATI**dır; bulut/üretim sunucusunda
> koşmuş bir kurulum **değildir** ve "üretim kanıtı" **denmez** — K-6 (bulut
> hesabı) ve K-9 (alan adı + TLS) kurucu kalemleri gelmedi.

## 0. Tek sayfa akış

| # | Adım | Komut / yer | Kanıt |
|---|---|---|---|
| 1 | Yayın al | `./dotnet/yayin-al.sh linux-x64` (Windows: `win-x64`) | `dotnet/yayin/<rid>/` + `kuyumcu-migrate` |
| 2 | Sırları üret + **adresi sor** + kısayolu üret | `sudo ./dotnet/kurulum.sh --hedef /etc/kuyumcu/.env` (Windows: `kurulum.ps1`) | "var/yok" listesi · `BYGATECH_ADRES` satırı · masaüstü kısayolu |
| 3 | Veritabanı + şema | `kuyumcu-migrate` (bundle) — `docs/RUNBOOK-SEMA-YUKSELTME.md` | `/saglik/hazir` → `{"hazir":true}` |
| 4 | Servis | `deploy/systemd/kuyumcu.service` · Windows `deploy/kuyumcu-service-kur.ps1` | `/saglik` → 200 |
| 5 | Ters vekil + TLS (ağ kurulumu) | `deploy/nginx-kuyumcu.conf` · `deploy/iis-web.config` | `docs/DEPLOY.md` § 5 |
| 6 | Sistem anahtarı + ilk davet | `docs/RUNBOOK-PILOT-DAVET.md` § 0–1 | davet `BEKLIYOR` → kayıt 201 |
| 7 | İlk giriş + kuruluş sihirbazı | kısayol → Giriş → **İşletme Kur** (davet kodu) → `/kurulum` | ilk kiracı + Sahip kullanıcı |
| 8 | **Yedek + geri yükleme tatbikatı** | `./dotnet/yedek-al-ve-dogrula.sh` | çıkış 0 · `/yedekler` "Son geri yükleme tatbikatı" |
| 9 | Kabul listesi | `docs/DEPLOY.md` § 10 | hepsi işaretli |

## 1. Kurulum adımı

`docs/DEPLOY.md` tek yayın yoludur (MÜHÜR 3); bu runbook onu **yeniden yazmaz**, sırayı ve teslimat kanıtını toplar.
Betikler (hepsi MEVCUT, P-33 yalnız adres/kısayolu ekledi):
`dotnet/yayin-al.sh` · `dotnet/kurulum.sh` / `kurulum.ps1` · `dotnet/yedek-al-ve-dogrula.sh` (+ `yedek-ortak.sh`).

> ⚠ **Sistem anahtarı kurulum betiğinde üretilmez.** `Sistem__Anahtari` olmadan
> sistem uçları kapalıdır (503) ve **ilk kiracının daveti üretilemez**. Anahtarı
> `docs/RUNBOOK-PILOT-DAVET.md` § 0'daki gibi ekrana basmadan üretip `.env`'e ekleyin.
> (Tatbikatta ölçüldü — § 5; kurulum betiğine eklenmesi Mehmet onayına bağlı
> açık kalemdir.)

## 2. Adres ve kısayol (K-26 — KARAR: kurulumda sorulur)

- Kurulum betiği adresi **bir kez sorar** (varsayılan `http://localhost:5199`);
  etkileşimsiz koşuda varsayılanı kullanır ve bunu ekrana yazar.
- Adres `.env`'de **tek satırdır**: `BYGATECH_ADRES=<adres>`.
- Masaüstü kısayolu **o adresle** üretilir (macOS `BYGATECH.webloc` · Linux
  `BYGATECH.desktop` · Windows `BYGATECH.url`). Adres kısayola **gömülü** değildir.
- Uygulamanın dinlediği nokta adresten türer: adres bu makineyi gösteriyorsa
  (`http://localhost:PORT`) uygulama o portu dinler (`Urls` + `ASPNETCORE_URLS`
  aynı değer); başka bir ana makineyi gösteriyorsa ters vekilin arkasındaki iç
  noktayı dinler.
- **Adres değişirse:** `sudo ./dotnet/kurulum.sh --hedef /etc/kuyumcu/.env --kisayol --adres <yeni>`
  (Windows: `-SadeceKisayol -Adres <yeni>`) → satır güncellenir, kısayol yeniden
  üretilir, **sırlara dokunulmaz**.
- Kullanıcı kılavuzu sabit adres **yazmaz**; "kurulumda sorulan adres kısayola yazılır" der.

## 3. Destek kanalı

- **Ürün içi:** hata bandındaki **hata kodu** ve (kanal açıldığında) Ayarlar'daki
  maskeli destek paketi (P-31). Paket kanalı varsayılan **KAPALIDIR**
  (`Destek__PaketKanaliAcik`, BK-17 — Mehmet turu sonrası açılır).
- **Kılavuz:** her ekranın üst çubuğundaki **Yardım** → `kilavuz.html` o ekranın bölümü.
- **İnsan kanalı (telefon / e-posta / saat):** ⚠ **KURUCU KALEMİ — boş.**
  Dış iletişim ve destek taahhüdü kurucu imzasıdır (K-19); SLA destek yanıt
  süresi `OLCULMEDI` damgalıdır (`docs/SLA.md` N-2). Bu satır doldurulmadan
  müşteriye teslimat **yapılmaz**.

## 4. Yedek anahtarı (K-18)

- `Yedek__SifrelemeAnahtari` kurulum betiğinde **ayrı** üretilir (alan şifreleme
  anahtarından farklı olması açılışta denetlenir).
- Anahtar kaybolursa yedek **açılamaz**. Anahtarın **sunucu dışında** saklanması
  (kasa/parola yöneticisi/kâğıt zarf — yöntem ve sorumlu) ⚠ **K-18 KURUCU
  KALEMİDİR ve gelmedi.** Bu satır doldurulmadan teslimat "tamam" sayılmaz;
  pilot kurulumunda yöntem ve saklayan kişi buraya yazılır.
- Tek yedek yolu `dotnet/yedek-al-ve-dogrula.sh`'tır; tatbikat en geç 30 günde
  bir koşar ve `/yedekler` ekranında görünür (`docs/YEDEK-GERI-YUKLEME.md` § 10).

## 5. YEREL KAP TATBİKATI — temiz kurulum (2026-09-23)

**Ne yapıldı:** geliştirme makinesinde, mevcut veritabanından TAMAMEN ayrı,
**boş** bir MySQL 8.4 kabında (`bygatech-tatbikat-mysql`, `127.0.0.1:3317`)
sıfırdan kurulum. Kap tatbikat sonunda durduruldu ve silindi (`--rm`).
Sır dosyaları tatbikat bitince boşaltıldı; hiçbir sır ekrana basılmadı.

| Adım | Komut | Sonuç (gerçek çıktı) |
|---|---|---|
| Sırlar + adres + kısayol | `kurulum.sh --hedef <geçici>/.env --adres http://localhost:5197 --kisayol-dizin <geçici>/masaustu` | çıkış 0 · 9 anahtar "var" · `BYGATECH.webloc → http://localhost:5197` |
| Boş MySQL | `docker run --rm … --env-file <yalnız MYSQL_ROOT_PASSWORD> mysql:8.4` | 8.4.11 · yalnız sistem şemaları |
| Şema | `dotnet ef database update --connection …` (build'li) | **70 migration** uygulandı · `__EFMigrationsHistory` = 70 |
| Uygulama (Production) | `.env` ortamıyla `dotnet run --no-launch-profile` | `/saglik` **200 Healthy** · `/saglik/hazir` **200** `{"hazir":true,"kod":"HAZIR"}` · `/api/v1/yedekler/ozet` oturumsuz **401** |
| İlk davet + kiracı | `POST /api/v1/sistem/davetler` → `POST /api/v1/auth/kayit` | davet `BEKLIYOR` · kayıt **201** (accessToken, refreshToken, tenantId) · tenants = 1 |
| Yedek + geri yükleme | `yedek-al-ve-dogrula.sh` (KUYUMCU_KAP=tatbikat kabı) | çıkış 0 · **76/76 tablo CHECKSUM eşleşti**, gerçek uyuşmazlık 0 · şema düşürüldü · 16 sn · `t202609231929549543` |
| Ekranda görünürlük | `GET /api/v1/yedekler/ozet` (Sahip) | **200** · `sonBasariliTatbikat` 2026-09-23T19:29:54 · `tatbikatYasGun` 0 · `tatbikatAzamiGun` 30 · `tatbikatBayat` false |

**Tatbikatın bulduğu kusurlar (Anayasa md.1 — gizlenmedi):**

1. `yedek-al-ve-dogrula.sh`, belgelerdeki gibi depo kökünden `./dotnet/…` ile
   çağrılınca **hiç çalışmıyordu** (kitaplığı `dotnet/dotnet/yedek-ortak.sh`'ta
   arıyordu). **Onarıldı** (P-33; betik kendi dizinine geçtikten sonra kitaplığı
   oradan okur).
2. Kurulumun yazdığı `ASPNETCORE_URLS` **etkisizdi**: `appsettings.Production.json`
   içindeki `"Urls"` onu eziyordu ve uygulama adrese değil 5000'e bağlandı.
   **Onarıldı** (P-33): kurulum etkin anahtar `Urls`'i de yazar; tatbikatta
   uygulama `127.0.0.1:5197`'yi dinledi.
3. `Sistem__Anahtari` kurulumda üretilmiyor → ilk davet için elle eklenmesi
   gerekiyor (§ 1 uyarısı). **Açık kalem** (Mehmet onayı).
4. Bu makine macOS olduğundan `yayin-al.sh`'ın linux/win çıktısı ve
   `kuyumcu-migrate` bundle'ı **koşturulamadı**; şema `dotnet ef database update`
   ile kuruldu, uygulama derleme çıktısından çalıştı (bu yüzden statik
   `kilavuz.html` yayın dışı çalıştırmada 404'tür — yayında `wwwroot` gelir).
   Linux/Windows sunucuda tatbikat **pilot kurulumunda ayrıca** koşulur.

## 6. Açık kalemler (teslimat öncesi kapanmalı)

| Kalem | Sahibi | Durum |
|---|---|---|
| Destek insan kanalı (telefon/e-posta/saat) | Kurucu (K-19) | boş |
| Yedek anahtarının sunucu dışı saklanması | Kurucu (K-18) | boş |
| Bulut/üretim sunucusunda tatbikat | Kurucu (K-6, K-9) → sonra ekip | yalnız yerel kap |
| `Sistem__Anahtari`'nın kurulum betiğine eklenmesi | Mehmet (güvenlik) | açık |
| Windows yolunda (`kurulum.ps1`) gerçek koşum | Batuhan (Windows makinesi) | metin sözleşmesi testli, koşulmadı |
