# PAKET KESIF-P3 · BELGE — uygulama raporu (30.09.2026, şerit t6)

Rol: Taşkın (backend) + Ertuğ (frontend). Hasımsız paket (kurucu 30.09 21:15). Bu yüzden bağlayıcı kanıt bu rapordaki ölçümlerdir. COMMIT YOK.

## Özet
Kabul ölçütlerinin hepsi karşılandı: #11, #23, #54, #55, #57, #59, #64, #66, #78 ve KESIF-P7 devri (gider pusulası K3 ve 960 binmesi). TOPLU ZO-BELGE'deki görünüm maddeleri de uygulandı. Veri ya da para davranışını değiştiren maddeler EK KOŞUL gereği hasımlı pakete devredildi (§ Açık kalanlar).

Sonuçlar:
- Build: 0 uyarı, 0 hata.
- Tam süit: 8173 test. 8172 geçti, 0 başarısız, 1 atlandı.
- denetim.sh: **DENETİM TEMİZ**.
- Mutasyon: 16/16 kırmızı döndü, hepsi geri alındı ve `cmp` AYNI.
- Gerçek başsız Chrome: 11 senaryo × 4 genişlik × 2 mod × 2 tema = 176 ölçüm (denetim izi son derlemeyle 16 kez daha ölçüldü).

