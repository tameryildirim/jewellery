# Görsel denetim 2 — 23.10.2026 (21.10 denetiminin düzeltmeler sonrası yeniden koşumu)

Kurucu: "ekran görüntülerine bakarak yazı boyutları, dropdownlar vs ne varsa test et tek tek" + "açılan her şeyin gölgesi olsa daha rahat seçer, araştır, uygula".
Koşum: `denetim-2.mjs` (başsız Chrome + CDP, yalıtılmış `kuyumcu_egitim` şeması, 5192; vekil 5181 sahip / 5183 tezgâhtar). Hiçbir kayıt yazılmadı; arayüz modu sunucuda değiştirilip başlangıca döndürüldü (`ham/tercih.jsonl`: BASIT → BASIT), tema ve dil yalnız tarayıcı önbelleğinde.

## 1. Kapsam

| Geçiş | Durum sayısı | Ne ölçüldü |
|---|---|---|
| Sahip matris | 52 rota × 5 durum = 260 (1280 Pro açık · 375 Basit koyu · 1024 Pro açık · 320 Basit açık · 812×375 Pro koyu) | sikisma-olc K1–K13 (taşma/kırık/binen/elipsis/jargon), yatay kaydırma, CLS (tam yükleme), tipografi (boy sayısı, ölçek dışı), kontrast <4,5:1, çift id, 24 px altı dokunma hedefi, ağ 4xx/5xx + JS hata, tam sayfa görüntü |
| Açılır listeler | 91 seçici (1280 Pro açık + 375 Basit koyu; çekmece/pencere içindekiler "+ / Yeni / Düzenle" ile açıldı) | açıldı mı, kutu, taşma (4 yön), 5 noktada örtülme, seçenek sayısı, satır boyu, en uzun satır, kesik, kaydırma, aria-expanded açık→kapalı, kapandı, değer korundu, seçili değer px / girdi px / h1 px ("başlık gibi mi") |
| Klavye | 52 rota × 30 Tab (gerçek `Input.dispatchKeyEvent`) | ilk durak, odak görünürlüğü (odaklıyken ↔ odak gidince, öğe + 5 ata) |
| EN dili | 52 rota (1280) | lang, h1, taşma, kalan Türkçe |
| Tezgâhtar | 52 × 2 (1280 açık · 375 koyu) | matris ölçümleri + 403 istekleri |
| Görüntü incelemesi | 48 temas sayfası (foto/sayfa) + 375/1024 görüş alanı kesitleri | gözle tek tek |

Ham: `ham/*.jsonl`, özet tablolar `ham/ozet.md` (ozet.py), görüntüler `foto/matris`, `foto/acilir`, `foto/klavye`, `foto/sayfa`, kanıt `foto/kanit`.

## 2. 21.10 bulgularının durumu

46 bulgunun 45'i kapalı doğrulandı (K-1, Y-1..Y-8, O-1..O-22, D-1..D-15; O-6/O-7/D-7/O-20 kurucu kararıyla tuvalden sapılarak). Yeniden koşumda hiçbirinin izi çıkmadı:
yatay kaydırma 0/260 durum (320 dahil) · K1 gizli 0 · K10 elipsis 0 · K12 0 · çift id 0 · JS hatası 0 · sahip ağ hatası 0 · seçili değer 91/91'de 22 px/400 (girdiyle eşit, "başlık gibi" 0) · açılır liste 91/91 açıldı, taşma 0 (bir yanlış pozitif, § 4) · kesik seçenek 0 · değer korundu 91/91 · aria-expanded 91/91 · tipografi 30 ekranda 7 basamak (22 → 7).
Açık kalan: O-17 (rem; kurucu kararı bekler), P-30 (insan ölçümü).

## 3. Yeni bulgular (bu koşumda) ve yapılanlar

