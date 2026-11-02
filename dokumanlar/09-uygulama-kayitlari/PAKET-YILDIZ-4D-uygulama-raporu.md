# PAKET YILDIZ-4D — uygulama raporu (10.10.2026)

YILDIZ-4C-2'den kalan işler: Müşteriler'de tuvalde olmayan "+ Süzgeç" ve eksik "Toptancı ·" öneki; Tezgâh 375 kusurları (ürün adı 3 satır, "Kart · Ana Kasa" kırık, alt şerit içeriğe biniyor, müşteri seçicide tutar kesik); 128 hâlin tamamı, 8 şema farkı, tam süit.

Hasımsız paket: kanıt bu rapordaki ölçümlerdir. Para, veri ve yasal davranışa dokunulmadı.

Ortam:
- API 5160, vekil 5161, CDP 9711. Tek Chrome profili `/private/tmp/kuyumcu-serit/pYILDIZ-4D/profil`.
- Kiracı `PAKETYILDIZ4D-TEST Tunalılar Kuyumculuk` (01M4JZXCQ1PF8G77CZH0RVYM05). `demo-tohumla.js --prova-kiraci --sema yildiz` ile kuruldu (6 cari, 12 ürün, 9 satış). Sistem anahtarı kendi rastgele değerim (dosya chmod 600; değer basılmadı).
- Kullanıcı "Hakan Tunalı" (`PATCH /kullanicilar/ben` 200). Ölçüm stoğu `POST /islemler/alis` ile girildi (5 ürün × 20, 200).
- Ham çıktılar `/private/tmp/kuyumcu-serit/pYILDIZ-4D/`. Kıyas tabloları ve görüntüler `ham/yildiz-4d/`.

## 1. Yapılanlar

### 1.1 Müşteriler — "+ Süzgeç" kaldırıldı (tuvalde yok)
- `YildizMusteriler.razor`: `#cari-suzgec-daha` düğmesi çizilmiyor. Alt satırda yalnız tuvalin notu kaldı.
- Özellik silinmedi:
  - **Pasif müşteri aramayla bulunur.** Arama yazılınca pasif kartlar da gelir. Satırda "kullanımda değil" yazar, kartından aktife alınır. Arama silinince yalnız aramanın açtığı pasif görünüm kapanır (`Cariler.YildizAramaPasifi`).
  - **Tür ve pasif süzgeci bağlantıdan gelir** (`?tur=`, `?aktif=false`). Bu durumda şerit görünür, bantta "Temizle" vardır (`Cariler.YildizSuzgecEtkin`). Aramanın açtığı pasif görünüm şeridi açmaz.
- Canlı doğrulama (`pasif.mjs`; Nermin `DELETE /cari/{id}` 200 ile pasife alındı):

| Adım | Satırlar | Şerit | Süzgeç düğmesi |
|---|---|---|---|
| Açılış | 5 (Nermin yok) | yok | yok |
| "Nermin" araması | "Nermin Çalışkan 0532 000 01 05 · kullanımda değil 0 ₺" | yok | yok |
| Arama silindi | 5 | yok | yok |
| `/cariler?aktif=false` | 6 (Nermin dahil) | var | yok |

  Sonra Nermin `PATCH {"aktif":true}` ile aktife alındı (200). SQL: 7 carinin hepsi `aktif=1`.

### 1.2 "Toptancı ·" öneki
- Kod öneki zaten yazıyordu: segment TOPTAN ise yazar. TOPTANCI sekmesi de aynı ölçütü kullanıyor.
- Tohumda Anadolu Döküm `tur=TEDARIKCI`, segmenti boş. Tohumun kendi kaydında `rol: 'TOPTANCI'` yazıyor.
- Ürünün kendi ucuyla düzeltildi: `PATCH /cari/{id} {"segment":"TOPTAN"}` 200. SQL: `TEDARIKCI · TOPTAN`.
- Kod bu yüzden değişmedi. Önek ve sekme tek ölçütte kaldı (UX-S31).
- `Y4D_04` öneki kilitliyor. "Belirsiz eşleşme" § 8'e yazıldı.

### 1.3 Tezgâh 375
- **Alt şerit:** 4C'deki `position: fixed` ve tahmini 300 px boşluk kaldırıldı. Şerit akışta yapışkan (sticky). Sağ sütun kutusunu kaldırır (`display: contents`). Böylece şerit kendi yüksekliği kadar yer kaplar ve sayfa sonunda hiçbir içerik altında kalmaz.
  - **PRO'da ek kusur bulundu ve düzeltildi.** Sayfa sınıfı orada `.tuval-pro`. `.ana-icerik{overflow:auto}` yapışkanı kendi kutusuna hapsediyordu. Sonuç: ilk ölçümde PRO 375'te 16 hâlin 15'inde Kaydet ekran dışındaydı.
  - Düzeltme: `≤1000 px` bloğunda `.ana-icerik:has(#yildiz-tezgah) { overflow-x: clip; overflow-y: visible; }`. Yeniden ölçümde 16/16 ekranda.
