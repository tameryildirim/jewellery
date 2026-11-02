# PAKET YILDIZ-AMBLEM — uygulama raporu (tur 2, 09.10.2026 ~22:30)

**Kaynak:** `TEMA-YILDIZ-PAKET-NOTU-2026-10-08.md` § 2a + tuval `ham/yildiz-tuval/kaynak/Amblem_sol.dc.html` (koyu) ve `Amblem_sol_acik.dc.html` (açık). `Amblem_orta` uygulanmadı.
**Rol:** Taşkın (backend: yok, görünüm paketi) + Ertuğ (frontend). Hasımsız paket: kanıt bu rapordaki ölçümlerdir.
**COMMIT YOK.** Dosya silinmedi. Para/veri/yasal davranışa dokunulmadı.

## 1. Yapılanlar

| Öğe | Uygulama |
|---|---|
| Yer | `YildizUstMenu.razor`: menü şeridinin başında tek `<a href="/" class="yildiz-marka amb" id="yildiz-marka">` (id değişmedi) → Bugün |
| İşaret | 40×40 SVG, tuvaldeki iki `path` birebir (stroke 1.1 / .8, stroke-opacity .75), hale `circle r=20` + radyal gradyan |
| Yazı | 110×16 SVG `<text x=0 y=13 font-size=13 font-weight=400 letter-spacing=4.4 fill=url(#…ti)>`; 6 `<tspan class="h hN">` (M İ H E N K), aralarında boşluk düğümü yok; harfler marka sözlüğünden tr-TR büyük harfle türetilir |
| Gradyan | linearGradient x1=0 x2=1, duraklar 0/.42/.5/.58/1; SMIL `animateTransform type=translate values="-1.2 0;-1.2 0;1.2 0;1.2 0" keyTimes="0;0.06;0.22;1" dur="11s" repeatCount="indefinite"` |
| Renk | Belirteç (`--amb-govde/orta/parilti/hale/hale-opaklik`): açık #3A3A3A→#5E5E5E, parıltı #C9C6BE, hale #141414 %10 · koyu #6A6A6A→#9A9A9A, parıltı #FFFFFF, hale #FFFFFF %16 (koyu: medya sorgusu + `data-tema="karanlik"`) |
| Döngü | `amb-cik` / `amb-hale` / `amb-harf` keyframes tuvaldeki yüzdelerle birebir, 11 s ease-out sonsuz; `.h1…h5` gecikmeleri .14/.28/.42/.56/.7 s |
| Hareket azaltma | animasyon none, hale opacity 0, `animateTransform{display:none}`; ek olarak orta/parıltı durakları gövde rengine eşitlenir (aşağıda sapma 1) |
| Ek dosya/JS | Yok (SVG + CSS); `IJSRuntime` yok, `url(`/`@import` yok — YA_04 kilitler |

### Tur 2'de yapılan (bu oturum)
Tur 1'in tam süitinde amblemin sabit renkli belirteç bloğu önce YILDIZ-4B bölümünde (Y4B_31 kırmızı), sonra 4A bölümünde (Y4A_32 kırmızı) kalmıştı. `/* AMBLEM — belirteçler */` bloğu **YILDIZ-1 belirteç bölümüne** (koyu belirteç tablosunun hemen ardı, satır 239–267; YILDIZ-2'den önce) taşındı; değerler değişmedi. `YildizAmblemTestleri` yer koşulu "YILDIZ-2 — KABUK'tan önce" olarak sıkılaştırıldı. Sonuç: Yildiz* + TasarimBelirtec + S1fGorunum filtreli 239/239 yeşil.

## 2. Sapmalar / belirsiz eşleşme tablosu (orkestratör kurucuya gösterir)

