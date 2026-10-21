# Mihenk — Satışa Hazırlık Görsel / Arayüz Denetimi (21.10.2026)

> **HÜKÜM: HAYIR — bugünkü hâliyle satışa hazır değil.** Metin kontrastı, açılır listeler, hata mesajları, hareket azaltma ve sayfa hızı iyi. Ama bir alıcının ilk demoda göreceği görünür kusurlar var: Ürünler ve Banka ekranlarında masaüstünde üst üste basılmış, okunmayan ve fareyle tıklanamayan düğmeler; telefonda (özellikle yatay tutulduğunda) satış ekranının barkod alanını ve ödeme bölümünü örten yapışkan alt panel; koyu temada okunmaz rapor baskısı; aynı üründe iki ayrı tasarım dili. Bu öbekler kapanmadan "satışa hazır" denemez.

| Önem | Adet |
|---|---|
| KRİTİK | 1 |
| YÜKSEK | 8 |
| ORTA | 22 |
| DÜŞÜK | 15 |
| **Toplam** | **46** |

**Nasıl ölçüldü**
- **Araç:** başsız Chrome + ham CDP (`betikler/tur/cdp.mjs` sürücüsü). Ölçerler: `betikler/sikisma-olc.js` (K1–K15), `betikler/tur/ekstra.js` ve bu denetim için yazılan kontrast / ARIA / tipografi / düzen / yerelleştirme / odak / CLS / hedef / çakışma ölçerleri.
- **Hedef ve oturumlar:** panel http://localhost:5199. Parolasız giriş vekilleri: sahip 5182, tezgâhtar 5183. Oturumsuz ekranlar ayrı, girişsiz bir tarayıcıda ölçüldü.
- **Kapsam:** 56 rotanın tamamı (54 oturumlu şablon + /giris + /sifre-sifirla). {Id}'li rotalar gerçek kayıtlarla açıldı.
- **Hacim:** 1.222 tam sayfa ölçümü (matris + duyarlılık + EN + oturumsuz), 169 klavye turu, 220 erişilebilirlik ek ölçümü (×4 alt test), 440 hedef/çakışma taraması, 137 açılır öğe; 1.686 ekran görüntüsü + 12 PDF. Ham veri: `ham/*.jsonl`.
- **Güvenlik ağı:** denetim tarayıcılarında POST/PUT/PATCH/DELETE istekleri CDP `Fetch` düzeyinde **engellendi**. İzin verilenler yalnız: oturum tazeleme, SignalR el sıkışması ve sahip için bilinçli mod değişimi (`PATCH /kullanicilar/ben`). Engellenen her istek kayıtlı: yalnız SignalR uzun-yoklama aktarımı (`/hubs/kur?id=…`, `/hubs/banka?id=…`); tek bir veri yazma isteği bile yok.
- **Yapılmayanlar:** form gönderilmedi; kaydet/sil/onayla düğmesine basılmadı; seçicilerde seçim yapılmadı. Kod, CSS, veri ve ayar değiştirilmedi; commit yapılmadı. 5179 ve 5199'a dokunulmadı.

---

## 1. Bulgular (önem sırasıyla)

Biçim: **[No] Ekran — durum (genişlik/mod/tema)** · ne görüldü · ölçülen değer · görüntü · önerilen düzeltme (**uygulanmadı**).

### KRİTİK

**[K-1] /urunler ve /banka — ≥1024 px, Basit+Profesyonel, açık+koyu: eylem bağlantıları aynı noktaya basılmış; ikisi fareyle tıklanamıyor**
- **Ne görüldü:** ürün listesinin sağ altında "+ Tüm ayrıntılar", "Fiyat kuralları" ve "Setler" üst üste çiziliyor; ortaya okunmaz bir yığın çıkıyor. /banka'da "Havale gönder (demo)" ile "Tüm ayrıntılar" aynı biçimde üst üste.
- **Ölçülen:**
  - Üç öğe de `position:absolute`. Kutular x=1086–1216, y=838, yükseklik 22 px.
  - `elementFromPoint` ile ölçüldü: "Fiyat kuralları" ve "Setler" düğmelerinin merkezinde üstte `#yildiz-urun-ayrinti` var. İkisi 1280 ve 1024 px'te **tıklanamıyor**; /banka'da "Havale gönder" de öyle.
  - 375 px'te öğeler yan yana diziliyor; hepsi tıklanabiliyor.
  - Klavye odağı bu düğmelere gidiyor ama odaklanan öğe örtülü kalıyor (2.4.11).
  - Hem sahip hem tezgâhtar oturumunda aynı.
- **Görüntü:** `foto/bulgu/cakisma-urunler-yakin-tezgahtar.png`, `foto/bulgu/cakisma-banka-yakin-sahip.png`, `foto/bulgu/cakisma-urunler-1280-y0.png`. Ham: `ham/dogrula-tezgahtar2.jsonl` (tip `ust-oge`), `ham/cakisma-*.jsonl`, `ham/hedef-*.jsonl`.
- **Neden kritik:** ana ürün ekranında göze batan bir bozukluk; iki işlev masaüstünde fareyle açılamıyor. Bu işlevlere "Tüm ayrıntılar" içinden ikinci bir yol olup olmadığı ölçülmedi.
- **Öneri:** `yz-liste-alt` eylem satırını mutlak konumdan çıkarın ve `display:flex; gap:24px; justify-content:flex-end` ile tek satıra dizin.

### YÜKSEK

**[Y-1] /islemler (Satış) — yatay telefon 812×375 ve 667×375: yapışkan alt panel barkod alanını ve satış türü sekmelerini örtüyor**
- **Ölçülen:**
  - `#yildiz-tezgah-alt` (sticky) 272 px; görünüm yüksekliği 375 px, yani ekranın **%73'ü**.
  - Açılışta (scrollY=0) `#islem-barkod` ve `#yildiz-tur-SATIS` merkezinde üstteki öğe bu panel (`ortulu:true`).
  - Barkod alanı ancak 150 px kaydırınca açılıyor.
  - Ödeme satırı görünür hâle geldiğinde bu kez "Kaydet ve yazdır" düğmesinin altında kalıyor.
- **Görüntü:** `foto/bulgu/islemler-812x375-ust.png`, `foto/bulgu/islemler-667x375-ust.png`. Ham: `ham/dogrula-tezgahtar2.jsonl`.
- **Öneri:** alt panel en çok `30vh` olsun. Kısa ekranda (`@media (max-height:500px)`) yapışkanlık kalksın ya da panel tek satıra (Fark + Kaydet) insin. Barkod alanı her yönde ilk ekranda görünmeli.

**[Y-2] /islemler — 375×812 dikey telefon, Basit+Pro: alt panel ekranın %37'sini kaplıyor; ödeme bölümü ve arama sonuç listesi altında kalıyor**
- **Ölçülen:**
  - Panel 298/812 px.
  - Açılışta "ÖDEME", `#yildiz-odeme-0` ("Nakit · Ana Kasa") ve "Gelen havaleler" örtülü.
  - "+ Başka ödeme" ile "Vazgeç" / "Kaydet ve yazdır" kutuları 58–65 × 29 px kesişiyor.
  - Ödemeye ancak sona kaydırınca (scrollY=381) ulaşılıyor.
  - Barkod araması açıkken sonuç listesinin son satırı ("Ürün seç, fiyatı kendin yaz") panelin altında kalıyor.
  - PRO 375'te toplam tutarın üst yarısı panelle kesiliyor.
- **Görüntü:** `foto/bulgu/islemler-375-kaydir-0-tezgahtar.png` (… `-200`, `-400`, `-son`), `foto/acilir/islemler__sayfa__0__KORU_karanlik_375.jpg`, `foto/matris/islemler__PRO_aydinlik_375.jpg`.
- **Öneri:** [Y-1] ile aynı. Ek olarak açık liste yapışkan panelin üstünde olsun (`z-index`) ya da yukarı açılsın.

**[Y-3] Rapor ve liste ekranları — koyu temada yazdırma: siyah zeminli ya da beyaz zeminde açık renk yazılı baskı**
- **Ölçülen:**
  - `@media print` altında gövde metin rengi `rgb(243,242,239)`, sayfa zemini beyaz.
  - /raporlar/kasa baskısı tamamen siyah zeminli.
  - /raporlar/islem-defteri'nde beyaz kartlarda neredeyse görünmeyen açık yazılar var.
  - /cariler, /islemler, /raporlar/kar-zarar da aynı durumda.
  - Belge ekranları (fatura, fiş, irsaliye, pusula, sayım tutanağı) beyaz kalıyor; onlarda sorun yok.
- **Görüntü:** `foto/yazdir/raporlar_kasa__print__karanlik.png`, `foto/yazdir/raporlar_islem_defteri__print__karanlik.png`, `foto/yazdir/cariler__print__karanlik.png`.
- **Öneri:** `@media print` altında açık tema belirteçlerini zorlayın (`[data-tema=karanlik]` dahil) ve `color-scheme: light` verin. /ayarlar'daki "kâğıt her zaman beyaz" kuralını rapor ve liste baskılarına da uygulayın.

**[Y-4] /raporlar/islem-defteri — A4 baskı: başlık ve tarih aralığı yok, mobil kart düzeni, 25 kayıt 6 sayfa**
- **Ölçülen:**
  - A4 PDF 6 sayfa.
  - İlk sayfa doğrudan "Giriş 1.107.737,66 ₺ · Çıkış …" satırıyla başlıyor. Kâğıtta "İşlem defteri" başlığı da, basılan tarih aralığı da yok.
  - Tablo yerine dar kart düzeni basılıyor; "Sütunlar" düğmesi de basılıyor.
- **Görüntü:** `foto/yazdir/raporlar_islem_defteri__A4.pdf`.
- **Öneri:** baskıda başlık, şube ve tarih aralığı basılsın; `@media print` altında tablo düzeni zorlansın; düğmeler gizlensin.

**[Y-5] Tasarım dili ikiye bölünmüş — 8 ekran ve parola sıfırlama eski düzende**
- **Ölçülen:** çoğu ekran Yıldız düzeninde (üst-başlık satırı + 64 px/200 başlık). Şu ekranlar eski kabukta: küçük başlık + saat + "Yardım" + zil + ayrı arama kutusu, görünür h1 22 px/300:
  - /gise, /e-belgeler, /kvkk, /vitrin, /siparisler/{Id}, /banka/ekstre/{IthalatId}
  - /gider-pusulalari/{Id}/belge ve /cikti/{Tur}/{Id}
- /sifre-sifirla da giriş ekranından tamamen farklı: eski kare logo, hap biçimli TR/EN, farklı yazı düzeni.
- **Görüntü:** `foto/matris/gise__PRO_aydinlik_1280.png`, `foto/matris/kvkk__PRO_aydinlik_1280.png`; karşılaştırma için `foto/oturumsuz/sifre_sifirla__aydinlik_1280.png` ile `foto/oturumsuz/giris__aydinlik_1280.png`.
- **Öneri:** bu ekranları Yıldız başlık desenine (`yz-bas`) taşıyın; /sifre-sifirla giriş ekranının diliyle yeniden çizilsin.

**[Y-6] 375 px'te eski kabuktan gelen ikinci araç satırı içeriğin ortasına düşüyor**
- **Ölçülen:**
  - En az 20 ekranda başlığın altında ~70–90 px boşluk var (ör. /raporlar, /kullanicilar, /ayarlar, /uyum, /kilavuz, /yedekler, /kurulum, /veri-*).
  - Boşluğun ardından arama düğmesi + "YARDIM" + zil satırı geliyor.
  - İçerik ilk ekranda ~200 px aşağı itiliyor.
- **Görüntü:** `foto/matris/raporlar__BASIT_karanlik_375.png`, `foto/matris/kullanicilar__BASIT_karanlik_375.png`, `foto/bulgu/cakisma-kurulum-375-y0.png`.
- **Öneri:** arama, yardım ve zil tek üst barda toplansın; ekran başlığının altında ikinci araç satırı olmasın.

