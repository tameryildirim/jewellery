# PAKET P-31 — T-4b MASKELİ DESTEK PAKETİ + /destek + HATA KİMLİĞİ (R-5)

**Tarih:** 2026-09-23 · **Dalga:** 7c · **Sürüm:** 1.0 · **Roller:** Taşkın (backend) + Ertuğ (frontend)
**Durum:** BİTTİ (paket kapısı 7/7). Derleme 0 uyarı / 0 hata. P-31 testleri 62/62 yeşil. `denetim.sh` TEMİZ. Canlıda 13 adım doğrulandı. 13 mutasyonun hepsi kırmızı verdi. Test kiracısı silindi (tenants 10 → 10).
Tam süit 4361 testte **3 kırmızı** verdi. Üçü de başka şeridin testi (S-1 hattı), § 9'da listelendi.

---

## § 0. Önkoşul ölçümü
| Önkoşul | Diskte mi? | Kanıt |
|---|---|---|
| P-02 (ölçüm kapısı, taze sayaç) | VAR | `denetim.sh` `# >>> P-02` bloğu (satır 1389–1880) |
| P-28 (telemetri + destek dakikası) | VAR | `Modules/Telemetri/`, `DestekKaynaklari.Tanilama` sabiti. P-28 raporunun "açık kalanlar" 3. maddesi bağlantıyı açıkça P-31'e bırakıyor. |
| T-4a (istek kimliği, künye) | VAR | `Common/IstekKimligi.cs`, `Modules/Meta/SurumModulu.cs`, HataBandi rozeti |

Önkoşulların hepsi diskte olduğu için kapsam daraltılmadı.

## § 1. Kimlik kapanış tablosu
| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-5** — T-4b destek paketi (1.0) | **KAPANDI**, BK-17 koşuluyla | Kod: `Modules/Destek/DestekModulu.cs` (allow-list 77–109, denetçi 115–139, fail-closed 214, controller 384). Kısa kod: `Core/Sozlesmeler/DestekSozlesmeleri.cs`. Günlük: `Common/ApiException.cs:98,161`. Ekran: `Pages/Ayarlar.razor` `#ayar-destek`. Testler: `P31DestekPaketiTestleri`, `P31TanilamaTestleri`, `P31DestekEkranTestleri` (62 test). Canlı: C1–C3, O1–O8 (§ 7). Sayaçlar: `denetim.sh` `# >>> P-31`. |
| R-5 / BK-17 — "Mehmet'in kendi turunda da doğrulanır" | **DEVREDİLDİ: Mehmet (06 Güvenlik) turu** | Bu koşul başka bir model ailesinin bağımsız turudur. Uygulayıcı kendi işini doğrulayamaz. Mekanizma bu yüzden fail-closed kuruldu: `Destek:PaketKanaliAcik` varsayılanı **false**. Kanal kapalıyken paket ucu 503 döner (canlı C1), ekrandaki düğme kilitlenir ve sebebini yazar. Depo yapılandırması kanalı açarsa `TANILAMA_PAKETINDE_SIR` sayacı kırılır. |

## § 2. Yapılanlar
1. **Kısa hata kodu.** Kod `HataKodu.Kisa` ile üretilir: sunucunun ULID'inin son 8 karakteri, yani rastgele kısmı. Kiracı ya da kullanıcı bilgisi taşımaz.
   - Sunucu günlüğü artık `hata kodu: XXXXXXXX` alanını da yazar. Bu hem 500 dalı hem de 5xx `ApiException` dalı için geçerli.
   - 5xx `ApiException` dalı önceden hiç günlüğe yazmıyordu. Bu açık kapatıldı.
   - 503 bilinçli kapalı durumdur, hata olarak günlüğe yazılmaz.
   - Eşleşme yalnız sunucu tarafında yapılır.
