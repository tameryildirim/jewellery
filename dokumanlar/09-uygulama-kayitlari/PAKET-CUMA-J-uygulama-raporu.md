# PAKET CUMA-J — Basit mod jargon + sığma artıkları · uygulama raporu

- **Tarih:** 25.09.2026 (ikinci uygulayıcı, 09:28 devir notu üzerine; ilk uygulayıcı ağ kopmasıyla ENOTFOUND hatası alıp yarıda kesilmişti)
- **Roller:** Taşkın (backend, sunucu koduna dokunulmadı) + Ertuğ (frontend)
- **4. TUR (25.09 11:20–12:05) — GÜNCEL DURUM § 14'TEDİR:** doğrulayıcının 6 bulgusu kapatıldı. Tam süit 6103 test: 6078 geçti, 25 kaldı; 25 kırmızının hepsi başka şeritlerden, CUMA-J farkı 0. `CJ_` 19/19 yeşil. Mutasyonlar M-CJ-14…19'un hepsi kırmızıya döndü. Bölmede 60/60 ölçüm alındı, yeni K12 dahil hepsi 0; yalnız /raporlar 375 BASİT K4 gerekçeli. 8 ekran görüntüsü bölmeden çekildi. tenants 17 → 16; fark başka şeritlerden, bu paketin katkısı 0.
- **Durum:** BİTTİ. Paket kapısının 7 maddesi kanıtlı. **3. tur (25.09 10:30–10:55, doğrulayıcı bulgularının kapanışı) için § 13'e bakın; § 5 ve § 8'deki tam süit ve denetim rakamları § 13 ile güncellenmiştir.**
- 3. tur özeti: tam süit 6027 test, 6025 geçti, 2 kaldı. İki kırmızı da başka bir şeridin şu an düzenlediği dosyalardan (§ 13.3), CUMA-J farkı = 0. Tarayıcı bölmesinde **bağımsız** 60 ölçüm alındı ve bölmeden 7 ekran görüntüsü çekildi. Jargon kalıbı `cari\p{L}*` olarak genişletildi. Test beklentileri sabit metne çevrildi.

## 0. Devralma (devir notu adımları)

| Adım | Bulgu | Kanıt |
|---|---|---|
| Ağaçtaki yarım iş | `Sozlukler.CumaJ.cs` (yeni), `Sozlukler.Katalog.cs` (katman bağı), `AltinPozisyonuKarti.razor`, `Evraklar.razor`, `Faturalar.razor`, `DenetimIzi.razor` + 6 test dosyası | `git status --porcelain` |
| Kalan mutasyon yedeği | 9 yedek (M-CJ-1…9): her biri ağaçtaki dosyayla **cmp AYNI** çıktı, ağaçta yarım mutasyon kalmamış | `cmp` döngüsü: 9/9 AYNI |
| Canlı süreç (5164/5165) | Dinleyen süreç yok | `lsof` boş |
| Test kiracısı artığı | `PAKETCUMA-J%` kiracısı yok; tenants = **13** (başlangıç) | SQL |
| Yarım kalan iş | Mutasyonlar 1–9 ve tam süit (07:03, 5990/5990) koşulmuştu. **Gerçek tarayıcı ölçümü, canlı doğrulama ve rapor yapılmamıştı.** Bu oturum bunları tamamladı. Ölçüm 375 px'te yeni bir K7 bulgusu çıkardı, o da düzeltildi (§ 2-(7)). | — |

## 1. Spesifikasyon ↔ kod doğrulaması

| # | Katalog iddiası (demo taraması 25.09 06:33) | Koddaki gerçek yer | Sapma / uyarlama |
|---|---|---|---|
| 1 | /evraklar ipucu "…giriş hareketini storno edin…" | `Sozlukler.cs` `Evrak.DegismezYol` (+ `Evrak.OnayMetin` de "storno" içeriyordu) | Yalnız BASİT katmanda karşılık verildi. PRO metni değişmedi (D10). `OnayMetin` de aynı jargonu taşıdığı için kapsama alındı. |
| 2 | /raporlar "…cari için düzeltme fişi…" | Metin `Raporlar.razor`'da değil, **`Bilesenler/AltinPozisyonuKarti.razor`** içinde; kaynağı `PozisyonSozlugu.DuzeltmeYolu` | **Dosya alanı sapması:** metin bir bileşende duruyor. Bileşen yalnız `/raporlar` ve `/panel`'de kullanılıyor, CUMA-S ve CUMA-M alanlarıyla çakışmıyor. PozisyonSozlugu'na DOKUNULMADI. BASİT kopya `CumaJSozlugu.Pozisyon(dil, basit)` üretiyor. |
| 3 | /veri-ice-aktarim "Müşteri / cari kartları" | `Sozlukler.cs` `IceAktarim.KaynakCari` (+ `GeriAlindiMesaji` "pasife alındı") | BASİT katmanda "Müşteri kartları". `VeriIceAktarim.razor` değişmedi (metin sözlükten geliyor). |
| 4 | /faturalar `#fk-no-yok-*` "Numara kesimde verilir" iki satır | `Faturalar.razor` taslak hücresi | Kısa metin "Kesimde verilir" + `hucre-tek-satir`. Uzun metin `title`'da. İki dilde de geçerli (K3 mod bağımsız). |
| 5 | /evraklar başlıkları "EVRAK TÜRÜ / EVRAKI VEREN MÜŞTERİ / VADE GÜNÜ" iki satır | `Evraklar.razor` thead | "Tür / Müşteri / Vade", tam ad `title`'da, tabloya `q10-th-tek` sınıfı. Yeni id'ler: `evrak-th-tur`, `evrak-th-veren`, `evrak-th-vade`. Mevcut id değişmedi. |
| 6 | /denetim-izi filtre satırı ±15 px hizasız | `DenetimIzi.razor` Şube alanı | Kök neden: çok şubede `Secici` kendi etiketini çiziyor, dıştaki ikinci "Şube" etiketi kutuyu bir satır aşağı itiyordu. Dış etiket artık yalnız tek şube satırında çiziliyor. |
| (7) | *(katalogda yok; bu oturumun 375 px ölçümü)* /faturalar `#sayfa-basi-faturalar-filtre` K7 kutuAlt±52 | "Fişten Fatura Kes" düğmesi filtre yuvasındaydı | Düğme bir **eylem**, filtre değil. `Eylem` yuvasına taşındı (id `ff-ac` korundu, ikincil kaldı, TEK birincil eylem korundu). Yan etkisi: 1440 px'te filtre çocuğu kuralı (column-flex) nedeniyle düğme metni kutunun dibine yapışıyordu, artık ortada. |

**CSS:** yeni CSS **gerekmedi**. Mevcut `hucre-tek-satir` (app.css) ve `q10-th-tek` (q10.css) sınıfları kullanıldı. `app.css`, `q10.css` ve `index.html`'e **yazılmadı**.

**Mevzuat:** pakette mevzuat hükmü yok (yalnız ekran metni ve yerleşim). WebSearch gerekmedi.

## 2. Yapılanlar (dosya bazında)

- `Kuyumcu.Core/I18n/Sozlukler.CumaJ.cs` (yeni):
  - `CumaJSadeMetinleri` (BASİT karşılıklar) ve `CumaJEkranMetinleri` (kısa başlıklar) iki **konumsal record**. TR ve EN aynı kurucudan geçiyor, eksik alan derleme hatası verir (UX-S31).
  - `Esleme()` lambda yolu kullanıyor, yanlış anahtar derlenmez.
  - `Pozisyon(dil, basit)` BASİT'te jargonsuz kopya döndürür, PRO'da `PozisyonSozlugu` aynen gelir.
- `Kuyumcu.Core/I18n/Sozlukler.Katalog.cs`: `Kur()` içine tek satır eklendi, CumaJ eşlemesi BasitSozluk katmanına bağlanıyor.
- `Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor`: `M => CumaJSozlugu.Pozisyon(Dil.Dil, Dil.BasitMi)`.
- `Kuyumcu.Web/Pages/Evraklar.razor`: kısa başlıklar, `title` ve `q10-th-tek`.
- `Kuyumcu.Web/Pages/Faturalar.razor`: `#fk-no-yok-*` kısa metin; `ff-ac` Eylem yuvasına taşındı.
- `Kuyumcu.Web/Pages/DenetimIzi.razor`: çift Şube etiketi giderildi.
- Testler: `CumaJTestleri.cs` (yeni) + `AltinPozisyonuKartiTestleri.cs`, `EvrakEkranTestleri.cs`, `FaturaEkranTestleri.cs`, `IceAktarimEkranTestleri.cs`, `EkranTestleri.cs` (DenetimIziEkranTestleri).