## Yapılanlar
| Bulgu | Değişiklik |
|---|---|
| #11 + P7 devri | `GiderPusulalari.razor`: tablo artık ZO-BELGE kart yığını kalıbında (`zo-belge`, `q10-yigin zo-yigin-dar`, `data-zo="golge"`, `data-etiket`, `q10-yigin-eylem`). Kap 1060 px'ten darsa satırlar kart olur (960/1280), genişte tablo kalır. TCKN ayrı sütun değil, satıcının alt satırında (9 → 8 sütun). Numara, ad, tutar ve tarih `hucre-tek-satir`. Satır içi `style` 3 → 0. |
| #64 | Liste ve detayda `KurKaynakAdi.AyarAdi` kullanılıyor ("22 Ayar · 4,35 g"). Ham kod `title` ipucunda. |
| #66 | `HurdaModulu.cs:139-140` cins alanı: `Hurda altın (22 Ayar — 916/1000)`. Maden adı ve milyem korunur (VUK md.234). Kod bilinmiyorsa kodun kendisi yazılır, uydurulmaz. |
| #78 | `Raporlar.razor` stok tablosu ve stok pastası etiketi ayar adını gösteriyor. |
| #23/#59 | `Faturalar.razor` kalem Ayar hücresi ayar adını gösteriyor (`fd-kalem-ayar-{id}`, ham kod `title`'da). Kalem penceresine `data-zo="kalem"` kabı eklendi. Pencere gövdesi 900 px'ten darsa satırlar kart olur (PRO 960'ta tablo 1024 / kap 920 kırpılıyordu). |
| #57 | `#fom-kilit` artık blok `<p>` ve düğmenin ÜSTÜNDE kendi satırında. Alt çubuktaki `#fk-kaydet-kilit` aynı cümleyi tekrarlamıyor, kısa metin gösteriyor (`KuyumKaydetKilitKisa`). |
| #54/#55 | `DenetimIzi.razor` `DetayMetni`, ekip kararı 2'ye göre çalışıyor: iki modda da görünür metinde ham kod yok, ham kayıt `title`'da. BÜYÜK_ALT_ÇİZGİ kodları okunur ada çevriliyor: belge türü (SATIS_FISI → "satış fişi"), sonra hareket türü (`KatalogSozlugu.HareketTuru`), bilinmeyen kod küçük harf ve boşlukla yazılıyor. PRO'da kebab kodlar (islem-defteri) ve alan adı listesi de çevriliyor. BASİT'te ekli "cari" biçimleri (carisi/cariye…) "müşterisi/…" oluyor. PRO'da "cari" kalıyor (karar 1). Sözlükte adı olmayan 3 kayıt türü eklendi: TevsikIhlali, TevsikDogrulanamayan, YasalParametre (canlı varlik_turu taraması). |
| TOPLU 08:50 | Defter bağlı fatura kaleminde adet salt okunur: `fd-kalem-adet-kilit-{id}` ve "Fişten değiştir" bağlantısı (`/islemler`). Fiyat alanları serbest kaldı, sunucu kuralı zaten aynıydı. `KalemVeri` sözleşmesine `HareketId` eklendi (isteğe bağlı; sunucu zaten döndürüyordu). |
| ZO-BELGE: boş bant | `zo-belge.css`: süzgeç yuvası olmayan SayfaBasi'de eylem yuvasının üst payı 0. Faturalar, Evraklar ve Gider pusulalarında ~64 px'lik boş bant kalktı. |
| ZO-BELGE: Fatura No | Taslakta "—" gösteriliyor (açıklama `title`'da). Sütun başa yaslı ve tek hizalı. |
| ZO-BELGE: iç kaydırma [YÜKSEK] | İşlem defteri ve Kim ne yaptı tablolarındaki iç kaydırma kaldırıldı, tablo sayfayla akıyor. Denetim izinde K9 3–4'ten 0'a indi. Sayfa boyu (25) değişmedi. |
| ZO-BELGE: kasa tekrar cümlesi | "Yalnız bugünün kasası sayılabilir" tabloların üstünde bir kez yazılıyor (`#kr-sayim-kural`). Satırdaki sebep `gorunmez` yapıldı, id aynı. |
| ZO-BELGE: çek-senet ipucu [YÜKSEK] | Özet ipucu ≥ 641 px'te sağa hizalı. 1280'de sağ kenar 1381'di, şimdi 1145. |
| ZO-BELGE: bölünmüş süzgeç | Dört durum tek şeritte ve dışarıda. `#evrak-durum-diger` id'si şeridin içinde `display: contents` ile yaşıyor. Katlanır yalnız kendi içindekileri sayıyor. |

Yeni metinlerin tamamı `Kuyumcu.Core/I18n/Sozlukler.KesifP3.cs` içinde (TR + EN, konumsal record). HUB'a dokunulmadı.

## Sapmalar ve gerekçeler
- Triyaj `Sozlukler.ZoBelge.cs` diyor, ama görev kuralı "yeni metin yalnız Sozlukler.KesifP3.cs". Metinler KesifP3'e yazıldı. ZoBelge'ye dokunulmadı.
- Yeni karar eski test beyanlarıyla çelişiyordu. Aşağıdaki testler yeni kararı ölçecek şekilde güncellendi:
  - `CJ_04` ("Kesimde verilir" → "—"; TOPLU DUGME-2)
  - `ZB45b`, `ZB36`, `Q105_B05` (PRO ham detay → karar 2: görünür metinde çeviri, `title`'da ham kayıt)
  - `ZB61` (iç kaydırma artık yok olmalı)
  - `ZB08`, `CJ_10` (dört durum dışarıda)
  - `C3_13` (hücre konum yerine `data-etiket` ile bulunuyor)
- `FaturalarSozlesmeleri.cs` (Contracts) sahip listesinde yok. Yalnız isteğe bağlı bir alan eklendi (`HareketId = null`), geriye uyumlu.
- Açılış ≤ 1,0 ölçütü (ZO-BELGE hakemi) ile TOPLU [YÜKSEK] "iç kaydırmayı kaldır, kırpma kabul edilemez" çelişiyor. TOPLU'nun daha yeni kararı uygulandı. Açılış ölçüsü orkestratör kararına bırakıldı.

## Migration
Gerekmedi (şema değişikliği yok).

## Testler
- Yeni testler (15):
  - `Faturalar_kalem_ayar_adi`
  - `Faturalar_onizleme_kilit_metni_tek`
  - `Faturalar_defter_bagli_kalem_adedi_salt_okunur_fisten_degistir_baglantisi`
  - `Raporlar_stok_ayar_adi`
  - `GiderPusulalari_ayar_adi`
  - `GiderPusulalari_tablo_kart_yigini_ve_tek_satir_hucreler`
  - `DenetimIzi_PRO_gorunur_ham_kod_yok_title_korunur`
  - `DenetimIzi_BASIT_cari_ekli_bicim_ve_ham_kod_yok`
  - `KasaRaporu_sayim_kurali_tabloda_tek_kez`
  - `KesifP3_css_kurallari`
  - API/DB: `GiderPusulasi_belge_cins_ayar_adi_ve_maden`
  - Güncellenen ve yeni beyan taşıyan 4 test (ZB36/ZB45b/Q105_B05/ZB61 kapsamında)
- Filtreli son koşum: 708/708 geçti.
- Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_t1`, trx `/private/tmp/kuyumcu-serit/trx-pKESIF-P3/`): total 8173, passed 8172, failed 0, 1 atlandı. **Bu paketin ürettiği yeni kırmızı: 0.**
- Not: bir ara filtreli koşum (Evrak/ZoBelge, test5) yanlışlıkla `KUYUMCU_TEST_DB` verilmeden koştu, yani varsayılan test veritabanını kullandı. 329/329 geçti. Diğer bütün koşumlar t1 veritabanında yapıldı.

## Mutasyon tablosu (hepsi geri alındı, `cmp` AYNI)
| Kod | Kırılan kural | Kırmızı test |
|---|---|---|
| M3-1 | Kalem Ayar → ham `k.AyarId` | Faturalar_kalem_ayar_adi |
| M3-2 | Rapor stok → ham kod | Raporlar_stok_ayar_adi |
| M3-3 | Pusula listesi → ham kod | GiderPusulalari_ayar_adi |
| M3-4 | Belge cinsi → `{g.AyarId}` | GiderPusulasi_belge_cins_ayar_adi_ve_maden |
| M3-5 | Alt çubuk yine uzun cümle | Faturalar_onizleme_kilit_metni_tek |
| M3-6 | PRO detay ham | DenetimIzi_PRO…, ZB36, ZB45b, Q105_B05 |
| M3-7 | BASİT cari eki çevrilmez | DenetimIzi_BASIT_cari_ekli… |
| M3-8 | Kart yığını sınıfı yok | GiderPusulalari_tablo_kart_yigini… |
| M3-9 | Defter bağlı adet düzenlenebilir | Faturalar_defter_bagli… |
| M3-10 | Kasa sebebi yine görünür | KasaRaporu_sayim_kurali_tabloda_tek_kez |
| M3-11 | İç kaydırma belirteci geri | ZB61 |
| M3-12 | Katlanır durumu da sayar | ZB08 |
| M3-13 | Boş bant kuralı kaldırıldı | KesifP3_css_kurallari |
| M3-14 | BÜYÜK_ALT_ÇİZGİ çevrilmez | DenetimIzi_BASIT…, DenetimIzi_PRO… |
| M3-15 | TevsikIhlali adı yok | DenetimIzi_PRO… |
| M3-16 | `#evrak-durum-diger` kimliği bozulur | CJ_10 |

Son mutasyondan sonra temiz build alındı: 0 uyarı, 0 hata.

## denetim.sh
- İlk koşum ✗ verdi: `P-23 ODEME_KALEMSIZ_YOL 1`. Satırın sahibi bu paket değil; `PAKETCIFT-BACAK-TEST Hakem…` kiracısının TAHSILAT hareketi.
- Son koşum: rc=0, **DENETİM TEMİZ**, 298 ✓.

## Canlı doğrulama
Ortam: API 5160 (kendi sistem anahtarıyla), vekil 5161, test kiracısı `PAKETKESIF-P3-TEST Kuyumculuk` (01M3SZE2DDTAF0KAD0YWC4KFRA).

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /gider-pusulalari/{K22}/belge | 200, cins ayar adıyla | 200 "Hurda altın (22 Ayar — 916/1000)" · madenAdi Altın |
| GET /gider-pusulalari/{G925}/belge | 200, gümüş cinsi | 200 "Hurda gümüş (925 Gümüş — 925/1000)" · Gümüş |
| GET /faturalar/{fisten}/kalemler | hareketId dolu | 200 · KALEM · hareketId true, K22 |
| PATCH kalem miktar farklı | 400 | 400 "Bu satır satış fişinden geldi; adedi fişten değiştirin." |
| PATCH kalem miktar aynı | 200, değişiklik yok | 200 · SQL miktar 1.000 aynı |
| SQL denetim_kayitlari | ham kayıt değişmez | "SATIS_FISI belgesi görüntülendi", "Pano kalemi güncellendi (AYAR_14)" AYNEN (çeviri yalnız gösterimde) |

## Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9701–9728)
Ölçüm aracı `sikisma-olc.js`, ek olarak şu sayaçlar kullanıldı:
- ham ayar kodu `\b[KG]\d{2,3}\b`
- BÜYÜK_ALT_ÇİZGİ
- sayfaya özel ölçüler: NET ÖDENEN'in üstünde başka öğe var mı (elementFromPoint), kalem tablosu sağ kenarı ile kap, kilit ve düğme kesişimi, cümle sayısı, ipucu kenarı, pencere içi K9

Sayfa senaryolarının tamamında (gider-pusulalari, faturalar, evraklar, denetim-izi, pusula-belge, raporlar-stok, kasa-raporu), 1440/1280/960/375 × BASİT/PRO × açık/koyu (112 ölçüm) şu sayaçlar 0: K1, K3, K5, K7, K8, K9, K10, yatay kaydırma, ham ayar kodu, BÜYÜK_ALT_ÇİZGİ.

| Ölçüt | Sonuç |
|---|---|
| #11 NET ÖDENEN üstünde başka öğe var mı | 1440/1280/960 "ok", başlık kesik değil. 375'te hücre ekran altında (elementFromPoint null); K9 0 |
| #23 kalem tablosu kırpılması | 32/32 `hucreGorunur=true`. PRO 960 önce 1024>920 taşıyordu, şimdi kart ve 920=920 |
| #57 kilit ve düğme | 16/16: kesişme yok, ayrı satır, cümle 1 kez |
| Evraklar ipucu | 16/16 taşma yok, dört durum tek şeritte, şerit taşması 0 |
| Pencere içi K9 (modal-panel) | 32/32 = 0 |
| Çekmece genel K9 | 6. Bu, pencerenin arkasındaki sayfa ile yapışkan alt çubuğun altından kayan içerikten geliyor; görüntüde binme yok (pusula-detay 960 görüntüsü) |
| BASİT↔PRO tutarlılık (tutarlilik.mjs, 6 rota, 1440) | mod farkı **0** (orkestratör tabanında bu rotalarda fark vardı) |

Read ile bakılan görüntüler:
- `olc-son-b-a/foto/gider-pusulalari__BASIT_aydinlik_1440/1280/960.png`
- `olc-son-p-k/foto/gider-pusulalari__PRO_karanlik_960.png`
- `olc-deneme-kalem/foto/fatura-kalem-1__PRO_aydinlik_960.png`
- `olc-b-a/foto/fatura-kalem-1__BASIT_aydinlik_1440.png`
- `olc-dz-p-a/foto/denetim-izi__PRO_aydinlik_1440.png`
- `olc-son-p-k/foto/denetim-izi__PRO_karanlik_1440.png`

Tüm görüntüler `/private/tmp/kuyumcu-serit/KESIF-P3/olc-*/foto/` altında, ham ölçümler `olcum.jsonl` ve `son-*.log` dosyalarında.

## DoD sözleşmesi
Yeni varlık yok. Defter bağlı kalemin adedi salt okunur; sebep `title`'da, düzeltme yolu "Fişten değiştir" bağlantısı (UX-S25/S30). Sunucu kuralı zaten 400 dönüyordu.

## Değişen dosyalar
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor`, `Raporlar.razor`, `DenetimIzi.razor`, `GiderPusulalari.razor`, `Evraklar.razor`, `KasaRaporu.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/zo-belge.css`
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs` (yalnız cins metni)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KesifP3.cs` (yeni)
- `dotnet/Kuyumcu.Contracts/Sayfalar/FaturalarSozlesmeleri.cs` (isteğe bağlı `HareketId`)
- Testler: `Kuyumcu.Tests/KesifP3Testleri.cs` (yeni), `FaturaEkranTestleri.cs`, `ZoBelgeTestleri.cs`, `UX1C3EkranTestleri.cs`, `EvrakEkranTestleri.cs`, `Q105FormJargonTabloTestleri.cs`

Dokunulan sayfalar: Faturalar, Raporlar, DenetimIzi, GiderPusulalari, Evraklar, KasaRaporu. PusulaBelgesi ve Cikti razor değişmedi.

## Test kiracısı
- `kiraci-temizle.sh … --uygula` ile silindi. SQL: bu kiracıya ait tenant, denetim ve hareket satırı 0.
- tenants 16 → 12. Bu azalmayı paralel şeritler yaptı; bu paketin kiracı sayısı 0.
- QA-TEST-* kiracılarına dokunulmadı.
- `fik.json` ve `sistem.key` `printf ''` ile boşaltıldı.

## Açık kalanlar (sahibiyle)
1. **Hasımlı pakete devir (veri/para davranışı; EK KOŞUL):**
   - (a) Faturalar, Evraklar, KasaRaporu ve Raporlar'da geçersiz girdi kapısı (`GecersizAlanKapisi`). POST edilen değeri değiştirir.
   - (b) İşlem defterinde 'Karışık' ödeme etiketi. Kaynağı rapor ucunun hesabı (sunucu, doğrulanmadı).
   - (c) PATCH has 6 hane birikimi (paket dışı).
2. **Orkestratör kararı:** İşlem defteri ve Kim ne yaptı iç kaydırması kaldırıldı. Açılış ≤ 1,0 ölçütü bu iki ekranda artık tutmuyor. Sayfa boyunu küçültme seçeneği `GorselSigmaSozlugu.DenetimSayfaBoyu` ve `IslemDefteri.razor` sahibinin işi.
3. DÜŞÜK: 375 px'te özet satırı (i) ipucu dairesi kendi satırında kalıyor ("öksüz"; Evraklar ve Kasa raporu).
4. DÜŞÜK: Rapor merkezi BASİT "Gün sonu" kartının sadeleştirilmesi ve Veri içe aktar maddeleri, P3 birleştirme listesinde değildi.
5. Başka şeridin uyarısı: `Kuyumcu.Tests/Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601`. Ara derlemelerde görüldü, son derlemede 0 uyarı.

## Kurucu / mali müşavir
- Mevzuat hükmü eklenmedi. Belge cinsinde maden adı ve milyem korunuyor.
- Çeviride "Tevsik" için sade ad seçildi: "Bankasız ödeme sınırı aşımı". Mali müşavir terimi onaylarsa kalır.


---

# TUR 2 (01.10.2026) — tasarım hakemi bulgularının kapatılması

Tur 1'den sonra tasarım hakemi 5 KRİTİK bulgu yazdı. Beşi de kapatıldı. Her biri gerçek başsız Chrome'da ölçüldü ve görüntüsüne Read ile bakıldı. Tarayıcı düzeyinde mutasyonla hakemin bulgusu yeniden üretildi, sonra geri alındı.

## Tur 2 özeti
- Build: **0 uyarı, 0 hata** (son temiz build, mutasyonlardan sonra).
- Tam süit (`KUYUMCU_TEST_DB=kuyumcu_test_t1`, trx `/private/tmp/kuyumcu-serit/trx-pKESIF-P3/tur2/`): **8256 test · 8253 geçti · 2 başarısız · 1 atlandı**. İki kırmızı da bu paketin değil (§ Tur 2 açık kalanlar). Bu paketin ürettiği yeni kırmızı: **0**.
- Mutasyon: bUnit/CSS düzeyinde **13/13** kırmızı döndü (M3-17 … M3-29). Hepsi geri alındı, `cmp` AYNI. Ek olarak 1 tarayıcı mutasyonu yapıldı (M3-TAR). Hakemin iki ölçümünü yeniden üretti, geri alındı, `cmp` AYNI.
- Gerçek başsız Chrome: 7 senaryo × 4 genişlik (1440/1280/960/375) × BASİT/PRO × açık/koyu = **112 ölçüm**. Ayrıca kuyum kutusu için temiz akışta 8 ölçüm daha alındı.
- denetim.sh: rc=1, **4 ✗**. Dördü de bu paketin değil (§ denetim). Bu paketin sayaçları ✓.

## Bulgu → düzeltme
| Hakem bulgusu | Düzeltme | Kanıt |
|---|---|---|
| **#23** PRO Kalemler 1440: tablo 1125 / kap 1104, "SATIR TOPLAMI" ve ₺ kesik | `zo-belge.css`: pencere `min(72rem→84rem, 100vw−2rem)`. 1440'ta kap 1296 px. PRO tablosuna `zo-kalem-genis` sınıfı eklendi (11 sütun). Bu sınıfla kap ≤ 1140 px ise tablo kart olur. 375 px'te PRO kartı tek sütun (`@container zokalem 520px` genis kuralı eklendi; ilk ölçüm id+sınıf özgüllüğünün bunu ezdiğini gösterdi). | 7 haneli tutarla (1.095.125,00 birim, 11 sütun) PRO 1440 tablo 1296 = kap 1296, kaydırma 0, kesik hücre 0, kap dışı hücre 0. 1280'de 1200=1200. 960/375'te kart. Görüntüler: `olc-t2-p-k-deneme/foto/fatura-kalem-7hane__PRO_karanlik_1440.png`, `olc-t3-p-k/foto/fatura-kalem-7hane__PRO_karanlik_375.png` |
| **#11** /gider-pusulalari 1440: tablo 1287 / kap 1122, yapışık eylem "Son düzenleme tarihi"ni örtüyor | `zo-belge.css`: `#gp-tablo` için ayrı eşik `@container zoyigin (max-width: 1320px)`. Tablo yalnız sığdığı genişlikte durur (kap > 1320). Altında kart ızgarası. Id özgüllüğü genel 1060 px kuralını ve yapışık gölgeyi ezer. | 1440 BASİT/PRO × açık/koyu: kart, 1122 = 1122, kaydırma 0, örtülü hücre 0, "Son düzenleme tarihi" görünür. Görüntü: `olc-t3-b-a/foto/gider-pusulalari__BASIT_aydinlik_1440.png` |
| **#57** Kuyum önizleme kutusu: iç boşluk 0, 12 px yazı, devre dışı düğme yer tutucu gibi | `Faturalar.razor`: kutu `kart zo-onizleme-kutu` (`#fom-kutu`). Kilit cümlesi `metin-kucuk` değil, kilit ikonlu. `zo-belge.css`: padding `var(--bosluk-4)`, aralık `var(--bosluk-2)`, kilit yazısı .875rem (14 px). Devre dışı düğmenin kenarı kesik çizgi, zemini şeffaf. | 16 ölçümün hepsinde padding 16px/16px, metin ve düğme kutudan 17 px içeride, düğme alt boşluğu 17 px, yazı 14 px, kesişme yok, cümle 1 kez, kenar `dashed`. Görüntüler: `olc-t5-p-k/foto/fatura-yeni-kuyum__PRO_karanlik_1440.png`, `olc-t5-b-a/foto/…BASIT_aydinlik_375.png` |
| **Kim ne yaptı** süzgecinde kural cümlesi 3 kez, BASİT'te KVKK maddesi | `DenetimIzi.razor`: ayrı "yalnız eklenir" satırı ve "Düzeltme şöyle yapılır" (AlternatifYol) kaldırıldı. BASİT'te yalnız "🔒 Kayıtlar değiştirilemez (i)" var, gerekçe (i) ipucunda. Değişmez kutusu ve yasal dayanak yok. PRO'da değişmez kutusu yalnız gerekçeyle ve yasal dayanak satırıyla. | BASİT 16 ölçüm: "yalnız eklenir" görünür 0 (ipucu kapalı), KVKK 0, "Düzeltme şöyle" 0. PRO 16 ölçüm: "yalnız eklenir" 1, "Düzeltme şöyle" 0. Görüntü: `olc-t3-b-k/foto/denetim-izi-suzgec__BASIT_karanlik_1440.png` |
| **Gider pusulası Detay** biçimsiz, BASİT'te iki yasal kutu, has 3 ondalık | `GiderPusulalari.razor`: `dl.tanim-listesi.zo-tanim-izgara` (`#gp-detay-liste`) iki sütunlu etiket/değer ızgarası. Alan başı dört değişmez kutusu listeden çıkarıldı. Bütün değişmezlik gerekçeleri tek KAPALI `<details id="gp-degismez">` içinde ("Hangi bilgiler değiştirilemez?"). BASİT'te tek sade cümle var, madde numarası yok. KVKK okuma notu yalnız PRO'da. İptal düğmesi BASİT'te "Pusulayı iptal et". Has `Bicim.Has` ile 4 ondalık yazılıyor. "Düzeltilebilen alanlar" açıklaması kapalı ipucuna işaret ediyor (eski metin "nedeni her alanın yanında yazar" diyordu, artık yanlış olurdu). | 32 ölçümün hepsinde ızgara `grid`, dt/dd aynı satırda, ipucu kapalı, has "4,6716 has" (API 4.6716). BASİT'te `md.` 0 ve KVKK 0. PRO'da madde numaraları yalnız PRO'da. Görüntüler: `olc-t2-p-k-deneme/foto/pusula-detay__PRO_karanlik_1440.png`, `olc-t3-b-a/foto/pusula-detay-ipucu-acik__BASIT_aydinlik_1440.png`, `olc-t3-p-k/foto/pusula-detay-ipucu-acik__PRO_karanlik_375.png` |

Yeni metinler yalnız `Sozlukler.KesifP3.cs` içinde (TR+EN): `DenetimDegismezKisa`, `PusulaDegismezOzet`, `PusulaDegismezSade`, `PusulaDuzenlenebilirAciklama`, `PusulaIptalEtSade`.

## Tur 2 sapmaları ve gerekçeleri
- **PRO gider pusulası:** alan başı dört DegismezIpucu (P-55) silinmedi. Tek kapalı ipucunun İÇİNE taşındı. Hakem "tek, kapalı bir ipucunda birleşsin" dedi. Kapalı ipucunun içinde P-55 testlerinin (`KilitliAlanlardaGerekceGorunur`) istediği gerekçe ve düzeltme yolu korunuyor. BASİT'te yalnız bir sade kutu var.
- **Güncellenen eski test:** `DenetimIziEkranTestleri.Append_only_notu_ekranda_gorunur`. Eski hâli "append-only" satırının varlığını istiyordu. Bu, hakemin kaldırılmasını istediği tekrar cümlesiydi. Test artık şunları ölçüyor: gerekçe ("yalnız eklenir") değişmez kutusunda var, "append-only" jargonu yok, "Düzeltme şöyle yapılır" satırı yok.
- **ProAlan yerine `@if (!Dil.BasitMi)`:** `UyumEkranTemeli` test tabanı ArayuzModuServisi kaydetmiyor. ProAlan orada `Detay_tiklayinca_tam_TCKN_gosterir` testini düşürdü. Davranış aynı.
- **Ölçüm notu (Anayasa md.1):** 4 paralel başsız Chrome koşumunda iki kez (t3-p-a, t4) `zo-belge.css` boş geldi (vekil `inceleme-vekili.py` eşzamanlı istekte boş gövde döndürüyor; HEAD Content-Length 0 görüldü). Bu koşumlar GEÇERSİZ sayıldı ve tek tek, sırayla yeniden koşuldu (`olc-t3-p-a2`, `olc-t5-*`). Tabloda yalnız geçerli koşumlar var: her birinde `izgara=grid` ve `pad=16px` stil sayfasının yüklendiğini kanıtlıyor. Geçersiz koşumun klasörü (`olc-t3-p-a`) duruyor, taşıma/silme kancaya takıldı.
- **Çekmece/pencere içi K9 (0–5):** Kalan değerler iki kaynaktan geliyor. Birincisi, çekmece gövdesinin yapışkan alt çubuğun (İptal/Kaydet) altından kayan içeriği. İkincisi, genel K9=6: pencerenin arkasındaki sayfa ile üst katman. Görüntülerde binme yok (`pusula-detay-ipucu-acik__PRO_karanlik_375.png`, `fatura-yeni-kuyum__BASIT_aydinlik_375.png`). Pencere içi ölçü artık `checkVisibility()` ile gizli (kapalı `<details>`) metni saymıyor.

## Tur 2 testleri (yeni 6 + güncellenen 1)
- `FaturaEkranTestleri.Faturalar_kalem_PRO_genis_tablo_sinifi_BASIT_yok`
- `FaturaEkranTestleri.Faturalar_onizleme_kutusu_ic_bosluklu_ve_kilit_buyuk_punto`
- `ZoBelgeTestleri.DenetimIzi_BASIT_degismez_tek_ipucu_madde_yok`
- `ZoBelgeTestleri.DenetimIzi_PRO_degismez_tek_cumle_duzeltme_satiri_yok`
- `KesifP3GiderPusulasiEkranTestleri.GiderPusulalari_detay_izgara_tek_kapali_ipucu_has_dort_hane`
- `KesifP3GiderPusulasiEkranTestleri.GiderPusulalari_detay_BASIT_tek_sade_gerekce_madde_yok`
- `KesifP3CssTestleri.KesifP3_css_kurallari`: 7 yeni beyan (84rem, zokalem 1140 genis, zokalem 520 genis, zoyigin 1320 #gp-tablo, kutu iç boşluğu, 14 px, ızgara)
- Güncellenen: `DenetimIziEkranTestleri.Append_only_notu_ekranda_gorunur`

Filtreli koşumlar: KesifP3/FaturaEkran/ZoBelge/UX1C3/PusulaEkran/DuzeltmeEkran/Q105/Denetim/GiderPusul/EkranTestleri → 2270 test. Bu paketin testlerinin hepsi yeşil. Tek kırmızı KarZarar P35_08 (aşağıda).

## Tur 2 mutasyon tablosu (hepsi geri alındı, `cmp` AYNI)
| Kod | Kırılan kural | Kırmızı test |
|---|---|---|
| M3-17 | PRO kalem tablosundan `zo-kalem-genis` kalktı | Faturalar_kalem_PRO_genis_tablo_sinifi_BASIT_yok |
| M3-18 | Pencere 84rem → 72rem | KesifP3_css_kurallari |
| M3-19 | GP eşiği 1320 → 1060 | KesifP3_css_kurallari |
| M3-20 | Önizleme kutusundan `zo-onizleme-kutu` kalktı | Faturalar_onizleme_kutusu_ic_bosluklu_ve_kilit_buyuk_punto |
| M3-21 | Kilit cümlesi yine `metin-kucuk` (ilk deneme başka şeridin yarım `Yasal3Metinleri` derleme hatasına çarptı, yeniden koşuldu) | Faturalar_onizleme_kutusu_ic_bosluklu_ve_kilit_buyuk_punto |
| M3-22 | BASİT dalı kapatıldı (değişmez kutusu + KVKK geri) | DenetimIzi_BASIT_degismez_tek_ipucu_madde_yok |
| M3-23 | PRO'da "Düzeltme şöyle yapılır" (AlternatifYol) geri | DenetimIzi_PRO_degismez_tek_cumle_duzeltme_satiri_yok, Append_only_notu_ekranda_gorunur |
| M3-24 | Detay listesinden ızgara sınıfı kalktı | GiderPusulalari_detay_izgara_tek_kapali_ipucu_has_dort_hane |
| M3-25 | Has yine 3 ondalık (`BelgeSayisi(…,3)`) | GiderPusulalari_detay_izgara_tek_kapali_ipucu_has_dort_hane |
| M3-26 | BASİT gerekçe yine VUK maddeli | GiderPusulalari_detay_BASIT_tek_sade_gerekce_madde_yok |
| M3-27 | BASİT iptal düğmesi yine "(VUK md.234)" | GiderPusulalari_detay_BASIT_tek_sade_gerekce_madde_yok |
| M3-28 | 375 PRO kart tek sütun kuralı kalktı | KesifP3_css_kurallari |
| M3-29 | Değişmez ipucu `open` başlar | GiderPusulalari_detay_izgara_tek_kapali_ipucu_has_dort_hane |
| **M3-TAR** (tarayıcı) | CSS'te üç kural birden eski hâline döndü: 72rem, zokalem 900, zoyigin 1060 | PRO karanlık 1440, gerçek Chrome. **Kalem (11 sütun, 7 hane):** tablo 1120 / kap 1104, kaydırma 16, kap dışı hücre 2. **Gider pusulası:** tablo 1176 / kap 1122, kaydırma 54, K9 5. Hakem bulgusunu yeniden üretti. Geri alındı, `cmp` AYNI, sunulan CSS yine 84rem (`run-mut-tar.log`). |

## Tur 2 canlı doğrulama (API 5160, vekil 5161, kiracı `PAKETKESIF-P3-TEST Kuyumculuk` 01M3T4NW346TB213JVFS2PV1JF)
| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /auth/giris | 200 | 200 |
| GET /gider-pusulalari/{GP-000005} | 200, has 4 hane | 200 · K22 · hasGrami 4.6716 · net 28029.6 → ekranda "4,6716 has" |
| GET /faturalar/{7 haneli}/kalemler | 200, 2 kalem | 200 · "22 Ayar Burma Bilezik 150 g" 1×1.095.125 ; "Çeyrek altın" 3×48.750,5 |
| GET /gider-pusulalari/{id}/belge | 200, cins ayar adıyla | 200 · "Hurda altın (22 Ayar — 916/1000)" |
| SQL gider_pusulalari | aynı | GP-2026-000005 · K22 · 4.671600 · 28029.60 |
| SQL fatura_kalemleri | 2 | 2 |
Sunucu davranışı değişmedi. Tur 2'de sunucu dosyasına dokunulmadı.

## Tur 2 ölçüm tablosu (başsız Chrome; geçerli koşumlar t3-b-a, t3-b-k, t3-p-a2, t3-p-k, t5-*)
Tablodaki her satır 4 kombinasyonun ölçümüdür: BASİT/PRO × açık/koyu. Bütün satırlarda K1 = K3 = K5 = yatay kaydırma = ham ayar kodu = BÜYÜK_ALT_ÇİZGİ = 0.

| Senaryo | 1440 | 1280 | 960 | 375 |
|---|---|---|---|---|
| gider-pusulalari | kart · 1122=1122 · örtülü 0 | kart · 962=962 · 0 | kart · 642=642 · 0 | kart · 349=349 · 0 |
| fatura-kalem-7hane (PRO 11 sütun) | tablo · 1296=1296 · kesik 0 | tablo · 1200=1200 · 0 | kart · 880=880 · 0 | kart · 311=311 · 0 |
| fatura-kalem-7hane (BASİT 8 sütun) | tablo · 1296=1296 · 0 | tablo · 1200=1200 · 0 | kart · 0 | kart · 0 |
| fatura-yeni-kuyum | pad 16 · 14 px · dashed · cümle 1 | aynı | aynı | aynı |
| denetim-izi-suzgec | BASİT: (i) · madde 0 · tekrar 0 / PRO: cümle 1 · düzeltme satırı 0 | aynı | aynı | aynı |
| pusula-detay (+ ipucu açık) | grid · kapalı · 4 hane · BASİT md 0 | aynı | aynı | aynı |

Ham ölçümler `/private/tmp/kuyumcu-serit/KESIF-P3/olc-t{2,3,5}-*/olcum.jsonl`, görüntüler aynı yerde `foto/` altında. Senaryo tanımları `senaryo-t2.json` ve `senaryo-kuyum.json`.

## denetim.sh (tur 2)
rc=1, 299 ✓, 4 ✗. Hiçbiri bu paketin değil:
- `Pages+Bilesenler+Layout record tanımı 4 (tavan 3)` ve `P-10 BU_PAKETTE_EKLENEN_RECORD +1`: kaynak `Kuyumcu.Web/Bilesenler/Uyum/SaklamaBekletmePaneli.razor:234 record TespitOzet`, 01.10 01:14'te değişti (YASAL-3 şeridi, dosya bu paketin değil).
- `P-02 OLCUM_TABANI_KOPYA 1 sapma`: aynı record sayacının ölçüm tabanından ayrışması.
- `P-30 DEMO_INSAN_SURESI`: tarih tetikli ("son tarih 2026-09-30 geçti"). Demo provası konusu, bu paketle ilgisi yok.

Bu paketin sayaçları ✓: IgnoreQueryFilters 0 · ExecuteUpdate 5/5 · Hareketler.Add 1 · satır içi style 61/196 · ModalPencere 47/55 (artış yok) · SABIT_RENK 0 · satır içi renk/px 0.

## Tur 2 test kiracısı
`zsh kiraci-temizle.sh 01M3T4NW346TB213JVFS2PV1JF --uygula` → rc=0. SQL sonuçları: tenants 13 (başlangıçta 13), bu kiracıya ait tenant, denetim, hareket ve gider pusulası satırı 0. API (5160) ve vekil (5161) durduruldu, portlar boş. `fik.json` ve `sistem.key` `printf ''` ile boşaltıldı (0 bayt). QA-TEST-* kiracılarına dokunulmadı.

## Tur 2 değişen dosyalar
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor`: önizleme kutusu, PRO kalem sınıfı
- `dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor`: detay ızgarası, tek kapalı ipucu, Bicim.Has, BASİT iptal etiketi, açıklama
- `dotnet/Kuyumcu.Web/Pages/DenetimIzi.razor`: değişmez bloğu
- `dotnet/Kuyumcu.Web/wwwroot/css/zo-belge.css`: 84rem, zokalem 1140/520 genis, zoyigin 1320 #gp-tablo, önizleme kutusu, tanım ızgarası, kapalı ipucu
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.KesifP3.cs`: 5 yeni anahtar, TR+EN
- Testler: `Kuyumcu.Tests/KesifP3Testleri.cs`, `FaturaEkranTestleri.cs`, `ZoBelgeTestleri.cs`, `EkranTestleri.cs`

Tur 2'de ikinci kez açılan sayfalar: Faturalar, GiderPusulalari, DenetimIzi. Gerekçe: aynı paketin tasarım hakemi düzeltme turu (TEK DOKUNUŞ istisnası, aynı paket). Tur 2'de sunucu kodu, Contracts ve migration değişmedi.

## Tur 2 açık kalanlar (sahibiyle)
1. **Tam süitte bu paketin olmayan 2 kırmızı:**
   - `GorunumSubesiTestleri.K3_Aylik_secili_sube_ve_tum_subeler`: GorunumSubesi alanı, SUBE-YASAL23 şeridi (t2).
   - `KarZararEkranTestleri.P35_08_Her_dugme_gercek_istek_atar`: KarZarar.razor'a dokunulmadı (dosya 30.09 03:59'dan beri aynı). Tur 1 tam süitinde yeşildi. O zamandan beri değişen ortak dosya `Kuyumcu.Web/Servisler/ApiIstemci.cs` (01.10 00:18, bu paketin değil; Rapor GET uçları SUBE-YASAL23'te). Filtreli koşumda her seferinde kırmızı, kararsızlık değil.
2. Tur 1 açık kalanları aynen geçerli: hasımlı pakete devir (a)(b)(c), orkestratör açılış ölçütü kararı, 375 (i) öksüzü, Rapor merkezi maddeleri.
3. **Ölçüm aracı:** `inceleme-vekili.py` eşzamanlı istekte boş gövdeli CSS döndürebiliyor. Paralel ölçüm koşan şeritler için orkestratöre not: ölçümleri sırayla koşun ya da her ölçümde stil sayfasının yüklendiğini doğrulayın.

## Kurucu / mali müşavir (tur 2)
Yeni mevzuat hükmü eklenmedi. BASİT ekranlarda madde numarası (VUK md.234, KVKK md.12) gösterilmiyor. PRO ekranlarda ve belge çıktısında aynen duruyor, yani hüküm silinmedi, yalnız BASİT görünümden kalktı.
