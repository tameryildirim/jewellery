# PAKET KESIF-P5 · SATIŞ-VEZNE + SATIS-4 — uygulama raporu (01.10.2026, t5 · 5166/5167)

Uygulayıcı: Taşkın (backend) + Ertuğ (frontend), Opus. Tur sayısı: **tur 1 (uygulama + iç düzeltme) + tur 2 (doğrulayıcı/hakem bulguları, § 0)**. COMMIT YOK.

## 0. TUR 2 (01.10 03:50–04:50) — doğrulayıcı + tasarım hakemi bulguları kapatıldı

Bu bölüm tur 1 raporunun ÜSTÜNE yazıldı; aşağıdaki § 1–14 tur 1 kaydıdır, tur 2'de değişen satırlar orada da güncellendi.

| Bulgu | Önem | Ne yapıldı | Kanıt |
|---|---|---|---|
| PRO kart satışı kaydedilince boş fişte (0 ₺) "Yasal kapılar — 30.000 ₺ nakit kuralı: **Aşıldı**" kırmızı kalıyordu (0 ₺ kapısı gerilemesi) | KRİTİK (hakem) | Kök neden: Kaydet sonrası sıfırlama `proTevsik` önizlemesini (önceki fişin `islemTutariKuraliDevrede=true`) taşıyordu; Vazgeç yolu sunucuya yeniden soruyordu, Kaydet yolu sormuyordu. Çözüm `Islemler.Satis4.cs › ProYasalBosFis()`: kayıttan sonra boş fişte bu bayrak düşer — sunucunun ifadesiyle birebir (`TevsikController`: devrede = `|fisToplamTl| > had`, 0 ₺'de devrede DEĞİL); had/kalan aynen kalır, ilk kalemde `TevsikOnizle` sunucudan yeniden sorar. **İstek atılmaz**: ilk denemede sunucuya yeniden sormayı (`TevsikOnizle(yalnizHad)`) seçtim; tam süit `YASAL2FisTaksitEkranTestleri.Pesin_KART_satista_kayit_sonrasi_sorgu_ATILMAZ_satir_yok` (YASAL-2 sözleşmesi: kayıttan sonra /tevsik/gunluk sorgusu yok) kırmızı verdi → istek atmayan yola dönüldü. İlk denediğim `ToplamTl > 0` koruması da `TS2TuvalProTestleri.Yasal_kapilar_sunucu_devrede_derse_Asildi`'yi kırdı (sunucu boş fişte "devrede" derse kart bunu göstermeli) → geri alındı; `Islemler.Pro.cs` net DEĞİŞMEDİ | bUnit `SATIS4a_PRO_kart_satisi_KAYDEDILINCE_bos_fiste_yasal_kapi_ASILDI_YOK` (sahte sunucu gerçeğiyle aynı: kural fiş tutarına göre; önce Nakit'te "Aşıldı" görülür, Kart'la kaydedilir, boş fişte "Aşıldı" 0 ve kırmızı sınıf 0). Canlı başsız Chrome: PRO 1440 açık (Derya, 145.665,35 ₺ Kart) ve PRO 1280 koyu (Tuna, 145.731,87 ₺ Kart) → 30 sn sonra panel **"Yasal kapılar uygun"**, `#islem-pro-nakit` sınıfı `metin-yesil`, fiş 0 ₺. Görüntüler `olc2/ky-PRO-aydinlik-1440/foto/satis4-kayit-30sn__PRO_aydinlik_1440.png`, `olc2/ky2-PRO-karanlik-1280/foto/satis4-kayit-30sn__PRO_karanlik_1280.png` (Read ile görüldü) |
| (a) dolu fişte tutar eşiğin altına inince otomatik Kart'ın Nakit'e dönmesi TESTSİZ (hayatta kalan mutasyon) | ORTA | Test düzeltildi: fiş boşaltılmıyor; iki kalem (20.000 + 20.000 = 40.000 > had → Kart) → ikinci kalem silinir, fiş DOLU kalır (20.000) → Nakit, `#islem-odeme-otomatik` yok, BASİT'te ödeme seçicisi yeniden gizli ve "Nakit" satırı görünür. (BASİT'te adet düzenleme alanı yok; adet 2→1 yerine 2 kalem→1 kalem aynı dalı sınar) | M1-1c artık KIRMIZI (aşağıda) |
| PRO'da otomatik Kart notu kapalı ödeme hapının içinde kalıyordu | DÜŞÜK | Not PRO "Yasal kapılar" panelinde nakit satırının altında da görünür (`FisProYanSutun` yeni `NakitNot` parametresi, id `islem-pro-nakit-not`). Gerçek müşteriyle Kart'ta nakit satırı "Uygun" (yeşil) — "Aşıldı" yalnız Nakit ya da Peşin Müşteri seçiliyken (Peşin'de Kaydet zaten "Bu tutarda Peşin Müşteri olmaz — müşteri seçin." ile kilitli; bu sunucunun K2 kapısıdır) | bUnit `SATIS4a_PRO_otomatik_kart_nedeni_YASAL_panelde_gorunur_nakit_satiri_ASILDI_degil`; canlı 16/16 satis4 ölçümünde PRO'da `proNakit=Uygun` + not dolu; görüntü `olc2/ky-PRO-aydinlik-1440/foto/satis4-once__PRO_aydinlik_1440.png` |
| Kasası olmayan kiracıda BASİT 1280 "Kasa seçin..." iki satıra kırılıyordu (K3_kirik) | DÜŞÜK | Kök neden: ALDIM sütunundaki `form-grid-2` 124 px'lik yarım sütun. `tuval-satis.css`: seçim bekleyen (yer tutucu gösteren) seçici satırın tamamını alır (`grid-column: 1 / -1`, 124 → 261 px @1280). Seçili kasa varken yerleşim değişmez (Havale · Kasa hizası korunur — aşağıda) | Önce: BASİT açık 1280 K3 0/1 `span.secici-metin "Kasa seçin..."` (görüntü `olc/kasasiz-once/foto/islemler_BASIT_aydinlik_1280.png`). Sonra: BASİT+PRO × açık+koyu × 1440/1280/960/375 → K1 0 · K3 0/0 · K7 0 · K9 0 · yatay 0 · K5 0 (görüntü `olc/kasasiz-s2-BASIT-karanlik/foto/islemler_BASIT_karanlik_1280.png`). "Kasa seçilmedi" hücresi bu ölçümde tek satır (K3 listesinde yok) → `white-space:nowrap` eklenmedi (uzun kasa adında 375'te yatay taşma riski). bUnit+CSS `Kasasiz_kiracida_BASIT_kasa_secici_yer_tutucusu_satirin_TAMAMINI_alir` |
| Uygulayıcının davet satırı silinmemişti | DÜŞÜK | `kiraci_davetleri` 01M3T6G0G5RS2M4BC9VWYJ2DPR silindi (row_count 1, kalan 0). Tur 2 kiracısının daveti (01M3TFFYCH33C4ZG4ETMSF2XS0) de silindi. **Temizlik adımı artık: `kiraci-temizle.sh <id> --uygula` + `DELETE FROM kiraci_davetleri WHERE isletme_adi='<test kiracısı adı>'`** | SQL § 8 |

