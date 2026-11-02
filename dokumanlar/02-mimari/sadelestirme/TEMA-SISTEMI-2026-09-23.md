# BYGATECH — TEMA SİSTEMİ: 8 RENK ŞEMASI × AYDINLIK/KARANLIK × 3 YAZI BOYUTU

> **Tarih:** 23 Eylül 2026 · **Yazan:** Ertuğ (03 Frontend) + Bertan (02 Baş Mimar), Tamer Bey koordinasyonu.
> **Bağlayıcı kaynak:** `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 5A (Görsel yön — "Sakin Lüks") ve § 5B (kullanıcı profili).
> **Ürün adı:** **BYGATECH** (kurucu talimatı, 23 Eylül). Bu belgede ve üretilen CSS'te ürün adı ByGatech'tir.
> **Çıktı ikizi:** `belirtecler-8sema.css` (aynı klasör) — S-1'in doğrudan taşıyacağı saf CSS.
> **Durum:** BELGE. Kod yazılmadı. `dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css` **yalnız okundu** (520 satır), hiçbir `.cs/.razor/.css` üretim dosyasına dokunulmadı.

---

## 0. Bir bakışta karar

| Soru | Karar | Nerede |
|---|---|---|
| Kaç şema? | **8** — Platin Mavi · Zümrüt · Grafit · Bordo · Okyanus · Menekşe · Zeytin · Fildişi | § 4 |
| Varsayılan şema | **Platin Mavi · aydınlık · 16 px** (ekip oyu, kurucuya sorulmadı) | § 7 |
| Karanlık tema nasıl üretildi? | Ters çevrilerek **DEĞİL** — 16 değer bloğu ayrı hesaplandı | § 5 |
| Kaç belirteç? | Blok başına **49 bildirim** = 13 çekirdek (§5A) + 2 menü (§5A) + 34 türetilmiş/durum/grafik/kenar-çubuğu | § 3 |
| Kaç ölçüm? | **496** çift (8 şema × 2 tema × 31 çift) | § 6 |
| AA geçen | **496 / 496** — AA'yı geçmeyen tek değer bırakılmadı | § 6 |
| Altın/amber | **0 ihlal** (48 vurgu değeri objektif testten geçti) | § 2 |
| Yazı boyutu | 16 / 18 / 20 px, `--yazi-govde` + tam ölçek | § 8 |
| Backend | `kullanicilar` tablosuna 3 kolon + 1 migration + 1 uç | § 9 |
| Demo sırası | 11 sahne, ~10 dk (kurucu talimatı, Pusula 10:51 demosundan çıkarıldı) | § 10 |

---

## 1. Çözülen çelişkiler (planın kendi içindeki ve araştırmalar arasındaki)

| # | Çelişki | Karar | Gerekçe |
|---|---|---|---|
| Ç-1 | **§ 8.1 (eski):** "üçten biri: A Altın Klasik / B Platin Mavi / C Zümrüt" ↔ **§ 5A (23 Eylül):** 8 şema, altın/amber yok | **§ 5A kazanır.** "A/B/C" adlandırması ölüdür; 8 ad geçerlidir | § 5A daha yeni ve kurucunun birebir talimatı ("altın/amber beğenilmedi") |
| Ç-2 | § 5A "Renk" satırı hâlâ "1 marka vurgusu (altın amber, kurucu A/B/C'den seçer)" diyor ↔ aynı § 5A'nın TEMA paragrafı "altın/amber yok" diyor | **TEMA paragrafı kazanır** (aynı bölümün daha sonra yazılmış, daha özel hükmü) | Kurucunun son sözü; tablo satırı güncellenmemiş artık metindir |
| Ç-3 | Araştırma 3 & 4: **odak halkası şemadan türetilmeli** (Radix adım 8) ↔ **odak halkası nötr olmalı** (Carbon: karanlıkta saf beyaz) | **Şemadan türetilir, ama İKİ PARÇALI halka zorunlu:** içte `--yuzey` renginde 2 px boşluk + dışta `--odak-halka` 2 px | Tek parçalı halka birincil düğmenin üstünde kaybolur. İki parçalı halka WCAG 2.2 SC 1.4.13 "değişim kontrastı"nı da karşılar ve marka kimliğini korur. Her şemada halka **hem `--yuzey` hem `--yuzey-govde`'ye karşı ≥3:1 ölçüldü** (16 ölçüm, 16 geçti) |
| Ç-4 | Araştırma 1: `--yuzey-hover` / `--yuzey-secili` **eklenmeli** (Radix 4-5) ↔ Araştırma 3: "13-15 belirteçte kal, her yeni belirteç 16 değere çarpılır" | **Eklendi.** Maliyet kabul edildi (16 × 2 = 32 ek değer) | Yoksa 8 şema da hover/seçili durumda ham renge düşer; liste ekranı BYGATECH'in en çok kullanılan yüzeyidir |
| Ç-5 | Araştırma 4: "durum renkleri şemadan **bağımsız** olmalı" ↔ görev metni: "4 durum rengi her şema × tema için" | **İkisi de doğru:** durum renginin *hue/chroma'sı* şemadan bağımsız sabittir (M3 error-paleti kuralı), ama *L'si* her şemanın kendi yüzeyine karşı ayrı ölçülüp ayarlanır | Anlam sabit kalır, kontrast her şemada garanti edilir |
| Ç-6 | "ALTIN/AMBER YOK" ↔ uyarı semantiği evrensel olarak sarı/kehribardır | **Yasak yalnız MARKA VURGUSU içindir.** `--uyari*` ailesi okra/kehribar kalır | Kurucunun reddettiği şey ürünün kimlik rengiydi, trafik ışığı değil. Objektif test yalnız `--vurgu`, `--vurgu-guclu`, `--odak-halka` üzerinde koşar |
| Ç-7 | Görev metni: "`data-sema` YOK" ↔ ölçüm: `belirtecler.css` 245-274. satırlarda `[data-sema="kurumsal"]` ve `[data-sema="zumrut"]` ZATEN var | **Ölçüm kazanır.** CSS'te `data-sema` var (P-36b), `TemaServisi.cs`'te yok. İkisi ayrı katman | Doğrudan dosyadan okundu. S-1'in işi: CSS'teki 2 eski şemayı (`kurumsal`, `zumrut`) 8 yeni şemayla değiştirmek ve `data-sema`yı servise bağlamak. `zumrut` adı korunur, değerleri yenilenir; `kurumsal` → `platin-mavi` olarak yeniden adlandırılır (eski ad geriye dönük takma ad olarak bırakılabilir) |
| Ç-8 | Plan § 5A: "kontrast testi 4 × 2 = 8 kombinasyon ölçer" ↔ 8 şema kararı | **8 × 2 = 16 kombinasyon.** Plandaki "4×2=8" sayısı eskidir, bu belge onu günceller | Aritmetik |
| Ç-9 | Araştırma 2: "karanlıkta kontrast bandı 12:1–16:1" ↔ WCAG'da tavan yoktur | **Bant ekip kararı olarak uygulandı** (kaynak diye gösterilmiyor); gövde metni 8 şemada 12,00–12,11:1 ölçüldü | Halation riski; WCAG tabanı (4,5:1) zaten fazlasıyla aşılıyor |
| Ç-10 | §5A belirteç adları `menuBaslik` / `menuAktifMetin` (camelCase) ↔ projenin tamamı kebab-case | **Kebab-case kazanır:** `--menu-baslik`, `--menu-aktif-metin` | Mevcut 520 satırlık dosyanın tek yazım kuralı; CSS özel niteliği büyük/küçük harfe duyarlıdır, karışık yazım tuzaktır |
| Ç-11 | Araştırmada Material `#121212`, `15,8:1`, `%87/%60/%38` gibi sayılar **birincil kaynaktan doğrulanamadı** | Bu sayılar **hiçbir değere dayanak yapılmadı**; yerlerine kendi ölçümlerimiz kondu | Anayasa md. 1 |

---

## 2. Altın/amber yasağının OBJEKTİF testi

"Altın değil" his değildir. Uygulanan ölçülebilir kural:

> Bir renk **altın/amber sayılır** ⇔ OKLCH hue ∈ [55°, 100°] **VE** chroma ≥ 0,10 **VE** L ≥ 0,55.

Bu test 8 şema × 2 tema × 3 belirteç (`--vurgu`, `--vurgu-guclu`, `--odak-halka`) = **48 değer** üzerinde koşturuldu: **ihlal 0**.

En yakın aday **Fildişi**'dir (hue 50°, sıcak taupe-bronz). Chroma'sı bilerek **0,033**'e çekildi — kehribar eşiğinin (0,10) **üçte birinden az**. Işıkta kahve/taupe okunur, altın okunmaz. Bugünkü `belirtecler.css`'in amber vurgusu (`--vurgu: #b45309`, `--odak-halka: #b45309`, `--grafik-1: #b45309`, karanlıkta `#f0b429` / `#fcd34d`) **ölüdür** ve S-1'de bu 8 şemanın hiçbirine girmez.

Ham `--amber-50…800` merdiveni dosyada **kalır** (özellik silinmez kuralı) ama yalnız `--uyari*` ailesini besler.

---

## 3. Belirteç sözleşmesi — her belirtecin TEK işi

### 3.1 Çekirdek 13 (§ 5A'nın istediği set)

