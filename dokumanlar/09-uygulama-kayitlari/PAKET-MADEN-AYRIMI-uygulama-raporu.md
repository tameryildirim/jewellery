# PAKET MADEN-AYRIMI — uygulama raporu (02.10, t1 · API 5160 / vekil 5161 / CDP 9707-9708)

> Taşkın (backend) + Ertuğ (frontend). Kaynak: `PAKET-KAR-RAPOR-2-uygulama-raporu.md` + hasım t2 / tasarım t2 bulguları.
> COMMIT YOK. Migration YOK (şema değişmedi; yalnız türetilmiş rapor alanları).
>
> **TUR 2 (03.10, 00:32–01:20)** — doğrulama turunun 3 KRİTİK + 2 DÜŞÜK bulgusu kapatıldı; ayrıntı **§ 14**. §3'teki "has binliksiz" sapması ve §12'deki "binlik gruplaması / hub etiketleri" açıkları TUR 2'de KAPANDI (§14).

## 1. Yapılanlar (bulgu → çözüm)

| Bulgu | Çözüm | Dosya |
|---|---|---|
| [YÜKSEK] Kâr-zarar › Stok "STOK HAS GRAMI" gümüşü topluyordu (H: 66,60 g; K: 141,96 g) | `KarZararServisi.StokDegerlemesi`: parti → ürün LEFT JOIN, maden ayardan (Stok yaşı ile AYNI kural; ürünsüz parti → ALTIN). `toplamHasGram` = **yalnız altın** (+ aynı değer `altinHasGram`), yeni `madenKirilimi[] {maden, madenAdi, adet, toplamGram (saf), fizikselGram (tartı; ürünü yoksa null)}`. Kart etiketi **"Altın (has)"**, gümüş kartın alt satırında "Gümüş: 120,00 g (saf 111,00 g)" | KarZararServisi.cs, KarZararSozlesmeleri.cs (StokVeri + StokMadenSatiri), KarZarar.razor |
| [YÜKSEK] Maliyeti hiç bilinmeyen stok "STOK MALİYETİ 0 ₺" | Sunucu: stokta mal var ve `maliyetliAdet=0` → `stokMaliyetiTl/Has = null` (`StokMaliyetiBilinmiyor`), kısmen biliniyorsa `stokMaliyetiKismi=true`. Ekran: null → **"Bilinmiyor"** + alt satır "Stoktaki malların alış maliyeti girilmemiş"; kısmi → etiket **"Bilinen stok maliyeti"** + sade not "Maliyeti bilinmeyen N adet bu tutara katılmadı." (sunucunun ham "bir kısım … İÇERMEZ" metni artık basılmaz; canlı görüntüde maliyet hiç bilinmezken "bir kısım" diyerek kartla çelişiyordu) | aynı |
| [YÜKSEK] BASİT Stok sekmesinde "Güvenilirlik: DÜŞÜK" açık | BASİT'te kapalı **"Bu rakamlar nereden geliyor?"** katlanırında (`#kz-stok-nereden`, rozet id `kz-stok-guven` aynı); PRO aynen | KarZarar.razor |
| [YÜKSEK] PRO'da "TL Bacağı / HAS Bacağı" + yapışık teknik paragraf | Başlıklar iki modda **"Para kârı" / "Altın kârı (has gram)"** (OrtakGorunum sözlüğü, yeni metin yok). PRO paragrafı ⓘ arkasında: "Has satış ile defterdeki has neden farklı? ⓘ" (`#kz-has-tanim` id'si korundu, details içinde) | KarZarar.razor, Sozlukler.KarRapor.cs |
| [ORTA] Köprü › Takas: iç içe kart, birimsiz "0", açık "DEĞİL … KATILMAZ" paragrafı, yinelenen İşçilik kartı | Dış `.kart` kaldırıldı (tek kart); alt satır **"n takas"**; gerekçe ⓘ arkasında (`#kz-takas-notu`); BASİT'te takas adedi 0 ve fark 0 ise kart çizilmez; köprüde `ISCILIK` satırı varsa ayrı İşçilik kartı çizilmez | KarZarar.razor, Sozlukler.KarRapor.cs |
| [ORTA] Has kârı BASİT 19,16 g ↔ PRO 19,1520 g; birim "g" ↔ "has" | **Tek kural (ekip kararı 5):** has her iki modda `Bicim.Has` (4 hane, "has") — BASİT 2 hane yuvarlaması kaldırıldı; kâr kartı sunucunun `karHas`'ı (istemci hesaplamaz). `karHasIkiHane` sunucuda duruyor ama ekran okumaz (alan silinmedi — şekil sözleşmesi) | KarZarar.razor |
| [ORTA] BASİT'te "Defterdeki altın gramı 159,37 g" üç kartlı köprünün yanında | BASİT'te ızgaradan çıktı; kapalı "Bu rakamlar nereden geliyor?" içinde **"Satılan malın gerçek gramı"** kutusu: altın önce, her maden kendi satırında ve biriminde + açıklama. PRO'da kart kalır, birimi madenine göre ("9,6180 has" / "saf 27,75 g") | KarZarar.razor |
| [ORTA] Ürünler has kârı 0,76 ↔ Özet 0,75 | Ürün satırı has kârı da `Bicim.Has` (4 hane) — özet ile aynı kural/rakam (canlı: özet 1,1336 has = ürün satırı 1,1336 has) | KarZarar.razor |
| [DÜŞÜK] Eksik yalnız iadeyse eksik kutusu yok; eksi tutar "−Y" | `#kz-maliyet-eksik` koşuluna `MaliyetiEksikIadeAdedi`; iade için ayrı satır `#kz-maliyet-eksik-iade`; eksik tutar < 0 ise tutar yazılmaz, yalnız adet ("Maliyeti eksik n satış — kâra katılmadı") | KarZarar.razor, Sozlukler.KarRapor.cs |
| [DÜŞÜK] Stok yaşı kova "Gümüş: 138,750 g" (saf, 3 hane) + formül satırı açık | Kova kırılımına `fizikselGram`; alt satır "Gümüş: 150,00 g (saf 138,75 g)"; devir formülü kart altından ⓘ'ye: "Devir hızı nasıl hesaplanır? ⓘ" (`#sy-devir-tanim`) | StokYaslandirmaRaporu.cs, RaporServisi.cs (DTO), Raporlar.razor |
| [ORTA, kapsam dışı işaretli → alındı] Stok değeri gümüşü "66,6000 has" | Sunucu satıra `fizikselGram` ekler (`maden` zaten vardı); ekran altın "has", diğer madenler "120,00 g (saf 111,00 g)"; reyon satırları ve ölü stok tablosu da aynı kural | RaporModulu.cs (StokDegeri), RaporlarSozlesmeleri.cs, Raporlar.razor |
| Tek biçim kuralı | `Bicim.MadenMiktari(maden, saf, fiziksel, kalıplar)`: ALTIN/bilinmeyen → `Bicim.Has`; diğer madenler 2 hane gram, kalıp sözlükten (TR "150,00 g (saf 138,75 g)" / "saf 138,75 g"; EN "… g pure") | Bicim.cs, Sozlukler.KarRapor.cs |

