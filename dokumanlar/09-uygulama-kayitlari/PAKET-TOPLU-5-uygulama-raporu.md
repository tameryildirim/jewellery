# PAKET TOPLU-5 — Uygulama raporu (para ekranları görünüm kuyruğu, hasımsız)

Taşkın + Ertuğ (Opus). 02.10.2026, tur 1. Dalga TOPLU-5 · t2 · API 5162 / vekil 5163 / CDP 9905–9908 · `KUYUMCU_TEST_DB=kuyumcu_test_t2`.
Kaynak: `ham/TOPLU-TASARIM-KUYRUGU-2026-09-29.md` bölümleri "01.10 19:35 Uzun demo", "01.10 19:50 TOPLU-2 hakem t2", "02.10 05:57 TOPLU-3 t2", "02.10 13:05 PARA-DEMO-2 t2", "02.10 21:14 PARA-DEMO-3 t2" (Cari/Çıktı/Gider/Kasalar/Virman/İşlemler/Döviz/Gün sonu maddeleri).
Kanıtlar: `ham/TOPLU-5-olcum/` (`once/`, `ara/`, `son/` — 240 ölçüm + 240 görüntü, `ek/` — 16 ölçüm, `canli/`, mutasyon sonuçları aşağıda).

Para / veri / yasal davranış DEĞİŞMEDİ. Migration yok. Sunucu kodu değişmedi.

## 1. Madde tablosu (önce / sonra / kanıt)

