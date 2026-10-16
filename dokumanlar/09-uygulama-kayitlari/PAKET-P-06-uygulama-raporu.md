# PAKET P-06 — DÜZENLENEBİLİRLİK ŞABLONU (SatirIciDuzenle GENİŞLETİLİR)

**Dalga:** DALGA 2 · **Sürüm:** 1.0 · **Tarih:** 2026-09-18
**Roller:** Taşkın (backend) + Ertuğ (frontend) · **Kimlikler:** B-4 · B-13 · B-18 · B-34 · B-54 · Y-15
**P-00 triyaj temaları (ek):** B-59 · B-60 · B-63 · B-65 · B-66 · B-67 · B-68 · B-70
**Önkoşul:** P-03 — **DİSKTE VAR** (`dotnet/Kuyumcu.Contracts/` mevcut; `PAKET-P-03-uygulama-raporu.md` + `...-DUZELTME-2026-09-18.md`). Kapsam daraltılmadı.
**Migration:** HAYIR (şema değişikliği yok — `__EFMigrationsHistory` 52 → 52).

---

## § 0 — Bir cümlede

"Her veri girişi ya düzeltilebilir ya da neden düzeltilemediği yazar" kuralı **ekran ekran değil şablon düzeyinde** kapatıldı: yeni `DuzenlenebilirListe` bileşeni kuralsız listeyi **çizim anında reddediyor**, `SatirIciDuzenle` açılışta odak + iş kuralı aralığı + `aria-describedby` ile genişletildi, gerekçeli istisnalar tek ve **cırcırlı** bir kütükte toplandı, ve altı sayaç `dotnet/denetim.sh`'a `# >>> P-06` bloğu olarak bağlandı.

---

## § 1 — Yapılanlar

### 1.1 `Bilesenler/DuzenlenebilirListe.razor` (YENİ, jenerik)
Planın istediği şablon. Üç şeyi **mekanik olarak** zorunlu kılar:

| Kural | Uygulama |
|---|---|
| UX-S25 — her kayıt ya düzeltilebilir ya gerekçelidir | `SatirEylemi` **ya da** `Gerekce` verilmezse `OnParametersSet` **InvalidOperationException atar**; liste hiç çizilmez |
| UX-S28 — hata "kayıt yok"a çevrilmez | `Hata` doluyken boş durum **bastırılır**, `role="alert"` bandı çizilir |
| UX-S29 — sessiz kırpma yok | `Sinir` verilip listeye değildiyse `#dl-{varlik}-sinir` uyarısı |

Gerekçe verildiğinde `DegismezIpucu` çizilir ve **hiç `<button>` üretilmez** (devre dışı düğme = "bir yolu var ama sana kapalı" yasağı).
`@typeparam TKayit` — **yeni istemci record açılmadı** (PLAN § 1 md.9).

### 1.2 `Bilesenler/SatirIciDuzenle.razor` (MEVCUT — genişletildi, davranış korunarak)
- **B-13 · açılışta odak:** `@ref="girdiRef"` + `OnAfterRenderAsync` → `FocusAsync()`. `AcilistaOdaklan` varsayılanı **true**. JS ortamı yoksa sessizce geçilir (odak kolaylıktır, veri yolu değil).
- **B-18 · iş kuralı sınırı alan altında:** `AsgariDeger` / `AzamiDeger` parametreleri + `AralikHatasi()`. Aralık dışı **ve** sayıya çevrilemeyen giriş alan altı Türkçe mesaj üretir, istek atmaz. Ayrıştırma kullanıcı kültürü → InvariantCulture yedekli ("12,5" yazan kullanıcı cezalandırılmaz).
- **B-18 · erişilebilirlik (B-63):** girdi hatalıyken `aria-invalid="true"` ve `aria-describedby` ile hata `<span>`'ine **bağlanır**.
- **B-34 · korunan sözleşme:** mevcut `@oninput` bağlaması ve `@onmousedown` blur bastırma bayrağı **değiştirilmedi**; üstlerine gerileme testi eklendi (hasım bulgusu R10).

