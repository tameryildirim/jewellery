# PAKET KAT-1 — KURUCU ŞİKÂYET KATALOĞU: TRY→₺ · basit mod jargonu · ham kodlar · kalan hizalar + TAM TARAMA

Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü · Opus · 2026-09-24 12:5x–15:40
Dalga: DEMO DÜZELTME — KATALOG · test VT `kuyumcu_test_l` · canlı API **5174** (kuyumcu_net) · vekil **5173**
Spesifikasyon: `ham/KURUCU-SIKAYET-KATALOGU.md` (K1–K10) + görev metnindeki "Birleşik koşullar".

**Durum: BİTTİ** — paket kapısının yedi maddesi sağlandı (aşağıda kanıtlarıyla). Sıfır olmayan her hücre ya düzeltildi ya da ölçülmüş gerekçeyle § 9.3'te yazılı.

## 1. Spesifikasyon ↔ kod doğrulaması (sapmalar)
| İddia (spesifikasyon) | Kodda bulunan | Karar |
|---|---|---|
| `Para.razor:17` `@(ParaBirimi ?? "TRY")` | aynen vardı | `Bicim.ParaIsareti` üzerinden ₺ |
| Kasalar `ParaAdi` TRY gösterir | ZATEN "Türk lirası" gösteriyordu (S3-6) | seçenek adı "₺ Türk lirası" oldu; kart satırı aynı |
| Banka `BankaDovizleri` ham kod | aynen (`new SecenekOgesi(d, d)`) | `KatalogSozlugu.ParaSecenegi` |
| SatisAnalizi ham `p.ParaBirimi` | aynen + ham "NAKIT/KART" araç adı (taramada bulundu) | ₺ + ödeme aracı sözlük adı |
| Gider "(TRY)" | iki yer: tek kasa satırı ve virman etiketi + gider kasa seçicisi (grep ile 3. yer) | üçü de ₺ |
| Kurulum döviz satırı | `d.ParaBirimi` + kabul edilen birimler listesi | ikisi de `ParaIsareti` |
| Cariler "{kod} kasası yok" | döviz (USD/EUR/GBP) içindir, TRY girmez; ama ÇIPLAK Türkçe metindi | sözlüğe taşındı (`DovizKasasiYok`) |
| `PanelSadeSozlugu.KasaGrubu` | ZATEN "TL kasalar" basıyor, "TRY" yok | değişiklik gerekmedi |
| Cikti.razor / A4Belge | `Bicim.Para`/`BelgeSayisi` kullanıyor, ham TRY yok | değişiklik gerekmedi; tarama `/cikti/SATIS_FISI/{Id}` K5=0 |
| grep ile yeni bulunan | `Vezne` kasa seçici "(TRY)", `FisModelleri.KasaSecenekleri` "(TRY)" (Satış ekranı pro kasa seçicisi — taban K5), `SiparisDetay` "Tahmini tutar (TRY)" | hepsi ₺ |
| islem-defteri "seçici metinleri üst üste (K9)" | 10:45 tabanındaydı; S1-i sonrası ağaçta K9=0 ölçüldü | yalnız ham kod düzeltildi |
| K8 jargon kaynakları | 55 PRO anahtar Sozlukler.cs'te + 2 sözlük girdisi + 2 ayrı sözlük dosyası + Kilavuz'da 1 çıplak metin | D10 katmanı (§ 2.2) |

Sunucu/veri/sözleşme tarafında para birimi kodu DEĞİŞMEDİ (canlı § 8: API `paraBirimi:"TRY"`, SQL `kasalar.para_birimi = TRY`). Migration YOK.
Mevzuat hükmü yazılmadı; "storno", "gider pusulası", "MASAK" mevzuat metinleri korunarak yalnız basit mod GÖRÜNÜR etiketleri sadeleştirildi (MASAK kelimesi basit metinde de duruyor).