| # | Konu | Tuval | Uygulama | Gerekçe |
|---|---|---|---|---|
| 1 | Hareket azaltmada SMIL süpürmesi | Chrome `display:none` ile SMIL'i durdurmaz; tuvalde süpürme sürüyor (ölçüldü: tuval 1,5 s ↔ 6 s karesi %3,1 fark) | Duraklar gövde rengine eşitlenir → süpürme görünmez; sabit kare tuvalin 6 s dinlenme karesiyle **piksel AYNI** (fark 0) | Not § 2a "bütün animasyonlar kapalı" açık hükmü; tuvaldeki davranış tarayıcı açığı |
| 2 | Gradyan kimliği | `id="ti"`, `id="hale"` | `yildiz-amb-ti`, `yildiz-amb-hale` | Proje kimlik kuralı (`<önek>-<alan>`); görünüm aynı (piksel fark 0) |
| 3 | Dar masaüstü (1001–1365 px) ve 375 px | Tuvalde yalnız 1440 var | Amblem DEĞİŞMEZ; şeridin sağı daralır (22 AYAR <1366, saat <1241 gizlenir; 1001–1160 boşluk daralır; ≤600 kullanıcı adı sol boşluğu 0) | BYGATECH yazısı şeride 124 px ekleyince 1001–1354 px'te şerit taşıyordu; 1440'ta hiçbir kural devreye girmez (tuval birebir) |
| 4 | İlk 0,56 s'de N ve K görünür | Tuvalde de aynı (`animation-delay` öncesi harf opak) | Aynen bırakıldı | Birebir kuralı; yalnız sayfa ilk açılışında, döngüde tekrarlamaz |
| 5 | Belirteç bloğunun yeri | — | `tema-yildiz.css` YILDIZ-1 belirteç bölümü ("/* AMBLEM — belirteçler */" başlığı) | 4A/4B/4C bölüm testleri sabit renk yasaklar; kural gövdeleri dosya sonundaki "/* AMBLEM */" bloğunda |

## 3. Ölçüm — tuval ↔ uygulama (başsız Chrome, zaman eşitlenmiş)

Araç: `/private/tmp/kuyumcu-serit/YILDIZ-AMBLEM/amblem-kiyas.mjs` (tuval-kiyas.mjs dönüşümü; `document.getAnimations()` currentTime + SVG `setCurrentTime` aynı ana sabitlenir; amblem bölgesi 270×72 px, 1440 px viewport). Vekil 5189 → API 5188, test kiracısı PAKETYILDIZAMBLEM-TEST.

| Mod | Tema | 0,5 s | 2,0 s | 6,0 s | 9,5 s | Ölçü/renk tablosu tuval = uyg. |
|---|---|---|---|---|---|---|
| BASİT | koyu | 0 / 19440 px | 0 | 0 | 0 | AYNI |
| BASİT | açık | 0 | 0 | 0 | 0 | AYNI |
| PRO | koyu | 0 | 0 | 0 | 0 | AYNI |
| PRO | açık | 0 | 0 | 0 | 0 | AYNI |

Ölçü tablosu (her iki sayfada aynı işlev): `a` 64,15.5 164×40 · gap 14px · işaret 40×40 · yazı 110×16 · Inter 13px / 400 / 4.4px / uppercase · harfler "BYGATECH" · harf x: 118 / 134.2 / 142.1 / 156.1 / 168.3 / 182.5 · duraklar koyu `rgb(106,106,106) / rgb(154,154,154) / rgb(255,255,255)`, hale `rgb(255,255,255)/0.16` · SMIL değer/keyTimes/dur birebir. Amblem dışı çalışan animasyon: **0** (yalnız amblem hareket eder).

**Hareket azaltma** (koyu + açık, BASİT + PRO): çalışan animasyon 0 · işaret opaklık 1 · 6 harf opaklık 1 · hale 0 · animateTransform display none · uygulama 1,5 s ↔ 6 s farkı 0 · uygulama ↔ tuval 6 s karesi 0.

Görüntüler (Read ile bakıldı, 4 adet): `kiyas/t2basit-koyu-yan-yana.png`, `kiyas/t2basit-acik-yan-yana.png` (4 zaman karesi, sol tuval / sağ uygulama), `kiyas/t2pro-azaltma-yan-yana.png`, `olc/pro-375-karanlik.png`. Tüm dosyalar `/private/tmp/kuyumcu-serit/YILDIZ-AMBLEM/` altında.

## 4. Sıkışma ölçümü (sikisma-olc.js) — 1440 · 1280 · 375 × BASİT · PRO × koyu · açık × `/`, `/islemler`, `/ekranlar` (36 satır)

| Genişlik | Mod | Tema | K1 gizli-görünen | K3 taşan/kırık | K5 TRY | K10 | yatay | Amblem kutusu | Amblemle binen |
|---|---|---|---|---|---|---|---|---|---|
| 1440 | BASİT/PRO | koyu/açık | 0 | 0/0 | 0 | 0 | 0 | 64,15.5 · 164×40 | 0 |
| 1280 | BASİT/PRO | koyu/açık | 0 | 0/0 | 0 | 0 | 0 | 64,15.5 · 164×40 | 0 |
| 375 | BASİT/PRO | koyu/açık | 0 | 0/0 | 0 | 0 | 0 | 16,11.5 · 164×40 | 0 |

