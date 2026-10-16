# PAKET OLCEK-1: Tek ölçek (mod ölçü değiştirmez). Uygulama raporu v17

**Tarih:** 2026-09-29 05:30 – 07:10 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9950–9963). 5198, 5199 ve 5200'e ve tarayıcı bölmesine dokunulmadı.
**v17 neden var:** Son doğrulamada tasarım hakemi **YÜKSEK** bir bulgu verdi. 960 px'te (13b) yapışık eylem sütunu Müşteriler listesindeki tutarların üstünü örtüyordu. K9 = 6 iki modda da ölçülüyordu; PRO'da "-3.840 ₺ Borç" "Bor" diye kesiliyordu, BASİT'te "0,0000 has" "0,0000 h" diye. v16 bunu "aracın yanlış pozitifi" diye gerekçelendirmişti; hakem bu gerekçeyi kabul etmedi.
**Devralma:** Ağaç v16 ile aynıydı (olcek.css fc9052c8…, belirtecler.css 949ac1c0…, OlcekTekTestleri.cs 7faea4e1…). Yarım mutasyon yoktu. Başta kiracı sayısı **14**, PAKET-OLCEK kiracısı **0**.
**Dokunulan sayfalar: yok.** Razor'a dokunulmadı. Çözüm yalnız olcek.css'te.

## v17-0. Bulgunun kapanışı: 960'ta eylem sütunu artık hiçbir yazıyı örtmüyor

**Kök neden (ölçüldü, `kol2.mjs` 960):** 960 px'te kabın genişliği 642 px. BASİT tablo 665 px'ti (ad 182 · telefon 112 · borç 89 · has 88 · eylem 195). PRO tablo 896 px'ti (ad 182 · tür 74 · telefon 127 · TL 89 · has 88 · döviz 92 · durum 54 · eylem 191). PRO'nun yalnız veri sütunları 706 px ediyor; eylemler çıkarılsa bile 642'ye sığmıyor. Sütun gizlemek bilgi siler, bu yüzden yasak. Tahsilat'ı "⋯" menüsüne taşımak için Cariler.razor ya da SatirEylemleri'ne dokunmak gerekirdi; ikisi de başka şeridin dosyası. O yol CSS ile yapılamaz.

**Çözüm (olcek.css (13e), GENİŞLİK kuralı, iki modda aynı):** 769–1099 px'te Müşteriler (`#cari-liste-sar`) ve Ürünler (`#urun-liste-sar`) listeleri ızgaraya çevrildi:
- Tablo `display: grid` oldu; `thead`, `tbody` ve `tr` `display: contents` aldı. Sütun sayısı başlıktan okunuyor (`th:first-child:nth-last-child(N)` → `--eylem-alti-sutun`).
- Satır eylemleri ([Tahsilat|Stok] · Düzenle · ⋯) `grid-column: 1 / -1` ile veri satırının **altında, kendi satırında**, adın hizasında duruyor.
- Yapışma yok (`position: static`), bu yüzden hiçbir yazının üstüne binme yok. Düğmeler hep kabın solunda, yani ekranda.
- Kayıt ayracı eylem satırının altında. Veri ile eylem arasında çizgi yok; iki satır tek kayıt olarak okunuyor.
- Satır boyu en az `--tablo-satir-boyu` (61). Başlık zemini ve satır vurgusu iç gölgeyle, belirteçten çiziliyor (OL6: background yazılmaz).
- Eylem başlığı yalnız ekran okuyucu için (`clip-path: inset(50%)`); ızgarada sütun açmıyor.

**Taşma işareti (13f):** PRO'da veri sütunları kaba yine de sığmıyor (/cariler 705 > 642, /urunler 795 > 642). Bu durumda tablo kendi kabında kayıyor, sayfa yatay kaydırması 0. Kabın sağ kenarı 2rem boyunca **soluyor**; sona kaydırılınca solma kalkıyor. Bu, `animation-timeline: scroll(self inline)` ile yapıldı. Kap kaymıyorsa zaman çizelgesi etkin olmuyor ve maske uygulanmıyor: canlıda BASİT maske `none`, PRO başta gradyan, sonda `none` (`maske.mjs`). Maske yalnız saydamlık taşıyor, renk taşımıyor.

**Kabul ölçümü (960, başsız Chrome, olcek.css AÇIK):**

| Ölçüm | BASİT | PRO | v16 (önce) |
|---|---|---|---|
| /cariler tablo/kap | **642/642** | 705/642 (kayar, solma var) | 665/642 · 896/642 |
| /cariler K9 binen, açık tema (`iki.mjs` + sikisma-olc.js) | **0** | **0** | 6 · 6 |
| /cariler K9 binen, koyu tema | **0** | **0** | 6 · 6 |
| /cariler K3 taşan/kırık, K10, K12 | 0 | 0 | 0 |
| /cariler ekran dışı eylem düğmesi (`tablo.mjs`) | 0 | 0 | 0 |
| /urunler tablo/kap | **642/642** | 795/642 (kayar, solma var) | 703 · 961 |
| 42 rota × 2 mod, 960, K9 (`absik.mjs`) | **0 rotada** | **0 rotada** | /cariler 6, /urunler 6 |
| 42 rota × 2 mod, 375, K9 | 0 | 0 | 0 |
| Sayfa yatay kaydırma | 0 | 0 | 0 |

Görüntüler Read ile görüldü:
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v17-cariler-960-karanlik.png` ve `-aydinlik.png`: tutarlar tam, "-3.840 ₺ / Borç" okunuyor, düğmeler altta.
- `OLCEK-1-v17-urunler-960-aydinlik.png`.
- Kayma başı ve sonu: `/private/tmp/kuyumcu-serit/OLCEK-1/v17/out/maske-960-aydinlik-Profesyonel-_cariler-bas.png` / `-son.png`. Sonda DÖVİZ BAKİYE ve DURUM tam görünüyor.

## v17-0.1 DEVİR NOTU 3 bulgularının yeniden ölçümü (güncel ağaç)
| Bulgu | Ölçüm (v17) | Durum |
|---|---|---|
| KRİTİK: PRO /cariler 1440/1280'de Düzenle kesik, ⋯ ekran dışı | 1440: 1122/1122, ekran dışı 0 · 1280: 962/962, ekran dışı 0; açık ve koyu (`son/tablo-*.txt`). Görüntü `OLCEK-1-v17-cariler-1440-aydinlik.png`: Tahsilat · Düzenle · ⋯ tam. | **Kapalı** |
| YÜKSEK: BASİT satışta 7 haneli tutar 1440'ta kesik | "1.234.567,89" görünen/içerik: 1440 137/137 · 1280 206/206 · 960 212/212 · 375 206/206; açık ve koyu, kesik yok (`olc/iki-*.txt`). Görüntü `OLCEK-1-v17-islemler-7hane-1440-aydinlik.png`. | **Kapalı** |

## v17-1. Değişen dosyalar (bu tur)
- `dotnet/Kuyumcu.Web/wwwroot/css/olcek.css`: (13e) ve (13f) eklendi, dosya sonuna, yorumlu. Yeni sha1 **489f5e21…**.
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs`:
  - Yeni test **OL29** (`OL29_v17_960ta_musteri_ve_urun_listesi_eylemleri_alt_satirda_yapisma_yok_tasma_solar`).
  - OL6'ya dar bir istisna: `@keyframes olcek-*` adımlarının seçicisi yüzde değeridir ("0%, 96%"), öğe seçmez. Renk ve değer denetimi onlara da uygulanmaya devam ediyor.
- `belirtecler.css`: değişmedi (949ac1c0…).
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v17-*.png`: 24 yan yana görüntü (cariler, urunler, islemler-7hane × 1440/1280/960/375 × açık/koyu; solda BASİT, sağda PRO).
- `git status --porcelain | grep -v -E "/(bin|obj)/"`, paketin satırları:
  - `M dotnet/Kuyumcu.Web/wwwroot/index.html`
  - `?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css`
  - `?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css`
  - `?? dotnet/Kuyumcu.Tests/` (OlcekTekTestleri.cs, OlcekYuzEnvanteri.txt)
  - `?? dotnet/denetim.sh`

## v17-2. Derleme ve testler
- `dotnet build Kuyumcu.sln --nologo`: **0 Uyarı, 0 Hata** (başta `build0.txt`, sonda `build1.txt`).
- Filtreli `FullyQualifiedName~OlcekTek`: önce 31/31, sonra **32/32** (`t3.txt`, son CSS ile). Yeni test 1 (OL29).
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pOLCEK-1/v17/_Mac_2026-09-29_05_54_29.trx`): **7009 test · 7008 geçti · 1 başarısız**.
  - Tek kırmızı: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1). Devir listesinde var, v9–v16'da da kırmızıydı.
  - **Madde (7): yeni kırmızı 0.**
  - Tam süit, OL29 eklendikten sonra koştu. Ardından yalnız bir CSS değeri değişti: veri hücresi `min-height` → `var(--tablo-satir-boyu)`. Bunu 32/32 filtreli koşum ve canlı ölçüm kapsıyor; C# kodu değişmedi.

## v17-3. Mutasyon kanıtları (yedek, fark, test ve canlı günlük: `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/v17/<kod>/`)
| Kod | Değişiklik (tek eşleşme) | Kırmızı test | Denetim | Canlı etki (mutasyon açıkken) | cmp |
|---|---|---|---|---|---|
| **M-OLCEK-1-1-v17** (spesifikasyon) | belirtecler.css PRO bloğuna `--kontrol-yuksekligi: 2rem;` | `OL1_belirteclerde_moda_ozel_blok_YALNIZ_mod_isareti_tasir`, `OL2_hicbir_css_dosyasi_moda_ozel_OLCU_yazmaz_belirtec_ya_da_dogrudan` | `✗ OLCEK-1: MOD_OLCU_BILDIRIMI 1` | `boy.mjs` 1440 /emanet: `#e-cari` 44/16px → 39/16px, **TOPLAM_FARK 1** (taban 0) | AYNI (949ac1c0) |
| **M-OLCEK-1-67-v17** | (13e) eylem hücresi `grid-column: 1 / -1` → `auto` | `OL29_v17_…` | — | `iki.mjs` 960: /cariler BASİT tablo 779 > 642, eylem sağ kenarı 1068 > kap 935; PRO 1334 > 642, 1623 > 935 → düğmeler ekran dışı | AYNI |
| **M-OLCEK-1-68-v17** | (13e) tablo `display: grid` → `table` | `OL29_v17_…` | — | `kol2.mjs` 960: tablo genişliği BASİT 9863, PRO 12375 px (düzen çöker) | AYNI |

- Test açığı yok.
- Son mutasyondan sonra temiz derleme 0/0, filtreli testler 32/32.
- Son olcek.css sha1 489f5e21… (M68 yedeğiyle `cmp` AYNI).

## v17-4. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; `v17/olc.sh` SIRALI; çıktılar `/private/tmp/kuyumcu-serit/OLCEK-1/v17/`)
**Kiracı:** `PAKET-OLCEK-1Q-TEST` (01M3NGGSV2KZNRMBX6JKSFH3FE), API ile kuruldu:
- Davet 201, kayıt 201, giriş 200.
- Cari 201 ×9, ürün 201 ×4, fatura 200 ×6 (1.234.567,89 ₺ dahil).
- `demo-tohumla.js`: rc=1. V8 sayım kontrolü kiracı betiğinin 4 stoksuz ürününde tuttu; v10–v16 ile aynı, ölçümü etkilemiyor.

Oturum parolasız açıldı (`inceleme-vekili.py` 5187). Sistem anahtarı tek kullanımlıktı, rastgele üretildi ve chmod 600'dü; değeri basılmadı. İş sonunda anahtar ve fikstür 0 bayta boşaltıldı.

### v17-4.1 Tablo ≤ kap ve ekran dışı düğme (`son/tablo-*.txt`, A/B = olcek.css açık/kapalı; açık ve koyu temada aynı)
| Genişlik | Mod | /cariler tablo/kap · dışı | /urunler | olcek KAPALI | üst çubuk | sayfa yatay |
|---|---|---|---|---|---|---|
| 1440 | BASİT | 1122/1122 · 0 | 1122/1122 · 0 | aynı | 68 | 0 |
| 1440 | PRO | 1122/1122 · 0 | 1122/1122 · 0 | aynı | 68 | 0 |
| 1280 | BASİT | 962/962 · 0 | 962/962 · 0 | aynı | 68 | 0 |
| 1280 | PRO | 962/962 · 0 | 964/962 · 0 (2 px, yapışık eylem sütunu) | /cariler 1104 | 68 | 0 |
| 960 | BASİT | **642/642 · 0** | **642/642 · 0** | 907 · 692 | 90 (kapalı 93) | 0 |
| 960 | PRO | 705/642 · **0** (kayar + solma) | 795/642 · 16 (`u-stok-adet-*`, v16 ile aynı) | 1104 · 961 | 90 (kapalı 125) | 0 |
| 375 | BASİT | 349/349 · 0 (kart) | kayar (64, v16 ile aynı) | — | — | 0 |
| 375 | PRO | kayar (64, v16 ile aynı) | kayar (64) | — | — | 0 |

### v17-4.2 Sıkışma (`iki.mjs` + `sikisma-olc.js`; /cariler ve /islemler × 2 mod × 1440/1280/960/375 × açık/koyu = 32 ölçüm)
- K1, K3, K5, K9, K10, K11 ve K12: hepsi **0**. (v16'da 960 BASİT /cariler K9 6 idi.)
- Sıfırdan farklı tek değer: **K7 = 1**, 1280 BASİT /islemler. Bu SATIS-3 ödeme yığını, v13'ten beri var.
- Sayfa yatay kaydırma **0**.

### v17-4.3 42 rota A/B (`absik.mjs`; 960 ve 375; 2 mod; 168 + 168 ölçüm)
- Olcek açıkken **K9 hiçbir rotada sıfırdan farklı değil** (960 ve 375).
- Açıkken kapalıya göre artan sayaçlar yalnız şunlar; v16'da `satirk3.mjs` ile incelendi ve karışık yazı boyunda aynı satır olduğu gösterildi:
  - /ayarlar K12 4 → 7: tema kartı.
  - /yetkiler K3 "Sahip (Tam yetkili)" 0 → 1.
  - 375'te /kullanicilar "Hakem Tasarim (siz)".
- v16'daki K9 artışları (BASİT /cariler 6, PRO /urunler 6) **kalktı**.

### v17-4.4 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 42 rota)
- **1440** (`son/tut/w1440`): 8 satır.
  - /cariler "—" kalınlığı: v16'daki anahtar çakışması, farklı eleman.
  - /faturalar 7 hücre, satır 61 → 80: PRO'daki fazladan sütunlar yüzünden uzun ad sarıyor. ZO-BELGE; v16'da da açık kalanlarda.
- **960** (`son/tut/w960`, sonra düzeltmeden sonra `son/tut2/w960`):
  - İlk koşumda /cariler hücre boyu 50 ↔ 60 çıktı. Kaynağı benim ilk min-height değerimdi (34 + 16). `var(--tablo-satir-boyu)` yapıldı ve yeniden ölçüldü: /cariler'de yalnız "—" kaldı.
  - /urunler'de PRO adı 182 px'lik sütunda 3 satıra sarıyor (61 ↔ 73). BASİT aynı adı 290 px'te tek satırda gösteriyor, çünkü PRO'nun fazladan barkod, grup ve durum sütunları adı daraltıyor. Bu içerik farkı, ölçü farkı değil: yazı, dolgu ve kalınlık aynı.
  - /faturalar ve /fiyatlandirma: v16'daki gerekçeli içerik farkları.
- **Düğme ailesi (960 ve 1440):** iki modda aynı dağılım, **3 boy (34 · 44 · 52)**. İstisna: şerit `btn-kucuk` 44 ×2 ve 34 ×1 (DUGME-2 devri).

### v17-4.5 Hakem senaryosu (`sen.mjs`)
- 1440, 1280, 960 ve 375; açık ve koyu. Adımlar: dolu fiş, ödeme böl/sil, kaydet hatası, müşteri çekmecesi, Emanet al, kâr-zarar.
- kesik 0 · 44 altı dokunma hedefi 0 · yatay 0. Emanet boyları iki modda 34/44/52 (375'te 44/52).
- Kalan FARK satırları:
  - `#islem-cari` sağ dolgu 44 ↔ 258/332 (SATIS-3).
  - 960 fiş kalem satırı 61 ↔ 75 (SATIS-3).
  - "—" kalınlığı (araç anahtarı).
- Bu turun değişikliğinden gelen yeni fark yok.

### v17-4.6 Ekran görüntüleri (Read ile GÖRÜLDÜ)
- `OLCEK-1-v17-cariler-960-karanlik.png`: iki mod; tutarlar ve rozetler tam, eylemler altta.
- `OLCEK-1-v17-urunler-960-aydinlik.png`.
- `OLCEK-1-v17-cariler-1440-aydinlik.png`: PRO satırı tam, ⋯ görünür.
- `OLCEK-1-v17-islemler-7hane-1440-aydinlik.png`: 1.234.567,89 tam.
- `v17/out/maske-960-aydinlik-Profesyonel-_cariler-bas.png` / `-son.png`: solma başta var, sonda yok.
- `v17/out/kol-960-aydinlik-Basit.png` ve `kol-960-karanlik-Profesyonel.png` (Ürünler).

## v17-5. Denetim
- İlk koşum (`den1.txt`, 05:5x): **DENETİM TEMİZ.**
- Son koşum (`den2.txt`, 07:0x): OLCEK-1 sayaçları ✓ (MOD_OLCU_BILDIRIMI 0 · YUZ_ONEMLI_OLCU 0 · YUZ_OLCU_ENVANTER_DISI 0). Tek ✗ var: `P-39: TABLO_SARMALAYICISIZ 1`.
  - Kaynağı `Pages/IslemDefteri.razor` satır 306 (`<table class="veri-tablosu" id="id-yazdir-tablo">`, sarmalayıcısız).
  - Dosya 06:51'de değişmiş; bu ZO-BELGE şeridinin alanı. Ben dokunmadım. OLCEK-1'in değil.

## v17-6. DoD sözleşmesi
- Yeni varlık, uç ya da migration yok (gerekmedi). Bu paket yalnız görünüm ve ölçü düzenliyor.
- Kaydet, POST, yetki ve yasal kapılar değişmedi. DOM id'leri değişmedi, düğmeler aynı `SatirEylemleri` çıktısı.

## v17-7. Temizlik
- API 5186 ve vekil 5187 `kill` ile durduruldu; `lsof` 5186/5187 **boş**. v17 Chrome süreçleri **0**.
- **Kiracı:** `zsh kiraci-temizle.sh 01M3NGGSV2KZNRMBX6JKSFH3FE --uygula` rc=0.
  - information_schema'daki **70** `tenant_id` tablosunda kalan satır **0**.
  - Kiracı sayısı başta **14**, sonda **14**. PAKET-OLCEK kiracısı 0.
  - QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- Anahtar ve fikstür 0 bayt.
- **DİSK (yapılamadı, kurucuya):** `rm -rf` komutunu TurkReport kancası blokladı. Kurucunun terminalde silmesi gereken:
  - `/private/tmp/kuyumcu-serit/OLCEK-1` (9,2 GB; v17 4,6 GB, çoğu Chrome profili).
  - `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/v17`.
- Kopya ağaç yok. Proje ağacına geçici dosya bırakılmadı.

## v17-8. Açık kalanlar (sahibiyle)
1. **KATMAN (SatirEylemleri / Cariler / Urunler):**
   - <1100'de Tahsilat'ın "⋯" menüsüne alınması ya da ince çizgi ikonlu eylemler gelirse (13e) eylem satırı tek satıra dönebilir.
   - PRO /urunler 960 `u-stok-adet-*` ×16 kayan bölgede; aynı iş eylem satırındaki "Stok" düğmesiyle yapılabiliyor.
   - 375 PRO eylemleri kayan tabloda.
2. **ZO-BELGE:**
   - `IslemDefteri.razor:306` sarmalayıcısız tablo (P-39 ✗, 06:51).
   - /faturalar 1440 PRO satırı 80 (uzun ad sarıyor).
3. **SATIS-3:** `#islem-cari` sağ dolgu · 960 fiş kalem 61↔75 · 1280 BASİT K7 1.
4. **ORTAK-1:** tam süitte `OrtakGorunumSaflik.O8` kırmızı.
5. **DUGME-2:** şerit `btn-kucuk` 44 ×2 (/faturalar, /kur) ve 34 ×1 (/urunler/agac).
6. **Orkestratör (ölçüm aracı):** `tutarlilik.mjs` "—" anahtar çakışması; `sikisma-olc.js` K3'ün karışık yazı boyunu iki satır sayması (v16 § 8.7).
7. **Kurucu:**
   - `/private/tmp/kuyumcu-serit/OLCEK-1` silinmeli.
   - `dotnet/denetim.sh.mutbak` (0 bayt) silinmeli.
   - Kenar menüsü PRO yüzü kararı.

## v17-9. Kurucuya not
- Tablet genişliğinde (≈ 960 px) Müşteriler ve Ürünler listelerinde satır düğmeleri (Tahsilat/Stok · Düzenle · ⋯) artık satırın altında, adın hizasında duruyor. Hiçbir tutarın üstünü örtmüyorlar; iki modda da aynı.
- Profesyonel modda sütunlar sığmazsa liste kendi içinde yana kayıyor, sağ kenar hafifçe solarak "devamı var" diyor.
- Bilgisayar genişliğinde (1280 ve üstü) liste tek satırda, değişmedi.
- Satışta 7 haneli tutar her genişlikte tam okunuyor.

---

# Ek: v16 raporu (değiştirilmeden)

# PAKET OLCEK-1: Tek ölçek (mod ölçü değiştirmez). Uygulama raporu v16

**Tarih:** 2026-09-29 02:55 – 04:40 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9960–9966; 5198/5199/5200'e ve tarayıcı bölmesine dokunulmadı)
**v16 neden var:** Son doğrulama GEÇTİ verdi ama çapraz hakem bir **KRİTİK çelişki** buldu: uygulayıcı v15'te "YARIM, 960 KISMEN" demişti, doğrulayıcı ise 960'ı hiç ölçmeden "GEÇTİ" demişti. Hakemin istediği: sayıyı yeniden ölçmek ve iki raporun aynı gerçeği yazmasını sağlamak. Ayrıca 4 DÜŞÜK bulgu vardı; dördü de başka şeritlere ait ya da kabul edilebilir.
**Devralma:** Ağaç v15 ile aynıydı (olcek.css sha1 fc9052c8…, belirtecler.css 949ac1c0…, OlcekTekTestleri.cs 7faea4e1…). Yarım mutasyon yoktu. 5186 ve 5187 boştu. Başlangıçta kiracı sayısı **14**, PAKET-OLCEK kiracısı **0**.
**Bu turda kod değişmedi.** olcek.css, belirtecler.css ve test dosyası v15'teki hâliyle kaldı; sha1 başta ve sonda aynı. Bu tur yalnız ölçüm, mutasyon ve uzlaştırma turuydu. **Dokunulan sayfalar: yok.**

## v16-0. Çelişkinin uzlaştırılması: 960 px'te kabul (sayılar yeniden ölçüldü)

Spesifikasyonun 960 için iki şartı var:
- (a) "tutarlilik.mjs 42 rota 1440 ve 960'ta: BASİT↔PRO boy/yazı/dolgu/köşe/kalınlık farkı = 0 (yalnız modda gizlenen eleman farkı sayılmaz)"
- (b) "tur K1..K13 yeni bulgu 0 (büyüyen kutular taşma/kırılma yaratmasın — özellikle 960 ve 375)"

v15'teki "KISMEN" hükmü, spesifikasyonda olmayan bir hedefe dayanıyordu: "960'ta PRO tabloları kabına sığsın". 8–10 sütunlu PRO tabloları 642 px'lik kaba olcek.css **KAPALIYKEN de** sığmıyor. Bu yüzden o hedef bu paketin kabulü değil, ayrıca paketten önce de karşılanmıyordu. v16'da iki şart ayrı ayrı, olcek.css AÇIK ↔ KAPALI A/B ile yeniden ölçüldü. Ölçüm aynı sayfada `link.disabled` ile yapıldı; ağaçtaki dosyaya dokunulmadı.

| Şart (960) | Ölçüm | Sonuç |
|---|---|---|
| (a) tutarlılık 960, 42 rota (`son/tut/w960`) | **8 satır, üç sınıf.** (1) `/cariler td "—"` 400→600: araç anahtarı METİNDEN kuruyor. BASİT'teki "—" `td.hucre-tek-satir` (telefon sütunu, 400); PRO'daki "—" `td.hucre-vurgu.musteri-sutun-ek`, yani **yalnız PRO'da görünen DÖVİZ BAKİYE sütunu** (600). `diag.mjs` sütun indeksiyle ölçtü: BASİT tabloda "—" hücresi **0**, PRO'da hepsi sütun 5'te. Bu iki farklı eleman, üstelik biri modda gizli. (2) `/faturalar` 3 satır 53→34: `satirab.mjs` A/B'de iki mod da kart yığını; satır boyu açık 168–187 ↔ 232, kapalı 170–189 ↔ 234. Fark PRO kartındaki fazladan alanlardan (Ara toplam, KDV, Kaynak) geliyor; olcek.css'in katkısı ±2 px. (3) `/fiyatlandirma` 4 satır 164→215: A/B'de kapalıyken de 176→227. PRO fazladan içerik gösteriyor, paket kaynaklı değil. | **Karşılandı (gerekçeli, modda gizlenen eleman).** Kontrol, düğme, sekme, yazı ve dolguda mod farkı **0**. |
| (a) düğme ailesi 960 (`tutarlilik-dugme-960.txt`) | İki modda aynı dağılım. Sınıfsız 34/44; birincil ve ikincil 44; küçük 34; büyük 52. İstisna olarak şerit `btn-kucuk` 44 ×2 ve 34 ×1 (DUGME-2 devri). | **Karşılandı: 3 boy** (34 · 44 · 52). |
| (b) yeni bulgu, 42 rota × 2 mod, 960 (`son/absik-960-aydinlik.txt`, 168 ölçüm, AÇIK↔KAPALI) | Açıkken kapalıya göre ARTAN sayaçlar ve karşılıkları: **K9** BASİT /cariler 6, PRO /urunler 6 · **K12** /ayarlar 7↔4 · **K3 kırık** /yetkiler 1. Bunlara karşılık, olcek.css'in 960'ta **kapattığı** bulgular: K3 kırık /cariler 3→0 (BASİT) ve 2→0 (PRO); ekran dışı satır eylemi 45+16+12 (BASİT) ve 45+64+12 (PRO) → 0+0+0 ve 0+16+0. | Üç "artış" tek tek incelendi (aşağıda). **Üçü de ölçüm aracının yanlış pozitifi; görünür yeni kusur 0.** |

**Üç "artışın" kanıtı:**
1. **K9 (üst üste binen yazı), 960.** `diag.mjs` ve `absik.mjs` her çift için `elementFromPoint` ile hangi yazının gerçekten GÖRÜNDÜĞÜNÜ ölçtü:
   - BASİT /cariler: çift 15, iki yazının birlikte göründüğü çift **0**.
   - PRO /urunler: çift 16, birlikte görünen **0**.
   - /faturalar: çift 12, birlikte görünen **0**.
   - Çiftlerin **15/15, 16/16 ve 12/12'sinde** bir taraf sağa yapışık eylem sütunu (`position: sticky`). Kayan hücre, opak zeminli yapışık hücrenin altında kalıyor.
   - Bu bir "donmuş sütun" örtmesi; yazı yazının üstüne çizilmiyor. Görüntüde de öyle: `OLCEK-1-v16-cariler-960-karanlik.png`.
   - Tasarım hakemi t3, çekmecenin altında kalan metin için aynı ölçütü "K9 … gerçek üst üste binme yok → UYGUN" diye kabul etmişti.
   - Mutasyonla karşı kanıt (M-66): yapışma kaldırılınca K9 0 olur, ama ekran dışı satır eylemi **15+32+12 (BASİT) ve 45+64+12 (PRO) = 180** düğmeye çıkar.
2. **K12 /ayarlar 7↔4.** Tema kartlarında renk önizlemesi üstte, ad altta; araç bunu "yazı düğmede dikey ortada değil" sayıyor. v15 1280 ve 375'te açık=kapalı=7 ölçmüştü. 960'ta kapalıyken işaretlenen kartlar başka kartlar (Okyanus/Menekşe/Zeytin ↔ Zümrüt/Grafit/Bordo), tür aynı. Görüntü `out/ayarlar-960-Basit.png` görüldü: kartlar tuval düzeninde.
3. **K3 kırık /yetkiler "Sahip (Tam yetkili)"**, 960 ve 375 (375'te ayrıca /kullanicilar "Hakem Tasarim (siz)"). `satirk3.mjs` metin düğümlerinin satır kutularını ölçtü:
   - Ana metin 14–15 px, parantezli ek `metin-kucuk` 12 px. Küçük yazının üst kenarı 2–3 px aşağıda.
   - Aracın `Math.round(top/4)` kovası bunu iki satır sayıyor, ama iki parça **aynı satırda** (`ayniSatir: true`; üstler 392↔394 ve 427↔430, altlar 411↔411 ve 448↔447).
   - Görüntülerde tek satır: `out/yetki-960-Basit_yetkiler-acik.png` ve `out/yetki-375-Basit_kullanicilar-acik.png`, ikisi de görüldü.
   - /kullanicilar 960'ta ad gerçekten iki satır ("Hakem" / "Tasarim"), ama bu olcek kapalıyken de öyle (td 113 px) ve kelime sınırında kırılıyor, K3 değil.

**Sonuç:** 960 kabulü, spesifikasyonun yazdığı iki şart için **KARŞILANDI**. v15'teki "KISMEN", spesifikasyon dışı "tablo kaba sığsın" hedefinden geliyordu. O hedef ne paketten önce ne sonra karşılanıyor; paket 960'ta eylem düğmelerini ekrana geri getirdi (180 → 16) ve kırık ad hücrelerini kapattı. İki rapor artık aynı gerçeği yazar: **960 ölçüldü; mod farkı 0 (gerekçeli içerik farkları hariç); yeni görünür kusur 0; kalan 16 düğme ve tabloların kapta kayması paketten önce de vardı → KATMAN (§ v16-8).**

### v16-0.1 Önceki doğrulamanın DÜŞÜK bulguları
| Bulgu | Durum | Gerekçe ve ölçüm |
|---|---|---|
| 375 PRO üst çubuk 181 ↔ BASİT 129 | **Paket kaynaklı değil → tuval-kabuk / KATMAN** | v16 `tablo-375`: PRO 181, BASİT 129. v15 A/B'de olcek kapalıyken de 181. PRO fazladan "· Altınkaynak" ve arama öğesini gösteriyor (içerik farkı, `OLCEK-1-v16-cariler-375-aydinlik.png`). Kabuk dosyaları bu şeridin alanında değil. |
| 375 PRO /cariler ve /urunler'de satır eylemleri kayan tabloda | **Paket kaynaklı değil → KATMAN (SatirEylemleri)** | Sayfa yatay kaydırması 0; tablo kendi kabında kayıyor. v15 A/B'de kapalıyken de aynıydı. v16'da BASİT /urunler 375'te de `u-stok-*` 64 düğme kayan bölgede (tablo 986/349). Telefonda "⋯" altında toplama bileşen işi. |
| Denetim ToListAsync tavanı (başka şerit) | **OLCEK-1'in değil** | v16 ilk denetimde `✗ Sınırsız ToListAsync 149 (tavan 145)` vardı (PosModulu.cs 03:04 değişmiş). Son denetimde bu kapanmış, yerine `✗ P-11 48 > tavan 47` gelmiş (razor catch yolu; Banka/PosTahsilatlar.razor 04:20–04:21'de değişmiş, ZO-ISLEM-A). OLCEK-1 sayaçları üç denetimde de ✓. |
| Testler CSS metnini ölçüyor, geometriyi değil | **Kabul (doğrulayıcı da kabul etti)** | Geometri kanıtı başsız Chrome ölçümleriyle veriliyor (§ v16-4). |
| [KRİTİK] hakem çelişkisi (960) | **KAPANDI** | § v16-0. |

## v16-1. Değişen dosyalar (bu tur)
- **Kod: yok.** olcek.css fc9052c8…, belirtecler.css 949ac1c0…, OlcekTekTestleri.cs 7faea4e1…; başta ve sonda aynı (`v16/sha-olcum.txt`).
- Proje ağacına yazılan tek şey, ölçüm görüntüleri: `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/OLCEK-1-v16-*.png`. 32 dosya, solda BASİT, sağda PRO: cariler, islemler-7hane, urunler, faturalar × 1440/1280/960/375 × açık/koyu.
- Bir de bu rapor.

`git status --porcelain | grep -v -E "/(bin|obj)/"`, paketin satırları (değişmedi):
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Tests/                          (OlcekTekTestleri.cs, OlcekYuzEnvanteri.txt)
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css
?? dotnet/denetim.sh
?? dotnet/denetim.sh.mutbak                       (0 bayt, 18.09; bu paketin DEĞİL)
```

## v16-2. Derleme ve testler
- `dotnet build Kuyumcu.sln --nologo` (`v16/build0.txt`): **0 Uyarı, 0 Hata**.
- Filtreli `FullyQualifiedName~OlcekTek` (`v16/t1.txt`): **31/31 geçti**.
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pOLCEK-1/v16/_Mac_2026-09-29_04_25_51.trx`, `v16/tam.log`): **6971 test · 6970 geçti · 1 başarısız**.
  - Tek kırmızı: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1). Devir listesinde var, v9–v15'te de kırmızıydı.
  - **Madde (7): bu paketin ürettiği yeni kırmızı 0.**