### 1.3 `Modules/Meta/DuzeltmePolitikasi.cs` — `EkranIstisnalari` kütüğü (B-54)
Yazma yapan ama mühürlü kalıbı olmayan sayfaların **tek ve sayılabilir** yeri. Her satır üç şey söyler: gerekçe · kullanıcının düzeltmeyi yaptığı **gerçek yer** · borç varsa devredilen paket.

| Sayfa | Gerekçe (özet) | Düzeltme nerede | Devir |
|---|---|---|---|
| `Giris.razor` | Kimlik doğrulama yüzeyi; kayıt oluşmaz ya da başka ekranda düzenlenir | İşletme kimliği `/isletme` (PATCH `/admin/tenant/kimlik`) | — (kalıcı) |
| `Panel.razor` | Salt okunur pano; tek yazması AYAR tercihi (kur kaynağı) | Aynı seçiciden geri alınır; geçmiş `/kur` | — (kalıcı) |
| `Fiyatlandirma.razor` | Gerçek düzenleme VAR (`#fl-duzenle-{id}` → PATCH) ama kendi `duzenlenenId` formunu kullanıyor; P-06'dan ÖNCE vardı, tek dokunuş kuralı gereği bu pakette yeniden yazılmaz | Liste/kampanya satırındaki Düzenle düğmesi | **P-17** |

### 1.4 Ekran düzeltmeleri (gerçek kusur kapanışı)
- **`Pages/VeriDisaAktarim.razor`** — dışa aktarım talebinin **kanıt kaydı** olduğu (KVKK md.11) ve doğru yolun "yeni talep aç" olduğu hiçbir yerde yazmıyordu: ne düğme ne gerekçe vardı. `DegismezIpucu` + sözlükten gerekçe + `TalepDuzeltmeYolu` eklendi. *(B-4 · B-54 · B-68 · UX-S25)*
- **`Pages/Siparisler.razor`** — sipariş düzenlenebilir (`D("Siparis")`) ama liste ekranında yol yalnız "Detay" yazan bir bağdı. Bağ artık **makineye de beyan ediliyor**: `data-duzenleme-yolu="SiparisDetay.razor"` + `title`/`aria-label` ("Detay ve düzenleme — kalemler, söz verilen gün ve açıklama detay ekranından değiştirilir"). Test hedefin **gerçekten var olduğunu ve kendisinde mühürlü kalıp taşıdığını** doğrular; aksi hâlde beyan borç saklamanın yeni yolu olurdu. *(B-4 · B-70 · UX-S25)*

### 1.5 Sözlük (TR + EN, çıplak metin yok)
`SozlukDuzeltme`: `ListeBos` · `ListeSiniri` · `AralikDisi` · `SozlukKabuk.SayiGecersiz` · `SozlukDisaAktarim.TalepDuzeltmeYolu` · `SozlukSiparis.DetayVeDuzenleme` — hepsi iki dilde.

---

## § 2 — Sapmalar ve gerekçeleri (Anayasa md.1)