## 2. Yapılanlar
### 2.1 K5 — TRY → ₺ (tek merkez)
* `Kuyumcu.Web/Servisler/Bicim.cs`: **`ParaIsareti(string? kod)`** — "TRY"/null/"" → "₺"; USD/EUR/GBP ve bilinmeyen kod AYNEN.
* `Kuyumcu.Core/I18n/Sozlukler.Katalog.cs`: `KatalogSozlugu.ParaSecenegi(kod, dil)` — seçici seçeneğinde "₺ Türk lirası" / "₺ Turkish lira"; değer (gövde) "TRY" kalır.
* Bağlanan yerler: `Bilesenler/Form/Para.razor` (tüm para girdilerinin son eki), `Bilesenler/Fis/FisModelleri.cs` (satış kasa seçicisi), `Pages/Gider.razor` (3 yer), `Pages/Vezne.razor`, `Pages/Cariler.razor` (tahsilat kasa seçicisi + döviz kasası yok metni), `Pages/Kurulum.razor` (2 yer), `Pages/SatisAnalizi.razor`, `Pages/SiparisDetay.razor`, `Pages/Kasalar.razor`, `Pages/Banka.razor`.
### 2.2 K8 — basit mod jargonu (D10) — sözlük KATMANI
* **`BasitSozluk`** (`Sozlukler.Katalog.cs`): PRO sözlüğün kopya-yaz KATMANI. Basit modda `DilServisi.T` bu katmanı döndürür; PRO sözlük nesnesine dokunulmaz (test: `Assert.NotSame` + PRO değer "Cariler" kalır). Anahtar yolları **lambda** (`s => s.Cari.Baslik`) — silinen/yanlış anahtar DERLEME hatası (UX-S31). Basit metinler ayrı konumsal record `BasitDilMetinleri` (TR+EN; 55 metin + 2 sözlük girdisi: `VarlikAdlari["Cari"]`, `["CariBakiye"]`).
* `DilServisi`: `BasitMi`, `BasitModuBagla(Func<bool>)`, `ModuDenetle()` — mod GERÇEKTEN değişince `Degisti` olayı → dil değişimine abone her ekran yeniden çizilir (canlıda sınandı § 9.2: /cariler Basit→Pro→Basit geçişinde "Cari" sayısı 0→2→0, sayfa yenilemeden). Bağ yoksa (bUnit, ön-işleme) davranış BİREBİR eski.
* `Program.cs` (Web): bağ + `ArayuzModuServisi.Degisti` ve `OturumServisi.Degisti` → `ModuDenetle`.
* Katmanın dışındaki üç metin: `Pages/Kilavuz.razor` (bölüm listesi çıplak "Cariler"), `Pages/Banka.razor` (`BankaDekont.EslesmisYolu` "Storno edin"), `Bilesenler/AltinPozisyonuKarti.razor` (`PozisyonSozlugu.DefterFarki` "Cari ekstrelerini") — basit modda `KatalogMetinleri` karşılığı.
* D10 karşılıkları: Cari→Müşteri, Storno→Geri al (düzeltme fişi) / "Geri alınan" (sayaç kartı), Vezne→Kasa kuyruğu, Mutabakat→Karşılaştırma ("Gün sonu karşılaştırması"), Pasife al→Kullanımdan kaldır, Denetim izi→Kim ne yaptı. PRO modda "Cari" KALIR.
### 2.3 Ham kodlar (UX-S31)
* `KatalogSozlugu.HareketTuru(kod, dil, basit)`: 28 hareket türü (sunucudaki tüm `Tur =` değerleri grep ile çıkarıldı: SATIS … SAYIM_DUZELTME, EVRAK_*, AVANS_*, KONSINYE_*); bilinmeyen kod UYDURULMAZ, kendisiyle döner (tarama yakalar).
* `KatalogSozlugu.OdemeAraci`: NAKIT/KART/HAVALE/ALTIN/VERESIYE/KARMA/DOVIZ.
* `Pages/IslemDefteri.razor`: tür seçici seçenekleri, tablo tür hücresi (`#id-tur-{id}` yeni id), ödeme seçici + hücresi. `Pages/SatisAnalizi.razor`: araç kartı, tablo, grafik etiketi.
### 2.4 K7 / K3 / K9 / K4 — kalan hizalar ve tren (app.css SONUNA "KATALOG" bloğu; sabit renk yok, satır içi stil yok)
| Kural | Ölçülen kusur (taban) | Sonuç (tam tarama) |
|---|---|---|
| (K7-a) şerit kontrolleri tek yükseklik + şeritte `.alan` alttan yaslı | /siparisler ±4, /pos/tahsilatlar ±4 (1280) | 0 |
| şeritte doğrudan `select` `display:block` | yıl/ay, durum seçimi yazısı kutunun üstüne yapışıktı (gözle) | düzeldi |
| (K7-b) ≤640 px iki tarih / iki seçim yan yana | /raporlar/kasa, /raporlar/satis-analizi, /raporlar (yıl·ay) 375 ±52 | satış analizi ve rapor merkezi 0; kasa raporunda 3. öğe (kasa seçimi) tek başına alt satır → § 9.3 |
| (K7-c) form ızgarasında düğme kontrol rayına (özgüllük: mevcut üç `:not` zinciri + öğe) | /emanet "İade Et" kutulardan 12 px aşağı (1280) | 0 |
| (K7-d) görsel gizli etiketli arama kutusu etiket rayından başlar | /emanet ±33, /urunler/etiketler ±30 (etiket üstü) | 0 |
| (K7-d2) ≤768 px satırda arama kendi satırına, seçici+düğme tabandan | /emanet 375 arama ↔ seçici 11 px kayık | 0 |
| (K7-e) kayıt seçici "Ara" düğmesi kutu boyunda | /hurda, /pos/tahsilatlar ±6 | 0 |
| (K9) `.katalog-blok-ek` | /hurda 375 tahlil farkı, /siparisler/{Id} 375 geçiş satırı | 0 |
| (K3) tablo içi küçük düğme / kısa durum metni tek satır | /vitrin "Varsayılana dön" ×3, /urunler "Görsel yok", "3 adet", panel "Satış Yap" | 0 (yalnız çok kelimeli `th` kaldı — § 9.3) |
| (K3 taşma) panel kutucuğunda özet kartları kapsayıcı sorgusuyla tek sütun | /  PRO 1280 "₺99.822,15" kartın dışına taşıyor (4) | 0 |
| (görsel) `button.ikincil` ortak ikincil düğme görünümü | app.css'te `.ikincil` YOKTU: 6 ekranda 36 düğme tarayıcı varsayılanıyla | ortak görünüm |
| şeritte onay kutusu tek satır | /siparisler "Yalnız gecikenler" kutusu yazının üstünde | düzeldi |
* **K4 tren — /raporlar profesyonel yüz**: 8 bölüm, 5.825 px = **7,1 ekran** (1280) → S1-h `Sekmeler` ile **6 sekme** (Özet · Grafikler · Cari yaşlandırma · Stok değeri · Stok yaşı · Gün sonu); hiçbir bölüm silinmedi, id'ler aynı. Rapor merkezindeki "/raporlar" kartları ilgili sekmeyi açar (`Raporlar.KartRotasi`: R05→gunsonu, R06/R20→stok, R15→yaslandirma, R03→cari). Sonuç: 1280 PRO boy **2,3** (1 bölüm).
* **K4 tren — /islemler basit**: "Son satışlar" kartı 13 fişle 1.747 px'ti (sayfa 4,0 ekran). `Bilesenler/Fis/FisSonSatislar.razor`: ilk **5 fiş** + "5 / 13 fiş gösteriliyor" + "Hepsini göster (13)" + "Tüm geçmiş: İşlem defteri →" (UX-S29: sınır yazılı, sessiz kırpma yok). Sonuç 1280 **2,9** (§ 9.3 gerekçe).
* `Pages/Siparisler.razor`: sekme / Yenile / Yeni sipariş düğmelerine ortak düğme sınıfları (tarayıcı varsayılanıyla çiziliyordu; `secili`/`ikincil` sınıf adları korundu).

