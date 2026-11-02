# PAKET DIL-SUNUCU — uygulama raporu (09.10 · t2 · 5162/5163)

Durum: **BITTI** (hasımsız paket; kendi kanıtım bağlayıcı). Commit yok. Dosya silme yok (yalnız kendi test kiracım komutla silindi). Paket 1 tur aldı.
Taşkın (backend) + Ertuğ (frontend) rolü, Opus. Kurucu bulgusu (08.10): "ingilizceye cevirdigimizde ... turkce kaliyor" — SINIF kapatıldı: sunucunun ekrana giden cümlesi artık istek diline göre sözlükten seçilir.

## 1. Karar (ekip oyu) — TEK YOL: sunucu `Accept-Language`'e göre sözlükten seçer
İki yol vardı: (a) sunucu kod+parametre döner, istemci çevirir; (b) sunucu istek diline göre sözlükten seçer. **(b) seçildi**, çünkü mevcut desen (GirdiKapisi / GOC-A: `GirdiSozlugu.Mesaj(hata, dil)`) zaten "sunucu sözlükten, dil parametresiyle" idi; 1.390+ ileti yerinin hepsini (a) yoluyla yeniden tasarlamak sözleşmeyi (message/error/statusCode/alanlar) kırardı. İki yol karıştırılmadı: kod+parametre yolu AÇILMADI.

Mekanizma:
- `Kuyumcu.Core/I18n/IstekDili.cs` — AsyncLocal "tr|en" (varsayılan **tr**: başlık yok / tanınmıyor / arka plan işi = eski davranış BİREBİR).
- `Kuyumcu.Api/Common/DilAraci.cs` — `Accept-Language` → IstekDili; yanıta `Content-Language` (yalnız /api). **UseRateLimiter, kimlik doğrulama, IstekKimligiAraci, HataYakalayici'dan ÖNCE** kayıtlı (DS18 sırayı kilitler).
- `Kuyumcu.Web/Servisler/ApiIstemci.cs` — her istekte arayüz dilini `Accept-Language: en|tr` olarak yazar (tarayıcının kendi dil başlığı BİLEREK ezilir: TR uygulama + İngilizce tarayıcı karışmasın). Kanıt: canlı tarayıcıda sayfanın tüm /api çağrıları `en` taşıdı.
- `Kuyumcu.Core/I18n/SunucuIletiSozlugu.cs` (motor) + `SunucuIletiSozlugu.Veri.cs` (**1244 TR→EN çifti**, `{}` yer tutuculu): tam eşleşme + şablon (regex, daha özgül şablon önce); yakalanan değer: bilinen kod/etiket çevrilir (ACIK→OPEN …), iç içe cümle ve " · " listeleri parça parça çevrilir, **veri (müşteri/ürün/şube adı, tutar) çevrilmez**; eşleşme yoksa cümle AYNEN (çeviri uydurulmaz).
- **Tek çıkış noktaları**: `HataYakalayici.Yaz` (tüm ApiException ailesi: 400/401/403/404/409/413/500, `alanlar` altı mesajlar dahil), JWT `OnChallenge` 401, `/api` 404 fallback, 429 oran sınırı (2 yer), doğrulama 400 fabrikası.
- Doğrulama kolu: `DogrulamaSozlugu` `partial` + `DogrulamaSozlugu.En.cs` (alan etiketleri EN, 15 DataAnnotations deseni EN, model bağlama erişimcileri istek anında dile bakar, JSON sayı hatası EN).
- Mevcut "sabit TR" kalıntıları istek diline bağlandı: ~25 yerde `GirdiSozlugu.Mesaj(…, "tr")`, `RaporSuzgecKurali.Mesaj("tr")`, `RaporDonemKurali…("tr")`, `KurBantSozlugu.Tr`, `AuthSifirlaSozlugu.Tr`, `UrunAcilisSozlugu.Tr`, `CiftBacakSozlugu.Tr` (statik alan → özellik), `GocASozlugu.Tr`, `Sozlukler.Tr.Pos`, `RolFiltresi` (`KesifP1Sozlugu.Tr`). DS17 bu sınıfın geri dönmesini kaynak taramasıyla engeller.
- Uyarı/başarı/not cümleleri: `RaporModulu` ve `StokYaslandirmaRaporu` `uyarilar` (gün sonu / mutabakat / yaşlandırma), 46+ `mesaj|uyari|hata|duzeltmeYolu|jetonUyarisi|giderNotu|notu|farkTanimi|tanim` yanıt alanı (POS, Fatura önizleme, Disa aktarım, Yedek, Kullanıcı, Şube, Tercih, Yetki …), sürüm `uyari`, parola sıfırlama mesajları → `SunucuIletiSozlugu.Cevir(…)` ile sarıldı.
- Birleştirmeyle yazılan parçalı cümleler tek tam cümleye çevrildi (sözlükte karşılığı olsun diye): Kasa kapanış farkı (4 tam cümle, `KasaServisi.KapanisFarkIletisi`), Kredi limiti aşımı (2 cümle), Rezerve adet, Maliyet yöntemi, Hurda alış, Stopaj ödemesi, Banka eşleştirme, Ekstre başlığı.
- Künye satırı (`SurumServisi.TekSatir(k, dil)`): TR `surum=… · derleme=… (dosya) · ortam=…` ESKİSİYLE aynı; EN `version=… · build=… (file) · environment=… · migrations=… · last=… · pending=…`. Ayarlar.razor `Dil.Dil` geçirir.
- Ham kodlar: `/raporlar/satis-analizi` "DIGER" → `KatalogSozlugu.OdemeAraci` (Diğer/Other); `/yedekler` ham `TAM · ELLE · BASARILI · BASARISIZ · BEKLIYOR …` → `DilSunucuSozlugu.KodAdi` (renk/mantık kodla çalışmaya devam eder, yalnız görünen ad çevrilir).
- DIL-EKRAN hakem artıkları: Banka "Yeni hesap" yer tutucuları ("Ziraat Bankası"/"Ana Hesap") sözlükten; `Cari.Baslik` EN "Accounts"→"Customers" (document.title ile h1 aynı ad).

