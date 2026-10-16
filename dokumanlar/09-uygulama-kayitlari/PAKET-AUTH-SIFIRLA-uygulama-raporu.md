# PAKET AUTH-SIFIRLA — "Parolamı unuttum" uygulama raporu

**Dalga:** AUTH-SIFIRLA (01.10 · şerit t9 · API 5188 / vekil 5189 · CDP 9981-9983 · KUYUMCU_TEST_DB=kuyumcu_test_t9)
**Roller:** Taşkın (backend), Ertuğ (frontend), Mehmet'in güvenlik maddeleri (1)-(7) bağlayıcı.
**Durum:** BİTTİ (tur 2, 01.10 ~12:00). Paket kapısının 7 maddesi sağlandı. Doğrulayıcının tur 1 bulguları (2 ORTA, 3 DÜŞÜK, 3 KRİTİK tasarım) **§ 14**'te tek tek kapatıldı ya da sahibine devredildi. COMMIT yapılmadı.
**Kurul 5 engeli:** KOD HAZIR, **KANAL KARARI BEKLİYOR.** Üretimde e-posta kanalı (SMTP/sağlayıcı) yok, bu yüzden özellik üretimde kapalı doğar. Hazırlık kuruluna "kapandı" olarak DEĞİL, "kod hazır, kanal kararı bekliyor" olarak taşınmalı (bkz. § 11, § 14 B-2).

---

## 1. Spesifikasyonun koda karşı doğrulanması

| İddia (kurul 5 / görev metni) | Kodda bulunan | Sonuç |
|---|---|---|
| "Şifremi unuttum yok, yalnız yönetici sıfırlaması var" | `Giris.razor`'daki "Parolamı unuttum" düğmesi yalnız yardım metni açıyordu. Kendi kendine sıfırlama ucu yoktu. Yönetici yolu `PATCH /kullanicilar/{id}/sifre` (KullaniciModulu). | DOĞRU |
| "Mevcut e-posta/bildirim altyapısını bul" | `IBildirimKanali` var, tek uygulaması `UygulamaIciKanal`. **ADR-0016 (dış kanal tekeli):** ürün e-posta/SMS göndermez. denetim.sh "IBildirimKanali DI kaydı = 1" sayacı ikinci kanalı engelliyor. SMTP istemcisi ya da yapılandırması **YOK**. | Sapma S-1'e bakın |
| Parola kuralı | `SifreDegistirDto`: 8-72 karakter, en az bir harf ve bir rakam (`^(?=.*[0-9])(?=.*[a-zA-Z]).+$`). | Aynısı kullanıldı. Yansıma testi farkı yakalar. |
| Refresh iptali "JETON mantığıyla uyumlu" | `AuthServisi.TumTokenlerIptalEt`, ExecuteUpdate beyaz listesindeki kullanım. | **Yeniden kullanıldı:** `private` → `internal`. Yeni ExecuteUpdate EKLENMEDİ, sayaç 5'te kaldı. |
| MFA | Giriş, MFA etkinse jeton vermiyor. Meydan okuma jetonu parola sürümüne (pv) bağlı. | Sıfırlama oturum AÇMAZ. Sonraki giriş MFA'dan geçer. |

## 2. Yapılanlar (ekip oyu maddeleri)

| Madde | Uygulama |
|---|---|
| (1) Yanıt her zaman aynı | `POST /auth/parola-sifirlama/istek` hesap var da olsa yok da olsa **bayt bayt aynı** 200 gövdesini döner (`IstekYanitMesaji`). **Süre tabanı:** yanıt en erken `YanitTabaniMs` (varsayılan 600 ms) sonra döner, iş daha kısa sürerse kalan süre beklenir. Gönderim ya da DB hatası yanıtı değiştirmez, Error düzeyinde günlüğe düşer (gizlenmez). Pasif kullanıcıya posta gitmez, yanıt yine aynıdır. Hız sınırı da hesap var/yok fark etmeksizin sayılır. |
| (2) Jeton | `RandomNumberGenerator.GetBytes(32)` ile üretilir, base64url (43 karakter). DB'de **yalnız SHA-256 hex** tutulur (`jeton_hash`, tekil indeks). Ömür 30 dk (`JetonOmruDakika`). Tek kullanımlıktır (`kullanildi` damgası + `[ConcurrencyCheck]`: eşzamanlı ikinci "tamamla" isteği `DbUpdateConcurrencyException` alır ve 400 döner). Yeni istek kullanıcının açık jetonlarını `iptal_edildi=1` yapar, başarılı sıfırlama da diğer açık jetonları iptal eder. Bağlantı jetonu **#parçada** taşır (`/sifre-sifirla#jeton=…`): parça sunucuya, vekil günlüğüne ve Referer'a gitmez. Bağlantı kökü **yapılandırmadan** gelir (`ParolaSifirlama:BaglantiTabani`). Host başlığından türetilmez, böylece Host zehirlenmesi kapanır. |
| (3) Hız sınırı | `ParolaSifirlamaHizSiniri`: **gövdedeki** e-posta (normalize) + IP başına 15 dk'da 3 istek. 4. istek 429 + `Retry-After` alır. Başlıkla aşılamaz. Ek olarak kaba bir IP tavanı var: `OranSiniri.ParolaSifirlama` 20 / 15 dk, iki uçta da geçerli. |
| (4) Parola kuralı + refresh iptali + denetim | Servis içinde elle kontrol edilir (8-72, harf+rakam, tekrar eşleşmesi), alan altı `alanlar.yeniSifre` / `alanlar.yeniSifreTekrar` döner. Kural ihlalinde jeton **yanmaz**. Başarıda BCrypt(12) kullanılır, ardından `TumTokenlerIptalEt` aynı transaction içinde çalışır. Denetim: `DenetimServisi.YazmaKaydet(AktifKullanici(kullanıcı), "Kullanici", id, "parola e-posta bağlantısıyla sıfırlandı (Parolamı unuttum) · tüm oturumlar kapatıldı", hassas:true)`. Parola, jeton ve özet DEĞERİ yazılmaz. |
| (5) MFA atlanmaz | `tamamla` yanıtı yalnız `{mesaj}` içerir: access, refresh ya da mfa jetonu YOK. Sonraki girişte `mfaGerekli=true` döner. |
| (6) Fail-closed | Kanal yapılandırılmamışsa ya da `BaglantiTabani` yoksa `GET …/durum` → `acik:false` + mesaj döner, `POST …/istek` → **503** "…dükkân sahibine ya da destek hattına başvurun." "Gönderildi" denmez, jeton üretilmez. Ekran formu çizmez, doğru yolu (sahip/destek + yönetici yolu) yazar. 503 alınırsa da kapalı görünüme geçer. |
| (7) Gerçek e-posta YOK | `IParolaSifirlamaPostasi` dar bir arayüzdür, bildirim kanalı değildir. IBildirimKanali sayacı 1'de kaldı. Uygulamaları: `KapaliParolaSifirlamaPostasi` (varsayılan, çağrılırsa patlar) ve `DosyaParolaSifirlamaPostasi` (.eml, Unix 600). DOSYA **yalnız Development/Testing ortamında** kabul edilir, üretimde KAPALI'ya düşer (jeton diske yazılmaz). Testte bellek içi `TestPostaKutusu` kullanılır. SMTP uygulaması YAZILMADI. |

**Uçlar** (`ParolaSifirlamaController`, `[AllowAnonymous]`, rota `api/v1/auth/parola-sifirlama`):
`GET durum` · `POST istek` (`[EnableRateLimiting(ParolaSifirlama)]`) · `POST tamamla` (aynı politika).