## 3. Dokunulan sayfalar (DoD EK 2 — tek dokunuş listesi)
Pages: IslemDefteri, SatisAnalizi, Kasalar, Banka, Cariler, Gider, Vezne, Kurulum, SiparisDetay, Siparisler, Kilavuz, Raporlar, Hurda, Urunler · Bilesenler: Form/Para, Fis/FisModelleri.cs, Fis/FisSonSatislar, AltinPozisyonuKarti.
Mevcut element id'leri DEĞİŞMEDİ; yeni id'ler: `id-tur-{Id}`, `islem-son-sinir`, `islem-son-sinir-metin`, `islem-son-hepsi`, `islem-son-defter`, `rp-*` (Sekmeler önek). Rota değişmedi.

## 4. DoD sözleşmesi
Yeni varlık/kayıt türü YOK; yeni uç YOK; yeni form YOK (yalnız görünüm ve sözlük). PATCH/DELETE/denetim kaydı gerektiren bir şey doğmadı. Yeni tek düğme (`#islem-son-hepsi`) gerçek bir durum değişikliği yapar (liste açılır/kapanır, test KAT_K4_02). Raporlar düzeltilemez değişmezliği (DegismezIpucu) aynı yerde korundu.

## 5. Migration
Gerekmedi (şema değişikliği yok).