2. **Ekran hatası kodu.** Önceden tarayıcıda doğan hatanın (deserialize hatası, NRE) kodu yoktu. Artık `KabukHatasi` `POST /api/v1/destek/istemci-hatasi` ucuna **yalnız hata türü adını** gönderir. Kodu sunucu üretir ve günlüğe yazar. Tür adı `[A-Za-z0-9_.]{1,80}` desenine süzülür; uymazsa "Bilinmeyen" yazılır.
3. **İç kod sızıntısı kapandı.** `KabukHatasi` artık ham `istisna.Message` basmıyor. Örneğin "The JSON value could not be converted to System.Int32…" gibi bir metin kullanıcıya gösterilmez; yerine Türkçe bir açıklama çıkar.
4. **HataBandi.** Ekranda 8 karakterlik kısa kod okunur. Tam kimlik `data-deger` özniteliğinde durur. Kopyala düğmesi kısa kodu kopyalar.
5. **Maskeli tanılama paketi** (`POST /api/v1/destek/paket`, yalnız SAHIP):
   - Paket allow-list ile üretilen bir JSON dosyasıdır. İçeriği: sürüm künyesi, bekleyen migration'lar, bool kapılar, son 24 saatin istek ve 5xx sayısı, son 200 denetim satırı, konu, isteğe bağlı hata kodu.
   - Denetim satırlarında yalnız tarih, eylem, varlık türü ve kullanıcının kısaltılmış kodu yer alır.
   - Kiracı kimliği kısaltılarak yazılır.
   - Kullanıcının yazdığı gerekçe **pakete girmez**; yalnız işletmenin kendi denetim izine yazılır.
6. **Fail-closed denetim.** `TanilamaDenetcisi` paket metnini tarar: TCKN, telefon, e-posta, IBAN, kart numarası, JWT, bağlantı dizesi, özel anahtar, Bearer ve sır adlı alanlar. Bulgu varsa paket **verilmez** (500). Ret izi (`RedKaydet`) denetime düşer; iz bulgunun yalnız türünü taşır, değerini taşımaz.
7. **P-28 bağlantısı.** Her paket `destek_dakikalari` tablosuna `kaynak=TANILAMA`, **pasif, 0 dk** bir satır açar. Rakam uydurulmaz. Operatör gerçek dakikayı P-28 sistem ucuyla girer (canlı O8).
8. **Diğer kurallar:**
   - Saatte en çok 3 paket (429).
   - Geçmiş ucu son 50 paketi döner ve sınırı yanıtta bildirir.
   - Paket kaydı değişmezdir; PATCH/DELETE ucu 404 döner.
9. **Ekran** (`Pages/Ayarlar.razor`, `#ayar-destek`, yalnız SAHIP):
   - "Neler var / neler yok" listeleri gösterilir.
   - Konu seçici sunucunun beyaz listesinden gelir; adlar sözlükten çevrilir (UX-S31).
   - Gerekçe alanı (`GerekceAlani`) en az 20 karakter ister ve sayaç gösterir.
   - Hata kodu alanının altında Türkçe mesaj çıkar.
   - Kilitli düğme sebebini yazar (UX-S30).
   - Oluşturmadan önce satır içi onay istenir (UX-S27; yeni ModalPencere açılmadı, tavan 55 korundu). Sonuç başarı bandına dosya adıyla yazılır.
   - Geçmiş tablosu, boş durum ve sınır metni (UX-S29) var. Hata bandı listeyi boşaltmaz (UX-S28). `DegismezIpucu` gösterilir (UX-S25).
10. **Sözlük.** `Core/I18n/DestekSozlugu.cs` TR ve EN metinleri taşır. Ortak `Sozlukler.cs` dosyasına dokunulmadı; `PozisyonSozlugu` deseni izlendi.
11. **denetim.sh.** `# >>> P-31` bloğu eklendi (§ 6).

