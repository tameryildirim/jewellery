# Erişilebilirlik (A11y) Denetim Raporu

**Kapsam:** 18 ekran/bileşen grubu (11 web + 7 mobil) · **Yöntem:** Ekran başına bağımsız denetçi ajan (WCAG 2.1 AA / React Native erişilebilirlik kontrol listesi) + her ekranın bulgularını koda karşı çürütmeye çalışan doğrulayıcı ajan · **Sonuç:** 137 doğrulanmış bulgu

Bulguların çoğu **paylaşılan bileşenlerdeki az sayıda kök nedenden** kaynaklanıyor ve ekranlara yayılıyor. Aşağıdaki ilk iki bölümdeki ~12 düzeltme, 137 bulgunun yaklaşık üçte ikisini tek seferde kapatır.

## Önem dağılımı

| Önem | Adet | Açıklama |
|---|---|---|
| 🔴 Kritik | 7 | Klavye/ekran okuyucu kullanıcısını tamamen engelliyor |
| 🟠 Yüksek | 62 | Temel işlevlere erişimi ciddi biçimde zorlaştırıyor |
| 🟡 Orta | 42 | Belirli durumlarda erişimi engelliyor |
| ⚪ Düşük | 26 | İyileştirme; kullanıcıyı engellemez ama gürültü yaratır |

---

## A. Web — Paylaşılan bileşen kök nedenleri (`apps/web/src/components/ui/index.tsx`)

Bu dosyadaki düzeltmeler tüm web ekranlarına otomatik yayılır.

### A1. 🔴 KRİTİK — Modal: dialog semantiği, Escape ve odak yönetimi yok (satır ~165)
6 ekranda raporlandı (Cariler, Ürünler, Kasalar, Banka, Faturalar + genel).
- `role="dialog"` ve `aria-modal="true"` yok → ekran okuyucu modalın açıldığını algılamaz
- Odak modala taşınmıyor, focus trap yok → Tab ile arkadaki sayfaya çıkılabiliyor
- Escape ile kapanmıyor
- **WCAG:** 4.1.2, 2.4.3, 2.1.2
- **Düzeltme:** `role="dialog" aria-modal="true" aria-labelledby={baslikId}`; açılışta ilk odaklanabilir öğeye focus, kapanışta odağı iade, `keydown`'da Escape → onClose, focus trap. Alternatif: yerel `<dialog>` veya Radix/Headless UI Dialog.

### A2. 🟠 YÜKSEK — Input/Select: label alana programatik bağlı değil (satır 46, 68)
Tüm formlar etkileniyor (Login, Cariler, Ürünler, İşlemler, Kasalar, Banka, Faturalar, Ayarlar).
- `<label>` htmlFor/id olmadan render ediliyor; ekran okuyucu alan adını okumaz, etikete tıklamak odak vermez
- **WCAG:** 1.3.1, 3.3.2, 4.1.2
- **Düzeltme:** `useId()` ile id üret, `<label htmlFor={id}>` + `<input id={id}>`.

### A3. 🟠 YÜKSEK — Modal kapatma butonu "×" erişilebilir ada sahip değil (satır 173)
- **Düzeltme:** `aria-label={t.ortak.kapat}` ekle.

### A4. 🟠 YÜKSEK — ErrorAlert duyurulmuyor (satır ~158)
Dinamik hata bantları `role="alert"` içermiyor; ekran okuyucu kullanıcısı form hatasından habersiz kalıyor.
- **WCAG:** 4.1.3 — **Düzeltme:** kök div'e `role="alert"`. Başarı bantları için `role="status"`.

### A5. 🟠 YÜKSEK — Primary buton kontrastı: beyaz metin / amber-500 ≈ 2.2:1 (satır 21)
Uygulamadaki tüm ana eylem butonları. 4.5:1 gerekir.
- **WCAG:** 1.4.3 — **Düzeltme:** `bg-amber-700` zemin (beyazla ≈4.8:1) veya `bg-amber-400` + `text-amber-950`.

