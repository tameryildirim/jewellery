# PAKET RAPOR-3 — uygulama raporu (08.10, t1 · API 5160 / vekil 5161 / CDP 9701-9703)

> Taşkın (backend) + Ertuğ (frontend). Kaynak: `ham/RAPOR-3-BULGULAR.md` (RAPOR-2 kalanları). COMMIT YOK. Migration YOK (şema değişmedi).
> Ham kanıt: `ham/RAPOR-3-olcum/` — `canli/*.json`, `canli-olc.txt`, `sql-bagimsiz.txt`, `mut-sonuc.txt`, `hedef-olcum.jsonl` + `hedef-son.json` (66 ölçüm), `foto/` (66 görüntü), `denetim.log`.

## TUR 2 (08.10 02:00–02:45) — doğrulayıcı bulgularının kapatılması

Kanıt: `ham/RAPOR-3-olcum/` → `canli-t2/` (aylik.json, gunsonu.json, canli-ozet.json, sql-bagimsiz.txt), `t2-olcum.jsonl` + `t2-olcum-375.jsonl` (30+4 ölçüm), `foto-t2/` (34 görüntü), `denetim-t2.log`; mutasyon `/private/tmp/kuyumcu-serit/RAPOR-3/mut2-sonuc.txt` (betik `araclar/mut2.py`, yedek `mut-yedek/RAPOR-3/mut2/`).

### T2.1 Bulgu → çözüm
| Bulgu | Çözüm | Kanıt |
|---|---|---|
| [KRİTİK] Alan grafiği eksen yazıları noktanın altında değil (space-between; 8 noktada son yazı 07.10, bugünün tepesi dünün tarihiyle) | `AlanGrafik.razor`: eksen artık (n−1) EŞİT HÜCRELİ ızgara (`grid-auto-flow: column; grid-auto-columns: minmax(0,1fr)` — satır içi style YOK, sayaç 55/196 değişmedi). i. yazı i. hücrenin SOL kenarına `translateX(-50%)` ile ortalanır = tam X(i)=i/(n−1)·G. İlk yazı sola, SON NOKTA HER ZAMAN yazılır ve sağa yaslıdır; sona yarım adımdan yakın ara yazı düşer (çakışma yok). Her yazı `data-indeks` + `data-konum` (= X(i)/G·100) taşır | bUnit R3_31 (8 nokta: son "08.10", konum 100, her yazı kendi hücresinde, konum = i/(n−1)·100), R3_32 (31 nokta, son "31.10"). Tarayıcı: Panel PRO 1440/1280/375 × açık/koyu — 7 nokta, yazılar 02.10·04.10·06.10·08.10, **ölçülen yazı x − nokta x = 0 px** (6/6), taşma 0; aynı CSS ile sentetik 31 nokta ekseni (01·08·15·22·31.10) sapma **0 px**, taşma 0. Görüntü: `foto-t2/panel__PRO_aydinlik_1440.png` (08.10 tepenin altında), `panel__PRO_karanlik_375.png` |
| [ORTA] Gün sonu USD girişini TL nakit sayıyor (aylık ≠ gün sonu dövizli kasada) | `RaporModulu.PosFaturaMutabakati`: kasa girişi kırılımı para birimine göre gruplanır; TL tahsilatı, virman ve açılış devri YALNIZ `ParaBirimi=="TRY"` (aylıkla AYNI süzgeç). Dövizli girişler (virman/açılış dışı) `tahsilat.dovizGiris[{paraBirimi,tutar}]` alanında AYRI döner, TL'ye çevrilmez/toplanmaz. İstemci: `MutabakatTahsilat.DovizGiris` (isteğe bağlı); PRO Gün sonu "USD kasasına giren (TL tahsilata girmez) 100,00 $" (`#mut-doviz-giris-USD`), BASİT kırılım "… · ayrıca 100,00 $ döviz girişi". Sözlük `KR.DovizKasaGirisi`, `KR.DovizGirisKisa` TR+EN | HTTP R3_03 (TL 500 + USD 100 → gün sonu toplam 500 = nakit 500 = aylık 500; dovizGiris tek satır USD 100). Canlı aşağıda. bUnit R3_33 (BASİT), R3_34 (PRO) |
| [DÜŞÜK] "Ayın tüm altın hareketi (sayım, emanet dâhil)" açılış stoğunu söylemiyor | Etiket "Ayın tüm altın hareketi (açılış, sayım, emanet dâhil)" / EN "(incl. opening stock, counts, custody)" | R3_10; tarayıcı PRO Özet 1440/1280/375 |
| [DÜŞÜK] Panel "Son 7 günlük satış" ayın tüm günlerini çiziyor | Panel grafiği `TakeLast(7)`; ayın ilk 6 gününde (aylık yanıt <7 gün) önceki ayın `/raporlar/aylik` yanıtı da istenir ve birleştirilir (hata olursa mevcut hata bandı) | R3_28 (8 günlük ay → 7 nokta, yazılar 02·04·06·08.10), R3_29 (3 günlük ay + Eylül → ilk 27.09, son 03.10). Canlı 7 nokta |