## 2. Sapmalar ve gerekçeleri
1. **Anahtar = TR cümle şablonu** (kod değil): gerekçe yukarıda (mevcut desen, sözleşme korunur). Denetim sayacı bu yüzden kaynak literalini sözlükteki şablonla eşler.
2. **"Önce" ölçümü**: eski kod geri alınamaz (git checkout/stash yasak). "Önce" = AYNI yeni derleme, ama vekil `Accept-Language`'i `tr`'ye eziyor (`vekil-once.py`, proje betiği KOPYASI, orijinale dokunulmadı) → sunucu davranışı eski kodla bayt bayt aynı. Künye için "önce" DIL-EKRAN raporundaki (tur 2) sabit Türkçe anahtarlar.
3. **Sayaç tanımı genişledi** (aşağıda §6): `new *Exception(...)` yanında `uyarilar.Add`, `eksikler.Add`, `ErrorMessage=`, `message|mesaj|uyari|hata|…=`, `const string` iletileri de sayılır (SQL sabitleri hariç). Çalışırken kapsam genişledikçe BAŞLANGIÇ değeri de yeniden ölçüldü: 1531 yer.
4. Yeni varlık/kayıt türü açılmadı → **DoD sözleşmesi: uygulanamaz** (PATCH/DELETE/yetki/denetim ucu eklenmedi; yalnız ileti metni).
5. **Dokunulan sayfalar** (tek dokunuş kuralı): `Banka.razor` (2 yer tutucu), `Ayarlar.razor` (künye çağrısı), `Yedekler.razor` (4 ham kod hücresi) — hepsi YALNIZ ileti gösterimi; gerekçe: dil sınıfı, başka dalgayla çakışan alan değil. GOC-C alanındaki `SiparisModulu.cs`'ye DOKUNULMADI; yalnız onların yeni eklediği bir cümle için SÖZLÜĞE satır eklendi (sayaç).
6. Para biçimi sunucu cümlesinde tr-TR kaldı ("99.339,60 ₺"): istemcideki `Bicim.Tl` EN modda da aynı biçimi basıyor; ikisini ayrıştırmamak için DOKUNULMADI (kurucu kararı gerekir).