### A6. 🟡 ORTA — LoadingOverlay/Spinner sessiz (satır 133–143)
- **Düzeltme:** kapsayıcıya `role="status"` + sr-only "Yükleniyor…" metni; spinner div'ine `aria-hidden`.

### A7. 🟡 ORTA — Input hata mesajı alana bağlanmamış (satır 55)
`error` metni `aria-describedby` ile input'a bağlı değil, `aria-invalid` yok.
- **Düzeltme:** hata varken `aria-invalid="true"` + `aria-describedby={hataId}`.

### A8. ⚪ DÜŞÜK — EmptyState: 📭 emojisi gizlenmemiş, mesaj slate-400 (satır 148–149)
- **Düzeltme:** emojiye `aria-hidden="true"`, mesaj rengini `text-slate-500+` yap.

---

## B. Mobil — Paylaşılan bileşen kök nedenleri (`apps/mobile/src/…`)

### B1. 🟠 YÜKSEK — Btn: `accessibilityRole="button"` yok (`ui.tsx` ~37)
Tüm ekranlardaki tüm butonlar. TalkBack/VoiceOver öğeyi buton olarak tanıtmaz.
- **Düzeltme:** TouchableOpacity'ye `accessibilityRole="button"`, loading sırasında `accessibilityState={{ busy: true, disabled }}`.

### B2. 🟠 YÜKSEK — Field: TextInput'un erişilebilir etiketi yok (`ui.tsx` ~82)
Görünür `Text` etiketi TextInput ile ilişkilendirilmemiş; placeholder tek başına etiket değildir.
- **Düzeltme:** `<TextInput accessibilityLabel={label} …>`.

### B3. 🟠 YÜKSEK — Btn primary/danger kontrastı (`ui.tsx` ~60)
Beyaz metin amber (≈2.15:1) ve red (≈3.76:1) üzerinde yetersiz.
- **Düzeltme:** koyu zemin tonları (amber700 vb.).

### B4. 🟠 YÜKSEK — ErrorBand/SuccessBand duyurulmuyor (`ui.tsx` ~176)
- **Düzeltme:** `accessibilityLiveRegion="polite"` (Android) + gösterim anında `AccessibilityInfo.announceForAccessibility(mesaj)`.

### B5. 🟠 YÜKSEK — DilSecici: rol/durum yok + ~21pt dokunma hedefi (`DilSecici.tsx`)
- **Düzeltme:** `accessibilityRole="button"`, `accessibilityState={{ selected }}`, `accessibilityLabel` ("Türkçe'ye geç"), min 44×44pt hedef (hitSlop veya padding).

### B6. 🟠 YÜKSEK — Tab bar: emoji ikonlu sekmelerin erişilebilir etiketi yok (`(tabs)/_layout.tsx` ~36)
Ekran okuyucu rota adını + ham emojiyi okuyor.
- **Düzeltme:** her `Tabs.Screen`'e `options.tabBarAccessibilityLabel` (çeviriden) ekle; TabIcon içindeki emojiye `accessible={false}`.

### B7. 🟠 YÜKSEK — StatCard `sub` metni: slate400, 10px (`ui.tsx` ~139)
≈2.6:1 kontrast. — **Düzeltme:** slate600+ ve/veya ≥12px.

### B8. ⚪ DÜŞÜK — Btn `sm` dokunma hedefi ~31pt (`ui.tsx` ~45); Chip gray 11px ~4.3:1 (`ui.tsx` ~203)

---

## C. Web — Ekrana özgü bulgular

### Login (`LoginPage.tsx`)
- 🟡 Aktif sekme: amber-600 / amber-50 ≈3.1:1 (satır 92) → `text-amber-700`
- ⚪ Gem logosu `aria-hidden` değil (satır 74)

