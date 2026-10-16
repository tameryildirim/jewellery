# PAKET YILDIZ-AMBLEM-2 — uygulama raporu (tur 1 + tur 2, 10.10.2026)

> **Tur 2 (18:30–19:15) en altta § T2.** Tur 1 bölümleri (§ 1–13) kayıt olarak durur; tur 2 ile değişen ölçüm/test/mutasyon sayıları § T2'dedir ve bağlayıcı olan onlardır.

Kaynak: `EMIR-AMBLEM-YILDIZ-2026-10-10.md` (tek doğruluk) · tuval `ham/yildiz-tuval/kaynak/Amblem_sol(.acik).dc.html`, `Giris4(a).dc.html` (10.10 16:45). Hasımsız paket: kanıt bu rapordadır. Yalnız görünüm; para/veri/yasal davranışa dokunulmadı. COMMIT YOK.

## 1. Yapılanlar
- **Kabuk (40 px)** — `Bilesenler/Kabuk/YildizUstMenu.razor`: v1 (harf harf düz SVG elmas + hale) kaldırıldı. Tuvaldeki `<span class="amb-parla">…</span>` bloğu birebir: dış sarmalayıcı `.amb-parla` (parlama süzgeci) → `.amb-ik` (perspektif kutusu 40 px, 640 px) → gradyan tanımı (`yildiz-amb-ti`, SMIL `values="-1.2 0;-1.2 0;1.2 0;1.2 0" keyTimes="0;0.06;0.22;1" dur="11s"`) + `.tas3d` (preserve-3d, 20° duruş) → 8 yüz (4 taç kapalı üçgen `M0 23.6 L12.4 0 L24.9 23.6 Z` + 4 pavyon "V" `M0 0 L12.4 23.6 L24.9 0`, çizgi 1,2, linejoin round). Taş blok bir statik `RenderFragment AmblemTasi(TasOlcusu)` olarak tek yerde; kabuk ve giriş aynı çizimi kullanır (yalnız ölçüler farklı). "MİHENK" 110×16, Inter 13/400/4,4, taşla aynı gradyan; tek `<a>` → `/` (Bugün).
- **Giriş (96 px)** — `Pages/Giris.razor` YALNIZ Yıldız dalı (`YildizKabuk.Mi` — kabukla aynı karar noktası): taş 96 px ortada + 22 px + "MİHENK" 220×32 (viewBox 110×16, x=57 ortalı) `h1#giris-amblem` içinde; sağ panel ve "Hoş geldin" başlığı Yıldız giriş sekmesinde çizilmez; form tuval düzeninde (380 px, amblem↔form 56 px) ve `amb-form-gel .9s ease-out 1.6s both` ile belirir (alanlar opaklık 0 iken de yazılabilir — kilit yok). Form, MFA (kurtarma kodu alanı C4_62), kayıt, dil seçici, sürüm, P-30 beyanı **aynen**. Alanlar tuval `.alan` kalıbı (altı kıl çizgi; hata çizgisi `--tehlike`); ≤768 px dil seçici sağ üst köşe (amblem ortada kalır).
- **CSS** — `tema-yildiz.css`: `/* AMBLEM — belirteçler */` (YILDIZ-1 belirteç bölümünde) v2 renkleri + `--glow` + `--amb-sifir`; `/* AMBLEM */` bloğu v1 yerine v2 (tuval `<style>` kuralları değerleriyle aynen + tuvalin satır içi stilleri sınıf olarak). Dar masaüstü kuralları (v1'den, şerit için) korundu.
- **Testler** — `YildizAmblem2Testleri.cs` (YA2_01–YA2_17, 20 koşum); `YildizAmblemTestleri.cs` v1 testleri v2'ye taşındığı için yalnız yönlendirme notuna indirildi (dosya silme yasak).

## 2. Sapmalar / belirsiz eşleşme (gerekçeli)
| # | Konu | Tuval | Uygulama | Gerekçe |
|---|---|---|---|---|
| 1 | Hareket azaltma | `.amb-ik{opacity:0}` (taş kaybolur), dönüş ve yazı sürer | Bütün animasyon `none`; taş 20°, yazı opaklık 1; SMIL ışığı durak eşitlemesiyle görünmez | Emir § 4.5/4.6 ("taş hiç kaybolmaz", "taş 20°'de, yazı tam opak sabit") tuval kuralıyla çelişiyor; emir esas alındı. Chrome SMIL'i `display:none` ile durdurmaz (v1'de ölçüldü) |
| 2 | Satır içi stiller | `style="…"` | Aynı değerler sınıf kuralı | CSP / denetim "satır içi style" cırcırı; piksel fark 0 |
| 3 | Sınıf/ad | `.yz`, `form-gel`, `id="ti"` | `.amb-yuz`, `amb-form-gel`, `yildiz-amb-ti` / `giris-amb-ti` | `.yz` uygulamada Yıldız ekran kabı (çakışma); Y1_02 yalnız `amb-*` keyframes'i kapsam istisnası sayar; kimlik kuralı |
| 4 | Durak renkleri | öznitelik `stop-color` (dosya başına sabit) | sınıf + belirteç (`--amb-govde/orta/parilti`), açık/koyu tema belirteçten | razor'da sabit renk yok (v1 ile aynı yöntem); `rgba(255,255,255,0)` → `var(--amb-sifir)` (Y1_10 kural gövdesinde sabit renk yasağı); çözülen değer aynı |
| 5 | Ölü kurallar alınmadı | `.amb-hale{display:none}`, `@keyframes amb-cik/amb-hale`, `.gir .h{animation:amb-harf…}` (amb-harf tanımsız), `.gir .amb-ik .ti-anim` | yok | Hiçbir öğeye/tanımlı keyframes'e bağlı değiller (görünüm etkisi 0; piksel fark 0 ile kanıtlı). `amb-harf` alınsaydı v1 harf harf yolu açık kalırdı (emir "harf harf DEĞİL") |
| 6 | Giriş Yıldız şeması — **belirsiz eşleşme** | — | `YildizKabuk.Mi` (dükkân/kullanıcı görünümü bellekte Yıldız ise) | Görünüm oturum öncesi sunucudan okunamaz ve bilerek localStorage'a yazılmaz (GorunumServisi notu). Bu yüzden Yıldız girişi **Yıldız oturumundan çıkışta** (aynı sekme) görünür; soğuk açılışta (yeni sekme/yenileme) şema bilinmediği için eski giriş çizilir. Soğuk açılışta da Yıldız istenirse şema önbelleği (tema.js) gerekir — kapsam dışı, kurucu kararı |
| 7 | Giriş alt satırı "TUNALILAR Kuyumculuk · Merkez Şube" | var | yok | Oturum öncesi dükkân adı bilinmez (uydurulmaz); sürüm satırı ve P-30 beyanı yerinde |
| 8 | Giriş formu iç düzeni | tek satır "Parolamı unuttum" + "Giriş yap" | mevcut bileşenler (etiket üstte, tam genişlik düğme, altta bağlantılar) | Form davranışı/kimlikleri değişmez koşulu; yalnız alan çizgisi tuval kalıbına alındı |
| 9 | 375 / 1280 | tuvalde yok | amblem değişmez; ≤768 dil seçici sağ üst | taşma/binme 0 (ölçüm § 4) |

