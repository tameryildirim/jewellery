# PAKET YILDIZ-5A — uygulama raporu (10.10.2026)

Kapsam: Günlük iş ve Müşteri-stok gruplarında kalan 8 ekranın Yıldız yüzü. Ekranlar Emanet, Sipariş ve tamir, Bildirimler, Ürünler, Ürün ağacı, Etiketler, Fiyat listeleri ve Sayım. Tuvaller `Y_*.dc.html`, koyu ve açık halleriyle, BASİT ve PRO modlarda.

Hasımsız bir paket. Kanıt bu rapordaki ölçümlerdir. Para, veri ve yasal davranışa dokunulmadı: yeni uç, yeni yazma yolu ya da migration yok. Ekrandaki her sayı mevcut uçlardan okunuyor.

Ortam:
- API 5160, vekil 5161, CDP 9712. Tek Chrome profili `/private/tmp/kuyumcu-serit/pYILDIZ-5A/profil`.
- Kiracı `PAKETYILDIZ5A-TEST Tunalılar Kuyumculuk` (01M4KEXJFXWQZVSQ45S4WV9F0B). `demo-tohumla.js --prova-kiraci --sema yildiz` ile kuruldu (6 cari, 12 ürün, 9 satış).
- Tuvaldeki veriye yaklaşmak için ek veri ürünün kendi uçlarıyla girildi (`seed.mjs`):
  - 3 emanet girişi,
  - 4 sipariş ve durum geçişleri,
  - 4 grup + 6 alt grup ve ürünlere atanması,
  - 2 fiyat listesi ve müşteriye bağlanması,
  - vade hatırlatma işinin elle çalıştırılması (1 bildirim),
  - 1 sayım ve okutmalar.
- Ham çıktılar `/private/tmp/kuyumcu-serit/pYILDIZ-5A/`. Kıyas tabloları ve görüntüler `dokumanlar/09-uygulama-kayitlari/ham/yildiz-5a/`.
- Parola ve sistem anahtarı dosyalarda (chmod 600). Değerleri basılmadı.

## 1. Yapılanlar

### 1.1 Üç tuval kalıbı, üç ortak bileşen (`Bilesenler/Yildiz/`)

**`YildizListe.razor` + `YildizListeVerisi.cs` — liste kalıbı (6 ekran).** Tuval `<main>` ile birebir:
- üst satır + 64 px ince başlık, 420×56 alt çizgili arama (girdi 19 px), tek beyaz hap;
- `.sek` sekmeleri (14 px · 0,1em · büyük harf);
- sütun başlığı satırı (10 px dolgu, `.ust` 0,14em);
- kıl çizgili satırlar: ad 19/400, değer 17/300, soluk 17/300;
- sağda `.b3` satır eylemi ve en altta soluk not.

Sütun ızgaraları tuvaldeki `grid-template-columns` değerleridir; satır içi style yok. Bileşen istek atmaz, her eylem sayfanın mevcut geri çağrısıdır. Kilitli eylem disabled çizilir ve sebebini title ile aria'da yazar (UX-S30). Boş durum yalnız başarılı ve gerçekten boş yanıtta çıkar; hata ayrı bantta kalır (UX-S28).

**`YildizEtiketler.razor` — form kalıbı (Y_Etiketler):**
- solda `.k` satırları, "Başka ürün ekle" (`.ekle`), Yazdır (b1) ve Vazgeç (b2);
- sağda 420 px Önizleme.

**`YildizSayim.razor` — özet kalıbı (Y_Sayim):**
- 40 px başlık, 104 px ince rakam ve 52 px soluk ek, 20 px cümle, b1 ve b2;
- sağda alta yaslı fark listesi ve BAŞLANGIÇ · SAYAN · HAS FARKI satırı.

### 1.2 Sayfalara yalnız Yıldız dalı eklendi

Her sayfanın razor dosyasındaki değişiklik:
- `@if (YildizMi) { <Yildiz… /> }`,
- özgün kökün `@if (YildizOzgunCiz) { … }` ile sarılması,
- köke `data-yz-ozgun="@YildizOzgunIsaret"` özniteliği. Değer Yıldız dışında `null` olduğu için öznitelik yazılmaz.

Yıldız kodunun tamamı `Pages/<Sayfa>.Yildiz.cs` kısmi dosyalarında (Cariler.Yildiz.cs deseni). Öteki 8 şemada `YildizMi = false` olduğu için özgün ekran aynen çizilir:
- platin oturumunda 8 rotada `[data-yz-*]`, `.yz` ve `[id^=yildiz-]` sayısı 0 (`platin-dom.out`);
- test Y5A_12 aynı koşulu kilitliyor.

**"Ayrıntı görünümü"** (tuvalde olmayan özellikler silinmedi):
- Liste ekranında "+ Tüm ayrıntılar" (tuval `.ekle` kalıbı, notun sağ ucunda) ya da birincil eylem, özgün ekranı Yıldız'ın kısa başlığı ve "Listeye dön" (b2) altında açar.
- Özgün ekranın kendi başlığı bu görünümde gizlidir.
- Süzgeçler, kampanyalar, reyonlar, iş çatısı, sayım satır düzeltmesi ve ad düzeltmesi burada.

Ekran ekran:

| Ekran | Satır kaynağı | b1 | Satır / .b3 | Ayrıntıda |
|---|---|---|---|---|
| Emanet | GET /cari + müşteri başına GET /emanet/{id} (en çok 10; Bugün'le aynı sınır, sınır notu UX-S29). Girişler maden maden en eskiden düşülür: "Geri verildi" · "Emanette" · "30 günü aştı" | "Emanet al" → özgün müşteri seçici + Emanet al/iade | ad → müşteri ayrıntısı; "Geri ver" → MEVCUT iade çekmecesi (geri verilmişte kilitli + sebep) | geçmiş, belge, geri al |
| Sipariş ve tamir | sayfanın GET /siparisler listesi; sekmeler Açık/Hazır/Teslim edildi istemcide | "Yeni iş" → MEVCUT form | ad → /siparisler/{id}; "Teslim et" yalnız sunucu izin veriyorsa açık (aksi kilitli + sebep) | tür sekmeleri, durum/geciken süzgeci |
| Bildirimler | sayfanın listesi; Hepsi = TUMU, Okunmadı = ACIK (Yıldız'da açılış Hepsi). Nereden = hedef ekranın menü adı | — | ad → MEVCUT okundu (kapat) onayı; "Git" → hedef rota (yoksa kilitli + sebep); okunan satır soluk | PRO: iş çatısı |
| Ürünler | sayfanın listesi + stok adetleri; Fiyat = GET /fiyat/teklifler (tezgâhla aynı salt okuma motoru, 25'lik parça) | "Yeni ürün" → MEVCUT pencere | ad → MEVCUT düzenleme çekmecesi; sekmeler = ağacın kök grupları | süzgeçler, şube stoku, barkod |
| Ürün ağacı | sayfanın ağacı + GET /urunler + /urunler/stok-adetleri toplamı; alt gruplar girintili tek satır | "Yeni grup" → MEVCUT çekmece | kök satır → MEVCUT grup çekmecesi | reyonlar, pasif/aktif |
| Etiketler | sayfanın etkin ürünleri; seçim = MEVCUT Sec; kâğıt özgün `.urun-etiket-sayfa` (ekranda gizli, yazdırmada aynen) | Yazdır → MEVCUT `window.print` | "Başka ürün ekle" → seçim listesi (Ekle/Çıkar) | Tümünü seç, taslak |
| Fiyat listeleri | sayfanın listeleri; Ürün = etkin ürün sayısı; Kullanım = varsayılan ya da bağlı müşteri sayısı (GET /cari) | "Yeni liste" → MEVCUT form | ad → ürünlere etkisi paneli; "Düzenle" → MEVCUT form | kampanyalar, pasife alma |
| Sayım | sayfanın açık sayımı (Yıldız'da açık oturum bir kez okunur); BAŞLANGIÇ = yanıttaki `acildi` | "Sayımı bitir" → MEVCUT Kapat (hareketli satır kararı gerekiyorsa ayrıntıda sorulur) | sağda en çok 4 satır (tuval) + "N ürün daha" | okutma, satır düzeltme, iptal |

### 1.3 Dil ve stil dosyaları

- Sözlük `Kuyumcu.Core/I18n/Sozlukler.Yildiz5A.cs`: 9 sınıf, TR ve EN.
- Ekran adları menüden alınır (`YildizListeBicim.EkranAdi`). "Tüm ekranlar" karesiyle aynı tek kaynaktır (UX-S31).
- `tema-yildiz.css` içinde `/* YILDIZ-5A */` … `/* YILDIZ-5A sonu */` bloğu:
  - her seçici `[data-sema="yildiz"]` kapsamlı, sabit renk yok (Y5A_11 sınıyor);
  - dar ekran kuralları ≤1000 px ve ≤480 px;
  - Sayım için 1001–1360 px kuralı.
- Tarih ve saat `Bicim.GunBicim/IsletmeZamani/Saat` üzerinden (TARIH_TEK ✓). Para `Bicim.TlTam` ile.

## 2. Gerçek (başsız) Chrome ölçümü

### 2.1 Tuval kıyası — `tuval-kiyas.mjs`, 1440×900, son kod

Amblem kararı gereği `navKaynak` kullanıldı (4D'deki gibi). Açık hal `tuvalAcik` ile ölçüldü.

| Ekran (öğe) | BASİT koyu · açık | PRO koyu · açık | FARK gerekçesi |
|---|---|---|---|
| Emanet (39) | 39/39 · 39/39 | 39/39 · 39/39 | — |
| Sipariş ve tamir (28) | 28/28 · 28/28 | 28/28 · 28/28 | — |
| Bildirimler (16) | 16/16 · 16/16 | 16/16 · 16/16 | — |
| Ürün ağacı (26) | 26/26 · 26/26 | 26/26 · 26/26 | — |
| Fiyat listeleri (33) | 33/33 · 33/33 | 33/33 · 33/33 | — |
| Ürünler (38) | 36/38 · 36/38 | 36/38 · 36/38 | **veri:** 12 ürün var, tuvalde 6. `main` boyu 1241 > 828 ve alt notun y'si buna bağlı. Yazı ve renk AYNI |
| Etiketler (28) | 27/28 · 27/28 | 27/28 · 27/28 | **tuval kusuru:** tuvaldeki içerik 900 px'e sığmıyor, `<nav>` 68,9 px'e eziliyor. `main` boyu farklı. Bütün öğeler 3,1 px içinde AYNI |
| Sayım (28) | 27/28 · 27/28 | 27/28 · 27/28 | **veri:** "14 Ayar Zirkon Taşlı Yüzük" 229 px'lik ad sütununda iki satır |

- Etiketler ve Sayım değerleri k7 koşumundan (son kod), öbürleri k4'ten. k4'ten sonra yalnız ≤1360 px kuralları ve Sayım/Etiketler değişti.
- Görüntüler ve tablolar `ham/yildiz-5a/k4-*`, `k7-*`.
- **Kuruş/₺ alt parçası:** bu 8 tuvalde ayrı soluk kuruş öğesi yok. Ürünler "6.922 ₺" yazar (tam lira), uygulama `Bicim.TlTam` ile aynı biçimi verir.

### 2.2 Sıkışma — `sikisma-olc.js` (`olc.mjs`)

Kapsam: 8 rota × 1440/1280/375 × koyu/açık × BASİT/PRO = 96 ölçüm (`olc-BASIT2/PRO2-dar.json`, `olc-ozet2.txt`).

- K1, K3 taşan, K3c, K5 (TRY), K8, K9, K10, K10c, K11, K12 ve yatay: hepsi **0**.
- **K3 kırık:** son kodla 1280'de `/sayim` 2'ydi ("22 Ayar Yarım Altın" iki satır). 1001–1360 px kuralı eklendi; yeniden ölçümde `/sayim` 1280/1440/375 × BASİT/PRO hepsi **0** (`olc-sayim-*`).
- **K7 (12):** `/emanet`, `/siparisler`, `/urunler` 1440/1280. Başlık bloğu ve arama alt kenara yaslı; bu tuvalin kendi hizası. Müşteriler'de 4B'den beri bilinen durumla aynı.

### 2.3 Read ile bakılan görüntüler (3)

1. `k1-emanet-koyu-BASIT-yan-yana.png`: tuvalin ham hali. Satır içindeki `.b3`'ler satırın altında tam genişlik hap olarak çiziliyor (§ 6, belirsiz eşleşme 1).
2. `k6-sayim-koyu-BASIT-yan-yana.png`: tuval ile uygulama aynı düzende (rakam, cümle, liste, bilgi satırı, b1 ve b2).
3. `g/375-dort.png`: 375 px koyu (Emanet, Etiketler, Fiyat, Ürünler). Burada sütun başlığı satırının dar ekranda sarıldığı görüldü. Düzeltildi (sıra kuralı), Y5A_11 bunu kilitliyor ve M5A-09 mutasyonu kırmızı.

### 2.4 Öteki 8 şema — `sema-fark.mjs` (kiracı ve kullanıcı platin; 8 rota × 8 şema × 2 mod)

- **PRO:** 128 karşılaştırma · **FARK 0**.
- **BASİT:** 128 karşılaştırma, ham FARK 10. Yineleme (4 rota, `fark-basit2.out`) FARK 12, ama **küme her koşumda farklı**: kararsız.
- `fark-bak.mjs` incelemesi (`fark-bak.out`). A = bağlı stil dosyası, B = kapalı, C = güncel CSS satır içi, E = 5A ÖNCESİ CSS satır içi:
  - `/sayim` zümrüt koyu ve `/fiyatlandirma` okyanus koyu: A=B=C=E (0 piksel).
  - `/urunler` zümrüt koyu: A↔B 74 piksel, ama B↔C = 0, B↔E = 0 ve **C↔E = 0**.
- Sonuç: 5A eklemesi öteki şemada tek piksel değiştirmiyor. Fark, bağlı stil dosyasını aç/kapa yönteminin kararsız yan etkisi. 4D'de aynı dört şema/tema kalıbıyla görülen durumla aynı. Açık kalan olarak § 7'ye yazıldı.

### 2.5 Yıldız koruması — 4D eşlemeleri, BASİT, bu kiracıyla

| Ekran | Sonuç | FARK |
|---|---|---|
| Tüm ekranlar | 66/67 · 66/67 | yalnız `main` boyu (veri, 4D ile aynı) |
| Tezgâh | 35/36 · 35/36 | "ödeme1 kasa" satır yüksekliği 18,16 (tuval 21,75). Kaynak `wwwroot/css/yasal.css:109` (`line-height: 1.2109`). YASAL-KAPI şeridinin dosyası; 5A'nın değil |
| Müşteriler | 20/31 · 20/31 | satır 3 durum metni bu kiracıda iki satır ("bize borçlu … · emanette 9,16 g has" — veri). Aşağıdaki satırlar 14,5 px kaydı. Yazı, renk ve ölçü AYNI. 5A CSS'i `.yz-musteri` öğelerine dokunmuyor |

## 3. Davranış — her ekranda ana işlem canlı (Yıldız yüzünden, başsız Chrome; `isle.mjs`) + SQL

| Ekran | İşlem | Ekran kanıtı | SQL (`sql-emanet.txt`, `sql-davranis.txt`) |
|---|---|---|---|
| Emanet | Tuna satırı "Geri ver" → iade çekmecesi → Kaydet | satır "Geri verildi" | `EMANET_CIKIS -2.9312 Tuna Akarsu tl 0.00` |
| Bildirimler | satır → okundu notu → Kapat | satır soluk (`yz-liste-sat--soluk`) | `kapatildi` dolu, not "Okundu — 5A canlı doğrulama" |
| Ürün ağacı | Yeni grup "Künye", "Küpe" → Kaydet | bant "Kaydedildi.", satırlar listede | `urun_kategorileri` Küpe, Künye (`ust_id` null, aktif) |
| Fiyat listeleri | Toptan "Düzenle" → kâr %3 → Kaydet → Listeye dön | satır "%4 %3 12 1 toptancı" | `Toptan 3.000 4.00` |
| Ürünler | satır → çekmece → ad "22 Ayar Zincir Kolye 45 cm" → Kaydet | satır yeni ad | `urunler.ad` yeni ad |
| Sipariş ve tamir | Yeni iş → Nermin + "Küpe kilidi tamiri" → Kaydet → Listeye dön | bant "Sipariş oluşturuldu: S-2026-000005", satır listede | `S-2026-000005 TASLAK` |
| Etiketler | Burma + Çeyrek seçildi → Yazdır | bant "Yazdırma penceresi açıldı…", "+1 ürün", kâğıtta 4 etiket (2 × 2), özgün kök ekranda `display:none` | yazma yok (istemci) |
| Sayım | "Sayımı bitir" | bant "1 satır stoğa uygulandı, 0 satır atlandı.", cümle "Sayım kapandı." | `Vitrin sayımı KAPANDI` |

Sipariş "Teslim et": HAZIR satırlarda `/siparisler/{id}` bağlantısı, ötekilerde kilitli ve sebebi "Henüz teslime hazır değil."

## 4. Testler

- **Yeni:** `Kuyumcu.Tests/Yildiz5ATestleri.cs`, 13 test, hepsi yeşil:
  - Y5A_01: liste kalıbı ve her düğmenin gerçek geri çağrısı, kilit sebebi, satır içi style yok.
  - Y5A_02: boş ve hata ayrımı.
  - Y5A_03: ayrıntı görünümü.
  - Y5A_04: emanet FIFO, storno, eşik, maden ayrımı, okuma sırası.
  - Y5A_05: sipariş sekmeleri ve teslim kilidi.
  - Y5A_06: Nereden.
  - Y5A_07: milyem ve ağaç toplamları.
  - Y5A_08: fiyat Kullanım.
  - Y5A_09: sayım sırası, iç span'lı fark hücresi, cümle, 4 satır sınırı.
  - Y5A_10: etiket formu, kilitler, seçim.
  - Y5A_11: CSS ızgaraları tuvalden aynen, kapsam, renk, dar ekran sırası, kâğıt muafiyeti.
  - Y5A_12: 8 sayfanın dalı ve "Yıldız dışında öznitelik yok".
  - Y5A_13: biçimler.
- **Tam süit:** § 4.1.

### 4.1 Tam süit

Süit bir kez, son kodla koşuldu: `dotnet test … --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pYILDIZ-5A`.

Sonuç: **12.386 test · 12.374 geçti · 9 başarısız · 3 atlandı** (11 dk 5 sn). Yildiz5A'nın 13 testi yeşil.

Kırmızıların **hiçbiri 5A'nın değil**. Hiçbiri 5A'nın dosyalarını (8 sayfanın Yıldız dalı, Bilesenler/Yildiz/YildizListe·Etiket·Sayim, 5A CSS bloğu) kullanmıyor:

| Test | Hata | Sahibi |
|---|---|---|
| Yildiz5BTestleri.Y5B_01 | Assert.True | YILDIZ-5B şeridi (aynı anda yazılıyor) |
| Toplu1SafTestleri.TARIH_TEK… | `Pages/EvrakTakvimi.Yildiz.cs:72`, `Pages/KasaRaporu.Yildiz.cs:56` | YILDIZ-5B ve kardeş Yıldız şeritleri |
| YasalKapi2NakitSiniriEkranTestleri.YK3_09 | olay tutucu kayıp (Islemler) | YASAL-KAPI şeridi (t3) |
| SATIS3EkranTestleri.YK3_F01 (BASIT, PRO) | `#islem-kaydet-kilit` yok | YASAL-KAPI / ISLEM-BANT şeridi (Islemler) |
| CumaSAldimSadeTestleri.CUMA_S_02 | zaman aşımı (Islemler) | ISLEM-BANT şeridi (t5) |
| DegisimEkranTestleri.DG_N_PRO… | `#islem-odeme-otomatik` yok | ISLEM-BANT şeridi (t5) |
| RaporDortTestleri.RD_KH01, RD_KH02 | "Rapor dönemi en çok 400 gün olabilir." | rapor dönemi sınırı (5A'nın dışında; sunucu tarafı) |

Bu yüzden "5A yeni kırmızı üretmedi" maddesi (7) sağlanmıştır. Devir notu listesi bana verilmedi; ayrım yukarıdaki sahiplik tablosuyla yapıldı.

**denetim.sh** (`bash denetim.sh`, `denetim1.log`) sonucu: DENETİM BAŞARISIZ, 3 ✗; hiçbiri 5A'nın değil (bkz. § 7 madde 3). 5A'nın tüm sayaçları ✓ (TARIH_TEK satırında 5A dosyası yok; GRID-1 ham tablo 8/8; DIL-EKRAN 0).

## 5. Mutasyon (`mut.sh`: yedek → tek eşleşmeli perl → derle → filtreli test → geri koy → cmp)

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M5A-01 | YildizListe kilitli .b3 `disabled` → `@(false)` | Y5A_01 | AYNI |
| M5A-02 | Emanet "geri verildi" `<= 0m` → `< 0m` | Y5A_04 | AYNI |
| M5A-03 | CSS emanet ızgarası 120px → 130px | Y5A_11 | AYNI |
| M5A-04 | Sipariş "Açık" sekmesi teslim edileni de alır | Y5A_05 | AYNI |
| M5A-05 | Etiket kök işareti Yıldız dışında "kapali" | **ilk koşum: kırmızı yok (TEST AÇIĞI)** → Y5A_12 regex'i `: null;` ile güçlendirildi → M5A-05b: Y5A_12 | AYNI |
| M5A-06 | Sayım eksik fark hücresi İçKötü → Değer | Y5A_09 | AYNI |
| M5A-07 | Fiyat Kullanım "Varsayılan" dalı silindi | Y5A_08 | AYNI |
| M5A-08 | YildizListe ayrıntı modunda da liste çizilir | Y5A_03 | AYNI |
| M5A-09 | Dar ekranda başlık satırı gizleme kuralı silindi | Y5A_11 | AYNI |
| M5A-10 | Ürün ağacı alt ağaç toplamı çocukları atlar | Y5A_07 | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 uyarı, 0 hata. Kayıtlar `mut-M5A-*.log` ve `mut.out`.

## 6. Belirsiz eşleşme (orkestratör kurucuya gösterir)

| # | Tuval | Uygulama | Gerekçe |
|---|---|---|---|
| 1 | Y_Emanet, Y_Siparisvetamir, Y_Bildirimler ve Y_Fiyatlisteleri satırları `<a class="sat">` içinde `<a class="b3">` taşıyor. HTML iç içe `<a>`'ya izin vermiyor, tarayıcı satırı kapatıyor. Tuval görüntüsünde `.b3` satırın ALTINDA tam genişlikte hap olarak çiziliyor (Read 1) | `.b3` satırın son ızgara sütununda (başlık satırındaki boş `<span>` ve 140 px sütun tuvalin niyeti) | Kıyasta bu dört tuvalin kopyası kullanıldı: yalnız satır `<a>` → `<div>` (`/private/tmp/kuyumcu-serit/pYILDIZ-5A/tuval/`); öteki her şey aynı. Kurucu tam genişlik hapı istiyorsa söylesin |
| 2 | Menüde "Bugün" (ya da Sayım'da "Müşteriler") seçili | kabuk bu ekranlarda seçili kelime göstermiyor | tuval kopyası; kabuk bu paketin alanı dışında |
| 3 | Emanet "Ne bıraktı: 22 ayar · 12,5 g · taş 0,5 g" | girişin açıklaması, yoksa maden adı | sunucu girişin ayar/gram/taş bilgisini saklamıyor (yalnız has); uydurulmadı |
| 4 | Emanet satırları "2 Eyl / 28 Eyl" ve "30 günü aştı" | test verisi bugüne tarihli | uçlar geçmiş tarih almıyor; eşik kuralı Y5A_04 ile sınandı |
| 5 | Sipariş "İş: Zincir kaynak" | siparişin açıklaması, yoksa "Müşteri siparişi · S-…" | liste yanıtında kalem adı yok |
| 6 | Sipariş "Teslim et" (.b3) | izinli geçişte ayrıntı/düzenleme ekranına bağlantı | teslim geçişi ayrıntı ekranında; satırdan durum değiştiren yeni yazma yolu açılmadı (davranış değişmez) |
| 7 | Bildirim notu "Okunan bildirim kaybolmaz, soluklaşır." | "Okunan bildirim soluklaşır; 30 gün listede kalır." | sunucu kapalıyı 30 gün gösteriyor; "kaybolmaz" doğru olmazdı |
| 8 | Ürün ağacı notu "…sürükleyerek taşınır." | "Gruba dokununca düzenlemesi açılır; alt grup, üst grubu seçilerek eklenir." | üründe sürükleme yok |
| 9 | Etiket "Etiket boyu: Küçük · 38 × 12 mm" | "50 × 30 mm" | ürünün tek etiket boyu (app.css `.urun-etiket`) |
| 10 | Etiket "Etikette ne yazsın: Ad · ayar · gram · barkod" | "Ad · maden · ayar · gram · has · barkod" | kâğıttaki gerçek içerik |
| 11 | Etiket notu "Etiket yazıcısı: Ayarlar › Yazıcılar." | "Etiket, tarayıcının yazdırma penceresinden basılır." | üründe yazıcı ayarı yok |
| 12 | Etiket önizleme barkodu `▌▌▐▌…` | aynı süs dizisi (aria-hidden) | kâğıttaki barkod gerçek SVG |
| 13 | Fiyat notu "…tezgâhtar değiştirebilir." | "…yoksa varsayılan liste geçer." | satışta liste seçimi yok (yalnız gösterilir) |
| 14 | Fiyat "Ürün 13" (her listede aynı) | etkin ürün sayısı | listeye özel ürün ataması yok, her liste tüm ürünlere uygulanıyor |
| 15 | Fiyat "Kullanım: 2 toptancı" | bağlı müşteri sayısı (toptan segmenti "toptancı") | cari `fiyatListesiId` ve `segment` |
| 16 | Ürünler sekmeleri "Sarrafiye · Bilezik · Gümüş" | ürün ağacının kök grupları | "Gümüş" bir grup değil, maden; tek kaynak ağaç (UX-S31) |
| 17 | Sayım cümlesi "İki ürün…" | sayılar 1–10 yazıyla, sonrası rakamla | — |
| 18 | Sayım "SAYAN Hakan T." | oturumdaki kullanıcı ("Ad S.") | yanıtta sayan kişi adı yok; ekranda sayan, oturumdaki kullanıcıdır |
| 19 | Sayım sağda 4 satır | en çok 4 satır (fark → sayılmayan → eşit) + "N ürün daha" | tam liste ayrıntıda |
| 20 | — (tuvalde yok) | "+ Tüm ayrıntılar" (`.ekle`): Emanet'te yok; Bildirimler'de yalnız PRO | özellik silinmez kuralı; kalıp tuvalin `.ekle`'si |
| 21 | Etiket "Adet: 2 etiket" düz metin | aynı görünüşte girdi (`field-sizing: content`, ok yok) | düzenlenebilirlik (UX-1) |

## 7. Açık kalanlar

1. **8 şema BASİT sema-fark kararsızlığı** (§ 2.4). CSS içeriğinin etkisi 0 kanıtlı. Yöntem yan etkisi 4D'den beri açık. Öneri: `sema-fark.mjs`'de bağlı dosyayı kapatmak yerine satır içi kopyayla kıyas (fark-bak C↔E yöntemi).
2. **Tezgâh "ödeme1 kasa" satır yüksekliği:** `wwwroot/css/yasal.css:109` (YASAL-KAPI şeridi). Tezgâh tuval kıyası 35/36. Sahibine devir.
3. **denetim.sh:**
   - `TOPLU-1 TARIH_TEK` = 1: `Pages/KasaRaporu.Yildiz.cs:56`, YILDIZ-5B şeridi.
   - `GIRDI-2 TEST_GECERSIZ_FIKSTUR` = 1: `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, MASAK doğrulayıcı şeridi.
   - `P-30 DEMO_INSAN_SURESI`: kabulde hariç tutulan madde.
   - 5A'nın sayaçlarının hepsi ✓.
4. **Paralel şerit:** YILDIZ-5B, `YildizListe` tiplerini kullanan `YildizOzet*` dosyalarını aynı dizine yazıyor. Ara derlemeleri iki kez kırdı (onların ara hali). Son derleme 0/0.
5. **Bildirimler** tuvalde 5 satır, test verisinde 1 bildirim (işler elle çalıştırıldı, yalnız vade işi bildirim üretti). Kıyas satır 1 + başlık + sekmeler üzerinden yapıldı.

## 8. DoD sözleşmesi

Yeni varlık ya da kayıt türü yok, yeni uç yok. Bütün yazma yolları (emanet giriş/çıkış/geri al, sipariş oluştur/geçiş, bildirim kapat, ürün/kategori/reyon/fiyat listesi/kampanya oluştur-düzenle-pasif, sayım aç/say/kapat/iptal) MEVCUT ekranlardadır ve Yıldız'da ya doğrudan ya "ayrıntı"dan erişilebilir. Değişmez kayıtlar (sayım satırı, kapalı bildirim) özgün ekranlardaki DegismezIpucu ile kalır. Yıldız'da kilitli her düğme sebebini yazar.

## 9. Değişen dosyalar (5A)

- Yeni:
  - `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/`: YildizListe.razor, YildizListeVerisi.cs, YildizEtiketler.razor, YildizEtiketVerisi.cs, YildizSayim.razor
  - `dotnet/Kuyumcu.Web/Pages/`: Emanet.Yildiz.cs, Siparisler.Yildiz.cs, Bildirimler.Yildiz.cs, Urunler.Yildiz.cs, UrunAgaci.Yildiz.cs, Fiyatlandirma.Yildiz.cs, UrunEtiketleri.Yildiz.cs, Sayim.Yildiz.cs
  - `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yildiz5A.cs`
  - `dotnet/Kuyumcu.Tests/Yildiz5ATestleri.cs`
- Değişen (yalnız Yıldız dalı): `dotnet/Kuyumcu.Web/Pages/{Emanet,Siparisler,Bildirimler,Urunler,UrunAgaci,Fiyatlandirma,UrunEtiketleri,Sayim}.razor`
- Değişen (yalnız `/* YILDIZ-5A */` bloğu): `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`
- Kalıcı araç değişikliği yok. `tuval-kiyas.mjs` olduğu gibi kullanıldı. Eşlemeler ve yardımcılar `/private/tmp/kuyumcu-serit/pYILDIZ-5A/`.

## 10. Kurucu / mali müşavir

Bu paket mevzuat hükmü içermiyor. Yasal eşik ya da oran yazılmadı. Emanet "30 gün" eşiği Bugün ekranının mevcut hatırlatma sabitidir (yasal kural değil).

## 11. Temizlik

- API (PID 33040) ve vekil yalnız kendi PID'lerimle durduruldu. 5160 ve 5161 boş. 5199/5200 dokunulmadı.
- Test kiracısı `zsh kiraci-temizle.sh 01M4KEXJFXWQZVSQ45S4WV9F0B --uygula` ile silindi (rc 0). Kiracıya ait `tenants`, `hareketler`, `cariler`, `urunler` satırları 0.
- `tenants` sayısı başta 22, sonda 25. Fark başka şeritlerin bu sürede açtığı kiracılardır (PAKETYILDIZ5B-TEST, PAKETYASAL-KAPI-3-TEST, PAKET-EKRAN-DENETIM, PAKETYILDIZ-TEZGAH-ODEME-TEST, PAKET-SATINALMA-HAKEM-TEST …). Benim açtığım tek kiracı silindi.
- Proje ağacında geçici dosya yok. Mutasyon yedekleri `/private/tmp/kuyumcu-serit/mut-yedek/pYILDIZ-5A/`, her biri geri konup cmp AYNI.
- COMMIT YOK.

---

# TUR 2 (10.10.2026, 23:00–23:50) — tasarım hakemi KRİTİK bulgularının kapanışı

## T2.1 Bulgular ve yapılanlar

| Bulgu (hakem) | Kök neden | Düzeltme (yalnız Yıldız dalı) |
|---|---|---|
| KRİTİK 1 — Sayım: "Barkod okut" eski ekrana geçiyor; ayrıntı başlığı x=0'da; "← Sayım Oturumları" + "Kapat ve Deftere Yaz" iki birincil | b2 → `yildizAyrinti = true`; `.yz-sayim` ayrıntı başlığının dolgu kuralı yoktu (`.yz-liste .yz-liste-ana` kapsamıydı) | "Barkod okut" artık Yıldız sayım yüzünde açılır: cümlenin altında `.yz-duzen` içinde sayfanın MEVCUT `sy-barkod` (Arama, Enter = Say) ve `sy-adet` alanları. b2 "Say" olur (MEVCUT `Say`, gövde aynı). Hareketli satır kararı (`sy-hareketli-onay`) da aynı yüzde. "Sayımı bitir" doğrudan MEVCUT `Kapat`. "+ N ürün daha" sağdaki listeyi Yıldız kalıbında tüm satırlara açar; özgün ekran yalnız ardından gelen "Tüm ayrıntılar"da. CSS: `.yz-sayim.yz-liste--ayrinti .yz-liste-ana` 48/64/32 (≤1000: 32/24, ≤480: 24/16), `[data-yz-ozgun="sayim"] #sy-listeye-don { display:none }`, `.yz-sayim-okut .arama-kutu { flex: 0 0 auto }` (Arama'nın 12rem esnek tabanı sütunda 192 px boşluk açıyordu). "Sayım başlat" da Yıldız form kalıbında (Ad · Şube tekse salt okunur · Kapsam). |
| KRİTİK 2 — "Emanet al", "Yeni iş", "Yeni liste" eski tema formlarına düşüyor | b1 → `yildizAyrinti = true` (özgün ekran) | Üçü (ve Fiyat "Düzenle") Yıldız form kalıbında açılır: `Bilesenler/Yildiz/YildizForm` (tuval Y_Gidervevirman: üst satır + 64 px başlık, `.k` satırları, "+ Ayrıntı ekle", tek beyaz hap "Kaydet" + altı çizili "Vazgeç", sağda "Not"). Alanlar sayfanın MEVCUT bileşenleri, AYNI kimlik ve AYNI kapılarla (girisForm/HamMetinKabi, formKapisi, listeForm/sayiKapisi); kayıt MEVCUT geri çağrı (GirisKaydet, Olustur, ListeKaydet), gövdeler değişmedi. Özgün form/çekmece bu görünümde çizilmediği için kimlik tek. |

Form içerikleri (zorunlu önce; isteğe bağlı "+ Ayrıntı ekle" arkasında):
- **Emanet al:** Müşteri (`e-cari`, sunucu sınırı arama kutusu ve sınır notu dahil) · Ayar · Gram ağırlık → ek: Taş ağırlığı, Açıklama. Müşteri seçilmeden Kaydet: istek yok, sebep alan altında (`EmanetMusteriSecilmedi`). Not = tuval emanet notu. Kayıt sonrası liste yeniden okunur, bant "Emanet teslim alındı…".
- **Yeni iş:** "Tür" seçim satırı (tuval "Ne yapıyorsun" kalıbı; Müşteri siparişi/Atölye siparişi/Tamir) · Müşteri · Şube (tekse salt okunur, UX-S22) · Serbest tanım (kalem 1) → ek: ürün, ayar, gram, adet, söz günü, açıklama → sonra "Tüm ayrıntılar" (çok kalem, teklif) özgün formda. TAMIR'de emanet uyarısı notun YERİNE (tek yerde).
- **Yeni liste / Düzenle:** "Tür" seçim satırı · Liste · Kâr · İşçilik (tuval sütun adları, UX-S31) · makul aralık onayı (gerekirse) → ek: Varsayılan, Aktif. Kampanyalar ve kullanımdan kaldırma "Tüm ayrıntılar"da.
- Sözlük: `Sozlukler.Yildiz5A.cs` → `YildizForm5ASozlugu` (TR+EN).
- Dar ekran: ≤480 px'te "Tür" seçenekleri satır sarar, kelime kırılmaz (ilk ölçümde 375'te K3 kırık 3 idi → 0).

## T2.2 Gerçek (başsız) Chrome — birincil eylem tıklandıktan SONRA ölçüm (`t2/eylem.mjs`: hakem jar.mjs + sayim-kenar.mjs + sikisma-olc `__olc`)

6 eylem (Emanet al · Yeni iş · Yeni liste · Düzenle · Barkod okut · + N ürün daha) × 1440/375 × koyu/açık × BASİT/PROFESYONEL = 48 ölçüm (`ham/yildiz-5a/eylem-*.out`):

| Ölçü | Tur 1 (hakem) | Tur 2 |
|---|---|---|
| eski tema öğesi (`.btn/.kart/.izgara-sirala/.stat-etiket/.bilgi-kutu/.uyari-bant/.stat-izgara/details.emanet-bilgi`) | Sipariş 13 · Fiyat 16 · Emanet 3 · Sayım 19 | **0 / 48** |
| görünür birincil düğme | Fiyat 2, Sayım 2 | **tam 1 / 48** (formlarda "Kaydet", sayımda "Sayımı bitir") |
| özgün ekran (`[data-yz-ozgun]`) çizili | evet | **0 / 48** |
| h1 sol kenarı (main'e göre) | Sayım 0 | **64 px (1440) · 16 px (375)** — 48/48 |
| K1 · K3 taşan/kırık · K3c · K5 · K7 · K8 · K9 · K10 · K11 · K12 · yatay | — | **hepsi 0 / 48** |

Read ile bakılan görüntüler (tur 2'de 2): `ham/yildiz-5a/t2-formlar-koyu-1440.png` (dört form/okutma koyu 1440 — barkod kutusundaki 192 px boşluk burada görüldü ve düzeltildi) ve `t2-formlar-koyu-375-PRO.png` (375 PRO koyu: dört ekran tuval dilinde, tek beyaz hap).

Tuval kıyası (son kod, BASİT, `tuval-kiyas.mjs`): Emanet 39/39 · Sipariş 28/28 · Fiyat 33/33 · Sayım koyu 27/28 · açık 27/28 (tur 1'deki veri farkı, değişmedi). **Yıldız koruması:** Tüm ekranlar 66/67 · Tezgâh 35/36 · Müşteriler 20/31 — tur 1 tabanıyla birebir aynı (gerekçeler § 2.5; 5A'nın tur 2 CSS'i yalnız `.yz-sayim` ve `#yildiz-siparis-form/#yildiz-fiyat-form` kapsamlı).
Öteki şemalar: platin'de 4 rota + özgün "Yeni" düğmeleri tıklandıktan sonra `[data-yz-ozgun], .yz, [id^=yildiz-], .yz-form` = **0**, özgün form çizili (`t2/platin.out`). Y5A_12 kaynak kilidi güncellendi (dal bloğu, bileşen dalın içinde ve özgün kökten önce).

## T2.3 Canlı davranış — Yıldız formlarından, başsız Chrome + SQL (`t2/canli.out`, `t2/sql-canli.txt`)

| Eylem | Ekran | SQL |
|---|---|---|
| Emanet al → Derya, 22 ayar, 12,5 g → Kaydet | bant "Emanet teslim alındı ve müşterinin emanet defterine işlendi.", form kapandı, liste | `EMANET_GIRIS · Derya Yıldızlı · has 11.45 · tl 0.00` |
| Yeni iş → Tamir, Nermin, "Küpe kilidi tamiri 5A-t2" → Kaydet | bant "Sipariş oluşturuldu: S-2026-000005" | `S-2026-000005 TAMIR TASLAK` · kalem tanımı "Küpe kilidi tamiri 5A-t2" |
| Yeni liste → Özel, "Düğün paketi", kâr 3, işçilik 5 → Kaydet | form kapandı, listede | `fiyat_listeleri: Düğün paketi · OZEL · 3.000 · 5.00 · aktif 1` |
| Sayım → Barkod okut → KLY-14-004, adet 1 → Say | bant "Sayıldı: 14 Ayar Zirkon Taşlı Kolye × 1", özgün ekran yok | `sayim_satirlari KLY-14-004 sistem 3 · sayılan 1 · sayildi dolu` |

## T2.4 Testler ve mutasyon

Yeni `Kuyumcu.Tests/Yildiz5AFormTestleri.cs` (bUnit, sayfa düzeyinde, Yıldız şemasıyla): F01 Emanet (BASİT+PRO) · F02 Yeni iş (BASİT+PRO) · F03 Yeni liste + Düzenle (BASİT+PRO) · F04 Sayım okut/say/+N/bitir (BASİT+PRO) · F05 Sayım başlat · F06 CSS kilidi = 10 test. `Yildiz5A*` filtreli: 23/23 yeşil (P-SAYIM değişikliğinden sonra yeniden koşum: 32/33, tek kırmızı Q95 — aşağıda).

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M5A-11 | Emanet al yine özgün ekranı açar | Y5A_F01 (×2) | AYNI |
| M5A-12 | Yeni iş yine özgün ekranı açar | Y5A_F02 (×2) | AYNI |
| M5A-13 | Fiyat Düzenle yine özgün ekranı açar | Y5A_F03 (×2) | AYNI |
| M5A-14 | Barkod okut yine özgün ekranı açar | Y5A_F04 (×2) | AYNI |
| M5A-15 | Sayımı bitir hareketlide özgün ekrana geçer (eski davranış) | Y5A_F04 (×2) | AYNI |
| M5A-16 | Sayım ayrıntı başlığı yatay dolgusu 64 → 0 | Y5A_F06 | AYNI |
| M5A-17 | Emanet müşteri kapısı silindi | Y5A_F01 (×2) | AYNI |
| M5A-18 | "+ N ürün daha" yine özgün ekranı açar | Y5A_F04 (×2) | AYNI |
| M5A-19 | Fiyat Yıldız formu hiç çizilmez | Y5A_F03 (×2) | AYNI |

Son mutasyondan sonra `dotnet build Kuyumcu.sln`: 0 uyarı · 0 hata (`t2/bsln2.log`, `bsln3.log`).

**Tam süit (tur başına bir kez, `trx-pYILDIZ-5A-t2`):** 12.429 test · 12.419 geçti · 7 başarısız · 3 atlandı. Sahipleri:

| Test | Sahibi / gerekçe |
|---|---|
| DegisimEkranTestleri.DG_N_PRO_… | ISLEM-BANT şeridi (tur 1 listesinde de vardı) |
| RaporDortTestleri.RD_KH01, RD_KH02 | rapor dönemi sınırı (tur 1 listesinde de vardı) |
| Yildiz1Testleri.Y1_10_kural_govdelerinde_sabit_renk_yok | `.yz-tezgah-odeme-govde` kuralı — YILDIZ-TEZGAH-ODEME şeridi (5A bloğu dışında) |
| Q95SayimFormTestleri.Q95_ad_BOS_sube_DOLU_hata_yalniz_ADDA | **P-SAYIM şeridi** (eşzamanlı; `Sayim.razor:411` "ad tarih-saatle OTOMATİK dolar") — boş ad artık oluşmuyor; filtreli yeniden koşumda da kırmızı, kaynağı P-SAYIM'in Ac değişikliği |
| UX1C2EkranTestleri.Sayim_bos_adla_… | P-SAYIM (aynı değişiklik; filtreli yeniden koşumda YEŞİL — kararsız) |
| Yildiz5AFormTestleri.Y5A_F05_… | süit koşarken P-SAYIM `YildizYeni`'yi "tek şubede form sorma" olarak değiştirdi ve bu testi 23:41'de kendisi güncelledi; güncel hâli filtreli koşumda YEŞİL |

→ 5A tur 2'nin ürettiği yeni kırmızı **0**.

**denetim.sh** (`t2/denetim.log`): DENETİM BAŞARISIZ, 2 ✗ — P-30 (kabulde hariç) ve GIRDI-2 TEST_GECERSIZ_FIKSTUR = 1 (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, MASAK doğrulayıcı şeridi; tur 1'de de vardı). 5A sayaçları ✓ (TOPLU-1 TARIH_TEK 0, ModalPencere 48/55, GRID-1 8/8, DIL-EKRAN 0).

## T2.5 Belirsiz eşleşme (tur 2 eki — orkestratör kurucuya gösterir)

| # | Tuval | Uygulama | Gerekçe |
|---|---|---|---|
| 22 | Y_Emanet/Y_Siparisvetamir/Y_Fiyatlisteleri'nde form çizimi YOK | form = tuval Y_Gidervevirman kalıbı (YildizForm, YILDIZ-5B ile ortak) | tuvaldeki en yakın form kalıbı |
| 23 | Y_Sayim: "Barkod okut" altı çizili düğme; okutma alanı çizimi yok | düğme okutma alanını cümlenin altında `.k` kalıbında açar; açıkken b2 "Say" | tuvalin `.k`/`.alan input` kalıbı; eski ekrana geçmemek için |
| 24 | Y_Sayim sağda 4 satır | "+ N ürün daha" → aynı listede tüm satırlar; sonra "+ Tüm ayrıntılar" | özellik silinmez; ikinci adım özgün ekran |
| 25 | Form notları | Sipariş: "Kaydedilen iş Açık sekmesinde görünür; hazır olunca Teslim et açılır." · Sayım başlat: "Okuttukça fark görünür. Sayılmayan ürün sıfırlanmaz." | tuvalde bu formlar yok; cümleler ekranın gerçek davranışından |

## T2.6 Açık kalanlar (tur 2)
1. **P-SAYIM eşzamanlı şeridi** Sayim.razor / Sayim.Yildiz.cs / Yildiz5AFormTestleri.cs (F05) üzerinde çalışıyor (tek şubede "Sayım başlat" formsuz). Q95_ad_BOS kırmızısı onun; dosya sahipliği orkestratörce netleştirilmeli.
2. Sayım "Tüm ayrıntılar" (özgün ekran) hâlâ eski tema (19 öğe, BASİT'te ürün kodları K8 "jargon" sayılıyor, "22 Ayar Künye KNY-22-010" hücresi K3 kırık 1) — hakemin önerdiği gibi yalnız "+ Tüm ayrıntılar" arkasında; öteki şemalardaki özgün ekranla aynı.
3. Tur 1 açık kalanları (§ 7) aynen.

## T2.7 Temizlik
- API (dotnet run PID 50094'ün dinleyicisi 50112) ve vekil (51157) yalnız kendi PID'lerimle durduruldu; 5160/5161 boş. 5199/5200 dokunulmadı.
- Kiracı `PAKETYILDIZ5A-TEST Tunalılar Kuyumculuk` (01M4KQ0FPM0Z3TM6CE55SKWW6S) `zsh kiraci-temizle.sh … --uygula` ile silindi (rc 0); `tenants` başta 23, sonda 23; kiracının `tenants`/`hareketler` satırı 0.
- Mutasyon yedekleri `/private/tmp/kuyumcu-serit/mut-yedek/pYILDIZ-5A/`, hepsi geri konup cmp AYNI. Proje ağacında geçici dosya yok. COMMIT YOK.

## T2.8 Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Web/Pages/{Emanet,Siparisler,Fiyatlandirma,Sayim}.razor` — yalnız `@if (YildizMi)` dalı (form | liste seçimi)
- `dotnet/Kuyumcu.Web/Pages/{Emanet,Siparisler,Fiyatlandirma,Sayim}.Yildiz.cs`
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizSayim.razor` (ChildContent okutma yuvası), `YildizEtiketVerisi.cs` (OkutAcik)
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` — yalnız `/* YILDIZ-5A */` bloğu
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yildiz5A.cs` (YildizForm5ASozlugu)
- `dotnet/Kuyumcu.Tests/Yildiz5AFormTestleri.cs` (yeni), `Yildiz5ATestleri.cs` (Y5A_12 dal kilidi)
- `YildizForm.razor` (YILDIZ-5B) DEĞİŞTİRİLMEDİ, yalnız kullanıldı.

Migration yok; yeni uç/varlık yok; para/veri/yasal davranış değişmedi (gövdeler testle aynı kilitli). Mevzuat hükmü yok.