## § 3. Sapmalar ve gerekçeleri
| # | Sapma | Gerekçe |
|---|---|---|
| S1 | **`Pages/Destek.razor` açılmadı.** "/destek" bir API kanalıdır (`/api/v1/destek/*`); ekran Ayarlar'da bir karttır. | Plan bloğunun "Dosyalar" listesinde yalnız `Pages/Ayarlar.razor` var ve kapsam kilidi yeni ekran açmayı yasaklıyor. **Plan üstündür.** T-4 tasarım belgesi ayrı bir `/destek` sayfası öngörüyordu; bu çelişki burada kayda geçti. |
| S2 | **`destek_paketleri` tablosu ve jeton/indir uçları yok.** Migration yapılmadı. | Plan "Migration: HAYIR" diyor. Paket kaydı `denetim_kayitlari` içinde duruyor (`varlik_turu=DestekPaketi`, `varlik_id=paket kodu`). Dosya POST yanıtında doğrudan dönüyor. Geçmiş denetim izinden okunuyor. Tek kullanımlık jetona gerek kalmadı çünkü dosya sunucuda saklanmıyor. T-4 tasarımındaki tablo, jeton ve 15 dakikalık süre bu yüzden uygulanmadı. |
| S3 | Paket biçimi zip değil, JSON. | Tek dosya olduğu için sıkıştırmanın faydası yok. JSON metni fail-closed denetçiyle doğrudan taranabiliyor. |
| S4 | Plan dosya listesi dışında dokunulan dosyalar: `Modules/Destek/DestekModulu.cs` (YENİ), `Modules/ModulKayit.cs` (1 DI satırı), `Core/Sozlesmeler/DestekSozlesmeleri.cs` (YENİ), `Core/I18n/DestekSozlugu.cs` (YENİ), `denetim.sh` (P-31 bloğu), Tests. | Uçlar servis olmadan yazılamaz. Pages'e yeni record açmak yasak olduğu için ortak tipler Core'a kondu. |
| S5 | `DuzeltmePolitikasi.cs`'e girdi eklenmedi. | Yeni bir varlık açılmadı. Paket kaydı bir `DenetimKaydi` satırıdır ve o satır zaten `SALT_OKUNUR · DENETIM` damgalı (`DuzeltmePolitikasi.cs:204`). |
| S6 | Mevcut test uyarlandı: `S3aEkranTestleri.Sayfa_cokunce_BOS_EKRAN_degil_GORUNUR_bant_cizilir`. | Test ham istisna mesajının ekranda görünmesini bekliyordu. Bu davranış planın `IC_KOD_SIZINTISI=0` sayacıyla çelişiyor ve `KabukHatasi.razor` plan dosyasıdır. Test artık "ham mesaj YOK, Türkçe açıklama VAR" diye sınıyor. |
| S7 | Canlı doğrulamada sistem anahtarı ve kanal ortam değişkeniyle verildi (`Sistem__Anahtari`, `Destek__PaketKanaliAcik=true`). | Yalnız bu oturum içindi; depo yapılandırması değiştirilmedi. Anahtar dosyası iş sonunda `printf ""` ile boşaltıldı. |
| S8 | Tahmin 60 ajan-dk idi, gerçekleşen yaklaşık 150 dk. | Üç neden: başka şeritlerin eşzamanlı derlemeleri `obj/` kilidine takıldı ve 5 mutasyon yeniden koşuldu; başlangıç tam süiti başka şeridin yarım migration'ı yüzünden 1710 kırmızı verdi; kayıt davet gerektirdiği için canlı kiracı davetle açıldı. |

## § 4. Migration
**Gerekmedi** (plan: HAYIR). `__EFMigrationsHistory` = 70, değişmedi.

## § 5. Testler
| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Önce: tam süit (20:39, başka şeridin yarım `sistem_carisi` migration'ı ile) | 4237 | 2527 | 1710 (1708'i `Unknown column 'sistem_carisi'`) |
| P-31 filtreli (`~P31`) | 62 | 62 | 0 |
| İlgili gruplar (P31, S3a, SurumEkran, IstekKimligi, HataKimligiDallari, P28, DuzeltmePolitikasi, OlcumKapisi) | 183 | 183 | 0 |
| **Sonra: tam süit** (trx: `scratchpad/trx-pP-31/_Mac_2026-09-23_21_23_01.trx`) | **4361** | **4358** | **3** (§ 9, başka şerit) |

