# PAKET KAR-RAPOR-2 — uygulama raporu (02.10, t1 · API 5160 / vekil 5161 / CDP 9706-9707)

> Taşkın (backend) + Ertuğ (frontend). Kaynak: `PAKET-KAR-RAPOR-uygulama-raporu.md` + tasarım hakemi/hasım kalanları.
> Devir: ilk uygulayıcı 16:49'da internet kesintisiyle düştü (kod, 13 mutasyon, tam süit, ilk tur bitmişti).
> Devralan (19:40 sonrası): ağacı gözden geçirdi, ölçüm turunun görüntülerine baktı, Ürünler tablosu başlık hizasını
> düzeltti (+ test + mutasyon M2-14), turu E ve F kiracılarında yeniden koştu, tam süiti son kodla bir kez koştu, raporu yazdı.
> COMMIT YOK. Migration YOK.

## 1. Yapılanlar (bulgu → çözüm)

| Bulgu | Çözüm | Dosya |
|---|---|---|
| [KRİTİK] Özet kartları: Hasılat − Maliyet ≠ Kâr | İlk kart artık **"Maliyeti bilinen satış"** (bilinenHasilatTl); toplam satış ve maliyeti eksik tutar aynı kartta sönük alt satırda. Has bacağı aynı kalıp (bilinenHasilatHas). Kartlar her durumda toplanabilir: E-KISMI 3.002 − 2.000,67 = 1.001,33 ₺ · 0,4506 − 0,3003 = 0,1503 g | KarZararServisi.cs, KarZararSozlesmeleri.cs (BilinenHasilatTl, BilinenHasilatHas), KarZarar.razor |
| [YÜKSEK] Bilinmeyen maliyet "0 ₺" | Sunucu HESAPLANAMADI'da `maliyetTl / brutMaliyetTl / iadeMaliyetTl / maliyetHas = null` (dönem, ürün satırı, aylık, köprü MALIYET/IADE_MALIYETI). Ekran: kart "Hesaplanamadı", ürün hücresi "Bilinmiyor", köprü "Hesaplanamadı". KISMI ürün satırında yalnız bilinen maliyet + "Bilinen hasılat" alt satırı | KarZararServisi.cs, KarZarar.razor, KarZararModelleri.cs, RaporServisi.cs |
| [YÜKSEK] Stok yaşı kova has'ına gümüş karışıyor | Kova `hasGram` = **yalnız altın has** (`altinHasGram`); kova içinde `madenKirilimi` (maden bazlı ayrı gram). Ekran: "9,1600 has · Gümüş: 83,250 g (has toplamına katılmaz)" | StokYaslandirmaRaporu.cs, KarZararSozlesmeleri.cs (MadenKirilimi), Raporlar.razor |
| [ORTA] Köprüde ham "GIDER", TUTAR başlığı hizasız | GIDER → `KR.KopruGider` ("Gider", TR+EN Sozlukler.KarRapor.cs); başlık `class="sayi"` (sağ). **Devralan ek:** aynı kusur Ürünler tablosunda da vardı (sayısal başlıklar solda, değerler sağda) → 7 sayısal başlık `class="sayi"` | KarZarar.razor, Sozlukler.KarRapor.cs |
| [ORTA] HESAPLANAMADI'da MALİYET 0 ₺ / 0,0000 g | Yukarıdaki null + "Hesaplanamadı" | aynı |
| [ORTA] 1 kuruş farkı | **Tek kural (ekip oyu Bertan + Mustafa): kâr = yuvarlanmış bileşenlerin farkı** (`karTl = R2(bilinenHasilat) − R2(maliyet)`; has R4). Gerekçe: ekranda yan yana duran kartları kullanıcı çıkarır; kesin değerden yuvarlama 1 kuruş fark bırakıyordu. KRS03 (hasım) ve KR15 bu kurala göre yeniden mühürlendi (500,125→ maliyet 1.000,88, kâr 500,12; ayrıca `hasilat − maliyet == kar` iddiası) | KarZararServisi.cs, Dogrulayici/KARRAPORSinirTestleri.cs, KarRaporTestleri.cs |
| [DÜŞÜK] Panel eski "maliyet eksik" dalı | `SatirsizAdet` artık `KarsilamaToleransi = 0,0005` toleransını MaliyetiBilinenler ile aynı uygular; Panel eski dalı yalnız `karDurumu is null` (eski şekil yanıt) iken açar | KarZararServisi.cs, Panel.razor |
| [YÜKSEK, kapsam içine alındı] BASİT'te "Bu kayıt değiştirilemez" + "Güvenilirlik: DÜŞÜK" | BASİT'te ikisi de KAPALI "Bu rakamlar nereden geliyor?" katlanırının içinde; PRO aynen (bilgi silinmedi) | KarZarar.razor |