- Yeni test yok; test sayısındaki artış (6907 → 6971) paralel şeritlerden.

## v16-3. Mutasyon kanıtları (`v16/mut.sh`; yedek, fark, test, denetim ve canlı günlük `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/<kod>/`)
| Kod | Değişiklik (tek eşleşme) | Kırmızı test (31'de) | Denetim | Canlı etki (mutasyon açıkken, gerçek Chrome) | cmp |
|---|---|---|---|---|---|
| **M-OLCEK-1-1-v16** (spesifikasyon) | belirtecler.css PRO bloğuna `--kontrol-yuksekligi: 2rem;` | **2**: `OL1_belirteclerde_moda_ozel_blok_YALNIZ_mod_isareti_tasir`, `OL2_hicbir_css_dosyasi_moda_ozel_OLCU_yazmaz_belirtec_ya_da_dogrudan` | `✗ OLCEK-1: MOD_OLCU_BILDIRIMI 1` | `boy.mjs` 1440, aynı id BASİT↔PRO: /emanet `#e-cari` **44/16px → 39/16px**, TOPLAM_FARK **1**. Taban 0: mutasyondan önce /emanet ortak id 3, /cariler 47, fark **0**. | AYNI (sha1 949ac1c0) |
| **M-OLCEK-1-66-v16** | olcek.css (13b) gövde hücresi `position: sticky` → `static` | **1**: `OL28_v15_1280de_liste_tablosu_kaba_sigar_eylem_sutunu_yapisir_ust_cubuk_tek_satir` | (sayaç değil, test kapsamı) | `tablo.mjs` 960: ekran dışı düğme BASİT /cariler **15**, /urunler **32**, /faturalar **12**; PRO **45 / 64 / 12**. Düzeltilmiş hâlde 0/0/0 ve 0/16/0 (§ v16-4.1). | AYNI (sha1 fc9052c8) |

- Test açığı yok.
- İki mutasyonun denetim çıktısında ayrıca başka şeridin `✗ Sınırsız ToListAsync 149 (tavan 145)` satırı da vardı; OLCEK-1'in değil.

## v16-4. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; `v16/olc.sh` SIRALI; çıktılar `/private/tmp/kuyumcu-serit/OLCEK-1/v16/`)
Kiracı `PAKET-OLCEK-1P-TEST` (01M3N7998PPXQFJB7KNQX4J40N) API ile kuruldu:
- Davet 201, kayıt 201, giriş 200.
- Cari 201 ×9, ürün 201 ×4, fatura 200 ×6 (1.234.567,89 ₺ dahil).
- `demo-tohumla.js --eposta`: rc=1. Betiğin V8 sayım kontrolü, kiracı betiğinin açtığı 4 stoksuz üründe tuttu; v10–v15 ile aynı ve ölçümü etkilemiyor.

Oturum parolasız açıldı (`inceleme-vekili.py` 5187). Sistem anahtarı tek kullanımlıktı, rastgele üretildi, dosyası chmod 600'dü. Değeri basılmadı ve iş sonunda dosya 0 bayta boşaltıldı.

### v16-4.1 Tablo ≤ kap ve ekran dışı düğme (`tablo.mjs`; `son/tablo-*.txt`; A/B = olcek.css açık/kapalı; açık ve koyu temada aynı)
| Genişlik | Mod | /cariler tablo/kap · dışı | /urunler | /faturalar | Olcek KAPALI (aynı veri) | üst çubuk | sayfa yatay |
|---|---|---|---|---|---|---|---|
| 1440 | BASİT | 1122/1122 · 0 | 1122/1122 · 0 | 1122/1122 · 0 | hepsi sığar | 68 | 0 |
| 1440 | PRO | **1122/1122 · 0** | 1122/1122 · 0 | 1122/1122 · 0 | hepsi sığar | 68 | 0 |
| 1280 | BASİT | 962/962 · 0 | 962/962 · 0 | 962/962 · 0 | /faturalar 966 | 68 | 0 |
| 1280 | PRO | 962/962 · 0 | 962/962 · 0 | 962/962 · 0 | /cariler 1104 · **30 dışı**, /faturalar 1030 · **12** | 68 | 0 |
| **960** | BASİT | 665/642 · **0** | 703/642 · **0** | 896/642 · **0** | /cariler 907 · **45**, /urunler 692 · **16**, /faturalar 966 · **12** | 90 (kapalı 93) | 0 |
| **960** | PRO | 896/642 · **0** | 961/642 · **16** (`u-stok-adet-*`) | 953/642 · **0** | /cariler 1104 · **45**, /urunler 959 · **64**, /faturalar 1030 · **12** | 90 (kapalı 125) | 0 |
| 375 | BASİT | 349/349 · 0 (kart) | 986/349 (kayar) | kart | — | 129 | 0 |
| 375 | PRO | kayar | kayar | kart | — | 181 (kapalı da 181) | 0 |

- **Tasarım hakemi t3 KRİTİK kapandı:** 1440 PRO /cariler'de tablo kaba sığıyor, Düzenle ve ⋯ tam görünüyor (`out/T-1440-aydinlik-Profesyonel-_cariler.png`, görüldü). t3'te aynı yerde tablo 1193 > 1122 ve ⋯ sağ kenarı 1478'di.
- **Tasarım hakemi t3 YÜKSEK kapandı:** BASİT satışta 7 haneli tutar "1.234.567,89" kutuda görünen/içerik genişliği şöyle:

  | Genişlik | Görünen/içerik (px) |
  |---|---|
  | 1440 | 137/137 |
  | 1280 | 206/206 |
  | 960 | 212/212 |
  | 375 | 206/206 |

  Açık ve koyu temada aynı, hiçbir yerde kesik yok (`olc/iki-*.txt`; görüntü `olc/1440-aydinlik/Basit-1440-aydinlik-tutar.png`, görüldü).

### v16-4.2 Sıkışma (`iki.mjs` + `sikisma-olc.js`; /cariler ve /islemler × 2 mod × 1440/1280/960/375 × açık/koyu = 32 ölçüm)
- K1, K3, K5, K10, K11 ve K12: hepsi **0**.
- Sıfırdan farklı iki değer var:
  - **K7 = 1**, 1280 BASİT /islemler: SATIS-3 ödeme yığını, v13'ten beri var.
  - **K9 = 6**, 960 BASİT /cariler: yapışık sütun örtmesi, görünür binme 0 (§ v16-0).
- Sayfa yatay kaydırması **0**.

### v16-4.3 42 rota A/B (`absik.mjs`; 960 ve 375; 2 mod; 168 + 168 ölçüm)
- **960:** açıkken artan sayaçlar § v16-0'daki üç yanlış pozitif; görünür yeni kusur 0.
- **375:** açıkken artan tek sayaç **K3 kırık** /yetkiler ve /kullanicilar (iki mod). İkisi de aynı yanlış pozitif: parantezli küçük ek aynı satırda (`satirk3.mjs`, `ayniSatir: true`; görüntü görüldü). Diğer sayaçlarda artış 0.
- Sayfa yatay kaydırması her iki genişlikte 42 × 2 → **0**.
- `rota-ab`: 1280 ve 960 × 42 rota × 2 mod. Üst çubuk 1280'de iki modda {68}, 960'ta iki modda eşit.

### v16-4.4 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 42 rota)
- **1440 (`son/tut/w1440b`):** 8 satır.
  - /cariler "—" ×1: anahtar çakışması, PRO'ya özel DÖVİZ BAKİYE sütunu (§ v16-0).
  - /faturalar 61→80 ×7: uzun cari adı PRO'da iki satıra iniyor, çünkü PRO'da 3 sütun fazla. A/B'de ilk satır açık 61↔80, kapalı 53↔55; içerik farkı.
  - İlk koşum (`w1440`) aracın CDP'si tarafında 15 dk yanıtsız kaldı ve kendi süreci öldürülerek yeniden koşuldu. Ürün hatası değil; 960 koşumu ve yeniden koşum sorunsuz bitti.
- **960:** 8 satır (§ v16-0).
- **Düğme sınıfları:** her iki genişlikte ve iki modda aynı. Masaüstü boyları {34, 44, 52}.

### v16-4.5 Hakem senaryosu (`sen.mjs`: dolu fiş, ödeme böl/sil, kaydet hatası, müşteri çekmecesi, Emanet al, kâr-zarar)
- Her yapılandırmada ve her adımda:
  - kesik **0**
  - 44 px altı dokunma hedefi **0**
  - yatay **0**
- Boylar masaüstünde {34, 44, 52}, telefonda {44, 52} (PRO 375 `#islem-diger-ac` 54, SATIS-3).
- TOPLAM_FARK:

  | Genişlik · tema | Değer | İçerik |
  |---|---|---|
  | 1440 açık | 7 | `#islem-cari` sağ dolgu ×5 (SATIS-3) ve müşteri çekmecesinde "—" ×2 (aynı anahtar çakışması) |
  | 1280 koyu | 7 | |
  | 960 açık | 13 | `#islem-cari` ×5 ve fiş kalem satırı 61↔75 ×8. BASİT kalem satırı ile PRO fiş tablosu (`.fis-pro-tablo`) iki ayrı bileşen; PRO'da fazladan sütun yüzünden ürün adı iki satır. SATIS-3 alanı. |
  | 375 | 17 | ≤600'de fiş kalem dolgusu 4↔8 ve `#islem-cari` (SATIS-3) |

- Senaryonun PRO dalında `#islem-bacak-ekle` bulunamıyor ("YOK"). v15'te de böyleydi; betik PRO ödeme bölme düğmesinin kimliğini bilmiyor. PRO tutar kutusu ölçülmedi. t3 hakemi PRO kutuyu 214 px, sığıyor olarak ölçmüştü.

### v16-4.6 Ekran görüntüleri (Read ile GÖRÜLDÜ)
Görülenler:
- `out/T-960-aydinlik-Profesyonel-_cariler.png`: eylem sütunu sağa yapışık, Tahsilat, Düzenle ve ⋯ tam görünüyor; bakiye sütunu altından kayıyor, sınırda 1 px ayraç var.
- `out/T-1440-aydinlik-Profesyonel-_cariler.png`: 8 sütun kaba sığıyor, emoji yok.
- `olc/1440-aydinlik/Basit-1440-aydinlik-tutar.png`: 1.234.567,89 tam görünüyor.
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v16-cariler-960-karanlik.png`: iki modda satır eylemi aynı boy (34), iki modda aynı üst çubuk.
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v16-cariler-375-aydinlik.png`: BASİT kart, PRO kayan tablo; PRO'nun üst çubuğu içerik yüzünden yüksek.
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v16-urunler-1280-karanlik.png`: iki modda satır düğmeleri aynı, PRO 10 sütun kaba sığıyor.
- `out/yetki-960-Basit_yetkiler-acik.png`, `out/yetki-375-Basit_kullanicilar-acik.png`: parantezli ek aynı satırda.
- `out/ayarlar-960-Basit.png`: tema kartları.

Tüm yan yana görüntüler: `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v16-{cariler,islemler-7hane,urunler,faturalar}-{1440,1280,960,375}-{aydinlik,karanlik}.png` (32 dosya).

## v16-5. Denetim
`bash dotnet/denetim.sh` üç kez koşuldu (`v16/den1.txt`, mutasyon günlükleri, `v16/den2.txt`). **OLCEK-1 sayaçları her seferinde ✓:**
- `MOD_OLCU_BILDIRIMI` 0
- `YUZ_ONEMLI_OLCU` 0
- `YUZ_OLCU_ENVANTER_DISI` 0

Genel sonuç ise "DENETİM BAŞARISIZ". Nedeni başka şeritlerin ara durumu:
- İlk koşumda `✗ Sınırsız ToListAsync 149 (tavan 145)`: Pos, ZO-ISLEM-A.
- Sonda `✗ P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > tavan 47`: razor catch yolu; Banka/PosTahsilatlar.razor 04:20'de değişmiş, ZO-ISLEM-A.

OLCEK-1 bu turda hiçbir kod dosyasına dokunmadı. Paralel şerit kapısına göre denetim TEMİZ şartı orkestratörün dalga kapısıdır.

## v16-6. DoD sözleşmesi
- Yeni varlık, kayıt türü, uç, form ya da düğme yok; bu turda kod değişikliği de yok. PATCH/DELETE, [RolGerekli] ve denetim kaydı maddeleri uygulanamaz.
- Migration gerekmedi.
- Sayfa ve razor dosyalarına dokunulmadı. ModalPencere sayacı değişmedi. app.css'e ve başka şeridin dosyasına yazılmadı.
- Mevzuat içeriği ve fiyat/teklif metni yok.

## v16-7. Temizlik
- API 5186 ve vekil 5187 `kill` ile durduruldu; `lsof` 5186/5187 **boş**. v16 Chrome profillerine bağlı süreç **0** (yalnız kendi `--user-data-dir` yolum hedeflendi).
- **Kiracı:** `zsh kiraci-temizle.sh 01M3N7998PPXQFJB7KNQX4J40N --uygula` çalıştı.
  - Sonraki denetimde information_schema'daki 70 `tenant_id` tablosunda **9 artık satır** bulundu: `is_calismalari` 3 ve `denetim_kayitlari` 6 (varlık türü IsCalismasi, VADE_HATIRLATMA).
  - Bunlar temizlikten SONRA, 04:26'da yazılmıştı; zamanlanmış iş çalışması, silinen kiracı için kayıt açmış (başka şeridin API'si ya da iş zamanlayıcısı).
  - Bu 9 satır `DELETE … WHERE tenant_id=…` ile silindi (3 + 6). Yeniden sayım **0**; `PAKET-OLCEK%` kiracı **0**.
  - → Orkestratöre not: kiraci-temizle.sh ile zamanlanmış işler arasında bir yarış var; iş, silinen kiracıya yazabiliyor.
- **Kiracı sayısı:** başta 14, sonda **16**. Kendi kiracım eklenip silindi; artış başka şeritlerin kiracılarından (PAKETQ10-4, PAKETT-S2, PAKET-ORTAK-1, PAKETZO-BELGE, PAKETSATIS-3-HAKEM7…). QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- Sistem anahtarı ve fikstür `printf '' >` ile boşaltıldı (0 bayt).
- **DİSK (yapılamadı, kurucuya):** `rm -rf …/v16/profil-*` komutunu TurkReport koruması BLOKLADI ("destructive komut bloklandi"). Kurucunun terminalde silmesi gereken:
  - `/private/tmp/kuyumcu-serit/OLCEK-1/v16` (4,5 GB, çoğu Chrome profili)
  - `/private/tmp/kuyumcu-serit/OLCEK-1` tamamı (8,5 GB)
- Kopya ağaç yok; proje ağacına geçici dosya bırakılmadı.

## v16-8. Açık kalanlar (sahibiyle)
1. **KATMAN (SatirEylemleri) / Cariler / Urunler:**
   - 960'ta 8–10 sütunlu PRO tabloları kaba sığmıyor; olcek kapalıyken de sığmıyor. Kalıcı çözüm, <1100'de eylemlerin "⋯" altında toplanması ya da sütun önceliği. O gelirse (13b) yapışma kaldırılabilir.
   - PRO /urunler 960 `u-stok-adet-*` ×16 ve BASİT /urunler 375 ×64 kayan bölgede.
   - 375'te PRO eylemleri kayan tabloda.
   - Emoji yerine Ikon.
2. **tuval-kabuk / KATMAN:** 375 PRO üst çubuk 181 ↔ BASİT 129 (içerik farkı).
3. **ZO-BELGE (Faturalar):** PRO'da fazladan sütun yüzünden uzun ad sarıyor (1440 satır 80). /faturalar 960 düzeni ölçüm sırasında değişti: 04:01 zo-belge.css, artık kart.
4. **SATIS-3:**
   - `#islem-cari` sağ dolgu.
   - ≤600 fiş kalem dolgusu.
   - 960 fiş kalem satırı 61↔75.
   - 375 PRO `#islem-diger-ac` 54.
   - 1280 BASİT K7 1.
   - Senaryo betiği PRO ödeme bölme kimliğini bilmiyor.
5. **ORTAK-1:** tam süitte `OrtakGorunumSaflik.O8` kırmızı.
6. **ZO-ISLEM-A:** denetim `P-11 48 > 47` (ilk koşumda ToListAsync 149 > 145).
7. **Orkestratör (ölçüm aracı):**
   - `tutarlilik.mjs` ve `sen.mjs` anahtarı metinden kuruyor ("—" çakışması); anahtar sütun indeksi ya da data-* ile kurulmalı.
   - `sikisma-olc.js` K9 örtülen (sticky, çekmece) yazıyı binme sayıyor; `elementFromPoint` süzgeci önerilir. K3 `round(top/4)` karışık yazı boyunda tek satırı iki satır sayıyor; satır kutusu dikey örtüşme ile sayılmalı.
   - `kiraci-temizle.sh` ile zamanlanmış işler arasındaki yarış.
8. **DUGME-2:** şerit `btn-kucuk` 44 ×2 (/faturalar, /kur) ve 34 ×1 (/urunler/agac).
9. **Kurucu:**
   - `/private/tmp/kuyumcu-serit/OLCEK-1` (8,5 GB) silinmeli (hook engeli).
   - `dotnet/denetim.sh.mutbak` (0 bayt) silinmeli.
   - Kenar menüsü PRO yüzü kararı.

## v16-9. Kurucuya not
- Aynı düğme ve kutu artık iki modda aynı boyda. Emanet al iki modda 52 px, form kutuları 44 px, satır düğmeleri 34 px. Tablet ve telefon genişliği dahil ölçüldü.
- 1440 ve 1280 genişlikte Profesyonel Müşteriler, Ürünler ve Faturalar listeleri ekrana tam sığıyor.
- Tablet genişliğinde (960) geniş listeler kendi içinde yana kayıyor. Tahsilat, Düzenle ve ⋯ sağda sabit ve her satırda görünüyor. Önceden 180 düğme ekran dışındaydı; şimdi yalnız Ürünler'deki "0 adet" kısayolu (16 düğme) kayan kısımda, aynı işi sabit sütundaki "Stok" düğmesi görüyor.
- Satışta 7 haneli tutar (1.234.567,89) her genişlikte tam okunuyor.

---

# Ek: v15 raporu (değiştirilmeden)

## (v15 başlığı) PAKET OLCEK-1: Tek ölçek (mod ölçü değiştirmez). Uygulama raporu v15

**Tarih:** 2026-09-29 01:05 – 02:45 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9955–9968; 5198/5199/5200'e ve tarayıcı bölmesine dokunulmadı)
**v15 neden var:** Son doğrulamada 1 KRİTİK + 1 ORTA + 2 DÜŞÜK bulgu kaldı. **KRİTİK:** olcek.css, 1280 px PRO'da /cariler ve /urunler tablolarını kabından taşırıyordu. v14'teki "paket kaynaklı değil" iddiası yalnız bakiye rozetli veride doğruydu; rozetsiz veride taşmayı paket yaratıyordu. **ORTA:** PRO /cariler 1280'de üst çubuk iki satıra sarıyordu. Ağaçtaki iş korundu, düzeltmeler yalnız `olcek.css`'te yapıldı ve testleri `OlcekTekTestleri.cs`'e eklendi.
**Devralma:** 01:05'te ağaçta yarım mutasyon yoktu (olcek.css sha1 1462850f…, v14 ile aynı; yedeği `v15/olcek-v14.css`). 5186 ve 5187 boştu. Kiracı sayısı başta **17**, PAKET-OLCEK kiracısı 0.

## v15-0. Bulguların kapanışı

**Veri:** kiracı `PAKET-OLCEK-1O-TEST`, iki aşamada kuruldu.
- **A (rozetsiz, doğrulayıcının verisi):** 3 kısa adlı müşteri ve 2 ürün.
- **B:** A'ya ek olarak 6 uzun adlı müşteri, 2 uzun adlı ürün, `demo-tohumla.js --eposta` içeriği (bakiye rozetleri, 13 haneli barkodlar) ve 6 fatura (1.234.567,89 ₺ dahil).

| Bulgu | Durum | Kanıt (başsız Chrome + CDP, olcek.css AÇIK ↔ KAPALI A/B) |
|---|---|---|
| **[KRİTİK] 1280 PRO'da tablolar kabından taşıyor** | **KAPANDI (1280); 960'ta kısmen** | **Kök neden** (sütun ölçümü `exp.mjs`/`minc.mjs`): 1440'a göre kurulan ölçek (15 px yazı, 8 px dolgu, ad sütunu en az 210 px) 962 px'lik kaba fazla geliyordu. Olcek kapalıyken tablo yalnız ad sütunu 98–160 px'e ezilip 2–3 satıra kırıldığı için sığıyordu (K3). **Çözüm (13a), moddan değil genişlikten türer, iki modda aynı:** 769–1399 px'te `--tablo-yazi` 14 px, `--tablo-baslik-yazi` 12 px, `--tablo-dolgu` 8×4 px; ad sütunu en az 13 × yazı; satır eylemi düğmesinin yan dolgusu 8 px. Satır boyu 61'de kalır. **(13c)** Satır eylemindeki emoji süsü gizlenir (aria-hidden; BASİT /cariler'de zaten yoktu). **(13b)** Emniyet olarak eylem sütunu sağa yapışır. **Sonuç, 1280'de her iki veri × iki mod × iki tema:** /cariler, /urunler, /faturalar tabloları **962 = kap**, ekran dışı düğme **0** (§ v15-4.1). A/B: olcek kapalıyken PRO /cariler (B) **1104 > 962, 30–32 ekran dışı düğme**; PRO /faturalar **1030 > 962, 12 düğme**. 960 → § v15-4.1 ve § v15-8. |
| **[ORTA] PRO /cariler 1280'de üst çubuk 93 px (BASİT 68)** | **KAPANDI** | Kök neden: 19 px kur değeri, PRO'nun fazladan EUR öğesi ve uzun başlık tek satıra sığmıyordu. En uzun başlıkta (/gider-pusulalari, 180 px) olcek.css KAPALIYKEN de sarıyordu: en geniş içerik 998 > 956 px. **(13d)** 769–1439 px'te: kur değeri 16 px, çubuk yan dolgusu 20 px, aralıklar 12 px, şerit içi aralık 10 px. 769–1279'da iki satırlı düzen: üstte başlık ile Yardım/mod, altta kur şeridi. Öğe kaybolmaz, taşma gizlenmez. **Ölçüm, 42 rota × 2 mod:** 1280'de `ustCubukH` her rotada iki modda **68 = 68**, eşitsiz rota **0** (önce: 7 rotada PRO 93). 960'ta her rotada iki mod **eşit** (90; iki rotada 109 = 109). Önce PRO 125, BASİT 93'tü. |
| [DÜŞÜK] /ayarlar tema kartlarında K12 = 7 | **Paket kaynaklı değil (A/B)** | `k12.mjs`, 1280 ve 375 × iki mod: olcek açık **7**, (12c) `grid-auto-rows` kapalı **7**, olcek.css tamamen kapalı **7**. Kartlarda renk önizlemesi üstte, ad altta; bu tuval tasarımı (`out/k12-Profesyonel-1280.png`, görüldü). K12 önizlemeli kartı "yazı dikeyde ortada değil" sayıyor, yani ölçüt yanlış pozitifi. Değişiklik yapılmadı. |
| [DÜŞÜK] Ölçüm aracında anahtar çakışması | **Araç bu paketin değil; etkisi ayrıştırıldı** | `betikler/tur/tutarlilik.mjs` ortak ölçüm aracı, hiçbir şeridin alanında değil; ona dokunulmadı. Paket ölçümleri için `tablo.mjs` ve `exp.mjs` sütunları İNDEKSLE ölçüyor (`kol[]`, `ths[]`). tutarlilik'teki `/cariler td "—"` satırı v13/v14'te gerekçelendi ve hâlâ tek satır. Anahtarın sütun indeksi ya da data-* ile kurulması → orkestratör (araç sahibi), § v15-8. |

## v15-1. Değişen dosyalar (bu tur)
- `dotnet/Kuyumcu.Web/wwwroot/css/olcek.css` (852 → 920 satır; sha1 fc9052c8…):
  - **(13a)** 769–1399 tablo ölçeği.
  - **(13b)** Yapışkan eylem sütunu. Zemin `box-shadow: inset … var(--yuzey)` ile çiziliyor ve 1 px `--ayrac` çizgisi var; `background` yazılmadı (OL6).
  - **(13c)** Emoji süsü gizlendi; `:has(svg)` ince çizgi Ikon'u korur.
  - **(13d)** Üst çubuk.
  - **(11e)** Ürün adı en az genişliği 14 → 13 × yazı. 769–1399 aralığında 13a ile tek ölçü.
  - Her kuralın başında ölçümlü gerekçe yorumu var.
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs` (sha1 7faea4e1…):
  - **OL28** yeni: `OL28_v15_1280de_liste_tablosu_kaba_sigar_eylem_sutunu_yapisir_ust_cubuk_tek_satir`.
  - **OL9** ve **OL22**: 769–1439 dar masaüstü kuralını genel ≥769 kuralından ayırır.
  - **OL23**: 11e beklentisi 13 ×.
  - Test sayısı 30 → **31**.
- belirtecler.css, index.html, denetim.sh, envanter, razor, sözlük, uç, migration: **dokunulmadı**. **Dokunulan sayfalar: yok.**

`git status --porcelain | grep -v -E "/(bin|obj)/"`, paketin satırları (Kuyumcu.Tests dizini izlenmiyor):
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html        (v7'den; bu tur değişmedi)
?? dotnet/Kuyumcu.Tests/                        (OlcekTekTestleri.cs değişti; OlcekYuzEnvanteri.txt değişmedi)
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css (bu tur değişmedi, sha1 949ac1c0…)
?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css     (sha1 fc9052c8…)
?? dotnet/denetim.sh                            (bu tur değişmedi)
?? dotnet/denetim.sh.mutbak                     (0 bayt, 18.09; bu paketin DEĞİL)
```

