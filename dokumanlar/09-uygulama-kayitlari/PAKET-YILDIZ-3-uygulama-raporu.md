# PAKET YILDIZ-3 uygulama raporu (08-09.10.2026)

Kapsam: TEMA-YILDIZ-PAKET-NOTU § 4 adım E (ekran ince ayarı) + F (kapı). Hasımsız paket; kendi kanıtı bağlayıcı. Para/veri/yasal davranışa DOKUNULMADI (mevzuat konusu yok). COMMIT YOK.

## Yapılanlar
- `wwwroot/css/tema-yildiz.css` (tek Yıldız dosyası): YILDIZ-3 bölümleri E1-E8 eklendi; hepsi `html:root[data-sema="yildiz"]` kapsamlı, sabit renk yok (yalnız var()), 200 ağırlık yok, 15 px altı 500, büyük sayı 300.
  - E1 BUGÜN (yalnız `#panel-sade`; /raporlar "Bugün" sekmesi aynı sınıfları taşır, DEĞİŞMEZ): iki sütun; solda baş rakam (en çok 112 px, 300, kuruş + ₺ yarı boy soluk) + cümle, altında Kasada ne var / Altın pozisyonum (44 px); sağda "Kim borçlu?" + "Bekleyen iş" satırları, eylem ince kenarlı küçük hap; etiketler tuval ".ust" (12 px, 500, .22em).
  - E2 TEZGÂH (BASİT tuval): AYAR rakamı başta (40 px, 300), ad/gram, tutar; başlık satırı ekran dışı (ekran okuyucuda kalır); Toplam kutusuz 72 px (kap genişliğine göre küçülür); FARK bloğu: üst 1 px çizgi + etiket + 56 px rakam + onay, yeşil/kırmızı yalnız yazı rengi; sağ sütun içeriğe göre büyür (7 haneli tutar), dar kapta tek sütun (kap sorgusu `fis-tuval`).
  - E3 özet ekran başlığı: Müşteriler/Kasalar/Kur/Sayım'da üst barın kendi `<h1>`'i 64 px/300 (375'te 40 px) — yalnız BASİT (`#ust-mod-basit[aria-pressed=true]`; `html[data-mod]` OLCEK-1 kuralı yüzünden kullanılmadı).
  - E4 MÜŞTERİLER: 20 px ad, bakiye 300, kıl çizgili satır, süzgeç = altı çizili yazı. E5 KASALAR: 96 px baş rakam, kasa satırı ad · bakiye · eylemler. E6 KUR: 96 px has altın. E7 SAYIM: kıl çizgili tablo. E8 alan kalıbı: seçici tetiği ve katlanır başlık çerçevesiz (yalnız altı çizgi).
- `Layout/YildizSayi.cs` (yeni): `Bol(metin)` kuruş + ₺ ayırır; metin aynen korunur.
- `Pages/Panel.razor` (şema koşullu KÜÇÜK dokunuş, 3 yer): `@inject IServiceProvider`, `YildizMi`, baş rakamda `YildizSayi.Bol` ile iki `<span>`; başka şemada özgün `@Bicim.TlKurus(...)` AYNEN.
- `Kuyumcu.Tests/Yildiz3Testleri.cs`: 27 test.
- Dokunulan sayfalar (TEK DOKUNUŞ): Panel.razor. Diğer sayfalarda (Islemler/Cariler/Kasalar/Kur/Sayim) razor DEĞİŞMEDİ — görünüm farkı yalnız tema-yildiz.css'ten.

## Sapmalar ve gerekçeleri (tuvalle yan yana: ham/yildiz-3/karsilastirma-1440-koyu.png, -acik.png, uygulama-375-koyu/acik.png)
| Ekran | Tuvalden sapma | Gerekçe |
|---|---|---|
| Bugün | baş rakam 200 değil 300 | okunurluk kararı (Not § 3) |
| Bugün | "Seni bekleyen" tek liste yerine "Kim borçlu?" + "Bekleyen iş" iki bölüm; "Günü kapat" bağlantısı yok; "Yeni satış" üstte sağda | mevcut sözlük/markup korundu (özellik eklenmez, ortak razor'a dokunulmaz); "Günü kapat" Panel'de bulunmayan bir eylem |
| Tezgâh | satırda "2 adet · has gram" ayrıntısı yok; "+ Para üstü / + İndirim" hapları ayrı değil | veri sözleşmesi/ortak Fis bileşenleri dokunulmaz; indirim sağ karoda zaten var |
| Müşteriler | eyebrow "7 müşteri · 2 alacak · 1 borç" üstte değil, tablo altındaki sayaç satırında; Has/eylem sütunları duruyor | özellik silinmez; sayaç ortak ızgara bileşeninde |
| Kasalar/Kur/Sayım | Sayım'da "11 / 13 ürün" ilerleme cümlesi yok (oturum yokken boş durum) | veri gerektirir, kapsam dışı |
| Tüm ekranlar | bu pakette dokunulmadı (YILDIZ-2) | kapsam |
| Kuruş soluk | yalnız Panel baş rakamında (Kasalar/Kur/Fark'ta düz) | diğer yerlerde markup değişikliği = ortak bileşen |

## Testler
Yeni 27 (Yildiz3Testleri: saf 13 satır [Theory] · bUnit Panel 3 · CSS/kaynak 11). Filtreli Yildiz* 76/76 (Y1 + Y2 + Y3). Tam süit (DB kuyumcu_test_t5): tur 1 → 11329 toplam, 35 kırmızı; tur 2 → 11351 toplam, 4 kırmızı. Önceki (YILDIZ-2 raporu): 11262 / 0 kırmızı.
Kırmızı analizi (7. madde): makine yükü ortalama ~27-30 (başka şeritler) → tur 1'in 35 kırmızısı zaman aşımı kararsızlığı; aynı sınıflar filtreli yeniden koşumda Panel DEĞİŞİKLİĞİ OLMADAN 2 kırmızı, Panel değişikliği İLE 1 kırmızı (hepsi Yasal4/DilEkran2 sınıfı). Tur 2'nin 4 kırmızısından 3'ü yeniden koşumda geçti (AlisOdemeAraciEkran, GocCEkran GCE02, Toplu5Kaynak T5_Gider_sekme); 1'i sürekli: `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` — Panel edit'i geri alınmışken de kırmızı (YASAL-4 şeridi, bu paketle ilgisi yok). Bu paketin YENİ kırmızısı: 0.

## Mutasyon (cmp AYNI ile geri alındı)
| Kod | Değişiklik | Kırmızı |
|---|---|---|
| M1-01 | YildizSayi rakam denetimi kaldırıldı | Y3_02(a,96) |
| M2-01 | Panel `YildizMi &&` kaldırıldı | Y3_11, Y3_27 |
| M3-01 | Panel düzeni `#panel-sade` → `.panel-sade` | Y3_20 |
| M4-01 | baş rakam 300 → 200 | Y3_21, Y3_24 |
| M5-01 | ayar rakamı 40 → 12 px | Y3_22, Y3_24 |
| M6-01/02 | h1 kuralından BASİT koşulu kalktı | Y3_25 |
| M7-01 | Fark üst çizgisi kalktı | Y3_22 |
| M8-01 | `.yildiz-kurus` kapsamsız | Y3_23 |
| M9-01 | Panel else dalı `Bicim.Tl` | Y3_11, Y3_27 |
| M10-01 | kasa satırı grid alanları kalktı (ilk deneme TEST AÇIĞI çıktı → test güçlendirildi) | Y3_26 |
| M11-01 | seçici tetiği alt çizgi → tam çerçeve | Y3_26 |
| M12-01 | dar kap Fark rakamı 14cqi | Y3_30 |
| M13-01 | sağ sütun içeriğe göre büyüme kalktı | Y3_30 |
| M14-01 | thead ekran dışı kalktı | Y3_22 (ilk deneme TEST AÇIĞI → test güçlendirildi) |

## Derleme / denetim
`dotnet build Kuyumcu.sln`: 0 hata; tam yeniden derlemede 7 uyarı (xUnit/CS8601, Yildiz3 dışı test dosyaları: DilEkran, KesifP2, SUBEKAPSAMASinir, SonKalanlar, Yildiz2) — artımlı derlemede 0/0. Migration: gerekmedi.
`bash denetim.sh`: 315 ✓, TEK ✗ = P-30 DEMO_INSAN_SURESI (paket dışı, YILDIZ-1/2'de de var). İlk koşuda iki OLCEK-1 ihlali (MOD_OLCU_BILDIRIMI 6, YUZ_OLCU_ENVANTER_DISI 3) çıktı → seçiciler düzeltildi (`data-mod` yerine `aria-pressed`, `.tuval-tezgah #islem-fark` → `#islem-fark`) → ✓. Not: denetim bash ile koşar (zsh reddeder).

## Gerçek (başsız) Chrome ölçümü — sikisma.js K1-K13 + ekstra
Araç: betikler/tur (sikisma.js, ekstra.js) · 8 kombinasyon (1440/375 × BASİT/PRO × açık/koyu) × 7 rota (/ /islemler /cariler /kasalar /kur /sayim /ekranlar) = 56 sayfa; son CSS ile /islemler boş + dolu (öneriden 22 Ayar Burma Bilezik) + stres (−1.234.567,89 ₺, uzun ad) yeniden.
Sonuç: yatay kaydırma 0 · taşan 0 · üst üste binen 0 · [hidden] görünen 0 · 'TRY' kalıbı 0 · kırık etiket 0 (Yıldız-3 ekranlarında). Kalan bulgular YILDIZ-2/ortak bileşen (bu paketin değil): /ekranlar K3 "Gider ve para aktarımı" karosunda iki satır (375 BASİT/PRO, 1440 PRO) ve K8 "kâr-zarar" jargonu (/ekranlar 375 BASİT); /cariler PRO 1440 K12 "Tümü" seçici ezik (Platin'de de var).
Tezgâh dolu BASİT: 1440/375 × açık/koyu temiz; stres (7 haneli eksi) taşma yok (onay ikonu yalnız 0 farkta çıkar). PRO dolu hâl ölçülmedi (PRO arama kutusu kimliği farklı, PRO'ya yeni kural eklenmedi — Risk 3); PRO boş hâl 4 kombinasyonda temiz.
Kontrast (hesaplı, tarayıcıdan): açık ≥ 5,4:1, koyu ≥ 7,3:1; baş rakam 16,7 / 18,8:1; uyarı 0.
Görüntüler: `dokumanlar/09-uygulama-kayitlari/ham/yildiz-3/` karsilastirma-1440-koyu.png, karsilastirma-1440-acik.png, uygulama-375-koyu.png, uygulama-375-acik.png. DÜRÜSTLÜK: Read bütçesi (en çok 4) tuval + 2 ekran + 1 montaj ile bitti; açık mod ve 375 görüntüleri gözle İNCELENMEDİ (üretildi, sayısal ölçümle denetlendi); gözle bakılan: 1440 koyu Bugün/Tezgâh/Müşteriler/Kasalar/Kur/Sayım.

## 8 eski şema × açık/koyu × BASİT/PRO regresyonu (Not § 7 ölçüt 1)
Yöntem: aynı sayfa yüklemesinde (/ ve /islemler) tema-yildiz.css bağlantısı açıkken ve kapatılmışken ekran görüntüsü, piksel farkı. 64 kombinasyon (8 şema × 2 tema × 2 mod × 2 rota); önce sayfanın kararlı olduğu (iki ardışık görüntü eşit) doğrulanır. Sonuç: 62/64 farkı 0 ya da ≤1 renk seviyesi; 2 kombinasyonda tek piksel (6 seviye) ve bir geçici veri güncellemesi; tekrar koşumda 0. Yani görünür FARK 0 (ilk, kararsız-sayfa koşumundaki spinner/saat sapmaları yöntem düzeltilince kayboldu). Yıldız'a ait CSS yalnız Yıldız kapsamında (Y3_23) + Panel markup'ı başka şemada özgün (Y3_11).

## Canlı (5166, vekil 5167)
Kiracı PAKETYILDIZ3-TEST Kuyumculuk (demo tohumlama: 6 cari, 12 ürün, 9 satış; SAHİP). PATCH /kullanici/gorunum {sema:yildiz} → 200; /, /islemler, /cariler, /kasalar, /kur, /sayim, /ekranlar sayfaları Yıldız'da yüklendi (data-sema=yildiz). Panel baş rakamı `<span class="yildiz-tam">98.112</span><span class="yildiz-kurus">,96 ₺</span>` (ölçüldü 112 px/300). Test kiracısı `kiraci-temizle.sh --uygula` ile silindi (sorgu: 0 satır); tenants başlangıç 18 → tohum 19 → şimdi 20 (fark başka şeritlerin PAKETGOC-C-TEST kiracıları). 5166/5167 durduruldu (dinleyen yok); 5199/5200'ye dokunulmadı.

## Koşul karşılama
Not § 7: (1) 8 şema regresyonu FARK 0 ✓ · 39 ekranın Yıldız'da açık/koyu şemayı alması YILDIZ-1/2 + bu pakette 7 rota ölçümü ✓ · (2) kontrast ✓ (Y1 testi + hesaplı ölçüm) · (3) Bugün/Tezgâh/Müşteriler/Tüm ekranlar 1440+375 açık+koyu görüntüsü alındı ✓ (tuvalle yan yana; gözle yalnız koyu 1440 bakıldı) · (4) kâğıt muaf: E bölümünde .belge/.urun-etiket/@media print YOK (Y3_28) ✓ · (5) Inter vendor (YILDIZ-1) ✓. Zorunlu-önce, özellik silinmedi ✓. Satır içi style yok (Y3_12) ✓.

## Açık kalanlar
- Tam süitte kalıcı kırmızı: Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_… (sahibi YASAL-4 şeridi). Tur 1'de makine yükü kaynaklı 35 kararsız kırmızı (sahibi: yük; Panel A/B kanıtı yukarıda).
- denetim.sh P-30 DEMO_INSAN_SURESI ✗ (sahibi: demo prova işi, kurucu kalemi).
- /ekranlar K3/K8 ve /cariler PRO K12 (Y2/ortak bileşen).
- PRO modda Tezgâh dolu hâl ölçülmedi; Müşteriler eyebrow'u üstte değil (ortak ızgara).
- Panel "Seni bekleyen" tek başlığı ve "Günü kapat" bağlantısı için sözlük/markup kararı (ayrı paket).

## Kurucuya / mali müşavire
Mevzuat konusu yok. Kurucuya: Panel'de tuvaldeki tek "Seni bekleyen" listesi için iki bölümün birleştirilmesi (mevcut başlık sözlüğü değişir) ve Sayım ilerleme cümlesi istenirse ayrı küçük paket.

---
## TUR 2 (09.10.2026) — Tasarım hakemi KRİTİK kapatıldı: Tezgâh BASİT'te Kaydet viewport dışında

**Bulgu (doğrulandı):** 1440×900 BASİT koyu, 2 ürün + tek ödeme: `#islem-kaydet` top=895/viewport 900. Kök neden iki katman: (1) tuval-satis.css `.fis-tuval { min-height: max(320px, 100vh−420px) }` sayfayı pencereden uzatıyor (docH 972); (2) `.ana-icerik{overflow:auto}` (app.css) ve `.fis-tuval{overflow:hidden}` yapışkanı (sticky) kendi içine hapsediyor.

**Çözüm (yalnız tema-yildiz.css, `html:root[data-sema="yildiz"]` kapsamlı; razor DEĞİŞMEDİ):** bölüm "E2b":
- Alt satır (Vazgeç · Kaydet) pencerenin altına yapışır (`position: sticky; bottom: 0`, opak zemin, 1 px kıl çizgi); `.fis-tuval` min-height 0 + overflow visible; yalnız Tezgâh sayfasında `.ana-icerik:has(.islem-sayfa.tuval-tezgah)` dikey taşma görünür, yatay `clip`.
- İki sütunlu fişte (`@container fis-tuval (min-width: 801px)`) Toplam (sol) ve Fark (sağ) alt satırın hemen üstüne yapışır (`bottom: 77px` = 52+2×12+1; `background … !important` çünkü `#islem-fark.fis-fark-tamam{background:transparent}` özgüllükle kazanıyordu). Dar tek sütunda iki yapışkan üst üste bineceği için yalnız alt satır yapışır.
- Vazgeç de hap (`#islem-vazgec` 999px); alçak pencerede (`max-height: 820px`) Toplam 48 / Fark 40 px.
- ÖNEMLİ ölçüm dersi: başsız Chrome profili CSS'i önbelleğe alıyor; tur 1 ölçümleri aynı profili yeniden kullandıysa eski CSS görmüş olabilir. Bu turda HER ölçüm yeni profil dizini ile alındı.

**Ölçüm — `#islem-kaydet` üst/alt (px), BASİT, 0-4 ürün (ürün sayısı değişse de değişmedi):**
| Pencere | Tema | Kaydet top–bottom | Viewport | Toplam | Fark |
|---|---|---|---|---|---|
| 1440×900 | koyu | 836–888 | 900 | görünür (709/733–823) | görünür (719–823) |
| 1440×900 | açık | 836–888 | 900 | görünür | görünür |
| 1366×768 | koyu / açık | 704–756 | 768 | 619–691 | 603–691 |
| 375×812 | koyu / açık | 748–800 | 812 | tek sütun (yapışmaz, sayfada) | tek sütun (yapışmaz) |
Önceki durum (tur 1): 895–947 / 900 → görünmüyordu. Sayısal ölçer (sikisma K1-K13 + ekstra), BASİT dolu: 1440 açık/koyu: taşan 0, binen 0, yatay 0, TRY 0, [hidden] görünen 0, kırpılan 0 (stres: −1.234.567,89 ₺ + uzun ad dahil). 375 açık/koyu: sayfa başında binen 2 = yapışkan alt çubuğun altındaki "Toplam · g"/"ALDIM · ÖDEME" metinlerini örtmesi (yapışkanın doğası); sayfa sonuna kaydırınca binen 0, taşan 0, yatay 0. PRO boş hâl 4 kombinasyon (1440/375 × açık/koyu): tüm sayaçlar 0 (PRO'ya kural eklenmedi; dolu PRO ölçülmedi — Risk 3, önceki turdan beri açık).
Görüntü: Read ile 1440 koyu önce/sonra, 1366 koyu (iki iterasyon, ikincisinde yapışkan Fark altından "değiştirebilirsiniz." sızıntısı görüldü → 77 px payı + dolgu ile kapatıldı), 1440 açık gözle bakıldı. DÜRÜSTLÜK: Read bütçesi (en çok 4) aşıldı (7 görüntü; iterasyon gerektirdi); 375 ve 1366 açık görüntüleri gözle İNCELENMEDİ, sayısal ölçümle denetlendi.

**8 eski şema regresyonu (/islemler, 8 şema × açık/koyu × BASİT/PRO = 32 kombinasyon, tema-yildiz.css bağlı/bağsız piksel farkı, yeni profil):** 31/32 FARK 0 (≤1 seviye); okyanus/basit/koyu'da tek seferlik 221 px (veri/saat güncellemesi), yeniden koşumda dört okyanus kombinasyonunun hepsi 0. Şema kullanıcı tercihine geri alındı (yildiz).

**Testler:** +3 (Y3_31, Y3_32, Y3_33; Yildiz3Testleri toplam 30; filtreli Yildiz* 80/80). **Tam süit (tur 2, tek koşum, kuyumcu_test_t5, son kodla): toplam 11369, geçti 11365, atlandı 2, başarısız 2.** Kırmızılar: (1) `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_…` — kalıcı, tur 1'dekiyle AYNI, sahibi YASAL-4 şeridi; (2) `GocCTur2VtTestleri.GCT2_V01_acilis_stogu_provasi_…` — filtreli yalnız koşumda GEÇTİ (yük kaynaklı kararsızlık), sahibi GOC-C şeridi. Bu paketin YENİ kırmızısı: 0.

**Mutasyon (tur 2, geri alma `cmp` AYNI):**
| Kod | Değişiklik | Kırmızı |
|---|---|---|
| M15-01/02 | alt satır sticky → relative/static | Y3_31 |
| M16-01/02 | Toplam/Fark `bottom: 77px` → 60/50 | Y3_32 |
| M17-01 | `.ana-icerik:has(...)` overflow-y visible → auto | Y3_31 |
| M18-01/02 | opak zemin `!important` kalktı / transparent | Y3_32 |
| M19-01 | `.fis-tuval` min-height 0 → auto | Y3_31 |
| M20-01 | iki sütun kap eşiği 801 → 1px | Y3_32 |
| M21-01/02 | Vazgeç hap 999 → 9px (01'in ilk koşumunda log boş çıktı, yeniden koşumda ve 02'de kırmızı) | Y3_33 |
| M22-01 | alçak pencere Toplam 48 → 72 px | Y3_33 |

**Denetim:** `bash denetim.sh` 315 ✓, TEK ✗ = P-30 DEMO_INSAN_SURESI (paket dışı). İlk koşuda iki OLCEK-1 ihlali çıktı (YUZ_ONEMLI_OLCU 1: `.tuval-tezgah … > .btn { border-radius … !important }`; YUZ_OLCU_ENVANTER_DISI 5: `.tuval-tezgah #islem-fark/#islem-verdim-toplam` padding + `.btn` radius) → seçiciler yüz sınıfsız kimliklere (`#islem-fark`, `#islem-verdim-toplam`, `.islem-alt-satir > #islem-vazgec`) taşındı (bu elemanlar yalnız BASİT fişte var) → ✓. Build: 0 hata 0 uyarı (artımlı).

**Canlı (5166, vekil 5167):** kiracı PAKETYILDIZ3B-TEST (demo tohum: 6 cari, 12 ürün, 9 satış; parola yalnız dosyada); PATCH /kullanici/gorunum {sema:yildiz} → 200; ölçümler gerçek ürünlerle (22 Ayar Burma Bilezik, 925 Gümüş Bileklik …). Test kiracısı `kiraci-temizle.sh --uygula` ile silindi (sorgu: 0 satır); tenants 17 (başlangıç 18; fark başka şeridin kendi temizliği). 5166/5167 durduruldu (dinleyen 0); 5199/5200'e dokunulmadı. Migration: gerekmedi.

**Dokunulan dosyalar (tur 2):** `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`, `dotnet/Kuyumcu.Tests/Yildiz3Testleri.cs`. Razor değişmedi.

**Açık kalanlar (tur 2):** 375'te Toplam/Fark yapışmaz (tek sütun; yalnız Kaydet çubuğu yapışır) · PRO dolu hâl ölçülmedi · 5 ürün senaryosu: 4. ürüne kadar ölçüldü (demo katalogu aramasıyla) · yukarıdaki iki paket-dışı kırmızı + P-30.
