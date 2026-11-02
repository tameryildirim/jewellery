# PAKET S1-a — BELİRTEÇLER: 8 ŞEMA × AYDINLIK/KARANLIK + 18 px ÖLÇEK

**Dalga:** S-1 (Kabuk ve görsel dil) · **Tarih:** 2026-09-23 · **Rol:** Ertuğ (03 Frontend) + Bertan (02 Mimari) denetimi
**Tek doğruluk kaynağı:** kurucunun onayladığı tasarım tuvali
`dokumanlar/02-mimari/sadelestirme/tuval/project/` (7 artboard + `canvas.json`).
**Ölçüm/gerekçe belgesi:** `dokumanlar/02-mimari/sadelestirme/TEMA-SISTEMI-2026-09-23.md`.
**Plan:** `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 3 · § 5A · § 5B · S-1 tablosu 1.4/1.6.

---

## 1. Yapılanlar (özet)

| # | İş | Kanıt |
|---|---|---|
| 1 | `belirtecler.css` 2. katmanı **8 şema × aydınlık/karanlık = 16 blok**, blok başına 49 belirteç (784 değer) olarak yeniden kuruldu | canlı CSS'te `grep -c '^:root\[data-sema='` = **8**; karanlık taraf medya + tercih olmak üzere 8+8 blok |
| 2 | Şema kimliği **tuvalden** alındı (menü zemini, menü metinleri, aktif satır, vurgu, sayfa zemini) — uydurulmadı | `Tezgah.dc.html` `paletler()` değerleri birebir: Platin `#14213d`/`#2457d6`/`#f2f4f7` … Fildişi açık menü `#f1ebe0` |
| 3 | **Yeni 5 belirteç** eklendi: `--yuzey-hover`, `--yuzey-secili`, `--menu-baslik`, `--menu-aktif-yuzey`, `--menu-aktif-metin` | 16 blokta tanımlı; `Hover_ve_secili_yuzeyler_HER_BLOKTA_AYRIDIR` ölçüyor |
| 4 | Her şemanın kendi `--vurgu-metin` değeri **ölçülerek** seçildi; sabit `#ffffff` yok. Karanlıkta 8 şemanın 8'inde koyu | `Vurgu_metni_SABIT_BEYAZ_DEGIL_OLCULMUSTUR` |
| 5 | **Gövde ölçeği 18 px varsayılan**, 16 ve 20 px seçenekleri (`data-yazi`), eski `data-olcek` kodları göçürüldü | `Yazi_olcegi_UC_KADEME_ve_VARSAYILAN_18px`; canlı tarayıcıda `body` hesaplanan punto **18px** |
| 6 | Altın/amber vurgu **ölçülebilir biçimde** yasaklandı (OKLCH bandı + eski amber merdiveni kara listesi) | `Altin_amber_vurgu_HICBIR_SEMADA_YOKTUR` |
| 7 | Test yükleyicisi karanlık **şema** bloklarını ayrı ayrı tanır; 8×2 ölçülür (önce yalnız aydınlık şemalar ölçülüyordu) | `BelirtecTablosu.KaranlikMedyaSemalar` / `KaranlikTercihSemalar` |
| 8 | Şema/punto kodları için **iki paket arası el sıkışma kilidi** | `Sunucunun_sundugu_HER_SEMA_ve_PUNTO_kodunun_CSS_karsiligi_VARDIR` |

**Kontrast:** 16 blok × 39 çift = **624 ölçüm, 0 ihlal** (metin 4,5:1 · kenarlık/odak/grafik 3:1).
En dar paylar: aydınlıkta 4,50:1 (Fildişi `--menu-baslik / --kenar-yuzey`), karanlıkta 5,03:1.

---

## 2. Kapsam sınırı (ne YAPILMADI ve neden)