### Tur 2 testleri
| Koşum | Toplam | Geçti | Başarısız | Atlandı |
|---|---|---|---|---|
| KesifP5 filtreli (son) | 41 | 41 | 0 | 0 |
| KesifP5 + TS2TuvalPro + SATIS2Ekran + YASAL2 + SATIS3 + BasitModPos (son kod) | 676 | 675 | 0 | 1 |
| Tam süit ara (requery sürümü, `tam-t2b.log`) | 8407 | 8403 | 3 | 1 | 
| **Tam süit SON kod** (trx `/private/tmp/kuyumcu-serit/trx-pKESIF-P5-t2c/_Mac_2026-10-01_04_36_05.trx`) | **8415** | **8414** | **0** | 1 |

Ara koşumdaki 3 kırmızı: `YASAL2FisTaksit…Pesin_KART_satista_kayit_sonrasi_sorgu_ATILMAZ` (benim requery denemem — düzeltildi), `SATIS2Ekran…PRO_ust_satir_Musteri_ve_TEK_SATIR_ozet…` (filtreli koşumda yeşil, son tam süitte yeşil — yük altında kararsız), `KarZarar…P35_08` (takvim bağımlı, son tam süitte yeşil). **Son kodla tam süit 0 kırmızı → bu paketin yeni kırmızısı 0.**
Yeni test yöntemi tur 2: +3 (KesifP5Testleri.cs: 27 yöntem / 41 koşum); 1 test güçlendirildi.

### Tur 2 mutasyonları (yedek → tek eşleşmeli değişiklik → filtreli test → geri al → `cmp`)
| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M1-1c | `OdemeVarsayilaniniUygula` dolu fiş dalı `if (false && odemeOtomatik …)` (tur 1'de HAYATTA KALAN) | SATIS4a_tutar_esik_ALTINA_inince_otomatik_secim_NAKITe_doner | AYNI |
| M1-6b | Kaydet sonrası `ProYasalBosFis()` çağrısı silindi | SATIS4a_PRO_kart_satisi_KAYDEDILINCE_bos_fiste_yasal_kapi_ASILDI_YOK (satır 232: "Aşıldı" iddiası) | AYNI |
| M1-6c | `ProYasalBosFis` bayrağı `false` yerine `true` | SATIS4a_PRO_kart_satisi_KAYDEDILINCE_bos_fiste_yasal_kapi_ASILDI_YOK | AYNI |
| M1-7 | `NakitNot="@OdemeOtomatikNotu"` bağı silindi | SATIS4a_PRO_otomatik_kart_nedeni_YASAL_panelde_gorunur_nakit_satiri_ASILDI_degil | AYNI |
| M6-3 | kasa yer tutucu `grid-column: 1 / -1` kuralı boşaltıldı | Kasasiz_kiracida_BASIT_kasa_secici_yer_tutucusu_satirin_TAMAMINI_alir | AYNI |

Not (Anayasa md.1): M1-7'nin İLK koşumu geçersizdi — aynı ağaçta başka şeridin eşzamanlı derlemesi `bin`'i M1-1c'nin mutasyonlu çıktısıyla bıraktı (kırmızı test yanlış olanıydı, beklenen test yeşildi; dosya zaman damgaları dll'in benim derlememden sonra yazıldığını gösterdi). Yeniden koşuldu → beklenen test kırmızı. Ayrıca tur 2'deki ilk iki çözüm denemesinin mutasyonları (M1-6 `ToplamTl>0` koruması, M1-6 requery) da kırmızı verdi ama o kodlar son hâlde YOK. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 hata, 2 uyarı — ikisi de başka şeridin test dosyasında** (`Kuyumcu.Tests/Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601`, `Kuyumcu.Tests/KesifP2Testleri.cs(454,9) xUnit2029`); bu paketin dosyalarında uyarı 0.

### Tur 2 canlı + tarayıcı (API 5166 · vekil 5167 · başsız Chrome CDP 9502–9506; kiracı PAKETKESIF-P5-TEST Tur2 `01M3TFFYF9K83KWRVXK2ZJ7M32`, önce KASASIZ ölçüldü, sonra `demo-tohumla.js --eposta` ile ürün uçlarından dolduruldu)

Ölçüm matrisi SON kodla: 5 senaryo (vezne, islemler, satis4, hurda4, havale) × 4 genişlik (1440/1280/960/375) × BASİT+PRO × açık+koyu = **80 ölçüm, 80/80 TEMİZ** (K1 0 · K3 0/0 · K5 TRY 0 · K7 0 · K9 0 · K10 0 · yatay 0 · K12 0 · K13 0 · K10b 0). Ham: `/private/tmp/kuyumcu-serit/KESIF-P5/olc2/n-*/olcum.jsonl`.
(Dürüstlük notu: matris koşarken aynı ağaçta iki derleme oldu — biri benim son düzeltmem, ikisi mutasyon koşumu (M1-6b/c); ikisi de yalnız KAYIT SONRASI PRO durumunu değiştirir, matris kaydetmez → yerleşim ölçümüne etkisi yok. Kayıt sonrası ölçümler (aşağıda) temiz son derlemeden sonra koşuldu.)

