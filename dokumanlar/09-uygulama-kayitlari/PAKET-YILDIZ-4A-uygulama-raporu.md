# PAKET YILDIZ-4A — uygulama raporu (09.10.2026)

Kapsam: karşılaştırma aracı (`betikler/tur/tuval-kiyas.mjs`) + kabuk (üst menü, kur şeridi, işaret, kullanıcı) + Bugün (Panel), tuval `Bugun4` (koyu) / `Bugun4a` (açık) ile BİREBİR. Hasımsız paket; kendi kanıtı bağlayıcı. Para/veri/yasal davranışa dokunulmadı (mevzuat konusu yok). COMMIT YOK. Migration gerekmedi.

## Sonuç (kabulün özü)
`tuval-kiyas.mjs`, 1440×900, aynı Inter dosyası, 34 eşlenen öğe × {font-size, ağırlık, harf aralığı (em), satır yüksekliği, renk, aile, büyük harf} + {x, y, w, h} (+ öğeye özel: zemin, kenar rengi/kalınlığı, yarıçap, yükseklik, dolgu):

| Kombinasyon | Öğe | Tolerans içinde | FARK | Bulunamayan | Yatay taşma |
|---|---|---|---|---|---|
| koyu · BASİT (Bugun4) | 34 | 34 | 0 | 0 | 0 |
| açık · BASİT (Bugun4a) | 34 | 34 | 0 | 0 | 0 |
| koyu · PRO (Bugun4) | 34 | 34 | 0 | 0 | 0 |
| açık · PRO (Bugun4a) | 34 | 34 | 0 | 0 | 0 |

Tablolar (öğe, tuval değeri, uygulama değeri, fark): `ham/yildiz-4a/son-{koyu,acik}-{basit,pro}-tablo.md`. Yan yana (tuval | uygulama): `…-yan-yana.png`; fark (difference): `…-fark.png`. İlk turda (r1) 5 FARK çıktı ve düzeltildi: nav zemini, kullanıcı adının büyük harfi (CSS dönüşümü yerine metin), satır adından sonraki boşluk (Razor boş `<text>`ı atıyordu), kuruşun x'i (veri genişliği → `yatay: veri`), "15 gün" yazımı.
Kuruş/₺ AYRI öğe olarak ölçüldü: tuval `56px/200/-0.07em/lh 53.2/rgb(154,152,147)` = uygulama aynı (harf aralığı tuvalde ebeveynin px değerinden miras; uygulamada da öyle bırakıldı).
Gözle (Read): 1440 koyu BASİT yan yana, 1440 açık BASİT yan yana, 375 açık BASİT, kullanıcı menüsü (koyu). Fark yalnız VERİ: tarih (Cuma 9 Ekim), tutarlar, ve tuvaldeki iki satır ("Dünkü gün sonu kapatılmadı", "Derya Yıldızlı'nın emaneti") — aşağıda "belirsiz eşleşme".

### Kesin değer eşitliği sağlanan tuval ögeleri (özet)
nav 72 px / alt çizgi `--h` · işaret 40×40 halkasız elmas · menü 14px/400/.14em büyük harf, seçili 1px alt çizgi, ilk kelime x=168 · kur şeridi "HAS 6.656 · 22 AYAR 6.097 · 17:40" 14px/400/.06em, değerler 500, ₺ YOK · kullanıcı "HAKAN T." 14px/400/.06em, sağdan 64 · üst satır 12px/500/.22em · dev rakam 112px/**200**/−0,035em/satır .95 (opsz Inter) · kuruş+₺ yarı boy soluk · cümle 20px/300 en çok 520 · b1 56 px hap · b2 altı çizili · "Seni bekleyen" alta yaslı, satır 22px dolgu + 1px çizgi, durum 15px soluk, borç kırmızı, b3 40 px ince hap · alt şerit 14px/.06em, değerler 500, "+159,3 g has" biçimi.