| # | Plan ne diyor | Gerçek | Ne yapıldı |
|---|---|---|---|
| S1 | `DUZENLENEMEYEN_GIRIS` **tabanı P-02'de ölçülen değerdir** | `grep -n 'DUZENLENEMEYEN' dotnet/denetim.sh` → **P-02 bloğunda bu sayaç YOK**; P-02 raporunda da geçmiyor | Taban **bugün taze ölçüldü** (PLAN § 1 md.2). Ölçüm komutu denetim.sh'a yorum olarak yazıldı. Belgeden kopyalanan hiçbir rakam yok. |
| S2 | B-54 "**21** gerekçeli istisna" | Politika haritasında düzeltme yolu (HedefUc/AlternatifYol) **hiç olmayan** SALT_OKUNUR/STORNO kaydı bugün **24**; ekran düzeyinde kalıpsız yazan sayfa **3** | Plandaki 21 rakamı doğrulanamadı, **kullanılmadı**. İstisna kütüğü ekran düzeyinde kuruldu (3 satır, tavan 3). Politika haritasındaki 24 kalem **kapsam dışı bırakılmadı**, § 6'da açık kalan olarak yazıldı. |
| S3 | Dosya listesinde `Kuyumcu.Tests/UX1C3DuzeltmeSozlesmeTestleri.cs` | O dosya UX-1-C3'ün **kendi sözleşme** testleri (storno/pusula); P-06 testleri oraya karışırsa iki paketin kanıtı iç içe geçerdi | Yeni dosya: `Kuyumcu.Tests/P06DuzenlenebilirlikTestleri.cs` (22 test). Mevcut dosya **değiştirilmedi**. |
| S4 | Dosya listesinde `Kuyumcu.Core/I18n/DogrulamaSozlugu.cs` | O dosya **sunucu** DTO doğrulama mesajlarını çevirir; P-06'nın eklediği metinler **istemci** form/şablon metinleridir | Metinler `Kuyumcu.Core/I18n/Sozlukler.cs`'e (TR+EN) yazıldı; `DogrulamaSozlugu.cs`'e **dokunulmadı** (gereksiz tek-el çakışması yaratmamak için). |
| S5 | Dosya listesinde `Modules/Meta/DuzeltmePolitikasi.cs:87` | Satır 87 bugün `GerekceAnahtarlari` bloğunun ortası; istisna kütüğü oraya girmez | Kütük dosyanın sonuna, `Varliklar` sözlüğünden **önce** kondu. Satır numarası kaymış — koda bakıldı (talimat md.3). |
| S6 | Sayaç adı `DUZENLEME_KALIBI_YOK_SAYFA = 0` | Kalıpsız sayfa sayısı 0 olamıyor (3 gerekçeli istisna var) | Sayaç **"kütüksüz kalıpsız sayfa = 0"** olarak tanımlandı: kalıpsız her sayfa kütükte **yazılı** olmak zorunda. Kütük ayrıca kendi cırcır tavanını taşıyor (`ISTISNA_KUTUK_TAVAN`), böylece "gizleyerek sayaç düşürme" mekanik olarak imkânsız. |

**Plan ile koşullar çelişkisi:** tespit edilmedi (koşullar planı tekrarlıyordu).

---