## 3. Migration
**gerekmedi** (şema değişmedi). Kiracı tablosu/varlık yok.

## 4. Test sayıları
- Önce (DIL-EKRAN raporu, tam süit): 11364 · Şimdi tam süit: **11616** (trx `/private/tmp/kuyumcu-serit/trx-pDIL-SUNUCU/`): çalışan 11614, geçti 11611, **başarısız 3**, atlanan 2. Başkalarının testleri de bu aralıkta arttı; bu paketin yeni testi: **35** (`Kuyumcu.Tests/DilSunucuTestleri.cs`, 35/35 yeşil).
- Dağılım: saf 27 (DS01–DS19 + DS07b; DS08 8 satırlı Theory), DB 1 (DS20 gün sonu uyarısı TR/EN), HTTP 7 (DS30–DS36: aynı hata EN→İngilizce / TR→Türkçe, **aynı alan anahtarı, aynı status, aynı `error`**; 401 token yok/bozuk, 403 rol, 400 doğrulama + girdi kuralı, 404 cari/uç, servis içi 400).
- Tam süitteki 3 kırmızı (**hepsi başkasının**, bu paketin yeni kırmızısı **0**): `GOCC2SinirTur3Testleri.DG31_…asim_SESSIZ_kalmaz` (beklenen 25000, gelen 10000 — GOC-C sayısal sınır); `GocC2EkranTestleri.GC2T03_urunler_PRO_barkod_…_kart_iki_sutun` (CSS regex — GOC-C); `SATIS3EkranTestleri.T17_Satis_giris_satiri_tus_tus_1_500_kalem_EKLENMEZ…` (beklenen "Ondalık için virgül kullanın…", gelen "Tam sayı girin, nokta kullanmayın" — SayiGirisi/SATIS lanesi). Üçü de dil/sözlük/başlık yoluna değmiyor (hata iletileri yukarıda). DIL-EKRAN devir notundaki OL16/OL26/Yasal4Http kırmızıları bu koşumda YEŞİL.
- Derleme: `dotnet build Kuyumcu.sln` **0 hata 0 uyarı** (son mutasyondan sonra temiz derleme; build7).

## 5. Mutasyon tablosu (hepsi geri alındı, `cmp` AYNI; her mutasyon filtreli `DilSunucu` koşumu)
| Kod | Dosya | Değişiklik | Kırmızı | Kırmızı testler |
|---|---|---|---|---|
| M1 | ApiException.cs | `Yaz()` hata iletisini çevirmez | 2 | DS30, DS35 |
| M2 | DilAraci.cs | Accept-Language yok sayılır | 7 | DS30–DS36 |
| M3 | SunucuIletiSozlugu.cs | şablon eşleşmesi kapalı | 7 | DS02, DS04, DS06, DS07, DS12, DS20, DS35 |
| M4 | RaporModulu.cs | gün sonu uyarıları çevrilmez | 1 | DS20 |
| M5 | ApiIstemci.cs | istemci dil başlığı hep `tr` | 1 | DS14 |
| M6 | SurumServisi.cs | künye etiketleri hep `tr` | 1 | DS11 |
| M7 | SunucuIletiSozlugu.cs | iç içe cümle çevrilmez | 1 | DS07b (ilk koşumda DS07 yetmedi → TEST AÇIĞI bulundu, DS07b eklendi, tekrar kırmızı) |
| M8 | KasaModulu.cs | kasa fark cümlesi sözlükten ayrışır | 1 | DS12 |
| M9 | RolFiltresi.cs | 403 rol iletisi hep `Tr` | 2 | DS32, DS17 |
| M10 | DogrulamaSozlugu.cs | doğrulama alan etiketi EN kolu yok | 1 | DS13 |
| M11 | GirdiOznitelikleri.cs | girdi kuralı iletisi hep `"tr"` | 2 | DS36, DS17 |
| M12 | Program.cs | 401 token iletisi çevrilmez | 1 | DS31 |
| M13 | IstekDili.cs | dil çözümü "en" tanımaz | 18 | DS02–DS13, DS20, DS30–DS36 |
| M14 | Sozlukler.Katalog.cs | ham kod DIGER çevrilmez | 1 | DS15 |
| M15 | DilSunucuSozlugu.cs | yedek ham durum kodu EN'de çevrilmez | 1 | DS19 |
| M16 | SunucuIletiSozlugu.Veri.cs | sözlükten 1 satır silinir (denetim mutasyonu) | `denetim.sh` ✗ | SUNUCU_CIPLAK_TURKCE_ILETI 17 yer (tavan 0) → geri alınca ✓ |