| senaryo | 1440 | 1280 | 960 | 375 | mod × tema |
|---|---|---|---|---|---|
| satis4 (Derya, ~145.687 ₺) | Kart + not, Kaydet etkin | aynı | aynı | aynı | 16/16; PRO'da panel "Uygun" + not |
| hurda4 (Kemal, 48,75 g) | satıcı ad/TCKN/adres kutusu 0, MASAK 1, Havale | aynı | aynı | aynı | 16/16 |
| islemler arama satırı | "95.4xx ₺ · Stok 3" | aynı | aynı | aynı | 16/16 |
| islemler Vazgeç/Kaydet | 52/52 alt ±0 | 52/52 ±0 | 52/52 ±0 | 52/52 ±0 | 16/16 |
| vezne ürün seçici (px) | 467 B / 369 P | 799 | 479 | 202 | 16/16 |
| **Havale · Kasa hizası (2 TL kasası, PRO)** | ±0 aynı satır | ±0 | ±0 (+1024 ±0) | 768 ve 375 alt alta | açık+koyu 6 genişlik, K3/K7/yatay 0 |
| kasasız /islemler | K3 0/0 | K3 0/0 (önce 0/1) | K3 0/0 | K3 0/0 | BASİT+PRO × açık+koyu |

Kayıt sonrası (SATIS-4 a + d + hakem KRİTİK), SQL kanıtıyla:
| Adım | Beklenen | Gerçek |
|---|---|---|
| PRO 1440 açık, Derya, Kart 145.665,35 ₺, Kaydet | Kart varsayılan, Kaydet etkin; 30 sn sonra son fiş görünür, boş fişte "Aşıldı" yok | 0 sn ve 30 sn "Son fiş: Satış · 04:31 · 145.665,35 ₺"; panel "Yasal kapılar uygun", nakit `metin-yesil`, kalem 0. SQL `odeme_kalemleri`: **KART 145665.35** |
| PRO 1280 koyu, Tuna, Kart 145.731,87 ₺ | aynı | aynı; SQL **KART 145731.87** (Derya'nın 2. satışı MASAK kimlik kapısıyla kilitlendi — yasal ve doğru, bu yüzden ikinci müşteri) |
| BASİT 1440 açık, Selin, Kart 145.763,70 ₺ | aynı (BASİT'te nakit bandı yok) | 30 sn "Son fiş: Satış · 04:34 · 145.763,70 ₺"; SQL **KART 145763.70** |
| Hurda BASİT 1440, Kemal, 48,75 g, kaydet | tek kimlik formu, 1 POST | satıcı kutuları 0, kimlik kutusu 1, not "… ikinci kez yazılmaz."; SQL: HURDA_ALIS **1** hareket (HAVALE −241194.04), bacak HAVALE 241194.04, `gider_pusulalari` **1**, `masak_kimlik_tespitleri` **1** |

Not: kabuldeki "129.247,86 ₺" tutarı tur 1'in kur anındaki fiyatıdır; canlı kur değiştiği için aynı iki ürün ~145,7 bin ₺ geldi (haddin 4,8 katı) — davranış sınaması aynıdır. bUnit testleri 129.247,86 ₺ ile sınar.

Kapanış: API (PID 9981) ve vekil durduruldu — 5166/5167 LISTEN 0; bu paketin Chrome süreçleri (9502–9506) 0. `zsh kiraci-temizle.sh 01M3TFFYF9K83KWRVXK2ZJ7M32 --uygula` → ikinci çağrı "Kiracı YOK (bağlantı sınandı)"; kiracı/hareket satırı 0; iki davet satırı silindi; **tenants = 12 = tur 2 başlangıcı**. QA-TEST-* ve başka şeritlerin kiracılarına dokunulmadı (hakemin "PAKETKESIF-P5-TEST Tasarim Hakemi …" davet satırı hakemindir — dokunulmadı).

### Tur 2 denetim (`bash dotnet/denetim.sh`, son kod)
302 ✓, 1 ✗: `P-30 DEMO_INSAN_SURESI` (23.09 demo-prova dosyaları; sahibi orkestratör/demo — tur 1'deki ile aynı). Bu paketin sayaçları ✓: ModalPencere 47/55 · SATIR_ICI_STYLE 61 · SABIT_RENK 0 · ESIK_ISTEMCIDE 4/4 · ODEME_KALEMSIZ_YOL 0 · CARI_BAKIYE ayna yeni 0. `Islemler.razor` 1399 satır (≤1400).

### Tur 2 değişen dosyalar
`Kuyumcu.Web/Pages/Islemler.razor` (2 satır: `NakitNot` bağı, Kaydet sonrası `ProYasalBosFis()`), `Kuyumcu.Web/Pages/Islemler.Satis4.cs` (`ProYasalBosFis`), `Kuyumcu.Web/Bilesenler/Fis/FisProYanSutun.razor` (`NakitNot`), `Kuyumcu.Web/wwwroot/css/tuval-satis.css` (kasa yer tutucu satırı), `Kuyumcu.Tests/KesifP5Testleri.cs` (+3 yöntem, 1 güçlendirme, sahte API `kasalarJson` + tutara göre tevsik). `Islemler.Pro.cs` denenip geri alındı (net değişiklik yok). Sözlük değişikliği yok (not metni mevcut `KesifP5` anahtarından).

## 1. Yapılanlar (kısa)

| Madde | Ne yapıldı | Dosya |
|---|---|---|
| #17 /vezne kalem satırı | Satır kendi düzenini taşır (`.vz-kalem-satir`): ürün seçici en az 12rem ve satırın en geniş alanı (54 px → 202–799 px), kutular ve "Kalemi sil" alt kenardan hizalı (±10 → 0). ≤1399 px'te iki sıra: 1. sıra ürün + Kalemi sil, 2. sıra adet · birim fiyat (· işçilik). Alanın boş alt satırının 4 px ızgara aralığı etikete taşındı. BASİT'te "Kur tahsil anında sunucudan alınır" yerine "Fiyat, tahsil anındaki güncel kurdan hesaplanır." (PRO metni HUB'dan aynen) | Vezne.razor, tuval-satis.css, Sozlukler.KesifP5.cs |
| ORTAK-1 YÜKSEK /vezne 960 | Kuyruk tablosu 641–1099 px'te ızgara: eylem hücresi veri satırının ALTINDA kendi satırında (kap dışı eylem 2 → 0, iç kaydırma 234 px → 0); ≤640 px'te `q10-yigin` kart yığını (her hücre `data-etiket`). Izgarada td yüksekliği `auto` (Toplam rozeti eylem satırına biniyordu — canlı ölçümde bulundu) | Vezne.razor, tuval-satis.css |
| ORTAK-1 ORTA `VezneModulu.cs:785` | Şubesiz kullanıcı metni: "Kasa kuyruğu için kullanıcıya şube atanmalı. Yönetim > Kullanıcılar ekranından şube seçin." (kural aynı) | VezneModulu.cs |
| #67 /islemler alt satır | Vazgeç 44 px ↔ Kaydet 52 px → ikisi `--dugme-yuksekligi-buyuk` (52). ≤1099 px'te ikisi yarım genişlik taban alır ve birlikte sarar; BASİT'te "+ Ayrıntı ekle", PRO telefonda ödeme grubu kendi satırında; ≤560 px'te Kaydet tek satır (K3 yok). 16 durumda alt kenar farkı 0 | tuval-satis.css |
| #70 satır içi birim fiyat | "11052,67" → "11.052,67" (tr-TR binlikli, kuruş her zaman yazılır: "1.500,00" P-78'de belirsiz değil); açılıp aynen kaydedilince değer DEĞİŞMEZ; K7 1440–960 ±0, 375'te alt alta | FisKalemTablosu.razor |
| SATIS-4 (a) | Tutar sunucunun bildirdiği nakit haddini aşınca ödeme varsayılanı kendiliğinden **Kart** (satış, takasta müşteri öderse) ya da **Havale** (hurda/alış/iade, para dışarı) olur; altında tek satır neden ("Tutar 30.000 ₺ nakit sınırını aştığı için … Kart olarak seçildi. İsterseniz değiştirebilirsiniz." — sayı sunucudan). Elle seçim EZİLMEZ; tutar haddin altına inerse otomatik seçim Nakit'e döner; Vazgeç/boş fişte otomatik seçim sıfırlanır; had bilinmiyorsa (uç düştü) davranış eskisi gibi Nakit (istemcide sabit eşik YOK). Canlıda bulunan gerileme de kapatıldı: büyük kartlı satıştan sonra **0 ₺ boş fişte** "Bu tutarda nakit alınamaz" bandı ve açık ödeme seçicisi kalıyordu (önceki fişin önizleme bayrağı) → boş fişte bayrak düşer | Islemler.Satis4.cs (yeni), Islemler.razor (2 satır, satır sayısı 1399), FisOdemeKasa.razor |
| SATIS-4 (b) | **Tek kimlik formu:** Hurda alışında MASAK kimlik kutusu açıkken satıcının ad/TCKN/adres kutuları ikinci kez çizilmez; kutunun altında "… aşağıdaki müşteri kimliği kutusundan alınır — ikinci kez yazılmaz." Gövdedeki VUK md.234 `satici` bloğu MASAK kutusundan doldurulur (alanlar AYNEN gider). MASAK belgesi TC kimlik değilse (pasaport) satıcı TCKN kutusu görünür kalır (değer uydurulmaz). Adres zorunluysa ve boşsa yolu gösteren alan hatası. MASAK kutusu seçili GERÇEK müşteriden (ad · yalnız kimlik türü TCKN ise 11 haneli no · adres) ve önceden yazılmış satıcı alanlarından **BİR KEZ** ön dolar; kullanıcının sildiği alan yeniden doldurulmaz | Islemler.Satis4.cs, FisHurdaAlis.razor, Islemler.razor |
| SATIS-4 (c) | Arama satırında **fiyat ve stok**: iki TOPLU istek — stok `GET /urunler/stok-adetleri` (ISTEK-1 ucu, oturum şubesi), fiyat yeni `GET /fiyat/teklifler` (tekli `/fiyat/teklif` ile AYNI motor, aynı cari listesi, aynı kur; en çok 25 ürün, aşımı 400). Satır başına istek YOK. Uç düşerse "—" / "Stok —" (hata "stok yok"a çevrilmez); stok 0 → "Stokta yok" | FiyatTopluTeklifModulu.cs (yeni), TopluTeklifSozlesmeleri.cs (yeni, Contracts), Islemler.Arama.cs, FisKalemGirisi.razor |
| SATIS-4 (d) | Son kayıt satırı artık her türde ve PRO'da da: "Son fiş: Satış · 02:55 · 145.579,59 ₺", bir sonraki kayda kadar durur (başarı bandı 3 sn'de kapanır, satır kapanmaz). Sunucu okunur fiş numarası döndürmüyor (ULID) — numara uydurulmadı | Islemler.Diger.cs, Islemler.Satis4.cs |

## 2. Sapmalar ve gerekçeleri

1. **Yeni uç dosya alanı dışında:** (c)'nin fiyatı için sunucu ucu gerekiyordu (liste `/urunler` altın üründe fiyat taşımıyor). Fiyat/Ürün modüllerindeki mevcut dosyalara DOKUNULMADI; yeni denetleyici **ayrı yeni dosyada** (`Api/Modules/Fiyat/FiyatTopluTeklifModulu.cs`) ve yeni sözleşme **ayrı yeni dosyada** (`Contracts/Fiyat/TopluTeklifSozlesmeleri.cs`). Eşzamanlı şeritlerle (CARI-BAKIYE, YASAL-3, KESIF-P3) dosya çakışması yok. Salt okuma, `[ModulYetkisi("fiyat-listesi")]` (tekli teklifle aynı sınıf kapısı).
2. **(b) yönü:** Kurul "MASAK kutusu satıcı kimliğinden dolsun" diyor; `MasakFormu.razor` ortak bileşen (dokunma yasağı) olduğundan dolu alanları orada gizleyemedik. Tek form şöyle sağlandı: MASAK kutusu açıkken satıcı ad/TCKN/adres kutuları hiç çizilmez, veri tek kutudan (MASAK) iki gövdeye gider; önceden yazılmış satıcı değerleri MASAK'a bir kez taşınır. Sonuç aynı: kimlik bir kez yazılır.
3. **Satış içindeki eski altın ödemesi (b):** `AltinBacagiPaneli.razor` ortak bileşen (dosya alanı dışı). Burada MASAK kutusu seçili müşteriden ön dolar (ad/TCKN/adres), panel de aynı müşteriden dolar → iki kutu da elle yazılmaz; ancak panel kutuları görsel olarak kalır → **açık kalan** (sahibi: AltinBacagiPaneli / P7).
4. **Mevcut testlerin güncellenmesi (davranış değişikliği):** 26 test yöntemi eski varsayılanı (eşik üstünde Nakit) kodluyordu. Hepsi kurulun (a) kararına göre "kullanıcı nakdi ELLE seçer" adımı (`Satis4Yardim.NakitElleSec`) eklenerek niyetleri korunarak güncellendi (nakit kuralı iddiaları aynen duruyor); T19/T19d/T24 kimlik testlerine "müşteriden dolan belge no silinir" adımı eklendi; `T10_tek_KART_…` son iddiası yeni davranışa (yeni fiş varsayılanı otomatik, not görünür) çevrildi; `P78 Gosterilen_fiyat…` "45000,00" → "45.000,00" (#70 ile doğrudan çelişiyordu; gidiş-dönüş değişmezliği iddiası korundu); `KY10` hücre iddiası `data-etiket`li hâle regex ile uyarlandı. Dosyalar: SATIS3EkranTestleri.cs, SATIS3T24YasalMesajTestleri.cs, Dogrulayici/SATIS3SinirTestleri.cs, Dogrulayici/SATIS3T24SinirTestleri.cs, BasitModPosTestleri.cs, CumaSAldimSadeTestleri.cs, OrtakGorunumKiyasTestleri.cs, P78SayiGirisiTestleri.cs.
5. **Kurul 5 (a) "Kaydet pasif kalmaz":** Kaydet, gerçek müşteri seçiliyken etkin (ölçüldü). **Peşin Müşteri** ile haddin üstündeki satışta Kaydet yine kilitli ("Bu tutarda Peşin Müşteri olmaz — müşteri seçin.") — bu, sunucunun Peşin K2 kapısıdır (yasal; değiştirilmedi).
6. **TOPLU § SATIS-3 kapanış 5 ORTA maddeleri** — yapılanlar: kasa/ödeme kalıbında kaydet sonrası artık kalan bant/seçici (0 ₺) kapatıldı; fiyat sütunu "—" → toplu teklif fiyatı. Yapılmayanlar "Açık kalanlar"da tek tek.

## 3. Migration
Gerekmedi (şema değişikliği yok).

## 4. Testler

| Koşum | Toplam | Geçti | Başarısız | Atlandı | Not |
|---|---|---|---|---|---|
| Filtreli (ilk durum, eski testler, değişiklik sonrası) | 2160 | 2125 | 34 | 1 | 32'si (a) davranış değişikliğinin eski varsayılanı kodlayan testleri + P78 + KY10 → güncellendi |
| Filtreli genişletilmiş (güncellemeden sonra) | 3204 | 3202 | 1→0 | 1 | CUMA_S_02 de güncellendi |
| KesifP5 (son) | 27 | 27 | 0 | 0 | |
| **Tam süit tur 1** (ara kod) | 8295 | 8292 | 2 | 1 | GorunumSubesi K3_Aylik, KarZarar P35_08 |
| **Tam süit tur 2** (SON kod, trx `/private/tmp/kuyumcu-serit/trx-pKESIF-P5/_Mac_2026-10-01_02_54_10.trx`) | **8346** | **8343** | **2** | 1 | aynı iki test — bkz. § 11 |

Yeni test: **27** (`Kuyumcu.Tests/KesifP5Testleri.cs`): bUnit 20 (Islemler 15 + bileşen 2 + Vezne 3 yöntem/theory), HTTP 3, saf/CSS 2; zorunlu adlar: `Vezne_kalem_urun_secici_ezik_degil`, `FisKalemTablosu_birim_fiyat_binlikli`, `Islemler_alt_dugmeler_ayni_sinif` VAR.

**Bu paketin yeni kırmızısı: 0.** Tam süitteki iki kırmızı paketten bağımsız ve takvime bağlı: ölçüm anı yerel 01.10 02:55 / UTC 30.09 23:57 — `K3_Aylik` "bu ay" raporunu UTC/yerel ay sınırında karşılaştırıyor; `P35_08` sırayla "geçen ay → bugün → bu ay" tıklıyor, ayın 1'inde "bugün" = "bu ay" aralığı olduğu için UX-4d md.22 aynı sorguyu tekrar göndermiyor → istek yok. Bu paket Rapor/KarZarar dosyalarına dokunmadı; iki test tur 1'de de (P5 değişikliklerinin çoğu yokken aynı nedenle) kırmızıydı ve tek başına koşumda da kırmızı. Sahibi: Rapor/KarZarar test sahipleri (takvim bağımlılığı).

## 5. Mutasyon kanıtı (her biri: yedek → tek eşleşmeli değişiklik → filtreli test → geri al → `cmp` AYNI)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M1-1 | (a) otomatik seçim yazılmaz (`odemeTuru = hedef` silindi) | test koşumu çöktü (sonsuz çizim → stack overflow, koşum iptal = KIRMIZI); temiz sürüm M1-1b ↓ | AYNI |
| M1-1b | satışta hedef `KART` → `null` | SATIS4a_esik_ustu…KART (BASIT+PRO), SATIS4a_elle_NAKIT…EZILMEZ, SATIS4a_tutar_esik_ALTINA…, SATIS4a_Vazgec…, SATIS4a_KAYITTAN_sonra_BOS_fiste… (6) | AYNI |
| M1-2 | elle seçim bayrağı yok sayıldı | SATIS4a_elle_NAKIT_secimi_EZILMEZ_kilit_sebebi_haddi_soyler | AYNI |
| M1-3 | had yerine 30.000 geri düşüşü (`TevsikHaddi`) | SATIS4a_had_BILINMIYORSA_varsayilan_NAKIT_kalir_istemci_esik_uydurmaz | AYNI |
| M1-4 | boş fişte bayat nakit bayrağı düşürülmez | SATIS4a_KAYITTAN_sonra_BOS_fiste_0_TL_nakit_bandi_ve_acik_odeme_secici_YOK | AYNI |
| M1-5 | Vazgeç'te otomatik kart Nakit'e dönmez | SATIS4a_Vazgec_ile_bosalan_fiste_OTOMATIK_kart_varsayilana_NAKITe_doner | AYNI |
| M2-1 | `KimlikMasakta="false"` (tek form kapalı) | SATIS4b_Hurda_MASAK_acikken…TEK_form…, SATIS4b_Hurda_MASAK_PASAPORT…, SATIS4b_Hurda_tek_formda_adres_EKSIKSE… (ilk koşum başka şeridin eşzamanlı derlemesi yüzünden `obj/…pdb kilitli` ile geçersizdi → yeniden koşuldu) | AYNI |
| M2-2 | satıcı TCKN ← MASAK eşitlemesi silindi | SATIS4b_Hurda_MASAK_acikken_satici_kimligi_TEK_form_POST_bir_kez_govdede_VUK234_alanlari | AYNI |
| M2-3 | ön dolum "bir kez" koruması kaldırıldı | SATIS4b_Satis_MASAK_secili_musteriden_BIR_KEZ_doldu_silinen_alan_yeniden_DOLMAZ | AYNI |
| M2-4 | kimlik türü TCKN koşulu kaldırıldı | ilk koşumda HAYATTA KALDI (test verisi 10 haneli VKN) = test açığı → test 11 haneli TCKN-dışı no ile güçlendirildi → SATIS4b_TCKN_disi_kimlik_turlu_musteride_11_haneli_no_BELGE_NO_olarak_dolmaz KIRMIZI | AYNI |
| M2-5 | adres eksik alan hatası kaldırıldı | SATIS4b_Hurda_tek_formda_adres_EKSIKSE_yolu_kimlik_kutusunu_gosteren_alan_hatasi | AYNI |
| M3-1 | arama satırı toplu fiyatı okumaz | SATIS4c_arama_satirinda_FIYAT_ve_STOK… (BASIT+PRO) | AYNI |
| M3-2 | stok ucu düşünce "stok 0" yazılır | SATIS4c_toplu_uclar_DUSERSE_liste_kalir_fiyat_ve_stok_bilinmiyor_yazar | AYNI |
| M3-3 | sunucu 25 ürün sınırı kaldırıldı | KesifP5HttpTestleri.Toplu_teklif_TEKLI_teklifle_AYNI_fiyat_sinirlar_400_kiraci_yalitimi | AYNI |
| M4-1 | son kayıt yalnız BASİT tezgâhta | SATIS4d_PRO_satista…, SATIS4d_Hurda_kaydinda… | AYNI |
| M5-1 | birim fiyat binliksiz | FisKalemTablosu_birim_fiyat_binlikli, P78…Gosterilen_fiyat… | AYNI |
| M6-1 | ürün seçici tabanı/asgarisi kaldırıldı (css) | Vezne_kalem_urun_secici_ezik_degil (basit: True/False) | AYNI |
| M6-2b | Vazgeç büyük düğme boyu kaldırıldı (css) | Islemler_alt_dugmeler_ayni_sinif | AYNI |
| M7-1 | Vezne şubesiz metni eski hâline | KesifP5HttpTestleri.Vezne_subesiz_kullanici_mesaji_ekran_adiyla_ve_yapilacak_isle | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 uyarı 0 hata**. Proje ağacında .mutorj/.bak/.tmp yok (yedekler `/private/tmp/kuyumcu-serit/KESIF-P5/mut-yedek/KESIF-P5/`).

## 6. Denetim (`dotnet/denetim.sh`)
Son koşum 300 ✓. Bu paketin sayaçları ✓ (B-3 ESIK_ISTEMCIDE 4/4 — ilk koşumda benim yorum satırım 5'e çıkarmıştı, sayı yorumdan çıkarıldı; ModalPencere 47/55; SATIR_ICI_STYLE 61; SABIT_RENK 0). Kalan ✗'ler bu paketin değil:
- `P-23 ODEME_KALEMSIZ_YOL = 2` ve `CARI_BAKIYE_AYNA_UYUSMAZ YENİ 1` — canlı veri; SQL: iki TAHSILAT/NAKIT satırı **PAKETCARI-BAKIYE-HAKEM-TEST** kiracısında (23:49) — sahibi CARI-BAKIYE hakem şeridi.
- `P-30 DEMO_INSAN_SURESI` — 23.09 demo-prova dosyaları, son tarih 30.09 geçti — sahibi orkestratör/demo.

## 7. Canlı doğrulama (API 5166 + vekil 5167, kiracı PAKETKESIF-P5-TEST `01M3T6G0J0Y98E3HJYE4RF2688`, demo-tohumla ile tohumlandı)

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /fiyat/teklifler (3 ürün) | 200, tekli teklifle aynı fiyat | 200; Burma 95.339,48 = tekli /fiyat/teklif 95.339,48; gümüşler 3.200 / 2.400 |
| 26 ürün | 400 | 400 "Tek istekte en fazla 25 ürün sorulabilir" |
| kurSnapshotId yok | 400 | 400 "Kur kaydı gerekli (kurSnapshotId)" |
| GET /urunler/stok-adetleri | 200 | 200 (2 / 3 / 3 adet) |
| Tarayıcı: satış 145.461,35 ₺ (Burma + Zincir, müşteri Derya), BASİT 1440 | ödeme Kart, not, Kaydet etkin, tek POST | Kart + not, Kaydet etkin, kaydedildi; SQL `odeme_kalemleri`: tek bacak **KART 145461.35** (fiş 01M3T8KQ…) |
| Son fiş 30 sn | görünür | 0 sn ve 30 sn: "Son fiş: Satış · 01:57 · 145.461,35 ₺" (BASİT), "Son fiş: Satış · 02:55 · 145.579,59 ₺" (PRO koyu 1280) — bant kapalı, satır duruyor |
| Tarayıcı: hurda 48,75 g (240.733,35 ₺), müşteri Kemal (kimliksiz kart), BASİT | ödeme Havale, kimlik 1 kez, 1 POST | Havale + not; satıcı ad/TCKN/adres kutusu 0, kimlik kutusu 1; kaydedildi. SQL: HURDA_ALIS **1** hareket (HAVALE −240733.35), `gider_pusulalari` **1** (GP-2026-000001, NIHAI_TUKETICI, ad dolu, TCKN şifreli+özet dolu, adres dolu, net 240733.35), `masak_kimlik_tespitleri` **1** |
| Kurucu Havale · Kasa hizası (2 TL kasası açıldı) PRO | 1440/1280/1024/960 aynı satır ±0; 768/375 alt alta | 604–648/604–648 · 662–706 · 676–720 · 676–720 (±0); 768 ve 375 alt alta; K7 0 |

Ölçüm tablosu (başsız Chrome, `betikler/tur/cdp.mjs` + `sikisma.js`/`ekstra.js`; 5 senaryo × 4 genişlik × 2 mod × 2 tema = **80 ölçüm**, son kodla): **K1 0 · K3 0/0 · K5 (TRY) 0 · K7 0 · K9 0 · K10 0 · K12 ezik 0 · K13 0 · K10b 0 · yatay kaydırma 0 — 80/80**. Ayrıntı: `/private/tmp/kuyumcu-serit/KESIF-P5/son-ozet.txt`, görüntüler `/private/tmp/kuyumcu-serit/KESIF-P5/olc/son-{BASIT,PRO}-{aydinlik,karanlik}/foto/`.

| rota | 1440 | 1280 | 960 | 375 | mod × tema |
|---|---|---|---|---|---|
| /vezne ürün seçici genişliği | 467 (B) / 369 (P) | 799 | 479 | 202 (≥192) | 4/4 K12 0, Kalemi sil alt ±0, BASİT "sunucu" 0, kuyruk eylemi kap dışı 0 |
| /islemler Vazgeç/Kaydet boy · alt farkı | 52/52 · 0 | 52/52 · 0 | 52/52 · 0 | 52/52 · 0 | 16/16 |
| /islemler PRO satır içi birim fiyat | "95.328,33" · K7 0 | aynı | aynı | alt alta (K7 0) | 4/4 |
| arama satırı | "95.302,70 ₺ · Stok 2" | aynı | aynı | aynı | 16/16 |
| satış 145 bin (a) | Kart + not, Kaydet etkin | aynı | aynı | aynı | 16/16 |
| hurda 240 bin (b) | satıcı kutusu 0, MASAK 1, Havale | aynı | aynı | aynı | 16/16 |

Görüntüler Read ile görüldü: vezne PRO koyu 960 (iki sıra kalem satırı, eylemler altta), hurda BASİT koyu 375, satış BASİT açık 1440 ve PRO koyu 1280 30. sn, hurda PRO açık 1440.

**Canlıda bulunup düzeltilenler:** 0 ₺ boş fişte "nakit alınamaz" bandı (M1-4), Vazgeç sonrası notsuz kalan otomatik Kart (M1-5), kuyruk ızgarasında Toplam rozeti eylem satırına biniyordu, PRO 1280 kalem etiketleri iki satır (±26), BASİT 960 / PRO 375 Vazgeç–Kaydet ayrı satır.

API ve vekil süreçleri durduruldu (5166/5167 LISTEN yok), bu paketin Chrome süreçleri 0.

## 8. Test kiracısı
`zsh betikler/kiraci-temizle.sh 01M3T6G0J0Y98E3HJYE4RF2688 --uygula` → ikinci çağrı "Kiracı YOK (bağlantı sınandı)". SQL: kiracı 0 satır, hareketleri 0. `tenants` = 16 (başlangıç 13; artış eşzamanlı şeritlerin açık kiracıları: PAKETCARI-BAKIYE-HAKEM-TEST, PAKETBELGE-KALAN-TEST ×3 vb. — bu paketin kiracısı kalmadı). QA-TEST-* kiracılarına dokunulmadı.

## 9. DoD sözleşmesi
Yeni varlık / kayıt türü **yok**. Yeni uç `GET /api/v1/fiyat/teklifler` salt okumadır (yazmaz, denetim kaydı gerektirmez), `[ModulYetkisi("fiyat-listesi")]`, 25 ürün sınırı + 400, kiracı süzgeci, Contracts türü. Ekranda yeni form yok; mevcut formların düzenleme yolları değişmedi. Vezne fişi düzenle/iptal/aktif akışı aynen. Finansal satırlara hard DELETE yok.

## 10. Dokunulan sayfalar (TEK DOKUNUŞ)
`Pages/Vezne.razor`, `Pages/Islemler.razor` (+ `Islemler.Arama.cs`, `Islemler.Diger.cs`, yeni `Islemler.Satis4.cs`), `Bilesenler/Fis/FisKalemTablosu.razor`, `FisKalemGirisi.razor`, `FisHurdaAlis.razor`, `FisOdemeKasa.razor`.

## 11. Değişen dosyalar
Kod: `Kuyumcu.Web/Pages/Vezne.razor`, `Kuyumcu.Web/Pages/Islemler.razor`, `Kuyumcu.Web/Pages/Islemler.Arama.cs`, `Kuyumcu.Web/Pages/Islemler.Diger.cs`, `Kuyumcu.Web/Pages/Islemler.Satis4.cs` (yeni), `Kuyumcu.Web/Bilesenler/Fis/FisKalemTablosu.razor`, `FisKalemGirisi.razor`, `FisHurdaAlis.razor`, `FisOdemeKasa.razor`, `Kuyumcu.Web/wwwroot/css/tuval-satis.css`, `Kuyumcu.Core/I18n/Sozlukler.KesifP5.cs` (yeni), `Kuyumcu.Api/Modules/Vezne/VezneModulu.cs`, `Kuyumcu.Api/Modules/Fiyat/FiyatTopluTeklifModulu.cs` (yeni), `Kuyumcu.Contracts/Fiyat/TopluTeklifSozlesmeleri.cs` (yeni).
Test: `Kuyumcu.Tests/KesifP5Testleri.cs` (yeni) + § 2 md.4'teki 8 dosya.
(Not: depo tek ilk commit'li olduğundan `git status --porcelain` bu dosyaları toplu "??/M" gösteriyor; liste yedek karşılaştırmasıyla çıkarıldı.)

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| #17 ürün ≥12rem, K12/K10 0, Kalemi sil K7 ≤3, BASİT "sunucu" 0 | KARŞILANDI | 16 ölçüm, bUnit Vezne_kalem_urun_secici_ezik_degil |
| #67 Vazgeç = Kaydet boyu, alt fark ≤1 | KARŞILANDI | 16/16 · 52/52 · 0 |
| #70 K7 ≤3, "11.052,67" | KARŞILANDI | PRO 1440–960 ±0; FisKalemTablosu_birim_fiyat_binlikli |
| ORTAK-1 /vezne 960 eylemleri | KARŞILANDI | kap dışı 0, kaydırma 0 (4 genişlik × 4) |
| VezneModulu metni | KARŞILANDI | HTTP testi |
| SATIS-3 hedefli 375 kalem adı K3 | KARŞILANDI | islemler 375 BASIT/PRO K3 0/0 |
| SATIS-4 (a) nakit değil + Kaydet etkin | KARŞILANDI (gerçek müşteride; Peşin'de yasal K2 kilidi) | canlı + 16 ölçüm + SQL KART |
| SATIS-4 (b) kimlik 1 kez | KARŞILANDI (hurda); satış eski altın panelinde kısmi | canlı: 1 POST, 1 pusula, 1 kimlik tespiti |
| SATIS-4 (c) fiyat + stok | KARŞILANDI | canlı + 16 ölçüm, 2 toplu istek |
| SATIS-4 (d) 30 sn sonra fiş | KARŞILANDI | BASİT ve PRO 30 sn görüntüsü |
| Gerileme: 0 ₺ kapısı | KARŞILANDI (+ canlıda bulunan 0 ₺ bant gerilemesi kapatıldı) | ZOP23 filtreli yeşil, M1-4 |
| Gerileme: yasal kapıda tek cümle | KARŞILANDI | T24 testleri yeşil |
| Gerileme: Havale · Kasa 6 genişlik | KARŞILANDI | § 7 tablo |
| Build 0/0 | KARŞILANDI | son build 0 uyarı 0 hata |
| Denetim — paket sayaçları | KARŞILANDI | § 6 (✗'ler başka sahipli) |
| Tam süit 0 yeni kırmızı | KARŞILANDI | § 4 |
| Mutasyon | KARŞILANDI | 19 mutasyon kırmızı, cmp AYNI |
| Test kiracısı silindi | KARŞILANDI | § 8 |

## 13. Açık kalanlar
- **Satış içindeki eski altın ödemesi (b):** `AltinBacagiPaneli.razor` (ortak bileşen) satıcı kutularını hâlâ çiziyor; ikisi de seçili müşteriden dolduğu için elle iki kez yazılmıyor ama görsel tekrar sürüyor. Sahibi: P7 / AltinBacagiPaneli.
- **TOPLU § SATIS-3 kapanış 5 ORTA — yapılmadı:** (1) BASİT MASAK jargonunun metin kısmı ("MASAK Kimlik Tespiti", "8 yıl korunur") → P8 (metin) + `MasakFormu` ortak bileşen; (2) tek kasada BASİT'te kasa satırını tamamen gizleme / PRO tek kalıp → `FisOdemeKasa` kalıp birleştirmesi ayrı iş; (3) tür adları (BASİT "Değişim" = PRO "Takas", "Ürün değişimi") → metin P8; (4) iade listesinin müşteriye göre süzülmesi (`/islemler/iade-adaylari` cari süzgeci sunucu işi); (5) eski altında has fiyatının kurdan dolması → `AltinBacagiPaneli` (ortak); (6) Diğer formlarında ortak arama kutusu (`FisDiger*`) — büyük iş, ayrı paket önerilir.
- ~~PRO "Yasal kapılar" kartı boş fişte "Aşıldı"~~ → **tur 2'de KAPATILDI** (§ 0).
- **Kurul kuyruğu (tur 2 DÜŞÜK):** PRO'da Peşin Müşteri + hadde üstü tutarda ödeme otomatik Kart olsa da nakit satırı "Aşıldı" der (Peşin K2 kapısı; Kaydet zaten "müşteri seçin" ile kilitli). Satır metninin "Peşin Müşteri ile bu tutar olmaz" diye ayrılması kurul/metin (P8) kararı — kuyruğa yazıldı.
- **P7/P5 kuyruğu (tur 2 DÜŞÜK):** "Kasa seçilmedi" ALDIM hücresi bu ölçümde tek satır; uzun kasa adlarında 1280'de kırılabilir — `nowrap` 375'te yatay taşma riski taşıdığı için eklenmedi; kasa adı uzunluğu için P7 ortak kural önerisi.
- **Hareket `odeme_turu` KARMA:** tek kartlı tezgâh fişi defterde KARMA yazılıyor (bacak tablosunda doğru: KART). TOPLU ZO-BELGE "Karışık" maddesiyle aynı kök — sahibi rapor/SatisModulu.
- Tam süitteki 2 takvim bağımlı kırmızı (§ 4) — sahipleri Rapor/KarZarar.

## 14. Kurucu / mali müşavir notları
- **MASAK kimlik ön dolumu (b):** Tedbirler Yönetmeliği md.5/2 "Kimlik tespiti, iş ilişkisi tesisinden veya işlem yapılmadan önce tamamlanır" ve md.6/2 bilgilerin doğruluğunun kimlik belgesi "üzerinden teyit edilir" hükmünü taşır (birincil kaynak: https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf). Ekran yalnız YAZIMI kolaylaştırır (müşteri kartındaki ad/TCKN/adres bir kez ön doldurulur, alanlar düzenlenebilir); **teyit yükümlülüğü değişmedi**. Soru (mali müşavir/uyum görevlisi): müşteri kartından ön doldurulan kimliğin her işlemde belge aslıyla teyit edildiğine dair ekranda ayrı bir onay kutusu istenir mi? Karar gelene dek davranış: ön dolum + sunucunun mevcut kimlik kapıları (fail-closed) aynen.
- **Nakit sınırı:** sayı koda yazılmadı; sunucunun `/tevsik/gunluk` haddi kullanılıyor (mevcut yapılandırma). Bu pakette yeni mevzuat hükmü yazılmadı.
