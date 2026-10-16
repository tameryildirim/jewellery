# PAKET RAPOR-2 — uygulama raporu (03.10, t1 · API 5160 / vekil 5161 / CDP 9709-9711)

> Taşkın (backend) + Ertuğ (frontend). Kaynak: `PAKET-RAPOR-GORUNUM-uygulama-raporu.md` + RAPOR-GORUNUM hasım t2 / tasarım t2 bulguları.
> COMMIT YOK. Migration YOK (şema değişmedi). Ham kanıt: `ham/RAPOR-2-olcum/` (canlı JSON, SQL, mutasyon, tur analizi, 77 görüntü).

## 1. Tutarlılık maddeleri (TUTARLILIK İLKESİ) — tanım, kök neden, karar

| Kavram | Ekranlar · uç | Kök neden | Karar (Bertan + Mustafa oyu) | Kanıt |
|---|---|---|---|---|
| **Bugünün has satışı** | Panel + Rapor merkezi "Bugün ne sattım" → `GET /raporlar/gunluk` `satis.toplamHas` (DEFTER has gramı, yalnız altın); Kâr-zarar has bölümü → `GET /raporlar/kar-zarar` `hasilatHas` (TL satış tutarının satış günü altın fiyatıyla karşılığı) | İki AYRI büyüklük; Kâr-zarar başlığı "Altın kârı (has gram)" dediği için aynı şey gibi okunuyordu (`Sozlukler.OrtakGorunum.cs:90` `KarZararAltinKariBasit`) | FARKLI TANIM → ekranda kapsam etiketi: başlık `KR.AltinKariBaslik` "Altın kârı (satış tutarının altın karşılığı)"; "has gram" ifadesi kalktı. Defter gramı PRO'da "Defterdeki altın" kartında (değişmedi). Panel = Rapor merkezi AYNI uç, AYNI sayı | Canlı: Panel "28,3960 has" = Rapor merkezi "28,3960 has" = API 28.396 = SQL SATIS ALTIN Σ\|has\| 28,396000; Kâr-zarar hasilatHas 24.5092 başlığı "satış tutarının altın karşılığı" (görüntü `kz-ust_*`); R2_40 |
| **Altın pozisyonu** | Panel + Rapor merkezi → `AltinPozisyonuKarti` (Sade) → `GET /pozisyon` | Sade yüzde kırılım yalnız Vitrin + Alacak çiziyordu; Hurda ve Borç `Tam &&` koşuluyla gizliydi (`AltinPozisyonuKarti.razor` eski satır 76/81) → ana değer ≠ kırılım toplamı | Sıfır olmayan Hurda ve Borç sade yüzde de kırılımda: gösterilen kalemler toplamı = ana değer (NetGram = Vitrin + Hurda + Alacak − Borç, sunucu sözleşmesi) | Canlı iki ekranda "+15,1140 has · Vitrin 14,1980 · Alacak 0,0000 · Hurda 0,9160" (14,198+0,916 = 15,114); SQL: SAYIM 32,976 + ALIŞ 9,618 − SATIŞ 28,396 = 14,198 vitrin, HURDA_ALIS ALTIN 0,916; R2_23 (iki ekranda toplam = net), TP1_09 güncellendi |
| **Bekleyen iş sayısı** | Panel: bildirimler + uyum sayaçları (`/masak/ozet`, `/gider-pusulalari/sure-takibi`, `/masak/risk/ozet`, `/stopaj/ozet`) + yasal bantlar (`UyumYasalBantlari` → `/masak/uyum-ozet`); Rapor merkezi: YALNIZ `/bildirimler` | Rapor merkezi iki kaynağı hiç okumuyordu → Panel "Bekleyen 1 iş üstteki uyarıda", Rapor merkezi aynı anda "Bekleyen iş yok" | Tek tanım = Panel'in tanımı. Rapor merkezi kartı AYNI bileşeni (`UyumYasalBantlari`, kartın içinde) ve AYNI uyum uçlarını (SAHİP/MÜDÜR/MUHASEBE; tezgâhtarda istek YOK) okur; boş durum yalnız üçü de sıfırken, yasal bantta iş varken Panel'le AYNI cümle | Canlı (yasal liste yüklenmemiş, listeEski=true → 1 iş): Panel "Bekleyen 1 iş üstteki uyarıda." = Rapor merkezi aynı cümle + bant kartın içinde (görüntü `rm-bekleyen_*`); R2_20/R2_21/R2_22 |
| **Bugün ne aldım** | Rapor merkezi → `/raporlar/gunluk` `alis`/`hurda` | Sunucu `madenDagilimi`ni döndürüyordu, istemci sözleşmesi (`IslemOzet`, `HurdaOzet`) okumuyordu; has satırı koşulsuz "0,0000 has" | Has satırı YALNIZ ALTIN; altın dışı maden kendi satırında "Gümüş: saf 37,00 g" (gramlar toplanmaz); alış yalnız gümüşse has satırı YAZILMAZ | Canlı: "3 alış 9,6180 has · Gümüş: saf 37,00 g · Hurda 3.674,80 ₺ 0,9160 has · Gümüş: saf 37,00 g" = SQL ALIS ALTIN 9,618 / GUMUS 37,000; HURDA ALTIN 0,916 / GUMUS 37,000; R2_01 (DB), R2_24 (yalnız gümüş → has satırı yok), R2_25 |