## § 3 — Kimlik kapanış tablosu

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-4** — düzenlenemeyen veri girişi → 0, şablon kuralı | **KAPANDI (mekanizma)** | `Bilesenler/DuzenlenebilirListe.razor:113-125` (çizim anında red) · test `P06_YAZAN_her_sayfa_KALIP_ya_da_KUTUKLU_istisnadir` · denetim.sh `P-06: DUZENLENEMEYEN_GIRIS 3 (tavan 3)` + `DUZENLEME_KALIBI_YOK_SAYFA 0` |
| **B-13** — ortak SatirIciDuzenle sözleşmesi (Enter/odak) | **KAPANDI** | `SatirIciDuzenle.razor:27,87-108` · testler `P06_ENTER_tam_bir_istek_uretir`, `P06_ESC_vazgecer_ve_ESKI_degere_doner`, `P06_ACILISTA_ilk_alana_odaklanma_SOZLESMESI_kodda_vardir` · mutasyon M06-3 |
| **B-18** — alan altı doğrulama + iş kuralı sınırları | **KAPANDI** | `SatirIciDuzenle.razor:131-136,149-167` · testler `P06_ARALIK_disi_deger_alan_alti_mesaj_verir`, `P06_ARALIK_icindeki_deger_GECER`, `P06_SAYI_OLMAYAN_giris_alan_alti_mesaj_verir`, `P06_DOGRULAMA_mesaji_ALAN_ANAHTARLI_id_tasir` · denetim.sh `ALAN_ANAHTARSIZ_DOGRULAMA 0` · mutasyonlar M06-2, M06-4 |
| **B-34** — dolu formda ilk "Kaydet" etkisiz | **KAPANDI** | test `P06_DOLU_formda_ILK_kaydet_istek_uretir` (mousedown→blur→click gerçek sırası, `Assert.Equal(1, sayac)`) · **mutasyon M06-1 kanıtı**: blur bastırma kaldırılınca bu test + 2 S-3c gerileme testi KIRMIZI |
| **B-54** — gerekçeli düzenlenemezlik istisnaları | **KISMEN** | Ekran düzeyi **kapandı**: `DuzeltmePolitikasi.EkranIstisnalari` (3 satır, her biri gerekçe + somut düzeltme yeri) · testler `P06_KUTUKTEKI_her_istisna_GERCEKTEN_kalipsizdir`, `P06_HER_istisna_GEREKCE_ve_SOMUT_duzeltme_yeri_yazar`, `P06_ISTISNA_sayisi_TAVANI_asamaz` · mutasyon M06-7. **Açık:** politika haritasındaki 24 SALT_OKUNUR/STORNO kaydının düzeltme yolu hâlâ boş (§ 6/A1). |
| **Y-15** — düzeltme politikası rozeti + iç kod temizliği | **KAPANDI** | testler `P06_HAM_politika_kodu_EKRAN_METNINE_basilmaz`, `P06_HER_gerekce_anahtarinin_TR_ve_EN_karsiligi_VARDIR` · `VeriDisaAktarim.razor:151-153` gerekçe sözlükten çözülür |
| **B-59** — ham teknik değer gösterimi | **KISMEN** | `P06_HAM_politika_kodu_EKRAN_METNINE_basilmaz` ham politika kodunu (SALT_OKUNUR/STORNO/DUZENLENEBILIR) 42 sayfada tarar → 0. ULID/enum/slug taraması bu paketin yüzeyi değil → **devredildi: P-14 / P-17** |
| **B-60** — başarı bandı kalıbı | **KISMEN** | Şablon tarafı: `DuzenlenebilirListe` hata bandını `role="alert"` ile kendi yerinde çizer (`P06_DuzenlenebilirListe_HATAYI_bos_duruma_CEVIRMEZ`). Sayfa sayfa başarı bandı denetimi → **devredildi: P-17** |
| **B-63** — erişilebilirlik toplu borcu | **KISMEN** | `aria-invalid` + `aria-describedby` + `role="alert"` şablona girdi (`P06_DOGRULAMA_mesaji_ALAN_ANAHTARLI_id_tasir`, mutasyon M06-4). İsimsiz düğme / eksik h1 / şifre sıfırlama → **devredildi: P-09, P-14** |
| **B-65** — metin ve adlandırma temizliği | **DEVREDİLDİ: P-14** | Bu paketin yüzeyi form/şablon; çift başlık ve çift GET sayfa işidir |
| **B-66** — onay ve geri alma | **KISMEN** | `DegismezIpucu` yolunda devre dışı düğme üretilmiyor (`P06_DuzenlenebilirListe_GEREKCE_ile_DegismezIpucu_cizer` → `Assert.Empty(FindAll("button"))`). Yıkıcı eylem onay modalı → **devredildi: P-12** |
| **B-67** — sessiz etkisizlik | **KAPANDI (şablon yüzeyi)** | `P06_BOS_gonderimde_alan_alti_mesaj_cikar_ve_duzenleme_ACIK_kalir` + M06-2: her reddedilen gönderim alan altı mesaj üretir |
| **B-68** — boş durum / hata ayrımı | **KAPANDI (şablon yüzeyi)** | `P06_DuzenlenebilirListe_HATAYI_bos_duruma_CEVIRMEZ` + `P06_DuzenlenebilirListe_GERCEKTEN_bos_listede_bos_durum_cizer` · mutasyon M06-6 |
| **B-70** — oluşturma ↔ düzenleme alan paritesi | **KISMEN** | `Siparisler.razor` düzenleme yolu beyan edildi ve hedefi doğrulandı (M06-8). Alan kümesi paritesi taraması → **devredildi: P-10 ("sunucunun kabul ettiği alanları forma bağla")** |

