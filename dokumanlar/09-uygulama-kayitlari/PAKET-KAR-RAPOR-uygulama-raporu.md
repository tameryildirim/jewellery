# PAKET KAR-RAPOR — uygulama raporu (02.10, t1 · API 5160 / vekil 5161 / CDP 9705) — TUR 2

> §0 tur 2 değişikliklerini ve son ölçümleri taşır; §1–§13 tur 1 kaydıdır (tur 2 ile güncellenen satırlar §10–§12'de işaretli).

Rol: Taşkın (backend) + Ertuğ (frontend). COMMIT YOK. Migration gerekmedi.

## 0. TUR 2 (02.10 ~15:00) — hasım doğrulayıcı bulgularının kapatılması

| Bulgu | Ne yapıldı | Kanıt |
|---|---|---|
| [KRİTİK] Kâr kuruş yuvarlaması ToEven (`KarZararServisi.cs:514` `R()`) | `KarZararServisi.cs` içindeki **47 `Math.Round` çağrısının tamamı** (R(), Yuzde(), karTl, bilinenHasilatTl, maliyetiEksikTutarTl, karMarjiYuzde, ürün satırı kârı l.599-621, hasılat/maliyet/has alanları) `MidpointRounding.AwayFromZero` aldı → dosyada tek kural (proje kuralı, `RaporModulu.Yuvarla2` ile aynı). `grep "Math.Round" | grep -v AwayFromZero` = 0 satır. | KR15 (yeni) + KRS03 (hasım) yeşil; canlı KR-Z ürün satırı **500,13** (kesin 500,125; SQL) |
| [DÜŞÜK] Gün sonu aynı tutar iki uyarıda | `RaporModulu.cs` (pos-fatura-mutabakatı, l.~609): fark TAMAMEN faturalanmamış satıştan geliyorsa (`bagimsizFaturalar.Count == 0 && faturasiz.Count > 0 && fark == faturasizTl`) "…fark var" uyarısı basılmaz; bağsız fatura/kısmi faturalama varsa uyarı aynen kalır. Metin (Mutabakat sözlüğü) değişmedi. | KR25 (tek uyarı) + KR26 (bağsız faturada uyarı kalır); canlı C/D: `uyarilar` = yalnız "5 satış faturalandırılmamış (toplam 8.301,66 ₺)"; görüntü `tur2-foto/raporlar__PRO_aydinlik_1440__sekme5.png` tek sarı satır |
| [DÜŞÜK] Kısmi kâr yalnız alttaki küçük satırda | `Raporlar.razor`: `IpucuKart`'a isteğe bağlı `alt` (StatKart ile aynı `.stat-alt` sınıfı, yeni id `rp-kar-alt`); KISMI (`KarTl != null && eksik > 0`) iken kartın kendisinde "Kâr yalnız maliyeti bilinen satışlardan hesaplandı." (sözlükteki mevcut `KarKismiNotu`, TR+EN). `#rp-kar-eksik` satırında "kâra katılmadı · Ayrıntı →" ayracı. HESAPLANAMADI/TAM'da alt metin yok. | KR20/KR22 (`#rp-kar-alt` yok), KR21 (var + ayraç); görüntü `raporlar__PRO_aydinlik_1440.png`, `raporlar__PRO_karanlik_375.png` |
| [DÜŞÜK] 5199 dinlemiyor | Kapsam dışı; 5199'a dokunulmadı. Orkestratör/kurucu kalemi (§12). | — |

**Tur 2 tuzak kaydı (Anayasa md.1):** MKR-18 (razor mutasyonu) geri alındıktan sonra yapılan `dotnet build Kuyumcu.sln` Razor kaynak üretecinin artımlı önbelleği yüzünden `Kuyumcu.Web.dll`'i mutasyonlu hâlde bıraktı (cmp kaynakta AYNI idi). İlk tam süit (trx-pKAR-RAPOR-t2) bu yüzden KR21'de kırmızı verdi; tur turu da aynı ikiliyle başlamıştı. Tespit: `--no-build` ile KR21 3/3 kırmızı → `touch` + yeniden derleme → 5/5 yeşil. Tur durduruldu, değişen tüm dosyalar `touch`'lanıp sln temiz derlendi, API yeniden başlatıldı, **tam süit ve tarayıcı turu baştan koşuldu** (aşağıdaki sayılar ikinci koşumundur). Öneri (orkestratör): mutasyon betiği razor geri alımından sonra `touch` etsin.

### Tur 2 testleri
- Yeni: KR15 (DB, kuruş yarım sınırı + ürün satırı), KR25, KR26 (DB, gün sonu uyarıları); KR20/KR21/KR22'ye kart alt metni ve ayraç iddiaları eklendi. KarRapor + KARRAPORSinir filtreli: **39/39 geçti**. İlgili geniş küme (KarRapor|KARRAPORSinir|UX4dServis|ParaDemo2|PARADEMO2|TP1_03|S34RaporBugun|GorunumSubesi|ParaDemoTestleri|Yasal4Http|ZoBelge|ZOBELGE|UX1C3|Toplu4): 581 koşum, ilk koşumda yalnız kendi yeni 2 testim kırmızıydı (KR15'in 1 kuruş tutarlılık iddiası, KR26'nın tutar biçimi) → düzeltildi.
- **Tam süit (tur 2, son kod, trx `/private/tmp/kuyumcu-serit/trx-pKAR-RAPOR-t2b`):** toplam **9362** · geçti **9358** · başarısız **2** · atlandı 2. Kırmızılar: `ParaDemoDbTestleri.PD1_toplam_bakiye_sayilirsa_kart_havale_kadar_FAZLA_cikar_gerekce_ister` ve `Dogrulayici.PARADEMOSinirDbTestleri.PDS_kurus_0_005_fark_bir_kurusa_yuvarlanir_gerekce_ister` — ikisi de `"800,00 TL fark"` / `"0,01 TL fark"` bekliyor, sunucu "₺" basıyor; metin `Kuyumcu.Api/Modules/Kasa/KasaModulu.cs`'te (dosya 15:02'de başka şerit tarafından değiştirildi — PARA-DEMO-2 t2 "Kasalar" alanı). Bu paket KasaModulu'ne dokunmadı → **bu paketin yeni kırmızısı 0**. Tur 1'deki iki yabancı kırmızı (PATCH t9, PARADEMO2 DG_has) artık yeşil.
- Build: `dotnet build Kuyumcu.sln` **0 hata**; 2 uyarı başka şeritlerin test dosyalarında (SUBEKAPSAMASinirTestleri.cs:49 CS8601, KesifP2Testleri.cs:455 xUnit2029) — tur 1 ile aynı.
- denetim.sh: tek ✗ `P-30 DEMO_INSAN_SURESI (BK-45)` (izinli istisna); diğer tüm sayaçlar ✓.