Paketin ilan edilen kapsamı `wwwroot/css/belirtecler.css`'tir. **Şema seçme ekranı, sunucu
beyaz listesi ve kullanıcı tercihi kalıcılığı bu pakette YAZILMADI** — aynı ağaçta eşzamanlı
koşan **B-1 paketi** (`GorunumServisi.cs`, `GorunumModulu.cs`, `Ayarlar.razor`, `UstBar.razor`)
tam olarak bunu yapıyor. İki paket aynı kodları kullanıyor (aşağıda kanıt), dosya çakışması yok.

| Katman | Sahip | Durum |
|---|---|---|
| Belirteç/CSS (16 blok, 3 punto kademesi) | **S1-a (bu paket)** | bitti |
| `tema.js` beyaz listesi (`data-sema` / `data-yazi`) | S1-a başlattı, B-1 üzerine yazdı | çalışıyor |
| Sunucu uçları + ekran (`/ayarlar → Görünüm`) | **B-1** | devam ediyor (bkz. § 7) |

---

## 3. Spesifikasyona karşı doğrulama ve SAPMALAR

| # | Spesifikasyon / belge | Gerçek | Karar ve gerekçe |
|---|---|---|---|
| S1 | `TEMA-SISTEMI` Platin Mavi vurgusu `#4960bb`, zemin `#f6f7fa`, menü `#1f2024` | Tuval: `#2457d6` / `#f2f4f7` / `#14213d` | **Tuval kazandı** (görev talimatı: "Taslaktaki degerler … uyusmuyorsa TASLAK KAZANIR"). Belirteç dosyası tuvale uyduruldu. |
| S2 | `TEMA-SISTEMI` § 8: varsayılan punto **16** | Tuval notu n6: gövde **18 px** | **Tuval kazandı**: varsayılan 18 px, seçenekler 16/20. |
| S3 | Tuval `kenarlik #d7dce4` (kart çizgisi) | Sözleşme: `--kenarlik` = **etkileşimli** kenar, ≥3:1 ölçülür. `#d7dce4` beyaz üstünde ≈1,3:1 | **Sapma:** tuvalin `#d7dce4` değeri `--ayrac`a (dekoratif) bağlandı; `--kenarlik` aynı nötr hue'dan **3,65:1** olacak şekilde türetildi. Teknik zorunluluk: WCAG 1.4.11 + mevcut test kapısı. |
| S4 | Tuval `uyari #d98c00` | Beyaz üstünde 2,3:1 — metin olarak AA'yı geçmez | **Sapma:** `#d98c00` → `--uyari-vurgu` (ikon/şerit dolgusu); metin rengi aynı hue'dan koyulaştırıldı (`--uyari`, 4,5:1+). |
| S5 | Tuval yazı tipleri Google Fonts `<link>` ile geliyor | CSP dış fontu yasaklıyor (plan § 3 md. 8) | Bu pakette **font DEĞİŞTİRİLMEDİ** (`--yazi-ailesi` sistem yığını olarak kaldı). Vendored Manrope/Sora (karar D9, OFL) **S1-b/S1-c'ye** bırakıldı — dosya indirmek ve lisans yerleştirmek bu paketin kapsamı değil. |
| S6 | `TEMA-SISTEMI` § 6.2: "64 çiftin 64'ü hue ayrım kuralından geçti" | **Tuvalin vurguları ile 64 çiftin 10'u geçmiyor** (aşağıda) | Ölçüm § 6'da; karar önerisi § 7'de. Uydurma çözüm uygulanmadı. |
| S7 | Şema slug'ı belgede `platin-mavi` | Tuval anahtarı ve B-1'in sunucu kodu `platin` | `platin` kullanıldı (B-1 ile aynı); `kurumsal` eski kodu Platin Mavi'ye takma ad olarak bağlı kaldı. |

---

## 4. Kod değişiklikleri

