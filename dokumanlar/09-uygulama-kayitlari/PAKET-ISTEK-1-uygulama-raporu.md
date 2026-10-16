# PAKET ISTEK-1 — İstek sınırı ve tekrar çekilen listeler · Uygulama raporu (tur 2)

**Tarih:** 2026-09-25 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** ISTEK (t1 · test DB `kuyumcu_test_t1` · canlı API 5160 · vekil 5161 · CDP 9705–9733)
**Durum beyanı:** BITTI — paket kapısının 7 maddesi sağlandı (§ 10). Tur 2, doğrulayıcının 6 bulgusunu ve hakem çelişkisini kapattı (§ 0).

---

## 0. Tur 2 — doğrulayıcı bulgularının kapatılması

| Bulgu | Ne yapıldı | Kanıt |
|---|---|---|
| **[ORTA] + [KRİTİK hakem çelişkisi] Israrlı 429'da canlı kur "taze" gösteriliyordu** | Doğrulayıcı haklıydı: tur 1 raporundaki "(2) KARŞILANDI" ve S3 gerekçesi ("kur fotoğrafına bağlanan yazmalar bayat kurla yapılamaz") **yalnız önbellek için** doğruydu; 429 son-iyi yedeği `/kur/guncel`'i de kapsıyordu ve `TazeGetAsync` de yedeğe düşüyordu. Düzeltme iki katlı: (a) `ApiIstemci.GetGovdeAsync` son-iyi yedeğini **yalnız taze olmayan** okumaya verir (`TazeGetAsync` = "Yenile" ve `CanliKurServisi.CekAsync` artık `OranSinirHatasi` alır); (b) `ListeOnbellegi.SonIyiAl` `/kur/guncel*` yolunu **hiçbir zaman** yedekten vermez. | Yeni `ISTEK1_34` (başarılı kur → ısrarlı 429 → `SonHata != null`, son başarılı okuma İLERLEMEZ → eşik dolunca `Bayat == true`, `AlAsync()` 429 fırlatır — yazma yolu bayat kur alamaz) · `ISTEK1_16` · **canlı** ölçüm (§ 6-B): push kanalı kopukken 429 süresince saat `hatali` + ipucu "Son sorgu başarısız …", yoğunluk bitince kendiliğinden düzeldi |
| **[ORTA] Son-iyi yedeği her eski GET gövdesini döndürüyordu, yaşsız** | Yedek artık **alındığı zamanla** saklanır ve yalnız yaşı **Retry-After + 60 sn** içindeyse döner (`ListeOnbellegi.SonIyiEkPayi`). 10:00'da çekilen kasa raporu 10:20'de 429 alırsa eski bakiye DÖNMEZ; sayfa hatayı kendi bandında gösterir. `TazeGetAsync` yedeğe düşmez (yukarıda). | `ISTEK1_15` (60 sn sonra yedek döner; 20 dk sonra `OranSinirHatasi`) |
| **[DÜŞÜK] /kasalar (bakiye) 60 sn önbellekte** | `/kasalar` tazeliği **10 sn** (`ListeOnbellegi.KasaTazelik`); diğer listeler 60 sn. | `ISTEK1_17`; kurucuya not § 14 |
| **[DÜŞÜK] `OranSinirMesaji` çıplak Türkçe** | Metin `UstBarSozlugu.OranSinir(dil, n)` TR+EN'e taşındı; `ApiIstemci.DilKaynagi` Program.cs'te `() => DilServisi.Dil`'e bağlandı. | `ISTEK1_18`; canlı İngilizce ekran (§ 6-B, `yeni-ekran-en.png`): "A little busy right now. Press “Try again” in 10 seconds — nothing was lost." |
| (canlı ölçümde yeni görüldü) saat ipucunda ".." | `UstBarSozlugu.SonSorguHatali` sebebin sonundaki noktayı kırpar. | `ISTEK1_19` (TR+EN); canlı ipucu tek nokta |
| **[DÜŞÜK] Yoğunlukta yeni sayfa ~20 sn halkada** | **Değiştirilmedi (bilinçli).** Halka, spesifikasyonun istediği "Retry-After'a göre bir kez otomatik yeniden dene" beklemesidir; beklerken üst barda sade bant açıktır, liste boşalmaz, "kayıt yok" gösterilmez. Beklemeyi kısaltmak ya 429 fırtınası (bekleme yok) ya da yaşsız yedek (bulgu 2) demekti. Açık kalan olarak yazıldı (§ 11). | 8/8 bant ölçümünde halka + sade bant, kırmızı bant 0 |
| **[DÜŞÜK] denetim.sh P-23 ODEME_KALEMSIZ_YOL** | Bu paketin değil: SQL ile 3/3 hareket `PAKETSATIS-2-TEST Kuyumculuk` kiracısında (SATIS-2). | § 9 |