K9 (üst üste binen): 375 px `/islemler`'de 3 — amblemli ve amblem kuralları silinmiş halde **aynı 3** (`k9.mjs`): `#yildiz-tezgah-baska-odeme` / `#yildiz-tezgah-para-ustu` ⟂ `#yildiz-tezgah-fark-tutar` → Tezgâh ekranı (YILDIZ-4C şeridi), amblemle ilgisiz. Diğer tüm satırlarda K9 = 0. K2 (dar sütun) yalnız `/ekranlar` %95–96 (geniş, gerekçe gerekmez).

**8 eski şema:** tur 1'de `sema-fark` ölçümü AMBLEM-1440 64 karşılaştırma FARK 0, AMBLEM-375 32 karşılaştırma FARK 0. Tur 2 değişikliği yalnız `html:root[data-sema="yildiz"]` kapsamlı belirteç bloğunu taşıdığından 8 şemanın hesaplanan stiline etkisi yoktur.

## 5. Testler

| Koşum | Toplam | Geçti | Başarısız | Atlanan |
|---|---|---|---|---|
| YildizAmblemTestleri (YA_01–YA_08, YA_03 tr+en) | 9 | 9 | 0 | 0 |
| Yildiz* + TasarimBelirtec + S1fGorunum (filtreli) | 239 | 239 | 0 | 0 |
| **Tam süit** (tur 2, bir kez, trx: `/private/tmp/kuyumcu-serit/trx-pYILDIZ-AMBLEM-t2`) | 11969 | 11965 | 1 | 3 |

Tam süit kırmızısı: `P26TedarikTestleri.Y30_ikinci_kapora_REDDEDILIR_ve_duzeltme_yolu_yazilidir` — KAPORA-EKRAN/SATINALMA şeridinin (t5) alanı; tur 1 tam süitinde de vardı. **Bu paketin ürettiği yeni kırmızı: 0.** Tur 1'deki Y4A_32/Y4B_31 kırmızıları (bu paketin) giderildi.

Yeni test sayısı: 9 (YildizAmblemTestleri). Önce 11960 → sonra 11969.

## 6. Mutasyon kanıtı (tur 2, `mut.zsh`: yedek → perl tek eşleşme → test → geri kopya → cmp)

| Kod | Değişiklik | Kırmızı test | Geri alındı |
|---|---|---|---|
| T2-MA-1 | razor SMIL `dur="11s"` → `10s` | YA_02_gradyan_duraklari_ve_SMIL_degerleri_tuvalle_birebir | cmp AYNI |
| T2-MA-2 | css `amb-cik 11s` → `10s` | YA_05_on_bir_saniye_dongu_harf_gecikmesi_ve_keyframes_tuvalle_birebir | cmp AYNI |
| T2-MA-3 | `.h1` gecikme `.14s` → `.2s` | YA_05_… | cmp AYNI |
| T2-MA-4 | hareket azaltmada `animation: none` → `amb-cik 11s` | YA_07_hareket_azaltmada_animasyon_kapali_hale_gizli | cmp AYNI |
| T2-MA-5 | `--amb-govde #3A3A3A` → `#3B3A3A` (taşınan blok) | YA_06_acik_ve_koyu_durak_renkleri_tuvalle_birebir | cmp AYNI |
| T2-MA-6 | dar masaüstü eşiği 1365 → 1440 | YA_08_dar_genislik_kurallari_amblemi_degil_sagi_daraltir_1440_dokunulmaz | cmp AYNI |
| T2-MA-7 | hareket azaltmada hale `opacity: 0` → `1` | YA_07_… | cmp AYNI |
| T2-MA-8 | tspan sınıfı `h h{k}` → `h{k}` | YA_03_BYGATECH_harf_harf_tspan_h0_h5_ve_bosluk_dugumu_yok (tr, en) | cmp AYNI |
| T2-MA-9 | amblem içine `<img src>` | YA_01_tek_baglanti…, YA_04_ek_dosya_gorsel_ve_betik_yok | cmp AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 uyarı 0 hata (tam derleme build9: 0 hata, 6 uyarı — hepsi başka şeritlerin test dosyalarında: SonKalanlarTestleri, KesifP2Testleri, Dogrulayici/SUBEKAPSAMASinirTestleri, Yildiz2Testleri (13:41, bu paketten önce); bu paketin dosyalarında uyarı 0).

## 7. Denetim (`dotnet/denetim.sh`)