### T2.2 Sapmalar
- `Kuyumcu.Web/Servisler/UyumServisi.cs` dosya alanı dışında: yalnız `MutabakatTahsilat` kaydına isteğe bağlı `DovizGiris` alanı + `MutabakatDovizGiris` kaydı eklendi (sözleşmenin istemci yüzü bu dosyada; varsayılan null → eski yanıtlar aynı davranır).
- `AlanGrafik.razor` dokunulan bileşen (tur 1'de de); Panel.razor/Raporlar.razor aynı paketin ikinci turu (gerekçe: doğrulayıcı bulgusu).
- 31 noktalı eksen canlıda Panel'de artık ÇIKMAZ (grafik 7 güne kırpıldı); 31 nokta konumu bUnit R3_32 + tarayıcıda aynı CSS ile kurulmuş sentetik eksenle ölçüldü (gerçek veriyle değil — açıkça belirtilir).
- Yan etki notu: dövizli açılış devri de `dovizGiris`'ten düşülür (yalnız müşteriden giren döviz kalır).

### T2.3 Testler
- Yeni/değişen: R3_03 (HTTP), R3_28 (yeniden yazıldı), R3_29, R3_31, R3_32, R3_33, R3_34 (yeni), R3_27 (önceki ay boş yanıtı), R3_10 (+etiket). `RaporUc*` filtreli: **24/24 geçti**. Geniş küme (Panel|Rapor|GunSonu|ParaDemo|Grafik|Mutabakat|P69|UX1C3|S31|S34|Toplu|KarZarar|Degisim): 1369 test, 1367 geçti, 1 atlandı, 1 kırmızı = `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula…` (tur 1'de de kırmızıydı; gider pusulası yolu, bu paketin değil).
- **Tam süit (tur başına bir kez, son kodla):** `/private/tmp/kuyumcu-serit/trx-pRAPOR-3-t2/_Mac_2026-10-08_02_33_14.trx` — toplam **10554**, çalışan 10552, geçen **10551**, başarısız **1** (yukarıdaki Yasal4, tur 1 açık kalanlar listesinde zaten var). **Bu paketin yeni kırmızısı: 0.** (Tur 1'deki `YASAL1SinirHttpTestleri.Sistem_ucu_EKSI_had_400` bu koşumda geçti.)
- Build: son mutasyondan sonra `touch` + `dotnet build Kuyumcu.sln --no-incremental` → **0 hata**, 4 uyarı (hepsi başka şeritlerin test dosyası: SUBEKAPSAMASinir:49, KesifP2:455, SonKalanlar:584/813; paketin uyarısı 0).

### T2.4 Mutasyonlar (TEK eşleşme, geri kopya `shutil.copyfile` = yeni mtime → bayat artımlı derleme tuzağı kapalı)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M21 | gün sonu TL tahsilatı dövizli girişi de sayar | R3_03 (elle tekrar: Expected 500, Actual 600,00) | AYNI |
| M22 | alan grafiği son nokta yazılmaz | R3_31, R3_32, R3_28 | AYNI |
| M23 | eksen n hücre (eşit aralık) — yazı noktasından kayar | R3_31, R3_32 | AYNI |
| M24 | Panel grafiği ayın tüm günlerini çizer | R3_28 | AYNI |
| M25 | ay başında önceki ay istenmez | R3_29 | AYNI |
| M26 | ayın altın hareketi etiketi açılışı söylemez | R3_10 | AYNI |
| M27 | BASİT gün sonu döviz girişini yazmaz | R3_33 | AYNI |
| M28 | PRO gün sonu döviz satırı yok | R3_34 | AYNI |
⚠ İlk koşumda M26/M27 başka bir şeridin o an yarım düzenlediği `Satinalma/SatinalmaModulu.cs(536)` CS1002 derleme hatasına denk geldi (M26 "YOK", M27 bayat ikiliyle "geçti"); dosya 02:14'te düzeldi, build yeşil doğrulandı, M26/M27/M21 yeniden koşuldu → yukarıdaki sonuçlar. 8/8 kırmızı, 8/8 cmp AYNI.

### T2.5 Canlı (5160, `dotnet run --no-build`, Sistem anahtarı ortam değişkeninden — değer basılmadı; kiracı PAKETRAPOR-3-TEST t2 `01M4CB1AJCG4JTNJ5DPNHKGR2C`)
Kurulum: davet 201, kayıt 201, giriş 200, kasa 200 ×3 (Ana TRY, Vitrin TRY, Dolar USD), ürün 201, satış 200 ×2 (9.000 + 8.500), kasa girişi 200 ×2 (TL 500, USD 100), virman 200 (300), açılış prova/uygula 200/200 (150.000).
| Uç | HTTP | API | Bağımsız SQL |
|---|---|---|---|
| `/raporlar/aylik?yil=2026&ay=10` tahsilat | 200 | toplamTl **18.000**, virmanGiris 300, acilisDevri 150.000; gunler 8, bugün satisTl=netSatisTl 17.500 | TRY, virman/açılış dışı: 17.500 + 500 = **18.000** |
| `/raporlar/pos-fatura-mutabakati?tarih=2026-10-08` | 200 | nakit **18.000**, toplam **18.000**, virman 300, açılış 150.000, **dovizGiris [USD 100]** | USD virman/açılış dışı **100**; virman TRY 300; ACILIS TRY 150.000 |
Eski kodla gün sonu toplamı 18.100 ("Nakit 18.100") olurdu; artık iki uç aynı sabit değer.

### T2.6 Gerçek tarayıcı (başsız Chrome, CDP 9701/9702, vekil 5161; `araclar/olc2.mjs`, `olc3.mjs`)
Rotalar `/` ve `/raporlar` (+ PRO Gün sonu sekmesi) × 1440/1280/375 × BASİT/PRO × açık/koyu = **34 ölçüm** (375 PRO Rapor merkezi ilk turda yüklenme bitmeden ölçülmüştü → yüklenme bekleyen `olc3.mjs` ile yeniden ölçüldü).
| Ölçüm | Sonuç |
|---|---|
| K1 · K3 taşan · K3 kırık · K5 TRY · K9 · K12 · K13 · yatay kaydırma | 0 (34/34) |
| Panel PRO eksen: nokta / yazılar / yazı−nokta sapması / taşma | 7 / 02.10·04.10·06.10·08.10 / **0 px** / 0 (6/6); sentetik 31: sapma 0 px, taşma 0 |
| Rapor merkezi BASİT gün sonu kırılımı | "Tahsilat 18.000,00 ₺: Nakit 18.000,00 ₺ · ayrıca 100,00 $ döviz girişi" (6/6) |
| Rapor merkezi PRO Özet | "AYIN TÜM ALTIN HAREKETİ (AÇILIŞ, SAYIM, EMANET DÂHİL)", TAHSİLAT 18.000,00 ₺ (6/6) |
| Rapor merkezi PRO Gün sonu | TOPLAM TAHSİLAT 18.000,00 ₺ · Nakit 18.000,00 ₺; "USD kasasına giren (TL tahsilata girmez) 100,00 $"; açılış 150.000,00 ₺; virman 300,00 ₺ (6/6) |
Read ile GÖRÜLEN: `foto-t2/panel__PRO_aydinlik_1440.png` (08.10 yazısı tepenin tam altında, 04.10/06.10 1/3–2/3'te), `panel__PRO_karanlik_375.png` (kırpım), `raporlar__BASIT_aydinlik_1280.png`, `raporlar__PRO_karanlik_375.png` (ilk: yükleniyor → yeniden ölçüldü), `raporlar__PRO_karanlik_375__gunsonu.png`.

### T2.7 Temizlik / denetim
- API (PID), vekil (PID), Chrome profilleri durduruldu; 5160/5161/9701/9702 dinleyen 0. `zsh kiraci-temizle.sh 01M4CB1AJCG4JTNJ5DPNHKGR2C --uygula` → ikinci çağrı "Kiracı YOK"; kasa_hareketleri bu kimlikle 0. tenants: tur başı **17** → son **16** (benim kiracım eklendi ve silindi; aradaki −1 başka şeridin kendi kiracısını silmesi — dokunmadım). QA-TEST-* dokunulmadı.
- `bash denetim.sh` (son kod, `denetim-t2.log`): **306 ✓**, tek ✗ P-30 DEMO_INSAN_SURESI (muaf) → TEMİZ (P-30 hariç). Satır içi style sayacı 55 (tavan 196) — artış yok.

### T2.8 Değişen dosyalar (tur 2)
`Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` (gün sonu TRY süzgeci + dovizGiris) · `Kuyumcu.Web/Bilesenler/AlanGrafik.razor` · `Kuyumcu.Web/wwwroot/css/rapor-gorunum.css` (grafik eksen ızgarası) · `Kuyumcu.Web/Pages/Panel.razor` (son 7 gün) · `Kuyumcu.Web/Pages/Raporlar.razor` (döviz satırı/parçası) · `Kuyumcu.Web/Servisler/UyumServisi.cs` (DovizGiris) · `Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs` (+2 anahtar TR+EN, etiket) · `Kuyumcu.Tests/RaporUcTestleri.cs` · yorum: `P69HasAdlandirmaTestleri.cs`, `UX1C3EkranTestleri.cs`. Migration YOK.

### T2.9 Açık kalanlar (tur 2 sonrası)
- `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula…` → YASAL-4 sahibi (tur 1'den beri, bu paketin kod yolu değil).
- Gün sonu BASİT/PRO'da dövizli girişler para birimi başına tek satır; döviz "kasa ayrımı" satırı (PARA-DEMO-2) ayrıca dövizi gösteriyorsa birleştirme → TOPLU (düşük).
- Tur 1 §11'deki diğer kalemler aynen (Panel grafiği maddesi bu turda KAPANDI).

---

## TUR 1

## 1. Yapılanlar (bulgu → çözüm)

| # | Bulgu | Çözüm | Kanıt |
|---|---|---|---|
| V1 | [ORTA] Rapor merkezi Özet "TAHSİLAT" açılış devrini (ve virmanı) sayıyor, Gün sonu saymıyor (150.000 ₺ fark) | `RaporModulu.AylikOzet`: aylık tahsilat Gün sonu (PARA-DEMO-2 md.5) ile AYNI tanım — `KarsiBacakId` dolu (virman) ve `Tur="ACILIS"` hareketine bağlı kasa bacağı (açılış devri) toplama girmez; ikisi `tahsilat.virmanGiris` / `tahsilat.acilisDevri` olarak ayrı döner. Ekranda kart ⓘ tanımı (`KR.TahsilatKapsamIpucu`) | Canlı: aylık 88.250,50 = gün sonu 88.250,50 = SQL 88.250,50 (eski formülle 238.550,50 olurdu); R3_01; M1, M2 |
| V2 | [DÜŞÜK] Panel "Son 7 günlük satış" grafiği brüt, kart net | `/raporlar/aylik` `gunler[].netSatisTl` (= satış − |iade|, storno çifti dışlı); Panel grafiği bunu çizer (alan yoksa brüt) | Canlı bugün satisTl 86.750,50 / netSatisTl 75.250,50 = SQL; R3_02, R3_27; M3, M12 |
| E1 | [YÜKSEK ×2] Bekleyen iş kartında iş varken boş-durum simgesi + "Bekleyen 1 iş üstteki uyarıda"; Rapor merkezinde bant kartın içinde + boş simge | Yeni ORTAK bileşen `Bilesenler/BekleyenUyumSatirlari.razor`: her yasal/nakit işi tuvaldeki gibi renkli noktalı SATIR ("● Yasal liste yüklenmedi — Uyum", doğrudan `/uyum?sekme=risk|nakit|saklama`). Panel BASİT + PRO kutucuğu + Rapor merkezi aynı bileşen; boş durum yalnız hiç iş yokken; "üstteki" yönlendirmesi kalktı. Veri yeni istek açmadan `UyumYasalBantlari.OzetDegisti` / `NakitSiniriPanelSatiri` sayısından; Rapor merkezinde bant kutuları `Gorunmez` (hata bandı yine çizilir) | Tarayıcı 66 ölçüm: boş durum 0, "üstteki" 0, bant kartta 0; R3_20, R3_21, R2_20, R2_32, YASAL2_Tur3 ×2; M4, M5, M6, M20 |
| E2 | [ORTA] Panel BASİT kâr satırı "eksik: 12 satış, 1 iade" kapsamı söylemiyor | `KR.PanelKarKapsam` "Kâr {tutar} (yalnız {N} satış) · {12 satış ve 1 iadenin maliyeti yok}" — N = kâr-zarar `satisAdedi` − maliyeti eksik satış; eksik öbeği U+00A0 ile bölünmez; tutar–₺ arası zaten U+00A0 | R3_22 (birebir metin), R2_27, KR41, MI30, RG31; M7 |
| E3 | [ORTA ×2] PRO Panel "NET HAS GRAM · Paralel defter" (günün tüm has hareketi; sayım/emanet dâhil) BASİT ile çelişiyor; Rapor merkezi aynı kavrama başka ad | Panel PRO kart → "Bugün satılan altın (iade düşülmüş)" = BASİT "Bugün ne sattım" has sayısının AYNISI (`netSatis.altinToplamHas`); "Paralel defter" kalktı. Rapor merkezi PRO aylık kart etiketi kapsamı söyler: "Ayın tüm altın hareketi (sayım, emanet dâhil)" | Görüntü panel PRO 18,3200 has = BASİT 18,3200 has; R3_23, R3_30, P69_03, C3_41; M8, M17 |
| E4a | [DÜŞÜK] Panel PRO grafik eksen yazıları ezik | `AlanGrafik.razor`: eksen yazıları SVG DIŞINDA HTML (preserveAspectRatio="none" yazıyı germez), en çok 5 yazı | Ölçüm SVG içi yazı 0; 1440 görüntüde "01.10 03.10 05.10 07.10" okunur; R3_27, R3_28; M18, M19 |
| E4b | [DÜŞÜK] Kur kartında "Kur (24K Altın)" iki kez | Gövdedeki tekrar kaldırıldı | Ölçüm başlık sayısı 1; R3_23 |
| E4c | [DÜŞÜK] Panel PRO Kasa Özeti "(bugün itibarıyla — dönemden bağımsız)" | Panel'de dönem seçici yok → not `gorsel-gizli` (ekran okuyucuda, id aynı) | Görüntü panel PRO |
| E5 | [DÜŞÜK] Rapor merkezi stopaj tek try içinde | Stopaj AYRI denenir; hatası `#bugun-bekleyen-stopaj-hata`, öbür sayaçlar sayılmaya devam | R3_24; M9 |
| E6 | [DÜŞÜK] Gün sonu "Fark var" rozeti farkı söylemiyor; faturasız satış brüt, "Bugün ne sattım" net | Rozet kalktı (bilgi satırda + eylem listesinde); iade varsa "(iade düşülmeden)" kapsam notu | Görüntü "Faturası kesilmemiş satış: 86.750,50 ₺ (iade düşülmeden)"; R3_26, S34_10; M11 |
| E7 | [DÜŞÜK] Rapor merkezi PRO #rp-yil 80 / #rp-ay 76 px ezik; düğme "Kâr-Zarar" | `#rp-yil-sar` en az 7.5rem, `#rp-ay-sar` 9.5rem; düğme `KR.KarZararDugme` "Kâr-zarar" | Ölçüm 1280/1440: 120 / 152 px, K12 0; R3_11, R3_30; M16 |
| E8 | [DÜŞÜK, K17] Stok yaşı ORTALAMA YAŞ / TAHMİNİ SATIŞ SÜRESİ kart altı cümleleri; Kâr-zarar bandının 3. satırı; Ürünler her satırda "Altın maliyeti bilinmiyor" | Cümleler "Devir hızı nasıl hesaplanır? ⓘ" içinde (`#sy-ortalama-yas-not`, `#sy-tahmini-sure-not`); bant 3. satırı "Nasıl hesaplanır? ⓘ" (`#kz-maliyet-eksik-nasil`, id aynı); Ürünler hücresinde yalnız "—", neden TEK ⓘ (`#kz-urun-not-ipucu`), satır notu `gorsel-gizli` | Ölçüm: stok yaşı kart altı [] + ipucunda; ürünlerde görünür not 0; R3_30, R3_42, KR62 |
| E9 | [DÜŞÜK] Para biçimi aynı sütunda kuruşlu/kuruşsuz | Panel, Rapor merkezi (+ Raporlar.GunSonu.cs), Kâr-zarar'da TL tutarları her zaman iki ondalık: `Bicim.Tl` → `Bicim.TlKurus`, `Bicim.Para` → `Bicim.BelgePara` (TRY iki hane, döviz aynen) | Görüntü Ürünler "26.250,50 ₺ / 49.000,00 ₺"; K5 (TRY) 0; R3_25, R3_42; M10 |
| E10 | [DÜŞÜK] Kâr-zarar grafik: maliyet yokken tek Hasılat çubuğu açıklamasız | `#kz-grafik-maliyet-yok` "Maliyet ve kâr: maliyet girilmediği için hesaplanamadı" | Görüntü kz PRO 1280 Grafikler; R3_42; M15 |
| E11 | [DÜŞÜK] Kâr-zarar boş dönem "0 ₺"; ters aralıkta hata iki kez + eski kartlar | Satış/iade/takas/işçilik yoksa Özet ve Grafikler'de "Bu dönemde satış yok" boş durumu; ters aralıkta hata YALNIZ alan altında (`KR.AralikHatasi` TR+EN), kartlar gizli | R3_40 (×2), R3_41, C3_50; M13, M14 |
| E12 | [DÜŞÜK] Akordeon son kart sırasına yapışık | `#kz-nereden, #kz-duzeltme { margin-top: var(--bosluk-4) }` | Ölçüm akordeon üst boşluğu 16 px; R3_11 |
| — | [DÜŞÜK] AltinPozisyonuKarti dokunulan listesinde yoktu | Bilgi: bu pakette dokunulmadı; kapı kaydı orkestratörde | — |
| — | [DÜŞÜK] R2_11 metin eşlemesi / kalıcı tarayıcı gerilemesi | Bu pakette hedefli tarayıcı ölçümü tekrarlandı (66 ölçüm); kalıcı kapıya ekleme orkestratör/TOPLU | §6 |

## 2. Sapmalar ve gerekçeler
- **Dosya alanı dışı (zorunlu, eklemeli):** `Bilesenler/BekleyenUyumSatirlari.razor` (YENİ — "iki ekran aynı bileşen" koşulu), `Bilesenler/Uyum/UyumYasalBantlari.razor` (+`OzetDegisti`, +`Gorunmez`; varsayılan davranış aynı), `Bilesenler/NakitSiniriPanelSatiri.razor` (+`Gorunmez`), `Bilesenler/AlanGrafik.razor` (eksen yazıları), `Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs` (`AylikGun.NetSatisTl`, `PanelKarOzeti.SatisAdedi` — isteğe bağlı, sonda). `Pages/Raporlar.GunSonu.cs` Rapor merkezinin parçası (para biçimi).
- **UX-S33:** Rapor merkezi Stok yaşı başlığındaki "(bugün itibarıyla — dönemden bağımsız)" notu KALDI — sayfada yıl/ay seçicisi var ve UX-S33 bağlayıcı (RAPOR-2 kararıyla aynı). Panel'de dönem seçici olmadığından Kasa Özeti notu ekran okuyucuya alındı.
- **Mühürlü test beklentileri güncellendi (gerekçe = bulgu metni):** para biçimi (iki ondalık) — DegisimGunSonu, DegisimIki, KarRapor, KarZararEkran, OrtakGorunumKiyas, ParaDemo2Ekran, ParaDemoEkran, RaporGorunum, RaporlarEkran, S31, S34, Toplu5, ZoBelge, G5SatisKirilimiEkran, RaporYaslandirma, Toplu3; kâr kapsam metni — KR41, MI30, RG31, R2_27; bekleyen satırı — R2_20, R2_32, YASAL2_Tur3 ×2, T6_Panel_bekleyen (kaynak sayımı: yönlendirme cümlesi 0, ortak bileşen 3); etiket — P69_03, C3_41; rozet — S34_10; KR62 ("—" + gizli "Bilinmiyor"); C3_50 sahte özeti 10 satış (boş dönem artık boş durum çizer).
- **Canlı veri:** kâr KISMI durumu canlıda üretilemedi (maliyetli alıştan sonraki satış açılış partisinden düştü → HESAPLANAMADI); KISMI kâr satırı bUnit ile birebir metinle kanıtlı (R3_22). İlk satış denemeleri 40.000 ₺ NAKIT → 400 NAKIT_SINIRI (beklenen fail-closed), ilk virman 400 KASA_NAKIT_YETERSIZ (beklenen); tutarlar ayarlandı.
- **Mutasyon betiği** perl yerine Python (TEK eşleşme şartı, geri kopya + `filecmp shallow=False`). ⚠ Tuzak bulundu ve giderildi: `shutil.copy2` eski mtime'ı geri koyduğu için artımlı derleme mutasyonlu çıktıyı bayat bıraktı (canlıda `bekleyen-yasal-liste` sınıfı yoktu) → değişen dosyalar `touch` + `dotnet build --no-incremental`; canlı okuma, tarayıcı ölçümü ve testler bu temiz derlemeden SONRA tekrarlandı.

## 3. DoD sözleşmesi
Yeni varlık/kayıt türü YOK, yeni uç YOK (mevcut `GET /raporlar/aylik` yanıtına salt okunur türetilmiş alanlar: `tahsilat.virmanGiris`, `tahsilat.acilisDevri`, `gunler[].netSatisTl`), yeni yazma düğmesi YOK; yeni bağlantılar (bekleyen satırları) gerçek rotaya gider. [RolGerekli] değişmedi; uyum/yasal veri yine yalnız yetkili rolde istenir (yeni istek açılmadı). Rapor rakamları türetilmiştir, düzenlenmez — mevcut "Rapordaki bir rakam yanlışsa" DegismezIpucu aynen. Sözlük: `Sozlukler.KarRapor.cs` +19 anahtar TR+EN (hub'a dokunulmadı).

## 4. Testler
- Yeni `Kuyumcu.Tests/RaporUcTestleri.cs` — **18 vaka**: HTTP R3_01; DB R3_02; saf R3_10, R3_11; bUnit R3_20–R3_28, R3_30, R3_40 (×2), R3_41, R3_42. Filtreli 18/18.
- İlgili geniş küme (40+ sınıf): ilk koşum 52 kırmızı (beklenti güncellemesi gerektirenler) → **1767/1773**; kalan 6'nın 4'ü TevsikIhlalDb/Yasal4Http (bu paketin kodu değil, §11), 2'si sonra düzeltildi (KR62, S34_10).
- Build: `dotnet build Kuyumcu.sln` **0 hata**; uyarı 4 — hepsi başka şeritlerin test dosyası (`SUBEKAPSAMASinirTestleri.cs:49`, `KesifP2Testleri.cs:455`, `SonKalanlarTestleri.cs:584,813`); bu paketin uyarısı 0.
- **Tam süit (bir kez, `/private/tmp/kuyumcu-serit/trx-pRAPOR-3/_Mac_2026-10-08_01_25_13.trx`, KUYUMCU_TEST_DB=kuyumcu_test_t1):** toplam **10503** · geçti **10489** · başarısız **12** · çalışmayan 2.
  - 5'i BU PAKETİN para biçimi/boş dönem değişikliğinin beklentisiydi (G5_Ekran_kasaya_giren_karti…, RaporYaslandirma Acilis_satiri…, Tarih_yok_satiri…, Toplu3 T3_Sifir_hucre…, UX1C3 C3_50) → beklentiler güncellendi; hedefli yeniden koşumda (102 test) **geçti**.
  - 7'si başka şerit / kararsız: YasalParametreHttp ×5 (hedefli yeniden koşumda **geçti**), `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula…` ve `Dogrulayici.YASAL1SinirHttpTestleri.Sistem_ucu_EKSI_had_400…` (yeniden koşumda da kırmızı). İkisi bu paketin değiştirmediği kod yolunda (gider pusulası süre takibi; yasal parametre sistem ucu); bu paketin RaporModulu değişikliği yalnız `AylikOzet` içindedir. Koşum saati 00:20–01:40 TSİ (UTC'de önceki gün) — gün sınırı etkisi OLASI, DOĞRULANMADI.
  - **Bu paketin yeni kırmızısı (düzeltme sonrası): 0** (hedefli koşumla doğrulandı; "tam süit tur başına bir kez" kuralı gereği ikinci kez koşulmadı).

## 5. Mutasyon tablosu (`/private/tmp/kuyumcu-serit/RAPOR-3/araclar/mut.py`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/RAPOR-3/mut/`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M1 | aylık tahsilattan açılış devri düşülmez | R3_01 | AYNI |
| M2 | aylık tahsilata virman girer | R3_01 | AYNI |
| M3 | günlük netSatisTl iadeyi düşmez | R3_02 | AYNI |
| M4 | Panel BASİT kartta yasal satır yok | R2_20, R3_20 | AYNI |
| M5 | Rapor merkezi bandı kartta yine çizer | R3_20 | AYNI |
| M6 | yasal iş varken boş durum | R3_20, YASAL2_Tur3 ×2 | AYNI |
| M7 | kâr kapsamı eksik satışı düşmez | R2_27, R3_22 | AYNI |
| M8 | PRO kart günün tüm has hareketi | R3_23 | AYNI |
| M9 | stopaj hatası öbür sayaçları düşürür | R3_24 | AYNI |
| M10 | Panel satış tutarı kuruşsuz | R3_25 | AYNI |
| M11 | gün sonu iade kapsam notu yok | R3_26 | AYNI |
| M12 | Panel grafiği brüt seri | R3_27 | AYNI |
| M13 | boş dönem kartları çizilir | R3_40 | AYNI |
| M14 | ters aralık hatası bantta ikinci kez | R3_41 | AYNI |
| M15 | maliyetsiz grafik etiketi yok | R3_42 | AYNI |
| M16 | ay seçici en az genişliği 5rem | R3_11 | AYNI |
| M17 | ayın has etiketi eski ad | P69_03, R3_30, C3_41 | AYNI |
| M18 | alan grafiği eksen yazısı yok | R3_27 | AYNI |
| M19 | eksen yazı adımı 8 (sığmaz) | R3_28 | AYNI |
| M20 | ortak satır sınıfı yok | R3_20 (ilk koşumda TEST AÇIĞI → test güçlendirildi → kırmızı) | AYNI |

20/20 kırmızı, 20/20 cmp AYNI; ardından temiz `--no-incremental` build 0 hata.

## 6. Gerçek tarayıcı (başsız Chrome, `araclar/olc.mjs` + `betikler/tur/sikisma.js` + `ekstra.js`, önbellek kapalı)
Rotalar `/`, `/raporlar`, `/raporlar/kar-zarar` (+ Stok yaşı, Grafikler, Ürünler sekmeleri) × 1440/1280/375 × BASİT/PRO × açık/koyu = **66 ölçüm** (`hedef-son.json`).

| Ölçüm | Sonuç |
|---|---|
| K1 gizli-görünen · K3 taşan · K5 TRY · K9 binen · K12 ezik · K13 kırpılan · yatay kaydırma | 0 (66/66) |
| K3 kırık kısa etiket | 0 — **istisna 2:** `/raporlar` BASİT 1280 açık+koyu: test verisindeki "RAPOR3 ikinci kasa" adı "Kasada ne var" kartında iki satıra sarar (çok kelimeli ad; tutar bölünmez). Bekleyen satırı ilk ölçümde de kırılıyordu → ortak satır kuralı + yazı boyu düzeltildi, son ölçümde 0 |
| Bekleyen iş: boş durum · "üstteki" · bant kartta | 0 · 0 · 0 (iki ekran, 4 mod/tema, 3 genişlik); satır "● Yasal liste yüklenmedi — Uyum" |
| Panel PRO: "Paralel defter/NET HAS GRAM" · kur başlığı · SVG içi yazı | yok · 1 · 0 |
| Rapor merkezi PRO #rp-yil / #rp-ay | 1440/1280: 120 / 152 px; 375: 351 / 351 px; düğme "Kâr-zarar" |
| Kâr-zarar akordeon üst boşluğu | 16 px |

Read ile GÖRÜLEN: `raporlar__BASIT_aydinlik_1280.png` (ilk: satır bozuk → düzeltildi), `raporlar__BASIT_karanlik_1280.png`, `panel__BASIT_aydinlik_1280.png`, `panel__PRO_aydinlik_1440.png` (eksen kırpık → 5 yazıya indirildi), `panel__PRO_karanlik_1440.png`, `kz__PRO_aydinlik_1280__Grafikler.png`, `kz__BASIT_karanlik_375__Ürünler.png`, `raporlar__PRO_aydinlik_1280.png`.

## 7. Canlı doğrulama (5160, Development, `dotnet run --no-build`; kiracı PAKETRAPOR-3-TEST `01M4C4ZXETRTFHE48B0PSP19MN`)
Kurulum: davet 201, kayıt 201, giriş 200, kasa 200 ×2, ürün 201 ×2, satış 200 ×5, iade 200, kasa girişi 200 ×2, virman 200, açılış prova/uygula 200/200, alış 200.

| Uç | HTTP | API | Bağımsız SQL |
|---|---|---|---|
| `/raporlar/aylik` tahsilat | 200 | toplamTl **88.250,50**, virmanGiris 300, acilisDevri 150.000 | kasa girişi toplam 238.550,50; virman 300; açılış 150.000; tanım (virman/açılış dışı) **88.250,50** |
| `/raporlar/pos-fatura-mutabakati` | 200 | tahsilat.toplam **88.250,50** (nakit 87.250,50 + kart 1.000), virman 300, açılış 150.000 | aynı |
| `/raporlar/aylik` gunler[bugün] | 200 | satisTl 86.750,50 · **netSatisTl 75.250,50** | SATIS 86.750,50 − |SATIS_IADE| 11.500 = 75.250,50 |
| `/raporlar/gunluk` netSatis | 200 | toplamTl 75.250,50 · altinToplamHas 18,32 | — |
| `/raporlar/kar-zarar` (bugün) | 200 | hasilatTl 75.250,50, karDurumu HESAPLANAMADI | — |
| `/masak/uyum-ozet` | 200 | listeEski true → bekleyen satırı 1 | — |

Temizlik: API, vekil, başsız Chrome PID ile durduruldu; 5160/5161/9701-9703 dinleyen 0. `zsh kiraci-temizle.sh 01M4C4ZXETRTFHE48B0PSP19MN --uygula` → ikinci çağrı "Kiracı YOK"; hareketler/kasa_hareketleri bu kimlik için 0. tenants: başlangıç **17** → son **17** (benimki silindi; bu arada başka şeritler kendi kiracılarını ekleyip sildi — dokunmadım). QA-TEST-* dokunulmadı. Gizli dosyalar (`/private/tmp/kuyumcu-serit/RAPOR-3/gizli/`, 600) içerik basılmadı.

## 8. Denetim
`bash denetim.sh` (son kod, `ham/RAPOR-3-olcum/denetim.log`): **306 ✓**, tek ✗ `P-30: DEMO_INSAN_SURESI` (muaf) → **TEMİZ (P-30 hariç)**.

## 9. Değişen dosyalar (bu paket)
```
 M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs            (AylikOzet: tahsilat tanımı + gunler netSatisTl)
?? dotnet/Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs     (AylikGun.NetSatisTl, PanelKarOzeti.SatisAdedi)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs             (+19 anahtar TR+EN, MaliyetYok)
 M dotnet/Kuyumcu.Web/Pages/Panel.razor
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor
?? dotnet/Kuyumcu.Web/Pages/Raporlar.GunSonu.cs               (para biçimi)
?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor
?? dotnet/Kuyumcu.Web/Bilesenler/BekleyenUyumSatirlari.razor  (YENİ ortak bileşen)
?? dotnet/Kuyumcu.Web/Bilesenler/Uyum/UyumYasalBantlari.razor (+OzetDegisti, +Gorunmez)
?? dotnet/Kuyumcu.Web/Bilesenler/NakitSiniriPanelSatiri.razor (+Gorunmez)
 M dotnet/Kuyumcu.Web/Bilesenler/AlanGrafik.razor
?? dotnet/Kuyumcu.Web/wwwroot/css/rapor-gorunum.css           (+(13)–(17), sabit renk yok)
?? dotnet/Kuyumcu.Tests/RaporUcTestleri.cs                    (YENİ, 18 vaka)
   + beklenti güncellemeleri (§2): RaporIkiTestleri, DegisimGunSonuTestleri, DegisimIkiTestleri, KarRaporTestleri, KarZararEkranTestleri,
     MadenIkiTestleri, OrtakGorunumKiyasTestleri, P69HasAdlandirmaTestleri, ParaDemo2EkranTestleri, ParaDemoEkranTestleri, RaporGorunumTestleri,
     RaporlarEkranTestleri, S31PanelSadeTestleri, S34RaporBugunTestleri, Toplu5Testleri, Toplu6Testleri, UX1C3EkranTestleri, ZoBelgeTestleri,
     G5SatisKirilimiEkranTestleri, RaporYaslandirmaTestleri, Toplu3Testleri
```
Dokunulan sayfalar: Panel.razor, Raporlar.razor, KarZarar.razor (gerekçe: RAPOR-2 kalanları bu üç ekranı açıkça istiyor — aynı rapor kümesinin devamı). Migration YOK.

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Özet tahsilat = Gün sonu (açılış/virman dışı) | KARŞILANDI | canlı API=SQL 88.250,50; R3_01; M1/M2 |
| Bekleyen iş satır listesi, boş durum yok, iki ekran aynı bileşen | KARŞILANDI | 66 ölçüm; R3_20/21; M4–M6, M20 |
| Panel kâr satırı kapsamı + bölünmez boşluk | KARŞILANDI | R3_22; M7 |
| PRO "NET HAS GRAM" kapsamı, BASİT ile çelişmez, "Paralel defter" yok | KARŞILANDI | görüntü + R3_23/R3_30; M8/M17 |
| K17 notları ⓘ | KARŞILANDI (Stok yaşı başlık notu UX-S33 gereği kaldı) | R3_30, R3_42 |
| Para biçimi tek kural | KARŞILANDI | R3_25, R3_42; M10 |
| Grafik eksenleri | KARŞILANDI | R3_27/28; M18/M19; görüntü |
| Boş dönem / geçersiz tarih | KARŞILANDI | R3_40/41; M13/M14 |
| Grafik maliyet yok etiketi | KARŞILANDI | R3_42; M15 |
| Seçici genişliği + "Kâr-zarar" | KARŞILANDI | ölçüm 120/152 px; M16 |
| Gün sonu rozet + iade kapsamı | KARŞILANDI | R3_26; M11 |
| Stopaj ayrı | KARŞILANDI | R3_24; M9 |
| build 0 hata (paketin uyarısı 0) | KARŞILANDI | b13.log |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | §8 |
| tam süit 0 yeni kırmızı | KARŞILANDI (5 kendi beklentisi düzeltildi, hedefli koşum yeşil) | §4 |
| tarayıcı 1440/1280/375 × BASİT/PRO × açık/koyu | KARŞILANDI (istisna: kasa adı sarması, §6) | §6 |
| test kiracısı silindi | KARŞILANDI | §7 |

## 11. Açık kalanlar (sahibiyle)
- `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula…` ve `Dogrulayici.YASAL1SinirHttpTestleri.Sistem_ucu_EKSI_had_400…` hedefli koşumda da kırmızı → **YASAL-4 / YASAL-1 sahipleri** (gece 00–03 TSİ gün sınırı olasılığı doğrulanmadı). `TevsikIhlalDbTestleri` ×4 filtreli koşumlarda kırmızıydı (tam süitte geçti) → **Tevsik sahibi** (kararsız).
- Rapor merkezi BASİT 1280 "Kasada ne var" kartında uzun kasa adı iki satıra sarar (iki ondalık tutar 3 karakter genişletti) → TOPLU (düşük).
- Panel grafiği "Son 7 günlük satış" adını taşırken ayın tüm günlerini çizer (öncesinden var) → Panel PRO sadeleştirme / TOPLU.
- PRO "Bugün satılan altın (iade düşülmüş)" etiketi 1440 kutucukta iki satıra sarar (kırık değil) → Panel PRO sadeleştirme.
- Kalıcı tarayıcı gerileme ölçümünün dalga kapısına eklenmesi → orkestratör/TOPLU.
- Hub `Toplu6Sozlugu.PanelBekleyenSeritte`, `TevsikTaksitSozlugu.PanelSeritte` Panel/Rapor merkezinde artık okunmuyor → başka okuyan yoksa hub temizliği (TOPLU).
- Mutasyon aracı tuzağı (geri kopyada eski mtime → bayat artımlı derleme) — şerit betiklerine `touch` eklenmesi → orkestratör.

## 12. Kurucu / mali müşavir
Mevzuat hükmü içermez (gösterim/tanım: tahsilat = kasaya müşteriden giren para, açılış devri ve virman hariç — PARA-DEMO-2 kararının aylık karta uygulanması; net satış = brüt − iade). Not yok.