- **Ürün adı (≤480 px):** kalem satırı `[ayar | ad + ayrıntı]`. Tutar adın sütununda alta iner, sağa yaslı ve tek satırdır. Ad sütunu 94 px'ten 247 px'e çıktı: "22 Ayar Çeyrek Altın" tek satır. "…" ile kırpma yok.
- **"Kart · Ana Kasa":** görüntüde tek satırdı. Ölçerin satır sayımı 20 px "Kart" ile 15 px "· Ana Kasa"nın üst kenarlarını (740,8 / 742,8 px) iki satır sayıyordu.
  - Araç adı kendi `<span class="yz-tezgah-arac">`ına alındı. Metin aynı: "Nakit · Ana Kasa".
  - `tuval-kiyas.mjs` `::metin` ölçümü ilk alt metin düğümüne düşecek şekilde uyarlandı. Eski eşlemeler çalışmaya devam ediyor; karma 11/11.
- **Müşteri seçici (≤480 px):** ad kırpılmıyor. "· borcu 10.491,69 ₺" tek parça; sığmazsa alt satıra iniyor (Read ile görüldü).

### 1.4 Ölçüm araçları (kalıcı, `betikler/tur/tuval-kiyas.mjs`)
- `"onek"`: tuvaldeki sabit sözcük (ör. "Toptancı ·") uygulamada yoksa FARK.
- `"tuvaldeYok"`: tuvalde olmayan öğe uygulamada görünürse FARK (ör. `#cari-suzgec-daha`).
- `"navKaynak"`: ekran tuvalinin menüsü Amblem tuvalinin menüsüyle değiştirilir; seçili öğe metinle eşlenir.
  - Gerekçe: kurucu kararı § 2a (amblem ve ByGatech her ekranda). 4. tur ekran tuvalleri bu karardan önce çizildi.
  - 4C-2'deki "menü x +124 px" farkı böylece kapandı. Kullanıldığı her tabloda başlıkta yazar.
- `::metin`: doğrudan metin düğümü yoksa ilk alt metin düğümü ölçülür.

## 2. Gerçek (başsız) Chrome ölçümü

### 2.1 Tezgâh 128 hâl — `hal.mjs` (son kod; `hal-son-*.out`, özet `ozet.py`)
16 hâl × BASİT/PRO × koyu/açık × 1440/375.

| Kombinasyon | ESKI | Kaydet ekranda | Yatay | K1/K3 taşan/K3 kırık/K5/K10/K10c | K9 (sayfa sonunda) | Görünür binme (375) |
|---|---|---|---|---|---|---|
| B-k-1440 | 0 | 16/16 | 0 | 0/0/0/0/0/0 | 0 | — |
| B-a-1440 | 0 | 16/16 | 0 | 0/0/0/0/0/0 | 0 | — |
| P-k-1440 | 0 | 16/16 | 0 | 0/0/0/0/0/0 | 0 | — |
| P-a-1440 | 0 | 16/16 | 0 | 0/0/0/0/0/0 | 0 | — |
| B-k-375 | 0 | 16/16 | 0 | 0/0/0/0/0/0 | **0** | **0** |
| B-a-375 | 0 | 16/16 | 0 | 0/0/0/0/0/0 | **0** | **0** |
| P-k-375 | 0 | 16/16 | 0 | 0/0/0/0/0/0 | **0** | **0** |
| P-a-375 | 0 | 16/16 | 0 | 0/0/0/0/0/0 | **0** | **0** |

Başlangıç ölçümü (aynı kiracı, eski kod, B-k-375):
- K3 kırık her dolu hâlde 2–5: ürün adları ve "Nakit/Kart · Ana Kasa".
- K10c 2: "· borcu 10.491,69 ₺" kesik.
- Şimdi hepsi 0.

375'te K9'un ölçülmesi:
- **Sayfa sonunda** (en alta kaydırınca) ham ölçer K9 = 0. Şeridin altında kalan içerik 0. Kaydet ekranda: 64/64.
- **Sayfa başında** ham K9 > 0. Sebep: şerit, içeriğin üstünde ekranın dibinde durur. "Kaydet ekranda" kabulü bunu gerektirir; içerik ekrandan uzunsa geometrik örtüşme kaçınılmazdır.
- Bu durum `ORTU` probuyla ayrıştırıldı: her şerit ⟂ içerik çiftinde örtüşme noktasındaki en üst öğe şeridin içinde. Şerit zemini opak (`var(--yuzey-govde)`).
- Görünür binme (iki yazının üst üste görünmesi) 64/64 hâlde **0**.