`DENETİM BAŞARISIZ` — iki ✗, ikisi de bu paketin dışı:
- `P-30: DEMO_INSAN_SURESI` (kapsam dışı, görevde hariç tutuldu).
- `GIRDI-2: TEST_GECERSIZ_FIKSTUR 1` → `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (MASAK-ESZAMAN-2 şeridi, t5).
Bu paketin sayaçları (sabit renk, kapsam, ModalPencere, Razor öznitelik vb.) ✓.

## 8. Canlı doğrulama

| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /api/v1/auth/giris` (fikstür, parola basılmadı) | 200 + jeton | 200, jeton var |
| `GET /api/v1/gorunum` | 200, sema yildiz | 200 `{"sema":"yildiz","yaziOlcegi":"18","temaModu":"sistem"}` |
| `GET http://localhost:5188/css/tema-yildiz.css` | 200, kaynakla aynı | 200; `cmp` kaynak = sunulan (AYNI); `amb-cik 11s` 1 kez; belirteç bloğu satır 239 |
| SQL `kiraci_ayarlari` | gorunum_semasi = yildiz | `01M4GHT9S6GJ4B9S57X9N1VYK5 · gorunum_semasi · yildiz` |

Veri/para uçlarına yazma yapılmadı (görünüm paketi).

**Temizlik:** API 5188 (PID 62730/62735) ve vekil 5189 (PID 63435) yalnız kendi PID'leriyle durduruldu; `lsof` 5188/5189 boş. `zsh kiraci-temizle.sh 01M4GHRZ47H3E2V2T0JFB1K6KC --uygula` → 1151 satır + tenants 1 satır silindi; kiracıya ait `kullanicilar`/`kiraci_ayarlari` sayısı 0. Tenants sayısı 23 (silmeden önce 23 bu kiracı dahil; aynı aralıkta başka şerit `PAKETYASAL-KAPI-GERI-TEST` ekledi → bu paketin payı 0). QA-TEST-* kiracılarına dokunulmadı. 5199/5200/5241 dokunulmadı.

## 9. DoD sözleşmesi
Yeni varlık/kayıt türü yok, yeni uç yok, migration yok. Amblem salt görünümdür: düzenlenebilir veri taşımaz (tek `<a>` → Bugün; gerçek gezinme yapar, "etkisiz düğme" değildir).

## 10. Değişen dosyalar (`git status --porcelain`, bu paket)
- `dotnet/Kuyumcu.Web/Bilesenler/Kabuk/YildizUstMenu.razor` (amblem; YILDIZ-2'den izlenmeyen dosya)
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` (yalnız "/* AMBLEM — belirteçler */" ve "/* AMBLEM */" blokları)
- `dotnet/Kuyumcu.Tests/YildizAmblemTestleri.cs` (yeni)
- Bu rapor.

**Dokunulan sayfalar:** yok (yalnız kabuk bileşeni YildizUstMenu).

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| 4 zaman karesi × koyu+açık, piksel fark ~0 | SAĞLANDI | 16/16 kare fark 0 (BASİT+PRO) |
| Ölçü/renk tablosu tolerans içinde | SAĞLANDI | tuval = uygulama (birebir) |
| Reduced-motion sabit+opak, hale gizli | SAĞLANDI | azaltma raporu; görüntü |
| Diğer 8 şemada kabuk fark 0 | SAĞLANDI (tur 1 ölçümü; tur 2 yalnız yildiz kapsamlı) | sema-fark 64+32 karşılaştırma FARK 0 |
| Ek dosya/JS yok | SAĞLANDI | YA_04 + T2-MA-9 |
| Mutasyon ≥3 (dur, harf gecikmesi, reduced-motion) | SAĞLANDI | 9/9 kırmızı, cmp AYNI |
| Build 0 hata | SAĞLANDI | build10 0/0 |
| Denetim TEMİZ (P-30 hariç) | BU PAKET İÇİN SAĞLANDI | tek ek ✗ MASAK-ESZAMAN-2 şeridinin |
| Tam süit 0 yeni kırmızı | SAĞLANDI | tek kırmızı P26 Y30 (t5, önceden var) |
| Gerçek tarayıcı 1280/1440/375 × BASİT/PRO × açık/koyu | SAĞLANDI | 36 satır ölçüm + görüntü |
| Test kiracısı silindi | SAĞLANDI | temizle.log, SQL 0 |

## 12. Açık kalanlar (sahibiyle)
- `P26TedarikTestleri.Y30_ikinci_kapora_REDDEDILIR_ve_duzeltme_yolu_yazilidir` kırmızı — KAPORA-EKRAN/SATINALMA (t5).
- denetim `GIRDI-2: TEST_GECERSIZ_FIKSTUR` — `Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` — MASAK-ESZAMAN-2 (t5).
- 375 px `/islemler` K9 = 3 (Tezgâh "+ Başka ödeme" / "+ Para üstü" ⟂ fark tutarı) — YILDIZ-4C (t1).
- Belirsiz eşleşme tablosu (§ 2) kurucu onayına.

## 13. Kurucu / mali müşavir
Mevzuat konusu yok.
