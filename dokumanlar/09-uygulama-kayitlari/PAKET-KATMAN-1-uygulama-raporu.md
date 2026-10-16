# PAKET KATMAN-1 — Açılır katmanlar tek kalıpta: uygulama raporu

**Tarih:** 2026-09-25 · **Şerit:** KATMAN (t5: API 5166, vekil 5167, test veritabanı `kuyumcu_test_t5`, CDP 9501-9518) · **Rol:** Ertuğ (frontend). Backend ve veri modeli değişmedi.
**Durum:** BİTTİ (tur 3, 14:50 — **§ 14 bu turun tek doğru kaynağıdır**; tur 2 kaydı: 12:40). Doğrulayıcının KRİTİK bulgusu (375 px'te bağlı menünün iç kaydırması çalışmıyordu) kapatıldı, gerçek Chrome tekerlek ölçümüyle kanıtlandı; öteki üç bulgu ölçüldü ve sahibiyle yazıldı (tur 2) — § 13 tur 2 kaydıdır, § 5–§ 10 tur 1 kaydıdır (düzeltilen iddialar orada işaretli).

## 1. Sorun (ölçüldü) ve kök neden
- Kurucu 25.09: "Diğer işlemlere tıklıyorum, sayfanın ortasında seçim çıkıyor; ayrıca tipi farklı."
- Kök neden: `.satir-menu` bir HTML popover'dı. Tarayıcının varsayılan kuralı (`[popover]{inset:0;margin:auto}`) onu top-layer'da ekranın ORTASINA koyuyordu, açıcısıyla bağı yoktu. Bu menüyü kullanan üç yer var:
  - SatirEylemleri "⋯" (Cariler, Ürünler, Faturalar),
  - Kasalar "⋯",
  - FisDigerIslemler "Diğer ▾".
- Öğeler `.btn-kucuk` sınıfı yüzünden 13 px'ti. Seçici listesi seçenekleri gövde boyundaydı (18 px, Manrope).
- Taban turu (değişiklikten önce, § 5) şunu ölçtü: **277 SERBEST-YUZEN · 643 küçük yazı (<14 px) · 31 ekran dışı**.

## 2. Yapılanlar
| Dosya | Değişiklik |
|---|---|
| `wwwroot/js/katman.js` (YENİ) | Tek konum yardımcısı. `beforetoggle` ilk karede katmanı açıcının altına koyar, böylece ortada parlama olmaz. `toggle` kesin yerleşimi yapar: <br>· önce aşağı açar; yer yoksa yukarı çevirir (flip); <br>· sağ yarıdaki açıcıda sağ kenarları hizalar; <br>· görüş alanına 8 px payla kısar (clamp); <br>· azami boy = seçilen taraftaki kalan alan; <br>· iki geçiş yapar: yeni konumda doğal boy yeniden ölçülür. <br>Sayfa kaydırılınca ya da pencere boyu değişince yeniden konumlar; açıcı ekrandan çıkarsa `hidePopover` ile kapatır. Açılınca odak ilk etkin öğeye geçer. Değerler CSS belirteci olarak yazılır (`--katman-ust/-sol/-azami-boy`). Blazor'un koşullu çizdiği palet `data-katman-acici` + MutationObserver ile aynı yardımcıya bağlanır. eval yok, satır içi betik yok. |
| `wwwroot/css/katman.css` (YENİ) | (1) Bağlı popover UA ortalamasını ezer: `inset:auto; margin:0; top/left: var(--katman-…)`. Kural yalnız `data-katman="bagli"` varken devreye girer; betik çalışmazsa eski davranış kalır. <br>(2) 480 px altında menü genişliği `100vw-16px`. <br>(3) Seçici listesi öğeleri ve yüksekliği `--secici-azami-boy`. <br>(4) Palet öğeleri ve bağlı palet. <br>(5) Vitrin çekmecesinde 13 px'lik `.dugme-kucuk` → `.875rem`. <br>(6) `.gk-ayrinti` gövdesi kart içinde, kendi içinde kayar. <br>Öğe yazısı TEK belirteçten gelir: `--katman-oge-boyu: var(--yazi-govde)` (ölçek kademesiyle 16/18/20). Zemin, kenar, köşe ve gölge seçici listesiyle aynı belirteçler; sabit renk yok. |
| `wwwroot/js/secici-yerlesim.js` | Yukarı açma kararına "altta 240 px'ten az yer" (ASGARI_ALT) eklendi. Seçilen taraftaki kalan alan `--secici-azami-boy` + `data-katman-azami` olarak yazılıyor. Böylece sonradan dolan uzak liste ekran dışına çıkmıyor (taban: 960 px'te `#islem-cari-liste-kap` alt kenarı 4 px dışarıdaydı, 375'te `#id-tur/#id-odeme/#e-cari` listeleri dışarıdaydı). Tek çağıran yine Secici.razor (P-39 kilidi). |
| `wwwroot/index.html` | `css/katman.css` (app.css'ten sonra) ve `js/katman.js` eklendi. |
| `Bilesenler/GlobalArama.razor` | Palete `data-katman-acici="ust-kayit-ara"` eklendi, bir de açıklama yorumu. Mevcut id'ler değişmedi. |
| `Bilesenler/SatirEylemleri.razor`, `Bilesenler/Fis/FisDigerIslemler.razor` | Yalnız başlık yorumu düzeltildi ("JS yok" → katman.js). İşaretleme ve id'ler değişmedi. |
| `denetim.sh` | index.html betik sayacı 8 → 9, gerekçesiyle (P-39 deseni). Paket yeni betik istiyordu; bu değişiklik onun zorunlu sonucu. |
| Testler | Yeni `KatmanTekKalipTestleri.cs` (11 test). `KabukAramaEkranTestleri` içine +1 test (`K1_Palet_…`). `TasarimBelirtecTestleri` betik sayısı 8 → 9, gerekçeli. |

**Dokunulan sayfalar (TEK DOKUNUŞ):** hiçbir `Pages/*.razor` dosyası açılmadı. Kasalar, Vitrin ve Ayarlar yalnız ortak CSS/JS ile düzeldi; eşzamanlı şeritlerin sayfalarına dokunulmadı.
**Satış "Diğer" kutusu:** tuvalde (Profesyonel.dc.html) kesik çerçeve + noktalı özet VAR. Mevcut düğme tuvale uyuyor ve CUMA-S alanında (tuval-satis.css), dokunulmadı. Değişen yalnız menünün bağlanması.

## 3. Sapmalar ve gerekçeleri
1. **375 px'te alttan açılan sayfa (bottom sheet) kullanılmadı.** Bağlı menü `100vw-16px` genişlikle açıcının altında ya da üstünde açılıyor, tur ölçümünde `bagli-acilir`. Bottom sheet açıcıya bağlı olmadığından tur'da SERBEST-YUZEN sayılırdı. Spesifikasyon bunu "gerekirse" diye yazmış.
2. **Tek yardımcı ilkesi:** popover ve palet katman.js'te. Seçici listesi Secici.razor'un tek çağıranı olan secici-yerlesim.js'te kaldı ve aynı PAY=8 kuralını izliyor. P-39 testi kuyumcuSecici'yi tek çağıranlı kilitliyor, taşımak o kilidi bozardı.
3. **Vitrin çekmecesi düğmesi `.875rem` oldu (katman belirteci değil).** Çekmece bir form. Öteki düğmeler `.dugme` = .875rem (14 px), 13 px'lik tek öğe `.dugme-kucuk` idi. Düğmeyi 18 px yapmak çekmecedeki öteki düğmelerle uyumsuz olurdu.
4. **Görünüm ayrıntısının (details) gövdesi `max-height: 100vh-16px` ile kendi içinde kayıyor.** Taban ölçümünde 1199-1612 px'lik gövde ekranın altına taşıyordu (ekran-dışı).

## 4. Migration
Gerekmedi. Şema değişikliği yok.

## 5. Tur ölçümü — önce ve sonra (betikler/tur, başsız Chrome, kendi kiracım, vekil 5167)
Rotalar: /islemler /cariler /urunler /faturalar /kasalar /vitrin /raporlar/islem-defteri /emanet /ayarlar. Her rota {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1024, 960, 375} ile tarandı. GlobalArama 375'te her rotada açıldı (masaüstünde açıcısı gizli, Ctrl+K). Kiracı verisi: 6 müşteri, 5 ürün, 2 kasa, 2 fatura (API ile tohumlandı).

**analiz.py özetleri**
- Önce (`scratchpad/k1/analiz-once.txt`): kayıt 1350 · katman 1142 (893 açılan) · hata 0 · geçersiz 0. `bagli-acilir yazi boylari [('14px',157),('18px',115),('16px',32),('13px',14),('15.2px',6)]`. Benzersiz bulgu 70.
- Sonra, son kod (`scratchpad/k1/analiz-sonra2.txt`): kayıt 1324 · katman 1116 (897 açılan) · hata 0 · geçersiz 0. `bagli-acilir yazi boylari [('18px',435),('14px',20),('16px',8)]`. Benzersiz bulgu 23.

| Ölçü (açılan katman sayısı) | ÖNCE | SONRA |
|---|---|---|
| SERBEST-YUZEN | **277** | **0** |
| kucuk-yazi (<14 px) | **643** | **0** |
| ekran-disi | 31 | 2 (tek vaka, 2 tema; aşağıda ölçüm artefaktı olarak açıklandı) |
| sayfa-kaydi | 14 | 14 (hepsi /cariler 375 "Düzenle" çekmecesi; kapsam dışı, § 10) |
| Satır menüsü + palet + seçici öğe yazı boyları | 13/14/15.2/16/18 px | **yalnız 18 px** (TEK) |
| Yatay kaydırma | — | 0 (foto ölçümleri: `yatay: 0`) |

- **K1..K13 sayfa bulguları, taban ile karşılaştırma (`comm`):** yeni bulgu türü yok.
  - Tek fark `/ayarlar?sekme=guvenlik K4 tren` oranı (önce 2.5–4.9×6, sonra 2.1–2.7×6). Aynı bulgu hem önce hem sonra var. Oran, her tur girişinde oturum listesi büyüdüğü için oynuyor; bu sayfaya dokunulmadı.
  - `/faturalar K7` bulgusu sonrada kalktı.
- **[TUR 3 DÜZELTMESİ — bu madde EKSİKTİ, bkz. § 14]** Aşağıdaki kapalı-details gözlemi doğrudur, AMA "ürün kusuru değil" sonucu YANLIŞTI: "Elle ekle" AÇIKKEN Satış PRO'da liste `.islem-pro-kalem-kart` (overflow:hidden) tarafından kesiliyordu (32 durumun 16'sında). **Kalan ekran-dışı `/islemler 375 BASİT #islem-urun-liste-kap [12,575,345,351]` bir ölçüm artefaktı.** Tabanda birebir aynı değerle vardı. Kanıt (scratchpad `k1/tur/p-kay2.js`, `p-kay3.js`):
  - `#islem-urun`, KAPALI `details#islem-elle` ("Elle ekle") içinde duruyor (`elleOpen:false`);
  - tur onu `.click()` ile açıyor ama `scrollIntoView` kapalı details içinde kaydıramıyor (`s1:0`). Açıcı ekran dışında kalıyor (y=930–974).
  - Katman buna rağmen doğru bağlanıyor: `yukari` sınıfı var, azami boy 921 px, açıcının hemen üstünde.
  - Gerçek kullanıcı kapalı details içindeki düğmeye tıklayamaz. Dosya CUMA-S alanında (FisKalemGirisi).
- **Açılır katmanlardaki 14/16 px öğeler:** yalnız sayfa içi açılır formlarda kaldı. Bunlar `#islem-elle-satir` ("Elle ekle", CUMA-S) ve Ayarlar "Ayrıntılı görünüm" form etiketleri. Açılır menü ya da liste değiller; T-K3 tuvalindeki form ölçüsünde duruyorlar.

**Görüntüler (hepsi Read ile açılıp bakıldı):** yol `scratchpad/k1/tur/{once,sonra}/foto/`
- ÖNCE:
  - `islemler__PRO_aydinlik_1440__acici1.png`: Diğer ekranın ortasında, 13 px.
  - `islemler__PRO_aydinlik_375__acici3.png`: Diğer serbest yüzüyor.
  - `cariler__BASIT_aydinlik_375__acici4.png`: ⋯ ortada.
- SONRA:
  - `islemler__PRO_aydinlik_1440__acici1.png`: Diğer ▾ düğmesinin hemen altında, 18 px Manrope.
  - `islemler__PRO_aydinlik_375__acici3.png`: 375'te açıcının altında, ekran genişliğinde. ⚠ TUR 1'DEKİ "iç kaydırmalı" İDDİASI YANLIŞTI — doğrulayıcı ölçümle çürüttü (st=0); düzeltme ve yeni ölçüm § 13.
  - `cariler__BASIT_aydinlik_375__acici4.png`: ⋯ altında.
  - `cariler__PRO_aydinlik_375__acici2.png`: palet arama düğmesinin altında, tam genişlik.
  - `cariler__PRO_karanlik_1440__acici5.png`: iki geçiş düzeltmesinden önce yukarı açılan menü 26 px kopuktu; `sonra2`'de bağlı.
  - `scratchpad/k1/p-cariler-1440.png`: ⋯ sağ kenarı açıcıya hizalı.

## 6. Tuval karşılaştırması (1440×900)
Birleştirilmiş görüntüler:
- `ham/TUVAL-KARSILASTIRMA/KATMAN-1-Profesyonel-1440.png` (tuval Profesyonel.png | bizde /islemler PRO, Diğer açık)
- `ham/TUVAL-KARSILASTIRMA/KATMAN-1-Musteriler-1440.png` (tuval Musteriler.png | bizde /cariler PRO, ⋯ açık)

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| "Diğer ▾" düğmesi | kesik çerçeve, `Diğer ▾ <13 px özet>` | aynı; özet UX-S31 adlarıyla | AYNI (CUMA-S/T-S2 çıktısı, dokunulmadı) |
| Diğer menüsü | tuvalde çizilmemiş | düğmenin 4 px altında, sol kenarlar hizalı, 18 px Manrope, --yuzey/--kenarlik/--yaricap/--golge-lg | GEREKÇELİ FARK: tuvalde yok. Özellik silinmedi, kurucu şikâyetindeki "ortada serbest" giderildi |
| Satır "⋯" menüsü | tuvalde çizilmemiş (Müşteriler tuvali satır tıklayınca çekmece gösteriyor) | ⋯ düğmesinin altında, sağ kenarlar hizalı | GEREKÇELİ FARK: tuvalde yok, eylem kümesi (P-61) korundu |
| Sayfa gövdesi (sekmeler, kartlar, tablo) | tuval | değişmedi | Bu paketin kapsamı dışı (T-S2/CUMA-M şeritleri). Bu paket yalnız katmanları değiştirdi |

## 7. Testler
- **Önce:** paketten önce tam süit koşulmadı. Paket sırasındaki ilk tam koşum (`trx-pKATMAN-1/_Ramazan-MacBook-Air_2026-09-25_10_01_00.trx`) **6003/6003** geçti; bu, yeni testler dâhil ağacın o anki hâlidir.
- **Son tam koşum:** 6048 test, **6039 geçti, 9 başarısız.** 9 kırmızının hepsi başka şeridin sonradan eklediği işte (§ 10). Benim 12 testim ve dokunduğum dosyaların testleri tam süitte yeşil (`grep`: KatmanTekKalip ve K1_Palet için 12 Başarılı; SatirEylemleri/KabukArama/P39/TasarimBelirtec/S1iGorsel için 0 Başarısız).
- **Filtreli koşum** (Katman, KabukArama, SatirEylemleri, P39, S1iGorsel, TasarimBelirtec, Cari, GiderPusula, Kasa, Fis, Secici, Vitrin, Gorunum, Islemler): **897/897** yeşil.
- **Yeni testler (12):**
  - K1_01 index yükleme sırası
  - K1_02 UA ortalamasını ezen kural
  - K1_03 480 px genişlik
  - K1_04 TEK yazı belirteci, 14 px altı yok
  - K1_05 sabit renk yok, zemin/kenar/gölge seçiciyle aynı
  - K1_06 CSP
  - K1_07 secici-yerlesim, node ile GERÇEK kod: yukarı açma + azami boy
  - K1_08 aşağıya yerleşir, odak ilk öğede
  - K1_09 yukarı döner
  - K1_10 sağ kenar hizası ve clamp
  - K1_11 375'te sığar
  - K1_Palet: `data-katman-acici` gerçek açıcıyı gösteriyor, satır içi style yok
  - K1_08..K1_11 gerçek katman.js'i node + sahte DOM ile koşturuyor.
- **Derleme:** `dotnet build Kuyumcu.sln` 0 hata, 1 uyarı. Uyarı `EtiketEkranTestleri.cs(145) xUnit1031`, benim dosyam değil.

## 8. Mutasyon kanıtları (yedek `scratchpad/mut-yedek/KATMAN-1/`; her mutasyondan sonra geri alındı, `cmp` birebir)
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M-K1-1 / 1b | katman.js toggle → `if (false)` | K1_08, K1_09, K1_10, K1_11 |
| M-K1-2 | flip kararı `var yukari=false` | K1_09 |
| M-K1-3 | clamp satırı silindi | K1_10, K1_11 |
| M-K1-4 | katman.css `inset:auto` → `inset:0` | K1_02 |
| M-K1-5 | menü öğesi font-size → `var(--yazi-sm)` | K1_04 |
| M-K1-6 | GlobalArama `data-katman-acici` silindi (derlemeli) | K1_Palet_aciciya_baglanir… |
| M-K1-7 | secici azami boy satırları silindi | ilk denemede **HİÇBİR TEST KIRILMADI (test açığı)**: K1_07 yalnız dizge arıyordu. Test node ile gerçek davranışa çevrildi; tekrar: **K1_07 kırmızı** |
| M-K1-8 | index.html'den katman.js silindi | K1_01, Mod_yazicisi_BEYAZ_LISTE… |
| M-K1-9 | ASGARI_ALT koşulu kaldırıldı | K1_07 |
| M-K1-10 | CSS `min(24rem, var(--secici-azami-boy))` → `70vh` | K1_07 |
| M-K1-11 (tur) | `yerlestir` ilk satırda `return false` | tur'da SERBEST-YUZEN geri GELMEDİ, çünkü `beforetoggle` tek başına bağlıyor. Yardımcının tamamı kapatılarak M-K1-12 ile tekrarlandı |
| **M-K1-12 (tur)** | index.html'den katman.js çıkarıldı; PRO aydınlık 1440, 5 rota | **SERBEST-YUZEN 16'ya döndü** (/cariler 7, /urunler 5, /kasalar 2, /faturalar 2). Geri alındı, cmp birebir. Unit tarafında M-K1-8 → K1_01 kırmızı |

Son mutasyondan sonra temiz derleme alındı (build6: 0 hata).

## 9. Paket kapısı (7 madde)
| # | Koşul | Durum | Kanıt |
|---|---|---|---|
| 1 | build 0 hata | ✓ | build6.log: `0 Hata`, 1 yabancı uyarı |
| 2 | Paketin testleri 0 başarısız | ✓ | 12/12; filtreli 897/897 |
| 3 | Paketin sayaçları denetim.sh'ta ✓ | ✓ | `index.html <script> sayısı (sabit 9) 9`, `satır içi <script> 0`, `ModalPencere 48 (tavan 55)`, `emoji 0`, `SABIT_RENK 0`, `SECICI_TEK_KAYNAK 1`. Değişiklik sonrası ilk koşum `DENETİM TEMİZ.` verdi (denetim2.log) |
| 4 | Mutasyon kanıtı | ✓ | § 8 (12 madde, 1 açık kapatıldı) |
| 5 | Canlı doğrulama | ✓ | § 5 tur önce/sonra + foto ölçümleri (bağlı, 18 px Manrope, yatay 0) |
| 6 | Test kiracısı silindi | ✓ | `SİLİNDİ: PAKETKATMAN-1-TEST Kuyumculuk (01M3BMFQNA97T4AFJXNNK8P694)`, 2143 satır + tenants 1; davet satırı silindi (sayım 0). Başlangıçta tenants 14, sonda 15. Fark eşzamanlı şeritlerden geliyor: benim kiracım 0 satır; PAKETL-1b ve PAKETCUMA-J gitmiş, PAKET-ORTAK-1, PAKETCUMA-S, PAKET-ISTEK-1 gelmiş. QA-TEST-* kiracılarına dokunulmadı |
| 7 | Yeni kırmızı üretilmedi | ✓ | Tam süitteki 9 kırmızı Urunler.razor stok adetleri (yeni iç record), UrunStokOzetModulu ve SADE1 Hurda işlerinden geliyor; hiçbiri bu paketin dosyası değil. Paketten sonraki ilk tam koşum 6003/6003 idi |

API (5166) ve vekil (5167) durduruldu, `lsof` boş. Başsız Chrome süreçlerim kapatıldı. 5199 ve öteki şerit portlarına dokunulmadı; tarayıcı bölmesi kullanılmadı.

### Birleşik koşul karşılama
| Koşul | Durum |
|---|---|
| (1) Bağlı açılır: açıcının altı ya da üstü, clamp 8 px, kaydırmada konumlanır ya da kapanır, Esc ve dışarı tıklama kapatır (yerli popover), odak ilk öğede, ortak yazı/zemin/köşe/gölge | ✓ |
| (2) 375'te bağlı menü ekrana sığar | ✓ (`[8, y, 359, h]`) |
| (3) Seçici yer yoksa yukarı açılır, azami boy kalan alan, iç kaydırma | ✓ (tek artefakt § 5) |
| (4) Palet uygulama ölçeğinde, 375'te tam genişlik, ekran içinde | ✓ |
| (5) Vitrin pano çekmecesi 13 px → 14 px | ✓ |
| (6) GorunumKarti ayrıntı gövdesi kart içinde | ✓ (ekran-dışı 0) |
| SERBEST-YUZEN = 0 · küçük yazı = 0 | ✓ |
| ekran-dışı = 0 | ✓ (artefakt dışında) |
| sayfa-kaydi = 0 | ✗ — /cariler 375 "Düzenle" çekmecesi; tabanda da vardı, kapsam dışı, § 10 |
| id'ler değişmedi | ✓ |
| CSP: satır içi betik yok | ✓ |
| app.css'e yazılmadı | ✓ |
| Sozlukler: yeni metin yok | ✓ |
| Kaydet/POST, yetki ve yasal kapılar | Değişmedi |

**DoD sözleşmesi (yeni varlık):** yok. Paket veri modeli, uç ya da kayıt türü eklemedi; PATCH/DELETE/denetim yükümlülüğü doğmadı. Her menü öğesinin mevcut `@onclick` ve `disabled` + kilit sebebi davranışı aynen korundu.

## 10. Açık kalanlar (sahibiyle)
1. **Tam süitte 9 kırmızı** (son koşum):
   - `SozlesmeYansimaTestleri` ×2, `SozlesmeKapsamTestleri` ×2, `OlcumKapisiTestleri` ×2 (`Urunler.StokAdetleriYaniti/StokAdetSatiri` razor içi record);
   - `S33UrunlerEkranTestleri.S33_05/S33_06` ("4,25 adet");
   - `SADE1EkranTestleri.Hurda_sube_baglami…`.
   - Sahibi: Urunler stok adedi ve Hurda işini yapan eşzamanlı şerit. Bu işler, bu paketin ilk tam koşumundan (6003/6003) sonra ağaca girdi.
2. **denetim.sh'ta 5 ✗, aynı şeritten:**
   - razor record 5 > tavan 3,
   - P-10 ve P-16 record sayaçları,
   - Sınırsız ToListAsync 146 > 145 (`Modules/Urun/UrunStokOzetModulu.cs`),
   - P-02 ölçüm tabanı sapması.
   - Bu paketin değişikliklerinden sonraki ilk koşum `DENETİM TEMİZ.` idi.
3. **/cariler 375 BASİT "Düzenle" çekmecesi açılınca sayfa kayıyor** (-51 … -693 px). Tabanda da vardı. Sebep Cekmece/pencere kilidi (cekmece.js, `body.pencere-acik`). Bu paketin dosya alanında değil; Cekmece sahibine (UX tek düzenleme kalıbı) ya da CUMA-M'ye ait.
4. **`#islem-urun` kapalı "Elle ekle" details içinde olduğu hâlde tur onu görünür sayıp tıklıyor.** Bu tur aracının artefaktı: `vis()` kapalı details'i elemiyor. Öneri: ekstra.js `vis()`'e `e.closest('details:not([open])')` elemesi (orkestratörün betiği). ~~Ürün kusuru değil.~~ **[TUR 3 DÜZELTMESİ] Bu sonuç YANLIŞTI:** kapalı details'e tıklama gerçekten tur artefaktıdır (§ 14.4, `checkVisibility()` false), ama "Elle ekle" AÇIKKEN Satış PRO'da liste `.islem-pro-kalem-kart` (overflow:hidden) tarafından kesiliyordu — gerçek ürün kusuru. § 14'te `secici-yerlesim.js` kırpan ata denetimi + `.sabit` ile KAPANDI.
5. `/ayarlar` "Ayrıntılı görünüm" ve "Elle ekle" form etiketleri 14/16 px'te kaldı (sayfa içi form, açılır menü değil). Tuval ölçüsü korundu.

## 11. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` süzüldü; yalnız bu paketinkiler:
```
 M dotnet/Kuyumcu.Web/wwwroot/index.html
?? dotnet/Kuyumcu.Web/wwwroot/js/katman.js                (yeni)
?? dotnet/Kuyumcu.Web/wwwroot/css/katman.css              (yeni)
?? dotnet/Kuyumcu.Web/wwwroot/js/secici-yerlesim.js       (değişti; dosya izlenmiyor)
?? dotnet/Kuyumcu.Web/Bilesenler/GlobalArama.razor        (değişti)
?? dotnet/Kuyumcu.Web/Bilesenler/SatirEylemleri.razor     (yalnız yorum)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerIslemler.razor (yalnız yorum)
?? dotnet/denetim.sh                                      (betik sayacı 8→9)
?? dotnet/Kuyumcu.Tests/KatmanTekKalipTestleri.cs         (yeni)
?? dotnet/Kuyumcu.Tests/KabukAramaEkranTestleri.cs        (+1 test)
?? dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs        (8→9)
   dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/KATMAN-1-{Profesyonel,Musteriler}-1440.png (yeni)
```
`dotnet/denetim.sh.mutbak` bu paketin dosyası değil; başka şeridin mutasyon yedeği.

## 12. Kurucuya / mali müşavire notlar
Mevzuat konusu yok. Kurucunun göreceği değişiklik: menü öğeleri artık seçici listeleriyle aynı boyda (18 px). Önceden 13 px'tiler, bu yüzden menüler biraz daha büyük görünüyor.

## 13. Tur 2 — doğrulama bulgularının kapatılması (2026-09-25 11:45–12:40)
Yalnız iki dosya değişti: `wwwroot/js/katman.js` ve `Kuyumcu.Tests/KatmanTekKalipTestleri.cs`. Başka dosyaya dokunulmadı (razor, css, index.html, denetim.sh aynı).

### 13.1 [KRİTİK] İç kaydırma — KAPANDI
**Kök neden (doğrulayıcının teşhisi koda karşı doğrulandı):**
- `window.addEventListener('scroll', hepsiniYenile, true)` yakalama evresinde katmanın KENDİ kaydırmasını da yakalıyordu;
- `yerlestir` doğal boyu ölçmek için her geçişte `--katman-azami-boy`'u KALDIRIYORDU → kutu bir an büyüyor, tarayıcı `scrollTop`'u 0'a kısıyordu. Her tekerlek adımı kaydırmayı sıfırlıyordu.

**Düzeltme (katman.js):**
1. `kaydirmada(e)`: olayın hedefi bir bağlı katmanın kendisi ya da içindeki öğeyse (`closest('[popover].satir-menu, [data-katman-acici]')`) yeniden yerleştirme YAPILMAZ. Sayfa/kap kaydırmasında eskisi gibi çalışır.
2. `dogalBoy(katman, kutu)`: doğal boy azami boy KALDIRILMADAN ölçülür: `scrollHeight + (offsetHeight − clientHeight)`. `removeProperty('--katman-azami-boy')` koddan çıktı; `scrollTop` böylece korunur (sayfa kaydırmasında da).

**Gerçek Chrome ölçümü** (başsız, CDP 9501-9504, vekil 5167, kendi kiracım; `Input.dispatchMouseEvent` mouseWheel ×8, deltaY 120, katmanın ortasında). Betik: `scratchpad/k1b/tur/tekerlek.mjs`.

| Senaryo | Doğrulayıcı (eski kod) | Şimdi | Son öğe |
|---|---|---|---|
| /cariler 375×667 "⋯" (sh 414, ch 296) | st=0; "Kapat @719-763" kutu altı 658'in dışında | **st=118** (= sh−ch, sonuna kadar) | "Kapat @601-645", kutu 360-658 içinde, görünür |
| /cariler 375×812 "⋯" (yukarı açılır) | st=0; "Kapat @367-411", alt 386 | **st=38** (= 414−376) | "Kapat @329-373" görünür |
| /islemler 375×812 "Diğer" (kurucunun menüsü) | st=0; "Çek/senet devri @833-877", alt 804 | **st=86** (= 484−398) | "Çek/senet devri @747-791" görünür |
| CANLI MUTASYON: eski katman.js geri kondu, /cariler 375×667 | — | **st=0**, "Kapat @719-763" görünmez | geri alındı, `cmp` birebir; vekil yeni dosyayı veriyor (`kaydirmada` 2 eşleşme) |

Ekran görüntüleri (Read ile bakıldı) `scratchpad/k1b/tur/`: `sonra-cariler-375x667.png` (Pasife Al + Kapat görünür), `sonra-cariler-375x812.png`, `sonra-islemler-diger-375x812.png` (Siparişten satış + Çek/senet devri görünür; doğrulayıcının `prob6` görüntüsünde kesikti), `mut-eski-cariler-375x667.png` (eski kodda Döviz Veresiye'de kesik).

**Yeni testler (node + sahte DOM, GERÇEK katman.js).** Sahte DOM tarayıcının kısmasını taklit eder: azami boy kaldırılınca kutu içeriğe göre büyür, `scrollTop` `[0, scrollHeight−clientHeight]` aralığına kısılır.
- `K1_12_katman_ici_kaydirma_yeniden_yerlestirmez_scrollTop_korunur(hedef: "katman" | "oge")`: iç kaydırma olayı `--katman-azami-boy`'a 0 yazım/0 silme; `scrollTop` 90'da kalır.
- `K1_13_sayfa_kaydirilinca_yeniden_konumlar_ama_azami_boyu_kaldirmaz_scrollTop_korunur`: sayfa kaydırması yeniden konumlar (yukarı döner, azami 318, üst 8), azami boy hiç silinmez, `scrollTop` 90.
- `K1_14_yon_karari_DOGAL_boyla_verilir_onceki_acilistan_kalan_azami_boy_yaniltmaz`: önceki açılıştan 150 px azami boy kalmışken yön kararı doğal boyla (416) verilir → üst, azami 348, üst 8.

**Mutasyonlar** (yedek `scratchpad/mut-yedek/KATMAN-1b/katman.js`; her biri `perl -0777 -pi` tek eşleşme → `dotnet test --filter KatmanTekKalip` → yedek geri → `cmp` AYNI):
| Kod | Değişiklik | Kırmızı |
|---|---|---|
| M-K1-13 | `kaydirmada` içindeki `return` → `{ }` (iç kaydırma süzgeci kapalı) | `K1_12(hedef:"katman")`, `K1_12(hedef:"oge")` — 2/14 kırmızı |
| M-K1-14 | `yerlestir` döngüsüne `removeProperty('--katman-azami-boy')` geri eklendi (eski davranış) | `K1_13_sayfa_kaydirilinca…` — 1/14 kırmızı |
| M-K1-15 | `dogalBoy(katman, k)` → `k.height` (kısılmış kutu boyu) | ilk denemede **hiçbir test kırılmadı (test açığı: iki geçiş K1_13'te yakınsıyordu)** → `K1_14` eklendi; tekrar: `K1_14_yon_karari…` kırmızı (1/15) |
| M-K1-16 (canlı) | eski katman.js tarayıcıya verildi | tekerlek ölçümü st=0 (yukarıdaki tablo) |

### 13.2 [ORTA] Sayfa kayması — ölçüldü, bu paketin değil, sahibiyle
Tur (önce `k1/tur/once` 09:47 · sonra `k1b/tur/sonra` 12:30), >40 px kayma sayısı:
| Rota | Önce (paketsiz) | Tur 1 sonrası (10:46) | Doğrulayıcı (11:15-11:43) | Şimdi |
|---|---|---|---|---|
| /vitrin (Düzenle çekmecesi) | **0** | **0** | 348 | 352 |
| /cariler (Düzenle çekmecesi) | 14 | 14 | 24 | 30 |
| /faturalar (Düzenle çekmecesi) | 0 | 0 | 0 | 18 |
Hepsi `cekmece`/`tam-ekran` türü "Düzenle" açıcısı; hiçbiri bağlı menü, seçici ya da palet değil.

**Bu paketin olmadığının kanıtı:** `scratchpad/k1b/tur/vitrin.mjs`, 1440×900, /vitrin'de 5 "Düzenle" sırayla:
- katman.js + katman.css YÜKLÜ: kayma 0, −36, −72, −115, −158 (her açılışta `scrollY` 357'ye dönüyor);
- katman.js + katman.css CDP `Network.setBlockedURLs` ile ENGELLİ (`window.kuyumcuKatman` yok): **birebir aynı** 0, −36, −72, −115, −158.
Kayma, taban (09:47) ve tur 1 (10:46) ölçümlerinde yoktu; `wwwroot/js/cekmece.js` 10:55:45'te başka şeritçe değişti ("sayfa zıplaması" kilidi: `sabitle()` açılışta `document.__kuyumcuKaydirma` saklar, `konumuKoru()` geri yazar — `scrollY` sabit 357'ye dönmesi saklanan konumun eskidiğini gösteriyor). **Sahibi: cekmece.js'i 10:55'te değiştiren şerit (doğrulayıcıya göre ORTAK-1 "Çekmece açılınca sayfa zıplaması").** Vitrin.razor ve Cekmece/cekmece.js bu paketin dosya alanında değil (Vitrin yalnız pano çekmecesi CSS'i için alanımda); dokunulmadı.

### 13.3 [DÜŞÜK] Tek yazı boyu — ölçüldü; çekmece orkestratör kararına bırakıldı
Son tur, her bağlı açıcı ayrı ayrı: `#c-menu-ac` 112, `#u-menu-ac` 80, `#fk-menu-ac` 32, `#kasa-menu-ac` 31, `#islem-diger-ac` 8, `#ust-kayit-ara` (palet) 36, seçiciler (`#id-tur`, `#id-odeme`, `#e-cari`, `#cari-tur-filtre`, `#urun-*-filtre`, `#islem-cari/-urun/-kasa`) — **öğe yazısı yalnız 18px** (TEK). 14/16/13 px görülenler bağlı menü değil sayfa içi açılan form bölümleri: `#islem-elle-ac`, `#islem-diger-ayrinti-ac`, `#islem-pro-odeme-degistir` (13 px ×8 — CUMA-S'in yeni öğesi, Pages/Islemler; tur 1'de yoktu), `/ayarlar #ayar-gorunum-ayrinti-baslik` form etiketleri. Vitrin pano çekmecesi 14 px (çekmecenin öteki `.dugme`leriyle aynı, § 3.3). Kabul ölçütündeki "TEK font-size" çekmeceleri (ortak Cekmece, 14 px form ölçüsü) kapsıyorsa bu tek pano çekmecesinde değil tüm çekmecelerde karar ister → **orkestratöre**; bu turda değiştirilmedi.

### 13.4 [DÜŞÜK] Emoji ikonlar — sahibi
Emoji `SatirEylemleri`'nin kendisinde değil, çağıranın verdiği `Eylem.Ikon` metninden geliyor (/cariler: 📝 Veresiye, ✍ Düzeltme Fişi, 🗑 Pasife Al — `Pages/Cariler.razor`, CUMA-M alanı). Eşzamanlı şerit kuralı gereği sayfaya dokunulmadı. **Sahibi: CUMA-M** — `Ikon` alanına Ikon bileşeni adı verilmeli.

### 13.5 Son tur (sonra, 12:30) — analiz.py
`scratchpad/k1b/analiz-sonra3.txt`: kayıt 1426 · katman 1218 (937 açılan) · hata 0 · geçersiz 0.
| Ölçü (açılan katman) | ÖNCE (09:47) | ŞİMDİ |
|---|---|---|
| SERBEST-YUZEN | 277 | **0** |
| ekran-dışı | 31 | ~~0 (artefakt da kalktı)~~ **[TUR 3 DÜZELTMESİ] YANLIŞ İDDİA:** `#islem-urun` bu turda hiç açılmamıştı (ölçümsüz); gerçek durumda Satış PRO'da liste kesiliyordu → § 14 |
| küçük yazı <14 px | 643 | 8 — hepsi `#islem-pro-odeme-degistir` (CUMA-S sayfa içi bölüm, bağlı menü değil) |
| sayfa-kaydı | 14 | 400 — hepsi "Düzenle" çekmecesi, § 13.2, bu paketin değil |
| yatay kaydırma (208 sayfa/sekme) | — | 0 · K1 gizli-görünen 0 · K5 'TRY' 0 |
| K1..K13 yeni bulgu (rota bazında `comm`) | — | **0 yeni**; 7 kalkan (/faturalar K7, /urunler K7, /islemler K3×2, K4, K10, K12) |
Görüntüler (Read ile bakıldı): `k1/tur/once/foto/islemler__PRO_aydinlik_1440__acici1.png` (Diğer ortada, 13 px) ↔ `k1b/tur/sonra/foto/islemler__PRO_aydinlik_1440__acici1.png` (Diğer ▾ altında, 18 px); `k1/tur/once/foto/cariler__PRO_aydinlik_375__acici4.png` (⋯ ortada) ↔ `k1b/tur/sonra/foto/cariler__PRO_aydinlik_375__acici9.png` (açıcının üstünde, ekran genişliğinde); `k1b/tur/sonra/foto/cariler__PRO_aydinlik_1440__acici8.png` (⋯ sağ kenara hizalı, 18 px).

### 13.6 Kapı (7 madde) — tur 2
| # | Durum | Kanıt |
|---|---|---|
| 1 build | ✓ | `dotnet build Kuyumcu.sln` 0 Hata 0 Uyarı (k1b-build.log; son mutasyondan sonra temiz derleme build-son2.log: 0 Uyarı 0 Hata — ilk deneme build-son.log eşzamanlı derleme yarışıyla `MSB3030 apphost kopyalanamadı` verdi, öteki derlemeler bitince tekrarlandı); test projesi yeniden derlemede 1 uyarı `ApiIstemci.cs(307) CS0219` — başka şeridin dosyası |
| 2 testlerim | ✓ | KatmanTekKalip 15/15 + K1_Palet; filtreli (Katman/SatirEylem/FisDiger/Secici/KabukArama) 153/153 (yeni testlerden önce); tam süitte 16/16 Başarılı |
| 3 sayaçlarım | ✓ | denetim.sh: `index.html <script> sayısı (sabit 9) 9`, `satır içi <script> 0`, `ModalPencere 48 (tavan 55)`, `SABIT_RENK 0`, `SECICI_TEK_KAYNAK 1`. Denetimde 2 ✗ başkasının: ToListAsync 146>145 (Modules — bu paket backend'e dokunmadı), P-23 ODEME_KALEMSIZ_YOL 3 (SQL ile: üç satırın hepsi `PAKETSATIS-2-TEST Kuyumculuk` kiracısında) |
| 4 mutasyon | ✓ | § 13.1 (M-K1-13..16; bir açık bulundu, K1_14 ile kapandı) |
| 5 canlı | ✓ | § 13.1 tekerlek tablosu + § 13.5 tam tur (API 5166, vekil 5167, 5 müşteri + 1 tedarikçi, 5 ürün, 2 kasa, 2 fatura API ile, HTTP 201/200) |
| 6 kiracı silindi | ✓ | `kiraci-temizle.sh 01M3BWZ66CMHNS36RPNCDP9RWV --uygula` (881 satır + tenants 1); davet satırı silindi (row_count 1, kalan 0); tenants başta 16, sonda 16; QA-TEST-* dokunulmadı. API ve vekil `kill` edildi, 5166/5167 `lsof` boş |
| 7 yeni kırmızı yok | ✓ | Tam süit (`trx-pKATMAN-1b/_Ramazan-MacBook-Air_2026-09-25_12_33_39.trx`): 6134 · 6118 geçti · 16 başarısız. Bu turda değişen yalnız katman.js (bUnit JS çalıştırmaz) ve KatmanTekKalip testleri; 16 kırmızının hiçbiri bu dosyalara dayanmıyor. Sahipleri: CUMA-J (`CumaJTestleri.CJ_01`, `EvrakEkranTestleri.CJ_05`), CUMA-S/SATIS-2 (`S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz`, `SATIS2EkranTestleri` ×5, `TS1TuvalSatisTestleri.Son_uc_satis…`, `UX1C1EkranTestleri` ×6), ISTEK-1 (`Istek1OnbellekTestleri.ISTEK1_08…`) |

**Dokunulan sayfalar (tur 2):** yok.

## 14. Tur 3 — doğrulayıcının KRİTİK bulgusu: kırpan ata (2026-09-25 13:44–14:55)
Değişen dosyalar (bu tur): `wwwroot/js/secici-yerlesim.js`, `wwwroot/css/katman.css`, `Kuyumcu.Tests/KatmanTekKalipTestleri.cs`. Razor, index.html, app.css, denetim.sh'a dokunulmadı. **Dokunulan sayfalar (tur 3): yok.**

### 14.1 Kök neden (koda karşı doğrulandı, tabanda yeniden ölçüldü)
`secici-yerlesim.js` yer hesabını yalnız GÖRÜŞ ALANINA göre yapıyordu. Satış PRO'da `#islem-urun` seçicisi `.islem-pro-kalem-kart` (overflow:hidden) içinde; `.secici-liste-kap` `position:absolute` olduğundan kartın dışında kalan kısmı kesiliyordu. Tur 1/2 raporundaki "tur artefaktı, ürün kusuru değil" (§ 10.4) ve "ekran-dışı 0" (§ 13.5) iddiaları YANLIŞTI; ikisi de yerinde işaretlendi. Tur verimde `#islem-urun` hiç "Elle ekle" açıkken açılmamıştı — iddia ölçümsüzdü.

### 14.2 Düzeltme
- `secici-yerlesim.js`: `kirpanKutu(panel)` — panelin atalarından `overflow ≠ visible` olanların kutularının kesişimi. Panelin açılacağı kutu (liste sonradan büyüyebileceği için en az 240 px varsayılır) bu kesişime sığmıyorsa panel `.sabit` olur ve `sabitKonumla()` konumu tetiğe göre belirteçle verir: aşağı açılışta `--secici-ust = tetik.bottom + 4`, yukarı açılışta `--secici-alt = innerHeight − tetik.top + 4`, `--secici-sol` (sağa taşarsa sağ kenar hizası, ekrana 8 px payla kenetli), `--secici-gen`, `--secici-azami-boy`. Dönüşümlü (transform) bir ata fixed'in referansını değiştirirse ölçülen fark belirtece eklenir.
- Açık `.sabit` paneller: `scroll` (yakalama evresi) ve `resize` olayında tetikle birlikte yeniden konumlanır. Panelin KENDİ iç kaydırması yeniden yerleştirmez (katman.js K1_12 dersi). Kırpan ata yoksa ya da panel ona sığıyorsa eski yol (`.yukari` / `.sola` / `.kaydir`) aynen çalışır.
- `katman.css`: `.secici-kap > .secici-liste-kap.sabit { position: fixed; top: var(--secici-ust, auto); bottom: var(--secici-alt, auto); left: var(--secici-sol, …); width: var(--secici-gen, auto); min-width: 0; max-width: calc(100vw − 16px); translate: none }`. Sabit renk yok; app.css'e yazılmadı.
- Alternatif (listeyi top-layer'a taşımak) seçilmedi: Secici.razor'un perde/odak düzeni ve 50+ çağıranı değişirdi. `.sabit` yalnız gerektiğinde devreye girer.

### 14.3 Canlı ölçüm — önce/sonra (başsız Chrome, API 5166, vekil 5167, kendi kiracım)
Betik `scratchpad/k1c/sonda.mjs`. "Elle ekle" açılır, `#islem-urun` tıklanır. İki konum ölçülür: `dogal` (sayfa başı, tetik aşağıda → yukarı açılır; doğrulayıcının senaryosu) ve `orta` (tetik ortada → aşağı açılır). Listenin 5 dikey noktasında `elementFromPoint` listenin içinde mi (1 = görünür), arama kutusu görünür mü? ÖNCE = eski `secici-yerlesim.js` canlıya geri kondu (canlı mutasyon M-K1-22); sonra geri alındı, `cmp` AYNI.

| Mod | Genişlik | Konum | ÖNCE sınıf · 5 nokta · arama | SONRA sınıf · 5 nokta · arama |
|---|---|---|---|---|
| PRO | 1440 | dogal/orta | aşağı · 11000 · ✓ (alt 3/5 kesik) | sabit · **11111** · ✓ |
| PRO | 1024 | dogal | yukari · 00011 · ✗ (arama + ilk 3 öğe yok) | yukari sabit · **11111** · ✓ |
| PRO | 1024 | orta | aşağı · 11000 · ✓ | sabit · **11111** · ✓ |
| PRO | 960 | dogal | yukari · 00011 · ✗ | yukari sabit · **11111** · ✓ |
| PRO | 960 | orta | aşağı · 11000 · ✓ | sabit · **11111** · ✓ |
| PRO | 375 | dogal | yukari · 00011 · ✗ | yukari sabit · **11111** · ✓ |
| PRO | 375 | orta | aşağı · 11110 · ✓ | sabit · **11111** · ✓ |
| BASİT | 1440/1024/960/375 | dogal/orta | 11111 · ✓ (kırpan ata yok) | 11111 · ✓ (değişmedi, `.sabit` yok) |

Aydınlık ve koyu tema birebir aynı sonucu verdi (32 + 32 kayıt: `sonda-once.log`, `sonda-sonra.log`). ÖNCE: 16/16 PRO durumunda liste kesik; SONRA: 32/32 durumda 5/5 nokta görünür. Ekran dışı 0, yatay kaydırma 0, öğe yazısı 18 px.

**Kaydırmada izleme (canlı, `sonda2.mjs`, PRO 1024):** liste `yukari sabit`, tetik 468–512, liste 137–464 (aralık 4). Sayfa 60 px kaydırıldı: tetik 408–452, liste 77–404 (aralık 4) → liste tetikle birlikte gidiyor.

**Görüntüler (Read ile bakıldı):**
- `ham/TUVAL-KARSILASTIRMA/KATMAN-1c-islem-urun-once-sonra-1024.png`: solda yalnız "Yarım/Zincir" görünüyor, arama kutusu kartın üstünde kesik; sağda arama + 6 öğe tam.
- `…-375.png` (koyu): aynı kusur ve düzeltme.
- `scratchpad/k1c/sonda/sonra-islem-urun-PRO-aydinlik-1440-orta.png`: aşağı açılış, liste kart altından taşıyor ama kesilmiyor.
- `sonda2-B-kaydirma-sonrasi-1024.png`.

### 14.4 Tam tur (sonra, 14:15–14:47) — `tur.mjs` 9 rota × {BASİT,PRO} × {aydınlık,karanlık} × {1440,1024,960,375}
Kayıt 1426, açılan katman 937, hata 0, 16/16 kombinasyon. Dağılım: bağlı-açılır 491 · çekmece 315 · tam-ekran 127 · SERBEST-YÜZEN 4. `.sabit` olan seçiciler: `#islem-urun` 8, `#islem-kasa` 6, `#urun-maden-filtre` 4, `#e-cari` 4, `#urun-kategori-filtre` 2, `#cari-tur-filtre` 2, `#urun-durum-filtre` 2.

| Ölçü | Taban (k1 09:47) | Tur 2 (12:30) | Tur 3 (14:47) | Açıklama |
|---|---|---|---|---|
| SERBEST-YÜZEN | 277 | 0 | 4 | 4'ü de `#islem-urun` (PRO 1024/960, 2 tema). **Tur artefaktı, ürün değil:** tur tetiğe KAPALI "Elle ekle" details içindeyken JS `.click()` ile tıklıyor. Canlı kanıt (`sonda2.mjs` A): `detailsAcik:false`, `tetikGorunur:false`, `listeGorunur:false`. Ekran görüntüsü `tur/sonra/foto/islemler__PRO_aydinlik_1024__acici5.png`: details kapalı, liste hiç çizilmemiş. Açık details'le gerçek kullanım § 14.3'te 32/32 tam görünür. Tur 2'de 0 çıkmasının sebebi `.sabit` değil; o turda `#islem-urun` tıklanmamıştı. |
| ekran-dışı | 31 | 0 (ölçümsüz) | 18 | 12'si aynı artefakt (`#islem-urun`, kapalı details, tetik y≈880–1040, görünmez). 6'sı `#islem-pro-odeme` (`FisProOdemeHapi.razor` + `tuval-satis.css`, **CUMA-S**; 1024'te sağdan 3 px, 375'te 75 px taşıyor; PRO; bu paketin dosyası değil). |
| küçük yazı (<14 px) | 643 | 8 | **0** | — |
| bağlı-açılır öğe yazısı | — | 18 | 18 px ×434, 14 px ×51, 16 px ×24 | 14/16 olanlar sayfa içi form (§ 13.3), bağlı menü/seçici/palet değil. |
| sayfa-kaydı >40 px | 14 | 400 | 9 | 9'u da tam-ekran "Düzenle"; bağlı-açılır/seçici/palet 0. |
| yatay kaydırma · K1 gizli-görünen · K5 'TRY' | — | 0·0·0 | 0·0·0 | — |
| K1..K13 yeni bulgu (tur 2 ile rota bazında `comm`) | — | — | 1 | `/islemler K4 tren 2.6×6` (PRO 375/960). Sayfa yükünde ölçülen düzen bulgusu; açık katman yokken `.sabit` kuralı devreye girmez. Satış sayfası/Fis bileşenleri bu turda CUMA-S tarafından değişti (Fis/*.razor 14:20–14:37). **Sahibi: CUMA-S.** |

### 14.5 Testler (node + sahte DOM, GERÇEK secici-yerlesim.js)
- `K1_15_kirpan_ata_paneli_keseceginde_panel_SABIT_olur_ve_ekranda_tetige_bagli_tam_gorunur`: doğrulayıcının ölçüsü (1024×768, tetik 445–489, kart 339–647 overflow:hidden, panel 327 px). Beklenen: `sabit` + `yukari`, `--secici-alt` 327px, azami 433px, sol 40px, gen 320px; kutu 114–441, ekran içinde. Ek olarak katman.css `.sabit` kuralı: position fixed, bottom/left/width belirteçten.
- `K1_16_kirpan_ata_yoksa_ya_da_panel_ataya_sigiyorsa_eski_yerlesim_SABIT_yok`: kart overflow:visible → `sabit` yok; kart 20–760 (panel sığıyor) → `sabit` yok.
- `K1_17_sabit_panel_sayfa_kayinca_tetikle_gider_kendi_ic_kaydirmasi_yeniden_yerlestirmez`: sayfa kaydırması 100 px → `--secici-alt` 427px. Panelin kendi iç kaydırması → 0 belirteç yazımı/silmesi.

Filtreli koşumlar:
- `KatmanTekKalip` 18/18 ✓.
- Katman/Secici/SatirEylem/FisDiger/KabukArama/P39/S1iGorselSigma2/TasarimBelirtec: 232/235. Kırmızı 3'ü `S25bSaticiEkranTestleri` E1/E2/E4 (Satış ekranı WaitFor zaman aşımı). Bu dosyalar JS/CSS okumuyor (bUnit tarayıcı JS'i çalıştırmaz). Aynı üç test hemen ardından tam süitte YEŞİL geçti. Kararsız, bu paketin değil.

**Not:** Test DLL'i ilk derlemede yeni testleri içermedi. Eşzamanlı şeridin derlemesi dosya kaydımdan önce başlamış, `CoreCompile` "güncel" sanmıştı. `touch` + yeniden derlemeyle çözüldü; 18 test `--list-tests` ile doğrulandı.

### 14.6 Mutasyonlar (yedek `scratchpad/mut-yedek/KATMAN-1c/`; her biri `perl -0777 -pi` tek eşleşme → `dotnet test --filter KatmanTekKalip` → yedek geri → `cmp` AYNI)
| Kod | Değişiklik | Kırmızı |
|---|---|---|
| M-K1-17 | `var kirpan = kirpanKutu(panel);` → `var kirpan = null;` (ata denetimi kaldırıldı — doğrulayıcının istediği mutasyon) | `K1_15…`, `K1_17…` (2/18) |
| M-K1-18 | `hepsiniYenile` içindeki `sabitKonumla(...)` silindi (kaydırmada izleme yok) | `K1_17…` (1/18) |
| M-K1-19 | iç kaydırma süzgeci `return true;` → `{}` | `K1_17…` (1/18) |
| M-K1-20 | katman.css `.sabit` kuralından `position: fixed;` silindi | `K1_15…` (1/18) |
| M-K1-21 | `kirparMi` → `return false` (overflow asla kırpmaz sayılır) | `K1_15…`, `K1_17…` (2/18) |
| M-K1-22 (canlı) | Eski secici-yerlesim.js canlıya verildi (konumlama yardımcısı devre dışı) | `sonda-once`: 16/16 PRO durumunda liste kesik (5 noktadan 2-4'ü görünmez, 1024/960/375 dogal'da arama kutusu görünmez) |

Son mutasyondan sonra dosyalar `cmp` ile yedeğe birebir eşit. Son temiz derleme `build-son3.log`: 0 Hata, 1 Uyarı. Uyarı `EtiketEkranTestleri.cs(145) xUnit1031`, başka şeridin dosyası. İlk iki deneme başka şeridin eşzamanlı derlemesi yüzünden `MSB4018 ConvertDllsToWebCil … Kuyumcu.Web.dll being used by another process` verdi; 45 sn sonra tekrarlandı, yeşil.

### 14.7 Kapı (7 madde) — tur 3
| # | Durum | Kanıt |
|---|---|---|
| 1 build | ✓ | `dotnet build Kuyumcu.sln` 0 Hata (build-son3.log) |
| 2 testlerim | ✓ | KatmanTekKalip 18/18; tam süitte hepsi Başarılı |
| 3 sayaçlarım | ✓ | denetim.sh: `index.html <script> 9`, `satır içi <script> 0`, `SABIT_RENK 0`, `SECICI_TEK_KAYNAK 1`, `ModalPencere 48 (tavan 55)`. Tek ✗ `P-23 ODEME_KALEMSIZ_YOL 3`: SQL ile üç satırın hepsi `PAKETSATIS-2-TEST Kuyumculuk` kiracısında (SATIS-2 şeridi). |
| 4 mutasyon | ✓ | § 14.6, 5 statik + 1 canlı, hepsi kırmızı |
| 5 canlı | ✓ | § 14.3: 64 ölçüm + kaydırma; § 14.4 tam tur. Tohum API ile: 6 cari 201, 5 ürün, 2 kasa 200, 2 fatura 200. |
| 6 kiracı silindi | ✓ | `PAKETKATMAN-1C-TEST` (01M3C373…): 70 tenant_id tablosunda kalan satır 0, tenants satırı 0, davet satırı silindi (row_count 1, kalan 0). tenants: başta 15, sonda 17. +2 fark başka şeritlerin bu arada açtığı kiracılar: `PAKETSADE-1-TEST Dogrulayici` 14:26, `PAKET-OLCEK-1B-TEST` 14:38 (yerel saat). QA-TEST-* dokunulmadı. API ve vekil `kill`; 5166/5167 `lsof` boş. **Not:** `kiraci-temizle.sh` yanlışlıkla `bash` ile çağrıldı. Betik zsh ister (`${=TABLOLAR}: bad substitution`); yalnız tenants satırını sildi. Kalan satırlar aynı kiracı kimliğiyle SQL'le silindi (FK denetimi oturum içinde kapatılıp açıldı), 0 doğrulandı. |
| 7 yeni kırmızı yok | ✓ | Tam süit `trx-pKATMAN-1c/_Ramazan-MacBook-Air_2026-09-25_14_15_05.trx`: **6201/6201 geçti, 0 başarısız** |

### 14.8 Açık kalanlar (sahibiyle) — tur 3
1. **`#islem-pro-odeme` ("Nakit · Kasa seçilmedi" ödeme katmanı) ekran dışına taşıyor:** 1024/960'ta sağdan 3 px, 375'te 75 px (PRO). Sahibi: **CUMA-S** (`Bilesenler/Fis/FisProOdemeHapi.razor`, `css/tuval-satis.css`). Çözüm: `data-katman="bagli"` + katman.js'e bağlanabilir.
2. **`/islemler K4 tren 2.6×6` (PRO 375/960)** — yeni sayfa düzeni bulgusu. Sahibi: **CUMA-S**.
3. **Tur aracı** (orkestratörün betiği `betikler/tur/ekstra.js`): kapalı `details` içindeki açıcılara tıklıyor. Sonuç: 4 SERBEST-YÜZEN + 12 ekran-dışı yalancı bulgu. Öneri: `vis()`'e `!e.closest('details:not([open])')` ve `e.checkVisibility()` eklensin.
4. **Çekmece öğe yazısı 14 px mi, katman belirteci mi** (tek font-size kabulü): orkestratör kararı. § 13.3'teki gibi, bu turda değişmedi.
5. **Düzenle çekmecesi/tam ekran sayfa kayması:** 9 kayıt, tümü tam-ekran. Sahibi ORTAK-1 (cekmece.js).
6. **Backend gözlemi (bu paketin değil):** `POST /api/v1/urunler` + `acilisStogu`, kiracının ilk kur fotoğrafı yokken **HTTP 400** döndü ("Sayım kaydı için en az bir kur girişi gerekir"). Buna rağmen ürün satırı KALICI yazıldı: aynı kodla ikinci istek 409 "kod zaten kullanımda" verdi, SQL'de 5 ürün var. 400 dönen istek yarım kayıt bırakıyor, işlem atomik değil. Sahibi: Ürün modülü (backend, Taşkın).
7. Tur profil dizinleri `betikler/tur/profil-1` ve `betikler/tur/private` proje ağacında (önceki turdan, sahibi belirsiz). Bu tur tüm profilleri scratchpad'e yazdı. Silme kurucuda (dosya silme yasak).