| # | Madde (kaynak) | Önce (ölçüm, 02.10 21:2x, başsız Chrome) | Sonra | Kanıt (test · mutasyon) |
|---|---|---|---|---|
| C1 | YÜKSEK Basılı ekstre önizleme: kimlik uyarısı + değişmezlik notu iki amber kutu, düğme tekrarı | Kimlik kutusu TOPLU-3'te sadeleşmişti (başlık/yol gizli, tek düğme). Kalan: alttaki "Bu kayıt değiştirilemez" kutusu 3 satır (gerekçe + "Düzeltme şöyle…") | Değişmezlik notu TEK satır (45 px, 375'te 83 px): "Belge kayıttan üretilir; burada düzeltilmez." + ⓘ (gerekçe ve düzeltme yolu ⓘ içinde, metin silinmedi). Kimlik kutusu id'leri/yapısı AYNEN (testler ve id sözleşmesi) | `T5_Cikti_degismez_notu_TEK_satir…` · M5-1 |
| C2 | ORTA Fişte "işletme kartı eksik" kutusu dili + demo-tohumla işletme bilgisi | TOPLU-3 kutuyu sadeleştirdi, demo kimliği doldurdu ama "(örnek adres)", "Örnek Vergi Dairesi" | demo-tohumla: "Gümüşhane Sokak No: 12, Kuyumcular Çarşısı, Merkez / Ankara", "Merkez Vergi Dairesi", vergi no 0000000000 (kurgusal olduğu belli). 'DEMO' filigranı YOK. Canlıda fiş: "… · Vergi no / TCKN: 0000000000 · 0532 000 01 00" | `T5_Isletme_kimligi_dogal…` · M5-4 · `canli/sql.txt` |
| C3 | ORTA Ekstre telefon biçimsiz | "05320000104" | "0532 000 01 04" (karşı taraf ve düzenleyen; ekranlardaki tek kaynak `TelefonBicimi.Goster`) | `T5_Belgede_telefonlar…` · M5-2 |
| C4 | ORTA Ekstrede "0,0000 has" | Tutarlar: "Has gram bakiye 0,0000 has" | Sıfır has ARA satırı basılmaz; toplam ve sıfır olmayan has satırı aynen | `T5_Sifir_has…`, `T5_Basilir…` · M5-3 |
| C5 | /cikti "Yazdır" koyu temada soluk | TOPLU-3 ölçümle kapatmıştı | 16 bileşimde ölçüldü: açık zemin rgb(36,87,214)/yazı rgb(253,253,255), koyu rgb(145,185,255)/rgb(9,21,44) — kapalı | `son/olcum.jsonl` ekstre.yazdirKontrast |
| C6 | (ölçümde bulundu) Ekstre PRO/BASİT 960 K3 kırık 8 | 7 sütun, "Has etkisi" bütün satırlarda boş | Hiçbir satırında değeri olmayan sütun çizilmez → kırık 8 → 3 | `T5_Hicbir_satirinda_degeri…`, `T5_BosSutunlar…` · M5-33, M5-34 |
| K1 | ORTA PRO /cariler sayfa adı iki kez | H1 "Müşteriler" + H2 "Cariler" | Şerit başlığı `UstBardaGizle` (DOM'da, görsel gizli) — 16 bileşimde görünen başlık yalnız "Müşteriler" | `T5_Serit_basligi…` · M5-5 |
| K2 | ORTA /cariler PRO 960 sağ kırpık (K13) | tablo-sar 762/642 (Döviz + Durum sütunları kesik) | Döviz sütunu iki modda da yalnız döviz bakiyesi varken; Durum sütunu PRO'da yalnız "Pasifleri göster" açıkken (kapalıyken her satır "Aktif"). 960: 642/642 | `T5_PRO_doviz_sutunu…`, `T5_PRO_durum_sutunu…` · M5-7, M5-35 |
| K3 | ORTA 12 px "En çok 100 kayıt…" notu | PRO'da hep görünür | PRO da BASİT gibi yalnız sınıra değince | `T5_Sinir_notu…` · M5-6 |
| K4 | ORTA /cariler iki farklı çekmece kalıbı | Döviz çekmeceleri 480 px ortak kalıp; Düzenle 440 px tuval kalıbı | Döviz Veresiye + Döviz Tahsilat aynı tuval ölçüsünde (≥1024: 440 px, dolgu 24/26, başlık çekmece boyu, alt şerit kenarsız, Kaydet esnek). 1440/1280'de 440 ölçüldü | `T5_Doviz_cekmeceleri…`, `T5_Doviz_cekmecesi_musteri…` · M5-10, M5-28 |
| K5 | ORTA PRO Döviz Veresiye 3 satır açıklama + "sunucu hesaplar", boş tutar mesajı | Üç 12 px paragraf; "TL karşılığı (bilgi amaçlı — sunucu hesaplar)" | Üç paragraf tek ⓘ (id'ler aynen); etiket "TL karşılığı" (+ "Kurdan geldi" rozeti); tutar silinince alan altında "Tutarı girin." (role=alert). BASİT'te ⓘ yok (zorunlu önce) | `T5_PRO_doviz_veresiye…`, `T5_BASIT_doviz…`, `T5_Bos_tutar…` · M5-8, M5-9, M5-11 |
| K6 | ORTA Tahsilat uyarısındaki bağlantı fark edilmiyor (K22) | Müşteri paneli gecikme bandındaki "Tahsilat" bağlantısı bant metniyle aynı görünümde | Alt çizgili + kalın (renk belirteci aynı) | `T5_Kirilim_parcasi…tahsilat_baglantisi` · M5-32 |
| G1 | YÜKSEK Gider kasa seçicisi TOPLAMI gösteriyor | "Ana Kasa (₺) · 216.066,76 ₺" | "Ana Kasa (₺) · nakit 193.408,90 ₺" (sunucu /kasalar/nakit-ayrimi). Ayrım okunamazsa "· toplam X" etiketli (P-50 "seçmeden görülür" korunur) | `T5_Kasa_seciciinde…` + `GiderEkranTestleri.Kasa_secilince…` · M5-12 |
| G2 | ORTA Nakit aşımında aynı bilgi iki kez, "havale (0 ₺)" | Bant "…çekmecede 193.408,90 nakit var… kart (…) ve havale (0 ₺)…" + kilit aynı sayıyı tekrar | Bant yalnız kilidin söylemediğini: "Kasadaki kartla alınan 22.657,86 ₺ çekmecede değil; gider yalnız nakitten ödenir." (sıfır kalem yok); kilit "Çekmecedeki nakit yetmiyor: 193.408,90 ₺ var". Sunucu reddi metni DEĞİŞMEDİ | `T5_Nakit_asiminda…`, `T5_Nakit_bant_metni…`, `PD3T2_gider_kilidi…` (beklenti güncellendi) · M5-13, M5-14 |
| G3 | DÜŞÜK 375'te Gider sekme şeridi iki satır | şerit 100 px (app.css ≤600 px 2 sütun ızgara) | Yalnız bu şerit tek satır esnek dizi: 52 px (BASİT/PRO açık/koyu 375) | `T5_Gider_sekme_seridi…` · M5-27 · `ek/` |
| G4 | DÜŞÜK Boş listede "Bu kayıt değiştirilemez" | Boş giderde kutu görünüyor | Kutu yalnız listede kayıt varken (metin/id aynen) | `T5_Bos_gider_listesinde…` · M5-15 |
| G5 | DÜŞÜK Gider asıl sayı en küçük yazı | 12 px kasa/nakit/toplam satırları | ≥14 px; nakit rozeti --yazi-md kalın (16–18 px); BASİT'te nakit görünürken "Kasa toplamı (kart ve havale dahil)" satırı yok, PRO aynen | `T5_Gider_sayilari…`, `T5_BASIT_kasa_toplami…` · M5-16, M5-31 |
| V1 | ORTA Virman BASİT jargon + hizasız alt not | "Virman (kasa ↔ banka aktarımı)", "iki bacak… ciroya…"; alt not kart kenarına yapışık, boş listede de | BASİT: "Para aktarımı (kasa ↔ banka)", "Kasadan bankaya ya da bankadan kasaya para aktarır; satış ya da gider sayılmaz."; "Ne aktarılıyor?" açıklaması ⓘ; alt not kart dolgusuyla hizalı, yalnız kayıt varken, BASİT dili. PRO metinleri aynen | `T5_BASIT_virman…`, `T5_Virman_iki_taraf…` · M5-17, M5-18 |
| KS1 | DÜŞÜK Kasalar "Nakit · Kart/Havale" 12 px soluk | 12 px rgb(90,101,119) | 14 px gövde rengi (16 bileşim) | `T5_Kasalar_ayrim…` · M5-26 |
| I1 | YÜKSEK BASİT Değişim kimlik başlığı "Müşteri kimliği (MASAK)" | İşlemler MasakFormu çağrısı `Sade` almıyordu | BASİT: "Kimlik bilgisi gerekli (tutar yüksek)" (Banka/Hurda ile aynı), kısaltmasız alan adı; PRO: yasal atıflı başlık AYNEN. Kapı kuralı aynen (Kaydet kilitli) | `T5_BASIT_degisim_kimlik…`, `T5_PRO_degisim…` · M5-23 · görüntü `degisim-masak__BASIT_aydinlik_1440.png` |
| I5 | DÜŞÜK kilit "TCKN" ↔ alan "Belge No" | "Eksik: TCKN, adres…" | Kilit alanın KENDİ etiketiyle: BASİT "T.C. kimlik numarası", PRO "Belge No (TCKN / Pasaport)" (koşul aynen) | `PD3E_T_*`, `PD2E_8_*` (beklenti güncellendi) · M5-25 · canlı: "Eksik: satıcı türü, T.C. kimlik numarası, işlem amacı, …" |
| GS1 | ORTA BASİT gün sonu kırılımı iki satır | "Tahsilat …: Nakit … · Kart … · Havale 0 ₺" 12 px, tutar ortasından kırılıyor | Sıfır kalem yok; her kalem bölünmez parça (kırılım kalem sınırında); 14 px. 1440 kartında hâlâ iki satır (336 px kartta 14 px'te sığmıyor) — ama tutar bölünmüyor | `T5_BASIT_kirilimda…` · M5-19, M5-30 |
| GS2 | ORTA Gün sonunda aynı tutar iki biçimde | "Fark var 98.487,29 ₺" + "Faturasız satış: 9 · 98.487,29 ₺" | Faturasız tutar farkın kendisiyse ikinci kez basılmaz: "Faturasız satış: 9" | `T5_BASIT_faturasiz…` · M5-20 |
| GS3 | DÜŞÜK PRO gün sonu: "FARK VAR", formül satırı, iki sarı uyarı | "FARK VAR" büyük harf; formül açık paragraf; iki sarı uyarı KAR-RAPOR'da tek kutuya inmişti | Durum "Fark var"/"Tutuyor"; formül "Nasıl hesaplanır?" ⓘ (id `#mut-fark-tanimi` aynen); tek uyarı kutusu (ölçüldü) | `T5_PRO_gun_sonu…` · M5-21, M5-22 |
| GS4 | ORTA Gün sonu "Tahsilat" kasa bakiyesine eşit | Ölçüm: tahsilat 66.066,76 ≠ kasa 216.066,76 (açılış devri 150.000 ayrı satır, PARA-DEMO-2 md.5) | Kod değişmedi — KAPALI | `canli/mutabakat.txt` |
| I3 | ORTA Kayıttan sonra ödeme paneli başarı bandını örtüyor | PARA-DEMO-2 md.11 `proOdemeAcik=false` | Kod değişmedi — KAPALI | `ParaDemo2EkranTestleri.PD2E_11_…` yeşil |

## 2. Yapılmayan / devredilen maddeler (gerekçeli)

| Madde | Neden | Devir |
|---|---|---|
| ORTA "Değişim seçiliyken sayfa başlığı 'Satış'" | Üst bar başlığı rotadan gelir (`Layout/UstBar.razor`, dosya alanı DIŞI). Üst barın sayfa yuvası (`UstBarSekmeleri`) denendi; yuva doluyken kabuk kur şeridini ve Basit/Profesyonel anahtarını gizliyor (tuval-kabuk.css (2), Ayarlar tuvali kuralı) — gerçek tarayıcı ölçümünde görüldü, GERİ ALINDI (`sonra/foto/bolunmus__BASIT_aydinlik_1440.png` yan etki kanıtı). | UstBar başlığı /islemler'de seçili türü okumalı (Layout sahibi). |
| DÜŞÜK "BASİT satışta ödeme bölününce fiş tek sütuna düşüyor" | Boş fişte "+ Başka ödeme ekle" tek bacakta kalıyor; bölünmüş durum betikle üretilemedi (ürün kalemi gerekir). Ölçülemedi → yapılmadı (varsayım yok). | Sonraki görünüm turunda kalemli fişle ölç. |
| DÜŞÜK "Raporlar PRO 1280 yıl/ay seçici ezik" | Raporlar'ın gün sonu DIŞI bölümü (KAR-RAPOR-2 şeridi). | KAR-RAPOR-2 sonrası. |
| DÜŞÜK "Müşteri detayı yüzen pencere" | Kalıp kararı (sürüklenebilir modal → çekmece) ekip oyu ister; "yalnız görünüm" sınırını aşar. | Görünüm turu, ekip oyu. |
| ORTA "Değişimde 30.000 ₺ üstü fark Nakit kalıyor" + DÜŞÜK hasım "Günü Kapat sayılan nakit zorunlu değil" | YASAL / sunucu davranışı — paket sınırı. | Hasımlı pakete devir. |
| Ekstre 960'ta 3 hücre iki satır (K3) | A4 önizleme 960'ta kâğıttan dar; "Satış · Veresiye", "Tahsilat · Nakit", "Veresiye ara ödemesi" kelime sınırında kırılıyor; tutar sütunları tek satır. 8 → 3. | Kabul (gerekçeli). |

## 3. Gerçek tarayıcı ölçümü (başsız Chrome, `betikler/tur/cdp.mjs` + `sikisma-olc.js`)

Yayın: `dotnet publish` → `/private/tmp/kuyumcu-serit/TOPLU-5/yayin-son` (eşzamanlı şeritlerin bin'i bozmasına karşı kendi kopyam), parolasız oturum `inceleme-vekili.py` (5163). Matris: 15 senaryo × {1440, 1280, 960, 375} × {BASİT, PRO} × {açık, koyu} = **240 ölçüm, 240 tam sayfa görüntü, hata 0, mod uyuşmazlığı 0**. Ek: gider sekme/nakit aşımı 16 ölçüm (`ek/`; CSS düzeltmesi sonrası `toplu5.css` yayın kopyasına aynen konarak).

Ekran başına toplam (16 bileşim):

| Senaryo | K1 gizli | K3 taşan/kırık | K5 TRY | K8 jargon | K9 binen | Yatay | Not |
|---|---|---|---|---|---|---|---|
| /cariler | 0 | 0/0 | 0 | 0 | 0 | 0 | görünen başlık yalnız "Müşteriler"; tablo-sar 960: 642/642 |
| /cariler Döviz Veresiye | 0 | 0/0 | 0 | 0 | 96 | 0 | K9 = çekmece arka katmanı altındaki liste (önce de 6/bileşim); çekmece 440 px (≥1024) |
| /cariler DV boş tutar | 0 | 0/0 | 0 | 0 | 96 | 0 | "Tutarı girin." 16/16 |
| /cariler Döviz Tahsilat | 0 | 0/0 | 0 | 0 | 96 | 0 | aynı (katman) |
| /cikti ekstre | 0 | 0/12 | 0 | 0 | 0 | 0 | kırık yalnız 960 (3/bileşim, önce 8) |
| /cikti satış fişi | 0 | 0/0 | 0 | 0 | 0 | 0 | doğal işletme kimliği |
| /gider (seçici açık) | 0 | 0/0 | 0 | 0 | 36 | 0 | K9 = açık seçici listesi alttakini örtüyor (katman) |
| /gider nakit aşımı | 0 | 0/0 | 0 | 0 | 0 | 0 | bant+kilit tekrarsız 16/16 (`ek/`) |
| /gider virman | 0 | 0/0 | 0 | 0 | 0 | 0 | BASİT başlık/açıklama sade |
| /kasalar | 0 | 0/0 | 0 | 0 | 0 | 0 | ayrım satırı 14 px |
| gün sonu BASİT kart | 0 | 0/0 | 0 | 0 | 0 | 0 | sıfır kalem yok |
| gün sonu PRO sekme | 0 | 0/0 | 0 | 0 | 0 | 0 | "Fark var", ⓘ, büyük harf 0 |
| /islemler Değişim | 0 | 0/0 | 0 | 0 | 0 | 0 | kur şeridi + mod anahtarı görünür |
| /islemler Değişim + kimlik | 0 | 0/0 | 0 | 0 | 32 | 0 | K9 = açık "Satıcı tipi" listesi (katman) |
| /islemler satış | 0 | 0/0 | 0 | 0 | 0 | 0 | — |

Gider sekme şeridi (ek): 1440 ve 375'te 52 px, üç sekme aynı y'de (BASİT/PRO, açık/koyu).
Görüntülere Read ile bakıldı: `son/foto/gunsonu__BASIT_aydinlik_1440.png`, `son/foto/cariler-dv-bos__BASIT_aydinlik_1440.png`, `son/foto/degisim-masak__BASIT_aydinlik_1440.png`, `son/foto/gunsonu-pro__PRO_karanlik_1280.png`, `ek/foto/gider-asim__BASIT_karanlik_375.png`, `ara/foto/ekstre__PRO_aydinlik_960.png`, `ara/foto/cariler__PRO_aydinlik_960.png`.

## 4. Canlı doğrulama (API 5162, test kiracısı `PAKETTOPLU-5-TEST Kuyumculuk` 01M3YXJVZHACV816299C45X3EH)

- Kurulum: `demo-tohumla.js --prova-kiraci` (ürünün kendi uçları; 6 cari, 12 ürün, 9 satış). Sistem anahtarı oturumluk rastgele, yalnız ortam değişkeninde; dosya iş sonunda boşaltıldı.
- `PATCH /admin/tenant/kimlik` (yeni demo kimliği) **200**; `POST /kasalar` şubesiz **400 "Şube zorunludur"** (doğru ret), şubeyle **200**; `POST /gider-kategorileri` **200**.
- `GET /kasalar/nakit-ayrimi` **200**: Ana Kasa toplam 216066.76 = nakit 193408.90 + kart 22657.86, havale 0 → ekranda seçici "nakit 193.408,90 ₺", bantta yalnız kart payı.
- `GET /raporlar/pos-fatura-mutabakati` **200**: tahsilat 66066.76 (nakit 43408.90, kart 22657.86, havale 0), açılış devri 150000, fark 98487.29 = faturasız 98487.29 → ekranda havale yok, faturasız satırda tutar tekrarı yok.
- `GET /cikti/CARI_EKSTRE/{Kemal}` **200**: telefon "05320000104", has bakiye 0.000000 → ekranda "0532 000 01 04", has satırı yok.
- SQL (`canli/sql.txt`): tenants adres/vergi dairesi/vergi no yeni değerler; cariler.telefon ham "05320000104" (veri DEĞİŞMEDİ, yalnız görünüm); kasalar Ana Kasa 216066.76 + Vitrin Kasası 0.00; hareketler KART 22657.86.
- Kapanış: API ve vekil `kill` ile durduruldu; 5162, 5163, CDP 9900–9920 dinleyici yok. `zsh kiraci-temizle.sh 01M3YXJVZHACV816299C45X3EH --uygula` kod 0 (75 tablo). SQL: bu kimlik için tenants 0, cariler 0, hareketler 0. tenants başlangıçta 18, sonda 16 — fark başka şeritlerin kendi kiracıları (3 × PAKETKAR-RAPOR-2-TEST-HAKEM silindi, 1 × PAKETMADEN-AYRIMI-TEST-M eklendi); bu paket +1/−1. QA-TEST-* kiracılarına dokunulmadı.

## 5. Testler

- Yeni: `Kuyumcu.Tests/Toplu5Testleri.cs` — 36 test metodu (Theory dahil 45 koşum), 7 sınıf (Çıktı, demo tohum, Cariler, Gider, Gün sonu, İşlemler, Kaynak/CSS/sözlük).
- Beklentisi güncellenen eski testler (kasıtlı görünüm değişikliği; önce yedekleri `/private/tmp/kuyumcu-serit/TOPLU-5/once-yedek/`):
  - `ParaDemo3Tur2GiderTestleri.PD3T2_gider_kilidi_cekmecedeki_nakde_bagli` (bant metni)
  - `ParaDemo3TakasKilitTestleri.PD3E_T_BASIT_…`, `PD3E_T_PRO_…` ("TCKN" → alan etiketi)
  - `ParaDemo2IslemEkranTestleri.PD2E_8_…` (aynı)
  - `S32MusterilerEkranTestleri.S32_07_…` (PRO Durum sütunu)
  - `TM1MusterilerTuvalTestleri.TM1_09_…` (PRO sınır notu)
- Filtreli: Toplu5 + ParaDemo3 85/85; geniş ilgili küme (Toplu*, ParaDemo*, Cari*, Gider*, Kasa*, Raporlar, İşlem, MASAK, Çıktı …) 2229/2231 yeşil (2 atlanan), Cari/Çıktı kümesi 99/99.
- Tam süit (tur 1, `trx-pTOPLU-5`): 9724 test, 9720 geçti, 2 başarısız, 2 atlandı. İki kırmızı tek başına 3/3 yeşil (kararsız, yük altında): `SATIS3EkranTestleri.T13_MOD_ve_SEKME_degisince_liste_kendiliginden_ACILMAZ`, `MadenAyrimiDbTestleri.MA04_Stok_yasi_kova_gumus_fiziksel_ve_saf_gram` (stok yaşı — KAR-RAPOR-2/MADEN-AYRIMI alanı). Bu paketin kodu ikisine dokunmuyor → **yeni kırmızı farkı 0**.
- Tam süit tur 2 ve 3 (son kodla): başka şeridin eşzamanlı derleme/temizliği `Kuyumcu.Tests/bin` ve `Kuyumcu.Api/bin` içindeki derlemeleri koşu sırasında sildi; 1454 HTTP/DB testi "Could not load … Microsoft.IdentityModel.Tokens / Can't find …deps.json" ile düştü (altyapı). Çözüm: derleme çıktısının anlık kopyası `/private/tmp/kuyumcu-serit/TOPLU-5/kok/dotnet/testbin` (kök dizinler sembolik bağ; testler kaynak dosyaları gerçek ağaçtan okur).
- **Tam süit tur 4 (son kod, yalıtılmış kopya, `trx-pTOPLU-5`): 9738 test · 9734 geçti · 2 başarısız · 2 atlandı.** Kırmızılar bu paketin değil: `P37cPanelVeKapsamTestleri.Agacta_dolu_mutasyon_yedegi_veya_bak_dosyasi_KALMAZ` → `dotnet/denetim.sh.mutbak` (oturum başında ağaçta vardı; bu paket üretmedi), `PatchUcServisTestleri.P3_servis_alis_fisi_kalem_birim_fiyat_kolon_tavani_RET` (PATCH-3 şeridi, SatisModulu girdi doğrulaması). **Yeni kırmızı farkı 0.**

## 6. Mutasyon tablosu (35 adet; yöntem: yedek → TEK eşleşmeli değişiklik → gerekiyorsa derle → filtreli test → geri yükle → `cmp` AYNI)

Not: tek eşleşme kontrolü `perl` yerine python ile yapıldı (aynı etki; eşleşme sayısı ≠ 1 ise mutasyon koşulmaz). Sonuçlar `/private/tmp/kuyumcu-serit/TOPLU-5/mut/sonuc-*.json`.

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M5-1 | Çıktı değişmezlik notu eski 3 satıra | T5_Cikti_degismez_notu_TEK_satir… | AYNI |
| M5-2 | belge telefonu ham | T5_Belgede_telefonlar_ekrandaki_TEK_bicimle | AYNI |
| M5-3 | sıfır has satırı basılır | T5_Basilir…, T5_Sifir_has_bakiye_satiri… | AYNI |
| M5-4 | demo adres "(örnek adres)" | T5_Isletme_kimligi_dogal… | AYNI |
| M5-5 | UstBardaGizle=false | T5_Serit_basligi_ust_bar_tekrari… | AYNI |
| M5-6 | PRO sınır notu hep | T5_Sinir_notu_sinirin_altinda_YOK | AYNI |
| M5-7 | PRO döviz sütunu hep | T5_PRO_doviz_sutunu… | AYNI |
| M5-8 | DV ⓘ kimliği bozuk | T5_PRO_doviz_veresiye_uc_aciklama… | AYNI |
| M5-9 | TL karşılığı eski etiket | T5_PRO_doviz_veresiye_uc_aciklama… | AYNI |
| M5-10 | çekmece sarmalayıcısı yok | T5_Doviz_cekmeceleri_musteri_cekmecesiyle… (2) | AYNI |
| M5-11 | boş tutar mesajı yok | T5_Bos_tutar_alan_altinda… | AYNI |
| M5-12 | seçicide nakit yok | T5_Kasa_seciciinde_CEKMECEDEKI_NAKIT… (2) | AYNI |
| M5-13 | bant eski nakit cümlesi | PD3T2_gider_kilidi… (2), T5_Nakit_asiminda… | AYNI |
| M5-14 | sıfır havale yazılır | T5_Nakit_asiminda… | AYNI |
| M5-15 | boş listede değişmez kutusu | T5_Bos_gider_listesinde… (2) | AYNI |
| M5-16 | BASİT toplam satırı | T5_BASIT_kasa_toplami_satiri_YOK_PRO_VAR | AYNI |
| M5-17 | BASİT virman başlığı eski | T5_BASIT_virman_basligi… | AYNI |
| M5-18 | virman notu boş listede | T5_Virman_iki_taraf_notu… (2) | AYNI |
| M5-19 | sıfır nakit kalemi yazılır | T5_BASIT_kirilimda_SIFIR_kalem_yok… | AYNI |
| M5-20 | faturasız tutar tekrarı | T5_BASIT_faturasiz_satir… | AYNI |
| M5-21 | "FARK VAR" geri | T5_PRO_gun_sonu_durumu… | AYNI |
| M5-22 | formül ⓘ kimliği bozuk | T5_PRO_gun_sonu_durumu… | AYNI |
| M5-23 | MasakFormu Sade yok | T5_BASIT_degisim_kimlik…, T5_Islemler_razor…Sade_tasir | AYNI |
| M5-24 | (geri alınan üst bar denemesi) | T5_Degisim_seciliyken_ust_bar… (2) — madde geri alındı, test kaldırıldı | AYNI |
| M5-25 | kilit adı hep PRO etiketi | PD3E_T_BASIT_…, PD2E_8_…(BASIT) | AYNI |
| M5-26 | kasa ayrım 12 px | T5_Kasalar_ayrim_satiri_14px… | AYNI |
| M5-27 | sekme şeridi display:flex yok | T5_Gider_sekme_seridi_dar_ekranda_TEK_satir | AYNI |
| M5-28 | DV çekmece 480 px | T5_Doviz_cekmecesi_musteri_cekmecesi_olcusunde | AYNI |
| M5-29 | index.html toplu5 bağı yok | T5_toplu5_css_toplu4ten_sonra… | AYNI |
| M5-30 | kırılım parçası bölünür | T5_Kirilim_parcasi_bolunmez… | AYNI |
| M5-31 | nakit rozeti kalın değil | T5_Gider_sayilari_en_az_14px… | AYNI |
| M5-32 | tahsilat bağlantısı alt çizgisiz | T5_Kirilim_parcasi_bolunmez_ve_tahsilat_baglantisi… | AYNI |
| M5-33 | boş sütun çizilir | T5_Hicbir_satirinda_degeri_olmayan_sutun… | AYNI |
| M5-34 | boşluklu hücre "dolu" sayılır | T5_BosSutunlar_saf_kural, T5_Hicbir_satirinda… | AYNI |
| M5-35 | PRO Durum sütunu hep | T5_PRO_durum_sutunu…, S32_07_… | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 uyarı 0 hata.

## 7. Derleme ve denetim

- `dotnet build Kuyumcu.sln --nologo`: **0 Uyarı, 0 Hata** (son temiz derleme). Ara bir derlemede görülen 2 uyarı başka dosyalardaydı (SUBEKAPSAMASinirTestleri.cs, KesifP2Testleri.cs) ve sonraki temiz derlemede yok. Bir ara KarZarar.razor (KAR-RAPOR-2 şeridi) yarım değişikliği Web derlemesini kırdı; beklenip düzelince devam edildi.
- `bash denetim.sh`: 303 ✓, **1 ✗ = P-30 DEMO_INSAN_SURESI** (kabulde hariç tutulan, kurucu kalemi). Sayaçlar: Pages ModalPencere 48 (tavan 55), duzenlenenId 1 (tavan 1) — artış yok.

## 8. DoD sözleşmesi

Bu paket yeni varlık / kayıt türü / uç AÇMADI. Dokunulan düzenleme yolları aynen: Döviz Veresiye/Tahsilat çekmeceleri (POST uçları aynen, Cekmece kalıbı), Gider (storno yolu ve DegismezIpucu gerekçesi aynen; yalnız boş listede kutu yok), Çıktı (türetilmiş belge — DegismezIpucu gerekçesi ⓘ içinde, düzeltme yolu kaynaktadır). Her yeni metin `Sozlukler.Toplu5.cs` (TR+EN, konumsal record — eksik anahtar derleme hatası). Yeni CSS yalnız belirteç (sabit renk testi). Mevcut id'ler korundu; yeni id'ler: `#cikti-degismez-satir`, `#cikti-degismez-ipucu`, `#cikti-degismez-aciklama`, `#cikti-karsi-telefon`, `#cikti-duzenleyen-telefon`, `#dv-bilgi-satir`, `#dv-bilgi`, `#dv-bilgi-etiket`, `#dv-tutar-bos`, `#gd-v-arac-ipucu-satir`, `#gd-v-arac-bilgi`, `#mut-fark-tanimi-satir`, `#mut-fark-bilgi`.

Dokunulan sayfalar (TEK DOKUNUŞ kaydı): Cariler.razor, Cikti.razor, Gider.razor, Raporlar.razor (yalnız gün sonu kartı/sekmesi + iki yardımcı yöntem), Islemler.razor (tek satır, satır sayısı 1399 aynen), Islemler.ParaDemo3.cs, Islemler.Toplu5.cs (yeni), Bilesenler/Yazdirma/A4Belge.razor. Kasalar.razor DEĞİŞMEDİ (yalnız CSS).

## 9. Değişen dosyalar

Yeni: `dotnet/Kuyumcu.Core/I18n/Sozlukler.Toplu5.cs`, `dotnet/Kuyumcu.Web/wwwroot/css/toplu5.css`, `dotnet/Kuyumcu.Web/Pages/Islemler.Toplu5.cs`, `dotnet/Kuyumcu.Tests/Toplu5Testleri.cs`, bu rapor.
Değişen: `dotnet/Kuyumcu.Web/Pages/Cariler.razor`, `Cikti.razor`, `Gider.razor`, `Raporlar.razor`, `Islemler.razor`, `Islemler.ParaDemo3.cs`, `dotnet/Kuyumcu.Web/Bilesenler/Yazdirma/A4Belge.razor`, `dotnet/Kuyumcu.Web/wwwroot/index.html` (tek link satırı), `dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js` (yalnız işletme kimliği), testler `ParaDemo3Testleri.cs`, `ParaDemo2EkranTestleri.cs`, `S32MusterilerEkranTestleri.cs`, `TM1MusterilerTuvalTestleri.cs` (yalnız beklenti).
(`git status --porcelain` bu ağaçta proje dosyalarının tamamını `??` gösteriyor — ilk commit sonrası izlenmiyor; liste bu yüzden elle ve önce-yedekleriyle tutuldu.)

## 10. Koşul karşılama

| Koşul | Durum |
|---|---|
| Madde tablosu önce/sonra/kanıt | § 1–2 |
| Başsız Chrome 1440/1280/960/375 × BASİT+PRO × açık+koyu, Read ile | § 3 (240 + 16 ölçüm) |
| Her değişiklik için test + mutasyon | § 5–6 (35 mutasyon, hepsi kırmızı, cmp AYNI) |
| build 0/0 | § 7 |
| denetim TEMİZ (P-30 hariç) | § 7 |
| Tam süit 0 yeni kırmızı | § 5 (tur 4: 2 kırmızı, ikisi de başka sahipli; fark 0) |
| Para/veri/yasal davranışa dokunmama | Sunucu kodu değişmedi; MASAK kapısı aynen (yalnız BASİT başlık/etiket) |
| Test kiracısı silindi | § 4 |
| COMMIT yok, dosya silme yok | Uyuldu |

## 11. Kurucu / mali müşavir

Mevzuat hükmü eklenmedi/değişmedi; soru yok. Demo işletme kimliği kurgusaldır (vergi no 0000000000); adres doğal ama gerçek bir işletmeye ait değildir — demo öncesi kurucu kendi tercih ettiği kurgusal adresi `demo-tohumla.js` SENARYO.isletme'den değiştirebilir.

---

# TUR 2 (02.10.2026 ~23:30) — önceki doğrulamada kalan bulgular

Kanıtlar: `ham/TOPLU-5-olcum/tur2/` (olcum.jsonl 32 ölçüm, foto/ 32 görüntü, canli-api.txt, canli-sql.txt).
Para / veri / yasal davranış DEĞİŞMEDİ. Sunucu kodu değişmedi (API `belgeNo` hâlâ tam kimliği döndürür). Migration yok.

## T2-1. Madde tablosu

| # | Bulgu | Önce | Sonra | Kanıt |
|---|---|---|---|---|
| KR-1 | [KRİTİK] Satış fişinde "Belge no" olarak 26 haneli ham kayıt kodu (ULID) | Sağ üstte 8pt soluk "01M3Z355KJ1KMDCYYB8A6H51X2", etiket "Belge no" | Etiket **"Kayıt kodu"**, değer kimliğin son 8 hanesi **"D9HA-60YD"** (11pt, kalın, opaklık 1 — okunur boy). Tam numara YALNIZ ekranda `title` ("Tam kayıt numarası: …"), kâğıda basılan metinde yok. Okunur belge numarası taşıyan türler (BelgeNoKayitKimligi=false) "Belge no" etiketiyle AYNEN; ekstre "Ekstre tarihi" AYNEN | `Toplu5FisKoduTestleri` (4 test + 4 Theory koşumu) · M5-36…M5-40 · 32 ölçüm `tur2/olcum.jsonl` (`ham:false` 16/16) · `tur2/foto/fis__BASIT_aydinlik_1440.png`, `fis__PRO_karanlik_375.png` |
| KR-2 | [KRİTİK — KAPSAM DIŞI] BASİT Değişim formu fiş kalıbında değil | — | **YAPILMADI — hasımlı pakete devir.** Değişim'i Verdim/Aldım fiş kalıbına taşımak form yapısını, kimlik kutularını ve ödeme akışını yeniden kurar; "yalnız görünüm, hasımsız" paket sınırını aşar ve MASAK kimlik kapısının bağlandığı alanlara dokunur. Bulgunun kendisi de "Ayrı hasımlı pakete" diyor. | — |

### Neden sıra numarası değil, kısa kod (sapma gerekçesi)
- Hareket tablosunda okunur fiş sıra numarası sütunu YOK (CiktiModulu Q9-4 notu; `hareketler.fis_id` bu kiracıda NULL, BelgeNo = hareket kimliği — `canli-sql.txt`). Sıra numarası üretmek veri modeli/sunucu değişikliğidir (sütun + sayaç + eşzamanlılık) → bu paketin "para/veri/yasal davranışa dokunma" sınırının dışında.
- Bir fişe "2026-000123" biçiminde sıra numarası basmak, VUK kapsamındaki seri-sıra numaralı belge (perakende satış fişi/fatura) sanılmasına yol açabilir; bunun yasal belge numarası olup olmayacağı birincil kaynakla doğrulanmadan yazılamaz (mevzuat kuralı). Bu yüzden numara UYDURULMADI; etiket de "Belge no" yerine "Kayıt kodu" oldu.
- Kısa kod = ULID'in son 8 hanesi (rastgele kısım, 32^8 ≈ 1,1 × 10^12): bir işletmenin fişlerini gözle ayırt etmeye yeter; saf görünüm kuralı `A4Belge.KisaKod`.
- **Hasımlı pakete devir (öneri):** (a) okunur fiş sıra numarası gerekiyorsa sunucuda kiracı başına sayaç + mevzuat teyidi; (b) kısa kodla arama (son 8 hane ile fiş bulma) sunucu araması ister — bugün personel tam numarayı ekranda `title`dan görür.

## T2-2. Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9906, `sikisma-olc.js`)

Yayın kopyası `/private/tmp/kuyumcu-serit/TOPLU-5/yayin-t2` (Release), API 5162, vekil 5163. 2 senaryo × {1440, 1280, 960, 375} × {BASİT, PRO} × {açık, koyu} = **32 ölçüm, hata 0, mod uyuşmazlığı 0**.

| Senaryo | Genişlik | K1 | K3 taşan/kırık | K5 TRY | K8 | K9 | Yatay | #cikti-no | etiket | ham kimlik metinde |
|---|---|---|---|---|---|---|---|---|---|---|
| Satış fişi | 1440/1280/960/375 × BASİT/PRO × açık/koyu (16) | 0 | 0/0 | 0 | 0 | 0 | 0 | "D9HA-60YD" (14,67 px = 11pt, opaklık 1, tek satır 20 px) | "Kayıt kodu" | YOK (16/16) |
| Cari ekstre (gerileme) | 1440/1280/375 (12) | 0 | 0/0 | 0 | 0 | 0 | 0 | "02.10.2026" | "Ekstre tarihi" | — |
| Cari ekstre (gerileme) | 960 (4) | 0 | 0/1 | 0 | 0 | 0 | 0 | "02.10.2026" | "Ekstre tarihi" | — (tur 1'de 3 kırık, kabul gerekçeli) |

Görüntülere Read ile bakıldı: `tur2/foto/fis__BASIT_aydinlik_1440.png` (sağ üst "Kayıt kodu / D9HA-60YD", başlıkla dengeli), `tur2/foto/fis__PRO_karanlik_375.png` (kod tek satır, "SATIŞ FİŞİ" yanında), `tur2/foto/ekstre__PRO_aydinlik_960.png` (ekstre tarihi aynen).

## T2-3. Canlı doğrulama (API 5162, kiracı `PAKETTOPLU-5-TEST2 Kuyumculuk` 01M3Z5GGGX0F392BYT5ZTPHZPC)

- `demo-tohumla.js --prova-kiraci` kod 0 (6 cari, 12 ürün, 9 satış). Sistem anahtarı oturumluk rastgele (dosyada, iş sonunda boşaltıldı); fikstür chmod 600, iş sonunda boşaltıldı.
- `GET /cikti/SATIS_FISI/01M3Z5H27BM0T6FJYKD9HA60YD` **200**, `belgeNo` = "01M3Z5H27BM0T6FJYKD9HA60YD" (sunucu sözleşmesi AYNEN) → ekranda "D9HA-60YD".
- SQL: `hareketler` id 01M3Z5H27BM0T6FJYKD9HA60YD · fis_id NULL · SATIS · 11020.94 (veri değişmedi).
- Kapanış: API (PID 91578) ve vekil `kill`; 5162/5163/9900–9920 dinleyici yok. `zsh kiraci-temizle.sh 01M3Z5GGGX0F392BYT5ZTPHZPC --uygula` kod 0; SQL: tenants bu kimlik 0, hareketler 0, cariler 0; **tenants 16 → 17 → 16** (başlangıç değeri). QA-TEST-* kiracılarına dokunulmadı.

## T2-4. Testler ve mutasyon

- Yeni: `Toplu5FisKoduTestleri` (Toplu5Testleri.cs): `T5_Satis_fisinde_ham_kimlik_BASILMAZ_kisa_kayit_kodu_basilir`, `T5_Okunur_belge_numarasi_aynen_basilir_etiket_Belge_no`, `T5_KisaKod_saf_kural` (4 koşum), `T5_Kisa_kod_okunur_boyda_soluk_degil` → 7 koşum.
- Beklentisi güncellenen (kasıtlı görünüm değişikliği; önce yedekleri `/private/tmp/kuyumcu-serit/TOPLU-5/tur2-once/`): `Q94EkranTestleri.Q94_belge_no_kayit_kimligi_tek_satir_soluk_teknik_aciklama_YOK` (tam kimlik → "XM7H-0EVQ" + title), `Toplu2CiktiTestleri.T2_Satis_fisinde_kayit_kimligi_tek_satir_soluk_sinifla_basilir` (→ "1YJE-PP4P"). `belge-no-kimlik` sınıfı korunur (ortak.css nowrap kuralı ve T2 CSS testi aynen); `belge-no-kisa` toplu5.css'te boyu/opaklığı geçersiz kılar.
- Filtreli (Toplu5 | Q94 | Toplu2 | Cikti): **143/143 yeşil**.

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M5-36 | Kayıt kimliğinde de ham BelgeNo basılır | Q94_belge_no_kayit_kimligi…, T2_Satis_fisinde_kayit_kimligi…, T5_Satis_fisinde_ham_kimlik_BASILMAZ… | AYNI |
| M5-37 | Etiket hep "Belge no" | T5_Satis_fisinde_ham_kimlik_BASILMAZ… | AYNI |
| M5-38 | Kısa kod gruplanmaz ("8A6H51X2") | Q94_…, T2_Satis_…, T5_KisaKod_saf_kural ×2, T5_Satis_… | AYNI |
| M5-39 | KayitKoduMu bayrağa bakmaz (okunur numara da kısaltılır) | T5_Okunur_belge_numarasi_aynen_basilir_etiket_Belge_no | AYNI |
| M5-40 | toplu5.css kısa kod 8pt/opaklık .7 (eski soluk görünüm) | T5_Kisa_kod_okunur_boyda_soluk_degil | AYNI |

Not (md.1): M5-40 ilk koşumu `--no-build` ile M5-39'un derlenmiş ikilisi üzerinde koştu (kaynak geri alınmış ama yeniden derlenmemişti) ve fazladan `T5_Okunur…` kırmızısı verdi; temiz derlemeden sonra M5-40 YENİDEN koşuldu → yalnız `T5_Kisa_kod_okunur_boyda_soluk_degil` kırmızı (tablodaki sonuç bu). Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata**.

## T2-5. Derleme · denetim · tam süit

- `dotnet build Kuyumcu.sln --nologo` (temiz, son kod): 0 Uyarı 0 Hata. (İlk artımlı derlemede 2 uyarı başka dosyalarda: SUBEKAPSAMASinirTestleri.cs CS8601, KesifP2Testleri.cs xUnit2029 — bu paketin değil; temiz derlemede 0.)
- `bash denetim.sh`: 303 ✓, 1 ✗ = **P-30 DEMO_INSAN_SURESI** (kabulde hariç tutulan, kurucu kalemi). Başka ihlal yok.
- **Tam süit tur 2 (son kod, `trx-pTOPLU-5-t2`, `KUYUMCU_TEST_DB=kuyumcu_test_t2`): 9751 test · 9748 geçti · 1 başarısız · 2 atlandı.** Tek kırmızı `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` — bUnit `WaitForFailedException` (zaman aşımı, yük altında); sınıf tek başına 3 kez koşuldu: **6/6, 6/6, 6/6 yeşil** (kararsız). Test İşlemler ekranının ödeme aracını sınar; bu turun tek değişikliği A4Belge numara hücresi + toplu5.css yeni sınıf + sözlük — ilişkisi yok. **Yeni kırmızı farkı 0.** (Tur 1'in iki başka-sahipli kırmızısı — P37c mutbak dosyası ve PATCH-3 kolon tavanı — bu koşumda yeşil.)

## T2-6. DoD sözleşmesi
Yeni varlık / uç / kayıt türü YOK. Çıktı türetilmiş belgedir (DegismezIpucu gerekçesi aynen). Yeni id: `#cikti-no-etiket` (`<önek>-no-etiket`); mevcut `#cikti-no` id'si ve `belge-no-kimlik` sınıfı korundu. Yeni metin `Sozlukler.Toplu5.cs` (TR+EN, konumsal record). Yeni CSS yalnız belirteç (sabit renk testi yeşil).
Dokunulan sayfalar/bileşenler (TEK DOKUNUŞ — gerekçe: aynı paketin tur 2'si, hakem KRİTİK bulgusu): `Bilesenler/Yazdirma/A4Belge.razor` (tur 1'de de bu pakette açıldı).

## T2-7. Değişen dosyalar (tur 2)
`dotnet/Kuyumcu.Web/Bilesenler/Yazdirma/A4Belge.razor`, `dotnet/Kuyumcu.Core/I18n/Sozlukler.Toplu5.cs`, `dotnet/Kuyumcu.Web/wwwroot/css/toplu5.css`, `dotnet/Kuyumcu.Tests/Toplu5Testleri.cs`, `dotnet/Kuyumcu.Tests/Q94CiktiBelgeTestleri.cs` (yalnız beklenti), `dotnet/Kuyumcu.Tests/Toplu2Testleri.cs` (yalnız beklenti), bu rapor. (`git status --porcelain` bu ağaçta izlenmeyen dosyaları `??` gösteriyor; liste elle ve `tur2-once/` yedekleriyle tutuldu.)

## T2-8. Açık kalanlar / devir
- KR-2 BASİT Değişim'i Verdim/Aldım fiş kalıbına taşıma → **hasımlı paket** (veri/yasal davranış değişmeden; MASAK kimlik kapısı alanlarına dokunur).
- Okunur fiş sıra numarası (sunucu sayacı + VUK teyidi) ve kısa kodla fiş arama → **hasımlı/sunucu paketi**.
- Tur 1 § 2'deki devirler aynen geçerli (üst bar başlığı, bölünmüş ödeme ölçümü, Raporlar PRO 1280 seçici, müşteri detayı yüzen pencere, Değişim 30.000 ₺ ödeme aracı, Günü Kapat sayılan nakit).
- Kararsız test: `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` (İşlemler sahibi; yük altında bUnit zaman aşımı).

## T2-9. Kurucu / mali müşavir
- Soru: Müşteriye verilen satış fişinde (yazar kasa/ÖKC fişi ve fatura DIŞINDA kalan bu bilgi belgesinde) okunur bir sıra numarası isteniyor mu, isteniyorsa bunun VUK açısından seri-sıra numaralı belgeyle karıştırılmaması için biçimi ne olmalı? Teyit gelene dek fişte yalnız "Kayıt kodu" (kimliğin son 8 hanesi) basılır.