### Tur 2 mutasyonları (yedek `ham/KAR-RAPOR-olcum/mut-yedek/tur2/`, her biri geri kopyalandı, `cmp` AYNI)

| Kod | Değişiklik | Kırmızı testler |
|---|---|---|
| MKR-14 | `R()` içinden `MidpointRounding.AwayFromZero` silindi (ToEven) | KR15, KRS03 |
| MKR-15 | ürün satırı kârı `Math.Round(bilinenHasilat - maliyet, 2)` (ToEven) | KR15 |
| MKR-16 | gün sonu bastırma kaldırıldı (`if (fark != 0m)`) | KR25 |
| MKR-17 | fark uyarısı hep bastırılıyor (`&& false`) | KR26 |
| MKR-18 | `KarKismi => false` (kartta kısmi notu yok) | KR21 |

### Tur 2 canlı doğrulama (API 5160, yanıtlar okundu; bağımsız SQL `ham/KAR-RAPOR-olcum/t2-sql-C.txt`, `t2-sql-D.txt`)

Kiracı C `PAKETKAR-RAPOR-TEST-C` (01M3Y7P99N3XF0KBFZY07BR9PM), D `PAKETKAR-RAPOR-TEST-D` (01M3Y7Q5YXH865N607KMDK2WJ4).