**Yeni test: 62** (theory satırları dahil). Test sınıfları:
- `P31DestekPaketiTestleri` (HTTP + MySQL): kanal kapısı 503, geçmiş zarfı, 400 hataları (gerekçe, konu, serbest metin kod), 403 (MUDUR, TEZGAHTAR), içerikte kişisel veri olmaması, denetim + TANILAMA pasif 0 dk satırı, fail-closed 500 + ret izi, 429, kiracı izolasyonu, UTC tarih, PATCH/DELETE yok, istemci hata kodu, 401, tür süzgeci.
- `P31TanilamaTestleri` (saf): kısa kod, Crockford doğrulaması, denetçinin 11 deseni, gerçekçi temiz paket, allow-list yansıması, günlükte kısa kod, 5xx ApiException, 503/404/400 günlüğe yazılmaz, sözlük kapsamı.
- `P31DestekEkranTestleri` (bUnit): kanal kapalıyken kilit ve sebep, 10/20 sayacı, hata kodu alan mesajı, onay → gerçek POST → bant (gövde JSON ayrıştırılarak sınandı), BosDurum, konu çevirisi, UX-S28, TEZGAHTAR rolünde kart yok ve istek yok, HataBandi kısa kodu, KabukHatasi ham mesaj yok ve kod sunucudan.

## § 5b. Mutasyon tablosu (13/13 kırmızı)
Yöntem: yedek `scratchpad/mut-yedek/P-31/` → `perl -0777 -pi` → filtreli test → geri kopyala → `cmp` birebir.

⚠ İlk otomatik koşuda M31-3, M31-4, M31-5, M31-11 ve M31-12 sonuçsuz ya da yanlış yeşil çıktı. Sebebi, başka şeritlerin eşzamanlı derlemesinin `Kuyumcu.Web/obj` dosyalarını kilitlemesiydi (günlükte `MSB4018 … process cannot access the file`). Bu mutasyonlar açık derleme adımı ve yeniden denemeyle tekrar koşuldu. M31-3 ve M31-5 bir kez daha elle koşuldu; ikisinde de derleme 0 hata verdi ve test kırmızıya döndü.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M31-1 | Kanal varsayılanı `false` → `true` | `P31DestekPaketiTestleri.Kanal_yapilandirmayla_acilmadikca_paket_503_ve_iz_yok` | cmp birebir |
| M31-2 | Denetçi atlandı (`bulgu = new List`) | `…Denetimden_gecmeyen_paket_VERILMEZ_500_ve_ret_izi_duser` | cmp birebir |
| M31-3 | `KiraciKisa` yerine tam `TenantId` | `…Paket_uretilir_kisisel_veri_ve_gerekce_icermez` (elle yeniden koşum) | cmp birebir |
| M31-4 | Gerekçe alt sınırı 20 → 1 | `…Gerekce_20_karakterden_kisa_ise_400` | cmp birebir |
| M31-5 | Paket ucuna MUDUR rolü eklendi | `…SAHIP_disi_rol_403(rol: "MUDUR")` (elle yeniden koşum) | cmp birebir |
| M31-6 | TANILAMA satırı `Aktif = true` | `…Paket_denetim_izine_ve_P28_destek_dakikasina_TANILAMA_pasif_0dk_yazilir` | cmp birebir |
| M31-7 | 500 günlüğünden "hata kodu:" çıkarıldı | `P31TanilamaTestleri.Beklenmeyen_hata_gunlugunde_tam_kimlik_ve_KISA_KOD_var` | cmp birebir |
| M31-8 | HataBandi ekranda tam kimliği gösterdi | `P31DestekEkranTestleri.HataBandi_ekranda_KISA_kodu_gosterir_tam_kimlik_makine_degeri` | cmp birebir |
| M31-9 | KabukHatasi ham mesajı bastı | `P31DestekEkranTestleri.Ekran_hatasi_HAM_mesaji_basmaz_…` + `S3aKabukHatasiTestleri.Sayfa_cokunce_…` | cmp birebir |
| M31-10 | `HataKodu.GecerliMi` her şeyi kabul etti | `P31TanilamaTestleri.Kullanici_kodu_yalniz_Crockford_8_ya_da_26` (5 satır) + `P31DestekPaketiTestleri.Serbest_metin_hata_kodu_reddedilir_400` | cmp birebir |
| M31-11 | Saatlik tavan 3 → 99 | `…Saatlik_tavan_asilinca_429` | cmp birebir |
| M31-12 | Ekran kanal kapısını yok saydı | `P31DestekEkranTestleri.Kanal_kapaliyken_Olustur_kilitli_ve_sebebi_yazili` | cmp birebir |
| M31-13 | Paket bölgesine `d.Detay` okuması eklendi | `denetim.sh`: `✗ P-31: TANILAMA_PAKETINDE_SIR 1 (okuma=1)` | cmp birebir |