---

## § 4 — Derleme, testler, mutasyon

### Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata** (exit 0).

### Testler
| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Filtreli (`~P06Duzenlenebilirlik`) | 22 | 22 | 0 | 0 |
| **Tam süit** (`--logger trx`) | **3004** | **3004** | **0** | 0 |

Önce: 2982 · Sonra: 3004 · **Yeni test: 22.** trx: `…/scratchpad/trx-pP-06/`.

### Mutasyon tablosu (hepsi `cmp` ile birebir geri alındı)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M06-1** | `SatirIciDuzenle` blur bastırma bayrağı (`dugmeyeBasildi`) kaldırıldı | `P06_DOLU_formda_ILK_kaydet_istek_uretir` · `S3cFormSaglamligiTestleri.SatirIci_KAYDET_dugmesinde_blur_ile_CIFT_gonderim_olmaz` · `…SatirIci_IPTAL_dugmesinde_blur_KAYDETMEZ` (3 kırmızı) | `cmp` AYNI |
| **M06-2** | `Kaydet()` içindeki `AralikHatasi()` kapısı kaldırıldı | `P06_ARALIK_disi_deger_alan_alti_mesaj_verir` · `P06_SAYI_OLMAYAN_giris_alan_alti_mesaj_verir` | `cmp` AYNI |
| **M06-3** | `AcilistaOdaklan` varsayılanı `true` → `false` | `P06_ACILISTA_ilk_alana_odaklanma_SOZLESMESI_kodda_vardir` | `cmp` AYNI |
| **M06-4** | Girdiden `aria-describedby` kaldırıldı | `P06_DOGRULAMA_mesaji_ALAN_ANAHTARLI_id_tasir` · **denetim.sh** `ALAN_ANAHTARSIZ_DOGRULAMA` ✗ | `cmp` AYNI |
| **M06-5** | `DuzenlenebilirListe` kapısı `throw` → `_ = new …` | `P06_DuzenlenebilirListe_EYLEMSIZ_ve_GEREKCESIZ_cizilemez` | `cmp` AYNI |
| **M06-5b** | (M06-5 tekrarı, sayaç açığı kapatıldıktan sonra) | **denetim.sh** `SABLON_BUTUNLUGU` ✗ | `cmp` AYNI |
| **M06-6** | Hata dalı `@if (false && …)` yapıldı (hata → boş durum) | `P06_DuzenlenebilirListe_HATAYI_bos_duruma_CEVIRMEZ` | `cmp` AYNI |
| **M06-7** | İstisna kütüğüne 4. satır eklendi | `P06_ISTISNA_sayisi_TAVANI_asamaz` · **denetim.sh** `ISTISNA_KUTUK_TAVAN 4 > 3` ✗ | `cmp` AYNI |
| **M06-8** | `Siparisler.razor`'dan `data-duzenleme-yolu` kaldırıldı | `P06_YAZAN_her_sayfa_KALIP_ya_da_KUTUKLU_istisnadir` | `cmp` AYNI |
| **M06-8b** | (M06-8 tekrarı, sayaç açığı kapatıldıktan sonra) | **denetim.sh** `DUZENLENEMEYEN_GIRIS 4 > 3` ✗ + `DUZENLEME_KALIBI_YOK_SAYFA 1 ≠ 0` ✗ | `cmp` AYNI |

### ⚠ Mutasyonun bulduğu İKİ SAYAÇ AÇIĞI (md.1 — gizlenmedi, düzeltildi)
1. **M06-5 denetim.sh'ı kırmadı.** Sayaç `grep -q 'InvalidOperationException'` yapıyordu; `_ = new InvalidOperationException(` hâlâ eşleşiyordu. Kalıp `throw new InvalidOperationException` olarak sıkılaştırıldı → **M06-5b'de sayaç ✗ verdi.**
2. **M06-8 denetim.sh'ı kırmadı.** Sayaç `grep -q 'data-duzenleme-yolu'` yapıyordu; kaldırılan öznitelikten sonra **yorum satırındaki** aynı kelime eşleşiyordu. Kalıp `data-duzenleme-yolu="<Ad>.razor"` regex'ine sıkılaştırıldı → **M06-8b'de sayaç ✗ verdi.**