| Senaryo | Beklenen | Gerçek (HTTP 200) | SQL |
|---|---|---|---|
| C-1: alış 3×1.000,37; satış 1.500,11+1.600,22+1.700,33 | kâr 1.799,55 TAM | hasılat 4800.66 · maliyet 3001.11 · **kâr 1799.55** · TAM · eksik 0 | KR-X 3 satış, maliyet 3001.11, null 0 |
| C-2: + maliyetsiz açılış stoğundan 2.000 | kâr bilinenlerden + eksik satırı | hasılat 6800.66 · **kâr 1799.55** · KISMI · eksik **1 / 2000.0** · bilinenHasılat 4800.66 | KR-Y 1 satış, null 1, eksik 2000.00 |
| C-3: + alış 8×**1.000,875** (HTTP 200), satış 1.501 | ürün satırı 500,13 (AwayFromZero) | ürün KR-Z: hasılat 1501.0 · maliyet 1000.88 · **kâr 500.13** TAM; dönem kârı **2299.68** KISMI | KR-Z maliyet_kesin 1000.875; bilinen_kar_kesin **2299.675** |
| C aylık | kâr ≠ satış | satış 8301.66 · kâr 2299.68 · KISMI · eksik 1/2000 · KAR_ZARAR_SERVISI | — |
| C gün sonu | tek uyarı | `uyarilar` = ["5 satış faturalandırılmamış (toplam 8.301,66 ₺)"], fark 8301.66 | — |
| D: yalnız maliyetsiz 2 satış (2.500 + 1.500,50) | kâr gösterilmez | hasılat 4000.5 · **karTl None · karHas None · dönemKârı None** · HESAPLANAMADI · eksik 2/4000.5; aylık karKaynagi HESAPLANAMADI; gün sonu tek uyarı | 2 satış 4000.50, null 2, bilinen 0 |

Ham yanıtlar: `ham/KAR-RAPOR-olcum/t2-kz-C-1-tam.json, t2-kz-C-2-kismi.json, t2-kz-C-3-kurus.json, t2-kz-D-4-hicbiri.json, t2-urun-C.json, t2-aylik-C.json, t2-aylik-D.json, t2-mutabakat-C.json, t2-mutabakat-D.json`.
API (PID) ve vekil durduruldu; `lsof` 5160/5161/9705 boş. Kiracılar `zsh kiraci-temizle.sh <id> --uygula` ile silindi: `PAKETKAR%` = 0, iki kiracının `hareketler` satırı 0. tenants: başlangıç 16 → son 15 (fark başka şeritlerin silmesi; diğer PAKET* kiracıları — Q10-4, T-S2, SKB, TOPLU-3, SKB-HAKEM — yerinde, dokunulmadı).

