# Tasarım Denetimi — 21 Eylül 2026

> Paralel tasarım denetçisi şeridi. Kurucunun talimatı: *"tüm sayfalardaki tasarımı kontrol eden başka bir paralel ajan görevlendir."*
> **Salt okunur tur.** Hiçbir kaynak dosya değiştirilmedi, build/test koşulmadı, hiçbir kayıt kaydedilmedi veya silinmedi.
> Ham veri: `dokumanlar/09-uygulama-kayitlari/` yanındaki scratchpad dosyası `tasarim-denetimi.json` (yol § 10'da).

## 0. Kapsam ve yöntem

| | |
|---|---|
| Sunucu | `http://localhost:5199` (QA yayın anlık görüntüsü) |
| Genişlikler | 1440×900 · 1280×900 · 1024×820 · 375×812 |
| Ölçülen | 43 rota + 25 açılan form/çekmece durumu = **68 anahtar × 4 genişlik = 267 ölçüm satırı** |
| Ölçüm aracı | `element.getBoundingClientRect()`, `getComputedStyle()`, `document.elementFromPoint()`. **Göz kararı yok.** |
| Eşikler | hiza > 2 px · yükseklik > 2 px · kesik `scrollWidth > clientWidth + 1` · dokunma < 44 px |

**Önemli yöntem düzeltmesi (Anayasa md. 1).** İlk turda sabit süreli bekleme kullanıldı ve `/gider`, `/hurda`, `/isletme`, `/kurulum`, `/veri-ice-aktarim`, `/islemler` gibi sayfalar **eksik** ölçüldü (örn. `/hurda` 7 kontrol yerine 1 kontrol raporladı). `window.fetch` sarmalanıp "açık istek = 0 + son yanıttan 380 ms + DOM 2 örnekte sabit" koşuluna geçildi ve **bütün turlar baştan koşuldu**. Raporun tamamı bu ikinci (v3) turun verisidir.

**Formlar düğme arkasında.** Sayfaların çoğu varsayılan görünümde 1-6 kontrol gösteriyor. Form satırlarını ölçebilmek için her sayfada "+ Yeni …" ve "Düzenle" düğmeleri açıldı, ölçüm yapıldı, Esc/perde ile kapatıldı. **Hiçbir kaydet/sil düğmesine basılmadı.** `/islemler`'de kurucunun mükerrer fiyat örneğini doğrulamak için bir ürün seçildi, kaydedilmedi.

**ÖLÇÜLMEDİ (sebep).**
- **Ekran görüntüsü:** tarayıcı paneli gizli olduğu için sayfa kare üretmiyor; `computer.screenshot` 5 sn zaman aşımına uğradı. En kötü 6 sayfanın görüntüsü **bu turda çekilemedi**. Piksel ölçümleri etkilenmedi (gizli sekmede de düzen hesaplanıyor; kurucunun bildirdiği 19 px kusuru birebir yeniden üretildi).
- **Açılan form durumlarında (25 anahtar) 375 px ekran içi dokunma sayımı:** yalnızca 43 temel rotada yapıldı.

**AÇILMADI.**
- **`/vade-takvimi` — BOŞ İÇERİK (4 genişlikte de).** Rota çözülüyor, `main.ana-icerik` metni 40 karakterin altında, sayfa gövdesi gelmiyor. Tek kontrol sayılıyor (kenar çubuğu araması). Bu bir tasarım kusuru değil, **eksik/bozuk sayfadır** — ilgili şeride eskale edilmeli.
- **`/giris`** oturum açıkken `/` adresine yönlendiriyor. Gerçek giriş ekranı ayrıca ölçüldü (jetonlar silinip tam yeniden yükleme yapıldı): **4 genişlikte de hiza/kesik/taşma kusuru yok**, yalnızca 4 dokunma hedefi 44 px altında (TR 32×22, EN 32×22, "Giriş Yap" ve "İşletme Kur" yüksekliği 33 px).

---

## 1. Özet

### 1.1 Kusur türü × adet

| # | Kusur türü | Kayıt | Ayrı kalıp | Değerlendirme |
|---|---|---|---|---|
| 2 | **Form satırı hizası — kademe A (kesin)** | **28** | **8 satır kalıbı / 6 ekran** | **Gerçek. En kritik bölüm.** |
| 2 | Form satırı hizası — kademe B (şüpheli) | 30 | — | Büyük olasılıkla yanlış pozitif (açık çekmecenin arkasındaki filtre şeridi aynı y bandında) |
| 3 | Yükseklik tutarsızlığı | 31 | 10 grup | Gerçek kök: metin kutusu 34,5 px ↔ seçici düğmesi 32 px |
| 4 | Kesik metin — gerçek | 6 | 2 alan | `input#duz-eposta` 39 px, `input#duz-unvan` 28 px |
| 4 | Kesik metin — yanlış pozitif | 39 | 6 seçici | `label.gorsel-gizli` 1×1 px ekran okuyucu etiketi — **kusur değil** |
| 5 | Taşma — belge yatay kayması | **67** | 1 kök | **Yalnız 1024 px, TÜM sayfalar**, 91-160 px |
| 5 | Taşma — ana içerik yatay | 1 | — | `/hurda` @375: 58 px |
| 5 | Taşma — viewport'u aşan öğe | 67 | tablo kalıbı | 375 px'te 91-1074 px |
| 6 | Mükerrer bilgi — gerçek | 29 | **8 alan / 5 ekran** | `span#*-onizleme` kalıbı |
| 6 | Mükerrer bilgi — şüpheli | 170 | 78 | Tablo gövdesinde aynı değerin iki satırda geçmesi — çoğu normal |
| 7 | Boşluk/ölçek tutarsızlığı | 257 | 1 kök | 15 farklı düğme yüksekliği (22-71 px), 5 farklı kart yarıçapı |
| 8 | Dokunma hedefi < 44 px @375 | **443 / 455 (%97,4)** | 1 kök + gövde | Ekran içi ölçüm |
| 9 | Açılır panel görüş alanı dışında | 5 (+2 kısmi) | 1 bileşen | `.secici-liste-kap` |
| — | Açılmayan sayfa | 1 rota × 4 genişlik | — | `/vade-takvimi` BOŞ |

### 1.2 En kötü 10 sayfa

| Sıra | Ekran | Neden |
|---|---|---|
| 1 | **`/islemler`** (+ `# Ekle`, ürün seçili) | Kurucunun 19 px satırı (3 genişlik) · "Ekle" sonrası **41 px** · açılır panel 1280'de 170 px, 1024'te 324 px katlamanın altında · 375'te 4 öğe ekranı aşıyor (iade düğmeleri 197/298 px) · mükerrer birim fiyat · 18/19 dokunma hedefi < 44 px |
| 2 | **`/cariler # ✏️ Düzenle`** | 2 kademe-A hiza kusuru (22 px ×2, 3 genişlikte) · 2 gerçek kesik girdi (39 px, 28 px) · mükerrer kredi limiti · 1024'te tablo 80 px aşıyor |
| 3 | **`/urunler # ✏️ Düzenle` / `# + Yeni Ürün`** | 33 kontrollü dev form · yükseklik tutarsızlığı 4 genişlikte · mükerrer işçilik tutarı · 375'te tablo 965 px aşıyor · 1024'te 2 öğe taşıyor |
| 4 | **`/isletme`** | 1280'de 2, 1024'te 3 açılır panel **tamamen** görüş alanının altında (223-471 px) |
| 5 | **`/hurda`** | Ödeme türü paneli tamamen katlamanın altında (1280: 249 px, 1024: 407 px) · 375'te **ana içeriğin kendisi** 58 px yatay kayıyor · mükerrer birim fiyat |
| 6 | **`/cariler # + Yeni Cari`** | `fieldset#cari-p16` 22 px hiza kusuru, 3 genişlikte |
| 7 | **`/urunler/etiketler`** | `form-grid-2` 20,5 px hiza kusuru, 3 genişlikte · 375'te tablo 363 px aşıyor |
| 8 | **`/emanet`** (+ `# + Emanet Al`) | 10,2 px hiza kusuru, **4 genişlikte de** (tek ekranda 8 kayıt) |
| 9 | **`/faturalar # + Yeni Fatura` / `# Düzenle`** | 4,5 px yükseklik tutarsızlığı 4 genişlikte · mükerrer toplam · 1024/375'te taşma |
| 10 | **`/kur`** | 375'te 2 tablo ekranı aşıyor (259 px, 234 px) · tek sayfada 101 adet 29 px yüksekliğinde düğme · 20 şüpheli mükerrer kayıt (incelendi: alış/satış ve geçmiş tablolarında aynı tutarların geçmesi — kusur değil) |
| — | `/vade-takvimi` | **AÇILMADI — boş içerik** (4 genişlikte) |

---

## 2. Form satırı hizası — en önemli bölüm

Eşik: aynı satırdaki kontrollerin `top` farkı **> 2 px**.
Satır tanımı: aynı bölgede (main/üst bar/kenar), dikey olarak örtüşen ve `|Δtop| ≤ 40 px` olan `input, select, textarea, [role=combobox]` kontrolleri. `checkbox/radio/range/file/submit` hariç (bunların farklı hizalanması normaldir).
**Kademe A** = kontrollerin ortak atasına uzaklık ≤ 4 → kesin aynı satır. **Kademe B** = aynı y bandı ama ortak ata uzak.

### 2.1 KADEME A — kesin kusurlar (28 kayıt, 8 satır kalıbı)

| # | Sayfa | Satır kapsayıcısı | Alanlar | Fark | Genişlikler |
|---|---|---|---|---|---|
| A1 | `/islemler # Ekle` | `div.kart-govde.form-dikey > div.satir` | Adet (isteğe bağlı) · Birim Fiyat (₺) (isteğe bağlı) · Ürün | **41,0 px** | 1024, 1280, 1440 |
| A2 | `/cariler # + Yeni Cari` | `fieldset#cari-p16` | Segment · Standart iskonto (%) (isteğe bağlı) | **22,0 px** | 1024, 1280, 1440 |
| A3 | `/cariler # ✏️ Düzenle` | `fieldset#duz-p16` | Segment · Standart iskonto (%) (isteğe bağlı) | **22,0 px** | 1024, 1280, 1440 |
| A4 | `/cariler # ✏️ Düzenle` | `div#cekmece-cari-govde > div.form-dikey > div.form-grid-2` | Kredi Limiti (isteğe bağlı) · Varsayılan vade (gün) (isteğe bağlı) | **22,0 px** | 1024, 1280, 1440 |
| A5 | `/evraklar` | `div.kart > div.kart-govde.form-grid-3` | Evrak türü · Vade günü (Bitiş) (isteğe bağlı) · Vade günü (Başlangıç) (isteğe bağlı) | **22,0 px** | 1024 |
| A6 | `/urunler/etiketler` | `div.kart.yazdirma-gizle > div.form-grid-2` | Ara · Her üründen kaç adet (isteğe bağlı) | **20,5 px** | 1024, 1280, 1440 |
| A7 | **`/islemler`** | `div.kart-govde.form-dikey > div.satir` | Adet (isteğe bağlı) · Ürün · Birim Fiyat (₺) (isteğe bağlı) | **19,0 px** | 1024, 1280, 1440 |
| A7b | `/islemler` (ürün seçili) | aynı `div.satir` | Adet (isteğe bağlı) · Ürün | **19,0 px** | 1024 |
| A8 | `/emanet` ve `/emanet # + Emanet Al` | `main.ana-icerik > div.dikey-bosluk > div.satir` | Müşteri ara (ad, telefon) · Müşteri | **10,2 px** | **1440, 1280, 1024, 375 (hepsi)** |

**A7 = kurucunun bildirdiği kusurdur ve birebir doğrulandı:** `top` değerleri 1440'ta 614,5 / 633,5 / 633,5 → **19 px**. "Adet (isteğe bağlı)" etiketi tek satır, "Birim Fiyat (₺) (isteğe bağlı)" iki satıra sarıyor; satır etikete göre hizalandığı için kutu 19 px yukarıda kalıyor.

**A1 aynı satırın "Ekle" sonrası hâlidir ve kusur 41 px'e çıkıyor** — yani kullanıcı kalem eklemeye çalıştıkça kayıklık büyüyor.

### 2.2 KADEME B — şüpheli (30 kayıt)

Kademe B kayıtlarının **tamamı** kapsayıcı olarak `main.ana-icerik` veriyor; hepsi açık bir çekmece/form alanı ile arkada kalan sayfa filtre şeridinin aynı y bandına düşmesinden kaynaklanıyor (örn. `/cariler # Düzenle` @1280: "Ad, telefon veya e-posta ara…" (arka plandaki liste filtresi) ile "Tür / Telefon / E-posta" (çekmece içi) 25,5 px). Bunlar **farklı görsel katmanlardır**, muhtemelen yanlış pozitiftir; kesin kusur sayısı **kademe A = 28**'dir. Tam liste JSON'da (`hizaKusurlari`, `kademe: "B"`).

---

## 3. Kesik metin

Kural: `label, button, th, a, rozet, [role=combobox]` için `scrollWidth > clientWidth + 1`; girdi kutuları ayrı listelendi.

### 3.1 Gerçek kusurlar (2 alan × 3 genişlik = 6 kayıt)

| Sayfa | Alan | Seçici | scrollWidth | clientWidth | Fark | Genişlikler |
|---|---|---|---|---|---|---|
| `/cariler # ✏️ Düzenle` | E-posta (isteğe bağlı) | `input#duz-eposta` | 255 | 216 | **39 px** | 1024, 1280, 1440 |
| `/cariler # ✏️ Düzenle` | Ticari ünvan (isteğe bağlı) | `input#duz-unvan` | 227 | 199 | **28 px** | 1024, 1280, 1440 |

Yani kayıtlı e-posta ve ticari ünvan değerleri kutuya sığmıyor; kullanıcı değerin tamamını görmeden düzenliyor.

### 3.2 Yanlış pozitifler (39 kayıt) — kusur DEĞİL

`label.gorsel-gizli` öğeleri **1×1 px** ölçüsündedir (ekran okuyucu etiketi). `scrollWidth > clientWidth` bu öğelerde tasarım gereğidir. Doğrulandı: `/cariler` `div#cari-arama-sar > label.gorsel-gizli` → 1×1 px, sol 263. Dağılım: cari-arama 12, urun-arama 12, islem-barkod 9, e-cari-ara 8, et-ara 4, ara-belge 1.

**Görünür metin öğelerinde (label, button, th, rozet) 267 ölçüm satırında hiç gerçek kırpılma bulunmadı.**

---

## 4. Taşma

### 4.1 KRİTİK — 1024 px'te belge yatay kayıyor (TÜM sayfalar)

| Ölçüm | Değer |
|---|---|
| Kaynak | `div.uygulama > div.icerik-sutunu > header.ust-bar > div.ust-sag` → içindeki `div#ust-mod-anahtar` (Basit / Profesyonel modu anahtarı) |
| Sağ kenarı | 1115 – 1178 px (viewport 1024 px) |
| Taşma | **91 – 160 px**, ölçülen 67 sayfa-durumunun tamamında |
| Kanıt | `window.scrollTo(2000,0)` sonrası `window.scrollX = 124,5` (`/urunler` @1024) → sayfa gerçekten yatay kayıyor, ölçüm artefaktı değil |
| Diğer genişlikler | 1440: 0 · 1280: 0 · 375: 0 → **yalnızca 1024 px** |

Örnekler: `/vade-takvimi` 160 · `/pos/tahsilatlar` 156 · `/kullanicilar` 153 · `/bildirimler` 147 · `/siparisler` 141 · `/faturalar` 137 · `/urunler` 124 · `/kilavuz` 122 · `/ayarlar` 121 · `/cariler` 117 · `/` 108 · `/kur` 91.

Pratik sonucu: 1024 px'te (yaygın dizüstü/tablet yatay) her ekranda yatay kaydırma çubuğu çıkıyor ve **"Profesyonel" düğmesi ekranın dışında kalıyor**.

### 4.2 Ana içerik yatay kayması
Tek vaka: **`/hurda` @375 → `main.ana-icerik` 58 px** (kaynak `table#hrd-defter`).

### 4.3 Viewport'u aşan öğeler (en büyük 16)

| Sayfa | Gen. | Öğe | Aşım |
|---|---|---|---|
| `/faturalar` | 375 | `table.veri-tablosu` | 1074 px |
| `/fiyatlandirma` (+ Yeni Liste / Düzenle) | 375 | `table#fl-tablo` | 1006 px |
| `/urunler` | 375 | `table.veri-tablosu` | 965 px |
| `/banka` | 375 | `table.veri-tablosu` | 733 px |
| `/kullanicilar` | 375 | `table.veri-tablosu` | 702 px |
| `/fiyatlandirma` | 375 | `table#km-tablo` | 600 px |
| `/bildirimler` | 375 | `table.veri-tablosu` | 568 px |
| `/raporlar/kar-zarar` | 375 | `table.veri-tablosu` | 476 px |
| `/islemler` | 375 | `table.veri-tablosu` | 391 px |
| `/urunler/etiketler` | 375 | `table.veri-tablosu` | 363 px |
| `/raporlar` | 375 | `table.veri-tablosu` | 330 px |
| `/raporlar/islem-defteri` | **1280** | `table#id-tablo` | 313 px |
| `/islemler` | 375 | `button#islem-tur-ALIS_IADE` | **298 px** |
| `/islemler` | 375 | `button#islem-tur-SATIS_IADE` | **197 px** |
| `/sayim` | 375 | `table#sy-tablo` | 91 px |
| `/islemler` | 375 | `button#islem-tur-TRANSFER` | **88 px** |

`/islemler` işlem türü düğmeleri (Transfer, Satış İadesi, Alış İadesi) 375 px'te ekranın dışında kalıyor — tabloların aksine bunlar **kaydırma kabında değil**, dolayısıyla telefonda bu üç işlem türüne erişilemiyor olabilir (elle doğrulanmalı).

---

## 5. Mükerrer bilgi

### 5.1 Gerçek kusurlar — `span#*-onizleme` kalıbı (8 alan / 5 ekran)

Aynı değer hem girdinin **içinde** hem hemen **altında** gösteriliyor:

| Sayfa | Alan | Girdideki değer | Altındaki metin | Seçici | Genişlikler |
|---|---|---|---|---|---|
| **`/islemler`** (ürün seçili) | Birim Fiyat (₺) | `36893.96` | `₺36.893,96` (+ "fiyat listesinden geldi") | `span#islem-birimfiyat-onizleme` | 1024 (39 px altında) |
| `/hurda` | Birim fiyat (TL / has gram) | `6805.35` | `₺6.805,35` | `span#hrd-fiyat-onizleme` | 375/1024/1280/1440 |
| `/cariler # ✏️ Düzenle` | Kredi Limiti | `750000` | `₺750.000,00` | `span#duz-limit-onizleme` | 375/1024/1280/1440 |
| `/urunler # ✏️ Düzenle` | İşçilik Tutarı (₺) | `900` | `₺900,00` | `span#d-iscilik-onizleme` | 375/1024/1280/1440 |
| `/fiyatlandirma # + Yeni Liste` | İşçilik çarpanı (%) | `100` | `100` | `span#fl-carpan-onizleme` | 375/1024/1280/1440 |
| `/fiyatlandirma # Düzenle` | İşçilik çarpanı (%) | `100` | `100` | `span#fl-carpan-onizleme` | 375/1024/1280/1440 |
| `/faturalar # Düzenle` | Toplam | `119200.00` | `₺119.200,00` | `td.hucre-vurgu.sayi` | 1024, 1280 |
| `/raporlar/kasa` | Başlangıç (tarih) | `2026-09-08` | `2026-09-08` | `td.metin-kucuk` | 1280, 1440 |

**Kurucunun örneği doğrulandı.** `/islemler`'de varsayılan boş formda görünmüyor; ürün seçildiğinde ortaya çıkıyor — bu yüzden önceki turlarda yakalanmamış.
En kötüsü `/fiyatlandirma`: önizleme, girdiyle **birebir aynı** metni gösteriyor (`100` / `100`), hiçbir bilgi eklemiyor.

### 5.2 Şüpheliler (170 kayıt / 78 ayrı metin) — çoğu kusur DEĞİL

Aynı kapsayıcı içinde aynı metnin iki kez geçmesi genelde tablo davranışıdır. En sık: `₺0,00` (14 sayfa), `0.0000 has` (3), `1.000 g` (3). `/kur` tek başına 20 kayıt üretiyor (alış/satış tablosu ile geçmiş tablosunda aynı tutarlar). **Bunlar kusur olarak sayılmamalıdır.**

---

## 6. Boşluk / ölçek tutarsızlığı

Ölçüm 1280 px'te; diğer genişliklerde aynı kalıplar gözlendi. 257 sayfa-genişlik kaydında en az bir tutarsızlık var.

| Bulgu | Ölçülen |
|---|---|
| **Düğme yüksekliği** | Sistem genelinde **15 farklı değer**: 22, 23, 29, 31, 32, 35, 36, 37, 39, 43, 44, 45, 51, 53, 71 px |
| **Kontrol yüksekliği** | Metin kutusu `input.girdi` **34,5 px** ↔ seçici düğmesi `[role=combobox]` **32 px** ↔ bazı seçiciler **37 px** |
| **Kart köşe yarıçapı** | **5 farklı değer** karışık: 0px / 6px / 8px / 12px / 16px |
| **Düğme köşe yarıçapı** | 0px / 6px / 8px karışık (ör. `/cariler`: 8px ve 6px yan yana) |

En kötü örnekler:
- `/ayarlar` — tek sayfada **5 farklı düğme yüksekliği**: 32, 35, 36, 37, 45 px
- `/banka # + Yeni Hesap` — **7 farklı**: 22, 29, 31, 32, 35, 37, 43 px
- `/islemler` — 29, 31, 35, 37, 45 px
- `/kasalar # + Yeni Kasa` — 4 farklı kart yarıçapı: 0px, 8px, 12px, 16px
- `/hurda` — 29, 35 ve **71 px** düğme aynı sayfada
- `/siparisler # Yeni sipariş` — 23 px'lik 10 düğme ve 39 px'lik 1 düğme

---

## 7. Dokunma hedefi (375 px)

**Ölçüm düzeltmesi:** kenar çubuğu (`.kenar-cubugu`) 375 px'te `left: -240px` konumunda, **ekranın dışında**. DOM'daki 85-190 tıklanabilir öğe sayımı bu yüzden yanıltıcıdır. Anlamlı sayım ekran içi öğelerdir.

| | |
|---|---|
| 43 rotada ekran içi tıklanabilir öğe | **455** |
| Bunlardan 44 px'ten küçük | **443** |
| Oran | **%97,4** |

Her ekranda tekrar eden kabuk öğeleri: `Çıkış` 62×23 · `Menüyü aç` 34×32 · `⟳` 32×32 · `Basit` 56×23 · `Profesyonel` 96×23 — **beşi de 44 px'in altında**.

En yoğun sayfalar: `/vitrin` 28/28 · `/kilavuz` 19/19 · `/islemler` 18/19 · `/ayarlar` 18/20 · `/urunler/etiketler`, `/kasalar`, `/veri-disa-aktarim` 16/16 · `/yetkiler` 14/14 · `/kurulum` 13/13 · `/cariler` 13/13 · `/siparisler` 13/13.

Tam tablo (43 satır) JSON'daki `dokunmaHedefi375.satirlar` alanındadır.

---

## 8. Açılır panel örtüşmesi

Yöntem: `.secici-dugme` / `[role=combobox]` tetikleyicileri tıklandı, açılan `.secici-liste-kap` panelinin komşu kontrollerle kesişim alanı px² olarak toplandı, panel perdeye tıklanarak kapatıldı.

**Yorum notu (önemli):** bir açılır panelin **altındaki** içeriği örtmesi normaldir (panel z-index 41, perde 40). Gerçek kusur iki durumdur: (a) panelin **aynı satırdaki** komşu kontrolü örtmesi, (b) panelin **görüş alanının dışında** açılması.

### 8.1 Bilinen 5056 px² kusuru — DOĞRULANAMADI

Talimattaki *"şerit ikinci satıra sardığında 1280 px'te 5056 px²"* bulgusu bu turda **üretilemedi**. 1280 px'te `/urunler` filtre şeridi gerçekten ikinci satıra sarıyor ("Tüm reyonlar" tetiği y=314, diğerleri y=246) ama panellerin komşu kontrollerle kesişimi **0 px²** ölçüldü (paneller x ekseninde çakışmıyor). 1024 px'te aynı şeritte en fazla **3524 px²** ölçtüm ("Tüm reyonlar"). **Ölçülen tüm sayfalarda `ayniSatirPx2` = 0.** 5056 değeri ya farklı bir yayın sürümünde ya da farklı bir viewport yüksekliğinde oluşmuş olmalı — bu turda reprodüksiyonu yok.

### 8.2 Gerçek kusur: panel görüş alanının altında açılıyor (5 vaka)

| Sayfa | Gen. | Tetik | Panel top | Viewport H | Alt taşma | Durum |
|---|---|---|---|---|---|---|
| `/hurda` | 1280 | Nakit (ödeme türü) | 995 | 900 | **249 px** | **Tamamen görüş dışı** |
| `/hurda` | 1024 | Nakit | — | 820 | **407 px** | **Tamamen görüş dışı** |
| `/isletme` | 1280 | Kur kaynağı seç | 935 | 900 | **223 px** | **Tamamen görüş dışı** |
| `/isletme` | 1280 | Maliyet yöntemi seç | 1030 | 900 | **318 px** | **Tamamen görüş dışı** |
| `/isletme` | 1024 | Profesyonel | — | 820 | 136 px | **Tamamen görüş dışı** |
| `/isletme` | 1024 | Kur kaynağı seç | — | 820 | **376 px** | **Tamamen görüş dışı** |
| `/isletme` | 1024 | Maliyet yöntemi seç | — | 820 | **471 px** | **Tamamen görüş dışı** |
| `/islemler` | 1280 | Ürün seçin… | 697 | 900 | 170 px | Kısmi (alt kısmı görünmüyor) |
| `/islemler` | 1024 | Ürün seçin… | — | 820 | 324 px | Kısmi |
| `/isletme` | 1024 | Başlangıç | — | 820 | 115 px | Kısmi |
| `/denetim-izi` | 1024 | Tüm kayıt türleri | — | 820 | 54 px | Kısmi |

**Şiddet değerlendirmesi (doğrulandı):** panel açıkken sayfa hâlâ kaydırılabiliyor (`body overflow: auto hidden`) ve panel sayfayla birlikte kayıyor — yani liste **erişilemez değil**, ama tıklama anında **hiç görünmüyor** ve otomatik olarak görüş alanına getirilmiyor. Kullanıcı açısından "seçiciye bastım, bir şey olmadı" deneyimi.

### 8.3 Ölçülen diğer kesişimler (normal örtme)

`/isletme` Başlangıç 105.240 px² · `/gider` (etiketsiz "—" seçici) 100.050 ve 53.520 px² · `/islemler` Cari seçin 83.952 px² · `/isletme` Profesyonel 67.280 px² · `/hurda` Nakit 55.322 px² · `/isletme` Kur kaynağı 48.705 px² · `/islemler` Nakit 38.645 px² · `/islemler` Ürün seçin 21.467 px² · `/denetim-izi` Tüm eylemler 10.131 px² · `/kur` Altınkaynak 3.840 px². Bunların tamamı "aşağıdaki içeriği örtme" tipidir; **aynı satır kesişimi hiçbirinde yok.**

Yan bulgu: **`/gider` sayfasındaki iki seçicinin görünen etiketi "—" (uzun tire).** Seçicinin ne seçtiği okunmuyor.

---

## 9. Kök neden gruplaması — tek dokunuşla en çok ekranı düzeltecek olan başta

| Sıra | Kök | Nerede | Etkilenen ekran | Ölçülen | Tek dokunuş etkisi |
|---|---|---|---|---|---|
| **K1** | **Üst bar sağ grubu 1024 px'te viewport'u aşıyor** | `header.ust-bar > div.ust-sag` (içinde `div#ust-mod-anahtar`) | **43 rotanın TAMAMI** (1024 px) | 91-160 px yatay kayma, `scrollX=124,5` ile kanıtlı | **En yüksek. Tek CSS kuralı 43 ekranı birden düzeltir.** |
| **K2** | **Form satırı etiketin üstüne göre hizalanıyor, kontrolün altına göre değil** | `div.satir`, `div.form-grid-2`, `div.form-grid-3`, `fieldset` — alan hücresi `label + kontrol` dikey yığını, grid `align-items` başlangıca hizalı | **6 ekran / 8 satır kalıbı** (islemler, cariler-yeni, cariler-düzenle ×2, evraklar, urunler-etiketler, emanet) | 10,2 - 41 px | **Yüksek.** 28 kesin hiza kusurunun **tamamı** bu tek kalıptan. Çözüm: satır kapsayıcısına `align-items: end` ya da etiket bloğuna sabit `min-height`. Tetikleyici: `(isteğe bağlı)` ekinin ikinci satıra sarması. |
| **K3** | **Dokunma hedefleri 44 px altında** | Kabuk düğmeleri (Çıkış/Basit/Profesyonel/Menü/⟳) + gövde `.dugme` 29-35 px | **43 rota** (375 px) | 443/455 = %97,4 | **Yüksek.** 5 kabuk düğmesi her ekranda tekrar ediyor; gövde için tek `min-height` tokeni. |
| **K4** | **Düğme yüksekliği ve kart yarıçapı tek ölçekten gelmiyor** | `.dugme`, `.dugme-kucuk`, `.dugme-ikincil`, `.kart`, `.stat-kart` | Neredeyse tüm ekranlar | 15 farklı düğme yüksekliği (22-71 px), 5 farklı kart yarıçapı | **Orta-yüksek.** Token seti tanımlamak tüm ekranları toparlar. |
| **K5** | **Tablolar 375 px'te içeriği ekranın dışına taşıyor** | `table.veri-tablosu` / `.tablo-sar` | 14+ ekran (375 px), ayrıca `/raporlar/islem-defteri` @1280 | 91-1074 px aşım; `/hurda`'da `main.ana-icerik`'in kendisi 58 px | **Orta-yüksek.** Tek tablo bileşeni düzeltmesi. |
| **K6** | **Açılır seçici paneli görüş alanı kontrolü yapmıyor** | `.secici-liste-kap` (`position: absolute`, tetiğin altına sabit yerleşim) | `/hurda`, `/isletme` (3 seçici), `/islemler`, `/denetim-izi` — 1024 ve 1280 px | 54-471 px alt taşma; 5 vakada panel tamamen katlamanın altında | **Orta.** Tek bileşene "yer yoksa yukarı aç (flip) + scrollIntoView" eklemek tüm seçicileri düzeltir. |
| **K7** | **Girdi değerini altında tekrar eden önizleme etiketi** | `span#*-onizleme` kalıbı: `islem-birimfiyat-onizleme`, `hrd-fiyat-onizleme`, `duz-limit-onizleme`, `d-iscilik-onizleme`, `fl-carpan-onizleme` | **5 ekran** | Girdi ile 28-39 px altındaki metin aynı; `/fiyatlandirma`'da birebir aynı | **Orta.** Tek kalıp, 5 ekran. Kural: önizleme yalnızca **biçim değiştiriyorsa** gösterilsin, birebir aynıysa gizlensin. |
| **K8** | **İki farklı kontrol yüksekliği (metin kutusu 34,5 px / seçici düğmesi 32 px)** | `input.girdi` ↔ `button.secici-dugme` dolgu/satır yüksekliği | Çekmece formları: cariler, urunler, faturalar (4 genişlikte) | 2,5 px (fatura çekmecesinde 4,5 px) | **Orta.** Tek token düzeltmesi 31 yükseklik kaydını kapatır. |

**Kapsam dışı ama eskale edilmeli:** `/vade-takvimi` 4 genişlikte de boş içerik döndürüyor (tasarım kusuru değil, eksik/bozuk sayfa).

---

## 10. Ölçüm betiği

Aşağıdaki JS, sonraki turlarda ve `dotnet/denetim.sh` sayacında aynısıyla koşulmak üzere verilmiştir. Tarayıcı konsolunda (oturum açılmış hâlde) çalıştırılır.

**Dosya yolları**
- Rapor: `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari/TASARIM-DENETIMI-2026-09-21.md`
- Ham veri: `/private/tmp/claude-501/-Users-ramazanakkocan-Desktop-ByGatech-projeler-jewellery/f8597a15-bda0-4765-a57f-77f2b6380580/scratchpad/tasarim-denetimi.json`

### 10.1 Güvenilir bekleme (bu olmadan ölçüm YANLIŞ çıkar)

```js
// fetch sayaci - gezinme sonrasi veri yuklemesini beklemek icin sart
(function(){ if(window.__fh) return; window.__fh=true; window.__aktif=0; window.__sonBitis=performance.now();
  const of=window.fetch.bind(window);
  window.fetch=function(){ window.__aktif++;
    return of.apply(null,arguments).then(r=>{window.__aktif--; window.__sonBitis=performance.now(); return r;},
                                          e=>{window.__aktif--; window.__sonBitis=performance.now(); throw e;}); };
})();

// Gizli sekmede setTimeout/rAF kisitlanir; MessageChannel kisitlanmaz.
window.__tick=()=>new Promise(r=>{const c=new MessageChannel(); c.port1.onmessage=()=>r(); c.port2.postMessage(0);});
window.__uyu=async function(ms){ const t0=performance.now(); while(performance.now()-t0<ms){ await window.__tick(); } };

window.__bekle=async function(maks){ maks=maks||7000; const t0=performance.now(); await window.__uyu(220);
  while(performance.now()-t0<maks){
    if(window.__aktif===0 && performance.now()-window.__sonBitis>380 && performance.now()-t0>550) break;
    await window.__uyu(90);
  }
  let onceki='',sabit=0; const t1=performance.now();
  while(performance.now()-t1<1800){ await window.__uyu(110);
    const m=document.querySelector('main.ana-icerik')||document.body;
    const t=(m.innerText||'').length+'|'+m.querySelectorAll('*').length;
    if(t===onceki){ sabit++; if(sabit>=2) break; } else { sabit=0; onceki=t; }
  }
  await window.__uyu(110); };

window.__git=async function(p){
  if(document.querySelector('.secici-perde')) document.querySelector('.secici-perde').click();
  if(location.pathname===p && p!=='/') { Blazor.navigateTo('/'); await window.__bekle(3000); }
  Blazor.navigateTo(p);
  const t0=performance.now(); while(location.pathname!==p && performance.now()-t0<3500){ await window.__uyu(50); }
  window.scrollTo(0,0); await window.__bekle(); return location.pathname; };
```

### 10.2 Yardımcılar

```js
window.__kis=t=>(t||'').replace(/\s+/g,' ').trim().slice(0,70);
window.__yol=function(el){ if(!el) return ''; const p=[]; let n=el,d=0;
  while(n && n.nodeType===1 && d<4){ let s=n.tagName.toLowerCase();
    if(n.id){ p.unshift(s+'#'+n.id); break; }
    if(typeof n.className==='string' && n.className.trim()) s+='.'+n.className.trim().split(/\s+/).slice(0,2).join('.');
    p.unshift(s); n=n.parentElement; d++; }
  return p.join('>'); };
window.__gor=function(el){ const r=el.getBoundingClientRect(); if(r.width<=0||r.height<=0) return false;
  const st=getComputedStyle(el); return st.visibility!=='hidden' && st.display!=='none' && st.opacity!=='0'; };
window.__etiket=function(el){ let t='';
  try{ if(el.id){ const l=document.querySelector('label[for="'+CSS.escape(el.id)+'"]'); if(l) t=l.innerText; } }catch(e){}
  if(!t){ const l=el.closest('label'); if(l) t=l.innerText; }
  if(!t){ let p=el.parentElement; for(let i=0;i<3&&p;i++){ const l=p.querySelector('label'); if(l){ t=l.innerText; break; } p=p.parentElement; } }
  if(!t) t=el.getAttribute('aria-label')||el.getAttribute('placeholder')||el.getAttribute('name')||'';
  if(!t) t=window.__kis(el.innerText||'')||el.tagName.toLowerCase();
  return window.__kis(t); };
// En yakin ortak ata + uzaklik (kademe A/B ayrimi icin)
window.__lca=function(a,b){ const A=[]; let n=a; while(n){A.push(n); n=n.parentElement;}
  let m=b,db=0; while(m){ const i=A.indexOf(m); if(i>=0) return {el:m, d:Math.max(i,db)}; m=m.parentElement; db++; } return null; };
window.__bolge=function(el){ if(el.closest('main')) return 'main';
  if(el.closest('header,.ust-bar')) return 'ustbar';
  if(el.closest('nav,aside,.kenar-cubugu')) return 'kenar'; return 'diger'; };
window.__kesisim=function(a,b){ const x=Math.max(0,Math.min(a.right,b.right)-Math.max(a.left,b.left));
  const y=Math.max(0,Math.min(a.bottom,b.bottom)-Math.max(a.top,b.top)); return Math.round(x*y); };
```

### 10.3 Asıl ölçüm — `__olc(sayfa)`

```js
window.__olc = function(sayfa){
  const gor=window.__gor, yol=window.__yol, et=window.__etiket, kis=window.__kis;
  const main=document.querySelector('main.ana-icerik')||document.body;
  const D={sayfa, gen:innerWidth, hiza:[], yukseklik:[], kesik:[], kesikGirdi:[],
           tasma:{}, mukerrer:[], olcek:{}, dokunma:{}, sayilar:{}};
  const HARIC='checkbox,radio,range,file,hidden,submit,image,color';

  // --- 1 + 2: SATIR HIZASI ve YUKSEKLIK ---
  const kont=Array.from(document.querySelectorAll('input,select,textarea,button[role=combobox],[role=combobox]'))
    .filter(e=>!(e.tagName==='INPUT' && HARIC.split(',').includes((e.type||'').toLowerCase())))
    .filter(gor);
  const items=kont.map(el=>{ const r=el.getBoundingClientRect();
    return {el, t:Math.round((r.top+scrollY)*10)/10, b:r.bottom+scrollY, l:r.left+scrollX,
            h:Math.round(r.height*10)/10, bolge:window.__bolge(el)}; })
    .sort((a,b)=>a.t-b.t||a.l-b.l);
  const gruplar=[];                                  // satir = ayni bolge + dikey ortusme + |dtop|<=40
  items.forEach(it=>{ let h=null;
    for(const g of gruplar){ if(g.bolge!==it.bolge) continue;
      if(g.items.some(o=>Math.abs(o.t-it.t)<=40 && o.b>it.t+1 && o.t<it.b-1)){ h=g; break; } }
    if(h) h.items.push(it); else gruplar.push({bolge:it.bolge, items:[it]}); });
  gruplar.filter(g=>g.items.length>1).forEach(g=>{
    let anc=g.items[0].el, derin=0;
    for(let i=1;i<g.items.length;i++){ const L=window.__lca(anc,g.items[i].el); if(!L) return; anc=L.el; derin=Math.max(derin,L.d); }
    const tops=g.items.map(x=>x.t), hs=g.items.map(x=>x.h);
    const fh=Math.round((Math.max(...tops)-Math.min(...tops))*10)/10;   // HIZA FARKI
    const fy=Math.round((Math.max(...hs)-Math.min(...hs))*10)/10;       // YUKSEKLIK FARKI
    const ort={kapsayici:yol(anc), derinlik:derin, kademe:(derin<=4?'A':'B'),
               bolge:g.bolge, alanlar:g.items.map(x=>et(x.el)), y:Math.round(g.items[0].t)};
    if(fh>2) D.hiza.push({...ort, fark:fh, esik:2, toplar:tops});
    if(fy>2) D.yukseklik.push({...ort, fark:fy, esik:2, yukseklikler:hs});
  });

  // --- 3: KESIK METIN (label.gorsel-gizli = 1x1px a11y etiketi, YANLIS POZITIF) ---
  const kesikSec='label,button,th,a,.rozet,.etiket,.stat-etiket,.stat-deger,.deger,[role=combobox],.sekme';
  Array.from(main.querySelectorAll(kesikSec)).filter(gor).forEach(e=>{
    if(e.scrollWidth>e.clientWidth+1 && e.clientWidth>0)
      D.kesik.push({sec:yol(e), metin:kis(e.innerText), scrollW:e.scrollWidth, clientW:e.clientWidth,
                    fark:e.scrollWidth-e.clientWidth,
                    yanlisPozitif:(e.classList.contains('gorsel-gizli')||e.clientWidth<=2)});
  });
  Array.from(document.querySelectorAll('input,textarea,select')).filter(gor).forEach(e=>{
    if(e.scrollWidth>e.clientWidth+1 && e.clientWidth>0)
      D.kesikGirdi.push({sec:yol(e), etiket:et(e), scrollW:e.scrollWidth, clientW:e.clientWidth}); });

  // --- 4: TASMA ---
  D.tasma.main  = main ? main.scrollWidth-main.clientWidth : null;
  D.tasma.belge = document.documentElement.scrollWidth-document.documentElement.clientWidth;  // <- K1 burada yakalanir
  D.tasma.asanlar=[];
  Array.from(document.querySelectorAll('body *')).forEach(e=>{           // NOT: 'main *' degil 'body *'
    if(D.tasma.asanlar.length>=12 || !gor(e)) return;
    const r=e.getBoundingClientRect();
    if(r.right>innerWidth+1 && r.width<innerWidth*4){
      const p=e.parentElement; if(p && p.getBoundingClientRect().right>innerWidth+1) return;
      D.tasma.asanlar.push({sec:yol(e), asim:Math.round(r.right-innerWidth), metin:kis(e.innerText)}); } });

  // --- 5: MUKERRER BILGI (girdi degeri hemen altinda tekrar ediyor mu) ---
  const sayi=s=>(s||'').replace(/[^0-9]/g,'');
  const yapraklar=Array.from(main.querySelectorAll('div,span,small,p,strong,em,label,td'))
    .filter(e=>e.children.length===0).filter(gor);
  Array.from(main.querySelectorAll('input')).filter(gor).forEach(i=>{
    const v=sayi(i.value); if(v.length<3) return; const r=i.getBoundingClientRect();
    yapraklar.forEach(e=>{ const er=e.getBoundingClientRect();
      if(er.top<r.top-12||er.top>r.bottom+150||Math.abs(er.left-r.left)>340) return;
      const tn=sayi(e.innerText);
      if(tn.length>=3 && tn.indexOf(v)>=0)
        D.mukerrer.push({tip:'girdi-alti-tekrar', alan:et(i), deger:kis(i.value),
                         tekrar:kis(e.innerText), sec:yol(e), dy:Math.round(er.top-r.top)}); }); });

  // --- 6: OLCEK TUTARSIZLIGI ---
  const hist=(l,f)=>{ const h={}; l.forEach(e=>{ const v=f(e); h[v]=(h[v]||0)+1; }); return h; };
  const btn=Array.from(main.querySelectorAll('button,a.dugme,.dugme')).filter(gor).filter(e=>kis(e.innerText).length>0);
  const inp=Array.from(main.querySelectorAll('input,select'))
    .filter(e=>!(e.tagName==='INPUT'&&HARIC.split(',').includes((e.type||'').toLowerCase()))).filter(gor);
  D.olcek.dugmeYukseklik=hist(btn,e=>Math.round(e.getBoundingClientRect().height));
  D.olcek.girdiYukseklik=hist(inp,e=>Math.round(e.getBoundingClientRect().height));
  D.olcek.dugmeYaricap  =hist(btn,e=>getComputedStyle(e).borderRadius);
  D.olcek.kartYaricap   =hist(Array.from(main.querySelectorAll('[class*=kart],[class*=panel],[class*=kutu]')).filter(gor),
                              e=>getComputedStyle(e).borderRadius);

  // --- 7: DOKUNMA HEDEFI (375pxte kenar cubugu ekran disinda -> EKRAN ICI ile sinirla) ---
  const tik=Array.from(document.querySelectorAll(
    'a,button,select,summary,[role=button],[role=combobox],input[type=checkbox],input[type=radio]')).filter(gor);
  const ic=tik.filter(e=>{const r=e.getBoundingClientRect(); return r.right>0 && r.left<innerWidth;});
  const kucuk=ic.map(e=>({e,r:e.getBoundingClientRect()})).filter(x=>x.r.height<44||x.r.width<44);
  D.dokunma={domdaki:tik.length, ekranIci:ic.length, kucuk:kucuk.length,
             ornek:kucuk.slice(0,8).map(x=>({sec:yol(x.e), metin:kis(x.e.innerText),
                                             w:Math.round(x.r.width), h:Math.round(x.r.height)}))};

  D.sayilar={kontrol:kont.length,
             hiza:D.hiza.length, hizaA:D.hiza.filter(x=>x.kademe==='A').length,
             yukseklik:D.yukseklik.length,
             kesik:D.kesik.filter(x=>!x.yanlisPozitif).length, kesikGirdi:D.kesikGirdi.length,
             mukerrer:D.mukerrer.length, tasmaMain:D.tasma.main, tasmaBelge:D.tasma.belge,
             asan:D.tasma.asanlar.length, dokunmaEkranIci:D.dokunma.ekranIci, dokunma44Alti:D.dokunma.kucuk};
  D.bos=(main.innerText||'').trim().length<40;   // "ACILMADI / BOS-ICERIK" tespiti
  return D;
};
```

### 10.4 Açılır panel ölçümü (§ 8)

```js
window.__panelKapat=async function(){ const p=document.querySelector('.secici-perde');
  if(p){ p.click(); await window.__uyu(250); }
  if(document.querySelector('.secici-liste-kap')){
    document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true})); await window.__uyu(250); } };

window.__panel=async function(sayfa){
  const sonuc=[]; await window.__git(sayfa);
  const sec='button.secici-dugme,button[role=combobox],[role=combobox],button[aria-haspopup=listbox],button[class*=secici]';
  const n=Array.from(document.querySelectorAll(sec)).filter(window.__gor).length;
  for(let i=0;i<Math.min(n,6);i++){
    await window.__panelKapat();
    const b=Array.from(document.querySelectorAll(sec)).filter(window.__gor)[i]; if(!b) continue;
    const bt=window.__kis(b.innerText), br=b.getBoundingClientRect();
    b.click(); await window.__uyu(550);
    const panel=document.querySelector('.secici-liste-kap');
    if(!panel || !window.__gor(panel)){ sonuc.push({tetik:bt, durum:(b.disabled?'devre-disi':'panel-acilmadi')}); continue; }
    const pr=panel.getBoundingClientRect();
    const ortus=Array.from(document.querySelectorAll('main input,main select,main textarea,main button,main label,main .girdi'))
      .filter(window.__gor).filter(e=>!panel.contains(e) && e!==b)
      .map(e=>{ const er=e.getBoundingClientRect();
        return {sec:window.__yol(e), et:window.__kis(e.innerText||window.__etiket(e)), a:window.__kesisim(pr,er),
                ayniSatir:(Math.abs(er.top-br.top)<=40 && er.bottom>br.top+1 && er.top<br.bottom-1)}; })
      .filter(x=>x.a>0).sort((a,b2)=>b2.a-a.a);
    sonuc.push({tetik:bt, panel:[pr.left,pr.top,pr.width,pr.height].map(Math.round),
      toplamPx2:ortus.reduce((s,x)=>s+x.a,0),
      ayniSatirPx2:ortus.filter(x=>x.ayniSatir).reduce((s,x)=>s+x.a,0),   // <- GERCEK kusur olcusu
      altTasmaPx:Math.round(Math.max(0,pr.bottom-innerHeight)),
      sagTasmaPx:Math.round(Math.max(0,pr.right-innerWidth)),
      tamamenGorusDisi:(pr.top>=innerHeight),                              // <- GERCEK kusur olcusu
      enBuyuk:ortus.slice(0,3)});
  }
  await window.__panelKapat(); return sonuc; };
```

### 10.5 `denetim.sh` için önerilen sayaçlar (fail-closed eşikler)

| Sayaç | Bugünkü değer | Önerilen eşik |
|---|---|---|
| `hizaKademeA` toplamı | 28 | **0** |
| `tasmaBelge` (1024 px'te sıfırdan büyük sayfa) | 67 | **0** |
| `mukerrer.girdiAltiTekrar` (ayrı alan) | 8 | **0** |
| `panel.tamamenGorusDisi` | 5 | **0** |
| `olcek.dugmeFarkliYukseklikSayisi` (sistem geneli) | 15 | **≤ 3** |
| `dokunma44Alti / ekranIci` @375 | %97,4 | **≤ %10** |
| `kesikGirdi` (gerçek) | 2 alan | **0** |
| `BOS-ICERIK` sayfa | 1 (`/vade-takvimi`) | **0** |

---

*Denetim 21 Eylül 2026, 21:04 yayını (dalga 6b) üzerinde yapılmıştır. Kaynak kod okunmadı; tüm bulgular canlı DOM ölçümüdür.*