| No | Önem | Ekran / durum | Ne görüldü (ölçülen) | Yapılan | Kanıt |
|---|---|---|---|---|---|
| G2-1 | YÜKSEK | /islemler 1280×800 ve 1024×768 (dizüstü) | Yapışkan Fark/Kaydet bloğu (303 px) "+ İndirim" düğmesini örtüyordu (1024×768: 7 px binme, K9; 1280×800 görüntüde düğme kırpık) | Kısa ekran kuralı 767 → 840 px (aralık 16 → blok 267 px); test eşiği güncellendi | alt üst 449 > İndirim alt 396 (1024×768); 481 > 396 (1280×800) |
| G2-2 | YÜKSEK | Tezgâhtar, 15 ekran | Her ekranda `/bildirimler/sayi` 403 (üst bar rozeti); panelde kasa-defteri/cari-yaşlandırma/vade/günlük/aylık/işlem-defteri/kâr-zarar/pozisyon, /kur'da geçmiş, /banka'da karar-bekleyen, /vitrin'de pano/ayarlar, /faturalar'da özel matrah → 24 istek 403. Kök neden: profil (/auth/ben) gelmeden sayfalar istek atıyor; "rol bilinmiyor → at" kuralı 403'ü üretiyordu | `RolKapisi` (profil en çok 3 sn beklenir; oturumsuz çizimde bekleme yok) + 9 çağrı noktası; YetkiKapisi modül yetkisi yüklenmeden sayfayı çizmez | tezgâhtar 15 ekranda 403: **0** |
| G2-3 | ORTA | /gider 375/320/812 | Tam yüklemede CLS 0,22 / 1,1 / 1,43: spinner → form, Kasa/Personel satırı ve kilit satırı sonradan geliyordu | Form yüklenirken çizilir (Kasa/Personel devre dışı), kilit satırı sabit yerde | /gider 375 CLS **0** · 1280 0,048 |
| G2-4 | ORTA | /gider klavye | Seçici satırları (Kasa · Kategori · Personel) O-3/O-4 dışında kalmıştı: çizgi 1,27:1, Tab'da görünür değişiklik yok | `.secici-alan` çizgi --kenarlik + odakta koyu çizgi ve gölge | gerçek Tab: gd-kasa/kategori/personel görünür |
| G2-5 | ORTA | /hurda, /kasalar, /raporlar/kasa 1024 | "Hurda alış · 15 Eki" 3 satır (ad sütunu 66 px), "Nakit" 36 px sütundan taşıyor, "+ Tüm ayrıntılar" iki satır | 1001–1360 ızgaraları: ad sütunu genişledi, eylem satırı sarar, ekle tek satır | hurda ad 2 satır, ekle 23 px; kasalar taşma 0 |
| G2-6 | DÜŞÜK | /kurulum, sayfa dibindeki seçici | Yıldız kabuğunda kaydıran kap `.ana-icerik`; sayfa dibindeki liste görüş alanının altına açılıyordu (ölçüm kapalı akordeon içindeki seçiciye programla tıklanınca; kullanıcı yolu § 4) | secici-yerlesim.js: panel de en yakın konuma kaydırılır | akordeon açılıp seçilince liste 476–671 / 900 |
| G2-7 | DÜŞÜK | Sipariş düzenle (cari, usta), /gider Kasa 375, /veri-ice-aktarim Kaynak 375 | Seçenek metni 3 satıra sarıyor (kesilmiyor) | Değişiklik yok: uzun seçenek kesilmek yerine sarar (O-13 kararı) | — |
| G2-8 | DÜŞÜK | 24 px altı hedefler | Üst şerit "HAS 6.467 · 20:42" (148×17), kullanıcı adı (73×20), "YARDIM" (62×15), "+ Tüm ayrıntılar" (137×23) — metin bağlantıları, 2.5.8 aralık istisnası (komşusuz) | Değişiklik yok (ölçek 12/14 px metin bağlantısı; tuval) | — |
| G2-9 | DÜŞÜK | /ekranlar nokta ayracı "·" 1,27:1 | Dekoratif ayraç (kontrast ölçeri metin sandı) | Değişiklik yok | — |
| G2-10 | DÜŞÜK | /banka tam yükleme CLS 0,22–0,44 | Yükleniyor kutusu (192 px) → liste geçişi | Değişiklik yok (yükleme → içerik; iskelet yok) | — |

## 4. Yanlış pozitifler (rapora bulgu olarak girmedi)
- /kurulum `kur-acilis-tur` "liste 339 px taşıyor": seçici KAPALI "Açılış bakiyesi" akordeonunun içinde; denetim programla tıkladı. Kullanıcı yolu (akordeon aç → seç) ölçüldü: liste görüş alanında. (G2-6 yine de panel kaydırmasını ekledi.)
- /uyum `uy-supheli-durum` "5 noktada örtülü": denetim "+ Yeni bildirim" penceresini açtıktan sonra sayfadaki (pencere dışı) seçiciyi ölçtü; örten şey pencerenin kendi perdesi — doğru davranış.
- /kilavuz "video kontrast 1:1" ve "Hurda alışı ⟂ ·": `<video>` öğesi ve bölüm listesinde kalın ad + soluk açıklama (aynı satır, iç içe değil).
- /vitrin/ekran ve belge sayfalarında "görünür h1 0": kiosk ekranı ve kâğıt belge (h1 belgenin kendisi).
- Klavye turunda ilk durak "ust-yardim" (24 rota): eski kabuk sayfalarında rota değişince başlık odaklanır (ekran okuyucu için), ilk Tab Yardım'a gider; "İçeriğe geç" DOM'da ilk sıradadır ve Yıldız yüzlerinde ilk duraktır (21 rota).

## 5. Gölge / derinlik katmanı (kurucu 23.10)
Araştırma: NN/g göz izleme (71 katılımcı, 9 sayfa çifti) — zayıf işaretli öğelerde %22 daha uzun süre, %25 daha çok sabitleme; Material 3 menü kabı 3 dp yükselti (düzey 2) + kaldırılmış yüzey rengi; WCAG 1.4.11 — bileşeni ayıran tek ipucu kenarsa 3:1. Bizde açık listenin yüzeyi zeminle AYNI, kenarı 1,27:1, gölge yok; seçenek satırında fare üstü zemini yok.
Uygulanan (tema-yildiz.css "AÇILIR YÜZEYLERDE DERİNLİK", tuvalden bilerek sapma): açılır liste / barkod sonucu / kullanıcı menüsü / elle giriş: kaldırılmış yüzey (açıkta zeminden açık, koyuda --yuzey-3), 3,26:1 kenar, yumuşak gölge; seçenek satırı fare üstü ve klavye gezintisinde zemin; çekmece ve pencere: perde (açık %32, koyu %60) + geniş gölge. Belirteçler `--yuzey-acilir`, `--golge-acilir`, `--golge-katman`, `--perde` (ham hex yok, renk yalnız belirteçle).
Kanıt: `foto/kanit/golge-liste-aydinlik.png`, `golge-liste-karanlik.png`, `golge-pencere.png`; ölçüm: liste gölge var, yüzey ≠ zemin, kenar 3,26:1 (açık) / koyuda --metin-soluk; fare üstü zemin var; perde rgba(20,20,20,.32), pencere gölgesi var.

## 6. Kapılar
- Tam süit: 12.820/12.823 (3 atlanan), 0 başarısız · denetim yalnız P-30.
- Tarayıcı kanıtı `dogrula-3.mjs`: 12/12.
- Geri alma: sahip modu BASIT → BASIT (`ham/tercih.jsonl`), dil anahtarı silindi, tezgâhtar tercihleri değişmedi, 5181/5183 vekil Chrome'ları kapatıldı.