## Yapılanlar
- **Araç** `dokumanlar/09-uygulama-kayitlari/betikler/tur/tuval-kiyas.mjs` (kalıcı): .dc.html → statik (support.js/x-dc atılır, `<helmet>` → `<head>`, Google Fonts → yerel Inter `file://`), iki sayfa aynı viewport, eşleme JSON'u (`tuval`/`uygulama` seçicileri; `::metin` = ilk metin düğümünün kutusu; `hiza:sag`, `dikey:alt|yok`, `yatay:veri`), tolerans: boy ±0,5 px · ağırlık eşit · harf aralığı ±0,01em · renk/aile/büyük harf eşit · satır ±0,5 px · konum/boyut ±4 px (metin farklıysa genişlik "veri" bilgisi). Çıktı: tablo.md/json, tuval.png, uygulama.png, yan-yana.png, fark.png; çıkış kodu 0/1/2. Öz sınama (tuval↔tuval) 34/34 AYNI.
- **Yazı tipi:** tuval `Inter:opsz,wght@14..32,200..600` istiyor; vendor'lı dosya yalnız `wght` sürümüydü (md5 farklı, 112 px'te genişlik 0,8 px farklı, "Display" kesimi yok). `wwwroot/font/inter-latin.woff2` ve `inter-latin-ext.woff2` aynı adla gstatic v20 opsz sürümüyle DEĞİŞTİRİLDİ (aynı alt küme, aynı unicode aralığı, aynı OFL; eski dosyalar `/private/tmp/kuyumcu-serit/pYILDIZ-4A/yedek-font/`). Inter yalnız Yıldız'da kullanılır.
- **Bugün** `Bilesenler/Yildiz/YildizBugun.razor` (yeni) + `YildizBugunVerisi.cs` (veri sınıfı, `YildizBekleyenSatir`, saf `YildizBugunBicim`: üst satır, şube adı, cümle, kur tam lira, has bir ondalık, cari toplamları).
- **Panel.razor** (TEK DOKUNUŞ ikinci kez — gerekçe: kurucu 09.10 birebir talebi; YILDIZ-3'ün Panel içi yamasının yerine): `@if (YildizMi) { …YildizBugun… } else { <text>özgün gövde AYNEN</text> }`; YILDIZ-3'ün `SatisSorusu` içindeki Yıldız dalı ÖLÜ kaldığı için özgün `@Bicim.TlKurus(SatisGosterTl)`'a geri alındı; sade yükleyiciler Yıldız'da iki modda da açılır; şema değişimine abonelik; `YildizVerisi` + `YildizBekleyenSatirlari` (YENİ İSTEK YOK — mevcut yükleyicilerin verisi).
- **Kabuk** `YildizUstMenu.razor`: kur şeridi tuval metni (tek satır, ₺ işaretsiz, tam değer `title`'da), kullanıcı adı tr-TR büyük harf, Basit/Profesyonel anahtarı kullanıcı menüsüne (`#yildiz-mod-basit/-profesyonel`, aria-pressed, hata bandı `#yildiz-mod-hata`; UstBar ile aynı servis).
- **tema-yildiz.css**: kabuk değerleri tuvale (menü/kur/kullanıcı 400, ilk kelime +24, kullanıcı +36, nav 18px/1.45, zeminsiz); YILDIZ-3 E1 (`#panel-sade`) ölü kuralları silindi; yeni "PAKET YILDIZ-4A — BUGÜN" bölümü; Bugün rotasında `.ust-bar` gizli (bantlar ayrı öğe, durur), `#ust-mod-anahtar` Yıldız'da gizli; ≤1000 px tek sütun (taşma 0).
- **Sözlük** `Sozlukler.Yildiz.cs`: `YildizBugunSozlugu` (TR+EN).
- **Testler** `Yildiz4ATestleri.cs` (yeni, 24 test koşumu). Eski Yıldız testleri kurucu kararıyla güncellendi: Y1_06 (200 yalnız `.yildiz-bugun…`te serbest), Y2_43 (yalnız YILDIZ-2 bölümü), Y3 EBlok (4A başlığında biter), Y3_10/12/20/21/27 (yeni yapı; Y3_21 adı `…tuval_112px_200…` oldu).

## Sapmalar / belirsiz eşleşme (orkestratör kurucuya gösterir)
| Tuval öğesi | Uygulamada | Neden |
|---|---|---|
| "Dünkü gün sonu kapatılmadı · Kasa · Kapat" satırı | ÇİZİLMEDİ | Sistemde "dün kapanmadı" bilgisi veren uç/bildirim yok; uydurulmadı (Anayasa md.1). Kalıp hazır: veri gelirse aynı satır. Hasımlı pakete devir (kural: hangi kasa, hareket olmuşsa mı?). |
| "Derya Yıldızlı'nın emaneti · 9,16 g has · 30 günü aştı · Aç" | ÇİZİLMEDİ | Emanette süre kavramı yok (EmanetModulu'nda süre alanı/ucu yok). Aynı gerekçe. |
| "15 gün" (alacak) | vadeye kalan gün = "15 gün" (tuvaldeki TUNALILAR verisinde Tuna'nın vadesine 15 gün var) | vadesi GEÇMİŞ alacak tuvalde yok → "N gün geçti" (soluk). Vade bugün/vadesiz → mevcut metin ("Vade bugün"/"Açık hesap"). |
| Kritik iş satırı rengi | durum sütunu `--kotu` | tuvalde yalnız borç tutarı kırmızı; "renk yalnız durum için" kuralının en yakın kalıbı. |
| Yasal liste/uyum/stopaj/bildirim işleri | "Seni bekleyen" satırı (metin · kaynak · "Aç") | Paket koşulu: yasal bant listede satır. Yasal değişiklik bandı listenin başında satır kalıbıyla. |
| KÂR | "maliyet eksik" hem hiç maliyet yokken hem KISMEN eksikken | kartın fail-closed kuralı; kısmi kâr rakamı alt şeritte gösterilmez (Kâr-zarar ekranında). |
| Kur yokken | "Kur yok" (mevcut) | tuvalde hâl yok. |
| Çok şubeli görünüm rozeti | gizli; şube adı üst satırda | tuval "· MERKEZ ŞUBE". Geçersiz şube bandı durur. |
| 375 px | tek sütun, rakam min(112px,15vw), alt şerit sarar | tuvalde telefon hâli yok. |
| Not § 3 okunurluk (200 yerine ≥300, 15 px altı 500) | UYGULANMADI (Bugün + kabuk) | kurucu 09.10 iptal etti. |

## Testler
- Önce: Yıldız filtreli 80 test (Y1+Y2+Y3). Sonra: 104 (Yildiz4ATestleri +24 test koşumu; Theory satırları dahil) — `FullyQualifiedName~Yildiz` 104/104; `Yildiz|Panel|Kabuk` 363/363.
- Tam süit (KUYUMCU_TEST_DB=kuyumcu_test_t1, tek tur): 11730 toplam, 11692 geçti, **36 başarısız** (trx `/private/tmp/kuyumcu-serit/trx-pYILDIZ-4A`). Bu paketin ürünü: **1** — `Toplu1SafTestleri.TARIH_TEK_razor_ve_sayfa_kodunda_ham_tarih_bicimi_yok` (YildizBugunVerisi'nde ham `ToString` tarih) → `Bicim.GunBicim`'e taşındı, yeniden koşumda yeşil; denetim TARIH_TEK ✓. Kalan 35 başkasının (aşağıda). Devir notunda kırmızı listesi verilmedi; ayrım dosya/kapsamla yapıldı: hiçbiri bu paketin dokunduğu dosyaya bağlı değil (S1c → app.css `overflow-wrap`, P8_04 → Kasa sözlüğü).
- Derleme: `dotnet build Kuyumcu.sln` 0 hata, 0 uyarı (artımlı); tam derlemede 6 xUnit/CS8601 uyarısı başka dosyalarda (Yildiz4A'nın 2 uyarısı giderildi).

## Mutasyon (kaynak `mut-yedek/` → perl tek eşleşme → filtreli test → geri kopya → `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M1-01 | cümlede "nakit" koşulu kalktı | Y4A_03 |
| M2-01 | kur değerine " ₺" eklendi | Y4A_04 (×3), Y4A_20 |
| M3-01 | Panel `@if (YildizMi)` → `false` | Y3_10, Y3_12, Y3_27, Y4A_10, 11, 13, 14, 15, 16, 17 |
| M4-01 | dev rakam 200 → 300 | Y3_21, Y4A_31 |
| M5-01 | has N1 → N4 | Y4A_05, Y4A_10 |
| M6-01 | Bugün'de üst bar gizleme kalktı | Y4A_31 (ilk deneme bayat DLL ile Y4A_05/10'u da gösterdi → temiz derlemeden sonra tekrarlandı: yalnız Y4A_31) |
| M7-01 | Yıldız PRO'da sade yükleyiciler açılmıyor | Y4A_11 |
| M8-01 | kullanıcı adı büyük harfe çevrilmiyor | Y4A_21 (test önce gevşekti → `"HAKAN T."` birebir yapıldı) |
Hepsi geri alındı, `cmp` AYNI; son mutasyondan sonra temiz `dotnet build` 0 hata.

## Denetim
`bash denetim.sh`: 314 ✓, 3 ✗ — hiçbiri bu paketin değil: `Sınırsız ToListAsync (Modules) 142/140` (sunucu modülleri — bu paket Api'ye dokunmadı; eşzamanlı şerit), `P-03 CONTRACTS_DISI_MODEL: KaporaGecmisSatiri` (Kapora şeridi), `P-30 DEMO_INSAN_SURESI` (bilinen). İlk koşudaki TOPLU-1 TARIH_TEK ✗ bu paketindi, giderildi.

## Gerçek (başsız) Chrome ölçümü — sikisma-olc.js (K1-K12 + yatay)
Rota `/`, 1280×800 · 375×812 · 1440×900 × BASİT/PRO × açık/koyu = 12 ölçüm: K1 0 · K3 taşan 0 / kırık 0 · K5 TRY 0 · K7 0 · K8 jargon 0 · K9 binen 0 · K10 0 · K11 0 · K12 0 · yatay kaydırma 0; Bugün genişliği = ana alan (dar sütun yok); üst bar ve eski mod anahtarı görünmez. İlk ölçümde K3 kırık 1-3 (soluk ek "· biz borçluyuz" iki satıra bölünüyordu) → `white-space: nowrap`; 375'te cümle ile "Yeni satış" bitişikti (gözle) → tek sütunda `gap: 40px`. Sonuçlar `ham/yildiz-4a/olc-{BASIT,PRO}.json`.
Canlı mod anahtarı: kullanıcı menüsünde "Profesyonel" tıklandı → `PATCH /api/v1/kullanicilar/ben` gönderildi, aria-pressed değişti, hata bandı yok (CDP ağ kaydı). İlk denemede istek gitmedi: aynı dakikada çok giriş yüzünden 429 ("Çok fazla istek") — bekleyince geçti.

## 8 eski şema (FARK 0)
`fark.mjs` (YILDIZ-1 betiği, kendi portlarıma uyarlandı): kullanıcı şeması geçici olarak platin yapıldı (özgün kabuk çizilsin), 8 şema × açık/koyu × {Panel, Satış} × tema-yildiz.css açık↔kapalı ekran görüntüsü sha256: BASİT 32/32 FARK 0, PRO 32/32 FARK 0 (`fark-basit.out`, `fark-pro.out`). Razor tarafı: başka şemada Panel'in özgün gövdesi `else <text>` içinde AYNEN (Y4A_12 BASİT/PRO). Ölçümden sonra şema yildiz'e geri alındı.

## Canlı (API 5160, vekil 5161, CDP 9701-9707)
Kiracı `PAKETYILDIZ4A-TEST Tunalılar Kuyumculuk` (`uzun-tohumla-tunalilar.js --prova-kiraci --sema yildiz`: 6 cari, 12 ürün, 9 satış). API yanıtları (okundu): `/gorunum` 200 sema=yildiz; `/raporlar/gunluk` 200 adet 9, toplamTl 99.339,60; Ana Kasa 216.629,20 TRY; `/raporlar/cari-yaslandirma` 200 Tuna 22.212,70 · Kemal 10.497,70 · Anadolu Döküm −3.840; `/pozisyon` 200 net 159,2652 g. Ekranda: "99.339,60 ₺", "Bugün 9 satış. Kasada 193.768,68 ₺ nakit. Alacağın 32.710,40 ₺, borcun 3.840 ₺." (nakit = kasa − kart/havale, mevcut ayrım ucu), "+159,3 g has".
SQL: `kullanicilar` → Hakan Tunalı · SAHIP · sema `yildiz`; `kasalar` → Ana Kasa TRY 216629.20.
Temizlik: API ve vekil kendi PID'leriyle durduruldu (5160/5161 dinleyen yok). `zsh kiraci-temizle.sh <id> --uygula`; ardından `denetim_kayitlari` 12 ve `is_calismalari` 6 artık satır kaldığı görüldü → elle silindi; 75 tenant_id tablosunda artık 0, `tenants` satırı 0. tenants sayısı: başlangıç 21 → şimdi 19 (aradaki fark eşzamanlı şeritlerin kendi kiracılarını silmesinden; benim açtığım kiracı silindi). 5199/5200/5241'e dokunulmadı.

## DoD sözleşmesi
Yeni varlık/kayıt türü YOK (yalnız görünüm). Bugün'deki her eylem bağlantıdır (Panel POST ETMEZ): Yeni satış → /islemler, Günü kapat → /kasalar (yalnız Kasa'yı açabilen role), Tahsil et/Öde → /cariler?cariId=…, iş satırları "Aç" → ilgili ekran, dev rakam → işlem defteri, alt şerit → raporlar/ürünler/kâr-zarar (UX-S23). Düğme olarak yalnız kullanıcı menüsündeki Basit/Profesyonel (gerçek PATCH). Boş durum yalnız başarılı boş yanıtta; her yükleyicinin hata bandı ayrı (UX-S28); sunucu sınırına değince toplam yazılmaz, not yazılır (UX-S29).

## Değişen dosyalar
- `dotnet/Kuyumcu.Web/Pages/Panel.razor` (M)
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizBugun.razor`, `YildizBugunVerisi.cs` (yeni)
- `dotnet/Kuyumcu.Web/Bilesenler/Kabuk/YildizUstMenu.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`
- `dotnet/Kuyumcu.Web/wwwroot/font/inter-latin.woff2`, `inter-latin-ext.woff2` (opsz sürümü)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yildiz.cs`
- `dotnet/Kuyumcu.Tests/Yildiz4ATestleri.cs` (yeni), `Yildiz1Testleri.cs`, `Yildiz2Testleri.cs`, `Yildiz3Testleri.cs`
- `dokumanlar/09-uygulama-kayitlari/betikler/tur/tuval-kiyas.mjs` (yeni)
- `dokumanlar/09-uygulama-kayitlari/ham/yildiz-4a/` (kanıtlar)
(Yıldız dosyalarının çoğu git'te hâlâ izlenmeyen `??` durumunda — önceki Yıldız paketlerinden.)

## Koşul karşılama
| Koşul | Durum |
|---|---|
| tuval-kiyas.mjs, Bugun4 + Bugun4a tüm eşlenen öğeler tolerans içinde | ✓ 4×34/34 |
| kuruş/₺ ayrı ölçüldü | ✓ |
| yan yana görüntü 1440 koyu + açık, Read | ✓ (ikisi de bakıldı) |
| 375 taşma 0, tuval dilinde | ✓ (K1-K12 0, yatay 0; gözle açık BASİT) |
| 8 eski şema piksel FARK 0 | ✓ 64/64 |
| bUnit + mutasyon ≥3 kırmızı, cmp AYNI | ✓ 8 mutasyon |
| build 0/0 | ✓ (artımlı 0/0; tam derlemede 6 başka dosya uyarısı) |
| denetim TEMİZ (P-30 hariç) | ✗ yalnız başkasına ait 2 madde (ToListAsync Modules, KaporaGecmisSatiri) — bu paketinkiler ✓ |
| tam süit 0 yeni kırmızı | ✓ (tek yeni kırmızı TARIH_TEK giderildi) |
| BASİT+PRO aynı tuval düzeni, mod anahtarı kullanıcı menüsünde | ✓ |
| tuvalde olmayan öğe yok (kutucuk, selam, "Kutucukları düzenle", yasal bant) | ✓ |

## Açık kalanlar
- "Dünkü gün sonu kapatılmadı" ve "süresi geçen emanet" satırları: veri kaynağı yok → hasımlı pakete devir (kural + uç).
- Diğer ekranlarda (Tezgâh, Müşteriler, Kasalar…) üst bar (`.ust-bar`) Yıldız'da hâlâ görünür (tuvalde yok) — YILDIZ-4B+ ekran paketleri (başlık ana alana taşınınca).
- YILDIZ-3 E1 kaldırıldı; E2-E8 (Tezgâh vb.) hâlâ "okunurluk" uyarlamalı — sonraki birebir paketlerde aynı araçla.
- Tam süitteki başkasına ait kırmızılar (35): KurBantEkran KB20-25, KurEkran, S35KurEkran 07/08, UX1C2Ekran ×3, UX4bEkran, S3eKurKaynagi ×2, GocAEkran GE04 (kur/KUR-BANT şeridi); KaporaEszaman KE01/KE05, SozlesmeYansima (Kapora); S36Kasalar 07-09, Toplu7B* ×9, KesifSozlukTarama P8_04, S3dEkran, S3fEkran (TOPLU-7/Kasa şeridi); S1cUstBar (app.css `overflow-wrap`, sahibi belirsiz — app.css'e bu paket dokunmadı).
- Temizlikte `pkill -f profil3` kullanıldı (kendi `pYILDIZ-4A/profil3` Chrome'umu kapatmak için); başka şeridin aynı adlı profil dizinli bir Chrome'u olsaydı o da kapanırdı — kontrol edilemedi.
- denetim.sh ✗: Modules ToListAsync 142/140, P-03 KaporaGecmisSatiri, P-30.

## Kurucuya / mali müşavire
Mevzuat konusu yok. Kurucuya: (1) yukarıdaki "belirsiz eşleşme" tablosu; (2) gün sonu ve emanet süresi satırları için veri kuralı kararı.