Dürüst not: ilk ardışık koşum harness'ında geri yüklenen kaynağın ESKİ mtime'ı (`copy2`) yüzünden bayat derleme çıktı (M5–M9 kırmızıları bir önceki mutasyona karıştı); harness düzeltildi (`copyfile` + `utime`), tüm mutasyonlar baştan koşuldu. M13 ardışık koşumda bir kez daha bayat çıktı (0 kırmızı) → **tek başına** yeniden koşuldu (18 kırmızı). Sonuç tabloları yalnız düzeltilmiş koşumdan.

## 6. Denetim (`dotnet/denetim.sh`)
Yeni sayaç **SUNUCU_CIPLAK_TURKCE_ILETI**: Api'de kullanıcıya giden ileti yerlerinde (new *Exception / uyarilar.Add / eksikler.Add / ErrorMessage= / mesaj|uyari|hata|…= / `const string` iletileri) Türkçe harfli ya da ASCII Türkçe kök içeren (zorunlu, olamaz, bulunamad …) literal'lerden `SunucuIletiSozlugu.Veri.cs`'de KARŞILIĞI olmayanların sayısı.
- **Başlangıç (sözlük boş olsaydı): 1531 ileti yeri → şimdi 0** (tavan 0, fail-closed; yeni ileti yazan sözlüğe de satır eklemek zorunda). Bu paketin sıfırladığı sınıflar: gün sonu/mutabakat/yaşlandırma uyarıları, tüm 400/401/403/404/409 iletileri (Yaz yolu), doğrulama/girdi iletileri, başarı `mesaj`ları, künye, ham kodlar.
- Çıktı: `✓ DIL-SUNUCU: SUNUCU_CIPLAK_TURKCE_ILETI 0` · `✓ DIL-EKRAN: EN_MODDA_TURKCE_BASLIK 0` · yalnız `✗ P-30: DEMO_INSAN_SURESI` (kapsam dışı, P-30 haric) → **DENETİM TEMİZ (P-30 hariç)**.
- Sayaç parser'ı denetim.sh içinde (python; yorum satırı, string içi `;`, `//` yorum içi tırnak, `{0:N2}` format kalıbı, `$"…{string.Join(", ", x)}…"` iç içe tırnaklar doğru ele alınır).

