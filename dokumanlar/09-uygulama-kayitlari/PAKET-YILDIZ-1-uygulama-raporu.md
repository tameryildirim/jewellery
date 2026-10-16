# PAKET YILDIZ-1 uygulama raporu (08.10.2026)

Kapsam: TEMA-YILDIZ-PAKET-NOTU § 4 adım A+B+C. Hasımsız paket; kendi kanıtı bağlayıcı.
Para/veri/yasal davranışa DOKUNULMADI. Mevzuat hükmü yok.

## Yapılanlar
- Inter vendor (latin + latin-ext woff2, OFL lisansı) wwwroot/font/; @font-face tema-yildiz.css içinde; CSP font-src 'self' korundu.
- wwwroot/css/tema-yildiz.css (TEK yeni dosya, yalnız :root[data-sema="yildiz"] altında): belirteçler açık (#F5F4F1) + koyu (#000, sistem-koyu dahil), okunurluk kararı (200 ağırlık yok, büyük sayı >=300, gövde 400, 15 px altı 500), ortak bileşen varyantları (kartsız bölüm, b1/b2/b3 düğme, altı çizili sekme, çip->metin, liste satırı, boş durum, adımlar, takvim).
- tema.js yalnız SEMALAR listesi; index.html yalnız link/preload; Ayarlar.razor yalnız şema seçici önizleme öğesi; sözlük "Yıldız" adı (TR+EN); sunucu şema listesi 9'a çıktı (GorunumModulu/GorunumServisi/GorunumSozlugu).
- Kullanıcı tercihi yolu ile kalıcı; mevcut 8 şema ve sol menü değişmedi (kabuk/D-E adımları kapsam dışı).

## Bu turda tamamlanan (devir sonrası)
- 5 beklenti kırmızısı (TasarimBelirtec Sema_beyaz_listesi, B1H2 8->9, BE2 9->10, S1f_E1 8->9, S1f_E14 18->19) yeni 9. şemaya uyarlanmış; filtreli koşumda yeşil.
- denetim OLCEK-1 YUZ_OLCU_ENVANTER_DISI 3 + OL26: Kuyumcu.Tests/OlcekYuzEnvanteri.txt'e 3 onaylı satır eklendi (tema-yildiz.css, tuval-tezgah .ekran-karo padding-left/right 8px ve border-radius 999px; mod-bağımsız tek kural, yalnız yıldız şemasında).

## Testler
- Yeni Yildiz1*.cs: 18 Fact/Theory (Yildiz1Testleri + Yildiz1HttpTestleri); filtreli (Yildiz1|TasarimBelirtec|B1|S1f|OlcekTek) 118 test 0 başarısız (envanter düzeltmesinden sonra OL26 yeşil).
- Tam süit: 11211 toplam, 11208 geçti, 2 atlandı, 1 başarısız = FaturaEkranTestleri.Y4C_K12_... (basit:False): rastgele GUID içinde "805" alt dizisi -> KARARSIZ test, benim paketimle ilgisiz; tek başına tekrar koşuda 2/2 yeşil. Sahibi: FATURA/Y4C şeridi (GUID'e bağımlı düzeltilmeli). Yeni kırmızı = 0.
- Derleme 0 uyarı 0 hata. denetim.sh: yalnız P-30 DEMO_INSAN_SURESI ✗ (paket dışı, görev metni "P-30 hariç"); OLCEK-1, P-36, P-36b ✓.

## Mutasyonlar (mutlar-css.out / mutlar-cs.out; hepsi cmp AYNI geri alındı)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M1 | tema-yildiz.css'te seçici :root kapsamından çıkarıldı | Y1_02_tema_yildiz_css_HER_kural_yildiz_kapsamlidir |
| M2 | kontrast çifti bozuldu | Kontrast_AA_saglanir, Y1_03, Y1_05 |
| M3 | belirteç silindi / blok farklılaştı | Y1_03, Y1_04 |
| M4 | tema.js SEMALAR'dan yildiz çıktı | Y1_01 |
| M5 | index.html css sırası | Y1_08 |
| M6 | Inter yerel yol | Y1_09 |
| M7 | 200 ağırlık / büyük sayı | Y1_06 |
| M8 | kağıda sızma | Y1_07 |
| M9 | zemin rengi | Kontrast_AA_saglanir, Y1_03 |
| M10 | sunucu şema listesinden yildiz | B1_1, B1H2, GE8, Y1H1, Y1H2, Y1_01 |
| M11 | koyu blok değeri | Y1_03 |
| M12 | font lisansı/ dış kaynak | Y1_09 |
| M14, M15 | bileşen varyantı silindi | Y1_12 |
| M16 | istemci şema/ad listesi | S1f_K6, Y1_01 |
| M17 | Ayarlar önizleme öğesi | BE2, GE8, S1f_E14, S1f_E1, Y1_01 |
| MP | seçici :root'a genişletildi (8 eski şema) | piksel karşılaştırması 32/32 FARK (fark-mut.out) |
(M13 numarası kullanılmadı.)

## Piksel karşılaştırması (8 eski şema x 2 mod, Panel+Satış, 1440/375, BASIT+PRO; yıldız css açık/kapalı)
fark-son.out: 32 karşılaştırma, FARKLI 0.

## Gerçek (başsız) Chrome ölçümü (olc-son.txt, 24 kombinasyon, ekranlar Panel/İşlemler/Cariler)
1440+375 x BASIT+PRO x açık+koyu: zemin açık rgb(245,244,241), koyu rgb(0,0,0); K1(gizli görünen) 0, K3 taşan 0, K5 TRY 0, K9 0, K10 0, K12 0, yatay 0, Y1 küçük-ince 0, Y2 büyük-ince 0, Y4 birincil düğme hap (999px) 1 her ekranda, inter=2 (latin + latin-ext yüklü). Y3 (kart kalıntısı): 375'te 0; 1440'ta yalnız Cariler tablo yapışkan sütun gölgesi (th/td sticky, kart değil: BASIT 1, PRO 9). Ağsız (agsiz.mjs) Chrome: Inter vendor dosyadan yüklendi. Görüntüler: /private/tmp/kuyumcu-serit/pYILDIZ-1/foto/ (24 png; panel/islemler/cariler).
Kağıt (.belge*, etiket, print) tema-yildiz.css'te yok (Y1_07).

## Canlı API (5166) + SQL
Test kiracısı PAKETYILDIZ1-TEST: Ayarlar>Görünüm şema listesi 9 öğe (yildiz dahil), seçim kalıcı (Y1H1), dükkân varsayılanı ve geçersiz kod 400 (Y1H2) testlerle ve canlı oturumla doğrulandı. İş sonunda API (PID 45896) ve vekil (5167) durduruldu; 5166/5167 boş. Kiracı silindi: tenants 18 -> 17 (QA-TEST kiracılarına dokunulmadı).

## Sözleşme-DoD
Yeni varlık/kayıt türü YOK (yalnız CSS/font/sözlük/liste); mevcut kullanıcı-görünüm tercihi ucu aynı PATCH sözleşmesiyle. Dokunulan sayfa: Ayarlar.razor (tek dokunuş, yalnız önizleme öğesi).

## Değişen dosyalar (ana)
tema-yildiz.css, font/Inter*+OFL, tema.js (SEMALAR), index.html (link/preload), Ayarlar.razor, Ayarlar.cs (Core), Sozlukler*/GorunumSozlugu, Gorunum modülü/servisi, Kuyumcu.Tests: Yildiz1*.cs, TasarimBelirtecTestleri, B1/S1f beklentileri, OlcekYuzEnvanteri.txt.

## Açık kalanlar
- Kabuk (üst 5 kelime menü, Tüm ekranlar karesi, D) ve ekran ince ayarı (E) bu paketin kapsamında DEĞİL (not § 4: ayrı paketler); kurucu kararı "Yıldız ayrı tema" için D paketi gerekir.
- FaturaEkranTestleri.Y4C_K12 GUID'e bağımlı kararsız (sahibi: Fatura şeridi).
- denetim P-30 (kronometreli demo ölçümü) paket dışı.
## Kurucuya / mali müşavire
Mevzuat konusu yok.