Son mutasyondan sonra temiz derleme: **0 uyarı, 0 hata**.

**Kendi hatam (Madde 1):** M31-13'ten sonra, yedeği alınmamış `KabukHatasi.razor` dosyasına yanlışlıkla bir perl değişikliği uyguladım. Eklenen satır, yani `private string Ham(…)` yardımcısı, perl ile tek eşleşmeyle geri silindi. Dosya M31-9 yedeğiyle `cmp` ile birebir aynı çıktı. Bu değişiklik hiç derlenmedi.

## § 6. denetim.sh
Son koşum `DENETİM TEMİZ.` (exit 0). P-31 satırları:
```
✓ P-31: TANILAMA_PAKETINDE_SIR   0 (-eq 0: allow-list + denetçi + kanal kapalı)
✓ P-31: IC_KOD_SIZINTISI         0 (-eq 0: 3 dosya tarandı)
```
Yan sayaçlar yükselmedi: ModalPencere 55/55 · ölü düğme 71/71 · kültürsüz ToString 27/27 · P-09 OZNITELIKSIZ_UC 0 (349 uç) · P-11 DUSUREN 47/47.

Bu sayaçlar ilk koşumda bu paket yüzünden kırılmıştı ve düzeltildi:
- ölü düğme 74: `@onclick` satırı `<button>` ile aynı satıra alındı;
- kültürsüz ToString 28: InvariantCulture eklendi;
- P-09: `istemci-hatasi` ucuna dört rol açıkça yazıldı;
- P-11 48: catch içindeki `= null` ataması kaldırıldı.

Ölçüm tabanı: `IC_KOD_SIZINTISI` P-31'den önce 1'di (KabukHatasi `@istisna.Message`), sonra 0 oldu. Ölçüm komutları blok yorumunda yazılı.

## § 7. Canlı doğrulama (5198)
Kiracı `PAKETP31-TEST Kuyumculuk` (`01M37QRVXZ89DH9BMQZZFP7TG7`) sistem davetiyle kayıt yoluyla açıldı.

| Adım | Beklenen | Gerçek |
|---|---|---|
| C1 kanal kapalı, POST /destek/paket | 503 + Türkçe mesaj + istekKimligi | **503** "…güvenlik doğrulaması tamamlanana dek kapalıdır…" |
| C2 kanal kapalı, GET /destek/paket/gecmis | 200, kanalAcik=false, sinir=50 | **200** `{"kanalAcik":false,"sinir":50,…,"kayitlar":[]}` |
| C3 POST /destek/istemci-hatasi (gövdede `mesaj` ile telefon da gönderildi) | 200, hataKodu = başlık kimliğinin son 8 karakteri, kod günlükte, kişisel veri günlükte yok | **200** `hataKodu 1GSRA5AM`, başlık `…01GSRA5AM`. Günlük satırı: `İstemci ekran hatası · hata kodu: 1GSRA5AM · tür: JsonException`, kapsamda tam kimlik var. "Ayse" ve "05321234567" günlükte **0** kez geçiyor. |
| O1 kanal açık, gerekçe 4 karakter | 400 | **400** "Gerekçe en az 20 karakter olmalı (4/20)." |
| O2 hataKodu serbest metin | 400 | **400** |
| O3 geçerli paket (gerekçede ad ve telefon var) | 200 JSON + dosya adı + kişisel veri yok | **200**, 2967 bayt, `destek-paketi-42NX7P4E.json`, 13 anahtar. "Ayse", "05321234567", tam tenant kimliği, işletme adı, soyad ve e-posta pakette **yok**. |
| O4 geçmiş | Paket kısa kod ve konuyla listelenir | **200** `{"paketKodu":"42NX7P4E","konu":"HATA"}` |
| O5/O6 saatlik tavan | 2. ve 3. paket 200, 4. paket 429 | **200, 200, 429** "Bir saatte en çok 3…" |
| O7 PATCH / DELETE paket | 404/405 | **404 / 404** |
| SQL: denetim_kayitlari | EKLEME 3 | `EKLEME 3 · OKUMA 2` |
| SQL: destek_dakikalari | 3 satır, TANILAMA, aktif=0, dakika=0 | 3 satır `TANILAMA 0 0 YAZILI HATA/DIGER/DIGER` |
| O8 operatör PATCH `{dakika:15, aktif:true}` (P-28 ucu) | 200; aktif toplam 15 | **200**; `TANILAMA 1 15`, `sum(aktif)=15` |
| Günlük: paket kodu | Var | "Tanılama paketi üretildi · paket kodu: 42NX7P4E · boyut: 2967" |