### Panel (`DashboardPage.tsx`)
- 🟠 `text-slate-400` kur takip metni ≈2.8:1 (satır 123)
- 🟡 Recharts grafiğinin `role="img"` + aria-label'ı ve metin alternatifi yok (satır 71)

### Cariler (`CariPage.tsx`)
- 🟠 Arama input'u yalnızca placeholder ile etiketli (satır 75) → `aria-label` ekle
- 🟠 Tür filtresi Select'in erişilebilir adı yok (satır 82)
- 🟡 `text-slate-400` telefon sütunu (satır 175)
- ⚪ `<th>` scope yok; işlem sütununun başlığı boş (satır 103)

### Ürünler (`UrunPage.tsx`)
- 🟠 Arama input'u placeholder-only (satır 70); ayar filtresi Select adsız (satır 77)
- 🟡 `text-amber-600` has gram küçük metni sınırda (satır 116)
- ⚪ `<th>` scope/boş başlık (satır 96)

### İşlemler (`SatisPage.tsx`)
- 🟡 İşlem türü sekmeleri: seçili durum yalnızca renkle, `aria-pressed` yok (satır 120)
- 🟡 Başarı/hata mesajları duyurulmuyor (satır 227) — A4 ile birlikte çözülür
- 🟡 "Sil" butonu `text-red-500` text-xs kontrastı (satır 183)
- ⚪ `<th>` scope; Sil sütunu başlığı boş (satır 168-169)

### Kasalar (`KasaPage.tsx`)
- 🟠 `text-slate-400` döviz metni (satır 106)

### Banka (`BankaPage.tsx`)
- 🟠 IBAN metni `text-slate-400` (satır 172)
- 🟡 Seçili hesap sekmesi: durum programatik iletilmiyor + yalnızca renk (satır 147) → `aria-pressed`
- 🟡 `<th>` scope yok; işlem sütunu başlığı boş (satır 186)
- ⚪ Lucide ikonlar `aria-hidden` değil (satır 157)

### Faturalar (`FaturaPage.tsx`)
- 🟠 `text-slate-400` metinler (satır 134)
- ⚪ `<th>` scope/boş başlık (satır 116); ikonlar aria-hidden değil (satır 101)

### Raporlar (`RaporPage.tsx`)
- 🟠 Yıl/ay select'lerinin erişilebilir etiketi yok (satır 83) — bunlar paylaşılan Select değil, sayfa içi çıplak `<select>`
- 🟡 Recharts grafiklerinin metin alternatifi yok (satır 117)
- 🟡 Yaşlandırma tablosundaki renkli metinler sınırda kontrast (satır 166)
- ⚪ `<th>` scope (satır 158)

### Ayarlar (`AyarlarPage.tsx`)
- 🟠 Şifre formu etiketleri bağlı değil (satır 83) — A2 çözer
- 🟡 Dil kartları: seçili durum yalnızca renkle, `aria-pressed` yok (satır 39)
- 🟡 Hata mesajı `aria-describedby` ile bağlı değil (satır 97) — A7 çözer
- 🟡 Başarı mesajı duyurulmuyor (satır 106)
- ⚪ Bayrak emojileri `aria-hidden` değil (satır 49)

### Layout (`Sidebar.tsx`, `Header.tsx`, `DilSecici.tsx`)
- 🟡 Header kur bandında `text-slate-400` (Header.tsx:28)
- 🟡 DilSecici (web): seçili dil yalnızca renkle, `aria-pressed` yok (DilSecici.tsx:15)
- ⚪ Sidebar lucide ikonları `aria-hidden` değil (Sidebar.tsx:53); pasif dil butonu kontrastı sınırda

---

## D. Mobil — Ekrana özgü bulgular

### Giriş (`(auth)/giris.tsx`)
- 🟠 Sekme butonları: `accessibilityRole`/`State` yok (satır 75); aktif sekme kontrastı ≈2.08:1 (satır 128)
- 🟠 TextInput'lar etiketle ilişkisiz (satır 92) — B2 çözer; hata bandı duyurulmuyor (satır 88) — B4 çözer
- 🟡 Placeholder kontrastı ≈2.9:1 (satır 92); dil seçici rol/durum + hedef (satır 58) — B5 çözer
- ⚪ 💎 logo emojisi gizlenmemiş (satır 64)