**Ekran:**
- Yeni `Pages/SifreSifirla.razor` (`/sifre-sifirla`). Giriş ekranının İKİ PANEL sınıflarını kullanır, yeni CSS yok.
  - İstek adımı: e-posta → "Bağlantı gönder".
  - Yeni parola adımı: iki parola alanı (kural ipucu alanın altında) → "Parolayı kaydet".
  - Her adımda tek birincil düğme var. Boş/geçersiz gönderimde alan altında Türkçe mesaj çıkar, istek gitmez. Başarı ve hata bantları var.
  - Durum okunamazsa ayrı hata metni çıkar (UX-S28).
  - Jeton reddedilirse "Yeni bağlantı iste" yolu gösterilir.
  - Jeton yalnız parçadan okunur. Açık sekmede yalnız parça değişse de okunur (`LocationChanged`).
- `Giris.razor` (yalnız bağlantı): "Parolamı unuttum" bölmesine `#giris-parola-sifirla-bag` (`/sifre-sifirla`) bağlantısı eklendi. Yönetici yolu metni aynen kaldı. Mevcut id'ler değişmedi.
- Kabuk: `AnaYerlesim.GirisOncesiRota` artık "/giris" ve "/sifre-sifirla" rotalarını kapsar. Oturumsuz kullanıcı girişe atılmaz, sayfa kabuksuz çizilir. Oturum açıkken de "/"a atılmaz.
- `ApiIstemci.GirisOncesiUc`: `/auth/parola-sifirlama/` eklendi (düşmüş sekmeden de gider).
- Metinler `Sozlukler.AuthSifirla.cs` (TR+EN, konumsal record) dosyasında. Ekranda jargon yok ("jeton", "token", "SMTP" geçmez, testle bekçilenir).