## 7. Canlı doğrulama (5162; vekil 5163; gerçek başsız Chrome; test kiracısı `PAKETDILSUNUCU-TEST Kuyumculuk`, demo-tohumla `--prova-kiraci`: 6 cari / 12 ürün / 9 satış)
**API (curl, yanıtlar okundu)** — aynı hata, iki dil:
| Senaryo | TR | EN | alanlar | error / status |
|---|---|---|---|---|
| 401 token yok | Token bulunamadı | Token not found | — | Unauthorized 401 |
| 401 yanlış parola | E-posta veya şifre hatalı | E-mail or password is wrong | — | Unauthorized 401 |
| 404 cari yok | Cari bulunamadı | Customer not found | — | Not Found 404 |
| 404 uç yok | Uç bulunamadı | Endpoint not found | — | Not Found 404 |
| 400 doğrulama (ad boş) | ["Ad zorunludur"] | ["The name is required"] | `ad` = `ad` | Bad Request 400 |
| 400 girdi kuralı (ad "1") | ["En az 2 karakter yazın."] | ["Type at least 2 characters."] | `ad` = `ad` | Bad Request 400 |
| 400 servis içi (sıralama) | Bilinmeyen sıralama alanı: yokBoyleAlan. Geçerli: … | Unknown sort field: yokBoyleAlan. Valid: … | — | Bad Request 400 |
| 409 aynı vergi no | '1234567890' vergi/TC numarası bu işletmede başka bir AKTİF cari kartta kayıtlı | The tax/national ID number '1234567890' is already registered on another ACTIVE customer card in this business | `vergiNo` = `vergiNo` | Conflict 409 |
| 403 TEZGAHTAR → SAHİP ucu | Bu işlem için gerekli rol: dükkân sahibi | Role required for this action: the shop owner | — | Forbidden 403 |
| 429 oran sınırı | Çok fazla istek gönderildi, lütfen biraz bekleyin | Too many requests were sent, please wait a moment | — | TooManyRequests 429 |
| Gün sonu uyarısı | 9 satış faturalandırılmamış (toplam 99.339,60 ₺) | 9 sales not invoiced (total 99.339,60 ₺) | — | 200 |
Yanıt başlığı `Content-Language: tr|en` (yalnız /api).
**SQL kanıtı** (`kuyumcu_net`): `hareketler WHERE tur='SATIS'` → **9 satır / 99.339,60 TL**, `faturalar` → **0** — "9 sales not invoiced (total 99.339,60 ₺)" cümlesi veriyle birebir. Test kiracısı komutla silindi (`kiraci-temizle.sh <id> --uygula`): kiracı satırı 0, hareketler 0, kullanıcılar 0. **tenants: başlangıç 19 → silme sonrası 18** (aradaki fark başka şeritlerin kendi test kiracılarını eklemesi/silmesi; benim kiracım listede YOK). API (5162) ve vekil (5163) kapatıldı, `lsof` boş.

**Tarayıcı — sunucu kaynaklı Türkçe düğüm, önce → sonra (EN, 1440, açık; 42 rota + sekmeler, veri dolu kiracı):**
| Ölçüm | Önce | Sonra |
|---|---|---|
| PRO Raporlar > "End of day" uyarı bandı | `9 satış faturalandırılmamış (toplam 99.339,60 ₺)` | `⚠ 9 sales not invoiced (total 99.339,60 ₺)` |
| Sunucu kaynaklı Türkçe CÜMLE düğümü (PRO/BASİT) | 1 / 0 | **0 / 0** |
| Ham kod (DIGER, TAM, ELLE, BASARILI, BASARISIZ) — `/raporlar/satis-analizi`, `/yedekler`, BASİT+PRO | 5 tekil kod | **0** (kalan yalnız ISO para kodu USD/EUR/GBP — uluslararası, çevrilmez) |
| Ayarlar künye satırı (`#ayar-kunye-satir`) | `surum=… · derleme=… (dosya) · ortam=… · migration=…` | `version=1.0.0 · build=… (file) · environment=Development · migrations=91 · last=…` |
| Accept-Language (sayfanın /api çağrıları) | tarayıcı dili | **`en`** (TR modda `tr`) |
| Taranan tekil metin (PRO / BASİT) | 102 / 88 | 101 / 88 — kalanın hepsi VERİ/MARKA: müşteri/ürün/şube adları (Kemal Ardıç, 22 Ayar Çeyrek Altın …), seed adları (Peşin Müşteri, Ana Kasa, Merkez Şube → md.17), marka "ByGatech", denetim izi `title` ham kayıt (DIL-EKRAN gerekçeli) |
**TR modda İngilizce kalıntı: 0** (PRO+BASİT 1440; yalnız "Open-Meteo (CC BY 4.0)" lisans atfı ve Türkçe "Transfer" sözcüğü).