### Aynı jargonun başka ekranlardaki izleri (grep Sozlukler)

| Metin | Konum | Durum |
|---|---|---|
| "…dekontu Storno edin…" | `Sozlukler.BankaDekont.cs` EslesmisYolu | BASİT karşılığı zaten var: `Katalog.BankaEslesmisYoluBasit` |
| "…dekontu STORNO edin…" | `Sozlukler.cs` DekontDegismezYolu | BASİT karşılığı zaten var: `Q105.DekontDegismezYoluBasit` |
| "…fişi doğduğu ekrandan … storno edin" | `Sozlukler.cs` HareketStornoYolu | Bu paketin 5 ekranında görünmüyor, dosya alanı dışında. **Açık kalan (§ 9).** |

## 3. DoD sözleşmesi

Yeni varlık yok, yeni kayıt türü yok, yeni uç yok. Sunucu koduna dokunulmadı. Mevcut düzenleme yolları (evrak Düzenle, fatura Düzenle, DegismezIpucu) aynen duruyor. Tek düğme değişikliği `ff-ac`: yalnız yeri değişti, tıklayınca yine `FistenModalAc` çağrılıyor (mevcut `FaturaEkranTestleri` satır 336 testi yeşil).

## 4. Derleme · migration

- `dotnet build Kuyumcu.sln --nologo` sonucu **0 Hata, 0 Uyarı** (son temiz derleme, M-CJ-10 geri alındıktan sonra).
  - Arada bir derleme 1 uyarı verdi: `EtiketEkranTestleri.cs(145) xUnit1031`. Dosya 24.09 tarihli, izlenmiyor, bu pakete ait değil. Artımlı derlemede yeniden derlendiği için göründü; son temiz derlemede 0 uyarı.
- Migration: **gerekmedi** (şema değişikliği yok).

## 5. Testler

| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| CUMA-J filtreli (`FullyQualifiedName~CJ_`), devralma anı | 13 | 13 | 0 |
| Fatura ekranı + CJ filtreli (CJ_08 eklendikten sonra) | 51 | 51 | 0 |
| Tam süit, ilk uygulayıcı 07:03 (`trx-pCUMA-J`) | 5990 | 5990 | 0 |
| Tam süit, bu oturum 09:3x (`trx-pCUMA-J-2`) | 5990 | 5990 | 0 |
| **Tam süit, son (`trx-pCUMA-J-3`, CJ_08 dahil)** | **6022** | **6022** | **0** |

Artış 5990'dan 6022'ye: +1 CJ_08, geri kalanı eşzamanlı şeritlerin testleri. Devir notundaki kırmızı listesi boş, bu paketin kırmızısı da 0; **FARK = 0**.

**Bu paketin testleri:** 12 metot, 14 koşum.
- `CumaJTestleri`: CJ_01, CJ_01b, CJ_02b, CJ_03
- `AltinPozisyonuKartiTestleri.CJ_02` (basit: True/False)
- `EvrakEkranTestleri`: CJ_03b, CJ_05 (True/False)
- `FaturaEkranTestleri`: CJ_04, **CJ_08 (bu oturum)**
- `IceAktarimEkranTestleri.CJ_07` (True/False)
- `DenetimIziEkranTestleri.CJ_06`

## 6. Mutasyon tablosu

M-CJ-1…9 ilk uygulayıcının koşumudur. Değişikliğin tarifi, log dosyasındaki kırmızı test ve iddia iletisinden çıkarılmıştır (`scratchpad/mut-M-CJ-<n>-test.log`). Yedeklerin 9'u da ağaçla cmp birebir çıktı. M-CJ-10 bu oturumda koşuldu.

| Kod | Değişiklik | Kırmızı testler | Geri alındı (cmp) |
|---|---|---|---|
| M-CJ-1 | `Evrak_DegismezYol` BASİT metni storno'lu metne döndü | CJ_01, CJ_05(basit:True) | birebir |
| M-CJ-2 | `IceAktarim_KaynakCari` "Müşteri / cari kartları"na döndü | CJ_01, CJ_07(basit:True) | birebir |
| M-CJ-3 | Pozisyon BASİT `DuzeltmeYolu` "cari için" metnine döndü | CJ_02b, AltinPozisyonuKarti.CJ_02(basit:True) | birebir |
| M-CJ-4 | `Katalog.Kur()` içindeki CumaJ eşleme satırı devre dışı bırakıldı | CJ_01b, CJ_01, CJ_05(True), CJ_07(True) | birebir |
| M-CJ-5 | `AltinPozisyonuKarti` BASİT katmanı atladı (PRO sözlük) | CJ_02(basit:True) | birebir |
| M-CJ-6 | `#fk-no-yok` hücresi uzun metne döndü | CJ_04 | birebir |
| M-CJ-7 | Evrak "Veren" başlığı uzun `KaynakCari` metnine döndü | CJ_03b | birebir |
| M-CJ-8 | DenetimIzi dış Şube etiketi koşulsuz çizildi | CJ_06 (2 label) | birebir |
| M-CJ-9 | `Evrak_OnayMetin` BASİT metni storno'lu metne döndü | CJ_01 | birebir |
| **M-CJ-10** | `ff-ac` düğmesi filtre yuvasına geri taşındı | **CJ_08** (Başarısız: 1) | **CMP-BİREBİR** |

Son mutasyondan sonra `dotnet build Kuyumcu.sln` 0 hata, 0 uyarı. Mutasyon yedekleri `scratchpad/mut-yedek/CUMA-J/` altında, proje ağacında yedek dosya yok.

## 7. Gerçek tarayıcı ölçümü (5 rota × 3 genişlik × 2 mod × 2 tema = 60 satır)

**Yöntem:**
- API **5164** (kendi portum) ve vekil **5165** (`inceleme-vekili.py`, parola çıktıya yazılmadı).
- Test kiracısı `PAKETCUMA-J-TEST Kuyumculuk` açıldı. Kayıt davetle açılıyor (F-2-min), bu yüzden `kiraci_davetleri` tablosuna tek kullanımlık bir davet satırı SQL ile yazıldı; kurucu 23.09'da satır işlemine izin vermişti. İş sonunda satır silindi.
- Tohum veri: 1 müşteri, 2. şube (çok şubeli durumda Secici görünsün diye), 1 çek + 1 senet, 1 taslak fatura.
- Tarayıcı bölmesi gizli olduğu için ekran görüntüsü alınamadı ("pane is not displayed"). Ölçümler iki bağımsız yoldan yapıldı:
  - **(a) Tarayıcı bölmesi**, kendi sekmem `tab-83`: `sikisma-olc.js` satır içi yüklendi, 60 ölçüm alındı. Mod geçişi Basit/Profesyonel düğmesiyle, tema `kuyumcuTema.uygula` ile.
  - **(b) Başsız Chrome + CDP** (`scratchpad/cj.mjs`, port 9491): betik **dosyanın kendisi** okunarak çalıştırıldı. Mod `PATCH /kullanicilar/ben {arayuzModu}` ile (HTTP 200), tema `localStorage kuyumcu-tema` ile. `data-mod` ve `data-tema` her satırda doğrulandı. 60 ekran görüntüsü bu yoldan alındı.
- İlk (b) denemesi rota başına sayfayı yeniden yüklüyordu. `Yenile` oran sınırı (10/dk) aşıldı, 429 geldi: ekranda "Sunucu çok yoğun" bandı çıktı ve mod PRO'ya düştü. **O koşum geçersiz sayıldı.** Betik SPA gezinmesine (`Blazor.navigateTo`) çevrildi. Her satırda hata bandı (`bant=`) ve "Kur alınamadı" (`kurYok=`) kontrolü eklendi. Kabul koşumunda 60/60 satırda bant boş, `kurYok=false`.

**Sonuç (b), kabul koşumu:** 60/60 satırda K1, K3 taşan/kırık, K5, K6, K7, K8, K9, K10, K11 ve yatay kaydırma **0**. K2 kart sağ kenarı %97–98 (dar sütun yok).

