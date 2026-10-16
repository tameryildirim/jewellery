# Paket 45 — İki aşamalı giriş (TOTP / MFA) — TB-14

> Katman: Katman 7 — Platform · Tahmin: Faz A 2–3 gün, Faz B 1 gün (PARITE tahmini 2–3 gün; Faz B'nin ayrılmasıyla toplam 3–4 gün. Fark, QR'ın kapsam dışı tutulmasıyla dengeleniyor.) gün · Önkoşullar: yok · Kaynak: katman mimarı K7-45 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL (veto koşulu) · Dalga 7
> ⚠ **VETO:** Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOŞULLU VETO: meydan okuma jetonu (aynı Secret) JwtBearer tarafından access token olarak kabul edilirken sevk edilirse MFA fiilen atlanır. Farklı audience ya da 'amac=mfa' reddi + 'mfaToken ile korumalı uç → 401' HTTP testi olmadan Faz A canlıya alınamaz.

## Hedef
Kuyumcu dükkânında SAHİP hesabı bugün tek parolayla tüm müşteri kimlik verisine (TCKN, belge no) ve tüm defter/kasa hareketlerine erişiyor; parola tezgâhta paylaşılan, yazılıp yapıştırılan bir sırdır. Bu paket, kullanıcıya telefonundaki doğrulayıcı uygulamayla (Google/Microsoft Authenticator, 1Password) ikinci adım ekler ve çalınan/paylaşılan paroladan sonra uzaktan giriş yapılmasını engeller. Kurtarma kodları ve yönetici sıfırlaması, telefonu kaybeden dükkân sahibinin işinden olmamasını sağlar. TB-14'ü ve Mehmet'in PLAN-2 koşul 6'sını kapatır.

## Kodda bugünkü durum
MFA/TOTP KODDA SIFIR: proje genelinde `grep -rniE 'totp|\bmfa\b|2fa'` yalnız belge/plan dosyalarında isabet veriyor, tek satır kod yok (teknik-borc-envanteri.md:255 aynı ölçümü kayda geçmiş). Var olan altyapı: JWT access 15 dk + refresh 7 gün, rotasyon ve çalıntı-token tespiti AuthModulu.cs:170-199; token üretimi/hash'i Auth/TokenServisi.cs:19-45 (SHA-256 hash deseni TokenServisi.cs:45); giriş ucu 5/dk oran sınırlı (OranSiniri.cs:18, 46-49 + AuthModulu.cs 'giris' ucu [EnableRateLimiting(OranSiniri.Giris)]); AES-256-GCM alan şifreleme + sürüm öneki Common/AlanSifreleme.cs:35-90, DI'da singleton Program.cs:41; kullanıcı varlığı Entities.cs:75-89 (SifreHash BCrypt 12, AuthModulu.cs:84); denetim izi varlığı Data/DenetimIzi.cs:26-51 ve `DenetimServisi` Program.cs:46; kiracı filtresi KuyumcuDbContext.cs:99-100; giriş ekranı tek adımlı Kuyumcu.Web/Pages/Giris.razor:120-137 ve 207-221; oturum saklama OturumServisi.cs:16-38. Dış paket yok: Kuyumcu.Api.csproj:10-17 içinde OTP kütüphanesi bulunmuyor.

## Kapsam
**İçinde:**
- FAZ A (tek başına sevk edilebilir): TOTP kurulumu (giz üretimi, otpauth URI, elle giriş), kurulum doğrulaması, ETKIN'e geçiş
- FAZ A: girişin ikinci adımı — parola doğru + MFA etkin → token YOK, kısa ömürlü meydan okuma jetonu; ikinci uçta 6 haneli kod ya da kurtarma kodu ile token
- FAZ A: 10 adet tek kullanımlık kurtarma kodu (üretim, gösterim BİR KEZ, tüketim, kalan sayısı)
- FAZ A: kaba kuvvete karşı üç katman — uç oran sınırı, DB'de yanlış deneme sayacı + 15 dk kilit, kullanılmış TOTP adımının tekrar kabul edilmemesi
- FAZ A: MFA kapatma (parola + geçerli kod ister), kurtarma kodlarını yenileme
- FAZ A: her MFA olayının denetim izine HASSAS olarak yazılması
- FAZ A: Ayarlar ekranında 'İki aşamalı giriş' kartı, Giriş ekranında ikinci adım formu (yapıştırılabilir, autocomplete="one-time-code")
- FAZ B: SAHIP'in başka kullanıcının MFA'sını sıfırlaması (kullanıcı yönetimi ekranı + uç)
- FAZ B: kiracı düzeyinde 'SAHIP/MUDUR için zorunlu' yapılandırması (varsayılan KAPALI)
- FAZ B: ADR-0016 kaydı + tehdit modeli/ASVS matrisi/TB-14 güncellemesi

