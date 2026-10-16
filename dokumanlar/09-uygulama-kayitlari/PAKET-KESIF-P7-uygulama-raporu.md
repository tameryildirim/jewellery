# PAKET KESIF-P7 — BİLEŞEN-GÖRÜNÜM — uygulama raporu (30.09.2026, şerit t1)

Rol: Taşkın (backend) + Ertuğ (frontend). Kaynak: `ham/KESIF-TRIYAJ-2026-09-30.md` § KESIF-P7 + § Ortak kurallar.
Çalışma dizini: `/private/tmp/kuyumcu-serit/KESIF-P7/` (ölçüm `olc/`, mutasyon `mut.py` + `mut-M7-*.log`, önce kaynak `once-kaynak/`).
COMMIT YOK. Sayfa `.razor` dosyalarına, `app.css`'e, `olcek.css`'e (cmp: değişmedi) ve `Sozlukler*.cs`'e dokunulmadı.

Not: Paket 16:10–19:32 arasında bir oturumda uygulandı. O oturum rapor yazılmadan kapandı. Bu oturum son durumu yeniden doğruladı: build, filtreli test, denetim.sh, /pos satır içi ölçümü (önceki ölçümde tohumda taksit satırı yoktu, bu yüzden ölçülememişti) ve test kiracısı temizliği. Sonra bu raporu yazdı.

## 0. TUR 2 (30.09, 20:47–22:20) — doğrulayıcı + tasarım hakemi bulguları

Tur 2 kodu 20:47–21:11 arasında yazıldı (o oturum rapor yazmadan kapandı). Bu oturum kodu değiştirmedi. Şunları yeniden koştu ve kanıtladı: build, filtreli test, mutasyon (cmp ile), denetim, gerçek tarayıcı ölçümü ve kiracı temizliği. Aşağıdaki sayılar bu oturumun çıktılarıdır.

### 0.1 Kapanan bulgular