**[Y-7] Yetkisiz ekran karşılaması üç farklı biçimde; bazılarında ham modül kodu görünüyor (tezgâhtar)**
- **(a) Temiz "yetki kapısı":** "Bu sayfayı yalnız işletme sahibi ya da müdür ya da muhasebe açabilir · Hesabınız: tezgâhtar · … Ana sayfaya dön". Görülen ekranlar: /gider, /hurda, /isletme, /uyum, /kvkk, /yedekler, /kurulum …
- **(b) Kırmızı bant + ham kod:** "Bu ekran için yetkiniz yok **(rapor)**", "(kullanici)", "(bildirim)", "(evrak)". Görülen ekranlar: /raporlar/kar-zarar, /kullanicilar, /bildirimler, /evraklar.
- **(c) Denetimler açık + altta kırmızı rol bandı:** 7/30/60 gün düğmeleri görünürken altta "Bu işlem için gerekli rol: …" yazıyor. Görülen ekranlar: /raporlar/vade-takibi, /raporlar/kasa, /raporlar/islem-defteri, /raporlar/satis-analizi.
- **Ayrıca:** tezgâhtarın girebildiği /faturalar'da kırmızı "Özel matrah ayarı okunamadı. Bu işlem için gerekli rol…" bandı çıkıyor.
- **Görüntü:** `foto/bulgu/tezgahtar-raporlar_kar_zarar.png`, `foto/bulgu/tezgahtar-faturalar.png`, `foto/bulgu/tezgahtar-gider.png`, `foto/bulgu/tezgahtar-raporlar_vade_takibi.png`.
- **Öneri:** tüm yetkisiz durumlarda tek bileşen (a) kullanılsın; ham kodlar metinden çıksın. İzinli ekranda okunamayan yönetici ayarı için hata bandı gösterilmesin, ilgili bölüm sessizce gizlensin.

**[Y-8] "Çevrimdışısınız — kayıt yapılamaz" kararı yalnız `navigator.onLine`'a dayanıyor**
- **Ölçülen:**
  - Ölçüm sırasında bant tüm ekranlarda aralıklı göründü (ör. `foto/matris/cariler__PRO_aydinlik_1280.png` ve 375 koyu görüntülerinin çoğu).
  - Aynı anda yerel sunucu yanıt veriyor ve veriler yükleniyordu.
  - Sonradan aynı tarayıcıda `navigator.onLine=true` ve bant yok. Kaynak: `wwwroot/js/pwa.js` `online/offline` olayları → `PwaServisi.Cevrimdisi`.
- **Neden önemli:** sunucu dükkânın yerel ağındayken internet ya da işletim sistemi ağ durumu dalgalanırsa tezgâhtara yanlışlıkla "kayıt yapılamaz" denir.
- **Öneri:** çevrimdışı kararını sunucuya kısa bir sağlık yoklamasıyla verin; `navigator.onLine` yalnız ipucu olarak kullanılsın.

### ORTA