## 3. Ölçüm — tuval ↔ uygulama (başsız Chrome, zaman eşitlenmiş)
Araç: `/private/tmp/kuyumcu-serit/YILDIZ-AMBLEM-2/amblem2-kiyas.mjs` (tuval-kiyas.mjs dönüştürücüsü; Inter = uygulamanın kendi woff2'si; `document.getAnimations()` currentTime + SVG `pauseAnimations/setCurrentTime` aynı ana; bölge = amblem kutusu + 12 px, 1440×900, CDP 9601). API 5164 → vekil 5165, kiracı `PAKETYILDIZAMBLEM2-TEST Tunalılar Kuyumculuk` (sema yildiz). Son koşum: `kiyas-son2/` (temiz derleme).

Piksel farkı (farklı piksel / toplam; kanal farkı en büyük):

| Yüzey | Tema | 0 sn | 2 sn | 6 sn | 9,5 sn | 14 sn | Hareket azaltma |
|---|---|---|---|---|---|---|---|
| Kabuk (188×64) | koyu | 0 | 0 | 0 | 0 | 0 | 6 px, en çok 1/255 |
| Kabuk | açık | 0 | 0 | 0 | 0 | 0 | 182 px, en çok 1/255 |
| Giriş (244×174) | koyu | 0 | 0 | 0 | 0 | 0 | 29 px, en çok 1/255 |
| Giriş | açık | 0 | 0 | 0 | 0 | 0 | 318 px, en çok 1/255 |

**20/20 kare FARK 0.** Konum: kabuk tuval = uygulama `64,15.5 164×40`; giriş kutusu 220×150 iki tarafta. Giriş ekranda dikey ortalandığından form boyu farklı → uygulamada y=165.953, tuvalde 236; alt piksel evresi eşitlenmeden (`kiyas-g0`) fark en çok 2/255 (%0,09–0,23); evre eşitlenince (tuval kabına 0,4766 px üst dolgu → aynı kesir) 0. Hareket azaltma referansı tuvalde döndürme 0, ışık 0, parlama 0, yazı 6 sn karesidir; kalan ≤1/255 fark tuvalin yazıdaki `blur(0)` süzgeç katmanıdır (görünmez).

Süre ölçümü (getAnimations, iki tarafta aynı): `amb-don 28000 ms linear sonsuz` · `amb-parla 11000 ms` · `amb-yazi 11000 ms` · SMIL `dur=11s indefinite` · giriş `amb-form-gel 900 ms, gecikme 1600 ms`.
Hareket azaltma (4 durum): çalışan animasyon 0 · taş `matrix3d(0.939693,…,-0.34202…)` = rotateY(20°) · yazı opaklık 1 · `.amb-ik` opaklık 1 · parıltı durağı gövde rengi.
3B kutuda süzgeç yok (canlı, 4 durum): `.amb-parla` animasyon `amb-parla`; `.amb-ik` / `.tas3d` / `.amb-yuz` filter `none`.

Görüntüler (Read ile bakıldı): `kiyas-son/kabuk-karanlik-yan-yana.png`, `kiyas-son/giris-aydinlik-yan-yana.png` (sol tuval | sağ uygulama; 0/2/6/9,5/14 sn + azaltma), `olc/giris-375-karanlik.png` (ilk hâl — dil seçici amblemin yanındaydı → düzeltildi, yeniden ölçüldü), `olc/giris-1440-aydinlik.png` (parola kutusu çerçeveli → düzeltildi, sayısal doğrulama: e-posta ve parola kutusu kenar `0/1px/0` yarıçap 0). Tüm dosyalar `/private/tmp/kuyumcu-serit/YILDIZ-AMBLEM-2/`.

## 4. Sıkışma ölçümü (sikisma-olc.js, son kod) — `olc/olc.json`
| Rota | Genişlik | Mod | Tema | Amblem kutusu | K1 | K3 | K3c | K5 | K9 | K10 | K10c | K11 | Yatay kaydırma |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `/` kabuk | 1440 / 1280 | BASİT, PRO | koyu, açık | 64,15.5 164×40 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| `/` kabuk | 375 | BASİT, PRO | koyu, açık | 16,11.5 164×40 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| `/giris` Yıldız | 1440 / 1280 | (oturum öncesi) | koyu, açık | 610 / 530, 116 · 220×150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| `/giris` Yıldız | 375 | (oturum öncesi) | koyu, açık | 77.5,80 220×150 (ortada) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
18 satırın hepsinde bütün K sayaçları 0 (K2 dar sütun ölçülmedi = tek sütun; K4 boy 1–1,3 ekran).

**Öteki 8 şema:** `sema-fark.mjs amblem2-8sema` (kabuk `/`, `/islemler` × 8 şema × açık/koyu, tema-yildiz.css açık↔kapalı) → **32 karşılaştırma · FARK 0**. Giriş: `giris-8sema.mjs` (1440 + 375 × 8 şema × açık/koyu; Yıldız dalı çizilmedi doğrulandı) → **32 karşılaştırma · FARK 0**.
**Yıldız tipografi taraması:** `yildiz-tipo-tara.mjs --mod BASIT` `/` 1440+375 × koyu+açık → 138 düğüm, **KURAL DIŞI 0**.
**tuval-kiyas (Bugün/Tezgâh/Müşteriler/Tüm ekranlar):** eşleme JSON'ları bu ağaçta/geçici dizinde bulunamadı → koşulamadı. Eşdeğer kanıt: amblem `<a>` kutusu v1 ile aynı (64,15.5 164×40, v1 raporu § 3) ve amblem bölgesi tuvalle piksel 0 → şeridin geri kalanı yer değiştirmedi; içerik alanına dokunulmadı.

## 5. Testler
| Koşum | Toplam | Geçti | Başarısız | Atlanan |
|---|---|---|---|---|
| YildizAmblem2 (YA2_01–17; 02/04/10 Theory) | 20 | 20 | 0 | 0 |
| Yildiz* + TasarimBelirtec + S1fGorunum + Giris* + P30DemoKisit + GocD + AuthSifirla (ara, YA2_17 öncesi) | 790 | 790 | 0 | 0 |
| Tam süit (bir kez, son kod) | 12226 | 12220 | 3 (başka şeritler) | 3 |

Yeni test: 20 koşum (17 yöntem) eklendi; v1 YildizAmblemTestleri'nin 9 koşumu v2'ye taşındı (kaldırıldı) → net +11.

### 5a. Tam süit
Tam süit (bir kez, son kod; trx `/private/tmp/kuyumcu-serit/trx-pYILDIZ-AMBLEM-2`, 7,8 dk): **12226 toplam · 12220 geçti · 3 başarısız · 3 atlandı.**
Kırmızılar — hiçbiri bu paketin dosyalarına dokunmuyor (amblem/giriş/tema-yildiz ile ilgisiz):
- `RaporDortTestleri.RD_KH01_Ay_ontoplami_…` ve `RD_KH02_Kapanmis_aya_dusen_…` — `KarZararServisi.DonemDogrula` "Rapor dönemi en çok 400 gün olabilir" (tek başına koşumda da kırmızı; rapor/maliyet şeridi `RaporDortTestleri.cs` — izlenmeyen yeni dosya, başka şerit). Sahibi: RAPOR-4 şeridi.
- `SATIS3EkranTestleri.Alis_turunde_AYNI_arama_kutusu_calisir` — tek başına yeniden koşumda **yeşil** (kararsız; `Pages/Islemler.razor` süit sırasında 17:38'de başka şerit tarafından değiştirildi). Sahibi: SATIS-3 / t5 şeridi.
**Bu paketin ürettiği yeni kırmızı: 0** (YildizAmblem2 20/20 yeşil; Yildiz*/Giris* filtreli 790/790).

## 6. Mutasyon kanıtı (`mut.zsh`: yedek → perl tek eşleşme → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | Geri alındı |
|---|---|---|---|
| MA2-1 | `amb-don 28s` → `27s` | YA2_06_donus_28sn_20_380_linear_durus_20_derece | cmp AYNI |
| MA2-2 | `@keyframes amb-don from rotateY(20deg)` → `15deg` | YA2_06_… | cmp AYNI |
| MA2-3 | süzgeç 3B kutuya (`.tas3d` kuralına `filter: drop-shadow(0 0 7px var(--glow))`) | YA2_07_parlama_dis_sarmalayicida_3B_kutuda_filter_yok | cmp AYNI |
| MA2-4 | yazı harf harf (`.amb .h1 { animation: amb-harf …; animation-delay: .14s }` + `@keyframes amb-harf`) | YA2_08_yazi_butun_olarak_cikar_harf_harf_yok_tas_kaybolmaz | cmp AYNI |
| MA2-5 | hareket azaltmada `animation: none` → `amb-don 28s linear infinite` | YA2_06, YA2_11_hareket_azaltmada_sabit_tas_20_derece_yazi_opak | cmp AYNI |
| MA2-6 | koyu `--glow .45` → `.25` | YA2_09_acik_ve_koyu_renkler_ve_glow_tuvalle_birebir | cmp AYNI |
| MA2-7 | razor SMIL `dur="11s"` → `10s` | YA2_03_gradyan_ve_SMIL_11sn_… | cmp AYNI |
| MA2-8 | taç yüzünde `Z` (ekvator çizgisi) silindi | YA2_02_sekiz_yuz_… (kabuk: True, kabuk: False) | cmp AYNI |
| MA2-9 | Giriş: sağ panel Yıldız'da da çizilir (`@if (true)`) | YA2_13_giris_yildizda_amblem_96_… | cmp AYNI |
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı 0 Hata**.

## 7. Denetim (`bash dotnet/denetim.sh`)
`DENETİM BAŞARISIZ` — iki ✗, ikisi de bu paketin dışı ve önceden var:
- `P-30: DEMO_INSAN_SURESI` (görevde hariç).
- `GIRDI-2: TEST_GECERSIZ_FIKSTUR 1` → `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (MASAK-ESZAMAN şeridi; v1 raporunda da vardı).
Ara koşumda bu paketin ürettiği `record tanımı 4 (tavan 3)` + `P-02 OLCUM_TABANI_KOPYA` + `P-10` ✗'leri görüldü (taş ölçü tipi `record` idi ve yorumda "record" sözcüğü sayılıyordu) → salt okunur sınıfa çevrildi, yorum düzeltildi → üçü ✓. Satır içi style 24 (tavan 196, artış 0).

## 8. Canlı doğrulama (API 5164, vekil 5165)
| Adım | Beklenen | Gerçek |
|---|---|---|
| Kiracı tohumu `demo-tohumla.js --prova-kiraci --sema yildiz` | kiracı + 6 cari/12 ürün/9 satış | `01M4K3S4Y51H4PA9181JE3D42J` · `{"cari":6,"urun":12,"satis":9}` |
| `POST /api/v1/auth/giris` (fikstür; parola basılmadı) | 200 + jeton | 200, jeton var |
| `GET /api/v1/gorunum` | 200 sema yildiz | `{"sema":"yildiz","yaziOlcegi":"18","temaModu":"sistem"}` |
| `PATCH /gorunum {"sema":"platin"}` (8 şema ölçümü için) → geri `yildiz` | 200 ×2 | 200 `Görünüm kaydedildi` ×2 |
| `GET /css/tema-yildiz.css` | 200, kaynakla aynı | 200, `cmp` AYNI, `amb-don 28s linear infinite` 1 kez |
| SQL `kiraci_ayarlari` | gorunum_semasi = yildiz | `01M4K3S4Y51H4PA9181JE3D42J · gorunum_semasi · yildiz` |
**Temizlik:** API (PID 37085/37092) ve vekil (PID 37567) yalnız kendi PID'leriyle durduruldu; `lsof` 5164/5165 boş. `zsh kiraci-temizle.sh 01M4K3S4Y51H4PA9181JE3D42J --uygula` → kiracı satırı 0, kullanıcı satırı 0. Tenants: başta 23, sonda 22 (bu kiracı silindi; aradaki fark eşzamanlı şeritlerin hakem kiracıları — `PAKETHAKEM-TEST 9206da` 14:44'te eklendi, başka biri silindi; QA-TEST-* ve diğerlerine dokunulmadı). 5199/5200/5241 dokunulmadı. Sistem anahtarı rastgele üretildi, basılmadı.

## 9. DoD sözleşmesi
Yeni varlık/kayıt türü yok (yalnız görünüm). Düzenlenebilir veri yok; amblem salt görsel, giriş formu davranışı değişmedi. Migration: gerekmedi.

## 10. Değişen dosyalar (bu paket)
```
 M dotnet/Kuyumcu.Web/Pages/Giris.razor                       (yalnız Yıldız koşullu dal)
?? dotnet/Kuyumcu.Web/Bilesenler/Kabuk/YildizUstMenu.razor    (amblem v1 → v2; dosya önceki şeritlerden izlenmeyen)
?? dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css             (yalnız "AMBLEM — belirteçler" ve "/* AMBLEM */" blokları)
?? dotnet/Kuyumcu.Tests/YildizAmblem2Testleri.cs              (yeni)
?? dotnet/Kuyumcu.Tests/YildizAmblemTestleri.cs               (v1 testleri → yönlendirme notu)
```
Dokunulan sayfalar (TEK DOKUNUŞ): `Pages/Giris.razor` (Yıldız dalı), `Bilesenler/Kabuk/YildizUstMenu.razor` (amblem bölümü).

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| 8 yüz, üst-alt eşit, çizim (siluet + ekvator + 2 iç kesim) | KARŞILANDI | YA2_02/10; piksel 0 |
| Ölçü formülleri § 3 (r=.44S, h=.5S, e, s, r/√2, atan 31,9°, perspektif 16S, çizgi 1,2/2,2) | KARŞILANDI | YA2_02, YA2_10 |
| 20° duruş, 28 sn dönüş linear | KARŞILANDI | YA2_06; canlı 28000 ms linear |
| Işık 11 sn SMIL, taş+yazı aynı gradyan | KARŞILANDI | YA2_03; canlı dur 11s |
| Parlama %18 zirve, süzgeç dış sarmalayıcıda, 3B kutuda filter yok | KARŞILANDI | YA2_07; canlı filter none |
| Yazı bütün olarak, harf harf yok, taş kaybolmaz | KARŞILANDI | YA2_08; MA2-4 |
| Renkler açık/koyu + glow | KARŞILANDI | YA2_09; 20 kare piksel 0 |
| Hareket azaltmada sabit | KARŞILANDI (tuvalden sapma § 2 #1) | YA2_11; canlı çalışan 0 |
| Kabuk 40 px + giriş 96 px ortada, form 1,6 sn | KARŞILANDI | YA2_01/13/15; ölçüm § 3 |
| MFA (C4_62) ve giriş davranışı değişmez | KARŞILANDI | YA2_16 + Giris* filtreli testler yeşil |
| 20 kare tuval↔uygulama ~0 | KARŞILANDI | § 3 tablo: 20/20 = 0 |
| Diğer 8 şema FARK 0 | KARŞILANDI | kabuk 32/32, giriş 32/32 |
| Ek dosya/JS yok | KARŞILANDI | YA2_05 |
| Mutasyon ≥4 | KARŞILANDI | 9/9 kırmızı, cmp AYNI |
| build 0/0, denetim (P-30 hariç) | KISMEN — bu paketin sayaçları ✓; GIRDI-2 başka şeridin | § 7 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | § 5a: 3 kırmızı başka şeritlerin |
| tuval-kiyas 4 ekran tablosu | KOŞULAMADI (eşleme dosyası yok) — eşdeğer kanıt § 4 | |

## 12. Açık kalanlar (sahibiyle)
- Giriş ekranının soğuk açılışta Yıldız olması (şema önbelleği) — kurucu kararı (§ 2 #6).
- `GIRDI-2 TEST_GECERSIZ_FIKSTUR` — MASAK-ESZAMAN şeridi (t5).
- `P-30 DEMO_INSAN_SURESI` — kapsam dışı.
- tuval-kiyas eşleme JSON'ları kalıcı yere alınmalı (betikler/tur) — orkestratör.

## 13. Kurucu / mali müşavir
Mevzuat içeriği yok. Kurucuya: giriş ekranı Yıldız düzeni yalnız Yıldız oturumundan çıkışta görünür (soğuk açılışta şema bilinmez); istenirse tema önbelleğiyle genişletilebilir.


---

## T2. TUR 2 — hakem KRİTİK bulgusu: üst şerit taşması (1366/1280/1024/375)

**Bulgu (tasarım hakemi):** Yıldız kabuğu, kullanıcı "HATİCE GÜLİZAR K." (146 px) ile 1366 → scrollWidth 1436, 1280 → 1300, 1024 → 1037, 375 → 418; ad kesik, her ekranda yatay kaydırma. Tur 1'in `max-width: 1365px` eşiği 1366'yı kapsamıyordu.

### T2.1 Yapılanlar (amblem ölçü/yer DEĞİŞMEDİ)
1. **Ad kısaltma — `YildizUstMenu.razor` `KartAdi()`**: birden çok ön adda yalnız **ilki** + soyadın baş harfi ("Hatice Gülizar Karaosmanoğlu" → `HATİCE K.`; tuval kalıbı "HAKAN T." aynen). Ad metni `<span class="yildiz-kullanici-ad">` içinde (düğmenin `TextContent`'i değişmedi → Yildiz4A "HAKAN T." testi yeşil). Düğmeye `data-bas` (baş harfler, `HK`) eklendi.
2. **Eşikler gerçek ölçümle yeniden kuruldu — `tema-yildiz.css` "AMBLEM — DAR MASAÜSTÜ" bloğu** (ölçülen: geniş düzen tam 1331+U, 22 AYAR'sız 1195+U; sıkı düzen 22 AYAR'sız 959+U, saatsiz 892+U; U = ad genişliği):
   - `≤1439` 22 AYAR gizli (1366 artık İÇİNDE) · `1001–1304` sıkı düzen (ara 20, kenar 24) · `≤1080` saat gizli · ≤1000 ve ≤600 mevcut 4A kuralları.
   - Ad hiçbir genişlikte şeridi taşıramaz: `.yildiz-kullanici {flex:0 1 auto; min-width:0}`, düğme `max-width:100%`, iç span `inline-block; overflow:hidden; text-overflow:ellipsis; vertical-align:top`.
   - **Satır tabanı**: overflow düğmenin kendisine (ya da blok span'a) verilince 1440'ta ad 1,1 px / 4,6 px yukarı kayıyordu (ölçüldü: y 26,45 → 25,34 / 21,8); `inline-block + vertical-align: top` iç span ile y = 26,45 aynen.
   - `≤600`: ad yerine baş harfler (`::before { content: attr(data-bas) }`, span `font-size:0` — ad metni DOM'da ve erişilebilir adda kalır).
3. **1440 dokunulmadı**: 1440'ta görünüm piksel eşit — şerit (x 240–1440, 88 px) önce/sonra farkı yalnız saat hanesinde (bbox x 1205–1223 = "18:37"→"18:43", dakika değişti); ad/kur/menü kutuları birebir (`#yildiz-kullanici-ac 1300.75,26.45,75.25,20.3` önce = sonra).

### T2.2 Ölçüm — başsız Chrome (CDP 9601, API 5164 → vekil 5165, ön bellek kapalı), kullanıcı "Hatice Gülizar Karaosmanoğlu"
`t2/olc-t2.mjs` + `sikisma-olc.js`; 10 genişlik × BASİT/PRO × koyu/açık = **40 durum** (`/`):

| Genişlik | scrollWidth = innerWidth | ad sağ ≤ iç kenar | 22 AYAR | saat | ad | K1 K3 K3c K5 K9 K10 K12 yatay | Amblem |
|---|---|---|---|---|---|---|---|
| 1440 | 1440 ✓ | 1376 ≤ 1376 | görünür | görünür | HATİCE K. 75 px | 0 | 64,15.5 164×40 |
| 1366 | 1366 ✓ | 1302 ≤ 1302 | gizli | görünür | 75 px | 0 | 64,15.5 164×40 |
| 1280 | 1280 ✓ | 1256 ≤ 1256 | gizli | görünür | 75 px | 0 | 24,15.5 164×40 |
| 1200 | 1200 ✓ | 1176 ≤ 1176 | gizli | görünür | 75 px | 0 | 24,15.5 |
| 1100 | 1100 ✓ | 1076 ≤ 1076 | gizli | görünür | 75 px | 0 | 24,15.5 |
| 1024 | 1024 ✓ | 1000 ≤ 1000 | gizli | gizli | 75 px | 0 | 24,15.5 |
| 1000 | 1000 ✓ | 976 ≤ 976 | gizli | gizli | 75 px | 0 | 24,11.5 |
| 768 | 768 ✓ | 744 ≤ 744 | gizli | gizli | 75 px | 0 | 24,11.5 |
| 600 | 600 ✓ | 584 ≤ 584 | — (kur gizli) | — | "HK" | 0 | 16,11.5 |
| 375 | 375 ✓ | 359 ≤ 359 | — | — | "HK" (44 px dokunma hedefi) | 0 | 16,11.5 |

Her satır BASİT ve PRO × koyu ve açık için AYNI (40/40; `t2/olc-t2_.json`).

**Ad uzunluğu zorlaması** (`t2/uzun.mjs`, 4 ad × 15 genişlik = 60 durum: HAKAN T., HATİCE K., MUHAMMEDALİ K. (137 px), ABDURRAHMANGAZİ KARAOSMANOĞLU (300 px); 1440/1439/1366/1305/1304/1280/1081/1080/1024/1001/1000/768/601/600/375): **60/60 scrollWidth = innerWidth ve ad iç kenarın içinde**; sığmayan ad "…" ile kesilir (ör. 1366'da `ABDURRAHMANG…`, görüntü `t2/ust-1366-uzun-ad.png`).

**Bütün Yıldız ekranları** (`t2/rotalar.mjs`): `/`, `/islemler`, `/cariler`, `/kasa`, `/ekranlar` × 1440/1366/1280/1024/375 × koyu/açık × BASİT/PRO = **100 durum, yatay taşma 0, ad iç kenarın içinde 100/100.**

Görüntüler (Read ile bakıldı, 3 adet): `t2/ust-1366-basit-karanlik.png` (HAS · saat · HATİCE K. — 22 AYAR yok, kesik yok), `t2/ust-375-profesyonel-aydinlik.png` (amblem + MENÜ + HK), `t2/ust-1366-uzun-ad.png` (300 px ad "…" ile, şerit taşmıyor).

**Tuval ↔ uygulama 20 kare yeniden** (`kiyas-t2`, `--giris-faz 0.4766`): kabuk/giriş × koyu/açık × 0/2/6/9,5/14 sn → **20/20 FARK 0**; kutular tuvalle aynı (kabuk 64,15.5 164×40; giriş 610 · 220×150). Hareket azaltma karesi: en büyük kanal farkı 2/255, >8 eşiğinde 0 piksel (tuvalin azaltma kuralı emirle çeliştiği için referans farklı — § 2 #1); süzgeç: `.amb-parla` animasyonlu, `.amb-ik/.tas3d/.amb-yuz` filter `none`; çalışan animasyon 0.
**Öteki 8 şema**: `sema-fark.mjs` `/`, `/islemler` × 8 şema × açık/koyu, **1440, 1366 ve 375'te** → 3 × 32 = **96 karşılaştırma · FARK 0** (kiracı geçici olarak `platin`, sonra `yildiz`'e geri alındı).
**Yıldız tipografi**: `yildiz-tipo-tara.mjs` BASİT ve PRO, `/` × 1440/1366/375 × koyu/açık → 274 düğüm, **KURAL DIŞI 0**, yatay kayan 0, kesik yazı 0.

### T2.3 Testler
| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| YildizAmblem + Yildiz4A + Yildiz2Testleri (filtreli) | 67 | 67 | 0 |
| Tam süit (bir kez, son kod, trx `/private/tmp/kuyumcu-serit/trx-pYILDIZ-AMBLEM-2-t2`, 11,9 dk) | 12275 | 12263 | 9 (3 atlandı) |

Yeni/değişen: `YA2_12` eşikleri 1439/1080 + sıkı düzen; yeni `YA2_18` (Theory ×4: HAKAN T./HT · HATİCE K./HK · "  İsmail   Hakkı " → İSMAİL I./İI · soyadsız AYŞE/A) ve `YA2_19` (ad kutusu küçülür, "…", satır tabanı, ≤600 baş harf). YildizAmblem2 sınıfı 20 → **25 koşum** (+5).

**Tam süit kırmızıları — 9, hiçbiri bu paketin dosyalarını çizmiyor/okumuyor:**
- `RaporDortTestleri.RD_KH01`, `RD_KH02` — "Rapor dönemi en çok 400 gün" (tur 1'de de vardı; RAPOR-4 şeridi).
- `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz` (1414 satır), `S1iGorselSigma2Testleri.S1i_06…`, `Toplu5KaynakTestleri.T5_Islemler_razor_1400…` — `Pages/Islemler.razor` süit sırasında 19:05'te başka şeritçe değiştirildi (t5).
- `SonKalanlarTahsilatEkranTestleri.SKC01…` (BASİT, PRO) — `Pages/Cariler.razor` 18:47'de başka şeritçe değiştirildi; tek başına koşumda da kırmızı (t5 / ISLEM-BANT-2).
- `FaturaEkranTestleri.Y4C_K12…(basit: True)`, `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini…` (51↔52) — tek başına yeniden koşumda **yeşil** (eşzamanlı ağaç değişimi / kararsız).
Bu testlerin hiçbiri `YildizUstMenu`'yu çizmiyor (grep: yalnız Yildiz2/Yildiz4A/YildizAmblem2 çiziyor, üçü yeşil). **Bu paketin ürettiği yeni kırmızı: 0.**

### T2.4 Mutasyon (tur 2'de hepsi yeniden koşuldu; `mut.zsh`: yedek → perl tek eşleşme → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | Geri alındı |
|---|---|---|---|
| T2-MA2-1 | `amb-don 28s` → `27s` | YA2_06_donus_28sn_20_380_linear_durus_20_derece | cmp AYNI |
| T2-MA2-2 | `@keyframes amb-don from rotateY(20deg)` → `15deg` | YA2_06 | cmp AYNI |
| T2-MA2-3 | süzgeç 3B kutuya (`.tas3d` + `filter: drop-shadow(…var(--glow))`) | YA2_07_parlama_dis_sarmalayicida_3B_kutuda_filter_yok | cmp AYNI |
| T2-MA2-4 | yazı harf harf (`.amb-yz .h1 { animation: amb-harf … .14s }` + keyframes) | YA2_08_yazi_butun_olarak_cikar_harf_harf_yok_tas_kaybolmaz | cmp AYNI |
| T2-MA2-10 | 22 AYAR eşiği `1439` → eski `1365` (hakem bulgusunun geri dönüşü) | YA2_12_dar_genislik_kurallari… | cmp AYNI |
| T2-MA2-11 | iç span `vertical-align: top` silindi (satır tabanı kayar) | YA2_19_ad_kutusu_kuculur… | cmp AYNI |
| T2-MA2-14 | düğmenin kendisine `overflow: hidden` | YA2_19 | cmp AYNI |
| T2-MA2-12 | razor `IlkAd` → bütün ön adlar (`string.Join`) | YA2_18 (Hatice Gülizar, İsmail Hakkı) | cmp AYNI |
| T2-MA2-13 | razor `data-bas` silindi | YA2_18 (4/4) | cmp AYNI |
Kaynaklar mutasyon sonrası tur 2 yedekleriyle `cmp` AYNI; son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata, 6 Uyarı** — 6 uyarının hepsi başka şeritlerin test dosyalarında (`SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455`, `SonKalanlarTestleri.cs:593/824`, `Yildiz2Testleri.cs:194/234` xUnit analizörleri); bu paketin dosyalarında uyarı 0.

### T2.5 Denetim (`bash dotnet/denetim.sh`)
`DENETİM BAŞARISIZ` — tur 1 ile aynı iki ✗, ikisi de paket dışı: `P-30 DEMO_INSAN_SURESI` (görevde hariç) ve `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1` → `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (MASAK-ESZAMAN şeridi). Bu paketin sayaçları ✓: record tanımı 3 (tavan 3), satır içi style 24 (tavan 196), ModalPencere 48 (tavan 55), P-02 OLCUM_TABANI_KOPYA 0. 315 ✓.

### T2.6 Canlı doğrulama (API 5164, vekil 5165)
| Adım | Beklenen | Gerçek |
|---|---|---|
| `demo-tohumla.js --prova-kiraci --sema yildiz` (`Sistem__Anahtari` ortamdan; ilk deneme anahtarsız API ile **503** "Sistem:Anahtari yapılandırılmamış" — API anahtarla yeniden başlatıldı) | kiracı + veri | `01M4K78M1GQZQ7G4ZWEN65PWH5` · `{"cari":6,"urun":12,"satis":9}` |
| SQL: sahip adı uzun ad yapıldı (hakem senaryosu) | 1 satır | `Hatice Gülizar · Karaosmanoğlu · SAHIP` |
| `POST /auth/giris` (parola basılmadı) | 200 + jeton | 200, jeton var |
| `PATCH /gorunum {"sema":"platin"}` → 8 şema → `{"sema":"yildiz"}` | 200 ×2 | 200 "Görünüm kaydedildi" ×2; `GET /gorunum` → `{"sema":"yildiz",…}` |
| SQL `kiraci_ayarlari` | gorunum_semasi = yildiz | `01M4K78M1GQZQ7G4ZWEN65PWH5 · gorunum_semasi · yildiz` |
| `GET /css/tema-yildiz.css` | kaynakla aynı | `cmp` AYNI; `max-width: 1439px` 1 kez |
| Uygulama düğmesi | `HATİCE K.`, `data-bas="HK"` | ölçümde okundu (§ T2.2) |
**Temizlik:** API (PID 26335/26339) ve vekil (PID 26819) yalnız kendi PID'leriyle durduruldu; ilk (anahtarsız) API 26078/26099 da durduruldu; `lsof` 5164/5165 boş. `zsh kiraci-temizle.sh 01M4K78M1GQZQ7G4ZWEN65PWH5 --uygula` → kiracı satırı 0, kullanıcı satırı 0. **Tenants: başta 22, sonda 22.** Fikstür/jeton dosyaları `printf ''` ile boşaltıldı.

### T2.7 Sapmalar (tur 2)
| # | Konu | Gerekçe |
|---|---|---|
| T2-a | `KartAdi()` ve `data-bas` (kullanıcı düğmesi) değişti — dosya alanında "amblem" yazıyordu | Hakem bulgusu açıkça kullanıcı düğmesinin kısaltılmasını istedi; taşmayı amblemin eklediği 124 px doğurdu. Davranış/yetki/veri değişmedi; yalnız görünen ad biçimi. |
| T2-b | 1366–1439'da 22 AYAR gizli; 1001–1304'te sıkı düzen; ≤1080 saat gizli | Tuvalde yalnız 1440 var (orada değişiklik yok). Gerçek ölçülen genişliklerden türetildi (tur 1 de ≤1365'da aynı kalıpla 22 AYAR'ı gizliyordu; eşik yalnız genişledi). |
| T2-c | ≤600'de ad yerine baş harfler | Hakem önerisi; 375'te uzun adlar 44 px'lik dokunma hedefine sığar, erişilebilir ad tam. |
| T2-d | Uç durumda (ad > yer) "…" kesimi | YILDIZ-4D'nin "… yok" kuralı Tezgâh kalem/ müşteri adı içindir; burada alternatif taşma/kaydırmadır. Normal adlarda (≤109 px) hiçbir genişlikte devreye girmiyor (ölçüm: HAKAN T./HATİCE K. 30/30 kesik değil). |

### T2.8 Değişen dosyalar (tur 2)
```
?? dotnet/Kuyumcu.Web/Bilesenler/Kabuk/YildizUstMenu.razor   (KartAdi ilk ön ad + iç span + data-bas; IlkAd/BasHarfler/AdKulturu)
?? dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css            (yalnız "/* AMBLEM — DAR MASAÜSTÜ */" bloğu)
?? dotnet/Kuyumcu.Tests/YildizAmblem2Testleri.cs             (YA2_12 güncel, YA2_18, YA2_19; Menu() ad/soyad parametresi)
```
`Pages/Giris.razor` tur 2'de değişmedi. Dokunulan sayfalar (TEK DOKUNUŞ): tur 1 ile aynı iki dosya; ikinci dokunuş aynı paketin hakem düzeltmesi (gerekçe: hakem KRİTİK).

### T2.9 Koşul karşılama (tur 2 ekleri)
| Koşul | Durum | Kanıt |
|---|---|---|
| Hakem KRİTİK: 1024/1280/1366/1440/375 × BASİT/PRO × açık/koyu scrollWidth = innerWidth | KARŞILANDI | § T2.2 40/40 + 100/100 rota + 60/60 ad zorlaması |
| Amblem ölçü/yer değişmez | KARŞILANDI | amblem kutusu her genişlikte tur 1 ile aynı; 20/20 kare FARK 0 |
| 1440 tuval birebir | KARŞILANDI | şerit önce/sonra yalnız saat hanesi farklı; kutular birebir |
| 8 şema FARK 0 | KARŞILANDI | 96/96 |
| Mutasyon ≥4 (28→27, 20→15, filter 3B, harf harf) + tur 2 kuralları | KARŞILANDI | 9/9 kırmızı, cmp AYNI |
| build 0 hata · denetim (P-30 hariç) | KISMEN — 0 hata; 6 uyarı ve GIRDI-2 başka şeritlerin | § T2.4, T2.5 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 9 kırmızının sahibi başka şeritler (§ T2.3) |

### T2.10 Açık kalanlar (sahibiyle)
- `Pages/Islemler.razor` 1414 satır (3 test) — t5 şeridi (19:05 değişikliği).
- `SKC01` tahsilat çekmecesi (`Pages/Cariler.razor` 18:47) — t5 / ISLEM-BANT-2.
- `RD_KH01/02` — RAPOR-4 şeridi.
- `GIRDI-2 TEST_GECERSIZ_FIKSTUR` — MASAK-ESZAMAN şeridi; `P-30` — kapsam dışı.
- 6 derleme uyarısı — başka şeritlerin test dosyaları.
- Giriş ekranının soğuk açılışta Yıldız olması (§ 2 #6) — kurucu kararı.