**Ölçüm (sikisma-olc K1..K12; 5 rota: /raporlar /ayarlar /banka /cariler /kasalar) — her kombinasyonda KAÇ İHLAL:**
| Genişlik | Mod | Tema | EN | TR |
|---|---|---|---|---|
| 1440 | BASİT | açık | 0 | 0 |
| 1440 | BASİT | koyu | 0 | — |
| 1440 | PRO | açık | 0 | 0 |
| 1440 | PRO | koyu | 0 | — |
| 375 | BASİT | açık | 0 | — |
| 375 | BASİT | koyu | 0 | — |
| 375 | PRO | açık | 0 | — |
| 375 | PRO | koyu | 0 | — |
Her satırda K1 gizli-görünen 0 · K3 taşan/kırık 0 · K5 TRY 0 · K7 hizasız 0 · K8 jargon 0 · K9 binen 0 · K10 elipsis 0 · K11 hata bandı 0 · yatay kaydırma 0 · K2 dar sütun ≥ %97 (en geniş blok ana alanın %97–116'sı). **Tek sapma, TÜM kombinasyonlarda ve TR'de de AYNEN: `/ayarlar` K12=7 (düğme dikey) — önceden var, dil/sunucu ileti değişikliğiyle ilgisi yok.** Not: ölçerin "mod" sütunu EN'de BASİT'i "PRO" yazar (araç "Basit" düğmesini TR metinle arıyor); gerçek mod `aria-pressed` ile doğrulandı. Tema kanıtı: açık `bg=rgb(242,244,247)`, koyu `bg=rgb(14,18,25)` (açık koşular `prefers-color-scheme: light` emülasyonu ile; ilk matris sistem koyuyla açık görünüp yeniden koşuldu). 1280 genişliği ayrı koşulmadı: ölçüm 1440 + 375 (CSS/yerleşim DEĞİŞMEDİ; yalnız metin).
**Ekran görüntüleri (Read 4/4):** `/private/tmp/kuyumcu-serit/DIL-SUNUCU/foto/` — `gunsonu-en-PRO-aydinlik-1440.png` (sarı bant, tek satır, taşma yok), `gunsonu-en-PRO-karanlik-375.png` (bant iki satıra sarar, kesilmez), `kunye-en-PRO-aydinlik-1440.png`, `kunye-en-PRO-aydinlik-375.png` (Ayarlar EN, görünüm kartları). Künye SATIR metni görüntüde değil DOM'dan okundu (`hata-en-*.json`).

## 8. Değişen dosyalar (bu paketin; `git status` tüm ağacı "untracked" gösterdiği için işaretle bulundu)
**Yeni:** `Kuyumcu.Core/I18n/IstekDili.cs` · `SunucuIletiSozlugu.cs` · `SunucuIletiSozlugu.Veri.cs` (1244 çift) · `DogrulamaSozlugu.En.cs` · `DilSunucuSozlugu.cs` · `Kuyumcu.Api/Common/DilAraci.cs` · `Kuyumcu.Tests/DilSunucuTestleri.cs`.
**Değişen (ileti/dil):** Core: `DogrulamaSozlugu.cs`, `Sozlukler.cs` (Cari.Baslik EN), `Sozlukler.Katalog.cs` (OdemeDiger). Api: `Program.cs`, `Common/{ApiException,OranSiniri}.cs`, `Common/Dogrulama/{GirdiKapisi,GirdiOznitelikleri}.cs`, `Auth/RolFiltresi.cs`, `Modules/` Admin, Auth(Auth/Mfa/ParolaSifirlama), Banka(Banka/Ekstre/EkstreAyristirici), Cari, Denetim, Disaaktarim, Fatura, Gorunum(2), Hurda(Hurda/GiderPusulasiKapisi/Stopaj), Iceaktarim(2), Isler, Kasa, Kullanici, Kur, Maliyet/KarZararServisi, Meta/SurumModulu, Pos, Rapor(RaporModulu/RaporOrtak/StokYaslandirmaRaporu), Satis(SatisModulu/P21/StornoServisi), Sistem, Sube, Tercih, Urun(Kategori/Urun), Vade/VadeKapisi, Yedek, Yetki. Web: `Servisler/{ApiIstemci,SurumServisi}.cs`, `Pages/{Ayarlar,Banka,Yedekler}.razor`. `dotnet/denetim.sh` (yalnız yeni sayaç).
Değişiklik biçimi: ileti satırları (`"tr"`→`IstekDili.Gecerli`, `Cevir(…)` sarmalama, parça birleştirme→tam cümle); iş kuralı, para, veri, yetki DEĞİŞMEDİ. Hasımlı pakete devir gerekmedi.

## 9. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| EN taramada sunucu kaynaklı Türkçe düğüm 0 (veri/kâğıt hariç), önce/sonra | ✓ | §7 tablo |
| TR modda İngilizce kalıntı 0 | ✓ | §7 |
| HTTP: aynı hata EN→İngilizce, TR→Türkçe, aynı alan anahtarı | ✓ | DS30–DS36 + canlı tablo |
| Mutasyon ≥3 kırmızı, cmp AYNI | ✓ | 15 mutasyon + 1 denetim mutasyonu |
| Denetim yeni sayaç azaldı | ✓ | 1531 → 0 |
| build 0/0 · denetim TEMİZ (P-30 hariç) · tam süit 0 yeni kırmızı | ✓ | build7; denetim2.log; trx (3 kırmızı başkasının) |
| Para/veri/yasal davranış DEĞİŞMEDİ | ✓ | yalnız ileti metni; kâğıt (fiş/fatura/gider pusulası) TR kalır (`CiktiModulu` DOKUNULMADI) |
| Gerçek tarayıcı 1440+375 · BASİT+PRO · açık+koyu | ✓ | §7 ölçüm tablosu + 4 görüntü |
| Test kiracısı silindi | ✓ | §7 SQL |
| Tam süit TEK kez · Read ≤4 | ✓ | 1 koşum · 4 görüntü |

## 10. Açık kalanlar
1. **Sayaç kör noktaları (dürüst):** değişkenden/yardımcıdan dönen ileti (`Kasa` 4 cümle yardımcısı, `KolonTavani.EnAzTutarMesaji`, `SistemCarisi` sabitleri dolaylı) yalnız TEST + sözlük girdisiyle korunur; çok parçalı birleştirme ve `string.Format` ile kurulan yeni cümleler sayacı ancak literal görünürse yakalar.
2. **Sözlük çevirileri elle yazıldı**; yasal/mevzuat atıflı cümleler (VUK md.234, KVKK md.12, Kuyum Ticareti Yönetmeliği md.11/(e), MASAK) EN karşılığıyla arayüz iletisidir, belge/mevzuat metni DEĞİLDİR — mali müşavir/hukukçu EN metni gözden geçirmeli. Mevzuat hükmü eklenmedi/değiştirilmedi.
3. Sunucu cümlesindeki tutar biçimi tr-TR (istemci `Bicim.Tl` ile tutarlı); EN'de "₺99,339.60" isteniyorsa her iki taraf birlikte değişmeli (kurucu kararı).
4. BASİT modda `document.title` ≠ h1 (EN ve TR'de de, ör. "Orders" ↔ "Orders and repairs") — mod adı sınıfı, dil sınıfı değil; kapsam dışı.
5. `/ayarlar` K12=7 (önceden var, dil bağımsız).
6. Tam süitteki 3 kırmızı sahipleriyle: DG31 + GC2T03 → **GOC-C (t1)**; T17 → **SATIS-3/SayiGirisi şeridi**.
7. `SiparisModulu.cs:907` (GOC-C) yeni Türkçe iletisi için sözlüğe ben satır ekledim; GOC-C sahipliği aynen onlarda.

## 11. Kurucuya / mali müşavire notlar
- **md.17 (karar bekliyor):** seed/veri adları (Peşin Müşteri, Ana Kasa, Merkez Şube) EN modda Türkçe görünür — bu paket veri çevirmez; ayrı karar.
- Yasal belge kâğıtları (fiş, fatura, gider pusulası) Türkçe kalır (yasal zorunluluk) — kâğıt muaf.
- EN iletilerdeki yasal atıf cümleleri için mali müşavir teyidi önerilir (§10/2); mevzuat sabiti yazılmadı.
- Tutar biçimi kararı (§10/3).