## 2. Rapor sunucu uçları tarama tablosu (maden ayrımı)

| Uç / alan | Durum (önce) | Durum (sonra) |
|---|---|---|
| `GET /raporlar/stok-degerlemesi` `toplamHasGram` | altın+gümüş TOPLAM (hata) | **yalnız altın** + `madenKirilimi` (saf + tartı) — DÜZELTİLDİ |
| `GET /raporlar/stok-degerlemesi` `stokMaliyetiTl/Has` | maliyetsizde 0 | **null** (+ `stokMaliyetiKismi`) — DÜZELTİLDİ |
| `GET /raporlar/stok-degeri` `toplamHas`, `madenDagilimi` | toplam yalnız altın (doğru); satır `toplamHas` ürünün kendi madeninin saf gramı | doğru; satıra `fizikselGram` eklendi; ekran birimi madenden |
| `GET /raporlar/stok-yaslandirma` kova `hasGram` | KAR-RAPOR-2'de altın-yalnız yapılmıştı | doğru; kova `madenKirilimi`'ne `fizikselGram` |
| `GET /raporlar/aylik` `netHasGramHareketi`, `madenDagilimi` | `HareketToplami.AltinHas` + maden dağılımı (doğru) | değişmedi; ekran maden dağılımı tablosunda gümüş artık "has" değil saf gram |
| `GET /raporlar/kar-zarar` `hasDefterMadenDagilimi` | maden ayrı (doğru) | değişmedi; ekran birimi madenden |
| Pozisyon (`PozisyonModulu`) | maden başına ayrı satır (doğru, kodla doğrulandı) | DOKUNULMADI |
| `GET /raporlar/pos-fatura-mutabakati` `hurda.hasGrami` | **tüm madenlerin toplamı** (RaporModulu.cs ~744) | DOKUNULMADI — Gün sonu sekmesi TOPLU-5 şeridinin alanı; açık kalanlara yazıldı |
| Panel altın pozisyonu | pozisyon ucunu okur (maden ayrı) | DOKUNULMADI |

## 3. Sapmalar ve gerekçeleri
- **Has binlik gruplaması:** ilke "binlik gruplu, 4 ondalık" diyor; `Bicim.Has` bilerek **binliksiz** (F4, kod yorumu: "nokta hem binlik hem ondalık sanılmasın") ve 6 başka paketin mühürlü testleri (BicimTestleri, KESIFP2/ZOBELGE/BELGEKALAN/EMANET1/ORTAK1 Dogrulayici) bu biçime bağlı. `Bicim.Has`'ı değiştirmek bu paketin alanı dışında ~15 kırmızı üretirdi; `KarHasGram` yorumunda karar BICIM-TEK'e bırakılmış. Bu pakette **tek kural = Bicim.Has** uygulandı (4 hane, "has", BASİT=PRO); binlik gruplama kararı açık kalanlarda.
- **Alan dışı zorunlu dokunuşlar (derleme/sözleşme bağımlılığı):** `Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs` (StokVeri alanları), `Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs` (StokUrun.Maden/FizikselGram), `Kuyumcu.Web/Servisler/RaporServisi.cs` (YaslandirmaMadeniDto.FizikselGram).
- **Eski davranışı mühürleyen testler güncellendi (gerekçe: ekip kararı 5 eski ORTAK-1 "BASİT N2" kararını değiştirir):** `KarZararEkranTestleri.cs` (P35_04, P35_05, ORTAK1_HK_bacak…, ORTAK1_HK30 ×2), `Dogrulayici/ORTAK1SinirTestleri.cs` (OK1–OK4, OK6, OK7 — niyet "sıfırdan farklı has sıfır görünmez" korunarak 4 hane "has"), `BelgeKalanTestleri.cs` (BK reyon: gümüş "83,2500 has" → "saf 83,25 g"), `KarRaporTestleri.cs` (KR61, KR66, KR67 → Theory). Her değişiklik yerinde `MADEN-AYRIMI` yorumu ile.
- **Panel.razor** (eksik-iade dalı) bu paketin dosya alanında DEĞİL → dokunulmadı, açık kalanlara.
- Raporlar.razor'da yalnız Stok değeri / Stok yaşı sekmeleri + Özet sekmesindeki maden dağılımı hücresi (tek satır, aynı ilke) değişti; Gün sonu sekmesine dokunulmadı.