Son mutasyondan sonra temiz derleme: **0 Uyarı · 0 Hata.**

---

## § 5 — denetim.sh ve canlı doğrulama

### `dotnet/denetim.sh` → **DENETİM TEMİZ** (exit 0)
Yeni `# >>> P-06 … # <<< P-06` bloğu (denetim.sh:1684-1802), her sayacın **ölçüm komutu yorumda**:

```
═══ P-06 düzenlenebilirlik şablonu ═══
  ✓ P-06: DUZENLENEMEYEN_GIRIS         3 (tavan 3, cırcır)
  ✓ P-06: DUZENLEME_KALIBI_YOK_SAYFA   0 (-eq 0)
  ✓ P-06: ISTISNA_KUTUK_TAVAN          3 (tavan 3, cırcır)
  ✓ P-06: ALAN_ANAHTARSIZ_DOGRULAMA    0 (-eq 0; hata id alan anahtarlı + aria-describedby)
  ✓ P-06: BU_PAKETTE_EKLENEN_RECORD    0 (-eq 0)
  ✓ P-06: SABLON_BUTUNLUGU             DuzenlenebilirListe kapısı + odak + aralık yerinde
DENETİM TEMİZ.
```

**Taban ölçümleri (2026-09-18, taze):** yazma yapan 42 `.razor`'dan kalıpsız olan **3** (`Giris`, `Panel`, `Fiyatlandirma`) · kütük **3** · toplu doğrulama mesajı **0** · bu pakette eklenen record **0**. Tavanlar yalnız azalır.

### Canlı doğrulama — port 5198 (5199'a **dokunulmadı**)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `GET /health` | 200 | **200** | hazır olana dek beklendi |
| 2 | `GET /api/v1/meta/duzeltme-politikalari` jetonsuz | 401 | **401** | `{"message":"Token bulunamadı","statusCode":401,…}` |
| 3 | Aynı uç geçersiz jetonla | 401 | **401** | `{"message":"Geçersiz veya süresi dolmuş token",…}` |
| 4 | `GET /siparisler` (uygulama kabuğu) | 200 | **200** | `<title>Kuyumcu ERP` |
| 5 | `GET /veri-disa-aktarim` | 200 | **200** | — |
| 6 | `GET /_framework/Kuyumcu.Web.wasm` | 200 + yeni bileşen içinde | **200**, `strings` çıktısında **`DuzenlenebilirListe`** bulundu | P-06 kodu servis edilen anlık görüntüde |
| 7 | SQL: `tenants` · `__EFMigrationsHistory` | 8 · 52 (değişmez) | **8 · 52** | migration yok, şema el değmedi |
| 8 | SQL: `tenants where ad like 'PAKET-P-06%'` | 0 | **0** | test kiracısı **oluşturulmadı** (aşağı bkz.) |
| 9 | API durdurma | 5198 boş | **`lsof -nP -iTCP:5198 -sTCP:LISTEN` = 0 satır** | sadece kendi PID'im öldürüldü |

**Test kiracısı açılmadı — sebebi (md.1, gizlenmedi):** `POST /api/v1/auth/kayit` **403** döndü: *"Geçerli bir davet jetonu gerekiyor. Kayıt yalnız yöneticinin ürettiği tek kullanımlık davetle açılır…"*. Davet jetonu üretmek sistem anahtarı ister; P-06 **hiçbir uç eklemediği ve şema değiştirmediği** için jetonlu bir akış zorlamak yerine kiracı **hiç açılmadı** — `tenants` 8'de kaldı, `QA-TEST-*` fikstürlerine **dokunulmadı**. Kiracı gerektiren tek kanıt olan **gerçek tarayıcıda odak/Enter doğrulaması zaten plan DoD md.7 gereği QA turuna bırakılmıştır** (aşağı § 6/A2).