**Dışında:**
- SMS / e-posta ile kod gönderimi (operatör sözleşmesi yok — PARITE C sınıfı)
- WebAuthn / donanım anahtarı / parolasız giriş
- QR kod çizimi (kütüphane yok; ortak_kararlar madde 3)
- 'Bu cihazı 30 gün hatırla' (güvenilir cihaz) — ayrı paket, cihaz parmak izi tasarımı gerektirir
- Parola sıfırlama / e-posta doğrulama akışı (bugün de yok; MFA bunu gerektirmez)
- TOTP gizinin anahtar rotasyonu (AlanSifreleme.cs:78-81 sürüm kontrolü zaten rotasyonu Faz B'ye bırakmış — bu paket sürüm 1'de kalır)

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| kullanici_mfa | YENİ tablo (1:1 kullanıcı). `KullaniciMfa : IKiraciVarlik` Entities.cs'e eklenir, KuyumcuDbContext'e `DbSet<KullaniciMfa> KullaniciMfalari` girer. | id (36, PK ULID) · tenant_id (36) · kullanici_id (36) · yontem (20, sabit 'TOTP') · gizli_sifreli (255, AlanSifreleme çıktısı 'v1:...') · durum (20: BEKLIYOR|ETKIN) · etkinlesti (datetime null) · son_kullanilan_adim (bigint null — tekrar saldırısı kapısı) · yanlis_deneme (int, varsayılan 0) · kilit_bitis (datetime null) · kurtarma_uretildi (datetime null) · olusturuldu · guncellendi | UNIQUE (tenant_id, kullanici_id) — bir kullanıcıya iki MFA kaydı imkânsız; ayrıca (tenant_id) öncü indeks bu bileşikle karşılanır | YYYYMMDDHHMMSS_MfaTotp (tek migration, iki tabloyu birlikte kurar) |
| mfa_kurtarma_kodlari | YENİ tablo (1:N). `MfaKurtarmaKodu : IKiraciVarlik`. | id (36) · tenant_id (36) · kullanici_id (36) · kod_hash (64, SHA-256 hex — TokenServisi.Hashle ile aynı biçim) · kullanildi (bool) · kullanildi_tarih (datetime null) · olusturuldu | (tenant_id, kullanici_id, kullanildi) — 'kalan kod' sayımı ve doğrulama taraması için; UNIQUE (tenant_id, kod_hash) kazara çakışmayı yakalar | aynı migration (YYYYMMDDHHMMSS_MfaTotp) |
| kullanicilar | DEĞİŞİKLİK YOK. MFA durumu ayrı tabloda; `Temizle()` (AuthModulu.cs:246-260) alan listesi bozulmaz, yalnız `mfaEtkin` bool'u yanıt nesnesine EKLENİR (okuma kullanici_mfa'dan). | — | — | — |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/api/v1/auth/mfa/durum` | tüm roller (kendi kaydı) | Kendi MFA durumum: { etkin, durum, kalanKurtarmaKodu, etkinlesti } | JWT zorunlu |
| POST | `/api/v1/auth/mfa/baslat` | tüm roller | Giz üretir (20 bayt rastgele → base32), ŞİFRELİ kaydeder, durum=BEKLIYOR, { gizBase32, otpauthUri } döner. Zaten ETKIN ise 409. | JWT · OranSiniri.Mfa · denetim izi (EKLEME, Hassas=true) |
| POST | `/api/v1/auth/mfa/dogrula` | tüm roller | { kod } doğruysa durum=ETKIN, 10 kurtarma kodu üretir ve DÜZ METİN OLARAK YALNIZ BU YANITTA döner. Yanlışsa 401 + sayaç. | JWT · OranSiniri.Mfa · kilit kapısı · denetim izi |
| POST | `/api/v1/auth/mfa/kapat` | tüm roller | { sifre, kod } ikisi de doğruysa MFA kaydı ve kurtarma kodları silinir (durum kaydı SİLİNİR, denetim izi kalır). | JWT · parola + kod birlikte · OranSiniri.Mfa · denetim izi (SILME, Hassas) |
| POST | `/api/v1/auth/mfa/kurtarma-yenile` | tüm roller | { kod } doğruysa eski kodlar silinir, 10 yeni kod üretilip bir kez döner. | JWT · OranSiniri.Mfa · denetim izi |
| POST | `/api/v1/auth/giris` | anonim | DEĞİŞİR: MFA etkinse { mfaGerekli: true, mfaToken } döner; accessToken/refreshToken YOKTUR. MFA yoksa bugünkü davranış aynen korunur. | OranSiniri.Giris (5/dk) |
| POST | `/api/v1/auth/giris/mfa` | anonim | { mfaToken, kod } — kod 6 hane ise TOTP, 10-14 karakter ise kurtarma kodu olarak çözümlenir; başarılıysa access+refresh döner ve son_giris yazılır. | OranSiniri.MfaGiris (5/dk, anahtar = jetondaki kullanıcı) · DB kilit kapısı · adım tekrar kapısı · denetim izi |
| POST | `/api/v1/kullanicilar/{id}/mfa-sifirla` | SAHIP | FAZ B: yöneticinin, cihazını ve kurtarma kodlarını kaybeden kullanıcının MFA'sını kapatması. Kendi hesabına uygulanamaz (401/403). | [RolGerekli("SAHIP")] TAM eşleşme · denetim izi (Hassas, detay='mfa-sifirlandi:{hedefId}') |

## Servis ve dosyalar
- **YENİ** `dotnet/Kuyumcu.Api/Common/Totp.cs` — RFC 6238 TOTP: `Base32.Kodla/Coz` (RFC 4648, padding'siz, büyük harf), `Totp.Uret(byte[] giz, long adim)`, `Totp.Dogrula(giz, kod, an, driftAdim=1, out long kullanilanAdim)`. HMAC-SHA1, 6 hane, 30 sn adım. Kod karşılaştırması `CryptographicOperations.FixedTimeEquals` ile sabit zamanlı. Saf sınıf — DB/HTTP bilmez, saf birim testiyle RFC vektörlerine karşı kanıtlanır.
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Auth/MfaModulu.cs` — `MfaServisi` (kurulum/doğrulama/kapatma/kurtarma) + `MfaAyarlari` (Hane=6, AdimSaniye=30, DriftAdim=1, KurtarmaKoduSayisi=10, MaksYanlisDeneme=5, KilitDakika=15, MeydanOkumaDakika=5, ZorunluRoller=[] ) + `MfaController` ([Authorize] uçlar). Giriş ikinci adımı AuthServisi'nde kalır (kimlik öncesi yol orada). Servis içi DTO doğrulaması ELLE yapılır (DataAnnotations yalnız controller yolunda çalışır).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs` — `Giris` (bugün 143-176): parola doğrulandıktan SONRA MFA kaydı sorgulanır; ETKIN ise token üretmeden `{ mfaGerekli = true, mfaToken }` döner (satır 167-174'teki token üretimi bu dala girmez). YENİ `GirisMfa(mfaToken, kod, ip, ua)`: jetonu doğrular, kilit + kod + adım tekrar kapılarından geçirir, mevcut `RefreshUretVeKaydet` (satır 226-246) ile token döndürür. `SonGiris` güncellemesi ikinci adıma taşınır — ancak ExecuteUpdate SAYISI ARTMAZ: var olan satır (174-175) yeni metoda TAŞINIR, kopyalanmaz. Controller'a iki yeni uç eklenir.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Auth/TokenServisi.cs` — `MfaMeydanOkumaUret(kullaniciId, tenantId, sifreHash, dakika)` ve `MfaMeydanOkumaCoz(jeton)` eklenir: aynı `JwtAyarlar.Secret` ile imzalı, `amac=mfa` + `pv` (parola hash'inin SHA-256'sının ilk 16 hex'i) talepleri taşır, ömrü MfaAyarlari.MeydanOkumaDakika. Çözümde `amac` ve `pv` eşleşmezse null döner. `AccessUret` DEĞİŞMEZ (amr talebi Faz B önerisi, açık soruda).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/Entities.cs` — `KullaniciMfa` ve `MfaKurtarmaKodu` sınıfları (her ikisi `: IKiraciVarlik`, [Table] + [Column] öznitelikleriyle, mevcut Entities.cs:75-103 deseninin aynısı).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` — İki DbSet (satır 57-80 bloğuna) + OnModelCreating'e (satır 99 civarı) iki UNIQUE indeks ve bir arama indeksi. Global kiracı filtresi otomatik uygulanır (IKiraciVarlik tarayıcısı).
- **YENİ** `dotnet/Kuyumcu.Api/Data/Migrations/YYYYMMDDHHMMSS_MfaTotp.cs (+Designer)` — İki tablo. ⚠ TUZAK (SONRAKI-OTURUM-PROMPT §6): `--no-build` YASAK; önce `dotnet build` yeşil, sonra `dotnet ef migrations add`.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Common/OranSiniri.cs` — İki yeni politika sabiti: `Mfa` (kullanıcı başına 10/dk — kurulum/kapatma uçları) ve `MfaGiris` (5/dk; bölüt anahtarı = meydan okuma jetonundan çözülen kullanıcı, çözülemezse IP). Mevcut `Anahtar/Kullanici` yardımcıları yeniden kullanılır; `ClaimTypes.NameIdentifier` tuzağı (OranSiniri.cs:118-130) tekrar edilmez.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Program.cs` — `MfaAyarlari` yapılandırmadan okunur (Program.cs:54-90'daki `GetSection(...).Get<T>() ?? new()` deseni birebir), singleton kaydı.
- **DEGISTIR** `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` — `s.AddScoped<Auth.MfaServisi>();` (ModulKayit.cs:11 hizasına).
- **DEGISTIR** `dotnet/Kuyumcu.Api/appsettings.json` — "Mfa": { "Hane": 6, "AdimSaniye": 30, "DriftAdim": 1, "KurtarmaKoduSayisi": 10, "MaksYanlisDeneme": 5, "KilitDakika": 15, "MeydanOkumaDakika": 5, "ZorunluRoller": [] } — SIR YOK, tamamı davranış ayarı.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukAuth`'a (satır 83-106) yeni `required` alanlar: MfaBaslik, MfaAciklama, MfaKur, MfaKapat, MfaKod, MfaKodPlaceholder, MfaKurtarmaKodu, MfaKurtarmaUyari, MfaKurtarmaYenile, MfaEtkin, MfaPasif, MfaGizElle, MfaKilitli, MfaKodHatali, MfaKalanKod. TR (1151) ve EN (2085) blokları BİRLİKTE doldurulur — `required` olduğu için biri eksikse DERLENMEZ (bu kasıtlı kapı).
- **DEGISTIR** `dotnet/Kuyumcu.Web/Servisler/OturumServisi.cs` — Değişiklik gerekmeyebilir; meydan okuma jetonu localStorage'a YAZILMAZ, yalnız Giris.razor bileşen alanında tutulur (sayfa yenilenirse baştan giriş). Bu kasıtlıdır — açık soruda gerekçesi var.
- **DEGISTIR** `dotnet/denetim.sh` — `BEKLENEN_VARLIK=15` → 17 (satır 68). ExecuteUpdate beyaz listesi (satır 58) 4'TE KALIR — artarsa tasarım ihlal edilmiş demektir, bu betik o ihlalin bekçisidir.

## Ekranlar
- `/giris` · `dotnet/Kuyumcu.Web/Pages/Giris.razor` — Üçüncü bir görünüm eklenir: `adim == "mfa"`. `GirisYap` (satır 207-221) yanıtta `mfaGerekli` görürse jetonu alanda tutar ve ikinci adımı çizer. Form tek alan: 6 haneli kod, `inputmode="numeric"`, `autocomplete="one-time-code"`, YAPIŞTIRMA ENGELLENMEZ (WCAG 2.2 §3.3.8 — PAZAR-ARASTIRMASI-2026-09-16.md:110 bu şartı yazılı olarak koymuş), altında 'Kurtarma kodu kullan' bağlantısı ve 'Geri dön' düğmesi. Lambda kuralı: her @onclick ADLANDIRILMIŞ metot (§6 tuzağı, iki kez ısırmış). (menü/yetki: anonim)
- `/ayarlar` · `dotnet/Kuyumcu.Web/Pages/Ayarlar.razor` — Yeni kart: 'İki aşamalı giriş'. Pasifken: 'Kur' düğmesi → giz + otpauth URI + elle giriş talimatı + doğrulama kodu alanı. Doğrulama sonrası 10 kurtarma kodu TEK SEFER gösterilir, 'İndir' (mevcut wwwroot/js/indir.js kullanılır) ve 'Bir daha gösterilmeyecek' uyarısıyla. Etkinken: durum rozeti, kalan kurtarma kodu sayısı, 'Kodları yenile', 'Kapat' (parola + kod ister). (menü/yetki: tüm roller (kendi hesabı))
- `/kullanicilar` · `dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor` — FAZ B: listeye 'İki aşamalı giriş' sütunu (Rozet bileşeni: Etkin/Pasif) ve SAHIP'e görünür 'MFA sıfırla' işlemi (onay modalı, ModalPencere bileşeni). Kendi satırında düğme çizilmez. (menü/yetki: SAHIP (sıfırlama); sütun MUDUR'a salt okunur)

## İş kuralları (fail-closed)
- FAIL-CLOSED 1: `durum=ETKIN` kaydı olan kullanıcı için /auth/giris asla accessToken/refreshToken döndürmez. MFA servisi patlarsa giriş 401/500 ile REDDEDİLİR; atlama yolu yok.
- FAIL-CLOSED 2: giz şifresi çözülemezse (anahtar rotasyonu/bozuk veri) doğrulama başarısız sayılır, kullanıcı içeri alınmaz.
- durum=BEKLIYOR olan kayıt girişin ikinci adımını TETİKLEMEZ (yarım kurulum kullanıcıyı dışarıda bırakmaz), ancak ikinci bir /mfa/baslat çağrısı BEKLIYOR kaydının üstüne yazar.
- TEKRAR KAPISI: doğrulamada kullanılan TOTP adımı `son_kullanilan_adim`e yazılır; aynı veya daha küçük adımla gelen kod REDDEDİLİR (omuz sörfü ile kapılan kod ikinci kez çalışmaz).
- DRIFT: kabul penceresi ±1 adım (≈ ±30 sn). Yapılandırmadan okunur, varsayılan 1. Genişletmek kaba kuvvet yüzeyini büyütür.
- KİLİT: `yanlis_deneme >= MaksYanlisDeneme` (5) → `kilit_bitis = şimdi + 15 dk`; kilitliyken kod doğru olsa bile 429/423 döner ve sayaç sıfırlanmaz. Başarılı doğrulamada sayaç sıfırlanır, kilit temizlenir.
- KURTARMA KODU: 10 adet, her biri en az 80 bit entropi (16 karakter Crockford-base32, 4'erli gruplanmış), DB'de yalnız SHA-256 hash'i. Kullanılan kod `kullanildi=true` işaretlenir, ASLA silinmez (denetim izi). Tüm kodlar tükenirse giriş yalnız TOTP ile mümkündür.
- KURTARMA KODU DÜZ METNİ yalnız üretildiği HTTP yanıtında bulunur; ikinci bir uçtan okunamaz, loglanmaz, denetim izine YAZILMAZ (DenetimIzi.cs:45 'DEĞERLER YAZILMAZ' kuralı).
- MFA KAPATMA parola VE geçerli kod ister — yalnız açık oturum yetmez (çalınan oturumun MFA'yı kapatmasını engeller).
- YÖNETİCİ SIFIRLAMA (Faz B) kendi hesabına uygulanamaz; ayrıca sıfırlanan kullanıcıya uygulama içi bildirim bırakılır (paket 6 çatısı varsa oraya, yoksa denetim izi yeterlidir).
- DENETİM İZİ: mfa-kuruldu, mfa-etkinlesti, mfa-kapatildi, mfa-kurtarma-uretildi, mfa-kurtarma-kullanildi, mfa-kilitlendi, mfa-sifirlandi olayları `DenetimKaydi` ile, `Hassas=true`, detayda YALNIZ olay adı ve hedef kimlik.
- GİZ hiçbir yanıtta ikinci kez dönmez: /mfa/baslat'tan sonra `gizBase32` yalnız o yanıtta vardır; /mfa/durum gizi ASLA döndürmez.
- ZAMAN KAYNAĞI: adım hesabı `DateTime.UtcNow` üzerinden (Zaman.cs yerel gün araçları burada KULLANILMAZ — TOTP mutlak UTC saniyedir).

## Testler
- `dotnet/Kuyumcu.Tests/TotpTestleri.cs` [saf]
  - RFC 6238 Ek B vektörleri (SHA-1 gizi '12345678901234567890', T=59 → 287082; T=1111111109 → 081804; T=1234567890 → 005924) birebir üretilir
  - Base32 kodla/çöz gidiş-dönüş rastgele 100 gizde kayıpsız; padding'siz büyük harf çıktı
  - Drift: an-29 sn ve an+29 sn kabul, an-31/an+31 RED
  - Kod 6 haneye sıfırla doldurulur (başında 0 olan adım)
- `dotnet/Kuyumcu.Tests/MfaTestleri.cs` [DB]
  - Kurulum: /mfa/baslat sonrası DB'deki `gizli_sifreli` 'v1:' önekli ve düz gizi İÇERMEZ (AlanSifrelemeTestleri deseni)
  - Doğrulama başarılı → durum=ETKIN, 10 kurtarma kodu satırı yazıldı, hiçbiri düz metin değil
  - Aynı TOTP kodu ikinci kez → RED (son_kullanilan_adim kapısı)
  - 5 yanlış deneme → kilit_bitis dolu; kilitliyken DOĞRU kod bile RED
  - Kurtarma kodu bir kez çalışır, ikinci kez RED; kullanildi_tarih yazılır
  - MFA kapatma: yanlış parola + doğru kod → RED; ikisi doğru → kayıt ve kodlar silinir
  - Kiracı sızıntısı: A kiracısının kullanıcısı B kiracısının MFA kaydını göremez/sıfırlayamaz (KiraciIzolasyonTestleri deseni)
- `dotnet/Kuyumcu.Tests/MfaHttpTestleri.cs` [HTTP]
  - MFA etkin kullanıcıda /auth/giris yanıtında accessToken ve refreshToken ALANLARI YOK, mfaGerekli=true (camelCase tuzağı: OrdinalIgnoreCase ile ara)
  - /auth/giris/mfa doğru kodla 200 + access/refresh; yanlış kodla 401
  - Süresi dolmuş meydan okuma jetonu (MeydanOkumaDakika=0 ile kurulan ortam) → 401
  - Parola değiştikten sonra elde tutulan eski meydan okuma jetonu → 401 ('pv' talebi)
  - /mfa/baslat JWT'siz → 401; başka kullanıcının /kullanicilar/{id}/mfa-sifirla ucuna TEZGAHTAR erişimi → 403 (Faz B)
  - MfaGiris oran sınırı: ardışık denemelerde 429 + Retry-After (HttpBoruHattiTestleri.cs:235-260 deseni)
- `dotnet/Kuyumcu.Tests/MfaEkranTestleri.cs` [bUnit]
  - Giris.razor: sahte API mfaGerekli döndüğünde ikinci adım formu çizilir, kod alanı autocomplete="one-time-code" ve readonly/paste engeli YOK
  - İkinci adımda kod girilip gönderilince POST /auth/giris/mfa çağrılır ve gövdede mfaToken bulunur
  - Ayarlar.razor: MFA pasifken 'Kur' düğmesi POST /auth/mfa/baslat'a gider; doğrulama sonrası 10 kurtarma kodu ekranda listelenir
  - Sahte API varsayılanı '{}' tuzağı: /mfa/durum için gerçekçi yanıt tanımlanır (aksi hâlde JsonException)

## Mutasyon kanıtları
- `son_kullanilan_adim` kontrolü kaldırılırsa → MfaTestleri 'Ayni_TOTP_kodu_ikinci_kez_reddedilir' KIRMIZI
- Kilit kapısı (`kilit_bitis` kontrolü) kaldırılırsa → 'Kilitliyken_dogru_kod_bile_reddedilir' KIRMIZI
- Giriş dalında MFA kontrolü kaldırılıp token doğrudan döndürülürse → MfaHttpTestleri 'Mfa_etkinken_giris_token_dondurmez' KIRMIZI
- Giz `Sifrele` yerine düz yazılırsa → 'Gizli_veritabaninda_duz_metin_degildir' KIRMIZI
- Kurtarma kodu `kullanildi` işaretlemesi kaldırılırsa → 'Kurtarma_kodu_tek_kullanimliktir' KIRMIZI
- Drift penceresi 1'den 10'a çıkarılırsa → TotpTestleri 'Drift_31_saniye_reddedilir' KIRMIZI
- 'pv' (parola sürümü) talebi jetondan çıkarılırsa → 'Parola_degisince_bekleyen_meydan_okuma_gecersiz' KIRMIZI
- Yönetici sıfırlamadan [RolGerekli("SAHIP")] kaldırılırsa → 'Tezgahtar_mfa_sifirlayamaz' KIRMIZI (Faz B)

## Canlı doğrulama
- API'yi `--urls http://localhost:5199` ile başlat (§6: 5000 portu macOS ControlCenter'da). Yeni test kiracısı kur, kur kaydı oluşana kadar bekle (§6: 10 sn tuzağı).
- /auth/mfa/baslat çağır, dönen `otpauthUri`yi GERÇEK bir doğrulayıcı uygulamaya (Google/Microsoft Authenticator) ELLE ekle — kendi ürettiğimiz kodla değil, üçüncü taraf uygulamanın ürettiği kodla doğrula. Bu, RFC uyumunun tek gerçek kanıtıdır.
- Çık, yeniden giriş yap: yanıtta accessToken OLMADIĞINI ve mfaGerekli=true geldiğini gövdeyi OKUYARAK doğrula (`-o /dev/null` KULLANMA, §6).
- Aynı kodu iki kez gönder → ikincisinin reddedildiğini gör; 5 yanlış kod gönder → kilit mesajını ve 429/423'ü gör; 15 dk beklemeden doğru kodun da reddedildiğini gör.
- Bir kurtarma koduyla giriş yap, aynı kodu tekrar dene → reddedilmeli. `kalanKurtarmaKodu` 9'a düşmeli.
- Tarayıcıda /giris ikinci adımında kodu YAPIŞTIR (WCAG 3.3.8) ve şifre yöneticisinin one-time-code önerisini gör.
- denetim_kayitlari tablosunda mfa-* olaylarını ve `hassas=1` işaretini SQL ile doğrula; hiçbir satırda düz giz/kurtarma kodu OLMADIĞINI doğrula.
- `dotnet build` temiz → `dotnet test` 0 başarısız → `./denetim.sh` TEMİZ. Test kiracısını sil (API'yi ÖNCE durdur, §6 kur zamanlayıcısı).

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/Entities.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Auth/TokenServisi.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Common/OranSiniri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs (Faz B: mfa-sifirla ucu)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.json`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/appsettings.Development.json (yalnız Mfa bölümü gerekiyorsa)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Giris.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Ayarlar.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor (Faz B)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/css (yeni sınıf adı seçmeden ÖNCE grep — §6 CSS çakışma tuzağı)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/02-mimari/adr/README.md`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/05-guvenlik/tehdit-modeli.md`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/05-guvenlik/asvs-uygunluk-matrisi.md`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/08-degerlendirme/teknik-borc-envanteri.md`
- `YENİ: dotnet/Kuyumcu.Api/Common/Totp.cs · dotnet/Kuyumcu.Api/Modules/Auth/MfaModulu.cs · dotnet/Kuyumcu.Api/Data/Migrations/YYYYMMDDHHMMSS_MfaTotp(.Designer).cs · dotnet/Kuyumcu.Tests/TotpTestleri.cs · MfaTestleri.cs · MfaHttpTestleri.cs · MfaEkranTestleri.cs · dokumanlar/02-mimari/adr/0016-mfa-totp.md`
- `DOKUNULMAZ (bu pakette değişmez): KenarCubugu.razor, UstBar.razor, HareketFabrikasi.cs, IslemDogrulayici.cs, Tevsik/Masak modülleri`

## Mevzuat dayanağı
- 6698 s. KVKK md.12/1 — veri güvenliğine ilişkin 'uygun güvenlik düzeyi' yükümlülüğü — **DOGRULANDI** — Maddenin varlığı ve genel yükümlülük kesindir; ancak MFA'yı ADIYLA zorunlu kılmaz. Kodda bu maddeye dayanan bir kapı KURULMAZ.
- KVKK Kurumu 'Kişisel Veri Güvenliği Rehberi' — uzaktan erişimde çok faktörlü doğrulama tavsiyesi — **DOGRULANMALI** — Rehberin MFA'yı tavsiye ettiği hatırlanıyor fakat bu oturumda metin AÇILMADI. Uygulayıcı bu cümleyi belgeye/pazarlama metnine koymadan önce rehberin güncel sürümünü doğrulamalıdır. Kod davranışı bu doğrulamaya BAĞLI DEĞİLDİR.
- MASAK / 5549 s. Kanun — **DOGRULANDI** — Perakende kuyumcuya MFA yükümlülüğü GETİRMEZ. Bu paket bir uyum paketi değil, güvenlik paketidir; MASAK kapılarına dokunmaz.
- RFC 6238 (TOTP) + RFC 4226 (HOTP) + RFC 4648 (Base32) — **DOGRULANDI** — Mevzuat değil teknik standart. Uyum, RFC 6238 Ek B test vektörleriyle testte KANITLANIR — 'uyumlu' iddiası vektör geçmeden yazılmaz.

## Riskler
- KİLİTLENME (en büyük risk): tek kullanıcılı dükkânda SAHİP hem telefonunu hem kurtarma kodlarını kaybederse içeri girecek kimse kalmaz. Faz B yönetici sıfırlaması bunu SAHİP dışındaki kullanıcılar için çözer; SAHİP için çözmez. Azaltım: kurtarma kodlarını indirme/yazdırma zorlaması + sunucu tarafında elle kapatma runbook'u (DevOps, Batuhan) — runbook YAZILMADAN Faz A canlıya alınmamalı.
- SAAT KAYMASI: sunucu ya da telefon saati kayarsa kodlar tutmaz. ±1 adım pencere ±30 sn tolere eder; sunucuda NTP şart. Belirti 'kod hatalı' olarak görünür ve destek çağrısına dönüşür — hata mesajına 'telefonunuzun saatinin otomatik ayarlı olduğundan emin olun' cümlesi eklenmeli.
- ORAN SINIRI SÜREÇ İÇİ: OranSiniri bellekte tutulur (OranSiniri.cs:41-...); API yeniden başlarsa sayaç sıfırlanır. DB sayacı bu boşluğu kapatır — DB sayacı atlanırsa kaba kuvvet kapısı FİİLEN AÇIK kalır.
- ANAHTAR ROTASYONU: AlanSifreleme.cs:78-81 sürüm eşleşmezse istisna atar. Anahtar rotasyonu yapılırsa TOTP gizleri çözülemez ve TÜM MFA kullanıcıları kilitlenir. Rotasyon planına MFA gizlerinin yeniden şifrelenmesi maddesi eklenmeli (bugün rotasyon Faz B'de, yani bugün risk değil ama kayda geçer).
- SÖZLÜK `required` ALANLARI: Sozlukler.cs'e TR eklenip EN unutulursa derleme kırılır (kasıtlı) — ama iki blok (1151 ve 2085) arası 900 satır; uygulayıcı ikisini AYNI düzenlemede yazmazsa zaman kaybeder.
- bUnit SAHTE API: yeni uçlar (/mfa/durum) mevcut ekran testlerinin sahte API'lerine eklenmezse JsonException ile kırılır (§6'da 5 CariEkran testi bundan kırılmış).

## Açık sorular
- SAHİP hem cihazını hem kurtarma kodlarını kaybederse kurtarma yolu ne olacak? (a) sunucuda elle SQL + runbook, (b) ikinci SAHİP kullanıcısı zorunluluğu, (c) e-posta ile sıfırlama (bugün e-posta kanalı YOK). Karar oylamaya ait; Faz A bu karar verilmeden CANLIYA alınmamalı.
- MFA kiracı düzeyinde ZORUNLU kılınabilmeli mi (SAHIP/MUDUR için)? Faz B'de yapılandırmadan okunan `ZorunluRoller` olarak tasarlandı, varsayılan BOŞ (kapalı). Zorunluluk açılırsa MFA kurmamış kullanıcı giriş yapabilmeli mi yoksa kurulum ekranına mı zorlanmalı — karar verilmedi.
- Access token'a `amr=mfa` talebi eklenip hassas uçlarda (dışa aktarım, MASAK, kullanıcı yönetimi) 'MFA ile girilmiş olma' şartı aranmalı mı? Güçlü bir sıkılaştırma ama var olan token sözleşmesini değiştirir; ayrı karar.
- Meydan okuma jetonu bilinçli olarak localStorage'a yazılmıyor (sayfa yenilenirse baştan giriş). Kullanılabilirlik açısından kabul edilebilir mi, yoksa sessionStorage'a mı yazılsın? Ertuğ'un görüşü alınmalı.
- Kurtarma kodu biçimi: 16 karakter Crockford-base32 önerildi (karışabilen I/L/O/U harfleri yok). Kuyumcunun elle yazması için 10 karakter yeterli görülürse entropi ~50 bit'e düşer — kabul edilir mi? Öneri: 16'da kalınsın.
- QR kodu hangi pakette gelecek? (43 mobil / 44 PWA ile birlikte bir JS kütüphanesi girecekse orada; girmeyecekse hiç.) Bu paket QR'sız da kullanılabilir ama kurulum sürtünmesi artar.
- `kullanicilar` tablosuna `mfa_etkin` denormalize bayrağı eklensin mi? Her girişte ikinci tabloya sorgu atmamak için cazip; ancak iki yerde tutulan gerçek ayrışabilir. Öneri: EKLENMESİN (giriş yolunda tek indeksli sorgu ucuz). Karar oylamaya bırakılabilir.

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — Dış bağımlılık eklenmemesi doğru (~120 satır, ölçülebilir). Gizin ADR-0003 alan şifrelemesiyle, kurtarma kodlarının refresh token deseniyle SHA-256 hash'lenmesi (BCrypt yerine, DoS gerekçesiyle) isabetli. MFA meydan okumasının kısa ömürlü imzalı JWT + parola sürümü talebiyle kurulması üçüncü tablo açmadan doğru çözüm. İnce yetkilerin JWT'ye konmaması ve fail-closed giriş dalı doğru.
  - Koşullar: SAHİP KİLİTLENME RUNBOOK'U YAZILMADAN Faz A CANLIYA ALINMAZ. Tek kullanıcılı dükkânda SAHİP telefonunu ve kurtarma kodlarını kaybederse içeri girecek kimse kalmaz; Faz B yönetici sıfırlaması bunu SAHİP için çözmez. Bu, paketin tek gerçek üretim riski. · Seviye tablosu ÇAKIŞMASI: paket 46 AktifKullanici.Seviye'yi 6/5/4/3/2/1 olarak yeniden numaralandırıyor. İki paket aynı dosyayı aynı anda değiştiremez; 45 seviye tablosuna DOKUNMUYOR, dolayısıyla 46'dan bağımsız sevk edilebilir — ama sıraya alınmalı. · Anahtar rotasyonu planına 'MFA gizlerinin yeniden şifrelenmesi' maddesi EKLENMEDEN Faz A sevk edilmez: AlanSifreleme sürüm eşleşmezse istisna atıyor, rotasyon günü tüm MFA kullanıcıları kilitlenir. · RFC 6238 uyum iddiası, Ek B test vektörleri YEŞİL olmadan hiçbir belgede yazılmaz; ayrıca canlı doğrulamada ÜÇÜNCÜ TARAF doğrulayıcı uygulamayla (kendi ürettiğimiz kodla değil) giriş kanıtı alınacak. · DB deneme sayacı + kilit (OranSiniri'nin süreç içi olması nedeniyle) Faz A kapsamında ZORUNLU; atlanırsa kaba kuvvet kapısı fiilen açık kalır.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — TB-14'ü kapatan, RFC vektörleriyle kanıtlanan, fail-closed bir tasarım. Tek ciddi açık aynı anahtarla imzalanan ikinci jetonun bearer olarak kabul riski; koşulla kapanır.
  - Koşullar: KRİTİK: MFA meydan okuma jetonu access token ile AYNI Secret'la imzalanıyor. JwtBearer middleware bu jetonu geçerli bearer olarak KABUL EDEBİLİR (parola doğru ama MFA geçilmemiş kullanıcı API'ye girer). Zorunlu: farklı audience ('kuyumcu-mfa') VEYA OnTokenValidated'da 'amac=mfa' talebi taşıyan jetonu reddet; HTTP testi 'mfaToken ile GET /cari → 401'. Bu koşul karşılanmadan Faz A sevk edilmez. · MfaGiris oran sınırı bölütü: jeton çözülemezse IP; çözülürse kullanıcı — çözümleme imza DOĞRULAMALI olsun (imzasız parse ile kullanıcı kimliği sahtelenip başkasının kotası tüketilmesin). · Kilitlenme runbook'u (Batuhan) Faz A canlıya alınmadan yazılsın (spec riski) — SAHİP tek hesapsa kurtarma yolu yoksa sevk yok. · Giz 'v1:' AES ile, kurtarma kodları SHA-256 (yüksek entropi, BCrypt yerine) KABUL; düz kod yalnız üretim yanıtında, loglanmaz, denetim izine yazılmaz — MfaTestleri 'düz metin değil' zorunlu. · MFA kapatma parola+kod; yönetici sıfırlama [RolGerekli("SAHIP")] ve kendine uygulanamaz; her olay Hassas=true denetim izi. · ExecuteUpdate sayısı 4'te kalır (SonGiris taşınır, kopyalanmaz) — denetim.sh kanıtı. · Faz B 'amr=mfa' talebi ile hassas uçlar (dışa aktarım, MASAK, kullanıcı yönetimi) için MFA şartı: DESTEKLİYORUM, ayrı oylama.
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Fail-closed tasarım ve DB+bellek çift oran sınırı doğru; mutasyonlar test adlarıyla eşleşiyor. Zaman bağımlı testler ve yapılandırma geçersiz kılma yöntemi belirsiz; en büyük risk (SAHİP kilitlenmesi) test değil süreçle kapanıyor.
  - Koşullar: Kilitlenme riski: 'SAHIP telefon + kurtarma kodlarını kaybederse' runbook (Batuhan) yazılmadan Faz A CANLIYA alınmaz — bu, kabul ölçütüne bağlayıcı madde olarak yazılsın; test değil süreç kapısı. · MfaHttpTestleri 'süresi dolmuş meydan okuma jetonu (MeydanOkumaDakika=0)' HttpOrtami'nin yapılandırma geçersiz kılmasını gerektirir; mevcut fabrika deseni buna izin veriyor mu OKUNMADI — uygulayıcı önce doğrulasın, yoksa TokenServisi saf testi ile (sahte zaman) ölçsün. · 'Kilitliyken doğru kod bile reddedilir' testi zaman bağımlı: kilit_bitis = şimdi+15 dk; test sahte saat kullanmalı (Zaman soyutlaması yoksa TOTP sınıfına IClock/Func<DateTime> parametresi eklensin) — aksi hâlde yavaş CI'da kararsız. · TotpTestleri RFC 6238 Ek B vektörleri EVET; ancak canlı doğrulamadaki 'gerçek Authenticator ile kod üret' adımı otomasyon dışı; raporda ekran görüntüsü + zaman damgası şart. · MFA kapatma ucu (durum kaydı SİLİNİR) ExecuteDelete değil Remove+SaveChanges ile — denetim.sh:53 beyaz listesi 4 kalmalı; kabul ölçütüne yazılsın. · MfaEkranTestleri sahte API'ye /mfa/durum yanıtı eklenmesi mevcut Ayarlar ekran testlerini (TemaEkranTestleri) kırmamalı; mevcut ekran testleri 0 başarısız kabul ölçütü.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- VETO: Mehmet — MFA meydan okuma jetonu access token ile AYNI Secret'la imzalanıyor; JwtBearer onu geçerli bearer sayabilir ve MFA fiilen atlanır. ZORUNLU: farklı audience ('kuyumcu-mfa') VEYA OnTokenValidated'da 'amac=mfa' talebi taşıyan jetonun reddi + HTTP testi 'mfaToken ile GET /cari → 401'. Bu karşılanmadan Faz A sevk edilmez.
- SAHİP KİLİTLENME RUNBOOK'U (Batuhan/DevOps) YAZILMADAN Faz A CANLIYA ALINMAZ — süreç kapısı, test değil (üç mercek ortak). Tek kullanıcılı dükkânda telefon+kurtarma kodları kaybolursa Faz B yönetici sıfırlaması SAHİP'i kurtarmaz; kurtarma kodlarını indirme/yazdırma zorlaması eklenir.
- Anahtar rotasyonu planına 'MFA gizlerinin yeniden şifrelenmesi' maddesi EKLENMEDEN Faz A sevk edilmez (AlanSifreleme.cs:78-81 sürüm eşleşmezse istisna → tüm MFA kullanıcıları kilitlenir).
- DB deneme sayacı + kilit Faz A kapsamında ZORUNLU: OranSiniri bellekte (OranSiniri.cs:41-...), API yeniden başlarsa sayaç sıfırlanır ve kaba kuvvet kapısı fiilen açık kalır. MfaGiris oran sınırı bölütü: jeton çözülemezse IP, çözülürse kullanıcı — çözümleme İMZA DOĞRULAMALI olsun (kota sahteciliği olmasın).
- RFC 6238 uyum iddiası Ek B test vektörleri YEŞİL olmadan hiçbir belgede yazılmaz; canlı doğrulamada ÜÇÜNCÜ TARAF doğrulayıcı uygulamayla giriş kanıtı + ekran görüntüsü + zaman damgası (otomasyon dışı adım).
- Giz 'v1:' AES ile, kurtarma kodları SHA-256 (yüksek entropi, BCrypt yerine DoS gerekçesi) KABUL; düz kod yalnız üretim yanıtında, loglanmaz, denetim izine yazılmaz (MfaTestleri 'düz metin değil'). Kurtarma kodu biçimi 16 karakter Crockford-base32'de kalsın.
- Yetki/iz: MFA kapatma parola+kod; yönetici sıfırlama [RolGerekli("SAHIP")] ve kendine uygulanamaz; her olay Hassas=true denetim izi. MFA kapatmada kayıt Remove+SaveChanges ile silinsin, ExecuteDelete YOK — denetim.sh:53 beyaz listesi 4'te kalmalı (SonGiris taşınır, kopyalanmaz).
- Zaman bağımlılığı: 'kilitliyken doğru kod bile reddedilir' ve 'süresi dolmuş meydan okuma jetonu' testleri sahte saatle kurulsun (Zaman soyutlaması yoksa TOTP/servise IClock/Func<DateTime>); HttpOrtami'nin yapılandırma geçersiz kılmaya izin verdiği önce DOĞRULANSIN, vermiyorsa saf test.
- Saat kayması: sunucuda NTP şart, ±1 adım pencere; hata mesajına 'telefonunuzun saatinin otomatik ayarlı olduğundan emin olun' cümlesi eklensin.
- Çakışma ve kapsam: paket 46 AktifKullanici.Seviye'yi yeniden numaralandırıyor — 45 seviye tablosuna DOKUNMAZ, aynı dalgada seri sıraya alınır (45 önce). Sozlukler.cs TR ve EN blokları (satır ~1151 ve ~2085) AYNI düzenlemede yazılsın; bUnit sahte API'lere /mfa/durum yanıtı eklensin, mevcut Ayarlar/Tema ekran testleri 0 başarısız kalsın. Faz B 'amr=mfa' ile hassas uçlarda MFA şartı: destekleniyor, AYRI oylama.