## 4. DoD sözleşmesi
Yeni varlık/kayıt türü YOK; tüm değişiklikler türetilmiş, salt okunur rapor alanları. Kâr-zarar / stok değerlemesi / stok değeri / stok yaşı raporlardır: düzeltme yolu kaynak işlemin kendi ekranında ters kayıt (DegismezIpucu PRO'da görünür, BASİT'te "Bu rakamlar nereden geliyor?" katlanırında — KAR-RAPOR-2'den aynen). Yeni düğme yok (yalnız ⓘ `details` açıcıları ve mevcut `Katlanir`; MA22 katlanır tıklanınca içeriği doğrular). [RolGerekli] uçlar değişmedi.

## 5. Testler
- Yeni dosya `Kuyumcu.Tests/MadenAyrimiTestleri.cs`: **17 yöntem / 24 vaka** — DB (MA01–MA05, MA06×4), saf (MA10), bUnit Kâr-zarar (MA20×2, MA21×2, MA22, MA23×2, MA24, MA25×2, MA26, MA27), bUnit Raporlar (MA30, MA31). KR67 Fact→Theory (+1 vaka). Yeni vaka toplamı **25**.
- Filtre `MadenAyrimi` (yeni sınıflar + MaliyetTestleri içindeki eski `MadenAyrimiTestleri`): **35/35 yeşil**. Geniş filtre (KarRapor|KARRAPOR|KarZarar|Rapor|Maden|Bicim|ORTAK1|…|BelgeKalan): **1329/1329 yeşil** (son kod öncesi), son kodla MadenAyrimi 35/35.
- Build: `dotnet build Kuyumcu.sln` **0 hata, 0 uyarı** (son temiz derleme; ara derlemelerde görülen 2 uyarı başka şeritlerin dosyasında: SUBEKAPSAMASinirTestleri.cs:49 CS8601, KesifP2Testleri.cs:455 xUnit2029).
- **Tam süit (son kod)** `trx-pMADEN-AYRIMI-t3`: **toplam 9738 · geçti 9735 · başarısız 1 · atlanan 2**. Tek kırmızı `P07DayaniklilikTestleri.B10_esZamanli_5_istek_TEK_jeton_yenileme_uretir` (eşzamanlılık zamanlaması; bu paketin dosyalarıyla ilgisiz — jeton yenileme) — yalnız koşturuldu **3/3 yeşil** → kararsız, YENİ KIRMIZI 0. Ara koşum (`trx-pMADEN-AYRIMI`, tur 1 kodu): 9722/9722 yeşil.
- Not: aynı ağaçta başka şeritlerin eşzamanlı derlemesi bir koşumu bozdu (`t2`: 2945 "Could not load file or assembly NUlid/Pomelo" — bin dizini koşum sırasında silindi); geçersiz sayıldı, sessiz anda yeniden koşuldu (`t3`).

## 6. Mutasyon tablosu (yedek `/private/tmp/kuyumcu-serit/mut-yedek/MADEN-AYRIMI/`, geri alma ters tek eşleşme + `cmp`)

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M1 | stok-degerlemesi `toplamHasGram` = tüm madenlerin toplamı | MA01_Stok_degerlemesi_has_toplami_yalniz_altin_gumus_ayri_kirilimda | AYNI |
| M2 | `StokMaliyetiBilinmiyor` hep false (0 ₺ geri) | MA02_Maliyeti_hic_bilinmeyen_stok_maliyeti_null_sifir_degil; MA06(0,5,True) | AYNI |
| M3 | ekran stok maliyeti `?? 0m` | MA21 (basit True/False) | AYNI |
| M4 | BASİT stok katlanırı açık başlar | MA22_BASIT_stok_guvenilirlik_kapali_katlanirda_PRO_acik | AYNI |
| M5 | başlık tekrar "TL Bacağı" | MA23 ×2; ORTAK1_HK_bacak… ×2; P35_05 | AYNI |
| M6 | has kârı `karHasIkiHane` (BASİT ayrı rakam) | KR61; KR67 ×2; MA25 ×2 | AYNI |
| M7 | BASİT takas 0'da da çizilir | MA24_Takas_ve_iscilik_kartlari | AYNI (ilk koşum bayat derlemeyle MA25 göstermişti; dokunma+2 sn beklemeyle yeniden koşuldu) |
| M8 | `MadenMiktari` her madeni "has" yazar | MA10; MA20 ×2; MA26; MA30; MA31 | AYNI |
| M9 | stok yaşı `fizikselGram` hep null | MA04_Stok_yasi_kova_gumus_fiziksel_ve_saf_gram | AYNI |
| M10 | stok değeri `fizikselGram` ters koşul | MA05_Stok_degeri_satirinda_maden_ve_fiziksel_gram_toplam_yalniz_altin | AYNI |
| M11 | eksik kutusu yalnız satış adedine bakar | MA27_Maliyeti_eksik_yalniz_iade_kutuyu_acar_eksi_tutar_basilmaz | AYNI |
| M12 | BASİT'te defter kartı ızgarada | MA26_BASIT_defter_grami_kopruden_ayri_katlanirda_PRO_kartta_madenine_gore | AYNI |
| M13 | devir formülü tekrar kart altında | MA31_Stok_yasi_gumus_birimi_ve_devir_tanimi_ipucunda | AYNI |
| M14 | İşçilik kartı köprüde varken de çizilir | MA24 | AYNI |
| M15 | stok değeri satırı `Bicim.Has` | MA30_Stok_degeri_gumus_satiri_has_yazmaz_altin_has | AYNI |
| M16 | stok kartı gümüş alt satırı kaldırıldı | MA20 ×2 | AYNI (ilk koşumda ters değiştirme `/>` ilk eşleşmeye düştü → FARKLI; dosya yedekten geri kopyalandı, `cmp` AYNI; benzersiz kalıpla yeniden koşuldu) |
| M17 | defter satırları altın-önce sıralaması kaldırıldı | MA26 (fikstür gümüş-önce yapıldıktan sonra; ilk denemede kırmızı yoktu = test açığı → kapatıldı) | AYNI (ilk ters değiştirme yanlış satıra düştü → yedekten geri, cmp AYNI) |

17/17 kırmızı. Her mutasyon sonrası dosyalar yedekle `cmp` AYNI doğrulandı; son temiz `dotnet build Kuyumcu.sln` 0 hata 0 uyarı.

## 7. denetim.sh (son kod)
Tek ✗: `P-30: DEMO_INSAN_SURESI (BK-45)` (görevde muaf). 303 ✓; bu paketin sayaçları ✓ (Pages altında yeni ModalPencere 0, satır içi style 0, sabit renk 0, ham ToString yok — yalnız mevcut `ToString("N0")` adet kalıbı).

## 8. Canlı doğrulama (API 5160; ham: `ham/MADEN-AYRIMI-olcum/`)
Test kiracısı **M** `PAKETMADEN-AYRIMI-TEST-M` (01M3Z0YFEZQ8GSPNYV2BVM0ED9). Ürünler: MA-A 22 ayar bilezik 3,5 g (açılış 2, maliyetsiz) · MA-G 925 gümüş yüzük 30 g (açılış 5, maliyetsiz).

| Adım | HTTP | API | Bağımsız SQL |
|---|---|---|---|
| 1 maliyetsiz stok | 200 | stok-degerlemesi `toplamHasGram 6.412 = altinHasGram`; kırılım ALTIN 6.412 (tartı 7,0) / GUMUS 138.75 (tartı 150); **stokMaliyetiTl null, stokMaliyetiHas null**, kısmi false | — |
| 1 stok yaşı / stok değeri | 200/200 | kova hasGram 6.412; GUMUS 138.75 / 150 · stok-degeri toplamHas 6.412; satır MA-G GUMUS 138.75 / 150 | — |
| 2 alış 2 × 15.000,25 (altın) | 200 | `stokMaliyetiTl 30000.5`, kısmi **true**, maliyetliAdet 2 / maliyetsiz 7 | — |
| 3 satış altın + gümüş (FIFO maliyetsiz) | 200 | kâr HESAPLANAMADI; defter GUMUS 27.75 / ALTIN 3.206 | — |
| 4 iki altın satışı daha | 200 | KISMI: bilinen 22.500,55 − maliyet 15.000,25 = **kâr 7.500,30**; has 3,4002 − 2,2666 = **1,1336** (ürün satırı karHas 1,1336 = özet) | — |
| 4 son stok | 200 | `toplamHasGram 3.206`; GUMUS 111,00 saf / 120 tartı; stokMaliyetiTl 15.000,25 kısmi | `altin_has_yalniz 3.2060` · eski karışık toplam 114.2060 · MA-A K22 1 adet 3,206 saf / 3,5 tartı / bilinen 15.000,25 · MA-G G925 4 adet 111,0 saf / 120 tartı / maliyetsiz 4 (`sql-bagimsiz.txt`) |

## 9. Gerçek tarayıcı ölçümü (başsız Chrome, `betikler/tur/tur.mjs` + sikisma-olc, CDP 9707, vekil 5161; API yayın anlık görüntüsünden — ağaçtaki eşzamanlı derleme `bin/` dizinini silip çalışan API'yi düşürdüğü için)
Temiz koşum `tur4`: 226 kayıt (24 sayfa + 78 sekme), **hata 0, geçersiz 0**, 12 kombinasyonun 12'si.

| Rota | Genişlik × Mod × Tema | Sonuç |
|---|---|---|
| /raporlar/kar-zarar (Özet, Grafikler, Köprü, Ürünler, Stok) | 1440/1280/375 × BASİT/PRO × açık/koyu | **temiz** — taşan 0 · kırık 0 · binen 0 · yatay 0 · gizli-görünen 0 · TRY 0 · jargon 0 · ezik 0 · kırpık 0 · K4 0 |
| /raporlar (Stok değeri, Stok yaşı dahil) | 12 kombinasyon | yalnız önceden var olan: K12 ezik `#rp-yil`/`#rp-ay` (80/76 px, PRO 1280/1440 — KAR-RAPOR-2'de de vardı); K4 "tren" 375 PRO ana sayfa ve Gün sonu (önceden var); **K4 tren 3x4 Stok yaşı 375 PRO** — sayfa uzunluğu 10 istif kart; bölüm sayısı ölçeri `h2 + details>summary` sayar, eklenen "Devir hızı nasıl hesaplanır? ⓘ" bir `summary` olduğundan 3→4 oldu (görüntüde yeni bir bölüm yok; istenen ⓘ kalıbının ölçer yan etkisi) |

Görüntüler Read ile GÖRÜLDÜ (`ham/MADEN-AYRIMI-olcum/foto/`):
- `tur1-maliyetsiz__BASIT_aydinlik_1280__sekme4.png`: Stok — "ALTIN (HAS) 6,4120 has · Gümüş: 150,00 g (saf 138,75 g)", "STOK MALİYETİ **Bilinmiyor**", güvenilirlik rozeti yok, "Bu rakamlar nereden geliyor?" kapalı.
- `raporlar_kar_zarar__BASIT_karanlik_375__sekme4.png`: "3,2060 has · Gümüş: 120,00 g (saf 111,00 g)", "BİLİNEN STOK MALİYETİ 15.000,25 ₺", "Maliyeti bilinmeyen 4 adet bu tutara katılmadı.".
- `raporlar_kar_zarar__PRO_aydinlik_1440.png`: başlıklar "Para kârı / Altın kârı (has gram)"; 3,4002 − 2,2666 = 1,1336 has; defter "saf 27,75 g" (Gümüş) / "9,6180 has" (Altın); "Has satış ile defterdeki has neden farklı? ⓘ".
- `raporlar_kar_zarar__BASIT_karanlik_1280.png`: has kartları 4 hane (PRO ile aynı rakam), 3 kart, defter kartı yok, kapalı katlanır.
- `raporlar__PRO_karanlik_1440__sekme3.png`: Stok değeri gümüş satırı "120,00 g (saf 111,00 g)", altın "3,2060 has"; reyon "Gümüş: saf 111,00 g · Altın: 3,2060 has".
- `raporlar__PRO_aydinlik_375__sekme4.png`: Stok yaşı kova "3,2060 has · Adet: 5 · Gümüş: 120,00 g (saf 111,00 g)", devir tanımı ⓘ arkasında.
(Önceki `tur3` koşumu iki tur örneğinin aynı tarayıcıyı paylaşmasıyla kirlendi — 1440 adlı görüntü 375 genişlikteydi; geçersiz sayıldı, `tur4` tek örnekle temiz koşuldu.)

## 10. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin dokunduğu)
```
 M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs                (StokDegeri satırı fizikselGram)
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor                        (stok değeri, reyon, stok yaşı, özet maden hücresi)
 M dotnet/Kuyumcu.Web/Servisler/Bicim.cs                          (MadenMiktari)
?? dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs          (dizin izlenmiyor)
?? dotnet/Kuyumcu.Api/Modules/Rapor/StokYaslandirmaRaporu.cs
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs                 (14 yeni anahtar TR+EN)
?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor
?? dotnet/Kuyumcu.Web/Servisler/RaporServisi.cs
   dotnet/Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs
   dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs
   dotnet/Kuyumcu.Tests/MadenAyrimiTestleri.cs                    (yeni)
   dotnet/Kuyumcu.Tests/KarRaporTestleri.cs
   dotnet/Kuyumcu.Tests/KarZararEkranTestleri.cs
   dotnet/Kuyumcu.Tests/BelgeKalanTestleri.cs
   dotnet/Kuyumcu.Tests/Dogrulayici/ORTAK1SinirTestleri.cs
```
Dokunulan sayfalar: KarZarar.razor (KAR-RAPOR-2'de açılmıştı — gerekçe: aynı bulgu zincirinin devam paketi, hasım t2 bulguları), Raporlar.razor (stok değeri / stok yaşı / özet maden hücresi; Gün sonu HARİÇ).

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Her rapor ekranında has toplamı yalnız altın, gümüş ayrı | KARŞILANDI | §8 SQL 3,2060 = API; MA01/MA05/MA20/MA30/MA31; görüntüler |
| Bilinmeyen maliyet hiçbir yerde 0 değil | KARŞILANDI | §8 adım 1 null; MA02/MA21; tur1 görüntüsü "Bilinmiyor" |
| BASİT/PRO aynı rakam | KARŞILANDI | MA25/KR67; görüntüler 1,1336 has iki modda |
| BASİT jargon/teknik paragraf/rozet ⓘ veya katlanırda | KARŞILANDI | MA22/MA23/MA24/MA26/MA31 |
| Mutasyon ≥1 kırmızı, cmp AYNI | KARŞILANDI | §6, 17/17 |
| Başsız Chrome 1440/1280/375 BASİT+PRO açık+koyu, Read | KARŞILANDI | §9, tur4 hata 0 |
| build 0/0 | KARŞILANDI | son temiz derleme 0 hata 0 uyarı |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | §7 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | §5 (tek kırmızı kararsız, ilgisiz, 3/3 yeşil) |
| Test kiracısı silindi | KARŞILANDI | `zsh kiraci-temizle.sh 01M3Z0YFEZQ8GSPNYV2BVM0ED9 --uygula`; 75 tenant_id'li tabloda kalan satır 0; tenants başlangıç 16 → son 15 (fark −1 başka şeridin kendi kiracısını silmesi; betik yalnız verilen kimliği siler). QA-TEST-* dokunulmadı |

## 12. Açık kalanlar (sahibiyle)
- **Has binlik gruplaması** (ilke "binlik gruplu") — `Bicim.Has` F4 binliksiz; değişikliği 6 paketin mühürlü testlerini etkiler → BICIM-TEK / orkestratör kararı.
- **`pos-fatura-mutabakati` `hurda.hasGrami` tüm madenleri topluyor** (RaporModulu.cs ~744, Gün sonu) → TOPLU-5 / Gün sonu sahibi.
- **Panel.razor** eksik-iade dalı (`karEksikSatis > 0` koşulu yalnız satış) — dosya alanı dışı → TOPLU kuyruğu.
- Sözlük hub metinleri: PRO kart etiketi "Defter has gramı (Gümüş)", tablo başlıkları "Has gram (ürünün madeni)" / "Has gram (madene göre)" ve `MadenToplanmaz` uyarısındaki "her madenin has gramı" — gümüş için "has" kelimesi (Sozlukler.cs / Sozlukler.RaporHasAdi.cs, hub — anahtar eklenmez/değiştirilmedi) → TOPLU kuyruğu.
- Stok yaşı 375 PRO K4 "tren" (ölçer ⓘ summary'yi bölüm sayıyor) — ölçer iyileştirmesi önerisi.
- `/raporlar` K12 `#rp-yil`/`#rp-ay` ezik — önceden var, bu paketin dışı.
- `P07DayaniklilikTestleri.B10` kararsız (eşzamanlılık) — sahibi P-07.
- Ağaç paylaşımı: başka şeritlerin derlemesi `bin/` dizinini çalışan API'nin altından siliyor (BCrypt/NUlid yüklenemedi) — canlı doğrulamada yayın anlık görüntüsü kullanılmalı (orkestratöre süreç notu).

## 13. Kurucu / mali müşavir
Mevzuat hükmü içermez (rapor sunumu, birim ve biçim). Not: gümüş için "saf gram" gösterimi (925 milyem × tartı) teknik bir türetmedir; defter/beyan birimi değildir.


## 14. TUR 2 — doğrulama bulgularının kapatılması (03.10)

### 14.1 Bulgu → çözüm
| Bulgu | Çözüm | Dosya |
|---|---|---|
| [KRİTİK] Gümüş "has" taşıyor: ürün satırında gümüş kolye "0,1359 has", altın kârı has toplamı gümüşü içeriyor, PRO kartı "Defter has gramı (Gümüş)" | **Sunucu:** `Olc` has bacağı (hasilatHas, iadeHas, bilinenHasilatHas, maliyetHas, karHas, karHasIkiHane) YALNIZ ALTIN hareketlerinden (`AltinMaden` = `HareketToplami.MadenKodu` tek kaynak); maliyetHas yalnız altın hareketlerinin tüketim satırları; altın alt kümesi HESAPLANAMADI ise karHas/maliyetHas **null** (gümüşün bilinen maliyeti altın kârını "hesaplanmış" yapmaz). Yanıta `hasBacagiMadeni:"ALTIN"`. `kursuzSatis` güvenilirlik sayımı tüm satışlar için korundu. `UrunBazli`: satıra `maden`; altın dışı satırda `hasilatHas/maliyetHas/karHas = null` → ekran "—". **Ekran:** defter kartı etiketi altında "Defterdeki altın (has)", diğer madenlerde "Defterdeki {maden} (saf gram)"; PRO ⓘ tanım metni yeni anahtar `HasTanimMetni` (gümüşün altın kârına katılmadığını söyler) | KarZararServisi.cs, KarZarar.razor, Sozlukler.KarRapor.cs |
| [KRİTİK] Has binlik gruplanmıyor ("1447,3856 has" ↔ "9.237.000 ₺") | `Bicim.Has` → tr-TR **N4** ("1.447,3856 has"; 1.000 altı değişmez). `MadenMiktari` gümüş gramı da N2 gruplu ("1.500,00 g (saf 1.387,50 g)") — aynı ekranda tek sayı dili. `HasGirdi` (giriş kutusu) DEĞİŞMEDİ. Bağlı mühürlü testler aynı pakette güncellendi (§14.3) | Bicim.cs |
| [KRİTİK] BASİT Özet'te ekranda olmayan kartlara gönderme yapan teknik maden bandı | BASİT'te bant **hiç çizilmez**; PRO'da sarı kutu yerine "Birden çok maden var ⓘ" (kapalı `details`), metin: "Altın has olarak, gümüş saf gram olarak ayrı gösterilir; farklı madenlerin gramları toplanmaz." (`#kz-maden-uyari` id korundu; yeni `#kz-maden-uyari-ipucu`, `#kz-maden-uyari-metin`) | KarZarar.razor, Sozlukler.KarRapor.cs |
| [DÜŞÜK] stokMaliyetiHas: TL maliyeti bilinen ama has fiyatı olmayan parti 0 sayılıyor | Has maliyeti AYRI sayılır (`hasMaliyetliAdet`, `hasMaliyetsizAdet`, `stokMaliyetiHasKismi`), yalnız ALTIN partilerinden; TL ya da has maliyeti hiç bilinmiyorsa `stokMaliyetiHas = null` | KarZararServisi.cs |
| [DÜŞÜK] Gümüş için "has" taşıyan tablo başlıkları ("Has gram (ürünün madeni)" / "(madene göre)" / "(satırdaki maden)") | Raporlar.razor'da üç başlık yeni anahtar **"Miktar (madenine göre)"** (hub'a dokunulmadı; id'ler aynı) | Raporlar.razor, Sozlukler.KarRapor.cs |
| [ORTA] Panel.razor eksik-iade dalı | Dosya alanı DIŞI → TOPLU kuyruğunda (değişmedi) | — |
| [DÜŞÜK] Rapor merkezi BASİT "Altın pozisyonum 0,0 g has" / pos-fatura-mutabakati hurda.hasGrami | Gün sonu / Panel alanı (TOPLU-5) → açık kalan (değişmedi) | — |

Yeni sözlük anahtarları (TR+EN, Sozlukler.KarRapor.cs): `DefterAltinEtiketi`, `DefterMadenEtiketi`, `MadenAyriAc`, `MadenAyriMetin`, `MiktarMadenineGore`, `HasTanimMetni`.

### 14.2 DoD sözleşmesi (tur 2)
Yeni varlık/kayıt türü yok; yalnız türetilmiş rapor alanları (`hasBacagiMadeni`, satır `maden`, `hasMaliyetliAdet/hasMaliyetsizAdet/stokMaliyetiHasKismi`). Düzeltme yolu değişmedi (kaynak işlemin ekranında ters kayıt; DegismezIpucu PRO'da görünür). Yeni düğme yok (yalnız ⓘ `details`). Migration yok.

### 14.3 Testler
- Yeni (MadenAyrimiTestleri.cs): **MA07** (altın+gümüş kiracı ile altın-yalnız kiracı has bacağı birebir aynı; gümüş ürün satırı has null; altın satırı = dönem altın kârı), **MA08** (altın maliyeti bilinmiyor + gümüş bilinen → karHas null, karTl 800), **MA09** (kursuz parti has maliyeti null, kısmi işaret ayrı), **MA11 ×2** (Has N4, en-US/tr-TR, 1.447,3856 / 1.400,0000 / -1.234,5000; gümüş 1.500,00 g), **MA28 ×2** (bUnit: gümüş satır "—", altın "19,1520 has"), **MA29** (BASİT bant yok, PRO kapalı ⓘ), **MA32 ×2** (bUnit: 1.447,3856 has ve stok 1.400,0000 has iki modda). **12 yeni vaka.** MA26 güncellendi (yeni etiketler + ⓘ metni).
- Bağlı testler — gerekçe: ekip kararı 5 İLKE "has her yerde binlik gruplu" + tasarım hakemi KRİTİK; her yerde `MADEN-AYRIMI tur 2` yorumu: BicimTestleri (Has_TurkceVirgulOndalikBasar), Dogrulayici/ZOBELGESinir (2 literal), KESIFP2Sinir (1), BELGEKALANSinir (1), ORTAK1Sinir (4 literal + OS6 kademe "uzun"→"cok-uzun": "123.456,7891 has" 16 karakter), EMANET1Sinir (10 literal), MADENAYRIMISinir MAS10 (gümüş gram gruplu), P69HasAdlandirma P69_03 (başlıklar "Miktar (madenine göre)"), KarZararEkranTestleri (P35_06, P45_04, ORTAK1_HK_bacak… — "Defter has gramı (Altın/Gümüş)" → yeni etiketler).
- Filtreli geniş koşum (MadenAyrimi|MADENAYRIMI|P69|Bicim|Sinir|KarZarar|KarRapor|Rapor|Emanet|Maliyet|Panel): **2725 toplam · 2724 geçti · 0 başarısız · 1 atlanan**.
- Build (`dotnet build Kuyumcu.sln`, son temiz): **0 hata**; uyarılar yalnız başka şeridin dosyası (SUBEKAPSAMASinirTestleri.cs:49 CS8601). Bu paketin dosyalarında 0 uyarı.
- **Tam süit** (`trx-pMADEN-AYRIMI-t4/_Mac_2026-10-03_00_56_42.trx`, son kodla): **9825 · geçti 9821 · başarısız 2 · atlanan 2**. Kırmızılar bu paketin ürünü DEĞİL:
  - `OlcekTekTestleri.OL26_v13_mod_yuzu_sinifi_kontrol_olcusu_YALNIZ_onayli_envanterden` — mesaj `degisim-gunsonu.css | .tuval-tezgah #takas-tuval-fark-bekle | padding` (DEĞİŞİM-GÜNSONU şeridinin CSS'i; bu paket CSS'e dokunmadı).
  - `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` — devir notunda zaten listeli (PATCH-3 / Gün sonu); bu turda RaporModulu.cs'e dokunulmadı.
  → **Yeni kırmızı 0.**

### 14.4 Mutasyonlar (tur 2; yedek `/private/tmp/kuyumcu-serit/mut-yedek/MADEN-AYRIMI-t2/`, betik `mut-t2.sh`, ham `ham/MADEN-AYRIMI-olcum/t2/mut-t2-sonuc.txt`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M18 | Olc brüt has hasılatı tüm madenlerden | MA07_Altin_kari_has_yalniz_altin_satislarindan_gumus_satiri_has_tasimaz | AYNI |
| M19 | UrunBazli `altinUrun = true` | MA07 | AYNI |
| M20 | `Bicim.Has` tekrar F4 | MA11 ×2; BicimTestleri.Has_TurkceVirgulOndalikBasar; MA32 ×2 | AYNI |
| M21 | BASİT'te maden bandı geri | MA29_Maden_bandi_BASITte_yok_PROda_ipucunda | AYNI |
| M22 | gümüş kart etiketi eski "Defter has gramı ({0})" | MA26; KarZararEkranTestleri.P45_04 | AYNI |
| M23 | stokMaliyetiHas yalnız TL kuralına bakar | MA09_Stok_has_maliyeti_kursuz_partiyi_sifir_saymaz_ayri_sayilir | AYNI |
| M24 | altın alt kümesi fail-closed kaldırıldı | MA08_Altin_maliyeti_bilinmiyorsa_has_kari_null_… | AYNI |
7/7 kırmızı; üç kaynak dosya yedekle `cmp` AYNI; ardından temiz `dotnet build Kuyumcu.sln` 0 hata.

### 14.5 denetim.sh (tur 2 son kod)
303 ✓, tek ✗ `P-30: DEMO_INSAN_SURESI (BK-45)` (görevde muaf). → TEMİZ (P-30 hariç).

### 14.6 Canlı doğrulama (yayın anlık görüntüsü `yayin2`, API 5160; ham `ham/MADEN-AYRIMI-olcum/t2/`)
Kiracı **PAKETMADEN-AYRIMI-TEST-M2** (01M3ZA3333N8MT7HMTAAHPJSET). Ürünler: M2-A 22 ayar 3,5 g (açılış 2, maliyetsiz) · M2-G 925 gümüş 30 g (açılış 5) · M2-K 24 ayar külçe 1000 g (açılış 2).
| Adım | HTTP | API | Bağımsız SQL |
|---|---|---|---|
| kur + ürünler | 201×5 | stok-degerlemesi `toplamHasGram 2006.412` (altın), GUMUS 138,75 saf / 150 tartı ayrı; stokMaliyetiTl/Has **null** | — |
| alış altın 1×15.000,25 + gümüş 1×900 | 200×2 | stokMaliyetiTl 15.900,25 kısmi; **stokMaliyetiHas 2,2651 (yalnız altın)** | — |
| 3 altın + 6 gümüş satış | 200×9 | kâr-zarar KISMI: karTl 6.599,75 (gümüş dahil); **hasBacagiMadeni ALTIN, hasilatHas 9,5133, bilinenHas 3,1711, maliyetHas 2,2651, karHas 0,9060**; ürün M2-G `maden GUMUS, hasilatHas/maliyetHas/karHas null`, karTl 600; M2-A karHas 0,906 | SATIS ALTIN tl/kur = **9,5133**, GUMUS 1,3590 (eski karışık toplam 10,8723 olurdu); bilinen altın 3,1711 − 2,2651 = **0,9060** (eski hesap gümüşün 0,2265−0,1359 farkını ekleyip 0,9966 derdi) |
| gümüş alış 3×950 (stokta gümüş kalsın) | 200 | toplamHasGram 2000 (külçe), GUMUS 83,25 saf / 90 tartı; stokMaliyetiTl 2.850 kısmi; **stokMaliyetiHas null** (altın külçenin maliyeti yok), hasMaliyetsizAdet 2 | stok_partileri: G925 3 adet 83,25 saf / 90 tartı, maliyetsiz 0 · K24 2 adet 2000,0 saf, maliyetsiz 2 / has-maliyetsiz 2 |

### 14.7 Gerçek tarayıcı (başsız Chrome, `tur.mjs` + sikisma-olc, CDP 9707, vekil 5161; `tur5`)
226 kayıt (24 sayfa + 78 sekme), **hata 0, geçersiz 0**, 12 kombinasyon (1440/1280/375 × BASİT/PRO × açık/koyu).
| Rota | Sonuç |
|---|---|
| /raporlar/kar-zarar Grafik / Köprü / Ürünler / Stok | **temiz** (taşan 0 · kırık 0 · binen 0 · yatay 0 · gizli-görünen 0 · TRY 0) |
| /raporlar/kar-zarar Özet | yalnız **K4 "tren" 3.4x4, PRO 375 açık+koyu** — ölçer `h2 + details>summary` sayar; PRO'ya eklenen "Birden çok maden var ⓘ" bir `summary` (istenen ⓘ kalıbının ölçer yan etkisi; görüntüde yeni bölüm yok — Stok yaşı'ndaki tur 1 bulgusuyla aynı nitelik) |
| /raporlar (tüm sekmeler) | yalnız önceden var olanlar: K12 `#rp-yil/#rp-ay` ezik (PRO 1280/1440), K4 tren 375 ana/Gün sonu/Stok yaşı |

Görüntüler Read ile GÖRÜLDÜ (`ham/MADEN-AYRIMI-olcum/t2/foto/`):
- `raporlar_kar_zarar__BASIT_aydinlik_1440.png`: maden bandı YOK; altın kârı 3,1711 / 2,2651 / 0,9060 has (PRO ile aynı).
- `raporlar_kar_zarar__PRO_aydinlik_1440.png`: kartlar "DEFTERDEKİ GÜMÜŞ (SAF GRAM) saf 166,50 g" ve "DEFTERDEKİ ALTIN (HAS) 9,6180 has"; "Birden çok maden var ⓘ" kapalı; sarı maden kutusu yok.
- `raporlar_kar_zarar__BASIT_aydinlik_1440__sekme3.png`: Ürünler — 22 ayar bilezik HAS KÂR "0,9060 has", 925 gümüş kolye HAS KÂR **"—"**, TL kârı 600 ₺.
- `raporlar_kar_zarar__PRO_karanlik_375__sekme4.png`: Stok — "ALTIN (HAS) **2.000,0000 has** · Gümüş: 90,00 g (saf 83,25 g)", "BİLİNEN STOK MALİYETİ 2.850 ₺".
- `raporlar__PRO_karanlik_1440__sekme3.png`: Stok değeri — başlık "MİKTAR (MADENİNE GÖRE)", külçe "2.000,0000 has", gümüş "90,00 g (saf 83,25 g)", reyon "Altın: 2.000,0000 has · Gümüş: saf 83,25 g".
- `raporlar_kar_zarar__PRO_aydinlik_375.png`: tüm özet istifi; taşma yok.

### 14.8 Temizlik
API (PID dosyası `api-t2.pid`) ve vekil `kill` ile durduruldu; 5160/5161/9707 dinleyen yok. `zsh kiraci-temizle.sh 01M3ZA3333N8MT7HMTAAHPJSET --uygula`: 75 tenant_id'li tablodan 272 satır + tenants 1 satır silindi; doğrulama `tenants where id=…` 0, `hareketler` 0. tenants: başlangıç 16 → son 17 (fark: eşzamanlı şeritlerin kendi kiracıları — PAKETPATCH-4-TEST, PAKETDEGISIM-GUNSONU-TEST vb.; benimki silindi). QA-TEST-* dokunulmadı.

### 14.9 Değişen dosyalar (tur 2)
`dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs` · `dotnet/Kuyumcu.Web/Pages/KarZarar.razor` · `dotnet/Kuyumcu.Web/Pages/Raporlar.razor` (yalnız 3 tablo başlığı) · `dotnet/Kuyumcu.Web/Servisler/Bicim.cs` · `dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs` · testler: `MadenAyrimiTestleri.cs`, `KarZararEkranTestleri.cs`, `BicimTestleri.cs`, `P69HasAdlandirmaTestleri.cs`, `Dogrulayici/{MADENAYRIMI,ZOBELGE,KESIFP2,BELGEKALAN,ORTAK1,EMANET1}SinirTestleri.cs`. Dokunulan sayfalar: KarZarar.razor, Raporlar.razor (gerekçe: aynı paketin doğrulama turu düzeltmesi; Gün sonu sekmesine dokunulmadı).

### 14.10 Açık kalanlar (tur 2 sonrası, sahibiyle)
- Panel.razor eksik-iade dalı (`MaliyetiEksikIadeAdedi` koşulu) → TOPLU kuyruğu.
- Rapor merkezi/Panel "Altın pozisyonum 0,0 g has" ayrı has biçimi → TOPLU-5 / BICIM-TEK (Bicim.Has artık N4 — o kartın Bicim.Has'a bağlanması yeterli).
- `pos-fatura-mutabakati hurda.hasGrami` tüm madenleri topluyor → Gün sonu sahibi (TOPLU-5).
- Hub sözlüğünde kalan eski metinler (`KarZarar.MadenToplanmaz`, `HasDefterGramMaden`, `HasTanimNotu`, `RaporHasSozlugu.*HasBaslik`) artık bu ekranlarda kullanılmıyor; hub temizliği → TOPLU kuyruğu.
- PRO defter kartlarında sıra sunucudan (gram büyüklüğüne göre; gümüş önce gelebiliyor), BASİT katlanırda altın önce — kozmetik, TOPLU kuyruğu.
- Ölçer K4 "tren": ⓘ `summary`'leri bölüm sayıyor (kar-zarar Özet PRO 375, Stok yaşı PRO 375) → ölçer iyileştirmesi önerisi.
- Tam süitte başkasına ait 2 kırmızı: OL26 (degisim-gunsonu.css — DEĞİŞİM-GÜNSONU şeridi), Yasal4Http Mutabakat pusula (PATCH-3 / Gün sonu sahibi).

### 14.11 Kurucu / mali müşavir
Mevzuat hükmü yok (gösterim/birim kuralı). Not: altın kârının "has" bacağı artık yalnız altın satışlarını ölçer; gümüş satışının kârı yalnız TL bacağında görünür — bu bir proje gösterim kararıdır (ekip kararı 5), defter/beyan kuralı değildir.
