# PAKET S-3c — Form ve etkileşim sağlamlığı (uygulama raporu)

> Dalga 3-H (hata düzeltme · S-3a/b/c). Roller: Taşkın (backend) + Ertuğ (frontend).
> Tarih: 2026-09-18. Kapsam kaynağı: `HAZIRLIK-KURULU-3.md` § engel 20 + QA turu 3
> KRİTİK/YÜKSEK bulguları.
> **Yeni ekran / yeni modül / yeni özellik AÇILMADI** (kurucu talimatı 13 · kapsam kilidi).
> **Commit yok** — çalışma ağacında bırakıldı.

---

## 1. Özet

| Ölçüm | Değer |
|---|---|
| Derleme | `dotnet build Kuyumcu.sln --nologo` → **0 hata · 0 uyarı** |
| Denetim | `dotnet/denetim.sh` → **DENETİM TEMİZ** |
| Tam süit | **2507/2507 yeşil** (önce 2485) · 0 atlanan · 0 başarısız |
| Yeni test | **22** (7 HTTP + 2 servis + 13 bUnit) · 2 mevcut test sözleşmeye uyarlandı |
| Mutasyon | 11 koşum · **10 kanıt** · **1 TEST AÇIĞI bulundu → ölü mekanizma kaldırıldı** |
| Migration | **gerekmedi** (şema değişmedi; kapı doğrulama katmanındadır) |
| Canlı | 5198'de gerçek MySQL ile 7 HTTP adımı + 3 SQL sayımı · test kiracısı silindi (tenants **8 → 8**) |

### Kapanan kalemler

| # | Kalem | Durum |
|---|---|---|
| 20-a | `/fiyatlandirma` — eksi marj (−5) ve %500 işçilik uyarısız kabul ediliyor (YÜKSEK) | **KAPANDI** — sert sınır + rahat aralık + bilinçli onay, sunucu **ve** ekran |
| 20-b | Satır içi düzenlemede odak kaybı değişikliği sessizce yutuyor (`/sayim` YÜKSEK) | **KAPANDI** — `SatirIciDuzenle`de blur=kaydet, çift gönderim korumasıyla |
| 20-c | `ModalPencere` "her açılışta durum sıfırlanır" sözleşmesi | **KAPANDI (sözleşme + test)** — `Acildi` geri çağrısı + kapalıyken hiç çizilmeme ayağı teste bağlandı |
| QA-3 `/sayim` | "Enter kaydetmiyor" | **DOĞRULANDI: kod zaten doğruydu** — sayfa düzeyinde regresyon testine bağlandı (aşağıda) |
| QA-3 `/urunler` stok modalı ondalık `adet` (KRİTİK) | **ZATEN KAPALI bulundu** — `StokVeri.Adet` **decimal** (`Urunler.razor:900`) |
| QA-3 `/urunler` renderer ölümü (KRİTİK) | **ZATEN KAPALI bulundu** — `KabukHatasi` hata sınırı `AnaYerlesim.razor:14,37` |

### Devredilen kalemler (gerekçeli)

| Kalem | Karar |
|---|---|
| 5 rotada tablo kartlaştırma (375 px, 29/29 kapı) | **devredildi: UX-mobil dilimi (S-3d)** — bu paketin borcu form/etkileşim davranışıdır; kartlaştırma DOM/CSS yeniden düzenlemesidir, bUnit ile ölçülemez (CSS uygulanmaz) ve kanıtı yalnız canlı 375 px turudur. Aynı dalgada iki farklı kanıt rejimi karıştırılmadı. |
| T-4a hata kimliği / sürüm damgası | **devredildi: T-4a** — kendi paket kodu var; hata kimliği ayağı S-3a'da (`KabukHatasi` destek kodu) zaten kapandı, kalan **sürüm damgası** T-4a'nın telemetri/sürüm dilimine aittir. |

---

## 2. Yapılanlar