1440 ham K9 (12–13 adet): müşteri katmanının ve PRO kalem düzenlemesinin `overflow:auto` kırpması. 4C-2'de gerekçelenen durumla aynı (ölçer kırpmayı bilmiyor). Bu paket 1440 yerleşimini değiştirmedi.

### 2.2 Tuval kıyası — 1440×900, BASİT + PRO × koyu + açık (`ham/yildiz-4d/k1-*`, `m1-*`)

| Ekran / hâl | BASİT koyu · açık | PRO koyu · açık |
|---|---|---|
| **Müşteriler** (31 öğe: 28 + Süzgeç yok + tür şeridi yok + "Süzgeç" sözcüğü yok; Toptancı öneki sınanır) | **31/31 · 31/31** | **31/31 · 31/31** |
| Tezgâh (statik, 36) | 36/36 · 36/36 | 36/36 · 36/36 |
| Tüm ekranlar (67) | 66/67 · 66/67 | 66/67 · 66/67 |
| karma 11 · indirim 7 · müşteri alanı 4 · müşteri satırı 5 · hata 1 · başarı 4 · kimlik 5 · Peşin sorusu 4 | hepsi AYNI | hepsi AYNI |
| kalem düzeltme (PRO hâli, 7) | — (BASİT'te yok) | 7/7 · 7/7 |
| Bugün iki satır (7) | ölçülemedi (§ 2.5) | ölçülemedi |

- Müşteriler başlangıç ölçümü (eski kod): FARK 2 (Toptancı öneki yok, "+ Süzgeç" görünür) ve menü x +124 px. Şimdi FARK 0.
- Tüm ekranlarda tek FARK `main` boyu: tuval 1856,5, uygulama 1792,2. Sebep veri. Başka şeridin eklediği satın alma/irsaliye kareleriyle kare sayısı 41/39; 4C-2'de de aynıydı. Kareler, menü ve yazı AYNI.
- Tek bir koşumda `hal-musteri-alan` BASİT için "taşma 82" yazıldı. Aynı eşleme yeniden koşuldu (`k2-*`) ve taşma 0 çıktı. Elle prob da 0 verdi. Kalıcı değil, kararsız bir ölçümdü.
- Süzgeç hâlinin eşlemesi (`hal-suzgec-*`) artık uygulanmaz; o öğe kaldırıldı.

### 2.3 Sıkışma (`sikisma-olc.js`) — `/`, `/islemler`, `/cariler`, `/ekranlar` × 1280/375 × koyu/açık × BASİT/PRO (32 ölçüm; `olc-*-dar.json`, `olc-ozet.txt`)
- K1, K3 taşan, K3 kırık, K5 (TRY), K10, K10c, yatay: hepsi **0**.
- K7 `/cariler` 1280 ±14: 4B'den beri bilinen durum; tuvalle aynı.
- K8 "r-zarar": ölçerin yanlış pozitifi ("Kâr-zarar").
- K9 `/islemler` 375 koyu, boş fiş (3): şerit örtüsü (§ 2.1). Görünür binme 0.

### 2.4 Read ile bakılan görüntüler (4)
1. `g-once/B-k-375-06-karma.png`: başlangıç. Şerit içeriğin üstünde.
2. `g-r2/B-k-375-08b-esik-musteri-acik.png`: müşteri seçicide "· borcu 10.491,69 ₺" tam. Ad kırpılmamış, uzun ad iki satır.
3. `g-kanit/P-a-375-karma-son.png`: PRO açık 375 karma, sayfa sonu. Son öğe ("+ İndirim / − Tek ödeme") şeridin üstünde bitiyor. "Kart · Ana Kasa" tek satır.
4. `ham/yildiz-4d/k1-musteri-acik-PROFESYONEL-yan-yana.png`: tuval | uygulama. Süzgeç yok, "Toptancı · 0532 000 01 06", menü konumları eşleşiyor.

### 2.5 Bugün kıyası — yapılamadı (Anayasa md. 1)
- 4C-2'deki senaryo, iki satırın tarihini SQL ile geriye kaydırıyordu: emanet +31 gün, kasa hareketi dün. Bu UPDATE'ler yalnız kendi test kiracımdaydı, ama izin sınıflandırıcısı reddetti ("Modify Shared Resources").
- Ürün uçları geçmiş tarih almıyor, bu yüzden iki satır üretilemedi. `hal-bugun` 7/7 öğe "BULUNAMADI" (veri yok).
- Bu paket Bugün koduna dokunmadı. Son ölçüm 4C-2'de 7/7 × 4.

## 3. 8 eski şema piksel farkı — `sema-fark.mjs` (kiracı + kullanıcı `platin`'e alındı, sonra `yildiz`e geri)
- **PRO:** 64 karşılaştırma · **FARK 0**.
- **BASİT:** 64 karşılaştırma · ham FARK 4 (iki koşumda aynı): `/cariler` zümrüt koyu, okyanus koyu, zeytin koyu, fildişi açık.
- İnceleme (`fark-bak.mjs`):
  - Fark **tek piksel**: (25, 795), platin kabuğunun yan menüsünde `#kenar-grup-dugme-yonetim`. Bu öğe Müşteriler içeriği değil, 4D'nin dokunmadığı bir kabuk öğesi.
  - 1500 ms beklemede de duruyor; zamanlamadan değil.
  - Aynı sırada sayfa `<link>` stil dosyası kapatılıp tema-yildiz.css'in **güncel** metni satır içi `<style>` olarak eklenince fark **0**. 4D öncesi (ilk yedek) metinle de 0.
  - Yani **CSS içeriği** öteki şemada pikseli değiştirmiyor. Fark, betiğin kullandığı "bağlı stil dosyasını aç/kapat" yönteminin bir yan etkisi.
- Kapsam taraması: tema-yildiz.css'teki her kural seçicisi (parantez duyarlı bölme) `[data-sema="yildiz"]` kapsamında.
- 4C-2'de BASİT 64/64 FARK 0'dı. O tarihten beri bu dosyaya başka şeritler de yazdı (YILDIZ-AMBLEM-2, YILDIZ-TIPO; dosya 17:45'te başkası tarafından değişti). Tek pikselin hangi değişiklikten doğduğu ayrıştırılamadı. Açık kalan 2.

## 4. Testler
- **Yeni:** `Kuyumcu.Tests/Yildiz4DTestleri.cs`. 8 test:
  - `Y4D_01`: Süzgeç yok, tuval kalıbı aynen.
  - `Y4D_02`: şerit yalnız bağlantı süzgecinde.
  - `Y4D_03`: pasif aramayla bulunur.
  - `Y4D_04`: Toptancı öneki ve pasif satırı.
  - `Y4D_05`: yapışkan şerit, 300 px tahmini yok, PRO overflow kuralı.
  - `Y4D_06`: 375 kalem ızgarası.
  - `Y4D_07`: 375 müşteri seçici.
  - `Y4D_08`: ödeme aracı ayrı parça, metin aynı.
- **Uyarlanan:**
  - `Yildiz4BTestleri.Y4B_13` ve `Yildiz4CTestleri.Y4C_14`: Süzgeç düğmesi artık yok.
  - `Y4C_42`: dar ekranda fixed → sticky, `padding-bottom: 0`.

| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| `Yildiz\|TM1Musteri\|Cariler` (t1, ilk) | 262 | 258 | 4 (yeni testlerde medya bulucu hatası + Y4C_42 eski sözleşme → düzeltildi) |
| aynı filtre (t2) | 262 | 262 | 0 |
| Tam süit (son derleme, `trx-pYILDIZ-4D/tam.trx`) | 12226 (12223 koşuldu) | 12221 | 2 |

Tam süitteki 2 kırmızı:
- `RaporDortTestleri.RD_KH01` ve `RD_KH02`: "Rapor dönemi en çok 400 gün olabilir" (RAPOR şeridi). 4C-2'nin devir listesinde aynı ikisi vardı.
- Bu paketin dokunmadığı sunucu kuralı. **Bu paketin ürettiği yeni kırmızı 0.**
- Yıldız testlerinin hepsi yeşil. Süre 7,8 dk, `KUYUMCU_TEST_DB=kuyumcu_test_t1`.

## 5. Mutasyon (`mut.py`; yedek `mut-yedek/pYILDIZ-4D/mut/`; tek eşleşme denetlenir; geri yüklemede copyfile + utime)

| Kod | Mutasyon | Kırmızı test(ler) | Geri alındı |
|---|---|---|---|
| M4D-01 | Süzgeç şeridi koşulsuz çizilir | Y4D_01, Y4D_02 | cmp AYNI |
| M4D-02 | "+ Süzgeç" düğmesi geri eklenir | Y4D_01, Y4B_13, Y4C_14 | cmp AYNI |
| M4D-03 | Aramanın açtığı pasif görünüm şeridi açar | Y4D_02 | cmp AYNI |
| M4D-04 | Arama silinince pasif görünüm kapanmaz | Y4D_03 | cmp AYNI |
| M4D-05 | Toptancı öneki yazılmaz | Y4D_04 | cmp AYNI |
| M4D-06 | Şerit yine `position: fixed` | Y4D_05, Y4C_42 | cmp AYNI |
| M4D-07 | 375 kalem ızgarası yine 3 sütun | Y4D_06 | cmp AYNI |
| M4D-08 | Müşteri adı yine kırpılır | Y4D_07 | cmp AYNI |
| M4D-09 | Araç adı ayrı `<span>` değil | Y4D_08 | cmp AYNI |
| M4D-10 | PRO ana alan dikey taşma kuralı kaldırılır | Y4D_05 | cmp AYNI |

10/10 kırmızı. Son mutasyondan sonra kaynaklara `touch` uygulandı ve `dotnet build Kuyumcu.sln` → **0 Uyarı, 0 Hata** (`build-son.log`).

## 6. Denetim (`bash dotnet/denetim.sh`, `denetim1.log`)
- 315 ✓.
- ✗ yalnız **P-30** (bilinen, kapsam dışı) ve **GIRDI-2 TEST_GECERSIZ_FIKSTUR** (`MASAKESZAMANSinirTestleri.cs:187`, t5 şeridi; 4C-2'de de vardı).
- Bu paketin sayaçları ✓.

## 7. Temizlik
- API (dotnet run 81233 + 81240) ve vekil (56886) kendi PID'leriyle durduruldu. 5160/5161 dinleyen 0. Profilim için çalışan Chrome 0.
- `zsh kiraci-temizle.sh 01M4JZXCQ1PF8G77CZH0RVYM05 --uygula` rc 0 (75 tablo). Sonra SQL'de bu kiracıya ait tenants/cariler/hareketler satırı 0.
- **tenants: başta 22, sonda 23.** Benim kiracım silindi. Artış başka şeritlerin kiracılarından (PAKETHAKEM2-TEST, PAKETYILDIZTIPO-TEST, PAKETYILDIZAMBLEM2-TEST, PAKETHAKEM-TEST); onlara dokunulmadı.
- QA-TEST, 5199/5200/5241 dokunulmadı.

## 8. Belirsiz eşleşme (orkestratör kurucuya gösterir)

| Öğe | Uygulamada | Neden |
|---|---|---|
| Pasif müşteriye erişim | Arama yazılınca pasifler de gelir ("kullanımda değil") | Tuvalde süzgeç yok; özellik silinmez. Tuvalin arama kutusu kalıbı |
| Tür süzgeci | Yalnız bağlantıdan (`?tur=`); etkinse şerit + bantta "Temizle" | Tuvalde tür seçimi yok |
| "Toptancı ·" | Segment TOPTAN carisinde (TOPTANCI sekmesiyle aynı ölçüt) | Tuvaldeki Anadolu tedarikçi + toptancı. Tohum segmenti boş bırakıyor (açık kalan 3) |
| 375 kalem satırı | Ayar · ad/ayrıntı üstte, tutar alt satırda sağda | Tuvalde telefon hâli yok; ad tek satır kuralı |
| 375 müşteri seçici | "· borcu N ₺" sığmazsa alt satırda, tek parça | Aynı |
| 375 alt şerit | Yapışkan, sayfa sonunda içerikten sonra | Kurucu: "içerik alt şeridin yüksekliği kadar boşluk bırakır" |
| Kıyasta menü | Amblem tuvalinin menüsü (`navKaynak`) | § 2a kurucu kararı; ekran tuvalleri ondan önce çizildi |

## 9. DoD sözleşmesi
- Yeni varlık ya da uç yok; yalnız görünüm.
- Cari pasife alma/aktife alma mevcut uçlarla yapılır (kartta). Pasif kart artık Yıldız'da aramayla bulunur.
- Finansal varlıklarda silme yok.

## 10. Dokunulan sayfalar
- `Pages/Cariler.Yildiz.cs` (Yıldız dalı).
  - Gerekçe: "+ Süzgeç" kaldırılınca pasif müşteriye Yıldız'da ulaşma yolu kalmıyordu (özellik silinmez kuralı).
  - Yalnız arama geri çağrısı ve şerit koşulu değişti. Öteki şemalarda bu üyeler çizime girmez.
  - YILDIZ-4B/4C'de de açılmıştı; ikinci dokunuş gerekçesi bu.

## 11. Değişen dosyalar
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizMusteriler.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizMusteriVerisi.cs`
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizTezgah.razor` (araç adı span)
- `dotnet/Kuyumcu.Web/Pages/Cariler.Yildiz.cs`
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`: dar ekran şerit bloğu, Müşteriler alt satırı, dosya sonunda "YILDIZ-4D" bloğu.
- `dotnet/Kuyumcu.Tests/Yildiz4DTestleri.cs` (yeni), `Yildiz4BTestleri.cs` (Y4B_13), `Yildiz4CTestleri.cs` (Y4C_14, Y4C_42)
- `dokumanlar/09-uygulama-kayitlari/betikler/tur/tuval-kiyas.mjs` (onek, tuvaldeYok, navKaynak, ::metin)
- Migration: gerekmedi.
- Not: bu dosyaların hepsi git'te izlenmeyen (`??`) Yıldız dizinlerinde. `git status` satır bazında ayırmıyor.

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Müşteriler tuval-kiyas FARK 0 (Süzgeç yok, Toptancı var) | ✓ | 31/31 × 4 (tur 1 ve tur 2 `k3-musteri-*`) |
| Müşteriler 375 satır hizası (hakem KRİTİK) | ✓ (tur 2) | 4/4 kombinasyonda ad 1 satır, tutar sağ kenarı tek değer (359 px), § 15 |
| Tezgâh 128 hâl ESKI 0 | ✓ | 128/128 (§ 2.1) |
| 375 K3/K9/K10 0 | ✓ (K9: sayfa sonunda 0; sayfa başında yalnız opak şerit örtüsü, görünür binme 0) | § 2.1 |
| 8 şema FARK 0 | ✓ (tur 2) | Tur 2 son kodla: BASİT 64/64 FARK 0, PRO 64/64 FARK 0 (§ 15.4). Tur 1'deki tek piksel yeniden çıkmadı |
| Tam süit 0 yeni kırmızı | ✓ (tur 2) | 12271/12274; RaporDort ×2 (başka şerit) + YayinAcilis 1 (kararsız, tek başına 2/2 yeşil), § 15.5 |
| Mutasyon ≥ 3 | ✓ | 10/10 (tur 1) + 5/5 ve 1 canlı (tur 2) |
| Build 0/0 | ✓ | build-son.log |
| Denetim temiz (P-30 hariç) | ✓ bu paket için | yalnız P-30 + GIRDI-2 (t5) |
| Gerçek Chrome 1280/1440 + 375 · BASİT + PRO · koyu + açık | ✓ | § 2 |
| Test kiracısı silindi | ✓ | § 7 |

## 13. Açık kalanlar
1. **Bugün iki satır kıyası** bu pakette ölçülemedi (§ 2.5). Tarih kaydırma SQL'i izinsiz. Kurucu izin verirse ya da tohum geçmiş tarihli emanet/kasa üretebilirse yeniden koşulur. Bugün koduna dokunulmadı.
2. ~~**8 şema BASİT `/cariler` tek piksel**~~ Tur 2'de kapandı (BASİT 64/64 FARK 0). (kabuk yan menüsü `#kenar-grup-dugme-yonetim`). Satır içi stil ile içerik eşdeğerliği 0. sema-fark.mjs'nin yöntemi ya da başka şeridin değişikliği. Orkestratörün ağaç kapısında yeniden ölçülmeli.
3. **Tohum (`demo-tohumla.js`, alan dışı):** `rol: 'TOPTANCI'` olan cariye `segment: 'TOPTAN'` yazılmıyor. Yazılırsa "Toptancı ·" taze tohumda da çıkar. Öneri: tohum sahibi şeride devir.
4. **Kullanılmayan sözlük girdileri:** `Sozlukler.Yildiz.cs` `DigerSuzgec`/`DigerSuzgecKapat` artık çizilmiyor. Dosya alanım dışında; silinmedi.
5. **(4C-2'den)** Karma tutar girdisi kuruş sıfırsa ",00" göstermiyor (paylaşılan Fiş girdisi). 1440 müşteri katmanı K9 kırpma yanlış pozitifi.
6. **(Başka şerit)** RaporDort `RD_KH01`/`RD_KH02` kırmızı (RAPOR). P-30. GIRDI-2 (t5).
7. **PRO 375 karma satırında "Çıkar" tutarın sağında** (BASİT'te arada). Tuvalde 375 yok, bu paketin kapsamı değil; gözlem.
8. **Olaylar:**
   - Fikstür kurarken bir komutum, silinmiş eski bir test kiracısının (4C-2 r2) fikstüründeki tohum varsayılan parolasını terminal çıktısına bastı (sed deseni eşleşmedi). Gerçek kullanıcı parolası değil; projenin demo tohum değeri. Yine de kayda geçirildi.
   - Çalışma sırasında tema-yildiz.css'e başka şeritler de yazdı (AMBLEM-2, TIPO). Benim bloklarım dışında bir şeye dokunulmadı.

## 14. Kurucu / mali müşavir
Mevzuat hükmü yazılmadı. Eşik eklenmedi.

## 15. Tur 2 — hakem KRİTİK: Müşteriler 375 satır hizası

### 15.1 Bulgu ve kök neden
- Hakem: 375 px'te her `#yildiz-musteri-liste > a` satırı kendi ızgarasını kuruyordu. ≤1000 px kuralı `minmax(0, 1fr) auto` idi.
- Sonuç: ikinci sütun satırın kendi içeriğine göre genişliyordu. "Toptancı · 0532 000 01 06" o sütunu 192 px'e açınca "Anadolu Döküm Toptan Kuyum" 3 satıra kırılıyordu.
- Tutar ikinci satırın **birinci** sütununa düşüyordu (DOM sırası: ad, alt, tutar, durum). Sağ kenarı bu yüzden satırdan satıra kayıyordu: 151/197/202/224 px.
- Kök neden bu paketteki önek değil, 4B'den kalan dar ekran kuralı. Önek yalnız görünür kıldı.

### 15.2 Düzeltme (yalnız Yıldız dalı, `tema-yildiz.css` dosya sonu "YILDIZ-4D tur 2" bloğu, `@media (max-width: 1000px)`)
- **Liste:** ortak sütunlu ızgara, `minmax(0, 1fr) max-content`.
- **Satır:** `grid-template-columns: subgrid`. Sütun genişliği artık satırın içeriğine göre değil, listenin tamamına göre.
- **Yerleşim:**
  - 1. satır: ad, tam genişlik (`grid-column: 1 / -1`).
  - 2. satır: "Toptancı · telefon" solda; tutar sağa yaslı, tek satır (`justify-self: end; white-space: nowrap`).
  - 3. satır: durum, solda.
- **Kapsam:** 375'i de kapsayan ≤1000 px seçildi, çünkü aynı kusur 481–1000 px'te de vardı (760 px ölçümü aşağıda).
- **Değişmeyenler:** 1440 tuval düzeni değişmedi (dört sütun kuralı aynen; `Y4D_09` kilitliyor). Razor ve veri kodu da değişmedi.
- **Dosya bütünlüğü:** önceki 2795 satır birebir korundu (`diff` AYNI). Yalnız sona 12 satır eklendi.

### 15.3 Gerçek başsız Chrome ölçümü — `betikler/tur/musteri-hiza.mjs` (kalıcı; ham `r2/mus-*.out`)
| Kombinasyon | Satır | Ad en çok satır | Tutar tek satır | Tutar sağ kenarı | "Toptancı ·" satırı | Yatay | Süzgeç |
|---|---|---|---|---|---|---|---|
| B-koyu-375 | 6 | **1** | ✓ | **[359]** | Anadolu… 1 satır · önek 1 satır · −3.840 ₺ | 0 | yok |
| B-açık-375 | 6 | **1** | ✓ | **[359]** | aynı | 0 | yok |
| P-koyu-375 | 6 | **1** | ✓ | **[359]** | aynı | 0 | yok |
| P-açık-375 | 6 | **1** | ✓ | **[359]** | aynı | 0 | yok |
| 760 (4 kombinasyon) | 6 | 1 | ✓ | [736] | aynı | 0 | yok |
| 1440 (4 kombinasyon) | 6 | 1 | ✓ | [1112]; ızgara `463/356/180/240` (tuval düzeni) | aynı | 0 | yok |

- **Canlı mutasyon (aynı tarayıcı, B-koyu-375):** yeni blok kapatıldı (`@media (max-width: 1px)`). Sonuç hakemin ölçümünü birebir üretti:
  - ızgara satır başına `134.8/192.2`, `181.4/145.6`, `186.5/140.5`;
  - ad 3 satır;
  - tutar sağ kenarı `[151, 197, 202, 224]`.
  - Geri alındı, `cmp` AYNI.
- `sikisma-olc.js`, `/cariler` × 1280/375 × koyu/açık × BASİT/PRO: K1, K3, K3c, K5, K9, K10, K10c, yatay hepsi 0. K7 1280 = 1 (tur 1'deki bilinen durum).
- Read ile bakılan görüntüler: `ham/yildiz-4d/r2-B-k-375-musteriler.png` ve `r2-P-a-375-musteriler.png`. Ad tek satır, tutarlar aynı sağ kenarda, önek ve durum altta. Dört kombinasyonun görüntüsü de `ham/yildiz-4d/r2-*` altında.

### 15.4 Koruma ölçümleri (tur 2, son kod)
- **Tuval kıyası 1440** (`kiyas.out`, `k3-*`):
  - Müşteriler 31/31 × 4, FARK 0.
  - Tezgâh 36/36 × 4.
  - Tüm ekranlar 66/67 × 4. Tek FARK `main` boyu 1856,5/1792,2 (veri, tur 1 ile aynı).
- **Tezgâh 128 hâl** (`r2/hal-*.out`, `ozet.py`):
  - 8 kombinasyonun hepsinde ESKI 0, Kaydet ekranda 16/16, yatay 0.
  - K1, K3t/K3k, K5, K10, K10c: 0.
  - 375'te sayfa sonu K9 0, görünür örtü 0, şerit altında kalan 0.
- **8 eski şema** (`r2/fark.log`): BASİT 64 karşılaştırma FARK **0**, PRO 64 karşılaştırma FARK **0**.

### 15.5 Testler ve süit
- **Yeni testler:**
  - `Y4D_09_dar_ekranda_musteri_satirlari_ortak_sutunlu_ad_tam_genislik_tutar_ayni_kenarda`: medya ≤1000; liste ızgarası, subgrid, ad/alt/durum/tutar yerleşimi; 1440 dört sütun kuralının korunduğu.
  - `Y4D_10_musteri_satiri_dort_parca_sirasi_aynen`: bUnit, satır parça sırası ve metinleri.
- **Filtreli koşumlar:**
  - `Yildiz|Cariler`: 244/244.
  - `Yildiz4DTestleri`: 10/10.
- **Tam süit** (`trx-pYILDIZ-4D/tam-r2.trx`): 12277 toplam, 12274 koşuldu, 12271 geçti, 3 kaldı.
  - `RaporDortTestleri.RD_KH01` ve `RD_KH02`: RAPOR şeridi. Tur 1 ve 4C-2 devir listesinde de vardı.
  - `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner`: "Unknown database 'kuyumcu_t1_hazir'". Ortam/yardımcı veritabanı yarışı. Tek başına iki kez koşuldu, ikisinde de 10/10 yeşil, yani kararsız. Bu paket yalnız CSS ve test dosyası değiştirdi.
  - **Yeni kalıcı kırmızı 0.**
- `Y4C_44` (bUnit zamanlamalı, 4C): mutasyon koşumlarının ikisinde kırmızı göründü. Mutasyonsuz tek başına 3/3 yeşil; CSS'e bağlı değil (yük altında `WaitForAssertion` süresi). Kayda geçirildi.

### 15.6 Mutasyon (tur 2; `r2/mut.py`, yedek `mut-yedek/pYILDIZ-4D/r2/`, tek eşleşme denetimli)
| Kod | Mutasyon | Kırmızı test | Geri alındı |
|---|---|---|---|
| M4D-11 | Satır `subgrid` yerine kendi ızgarası (`minmax(0, 1fr) auto`) | Y4D_09 | cmp AYNI |
| M4D-12 | Ad tam genişlik değil (`grid-column: 1`) | Y4D_09 | cmp AYNI |
| M4D-13 | Tutar birinci sütunda (`grid-column: 1`) | Y4D_09 | cmp AYNI |
| M4D-14 | Liste ızgara değil (`display: flex`) | Y4D_09 | cmp AYNI |
| M4D-15 | Tutar sağa yaslı değil (`justify-self: end` silinir) | Y4D_09 | cmp AYNI |
| M4D-C1 (canlı) | Yeni blok kapatılır | Tarayıcıda: ad 3 satır, tutar kenarı 4 farklı değer | cmp AYNI |

Son mutasyondan sonra `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata** (`r2/build-son.log`).

### 15.7 Denetim, canlı veri, temizlik
- **Denetim** (`r2/denetim.log`): 315 ✓. ✗ yalnız P-30 ve GIRDI-2 `TEST_GECERSIZ_FIKSTUR` (`MASAKESZAMANSinirTestleri.cs:187`, t5 şeridi); ikisi tur 1 ile aynı. Bu paketin sayaçları ✓.
- **Kiracı:** `PAKETYILDIZ4D-TEST Tunalılar Kuyumculuk` (01M4K8AKJ916NPVTM7TES5KYAD), `demo-tohumla.js --prova-kiraci --sema yildiz` ile kuruldu.
  - Anadolu Döküm: `PATCH /cari/{id} {"segment":"TOPTAN"}` 200.
  - SQL: 7 cari; Anadolu `TEDARIKCI · TOPTAN · aktif=1`. Peşin Müşteri listede gösterilmez, bu yüzden ekranda 6 satır.
- **Durdurma:** API (47738 + 47766) ve vekil (53778) kendi PID'leriyle durduruldu. 5160/5161'de dinleyen 0.
- **Temizlik:** `zsh kiraci-temizle.sh 01M4K8AKJ916NPVTM7TES5KYAD --uygula` rc 0. SQL'de bu kiracıya ait tenants/cariler/hareketler 0.
- **tenants: başta 23, sonda 23.**
- QA-TEST, 5199/5200/5241 dokunulmadı.

### 15.8 Belirsiz eşleşme (tur 2 eki)
| Öğe | Uygulamada | Neden |
|---|---|---|
| Müşteriler ≤1000 px satırı | Ad tam genişlik; altında "Toptancı · telefon" ile sağda tutar; en altta durum | Tuvalde telefon hâli yok. Hakemin istediği düzen (ad ilk satır, önek ve durum altta, tutar aynı sağ kenarda). Durum, tutarla aynı satıra sığmadığı için üçüncü satırda |

### 15.9 Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`: dosya sonuna "YILDIZ-4D tur 2" bloğu (12 satır).
- `dotnet/Kuyumcu.Tests/Yildiz4DTestleri.cs`: Y4D_09, Y4D_10.
- `dokumanlar/09-uygulama-kayitlari/betikler/tur/musteri-hiza.mjs` (yeni, kalıcı ölçüm betiği).
- `git status --porcelain`: üçü de `??` (izlenmeyen Yıldız dosyaları).
- Migration: gerekmedi. Commit yok.