### Panel (`(tabs)/index.tsx`)
- 🟠 B5/B7 türevleri (dil seçici, StatCard sub)
- ⚪ Hızlı erişim emojileri gizlenmemiş (satır 113)

### Cariler (`(tabs)/cariler.tsx`)
- 🟠 Modal kapatma "×" etiketsiz (satır 115) + 44pt altı hedef
- 🟠 Tür seçim butonları: rol/seçili durum yok (satır 126); arama placeholder-only (satır 64); hata bandı sessiz (satır 138)
- 🟡 Cari kartı TouchableOpacity'de `accessibilityRole` yok (satır 79); slate400 metinler (satır 202)
- ⚪ 🔍 emojisi gizlenmemiş (satır 63)

### İşlemler (`(tabs)/islemler.tsx`) — en yoğun ekran
- 🔴 **Kalem silme "×" butonu: ekran okuyucu için anlamsız + çok küçük dokunma hedefi (satır 187).** Sepetten ürün çıkarma tamamen erişilemez. → `accessibilityLabel={\`\${k.urunAdi} kalemini sil\`}` + ≥44pt hedef.
- 🟠 İşlem türü seçici: rol/seçili durum yok (satır 107); ayar butonları: durum yok + küçük hedef (satır 208); "× Değiştir": kontrast + hedef (satır 129)
- 🟠 Öneri listesi öğelerinde buton rolü yok (satır 137); ürün arama placeholder-only (satır 153); Field etiketsiz (satır 120); hata/başarı bantları sessiz (satır 227); aktif sekme 10px beyaz/amber (satır 251); slate400 ayar bilgisi (satır 159)
- ⚪ Tür emojileri gizlenmemiş (satır 112)

### Kasalar (`(tabs)/kasalar.tsx`)
- 🟠 Modal kapatma "×": etiketsiz + küçük hedef (satır 93-94); hareket türü butonları rol/durum yok (satır 102); TextInput etiketsiz (satır 112); bantlar sessiz (satır 114)
- 🟡 Aktif/pasif yalnızca chip rengiyle (satır 67); döviz metni slate400 (satır 134); Btn rolü yok (satır 74)
- ⚪ Hareket türü butonları 44pt altı (satır 138)

### Raporlar (`(tabs)/raporlar.tsx`)
- 🟠 Ay gezinme "‹ ›" butonları: rol/etiket yok (satır 107)
- 🟡 Gezinme hedefleri 28×28pt (satır 162); kâr marjı emerald yüzdesi ≈2.5:1 (satır 145); StatCard sub (satır 89)

---

## Önerilen düzeltme sırası

1. **A1 — Modal** (kritik, 6 ekranı kurtarır) ve **mobil İşlemler kalem silme butonu** (kritik)
2. **A2+A3+A4+A7** (web ui/index.tsx tek dosya) ve **B1+B2+B4** (mobil ui.tsx tek dosya) — form ve durum bildirimi altyapısı
3. **A5+B3 kontrast paleti** — amber buton tonu tek yerden değişir; `slate-400` → `slate-500/600` genel taraması
4. Ekrana özgü kalanlar: arama alanlarına `aria-label`, sekmelere `aria-pressed`/`accessibilityState`, `<th scope>`, grafik alternatifleri, dekoratif ikon/emoji gizleme, dokunma hedefleri

---
*Denetim çok-ajanlı statik kod analiziyle yapıldı (36 ajan: 18 denetçi + 18 doğrulayıcı). Ekran okuyucu (NVDA/VoiceOver/TalkBack) ile canlı test, düzeltmeler sonrası ayrıca önerilir. Ham bulgu listesi: `scratchpad/a11y-sonuc.json`*
