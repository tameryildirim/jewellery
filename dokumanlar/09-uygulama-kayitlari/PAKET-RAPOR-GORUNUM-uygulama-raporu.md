# PAKET RAPOR-GORUNUM — uygulama raporu (03.10, t1 · API 5160 / vekil 5161 / CDP 9709)

> Taşkın (backend) + Ertuğ (frontend). Kaynak: `PAKET-MADEN-2-uygulama-raporu.md` + MADEN-2 hasım/tasarım bulguları.
> COMMIT YOK. Migration YOK (şema değişmedi; yalnız türetilmiş rapor alanları). Ham kanıt: `ham/RAPOR-GORUNUM-olcum/`.

## 0. TUR 2 (03.10) — doğrulayıcı bulgularının kapatılması (bu bölüm §1–§13'ü GÜNCELLER)

| Bulgu (doğrulayıcı) | Çözüm | Kanıt |
|---|---|---|
| **[KRİTİK]** `Bicim.HasIsaretli(-0.00004)` → "−0,0000 has" (RGS03 HATA) | `Bicim.cs`: önce `Math.Round(n, 4, AwayFromZero)`, işaret YUVARLANMIŞ değerden; tek-argümanlı imza korunur (`"has"`), yeni `HasIsaretli(n, birim)` | RGS03 + yeni RG12 yeşil; mutasyon R1 (işareti yuvarlanmamış değerden oku) → RGS03 + RG12 KIRMIZI, cmp AYNI. Panel "Kim borçlu" eşiği (0.000001) DEĞİŞMEDİ — Panel'de bu paketin alanı yalnız "Bugün ne sattım"; artık bu aralıkta en kötü "0,0000 has" yazar, eksi işareti yazmaz (açık kalanlarda) |
| [DÜŞÜK] Grafik: bu dönem HESAPLANAMADI + önceki KISMI iken etiket "Satış" ama değer toplam hasılat | Etiket ve 1. seri değeri AYNI koşula (`GrafikHasilatBilinenMi(o)`) bağlandı; karşılaştırılan dönemin satış çubuğu bilinen kümedense yeni not `#kz-grafik-onceki-hasilat-notu` ("…O dönemin toplam satışı X") | RG44 (HESAPLANAMADI + önceki KISMI: etiket "Hasılat", değer 10.000 ₺, kendi notu yok, önceki notu 9.000 ₺); R4 (eski `||` koşulu) → RG44 KIRMIZI; R5 (not kapalı) → RG44 KIRMIZI; R9 (1. seri yine toplam) → RG41 KIRMIZI |
| [DÜŞÜK] EN modda has birimi Türkçe "has" | Sözlük `KarRaporSozlugu.HasBirimKisa` (TR "has", EN "fine"); Panel `HasMetni` ve Rapor merkezi `BugunHas` `Bicim.HasIsaretli(n, KR.HasBirimKisa)` — iki ekran yine AYNI metin | RG32 (Panel EN "26,0588 fine", Kim borçlu "−22,0000 fine"), RG24 (Rapor merkezi EN "26,0588 fine"); R2 → RG32, R7 → RG24 KIRMIZI |
| [DÜŞÜK] Ürünler'de gümüş satırının "—" has kârı gerekçesiz | Sözleşme `UrunSatiri` + `string? Maden = null` (sunucu `maden`'i zaten döndürüyordu); altın dışı satırda not `.kz-urun-has-altin-degil` "Has yalnız altında hesaplanır" (EN "Fine gold is calculated for gold only") | RG45; R6 → RG45 KIRMIZI; tarayıcı 1280 PRO ve 375 BASİT koyu görüntüsünde iki gümüş satırında not görünür |
| [DÜŞÜK] Stok değeri sırası yalnız gelen sayfa içinde (istemci) | Sıralama SUNUCUYA taşındı (`RaporModulu.StokDegeri`): ürün kartına sol birleşim, madenin ayar kimlikleri IN listesi (`Ayarlar.Tumu`), sıra = ALTIN (ve ayarı bilinmeyen) 0 · Gümüş 1 · Platin 2 · Paladyum 3 · Çelik 4, sonra miktar ↓, sonra ürün kimliği — `Skip/Take`'TEN ÖNCE. İstemci `StokSatirlari` artık sunucu sırasını aynen kullanır | Yeni DB testi RG06 (limit=1: 1. sayfa altın 1 adet, 2. sayfa gümüş 277,5 g — eski sıra gümüşü öne alırdı); R3 → RG06 KIRMIZI (son kodla yeniden koşuldu, cmp AYNI). Canlı: limit=1 sayfa sayfa `RG-T ALTIN 3.206 → RG-K ALTIN 3.206 → RG-A ALTIN 2.34 → RG-G GUMUS 83.25 → RG-Z GUMUS 0` = bağımsız SQL sırası |
| [ORTA] 5199 dinlemiyor | DOKUNULMADI (görev kuralı). Bu turda da `lsof -nP -iTCP:5199 -sTCP:LISTEN` boş döndü → orkestratöre (açık kalanlar) | — |
| [DÜŞÜK] Paylaşılan `kuyumcu_test` çakışması | Bu turdaki TÜM test koşumları `KUYUMCU_TEST_DB=kuyumcu_test_t1` ile (mutasyon betiği dahil) | §0 test tablosu |

**Tur 2 denetim düzeltmesi:** ilk denetim koşumunda `Sınırsız okuma beyaz listesi: > RaporModulu.cs:AyarlariOf / < RaporModulu.cs:StokDegeri` çıktı — denetim betiği metot adını son bildirimden okuduğu için yeni yerel işlev, StokDegeri'nin mevcut (beyaz listedeki) `ToDictionaryAsync`'ini kendine mal etti. Yerel işlev lambdaya çevrildi (davranış aynı) → denetim yeniden ✓.