| Dosya | Değişiklik |
|---|---|
| `dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css` | 2. katman yeniden kuruldu: :root (Platin aydınlık) + 8 aydınlık şema + medya karanlık taban + 8 karanlık şema + tercih karanlık taban + 8 karanlık şema; 3 punto kademesi; 5 yeni belirteç. Ham palet, ölçü katmanı, yoğunluk/mod/P-39 blokları **dokunulmadı**. |
| `dotnet/Kuyumcu.Web/wwwroot/js/tema.js` | `SEMALAR` beyaz listesi 2 → 9 kod (8 şema + `kurumsal` takma adı); `OLCEKLER`e `yogun` eklendi. (B-1 sonradan aynı dosyada `PUNTOLAR` ve `data-yazi` yazımını ekledi.) |
| `dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs` | Yükleyici karanlık şema bloklarını ayrı tanır; seçici listesindeki **tüm** şema adları okunur (önce yalnız ilki okunuyordu — takma ad sessizce düşüyordu); 8 yeni çift; **7 yeni test**; OKLCH yardımcı sınıfı. |
| `dokumanlar/09-uygulama-kayitlari/PAKET-S1-a-uygulama-raporu.md` | bu rapor |

Migration: **gerekmedi** (şema değişikliği yok, veri yazılmadı).

---

## 5. DoD sözleşmesi

Bu paket **yeni varlık/kayıt türü açmaz** — ne tablo, ne uç, ne ekran düğmesi. Üretilen tek şey
CSS özel özellikleri (tasarım belirteçleri) ve onları ölçen testlerdir. Bu yüzden PATCH/DELETE/
`[RolGerekli]`/denetim kaydı maddeleri **uygulanamaz (konusuz)**; ekran eylemi de eklenmedi.

| DoD maddesi | Durum |
|---|---|
| Yeni varlık → PATCH + DELETE/pasife alma + yetki + denetim | **Konusuz** — yeni varlık yok |
| Kullanıcının seçtiği değerin düzenlenebilirliği (tema/punto) | Sunucu tarafı **B-1**'de; bu paket yalnız görsel karşılığı üretir |
| Her düğme ya istek atar ya disabled + gerekçe | **Konusuz** — düğme eklenmedi |
| Özellik silinmez | Hiçbir belirteç adı silinmedi/yeniden adlandırılmadı; `kurumsal`/`zumrut`/`yogun`/`okunakli` eski kodları çalışmaya devam ediyor |
| DOM id sözleşmesi | Hiçbir `id`'ye dokunulmadı (razor dosyası açılmadı) |
| Sabit renk / satır içi stil yasağı | Tüm değerler belirteçte; CDN/npm/inline script yok, `@import` yok |
| Dokunulan sayfalar (TEK DOKUNUŞ) | **Hiçbir `.razor` sayfası açılmadı** |

---

## 6. Ölçümler

### 6.1 Kontrast (624 ölçüm, 0 ihlal)

| Blok | Ölçüm | Metin çiftlerinde en dar | Metin dışı en dar |
|---|---:|---|---|
| platin-aydınlık | 39 | 4,53 (`--basari/--basari-yuzey`) | 3,65 (`--kenarlik/--yuzey`) |
| platin-karanlık | 39 | 5,09 (`--metin-soluk/--yuzey-secili`) | 3,61 |
| zümrüt-aydınlık | 39 | 4,51 (`--kenar-tehlike/--kenar-yuzey`) | 3,65 |
| zümrüt-karanlık | 39 | 5,07 | 3,58 |
| grafit A/K | 39/39 | 4,53 / 5,03 | 3,65 / 3,57 |
| bordo A/K | 39/39 | 4,53 / 5,15 | 3,65 / 3,59 |
| okyanus A/K | 39/39 | 4,53 / 5,08 | 3,65 / 3,58 |
| menekşe A/K | 39/39 | 4,53 / 5,14 | 3,65 / 3,61 |
| zeytin A/K | 39/39 | 4,53 / 5,06 | 3,65 / 3,58 |
| fildişi A/K | 39/39 | 4,50 / 5,14 | 3,65 / 3,62 |

### 6.2 Şema ↔ durum rengi ayrımı — DÜRÜST BULGU