**`strings` notu (dürüstlük):** servis edilen `Kuyumcu.Core.wasm` içinde yeni Türkçe sözlük metinleri `strings` ile **bulunamadı** — ama **eski, bilinen bir metin de bulunamadı** (kalibrasyon: `'Bu alan boş bırakılamaz'` → 0). Yani bu araç bu dosyada metin çıkaramıyor; **negatif sonuç kanıt değildir**, bu yüzden tablo 6. satırda yalnız tip adı kanıtı kullanıldı.

---

## § 6 — Açık kalanlar

| # | Konu | Neden açık | Nereye |
|---|---|---|---|
| **A1** | Politika haritasındaki **24** SALT_OKUNUR/STORNO kaydının `HedefUc`/`AlternatifYol` alanı boş — kullanıcıya "değiştirilemez" deniyor ama **somut düzeltme yolu** yazılmıyor (ör. `DenetimKaydi`, `GunKapanisi`, `SayimOturumu`, `TAKAS`, `TRANSFER`, `EVRAK_CIRO`) | Her biri **ayrı bir iş kararı** (hangi ters kayıt, hangi fiş); bu pakette karar uydurmak md.1 ihlali olurdu | **P-12** (defter tarafı düzenlenebilirlik + storno görünürlüğü) — Aşkın onayı ile |
| **A2** | `document.activeElement` odak kanıtı **gerçek tarayıcıda** alınmadı | bUnit odağı taşımaz; plan DoD md.7 bunu zaten **QA turuna** havale ediyor | DALGA 2 **QA turu** (`betikler/qa-turu.js`) |
| **A3** | `Fiyatlandirma.razor` ikinci düzenleme kalıbını (`duzenlenenId`) kullanmaya devam ediyor | Tek dokunuş kuralı: sayfa bu dalgada başka pakette de açılabilir; yeniden yazım kapsam büyütürdü | **P-17** (kütükte yazılı) |
| **A4** | `DuzenlenebilirListe` henüz **hiçbir sayfada kullanılmıyor** (şablon kuruldu, göç başlamadı) | P-06 **mekanizma paketidir**; sayfa göçü tek-el zincirlerine tabidir (§ 7 dosya çakışma haritası) | **P-10 · P-13 · P-16** |

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** `Pages/VeriDisaAktarim.razor` · `Pages/Siparisler.razor`. İkisine de bu dalgada başka paket dokunmadı.

---