| Bulgu | Kök neden (ölçüldü) | Çözüm | Tarayıcı kanıtı |
|---|---|---|---|
| **[KRİTİK] DUGME-2 koyu tema: seçili segment ana eylem mavisiyle çerçeveli (hap içinde hap)** | `dugme.css` karanlık bloğu `--secili-dolgu: --vurgu-yuzey`, `--secili-dolgu-kenar: --vurgu` kullanıyordu. Kenar rgb(145,185,255) idi, bu da `.btn-birincil` zeminiyle aynı renk | Karanlık için ayrı eşleme **kaldırıldı**. İki temada tek aile var: `--secili-dolgu = --secili-dolgu-kenar = --kenar-yuzey` (fildişi şemada `--menu-aktif-yuzey`), yazı `--kenar-metin-guclu`. Seçili = dolu, çerçevesiz. Bu, üst çubuktaki `#ust-mod-basit` ile aynı yüzey | Koyu, 4 genişlik × BASİT/PRO: `#cari-segment` seçili bg **rgb(9,13,22)** = kenar rgb(9,13,22) = `#ust-mod-basit` bg rgb(9,13,22). Birincil düğme rgb(145,185,255): **eşit değil**. `/gider` sekmesi aynı. Kap kenarı rgb(113,117,124) / kap zemini rgb(26,30,38). Açık tema değişmedi: rgb(20,33,61) = kenar = üst anahtar |
| [DÜŞÜK] Satır içi kutu açılınca tablo genişliyor, “Aktif” rozeti yapışık eylem sütununun altına giriyor (K9) | Tablo hücresinde girdi kendi boyunu (size=20 → 192 px) koruyordu. Tablo kabı 1021 > 962 oldu | `ortak.css`: `td .satir-ici.si-acik > .si-kutu > .si-girdi { inline-size: 8.25rem; padding-inline: var(--bosluk-2) }`. Denenen değerler: 8rem telefonun ilk hanesini kesti; 9,5rem kabı 981 > 962 taşırdı | /cariler PRO 1280 telefon açık, önce/sonra: kap **1021/962 → 962/962**, **K9 3 → 0**, girdi scrollWidth = clientWidth 130 (değer tam). 1440: 1122 = 1122. 960 ve 375'te tablo kendi kabında kayıyor (tasarım gereği), K9 0 |
| [DÜŞÜK] SayfaBasi açıklaması `display:none`, ekran okuyucudan da düşüyor | `display:none` erişilebilirlik ağacından çıkarır | `ortak.css`: görsel gizleme kalıbı: `position:absolute; 1×1 px; clip: rect(0 0 0 0); overflow: clip`. `overflow: hidden` O7 kuralıyla yasak (tam süit 2'de O7 kırmızıydı, `clip` ile düzeldi). Yer kaplamaz, çizilmez, erişilebilirlik ağacında kalır. PRO'da görünür yapılması ekip oyuna bırakıldı (K17 kataloğu “açıklama satırı 0” der) | /siparisler 16 bileşim: `#sayfa-basi-…-aciklama` 1×1 px, display block, metin DOM'da (“Müşteri siparişi, dış atölye siparişi ve…”). Sayfa yatay kaydırması 0 |
| [DÜŞÜK] Kiracı sayısı ölçütü | Paralel şeritler kiracı açıp siliyor, toplam sayı ölçüt olamaz | Ölçüt: **`PAKETKESIF-P7%` öneki = 0** | § 0.5 |

### 0.2 Testler
- `KesifP7Testleri.cs`: **16 test** (14'e 2 yeni eklendi):
  - `Koyu_temada_secili_kenar_birincil_dugme_zemini_degil_ve_cercevesiz`: her KARANLIK tema × şema için seçili kenar ≠ `--vurgu`, dolgu ≠ `--vurgu`, kenar = dolgu, dolgu = üst çubuk anahtarı yüzeyi. Ayrıca dugme.css'te karanlık/prefers-color-scheme bloğunda `--secili-dolgu*` için kenar-yüzey dışı bir eşleme olmadığını da denetler.
  - `Tablo_hucresinde_acik_satir_ici_girdi_sutunu_genisletmez`.
  - `Sayfa_basi_aciklama_satiri_cizilmez_metin_DOMda_kalir` görsel gizlemeye göre yeniden yazıldı.
- Build (`build-t2son.log`): **0 Uyarı · 0 Hata**. Son mutasyondan sonraki temiz build (`build-t2mutsonra.log`): **0 · 0**.
- Filtreli koşum (`t-t2son.log`), filtre `KesifP7Testleri|DugmeIkiTestleri|KatmanTekKalipTestleri|QA10KRITIKSinirTestleri`: **67/67 başarılı**.
- Tam süit, tur 2'nin tek koşumu (son kodla, 21:11–21:14, `tam-suit3.log`, `trx-pKESIF-P7/tur2.trx`): **Toplam 8127 · Geçti 8125 · Başarısız 1 · Atlandı 1**. Tek kırmızı `QA10KRITIKSinirTestleri.DQA10_qa_senaryosu_125_50_storno_toplam_0_bakiye_5508_51` (`Assert.Contains "0,00"`, gerçek “0 ₺”):
  - P7'ye ait değil. Test dosyası (`Dogrulayici/QA10KRITIKSinirTestleri.cs`) başka bir şerit tarafından koşum sırasında, 21:12:13'te değiştirildi. Beklenti “0 ₺” oldu. `Bicim.cs`'e P7 dokunmadı.
  - Bu oturumun filtreli koşumunda aynı test **yeşil**.
  - Tam süit 2'deki O7 kırmızısı P7'nindi (`overflow: hidden`). Tam süit 3'te `clip` ile kapandı.
  - **P7'nin yeni kırmızısı: 0.**

### 0.3 Mutasyon (tur 2, `mut2.sh`, günlükler `mut2-M7-*.log`, yedek `mut-yedek/KESIF-P7t2b/`)

| Kod | Dosya | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|---|
| M7-29 | dugme.css | `--secili-dolgu-kenar` → `var(--vurgu)` (hap içinde hap geri) | Koyu_temada_secili_kenar_birincil_dugme_zemini_degil_ve_cercevesiz | AYNI |
| M7-30 | dugme.css | `html[data-mod][data-tema="karanlik"] { --secili-dolgu: var(--vurgu-yuzey) }` eklendi (eski karanlık eşleme) | Koyu_temada_secili_kenar_birincil_dugme_zemini_degil_ve_cercevesiz | AYNI |
| M7-31 | ortak.css | td içi si-girdi `inline-size: 8.25rem` → `auto` | Tablo_hucresinde_acik_satir_ici_girdi_sutunu_genisletmez | AYNI |
| M7-32 | ortak.css | açıklama `position: absolute` → `display: none` | Sayfa_basi_aciklama_satiri_cizilmez_metin_DOMda_kalir | AYNI |
| M7-33 | ortak.css | açıklama `overflow: clip` → `hidden` | Sayfa_basi_aciklama_… + OrtakGorunumSaflik.O7_ortak_css_sabit_renk_ve_gizleyen_tasma_tasimaz | AYNI |

### 0.4 Gerçek tarayıcı ölçümü (başsız Chrome CDP 9702/9703, vekil 5161 → API 5160)
- **Açılış matrisi, son kod** (`olc/tur2-acilis/`): 46 rota × BASİT/PRO × açık/koyu × 1440/1280/960/375, **736 ölçüm, hata 0**. Tablo `analiz.py once-acilis tur2-acilis`:

| bileşim | K10 | K3kirik | K7 | K7x | yatay | anaYatay | bitisik | K9 |
|---|---|---|---|---|---|---|---|---|
| BASIT açık 375 / 960 / 1440 | 0/0/0 | 0/0/0 | 0 | 0 | 0 | 0 | 0/0/0 | 0/0/2ᵃ |
| BASIT açık 1280 | 0 | 1ᵇ | 0 | 0 | 0 | 0 | 0 | 3ᵃ |
| BASIT koyu 375 / 960 / 1440 | 0/0/0 | 0/0/0 | 0 | 0 | 0 | 0 | 0/0/0 | 0/0/2ᵃ |
| BASIT koyu 1280 | 0 | 1ᵇ | 0 | 0 | 0 | 0 | 0 | 3ᵃ |
| PRO açık 375 / 960 / 1440 | 0/0/0 | 0/0/0 | 0 | 0 | 0 | 0 | 0/1ᶜ/0 | 0/0/2ᵃ |
| PRO açık 1280 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 5ᵃ |
| PRO koyu 375 / 960 / 1440 | 0/0/0 | 0/0/0 | 0 | 0 | 0 | 0 | 0/1ᶜ/0 | 0/0/2ᵃ |
| PRO koyu 1280 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 5ᵃ |

  - ᵃ K9'un tamamı `/denetim-izi`'den geliyor (başka rota yok). Ölçer örneği: `td "30.09.2026 22:02:15" ⟂ #dz-onceki "Önceki"` ve `td "…" ⟂ #dz-sayfa-bilgi`. Kaydırılan kayıt tablosunun kap dışındaki satırları sayfalama çubuğunun altında kalıyor.
    - Tur 1'in “önce” ölçümünde 1440'ta da vardı (5/4). P7 bulgu listesinde değil; sayfa işaretlemesi. Sayı denetim kaydı arttıkça değişiyor.
    - İstek satırı § 0.6'da.
  - ᵇ `/islemler` BASİT 1280: “Kasa seçin...” yer tutucusu iki satıra sarıyor. Kök neden ölçüldü (`olc/t2b/`):
    - `#islem-kasa-sar` 124 px. `form-grid-2` iki `1fr` iz taşıyor ve gizli “Ödeme Türü” hücresi (genişlik 0) ilk izi tutuyor. Metin 91 px istiyor, 82 px alıyor. 1440/960/375'te sığıyor.
    - Bu tur 2 ile gelmedi. Tur 2 tohumunda birden çok kasa var, bu yüzden seçici görünüyor. Tur 1 tohumunda tek kasa vardı ve salt okunur `#islem-kasa-tek` görünüyordu.
    - P7 öncesinde app.css aynı yeri “Kasa se…” diye keserdi (K10).
    - Düzeltme sayfa ızgarasında ve `tuval-satis.css` Satış'a ait. P7 bu dosyayı ezemez. İstek satırı § 0.6'da.
  - ᶜ `/cariler` PRO 960 çok satırlı ad ölçer yanılgısı (§ 2, tur 1'de de vardı).
  - **Tema yarışı (ölçer):** açık koşumun ilk rotalarında (`/ayarlar`, `/bildirimler`, `/denetim-izi`, `/ekranlar`) sunucudaki önceki karanlık tercih geç yüklendi, 32 “açık” görüntü koyu çıktı.
    - Bu 4 rota açık temada yeniden ölçüldü (`olc/t2e/`, `olc/t2f/`, `data-tema=aydinlik`, gövde rgb(242,244,247)).
    - BASİT/PRO × 1440/1280/960/375: /ayarlar, /bildirimler, /ekranlar **tüm sayaçlar 0**. /denetim-izi yalnız ᵃ K9.
- **Hedefli senaryo, son kod** (`olc/tur2-son-hedef/`): /cariler (renk + satır içi telefon), /gider (sekme rengi), /uyum, /siparisler (açıklama). BASİT/PRO × açık/koyu × 1440/1280/960/375 = **128 ölçüm, hata 0**.
  - Açılışlar temiz. Tek istisna ᶜ.
  - Renk ölçümleri § 0.1'deki gibi.
- **Görüntüler (Read ile görüldü):**
  - `olc/tur2-acilis/foto/cariler__BASIT_karanlik_1440__00_acilis.png`: “Hepsi” dolu koyu, çerçevesiz, üst anahtarla aynı. Ana eylem “Yeni müşteri” ayrı mavi.
  - `…/gider__BASIT_karanlik_1440__00_acilis.png`: “Giderler” dolu, mavi çerçeve yok.
  - `…/uyum__PRO_karanlik_960__00_acilis.png`
  - `…/cariler__BASIT_aydinlik_1280__00_acilis.png`: açık tema lacivert, değişmedi.
  - `olc/tur2-si825/foto/cariler__PRO_karanlik_1280__02_si_telefon.png`: “0532 123 45 67” tam, ✓ ✗ yanında, “Aktif” rozeti ayrı ve üst üste binme yok.
  - `olc/tur2-son-hedef/foto/cariler__BASIT_karanlik_375__01_renk.png`
  - `olc/tur2-son-hedef/foto/cariler__PRO_aydinlik_375__02_si_telefon.png`

### 0.5 Temizlik
- API 5160 (PID 13337) ve vekil 5161 (PID 14190) yalnız kendi PID'leriyle durduruldu. `lsof` 5160/5161/9702/9703 boş. 5199/5200'e dokunulmadı.
- `zsh kiraci-temizle.sh 01M3SQ58T1C7EXXVSQEG20QAK2 --uygula`: `PAKETKESIF-P7-TEST Tur2`, 1172 satır + tenants 1 silindi (`temizlik3.log`).
- SQL: `SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKETKESIF-P7%'` → **0**. Toplam 13; paralel şeritler yüzünden bilgi amaçlı. QA-TEST-* 4 kiracı, dokunulmadı.
- `fik.json`, `fik2.json`, `sistem.key`, `kayit-yanit.json` `printf '' >` ile boşaltıldı.

### 0.6 Denetim + açık kalanlar (tur 2)
- `bash denetim.sh` (`denetim-t2son.log`): P7 sayaçları ✓. Bunlar: DUGME-2 ×3, P-36 SECICI_TEK_KAYNAK, P-39 FORM_SATIR_HIZA_SAPMASI 0, ModalPencere 47 (tavan 55), satır içi style 67 (tavan 196).
- Tek ✗ `P-23: ODEME_KALEMSIZ_YOL (canlı) 4`. 4 satırın hepsi `PAKETYASAL2-TEST-HAKEM` kiracısında (01M3SQHTE445AB1438ACXXW7DJ, ödeme türü KARMA, veresiye 21000/21000/25000/22500,50).
  - Sahibi YASAL2 hakem şeridi. P7 deftere yazmaz. Bu yüzden DENETİM TEMİZ ağaç ölçüsünde şu an sağlanmıyor, ama P7 kaynaklı değil.
- İstek satırları:
  - **Sahibi SATIŞ (`tuval-satis.css` / Islemler.razor:167 `form-grid-2`):** çok kasalı kiracıda `#islem-kasa-sar` gizli “Ödeme Türü” hücresi yüzünden yarım sütunda kalıyor (1280'de 124 px) ve yer tutucu iki satıra sarıyor. Öneri: tek kasa kuralındaki gibi `:has(#islem-kasa)` ile seçici tüm satırı alsın.
  - **Sahibi Denetim izi sayfası:** `/denetim-izi` kayıt tablosunun kaydırılan satırları sayfalama çubuğuyla kesişiyor (K9 2–5).
  - **Ölçer (orkestratöre):** p7/tur sürücüsünde ilk rotalarda sunucu tercihindeki tema geç yükleniyor. Ölçüm öncesinde `data-tema` beklenmeli.

## 1. Yapılanlar (kök neden → tek kaynak)

| Sınıf | Bulgular | Kök neden (ölçüldü) | Çözüm (dosya) |
|---|---|---|---|
| K10 seçici kesiği | #19 #26 #32 #36 #46 #62 #63 #89 #90 | `app.css:1430` `.secici-metin` tek satır + `text-overflow: ellipsis` | `Secici.razor`: `secici-metin--tam` sınıfı + `title` = tam görünen metin (yer tutucuda yok). `ortak.css`: `.secici-tetik > .secici-metin--tam { white-space: normal; overflow: visible }`, iki satır kontrol boyuna (44 px) sığar, daha uzun değerde kutu büyür (`height:auto`). Çekmecede satır aralığı 1.2. `app.css` ezildi, düzenlenmedi |
| K10 komşu hizası | #27 #69 (/islemler Transfer/Değişim 375) | Uzun değer sarınca komşu kutu ±21 px kayıyordu | `ortak.css`: seçicili alan dar ekranda kendi satırına (`flex: 1 1 100%`). Değişim kaynağı `grid-column: 1 / -1`. Ödeme bacağı satırları hariç tutuldu (Havale · Kasa hizası korunur) |
| Katlanır başlık kesiği | #56 | `katlanir.css:17` üç nokta | `Katlanir.razor`: `title="@Baslik"`. `katlanir.css`: `white-space: normal` |
| K7 satır içi | #49 #58 #68 #73 #77 | Açık kutu (girdi + ✓ ✗) sarıyor, hücre boyu +38 px | `SatirIciDuzenle.razor`: açıkken kapta `si-acik` sınıfı. `ortak.css`: `.satir-ici.si-acik > .si-kutu { display:inline-flex; flex-wrap:nowrap }`. `dugme.css`: `.si-tetik` dinlenirken kutu çizmez (ZO-ISLEM-A kesikli kutu kararı ile birlikte) |
| K7 süzgeç yuvası | #9 + KIYAS #4 | Etiketli (Tür/Durum) ve etiketsiz (arama) kontroller ortadan hizalanıyordu | `SayfaBasi.razor`: yuvaya her zaman `filtre-yuvasi` sınıfı. `ortak.css`: `align-items: flex-end`, dar ekranda `flex-wrap: wrap`, seçici `min-width: auto` |
| K7 form ızgarası | #39 #41 #80 | Ölçüldü: yükseklik farkı değil, iç içe `.alan > .alan` ve seçicili alanın `row-gap`'i ızgaradan farklı (±8 px) | `ortak.css`: iç `.alan` ve seçici+not alanı `row-gap: inherit` |
| K3 tablo | #24 #43 #51 #53 #65 #88 #91 | Kısa değerler (kod, tarih, tutar, rol) bölünüyordu | `ortak.css`: `td > code` ve kısa değer hücreleri `nowrap`. Tablo kendi kabında kayar. Sayfaya özgü: `/gider-pusulalari` satıcı adı `min-width: 9em`, 1400 px üstünde hücre dolgusu 6 px (1440'ta “Yazdır” sütunu kesiliyordu). `/yetkiler` rol hücresi `nowrap`. `/cikti` 375'te ana yatay kaydırma 0 |
| Ad–rozet bitişik | #48 | “Tuna AkarsuDüşük”: rozet ile ad arasında boşluk yok | `ortak.css`: `td.hucre-vurgu` içindeki rozete `margin-inline-start: var(--bosluk-2)`, rozet `nowrap` (ölçüm: 1440/375 bitisik → 0) |
| DUGME-2 [YÜKSEK] tuval gerilemesi | TOPLU § DUGME-2 | Segmentte seçili dolgu ana eylem mavisiydi (hap içinde hap) | `dugme.css`: `--secili-dolgu: var(--kenar-yuzey)` (lacivert, tuvaldeki gibi), koyu temada `--vurgu-yuzey`/`--vurgu` (**tur 2'de kaldırıldı**, § 0.1). Seçili olmayan öğenin çerçevesi yok, kabın kenarı var |
| DUGME-2 [ORTA] | TOPLU § DUGME-2 | select kabı aşıyordu | `dugme.css`: `max-width: 100%; min-width: 0` |
| KATMAN-1b Kasalar ⋯ | TOPLU § KATMAN-1b | Menü uzun başlıkla 632 px'e genişliyordu. Devre dışı öğe dolu kırmızıydı | `katman.css`: menü `min(22rem, 100vw − 2·pay)`. Etkin öğe opak, devre dışı öğe dolgusuz. `.btn-ikincil` kenarı geri geldi (app.css `.kasa-duzenle-btn` `border:0` ezmesi) |
| ORTAK-1 K17 | TOPLU § ORTAK-1 | SayfaBasi açıklama satırları ekranı dolduruyordu | `ortak.css`: `.sayfa-basi-aciklama { display:none }` (**tur 2'de görsel gizlemeye çevrildi**, § 0.1) |

**Gerilemeye dikkat maddeleri:**
- `tuval-satis.css` Satış'a özgü kurallar ezilmedi.
- Kurucunun **Havale · Kasa hizası** önce ve sonra birebir aynı. `olc/once-satis` ile `olc/son2-satis` karşılaştırıldı: PRO 1440'ta `Ödeme Türü@437-456 | islem-odeme@460-504 | Kasa@437-456 | islem-kasa-tek@460-504`, iki koşumda aynı. BASIT'in 3 genişliği de aynı.
- KATMAN-1b 18 px kuralı yerinde (`katman.css:23` `--katman-oge-boyu`).

## 2. Sapmalar ve gerekçeleri
- **K10 “ikinci satıra sarma”** spesifikasyonun izin verdiği yoldur. Ölçerin `K3kirik` sayacı, /cariler PRO kimlik türünde “T.C. Kimlik No (11 hane)” seçili metninin iki satıra sarılmasını kırılma sayıyor. Bu tasarım gereği: aynı satırdaki komşuyla K7x = 0.
- **/cariler 960 `bitisik` = 1–3 kalıntısı** ölçer yanılgısı. Kaynağı “Ege Sarraflık … Ltd. Şti.” adı: 3 satıra sarıyor ve ölçer `ara=-370` veriyor (son satırın sağ kenarı ile sonraki hücredeki rozet arası). Ekran görüntüsünde ad ile rozet ayrı sütunda, arada boşluk var (`olc/son2-acilis/foto/cariler__PRO_aydinlik_960__00_acilis.png`, Read ile görüldü).
- **K9 (üst üste binen yazı)** önce/sonra aynı düzeyde. Sayı artışları iki kaynaktan geliyor, ikisi de P7 kaynaklı değil:
  - 1280 önce ölçülmediği için “0→n” görünüyor.
  - Ölçüm sırası açık çekmeceyi sonraki adıma taşıyor (/kasalar geçmiş çekmecesi, /pos Düzenle çekmecesi). Örnek: `#cekmece-kasa-gecmis`.
- **/siparisler yeni K7 ±48/56 ve /vezne `#vz-f-kalem-0` K7 ±10** önce ve sonra aynı. Bu iki yer P7 bulgu listesinde yok ve kaynakları sayfa işaretlemesi. İstek satırları § 9'da.
- **BASIT modda /cariler ve /islemler'de satır içi düzenleme ve seçici tetikleri yok** (tasarım gereği; ölçer `si-yok/tetik-yok` yazar). K7 ölçümü bu yerlerde PRO'da yapıldı.

## 3. Migration
Gerekmedi (şema değişikliği yok; yalnız bileşen + CSS).

## 4. Testler
- Yeni: `Kuyumcu.Tests/KesifP7Testleri.cs`, **14 test**. Zorunlu 4 test: `Secici_uzun_deger_tam_metin_ve_title`, `Secici_ellipsis_sinifi_yok`, `SatirIciDuzenle_acik_kutu_hucre_sinifi`, `SayfaBasi_filtre_yuvasi_etiketli_etiketsiz_ayni_hiza_sinifi`. Diğer 10 test:
  - `Katlanir_basligi_kesilmez_tam_metin_title`
  - `Form_izgarasi_secici_not_alani_satir_araligini_izgaradan_devralir`
  - `Dar_ekranda_secicili_alan_kendi_satirinda_odeme_bacagi_disinda`
  - `Uzun_deger_komsusuz_satirda_cekmecede_iki_satir_kontrol_boyuna_sigar`
  - `Kisa_deger_hucresi_bolunmez_tablo_kendi_kabinda_kayar`
  - `Sayfa_basi_aciklama_satiri_cizilmez_metin_DOMda_kalir`
  - `Segment_secili_lacivert_dolgu_secili_olmayan_cercevesiz_kap_kenarli`
  - `Segment_secili_dolgu_HER_temada_okunur`
  - `Select_kabini_asmaz_ve_satir_ici_acici_dinlenirken_kutu_cizmez`
  - `Satir_menusu_etkin_opak_devre_disi_dolgusuz_genislik_sinirli`
- Güncellenen: `DugmeIkiTestleri.cs` (DUGME-2 seçili dolgu beklentisi tuvale döndü) ve `KatmanTekKalipTestleri.cs` (menü en çok 22rem).
- Filtreli koşum (son kod, bu oturum): `KesifP7Testleri|DugmeIkiTestleri|KatmanTekKalipTestleri` → **Başarılı 53 / Başarısız 0** (`t-p7final.log`).
- Tam süit (son kod, 19:24–19:27, `trx-pKESIF-P7`): **Toplam 8056 · yürütülen 8055 · geçen 8054 · başarısız 1**. Tek kırmızı `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_hatasindan_sonra_pencere_acilinca_katlama_YENIDEN_yazilir`:
  - P7 dosyaları geri alınıp derlendiğinde de kırmızı (`t-firtina-once.log` 19:28, `build-geri.log`), yani P7 kaynaklı değil.
  - Devir listesinde zaten var: KESIF-P1/P2 raporları sahibini KESIF-P1 (AnaYerlesim) / SUBE-YASAL23 (Tercih) olarak yazıyor.
  - **P7'nin yeni kırmızısı: 0.**
- Build (bu oturum, `build14.log`): **0 Uyarı · 0 Hata**.

## 5. Mutasyon tablosu
Yöntem: yedek → tek eşleşmeli değişiklik → filtreli test → geri kopya → `cmp` (`mut.py`, `mut-sonuc.json`). Her mutasyonun `mut-M7-*.log` dosyası var. Hepsi kırmızı, hepsinde geri alma sonrası cmp AYNI. M7-2 ilk koşuda derleme hatası verdi; ikinci koşuda test kırmızı, cmp AYNI (`mut-sonuc.json` 2. satır).

| Kod | Dosya | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|---|
| M7-1 | Secici.razor | `secici-metin--tam` sınıfı silindi | Secici_ellipsis_sinifi_yok | AYNI |
| M7-2 | Secici.razor | title = ham `Deger` | Secici_uzun_deger_tam_metin_ve_title | AYNI |
| M7-3 | SatirIciDuzenle.razor | `si-acik` sınıfı silindi | SatirIciDuzenle_acik_kutu_hucre_sinifi | AYNI |
| M7-4 | SayfaBasi.razor | `filtre-yuvasi` silindi | SayfaBasi_filtre_yuvasi_etiketli_etiketsiz_ayni_hiza_sinifi | AYNI |
| M7-5 | Katlanir.razor | başlık `title` silindi | Katlanir_basligi_kesilmez_tam_metin_title | AYNI |
| M7-6 | ortak.css | secici-metin--tam `nowrap` | Secici_ellipsis_sinifi_yok | AYNI |
| M7-7 | ortak.css | filtre yuvası `center` | SayfaBasi_filtre_yuvasi_… | AYNI |
| M7-8 | ortak.css | si-kutu `wrap` | SatirIciDuzenle_acik_kutu_hucre_sinifi | AYNI |
| M7-9 | ortak.css | seçici+not alanı row-gap sabit | Form_izgarasi_… | AYNI |
| M7-10 | ortak.css | açıklama satırı `block` | Sayfa_basi_aciklama_satiri_… | AYNI |
| M7-11 | ortak.css | `td > code` `normal` | Kisa_deger_hucresi_bolunmez_… | AYNI |
| M7-12 | ortak.css | dar ekranda seçicili alan `auto` | Dar_ekranda_secicili_alan_… | AYNI |
| M7-13 | katlanir.css | başlık `nowrap` | Katlanir_basligi_kesilmez_tam_metin_title | AYNI |
| M7-14 | dugme.css | seçili dolgu `var(--vurgu)` (mavi) | Segment_secili_lacivert_dolgu_… | AYNI |
| M7-15 | dugme.css | koyu tema seçili kenar | Segment_secili_dolgu_HER_temada_okunur | AYNI |
| M7-16 | dugme.css | select `max-width` silindi | Select_kabini_asmaz_… | AYNI |
| M7-17 | dugme.css | .si-tetik dinlenirken kenar çizer | Select_kabini_asmaz_… | AYNI |
| M7-18 | dugme.css | segment öğesi kendi çerçevesi | D2_06_…, Segment_secili_lacivert_dolgu_… | AYNI |
| M7-19 | katman.css | devre dışı menü öğesi dolu kırmızı | Satir_menusu_etkin_opak_… | AYNI |
| M7-20 | katman.css | menü genişlik sınırı kaldırıldı | K1_02_bagli_popover_…, Satir_menusu_… | AYNI |
| M7-21 | dugme.css | select `min-width: 0` silindi | Select_kabini_asmaz_… | AYNI |
| M7-22 | ortak.css | iç `.alan` row-gap sabit | Form_izgarasi_… | AYNI |
| M7-23 | ortak.css | yuvada seçici `min-width: 0` | Form_izgarasi_… | AYNI |
| M7-24 | ortak.css | çekmecede satır aralığı 1.3 | Uzun_deger_komsusuz_satirda_… | AYNI |
| M7-25 | ortak.css | değişim kaynağı `grid-column: auto` | Uzun_deger_komsusuz_satirda_… | AYNI |
| M7-26 | ortak.css | sipariş kalemleri `overflow-x: visible` | Uzun_deger_komsusuz_satirda_… | AYNI |
| M7-27 | ortak.css | /yetkiler rol hücresi `normal` | Kisa_deger_hucresi_bolunmez_… | AYNI |
| M7-28 | ortak.css | yuva dar ekranda `nowrap` | Form_izgarasi_… | AYNI |

Son mutasyondan sonra temiz build: `build13.log` (19:29) ve bu oturumda `build14.log`, ikisi de 0 hata.

## 6. denetim.sh
`/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh` → **DENETİM TEMİZ**, ✗ sayısı 0 (`denetim-final.log`, bu oturum; 19:29'da `denetim-son.log` da temizdi).

## 7. Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9702, vekil 5161 → API 5160)
Ölçer: `sikisma-olc.js` + `tur/kesif.mjs` K7x + `tur/ekstra.js` + `olc/p7-olc.js`. Sürücü `olc/p7.mjs`, matris `olc/matris.sh`.

**Açılış (46 rota × BASİT/PRO × açık/koyu × 1440/960/375, 1280 yalnız sonra)** — önce 552, sonra 736 ölçüm (hata 0), `analiz-acilis.md`:

| bileşim | K10 | K3kirik | K7 | K7x | yatay | anaYatay | bitisik |
|---|---|---|---|---|---|---|---|
| BASIT açık 375 | 0→0 | 2→0 | 0→0 | 0→0 | 0→0 | 1→0 | 2→0 |
| BASIT açık 960 | 0→0 | 7→0 | 0→0 | 0→0 | 0→0 | 0→0 | 2→1* |
| BASIT açık 1440 | 0→0 | 6→0 | 0→0 | 0→0 | 0→0 | 0→0 | 2→0 |
| BASIT koyu 375 | 0→0 | 2→0 | 0→0 | 0→0 | 0→0 | 1→0 | 2→0 |
| BASIT koyu 960 | 0→0 | 9→0 | 0→0 | 0→0 | 0→0 | 0→0 | 2→1* |
| BASIT koyu 1440 | 0→0 | 6→0 | 0→0 | 0→0 | 0→0 | 0→0 | 2→0 |
| PRO açık 375 | 0→0 | 2→0 | 0→0 | 0→0 | 0→0 | 1→0 | 1→0 |
| PRO açık 960 | 0→0 | 9→0 | 0→0 | 0→0 | 0→0 | 0→0 | 1→1* |
| PRO açık 1440 | 0→0 | 6→0 | 0→0 | 0→0 | 0→0 | 0→0 | 1→0 |
| PRO koyu 375 | 0→0 | 2→0 | 0→0 | 0→0 | 0→0 | 1→0 | 1→0 |
| PRO koyu 960 | 0→0 | 9→0 | 0→0 | 0→0 | 0→0 | 0→0 | 1→1* |
| PRO koyu 1440 | 0→0 | 6→0 | 0→0 | 0→0 | 0→0 | 0→0 | 1→0 |
| 1280 (4 bileşim, yalnız sonra) | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

\* /cariler 960 ölçer yanılgısı (§ 2).

**Hedefli senaryolar (24 senaryo, 29 bulgunun adımları; açık tema + koyu 1280/375)** — önce 372, sonra 496 ölçüm (hata 0), `analiz-hedef.md`:
- K10 kesik: 1440 2→0 / 3→0, 960 2→0 / 3→0, 375 6→0 / 6→0.
- Rotalar: /raporlar/kasa, /banka, /gider, /uyum, /vezne, /islemler kaynak-sec, /cariler kimlik-vkn.
- K3kirik: 6–8 → 0. Tek istisna PRO /cariler kimlik-tc (tasarım gereği sarma, § 2).
- K7 ölçüm yerleri, hepsi **K7 0 · K7x 0**:
  - /cariler süzgeç ve satır içi (PRO)
  - /kasalar si-ac + ⋯ menü
  - /faturalar kalem-si
  - /islemler PRO ürün/kaynak seç
  - /urunler yeni/parite/daha-fazla
  - /urunler/agac yeni-kategori
- Ek (/uyum MASAK tablosu + /cariler rozet, `analiz-ek.md`): K3kirik 960 2→0, bitisik 1440/375 → 0.

**/pos satır içi (#77) — bu oturumda ölçüldü** (`olc/son4-pos/`, 36 ölçüm; tohum: 1 POS cihazı + 3 ve 6 taksit oranı, SQL'de `3 0.0425 1 / 6 0.0790 1`):

| mod × tema | 1440 si-ac | 960 si-ac | 375 düzenle (iki satır içi kutu açık) |
|---|---|---|---|
| BASIT açık | ok · K7 0 · K7x 0 · yatay 0 | ok · 0 · 0 · 0 | K7 0 · K7x 0 · yatay 0 |
| BASIT koyu | ok · 0 · 0 · 0 | ok · 0 · 0 · 0 | 0 · 0 · 0 |
| PRO açık | ok · 0 · 0 · 0 | ok · 0 · 0 · 0 | 0 · 0 · 0 |
| PRO koyu | ok · 0 · 0 · 0 | ok · 0 · 0 · 0 | 0 · 0 · 0 |

375'te ölçer `si-yok` yazıyor. Sebep: 1440 ve 960 adımlarında açılan iki kutu aynı oturumda açık kaldı, tıklanacak kapalı tetik kalmadı. Ekran görüntüsünde iki kutu da açık ve satır boyu bozulmadan duruyor.

**Ekran görüntüleri (Read ile görüldü):**
- `olc/son2-acilis/foto/cariler__PRO_aydinlik_960__00_acilis.png`: ad sarıyor, rozet ayrı sütunda, tablo kendi kabında kayıyor.
- `olc/son3-kasalar/foto/kasalar__BASIT_karanlik_1280__03_menu.png`: ⋯ menü 22rem ile sınırlı; Düzenle etkin ve kenarlı, Pasife Al tehlike rengi.
- `olc/son2-hedef/foto/islemler__PRO_aydinlik_375__02_kaynak_sec.png`: “30.09.2026 · 24 Ayar Gram Altın · Anadolu Külçe Serisi · Anadolu Holding Kuyumculuk Ticaret A.Ş.” 3 satır, üç nokta yok.
- `olc/son4-pos/foto/pos__PRO_karanlik_1440__02_si_ac.png`: açık kutu (4,25 ✓ ✗) ile “7,9” satırı hizalı.
- `olc/son4-pos/foto/pos__BASIT_aydinlik_375__01_duzenle.png`

Toplam ekran görüntüsü: `olc/` altında 7469 png.

## 8. Canlı doğrulama + temizlik
- API yalnız 5160'ta, vekil 5161'de koşuldu; iş bitince yalnız bu iki PID durduruldu. `lsof` 5160/5161 boş. 5199/5200'e dokunulmadı.
- Test kiracıları:
  - 1. oturum `PAKETKESIF-P7-TEST Kuyumculuk` (01M3S77H6QH4CXJXGREN2D5RG4): 3527 satır + tenants 1 silindi (`temizlik.log`).
  - Bu oturum aynı adla 01M3SK9HSMX4A438XRSJ01MG44: 68 satır + tenants 1 silindi (`temizlik2.log`, `zsh kiraci-temizle.sh … --uygula`).
- SQL: `select count(*) from tenants` → **13**. P7 kiracı sayısı **0**. QA-TEST-* kiracılarına dokunulmadı.
- Fikstür ve anahtar dosyaları `printf '' >` ile boşaltıldı (`fik.json`, `sistem.key`).

## 9. DoD sözleşmesi
Yeni varlık/kayıt türü yok, yeni uç yok. Düzenleme yolları değişmedi: SatirIciDuzenle, Çekmece ve ⋯ menü görünüm düzeyinde düzeltildi. Mevcut element id'leri değişmedi. Yeni CSS'te sabit renk yok (belirteç `var()`), satır içi style yok.

## 10. Dokunulan dosyalar (tek dokunuş)
- `dotnet/Kuyumcu.Web/Bilesenler/Secici.razor`, `SatirIciDuzenle.razor`, `SayfaBasi.razor`, `Katlanir.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/ortak.css`, `katman.css`, `katlanir.css`, `dugme.css` (`olcek.css` değişmedi)
- `dotnet/Kuyumcu.Tests/KesifP7Testleri.cs` (yeni), `DugmeIkiTestleri.cs`, `KatmanTekKalipTestleri.cs`
- Dokunulan sayfa `.razor`: **yok**.

`git status --porcelain | grep -v -E "/(bin|obj)/"`: dosyalar depoda izlenmiyor (`??`). Liste yukarıdaki gibidir.

## 11. Açık kalanlar (sahibiyle)
- `KATMAN2MenuTercihEkranTestleri.Istek_firtinasi_SENKRON_…` tam süitte kırmızı. Sahibi KESIF-P1 (AnaYerlesim) / SUBE-YASAL23 (Tercih). P7 dosyaları geri alınınca da kırmızı.
- İstek satırı, sahibi KESIF-P5 (Vezne/Siparişler sayfa işaretlemesi):
  - /siparisler yeni: `div.alan` Müşteri ve Kalemler K7 ±48/56.
  - /vezne `#vz-f-kalem-0` “Ürün — Adet (isteğe bağlı)” K7 ±10.
  - Önce/sonra aynı; P7 bulgu listesinde değil.
- İstek satırı, sahibi KESIF-P4: /urunler seçenek metni “G800 — 800 Gümüş (800‰)” (`Urunler.razor:1142`). K10 kesiği kapandı, metnin kendisi P4'te.
- KATMAN-1b Kasalar ⋯ açıklama cümlesinin kısaltılması P8'e gider (sözlük).
- ŞUBE-KAPSAM `Secici.razor:302` ad değişimi kapıda birleştirilecek. P7 o satıra dokunmadı.
- Ölçer iyileştirmesi (orkestratöre): `kesif.mjs` `bitisik` sayacı çok satırlı ad hücresinde yanlış pozitif veriyor (`ara` negatif). K9 sayacı, önceki adımdan açık kalan çekmeceyi sayıyor.

## 12. Kurucu / mali müşavir
Mevzuat içeren iş yok. Soru yok.