Canlıda bulunup düzeltilen hata: geçmiş ucu tarihi `Z`'siz (Kind=Unspecified) döndürüyordu. `DateTime.SpecifyKind(Utc)` eklendi ve teste `EndsWith("Z")` kilidi kondu.

Rol kısıtları (MUDUR/TEZGAHTAR 403) canlıda ayrı kullanıcı açılarak **denenmedi**. HTTP testleri ve M31-5 mutasyonu bunu kapsıyor.

**Temizlik:**
- API durduruldu (PID 7441 → 12310 → 14766); `lsof -nP -iTCP:5198 -sTCP:LISTEN` boş.
- `tenant_id` kolonu olan 70 tablo, `kiraci_davetleri` ve `tenants` temizlendi; kalan satır 0.
- `tenants` sayısı **10 → 10**. `QA-TEST-*` kiracıları: 4, dokunulmadı.

## § 8. DoD sözleşmesi
| Kayıt | Oluşturma | Düzenleme | Pasife alma | Gerekçe |
|---|---|---|---|---|
| Destek paketi (denetim satırı) | POST /destek/paket (SAHIP) | YOK | YOK | Denetim izinin parçasıdır, değişmez. Doğru yol yeni paket oluşturmaktır. `DenetimKaydi` SALT_OKUNUR·DENETIM damgalı. Ekranda DegismezIpucu gösterilir. |
| Destek dakikası (TANILAMA) | Paketle otomatik doğar | P-28 PATCH (operatör) | P-28 DELETE → pasif, PATCH aktif=true | P-28 sözleşmesi aynen geçerli. Canlı O8'de doğrulandı. |
| İstemci hata bildirimi | POST /destek/istemci-hatasi | — | — | Kayıt oluşmaz; yalnız günlük satırı yazılır. |

## § 9. Açık kalanlar
1. **BK-17 / Mehmet turu.** Kanal varsayılan olarak kapalı. Mehmet `TANILAMA_PAKETINDE_SIR=0` ve `IC_KOD_SIZINTISI=0` sayaçlarını kendi turunda doğrularsa kanal üretim yapılandırmasında `Destek__PaketKanaliAcik=true` ile açılır. Açma işi orkestratör/kurucu kararıdır; açılırsa denetim.sh'taki (c) kuralı güncellenmeli.
2. **Tam süitteki başka şeride ait 3 kırmızı** (bu paketin dosyalarına dokunmuyor):
   - `S1eGirisTestleri.Tuval_IKI_PANEL_sol_kart_ve_sag_panel` ve `S1eGirisTestleri.Sozluk_IKI_DILDE_dolu_ve_farkli`: `Pages/Giris.razor` ve `GirisSozlugu.cs` üzerinde çalışan **S-1 şeridi (S1-e)**.
   - `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir`: `Layout/UstBar.razor` ve CSS üzerinde çalışan **S-1 şeridi (S1-c)**.
   - Bu paketin yeni kırmızı katkısı **0**.