## 3. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S-1 | **Gerçek SMTP kanalı yazılmadı. Üretimde özellik KAPALI doğar.** | ADR-0016 (dış kanal tekeli) sözleşme, KVKK ve gönderen kimliği olmadan dışarı e-posta göndermeyi yasaklıyor. Görev maddesi (6) bu durumu fail-closed tanımlıyor. Açmak kurucu kararıdır, bkz. § 11. |
| S-2 | Kayıtlı paylaşılan dosyalara dokunuldu: `RotaBasliklari.cs`, `MenuTanimi.MenusuzRotalar`, `RotaModulleri.cs`, `DuzeltmePolitikasi` varlık haritası, `denetim.sh` (BEKLENEN_VARLIK 75→76, sayfa tavanı 48→49, P02_ROTA_BEKLENEN 48→49), `ham/olcum-2026-09-18.json` (76 / 49), `betikler/qa-turu.js` (ROTA_KAPSAM_BEKLENEN 49), `docs/KULLANIM_KILAVUZU.md` (+ `kilavuz-uret.sh` ile `kilavuz.html`), testlerdeki sabit sayılar (`RotaBaslikTestleri` 49, `P37cPanelVeKapsamTestleri` 49/46), `AnaYerlesim.razor`, `ApiIstemci.cs`. | Yeni rota ve yeni varlık bu kayıtlara yazılmadan kapı kırmızı kalıyordu (ilk tam süitte 5 kırmızı, denetim'de 7 ✗). Hepsine yalnız EKLEME yapıldı, satırlar YASAL-1 emsaliyle gerekçeli. YASAL-4'ün aynı satırlarına (75 notu vb.) dokunulmadı. |
| S-3 | Sayfa istisna kütüğüne EKLENMEDİ. Yerine `data-duzenleme-yolu="Ayarlar.razor"` kullanıldı. | P-06 istisna tavanı "ARTIRILMAZ" (3). Sayfanın yazdığı tek kullanıcı verisi paroladır. Sonradan değişikliği Ayarlar'daki şifre değiştirme (`PATCH /kullanicilar/ben/sifre`) ya da yönetici yolu yapar. Bu beyan doğrudur ve makineyle doğrulanır. |
| S-4 | Rota başlığı "Parolamı unuttum", sayfa içi başlık "Parolanı yenile" / "Yeni parola belirle". | Canlı ölçümde yakalandı: başlık rota adıyla aynı olunca `SayfaBasi` onu "üst barda var" sayıp gizliyordu. Oysa bu sayfada üst bar yok. bUnit testi eklendi. |
| S-5 | Açık sekmede parça değişimi işleniyor (`LocationChanged`). | Canlı ölçümde yakalandı: aynı sekmede `/sifre-sifirla` → `#jeton=` geçişinde form değişmiyordu. bUnit testi eklendi. |
| S-6 | Sayfa açıklama satırı (`sayfa-basi-aciklama`) giriş düzeninde görünmüyor. | Mevcut giriş düzeninin davranışıdır, Giris.razor'da da aynı. Değiştirilmedi. Yeni parola kuralı alanın altında ipucu olarak görünür. |
| S-7 | `ParolaSifirlamaJetonu` için PATCH/DELETE ucu yok. | Bkz. § 9 DoD sözleşmesi: güvenlik jetonudur, düzenlenmez. |

## 4. Migration

- Ad: `20261001065111_AuthSifirlaParolaJetonlari`. Önce `dotnet build Kuyumcu.sln` yeşil, ardından build'li `dotnet ef migrations add` çalıştırıldı. `--no-build` kullanılmadı.
- `Up()` yalnız `parola_sifirlama_jetonlari` tablosunu oluşturur: id, tenant_id, kullanici_id, jeton_hash(64), gecerlilik, kullanildi NULL, iptal_edildi, ip_adresi, olusturuldu. İndeksler: `IX_…_jeton_hash` UNIQUE ve `IX_…_tenant_id_kullanici_id`. Başka şeridin değişikliği karışmadı (dosya okundu). .cs 2636 bayt, .Designer.cs 258207 bayt.
- `kuyumcu_net`'e `dotnet ef database update` ile uygulandı ("Applying migration '20261001065111_AuthSifirlaParolaJetonlari'. Done."). `__EFMigrationsHistory` en üstte bu kaydı gösteriyor. `SHOW CREATE TABLE` okundu.
- Varlık `IKiraciVarlik` + (tenant_id, kullanici_id) indeksi taşır. Kiracı süzgeci otomatik kurulur. Anonim akış `SistemKapsami.Sistem()` ile çalışır (Giris/Yenile ile aynı kalıp).

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Paket filtreli (`~AuthSifirla`) — son kod | 61 | 61 | 0 |
| İlgili filtreli (AuthSifirla, Kilavuz, RotaBaslik, S1eGiris, TK4, SayfaBasi) | 193 | 193 | 0 |
| Tam süit tur 1 (trx-pAUTH-SIFIRLA) | 8672 | 8665 | 6 (5'i bu paketin kayıt eksikleri, düzeltildi. 1'i YASAL-4 doğrulayıcısı) |
| **Tam süit tur 2 — son kod** (trx-pAUTH-SIFIRLA-2) | 8675 | 8663 | 11 (hiçbiri bu paketin değil, bkz. § 10). 1 atlandı. |

Yeni test sayısı: **61**
- `AuthSifirlaTestleri`: gerçek HTTP, 20 vaka.
- `AuthSifirlaSafTestleri`: 12 vaka.
- `AuthSifirlaEkranTestleri`: bUnit + saf, 29 vaka.

Paket öncesi süit büyüklüğü bu ağaçta yaklaşık 8614'tü (8675 − 61; başka şeritler de test ekliyor).

Sınanan kabul maddeleri:
- var/yok e-posta için aynı gövde ve süre tabanı (≥ taban, fark < taban)
- 32 bayt jeton, DB'de düz metin 0 (ham SQL `CONCAT_WS … LIKE`), 30 dk ömür
- tek kullanım, süresi dolunca 400, ikinci istek eskiyi iptal eder, kullanım diğer açık jetonları öldürür, uydurma jeton tek tip mesaj alır
- 4. istekte 429 + Retry-After, kayıtsız e-postada da aynı 429, başka IP ayrı bölüt
- sıfırlama sonrası eski refresh 401 ve `refresh_tokenler` hepsi iptal
- parola kuralı (3 vaka) ve tekrar uyuşmazlığı alan hatası verir, jeton yanmaz
- denetim kaydı yazılır, değer yazılmaz
- MFA atlanmaz
- kanal yokken 503 ve jeton üretilmez
- fabrika fail-closed (üretimde DOSYA → kapalı, SMTP → kapalı)
- dosya kanalı izni 600
- parola kalıbının KullaniciModulu ile aynı olduğu (yansıma)
- ekran: kapalı / 503 / durum hatası / alan altı mesaj / gövde ayrıştırma / tek birincil / parça okuma / red sonrası yeni bağlantı / başlık görünürlüğü / giriş bağlantısı

## 6. Mutasyon kanıtları

Yöntem: kaynak `/private/tmp/kuyumcu-serit/mut-yedek/AUTH-SIFIRLA/<kod>.yedek` dosyasına kopyalandı, `perl -0777 -pi` ile tek eşleşmeli değişiklik yapıldı, test projesi derlendi, `--filter` ile koşuldu, ardından yedek geri kopyalandı ve `cmp` ile doğrulandı. Hepsinde derleme hatası 0, `cmp` = AYNI. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 hata.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M1-1 | **hash kaldır:** `JetonUret` → `(duz, duz)` | `DB_de_jeton_DUZ_METIN_yok_yalniz_SHA256_ve_30_dk`, `Jeton_en_az_32_bayt_rastgele_ve_hash_SHA256` + 9 akış testi (11 kırmızı) | AYNI |
| M1-2 | **tek kullanım kaldır:** `jeton.Kullanildi is not null ||` silindi | `Jeton_TEK_KULLANIMLIK_ikinci_kullanim_400` | AYNI |
| M1-3 | **süre kaldır:** `|| jeton.Gecerlilik <= simdi` silindi | `Suresi_dolan_jeton_400_parola_degismez` | AYNI |
| M1-4 | **hız sınırı kaldır:** `… && false` | `Hiz_siniri_ayni_eposta_ayni_IP_DORDUNCU_istekte_429`, `Hiz_siniri_KAYITSIZ_epostada_da_ayni_429_varlik_sizmaz` | AYNI |
| M1-5 | refresh iptali kaldır (`TumTokenlerIptalEt` satırı) | `Sifirlama_sonrasi_ESKI_refresh_jetonlari_401` | AYNI |
| M1-6 | fail-closed kaldır (`if (!Acik)` → `if (false)`) | `Kanal_yokken_istek_503_ve_GONDERILDI_demez_jeton_uretmez` | AYNI |
| M1-7 | yeni istekte eskileri iptal etme kaldır | `Ikinci_istek_ESKI_jetonu_gecersiz_kilar` | AYNI |
| M1-8 | süre tabanı kaldır (`Task.Delay`) | `Var_ve_yok_eposta_AYNI_yanit_AYNI_sure_araligi` | AYNI |
| M1-9 | harf+rakam kuralı kaldır | `Parola_kurali_ihlali_…(sadeceharfler)`, `…(1234567890)` | AYNI |
| M1-10 | jeton ömrü 30 → 300 | `DB_de_jeton_DUZ_METIN_yok_yalniz_SHA256_ve_30_dk`, `Kanal_acikken_durum_acik_doner_hesap_bilgisi_icermez` | AYNI |
| M1-11 | ekran: 503'te "gönderildi" bandı | `Istek_503_donerse_KAPALI_gorunur_gonderildi_DEMEZ` | AYNI |
| M1-12 | kabuk: `/sifre-sifirla` giriş-öncesi rota değil | `Kabuk_GIRIS_ONCESI_rotalar(/sifre-sifirla)`, `(/sifre-sifirla/)` | AYNI |

Hiçbir testi kırmayan mutasyon yok.

## 7. Denetim (`dotnet/denetim.sh`)

Son koşum: tek ✗ **P-30 DEMO_INSAN_SURESI**. Kabulde hariç tutulan, bu paketle ilgisiz bir kalemdir. 303 ✓. Bu paketin dokunduğu sayaçlar:

```
✓ ExecuteUpdate/Delete (beyaz liste 5)           5
✓ IBildirimKanali DI kaydı (1 olmalı)          1
✓ IKiraciVarlik uygulayan varlık                76
✓ Pages: @onclick'siz ve disabled'sız <button>  71 (tavan 71)
✓ Pages/*.razor dosya sayısı                   49 (tavan 49)
✓ P-02: ROTA_KAPSAM (@page ≡ RotaBasliklari)   49
✓ P-02: OLCUM_TABANI_KOPYA                       0 (varlık 76 · rota 49 · record 3)
✓ P-06: DUZENLENEMEYEN_GIRIS                     2 (tavan 3, cırcır)
✓ P-06: DUZENLEME_KALIBI_YOK_SAYFA               0 (-eq 0)
✓ P-06: ISTISNA_KUTUK_TAVAN                      3 (tavan 3, cırcır)
✓ P-09: ROTA_YETKI_ESLESMEZ                      0 (iki yönlü, 49 rota)
✓ P-14: MENU_KAPSAM                              0 (-eq 0: 49 rota tarandı)
✓ P-33: KILAVUZ_EKSIK_EKRAN                      0 (49 rota, hepsi kılavuzda)
✓ P-33: KILAVUZ_URETILMIS                        1 (damga = md + şablon + HARİÇ karması)
```

Build (tur 1): `dotnet build Kuyumcu.sln` 0 hata. ⚠ **Tur 1'deki "bu paketin dosyalarında uyarı 0" beyanı YANLIŞTI** (doğrulayıcı yakaladı): `AuthSifirlaTestleri.cs(507-509)` 3 × xUnit2000 uyarısı vardı. Tur 2'de düzeltildi, bkz. § 14 B-1.

## 8. Canlı doğrulama (API 5188, Development, `ParolaSifirlama__Kanal=DOSYA`, posta dizini `/private/tmp/kuyumcu-serit/AUTH-SIFIRLA/posta`, kiracı `PAKETAUTH-SIFIRLA-TEST`)

Betikler `canli.py` ve `canli2.py`, sonuçlar `canli-sonuc.json` ve `canli-sonuc2.json`. Sistem anahtarı oturum için üretildi (600 izinli dosya, ekrana basılmadı).

| Adım | Beklenen | Gerçek |
|---|---|---|
| Davet + kayıt | 201 / 201 | 201 / 201 (tenant 01M3V5GNNSVZ6BMXEPM462EG9R) |
| GET durum | 200 acik=true | 200 acik=True |
| İstek: kayıtlı / kayıtsız e-posta | 200/200, gövde aynı, ≥600 ms | 200/200, gövde_aynı=True, 604/604 ms |
| Posta kutusu | kayıtlı=1, kayıtsız=0 | 1 / 0. Dosya izni 0o600, jeton 43 karakter |
| SQL: düz metin / SHA-256 eşleşmesi / ömür | 0 / 1 / 1800 sn | 0 / 1 / 1800 sn |
| İkinci istek → eski jeton | iptal_edildi=1 | 200, iptal_edildi=1 |
| Eski jetonla tamamla | 400 | 400 "Bu bağlantı geçersiz, kullanılmış ya da süresi dolmuş…" |
| Kurala uymayan parola | 400 alanlar.yeniSifre | 400 ['yeniSifre'] |
| Tamamla (geçerli) | 200, oturum jetonu yok | 200 "Parolanız yenilendi…", jetonsuz=True |
| Aynı jetonla ikinci tamamla | 400 | 400 |
| SQL: kullanılan jeton | kullanildi dolu, iptal 0 | 1 / 0 |
| Eski refresh `/auth/yenile` | 401 | 401 |
| SQL `refresh_tokenler` (açık / toplam) | 0 / ≥1 | 0 / 2 |
| Giriş: eski / yeni parola | 401 / 200 | 401 / 200 |
| SQL denetim (kullanıcının kendi kaydı) / parola-bağlantı sızıntısı / özet sızıntısı | 1 / 0 / 0 | 1 / 0 / 0 |
| Hız sınırı | 200×3, sonra 429 + Retry-After | [200,200,200] → 429, Retry-After=900 |
| MFA (MÜDÜR kullanıcı, TOTP kur + doğrula) | 200 / 200 | 200 / 200 |
| MFA açık: tamamla → giriş | 200 jetonsuz → mfaGerekli=true, access yok | 200 jetonsuz → 200 mfaGerekli=True, access=yok |
| **Kanal KAPALI** (API ortam değişkensiz yeniden başlatıldı) | durum acik=false, istek 503 | acik=false + mesaj, 503 "…dükkân sahibine ya da destek hattına başvurun." |
| **Tarayıcıda uçtan uca** (başsız Chrome, vekil 5189) | istek → posta → bağlantı → yeni parola → giriş | Başarı bandı "Bu e-posta adresi kayıtlıysa bağlantı gönderildi…", posta +1, yeni parola adımı açıldı, "Parolan yenilendi…", girişe dön + yeni parolayla gerçek giriş sonrası yol `/` |

Not (Anayasa md.1, açıkça): 3. betik koşusunda TEZGÂHTAR kullanıcıyla `/auth/mfa/kur` 403 döndü. Aynı koşuda giriş isteklerinin IP başı 5/dk giriş sınırına (429) takıldığı görüldü. 403'ün nedeni ARAŞTIRILMADI: bu paketin kapsamı değil. MFA maddesi MÜDÜR kullanıcıyla doğrulandı.

### Gerçek tarayıcı ölçümü (sikisma-olc.js `__olcTam`, başsız Chrome, CDP 9981-9983)

Ölçülen sayaçlar: K1 gizli-görünen, K3 taşan/kırık, K5 TRY, K7 hizasız, K8 jargon, K9 binen, K10 elips, K11 hata bandı, K12 düğme dikey ve yatay kaydırma. **Tüm satırlarda hepsi 0.** Birincil düğme sayısı ≤1. Ham kayıtlar `olcum-*.jsonl` ve `ekran-*.log` dosyalarında.

| Rota / durum | Mod | 1280 açık | 1280 koyu | 375 açık | 375 koyu | Kart / ana alan (1280 · 375) |
|---|---|---|---|---|---|---|
| /giris ("Parolamı unuttum" açık) | anonim | 0 | 0 | 0 | 0 | 440/610 (%72) · 343/375 |
| /sifre-sifirla istek | anonim | 0 | 0 | 0 | 0 | 440/610 · 343/375 |
| /sifre-sifirla boş gönderim (alan altı) | anonim | 0 | 0 | 0 | 0 | aynı |
| /sifre-sifirla istek başarı | anonim | 0 | 0 | 0 | 0 | aynı |
| /sifre-sifirla#jeton yeni parola | anonim | 0 | 0 | 0 | 0 | aynı |
| yeni parola uyuşmaz (alan altı) | anonim | — | — | 0 | — | 343/375 |
| yeni parola başarı | anonim | 0 | 0 | 0 | 0 | aynı |
| /sifre-sifirla **kanal kapalı** | anonim | 0 | 0 | 0 | 0 | aynı |
| /sifre-sifirla (oturum açık) | **BASİT** | 0 | 0 | 0 | 0 | aynı (mod düğmesi aria-pressed=true) |
| /sifre-sifirla (oturum açık) | **PRO** | 0 | 0 | 0 | 0 | aynı (mod düğmesi aria-pressed=true) |

Dar sütun: en geniş blok ana alanın %72'si (> %60), gerekçe gerekmez. Sayfa kimlik öncesi bir yüzeydir. BASİT/PRO yalnız oturum açıkken anlamlıdır. İki modda da ölçüldü, ekran aynı.

Ekran görüntüleri (Read ile görüldü), `/private/tmp/kuyumcu-serit/AUTH-SIFIRLA/ekran/`:
- `sifirla-istek__ANONIM_aydinlik_1280.png`
- `sifirla-istek-basari__ANONIM_karanlik_1280.png`
- `sifirla-yeni__ANONIM_karanlik_375.png`
- `sifirla-yeni-uyusmaz__ANONIM_aydinlik_375.png`
- `giris-yardim__ANONIM_karanlik_375.png`
- `sifirla-kapali__ANONIM_aydinlik_375.png`
- `sifirla-istek-oturumlu__PRO_karanlik_375.png`
- (toplam 37 görüntü)

Görülenler:
- Başlık görünüyor.
- Tek birincil düğme var.
- Alan altı hata kırmızı, kutuyu çerçeveliyor.
- Başarı bandı koyu temada okunur.
- Kapalı durum, yönetici yolunu da yazıyor.

### Kapanış

- API (5188), vekil (5189) ve bu şeridin başsız Chrome süreçleri durduruldu. `lsof` ile 5188, 5189 ve 9981-9983 boş.
- Test kiracıları `zsh kiraci-temizle.sh <id> --uygula` ile silindi, üçü de çıkış 0:
  - 01M3V5FSMMBYJQQTKHKYG3YC15 — yarım kalan 1. koşu
  - 01M3V5G7F3P2EV6NE251JSE6ZY — 2. koşu
  - 01M3V5GNNSVZ6BMXEPM462EG9R — 3. koşu
- SQL sonrası:
  - `tenants WHERE ad LIKE 'PAKETAUTH%'` = 0
  - `parola_sifirlama_jetonlari` = 0
  - `kullanicilar WHERE email LIKE 'paketauth-sifirla%'` = 0
- **tenants sayısı:** başlangıçta 14, sonda 13.
  - Fark bu paketten gelmiyor: başka şeritler aynı sürede kiracı açıp sildi. Örneğin `PAKETKP6-TEST` 07:06Z'de, yani bu paket başladıktan sonra açıldı.
  - Bu paketin açtığı 3 kiracının üçü de silindi. `QA-TEST-*` kiracılarına dokunulmadı.

## 9. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Neden |
|---|---|---|
| `ParolaSifirlamaJetonu` (yeni) | **Salt sistem kaydı.** PATCH/DELETE ucu YOK. `DuzeltmePolitikasi` haritasında `S("ParolaSifirlamaJetonu", Sistem)` damgası var. | Güvenlik jetonudur: sistem üretir, sistem tüketir. Kullanıcı görmez. "Düzeltme" = ekrandan **yeni bağlantı iste** (eskisi otomatik iptal olur, madde 2). Silme yerine `iptal_edildi` / `kullanildi` damgası kullanılır. Kiracı silinince satırlar `kiraci-temizle.sh` ile gider. Finansal varlık değildir. |
| `Kullanici.SifreHash` (değişen alan) | Sonradan değişiklik: Ayarlar → şifre değiştir (`PATCH /kullanicilar/ben/sifre`) ya da yönetici Kullanıcılar → Şifre Sıfırla (`PATCH /kullanicilar/{id}/sifre`). Sayfada `data-duzenleme-yolu="Ayarlar.razor"` beyanı var. | — |
| Yetki | Uçlar `[AllowAnonymous]`: kimlik öncesidir. Koruma madde 1-3 ile sağlanır (aynı yanıt, hız sınırı, IP tavanı). | — |
| Denetim | `DenetimServisi.YazmaKaydet` çağrılır (yeniden yazılmadı) ve otomatik yazma izi düşer. | — |

Ekranda her düğme bir istek atar ya da disabled'dır: gönderim sırasında `disabled="@yukleniyor"`. Bağlantılar `href` taşır. Yeni ModalPencere kullanımı yok.

## 10. Açık kalanlar

- **Tam süitte başka şeritlere ait 11 kırmızı** (bu paket DOKUNMADI). Dosyaları 10:30-10:32'de başka şerit tarafından değiştirilmiş: `Bicim.cs`, `Faturalar.razor`, `FaturaModulu.cs`. Görev metnine göre bunlar TOPLU-1 alanı.
  - `BelgeKalanTur2SafTestleri.BK2_Bicim_isletme_gunu_Istanbul`, `.BK2_istemci_gunu_isletme_dilimine_sabit`
  - `Dogrulayici.BELGEKALANIstemciSinirTestleri.DGR_BK_istemci_gunu_liste_gunuyle_ayni` ×3
  - `Dogrulayici.BELGEKALANTur2SinirTestleri.DGR_BK2_isletme_gunu_sinir` ×2
  - `TM1MusterilerTuvalTestleri.TM1_13_Zaman_metni_bugun_dun_ve_tarih`
  - Yukarıdakiler `Bicim.cs` tarih değişikliği → **TOPLU-1**.
  - `FaturaEkranTestleri.KuyumTaslaginaToplamGonderilmez`, `FaturaFistenTestleri.Ayni_fisten_ikinci_fatura_400_ve_mesaj_fatura_numarasini_tasir`, `FaturaKalemTestleri.Ayni_fisten_ikinci_kalemli_fatura_400_doner`
  - Bu üçü `Faturalar.razor` taslak çekmecesi / `FaturaModulu` → **TOPLU-1** (Faturalar taslak) ya da **YASAL-4** (Faturalar özel matrah).
  - Tur 1'deki `Dogrulayici.YASAL4HttpSinirTestleri.SINIR_esZamanli_iki_tam_odeme_kesilenin_ustune_cikmamali` (**YASAL-4**) tur 2'de yeşildi.
  - **Bu paketin yeni kırmızısı: 0.** Tur 1'deki 5 paket kırmızısı (kayıt sayaçları) düzeltildi.
- `denetim.sh` P-30 DEMO_INSAN_SURESI (kabulde hariç).
- Gerçek e-posta kanalı (SMTP / sağlayıcı) yok: kurucu kararı, aşağıda.

## 11. Kurucuya / mali müşavire notlar

- **Mevzuat:** bu paket yasal kural, eşik ya da oran içermiyor. Mali müşavir sorusu yok.
- **Kurucu kararı (K-yeni):** Üretimde "Parolamı unuttum" bugün **kapalı** doğar ve ekran "dükkân sahibine ya da destek hattına başvurun" der. Açmak için:
  1. ADR-0016 kapsamında bir e-posta gönderim sağlayıcısı seçilmeli (sözleşme, KVKK aydınlatma, gönderen alan adı / SPF-DKIM).
  2. `IParolaSifirlamaPostasi` için bir SMTP/API uygulaması yazılmalı (tek dar arayüz hazır, iş kodu değişmez).
  3. Kurulumda `ParolaSifirlama:Kanal` ve `ParolaSifirlama:BaglantiTabani` (dükkânın https adresi) verilmeli.

  Bu karar verilene dek tek kullanıcılı dükkânda patron parolasını unutursa yol yine destek hattıdır. Bunu artık ekran açıkça yazıyor.

  **Sonuç (tur 2, doğrulayıcı ORTA bulgusu):** kurul 5 maddesi bu karar ve SMTP/API uygulaması gelmeden **"kapandı" sayılmamalı**. Doğru etiket: **"kod hazır, kanal kararı bekliyor."** Bu şerit SMTP yazmadı: ADR-0016 dış kanal tekeli kurucu kararı olmadan delinemez. Kod tarafında açmak için yalnız `IParolaSifirlamaPostasi`'nın bir uygulaması ve fabrikaya bir `case` gerekir. İş kodu, uçlar, ekran ve testler değişmez.

## 12. Değişen dosyalar (bu paket)

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından bu pakete ait olanlar. Ağaçta başka şeritlerin değişiklikleri de var.

**Yeni:**
- `dotnet/Kuyumcu.Api/Data/ParolaSifirlamaJetonu.cs`
- `dotnet/Kuyumcu.Api/Modules/Auth/ParolaSifirlamaModulu.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20261001065111_AuthSifirlaParolaJetonlari.cs` + `.Designer.cs` (+ `KuyumcuDbContextModelSnapshot.cs` güncellendi)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.AuthSifirla.cs`
- `dotnet/Kuyumcu.Web/Pages/SifreSifirla.razor`
- `dotnet/Kuyumcu.Tests/AuthSifirlaTestleri.cs`
- `dotnet/Kuyumcu.Tests/AuthSifirlaEkranTestleri.cs`

**Değişen:**
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` (DbSet + 2 indeks)
- `dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs` (`TumTokenlerIptalEt` private → internal)
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` (DI)
- `dotnet/Kuyumcu.Api/Common/OranSiniri.cs` (politika `parola-sifirlama`)
- `dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` (varlık damgası; istisna kütüğü DEĞİŞMEDİ)
- `dotnet/Kuyumcu.Web/Pages/Giris.razor` (yalnız bağlantı + yorum)
- `dotnet/Kuyumcu.Web/Layout/AnaYerlesim.razor`
- `dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs`
- `dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs`
- `dotnet/Kuyumcu.Core/Erisim/RotaModulleri.cs`
- `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs`
- `dotnet/Kuyumcu.Web/wwwroot/kilavuz.html` (üretildi)
- `docs/KULLANIM_KILAVUZU.md`
- `dotnet/denetim.sh`
- `dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs`
- `dotnet/Kuyumcu.Tests/P37cPanelVeKapsamTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json`
- `dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js`

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Giris.razor` (yalnız bağlantı), `Pages/SifreSifirla.razor` (yeni).

**Geçici dosyalar** yalnız `/private/tmp/kuyumcu-serit/AUTH-SIFIRLA/` ve `/private/tmp/kuyumcu-serit/mut-yedek/AUTH-SIFIRLA/` altında. Proje ağacına .bak/.tmp/.mutorj bırakılmadı.

**Olay:** `ApiIstemci.cs` düzenlenirken bir `perl` ifadesinin ayırıcı hatası 3 satırı dosyanın başına ekledi. Derleme bunu hemen yakaladı (7 hata). Eklenen satırlar aynı oturumda kaldırıldı ve doğrulandı: dosya `using System.Net;` ile başlıyor. Asıl değişiklik `Edit` aracıyla yapıldı.

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Var/yok aynı yanıt | KARŞILANDI | HTTP testi + canlı (gövde aynı, 604/604 ms) |
| (2) 32 bayt, SHA-256, 30 dk, tek kullanım, iptal | KARŞILANDI | testler + M1-1/2/3/7/10 + SQL |
| (3) 15 dk'da 3, 4. istekte 429 | KARŞILANDI | testler + M1-4 + canlı 429 |
| (4) Parola kuralı, refresh iptali, denetim (değer yok) | KARŞILANDI | testler + M1-5/9 + canlı SQL |
| (5) MFA atlanmaz | KARŞILANDI | HTTP testi + canlı (mfaGerekli=True) |
| (6) SMTP yokken fail-closed 503 + ekran | KARŞILANDI (tur 2: giriş bağlantısı da durumu soruyor) | test + M1-6/M1-11/M2-1 + canlı 503 + ekran görüntüsü `giris-yardim-kapali` |
| (7) Gerçek e-posta yok, dosya/test kanalı | KARŞILANDI | fabrika testleri, DOSYA canlı (0o600) |
| build 0 hata | KARŞILANDI | tur 2: b-son.log, 0 hata |
| build 0 uyarı | **PAKET DOSYALARINDA KARŞILANDI, AĞAÇTA DEĞİL** | tur 2: xUnit2000 ×3 giderildi. Kalan 3 uyarının hiçbiri bu paketin değil: CS8601 `Dogrulayici/SUBEKAPSAMASinirTestleri.cs`, xUnit2029 `KesifP2Testleri.cs`, MSB3026 eşzamanlı şerit dosya kilidi (geçici) |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | tur 2: denetim6.log, 303 ✓, tek ✗ P-30 |
| tam süit 0 yeni kırmızı | KARŞILANDI | tur 2: trx-pAUTH-SIFIRLA-3, 8740 test, 2 kırmızı. İkisi de KESIF-P6'nın (`Uyum.razor` 11:44'te değişti) |
| Tur 1 doğrulayıcı bulguları | KARŞILANDI / DEVREDİLDİ | § 14 |
| Başsız Chrome 1280+375 · BASİT+PRO · açık+koyu | KARŞILANDI | § 8 tablo + görüntüler |
| Test kiracısı silindi | KARŞILANDI | SQL PAKETAUTH% = 0 |