### Tur 2 gerçek tarayıcı ölçümü (başsız Chrome CDP 9705, vekil 5161, kiracı C = KISMI durum)
`/raporlar` (6 sekme), `/raporlar/kar-zarar` (5 sekme), `/` (Panel) × 1440/1280/375 × BASİT/PRO × açık/koyu: 36 sayfa + 78 sekme + 140 katman; **hata 0, geçersiz 0**. 114 sayfa+sekme kaydında: gizli-görünen 0 · taşan 0 · kırık 0 · binen 0 · yatay kaydırma 0 · TRY kalıbı 0 · kırpılan 0 · kesik değer 0 · etiket binmesi 0 · hata bandı 0. Bulunanlar tur 1 ile aynı sınıf, ÖNCEDEN VAR: K12 `#rp-yil/#rp-ay` ezik (1280/1440, TOPLU-5), K4 "tren" (Rapor merkezi 375, Gün sonu — gün sonu bu turda bir uyarı kısaldı: 375'te 3x→2.7x). Tablo `ham/KAR-RAPOR-olcum/t2-olcum-tablo.md`, ham `t2-olcum-1.jsonl`, `t2-analiz.txt`, 155 görüntü `tur2-foto/`.
Read ile bakılan görüntüler: `raporlar__PRO_aydinlik_1440.png` (KÂR 2.299,68 ₺ + kart içinde "Kâr yalnız maliyeti bilinen satışlardan hesaplandı." + "…kâra katılmadı · Ayrıntı →"), `raporlar__PRO_aydinlik_1440__sekme5.png` (gün sonu TEK uyarı), `raporlar__PRO_karanlik_375.png` (koyu, dar: kart alt metni sığıyor), `kok__BASIT_karanlik_1280.png` (Panel "Kâr 2.299,68 ₺ · 1 satışın maliyeti eksik"), `raporlar_kar_zarar__BASIT_aydinlik_1280.png` (Kâr/Zarar KISMI kutusu).

## 1. Kök neden (dosya:satır, ölçülmüş)

| Bulgu | Kök neden | Kanıt |
|---|---|---|
| Rapor merkezi PRO "KÂR" = aylık satış | **(b) hesap tanımı hatası** — `Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` `Olc` içinde eski satır `var karTl = hasilatTl - maliyetTl;` (orijinal dosyada ~l.355). `hasilatTl` TÜM satışları, `maliyetTl` yalnız `BirimMaliyetTl` dolu parti satırlarını topluyordu → maliyeti bilinmeyen satış kâra **0 maliyetle** giriyordu. Ayrıca parti defteri geride kalan (satırı hiç yazılmamış) adet `maliyetiBilinmeyenAdet`'e bile girmiyordu (`MaliyetMotoru.PartiTuket` l.157: `kalanIhtiyac` için satır yazılmaz). Rapor merkezi bu değeri `RaporModulu.cs` l.~295 (`karZarar.Donem` → `karTl`) ile aynen basıyordu. | Kiracı B (demo tohumu): 9 satış, 9 tüketim satırının 9'u `birim_maliyet_tl IS NULL` (SQL), eski formülle kâr = 98.960,50 = satış. |
| **(a) tohumda maliyet yok** | `betikler/demo-tohumla.js` l.535-538: ürünler `acilisStogu` ile açılıyor → `UrunModulu.StokSayimGuncelle` (l.1040) `SAYIM_FAZLA` partisi **maliyetsiz** açar; demo satışları FIFO ile bu partileri tüketir. | SQL: `stok_partileri` SAYIM_FAZLA 12/12 NULL maliyet, ALIS 1. |
| Panel "Kâr: maliyet eksik" | Panel doğru davranıyordu (rakam basmıyordu) ama sebep (a)+(b): sunucu kârı satışa eşit döndürüyordu, ekran yalnız "eksik" diyebiliyordu. | Panel.razor eski l.431-438. |
| Gün sonu PRO iki sarı uyarı | `Raporlar.razor` gün sonu sekmesi her `m.Uyarilar` öğesi için ayrı `.uyari-kutu` çiziyordu (aynı 98.960,50 ₺ tutarı iki kutuda). | Görüntü sekme5. |
| Stok yaşı ham alan adları | `StokYaslandirmaRaporu.cs` l.259-261 sunucu uyarı metni ters tırnaklı `altinHasGram`, `madenDagilimi` içerir; `Raporlar.razor` `@foreach (var uy in yaslandirma.Uyarilar)` bunu ekrana basıyordu. | bUnit KR23 fikstürü. |

## 2. Yapılanlar

**Sunucu (KarZararServisi.cs — kâr/zarar motoru; RaporModulu.cs yalnız aylık kâr bölümü):**
- `MaliyetiBilinenler(...)`: bir satış/iade ancak (1) en az bir parti satırı varsa, (2) tüm satırlarda birim TL maliyet doluysa, (3) satırlar adedi tamamen karşılıyorsa "bilinen" sayılır. Yarım bilinen satış TAMAMEN eksiktir (fail-closed).
- `karTl = bilinenHasılat − bilinenMaliyet`; hiçbir satış bilinmiyorsa `karTl/karHas/donemKariTl = null`. Yeni alanlar: `karDurumu` (VERI_YOK/TAM/KISMI/HESAPLANAMADI), `bilinenHasilatTl`, `maliyetiEksikSatisAdedi`, `maliyetiEksikIadeAdedi`, `maliyetiEksikTutarTl`. `karMarjiYuzde` bilinen hasılat üzerinden. Satırsız (parti geride kalan) adet artık `maliyetiBilinmeyenAdet`'e girer.
- Kâr köprüsü: eksik varsa `MALIYETI_EKSIK_SATIS` kalemi (−tutar); hesaplanamayınca BRUT_KAR/DONEM_KARI `null`. İşaretli kalem toplamı = dönem kârı (KR02).
- Ürün bazlı (`/raporlar/kar-zarar/urun`) aynı kural; kârı hesaplanamayan ürün sona.
- Aylık özet (`/raporlar/aylik`): `karDurumu`, `maliyetiEksikSatisAdedi`, `maliyetiEksikTutarTl` aynı kaynaktan; `karKaynagi` kâr null ise `HESAPLANAMADI`.
- Uyarı metni: "kâr olduğundan yüksek görünebilir" yerine "bu satışlar kâra KATILMADI".

**Ekran:**
- Raporlar.razor: kâr kartı null → "Hesaplanamadı" (satış tutarı basılmaz); altında `#rp-kar-eksik` "Maliyeti eksik N satış, X ₺ — kâra katılmadı · Ayrıntı →" (/raporlar/kar-zarar bağlantısı, UX-S23). Stok yaşı: sunucu uyarıları basılmaz; `#sy-uyari-maden`, `#sy-uyari-devir` sözlükten, veriden türetilir. Gün sonu: uyarılar tek `#mut-uyarilar` kutusunda (2+ ise "Dikkat edilecekler" başlığı).
- KarZarar.razor: Kâr / Dönem kârı / Has kârı null → "Hesaplanamadı"; `#kz-maliyet-eksik` kutusu (adet+tutar + nasıl düzeltilir); köprüde yeni kalem sözlükten; ürün satırı kârı null → "Hesaplanamadı"; grafikte hesaplanamayan kâr çubuğu 0 olarak ÇİZİLMEZ; ham `uyari` eksik kutusu varken ikinci kez yazılmaz.
- Panel.razor (yalnız kâr alanı): HESAPLANAMADI → "Kâr hesaplanamadı — maliyet girilmemiş"; KISMI → "Kâr X · N satışın maliyeti eksik"; TAM → "Kâr X"; eski şekil yanıt (eksik alanı yok + maliyetsiz adet) → eski "Kâr: maliyet eksik" (fail-closed korundu).
- Sözlük: `Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs` (TR+EN konumsal record; HUB'a anahtar eklenmedi).
- Sözleşmeler: `PanelKarOzeti.KarTl` → `decimal?` (+3 isteğe bağlı alan), `AylikYanit` +3 isteğe bağlı alan, `KarKoprusuSatiri.TutarTl`, `KarZararOnceki.KarTl/DonemKariTl`, `UrunSatiri.KarTl/KarHas` → nullable (+3 alan). Eski alan silinmedi/yeniden adlandırılmadı.

## 3. Sapmalar ve gerekçeleri
- **KarZararServisi.cs** "dosya alanı"nda adıyla yok; kâr hesabının TEK kaynağı burası (RaporModulu kârı bu servisten okur — UX-4d md.21). Görev "Kar hesabı kök nedeni + düzeltme" dediği için düzeltme kaynağında yapıldı; RaporModulu'nde yalnız aylık kâr bloğu değişti.
- Sözleşme dosyaları (Contracts/Sayfalar/PanelSozlesmeleri.cs — yalnız `PanelKarOzeti`, RaporlarSozlesmeleri.cs — yalnız `AylikYanit` sonu, KarZararSozlesmeleri.cs — yalnız `UrunSatiri`) ve Web/Servisler/KarZararModelleri.cs: null kâr JSON'unu `decimal`'a çözemezdi (ekran hata bandına düşerdi); zorunlu ve en dar değişiklik.
- `UX4dServisTestleri.M21_Aylik_kar_...` testi eski fail-open davranışı (partisiz satışta kâr = 80.000 = satış, `karKaynagi=KAR_ZARAR_SERVISI`) mühürlüyordu; yeni doğru davranışa (null + HESAPLANAMADI + eksik 1/80.000) güncellendi, gerekçe yorumda.
- **demo-tohumla.js DEĞİŞTİRİLMEDİ**: maliyetli açılış ancak `/kurulum/acilis/prova+uygula` (prova jetonu, 20+ karakter gerekçe, mühür) hattıyla yazılabiliyor; ürün kartının `acilisStogu` alanı maliyet almıyor ve UrunModulu PATCH şeridinin (t9) alanında. Demo artık dürüstçe "Kâr hesaplanamadı — maliyet girilmemiş" gösteriyor. Demoda kâr rakamı istenirse: tohum açılış stoğunu kurulum açılış hattıyla `TlTutar` (maliyet) vererek yazmalı → açık kalan.
- Gün sonu çift uyarı: dosya Raporlar.razor (dosya alanında). Uyarıların metni (Mutabakat.cs, PARA-DEMO-2 alanı) değiştirilmedi; yalnız tek kutuda toplandı.
- Canlı doğrulama portu 5198 değil 5160 (görev İZOLASYON maddesi). Sistem uçları `appsettings.json`'da yer tutucu anahtar nedeniyle 503 verdi; kendi API örneğime bu oturumda üretilen rastgele bir test anahtarı `Sistem__Anahtari` ortam değişkeniyle verildi (dosyada chmod 600, değer hiçbir çıktıya basılmadı).

## 4. DoD sözleşmesi
Yeni varlık/kayıt türü YOK (yalnız hesaplanan rapor alanları). Rapor rakamları salt okunurdur; ekranda mevcut `DegismezIpucu` ("Rapor rakamları işlem kayıtlarından türetilir… kaynağı olan işlem kendi ekranından ters kayıtla düzeltilir") korunur; maliyeti eksik kutusu düzeltme yolunu yazar (mal alışla ya da açılış stoğu maliyetiyle girilir). Yeni uç yok → [RolGerekli] değişmedi (mevcut kar-zarar uçları SAHIP/MUDUR/MUHASEBE). Yeni düğme yok; yeni bağlantılar gerçek rotaya gider.

## 5. Testler
- Yeni: `Kuyumcu.Tests/KarRaporTestleri.cs` — 10 DB (KR01–KR10), 4 saf (KR11 teorisi 4 vaka, KR12–KR14), 5 Raporlar bUnit (KR20–KR24), 3 Kâr/Zarar bUnit (KR30–KR32), 3 Panel bUnit (KR40–KR42) = **25 test metodu / 28 koşum**. Filtreli: 31/31 geçti (KarRapor + M21 + TP1_03).
- İlgili eski kümeler (Kâr/Maliyet/Rapor/Panel/UX4d/P69/ORTAK1/ZOBELGE/…): 954 koşum; değişiklik sonrası 2 kırmızı (M21 → güncellendi, TP1_03 → Panel eski şekil dalı eklendi) → yeşil.
- **Tam süit (son kodla, trx /private/tmp/kuyumcu-serit/trx-pKAR-RAPOR):** toplam 9334 · geçti 9330 · başarısız 2 · atlanan 2. Kırmızılar BAŞKA şeritlerin: `PatchSozlesmeKolonTavaniTestleri.PATCH_vezne_tahsilat_tutar_kolon_tavani_400…(1e30)` (500 — PATCH şeridi t9), `Dogrulayici.PARADEMO2SinirTestleri.DG_has_4_hane_0_0001_sinir_AwayFromZero_eksi_U2212` (has biçimi — PARA-DEMO-2 t2). Kâr/rapor/panel koduna dokunmuyorlar → **bu paketin yeni kırmızısı 0**.
- Build: 0 hata; 2 uyarı başka şeritlerin test dosyalarında (SUBEKAPSAMASinirTestleri.cs:49 CS8601, KesifP2Testleri.cs:455 xUnit2029). Kendi xUnit2031 uyarım düzeltildi.

## 6. Mutasyon tablosu (yedek: ham/KAR-RAPOR-olcum/mut-yedek, her biri geri kopyalandı, cmp AYNI)

| Kod | Değişiklik | Kırmızı testler |
|---|---|---|
| MKR-1 | MaliyetiBilinenler null-maliyet kontrolü silindi | KR12, KR04 |
| MKR-2 | adet karşılama kontrolü silindi | KR12, KR05 |
| MKR-3 | `bilinenHasilatTl - maliyetTl` → `hasilatTl - maliyetTl` (eski hata) | KR02, KR08 |
| MKR-4 | KarDurumuBelirle HESAPLANAMADI → KISMI | KR11(0,4), KR03, KR04, KR05, KR10, M21 |
| MKR-5 | satırsız adet maliyetsiz sayılmıyor | KR02 |
| MKR-6 | köprüde MALIYETI_EKSIK_SATIS kalemi silindi | KR02 |
| MKR-7 | ürün satırı kârı tüm hasılattan | KR09 |
| MKR-8 | aylık özette eksik adet aktarılmıyor | KR08, M21 |
| MKR-9 | Rapor merkezi kâr null → satış tutarı | KR20 |
| MKR-10 | Panel HESAPLANAMADI dalı kapalı | KR40 |
| MKR-11 | Stok yaşı sunucu metnini basıyor | KR23 |
| MKR-12 | Gün sonu her uyarı ayrı kutu | KR24 |
| MKR-13 | KarZarar `TlYaDa(null)` → 0 ₺ | KR30, KR31, KR32 |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 hata.

## 7. denetim.sh
Tek ✗: `P-30: DEMO_INSAN_SURESI (BK-45)` (görevde izinli istisna, kurucu kalemi). Diğer tüm sayaçlar ✓.

## 8. Canlı doğrulama (API 5160, yanıtlar okundu + bağımsız SQL)

Kiracı A `PAKETKAR-RAPOR-TEST-A` (01M3Y4G8Q6D0383WW81BWNR1A4), B `PAKETKAR-RAPOR-TEST-B Kuyumculuk` (01M3Y4GWD0FXZ32WBNHRHZY8NZ, demo-tohumla --prova-kiraci).

| Senaryo | Beklenen | Gerçek (GET /raporlar/kar-zarar, HTTP 200) | SQL |
|---|---|---|---|
| 1. Alış 3×1.000,37; satış 1.500,11+1.600,22+1.700,33 | kâr 1.799,55, TAM | hasilat 4800.66 · maliyet 3001.11 · **kar 1799.55** · TAM · eksik 0 | bilinen_kar = 1799.55; 3 satış 1 satır, null 0, maliyet 1000.37 |
| 2. + açılış stoğundan (maliyetsiz) 1 satış 2.000 | kâr yalnız bilinenlerden + eksik satırı | hasilat 6800.66 · **kar 1799.55** · KISMI · eksik **1 / 2000.0** · bilinmeyenAdet 1 | eksik_tutar 2000.00 (1 satış, null maliyet) |
| 2b. Aylık özet A | kâr ≠ satış | satis.toplamTl 6800.66 · karTl 1799.55 · KISMI · eksik 1/2000 | — |
| 2c. Ürün bazlı A | KR-X TAM, KR-Y hesaplanamadı | KR-X kar 1799.55 TAM; KR-Y kar None HESAPLANAMADI eksik 2000 | — |
| 3. Tümü maliyetsiz (demo B) | kâr gösterilmez | hasilat 98960.5 · **karTl None · karHas None · donemKari None** · HESAPLANAMADI · eksik 9/98960.5; aylık karKaynagi HESAPLANAMADI | 9 satış toplam 98960.50; 9 tüketim satırının 9'u NULL maliyet; partiler SAYIM_FAZLA 12 NULL |

Ham yanıtlar: `ham/KAR-RAPOR-olcum/kz-1-tam.json, kz-2-kismi.json, kz-3-hicbiri.json, aylik-a.json`.

## 9. Gerçek tarayıcı ölçümü (başsız Chrome, tur.mjs + sikisma ölçer, kiracı B)
Rotalar `/raporlar` (6 sekme), `/raporlar/kar-zarar` (5 sekme), `/` (Panel) × 1440/1280/375 × BASİT/PRO × açık/koyu = 36 sayfa + 78 sekme + 140 katman; geçersiz 0, hata 0.
TOPLAM (114 sayfa+sekme kaydı): gizli-görünen 0 · taşan 0 · kırık 0 · binen 0 · yatay kaydırma 0 · TRY kalıbı 0 · kırpılan 0 · kesik değer 0 · etiket binmesi 0 · hata bandı 0. Tablo: `ham/KAR-RAPOR-olcum/olcum-tablo.md`, ham: `olcum-1.jsonl`, `analiz.txt`, 155 görüntü `foto/`.
Read ile bakılan görüntüler: raporlar PRO açık 1440/375, koyu 1440 (Kâr "Hesaplanamadı" + eksik satırı); sekme4 Stok yaşı (sade metin, ham alan adı yok); sekme5 Gün sonu (tek "Dikkat edilecekler" kutusu); kar-zarar PRO açık 1440, koyu 375; Panel BASİT açık 1280, koyu 375 ("Kâr hesaplanamadı — maliyet girilmemiş").
Ölçerin bulduğu ÖNCEDEN VAR olan, bu paketin ürünü olmayan: K12 `#rp-yil/#rp-ay` seçici ezik (1280/1440 — TOPLU-5 kuyruğunda kayıtlı DÜŞÜK), K4 "tren" sayfa boyu (Rapor merkezi 375, Gün sonu). KISMI hâlin ekranı bUnit (KR21, KR41) ile sınandı; tarayıcı turu yalnız B (HESAPLANAMADI) kiracısında koşuldu.

## 10. Değişen dosyalar
- dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs
- dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs (aylık kâr bloğu; tur 2: gün sonu fark uyarısı bastırma, l.~609)
- dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs (yeni)
- dotnet/Kuyumcu.Contracts/Sayfalar/PanelSozlesmeleri.cs (PanelKarOzeti)
- dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs (AylikYanit)
- dotnet/Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs (UrunSatiri)
- dotnet/Kuyumcu.Web/Servisler/KarZararModelleri.cs
- dotnet/Kuyumcu.Web/Pages/KarZarar.razor
- dotnet/Kuyumcu.Web/Pages/Raporlar.razor (kâr kartı, stok yaşı uyarıları, gün sonu uyarı kutusu)
- dotnet/Kuyumcu.Web/Pages/Panel.razor (yalnız kâr alanı)
- dotnet/Kuyumcu.Tests/KarRaporTestleri.cs (yeni)
- dotnet/Kuyumcu.Tests/UX4dServisTestleri.cs (M21 beklentisi)

Dokunulan sayfalar (TEK DOKUNUŞ kaydı): Raporlar.razor, KarZarar.razor, Panel.razor.

## 11. Koşul karşılama
| Koşul | Durum |
|---|---|
| Kök neden dosya:satır | ✓ §1 |
| 3 bilinen satış → kâr kuruşu kuruşuna | ✓ canlı 1799.55 = SQL |
| 1 maliyetsiz → kâr bilinenlerden + eksik satırı | ✓ canlı KISMI 1/2000 |
| Tümü maliyetsiz → kâr gösterilmez | ✓ canlı null, ekran "Hesaplanamadı" |
| Panel kâr alanı | ✓ |
| Gün sonu çift uyarı | ✓ tek kutu |
| Stok yaşı etiketleri | ✓ |
| Mutasyon her kural ≥1 kırmızı, cmp AYNI | ✓ 13/13 |
| Build 0 hata | ✓ (2 uyarı başka şeritte) |
| denetim TEMİZ (P-30 hariç) | ✓ |
| Tam süit 0 yeni kırmızı | ✓ |
| Tarayıcı 1440/1280/375 × BASİT/PRO × açık/koyu + Read | ✓ (tur 2: kiracı C KISMI ile yeniden) |
| Tur 2 KRİTİK: kâr AwayFromZero | ✓ 47/47 Math.Round, KR15+KRS03, canlı 500,13 |
| Tur 2: gün sonu tek uyarı | ✓ KR25/KR26, canlı C/D tek uyarı, görüntü |
| Tur 2: kısmi kâr kartta işaretli + ayraç | ✓ KR21, görüntü |
| Test kiracısı silindi | ✓ A ve B 0 satır; tenants 15 → 16 (fark başka şeritlerin o sırada açtığı kiracı: PAKETPARA-DEMO-2-TEST Dogrulayici vb.; PAKETKAR% = 0) |

## 12. Açık kalanlar
- **(tur 2, Bertan/orkestratör kararı)** Kâr KESİN değerden yuvarlanıyor (500,125 → 500,13), hasılat ve maliyet ayrı ayrı yuvarlanıyor (1.000,875 → 1.000,88) → ekranda hasılat − maliyet ile kâr arasında en çok **1 kuruş** görünür fark olabilir (canlı C: 6.301,66 − 4.001,99 = 2.299,67 ≠ 2.299,68). Hasımın KRS03'ü kesin-değer kuralını mühürlüyor; "bileşenlerden hesapla" kuralı KRS03 ile çelişir. Tek kural kararı verilene dek mevcut hâl korunur; KR15 farkı ≤ 0,01 ile sınırlar.
- **(tur 2)** Tam süitteki yabancı kırmızılar: `ParaDemoDbTestleri.PD1_toplam_bakiye_…`, `PARADEMOSinirDbTestleri.PDS_kurus_0_005_…` — KasaModulu "TL"→"₺" metni (PARA-DEMO-2 t2).
- **(tur 2)** 5199 inceleme sunucusu dinlemiyor (hasım gözlemi; bu paket dokunmadı) — kurucu/orkestratör.
- **(tur 2)** Mutasyon geri alımından sonra Razor artımlı derleme bayat dll bırakabiliyor — mutasyon betiği geri alımdan sonra `touch` etmeli.
- Demo tohumu maliyetsiz açılış stoğu yazıyor → demoda kâr hep "hesaplanamadı". Öneri: demo-tohumla açılış stoğunu `/kurulum/acilis` hattıyla `TlTutar` (maliyet) vererek yazsın ya da ürün kartı açılış stoğuna isteğe bağlı birim maliyet eklensin (UrunModulu — t9 PATCH şeridi bitince).
- Raporlar PRO yıl/ay seçici ezik (K12) — TOPLU-5.
- (tur 1) Tam süitteki başka şerit kırmızıları PatchSozlesmeKolonTavaniTestleri, PARADEMO2SinirTestleri.DG_has… — tur 2 tam süitinde artık yeşil.
- Sunucu `StokYaslandirmaRaporu` uyarı metinleri hâlâ ters tırnaklı alan adı taşıyor (API tüketicisi için); ekran artık basmıyor.

## 13. Kurucu / mali müşavir
- (tur 2) Kuruş yuvarlaması mevzuat hükmü değil, proje kuralıdır (AwayFromZero, 141 kullanım); mevzuat sabiti yazılmadı.
Mevzuat hükmü yazılmadı/değiştirilmedi. Soru (mali müşavir): açılış/sayım fazlası stoğun maliyeti bilinmediğinde dönem kârı raporunda bu satışlar için hangi değerleme esas alınmalı (açılış günü has×kur mu, son alış maliyeti mi)? Şu an tutucu: kâra katılmıyor, ayrı gösteriliyor.