**Hakem çelişkisinin sonucu (iki rapor aynı gerçeği yazsın):** Tur 1'de (2) koşulu için "KARŞILANDI" yanlıştı — `/kur/guncel` 429'da son-iyi yedeğinden "taze" işaretleniyordu. Tur 2 kodunda bu yol kapalı; doğrulayıcının yoklama testinin karşılığı olan `ISTEK1_34` şimdi yeşil, eski davranışa dönüş mutasyonu (MISTEK-17c) onu **kırmızıya** çeviriyor.

## 1. Özet

| Hedef | Önce | Sonra |
|---|---|---|
| Genel kullanıcı başı sınır (kayan pencere 1 dk / 6 dilim) | 100/dk | **300/dk** (`OranSiniri.GenelDakikaSiniri`) |
| Giriş / yenile / kayıt / MFA / MFA-giriş sınırları | 5/dk · 10/dk · 3/saat · 10/dk · 5/dk | **değişmedi** (canlı: giriş 6. deneme 429, Retry-After 60) |
| Genel sınır reddinde `Retry-After` | gönderilmiyordu | 10 sn (bir dilim) |
| 10 ekran / 30 sn tek kullanıcı — `/api/v1` isteği (doğrudan, push açık; orkestratörün 5199 ölçümüyle aynı düzen) | **67** | **40 · 40** (iki koşum) → **%40,3** |
| Aynı tur, vekil üzerinden (WebSocket yok → kur 10 sn yoklaması) | 67 | 41 → %38,8 (fark: 1 kur yoklaması) |
| Aynı turda 429 | 0 | 0 |
| `/urunler` stok sütunu | satır başına `GET /urunler/{id}/stok` (12 istek) | **tek** `GET /urunler/stok-adetleri?ids=…` |
| 429'da ekran | liste boşalıyor, kart başına kırmızı "Sunucu şu anda çok yoğun (istek sınırı)…" | liste yerinde (≤ Retry-After+60 sn yaşlı veriyle); **tek** sade satır üst barın altında |
| Zil sayısı alınamazsa | "🔔 ?" | "?" yok; rozet görünmez, sebep ekran okuyucuda; 429'da son sayı korunur |
| Saat kırmızı çerçevesi | her geçici hatada | yalnız kur **gerçekten bayatsa** (push kopuk + son başarılı okuma 30 sn'den eski); sebep ipucunda |

## 2. Spesifikasyonun koda karşı doğrulanması ve sapmalar

* Görevdeki "spesifikasyon" yolu (`Kuyumcu.Api/Common/OranSiniri.cs` — "Birleşik koşullar") bir kod dosyasıdır; dosyada bu bölüm **yok**. Bağlayıcı kaynak: görevin EK KOŞULLAR / KAPSAM / KABUL maddeleri.
* "Mevcut durum" doğrulandı: GlobalLimiter 100/dk kayan pencere ✓; tek kullanıcı turu 67 istek ✓. Ek bulgu: en büyük kalem `/urunler/{id}/stok` ×12 (N+1).
* **Filtre AÇMAK zaten istek atmıyordu** (Secici yalnız `acik` durumunu çevirir) — ölçüldü; #45/#46'nın gerçek sebebi sayfalardaki `catch { Liste = []; }` ve yeniden yüklemede tabloyu silen halka idi; düzeltme bunlara yapıldı.

**Sapmalar (gerekçeli):**

* **S1 — Alan dışı YENİ dosya `Kuyumcu.Api/Modules/Urun/UrunStokOzetModulu.cs`** (+ `Kuyumcu.Contracts/Sayfalar/UrunlerSozlesmeleri.cs`'e 2 kayıt): %40 hedefi yalnız önbellekle %19–33'te kalıyordu; 12 isteklik N+1 tek istek oldu. Mevcut `UrunModulu.cs` satırına dokunulmadı. Uç salt OKUMA, `[Authorize]` + `[ModulYetkisi("urun")]`, kiracı süzgeçli, ≤500 kimlik. Geri alma: dosya + `Urunler.razor StokAdetleriniYukle` + 2 Contracts kaydı.
* **S2 — Önbellek geçersiz kılma TUTUCU:** bu istemciden giden HER yazma (başarılı ya da değil) önbelleğin TAMAMINI ve son-iyi yedeğini düşürür (bakiye onlarca uçtan değişir; eksik harita bayat bakiye gösterirdi).
* **S3 — `/kur/guncel` önbelleği:** yalnız canlı kur kanalı (SignalR) BAĞLIYKEN önbellekten verilir; her push'ta ve kanal kopunca düşer. **Tur 2 düzeltmesi:** kur 429'da son-iyi yedeğinden de ASLA verilmez (§ 0).
* **S4 — Oturum olayı önbelleği boşaltır** (ilk kullanıcı ataması hariç).
* **S5 — "Retry-After'a göre bir kez otomatik yeniden dene":** P-07 üstel geri çekilmesi (≤5 sn, 3 deneme) korunur; Retry-After 5–15 sn ise BİR KEZ beklenir (üst bar bandı açık); 15 sn'den uzunsa karar kullanıcıya kalır.
* **S6 — Israrlı 429'da "son iyi yanıt" (tur 2'de daraltıldı):** yol başına son başarılı GET gövdesi zamanıyla saklanır (en fazla 200 yol, oturum kimliğine bağlı, yazmalarda düşer). Yalnız (a) taze olmayan okumada, (b) kur yolu dışında, (c) yaşı Retry-After + 60 sn içindeyse döner; dönerse üst bar sade bandı açılır. Aksi hâlde `OranSinirHatasi` çağırana ulaşır ve sayfa kendi hata bandını gösterir (UX-S28).
* **S7 — Tek uçuş yarışı (mevcut P-07 kusuru, testte yakalandı):** uçuş kaydı sonuçtan ÖNCE siliniyor.
* **S8 — Ürün formunun seçenek kaynakları** formun ilk açılışında yüklenir (UX-S26 korunur); `P10EkranTestleri.Parite_secenekleri_TEK_KAYNAKTAN_gelir` buna göre güncellendi.
* **S9 — Mesajlar sözlükte (tur 2):** sayfa bandı `UstBarSozlugu.OranSinir` (TR+EN), üst bar `UstBarSozlugu.Yogunluk` (TR+EN); "istek sınırı"/"429" jargonu yok.
* **S10 — `/kasalar` tazeliği 10 sn (tur 2):** doğrulayıcı önerisi. Bedeli: 10 ekran turunda `/kasalar` ×2 (İşlemler + Kasalar ekranı 15 sn arayla) — bakiye ekranı taze bakiye göstermeli, bu bilinçli.

## 3. Yapılanlar (dosya dosya)

| Dosya | Değişiklik |
|---|---|
| `Kuyumcu.Api/Common/OranSiniri.cs` | `GenelDakikaSiniri = 300`, `GenelDilimSayisi = 6`, `GenelDilimSaniye = 10`; gerekçe notu (Tamer + Mehmet); `OnRejected` Retry-After yedeği |
| `Kuyumcu.Web/Servisler/ListeOnbellegi.cs` (YENİ) | beyaz liste (60 sn; `/kasalar` **10 sn**; `/kur/guncel` yalnız push bağlıyken), nesil sayacı, oturum kimliği, **yaş sınırlı** son-iyi yedeği (kur hariç), `Etkin` anahtarı |
| `Kuyumcu.Web/Servisler/ApiIstemci.cs` | `Onbellek`, `TazeGetAsync` (**yedeğe düşmez**), yazmalarda düşürme, `Yogun`/`YogunlukDegisti`, tek seferlik Retry-After beklemesi, 429 yedeği çağıran düzeyinde (`GetGovdeAsync`), tek uçuş yarışı düzeltmesi, `DilKaynagi` + sözlükten mesaj |
| `Kuyumcu.Web/Servisler/CanliKurServisi.cs` | push → `PushUygula`, bağlantı durumu önbelleğe bildirilir, `CekAsync` önbelleği atlar, `Bayat` + `BayatEsigi` |
| `Kuyumcu.Web/Layout/UstBar.razor` | tek yoğunluk bandı `#ust-yogunluk-bandi`; zil "?" kaldırıldı; 429'da son sayı; saat `hatali` yalnız `Canli.Bayat` |
| `Kuyumcu.Core/I18n/UstBarSozlugu.cs` | `Yogunluk(dil)`, **`OranSinir(dil, n)`** TR+EN; `SonSorguHatali` çift nokta düzeltmesi |
| `Kuyumcu.Web/Program.cs` | **tur 2:** `ApiIstemci.DilKaynagi = () => dil.Dil` (1 satır + yorum) |
| `Kuyumcu.Web/Pages/Urunler.razor` | hata listeyi silmez, bant tablonun üstünde, halka yalnız liste yokken; toplu stok; form kaynakları tembel |
| `Kuyumcu.Web/Pages/Cariler.razor` | yalnız: hata listeyi silmez + halka yalnız liste yokken |
| `Kuyumcu.Api/Modules/Urun/UrunStokOzetModulu.cs` (YENİ) | `GET /api/v1/urunler/stok-adetleri?ids=` |
| `Kuyumcu.Contracts/Sayfalar/UrunlerSozlesmeleri.cs` | `StokAdetSatiri`, `StokAdetleriYaniti` |
| Testler | `ISTEK1Testleri.cs` (34 test yöntemi / 50 vaka; tur 2'de +6 yöntem: `ISTEK1_15..19`, `ISTEK1_34`); sahte API: `S33UrunlerEtiketlerEkranTestleri.cs`, `UrunEkranTestleri.cs`, `UX1C1EkranTestleri.cs`; `P10EkranTestleri.cs`; `HttpOrtami.cs` (yorum) |

**Dokunulan sayfalar (DoD EK 2):** `Pages/Urunler.razor`, `Pages/Cariler.razor`, `Layout/UstBar.razor` — tur 2'de **hiçbir razor dosyası açılmadı** (yalnız servis + sözlük + Program.cs).

## 4. Migration
Gerekmedi (şema değişikliği yok).

## 5. Canlı doğrulama (tur 2 — API 5160, kiracı `PAKET-ISTEK-1-TEST Kuyumculuk` `01M3C38EBAZAGRMSDNKZ2W709C`, demo-tohumla: 6 cari · 12 ürün · 9 satış)

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/giris` | 200 | 200 |
| 2 | `GET /urunler?limit=50` | 200, 12 | 200, 12 |
| 3 | `GET /urunler/stok-adetleri?ids=<12>,YOKBOYLEBIRURUN` | 200, 12 satır | 200, 12 satır; örnek `{urunId:01M3C38WDT…, toplamAdet:3.0, toplamHasGram:4.809}` |
| 4 | SQL `SELECT urun_id,SUM(adet),SUM(toplam_has_gram) FROM stoklar WHERE tenant_id=… GROUP BY urun_id` | API = SQL | SQL 12 satır · API 12 satır · **12/12 eşit** |
| 5 | Tek uç `GET /urunler/{id}/stok` | aynı toplam | 3.0 = 3.0 |
| 6 | `ids` boş | 400 | 400 "En az bir ürün kimliği verilmelidir (ids)" |
| 7 | 501 kimlik | 400 | 400 "Tek istekte en fazla 500 ürün sorulabilir" |
| 8 | kimliksiz | 401 | 401 |
| 9 | Giriş 6 hatalı deneme | 5×401, 6. 429 | `[401,401,401,401,401,429]`, Retry-After 60 |
| 10 | Genel sınır: 301 × `GET /auth/ben` (75 sn bekledikten sonra) | 1–300 geçer, 301. 429 | 1..299'da 429 = **0** · 300. **200** · 301. **429** · Retry-After **10** · gövde `{message:"Çok fazla istek gönderildi, lütfen biraz bekleyin", error:"TooManyRequests", statusCode:429}` |

Not (Anayasa md.1): ilk koşumda #10 "ilk 300'de 94 × 429" verdi — aynı kullanıcı az önce tohumlama betiğiyle yüzlerce istek atmıştı (pencere doluydu). 75 sn beklenip yeniden koşuldu; tabloya ikinci koşum yazıldı.

**CDP ölçümleri** (başsız Chrome; `scratchpad/istek1/olc.mjs`):

| Ölçüm | İstek | 429 | Not |
|---|---|---|---|
| ÖNCE (tur 1, vekil) | 67 | 0 | stok×12 · subeler×5 · cari×4 · reyonlar×4 · kur×3 … |
| SONRA tur 2, doğrudan 5160 (push açık) — 2 koşum | **40 · 40** | 0 | tekrar edenler: cari×3 (3 ekran 3 farklı sorgu) · urunler×2 (farklı sorgu) · kasalar×2 (10 sn tazelik, bilinçli) · urun-kategorileri×2 (farklı sorgu) |
| SONRA tur 2, vekil (WebSocket yok) | 41 | 0 | +1 = kur 10 sn yoklaması (push yok) |
| 5 paralel sekme, aynı kullanıcı, 60 sn (vekil) | 329 | 28 | kırmızı sayfa bandı **0**; üst bar sade bandı 24 gözlemde; zil "?" **0**; yoğun anda tablosu 0 görünen 24 gözlemin 17'si yükleniyor halkasında, 7'si (`/urunler/agac` 3, `/emanet` 3, `/` 1) tablosuz ekranlar; boş durum sayısı yoğun ve sakin gözlemlerde aynı (ağaç 2 — tohumda kategori/reyon yok; raporlar 1 — CUMA-J katalog notu) → **hata yüzünden "kayıt yok"a dönen ekran 0** |

## 6. Gerçek tarayıcı ölçümü (kurucu kuralı 2026-09-24)

**A — Yoğunluk bandı AÇIKKEN `/urunler`, 8/8 kombinasyon** (`scratchpad/istek1-r2/tur/bant.mjs` — kotayı 340 GET ile doldurur, ekranı yeniden açar; sikisma.js + ekstra.js):

| Genişlik | Mod | Tema | Bant | Kutu | yatay | K1 gizli-görünen | K3 taşan/kırık | TRY | K9 binen | K10b | K11 | K12 | K13 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1280 | PRO | açık | ✓ | 1012×45 (tek satır) | 0 | 0 | 0/0 | 0 | 0 | 1¹ | 0 | 0 | 0 |
| 1280 | PRO | koyu | ✓ | 1012×45 | 0 | 0 | 0/0 | 0 | 0 | 1¹ | 0 | 0 | 0 |
| 1280 | BASİT | açık | ✓ | 1012×45 | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1280 | BASİT | koyu | ✓ | 1012×45 | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 | PRO | açık | ✓ | 375×64 (iki satır, taşma yok) | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 | PRO | koyu | ✓ | 375×64 | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 | BASİT | açık | ✓ | 375×64 | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 375 | BASİT | koyu | ✓ | 375×64 | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 |

¹ PRO 1280 "Tüm kategoriler" seçici metni kesik — KATMAN/Secici, tur 1 ÖNCE görüntüsünde de var. Dar sütun yok (tablo/ana blok ana alanın tamamı).

**B — Israrlı 429'da kur saati ve sayfa hata metni** (`tur/kur-bayat.mjs`, 1280 PRO; canlı kur kanalı `Network.setBlockedURLs */hubs/*` ile KOPUK):

| Dil | t (sn) | Saat sınıfı | İpucu |
|---|---|---|---|
| TR | 0–29 | normal | "Son başarılı sorgu: 14:24:01" |
| TR | 34–39 | normal (eşik dolmadı) | "Son sorgu başarısız (14:24:21): Biraz yoğunluk var. 10 saniye sonra “Yeniden dene”ye basın — bilgileriniz silinmedi. Ekranda son bilinen kur duruyor; …" |
| TR | 44–49 | **`hatali` (kırmızı)** | aynı, 14:24:31 |
| TR | 54+ | normal | "Son başarılı sorgu: 14:24:41" (yoğunluk bitti) |
| EN | 34–39 / 44–49 / 54+ | normal / **`hatali`** / normal | "Last query failed (…): A little busy right now. Press “Try again” in 10 seconds — nothing was lost. The last known rate is still shown; …" |

İlk denemede kanal kopartılmadan ölçüldü: push bağlıyken P-70 gereği saat her aralıkta "son başarılı" ilerledi (kur itilerek geliyor; bayat değil) — doğru davranış; bu yüzden ölçüm kanal kopuk tekrarlandı.
Yeni açılan ekran (son-iyi yedeği yok): TR `/raporlar/vade-takibi` — Retry-After beklemesinde halka + sade üst bant; EN `/raporlar/satis-analizi` — sayfa bandı İngilizce "A little busy right now. Press “Try again” in 10 seconds — nothing was lost." Ölçer: yatay 0 · K1 0 · K3 0 · TRY 0 · K9 0.

**Bakılan ekran görüntüleri (Read ile açıldı, tur 2):**
1. `scratchpad/istek1-r2/tur/kur/saat-hatali-tr.png` — /cariler 1280 PRO koyu: saat kırmızı çerçeve (14:24), üst bar sade amber bant, cari listesi yerinde (7 satır, bakiyeler dolu).
2. `…/tur/kur/saat-hatali-en.png` — /urunler EN: saat kırmızı (14:21), "A little busy right now — this will refresh by itself shortly.", ürün tablosu dolu.
3. `…/tur/kur/yeni-ekran-en.png` — /raporlar/satis-analizi EN: sayfa hata bandı İngilizce (çıplak Türkçe yok).
4. `…/tur/kur/yeni-ekran-tr.png` — /raporlar/vade-takibi: Retry-After beklemesinde halka + sade bant; "kayıt yok" yok.
5. `…/tur/bant/_urunler__PRO_aydinlik_1280.png` — bant tek satır, halka (tek seferlik bekleme), kırmızı bant yok.
6. `…/tur/bant/_urunler__BASIT_karanlik_375.png` — bant iki satır, taşma yok.
7. `…/olcum-sonra-r2/urunler.png` — 12 ürün, stok "3 adet" (toplu uç), bant yok.
8. `…/olcum-sonra-r2/raporlar.png` — 16 rapor kartı + aylık özet kartları dolu, kırmızı bant yok (#0).
9. `…/olcum-paralel-r2/yogun-2-2.png` — 5 sekme yükünde /cariler: sade bant + halka; kırmızı "çok yoğun" bandı yok (#46).

## 7. Testler

* `ISTEK1Testleri.cs`: 34 test yöntemi / **50 vaka** (Theory satırları dahil). Filtreli koşum `FullyQualifiedName~Istek1`: **50/50 yeşil** (tur 1: 32 yöntem).
* Tam süit (`--logger trx --results-directory scratchpad/trx-pISTEK-1-r3`): **6226 toplam · 6224 geçti · 2 başarısız** — ikisi de `SATIS2EkranTestleri` (SATIS-2 şeridinin şu an düzenlediği dosya, son değişiklik 14:40; bu paket nakit uyarısı CSS sınıflarına dokunmaz):
  `SATIS2EkranTestleri.Nakit_uyarisi_esik_BILINMIYORSA_tutucu_GORUNUR`, `SATIS2EkranTestleri.BASIT_nakit_uyarisi_BOS_fiste_ve_esigin_uzaginda_GORSEL_GIZLI_metin_DOMda_kalir`.
* Bir önceki tam koşum (`trx-pISTEK-1-r2`, 6189 toplam) 2 kırmızı: `KurEkranTestleri.Manuel_form_mevcut_degerlerle_acilir` ("7100" ≠ "7.100", kültür sızıntısı) ve `PosTeklifEkranTestleri.Hediye_…` (WaitFor zaman aşımı) — ikisi sınıf başına ayrı koşumda **25/25 yeşil** (kararsız; bu paketin değil), son tam koşumda da yeşil.
* **Bu paketin yeni kırmızı üretmediği (kapı md. 7):** son koşumdaki kırmızıların hiçbiri ISTEK1 dosyalarında ya da bu paketin dokunduğu servis/sayfaları sınayan sınıflarda değil; ISTEK1* 0 kırmızı.

## 8. Mutasyon kanıtı (`perl -0777` tek eşleşme → filtreli test → yedeği geri kopyala → `cmp` birebir)

Tur 1 (yedek `scratchpad/mut-yedek/ISTEK-1/`, 15/15 `cmp` birebir):

| Kod | Değişiklik | Kırmızıya dönen testler |
|---|---|---|
| MISTEK-1 | `TryAl` hep kaçırır (önbellek kapalı) | ISTEK1_01, _02, _03, _06, _07, _25 |
| MISTEK-2 | yazma önbelleği düşürmez | ISTEK1_04 (POST/PATCH/DELETE), _08 |
| MISTEK-3 | son-iyi yedeği yok | ISTEK1_09, _32 |
| MISTEK-4 | Retry-After tek seferlik bekleme kaldırıldı | ISTEK1_11, _12 |
| MISTEK-5 | Urunler catch'i listeyi boşaltır | ISTEK1_20 |
| MISTEK-6 | Cariler catch'i listeyi boşaltır | ISTEK1_21 |
| MISTEK-7 | toplu stok yalnız ilk ürünü sorar | ISTEK1_24, S33_05 |
| MISTEK-8 | zile "?" geri konur | ISTEK1_30 |
| MISTEK-9 | genel sınır 300 → 100 | ISTEK1_40 (HTTP) |
| MISTEK-10 | zilin 429 yakalaması kapatılır | önce 0 kırmızı (TEST AÇIĞI) → ISTEK1_31 güçlendirildi → **ISTEK1_31** |
| MISTEK-11 | kur "yalnız push bağlıyken" kuralı kaldırılır | ISTEK1_07, CariEkran.Tahsilat_KUR_fotografina_baglanir, P07.B57×2 |
| MISTEK-12 | `Bayat` = herhangi bir hata | ISTEK1_33 |
| MISTEK-13 | tek uçuş kaydı sonuçtan sonra silinir | önce 0 kırmızı (TEST AÇIĞI) → ISTEK1_14 eklendi → **ISTEK1_14** |
| MISTEK-14 | toplu stok `Sum` → `Max` | ISTEK1_42 (HTTP) |
| MISTEK-15 | nesil denetimi kaldırılır | ISTEK1_08 |

Tur 2 (yedek `scratchpad/mut-yedek/ISTEK-1-r2/`, 7/7 `cmp` birebir; filtre `FullyQualifiedName~Istek1`, 48–50 vaka):

| Kod | Değişiklik | Kırmızıya dönen testler |
|---|---|---|
| MISTEK-16 | `SonIyiAl` yaş denetimi silinir | **ISTEK1_15** |
| MISTEK-17a | `GetGovdeAsync` yedek koşulundan `!taze` silinir (Yenile yedeğe düşer) | **ISTEK1_16** |
| MISTEK-17b | `SonIyiAl` kur yolu dışlaması silinir | **ISTEK1_16** |
| MISTEK-17c | 17a + 17b birlikte (= tur 1 davranışı, doğrulayıcının yakaladığı hâl) | **ISTEK1_34**, ISTEK1_16 |
| MISTEK-18 | `/kasalar` tazeliği 10 → 60 sn | **ISTEK1_17** |
| MISTEK-19 | `OranSinirMesaji` dili yok sayar ("tr") | **ISTEK1_18** |
| MISTEK-20 | `SonSorguHatali` nokta kırpması silinir | **ISTEK1_19** (tr, en) |
| MISTEK-3r | yedek koşulu `false` (tur 2 kodunda yeniden) | ISTEK1_09, _15, _16, _32 |

(Bir koşum — MISTEK-3r ilk deneme — başka şeridin eşzamanlı derlemesi yüzünden `MSB4018 ConvertDllsToWebCil … being used by another process` ile derlenemedi; yeniden koşuldu, sonuç yukarıda.)
Son mutasyondan sonra değişen dosyalara `touch` + TEMİZ `dotnet build Kuyumcu.sln`: **0 Hata, 0 Uyarı** (`scratchpad/istek1-r2/build4.log`).

## 9. Denetim (`dotnet/denetim.sh`)

* Bu paketin sayaçları: **Sınırsız ToListAsync (Modules · B-8) 145 (tavan 145) ✓**, **P-07 APIISTEMCI_DISI_HTTPCLIENT 0 ✓**, SozlesmeKapsam ✓.
* Genel sonuç: **DENETİM BAŞARISIZ — tek ihlal `P-23: ODEME_KALEMSIZ_YOL 3`** (canlı DB). SQL: 3/3 hareket `PAKETSATIS-2-TEST Kuyumculuk` kiracısında → sahibi SATIS-2. Bu paketin kiracısı silindi; katkısı 0.

## 10. DoD / koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Genel sınır 100→300, giriş/yenile/kayıt/MFA değişmedi, gerekçe yorumda | ✓ | OranSiniri.cs; ISTEK1_40/41; canlı #9 #10 |
| (2) İstemci önbelleği tek serviste, in-flight birleştirme, 60 sn, yazmada geçersiz kılma, kur push'ta ek çekim yok | ✓ (tur 2'de düzeltildi; S2/S3/S6/S10 sapmalı) | ListeOnbellegi.cs; ISTEK1_01–08, _14, _15–17, _34; canlı § 6-B |
| (3) Filtre açmak istek atmaz | ✓ | ISTEK1_22/23 |
| (4) 429'da liste ekranda, tek sade bant, Retry-After'a göre bir kez yeniden dene, UX-S28 | ✓ | ISTEK1_09–12, _15, _20, _21, _32; § 6-A 8/8; paralel ölçüm kırmızı bant 0 |
| (5) Zil "?" yok; saat kırmızısı yalnız bayat kurda, her sayfada aynı (tek bileşen UstBar), sebep ipucunda | ✓ | ISTEK1_30, _31, _33, _34; canlı § 6-B |
| KABUL: 10 ekran/30 sn ≥ %40 azalma, 429 = 0 | ✓ (sınırda) | doğrudan 67 → 40 (%40,3) 2/2 koşum; vekil 41 (%38,8 — WebSocket yok) |
| KABUL: 5 sekme 60 sn → 429 = 0 **ya da** görünen boşalma 0 | ✓ (ikinci kol) | 429 = 28 (329 istek > 300/dk), görünen boşalma 0 |
| KABUL: HTTP 300/dk, giriş 5/dk | ✓ | ISTEK1_40, _41; canlı |
| KABUL: bUnit önbellek/geçersiz kılma/429 liste/zil | ✓ | § 7 |
| KABUL: mutasyonlar | ✓ 15 + 8 | § 8 |
| Gerçek tarayıcı kuralı | ✓ | § 6 |
| Paket kapısı 7 madde | ✓ | build 0/0 · 50/50 · sayaçlarım ✓ · mutasyon · canlı · kiracı silindi · yeni kırmızı 0 |

**DoD sözleşmesi (SÖZLEŞME-DoD):** Yeni varlık/kayıt türü **yok**. Yeni uç `GET /urunler/stok-adetleri` salt okumadır (türetilmiş toplam; düzeltme yolu mevcut stok penceresi/sayım düzeltmesi). Önbellek istemci içi geçici durumdur. Yeni düğme yok; yeni bant role=status bilgi satırıdır.

## 11. Açık kalanlar (sahibiyle)

* Tam süitte başkasına ait 2 kırmızı: `SATIS2EkranTestleri.Nakit_uyarisi_esik_BILINMIYORSA_tutucu_GORUNUR`, `SATIS2EkranTestleri.BASIT_nakit_uyarisi_BOS_fiste_…` (SATIS-2, dosya düzenleniyor). Kararsız (ayrı koşumda yeşil): `KurEkranTestleri.Manuel_form_mevcut_degerlerle_acilir` (kültür sızıntısı "7.100"), `PosTeklifEkranTestleri.Hediye_…` (WaitFor zaman aşımı).
* denetim.sh `P-23 ODEME_KALEMSIZ_YOL 3` → `PAKETSATIS-2-TEST` (SATIS-2).
* **Yoğunlukta yeni açılan ekran Retry-After (10 sn) beklemesi boyunca halkada kalır**; art arda iki istek atan sayfada ~20 sn. Bilinçli (§ 0). Daha kısa bekleme ya sunucuya fırtına ya da yaşsız yedek demek; iyileştirme önerisi: sayfa başına paralel istek (orkestratör/ORTAK-1 değerlendirmesi).
* Yedek süresi dolmuş (> Retry-After+60 sn) ekranlarda 429 artık sayfanın kendi hata bandını gösterir (ör. `satis-analizi` EN görüntüsü) — tasarım gereği; o sayfaların "hata anında eski veriyi tut" davranışı kendi şeritlerinde (CUMA-J Raporlar*, ORTAK-1 SatisAnalizi*/VadeTakibi*).
* %40 hedefi sınırda. Kalan tekrarlar başka şeritlerin ekranlarında: `/cari` ×3 (3 ekran 3 farklı sorgu), `/urunler` ×2 ve `/urun-kategorileri` ×2 (farklı sorgular). Ortak sorgu biçimine geçilirse ~3 istek daha düşer.
* `/urunler` PRO 1280 "Tüm kategoriler" kesik (KATMAN/Secici). `/raporlar` "Rapor Merkezi" katalog yüklenirken boş durum (CUMA-J).
* 5 sekme × aynı kullanıcı × 3 sn'de bir gezinme ~330 istek/dk (>300): 429 olur, ekran boşalmaz; daha fazlası güvenlik kararı (Mehmet).

## 12. Kiracı temizliği

`PAKET-ISTEK-1-TEST Kuyumculuk` (`01M3C38EBAZAGRMSDNKZ2W709C`) `betikler/kiraci-temizle.sh --uygula` ile silindi; `kiraci_davetleri.kullanan_tenant_id` satırı (1) ayrıca silindi. Doğrulama: `tenants WHERE id=…` = **0**; information_schema'daki `tenant_id` kolonlu **70 tablonun hiçbirinde** satır yok. `tenants` sayısı: başlangıçta **16**, sonda **15** — fark başka şeritlerin kendi kiracılarını açıp kapamasından (PAKETSADE-1-TEST ve PAKETKATMAN-1C-TEST kalktı, PAKET-OLCEK-1B-TEST geldi); bu paketin net katkısı 0. QA-TEST-* / DEMO-* kiracılarına dokunulmadı. API (5160) ve vekil (5161) yalnız kendi PID'leriyle durduruldu; `lsof` boş; açık başsız Chrome 0.

## 13. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin dokunduğu)

```
 M dotnet/Kuyumcu.Web/Layout/UstBar.razor
 M dotnet/Kuyumcu.Web/Pages/Cariler.razor
 M dotnet/Kuyumcu.Web/Pages/Urunler.razor
 M dotnet/Kuyumcu.Web/Program.cs                              (tur 2: DilKaynagi bağı)
 M dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs
 M dotnet/Kuyumcu.Web/Servisler/CanliKurServisi.cs
?? dotnet/Kuyumcu.Api/Common/OranSiniri.cs                    (izlenmeyen dosya, değişti)
?? dotnet/Kuyumcu.Api/Modules/Urun/UrunStokOzetModulu.cs      (YENİ)
?? dotnet/Kuyumcu.Core/I18n/UstBarSozlugu.cs                  (izlenmeyen dosya, değişti)
?? dotnet/Kuyumcu.Web/Servisler/ListeOnbellegi.cs             (YENİ)
   dotnet/Kuyumcu.Contracts/Sayfalar/UrunlerSozlesmeleri.cs   (izlenmeyen dizin, değişti)
   dotnet/Kuyumcu.Tests/ISTEK1Testleri.cs                     (YENİ)
   dotnet/Kuyumcu.Tests/S33UrunlerEtiketlerEkranTestleri.cs · UrunEkranTestleri.cs · UX1C1EkranTestleri.cs · P10EkranTestleri.cs · HttpOrtami.cs
```

## 14. Kurucuya / mali müşavire notlar

* Mevzuat hükmü içermez (mali müşavir notu yok).
* Güvenlik kararı: genel sınır 300/dk (kullanıcı başı), kaba kuvvet sınırları aynı. Canlıda 301. istek 429 + Retry-After 10.
* **Kasa bakiyesi:** kasa listesi en fazla **10 sn** önbellekte tutulur. Başka bir tezgâhın satışı bu ekranın önbelleğini düşürmez; yani iki tezgâhlı dükkânda "Kasada ne var?" en fazla 10 sn geride kalabilir (bu tezgâhın kendi yazması anında düşürür). Daha sıkı istenirse sıfıra çekilebilir; bedeli ekran başına bir istek.
* **Yoğunlukta kur:** canlı kur kanalı kopukken ve sunucu yoğunken saat en geç 30 sn'de kırmızıya döner ve sebebini söyler; bayat kur "taze" gösterilmez.