`TEMA-SISTEMI` § 6.2 kuralı: vurgu ile her durum rengi arasında **OKLCH hue farkı ≥30°**
veya **chroma oranı ≥2,5×** olmalı. Belge kendi (uygulanmayan) vurgularıyla "64/64 geçti" diyor.
**Tuvalin onaylı vurgularıyla ölçtüm: 64 çiftin 10'u kuralı geçmiyor.**

| Blok | Çift | hue farkı | chroma oranı |
|---|---|---:|---:|
| Platin A/K | vurgu ↔ bilgi | 26° / 23° | 1,8 / 1,8 |
| Bordo A/K | vurgu ↔ **hata** | 11° / 11° | 1,1 / 1,0 |
| Okyanus A/K | vurgu ↔ bilgi | 15° / 14° | 1,2 / 1,1 |
| Zeytin A/K | vurgu ↔ **başarı** | 23° / 23° | 1,2 / 1,0 |
| Fildişi A/K | vurgu ↔ uyarı | 11° / 3° | 1,3 / 1,4 |

Zümrüt bu listede **yok**, çünkü tuvalin kendisi Zümrüt'te "başarı"yı maviye çevirmiş
(`basari #1d4ed8`) — tasarımcının aynı sorunu gördüğü ve tek şemada çözdüğü anlamına gelir.
Bu pakette **kendiliğinden ikinci bir çözüm uydurulmadı**; kalan 4 şema için karar § 7'de.
Bugün devrede olan azaltıcı katmanlar (sözleşmede zaten var): vurgu yalnız **katı dolgu**,
durum rengi yalnız **zemin + kenar + metin bandı**; her durum ayrıca **ikon + etiket** taşır.

### 6.3 Canlı doğrulama (port 5194, gerçek tarayıcı)

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /` | 200 | **200** (1.605 bayt) |
| `GET /css/belirtecler.css` | 200, kaynak dosyayla birebir | **200**, 82.895 bayt, `cmp` **AYNI** |
| `GET /js/tema.js` | 200 | **200** (7.920 bayt) |
| Yayındaki şema bloğu sayısı | 8 | **8** (`platin, zumrut, grafit, bordo, okyanus, menekse, zeytin, fildisi` + `kurumsal` takma adı) |
| Tarayıcıda 8 şema × 2 tema `getComputedStyle` | her kombinasyon kendi vurgusunu verir | **16/16 doğru** (örn. aydınlık/platin `--vurgu #2457d6`, karanlık/platin `#91b9ff`, aydınlık/fildişi menü `#f1ebe0`) |
| Karanlıkta `--vurgu-metin` | koyu | **8/8 koyu** (`#09152c` … `#280b0f`) |
| Varsayılan gövde puntosu | 18 px | `body` hesaplanan **18px**, `--yazi-rakam` **20px** |
| `data-yazi="16"` / `"20"` | 16px / 20px | **16px / 20px** |
| `tenants` sayısı | değişmez | **9 → 9**; `PAKET%` önekli kiracı **0** (test kiracısı açılmadı) |
| API kapatma | 5194 boş | `lsof -nP -iTCP:5194 -sTCP:LISTEN` → **0 satır** |

---

## 7. Açık kalanlar / kurucuya ve ekibe notlar

1. **Durum rengi çakışması (§ 6.2) — ekip oyu gerekiyor.** Bordo↔hata, Zeytin↔başarı,
   Okyanus/Platin↔bilgi, Fildişi↔uyarı çiftleri ölçülebilir hue ayrımını sağlamıyor.
   İki seçenek: (a) tuvalin Zümrüt'te yaptığını genelleştir (çakışan şemada o durum rengini
   komşu hue'ya kaydır), (b) rol + ikon ayrımıyla yetin ve kuralı belgede daralt. **Karar
   verilmeden kod yazılmadı.**