### Tur 2 testler
| Koşum | Sonuç |
|---|---|
| Paket + doğrulayıcı sınır testleri (`RaporGorunum|RAPORGORUNUM`) | **35/35** (RG01–06, RG10–12, RG20–24, RG30–32, RG40–45 = 23 + doğrulayıcının RGS/RGD 12) |
| Geniş ilgili küme (ZOBELGE, MadenIki, P57, KarRapor, S31, Toplu4, P69, KarZarar, MadenAyrimi, MaliyetTestleri, RaporSeti, P21Ticaret, Panel, Raporlar, StokDeger + paket) | **760/760** |
| **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pRAPOR-GORUNUM-t2/_Mac_2026-10-03_06_29_34.trx`, KUYUMCU_TEST_DB=kuyumcu_test_t1) | toplam **10195** · geçti **10190** · başarısız **2** · atlanan 3. Kırmızılar: `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` (bUnit WaitFor zaman aşımı), `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` — POS/Satış ekranı, bu paket dokunmadı; ikisi tek başına yeniden koşuldu **2/2 geçti** (zamanlama kararsızlığı). **Bu paketin yeni kırmızısı 0.** |
| Yeni test sayısı (tur 2) | 6: RG06, RG12, RG24, RG32, RG44, RG45 (paket toplamı 23) |

### Tur 2 mutasyonlar (betik `/private/tmp/kuyumcu-serit/RAPOR-GORUNUM/t2/mut2.py`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/RAPOR-GORUNUM-t2/`, ham `ham/RAPOR-GORUNUM-olcum/mut-sonuc-tur2.txt`)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| R1 | HasIsaretli işareti yuvarlanmamış değerden | RGS03 (doğrulayıcı), RG12 | AYNI |
| R2 | Panel has birimi sözlüksüz | RG32 | AYNI |
| R3 | Stok değeri sunucu sırasında maden anahtarı yok (son kodla tekrar) | RG06 | AYNI |
| R4 | Grafik etiketi eski `\|\| OncekiHasilatBilinenMi` | RG44 | AYNI |
| R5 | Önceki dönem küme notu kapalı | RG44 | AYNI |
| R6 | Gümüş has notu kapalı | RG45 | AYNI |
| R7 | Rapor merkezi Bugün birimi sözlüksüz | RG24 | AYNI |
| R8 | Panel has eski "N1 g has" (M4'ün yeni imzayla tekrarı) | RG30, S31_07, S31_12, T4_Kim_borclu | AYNI |
| R9 | Grafik 1. seri yine toplam hasılat (M5'in yeni koşulla tekrarı) | RG41 | AYNI |
9/9 kırmızı, 9/9 cmp AYNI; ardından temiz `dotnet build Kuyumcu.sln` **0 hata** (2 uyarı başka şeritlerin dosyalarında: `SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455` xUnit2029).

### Tur 2 canlı (API 5160 `dotnet run --no-build`, Development; ham `ham/RAPOR-GORUNUM-olcum/tur2-canli/`)
Kiracı **PAKETRAPOR-GORUNUM-TEST** (01M3ZW6ZWA6X7SX0HV9R8TCDPK). Kurulum: davet 201, kayıt 201, giriş 200, ürün 201×5, cari 201×2, alış 200×4, satış 200×6, iade 200, kasa 200, kasa girişi 200, hurda 200×2. (İlk API başlatmada `/sistem/davetler` 503 "Sistem:Anahtari yapılandırılmamış" döndü → API, anahtar ortam değişkeniyle yeniden başlatıldı; değer basılmadı.)

| Uç | HTTP | API | Bağımsız SQL |
|---|---|---|---|
| `/raporlar/gunluk` | 200 | satis.toplamHas 8.752 = altinToplamHas; GUMUS 31.45 ayrı | SATIS ALTIN 8,752000 (4) · GUMUS 31,450000 (2) |
| `/raporlar/pos-fatura-mutabakati` | 200 | hurda.hasGrami 9.16 · altinHasGrami 9.16 · [ALTIN 9.16, GUMUS 92.5] | HURDA_ALIS ALTIN 9,160000 · GUMUS 92,500000 |
| `/raporlar/stok-yaslandirma` | 200 | G0_30 = 7 · tahminiSatisSuresiGun 126 · ortalamaYasGun 13.6 | kalan 7,000 · ort_yas 13,6 |
| `/raporlar/kar-zarar` | 200 | KISMI · hasilat 48100 · bilinen 44100 − maliyet 31200 = kâr 12900 | hasılat 48100,00 |
| `/raporlar/stok-degeri?limit=1&ofset=0..4` | 200×5 | RG-T, RG-K, RG-A (ALTIN) → RG-G, RG-Z (GUMUS) | aynı sıra (maden_sira, toplam_has_gram ↓) |
| `/raporlar/kar-zarar/urun` | 200 | RG-G / RG-Z `hasKarDurumu` null, karHas null (ekran: "Has yalnız altında hesaplanır") | — |

Temizlik: API, vekil ve kendi başsız Chrome'um (CDP 9709, profil `t2/tur/profil-1`) PID ile durduruldu; 5160/5161/9709 dinleyen yok. `zsh kiraci-temizle.sh 01M3ZW6ZWA6X7SX0HV9R8TCDPK --uygula` → tenants/hareketler/stok_partileri/stoklar bu kimlik için **0**; tenants başlangıç **17** → son **17**. QA-TEST-* dokunulmadı.

### Tur 2 gerçek tarayıcı (başsız Chrome `tur.mjs`, vekil 5161, CDP 9709; ham `ham/RAPOR-GORUNUM-olcum/analiz-tur3.txt`, `olcum-tur3.jsonl`, görüntüler `tur2-foto/`)
272 kayıt (36 sayfa = 3 rota × 12 kombinasyon: 1440/1280/375 × BASİT/PRO × açık/koyu; 78 sekme; 158 katman), **hata 0 · geçersiz 0**. (analiz.py'nin "eksik" satırı tur 1'deki gibi tema alanını okuyamamasından — kayıtlarda 12 kombinasyon var.)

| Rota | 1440 | 1280 | 375 |
|---|---|---|---|
| `/` Panel | temiz ×4 | temiz ×4 | temiz ×4 |
| `/raporlar/kar-zarar` Grafik · Köprü · Ürünler · Stok | temiz ×4 | temiz ×4 | temiz ×4 |
| `/raporlar/kar-zarar` Özet | temiz | temiz | K4 tren 3.4x5 PRO (tur 1 ile aynı, önceden var) |
| `/raporlar` tüm sekmeler | K12 `#rp-yil/#rp-ay` PRO (TOPLU-6 alanı) | K12 (+Gün sonu K4, DEĞİŞİM-2) | K4 tren PRO (tur 1 ile aynı) |
Yeni bulgu 0 (tur 1 ile aynı 12 benzersiz kalem, hepsi önceden var/başka şerit alanı).

Read ile görülen görüntüler: `raporlar_kar_zarar__PRO_aydinlik_1280__sekme3.png` (iki gümüş satırında "— Has yalnız altında hesaplanır", RG-K "Alış günü altın fiyatı yok", RG-A "Altın maliyeti bilinmiyor"); `raporlar_kar_zarar__BASIT_karanlik_375__sekme3.png` (kart yığını, tüm sayılar görünür, gümüş notları); `kok__BASIT_karanlik_375.png` (Panel "6 satış 8,7520 has", kâr satırı tek parça, pozisyon "+17,9120 has · Vitrin 8,7520 has · Alacak 0,0000 has"); `raporlar__BASIT_aydinlik_1280.png` (Rapor merkezi Bugün "6 satış 8,7520 has" = Panel); `raporlar__PRO_karanlik_375__sekme3.png` (Stok değeri SUNUCU sırası altın önce, "0,00 g (saf 0,00 g)", ALTIN (HAS) 8,7520 has, GÜMÜŞ TOPLAMI 90,00 g (saf 83,25 g)); `raporlar_kar_zarar__PRO_aydinlik_1280__sekme1.png` (çubuklar Satış 44.100 / Maliyet 31.200 / Kâr 12.900 / Dönem kârı 12.900, alt not "Toplam satış 48.100 ₺").

### Tur 2 değişen dosyalar
`Kuyumcu.Web/Servisler/Bicim.cs` (HasIsaretli yuvarla-sonra-işaret + birim), `Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs` (+3 anahtar TR+EN: HasBirimKisa, HasYalnizAltinNot, GrafikOncekiHasilatNotu), `Kuyumcu.Web/Pages/Panel.razor` (yalnız `HasMetni` birimi), `Kuyumcu.Web/Pages/Raporlar.razor` (`BugunHas` birimi, `StokSatirlari` sunucu sırası — Gün sonu ve yıl/ay seçicisi HARİÇ), `Kuyumcu.Web/Pages/KarZarar.razor` (grafik etiket koşulu + önceki dönem notu, gümüş has notu), `Kuyumcu.Contracts/Sayfalar/KarZararSozlesmeleri.cs` (+`UrunSatiri.Maden`, isteğe bağlı, sona), `Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` (StokDegeri sayfa sırası sunucuda), `Kuyumcu.Tests/RaporGorunumTestleri.cs` (+6 test; RG21 sahte yanıtı sunucu sırasına çekildi). Migration YOK. Dokunulan sayfalar (tur 1 ile aynı üçü): Panel.razor, Raporlar.razor, KarZarar.razor — aynı paketin 2. turu (gerekçe: doğrulayıcı bulguları).

### Tur 2 açık kalanlar (ek)
- Kurucunun inceleme sunucusu **5199 dinlemiyor** (tur 2 başında ve sonunda `lsof` boş) — bu şerit dokunmadı → **orkestratör** (gece yetkisine göre yeniden yayın).
- Panel "Kim borçlu" listeye alma eşiği (`Alacak/BizBorcluyuz` 0.000001) görüntü hassasiyetinden (0.00005) küçük: |has| ∈ (0.000001, 0.00005) olan cari artık "0,0000 has" (işaretsiz) satırıyla görünür → **Panel / TOPLU-5 sahibi** (eşik değişikliği listeye kimin gireceğini değiştirir; bu paketin Panel alanı yalnız "Bugün ne sattım").
- Tam süitte kararsız (tek başına yeşil): `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir`, `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` → **POS/Satış ekranı sahibi**.


## 1. Veri maddeleri — kanıt ve çözüm

| Madde | Tespit (kanıtla) | Çözüm |
|---|---|---|
| **(a)** Panel ve Rapor merkezi "Bugün ne sattım … has" | İki ekran da AYNI uçtan okuyor: `GET /raporlar/gunluk` → `satis.toplamHas` (sunucuda zaten yalnız altın, P-69; `altinToplamHas` ile aynı). Fark yalnız BİÇİMDİ: Panel `HasMetni` = `N1 + "g has"` ("26,1 g has"), Rapor merkezi `Bicim.Has` (N4, "26,0588 has") | Tek biçim kaynağı `Bicim.HasIsaretli` (Bicim.Has + "−"). Panel `HasMetni` ve Raporlar `BugunHas` ona bağlandı. Panel "Kim borçlu" has satırı da aynı yardımcıyı kullandığı için aynı dile geçti (YÜKSEK bulgu bunu da istiyordu). Canlı: API 8,752 = SQL 8,752000 = Panel "8,7520 has" = Rapor merkezi "8,7520 has" (görüntüler §7) |
| **(b)** pos-fatura-mutabakati `hurda.hasGrami` tüm madenleri topluyor | Kod: `hurdalar.Sum(h => h.HasGrami)` (maden ayrımı yok). Canlı eski formül 101,66 (= altın 9,16 + gümüş 92,5) | `hasGrami` (alan adı korunur) ve yeni `altinHasGrami` YALNIZ ALTIN; yeni `madenDagilimi` (altın önce, gümüş ayrı, gramlar toplanmaz). Canlı: `hasGrami 9.16 · altinHasGrami 9.16 · madenDagilimi [ALTIN 9.16, GUMUS 92.5]` = SQL (ALTIN 9,160000 · GUMUS 92,500000) |
| **(c)** Stok yaşı "Ortalama raf ömrü 310,9 gün" ile "tüm stok 0-30 gün" | İki hesap da doğru, AD yanlıştı: `ortalamaRafOmruGun = kalan × pencere ÷ tüketilen` — bugünkü satış hızıyla malın kaç günde BİTECEĞİNİN tahmini (ileriye). Kovalar malın BUGÜNKÜ yaşı (geriye). Canlı: kalan 7, tüketilen 5 (90 gün) → 7×90/5 = **126** (API = SQL) ; gerçek ortalama yaş **13,6** gün (SQL `Σ adet×DATEDIFF ÷ Σ adet` = 13,6; en yaşlı 25 gün → hepsi 0-30 kovasında, çelişki yok) | Sunucu: yeni `devir.tahminiSatisSuresiGun` (= eski alan, doğru adıyla; eski alan korunur) + `devir.ortalamaYasGun` (adet ağırlıklı gerçek yaş, SQL'de `DATEDIFF` toplamı; parti yoksa null). Ekran: "Ortalama yaş (gün)" + "Tahmini satış süresi (gün)" kartları, alt satırda ne olduğunu söyler. Ondalık tek kural: oranlar 2 hane (`Bicim.Oran`: "0,71", "2,90"), gün tam sayı (`Bicim.GunSayisi`: "14", "126") |
| **(d)** Kâr-zarar grafikte hasılat çubuğu kârla tutarsız | VERİ doğru, ÇİZİM iki kümeyi karıştırıyordu: çubuk `hasilatTl` (tüm satış, 48.100) çiziyordu, maliyet yalnız bilinen satışların (31.200), kâr = bilinen satış − maliyet (44.100 − 31.200 = 12.900). Canlı API: hasilatTl 48100 = SQL 48100,00; bilinenHasilatTl 44100 − maliyetTl 31200 = karTl 12900 | Çubuk kâr kümesinden: KISMI'de "Satış" = maliyeti bilinen satış (44.100); altında not "Satış çubuğu yalnız maliyeti bilinen satışlardır … Toplam satış 48.100 ₺". Karşılaştırılan dönem de kendi kümesiyle: sunucu `onceki.bilinenHasilatTl` döndürür (canlı: 0, veri yok). Eksen yazıları SVG dışında 14 px + değer (aşağıda) |

## 2. Görünüm maddeleri

| Bulgu | Çözüm | Dosya |
|---|---|---|
| [ORTA/YÜKSEK] 375'te rapor tabloları sayı göstermiyor (Kâr-zarar Ürünler 1185 px / 351 px; Stok değeri toplamı görünmüyor) | Ortak `q10-yigin` kart yığını (her hücre `data-etiket` başlığıyla): `#kz-urun-tablo`, `#rapor-stok-tablo`, `#reyon-stok-tablo`, `#sy-olu-tablo`. Yığında toplam satırları (tfoot) da kart satırı olur (`rapor-gorunum.css`). Ürün adı kesilmez, sarar. Geniş ekranda tablo aynen | KarZarar.razor, Raporlar.razor, rapor-gorunum.css |
| [YÜKSEK] Grafik eksen yazıları ~6 px, değer yok, iki kart dikeyde hizasız | `CubukGrafik` yeni isteğe bağlı parametre `EtiketAltta` + `DegerBicimi`: etiket ve değer SVG'nin altında gerçek yazı boyunda (≥14 px), çubuk sütunlarıyla eşit genişlik. Parametre verilmezse SVG çıktısı birebir eskisi (Panel/Raporlar değişmez). Eksen etiketi kısa ("Satış") — dar sütunda kırılmaz. `#kz-grafikler > .kart { margin-top: 0 }` | CubukGrafik.razor, KarZarar.razor, rapor-gorunum.css |
| [ORTA] Her sekmenin altında 4 satırlık "Bu kayıt değiştirilemez" kutusu, iki farklı düzeltme yolu, "bakiye" | Kâr-zarar PRO: kapalı tek katlanır `#kz-duzeltme` "Rapordaki bir rakam yanlışsa" (Rapor merkeziyle aynı ad, ZoBelge sözlüğü); içinde rapora özgü TEK cümle `KR.RaporTuretilmisGerekce`, alternatif yol yok. BASİT "Bu rakamlar nereden geliyor?" içindeki kutu ve Rapor merkezi `#rp-duzeltme` da aynı tek cümle | KarZarar.razor, Raporlar.razor, Sozlukler.KarRapor.cs |
| [DÜŞÜK] Has-KISMI bandı tekil/çoğul + "(has)" | `KarRaporSozlugu.HasYalnizEksik(m, adet)`: 1 → "…; bu satış altın kârına katılmadı.", N → "…; bu satışlar …"; "(has)" eki kalktı (TR+EN) | Sozlukler.KarRapor.cs, KarZarar.razor |
| [DÜŞÜK] Ürünler'de TL maliyeti bilinen satırda "Altın maliyeti bilinmiyor"; "—" ile alt yazı hizasız; Külçe satırında gerekçesiz "—" | TL maliyeti bilinen satır → "Alış günü altın fiyatı yok" (`HasFiyatYokSatirNot`), TL de bilinmiyorsa eski metin. Has kârı null olan HER altın satırı not taşır (KISMI hariç; sunucunun `hasKarDurumu`'su null değilse). "—" + not tek blok `.kz-has-blok` (dikey ortalı, sağa yaslı) | KarZarar.razor, rapor-gorunum.css |
| [DÜŞÜK] Stok değeri: 0 stoklu gümüş "saf 0,00 g"; gümüş toplamı yok; toplam çizgisi yalnız son sütunda; altın has ile gümüş gram tek sayı gibi sıralı | Altın dışı madende tartı gramı biliniyorsa her satır "X g (saf Y g)" (`Bicim.MadenMiktariTam`, 0 stok "0,00 g (saf 0,00 g)"). Gümüş toplamı ayrı tfoot satırı `#rapor-stok-toplam-gumus` — sunucunun `madenDagilimi`'nden (sunucu `stok-degeri` maden dağılımına `fizikselGram` ekler; istemci toplamaz). Sıra: altın önce, maden içinde miktara göre. tfoot üst çizgisi tüm satırda | Raporlar.razor, RaporModulu.cs (StokDegeri maden dağılımı), RaporlarSozlesmeleri.cs, Bicim.cs |
| [DÜŞÜK] Reyon tablosu başlık/değer hizası | ADET ve MİKTAR başlıkları `sayi` (sağ) | Raporlar.razor |
| [DÜŞÜK] Akordeon/ızgara aralıkları (0–4 px) | `#rp-duzeltme`, `#kz-duzeltme` üst boşluğu, `#rapor-maden-dagilimi` alt boşluğu, stok yaşı açıklama/ⓘ satırı/ölü stok başlığı `var(--bosluk-…)` | rapor-gorunum.css |
| [DÜŞÜK] "NET HAS GRAM HAREKETİ — HAS (ALTIN)" | "Net altın hareketi (has)" (`KR.NetAltinHareketi`) | Raporlar.razor, Sozlukler.KarRapor.cs |
| [DÜŞÜK] PRO Özet: gümüş defter kartı altından önce; 6. kart "Dönem kârı" tek başına | Defter kartları altın önce; `#kz-tl-kpi` ≥64rem'de 3+3 | KarZarar.razor, rapor-gorunum.css |
| [DÜŞÜK] "Maliyeti eksik 9 satış, 95.566,52 ₺" iadeden netlenmiş | Eksik iade varken metin "… (iadeler düşülmüş) — kâra katılmadı" (`MaliyetiEksikSatirIadeli`) | KarZarar.razor, Sozlukler.KarRapor.cs |
| [DÜŞÜK] Panel kâr satırında "iade" tek başına alt satıra | "9 satış ve 1 iade" öbeği bölünmez boşlukla tek parça (Panel'de; sözlük metni değişmedi) | Panel.razor |

## 3. Sapmalar ve gerekçeleri
- **Dosya alanı dışı (zorunlu, gerekçeli):** `Kuyumcu.Web/Bilesenler/CubukGrafik.razor` (eksen yazıları bileşenin içinde; yalnız isteğe bağlı parametre — varsayılan çıktı birebir), `Kuyumcu.Web/Servisler/Bicim.cs` (+`HasIsaretli`, `Oran`, `GunSayisi`, `MadenMiktariTam` — denetim P-36b "biçim yalnız Bicim.cs'ten" kuralı gereği), `Kuyumcu.Web/Servisler/KarZararModelleri.cs` (+`KarZararOnceki.BilinenHasilatTl`), `Kuyumcu.Web/Servisler/RaporServisi.cs` (+`DevirDto.TahminiSatisSuresiGun/OrtalamaYasGun`), `Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs` (+`StokMadenToplami`, `StokYaniti.MadenDagilimi`), `wwwroot/index.html` (+1 satır `rapor-gorunum.css`, toplu6.css'ten sonra olcek.css'ten önce), `RaporModulu.StokDegeri` (maden dağılımına `fizikselGram`; gümüş toplam satırı için). Panel.razor'da "Bugün ne sattım" kartına ek olarak `HasMetni` yardımcısı ortak olduğu için "Kim borçlu" has satırı da değişti (YÜKSEK bulgu istiyordu; TOPLU-6'nın Panel alanı yalnız yasal liste bandı — çakışma yok).
- **Mühürlü test beklentileri güncellendi (gerekçe: bulgu metni/biçimi):** `MadenIkiTestleri` MI10 (tekil kalıp), MI14 ("Alış günü altın fiyatı yok"), MI30 (bölünmez öbek); `S31PanelSadeTestleri` S31_07 ("10,4000 has"), S31_12 ("−22,0000 has"); `Toplu4Testleri` T4 Kim borçlu ("12,3000 has"); `KarRaporTestleri` KR65 (PRO kutu kapalı katlanırda, tek cümle); `KarZararEkranTestleri.Sayfa_EnAzBirVeriGrafigiCizer` (eksen SVG dışında); `P57RaporBicimEkranTestleri` ("2,03", "181"); `P69HasAdlandirmaTestleri` P69_03 (`NetAltinHareketi`; "Toplam Has" yasağı aynen).
- `BugunHas` örnek (instance) metot olarak kaldı — ZOBELGE sınır testleri yansımayla `BindingFlags.Instance` arar (ilk tam süitte static yaptığım için 8 ZOBELGE testi kırmızıydı; düzeltildi, filtreli 542/542).
- Mutasyon betiği `perl` yerine Python ile TEK eşleşme kontrollü (sayım ≠ 1 ise atlar) değişiklik yaptı; geri kopya + `touch` + `cmp` (`filecmp shallow=False`).
- Canlı test kiracısında RG-K partisinin `birim_maliyet_has`'ı NULL'a, RG-T/RG-G partilerinin `giris_tarihi` 20/25 gün geriye SQL ile çekildi (yalnız kendi kiracım; has-KISMI ve yaş senaryosu — MADEN-2 §2 gerekçesi).
- Maden dağılımı (Rapor merkezi Özet) gümüşü yalnız "saf … g" yazar: defter (hareketler) fiziksel tartı gramı tutmaz, yalnız saf/has gram — BİLİNÇLİ (kayda geçti).

## 4. DoD sözleşmesi
Yeni varlık/kayıt türü YOK, yeni uç YOK, yeni düğme YOK. Yeni alanların hepsi türetilmiş salt okunur rapor alanı: mutabakat `hurda.altinHasGrami`, `hurda.madenDagilimi`; stok yaşı `devir.tahminiSatisSuresiGun`, `devir.ortalamaYasGun`; kâr-zarar `onceki.bilinenHasilatTl`; stok-değeri `madenDagilimi[].fizikselGram`. Rapor rakamları düzenlenmez: DegismezIpucu (rapora özgü tek cümle, "kaynağı olan işlemin kendi ekranından ters kayıtla") kapalı "Rapordaki bir rakam yanlışsa" katlanırında. [RolGerekli] uçlar değişmedi. Finansal tabloya yazma yok.

## 5. Testler
- Yeni dosya `Kuyumcu.Tests/RaporGorunumTestleri.cs` — **17 vaka**: DB RG01 (mutabakat hurda yalnız altın, gümüş ayrı), RG02 (gunluk satış has yalnız altın), RG03 (ortalama yaş 28,0 ≠ tahmini 450), RG04 (raf boşken yaş null), RG05 (onceki.bilinenHasilatTl); saf RG10 (tekil/çoğul TR+EN), RG11 (HasIsaretli); bUnit Raporlar RG20 (Bugün "26,0588 has"), RG21 (kart yığını, sıra, gümüş biçimi/toplamı, reyon hizası), RG22 (stok yaşı kartları ve ondalıklar), RG23 (tek cümle); bUnit Panel RG30 (Panel = Rapor merkezi "26,0588 has", Kim borçlu), RG31 (bölünmez öbek); bUnit Kâr-zarar RG40–RG43 (grafik kümesi + okunur eksen, Ürünler yığını + gerekçeli has hücresi, iadeli eksik metni + altın önce).
- Filtreli: `RaporGorunum` 17/17; geniş (ZOBELGE|RaporGorunum|MadenIki|P57|KarRapor|S31|Toplu4|P69|KarZarar) **542/542** (son kodla).
- Build (son, `b8.log`): **0 hata · 0 uyarı** (ağaçtaki 2 uyarı başka şeritlerin dosyasında: `SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455` xUnit2029 — bazı derlemelerde artımlı olduğu için görünmedi).
- **Tam süit 1** (`trx-pRAPOR-GORUNUM/_Mac_2026-10-03_05_31_31.trx`): toplam 10141 · geçti 10130 · başarısız 9 — 8'i benim (`ZOBELGESinirTestleri` ZOBS12/13, `ZOBELGESinirTestleriTur3` ZOBT3_01–06: `BugunHas`'ı static yapmıştım, yansıma `Instance` arıyor) → düzeltildi; 1'i başkasının (`Toplu6GiderTestleri.T6_Para_aktarimi_BASIT_kaydet_dugmesi…` — TOPLU-6 Virman).
- **Tam süit 2** (son kod): §5a'da.

### 5a. Tam süit 2 (son kod)
`/private/tmp/kuyumcu-serit/trx-pRAPOR-GORUNUM-son/_Mac_2026-10-03_05_38_53.trx`: **toplam 10141 · geçti 10137 · başarısız 2 · atlanan 2**. Kırmızılar: `SATIS2EkranTestleri.PRO_ust_satir_Musteri_ve_TEK_SATIR_ozet_kutular_KATLI_tiklaninca_acilir`, `SATIS3EkranTestleri.T13_ODAK_tek_basina_listeyi_ACMAZ_F2_ve_kisayol_F2_ACAR` — Satış ekranı (Islemler.razor; bu paket dokunmadı, o dosya eşzamanlı şeritte değişiyor). İkisi tek başına yeniden koşuldu: **2/2 geçti** (zamanlama/kararsızlık). RaporGorunum 17/17 ve 8 ZOBELGE testi trx'te Passed. **Bu paketin yeni kırmızısı 0.**

## 6. Mutasyon tablosu (betik `/private/tmp/kuyumcu-serit/RAPOR-GORUNUM/mut.py`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/RAPOR-GORUNUM/`, ham `ham/RAPOR-GORUNUM-olcum/mut-sonuc.txt`, `mut-sonuc-r.txt`)

| Kod | Değişiklik | Kırmızı test (adıyla) | cmp |
|---|---|---|---|
| M1 | mutabakat hurda has yine tüm madenler | RG01 | AYNI |
| M2 | ortalama yaş adet ağırlıksız | RG03 | AYNI |
| M3 | `onceki.bilinenHasilatTl` yok | RG05 | AYNI |
| M4 | Panel has eski "N1 g has" | RG30; S31_07; S31_12; T4_Kim_borclu | AYNI |
| M5 | grafik hasılat çubuğu yine toplam hasılat | RG41 | AYNI |
| M6 | has-yalnız eksik notu tekil/çoğul ayrımsız | MI10 (basit True/False) | AYNI |
| M7 | stok miktarı eski biçim (0 stok "saf 0,00 g") — son kodla tekrar | RG21 | AYNI |
| M8 | stok sırası (altın önce) kaldırıldı | RG21 | AYNI |
| M9 | stok yaşı oranı eski `Bicim.Miktar` — son kodla tekrar | RG22; P57 HasVeGramHucreleri | AYNI |
| M10 | PRO düzeltme kutusu eski iki yollu metin | KR65 | AYNI |
| M11 | `EtiketAltta` yok sayılır (SVG içi yazı) | RG41; Sayfa_EnAzBirVeriGrafigiCizer | AYNI |
| M12 | TL'si bilinen satırda yine "Altın maliyeti bilinmiyor" | RG42; MI14 (tr, en) | AYNI |
| M13 | iadeli eksik metni hiç seçilmez | RG43 | AYNI |
| M14 | Ürünler tablosunda kart yığını sınıfı yok | RG42 | AYNI |
| M15 | Panel eksik öbeği bölünebilir | RG31; MI30 | AYNI |
| M16 | PRO defter kartları altın önce değil | RG43 | AYNI |

16/16 kırmızı, 16/16 cmp AYNI; sonra temiz `dotnet build Kuyumcu.sln` 0/0.

## 7. Canlı doğrulama (API 5160 `dotnet run --no-build`, Development; ham `ham/RAPOR-GORUNUM-olcum/canli-olc.txt`, `sql-bagimsiz.txt`)
Kiracı **PAKETRAPOR-GORUNUM-TEST** (01M3ZQAZ19CT7PTHJJ7968DDB3). Kurulum: davet 201, kayıt 201, giriş 200, ürün 201×5, cari 201×2, alış 200×4; satış 200×6, iade 200, kasa 200, kasa girişi 200, hurda alış 200×2 (K22 10 g, G925 100 g).

| Uç | HTTP | API | Bağımsız SQL |
|---|---|---|---|
| `/raporlar/gunluk` | 200 | satis.toplamHas **8.752** = altinToplamHas 8.752; madenDagilimi GUMUS 31.45 ayrı | SATIS ALTIN Σ\|has\| **8,752000** (4), GUMUS 31,450000 (2) |
| `/raporlar/pos-fatura-mutabakati` | 200 | hurda.sayisi 2 · **hasGrami 9.16** · altinHasGrami 9.16 · madenDagilimi [ALTIN 9.16, GUMUS 92.5] | HURDA_ALIS ALTIN **9,160000**, GUMUS 92,500000; eski hatalı toplam 101,660000 |
| `/raporlar/stok-yaslandirma` | 200 | kovalar G0_30 = 7 · devirHizi 0.7143 · yillik 2.8969 · ortalamaRafOmruGun 126 = **tahminiSatisSuresiGun 126** · **ortalamaYasGun 13.6** · kalan 7 · tüketilen 5 | kalan 7,000 · ort_yas **13,6** · en yaşlı 25 gün; tüketim(90 gün) 5,000 → 7×90/5 = 126 |
| `/raporlar/kar-zarar?…&karsilastir=ONCEKI_DONEM` | 200 | KISMI · hasilatTl 48100 · **bilinenHasilatTl 44100 − maliyetTl 31200 = karTl 12900** · eksik 2 satış + 1 iade (4000) · hasKarDurumu KISMI · yalnizHasEksik 1 · onceki.bilinenHasilatTl 0 | hasılat (SATIS − SATIS_IADE) **48100,00** |
| `/raporlar/stok-degeri` | 200 | toplamHas 8.752 · madenDagilimi GUMUS 83.25 / **fiziksel 90.0**, ALTIN 8.752 / 11.0 · RG-Z adet 0 | — |
| `/raporlar/kar-zarar/urun` | 200 | RG-T TAM 0.906 has · RG-K HESAPLANAMADI (TL 6000, maliyet 15000) · RG-A HESAPLANAMADI (TL de yok) | — |

Temizlik: API (PID) ve vekil `kill`; 5160/5161/9709 dinleyen yok. `zsh kiraci-temizle.sh 01M3ZQAZ19CT7PTHJJ7968DDB3 --uygula` → tenants/hareketler/stok_partileri bu kimlik için **0**. tenants: başlangıç **15** → son **18** (benimki silindi; +3 eşzamanlı şeritlerin kiracıları — listede `PAKETPATCH-5-TEST`×2, `PAKET-TOPLU-6-TEST` vb.; dokunulmadı). QA-TEST-* (4) dokunulmadı.

## 8. Gerçek tarayıcı (başsız Chrome `betikler/tur/tur.mjs` + sikisma-olc, CDP 9709, vekil 5161; son tur ham `ham/RAPOR-GORUNUM-olcum/olcum-1.jsonl`, `analiz.txt`, ilk tur `analiz-tur1.txt`)
270 kayıt (36 sayfa + 78 sekme + 156 katman), **hata 0 · geçersiz 0**, 12 kombinasyon (1440/1280/375 × BASİT/PRO × açık/koyu).

| Rota | 1440 | 1280 | 375 | Not |
|---|---|---|---|---|
| `/` (Panel) | temiz ×4 | temiz ×4 | temiz ×4 | taşan 0 · kırık 0 · binen 0 · yatay 0 · gizli-görünen 0 · TRY 0 |
| `/raporlar/kar-zarar` Grafik / Köprü / Ürünler / Stok | temiz ×4 | temiz ×4 | temiz ×4 | ilk turda Grafik'te K3 "Maliyeti bilinen satış" kırılıyordu → etiket "Satış" + not; ikinci tur temiz |
| `/raporlar/kar-zarar` Özet | temiz | temiz | K4 tren 3.4x5 (PRO a/k) | önceden var (MADEN-2: 3.6x4), ölçer ⓘ summary |
| `/raporlar` Özet/Cari/Grafik/Stok/Stok yaşı | K12 `#rp-yil/#rp-ay` ezik (PRO) | aynı | K4 tren (PRO) | K12 önceden var (TOPLU-6 alanı: yıl/ay seçicisi); Stok değeri 375 K4 2.2x4: kart yığını sayfayı uzatır (sayılar artık görünür — bulgunun istediği takas) |
| `/raporlar` Gün sonu | K12 | K12 + K4 | K4 | DEĞİŞİM-2 alanı, dokunulmadı |

Görüntüler Read ile GÖRÜLDÜ (`ham/RAPOR-GORUNUM-olcum/foto/`):
- `kok__BASIT_aydinlik_375.png`: "6 satış 8,7520 has", "Kâr 12.900 ₺ · maliyeti eksik: 2 satış ve 1 iade" TEK satır, "Altın pozisyonum +17,9120 has · Vitrin 8,7520 has · Alacak 0,0000 has", Kim borçlu.
- `raporlar__BASIT_aydinlik_1280.png`: Rapor merkezi Bugün "6 satış 8,7520 has" (Panel ile AYNI), hurda "9,1600 has".
- `raporlar_kar_zarar__PRO_aydinlik_1280__sekme1.png` / `…_375__sekme1.png`: çubuklar 44.100 / 31.200 / 12.900 / 12.900, etiket+değer okunur, not "Toplam satış 48.100 ₺", iki kart aynı üst çizgide.
- `raporlar_kar_zarar__BASIT_karanlik_375__sekme3.png`: Ürünler kart yığını — her üründe Satış/Hasılat/Maliyet/İade/Kâr/HAS kâr/Marj görünür; RG-K "— Alış günü altın fiyatı yok", RG-A "— Altın maliyeti bilinmiyor".
- `raporlar_kar_zarar__PRO_aydinlik_1440.png`: TL kartları 3+3, "Maliyeti eksik 2 satış, 4.000 ₺ (iadeler düşülmüş)", has bölümünde "Defterdeki altın" gümüşten önce, "1 altın satışının alış günü altın fiyatı yok; bu satış altın kârına katılmadı.", altta kapalı "Rapordaki bir rakam yanlışsa".
- `raporlar__PRO_aydinlik_375__sekme3.png` / `…karanlik_375__sekme3.png`: Stok değeri kart yığını, altın önce, "0,00 g (saf 0,00 g)", "ALTIN (HAS) 8,7520 has", "GÜMÜŞ TOPLAMI 90,00 g (saf 83,25 g)", reyon kartı.
- `raporlar__PRO_aydinlik_1280__sekme4.png`: Stok yaşı 0-30 gün 8,7520 has · Devir hızı 0,71 · Yıllık 2,90 · Ortalama yaş 14 · Tahmini satış süresi 126 · ölü stok boş metni; aralıklar ayrık.

## 9. Değişen dosyalar (bu paketin dokunduğu; `git status --porcelain | grep -v -E "/(bin|obj)/"` süzümü)
```
 M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs           (mutabakat hurda altın/maden dağılımı; StokDegeri maden dağılımı fizikselGram)
?? dotnet/Kuyumcu.Api/Modules/Rapor/StokYaslandirmaRaporu.cs  (ortalamaYasGun, tahminiSatisSuresiGun)
?? dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs      (yalnız onceki.bilinenHasilatTl)
 M dotnet/Kuyumcu.Web/Pages/Panel.razor                      (HasMetni → Bicim.HasIsaretli; kâr satırı bölünmez öbek)
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor                   (Bugün has kaynağı, Stok değeri, reyon, Stok yaşı, düzeltme notu, net altın etiketi — Gün sonu ve yıl/ay seçicisi HARİÇ)
?? dotnet/Kuyumcu.Web/Pages/KarZarar.razor
 M dotnet/Kuyumcu.Web/Bilesenler/CubukGrafik.razor            (EtiketAltta / DegerBicimi — isteğe bağlı)
 M dotnet/Kuyumcu.Web/Servisler/Bicim.cs                      (+HasIsaretli, Oran, GunSayisi, MadenMiktariTam)
?? dotnet/Kuyumcu.Web/Servisler/KarZararModelleri.cs          (+KarZararOnceki.BilinenHasilatTl)
?? dotnet/Kuyumcu.Web/Servisler/RaporServisi.cs               (+DevirDto alanları)
?? dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs  (+StokMadenToplami, StokYaniti.MadenDagilimi)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KarRapor.cs             (+12 anahtar TR+EN, HasYalnizEksik)
?? dotnet/Kuyumcu.Web/wwwroot/css/rapor-gorunum.css           (yeni şerit dosyası; sabit renk yok)
 M dotnet/Kuyumcu.Web/wwwroot/index.html                      (+1 link satırı)
?? dotnet/Kuyumcu.Tests/RaporGorunumTestleri.cs               (yeni, 17 vaka)
?? dotnet/Kuyumcu.Tests/{MadenIkiTestleri,S31PanelSadeTestleri,Toplu4Testleri,KarRaporTestleri,KarZararEkranTestleri,P57RaporBicimEkranTestleri,P69HasAdlandirmaTestleri}.cs (beklenti güncellemeleri, §3)
```
Dokunulan sayfalar: Panel.razor, Raporlar.razor, KarZarar.razor (gerekçe: MADEN-2'nin devir bulguları bu üç ekranı açıkça istiyor; Raporlar Gün sonu sekmesi ve yıl/ay seçicisine dokunulmadı).

## 10. Denetim
`bash denetim.sh`: 303 ✓ · tek ✗ `P-30: DEMO_INSAN_SURESI (BK-45)` (muaf) → **TEMİZ (P-30 hariç)**. İlk koşumda `P-36b: GIRDI_AILESI_DISI_BICIM 3` (razor içinde `ToString("N2")`) çıktı → biçim `Bicim.cs`'e taşındı, tekrar koşumda ✓. Satır içi style yok, yeni CSS'te sabit renk yok, Pages altında yeni ModalPencere yok.

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Panel ve Rapor merkezi aynı sayı, tek kaynak (Bicim.Has, yalnız altın) | KARŞILANDI | RG20/RG30/RG02; canlı 8,752 = SQL = iki ekran "8,7520 has" |
| (b) mutabakat hurda yalnız altın, gümüş ayrı | KARŞILANDI | RG01, M1; canlı 9,16 / gümüş 92,5 = SQL |
| (c) raf ömrü ↔ 0-30 gün — hangi hesap doğru, API=SQL | KARŞILANDI | §1; RG03/RG04/RG22, M2/M9; canlı 126 / 13,6 = SQL |
| (d) grafik hasılat çubuğu — veri mi çizim mi | KARŞILANDI (çizim) | §1; RG41/RG05, M3/M5/M11; canlı 44100−31200=12900 |
| 375'te rapor tablolarında sayılar görünür | KARŞILANDI | RG21/RG42, M14; görüntüler §8 |
| Eksen yazıları okunur, düzeltme kutusu tek satır, aralıklar, tekil/çoğul, 'has' iki kez, gümüş toplamı/biçimi, altın önce | KARŞILANDI | §2; RG10/RG21/RG23/RG43, KR65, P69_03; görüntüler |
| Mutasyon ≥1 kırmızı, cmp AYNI | KARŞILANDI | §6, 16/16 |
| Başsız Chrome 1440/1280/375 × BASİT/PRO × açık/koyu, Read | KARŞILANDI | §8 |
| build 0/0 | KARŞILANDI | `b8.log` 0 hata 0 uyarı |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | §10 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | §5a (2 kırmızı Satış ekranı, tek başına 2/2 yeşil) |
| Test kiracısı silindi | KARŞILANDI | §7 |

## 12. Açık kalanlar (sahibiyle)
- `/raporlar` K12 `#rp-yil/#rp-ay` ezik (PRO 1280/1440) → **TOPLU-6** (Raporlar yıl/ay seçicisi onların alanı).
- Gün sonu sekmesi K4 tren (1280/375) → **DEĞİŞİM-2**.
- Ölçer K4 "tren" 375 PRO (Kâr-zarar Özet 3.4x5, Rapor merkezi Özet/Stok yaşı/Stok değeri): sayfa boyu > 2 ekran ve ≥ 4 bölüm; Stok değeri'nde kart yığını sayıları görünür yapmanın doğal sonucu → ölçer iyileştirmesi / TOPLU.
- Son tam süitte kararsız (tek başına yeşil): `SATIS2EkranTestleri.PRO_ust_satir…`, `SATIS3EkranTestleri.T13_ODAK…` (Satış ekranı sahibi). İlk tam süitte başkasına ait kırmızı: `Toplu6GiderTestleri.T6_Para_aktarimi_BASIT_kaydet_dugmesi_ve_aciklama_tek_ad` (TOPLU-6). Ara koşumlarda görülen `RAPORYASLANDIRMASinirTestleri.Kurus_yuvarlamasi…` (Cari DuzeltmeYaz, Cari şeridi) ve `PatchBesHttpTestleri.P5_kasa_elle_hareket…komisyonOran` (PATCH-5) son tam süitte görülmedi/geçti — bu paketin dosyasına bağlı değil.
- Panel PRO kart içerikleri ("Son işlemler" kırılımı, "Son 7 Günlük Satış" ekseni, "Kur (24K Altın)" iki kez) → Panel PRO sahibi (bulgu KAPSAM DIŞI). Not: `CubukGrafik` artık `EtiketAltta` seçeneği sunuyor; Panel sahibi aynı parametreyle eksen okunurluğunu tek satırda alabilir.
- Has-bilinen ama kursuz satış asimetrisi (KarZararServisi `HasCevir`, kuramsal) → ileride has-bilinen küme `AltinHasFiyati > 0` ile sınırlansın (bu paketin KRİTİK'i değil; KarZararServisi'nde bu pakette yalnız grafik verisine dokunuldu).
- Hub sözlüğünde artık okunmayan `RaporHasSozlugu.NetHasAltin` ve `Dil.T.RaporSeti.OrtalamaRafOmru` anahtarları → hub temizliği TOPLU kuyruğu (hub'a dokunulmadı).

## 13. Kurucu / mali müşavir
Mevzuat hükmü içermez (rapor gösterim/hesap kuralları: has yalnız altın, tahmini satış süresi tanımı, grafik kümesi). Not: "Tahmini satış süresi" bir öngörüdür (bugünkü satış hızı sabit varsayımı), defter/beyan bilgisi değildir.