Backend (Taşkın): sunucu tarafında DEĞİŞİKLİK GEREKMEDİ — dört kavramın veri kaynağı zaten doğruydu (has yalnız altın, maden dağılımı ayrı, NetGram sözleşmesi). Kusurlar istemcide okunmayan alan / gizlenen kalem / tek kaynaktan okuma idi. Sözleşme eki: `PanelSozlesmeleri.cs` `GunlukMadenPayi` + `IslemOzet/HurdaOzet.MadenDagilimi` (isteğe bağlı, sonda).

## 2. Görünüm maddeleri

| Bulgu | Çözüm | Ölçüm (başsız Chrome) |
|---|---|---|
| [KRİTİK] Ürünler tablosu 1280/1440'ta sağdan kırpılıyor | Ad sütunu sarar: `td.kz-urun-ad { white-space: normal; overflow-wrap: anywhere; min-width: 12ch; max-width: 26ch }`, iç span da sarar, "maliyet eksik" rozeti adın altında blok; has gerekçe notu sarar (max 18ch) | 73 karakterlik ad: 1280 tablo 964 = kap 964, 1440 tablo 1124 = kap 1124; kırpılan başlık 0, taşan hücre 0, yatay kaydırma yok — BASİT/PRO × açık/koyu (8 durum) |
| [YÜKSEK] 375 kartında ad kesik | `.rg-rapor-tablo td:first-child > span { white-space: normal; overflow-wrap: anywhere }` | 375: td.scrollWidth 333 = clientWidth 333 (tüm satırlar), görüntü `kz-urunler_PRO_karanlik_375` |
| [DÜŞÜK×2] Grafikler iki kart hizasız | KÖK NEDEN: `app.css:1867-1868` `.form-grid-2 > :not(.alan)… { align-self: end }` kartlara uyuyordu (ızgaranın align-items'ı öğe align-self'ini yenmez) → `#kz-grafikler > .kart { align-self: start }` | üst farkı 1280/1440: 48 px → **0 px** (8 durum); 375'te alt alta (beklenen) |
| [DÜŞÜK] Dönem seçici dağınık, "Bu ay" birincil | Üç ön ayar tek segment (`musteri-segment`, `role=group`, `aria-pressed`, seçili vurgulu, birincil düğme yok; id'ler aynı). Filtre yuvasının 152 px taban/en az genişlik kuralı segmenti kenar menünün altına taşırıyordu → `#kz-donem-segment { flex: 0 0 auto; width: auto; min-width: 0; flex-direction: row }` | 1280: 296-385 / 389-461 / 465-530 aynı satır, boy 34; 375: üçü 112 px eşit, boy 44 (görüntü `kz-ust_*`) |
| [DÜŞÜK] K17 düz açıklama satırları | Stok yaşı açıklaması, "madenler"/"devir" notları, Reyon notu, grafik altı notlar, pasta notu, Rapor merkezi "Diğer raporlar" → ⓘ (`t4-ipucu-satir` + `zo-ipucu`), metin ve id'ler aynı. "Diğer raporlar" ⓘ sayfa sonunda yukarı açılır (turda ekran-dışı çıktı, düzeltildi: 1280 684-760 / vh 800; 375 696-754 / vh 812) | görüntü `rm-stokyasi_*`, `kz-grafikler_*`, `rm-diger_375` |
| [DÜŞÜK] Stok yaşı 5+5+1, eşikler açık | `#sy-devir-izgara` ≥64rem 3+3; eşik alanları kapalı `details.sade-ayrinti#sy-ayarlar` "Ayarları değiştir" | dizilim 1280/1440 [3,3], ayarlar kapalı |
| [DÜŞÜK] Panel kâr satırı "maliyeti eksik:" sonrası kırılıyor | İadeli durumda kısa kalıp `KR.PanelKarKismiKisa` "Kâr … · eksik: 9 satış, 1 iade", "eksik:" ile öbek arası U+00A0, öbek bölünmez | R2_27; canlı (iade MASAK kümülatif sınırına takıldığından yalnız satış eksik): satır sayısı 1 |
| [DÜŞÜK] "HAS kâr" / "Ürün Adı" etiketi | `KR.HasKarEtiket` "Has kâr" (th + kart etiketi); ad hücresinde data-etiket yok | 375 kart görüntüsü: ad etiketsiz başlık, "Has kâr" |
| [DÜŞÜK] Kim borçlu has eşiği | `Panel.HasGosterimEsigi = 0.00005` (görüntü hassasiyeti) — Alacak/Borç/tutar parçası | R2_26 (0,00004 listede yok, 0,00005 "0,0001 has") |

`(bugün itibarıyla — dönemden bağımsız)` alt metni KALDI: UX-S33 bağlayıcı sözleşme maddesi (dönemden bağımsız kart bunu yazar).

## 3. Sapmalar ve gerekçeler
- Spesifikasyon "KAPSAM DIŞI" dediği iki maddeyi (pozisyon kırılımı, bekleyen iş) bu paketin DOSYA ALANI ve TUTARLILIK İLKESİ açıkça kapsadığı için uyguladım (AltinPozisyonuKarti.razor, Panel/Raporlar).
- Sözleşme dosyası `Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs` dosya alanında yazılı değil; sunucunun zaten döndürdüğü alanı okumak için ZORUNLU (isteğe bağlı, sonda; eski yanıt bozulmaz).
- Sahte API: `S34RaporBugunTestleri` varsayılanı tanımsız uca 404 dönüyordu → Rapor merkezi yeni okuduğu uyum uçları için gerçekçi "iş yok" yanıtları eklendi.
- Mühürlü test beklentileri (gerekçe = bulgu metni): KarZararEkranTestleri (P35_04, P35_05, ORTAK1_HK), MadenAyrimiTestleri MA23 (başlık), MadenIkiTestleri MI30 + RaporGorunumTestleri RG31 (kısa kalıp), RG42 (ad hücresi etiketsiz), S31PanelSadeTestleri TP1_09 (sade kırılımda hurda/borç).
- Canlı iade isteği MASAK kümülatif 185.000 TL kuralına takıldı (400, beklenen fail-closed davranış) → iade adımı atlandı, hurda ayrı adımda yapıldı; Panel kısa kalıbı bUnit (R2_27, MI30, RG31) ile kanıtlı.
- Mutasyon betiği perl yerine Python (TEK eşleşme sayımı ≠ 1 ise atlar), geri kopya + `filecmp shallow=False`.

## 4. DoD sözleşmesi
Yeni varlık/kayıt türü YOK, yeni uç YOK, yeni yazma düğmesi YOK. Yeni düğmeler: üç dönem ön ayarı (mevcut id'ler, her biri istek atar — R2_42 tıklama + aria-pressed). Yeni alanlar salt okunur türetilmiş rapor alanı (`alis/hurda.madenDagilimi` istemcide okunur). Rapor rakamları düzenlenmez; mevcut "Rapordaki bir rakam yanlışsa" katlanırı (DegismezIpucu gerekçesi) aynen. [RolGerekli] değişmedi; uyum uçları yalnız SAHİP/MÜDÜR/MUHASEBE rolünde çağrılır (R2_22).

## 5. Testler
- Yeni `Kuyumcu.Tests/RaporIkiTestleri.cs` — 16 metot / 17 vaka: DB R2_01; saf R2_10, R2_11; Panel+Rapor merkezi aynı bağlamda R2_20–R2_28; Rapor merkezi PRO R2_30; Kâr-zarar R2_40 (×2), R2_41, R2_42. Filtreli 17/17.
- Geniş ilgili küme (RaporIki, RaporGorunum, KarZarar, MadenIki, MadenAyrimi, S31, S34, KarRapor, AltinPozisyonu, ZoBelgeReyon, RaporEkran, Toplu4, P57, P69, Panel, Raporlar, ZOBELGE, RaporSeti): **836/836**.
- Build (son, `b6.log`): `dotnet build Kuyumcu.sln` **0 hata · 0 uyarı**.
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pRAPOR-2/_Mac_2026-10-03_08_16_50.trx`, KUYUMCU_TEST_DB=kuyumcu_test_t1): toplam **10274** · geçti **10268** · başarısız **4** · çalışmayan 2. Kırmızılar (hiçbiri bu paketin dosyası değil):
  - `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir`, `PosBarkodTestleri.Bulunamayan_barkod_KULLANICIYA_soylenir` — bUnit WaitFor zaman aşımı; tek başına **geçti** (kararsız; RAPOR-GORUNUM devrinde de aynı).
  - `Dogrulayici.PATCH5SinirHttpTestleri.D_P5_kasa_hareket_sifir_eksi…(0.005)` — tek başına **geçti**.
  - `Dogrulayici.Q101SinirTestleri.DQ101_kurus_yuvarlamasi_0_005_yukari_ve_storno_net_sifir` — tek başına da **KIRMIZI** (BadRequest beklenip OK); `KasaModulu.cs` değişik (SON-KALANLAR t2 şeridinin alanı: Kasa para birimi/kuruş). Bu paket KasaModulu'na dokunmadı.
  - **Bu paketin yeni kırmızısı 0.**

## 6. Mutasyon tablosu (betik `/private/tmp/kuyumcu-serit/RAPOR-2/mut.py`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/RAPOR-2/`, ham `ham/RAPOR-2-olcum/mut-sonuc.txt`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M1 | has satırı her zaman (yalnız gümüş günü 0,0000 has) | R2_24 | AYNI |
| M2 | altın da ayrı maden satırına girer | R2_24 | AYNI |
| M3 | Rapor merkezi yasal bandı saymaz | R2_20 | AYNI |
| M4 | uyum sayaçları okunmaz | R2_21 | AYNI |
| M5 | yetkisiz rolde de uyum isteği | R2_22 | AYNI |
| M6 | sade kırılımda hurda yok | R2_23, TP1_09 | AYNI |
| M7 | Kim borçlu eşiği 0,000001 | R2_26 | AYNI |
| M8 | Panel kâr eski uzun kalıp | R2_27, MI30, RG31 | AYNI |
| M9 | başlık yine "has gram" | R2_40 (×2), ORTAK1_HK (×2), MA23 (×2), P35_05 | AYNI |
| M10 | kartta "Ürün Adı" etiketi geri | R2_41, RG42 | AYNI |
| M11 | "HAS kâr" eski yazım | R2_41, P35_04 | AYNI |
| M12 | seçili ön ayar hep "bu ay" | R2_42 | AYNI |
| M13 | "Bu ay" yine birincil | R2_42 | AYNI |
| M14 | 375 iç span nowrap | R2_11 | AYNI |
| M15 | ad sütunu azami genişliksiz | R2_11 | AYNI |
| M16 | grafik kartı align-self yok | R2_11 | AYNI |
| M17 | eşikler açılışta açık | R2_30 | AYNI |
| M18 | madenler notu ⓘ dışında | R2_30 | AYNI |
| M19 | kısa öbek yine "ve" | R2_10 | AYNI |
| M20 | diğer raporlar düz satır | ilk koşum 0 kırmızı (TEST AÇIĞI) → R2_28 güçlendirildi (doğrudan çocuklar SPAN+DETAILS) → tekrar **R2_28 KIRMIZI** | AYNI |
20/20 kırmızı, 20/20 cmp AYNI; ardından temiz `dotnet build Kuyumcu.sln` 0/0 ve R2 17/17.

## 7. Denetim
`bash denetim.sh` (son, `ham/RAPOR-2-olcum/denetim2.log`): 303 ✓ · tek ✗ `P-30: DEMO_INSAN_SURESI` (muaf) → **TEMİZ (P-30 hariç)**. İlk koşumda bu paketten iki ✗ çıktı ve düzeltildi: `Pages: @onclick'siz <button> 73 (tavan 71)` (segment düğmelerinde @onclick ikinci satırdaydı; sayaç satır bazlı → @onclick ilk satıra, sayaç 70) ve `P-39: TABLO_SARMALAYICISIZ 1` (`class="tablo-sar kz-urun-sar"` tam sınıf eşleşmesini bozdu → `class="tablo-sar" id="kz-urun-sar"`).

## 8. Canlı doğrulama (API 5160 `dotnet run --no-build`, Development; ham `ham/RAPOR-2-olcum/canli/`, `canli-olc.txt`, `sql-bagimsiz.txt`)
Kiracı **PAKETRAPOR-2-TEST** (01M401PFPB823Q2A2RXQ4CYVQ2). Kurulum: davet 201, kayıt 201, giriş 200, ürün 201×3 (biri 73 karakter adlı açılış stoklu), cari 201×2, alış 200×3 (1 altın, 2 gümüş), satış 200×5, iade 400 (MASAK kümülatif — beklenen), kasa 200, kasa girişi 200, hurda 200×2 (K22 1 g, G925 40 g).

| Uç | HTTP | API | Bağımsız SQL |
|---|---|---|---|
| `/raporlar/gunluk` | 200 | satis.toplamHas 28.396; alis.toplamHas 9.618, madenDagilimi GUMUS 37.0 / ALTIN 9.618; hurda.toplamHas 0.916, madenDagilimi GUMUS 37.0 / ALTIN 0.916 | SATIS ALTIN 28,396000 · ALIS ALTIN 9,618 / GUMUS 37,000 · HURDA ALTIN 0,916 / GUMUS 37,000 |
| `/raporlar/kar-zarar` | 200 | hasilatHas 24.5092 (TL→has), KISMI, eksik 2 satış | (farklı tanım — başlıkla ayrıldı) |
| `/pozisyon` | 200 | net 15.114 = vitrin 14.198 + hurda 0.916 + alacak 0 − borç 0 | SAYIM 32,976 + ALIS 9,618 − SATIS 28,396 = 14,198; HURDA ALTIN 0,916 |
| `/masak/uyum-ozet` | 200 | listeEski true → bant işi 1 | — |
| `/bildirimler`, `/masak/ozet`, `/gider-pusulalari/sure-takibi`, `/masak/risk/ozet`, `/stopaj/ozet` | 200×5 | bildirim 0, uyum sayacı 0 | — |
| `/raporlar/kar-zarar/urun` | 200 | R2-L ad 73 karakter, maliyetiBilinmeyenAdet 2 | — |

Temizlik: API, vekil, başsız Chrome'lar PID ile durduruldu; 5160/5161/9709/9710/9711 dinleyen yok. `zsh kiraci-temizle.sh 01M401PFPB823Q2A2RXQ4CYVQ2 --uygula` → tenants/hareketler/stok_partileri bu kimlik için **0** (ikinci çağrı "Kiracı YOK"). tenants: başlangıç **16** → son **15** (benimki silindi; −1 eşzamanlı başka şeridin kendi temizliği — dokunmadım). QA-TEST-* dokunulmadı.

## 9. Gerçek tarayıcı (başsız Chrome)
**(a) Genel tur** `betikler/tur/tur.mjs` (CDP 9709, vekil 5161; `ham/RAPOR-2-olcum/tur-analiz.txt`, `tur-olcum-1.jsonl`): 278 kayıt (36 sayfa = 3 rota × 12 kombinasyon 1440/1280/375 × BASİT/PRO × açık/koyu; 78 sekme; 164 katman), **hata 0 · geçersiz 0**. Temiz: `/`, Kâr-zarar Köprü/Stok/**Ürünler**. Kalan bulgular ÖNCEDEN VAR / başka şerit: K12 `#rp-yil/#rp-ay` (TOPLU-6), K4 "tren" 375 PRO ve Gün sonu (DEĞİŞİM-2). Yeni çıkan tek kalem `[i] ekran-dışı` (Rapor merkezi BASİT "Diğer raporlar ⓘ" aşağı açılıyordu) → yukarı açılır yapıldı, hedefli ölçümle doğrulandı.

**(b) Hedefli ölçüm** `/private/tmp/kuyumcu-serit/RAPOR-2/hedef.mjs` (`ham/RAPOR-2-olcum/hedef-olcum.jsonl`; son derleme ile tekrar: `hedef-son-*.log`):

| Ölçüm | 1440 B/P a/k | 1280 B/P a/k | 375 B/P a/k |
|---|---|---|---|
| Ürünler tablo/kap (px) · kırpılan başlık · taşan hücre | 1124/1124 · 0 · 0 | 964/964 · 0 · 0 | 351/351 kart yığını · ad td scroll=client 333 · 0 |
| Grafikler üst farkı | 0 | 0 | alt alta (tek sütun) |
| Dönem segmenti | tek satır, boy 34 | tek satır, boy 34 | eşit 112 px, boy 44 |
| Stok yaşı ölçüm dizilimi (PRO) | 3+3, ayarlar kapalı | 3+3, kapalı | tek sütun |
| Panel = Rapor merkezi (BASİT) | satış has 28,3960 = 28,3960 · pozisyon +15,1140 kırılım toplamı tutar · bekleyen aynı cümle | aynı | aynı |

Read ile GÖRÜLEN görüntüler (`ham/RAPOR-2-olcum/foto/`): `kz-urunler_PRO_aydinlik_1280.png` ve `_1440.png` (uzun ad iki satıra sarar, rozet altta, HAS KÂR ve MARJ görünür), `kz-urunler_PRO_karanlik_375.png` (kart yığını, ad tam, etiketsiz, "Has kâr"), `kz-grafikler_PRO_aydinlik_1280.png` (düzeltme ÖNCESİ alttan hiza) → `kz-grafikler_BASIT_karanlik_1280.png` (aynı üst çizgi, notlar ⓘ), `kz-ust_PRO_aydinlik_1280.png` (ilk sürümde segment kenar menünün altına taşmıştı → düzeltme sonrası tek grup), `kz-ust_BASIT_karanlik_375.png`, `rm-bugun-alis_BASIT_aydinlik_1280.png` ("3 alış 9,6180 has / Gümüş: saf 37,00 g / Hurda 3.674,80 ₺ 0,9160 has / Gümüş: saf 37,00 g"), `rm-bekleyen_BASIT_karanlik_1440.png` (bant kartın içinde + "Bekleyen 1 iş üstteki uyarıda."), `panel-pozisyon_BASIT_karanlik_375.png` (+15,1140 · Vitrin · Alacak · Hurda 0,9160), `rm-stokyasi_PRO_aydinlik_1440.png` (5 kova + 3+3, "Nasıl hesaplanır? ⓘ", "Ayarları değiştir ▸"), `rm-diger_375.png` (ipucu yukarı açık), `panel-satis_BASIT_karanlik_1280.png`.

## 10. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` süzümü, bu paketin dokunduğu)
```
 M dotnet/Kuyumcu.Web/Pages/Panel.razor                    (HasGosterimEsigi; kısa kâr kalıbı)
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor                 (Bugün ne aldım maden satırları; bekleyen iş tek tanım; K17 ⓘ; stok yaşı 3+3 + katlanır eşikler)
?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor                 (dönem segmenti; has başlığı; Ürünler ad hücresi; "Has kâr"; grafik notları ⓘ)
?? dotnet/Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor (sade kırılımda Hurda/Borç)
?? dotnet/Kuyumcu.Web/wwwroot/css/rapor-gorunum.css        (+ (7)–(12) kuralları; sabit renk yok)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs          (+12 anahtar TR+EN, EksikSayiKisa)
?? dotnet/Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs  (GunlukMadenPayi, MadenDagilimi isteğe bağlı)
?? dotnet/Kuyumcu.Tests/RaporIkiTestleri.cs                (yeni)
?? dotnet/Kuyumcu.Tests/{KarZararEkranTestleri,MadenAyrimiTestleri,MadenIkiTestleri,S34RaporBugunTestleri,RaporGorunumTestleri,S31PanelSadeTestleri}.cs (beklenti/sahte API güncellemeleri, §3)
```
Dokunulan sayfalar: Panel.razor, Raporlar.razor, KarZarar.razor (gerekçe: RAPOR-GORUNUM hasım/tasarım t2 bulguları bu üç ekranı açıkça istiyor — aynı rapor kümesinin devamı; Raporlar Gün sonu ve yıl/ay seçicisine dokunulmadı). Migration YOK.

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| [KRİTİK] Ürünler tablosu 1280/1440 kırpılmaz | KARŞILANDI | §9 tablo 964/964, 1124/1124, 8 durum; M14/M15 |
| [YÜKSEK] 375 ad sarar | KARŞILANDI | td scroll=client; görüntü |
| Grafikler üst farkı ≤ 1 px | KARŞILANDI | 48 → 0 px; M16 |
| Bugün ne aldım gümüş ayrı, yalnız gümüşte 0,0000 has yok | KARŞILANDI | R2_01/24/25, canlı = SQL; M1/M2 |
| Has satış iki sayı → kapsam etiketi | KARŞILANDI | R2_40; canlı; M9 |
| Altın pozisyonu ana = kırılım toplamı | KARŞILANDI | R2_23; canlı = SQL; M6 |
| Bekleyen iş iki ekranda aynı | KARŞILANDI | R2_20/21/22; canlı iki ekran aynı cümle; M3/M4/M5 |
| K17 notları ⓘ | KARŞILANDI | R2_28/R2_30; M18/M20 |
| Stok yaşı 3+3, eşikler katlanır | KARŞILANDI | R2_30, ölçüm [3,3]; M17 |
| Dönem seçici tek segment | KARŞILANDI | R2_42, ölçüm; M12/M13 |
| Panel kâr satırı | KARŞILANDI | R2_27/MI30/RG31; M8 |
| Etiket tutarsızlığı | KARŞILANDI | R2_41; M10/M11 |
| Kim borçlu eşiği 0,00005 | KARŞILANDI | R2_26; M7 |
| build 0/0 | KARŞILANDI | b6.log |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | §7 |
| tam süit 0 yeni kırmızı | KARŞILANDI | §5 |
| test kiracısı silindi | KARŞILANDI | §8 |

## 12. Açık kalanlar (sahibiyle)
- `Dogrulayici.Q101SinirTestleri.DQ101_kurus_yuvarlamasi_0_005_yukari_ve_storno_net_sifir` tek başına da kırmızı → **SON-KALANLAR (t2) / KasaModulu sahibi**.
- Kararsız (tek başına yeşil): `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden…`, `PosBarkodTestleri.Bulunamayan_barkod…` → **POS ekranı sahibi**; `PATCH5SinirHttpTestleri.D_P5_kasa_hareket…(0.005)` → **PATCH-5 / Kasa**.
- `/raporlar` K12 `#rp-yil/#rp-ay` ezik → **TOPLU-6**; Gün sonu K4 → **DEĞİŞİM-2**; 375 PRO K4 "tren" (sayfa boyu) → ölçer/TOPLU.
- Kova ızgarası: "Yaşı bilinmeyen" kartı varsa 5+1 dizilir (bu veride yoktu) → TOPLU kuyruğu (düşük).
- Hub `OrtakGorunumSozlugu.KarZararAltinKariBasit` ve `Dil.T.KarZarar.HasKar` artık Kâr-zarar'da okunmuyor (başka okuyan yoksa hub temizliği) → TOPLU (hub'a dokunulmadı).

## 13. Kurucu / mali müşavir
Mevzuat hükmü içermez (gösterim/tanım: has yalnız altın, bekleyen iş tanımı, görüntü eşiği). Not: canlı iade adımı MASAK kümülatif 185.000 TL kimlik tespiti kuralına takıldı (mevcut fail-closed kapı, bu paket değiştirmedi).

---

# TUR 2 (03.10, t1 · API 5160 / vekil 5161 / CDP 9711) — doğrulamada kalan bulguların kapatılması

Ham kanıt: `ham/RAPOR-2-olcum/tur2/` (hedef2-olcum.jsonl, 42 görüntü `foto/`, canli-olc.txt, sql-bagimsiz.txt, mut-sonuc.txt, denetim2.log) + `ham/RAPOR-2-olcum/canli-tur2/*.json`. Betikler `/private/tmp/kuyumcu-serit/RAPOR-2/tur2/`. Migration YOK.

## T2-1. [KRİTİK, tasarım hakemi] "Bugün ne sattım" ↔ Kâr-zarar — aynı gün iki sayı
**Kök neden:** `/raporlar/gunluk` `satis.toplamTl/toplamHas` yalnız `Tur=="SATIS"` toplar, iadeyi düşmez, storno çiftini dışlamaz (`RaporModulu.cs` GunlukOzet); Kâr-zarar `hasilatTl` = brüt − iade ve `altinHasilatHasDefterGram` = Σ|satış has| − Σ|iade has|, storno çifti dışlı (`KarZararServisi.cs:296-352`). Ek: aylık `satis.toplamTl` da iadeyi düşmüyordu (Rapor merkezi PRO "AYLIK SATIŞ").
**Karar (Bertan + Mustafa oyu): yol (a)+(b) birlikte** — aynı kavram aynı sayı: "Bugün ne sattım" Kâr-zarar ile BİREBİR aynı NET tanımı gösterir; iade varsa kapsam satırı "1 iade (−21.000 ₺)" görünür (fark iade satırıyla görünür). Gerekçe: Mustafa — kâr ve ciro iade düşülmüş hasılattan okunur, tezgâhtarın "bugün ne sattım"ı da bu; Bertan — tanım tek yerde (Kâr-zarar ölçümüyle aynı küme ve aynı 48-A storno süzgeci), eski `satis` alanı şekil sözleşmesi (P69 bekçisi) gereği korunur, yeni alan ekleme.
- Sunucu: `/raporlar/gunluk` yeni `netSatis {satisAdet, iadeAdet, brutTl, iadeTl, toplamTl, brutAltinHas, iadeAltinHas, altinToplamHas}` — SATIS+SATIS_IADE, `StornoCiftiniDisla`, has yalnız altın. `/raporlar/aylik` yeni `iade {sayisi, toplamTl}` + `netSatisTl`.
- Sözleşme: `PanelSozlesmeleri.GunlukOzet.NetSatis` (+`NetSatisOzet`), `RaporlarSozlesmeleri.AylikYanit.Iade/NetSatisTl` (+`AylikIade`) — isteğe bağlı, sonda; alan yoksa ekran eski brüte düşer.
- Ekran: Panel BASİT kart + PRO "GÜNLÜK SATIŞ" (alt satır iade), Rapor merkezi BASİT kart, PRO "AYLIK SATIŞ" (net + "İade düşüldü · brüt 143.500 ₺ · 1 iade"). PRO "GÜNÜN NAKİT AKIŞI (satış − alış − hurda)" da AYNI net satıştan (ilk ölçümde 93.625,20 brütten hesaplanıp etiketle çelişiyordu → 72.625,20 = 122.500 − 46.200 − 3.674,80).
- Sözlük (`Sozlukler.KarRapor.cs`, TR+EN): `BugunIadeSatiri`, `AylikSatisNetAlt`.

**Canlı (5160, kiracı PAKETRAPOR-2-TEST `01M406QYVBXC9QT19Y2F1CJ40C`):** iade bu kez ALTIN satıştan (R2-T 21.000 ₺) → HTTP 200 (tur 1'deki MASAK kümülatif 400'ü 60.000 ₺ iadeydi).

| Ölçüm | API | Bağımsız SQL |
|---|---|---|
| `/raporlar/gunluk` netSatis | satisAdet 5, iadeAdet 1, brüt 143.500, iade 21.000, **toplamTl 122.500**, brüt has 28,396, iade has 3,206, **altinToplamHas 25,19** | SATIS ALTIN 142.000 + GUMUS 1.500; SATIS_IADE ALTIN |tl| 21.000, |has| 3,206 → **net_tl 122.500,00 · net_altin_has 25,190000**; storno 0 |
| `/raporlar/kar-zarar` (bugün) | **hasilatTl 122.500**, brutHasilatTl 143.500, iadeTl 21.000, **altinHasilatHasDefterGram 25,19** | aynı |
| `/raporlar/aylik` | satis.toplamTl 143.500 (eski alan), iade {1, 21.000}, **netSatisTl 122.500** | — |
| `/raporlar/kar-zarar` (bu ay) | **hasilatTl 122.500** | — |

**Tarayıcı (başsız Chrome, 1440/1280/375 × BASİT/PRO × açık/koyu = 12 kombinasyon × 3 rota):** Panel BASİT `122.500 ₺ · 5 satış · 1 iade (−21.000 ₺) · 25,1900 has` = Rapor merkezi BASİT aynı = Kâr-zarar BASİT "Toplam satış 122.500 ₺" = Kâr-zarar PRO "DEFTERDEKİ ALTIN (HAS) 25,1900 has"; Panel PRO "GÜNLÜK SATIŞ 122.500 ₺ / 1 iade (−21.000 ₺)", "GÜNÜN NAKİT AKIŞI 72.625,20 ₺"; Rapor merkezi PRO "AYLIK SATIŞ 122.500 ₺ / İade düşüldü · brüt 143.500 ₺ · 1 iade". Ölçer (`sikisma-olc.js`) 36 sayfa: K1 0 · K3 0/0 · K5 (TRY) 0 · K9 0 · K10 0 · K11 0 · K12 0 · yatay 0 (K4 "tren" önceden var — tur 1 §12). Read ile GÖRÜLEN: `panel-satis_BASIT_aydinlik_1280.png`, `rm-satis_BASIT_karanlik_375.png`, `rm-pro-ozet_PRO_aydinlik_1280.png`, `kz-bugun_BASIT_aydinlik_1440.png`, `panel-pro-stat_PRO_karanlik_375.png` (ilk sürüm, nakit akışı brüt) → `panel-pro-stat_PRO_aydinlik_1280.png` (düzeltme sonrası 72.625,20), `rm-bekleyen_BASIT_karanlik_1440.png`. Panel 1280'de alt satır öğeleri iki satıra sarar ("5 satış · 1 iade (−21.000 ₺)" / "25,1900 has"); öğe içi kırılma yok (K3 kırık 0).

## T2-2. [DÜŞÜK] Bekleyen iş — nakit sınırı (TevsikIhlali) ayrımı Rapor merkezinde de
`Raporlar.razor`: bekleyen iş kartına Panel'in `NakitSiniriPanelSatiri` bileşeni (aynı uç `/tevsik/ihlaller`, yalnız SAHİP/MUHASEBE) eklendi; `/bildirimler` sonucu Panel ile AYNI süzgeçten geçer (`IlgiliVarlikTuru != "TevsikIhlali"`), boş durum cümlesi Panel.razor:735 formülünün aynısı (`PanelSeritte` → yasal bant → `BekleyenYok`). Canlı kiracıda kapanmamış ihlal 0 (yalnız yasal bant 1 iş → iki ekran "Bekleyen 1 iş üstteki uyarıda."); ihlalli durum bUnit R2_32 ile.

## T2-3. Diğer düşükler
- Ölü kod `KarZarar.razor` `HasBacagiBaslik` kaldırıldı (grep 0); `Sozlukler.KarRapor.cs` yetim `<summary>` doğru metoda (`EksikSayi`) taşındı.
- Çok küçük gümüş: `Raporlar.AltinDisiMadenler` süzgeci `|gram| ≥ GramGosterimEsigi (0,005 g)` — 0,0049 g "saf 0,00 g" satırı açmaz (R2_33).
- Kırılım öğe yuvarlaması (±0,0001): BİLGİ — değişiklik yapılmadı (gösterim kalemleri N4, ana değer sunucu Y6; fark yalnız 0,00005 altı kalemlerde). Açık kalan.
- R2_11 metin eşlemesi: tarayıcı gerileme ölçümü kalıcı kapıya eklenmedi (dalga kapısı/TOPLU kararı) — bu turda hedefli tarayıcı ölçümü tekrarlandı.
- 5199: dokunulmadı (orkestratör).

## T2-4. Testler (yeni 7 → RaporIkiTestleri 24 vaka)
R2_02 (DB: gunluk netSatis = Kâr-zarar bugün hasilatTl ve altinHasilatHasDefterGram; storno çifti ve gümüş has dışı), R2_03 (DB: aylık netSatisTl = Kâr-zarar bu ay), R2_29 (Panel + Rapor merkezi aynı net TL/has + iade satırı, brüt görünmez), R2_31 (PRO aylık kart net + brüt alt satırı), R2_32 (nakit sınırı iki ekranda aynı tanım), R2_33 (0,0049 g gümüş satırı yok), R2_34 (Panel PRO günlük satış net + nakit akışı aynı satıştan).
Filtreli: RaporIki **24/24**; geniş ilgili küme (RaporIki, RaporGorunum, KarZarar, MadenIki, MadenAyrimi, S31, S34, KarRapor, AltinPozisyonu, ZoBelgeReyon, RaporEkran, Toplu4, P57, P69, Panel, Raporlar, ZOBELGE, RaporSeti, Yasal2, Gunluk, Aylik) **908/908**; Panel/S31/Kasa kümesi **564/564** (1 atlanan, önceden var).
**Tam süit (son kod)** `/private/tmp/kuyumcu-serit/trx-pRAPOR-2-t2b/*.trx`: toplam **10311 · geçti 10309 · başarısız 0** · çalışmayan 2. Tur 1'in 4 kırmızısı (PosBarkod ×2, D_P5, DQ101) bu koşumda yok. **Bu paketin yeni kırmızısı 0.**
Build: `dotnet build Kuyumcu.sln` **0 hata**; uyarı: yalnız test projesi yeniden derlendiğinde 4 uyarı — `Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49`, `KesifP2Testleri.cs:455`, `SonKalanlarTestleri.cs:584,813` (bu paketin dosyası değil; SonKalanlar = t2 şeridi). Son artımlı build 0/0.

## T2-5. Mutasyonlar (betik `tur2/mut2.py`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/RAPOR-2-t2/`, TEK eşleşme, geri kopya + `filecmp shallow=False`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M21 | gunluk net satış iadeyi düşmez | R2_02 | AYNI |
| M22 | gunluk net satışta storno çifti dışlanmaz | R2_02 | AYNI |
| M23 | net has iade hası düşmez | R2_02 | AYNI |
| M24 | aylık netSatisTl iadeyi düşmez | R2_03 | AYNI |
| M25 | Panel bugün satış brüt | R2_29 | AYNI (iki kez koşuldu) |
| M26 | Rapor merkezi bugün satış brüt | R2_29 | AYNI |
| M27 | Panel iade kapsam satırı yok | R2_29 | AYNI |
| M28 | Rapor merkezi bugün has brüt | R2_29 | AYNI |
| M29 | TevsikIhlali bildirimi listede ikinci kez | R2_32 | AYNI |
| M30 | Rapor merkezinde nakit sınırı şeridi yok | R2_32 | AYNI |
| M31 | gram eşiği 0,0000001 (saf 0,00 g satırı) | R2_33 | AYNI |
| M32 | PRO aylık satış kartı brüt | R2_31 | AYNI |
| M33 | PRO nakit akışı brüt satıştan | R2_34 | AYNI |
13/13 kırmızı, 13/13 cmp AYNI; son mutasyondan sonra temiz build 0/0, RaporIki 24/24.

## T2-6. Denetim
`bash denetim.sh` (son kod, `ham/RAPOR-2-olcum/tur2/denetim2.log`): **303 ✓**, tek ✗ `P-30: DEMO_INSAN_SURESI` (muaf) → **TEMİZ (P-30 hariç)**.

## T2-7. DoD sözleşmesi
Yeni varlık/kayıt türü YOK, yeni uç YOK (iki mevcut GET ucuna salt okunur türetilmiş alan), yeni yazma düğmesi YOK. [RolGerekli] değişmedi. Nakit sınırı şeridi Panel'deki rol kapısıyla (SAHİP/MUHASEBE; yetkisizde istek atılmaz).

## T2-8. Temizlik
API (PID), vekil (PID), başsız Chrome (9711) durduruldu: 5160/5161/9711 dinleyen 0. `zsh kiraci-temizle.sh 01M406QYVBXC9QT19Y2F1CJ40C --uygula` → ikinci çağrı "Kiracı YOK"; tenants/hareketler bu kimlik için 0. tenants: başlangıç **16** → son **15** (benimki silindi; ayrıca SON-KALANLAR şeridinin kendi `PAKETSK-TEST` kiracısı o şeridin temizliğiyle gitti — dokunmadım). QA-TEST-* dokunulmadı.

## T2-9. Değişen dosyalar (tur 2)
```
 M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs          (gunluk netSatis; aylik iade + netSatisTl)
?? dotnet/Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs   (NetSatisOzet, GunlukOzet.NetSatis)
?? dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs (AylikIade, AylikYanit.Iade/NetSatisTl)
 M dotnet/Kuyumcu.Web/Pages/Panel.razor                      (net satış BASİT+PRO, iade satırı, nakit akışı net)
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor                   (net satış, PRO aylık net, nakit sınırı şeridi + süzgeç, gram eşiği)
?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor                   (ölü HasBacagiBaslik silindi)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs            (+2 anahtar TR+EN; yetim summary)
?? dotnet/Kuyumcu.Tests/RaporIkiTestleri.cs                  (+7 test)
```
Dokunulan sayfalar (aynı paketin 2. turu, gerekçe = doğrulama bulguları): Panel.razor, Raporlar.razor, KarZarar.razor. Sapma: `RaporlarSozlesmeleri.cs` dosya alanında yazılı değil — aylık netin istemciye taşınması için zorunlu (isteğe bağlı alan, sonda).

## T2-10. Açık kalanlar (sahibiyle)
- Kırılım öğe yuvarlaması ±0,0001 (AltinPozisyonuKarti) — bilgi, gerekirse "yuvarlama farkı" notu → Panel/pozisyon sahibi.
- Kalıcı tarayıcı gerileme ölçümü (Ürünler tablo/kap, grafik üst farkı) dalga kapısına → orkestratör/TOPLU.
- Aylık özetin günlük grafiği (`gunler[].satisTl`) brüt kalır (grafik "satış" çubuğu); net yalnız kartta → TOPLU (düşük).
- Hub `OrtakGorunumSozlugu.KarZararAltinKariBasit`, `Dil.T.KarZarar.HasKar` başka okuyan yoksa hub temizliği → TOPLU.
- Build uyarıları 4 (başka şeritlerin test dosyaları) → SON-KALANLAR / SUBE-KAPSAM / KESIF sahipleri.
- 5199 dinlemiyor → orkestratör.

## T2-11. Kurucu / mali müşavir
Mevzuat hükmü içermez (gösterim tanımı: net satış = brüt − iade; storno 48-A süzgeci). Not yok.
