# PAKET KATMAN-1b — Kayıt paleti regresyonu + katman yazı boyu tek: uygulama raporu

**Tarih:** 2026-09-25 (15:50–16:55) · **Şerit:** KATMAN 2. tur (t5: API 5166, vekil 5167, `kuyumcu_test_t5`, CDP 9505–9513) · **Rol:** Ertuğ (frontend). Backend, veri modeli, uçlar değişmedi.
**Durum:** BİTTİ — paket kapısının 7 maddesi sağlandı. **EN GÜNCEL KANIT § 16 (29.09 22:20–23:00 turu: canlı DB ile yeniden ölçüm + KIYAS devri)**; önceki kanıt § 15 (28.09 00:28–01:50): doğrulayıcı bulguları kapatıldı, OLCEK-1 ile çıkan yeni çakışma (çekmece/liste içinde 16/14 px) düzeltildi, her kombinasyon için ekran görüntüsü alındı. Tam süitteki 24 kırmızının hepsi başka şeritlerin (EMANET-1, ZO-BELGE) dosyalarında (§ 10).

## 1. Spesifikasyon koda karşı (devir notu doğrulandı)
- `tuval-kabuk.css:240` `@media (min-width:769px){ .ust-bar .kayit-arama-dugme{display:none} }` → masaüstünde `#ust-kayit-ara` `display:none`, kutusu **0×0 (0,0)**. Canlıda ölçüldü: `acici:[0,0]` (1440/1024).
- `katman.js yerlestir()`: `if (a.bottom < 0 || a.top > g.h) return false;` 0×0 kutuyu reddetmiyordu → palet `data-katman="bagli"` alıyordu; `katman.css` bağlı palet kuralı (`top/left: var(--katman-…)`, `transform:none`) paleti **sol üst köşeye** koyuyordu.
- **Canlı yeniden üretim (M-K1b-9, § 8):** eski davranış (razor'da `data-katman-acici` + koruma yok + eski bağlı palet kuralı) → 1440 PRO `/cariler` Ctrl+K: palet kutusu **[8, 4, 576, 150]**, `merkezFark −424`, `data-katman="bagli"`. Görüntü: `ham/TUVAL-KARSILASTIRMA/KATMAN-1b-palet-once-sonra-1440.png` (solda önce, sağda sonra).
- Çekmece yazı boyları (ölçüldü, tabanda): `×` kapat `--yazi-2xl` (28 px), `.dugme` .875rem (14 px), Vitrin pano `#cekmece-pano-kalem .dugme-kucuk` katman.css'te `.875rem`, form girdileri 16 px.

## 2. Yapılanlar (devir notu: ağaçtaki iş KORUNDU, yeniden yazılmadı)
| Dosya | Değişiklik |
|---|---|
| `wwwroot/js/katman.js` | `gorunurKutu(a)` (en>0 **ve** boy>0) + `bagiCoz(katman)` eklendi. `yerlestir()`: açıcı görünmezse bağ KURULMAZ, önceden kurulmuşsa ÇÖZÜLÜR (`data-katman`, `data-katman-yon`, `--katman-ust/-sol/-azami-boy` silinir) ve `false` döner. `beforetoggle` ilk karede de görünmez açıcıya bağlamaz. Kalan mekanizma (flip, clamp, iki geçiş, kaydırma süzgeci) aynen. |
| `Bilesenler/GlobalArama.razor` | Palet üzerindeki `data-katman-acici="ust-kayit-ara"` kaldırıldı + açıklama yorumu. **id'ler değişmedi**; Ac/Kapat/Ctrl+K/arama yolları değişmedi. |
| `wwwroot/css/katman.css` | **§ 4 palet:** bağlı palet kuralları (`[data-katman="bagli"]`) SİLİNDİ. TEK KALIP = perdeli komut paleti: masaüstünde app.css'in üst-orta düzeni (top 10vh, yatay orta) korunur, `max-height: calc(100vh − 10vh − 8px)` + iç kaydırma; ≤480 px `top: 8px`, `width: calc(100vw − 16px)`. `.metin-kucuk` → `--yazi-md`. Karanlık temada perde `--govde` karışımı (önce `--metin` karışımı karanlıkta sayfayı AĞARTIYORDU — 1440 karanlık görüntüsünde görüldü). **§ 5 çekmeceler:** `.cekmece :is(button, a, li, label, input, select, textarea, .dugme, .btn) { font-size: var(--katman-oge-boyu) }` — Vitrin pano çekmecesine özel `.875rem` kuralı kalktı (genel kural onu da kapsıyor). `×` kapat öğe boyuna inince görünür çerçeveli kutuya alındı (tuval müşteri çekmecesi kalıbı: `--ayrac` çerçeve, `--yuzey` zemin; dokunma hedefi korunur). Sabit renk yok. |
| `Kuyumcu.Tests/KatmanTekKalipTestleri.cs` | K1_03 palet satırı yeni kurala çevrildi; YENİ: K1_18 (Theory ×2: `oge`/`popover`), K1_19, K1_20, K1_21. |
| `Kuyumcu.Tests/KabukAramaEkranTestleri.cs` | `K1_Palet_aciciya_baglanir…` → `K1_Palet_BAGLI_DEGIL_perdeli_komut_paleti_aria_modal` (paket kendi testi; bağlanma artık kusur). |

`Pages/Vitrin.razor` AÇILMADI (pano çekmecesinin ölçüsü katman.css'teki genel çekmece kuralıyla çözüldü). **Dokunulan sayfalar (TEK DOKUNUŞ): yok.** app.css, Sozlukler, denetim.sh, index.html değişmedi.

## 3. Sapmalar ve gerekçeleri
1. **Palet 375'te de açıcıya bağlanmıyor.** Devir notu "masaüstünde üst orta sabit komut paleti (perde ile), 375'te tam genişlik — tek kalıp" diyor. 375'te açıcı görünür olduğundan bağlı kalsaydı iki ayrı konumlama kuralı (bağlı / ortada) olurdu. Tek kalıp seçildi: perdeli komut paleti her genişlikte; 375'te üstte 8 px, tam genişlik. tur.mjs bunu `SERBEST-YUZEN` + `modal:true` (aria-modal + perde) sınıflıyor — kabul "perdeli palet modal sayılır" diyor (§ 5).
2. **Kapsama iki savunma:** JS koruması (0×0 açıcıya bağlanmaz) + CSS'te palet için bağlı kural yok. Canlıda yalnız razor+JS eski hâle getirildiğinde palet yine ortada kaldı (`data-katman="bagli"` yazıldı ama konum değişmedi, M-K1b-8b); sol üst yapışma ancak eski CSS kuralı da geri konunca üredi (M-K1b-9).
3. **Müşteri çekmecesi BASİT (`/cariler` `#cekmece-cari`) 15/16/17/18 px kaldı — GEREKÇELİ FARK:** `tuval-panel-musteri.css` (CUMA-M) `.musteri-panel-kap--sade #cekmece-cari …` kuralları tuval `Musteriler.dc.html` ölçüleridir (etiket 15 px = gövde×.8333, düğme 17 px; tuvalde 15/17 px öğeler sayıldı). Kurucu talimatı tuvali TEK doğruluk kaynağı yapıyor ve dosya CUMA-M'nin; ezilmedi. Öteki bütün çekmeceler (Ürün, Kasa, Vitrin pano, Müşteri PRO) tek 18 px.
4. **Tuvalde palet ve çekmece açık durumu yok** (`Profesyonel.dc.html` yalnız menüde "Ekran ara ⌘K"). Karşılaştırma, palet açıkken ekranın tuvalle yan yana görüntüsüyle yapıldı (§ 6).

## 4. Migration
Gerekmedi (şema değişikliği yok).

## 5. Canlı ölçüm (başsız Chrome, betikler/tur/cdp.mjs; API 5166, vekil 5167, kendi kiracım PAKETKATMAN-1B-TEST; tohum API ile: 6 cari 201, 5 ürün 201, 2 kasa 200, 2 fatura 200)
### 5.1 Palet — Ctrl+K (CDP `Input.dispatchKeyEvent`, Ctrl + KeyK) ve 375'te düğme; 7 rota (/, /islemler, /cariler, /urunler, /faturalar, /kasalar, /vitrin) × {BASİT, PRO} × {aydınlık, karanlık}
Betik `scratchpad/k1d/palet.mjs`; 196 ölçüm, açılmayan 0, mod uyuşmazlığı 0.
| Mod | Genişlik | Yol | n | kutu [sol, üst, en] | merkez farkı | data-katman | ekran dışı | perde tam ekran | palet en üstte | yazı | aile | yatay | açıcı kutusu | odak |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BASİT/PRO | 1440 | Ctrl+K | 14+14 | [432, 90, 576] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | Manrope | 0 | 0×0 | #kabuk-arama-girdi |
| BASİT/PRO | 1440 | Ctrl+K + "a" yazılı (6 sonuç) | 14+14 | [432, 90, 576] (boy 664) | 0 | yok | 0 | 28/28 | 28/28 | 18 px | Manrope | 0 | 0×0 | girdi |
| BASİT/PRO | 1024 | Ctrl+K / + yazılı | 28+28 | [224, 77, 576] | 0 | yok | 0 | ✓ | ✓ | 18 px | Manrope | 0 | 0×0 | girdi |
| BASİT/PRO | 375 | Ctrl+K / + yazılı / düğme | 42+42 | [8, 8, 359] | 0 | yok | 0 | ✓ | ✓ | 18 px | Manrope | 0 | 44×44 | girdi |

### 5.2 Çekmeceler — "Düzenle" (/cariler, /urunler, /kasalar) + Vitrin pano kalemi; × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1024, 375}
Betik `scratchpad/k1d/cekmece.mjs`; 48 ölçüm, 46 açıldı (BASİT aydınlık 1440'ta ilk iki rota mod geçişinden hemen sonra açıcı henüz çizilmemişti; aynı rotalar öteki 22 kombinasyonda açıldı).
| Çekmece | BASİT öğe yazısı | PRO öğe yazısı |
|---|---|---|
| #cekmece-urun | 18 px (TEK) | 18 px (TEK) |
| #cekmece-kasa | 18 px | 18 px |
| #cekmece-pano-kalem (Vitrin) | 18 px | 18 px |
| #cekmece-cari | 15 / 16 / 17 / 18 px — tuval ölçüsü, § 3.3 | 18 px |
Yatay kaydırma 0, iç taşma 0.

### 5.3 Tam tur — `tur.mjs` 7 rota × {BASİT,PRO} × {aydınlık,karanlık} × {1440,1024,375} + `analiz.py`
`scratchpad/k1d/tur/sonra`, `analiz.txt`: kayıt 954 · sayfa 84 · katman 870 (açılan 644) · hata 0 · geçersiz 0. (analiz.py'nin "kombinasyon eksik" satırı 42 kayıt/kombinasyon bekliyor — o eşik 9 rotalık tur içindir; burada 7 rota.)
| Ölçü | Sonuç | Açıklama |
|---|---|---|
| SERBEST-YÜZEN (modal olmayan) | **0** | 28 SERBEST-YÜZEN kaydının 28'i `#kabuk-arama-palet` 375 (aria-modal + tam ekran perde) → kabul gereği modal |
| küçük yazı (<14 px) | **0** | — |
| bağlı-açılır öğe yazısı | 18 px ×258; 14/16 px ×30 | 14/16 olanlar bağlı menü değil, Satış sayfa içi bölümleri (`#islem-elle-ac`, `#islem-diger-ayrinti-ac`, `#islem-pro-odeme-degistir`) — CUMA-S, önceki turda da vardı |
| çekmece/tam-ekran öğe yazısı | 18 px ×288; karışık ×40 | 40'ın hepsi `/cariler` BASİT `#cekmece-cari` (§ 3.3) |
| ekran-dışı | 4 | hepsi `#islem-pro-odeme` (PRO 1024/375) — CUMA-S, KATMAN-1 § 14.8'de zaten açık |
| sayfa-kaydı >40 px | 0 | — |
| yatay kaydırma · K1 gizli-görünen · K5 'TRY' | 0 · 0 · 0 | — |
| K1..K13 sayfa bulguları | 2 | `/urunler K10 kesik "Tüm kategoriler"` (PRO 1440/1024), `/vitrin K3 kırık #vitrin-kur-kaynagi` (1024) — ikisi de KATMAN-1 tur 3 tabanında (`k1c/analiz-sonra.txt`) birebir vardı; bu paketin dosyası değil |

**Görüntüler (Read ile bakıldı):** `k1d/palet/palet-_cariler-PRO-karanlik-1440-ctrlK.png` (üst orta, perde koyu), `palet-_cariler-PRO-aydinlik-375-dugme.png` (tam genişlik, üstte), `palet-_urunler-BASIT-aydinlik-1440-ctrlK-yazili.png` (sonuçlar 18 px, ekran içinde), `k1d/cekmece/cekmece-vitrin-PRO-aydinlik-1440.png` (pano çekmecesi tek ölçü), `cekmece-urunler-BASIT-karanlik-375.png` (çerçeveli ×).

## 6. Tuval karşılaştırması (1440×900) — `ham/TUVAL-KARSILASTIRMA/`
- `KATMAN-1b-Profesyonel-palet-1440.png` (tuval Profesyonel ↔ /islemler PRO, Ctrl+K açık)
- `KATMAN-1b-Musteriler-palet-1440.png` (tuval Musteriler ↔ /cariler BASİT, palet + sonuçlar)
- `KATMAN-1b-palet-once-sonra-1440.png` (önce: sol üst [8,4] ↔ sonra: üst orta [432,90])

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Menü "Ekran ara ⌘K" | var | var ("Ekran ara Ctrl K") | AYNI (bu paket değiştirmedi) |
| Üst bar kayıt arama düğmesi | yok | masaüstünde gizli (Ctrl+K), dar ekranda görünür | AYNI (tuval-kabuk.css, değişmedi) |
| Kayıt arama paleti | tuvalde açık durumu çizilmemiş | perdeli, üst orta, 576 px, 18 px Manrope, belirteç renkleri | GEREKÇELİ FARK: tuvalde olmayan ama bizde olan özellik silinmez; konumu kurucu/devir notu kararı |
| Sayfa gövdesi (satış kartları, tablo) | tuval | değişmedi | Kapsam dışı (başka şeritler) |

## 7. Testler
- **Önce:** KatmanTekKalip 18 + K1_Palet 1 (KATMAN-1 tur 3).
- **Sonra:** KatmanTekKalip **23** + K1_Palet 1 = **24/24 yeşil** (filtreli; tam süitte de 24 Başarılı). Yeni: 5 (K1_18 ×2, K1_19, K1_20, K1_21); değişen: K1_03, K1_Palet.
  - K1_18 `(yol: "oge"|"popover")`: gerçek katman.js node+sahte DOM; 0×0 açıcı → `data-katman` YOK, `--katman-ust/-sol` YOK; popover'da `beforetoggle` ilk karesi de bağlamaz.
  - K1_19: önce görünür açıcıya bağlanır (koruma gerçek bağı engellemez), açıcı gizlenip `resize` gelince bağ ÇÖZÜLÜR.
  - K1_20: en 0 ya da boy 0 açıcı da görünmez sayılır.
  - K1_21: palette bağlı kural yok; masaüstünde top/left/transform ezilmez, max-height + iç kaydırma; ≤480 top 8 px + tam genişlik; app.css üst-orta düzeni (fixed, left 50%, translateX(−50%)); çekmece öğe yazısı TEK belirteç, pano `.875rem` kuralı yok, rem font-size yok, × çerçeve belirteçten.
  - K1_Palet: palet `data-katman-acici`/`data-katman` taşımaz, role=dialog, aria-modal=true, perde var, satır içi style yok.
- **Derleme:** `dotnet build Kuyumcu.sln` son (mutasyonlardan sonra) **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145) xUnit1031` — başka şeridin dosyası). İlk denemeler eşzamanlı şerit derlemesi yüzünden `MSB4018 ConvertDllsToWebCil … Kuyumcu.Web.dll being used by another process` verdi; 30–40 sn sonra tekrarlandı, yeşil.
- **Tam süit** (`trx-pKATMAN-1b/_Ramazan-MacBook-Air_2026-09-25_16_40_34.trx`): **6304 · 6280 geçti · 24 başarısız** (§ 10).

## 8. Mutasyon kanıtları (yedek `scratchpad/mut-yedek/KATMAN-1b/`; `perl -0777 -pi` tek eşleşme → `dotnet test --no-build --filter "KatmanTekKalip|K1_Palet"` → yedek geri → `cmp` AYNI)
JS/CSS mutasyonlarında `--no-build`: testler kaynak dosyayı diskten okur; derleme eşzamanlı şeritle yarışıyordu (ilk deneme MSB4018). Razor mutasyonu derlemeli.
| Kod | Değişiklik | Kırmızı |
|---|---|---|
| **M-K1b-1** | `yerlestir`: `if (!gorunurKutu(a)) { bagiCoz(katman); return false; }` silindi (**0×0 açıcı koruması kaldırıldı — kabulün istediği**) | K1_18(oge), K1_18(popover), K1_19, K1_20 (4/24) |
| M-K1b-2 | `beforetoggle` içindeki `if (!gorunurKutu(r)) return;` silindi | K1_18(popover) (1/24) |
| M-K1b-3 | `bagiCoz`'da `removeAttribute('data-katman')` silindi | K1_19 (1/24) |
| M-K1b-4 | `a.width > 0 && a.height > 0` → `\|\|` | K1_20 (1/24) |
| M-K1b-5 | 480 px palet `width: calc(100vw − 2*pay)` silindi | K1_03, K1_21 (2/24) |
| M-K1b-6 | çekmece font-size `var(--katman-oge-boyu)` → `var(--yazi-md)` | K1_21 (1/24) |
| M-K1b-7 | katman.css'e `.kayit-arama-palet[data-katman="bagli"] { top: var(--katman-ust) }` geri eklendi | K1_21 (1/24) |
| M-K1b-8 (derlemeli) | GlobalArama'ya `data-katman-acici="ust-kayit-ara"` geri eklendi | K1_Palet_BAGLI_DEGIL… (1/24) |
| M-K1b-8b (canlı) | M-K1b-8 + canlıda katman.js koruması kaldırıldı | palet `data-katman="bagli"` aldı ama CSS'te bağlı palet kuralı olmadığından üst ortada kaldı [432,90] (ikinci savunma çalışıyor) |
| **M-K1b-9 (canlı)** | M-K1b-8b + eski bağlı palet CSS kuralı | **palet [8, 4, 576, 150], merkezFark −424 — regresyon birebir geri geldi** (aydınlık+karanlık); görüntü § 6 |
Hepsinden sonra katman.js, katman.css, GlobalArama.razor yedekle `cmp` AYNI; temiz derleme (`build-son.log`: 0 Hata) ve derlenen dll'de `data-katman-acici` 0 kez.

## 9. Paket kapısı (7 madde)
| # | Durum | Kanıt |
|---|---|---|
| 1 build | ✓ | `dotnet build Kuyumcu.sln` 0 Hata (build-son.log; 1 yabancı uyarı) |
| 2 testlerim | ✓ | 24/24 (KatmanTekKalip 23 + K1_Palet); tam süitte 24 Başarılı, 0 Başarısız |
| 3 sayaçlarım | ✓ | `bash denetim.sh` → **DENETİM TEMİZ.** (`index.html <script> 9`, `satır içi <script> 0`, `ModalPencere 48 (tavan 55)`, `SABIT_RENK 0`, `SECICI_TEK_KAYNAK 1`) |
| 4 mutasyon | ✓ | § 8: 8 statik + 2 canlı; 0×0 koruması kaldırılınca 4 test kırmızı |
| 5 canlı | ✓ | § 5: 196 palet + 48 çekmece + 954 kayıtlık tur ölçümü; görüntüler Read ile incelendi |
| 6 kiracı silindi | ✓ | `zsh kiraci-temizle.sh 01M3CACERK49K3XHDSKC386A2H --uygula`: 845 satır + tenants 1 silindi; 70 tenant_id tablosunda kalan satır **0** (information_schema ile sayıldı); davet satırı silindi (row_count 1, kalan 0). tenants: başta **16**, sonda **16**. QA-TEST-* dokunulmadı. API (5166) ve vekil (5167) `kill`; `lsof` boş; başsız Chrome'lar kapandı |
| 7 yeni kırmızı yok | ✓ | 24 kırmızının hiçbiri bu paketin dosyasına dayanmıyor (bUnit CSS/JS çalıştırmaz; testleri JS/CSS okuyanlar — KatmanTekKalip, TasarimBelirtec, P39, S1iGorsel — 0 başarısız). Sahipler § 10 |

### Birleşik koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Görünmez/0×0 açıcı → bağlı değil | ✓ | katman.js `gorunurKutu`/`bagiCoz`; K1_18–K1_20; canlı `data-katman` yok 196/196 |
| Kayıt paleti masaüstünde üst orta, perdeli | ✓ | 1440 [432,90,576], 1024 [224,77,576], merkez farkı 0, perde 196/196 |
| 375'te tam genişlik | ✓ | [8,8,359] 84/84 |
| Ctrl+K klavye ile açılır | ✓ | CDP Input.dispatchKeyEvent; odak kutuda 196/196 |
| Ekran dışı 0 (palet) | ✓ | 0/196 |
| SERBEST-YÜZEN 0 (perdeli palet modal) | ✓ | modal olmayan 0 |
| Küçük yazı 0 | ✓ | tur 0; palet 0 |
| Katman yazı boyu tek | ✓ (1 gerekçeli fark) | palet 18 px; satır menüsü/seçici 18 px; çekmeceler 18 px; istisna `#cekmece-cari` BASİT tuval ölçüsü (§ 3.3) |
| Vitrin pano çekmecesi .875rem → belirteç | ✓ | kural kalktı; canlı 18 px ×17 |
| 1440/1024/375 × BASİT/PRO × açık/koyu | ✓ | § 5 |
| id'ler değişmedi | ✓ | yalnız bir öznitelik kaldırıldı |
| app.css / Sozlukler / sayfa dokunuşu yok | ✓ | — |
| Kaydet/POST, yetki, yasal kapılar | Değişmedi | — |

**DoD sözleşmesi (yeni varlık):** yok — paket veri modeli, uç ya da kayıt türü eklemedi; PATCH/DELETE/denetim yükümlülüğü doğmadı. Paletin mevcut Kapat/Esc/perde davranışı aynen.

## 10. Açık kalanlar (sahibiyle)
1. **Tam süitte 24 kırmızı** (trx 16:40):
   - **EMANET-1 (T9)** — 17: `S35EmanetEkranTestleri` S35_12…S35_18 (7), `UX1C2EkranTestleri.Emanet_*` (4), `EmanetSadeTestleri` E2/E10 (2), `EmanetEkranTestleri.Elimizdeki_emanet…`, `P12EkranTestleri.B43_emanet_aciklamasi…`, `Dogrulayici.EMANET1SinirTestleri` S5/S8 (2).
   - **ZO-BELGE** — 7: `DenetimIziEkranTestleri` (Append_only_notu…, CJ_06…, Iki_sube_varsa…, Ozet_kartlari…, Sayfa_hata_vermeden_cizilir, Sube_secilince…, Varlik_filtresi…) — hata iletisi `zo-belge` sınıflı yeni işaretlemeyi gösteriyor.
2. `#islem-pro-odeme` ekran dışı (PRO 1024/375) ve Satış sayfa içi 14/16 px bölümler — **CUMA-S** (KATMAN-1 § 14.8'den süren).
3. **Vitrin pano çekmecesinde "Yuvarlama adımı" etiketi İKİ KEZ görünüyor** (görüntü `cekmece-vitrin-PRO-aydinlik-1440.png`): `Pages/Vitrin.razor` çekmecede hem `<label for="pano-yuvarlama">` hem `<Secici Etiket="…">` var. Dosya alanım bu sayfada yalnız ölçü; işaretlemeye dokunulmadı → **Vitrin sahibi / orkestratör**.
4. `/cariler` BASİT müşteri çekmecesi 15/17 px — tuval ölçüsü (CUMA-M); "tek yazı boyu" ile tuval çelişirse karar **orkestratör/kurucu**.
5. `/urunler K10 "Tüm kategoriler"` ve `/vitrin K3 #vitrin-kur-kaynagi` (1024) — tabanda da vardı; sahipleri Ürünler/Vitrin sayfa şeritleri.
6. Tur aracı (`betikler/tur/ekstra.js`) `modal` alanını hesaplıyor ama `tur` sınıfında kullanmıyor; perdeli palet `SERBEST-YUZEN` yazılıyor. Öneri (orkestratörün betiği): `tur` hesabında `modal` ise `modal` sınıfı.

## 11. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` (depo büyük ölçüde izlenmiyor; bu paketin dokunduğu dosyalar):
```
?? dotnet/Kuyumcu.Web/wwwroot/js/katman.js
?? dotnet/Kuyumcu.Web/wwwroot/css/katman.css
?? dotnet/Kuyumcu.Web/Bilesenler/GlobalArama.razor
   dotnet/Kuyumcu.Tests/KatmanTekKalipTestleri.cs
   dotnet/Kuyumcu.Tests/KabukAramaEkranTestleri.cs
   dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/KATMAN-1b-{Profesyonel-palet,Musteriler-palet,palet-once-sonra}-1440.png (yeni)
   dokumanlar/09-uygulama-kayitlari/PAKET-KATMAN-1b-uygulama-raporu.md (bu rapor)
```
Ölçüm betikleri ve çıktıları yalnız scratchpad'de (`k1d/`); proje ağacına profil/geçici dosya yazılmadı.

## 12. Kurucuya / mali müşavire notlar
Mevzuat konusu yok. Kurucunun göreceği: masaüstünde Ctrl+K ile açılan "Müşteri, ürün, fiş bul" paleti artık ekranın üst ortasında, koyu perdenin üstünde (karanlık temada perde artık sayfayı ağartmıyor); telefonda üstte tam genişlik. Düzenle çekmecelerinde yazılar tek boyda (18 px), kapat × çerçeveli kutuda.

---

## 13. 27.09 DEVAM TURU (Ertuğ, t5: API 5166 · vekil 5167 · `kuyumcu_test_t5` · CDP 9501–9509) — güncel ağaçta yeniden kanıt

### 13.1 Ağaç doğrulaması (devir notu: ağaçtaki iş KORUNDU)
- `katman.js` (`gorunurKutu`/`bagiCoz`, `yerlestir` 0×0 koruması, `beforetoggle` koruması), `katman.css` § 4 (perdeli palet, bağlı palet kuralı YOK) ve § 5 (çekmece öğesi `--katman-oge-boyu`), `GlobalArama.razor` (`data-katman-acici` yok) ağaçta birebir duruyor. Yeniden yazılmadı.
- Yarım mutasyon denetimi: `/private/tmp/kuyumcu-serit` yeniden başlatmada silinmişti (yedek yok); ilgili testler yeşil (13.3) → ağaçta yarım mutasyon YOK. Önceki scratchpad (`k1d/`) de boştu → bütün ölçümler yeniden koşuldu.

### 13.2 Bu turda yapılan (tek kod değişikliği)
| Dosya | Değişiklik | Gerekçe |
|---|---|---|
| `Pages/Vitrin.razor` (pano çekmecesi) | `<label for="pano-yuvarlama">` satırı kaldırıldı + açıklama yorumu | Önceki turun açık kalanı § 10.3: `Secici` etiketini kendisi çiziyor (`label.alan-etiket for=pano-yuvarlama`); ikinci label çekmecede "Yuvarlama adımı"nı İKİ KEZ gösteriyordu. Vitrin eşzamanlı şerit listesinde KATMAN şeridinin, dosya alanımda "yalnız pano çekmecesi" — değişiklik o çekmecenin içinde. id'ler değişmedi. |
| `Kuyumcu.Tests/P17EkranTestleri.cs` | YENİ `P17VitrinEkranTestleri.KATMAN1b_pano_cekmecesinde_yuvarlama_etiketi_TEK_kez` | `#cekmece-pano-kalem` içinde `label[for=pano-yuvarlama]` tek; metni `Sozlukler.Tr.Pano.Yuvarlama` olan label tek. |

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Vitrin.razor` — bu dalgada KATMAN-1b'nin İLK dokunuşu (25.09 turunda açılmamıştı, § 2). app.css, Sozlukler, denetim.sh değişmedi. Migration gerekmedi.

### 13.3 Derleme ve testler
- `dotnet build Kuyumcu.sln` (`build-son.log`, 22:19): **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145) xUnit1031` — başka şeridin). Eşzamanlı şeritlerin `--no-incremental` derlemeleri iki denemeyi `MSB4018 ConvertDllsToWebCil` ve `MSB3030 … dll bulunamadı` ile düşürdü; başka derleme bitince tekrarlandı, yeşil.
- Filtreli (KatmanTekKalip + K1_Palet + P17Vitrin + KabukArama): **62/62 Başarılı**.
- **Tam süit:** ilk koşum (`tam.log`) 988 kırmızı verdi — hepsi `FileNotFoundException BCrypt-Net-Next` vb.: başka şeridin `--no-incremental` derlemesi koşum ORTASINDA bin'i sildi (ortam; kod değil). Bu yüzden 22:19 derlemesinin anlık görüntüsü `/private/tmp/kuyumcu-serit/KATMAN-1b/j/dotnet/tbin` altına kopyalandı (Kuyumcu.sln ve kaynak dizinleri sembolik bağla; proje ağacına dosya yazılmadı) ve oradan koşuldu: `trx-pKATMAN-1b/_Mac_2026-09-27_22_19_39.trx` → **6333 · 6325 Başarılı · 8 Başarısız**. 8'in hiçbiri bu paketin dosyası değil (§ 13.7).
- 25.09 devir listesindeki 24 kırmızı (EMANET-1 17, ZO-BELGE 7) bu koşumda **0** — o şeritler kapatmış.
- `bash denetim.sh` → **DENETİM BAŞARISIZ** (tek ✗): `P-39: TABLO_SARMALAYICISIZ 2` — `Pages/PosTahsilatlar.razor` (22:31'de ZO-ISLEM-A şeridi değiştirdi; sarmalayıcısız iki `<table>`). **Benim sayaçlarım ✓:** `index.html satır içi <script> 0`, `ModalPencere 48 (tavan 55)`, `SABIT_RENK 0`, `SECICI_TEK_KAYNAK 1`.

### 13.4 Mutasyon kanıtları (yedek `/private/tmp/kuyumcu-serit/mut-yedek/KATMAN-1b/`; `perl -0777 -pi` → filtreli test → yedek geri → `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test(ler) |
|---|---|---|
| **M-K1b-1** | `yerlestir`: `if (!gorunurKutu(a)) { bagiCoz(katman); return false; }` silindi — **0×0 açıcı koruması kaldırıldı** | K1_18(oge), K1_18(popover), K1_19, K1_20 |
| M-K1b-2 | `beforetoggle` `if (!gorunurKutu(r)) return;` silindi | K1_18(popover) |
| M-K1b-3 | `bagiCoz` `removeAttribute('data-katman')` silindi | K1_19 |
| M-K1b-4 | `width > 0 && height > 0` → `\|\|` | K1_20 |
| M-K1b-5 | 480 px palet `width: calc(100vw − 2*pay)` silindi | K1_03, K1_21 |
| M-K1b-6 | çekmece `font-size: var(--katman-oge-boyu)` → `var(--yazi-md)` | K1_21 |
| M-K1b-7 | `.kayit-arama-palet[data-katman="bagli"] { top: var(--katman-ust) }` geri eklendi | K1_21 |
| M-K1b-8 (derlemeli) | GlobalArama'ya `data-katman-acici="ust-kayit-ara"` geri eklendi | K1_Palet_BAGLI_DEGIL_perdeli_komut_paleti_aria_modal |
| **M-K1b-10** (derlemeli, YENİ) | Vitrin pano çekmecesine `<label for="pano-yuvarlama">` geri eklendi | P17VitrinEkranTestleri.KATMAN1b_pano_cekmecesinde_yuvarlama_etiketi_TEK_kez |
M-K1b-8 ve M-K1b-10 tek derlemede birlikte uygulandı (eşzamanlı derleme yarışını azaltmak için); kırmızılar birbirinden ayrı ve her biri yalnız kendi mutasyonunun dosyasını sınıyor. Hepsinden sonra 4 dosya `cmp` AYNI; ardından temiz derleme 0 Hata.

### 13.5 Canlı ölçüm (başsız Chrome, `betikler/tur/cdp.mjs`; kendi kiracım `PAKETKATMAN-1b-TEST Olcum`, API ile tohum: 2 kasa 200, 6 cari 201, 3 ürün 201)
**Palet** (`/private/tmp/kuyumcu-serit/KATMAN-1b/olc.mjs`): 7 rota × {BASİT, PRO} × {aydınlık, karanlık}; Ctrl+K = CDP `Input.dispatchKeyEvent` (ctrl + KeyK); 196 ölçüm, açılmayan 0, mod uyuşmazlığı 0.
| Genişlik | Yol | n | kutu [sol, üst, en] | merkez farkı | data-katman | ekran dışı | perde tam | palet en üstte | yazı | aile | küçük | yatay | açıcı | odak | aria-modal |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | Ctrl+K | 28 | [432, 90, 576] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | Manrope | 0 | 0 | 0×0 | #kabuk-arama-girdi | true |
| 1440 | Ctrl+K + "a" | 28 | [432, 90, 576] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | Manrope | 0 | 0 | 0×0 | girdi | true |
| 1024 | Ctrl+K / + "a" | 28+28 | [224, 77, 576] | 0 | yok | 0 | ✓ | ✓ | 18 px | Manrope | 0 | 0 | 0×0 | girdi | true |
| 375 | Ctrl+K / + "a" / düğme | 28×3 | [8, 8, 359] | 0 | yok | 0 | ✓ | ✓ | 18 px | Manrope | 0 | 0 | 44×44 | girdi | true |

**Çekmeceler** ("Düzenle"; `olc.mjs` + `olc2.mjs`), {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1024, 375}:
| Çekmece | BASİT öğe yazısı | PRO öğe yazısı | "Yuvarlama adımı" etiketi |
|---|---|---|---|
| #cekmece-urun | 18 px TEK (6/6) | 18 px TEK (6/6) | — |
| #cekmece-kasa | 18 px TEK (6/6) | 18 px TEK (6/6) | — |
| #cekmece-pano-kalem (Vitrin) | 18 px TEK (6/6) | 18 px TEK (6/6) | **1** (12/12 — önce 2) |
| #cekmece-cari | 15 / 16 / 17 / 18 px (tuval ölçüsü, § 3.3) | 18 px TEK (6/6) | — |
Yatay kaydırma 0, iç taşma 0.

**Tam tur** (`tur.mjs` 7 rota × {BASİT,PRO} × {aydınlık,karanlık} × {1440,1024,375}; `analiz.py`; çıktı `/private/tmp/kuyumcu-serit/KATMAN-1b/tur/`): kayıt 865 · sayfa 84 · katman 781 (açılan 558) · hata 0 · geçersiz 0.
| Ölçü | Sonuç | Açıklama |
|---|---|---|
| SERBEST-YÜZEN (modal olmayan) | **0** | 28 SERBEST-YÜZEN kaydının 28'i `#kabuk-arama-palet` 375 ve `modal:true` (perdeli palet — kabul gereği modal) |
| küçük yazı (<14 px) | **0** | — |
| bağlı-açılır | 18 px TEK ×207; 14 px ×12; karışık 14/16 ×6 | 14/16 olanlar Satış sayfa içi bölümleri (`#islem-aciklama-sar` ← `#islem-diger-ayrinti-ac`, `#islem-pro-odeme`) — CUMA-S/SATIS-3, önceki turda da vardı |
| çekmece / tam-ekran | 18 px TEK ×176 / ×88; karışık 27 / 14 | karışıkların HEPSİ `/cariler` BASİT `#cekmece-cari` (§ 3.3) |
| ekran dışı | 4 | hepsi `#islem-pro-odeme` PRO 1024/375 — CUMA-S/SATIS-3 |
| sayfa kaydı > 40 px | 0 | — |
| yatay · K1 gizli-görünen · K5 'TRY' · K3 taşan · K9 · K12 · K13 | 0 · 0 · 0 · 0 · 0 · 0 · 0 | — |
| K sayfa bulguları | 3 | `/urunler K10 "Tüm kategoriler"` (PRO 1440/1024) ve `/vitrin K3 #vitrin-kur-kaynagi` (1024) — tabanda da vardı; YENİ görünen: `/cariler K3 kırık th "TL BAKİYE"/"DÖVİZ BAKİYE"` (PRO 1024) — Cariler tablo başlığı, bu paketin dosyası değil (CUMA-M/ORTAK-1 `ortak.css` tablo başlığı işi sürüyor, § 13.7) |

**Görüntüler (Read ile bakıldı):** `olc/foto/palet_cariler-PRO-karanlik-1440-ctrlK.png` (üst orta, koyu perde), `palet_cariler-BASIT-aydinlik-375-dugme.png` (üstte tam genişlik), `cekmece_vitrin-PRO-aydinlik-1440.png` ("Yuvarlama adımı" TEK kez, 18 px, × çerçeveli).

### 13.6 Tuval karşılaştırması (1440×900; solda tuval, sağda biz) — `ham/TUVAL-KARSILASTIRMA/`
- `KATMAN-1b-Profesyonel-palet-1440-2709.png` (tuval Profesyonel ↔ /islemler PRO, Ctrl+K açık) — Read ile bakıldı.
- `KATMAN-1b-Musteriler-palet-1440-2709.png` (tuval Musteriler ↔ /cariler BASİT, Ctrl+K açık).
Bölge tablosu § 6 ile aynı (menü "Ekran ara Ctrl K" AYNI; üst bar arama düğmesi masaüstünde gizli AYNI; palet tuvalde çizilmemiş → GEREKÇELİ FARK: özellik silinmez, konum devir notu kararı; sayfa gövdesi kapsam dışı).

### 13.7 Paket kapısı (27.09) ve açık kalanlar
| # | Durum | Kanıt |
|---|---|---|
| 1 build | ✓ | 0 Hata (1 yabancı uyarı) |
| 2 testlerim | ✓ | 62/62 filtreli; tam süitte KatmanTekKalip/K1_Palet/P17Vitrin 0 başarısız |
| 3 sayaçlarım | ✓ | denetim.sh'ta benim sayaçlarım ✓; tek ✗ P-39 TABLO_SARMALAYICISIZ = PosTahsilatlar.razor (ZO-ISLEM-A) |
| 4 mutasyon | ✓ | § 13.4: 9 mutasyon, hepsi adı yazılı testte kırmızı, hepsi cmp AYNI |
| 5 canlı | ✓ | § 13.5 (196 palet + 60 çekmece + 865 tur kaydı), görüntüler okundu |
| 6 kiracı silindi | ✓ | `kiraci-temizle.sh 01M3J4YCKQE2NWC8G5GG8517BV --uygula`; ardından 70 tenant_id tablosu tarandı: `kur_snapshots`'ta API zamanlayıcısının silmeyle yarışıp yazdığı 1 satır bulundu, silindi (row_count 1) → kalan **0**; davet satırı silindi (1 → 0). tenants: başta **18**, sonda **18**. API (5166) ve vekil (5167) `kill`, `lsof` boş, başsız Chrome'lar kapatıldı |
| 7 yeni kırmızı yok | ✓ | 8 kırmızının hiçbiri bu paketin dosyalarına dayanmıyor (sahipler aşağıda) |

**Açık kalanlar (sahibiyle):**
1. Tam süit 8 kırmızı: `P10PosBacagiEkranTestleri` ×4 (CIHAZ_YOKSA, COK_CIHAZ, KART_secilince, TEK_CIHAZ) — **ZO-ISLEM-A** (Pos*); `S24EskiAltinEkranTestleri` ×2 — **SATIS-3** (Islemler*); `OrtakGorunumSaflik.O2_tablo_basligi_tek_satir…` (ortak.css regex, dosya 22:31'de değişti) — **ORTAK-1**; `P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` → `dotnet/denetim.sh.mutbak` (0 bayt, 18.09 tarihli; bu paketin değil) — **orkestratör** (silmek dosya silme sayılır; kurucuya/orkestratöre bırakıldı).
2. denetim.sh `P-39 TABLO_SARMALAYICISIZ 2` — `Pages/PosTahsilatlar.razor` — **ZO-ISLEM-A**.
3. `#cekmece-cari` BASİT 15/16/17 px (tuval ölçüsü, CUMA-M) ↔ "tek yazı boyu" — **orkestratör/kurucu kararı** (değişmedi).
4. `#islem-pro-odeme` ekran dışı (PRO 1024/375) ve Satış sayfa içi 14/16 px bölümler — **SATIS-3/CUMA-S**.
5. `/urunler K10 "Tüm kategoriler"`, `/vitrin K3 #vitrin-kur-kaynagi` (1024), `/cariler K3 PRO 1024 tablo başlığı` — sayfa sahipleri.
6. Ağaçta 25.09'dan kalma **`PAKETKATMAN-1b-TEST Dogrulama`** kiracısı (01M3CDR07QP3ENEQH60PEEMK67, 25.09 13:58 UTC — o günkü hasım doğrulayıcının; kota kesintisinde silinmemiş). Kendi açtığım kiracı olmadığı için dokunmadım → **orkestratör** `kiraci-temizle.sh 01M3CDR07QP3ENEQH60PEEMK67 --uygula` ile silebilir.
7. Tur aracı perdeli paleti hâlâ `SERBEST-YUZEN` sınıflıyor (`ekstra.js` `modal` alanı `tur` hesabına girmiyor) — orkestratörün betiği.

### 13.8 Değişen dosyalar (bu paketin; `git status --porcelain | grep -v -E "/(bin|obj)/"` süzülmüş)
```
?? dotnet/Kuyumcu.Web/wwwroot/js/katman.js            (25.09, korundu)
?? dotnet/Kuyumcu.Web/wwwroot/css/katman.css          (25.09, korundu)
?? dotnet/Kuyumcu.Web/Bilesenler/GlobalArama.razor    (25.09, korundu)
?? dotnet/Kuyumcu.Web/Pages/Vitrin.razor              (27.09 — pano çekmecesi çift etiket)
   dotnet/Kuyumcu.Tests/KatmanTekKalipTestleri.cs     (25.09)
   dotnet/Kuyumcu.Tests/KabukAramaEkranTestleri.cs    (25.09)
?? dotnet/Kuyumcu.Tests/P17EkranTestleri.cs           (27.09 — yeni test)
   dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/KATMAN-1b-{Profesyonel,Musteriler}-palet-1440-2709.png (yeni)
   dokumanlar/09-uygulama-kayitlari/PAKET-KATMAN-1b-uygulama-raporu.md
```
Ölçüm betikleri/çıktıları yalnız `/private/tmp/kuyumcu-serit/KATMAN-1b/`. COMMIT YOK.

## 14. 27.09 DOĞRULAMA SONRASI TUR (23:19–23:50; Ertuğ, t5: API 5166 · vekil 5167 · `kuyumcu_test_t5` · CDP 9501–9502)

Amaç: hasım doğrulayıcının KRİTİK bulgusunu ("çekmece yazı boyu iddiası gerçek ekran görüntüsüyle değil yalnız JS ölçümüyle doğrulandı") kapatmak: sayıyı güncel ağaçta YENİDEN ölçmek, çekmecenin GERÇEK ekran görüntüsünü almak ve iki raporu aynı gerçeğe bağlamak. **Bu turda kod değişikliği YOK.**

### 14.1 Ağaç
- `katman.js`, `katman.css`, `GlobalArama.razor`, `Vitrin.razor` mutasyon yedekleriyle (`/private/tmp/kuyumcu-serit/mut-yedek/KATMAN-1b/`) `cmp` AYNI → yarım mutasyon yok.
- `dotnet build Kuyumcu.sln` → **0 Uyarı, 0 Hata** (`build-3.log`). Filtreli (KatmanTekKalip + K1_Palet + P17Vitrin + KabukArama) → **62/62**.

### 14.2 Canlı ölçüm (yeni kiracı `PAKETKATMAN-1b-TEST Olcum3`, davet satırı + `/auth/kayit` 201; tohum API ile: 2 kasa 200, 6 cari 201, 3 ürün 201, `/pano/guncel` 200)
Araç: `olc3.mjs` (= § 13.5 `olc.mjs`, çıktı dizini `olc3/`) ve `olc3b.mjs` (+ her palet kaydına **yüklü stil sayfaları listesi** `cssYuklu`).

**Önemli — ilk koşumda sapma, gizlenmedi:** BASİT ve PRO iki başsız Chrome'da AYNI kullanıcıyla PARALEL koşuldu. BASİT sonuçları § 13.5 ile aynı çıktı; **PRO koşumunda** çekmeceler `{14px:13, 16px:1, 28px:1, 13px:1}` (pano) / `{14px:6, 16px:2, 28px:1}` (kasa) ve palette 13/14 px (`kucuk` 14/28) verdi. Tanı:
1. Aynı sayfa PRO'da tek başına açıldığında (`tani2.mjs`) çekmece `label`/`.cekmece-kapat` = **18 px**; eşleşen kural `katman.css | .cekmece :is(button,a,li,label,…) => var(--katman-oge-boyu)`.
2. Aynı sayfada `katman.css` elle devre dışı bırakıldığında (`tani3.mjs`) değerler **14 px / 28 px** — ilk PRO koşumunun ölçtüğü değerlerle birebir aynı.
3. PRO tek başına yeniden koşuldu (`olc3b`): 14/14 palet kaydında `cssYuklu` = 15 stil sayfasının TAMAMI (katman.css dahil) ve sonuçlar aşağıdaki gibi **18 px TEK**.
→ Sonuç: ilk PRO koşumunda o tarayıcının açılışında `katman.css` UYGULANMAMIŞTI (eşzamanlı şeritlerin `dotnet build`'i sırasında statik varlık geçici olarak sunulamamış olabilir; API/vekil günlüğünde kesin 404 kanıtı bulunamadı — neden DOĞRULANAMADI, yalnız belirti ve tekrar ölçümü kesin). Kod kusuru değil; ölçüm tekrarlanabilir biçimde 18 px.

**Palet** (7 rota × {BASİT(olc3), PRO(olc3b)} × {aydınlık, karanlık}; Ctrl+K = CDP `Input.dispatchKeyEvent`):
| Genişlik | Yol | n (B+P) | kutu [sol, üst, en] | merkez farkı | data-katman | ekran dışı | perde tam | en üstte | yazı | küçük | yatay | açıcı | aria-modal |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | Ctrl+K / + "a" | 14+14 / 14+14 | [432, 90, 576] | 0 | yok | 0 | tümü | tümü | 18 px Manrope | 0 | 0 | 0×0 | true |
| 1024 | Ctrl+K / + "a" | 14+14 / 14+14 | [224, 77, 576] | 0 | yok | 0 | tümü | tümü | 18 px | 0 | 0 | 0×0 | true |
| 375 | Ctrl+K / + "a" / düğme | 14+14 ×3 | [8, 8, 359] | 0 | yok | 0 | tümü | tümü | 18 px | 0 | 0 | 44×44 | true |
(0×0 açıcıda palet `data-katman` taşımıyor = BAĞLI DEĞİL; sol üst köşeye yapışma yok.)

**Çekmeceler** ({BASİT, PRO} × {aydınlık, karanlık} × {1440, 1024, 375}; her hücre 6 ölçüm):
| Çekmece | BASİT öğe yazısı | PRO öğe yazısı | "Yuvarlama adımı" etiketi | iç taşma | yatay |
|---|---|---|---|---|---|
| #cekmece-pano-kalem (Vitrin) | **18 px TEK** (16 öğe, 6/6) | **18 px TEK** (16 öğe, 6/6) | **1** (12/12) | 0 | 0 |
| #cekmece-kasa | 18 px TEK (9 öğe) | 18 px TEK (9 öğe) | — | 0 | 0 |
| #cekmece-cari | 15 / 16 / 17 / 18 px (tuval ölçüsü, § 3.3 — açık kalan) | (PRO'da "Düzenle" satır menüsü üzerinden — bu turda açılmadı, `duzenle-yok`) | — | 0 | 0 |
| /urunler "Düzenle" | `duzenle-yok` (seçici bu turda ürün düzenle düğmesini bulamadı; § 13.5'te 18 px TEK ölçülmüştü) | aynı | — | — | — |

**Gerçek ekran görüntüleri (Read ile bakıldı, projeye kopyalandı):**
- `ham/TUVAL-KARSILASTIRMA/KATMAN-1b-cekmece-vitrin-PRO-aydinlik-1440-2709b.png` — pano çekmecesi sağda tam boy, "Gram Altın" başlığı, etiketler/girdiler/"Varsayılana dön"/İptal/Kaydet tek boyda (18 px), "Yuvarlama adımı" TEK kez, × çerçeveli.
- `ham/TUVAL-KARSILASTIRMA/KATMAN-1b-cekmece-vitrin-BASIT-karanlik-375-2709b.png` — 375 px, koyu tema, tam genişlik çekmece, taşma yok, "Yuvarlama adımı" TEK.
- `ham/TUVAL-KARSILASTIRMA/KATMAN-1b-palet-cariler-PRO-karanlik-1440-2709b.png` — Ctrl+K paleti üst ortada, koyu perde, 18 px, "Kapat" düğmesi.
Tüm ham görüntüler: `/private/tmp/kuyumcu-serit/KATMAN-1b/olc3/foto/` (44 dosya) ve `olc3b/foto/`.

### 14.3 Mutasyonlar (güncel ağaçta yeniden; yedek → `perl -0777 -pi` → filtreli test → yedek geri → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| **M-K1b-1** | `yerlestir`: `if (!gorunurKutu(a)) { bagiCoz(katman); return false; }` silindi (0×0 açıcı koruması) | K1_18(oge), K1_18(popover), K1_19, K1_20 (4) | AYNI |
| M-K1b-6 | `.cekmece :is(…) { font-size: var(--katman-oge-boyu) }` → `var(--yazi-md)` | K1_21_kayit_paleti_perdeli_komut_paleti_ust_orta_375te_tam_genislik_ce… (1) | AYNI |
| M-K1b-10 (derlemeli) | Vitrin pano çekmecesine `<label for="pano-yuvarlama">` geri eklendi | P17VitrinEkranTestleri.KATMAN1b_pano_cekmecesinde_yuvarlama_etiketi_TEK_kez (1) | AYNI |
Son mutasyondan sonra temiz derleme: ilk deneme eşzamanlı şerit derlemesiyle yarıştı (`MSB4018 GenerateWasmBootJson` dosya kilidi; bin'de mutasyonlu derleme kaldığı için filtreli 1 kırmızı) — kaynak `cmp` AYNI doğrulanıp tekrarlandı: **0 Uyarı 0 Hata**, filtreli **62/62**. (M-K1b-2/3/4/5/7/8 § 13.4'te bugün kanıtlandı; o dosyalar o zamandan beri değişmedi — `cmp` AYNI.)

### 14.4 Tam süit ve denetim
- `dotnet test … --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pKATMAN-1b/tur3` → **6378 · 6375 Başarılı · 3 Başarısız**: `S24EskiAltinEkranTestleri` ×2 (SATIS-3), `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir` (ZO-ISLEM-A, PosTahsilatlar.razor). Doğrulayıcının listesiyle birebir aynı; bu paketin testlerinde kırmızı 0 → **yeni kırmızı 0**.
- `denetim.sh` → **DENETİM BAŞARISIZ**, tek ✗ `P-39 TABLO_SARMALAYICISIZ 2` (Pages/PosTahsilatlar.razor — ZO-ISLEM-A). Benim sayaçlarım ✓: satır içi script 0, ModalPencere 48 (tavan 55), SABIT_RENK 0, SECICI_TEK_KAYNAK 1.

### 14.5 Temizlik
- API (PID 93986) ve vekil `kill`; `lsof` 5166/5167/9501/9502 boş.
- `kiraci-temizle.sh 01M3J8EE1AWX1HBG4XTP7PEGWH --uygula` (rc 0); API durdurulduktan SONRA 70 tenant_id tablosu tarandı → toplam **0** satır; `tenants` içinde kimlik 0; davet satırı `PKATMAN1B-DAVET-2709-3` silindi (row_count 1).
- tenants: başta **18**, sonda **17**. Fark benim değil: bu tur sırasında `01M3J2XAVSFDSJR0EH6WEAKNXY PAKETZO-BELGE-TEST Kuyumculuk` başka şerit (ZO-BELGE) tarafından silindi; benim kiracım eklendi (+1) ve silindi (−1).

### 14.6 Doğrulayıcı bulgularıyla eşleşme
| Bulgu | Durum |
|---|---|
| [KRİTİK] çekmece yazı boyu ekran görüntüsüz doğrulandı | KAPANDI (uygulayıcı tarafı): güncel ağaçta 24 çekmece + 168 palet ölçümü, üç görüntü projeye kopyalandı ve okundu; ilk PRO koşumundaki sapma ve tanısı § 14.2'de yazılı. Doğrulayıcının kendi sekmesinde tekrarlaması için betikler `/private/tmp/kuyumcu-serit/KATMAN-1b/olc3b.mjs`, `tani2.mjs`, `tani3.mjs`. |
| [DÜŞÜK] 25.09 kiracısı 01M3CDR07QP3ENEQH60PEEMK67 | AÇIK — kendi açtığım kiracı değil; kural gereği dokunmadım → orkestratör `kiraci-temizle.sh 01M3CDR07QP3ENEQH60PEEMK67 --uygula` |
| [DÜŞÜK] denetim P-39 / K5 | AÇIK — ZO-ISLEM-A |
| [DÜŞÜK] S24 ×2 | AÇIK — SATIS-3 |
| [DÜŞÜK] #cekmece-cari 15/16/17 px | AÇIK — orkestratör/kurucu kararı (tuval ölçüsü ↔ tek yazı boyu) |

### 14.7 Ek açık kalanlar
- Ağaçta `dotnet/Kuyumcu.Web/Pages/Islemler.Sade.cs.tmp` (25.09 11:29) duruyor — Islemler* SATIS-3 alanı, bu paketin değil; dosya silme yasak → orkestratör.
- Eşzamanlı şerit derlemeleri statik varlık sunumunu ve temiz derlemeyi geçici bozabiliyor (§ 14.2, § 14.3) — ölçüm betiklerine `cssYuklu` denetimi eklenmesi önerilir (orkestratör betiği).

---

## 15. 28.09 DEVAM TURU (00:28–01:50; Ertuğ, t5: API 5166 · vekil 5167 · `kuyumcu_test_t5` · CDP 9503–9518) — EN GÜNCEL KANIT

### 15.1 Ağaç ve yarım mutasyon denetimi
- `katman.js`, `katman.css`, `GlobalArama.razor`, `Vitrin.razor` 27.09 yedekleriyle (`/private/tmp/kuyumcu-serit/mut-yedek/KATMAN-1b/`) `cmp` AYNI → yarım mutasyon YOK; ağaçtaki iş korundu, yeniden yazılmadı.
- **Derleme yarışı çözümü (doğrulayıcı DÜŞÜK bulgusu):** bu turda bütün derleme/test koşuları şeride özel `--artifacts-path /private/tmp/kuyumcu-serit/KATMAN-1b/j/dotnet/art` ve `KUYUMCU_TEST_DB=kuyumcu_test_t5` ile yapıldı (`j/dotnet` = kaynak dizinlerine sembolik bağ; testler `Kuyumcu.sln`'i buradan bulur, kaynak CSS/JS'i buradan okur). Proje ağacının `bin/` klasörüne hiç yazılmadı; API de bu kopyadan (`dotnet art/bin/Kuyumcu.Api/debug/Kuyumcu.Api.dll --urls http://localhost:5166`) koşuldu. Kanıt: ilk denemede ağaç içi `bin/Debug` DLL'i başka şeridin derlemesiyle ezilmiş, yeni testim (K1_22) koşumda görünmemişti (23 test); yalıtık kopyada 24 test koştu.

### 15.2 Doğrulayıcı bulgularının kapatılması
| Bulgu | Önem | Bu turda | Kanıt |
|---|---|---|---|
| 25.09 kiracısı `01M3CDR07QP3ENEQH60PEEMK67 PAKETKATMAN-1b-TEST Dogrulama` | DÜŞÜK | **KAPANDI** — bulgunun önerdiği komut koşuldu: `zsh betikler/kiraci-temizle.sh 01M3CDR07QP3ENEQH60PEEMK67 --uygula` (rapor modu önce: 150 satır + tenants 1; rc 0). Paketin adını taşıyan test kiracısı; QA-TEST-* değil; kurucu izni (satır silme serbest). | 70 tenant_id tablosu taraması: kalan **0**; `tenants` içinde kimlik 0 |
| katman.js `yerlestir`: görüş alanı dışı dalında `bagiCoz` yok (ölü kod) | DÜŞÜK | **KAPANDI** — `if (a.bottom < 0 \|\| a.top > g.h) { bagiCoz(katman); return false; }` + YENİ test `K1_22_bagli_oge_acicisi_gorus_alani_DISINA_cikinca_bag_COZULUR_eski_belirtec_kalmaz` (önce görünür açıcıya bağlanır, açıcı `top 1000 > 900` olunca `resize` → `data-katman`, `--katman-ust/-sol` silinir). Test düzeltmeden ÖNCE kırmızıydı (`t4/once3.log`: 1 başarısız / 24), düzeltmeden sonra yeşil. | M-K1b-11 |
| Tam süit paralel şerit kirliliği | DÜŞÜK | **KAPANDI (bu şerit için)** — tam süit yalıtık DLL + `kuyumcu_test_t5` ile koşuldu (15.4) | 1424 sahte kırmızı yerine 3 kırmızı, hepsi sahipli |
| Eşzamanlı derleme test DLL'ini siliyor | DÜŞÜK | **KAPANDI (bu şerit için)** — `--artifacts-path` (15.1). Öneri kapi.sh'a: şerit başına `--artifacts-path` + `KUYUMCU_TEST_DB` | — |
| HAKEM ÇELİŞKİSİ — denetim.sh | KRİTİK | **YENİDEN ÖLÇÜLDÜ, İKİ RAPOR AYNI GERÇEĞE BAĞLANDI**: iki sonuç FARKLI ZAMANLARDA doğruydu. 27.09 23:4x uygulayıcı koşumunda `P-39 TABLO_SARMALAYICISIZ 2` (PosTahsilatlar.razor, 22:31'de ZO-ISLEM-A değiştirmişti) ✗ idi; doğrulayıcının 28.09 00:04 koşumunda ZO-ISLEM-A düzeltmişti → TEMİZ. **Bugünkü ölçüm: 00:42 `DENETİM TEMİZ.` rc 0 (✗ sayısı 0) ve 01:48 `DENETİM TEMİZ.` rc 0** (`t4/denetim.log`, `t4/denetim2.log`); `TABLO_SARMALAYICISIZ 0`. | § 15.5 |
| HAKEM ÇELİŞKİSİ — ekran görüntüsü eksik kombinasyonlar | KRİTİK | **KAPANDI** — HER kombinasyon için gerçek ekran görüntüsü alındı: palet 7 rota × {BASİT,PRO} × {aydınlık,karanlık} × {1440,1024,375} = **84 görüntü** + 375 düğme ile 14 + yazılı 4; çekmece 4 çekmece × 12 kombinasyon = **48 görüntü**. İki kontak tabakası projeye kondu (15.6); tek tek görüntüler Read ile incelendi. | `ham/TUVAL-KARSILASTIRMA/KATMAN-1b-palet-tum-kombinasyon-2809.png`, `…-cekmece-tum-kombinasyon-2809.png` |

### 15.3 Bu turda bulunan YENİ kusur ve düzeltmesi (ölçüm sayesinde; gizlenmedi)
Tam ölçümde `#cekmece-urun` (/urunler "Düzenle") **{14 px ×1, 16 px ×9, 18 px ×8}** çıktı (27.09'da 18 px TEK ölçülmüştü). Tanı (`tani-urun.mjs`): girdiler `.girdi` 16 px, `.secici-tetik` 16 px, "Daha fazla" `.dugme-kucuk` 14 px. Kök neden: **OLCEK-1** şeridinin 28.09 00:26'da eklediği `olcek.css (0b)` kuralı `html[data-mod] :where(.ana-icerik) .girdi|.dugme|.secici-tetik|.dugme-kucuk { font-size: var(--yazi-kontrol[-kucuk]) }` — özgüllük (0,2,1), katman.css'ten SONRA yükleniyor; benim `.cekmece :is(…)` kuralım (0,2,0) yeniliyordu. `.ana-icerik` içinde çizilen çekmeceler (Ürün) etkilendi, dışındakiler (Kasa, Vitrin pano) etkilenmedi. Aynı neden tam turda seçici listelerindeki arama girdisini (`.secici-ara.girdi`) 16 px yapıyordu (/, /cariler, /islemler, /urunler liste kapları "16+18 px").
| Düzeltme (yalnız `css/katman.css`, dosya alanım) | Gerekçe |
|---|---|
| § 5 `html[data-mod] .cekmece :is(button, a, li, label, input, select, textarea, .dugme, .btn) { font-size: var(--katman-oge-boyu) }` (0,3,1) | Katman içinde TEK boy kazanır; sayfa gövdesindeki kontroller OLCEK-1 ölçeğinde kalır (olcek.css'e dokunulmadı). `#cekmece-cari` BASİT tuval kuralları kimlikli, daha özgül — değişmedi. |
| § 5 `html[data-mod] .cekmece .secici-tetik > .secici-metin { white-space: normal; text-overflow: clip }` | 18 px'e çıkınca yarım sütundaki `#d-ayar` değeri kesildi ("K22 — 22 Ayar (916…", 184 > 176 px, `tani-ayar.mjs`). Kesmek yerine ikinci satıra iner; ölçüm sonrası kesik 0. |
| § 3 `html[data-mod] .secici-liste-kap .secici-ara` | Liste içindeki arama girdisi 18 px (tani-liste.mjs: sonrasında 18 px dışı öğe 0). |
Testler: K1_21 (çekmece seçicisi `html[data-mod] .cekmece :is(` ile başlar + sarma kuralı), K1_04 (liste arama girdisi). OLCEK/Dugme/TasarimBelirtec/P39 testleri dahil ilgili 330 test yeşil (`t4/filt5.log`).

### 15.4 Derleme ve testler
- `dotnet build Kuyumcu.sln --artifacts-path …` son (mutasyonlardan sonra): **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145) xUnit1031` — başka şeridin dosyası) (`t4/build-final.log`).
- Filtreli (KatmanTekKalip 25 + K1_Palet + P17Vitrin + KabukArama): **63/63**; OLCEK/Dugme/TasarimBelirtec/P39 ile birlikte **330/330**.
- Testler: önce (27.09) KatmanTekKalip 23 → sonra **25** (yeni K1_22; K1_18 Theory ×2 dahil); değişen K1_04, K1_21. Yeni test sayısı bu tur: 1.
- **Tam süit** (yalıtık DLL, `kuyumcu_test_t5`): `trx-pKATMAN-1b/tur4b/_Mac_2026-09-28_01_41_43.trx` → **6444 · 6441 Başarılı · 3 Başarısız**:
  1. `P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` → `dotnet/denetim.sh.mutbak` (18.09, bu paketin değil; dosya silme yasak) — **orkestratör/kurucu**.
  2. `UX4cEkranTestleri.M10_Liste_sunucu_sinirina_degince_DARALTIN_uyarisi_basilir` (Pages/Emanet `#e-cari-sinir`; yalıtık tekrar koşumda da kırmızı 1/27) — **EMANET-1** (testin kendi yorumu "EMANET-1 (doğrulayıcı 28.09)").
  3. `MadenDefteriTestleri.GeriDoldurmaMigrationiUrunAyarindanTuretir` — ORTAM: test `Kuyumcu.Api/Data/Migrations`'ı DLL'in üst klasörlerinde arıyor, yalıtık `art/bin` düzeninde bulamıyor (`DirectoryNotFoundException …/art/bin/Kuyumcu.Api/Data/Migrations`). Ağaç içi DLL ile aynı test **1/1 Başarılı**. Kod kusuru değil.
  Bir önceki tam koşum (00:41, `tur4/_Mac_2026-09-28_00_41_56.trx`): 6420 · 7 kırmızı (üstteki 1 ve 3 + `ZOBELGESinirTestleri` ZOBS05/ZOBS13 — ZO-BELGE; `S24EskiAltin` ×2 ve `SATIS3…Barkod_dizisi…` — SATIS-3); ikinci koşumda o şeritler kapatmış.
  → **Bu paketin yeni kırmızısı 0** (paket sınıflarında kırmızı 0; kırmızıların hiçbiri katman.js/css, GlobalArama, Vitrin dosyalarına dayanmıyor).

### 15.5 denetim.sh
`bash dotnet/denetim.sh` 00:42 ve 01:48 → **`DENETİM TEMİZ.` rc 0, ✗ 0.** Sayaçlarım: `index.html satır içi <script> 0`, `ModalPencere 48 (tavan 55)`, `SABIT_RENK 0`, `SECICI_TEK_KAYNAK 1`, `P-39 TABLO_SARMALAYICISIZ 0`.

### 15.6 Mutasyon kanıtları (yedek `/private/tmp/kuyumcu-serit/mut-yedek/KATMAN-1b-t4/`; `perl -0777 -pi` tek eşleşme → yalıtık DLL ile filtreli test → yedek geri → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| **M-K1b-1** | `yerlestir`: `if (!gorunurKutu(a)) { bagiCoz(katman); return false; }` silindi — **0×0 açıcı koruması** | K1_18(oge), K1_18(popover), K1_19, K1_20 (4/25) | AYNI |
| M-K1b-2 | `beforetoggle` `if (!gorunurKutu(r)) return;` silindi | K1_18(popover) | AYNI |
| M-K1b-3 | `bagiCoz` `removeAttribute('data-katman')` silindi | K1_19, K1_22 | AYNI |
| M-K1b-4 | `a.width > 0 && a.height > 0` → `\|\|` | K1_20 | AYNI |
| M-K1b-5 | 480 px palet `width: calc(100vw − 2*pay)` silindi | K1_03, K1_21 | AYNI |
| M-K1b-6 | çekmece `font-size: var(--katman-oge-boyu)` → `var(--yazi-md)` | K1_21 | AYNI |
| M-K1b-7 | `.kayit-arama-palet[data-katman="bagli"] { top: var(--katman-ust) }` geri eklendi | K1_21 | AYNI |
| M-K1b-8 (derlemeli) | GlobalArama'ya `data-katman-acici="ust-kayit-ara"` | K1_Palet_BAGLI_DEGIL_perdeli_komut_paleti_aria_modal | AYNI |
| M-K1b-10 (derlemeli) | Vitrin pano çekmecesine `<label for="pano-yuvarlama">` geri | P17VitrinEkranTestleri.KATMAN1b_pano_cekmecesinde_yuvarlama_etiketi_TEK_kez | AYNI |
| **M-K1b-11** (YENİ) | görüş alanı dışı dalında `bagiCoz` kaldırıldı (eski ölü kod) | K1_22_bagli_oge_acicisi_gorus_alani_DISINA_cikinca_bag_COZULUR_eski_belirtec_kalmaz | AYNI |
| **M-K1b-12** (YENİ) | çekmece seçicisi `html[data-mod] .cekmece :is(` → `.cekmece :is(` (OLCEK-1'e yenilen eski özgüllük) | K1_21 | AYNI |
| **M-K1b-13** (YENİ) | çekmece seçici değerinde `white-space: normal` silindi | K1_21 | AYNI |
| **M-K1b-14** (YENİ) | `html[data-mod] .secici-liste-kap .secici-ara` → `.secici-liste-kap .secici-ara` | K1_04_oge_yazi_boyu_TEK_belirtec_ve_govde_kademesine_bagli | AYNI |
M-K1b-8 ve M-K1b-10 tek derlemede birlikte uygulandı (63 testte 2 kırmızı, her biri kendi dosyasını sınıyor). Son mutasyondan sonra temiz çözüm derlemesi 0 Hata (15.4).

### 15.7 Canlı ölçüm (başsız Chrome `betikler/tur/cdp.mjs`; kendi kiracım `PAKETKATMAN-1b-TEST Olcum4` 01M3JD8E9AXF00648K1T3DX4AW — davet satırı + `/auth/kayit` **201**; tohum: 2 kasa **200/200**, 6 cari **201**×6, 3 ürün **201**×3 (ayar K22/K14 — ilk denemede `/ayarlar` **404** "Uç bulunamadı" döndü, ayar kodları `/urunler` 400 yanıtının kabul listesinden okundu), `/pano/guncel` **200**, `/kur/guncel` **200**). BASİT ve PRO SIRAYLA koşuldu (paralel koşum 27.09'da stil sapması üretmişti); her palet kaydında yüklü stil sayfası denetimi: katman.css **196/196** (olc4), **112/112** (olc6).

**Palet** (`t4/olc4.mjs`, 244 kayıt, hata 0): 7 rota × {BASİT, PRO} × {aydınlık, karanlık}; Ctrl+K = CDP `Input.dispatchKeyEvent` (ctrl + KeyK)
| Genişlik | Yol | n | kutu [sol, üst, en] | merkez farkı | data-katman | ekran dışı | perde tam | en üstte | yazı | aile | küçük | yatay | açıcı | aria-modal | mod uyuşmazlığı |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | Ctrl+K / + "a" | 28 / 28 | [432, 90, 576] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | Manrope | 0 | 0 | 0×0 | true | 0 |
| 1024 | Ctrl+K / + "a" | 28 / 28 | [224, 77, 576] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | Manrope | 0 | 0 | 0×0 | true | 0 |
| 375 | Ctrl+K / + "a" / düğme | 28 ×3 | [8, 8, 359] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | Manrope | 0 | 0 | 44×44 | true | 0 |
→ 0×0 açıcıda palet BAĞLI DEĞİL; sol üst köşeye yapışma 0/196.

**Çekmeceler** (`olc6.mjs` 4 rota + `olc7.mjs` /urunler sarma düzeltmesinden sonra; {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1024, 375}; açılmayan 0/48):
| Çekmece | BASİT öğe yazısı | PRO öğe yazısı | "Yuvarlama adımı" | iç taşma | yatay |
|---|---|---|---|---|---|
| #cekmece-urun | **18 px TEK** (18 öğe, 6/6) — düzeltmeden önce 14/16/18 | **18 px TEK** (64 öğe, 6/6) | — | 0 | 0 |
| #cekmece-kasa | 18 px TEK (9) | 18 px TEK (9) | — | 0 | 0 |
| #cekmece-pano-kalem | 18 px TEK (16) | 18 px TEK (16) | **1** (12/12) | 0 | 0 |
| #cekmece-cari | 15/16/17/18 px — tuval ölçüsü (CUMA-M; § 3.3, açık karar) | **18 px TEK** (40, 6/6 — 27.09'da açılamamıştı, bu tur `c-duzenle-*` ile açıldı) | — | 0 | 0 |

**Tam tur** (`tur.mjs` 7 rota × {BASİT,PRO} × {aydınlık,karanlık} × {1440,1024,375} + `analiz.py`; `t4/tur2/`, düzeltmelerden SONRA): kayıt 854 · sayfa 84 · katman 770 (açılan 562) · hata 0 · geçersiz 0.
| Ölçü | Sonuç | Açıklama |
|---|---|---|
| SERBEST-YÜZEN (modal olmayan) | **0** | 28 kaydın 28'i `#kabuk-arama-palet` 375 (aria-modal + perde) — kabul gereği modal |
| küçük yazı (<14 px) | **0** | — |
| bağlı-açılır | 18 px TEK ×210; 14/16 ×18 | 18'in hepsi /islemler sayfa içi bölümleri `#islem-aciklama-sar` (12), `#islem-pro-odeme` (6) — SATIS-3/CUMA-S, önceki turlarda da vardı. Düzeltmeden önceki turda (`t4/tur/`) 16+18 px olan 52 seçici listesi kaydı **0'a indi**. |
| çekmece / tam-ekran | 18 px TEK ×176 / ×88; karışık 28 / 14 | karışıkların HEPSİ `/cariler` BASİT `#cekmece-cari` (tuval ölçüsü) |
| ekran dışı | 4 | hepsi `#islem-pro-odeme` PRO 1024/375 — SATIS-3/CUMA-S |
| sayfa kaydı > 40 px | 0 | — |
| yatay · K1 · K5 'TRY' · K3 taşan · K9 · K12 · K13 | 0 · 0 · 0 · 0 · 0 · 0 · 0 | — |
| K sayfa bulguları | 2 | `/urunler K10 "Tüm kategoriler"` (PRO 1440/1024), `/vitrin K3 #vitrin-kur-kaynagi` (1024) — tabanda da vardı, sayfa sahiplerinin (27.09'daki `/cariler K3 th` bu turda YOK) |

### 15.8 Görsel kanıt (Read ile bakıldı) — `ham/TUVAL-KARSILASTIRMA/`
- `KATMAN-1b-palet-tum-kombinasyon-2809.png` — 84 palet görüntüsü (7 rota × 2 mod × 2 tema × 3 genişlik), her karede dosya adı.
- `KATMAN-1b-cekmece-tum-kombinasyon-2809.png` — 48 çekmece görüntüsü (4 çekmece × 12 kombinasyon; Ürün çekmecesi sarma düzeltmesinden sonraki çekimle).
- `KATMAN-1b-Profesyonel-palet-1440-2809.png` (tuval Profesyonel ↔ /islemler PRO Ctrl+K), `KATMAN-1b-Musteriler-palet-1440-2809.png` (tuval Musteriler ↔ /cariler BASİT Ctrl+K).
- `KATMAN-1b-cekmece-urun-BASIT-karanlik-1440-2809.png`, `…-PRO-karanlik-1440-2809.png` — Ürün çekmecesi 18 px, "K22 — 22 Ayar (916‰)" kesilmeden iki satırda.
Tek tek bakılanlar: `palet_cariler-BASIT-aydinlik-1440-ctrlK` (üst orta, perde), `palet_islemler-PRO-karanlik-1024-ctrlK` (üst orta, koyu perde), `cekmece_vitrin-BASIT-aydinlik-1440` ve `-PRO-karanlik-375` ("Yuvarlama adımı" TEK, 18 px, × çerçeveli), `cekmece_urunler-BASIT-aydinlik-1440` (düzeltmeden önce kesik "916…" — kusuru bu görüntü gösterdi), `cekmece_urunler-PRO-karanlik-375`, `cekmece_urunler-BASIT-karanlik-1440` (sonra).
Bölge tablosu § 6 ile aynı (menü "Ekran ara Ctrl K" AYNI; üst bar arama düğmesi masaüstünde gizli AYNI; palet tuvalde çizilmemiş → GEREKÇELİ FARK: özellik silinmez, konum devir notu kararı; sayfa gövdesi kapsam dışı).

### 15.9 Temizlik
- API (PID 87525) ve vekil `kill`; `lsof` 5166/5167 boş; başsız Chrome'ların hepsi kapandı (yarıda kestiğim olc5 koşumunun 9507 tarayıcısı elle `kill`).
- `kiraci-temizle.sh 01M3JD8E9AXF00648K1T3DX4AW --uygula` (1058 satır + tenants 1), API DURDURULDUKTAN SONRA; davet satırı `PKATMAN1B-DAVET-2809` silindi (ROW_COUNT 1). 70 tenant_id tablosunda iki kimlik (benim + 25.09 kiracısı) için kalan **0**, `tenants` içinde **0**.
- tenants: başta **19**, sonda **17**. Hesap: −1 25.09 kiracısı (bulgu gereği), +1/−1 benim kiracım, −1 başka şeritler NET (bu süre içinde `PAKETZO-ISLEM-A-TEST`, `PAKET-OLCEK-1E-TEST`, `PAKET-ORTAK-1-TEST2` vb. silindi, `PAKETEMANET-1-TEST T28`, `PAKETZO-ISLEM-A-HAKEM-TEST`, yeni `PAKETZO-BELGE-TEST` eklendi — hiçbirine dokunmadım). QA-TEST-* dokunulmadı.
- Parola/jeton içeren geçici dosyalar (`t4/gizli.json`, `t4/fikstur.json`, chmod 600) `printf '' >` ile boşaltıldı; proje ağacına geçici dosya yazılmadı.

### 15.10 Değişen dosyalar (bu turda)
```
?? dotnet/Kuyumcu.Web/wwwroot/js/katman.js     (28.09: görüş alanı dışı dalında bagiCoz)
?? dotnet/Kuyumcu.Web/wwwroot/css/katman.css   (28.09: § 5 html[data-mod] özgüllüğü + seçici değeri sarma; § 3 liste arama girdisi)
   dotnet/Kuyumcu.Tests/KatmanTekKalipTestleri.cs (28.09: K1_22 yeni; K1_04, K1_21 güncel; Gorunmez yardımcısına "disarida" kipi)
   dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/KATMAN-1b-*-2809.png (6 yeni görüntü)
   dokumanlar/09-uygulama-kayitlari/PAKET-KATMAN-1b-uygulama-raporu.md
```
`GlobalArama.razor`, `Vitrin.razor` bu turda değişmedi (27.09 hâli, `cmp` AYNI). **Dokunulan sayfalar (TEK DOKUNUŞ): bu turda yok.** app.css, olcek.css, Sozlukler, denetim.sh değişmedi. Migration gerekmedi. COMMIT YOK.

### 15.11 Paket kapısı (28.09)
| # | Durum | Kanıt |
|---|---|---|
| 1 build | ✓ | 0 Hata (1 yabancı uyarı) |
| 2 testlerim | ✓ | 63/63 filtreli (+ ilgili 330/330); tam süitte paket sınıflarında 0 kırmızı |
| 3 sayaçlarım | ✓ | denetim.sh TEMİZ (tam ağaç, ✗ 0) |
| 4 mutasyon | ✓ | 13 mutasyon, her biri adı yazılı testte kırmızı, hepsi cmp AYNI |
| 5 canlı | ✓ | 244 palet/çekmece + 40 ürün çekmecesi + 854 tur kaydı; 84 + 48 ekran görüntüsü |
| 6 kiracı silindi | ✓ | kendi kiracım + 25.09 artığı: kalan satır 0 |
| 7 yeni kırmızı yok | ✓ | tam süitte 3 kırmızı: EMANET-1 (1), orkestratör mutbak (1), ortam/yalıtık düzen (1) |

### 15.12 Açık kalanlar (sahibiyle)
1. `dotnet/denetim.sh.mutbak` (P37c kırmızısı) ve `Pages/Islemler.Sade.cs.tmp` ağaçta — dosya silme yasak → **orkestratör/kurucu**.
2. `UX4cEkranTestleri.M10_Liste_sunucu_sinirina…` — **EMANET-1**.
3. `MadenDefteriTestleri.GeriDoldurmaMigrationi…` yalıtık `--artifacts-path` düzeninde Migrations klasörünü bulamıyor (ağaç içinde yeşil) — kapi.sh'a şerit başına artifacts-path eklenecekse test kök bulma yolu `Kuyumcu.sln` aramasına çevrilmeli → **orkestratör** (test dosyası bu paketin değil).
4. `#cekmece-cari` BASİT 15/16/17 px (tuval ölçüsü) ↔ "tek yazı boyu" — **orkestratör/kurucu kararı**.
5. `#islem-pro-odeme` ekran dışı (PRO 1024/375), `#islem-aciklama-sar`/`#islem-pro-odeme` 14/16 px — **SATIS-3/CUMA-S**.
6. `/urunler K10 "Tüm kategoriler"`, `/vitrin K3 #vitrin-kur-kaynagi` (1024) — sayfa sahipleri.
7. **OLCEK-1 ile sınır notu:** olcek.css (0b) kontrol ölçeği katman içinde artık katman.css'e yenilir (bilinçli: katman öğe yazısı TEK). OLCEK-1 katmanlar için de kontrol ölçeği isterse iki şerit tek belirteçte (`--katman-oge-boyu` ↔ `--yazi-kontrol`) uzlaşmalı → **orkestratör**.
8. Ürün çekmecesinde uzun seçici değeri iki satıra iniyor (kesilmek yerine); tuvalde bu çekmece yok. Daha geniş sütun istenirse Ürünler sayfa sahibi/ORTAK-1 (Cekmece genişliği) kararı.
9. `/api/v1/ayarlar` 404 (tohum betiğinde varsayılan uç yoktu) — bilgi; ayar kodu listesi yalnız doğrulama iletisinden okunabiliyor.
10. Canlı API günlüğünde iş zamanlayıcısının `is_calismalari` benzersiz anahtar çakışması (`Duplicate entry …-MASAK_SIB_SURESI-2026-09-28`) — birden çok şerit API'si aynı kuyumcu_net'te aynı günlük işi koşuyor; ortam, bu paketin değil → **orkestratör**.

## 16. 29.09 YENİDEN DOĞRULAMA + KIYAS DEVRİ (22:20–23:00; Ertuğ + Taşkın rolü, t5: API 5166 · vekil 5167 · `kuyumcu_test_t5` · CDP 9511–9514) — EN GÜNCEL KANIT

### 16.1 Ortam ve ağaç (hakemin KRİTİK bulgusu: "Docker çöktü, canlı doğrulanamadı")
- Bu turda MySQL AYAKTA: `nc -z localhost 3306` → açık; `docker exec kuyumcu-mysql …` sorguları yanıt verdi; API 5166 kuyumcu_net'e bağlandı (kayıt 201, tohum 200/201). Yani 28.09'daki "DB'ye bağlı adımlar koşulamadı" açığı bu turda **gerçek DB ve gerçek uygulama** ile kapatıldı (aşağıda tam süit, denetim.sh P-05 CANLI ŞEMA, tenants sayımı, canlı ölçüm).
- Ağaçtaki iş KORUNDU: `katman.js` (gorunurKutu/bagiCoz, satır 87–107, beforetoggle koruması 150), `katman.css` (§ 3–5), `GlobalArama.razor`, `Vitrin.razor` yeniden yazılmadı. `/tmp` yeniden başlatmada silindiği için 28.09 mutasyon yedekleri yok; yarım mutasyon denetimi bunun yerine şöyle yapıldı: bütün paket testleri yeşil (16.3) ve her mutasyon kodu yeni yedekle yeniden uygulanıp geri alındı (16.4) → ağaçta yarım mutasyon YOK.
- 28.09 13:49'daki kesilen koşumun (`KATMAN-1b/t5/build*.log`) kod bıraktığına dair iz yok (yalnız derleme günlükleri).

### 16.2 Bu turda yapılan kod değişikliği — KIYAS 28.09 "Şeritlere devir → KATMAN" satırları
| KIYAS satırı | Durum | Değişiklik |
|---|---|---|
| `KenarCubugu.razor:494-495` aktif rotanın grubu kayıtlı katlama tercihinden bağımsız açık olmalı (YÜKSEK) | **KAPANDI** | `GrupAcik`: aktif grup ⇒ açık (bu ziyarette elle katlanmadıkça); öteki gruplar ⇒ `Tercih.GrupAcik(slug) ?? false`. Aktif grubun başlığına basmak yalnız o ziyaret için katlar (`aktifKatlanan`, `KonumDegisti`'nde sıfırlanır) ve kalıcı tercihe YAZMAZ. Tohumlama (`AcikOlanlar`) aktif grubu artık İÇERMEZ — kök neden buydu: /kasalar'da ilk katlamada `["para-banka"]` tohumlanıp her sayfada açık kalıyordu. Düğme her durumda görünür bir etki üretir (açılır/kapanır) — "tıklanınca hiçbir şey olmayan düğme" yok. |
| Kenar menü 1280×900 ölçümü: `#kenar-nav` kayıyor mu, son gruplar erişilebilir mi, kullanıcı kartı örtüyor mu | **ÖLÇÜLDÜ, KUSUR YOK** | 16.6: `overflow-y:auto`; uzun grupta (/kasalar BASİT 794>626, /kullanicilar 700>626) kayıyor; en alta kaydırınca son grup (`#kenar-grup-dugme-yonetim`) görünür ve `elementFromPoint` ile tıklanabilir 16/16; kart üstü 818 ≥ nav altı 806 → örtme 0/16; yatay 0. Kodda değişiklik gerekmedi. |
| `Kasalar.razor:116, :121, :139, :147` emojiler → `<Ikon>` (ORTA) | **KAPANDI** | ♻ Aktife al → `Ikonlar.Onay`, 🌙 Günü Kapat → `Ikonlar.Saat`, ✏️ Düzenle → `Ikonlar.Kalem`, 🗑 Pasife al → `Ikonlar.Carpi` (UX-5 ince çizgi sprite; yeni ikon/sözlük metni yok, id'ler değişmedi). Görüntü: `olc/foto/cekmece_kasalar-BASIT-aydinlik-1440.png` ("Günü Kapat" saat ikonuyla). |
KIYAS'ın öteki satırları (ZO-BELGE Raporlar, ORTAK-1 çift başlık/Panel/KarZarar, sahipsiz TEMA-TEK/URUN-SADE/BICIM-TEK vb.) alan dışı — DOKUNULMADI.

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Kasalar.razor` (gerekçe: KIYAS devri, KATMAN dosya alanındaki sayfa; yalnız 4 düğmenin ikon işaretlemesi) · `Layout/KenarCubugu.razor` (KATMAN alanı; yalnız grup açıklık kuralı). app.css, olcek.css, Sozlukler, denetim.sh DEĞİŞMEDİ. Migration gerekmedi. COMMIT YOK.

### 16.3 Derleme ve testler (yalıtık: `dotnet build …/Kuyumcu.sln --artifacts-path /private/tmp/kuyumcu-serit/KATMAN-1b/j/dotnet/art4`, `KUYUMCU_TEST_DB=kuyumcu_test_t5`)
- Not: sembolik bağlı `j/dotnet/Kuyumcu.sln` üzerinden derleme alt klasör .cs dosyalarını görmedi (132 CS0246 — `Fis/`, `Yazdirma/` türleri; `t6/build.log`); gerçek yol `Kuyumcu.sln` + `--artifacts-path j/dotnet/art4` ile derlendi (28.09 13:49'daki `t5/build3.log` ile aynı çözüm).
- `dotnet build` (son, mutasyonlardan sonra): **0 Uyarı, 0 Hata** (`t6/build-son.log`).
- Filtreli paket + komşu sınıflar (KatmanTekKalip, KabukArama, MenuKatlama, P17Vitrin, S1gMenuTuval, S1jMenuArama, TK2KabukTuval, TercihKatlama, Kasa*, IkonTestleri, ErisimKapatma, Olcek, Dugme, TasarimBelirtec, P39): **752/752** (ilk koşumdaki tek kırmızı `Guvenlik_basliklarinin_TEK_KAYNAGI_Program_cs_tir` yalıtık kökte `../deploy` yoktu → `j/deploy` bağı eklendi, 1/1 yeşil).
- Yeni testler (5): `MenuKatlamaTestleri.KIYAS2a_tercih_para_banka_iken_faturalarda_BELGE_UYUM_acik_para_banka_tercihle_acik`, `…KIYAS2a_aktif_grup_bu_ziyarette_katlanir_TERCIHE_YAZILMAZ_rota_degisince_yine_acik`, `…KIYAS2a_oteki_grubu_katlayinca_AKTIF_grup_TOHUMA_girmez`, `KatmanTekKalipTestleri.K1_23_KIYAS5_Kasalar_satir_eylemlerinde_EMOJI_YOK_ince_cizgi_Ikon_var`; değişen 1: `Kayitli_tercih_VARSAYILANI_ezer` → `Kayitli_tercih_OTEKI_gruplari_yonetir_AKTIF_grup_yine_ACIK` (eski kuralı — "tercih aktif grubu da kapatır" — belgeleyen test; KIYAS 2a o kuralı kusur saydı).
- **Tam süit** (`trx-pKATMAN-1b/t6/_Mac_2026-09-29_22_44_47.trx`): 7361 · 7230 Başarılı · **131 Başarısız**. Ayrıştırma:
  - **129 = yalıtık düzen artığı**: testler depo kökündeki `deploy/`, `dokumanlar/`, betikler, `kurulum.sh`, `yayin/` vb. dosyaları `Kuyumcu.sln`'in üstünde arıyor; yalıtık kök `j/` yalnız `dotnet/` taşıyordu. `j/` altına depo kökünün bütün girişleri sembolik bağlandı, başarısız 19 sınıf yeniden koşuldu (`trx-pKATMAN-1b/t6/tekrar.trx`): **228 → 226 Başarılı, 2 Başarısız**.
  - `MadenDefteriTestleri.GeriDoldurmaMigrationiUrunAyarindanTuretir` — yalıtık `art4/bin/Kuyumcu.Api/Data/Migrations` yok; `Data` bağı eklenince **1/1 Başarılı** (ortam, kod kusuru değil; § 15.12-3 ile aynı).
  - `Dogrulayici.ZOBELGEFistenOnizlemeSinirTestleri.DZB4_04_alis_fisi_kurus_0_005_ekranda_AwayFromZero_0_01` — beklenen `"0,01 ₺"` ile gerçek `"0,01 ₺"` arasında boşluk karakteri farkı (NBSP↔boşluk); test dosyası `ZOBELGESinirTestleri.cs` 22:44:50'de (koşum sırasında) ZO-BELGE tarafından değiştirildi → **ZO-BELGE**, bu paketin dosyası değil.
  → **Bu paketin yeni kırmızısı 0.**

### 16.4 Mutasyon kanıtları (yedek `/private/tmp/kuyumcu-serit/mut-yedek/KATMAN-1b-t6/`; betik `t6/mut.sh`: yedek → `perl -Mutf8 -0777 -pi` tek eşleşme → (razor ise yalıtık derleme) → filtreli test → yedek geri → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| **M-K1b-1** | `yerlestir`: `if (!gorunurKutu(a)) { bagiCoz(katman); return false; }` silindi — **0×0 açıcı koruması (kabulün istediği)** | K1_18(oge), K1_18(popover), K1_19, K1_20 | AYNI |
| M-K1b-2 | beforetoggle `if (!gorunurKutu(r)) return;` silindi | K1_18(popover) | AYNI |
| M-K1b-6 | çekmece font-size `var(--katman-oge-boyu)` → `var(--yazi-md)` | K1_21 | AYNI |
| M-K1b-7 | `.kayit-arama-palet[data-katman="bagli"] { top: var(--katman-ust) }` geri eklendi | K1_21 | AYNI |
| M-K1b-8 (derlemeli) | GlobalArama paletine `data-katman-acici="ust-kayit-ara"` | KabukAramaEkranTestleri.K1_Palet_BAGLI_DEGIL_perdeli_komut_paleti_aria_modal | AYNI |
| M-K1b-10 (derlemeli) | Vitrin pano çekmecesine ikinci `<label for="pano-yuvarlama">` | P17VitrinEkranTestleri.KATMAN1b_pano_cekmecesinde_yuvarlama_etiketi_TEK_kez | AYNI |
| M-K1b-11 | görüş alanı dışı dalında `bagiCoz` kaldırıldı | K1_22_bagli_oge_acicisi_gorus_alani_DISINA_cikinca_bag_COZULUR_eski_belirtec_kalmaz | AYNI |
| M-K1b-12 | `html[data-mod] .cekmece :is(` → `.cekmece :is(` | K1_21 | AYNI |
| M-K1b-13 | çekmece seçici değerinde `white-space: normal` silindi | K1_21 | AYNI |
| M-K1b-14 | `html[data-mod] .secici-liste-kap .secici-ara` özgüllüğü düşürüldü | K1_04_oge_yazi_boyu_TEK_belirtec_ve_govde_kademesine_bagli | AYNI |
| **M-K1b-15** (YENİ, derlemeli) | `GrupAcik` eski kurala (`Tercih.GrupAcik(slug) ?? slug == AktifGrupSlug()`) döndürüldü | MenuKatlama: KIYAS2a_tercih_para_banka…, KIYAS2a_aktif_grup_bu_ziyarette…, Kayitli_tercih_OTEKI_gruplari…, Acik_gruba_basinca_KAPANIR… | AYNI |
| **M-K1b-16** (YENİ, derlemeli) | `KonumDegisti`'nde `aktifKatlanan = null` silindi | KIYAS2a_aktif_grup_bu_ziyarette_katlanir_TERCIHE_YAZILMAZ_rota_degisince_yine_acik | AYNI |
| **M-K1b-17** (YENİ, derlemeli) | tohum yine aktif grubu içeriyor (`.Where(GrupAcik)`) | KIYAS2a_oteki_grubu_katlayinca_AKTIF_grup_TOHUMA_girmez | AYNI |
| **M-K1b-18** (YENİ) | Kasalar "Günü Kapat"a 🌙 geri kondu | K1_23_KIYAS5_Kasalar_satir_eylemlerinde_EMOJI_YOK_ince_cizgi_Ikon_var | AYNI |
Hiçbir mutasyon "kırmızısız" kalmadı. M-K1b-10'un ilk denemesi yanlış sözlük anahtarıyla derlenmedi (1 Hata) — o koşum sayılmadı, doğru anahtarla tekrarlandı. Son mutasyondan sonra altı dosyanın hepsi yedekle `cmp` AYNI; temiz çözüm derlemesi 0 Uyarı 0 Hata.

### 16.5 denetim.sh (tam ağaç, canlı MySQL)
`bash dotnet/denetim.sh` 22:5x → **`DENETİM TEMİZ.` rc=0, ✗ sayısı 0** (`t6/denetim.log`). **P-05 CANLI ŞEMA adımı bu kez TAMAMLANDI**: `DOGAL_ANAHTAR_KISITSIZ 0`, `BENZERSIZLIK_SOZLUKSUZ 0`, `CANLI_KISITSIZ 0`, `INDEKS_IFADESIZ 0`. Sayaçlarım: `index.html satır içi <script> 0`, `ModalPencere 48 (tavan 55)`, `SABIT_RENK 0`, `SECICI_TEK_KAYNAK 1`. (28.09 hakem çelişkisi: iki rapor farklı ortamı anlatıyordu; bu turda komut canlı DB ile uçtan uca bitti.)

### 16.6 Canlı ölçüm — GERÇEK uygulama (API `Kuyumcu.Api.dll` 5166 → kuyumcu_net; vekil 5167; başsız Chrome `betikler/tur/cdp.mjs`)
Kiracı `PAKETKATMAN-1b-TEST Olcum6` (01M3QB7GDWDSWB333ZB83W1GDZ): davet satırı (`kiraci_davetleri`, not `PKATMAN1B-DAVET-2909`) + `/auth/kayit` **201**; tohum: `/subeler` 200, 2 kasa **200/200**, 6 cari **201**×6, 3 ürün **201**×3, `/pano/guncel` 200, `/kur/guncel` 200. BASİT ve PRO SIRAYLA (betik `t6/olc.mjs`, analiz `t6/analiz.py`).

**Palet** (112 kayıt, açılmayan 0, mod uyuşmazlığı 0; 7 rota × {BASİT,PRO} × {aydınlık,karanlık}; Ctrl+K = CDP `Input.dispatchKeyEvent` ctrl+KeyK; 375'te ayrıca düğme)
| Genişlik | Yol | n | kutu [sol,üst,en] | merkez farkı | data-katman | ekran dışı | perde tam | en üstte | öğe yazısı | başlık/durum | aile | yatay | açıcı | odak | aria-modal |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | Ctrl+K | 28 | [432, 90, 576] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | 16 px | Manrope | 0 | **0×0** | #kabuk-arama-girdi | true |
| 1024 | Ctrl+K | 28 | [224, 77, 576] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | 16 px | Manrope | 0 | 0×0 | girdi | true |
| 375 | Ctrl+K | 28 | [8, 8, 359] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | 16 px | Manrope | 0 | 44×44 | girdi | true |
| 375 | düğme | 28 | [8, 8, 359] | 0 | yok | 0 | 28/28 | 28/28 | 18 px | 16 px | Manrope | 0 | 44×44 | girdi | true |
→ 0×0 açıcıda palet BAĞLI DEĞİL; sol üst köşeye yapışma **0/112**. Öğeler (girdi, düğme, sonuç) TEK 18 px. 16 px olan yalnız başlık `#kabuk-arama-palet-baslik` (app.css `.kayit-arama-baslik` `--yazi-md`) ve durum satırı `#kabuk-arama-durum` (`.metin-kucuk` → katman.css `--yazi-md`): § 5 kuralı gereği başlık/ipucu öğe değildir, kendi kademesinde; <14 px değil (küçük yazı 0). 28.09 tablosundaki "18 px" yalnız öğeleri sayıyordu; bu turun betiği span/başlığı da saydığı için ayrı sütunda gösterildi.

**Çekmeceler** (48 ölçüm, açılmayan 0; {BASİT,PRO} × {aydınlık,karanlık} × {1440,1024,375})
| Çekmece | BASİT öğe yazısı | PRO öğe yazısı | "Yuvarlama adımı" | iç taşma | yatay |
|---|---|---|---|---|---|
| #cekmece-urun | 18 px TEK (108) | 18 px TEK (384) | — | 0 | 0 |
| #cekmece-kasa | 18 px TEK (54) | 18 px TEK (54) | — | 0 | 0 |
| #cekmece-pano-kalem (Vitrin) | 18 px TEK (96) | 18 px TEK (96) | **1** (12/12) | 0 | 0 |
| #cekmece-cari | 18 px ×48 · 15 px ×18 (alan etiketleri) | 18 px ×138 · **15 px ×102 (alan etiketleri)** | — | 0 | 0 |
**YENİ BULGU (gizlenmedi, alan dışı):** 28.09'da PRO'da 18 px TEK olan müşteri çekmecesinde alan etiketleri artık 15 px. Kök neden (tanı `t6/tani2.mjs`, uygulanan kurallar): `olcek.css` → `html[data-mod] .musteri-panel-kap #cekmece-cari .alan-etiket { font-size: var(--yazi-etiket) }` — kimlikli, katman.css'in genel çekmece kuralından özgül; iki modda da uygulanıyor. Dosya **OLCEK-1**'in, kural `#cekmece-cari`'ye bilinçli yazılmış; açık kararla (§ 15.12-4, "tuval ölçüsü ↔ tek yazı boyu") aynı çelişkinin PRO'ya genişlemesi. Katman.css'te kimlikli bir ezme yazmak OLCEK-1'in kararını sessizce ezerdi → dokunulmadı, **orkestratör/kurucu kararı** (16.9). 15 px < 14 px değil (küçük yazı 0).

**Kenar menü 1280×900** (KIYAS 2b; 16 ölçüm: 4 rota × 2 mod × 2 tema)
| Rota | Mod | kayıyor (scrollH>clientH) | açık gruplar | son grup görünür/tıklanır (en alta kaydırınca) | kart listeyi örter | yatay |
|---|---|---|---|---|---|---|
| /kasalar | BASİT | ✓ (794>626) | para-banka | ✓/✓ | hayır (818 ≥ 806) | 0 |
| /faturalar | BASİT | hayır (626) | belge-uyum | ✓/✓ | hayır | 0 |
| /raporlar | BASİT | hayır | rapor | ✓/✓ | hayır | 0 |
| /kullanicilar | BASİT | ✓ (700>626) | yonetim | ✓/✓ | hayır | 0 |
| aynı 4 rota | PRO | hayır (PRO satırları kısa) | aktif grup | ✓/✓ | hayır | 0 |
Karanlık tema aynı sonuç (16/16). `overflow-y: auto` her durumda.

**KIYAS 2a canlı senaryo** (`t6/tani3.mjs`, PRO, 1280×900, gerçek sunucu tercihi):
| Adım | Açık gruplar | Beklenen |
|---|---|---|
| 1 /kasalar | para-banka | aktif açık |
| 2 "Raporlar" başlığına bas | para-banka, rapor | — |
| 3 `GET /tercihler/benim` → 200, `menu-acik-gruplar` = `["rapor"]` | — | **aktif grup tohuma GİRMEDİ** (eski kod `["para-banka","rapor"]` yazardı) |
| 4 /faturalar | **belge-uyum**, rapor | aktif grup açık, para-banka artık her sayfada açık DEĞİL |
| 5 aktif grubu katla | rapor | bu ziyarette katlanır |
| 6 /uyum (aynı grup) | belge-uyum, rapor | rota değişince yine açık |
| 7–8 /kasalar + sayfa yenileme | para-banka, rapor | tercih kalıcı, aktif açık |
| 9 "Katlamayı sıfırla" | para-banka | tercih silindi (temizlik) |
Görüntü: `olc/foto/menu-kiyas2a-faturalar-1280.png` (Belge ve uyum + Raporlar açık, Para ve banka kapalı — Read ile bakıldı).

### 16.7 Görsel kanıt (Read ile bakıldı) — `ham/TUVAL-KARSILASTIRMA/`
- `KATMAN-1b-palet-tum-kombinasyon-2909.png` — 112 palet görüntüsü (7 rota × 2 mod × 2 tema × {1440,1024,375 Ctrl+K, 375 düğme}), her karede dosya adı.
- `KATMAN-1b-cekmece-menu-tum-kombinasyon-2909.png` — 48 çekmece + 17 menü görüntüsü.
- Tek tek bakılanlar: `palet_cariler-BASIT-aydinlik-1440-ctrlK` (üst orta, perdeli, 576 px), `palet_islemler-PRO-karanlik-375-dugme` (tam genişlik, üstte, koyu), `cekmece_kasalar-BASIT-aydinlik-1440` (Kasa çekmecesi 18 px; kartta "Günü Kapat" ince çizgi saat ikonu), `cekmece_vitrin-PRO-karanlik-1024` ("Yuvarlama adımı" TEK, × çerçeveli), `menu-kiyas2a-faturalar-1280`.
- Tuval bölge tablosu § 6/§ 15.8 ile aynı (palet tuvalde çizilmemiş → GEREKÇELİ FARK; menü "Ekran ara Ctrl K" AYNI). Kenar menü: tuval Panel.dc.html "aktif rotanın grubu açık" kuralına bu turla birlikte uyuldu.

### 16.8 Temizlik
- API (PID 52899) ve vekil (PID 59201) `kill`; `lsof` 5166/5167 **boş**; başsız Chrome (`KATMAN-1b/t6/profil*`) süreç sayısı **0**. 5199 (kurucunun sunucusu) dokunulmadı, ayakta.
- `zsh betikler/kiraci-temizle.sh 01M3QB7GDWDSWB333ZB83W1GDZ --uygula` (API durduktan sonra); ikinci çağrı "Kiracı YOK (bağlantı çalışıyor)". 70 tenant_id tablosunda bu kimlik için kalan satır **0**; `tenants` içinde **0**; davet satırı silindi (ROW_COUNT 1, kalan 0).
- tenants: başta **13**, sonda **14** — fark benim değil: `PAKETSATIS-3-TEST t21 Kuyumculuk` 19:39:43 UTC'de (22:39 yerel, koşum sırasında) SATIS-3 tarafından açıldı; dokunulmadı. QA-TEST-* dokunulmadı.
- Parola/jeton içeren geçici dosyalar (`t6/fikstur.json`, `t6/kayit.json`, `t6/kayit-yanit.json`, chmod 600) `printf '' >` ile boşaltıldı. Proje ağacına geçici dosya yazılmadı (yalıtık derleme `/private/tmp/kuyumcu-serit/KATMAN-1b/j/dotnet/art4`; ek sembolik bağlar yalnız `/private/tmp` altında).

### 16.9 Değişen dosyalar (bu turda)
```
 M dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor   (GrupAcik/AcikOlanlar/GrupKatla + KonumDegisti sıfırlama)
 M dotnet/Kuyumcu.Web/Pages/Kasalar.razor        (4 emoji → <Ikon>)
   dotnet/Kuyumcu.Tests/MenuKatlamaTestleri.cs   (1 test yeni kurala, 3 yeni test)
   dotnet/Kuyumcu.Tests/KatmanTekKalipTestleri.cs (K1_23 yeni)
   dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/KATMAN-1b-{palet,cekmece-menu}-tum-kombinasyon-2909.png
   dokumanlar/09-uygulama-kayitlari/PAKET-KATMAN-1b-uygulama-raporu.md
```
katman.js, katman.css, GlobalArama.razor, Vitrin.razor bu turda DEĞİŞMEDİ (mutasyon sonrası `cmp` AYNI).

### 16.10 Paket kapısı (29.09)
| # | Durum | Kanıt |
|---|---|---|
| 1 build | ✓ | 0 Uyarı 0 Hata (`build-son.log`) |
| 2 testlerim | ✓ | filtreli 752/752; tam süitte paket sınıflarında 0 kırmızı |
| 3 sayaçlarım | ✓ | denetim.sh TEMİZ, P-05 canlı şema dahil, ✗ 0 |
| 4 mutasyon | ✓ | 14 mutasyon, her biri adı yazılı testte kırmızı, hepsi cmp AYNI; 0×0 koruması kaldırılınca 4 test kırmızı |
| 5 canlı | ✓ | gerçek uygulama + gerçek DB: 112 palet + 48 çekmece + 16 menü + KIYAS 2a senaryosu; 177 görüntü |
| 6 kiracı silindi | ✓ | kalan satır 0; tenants farkı SATIS-3'ün |
| 7 yeni kırmızı yok | ✓ | 131 = 129 yalıtık düzen + 1 ortam (Migrations yolu, bağla yeşil) + 1 ZO-BELGE (DZB4_04) |

### 16.11 Açık kalanlar (sahibiyle)
1. **#cekmece-cari alan etiketleri 15 px — artık PRO'da da** (olcek.css `html[data-mod] .musteri-panel-kap #cekmece-cari .alan-etiket`) ↔ "katman öğe yazısı TEK" — **OLCEK-1 + orkestratör/kurucu kararı** (§ 15.12-4'ün genişlemesi).
2. `Dogrulayici.ZOBELGEFistenOnizlemeSinirTestleri.DZB4_04…` (NBSP/boşluk) — **ZO-BELGE**.
3. Yalıtık `--artifacts-path` düzeninde depo kökü dosyalarını okuyan testler (129) ve MadenDefteri Migrations yolu: kapi.sh şerit başına yalıtım kullanacaksa yalıtık kök depo kökünün tamamını (sembolik bağlarla) taşımalı — bu turda böyle yapıldı → **orkestratör**.
4. Sembolik bağlı `j/dotnet/Kuyumcu.sln` ile derleme alt klasör .cs'leri görmüyor (132 CS0246); gerçek sln + `--artifacts-path` kullanılmalı → **orkestratör** (kapi.sh notu).
5. § 15.12'deki 1, 5, 6, 7, 8, 10 maddeleri sürüyor (mutbak/`.cs.tmp` dosyaları, SATIS-3 `#islem-pro-odeme`, /urunler K10 ve /vitrin K3, OLCEK-1 sınır notu, ürün çekmecesi seçici sarma, iş zamanlayıcı çakışması — API günlüğünde bu turda da `Failed executing DbCommand` satırları var).
6. KIYAS emoji envanterinin KATMAN dışındaki sayfaları (Urunler 6, Cariler 6, GiderPusulalari, UrunAgaci, Panel, Uyum, UrunEtiketleri, Fiyatlandirma) ve denetim.sh emoji sayacının `Pages/` kapsamı — sahipsiz/ORTAK-1 (KIYAS "yeni paket önerisi" UX-5-GENIS).

**DoD sözleşmesi:** yeni varlık/uç yok; menü katlama tercihi mevcut `/tercihler/benim` PATCH/DELETE yolunu kullanır (Katlamayı sıfırla = DELETE, canlı 16.6 adım 9). Kasalar düğmelerinin davranışı (aktife al / gün kapat / düzenle / pasife al + onay) değişmedi.

## 17. 29/30.09 SON TUR (23:50–00:20; Ertuğ + Taşkın rolü, t8: API 5166 · vekil 5167 · `kuyumcu_test_t5` · CDP 9511–9518) — EN GÜNCEL KANIT, hakem çelişkilerini kapatır

### 17.1 Ağaç ve yarım mutasyon denetimi
- 23:21–23:47 arasında bir "t7" koşumu (`/private/tmp/kuyumcu-serit/KATMAN-1b/t7/`) doğrulayıcı bulgularının üç DÜŞÜK maddesini kodla kapatmış, ama rapora yazamadan kesilmişti. Ağaçtaki hâli yeniden YAZILMADI, KORUNDU ve bu turda kanıtlandı:
  - `katman.css` § 7: `@media (max-width:480px) { #kasa-liste .satir-eylemler { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)) } }` — 375 Kasalar kartında "⋯" tek başına alta düşmüyor (2×2 ızgara).
  - `katman.css` § 8: `.ana-icerik #vitrin-ayarlar > tbody > tr > td:first-child:not([colspan]) { white-space: nowrap }` + başlık rozetinin kendi satırına inmesi + `#vitrin-kur-kaynagi { white-space: nowrap }` — /vitrin 375 tablo kalem adları kırılmıyor; tablo kendi `tablo-sar` kabında kayar, sayfa kaymaz.
  - Testler: `KatmanTekKalipTestleri.K1_24_375te_Kasalar_kart_eylemleri_2x2_IZGARA_Diger_acicisi_tek_basina_alta_dusmez`, `K1_25_Vitrin_pano_ayarlari_tablosunda_kalem_adi_HER_GENISLIKTE_kirilmaz`.
- Yarım mutasyon: t7'nin her yedeği (`mut-yedek/KATMAN-1b-t7/`) ağaçla `cmp` edildi: `katman.js` = M-K1b-1 yedeği AYNI; `katman.css` = M-K1b-22 yedeği AYNI (son mutasyon geri alınmış). Ağaçta yarım mutasyon YOK.
- Bu turda KOD DEĞİŞİKLİĞİ YAPILMADI (yalnız doğrulama + rapor + birleşik görüntü).

### 17.2 Derleme ve paket testleri
- `dotnet build Kuyumcu.sln --nologo --artifacts-path /private/tmp/kuyumcu-serit/KATMAN-1b/j/dotnet/art4` → **0 Uyarı, 0 Hata** (`t8/build.log`); mutasyonlardan sonra yeniden: **0 Uyarı, 0 Hata** (`t8/build-son.log`).
- Filtreli (KatmanTekKalip, KabukArama, MenuKatlama, P17Vitrin, S1gMenuTuval, S1jMenuArama, TK2KabukTuval, TercihKatlama, Kasa*, IkonTestleri): **459/459 Geçti** (`t8/filt.log`).

### 17.3 Mutasyon kanıtları (yedek `/private/tmp/kuyumcu-serit/mut-yedek/KATMAN-1b-t8/`, betik `t8/mutlar.sh`; `perl -Mutf8 -0777 -pi` tek eşleşme → (razor ise derleme, 0 Hata) → filtreli test → yedek geri → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| **M-K1b-1** | `yerlestir`: `if (!gorunurKutu(a)) { bagiCoz(katman); return false; }` silindi — **0×0 açıcı koruması** | K1_18(oge), K1_18(popover), K1_19, K1_20 | AYNI |
| M-K1b-2 | beforetoggle `if (!gorunurKutu(r)) return;` silindi | K1_18(popover) | AYNI |
| M-K1b-6 | çekmece öğe yazısı `var(--katman-oge-boyu)` → `var(--yazi-md)` | K1_21 | AYNI |
| M-K1b-11 | görüş alanı dışı dalında `bagiCoz` kaldırıldı | K1_22 | AYNI |
| M-K1b-19 | Kasalar 375 ızgarasında `grid-template-columns` silindi | K1_24 | AYNI |
| M-K1b-20 | #vitrin-ayarlar ilk sütun `nowrap` boşaltıldı | K1_25 | AYNI |
| M-K1b-22 | `#vitrin-kur-kaynagi` `nowrap` boşaltıldı | K1_25 | AYNI |
| M-K1b-15 (derlemeli) | `GrupAcik`: öteki gruplar `?? false` → `?? true` | MenuKatlama 13 test (ör. `Varsayilan_olarak_YALNIZ_aktif_rotanin_grubu_aciktir`, `KIYAS2a_oteki_grubu_katlayinca_AKTIF_grup_TOHUMA_girmez`) | AYNI |
| M-K1b-16 (derlemeli) | `KonumDegisti`'nde `aktifKatlanan = null` silindi | `KIYAS2a_aktif_grup_bu_ziyarette_katlanir_TERCIHE_YAZILMAZ_rota_degisince_yine_acik` | AYNI |
| M-K1b-18 (derlemeli) | Kasalar "Günü Kapat"a 🌙 geri kondu | `K1_23_KIYAS5_Kasalar_satir_eylemlerinde_EMOJI_YOK_ince_cizgi_Ikon_var` | AYNI |
Kırmızısız kalan mutasyon 0. Günlük `t8/mutlar.log`.

### 17.4 Tam süit (`trx-pKATMAN-1b/t8-tam.trx`, `KUYUMCU_TEST_DB=kuyumcu_test_t5`)
**7377 · 7376 Başarılı · 1 Başarısız**: `P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` → koleksiyon `["dotnet/denetim.sh.mutbak"]` — devir notundaki bilinen kırmızı (bu paketin dosyası değil; dosya silme yasak → orkestratör/kurucu). § 16.3'teki 129 yalıtık düzen artığı ve DZB4_04 bu koşumda YOK (yalıtık kök sembolik bağlarla tamam; ZO-BELGE testini düzeltmiş). **Bu paketin yeni kırmızısı 0.**

### 17.5 denetim.sh (tam ağaç, canlı MySQL; `t8/denetim.log`)
**`DENETİM TEMİZ.` rc=0, ✗ sayısı 0**; P-05 CANLI ŞEMA tamamlandı (`DOGAL_ANAHTAR_KISITSIZ 0`, `CANLI_KISITSIZ 0`); `index.html satır içi <script> 0`; `ModalPencere 47 (tavan 55)`; `SECICI_TEK_KAYNAK 1 (satır içi renk/px 0)`. MySQL bu turda baştan sona ayaktaydı (`nc -z localhost 3306` açık).

### 17.6 Canlı ölçüm — GERÇEK uygulama (API `art4/bin/Kuyumcu.Api/debug/Kuyumcu.Api.dll` 5166 → kuyumcu_net; vekil 5167; başsız Chrome `betikler/tur/cdp.mjs`)
Kiracı `PAKETKATMAN-1b-TEST Olcum8` (01M3QF6HAW53QX89G450J210MB): davet satırı + `/auth/kayit` **201**; `/subeler` 200; 2 kasa **200/200**; 6 cari **201**×6; 3 ürün **201**×3; `/pano/guncel` **200**; `/kur/guncel` ilk çağrıda **404** (yeni kiracı; ölçümdeki üst şeritte kur 00:05'te dolu geldi: HAS 6.620 ₺, Vitrin "Kaynak: Altınkaynak · 30.09.2026 00:05").

**Palet** (112 kayıt, açılmayan 0, mod uyuşmazlığı 0; 7 rota × {BASİT,PRO} × {aydınlık,karanlık}; Ctrl+K = CDP `Input.dispatchKeyEvent`)
| Genişlik | Yol | n | kutu [sol,üst,en] | merkez farkı | data-katman | ekran dışı | perde | en üstte | yazı (öğe/başlık) | aile | yatay | açıcı | aria-modal |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | Ctrl+K | 28 | [432, 90, 576] | 0 | yok | 0 | 28/28 | 28/28 | 18/16 px | Manrope | 0 | **0×0** | true |
| 1024 | Ctrl+K | 28 | [224, 77, 576] | 0 | yok | 0 | 28/28 | 28/28 | 18/16 px | Manrope | 0 | **0×0** | true |
| 375 | Ctrl+K | 28 | [8, 8, 359] | 0 | yok | 0 | 28/28 | 28/28 | 18/16 px | Manrope | 0 | 44×44 | true |
| 375 | düğme | 28 | [8, 8, 359] | 0 | yok | 0 | 28/28 | 28/28 | 18/16 px | Manrope | 0 | 44×44 | true |
→ 0×0 açıcıda palet BAĞLI DEĞİL; sol üst köşeye yapışma **0/112**; odak 112/112 `#kabuk-arama-girdi`. 16 px yalnız başlık ve durum satırı (öğe değil; § 16.6 ile aynı).

**Çekmeceler** (48 ölçüm, açılmayan 0, iç taşma 0, yatay 0)
| Çekmece | BASİT | PRO | not |
|---|---|---|---|
| #cekmece-pano-kalem (Vitrin, paketin hedefi) | 18 px TEK (96) | 18 px TEK (96) | "Yuvarlama adımı" etiketi 1 (12/12) |
| #cekmece-kasa | 18 px TEK (54) | 18 px TEK (54) | |
| #cekmece-urun | 18 px TEK (108) | 18 px TEK (384) | |
| #cekmece-cari | 18 px ×48 · 15 px ×18 | 18 px ×138 · 15 px ×102 | alan etiketleri — OLCEK-1 kimlikli kuralı (açık kalan 1) |

**/vitrin ve /kasalar sayfa ölçümü** (`volc.mjs`, 32 kayıt: 2 rota × {1440,1280,1024,375} × 2 mod × 2 tema; sikisma-olc.js): K1 0 · K3 taşan 0 · **K3 kırık 0** (önceden /vitrin 375'te 22) · K5 (TRY) 0 · K9 0 · K10 0 · K12 0 · yatay 0.
- /vitrin #vitrin-ayarlar: 1440 tablo 1074 = kap 1074 (sığar), 1280 914 = 914, 1024 695 > 658 ve 375 808 > 317 → tablo **kendi kabında** kayar, sayfa yatay kaydırması 0.
- /kasalar 375: her kart `display:grid`, eylemler 2 satır (2×2), "⋯" 156×44, **tek başına alta düşen 0/8**.

**Kenar menü 1280×900** (16 ölçüm): aktif grup açık (kasalar→para-banka, faturalar→belge-uyum, raporlar→rapor, kullanicilar→yonetim) 16/16; son grup görünür+tıklanır 16/16; kart listeyi örter 0; yatay 0; BASİT /kasalar ve /kullanicilar'da nav kayar (`overflow-y:auto`).

**Tam sayfa K1..K12** (84 kayıt, 7 rota × 3 genişlik × 2 mod × 2 tema): hepsi 0. K2 (dar sütun) yalnız /islemler 1440'ta %73 (BASİT) / %69 (PRO): iki sütunlu satış tezgâhı (tuval düzeni), SATIS-3 alanı — bilinçli, bu paketin ekranı değil.

### 17.7 Görsel kanıt (Read ile bakıldı)
- Birleşik: `ham/TUVAL-KARSILASTIRMA/KATMAN-1b-t8-palet-cekmece-kasalar-vitrin-3009.png` (168 kare: 112 palet + 48 çekmece + 8 sayfa 375, her karede dosya adı).
- Tek tek bakılanlar: `palet_cariler-BASIT-aydinlik-1440-ctrlK` (üst orta, perdeli, 576 px), `palet_urunler-PRO-karanlik-375-dugme` (tam genişlik, üstte), `cekmece_vitrin-PRO-karanlik-1024` (pano çekmecesi 18 px tek boy, "Yuvarlama adımı" tek), `kasalar-BASIT-aydinlik-375` (+ Hareket · Geçmiş / Günü Kapat · ⋯ 2×2, saat ikonu ince çizgi), `vitrin-PRO-karanlik-375` (kalem adları tek satır; tablo kendi kabında kayar).

### 17.8 Temizlik ve kiracı sayısı (hakem çelişkisi — yeniden ölçüldü)
- API (PID 47117) ve vekil `kill`; `lsof` 5166/5167 **boş**; başsız Chrome (t8 profilleri) süreç **0**. 5199/5200 (kurucunun sunucuları) dokunulmadı.
- `kiraci-temizle.sh 01M3QF6HAW53QX89G450J210MB --uygula`: 244 satır + tenants 1 silindi; ikinci çağrı "Kiracı YOK (bağlantı çalışıyor)". Davet satırı silindi (ROW_COUNT 1, kalan 0). `fikstur.json` `printf '' >` ile boşaltıldı.
- **tenants: başta 13 (23:51, sorgu çıktısı listelendi), sonda 15.** Fark +2 BU PAKETİN DEĞİL: `PAKETORTAK-1-HAKEM-TEST …` (01M3QF78PZJC8GEKMNN4TBFAYR, 20:55:39 UTC = 23:55 yerel) ve `PAKETORTAK-1-HAKEM-TEST PRO …` (01M3QFC8BCCF4RPMSVGQBPMADE, 20:58:22 UTC) ölçüm sırasında ORTAK-1 hakemi tarafından açıldı; dokunulmadı. Bu paketin kiracısı: 0 satır, tenants'ta 0. **Çelişkinin açıklaması:** 13→14 (§ 16.8) ve doğrulayıcının 15→15'i farklı anlarda paralel şeritlerin açtığı kiracıları sayıyor; tutarlı gerçek = "bu paketin kiracısı sıfır kaldı, sayım farkları başka şeritlerin canlı kiracılarıdır". QA-TEST-* dokunulmadı.

### 17.9 Değişen dosyalar — TAM liste (hakem çelişkisi: katman.js/katman.css önceki listelerde yoktu)
`git status --porcelain | grep -v -E "/(bin|obj)/"` süzüldü; bu paketin dosyaları (`??` = ilk commit'ten sonra yeni ve izlenmeyen; `git diff` bunları GÖSTERMEZ — önceki listelerin eksikliği bundandı):
```
?? dotnet/Kuyumcu.Web/wwwroot/js/katman.js              (paketin asıl düzeltmesi: gorunurKutu/bagiCoz, 0×0 koruması)
?? dotnet/Kuyumcu.Web/wwwroot/css/katman.css            (perdeli üst-orta palet, TEK öğe yazısı, § 7 Kasalar 375, § 8 Vitrin tablo)
?? dotnet/Kuyumcu.Web/wwwroot/js/secici-yerlesim.js     (KATMAN-1 kirpanKutu; bu turda değişmedi)
?? dotnet/Kuyumcu.Web/Bilesenler/GlobalArama.razor      (bağlı değil, perdeli komut paleti)
?? dotnet/Kuyumcu.Web/Pages/Vitrin.razor                (yalnız pano çekmecesi)
 M dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor          (KIYAS 2a)
 M dotnet/Kuyumcu.Web/Pages/Kasalar.razor               (KIYAS 5: 4 emoji → Ikon)
 M dotnet/Kuyumcu.Web/wwwroot/index.html                (katman.css/katman.js bağlantıları — ortak dosya, başka şeritlerin satırları da var)
?? dotnet/Kuyumcu.Tests/KatmanTekKalipTestleri.cs       (Kuyumcu.Tests/ dizininin tamamı izlenmiyor)
?? dotnet/Kuyumcu.Tests/MenuKatlamaTestleri.cs
?? dotnet/Kuyumcu.Tests/KabukAramaEkranTestleri.cs
?? dotnet/Kuyumcu.Tests/P17EkranTestleri.cs             (KATMAN1b_pano_cekmecesinde_yuvarlama_etiketi_TEK_kez)
?? dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/KATMAN-1b-*.png
   dokumanlar/09-uygulama-kayitlari/PAKET-KATMAN-1b-uygulama-raporu.md
```
Ağacın tamamında 507 değişik/izlenmeyen giriş var (paralel şeritler); yukarıdakiler dışında bu paket dosya yazmadı. Proje ağacına geçici dosya bırakılmadı. COMMIT YOK.

### 17.10 Paket kapısı (30.09)
| # | Durum | Kanıt |
|---|---|---|
| 1 build | ✓ | 0 Uyarı 0 Hata (iki kez) |
| 2 testlerim | ✓ | filtreli 459/459 |
| 3 sayaçlarım | ✓ | denetim.sh TEMİZ rc=0, ✗ 0, P-05 canlı dahil |
| 4 mutasyon | ✓ | 10 mutasyon, hepsi adı yazılı testte kırmızı, hepsi cmp AYNI; 0×0 koruması kaldırılınca 4 test kırmızı |
| 5 canlı | ✓ | gerçek uygulama + gerçek DB: 112 palet + 48 çekmece + 16 menü + 84 sayfa + 32 vitrin/kasalar ölçümü, 168+ görüntü |
| 6 kiracı silindi | ✓ | 0 satır; tenants farkı ORTAK-1 hakeminin |
| 7 yeni kırmızı yok | ✓ | tek kırmızı P37c `denetim.sh.mutbak` (devirde bilinen) |

### 17.11 Doğrulayıcı bulgularının durumu
| Bulgu | Durum |
|---|---|
| [ORTA] #cekmece-cari 15 px | AÇIK — OLCEK-1 kimlikli kuralı; orkestratör "tuval ölçüsü ↔ tek yazı boyu" kararı verecek (katman.css'ten ezmek OLCEK-1 kararını sessizce ezerdi). Paketin hedefi Vitrin pano çekmecesi 8/8 kombinasyonda 18 px TEK. |
| [DÜŞÜK] /vitrin 375 tablo hücreleri kırılıyor | KAPANDI — K3 kırık 22 → 0 (32/32 ölçüm); K1_25 + M-K1b-20/22 |
| [DÜŞÜK] KIYAS 2a/5 kapsam genişlemesi | Kapı kaydına "KIYAS devri" gerekçesi (§ 16.2) — orkestratör |
| [DÜŞÜK] 375 Kasalar "⋯" alta tek düşüyor | KAPANDI — 2×2 ızgara, tek başına 0/8; K1_24 + M-K1b-19 |
| [KRİTİK] kiracı sayısı çelişkisi | KAPANDI — § 17.8 (13→15, +2 ORTAK-1 hakemi, zaman damgalı kimliklerle) |
| [KRİTİK] değişen dosyalar listesi | KAPANDI — § 17.9 tam liste (untracked dosyalar dahil) |

### 17.12 Açık kalanlar (sahibiyle)
1. #cekmece-cari alan etiketleri 15 px (olcek.css) → **OLCEK-1 + orkestratör/kurucu**.
2. `dotnet/denetim.sh.mutbak` (P37c kırmızısı) ve `Pages/Islemler.Sade.cs.tmp` → **orkestratör/kurucu** (dosya silme yasak).
3. /islemler 1440 dar sütun %69–73 (iki sütunlu tezgâh) → **SATIS-3** (bilinçli düzen ise gerekçe onların raporunda).
4. /vitrin #vitrin-ayarlar 1024/375'te tablo kendi kabında yatay kayar (sayfa kaymaz) — ORTAK-1 (A) kuralına uygun; daha dar sütun istenirse Vitrin tasarım kararı.
5. § 15.12'deki 5–10 ve § 16.11-3..6 maddeleri sürüyor (sahipleri orada).

## 18. 30.09 TURU (00:40–02:10; Ertuğ + Taşkın rolü, t9: API 5166 · vekil 5167 · `kuyumcu_test_t5` · CDP 9511–9519) — EN GÜNCEL KANIT, tasarım hakeminin 3 KRİTİK bulgusu kapatıldı

### 18.1 Ağaç ve devir
- § 17'deki iş (katman.js 0×0 koruması, katman.css § 1–8, GlobalArama perdeli palet, Vitrin pano çekmecesi, KenarCubugu KIYAS 2a, Kasalar ikonları) KORUNDU, yeniden yazılmadı. Yarım mutasyon yok: bütün paket testleri yeşil (18.3), her t9 mutasyonu `cmp AYNI` ile geri alındı.
- KIYAS-BULGULARI 28.09 "Şeritlere devir → KATMAN" satırları § 16.2'de kapanmıştı (KenarCubugu 2a, menü 1280×900 ölçümü, Kasalar emojileri); bu turda yeniden ölçüldü (18.5 kenar menü 16/16).

### 18.2 Bu turda yapılan kod değişikliği (tasarım hakemi 29.09 KRİTİK × 3 + ölçümde bulunan 1 kusur)
| Bulgu | Değişiklik | Dosya |
|---|---|---|
| **KRİTİK-1** Boş durum çift ve yanlış ("Kemal" aranınca EKRANLAR altında yarım ekranlık "yok" kutusu; "zzqqxx"te aynı mesaj simgesiyle iki kez) | Boş bölüm ÇİZİLMEZ: EKRANLAR başlığı yalnız eşleşen ekran varsa; KAYITLAR başlığı yalnız sonuç/hata/yükleniyor varsa. "Yok" yalnız iki bölüm de gerçekten boşken (istek başarılı, hata yok) TEK kez, simgesiz, kısa: `#kabuk-arama-yok` ("Sonuç yok"). Kısa sorguda (1 harf) panelde ikinci "en az 2 harf" kutusu da kalktı — durum satırı zaten söylüyor. Hata bandı (`#kabuk-arama-hata`) aynı yerde, "yok"a çevrilmez (UX-S28). | `Bilesenler/GlobalArama.razor`, `Kuyumcu.Core/I18n/Sozlukler.MenuArama.cs` (`PaletSonucYok` TR+EN), `css/katman.css` § 9 |
| **KRİTİK-2** Her aramada gereksiz sınır uyarısı ("En çok 10 kayıt … süzgeçleri kullanın") + altta ikinci "daha var" satırı | Durum satırı yalnız `yanit.DahaVar` iken çıkar, TEK yerde (`#kabuk-arama-daha-var` kimliği korunarak durum satırının içine taşındı; alttaki ikinci satır kaldırıldı). Metin sade: "Daha fazla sonuç var, adı biraz daha yazın" — "süzgeç" sözcüğü yok. | `GlobalArama.razor`, `Sozlukler.MenuArama.cs` (`PaletDahaVar` TR+EN) |
| **KRİTİK-3** Palet alt bilgisi tr-TR değil ("4.1 g", telefon ham "05320000104") | Sunucu alt bilgisi: gram `EkranGram` (tr-TR, `#,##0.###` → "4,1 g", "1,75 g", "1.250 g"); telefon `EkranTelefon` → "0532 000 01 04" (0'lı 11 hane; +90/90 ve 0'sız 10 hane aynı kalıba iner; kalıba uymayan — yurt dışı/dahili — DEĞİŞMEDEN döner, tahminle biçim uydurulmaz). Arama ham değerle yapılmaya devam eder ("05320000104" yazınca bulur). | `Kuyumcu.Api/Modules/Arama/AramaModulu.cs` (UrunAra, CariAra + 2 statik yardımcı) |
| **Ölçümde bulunan (bu tur)**: arama yazılı palette bölüm başlığı 13 px, tür rozeti 12 px (küçük yazı < 14 px; önceki turlar paleti yalnız boş açıp ölçtüğü için görünmemişti) | `.kayit-arama-palet .kabuk-arama-baslik { font-size: var(--yazi-md) }` (16 px, durum satırıyla aynı ikinci boy) · `.kayit-arama-palet .secici-sonuc .rozet { font-size: var(--katman-oge-boyu) }` (rozet sonuç satırının parçası → öğe boyu; K1_04 "palet sonucu tek belirteç" kuralı). | `css/katman.css` § 10 |

**Sapma ve gerekçe — dosya alanı:** `AramaModulu.cs` KATMAN'ın dosya listesinde yok (hakem "orkestratör açsın ya da sahibine devretsin" dedi). Bulgu KRİTİK ve görev metni "önce bunları kapat" dediği için, yalnız palette görünen iki satır (Ürün gramı, Cari telefonu) ve iki saf yardımcı değiştirildi; aynı dosyada ZO-BELGE (29.09, fiş K5) ve ZO-ISLEM-A (28.09, banka dekontu) aynı türden biçim düzeltmesini yapmış — emsal. Fatura/defter hareketi satırlarındaki `InvariantCulture` ("2100.00 TRY") DOKUNULMADI: bu türler rotasız olduğu için palette hiç çizilmez; KayitSecici kullanan ekranlar (Faturalar/İşlem Defteri) ZO-BELGE alanıdır → açık kalanlar. Kapı kaydına "tek dokunuş gerekçesi: tasarım hakemi KRİTİK-3" yazılmalı (orkestratör).
**Dokunulan sayfalar (TEK DOKUNUŞ):** Pages/ altında sayfa açılmadı. Bileşen: `GlobalArama.razor` (KATMAN alanı). app.css, olcek.css, Sozlukler HUB'ı, denetim.sh DEĞİŞMEDİ. Migration GEREKMEDİ (şema değişmedi). COMMIT YOK. Yeni element kimliği: `#kabuk-arama-yok` (önek kuralına uygun); mevcut kimlikler değişmedi (`#kabuk-arama-daha-var` korunup konumu değişti).

### 18.3 Derleme ve testler (`dotnet build Kuyumcu.sln --nologo --artifacts-path /private/tmp/kuyumcu-serit/KATMAN-1b/j/dotnet/art4`)
- Derleme: **0 Uyarı, 0 Hata** (`t9/build.log`, `build2.log`, mutasyon sonrası temiz `build-son.log`, CSS düzeltmesi sonrası `build-son3.log`).
- Yeni testler (11 yöntem / 19 durum):
  - `KabukAramaEkranTestleri`: `KATMAN1b_1_kayit_0_ekran_BOS_DURUM_YOK_ekran_basligi_CIZILMEZ`, `KATMAN1b_0_kayit_0_ekran_TEK_bos_durum_SIMGESIZ_ve_KISA`, `KATMAN1b_ekran_ve_kayit_varken_iki_baslik_bos_durum_YOK`, `KATMAN1b_ekran_var_kayit_0_YOK_mesaji_ve_KAYITLAR_basligi_CIKMAZ`, `KATMAN1b_sinir_satiri_daha_var_YOKKEN_cikmaz`, `KATMAN1b_sinir_satiri_daha_var_iken_TEK_kez_sade_metinle`
  - `AramaHttpTestleri` (gerçek HTTP boru hattı + saf): `KATMAN1b_urun_alt_bilgisinde_GRAM_tr_TR_4_virgul_1_g`, `KATMAN1b_cari_alt_bilgisinde_TELEFON_0532_000_01_04_kalibinda_ham_numarayla_ARANIR`, `KATMAN1b_EkranTelefon_TR_kalibi_uymayani_DEGISTIRMEZ` (6 durum), `KATMAN1b_EkranGram_tr_TR` (4 durum)
  - `KatmanTekKalipTestleri.K1_26_Palet_YOK_satiri_KISA_12rem_kutu_degil_oge_yazi_boyunda` (+ başlık/rozet boyu)
- Filtreli paket + komşu sınıflar (KabukArama, KatmanTekKalip, AramaHttp, MenuKatlama, P17Vitrin, S1gMenuTuval, S1jMenuArama, TK2KabukTuval, TercihKatlama, Kasa*, IkonTestleri, ZoBelgeAramaFis, ZOISLEMAArama, SeciciUzakKip, P37Revizyon, TasarimBelirtec, D22): **579/579 Başarılı** (`t9/filt4.log`). Ara koşumda (`filt3.log`) `K1_04` kırmızı çıktı — rozeti önce `--yazi-md` yapmıştım, K1_04 "palet sonucu tek belirteç" kuralı yakaladı → rozet `--katman-oge-boyu`'ya alındı, yeşil.

### 18.4 Mutasyon kanıtları (yedek `/private/tmp/kuyumcu-serit/mut-yedek/KATMAN-1b-t9/`, betik `t9/mut.sh` + `t9/mutlar.sh`; `perl -Mutf8 -0777 -pi` tek eşleşme → (derlemeli ise 0 Hata) → filtreli test → yedek geri → `cmp`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| **M-K1b-1** (güncel ağaçta yeniden) | katman.js `if (!gorunurKutu(a)) { bagiCoz(katman); return false; }` silindi — **0×0 açıcı koruması** | K1_18(oge), K1_18(popover), K1_19, K1_20 | AYNI |
| M-K1b-23 | EKRANLAR başlığı yeniden koşulsuz çizildi | `KATMAN1b_1_kayit_0_ekran_…`, `KATMAN1b_0_kayit_0_ekran_…` | AYNI |
| M-K1b-24 | `kayitBolumuVar = true \|\| …` (KAYITLAR başlığı hep) | `KATMAN1b_ekran_var_kayit_0_…`, `KATMAN1b_0_kayit_0_ekran_…` | AYNI |
| M-K1b-25 | `ikisiDeBos`'tan `ekranlar.Count == 0 &&` silindi | `KATMAN1b_ekran_var_kayit_0_YOK_mesaji_ve_KAYITLAR_basligi_CIKMAZ` | AYNI |
| M-K1b-26 | durum satırı `yanit is { DahaVar: true }` → `yanit is not null` | `KATMAN1b_sinir_satiri_daha_var_YOKKEN_cikmaz` | AYNI |
| M-K1b-27 | simgesiz "yok" → eski `<BosDurum AramaSonucYok/>` | `KATMAN1b_0_kayit_0_ekran_TEK_bos_durum_SIMGESIZ_ve_KISA`, `K1_26_…` | AYNI |
| M-K1b-28 (Api) | `EkranGram` → `InvariantCulture "0.####"` | `KATMAN1b_urun_alt_bilgisinde_GRAM_tr_TR_4_virgul_1_g` + `KATMAN1b_EkranGram_tr_TR` ×4 | AYNI |
| M-K1b-29 (Api) | CariAra `EkranTelefon(c.Telefon)` → `c.Telefon` | `KATMAN1b_cari_alt_bilgisinde_TELEFON_0532_000_01_04_…` | AYNI |
| M-K1b-30 (css) | § 9 `height: auto` silindi | `K1_26_…` (aynı koşumdaki telefon kırmızısı M-29 DLL'inin artığı — css mutasyonu derleme istemediği için DLL M-29'dan kalmıştı; ardından temiz derleme alındı) | AYNI |
| M-K1b-31 (css) | § 10 palet başlık boyu kuralı silindi | `K1_26_…` | AYNI |
Kırmızısız kalan mutasyon **0**. Günlük `t9/mutlar.log`. Son mutasyondan sonra temiz derleme 0/0.

### 18.5 Tam süit (`trx-pKATMAN-1b/t9-tam.trx`, `KUYUMCU_TEST_DB=kuyumcu_test_t5`)
**7444 · 7441 Başarılı · 3 Başarısız** — hiçbiri bu paketin değil:
- `P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` → `["dotnet/denetim.sh.mutbak"]` (devirde bilinen; dosya silme yasak → orkestratör/kurucu).
- `MadenDefteriTestleri.GeriDoldurmaMigrationiUrunAyarindanTuretir` → yalıtık `art4/bin/Kuyumcu.Api/Data/Migrations` yok (devirde bilinen yalıtık düzen artığı; ağaç içinde yeşil).
- `Dogrulayici.SATIS3SinirTestleriT22Hasim.D22_avans_iadesi_tevsik_reddi_SESSIZ_kalmamali` → `WaitForFailedException` (tam süit yükü altında); tek başına/filtreli koşumda **Başarılı** (`filt4.log`); dosya SATIS-3'ün (`SATIS3SinirTestleri.cs`, 01:04'te değişti) → **SATIS-3** (kararsızlık).
→ **Bu paketin yeni kırmızısı 0.**

### 18.6 denetim.sh (tam ağaç, canlı MySQL; `t9/denetim.log`)
`DENETİM BAŞARISIZ` rc=1, **✗ sayısı 1**: `P-23: ODEME_KALEMSIZ_YOL (beklenen 0) — 1 hareket ödeme yazdı ama kalem yazmadı` — önceki doğrulamada adı konan canlı veri (TEST-KESIF kiracısı, 29.09 21:10 UTC KART hareketi); bu paket ödeme yoluna ve o kiracıya dokunmadı → **orkestratör kapısı / TEST-KESIF sahibi**. Paketin sayaçları ✓: `Pages: ModalPencere kullanımı 47 (tavan 55)`, P-05 CANLI ŞEMA tamamlandı (DOGAL_ANAHTAR_KISITSIZ 0, CANLI_KISITSIZ 0, INDEKS_IFADESIZ 0), emoji/ikon, satır içi stil, SECICI_TEK_KAYNAK ✓. MySQL baştan sona ayaktaydı.

### 18.7 Canlı doğrulama — GERÇEK uygulama (API `art4/bin/Kuyumcu.Api/debug/Kuyumcu.Api.dll` 5166 → kuyumcu_net; vekil 5167; başsız Chrome `betikler/tur/cdp.mjs`)
Kiracı `PAKETKATMAN-1b-TEST Olcum9` (01M3QKGXANK7N2P3NTT4XJXS4B): davet satırı + `/auth/kayit` **201**; `/subeler` 200; 2 kasa **200/200**; 6 cari **201**×6 + `Kemal Ardıç` (telefon 05320000104) **201** + `Kemal Test 00..10` **201**×11; 3 ürün **201**×3 (gram 5,25 / 4,1 / 1,75); `/pano/guncel` 200; `/kur/guncel` **404** (yeni kiracı, ilk çağrı; ölçümde üst şerit dolu geldi).
API günlüğünde açılışta `is_calismalari` benzersiz anahtar çakışmaları (başka API örneklerinin bugünkü günlük işleri — MASAK_SIB_SURESI/VADE_HATIRLATMA, idempotens anahtarı) var; paketle ilgisiz, istekler etkilenmedi.

**API yanıtları (okundu):**
| İstek | Kod | Yanıt (özet) |
|---|---|---|
| `GET /arama?q=Ardıç` | 200 | sayi 1, dahaVar false, `Kemal Ardıç · "0532 000 01 04"` |
| `GET /arama?q=Kolye` | 200 | sayi 1, `14 Ayar Zirkon Taşlı Kolye · "KT9-1 · K14 · 4,1 g"` |
| `GET /arama?q=05320000104` | 200 | sayi 1 — ham numarayla bulur, alt bilgi biçimli |
| `GET /arama?q=zzqqxx` | 200 | sayi 0, sonuclar [] |
| `GET /arama?q=Kemal` | 200 | sayi 10, **dahaVar true** |
SQL kanıtı: `cariler.telefon = '05320000104'` (ham saklanır), `urunler.gram_agirlik = 4.1000 / 1.7500` — biçim yalnız ekranda.

**Palet araması** (60 kayıt: 5 sorgu × {1440,1024,375} × {BASİT,PRO} × {aydınlık,karanlık}; panelsiz 0, mod uyuşmazlığı 0)
| Sorgu | .bos-durum | simge | başlıklar | ekran/kayıt | durum satırı | alt bilgi | küçük yazı | ekran dışı | yatay |
|---|---|---|---|---|---|---|---|---|---|
| Ardıç (1 kayıt, 0 ekran) | **0** (12/12) | 0 | KAYITLAR | 0/1 | boş | `0532 000 01 04` | 0 | 0 | 0 |
| zzqqxx (0/0) | **1** "Sonuç yok", 40 px (12/12) | **0** | yok | 0/0 | boş | — | 0 | 0 | 0 |
| Kolye | 0 | 0 | KAYITLAR | 0/1 | boş | `KT9-1 · K14 · 4,1 g` | 0 | 0 | 0 |
| cari (1 ekran, 0 kayıt) | 0 | 0 | EKRANLAR | 1/0 | boş | — | 0 | 0 | 0 |
| Kemal (10, daha var) | 0 | 0 | KAYITLAR | 0/10 | **"Daha fazla sonuç var, adı biraz daha yazın"** (tek) | `0532 000 01 04` | 0 | 0 | 0 |
"süzgeç" geçen palet 0/60; noktalı gram 0/60; ham telefon 0/60. Yazı boyları: öğe/girdi/rozet 18 px, başlık/durum/alt bilgi 16 px (küçük yazı 0). Paletteki tek "taşan" öğe `label.gorsel-gizli` ("Ara ve git", ekran okuyucu etiketi, 1 px — bilinçli görsel gizleme, kusur değil).

**Palet konumu** (112 kayıt; Ctrl+K = CDP `Input.dispatchKeyEvent`): 1440 `[432,90,576]`, 1024 `[224,77,576]`, 375 `[8,8,359]` (Ctrl+K ve düğme); merkez farkı 0; `data-katman` yok; ekran dışı 0; perde 112/112; en üstte 112/112; açıcı masaüstünde **0×0** iken palet BAĞLI DEĞİL, sol üst köşeye yapışma **0/112**; odak 112/112 `#kabuk-arama-girdi`; aria-modal true; yazı 18/16; Manrope; yatay 0.
**Çekmeceler** (48): `#cekmece-pano-kalem` 18 px TEK (BASİT 96, PRO 96), "Yuvarlama adımı" etiketi 1 (12/12); `#cekmece-kasa` 18 TEK; `#cekmece-urun` 18 TEK; `#cekmece-cari` 18 + **15 px** alan etiketleri (OLCEK-1 kuralı, açık kalan). İç taşma 0, yatay 0.
**Kenar menü 1280×900** (16): aktif grup açık 16/16 (kasalar→para-banka, faturalar→belge-uyum, raporlar→rapor, kullanicilar→yonetim); son grup görünür + tıklanır 16/16; kart örtme 0; yatay 0.
**Tam sayfa sikisma-olc.js** (84: 7 rota × 3 genişlik × 2 mod × 2 tema): K1, K3 taşan/kırık, K5 (TRY), K7, K8, K9, K10, K11, K12, yatay → **hepsi 0**. K2 yalnız /islemler 1440 %73/%69 (SATIS-3 iki sütunlu tezgâh; § 17.6 ile aynı).

### 18.8 Görsel kanıt (Read ile bakıldı)
- `ham/TUVAL-KARSILASTIRMA/KATMAN-1b-t9-palet-arama-3009.png` — 6 kare: `Kemal` BASİT aydınlık 1440 (durum satırında tek "Daha fazla sonuç var…", EKRANLAR başlığı yok, sonuç en üstte), `zzqqxx` PRO karanlık 1440 (tek satır "Sonuç yok", simgesiz), `Ardıç` BASİT karanlık 1024 (KAYITLAR · Müşteri · Kemal Ardıç · 0532 000 01 04), `Kolye` PRO aydınlık 375 (KT9-1 · K14 · 4,1 g), `zzqqxx` BASİT aydınlık 375, `cari` PRO karanlık 375 (yalnız EKRANLAR · Müşteriler).
- Ayrıca `t9/olc9/foto/palet_cariler-PRO-karanlik-1440-ctrlK.png` (üst orta, perdeli, 576 px) ve `cekmece_vitrin-BASIT-aydinlik-1440.png` (pano çekmecesi tek boy) incelendi. Tüm kareler: `/private/tmp/kuyumcu-serit/KATMAN-1b/t9/olc9/foto/` (320 dosya).

### 18.9 Temizlik ve kiracı sayısı
- API (kill PID) ve vekil kapatıldı: `lsof` 5166 **0**, 5167 **0**; t9 başsız Chrome süreçleri **0** (yalnız `--user-data-dir=/private/tmp/kuyumcu-serit/KATMAN-1b/t9…` olanlar). 5199/5200 dokunulmadı.
- `kiraci-temizle.sh 01M3QKGXANK7N2P3NTT4XJXS4B --uygula` → ikinci çağrı "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)". Davet satırı silindi (ROW_COUNT 1, kalan 0). `fikstur.json` `printf '' >` ile boşaltıldı.
- **tenants: başta 14 (01:05), sonda 14.** QA-TEST-* ve başka şeritlerin kiracılarına dokunulmadı.

### 18.10 Değişen dosyalar (bu tur; `git status --porcelain | grep -v -E "/(bin|obj)/"` — ağaçta toplam 507 giriş, paralel şeritler)
```
?? dotnet/Kuyumcu.Web/Bilesenler/GlobalArama.razor        (boş bölüm çizilmez; tek simgesiz "yok"; sınır satırı yalnız DahaVar)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.MenuArama.cs        (PaletSonucYok, PaletDahaVar — TR+EN)
?? dotnet/Kuyumcu.Web/wwwroot/css/katman.css              (§ 9 kısa "yok" satırı, § 10 palet başlık/rozet boyu)
?? dotnet/Kuyumcu.Api/Modules/Arama/AramaModulu.cs        (EkranGram, EkranTelefon; UrunAra/CariAra alt bilgisi) — dizin izlenmiyor
?? dotnet/Kuyumcu.Tests/KabukAramaEkranTestleri.cs         (6 yeni test)
?? dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs               (4 yeni test yöntemi)
?? dotnet/Kuyumcu.Tests/KatmanTekKalipTestleri.cs          (K1_26)
?? dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/KATMAN-1b-t9-palet-arama-3009.png
   dokumanlar/09-uygulama-kayitlari/PAKET-KATMAN-1b-uygulama-raporu.md
```
(katman.js, secici-yerlesim.js, Vitrin.razor, KenarCubugu.razor, Kasalar.razor, index.html bu turda DEĞİŞMEDİ — § 17.9.) Proje ağacına geçici dosya bırakılmadı (yedekler `/private/tmp/kuyumcu-serit/` altında). COMMIT YOK.

### 18.11 Paket kapısı (30.09)
| # | Durum | Kanıt |
|---|---|---|
| 1 build | ✓ | 0 Uyarı 0 Hata (son: `build-son3.log`) |
| 2 testlerim | ✓ | filtreli 579/579 |
| 3 sayaçlarım | ✓ | denetim.sh'ta paketin sayaçları ✓; tek ✗ P-23 canlı veri (TEST-KESIF, başka şerit) |
| 4 mutasyon | ✓ | 10 mutasyon (M-K1b-1, 23–31), hepsi adı yazılı testte kırmızı, hepsi cmp AYNI; 0×0 koruması kaldırılınca 4 test kırmızı |
| 5 canlı | ✓ | gerçek uygulama + gerçek DB: 60 palet araması + 112 palet + 48 çekmece + 16 menü + 84 sayfa; API yanıtları + SQL |
| 6 kiracı silindi | ✓ | tenants 14 → 14 |
| 7 yeni kırmızı yok | ✓ | 3 kırmızı: P37c (bilinen), MadenDefteri (bilinen yalıtık düzen), D22 (SATIS-3, yük altında kararsız, tek başına yeşil) |

### 18.12 Doğrulayıcı bulgularının durumu
| Bulgu | Durum |
|---|---|
| [KRİTİK] Palet boş durumu çift ve yanlış | **KAPANDI** — 18.2; canlı 60/60; M-K1b-23/24/25/27 |
| [KRİTİK] Her aramada sınır uyarısı, "süzgeç" | **KAPANDI** — 18.2; canlı: yalnız "Kemal"de (dahaVar) tek satır, "süzgeç" 0/60; M-K1b-26 |
| [KRİTİK] Palet sayı biçimi (4.1 g, ham telefon) | **KAPANDI** (palette görünen türler) — 18.2; canlı "4,1 g", "0532 000 01 04"; M-K1b-28/29. Alan dışı dosyaya dokunuş gerekçeli (18.2 sapma). |
| [DÜŞÜK] P-23 ODEME_KALEMSIZ_YOL=1 | AÇIK — başka şeridin canlı verisi (TEST-KESIF); orkestratör kapısı |
| [DÜŞÜK] PosBarkod testi tam süitte kararsız | Bu turda tam süitte yeşil; POS sahibine ait (değişiklik yapılmadı) |
| [DÜŞÜK] #cekmece-cari 15 px | AÇIK — OLCEK-1 + orkestratör kararı (değişmedi) |
| [DÜŞÜK] denetim.sh.mutbak, *.bak artıkları | AÇIK — dosya silme yasak → orkestratör/kurucu |

### 18.13 Açık kalanlar (sahibiyle)
1. `AramaModulu.cs` fatura/defter hareketi alt bilgisi hâlâ `InvariantCulture` ("2100.00 TRY") — palette çizilmez ama KayitSecici kullanan ekranlarda görünebilir → **ZO-BELGE** (Faturalar/İşlem Defteri sahibi); ayrıca ürün alt bilgisindeki ayar kodu "K14" ham koddur (UX-S31) → ayar sözlüğü sahibi / orkestratör.
2. P-23 ODEME_KALEMSIZ_YOL=1 (TEST-KESIF kiracısı) → **orkestratör kapısı / o kiracının şeridi**.
3. `D22_avans_iadesi_tevsik_reddi_SESSIZ_kalmamali` tam süitte zaman aşımı → **SATIS-3**.
4. #cekmece-cari 15 px alan etiketleri → **OLCEK-1 + orkestratör/kurucu**.
5. `dotnet/denetim.sh.mutbak`, `Pages/Islemler.Sade.cs.tmp`, 0 baytlık `*.bak` → **orkestratör/kurucu** (dosya silme yasak).
6. Yalıtık düzende `MadenDefteriTestleri` Migrations yolu → **orkestratör** (kapi.sh artifacts-path kararı).
7. `AramaModulu.cs` dokunuşu KATMAN alanı dışında — kapı kaydına gerekçe (tasarım hakemi KRİTİK-3) → **orkestratör**.
8. § 17.12'deki 3–5 maddeleri sürüyor.

### 18.14 DoD sözleşmesi
Yeni varlık/kayıt türü YOK (yalnız görünüm + alt bilgi biçimi). Düğmeler: palette yeni düğme eklenmedi; "yok" satırı düğme değildir (role=status). Hata bandı ve boş durum ayrı (UX-S28). Mevzuat hükmü içermez. Ücretsiz pilot kuralı: fiyat/teklif metni üretilmedi.

## 19. 30.09 YENİDEN DOĞRULAMA TURU (02:28–02:50; Ertuğ + Taşkın rolü, t10: API 5166 · vekil 5167 · `kuyumcu_test_t5` · CDP 9511) — KOD DEĞİŞİKLİĞİ YOK
- **Ağaç:** katman.js, katman.css, GlobalArama.razor, AramaModulu.cs t9 yedekleriyle `cmp` AYNI → yarım mutasyon yok, § 18 işi korundu. KIYAS KATMAN satırları (KenarCubugu 2a, menü 1280, Kasalar emoji) yerinde: Kasalar.razor'da ekrana çizilen emoji 0 (tek eşleşme bir yorumdaki ⚠), `<Ikon>` 4.
- **Derleme:** `dotnet build Kuyumcu.sln --artifacts-path …/art4` → **0 Uyarı 0 Hata** (`t10/build.log`).
- **Filtreli paket + komşu testler** (§ 18.3 filtresi): **590/590 Geçti** (`t10/filt.log`).
- **Mutasyonlar** (yedek `mut-yedek/KATMAN-1b-t10/`): M-K1b-1 (0×0 koruması silindi) → K1_18(oge), K1_18(popover), K1_19, K1_20 KIRMIZI, cmp AYNI · M-K1b-2 (beforetoggle koruması) → K1_18(popover) KIRMIZI, cmp AYNI · M-K1b-6 (çekmece öğe yazısı → --yazi-md) → K1_21 KIRMIZI, cmp AYNI. Hepsi css/js (derleme gerektirmedi).
- **denetim.sh:** rc=1, tek ✗ `P-23 ODEME_KALEMSIZ_YOL = 2` — iki satır da **TEST-KESIF Kuyumculuk** kiracısının (KART 29.09 21:10, NAKIT 29.09 23:19; SQL ile doğrulandı) → orkestratör/o şerit. Paket sayaçları ✓ (ModalPencere 47/55).
- **Canlı ölçüm — KISMİ:** kiracı `PAKETKATMAN-1b-TEST Olcum10` (kayıt 201, 2 kasa 200, 18 cari 201, 3 ürün 201, pano 200). Ölçüm koşumu BASİT-aydınlık'ın 46/80 kaydında **kesildi** (oturum çıktı süresi doldu). Alınan 46 kayıtta: palet 26 kayıt — 1440 `[432,90,576]`, 1024 `[224,77,576]`, 375 `[8,8,359]`, merkez farkı 0, `data-katman` yok, açıcı masaüstünde 0×0 iken yapışma 0, perde 26/26, en üstte 26/26, yazı 18/16 px, ekran dışı 0, yatay 0; sikisma-olc 20 sayfa kaydı K1/K3/K5/K7–K12/yatay = 0. Çekmece, arama ve menü ölçümleri ile BASİT-karanlık/PRO bu turda KOŞULMADI (§ 18.7'deki tam ölçüm geçerli kanıt; kod o zamandan beri değişmedi — cmp AYNI).
- **Temizlik:** API/vekil/Chrome durduruldu (5166/5167 dinleyen 0); kiracı `kiraci-temizle.sh --uygula` → "Kiracı YOK"; davet satırı silindi (1); fikstür boşaltıldı; **tenants 14 → 14**. 5199/5200'e dokunulmadı. COMMIT YOK. Tam süit bu turda KOŞULMADI (hız kuralı: uygulayıcı koşmaz).
- **Açık:** bu turun canlı ölçümü kısmi (kalan 274/320 kayıt); § 18.13 açık kalanları sürüyor.

## 20. 30.09 SON YENİDEN DOĞRULAMA TURU (02:55–03:20; Taşkın + Ertuğ rolü, t11: API 5166 · vekil 5167 · `kuyumcu_test_t5` · CDP 9511–9514) — EN GÜNCEL KANIT, hakem çelişkisi kapatıldı

### 20.1 Ağaç ve yarım mutasyon
katman.js ve katman.css t10 yedekleriyle `cmp` AYNI (tur başında) → yarım mutasyon yok, § 18 işi korundu. Docker `kuyumcu-mysql` 30 saattir ayakta (healthy).

### 20.2 Önceki doğrulama bulgularının kapatılması
| Bulgu | Sonuç |
|---|---|
| **[KRİTİK] Hakem çelişkisi — `L1bSinir D1` kırmızısı** | **ÇÖZÜLDÜ, sayı yeniden ölçüldü.** (a) `L1bSinirTestleri` KATMAN-1b'nin DEĞİL, **PAKET L-1b'nin** (çok kalemli alış fişi) hasım sınır testidir (`Dogrulayici/L1bSinirTestleri.cs`, 25.09 10:02; kiracı öneki `PAKETL1b-TEST-DOG-`). Doğrulayıcının kendi dosyası ayrıdır: `Dogrulayici/KATMAN1bSinirTestleri.cs` (23/23 yeşil — bu turda da yeşil). Ad benzerliği karışıklığın kaynağı. (b) D1'in kırmızı nedeni trx'ten okundu (`trx-dogrulama-pKATMAN-1b/_Mac_2026-09-30_02_36_36.trx`): `400: "Alış fişindeki kalemin tutarı 0 TL olamaz: ödemesi olmayan işlem kaydedilmez…"` — bu metin `Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs` `OdemeTutarKapisi.SifirOlamaz` (P-23 kapısı, test sınıfı `ZOISLEMAOdemeTutarKapisiTestleri` → **ZO-ISLEM-A**) kaynaklıdır. Dosya 02:50'de yeniden değişti; güncel hâli "bedelsiz mal girişi = 0 TL alış ödeme etiketi TAŞIMAZ" istisnasını içeriyor. (c) Zaman çizelgesi (doğrulayıcı trx'leri): D1 00:20 Geçti · 01:39 **Kaldı** · 01:43 Geçti · 02:39 **Kaldı**; bu tur filtreli (02:58) **Geçti**, tam süit (t11) **Geçti**. → D1, ZO-ISLEM-A'nın P-23 çalışmasının ara durumuna bağlı değişken bir kırmızıdır; KATMAN-1b dosyaları (katman.js/css, GlobalArama, Sozlukler.MenuArama, AramaModulu) alış yolunu çağırmaz. İki rapor artık aynı gerçeği yazıyor: **paketin yeni kırmızısı 0 — bu kez DOĞRUDAN tam süitle ölçüldü** (20.4). |
| [DÜŞÜK] `data-katman-acici` / MutationObserver ölü kod | **Gerekçe yazıldı, yol korundu** (katman.js başlık yorumu, yalnız yorum): razor tüketicisi bugün 0; (1) popover olmayan, Blazor'un koşullu çizdiği bağlı katman ikinci konumlama kodu yazılmadan tek kalıba bu öznitelikle girer; (2) 0×0/ekran dışı açıcı korumasının popover'sız kolu bu yolla sınanır. Kanıt — **M-K1b-32**: `BAGLI_OGE` seçicisi bozulunca `K1_19_acici_sonradan_gizlenirse_bag_COZULUR…` ve `K1_22_bagli_oge_acicisi_gorus_alani_DISINA…` KIRMIZI (60 geçti/2 kaldı), cmp AYNI. Silinirse regresyon kalkanı yarıya iner. Gözlemcinin maliyeti yorumda dürüstçe yazıldı (eklenen her öğede tek seçici sorgusu). |
| [DÜŞÜK] § 19 canlı ölçüm kısmi (46/80) | **KAPANDI**: bu tur 4 kombinasyonun hepsi tamam — **320/320 kayıt** (20.5). |
| [DÜŞÜK] Vitrin çekmecesi açıkken K9=6 (ölçüm artefaktı) | Bu turun sayfa ölçümü (çekmece kapalı) K9 = 0/84; çekmece açıkken K9 elemesi orkestratörün `sikisma-olc.js` betiği işidir (dosya alanım dışı) → açık kalan. |
| [DÜŞÜK] Tam süitte başka şeritlere ait kırmızılar | 20.4'te yeniden ölçüldü, sahipleriyle. |

### 20.3 Derleme ve paket testleri
- `dotnet build Kuyumcu.sln --nologo --artifacts-path /private/tmp/kuyumcu-serit/KATMAN-1b/j/dotnet/art4` → **0 Uyarı, 0 Hata** (`t11/build.log`).
- Filtreli (§ 18.3 filtresi + `KATMAN1bSinir` + `L1bSinir`): **632/632 Geçti** (`t11/filt.log`).

### 20.4 Tam süit (`trx-pKATMAN-1b/t11-tam.trx`, `KUYUMCU_TEST_DB=kuyumcu_test_t5`, yalıtık art4 derlemesi 02:57)
**7556 · 7549 Başarılı · 7 Başarısız** — hiçbiri bu paketin değil; `L1bSinir D1` **Geçti**:
| Test | Neden | Sahip |
|---|---|---|
| `P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` | `["dotnet/denetim.sh.mutbak"]` (bilinen artık; dosya silme yasak) | orkestratör/kurucu |
| `ORTAK1KarZararSinirTestleri.OK3`, `OK8`; `KarZararEkranTestleri.ORTAK1_HK30_…` ×3 | `Pages/KarZarar.razor` 02:59'da değişiyor (ORTAK-1 ara durumu; denetim P-36b ✗ de aynı dosyanın 384. satırı) | **ORTAK-1** |
| `MadenDefteriTestleri.GeriDoldurmaMigrationiUrunAyarindanTuretir` | yalıtık düzende `art4/bin/Kuyumcu.Api/Data/Migrations` yok (bilinen düzen artığı) | orkestratör (kapi.sh) |
→ **Bu paketin yeni kırmızısı 0** (doğrudan ölçüm).

### 20.5 Mutasyonlar (güncel ağaç; yedek `/private/tmp/kuyumcu-serit/mut-yedek/KATMAN-1b-t11/`, betik `t11/mut.sh`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M-K1b-1 | katman.js `if (!gorunurKutu(a)) { bagiCoz(katman); return false; }` silindi (0×0 açıcı koruması) | K1_18(oge), K1_18(popover), K1_19, K1_20 | AYNI |
| M-K1b-6 | katman.css çekmece öğe yazısı `--katman-oge-boyu` → `--yazi-md` | K1_21_kayit_paleti_…_cekmece_yazi_TEK | AYNI |
| M-K1b-32 | katman.js `BAGLI_OGE` seçicisi bozuldu (data-katman-acici yolu) | K1_19, K1_22 | AYNI |
**Dürüstlük notu:** ilk denemede betik mutasyon sonrası test dizinine `cd` ettiği için göreli yolla verilen dosyaları geri kopyalayamadı ("No such file"); katman.js ve katman.css yedekten MUTLAK yolla hemen geri yüklendi ve `cmp AYNI` doğrulandı (0×0 koruması satırı 1, gerekçe yorumu 1). O ilk M-K1b-6 koşumu js artığıyla kirli olduğu için geçersiz sayıldı; yukarıdaki tablo mutlak yollarla TEMİZ yeniden koşumdur. Mutasyonlar yalnız css/js (derleme gerektirmez).

### 20.6 denetim.sh (`t11/denetim.log`)
rc=1, ✗ 2: `P-23 ODEME_KALEMSIZ_YOL = 2` (TEST-KESIF kiracısı, § 19 ile aynı → orkestratör / o şerit) · `P-36b GIRDI_AILESI_DISI_BICIM = 1` → `Pages/KarZarar.razor:384` `ToString("N…")` (dosya 02:59'da değişti → **ORTAK-1**). Paketin sayaçları ✓ (ModalPencere 47/55, emoji/ikon, satır içi stil, SECICI_TEK_KAYNAK, P-37 menü).

### 20.7 Canlı doğrulama — GERÇEK uygulama (art4 `Kuyumcu.Api.dll` 5166 → kuyumcu_net; vekil 5167; başsız Chrome `betikler/tur/cdp.mjs`)
Kiracı `PAKETKATMAN-1b-TEST Olcum11` (01M3QSXERJ99D3GYKQVPKJZ44Y): davet satırı + `/auth/kayit` **201**; `/subeler` 200; 2 kasa 200/200; 6 cari + Kemal Ardıç + 11 Kemal Test **201**×18; 3 ürün **201**×3; `/pano/guncel` 200; `/kur/guncel` 404 (yeni kiracı, ilk çağrı).
**API yanıtları (okundu, `t11/arama.log`):** `Ardıç` 200 sayi 1 alt `0532 000 01 04` · `Kolye` 200 sayi 1 `KT9-1 · K14 · 4,1 g` · `05320000104` 200 sayi 1 (ham numarayla bulur) · `zzqqxx` 200 sayi 0 · `Kemal` 200 sayi 10 **dahaVar true**. SQL: `cariler.telefon='05320000104'` (ham saklanır), `urunler.gram_agirlik` 5.2500/4.1000/1.7500.
**Ölçüm: 320/320 kayıt** (4 kombinasyon × 80; BASİT ve PRO SIRAYLA; mod uyuşmazlığı 0):
| Ölçüm | Kayıt | Sonuç |
|---|---|---|
| Palet Ctrl+K 1440 | 28 | kutu `[432,90,576]`, merkez farkı 0, `data-katman` yok, açıcı **0×0** iken bağ yok (sol üst yapışma 0), perde 28, en üstte 28, aria-modal true, odak `#kabuk-arama-girdi`, yazı 18/16 px Manrope, ekran dışı 0, yatay 0 |
| Palet Ctrl+K 1024 | 28 | `[224,77,576]`, açıcı 0×0, gerisi aynı |
| Palet 375 Ctrl+K + düğme | 28+28 | `[8,8,359]` tam genişlik, açıcı 44×44, gerisi aynı |
| Çekmeceler (/cariler, /urunler, /kasalar, /vitrin × 3 genişlik) | 48 | `#cekmece-pano-kalem` **18 px TEK** (BASİT 96, PRO 96), "Yuvarlama adımı" etiketi **1** (12/12); `#cekmece-kasa` 18 TEK; `#cekmece-urun` 18 TEK; `#cekmece-cari` 18 + 15 px (OLCEK-1/CUMA-M, açık kalan); iç taşma 0, yatay 0 |
| Palet araması (5 sorgu × 3 genişlik × 2 mod × 2 tema) | 60 | `zzqqxx` → tek "Sonuç yok", simgesiz, 40 px; `Ardıç`/`Kolye` yalnız KAYITLAR; `cari` yalnız EKRANLAR; `Kemal` → tek durum satırı "Daha fazla sonuç var, adı biraz daha yazın"; süzgeç 0, noktalı gram 0, ham telefon 0, küçük yazı (<14 px) 0, ekran dışı 0, yatay 0 |
| Kenar menü 1280×900 | 16 | aktif grup açık 16/16, son grup görünür+tıklanır 16/16, kart örtme 0, yatay 0 |
| sikisma-olc.js (7 rota × 3 genişlik × 2 mod × 2 tema) | 84 | K1, K3 taşan/kırık, K5 (TRY), K7, K8, K9, K10, K11, K12, yatay → **hepsi 0**; K2 yalnız /islemler 1440 %73/%69 (SATIS-3 iki sütunlu tezgâh, § 17.6/18.7 ile aynı) |

### 20.8 Görsel kanıt (Read ile bakıldı)
`ham/TUVAL-KARSILASTIRMA/KATMAN-1b-t11-palet-cekmece-3009.png` — 6 kare: /cariler PRO karanlık 1440 Ctrl+K (üst orta, perdeli, 576 px) · /vitrin BASİT aydınlık 1024 Ctrl+K · /urunler BASİT aydınlık 375 düğme (tam genişlik) · /vitrin PRO karanlık 1440 pano çekmecesi (tek boy, "Yuvarlama adımı" bir kez) · `Kemal` BASİT aydınlık 1440 (tek sınır satırı, yalnız KAYITLAR, "0532 000 01 04") · `zzqqxx` PRO karanlık 375 (tek "Sonuç yok"). Tüm kareler `/private/tmp/kuyumcu-serit/KATMAN-1b/t11/olc11/foto/` (320 dosya).

### 20.9 Temizlik
API PID ve vekil PID `kill`; `lsof` 5166 **boş**, 5167 **boş**; t11 Chrome süreçleri 0. `kiraci-temizle.sh 01M3QSXERJ99D3GYKQVPKJZ44Y --uygula` → ikinci çağrı "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)"; davet satırı silindi (row_count 1, kalan 0); `fikstur.json` `printf '' >` ile boşaltıldı. **tenants: başta 16, sonda 20** — artış tamamen paralel şeritlerin YENİ kiracıları (`PAKETZO-ISLEM-A-TEST Dogrulama` ×4 vb.); `PAKETKATMAN-1b%` adlı kiracı **0** → bu paketin kiracı farkı 0. QA-TEST-*, 5199/5200 dokunulmadı. COMMIT YOK.

### 20.10 Değişen dosyalar (bu tur)
```
dotnet/Kuyumcu.Web/wwwroot/js/katman.js   (yalnız başlık yorumu: data-katman-acici yolunun korunma gerekçesi; kod değişmedi)
dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/KATMAN-1b-t11-palet-cekmece-3009.png
dokumanlar/09-uygulama-kayitlari/PAKET-KATMAN-1b-uygulama-raporu.md
```
Paket dosyalarının `git status` durumu: `?? katman.js, katman.css, secici-yerlesim.js, GlobalArama.razor, Pages/Vitrin.razor, Sozlukler.MenuArama.cs, Modules/Arama/` · ` M KenarCubugu.razor, Kasalar.razor` (ağaçta toplam 507 giriş, paralel şeritler).

### 20.11 Paket kapısı (30.09 t11)
| # | Durum | Kanıt |
|---|---|---|
| 1 build | ✓ | 0 Uyarı 0 Hata |
| 2 testlerim | ✓ | filtreli 632/632 |
| 3 sayaçlarım | ✓ | paketin denetim sayaçları ✓; iki ✗ başka şeritlerin (P-23 TEST-KESIF, P-36b KarZarar/ORTAK-1) |
| 4 mutasyon | ✓ | M-K1b-1, -6, -32 → adı yazılı testler kırmızı, cmp AYNI |
| 5 canlı | ✓ | 320/320 kayıt + API yanıtları + SQL |
| 6 kiracı silindi | ✓ | kendi kiracım 0; artış paralel şeritlerin |
| 7 yeni kırmızı yok | ✓ | tam süit doğrudan koşuldu: 7 kırmızı, hepsi sahipli; D1 Geçti |

### 20.12 Açık kalanlar (sahibiyle)
1. `L1bSinir D1` değişkenliği → **ZO-ISLEM-A** (`OdemeSepeti.cs` P-23 0 TL kapısı; bedelsiz alış istisnası 02:50'de eklendi, testle birlikte kararlılığı o şerit doğrulamalı).
2. KarZarar ×5 kırmızı + denetim P-36b (`KarZarar.razor:384`) → **ORTAK-1**.
3. P-23 ODEME_KALEMSIZ_YOL=2 (TEST-KESIF) → orkestratör / o şerit.
4. `denetim.sh.mutbak` (P37c) → orkestratör/kurucu (dosya silme yasak).
5. MadenDefteri yalıtık düzen → orkestratör.
6. sikisma-olc K9: opak katmanın arkasındakileri elesin → orkestratörün betiği.
7. `#cekmece-cari` 15 px → OLCEK-1 + orkestratör/kurucu. § 18.13'teki diğer maddeler sürüyor.