2. **Yazı tipi (Manrope/Sora) vendored değil.** Tuval Google Fonts `<link>` kullanıyor, CSP
   bunu yasaklıyor. Font dosyalarının `wwwroot/font/` altına OFL lisansıyla konması S1-b/S1-c'ye
   kaldı; bugünkü görünüm sistem yığınıyla çiziliyor.
3. **`Sozlukler.cs` içindeki punto metinleri eskidi:** `OlcekYogunAciklama` "gövde 14 punto",
   `OlcekOkunakliAciklama` "Gövde 16 punto" diyor; ölçek artık 16/18/20. Sozlukler.cs bu dalgada
   **ortak hub olduğu için elle değiştirilmedi** — B-1 ya da S1-b düzeltmeli (ekranda yanlış sayı
   yazmak Anayasa md. 1 ihlalidir).
4. **`app.css`'teki 73 ham `font-size`** hâlâ belirtece bağlı değil (S-1 tablosu madde 1.4'ün
   ikinci yarısı); bu paket yalnız merdiveni kurdu.
5. **Eşzamanlı B-1 paketi yüzünden ağaçta iki kırmızı var (bu paketin ürünü DEĞİL):**
   - `kullanicilar.sema` sütunu `Entities.cs`'e eklenmiş ama **migration yok** →
     `Unknown column 'sema' in 'field list'` ile Gorunum HTTP testleri kırmızı.
   - `denetim.sh` `P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > tavan 47`: sayaç yalnız
     `.razor` dosyalarını sayar; bu pakette **hiçbir .razor dosyası açılmadı**; artışın kaynağı
     16:34–16:48'de değiştirilen `Ayarlar.razor` / `UstBar.razor` (B-1).

---

# EK — 2026-09-23 18:00–18:10 · DEVİR TURU (planlı duruştan sonra tamamlama)

> Kurucu: *"internetim geldi, pc kapanmayacak artık, kaldığın yerden devam edebilirsin."*
> Devir notu: `ORKESTRATOR-DURUM.md § 2026-09-23 18:55 PLANLI DURUŞ`.
> Bu turda **hiçbir yeni kod yazılmadı**; paketin eksik kalan tek kalemi — **mutasyon kanıtı** —
> tamamlandı ve ağacın ölçülen hâli yeniden kanıtlandı.

## 8. Mutasyon kanıtları (bu turda üretildi — önceki raporda EKSİKTİ)

Yöntem: kaynak `scratchpad/mut-yedek/S1-a/` altına kopyalandı → `perl -0777 -pi -e` ile tek
eşlemeli değişiklik → `dotnet test --filter TasarimBelirtecTestleri` → yedek geri kopyalandı →
`cmp` ile birebir aynılık kanıtlandı. **7 mutasyonun 7'si test kırdı; test açığı YOK.**