## 2. Sapmalar ve gerekçeleri
- Dosya alanı dışında zorunlu dokunuşlar (derleme bağımlılığı, mantıksal küçük): `Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs` (MaliyetTl `decimal?`, BilinenHasilatTl, kova MadenKirilimi), `Kuyumcu.Web/Servisler/KarZararModelleri.cs` + `RaporServisi.cs` (MaliyetTl `decimal?`), `Kuyumcu.Tests/Dogrulayici/KARRAPORSinirTestleri.cs` (KRS03 — görev metni "kararı KRS03/KR15 ile mühürleyin" diyor). Eşzamanlı şeritlerin (PARA-DEMO-3, PATCH-2) dosyalarına dokunulmadı; RaporModulu.cs bu pakette DEĞİŞMEDİ.
- Kart başlıklarının büyük harfi ortak `.stat-etiket` stilinden geliyor (tüm ekranlar); tek ekranda değiştirmek ortak kalıbı bölerdi → açık kalanlara yazıldı. PRO'daki "TL Bacağı / HAS Bacağı" başlığı ve "HAS hasılatı iki farklı büyüklüktür…" metni görevde KAPSAM DIŞI işaretli; dokunulmadı.
- Stok yaşı kovasında `maliyetTl` kısmi (maliyetiBilinmeyenAdet>0) — ekranda gösterilmiyor; bulgu kapsamında değil, açık kalanlara yazıldı.

## 3. DoD sözleşmesi
Yeni varlık/kayıt türü YOK; tüm değişiklikler türetilmiş rapor alanları. Kâr-zarar/stok yaşı salt okunur raporlardır: düzeltme yolu kaynak işlemin kendi ekranında ters kayıt — DegismezIpucu metni PRO'da görünür, BASİT'te "Bu rakamlar nereden geliyor?" katlanırında. Yeni düğme yok (katlanır açıcı gerçek durum değiştirir; KR64 tıklar ve içeriği doğrular).

## 4. Testler
- Önce: KarRaporTestleri.cs 27 Fact + 1 Theory(4) ; sonra 37 Fact + 3 Theory(8) → **+14 test vakası**. Filtre `KarRapor|KARRAPOR`: **57/57 yeşil** (son kod).
- Yeni: KR43 (Panel), KR50–KR53 (DB/HTTP: KISMI toplanabilir, HESAPLANAMADI null, satırsız tolerans, stok yaşı gümüşsüz), KR60–KR66 (bUnit: KISMI kartlar, BASİT has toplanabilir, 0 basılmaz, köprü GIDER + başlık hizası + Ürünler başlık hizası, BASİT katlanır, PRO görünür, stok yaşı maden alt satırı).
- Tam süit (son kod, `trx-pKAR-RAPOR-2-t2`): **total 9510 · executed 9507 · passed 9507 · failed 0** (3 atlanan). Yeni kırmızı 0. (İlk uygulayıcının 16:43 koşumundaki tek kırmızı `PatchIkiHttpTestleri.P2_satinalma_POST_kalem_ve_MAL_KABUL_adet_0_0004_400` PATCH-2 şeridine aitti; son koşumda yeşil.)
- Build: `dotnet build Kuyumcu.sln` **0 hata**, 2 uyarı — ikisi de başka şeritlerin dosyasında (`SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455` xUnit2029); bu paketin dosyalarında uyarı 0.

## 5. Mutasyon tablosu (yedek `/private/tmp/kuyumcu-serit/KAR-RAPOR-2/mut/`, her biri geri kopyalandı, `cmp` AYNI, sonra temiz build)

| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M2-1 | kâr = R2(kesin hasılat − kesin maliyet) | KRS03_Kurus_yarim_sinirinda_AwayFromZero; KR15_Kurus_yarim_sinirinda_AwayFromZero_hasilat_eksi_maliyet_kar |
| M2-2 | dönem maliyetTl HESAPLANAMADI'da da sayı | KR51_HESAPLANAMADI_maliyet_null_sifir_degil_donem_urun_kopru; KR05_Yarisi_bilinen_satis_TAMAMEN_eksik_sayilir |
| M2-3 | ürün satırı maliyetTl null değil | KR51 |
| M2-4 | kova hasGram = tüm madenlerin gramı | KR53_Stok_yasi_kova_has_toplamina_gumus_gramı_karismaz |
| M2-5 | SatirsizAdet toleranssız | KR52_Satirsiz_adet_MaliyetiBilinenler_ile_ayni_toleransi_kullanir (tur 1'de açık çıktı → KR52 eklendi, tekrar kırmızı) |
| M2-6 | KISMI ilk kart bilinen hasılat değil | KR60_…(basit: False), KR60_…(basit: True) |
| M2-7 | maliyet kartı `?? 0m` | KR62_…(False), KR62_…(True) |
| M2-8 | köprü GIDER eşlemesi silindi | KR63_Kopru_GIDER_sozlukten_tutar_basligi_saga_hizali |
| M2-9 | BASİT katlanır kaldırıldı | KR64_BASIT_guvenilirlik_ve_degismez_kutusu_kapali_katlanirda_PRO_acik |
| M2-10 | stok yaşı maden alt satırı kaldırıldı | KR66_Stok_yasi_kova_yalniz_altin_has_gumus_alt_satirda_ayri |
| M2-11 | Panel eski dal koşulsuz | KR43_Panel_TAM_yuvarlama_artigi_bilinmeyen_adet_eski_dali_acmaz |
| M2-12 | BASİT has kartı 2 ondalık zorlaması | KR61_BASIT_has_karti_gorunen_bilesenlerden_toplanabilir |
| M2-13 | köprü MALIYET HESAPLANAMADI'da sayı | KR51 |
| M2-14 (devralan) | Ürünler "Kâr" başlığından `sayi` sınıfı kaldırıldı | KR63_Kopru_GIDER_sozlukten_tutar_basligi_saga_hizali |

14/14 mutasyon kırmızı, 14/14 `cmp` AYNI.

## 6. denetim.sh (bash, son kod)
Tek ✗: `P-30: DEMO_INSAN_SURESI (BK-45)` (görevde muaf; demo prova kronometresi, bu paketle ilgisiz). Bu paketin sayaçları ✓.

## 7. Canlı doğrulama (API 5160, yanıtlar okundu; ham: `ham/KAR-RAPOR-2-olcum/`)

| Senaryo | HTTP | API | Bağımsız SQL (`sql-bagimsiz.txt`) |
|---|---|---|---|
| E-1 TAM | 200 | hasılat 3.002 = bilinen 3.002; maliyet 2.000,67; kâr 1.001,33; has 0,4506−0,3003=0,1503 | iki satış 1.501,00 × maliyet_kesin 1.000,333 → R2 2.000,67 |
| E-2 KISMI | 200 | hasılat 5.002,55; bilinen 3.002; eksik 2.000,55; maliyet 2.000,67; kâr 1.001,33 (=3.002−2.000,67); has 0,4506−0,3003=0,1503; köprü MALIYETI_EKSIK_SATIS −2.000,55 | bilinen 3.002,00 · eksik 2.000,55 · toplam 5.002,55 · maliyet 2.000,67 · kâr 1.001,33 |
| E ürün | 200 | KR2-X TAM maliyet 2.000,67; KR2-Y HESAPLANAMADI maliyet **null**, kâr null | üçüncü satış null_maliyet=1 |
| F-3 HESAPLANAMADI | 200 | hasılat 2.500; maliyet **null**; kâr null; dönem kârı null; has maliyet null; köprü MALIYET/IADE_MALIYETI/BRUT_KAR null | bilinen 0,00 · eksik 2.500,00 |
| Aylık (E/F) | 200 | E karTl 1.001,33 KISMI · F karTl null HESAPLANAMADI | — |
| Stok yaşı (E) | 200 | G0_30: adet 5, **hasGram 9,16 = altinHasGram 9,16**, madenKirilimi ALTIN 9,16 / GUMUS 83,25 | ayar K22 2 adet 9,16 has · G925 3 adet 83,25 g → gümüş has'a katılmadı |

Not: c2.py içindeki ilk gümüş ürün denemesi HTTP 400 döndü (ZIYNET grubu + G925 ayarı; sunucu haklı olarak reddetti) → c3.py `tip: GUMUS` ile yeniden açtı, yukarıdaki stok yaşı yanıtı ondandır.
Kâr kuruş kuralı tüm ekranlarda aynı: Kâr-zarar kartları, köprü, ürün satırı, aylık kâr ve Panel aynı sunucu alanını (`karTl`) basar. **Düzeltme (tur 2, hasım bulgusu):** tur 1'de BASİT has kâr kartı istemcide `R2(hasılat) − R2(maliyet)` olarak yeniden hesaplanıyordu; bu cümle o hâliyle YANLIŞTI. Tur 2'de kural sunucuya taşındı (`karHasIkiHane`, §13) — artık hiçbir ekran kârı yeniden hesaplamaz.

## 8. Gerçek tarayıcı ölçümü (başsız Chrome, `betikler/tur/tur.mjs` + sıkışma ölçer)
Rotalar `/raporlar/kar-zarar` (5 sekme), `/raporlar` (6 sekme, stok yaşı dahil), `/` (Panel). Tur 1 = kiracı E (KISMI) tüm rotalar; tur 2 (devralan, son kod) = E kâr-zarar + F (HESAPLANAMADI) tüm rotalar.

| Rota | Genişlik | Mod × Tema | Ölçüm (tur1 E / tur2 E / tur2 F) |
|---|---|---|---|
| /raporlar/kar-zarar | 1440 · 1280 · 375 | BASİT/PRO × açık/koyu (12 kombinasyon) | taşan 0 · kırık 0 · binen 0 · yatay 0 · gizli-görünen 0 · TRY 0 · jargon 0 · elipsis 0 · hizasız 0 · kırpık 0 |
| /raporlar | 1440 · 1280 · 375 | 12 kombinasyon | hepsi 0; yalnız K12_ezik = `#rp-yil`/`#rp-ay` seçicileri (80/76 px, bu paketin dışında, önceden var) |
| / (Panel) | 1440 · 1280 · 375 | 12 kombinasyon | hepsi 0 |

Ölçüm satırları: E tur1 260, E tur2 98, F tur2 260 (`ham/KAR-RAPOR-2-olcum/olcum-*.jsonl`).
Görüntüler Read ile görüldü (`ham/KAR-RAPOR-2-olcum/foto/`): E-BASİT 1440 özet (3.002 − 2.000,67 = 1.001,33; 0,45 − 0,30 = 0,15 g), E-PRO koyu 1280 özet (4 ondalık, toplanabilir), E-BASİT koyu 375, E köprü (Gider, TUTAR sağda), E2 Ürünler (başlıklar sağa hizalı, Kar2 Künye "Bilinmiyor / Hesaplanamadı"), E stok yaşı (9,1600 has · Gümüş 83,250 g ayrı), F-BASİT 1440 (MALİYET "Hesaplanamadı", has maliyet "Hesaplanamadı"), F-PRO koyu 1280, F köprü (Maliyet/Brüt kâr/Dönem kârı "Hesaplanamadı"), F Panel BASİT ("Kâr hesaplanamadı — maliyet girilmemiş").

## 9. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin dokunduğu)
```
 M dotnet/Kuyumcu.Web/Pages/Panel.razor
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor
?? dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs        (dizin izlenmiyor)
?? dotnet/Kuyumcu.Api/Modules/Rapor/StokYaslandirmaRaporu.cs
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs
?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor
?? dotnet/Kuyumcu.Web/Servisler/KarZararModelleri.cs
?? dotnet/Kuyumcu.Web/Servisler/RaporServisi.cs
   dotnet/Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs
   dotnet/Kuyumcu.Tests/KarRaporTestleri.cs
   dotnet/Kuyumcu.Tests/Dogrulayici/KARRAPORSinirTestleri.cs
```
Dokunulan sayfalar: KarZarar.razor, Raporlar.razor (stok yaşı sekmesi), Panel.razor (yalnız kâr alanı).

## 10. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Özet kartları toplanabilir (TAM/KISMI/HESAPLANAMADI, API + SQL) | KARŞILANDI | §7, KR50/KR60/KR61 |
| Bilinmeyen değer hiçbir ekranda 0 basılmaz | KARŞILANDI | KR51/KR62, F görüntüleri |
| Stok yaşı altın has gümüşsüz (SQL) | KARŞILANDI | §7 stok yaşı, KR53/KR66 |
| Kâr kuruş kuralı tek ve tüm ekranlarda aynı | KARŞILANDI | KRS03/KR15, M2-1 |
| GIDER sözlükten, başlık hizası | KARŞILANDI | KR63, M2-8/M2-14 |
| BASİT değişmez kutusu/güvenilirlik katlanırda | KARŞILANDI | KR64/KR65, M2-9 |
| Mutasyon ≥1 kırmızı, cmp AYNI | KARŞILANDI | §5, 14/14 |
| Başsız Chrome 1440/1280/375 BASİT+PRO açık+koyu, Read | KARŞILANDI | §8 |
| build 0 hata | KARŞILANDI | 0 hata (2 uyarı başka şeritte) |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | §6 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 9507/9507 |
| Test kiracısı silindi | KARŞILANDI | E 01M3YDASBP…, F 01M3YDC0X… `kiraci-temizle.sh --uygula`; kalan satır 0; tenants 16 (17 − 2 + PARA-DEMO-3 şeridinin arada açtığı 1) |

## 11. Açık kalanlar
- Kart başlıklarının büyük harfi (ortak `.stat-etiket`, tüm ekranlar) — TOPLU kuyruğuna; tek ekranda çözülmedi.
- PRO "TL Bacağı / HAS Bacağı" başlıkları ve "HAS hasılatı iki farklı büyüklüktür…" paragrafı — görevde KAPSAM DIŞI (TOPLU kuyruğu satır 70).
- `/raporlar` yıl/ay seçicileri dar (K12_ezik 80/76 px) — önceden var, bu paketin dışı.
- Stok yaşı kova `maliyetTl` kısmi toplam (maliyetiBilinmeyenAdet>0) — ekranda gösterilmiyor; ileride gösterilirse "bilinen maliyet" etiketi gerekir.
- Başlık hizası K7 ölçeri tablo başlıklarını yakalamıyor (ürün tablosu kusuru ölçerde 0 çıkıyordu, gözle bulundu) — ölçer iyileştirmesi önerisi.

## 12. Kurucu / mali müşavir
Mevzuat hükmü içermez (rapor sunumu ve yuvarlama kuralı). Yuvarlama: kuruş AwayFromZero, kâr = yuvarlanmış bileşenlerin farkı — muhasebe pratiğiyle çelişip çelişmediği mali müşavire bilgi olarak sorulabilir (ekranda tutarlılık için seçildi).

---

## 13. TUR 2 — hasım doğrulayıcının kalan bulguları (02.10 ~20:20–20:45, devralan uygulayıcı)

Devir: önceki uygulayıcının kodu korunarak yalnız hasımın 5 bulgusu kapatıldı (BAŞTAN YAZILMADI). Başlangıçta 5160/5161/9706-9708 portlarında artık süreç yoktu; kaynakta `if (false` / `@if (true)` izi 0. Migration YOK.

| Bulgu | Çözüm | Test / kanıt |
|---|---|---|
| [KRİTİK] Ürünler KISMI satırı: "Maliyeti bilinen satış" alt satırı tutara yapışık (`5.000,55 ₺Maliyeti bilinen satış…`) | `KarZarar.razor` hasılat hücresinde `<span class="… kz-urun-bilinen">` → `<div class="… kz-urun-bilinen">` (blok öğe, kendi satırında; CSS dosyasına dokunulmadı, sabit renk/stil yok) | **KR68** (BASİT+PRO): alt satır `DIV`, metni `Maliyeti bilinen satış: 3.500,55 ₺`, hücrenin kendi metni yalnız `5.000,55 ₺`; satırda 3.500,55 − 2.000,01 = 1.500,54. Gerçek tarayıcı: kiracı K, ürün **T2-X KISMI** (aşağıda) |
| [ORTA] BASİT has kâr kartı istemcide yeniden hesaplanıyordu; rapor "hiçbir ekran yeniden hesaplamaz" diyordu | Kural sunucuya taşındı: `KarZararServisi` yeni alan **`karHasIkiHane = R2(bilinenHasilatHas) − R2(maliyetHas)`** (HESAPLANAMADI'da null). `HasKarGoster` BASİT'te bu alanı basar; alan yoksa (eski şekil yanıt) sunucunun `karHas`'ı basılır — istemci farkı ALMAZ. §7'deki yanlış cümle düzeltildi | **KR67** (sunucu alanı kasıtlı 17,41 verilir → ekranda 17,41 g; istemci 17,39 hesaplasaydı kırmızı), **KR61** (17,39), **KR50** (DB: `karHasIkiHane` = R2−R2), **KR51** (HESAPLANAMADI'da null) |
| [DÜŞÜK] Grafik: önceki dönem maliyeti null → 0 çubuğu | `CiftNokta` ortak bileşen (Grafikler.cs, dosya alanı dışı) değiştirilmedi; null değer yüksekliği 0 olan (görünmez) çubuk olarak kalır ve ölçek tabanı zaten 0 olduğundan ölçeği değiştirmez. Ayrım ekrana yazılır: önceki dönemde bilinmeyen kalemler için grafiğin altında **`#kz-grafik-onceki-bilinmiyor`**: "Karşılaştırılan dönemde bilinmiyor: Maliyet, Kâr, Dönem kârı — bu çubuk çizilmedi (sıfır değil)." (TR+EN `Sozlukler.KarRapor.cs` → `GrafikOncekiBilinmiyor`) | **KR69** (not var, üç kalem), **KR69b** (önceki biliniyorsa not yok). Canlı: geçmiş tarihli satış canlıda üretilemediği için bu not yalnız bUnit ile kanıtlı (açık kalanlar) |
| [DÜŞÜK] Stok yaşı: kova (LEFT JOIN) ile madenDagilimi (INNER JOIN) farklı küme | `StokYaslandirmaRaporu.cs` maden kırılımı sorgusu da LEFT JOIN; ürünsüz parti iki rakamda da aynı varsayımla (ayar yok → ALTIN) sayılır | **KR54** (ürünsüz parti 5 g: kova hasGram = altinHasGram = 2×has + 5; kovaların toplamı = altinHasGram) |
| [DÜŞÜK] KRS03 beklenen değeri uygulayıcı tarafından değiştirildi | Kod değişikliği yok. Kural kararı: **kâr = yuvarlanmış bileşenlerin farkı** (ekip oyu Bertan + Mustafa; gerekçe §1). Orkestratörden istek: bu kararı ve KRS03'ün 500,13 → 500,12 yeniden mühürlenmesini kapı kaydına yazması. Mali müşavir notu açık (§12) | KRS03, KR15 yeşil |

### 13.1 Ekip oyu — kâr kuruş kuralı (Bertan + Mustafa)
- **Bertan (mimari):** Tek kaynak sunucu. Ekranda yan yana duran iki sayının farkı üçüncü karttaki sayı olmak zorunda; bu yalnız "kâr = görünen bileşenlerin farkı" ile garanti edilir. Görünen hassasiyet iki tane (TL 2 hane; has PRO 4 hane / BASİT 2 hane), bu yüzden sunucu iki has alanı verir (`karHas` R4, `karHasIkiHane` R2). İstemci hesap yapmaz.
- **Mustafa (FinOps):** Kuruş farkı yuvarlamadan doğar, para kaybı değildir; kesin değerden yuvarlanmış kâr ile bileşen farkı en fazla 1 kuruş (has 0,0001 / 0,01 g) ayrışır. Fatura/defter tutarları bu rapordan üretilmez (salt okunur rapor), bu yüzden bileşen farkı kuralı muhasebe kaydını etkilemez. Mali müşavire bilgi notu (§12) açık kalır.
- Karar oybirliği: kâr = yuvarlanmış bileşenlerin farkı; her ekranda aynı.

### 13.2 Değişen dosyalar (tur 2)
`Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` (karHasIkiHane) · `Kuyumcu.Api/Modules/Rapor/StokYaslandirmaRaporu.cs` (LEFT JOIN) · `Kuyumcu.Web/Pages/KarZarar.razor` (div alt satır, HasKarGoster sunucu alanı, grafik notu) · `Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs` (GrafikOncekiBilinmiyor TR+EN) · `Kuyumcu.Tests/KarRaporTestleri.cs` (KR54, KR67, KR68×2, KR69, KR69b + KR50/KR51 iddiaları + Kismi fikstürüne karHasIkiHane). Dokunulan sayfa: yalnız KarZarar.razor (tur 1'de de açılmıştı — aynı paketin 2. turu, gerekçe: hasım bulgusu). Panel.razor / Raporlar.razor bu turda değişmedi. CSS dosyası değişmedi.

### 13.3 Testler (tur 2)
- Build: `dotnet build Kuyumcu.sln` **0 hata, 2 uyarı** (ikisi başka şeritlerin dosyasında: `SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455` xUnit2029).
- Filtre `KarRapor|KARRAPOR`: **70/70 yeşil** (tur 1 sonu 57; +6 yeni test yöntemi = 7 vaka: KR54, KR67, KR68(×2), KR69, KR69b; geri kalan fark, filtreye düşen diğer test sınıflarındaki vakalardır).
- Tam süit (son kod, `trx-pKAR-RAPOR-2-t3/_Mac_2026-10-02_20_29_28.trx`): **total 9523 · executed 9520 · passed 9520 · failed 0** (3 atlanan). Yeni kırmızı 0.

### 13.4 Mutasyonlar (tur 2) — yedek `/private/tmp/kuyumcu-serit/KAR-RAPOR-2/tur2b/mut/`, her biri geri kopyalandı, `cmp` AYNI, ardından temiz build 0 hata

| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M2-15 | alt satır `div` → `span` (yapışık hâle dönüş) | KR68_Urunler_KISMI_satir_bilinen_hasilat_ayri_blokta_tutara_yapismaz(basit: False), (basit: True) |
| M2-16 | BASİT has kârı sunucu alanı yerine `karHas` | KR61_BASIT_has_karti_gorunen_bilesenlerden_toplanabilir; KR67_BASIT_has_kari_sunucu_alanindan_istemci_yeniden_hesaplamaz |
| M2-17 | sunucu `karHasIkiHane = karHas` | KR50_KISMI_ozet_kartlari_toplanabilir_TL_ve_has_kopru_ve_urun_satiri |
| M2-18 | maden kırılımı tekrar INNER JOIN | KR54_Stok_yasi_urunsuz_parti_kova_ve_maden_dagilimi_ayni_kume |
| M2-19 | grafik notundan Maliyet kalemi silindi | KR69_Grafik_onceki_donem_bilinmeyen_maliyet_icin_bilinmiyor_notu |

5/5 kırmızı, 5/5 `cmp` AYNI. (Tur 1'in 14 mutasyonu §5'te; toplam 19.)

### 13.5 denetim.sh (son kod)
Tek ✗: `P-30: DEMO_INSAN_SURESI (BK-45)` (görevde muaf; demo prova kronometresi). Bu paketin sayaçları ✓.

### 13.6 Canlı doğrulama (API 5160, son kod; ham: `ham/KAR-RAPOR-2-olcum/tur2/`)
İki test kiracısı: **K** `PAKETKAR-RAPOR-2-TEST-T2K` (01M3YTMAWYXV6C05KCSH62J8MB) — TAM → KISMI; **L** `PAKETKAR-RAPOR-2-TEST-T2L` (01M3YTPB9N35E8RREFP8Y08GS8) — HESAPLANAMADI.

| Senaryo | HTTP | API | Bağımsız SQL (`tur2/sql-bagimsiz.txt`, hareketler + stok_parti_tuketimleri) |
|---|---|---|---|
| K-1 TAM (T2-Z alış 800,004 → satış 1.200) | 200 | hasılat 1.200 = bilinen; maliyet 800; kâr 400 (= 1.200 − 800); has 0,182 − 0,1213 = 0,0607; BASİT: 0,18 − 0,12 = **karHasIkiHane 0,06** | T2-Z bilinen 1.200, maliyet_R2 800,00, kâr 400,00 |
| K-2 KISMI (+T2-X açılış 1 maliyetsiz + alış 2 × 1.000,005; satış 1.500/1.500/2.000,55; +T2-Y açılış 2, satış 2.500) | 200 | toplam 8.700,55 · **bilinen 4.700,55** · eksik 4.000 · maliyet 2.800,01 · **kâr 1.900,54 (= 4.700,55 − 2.800,01)**; has 0,7128 − 0,4246 = 0,2882; BASİT 0,71 − 0,42 = **karHasIkiHane 0,29**; köprü MALIYETI_EKSIK_SATIS −4.000 | toplam 8.700,55 · bilinen 4.700,55 · eksik 4.000,00 · maliyet_R2 2.800,01 · kâr 1.900,54 · bilinen adet 3 / eksik 2 |
| K ürün satırları | 200 | **T2-X KISMI** hasılat 5.000,55 · bilinen 3.500,55 · maliyet 2.000,01 · kâr 1.500,54 · T2-Z TAM 1.200/800/400 · T2-Y HESAPLANAMADI maliyet **null**, kâr null | T2-X 5.000,55 / 3.500,55 / 2.000,01 / 1.500,54 · T2-Y maliyet NULL, kâr NULL · T2-Z 1.200/800/400 |
| K aylık | 200 | karTl 1.900,54 KISMI (özetle aynı kuruş) | — |
| K stok yaşı (+T2-G gümüş G925 açılış 3) | 200 | G0_30 adet 4 · **hasGram 4,58 = altinHasGram 4,58** · madenKirilimi ALTIN 4,58 / GUMUS 83,25 | K22 1 adet 4,58 g · G925 3 adet 83,25 g → gümüş has'a katılmadı |
| L-3 HESAPLANAMADI | 200 | hasılat 2.500 · bilinen 0 · **maliyet null · kâr null · dönem kârı null · has maliyet null · karHas null**; köprü MALIYET / IADE_MALIYETI / BRUT_KAR / DONEM_KARI null; aylık karTl null | 1 satış null_maliyet=1 · bilinen 0,00 · eksik 2.500,00 |

### 13.7 Gerçek tarayıcı (başsız Chrome, `betikler/tur/tur.mjs`, CDP 9708, vekil 5161, kiracı K — KISMI ürün satırı DAHİL)
Rotalar `/raporlar/kar-zarar` (5 sekme), `/raporlar` (6 sekme), `/` · genişlik 1440 · 1280 · 375 · BASİT/PRO × açık/koyu = 12 kombinasyon; 260 ölçüm satırı (`tur2/olcum-K-tur2.jsonl`), hata 0.

| Rota | Genişlik × Mod × Tema | Sonuç (analiz.py) |
|---|---|---|
| /raporlar/kar-zarar (Özet, Grafikler, Kâr köprüsü, Ürünler, Stok) | 1440/1280/375 × BASİT/PRO × açık/koyu | **temiz** — taşan 0 · binen 0 · kırık 0 · yatay 0 · gizli-görünen 0 · TRY 0 · ezik 0 · kırpık 0 |
| / (Panel) | 12 kombinasyon | **temiz** |
| /raporlar (stok yaşı sekmesi dahil) | 12 kombinasyon | yalnız önceden var olan, bu paketin dışındaki bulgular: K12 ezik `#rp-yil`/`#rp-ay` (80/76 px, 1280/1440 PRO); K4 "tren" ana sekme şeridinde 375 PRO ve **Gün sonu** sekmesinde 1280/375 PRO (Gün sonu PARA-DEMO-3 şeridinin alanı) |

Görüntüler Read ile GÖRÜLDÜ (`tur2/foto/`):
- `raporlar_kar_zarar__PRO_aydinlik_1280__sekme3.png`: Tur2 Bilezik satırı — "5.000,55 ₺" üstte, altında ayrı satırda küçük ve sönük "Maliyeti bilinen satış: 3.500,55 ₺"; Maliyet 2.000,01 ₺, Kâr 1.500,54 ₺; Künye "Bilinmiyor / Hesaplanamadı". Yapışma YOK.
- `raporlar_kar_zarar__BASIT_karanlik_1440__sekme3.png`: aynı satır koyu temada iki satır, başlıklar sağa hizalı.
- `raporlar_kar_zarar__BASIT_karanlik_375__sekme3.png`: tablo kendi kaydırma kabında; sayfada yatay kaydırma yok (ölçer 0); alt satır kabın içinde sağa kayınca görünür.
- `raporlar_kar_zarar__BASIT_aydinlik_1280.png`: Özet — Maliyeti bilinen satış 4.700,55 ₺ − Maliyet 2.800,01 ₺ = Kâr 1.900,54 ₺; has 0,71 g − 0,42 g = 0,29 g (sunucunun karHasIkiHane'si); sarı satır "Maliyeti eksik 2 satış, 4.000 ₺ — kâra katılmadı".

### 13.8 Test kiracıları
K ve L `zsh betikler/kiraci-temizle.sh <id> --uygula` ile silindi; iki kimlikle `tenants`, `hareketler`, `stok_partileri` satırı **0**. tenants: başlangıç **16** → (+2 açıldı) → son **15**. Fark −1 benim değil: koşum sırasında eşzamanlı bir şerit kendi kiracısını sildi (silme betiği yalnız verilen kimliği siler — satır 42–43). QA-TEST-* kiracılarına dokunulmadı. API (PID 19290 sonrası yeniden başlatılan süreç), vekil ve Chrome durduruldu; 5160/5161/9708 dinleyen yok. Fikstür dosyaları `printf ''` ile boşaltıldı.

### 13.9 Açık kalanlar (tur 2)
- Grafik "önceki dönem bilinmiyor" notu yalnız bUnit ile kanıtlı (KR69/KR69b); canlıda geçmiş tarihli maliyetsiz satış üretmek bu paketin uçlarıyla mümkün değildi. Ortak `CiftNokta` tipine null desteği (gerçekten "çizilmeyen" çubuk + işaret) ortak grafik bileşeninin işidir → TOPLU kuyruğuna öneri.
- 375 px'te ürün tablosu kendi kabında yatay kaydırılıyor (önceden var olan tablo kalıbı); KISMI alt satırı kaydırmadan görünmez. Dar ekranda kart düzeni TOPLU kuyruğuna öneri.
- `/raporlar` K12 (yıl/ay seçici) ve K4 (sekme şeridi 375, Gün sonu) — bu paketin dışı, önceden var.
- KRS03/KR15 kural kararı orkestratör kapı kaydına yazılmalı (orkestratör işi). Mali müşavir notu (§12) açık.