3. Canlıda rol 403 adımı denenmedi (§ 7).
4. Operatör ekranı yok; TANILAMA dakikaları operatör ucundan girilir. Ekran P-28'de "operatör ekranı (F-2/Faz B hattı)" olarak devredilmişti.

## § 10. Kurucuya notlar
- ⚠ **SIR ÇIKTIYA DÜŞTÜ (Madde 1, gizlenmiyor).** Test kiracısını silerken zsh'ta bir kabuk değişkenini tek komut sanıp çalıştırdım. Değişken bölünmedi ve "command not found" hata satırı **MySQL root parolasını araç çıktısına bastı**. Parola hiçbir dosyaya, rapora ya da depoya yazılmadı; yalnız bu oturumun araç çıktısında göründü. **Öneri:** `kuyumcu-mysql` root parolası döndürülsün. Doğru kalıp bir fonksiyon (`q(){ docker exec … -p"$P" … }`) ya da `${=VAR}` kullanmaktır; ortam tuzakları notuna eklenmeli.
- Mevzuat hükmü yok: bu paket VUK, KDV, MASAK ya da e-belge kuralına dokunmuyor. Birincil kaynak araştırması gerekmedi.

## § 11. Dokunulan sayfalar (TEK DOKUNUŞ)
`Pages/Ayarlar.razor` · `Bilesenler/HataBandi.razor` · `Bilesenler/KabukHatasi.razor`

## § 12. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin payı)
- M `dotnet/Kuyumcu.Api/Common/ApiException.cs`
- M `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- M `dotnet/Kuyumcu.Web/Bilesenler/HataBandi.razor`
- M `dotnet/Kuyumcu.Web/Pages/Ayarlar.razor`
- ?? `dotnet/Kuyumcu.Web/Bilesenler/KabukHatasi.razor` (depoda izlenmiyordu; değiştirildi)
- ?? `dotnet/denetim.sh` (izlenmiyor; P-31 bloğu eklendi)
- YENİ `dotnet/Kuyumcu.Api/Modules/Destek/DestekModulu.cs`
- YENİ `dotnet/Kuyumcu.Core/Sozlesmeler/DestekSozlesmeleri.cs`
- YENİ `dotnet/Kuyumcu.Core/I18n/DestekSozlugu.cs`
- YENİ `dotnet/Kuyumcu.Tests/P31DestekPaketiTestleri.cs`, `P31TanilamaTestleri.cs`, `P31DestekEkranTestleri.cs`
- M `dotnet/Kuyumcu.Tests/S3aEkranTestleri.cs` (S6)

## § 13. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Her beklenmeyen hata kısa kod gösterir | ✓ | HataBandi kısa kod; KabukHatasi ekran hatası için kodu sunucudan alır |
| Aynı kod sunucu günlüğünde bulunur | ✓ | `ApiException.cs:98,161`; canlı C3 günlük satırı |
| Kod sır/kişisel veri taşımaz | ✓ | ULID'in rastgele kuyruğu; C3'te kişisel veri günlükte 0 |
| Tanılama paketinde sır/kişisel veri olmadığı denetimle kanıtlanır (fail-closed) | ✓ | Denetçi + 500 + ret izi (test ve M31-2), sayaç ve M31-13, canlı O3 |
| Destek dakikası bu kanaldan P-28'e yazılır | ✓ | TANILAMA pasif 0 dk satırı, operatör PATCH (O8) |
| Mehmet'in turunda doğrulanır | DEVREDİLDİ | Kanal varsayılan kapalı (BK-17) |
| Sayaçlar TANILAMA_PAKETINDE_SIR=0 · IC_KOD_SIZINTISI=0 | ✓ | denetim.sh P-31 bloğu |
| Migration HAYIR | ✓ | 70 → 70 |
| Pages içinde yeni record yok | ✓ | Tipler Core/Sozlesmeler'de |
| Tek birincil düğme · yeni ModalPencere yok | ✓ | İkincil düğmeler + satır içi onay; tavan 55 |