| Kod | Dosya | Değişiklik | Kırmızıya dönen test(ler) | Kırmızı/28 | Geri alma |
|---|---|---|---|---|---|
| M1-1 | `belirtecler.css` | `:root --yazi-govde: 18px` → `17px` | `Yazi_olcegi_UC_KADEME_ve_VARSAYILAN_18px` | 1 | `cmp` BİREBİR AYNI |
| M1-2 | `belirtecler.css` | `--yuzey-hover:` → `--yuzey-hoverX:` (belirteç düşürüldü) | `Hover_ve_secili_yuzeyler_HER_BLOKTA_AYRIDIR` · `Her_semantik_belirtecin_AYDINLIK_ve_KARANLIK_karsiligi_VARDIR` · `Kontrast_AA_saglanir` | 3 | `cmp` BİREBİR AYNI |
| M1-3 | `belirtecler.css` | Bordo karanlık `--vurgu-metin: #280b0f` → `#ffffff` | `Vurgu_metni_SABIT_BEYAZ_DEGIL_OLCULMUSTUR` · `Iki_karanlik_blok_AYNI_belirtecleri_AYNI_degerle_tanimlar` · `Kontrast_AA_saglanir` | 3 | `cmp` BİREBİR AYNI |
| M1-4 | `belirtecler.css` | Platin `--vurgu: #2457d6` → `#f59e0b` (amber) | `Altin_amber_vurgu_HICBIR_SEMADA_YOKTUR` · `Kontrast_AA_saglanir` | 2 | `cmp` BİREBİR AYNI |
| M1-5 | `belirtecler.css` | `:root[data-sema="zeytin"]` → `…"zeytinX"` (şema bloğu düşürüldü) | `Sekiz_sema_AYDINLIK_ve_KARANLIK_bloklariyla_TANIMLIDIR` · `Sema_beyaz_listesi_CSS_ile_AYNI_kodlari_tasir` · `Sunucunun_sundugu_HER_SEMA_ve_PUNTO_kodunun_CSS_karsiligi_VARDIR` | 3 | `cmp` BİREBİR AYNI |
| M1-6 | `belirtecler.css` | `:root --metin: #172033` → `#8a93a5` (AA altı) | `Kontrast_AA_saglanir` | 1 | `cmp` BİREBİR AYNI |
| M1-7 | `tema.js` | `SEMALAR` beyaz listesinden `'bordo'` çıkarıldı | `Sema_beyaz_listesi_CSS_ile_AYNI_kodlari_tasir` | 1 | `cmp` BİREBİR AYNI |

