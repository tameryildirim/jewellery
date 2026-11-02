# PAKET YILDIZ-AMBLEM-3 — uygulama raporu (10.10.2026, tur 1)

Kaynak: `EMIR-AMBLEM-YILDIZ-2026-10-10.md` (tek doğruluk) · tuval `ham/yildiz-tuval/kaynak/Giris4.dc.html`, `Giris4a.dc.html` (10.10 16:45). Önceki: `PAKET-YILDIZ-AMBLEM-2-uygulama-raporu.md` (kabuk amblemi birebir — KABUĞA DOKUNULMADI). Hasımsız paket: kanıt bu rapordadır. Yalnız görünüm; para/veri/yasal/güvenlik davranışına dokunulmadı. COMMIT YOK.

## 1. Yapılanlar
1. **Hakem KRİTİK-2 — oturumsuz /giris Yıldız (ekip oyu kararı uygulandı).**
   - Mevcut şema çözümleme yolu okundu (VARSAYILMADI): `Program.cs` oturum yokken `/gorunum` okumaz (uç kimlik ister — canlıda oturumsuz `GET /api/v1/gorunum` → **401** ölçüldü); `GorunumServisi.Sema` varsayılanı `platin`; `YildizKabuk.Mi` yalnız bellekteki servis değerine bakar. Bu yüzden soğuk açılışta şema bilinemiyordu.
   - `wwwroot/js/tema.js` (`kuyumcuGorunum` ad alanı, yalnız bu anahtar): sunucudan gelen her şema uygulamasında (`sema()` — oturum açılınca / şema değişince; iki çağıran var: `GorunumServisi`, `KullaniciGorunumServisi`) değer bu cihaza `bygatech-son-sema` olarak **yazılır**; `sonSema()` beyaz listeyle **okur**; dosya sonunda (blazor.webassembly.js'ten önce, ilk boyamadan önce) kök `data-sema` özniteliğine uygulanır. Çıkış yolu anahtara dokunmaz (OturumServisi yalnız jeton anahtarlarını siler) → **çıkışta kalır**. Hiç giriş yapılmamış cihazda anahtar yok → varsayılan şema (Platin).
   - `Pages/Giris.razor`: `YildizMi` = (görünüm bu sekmede sunucudan okunduysa) `YildizKabuk.Mi` — sunucu esastır; (okunmadıysa) cihazdaki son şema == `yildiz`. Son şema `OnInitialized`'da **eşzamanlı** okunur (`IJSInProcessRuntime`) → ilk çizim doğrudan Yıldız düzeni (FOUC yok, canlıda MutationObserver ile ölçüldü § 3c). JS yoksa/okunamazsa null → varsayılan düzen (uydurulmaz).
   - `index.html` gerekmedi (tema.js zaten blazor'dan önce yükleniyor; yeni `<script>` yok).
2. **Hakem KRİTİK-1 — Giris4/Giris4a düzeni BİREBİR.** `Giris.razor` yalnız Yıldız dalında: form satırı tuvaldeki gibi tek satır *Parolamı unuttum (b2) | Giriş yap (b1)* (`#giris-yildiz-satir`); yer tutucular `ad@dukkan.com` / `••••••••`. Tuvalde OLMAYAN üç öğe (sürüm satırı, "Yeni dükkân kur", P-30 · BK-47 "Bu kurulumda HARİÇ olanlar (8)") kart akışından çıkıp tuvalin **alt bilgi satırına** (`#giris-alt`, tuvaldeki "TUNALILAR Kuyumculuk · Merkez Şube" yerinde ve kalıbında: alt 40 px, 12 px, .2em, büyük harf, soluk) taşındı — özellik silinmedi; HARİÇ listesi katlı (`details`) açılır. Böylece amblem + form tuvaldeki gibi dikeyde ortalandı: **1440×900 amblem y = 236** (önce 166).
   - Öteki 8 şema: aynı düğmeler tek tanımdan (RenderFragment) eski sırayla çizilir — DOM sırası, kimlikler, yer tutucular AYNEN (YA3_06 + 32 piksel karşılaştırması).
3. **CSS — `tema-yildiz.css` yeni `/* GIRIS */` bloğu** (AMBLEM bloğundan sonra, `/* YILDIZ-4D */` öncesi; AMBLEM bloğuna dokunulmadı): tuval `.alan` (58 px satır, altı 1 px `--kenarlik` çizgi satırda, renk `--metin-soluk`), `.ust` etiket (86 px · 11 px · 500 · .22em · büyük harf; YILDIZ-TIPO'nun genel etiket kuralını `:not(#_)` ile ezer), girdi (19 px / 300, kenar 0, tarayıcı dolgusu 1px 2px, doğal yükseklik), b2/b1 değerleri, alt bilgi, dil seçici tuval kalıbında (kutusuz, seçili = altı çizili yazı — eski gri kapsül Yıldız girişine sızmıyordu artık), ≤768 px simetrik dolgu + alt bilgi akışta (hata mesajları formu uzatınca binmesin — ölçüldü). Sabit renk yok (hepsi belirteç). Hata/ipucu varken çizgi hücrelere iner, mesaj çizginin altında; hata rengi `--tehlike`.
4. **"Göster"** tuvalde yok; özellik (parola görünürlüğü) korunur → tuval `.ust` kalıbında (11 px, .22em, soluk), kutunun içinde sağda; girdi tuvaldeki gibi satırın tamamı (278 px).
5. **Testler** — `Kuyumcu.Tests/YildizAmblem3Testleri.cs` (YA3_01–YA3_11, 16 koşum).

## 2. Sapmalar / belirsiz eşleşme (gerekçeli)
| # | Konu | Tuval | Uygulama | Gerekçe |
|---|---|---|---|---|
| 1 | Alt bilgi metni | "TUNALILAR Kuyumculuk · Merkez Şube" | "Yeni dükkân kur · BYGATECH 1.0 · BYGATECH · Bu kurulumda HARİÇ olanlar (8)" | Oturum öncesi dükkân adı bilinmez (uydurulmaz — AMBLEM-2 § 2 #7). Tuvalde olmayan üç öğe görev metnindeki "tuval kalıbıyla alt bilgi" seçeneğiyle bu satıra alındı; HARİÇ satırı form akışında artık YOK. Öğeler 44 px dokunma hedefi taşıdığından metin tuvaldekinden 13 px yukarıda (yer "kalıp" bilgisi, tuval-kiyas § 3a) |
| 2 | "Göster" | yok | var (tuval .ust kalıbı) | Parola görünürlüğü özelliği silinmez; piksel farkının TEK kaynağı (§ 3b: bölgedeki fark kutusu yalnız "GÖSTER" yazısı) |
| 3 | Dil seçici | yok | sağ üst köşe, kutusuz yazı | EN kullanıcı için zorunlu; köşede, akış dışı (amblem/form yerini etkilemez) |
| 4 | 375 px | tuvalde telefon hâli yok | form 343 px (16 px kenar), alt bilgi akışta (kartın son öğesi) → amblem y 111 | Tuval kabı sabit 1440 px (375'te form −2,5 px'ten başlıyor). Alt bilgi mutlak kalsaydı boş gönderimde hata mesajlarıyla uzayan form alt bilgiye biniyordu (ölçüldü K9 = 2) → akışa alındı, K9 0 |
| 5 | Giriş öncesi şema öteki şemalarda | — | son şema bordo ise oturumsuz giriş bordo | Ekip oyu kararının doğal sonucu ("giriş öncesi şema = bu cihazda en son kullanılan şema"); öteki 8 şemanın kendi girişi FARK 0 (§ 3d) |
| 6 | `GorunumServisi.cs` belge yorumu "localStorage'a YAZILMAZ" | — | dokunulmadı (dosya alanım dışı) | Karar yalnız GÖRÜNÜM önbelleğini cihaza yazar, doğruluk kaynağı sunucu kalır (YA3_03: sunucu okunduysa önbellek sunucuyu EZMEZ). Yorumun güncellenmesi → orkestratör notu |
| 7 | Hareket azaltma karesi | tuval kuralı taşı gizler | emir § 4.6 (taş 20°, yazı opak) | AMBLEM-2 § 2 #1 ile aynı; referans farklı olduğundan bu kare 20 karenin dışında (§ 3a) |

## 3. Ölçüm — başsız Chrome (betikler/tur/cdp.mjs; API 5164 → vekil 5165; CDP 9606/9607; profiller `/private/tmp/kuyumcu-serit/YILDIZ-AMBLEM-3/profil`, `profil2`)

### 3a. tuval-kiyas (betikler/tur/tuval-kiyas.mjs, eşleme `esleme-{karanlik,aydinlik}.json`) — Giris4 / Giris4a, 1440×900
**Koyu: 14 öğe · tolerans içinde 14 · FARK 0. Açık: 14 öğe · tolerans içinde 14 · FARK 0.** (`tk/karanlik-tablo.md`, `tk/aydinlik-tablo.md`)
| Öğe | Tuval | Uygulama |
|---|---|---|
| amblem (taş + BYGATECH) | 610,236 220×150 | 610,236 220×150 |
| form | 530,442 380×222 | 530,442 380×222 |
| E-POSTA / PAROLA etiketi | 11px/500/.22em/rgb(154,152,147) @530,462.5 86×15.9 / 530,530.5 | aynı |
| alan satırı (çizgi --p) | 530,442 380×58 · çizgi rgb(201,198,190) | aynı |
| e-posta girdisi `ad@dukkan.com` | 19px/300 @632,455.7 278×29.5 | aynı |
| parola girdisi `••••••••` | 19px/300 @632,523.7 278×29.5 | aynı |
| Parolamı unuttum (b2) | 14px/400/.04em @530,616 132.6×40 | aynı |
| Giriş yap (b1) | 16px/500/.02em @760.4,608 149.6×56 | aynı |
| alt bilgi (kalıp) | 12px/400/.2em soluk | aynı (yer: kalıp, bilgi) |
| tuvalde yok: Hoş geldin / sağ panel / eski marka | — | 0 / 0 / 0 |

Yer tutucular (yokla.json, iki tema): tuval `["ad@dukkan.com","••••••••"]` = uygulama `["ad@dukkan.com","••••••••"]`.

### 3b. Giriş bölgesi piksel farkı (`form-kiyas.mjs`, 440×460 bölge: amblem + form, t = 6 sn, form belirmiş)
| Tema | Fark (piksel / 202 400) | Fark kutusu | "Göster" gizlenince |
|---|---|---|---|
| koyu | 273 | x 851–907, y 531–542 (yalnız "GÖSTER" yazısı) | **0** |
| açık | 273 | aynı (yalnız "GÖSTER") | 19 (b1 hap kenarı, stil değişimi sonrası yeniden çizim — `tam` karede aynı bölge FARK 0) |
Yan yana görüntü: `fk-son/form-karanlik-yan-yana.png` (Read ile bakıldı, önceki ölçüm `fk1`).

### 3c. Amblem 5 kare × koyu+açık × kabuk+giriş (AMBLEM-2'nin `amblem2-kiyas.mjs`'i, zaman eşitlenmiş; **evre düzeltmesi YOK** — artık iki tarafta y=236 tam sayı)
| Yüzey | Tema | 0 | 2 | 6 | 9,5 | 14 sn | Hareket azaltma (referans farklı, § 2 #7) |
|---|---|---|---|---|---|---|---|
| Kabuk 188×64 | koyu | 0 | 0 | 0 | 0 | 0 | 6 px, en çok 1/255 |
| Kabuk | açık | 0 | 0 | 0 | 0 | 0 | 361 px, en çok 2/255 |
| Giriş 244×174 | koyu | 0 | 0 | 0 | 0 | 0 | 44 px, en çok 19/255 |
| Giriş | açık | 0 | 0 | 0 | 0 | 0 | 823 px, en çok 33/255 |
**20/20 kare FARK 0** (`kiyas-son/`, son kod). Kutular: kabuk 64,15.5 164×40 iki tarafta; giriş 610,236 220×150 iki tarafta. Süre ölçümü (getAnimations): amb-don 28000 ms linear · amb-parla/amb-yazi 11000 ms · SMIL dur 11s · form 900 ms gecikme 1600 ms. Hareket azaltmada çalışan animasyon 0; taş `matrix3d(0.939693,…,-0.34202…)` = rotateY(20°); yazı/kutu opaklık 1. 3B kutuda süzgeç yok: `.amb-parla` animasyonlu, `.amb-ik/.tas3d/.amb-yuz` filter `none` (4 durum). Azaltma karesindeki kalan fark: tuval referansı süzgeç katmanlı (drop-shadow 0) — görsel olarak aynı (`kiyas/giris-aydinlik-yan-yana.png` Read ile bakıldı).

### 3d. YENİ PROFİL akışı (`akis.mjs`, yeni profil dizini `profil2`, gerçek Chrome)
| Adım | data-sema (DOMContentLoaded) | İlk çizimde #giris-sayfa | Anahtar | Sonuç |
|---|---|---|---|---|
| 1 hiç giriş yok → /giris | null | `giris-sayfa giris-iki-panel` (marka + panel) | yok | **Platin giriş** ✓ |
| 2 Yıldız kiracısında giriş (vekil) | — | — | `yildiz` | kabuk Yıldız |
| 3 çıkış (aynı sekme) | yildiz | `… giris-yildiz` | `yildiz` (KALDI) | Yıldız giriş ✓ |
| 4 /giris yeniden yükle (soğuk) | **yildiz** | `… giris-yildiz` (ilk çizimde — FOUC yok) | `yildiz` | **Yıldız giriş** ✓ |
| 5 about:blank → /giris | yildiz | `… giris-yildiz` | `yildiz` | Yıldız giriş ✓ |
Adım 3–5'te HARİÇ satırı alt bilgide (`#giris-alt #giris-kisit`), yer tutucular `ad@dukkan.com` / `••••••••`.

### 3e. Öteki 8 şema — giriş FARK 0 (`giris-8sema.mjs`)
Cihaz anahtarı = şema (gerçek yol: anahtar → yeniden yükle) × 1440/375 × açık/koyu; tema-yildiz.css açık ↔ kapalı ekran görüntüsü özütü: **32 karşılaştırma · FARK 0 · Yıldız dalı çizilen 0 · data-sema = istenen şema 16/16**. Kabuk/ekran CSS'ine dokunulmadı (yalnız GIRIS bloğu, hepsi `html:root[data-sema="yildiz"] .giris-yildiz` kapsamında — YA3_11).

### 3f. Sıkışma ölçümü (`sikisma-olc.js` `__olcTam`, son kod) — `olc-son/olc.json`
| Rota | Şema | Genişlik | Mod (data-mod) | Tema | Amblem | Form | K1 | K3 | K3c | K5 | K7 | K9 | K10 | K10c | K11 | K12 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /giris | Yıldız | 1440×900 | BASİT, PRO | koyu, açık | 610,236 | 530,442 380×222 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /giris | Yıldız | 1280×800 | BASİT, PRO | koyu, açık | 530,186 | 450,392 380×222 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /giris | Yıldız | 375×812 | BASİT, PRO | koyu, açık | 77.5,111.3 | 16,317.3 343×222 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /giris | Platin | 1440/1280/375 | BASİT, PRO | koyu, açık | — | eski düzen | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
24/24 satır bütün K sayaçları 0 (K2 dar sütun yok — tek sütun; K4 boy 1–1,2 ekran). Boş gönderim (hata hâli, `hata.mjs`): 1440 ve 375'te mesaj "Bu alan zorunludur." çizginin altında, çizgi `--tehlike` rengi, odak ilk hatalı alanda, K9 0 · K3 0 · K7 0 · yatay 0.
Ara ölçümde bulunup düzeltilenler: etiket 12 px/.18em (YILDIZ-TIPO kuralı eziyordu) → 11 px/.22em; `.alan-kontrol` sütun yönlü (girdi 208 px ortalanıyordu) → satır; girdi dolgusu 0 → 1px 2px (2 px yazı kayması); K7 "GÖSTER kutuAlt±7" → kutu düz blok; K3c gizli kısıt kaynağı → kapalı `details` içeriği `display:none`; 375 hata hâlinde alt bilgi binmesi (K9 2) → akışta.
Görüntüler (Read ile bakıldı): `olc-son/yildiz-375-PRO-karanlik.png` (önceki koşum `olc-son`), `olc-son/yildiz-1280-BASIT-aydinlik.png`, `olc-son2/yildiz-375-BASIT-aydinlik.png`, `tk/aydinlik-orta-yan-yana.png` (tuval | uygulama). Hepsi `/private/tmp/kuyumcu-serit/YILDIZ-AMBLEM-3/`.
`yildiz-tipo-tara.mjs` koşulmadı: araç yalnız oturumlu menü ekranlarını tarar (/giris kapsam dışı); giriş tipografisi tuval-kiyas'ta öğe öğe birebir (§ 3a). Not: tuval alt bilgi `.2em` harf aralığı tipo aracının izinli listesinde yok — tuval birebir esas alındı.

## 4. Testler
| Koşum | Toplam | Geçti | Başarısız | Atlanan |
|---|---|---|---|---|
| YildizAmblem3 (YA3_01–11; 02/03 Theory) | 16 | 16 | 0 | 0 |
| YildizAmblem* + Yildiz1 + TasarimBelirtec + Giris* (son CSS ile) | 521 | 521 | 0 | 0 |
| YildizAmblem/Giris/P30/S1e/TasarimBelirtec/Yildiz*/Gorunum/KesifP1/Tema/AuthSifirla/GocD/Mfa/C4 (ara) | 1345 | 1345 | 0 | 0 |
| **Tam süit (bir kez, son kod; trx `/private/tmp/kuyumcu-serit/trx-pYILDIZ-AMBLEM-3`, 5,7 dk)** | **12345** | **12338** | **4** | **3** |
Yeni test: 16 koşum (11 yöntem). Önce (AMBLEM-2 T2 tam süiti): 12275.

**Tam süit kırmızıları — 4, hiçbiri bu paketin ürünü değil:**
- `RaporDortTestleri.RD_KH01…`, `RD_KH02…` — "Rapor dönemi en çok 400 gün" (devir listesinde; RAPOR-4 şeridi).
- `FaturaEkranTestleri.Y4C_K12…(basit: True)` — devir listesinde (kararsız); tek başına yeniden koşum **yeşil**.
- `Yildiz4ATestleri.Y4A_10_Yildiz_BASIT_Panel…` (`#yildiz-bugun-kar` bulunamadı) — Panel/Bugün ekranı (`Bilesenler/Yildiz/*`, t1 YILDIZ-4D/5A şeridi süit sırasında bu dizinde 20:27–20:42 arası yazıyordu; aynı sırada `YildizListeVerisi.cs` derlemeyi 20:28–20:35 arası kırmızıya düşürdü). Tek başına yeniden koşum **yeşil** (`tekrar.log`: 3/3). Giriş/tema.js/GIRIS bloğuyla ilgisi yok.
**Bu paketin ürettiği yeni kırmızı: 0.**

## 5. Mutasyon kanıtı (`mut.zsh`: yedek `/private/tmp/kuyumcu-serit/mut-yedek/YILDIZ-AMBLEM-3/` → perl tek eşleşme → filtreli test → TERS perl ile geri alma (ortak dosyayı kopyayla ezmemek için) → cmp)
| Kod | Değişiklik | Kırmızı test | Geri alındı |
|---|---|---|---|
| M3-1 | tema.js `sema()` son şemayı yazmıyor (setItem/removeItem silindi) | YA3_09_tema_js_son_semayi_yazar_okur_ilk_boyamada_uygular | cmp AYNI |
| M3-2 | tema.js ilk boyamada son şemayı uygulamıyor | YA3_09 | cmp AYNI |
| M3-3 | Giris.razor `YildizMi` son şema yedeği kaldırıldı (`\|\| false`) — KRİTİK-2'nin geri dönüşü | YA3_01, YA3_04, YA3_05, YA3_07 | cmp AYNI |
| M3-4 | Giris.razor `!GorunumOkundu` koşulu kaldırıldı (önbellek sunucuyu ezer) | YA3_03 (sunucu: platin, son: yildiz) | cmp AYNI |
| M3-5 | Yer tutucu `ad@dukkan.com` → `ornek@isletme.com` | YA3_04 | cmp AYNI |
| M3-6 | Yıldız satırında sıra ters (Giriş yap solda) | YA3_04 | cmp AYNI |
| M3-7 | Etiket 11 px → 12 px | YA3_10 | cmp AYNI |
| M3-8 | Alt bilgi `absolute` → `static` (amblem y=236 bozulur) | YA3_10 | cmp AYNI |
| M3-9 | `.alan-kontrol` `flex-direction: row` silindi | YA3_10 | cmp AYNI |
| M3-10 | Parola kutusu `block` → `flex` (K7 hizasızlığı geri gelir) | YA3_10 | cmp AYNI |
| M3-11 | 375'te alt bilgi yeniden `absolute` (hata hâlinde binme) | YA3_10 | cmp AYNI |
11/11 kırmızı. Son mutasyondan sonra üç kaynak yedekle `cmp` AYNI; temiz `dotnet build Kuyumcu.sln`: **0 Hata, 6 Uyarı** — 6 uyarının hepsi başka şeritlerin test dosyalarında (`SUBEKAPSAMASinirTestleri.cs:49`, `KesifP2Testleri.cs:455`, `SonKalanlarTestleri.cs:593/824`, `Yildiz2Testleri.cs:194/234`); bu paketin dosyalarında uyarı 0 (ilk sürümdeki 3 xUnit uyarısı düzeltildi).

## 6. Denetim (`bash dotnet/denetim.sh`)
`DENETİM BAŞARISIZ` — üç ✗, üçü de paket dışı: `P-30 DEMO_INSAN_SURESI` (görevde hariç) · `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1` → `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (MASAK-ESZAMAN şeridi, AMBLEM-2'de de vardı) · `DIL-SUNUCU SUNUCU_CIPLAK_TURKCE_ILETI 3` → `Kuyumcu.Api/Modules/Irsaliye/IrsaliyeModulu.cs:401/405`, `Kuyumcu.Api/Modules/Satinalma/SatinalmaModulu.cs:445` (t5 SATINALMA-EKRAN şeridi, yeni). Bu paketin sayaçları ✓: record 3 (tavan 3) · satır içi style 24 (tavan 196) · index.html satır içi script 0 · ModalPencere 48 (tavan 55) · P-02 0 · P-36b GIRDI_AILESI_TEK_KAYNAK 1 · P-39 FORM_SATIR_HIZA_SAPMASI 0. 314 ✓.

## 7. Canlı doğrulama (API 5164 `--no-build`, vekil 5165)
| Adım | Beklenen | Gerçek |
|---|---|---|
| `demo-tohumla.js --api http://localhost:5164 --prova-kiraci --sema yildiz` (ilk deneme `--api …/api/v1` → 404 `/api/v1/api/v1/sistem/davetler`, adres düzeltildi) | kiracı + veri | `01M4KBR0C3CVHZ62H0QAQEJVHR` "PAKETYILDIZAMBLEM3-TEST Tunalılar Kuyumculuk" · `{"cari":6,"urun":12,"satis":9}` |
| `POST /api/v1/auth/giris` (fikstür chmod 600; parola basılmadı) | 200 + jeton | 200, jeton var, mfaGerekli yok |
| `GET /api/v1/gorunum` (jetonlu) | 200 sema yildiz | `{"sema":"yildiz","yaziOlcegi":"18","temaModu":"sistem"}` |
| `GET /api/v1/kullanici/gorunum` | 200, çözülmüş şema yildiz | `"sema":"yildiz","semaKaynagi":"kiraci"` |
| `GET /api/v1/gorunum` **oturumsuz** | 401 (KRİTİK-2'nin kökü) | 401 `Token bulunamadı` |
| `GET /js/tema.js` / `GET /css/tema-yildiz.css` | 200, kaynakla aynı | 200 · `cmp` AYNI ×2 · `bygatech-son-sema` 2 kez |
| SQL `kiraci_ayarlari` | gorunum_semasi = yildiz | `01M4KBR0C3CVHZ62H0QAQEJVHR · gorunum_semasi · yildiz` · `gorunum_yazi_olcegi · 18` |
| Tarayıcı localStorage (akış § 3d) | giriş sonrası `yildiz`, çıkışta kalır | `yildiz` / `yildiz` |
**Temizlik:** API (PID 76292 + çocuk 76305) ve vekil (PID 76777) yalnız kendi PID'leriyle durduruldu; `lsof` 5164/5165 boş; 5199 dokunulmadı (dinliyor). `zsh kiraci-temizle.sh 01M4KBR0C3CVHZ62H0QAQEJVHR --uygula` → 374 satır + tenants 1 satır silindi; doğrulama: tenants 0 · kullanicilar 0 · kiraci_ayarlari 0. **Tenants: başta 23 (tohumdan önce), tohum sonrası ölçümde 23 (eşzamanlı bir şerit bu arada bir kiracı silmiş), sonda 22** — kendi kiracım silindi; QA-TEST-* ve öteki PAKET* kiracılarına dokunulmadı. Fikstür ve giriş yanıtı `printf ''` ile boşaltıldı.

## 8. DoD sözleşmesi
Yeni varlık/kayıt türü yok; migration gerekmedi. `bygatech-son-sema` cihaz görünüm önbelleğidir (veri değil): sunucu değeri esastır (YA3_03), beyaz liste dışı değer ne yazılır ne uygulanır; oturum/yetki/güvenlik taşımaz. Giriş formu davranışı (doğrulama, alan altı mesaj, odak, MFA ikinci adımı ve C4_62 kurtarma kodu alanı, kayıt sekmesi, parola yardımı) DEĞİŞMEDİ (YA3_07, YA3_08, YA2_16, Giris* filtreli testler yeşil). Her düğme gerçek eylem taşır (Göster/Unuttum/Giriş/Yeni dükkân/HARİÇ özeti).

## 9. Değişen dosyalar (bu paket)
```
 M dotnet/Kuyumcu.Web/Pages/Giris.razor                     (Yıldız dalı satır + alt bilgi; son şema okuma; düğmeler tek tanım)
?? dotnet/Kuyumcu.Web/wwwroot/js/tema.js                     (yalnız kuyumcuGorunum: SON_SEMA / sonSema / ilk boyama)
?? dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css            (yalnız yeni "/* GIRIS */" bloğu)
?? dotnet/Kuyumcu.Tests/YildizAmblem3Testleri.cs             (yeni)
```
(`git status --porcelain | grep -v -E "/(bin|obj)/"` — `index.html` ` M` görünüyor ama bu pakette DEĞİŞMEDİ, önceki şeritlerin.) Dokunulan sayfalar (TEK DOKUNUŞ): `Pages/Giris.razor` — AMBLEM-2'de de açılmıştı; ikinci dokunuş gerekçesi: hakem KRİTİK-1/KRİTİK-2 (aynı amblem dalgasının giriş düzeltmesi, görevin kendisi).

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Giriş 1440 koyu+açık tuval-kiyas FARK 0 (amblem y, form, etiketler, yer tutucular) | KARŞILANDI | § 3a: 14/14 ×2, amblem y 236 = 236 |
| Giriş 375 koyu+açık | KARŞILANDI (tuvalde 375 yok → kalıp + ölçüm) | § 3f: K sayaçları 0, yatay 0; etiket/yer tutucu/yazı aynı; § 2 #4 |
| Amblem 5 kare × koyu+açık (kabuk+giriş) ~0 | KARŞILANDI | § 3c: 20/20 FARK 0, evre düzeltmesiz |
| YENİ PROFİL: giriş → çıkış → yeniden yükle → Yıldız girişi | KARŞILANDI | § 3d adım 2–5 |
| Hiç giriş yapılmamış profil → Platin giriş | KARŞILANDI | § 3d adım 1 |
| FOUC yok | KARŞILANDI | § 3d: ilk çizimde `giris-yildiz`, DOMContentLoaded'da data-sema yildiz |
| Çıkışta anahtar kalır | KARŞILANDI | § 3d adım 3; YA3_09 (yalnız sema() yazar/siler) |
| HARİÇ satırı Yıldız girişinde form akışında yok (özellik silinmedi) | KARŞILANDI | YA3_05; § 3d |
| Yer tutucular tuvaldeki gibi; Göster tuval kalıbında | KARŞILANDI | YA3_04; § 3a; § 2 #2 |
| MFA (C4_62) ve giriş davranışı değişmez | KARŞILANDI | YA3_07, YA2_16, Giris* yeşil |
| Öteki 8 şema giriş FARK 0 | KARŞILANDI | § 3e: 32/32 |
| Kabuğa dokunulmadı | KARŞILANDI | kabuk 10 kare FARK 0; YA3_11 (GIRIS bloğunda .amb/tas3d yok) |
| 3B kutuda süzgeç yok | KARŞILANDI | § 3c filtre none ×4 |
| Mutasyon ≥4 kırmızı, cmp AYNI | KARŞILANDI | § 5: 11/11 |
| build 0 hata | KARŞILANDI (0 Hata; 6 uyarı başka şeritlerin) | § 5 |
| denetim TEMİZ (P-30 hariç) | KISMEN — bu paketin sayaçları ✓; GIRDI-2 ve DIL-SUNUCU başka şeritlerin | § 6 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | § 4: 4 kırmızının sahibi başka şeritler/kararsız |
| Test kiracısı silindi | KARŞILANDI | § 7 |

## 11. Açık kalanlar (sahibiyle)
- `DIL-SUNUCU SUNUCU_CIPLAK_TURKCE_ILETI 3` (Irsaliye/Satinalma modülleri) — t5 SATINALMA-EKRAN şeridi.
- `GIRDI-2 TEST_GECERSIZ_FIKSTUR` — MASAK-ESZAMAN şeridi; `P-30 DEMO_INSAN_SURESI` — kapsam dışı.
- `RD_KH01/02` — RAPOR-4 şeridi; `Y4C_K12`, `Y4A_10` — kararsız (tek başına yeşil; Y4A_10 süit sırasında t1 `Bilesenler/Yildiz/*` yazıyordu).
- 6 derleme uyarısı — başka şeritlerin test dosyaları.
- `GorunumServisi.cs` belge yorumu ("localStorage'a YAZILMAZ") kararla güncellenmeli — dosya alanım dışında; orkestratör.
- AMBLEM-2'nin `≤768 .giris-sol padding-top: 80px` kuralı artık GIRIS bloğunda `main.giris-sol` ile eziliyor (YA2_17 o kuralı kilitlediği için silinmedi); bir sonraki AMBLEM dokunuşunda sadeleştirilebilir.

## 12. Kurucu / mali müşavir
Mevzuat içeriği yok. Kurucuya: oturumsuz giriş ekranı artık bu cihazda en son kullanılan şemayla açılıyor (Yıldız kullanan dükkânın tezgâhında giriş Yıldız). Hiç giriş yapılmamış yeni bir cihazda (kiracı bilinmeden) varsayılan şema görünür — ilk girişten sonra Yıldız'a geçer.
