# PAKET DUGME-2 — Uygulama raporu (29.09.2026, şerit t8 · API 5186 · vekil 5187 · CDP 9955–9964)

> **TUR 3 (29.09 ~16:20–17:45).** Tasarım hakeminin YÜKSEK bulgusu kapatıldı: yapışık (sticky) eylem sütunu veriyi örtüyordu. Tur 3 bölümleri aşağıda. Tur 2 ve Tur 1 raporları değişmeden en altta.
> **Durum: BİTTİ.** Şerit kapısının 7 maddesi sağlandı. Tam süitte 3 kırmızı var; üçü de başka şeritlerin (SATIS-3, ZO-BELGE) ve hiçbiri DUGME-2 dosyası okumuyor (§ U5).

## U0. Önceki doğrulamadan kalan bulgu

| Bulgu | Durum | Kanıt |
|---|---|---|
| [YÜKSEK] dugme.css § (4) yapışık sütun 960/1024'te veriyi örtüyor (/urunler PRO 25/37, /vitrin 25 gizlenen metin; tur.mjs yeni K9) | KAPANDI | Yapışık sütun **tamamen kaldırıldı** (dugme.css'te `position: sticky` 0; D2_10 ve denetim bunu arar). Yerine OLCEK-1 § 13e kalıbı: eylem hücreleri veri satırının ALTINA, SOLA iner, veri sütunları kaba sığmazsa tablo kendi kabında kayar (kayan kısımda düğme yok). Ölçüm (başsız Chrome, 40 rota × BASİT/PRO × aydınlık/karanlık, `binen.js` = hakemin binen.mjs ölçütü): **gizlenen metin 0** her genişlikte (1440/1280/1024/960/375). tur.mjs K9: ÖNCE (tur 2 CSS, aynı sekme A/B) 11 bulgu, 8'i K9 → SONRA 3 bulgu, **K9 0, K10 yeni 0** (§ U3). |