---

## 14. Tur 2 — doğrulayıcı ve tasarım hakemi bulgularının kapatılması (01.10)

### 14.1 Bulgu → yapılan

| # | Bulgu | Önem | Yapılan | Kanıt |
|---|---|---|---|---|
| B-1 | Build uyarısı: `AuthSifirlaTestleri.cs(507-509)` 3 × xUnit2000. Raporda "uyarı 0" diye yanlış beyan edilmişti. | ORTA | `Assert.Equal` argümanları yer değiştirdi: beklenen = `ParolaSifirlamaServisi.ParolaKalibi/ParolaEnAz/ParolaEnFazla`. § 7'deki yanlış beyan düzeltildi. | `b2.log` ve `b-son.log`: 0 hata. AuthSifirla dosyalarında uyarı 0. Kalan uyarılar başka şeritlerin (§ 13). |
| B-2 | Üretimde özellik hep KAPALI, kurul 5 engeli üretimde fiilen kapanmıyor | ORTA | **Kod değişmedi, beyan değişti.** SMTP/API kanalı kurucu kararı gerektirir (ADR-0016 dış kanal tekeli, KVKK, gönderen alan adı). Rapor başlığı ve § 11 artık "kod hazır, kanal kararı bekliyor" diyor. Hazırlık kuruluna böyle taşınmalı. | § 11, başlık |
| B-3 | 5199 (kurucunun inceleme sunucusu) dinlemiyor | DÜŞÜK | Bu şerit 5199'a dokunmadı (5188/5189 ve CDP 9984-9986 kullandı). Tur 2 başında ve sonunda `lsof -iTCP:5199` yine **boş**. Yeniden başlatma ve kapanma nedenini araştırma **orkestratöre** devredildi (görev sınırı: 5199'a dokunma). | `lsof` çıktısı boş |
| B-4 | Denetim kaydı eylemi `EKLEME` | DÜŞÜK | `DenetimServisi.YazmaKaydet`'e geriye uyumlu, isteğe bağlı `eylem` parametresi eklendi. Verilmezse `EKLEME` kalır, tüm mevcut çağıranlar aynı davranır. Fonksiyon yeniden yazılmadı. Sıfırlama `eylem: DenetimEylemi.Guncelleme` verir. Test `Basarili_sifirlama_DENETIM_kaydi_yazar_DEGER_yazmaz` artık `iz.Eylem == GUNCELLEME` diye sınıyor. | Canlı SQL: `GUNCELLEME · Kullanici · hassas=1` (2 satır: SAHİP + MÜDÜR). Mutasyon M2-5 kırmızı. |
| B-5a | `#jeton=` parçası adres çubuğunda/geçmişte kalıyor | DÜŞÜK | Jeton okununca `Nav.NavigateTo("/sifre-sifirla", replace: true)` çağrılır. Bu, hem ilk açılışta hem açık sekmede parça değişince olur. Jeton yalnız bileşen belleğinde kalır. | bUnit `Jeton_okununca_PARCA_adres_cubugundan_silinir_jeton_bellekte_kalir`, `Acik_sekmede_gelen_parca_da_adres_cubugundan_silinir`. Başsız Chrome: tüm `JETON-*` / `OTURUM-JETON` satırlarında `location.hash = ""`, form açık. M2-2 kırmızı. |
| B-5b | Ekranda `(HttpRequestException)` gibi istisna tür adı | DÜŞÜK | Üç catch bloğundan tür adı kaldırıldı. Ekranda yalnız sözlük metni görünür. Teknik ayrıntı `Console.Error` ile tarayıcı konsoluna yazılır (gizlenmez). | bUnit `Ekrana_TEKNIK_istisna_adi_yazilmaz`. M2-6 kırmızı. |
| B-6 | **KRİTİK (tasarım):** Kanal kapalıyken giriş ekranı "E-postayla yeni parola al" diyor ve çıkmaza yolluyor | KRİTİK | `Giris.razor` yardım bölmesi açılınca `GET /auth/parola-sifirlama/durum` çağrılır. Sonuca göre: **acik=true** ise bağlantı ve yönetici yolu görünür. **acik=false** ise bağlantı ÇİZİLMEZ, kutu tek cümle yazar ("Parolanı dükkân sahibin ya da müdürün Kullanıcılar ekranındaki “Şifre Sıfırla” ile yeniler; dükkân sahibiysen destek hattını ara."). **Okunamazsa** bağlantı çizilmez, "kapalı" da denmez, aynı her-koşulda-geçerli cümle yazılır, bir sonraki açılışta yeniden sorulur. Sorgu sürerken "Kontrol ediliyor…" görünür. | bUnit `Giris_kanal_ACIKKEN_…`, `Giris_kanal_KAPALIYKEN_eposta_baglantisi_CIZILMEZ_…`, `Giris_durum_OKUNAMAZSA_…`. Başsız Chrome: kanal açık → `bag:true`, kanal kapalı (API env'siz) → `bag:false` + tek cümle (4/4 varyant). M2-1 kırmızı. |
| B-7 | **KRİTİK (tasarım):** 429 bandı olmayan "Yeniden dene" düğmesini gösteriyor, "900 saniye" yazıyor, "yoğunluk" diyor, "basın" hitabı kullanıyor | KRİTİK | `SifreSifirla.razor` `OranSinirHatasi`'nı kendisi yakalar. Metin `Sozlukler.AuthSifirla.cs` `HizSiniriKalip` alanından gelir: "Çok fazla deneme yaptın. {0} dakika sonra yeniden dene." Süre `Retry-After` başlığından dakikaya çevrilir (`ceil(sn/60)`, en az 1). Tamamla adımı da aynı metni kullanır. | bUnit `Hiz_siniri_429_KENDI_metni_DAKIKA_yazar_olmayan_dugmeyi_isaret_etmez` (gerçekçi 429 + `Retry-After: 900`). Başsız Chrome 4. istek: bant "Çok fazla deneme yaptın. 15 dakika sonra yeniden dene." M2-3 kırmızı. |
| B-8 | **KRİTİK (tasarım):** Geçersiz/kullanılmış bağlantıda işe yaramayan "Parolayı kaydet" birincil kalıyor. Asıl çıkış soluk gri bağlantı. Bant "giriş ekranından iste" diyor. | KRİTİK | Jeton reddedilince form ve "Parolayı kaydet" GİZLENİR. Tek birincil düğme `#sifirla-yeni-baglanti` "Yeni bağlantı iste" olur ve istek adımına döner. Soluk bağlantı kaldırıldı. Bant aynı ekrandaki eylemi gösterir: "Bu bağlantı artık geçerli değil. Aşağıdan yeni bağlantı iste." Alan hatası (kural ihlali) bu yola girmez. | bUnit `Jeton_reddedilirse_HATA_bandi_ve_YENI_BAGLANTI_yolu` (tek `.btn-birincil` = `sifirla-yeni-baglanti`, form yok, "Giriş ekranından" yok). Başsız Chrome `JETON-RED`: `form:false`, `birincil:["sifirla-yeni-baglanti:Yeni bağlantı iste"]`. Tıklayınca `istekForm:true`. M2-4 kırmızı. |

Yeni metinler `Sozlukler.AuthSifirla.cs`'de (TR+EN, konumsal record, eksik anahtar = derleme hatası): `BaglantiGecersiz`, `HizSiniriKalip` (+ `HizSiniri(int dakika)` yöntemi), `GirisKapaliYardim`. Sözlük testi (jargon yok, boş yok) yeni alanları da tarıyor.

### 14.2 Tur 2 değişen dosyalar

- `dotnet/Kuyumcu.Web/Pages/SifreSifirla.razor`: B-5a, B-5b, B-7, B-8
- `dotnet/Kuyumcu.Web/Pages/Giris.razor`: B-6, yalnız "Parolamı unuttum" yardım bölmesi ve onun kodu
  - **TEK DOKUNUŞ gerekçesi:** aynı paketin tur 2'si. Tasarım hakeminin KRİTİK bulgusu yalnız bu bölmeyi gerektiriyor. Başka şeridin bölgesine dokunulmadı.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.AuthSifirla.cs`: 3 yeni metin
- `dotnet/Kuyumcu.Api/Modules/Auth/ParolaSifirlamaModulu.cs`: `eylem: DenetimEylemi.Guncelleme`
- `dotnet/Kuyumcu.Api/Data/DenetimIzi.cs`: `YazmaKaydet`'e isteğe bağlı `string? eylem = null` parametresi
  - Ortak dosya, geriye uyumlu. Varsayılan davranış birebir korundu.
- `dotnet/Kuyumcu.Tests/AuthSifirlaTestleri.cs`: B-1 ve B-4
- `dotnet/Kuyumcu.Tests/AuthSifirlaEkranTestleri.cs`: 6 yeni vaka, 1 vaka bölündü/güçlendi, sahte API'ye gerçekçi 429 eklendi
- `dotnet/Kuyumcu.Tests/S1eGirisTestleri.cs`: `Parolami_unuttum_DOGRU_YOLU_yazar_istek_atmaz_ve_kapanir`
  - Eski "hiç istek yok" iddiası artık yanlış (bölme durumu GET ile soruyor).
  - Test şimdi "yalnız GET durum, yazma isteği yok" diye sınıyor.
  - Bu S1-e'nin testi. Davranış değişikliği bu paketin emri olduğu için güncellendi.

### 14.3 Testler (tur 2)

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Filtreli `AuthSifirla \| S1eGiris \| TK4EkranlarGiris` (son kod, `t-son.log`) | 109 | 109 | 0 |
| **Tam süit, son kod** (`trx-pAUTH-SIFIRLA-3`, tur başına bir kez) | 8740 (8739 koşuldu) | 8737 | **2** |

- Tam süitteki 2 kırmızı:
  - `KesifP6EkranTestleri.Uyum_saklama_sekmesi_MUDUR_gorunmez(MUDUR)`
  - `KesifP6EkranTestleri.Uyum_saklama_sekmesi_MUDUR_gorunmez(MUHASEBE)`
  - Sahibi **KESIF-P6 (t2)**. `Uyum.razor` koşu sırasında 11:44'te değişti. Bu paket o dosyaya dokunmadı.
  - **Bu paketin yeni kırmızısı: 0.** Tur 1'deki 11 yabancı kırmızı (TOPLU-1/YASAL-4) bu koşuda yeşil.
- Yeni test sayısı: tur 1'de 61, tur 2'de +6 → **67**. AuthSifirlaEkranTestleri 21 Fact + Theory'ler.

### 14.4 Mutasyon kanıtları (tur 2)

Yöntem § 6 ile aynı. Yedekler `/private/tmp/kuyumcu-serit/mut-yedek/AUTH-SIFIRLA-t2/` altında. Her değişiklik tek eşleşmelidir (`eslesme=1`). Her mutasyon için test projesi derlendi (0 hata), `--filter` ile koşuldu, yedek geri kopyalandı ve `cmp` AYNI çıktı. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` 0 hata verdi.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M2-1 | Giriş: `else if (sifirlamaAcik == true)` → `else if (true)` (bağlantı her zaman) | `Giris_kanal_KAPALIYKEN_eposta_baglantisi_CIZILMEZ_tek_dogru_yol_yazilir`, `Giris_durum_OKUNAMAZSA_baglanti_CIZILMEZ_kapali_da_denmez_teknik_ad_yok` | AYNI |
| M2-2 | Parça temizliği kaldırıldı (`ParcayiTemizle();` → yorum) | `Jeton_okununca_PARCA_adres_cubugundan_silinir_jeton_bellekte_kalir` | AYNI |
| M2-3 | 429 özel yakalama kaldırıldı | `Hiz_siniri_429_KENDI_metni_DAKIKA_yazar_olmayan_dugmeyi_isaret_etmez` | AYNI |
| M2-4 | Red görünümü kaldırıldı (`@if (jetonReddedildi)` → `@if (false)`, form kalır) | `Jeton_reddedilirse_HATA_bandi_ve_YENI_BAGLANTI_yolu` | AYNI |
| M2-5 | Denetim eylemi geri alındı (`eylem: DenetimEylemi.Guncelleme` silindi) | `Basarili_sifirlama_DENETIM_kaydi_yazar_DEGER_yazmaz` | AYNI |
| M2-6 | Ekrana istisna tür adı geri yazıldı | `Ekrana_TEKNIK_istisna_adi_yazilmaz` | AYNI |
| M1-1 (yeniden) | **hash kaldır:** `return (duz, duz)` | `DB_de_jeton_DUZ_METIN_yok_yalniz_SHA256_ve_30_dk` + 9 akış testi (10 kırmızı) | AYNI |
| M1-2 (yeniden) | **tek kullanım kaldır** | `Jeton_TEK_KULLANIMLIK_ikinci_kullanim_400` | AYNI |
| M1-3 (yeniden) | **süre kaldır** | `Suresi_dolan_jeton_400_parola_degismez` | AYNI |
| M1-4 (yeniden) | **hız sınırı kaldır** (`kira.IsAcquired \|\| true`) | `Hiz_siniri_ayni_eposta_ayni_IP_DORDUNCU_istekte_429`, `Hiz_siniri_KAYITSIZ_epostada_da_ayni_429_varlik_sizmaz` | AYNI |

**Not (Anayasa md.1):** M2-2'nin ilk koşusu güvenilmezdi.
- Kırmızı listesinde M2-2'nin değil, M2-1'in testleri çıktı.
- Nedeni: eşzamanlı şeritlerin derlemesi aynı `bin/obj` dosyalarına yazıyordu. Bunun kanıtı aynı koşunun son derlemesindeki MSB3026 "dosya başka süreç tarafından kullanılıyor" uyarısı.
- Tekrar ilk denemede de derleme 24 hata verdi (WebcilConverter dosya kilidi, eşzamanlı şerit). Kaynak hatası değildi.
- Üçüncü koşu temizdi: derleme 0 hata, beklenen test kırmızı, `cmp` AYNI. Tabloda bu sonuç yer alıyor.

### 14.5 Canlı doğrulama (tur 2, API 5188 Development, kanal DOSYA → sonra kanalsız)

`canli3.py` (§ 8'deki betiğin aynısı, iki farkla: denetim SQL'i `eylem='GUNCELLEME'` diye süzer, MFA kullanıcısı MÜDÜR). Sonuç `canli-sonuc3.json`: **22/22 OK, KALDI 0**. Öne çıkanlar:

| Adım | Beklenen | Gerçek |
|---|---|---|
| Kiracı | 201/201 | tenant 01M3V9X91ZDGB16EPC1FKEHXA7 |
| Var/yok e-posta | 200/200, gövde aynı, ≥600 ms | 200/200, gövde_aynı=True, 621/607 ms |
| SQL düz metin / SHA-256 / ömür | 0 / 1 / 1800 | 0 / 1 / 1800 |
| Eski jeton, ikinci tamamla | 400 / 400 | 400 / 400 |
| Eski refresh | 401 | 401 · `refresh_tokenler` açık 0 / 2 |
| **SQL denetim** | eylem=GUNCELLEME, değer sızıntısı 0 | `GUNCELLEME · Kullanici · hassas=1` × 2 (SAHİP + MÜDÜR), sızıntı 0 |
| Hız sınırı | 200×3 → 429 + Retry-After | [200,200,200] → 429, Retry-After=900 |
| MFA | tamamla jetonsuz → girişte mfaGerekli | 200 jetonsuz → mfaGerekli=True, access yok |
| Kanal KAPALI (API env'siz yeniden başlatıldı) | durum acik=false · istek 503 | `{"acik":false,…}` · HTTP 503 "…dükkân sahibine ya da destek hattına başvurun." |

### 14.6 Gerçek tarayıcı ölçümü (tur 2, başsız Chrome, CDP 9984-9986, vekil 5189)

Betik `ekran3.mjs` (`sikisma-olc.js __olcTam`), ham kayıtlar `olcum3-{anonim,oturum,kapali}.jsonl` ve `ekran3-*.log`, görüntüler `/private/tmp/kuyumcu-serit/AUTH-SIFIRLA/ekran3/`.

| Rota / durum | Mod | 1280 açık | 1280 koyu | 375 açık | 375 koyu | Birincil | Kart / ana alan |
|---|---|---|---|---|---|---|---|
| /giris yardım, **kanal açık** (bağlantı VAR) | anonim | 0 | 0 | 0 | 0 | 1 | 440/610 (%72) · 343/375 |
| /giris yardım, **kanal kapalı** (bağlantı YOK, tek cümle) | anonim | 0 | 0 | 0 | 0 | 1 | aynı |
| /sifre-sifirla **geçersiz bağlantı** (form yok, "Yeni bağlantı iste") | anonim | 0 | 0 | 0 | 0 | 1 | aynı |
| /sifre-sifirla **hız sınırı** ("15 dakika") | anonim | 0 | 0 | 0 | 0 | 1 | aynı |
| /sifre-sifirla#jeton (parça silinir, form açık) | **BASİT** | 0 | 0 | 0 | 0 | 1 | aynı |
| /sifre-sifirla#jeton (parça silinir, form açık) | **PRO** | 0 | 0 | 0 | 0 | 1 | aynı |

Hücrelerdeki "0", şu sayaçların hepsinin 0 olduğunu gösterir: K1 gizli-görünen, K3 taşan/kırık, K5 TRY, K7, K8 jargon, K9 binen, K10 elips, K11, K12 ve yatay kaydırma.

Görülen (Read) görüntüler:
- `sifirla-jeton-red__ANONIM_karanlik_375.png`: kırmızı bant ve altında tek mavi "Yeni bağlantı iste", form yok.
- `sifirla-hiz-siniri__ANONIM_aydinlik_1280.png`: bant "Çok fazla deneme yaptın. 15 dakika sonra yeniden dene.", tek birincil.
- `giris-yardim-kapali__ANONIM_aydinlik_375.png`: e-posta bağlantısı yok, tek cümle.
- `giris-yardim-acik__ANONIM_karanlik_1280.png`: "E-postayla yeni parola al" bağlantısı ve yönetici yolu.

Not: ölçümden sonra yalnız `#sifirla-yeni-baglanti` düğmesinin öznitelik SIRASI değişti (`@onclick` aynı satıra alındı). `denetim.sh`'ın satır tabanlı ölü-düğme sayacı 72'ye çıkmıştı, şimdi 71/71. DOM ve görünüm aynı, filtreli 109/109 yeşil.

### 14.7 Kapanış (tur 2)

- API 5188 (iki koşu) ve vekil 5189 yalnız kendi PID'leriyle durduruldu. Chrome süreçleri betik içinden kapandı. `lsof` 5188, 5189 ve 9984-9986 **boş**.
- Test kiracısı `01M3V9X91ZDGB16EPC1FKEHXA7`, `zsh kiraci-temizle.sh <id> --uygula` ile silindi (çıkış 0).
- Silme sonrası SQL:
  - `tenants WHERE id=… OR ad LIKE 'PAKETAUTH%'` = 0
  - kiracının `parola_sifirlama_jetonlari` satırları = 0
  - `kullanicilar LIKE 'paketauth-sifirla%'` = 0
- **tenants: başlangıç 14 → son 14.**
- `QA-TEST-*` kiracılarına dokunulmadı.

### 14.8 Denetim (tur 2, `denetim6.log`)

303 ✓, tek ✗ **P-30 DEMO_INSAN_SURESI** (kabulde hariç). Bu paketin sayaçları:
- ExecuteUpdate 5
- IBildirimKanali 1
- IKiraciVarlik 76
- ölü düğme 71/71
- ModalPencere 48
- ROTA_KAPSAM 49
- P-06: 2/0/3
- P-09 ve P-14 hepsi ✓
- P-33 KILAVUZ ✓

Ara koşuda (`denetim5.log`) görülen `P-08 FATURA_NO_SERBEST_METIN 3 > 2` son koşuda ✓. Bu paketle ilgisizdi: fatura dosyalarına dokunulmadı, başka şeridin ara durumuydu.