## § 7 — Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Mevcut Enter/Esc/blur sözleşmesi korunur + gerileme testi | ✅ | S-3c testleri yeşil (M06-1'de kırmızıya döndüler) + `P06_ENTER…`, `P06_ESC…` |
| Enter tam 1 istek · Esc vazgeçer · açılışta ilk alan odaklanır | ✅ (odak: kod+QA) | `P06_ENTER_tam_bir_istek_uretir`, `P06_ESC_vazgecer…`, `P06_ACILISTA…` + § 6/A2 |
| Dolu formda ilk Kaydet istek üretir (mutasyonla) | ✅ | `P06_DOLU_formda_ILK_kaydet_istek_uretir` + **M06-1** |
| PATCH: gönderilmeyen alan = değişmedi | ✅ (dokunulmadı) | P-06 hiçbir uç eklemedi/değiştirmedi; `SatirIciDuzenle` **tek alan** gönderir |
| Her doğrulama hatası alan altında; toplu mesaj yasak | ✅ | `ALAN_ANAHTARSIZ_DOGRULAMA = 0` + `P06_DOGRULAMA_mesaji_ALAN_ANAHTARLI_id_tasir` |
| B-54 istisnaları gerekçe + somut yol ile yazılır, gizlenmez | ✅ ekran / ⚠ politika | `P06_HER_istisna_GEREKCE_ve_SOMUT_duzeltme_yeri_yazar` · § 6/A1 |
| Sayaçlar denetim.sh'a cırcır olarak bağlandı, taban taze ölçüldü | ✅ | denetim.sh:1684-1802, ölçüm komutları yorumda |
| Tavan/eşik artırılmadı | ✅ | Tüm P-06 tavanları ilk kez ve **ölçülen değerde** kondu |
| Pages içinde yeni istemci record açılmadı | ✅ | `BU_PAKETTE_EKLENEN_RECORD = 0` + `P06_Bilesenler_altinda_YENI_istemci_record_ACILMADI` |
| Mevcut element id'leri değişmedi | ✅ | Yalnız **eklenen** id'ler: `dl-*`; `si-*` ve `sip-detay-*` aynen korundu |
| Sözlük TR+EN, çıplak metin yok | ✅ | 6 yeni anahtar × 2 dil · `P06_HER_gerekce_anahtarinin_TR_ve_EN…` |
| Migration yok | ✅ | `__EFMigrationsHistory` 52 → 52 |
| Tam süit 0 başarısız | ✅ | 3004/3004 |
| denetim.sh TEMİZ | ✅ | exit 0 |
| Test kiracısı silindi / fikstüre dokunulmadı | ✅ | Kiracı **hiç açılmadı**; `tenants` 8 → 8; `QA-TEST-*` el değmedi |
| COMMIT YOK · git komutu yok · rm/mv yok | ✅ | Yalnız `git status --porcelain` (salt okunur) |
| Sır/parola çıktıya yazılmadı | ✅ | Parola yalnız `$P` değişkeninde; uzunluk dahi maskelendi |
| 5199'a dokunulmadı | ✅ | Yalnız 5198 kullanıldı ve kapatıldı |

---

## § 8 — Değişen dosyalar (bu paket)

```
dotnet/Kuyumcu.Web/Bilesenler/DuzenlenebilirListe.razor   (YENİ, 126 satır)
dotnet/Kuyumcu.Tests/P06DuzenlenebilirlikTestleri.cs      (YENİ, 22 test)
dotnet/Kuyumcu.Web/Bilesenler/SatirIciDuzenle.razor       (genişletildi)
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs     (EkranIstisnalari kütüğü)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                     (6 anahtar × TR+EN)
dotnet/Kuyumcu.Web/Pages/VeriDisaAktarim.razor            (DegismezIpucu)
dotnet/Kuyumcu.Web/Pages/Siparisler.razor                 (data-duzenleme-yolu + erişilebilir etiket)
dotnet/denetim.sh                                         (# >>> P-06 bloğu, 6 sayaç)
```
`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısındaki diğer satırlar bu paketten **önce** vardı (depo zaten kirli; `bbfdc95 first commit` dışında commit yok).

---

## § 9 — Kurucuya / mali müşavire notlar

- **Mevzuat sabiti yazılmadı.** P-06 hiçbir yasal hüküm kodlamadı; VUK md.219/234, KVKK md.11 atıfları **mevcut** sözlük metinlerinden geliyor, bu pakette yeni bir yasal iddia üretilmedi.
- **§ 6/A1 kurucuya değil ekibe:** 24 kaydın düzeltme yolu P-12'de ekip oyuyla kararlaştırılacak; bir tanesi (gün kapanışı düzeltmesi) mali müşavir görüşü isteyebilir — P-12 raporunda ayrıca sorulacak.
- **Kayıt daveti kapısı çalışıyor:** `/auth/kayit` davet jetonu olmadan 403 veriyor (Mehmet'in koşulu canlıda doğrulandı). Pilot davetleri bu yüzden **K-19** kaleminde kurucuda kalmaya devam ediyor.

**Tahmin/gerçekleşen:** 300 ajan-dk tahmin · yaklaşık **240 ajan-dk** (mekanizma paketi olduğu için ekran göçü yapılmadı; göç P-10/P-13/P-16'ya bırakıldı).

**Durum: BİTTİ.**