### 2.1 Engel 20-a — fiyat listesinde makul aralık kapısı

**Koda karşı doğrulama:** spesifikasyonun iddiası **doğruydu**. Sunucuda yalnız
`[Range(-100,1000)]` / `[Range(0,1000)]` vardı; bu nitelikler **yalnız denetleyici
yolunda** çalışır ve −5 marj, 500 çarpan hiçbir uyarı üretmeden 200 dönüyordu
(`FiyatModulu.cs` eski 24-26. satırlar). İstemcide ise açık bir yorum vardı:
*"NEGATİF MARJ KURALI KONMADI"*.

**Karar (ekip oyu, kurucuya sorulmadı):** negatif marj **yasaklanmaz** — maliyetine /
indirimli liste meşru bir iş kararıdır. Ama **sessiz olamaz**. İki katmanlı kapı:

| Katman | Marj | İşçilik çarpanı | Davranış |
|---|---|---|---|
| **Sert sınır** | < −100 veya > 500 | < 0 veya > 500 | **Her hâlükârda 400** (onay işe yaramaz) |
| **Rahat aralık** | 0 – 100 | 0 – 200 | Serbest |
| **Risk bandı** | < 0 veya > 100 | > 200 | `riskOnayi=true` YOKSA **400** |

- Eşikler **tek kaynakta**: `Kuyumcu.Core/Sozlesmeler/FiyatAraliklari.cs` (yeni dosya —
  yeni özellik değil, iki taraf arasında **paylaşılan sözleşme**; "yeni istemci record
  yasağı"nın işaret ettiği doğru yer de burasıdır). Sunucu ve ekran aynı sabitleri okur,
  bu yüzden kullanıcı ekranda uyarı almadan sunucudan 400 yemez (UX-S30 + UX-S31).
- Kapı **serviste** durur (`FiyatServisi.AraligiDogrula`) — `ListeOlustur` ve
  `ListeGuncelle` çağırır. DataAnnotations aralıkları da daraltıldı (−100…500 / 0…500),
  ama kapı onlara bağlı değildir.
- **PATCH tuzağı çözüldü:** var olan riskli listenin değeri **değişmiyorsa** yeniden onay
  istenmez. Aksi hâlde eksi marjlı bir liste bir daha pasife bile alınamazdı
  (`Fiyatlandirma.razor` → `ListeDurumDegistir` mevcut değerleri aynen geri gönderir).
- Ekran: `#fl-risk-uyari` (Türkçe gerekçe) + `#fl-risk-onay` onay kutusu + `#fl-risk-not`
  (kilidin **sebebi**); onay verilmeden `#fl-kaydet` **disabled**; sert sınır dışında
  alan altı mesaj (`#alan-hata-fl-marj`). Onay her form açılışında **sıfırlanır**.
- Sözlük TR+EN: `MarjEksiUyari`, `MarjYuksekUyari`, `CarpanYuksekUyari`,
  `RiskOnayiEtiket`, `RiskOnayiSart`, `MarjAralikDisi`, `CarpanAralikDisi`.
  Ham kod (`MARJ_EKSI` vb.) ekrana **basılmaz**, sözlükten çevrilir (UX-S31).

### 2.2 Engel 20-b — satır içi düzenlemede odak kaybı

**Koda karşı doğrulama:** QA turu 3 *"Enter kaydetmiyor"* dedi; **kod doğruydu** —
`SatirIciDuzenle.Tus` Enter→Kaydet, Escape→İptal bağlıyor ve `KabukBilesenTestleri`
bunu zaten sınıyordu. Gerçekten **eksik olan blur ayağıydı**: kullanıcı değeri yazıp
başka yere tıkladığında değişiklik sessizce kayboluyordu. Bulgunun Enter ayağı için
sayfa düzeyinde (`/sayim`, gerçek POST'a kadar) regresyon testi eklendi — QA'nın
ölçtüğü şeyin aynısı, artık kapıda.

**Düzeltme:** `@onblur="Odaktan"`:
1. değer değişmişse **kaydeder**,
2. değişmemişse sessizce kapanır,
3. geçersizse (boş) **açık kalır ve alan altı mesaj** verir (sessiz etkisizlik yasak).

**Çift gönderim tuzağı:** Kaydet/İptal düğmesine tıklarken de blur olur. Blazor'da
`FocusEventArgs` `relatedTarget` taşımadığı için düğmelerin `@onmousedown` olayı
(blur'dan **önce** gelir) bir bayrak kaldırır ve blur çekilir. Testle kanıtlandı
(S3c-8, S3c-9). Bileşen 5 rotada ortaktır: `/cariler`, `/islemler`, `/sayim`,
`/veri-ice-aktarim` — düzeltme hepsine tek elden gitti (ikinci kalıp açılmadı).

### 2.3 Engel 20-c — ModalPencere "her açılışta sıfırlanır"

`Acik` kapalıdan açığa geçtiğinde `Acildi` geri çağrısı tetiklenir: ebeveynin **kendi**
form alanlarını (seçili kasa, yön, tutar) sıfırladığı tek meşru yer burasıdır.
Çocuk bileşenlerin durumu zaten sıfırlanır, çünkü modal kapalıyken **hiç çizilmez**.

**TEST AÇIĞI ve dürüst sonucu:** ilk uygulamada gövdeye `@key="acilisSayaci"` konmuştu.
Mutasyon M6 bu anahtarı kaldırdı ve **hiçbir test kırmızıya dönmedi** — çünkü `@if (Acik)`
zaten aynı işi yapıyor. Ölü mekanizma **kaldırıldı**; yerine sözleşmenin gerçek ayağı
(`@if`) M6b mutasyonuyla kapıya bağlandı (`@if (true)` → S3c-13 kırmızı).

### 2.4 Kapsam dışı ama ölçümde çıkan iki kalem (Madde 1)

1. **4 derleme uyarısı** (`MadenTestleri.cs`, xUnit2008) önceki kapı kayıtlarında
   "0 uyarı" denmesine rağmen **vardı** (dosya 17 Eylül tarihli, bu paketten önce).
   Anlamı değişmeyen `Assert.DoesNotMatch` / `Assert.Matches` çevirisiyle kapatıldı;
   kapı yeniden **0 uyarı**.
2. **Kararsız test:** `UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_...` tam süitte
   kırmızı, tek başına (35/35) ve iki sınıflı koşumda (48/48) yeşildi. Sebep ürün
   davranışı değil, **testin yarışı**: "Ekle" işleyicisi asenkron, iddia beklemesizdi.
   `WaitForAssertion` eklendi; sonraki tam süit **2507/2507 yeşil**.

---

## 3. DoD sözleşmesi

Bu paket **yeni varlık / yeni kayıt türü doğurmaz** — mevcut `FiyatListesi` varlığının
**doğrulama katmanına** dokunur. Bu yüzden yeni PATCH/DELETE/yetki/denetim ucu gerekmez;
var olanlar korunur:

| Varlık | Düzenleme yolu | Yetki | Denetim | Not |
|---|---|---|---|---|
| `FiyatListesi` | `PATCH /fiyat/listeler/{id}` (ekranda `#fl-duzenle-{id}`) | `[ModulYetkisi urun]` + SAHIP/MUDUR (mevcut) | mevcut | S-3c yalnız **kabul edilen değer aralığını** daralttı |
| `FiyatListesi` pasife alma | `PATCH … aktif=false` (satırda düğme) | aynı | mevcut | Varsayılan liste pasife alınamaz (mevcut kural, korundu) |
| `riskOnayi` alanı | **saklanmaz** — yalnız istek anının onayıdır | — | — | Kalıcı alan olmadığı için migration gerekmedi |

Salt okunur / değişmez bir kalem eklenmedi; `DegismezIpucu` gerektiren yeni durum doğmadı.

**Her düğme gerçek istek atar ya da sebebini söyler:** `#fl-kaydet` riskli değerde
**disabled** ve yanında `#fl-risk-not` sebebi yazılıdır (DoD EK md.1 + UX-S30).

**Tek düzenleme kalıbı:** yeni `duzenlenenId` kalıbı açılmadı, yeni `ModalPencere`
düzenleme kullanımı **0** (denetim.sh TEMİZ).

**Tek dokunuş — bu dalgada dokunulan sayfalar:** `Pages/Fiyatlandirma.razor` (S-3c'de
ilk kez). `Bilesenler/SatirIciDuzenle.razor` ve `Bilesenler/ModalPencere.razor`
sayfa değil ortak bileşendir; `Pages/Sayim.razor` **DEĞİŞMEDİ** (yalnız testi eklendi).

---

## 4. Testler (önce / sonra)

| Kapsam | Önce | Sonra |
|---|---|---|
| Tam süit | 2485 | **2507** (+22) |
| Başarısız | 0 | **0** |

Yeni dosyalar:
- `Kuyumcu.Tests/HttpFiyatAralikTestleri.cs` — 7 HTTP testi (400/200 + DB kanıtı)
- `Kuyumcu.Tests/S3cFiyatAralikServisTestleri.cs` — 2 servis testi (DataAnnotations'sız yol)
- `Kuyumcu.Tests/S3cFormSaglamligiTestleri.cs` — 13 bUnit testi (fiyat formu · satır içi · sayım · modal)

Uyarlanan mevcut testler:
- `FiyatlandirmaEkranTestleri.Yeni_liste_POST_govdesi_marj_ve_carpan_tasir` — −1.5 marj artık
  onay ister; test yeni sözleşmeyi (kilit + onay + `riskOnayi=true`) doğruluyor.
- `UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_...` — yarış düzeltmesi (§ 2.4).

---

## 5. Mutasyon tablosu

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| S3c-M1 | `ListeOlustur`dan `AraligiDogrula(dto, null)` silindi | `Servis_SERT_sinir_disi_marji_reddeder`, `Servis_riskli_carpani_ONAYSIZ_reddeder_ONAYLA_kabul_eder`, `EKSI_marj_onaysiz_400_doner_ve_liste_YAZILMAZ`, `YUKSEK_iscilik_carpani_onaysiz_400_doner` | `cmp` birebir |
| S3c-M2 | `ListeGuncelle`den `AraligiDogrula(dto, liste)` silindi | `PATCH_riskli_degere_gecis_onaysiz_400_doner_ve_eski_deger_KORUNUR` | `cmp` birebir |
| S3c-M3 | `FiyatAraliklari.RiskKodu` eksi marj eşiği −9999'a çekildi | `Fiyat_EKSI_MARJ_uyari_cikarir_KAYDET_kilitlenir_ve_ISTEK_ATILMAZ`, `Fiyat_RISK_ONAYI_isaretlenince_PATCH_gider…`, `Fiyat_SERT_SINIR_disi_deger_ONAYLA_BILE_gonderilmez`, `EKSI_marj_onaysiz_400_doner_ve_liste_YAZILMAZ` | `cmp` birebir |
| S3c-M4 | `SatirIciDuzenle`den `@onblur="Odaktan"` kaldırıldı | `SatirIci_ODAK_KAYBINDA_kaydeder`, `SatirIci_DEGISMEYEN_degerde_odak_kaybi_istek_ATMAZ`, `SatirIci_GECERSIZ_degerde_odak_kaybi_SESSIZ_kalmaz`, `SatirIci_KAYDET_dugmesinde_blur_ile_CIFT_gonderim_olmaz`, `SatirIci_IPTAL_dugmesinde_blur_KAYDETMEZ`, `Sayim_satir_ici_ODAK_KAYBINDA_POST_atilir` | `cmp` birebir |
| S3c-M5 | Çift gönderim koruması (`dugmeyeBasildi`) kaldırıldı | `SatirIci_KAYDET_dugmesinde_blur_ile_CIFT_gonderim_olmaz`, `SatirIci_IPTAL_dugmesinde_blur_KAYDETMEZ` | `cmp` birebir |
| S3c-M6 | `ModalPencere` gövdesinden `@key="acilisSayaci"` kaldırıldı | **HİÇBİRİ — TEST AÇIĞI.** Mekanizmanın ölü olduğu anlaşıldı, kod **kaldırıldı** (§ 2.3) | `cmp` birebir |
| S3c-M6b | `@if (Acik)` → `@if (true)` (modal kapalıyken de çizilsin) | `Modal_her_acilista_GOVDE_yeniden_kurulur_ve_Acildi_cagrilir` | `cmp` birebir |
| S3c-M7 | `Acildi.InvokeAsync()` silindi | `Modal_her_acilista_GOVDE_yeniden_kurulur_ve_Acildi_cagrilir` | `cmp` birebir |
| S3c-M8 | `RiskOnayiBekliyor => false` | `Fiyat_EKSI_MARJ_uyari_cikarir…`, `Fiyat_YUKSEK_ISCILIK_CARPANI_uyari_cikarir` | `cmp` birebir |
| S3c-M9 | "değişmeyen riskli değer yeniden onay istemez" erken dönüşü silindi | `DEGISMEYEN_riskli_deger_yeniden_onay_ISTEMEZ` | `cmp` birebir |
| S3c-M10 | İstemci gövdesinden `riskOnayi` alanı çıkarıldı | `Fiyat_RISK_ONAYI_isaretlenince_PATCH_gider_ve_govdede_riskOnayi_true_olur` | `cmp` birebir |

> Dürüst not (M4): `SatirIci_IPTAL_dugmesinde_blur_KAYDETMEZ` bu mutasyonda **bUnit'in
> "olay işleyicisi yok" istisnası** yüzünden kırmızı olur, mantık ihlali yüzünden değil.
> Aynı test M5'te **doğru sebeple** (çift gönderim) kırmızıya döner.

Son mutasyondan sonra: `dotnet build` → **0 hata · 0 uyarı**.

---

## 6. Denetim çıktısı

```
./denetim.sh → ... ✓ T-3: appsettings'te yedek anahtarı  0

DENETİM TEMİZ.
```

---

## 7. Canlı doğrulama (localhost:5198, gerçek MySQL)

Kiracı: `PAKETS-3c-TEST` (sistem anahtarı + tek kullanımlık davet). Yanıt gövdeleri
**okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /fiyat/listeler` marj −5, onaysız | 400 | **400** "Has marjı eksi: bu liste satışı maliyetin altına indirir. Devam etmek için riskOnayi alanını işaretleyin" |
| 2 | `POST /fiyat/listeler` çarpan 500, onaysız | 400 | **400** "İşçilik çarpanı %200'ün üzerinde…" |
| 3 | `POST /fiyat/listeler` marj −600 + `riskOnayi=true` | 400 (sert sınır) | **400** "Has marj yuzde -100 ile 500 arasında olmalıdır" |
| 4 | `POST /fiyat/listeler` marj −5 + `riskOnayi=true` | 200 | **200** `hasMarjYuzde: -5` |
| 5 | `PATCH` aynı riskli değer + yeni ad, onaysız | 200 | **200** `ad: "S3c İndirimli (yeni ad)" · hasMarjYuzde: -5` |
| 6 | `PATCH` marj 250'ye geçiş, onaysız | 400 | **400** "Has marjı %100'ün üzerinde…" |
| 7 | `PATCH` marj 12 / çarpan 120 (makul), onaysız | 200 | **200** |
| 8 | SQL: kiracının `fiyat_listeleri` satır sayısı | **1** (reddedilenler yazılmamış) | **1** |
| 9 | SQL: kalan satırın değerleri | 12.000 / 120.00 | **`S3c Perakende · 12.000 · 120.00`** |
| 10 | SQL: `tenants` (temizlikten sonra) | 8 | **8** |

**Temizlik:** API PID'i durduruldu — `lsof -nP -iTCP:5198 -sTCP:LISTEN` **0 satır**.
`information_schema`'dan `tenant_id` kolonu olan tüm tablolardan test kiracısı,
ardından `kiraci_davetleri` ve `tenants` satırı silindi; `SELECT COUNT(*) FROM tenants`
→ **8** (başlangıç değeri). `QA-TEST%` kiracıları **3 adet, dokunulmadı**.
Sistem anahtarı / davet jetonu / erişim jetonu dosyaları `printf '' >` ile boşaltıldı.
**5199 portuna dokunulmadı.**

---

## 8. Değişen dosyalar

> `git status --porcelain` bu depoda ayırt edici değil (tek commit var, ağacın tamamı "M").
> Bu paketin dokunduğu dosyalar:

**Ürün kodu**
- `dotnet/Kuyumcu.Core/Sozlesmeler/FiyatAraliklari.cs` *(YENİ — paylaşılan eşik sözleşmesi)*
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` *(7 yeni anahtar TR+EN; `DuzenlemekIcinTikla` metni blur'u da söylüyor)*
- `dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs` *(DTO `RiskOnayi` + `AraligiDogrula` kapısı)*
- `dotnet/Kuyumcu.Web/Pages/Fiyatlandirma.razor` *(uyarı + onay kutusu + kilitli Kaydet)*
- `dotnet/Kuyumcu.Web/Bilesenler/SatirIciDuzenle.razor` *(blur=kaydet + çift gönderim koruması)*
- `dotnet/Kuyumcu.Web/Bilesenler/ModalPencere.razor` *(`Acildi` sözleşmesi)*

**Test**
- `dotnet/Kuyumcu.Tests/HttpFiyatAralikTestleri.cs` *(YENİ)*
- `dotnet/Kuyumcu.Tests/S3cFiyatAralikServisTestleri.cs` *(YENİ)*
- `dotnet/Kuyumcu.Tests/S3cFormSaglamligiTestleri.cs` *(YENİ)*
- `dotnet/Kuyumcu.Tests/FiyatlandirmaEkranTestleri.cs` *(yeni sözleşmeye uyarlandı)*
- `dotnet/Kuyumcu.Tests/UX1C1EkranTestleri.cs` *(yarış düzeltmesi)*
- `dotnet/Kuyumcu.Tests/MadenTestleri.cs` *(4 analiz uyarısı kapatıldı)*

---

## 9. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Hatayı yeniden üreten test | ✅ | 22 yeni test · § 4 |
| Mutasyon kanıtı (geri al → kırmızı → iptal) | ✅ | § 5, 11 koşum · `cmp` birebir |
| 5198'de canlı doğrulama (HTTP + SQL) | ✅ | § 7, 10 adım |
| `dotnet/denetim.sh` TEMİZ | ✅ | § 6 |
| Tam süit 0 başarısız | ✅ | 2507/2507 |
| Test kiracısı silindi, `QA-TEST-*` korundu | ✅ | tenants 8 → 8 · QA-TEST 3 duruyor |
| Kapsam kilidi (yeni ekran/modül/özellik yok) | ✅ | yalnız mevcut ekran + ortak bileşen |
| Tip sözleşmesi kuralı (ad + tip doğrulaması) | ✅ | `StokVeri.Adet` **decimal** (`Urunler.razor:900`); `Liste` record'u `decimal HasMarjYuzde/IscilikCarpanYuzde` — sunucu `decimal` (`Entities`/`FiyatModulu`) |
| PATCH semantiği korundu | ✅ | Varsayılanlık/aktiflik koruma satırları değişmedi; M9 mutasyonu kapıyı kanıtladı |
| Yeni istemci record açılmadı | ✅ | Paylaşılan eşikler `Kuyumcu.Core`'a kondu |
| Yeni CSS'te sabit renk / satır içi style yok | ✅ | mevcut `.uyari-metni` / `.metin-kucuk` sınıfları kullanıldı, yeni CSS yazılmadı |
| Sözlük TR+EN, çıplak metin yok | ✅ | 7 anahtar iki dilde |
| COMMIT YOK | ✅ | çalışma ağacı |

---

## 10. Açık kalanlar

1. **5 rotada 375 px tablo kartlaştırma** — devredildi (S-3d / UX-mobil dilimi). Kanıt
   rejimi canlı tarayıcı turudur.
2. **T-4a sürüm damgası** — devredildi (T-4a paketi).
3. **Risk eşikleri iş kararıdır, mevzuat değil** — 0–100 marj / 0–200 çarpan bandı
   ekip oyuyla seçildi; **kurucuya/mali müşavire not:** bu bant kâr politikasıdır,
   daraltılması/genişletilmesi isteniyorsa tek dosyada (`FiyatAraliklari.cs`) değişir.
   Hiçbir yasal hüküm koda sabit yazılmadı.
4. **`riskOnayi` denetim izine yazılmıyor** — "kim bilinçli olarak zararına liste açtı"
   sorusu bugün yalnız listenin değerinden okunuyor. Denetim kaydına onay bayrağının
   eklenmesi ayrı bir dilimdir (devredildi: 34-A denetim zenginleştirme).
5. **Ağaçta eski mutasyon artıkları var** (`*.mutorj` 9 dosya, `*.bak` 1 dosya · 15 Eylül
   tarihli, bu pakete ait DEĞİL). `rm` hook tarafından engellendiği için silinmedi —
   **kurucunun kararına bırakıldı**.
6. **QA turu 3'ün iki KRİTİK'i ölçüldüğünde zaten kapalıydı** (stok modalı decimal +
   ErrorBoundary). QA turu 4, bu iki maddeyi **yayın anlık görüntüsü güncellendikten
   sonra** yeniden ölçmeli; aksi hâlde aynı bulgu üçüncü kez raporlanır.

---

## EK — 2026-09-18 · DURUM DÜZELTMESİ ve ikinci dilim

> Bu bölüm hasım doğrulayıcının bulguları üzerine eklendi (Anayasa md.1).

* **Bu paketin durumu 'BITTI' DEĞİL, 'KISMEN'dir.** Spesifikasyonun iki kalemi
  (5 rotada 375 px tablo kartlaştırma, T-4a hata kimliği/sürüm damgası) uygulanmadı;
  kapsam kilidi gereği **devredildi: S-3d** ve **devredildi: T-4a**.
* **Kurul engeli 20 AÇIK kalır** ve kurul tur 4'e kapanmış olarak gitmez.
* Birinci dilimde aralık/risk kapısı **yalnız** `ListeOlustur`/`ListeGuncelle`
  yollarına konmuştu; ürüne özel POST/PATCH ve toplu uygulama uçları kapısızdı.
  Bu boşluk **2. dilimde** kapatıldı — kanıtlar (testler, mutasyonlar, canlı
  doğrulama, SQL): `PAKET-S-3c-2-uygulama-raporu.md`.
* Rapordaki "5 rotada ortak bileşen" ifadesi yanlıştı: ölçüm **42 rota tarandı,
  `SatirIciDuzenle` 4 sayfada kullanılıyor** (2. dilim raporu § 9).
* Ağaç artığı sayısı düzeltmesi: 16 `.mutorj` + 1 `.bak` + 8 adet 0 baytlık
  migration `.cs` (hiçbiri S-3c'ye ait değil; kurucu temizlik listesinde).