## U1. Tur 3'te yapılanlar
- **css/dugme.css § (4)** yeniden yazıldı (sayfa dosyasına dokunulmadı):
  - **(4a) /vitrin `#vitrin-ayarlar`, 769–1439:** tablo 6 sütunlu ızgara. Düzenle hücresi alt satırda 1. sütunda, Varsayılana dön/"Varsayılan" 2.'den sona. İki eylem başlığı yalnız ekran okuyucu içindir (metin DOM'da). Sütun arası .75rem: sayfanın `.tablo` hücre dolgusu 1 px, "Sıra" ile "Alış marjı" yapışıktı (hakemin zoom-vitrin-1024 notu). Kalem adı kırılmaz (ORTAK-1 (A) karşılığı). Sonuç: /vitrin K3 kırık 1280'de **88 → 0**, 1024/960'ta **180 → 4**.
  - **(4b) /urunler PRO, 769–1099 (10 başlıklı yapı):** STOK "N adet" düğmesi (#u-stok-adet-*) eylem satırına iner: "3 adet · Stok · Düzenle · ⋯". `grid-auto-flow: row dense` sayesinde DURUM veri satırındaki boş yuvaya döner. `--eylem-alti-sutun: 8` (10 − eylem − STOK). STOK başlığı yalnız ekran okuyucu içindir. BASİT'in 7 sütunu kaba sığdığı için düzeni değişmez. Mod seçicisi yok.
  - **(4c) /fiyatlandirma `#fl-tablo`, `#km-tablo`, /kullanicilar (`.satir-eylemler.saga-yasla`), 769–1099:** yapışık kaldırılınca ölçülen yeni kırpıklar (#fl-durum-* sağ 1003 > kap 999 @1024; Kullanıcılar "Düzenle/Şifre Değiştir" sağ 987 > 935 @960). Aynı kalıp. Kısa veri hücreleri kırılmaz (K3 /kullanicilar 960: 12 → 8, tur 2 ile aynı). Varsayılan satır vurgusu hücrelere iç gölgeyle taşınır.
- **DugmeIkiTestleri.D2_10** yeniden yazıldı: `D2_10_dar_pencerede_EYLEM_satirin_altinda_kesilmez_ve_VERI_ORTULMEZ`. Sticky yok; vitrin aralığı 1280 ≤ üst < 1440; ızgara, yerleşim ve nowrap bildirimleri; urunler dense/8/STOK 1/2; 4c ızgara ve sütun sayısı.
- **denetim.sh DUGME-2 bloğu:** `DUGME2_KURAL_EKSIK` artık şu durumlarda artar: sticky varsa, vitrin ızgarası 1280'i kapsamıyorsa, STOK düğmesi eylem satırına inmiyorsa, 4c kuralı yoksa. Tur 2 CSS'i ile 4, güncel CSS ile 0.
- app.css, belirtecler.css, index.html, Secim.razor, Sözlükler ve razor sayfaları **değişmedi**.

## U2. Sapmalar ve gerekçeler
1. **Hakemin iki önerisinden hiçbiri birebir uygulanmadı.** (a) "Tablo kabı kaysın, eylem yapışmasın" tek başına uygulansa düğmeler yine ekran dışında kalıyordu. dugme-olc `kirpik` ölçütü r.right > W, ölçüm scan'inde `/vitrin` "Düzenle" 1250 > 1024. (b) "Gölge ve dolgu" yapışık sütunun altındaki metni yine örtüyordu. Seçilen üçüncü yol aynı şeridin OLCEK-1 § 13e kalıbı: eylemler satır altına. Böylece hem kırpık 0 hem gizlenen 0 oldu.
2. **Veri sütunları yine kayar.** /urunler PRO 1024'te 728 > 706, /vitrin 1024'te ~1000 > 658. Tablo kendi kabında yatay kayıyor, sayfada yatay kaydırma 0. Kayan bölümde düğme yok. ORTAK-1 (A) kuralı (başlık tek satır, sütun gizleme yasak) bu yüzden bozulmadı.
3. **/kullanicilar "Dugme Iki (siz)" K3 (tur.mjs, 4 durum): ölçüm yuvarlama artığı.** Ad (14 px) ve "(siz)" (12 px) aynı satırda. Üst kenarlar 292,5 ve 294,5 px, `Math.round(top/4)` → 73 ve 74 → "2 satır" sayılıyor. dugme.css kapalıyken 307,3 ve 309,3 → 77 ve 77. Görsel tek satır, hücre `white-space: nowrap`, yüksekliği 61 px (diğer satırlarla aynı). Ham ölçüm `tur3/k2.js` çıktısında.
4. **Tuvalle bölge farkı:** Tuvalde /vitrin marj tablosu ve /urunler PRO yok (1440 tuval boyutunda kurallar devreye girmez). 1440 görünümü değişmedi: 1440'ta ölçümler 0, tutarlılık farkı 11.

## U3. Gerçek tarayıcı ölçümü (başsız Chrome + CDP; tarayıcı bölmesi KULLANILMADI)
Araç `tur3/olc.mjs`: tur 2 aracına hakemin binen ölçütü (`binen.js`) eklendi. Kaynak dosyalar: `out/dar.json`, `dar2.json`, `genis.json`, `tel.json`.

| Genişlik | ölçüm | ortasız | silik | zayıf<3 | kırpık | native select | ok yok | binen (gizlenen) | K1 | K5 | K9 | Y kaydırma |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | 160 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 16* | 0 |
| 1280 | 160 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 16* | 0 |
| 1024 | 160 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 29* | 0 |
| 960 | 160 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 | 160 | 0 | 0 | 0 | 370** | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

\* K9'un tamamı /denetim-izi ve /raporlar/islem-defteri'de (ZO-BELGE sayfalaması). Tur 2'de de vardı; A/B aynı.
\** Telefon: tablo kabı içindeki satır eylemleri (/urunler 192, /vitrin 100, /cariler 56, /fiyatlandirma 8, /kullanicilar 8, /bildirimler 6). Tur 2'de 418'di. § 4 telefonu kapsamaz (≥ 769). Sayfada yatay kaydırma 0. Gerekçeli açık, § U7.
4c sonrası yeniden ölçüm (`dar2`, 5 rota × 1024/960 × 2 mod × 2 tema): tüm sütunlar 0, K3 kırık vitrin 4 ve kullanıcılar 8 (tur 2: 180 ve 8).
**tur.mjs K1..K13** (/urunler, /vitrin, /fiyatlandirma, /kullanicilar × 1024/960 × BASİT/PRO, karanlık):
- ÖNCE (tur 2 CSS, A/B): 11 bulgu. K9: fl th⟂EYLEMLER, kullanıcılar tarih/rozet⟂Düzenle, urunler "13,9232 has"⟂#u-stok-adet, vitrin "0 ₺"⟂#vitrin-duzenle.
- SONRA: 3 bulgu, **K9 0**. Kalan üç bulgu: urunler K10 "Tüm kategoriler" (ÖNCE'de vardı); vitrin K3 "Kur kaynağı ayarları →" (ÖNCE'de vardı); kullanıcılar K3 ölçüm artığı (§ U2-3).
- Kaynak: `tur3/tur`, `tur3/tur-once`.

**tutarlilik.mjs 1440 (40 rota):** BASİT↔PRO farkı **11** (tur 2'de 13). Artış yok. Farkların tamamı /cariler ve /raporlar/islem-defteri'de, veri ve tarihe bağlı.
**Görüntüler (Read ile bakıldı), `ham/TUVAL-KARSILASTIRMA/`:**
- `DUGME-2-t3-urunler-1024-Profesyonel-karanlik-once-sonra.png`: ÖNCE "13,9232" ve "3 adet" yapışık sütunda kesik. SONRA has değerleri tam, "3 adet" eylem satırında.
- `DUGME-2-t3-vitrin-1024-Profesyonel-…` ve `DUGME-2-t3-vitrin-1280-Basit-…`: ÖNCE kalem adı iki satır, "Yuvarlama adım" kesik. SONRA tüm başlıklar ve "10 ₺" görünür, Düzenle/Varsayılan satır altında.
- `DUGME-2-t3-fiyatlandirma-960-Profesyonel-…` ve `DUGME-2-t3-kullanicilar-960-Basit-…`.
Aydınlık tema ölçümle kapsandı (tablo satırı aydınlık × 2 mod). Görüntüler karanlıktır.

## U4. Mutasyon kanıtı (yedek `/private/tmp/kuyumcu-serit/mut-yedek/DUGME-2-t3/`, her biri `cmp` ile birebir geri alındı)
| Kod | Değişiklik | Kırmızı test | denetim (sıra/renk/eksik) | Canlı |
|---|---|---|---|---|
| M1-1 | index.html'den dugme.css bağlantısı silindi | D2_01 | 1/0/0 ✗ | 7 rota × 1024 PRO: kırpık 0 → **39**, silik 0 → 4, native select 0 → 1, K3 kırık → 23 |
| M3-1 | vitrin `display: grid` → `block` | D2_10 | 0/0/1 ✗ | /vitrin 1024 PRO kırpık 0 → **24** |
| M3-2 | STOK hücresi `grid-column: 1 / 2` → `auto` | D2_10 | 0/0/1 ✗ | /urunler 1024 PRO kırpık 0 → **12** |
| M3-3 | Kalem hücresine `position: sticky; left: 0` eklendi | D2_10 | 0/0/1 ✗ | /vitrin 1024 PRO binen (gizlenen) 0 → **22** |
| M3-4 | 4c eylem hücresi `grid-column: 1 / -1` → `auto` | D2_10 | 0/0/1 ✗ | /kullanicilar, /fiyatlandirma 960: kırpık 0 kaldı. Eylemler dar 1. sütuna sıkıştı ama ekran dışına düşmedi. Kural test ve denetimle korunuyor. |

Son mutasyondan sonra dugme.css `cmp` ile AYNI (`tur3/dugme.css.sonra`). Temiz `dotnet build Kuyumcu.sln`: **0 uyarı, 0 hata**.

## U5. Testler ve denetim
| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| DugmeIkiTestleri + OlcekTekTestleri (son derlemeden sonra) | 43 | 43 | 0 |
| TAM süit (trx: `/private/tmp/kuyumcu-serit/trx-pDUGME-2-t3/`) | 7256 | 7253 | 3 |

Kırmızılar (hiçbiri dugme.css, olcek.css, belirtecler.css ya da denetim.sh okumuyor):
- `SATIS3EkranTestleri.T19_tek_kalem_esik_ALTI_ama_musterinin_gunluk_KUMULATIFI_ustu_alanlar_ONCEDEN_acilir` ve `…T19_kumulatif_formu_doldurulup_KAYDEDILINCE_bos_fiste_MASAK_karti_KALKAR`: **SATIS-3**. Dosya 16:20'de değişti.
- `Dogrulayici.ZOBELGEAramaSinirTestleri.DZB_alis_fisi_negatif_toplam_isareti_kaybolmaz`: **ZO-BELGE**. Dosya 16:24'te değişti.
Tur 2'nin kırmızıları (O8, ZB57, ZB57b) artık yeşil. Fark kümesinin hiçbir üyesi bu paketin dosyasına dayanmıyor. Bu paket yeni kırmızı üretmedi.
`bash denetim.sh` → **DENETİM TEMİZ** (✓ DUGME2_BAGLANTI_SIRASI 0 · ✓ DUGME2_SABIT_RENK 0 · ✓ DUGME2_KURAL_EKSIK 0).

## U6. Canlı doğrulama ve temizlik
- API 5186'da çalıştı (Development, `--no-build`; sistem anahtarı yalnız süreç ortamında, çıktıya yazılmadı). Vekil 5187'de. CDP portları 9955–9964, profiller `/private/tmp/kuyumcu-serit/DUGME-2/tur3/profil-*`.
- Kiracı `PAKETDUGME-2-TEST Tur3 Kuyumculuk` (01M3PN4MAWTR4FXY1NZT6PZG8K). Davet 201, kayıt 201, giriş 200, /kur/guncel 200. demo-tohumla ile 6 cari, 12 ürün, 9 satış. `POST /cariler` 404 döndü (tur 2'deki gibi, uç adı farklı). demo-tohumla ilk denemede kullanıcı yokken çağrıldı ve 401 aldı; kiracı kurulduktan sonra 0 ile bitti.
- Paket şema, uç ya da iş kuralı değiştirmiyor. Canlı kanıt § U3'teki tarayıcı ölçümüdür. Migration: **gerekmedi**.
- Temizlik: API, vekil ve kendi Chrome süreçleri durduruldu. 5186 ve 5187'de dinleyen yok, kalan Chrome 0. `kiraci-temizle.sh --uygula` rc=0. SQL: `tenants where id=…` **0**, `kullanicilar where tenant_id=…` **0**.
- tenants: başlangıçta **14**, sonda **13**. Bu tur bir kiracı açıp aynısını sildi (net 0). −1 başka şeridin silmesi. QA-TEST-* ve 5199/5200'e dokunulmadı.
- Not: İlk tarama sırasında iki ölçüm süreci aynı kullanıcıyla farklı modda koştu (BENIOKU bunu yasaklar). O tarama (`tasan-*.log`) yalnız keşif için kullanıldı. Resmî ölçümlerin hepsi tek süreçte, sıralı koşuldu.

## U7. Açık kalanlar (sahibiyle)
- **Telefon satır eylemleri (375)**: tur 2 § T7 ile aynı (kırpık 418 → 370). Kart düzeni sayfa dosyası işi. Sahipler: Cariler/Urunler atanmamış, Vitrin KATMAN'ın. Orkestratör sahip atamalı.
- **ZO-BELGE:** DZB kırmızı. /denetim-izi ve /raporlar/islem-defteri K9 sayfalaması (önceden vardı).
- **SATIS-3:** T19 ×2 kırmızı.
- **KATMAN (Vitrin.razor):** "Kur kaynağı ayarları →" K3 (önceden vardı). `.tablo` hücre dolgusu 1 px; bu paket yalnız 769–1439 ızgarasında boşluk verdi.
- **/urunler K10** "Tüm kategoriler" seçici değeri kesik (önceden vardı; sahibi atanmamış).
- **Ölçüm aracı:** sikisma-olc `satir()` farklı yazı boylu tek satırı `round(top/4)` kovasında 2 satır sayabiliyor (§ U2-3). Taban çizgisine ya da `getClientRects` dikey örtüşmesine dayalı sayım önerilir.
- `dotnet/denetim.sh.mutbak` (0 bayt, 18.09 16:49) proje ağacında. Bu paketin değil; silinmesi kurucu onayıyla.

## U8. Değişen dosyalar (bu tur)
`dotnet/Kuyumcu.Web/wwwroot/css/dugme.css` · `dotnet/Kuyumcu.Tests/DugmeIkiTestleri.cs` · `dotnet/denetim.sh` (yalnız DUGME-2 bloğu). `git status --porcelain` (bin/obj hariç, şeritle ilgili): `M dotnet/Kuyumcu.Web/wwwroot/index.html` (tur 1) · `?? dotnet/Kuyumcu.Web/wwwroot/css/dugme.css` · `?? dotnet/Kuyumcu.Tests/` · `?? dotnet/denetim.sh`.

## U9. Koşul karşılama (tur 3)
| Koşul | Durum |
|---|---|
| binen.mjs gizlenen metin 0 | ✓ 0 (1440/1280/1024/960/375) |
| tur.mjs K9/K10 yeni bulgu 0 | ✓ K9 0; K10 yalnız önceden var olan |
| dugme-olc ORTASIZ 0 · native 0 · SİLİK 0 · zayıf<3 0 · kırpık 0 (1440/1280/1024/960) | ✓ |
| tutarlilik BASİT↔PRO farkı artmaz | ✓ 13 → 11 |
| Sayfa dosyası, app.css değişmez | ✓ |
| Mutasyon → adı yazılı test kırmızı → cmp | ✓ 5/5 |
| COMMIT yok | ✓ |

## U10. Kurucuya
Mevzuat içeriği yok. Göz onayı için `ham/TUVAL-KARSILASTIRMA/DUGME-2-t3-*-once-sonra.png`. Dar pencerede (769–1439 /vitrin, 769–1099 diğerleri) satır düğmeleri artık satırın altında, solda. Hiçbir değer düğmenin altında kalmıyor.

---
# TUR 2 (değişmeden korunan önceki rapor)
# PAKET DUGME-2 — Uygulama raporu (29.09.2026, şerit t8 · API 5186 · vekil 5187 · CDP 9955–9970)

> **TUR 2 (29.09 ~13:55–15:10).** Hasım doğrulayıcının 4 bulgusu kapatıldı ve ölçüm yeniden koşuldu. Tur 2 bölümleri hemen aşağıda. Tur 1 raporu, değişmeden, en altta "TUR 1" başlığı altında duruyor.
> **Durum: BİTTİ.** Şerit kapısının 7 maddesi sağlandı. Tam süitte 3 kırmızı var, üçü de başka şeritlerin. Ayrıntı § T5.

## T0. Önceki doğrulamadan kalan bulgular

| Bulgu | Durum | Kanıt |
|---|---|---|
| [KRİTİK] /vitrin satır düğmeleri 1280'de tablo kenarında kesikti (yapışık sütun kuralı 1279'da bitiyordu) | KAPANDI | `dugme.css` § (4) aralığı `769–1279` yerine artık **`769–1439`**. Üst sınır, taşmanın bittiği genişliğe göre seçildi: 1280'de scrollW 1086 > clientW 914 (taşıyor), 1440'ta sığıyor. 1440 tuval görünümüne dokunulmadı. D2_10 sabit "1279" yerine "üst sınır ≥ 1280 ve < 1440" diye güncellendi. Ölçüm (doğrulayıcının `olc.mjs`'i, kap içi kırpık dahil), /vitrin 1280 × BASİT/PRO × aydınlık/karanlık: kırpık **25 → 0** (M2-1). 40 rota × 1280: kırpık **0**. 1366'da da görüntü alındı. Görüntü: `ham/TUVAL-KARSILASTIRMA/DUGME-2-Vitrin-karanlik-1280-once-sonra-kaydir.png` (ÖNCE: "Düzenle" kesik, "Varsayılana dön" yok · SONRA: ikisi de görünür · kaydırılmış: veri sütunları eksiksiz) |
| [ORTA] /ekranlar favori yıldızları (38 düğme) siliktir ve rota ölçüm dışındaydı | KAPANDI | Yıldız artık dinlenirken de görünür: `opacity:1`, `1px solid var(--dugme-kenar)` kenar, ince çizgi yıldız `var(--sekme-metin)`. Birincil karoda (Satış) kenar `--vurgu-metin`, zemin `--vurgu`. /ekranlar 40 rotalık ölçüme eklendi. Silik: **38 → 0** her genişlik × mod × temada (1440/1280/1024/960/375). Karo yerleşimi tuvaldeki gibi kaldı: iç boşluk 14 px. Bunun yerine yıldız köşeye 1 px yaklaştı. İlk denemede iç boşluk 36 px yapılmıştı, 1440'ta 6 adı kırdığı için geri alındı (§ T2). |
| [DÜŞÜK] 375 px /cariler PRO satır düğmeleri ekran dışında kalıyor | GEREKÇELİ AÇIK | Telefonda yapışık sütun bilerek yok: /cariler PRO eylem hücresi (Tahsilat · Düzenle · ⋯) 375'te kabın yarısından fazlasını kaplardı. Satır eylemlerine tablo kabı yatay kaydırılarak ulaşılıyor. Sayfa bunu "Tablo yatay kaydırılabilir — parmağınızla sağa/sola kaydırın" yazısıyla söylüyor. Sayfada yatay kaydırma 0. Görüntü: `ham/TUVAL-KARSILASTIRMA/DUGME-2-Cariler-375-PRO-kaydir.png`. Kalıcı çözüm telefonda kart düzenidir; bu sayfa dosyası işidir (§ T7). |
| [DÜŞÜK] Proje ağacında tarayıcı profili ve ölçüm çıktısı var (`betikler/tur/profil-dugme` 117 MB, `dugme-olc.json/.txt`) | SAHİBİ BU PAKET DEĞİL, kanıt aşağıda | Profilin doğum zamanı **11:03:21**. Tur 1 uygulayıcısının ilk dosyası `/private/tmp/kuyumcu-serit/DUGME-2/tenants0.txt` **11:43:01**. Klasör bu turda da **14:19:50–14:20:25** arasında yeniden yazıldı (`tutarlilik-1440.json`, `dugme-olc.json`). Tur 2'nin bütün koşuları `/private/tmp/kuyumcu-serit/DUGME-2/tur2/` altında, kendi `profil-*` dizinleriyle yapıldı. Hiçbir koşu `betikler/tur`'da başlatılmadı (`dugme-olc.mjs`'in kopyası r1440/r960 alt dizinlerinde koşuldu). Silme kurucu onayına bırakıldı (dosya silme YASAK). |

## T1. Tur 2'de yapılanlar
- **css/dugme.css**
  - (4) Yapışık eylem sütunu aralığı artık `(min-width: 769px) and (max-width: 1439px)`.
  - (3c) /ekranlar yıldızı: `html[data-mod] .ekranlar-sayfa .ekran-karo-kap > .ekran-yildiz`. Bu sayfa `.ana-icerik` dışında (`uygulama-tam-ekran`); ilk seçici bu yüzden eşleşmedi, tarayıcıda ölçülerek bulundu. Birincil karo yıldızı ve üzerine gelme durumu da eklendi. Fareli masaüstünde (≥ 769) `top/right: 1px` kuralı var.
  - (3c) Vurgulu satırdaki ikincil düğme (`tr.satir-vurgu`): tur 2 ölçümünde /fiyatlandirma karanlıkta `#fl-duzenle-*` ve `#fl-goster-*` **2,97:1** çıktı (zayıf < 3). Kenar `--dugme-kenar-vurgulu` belirtecine geçti. Sonuç 0.
- **belirtecler.css:** `--dugme-kenar-vurgulu: var(--vurgu)`, :root ve önizleme blokları. Sabit değer yok.
- **DugmeIkiTestleri.cs:** D2_10 güncellendi (üst sınır 1280 ile 1439 arası). **D2_11 YENİ** (yıldız opacity 1, kenar, renk; karo iç boşluğu değişmez; köşe konumu). D2_09'a vurgulu satır kuralı eklendi. D2_07 kontrast tablosuna `--dugme-kenar-vurgulu / --vurgu-yuzey ≥ 3` çifti eklendi, D2_08 listesine de. Toplam **11 test**.
- **denetim.sh** DUGME-2 bloğu: `DUGME2_KURAL_EKSIK` artık iki koşulu daha sayıyor: yapışık aralık 1280'i kapsamıyor, /ekranlar yıldızı dinlenirken gizli.
- Sayfa (razor) dosyası, app.css, Sozlukler ve Secim.razor **değişmedi**.

## T2. Sapmalar ve gerekçeler (tur 2)
1. **Yapışık sütunun örttüğü veri (K9).** sikisma-olc K9, yapışık sütunun altında kalan hücreleri "binen" diye sayıyor: /vitrin 1280'de 6, 1024'te 6. Bu yüzden aynı ölçüme **K9g** eklendi: çakışmanın ortasında `elementFromPoint`, yapışık hücreyi döndürüyorsa yazı örtülmüş sayılır, görünür binme değildir. Sonuç: /vitrin 1280'de 69/69, 1024'te 47/47 örtülü, **görünür binme 0**. /urunler 1024'te 13/13 örtülü. A/B ölçümü (aynı sekmede dugme.css açık/kapalı): görünür binme iki durumda aynı. Kaydırınca bütün sütunlar görünüyor (görüntü: `…-kaydir`). 1366'da yuvarlama sütunu ("10 ₺") yapışık sütunun altına kısmen giriyor. Bu da "Excel dondurulmuş sütun" davranışı: ayraç çizgisi var, kaydırınca açılıyor.
2. **/ekranlar karo iç boşluğu.** Önce 36 px ayrıldı. 1440'ta 6 kısa ad iki satıra kırıldı (K3). Tuvaldeki 14 px korundu, yıldız köşeye 1 px yaklaştırıldı. Ad satır kutusuyla yıldız arasındaki dikey kesişim en fazla 3,2 px. Bu satır aralığı payı; harf çizgisine değmiyor ve K9 eşiği olan 4 px'in altında. 1440/1280/1024/900 × tüm karolar ölçüldü. 1024'teki K3 kırık 4, dugme.css kapalıyken de 4 (A/B), yani önceden vardı.
3. **Tuvalle fark (/ekranlar).** Tuvalde yıldız çizili değil, yalnız "yıldızla favorine ekle" ipucu var. Bizde yıldız hep görünüyor. GEREKÇELİ FARK: KABUL maddesi "silik eylem düğmesi 0". Görünmez düğme keşfedilemez; dokunmatikte zaten hep görünüyordu. Karo yerleşimi, sırası, metinleri ve ölçüleri tuvalle aynı: `DUGME-2-Ekranlar-tuval-once-sonra-1440.png`.

## T3. Gerçek tarayıcı ölçümü (başsız Chrome, CDP; tarayıcı bölmesi KULLANILMADI)
Araç: doğrulayıcının `olc.mjs`'i (dugme-olc ölçütleri, sıkı ±1,5 px orta, zayıf < 3:1, kap içi kırpık, native select, ok ikonu ve sikisma-olc K1–K12) + K9g. Önbellek kapalı, kopya: `/private/tmp/kuyumcu-serit/DUGME-2/tur2/olc.mjs`. **40 rota** (/ekranlar dahil) × BASİT/PRO × aydınlık/karanlık. Ham veri: `tur2/out/son-zorunlu.json` ve `son-genis.json`.

| Genişlik | ölçüm | ortasız | silik | zayıf<3 | kırpık | native select | ok yok | K1 | K3 taşan | K5 TRY | K9 görünür | Y kaydırma |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | 160 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 17 (önceden vardı*) | 0 |
| 1280 | 160 | 0 | 6** → 0 | 0 | **0** | 0 | 0 | 0 | 0 | 0 | 12 (önceden vardı*) | 0 |
| 1024 | 160 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 32 (önceden vardı*) | 0 |
| 960 | 160 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 | 160 | 0 | 6** → 0 | 0 | 418 (telefon, tablo kabı içinde kaydırılır; § T0/T7) | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

\* K9 görünür binmenin tamamı /denetim-izi ve /raporlar/islem-defteri'de ("Önceki/Sonraki" sayfalama ile tablo hücresi). A/B aynı sekmede ölçüldü, dugme.css açık/kapalı: 1024'te 7=7, 1280'de 3=3, 1440'ta 3=3 ve 1=1. Önceden vardı, sayfalar ZO-BELGE'nin.
\** /islemler BASİT `#islem-tur-*` karoları. Bu, SATIS-3'ün 14:42'de değiştirdiği tuval-satis.css'in ara hâlinden geldi. Güncel CSS ile /islemler 1280 ve 375 × 2 mod × 2 tema yeniden ölçüldü: silik 0 (`out/isl.json`, `isl375.json`).
Resmî araç `dugme-olc.mjs` (hakem kopyası, 40 rota): 1440 → ortasız 0 · silik 4 (yalnız /ekranlar birincil yıldız; düzeltildi, sonra 0) · kırpık 0. 1024 → 0/0/0. 960 → 0/0/0.
**K3 kırık:** A/B'de her rota aynı (/vitrin 22–23, /ayarlar 3–4, /bildirimler 2, /ekranlar 1024'te 4, /kullanicilar 1, /yetkiler 1). dugme.css yeni kırık üretmiyor.
**tutarlilik.mjs** (1440, 40 rota): BASİT↔PRO farkı **13**. Fark listesi tur 1 SONRA listesiyle, tarihler ayıklandıktan sonra `diff` ile AYNI. Artış yok.
**Ekran görüntüleri (Read ile bakıldı)**, `ham/TUVAL-KARSILASTIRMA/`:
- `DUGME-2-Vitrin-{karanlik-1280-once-sonra-kaydir,aydinlik-1280-once-sonra}.png`
- `DUGME-2-Ekranlar-tuval-once-sonra-1440.png` ve `DUGME-2-Ekranlar-karanlik-1440-375.png`
- `DUGME-2-Fiyatlandirma-karanlik-1440-once-sonra.png`
- `DUGME-2-Cariler-375-PRO-kaydir.png`
Tur 1 görüntüleri (Faturalar, Evraklar, VadeTakibi, Siparisler, Ayarlar, Panel; açık ve koyu) geçerli. Bu sayfalarda tur 2 kuralı devreye girmiyor.

**Bölge bölge fark (/ekranlar, tuval ile SONRA 1440):** başlık bandı AYNI · grup başlıkları AYNI · karo ızgarası, ölçü ve metin AYNI · karo iç boşluğu 14 px AYNI · "Sipariş ve tamir" tuvalde 2 satır, bizde 1 satır (önceden vardı, karo genişliği farkı, ÖNCE'de de aynı) · favori yıldızı tuvalde yok, bizde köşede ince çerçeveli: GEREKÇELİ FARK (§ T2-3).

## T4. Mutasyon kanıtı (tur 2; yedek `/private/tmp/kuyumcu-serit/mut-yedek/DUGME-2-t2/`, her biri `cmp` ile birebir geri alındı)
| Kod | Değişiklik | Kırmızı test | denetim.sh | Canlı |
|---|---|---|---|---|
| M1-1 (tekrar) | index.html'den dugme.css bağlantısı kaldırıldı | D2_01 | DUGME2_BAGLANTI_SIRASI=1 ✗ | 12 rota × 1280/1024/375: silik geri geldi (/ekranlar 152, /faturalar 8, /cariler 14…), native select 4 (/siparisler), /vitrin 1280 kırpık 100, /urunler 1024 kırpık 24 (`out/mut1.json`) |
| M2-1 | yapışık aralık 1439 → 1279 | D2_10 | KURAL_EKSIK=1 ✗ | /vitrin 1280 kırpık 0 → **25** (4 kombinasyonun her birinde) |
| M2-2 | yıldız `opacity: 1` silindi | D2_11 | KURAL_EKSIK=1 ✗ | Görünmez yıldız 0/38 → **38/38**. olc.mjs opacity'yi ölçmediği için bu durumda silik 0 gösterdi; not olarak düşüldü |
| M2-3 | vurgulu satır kenarı → `--dugme-kenar` | D2_09 | — | /fiyatlandirma 1440 karanlık zayıf 0 → **2** |
| M2-4 | `--dugme-kenar-vurgulu` → `var(--kenarlik)` | D2_07 (ör. KARANLIK-MEDYA 2,97 < 3; GRAFİT 2,85) | — | — |

Son mutasyondan sonra dugme.css, belirtecler.css, index.html ve denetim.sh `cmp` ile AYNI. Temiz `dotnet build Kuyumcu.sln`: **0 hata, 0 uyarı**.

## T5. Testler ve denetim (tur 2)
| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| DugmeIkiTestleri + OlcekTekTestleri | 43 | 43 | 0 |
| TAM süit (`--no-build`, trx: `/private/tmp/kuyumcu-serit/trx-pDUGME-2-t2/`) | 7198 | 7195 | 3 |

Kırmızılar hiçbir DUGME-2 dosyasını okumuyor, üçü de başka şeridin:
- `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu`: ORTAK-1. Tur 1 devir listesinde vardı.
- `ZoBelgeTestleri.ZB57_…` ve `ZB57b_…`: **ZO-BELGE**. DenetimIzi para çevirisi: "1500.50 TRY" beklenen "1.500,50 ₺" değil. Test dosyası 14:21'de ZO-BELGE tarafından değişti, iş sürüyor. Bu pakete ait değil.

`bash denetim.sh` → **DENETİM TEMİZ.** (✓ DUGME2_BAGLANTI_SIRASI 0 · ✓ DUGME2_SABIT_RENK 0 · ✓ DUGME2_KURAL_EKSIK 0). Yeni test sayısı tur 2'de 1 (D2_11); paket toplamı 11.

## T6. Canlı doğrulama ve temizlik (tur 2)
- API 5186'da çalıştı (Development, `--no-build`; sistem anahtarı yalnız bu süreç için ortam değişkeniyle verildi, çıktıya yazılmadı). Vekil 5187'de çalıştı.
- Kiracı `PAKETDUGME-2-TEST Tur2 Kuyumculuk` (01M3PCNMVQZZ52YKDW7HKHGGA1). Kurulum: davet 201, kayıt 201, giriş 200, /kur/guncel 200. demo-tohumla.js ile 6 cari, 12 ürün, 9 satış tohumlandı. `kiraci.py`'deki `POST /cariler` 404 döndü (uç adı farklı). Tohumlama bu yüzden demo-tohumla ile yapıldı; hata gizlenmedi.
- Paket şema, uç ya da iş kuralı değiştirmiyor. Canlı kanıt, § T3'teki tarayıcı ölçümü ve görüntüleridir.
- Temizlik: API (83763/83757) ve vekil (84367) durduruldu. 5186 ve 5187'de dinleyen süreç yok. Tur 2 başsız Chrome süreçleri kapatıldı (kalan 0). `kiraci-temizle.sh --uygula` rc=0. SQL: `tenants where id=…` **0**, kullanicilar **0**, denetim_kayitlari **0**.
- tenants: başlangıçta **15**, sonda **14**. Tur 2 bir kiracı açıp aynısını sildi (net 0). Aradaki değişimler başka şeritlerin: PAKETZO-BELGE-TEST ve PAKETZO-ISLEM-A-TEST kendi sahiplerince silindi, PAKETSATIS-3-TEST Dogrulayici eklendi. QA-TEST-* ve 5199/5200'e dokunulmadı. Migration: gerekmedi.

## T7. Açık kalanlar (sahibiyle)
- **Telefon satır eylemleri (375):** /cariler PRO (56), /urunler (96+96), /vitrin (50+50), /fiyatlandirma, /kullanicilar, /bildirimler PRO, /islemler PRO. Eylemler tablo kabı kaydırılınca ulaşılıyor, sayfada yatay kaydırma 0. Kart düzenine geçiş sayfa dosyası işidir. Cariler ve Urunler hiçbir şeride atanmamış (öneri: ORTAK-1 ya da yeni "TELEFON-KART" paketi). Vitrin KATMAN'ın. Orkestratör sahip atamalı.
- **ZO-BELGE:** ZB57/ZB57b kırmızı. /denetim-izi ve /raporlar/islem-defteri'de "Önceki/Sonraki" sayfalaması tablo hücresinin üstüne biniyor (K9 görünür; önceden vardı, A/B aynı).
- **ORTAK-1:** O8 kırmızı.
- **KATMAN (Vitrin.razor):** Kalem sütununda K3 kırık 22 (önceden vardı).
- **Orkestratör:** `betikler/tur/profil-dugme` (117 MB), `dugme-olc.json/.txt` ve `tutarlilik-*.json/.txt` proje ağacında. 11:03 ve 14:19–14:20'de başka bir koşum yazdı. Silinmesi kurucu onayıyla; kaynak aracın (`betikler/tur/dugme-olc.mjs`, `tutarlilik.mjs` varsayılan `cwd` profili) `/private/tmp`'ye yönlendirilmesi önerilir.
- **Ölçüm aracı:** olc.mjs ve dugme-olc `opacity:0` düğmeyi görünür sayıyor. K9 yapışık sütunun örttüğünü binme sayıyor. K9g yöntemi (`tur2/k9g.js`) ortak araca aktarılabilir.

## T8. Kurucuya
Mevzuat içeriği yok. Göz onayı için: `ham/TUVAL-KARSILASTIRMA/DUGME-2-Vitrin-*`, `DUGME-2-Ekranlar-*`, `DUGME-2-Fiyatlandirma-*`, `DUGME-2-Cariler-375-PRO-kaydir.png`. Yıldızların hep görünmesi istenmezse tek kural (`dugme.css` § yıldız) geri alınır. Bu durumda "silik 0" kabulü /ekranlar için gerekçeli istisna olur.

---

# TUR 1 (değişmeden korunan önceki rapor)
# PAKET DUGME-2 — Uygulama raporu (29.09.2026, şerit t8 · API 5186 · vekil 5187 · CDP 9951–9964)

**Konu:** Düğme ve seçici yazısı dikey ortada, düğme zeminden ayırt edilir. Kapsam tüm ekranlar.
**Kaynak:** Kurucu 25.09 ~12:20, Faturalar (BASİT, koyu tema): "yazının biri butonun üstünde diğeri altında, butonlar zeminle aynı renkte".
**Durum:** BİTTİ. Şerit kapısının 7 maddesi sağlandı. Tam süitte kalan 1 kırmızı başka şeride ait (ORTAK-1), ayrıntı § 9'da.

## 0. Önceki doğrulamadan kalan bulgular

| Bulgu | Durum | Kanıt |
|---|---|---|
| [KRİTİK] `css/dugme.css` yoktu, index.html'de bağlantısı da yoktu | KAPANDI | `wwwroot/css/dugme.css` oluşturuldu. index.html'de olcek.css'ten SONRA, ortak.css'ten ÖNCE bağlandı. Mutasyon: bağlantı kaldırılınca ölçüm 1440'ta 0/0/0 → **8/198/0**, 1024'te 0/0/0 → **8/198/124** oluyor (§ 5) |
| [YÜKSEK] Yerleşik (native) `<select>` kalmıştı (#sip-durum-suzgec, #kz-karsilastir, Yeni sipariş Tür/Cari) | KAPANDI | `html[data-mod] select:not([multiple]):not([size])` için appearance:none, ince çizgi "V" ok ve dikey orta. Ok iki gradyanla çiziliyor, rengi `--secim-ok` belirtecinden geliyor. Ölçüm: native-select 0 |
| [YÜKSEK] 1024'te satır düğmeleri kesikti (kirpik 132) | KAPANDI | 769–1279 px aralığında eylem sütunu sağa yapışık (sticky). /vitrin'de iki eylem sütunu var, ikisi de yapışık. /urunler PRO'da STOK sütunu yapışık (769–1099). Ölçüm 1024: kirpik **124 → 0** |
| [YÜKSEK] Vade takibi 7/30/60 gün: seçili düğme görünmüyordu | KAPANDI | `[aria-pressed="true"]` ve `.dugme.secili` için dolu vurgu zemini (`--sekme-secili-zemin`) eklendi. Görüntü: `ham/TUVAL-KARSILASTIRMA/DUGME-2-VadeTakibi-*.png` ("7 gün" dolu mavi) |

## 1. Yapılanlar
- **css/dugme.css (YENİ, tek şerit dosyası).** Beş bölüm:
  - (1) Düğme ailesinde (.dugme, .btn, .sekme, .sekmeler-sekme, .musteri-segment-oge, button.ikincil) `align-items:center` ve `line-height: var(--dugme-satir)`. `display` bilerek yazılmadı: `[hidden]` düğmeler gizli kalmalı.
  - (2) Yerleşik select: appearance:none, belirteç renkli ince çizgi ok.
  - (3a) Segment ve sekme: seçili olmayan sekme kap içinde kendi ≥ 3:1 kenarını taşıyor, seçili sekme dolu. Kapsam `.ana-icerik`. Üst bardaki sayfa sekmeleri (tuval-kabuk) dokunulmadan kaldı.
  - (3b) Basılı veya seçili düğme dolu zeminle görünüyor. Kalınlık bilerek yazılmadı, gerekçe § 3.
  - (3c) Çerçevesiz eylemler ≥ 3:1 kenar alıyor: katlanır başlık, #gk-sema/#gk-secenek, "Ayrıntı"/pozisyon kırılımı/müşteri bağlantı düğmesi, karşılama araç çubuğundaki ikincil düğme, `.gk-alt-dugme.btn-ikincil`, satır içi düzenleme açıcısı `.si-tetik` (kesikli kenar dinlenirken de görünür).
  - (4) Dar pencere yapışık eylem sütunu (769–1279), /vitrin çift sütun, /urunler PRO STOK sütunu.
- **belirtecler.css:** yalnız düğme ve sekme belirteçleri eklendi (`--dugme-kenar`, `--dugme-satir`, `--sekme-kenar`, `--sekme-metin`, `--sekme-secili-zemin`, `--sekme-secili-metin`, `--secim-ok`). Hepsi tema belirtecine takma ad, sabit değer yok. 8 şema × aydınlık/karanlık kendiliğinden çalışıyor. `.tema-onizleme` ve `.gorunum-sema-onizleme` kapları için ikinci blok var.
- **index.html:** tek `<link>` satırı eklendi.
- **Kuyumcu.Tests/DugmeIkiTestleri.cs (YENİ, 10 test):** D2_01…D2_10.
- **OlcekTekTestleri.OL5 (aynı şerit):** "olcek.css sondan bir önce" koşulu şöyle esnetildi: "olcek.css ile ortak.css arasında yalnız css/dugme.css olabilir". Spesifikasyon dugme.css'i olcek'ten sonra istiyor; O1 (ortak en sonda) korunuyor.
- **denetim.sh:** yeni `# >>> DUGME-2` bloğu, üç sayaç: DUGME2_BAGLANTI_SIRASI, DUGME2_SABIT_RENK, DUGME2_KURAL_EKSIK.
- **Bilesenler/Form/Secim.razor DEĞİŞMEDİ.** Gerekçe: bu bileşen radyo grubu, native select değil. 22 native `<select>` CSS genel kuralıyla çözüldü, sayfa dosyasına dokunmaya gerek kalmadı.
- **Sayfa dosyalarına (razor) dokunulmadı.** app.css ve Sozlukler hub'ı da değişmedi.

## 2. Spesifikasyondan sapmalar ve gerekçeler
1. **"Kontrast ≥ 3:1" ölçüsü.** dugme-olc.mjs `silik` eşiği 1,6'dır. Ayrıca `color(srgb 0.24 …)` ve saydam kenarı sayı olarak ayrıştırıyor, bu yüzden aydınlıkta yanlış GEÇTİ veriyor (#gk-dukkana-don bu yüzden ancak karanlıkta görünüyordu). Bu nedenle iki ek ölçüm yapıldı:
   - (a) Aynı aracın canvas ile renk çözen kopyası `dugme-olc-d2.mjs`: 1440 ve 1024'te 0/0/0.
   - (b) `kontrast.mjs`: 16 rota × BASİT/PRO × aydınlık/karanlık, 488 denetim. Tür başına en düşük gerçek oran: eylem **3,31**, seçili olmayan sekme **3,31**, seçici **3,31**, seçili sekme **6,16**. 3:1 altında kalan: **0**.
2. **Tuval Basit/Profesyonel anahtarı ile fark.** Tuvalde seçili olmayan seçenek çerçevesiz. Bizde kap içinde kendi ≥ 3:1 çerçevesini taşıyor. Gerekçe: KABUL maddesi "seçili olmayan sekme kontrastı ≥ 3:1" ve kurucunun "zeminle aynı renk" şikâyeti. Kap, 999 px köşe ve dolu seçili tuvaldeki gibi. Üst bardaki anahtar tuval ölçüsünde bırakıldı.
3. **Yapışık sütun ile başlık sarma karşılaştırması.** Kesik düğmeler başlık sarmayla da çözülebilirdi: /vitrin tablosu başlık sarınca 660/658 px oluyordu. Ancak ORTAK-1 (A) kuralı "başlık tek satır, ad hücresi kırılmaz" diyor, OLCEK-1 v17 de "sütun gizlemek YASAK" diyor. Bu yüzden yapışık eylem sütunu seçildi. Taşma yoksa sticky etkisiz, 1440 görünümü değişmiyor.
4. **Katlanır başlık.** Başlık düğmesi kartın kenarını üstleniyor (−1 px taşma, çift çizgi yok). Katlanir bileşeni SATIS-3'e ait. Kural yalnız dugme.css'te, daha özgül seçiciyle yazıldı.

## 3. Gerçek tarayıcı ölçümü (başsız Chrome, CDP; tarayıcı bölmesi KULLANILMADI)
**dugme-olc.mjs** (hakem kopyası, değiştirilmedi), 39 rota × BASİT/PRO × aydınlık/karanlık:

| Genişlik | ÖNCE (taban) | SONRA | Mutasyon (dugme.css bağlantısı kaldırıldı) |
|---|---|---|---|
| 1440 | ortasız 8 · silik 198 · kırpık 0 | **0 · 0 · 0** | 8 · 198 · 0 |
| 1024 | ortasız 8 · silik 198 · kırpık 124 | **0 · 0 · 0** | 8 · 198 · 124 |
| 1440 (d2, düzeltilmiş renk) | — | 0 · 0 · 0 | — |
| 1024 (d2) | — | 0 · 0 · 0 | — |

Ham dosyalar: `/private/tmp/kuyumcu-serit/DUGME-2/{once1440,once1024,resmi1440,resmi1024,d2-1440,d2-1024,mut1440,mut1024}/`.

**tutarlilik.mjs** (1440, 39 rota), BASİT↔PRO farkı:
- ÖNCE: 13
- İlk SONRA: 15. Fark `/ayarlar #arayuz-basit/#arayuz-profesyonel` kalınlığındaydı: basılı durum moda bağlı olduğu için yazılan kalınlık fark sayılıyordu. Kalınlık kuralı kaldırıldı.
- Son SONRA: **13**. Liste ÖNCE ile birebir aynı (`diff` → AYNI). Fark artmadı.

**sikisma-olc.js K1–K12** (12 rota × 1280/375 × BASİT/PRO × aydınlık/karanlık, aynı sekmede dugme.css kapalı/açık):

| Sayaç | ÖNCE toplam | SONRA toplam | Yeni bulgu |
|---|---|---|---|
| K1 gizli görünen | 0 | 0 | 0 |
| K3 taşan | 0 | 0 | 0 |
| K3 kırık | 184 | 184 | 0 |
| K5 TRY | 0 | 0 | 0 |
| K6 dar seçici | 0 | 0 | 0 |
| K7 hizasız | 0 | 0 | 0 |
| K9 binen | 0 | 0 | 0 |
| K10 elipsis | 0 | 0 | 0 |
| K11 hata bandı | 0 | 0 | 0 |
| K12 düğme dikey | 56 | 56 | 0 |
| Yatay kaydırma | 0 | 0 | 0 |

Kalan K3 ve K12 bulguları önceden vardı, hepsi başka şeridin sayfasında:
- K3 kırık: /vitrin'de 22/kombinasyon (Kalem sütunu "Gram Altın" iki satır, sayfa KATMAN'ın), /kullanicilar'da 1 ("Merkez Şube").
- K12: /ayarlar'da 7/kombinasyon. Bunlar #gk-sema kartları: yazı, önizlemenin altında durduğu için sayılıyor. K12 bileşik kart düğmesini yanlış ölçüyor, tuval tasarımı böyle.

**Ekran görüntüleri (Read ile bakıldı):**
- `ham/TUVAL-KARSILASTIRMA/DUGME-2-{Faturalar,Evraklar,VadeTakibi,Siparisler,Ayarlar,Panel}-{aydinlik,karanlik}-1440.png`: ÖNCE | SONRA yan yana.
- `DUGME-2-Panel-tuval-1440.png` ve `DUGME-2-Ayarlar-tuval-1440.png`: TUVAL | SONRA.
- 1024: `/private/tmp/kuyumcu-serit/DUGME-2/foto/sonra_{vitrin,urunler,kullanicilar}-Profesyonel-aydinlik-1024.png` ve `sonra-kaydir_vitrin-Basit-karanlik-1024.png`.
- 375 ve 1280: `foto/son375_*`, `foto/son1280_*`.

Bölge bölge fark (Panel ve Ayarlar, tuval ile SONRA):

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Altın pozisyon "Ayrıntı" | Mavi bağlantı yazısı | Mavi yazı, 999 px ≥ 3:1 çerçeveli küçük hap | GEREKÇELİ FARK (eylem zeminden ayrılır) |
| "Alacak 0,0 g" kırılımı | Düz yazı | Çerçeveli hap | GEREKÇELİ FARK (bu bir düğme, detaya götürüyor) |
| Kim borçlu "Tahsil et/Öde" | Çerçeveli | Çerçeveli | AYNI |
| Ayarlar şema kartları | 1 px açık kenar | 1 px ≥ 3:1 kenar, seçili 3 px vurgu | GEREKÇELİ FARK (seçilebilir kart sınırı 1.4.11) |
| Ayarlar alt ikincil düğme | Açık kenar | ≥ 3:1 kenar | GEREKÇELİ FARK (karanlıkta 1,5:1 idi) |
| Üst bar mod anahtarı / sayfa sekmeleri | Tuval | Değişmedi | AYNI |

## 4. Testler
| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| DugmeIkiTestleri (yeni) | 10 | 10 | 0 |
| Dugme + Olcek + TasarimBelirtec + OrtakGorunum + KatmanTekKalip + ZoBelge | 136 | 136 | 0 |
| TAM süit (`--no-build`, derleme ayrıca yeşil; trx: `/private/tmp/kuyumcu-serit/trx-pDUGME-2/`) | 7148 | 7147 | 1 |

- Yeni test sayısı: 10.
- Tam süitteki tek kırmızı: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (beklenen 15, gerçek 16). Test yalnız `Kuyumcu.Core/I18n/Sozlukler.OrtakGorunum.cs` kaydını yansıtma ile sayıyor, bu paketin hiçbir dosyasını okumuyor. İki dosya da 28.09 14:16–14:17 tarihli. Sahibi ORTAK-1, **bu paketin ürünü değil**.
- Derleme: 0 hata. İlk derlemede 0 uyarı vardı. Son derlemede 1 uyarı çıktı: xUnit1031, `EtiketEkranTestleri.cs:145`, başka şeridin dosyası.
- Eşzamanlı derleme kilidi yüzünden iki koşum bir kez "file in use" (MSB4018) hatası verdi. Yeniden denemede yeşil geçti, gizlenmedi.

## 5. Mutasyon kanıtı (yedek: `/private/tmp/kuyumcu-serit/mut-yedek/DUGME-2/`, her biri `cmp` ile birebir geri alındı)
| Kod | Değişiklik | Kırmızı test(ler) | denetim.sh |
|---|---|---|---|
| M1-1 | index.html'den dugme.css bağlantısı kaldırıldı | D2_01 | DUGME2_BAGLANTI_SIRASI=1 ✗ |
| M1-1b (tarayıcı) | Sayfada dugme.css bağlantısı kaldırıldı | dugme-olc 1440: 8/198/0 · 1024: 8/198/124 | — |
| M1-2 | select appearance none → auto | D2_04 | DUGME2_KURAL_EKSIK=1 ✗ |
| M1-3 | Basılı düğme zemini silindi | D2_05 | DUGME2_KURAL_EKSIK=1 ✗ |
| M1-4 | Seçili olmayan sekme kenarı silindi | D2_06 | DUGME2_KURAL_EKSIK=1 ✗ |
| M1-5 | Katlanır başlık kenarı → 0 | D2_06 | — |
| M1-6 | Yapışık eylem sütunu sticky → static | D2_10 | — |
| M1-7 | si-tetik kenarı → transparent | D2_09 | — |
| M1-8 | Sabit renk (#d7dce4) yazıldı | D2_02, D2_06 | DUGME2_SABIT_RENK=1 ✗ |
| M1-9 | Düğme satır yüksekliği belirteci → normal | D2_03 | — |
| M1-10 | belirtecler `--dugme-kenar` → var(--ayrac) | D2_07 (kontrast < 3 her temada) | — |

Son mutasyondan sonra index.html, dugme.css, belirtecler.css ve denetim.sh `cmp` ile AYNI çıktı. Temiz `dotnet build` 0 hata verdi.

## 6. denetim.sh
`DENETİM TEMİZ.` Yeni sayaçlar:
- ✓ DUGME2_BAGLANTI_SIRASI 0
- ✓ DUGME2_SABIT_RENK 0
- ✓ DUGME2_KURAL_EKSIK 0

## 7. Canlı doğrulama ve temizlik
- API 5186'da çalıştı (dotnet run --no-build, Development; sistem anahtarı yalnız bu süreç için ortam değişkeniyle verildi). Vekil 5187'de çalıştı.
- Test kiracısı `PAKETDUGME-2-TEST Kuyumculuk` (01M3P5ESK1MFFP4D6G1DJ2XKYQ). Kurulum: davet 201, kayıt 201, giriş 200, `/kur/guncel` 200. Tohumlama demo-tohumla.js ile yapıldı: 6 cari, 12 ürün, 9 satış.
- Temizlikte API ve vekil PID'leri durduruldu. 5186 ve 5187'de dinleyen süreç yok.
- `kiraci-temizle.sh --uygula` rc=0. SQL: tenant_id kolonu olan 70 tabloda bu kiracıya ait satır **0**, `tenants where id=…` **0**.
- tenants sayısı: başlangıçta 16. Şimdi **15**. Aradaki fark `PAKETZO-ISLEM-A-TEST` kiracısının yok olması. Bu kiracıyı bu paket silmedi: temizleme betiği yalnız bu paketin kimliğiyle koştu. Muhtemelen sahibi ZO-ISLEM-A kendi temizliğini yaptı.
- 5199/5200'e dokunulmadı. QA-TEST kiracılarına dokunulmadı.
- Migration gerekmedi (şema değişikliği yok).

## 8. DoD sözleşmesi
Yeni varlık veya kayıt türü yok. Paket yalnız görünüm katmanında (CSS, belirteç). PATCH/DELETE uçları, yetki ve Kaydet() yolları değişmedi. "Tıklanınca hiçbir şey olmayan düğme" üretilmedi.

## 9. Değişen dosyalar
Komut: `git status --porcelain | grep -v -E "/(bin|obj)/"`, bu pakete ait olanlar:
- `dotnet/Kuyumcu.Web/wwwroot/css/dugme.css` (YENİ)
- `dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css` (DUGME-2 belirteç bloğu)
- `dotnet/Kuyumcu.Web/wwwroot/index.html` (tek link satırı)
- `dotnet/Kuyumcu.Tests/DugmeIkiTestleri.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/OlcekTekTestleri.cs` (OL5)
- `dotnet/denetim.sh` (DUGME-2 bloğu)

`dotnet/denetim.sh.mutbak` 18.09 tarihli, bu pakete ait değil, dokunulmadı.

## 10. Açık kalanlar (sahibiyle)
- **ORTAK-1:** `OrtakGorunumSaflik.O8` kırmızı (sözlük kaydı 16 alan, test 15 bekliyor).
- **KATMAN (Vitrin.razor):** Kalem sütununda K3 kırık 22 ve tablo stili. "Varsayılan" düz yazısı ile "Varsayılana dön" düğmesi aynı sütunda farklı biçimde. Önceden vardı.
- **SATIS-3 veya başka şerit:** `EtiketEkranTestleri.cs:145` xUnit1031 uyarısı.
- **Ölçüm aracı:** `betikler/tur/dugme-olc.mjs` renk ayrıştırması `color(srgb …)`/saydam kenarda yanlış sonuç veriyor. Düzeltilmiş sürüm: `/private/tmp/kuyumcu-serit/DUGME-2/dugme-olc-d2.mjs`. Ortak araca aktarılması orkestratörün kararı.
- **sikisma-olc K12:** bileşik kart düğmelerini (#gk-sema) sayıyor. Araç iyileştirmesi önerilir.

## 11. Kurucuya ve mali müşavire
Mevzuat içeriği yok. Kurucunun göz onayı için görüntüler: `ham/TUVAL-KARSILASTIRMA/DUGME-2-*.png`. Seçili olmayan sekme artık çerçeveli; tuvalde çerçevesiz. Tercih farklıysa tek belirteçle (`--sekme-kenar`) geri çevrilebilir.