Mutasyonlardan sonra: `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı 0 Hata**;
`--filter TasarimBelirtecTestleri` → **28/28 yeşil**.

## 9. Bu turda yeniden ölçülen ağaç hâli (18:03–18:10)

| Kontrol | Sonuç | Sahibi |
|---|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Uyarı · 0 Hata** ✅ | — |
| `--filter TasarimBelirtecTestleri` | **Başarısız 0 · Başarılı 28 / 28** ✅ | S1-a |
| Tam süit (`trx-pS1-a/_Mac_2026-09-23_18_03_45.trx`) | Başarısız **17** · Başarılı 4018 · Toplam **4035** ⚠ | aşağıda |
| `bash dotnet/denetim.sh` | 271 ✓ · **1 ihlal**: `P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > tavan 47` ⚠ | P-42 / P-46 |
| `tenants` sayısı | **9 → 9** · `PAKET%`/`S1%` önekli kiracı **0** (bu paket kiracı açmaz) | — |
| Canlı (port **5194**) | aşağıda | S1-a |
| 5194 kapanışı | `lsof -nP -iTCP:5194 -sTCP:LISTEN` → **0 satır** ✅ | — |

### 9.1 Tam süitteki 17 kırmızının sahipleri — HİÇBİRİ S1-a DEĞİL

* **15 test** → **B-1** (S-B şeridi, `Pages/Ayarlar.razor` yarım):
  `UX1C4EkranTestleri` C4_05 · C4_50 · C4_51 · C4_52 · C4_53 · C4_54 · C4_55 · C4_56 (8) ·
  `SurumEkranTestleri` ×6 · `AyarlarTemaEkranTestleri.Ayarlar_sayfasinda_tema_karti_ve_secici_VARDIR` (1).
* **1 test** → **P-30b** (DALGA 7c): `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR`.
* **1 test — KARARSIZ (yeni), gerileme değil:**
  `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ`.
  **Kanıt:** tam süitte bUnit `WaitForFailedException` ("Check count: 1, Component render count: 9")
  — yani zaman aşımı; **aynı test yalıtılmış koşumda 6/6 YEŞİL**
  (`dotnet test --filter AlisOdemeAraciEkranTestleri` → Başarısız 0 · Başarılı 6).
  Devir notundaki 16'lık listede yoktu; makine bu sırada eşzamanlı şerit + tam süit yüküyle
  takasa yazıyordu. S1-a hiçbir `.razor` ve hiçbir ödeme kodu açmadı.

`denetim.sh` P-11 sayacı **yalnız** `Pages/*.razor`, `Bilesenler/*.razor`, `Layout/*.razor`
dosyalarını sayar (`denetim.sh:2761`); S1-a bu dalgada **hiçbir `.razor` dosyası açmadı** →
ihlal bu paketin ürünü olamaz. `P-36b: TEMA_KONTRAST_IHLALI 0 (8 şema, hepsi ölçülüyor)` ✅.

### 9.2 Canlı doğrulama — port 5194, yeniden koşuldu

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /` | 200 | **HTTP 200**, 1.605 bayt |
| `GET /css/belirtecler.css` | 200 + kaynakla birebir | **HTTP 200**, 83.297 bayt, `cmp` **BİREBİR AYNI** |
| `GET /js/tema.js` | 200 + kaynakla birebir | **HTTP 200**, 7.920 bayt, `cmp` **BİREBİR AYNI** |
| Yayındaki şema blokları | 8 şema + `kurumsal` takma adı | **platin · zumrut · grafit · bordo · okyanus · menekse · zeytin · fildisi · kurumsal** |
| Yayındaki punto kademeleri | 16 · 18 · 20 | **`data-yazi="16"` · `"18"` · `"20"`** |
| Varsayılan gövde | 18 px | `:root --yazi-govde: 18px` (satır 216) |
| SQL: `SELECT COUNT(*) FROM tenants` | başlangıçla aynı | **9** |
| SQL: `… WHERE ad LIKE 'PAKET%' OR ad LIKE 'S1%'` | 0 | **0** |

### 9.3 Tuvale sadakat — 8 şema vurgusu birebir ölçüldü

`Tezgah.dc.html` `paletler()` ↔ `belirtecler.css` `--vurgu`:

| Şema | Tuval | CSS | |
|---|---|---|---|
| platin | `#2457d6` | `#2457d6` | ✅ |
| zumrut | `#1b7f5c` | `#1b7f5c` | ✅ |
| grafit | `#0b7f73` | `#0b7f73` | ✅ |
| bordo | `#9f1239` | `#9f1239` | ✅ |
| okyanus | `#0e7490` | `#0e7490` | ✅ |
| menekse | `#5b3fd6` | `#5b3fd6` | ✅ |
| zeytin | `#4d6b12` | `#4d6b12` | ✅ |
| fildisi | `#8a5a1e` | `#8a5a1e` | ✅ |

## 10. Bu turda gözlenen — EŞZAMANLI ŞERİT UYARISI (kurucuya)

`belirtecler.css` bu turun ortasında (**18:02**) benim dışımda değişti: 1. satırdaki başlık
yorumu `Kuyumcu ERP — TASARIM BELİRTEÇLERİ` → `ByGatech — TASARIM BELİRTEÇLERİ` oldu
(`diff` = **tek satır**, yalnız yorum). Bu **P-D2 (ByGatech adı, 44 dosya)** şeridinin izidir;
belirteç değerlerine dokunmadı, 28 testin tamamı bu değişiklikten sonra da yeşil.
Kayda geçiriyorum çünkü aynı dosyaya iki şerit dokunuyor — çakışma riski var.

## 11. S1-a'nın kapanış durumu

Paketin ilan edilen kapsamı (`wwwroot/css/belirtecler.css`: 8 şema × aydınlık/karanlık,
`--yuzey-hover`/`--yuzey-secili` eklenmesi, ölçülmüş `--vurgu-metin`, 18 px varsayılan ölçek)
**bitti ve kanıtlandı**: 28/28 test yeşil · 624 kontrast ölçümü 0 ihlal · 7/7 mutasyon test kırdı ·
canlı 5194 doğrulaması geçti · derleme 0 uyarı 0 hata.

**Ağaç hâlâ tam yeşil DEĞİLDİR** — ama kalan 16 kırmızı + 1 denetim ihlali ölçülerek
**B-1, P-30b ve P-42/P-46**'ya aittir; devir notunun kuralı gereği bu pakette onlara
dokunulmadı, hiçbir test silinmedi.