| # | Belirteç | Radix adım karşılığı | Rolü (tek cümle) |
|---:|---|:--:|---|
| 1 | `--yuzey-govde` | 1 | Sayfanın altındaki en geniş zemin; asla vurgu rengi almaz. |
| 2 | `--yuzey` | 2 | Kart, üst bar, modal ve girdinin durduğu yüzey — aydınlıkta en açık, karanlıkta gövdeden bir kademe açık. |
| 3 | `--yuzey-2` | 3 | Tablo başlığı ve hafif bölüm zemini (bileşen dolgusunun normal hâli). |
| 4 | `--yuzey-3` | 3+ | Pill grubu, gri rozet, katlanmış bölüm — yüzeyin en belirgin kademesi. |
| 5 | `--metin` | 11 | Gövde metni; her yüzey kademesine karşı ≥4,5:1 ölçülür. |
| 6 | `--metin-guclu` | 12 | Başlık ve rakam; `--yuzey`'e karşı ≥7:1 ölçülür, karanlıkta **asla `#ffffff` değildir**. |
| 7 | `--metin-soluk` | 11− | Etiket, yardım metni, ikincil bilgi; yine ≥4,5:1 (soluk ≠ okunmaz). |
| 8 | `--kenarlik` | 7 | **Etkileşimli** bileşen kenarı (girdi, seçici, ikincil düğme); ≥3:1 ölçülür. |
| 9 | `--ayrac` | 6 | Salt dekoratif ayraç çizgisi; WCAG 1.4.11 kapsamı dışında, kontrast ölçülmez. |
| 10 | `--vurgu` | 9 | Şemanın katı marka rengi: birincil düğme dolgusu, aktif gösterge; sayfada ≤3 öğe. |
| 11 | `--vurgu-guclu` | 10 | Vurgunun hover/basılı hâli — aydınlıkta koyulaşır, **karanlıkta açılır**. |
| 12 | `--vurgu-metin` | 9 üstü | Vurgu dolgusunun üstündeki ön plan; **her şemada ölçülerek** kırık-beyaz veya koyu seçilir, sabit `#ffffff` değildir. |
| 13 | `--vurgu-yuzey` | 3 (vurgu ailesi) | Yumuşak vurgu zemini (seçili sekme, bilgi kartı); aydınlıkta çok açık, **karanlıkta koyu-doygun** (M3 container ton 90→30). |

### 3.2 Menü 2 (§ 5A'nın ayrıca istediği — açık menü için)

| # | Belirteç | Rolü |
|---:|---|---|
| 14 | `--menu-baslik` | Kenar çubuğundaki **grup başlığı** metni (Tanımlar, Raporlar…); satır metninden bir kademe soluk ama yine ≥4,5:1. |
| 15 | `--menu-aktif-metin` | **Aktif menü satırının** metni; kendi zemini `--menu-aktif-yuzey`'e karşı ≥4,5:1 ölçülür. |

### 3.3 Türetilmiş 34 (araştırmanın zorunlu kıldıkları + mevcut aile)

| Belirteç(ler) | Neden var |
|---|---|
| `--yuzey-hover`, `--yuzey-secili` | Radix adım 4/5 karşılığı; yoksa 8 şema da hover/seçili durumda ham renge düşer (Araştırma 1, K8). Karanlıkta fark aydınlıktakinden **daha büyük** tutuldu. |
| `--vurgu-kenar` | Yumuşak vurgu yüzeyinin kenarı (Radix 6/7 vurgu ailesi). |
| `--odak-halka` | `:focus-visible`ın tek kaynağı; iki parçalı halkanın dış parçası (Ç-3). |
| `--basari/-vurgu/-yuzey/-kenar`, `--uyari…`, `--tehlike…` (+`-dolgu`, `-dolgu-metin`), `--bilgi…` | 4 durum × 4 parça = 18 bildirim; § 6'da ayrıntılı. |
| `--kenar-yuzey`, `--kenar-yuzey-2`, `--kenar-metin`, `--kenar-metin-guclu`, `--kenar-ayrac`, `--kenar-tehlike`, `--menu-aktif-yuzey` | Kenar çubuğu ailesi — **Fildişi açık menü** şeması bunları şemaya bağlamayı zorunlu kıldı; diğer 7 şemada koyu marka menüsü, Fildişi aydınlıkta açık menü. |
| `--grafik-1…5` | Seri ayırt edici; anlamı efsane metni taşır, renk taşımaz. |

**Toplam: blok başına 49 bildirim × 16 blok = 784 elle hesaplanmış değer.**

> **Ad değişmedi.** Yukarıdaki 49 adın **44'ü bugünkü `belirtecler.css`'te zaten var.** Yeni eklenen 5: `--yuzey-hover`, `--yuzey-secili`, `--menu-baslik`, `--menu-aktif-yuzey`, `--menu-aktif-metin`. Hiçbir ad silinmedi, hiçbiri yeniden adlandırılmadı — S-1 bu paleti **isim eşleştirmesi yapmadan** uygulayabilir.

---

## 4. Sekiz şema

| Şema | `data-sema` | Karakter (tek cümle) | Aydınlık vurgu | Karanlık vurgu |
|---|---|---|---|---|
| **Platin Mavi** | `platin-mavi` | Sakin kurumsal mavi — bankacılık/ERP refleksi; en geniş kontrast payı, en az tartışma çıkaran şema. | `#4960bb` | `#a4b8f5` |
| **Zümrüt** | `zumrut` | Kuyum yeşili — zümrüt/teal, vitrin yeşiline yakın ama başarı yeşilinden 33° uzakta tutulmuş. | `#007b69` | `#8dc9ba` |
| **Grafit** | `grafit` | Renksiz profesyonel — vurgu neredeyse nötr (chroma 0,037); ekranda tek renk rakamlardır. | `#5c6a7f` | `#b1bbca` |
| **Bordo** | `bordo` | Kuyumcu kadifesi — şarap/mürdüm; sıcak ama tehlike kırmızısından 32° ayrı. | `#a2426d` | `#e6a4bd` |
| **Okyanus** | `okyanus` | Serin deniz mavi-yeşili — Zümrüt'ün soğuk kardeşi, bilgi mavisinden 40° uzakta. | `#00787b` | `#88c8ca` |
| **Menekşe** | `menekse` | Modern SaaS moru — en genç dil; rakip PusulaX'in mor gradyanının sakin, gradyansız karşılığı. | `#7d4fab` | `#c8ace9` |
| **Zeytin** | `zeytin` | Zeytin yeşili/haki — sıcak ve alışılmadık; başarı yeşilinden 33° ayrı tutuldu. | `#6a6e2f` | `#bbbe98` |
| **Fildişi** | `fildisi` | AÇIK MENÜ şeması — fildişi/ivory kağıt zemin, bronz-taupe vurgu; koyu kenar çubuğu istemeyen dükkân için. | `#796458` | `#c6b7af` |

### 4.1 Karanlık tarafın tek cümlelik farkı (şema başına)

| Şema | Karanlıkta ne değişti |
|---|---|
| **Platin Mavi** | Gece indigosu: vurgu ton 40'tan ton 80'e açılır, üstündeki metin koyuya döner. |
| **Zümrüt** | Soluk zümrüt: doygunluk %38 düşürüldü, koyu zeminde titremiyor. |
| **Grafit** | Grafit gümüşü: karanlıkta bile kromatik değil, uyum/denetim ekranları için en sakin seçenek. |
| **Bordo** | Gül kurusu: karanlıkta pastelleşir, kırmızı bantla karışmaz. |
| **Okyanus** | Buzlu teal: doygunluk düşük, uzun tablo oturumlarında yormuyor. |
| **Menekşe** | Lila: karanlıkta en yüksek algısal parlaklık, vurgu öğesi ≤3 kuralı burada en kritik. |
| **Zeytin** | Soluk haki: karanlıkta sarıya kaçmaması için hue 111'de sabitlendi. |
| **Fildişi** | Karanlıkta menü de koyulaşır (açık menü aydınlıkta kalır); vurgu sıcak kum tonuna açılır. |

### 4.2 Zemin ve menü (şema kimliğinin asıl taşıyıcısı)

| Şema | Aydınlık sayfa zemini | Aydınlık kart | Karanlık sayfa zemini | Karanlık kart | Menü zemini (aydınlık) |
|---|---|---|---|---|---|
| Platin Mavi | `#f6f7fa` | `#fefeff` | `#0f1117` | `#1c1e24` | `#1f2024` |
| Zümrüt | `#f4f8f7` | `#fdfffe` | `#0c1312` | `#18201e` | `#1d2220` |
| Grafit | `#f6f7f9` | `#feffff` | `#0f1215` | `#1c1e22` | `#1f2123` |
| Bordo | `#faf6f7` | `#fffeff` | `#160f12` | `#231c1e` | `#231f21` |
| Okyanus | `#f4f8f8` | `#fdffff` | `#0b1313` | `#172020` | `#1d2222` |
| Menekşe | `#f8f6fa` | `#fffeff` | `#131016` | `#1f1d23` | `#212023` |
| Zeytin | `#f7f7f4` | `#fffffd` | `#12120c` | `#1e1f18` | `#20211d` |
| Fildişi | `#fdf5f1` | `#fffefe` | `#17100c` | `#241c18` | `#fbf2ec` **(AÇIK MENÜ)** |

> **Saf siyah/saf beyaz yok:** hiçbir karanlık zemin `#000000`, hiçbir karanlık gövde metni `#ffffff` değildir. Ölçülen uçlar: en koyu değer Bordo'nun karanlık menü zemini `#100a0c`, en açık karanlık metin Zümrüt'ün `--metin-guclu`'sü `#e2e9e7`.

---

## 5. Karanlık tema — uygulanan 12 kural

Karanlık tema aydınlığın ters çevrilmişi **değildir**; 16 blok ayrı hesaplandı. Uygulanan kurallar ve **nerede görüldüğü**:

| # | Kural | Bu palette karşılığı |
|---:|---|---|
| K1 | Ters çevirme yasak | 49 belirtecin **49'u** da her blokta bağımsız hesaplandı; hiçbir karanlık değer aydınlık değerin aritmetik tersi değil. |
| K2 | Saf siyah zemin / saf beyaz metin yasak | Karanlık gövde zeminleri OKLCH L=0,18'de ve şema hue'su hafif karışmış; en güçlü metin L≤0,97 ve kırık beyaz. |
| K3 | Yükselti yönü ters işler | Aydınlık: yuzey (L 0,999) → govde (0,976) → yuzey-2 (0,970) → yuzey-3 (0,948) **koyulaşır**. Karanlık: govde (0,180) → yuzey (0,235) → yuzey-2 (0,268) → yuzey-3 (0,315) **açılır**. |
| K4 | Vurgu karanlıkta açılır, doygunluk düşer | Vurgu L 0,52 → 0,79 (M3'ün ton 40→80'inin OKLCH karşılığı); chroma ×0,62 (örn. Platin Mavi 0,145 → 0,090). |
| K5 | `--vurgu-metin` ölçülerek seçilir | Aydınlıkta 8 şemanın 8'inde kırık beyaz; **karanlıkta 8 şemanın 8'inde koyu** (`#071a16` … `#211017`). Sabit `#ffffff` hiçbir yerde yok. |
| K6 | Kontrast bandı katman başına | Karanlık gövde metni / kart: 12,00–12,11:1 (hedef bant 12–16). İkincil metin 4,6–5,6:1. Tavan aşılmadı (21:1 yok). |
| K7 | Kenarlık ≠ ayraç | `--kenarlik` her iki temada da **ölçüldü** (≥3:1, 48 ölçüm); `--ayrac` ölçüm dışı ve karanlıkta düşük kontrast kalabiliyor. |
| K8 | Hover/seçili rolleri eklendi | `--yuzey-hover` / `--yuzey-secili`; karanlıkta kademe farkı aydınlıktakinden geniş (0,235→0,290→0,340 ↔ 0,999→0,960→0,935). |
| K9 | Girdi zemini kap katmanının bir üstü | Carbon kuralı **belirteçle** sağlandı: modal `--yuzey`, içindeki kart `--yuzey-2`, içindeki girdi `--yuzey-3`. (S-1'in app.css kuralı; bu dosya merdiveni verir.) |
| K10 | Renk tek başına anlam taşımaz | 4 durum rengi 4 parçalı (`-vurgu` / `-yuzey` / `-kenar` / metin) + **ayrı siluetli ikon + metin etiketi** zorunlu (§ 6.2). |
| K11 | Karanlıkta punto küçültülmez | 3 yazı boyutunun en küçüğü 16 px; karanlıkta gövde ağırlığı ≥400 (§ 8). |
| K12 | Vurgu taşıyan öğe ≤3, karanlıkta daha katı | `VURGU_OGE_MAX = 3`; karanlıkta yalnız birincil düğme + aktif menü satırı + odak halkası. |

---

## 6. Durum renkleri ve ŞEMA-DURUM ÇAKIŞMASI

### 6.1 Karar: durum rengi şemadan türetilmez

Material 3'ün `error` paletini marka paletlerinden ayrı tutması ile aynı mantık: **4 durum renginin hue ve chroma'sı 8 şemada da sabittir**, yalnız tema değiştirir. Değişen tek şey açıklık (L): her şemanın kendi yüzeyine karşı 4,5:1'i tutana kadar kaydırılır.

| Durum | Hue (OKLCH) | Aydınlık metin | Karanlık metin | İkon silueti (zorunlu) |
|---|---:|---|---|---|
| Başarı | 145° | `#27762f` | `#9acd9b` | daire içinde ✓ |
| Uyarı | 65° | `#8a5619` | `#ddb590` | üçgen içinde ! |
| Hata | 27° | `#ac312c` | `#fca298` | daire içinde × |
| Bilgi | 238° | `#006b9b` | `#81c7f4` | daire içinde i |

### 6.2 Zümrüt ve Zeytin'de yeşil çakışması — nasıl çözüldü

**Sorun:** "Zümrüt" şeması yeşildir, "Zeytin" şeması yeşildir, "başarı" da yeşildir. Kullanıcı yeşil rozeti marka rengi mi başarı mı diye ayırt edemez.

**Yanlış çözüm (reddedildi):** başarı rengini şemaya göre kaydırmak. Anlamı bozar, renk körü kullanıcıda işe yaramaz, 16 blokta 16 farklı "başarı" üretir.

**Uygulanan çözüm — üç katman:**

1. **Ölçülebilir hue ayrımı.** Şema vurgusu ile her durum rengi arasında şu kuraldan **en az biri** sağlanmalı:
   - **(i)** OKLCH hue farkı **≥ 30°**, veya
   - **(ii)** OKLCH chroma oranı **≥ 2,5×** (biri nötr algılanır, diğeri kromatik).

   Buna göre hue'lar bilerek şöyle yerleştirildi: **başarı 145°** — **Zeytin 111°** (34° uzak) ile **Zümrüt 178°** (33° uzak) arasına tam ortalanmış. Zümrüt teal-zümrüde, Zeytin sarı-yeşile itildi; başarı ikisinin de olmadığı çim yeşilinde kaldı.
   Aynı kural diğer riskli çiftleri de çözdü: **Bordo 355° ↔ hata 27°** (32°), **Platin Mavi 270° ↔ bilgi 238°** (32°), **Okyanus 198° ↔ bilgi 238°** (40°).
   Kural **(ii)** ile geçenler: **Grafit ↔ bilgi** (chroma 3,4× — grafit nötr algılanır) ve **Fildişi ↔ uyarı / hata** (chroma 3,1× ve 4,9×).
   **16 blok × 4 durum = 64 çiftin 64'ü kuraldan geçti; ihlal 0.**

2. **Bağlam ayrımı (renk değil rol).** Şema vurgusu yalnız **katı dolgu** olarak görünür (birincil düğme, aktif menü satırı, odak halkası — sayfada ≤3 öğe). Durum rengi ise **hiçbir zaman katı marka dolgusu değildir**: her zaman `-yuzey` zemin + `-kenar` çerçeve + metin üçlüsüyle bir **bant/rozet** olarak çıkar. Aynı sayfada bir yeşil düğme ve bir yeşil bant yan yana dursa bile biçimleri farklıdır.

3. **Renk dördüncü ipucudur.** Her durum bildirimi **ayrı siluetli ikon + metin etiketi** taşır (§ 6.1 tablosu). Gerekçe ölçümdür, slogan değil: renk körlüğü (döteranopi/protanopi) altında yeşil ile mavi ayırt edilemez hâle gelir; erkeklerin yaklaşık %8'i etkilenir ve kuyumcu tezgâhı ağırlıklı erkek kullanıcı kitlesidir. **Kural:** hiçbir ekranda anlam yalnız renkle verilmez; `+/−` işareti, ikon veya etiket eşlik eder.

---

## 7. Varsayılan kararı — ekip oyu (kurucuya sorulmadı)

**Karar: Platin Mavi · aydınlık · 16 px.** (Tamer Bey mührü; 23 Eylül talimatı "bana sorma, ekip oyuyla karar ver".)

Gerekçe — ve **önce bir dürüstlük notu**: kontrast payı gerekçe DEĞİLDİR. Palet üretici her çifti aynı eşiğe kadar sürdüğü için 8 şemanın kontrast payı **birbirinin aynısıdır** (metin minimumu 8 şemada da 4,83:1; metin dışı minimum 3,23–3,26:1 arası). Varsayılanı kontrasta bakarak seçmek mümkün değil. Gerçek gerekçeler şunlar:

1. **Durum renkleriyle ayrım mesafesi (ölçüldü).** Şema vurgusunun 4 durum rengine hue uzaklıkları:

   | Şema | Vurgu hue | başarı | uyarı | hata | bilgi | Toplam | En yakın |
   |---|---:|---:|---:|---:|---:|---:|---:|
   | **Platin Mavi** | 270° | 125° | 155° | 117° | 32° | **429°** | 32° |
   | Zümrüt | 178° | 33° | 113° | 151° | 60° | 357° | 33° |
   | Grafit | 258° | 113° | 167° | 129° | 20° | 429° | **20°** (kural (i) düşer, chroma ile geçer) |
   | Bordo | 355° | 150° | 70° | 32° | 117° | 369° | 32° |
   | Okyanus | 198° | 53° | 133° | 171° | 40° | 397° | 40° |
   | Menekşe | 305° | 160° | 120° | 82° | 67° | 429° | 67° |
   | Zeytin | 112° | 33° | 47° | 85° | 126° | 291° | 33° |
   | Fildişi | 50° | 95° | 15° | 23° | 172° | 305° | **15°** (kural (i) düşer, chroma ile geçer) |

   **Dürüst okuma:** en yüksek asgari mesafe **Menekşe**'nindir (67°), Platin Mavi'nin değil. Platin Mavi toplamda birinci (429°) ve dört durumun **dördünde de kural (i)'den** geçen üç şemadan biridir; Grafit ve Fildişi yalnız chroma kuralıyla geçer (daha zayıf garanti). Yani Platin Mavi "en iyi" değil, **en iyi üçünden biri**; aralarındaki seçim 2.–4. maddelerle yapıldı.
2. **Öğrenme maliyeti sıfır.** § 5B personası (ilkokul/lise mezunu tezgâhtar) mavi-beyaz kurumsal arayüzü banka ve e-devlet ekranlarından tanıyor. Müşterinin şikâyeti "renkleri iyi değil" idi; tanıdık olan en az itiraz alandır. Menekşe (mor) tanıdık değil.
3. **Rakipten ayrışma.** Rakip PusulaX mor gradyan marka kullanıyor (`#5b2fd6` civarı, Pusula referans § 1). **Menekşe varsayılan olursa "Pusula'ya benzemiş" denir** — birinci madde onu öne çıkarsa da bu yüzden varsayılan yapılmadı; seçenek olarak listede kalır.
4. **"Sakin Lüks" yönüne uyum (§ 5A).** Gradyansız, doygunluğu orta, geniş yüzeye yayılmayan bir vurgu isteniyor. Platin Mavi'nin aydınlık vurgusu `#4960bb` (OKLCH L 0,52 / C 0,145) bu tarifin tam ortasıdır; Bordo ve Menekşe daha "konuşkan", Grafit ve Fildişi neredeyse renksiz (marka kimliği taşımaz).

**Planın önerisi de Platin Mavi'ydi; araştırma bunu değiştirmedi, sayıyla doğruladı.**

Varsayılan yazı boyutu **16 px**: § 5A'nın Ö2 ölçeği ve § 5B'nin 12. kuralı (30 dakikada öğrenilir) bunu zaten istiyor; ayrıca karanlık temada küçük punto cezalandırıldığı için 14 px varsayılan olamaz. **Bugünkü 14 px varsayılan değişiyor** — bu bilinçli bir kırılmadır ve S-1'de mevcut kiracıların ekranını etkiler; § 9'daki migration varsayılanı 16 yazar.

---

## 8. Yazı boyutu — 3 kademe

`:root[data-yazi="16" | "18" | "20"]`, varsayılan **16**.

| Belirteç | 16 (varsayılan) | 18 | 20 |
|---|---|---|---|
| `--yazi-govde` | **16px** | **18px** | **20px** |
| `--yazi-rakam` (para/gram/has) | 18px | 20px | 22px |
| `--yazi-xs` | .75rem | .8125rem | .875rem |
| `--yazi-sm` | .8125rem | .875rem | 1rem |
| `--yazi-md` | .875rem | 1rem | 1.125rem |
| `--yazi-lg` | 1rem | 1.125rem | 1.25rem |
| `--yazi-xl` | 1.125rem | 1.25rem | 1.375rem |
| `--yazi-2xl` | 1.5rem | 1.75rem | 2rem |
| `--yazi-3xl` | 2.25rem | 2.5rem | 2.75rem |
| `--satir-yuksekligi` | 1.55 | 1.55 | 1.6 |

Kurallar:
- `--yazi-rakam` § 5A'nın "para/gram/has gövdeden 1 kademe büyük" maddesinin belirteç karşılığıdır; `font-variant-numeric: tabular-nums` ve sağa dayama ile birlikte kullanılır.
- Bu blok **renk belirteci tanımlayamaz** (yoğunluk/mod bloklarıyla aynı sözleşme) — kontrast sözleşmesi yazı boyutundan bağımsızdır.
- Karanlık temada gövde ağırlığı **≥400**, punto **≥16 px**: küçük puntoda aydınlık modun okunabilirlik avantajı artar, bu yüzden karanlık tema küçültme yapamaz.
- `app.css`'teki **73 ham `font-size` bildirimi** S-1'de bu 9 belirtece bağlanacak; yeni ham değer eklenmeyecek.

---

## 9. Backend notu — tercih kalıcılığı (KOD YAZILMADI)

`kullanicilar` tablosunda bugün **tema/sema/yazi_boyutu kolonu yok**; tercih hiçbir yerde kalıcı değil. Gereken en küçük değişiklik tek bir migration ve tek bir uçtur: `kullanicilar` tablosuna üç `nullable` metin/tamsayı kolonu eklenir — `tema_modu` (`otomatik` | `aydinlik` | `karanlik`, varsayılan `otomatik`), `sema` (8 slug'dan biri, varsayılan `platin-mavi`) ve `yazi_boyutu` (16 | 18 | 20, varsayılan 16) — ve aynı üçlü `kiraci_ayarlari` tarafında **dükkân varsayılanı** olarak zaten var olan görünüm ayarlarının yanına eklenir; kullanıcı satırı `null` ise kiracı varsayılanı, o da yoksa sistem varsayılanı (`otomatik` / `platin-mavi` / `16`) uygulanır. API tarafında mevcut `ArayuzModuServisi` / `TemaServisi` deseninin birebir aynısı izlenir: tek bir `PATCH /api/kullanici/gorunum` ucu üç alanı **beyaz listeyle** doğrulayıp (8 şema slug'ı, 3 tema modu, 3 punto — liste dışı değer 400 döner, istemciye güvenilmez) kullanıcı satırına yazar; `GET /api/oturum` (ya da mevcut profil ucu) yanıtına aynı üçlü eklenir ve `_Host`/`App` render'ında `html` etiketine `data-tema`, `data-sema`, `data-yazi` öznitelikleri **sunucudan** basılır, böylece ilk boyamada yanıp sönme (FOUC) olmaz; `js/gorunum.js` yalnız beyaz listeli değeri yazmaya devam eder ve hiçbir eşik/iş kuralı istemciye gömülmez. Denetim izine `GORUNUM_DEGISTI` olayı eklenir (kim, ne zaman, hangi üçlü). Migration **geri alınabilir** olmalı (üç kolon `Down` ile düşer) ve mevcut satırlar için varsayılan `null` bırakılır — böylece bu paket hiçbir kiracının mevcut ekranını sessizce değiştirmez; yalnız yeni varsayılan yazı boyutunun 14→16 px çıkması bilinçli ve duyurulacak tek görsel kırılmadır.

---

## 10. BYGATECH satış demosu — ekran sırası (kurucu talimatı, 23 Eylül)

Kurucu: *"demo için hangi ekranlar sırasıyla ne olacak bilemem onu sen Pusula'dan ve diğerlerinden araştır kendin bul."*

**Kaynak:** `PUSULA-REFERANS-2026-09-23.md` § 6 (PusulaX Demo 10:51 kare şeridi) ve § 7 (klasik "Satış İşlemi Genel" 3:53 altyazısı) + kendi 46 sayfalık ekran envanterimiz (`dotnet/Kuyumcu.Web/Pages/*.razor`).
**Ritim kararı:** rakibin 10:51'iyle aynı uzunluk (~10–11 dk), aynı sıra mantığı (kimlik → genel bakış → tanım → asıl iş → çıktı → derinlik → kapanış), ama **iki yerde onu geçiyoruz**: 3. sahne (tema gösterisi — rakipte hiç yok, müşterinin "renkleri iyi değil" itirazının doğrudan cevabı) ve 9. sahne (uyum kapıları — rakipte hiç yok, asıl satış argümanı).

| # | Dakika | Ekran (dosya) | Ne gösterilir | Neden burada |
|---:|---|---|---|---|
| 1 | 0:00–0:20 | **Giriş** (`Giris.razor`) | BYGATECH markası, tek kart (e-posta/parola/Giriş), sağda sakin görsel + tek cümle | Rakip 0:10'da giriş ekranını gösteriyor; biz pazarlama sloganı yerine tek cümle koyuyoruz (§ 5A) |
| 2 | 0:20–1:00 | **Panel** (`Panel.razor`) | Sayılar önce, **Açık Pozisyon** kutucuğu, en fazla 1 grafik | Rakibin Ana Ekran'ı 0:20–1:30; Açık Pozisyon patronun sabah baktığı tek sayı (Pusula referans § 2) |
| 3 | 1:00–1:30 | **Ayarlar → Görünüm** (`Ayarlar.razor`) | **8 şema × aydınlık/karanlık × 3 punto tek tıkla değişir** | **Rakipte yok.** Müşterinin 3. cümlesinin cevabı; demoda erken verilmeli ki geri kalan 9 dakika seçilen şemayla izlensin |
| 4 | 1:30–3:30 | **Tezgâh fişi** (`Islemler.razor`, basit mod) | Peşin müşteri ön seçili → barkod okut → ödeme bacakları satır satır (nakit / kart / **eski altın** / açık hesap) → **FARK 0,00** → Kaydet | Rakibin en uzun sahnesi (3:30–5:00) ve klasik videonun tamamı (karma ödemeli tek satış); bizim "çevirisizlik" iddiamız burada kanıtlanır |
| 5 | 3:30–4:00 | **Çıktı** (`Cikti.razor`) | Kaydet → yazdır refleksi: termal fiş + A4 önizleme yan yana | Rakip 5:00'te aynısını yapıyor; "kaydet → çıktı" refleksi Pusula'dan alınacaklar listesinin 5. maddesi |
| 6 | 4:00–5:00 | **Müşteri kartı** (`Cariler.razor` + çekmece) | Bakiye/HAS rozeti, hareket dökümü, ≤3 satır eylemi + "⋯" | Rakipte cari fişi var ama rozet yok (alınacaklar md. 2); satır eylemi disiplini bizim farkımız |
| 7 | 5:00–6:00 | **Ürün + etiket** (`Urunler.razor`, `UrunEtiketleri.razor`), **Vitrin** (`Vitrin.razor`) | Fotoğraflı ürün kartı, barkod/etiket basma, fiyat sorgulamada **büyük rakam** (`--yazi-rakam`) | Rakibin 5:30–8:30 aralığının karşılığı; fotoğraf + büyük rakam alınacaklar md. 7 |
| 8 | 6:00–7:00 | **Emanet** (`Emanet.razor`), **Siparişler** (`Siparisler.razor`) | Emanet mal takibi, sipariş/tamir akışı | **Rakipte yok.** "Daha fazla iş yapıyor" iddiasının ilk kanıtı |
| 9 | 7:00–8:00 | **Uyum** (`Uyum.razor`), **Gider pusulası** (`GiderPusulalari.razor`) | Tevsik haddi uyarısı, MASAK kimlik kapısı, hurda alışta gider pusulası | **Rakipte yok ve asıl fark budur** — yasal kapı, sonradan eklenemeyen omurga |
| 10 | 8:00–9:00 | **Raporlar** (`Raporlar.razor`), **Kâr/Zarar** (`KarZarar.razor`), **Sayım** (`Sayim.razor`) | Kâr/zarar, kasa raporu, sayım oturumu | Rakip 8:30–9:30 raporlar; sayım oturumu rakip müşterisinin şikâyet ettiği açık (referans § 8) |
| 11 | 9:00–10:00 | **Basit ↔ Profesyonel** (`Islemler.razor` aynı ekran) + kapanış | Aynı ekranın iki yüzü: basit modda 6 düğme, pro modda tümü — "özellik kaybı 0" | BYGATECH İLKESİ'nin kapanış cümlesi: ilk bakışta az şey, bir tık altında her şey |

**Demo boyunca sabit kurallar:** her ekranda aynı kabuk, aynı düğme sırası (**Kaydet · Sil/Pasife al · Vazgeç**), ekranda **en çok 3 vurgu öğesi**, hiçbir ekranda ham kod/enum yok, her rakam sağa dayalı ve tabular.

**Dürüst kayıt:** Pusula demosunun zaman damgaları **320×180 kare şeridinden düzen okunarak** çıkarıldı; karelerdeki yazılar okunamadı, tık sayıları sayılamadı. Yukarıdaki sıra rakibin **düzen ritmine** ve **kendi 46 ekranımıza** dayanır; rakibin demosunun birebir metni doğrulanmadı.

---

## 11. Kontrast ölçümü — 496 çift, 496 geçti

**Yöntem:** WCAG 2.2 nispi parlaklık formülü (`0,2126R + 0,7152G + 0,0722B`, sRGB doğrusallaştırmasıyla), kontrast `(L1+0,05)/(L2+0,05)`. Renkler elle seçilmedi: OKLCH'de sabit rol tonlarından türetildi, gamut'a sığana kadar chroma düşürüldü, sonra **ölçülüp eşiği tutana kadar L kaydırıldı**. Yuvarlama yok — 2,99:1 geçmez sayıldı, paya 0,1 eklendi (hedef 4,60 / 3,10).

**Eşikler:** metin ↔ zemin **≥ 4,5:1** (WCAG 2.2 SC 1.4.3 AA) · etkileşimli kenar, odak halkası, katı vurgu, grafik serisi ↔ zemin **≥ 3:1** (SC 1.4.11). Güçlü metin ayrıca `--yuzey`'e karşı ≥7:1'e zorlandı.

**Kapsam:** 8 şema × 2 tema × **31 çift** = **496 ölçüm**.

| Sonuç | Sayı |
|---|---:|
| Toplam ölçüm | **496** |
| AA geçen | **496** |
| Kalan | **0** |
| Eşiğe en yakın çift | 3,23:1 (Okyanus, kenarlık / 3. katman) — eşik 3,0 |
| En düşük metin ölçümü | **4,83:1** (eşik 4,5) |
| En düşük metin-dışı ölçümü | **3,23:1** (eşik 3,0) |
| Karanlık gövde metni bandı | 12,00 – 12,11 : 1 |
| Saf beyaz (`#ffffff`) karanlık metin | **0 adet** |
| Saf siyah (`#000000`) karanlık zemin | **0 adet** |

### 11.1 Ölçülen 31 çift

1. gövde metni / kart · 2. gövde metni / sayfa zemini · 3. gövde metni / 3. katman · 4. gövde metni / seçili satır · 5. güçlü metin / kart (≥7) · 6. ikincil metin / kart · 7. ikincil metin / sayfa zemini · 8. **düğme metni / birincil düğme** · 9-11. kenarlık / kart, sayfa zemini, 3. katman (≥3) · 12-13. **odak halkası** / sayfa zemini, kart (≥3) · 14. vurgu (metin dışı) / kart (≥3) · 15. menü metni / menü zemini · 16. menü başlığı / menü zemini · 17. aktif menü metni / aktif satır · 18-19. başarı / kart, başarı yüzeyi · 20-21. uyarı / kart, uyarı yüzeyi · 22-23. hata / kart, hata yüzeyi · 24. hata düğme metni / hata dolgu · 25-26. bilgi / kart, bilgi yüzeyi · 27-31. grafik-1…5 / kart (≥3).

### 11.2 Tam tablo (496 satır)


#### Platin Mavi — AYDINLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#32353e` | `#fefeff` | **12.15:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#32353e` | `#f6f7fa` | **11.43:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#32353e` | `#eceef3` | **10.55:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#32353e` | `#e7e9f0` | **10.10:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#1e212a` | `#fefeff` | **15.95:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#646870` | `#fefeff` | **5.55:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#646870` | `#f6f7fa` | **5.22:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#f9fafd` | `#4960bb` | **5.48:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#80838c` | `#fefeff` | **3.76:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#80838c` | `#f6f7fa` | **3.54:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#80838c` | `#eceef3` | **3.26:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#3f54ae` | `#f6f7fa` | **6.34:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#3f54ae` | `#fefeff` | **6.74:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#4960bb` | `#fefeff` | **5.67:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#adb1bc` | `#1f2024` | **7.59:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#9b9ea7` | `#1f2024` | **6.08:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#c8d6ff` | `#293354` | **8.56:1** | 4.5:1 | ✅ |
| başarı / kart | `#27762f` | `#fefeff` | **5.60:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#27762f` | `#e3fae3` | **5.12:1** | 4.5:1 | ✅ |
| uyarı / kart | `#8a5619` | `#fefeff` | **6.08:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#8a5619` | `#ffefe1` | **5.45:1** | 4.5:1 | ✅ |
| hata / kart | `#ac312c` | `#fefeff` | **6.45:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#ac312c` | `#ffeeec` | **5.79:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#fdf9f9` | `#c0453d` | **4.83:1** | 4.5:1 | ✅ |
| bilgi / kart | `#006b9b` | `#fefeff` | **5.83:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#006b9b` | `#e6f5ff` | **5.28:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#556ab1` | `#fefeff` | **5.10:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#9e5c1a` | `#fefeff` | **5.21:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#975389` | `#fefeff` | **5.32:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#587b2d` | `#fefeff` | **4.86:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#007e84` | `#fefeff` | **4.83:1** | 3.0:1 | ✅ |

#### Platin Mavi — KARANLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#d7dbe6` | `#1c1e24` | **12.03:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#d7dbe6` | `#0f1117` | **13.63:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#d7dbe6` | `#2e313a` | **9.38:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#d7dbe6` | `#343841` | **8.48:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#e5e7ec` | `#1c1e24` | **13.46:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#9fa2ac` | `#1c1e24` | **6.54:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#9fa2ac` | `#0f1117` | **7.40:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#111524` | `#a4b8f5` | **9.30:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#80848e` | `#1c1e24` | **4.45:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#80848e` | `#0f1117` | **5.04:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#80848e` | `#2e313a` | **3.47:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#b2c3f4` | `#0f1117` | **10.79:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#b2c3f4` | `#1c1e24` | **9.52:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#a4b8f5` | `#1c1e24` | **8.53:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#adb1bc` | `#0a0c11` | **9.12:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#9b9ea7` | `#0a0c11` | **7.30:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#c8d6ff` | `#202949` | **9.84:1** | 4.5:1 | ✅ |
| başarı / kart | `#9acd9b` | `#1c1e24` | **9.19:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#9acd9b` | `#1e361f` | **7.23:1** | 4.5:1 | ✅ |
| uyarı / kart | `#ddb590` | `#1c1e24` | **8.80:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#ddb590` | `#3f2a15` | **7.14:1** | 4.5:1 | ✅ |
| hata / kart | `#fca298` | `#1c1e24` | **8.54:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#fca298` | `#452521` | **6.99:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#1e1311` | `#fca298` | **9.30:1** | 4.5:1 | ✅ |
| bilgi / kart | `#81c7f4` | `#1c1e24` | **9.05:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#81c7f4` | `#133245` | **7.27:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#9cafe8` | `#1c1e24` | **7.70:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#daa57a` | `#1c1e24` | **7.65:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#d39dc7` | `#1c1e24` | **7.47:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#9fbc83` | `#1c1e24` | **7.93:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#6ac2c6` | `#1c1e24` | **8.05:1** | 3.0:1 | ✅ |

#### Zümrüt — AYDINLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#2d3835` | `#fdfffe` | **12.10:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#2d3835` | `#f4f8f7` | **11.35:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#2d3835` | `#e9f0ee` | **10.51:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#2d3835` | `#e4ece9` | **10.11:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#192422` | `#fdfffe` | **15.88:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#5f6b68` | `#fdfffe` | **5.52:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#5f6b68` | `#f4f8f7` | **5.18:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#f7fbfa` | `#007b69` | **4.99:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#7b8683` | `#fdfffe` | **3.75:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#7b8683` | `#f4f8f7` | **3.52:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#7b8683` | `#e9f0ee` | **3.26:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#006e5e` | `#f4f8f7` | **5.78:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#006e5e` | `#fdfffe` | **6.16:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#007b69` | `#fdfffe` | **5.18:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#a7b5b1` | `#1d2220` | **7.59:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#96a19e` | `#1d2220` | **6.06:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#afe5d7` | `#153d35` | **8.57:1** | 4.5:1 | ✅ |
| başarı / kart | `#27762f` | `#fdfffe` | **5.62:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#27762f` | `#e3fae3` | **5.12:1** | 4.5:1 | ✅ |
| uyarı / kart | `#8a5619` | `#fdfffe` | **6.10:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#8a5619` | `#ffefe1` | **5.45:1** | 4.5:1 | ✅ |
| hata / kart | `#ac312c` | `#fdfffe` | **6.47:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#ac312c` | `#ffeeec` | **5.79:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#fdf9f9` | `#c0453d` | **4.83:1** | 4.5:1 | ✅ |
| bilgi / kart | `#006b9b` | `#fdfffe` | **5.85:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#006b9b` | `#e6f5ff` | **5.28:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#00816f` | `#fdfffe` | **4.79:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#925490` | `#fdfffe` | **5.37:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#2275ac` | `#fdfffe` | **4.97:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#a6543a` | `#fdfffe` | **5.29:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#767305` | `#fdfffe` | **4.95:1** | 3.0:1 | ✅ |

#### Zümrüt — KARANLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#d1dfdb` | `#18201e` | **12.09:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#d1dfdb` | `#0c1312` | **13.68:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#d1dfdb` | `#293431` | **9.38:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#d1dfdb` | `#2f3b38` | **8.48:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#e2e9e7` | `#18201e` | **13.48:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#9aa6a3` | `#18201e` | **6.61:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#9aa6a3` | `#0c1312` | **7.48:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#071a16` | `#8dc9ba` | **9.58:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#7a8784` | `#18201e` | **4.45:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#7a8784` | `#0c1312` | **5.03:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#7a8784` | `#293431` | **3.45:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#a1d0c4` | `#0c1312` | **11.04:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#a1d0c4` | `#18201e` | **9.76:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#8dc9ba` | `#18201e` | **8.86:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#a7b5b1` | `#070e0c` | **9.18:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#96a19e` | `#070e0c` | **7.32:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#afe5d7` | `#09332b` | **9.88:1** | 4.5:1 | ✅ |
| başarı / kart | `#9acd9b` | `#18201e` | **9.16:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#9acd9b` | `#1e361f` | **7.23:1** | 4.5:1 | ✅ |
| uyarı / kart | `#ddb590` | `#18201e` | **8.77:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#ddb590` | `#3f2a15` | **7.14:1** | 4.5:1 | ✅ |
| hata / kart | `#fca298` | `#18201e` | **8.51:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#fca298` | `#452521` | **6.99:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#1e1311` | `#fca298` | **9.30:1** | 4.5:1 | ✅ |
| bilgi / kart | `#81c7f4` | `#18201e` | **9.02:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#81c7f4` | `#133245` | **7.27:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#71c3b1` | `#18201e` | **8.02:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#cf9ecd` | `#18201e` | **7.46:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#81b8e3` | `#18201e` | **7.83:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#e19f8a` | `#18201e` | **7.54:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#b6b675` | `#18201e` | **7.86:1** | 3.0:1 | ✅ |

#### Grafit — AYDINLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#33363a` | `#feffff` | **12.12:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#33363a` | `#f6f7f9` | **11.33:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#33363a` | `#eceef1` | **10.45:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#33363a` | `#e7eaee` | **10.06:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#1f2226` | `#feffff` | **15.94:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#66686d` | `#feffff` | **5.57:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#66686d` | `#f6f7f9` | **5.20:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#f8fafd` | `#5c6a7f` | **5.25:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#7f848c` | `#feffff` | **3.76:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#7f848c` | `#f6f7f9` | **3.51:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#7f848c` | `#eceef1` | **3.24:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#505f73` | `#f6f7f9` | **6.07:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#505f73` | `#feffff` | **6.50:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#5c6a7f` | `#feffff` | **5.48:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#abb2bb` | `#1f2123` | **7.55:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#999fa7` | `#1f2123` | **6.05:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#cfd8e6` | `#30363e` | **8.48:1** | 4.5:1 | ✅ |
| başarı / kart | `#27762f` | `#feffff` | **5.64:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#27762f` | `#e3fae3` | **5.12:1** | 4.5:1 | ✅ |
| uyarı / kart | `#8a5619` | `#feffff` | **6.12:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#8a5619` | `#ffefe1` | **5.45:1** | 4.5:1 | ✅ |
| hata / kart | `#ac312c` | `#feffff` | **6.49:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#ac312c` | `#ffeeec` | **5.79:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#fdf9f9` | `#c0453d` | **4.83:1** | 4.5:1 | ✅ |
| bilgi / kart | `#006b9b` | `#feffff` | **5.86:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#006b9b` | `#e6f5ff` | **5.28:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#426fb1` | `#feffff` | **5.06:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#a3572c` | `#feffff` | **5.30:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#8e5695` | `#feffff` | **5.35:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#697719` | `#feffff` | **4.93:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#008079` | `#feffff` | **4.80:1** | 3.0:1 | ✅ |

#### Grafit — KARANLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#d7dbe0` | `#1c1e22` | **12.00:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#d7dbe0` | `#0f1215` | **13.51:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#d7dbe0` | `#2e3237` | **9.28:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#d7dbe0` | `#34383e` | **8.48:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#e4e6e9` | `#1c1e22` | **13.35:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#9fa2a7` | `#1c1e22` | **6.52:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#9fa2a7` | `#0f1215` | **7.34:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#13161a` | `#b1bbca` | **9.36:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#7d848d` | `#1c1e22` | **4.42:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#7d848d` | `#0f1215` | **4.97:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#7d848d` | `#2e3237` | **3.41:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#bdc5d1` | `#0f1215` | **10.80:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#bdc5d1` | `#1c1e22` | **9.59:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#b1bbca` | `#1c1e22` | **8.61:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#abb2bb` | `#0a0c10` | **9.15:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#999fa7` | `#0a0c10` | **7.34:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#cfd8e6` | `#262c34` | **9.80:1** | 4.5:1 | ✅ |
| başarı / kart | `#9acd9b` | `#1c1e22` | **9.20:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#9acd9b` | `#1e361f` | **7.23:1** | 4.5:1 | ✅ |
| uyarı / kart | `#ddb590` | `#1c1e22` | **8.81:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#ddb590` | `#3f2a15` | **7.14:1** | 4.5:1 | ✅ |
| hata / kart | `#fca298` | `#1c1e22` | **8.55:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#fca298` | `#452521` | **6.99:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#1e1311` | `#fca298` | **9.30:1** | 4.5:1 | ✅ |
| bilgi / kart | `#81c7f4` | `#1c1e22` | **9.07:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#81c7f4` | `#133245` | **7.27:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#90b3e7` | `#1c1e22` | **7.78:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#dea182` | `#1c1e22` | **7.57:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#cc9fd1` | `#1c1e22` | **7.51:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#acb97a` | `#1c1e22` | **7.93:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#6cc3bb` | `#1c1e22` | **8.08:1** | 3.0:1 | ✅ |

#### Bordo — AYDINLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#3c3236` | `#fffeff` | **12.27:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#3c3236` | `#faf6f7` | **11.51:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#3c3236` | `#f2ecee` | **10.58:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#3c3236` | `#efe7ea` | **10.16:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#291e22` | `#fffeff` | **16.02:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#706569` | `#fffeff` | **5.56:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#706569` | `#faf6f7` | **5.22:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#fcf9fa` | `#a2426d` | **5.68:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#8b8084` | `#fffeff` | **3.78:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#8b8084` | `#faf6f7` | **3.55:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#8b8084` | `#f2ecee` | **3.26:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#953661` | `#faf6f7` | **6.58:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#953661` | `#fffeff` | **7.01:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#a2426d` | `#fffeff` | **5.90:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#baadb1` | `#231f21` | **7.52:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#a69b9f` | `#231f21` | **6.06:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#ffc5da` | `#4d2837` | **8.51:1** | 4.5:1 | ✅ |
| başarı / kart | `#27762f` | `#fffeff` | **5.61:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#27762f` | `#e3fae3` | **5.12:1** | 4.5:1 | ✅ |
| uyarı / kart | `#8a5619` | `#fffeff` | **6.09:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#8a5619` | `#ffefe1` | **5.45:1** | 4.5:1 | ✅ |
| hata / kart | `#ac312c` | `#fffeff` | **6.46:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#ac312c` | `#ffeeec` | **5.79:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#fdf9f9` | `#c0453d` | **4.83:1** | 4.5:1 | ✅ |
| bilgi / kart | `#006b9b` | `#fffeff` | **5.84:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#006b9b` | `#e6f5ff` | **5.28:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#a14f72` | `#fffeff` | **5.38:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#3e8042` | `#fffeff` | **4.78:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#9e5c1a` | `#fffeff` | **5.22:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#007c91` | `#fffeff` | **4.86:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#6963ae` | `#fffeff` | **5.22:1** | 3.0:1 | ✅ |

#### Bordo — KARANLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#e5d7dc` | `#231c1e` | **12.01:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#e5d7dc` | `#160f12` | **13.58:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#e5d7dc` | `#382e32` | **9.40:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#e5d7dc` | `#3f3438` | **8.57:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#ece5e7` | `#231c1e` | **13.47:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#a99ea2` | `#231c1e` | **6.45:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#a99ea2` | `#160f12` | **7.29:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#211017` | `#e6a4bd` | **9.04:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#8c7f84` | `#231c1e` | **4.37:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#8c7f84` | `#160f12` | **4.94:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#8c7f84` | `#382e32` | **3.42:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#e8b3c6` | `#160f12` | **10.52:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#e8b3c6` | `#231c1e` | **9.31:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#e6a4bd` | `#231c1e` | **8.29:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#baadb1` | `#100a0c` | **9.06:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#a69b9f` | `#100a0c` | **7.29:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#ffc5da` | `#421d2d` | **9.86:1** | 4.5:1 | ✅ |
| başarı / kart | `#9acd9b` | `#231c1e` | **9.22:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#9acd9b` | `#1e361f` | **7.23:1** | 4.5:1 | ✅ |
| uyarı / kart | `#ddb590` | `#231c1e` | **8.82:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#ddb590` | `#3f2a15` | **7.14:1** | 4.5:1 | ✅ |
| hata / kart | `#fca298` | `#231c1e` | **8.57:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#fca298` | `#452521` | **6.99:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#1e1311` | `#fca298` | **9.30:1** | 4.5:1 | ✅ |
| bilgi / kart | `#81c7f4` | `#231c1e` | **9.08:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#81c7f4` | `#133245` | **7.27:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#dd9bb4` | `#231c1e` | **7.50:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#8fc090` | `#231c1e` | **8.05:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#daa57a` | `#231c1e` | **7.68:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#6dbfd3` | `#231c1e` | **7.99:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#abaae6` | `#231c1e` | **7.66:1** | 3.0:1 | ✅ |

#### Okyanus — AYDINLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#2c3838` | `#fdffff` | **12.10:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#2c3838` | `#f4f8f8` | **11.35:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#2c3838` | `#e8eff0` | **10.43:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#2c3838` | `#e3ebec` | **10.04:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#182425` | `#fdffff` | **15.88:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#5f6a6b` | `#fdffff` | **5.56:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#5f6a6b` | `#f4f8f8` | **5.22:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#f7fbfb` | `#00787b` | **5.07:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#7a8686` | `#fdffff` | **3.75:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#7a8686` | `#f4f8f8` | **3.52:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#7a8686` | `#e8eff0` | **3.23:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#006b6e` | `#f4f8f8` | **5.90:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#006b6e` | `#fdffff` | **6.29:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#00787b` | `#fdffff` | **5.27:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#a6b4b5` | `#1d2222` | **7.53:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#95a1a1` | `#1d2222` | **6.05:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#abe4e5` | `#103d3e` | **8.50:1** | 4.5:1 | ✅ |
| başarı / kart | `#27762f` | `#fdffff` | **5.63:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#27762f` | `#e3fae3` | **5.12:1** | 4.5:1 | ✅ |
| uyarı / kart | `#8a5619` | `#fdffff` | **6.11:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#8a5619` | `#ffefe1` | **5.45:1** | 4.5:1 | ✅ |
| hata / kart | `#ac312c` | `#fdffff` | **6.48:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#ac312c` | `#ffeeec` | **5.79:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#fdf9f9` | `#c0453d` | **4.83:1** | 4.5:1 | ✅ |
| bilgi / kart | `#006b9b` | `#fdffff` | **5.85:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#006b9b` | `#e6f5ff` | **5.28:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#007f82` | `#fdffff` | **4.80:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#9e507a` | `#fdffff` | **5.39:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#4a6db1` | `#fdffff` | **5.09:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#9f5b1d` | `#fdffff` | **5.25:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#5b7b2a` | `#fdffff` | **4.85:1** | 3.0:1 | ✅ |

#### Okyanus — KARANLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#d0dfdf` | `#172020` | **12.11:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#d0dfdf` | `#0b1313` | **13.70:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#d0dfdf` | `#293434` | **9.36:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#d0dfdf` | `#2e3b3b` | **8.48:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#e1e9e9` | `#172020` | **13.48:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#99a6a6` | `#172020` | **6.61:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#99a6a6` | `#0b1313` | **7.49:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#061a1a` | `#88c8ca` | **9.54:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#798787` | `#172020` | **4.45:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#798787` | `#0b1313` | **5.04:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#798787` | `#293434` | **3.44:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#9dcfd0` | `#0b1313` | **10.99:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#9dcfd0` | `#172020` | **9.71:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#88c8ca` | `#172020` | **8.83:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#a6b4b5` | `#060e0e` | **9.12:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#95a1a1` | `#060e0e` | **7.33:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#abe4e5` | `#033234` | **9.91:1** | 4.5:1 | ✅ |
| başarı / kart | `#9acd9b` | `#172020` | **9.16:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#9acd9b` | `#1e361f` | **7.23:1** | 4.5:1 | ✅ |
| uyarı / kart | `#ddb590` | `#172020` | **8.77:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#ddb590` | `#3f2a15` | **7.14:1** | 4.5:1 | ✅ |
| hata / kart | `#fca298` | `#172020` | **8.51:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#fca298` | `#452521` | **6.99:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#1e1311` | `#fca298` | **9.30:1** | 4.5:1 | ✅ |
| bilgi / kart | `#81c7f4` | `#172020` | **9.03:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#81c7f4` | `#133245` | **7.27:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#6ac2c4` | `#172020` | **8.01:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#da9bbb` | `#172020` | **7.43:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#95b1e8` | `#172020` | **7.70:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#daa47b` | `#172020` | **7.58:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#a1bc81` | `#172020` | **7.93:1** | 3.0:1 | ✅ |

#### Menekşe — AYDINLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#37333c` | `#fffeff` | **12.27:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#37333c` | `#f8f6fa` | **11.49:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#37333c` | `#efedf2` | **10.62:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#37333c` | `#ebe8ee` | **10.17:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#232029` | `#fffeff` | **15.93:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#6a666f` | `#fffeff` | **5.58:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#6a666f` | `#f8f6fa` | **5.22:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#fbf9fc` | `#7d4fab` | **5.61:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#86828b` | `#fffeff` | **3.74:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#86828b` | `#f8f6fa` | **3.50:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#86828b` | `#efedf2` | **3.24:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#71439e` | `#f8f6fa` | **6.52:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#71439e` | `#fffeff` | **6.96:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#7d4fab` | `#fffeff` | **5.83:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#b3afb9` | `#212023` | **7.53:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#a09ca6` | `#212023` | **6.02:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#e2ccff` | `#3d2d4e` | **8.50:1** | 4.5:1 | ✅ |
| başarı / kart | `#27762f` | `#fffeff` | **5.61:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#27762f` | `#e3fae3` | **5.12:1** | 4.5:1 | ✅ |
| uyarı / kart | `#8a5619` | `#fffeff` | **6.09:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#8a5619` | `#ffefe1` | **5.45:1** | 4.5:1 | ✅ |
| hata / kart | `#ac312c` | `#fffeff` | **6.46:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#ac312c` | `#ffeeec` | **5.79:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#fdf9f9` | `#c0453d` | **4.83:1** | 4.5:1 | ✅ |
| bilgi / kart | `#006b9b` | `#fffeff` | **5.84:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#006b9b` | `#e6f5ff` | **5.28:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#7e5ca4` | `#fffeff` | **5.28:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#836d00` | `#fffeff` | **5.03:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#a64f5f` | `#fffeff` | **5.35:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#00835e` | `#fffeff` | **4.74:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#0078a7` | `#fffeff` | **4.92:1** | 3.0:1 | ✅ |

#### Menekşe — KARANLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#ded9e4` | `#1f1d23` | **12.04:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#ded9e4` | `#131016` | **13.61:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#ded9e4` | `#333038` | **9.35:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#ded9e4` | `#3a363f` | **8.52:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#e8e6eb` | `#1f1d23` | **13.47:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#a4a0aa` | `#1f1d23` | **6.51:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#a4a0aa` | `#131016` | **7.35:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#191221` | `#c8ace9` | **9.16:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#86818c` | `#1f1d23` | **4.40:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#86818c` | `#131016` | **4.97:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#86818c` | `#333038` | **3.41:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#cfb9ea` | `#131016` | **10.58:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#cfb9ea` | `#1f1d23` | **9.36:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#c8ace9` | `#1f1d23` | **8.37:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#b3afb9` | `#0d0b10` | **9.09:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#a09ca6` | `#0d0b10` | **7.27:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#e2ccff` | `#322343` | **9.82:1** | 4.5:1 | ✅ |
| başarı / kart | `#9acd9b` | `#1f1d23` | **9.20:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#9acd9b` | `#1e361f` | **7.23:1** | 4.5:1 | ✅ |
| uyarı / kart | `#ddb590` | `#1f1d23` | **8.81:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#ddb590` | `#3f2a15` | **7.14:1** | 4.5:1 | ✅ |
| hata / kart | `#fca298` | `#1f1d23` | **8.55:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#fca298` | `#452521` | **6.99:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#1e1311` | `#fca298` | **9.30:1** | 4.5:1 | ✅ |
| bilgi / kart | `#81c7f4` | `#1f1d23` | **9.07:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#81c7f4` | `#133245` | **7.27:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#bda4dd` | `#1f1d23` | **7.56:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#c2b171` | `#1f1d23` | **7.81:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#e19ba5` | `#1f1d23` | **7.50:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#7bc3a4` | `#1f1d23` | **8.09:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#7abae0` | `#1f1d23` | **7.89:1** | 3.0:1 | ✅ |

#### Zeytin — AYDINLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#35362d` | `#fffffd` | **12.22:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#35362d` | `#f7f7f4` | **11.40:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#35362d` | `#eeeee9` | **10.51:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#35362d` | `#e9eae3` | **10.10:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#222319` | `#fffffd` | **15.86:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#67685f` | `#fffffd` | **5.64:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#67685f` | `#f7f7f4` | **5.26:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#fafaf7` | `#6a6e2f` | **5.17:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#83847b` | `#fffffd` | **3.78:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#83847b` | `#f7f7f4` | **3.52:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#83847b` | `#eeeee9` | **3.25:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#5f6223` | `#f7f7f4` | **6.01:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#5f6223` | `#fffffd` | **6.44:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#6a6e2f` | `#fffffd` | **5.40:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#b1b2a7` | `#20211d` | **7.55:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#9e9f96` | `#20211d` | **6.06:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#d8dbb8` | `#363720` | **8.55:1** | 4.5:1 | ✅ |
| başarı / kart | `#27762f` | `#fffffd` | **5.64:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#27762f` | `#e3fae3` | **5.12:1** | 4.5:1 | ✅ |
| uyarı / kart | `#8a5619` | `#fffffd` | **6.12:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#8a5619` | `#ffefe1` | **5.45:1** | 4.5:1 | ✅ |
| hata / kart | `#ac312c` | `#fffffd` | **6.49:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#ac312c` | `#ffeeec` | **5.79:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#fdf9f9` | `#c0453d` | **4.83:1** | 4.5:1 | ✅ |
| bilgi / kart | `#006b9b` | `#fffffd` | **5.86:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#006b9b` | `#e6f5ff` | **5.28:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#71750e` | `#fffffd` | **4.93:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#496db1` | `#fffffd` | **5.11:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#00816e` | `#fffffd` | **4.80:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#95538c` | `#fffffd` | **5.39:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#a55535` | `#fffffd` | **5.31:1** | 3.0:1 | ✅ |

#### Zeytin — KARANLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#dbddd1` | `#1e1f18` | **12.08:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#dbddd1` | `#12120c` | **13.67:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#dbddd1` | `#323229` | **9.41:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#dbddd1` | `#38392f` | **8.51:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#e7e7e1` | `#1e1f18` | **13.38:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#a3a49a` | `#1e1f18` | **6.59:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#a3a49a` | `#12120c` | **7.46:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#16170c` | `#bbbe98` | **9.41:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#84857a` | `#1e1f18` | **4.44:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#84857a` | `#12120c` | **5.03:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#84857a` | `#323229` | **3.46:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#c4c7a8` | `#12120c` | **10.81:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#c4c7a8` | `#1e1f18` | **9.56:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#bbbe98` | `#1e1f18` | **8.65:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#b1b2a7` | `#0c0d07` | **9.10:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#9e9f96` | `#0c0d07` | **7.30:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#d8dbb8` | `#2c2d16` | **9.87:1** | 4.5:1 | ✅ |
| başarı / kart | `#9acd9b` | `#1e1f18` | **9.16:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#9acd9b` | `#1e361f` | **7.23:1** | 4.5:1 | ✅ |
| uyarı / kart | `#ddb590` | `#1e1f18` | **8.77:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#ddb590` | `#3f2a15` | **7.14:1** | 4.5:1 | ✅ |
| hata / kart | `#fca298` | `#1e1f18` | **8.51:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#fca298` | `#452521` | **6.99:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#1e1311` | `#fca298` | **9.30:1** | 4.5:1 | ✅ |
| bilgi / kart | `#81c7f4` | `#1e1f18` | **9.02:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#81c7f4` | `#133245` | **7.27:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#b2b777` | `#1e1f18` | **7.86:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#94b2e8` | `#1e1f18` | **7.74:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#72c3b0` | `#1e1f18` | **8.03:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#d29dc9` | `#1e1f18` | **7.44:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#e09f87` | `#1e1f18` | **7.50:1** | 3.0:1 | ✅ |

#### Fildişi — AYDINLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#3c332e` | `#fffefe` | **12.23:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#3c332e` | `#fdf5f1` | **11.44:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#3c332e` | `#f8ebe3` | **10.55:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#3c332e` | `#f6e6dd` | **10.14:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#291f1a` | `#fffefe` | **15.98:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#6f6661` | `#fffefe` | **5.56:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#6f6661` | `#fdf5f1` | **5.20:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#fcf9f8` | `#796458` | **5.31:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#8b817c` | `#fffefe` | **3.77:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#8b817c` | `#fdf5f1` | **3.53:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#8b817c` | `#f8ebe3` | **3.25:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#6d594d` | `#fdf5f1` | **6.12:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#6d594d` | `#fffefe` | **6.55:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#796458` | `#fffefe` | **5.52:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#655b57` | `#fbf2ec` | **5.97:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#6f6763` | `#fbf2ec` | **5.01:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#5e4b41` | `#e8dfdb` | **6.26:1** | 4.5:1 | ✅ |
| başarı / kart | `#27762f` | `#fffefe` | **5.61:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#27762f` | `#e3fae3` | **5.12:1** | 4.5:1 | ✅ |
| uyarı / kart | `#8a5619` | `#fffefe` | **6.09:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#8a5619` | `#ffefe1` | **5.45:1** | 4.5:1 | ✅ |
| hata / kart | `#ac312c` | `#fffefe` | **6.46:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#ac312c` | `#ffeeec` | **5.79:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#fdf9f9` | `#c0453d` | **4.83:1** | 4.5:1 | ✅ |
| bilgi / kart | `#006b9b` | `#fffefe` | **5.83:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#006b9b` | `#e6f5ff` | **5.28:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#a35829` | `#fffefe` | **5.23:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#007e84` | `#fffefe` | **4.83:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#6d7614` | `#fffefe` | **4.91:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#556ab1` | `#fffefe` | **5.11:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#9a5284` | `#fffefe` | **5.33:1** | 3.0:1 | ✅ |

#### Fildişi — KARANLIK

| Çift | Ön plan | Zemin | Ölçülen | Eşik | Sonuç |
|---|---|---|---:|---:|:--:|
| gövde metni / kart | `#e4d8d2` | `#241c18` | **12.00:1** | 4.5:1 | ✅ |
| gövde metni / sayfa zemini | `#e4d8d2` | `#17100c` | **13.49:1** | 4.5:1 | ✅ |
| gövde metni / 3. katman | `#e4d8d2` | `#392f29` | **9.33:1** | 4.5:1 | ✅ |
| gövde metni / seçili satır | `#e4d8d2` | `#41352f` | **8.48:1** | 4.5:1 | ✅ |
| güçlü metin / kart | `#ebe5e1` | `#241c18` | **13.42:1** | 4.5:1 | ✅ |
| ikincil metin / kart | `#aaa09a` | `#241c18` | **6.54:1** | 4.5:1 | ✅ |
| ikincil metin / sayfa zemini | `#aaa09a` | `#17100c` | **7.35:1** | 4.5:1 | ✅ |
| düğme metni / birincil düğme | `#191513` | `#c6b7af` | **9.32:1** | 4.5:1 | ✅ |
| kenarlık / kart | `#8c817b` | `#241c18` | **4.42:1** | 3.0:1 | ✅ |
| kenarlık / sayfa zemini | `#8c817b` | `#17100c` | **4.97:1** | 3.0:1 | ✅ |
| kenarlık / 3. katman | `#8c817b` | `#392f29` | **3.44:1** | 3.0:1 | ✅ |
| odak halkası / sayfa zemini | `#cdc1bb` | `#17100c` | **10.70:1** | 3.0:1 | ✅ |
| odak halkası / kart | `#cdc1bb` | `#241c18` | **9.52:1** | 3.0:1 | ✅ |
| vurgu (metin dışı) / kart | `#c6b7af` | `#241c18` | **8.61:1** | 3.0:1 | ✅ |
| menü metni / menü zemini | `#baaea8` | `#110b07` | **9.03:1** | 4.5:1 | ✅ |
| menü başlığı / menü zemini | `#a69c97` | `#110b07` | **7.28:1** | 4.5:1 | ✅ |
| aktif menü metni / aktif satır | `#e2d4cd` | `#322925` | **9.82:1** | 4.5:1 | ✅ |
| başarı / kart | `#9acd9b` | `#241c18` | **9.23:1** | 4.5:1 | ✅ |
| başarı / başarı yüzeyi | `#9acd9b` | `#1e361f` | **7.23:1** | 4.5:1 | ✅ |
| uyarı / kart | `#ddb590` | `#241c18` | **8.84:1** | 4.5:1 | ✅ |
| uyarı / uyarı yüzeyi | `#ddb590` | `#3f2a15` | **7.14:1** | 4.5:1 | ✅ |
| hata / kart | `#fca298` | `#241c18` | **8.58:1** | 4.5:1 | ✅ |
| hata / hata yüzeyi | `#fca298` | `#452521` | **6.99:1** | 4.5:1 | ✅ |
| hata düğme metni / hata dolgu | `#1e1311` | `#fca298` | **9.30:1** | 4.5:1 | ✅ |
| bilgi / kart | `#81c7f4` | `#241c18` | **9.10:1** | 4.5:1 | ✅ |
| bilgi / bilgi yüzeyi | `#81c7f4` | `#133245` | **7.27:1** | 4.5:1 | ✅ |
| grafik-1 / kart | `#dea280` | `#241c18` | **7.64:1** | 3.0:1 | ✅ |
| grafik-2 / kart | `#6ac2c6` | `#241c18` | **8.09:1** | 3.0:1 | ✅ |
| grafik-3 / kart | `#afb879` | `#241c18` | **7.94:1** | 3.0:1 | ✅ |
| grafik-4 / kart | `#9cafe8` | `#241c18` | **7.74:1** | 3.0:1 | ✅ |
| grafik-5 / kart | `#d69cc2` | `#241c18` | **7.49:1** | 3.0:1 | ✅ |

---

## 12. Doğrulanamayanlar ve sınırlar (Anayasa md. 1)

1. **Bu belge ölçümünü kendi betiğiyle yaptı.** Projenin `TasarimBelirtecTestleri.Kontrast_AA_saglanir` testi bu koşumda **çalıştırılmadı** (beş kod şeridi aynı ağaçta koşuyor, hiçbir üretim dosyasına dokunulmadı). S-1 değerleri taşıdığında test **yeniden koşmalı** ve mutabakat doğrulanmalı. 496 ölçümün doğruluğu formülün doğru uygulanmasına dayanır; testle çapraz kontrol edilmedi.
2. **APCA (Lc) kapısı uygulanmadı.** Araştırma APCA eşiklerini (Lc 75/60/30) öneriyor, ancak APCA WCAG 3'ün henüz tavsiye olmamış taslağının parçası ve yasal dayanak değil. Bu palet **yalnız WCAG 2.2 AA ile** ölçüldü. APCA ikinci kapısı S-1'de eklenirse birkaç soluk metin çiftinin L'si yeniden ayarlanabilir.
3. **Renk körlüğü simülasyonu bu koşumda koşturulmadı.** Hue ayrımı kuralı (≥30° veya ≥2,5× chroma) ve "renk dördüncü ipucu" kuralı konuldu, ama 64 çift döteranopi/protanopi altında **ölçülmedi**. S-1 öncesi ölçülmeli; kural ihlali çıkarsa ikon/etiket zorunluluğu zaten emniyet kemeridir.
4. **Ekrandan bakılmadı.** Hiçbir şema gerçek monitörde görülmedi; "güzel mi" sorusunun cevabı bu belgede yok, yalnız "okunur mu" sorusunun cevabı var. Mockup üretimi ayrı şerittir.
5. **Araştırmadaki bazı sayılar birincil kaynaktan doğrulanamadı** (Material `#121212`, `15,8:1`, `%87/%60/%38` opaklıklar; Carbon g90/g100 hex dizisi; Apple'ın hex değerleri; astigmatizma yaygınlığı %30–50 aralığı). Bunların **hiçbiri** bu paletin bir değerine dayanak yapılmadı; yalnız yön verdi.
6. **"Karanlıkta doygunluk ×0,62" ve "kontrast bandı 12–16:1" bizim türetimimizdir**, alıntılanmış kural değil. M3'ün ton 40→80 eşlemesinin OKLCH karşılığı olarak seçildi. Ekip kararı olarak mühürlenmeli.
7. **`data-sema` durumu:** CSS'te var (P-36b, 2 şema), `TemaServisi.cs`'te yok. Bu belge ikisini de okuyup çelişkiyi Ç-7'de kaydetti; hangi katmanın "geçerli" sayıldığı **koda dokunmadan** doğrulanamadı — S-1 başlamadan `js/gorunum.js` ile `TemaServisi.cs` arasındaki bağ ölçülmeli.
8. **Ürün adı "ByGatech"** için marka/ticari unvan müsaitliği, alan adı ve tescil **kontrol edilmedi** — bu ekibin işi değil, Kurucu'nun kalemidir (tescil/imza/dış taahhüt).
9. **Mevcut 14 px varsayılanın 16 px'e çıkması** mevcut kiracıların ekranını değiştirecek tek kırılmadır ve bilinçlidir. Migration varsayılanı `null` bırakır ama sistem varsayılanı 16'dır; duyuru gerekir.

---

## 13. S-1'e devir listesi

1. `belirtecler-8sema.css`'teki 16 bloğu `dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css`'e taşı. Eski `[data-sema="kurumsal"]` ve `[data-sema="zumrut"]` blokları (245-274. satırlar) yeni 8 şemayla değiştirilir; `zumrut` adı korunur, `kurumsal` → `platin-mavi`.
2. Bugünkü amber vurgu değerlerini (`--vurgu: #b45309`, `--vurgu-guclu: #92400e`, `--odak-halka: #b45309`, `--grafik-1: #b45309`, karanlıkta `#f0b429`/`#fcd34d`) **8 şemanın hiçbirine taşıma.** `--amber-*` ham merdiveni kalır, yalnız `--uyari*`ı besler.
3. 5 yeni belirteç adını ekle: `--yuzey-hover`, `--yuzey-secili`, `--menu-baslik`, `--menu-aktif-yuzey`, `--menu-aktif-metin`.
4. Odak halkasını **iki parçalı** kurala geçir (CSS § 7'deki `box-shadow` kalıbı).
5. `app.css`'teki **73 ham `font-size`** ve **75 ham `padding`** bildirimini belirtece bağla.
6. `TasarimBelirtecTestleri.Kontrast_AA_saglanir` testini **16 kombinasyona** genişlet (bugün 1 tema ölçüyor); denetim kapısı `TEMA_KONTRAST_IHLALI` 8 şema × 2 tema gezsin.
7. `data-sema` / `data-tema` / `data-yazi` üçlüsünü sunucudan `html` etiketine bas (§ 9).
8. **Kâğıt muafiyeti değişmedi:** `.belge*`, `.urun-etiket*`, `@media print` bu belirteçlere başvurmaz; 8 şema kâğıda basılmaz.