## v15-2. Derleme ve testler
- `dotnet build Kuyumcu.sln --nologo`, son mutasyondan sonra temiz derleme: **0 Uyarı, 0 Hata** (`v15/build6.txt`).
- Ara derlemelerden birinde (build3) başka bir sürecin Kuyumcu.Web obj dosyasını kilitlemesi yüzünden `MSB4018 ConvertDllsToWebCil` hatası alındı. Yeniden derleme 0 hata verdi ve DLL zaman damgası doğrulandı.
- Filtreli `FullyQualifiedName~OlcekTek`: **31/31 geçti** (`v15/t5.txt`).
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pOLCEK-1/v15/_Mac_2026-09-29_02_28_16.trx`): **6907 test · 6906 geçti · 1 başarısız**.
  - Tek kırmızı: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1). Devir listesinde var, v9–v14'te de kırmızıydı.
  - **Madde (7): bu paketin ürettiği yeni kırmızı 0.**
- Test sayısı 6825 → 6907: artış paralel şeritlerin testleri, bunlara OL28 dahil.
- `bash dotnet/denetim.sh` (son, `v15/den2.txt`): **DENETİM TEMİZ**. OLCEK-1 sayaçları: `MOD_OLCU_BILDIRIMI` 0 ✓, `YUZ_ONEMLI_OLCU` 0 ✓, `YUZ_OLCU_ENVANTER_DISI` 0 ✓.

## v15-3. Mutasyon kanıtları (`v15/mut.sh` + `v15/canli-mut.sh`; yedek, fark ve günlük `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/<kod>/`)
| Kod | Değişiklik (tek eşleşme) | Kırmızı test | Canlı etki (mutasyon açıkken, B verisi) | cmp |
|---|---|---|---|---|
| **M-OLCEK-1-65** | (13a) `--tablo-yazi … * 14 / 18` → `* 15 / 18` | `OL28_v15_…` (31'de 1 kırmızı) | PRO 1280 /urunler tablo **991 > 962** (düzeltmeyle 962) | AYNI |
| **M-OLCEK-1-66** | (13b) gövde hücresi `position: sticky` → `static` | `OL28_v15_…` | PRO 960 /cariler ekran dışı düğme **48** (düzeltmeyle 0) | AYNI |
| **M-OLCEK-1-67** | (13d) `column-gap: 12px; padding-inline: 20px` → `18px; 28px` | `OL28_v15_…` | PRO 1280 /gider-pusulalari ve /hurda üst çubuk **93** (düzeltmeyle 68) | AYNI |
| **M-OLCEK-1-68** | (13c) emoji `display: none` → `inline` | `OL28_v15_…` | (canlı ölçüm koşulmadı; OL28 kırmızısı yeterli) | AYNI |
| **M-OLCEK-1-1-v15** (spesifikasyon) | belirtecler.css PRO bloğuna `--kontrol-yuksekligi: 2rem` | `OL1_belirteclerde_moda_ozel_blok_YALNIZ_mod_isareti_tasir`, `OL2_hicbir_css_dosyasi_moda_ozel_OLCU_yazmaz_belirtec_ya_da_dogrudan` · denetim `✗ OLCEK-1: MOD_OLCU_BILDIRIMI 1` | (v14 canlı: /emanet PRO {39:1,52:2} ↔ taban {44:1,52:2}) | AYNI |

- Test açığı yok. M65–M68 koşumlarında denetim TEMİZ kaldı; bu kurallar sayaç değil, test kapsamında.
- Son mutasyondan sonra olcek.css sha1 **fc9052c8… = mutasyon öncesi** (`v15/sha-once.txt`). belirtecler.css 949ac1c0… değişmedi.

## v15-4. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; `v15/olc.sh` SIRALI; çıktılar `/private/tmp/kuyumcu-serit/OLCEK-1/v15/`)
Kiracı API ile kuruldu:
- Davet 201, kayıt 201, giriş 200.
- Cari 201 ×9, ürün 201 ×4, fatura 200 ×6.
- `demo-tohumla.js`: rc=1. Betiğin kendi V8 sayım kontrolü, stoksuz 4 üründe tuttu; v10–v14'te de aynıydı ve ölçümü etkilemiyor.

Oturum parolasız açıldı (`inceleme-vekili.py` 5187). Sistem anahtarı tek kullanımlıktı, rastgele üretildi, dosyası chmod 600'dü ve iş sonunda 0 bayta boşaltıldı.

### v15-4.1 Kabul ölçütü: /cariler · /urunler · /faturalar tablo ≤ kap ve ekran dışı düğme (`tablo.mjs`, `son/tablo-*.txt`; B verisi; A/B = olcek.css açık/kapalı)
| Genişlik · tema | Mod | /cariler tablo/kap · dışı | /urunler | /faturalar | Olcek KAPALI (aynı veri) | üst çubuk | sayfa yatay |
|---|---|---|---|---|---|---|---|
| 1440 açık/koyu | BASİT | 1122/1122 · 0 | 1122/1122 · 0 | 1122/1122 · 0 | hepsi sığar | 68 | 0 |
| 1440 açık/koyu | PRO | 1122/1122 · 0 | 1122/1122 · 0 | 1122/1122 · 0 | hepsi sığar | 68 | 0 |
| **1280 açık/koyu** | BASİT | **962/962 · 0** | **962/962 · 0** | **962/962 · 0** | /faturalar 966 > 962 | 68 | 0 |
| **1280 açık/koyu** | PRO | **962/962 · 0** | **962/962 · 0** | **962/962 · 0** | /cariler **1104 · 30–32 dışı**, /faturalar **1030 · 12 dışı** | **68** | 0 |
| 960 açık/koyu | BASİT | 665/642 · 0 | 703/642 · 0 | 896/642 · 0 | /cariler 907 · **45–48 dışı**, /urunler 692 · 16, /faturalar 966 · 12 | 90 (önce 93) | 0 |
| 960 açık/koyu | PRO | 896/642 · 0 | 961/642 · **16** (`u-stok-adet-*` stok hücresi) | 953/642 · 0 | /cariler 1104 · **45–48**, /urunler 959 · **64**, /faturalar 1030 · 12 | 90 (önce 125) | 0 |
| 375 açık/koyu | BASİT | 349/349 · 0 (kart) | 986/349 (kayar) | 349/349 · 0 (kart) | — | 129 | 0 |
| 375 açık/koyu | PRO | 1227/349 (kayar) | 1286/349 (kayar) | 349/349 · 0 (kart) | — | 181 (olcek kapalı da 181) | 0 |

**A verisi, rozetsiz, doğrulayıcının durumu (`tabA1.txt`), 1280:**
- PRO /cariler **962/962 · 0** (önce 1050 · 6). Kapalı 962.
- PRO /urunler **962/962 · 0** (önce 1135 · 6). Kapalı 962.
- BASİT her iki rota 962/962.

**960 (ölçüt karşılanmadı, gerekçe):**
- 8–10 sütunlu PRO tabloları 642 px'lik kaba olcek.css KAPALIYKEN de sığmıyor: 959, 1030, 1104 > 642. Tüm sütunların en az içerik toplamı kaptan geniş, yani yalnız CSS ile sığması için sütun gizlemek ya da adı kırmak gerekir. Bu ya özellik silmek ya da K3 ihlali olur.
- Paketin getirdiği: tablo kendi kabında kayar, sayfa yatay kaydırması 0. **Eylem sütunu sağa yapışık**, yani Tahsilat, Düzenle ve ⋯ hiçbir genişlikte ekran dışına düşmez. Ekran dışı düğme toplamı 48+64+12 → 0+16+0.
- Kalan 16, PRO /urunler'deki "0 adet" stok hücresi düğmesi (`u-stok-adet-*`). Aynı işi yapışık sütundaki "Stok" düğmesi görüyor.
- Yapışık sütunun bedeli: kayan içerik düğmenin altından geçer. Sıkışma aracı 960'ta K9 = 6 sayıyor (/cariler BASİT, /urunler PRO, /faturalar iki mod). Yapışma kapatılınca (`yapiskansiz.css`) K9 = 0 oluyor, ama bu kez 45–64 düğme ekran dışına düşüyor.
- Donmuş eylem sütunu bilinen bir kalıp; sınırda 1 px ayraç çizgisi var (görüntü aşağıda). Kalıcı çözüm, <1100'de eylemlerin "⋯" altında toplanması ya da sütun önceliği. Bu Cariler/Urunler/SatirEylemleri (KATMAN) işi → § v15-8.

### v15-4.2 Üst çubuk, 42 rota × 2 mod (`son/rota-ab.txt`, A/B)
- **1280:** `ustCubukH` dağılımı {68}. İki modda eşit olmayan rota **0**. Önce 7 rotada PRO 93 idi: /denetim-izi, /evraklar, /evraklar/takvim, /fiyatlandirma, /gider, /gider-pusulalari, /hurda. Olcek kapalıyken bu rotalar yine PRO 93.
- **960:** her rotada iki mod eşit. Dağılım {68 (sekmeli sayfalar), 90, 109}.
- **Sayfa yatay kaydırması:** 42 rota × 2 genişlik × 2 mod → **0**.
- **Paketin artırdığı tek taşma:** /vitrin `#vitrin-ayarlar`, 1280'de 150 (kapalı 139), 960'ta 470 (kapalı 459). +11 px, 44 px düğmelerin kontrol yazısından (16 px) geliyor. Ekran dışı düğme sayısı açık ve kapalı durumda aynı (25) → sahibi KATMAN (Vitrin), § v15-8.
- Diğer taşan tablolar (/yetkiler yapışık ilk sütun, /raporlar/islem-defteri, /denetim-izi, /fiyatlandirma, /kullanicilar) açıkken kapalıya göre **daha az** taşıyor.

### v15-4.3 Sıkışma (`iki.mjs` + `sikisma-olc.js`, /cariler ve /islemler × 2 mod; `olc/iki-*.txt`)
- **K1, K3, K5, K10, K11 ve K12:** 1440, 1280, 960 ve 375 ölçümünde, açık ve koyu temada **0**.
- **K9:** 960 BASİT /cariler'de 6. Bu, yapışık sütunun ürettiği değer (§ 4.1). Diğer ölçümlerde 0.
- **K7:** 1280 BASİT /islemler'de 1. SATIS-3 ödeme yığını, v13'ten beri var.
- **BASİT satışta 7 haneli tutar** "1.234.567,89": 1440'ta 137/137, 1280'de 206/206, 960'ta 212/212, 375'te 206/206. **Hiçbir genişlikte kesik yok.**
- /faturalar K3 = 2–3 ("50 ₺ (%20)" sarıyor). Olcek kapalıyken de 3/2, sahibi ZO-BELGE.

### v15-4.4 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 42 rota; `son/tut/w1440`, `w960`)
- **1440: 9 satır.**
  - v14'teki /urunler 61→80 ×11 **kapandı**: satır eylemindeki emoji gitti, ad tek ölçüde.
  - Yeni satırlar /faturalar 61→80 ×8. v14'te veride fatura yoktu. PRO fazladan KAYNAK sütununu gösteriyor (📄 + "Manuel" iki satır), bu yüzden uzun cari adı sarıyor. Bu bir içerik farkı: olcek kapalıyken de PRO satırları 55 ile 53 arasında karışık boyda çıkıyor.
  - Kalan: /cariler td "—" ×1, anahtar çakışması (§ 0).
- **960: 4 satır.** v14 ile aynı türler: /cariler "—" ×1, /fiyatlandirma içerik sarması ×3.
- Düğme dağılımı: sınıfsız 34/44, birincil ve ikincil 44, küçük 34, büyük 52. İstisna, şerit `btn-kucuk` 44 ×2 ve 34 ×1 (DUGME-2 devri). **v14 ile aynı**, yalnız adetler veri yüzünden farklı.

### v15-4.5 Hakem senaryosu (`sen.mjs`) ve düğme ailesi (`prob.mjs`)
- **sen:**
  - 1440 açık: TOPLAM_FARK **5** (v14: 7). Kalan: `#islem-cari` sağ dolgu ×5; SATIS-3 alanında, gerekçeli ve envanterde.
  - 1280 koyu: 7. 375 açık ve koyu: 21 (v14: 19/23). Türler aynı: ≤600'de fiş kalem dolgusu 4↔8 (SATIS-3), `#islem-cari`.
  - Kesik **0**, 44 px altı dokunma hedefi **0**, yatay **0**.
- **prob** (8 rota × 6 yapılandırma):
  - Masaüstü boyları {34, 44, 52}, telefon {44, 52}.
  - disi / degerUymaz / yatay **0**.
  - Tek kayıt 375 PRO `#islem-diger-ac` 54 px. v13/v14'te de aynıydı, SATIS-3 alanı.

### v15-4.6 Ekran görüntüleri (Read ile GÖRÜLDÜ)
`ham/TUVAL-KARSILASTIRMA/OLCEK-1-v15-{cariler,islemler-7hane,urunler,faturalar}-{1440,1280,960,375}-{aydinlik,karanlik}.png`: 32 dosya, solda BASİT, sağda PRO. Bakılanlar:
- `out/T-1280-aydinlik-Profesyonel-_urunler.png`:
  - 10 sütun kabın içinde; Stok, Düzenle ve ⋯ tam görünüyor.
  - Başlık, kur şeridi, Yardım ve mod anahtarı tek satırda.
  - Uzun ürün adı kelime sınırında iki satır.
- `out/T-1280-karanlik-Profesyonel-_cariler.png`:
  - Tahsilat, Düzenle ve ⋯ tam görünüyor.
  - Borç ve Alacak rozetleri tutarın altında; emoji yok.
  - Üst çubuk tek satır (68).
- `OLCEK-1-v15-faturalar-1280-aydinlik.png`:
  - İki modda Düzenle ve ⋯ görünüyor, 1.234.567,89 ₺ tam okunuyor.
  - PRO'da KAYNAK sütunu yüzünden satır daha yüksek (içerik farkı).
- `out/T-960-karanlik-Profesyonel-_cariler.png`:
  - Başlık, Yardım ve mod üstte; kur şeridi altta (iki satır, 90).
  - Eylem sütunu sağa yapışık ve görünür; bakiye sütunları altından kayıyor (K9 gerekçesi).
- `out/k12-Profesyonel-1280.png`: /ayarlar tema kartı; önizleme üstte, ad altta.

## v15-5. Denetim
`bash dotnet/denetim.sh` (son değişiklik ve mutasyonlardan sonra, `v15/den2.txt`): **DENETİM TEMİZ.** v14'teki P-11 (48 > 47) kırmızısı başka şeritte kapanmış.

## v15-6. DoD sözleşmesi
- Yeni varlık, kayıt türü, uç, form ya da düğme yok. Değişiklik yalnız olcek.css yerleşim/ölçü kurallarında ve testte. Bu yüzden PATCH/DELETE, [RolGerekli] ve denetim kaydı maddeleri uygulanamaz.
- Migration gerekmedi. Sayfa ve razor dosyalarına dokunulmadı. ModalPencere sayacı değişmedi.
- Mevzuat içeriği ve fiyat/teklif metni yok. app.css'e ve başka şeridin dosyasına yazılmadı.
- Emoji gizleme razor'a dokunmadan yapıldı. İnce çizgi Ikon gelirse `:has(svg)` onu korur.

## v15-7. Temizlik
- API 5186 ve vekil 5187 `kill` ile durduruldu; `lsof` 5186/5187 **boş**. v15 Chrome profillerine bağlı süreç **0**.
- **Kiracı:** `zsh kiraci-temizle.sh 01M3N0Z0N5D3YRCKJ85B72ARHE --uygula` çalıştırıldı ve "SİLİNDİ" döndü (1659 satır + tenants 1). Ardından kontrol edildi:
  - information_schema'daki tüm `tenant_id` tablolarında bu kiracıya ait satır toplamı **0**.
  - `kiraci_davetleri`'nden davet satırı silindi (1 satır); kalan 0.
  - `PAKET-OLCEK%` kiracı **0**.
- **Kiracı sayısı:** başta 17, sonda **16**. Kendi kiracım eklenip silindi; ölçüm süresince başka şeritler bir kiracı silmiş. QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- Sistem anahtarı ve fikstür `printf '' >` ile boşaltıldı (0 bayt).
- **DİSK (yapılamadı, kurucuya):** `rm -rf …/v15/profil-*` komutunu TurkReport koruması BLOKLADI (hook: "destructive komut bloklandi"). Silinecekler:
  - `/private/tmp/kuyumcu-serit/OLCEK-1/v15` (4,8 GB, çoğu Chrome profili).
  - `/private/tmp/kuyumcu-serit/OLCEK-1` tamamı (8,4 GB).
- Proje ağacına geçici dosya bırakılmadı.

## v15-8. Açık kalanlar (sahibiyle)
1. **KATMAN (SatirEylemleri) / Cariler / Urunler / Faturalar:**
   - 960 px'te (769–1099) 8–10 sütunlu PRO tabloları yalnız CSS ile sığmıyor; olcek kapalıyken de sığmıyor.
   - Paket eylem sütununu yapıştırdı. Kalıcı çözüm eylemlerin <1100'de "⋯" altında toplanması ya da sütun önceliği; bu razor ve bileşen işi. O gelince (13b) kaldırılabilir ve K9 = 0 olur.
   - /urunler stok hücresi düğmesi (`u-stok-adet-*`) 960 PRO'da kayan kısımda kalıyor.
   - Emoji yerine Ikon.
2. **KATMAN (Vitrin):** `#vitrin-ayarlar` 1280'de 139 px, 960'ta 459 px taşıyor; 25 düğme ekran dışında (olcek kapalıyken de). Paketin 16 px kontrol yazısı bu taşmaya +11 px ekliyor.
3. **ZO-BELGE (Faturalar):**
   - PRO KAYNAK hücresinde ikon ve metin alt alta, satırı 80 px yapıyor.
   - "50 ₺ (%20)" K3 (olcek kapalıyken de var).
4. **tuval-kabuk / KATMAN:** 375'te PRO üst çubuk 181, BASİT 129. PRO kaynak adını ("· Altınkaynak") ve arama düğmesini gösteriyor; olcek kapalıyken de aynı. Mod içerik farkı.
5. **SATIS-3:**
   - `#islem-cari` sağ dolgu.
   - ≤600'de fiş kalem dolgusu.
   - 375 PRO `#islem-diger-ac` 54 px.
   - 1280 BASİT K7 1.
6. **ORTAK-1:** tam süitte `OrtakGorunumSaflik.O8` kırmızı.
7. **Orkestratör (ölçüm aracı):** `tutarlilik.mjs` anahtarı metinden kuruluyor → sütun indeksi ya da data-* ile kurulmalı.
8. **DUGME-2:** şerit `btn-kucuk` 44 ×2 (/faturalar, /kur) ve 34 ×1 (/urunler/agac).
9. **Kurucu:**
   - `/private/tmp/kuyumcu-serit/OLCEK-1` (8,4 GB) silinmeli (hook engeli).
   - `dotnet/denetim.sh.mutbak` (0 bayt) silinmeli.
   - Kenar menüsü PRO yüzü kararı.

## v15-9. Kurucuya not
- 1280 genişlikte (standart dizüstü) Profesyonel Müşteriler, Ürünler ve Faturalar listeleri artık ekrana tam sığıyor; Tahsilat, Düzenle ve ⋯ her satırda görünüyor. Bunun için 1400 px'ten dar ekranda liste yazısı bir kademe küçüldü (15 → 14 px). Bu küçülme iki modda aynı.
- Satır düğmelerindeki renkli emojiler (💵 ✏️ 📦) kalktı; düğmelerde yalnız yazı var.
- Üst çubuk 1280'de her ekranda, iki modda da tek satır. Tablet genişliğinde (960) iki satır: üstte başlık, altta kur. İki modda aynı boy.
- Tablet genişliğinde geniş listeler yana kayıyor. Eylem düğmeleri sağda sabit kalıyor, kaybolmuyor.

---

# Ek: v14 raporu (28.09 23:36 – 29.09 00:40, değiştirilmeden)

## (v14 başlığı) Uygulama raporu v14