## 6. Testler
* Yeni: `Kuyumcu.Tests/KatalogK1Testleri.cs` — 5 sınıf, 14 metot, **29 koşum**: KAT_K5_01 (6 veri) · KAT_K5_02 · KAT_K5_03 · KAT_K8_01 · KAT_K8_02 · KAT_K8_03 · KAT_K8_04 · KAT_K8_05 · KAT_HAM_01 (5 veri) · KAT_HAM_02 · KAT_HAM_03 · KAT_K4_01 (7 veri) · KAT_K4_02 · KAT_K4_03.
* Beklenen davranış değişikliğiyle güncellenen 5 eski test (ekranda "TRY" yazıyor diye doğruluyordu; kurucu kararıyla ₺): `S21FisBilesenTestleri` (Merkez (₺)), `P39TasarimKokleriTestleri` (son ek ₺), `KurulumEkranTestleri` (₺ var, TRY yok), `S36KasaBildirimTestleri` ("₺ Türk lirası" seçeneği; değerler hâlâ TRY/USD/EUR/GBP), `SatisAnaliziEkranTestleri` ("NAKIT" → "Nakit" kart etiketi).
* Filtreli: `Katalog` 39/39 (Katalog adlı eski testler dâhil) · Rapor+Katalog 154/154 · Sipariş/Bildirim/Vezne/Fatura 406/406 · Katalog+Sipariş+Denetim 297/297.
* **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_l`, trx `scratchpad/trx-pKAT-1/`): **5209 / 5209 geçti, 0 başarısız** (önce 5180 → sonra 5209 = +29). Devir notundaki kırmızı listesi boştu (S1-i: 5180/5180) → **bu paketin ürettiği yeni kırmızı: 0**. Ara koşumda (değişiklik sonrası ilk tam süit, `trx-pKAT-1-ara1`) 5200'den 5 kırmızı çıktı: yukarıdaki 5 eski test (TRY bekliyordu) — güncellendi.
* Derleme: `dotnet build Kuyumcu.sln` **0 hata**; tek uyarı `EtiketEkranTestleri.cs(145) xUnit1031` (başka pakete ait, S1-i § 15.6/3'te de kayıtlı; son derlemede 0 uyarı görüldü, artımlı derlemede ara ara çıkıyor).

## 7. Mutasyon kanıtları (yedek `scratchpad/mut-yedek/KAT-1/`, perl tek eşleşme → test → geri kopya → `cmp` AYNI)
| Kod | Değişiklik | Kırmızıya dönen testler | Geri alındı |
|---|---|---|---|
| MKAT-1 | `Bicim.ParaIsareti`: TRY dalı `kod ?? "TRY"` döndürür | KAT_K5_01 ("", "TRY", null) · KAT_K5_02 · P39 K7_Para_onizlemesi… · S21FisSaf FisHesap_bacak_kasasi… · KurulumEkran Kabul_edilmeyen_para_birimli… **+ TARAMA KIRMIZI** (mutant derlenip API'de sunuldu: /gider K5=1, /hurda K5=2, /raporlar/satis-analizi K5=2 — 4 kombinasyonun hepsinde) | cmp AYNI; temiz derleme + API yeniden başlatıldı |
| MKAT-2 | basit `Cari_Baslik: "Müşteriler"` → `"Cariler"` (basit anahtar pro metnine bağlandı) | KAT_K8_01 · KAT_K8_02 · KAT_K8_03 | cmp AYNI |
| MKAT-3 | `HareketTuru`: `"SATIS_IADE" => kod` (ham koda dönüş) | KAT_HAM_01(SATIS_IADE) · KAT_HAM_03 | cmp AYNI |
| MKAT-4 | `DilServisi.T` katmanı yok sayar | KAT_K8_03 · KAT_K8_04 | cmp AYNI |
| MKAT-5 | IslemDefteri tür hücresi `@s.Tur` (ham) | KAT_HAM_03 · KAT_K8_04 | cmp AYNI |
| MKAT-6 | `KartRotasi` R05 → sekmesiz rota | KAT_K4_01(R05) | cmp AYNI |
| MKAT-7 | son satışlar sınırı kaldırıldı | KAT_K4_02 | cmp AYNI |
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 uyarı 0 hata; tam süit ve tam tarama bu temiz ikiliyle koşuldu.

## 8. Canlı doğrulama (API 5174 · vekil 5173 · kuyumcu_net · test kiracısı `PAKETKAT-1-TEST Kuyumculuk` `01M39GCGP8683F152FJ6EV3DZY`)
Kiracı `betikler/demo-tohumla.js --prova-kiraci` ile ÜRÜNÜN UÇLARINDAN kuruldu (sistem anahtarı yalnız bu API sürecine verilen tek kullanımlık rastgele değer; ekrana yazılmadı): 6 cari, 12 ürün, 9 satış. Parametreli rotalar için ek veri yine API ile:
| İstek | HTTP | Yanıttan okunan |
|---|---|---|
| POST /api/v1/kasalar `{paraBirimi:"USD"}` | 200 | paraBirimi USD (döviz kodunun AYNEN kaldığını görmek için) |
| POST /api/v1/bankalar | 200 | id var |
| POST /api/v1/bankalar/{id}/ekstre/yukle (şablon CSV) | 200 | ithalatId, durum, satirSayisi… |
| POST /api/v1/siparisler (kalemsiz) | **400** | "Sipariş en az bir kalem içermeli" (gerçek kural) → kalemle tekrar **200** |
| POST /api/v1/islemler/satis-fisi (ALTIN bacaklı, NİHAİ TÜKETİCİ satıcı) | 200 | gider pusulası `GP-2026-000001` |
| GET /api/v1/kasalar | 200 | `[{Ana Kasa, TRY}, {Döviz Kasası, USD}]` — **sözleşme kodu değişmedi** |
| GET /api/v1/raporlar/islem-defteri | 200 | toplamSatir 26 · tür kodları HAM (HURDA_ALIS, SATIS, EMANET_GIRIS, TAHSILAT, ALIS, SAYIM_DUZELTME) · ödeme NAKIT/KART/KARMA/VERESIYE · paraBirimi ["TRY"] — çeviri yalnız EKRANDA |
| GET /api/v1/auth/ben | 200 | etkinArayuzModu BASIT · tenantId = test kiracısı |
| GET /api/v1/bankalar | 200 | ["TRY"] |
| PATCH /api/v1/kullanicilar/ben/arayuz-modu (her tarama fazında) | 200 | `data-mod` basit/profesyonel sayfada doğrulandı |
SQL (kuyumcu_net, kiracı süzgeçli): `kasalar` → (Ana Kasa, TRY), (Döviz Kasası, USD) · `hareketler` tür dağılımı ALIS 1, EMANET_GIRIS 1, HURDA_ALIS 1, SATIS 10, SAYIM_DUZELTME 12, TAHSILAT 1 · `banka_hesaplari` TRY 1 · `gider_pusulalari` 1 · `siparisler` 1 · `ekstre_ithalatlari` 1.
Not: yoğun ölçümde bir kez mod PATCH'i 429 döndü (kullanıcı başına 100/dk genel sınır) — o koşum mod zaten doğru olduğu için etkilenmedi; nihai taramada tüm mod geçişleri 200.

## 9. Gerçek tarayıcı ölçümü — TAM TARAMA
Araç: güncel `betikler/sikisma-olc.js` (`__olcTam`, K1–K10 + yatay) + ek sayaç: ana alanda görünür ham enum (`/\b[A-Z]{2,}_[A-Z_]{2,}\b/`) ve görünür "TRY" örneği. İki araç:
* (a) Başsız Chrome + CDP (`scratchpad/kat/kat-tara.mjs`): vekil 5173 üzerinden kendi oturumum; mod `PATCH …/arayuz-modu` + sayfa yenileme ile, tema uygulamanın kendi `kuyumcuTema.uygula()` işleviyle, genişlik `Emulation.setDeviceMetricsOverride` ile. **43 statik rota** (42 oturumlu + `/giris` oturumsuz) **+ 4 parametreli rota** × {1280×800, 375×812} × {BASİT, PRO} × {açık, koyu} = **372 ölçüm** (`scratchpad/kat/tam2.jsonl`, `giris.jsonl`).
  Parametreli rotalar LİSTEDEN TIKLANARAK açıldı: `/cikti/{Tur}/{Id}` ← işlem defteri "Belge aç" · `/gider-pusulalari/{Id}/belge` ← gider pusulaları "Yazdır" · `/siparisler/{Id}` ← sipariş "Detay". `/banka/ekstre/{IthalatId}` için uygulamada bir ithalat LİSTESİ yoktur (ekran yalnız ekstre yüklendikten hemen sonra `NavigateTo` ile açılır, Banka.razor:918) → yükleme yanıtındaki kimlikle aynı istemci rotasına gidildi (kayıt `oge-yok>dogrudan`).
  `/giris`: oturum açıkken panelle aynı yere yönlendirir; bu yüzden OTURUMSUZ ayrı tarayıcıda ölçüldü (mod oturumsuz ekranda uygulanmaz — tablo iki mod sütununa aynı değeri yazar, "(oturumsuz)").
* (b) Claude tarayıcı bölmesinde KENDİ sekmem (tab-38, vekil 5173, `javascript_tool` ile sikisma-olc.js): 1280 BASİT `/cariler /denetim-izi /raporlar/islem-defteri /gider /hurda /raporlar/satis-analizi /vezne /yetkiler` → hepsinde K1…K10, Y = 0; 375 BASİT koyu `/emanet /islemler /siparisler /` K7=0, `/raporlar/kasa` K7=1 (§ 9.3). Canlı mod geçişi (Basit→Pro→Basit, sayfa yenilemeden) `/cariler` ana alanında "Cari" sayısı **0 → 2 → 0**. Bölme gizli olduğu için ekran görüntüsü alınamadı ("pane is not displayed") — görüntüler (a)'dan.
* Ekran görüntüleri: `ham/KATALOG-ekran/` — her rota 1280 BASİT açık (**43 statik + 4 parametreli detay = 47 görsel**, `/giris` oturumsuz).

### 9.1 Sonuç tablosu (hücre: boy = sayfa yüksekliği / ekran; ardından yalnız SIFIR OLMAYAN sayaçlar)
| Rota | B-açık-1280 | B-koyu-1280 | P-açık-1280 | P-koyu-1280 | B-açık-375 | B-koyu-375 | P-açık-375 | P-koyu-375 |
|---|---|---|---|---|---|---|---|---|
| / | 1.3 | 1.3 | 1.7 | 1.7 | 1.8 | 1.8 | 2.7 | 2.7 |
| /islemler | 2.9 K4:2.9x5 | 2.9 K4:2.9x5 | 2.5 | 2.5 | 3.8 K4:3.8x5 | 3.8 K4:3.8x5 | 3.4 | 3.4 |
| /cariler | 1.3 | 1.3 | 1.3 | 1.3 | 1.3 | 1.3 | 1.4 | 1.4 |
| /urunler | 1.3 | 1.3 | 1.5 K3k:2 | 1.5 K3k:2 | 1.7 K7:1 | 1.7 K7:1 | 2.1 | 2.1 |
| /urunler/agac | 1.3 | 1.3 | 1.3 | 1.3 | 1.2 | 1.2 | 1.3 | 1.3 |
| /urunler/etiketler | 1.8 | 1.8 | 1.6 | 1.6 | 1.9 | 1.9 | 2 | 2 |
| /emanet | 1.3 | 1.3 | 1.2 | 1.2 | 1 | 1 | 1 | 1 |
| /siparisler | 1.3 | 1.3 | 1.2 | 1.2 | 1 | 1 | 1.1 | 1.1 |
| /hurda | 1.9 | 1.9 | 1.8 | 1.8 | 2.3 K3k:2 | 2.3 K3k:2 | 2.4 K3k:2 | 2.4 K3k:2 |
| /sayim | 1.3 | 1.3 | 1.3 | 1.3 | 1 | 1 | 1 | 1 |
| /kasalar | 1.5 | 1.5 | 1.5 | 1.5 | 1 | 1 | 1.3 | 1.3 |
| /vezne | 1.5 | 1.5 | 1.5 | 1.5 | 1 | 1 | 1.1 | 1.1 |
| /pos | 1.5 | 1.5 | 1.5 | 1.5 | 1.1 | 1.1 | 1.1 | 1.1 |
| /pos/tahsilatlar | 1.5 | 1.5 | 1.5 | 1.5 | 1.9 | 1.9 | 2 | 2 |
| /banka | 1.5 | 1.5 | 1.5 | 1.5 | 1.1 | 1.1 | 1.2 | 1.2 |
| /evraklar | 1.5 | 1.5 | 1.5 | 1.5 | 2.4 | 2.4 | 2.5 | 2.5 |
| /evraklar/takvim | 1.5 | 1.5 | 1.5 | 1.5 | 1.4 | 1.4 | 1.5 | 1.5 |
| /faturalar | 1.2 | 1.2 | 1.1 | 1.1 | 1.2 K7:1 | 1.2 K7:1 | 1.3 K7:1 | 1.3 K7:1 |
| /fiyatlandirma | 1.3 K3k:2 | 1.3 K3k:2 | 1.3 K3k:2 | 1.3 K3k:2 | 1 | 1 | 1 | 1 |
| /gider | 1.5 | 1.5 | 1.5 | 1.5 | 1.5 | 1.5 | 1.6 | 1.6 |
| /gider-pusulalari | 1.2 K3k:3 | 1.2 K3k:3 | 1.1 K3k:3 | 1.1 K3k:3 | 1.3 | 1.3 | 1.3 | 1.3 |
| /uyum | 1.2 | 1.2 | 1.1 | 1.1 | 1.4 | 1.4 | 1.5 | 1.5 |
| /raporlar | 1.8 | 1.8 | 2.3 | 2.3 | 2.7 K4:2.7x7 | 2.7 K4:2.7x7 | 4.6 K3k:4 | 4.6 K3k:4 |
| /raporlar/islem-defteri | 2.1 | 2.1 | 2.1 | 2.1 | 3 | 3 | 3 | 3 |
| /raporlar/kar-zarar | 1.7 | 1.7 | 1.7 | 1.7 | 3.3 | 3.3 | 3.3 | 3.3 |
| /raporlar/kasa | 1.7 | 1.7 | 1.7 | 1.7 | 2 K7:1 | 2 K7:1 | 2 K7:1 | 2 K7:1 |
| /raporlar/satis-analizi | 1.3 | 1.3 | 1.3 | 1.3 | 1.8 | 1.8 | 1.8 | 1.8 |
| /raporlar/vade-takibi | 1.6 | 1.6 | 1.6 | 1.6 | 2.2 | 2.2 | 2.3 | 2.3 |
| /bildirimler | 1.3 | 1.3 | 2.2 | 2.2 | 1 | 1 | 2.8 | 2.8 |
| /vitrin | 2.1 | 2.1 | 2.1 | 2.1 | 2.8 | 2.8 | 2.9 | 2.9 |
| /kur | 1.5 | 1.5 | 1.5 | 1.5 | 1.6 | 1.6 | 1.7 | 1.7 |
| /isletme | 1.4 | 1.4 | 1.4 | 1.4 | 1.2 | 1.2 | 1.3 | 1.3 |
| /kullanicilar | 1.4 K3k:1 | 1.4 K3k:1 | 1.4 K3k:1 | 1.4 K3k:1 | 1 | 1 | 1 | 1 |
| /yetkiler | 1.4 | 1.4 | 1.4 | 1.4 | 1.1 | 1.1 | 1.2 | 1.2 |
| /denetim-izi | 1.7 | 1.7 | 1.6 | 1.6 | 2.5 | 2.5 | 2.6 | 2.6 |
| /yedekler | 1.4 | 1.4 | 1.4 | 1.4 | 1.8 | 1.8 | 1.9 | 1.9 |
| /veri-disa-aktarim | 1.4 | 1.4 | 1.4 | 1.4 | 1.9 | 1.9 | 1.9 | 1.9 |
| /veri-ice-aktarim | 1.5 | 1.5 | 1.5 | 1.5 | 1.7 | 1.7 | 1.8 | 1.8 |
| /kurulum | 1.9 | 1.9 | 1.9 | 1.9 | 2.4 K4:2.4x4 | 2.4 K4:2.4x4 | 2.5 K4:2.5x4 | 2.5 K4:2.5x4 |
| /ayarlar | 1.5 | 1.5 | 1.5 | 1.5 | 2.7 K4:2.7x10 | 2.7 K4:2.7x10 | 2.8 K4:2.8x10 | 2.8 K4:2.8x10 |
| /ekranlar | 1.4 | 1.4 | 1.4 | 1.4 | 4.2 K4:4.2x6 | 4.2 K4:4.2x6 | 4.2 K4:4.2x6 | 4.2 K4:4.2x6 |
| /kilavuz | 2 | 2 | 2 | 2 | 2.2 | 2.2 | 2.2 | 2.2 |
| /cikti/SATIS_FISI/{Id} | 1.5 | 1.5 | 1.5 | 1.5 | 2 | 2 | 2 | 2 |
| /gider-pusulalari/{Id}/belge | 1.6 | 1.6 | 1.6 | 1.6 | 2.3 K4:2.3x5 | 2.3 K4:2.3x5 | 2.3 K4:2.3x5 | 2.3 K4:2.3x5 |
| /siparisler/{Id} | 1.3 | 1.3 | 1.3 | 1.3 | 1.5 | 1.5 | 1.6 | 1.6 |
| /banka/ekstre/{IthalatId} | 1.5 | 1.5 | 1.5 | 1.5 | 1.7 | 1.7 | 1.8 | 1.8 |
| /giris | 1 | 1 | 1 (oturumsuz) | 1 (oturumsuz) | 1.2 | 1.2 | 1.2 (oturumsuz) | 1.2 (oturumsuz) |

ÖLÇÜM SAYISI 372 TOPLAM {'K1': 0, 'K3t': 0, 'K3k': 44, 'K5': 0, 'K6': 0, 'K7': 10, 'K8': 0, 'K9': 0, 'K10': 0, 'Y': 0, 'ENUM': 0, 'K4': 22}

**Toplam (372 ölçüm): K1 0 · K3 taşan 0 · K3 kırık 44 (hepsi `th`) · K5 0 · K6 0 · K7 10 · K8 (BASİT) 0 · K9 0 · K10 0 · yatay 0 · ham enum 0 · K2<%70 0 · K4 22.**

### 9.2 Taban → sonuç
Taban (bu paketin kendi taban taraması, değişiklikten ÖNCE, `scratchpad/kat/taban.jsonl`, aynı kiracı, 43 rota × 8 = 344 ölçüm; parametreli rotalar tabanda yoktu): K3 taşan **16** · K3 kırık **120** · K5 **52** · K7 **40** · K8 (BASİT) **80** · K9 **4** · ham enum **8** · K4 **24** (/raporlar PRO 1280 7,1 ekran dâhil) · K1/K6/K10/yatay 0.
Sonuç (372 ölçüm): K3 taşan **0** · K3 kırık **44** (yalnız `th`) · K5 **0** · K7 **10** · K8 **0** · K9 **0** · ham enum **0** · K4 **22** (1280'de yalnız /islemler BASİT 2,9).

### 9.3 Sıfır olmayan hücrelerin ÖLÇÜLMÜŞ gerekçesi
1. **K3 kırık 44 — tamamı tablo başlığı (`th`)**, tek kelime kırılması **0**: /urunler PRO 1280 "ÜRÜN GRUBU", "HAS GRAM" · /fiyatlandirma 1280 "HAS MARJI (%)", "İŞÇİLİK ÇARPANI (%)" · /gider-pusulalari 1280 "BRÜT TUTAR", "NET ÖDENEN", "SON DÜZENLEME TARİHİ" · /kullanicilar 1280 "İKİ AŞAMALI" · /hurda 375 "Has gram", "Tahlil / mutabakat farkı" · /raporlar PRO 375 "Ödeme şekli", "Kasaya girer", "Satış toplamı", "Hareket sayısı". Hepsi kelime sınırında sarıyor (katalog kabulü: "iki kelimelik th kabul"; "SON DÜZENLEME TARİHİ" üç kelime, "(%)" birim eki). `white-space:nowrap` verilirse tablolar 1280'de yatay kaydırmaya girer — bilerek yapılmadı.
2. **K7 10 — hepsi 375 px, araç şeridinde ALT ALTA TAM GENİŞLİK öğeler** (sezgisel "üst farkı < 60 px = aynı sıra" iki satırı yan yana sayıyor). Kendi sekmemde ölçülen geometri (şerit 351 px): /raporlar/kasa: tarih [12,203,184,247] · tarih [192,203,363,247] (**yan yana, K7-b ile**) · kasa seçimi [12,255,363,299] (tam genişlik alt satır) — /faturalar: durum seçimi [12,222,363,266] · "Fişten Fatura Kes" [12,274,363,318] — /urunler: arama [12,222,363,294] · süzgeç [12,302,363,374] · düğme [12,382,363,426]. Hiçbirinde iki öğe aynı satırda değil; üçüncü öğeyi yan yana sığdırmak 375'te 115 px'lik kutular demektir (okunmaz). Görsel kusur değil.
3. **K4 22** (boy > 2 ekran ve ≥ 4 bölüm): 20 hücre 375 px'te (dar ekranda kartlar tek sütuna iner; S1-i § 9 kabulü: eşik 1280'de): /ayarlar 2,7–2,8 × 10 (S1-h sekmeli; bölüm sayısı sekme İÇİNDEKİ başlıklar), /ekranlar 4,2 × 6 (ekran kataloğu), /kurulum 2,4–2,5 × 4, /raporlar BASİT 2,7 × 7 (1280'de 1,3), `/gider-pusulalari/{Id}/belge` 2,3 × 5 (A4 belge, kâğıt düzeni), /islemler BASİT 3,8. **1280'de tek hücre çifti: /islemler BASİT 2,9 × 5** (açık/koyu) — taban 4,0'dı; son satışlar kartı 5 fişe indirildi (§ 2.4). Kalan boy fiş kartından (Verdim/Aldım 430 px + satış formu 952 px); iki sütunlu tezgâh fişi S1-i § 1(5)'te ölçülmüş gerekçe ve **S1-j**'ye devredilmiş iştir (orkestratör 13:50 gözlemi: "VERDİM sütunu dibe hizalı" da aynı iş).

## 10. Denetim
`dotnet/denetim.sh` → **DENETİM TEMİZ** (288 ✓, 0 ✗; son kodla, 14:4x). Yeni sayaç eklenmedi; mevcut sayaçların hiçbiri artmadı (ham hex, satır içi stil, ModalPencere düzenleme, S1-g hidden kuralı vb.).

## 11. Değişen dosyalar (bu paket)
`git status` bu ağaçta anlamlı değil (tek "first commit"ten sonra dosyaların çoğu izlenmiyor); liste oturumdaki düzenlemelerden:
* Yeni: `dotnet/Kuyumcu.Core/I18n/Sozlukler.Katalog.cs`, `dotnet/Kuyumcu.Tests/KatalogK1Testleri.cs`, `dokumanlar/09-uygulama-kayitlari/ham/KATALOG-ekran/*.png` (47), bu rapor.
* Değişen: `dotnet/Kuyumcu.Web/Servisler/Bicim.cs`, `Servisler/DilServisi.cs`, `Kuyumcu.Web/Program.cs`, `Bilesenler/Form/Para.razor`, `Bilesenler/Fis/FisModelleri.cs`, `Bilesenler/Fis/FisSonSatislar.razor`, `Bilesenler/AltinPozisyonuKarti.razor`, `Pages/{IslemDefteri,SatisAnalizi,Kasalar,Banka,Cariler,Gider,Vezne,Kurulum,SiparisDetay,Siparisler,Kilavuz,Raporlar,Hurda,Urunler}.razor`, `wwwroot/css/app.css` (yalnız dosya SONUNA KATALOG bloğu), testler `S21FisBilesenTestleri.cs`, `P39TasarimKokleriTestleri.cs`, `KurulumEkranTestleri.cs`, `S36KasaBildirimTestleri.cs`, `SatisAnaliziEkranTestleri.cs`, `dokumanlar/09-uygulama-kayitlari/ham/KURUCU-SIKAYET-KATALOGU.md` (sona "TAM TARAMA" bölümü).
* HUB `Sozlukler.cs`'e DOKUNULMADI. Migration yok. Proje ağacına geçici dosya bırakılmadı (mutasyon yedekleri scratchpad'de).

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| K5: ekranda görünen her TRY → ₺, tek merkez `Bicim.ParaIsareti`, USD/EUR/GBP aynen | SAĞLANDI | tarama K5 = 0 (372); USD kasası ekranda "USD"; KAT_K5_01..03 |
| Seçici okunur ad "₺ Türk lirası" | SAĞLANDI | Kasalar/Banka; S36_04 + KAT_K5_03 |
| Veri/sunucu/sözleşme kodu değişmez | SAĞLANDI | § 8 API + SQL |
| Kâğıt çıktılar dâhil | SAĞLANDI | `/cikti/SATIS_FISI/{Id}`, gider pusulası belgesi K5 = 0 |
| K8 basit mod jargonu 0, PRO'da "Cari" kalır, ayrı anahtar, HUB'a dokunulmaz | SAĞLANDI | BASİT K8 = 0 (184 BASİT ölçüm + 4 oturumsuz /giris); PRO "Cari" duruyor (canlı geçiş 0→2→0); KAT_K8_01..05 |
| Mevzuat metinleri korunur | SAĞLANDI | "MASAK" basit metinde de var; gider pusulası adları değişmedi |
| Ham enum görünür metinde 0 (islem defteri SATIS_IADE) | SAĞLANDI | ham enum sayacı 0 (372); KAT_HAM_01..03 |
| K7 kalanları ölç, sığıyorsa düzelt | SAĞLANDI | 40 → 10; kalan 10 ölçülmüş gerekçe § 9.3/2 |
| TAM TARAMA 43 × 8 + 4 parametreli (listeden tıklama) | SAĞLANDI | § 9.1 (372 ölçüm) — ekstre sayfasının listesi yok, gerekçe § 9 |
| 43 ekran görüntüsü 1280 BASİT açık | SAĞLANDI | `ham/KATALOG-ekran/` 47 görsel |
| Sonuç tablosu KATALOG md sonuna "TAM TARAMA — <saat>" | SAĞLANDI | `ham/KURUCU-SIKAYET-KATALOGU.md` |
| Sıfır olmayan her hücre düzeltilir ya da gerekçe | SAĞLANDI | § 9.3 |
| Özellik silinmez; DOM id ve rota değişmez; yetki ≠ mod | SAĞLANDI | tüm bölümler sekmelerde; yeni id'ler § 3; mod yalnız metni değiştirir |
| Sabit renk / satır içi stil yok; app.css yalnız sona | SAĞLANDI | denetim.sh TEMİZ |
| 3 zorunlu mutasyon (+ tarama kırmızısı) | SAĞLANDI | § 7 MKAT-1/2/3 (+4 ek) |
| İzolasyon: test VT _l, API 5174, vekil 5173, başka porta dokunulmadı | SAĞLANDI | 5179/5181/5199/5200'e istek yok |
| Test kiracısı silindi, tenants başlangıç sayısına döndü | SAĞLANDI | 70 tablo × kiracı = 0 satır; tenants 10 → 11 → **10**; davet satırı silindi; QA-TEST (4) dokunulmadı |

## 13. Açık kalanlar (sahipleriyle)
1. **/islemler BASİT 1280 boy 2,9** — tezgâh fişinin iki sütunlu düzeni (Verdim/Aldım üstten hizalı, orkestratör 13:50 gözlemi dâhil). Sahibi: **S1-j / S2-2**.
2. **Tablo başlıkları (`th`) kelime sınırında sarıyor** (44 hücre) — katalog kabulü içinde; kısaltılmış başlık sözlüğü istenirse ayrı iş (sahibi: Ertuğ / ürün dili).
3. Basit modda **küçük harfli** jargon (sayaç yalnız baş harfi büyük kalıbı sayar): /hurda "Tahlil / mutabakat farkı" sütun başlığı, `DogrulamaSozlugu ["CariId"] = "Cari"` (yalnız sunucu doğrulama hatası bandında görünür). Kapsam: bir sonraki okunabilirlik turu (Aşkın).
4. Ekstre ithalatlarının LİSTESİ yok (`/banka/ekstre/{IthalatId}` yalnız yükleme sonrası açılıyor) — geçmiş bir ekstreye dönüş yolu yok (UX-S25 açısından ürün sorusu). Sahibi: Taşkın/Ertuğ (Banka).
5. `EtiketEkranTestleri.cs(145) xUnit1031` uyarısı (başka paket, artımlı derlemede görünüyor).
6. 21 Eylül'den kalma 0 baytlık `.mutorj`/`.bak` artıkları ve `Migrations` altındaki 0 baytlık dosyaların silinmesi kurucuda (hook silmeyi engelliyor) — bu paket yeni artık bırakmadı.

## 14. Kurucuya / mali müşavire not
* Mevzuat hükmü yazılmadı/değiştirilmedi. Basit modda "Storno" yerine "Geri al (düzeltme fişi)" yazıyor; muhasebe kaydı ve belge adları (storno fişi, gider pusulası) değişmedi — mali müşavir ekranı PROFESYONEL modda aynı terimlerle görür.
* Döviz kodları (USD/EUR/GBP) kurucu kararı gereği kod olarak kaldı; yalnız Türk lirası ₺ işaretiyle yazılıyor.
