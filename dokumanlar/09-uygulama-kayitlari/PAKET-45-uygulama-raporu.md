# PAKET 45 — İki aşamalı giriş (TOTP / MFA), Faz A **API bacağı (45-API)** — uygulama raporu

> Tarih: 2026-09-16 · Dalga 2 · Rol: Taşkın (backend) · Kapsam: **yalnız API** (ekran 45-UI dalga 2c'de)
> Durum: **BİTTİ** — derleme 0 uyarı/0 hata · 1540/1540 test yeşil · `dotnet/denetim.sh` TEMİZ · canlı doğrulama 5198'de yapıldı · test kiracısı silindi (tenants 7 → 7)

---

## 1. Yapılanlar

### Veri modeli (migration `20260916175238_MfaTotp`)
| Tablo | İçerik |
|---|---|
| `kullanici_mfa` | 1:1 kullanıcı; `gizli_sifreli` (AES-256-GCM, `v1:` önekli), `durum` (BEKLIYOR/ETKIN), `son_kullanilan_adim` (tekrar kapısı), `yanlis_deneme`, `kilit_bitis`, `kurtarma_uretildi`. UNIQUE (tenant_id, kullanici_id) |
| `mfa_kurtarma_kodlari` | 1:N; `kod_hash` (SHA-256 hex), `kullanildi`, `kullanildi_tarih`. İndeks (tenant_id, kullanici_id, kullanildi) + UNIQUE (tenant_id, kod_hash) |

`kullanicilar` tablosu **değişmedi** (denormalize `mfa_etkin` bayrağı açılmadı — spesifikasyonun önerisi).
Her iki varlık `IKiraciVarlik`; global kiracı filtresi otomatik uygulanıyor (denetim.sh: 35 varlık, HasQueryFilter var).

### Kod
- **YENİ** `dotnet/Kuyumcu.Api/Common/Totp.cs` — RFC 4648 Base32 + RFC 6238 TOTP (HMAC-SHA1, 6 hane, 30 sn, ±1 adım drift, `CryptographicOperations.FixedTimeEquals`), otpauth URI üretici. **Dış paket eklenmedi.**
- **YENİ** `dotnet/Kuyumcu.Api/Modules/Auth/MfaModulu.cs` — `MfaAyarlari` (yapılandırmadan), `MfaKapilari` (kilit / kod çözümleme / tekrar kapısı / kurtarma kodu üretimi — **tek yerde**, hem istek bağlamı hem kimlik öncesi yol aynı kuralı kullanır), `MfaServisi`, `MfaController`.
- **DEĞİŞTİ** `Auth/TokenServisi.cs` — `Jetonlar.ApiAudience = "kuyumcu-api"` / `MfaAudience = "kuyumcu-mfa"`; `AccessUret` artık audience taşır; `MfaMeydanOkumaUret/Coz` (amac=mfa + `pv` parola sürümü, imza **doğrulamalı** çözüm).
- **DEĞİŞTİ** `Program.cs` — JwtBearer `ValidateAudience = true`, `ValidAudience = kuyumcu-api`; `MfaAyarlari` singleton.
- **DEĞİŞTİ** `Modules/Auth/AuthModulu.cs` — `Giris`: parola doğruysa ve MFA ETKİNSE **token üretilmez**, `{ mfaGerekli, mfaToken, gecerlilikDakika }` döner; yeni `GirisMfa`; `SonGiris` yazımı tek `SonGirisYaz` yardımcısına **taşındı** (kopyalanmadı).
- **DEĞİŞTİ** `Common/OranSiniri.cs` — `Mfa` (kullanıcı başına 10/dk), `MfaGiris` (5/dk; bölüt = meydan okuma jetonundan **imza doğrulanarak** çözülen kullanıcı, çözülemezse IP; gövde okunmaz).
- **DEĞİŞTİ** `Modules/Meta/DuzeltmePolitikasi.cs` — `KullaniciMfa` ve `MfaKurtarmaKodu` SALT_OKUNUR damgası + alternatif yol (`/auth/mfa/kaldir`, `/auth/mfa/kurtarma-yenile`).
- **DEĞİŞTİ** `appsettings.json` (`"Mfa"` bölümü, sır yok), `Modules/ModulKayit.cs`, `Kuyumcu.Core/I18n/Sozlukler.cs` (16 yeni anahtar, **TR ve EN aynı düzenlemede**).

### Uçlar
| Fiil | Yol | Yetki | Not |
|---|---|---|---|
| GET | `/api/v1/auth/mfa/durum` | JWT (kendi kaydı) | giz **asla** dönmez |
| POST | `/api/v1/auth/mfa/kur` | JWT | giz üretir, şifreli saklar, otpauth URI döner; ETKİN ise 409 |
| POST | `/api/v1/auth/mfa/dogrula` | JWT | ETKIN'e geçirir, 10 kurtarma kodunu **bir kez** döner |
| POST | `/api/v1/auth/mfa/kurtarma-yenile` | JWT | eski kodları siler, 10 yeni kod |
| POST | `/api/v1/auth/mfa/kaldir` | JWT | **parola + kod** ister; kayıt ve kodlar `Remove` ile silinir (toplu silme yok) |
| POST | `/api/v1/auth/mfa/giris` | anonim | meydan okuma jetonu `Authorization: Bearer`, gövdede `{ kod }` |
| POST | `/api/v1/auth/giris` | anonim | MFA etkinse token **yok**, meydan okuma jetonu var |

---

## 2. Sapmalar ve gerekçeleri

| Spesifikasyon | Uygulanan | Gerekçe |
|---|---|---|
| Uç adları `/mfa/baslat`, `/mfa/kapat`, `/auth/giris/mfa` | `/mfa/kur`, `/mfa/kaldir`, `/auth/mfa/giris` | Görev metnindeki **EK KOŞULLAR (oylama)** bu adları veriyor; oylama spesifikasyondan sonradır, oylamaya uyuldu. |
| Meydan okuma jetonu istek **gövdesinde** (`mfaToken`) | `Authorization: Bearer` başlığında | Mehmet'in koşulu: oran sınırı bölütü jetonu **imza doğrulayarak** çözmeli. Bölüt fabrikası istek gövdesini okuyamaz (akış tüketilir); başlık hem imza doğrulamasına hem "access token buradan geçemez" testine izin veriyor. Yan fayda: access token gönderilirse aynı kapıda 401. |
| Mehmet: "farklı audience **VEYA** amac=mfa reddi" | **İkisi birden** | Audience JwtBearer'da (fail-closed, orta katman), `amac` talebi çözümde. Access token'a da `aud=kuyumcu-api` eklendi — bu değişiklik `AccessUret`in tek çağrı noktalarında (AuthModulu ×3, HttpOrtami ×1) kapalıdır. |
| `denetim.sh` `BEKLENEN_VARLIK=15 → 17` | **Dokunulmadı** | Betik `-ge` ile karşılaştırıyor ve kodda bugün **35** kiracı varlığı var; 17'ye çıkarmak kapıyı sıkılaştırmaz, yalnız sayıyı yeniden yanlışlar. Denetim betiği bu pakette değiştirilmedi (gerçek sayı 35 olarak raporlanıyor). |
| Faz B: `/kullanicilar/{id}/mfa-sifirla`, `ZorunluRoller`, ADR-0016, ekranlar | **Yapılmadı** | Görev kapsamı açıkça **45-API Faz A**; ekran 45-UI'de, Faz B ayrı. `MfaAyarlari`'na `ZorunluRoller` **konmadı** (kullanılmayan yapılandırma alanı yanlış güven verir). |
| Kurtarma kodu "10-14 karakter ise kurtarma kodu" | Normalize edilmiş uzunluk **16** | Biçim 16 karakter Crockford-base32 olarak mühürlendi (birleşik koşul); 6 hane + tümü rakam → TOTP, 16 karakter → kurtarma kodu. Ayraç/boşluk ve O→0, I/L→1 normalize edilir (yapıştırma ve elle yazım). |
| Mevzuat: KVKK Rehberi "DOGRULANMALI" | Koda **hiçbir kapı yazılmadı** | Paket güvenlik paketidir; hiçbir davranış mevzuat iddiasına bağlanmadı. Rehber metni bu oturumda açılmadı — pazarlama/belge metnine yazılmadan önce doğrulanmalı (bkz. §9). |

---

## 3. Derleme ve migration

```
dotnet build Kuyumcu.sln --nologo → 0 Uyarı, 0 Hata
dotnet ef migrations add MfaTotp --project Kuyumcu.Api   (build'li, --no-build KULLANILMADI)
dotnet ef database update --connection ...kuyumcu_net    → Applying migration '20260916175238_MfaTotp'. Done.
```
`kuyumcu_net` doğrulaması: `show tables like 'kullanici_mfa'` → `kullanici_mfa`, `show tables like 'mfa_kurtarma_kodlari'` → `mfa_kurtarma_kodlari`. Migration sayısı 26 → **27**.

---

## 4. Testler

| Kapsam | Dosya | Adet |
|---|---|---|
| Saf (RFC 6238 / Base32 / drift) | `Kuyumcu.Tests/TotpTestleri.cs` | 9 |
| DB (şifreli saklama, tekrar, kilit, kurtarma, kapatma, kiracı izolasyonu) | `Kuyumcu.Tests/MfaTestleri.cs` | 9 |
| HTTP (giriş akışı, audience, ömür, pv, oran sınırı, denetim izi) | `Kuyumcu.Tests/MfaHttpTestleri.cs` | 14 |

**Yeni test: 32.** Tam süit: **1540 test, 1540 geçti, 0 başarısız** (trx: `…/scratchpad/trx-p45`). Önce: 1508.

Ara kırmızı (çözüldü ve kanıtı burada): `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` — *"Politika haritasında damgası olmayan varlık(lar): KullaniciMfa, MfaKurtarmaKodu"*. UX-1 kapısı yeni varlıkların düzeltme damgasını zorunlu kılıyor; ikisi de SALT_OKUNUR + gerekçe + alternatif yol ile haritaya eklendi (MFA kaydı yerinde düzenlenmez: gizi değiştirilebilir bir kayıt, çalınan oturumun ikinci faktörü kendi telefonuna taşımasına izin verirdi).

---

## 5. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M45-1 | `son_kullanilan_adim` (tekrar) kapısı kaldırıldı | `MfaTestleri.Ayni_TOTP_kodu_ikinci_kez_reddedilir` | `cmp` birebir |
| M45-2 | `KilitKontrol` gövdesi boşaltıldı | `MfaTestleri.Kilitliyken_dogru_kod_bile_reddedilir` | `cmp` birebir |
| M45-3 | Giriş dalındaki MFA kontrolü devre dışı (`false && mfaEtkin`) | `MfaHttpTestleri.Mfa_etkinken_giris_token_dondurmez` | `cmp` birebir |
| M45-4 | Giz `Sifrele` yerine düz (`"v1:" + giz`) yazıldı | `MfaTestleri.Gizli_veritabaninda_duz_metin_degildir` (+4 test daha) | `cmp` birebir |
| M45-5 | Kurtarma kodu `kullanildi` işaretlemesi kaldırıldı | `MfaTestleri.Kurtarma_kodu_tek_kullanimliktir` **ve** `MfaHttpTestleri.Kurtarma_kodu_girisde_tek_kullanimliktir` | `cmp` birebir |
| M45-6 | Drift penceresi 1 → 10 adım | `TotpTestleri.Drift_31_saniye_reddedilir` | `cmp` birebir |
| M45-7 | `pv` (parola sürümü) kapısı kaldırıldı | `MfaHttpTestleri.Parola_degisince_bekleyen_meydan_okuma_gecersiz` | `cmp` birebir |
| **M45-8 (VETO)** | `ValidateAudience = false` | `MfaHttpTestleri.Meydan_okuma_jetonu_korumali_uca_giremez` | `cmp` birebir |

Yedekler: `…/scratchpad/mut-yedek/p45/`. Son mutasyondan sonra `dotnet build` → 0 uyarı / 0 hata; `denetim.sh` TEMİZ.

---

## 6. Denetim betiği

```
✓ IgnoreQueryFilters (yasak)                     0
✓ new KuyumcuDbContext (yalnız fabrika)          1
✓ Find/FindAsync (yasak)                         0
✓ ExecuteUpdate/Delete (beyaz liste 4)           4     ← ARTMADI (SonGiris taşındı)
✓ Hareketler.Add (yalnız HareketFabrikasi)       1
✓ IKiraciVarlik uygulayan varlık                 35
✓ Finansal varlıkta [HttpDelete] (0 olmalı)      0
DENETİM TEMİZ.
```

---

## 7. Canlı doğrulama (localhost:5198, gerçek veri)

Test kiracısı: **PAKET45-TEST** (`paket45@test.local`). TOTP kodları, uygulamanın kendi kodundan **bağımsız** bir Python RFC 6238 uygulamasıyla üretildi (`scratchpad/totp.py`, `hmac/sha1`).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | POST `/auth/mfa/kur` | 200, otpauth URI | **200** · `otpauth://totp/Kuyumcu%20ERP:paket45%40test.local?secret=<GIZ>&issuer=…&algorithm=SHA1&digits=6&period=30` |
| 2 | POST `/auth/mfa/dogrula` (bağımsız uygulamanın kodu) | 200, 10 kurtarma kodu | **200**, `etkin:true`, 10 kod (19 karakterlik gruplu biçim) |
| 3 | GET `/auth/mfa/durum` | giz yok | `{"etkin":true,"durum":"ETKIN","yontem":"TOTP","kalanKurtarmaKodu":10,…}` |
| 4 | POST `/auth/giris` | token YOK | **200**, alanlar = `['gecerlilikDakika','mfaGerekli','mfaToken']` — accessToken/refreshToken **yok** |
| 5 | **VETO**: meydan okuma jetonuyla GET `/api/v1/cari` | 401 | **401** `{"message":"Geçersiz veya süresi dolmuş token",…}` |
| 6 | Access token'la POST `/auth/mfa/giris` | 401 | **401** `{"message":"Doğrulama oturumu geçersiz ya da süresi doldu…"}` |
| 7 | Doğru kodla POST `/auth/mfa/giris` | 200 access+refresh | **200**, alanlar = `['accessToken','kullanici','refreshToken']` |
| 8 | **Aynı kod ikinci kez** (yeni jetonla) | 401 | **401** "Doğrulama kodu hatalı…" (tekrar kapısı) |
| 9 | Kurtarma koduyla giriş | 200 | **200**; aynı kod tekrar → **401**; `kalanKurtarmaKodu` **10 → 9** |
| 10 | 5 yanlış kod | kilit | 401,401,401,401 → 5.'de **429** `{"message":"Çok fazla hatalı deneme: hesap 15 dakika kilitli"}` |
| 11 | **API yeniden başlatıldı** (bellek sayacı sıfır) + DOĞRU kod | yine kilitli | **429** aynı mesaj — DB sayacı süreç yeniden başlatmayı aşıyor |
| 12 | POST `/auth/mfa/kaldir` yanlış parola + geçerli kod | 401 | **401** "Şifre veya doğrulama kodu hatalı" |
| 13 | POST `/auth/mfa/kaldir` doğru parola + geçerli kod | 200 | **200** `{"etkin":false}` |
| 14 | MFA kapalıyken POST `/auth/giris` | eski davranış | **200**, `['accessToken','kullanici','refreshToken']` |

> Not (adım 11→12 arası): kilit testinden sonra kapatma ucunu **kendi test kiracımda** deneyebilmek için `kullanici_mfa.kilit_bitis`/`yanlis_deneme` SQL ile sıfırlandı. Başka hiçbir kiracıya dokunulmadı.

### SQL kanıtı
```sql
select durum, yanlis_deneme, kilit_bitis, left(gizli_sifreli,3), length(gizli_sifreli), son_kullanilan_adim …
→ ETKIN  5  2026-09-16 18:17:04  v1:  83  59652723

select detay, hassas, count(*) … where detay like 'mfa%' group by detay,hassas
→ mfa-kuruldu 1 1 · mfa-etkinlesti 1 1 · mfa-giris-basarisiz 1 7 · mfa-giris-basarili 1 5
  mfa-kilitlendi 1 1 · mfa-kapatma-reddedildi 1 3 · mfa-kapatildi 1 1      (hepsi hassas=1)

-- sızıntı taraması (düz giz / düz kurtarma kodu)
denetim_kayitlari içinde → 0 · kullanici_mfa.gizli_sifreli içinde → 0 · mfa_kurtarma_kodlari.kod_hash içinde → 0
API günlükleri (4 dosya) içinde grep -c → 0, 0, 0, 0

-- kapatmadan sonra
kullanici_mfa → 0 satır · mfa_kurtarma_kodlari → 0 satır · denetim izi KALDI
```

### Temizlik
API PID kapatıldı, `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş**. Test kiracısı `tenant_id` taşıyan 35 tablodan ve `tenants`'tan silindi.
`select count(*) from tenants` → **7** (başlangıçtaki değer). Kalan kiracılar: Demo Kuyumculuk, ByGatech Kuyumculuk, Düzeltme Sonrası Kuyumculuk, Demo Kuyumcu, Ramazan Deneme, **QA-TEST-1 Kuyumculuk, QA-TEST-2 Kuyumculuk** (QA fikstürlerine dokunulmadı). `kullanici_mfa` ve `mfa_kurtarma_kodlari` → 0 satır.

---

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| **VETO (Mehmet)** — meydan okuma jetonu bearer sayılmaz | ✅ | Ayrı audience + `ValidateAudience=true`; canlı adım 5 (401), test `Meydan_okuma_jetonu_korumali_uca_giremez`, mutasyon **M45-8** |
| Access token'la `/auth/mfa/giris` reddedilir | ✅ | Canlı adım 6, test `Access_token_ikinci_adimda_kabul_edilmez` |
| "Geçici atlama yolu" yok (FAIL-CLOSED 1) | ✅ | `Giris` MFA dalında token üretimi yok; mutasyon M45-3 |
| Giz AES `v1:`, kurtarma kodları SHA-256, düz metin yalnız üretim yanıtında | ✅ | SQL `v1:`/83 bayt; sızıntı taraması 0; testler |
| TOTP secret / kurtarma kodu ekrana-loga yazılmaz | ✅ | API günlüklerinde grep 0; denetim izinde 0; `/mfa/durum` gizi döndürmez |
| Oran sınırı 5/dk, kullanıcı anahtarı, **imza doğrulamalı** çözüm | ✅ | `OranSiniri.MfaAnahtari` → `TokenServisi.MfaMeydanOkumaCoz`; test `Ikinci_adim_oran_siniri_429_ve_retry_after_dondurur` |
| DB deneme sayacı + kilit (süreç yeniden başlasa da) | ✅ | Canlı adım 10–11 |
| Tekrar kapısı (aynı adım ikinci kez çalışmaz) | ✅ | Canlı adım 8, mutasyon M45-1 |
| Kurtarma kodu tek kullanımlık | ✅ | Canlı adım 9, mutasyon M45-5 |
| RFC 6238 Ek B vektörleri | ✅ | `TotpTestleri` (59→287082, 1111111109→081804, 1234567890→005924) |
| Denetim kaydı (açıldı/kapandı/başarısız) Hassas=true | ✅ | SQL tablosu §7 |
| MFA kapatma parola **ve** kod | ✅ | Canlı adım 12–13; DB testleri |
| SAHIP'in MFA'sını yalnız kendisi kapatabilir | ✅ | Kullanıcı kimliği **jetondan** gelir; başka kullanıcının kaydına dokunan uç YOK (Faz B sıfırlama ucu açılmadı) |
| `ExecuteUpdate/Delete` beyaz listesi 4'te kalır | ✅ | denetim.sh §6; `SonGiris` taşındı |
| Kapatmada `Remove` + `SaveChanges` (toplu silme yok) | ✅ | `MfaServisi.Kaldir` |
| Sozlukler TR+EN birlikte | ✅ | 16 anahtar, iki blok aynı düzenlemede; `required` kapısı derlemeyi geçti |
| Zaman bağımlı testler sahte saat/deterministik | ✅ | Kilit testi `kilit_bitis`'i doğrudan kurar; süresi dolmuş jeton `dakika: -1` ile üretilir (uyku yok) |
| Mevcut testler 0 başarısız | ✅ | 1540/1540 |

### DoD sözleşmesi (Bertan koşulu)
| Varlık | Düzenleme yolu | Gerekçe |
|---|---|---|
| `KullaniciMfa` | **SALT_OKUNUR** — PATCH yok. Düzeltme: `POST /auth/mfa/kaldir` (parola + kod) → `POST /auth/mfa/kur` | Gizi düzenlenebilir bir kayıt, çalınmış bir oturumun ikinci faktörü kendi cihazına taşımasına izin verirdi. `DuzeltmePolitikasi` haritasında gerekçe + alternatif yol ile damgalı (ekran `DegismezIpucu` ile gösterecek). |
| `MfaKurtarmaKodu` | **SALT_OKUNUR** — tüketilen kod silinmez, işaretlenir. Yenileme: `POST /auth/mfa/kurtarma-yenile` (tamamını değiştirir) | Tek kullanımlık sır; kısmi düzeltme diye bir şey yoktur, kullanım izi denetim için kalır. |
| Finansal varlık | **dokunulmadı** | Bu paket defter/kasa/fatura tablolarına hiç dokunmaz; hard DELETE yok. |
| Yetki/iz | Tüm MFA uçları `[Authorize]`, kullanıcı kimliği jetondan; her olay `Hassas=true` denetim kaydı | §7 SQL |

Ekran DoD maddeleri (her düğme istek atar, tek düzenleme kalıbı, sessiz form yasağı, tek dokunuş) bu pakette **uygulanamaz** — ekran yok; 45-UI'de (dalga 2c) karşılanacak. Sunucu tarafı hazır: `/mfa/durum` ekranın boş/etkin/kilitli durumlarını ayırt edebilecek alanları (`durum`, `kilitli`, `kalanKurtarmaKodu`) döndürür.

---

## 9. Açık kalanlar / kurucuya ve mali müşavire notlar

1. **SAHİP KİLİTLENME RUNBOOK'U (Batuhan/DevOps) YAZILMADI** — birleşik koşul: runbook yazılmadan **Faz A canlıya alınmaz**. Bu bir süreç kapısıdır, test değildir. Tek kullanıcılı dükkânda SAHİP telefonunu ve kurtarma kodlarını kaybederse içeri girecek kimse kalmaz; sunucuda elle kapatma yolu (`delete from kullanici_mfa where …`) runbook'a yazılmalı. **T-1 üretime yayın adımı bu madde kapanmadan başlayamaz.**
2. **Anahtar rotasyonu planına "MFA gizlerinin yeniden şifrelenmesi" maddesi eklenmedi** (rotasyon planı belgesi bu pakette değiştirilmedi). `AlanSifreleme` sürüm eşleşmezse istisna atar; rotasyon günü tüm MFA kullanıcıları kilitlenirdi. Bugün rotasyon Faz B'de olduğu için canlı risk yok, ama sevkten önce kapanmalı.
3. **Üçüncü taraf doğrulayıcı uygulamayla (Google/Microsoft Authenticator) elle kanıt alınmadı** — bu oturum başsız (headless) çalıştığı için telefon uygulaması kullanılamadı. Yerine, uygulamanın kodundan **bağımsız** bir Python RFC 6238 uygulamasıyla üretilen kodlarla canlı giriş yapıldı ve RFC Ek B vektörleri testte yeşil. Aşkın'ın "ekran görüntüsü + zaman damgası" koşulu **açık**; kurucu ya da QA, otpauth URI'yi gerçek bir uygulamaya ekleyip bir kez doğrulamalı.
4. **Faz B kapsam dışı**: yönetici `mfa-sifirla` ucu, kiracı düzeyinde zorunluluk (`ZorunluRoller`), `amr=mfa` ile hassas uçlarda MFA şartı, ADR-0016 + tehdit modeli/ASVS güncellemesi. Ayrı oylama/paket.
5. **Ekran (45-UI)**: Giriş ikinci adımı (`autocomplete="one-time-code"`, yapıştırma engeli YOK — WCAG 2.2 §3.3.8), Ayarlar kartı, kurtarma kodlarının bir kez gösterimi + indirme. Dalga 2c'de Kullanicilar/Ayarlar dokunuşuna gömülecek; sözlük anahtarları hazır.
6. **Mevzuat**: KVKK md.12/1 genel yükümlülüğü dışında MFA'yı **adıyla** zorunlu kılan bir hüküm yok; MASAK/5549 perakende kuyumcuya MFA yükümlülüğü getirmez. KVKK Kurumu "Kişisel Veri Güvenliği Rehberi"nin MFA tavsiyesi bu oturumda **açılıp doğrulanmadı** — pazarlama veya uyum metnine yazılmadan önce güncel sürümü doğrulanmalı (mali müşavir/hukuk). Kodda bu iddiaya dayanan hiçbir kapı yok.
7. **Saat kayması**: sunucuda NTP şarttır (±1 adım = ±30 sn tolerans). Hata mesajına "telefonunuzun saatinin otomatik ayarlı olduğundan emin olun" cümlesi eklendi.
8. **Access token audience'ı değişti** (`aud=kuyumcu-api`). Yayın sırasında **eski, audience'sız access token'lar geçersiz olur** (en fazla 15 dk ömür): dağıtım anında açık oturumlar bir kez yeniden giriş ya da refresh isteyebilir. Refresh token'lar etkilenmez.

---

## 10. Değişen dosyalar (bu paket)

**Yeni**
```
dotnet/Kuyumcu.Api/Common/Totp.cs
dotnet/Kuyumcu.Api/Modules/Auth/MfaModulu.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916175238_MfaTotp.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916175238_MfaTotp.Designer.cs
dotnet/Kuyumcu.Tests/TotpTestleri.cs
dotnet/Kuyumcu.Tests/MfaTestleri.cs
dotnet/Kuyumcu.Tests/MfaHttpTestleri.cs
```
**Değişen**
```
dotnet/Kuyumcu.Api/Auth/TokenServisi.cs
dotnet/Kuyumcu.Api/Common/OranSiniri.cs
dotnet/Kuyumcu.Api/Data/Entities.cs
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs
dotnet/Kuyumcu.Api/Program.cs
dotnet/Kuyumcu.Api/appsettings.json
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
```
(`git status --porcelain` çıktısındaki diğer satırlar önceki paketlere aittir; commit yapılmadı.)