| No | Ekran / durum | Ne görüldü | Ölçülen | Görüntü | Öneri |
|---|---|---|---|---|---|
| O-1 | Tüm ekranlar, 320 px (Pro ve Basit) | Yatay kaydırma — 1.4.10 Yeniden akış | 55 rotanın 53'ünde `scrollWidth` 328 (8 px). Tek kaynak `#yildiz-kullanici-ac` (min-width 44 px, sağ kenar 328 px) | `foto/bulgu/reflow-320-cariler.png` | Kullanıcı düğmesi ≤360 px'te menüye taşınsın ya da üst bar `gap` küçülsün |
| O-2 | /kasalar, 1024×768 | Büyük tutarın ",24 ₺" kuruş eki tablo başlığı "NE" üzerine biniyor | [469,278–571,341] ∩ [562,332–668,349] = 9×9 px | `foto/bulgu/kasalar-1024-binme.png` | Kahraman rakam `clamp()` ile küçülsün ya da sağ sütun 1024'te alta insin |
| O-3 | /gider formu (Kasa, Kategori, Tutar, Ne için, Personel) | Alan sınırı neredeyse görünmüyor — 1.4.11 | Alt çizgi `rgb(220,218,212)` / zemin `rgb(245,244,241)` = **1,27:1** (eşik 3:1). Arama kutularının çizgisi 3,26:1 ile geçiyor | `foto/matris/gider__PRO_aydinlik_1280.png` | Form alt çizgisi arama kutusundaki renge çekilsin |
| O-4 | Arama kutuları (/cariler, /urunler, /ekranlar, /emanet, /evraklar, /siparisler, /gider-pusulalari, /islemler barkod) ve /gider'in 5 alanı | Klavye odağında görünür değişiklik yok (2.4.7); yalnız imleç yanıp sönüyor | Odaklı ve odaksız arasında outline/gölge/kenar/zemin/renk farkı 0. 55 ekranın 10'unda | `foto/klavye/cariler__odak-ornek__KORU_aydinlik_1280.jpg` | `:focus-visible` ile alt çizgi 2 px ve koyu renk |
| O-5 | /kilavuz | 9 eğitim videosu Tab durağı ama odak görünmüyor | 70 Tab'ın 54'ü görünmez odakta | `foto/klavye/kilavuz__odak-ornek__KORU_aydinlik_1280.jpg` | Videolara 2 px odak halkası |
| O-6 | Sayfa başlıkları | Başlık 5 biçimde | 64/200: 34 ekran · 40/300: Kasalar, Kur, Hurda, Kâr-zarar, Satış analizi, Kasa raporu, Sayım, Uyum · 22/300: eski kabuk · 24/600: belgeler · / ve /islemler'de görünür h1 yok | § 4 | Tek ekran başlığı boyu |
| O-7 | Tipografi ölçeği | 1280 px'te **22 ayrı punto** (10–112). 13 / 13,3 / 14 / 14,7 / 15 / 16 / 16,7 / 16,8 / 17 gibi neredeyse aynı boylar. Düğmelerde 15 farklı stil | § 4 | `ham/tablo-yazi.md` | 1,25–1,33 oranlı tek ölçek (12-14-16-18-22-28-40-64); ara boylar silinsin |
| O-8 | Seçili değer (Secici) | Seçili değer 22 px/300; aynı formdaki girdiler 18 px. Eski kabukta (sipariş detayı) sayfa başlığıyla aynı boyda (22/300), başlık gibi okunuyor | 10 ölçümün 2'si "başlık gibi" | § 3 | Seçili değer girdi metniyle eşit: 18 px / 400 |
| O-9 | Düğme durumları | Basılı (active) durum hover'dan ayrışmıyor; birincil "Yeni müşteri / Yeni ürün" (yz-b1) ve sekmelerde hover yok | 34 düğme türünün 34'ünde active = hover; 8 türde hover farkı 0 | `ham/durumlar-sahip-*.jsonl` | `:active` için koyulaşma; birincil düğmeye hover |
| O-10 | Uzun içerik (DOM'a geçici yazıldı, yalnız görüntü) | Panelde 10 haneli tutar sağ sütunun üstüne taşıyor. /cariler 1280'de 6 metin kutusundan taşıyor. /irsaliyeler'de uzun firma adı 7 satıra kırılıyor, tarih sütunu "20.10.2…" diye kesiliyor | K9 6 · K3 6 | `foto/durum/kok__uzun-icerik__aydinlik_1280.jpg`, `foto/durum/irsaliyeler__uzun-icerik__aydinlik_1280.jpg`, `foto/durum/cariler__uzun-icerik__aydinlik_1280.jpg` | Kahraman rakamda `clamp()`; tarih sütununa `white-space:nowrap`; adlara satır sınırı + title |
| O-11 | /raporlar/islem-defteri, 1280 | "Fatura No: FTR-2026-000009" numaranın ortasından ikiye bölünüyor | 9 hücre (K3c) | `foto/matris/raporlar_islem_defteri__PRO_aydinlik_1280.png` | Belge numarasına `white-space:nowrap` |
| O-12 | /urunler/etiketler | "Adet" girdisi 14 px genişlikte | `#yildiz-etiket-adet` w=14 | `foto/matris/urunler_etiketler__PRO_aydinlik_1280.png` | `min-width: 4ch` + dolgu |
| O-13 | Barkod arama sonuçları, 375 | Ayar·gram bilgisi "22 Ayar…" diye kesiliyor — kuyumcu için kritik bilgi | 4 sonucun 3'ü kesik | `foto/acilir/islemler__sayfa__0__KORU_karanlik_375.jpg` | Alt bilgi sarabilsin, fiyat ayrı satıra insin |
| O-14 | Klavye sırası | /islemler'de ilk Tab ödeme paneline gidiyor; görsel sırada üst menü ve satış sekmeleri önce. 30+ ekranda ilk durak içerikteki "Yardım". "İçeriğe geç" bağlantısı yok | 2.4.3 / 2.4.1 | `ham/klavye-sahip-KORU-aydinlik-1280.jsonl` | DOM sırası = görsel sıra; "İçeriğe geç" bağlantısı |
| O-15 | Düzen kayması (CLS), 375 | 14 durumda 0,10–0,22: /urunler 0,22 · /raporlar/kasa 0,17 · /sayim 0,17 · /islemler 0,14 · /gider 0,14 · /bildirimler 0,10 | eşik 0,1 | `ham/matris-sahip.jsonl` (`cls`) | Liste ve özet blokları için yer tutucu yükseklik |
| O-16 | Yazdırma, aydınlık tema | Rapor ve liste baskılarında arayüz denetimleri basılıyor: "Günü kapat", "Tarih seç", "+ Karşılaştır", sekmeler, "Sütunlar" | Görünen denetim 3–13 | `foto/yazdir/raporlar_kasa__A4.pdf`, `foto/yazdir/raporlar_kar_zarar__A4.pdf` | `@media print { button, .yz-eylem { display:none } }` |
| O-17 | Kök yazı boyu %200 | Tarayıcının "yazı boyutu" tercihi metnin yalnız ~%22'sini büyütüyor (px tabanlı). Yakınlaştırma ile 1.4.4 yine de geçiyor | 55 ekran ortalaması 0,22 | `foto/erisim/*__metin200.jpg` | Punto belirteçleri `rem` cinsine |
| O-18 | /denetim-izi | Sunucu `GET /api/v1/denetim/ozet` için **500** döndü | Toplam 12 açılışta 1 kez (PRO, aydınlık 1280); 4 bilinçli tekrarda çıkmadı | `ham/matris-sahip.jsonl`, `ham/dogrula-sahip.jsonl` | Sunucu günlüğünde kök neden aranmalı; ekran özet okunamazsa ayrı hata satırı göstermeli |
| O-19 | Tezgâhtar: /, /raporlar | Açılışta yetkisi olmayan uçlara istek gidiyor (403); konsol kirleniyor, veri sessizce eksik kalıyor | 403 alan uçlar: bildirimler, pozisyon, raporlar/gunluk, kar-zarar, kasa-defteri, vade-takibi, cari-yaslandirma, pos-fatura-mutabakati | `ham/matris-tezgahtar.jsonl` | Rol bilinirken bu istekler hiç gönderilmesin |
| O-20 | /evraklar/takvim (Vade takvimi), 1280 ve 375 | Gün adları ("PZT SAL ÇAR PER CUM CMT PAZ") tek satırda sağa yaslı; takvim sütunlarının üstünde değil | Gün adı öğeleri ayrı hücre değil (tek metin) | `foto/bulgu/vade-takvimi-1280.png`, `foto/bulgu/vade-takvimi-375.png` | Gün adları ızgaranın ilk satırı olsun |
| O-21 | Satın alma ve İrsaliye çekmece seçicileri | Diğer seçicilerden farklı: 14 px yazı, 41 px satır (diğerleri 19 px ve 44–48 px). Açılınca tek satır "Yükleniyor…" | 4 seçici | `foto/acilir/satin_alma__acici:+ Yeni sipariş__0__KORU_aydinlik_1280.jpg` | Ortak Secici belirteçleri |
| O-22 | /banka | "Havale gönder **(demo)**" düğmesi ve hesap adında "· Demo" | — | `foto/bulgu/cakisma-banka-yakin-sahip.png` | Demo sağlayıcıya bağlıysa gerçek kurulumda görünmediği doğrulansın; değilse kaldırılsın |

### DÜŞÜK

| No | Ekran / durum | Ne görüldü | Kanıt | Öneri |
|---|---|---|---|---|
| D-1 | 23 ekran | İki h1: biri üst barda görsel olarak gizli, biri sayfada. /gider'de "Gider ve para aktarımı" ile "Gider ve virman", /isletme'de "İşletme" ile "İşletme ve Şubeler" farklı | `aria.basliklar` | Tek h1, aynı ad |
| D-2 | /sifre-sifirla, /fiyat/{Anahtar} | h1 yok | h1Sayi 0 | h1 eklensin |
| D-3 | Başlık yazımı | "Kâr-zarar" / "Kâr-Zarar", "Veri dışa aktar" / "Veri Dışa Aktarımı", "Satış analizi" / "Satış Analizi" | h1/h2 karşılaştırması | Tek yazım kuralı (cümle düzeni) |
| D-4 | Terimler | "Parolamı unuttum" ile "Şifre Sıfırla"; Pro çekmecede "Yeni Cari" (menüde "Müşteriler") | ekran metni | Tek terim |
| D-5 | Belge önizlemeleri (fiş/fatura/pusula) | 10 px etiket ve başlıklar (DÜZENLEYEN, KALEMLER …) | Ekrandaki en küçük metin 10 px | Ekran önizlemesinde ≥12 px (baskıda 7,5 pt kabul) |
| D-6 | Satır yüksekliği | Uzun açıklama paragrafları ve çok satırlı tablo hücreleri 1,2 | #kl-aciklama, #gk-yazi-notu, KVKK metni, çok satırlı td | Gövde metni 1,5 |
| D-7 | Yazı ağırlığı | 64 px/200 başlık, 17 px/300 gövde; hedef kitle (yaşlı tezgâhtar) için ince | `tipo.tip` | Gövde 400, başlık 300 |
| D-8 | /kasalar 375 | "904.389,07 ₺." satırı ile "Günü kapat" arasında ~4 px | görsel | 16 px boşluk |
| D-9 | /urunler, /urunler/agac 375 | Satırdaki sayılar etiketsiz ("22 15,20 0 92.772 ₺", "10 40 205,28 g") | görsel | Mobil kartta küçük etiketler (ayar, gram, adet) |
| D-10 | /veri-ice-aktarim | Tarayıcının kendi dosya girdisi ("Dosya Seç · Dosya seçilmedi") tasarıma uymuyor | görsel | /isletme'deki gibi özel dosya düğmesi |
| D-11 | Belge baskısı (A4) | Belgenin altında bej zemin bandı basılıyor | `foto/yazdir/cikti_SATIS_FISI_…__A4.pdf` | Baskıda `body{background:#fff}` |
| D-12 | EN dili | "Gün kapanışı yapılmadı" bildirimi çevrilmiyor; Türkçe belgede İngilizce sayı biçimi (26,384.92) | `ham/dil-sahip.jsonl` | Bildirim metni sözlüğe; belge kendi dilinin biçimini kullansın |
| D-13 | /kur, /evraklar, /gider-pusulalari, /vitrin | Çift `id` (`yildiz-kur`, `yildiz-evrak-ara`, `yildiz-pusula-ara`, `vitrin-pano-ipucu`) | `aria.ciftIdOrnek` | Tekil id (etiket `for` ilişkisi bozulmasın) |
| D-14 | /gider 1280 | "Personel" seçici listesi görünüm alanının 52 px altına taşıyor (yukarı açılmıyor) | altTasma 52 | Yer yoksa yukarı açılsın |
| D-15 | Tek seferlik yavaşlık | /irsaliyeler PRO 375 bir kez 17 sn | Tekrarlanmadı | İzlensin |

**Doğrulamada düşürülen yanlış pozitifler** (rapora bulgu olarak girmedi): /ayarlar ve /kurulum "adsız düğmeler" — kapalı `<details>` içinde; metinleri var ve Tab ile ulaşılmıyor. /kilavuz "kontrast 1:1" — `<video>` öğesi. /raporlar, /vitrin, /banka/ekstre 375 "metin çakışması" — gizli "i" bilgi balonu. K8 "jargon" sayacı — "kâr-zarar", "e-Fatura" gibi meşru tireli sözcükler. Secici açılınca odak tetikte kalıyor: `aria-activedescendant` deseni doğru; ↓ etkin seçeneği değiştiriyor, Esc kapatıyor ve değeri koruyor (`ham/hedef-sahip.jsonl`, tip `secici-fare`).

---

## 2. Açılır listeler

**Özet:**
- Toplam 137 açılır öğe ölçümü yapıldı: sahip Pro 1280 açık, sahip Basit 375 koyu, tezgâhtar 1280 açık ve 375 koyu. Çekmeceler içindekiler "+ / Yeni / Düzenle" açıcılarıyla açılıp ölçüldü.
- Secici listelerinin **tamamı** görünüm alanında açıldı (sağ/sol taşma 0). 375'te tam genişlik (343 px) ve 48 px dokunma satırı; 1280'de 44 px satır, 19 px yazı.
- Çok seçenekte kaydırma var (uyruk 34, ürün 19, ay 12). Arama kutusu ≥5 seçenekte var; eşleşme yoksa "Eşleşen seçenek yok." mesajı çıkıyor.
- `aria-expanded` açıkken true, kapanınca false. Kapatınca değer **her öğede korundu**; rota hiç değişmedi.
- Sorunlu olanlar:
  - 375'te barkod arama listesinin yapışkan panelin altında kalması ve sonuçlardaki üç nokta ([Y-2], [O-13]).
  - /gider "Personel" listesinin 52 px aşağı taşması ([D-14]).
  - Satın alma ve İrsaliye seçicilerinin farklı ölçüsü ([O-21]).
- Sayfa içi aramalar (/cariler, /urunler, /ekranlar, /emanet, /evraklar, /siparisler, /gider-pusulalari, /uyum) liste açmıyor, satır içi süzüyor. Hepsinde boş sonuç mesajı var: "Aramaya uyan müşteri yok.", "Ürün bulunamadı", "“zzqxw” adında bir ekran yok…".

Not: tezgâhtar ve sahip 1280 açık görüntüleri aynı adla kaydedildiğinden `foto/acilir/*KORU_aydinlik_1280*` sahibin görüntüsüdür; tezgâhtar ölçümleri ham kayıtta ayrıdır.

| Durum | Rota | Katman | Tür | Öğe | Açıldı | Kutu (x,y,g,y) | Taşma sağ/sol/alt | Örtülen | Seçenek | En uzun satır | Kaydırma | Arama/boş mesaj | aria-expanded (açık→kapalı) | Değer korundu |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| sahip-KORU-aydinlik-1280 | /islemler | sayfa | arama | `#islem-barkod ""` | evet | [106, 263, 650, 221] | 0/0/0 | – | 3/3 | 2  | hayır | Bulunamadı | true→false | True |
| sahip-KORU-aydinlik-1280 | /cariler | sayfa | arama | `#cari-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan müşteri yok. | None→None | True |
| sahip-KORU-aydinlik-1280 | /cariler | acici:Yeni müşteri | secici | `#cari-tur "Müşteri"` | evet | [801, 249, 447, 283] | 0/0/0 | – | 5/5 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /cariler | acici:Yeni müşteri | secici | `#cari-kimlik-turu "Belirtilm` | evet | [801, 505, 320, 195] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /cariler | acici:Yeni müşteri | secici | `#cari-uyruk "Seçilmedi"` | evet | [928, 505, 320, 384] | 0/0/0 | – | 34/34 | 1  | evet | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /cariler | acici:Yeni müşteri | secici | `#cari-segment "Seçilmedi"` | evet | [928, 505, 320, 283] | 0/0/0 | – | 5/5 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /cariler | acici:Yeni müşteri | secici | `#cari-fiyat-listesi "— (Vars` | evet | [801, 590, 447, 195] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /cariler | acici:Yeni müşteri | arama | `#cari-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan müşteri yok. | None→None | True |
| sahip-KORU-aydinlik-1280 | /ekranlar | sayfa | arama | `#ekranlar-ara ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | “zzqxw” adında bir ekran yok. Başka bir sözcü | None→None | True |
| sahip-KORU-aydinlik-1280 | /urunler | sayfa | arama | `#urun-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Ürün bulunamadı | None→None | True |
| sahip-KORU-aydinlik-1280 | /urunler | acici:Yeni ürün | secici | `#u-ayar "Ayar seçin"` | evet | [928, 257, 320, 384] | 0/0/0 | – | 8/8 | 1  | evet | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /urunler | acici:Yeni ürün | secici | `#u-tip "Ziynet"` | evet | [801, 125, 320, 384] | 0/0/0 | – | 11/11 | 1  | evet | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /urunler | acici:Yeni ürün | arama | `#urun-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Ürün bulunamadı | None→None | True |
| sahip-KORU-aydinlik-1280 | /urunler/agac | acici:Yeni grup | secici | `#k-ust "— kök seviye —"` | evet | [833, 275, 320, 195] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /emanet | sayfa | arama | `#yildiz-emanet-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Bu sekmede kayıt yok. | None→None | True |
| sahip-KORU-aydinlik-1280 | /gider | sayfa | secici | `#gd-kasa "—"` | evet | [64, 413, 636, 326] | 0/0/0 | – | 4/4 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /gider | sayfa | secici | `#gd-kategori "—"` | evet | [64, 469, 636, 326] | 0/0/0 | – | 4/4 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /gider | sayfa | secici | `#gd-personel "—"` | evet | [64, 625, 636, 326] | 0/0/52 | – | 4/4 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /faturalar | acici:Düzenle | secici | `#fd-duzenle-cari "Derya Yıld` | evet | [833, 184, 415, 371] | 0/0/0 | – | 7/7 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /faturalar | acici:Düzenle | secici | `#fd-kalem-yeni-urun "—"` | evet | [845, 470, 391, 384] | 0/0/0 | – | 19/19 | 1  | evet | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /faturalar | acici:Düzenle | secici | `#fd-duzenle-cari "Selin Ergü` | evet | [833, 184, 415, 371] | 0/0/0 | – | 7/7 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /faturalar | acici:Düzenle | secici | `#fd-kalem-yeni-urun "—"` | evet | [845, 470, 391, 384] | 0/0/0 | – | 19/19 | 1  | evet | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /gider-pusulalari | sayfa | arama | `#yildiz-pusula-ara ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan satıcı yok. | None→None | True |
| sahip-KORU-aydinlik-1280 | /raporlar | sayfa | secici | `#rp-yil "2026"` | evet | [64, 359, 320, 151] | 0/0/0 | – | 2/2 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /raporlar | sayfa | secici | `#rp-ay "Ekim"` | evet | [192, 359, 320, 384] | 0/0/0 | – | 12/12 | 1  | evet | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /satin-alma | acici:+ Yeni sipariş | secici | `#sa-form-tedarikci "Tedarikç` | evet | [833, 184, 415, 104] | 0/0/0 | – | 1/1 | 1  | hayır | Yükleniyor... | true→false | True |
| sahip-KORU-aydinlik-1280 | /satin-alma | acici:+ Yeni sipariş | secici | `#sa-form-urun-0 "Ürün seçin"` | evet | [833, 298, 415, 104] | 0/0/0 | – | 1/1 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /irsaliyeler | acici:+ Sevk irsaliy | secici | `#irs-form-alici "Alıcıyı seç` | evet | [833, 184, 415, 104] | 0/0/0 | – | 1/1 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /irsaliyeler | acici:+ Sevk irsaliy | secici | `#irs-form-urun-0 "Ürün seçin` | evet | [833, 298, 415, 104] | 0/0/0 | – | 1/1 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "Profesyonel"` | evet | [801, 348, 447, 195] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "İşletme vars` | evet | [801, 348, 447, 195] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-rol "Müdür"` | evet | [801, 499, 320, 195] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-durum "Aktif"` | evet | [928, 499, 320, 151] | 0/0/0 | – | 2/2 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "İşletme vars` | evet | [801, 348, 447, 195] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-rol "Tezgâhtar"` | evet | [801, 499, 320, 195] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-durum "Aktif"` | evet | [928, 499, 320, 151] | 0/0/0 | – | 2/2 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "İşletme vars` | evet | [801, 348, 447, 195] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-rol "Muhasebe"` | evet | [801, 499, 320, 195] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-durum "Pasif"` | evet | [928, 499, 320, 151] | 0/0/0 | – | 2/2 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "İşletme vars` | evet | [801, 348, 447, 195] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-rol "Tezgâhtar"` | evet | [801, 499, 320, 195] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /kullanicilar | acici:Düzenle | secici | `#d-durum "Pasif"` | evet | [928, 499, 320, 151] | 0/0/0 | – | 2/2 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /isletme | sayfa | secici | `#i-logo-tur "Görsel"` | evet | [77, 509, 1126, 107] | 0/0/0 | – | 1/1 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /uyum | sayfa | arama | `#ara-belge ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Bu belge numarasına ait kimlik tespiti buluna | None→– | None |
| sahip-KORU-aydinlik-1280 | /veri-ice-aktarim | sayfa | secici | `#ia-kaynak "Müşteri / cari k` | evet | [64, 392, 1152, 239] | 0/0/0 | – | 4/4 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-aydinlik-1280 | /kurulum | sayfa | secici | `#kur-acilis-tur ""` | evet | [88, 719, 1104, 195] | 0/0/14 | #kur-acilis-ac "Açılış bakiyesi Kasa, ba | 3/3 | 0  | hayır | – | true→false | True |
| sahip-KORU-aydinlik-1280 | /evraklar | sayfa | arama | `#yildiz-evrak-ara ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan evrak yok. | None→None | True |
| sahip-KORU-aydinlik-1280 | /siparisler | sayfa | arama | `#yildiz-siparis-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan iş yok. | None→None | True |
| sahip-KORU-aydinlik-1280 | /siparisler/01M4Z6HCVBFGCF65XMTPQ8VZ7H | sayfa | secici | `#sip-belge-tur "Görsel"` | evet | [77, 476, 1126, 195] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /islemler | sayfa | arama | `#islem-barkod ""` | evet | [58, 257, 301, 265] | 0/0/0 | – | 4/4 | 2 (kesik 3) | hayır | Bulunamadı | true→false | True |
| sahip-KORU-karanlik-375 | /cariler | sayfa | arama | `#cari-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan müşteri yok. | None→None | True |
| sahip-KORU-karanlik-375 | /cariler | acici:Yeni müşteri | arama | `#cari-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan müşteri yok. | None→None | True |
| sahip-KORU-karanlik-375 | /ekranlar | sayfa | arama | `#ekranlar-ara ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | “zzqxw” adında bir ekran yok. Başka bir sözcü | None→None | True |
| sahip-KORU-karanlik-375 | /urunler | sayfa | arama | `#urun-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Ürün bulunamadı | None→None | True |
| sahip-KORU-karanlik-375 | /urunler | acici:Yeni ürün | secici | `#u-ayar "Ayar seçin"` | evet | [16, 385, 343, 384] | 0/0/0 | – | 8/8 | 1  | evet | – | true→false | True |
| sahip-KORU-karanlik-375 | /urunler | acici:Yeni ürün | secici | `#u-tip "Ziynet"` | evet | [16, 37, 343, 384] | 0/0/0 | – | 11/11 | 1  | evet | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /urunler | acici:Yeni ürün | arama | `#urun-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Ürün bulunamadı | None→None | True |
| sahip-KORU-karanlik-375 | /urunler/agac | acici:Yeni grup | secici | `#k-ust "— kök seviye —"` | evet | [16, 275, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /emanet | sayfa | arama | `#yildiz-emanet-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Bu sekmede kayıt yok. | None→None | True |
| sahip-KORU-karanlik-375 | /gise | sayfa | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /gider | sayfa | secici | `#gd-kasa "—"` | hayır | – | – | – | –/– | –  | hayır | – | true→true | True |
| sahip-KORU-karanlik-375 | /gider | sayfa | secici | `#gd-kategori "—"` | evet | [16, 425, 343, 326] | 0/0/0 | – | 4/4 | 1  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /gider | sayfa | secici | `#gd-personel "—"` | evet | [16, 425, 343, 326] | 0/0/0 | – | 4/4 | 1  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /e-belgeler | sayfa | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /raporlar | sayfa | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /raporlar/vade-takibi | acici:+ Başka gün | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /satin-alma | acici:+ Yeni sipariş | secici | `#sa-form-tedarikci "Tedarikç` | evet | [16, 184, 343, 104] | 0/0/0 | – | 1/1 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /satin-alma | acici:+ Yeni sipariş | secici | `#sa-form-urun-0 "Ürün seçin"` | evet | [16, 298, 343, 104] | 0/0/0 | – | 1/1 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /satin-alma | acici:+ Yeni sipariş | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /irsaliyeler | acici:+ Sevk irsaliy | secici | `#irs-form-alici "Alıcıyı seç` | evet | [16, 184, 343, 104] | 0/0/0 | – | 1/1 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /irsaliyeler | acici:+ Sevk irsaliy | secici | `#irs-form-urun-0 "Ürün seçin` | evet | [16, 298, 343, 104] | 0/0/0 | – | 1/1 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /irsaliyeler | acici:+ Sevk irsaliy | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "Basit"` | evet | [16, 461, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "İşletme vars` | evet | [16, 461, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-rol "Müdür"` | evet | [16, 470, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-durum "Aktif"` | evet | [16, 440, 343, 159] | 0/0/0 | – | 2/2 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "İşletme vars` | evet | [16, 461, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-rol "Tezgâhtar"` | evet | [16, 470, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-durum "Aktif"` | evet | [16, 440, 343, 159] | 0/0/0 | – | 2/2 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "İşletme vars` | evet | [16, 461, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-rol "Muhasebe"` | evet | [16, 470, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-durum "Kullanım dışı"` | evet | [16, 440, 343, 159] | 0/0/0 | – | 2/2 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-arayuz-modu "İşletme vars` | evet | [16, 461, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-rol "Tezgâhtar"` | evet | [16, 470, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | secici | `#d-durum "Kullanım dışı"` | evet | [16, 440, 343, 159] | 0/0/0 | – | 2/2 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /kullanicilar | acici:Düzenle | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /isletme | sayfa | secici | `#i-logo-tur "Görsel"` | evet | [29, 432, 320, 111] | 0/0/0 | – | 1/1 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| sahip-KORU-karanlik-375 | /ayarlar | acici:Ayrıntılı görü | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kvkk | sayfa | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /veri-ice-aktarim | sayfa | secici | `#ia-kaynak "Müşteri kartları` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /kurulum | sayfa | secici | `#kur-acilis-tur ""` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /siparisler | sayfa | arama | `#yildiz-siparis-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan iş yok. | None→None | True |
| sahip-KORU-karanlik-375 | /yedekler | sayfa | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| sahip-KORU-karanlik-375 | /siparisler/01M4Z6HCVBFGCF65XMTPQ8VZ7H | sayfa | secici | `#sip-belge-tur "Görsel"` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| tezgahtar-KORU-aydinlik-1280 | /islemler | sayfa | arama | `#islem-barkod ""` | evet | [106, 263, 650, 265] | 0/0/0 | – | 4/4 | 2  | hayır | Bulunamadı | true→false | True |
| tezgahtar-KORU-aydinlik-1280 | /cariler | sayfa | arama | `#cari-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan müşteri yok. | None→None | True |
| tezgahtar-KORU-aydinlik-1280 | /cariler | acici:Yeni müşteri | arama | `#cari-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan müşteri yok. | None→None | True |
| tezgahtar-KORU-aydinlik-1280 | /ekranlar | sayfa | arama | `#ekranlar-ara ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | “zzqxw” adında bir ekran yok. Başka bir sözcü | None→None | True |
| tezgahtar-KORU-aydinlik-1280 | /urunler | sayfa | arama | `#urun-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Ürün bulunamadı | None→None | True |
| tezgahtar-KORU-aydinlik-1280 | /urunler | acici:Yeni ürün | secici | `#u-ayar "Ayar seçin"` | evet | [928, 257, 320, 384] | 0/0/0 | – | 8/8 | 1  | evet | – | true→false | True |
| tezgahtar-KORU-aydinlik-1280 | /urunler | acici:Yeni ürün | secici | `#u-tip "Ziynet"` | evet | [801, 125, 320, 384] | 0/0/0 | – | 11/11 | 1  | evet | Eşleşen seçenek yok. | true→false | True |
| tezgahtar-KORU-aydinlik-1280 | /urunler | acici:Yeni ürün | arama | `#urun-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Ürün bulunamadı | None→None | True |
| tezgahtar-KORU-aydinlik-1280 | /urunler/agac | acici:Yeni grup | secici | `#k-ust "— kök seviye —"` | evet | [833, 275, 320, 195] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| tezgahtar-KORU-aydinlik-1280 | /emanet | sayfa | arama | `#yildiz-emanet-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Bu sekmede kayıt yok. | None→None | True |
| tezgahtar-KORU-aydinlik-1280 | /faturalar | acici:Düzenle | secici | `#fd-duzenle-cari "Derya Yıld` | evet | [833, 184, 415, 371] | 0/0/0 | – | 7/7 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| tezgahtar-KORU-aydinlik-1280 | /faturalar | acici:Düzenle | secici | `#fd-kalem-yeni-urun "—"` | evet | [845, 470, 391, 384] | 0/0/0 | – | 19/19 | 1  | evet | – | true→false | True |
| tezgahtar-KORU-aydinlik-1280 | /faturalar | acici:Düzenle | secici | `#fd-duzenle-cari "Selin Ergü` | evet | [833, 184, 415, 371] | 0/0/0 | – | 7/7 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| tezgahtar-KORU-aydinlik-1280 | /faturalar | acici:Düzenle | secici | `#fd-kalem-yeni-urun "—"` | evet | [845, 470, 391, 384] | 0/0/0 | – | 19/19 | 1  | evet | – | true→false | True |
| tezgahtar-KORU-aydinlik-1280 | /evraklar | sayfa | arama | `#yildiz-evrak-ara ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Bu ekran için yetkiniz yok (evrak) | None→None | True |
| tezgahtar-KORU-aydinlik-1280 | /siparisler | sayfa | arama | `#yildiz-siparis-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan iş yok. | None→None | True |
| tezgahtar-KORU-aydinlik-1280 | /siparisler/01M4Z6HCVBFGCF65XMTPQ8VZ7H | sayfa | secici | `#sip-belge-tur "Görsel"` | evet | [77, 476, 1126, 195] | 0/0/0 | – | 3/3 | 1  | hayır | Eşleşen seçenek yok. | true→false | True |
| tezgahtar-KORU-karanlik-375 | /islemler | sayfa | arama | `#islem-barkod ""` | evet | [58, 257, 301, 265] | 0/0/0 | – | 4/4 | 2 (kesik 3) | hayır | Bulunamadı | true→false | True |
| tezgahtar-KORU-karanlik-375 | /cariler | sayfa | arama | `#cari-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan müşteri yok. | None→None | True |
| tezgahtar-KORU-karanlik-375 | /cariler | acici:Yeni müşteri | arama | `#cari-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan müşteri yok. | None→None | True |
| tezgahtar-KORU-karanlik-375 | /ekranlar | sayfa | arama | `#ekranlar-ara ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | “zzqxw” adında bir ekran yok. Başka bir sözcü | None→None | True |
| tezgahtar-KORU-karanlik-375 | /urunler | sayfa | arama | `#urun-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Ürün bulunamadı | None→None | True |
| tezgahtar-KORU-karanlik-375 | /urunler | acici:Yeni ürün | secici | `#u-ayar "Ayar seçin"` | evet | [16, 385, 343, 384] | 0/0/0 | – | 8/8 | 1  | evet | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /urunler | acici:Yeni ürün | secici | `#u-tip "Ziynet"` | evet | [16, 37, 343, 384] | 0/0/0 | – | 11/11 | 1  | evet | Eşleşen seçenek yok. | true→false | True |
| tezgahtar-KORU-karanlik-375 | /urunler | acici:Yeni ürün | arama | `#urun-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Ürün bulunamadı | None→None | True |
| tezgahtar-KORU-karanlik-375 | /urunler/agac | acici:Yeni grup | secici | `#k-ust "— kök seviye —"` | evet | [16, 275, 343, 207] | 0/0/0 | – | 3/3 | 1  | hayır | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /emanet | sayfa | arama | `#yildiz-emanet-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Bu sekmede kayıt yok. | None→None | True |
| tezgahtar-KORU-karanlik-375 | /gise | sayfa | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /faturalar | acici:Düzenle | secici | `#fd-duzenle-cari "Derya Yıld` | evet | [16, 184, 343, 384] | 0/0/0 | – | 7/7 | 1  | evet | Eşleşen seçenek yok. | true→false | True |
| tezgahtar-KORU-karanlik-375 | /faturalar | acici:Düzenle | secici | `#fd-kalem-yeni-urun "—"` | evet | [28, 426, 320, 378] | 0/0/0 | – | 19/19 | 1  | evet | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /faturalar | acici:Düzenle | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /faturalar | acici:Düzenle | secici | `#fd-duzenle-cari "Selin Ergü` | evet | [16, 184, 343, 384] | 0/0/0 | – | 7/7 | 1  | evet | Eşleşen seçenek yok. | true→false | True |
| tezgahtar-KORU-karanlik-375 | /faturalar | acici:Düzenle | secici | `#fd-kalem-yeni-urun "—"` | evet | [28, 426, 320, 378] | 0/0/0 | – | 19/19 | 1  | evet | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /faturalar | acici:Düzenle | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /raporlar/vade-takibi | acici:+ Başka gün | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /ayarlar | acici:Ayrıntılı görü | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /siparisler | sayfa | arama | `#yildiz-siparis-arama ""` | satır içi süzgeç | – | – | – | –/– | –  | hayır | Aramaya uyan iş yok. | None→None | True |
| tezgahtar-KORU-karanlik-375 | /yedekler | sayfa | menu | `#ust-kayit-ara "Müşteri, ürü` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |
| tezgahtar-KORU-karanlik-375 | /siparisler/01M4Z6HCVBFGCF65XMTPQ8VZ7H | sayfa | secici | `#sip-belge-tur "Görsel"` | hayır | – | – | – | –/– | –  | hayır | – | true→false | True |

Toplam ölçülen açılır öğe: 137; sorunlu (taşma/örtülme/kesik seçenek): 4.


## 3. Seçim sonrası görünüm

Seçim yapılmadı (kural). Ölçüm, değeri hâlihazırda dolu olan seçicilerde yapıldı.

| Öğe türü | Seçili değer | Etiket | Aynı formdaki girdi | Sayfa h1 | Sonuç |
|---|---|---|---|---|---|
| Secici (Yıldız ekranları) | 22 px / 300 / lh 1,2 | 12 px / 500 büyük harf | 18 px | 64 px / 200 | Başlıkla karışmıyor ama girdiden 4 px büyük |
| Secici (eski kabuk, sipariş detayı) | 22 px / 300 | 12 px | 18 px | **22 px / 300** | **Başlıkla aynı boy** — başlık gibi |
| Satın alma / İrsaliye çekmece seçicisi | 14 px | 12 px | 18 px | 64 px | Diğer seçicilerden küçük ([O-21]) |

| Durum | Rota | Öğe | Değer | Değer px/ağırlık/lh | Etiket px/ağırlık | Girdi px | h1 px/ağırlık | Başlık gibi mi |
|---|---|---|---|---|---|---|---|---|
| BASIT/karanlik/375 | /isletme | `span.secici-metin.secici-metin` | Görsel | 22/300/1.2 | –/– | 18 | 40/200 | hayır |
| BASIT/karanlik/375 | /veri-ice-aktarim | `span.secici-metin.secici-metin` | Müşteri kartları | 22/300/1.2 | –/– | 22 | 40/200 | hayır |
| BASIT/karanlik/375 | /kurulum | `span.secici-metin.secici-metin` |  | 22/300/1.2 | –/– | – | 40/200 | hayır |
| BASIT/karanlik/375 | /siparisler/01M4Z6HCVBFGCF65XMTPQ8VZ7H | `span.secici-metin.secici-metin` | Görsel | 22/300/1.2 | –/– | 18 | 18/300 | EVET |
| PRO/aydinlik/1280 | /raporlar | `span.secici-metin.secici-metin` | 2026 | 22/300/1.2 | –/– | – | 64/200 | hayır |
| PRO/aydinlik/1280 | /raporlar | `span.secici-metin.secici-metin` | Ekim | 22/300/1.2 | –/– | – | 64/200 | hayır |
| PRO/aydinlik/1280 | /isletme | `span.secici-metin.secici-metin` | Görsel | 22/300/1.2 | –/– | 18 | 64/200 | hayır |
| PRO/aydinlik/1280 | /veri-ice-aktarim | `span.secici-metin.secici-metin` | Müşteri / cari kartlar | 22/300/1.2 | –/– | 22 | 64/200 | hayır |
| PRO/aydinlik/1280 | /kurulum | `span.secici-metin.secici-metin` |  | 22/300/1.2 | –/– | – | 64/200 | hayır |
| PRO/aydinlik/1280 | /siparisler/01M4Z6HCVBFGCF65XMTPQ8VZ7H | `span.secici-metin.secici-metin` | Görsel | 22/300/1.2 | –/– | 18 | 22/300 | EVET |

Ölçülen seçili değer: 10; başlık gibi görünen: 2.


## 4. Yazı ölçeği tablosu

**Genel:**
- Yazı tipi: **Inter**, yerel woff2 dosyalarından yükleniyor. 660 sayfa-durumunun 648'inde tek aile; belge ekranlarında ek olarak monospace var.
- `html lang="tr"`: 660/660. Büyük harfe çevrilen metinlerde Türkçe dışı `lang` sayısı 0 (İ/ı doğru).
- 375'te 16 px altı girdi: **0** (iOS yakınlaştırma riski yok).
- En küçük metin: arayüzde 12 px; belge önizlemelerinde 10 px.
- Tutar sütunları: sola yaslı tutar hücresi yalnız fatura önizlemesindeki "Sıra 1". `tabular-nums` eksiği 0.
- Satır uzunluğu: 90 karakter üstü paragraf 0. Satır yüksekliği <1,4 olan çok satırlı metin [D-6].

**1280 px, Profesyonel, açık — sınıf başına en sık boylar (px/ağırlık):**

| Sınıf | En sık | Farklı stil sayısı |
|---|---|---|
| h1 (görünür) | 64/200 (34), 40/300 (8), 22/300 (6), 24/600 (3), 36/300 (1) | 5 |
| h2 | 16/500 (30), 28/500 (22), 12/500 (19), 10/500 (12), 20/500 (4) | 7 |
| etiket | 12/400 (27), 14/500 (21), 13/400 (9), 13/500 (4) | 4 |
| seçili değer | 22/300 | 1 |
| gövde (p/li) | 12/500 (125), 18/400 (125), 14/400 (45), 22/300 (34), 18/500 (21) | 21 |
| düğme | 16/500 (185), 14/400 (72), 15/400 (42), 19/400 (30), 16/400 (11), 22/300 (11) | 15 |
| tablo başlığı (th) | 13/500 (94), 10/500 (16) | 2 |
| tablo hücresi (td) | 15/500 (463), 13/400 (138), 12/500 (104), 15/400 (85), 16/500 (60) | 9 |
| **Tüm boylar (px)** | 10, 11, 12, 13, 13.3, 14, 14.7, 15, 16, 16.7, 16.8, 17, 18, 19, 20, 22, 24, 28, 36, 40, 52, 56, 64, 72, 104, 112 | **22+** |

**Modüler ölçek:** çekirdekte 12 → 14 → 16 → 18 → 22 → 28 → 40 → 64 dizisi var (ardışık oran 1,13–1,6). Bu tutarlı bir oran değil ve arada 1 px aralıklı boylar çok. 375'te h1 40/200'e iniyor; diğer sınıflar aynı kalıyor.

Rota başına ayrıntı:

| Rota | h1 | h2 | etiket | seçili değer | gövde (p/li) | düğme | tablo hücresi | en küçük |
|---|---|---|---|---|---|---|---|---|
| / | – | 12/500 | – | – | 15/400, 18/400 | 14/400 | – | 12 |
| /islemler | – | – | – | – | 14/400, 16/400 | 14/500, 15/400 | – | 12 |
| /cariler | 64/200 | 16/500, 12/500 | – | – | 12/500, 14/400 | 14/400, 16/500 | – | 12 |
| /kasalar | 40/300 | – | – | – | 12/500, 20/300 | 15/400, 14/400 | – | 12 |
| /ekranlar | 64/200 | 12/500 | – | – | 12/500 | 14/400 | – | 12 |
| /urunler | 64/200 | – | – | – | 12/500, 14/400 | 19/400, 14/400 | – | 12 |
| /urunler/agac | 64/200 | – | – | – | 12/500, 14/400 | 19/400, 14/400 | – | 12 |
| /urunler/etiketler | 64/200 | – | 12/400 | – | 12/400, 12/500 | 16/400, 15/400 | – | 12 |
| /emanet | 64/200 | – | – | – | 12/500, 14/400 | 14/400, 19/400 | – | 12 |
| /kur | 40/300 | – | – | – | 12/500, 20/300 | 14/400, 16/500 | – | 12 |
| /pos | 64/200 | – | – | – | 12/500 | 15/400, 14/400 | – | 12 |
| /pos/tahsilatlar | 64/200 | – | – | – | 12/500, 14/400 | 14/400, 15/400 | – | 12 |
| /vezne | 64/200 | – | – | – | 12/500, 18/400 | 14/400, 15/400 | – | 12 |
| /gise | 22/300 | 16/500, 12/500 | – | – | 14/500, 18/300 | 16/500, 14/400 | – | 12 |
| /banka | 64/200 | 16.8/600 | 14/500 | – | 12/500, 18/400 | 16/500, 15/400 | – | 12 |
| /gider | 64/200 | – | 12/400 | – | 12/500, 12/400 | 22/300, 14/400 | – | 12 |
| /hurda | 40/300 | – | – | – | 12/500, 20/300 | 15/400, 14/400 | – | 12 |
| /faturalar | 64/200 | 16/500 | – | – | – | 16/500, 14/400 | 15/500, 12/500 | 12 |
| /e-belgeler | 22/300 | – | – | – | 18/300 | 16/500, 14/400 | – | 14 |
| /gider-pusulalari | 64/200 | – | – | – | 12/500, 14/400 | 14/400, 16/500 | – | 12 |
| /raporlar | 64/200 | 16/500, 12/500 | 12/400 | – | 22/300, 12/500 | 16/500, 22/300 | – | 12 |
| /raporlar/kar-zarar | 40/300 | 28/500, 16/500 | 12/400, 13/400 | – | 12/500, 22/300 | 16/500, 14/400 | – | 12 |
| /raporlar/vade-takibi | 64/200 | – | 12/400, 13/400 | – | 12/500, 22/300 | 16/500, 14/400 | 15/500, 15/400 | 12 |
| /raporlar/islem-defteri | 64/200 | 16/500 | 12/400, 13/400 | – | 16/400, 12/500 | 16/500, 14/400 | 15/500, 13/400 | 12 |
| /raporlar/satis-analizi | 40/300 | 28/500, 16/500 | 12/400, 13/400 | – | 12/500, 17/300 | 16/500, 14/400 | 15/500 | 12 |
| /raporlar/kasa | 40/300 | – | – | – | 12/500, 20/300 | 14/400, 16/500 | – | 12 |
| /fiyatlandirma | 64/200 | – | – | – | 12/500, 14/400 | 15/400, 19/400 | – | 12 |
| /sayim | 40/300 | – | – | – | 12/500, 20/300 | 14/400, 16/500 | – | 12 |
| /satin-alma | 64/200 | 28/500, 16/500 | – | – | 14/500 | 16/500, 14/400 | 15/500, 15/400 | 12 |
| /irsaliyeler | 64/200 | 16/500, 12/500 | – | – | 14/500 | 16/500, 14/400 | 15/400, 16/500 | 12 |
| /kullanicilar | 64/200 | – | – | – | – | 16/500, 14/400 | 12/500, 15/500 | 12 |
| /yetkiler | 64/200 | – | – | – | 14/400, 18/400 | 16/500, 14/400 | 14/400, 15/500 | 12 |
| /isletme | 64/200 | 28/500 | 12/400 | – | 18/400, 12/500 | 16/500, 14/400 | – | 12 |
| /ayarlar | 64/200 | 20/500, 28/500 | 14/500, 13/500 | – | 12/500, 14/400 | 16/500, 15/500 | – | 12 |
| /kilavuz | 64/200 | 28/500 | – | – | 18/400, 18/500 | 16/500, 14/400 | – | 12 |
| /uyum | 40/300 | – | – | – | 12/500, 22/300 | 16/500, 14/400 | 15/500, 15/400 | 12 |
| /kvkk | 22/300 | – | – | – | 18/400 | 16/500, 14/400 | – | 14 |
| /denetim-izi | 64/200 | 16/500 | – | – | 16/400, 12/500 | 16/500, 14/400 | 15/500, 15/400 | 12 |
| /veri-disa-aktarim | 64/200 | 28/500 | 14/500, 12/400 | – | 14/400, 12/500 | 14/400, 16/500 | – | 12 |
| /veri-ice-aktarim | 64/200 | 16/500, 18/500 | 12/400 | – | 12/500 | 16/500, 14/400 | – | 12 |
| /kurulum | 64/200 | 28/500 | 12/400 | – | 14/500, 12/500 | 16/500, 14/400 | – | 12 |
| /bildirimler | 64/200 | – | – | – | 12/500, 14/400 | 14/400, 19/400 | – | 12 |
| /evraklar | 64/200 | – | – | – | 12/500 | 14/400, 15/400 | – | 12 |
| /evraklar/takvim | 64/200 | – | – | – | 12/500 | 14/400, 15/400 | – | 12 |
| /siparisler | 64/200 | – | – | – | 12/500 | 14/400, 15/400 | – | 12 |
| /yedekler | 64/200 | – | – | – | 12/500, 12/400 | 16/400, 16/500 | – | 12 |
| /vitrin | 22/300 | 16/500, 12/500 | – | – | 14/400, 14/500 | 16/500, 14/400 | 15/500 | 12 |
| /vitrin/ekran | – | 28/500 | – | – | 18/400 | 16/500 | – | 16 |
| /yasal-sinirlar | 64/200 | 16/500, 12/500 | – | – | 18/400, 18/500 | 14/400 | – | 12 |
| /siparisler/01M4Z6HCVBFGCF65XMTPQ8 | 22/300 | 28/500, 16/500 | 12/400 | – | 14/400, 14/500 | 16/500, 14/400 | 15/500, 15/400 | 12 |
| /gider-pusulalari/01M4Z6HCXJ29E5AP | 24/600 | 10/500, 28/500 | – | – | 13.3/400, 13.3/500 | 16/500, 14/400 | 13.3/400, 16.7/500 | 10 |
| /cikti/FATURA/01M5D9MGTDWBQGT92KHP | 24/600 | 10/500 | – | – | 13.3/400, 13.3/500 | 16/500, 18/500 | 13.3/400, 16.7/700 | 10 |
| /cikti/SATIS_FISI/01M5D9MGDM8ZZNJQ | 24/600 | 10/500 | – | – | 13.3/400, 13.3/500 | 16/500, 18/500 | 13.3/400, 16.7/700 | 10 |
| /banka/ekstre/01M00000000000000000 | 22/300 | – | – | – | 14/500 | 16/500, 14/400 | – | 14 |
| /fiyat/denetim-gecersiz-anahtar | – | – | – | – | – | – | – | 14 |

## 5. Test türü × sonuç matrisi

| # | Test türü | Sonuç | Gerekçe |
|---|---|---|---|
| 1 | Taşma / sıkışma | **KALDI** | K-1 çakışma; Y-1/Y-2 yapışkan panel; O-1 320 px; O-2 1024; O-10 uzun içerik; O-11. (1280/375 matrisinde K3/K13/yatay = 0) |
| 2 | Açılır listeler | **GEÇTİ (koşullu)** | 137 ölçümün tamamında öğe görünür ve erişilebilir; yalnız 375 barkod listesi panelin altında (Y-2), /gider 52 px taşma, farklı ölçülü 4 seçici |
| 3 | Seçim sonrası görünüm | **KALDI (ORTA)** | Seçili değer girdi metninden büyük; eski kabukta başlıkla aynı boy (O-8) |
| 4 | Tipografi | **KALDI** | 22+ punto, 5 başlık biçimi, 15 düğme stili (O-6, O-7). Yazı tipi, Türkçe büyük harf, iOS 16 px, tabular-nums GEÇTİ |
| 5 | Düzen | **KALDI** | İki tasarım dili (Y-5), 375 ikinci araç satırı (Y-6), takvim hizası (O-20). Ekran başına birincil eylem >1 olan ekran yok (0/55) |
| 6 | Erişilebilirlik WCAG 2.2 AA | **KALDI** | 1.4.3 GEÇTİ (0 ihlal); 1.4.10, 1.4.11, 2.4.7, 2.4.11, 2.4.3 KALDI (§ 6) |
| 7 | Duyarlılık | **KALDI** | 320, 1024, 812×375 ve 667×375'te bulgu; 414, 768, 1440, 1920 temiz |
| 8 | Bileşen durumları | **KALDI (ORTA)** | Active/hover eksik (O-9); hata durumu GEÇTİ (aria-invalid + mesaj); disabled'da sebep var (Satış) / yok (/isletme "Yükle") |
| 9 | Nielsen sezgileri | Ortalama şiddet 1,9 (en yüksek 3) | § 7 |
| 10 | Yerelleştirme | **GEÇTİ (koşullu)** | EN'de 110 sayfa-durumu, `lang=en`, taşma yok; kalan Türkçe metinler veri; D-12. TR'de tarih dd.MM.yyyy ve ₺, "TRY" 0, ISO/ABD tarih 0, ham kod 0 |
| 11 | Performans algısı | **GEÇTİ (koşullu)** | Soğuk açılış: giriş formu 0,79–0,82 sn, LCP ~1,6 sn, CLS 0,0002. Ekran geçişi medyan 0,87 sn, p90 1,2 sn. 375'te 14 durumda CLS >0,1 (O-15). Konsol hatası yalnız 404/401/500 sonuçları; 500 bir kez (O-18) |
| 12 | Tema ve yazdırma | **KALDI** | Koyu tema ekranda okunur (kontrast ihlali 0). Koyu temada rapor baskısı bozuk (Y-3), işlem defteri baskısı (Y-4), baskıda düğmeler (O-16). Belge (fiş/fatura/irsaliye/pusula/sayım) baskıları GEÇTİ; 80 mm fiş anahtarla 1 sayfa, taşma yok |

## 6. WCAG 2.2 AA — ihlal sayıları

**Sayım birimi:** "sayfa-durumu" = rota × genişlik × mod × tema. Sahip 440 + tezgâhtar 220 = 660 durum.

| Kriter | Sonuç | Sayı / kanıt |
|---|---|---|
| 1.4.3 Kontrast (metin) | GEÇTİ | 24.758 metin öğesi ölçüldü, **0 ihlal**. 2.228 öğe görsel zemin nedeniyle belirsiz kaldı; koyu temada da 0 |
| 1.4.11 Metin dışı kontrast | KALDI | /gider form çizgisi 1,27:1 (5 alan × 8 durum). Arama çizgileri 3,26:1 ile geçti |
| 1.4.1 Yalnız renkle bilgi | GEÇTİ (gözle) | Borç tutarları kırmızı ama "biz borçluyuz / bize borçlu" metni de var; durum rozetleri metinli ("Tamamlandı", "Eksik", "Kesildi"). Grafikte (satış analizi) çubuklar eksen etiketli |
| 1.4.4 Metni yeniden boyutlandırma | GEÇTİ (zoom) / ORTA (kök yazı) | %200 yakınlaştırmada 55 ekranın 48'inde yeni sorun yok; 7'sinde yalnız kesik değer. Kök yazı %200'de metnin %22'si büyüyor (O-17) |
| 1.4.10 Yeniden akış (320 px) | KALDI | 53/55 rota 8 px yatay kaydırma (O-1) |
| 1.4.12 Metin aralığı | GEÇTİ (koşullu) | 55 ekranın 52'sinde yeni sorun yok; işlem defteri (K3c 9), /ekranlar (1 binme), pusula belgesi (1 taşma) |
| 1.4.13 Üzerine gelince içerik | ÖLÇÜLMEDİ | Fare-üstü balonlar için kapatılabilirlik ve kalıcılık otomatik ölçülmedi (gerçek imleç hareketi gerekir) |
| 2.1.1 Klavye | GEÇTİ | Tüm ekranlarda Tab turu döngüsel; tuzak 0. Secici ↓/Esc çalışıyor |
| 2.4.1 Blokları atla | KALDI (DÜŞÜK) | "İçeriğe geç" bağlantısı yok (55/55); `main` ve `nav` yer imleri var |
| 2.4.3 Odak sırası | KALDI | /islemler ilk Tab ödeme paneli; 30+ ekranda "Yardım" menüden önce (O-14) |
| 2.4.7 Odak görünür | KALDI | 55 ekranın 10'unda görünmez odak (arama kutuları, /gider formu, /kilavuz videoları) |
| 2.4.11 Odak gizlenmez | KALDI | /urunler, /banka (örtülü odak, K-1); /islemler yatay telefon (Y-1) |
| 2.5.8 Hedef boyutu (en az) | KALDI (yalnız çakışmada) | Boşluk istisnasıyla hesaplandı: 660 durumda yalnız /urunler ve /banka 1280 (K-1). Geri kalan küçük hedefler ("HAKAN Y." 73×20, "YARDIM" 62×15, "i" 20×20) komşusuz olduğu için geçiyor |
| 2.3.3 / prefers-reduced-motion | GEÇTİ | 53 ekranda çalışan animasyon → azaltma tercihiyle 0 |
| 1.3.1 Başlık yapısı | DÜŞÜK | 23 ekranda iki h1 (biri görsel olarak gizli); /isletme ve /raporlar'da düzey atlama |
| 4.1.2 Ad, rol, değer | GEÇTİ | Adsız görünür düğme 0 (ham sayaçtaki 4 rota doğrulamada düştü); etiketsiz girdi 0; alt metinsiz görsel 0; `aria-expanded` eksik combobox 0; hata durumunda `aria-invalid=true` + açıklama |
| 4.1.1 (2.2'de kaldırıldı) çift id | DÜŞÜK | 4 rota (D-13) |

Otomatik ham sayaçlar:

| Kriter | Sayfa-durumu sayısı (660 içinde) | Etkilenen rota |
|---|---|---|
| 1.3.1 h1 sayısı ≠ 1 | 284 | 26: /banka, /bildirimler, /cariler, /ekranlar, /emanet, /evraklar, /evraklar/takvim, /fiyat/denetim-gecersiz-anahtar … |
| 4.1.1/ çift id | 44 | 4: /evraklar, /gider-pusulalari, /kur, /vitrin |
| 4.1.2 adsız düğme (ham) | 36 | 4: /ayarlar, /kilavuz, /kurulum, /raporlar |
| 1.3.1 başlık düzeyi atlama | 12 | 2: /isletme, /raporlar |

(Ham sayaç: "adsız düğme" 4 rotası doğrulamada yanlış pozitif çıktı — § 1 sonu; "h1 ≠ 1", görsel olarak gizli üst bar h1'inden — D-1.)

axe-core yerelde yok (`~` altında arandı, bulunamadı). Ağdan indirme yapılmadı; ölçümler bu denetim için yazılan betiklerle yapıldı.

## 7. Nielsen'in 10 sezgisi

Şiddet: 0 = sorun yok · 1 = kozmetik · 2 = küçük · 3 = büyük · 4 = felaket.

| # | Sezgi | Şiddet | Örnek ekran / kanıt |
|---|---|---|---|
| 1 | Sistem durumunun görünürlüğü | **3** | Yanlış "Çevrimdışısınız — kayıt yapılamaz" bandı (Y-8). Olumlu: üst barda canlı has fiyatı + saat, "Yükleniyor…" durumları |
| 2 | Sistem ile gerçek dünyanın eşleşmesi | 2 | Kuyumcu dili iyi (has, ayar, ramat, emanet). Ama "(rapor)/(kullanici)" ham kodlar (Y-7), "Ekstre ithalatı bulunamadı", Pro'da "Yeni Cari", 375'te etiketsiz sayılar (D-9) |
| 3 | Kullanıcı denetimi ve özgürlük | 1 | Her çekmecede "×" ve Vazgeç; Secici Esc ile değeri koruyarak kapanıyor. Belgelerde "← Geri" |
| 4 | Tutarlılık ve standartlar | **3** | İki tasarım dili (Y-5), 3 farklı yetki karşılaması (Y-7), 5 başlık biçimi, 22 punto (O-6/O-7), "Parola/Şifre" (D-4) |
| 5 | Hata önleme | 2 | Olumlu: "Kaydet ve yazdır" ürün yokken pasif ve sebebi yazılı ("Önce ürün ekleyin."); yasal sınır uyarısı. Olumsuz: tezgâhtara yetkisiz ekranın denetimleri gösteriliyor (vade takibi 7/30/60 gün) |
| 6 | Hatırlamak yerine tanıma | 2 | 375'te etiketsiz sayılar; barkod sonucunda ayar·gram kesik (O-13). Olumlu: "Tüm ekranlar" rol bazında süzülmüş |
| 7 | Esneklik ve kullanım verimliliği | 1 | Basit/Profesyonel modu, "Kısayollar", barkod girişi, Pro'da sütun seçimi |
| 8 | Estetik ve sade tasarım | 2 | Yıldız ekranları sade ve şık. Ama 375'te ikinci araç satırı ve boşluk (Y-6); /yedekler ve /isletme'de küçük punto yığınları; üst üste binen bağlantılar (K-1) |
| 9 | Hataları tanıma, teşhis, düzeltme | 2 | Olumlu: alan hataları açık ve örnekli ("Geçerli bir e-posta yazın (ör. ad@isletme.com)"). Olumsuz: tezgâhtar /faturalar'da anlamsız kırmızı bant; /banka/ekstre "Ekstre ithalatı bulunamadı" çıkış yolu söylemiyor |
| 10 | Yardım ve belgeler | 1 | Her ekranda "Yardım", "Açıklama" katlanırı, Kılavuz + 9 eğitim videosu. Videolar klavyeyle görünmez odakta (O-5) |

**Ortalama 1,9 · en yüksek 3** (durum görünürlüğü, tutarlılık).

## 8. ISO/IEC 25010 — kullanılabilirlik (etkileşim yeteneği) özeti

| Alt nitelik | Değerlendirme | Dayanak |
|---|---|---|
| Uygunluğun tanınabilirliği | İyi | Menü 5 ana başlıkta; "Tüm ekranlar" rol bazlı; ekran üst-başlıkları ne işe yaradığını söylüyor |
| Öğrenilebilirlik | İyi | Kılavuz, videolar, "Açıklama" katlanırları, Basit mod |
| İşletilebilirlik | **Zayıf (telefon)** / İyi (masaüstü) | Telefonda satış ekranı (Y-1, Y-2); masaüstünde iki işlev tıklanamıyor (K-1) |
| Kullanıcı hatasından koruma | Orta | Pasif düğmede sebep, yasal sınır uyarısı, alan doğrulama iyi; yanlış çevrimdışı bandı ve yetki karmaşası olumsuz |
| Kullanıcı arayüzü estetiği / ilgi | Orta | Yıldız dili şık; tutarsız ikinci dil ve punto kalabalığı düşürüyor |
| Kapsayıcılık (erişilebilirlik) | Orta | Kontrast ve hareket iyi; 1.4.10, 1.4.11, 2.4.7, 2.4.11 açık |
| Kullanıcıya yardım / öz-tanımlılık | İyi | Hata metinleri açıklayıcı, boş durum mesajları var |

**Genel:** masaüstü kullanımı satışa yakın; **telefon ve baskı** kullanımı satışa hazır değil.

## 9. Kapsanan / kapsanamayan rotalar

**Kapsanan:** 56/56 rota şablonu.
- Rotalar: / · /islemler · /cariler · /kasalar · /ekranlar · /urunler · /urunler/agac · /urunler/etiketler · /emanet · /kur · /pos · /pos/tahsilatlar · /vezne · /gise · /banka · /gider · /hurda · /faturalar · /e-belgeler · /gider-pusulalari · /raporlar · /raporlar/kar-zarar · /raporlar/vade-takibi · /raporlar/islem-defteri · /raporlar/satis-analizi · /raporlar/kasa · /fiyatlandirma · /sayim · /satin-alma · /irsaliyeler · /kullanicilar · /yetkiler · /isletme · /ayarlar · /kilavuz · /uyum · /kvkk · /denetim-izi · /veri-disa-aktarim · /veri-ice-aktarim · /kurulum · /bildirimler · /evraklar · /evraklar/takvim · /siparisler · /yedekler · /vitrin · /vitrin/ekran · /yasal-sinirlar · /siparisler/{Id} · /gider-pusulalari/{Id}/belge · /cikti/{Tur}/{Id} (FATURA, SATIS_FISI; yazdırmada ayrıca IRSALIYE ve SAYIM_TUTANAGI) · /banka/ekstre/{IthalatId} · /fiyat/{Anahtar} · /giris · /sifre-sifirla.

**Matris:**
- Sahip: 55 rota örneği × {1280, 375} × {Basit, Profesyonel} × {açık, koyu} = 440 durum.
- Tezgâhtar: 55 × {1280, 375} × Basit × {açık, koyu} = 220 durum.
- Duyarlılık (Pro açık): 320, 414, 768, 1024, 1440, 1920 ve 812×375 → 385 durum; 320 Basit 55 durum.
- EN dili: 110 durum.

**Kısmi kapsananlar / sınırlar:**
- **/banka/ekstre/{IthalatId}:** listeleyen bir okuma ucu bulunamadı; ithalat yalnız dosya yüklemeyle (yazma) oluşuyor. Yalnız "bulunamadı" hata durumu ölçüldü.
- **/fiyat/{Anahtar}:** gerçek anahtar bir sır olduğu için kullanılmadı. Yalnız geçersiz anahtar hata ekranı ölçüldü.
- **Tezgâhtar modu:** tezgâhtarın modu "işletme varsayılanından devralınmış" durumda (`arayuzModu=null`). Düğmeyle değiştirmek kullanıcı satırına kalıcı değer yazar ve devralmaya geri dönüş arayüzde tek adımda doğrulanamadığı için **değiştirilmedi**. Tezgâhtar yalnız Basit modda ölçüldü.
- **Klavye turu:** Pro-koyu turu 4 rotada durduruldu (süre). Koyu temada odak halkası kontrastı Basit-koyu 55 rotalık turla ölçüldü: halka <3:1 = 0.
- **Ölçülmedi:**
  - Ekran okuyucuyla gerçek duyurma (VoiceOver/NVDA): araç yok.
  - `forced-colors` / Windows yüksek kontrast.
  - 1.4.13 fare-üstü içerik.
  - Gerçek iOS/Android cihaz: yalnız emülasyon.
  - Gerçek yazıcı çıktısı: yalnız PDF ve print medyası.
  - Bilinçli seçim sonrası görünüm: kural gereği seçim yapılmadı.
- **Görüntü adı çakışması:** tezgâhtar ve sahip 1280-açık açılır liste ve klavye görüntüleri aynı adla yazıldı; görüntüler sahibe aittir. Tezgâhtar ölçümleri ham kayıtlarda ayrıdır.

## 10. Mod / tema / dil geri alma kanıtı

Başlangıç değerleri (08:00 civarı, API `/auth/ben` + `/kullanici/gorunum`):
- **Sahip:** arayuzModu=BASIT, etkin=BASIT · temaModu=aydinlik (kaynak: kullanıcı) · şema yildiz · punto 18 (sistem) · localStorage `kuyumcu-tema`=aydinlik · `kuyumcu-dil`=yok (TR).
- **Tezgâhtar:** arayuzModu=null (devralınmış, etkin BASIT) · temaModu=sistem · localStorage boş.

| Kayıt (ham/tercih.jsonl) | Zaman (UTC) | Mod | Tema (kaynak) | Punto | Dil anahtarı |
|---|---|---|---|---|---|
| sahip-matris-sonrasi | 08:25:54 | PROFESYONEL (beklenen: matris Pro ile bitti) | aydinlik (kullanıcı) | 18 | yok |
| **sahip-geri-al** (BASIT düğmesi) | 09:10:33 | PROFESYONEL → **BASIT** (`ok`) | aydinlik (kullanıcı) | 18 | yok |
| sahip-son | 09:25:44 | BASIT | aydinlik (kullanıcı) | 18 | yok |
| sahip-en-son | 09:32:45 | BASIT | aydinlik (kullanıcı) | 18 | yok |
| sahip-dogrulama-sonrasi | 09:33:25 | BASIT | aydinlik (kullanıcı) | 18 | yok |
| **sahip-final** | 09:33:51 | **BASIT** | **aydinlik (kullanıcı)** | **18** | **yok** |

- **Tema:** koyu/açık geçişi yalnız denetim tarayıcısının `kuyumcuTema.uygula()` önbelleğiyle yapıldı. Sunucudaki tema tercihine **hiç yazılmadı** (`/kullanici/gorunum` baştan sona aydinlik/kullanıcı).
- **Dil:** EN yalnız denetim tarayıcısının localStorage'ına yazıldı (DilServisi sunucuya yazmıyor). Bitişte anahtar silindi; kanıt: `ham/dil-sahip.jsonl` → `geri-alindi` {ls: null, h1: "Panel", lang: "tr", menü "Bugün…"}, görüntü `foto/dil/geri-alindi-TR.png`.
- **Tezgâhtar:** hiçbir tercihi değiştirilmedi.
- **Ekran görüntüsü:** `foto/geri-alma-sahip-geri-al.png`.

**Kapanış:** 5182 ve 5183 vekilleri ile tüm denetim Chrome süreçleri kapatıldı. 5179 (kurucunun tarayıcısı) ve 5199 (launchd servisi) çalışır durumda bırakıldı (curl 200). Tezgâhtar parolasını içeren geçici fikstür boşaltıldı.

## 11. Ek tablolar (otomatik üretildi)

### Matris özeti (sahip; rota sayısı = sayaç > 0 olan rota)
| Mod | Tema | Genişlik | Rota | yatay | K3_tasan | K3_kirik | K3c_kelimeKirik | K9_binen | K9b_etiketBin | K10_elipsis | K10b_kesik | K10c_kesikDeger | K12_ezik | K13_kirp | K15_etkilesimGorunurluk | K11_hataBandi | K5_TRY | kontrast | 1.4.11 | konsol | 4xx/5xx | CLS>0.1 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BASIT | aydinlik | 375 | 55 | 0 | 0 | 2 | 0 | 2 | 0 | 0 | 0 | 1 | 3 | 0 | 0 | 0 | 0 | 0 | 12 | 3 | 2 | 1 |
| BASIT | aydinlik | 1280 | 55 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 | 0 | 0 | 12 | 4 | 2 | 0 |
| BASIT | karanlik | 375 | 55 | 0 | 0 | 2 | 0 | 2 | 0 | 0 | 0 | 1 | 3 | 0 | 0 | 0 | 0 | 0 | 12 | 3 | 2 | 3 |
| BASIT | karanlik | 1280 | 55 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 | 0 | 0 | 12 | 4 | 2 | 1 |
| PRO | aydinlik | 375 | 55 | 0 | 0 | 2 | 0 | 2 | 0 | 0 | 0 | 1 | 3 | 0 | 0 | 0 | 0 | 0 | 12 | 4 | 3 | 6 |
| PRO | aydinlik | 1280 | 55 | 0 | 0 | 0 | 1 | 1 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 | 0 | 0 | 12 | 4 | 3 | 0 |
| PRO | karanlik | 375 | 55 | 0 | 0 | 2 | 0 | 2 | 0 | 0 | 0 | 1 | 3 | 0 | 0 | 0 | 0 | 0 | 12 | 3 | 2 | 3 |
| PRO | karanlik | 1280 | 55 | 0 | 0 | 0 | 1 | 1 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 | 0 | 0 | 12 | 4 | 2 | 0 |


### Duyarlılık (PRO açık; 320 ayrıca BASIT)
| Genişlik×Boy | Rota | yatay kaydırma olan | K13 kırpılan | K3 taşan | K9 binen | sabit öğe alanı >%25 |
|---|---|---|---|---|---|---|
| BASIT 320 | 55 | 53 (/, /islemler, /cariler, /kasalar, /ekranlar) | 0 | 0 | 2 | 0 |
| PRO 320 | 55 | 53 (/, /islemler, /cariler, /kasalar, /ekranlar) | 0 | 0 | 2 | 0 |
| PRO 414 | 55 | 0  | 0 | 0 | 2 | 0 |
| PRO 768 | 55 | 0  | 0 | 0 | 1 | 0 |
| PRO 812 | 55 | 0  | 0 | 0 | 2 | 0 |
| PRO 1024 | 55 | 0  | 0 | 4 | 3 | 0 |
| PRO 1440 | 55 | 0  | 0 | 0 | 1 | 0 |
| PRO 1920 | 55 | 0  | 0 | 0 | 1 | 0 |


### Klavye Tab turu
| Dosya | Rota | Odaklanan | Görünmez odak | Örtülü odak | Ekran dışı | Halka <3:1 | Geri sıçrama | İlk durak |
|---|---|---|---|---|---|---|---|---|
| sahip-KORU-aydinlik-1280 | /islemler | 68 | 3 ['#islem-barkod ""'] | 0 [] | 0 | 0 | 0 | #yildiz-odeme-0 "Nakit · A |
| sahip-KORU-aydinlik-1280 | /cariler | 68 | 4 ['#cari-arama ""'] | 0 [] | 0 | 0 | 3 | #cari-arama "" |
| sahip-KORU-aydinlik-1280 | /ekranlar | 69 | 2 ['#ekranlar-ara ""'] | 0 [] | 0 | 0 | 3 | #ekranlar-ara "" |
| sahip-KORU-aydinlik-1280 | /urunler | 69 | 3 ['#urun-arama ""'] | 4 ['#urun-kurallar-ac "Fiyat kuralları" ⟂ #yildiz-urun-ayrinti "Tüm ayrıntılar"'] | 0 | 0 | 4 | #urun-arama "" |
| sahip-KORU-aydinlik-1280 | /emanet | 68 | 4 ['#yildiz-emanet-arama ""'] | 0 [] | 0 | 0 | 0 | #yildiz-emanet-arama "" |
| sahip-KORU-aydinlik-1280 | /kur | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #yildiz-kur-b1 "Şimdi günc |
| sahip-KORU-aydinlik-1280 | /banka | 68 | 0 [] | 3 ['#yildiz-banka-havale "Havale gönder (demo)" ⟂ #yildiz-banka-ayrinti "Tüm ayrıntılar"'] | 0 | 0 | 0 | #yildiz-banka-b1 "Hesap ek |
| sahip-KORU-aydinlik-1280 | /gider | 68 | 20 ['#gd-kasa "—"', '#gd-kategori "—"'] | 0 [] | 0 | 0 | 4 | #yildiz-gider-secim-gider  |
| sahip-KORU-aydinlik-1280 | /gider-pusulalari | 67 | 6 ['#yildiz-pusula-ara ""'] | 0 [] | 0 | 0 | 0 | #yildiz-pusula-ara "" |
| sahip-KORU-aydinlik-1280 | /raporlar/kasa | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #yildiz-kasarapor-b1 "Günü |
| sahip-KORU-aydinlik-1280 | /isletme | 67 | 0 [] | 5 ['#belge-sec "" ⟂ #belge-sec-dugme "DOSYA SEÇ"'] | 0 | 0 | 4 | #ust-yardim "YARDIM" |
| sahip-KORU-aydinlik-1280 | /ayarlar | 69 | 0 [] | 0 [] | 0 | 0 | 9 | #ayar-sekme-gorunum "Görün |
| sahip-KORU-aydinlik-1280 | /kilavuz | 70 | 54 ['#kl-video-oynatici-satis ""'] | 0 [] | 0 | 0 | 0 | #ust-yardim "YARDIM" |
| sahip-KORU-aydinlik-1280 | /kvkk | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #ust-kur-saat "11:36" |
| sahip-KORU-aydinlik-1280 | /evraklar | 68 | 5 ['#yildiz-evrak-ara ""'] | 0 [] | 0 | 0 | 0 | #yildiz-evrak-ara "" |
| sahip-KORU-aydinlik-1280 | /evraklar/takvim | 66 | 0 [] | 0 [] | 0 | 0 | 8 | #yildiz-vade-ayrinti "Tüm  |
| sahip-KORU-aydinlik-1280 | /siparisler | 68 | 4 ['#yildiz-siparis-arama ""'] | 0 [] | 0 | 0 | 0 | #yildiz-marka "MİHENK" |
| sahip-KORU-aydinlik-1280 | /yedekler | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #ust-yardim "YARDIM" |
| sahip-KORU-aydinlik-1280 | /siparisler/01M4Z6HCVBFGCF65XM | 69 | 0 [] | 3 ['#belge-sec "" ⟂ #belge-sec-dugme "DOSYA SEÇ"'] | 0 | 0 | 6 | #ust-kur-saat "11:39" |
| sahip-KORU-karanlik-1280 | /islemler | 68 | 3 ['#islem-barkod ""'] | 0 [] | 0 | 0 | 0 | #yildiz-odeme-0 "Nakit · A |
| sahip-KORU-karanlik-1280 | /cariler | 68 | 4 ['#cari-arama ""'] | 0 [] | 0 | 0 | 3 | #cari-arama "" |
| sahip-KORU-karanlik-1280 | /ekranlar | 69 | 2 ['#ekranlar-ara ""'] | 0 [] | 0 | 0 | 3 | #ekranlar-ara "" |
| sahip-basit-KORU-karanlik-1280 | /islemler | 68 | 3 ['#islem-barkod ""'] | 0 [] | 0 | 0 | 0 | #yildiz-odeme-0 "Nakit · A |
| sahip-basit-KORU-karanlik-1280 | /cariler | 68 | 4 ['#cari-arama ""'] | 0 [] | 0 | 0 | 3 | #cari-arama "" |
| sahip-basit-KORU-karanlik-1280 | /ekranlar | 69 | 2 ['#ekranlar-ara ""'] | 0 [] | 0 | 0 | 3 | #ekranlar-ara "" |
| sahip-basit-KORU-karanlik-1280 | /urunler | 69 | 3 ['#urun-arama ""'] | 4 ['#urun-kurallar-ac "Fiyat kuralları" ⟂ #yildiz-urun-ayrinti "Tüm ayrıntılar"'] | 0 | 0 | 4 | #urun-arama "" |
| sahip-basit-KORU-karanlik-1280 | /emanet | 68 | 4 ['#yildiz-emanet-arama ""'] | 0 [] | 0 | 0 | 0 | #yildiz-emanet-arama "" |
| sahip-basit-KORU-karanlik-1280 | /kur | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #yildiz-kur-b1 "Şimdi günc |
| sahip-basit-KORU-karanlik-1280 | /banka | 68 | 0 [] | 3 ['#yildiz-banka-havale "Havale gönder (demo)" ⟂ #yildiz-banka-ayrinti "Tüm ayrıntılar"'] | 0 | 0 | 0 | #yildiz-banka-b1 "Hesap ek |
| sahip-basit-KORU-karanlik-1280 | /gider | 68 | 20 ['#gd-kasa "—"', '#gd-kategori "—"'] | 0 [] | 0 | 0 | 4 | #yildiz-gider-secim-gider  |
| sahip-basit-KORU-karanlik-1280 | /gider-pusulalari | 67 | 6 ['#yildiz-pusula-ara ""'] | 0 [] | 0 | 0 | 0 | #yildiz-pusula-ara "" |
| sahip-basit-KORU-karanlik-1280 | /raporlar/kasa | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #yildiz-kasarapor-b1 "Günü |
| sahip-basit-KORU-karanlik-1280 | /isletme | 68 | 0 [] | 5 ['#belge-sec "" ⟂ #belge-sec-dugme "DOSYA SEÇ"'] | 0 | 0 | 5 | #ust-yardim "YARDIM" |
| sahip-basit-KORU-karanlik-1280 | /ayarlar | 68 | 0 [] | 0 [] | 0 | 0 | 9 | #ayar-sekme-gorunum "Görün |
| sahip-basit-KORU-karanlik-1280 | /kilavuz | 70 | 54 ['#kl-video-oynatici-satis ""'] | 0 [] | 0 | 0 | 0 | #ust-yardim "YARDIM" |
| sahip-basit-KORU-karanlik-1280 | /kvkk | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #ust-kur-saat "12:20" |
| sahip-basit-KORU-karanlik-1280 | /evraklar | 68 | 5 ['#yildiz-evrak-ara ""'] | 0 [] | 0 | 0 | 0 | #yildiz-evrak-ara "" |
| sahip-basit-KORU-karanlik-1280 | /evraklar/takvim | 66 | 0 [] | 0 [] | 0 | 0 | 8 | #yildiz-vade-ayrinti "Tüm  |
| sahip-basit-KORU-karanlik-1280 | /siparisler | 68 | 4 ['#yildiz-siparis-arama ""'] | 0 [] | 0 | 0 | 0 | #yildiz-marka "MİHENK" |
| sahip-basit-KORU-karanlik-1280 | /yedekler | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #ust-yardim "YARDIM" |
| sahip-basit-KORU-karanlik-1280 | /siparisler/01M4Z6HCVBFGCF65XM | 69 | 0 [] | 3 ['#belge-sec "" ⟂ #belge-sec-dugme "DOSYA SEÇ"'] | 0 | 0 | 6 | #ust-kur-saat "12:22" |
| tezgahtar-KORU-aydinlik-1280 | / | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #yildiz-marka "MİHENK" |
| tezgahtar-KORU-aydinlik-1280 | /islemler | 68 | 3 ['#islem-barkod ""'] | 0 [] | 0 | 0 | 0 | #yildiz-odeme-0 "Nakit · A |
| tezgahtar-KORU-aydinlik-1280 | /cariler | 69 | 4 ['#cari-arama ""'] | 0 [] | 0 | 0 | 3 | #cari-arama "" |
| tezgahtar-KORU-aydinlik-1280 | /ekranlar | 69 | 3 ['#ekranlar-ara ""'] | 0 [] | 0 | 0 | 2 | #ekranlar-ara "" |
| tezgahtar-KORU-aydinlik-1280 | /urunler | 69 | 3 ['#urun-arama ""'] | 4 ['#urun-kurallar-ac "Fiyat kuralları" ⟂ #yildiz-urun-ayrinti "Tüm ayrıntılar"'] | 0 | 0 | 4 | #urun-arama "" |
| tezgahtar-KORU-aydinlik-1280 | /emanet | 68 | 4 ['#yildiz-emanet-arama ""'] | 0 [] | 0 | 0 | 0 | #yildiz-emanet-arama "" |
| tezgahtar-KORU-aydinlik-1280 | /kur | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #yildiz-kur-b1 "Şimdi günc |
| tezgahtar-KORU-aydinlik-1280 | /gise | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #ust-yardim "Yardım" |
| tezgahtar-KORU-aydinlik-1280 | /banka | 68 | 0 [] | 5 ['#yildiz-banka-havale "Havale gönder (demo)" ⟂ #yildiz-banka-ayrinti "Tüm ayrıntılar"'] | 0 | 0 | 0 | #yildiz-marka "MİHENK" |
| tezgahtar-KORU-aydinlik-1280 | /gider | 66 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "Yardım" |
| tezgahtar-KORU-aydinlik-1280 | /hurda | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "Yardım" |
| tezgahtar-KORU-aydinlik-1280 | /gider-pusulalari | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "Yardım" |
| tezgahtar-KORU-aydinlik-1280 | /raporlar | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /raporlar/satis-analizi | 67 | 0 [] | 5 ['#sa-panel-odeme "" ⟂ #sa-duzeltme-ac "Rapordaki bir rakam yanlışsa"'] | 0 | 0 | 5 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /raporlar/kasa | 66 | 0 [] | 0 [] | 0 | 0 | 8 | #yildiz-kasarapor-ayrinti  |
| tezgahtar-KORU-aydinlik-1280 | /satin-alma | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /irsaliyeler | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /kullanicilar | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /yetkiler | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /isletme | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /ayarlar | 69 | 0 [] | 0 [] | 0 | 0 | 9 | #ayar-sekme-gorunum "Görün |
| tezgahtar-KORU-aydinlik-1280 | /kilavuz | 70 | 54 ['#kl-video-oynatici-satis ""'] | 0 [] | 0 | 0 | 0 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /uyum | 66 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /kvkk | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "Yardım" |
| tezgahtar-KORU-aydinlik-1280 | /denetim-izi | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /veri-disa-aktarim | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /veri-ice-aktarim | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /kurulum | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /bildirimler | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #yildiz-bildirim-sekme-tum |
| tezgahtar-KORU-aydinlik-1280 | /evraklar | 67 | 5 ['#yildiz-evrak-ara ""'] | 0 [] | 0 | 0 | 0 | #yildiz-evrak-ara "" |
| tezgahtar-KORU-aydinlik-1280 | /evraklar/takvim | 66 | 0 [] | 0 [] | 0 | 0 | 8 | #yildiz-vade-ayrinti "Tüm  |
| tezgahtar-KORU-aydinlik-1280 | /siparisler | 68 | 4 ['#yildiz-siparis-arama ""'] | 0 [] | 0 | 0 | 0 | #yildiz-marka "MİHENK" |
| tezgahtar-KORU-aydinlik-1280 | /yedekler | 67 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /yasal-sinirlar | 66 | 0 [] | 0 [] | 0 | 0 | 7 | #ust-yardim "YARDIM" |
| tezgahtar-KORU-aydinlik-1280 | /siparisler/01M4Z6HCVBFGCF65XM | 69 | 0 [] | 3 ['#belge-sec "" ⟂ #belge-sec-dugme "DOSYA SEÇ"'] | 0 | 0 | 6 | #ust-yardim "Yardım" |
| tezgahtar-KORU-aydinlik-1280 | /gider-pusulalari/01M4Z6HCXJ29 | 67 | 0 [] | 0 [] | 0 | 0 | 6 | #ust-yardim "Yardım" |


## 12. Yöntem notları
- Her sayfa-durumunda: sikisma-olc K1–K15 + ekstra K12/K13/K10b/K9b + denetim ölçeri (kontrast, 1.4.11, 2.5.8, ARIA, başlıklar, tipografi, düzen, yerelleştirme, hareket) + CLS (PerformanceObserver) + konsol ve 4xx/5xx (CDP) + tam sayfa görüntü.
- **Zorunlu görüntüler:** her rota için 1280-Pro-açık (PNG) ve 375-Basit-koyu (PNG). Diğer durumlar JPEG.
- **Açılır listeler:** DOM `click()` ile açıldı. Konum, örtülme (5 noktada `elementFromPoint`), taşma, kaydırma, en uzun seçenek satır sayısı, kesik seçenek, arama ve boş mesaj (eşleşmeyen sorgu "zzqxw"), `aria-expanded` ve değer korunması ölçüldü. Kapatma dış tıklama/Escape ile yapıldı; seçim yapılmadı.
- **Klavye:** yalnız Tab (CDP `Input.dispatchKeyEvent`), 70 adım. Odak görünürlüğü, odaklı ve odaksız stil farkıyla ölçüldü. Enter/Boşluk hiç basılmadı.
- **2.5.8:** WCAG boşluk istisnasıyla hesaplandı (24 px çaplı dairelerin kesişmesi). Satır içi bağlantı istisnası uygulandı.
- **Uzun içerik:** yalnız denetim tarayıcısının DOM'unda metin geçici değiştirildi, sayfa yenilenerek geri alındı; sunucuya veri gitmedi.
- **Bilinen ölçer yanlış pozitifleri** § 1 sonunda listelendi; rapora yalnız görüntüyle doğrulanan bulgular alındı.