| Rota | 1440 B/P (açık·koyu) | 1280 B/P (açık·koyu) | 375 B/P (açık·koyu) | K4 boy×bölüm |
|---|---|---|---|---|
| /evraklar | 0 / 0 | 0 / 0 | 0 / 0 | 1×1 · 1.2×1 · 2.3–2.4×1 |
| /raporlar | 0 / 0 | 0 / 0 | 0 / 0 | B 1.9×7 · 2×7 · **2.8×7**; P 1.6×1 · 1.9×1 · 3.8×1 |
| /veri-ice-aktarim | 0 / 0 | 0 / 0 | 0 / 0 | 1.3×3 · 1.5×3 · 1.7–1.8×3 |
| /faturalar | 0 / 0 | 0 / 0 | 0 / 0 *(düzeltmeden önce 375'te K7=1)* | 1×2 · 1×2 · 1–1.3 |
| /denetim-izi | 0 / 0 | 0 / 0 | 0 / 0 | 1.4–1.5×1 · 1.6–1.7×1 · 2.5–2.6×1 |

(Hücrelerdeki "0" = K1–K11 + yatay toplam ihlal sayısı. Satır satır ham çıktı: `olcumler/CUMA-J/cj-matris.log` ve 12 JSON.)

**K4 gerekçesi (ölçülmüş):** /raporlar 375 BASİT 2.8 ekran × 7 bölüm, "boy > 2 ekran ve ≥ 4 bölüm" eşiğini aşıyor. Bu, rapor merkezinin 7 bölümlük kataloğunun telefonda tek sütuna yığılmasından geliyor. Bu paket /raporlar yapısını değiştirmedi (yalnız pozisyon kartı metni). 1280 ve 1440'ta eşik altında (≤ 2.0). **Açık kalan olarak yazıldı** (§ 9), sahibi rapor merkezi ekranı.

**K8 hariç tutma:** kabul koşumunda K8 = 0 çıktı, hariç tutulan eşleşme **yok**. Tohum veride QA-TEST kelimesi yok.

**(a) bölme ölçümü notları:**
- 1280 açık BASİT ilk turda /denetim-izi K9=3 çıktı (`span.secici-ad "Tüm şubeler" ⟂ th EYLEM`). Aynı anda `__olc()` tekrar edildi: 0. Seçici listesi önceki mod tıklamasından açık kalmıştı. Ardından yapılan tam tekrar (a) ve (b) koşumlarında 0.
- 375 /faturalar K7=1 (`#sayfa-basi-faturalar-filtre kutuAlt±52`) → § 1-(7) düzeltmesi. Düzeltmeden sonra 12 kombinasyonun hepsinde 0.

**Metin kanıtı (tarayıcı DOM'u, 1440):**
- BASİT /evraklar: storno sayısı 0, "ters kayıt" 1. PRO /evraklar: storno 1 (PRO metni korundu).
- BASİT /raporlar: "müşteri hesabı için" var, "cari için" yok.
- BASİT /veri-ice-aktarim: "Müşteri kartları" var. PRO'da "cari kart" var.
- /faturalar: `#fk-no-yok-*` = "Kesimde verilir", title "Numara kesimde verilir", yükseklik 17 px (tek satır), td 121 px.
- /evraklar başlıkları: TÜR/MÜŞTERİ/VADE, h=42 (tek satır).
- /denetim-izi kutu alt kenarları: satır 1 371/371/371, satır 2 484/484 (±0).

**Ekran görüntüleri:** `dokumanlar/09-uygulama-kayitlari/ekran/CUMA-J/` altında 60 PNG, adlandırma `CUMA-J-<gen>-<basit|pro>-<acik|koyu>-<rota>.png`. Göz kontrolünden geçenler:
- `CUMA-J-1440-basit-acik-evraklar.png`: kısa başlıklar, ipucu "ters kayıtla geri alın".
- `CUMA-J-1440-basit-koyu-faturalar.png`: "Kesimde verilir" tek satır; Fişten Fatura Kes + Yeni Fatura eylem yuvasında.
- `CUMA-J-1280-basit-acik-denetim-izi.png`: Kayıt türü ve Şube kutuları aynı hizada.
- `CUMA-J-375-basit-acik-faturalar.png`: filtre ve eylem ayrı satırlarda, yatay kaydırma yok.

## 8. denetim.sh

`./denetim.sh` sonucu **DENETİM BAŞARISIZ**, tek ✗:

```
✗ index.html <script> sayısı (sabit 8)         9 (beklenen 8)
```

Bu pakete ait değil. `index.html` ve yeni `js/secici-yerlesim.js` 25.09 **09:57:44**'te başka bir şerit tarafından değiştirildi; bu paket `index.html`'e hiç dokunmadı. Aynı koşumda 287 satır ✓. Bu paketin ilgili sayaçları ✓: ModalPencere düzenleme artışı 0, satır içi style, sabit renk, çıplak metin/Sozlukler TR+EN, SAYFA_BASI_DISI_DUZEN. İlk uygulayıcının 07:04 koşumu (aynı paket içeriğiyle) `DENETİM TEMİZ` idi.

## 9. Açık kalanlar (sahibiyle)

1. **denetim.sh `index.html <script>` 9/8.** Sahibi: `js/secici-yerlesim.js`'i ekleyen şerit (25.09 09:57). Ya sayaç sabiti güncellenmeli ya betik kaldırılmalı. Karar orkestratörün.
2. **/raporlar 375 BASİT K4 2.8×7.** Rapor merkezi mobil yığını. Sahibi: /raporlar ekranı; bu paketin kapsamı dışında.
3. **`HareketStornoYolu`.** "…storno edin" metninin BASİT karşılığı yok; bu paketin 5 ekranında görünmüyor. Sahibi: defter/hareket ekranı şeridi.
4. **(Gözlem, ölçüm yakalamıyor.)** `.sayfa-basi-filtre > *` kuralı (app.css, column-flex, `justify-content:flex-end`) filtre yuvasındaki **düğme ve select**lerin metnini kutunun dibine itiyor. /evraklar sekme düğmeleri (Tümü, Portföyde…) ve /faturalar "Tümü" select'i bundan etkileniyor. Bu paket `ff-ac` için bunu eylem yuvasına taşıyarak çözdü. Genel düzeltme **app.css**'te yapılmalı (SAYFA_BASI_DISI_DUZEN kuralı gereği sayfa başına CSS yazılamaz). Sahibi: orkestratör/app.css.
5. **(Gözlem.)** /denetim-izi BASİT modda "Kayıt türü" sütununda ham adlar görünüyor (ör. `TelemetriOlcumu`, Detay'da `ArayuzModu, Guncellendi`). K8 düzenli ifadesi bunları yakalamıyor. Sahibi: denetim izi ekranı (Q10-5 çevirisinin kapsamı).
6. `EtiketEkranTestleri.cs(145) xUnit1031` uyarısı: artımlı derlemede görünüyor. Sahibi: etiket şeridi.

## 10. Canlı doğrulama ve temizlik

| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /auth/kayit` davetsiz | 403 | 403, tek tip davet mesajı |
| Davet satırı (SQL) + `POST /auth/kayit` | 201 | 201 (tenantId, accessToken, refreshToken) |
| `POST /cari`, `POST /subeler`, `GET /kur/guncel` | 201/200 | 201 · 200 · 200 (ALTINKAYNAK anlık görüntüsü) |
| `POST /evraklar` ×2 (CEK, SENET) | 201 | 201 · 201 (PORTFOYDE) |
| `POST /faturalar` (taslak) | 200 | 200, `faturaNo: null` |
| `PATCH /kullanicilar/ben {arayuzModu}` ×12 | 200 | 12/12 200 |
| API 5164, vekil 5165, CDP 9491 durduruldu | dinleyen yok | `lsof` üçü de boş |
| Kiracı silindi (tenant_id kolonlu 70 tablo + tenants + davet satırı) | kalan 0 | kalan_satir=0 · kiracı 0 · davet 0 |
| tenants sayısı | 13 (başlangıç) | **13** |
| Fikstür dosyası (parola) | boşaltıldı | 0 bayt |

QA-TEST kiracılarına ve diğer şeritlerin PAKET*-TEST kiracılarına dokunulmadı.

## 11. Değişen dosyalar (bu paket)

```
 M dotnet/Kuyumcu.Web/Pages/Faturalar.razor
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.CumaJ.cs
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.Katalog.cs          (tek satır CumaJ bağı; dosya önceden izlenmiyordu)
?? dotnet/Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor
?? dotnet/Kuyumcu.Web/Pages/DenetimIzi.razor
?? dotnet/Kuyumcu.Web/Pages/Evraklar.razor
?? dotnet/Kuyumcu.Tests/CumaJTestleri.cs
   dotnet/Kuyumcu.Tests/{AltinPozisyonuKarti,EvrakEkran,FaturaEkran,IceAktarimEkran,Ekran}Testleri.cs (CJ testleri)
+  dokumanlar/09-uygulama-kayitlari/ekran/CUMA-J/ (60 PNG) · olcumler/CUMA-J/ (12 JSON + matris log)
```

`VeriIceAktarim.razor`, `Raporlar.razor`, css dosyaları ve `index.html`'e dokunulmadı. Sunucu koduna da dokunulmadı.

**Dokunulan sayfalar (DoD EK 2):** Evraklar.razor, Faturalar.razor, DenetimIzi.razor, Bilesenler/AltinPozisyonuKarti.razor. Faturalar.razor bu dalgada iki kez açıldı (ilk uygulayıcı + bu oturum). Gerekçe: aynı paketin devri ve 375 px K7 ölçüm bulgusu.

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) /evraklar storno → sade (BASİT), PRO korunur | ✓ | CJ_05, DOM storno 0/1 |
| (2) /raporlar cari → sade | ✓ | CJ_02, CJ_02b, DOM |
| (3) /veri-ice-aktarim "Müşteri kartları" | ✓ | CJ_07, DOM |
| Sözlük TR+EN, tek kaynak | ✓ | CJ_01b, konumsal record |
| (4) fatura hücresi tek satır, tablo kaymıyor | ✓ | CJ_04, h17, Y:0 |
| (5) evrak başlıkları tek satır, 1280 ve 1440 | ✓ | CJ_03b, K3 0 |
| (6) denetim filtre hizası ±3 px | ✓ | CJ_06, alt kenar ±0, K7 0 |
| Mevcut id'ler korunur | ✓ | CJ_03b, CJ_08 (`ff-ac`) |
| app.css'e yazılmaz | ✓ | CSS değişikliği yok |
| Gerçek tarayıcı 3 gen × 2 mod × 2 tema, K=0 | ✓ (K4 /raporlar 375 gerekçeli) | § 7, 60 PNG |
| Mutasyon: sözlük eski metne → kırmızı | ✓ | M-CJ-1, 2, 3, 9 |
| Tam süit yeni kırmızı yok | ✓ | 6022/6022 |
| Test kiracısı silindi | ✓ | tenants 13 |


## 13. Üçüncü tur: doğrulayıcı bulgularının kapanışı (25.09 10:30–10:55)

### 13.1 Bulgu → yapılan

| Bulgu | Derece | Yapılan | Kanıt |
|---|---|---|---|
| Tam süit "6022/6022" doğrulayıcıda tutmadı (5 kırmızı, başka şeritler) | DÜŞÜK | Tam süit yeniden koşuldu. Sonuç **6027 test, 2 kırmızı**; ikisi de CUMA-J dışında. Rapor artık gerçek sayıyı yazıyor. | § 13.3 |
| Jargon kalıbı çekimleri kaçırıyor (`carilerde`, `carilerin`) | DÜŞÜK | `CumaJJargon.Re` ve `betikler/sikisma-olc.js:76` kalıbı `cari\p{L}*` oldu (eski: `cari\|cariler\|cariden\|cariye\|carinin`). Yeni test **CJ_09**: `carilerde`, `carilerin` ve PRO `PozisyonSozlugu.Tr.Bos/DefterFarki` artık yakalanıyor; BASİT kopya temiz. `node --check` sonucu sözdizimi OK. Eski betiğin yedeği `scratchpad/mut-yedek/CUMA-J-r3/sikisma-olc.js.onceki`. | CJ_09 yeşil; M-CJ-11 |
| CJ_04, CJ_01 beklentiyi üretim sabitinden alıyor | DÜŞÜK | Beklenen değerler testte **sabit metin** oldu: CJ_01 (3 Assert), CJ_01b (1), CJ_04 ("Kesimde verilir"), CJ_05 (ipucu parçası), CJ_03b ("Tür", "Müşteri", "Vade"), AltinPozisyonuKarti.CJ_02 (tam DuzeltmeYolu metni). | M-CJ-12, M-CJ-13 kırmızı |
| Dosya alanı dışı (AltinPozisyonuKarti, Katalog.cs) | DÜŞÜK | Değişiklik yok. Kapı kaydına yazılması için bu satırda tekrar ediliyor: gerekçe § 1-(2). | — |
| **KRİTİK:** bağımsız ekran görüntüsü yoktu (bölme gizliydi) | KRİTİK | Bölme `preview_start` ile açıldı. Kendi sekmem `tab-91` (ilk sekme `tab-90` bölme gizlenince görüntü veremedi, kapatıldı). **60 ölçüm bölmede alındı** (başsız Chrome kullanılmadı). **7 ekran görüntüsü doğrudan bölmeden** çekildi ve gözle incelendi. | § 13.4 |

### 13.2 Test ve mutasyon (3. tur)

- `dotnet build Kuyumcu.sln --nologo`: **0 Hata, 0 Uyarı** (son temiz derleme, M-CJ-13 geri alındıktan sonra). Aradaki bir derlemede 1 uyarı görüldü (`EtiketEkranTestleri.cs(145) xUnit1031`, başka şerit, artımlı derleme).
- Filtreli koşum `FullyQualifiedName~CJ_`: **15/15 geçti** (önceki tur 14; CJ_09 eklendi).

| Kod | Değişiklik (tek eşleşme, `perl -0777 -pi`) | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M-CJ-11 | `Sozlukler.CumaJ.cs` BASİT pozisyon `Bos` metni eski "…carilerde altın yok." hâline döndü (dar kalıbın kaçırdığı çekim) | CJ_09, **CJ_02b** (dar kalıpla bu mutasyon YEŞİL kalırdı; açık kapandı) | cmp BİREBİR |
| M-CJ-12 | `EkranTr.FaturaNoKesimdeKisa` "Numara kesimde verilir" oldu | CJ_03, CJ_04 (artık sabit metinle) | cmp BİREBİR |
| M-CJ-13 | `SadeTr.Evrak_DegismezYol` "…giriş hareketini storno edin…" oldu | CJ_01, CJ_05(basit: True) (sabit metinle) | cmp BİREBİR |

Loglar: `scratchpad/M-CJ-11.log`, `M-CJ-12.log`, `M-CJ-13.log`. Yedekler `scratchpad/mut-yedek/CUMA-J-r3/`. Proje ağacında yedek dosyası yok.

### 13.3 Tam süit (paket kapısı madde 7)

`KUYUMCU_TEST_DB=kuyumcu_test_t3 dotnet test … --logger trx --results-directory scratchpad/trx-pCUMA-J-4`

```
Toplam test sayısı: 6027 · Başarısız: 2 · süre 2,6 dk
Başarısız Kuyumcu.Tests.UX4dEkranTestleri.M24_Sozlesmesiz_kaynak_seciliyken_PILOT_uyarisi_gorunur
   Assert.Contains() Failure: "Deneme kur kaynağı" içinde "sözleşmesiz" yok
Başarısız Kuyumcu.Tests.P22EkranTestleri.B33_sayim_DURUM_sutunu_gercek_durumu_yazar
   Expected: AÇIK  Actual: Açık
```

**Sahiplik:** iki test de koşum sırasında başka bir şeridin değiştirdiği dosyalara bağlı. `Sozlukler.Sade.cs` (`KurDenemeRozet: "Deneme kur kaynağı"`), `Sayim.razor`, `UX4dEkranTestleri.cs`, `UstBarSozlugu.cs` ve `CanliKurServisi.cs` 10:3x–10:46 arasında değişti; hiçbirine bu paket dokunmadı. Olası sahip: sadeleştirme / ortak görünüm şeridi (SADE1, OrtakGorunum). CUMA-J testlerinin 15'i de yeşil. **FARK = 0.** Doğrulayıcının 1. turda gördüğü 5 kararsız test (L1bH_04, L1bSinirTestleri.D7/D8, Q102, TS2) bu koşumda kırmızı değil.

### 13.4 Gerçek tarayıcı: bağımsız bölme ölçümü (60 satır)

- **Kurulum:** API 5164 ve vekil 5165 (`inceleme-vekili.py`, parola çıktıya yazılmadı, fikstür chmod 600, iş sonunda 0 bayt).
- **Test kiracısı:** `PAKETCUMA-J-TEST r3`, tek kullanımlık davet satırı SQL ile yazıldı.
- **Tohum:** 1 müşteri, 2. şube, 1 çek + 1 senet (PORTFOYDE), 1 taslak fatura (`faturaNo` null).
- **Ölçüm:** `sikisma-olc.js` (genişletilmiş kalıpla) sekme içine yüklendi. Mod `#ust-mod-basit` / `#ust-mod-profesyonel` düğmeleriyle değiştirildi. Tema `kuyumcuTema.uygula('aydinlik' | 'karanlik')` ile değiştirildi, `data-mod` ve `data-tema` her grupta doğrulandı. Genişlik `resize_window` ile ayarlandı. Rota geçişi SPA içinde `Blazor.navigateTo` ile yapıldı. Her grupta hata bandı boştu.

| Rota | 1440 B açık·koyu | 1440 P açık·koyu | 1280 B | 1280 P | 375 B | 375 P | K4 (boy×bölüm) B / P |
|---|---|---|---|---|---|---|---|
| /evraklar | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1–1.2–2.3 ×1 / 1–1.2–2.4 ×1 |
| /raporlar | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1.9–2.0–**2.8 ×7** / 1.6–1.9–3.9 ×1 |
| /veri-ice-aktarim | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1.3–1.5–1.7 ×3 / 1.3–1.5–1.8 ×3 |
| /faturalar | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1–1–1 ×2 / 1–1–1.3 ×1 |
| /denetim-izi | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1.5–1.7–2.5 ×1 / 1.4–1.6/1.7–2.6 ×1 |

"0" şu sayaçların hepsinin 0 olduğunu gösterir: K1, K3 taşan/kırık, K5, K6 dar seçici (ölçülen 0–3 seçici), K7, K8, K9, K10, K11 ve yatay kaydırma.
- K2 kart sağ kenarı: 1440 ve 1280'de %98, 375'te %97.
- K4: yalnız /raporlar 375 BASİT (2.8×7) eşiği aşıyor. Önceki turdaki gerekçe geçerli (§ 7, § 9-2). PRO 375 (3.9×1) bölüm eşiğinin altında.
- **K8 hariç tutma:** yok. Genişletilmiş `cari\p{L}*` kalıbıyla BASİT'teki 30 ölçümün hepsinde K8 = 0.
- **Önceki turla karşılaştırma:** sayılar önceki turun (b) başsız Chrome koşumuyla birebir aynı. Tek fark: /faturalar 375 PRO K4 1.3, önceki koşumda da 1–1.3 aralığındaydı. İki kaynak aynı gerçeği yazıyor.

**DOM kanıtı (bölme, 1440 BASİT):**
- /evraklar başlıkları: `TÜR|EVRAK NO|BANKA|MÜŞTERİ|VADE|TUTAR|DURUM|İŞLEMLER`.
- `#fk-no-yok-*`: "Kesimde verilir", title "Numara kesimde verilir", h = 17 px (tek satır), td 124,8 px.
- /denetim-izi kutu alt kenarları: 1. satır 371/371/371, 2. satır 484/484. Etiket üstleri 300/300/300 ve 413/413 (±0).
- /raporlar ipucu: "…müşteri hesabı için düzeltme fişi…".
- /veri-ice-aktarim: "Müşteri kartları" var, `cari*` eşleşmesi 0.

**Bölmeden çekilen ve gözle bakılan ekran görüntüleri (tab-91):**
1. 1440 BASİT açık /evraklar: kısa başlıklar tek satır; ipucu "…ya da girişi ters kayıtla geri alın…".
2. 1440 BASİT açık /faturalar: "Kesimde verilir" tek satır; "Fişten Fatura Kes" ve "+ Yeni Fatura" eylem yuvasında.
3. 1440 BASİT açık /denetim-izi: Başlangıç, Bitiş ve Eylem aynı hizada; Kayıt türü ve Şube aynı hizada; tek "Şube" etiketi.
4. 1440 BASİT açık /veri-ice-aktarim: "Müşteri kartları". **Bu görüntüde paket dışı görsel bir kusur görüldü (§ 13.5-1).**
5. 1280 PRO koyu /denetim-izi: hizalı; PRO'da "Cari" kayıt türü görünüyor (D10'a göre kabul).
6. 375 BASİT açık /faturalar: "Kesimde verilir" tek satır; tablo kendi kabında kayıyor, sayfada yatay kaydırma 0.
7. 375 BASİT açık /evraklar: sekme düğmelerinin metni kutunun dibine yapışık (§ 9-4'teki app.css gözlemi, görüntüyle doğrulandı).

Not: bölme ekran görüntüleri araç tarafından diske yazılmıyor; görüntüler bu oturumun kaydında duruyor. Dosya olarak arşivlenen 60 PNG, önceki turun başsız Chrome koşumundan kalıyor (`ekran/CUMA-J/`). Doğrulayıcı aynı ölçümü `preview_start` ile açılan kendi sekmesinde tekrarlayabilir; `tabs_create` ile açılan sekme, bölme gizliyse görüntü vermiyor.

### 13.5 Yeni açık kalanlar (3. tur, sahibiyle)

1. **/veri-ice-aktarim çift başlık ve çift etiket (görünür kusur, K sayaçları yakalamıyor).**
   - Başlık iki kez görünüyor: `SayfaBasi` bir kez basıyor, ardından `<div class="sayfa-baslik"><h1>` yeniden basıyor. `.sayfa-baslik` için CSS olmadığından h1 tarayıcı varsayılan boyutunda çıkıyor.
   - "Ne aktarılacak" üç kez görünüyor: kart başlığı, dış `<label for=ia-kaynak>` ve `Secici`'nin kendi etiketi.
   - "Gerekçe" iki kez görünüyor: dış label ve `CokSatir` Etiket.
   - Kart gövdesinde `kart-govde` sarmalı yok, içerik kenara yapışık.
   - Aynı "SayfaBasi + sayfa-baslik" ikilisi 5 sayfada var: Emanet, Isletme, Kur, UrunAgaci, VeriIceAktarim.
   - Bu paketin kapsamı yalnız (3) etiket metniydi. Sayfa 21.09'dan beri böyle; önceki turun PNG'si de aynı durumu gösteriyor. Ortak bir kalıp olduğu için tek sayfada yamamak yerine kalıbın sahibinin düzeltmesi önerilir.
   - **Sahibi:** sadeleştirme / SayfaBasi şeridi. **Kurucunun gözüne takılacak cinsten; öncelik yüksek.**
2. **Tam süit iki kırmızı** (§ 13.3). Sahibi: `Sozlukler.Sade.cs` ve `Sayim.razor` üzerinde çalışan şerit.
3. **denetim.sh** (10:5x koşumu) sonucu 5 ✗, hiçbiri bu paketin sayacı değil:
   - razor record 5/3 ve P-10/P-16 BU_PAKETTE_EKLENEN_RECORD: kaynak `Urunler.razor` (10:46) ve `Giris.razor`. Bu paketin 4 dosyasında record sayısı 0.
   - Sınırsız ToListAsync 146/145: Modules; bu paket sunucu koduna dokunmadı.
   - P-02 OLCUM_TABANI_KOPYA: record tabanına bağlı.
   - Önceki turdaki `index.html <script>` ✗ bu koşumda yok.
   - Bu paketin sayaçları ✓: satır içi style 105/196, ModalPencere 48/55, SABIT_RENK 0, SAYFA_BASI_DISI_DUZEN 0.

### 13.6 Canlı doğrulama ve temizlik (3. tur)

| Adım | Beklenen | Gerçek |
|---|---|---|
| Davet satırı (SQL) + `POST /auth/kayit` | 201 | 201, tenantId döndü, token var (değeri yazılmadı) |
| `POST /cari`, `POST /subeler`, `GET /kur/guncel` | 201/200/200 | 201 · 200 · 200 (ALTINKAYNAK) |
| `POST /evraklar` ×2 | 201 | 201 · 201, durum PORTFOYDE |
| `POST /faturalar` | 200 | 200, `faturaNo: null` |
| `GET /evraklar` · `GET /faturalar` | 200 | 200: `toplamAdet 2`, `toplamTutar 23000`; taslak fatura listede |
| SQL | 2 evrak, 1 taslak, 2 şube | `CEK PORTFOYDE 15000.00 · SENET PORTFOYDE 8000.00` · `fatura_no IS NULL = 1, TASLAK` · şube 2 |
| API 5164 ve vekil 5165 durduruldu | dinleyen yok | `lsof` boş |
| Kiracı silindi (tenant_id kolonlu 70 tablo + tenants + davet) | kalan 0 | İlk silmede `kur_snapshots` = 1 kaldı: API durdurulurken kur zamanlayıcısı 07:50:29 UTC'de satır yazmıştı. İkinci silmeden sonra 0. kiracı 0 · davet 0 |
| tenants sayısı | 16 (bu turun başlangıcı; diğer şeritlerin kiracıları arttırmış) | **16** |
| Fikstür dosyası | boşaltıldı | 0 bayt |

**3. turda değişen dosyalar:**
- `dotnet/Kuyumcu.Tests/CumaJTestleri.cs`: kalıp, sabit beklentiler, CJ_09.
- `dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs`: CJ_04.
- `dotnet/Kuyumcu.Tests/EvrakEkranTestleri.cs`: CJ_05, CJ_03b.
- `dotnet/Kuyumcu.Tests/AltinPozisyonuKartiTestleri.cs`: CJ_02.
- `dokumanlar/09-uygulama-kayitlari/betikler/sikisma-olc.js`: yalnız 76. satırdaki kalıp. Bu dosya ortak araç; doğrulayıcı bulgusu gereği değiştirildi, araç sahibine bilgi olarak yazılıyor.
- Üretim koduna (razor, sözlük, css) bu turda **dokunulmadı**.

## 14. Dördüncü tur: doğrulayıcı bulgularının kapanışı (25.09 11:20–12:05)

> Bu bölüm § 0–13'teki her rakamın **üzerine yazar**. Önceki turların rakamları o turun gerçeğidir, bu turun değil.

### 14.0 Devralma

| Adım | Bulgu |
|---|---|
| Mutasyon yedekleri (`mut-yedek/CUMA-J`, `CUMA-J-r3`) | Ağaçla `cmp`: M-CJ-7, 8, 10, 11, 12, 13 hepsi AYNI. Ağaçta yarım mutasyon yok. |
| 5164/5165 dinleyen süreç | Yok (`lsof` boş). |
| `PAKETCUMA-J%` kiracısı | Yok. |
| **tenants başlangıç** | **17** (08:29:12 UTC, SQL `count(*)`). |

### 14.1 Bulgu → yapılan

| Bulgu | Derece | Yapılan |
|---|---|---|
| /evraklar sekme düğmelerinde metin kutunun dibinde | ORTA | Doğrulayıcının **alternatif** önerisi uygulandı. `app.css` ve `ortak.css`'e dokunulmadı: bu dosyalar paket dışı, üstelik ORTAK-1 şeridi `ortak.css`'i 11:12'de düzenliyordu. Dört durum düğmesi `SayfaBasi` filtre yuvasından çıktı ve süzgeç kartının başına `sekmeler-serit`/`sekmeler-sekme` şeridi oldu (`#evrak-durum-seridi`, `role=group`, `aria-pressed`). Bu, S1-h'nin mevcut CSS'i; yeni CSS yazılmadı. "Vade takvimi" ile TEK birincil "Evrak gir" düğmesi `Eylem` yuvasına geçti. Dört düğmenin id'si ve `secili` sınıfı korundu. Ölçüm: 375 BASİT "Portföyde" kutusu 322–366, metni 333–355, merkez sapması **0 px**, `flex-direction: row`. |
| (aynı bulgu) sikisma-olc.js bu kusuru yakalamıyor | — | **K12 dugmeDikey** sayacı eklendi: tek satırlık düğmede yazının dikey merkezi, kutu merkezinden max(4 px, boyun %15'i) fazla saparsa sayılır. **Negatif kontrol** gerçek sekmede yapıldı: sayfaya `justify-content:flex-end` taşıyan 44 px bir düğme eklenince K12 = 1, `center` yapılınca 0, düğme kaldırılınca 0. `node --check` OK. Önceki sürümün yedeği: `scratchpad/mut-yedek/CUMA-J-r4/sikisma-olc.js.r3`. Bu ortak araç doğrulayıcı önerisiyle değiştirildi; araç sahibine bilgi olarak yazılıyor. |
| /veri-ice-aktarim çift başlık, çift etiket, kenara yapışık içerik | DÜŞÜK | `VeriIceAktarim.razor` bu paketin dosya alanında; sayfa yerinde düzeltildi. (1) `sayfa-baslik` h1 ve açıklama kaldırıldı; `SayfaBasi` ikisini zaten basıyor. (2) Dıştaki `<label for=ia-kaynak>` kaldırıldı; `Secici` kendi etiketini çiziyor. Tek kaynak durumunda `alan-etiket` span'ı var. (3) Kart başlığı artık adımın adı: "Dosyayı hazırlayın" (yeni id `ia-adim1-baslik`, `CumaJEkranMetinleri.IceAktarimHazirlikBaslik`, TR+EN). Böylece "Ne aktarılacak" yalnız bir kez görünüyor. (4) Dıştaki "Gerekçe" label'ı kaldırıldı; `CokSatir` Etiket'i basıyor. (5) Üç kartın içeriği `kart-govde dikey-bosluk` içine alındı. **Canlı DOM:** h1/h2 = ["Veri İçe Aktarımı", "Dosyayı hazırlayın", "İçe Aktarım Geçmişi"], "Ne aktarılacak" = 1, "Gerekçe" etiketi = 1, her kartta `:scope > .kart-govde` var. Aynı kalıbı taşıyan diğer 4 sayfa (Emanet, Isletme, Kur, UrunAgaci) dosya alanı dışında; **açık kalan (§ 14.6)**. |
| Evrak Vade hücresi ISO ve iki satır | DÜŞÜK | `VadeMetni()`: Türkçede `dd.MM.yyyy`, İngilizcede dilin kısa tarihi. Hücre `<span class="hucre-tek-satir" id="evrak-vade-{id}">` içinde. `Bicim.Makine` yalnız sorgu ve gövdede kaldı. **Canlı:** "15.10.2026/h19", "01.12.2026/h19" (tek satır). Aynı ekran görüntüsünde "SNT-2002" evrak no'su da tireden bölünüyordu; `hucre-vurgu` td'sine de `hucre-tek-satir` eklendi. |
| "Veren" sütunu PRO'da "Müşteri" | DÜŞÜK | Başlık moda bağlandı. BASİT'te **"Kimden"**: jargonsuz ve doğru, çünkü evrakı tedarikçi de vermiş olabilir. PRO'da **"Veren"** (spesifikasyonun önerisi). EN karşılıkları: "From" ve "Given by". Tam ad iki modda da `title`'da. `CumaJSozlugu.Ekran(dil, basit)`, `EkranProTr`, `EkranProEn`. **Canlı:** BASİT "KİMDEN", PRO "VEREN". |
| KRİTİK: ekran görüntüsü kaynağı çelişkisi | KRİTİK | Bu tur tek kaynak kullandı, ayrıntısı § 14.4. **Başsız Chrome kullanılmadı, diske PNG yazılmadı.** Bütün görüntüler tarayıcı bölmesinden alındı ve bu oturumun kaydında duruyor. Sayılar: **60 ölçüm satırı**, **8 ekran görüntüsü**. § 13.4'teki "7 görüntü / 60 ölçüm" 3. turun, doğrulayıcının "6 PNG / 54" sayısı ise kendi koşumunun rakamıdır; bu tur ikisinin de yerine geçer. `ekran/CUMA-J/` altındaki 60 PNG **2. turdan kalma ve eski yerleşimi gösteriyor**, bu turun kanıtı sayılmamalıdır. |
| KRİTİK: tenants sayısı çelişkisi | KRİTİK | Bu tur her iki uçta da ölçüldü (§ 14.5): **başlangıç 17 → son 16**. Aradaki fark başka şeritlerden: PAKET-ISTEK-1-TEST ve PAKETKATMAN-1-TEST-DOG silindi, PAKET-OLCEK-1-TEST eklendi. **Bu paketin etkisi net 0:** kendi kiracısını 1 açtı, 1 sildi; `PAKETCUMA-J%` = 0. § 13.6'daki "16 (başlangıç)" ifadesi 3. turun ortasında alınmış bir ölçümdü; "başlangıç değerine döndü" yazmak yanlıştı. Doğru söz: **"bu paketin kiracı katkısı 0"**. |

### 14.2 Test ve mutasyon (4. tur)

- `dotnet build Kuyumcu.sln --nologo`: son temiz derleme **0 Hata, 0 Uyarı**. Ara derlemelerde görülen `EtiketEkranTestleri.cs(145) xUnit1031` uyarısı başka bir şeridin artımlı derlemesinden.
- Yeni ve değişen testler:
  - **CJ_03b**: artık Theory, (BASİT "Kimden") ve (PRO "Veren").
  - **CJ_10**: yuvada düğme 0; şeritte 4 düğme sırasıyla; `aria-pressed`; eylem yuvasında Yeni ve Takvim; "Portföyde" tıklanınca `GET … durum=PORTFOYDE` isteği gidiyor ve `secili` sınıfı geliyor.
  - **CJ_11**: vade "01.12.2026", `hucre-tek-satir`, ISO yok; evrak no td'si `hucre-tek-satir`.
  - **CJ_12**: tek başlık, tek açıklama, "Ne aktarılacak" ×1, "Gerekçe" ×1, "Dosyayı hazırlayın", her kartta `kart-govde`.
  - **CJ_03**: Pro kayıtları + Kimden/Veren/Given by.
- Filtreli koşum: `CJ_|EvrakEkranTestleri|IceAktarimEkranTestleri|S3fEkranTestleri|KilitGerekceTestleri` → **70/70 geçti**. `CJ_` kapsamı **19 test** (önceki tur 15).

| Kod | Değişiklik (tek eşleşme, `perl -0777 -pi`) | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M-CJ-14 | `EkranProTr` Veren → "Müşteri" | CJ_03, CJ_03b(basit: False, "Veren") | cmp BİREBİR |
| M-CJ-15 | Vade `@VadeMetni(...)` → `@Bicim.Makine(...)` (ISO) | CJ_11 | cmp BİREBİR |
| M-CJ-16 | `ia-gerekce` önüne dış `<label>` geri kondu | CJ_12 | cmp BİREBİR |
| M-CJ-17 | Portföyde düğmesi `DurumSec("")` (tıklanınca istek yanlış) | CJ_10 | cmp BİREBİR |
| M-CJ-18 | BASİT `EvrakSutunVeren` "Kimden" → "Müşteri" | CJ_03, CJ_03b(basit: True, "Kimden") | cmp BİREBİR |
| M-CJ-19 | `sayfa-baslik` h1 geri kondu | CJ_12 | cmp BİREBİR |

**Not:** M-CJ-16…19'un ilk koşumu CUMA-S şeridinin o sıradaki derleme hatası nedeniyle **geçersiz** sayıldı: `Islemler.razor(153,88) CS0103 'DigerAyrintiAcik'`. Test kırmızısı değil derleme kırmızısıydı. Ağaç 11:34:47'de yeşile dönünce dördü yeniden koşuldu; yukarıdaki sonuçlar o koşumun. Loglar `scratchpad/M-CJ-14…19.log`, yedekler `scratchpad/mut-yedek/CUMA-J-r4/`. Son mutasyondan sonra temiz derleme 0/0. Proje ağacında yedek dosyası yok.

### 14.3 Tam süit (paket kapısı madde 7)

`KUYUMCU_TEST_DB=kuyumcu_test_t3 dotnet test … --logger trx --results-directory scratchpad/trx-pCUMA-J-r4`

```
Counters total=6103 executed=6103 passed=6078 failed=25
CJ_ testleri: 19 Passed (trx)
```

25 kırmızının **hiçbiri** bu paketin dosyalarına dokunmuyor. Kırmızı test dosyalarında Evraklar, VeriIceAktarim, Faturalar, DenetimIzi, CumaJ ya da AltinPozisyonuKarti referansı yok (grep 0). Sahipleri:
- **CUMA-S** (Islemler.razor 11:55'te değişti): CumaSAldimSadeTestleri.CUMA_S_04; S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz; S22TezgahFisiTestleri.Pro_modda_izgara_YOK…; TS1TuvalSatisTestleri ×3; TS2TuvalProTestleri ×4; UX1C1EkranTestleri ×6 (POS/P23); KatalogSonSatisTestleri.KAT_K4_02 ve K4_03.
- **ISTEK-1** (ListeOnbellegi.cs 11:58, ApiIstemci.cs 12:00): Istek1OnbellekTestleri ×5 ve Istek1EkranTestleri.ISTEK1_25.
- **ORTAK-1**: OrtakGorunumPanelTestleri.PN1 (panel değer uzunluk kademesi `null`).

**FARK (bu paketin ürettiği yeni kırmızı) = 0.**

### 14.4 Gerçek tarayıcı: ölçüm (60 satır) ve ekran görüntüleri

- **Kurulum:**
  - API 5164 (PID dosyası `cj4-api.pid`), vekil 5165 (`inceleme-vekili.py`).
  - Fikstür chmod 600; iş sonunda 0 bayt. Parola çıktıya yazılmadı.
  - Kiracı `PAKETCUMA-J-TEST r4`, tek kullanımlık davet satırı SQL ile yazıldı.
  - Tohum: 1 müşteri, 2. şube, CEK-1001 (15.000, vade 15.10.2026), SNT-2002 (8.000, vade 01.12.2026), 1 taslak fatura (`faturaNo` null).
- **Bölme:** başta gizliydi (tabs_context: "Browser pane is currently hidden"). `preview_start` ile açıldı → **tab-93**.
  - **Ölçümlerin 60'ı da tab-93'te alındı.** Ölçüm sürerken bölme yine gizlendi. JS ölçümü sürdü ama ekran görüntüsü "pane not displayed" hatasıyla alınamadı.
  - Görüntüler için `preview_start` yeniden çağrıldı → **tab-94**. Aynı kiracı, aynı oturum.
  - İş bitince iki sekme de kapatıldı ve görünüm "desktop"a döndürüldü. Başka sekmelere dokunulmadı.
- **Yöntem:**
  - `sikisma-olc.js` (K12 eklenmiş hâli) sekmeye yüklendi.
  - Mod `#ust-mod-basit` / `#ust-mod-profesyonel` ile değiştirildi, tema `kuyumcuTema.uygula`, genişlik `resize_window`, rota `Blazor.navigateTo`. Her satırda `data-tema` ve genişlik okundu.
  - İlk 1440 koşumu 45 sn araç sınırına takıldı. Arka planda süren koşumla ikinci koşum çakıştığı için o sonuç **atıldı**; 1440 temiz koşumla yeniden alındı.

| Rota | 1440 B açık·koyu | 1440 P açık·koyu | 1280 B | 1280 P | 375 B | 375 P | K4 (boy×bölüm) 1440/1280/375 B · P |
|---|---|---|---|---|---|---|---|
| /evraklar | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1/1.3/2.1 ×1 · 1/1.3/2.2 ×1 |
| /raporlar | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1.9/2/**2.8 ×7** · 1.6/1.9/3.9 ×1 |
| /veri-ice-aktarim | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1.3/1.5/1.7 ×3 · 1.4/1.5/1.8 ×3 |
| /faturalar | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1/1/1 ×2 · 1/1/1.3 ×1 |
| /denetim-izi | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 0·0 | 1.5/1.7/2.5 ×1 · 1.4/1.6/2.6 ×1 |

- "0" şu sayaçların hepsinin 0 olduğunu gösterir: K1, K3 taşan ve kırık, K5 TRY, K6 dar seçici (ölçülen 0–3), K7, K8, K9, K10, K11, **K12** ve yatay kaydırma.
- K2 kart sağ kenarı: 1440 ve 1280'de %98, 375'te %97.
- K4: yalnız /raporlar 375 BASİT (2.8×7) eşiği aşıyor. Bu, rapor merkezinin mobil yığını; gerekçesi § 7 ve § 9-2'de, sahibi /raporlar ekranı. PRO 375 3.9×1 (tek bölüm).
- **K8 hariç tutma: yok.** BASİT'teki 30 satırın hepsinde genişletilmiş `cari\p{L}*` kalıbıyla K8 = 0.
- **Sıra notu:** 60 ölçüm, evrak no td'sine `hucre-tek-satir` eklenmeden **önce** alındı. Bu değişiklik yalnız bir td'nin satır kırmasını kapatıyor; K sayaçları td ölçmediği için tabloyu etkilemez. Değişiklikten sonra alınan 1280 BASİT görüntüsü (aşağıda 3 numara) ve DOM ölçümü (`hucre-vurgu hucre-tek-satir`) sonucu doğruluyor.

**DOM kanıtı (canlı):**
- /evraklar 375 BASİT:
  - başlıklar `TÜR|EVRAK NO|BANKA|KİMDEN|VADE|TUTAR|DURUM|İŞLEMLER`;
  - `.sayfa-basi-filtre` = 0;
  - "Portföyde" kutusu ve metni merkez sapması 0 px;
  - vade "15.10.2026/h19", "01.12.2026/h19".
- /evraklar 1440 PRO koyu: "VEREN"; `storno` = 1 (PRO metni D10 gereği korundu).
- /faturalar: `#fk-no-yok-*` "Kesimde verilir", h = 17 px.
- /raporlar BASİT: `cari*` = 0, "müşteri hesabı için düzeltme fişi" var.
- /veri-ice-aktarim: § 14.1'deki DOM satırı.

**Bölmeden çekilen ve gözle bakılan ekran görüntüleri (8):**
1. tab-93, 1440 PRO açık /evraklar (tohumdan hemen sonra): VEREN başlığı, yerel tarihler, şerit kartta.
2. tab-94, 1280 BASİT açık /evraklar: sekme metinleri ortada; "KİMDEN"; tarih tek satır. Burada "SNT-2002" tireden bölünüyordu, düzeltildi.
3. tab-94, 1280 BASİT açık /evraklar (düzeltmeden sonra): "SNT-2002" tek satır.
4. tab-94, 1280 BASİT açık /veri-ice-aktarim: tek başlık, "Dosyayı hazırlayın", "Ne aktarılacak" ve "Gerekçe" birer kez, kart iç boşluğu var.
5. tab-94, 1280 BASİT açık /faturalar: "Kesimde verilir" tek satır; "Fişten Fatura Kes" ve "+ Yeni Fatura" eylem yuvasında.
6. tab-94, 1280 BASİT açık /denetim-izi: Başlangıç, Bitiş ve Eylem aynı hizada; Kayıt türü ve Şube aynı hizada; tek "Şube" etiketi.
7. tab-94, 1440 PRO koyu /evraklar: koyu temada şerit, VEREN, PRO ipucu.
8. tab-94, 375 BASİT açık /evraklar: şerit 2×2 ızgara, metinler ortada; eylemler üstte; yatay kaydırma 0.

**Görsel gözlem (sayaç yakalamıyor, paket dışı):** 1280 /evraklar'da müşteri adı "Ayşe / Yılmaz" iki satıra iniyor. Kişi adının sarması kabul edilebilir, ama tablo İşlemler sütunundaki 4 düğme yüzünden sıkışık. Bkz. § 14.6-3.

### 14.5 Canlı doğrulama ve temizlik (4. tur)

| Adım | Beklenen | Gerçek |
|---|---|---|
| Davet satırı (SQL) + `POST /auth/kayit` | 201 | 201, tenantId döndü, token var (değeri yazılmadı) |
| `POST /auth/giris` | 200 | 200 |
| `POST /cari`, `POST /subeler`, `GET /kur/guncel` | 201/200/200 | 201 · 200 · 200 (snapshot id var) |
| `POST /evraklar` ×2 | 201 | 201 · 201 |
| `POST /faturalar` | 200 | 200, `faturaNo: null` |
| `GET /evraklar` | 200 | 200, `toplamAdet 2`, `toplamTutar 23000.0` |
| SQL | 2 evrak, 1 taslak, 2 şube | `CEK PORTFOYDE 15000.00 2026-10-15 · SENET PORTFOYDE 8000.00 2026-12-01` · `TASLAK, fatura_no IS NULL = 1` · şube 2 |
| API 5164, vekil 5165, CORS yardımcısı 8765 durduruldu | dinleyen yok | `lsof` üçü de 0 (8765 denendi; sayfanın CSP'si `connect-src 'self'` olduğu için kullanılamadı, betik sekmeye yapıştırılarak yüklendi) |
| Kiracı silindi (tenant_id kolonlu 70 tablo + tenants + davet) | kalan 0 | kalan_satir=0 · kiracı 0 · davet 0 · `PAKETCUMA-J%` = 0 |
| **tenants** | başlangıç 17; bu paketin katkısı 0 | **son 16** (08:53:37 UTC). Fark başka şeritlerin: −PAKET-ISTEK-1-TEST, −PAKETKATMAN-1-TEST-DOG, +PAKET-OLCEK-1-TEST. |
| Fikstür dosyası | boşaltıldı | 0 bayt |

QA-TEST kiracılarına ve diğer şeritlerin PAKET*-TEST kiracılarına dokunulmadı.

### 14.6 denetim.sh ve açık kalanlar (4. tur)

**denetim.sh (11:5x):** `DENETİM BAŞARISIZ`, 2 ✗. İkisi de bu paketin dışında:
- `Sınırsız ToListAsync (Modules) 146/145`: bu paket sunucu koduna dokunmadı; 11:18'de değişen `Modules/Urun/UrunStokOzetModulu.cs` (OLCEK-1 ya da ürün şeridi).
- `P-23: ODEME_KALEMSIZ_YOL 3`: canlı veritabanı ölçümü; bu paketin kiracısı ödeme ya da hareket yazmadı. Sahibi satış/ödeme şeridi (CUMA-S ya da P-23).

Bu paketin sayaçları ✓: satır içi style 105/196, ModalPencere 48/55, SABIT_RENK 0, SAYFA_BASI_DISI_DUZEN 0 (yeni `sayfa-basi` sınıfı Pages'e yazılmadı).

**Açık kalanlar (sahibiyle):**
1. **Genel `.sayfa-basi-filtre > *` sütun kuralı.** Filtre yuvasında düğme taşıyan başka sayfalarda aynı kusur sürebilir. Artık K12 bunu ölçüyor. Kökten CSS düzeltmesi `app.css`/`ortak.css` sahibinin işi: ORTAK-1 / orkestratör.
2. **"SayfaBasi + sayfa-baslik" çift başlık kalıbı** 4 sayfada daha var: Emanet, Isletme, Kur, UrunAgaci. Sahibi sadeleştirme / SayfaBasi şeridi. Bu paket yalnız kendi dosya alanındaki VeriIceAktarim'i düzeltti.
3. /evraklar İşlemler sütunu 4 düğme taşıyor ve 1280'de tabloyu sıkıştırıyor (müşteri adı iki satır). Ürün kararı: satır eylemlerini "⋯" menüsüne toplamak. Sahibi evrak ekranı / sadeleştirme.
4. § 9-3 (`HareketStornoYolu`), § 9-5 (denetim izi ham kayıt türü adları) ve § 13.5 maddeleri, bu turda kapatılanlar dışında aynen açık.
5. `h2.kart-baslik` sınıfı h2'nin kendisinde olunca yazı büyük görünüyor (/veri-ice-aktarim'in 3 kartı; `.kart-baslik h2` kuralı yalnız iç h2'yi küçültüyor). Önceden de böyleydi, tutarlı; tasarım belirteci sahibine not.

### 14.7 Değişen dosyalar (4. tur)

- `dotnet/Kuyumcu.Web/Pages/Evraklar.razor`:
  - durum şeridi süzgeç kartına taşındı; Eylem yuvası kuruldu;
  - VadeMetni ve `evrak-vade-{id}`;
  - evrak no `hucre-tek-satir`;
  - `Ekran(dil, basit)`.
- `dotnet/Kuyumcu.Web/Pages/VeriIceAktarim.razor`: çift başlık, çift etiket, `kart-govde`, adım başlığı.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.CumaJ.cs`: Kimden/Veren, EkranPro*, `IceAktarimHazirlikBaslik`, `Ekran(dil, basit)`.
- Testler: `EvrakEkranTestleri.cs` (CJ_03b Theory, CJ_10, CJ_11), `IceAktarimEkranTestleri.cs` (CJ_12), `CumaJTestleri.cs` (CJ_03).
- `dokumanlar/09-uygulama-kayitlari/betikler/sikisma-olc.js`: K12 sayacı (ortak araç).
- **Dokunulmayanlar:** `app.css`, `ortak.css`, `q10.css`, `index.html`, sunucu kodu, CUMA-S ve CUMA-M dosyaları.
- **Dokunulan sayfalar (DoD EK 2):** Evraklar.razor (bu dalgada 2. kez) ve VeriIceAktarim.razor (bu dalgada ilk kez). Gerekçe: aynı paketin doğrulayıcı bulguları (ORTA ve DÜŞÜK).