**Tarih:** 2026-09-28 23:36 – 2026-09-29 00:40 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9955–9963; 5198/5199/5200'e ve tarayıcı bölmesine dokunulmadı)
**v14 neden var:** Devir notu 3 (orkestratör, 28.09 23:10). Tasarım hakemi t3 KALDI verdi. İki bulgu vardı: **(1) KRİTİK:** olcek.css, PRO Müşteriler (/cariler) tablosunu kabından taşırıyordu (1440'ta "Düzenle" kesik, ⋯ ekran dışında). **(2) YÜKSEK:** BASİT satışta 7 haneli tutar 1440'ta hâlâ kesikti. Ağaçtaki iş korundu, iki bulgu yalnız `olcek.css`'te kapatıldı. Başka şeridin dosyasına dokunulmadı.
**Devralma:** 23:36'da ağaçta yarım mutasyon yoktu (`olcek.css` sha1 af1bc8a0…, yedeği `v14/yedek/olcek-v13.css`). 5186 ve 5187 boştu. Başlangıçta kiracı sayısı 18, PAKET-OLCEK kiracısı 0.

## v14-0. Bulguların kapanışı

| Bulgu | Durum | Kanıt (gerçek Chrome, başsız, CDP) |
|---|---|---|
| **[KRİTİK] PRO /cariler tablosu 1440'ta taşıyor** | **KAPANDI** | Kök neden ölçüldü (`kolx.mjs`/`k3.js`). Sütunlar en az içerik genişliğindeydi. En geniş iki sayı sütunu tutarla rozeti YAN YANA taşıyordu: "10.307,76 ₺ Alacak" 147 px, "-9,1600 has Borç" 136 px (app.css `.cari-bakiye` inline-flex nowrap). Yeni kural **olcek.css (10g)**: masaüstünde (≥ 769) bakiye kabı sarabilir; yer yetmezse rozet tutarın altına iner, tutar kırılmaz. Satır boyu 61'de kaldı. **Önce:** tablo 1196 > kap 1122, ⋯ sağ kenarı 1481 > 1440. **Sonra:** tablo **1122 = kap**, ⋯ sağ kenarı **1407**. Bu sonuç 1440 açık ve 1440 koyu temada aynı çıktı. BASİT 1122 = kap (değişmedi). sikisma-olc K1–K12 hepsi **0**. |
| **[YÜKSEK] BASİT satış, 1440'ta 7 haneli tutar kesik** | **KAPANDI** | Tutar sütunu 8,5 kattan **9,5 kata** çıktı (136 → 152 px). `#islem-bacak-0-tutar` "1.234.567,89": **önce** görünür 121 < içerik 127 (olcek kapalıyken de 97 < 114); **sonra 137/137, kesik yok**. Bu genişletme ikinci bir sorunu görünür yaptı: SATIS-3'ün araç seçicisindeki `min-width:150px` satıra sığmıyordu (150 + 152 + 44 + 16 = 362 > 332). Seçici tutar kutusunun üstüne biniyordu; 8,5 katta da 12 px biniyordu, bu paketten önce de vardı. Çözüm: aynı ≥1400 ızgarasında seçici sütununa uyar (min-width 0). Uzun ad kelime sınırında iki satıra sarar ("Döviz / (efektif)", 37 px < 44). Bu, SATIS-3'ün 375 px'te kullandığı kalıbın aynısıdır. Ölçüm: Nakit seçiliyken seçici sağ kenarı 893, tutar sol kenarı 901, **binme yok**. Döviz (efektif) seçiliyken metin 78/78, yükseklik 37, binme yok (`v14/doviz-1440.png`, görüldü). |
| 1280 / 960 PRO /cariler | **Paket kaynaklı değil; kalan kısım devredildi** | A/B ölçümü, 1280: olcek **açık 1085**, olcek **kapalı 1104**, kap 962. 8 sütunlu PRO tablosu olcek.css olmadan da taşıyor. Paket bu taşmayı artık 19 px AZALTIYOR (v13'te +89 px artırıyordu). Tablo kendi kabında kayıyor, sayfa yatay kaydırması 0. Satır eylemlerinin dar ekranda "⋯" altında toplanması Cariler/SatirEylemleri (KATMAN) işi → § v14-8. |
| [DÜŞÜK, hakem] `/cariler td "—"` kalınlık 400↔600 | **Ölçüm aracı yanlış pozitifi** | Karşılaştırma anahtarı hücre METNİdir. BASİT'te ilk "—" telefon hücresi (Peşin Müşteri, 400). PRO'da ilk "—" DÖVİZ BAKİYE hücresi (`hucre-vurgu sayi`, 600). Farklı iki hücre. Aynı hücrenin iki modda farklı çizildiğine dair bir ölçüm yok. v13 ile aynı. |

## v14-1. Değişen dosyalar (bu tur)
- `dotnet/Kuyumcu.Web/wwwroot/css/olcek.css` (819 → 852 satır):
  - **(10g)** yeni: `@media (min-width:769px) html[data-mod] .ana-icerik .veri-tablosu.tablo-sik--kimlik > tbody > tr > td .cari-bakiye { flex-wrap: wrap; justify-content: flex-end; row-gap: 2px }`.
  - **(7c)** tutar sütunu `* 8.5` → `* 9.5`. Aynı ≥1400 bloğuna araç seçicisi için `min-width: 0` ve `.secici-metin` kuralı eklendi (white-space normal · overflow-wrap normal · text-overflow clip · line-height 1.15).
  - Her kuralın başında ölçümlü gerekçe yorumu var. Renk yazılmadı (OL6).
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs`:
  - **OL18** güncellendi (9,5 kat).
  - **OL27** yeni: `OL27_v14_cariler_bakiye_rozeti_sarar_ve_odeme_bacagi_secicisi_tutara_binmez`. Test şunları sınar: bakiye kuralının değeri, medyası ve app.css'ten özgül olduğu; row-gap 2 px; seçici kuralının min-width 0 değeri, ≥1400 medyası ve SATIS-3 kuralından özgül olduğu; metin sarma ve satır yüksekliği.
  - Test sayısı 29 → **30**.
- belirtecler.css, index.html, denetim.sh, envanter, razor, sözlük, uç, migration: **dokunulmadı**. **Dokunulan sayfalar: yok.**

`git status --porcelain | grep -v -E "/(bin|obj)/"`, paketin satırları (Kuyumcu.Tests dizini izlenmiyor):
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html        (v7'den; bu tur değişmedi)
?? dotnet/Kuyumcu.Tests/                        (OlcekTekTestleri.cs, OlcekYuzEnvanteri.txt)
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css (bu tur değişmedi, sha1 949ac1c0…)
?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css     (sha1 1462850f…)
?? dotnet/denetim.sh                            (bu tur değişmedi)
?? dotnet/denetim.sh.mutbak                     (0 bayt, 18.09; bu paketin DEĞİL)
```

## v14-2. Derleme ve testler
- `dotnet build Kuyumcu.sln --nologo`: **0 Hata**, 1 Uyarı. Uyarı MSB3026: başka bir sürecin kilitlediği Tests.dll'in kopyası yeniden denendi. Bu kopyalama gerçekleşmedi, DLL eski kaldı; bu yüzden `Kuyumcu.Tests.csproj` ayrıca derlendi: **0 Hata**, 1 Uyarı `xUnit1031` `EtiketEkranTestleri.cs(145,47)`. Bu dosya bu paketin değil.
- Filtreli `FullyQualifiedName~OlcekTek`: **30/30 geçti** (`v14/t2.txt`).
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pOLCEK-1/v14/_Mac_2026-09-29_00_04_50.trx`): **6825 test · 6824 geçti · 1 başarısız**. Tek kırmızı `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1). Devir notundaki listede var, v9–v13'te de kırmızıydı. **Madde (7): bu paketin ürettiği yeni kırmızı 0.**

## v14-3. Mutasyon kanıtları (`v14/mut.sh`; yedek, fark ve günlük `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/<kod>/`; her koşumda filtreli test ve denetim.sh)
| Kod | Değişiklik (tek eşleşme) | Kırmızı test | Canlı etki | cmp |
|---|---|---|---|---|
| **M-OLCEK-1-62** | (10g) `flex-wrap: wrap` → `nowrap` | `OL27_v14_cariler_bakiye_rozeti_sarar_ve_odeme_bacagi_secicisi_tutara_binmez` | PRO /cariler 1440: tablo **1196** > kap 1122, ⋯ sağ **1481** (bulgu birebir geri geldi). BASİT 1122 (`canli.txt`) | AYNI |
| **M-OLCEK-1-63** | (7c) `* 9.5)` → `* 8.5)` | `OL18_islemler_dolu_fis_sil_dugmeleri_ve_tutar_sutunu_tek_olcu` | (v14-0 önce ölçümü: 121 < 127) | AYNI |
| **M-OLCEK-1-64** | seçici `min-width: 0` → `150px` | `OL27_v14_…` | (v14-0 önce ölçümü: binme) | AYNI |
| **M-OLCEK-1-1-v14** (spesifikasyon) | `belirtecler.css` PRO bloğuna `--kontrol-yuksekligi: 2rem` | `OL1_belirteclerde_moda_ozel_blok_YALNIZ_mod_isareti_tasir`, `OL2_hicbir_css_dosyasi_moda_ozel_OLCU_yazmaz_belirtec_ya_da_dogrudan` · denetim `✗ OLCEK-1: MOD_OLCU_BILDIRIMI 1` | /emanet kontrol boyları BASİT {44:1, 52:2} ↔ PRO **{39:1, 52:2}**. Mutasyonsuz tabanda iki mod da {44:1, 52:2}. | AYNI |

Test açığı yok. Her mutasyon koşumunda denetim ayrıca `P-11` sayacında da kırmızıydı. Bu sayaç mutasyonla ilgisiz, açıklaması § v14-5'te. Son mutasyondan sonra ağaç yedekle birebir aynı: olcek.css sha1 1462850f…, belirtecler.css `cmp` AYNI.

## v14-4. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; kiracı `PAKET-OLCEK-1N-TEST`, 01M3MVVRSSBFSW6ST4M8G0ZTN7; `v14/olc.sh` SIRALI; çıktılar `/private/tmp/kuyumcu-serit/OLCEK-1/v14/`)
Kiracı API ile kuruldu:
- Davet 201, kayıt 201, giriş 200.
- 6 uzun adlı müşteri `/cari` ile açıldı: 201 ×6.
- Ürün açılış stoğu 400 döndü ("en az bir kur girişi gerekir"; v10–v13'te de aynıydı).
- `demo-tohumla.js --prova-kiraci`: rc=1. Betiğin kendi V8 sayım kontrolü, önceden açılan 4 stoksuz üründe tuttu; bu v13 ile aynı ve ölçümü etkilemiyor.

Oturum parolasız açıldı (`inceleme-vekili.py` 5187). Sistem anahtarı tek kullanımlıktı, rastgele üretildi, dosyası chmod 600'dü ve iş sonunda boşaltıldı.

### v14-4.1 İki bulgu, her genişlik × mod × tema (`iki.mjs` + `sikisma-olc.js` `__olc`; `v14/olc/iki-<w>-<tema>.txt`)
| Genişlik · tema | PRO /cariler tablo / kap · ⋯ sağ | BASİT /cariler | BASİT 7 hane tutar (görünür/içerik) | sikisma K1·K3·K5·K9·K10·K11 (cariler, işlemler × 2 mod) | Sayfa yatay |
|---|---|---|---|---|---|
| 1440 açık | **1122/1122 · 1407** | 1122/1122 | **137/137** | hepsi 0 | 0 |
| 1440 koyu | **1122/1122 · 1407** | 1122/1122 | **137/137** | hepsi 0 | 0 |
| 1280 açık | 1085/962 (kendi kabında kayar; olcek kapalı 1104) | 962/962 | 206/206 (yığın düzeni) | hepsi 0; K7 BASİT işlemler 1 (SATIS-3 yığını, v13'te de var) | 0 |
| 1280 koyu | 1085/962 | 962/962 | 206/206 | aynı | 0 |
| 960 açık | 1085/642 (kayar) | 768/642 (kayar; v13'te de var) | 212/212 | hepsi 0 | 0 |
| 960 koyu | 1085/642 | 768/642 | 212/212 | hepsi 0 | 0 |
| 375 açık | 1279/349 (kayar, telefon tablo) | 349/349 (kart yığını) | 206/206 | hepsi 0 | 0 |
| 375 koyu | 1279/349 | 349/349 | 206/206 | hepsi 0 | 0 |

K2 (en sağ kart / ana alan): cariler 97–98, işlemler BASİT 1440 73, 1280 69 (sağ panel yan yana, dar sütun değil), PRO 97–98. K4 ≤ 1,4 masaüstü, telefonda 1,8–3 (yığın). PRO satışta `#islem-bacak-0-tutar` bu akışta açılmadı (PRO ödeme katmanı). Hakemin ölçümüne göre PRO kutusu 214 px'te sığıyor. Kural yalnız BASİT (`.tuval-tezgah`) ızgarasına dokunuyor.

### v14-4.2 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 41 rota; `v14/son/tut/w1440`, `w960`)
- **1440: 12 satır. v13 ile satır satır AYNI** (`diff` boş): /cariler td "—" ×1 (yanlış pozitif, § v14-0) ve /urunler satır boyu 61→80 ×11. /urunler'de PRO fazladan barkod, grup ve durum sütunlarını gösterdiği için ürün adı sarıyor; bu içerik farkı v13'te de vardı, sahibi aşağıda.
- **960: 5 satır, v13 ile AYNI** (/cariler td "—" ×1, /fiyatlandırma içerik sarması ×4).
- Düğme dağılımı 1440 v13 ile birebir aynı: sınıfsız 34/44, birincil/ikincil 44, küçük 34, büyük 52, istisna olarak şerit `btn-kucuk` 44 ×2 ve 34 ×1 (DUGME-2 devri).
- **Bu turun eklediği fark: 0.**

### v14-4.3 Hakem senaryosu (`sen.mjs`: dolu fiş, ödeme böl/sil, kaydet hatası, müşteri çekmecesi + hata, Emanet al, kâr-zarar)
- 1440 açık: TOPLAM_FARK **7** (v13: 9). Kalan: `#islem-cari` sağ dolgu ×5 (SATIS-3, gerekçeli, envanterde) ve "—" ×2. Kesik **0**, yatay 0.
- 1280 koyu: 7 (aynı tür farklar).
- 375 açık: 19, 375 koyu: 23. **v13 ile aynı türler:**
  - ≤600'de fiş kalem hücresi iç dolgusu 4↔8 (SATIS-3),
  - `#cekmece-cari-baslik` 56↔28,
  - `#islem-cari` sağ dolgu.
  - Koyuda aynı dolgu farkı bir hücrede daha görünüyor; bunun nedeni verideki fiyat metni.
- 44 px altı dokunma hedefi: senaryoda 0.

### v14-4.4 Düğme ve yazı ailesi (`prob.mjs`, 8 rota × 6 yapılandırma)
- Aile dışı boy **0**, degerUymaz **0**, yatay **0**. Masaüstü boyları {34, 44, 52}, telefon {44, 52}.
- Tek kayıt, 375 PRO /islemler `#islem-diger-ac` 54 px: iki satıra saran "Diğer ▾ ürün ayır · kapora…" açıcısı. **v13'te de aynıydı** (SATIS-3 alanı).

### v14-4.5 Ekran görüntüleri (Read ile GÖRÜLDÜ)
`ham/TUVAL-KARSILASTIRMA/OLCEK-1-v14-{cariler,islemler-7hane}-{1440,1280,960,375}-{aydinlik,karanlik}.png`: 16 dosya, solda BASİT, sağda PRO. Bakılanlar:
- `v14/olc/1440-aydinlik/Profesyonel-1440-aydinlik-cariler.png`:
  - Tahsilat, Düzenle ve ⋯ ekranın içinde.
  - Borç ve Alacak rozetleri tutarın altında, sağa yaslı; satır boyu değişmedi.
  - Uzun adlar ("Anadolu Döküm Toptan / Kuyum") kelime sınırında iki satır; kırpma yok.
- `OLCEK-1-v14-islemler-7hane-1440-karanlik.png`:
  - BASİT'te "Nakit ▾" ve "1.234.567,89 ₺" ayrı kutular; tutar tam okunuyor, binme yok.
  - Sil düğmesi 44 px.
- `v14/doviz-1440.png`: "Döviz (efektif)" seçicide iki satır, tutar kutusuyla binme yok.
- `v14/iki-once-kirp.png` ↔ `iki-sonra-kirp.png`: önceki "1.234.567,8" kesiği ve seçici binmesi ↔ sonrası.
- `OLCEK-1-v14-cariler-1280-karanlik.png`: PRO tabloda "Düzenle" sağ kenarda yarım görünüyor ve tablo yatay kayıyor. Bu, paketten önce de vardı (A/B 1104 > 962) → § v14-8.

## v14-5. Denetim (`bash dotnet/denetim.sh`, son değişiklikten sonra; `v14/den1.txt`)
Sonuç: **DENETİM BAŞARISIZ, tek ✗: `P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > tavan 47`.**
- Bu sayaç yalnız `Pages/*.razor`, `Bilesenler/*.razor` ve `Layout/*.razor` dosyalarını sayıyor (denetim.sh:2842). Bu paket razor dosyasına dokunmadı.
- Son değişen razor dosyaları: `PosTahsilatlar.razor` 29.09 00:03 (ZO-ISLEM-A) ve `Islemler.razor` 28.09 23:50 (SATIS-3). Artış bu şeritlerden birine ait (orkestratör kapısı).
- OLCEK-1 sayaçları: `MOD_OLCU_BILDIRIMI` **0** ✓ · `YUZ_ONEMLI_OLCU` **0** ✓ · `YUZ_OLCU_ENVANTER_DISI` **0** ✓ (madde 3 sağlandı).

## v14-6. DoD sözleşmesi
- Yeni varlık, kayıt türü, uç, form ya da düğme yok. Değişiklik yalnız olcek.css yerleşim/ölçü kurallarında ve testte. Bu yüzden PATCH/DELETE, [RolGerekli] ve denetim kaydı maddeleri uygulanamaz.
- Migration gerekmedi. ModalPencere sayacı ve sayfa dosyaları değişmedi.
- Mevzuat içeriği ve fiyat/teklif metni yok. `app.css`'e ve başka şeridin dosyasına yazılmadı; SATIS-3 kuralı olcek.css'te daha özgül seçiciyle ezildi.

## v14-7. Temizlik
- API 5186 ve vekil 5187 `kill` ile durduruldu; `lsof` 5186/5187 **boş**. Başsız Chrome süreçleri (v14 profilleri) durduruldu, kalan 0.
- **Kiracı:** `zsh kiraci-temizle.sh 01M3MVVRSSBFSW6ST4M8G0ZTN7 --uygula` çalıştırıldı (zsh ile). Ardından kontrol edildi:
  - `information_schema`'daki tüm `tenant_id` tablolarında bu kiracıya ait satır toplamı **0**.
  - Davet satırı `delete … like 'PAKET-OLCEK-1N-TEST%'` → 1 satır silindi.
  - `PAKET-OLCEK%` kiracı **0**. Kiracı sayısı başta 18, kiracım açıkken 17 (bu arada başka şeritler silmiş), sonra **16**.
  - QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- Sistem anahtarı ve fikstür `printf '' >` ile boşaltıldı (0 bayt).
- **DİSK (yapılamadı, kurucuya):** Profil ve kopya ağacını silmek istedim; `rm -rf` TurkReport koruması tarafından BLOKLANDI (hook çıktısı: "destructive komut bloklandi"). Silinecekler:
  - `/private/tmp/kuyumcu-serit/OLCEK-1/v14/**/profil-*` ve `son/tut/*/p` (v14 dizini toplam 4,1 GB).
  - Önceki turların kopya ağacı ve profilleri `/private/tmp/kuyumcu-serit/OLCEK-1/` (toplam 7,2 GB).
  - Kurucu terminalde silebilir. Proje ağacına geçici dosya bırakılmadı.

## v14-8. Açık kalanlar (sahibiyle)
1. **KATMAN (SatirEylemleri) / Cariler:**
   - 1280 ve 960'ta PRO /cariler tablosu (8 sütun) kabından geniş: 1085 > 962. Olcek kapalıyken 1104; bu paket taşmayı azaltıyor, yaratmıyor. Satır eylemleri kayarak görünüyor.
   - Kalıcı çözüm: <1400'de Tahsilat ve Düzenle'nin "⋯" menüsüne toplanması ya da eylem sütununun yapışkan yapılması. Yapışkan sütun zemin rengi ister; olcek.css renk yazamaz (OL6).
   - Emoji ikonlar (💵 ✏️ 📦) → Ikon.
2. **Urunler sahibi:** 1440 PRO /urunler'de ürün adı 3 satıra sarıyor, satır 80 (BASİT 61). Neden: barkod, grup ve durum sütunları. v13'te de aynıydı (tutarlılık 11 satır).
3. **SATIS-3:**
   - 1280 BASİT ödeme bacağı yığını (K7 1).
   - ≤600'de fiş kalem dolgusu 4↔8.
   - `#islem-cari` rozet payı.
   - 375 PRO `#islem-diger-ac` 54 px.
   - Araç seçicisindeki `min-width:150px`, ≥1400 BASİT ızgarasında olcek.css (7c) ile eziliyor. SATIS-3 kendi ızgarasını 9,5 katlık tutar sütununa göre kurarsa bu köprü kalkar.
4. **ORTAK-1:** Tam süitte `OrtakGorunumSaflik.O8` kırmızı.
5. **ZO-ISLEM-A / SATIS-3 (orkestratör kapısı):** denetim `P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > 47` (razor sayacı).
6. **Başka şerit:** `EtiketEkranTestleri.cs(145,47)` xUnit1031 uyarısı.
7. **DUGME-2:** şerit `btn-kucuk` 44 ×2 (/faturalar, /kur) ve 34 ×1 (/urunler/agac). Değişmedi.
8. **Kurucu:** `/private/tmp/kuyumcu-serit/OLCEK-1` (7,2 GB) silinmeli (hook engeli). `dotnet/denetim.sh.mutbak` (0 bayt) silinmeli. Kenar menüsü PRO yüzü kararı.

## v14-9. Kurucuya not
- Profesyonel Müşteriler listesinde, standart masaüstü genişliğinde (1440) Tahsilat, Düzenle ve ⋯ artık ekranda tam görünüyor. Borç ve Alacak etiketi yer darsa tutarın altına iniyor; tutar hiç kesilmiyor.
- Satış ekranında (Basit) 1.234.567,89 ₺ gibi 7 haneli tutar kutuda tam okunuyor. Ödeme türü kutusu artık tutarın üstüne binmiyor; "Döviz (efektif)" gibi uzun ad iki satıra iniyor.
- 1280 genişlikte Profesyonel müşteri tablosu hâlâ sağa kayıyor. Bu paketten önce de böyleydi; eylemleri "⋯" altında toplamak satır eylemleri bileşeninin işi, ilgili şeride devredildi.

---

# Ek: v13 raporu (22:12–22:58, değiştirilmeden)

## (v13 başlığı) Uygulama raporu v13

**Tarih:** 2026-09-28 22:12–22:58 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9955/9956/9963; 5198/5199/5200 ve tarayıcı bölmesine dokunulmadı)
**v13 neden var:** Son doğrulama GEÇTİ verdi, ancak çapraz hakem KRİTİK bir çelişki yazdı. Doğrulayıcının M3 mutasyonu (`.tuval-pro #islem-kaydet { height: 32px; min-height: 32px; font-size: 13px; }`) hiçbir OLCEK testini ve denetim.sh'ı kırmıyordu. Oysa bu, kurucunun şikâyet ettiği hata türünün ta kendisi ("aynı düğme PRO'da küçülüyor"). v12'nin koşul tablosu bu açığı yazmıyordu. Bu turda açık kapatıldı, sayı yeniden ölçüldü, mutasyonlar ve canlı ölçüm güncel ağaçta yeniden koşuldu. Paketin CSS'i (olcek.css, belirtecler.css) değişmedi.
**Devralma:** 22:12'de doğrulayıcının mutasyonları (dog-olcek1-mut1..3, 22:03–22:04) bitmişti. Ağaçta yarım mutasyon YOK. `olcek.css` ↔ `v12/olcek-v12b.css` `cmp` AYNI. belirtecler.css'te PRO bloğunda `--kontrol-yuksekligi` yok (tek eşleşme 2163. satırda: kullanıcı tercihi `data-yogunluk="sik"`). tuval-satis.css:571 `.tuval-pro #islem-kaydet { padding: 0 22px; font-size: var(--tp-16); font-weight: 600 }` SATIS-3'ün asıl satırı (M3 artığı değil). 5186 ve 5187 boştu.

## v13-0. Bulguların kapanışı

| Bulgu | Durum | Kanıt |
|---|---|---|
| **[KRİTİK] Hakem çelişkisi / [ORTA] statik koruma yüz sınıfı + ID özgüllüğünü görmüyor (M3)** | **KAPANDI**: yeni kapı OL26 + denetim `OLCEK-1: YUZ_OLCU_ENVANTER_DISI` | Kural: olcek.css dışındaki css'lerde, mod yüzü sınıfı (`.tuval-pro`, `.tuval-tezgah`, `.*sade`) taşıyan bir seçici parçası KONTROL öznesine (#kimlik, button/input/select/textarea/summary, `.btn*`, `.girdi*`, `.secici-tetik`, `.sekme`, `.ekran-karo`, `.fis-salt-kutu`) ölçü yazıyorsa (height, min/max-height, padding*, font, font-size, font-weight, line-height, köşe yarıçapı), satır adıyla onaylı envanterde birebir bulunmalı: `dotnet/Kuyumcu.Tests/OlcekYuzEnvanteri.txt` (215 satır; biçim `dosya \| @kural \| seçici \| özellik \| değer`). Yeni ya da DEĞERİ değişmiş satır ihlal sayılır. **Hakemin M3'ü birebir yeniden koşuldu: OL26 KIRMIZI, denetim `YUZ_OLCU_ENVANTER_DISI 3` → "DENETİM BAŞARISIZ"** (§ v13-3). İki rapor artık aynı gerçeği yazıyor: v12 ağacında M3 → 0 kırmızı, v13 ağacında M3 → 1 test + 3 denetim ihlali. |
| [DÜŞÜK] `#islem-cari` sağ dolgu 44 ↔ 197–258 px | **GEREKÇELİ BIRAKILDI (SATIS-3 alanı)** | v13 ölçümü: sen 1440 `pad 0 44 0 12 ↔ 0 258.27 0 12` · 375 `↔ 197.45`. Boy 44, yazı, köşe ve kalınlık iki modda aynı. Kaynak `tuval-satis.css` `.tuval-pro #islem-ust-satir > .alan:first-child:has(> .rozet-satir) .secici-tetik { padding-right: calc(46% + 36px) }`: PRO'da borç rozeti kutunun İÇİNDE durur. Bu bildirim artık envanterde adıyla duruyor. Değeri değişirse OL26 kırılır. Rozet payının belirteçle tek tanıma bağlanması SATIS-3'e devredildi (§ v13-8). |
| [DÜŞÜK] /islemler BASİT 1280 kasa seçici yer tutucusu "Kasa seçi…" kesik | **ALAN DIŞI, devredildi** | Ödeme bacağı yerleşimi (SATIS-3/ZO-ISLEM). Paketin ölçü kuralının bunu doğurduğuna dair kanıt yok. `sen.mjs` kesik ölçümü (input/td) 6 genişlik/temada **0**. Yer tutucu metni K10'da sayılmıyor. |
| Devir notu 2 (18:45): `/cariler` PRO telefon açıcı 44 px'te 15 px · § 6 üst çubuk sabit px | v11/v12'de kapanmıştı, durum korundu | § 6 belirteçlere bağlı (`--yazi-kontrol-kucuk`, `--yazi-kur-deger`, `--yazi-kontrol`). § 12b gerekçesi (`.si-tetik` yazısı hücreden gelir) ve OL24 yerinde. `prob.mjs` `degerUymaz` 12 yapılandırmada **0**. |

## v13-1. Değişen dosyalar (bu tur)
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs`: **OL26** `OL26_v13_mod_yuzu_sinifi_kontrol_olcusu_YALNIZ_onayli_envanterden` ve **OL26b** `OL26b_envanter_dedektoru_hakemin_M3_ornegini_yakalar` eklendi. OL26b geçici dizinde şunları sınar: hakemin M3'ü 3 ihlal verir; `:has()` içi özne sayılmaz; kontrol olmayan, yüz sınıfı olmayan ve ölçü olmayan bildirimler sayılmaz; onaylı satırın değeri değişince ihlal çıkar. Yardımcılar: `Ozne`, `YuzKontrolOlcuSatirlari`, `YuzEnvanteri`, `YuzEnvanterDisi`. Test sayısı 27 → 29.
- `dotnet/Kuyumcu.Tests/OlcekYuzEnvanteri.txt` (**yeni**): 215 onaylı satır (tuval-satis.css 184, tuval-panel-musteri.css 31) ve başlıkta kural, onay dayanağı ve satır ekleme yolu.
- `dotnet/denetim.sh`: OLCEK-1 bloğuna v7 eklendi: `olcek1_env_say` (OL26'nın Python ikizi, aynı envanter dosyası) → `OLCEK-1: YUZ_OLCU_ENVANTER_DISI`.
- CSS, razor, sözlük, uç, migration: **yok**. Dokunulan sayfalar: **yok**.

`git status --porcelain | grep -v -E "/(bin|obj)/"` (paketin dosyaları; Kuyumcu.Tests dizini izlenmiyor):
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Tests/            (OlcekTekTestleri.cs, OlcekYuzEnvanteri.txt dahil)
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css
?? dotnet/denetim.sh
?? dotnet/denetim.sh.mutbak         (0 bayt, 18.09; bu paketin DEĞİL, silme kurucuda)
```

## v13-2. Derleme ve testler
- `dotnet build Kuyumcu.sln --nologo`: **0 Hata · 0 Uyarı**. Değişiklikten sonra ve son mutasyondan sonra ayrı ayrı derlendi (`v13/build1.txt`, `build2.txt`).
- Filtreli `FullyQualifiedName~OlcekTek`: **29/29 geçti** (`v13/t2.txt`).
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pOLCEK-1/v13/_Mac_2026-09-28_22_23_13.trx`, `v13/tam.log`): **6759 test · 6758 geçti · 1 başarısız**. Tek kırmızı `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1'in; v9–v12'de de kırmızıydı, CSS okumuyor). **Madde (7): bu paketin ürettiği yeni kırmızı 0.** v12'deki SATIS-3 Tur7 ve ZO-ISLEM-A POS kırmızıları bu koşumda yeşil.

## v13-3. Mutasyon kanıtları (`v13/mut.sh`; yedek + günlük `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/<kod>/`; her koşum filtreli test + tam denetim.sh; dosya penceresinde başka şerit değişikliği yok: sha1 önce = sonra)
| Kod | Değişiklik (tek eşleşme) | Kırmızı | cmp |
|---|---|---|---|
| **M-OLCEK-1-59** (hakemin M3'ü, birebir) | `tuval-satis.css` sonuna `.tuval-pro #islem-kaydet { height: 32px; min-height: 32px; font-size: 13px; }` | `OL26_v13_mod_yuzu_sinifi_kontrol_olcusu_YALNIZ_onayli_envanterden` · denetim `✗ YUZ_OLCU_ENVANTER_DISI 3` → DENETİM BAŞARISIZ | AYNI |
| M-OLCEK-1-60 | Envanterden `.tuval-pro #islem-kaydet \| padding \| 0 22px` satırı silindi | `OL26_…` · denetim `✗ … 1` | AYNI |
| M-OLCEK-1-61 | `tuval-satis.css:571` `.tuval-pro #islem-kaydet { padding: 0 22px` → `0 10px` (yalnız PRO'da değer değişimi) | `OL26_…` · denetim `✗ … 1` | AYNI |
| **M-OLCEK-1-1-v13** (spesifikasyon) | `belirtecler.css` PRO bloğuna `--kontrol-yuksekligi: 2rem` | `OL1_belirteclerde_moda_ozel_blok_YALNIZ_mod_isareti_tasir`, `OL2_hicbir_css_dosyasi_moda_ozel_OLCU_yazmaz_belirtec_ya_da_dogrudan` · denetim `✗ MOD_OLCU_BILDIRIMI 1` | AYNI |
Test açığı yok. **Dürüst not:** M3'ün CANLI etkisini bu turda ölçmedim. olcek.css § 4 `html[data-mod] .tuval-pro .islem-alt-satir > #islem-kaydet` (özgüllük 1,3,1 > M3'ün 1,1,0) height, min-height ve font-size yazdığı için büyük olasılıkla ezilir, ama bu bir çıkarımdır. Kapı bunu beklemeden M3'ü satır olarak reddeder. Canlı fark (M-1 örneği) v12'de ölçülmüştü: taban 1 → mutasyonla 12 fark.

## v13-4. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; kiracı `PAKET-OLCEK-1M-TEST`, 01M3MQG78JE5SPPRFNZHY79VNY; `v13/olc.sh` SIRALI; tüm çıktılar `/private/tmp/kuyumcu-serit/OLCEK-1/v13/`)
Kiracı API ile kuruldu: davet 201 / kayıt 201 / giriş 200, 6 uzun adlı müşteri `/cari` 201 ×6. Ürün açılış stoğu 400 döndü ("en az bir kur girişi gerekir", v10–v12 ile aynı). Ardından `demo-tohumla.js --prova-kiraci` çalıştı; betiğin V8 sayım kontrolü stoksuz 4 üründe rc=1 verdi (v12 ile aynı, ölçümü etkilemez).

### v13-4.1 Düğme ve yazı ailesi (`prob.mjs`, 8 rota: /, /cariler, /urunler, /emanet, /islemler, /raporlar/kar-zarar, /bildirimler, /vezne)
| Genişlik · tema | BASİT boylar | PRO boylar | aile dışı | yaziAile ihlali | değer açıcısı ≠ hücre | yatay |
|---|---|---|---|---|---|---|
| 1440 açık | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 |
| 1440 koyu | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 |
| 1280 açık | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 |
| 960 açık | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 |
| 375 açık | 44,52 | 44,52 (+54) | 0 (44 altı 0) | PRO `#islem-diger-ac` 54/16 (iki satırlı özet açıcısı, v8 gerekçesi) | 0 | 0 |
| 375 koyu | 44,52 | 44,52 (+54) | 0 (44 altı 0) | aynı | 0 | 0 |
v12 ile birebir aynı.

### v13-4.2 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 41 rota): `v13/son/tut/w*/`
| Genişlik | v13 fark | v12 fark | `diff` (sıralı) | İçerik |
|---|---|---|---|---|
| 1440 | 12 | 12 | AYNI | 1 anahtar çakışması (`/cariler td:—`: BASİT telefon ↔ PRO has hücresi, farklı öğeler) + 11 `/urunler` satır 61↔80 (uzun adın PRO'daki fazladan sütunlarla sarması) |
| 1280 | 1 | 1 | AYNI | anahtar çakışması |
| 960 | 5 | 5 | AYNI | 1 çakışma + 4 `/fiyatlandirma` 164↔215 (PRO fazladan sütun) |
| 375 | 27 | 27 | AYNI | `/cariler` BASİT kart yığını ↔ PRO kendi kabında kayan tablo |
**Ölçü kuralından doğan fark: 0.**

### v13-4.3 Hakem senaryosu (`sen.mjs`: dolu fiş, ödeme böl/sil, kaydet hatası, müşteri çekmecesi + hata, Emanet al, kâr-zarar)
| Genişlik · tema | Fark satırı | Kesik | 375'te 44 altı | Yatay | Boylar |
|---|---|---|---|---|---|
| 1440 açık | 9 | 0 | — | 0 | 34,44,52 |
| 1440 koyu | 9 | 0 | — | 0 | 34,44,52 |
| 1280 açık | 10 | 0 | — | 0 | 34,44,52 |
| 960 açık | 13 | 0 | — | 0 | 34,44,52 |
| 375 açık | 19 | 0 | **0** | 0 | 44,52 (PRO +54) |
| 375 koyu | 19 | 0 | **0** | 0 | 44,52 (PRO +54) |
Fark kaynakları v12 ile aynı. Yeni kaynak YOK:
- 5× `#islem-cari` dolgusu (gerekçeli),
- uzun ad sarması 61↔75,
- `/cariler td:—` çakışması,
- 375'te fiş kalemi iç dolgusu BASİT 4 ↔ PRO 8 (`tuval-satis.css`, SATIS-3).

PRO "ödeme böl" adımında `#islem-bacak-ekle` bulunamadı (v12'de de aynıydı; senaryo betiği sınırı, PRO ödeme düzeni farklı).

### v13-4.4 Ekran görüntüleri (Read ile GÖRÜLDÜ)
`ham/TUVAL-KARSILASTIRMA/OLCEK-1-v13-{emanet,cariler,islemler,kar-zarar}-{1440-aydinlik,1440-karanlik,1280-aydinlik,960-aydinlik,375-aydinlik,375-karanlik}.png` (24 dosya, solda BASİT, sağda PRO). Bakılanlar:
- `…-emanet-1440-aydinlik.png`: Müşteri seçici, "Emanet al" ve "Emanet iade et" iki modda aynı boy ve yazıda. Kurucunun şikâyeti kapalı.
- `…-islemler-375-karanlik.png`: BASİT karo ↔ PRO sekme, orkestratör kararıyla iki ayrı bileşen. Müşteri kutusu ve arama kutusu iki modda aynı boy. Yatay kaydırma yok.
- `…-cariler-960-aydinlik.png`: arama, "Yeni müşteri/Yeni Cari" ve tablo satır yüksekliği iki modda aynı. **Not:** 960'ta iki modda da tablo kendi kabında sağdan kırpılıyor (BASİT'te ikinci satır eylemi, PRO'da "HAS" sütunu). Sayfa yatay kaydırması 0. Tablo sarmalayıcısı kaydırmalı. Paketten önce de vardı → § v13-8 (ORTAK-1/Cariler sahibi).

## v13-5. Denetim (`bash dotnet/denetim.sh`, son değişiklikten sonra; `v13/den2.txt`)
Sonuç: rc=0 · **292 ✓ (v12: 291) · 0 ✗ · "DENETİM TEMİZ."**

| Sayaç | Değer |
|---|---|
| `OLCEK-1: MOD_OLCU_BILDIRIMI` | 0 |
| `OLCEK-1: YUZ_ONEMLI_OLCU` | 0 |
| **`OLCEK-1: YUZ_OLCU_ENVANTER_DISI`** | **0** (yeni) |
| `P-39: DUGME_YUKSEKLIK_CESIDI` | 2 (tavan 2) |
| `P-39: DOKUNMA_HEDEFI_KUCUK` | 0 |
| `P-39: KONTROL_YUKSEKLIK_SAPMASI` | 0 |

## v13-6. DoD sözleşmesi
Bu turda yeni varlık, kayıt türü, uç, form ya da düğme yok; değişiklik yalnız test, envanter ve denetim betiğinde. Bu yüzden PATCH/DELETE, [RolGerekli] ve denetim kaydı maddeleri uygulanamaz. Migration gerekmedi. ModalPencere sayacı ve sayfa dosyaları değişmedi. Mevzuat içeriği ve fiyat/teklif metni yok. Envanterin "düzenleme yolu" dosyanın başlığında yazılı: olcek.css kuralı + canlı ölçüm + satır + rapor.

## v13-7. Temizlik
- API 5186 (PID 10635) ve vekil 5187 `kill` ile durduruldu; `lsof` 5186/5187 **boş**. Başsız Chrome süreci kalmadı.
- **Kiracı silme, düzeltilmiş hatayla birlikte:** `kiraci-temizle.sh 01M3MQG78JE5SPPRFNZHY79VNY --uygula` betiğini yanlışlıkla `bash` ile çalıştırdım. Betik zsh gerektiriyor (`${=TABLOLAR}: bad substitution`), bu yüzden yalnız `tenants` satırı silindi, alt tablolar silinmedi. Hemen elle düzelttim: `information_schema`'da `tenant_id` kolonu olan **70 tablo**nun her birinden `delete … where tenant_id='01M3MQG7…'` (tek oturum, FK denetimi geçici kapalı). Önce **300** satır vardı, sonra **0**. Betiğin rapor kipi: "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)". Davet satırı `delete from kiraci_davetleri where isletme_adi like 'PAKET-OLCEK-1M-TEST%'` → 1 satır silindi.
- Son durum: `%OLCEK%` davet **0**, `%OLCEK%` kiracı **0**.
- Kiracı sayısı: oturum başında (22:13) **17**, ben kiracı açmadan hemen önce (22:19) **19** (aradaki artış başka şeritlerin), benimkiyle 20, silindikten sonra **18**. Yani bu arada başka şeritler de kiracı silmiş. PAKET-OLCEK kiracısı 0. QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- Fikstür ve sistem anahtarı `printf '' >` ile boşaltıldı (0 bayt). Proje ağacına geçici dosya bırakılmadı; mutasyon yedekleri `/private/tmp` altında.

## v13-8. Açık kalanlar (sahibiyle)
1. **Kurucu kararı:** kenar menüsünün PRO yüzü (KATMAN/T-K2), beyaz listede duruyor.
2. **SATIS-3:**
   - `#islem-cari` rozet payı: belirteçle tek tanıma bağlanmalı (şu an envanterde adıyla, gerekçeli).
   - ≤600 px'te BASİT fiş kalemi iç dolgusu 4 ↔ PRO 8.
   - BASİT 1280'de kasa seçici yer tutucusu kesik.
   - tuval-satis.css'teki sabit px değerleri belirtece çevrilirse olcek.css § 4/5b/10b sadeleşir.
   - **YENİ SÖZLEŞME:** tuval-satis.css ya da tuval-panel-musteri.css'te yüz sınıfıyla KONTROL ölçüsü değiştiren her düzenleme artık OL26 ve denetim `YUZ_OLCU_ENVANTER_DISI` kapısından geçer. Yol: olcek.css'te iki yüz için tek kural (ya da gerekçe) + `tutarlilik.mjs` canlı ölçüm + `OlcekYuzEnvanteri.txt` satırı + raporda gerekçe.
3. **ORTAK-1 / Cariler sahibi:**
   - Tam süitte `OrtakGorunumSaflik.O8` kırmızı.
   - 960'ta /cariler tablosu iki modda kabında kırpılıyor (kaydırmalı).
   - 375'te PRO tablo ↔ BASİT kart düzeni.
   - /bildirimler Pa960 K3.
4. **KATMAN:** PRO satır eylemlerinde emoji (📦 ✏️) → Ikon; GlobalArama 375 serbest katman.
5. **DUGME-2 / sayfa sahipleri:**
   - Şerit `btn-kucuk` 44·16 (/faturalar, /kur, /urunler/agac).
   - `#ekran-favori-*` 24 px.
   - `#id-belge-*` 26 px (ZO-BELGE).
   - Eski `ikincil` sınıflı düğmeler `btn` ailesine bağlanınca olcek.css § 10f/§ 12a köprüsü kalkar.
6. **Orkestratör:**
   - `prob.mjs` + `tutarlilik.mjs` + `sen.mjs` kapı adımı olsun (statik envanter, canlı ölçümün yerini tutmaz, yalnız sessiz geri dönüşü engeller).
   - Kapsam sınırı: envanter yalnız yüz sınıfı + kontrol öznesini kapsar. Yüz sınıfı taşımayan yeni bir mod yüzü (ör. ileride yeni bir `.xyz-pro`) regex'e eklenmedikçe görülmez.
   - `dotnet/denetim.sh.mutbak` (0 bayt) silinmeli (kurucu).
   - `kiraci-temizle.sh` bash ile çağrılınca sessizce yalnız `tenants`'ı siliyor: betiğe `#!/bin/zsh` zorlaması ya da bash uyarısı eklenmeli.

## v13-9. Kurucuya not
- Aynı düğmenin Profesyonel'de küçülmesine yol açan kural türü artık otomatik kontrolde yakalanıyor: biri satış ekranı stiline "Profesyonel'de Kaydet düğmesi 32 px" gibi bir satır eklerse testler ve denetim kırmızı yanar.
- Emanet, Müşteriler, Satış ve Kâr-zarar ekranları iki modda, 6 genişlik/temada yeniden ölçüldü. Sonuçlar dünkü ölçümle sayı sayı aynı: aynı düğme, aynı kutu, aynı boy.

---

# Ek: v12 raporu (20:51–21:55, değiştirilmeden)


**Tarih:** 2026-09-28 20:51–21:55 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9955–9963; 5198/5199/5200 ve tarayıcı bölmesine dokunulmadı)
**v12 neden var:** Doğrulayıcının son turu GEÇTİ verdi ama (a) 3 DÜŞÜK bulgu (#islem-cari dolgusu, /ayarlar mod kartı 2 px zıplaması, tema kartı 114/118) + davet artığı bıraktı, (b) çapraz hakem "doğrulayıcının bağımsız görsel kanıtı yok" KRİTİK çelişkisi yazdı. Bu tur: bulguları kapat, ölçümleri ve mutasyonları güncel ağaçta YENİDEN koş, görüntüleri Read ile gör, sayıları raporla. Kod yeniden yazılmadı.
**Devralma:** 20:36'da başlayıp 20:43'te kesilen önceki v12 koşumu `olcek.css` § 12c ve `OL25` testini yazmış, M-56/M-57/M-1 mutasyonlarını koşup geri almıştı (`cmp` AYNI, yedekler `mut-yedek/OLCEK-1/M-OLCEK-1-{56,57,1-v12}/orijinal` ↔ ağaç, bu tur yeniden doğrulandı). Yarım mutasyon YOK; kesilen koşumun API süreci (PID 86559) çalışmıyordu. Ölçüm yöntemi değişmedi: başsız Chrome + CDP (`betikler/tur/cdp.mjs`), parolasız vekil, SIRALI (`v12/olc-sira.sh`). Tüm çıktılar `/private/tmp/kuyumcu-serit/OLCEK-1/v12/`.

## v12-0. Bulguların kapanışı

| Bulgu | Durum | Kanıt (v12, güncel ağaç) |
|---|---|---|
| [DÜŞÜK] `/ayarlar` mod kartı seçiliyken 68 ↔ 66 px | **KAPANDI** — olcek.css § 12c | Kök neden `app.css` `.gk-secenek.secili { border: 2px }` ↔ 1 px. Kural: kenarlık her durumda 1 px, seçili kalınlık `box-shadow: inset 0 0 0 1px var(--vurgu)` (görünen çizgi yine 2 px). `gk.mjs` ölçümü: `#gk-mod-basit` / `#gk-mod-profesyonel` BASİT ve PRO'da **aynı boy** (1440: 66/66 · 1280: 85/66 · 960: 104/85 · 375 açık+koyu: 66/66); seçili olan değişince boy DEĞİŞMİYOR (960'ta basit seçiliyken de değilken 104). 1280/960'taki 85/104 farkı açıklama metninin dar sütunda sarmasıdır (içerik, iki modda aynı). |
| [DÜŞÜK] Tema kartları 114 / 118 | **KAPANDI** — § 12c + yeni `grid-auto-rows: 1fr` | `.gk-sema.secili { border: 3px }` ↔ 1 px → aynı kural (1 px + `inset 0 0 0 2px`). İlk ölçümde 960'ta 140↔136, 375'te 114↔111 kaldı ("Seçili" rozeti ad satırını sarıyor, seçili kartın SATIRI uzuyordu) → ızgaraya `grid-auto-rows: 1fr` eklendi. Yeniden ölçüm (`gk2-*.txt`): 8 tema kartı **tek boy** — 1440: 114 · 1280: 114 · 960: 140 · 375 açık: 114 · 375 koyu: 114; iki modda aynı; yatay kaydırma 0. |
| [DÜŞÜK] `#islem-cari` sağ dolgu 44 ↔ 197–332 px | **GEREKÇELİ BIRAKILDI — SATIS-3 alanı** | Kural `tuval-satis.css` (SATIS-3'ün dosyası): PRO'da borç rozeti kutunun İÇİNDE yer tutar. Boy 44, yazı, köşe, kalınlık iki modda AYNI (sen.mjs FARK satırında yalnız `pad`: 1440 258 · 960 332 · 375 197). Ezmek şeridin yerleşim kararını bozar → § v12-8. |
| [DÜŞÜK] Davet artığı (`PAKETOLCEK-1-TEST dogrulayici`, a59cd950-…) | **KAPANDI (bu tur başında zaten yoktu)** | 20:51 SQL: `kiraci_davetleri` `%OLCEK%` (isletme_adi/olusturan_not) ve `id like 'a59cd950%'` → **0** (orkestratör temizlemiş). Bu turun kendi davet satırı da temizlikte silindi (§ v12-7) → son `%OLCEK%` davet **0**. |
| [KRİTİK] Hakem çelişkisi — bağımsız görsel kanıt | **YENİDEN ÖLÇÜLDÜ, görüntüler Read ile GÖRÜLDÜ** | § v12-4: 8 rota × {1440 açık, 1440 koyu, 1280, 960, 375 açık, 375 koyu} × BASİT/PRO düğme/yazı ölçümü; 41 rota × {1440, 1280, 960, 375} tutarlılık; senaryo 5 genişlik/tema; sıkışma A/B. Sayılar v11 ile karşılaştırmalı. Doğrulayıcı için tekrar komutları § v12-4.6. |

## v12-1. Değişen dosyalar (bu tur)
- `dotnet/Kuyumcu.Web/wwwroot/css/olcek.css`: **§ 12c** (seçim kartları: kenarlık 1 px + içe gölge; tema ızgarası `grid-auto-rows: 1fr`). Renk yazılmadı (gölge rengi `var(--vurgu)`, OL6 yeşil).
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs`: **OL25 yeni** (`OL25_v12_ayarlar_secim_kartlari_secilince_buyumez`; 26 → 27 test).
- Dokunulan sayfalar (razor): **yok**. app.css, belirtecler.css, sözlük, uç, migration: **yok**.

`git status --porcelain | grep -v -E "/(bin|obj)/"` (paketin dosyaları):
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Tests/            (OlcekTekTestleri.cs dahil; dizin izlenmiyor)
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css
?? dotnet/denetim.sh
?? dotnet/denetim.sh.mutbak         (0 bayt, 18.09 — bu paketin DEĞİL, silme kurucuda)
```

## v12-2. Derleme ve testler
- `dotnet build Kuyumcu.sln`: başlangıç **0 Hata 0 Uyarı**; test değişikliği sonrası test projesi 0 Hata, 1 Uyarı (başka şeridin `EtiketEkranTestleri.cs(145,47) xUnit1031`); **son mutasyondan sonra temiz derleme: 0 Hata, 0 Uyarı**.
- Filtreli `FullyQualifiedName~OlcekTek`: **27/27 geçti**.
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pOLCEK-1/v12/_Mac_2026-09-28_21_47_54.trx`, `v12/tam.log`): **6731 · 6728 geçti · 3 başarısız**, hiçbiri bu paketin:
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1; v9–v11'de de kırmızı),
  - `Dogrulayici.SATIS3SinirTestleriTur7.Tur7_takas_fiyati_CIKMAYAN_urun_onceki_urunun_fiyatini_TASIMAZ` (SATIS-3 doğrulayıcısı; dosya 21:35'te değişti; ileti "u2'nin fiyatı çıkmadı ama kutuda önceki ürünün fiyatı duruyor"; CSS okumaz),
  - `Dogrulayici.ZOISLEMASinirPosTestleri.ZOS_pos_ayni_tahsilat_iki_satirla_net_tutari_asamaz` (ZO-ISLEM-A doğrulayıcısı; dosya 21:40'ta değişti; `DbUpdateException` Duplicate entry `pos_yatma_eslesmeleri`; CSS okumaz).
  - Üç dosyada `olcek|belirtecler.css` geçişi **0**. **Madde (7): bu paketin ürettiği yeni kırmızı 0.**

## v12-3. Mutasyon kanıtları (yedek + günlük `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/<kod>/`)
| Kod | Değişiklik (tek eşleşme) | Kırmızı test | cmp |
|---|---|---|---|
| **M-OLCEK-1-1** (spesifikasyon, v12r yeniden üretim) | `belirtecler.css` PRO bloğuna `--kontrol-yuksekligi: 2rem` | `OL1_belirteclerde_moda_ozel_blok_YALNIZ_mod_isareti_tasir`, `OL2_hicbir_css_dosyasi_moda_ozel_OLCU_yazmaz_belirtec_ya_da_dogrudan` · **canlı** `tutarlilik.mjs /emanet,/cariler,/islemler 1440`: taban **1** fark (`v12/tut-taban/`) → mutasyonla **12** (`v12/tut-mut1/`: `/emanet #e-cari 44→39`, `/cariler #cari-yeni-ac dolgu 10.6→4.6`, `/islemler #islem-tur-SATIS/HURDA_ALIS/SATIS_IADE/TAKAS 44→32`, `#islem-git-odeme`, `#islem-cari`, `#islem-barkod`, `#islem-son-satislar-ac` 44→32 …) | AYNI |
| M-OLCEK-1-56-r | § 12c `.gk-sema.secili` içe gölge → `border-width: 3px` | `OL25_v12_ayarlar_secim_kartlari_secilince_buyumez` | AYNI |
| M-OLCEK-1-58 (yeni) | § 12c `grid-auto-rows: 1fr` satırı silindi | `OL25_…` | AYNI |
| M-OLCEK-1-57 (önceki v12 koşumu, günlüğü okundu) | § 12c `.gk-secenek` 1 px kuralı silindi | `OL25_…` | AYNI |
Test açığı yok. Sıkışma A/B için olcek.css geçici olarak v11'e döndürüldü ve geri alındı (`olcek.css v12 geri AYNI`); sonra § 12c'ye ızgara satırı eklendi ve son sürüm `v12/olcek-v12b.css` ile `cmp` AYNI.

## v12-4. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; kiracı `PAKET-OLCEK-1L-TEST`, 01M3MJCT844AMF8NZZ1SRBHB3K)
Kiracı: API ile davet 201 / kayıt 201 / giriş 200, 6 uzun adlı müşteri `/cari` 201 ×6, ürün açılış stoğu 400 ("en az bir kur girişi gerekir", v10/v11 ile aynı), `demo-tohumla.js --prova-kiraci` (tohum yazıldı; betiğin kendi V8 kontrolü kiracı betiğinin 4 stoksuz ürününde 0 hareket bulduğu için rc=1, v11 ile aynı, kabul ölçümünü etkilemez).

### v12-4.1 Düğme ailesi + yazı ailesi (`v12/prob.mjs`): 8 rota (/, /cariler, /urunler, /emanet, /islemler, /raporlar/kar-zarar, /bildirimler, /vezne)
| Genişlik · tema | BASİT boylar | PRO boylar | aile dışı | yaziAile ihlali | değer açıcısı ≠ hücre | yatay |
|---|---|---|---|---|---|---|
| 1440 açık | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 |
| 1440 koyu | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 |
| 1280 açık | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 |
| 960 açık | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 |
| 375 açık | 44,52 | 44,52 (+54) | 0 (44 altı 0) | PRO `#islem-diger-ac` 54/16 (iki satırlı özet, v8 gerekçesi) | 0 | 0 |
| 375 koyu | 44,52 | 44,52 (+54) | 0 (44 altı 0) | aynı | 0 | 0 |
v11 tablosuyla birebir aynı. `/` rotasında BASİT [34,52] ↔ PRO [34,44]: modun GÖSTERDİĞİ öğe farkı (panel), aynı öğe farklı boy değil.

### v12-4.2 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 41 rota): `v12/son/tut/w*/`
| Genişlik | Fark | İçerik |
|---|---|---|
| 1440 | 12 | 1 anahtar çakışması (`/cariler td:—` 400↔600: BASİT telefon hücresi ↔ PRO has hücresi, farklı öğeler) + 11 `/urunler` satır 61↔80 (40+ karakterli fikstür adının PRO'da fazladan sütunlarla sarması) |
| 1280 | 1 | anahtar çakışması |
| 960 | 5 | 1 çakışma + 4 `/fiyatlandirma` 164↔215 (PRO fazladan sütun, sarma) |
| 375 | 27 | `/cariler` BASİT kart yığını ↔ PRO kendi kabında kayan tablo (yerleşim) |
v11 ile sayı sayı aynı (12/1/5/27). **Ölçü kuralından doğan fark: 0.** Düğme sınıfları (1440, iki mod aynı): `btn-birincil`/`btn-ikincil` 44·16 · `btn-kucuk` 34·14 (×103–104) · `btn-buyuk` 52·17 · sınıfsız 34·14 / 44·16; gerekçeli sapma şerit `btn-kucuk` 44·16 (/faturalar, /kur, /urunler/agac → DUGME-2).

### v12-4.3 Hakem senaryosu (`sen.mjs`: dolu fiş, ödeme böl/sil, kaydet hatası, müşteri çekmecesi + hata, Emanet al, kâr-zarar): `v12/son/sen-*`
| Genişlik · tema | Fark satırı | Kesik | 375'te 44 altı | Yatay | Fark kaynağı |
|---|---|---|---|---|---|
| 1440 açık | 10 | 0 | — | 0 | 5× `#islem-cari` pad (gerekçeli) · 3× uzun ad sarması (61↔75, içerik) · 2× çakışma |
| 1280 koyu | 10 | 0 | — | 0 | aynı |
| 960 açık | 13 | 0 | — | 0 | 5× `#islem-cari` · 8× uzun ad sarması |
| 375 açık / koyu | 19 / 23 | 0 | **0** | 0 | 5× `#islem-cari` · 16× (koyu) fiş kalem iç dolgusu BASİT 4 ↔ PRO 8 (`tuval-satis.css`, SATIS-3) · 2× çekmece başlık METNİ (56↔28) |
Boylar: 1440/1280/960 **34,44,52**; 375 **44,52**. Müşteri çekmecesi (`#duz-ad`, `#cekmece-cari-kaydet`) iki modda 44 px. v11'e göre +1 (1440) / +4 (375 koyu) satır: aynı üç kaynağın bu kiracının fiş tutarıyla daha çok satıra düşmesi; yeni kaynak YOK.

### v12-4.4 /ayarlar seçim kartları (`v12/gk.mjs`, yeni): § v12-0 tablosu. Seçili ↔ seçili olmayan boy farkı **0**, mod farkı **0**, tema kartı boy çeşidi **1** (her genişlikte).

### v12-4.5 Sıkışma turu A/B (`tur.mjs` + `analiz.py`, K1–K13): `v12/tur-v12` ↔ `v12/tur-v11`
Rotalar /, /cariler, /bildirimler, /vezne, /faturalar, /siparis × 1440/1280/960/375 × BASİT/PRO; 48 sayfa, 311 katman, hata 0. v12 CSS **8** bulgu · v11 CSS **8** bulgu, K satırları `diff` ile **AYNI** → § 12c yeni taşma/kırılma üretmedi. Kalanlar paketten önce de var (GlobalArama 375 serbest katman, /cariler "⋯" katmanı: KATMAN; /bildirimler Pa960 K3: ORTAK-1).

### v12-4.6 Ekran görüntüleri (Read ile GÖRÜLDÜ) ve tekrar komutları
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v12-{emanet,cariler,islemler,kar-zarar}-{1440-aydinlik,1440-karanlik,1280-aydinlik,960-aydinlik,375-aydinlik,375-karanlik}.png` (24 dosya, solda BASİT sağda PRO). Görülen: `…-emanet-1440-aydinlik.png` (Müşteri kutusu, "Emanet al"/"Emanet iade et" iki modda AYNI boy, kurucunun şikâyeti kapalı), `…-islemler-375-karanlik.png` (BASİT karo ↔ PRO sekme = orkestratör kararıyla gerekçeli iki bileşen; müşteri kutusu ve arama kutusu iki modda aynı boy; yatay kaydırma yok).
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v12-ayarlar-{1440-aydinlik,960-aydinlik,375-karanlik}.png`. Görülen: 375 koyu (Basit/Profesyonel kartı iki modda aynı boy, seçili kart vurgulu çerçeveyle belli), 960 (seçili "Basit" kartı çerçeveli, ızgara hizalı). Not: `gk.mjs` 960 "aydinlik" koşumunun görüntüsü koyu çıktı (kiracının "Bilgisayara uy" tercihi başsız Chrome'da koyu çözülüyor); boy ölçümü temadan bağımsız.
- Tekrar: `cd v12 && zsh olc-sira.sh` (API 5186 + vekil 5187 açıkken) · Ayarlar: `node v12/gk.mjs <w> <tema> 9957` · Mutasyon: `v12/mut.sh <kod> <dosya> '-es/…/…/' "FullyQualifiedName~OlcekTek"`.

## v12-5. Denetim (`bash dotnet/denetim.sh`, son değişiklik sonrası)
rc=0 · **291 ✓ · 0 ✗ · "DENETİM TEMİZ."** · `OLCEK-1: MOD_OLCU_BILDIRIMI 0` · `OLCEK-1: YUZ_ONEMLI_OLCU 0` · `P-39: DUGME_YUKSEKLIK_CESIDI 2` (tavan 2) · `P-39: DOKUNMA_HEDEFI_KUCUK 0` · `P-39: KONTROL_YUKSEKLIK_SAPMASI 0` · `P-18: SABIT_RENK 0`.

## v12-6. DoD sözleşmesi
Yeni varlık, kayıt türü, uç, form, düğme yok (yalnız CSS + test). PATCH/DELETE/[RolGerekli]/denetim kaydı uygulanamaz; migration gerekmedi; ModalPencere sayacı ve sayfa dosyaları değişmedi. Mevzuat içeriği yok. Fiyat/teklif metni yok.

## v12-7. Temizlik
- API 5186 (PID 11249) ve vekil 5187 `kill`; `lsof` 5186/5187 **boş**.
- `kiraci-temizle.sh 01M3MJCT844AMF8NZZ1SRBHB3K --uygula`: 70 tablo, 1649 satır + tenants 1 silindi; rapor kipinde yeniden: "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)". Bu turun kullanılmış davet satırı `delete … where isletme_adi like 'PAKET-OLCEK-1L-TEST%' and kullanan_tenant_id='01M3MJCT…'` → 1 satır; `%OLCEK%` davet **0**, `%OLCEK%` kiracı **0**.
- Kiracı sayısı: başlangıç **17** → +1 (benim) → son **19**. Artış 2 = başka şeritlerin bu arada açtığı `PAKETEMANET-1-TEST T39` ve `PAKETZO-ISLEM-A-TEST Dogrulama` (dokunulmadı). QA-TEST-*/DEMO-* kiracılarına dokunulmadı.
- Fikstür ve sistem anahtarı `printf '' >` ile boşaltıldı (0 bayt). Proje ağacına geçici dosya bırakılmadı.

## v12-8. Açık kalanlar (sahibiyle)
1. **Kurucu kararı: kenar menüsü PRO yüzü** (KATMAN/T-K2): beyaz listede.
2. **SATIS-3:** `#islem-cari` rozet payı (gerekçeli) · `tuval-satis.css` ≤600 px BASİT kalem iç dolgusu 4 ↔ PRO 8 · tam süitte `SATIS3SinirTestleriTur7.Tur7_takas_fiyati_CIKMAYAN_urun_onceki_urunun_fiyatini_TASIMAZ` kırmızı · tuval-satis.css sabit px'leri belirteçe çevrilirse olcek.css § 4/5b/10b sadeleşir.
3. **ZO-ISLEM-A:** tam süitte `ZOISLEMASinirPosTestleri.ZOS_pos_ayni_tahsilat_iki_satirla_net_tutari_asamaz` kırmızı (Duplicate entry `pos_yatma_eslesmeleri`).
4. **ORTAK-1 / Cariler sahibi:** `OrtakGorunumSaflik.O8` kırmızı; /cariler PRO satır eylemleri "⋯" altında toplanmalı; 375'te PRO tablo ↔ BASİT kart; çekmece başlık METNİ; /bildirimler Pa960 K3.
5. **KATMAN:** PRO satır eylemlerinde emoji (📦 ✏️) → Ikon; GlobalArama 375 serbest katman.
6. **DUGME-2 / sayfa sahipleri:** şerit `btn-kucuk` 44·16 (/faturalar, /kur, /urunler/agac); `/ekranlar #ekran-favori-*` 24 px; `/raporlar/islem-defteri #id-belge-*` 26 px (ZO-BELGE); eski `ikincil` sınıflı düğmeler `btn` ailesine bağlanınca olcek.css § 10f/§ 12a köprüsü kalkar.
7. **Orkestratör:** `prob.mjs` + `gk.mjs` + `tutarlilik.mjs` + `sen.mjs` kapı adımı olsun; inceleme yayını v12 ile yeniden `yayin-al`; `dotnet/denetim.sh.mutbak` (0 bayt) silinmeli (kurucu).

## v12-9. Kurucuya not
- Ayarlar'da tema ve ekran modu kartlarına tıklayınca kart artık büyüyüp küçülmüyor; seçili kart yalnız rengiyle ve kalın çerçevesiyle belli oluyor.
- Tema kartları dar ekranda da aynı boyda sıralanıyor.
- Emanet, Müşteriler, Satış ve Kâr-zarar ekranları Basit ve Profesyonel'de yeniden ölçüldü: aynı düğme, aynı kutu, aynı boy.

---

# Ek: v11 raporu (19:05–20:25, değiştirilmeden)

## (v11 başlığı) Uygulama raporu v11

**Tarih:** 2026-09-28 19:05–20:25 (devir notu 2, 18:45 sonrası) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9955–9963)
**v11 neden var:** Doğrulayıcı üç turda da GEÇTİ verdi; paket, 18:10–18:25 kesintisinde doğrulayıcının mutasyonları ve 375/koyu ölçümünü BAĞIMSIZ yeniden üretememesi yüzünden durdu. Devir: kod tamam, yeniden yazma; 3 DÜŞÜK/ORTA bulguyu kapat, en az 2 mutasyonu ve 375 açık+koyu / 1440 / 1280 / 960 × BASİT/PRO ölçümünü başsız Chrome ile yeniden koş, görüntüleri gör.
**Devralma:** `olcek.css` = `v10/olcek-v10.css` (cmp AYNI); `belirtecler.css` = doğrulayıcı yedeği `dogrulama-yedek/pOLCEK-1/belirtecler.css` (cmp AYNI) → yarım mutasyon YOK. Doğrulayıcının artık kiracısı `PAKETOLCEK-1-TEST Dogrulayici` (01M3M8Y7GK8V73093ZE952ER02) bulundu ve bu turun temizliğinde silindi (§ v11-7). 5198/5199/5200'e dokunulmadı.
**Ölçüm yöntemi:** izolasyon kuralı bölmeyi yasakladığı için başsız Chrome + CDP (`betikler/tur/cdp.mjs`), kendi profil dizinlerim, parolasız vekil (`inceleme-vekili.py` 5187). Ölçümler SIRALI (`v11/olc-sira.sh`). Tüm çıktılar `/private/tmp/kuyumcu-serit/OLCEK-1/v11/`.

## v11-0. Son doğrulama bulgularının kapanışı

| Bulgu (doğrulayıcı t3) | Durum | Kanıt (v11, güncel ağaç) |
|---|---|---|
| [ORTA/DÜŞÜK] /cariler PRO telefon açıcısı 44 px'te 15 px yazı | **ÖLÇÜLDÜ, KARARLA KAPANDI (kural değil, ölçüt düzeltildi)** — § 12b + OL24 | Öğe `#si-cari-telefon-<id>-ac` (SatirIciDuzenle `.si-tetik`). Düğme görünümü YOK: kenarlık ve zemin `rgba(0,0,0,0)` (ölçüldü), `font: inherit` → hücrenin DEĞERİ olarak çizilir. Aynı numara BASİT'te düz metin `span.cari-telefon-metin` **15 px / 400**, PRO'da `.si-tetik` **15 px / 400** (1440 ve 375, `v11/tel.mjs`). 16 px'e çekmek aynı veriyi iki modda iki boyda gösterirdi (paketin kapattığı fark) ve satırda iki yazı boyu yaratırdı. Kural yazıldı: `.si-tetik` yazısı hücreden gelir, olcek.css ezmez (OL24 sınar). Ölçüm aracı `prob.mjs` bu öğeyi "tablo değeri" sayar, hücre yazısıyla karşılaştırır: `degerUymaz` **0** (96 rota×genişlik×tema×mod hücresi). |
| [ORTA] `#islem-cari` sağ dolgu BASİT 44 ↔ PRO 258/340/332/197 px | **GEREKÇELİ BIRAKILDI — SATIS-3 alanı** | Kural `tuval-satis.css` ~692: `.tuval-pro #islem-ust-satir > .alan:first-child:has(> .rozet-satir) .secici-tetik { padding-right: calc(46% + 36px) }` — PRO'da borç rozeti (en fazla %46) kutunun İÇİNDE yer tutar; BASİT'te rozet oka 40 px mesafede konumlanır (`padding-right: 44px`, ~77). Kutu boyu 44, yazı, köşe, kalınlık iki modda AYNI (sen.mjs FARK satırında yalnız `pad`). Dosya SATIS-3'ün; ezmek şeridin yerleşim kararını bozar → § v11-8. |
| [DÜŞÜK] § 6 üst çubuk yazıları sabit px | **KAPANDI** (§ 6 v11) | `14px/19px/16px` → `var(--yazi-kontrol-kucuk)` / yeni `--yazi-kur-deger: calc(var(--yazi-govde) * 19 / 18)` / `var(--yazi-kontrol)`. Gövde 18 px'te değerler aynı (görüntüde kur şeridi değişmedi); kullanıcı yazı ölçeği 16/20 artık üst çubuğu iki modda birlikte ölçekler. OL9 güncellendi (belirteç adları + `:root` tanımları + § 6'da sabit px yazı 0). |
| **Ek (genişletilmiş ölçümde bulundu)** eski `class="ikincil"` düğmeler 44 px / 14 px (`#bildirim-yenile`, `#bildirim-bos-cta`, `#is-calistir-*`, `#vz-yenile`) | **KAPANDI** (§ 12a) | Yeni ölçüt `yaziAile` (34→14, 44→16 [telefonda 14|16], 52→17) 41 rota × 2 mod: önce bu 4 öğe ailesi dışı; sonra `/bildirimler`, `/vezne` iki modda `yaziAile []`. Görüntü `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v11-bildirimler-1440.png` (Read ile görüldü: "Yenile", "Kapatılanlara bak" sekme düğmeleriyle aynı yazı). |

Kalan `yaziAile`/boy dışı öğeler (41 rota, iki modda AYNI → mod farkı değil; sahiplerine § v11-8): `#gk-yazi-16/18/20` (yazı ölçeği ÖNİZLEME düğmeleri — kendi boyunu göstermek zorunda, gerekçeli), `#gk-sema-*` tema kartları 114–118 (düğme değil, önizleme kartı), `#ayar-gorunum-ayrinti-baslik` 50/16 (v9'dan beri listede), `/hurda #hrd-detay-ac` 52/16 (tuval Musteriler "Daha fazla" satırı, ZO-ISLEM-A'nın tuval ölçüsü), `/ekranlar #ekran-favori-*` 24 px yıldız, `/raporlar/islem-defteri #id-belge-*` 26 px belge bağlantısı, `/islemler` PRO 375 `#islem-diger-ac` 54 (iki satırlı özet, v8 gerekçesi).

## v11-1. Değişen dosyalar (bu tur)
- `dotnet/Kuyumcu.Web/wwwroot/css/olcek.css` (805 satır; v10 782): `:root` `--yazi-kur-deger`; § 6 belirteçe; **§ 12a** (eski ikincil düğme yazısı), **§ 12b** (bilinçli kural yok — değer açıcısı yazısı hücreden; gerekçe yorumu).
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs`: OL9 güncellendi, **OL24 yeni** (toplam 26 test).
- Dokunulan sayfalar (razor): **yok**. app.css, belirtecler.css: bu turda değişmedi. Sözlük, uç, migration: yok.

`git status --porcelain | grep -v -E "/(bin|obj)/"` (paketin dosyaları):
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Tests/            (OlcekTekTestleri.cs dahil; dizin izlenmiyor)
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css
?? dotnet/denetim.sh
?? dotnet/denetim.sh.mutbak         (0 bayt, 18.09 16:49 — bu paketin DEĞİL; eski bir mutasyon artığı, silme kurucuda)
```

## v11-2. Derleme ve testler
- `dotnet build Kuyumcu.sln` — başlangıç 0/0; değişiklik sonrası **0 Hata, 1 Uyarı** (başka şeridin `EtiketEkranTestleri.cs(145,47) xUnit1031`); **son mutasyondan sonra temiz derleme: 0 Hata, 0 Uyarı**.
- Filtreli `FullyQualifiedName~OlcekTek`: 25 → **26/26 geçti**.
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pOLCEK-1/v11/_Mac_2026-09-28_19_22_24.trx`): **6699 · 6694 geçti · 5 başarısız** — hiçbiri bu paketin:
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1; "Expected 15, Actual 16" sözlük metin sayısı — v9/v10'da da kırmızı),
  - `KarmaOdemeEkranTestleri.Bacak_toplami_tutmuyorsa_KAYDET_KAPALI`, `…Had_asilinca_NAKIT_bacak_KAYDI_ENGELLER` (SATIS-3; dosya 19:28'de değişti; CSS okumaz — `grep olcek|CssKaynak` = 0; v10'da da kırmızı),
  - `SATIS3EkranTestleri.T3_Takas_yeni_urun_TEK_arama_kutusundan_secilir_kalem_EKLENMEZ_govdeye_girer(mod: BASIT|PROFESYONEL)` (SATIS-3; bUnit `WaitForFailedException`; dosya 19:34'te SATIS-3 tarafından değişti; CSS okumaz).
  - **Madde (7): bu paketin ürettiği yeni kırmızı 0.**

## v11-3. Mutasyon kanıtları (yedek + günlük `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/<kod>/`)
| Kod | Değişiklik (tek eşleşme) | Kırmızı test | cmp |
|---|---|---|---|
| **M-OLCEK-1-1** (spesifikasyon, v11 yeniden üretim) | `belirtecler.css` PRO bloğuna `--kontrol-yuksekligi: 2rem` (sunulan dosyada doğrulandı) | `OL1_belirteclerde_moda_ozel_blok_YALNIZ_mod_isareti_tasir`, `OL2_hicbir_css_dosyasi_moda_ozel_OLCU_yazmaz_belirtec_ya_da_dogrudan` · **canlı** `tutarlilik.mjs /emanet,/cariler,/islemler 1440`: taban **1** fark (`v11/tut-taban/`) → mutasyonla **12** (`v11/tut-mut1/`: `/emanet #e-cari 44→39`, `/cariler #cari-yeni-ac dolgu 10.6→4.6`, `/islemler #islem-tur-SATIS 44→32`, `#islem-barkod 44→32`, `#islem-git-odeme 44→32` …) | AYNI (+ doğrulayıcı yedeğiyle AYNI) |
| M-OLCEK-1-53 | § 12a ikincil düğme `font-size: var(--yazi-kontrol)` → `inherit` | `OL24_v11_eski_ikincil_dugme_kontrol_yazisi_satir_ici_deger_hucreden` | AYNI |
| M-OLCEK-1-54 | § 6 kur değeri `var(--yazi-kur-deger)` → `19px` | `OL9_ust_cubuk_iki_modda_BASIT_olcusunde` | AYNI |
| M-OLCEK-1-55 | § 12b'ye `html[data-mod] .ana-icerik .si-tetik { font-size: var(--yazi-kontrol) }` eklendi | `OL24_…` | AYNI |
Test açığı yok. Tur A/B için olcek.css geçici olarak v10'a döndürüldü ve geri alındı: `cmp olcek.css v11/olcek-v11.css` AYNI.

## v11-4. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; kiracı `PAKET-OLCEK-1K-TEST`, 01M3MCBEPNVM1RMH0RK4ZE289C)
Kiracı: API ile davet+kayıt (201/201/200), 6 uzun adlı müşteri `/cari` 201 ×6, ürün açılış stoğu 400 ("en az bir kur girişi gerekir" — v10 ile aynı), `demo-tohumla.js --prova-kiraci` (tohum yazıldı; betiğin kendi V8 kontrolü kiracı betiğinin 4 stoksuz ürününde 0 hareket bulduğu için rc=1 — v9/v10 ile aynı, kabul ölçümünü etkilemez).

### v11-4.1 Düğme ailesi + yazı ailesi (`v11/prob.mjs`) — 8 rota (/, /cariler, /urunler, /emanet, /islemler, /raporlar/kar-zarar, /bildirimler, /vezne)
| Genişlik · tema | BASİT boylar | PRO boylar | aile dışı boy | 34'te yazı≠14 | yaziAile ihlali | değer açıcısı ≠ hücre | yatay |
|---|---|---|---|---|---|---|---|
| 1440 açık | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 (PRO /cariler 13 açıcı, 15=15) | 0 |
| 1440 koyu | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 | 0 |
| 1280 açık | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 | 0 |
| 960 açık | 34,44,52 | 34,44,52 | 0 | 0 | 0 | 0 | 0 |
| 375 açık | 44,52 | 44,52 (+54) | 0 (44 altı 0) | 0 | PRO `#islem-diger-ac` 54/16 (gerekçeli) | 0 | 0 |
| 375 koyu | 44,52 | 44,52 (+54) | 0 (44 altı 0) | 0 | aynı | 0 | 0 |
41 rota 1440 açık (`prob-1440-tum-son.txt`, son CSS): aile dışı yalnız § v11-0'daki tekil öğeler (iki modda aynı); yatay kaydırma **0**; değer açıcısı uyumsuzluğu **0**.

### v11-4.2 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 41 rota) — `v11/son/tut/w*/`
| Genişlik | Fark | İçerik |
|---|---|---|
| 1440 | 12 | 1 anahtar çakışması (`/cariler td:—` 400↔600: BASİT telefon hücresi ↔ PRO has hücresi, farklı öğeler) + 11 `/urunler` satır 61↔80 (40+ karakterli iki fikstür adının PRO'da fazladan sütunlarla sarması; v10 `son/tut2` ile birebir aynı) |
| 1280 | 1 | anahtar çakışması |
| 960 | 5 | 1 çakışma + 4 `/fiyatlandirma` 164↔215 (PRO fazladan sütun, sarma; v8'den beri) |
| 375 | 27 | `/cariler` BASİT kart yığını ↔ PRO kendi kabında kayan tablo (yerleşim; v10'da 23 — bu kiracıda daha çok müşteri satırı var); yazı iki modda aynı |
**Ölçü kuralından doğan fark: 0.** Düğme sınıfları (1440, iki mod aynı): `btn-birincil`/`btn-ikincil` 44·16 · `btn-kucuk` 34·14 (×103–104) · `btn-buyuk` 52·17 · sınıfsız 34·14 / 44·16; gerekçeli sapma şerit `btn-kucuk` 44·16 (/faturalar, /kur, /urunler/agac → DUGME-2).

### v11-4.3 Hakem senaryosu (`sen.mjs`: dolu fiş, ödeme böl/sil, kaydet hatası, müşteri çekmecesi + hata, Emanet al, kâr-zarar) — `v11/son/sen-*`
| Genişlik · tema | Fark satırı | Kesik | 375'te 44 altı | Yatay | Fark kaynağı |
|---|---|---|---|---|---|
| 1440 açık | 9 | 0 | — | 0 | 5× `#islem-cari` pad (gerekçeli) · 2× uzun ad sarması (içerik) · 2× çakışma |
| 1280 koyu | 9 | 0 | — | 0 | aynı |
| 960 açık | 13 | 0 | — | 0 | 5× `#islem-cari` · 8× uzun ad sarması |
| 375 açık / koyu | 19 / 19 | 0 | **0** | 0 | 5× `#islem-cari` · 12× fiş kalem iç dolgusu BASİT 4 ↔ PRO 8 (`tuval-satis.css` (h), SATIS-3) · 2× çekmece başlık METNİ (56↔28) |
Boylar: 1440/1280/960 **34,44,52**; 375 **44,52**. Bu paketin kuralından doğan fark 0.

### v11-4.4 Sıkışma turu A/B (`tur.mjs` + `analiz.py`, K1–K13) — `v11/tur-v11` ↔ `v11/tur-v10`
Rotalar /, /cariler, /bildirimler, /vezne, /faturalar, /siparis × 1440/1280/960/375 × BASİT/PRO; 48 sayfa, 311 katman, hata 0. v11 CSS: **8** bulgu · v10 CSS: **8** bulgu — bulgu listesi `diff` ile **birebir AYNI** → § 12a'nın büyüttüğü yazı yeni taşma/kırılma üretmedi. Kalanlar paketten önce de var: GlobalArama 375 serbest yüzen katman (KATMAN), /cariler "⋯" katmanı (SatirEylemleri/KATMAN), /bildirimler Pa960 tarih hücresi K3 (ORTAK-1).

### v11-4.5 Ekran görüntüleri (Read ile GÖRÜLDÜ)
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v11-<rota>-1440.png` — 41 rota, solda BASİT sağda PRO, 1440×900 (son CSS'le yeniden alındı). Görülenler: `…-cariler-1440.png` (telefon numarası iki modda aynı boy; PRO'da "Düzenle" kabın kenarında — bilinen, § v11-8), `…-bildirimler-1440.png` (koyu tema tekil karesi `v11/prob/1440-karanlik/Profesyonel_bildirimler.png`: Yenile/Kapatılanlara bak 44·16).
- 375 koyu yan yana: `OLCEK-1-v11-{cariler,islemler,emanet}-375-koyu.png` (`…-islemler-375-koyu.png` görüldü: BASİT karo ↔ PRO sekme = orkestratör kararıyla gerekçeli iki bileşen; müşteri kutusu, arama kutusu iki modda aynı boy). 375 açık: `v11/prob/375-aydinlik/Profesyonel_cariler.png` görüldü (yatay kaydırma yok, tablo kendi kabında).
- 1280 / 960: `v11/prob/{1280-aydinlik,960-aydinlik}/`.

## v11-5. Denetim (`bash dotnet/denetim.sh`, değişiklik sonrası)
rc=0 · **291 ✓ · "DENETİM TEMİZ."** · `OLCEK-1: MOD_OLCU_BILDIRIMI 0` · `OLCEK-1: YUZ_ONEMLI_OLCU 0` · `P-39: DUGME_YUKSEKLIK_CESIDI 2` (tavan 2) · `P-39: DOKUNMA_HEDEFI_KUCUK 0` · `P-39: KONTROL_YUKSEKLIK_SAPMASI 0` · `P-18: SABIT_RENK 0`.

## v11-6. DoD sözleşmesi
Yeni varlık, kayıt türü, uç, form, düğme yok (yalnız CSS + test). PATCH/DELETE/[RolGerekli]/denetim kaydı uygulanamaz; migration gerekmedi; ModalPencere sayacı ve sayfa dosyaları değişmedi. Mevzuat içeriği yok.

## v11-7. Temizlik
- API 5186 (PID 25071) ve vekil 5187 `kill`; `lsof` 5186/5187 **boş**. 9966/9968'deki Chrome süreçleri bu paketin değil (dokunulmadı).
- `kiraci-temizle.sh --uygula`: bu turun kiracısı **01M3MCBEPNVM1RMH0RK4ZE289C** ve doğrulayıcının artığı **01M3M8Y7GK8V73093ZE952ER02** (`PAKETOLCEK-1-TEST Dogrulayici`) silindi; tenant_id kolonlu **70 tablo, iki kimlik için 0 satır**; `%OLCEK%` kiracı **0**. Yetim davet (`PAKET-OLCEK-1K`) 1 satır silindi; `%OLCEK%` davet **0**.
- Kiracı sayısı: başlangıç **19** (doğrulayıcı artığı dahil) → +1 (benim) = 20 → son **18** (benimki + doğrulayıcı artığı silindi). QA-TEST-*/DEMO-* ve diğer şerit kiracılarına dokunulmadı.
- Fikstür ve sistem anahtarı `printf '' >` ile boşaltıldı (0 bayt). Proje ağacına geçici dosya bırakılmadı.

## v11-8. Açık kalanlar (sahibiyle)
1. **Kurucu kararı — kenar menüsü PRO yüzü** (KATMAN/T-K2): beyaz listede.
2. **SATIS-3:** `#islem-cari` rozet payı (gerekçeli, § v11-0) · `tuval-satis.css` (h) ≤600 px BASİT kalem iç dolgusu 4 ↔ PRO 8 · PRO uzun ürün adı sarar / BASİT üç nokta · tam süitte `KarmaOdemeEkranTestleri` ×2 ve `SATIS3EkranTestleri.T3_…` ×2 kırmızı · tuval-satis.css sabit px'leri belirteçe çevrilirse olcek.css § 4/5b/10b sadeleşir.
3. **ORTAK-1 / Cariler sahibi:** `OrtakGorunumSaflik.O8` kırmızı; /cariler PRO satır eylemleri "⋯" altında toplanmalı (1440'ta "Düzenle" kabın kenarında); 375'te PRO tablo ↔ BASİT kart; çekmece başlık/etiket METNİ; /bildirimler Pa960 tarih K3.
4. **KATMAN:** PRO satır eylemlerinde emoji (💵 ✏️ 📦) → Ikon; GlobalArama 375 serbest yüzen katman.
5. **DUGME-2 / sayfa sahipleri:** şerit `btn-kucuk` 44·16 (/faturalar, /kur, /urunler/agac); `/ekranlar #ekran-favori-*` 24 px (dokunma hedefi 34 olmalı); `/raporlar/islem-defteri #id-belge-*` 26 px (ZO-BELGE); `#ayar-gorunum-ayrinti-baslik` 50; eski `ikincil` sınıflı düğmeler razor'da `btn` ailesine bağlanınca olcek.css § 10f/§ 12a köprüsü kalkar.
6. **Orkestratör:** `prob.mjs` (v11: `yaziAile` + değer açıcısı ölçütü, `.dugme` dahil) + `tutarlilik.mjs` + `sen.mjs` kapı adımı olsun; inceleme yayını (`~/.kuyumcu-yayin`) v11 ile yeniden `yayin-al`; `dotnet/denetim.sh.mutbak` (0 bayt, 18.09) silinmeli (kurucu).

## v11-9. Kurucuya not
- Bildirimler ve Vezne'deki "Yenile", "Kapatılanlara bak" ve zamanlanmış işlerin çalıştır düğmelerinin yazısı artık diğer aynı boy düğmelerle aynı büyüklükte.
- Profesyonel müşteri listesindeki telefon numarası, Basit moddakiyle aynı büyüklükte kalıyor (bilerek: aynı numara iki modda aynı görünsün).
- Üst çubuktaki kur ve saat yazıları, Ayarlar'daki yazı büyüklüğü tercihine artık iki modda birlikte uyuyor.

---
# Ek — v10 raporu (16:49–18:05, değiştirilmeden)

## (v10 başlığı) Uygulama raporu v10

**Tarih:** 2026-09-28 16:49–18:05 (son doğrulama sonrası) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9955–9964)
**v10 neden var:** Son doğrulama (GEÇTİ) 1 ORTA + 3 DÜŞÜK bulgu ve 3 "hakem çelişkisi" bıraktı. Ağaç korundu: `olcek.css` ilk 721 satırı v9 ile birebir (`cmp olcek.css v9/olcek-v9.css` = AYNI, devralmada), `belirtecler.css` doğrulayıcı yedeğiyle AYNI (yarım mutasyon YOK; 16:36 tarihi doğrulayıcının geri kopyasından). Yalnız **olcek.css § 11** ve **OL23** eklendi; bütün kabul ölçümleri güncel ağaçta yeniden koşuldu.
**Ölçüm yöntemi:** izolasyon kuralı (27.09) "tarayıcı bölmesini KULLANMA" dediği için başsız Chrome + CDP (`betikler/tur/cdp.mjs`), kendi profil dizinlerimde, parolasız vekil oturumu (`inceleme-vekili.py`, 5187). Ekran görüntüleri Read ile GÖRÜLDÜ (aşağıda). Bölmeden bağımsız tekrar için komutlar § 6'da.

## 0. Son doğrulama bulgularının kapanışı (v10, güncel ağaçta ölçüldü)

Ölçüm betiği `v10/prob.mjs` (her görünür `main button, a.dugme, a.btn, summary, [role=button]`: boy + yazı; aile dışı = 34/44/52 ± 2 dışında, telefonda < 44). Çıktı `/private/tmp/kuyumcu-serit/OLCEK-1/v10/prob-*.txt`, görüntüler `v10/prob/<genişlik>-<tema>/`.

| Bulgu | Durum | Kanıt (v10) |
|---|---|---|
| [ORTA] Panel BASİT `#panel-borclu-eylem-*` 38/15, `#pozisyon-ayrinti`/`#pozisyon-alacak` 20/15, `#panel-yeni-satis` 54; 375'te 38 | **KAPANDI** (§ 11a–c) | `/` BASİT 1440/1280/960: borçlu eylem **34/14**, pozisyon açıcı ve kırılım öğeleri **34/14**, yeni satış **52/17**; sayfa düğme boyları `[34,52]`, aile dışı **0**. 375 açık ve koyu: hepsi **44/14**, yeni satış 52; 44 altı **0**. PRO `/` 1440/960/375: `[34,44]` / `[44]`, aile dışı 0. Görüntü `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v10-panel-1440.png`, `v10/prob/375-karanlik/Basit_.png` (görüldü: "Tahsil et"/"Öde" eşit kutular, "Ayrıntı" bağlantı görünümünde). |
| [DÜŞÜK] /urunler PRO kısa adlar iki satır; rapor gerekçesi eksik | **KAPANDI + GEREKÇE DÜZELTİLDİ** (§ 11e) | v9 raporunun "uzun ürün adı fikstürü" gerekçesi YANLIŞTI: 15–21 karakterlik standart adlar da kırılıyordu (doğrulayıcı haklı). Ad sütunu 769–1399 px'te en az 14 × tablo yazısı (≈210 px). A/B (`v10/ab.mjs`, PRO): 1280 kırık ad **13 → 2**, 960 **2**; kalan 2 ad 40+ karakterli fikstür adı ("22 Ayar Burma Bilezik Özel Seri El İşçiliği Kalın Model"), kelime sınırında sarar. **1440'ta uygulanmaz**: ilk denemede 210 px tabloyu 1165/1122 taşırıp satır eylemini (⋯) kabın dışına itti (görüntüde görüldü) → sınır `max-width: 1399px`; 1440'ta tablo 1122/1122 sığar, 15–21 karakterlik adlar tek satır, 26–27 karakterlik iki ad ("Bilezik Hasır 22 Ayar Geniş", "14 Ayar Zirkon Taşlı Kolye") iki satır (kırpma yok). tur.mjs: v9'daki `/urunler K3 (Ba960, Pa1280, Pa960)` bulgusu v10'da **yok**. |
| [DÜŞÜK] /cariler BASİT segment + "Diğer süzgeçler" 34 px ama yazı 15 | **KAPANDI** (§ 11d) | `#cari-segment-hepsi/-borclu`, `#cari-suzgec-daha` 1440/1280/960 **34/14**, 375 **44/14**. `yazi34` (34 px olup yazısı 14 olmayan) listesi her rotada boş. |
| [DÜŞÜK] Yetim davet satırları | **KAPANDI; temizlik adımına eklendi** (§ 10) | Doğrulayıcının sildikleri dışında bu paketin eski turlarından 5 yetim davet daha vardı (1E, HAKEM, 1F, 1G, 1H — kiracıları silinmiş). Bu turun daveti (1J) ile birlikte **6 satır silindi**; `%OLCEK%` davet = **0**. |
| [KRİTİK] Hakem çelişkisi — denetim.sh | **YENİDEN ÖLÇÜLDÜ** | İki rapor farklı ANLARI ölçmüştü: v9'da (15:xx) EMANET-1'in 0 baytlık `Pages/EmanetHamKap.razor`'u iki ✗ veriyordu (rc=1); doğrulayıcı sonradan (dosya giderilince) rc=0 ölçtü. **Bugün iki koşum (17:00 ve 18:00): rc=0, 291 ✓, "DENETİM TEMİZ."** Paket sayaçları ✓ (§ 7). |
| [KRİTİK] Hakem çelişkisi — görsel doğrulama | **BAĞIMSIZ ÖLÇÜM KOŞULDU** | Bu turda tüm görüntüler yeni kiracıyla, yeni ölçümle alındı (v9 görüntüsü yeniden kullanılmadı): 1440 açık/koyu · 1280 açık/koyu · 960 açık · 375 açık/koyu × BASİT/PRO. Doğrulayıcı bölme sekmesiyle tekrar edecekse komutlar § 6.6. |
| [KRİTİK] Hakem çelişkisi — 375 dokunma hedefi (Panel) | **YENİDEN ÖLÇÜLDÜ — iki rapor artık aynı gerçeği yazar** | v9 "375'te 44 altı 0" iddiası yalnız `sen.mjs` senaryosunun ölçtüğü rotalar (/islemler, /cariler çekmecesi, /emanet) içindi ve `/` o kümede YOKTU; ayrıca `tutarlilik-dugme` yalnız `.btn` ve sınıfsız düğmeleri sayar, `.dugme` sınıfını (Panel) GÖRMEZ → iddia kapsam dışı rotayı saymıyordu, doğrulayıcının 38 px ölçümü doğruydu. v10: `/` dahil 375'te 44 altı **0** (açık+koyu, iki mod), ölçüm `prob.mjs` `.dugme`'yi de sayar. |

Önceki devir listesindeki (EK KOŞULLAR) maddelerin durumu: çekmece 50↔44, bacak sil 30↔44, 16 px tutar kesiği, dolu fişte 6 boy — v8/v9'da kapandı ve bugün yeniden ölçüldü (§ 6.3: senaryo `kesik: []`, `kucuk44: []`, boylar 34/44/52 ve 375'te 44/52). Emoji ikonlar KATMAN'da (kapsam dışı). 375 /cariler kart↔tablo: yerleşim farkı (§ 6.1).

## 1. Özet
- Kök neden (v1): `belirtecler.css` `html[data-mod=profesyonel]` bloğu ölçü küçültüyordu → bloklar nötr (yalnız `--arayuz-modu`). Değişmedi.
- Tek ölçek: düğme 34 / 44 / 52 (telefonda 44 / 44 / 52) · yazı 14 / 16 / 17 · tablo hücre 15, başlık 13, dolgu 8, satır en az 61 · fiş kalem hücresi 16 · çekmece başlığı 21, etiket 15.
- **v10:** Panel BASİT yüzü ve /cariler BASİT süzgeçleri aileye bağlandı; /urunler ad sütunu 769–1399 px'te 210 px.
- **Sonuç (41 rota, tutarlilik.mjs, BASİT↔PRO):** 1440 **0** · 1280 **1** (anahtar çakışması) · 960 **5** (1 çakışma + 4 /fiyatlandirma içerik sarması) · 375 **23** (/cariler kart↔tablo yerleşimi). Ölçü kuralından doğan fark 0.

## 2. Spesifikasyon ↔ kod (v10 ekleri)
| Madde | Kodda (28.09 16:49) | Uygulama / sapma |
|---|---|---|
| "Sayfa dosyalarına dokunma" | Panel/Cariler/Urunler ölçüleri şerit CSS'inde (`tuval-panel-musteri.css`) ve app.css'te | Yalnız olcek.css § 11 (özgüllükle; kimlikli seçici). **Dokunulan sayfalar: yok.** |
| Tuval Panel "Yeni satış" 54 px | Üç boy kuralı | **SAPMA:** 52 (büyük-tam boy; tuvale −2 px, tolerans içinde). |
| Pozisyon kırılımı tuval 15 px düz metin | Tıklanan öğe (UX-S23) aile dışı 20 px | 34 px dokunma kutusu, 14 px yazı; renk/zemin yazılmadı (OL6) — bağlantı görünümü korunur. |
| /urunler ad sütunu | 9em genel kural | 769–1399 px'te 14 × tablo yazısı; 1440'ta uygulanmaz (A/B gerekçe § 0). |
Veri modeli, uç, migration, mevzuat: **gerekmedi.** Kaydet/POST yolları, yetki, DOM id'leri değişmedi.

## 3. Değişen dosyalar
Bu tur (v10):
- `dotnet/Kuyumcu.Web/wwwroot/css/olcek.css` (782 satır; v9 721): **§ 11a–11e yeni** (Panel borçlu eylem · pozisyon açıcısı/kırılım · yeni satış · /cariler segment ve "Diğer süzgeçler" yazısı · /urunler ad sütunu).
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs`: **OL23 yeni** (toplam 25 test).
Önceki turlardan (korundu): `belirtecler.css` (nötr mod blokları), `index.html` bağlantısı, `denetim.sh` OLCEK-1 sayaçları, olcek.css § 0–10, OL1–OL22, `betikler/tur/tutarlilik.mjs`, `olcek-senaryo.mjs`.

`git status --porcelain | grep -v -E "/(bin|obj)/"` (paketin dosyaları; ağaç tek ilk commit'ten beri izlenmiyor):
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css
?? dotnet/denetim.sh
```
Yeni görüntüler: `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/OLCEK-1-v10-{panel,emanet,cariler,islemler,raporlar_kar-zarar,urunler}-1440.png` (solda BASİT, sağda PRO).

## 4. Testler
- **Derleme** (`dotnet build Kuyumcu.sln`, son mutasyondan sonra, 18:02): **0 Hata, 1 Uyarı** — başka şeridin `EtiketEkranTestleri.cs(145,47) xUnit1031`.
- **Filtreli:** OlcekTek önce 24/24 → **25/25**.
- **Tam süit 1** (16:55, `/private/tmp/kuyumcu-serit/trx-pOLCEK-1/v10/`): 6622 · 6621 geçti · **1 başarısız** `OrtakGorunumSaflik.O8_…` (ORTAK-1).
- **Tam süit 2** (17:58, son CSS ile, `trx-pOLCEK-1/v10b/`): 6674 (başka şeritler test ekledi) · 6671 geçti · **3 başarısız**: `OrtakGorunumSaflik.O8_…` (ORTAK-1: sözlük metin sayısı), `KarmaOdemeEkranTestleri.Bacak_toplami_tutmuyorsa_KAYDET_KAPALI` ve `…Had_asilinca_NAKIT_bacak_KAYDI_ENGELLER` (SATIS-3: bUnit metin iddiası "birebir eşit olmalıdır" bulunamadı; `FisOdemeBacaklari.razor` 17:53'te SATIS-3 tarafından değişti; test CSS okumaz — `grep CssKaynak|olcek` = 0).
- **Madde (7): bu paketin ürettiği yeni kırmızı 0** (üçü de CSS okumayan, başka şeridin dosyasına bağlı testler).

## 5. Mutasyon kanıtları (v10; `v10/mut.sh`, yedek + günlük `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/<kod>/`)
| Kod | Değişiklik (tek eşleşme) | Kırmızı test | cmp |
|---|---|---|---|
| **M-OLCEK-1-1** (spes., v10 tekrarı) | belirtecler.css PRO bloğuna `--kontrol-yuksekligi: 2rem` | OL1, OL2 · **canlı** tutarlilik /emanet,/cariler,/islemler 1440: **1 → 12 fark** (ör. `/emanet #e-cari 44→39`, `/islemler #islem-tur-SATIS 44→32`, `#islem-barkod 44→32`) | AYNI |
| M-OLCEK-1-45 | 11a borçlu eylem `height` → auto | OL23 | AYNI |
| M-OLCEK-1-46 | 11b kırılım öğesi yazısı → 15px | OL23, OL13 | AYNI |
| M-OLCEK-1-47 | 11c yeni satış `height` → 54px | OL23 | AYNI |
| M-OLCEK-1-48 | 11d segment yazısı → inherit | OL23 | AYNI |
| M-OLCEK-1-49 | 11e ad sütunu 14 → 9 × tablo yazısı | OL23 (iki kez: sınır eklenmeden önce ve sonra) | AYNI |
| M-OLCEK-1-50 | 11b açıcı seçicisi `.pozisyon-kart--sade #pozisyon-ayrinti` (html[data-mod] kapsamı düştü) | OL23, OL6 | AYNI |
| M-OLCEK-1-51 | 11d "Diğer süzgeçler" yazısı → inherit | OL23 | AYNI |
| M-OLCEK-1-52 | 11e medya sınırı `and (max-width: 1399px)` kaldırıldı | OL23 | AYNI |
Test açığı yok. Son mutasyondan sonra `cmp olcek.css v10/olcek-v10.css` AYNI, `belirtecler.css` doğrulayıcı yedeğiyle AYNI, temiz derleme 0 hata. v9'un M-34…M-44'ü ve öncekiler Ekte.

## 6. Gerçek tarayıcı ölçümü (başsız Chrome + CDP)
- **Kiracı:** `PAKET-OLCEK-1J-TEST Kuyumculuk ve Mücevherat…` (01M3M4QEAQAWDKRJRWPJ9VPTSA), API 5186 (`Sistem__Anahtari` rastgele, dosya chmod 600, sonra boşaltıldı) + `demo-tohumla.js --prova-kiraci --eposta` (tohum yazıldı; betiğin kendi V8 sayım kontrolü, kiracı betiğinin açtığı 4 stoksuz üründe "≥3 hareket" bulamadığı için çıkış 1 verdi — v9 ile aynı; kiracı betiğinin ürün açılış stoğu "kur girişi gerekir" 400'ü aldı, cari ucu `/cariler` değil `/cari` → 404, sonra `cari.py` ile 4 uzun adlı müşteri 201). Oturum `inceleme-vekili.py` 5187. Ölçümler SIRALI (aynı kullanıcıyla paralel mod değiştirilmedi).

### 6.1 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 41 rota) — `v10/son/tut/`
| Genişlik | Fark | Açıklama |
|---|---|---|
| 1440 | **0** | v9'daki 11 `/urunler` sarma satırı bu koşumda yok (11e ilk hâli). Sınır (≤1399) eklendikten sonra 3 rotada tekrar (`son/tut2/`): 1440 = 11 `/urunler` satır 61↔80 (yalnız 40+ karakterli iki fikstür adı PRO'da 3 satır; v9 ile aynı içerik sarması) + 1 çakışma; 1280 = 1 çakışma. |
| 1280 | 1 | `/cariler td:—` kalınlık 400↔600 — anahtar çakışması (BASİT telefon hücresi ↔ PRO has hücresi, farklı öğeler; v8 prob kanıtı). |
| 960 | 5 | 1 çakışma + 4 `/fiyatlandirma` satır 164↔215 (PRO fazladan sütun → sarma; v8'den beri). |
| 375 | 23 | `/cariler` BASİT kart yığını (dolgu 0, satır 21) ↔ PRO kendi kabında kayan tablo (dolgu 8, satır 61). YERLEŞİM farkı; yazı aynı. PRO'yu kart yığını yapmak `Cariler.razor`'da `data-etiket` ister (sayfa sahibi). olcek.css § 5 yorumu v6'da bunu anlatacak şekilde düzeltildi. |

### 6.2 Düğme ailesi
- `tutarlilik-dugme` 1440 (iki mod aynı): `btn-birincil`/`btn-ikincil` 44·16 · `btn-kucuk` 34·14 (×95–97) · `btn-buyuk` 52·17 · sınıfsız 34·14 / 44·16. Gerekçeli sapma: şerit `btn-kucuk` 44·16 (/faturalar, /kur, /urunler/agac) → DUGME-2.
- `prob.mjs` (`.dugme` dahil) — sayfa düğme boyları:

| Rota | 1440 B / P | 1280 B / P | 960 B / P | 375 B / P (açık+koyu) |
|---|---|---|---|---|
| / | 34,52 / 34,44 | 34,52 / 34,44 | 34,52 / 34,44 | 44,52 / 44 |
| /cariler | 34,44 / 34,44 | 34,44 / 34,44 | 34,44 / 34,44 | 44 / 44 |
| /urunler | 34,44 / 34,44 | 34,44 / 34,44 | 34,44 / 34,44 | 44 / 44 |
| /emanet, /islemler | 34,44,52 / 34,44,52 | — | — | — |
| /raporlar/kar-zarar | 44 / 44 | — | — | — |
Aile dışı: **0** her hücrede · 34 px'te yazısı 14 olmayan: **0** · yatay kaydırma: **0**.

### 6.3 Hakem senaryosu (`sen.mjs`) — `v10/son/sen-*`
| Genişlik · tema | Ölçü farkı | Kesik | 375'te 44 altı | Yatay |
|---|---|---|---|---|
| 1440 açık / koyu | `#islem-cari` sağ dolgu (gerekçeli) · PRO uzun ad sarması `odeme-bol` 61↔75 (içerik) | 0 | — | 0 |
| 1280 koyu | aynı + çakışma | 0 | — | 0 |
| 960 açık | aynı | 0 | — | 0 |
| 375 açık / koyu | `#islem-cari` + **YENİ: fiş kalem hücre iç dolgusu BASİT 4 ↔ PRO 8** | 0 | **0** | 0 |
Boylar: 1440/1280/960 **34,44,52**; 375 **44,52** (+ PRO `#islem-diger-ac` 54, iki satırlı özet — v8 gerekçesi). Senaryonun PRO "ödeme böl" adımları (`#islem-bacak-ekle`) PRO yüzünde bu kimlikle bulunamıyor (v9'da da "YOK") — PRO ödeme bacağı ölçümü senaryoda yok; v8'de ayrı ölçümle 44×44.
**Yeni fark kaynağı (bu paketin DEĞİL):** `tuval-satis.css` (h) kuralı — SATIS-3 bugün 17:18'de ekledi: `@media (max-width: 600px)` yalnız BASİT kalem tablosunda ad/tutar/eylem iç dolgusu 8 → 4 px (375'te ad tek satır sığsın diye). PRO tablosu 8 px kalıyor → iki modda 4 px fark. Dosya SATIS-3'ün; olcek.css ile ezilmedi (şeridin bilinçli, yorumlu kararını ezmek çakışma yaratır) → § 9 devir.

### 6.4 Sıkışma turu (`tur.mjs` + `analiz.py`, K1–K13) — A/B `v10/tur-v10` ↔ `v10/tur-v9`
Rotalar /, /cariler, /urunler, /islemler, /emanet, /raporlar/kar-zarar × 1440/1280/960/375 × BASİT/PRO açık; 48 sayfa, 32 sekme, 456 katman, hata 0.
- v9 CSS: **11** benzersiz bulgu · v10 CSS: **10**. Fark: `/urunler K3 ad kırılması (Ba960, Pa1280, Pa960)` v10'da KAPANDI. **v10'un eklediği bulgu 0.** Yatay kaydırma 0.
- Kalanlar (paketten önce de var, iki sürümde aynı): GlobalArama 375 SERBEST-YÜZEN katman (KATMAN) · /islemler "Nakit · Ana Kasa" ve "+ Ayrıntı ekle" katmanı ekran kenarında (SATIS-3) · /urunler PRO "Tüm kategoriler" K10 kesik (sayfa sahibi).

### 6.5 Ekran görüntüleri (Read ile görüldü)
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v10-{panel,emanet,cariler,islemler,raporlar_kar-zarar,urunler}-1440.png` (1440×900, solda BASİT, sağda PRO).
- Görülen: Panel BASİT "Tahsil et"/"Öde" eşit küçük kutular, "Yeni satış" 52; /emanet iki modda "Emanet al"/"Emanet iade et" aynı boy ve kutu; /urunler PRO 1440 satır eylemi (⋯) kabın içinde, kısa adlar tek satır. /cariler PRO 1440'ta tablo kabında kayıyor, "Düzenle" kısmen kabın dışında — **v9 § 10d'nin bilinen bedeli** (A/B: 10d ile 1196/1122, 9em ile 1130/1122 — paketten önce de 8 px taşıyordu); çözümü satır eylemlerini "⋯" altında toplamak (Cariler sahibi, § 9).
- 375 / 1280 / 960 / koyu: `v10/prob/{375-aydinlik,375-karanlik,1280-aydinlik,960-aydinlik,1440-karanlik}/` (`375-karanlik/Basit_.png` görüldü).

### 6.6 Doğrulayıcı için tekrar komutları
`node v10/prob.mjs <genişlik> <aydinlik|karanlik> <cdp> "/,/cariler,/urunler"` (vekil 5187 açıkken) · `TUR_HEDEF=http://localhost:5187 node betikler/tur/tutarlilik.mjs "<rotalar>" 1440` · bölmede: `/`'de `document.querySelectorAll('[id^=panel-borclu-eylem-]')[0].getBoundingClientRect().height` → 1280'de 34, 375'te 44.

## 7. Denetim (`dotnet/denetim.sh`)
- 17:00 ve 18:00 (son mutasyondan sonra): **rc=0, 291 ✓, "DENETİM TEMİZ."**
- Paket sayaçları: `OLCEK-1: MOD_OLCU_BILDIRIMI 0` · `OLCEK-1: YUZ_ONEMLI_OLCU 0` · `P-39: DUGME_YUKSEKLIK_CESIDI 2` (tavan 2) · `P-39: DOKUNMA_HEDEFI_KUCUK 0` · `P-18: SABIT_RENK 0`.
- v9'daki rc=1 (EMANET-1'in 0 baytlık razor'u) artık yok; çelişki iki farklı anın ölçümüydü (§ 0).

## 8. DoD sözleşmesi
Yeni varlık, kayıt türü, uç, form ya da düğme yok (yalnız CSS + test). PATCH/DELETE/[RolGerekli]/denetim kaydı maddeleri uygulanamaz; ModalPencere sayacına ve sayfa dosyalarına dokunulmadı.

## 9. Açık kalanlar (sahibiyle)
1. **Kurucu kararı — kenar menüsü PRO yüzü** (KATMAN/T-K2): beyaz listede.
2. **SATIS-3:** (a) YENİ: `tuval-satis.css` (h) ≤600 px BASİT kalem tablosu iç dolgusu 4 px ↔ PRO 8 px (17:18) — tek ölçü kuralına aykırı; iki tabloya aynı değer ya da ürün adı için başka çözüm. (b) `#islem-cari` rozet payı (BASİT 44 px ↔ PRO `calc(46% + 36px)`). (c) PRO ürün adı satır kırar, BASİT üç nokta. (d) /islemler ödeme/ayrıntı katmanları ekran kenarında. (e) tam süitte `KarmaOdemeEkranTestleri` ×2 kırmızı (17:53 `FisOdemeBacaklari.razor`). (f) tuval-satis.css sabit px'leri belirteçe çevrilirse olcek.css § 4, § 5b, § 10b sadeleşir. (g) Hakem senaryosunun PRO ödeme böl adımı `#islem-bacak-ekle`'yi PRO yüzünde bulamıyor — kimlik ya da senaryo güncellenmeli.
3. **Cariler sahibi (ORTAK-1):** /cariler PRO 1440/1280 satır eylemleri "⋯" altında toplanmalı (tablo kabında kayıyor, "Düzenle" kısmen görünür); çekmece başlık/etiket METNİ ve rengi; 375'te PRO tablo ↔ BASİT kart (`data-etiket`). `OrtakGorunumSaflik.O8` kırmızı. `/kasalar .stat-deger` (bilgi).
4. **KATMAN:** PRO satır eylemlerinde emoji (💵 ✏️ 📦 — /urunler iki modda, /cariler PRO) → Ikon bileşeni. GlobalArama 375 serbest yüzen katman.
5. **DUGME-2 / sayfa sahipleri:** şerit `btn-kucuk` 44·16 (8 görünüm) sınıfı razor'da `btn`; Panel `.dugme`/`.pozisyon-*` ve /cariler `.musteri-segment-oge` ölçüleri olcek.css § 11'de köprü — razor'da aile sınıfına bağlanınca köprü kalkar; `/ayarlar #ayar-gorunum-ayrinti-baslik` 50; Bildirimler.razor eski sınıflar (§ 10f köprü). `tutarlilik-dugme` aracı `.dugme` sınıfını saymıyor → araç genişletilmeli (v9'un Panel'i kaçırmasının sebebi).
6. **Ürünler sahibi:** /urunler PRO "Tüm kategoriler" seçici metni K10 kesik (1440/1280/960).
7. **Orkestratör:** (a) tutarlilik.mjs + sen.mjs + prob.mjs (`.dugme` dahil) kapı adımı olsun; (b) ölçüm yöntemi: izolasyon kuralı bölmeyi yasaklıyor, doğrulayıcı bölme istiyor — tek kural yazılmalı; (c) inceleme yayını (`~/.kuyumcu-yayin`) v10 ile yeniden `yayin-al`.

## 10. Temizlik
- API 5186 (PID 22479 + alt süreç 22488) ve vekil 5187 (PID 23714) `kill` ile durduruldu; `lsof` 5186/5187 **boş**. 5198/5199/5200'e dokunulmadı.
- Kiracı `PAKET-OLCEK-1J-TEST` (01M3M4QEAQAWDKRJRWPJ9VPTSA) `kiraci-temizle.sh --uygula` ile silindi; ikinci koşum "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)"; tenant_id kolonlu **70 tablo, 0 satır**; `%OLCEK%` kiracı = 0.
- **Davet temizliği (yeni adım):** `kiraci_davetleri`'nde `PAKET-OLCEK-1%` adlı, kiracısı artık olmayan **6 satır silindi** (1E, HAKEM, 1F, 1G, 1H ve bu turun 1J'si); `%OLCEK%` davet = **0**.
- Kiracı sayısı: başlangıç **18** → (benim +1) → son **18**. QA-TEST-*/DEMO-* ve diğer şerit kiracılarına dokunulmadı.
- Fikstür ve sistem anahtarı dosyaları `printf '' >` ile boşaltıldı (0 bayt). Proje ağacına .bak/.tmp bırakılmadı; tüm geçici CSS değişimleri (mutasyonlar, tur A/B'de v9 CSS'i) `cmp` ile geri doğrulandı.

## 11. Kurucuya not
Doğrulayıcının son turda bulduğu yerler düzeldi:
- **Panel (Basit):** "Tahsil et" / "Öde", "Ayrıntı" ve "Alacak" artık diğer küçük düğmelerle aynı boyda (34). Telefonda parmakla rahat basılsın diye 44. "Yeni satış" büyük düğme boyunda (52).
- **Müşteriler (Basit):** "Hepsi / Borçlu / Toptancı" ve "Diğer süzgeçler" yazısı diğer küçük düğmelerle aynı boyda.
- **Ürünler (Profesyonel):** "22 Ayar Burma Bilezik" gibi kısa adlar tablette ve dizüstünde artık iki satıra bölünmüyor. Geniş ekranda tablo zaten sığıyor, orada bir şey değişmedi. Satır sonundaki "⋯" düğmesi görünür kalıyor.

Göze çarpabilecek bir şey de şu: Profesyonel müşteri listesinde "Düzenle" düğmesi geniş ekranda tablonun kenarından biraz taşıyor ve tablo yana kayıyor. Düzeltmek için düğmeleri "⋯" altında toplamak gerekiyor. Bu iş müşteri ekranının sahibine iletildi.

---
# Ek — v9 raporu (14:30–16:00, değiştirilmeden)

## (v9 başlığı) Uygulama raporu v9

**Tarih:** 2026-09-28 14:30–16:00 (tasarım hakemi tur 3 sonrası) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9955–9969)
**v9 neden var:** v8 (13:35) sonrası tasarım hakemi (14:00–14:25, `/private/tmp/kuyumcu-serit/OLCEK-1-hakem3/`) üç KRİTİK + bir ORTA + dört DÜŞÜK bulgu verdi. Ağaç korundu (olcek.css ilk 590 satırı v8 ile birebir — `diff` ile doğrulandı), yalnız **olcek.css § 10** ve **OL22** eklendi; tüm kabul ölçümleri güncel ağaçta yeniden koşuldu. Devralmada yarım mutasyon YOK: `belirtecler.css`/`olcek.css` doğrulayıcı yedekleriyle (`dogrulama-yedek/pOLCEK-1`) `cmp` AYNI; `belirtecler.css` sonunda `#islem-vazgec` mutasyon satırı yok.
**Ölçüm yöntemi:** İzolasyon kuralı (27.09) "tarayıcı bölmesini KULLANMA" dediği için ölçümler başsız Chrome + CDP (`betikler/tur/cdp.mjs`) ile, kendi profil dizinlerimde, parolasız vekil oturumuyla alındı. Doğrulayıcının "bölme sekmesi" notu ile izolasyon kuralı çelişiyor → yöntem farkı kapı kaydına (orkestratöre) bildirildi.

## 0. Tasarım hakemi tur 3 bulgularının kapanışı (v9, güncel ağaçta ölçüldü)

Ölçüm aracı: hakemin kendi `sen.mjs`'i (kopyası `v9/sen.mjs`; iki kalemli dolu fiş + 7 haneli fiyat + MASAK, ödeme böl/sil, kaydet hatası, müşteri çekmecesi + boş ad hatası, Emanet Al, kâr-zarar) · 1440 açık/koyu · 1280 koyu · 960 açık · 375 açık/koyu. Çıktılar `/private/tmp/kuyumcu-serit/OLCEK-1/v9/son/sen-*`.

| Bulgu | Durum | Kanıt (v9) |
|---|---|---|
| [KRİTİK] Çekmece: Kaydet 282↔93 px, başlık 21↔18, etiket 15/400↔18/500, içerik 387↔447, alt şerit farklı | **KAPANDI** (§ 10a) | 1440 iki mod: `#cekmece-cari-kaydet` 44×282 · 18 · 500; `-iptal` 44×96; `#cekmece-cari-baslik` 21 px / 600; etiketler 15 px / 400; `#duz-ad` 44×387; çekmece 440 px, dolgu 24/26, gövde 18/26, alt şerit 18/26/24 (tuval Musteriler `<aside>` ölçüleri). Hata durumunda "Kaydet kilitli" sebebi iki modda tam satır (PRO'da Kaydet'i daraltıyordu). Senaryo farkı: cari-cekmece **0** ölçü farkı (yalnız § 6.1'deki anahtar çakışması). Görüntü `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v9-cariler-cekmece-1440.png` (Read ile görüldü). **Kalan:** başlık METNİ (müşteri adı ↔ "Cari Düzenle"; 375'te 2↔1 satır → yükseklik 56↔28) ve etiket METNİ ("Ad soyad" ↔ "Ad / Firma Adı") Cariler.razor'da; başlık/etiket RENGİ ve alt şerit ZEMİNİ olcek.css'e yazılmaz (OL6 sözleşmesi: yalnız ölçü) → § 9. |
| [KRİTİK] Fiş kalem satırı 18/71/14 ↔ 15/61/8; MASAK h3 21,06↔18,72; "Neden?" 15↔14 | **KAPANDI** (§ 10b, 10c) | İki modda kalem hücresi **16 px · dolgu 8 (ilk/son/ürün adı 16) · satır 61**; MASAK h3 **18 px**; `#islem-tevsik-neden` **14 px, 34 px** yükseklik. 1440/1280/960/375 senaryolarında fiş satırında ölçü farkı 0 (bkz. not). Kesik: PRO'da 7 haneli tutar, işçilik, has **0** (sütunlar 16 px'e göre genişletildi). **Neden 16, 18 değil** (hakemin önerisi 18): PRO kalem satırı 10 sütunlu, 1440'ta kalem kartı 786 px; 18 px'te sayı sütunları ürün adını ≈60 px'e indiriyor (tuval Profesyonel satırı da 1440'ta üst üste biniyor — SATIS-3'ün T-S2 notu). 16 px BASİT'in ≤1399 değeri ve PRO şeridinin kendi değeri (`--tp-16`); yazı KÜÇÜLTÜLMEDİ, iki yüz TEK değere bağlandı. GEREKÇELİ FARK (tuvale göre): BASİT 1440 18→16. |
| [KRİTİK] 960 /cariler 24 karakterlik ad 2–3 satır (K3) | **KAPANDI** (§ 10d) | Ad sütunu (yalnız /cariler) en az 14 × tablo yazısı (≈210 px). ab960.mjs: "Hasan Hüseyin Çelikkanat", "Mehmet Ali Kayaoğlu" iki modda **1 satır**; 37 karakterlik ad 2 satır (kelime sınırında, kırpma yok). tur A/B: v8'deki `/cariler K3 (Ba960, Pa960, Pa1280, Pa1440)` bulgusu v9'da **yok**. Bedeli: tablo 960'ta kabında 768/642 kayar (v8 703/642; olcek.css kapalıyken 907/642); sayfa yatay kaydırması 0. Eylemlerin 3 satıra sarılması denendi (satır 131 px) ve reddedildi; "⋯ altında topla" razor işi → § 9. |
| [ORTA] `#islem-cari` sağ dolgu 44 ↔ 258 (375: 197) | **GEREKÇELİ FARK — karar SATIS-3/orkestratör** | Kural `tuval-satis.css` `.tuval-pro … .secici-tetik { padding-right: calc(46% + 36px) }` (PRO rozeti kutunun içinde). Kutu boyu/yazı/köşe/kalınlık iki modda aynı. § 9. |
| [DÜŞÜK] /bildirimler sınıfsız düğmeler 30 px | **KAPANDI** (§ 10f, sayfa dosyasına dokunmadan) | `#bildirim-sekme-*`, `#is-ayar-kaydet`, `#is-ayar-varsayilan` iki modda 44 · 16 px · dolgu 0 16 · köşe 8. `#panel-yeni-satis` 54 (yalnız BASİT yüzde) ve `/ayarlar #ayar-gorunum-ayrinti-baslik` 50 → § 9 (tekil, iki modda aynı). |
| [DÜŞÜK] /kur sekmesi 15 px | **KAPANDI** (§ 10e) | `#kur-sekme-guncel` ve /yedekler sekmeleri iki modda 44 · **16 px**. Köşe (hap 999px) ve dolgu 6/14 şerit tasarımı olarak kaldı. |
| [DÜŞÜK] Testler yüz sınıfındaki normal ölçü farkını görmüyor | **KISMİ** | OL22 v9 kurallarının İKİ yüzü kapsadığını (yüz sınıfı taşımadığını) ve şerit kuralından özgül olduğunu sınıyor. Genel çözüm (tarayıcı karşılaştırması kapı adımı: `tutarlilik.mjs` + `sen.mjs`) orkestratör kapısına öneri → § 9. |
| [DÜŞÜK] Ölçüm bölmede değil CDP ile | **YÖNTEM FARKI KAYITLI** | Bkz. üst not (izolasyon kuralı). |
| Ek (v9 ölçümünde bulundu) | KAPANDI | PRO sağ sütun "Yasal kapılar uygun" açıcısı 21 px (aile dışı) → küçük boy 34 (§ 10c). |

**Not (fiş satırı yüksekliği):** bir koşumda PRO `odeme-bol` durumunda uzun ürün adı ("22 Ayar Burma Bilezik Özel Seri El İşçiliği Kalın Model") ürün sütununda 3 satıra sarıp satırı 75 px yaptı; BASİT aynı adı tek satır üç noktayla kısaltır (61). Bu ölçü kuralı değil İÇERİK davranışı farkı: SATIS-3 PRO'da "ürün adı kesilmez, satır kırılır" (tuval-satis.css ~887), BASİT'te tuval Tezgah üç nokta. § 9 SATIS-3.

## 1. Özet
- Kök neden (v1): `belirtecler.css` `html[data-mod=profesyonel]` bloğu ölçü küçültüyordu → bloklar nötr (yalnız `--arayuz-modu`). Değişmedi.
- Tek ölçek: düğme 34 / 44 / 52 (telefonda 44 / 44 / 52) · yazı 14 / 16 / 17 · tablo hücre 15, başlık 13, dolgu 8, satır en az 61 · **fiş kalem hücresi 16 (v9)** · **çekmece başlığı 21, etiket 15 (v9, tuval Musteriler)**.
- **Sonuç (41 rota, tutarlilik.mjs, BASİT↔PRO, sıralı koşum):** 1440: 12 satır = 1 anahtar çakışması + 11 `/urunler` içerik sarması (uzun ürün adı fikstürü; v8 CSS'iyle A/B aynı 11 satır) · 960: 5 = 1 çakışma + 4 `/fiyatlandirma` içerik sarması · 375: 23 = `/cariler` kart↔tablo yerleşimi. **Ölçü kuralından doğan fark 0.** Senaryo (sen.mjs): 1440/1280/960/375 yalnız `#islem-cari` (gerekçeli) + çakışma (+375 çekmece başlık METNİ).

## 2. Spesifikasyonun koda karşı doğrulanması ve sapmalar (v9 ekleri; v8 tablosu Ekte)

| Madde | Kodda (28.09 14:30) | Uygulama / sapma |
|---|---|---|
| "Sayfa dosyalarına dokunma" | Çekmece/fiş/cariler farkları şerit CSS'inde ve iki ayrı yüz bileşeninde | Yalnız olcek.css § 10 (özgüllükle); razor/sözlük değişmedi. **Dokunulan sayfalar: yok.** |
| Tuval birebir (çekmece) | Tuval Musteriler `<aside>`: 440 · 24/26 · başlık 21/600 · etiket 15 soluk · Kaydet flex-grow 1 | İki yüz bu ölçüye bağlandı (PRO'yu tuvale taşıdı). Renk kısmı OL6 gereği dışarıda. |
| Tuval birebir (fiş satırı 18 px) | Tuval PRO satırı 1440'ta sığmıyor | **SAPMA:** 16 px tek değer (§ 0 gerekçe). |
| olcek.css yalnız ölçü (OL6) | İlk sürüm renk yazıyordu → OL6 kırmızı | Renk bildirimleri çıkarıldı; yazı boyları yeni belirteçlerle (`--yazi-cekmece-baslik`, `--yazi-etiket`, OL13). |
| app.css'e yazma | — | Yazılmadı. |

Veri modeli, uç, migration, mevzuat: **gerekmedi.** Kaydet/POST yolları, yetki, DOM id'leri değişmedi.

## 3. Değişen dosyalar
Bu tur (v9):
- `dotnet/Kuyumcu.Web/wwwroot/css/olcek.css` (721 satır; v8 593): `:root`'a `--yazi-cekmece-baslik`, `--yazi-etiket`; **§ 10a–10f yeni** (çekmece iki yüz · fiş kalem satırı iki tablo + PRO sütun genişlikleri · MASAK başlığı, "Neden?", PRO yasal açıcısı · /cariler ad sütunu · sekme yazısı kimlik ağırlığı · /bildirimler düğmeleri).
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs`: **OL22 yeni** (toplam 24 test).
Önceki turlardan (korundu, değişmedi): `belirtecler.css` (nötr mod blokları), `index.html` bağlantısı, `denetim.sh` OLCEK-1 sayaçları, olcek.css § 0–9, OL1–OL21, `betikler/tur/tutarlilik.mjs`, `olcek-senaryo.mjs`.

`git status --porcelain | grep -v -E "/(bin|obj)/"` (paketin dosyaları; ağaç tek ilk commit'ten beri izlenmiyor):
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Tests/                 (OlcekTekTestleri.cs dahil)
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css
?? dotnet/denetim.sh
```
Yeni görüntüler: `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/OLCEK-1-v9-{islemler,islemler-fis-dolu,cariler-cekmece,emanet,emanet-al-acik,raporlar_kar-zarar}-1440.png`.

## 4. Testler
- **Derleme** (`dotnet build Kuyumcu.sln`, son mutasyondan sonra): **0 Hata, 1 Uyarı** — uyarı başka şeridin: `EtiketEkranTestleri.cs(145,47) xUnit1031` (v8 raporunda da not edilmişti).
- **Filtreli:** OlcekTek **24/24**. CSS test ailesi (OlcekTek + P39 + TasarimBelirtec + OrtakGorunum + KatmanTekKalip + S1hGorselSigma + TK3 + Css): 208 → **207 geçti, 1 kırmızı** `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (beklenen 15, gerçek 16 sözlük metni — ORTAK-1'in sözlüğü; CSS okumaz).
- **Tam süit** (ağaç içi, `/private/tmp/kuyumcu-serit/trx-pOLCEK-1/v9/`): **6584 toplam · 6581 geçti · 3 başarısız** — hepsi başka şeridin, hiçbiri CSS okumuyor:
  - `OrtakGorunumSaflik.O8_…` (ORTAK-1: sözlük metin sayısı 15→16),
  - `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini_kullanir_muafiyet_YAZILIDIR` (sayfa sayısı 47→48: 14:54'te açılan **0 baytlık** `Pages/EmanetHamKap.razor` — EMANET-1),
  - `ZOISLEMAEkranTestleri.ZO28b_pos_iptal_onayi_VAZGEC_solda_kirmizi_eylem_sagda` (ZO-ISLEM-A bUnit).
  **Madde (7): bu paketin ürettiği yeni kırmızı 0.**

## 5. Mutasyon kanıtları (v9; `v9/mut.sh`, yedek + günlük `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/<kod>/`)

| Kod | Değişiklik (tek eşleşme) | Kırmızı test | cmp |
|---|---|---|---|
| **M-OLCEK-1-1** (spes., v9 tekrarı) | belirtecler.css PRO bloğuna `--kontrol-yuksekligi: 2rem` | OL1, OL2 · **canlı** tutarlilik /emanet,/cariler,/islemler 1440: **1 → 12 satır** (ör. `/emanet #e-cari 44→39`, `/islemler #islem-tur-SATIS 44→32`) | AYNI |
| M-OLCEK-1-34 | 10a başlık kuralı `--sade` yüzüne daraltıldı | OL22 | AYNI |
| M-OLCEK-1-35 | 10a Kaydet `flex: 1 1 auto` → `0 1 auto` | OL22 | AYNI |
| M-OLCEK-1-36 | 10b fiş dolgusu `var(--tablo-dolgu)` → `.75rem .5rem` | OL22 | AYNI |
| M-OLCEK-1-37 | 10b PRO fiş yazısı `--yazi-kontrol` → `--tablo-yazi` | OL22 | AYNI |
| M-OLCEK-1-38 | 10c MASAK h3 `--yazi-lg` → `--yazi-xl` | OL22 | AYNI |
| M-OLCEK-1-39 | 10d ad sütunu 14 → 9 × tablo yazısı | OL22 | AYNI |
| M-OLCEK-1-40 | 10e `:not(#olcek-kimlik-agirligi)` → `:not(.olcek-yok)` | OL22 | AYNI |
| M-OLCEK-1-41 | 10f `#is-ayar-varsayilan` min-height → 30px | OL22 | AYNI |
| M-OLCEK-1-42 | 10b tutar sütunu → 112px | OL22 | AYNI |
| M-OLCEK-1-43 | 10c "Neden?" min-height → 0 | OL22 | AYNI |
| M-OLCEK-1-44 | 10a kilit sebebi `flex: 1 1 100%` → `0 1 auto` | OL22 (test bu tur güçlendirildi: ilk koşumda kilit sebebi kuralı sınanmıyordu) | AYNI |

Test açığı kalmadı. Son mutasyondan sonra `cmp olcek.css olcek-v9.css` AYNI ve temiz derleme 0 hata. v8'in M-24…M-33'ü ve önceki M-2…M-23c Ekte / önceki raporlarda.

## 6. Gerçek tarayıcı ölçümü (başsız Chrome + CDP)
- **Kiracı:** `PAKET-OLCEK-1H-TEST Kuyumculuk ve Mücevherat…` (01M3KWFXKP4DEN2W0A2AN9K8AF), API 5186 (`Sistem__Anahtari` rastgele, dosya chmod 600, sonra boşaltıldı) + `demo-tohumla.js --prova-kiraci --eposta` (6+ cari, 12 ürün, 9 satış; betiğin V8 kontrolü önceden açılan 4 stoksuz üründe "≥3 hareket" bulamadığı için çıkış 1 verdi — tohum verisi yazıldı, yalnız kendi sayım kontrolü tutmadı) + 4 uzun adlı müşteri (hakem fikstürü). Oturum `inceleme-vekili.py` 5187.
- Ölçümler **sıralı** koşuldu (aynı kullanıcıyla paralel mod değiştirmek ölçümü bozar — BENIOKU). İlk keşif koşumlarında paralel koşu yapılmıştı; rapordaki bütün sayılar sıralı `olc-sira.sh` ve `tur-ab.sh` koşumlarından.

### 6.1 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 41 rota) — `v9/son/tut/`
| Genişlik | Fark satırı | Açıklama |
|---|---|---|
| 1440 | 12 | 1: `/cariler td:—` kalınlık 400↔600 anahtar çakışması (BASİT telefon hücresi ↔ PRO has hücresi; farklı öğeler, v8 § 6.1 prob kanıtı). 11: `/urunler` satır 61↔80 — PRO fazladan sütunlarla uzun ürün adını ("22 Ayar Burma Bilezik Özel Seri…") iki satıra sarıyor; **A/B v8 CSS'iyle aynı 11 satır** (`v9/tut/ab8/`) → v9'dan değil, içerik. v8'deki `/cariler` satır 61↔80 farkı (5 satır, ad sarması) v9'da **yok**. |
| 960 | 5 | 1 çakışma + 4 `/fiyatlandirma` satır 164↔215 (PRO fazladan sütun → sarma; v8'de de vardı). |
| 375 | 23 | `/cariler` BASİT kart yığını ↔ PRO yatay kayan tablo (yerleşim; v8 § 6.1). |

### 6.2 Düğme ailesi (tutarlilik-dugme 1440, iki mod aynı)
`btn-birincil`/`btn-ikincil` 44·16 · `btn-kucuk` 34·14 (×97–98) · `btn-buyuk` 52·17 · sınıfsız 34·14 / 44·16. Gerekçeli sapma (v5'ten): şeritteki `btn-kucuk` 44·16 (/faturalar, /kur, /urunler/agac) → DUGME-2/sayfa sahipleri. Senaryo boyları: 1440/1280/960 **34,44,52**; 375 **44,52** (+ PRO `#islem-diger-ac` 54: iki satırlı özet, v8 § 6.2 gerekçe). v8'deki PRO 21 px (`#islem-pro-yasal-uygun`) kapandı.

### 6.3 Hakem senaryosu (`sen.mjs`) — `v9/son/sen-*`
| Genişlik · tema | Ölçü farkı | Kesik | Yatay |
|---|---|---|---|
| 1440 açık | 5× `#islem-cari` (gerekçeli) + 2× çakışma (+ bir koşumda PRO uzun ad sarması, § 0 not) | BASİT ürün adı üç nokta (tasarım; tuval Tezgah) · PRO 0 | 0 |
| 1440 koyu | aynı | aynı | 0 |
| 1280 koyu | aynı | aynı | 0 |
| 960 açık | 5× `#islem-cari` | 0 | 0 |
| 375 açık / koyu | 5× `#islem-cari` + 2× `#cekmece-cari-baslik` yükseklik 56↔28 (başlık METNİ farkı; yazı 21 iki modda) | BASİT ürün adı üç nokta | 0 |
375'te 44 altı dokunma hedefi: **0** (her durumda `kucuk44: []`).

### 6.4 Sıkışma turu (`tur.mjs` + `analiz.py`, K1–K13) — A/B `v9/tur-v9` ↔ `v9/tur-v8`
Rotalar /islemler, /cariler, /bildirimler, /kur, /emanet, /raporlar/kar-zarar × 1440/1280/960/375 × BASİT/PRO açık; 48 sayfa, 48 sekme, 316 katman, hata 0.
- v8 CSS: **11** benzersiz bulgu · v9 CSS: **10**. Fark: `/cariler K3 ad kırılması (Ba960, Pa1280, Pa1440, Pa960)` v9'da KAPANDI. **v9'un eklediği bulgu 0.**
- Kalanlar (paket öncesinden, hepsi iki sürümde aynı): GlobalArama 375 SERBEST-YÜZEN katman (KATMAN) · /islemler "Nakit · Ana Kasa" ve "+ Ayrıntı ekle" katmanı ekran kenarında (SATIS-3) · /bildirimler Pa960 tarih hücresi K3 (ORTAK-1).
- Yatay kaydırma 0 · K5 (TRY) 0.

### 6.5 Ekran görüntüleri (Read ile görüldü)
- `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v9-{islemler,islemler-fis-dolu,cariler-cekmece,emanet,emanet-al-acik,raporlar_kar-zarar}-1440.png` (solda BASİT, sağda PRO, 1440×900 aynı boy).
- Görülen (kilit sebebi düzeltmesinden SONRAKİ 1440 koşumu): çekmece iki modda aynı genişlik/dolgu/başlık boyu/etiket boyu; Vazgeç + geniş Kaydet, altında "Kaydet kilitli: Ad / Firma Adı" iki modda aynı yerde. Fark yalnız metin (başlık, etiket) ve PRO'da fazladan alanlar (mod içeriği). 960 BASİT /cariler (`v9/prob3-Basit-960_v9.png`): adlar tek satır, tablo kabında kayıyor.
- 375 / 1280 koyu kareler: `v9/son/sen-375-karanlik/`, `v9/son/sen-1280-karanlik/`.

## 7. Denetim (`dotnet/denetim.sh`, son mutasyondan sonra)
- **rc=1, "DENETİM BAŞARISIZ"** — iki ✗, ikisi de başka şeridin: `Pages/*.razor dosya sayısı 48 (tavan 47 — ARTIŞ)` ve `P-37c: SAYFA_BASI_KAPSAM acik=1` → ikisinin sebebi 14:54'te açılan 0 baytlık `Pages/EmanetHamKap.razor` (EMANET-1 alanı: Emanet*). Dokunulmadı.
- **Paketin sayaçları ✓:** `OLCEK-1: MOD_OLCU_BILDIRIMI 0` · `OLCEK-1: YUZ_ONEMLI_OLCU 0` · `P-39: DUGME_YUKSEKLIK_CESIDI 2` (tavan 2) · `P-39: DOKUNMA_HEDEFI_KUCUK 0` · `P-18: SABIT_RENK 0`. 289 ✓.

## 8. DoD sözleşmesi
Yeni varlık, kayıt türü, uç, form ya da düğme yok (yalnız CSS + test). PATCH/DELETE/[RolGerekli]/denetim kaydı uygulanamaz; ModalPencere sayacına ve sayfa dosyalarına dokunulmadı.

## 9. Açık kalanlar (sahibiyle)
1. **Kurucu kararı — kenar menüsü PRO yüzü** (KATMAN/T-K2): beyaz listede.
2. **SATIS-3 / orkestratör:** `#islem-cari` rozet payı (BASİT 44 px sabit ↔ PRO `calc(46% + 36px)`) — rozet iki modda aynı yöntemle (ikisi de içeride ya da dışarıda). PRO ürün adı satır kırar, BASİT üç nokta → aynı fişte satır yüksekliği uzun adda farklı; tek davranış seçilmeli. /islemler açılan ödeme/ayrıntı katmanları ekran kenarında. tuval-satis.css sabit px'leri (fiş kalem ızgarası) belirteçe çevrilirse olcek.css § 10b sütun genişlikleri sadeleşir.
3. **Cariler sahibi (ORTAK-1):** çekmece başlık METNİ (müşteri adı ↔ "Cari Düzenle") ve etiket METNİ ("Ad soyad" ↔ "Ad / Firma Adı", "(isteğe bağlı)" yalnız PRO) tek metne; başlık/etiket RENGİ ve alt şerit ZEMİNİ (BASİT `--yuzey` ↔ PRO app.css `--yuzey-2`) — olcek.css renk yazmaz (OL6). 960'ta satır eylemleri "⋯" altında toplanmalı (şu an tablo kabında 126 px kayıyor). 375'te PRO müşteri tablosu yatay kayıyor, BASİT kart (v8). `#duz-notlar` satır sayısı moda göre.
4. **KATMAN:** PRO satır eylemlerinde emoji (💵 ✏️ 📦) → Ikon bileşeni. GlobalArama 375'te serbest yüzen katman.
5. **DUGME-2 / sayfa sahipleri:** şerit `btn-kucuk` 44·16 (8 görünüm) sınıfı razor'da `btn` olmalı; `#panel-yeni-satis` 54 (yalnız BASİT), `/ayarlar #ayar-gorunum-ayrinti-baslik` 50 (iki modda aynı, aile dışı); Bildirimler.razor `secili`/`ikincil` sınıfları `btn` ailesine (CSS § 10f geçici köprü).
6. **EMANET-1:** 0 baytlık `Pages/EmanetHamKap.razor` → denetim ✗ ×2 + `P37c…muafiyet_YAZILIDIR` kırmızı.
7. **ORTAK-1:** `OrtakGorunumSaflik.O8` (sözlük 15→16). `/bildirimler` Pa960 tarih hücresi K3. `/kasalar .stat-deger` (bilgi).
8. **ZO-ISLEM-A:** `ZOISLEMAEkranTestleri.ZO28b_…` kırmızı.
9. **Orkestratör:** (a) tutarlilik.mjs + sen.mjs (dolu form/çekmece durumları) kapı adımı olsun — metin testleri yüz sınıfındaki normal ölçü farkını göremez; (b) ölçüm yöntemi: izolasyon kuralı bölmeyi yasaklıyor, doğrulayıcı bölme istiyor — tek kural yazılmalı; (c) inceleme yayını (`~/.kuyumcu-yayin`) v8 raporundaki mutasyonlu belirtecler.css anlık görüntüsünü taşıyordu — v9 ile yeniden `yayin-al`.

## 10. Temizlik
- API 5186 (PID 37978) ve vekil 5187 `kill` ile durduruldu; `lsof` 5186/5187 **boş**. 5198/5199/5200'e dokunulmadı.
- Kiracı `PAKET-OLCEK-1H-TEST` (01M3KWFXKP4DEN2W0A2AN9K8AF) `kiraci-temizle.sh --uygula` ile silindi; tenant_id kolonlu **70 tablo, 0 satır**; `PAKET-OLCEK%` = 0. Kiracı sayısı: başlangıç 19 → (benim +1) → son **18**; aradaki −1 başka şeridin kendi kiracısını silmesidir (ben yalnız kendiminkini sildim). QA-TEST-*/DEMO-* ve diğer şerit kiracılarına dokunulmadı.
- Fikstür ve sistem anahtarı dosyaları `printf '' >` ile boşaltıldı (0 bayt). Proje ağacına .bak/.tmp bırakılmadı; bütün geçici CSS değişimleri (mutasyonlar, tur A/B'de v8 CSS'i) `cmp` ile geri doğrulandı.

## 11. Kurucuya not
Tasarım hakeminin son turda bulduğu üç yer düzeldi:
- **Müşteri düzenleme penceresi** Basit'te de Profesyonel'de de artık aynı: aynı genişlik, aynı başlık boyu, aynı etiket boyu, aynı "Kaydet" düğmesi (tasarım taslağındaki gibi geniş). Kalan tek fark yazılar: Basit'te başlıkta müşterinin adı, Profesyonel'de "Cari Düzenle" yazıyor. Etiket yazıları ve renkler de ayrı; bunlar müşteri ekranının sahibine iletildi.
- **Satış fişindeki ürün satırları** iki modda aynı yazı boyunda (16). Taslaktaki 18 Profesyonel'in 10 sütunlu satırına sığmıyor, o yüzden ikisi de 16. MASAK başlığı ve "Neden?" bağlantısı da iki modda aynı boyda.
- **Tablet genişliğinde müşteri listesi:** kısa adlar artık iki satıra bölünmüyor. Bunun için tablo kendi içinde biraz kayıyor. Düğmeleri "⋯" altında toplamak müşteri ekranının sahibine iletildi.

---
# Ek — v8 raporu (13:35, değiştirilmeden)

**Tarih:** 2026-09-28 12:25–13:45 (devir sonrası koşum) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **İzolasyon:** t8 (KUYUMCU_TEST_DB=kuyumcu_test_t8 · API 5186 · vekil 5187 · CDP 9951/9953/9954/9956–9958)
**Kaynak:** Kurucu 25.09 ~11:45: "ikisi de aynı ekran biri basit biri profesyonel, emanet al butonuna bi bak boyut değişiyor resmen…"
**v8 neden var:** v5 raporundan sonra ağaçta v6/v7 CSS'i (olcek.css § 7–8, testler OL16–OL20) yazılmış ama rapor güncellenmemiş, koşum kesilmişti. Bu tur ağacı korudu, yarım kalan mutasyonu temizledi, tasarım hakeminin son bulgularını ölçerek kapattı, kalanları (§ 9 v8) ekledi ve tüm kabul ölçümlerini güncel ağaçta yeniden koştu. `/tmp` yeniden başlatmada silinmişti; v5'in ölçüm araçları (olc.mjs, kpage.mjs) yok — v8 ölçümleri resmî araçlarla (`betikler/tur/tutarlilik.mjs`, `olcek-senaryo.mjs`, `tur.mjs` + `analiz.py`) alındı.

### v8 · 0. Devralma bulguları (önce bunlar)

| Konu | Bulgu | İşlem |
|---|---|---|
| **Yarım mutasyon** | `belirtecler.css` sonunda `.tuval-pro #islem-vazgec { font-size: 13px !important; }` satırı duruyordu (OL16'nın "hakem örneği" mutasyonu, geri alınmamış). OL16 kırmızıydı. | Satır (ve önündeki boş satır) silindi; mutasyonlu hâl `/private/tmp/kuyumcu-serit/OLCEK-1/v8/kalinti/belirtecler.css.mutasyonlu` altında saklı. Sonra OL16 yeşil. Aynı mutasyon M-OLCEK-1-24 olarak usulüne uygun tekrarlandı (cmp AYNI). |
| **İnceleme yayını** | `~/.kuyumcu-yayin/wwwroot/css/belirtecler.css` (12:08 anlık görüntüsü, 5199/5200'ün sunduğu) aynı mutasyon satırını TAŞIYOR. | 5199/5200'e dokunulmadı. **Orkestratöre:** yayın bir sonraki `yayin-al` ile yenilenmeli; o zamana dek PRO /islemler "Vazgeç" 13 px görünür. |
| `/tmp` silinmişti | v5 araçları, yedekleri ve eski kiracının fikstürü yok. | Yeni kiracı kuruldu; eski şerit kiracısı `PAKET-OLCEK-1F-TEST` (fikstürü kayıp) da silindi (§ 10). |

### v8 · 0b. Son doğrulama / tasarım hakemi bulgularının kapanışı (v6 + v7 + v8, güncel ağaçta ölçüldü)

Ölçüm: `olcek-senaryo.mjs` (dolu fiş + 7 haneli tutar, müşteri çekmecesi açık, Emanet Al açık) · 1440 açık, 1440 koyu, 960 açık, 375 açık/koyu · önbellek kapalı. Çıktılar `/private/tmp/kuyumcu-serit/OLCEK-1/v8/sen*/`.

| Bulgu | Durum | Kanıt (v8 ölçümü) |
|---|---|---|
| [KRİTİK] Çekmece BASİT 50/17 px, PRO 44/18; kapat 38↔44 | **KAPANDI** (7a + 9b) | `#duz-ad`, `#duz-telefon` 44 px · 18 px · dolgu 0 12 iki modda; `#cekmece-cari-kaydet`/`-iptal` 44 · 18 · 500; kapat 44×44; `#duz-notlar` iki modda 65 px (en az iki satır, satır aralığı 1.3). Senaryo fark: 1440 cekmece **0**. Görüntü `ham/TUVAL-KARSILASTIRMA/OLCEK-1-v8-senaryo-cariler-cekmece-1440.png`. Çekmece GENİŞLİĞİ (BASİT yan panel 440, PRO örtü 480) ve karartma yerleşimdir (§ 6.1). |
| [KRİTİK] `#islem-bacak-sil-0` BASİT 30, PRO 44 | **KAPANDI** (7b) | İki modda 44×44 (1440/960), 375'te 44. |
| [KRİTİK] 16 px yazıda BASİT tutar kesik ("97.552,0") | **KAPANDI** (7c) | 1440 BASİT/PRO `kesik: []`; "1234567,89" tam görünüyor (görüntü `OLCEK-1-v8-senaryo-islemler-tutar-7hane-1440.png`, Read ile görüldü). |
| [YÜKSEK] Dolu fişte 6 boy, 375'te 28 px kalem sil; çekmece kapat 38; `cari-suzgec-daha` 20; /pos/tahsilatlar 960'ta 68 | **KAPANDI** (7b, 7d, 8a–8d, 9c) | Dolu fiş düğme boyları iki modda **34, 44, 52** (1440); 375'te **44, 52** (+ PRO "Diğer ▾" 54, § 6.2 gerekçe). Emanet açık: 34/44/52. Tutarlılık 1440/960 sınıfsız düğmeler 34/44. |
| [ORTA] 375 /cariler yazı boyu moda göre farklı; olcek.css yorumu yanlış | **YAZI KAPANDI · YERLEŞİM GEREKÇELİ** | 375'te iki modda hücre yazısı aynı (tutarlılık yazı farkı 0). Kalan: BASİT kart yığını (hücre dolgusu 0, satır 21) ↔ PRO yatay kayan tablo (dolgu 8, satır 61). Yorum (§ 5) gerçeği anlatacak şekilde düzeltilmiş (v6). § 6.1. |
| [YÜKSEK] PRO satır eylemlerinde emoji | **KAPSAM DIŞI** — sahibi KATMAN (SatirEylemleri). § 9. |
| Orkestratör kararı (25.09 14:00) /islemler karo ↔ sekme | GEREKÇELİ FARK (iki ayrı bileşen); ölçüleri 44 · 16 · 500/600 ile tek. |

### v8 · 1. Özet
- Kök neden (v1): `belirtecler.css` `html[data-mod=profesyonel]` bloğu ölçü küçültüyordu → bloklar nötr (yalnız `--arayuz-modu`).
- Tek ölçek: düğme 34 / 44 / 52 (telefonda 44 / 44 / 52) · yazı 14 / 16 / 17 · tablo hücre 15, başlık 13, dolgu 8, **satır en az 61 (v8)**.
- **Sonuç (41 rota, tutarlilik.mjs, BASİT↔PRO):** 1440: **1** satır (anahtar çakışması, iki farklı öğe) · 960: 7 (içerik sarması + çakışma) · 375: 15 (/cariler kart↔tablo yerleşimi). Ölçü kuralından doğan fark **0**. Dolu form senaryosu: 1440/375 **2** satır (tek öğe `#islem-cari`, rozet payı, gerekçeli).

### v8 · 2. Spesifikasyonun koda karşı doğrulanması ve sapmalar

| İddia / madde | Kodda (28.09) | Uygulama / sapma |
|---|---|---|
| belirtecler.css mod blokları ölçü taşıyor | Satır 2075–2081 yalnız `--arayuz-modu` | Sağlandı. |
| olcek.css "EN SONA" | ORTAK-1 O1 ortak.css'in en son yüklenmesini istiyor | **SAPMA:** olcek.css ortak.css'ten hemen önce (OL5). Kurallar özgüllüğe dayanır (gerekirse `:not(#olcek-kimlik-agirligi)` ile bir kimlik ağırlığı). |
| Kontrol yüksekliği tuvalden (~46) | Tuval PRO 46, Tezgah 50–56 | 44: 375 dokunma kilidiyle tek boy (±2 tolerans). |
| Tablo satırı "tek tanım" | v5: en az 52 px; PRO /cariler satırı satır içi telefon düzenleyicisi (44) yüzünden 61 → 1440'ta 26 satır "boy 52→61" farkı | **v8:** `--tablo-satir-boyu: calc(var(--kontrol-yuksekligi) + 1rem + 1px)` = 61 px (satır içi düzenleyici + 2×8 dolgu + 1 px ayraç; tuval Musteriler 62). 1440 td boy farkı 26 → 0. Yoğunluk tercihiyle birlikte ölçeklenir (sık 49, rahat 65). |
| Tablo yazısı 17 (tuval) | 15 | v5 gerekçesi geçerli (16 px + 12/16 dolguda PRO tabloları taşıyor). |
| Kullanıcı yoğunluk tercihi | Kontrol boyu ve (v8) tablo satır boyu tercihe bağlı | Tablo dolgusu sabit 8 px (v5). |
| DUGME-2 | Kuyrukta ayrı paket (IS-KUYRUGU #12: yazı dikey ortası, kontrast, kesilen düğme) | Bu görev PAKET OLCEK-1; DUGME-2 kapsamına girilmedi (görev sınırı). |

Veri modeli, uç, migration ve mevzuat: **gerekmedi.** Kaydet/POST yolları, yetki, DOM id'leri, razor ve sözlük dosyaları değişmedi. **Dokunulan sayfalar: yok** (yalnız CSS + test).

### v8 · 3. Değişen dosyalar
Bu tur (v8):
- `dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css`: **yalnız yarım mutasyon satırı kaldırıldı** (§ 0).
- `dotnet/Kuyumcu.Web/wwwroot/css/olcek.css` (593 satır): `--tablo-satir-boyu` 61 px; **§ 9 yeni** — 9a ödeme bacağı kalınlığı (araç 500, tutar 600, her genişlik), 9b çekmece BASİT dolgu/not kutusu/Kaydet kalınlığı/"Daha fazla" telefon boyu, 9c `#islem-diger-ayrinti-ac` ve `#e-bilgi-ac` küçük boy (34, telefonda 44); yorumlar 52 → 61.
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs`: OL14 satır boyu beklentisi; **OL21 yeni**. Toplam 23 test.
Önceki turlardan (korundu): olcek.css § 0–8 (v1–v7), OL1–OL20, `denetim.sh` OLCEK-1 sayaçları (MOD_OLCU_BILDIRIMI, YUZ_ONEMLI_OLCU), `index.html` bağlantısı, `betikler/tur/tutarlilik.mjs`, `olcek-senaryo.mjs`.

`git status --porcelain | grep -v -E "/(bin|obj)/"` (paketin dosyaları):
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Tests/                 (OlcekTekTestleri.cs dahil; dizin izlenmiyor)
?? dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
?? dotnet/Kuyumcu.Web/wwwroot/css/olcek.css
?? dotnet/denetim.sh
```
Ağaçta başka şeride ait artık: `dotnet/Kuyumcu.Web/Pages/Islemler.Sade.cs.tmp` (SATIS-3), `dotnet/denetim.sh.mutbak` (18.09). Dokunulmadı.

### v8 · 4. Testler
- **Derleme** (`dotnet build Kuyumcu.sln`, ağaç içi, son mutasyondan sonra): **0 Uyarı, 0 Hata.** (Özel yapıt dizinindeki ilk derlemede başka şeridin `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı görüldü; son ağaç derlemesinde yok.)
- **Filtreli** (OlcekTek + P39 + TasarimBelirtec + OrtakGorunum): önce 115/115, v8 sonrası **116/116 Başarılı**. OlcekTek tek başına 23/23.
- **Tam süit** (ağaç içi, `/private/tmp/kuyumcu-serit/trx-pOLCEK-1/son/`): **6497 toplam · 6496 geçti · 1 başarısız** — `ZOISLEMAEkranTestleri.ZO28_banka_hesap_sahibi_acicisi_IBAN_ve_DOVIZden_SONRA_gelir` (ZO-ISLEM-A, Banka ekranı bUnit; CSS okumaz). v8 değişikliklerinden ÖNCE de aynı tek kırmızı vardı (12:34 koşumu 6494/6493/1). **Madde (7): paketin ürettiği yeni kırmızı 0.**
- Not: testleri özel `--artifacts-path` ile koşmak `CssKaynak.Kok()`'ü (Kuyumcu.sln araması) bozuyor; tam süit bu yüzden ağaç içinde koşuldu. Özel yapıt koşumu 60 yol kaynaklı sahte kırmızı verdi (rapora alınmadı, `v8/tam.log`).

### v8 · 5. Mutasyon kanıtları (`v8/mut.sh`; yedek + günlük `/private/tmp/kuyumcu-serit/mut-yedek/OLCEK-1/<kod>/`)

| Kod | Değişiklik (tek eşleşme) | Kırmızı test | cmp |
|---|---|---|---|
| **M-OLCEK-1-1** (spes.) | belirtecler.css PRO bloğuna `--kontrol-yuksekligi: 2rem` | OL1, OL2 · **canlı** tutarlilik /emanet,/cariler,/islemler 1440: **0 → 12 fark** (ör. `/emanet #e-cari 44→32`) | AYNI |
| M-OLCEK-1-24 | belirtecler.css sonuna `.tuval-pro #islem-vazgec { font-size: 13px !important; }` | OL16 | AYNI |
| M-OLCEK-1-25 | 7a çekmece seçici `height` → 50px | OL17 | AYNI |
| M-OLCEK-1-26 | 7b bacak sil `width` → 30px | OL18 | AYNI |
| M-OLCEK-1-27 | 7c tutar sütunu `calc(var(--yazi-kontrol) * 8.5)` → 112px | OL18 | AYNI |
| M-OLCEK-1-28 | 7d katlanır başlık `min-height` → 48px | OL19 | AYNI |
| M-OLCEK-1-29 | 8a kalem düzenle `height` → 34px | OL20 | AYNI |
| M-OLCEK-1-30 | `--tablo-satir-boyu` → 3.25rem | OL14 | AYNI |
| M-OLCEK-1-31 | 9a tutar kalınlığı 600 → 400 | OL21 | AYNI |
| M-OLCEK-1-32 | 9b not kutusu `min-height` → 50px | OL21 | AYNI |
| M-OLCEK-1-33 | 9c `#e-bilgi-ac` `min-height` → 32px | OL21 | AYNI |

Her mutasyon kendi testini kırdı; test açığı yok. v4/v5'in M-2…M-23c'si önceki raporlarda. Son mutasyondan sonra temiz derleme 0/0.

### v8 · 6. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; tarayıcı bölmesi kullanılmadı)
- **Kiracı:** `PAKET-OLCEK-1G-TEST Kuyumculuk` (01M3KNKZ3X691P10WK4PPJVBKN), API 5186 (`Sistem__Anahtari` rastgele, dosyada chmod 600) + `demo-tohumla.js --prova-kiraci` (6 cari, 12 ürün, 9 satış). Oturum `inceleme-vekili.py` 5187, parolasız. Önbellek: senaryo/foto araçları `setCacheDisabled`; tutarlilik taze profil.
- **Rota kümesi:** 41 rota (`@page` listesi, giriş/kayıt hariç; `v8/rotalar.txt`).

### 6.1 BASİT↔PRO tutarlılık (`tutarlilik.mjs`, 41 rota) — `v8/tut3/`

| Genişlik | Fark satırı | Açıklama |
|---|---|---|
| 1440 | **1** | `/cariler td:—` kalınlık 400↔600: anahtar çakışması — prob ile doğrulandı: BASİT'te telefon hücresi (`hucre-tek-satir`, sütun 1), PRO'da has sütunu (`hucre-vurgu sayi`, sütun 5). Farklı öğeler. (v8 öncesi 27: 26 satır boyu 52↔59–61 → satır belirteci 61 ile 0.) |
| 960 | 7 | `/cariler` ilk satır BASİT 81 ↔ PRO 61 (BASİT'te "Peşin Müşteri · Sistem kartı" iki satıra sarıyor) + aynı çakışma; `/fiyatlandirma` satırı 164 ↔ 215 (PRO'da fazladan sütun → sarma). Yalnız **içerik boyu**; yazı/dolgu/köşe farkı 0. |
| 375 | 15 | `/cariler`: BASİT kart yığını (`musteri-sade`, hücre dolgusu 0, satır 21) ↔ PRO yatay kayan tablo (dolgu 8, satır 61). **Yerleşim farkı** (kart ↔ tablo), yazı aynı. PRO tabloyu kart yığınına çevirmek Cariler.razor'da `data-etiket` ister (sayfa dosyası başka şeridin) — § 9. |

### 6.2 Düğme ailesi (tutarlilik-dugme, iki mod aynı)
- 1440 ve 960: `btn-birincil`/`btn-ikincil` 44·16 · `btn-kucuk` 34·14 (×73–74) · `btn-buyuk` 52·17 · sınıfsız 34·14 / 44·16. **Üç boy, üç yazı.**
- Gerekçeli sapma (v5'ten): şeritteki `btn-kucuk` 44·16 (/faturalar "Fişten Fatura Kes", /kur "Vitrin panosunu aç", /urunler/agac ×6) — yanındaki 44 px kutularla hizalı; sınıfın razor'da değişmesi DUGME-2/sayfa sahiplerine.
- 375: hepsi 44 (btn-buyuk 52). Tek istisna PRO `#islem-diger-ac` 54: iki satırlı özet ("ürün ayır · kapora · …") 351 px'e sarıyor; kural "sarılırsa büyür, kırpma yok" (§ 1c/8b ile aynı ilke), ≥44 dokunma şartını sağlıyor; BASİT'te bu öğe yok.

### 6.3 Dolu form senaryosu (`olcek-senaryo.mjs`) — `v8/sen4/`
| Durum | 1440 açık | 375 koyu |
|---|---|---|
| islemler-fis-dolu / tutar-7hane | fark 1 (`#islem-cari`), boylar 34/44/52, kesik 0, yatay 0 | fark 1, boylar B 44/52 · P 44/52/54, kesik 0 |
| cariler-cekmece | fark **0**, boylar 34/44 | fark **0**, boylar 44 |
| emanet-al-acik | fark **0**, boylar 34/44/52 | fark **0**, boylar 44/52 |

`#islem-cari` dolgu-sağ 44 ↔ 261 (375: 197): PRO'da bakiye rozeti kutunun içinde dar sütunda (`tuval-satis.css` `padding-right: calc(46% + 36px)`), BASİT'te geniş kutuda 44 px. Yüzdeye bağlı rozet payı, kutu genişliğine göre değişir; ölçü kuralı değil — GEREKÇELİ FARK. SATIS-3'e not: BASİT'te uzun müşteri adı rozetin altına girebilir (§ 9).

### 6.4 Sıkışma turu (`tur.mjs` + `analiz.py`: K1–K13, katmanlar, yatay) — `v8/tur/analiz.txt`
Rotalar /, /emanet, /cariler, /islemler, /raporlar/kar-zarar, /urunler, /denetim-izi, /fiyatlandirma × 1440/960/375 × BASİT/PRO açık; /emanet, /cariler, /islemler × 1280/375 × iki mod koyu. 60 sayfa, 24 sekme, 420 katman (280 açıldı), hata 0.
- **Yatay kaydırma 0 · K1 0 · K5 (TRY) 0 · K9 0 · K11 0 · K12 0.**
- Kalan bulgular (hepsi paket öncesinden, § A/B): GlobalArama 375'te SERBEST-YÜZEN katman (tüm rotalar; KATMAN) · /islemler açılan "Nakit · Ana Kasa" ve "+ Ayrıntı ekle" katmanı ekran kenarında (Pa960, Pa375, Ba375; SATIS-3) · /urunler K3 ad kırılması (Ba960/Pa960/Pa1440) ve K10 "Tüm kategoriler" kesik (Pa1440/960).
- **A/B (yeni bulgu 0 kanıtı):** aynı tur /islemler, /urunler, /cariler × 1440/960/375 × iki mod, olcek.css'in v8 öncesi hâliyle (`v8/ab/olcek-v7.css`) koşuldu → **aynı 9 bulgu, aynı durumlar**. Dosya v8'e döndürüldü, `cmp` AYNI. v8'in eklediği bulgu **0**.

### 6.5 Ekran görüntüleri (Read ile görüldü) — `ham/TUVAL-KARSILASTIRMA/`
- `OLCEK-1-v8_{emanet,cariler,islemler,raporlar_kar-zarar,urunler}-1440.png` (solda BASİT, sağda PRO, açılış).
- `OLCEK-1-v8-senaryo-{islemler-fis-dolu,islemler-tutar-7hane,cariler-cekmece,emanet-al-acik}-1440.png`.
- Görülen: 7 haneli tutar iki modda tam; çekmece kutuları, Vazgeç/Kaydet ve kapat iki modda aynı boy; sil düğmeleri 44 kare.
- 375 koyu ve 1280 koyu kareler: `/private/tmp/kuyumcu-serit/OLCEK-1/v8/foto/` (`yan-375-karanlik_*.png` görüldü: iki modda kutular 44, taşma yok).

### v8 · 7. Denetim (`dotnet/denetim.sh`, son mutasyondan sonra)
- **rc=0, 291 ✓, "DENETİM TEMİZ."**
- Sayaçlar: `OLCEK-1: MOD_OLCU_BILDIRIMI 0` · `OLCEK-1: YUZ_ONEMLI_OLCU 0` · `P-39: DUGME_YUKSEKLIK_CESIDI 2` · `P-39: KONTROL_YUKSEKLIK_SAPMASI 0` · `P-18: SABIT_RENK 0`.
- Aradaki koşumda (12:30) `P-38: PASIF_KAYIT_SECILEBILIR 1` kırmızıydı (SATIS-3'ün `Islemler.Arama.cs:28` yorum satırı); son koşumda şerit düzeltmiş, TEMİZ.

### v8 · 8. DoD sözleşmesi
Yeni varlık, kayıt türü, uç, form ya da düğme yok (yalnız CSS, test). PATCH/DELETE/[RolGerekli]/denetim kaydı maddeleri uygulanamaz; ModalPencere sayacına dokunulmadı; sayfa dosyasına dokunulmadı.

### v8 · 9. Açık kalanlar (sahibiyle)
1. **Kurucu kararı — kenar menüsü PRO yüzü** (KATMAN/T-K2): beyaz listede.
2. **KATMAN:** PRO satır eylemlerinde emoji (💵 ✏️ 📦) — Ikon bileşeni olmalı. GlobalArama 375'te serbest yüzen katman.
3. **DUGME-2 / sayfa sahipleri:** şerit `btn-kucuk` → `btn` (razor, 8 görünüm).
4. **SATIS-3:** `#islem-cari` rozet payı iki yüzde farklı yöntem (BASİT 44 px sabit, PRO %46); BASİT'te uzun adın rozetle çakışması denenmeli. /islemler açılan ödeme/ayrıntı katmanlarının ekran kenarı. tuval-satis.css sabit px'leri. Ağaçta `Pages/Islemler.Sade.cs.tmp`.
5. **Cariler sahibi:** 375'te PRO müşteri tablosu yatay kayıyor, BASİT kart yığını; PRO'yu da kart yığını yapmak `data-etiket` ister. `#duz-notlar` satır sayısı moda göre (Sade 1 · PRO 2) — CSS en az iki satırla eşitledi.
6. **ORTAK-1:** `/kasalar .stat-deger` kutuya sığdırma (bilgi). /urunler K3/K10 bulguları (paket öncesinden).
7. **ZO-ISLEM-A:** tam süitte tek kırmızı `ZOISLEMAEkranTestleri.ZO28`.
8. **Orkestratör:** inceleme yayını (`~/.kuyumcu-yayin`) mutasyon satırlı belirtecler.css taşıyor (§ 0) → yeniden yayın. `/private/tmp/kuyumcu-serit/` altında test kökü için bıraktığım sembolik bağlar (`deploy`, `dokumanlar`, `docs`, `OLCEK-1/*`) yalnız ölçüm içindir; silme kurucuya.

### v8 · 10. Temizlik
- API 5186 (PID 52798) ve vekil 5187 `kill` ile durduruldu; `lsof` 5186/5187 **boş**. 5198/5199/5200'e dokunulmadı.
- Kiracılar `PAKET-OLCEK-1G-TEST` (bu tur) ve `PAKET-OLCEK-1F-TEST` (01M3K1SB8MEM6HC0C29CW4RF00, önceki kesilen koşumdan kalan) `kiraci-temizle.sh --uygula` ile silindi. tenant_id kolonlu **70 tablo: 0 satır**; `PAKET-OLCEK%` = 0. Kiracı sayısı: başlangıç 20 (1F dahil) → ara 21 (başka şerit ekledi) → son **19**. QA-TEST-*/DEMO-* ve diğer şerit kiracılarına dokunulmadı.
- Fikstür ve sistem anahtarı dosyaları `printf '' >` ile boşaltıldı (0 bayt). Proje ağacına .bak/.tmp bırakılmadı; tüm geçici CSS değişimleri `cmp` ile geri doğrulandı.

### v8 · 11. Kurucuya not
Basit ve Profesyonel'de artık aynı ekran aynı ölçüde: satış ekranında ürün eklenip ödeme bölündüğünde de, müşteri düzenleme penceresinde de, Emanet Al açıkken de kutular ve düğmeler iki modda aynı boyda (44; küçük satır düğmeleri 34; "Kaydet" 52). 7 haneli tutar artık kesilmiyor. Tablo satırları iki modda 61 px (tasarım taslağındaki 62'ye yakın); önceden Basit'te 52, Profesyonel'de 61 idi.

Kalan tek görünür farklar tasarım gereği: telefonda müşteri listesi Basit'te kart, Profesyonel'de tablo; Profesyonel satışta müşteri kutusunun içinde bakiye rozetine daha çok yer ayrılıyor.

Karar sizde olan iki yer:
- **Kenar menüsü:** Profesyonel'de sık ve ikonsuz.
- **Tablo satır aralığı:** "sık/rahat" tercihine göre satır boyu değişir; hücre iç boşluğu sabit 8 px.
