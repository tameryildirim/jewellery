# PAKET YILDIZ-TIPO — Uygulama raporu (tur 3 — doğrulayıcı KRİTİK kapatıldı; altta tur 2 kaydı)

**Tarih:** 10 Ekim 2026 · **Şerit:** t9 (API 5188 · vekil 5189 · CDP 9981–9986) · **Model:** Opus · **Hasımsız paket** (kanıt bağlayıcı) · **COMMIT YOK**

Kurucu bulgusu (09.10): "yazı tipleri özellikle dropdownlarda eski temalardan kalma boyutları bile farklı geliyor, tam bir kontrol sağlansın" + "birebir tasarım neyse aynısını istisnasız".


## TUR 3 (10.10 akşam) — Doğrulayıcı KRİTİK: koyu Yıldız'da seçili sekme eski temanın dolu bloğu

**Durum: kapatıldı.** Şerit t9 (API 5188 · vekil 5189 · CDP 9981/9983/9984/9982/9986). Yalnız görünüm; para/veri/yasal davranış değişmedi; razor değişmedi; migration yok. COMMIT YOK.

### T3.1 Kök neden (doğrulandı)
- `dugme.css` `html[data-mod] .ana-icerik .sekmeler-serit > .sekmeler-sekme.secili { background: var(--secili-dolgu) }` = (0,5,1); `tema-yildiz.css` Yıldız kuralı da (0,5,1) ve dugme.css SONRA yükleniyor → kazanıyordu. Ölçüm (`t3/yan/olcum.json`): ÖNCE `/kur` "Güncel kur" zemin rgb(243,242,239) yazı rgb(0,0,0); SONRA zemin saydam, yazı rgb(243,242,239), alt çizgi 1 px rgb(243,242,239) — tuval Musteri4 "HEPSİ" (b2: zemin saydam, yazı --m) ile aynı dil.
- İkinci kök neden: `tuval-satis.css` `:root[data-tema="karanlik"] .islem-sayfa.tuval-pro .tuval-pro-sekmeler > .sekme.secili` = (0,7,0) (/islemler PRO sekmeleri). Canlı mutasyon gösterdi: `:not(#_)` kaldırılınca (0,6,2) /kur'da yine dolu (aşağıda MT3-L).

### T3.2 Tarama aracına ZEMİN ölçüsü (doğrulayıcı istemi)
`betikler/tur/yildiz-tipo-tara.mjs`: her metin düğümünün ETKİN zemini (kendisi ya da ilk saydam olmayan atası) ölçülür; izinli: saydam / `--yuzey-govde`; `--metin` dolgu YALNIZ b1 hapında (`.dugme-birincil, .btn-birincil, .yildiz-b1, *-b1`) ve sekme/segment (`.sekme, .sekmeler-sekme, .musteri-segment-oge, [role=tab]`) b1 SAYILMAZ. Kural dışı → bulgu `zemin` (renk @ sahip öğe).

Bu ölçü KRİTİK'ten fazlasını buldu — eski temadan kalan zeminler (hepsi bu paketin kapsamı: "eski temadan kalan … renk Yıldız'da HİÇ kalmaz"):

| Bulgu (ÖNCE, BASİT tam tarama) | Ekran | Düzeltme (yalnız `/* YILDIZ-TIPO */` bloğu, T7/T8) |
|---|---|---|
| Seçili sekme dolu blok (koyu) — `#kur-sekme-guncel #gd-sekme-giderler #gp-sekme-pusulalar #uy-sekme-tespit #bildirim-sekme-acik #evrak-suzgec-tumu #rp-sekme-ozet #kz-sekme-ozet #sa-sekme-odeme` + `/islemler` PRO `#islem-tur-SATIS` | 9+ ekran (doğrulayıcının listesi) | T7: `html:root[data-sema=yildiz] body .ana-icerik :is(.sekmeler-serit > .sekmeler-sekme, .tuval-pro-sekmeler > .sekme).secili:not(#_)` → saydam, yazı/alt çizgi `--metin` (1,6,2) |
| Kullanıcı menüsü dil seçici TR/EN: gri kapsül (--yuzey-3) + dolu seçili (331 düğüm × 4) | tüm ekranlar (menü katmanı) | T7: aynı menüdeki Görünüm anahtarının (`.yildiz-mod-oge`) kalıbı: altı çizili yazı 15/400, soluk/--metin |
| Bilgi/uyarı kutuları --yuzey-2/--uyari-yuzey: `.degismez-ipucu` (12 tür), `.sayfa-basi-aciklama` paneli (9 ekran), `.uyari-kutu`, `.yasal-not`, `.emanet-bilgi-metin`; PRO ürün çekmecesi `.dosya-secici`, `.gorsel-galerisi` | /raporlar*, /yasal-sinirlar, /sayim, /evraklar/takvim, /kullanicilar, /yetkiler, /veri-disa-aktarim, /pos/tahsilatlar, /bildirimler, /emanet, /kur, /gider, … , /urunler (PRO) | T8: zemin saydam (kenar korunur) |
| Örten yüzeyler: `.cekmece-alt` (çekmece altı), `.menu-arama-satir` (arama paleti), yapışkan tablo başlığı `tr[id$=-baslik-satiri] > th` | /urunler /satin-alma, arama katmanı, 10 tablo | T8: `--yuzey-govde` (saydam değil — içerik altından görünmesin) |
| Vurgulu satır `tr.satir-vurgu` (--vurgu-yuzey) | /yetkiler, /fiyatlandirma | T8: dolgu yok, ilk hücrede 2 px `--metin` işaret (ölçüm: `inset 2px 0 0 rgb(243,242,239)`) |
| Seçili seçenek hapı `.secim-secenek.secili` | /faturalar /hurda /pos/tahsilatlar | T8: dolgusuz, kenar+yazı `--metin` |
| Tezgâh tür ızgarası seçili karo `.ekran-karo-birincil` (olcek.css b1 gibi dolu) | /islemler BASİT "SATIŞ ▾" katmanı | T8: dolgusuz, kenar+yazı `--metin` (yalnız renk; biçim/yerleşim YILDIZ-4C'nin) |
| Dolu kırmızı tehlike düğmesi `.btn-tehlike` ("Pasife al" satır eylemi) | /urunler | T8: b3 hapı, kenar+yazı `--tehlike` |

Tuvalde olmayan hiçbir öğe eklenmedi; yalnız zemin/renk/kenar rengi değişti, boyut/boşluk/yerleşim değişmedi.

### T3.3 ÖNCE / SONRA (zemin ölçüsü dahil; 42 ekran × 1440+375 × koyu+açık, her açılır katman)

| Koşum | Ekran | Sayfa ölçümü | Katman | Düğüm | **KURAL DIŞI** | Native select | Dağılım |
|---|---|---|---|---|---|---|---|
| ÖNCE BASİT (`once-t3/`) | 42 | 168 | 746 | 23 014 | **2 014** | 0/0 | zemin 2014 (tipografi 0) |
| ÖNCE PRO, sekmeli 10 ekran, katmansız (`once-t3pro/`) | 10 | 40 | 0 | 1 726 | **58** | 0/0 | zemin 58 |
| Canlı mutasyon MT3-L (T7'den `:not(#_)` çıkarıldı), PRO 7 sekmeli ekran | 7 | 28 | 0 | 1 372 | **2** (`#kur-sekme-guncel` koyu ×2) | 0/0 | zemin 2 → geri alındı, cmp AYNI |
| **SONRA BASİT** (`sonra-t3/`) | 42 | 168 | 744 | 23 061 | 16 → yalnız /islemler seçili karo (karo kuralı tarama sürerken eklendi) | 0/0 | — |
| **SONRA PRO 1440 + 375 (34 ekran)** (`sonra-t3/`; 30 dk arka plan sınırında 375'in /isletme'sinde kesildi — açıkça) | 42 | — | — | — | 234 → yalnız /urunler `.dosya-secici`/`.gorsel-galerisi` | 0/0 | — |
| **SONRA PRO 375 kalan 7 ekran + /urunler** (`sonra-t3b/`, son CSS) | 8 | 16 | 104 | 4 824 | **0** | 0/0 | — |
| **SONRA PRO 1440 /urunler** (`sonra-t3c/`, son CSS) | 1 | 2 | 40 | 3 404 | **0** | 0/0 | — |
| **SONRA BASİT /islemler + /urunler** (`sonra-t3d/`, son CSS) | 2 | 8 | 102 | 5 226 | **0** | 0/0 | — |

→ Son CSS ile her ekran × mod × genişlik × tema kombinasyonu en az bir kez 0 kural dışı ölçüldü (son iki eklenti — karo, dosya seçici/galeri — yalnız seçici ekler, başka ekranlardaki sonucu azaltabilir, artıramaz; etkilendikleri ekranlar son CSS ile yeniden tarandı). Tipografi (aile/boy/ağırlık/aralık/renk) kural dışı: 0 (tur 2 ile aynı).

### T3.4 Gerçek tarayıcı sıkışma (`sikisma-olc.js`, 14 ekran × BASİT/PRO × 1440/375 × koyu/açık = 112 ölçüm, `t3/sikisma-t3.txt`)
K1/K3/K5/K7/K10/K11/K12/Y hepsi 0. K6 "0/n" (kural dışı 0). Tek sıfır olmayan: **/islemler 375 K9 3** ("+ Başka ödeme"/"+ Para üstü" ⟂ Fark "0 ₺") — tur 2'de A/B ile TIPO bloğundan bağımsız kanıtlanan YILDIZ-4C sabit alt çubuğu (açık kalan 1, aynen).

### T3.5 Yan yana görüntüler (Read ile bakılan: 3 — biri yanlış göreli yolla boş çıktı, yeniden üretildi)
`/private/tmp/kuyumcu-serit/pYILDIZ-TIPO/t3/yan/`
- `K1-sekme-kur.png` — TUVAL Musteri4 koyu (HEPSİ altı çizili) | ÖNCE /kur koyu 1440 ("Güncel kur" beyaz dolu blok, yazı kenara değiyor) | SONRA (altı çizili yazı, tuvalle aynı dil). Gözle doğrulandı.
- `K2-375-dil.png` — ÖNCE /gider koyu 375 ("Giderler" dolu blok) | SONRA (altı çizili) | ÖNCE menü dil seçici (dolu TR, 12 px/500) | SONRA (altı çizili TR/EN, Görünüm "Basit/Profesyonel" ile aynı). Gözle doğrulandı.
- `K3-vurgu-acik.png` — /fiyatlandirma vurgulu satır + /kur AÇIK tema (Read bütçesi için açılmadı; ölçüm: açıkta seçili sekme zemin saydam, yazı/alt çizgi rgb(20,20,20) = açık --m).
ÖNCE görüntüleri dosyaya dokunmadan alındı: yalnız ölçüm sekmesinde CSSOM'dan T7/T8 kuralları silindi (12 kural).

### T3.6 Eski 8 şema — piksel FARK (`fark-ab.mjs`, animasyon dondurulmuş; kullanıcı şeması API ile platin yapıldı, sonra `{"sema":null}` ile kiracı şemasına (yildiz) döndü — GET doğruladı)
| Mod | Karşılaştırma (/kur /gider /urunler /fiyatlandirma × 8 şema × 2 tema) | Dosya AÇIK↔KAPALI | **TIPO VAR↔YOK** | Kararsız |
|---|---|---|---|---|
| PRO | 64 | 0 | **0** | 2 |
| BASİT | 64 | 0 | 1 (kararsız satır: /urunler menekse açık — B hâli iki çekimde farklı, A=C) | 1 |
→ 8 eski şemada etki 0; blok yalnız `html:root[data-sema="yildiz"]` (YT_08 + Y1_02 kilitli, yeşil).

### T3.7 Testler
- Yeni: `YT_10_secili_sekme_dolgusuz_alti_cizili_ve_eski_dolgu_kuralini_yener` (değerler + CSS Seçici-4 özgüllük hesabı: dugme.css/tuval-satis.css/olcek.css/app.css'teki her seçili sekme ve dil seçici rakibinden KESİN yüksek + araçta zemin ölçüsü ve sekme≠b1 kuralı), `YT_11_metin_zemini_eski_gri_kutu_vurgulu_satir_secenek_tehlike_dolgusu_kalmaz` (T8 değerleri + her eski kural rakibinden kesin yüksek özgüllük).
- ÖNCE (CSS düzeltmesi yokken): YT_10 KIRMIZI ("… { background } bulunamadı") — `t3/test-once.log`.
- SONRA filtreli (`YildizTipo|Yildiz1Testleri`): **23/23 geçti** (`t3/test-e.log`).
- Tam süit (bir kez, `/private/tmp/kuyumcu-serit/trx-pYILDIZ-TIPO-t3/_Mac_2026-10-10_16_33_18.trx`, KUYUMCU_TEST_DB=kuyumcu_test_t9): **12 185 toplam · 12 174 geçti · 8 başarısız · 3 koşulmadı**. Not: tam süit YT_11'in karo + dosya seçici/galeri satırları eklenmeden önceki test koduyla koştu; o eklemelerden sonra YildizTipo filtreli 23/23 yeşil (tam süit tur başına bir kez kuralı gereği yeniden koşulmadı).

| Kırmızı | Bu paket mi? | Kanıt |
|---|---|---|
| RaporDortTestleri.RD_KH01 / RD_KH02 | Hayır | Filtreli tekrarda yine kırmızı; tur 2'de de vardı (rapor dönemi 400 gün sınırı, sunucu) |
| S24EskiAltinEkranTestleri.S24_Satici_Kimligi… / S24_Fark_Sifir… | Hayır | Filtreli tekrarda kırmızı; bUnit (CSS yüklemez), dosya CSS okumaz; eski altın/işlem bandı şeridinin yarım işi (`Sozlukler.EskiAltin.cs` izlenmeyen yeni dosya) |
| IslemBant2BilesenTestleri.IB2_11 ×2, SATIS3EkranTestleri.IB2_34 | Hayır | Filtreli tekrarda YEŞİL (paralel koşum kararsızlığı; ISLEM-BANT şeridi) |
| FaturaEkranTestleri.Y4C_K12 (basit: True) | Hayır | Filtreli tekrarda YEŞİL (4C testi; tur 2'de basit: False kararsızdı) |
→ **Bu paketin yeni kırmızısı 0.**

### T3.8 Mutasyon (tur 3: 12 dosya mutasyonu + 1 canlı; hepsi kırmızı, hepsi cmp AYNI)
Yöntem: yedek `/private/tmp/kuyumcu-serit/mut-yedek/YILDIZ-TIPO-t3/` → `perl -0777 -pi` tek eşleşme → filtreli test → geri kopya → `cmp` (`t3/mut3.zsh`, günlükler `t3/mut-MT3-*.log`).

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| MT3-1 | T7 seçili sekme `background: transparent` → `var(--secili-dolgu)` | YT_10 | AYNI |
| MT3-2 | T7 seçicisinden `:not(#_)` çıkar | YT_10 | AYNI |
| MT3-3 | T7 `border-bottom-color: var(--metin)` → `transparent` | YT_10 | AYNI |
| MT3-4 | Dil seçici seçili yazı `var(--metin)` → `var(--vurgu-metin)` | YT_10 | AYNI |
| MT3-5 | Tarama aracı: zemin kuralından "sekme b1 sayılmaz" koşulunu sil | YT_10 | AYNI |
| MT3-6 | T8 kutular `background: transparent` → `var(--yuzey-2)` | YT_11 | AYNI |
| MT3-7 | Tehlike düğmesi zemini → `var(--tehlike-dolgu)` | YT_11 | AYNI |
| MT3-8 | Vurgulu satır işaret gölgesini sil | YT_11 | AYNI |
| MT3-9 | Vurgulu satır hücre zemini → `var(--yuzey-2)` | YT_11 | AYNI |
| MT3-10 | Seçili seçenek hapı zemini → `var(--vurgu-yuzey)` | YT_11 | AYNI |
| MT3-11 | Seçili tür karosu zemini → `var(--vurgu)` | YT_11 | AYNI |
| MT3-12 | Kutular listesinden `.gorsel-galerisi`'ni çıkar | YT_11 | AYNI |
| MT3-L (canlı) | T7'den `:not(#_)` çıkar → başsız Chrome PRO 7 ekran | tarama: **2 kural dışı** (`#kur-sekme-guncel` dolu, koyu 1440+375) | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata · 6 Uyarı** — 6 uyarının hepsi başka şeritlerin test dosyalarında (SUBEKAPSAMASinirTestleri.cs:49, KesifP2Testleri.cs:455, SonKalanlarTestleri.cs:593/824, Yildiz2Testleri.cs:194/234); bu paketin dosyalarında 0.

### T3.9 Denetim (`bash denetim.sh`)
315 ✓ · 2 ✗ — ikisi de bu paketin değil: P-30 DEMO_INSAN_SURESI (istemde hariç), GIRDI-2 TEST_GECERSIZ_FIKSTUR 1 (`Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, başka şerit). CSS sayaçları ✓ (P-18 SABIT_RENK 0, TEMA_HAM_PALET_VURGU 0, P-36/P-36b KONTRAST 0, DUGME2_* 0). (Tur 2'deki P-11 artık ✓.)

### T3.10 Canlı (API + SQL)
Test kiracısı `PAKETYILDIZTIPO-TEST Tunalılar Kuyumculuk` (01M4JYR1CBKY3Q8960NXK1GFWP), `demo-tohumla.js --prova-kiraci --sema yildiz` ile ürünün uçlarından kuruldu (6 cari, 12 ürün, 9 satış). `POST /api/v1/auth/giris` 200; `PATCH /api/v1/kullanici/gorunum {"sema":"platin"}` 200 → GET `sema: platin, semaKaynagi: kullanici`; `PATCH {"sema":null}` 200 → GET `sema: yildiz, semaKaynagi: kiraci, ozelSema: null` (PATCH "null = sil"); `PATCH /api/v1/kullanicilar/ben/arayuz-modu {"arayuzModu":"BASIT"}` 200 `{"arayuzModu":"BASIT","etkinArayuzModu":"BASIT"}`. SQL: kiracı satırı vardı (tenants 23); `zsh kiraci-temizle.sh … --uygula` sonrası tenant_id kolonlu TÜM tablolarda kalan satır **0**, `tenants` **22** (başlangıç değeri). 5188/5189 ve bu şeridin Chrome süreçleri durduruldu (portlar boş). 5199/5200 ve diğer şeritlerin portlarına dokunulmadı.

### T3.11 Dokunulan dosyalar (tur 3)
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` — YALNIZ `/* YILDIZ-TIPO */ … /* YILDIZ-TIPO sonu */` bloğu (T7, T8 eklendi). 4C kurallarına dokunulmadı.
- `dotnet/Kuyumcu.Tests/YildizTipoTestleri.cs` — YT_10, YT_11, `Ozgulluk()`/`UstVirgul()` yardımcıları.
- `dokumanlar/09-uygulama-kayitlari/betikler/tur/yildiz-tipo-tara.mjs` — zemin ölçüsü.
- Bu rapor. Razor sayfası: yok. (git: üçü de `??` izlenmeyen; COMMIT YOK.)

### T3.12 Açık kalanlar (tur 3)
1. /islemler 375 K9 3 — YILDIZ-4C sabit alt çubuğu (tur 2 maddesi 1, değişmedi).
2. YILDIZ-4C'ye bilgi: Tezgâh tür ızgarasının seçili karosu (`.ekran-karo-birincil`) artık Yıldız'da dolgusuz (yalnız renk, bu paketin bloğunda); 4C bu kalıbı farklı isterse kendi bloğunda daha özgül yazabilir.
3. Tam süit kırmızıları: RD_KH01/02 (rapor şeridi), S24 ×2 (eski altın/işlem bandı şeridi) — sahipleriyle; IB2_11/IB2_34/Y4C_K12 kararsız.
4. Denetim GIRDI-2 (MASAKESZAMANSinirTestleri.cs:187) ve P-30 — başka şerit/kurucu.
5. Tarama aracı satır hover zeminini ölçmez (yalnız açılış hâli); `.veri-tablosu tbody tr:hover` (--yuzey-2) gibi durumlar ölçüm dışı — gözlem.

---

# TUR 2 KAYDI (değişmeden)

## 1. Devralınan durum (tur 1, 09.10 gece)

Önceki koşum `/* YILDIZ-TIPO */` bloğunu (T1–T6), `YildizTipoTestleri` (YT_01–08) ve `betikler/tur/yildiz-tipo-tara.mjs` aracını yazmış, ama "sonra" taraması ve rapor yoktu. Ayrıca:
- Tarama aracında **sözdizimi hatası** vardı (yorum satırı `for` döngüsünün `}`'ını yutuyordu → `node --check`: `SyntaxError: Unexpected token 'finally'`). Düzeltildi.
- Tur 1'in 5188/5189/9981 süreçleri öksüz kalmıştı (09.10 23:37'den beri); yalnız bu şeridin PID'leri durduruldu, güncel derlemeyle yeniden açıldı.

## 2. Tur 2'de bulunan ve düzeltilen kusurlar (canlı başsız Chrome ile)

| # | Kusur | Kök neden (ölçüldü) | Düzeltme (yalnız `tema-yildiz.css` YILDIZ-TIPO bloğu) |
|---|---|---|---|
| 1 | Tur 1'in 600/700 → 500 ağırlık kuralı HİÇBİR ekranda çalışmıyordu (h2/h3/th/strong 600–700 kaldı, 114 düğüm @1440 koyu BASİT) | Dışlama `:not([id^="yildiz-"] *)`; oysa `AnaYerlesim.razor` tüm sayfayı `#yildiz-kabuk` ile sarıyor → her şey dışlanıyordu | `:not(.yz *, [id^="yildiz-"]:not(#yildiz-kabuk), [id^="yildiz-"]:not(#yildiz-kabuk) *)`; YT_03'e çıplak dışlamayı yasaklayan regex |
| 2 | Tarama düzeltmeden sonra eski CSS'i ölçüyordu | Statik CSS `Cache-Control` taşımıyor → sezgisel önbellek | Araca `Network.setCacheDisabled` |
| 3 | Tezgâh barkod/ad öneri satırı 600 | `.fis-arama-ad` `.yz` içinde dışlanmıştı | `.fis-arama-ad` 400 (açılır liste birincil satırı = tuval `.sat` 19/400) |
| 4 | Seçili sekme 600 (PRO, `#islem-tur-SATIS`) | olcek.css / tuval-satis.css `.tuval-pro-sekmeler > .sekme.secili` | `:is(.sekme, .sekmeler-sekme).secili` 500 |
| 5 | "Ayarları kaydet" (`#is-ayar-kaydet`, sınıfsız `<button>`) yazısı #FFF, zemin gri | Hiç kural yok; koyu color-scheme'de tarayıcı buttontext/buttonface | Sınıfsız düğme → tuval b3 kalıbı (ince kenarlı hap, `--metin`), `.yz`/`.yildiz-ust` hariç |
| 6 | /raporlar 375'te panel sayısı 34,21 px (ölçek dışı) | `.panel-soru-sayi` akışkan `min(…, cqi)` | Akışkan boy tuval basamağına (`--tipo-akis` + sign() basamak formülü) |
| 7 | **Giriş kutuları 19 px — tuvalde YOK.** Tuval `.alan input` ve `.k .v` = 22 px / 300 (46 dosya); 19 px tuvalde yalnız liste satırı | Tur 1 uyarlaması | Girdi/Seçici tetiği/select: 22 px · 300 · −0,01em (Tezgâh `.yz-duzen` ile aynı); arama kutusu ve Seçici araması 22 px · 300 · normal (tuval `.alan input`); seçenek satırı 19/400 kaldı |
| 8 | **Form etiketleri 14 px/500 (modal) ve 18 px/500 (çekmece) — tuvalde YOK.** Tuval `.k label` = 12 px · .18em · BÜYÜK HARF · `--s` · 400 (36 dosya) | YILDIZ-1 "15 px altı 500" okunurluk uyarlaması (kurucu 09.10 İPTAL etti) | `.alan-etiket`, `.alan > label`, `.secici-alan > label` → 12/400/.18em/uppercase/soluk; kutucuk etiketi (`.alan-onay`, `.onay-etiket`) hariç (ilk denemede /siparisler "YALNIZ GECİKENLER" "Yenile"ye biniyordu → düzeltildi, K9 0) |
| 9 | /raporlar PRO 375: "Ayrıntı →" iki satıra bölünüp metne biniyordu (K3 1, K9 2) | Tuval gövde aralığı (normal) satırı uzattı — A/B ile kanıtlandı | `.uyari-metni > a[id$="-git"]` `white-space: nowrap` |

## 3. Tarama aracı ve ölçüt (tuvalden çıkarılan izinli küme)

`dokumanlar/09-uygulama-kayitlari/betikler/tur/yildiz-tipo-tara.mjs` (kalıcı). Menüdeki TÜM ekranlar (Tüm ekranlar karoları + /ekranlar = **42 rota**; 39 tuval ekranı + /ekranlar + iki alt rota) × BASİT/PRO × 1440/375 × koyu/açık. Her ekranda görünen her metin düğümü + girdi/select/textarea + yer tutucu; sonra açılabilen her katman (Seçici paneli, "+ ekle" katlanırları, details, çekmece, modal, menü, bildirim — kayıt/silme/gönderme yok) açılır, yeni görünen düğümler ölçülür.

| Özellik | İzinli küme (tuval `ham/yildiz-tuval/kaynak/*.dc.html`) |
|---|---|
| Aile | ilk aile `Inter` |
| Boy (px) | 11 12 13 14 15 16 17 18 19 20 22 28 36 40 52 56 64 72 104 112 |
| Ağırlık | 200 300 400 500 |
| Harf aralığı (em) | 0 −.005 −.01 −.035 .02 .04 .06 .08 .1 .14 .18 .22 .3 |
| Renk | yalnız Yıldız belirteçleri `--m --s --p --h --z --iyi --kotu` (hesaplanmış rgb) |
| Muaf | amblem (`#yildiz-marka`, ayrı paket), kâğıt (`.belge*`, `.urun-etiket*`), görsel gizli |

## 4. ÖNCE / SONRA tablosu

| Koşum | Ekran | Sayfa ölçümü | Katman | Düğüm | **KURAL DIŞI** | Native select (kural dışı/görünen) | Özellik dağılımı |
|---|---|---|---|---|---|---|---|
| ÖNCE BASİT (09.10 23:13, `once/`) | 42 | 168 | 746 | 23 068 | **8 664** | 0/0 | aralık 8340 · ağırlık 2184 · boy 216 · yer tutucu rengi 114 |
| ÖNCE PRO (09.10 22:58, `once1/`) | 40 | 160 | 730 | 27 504 | **2 916** | 0/0 | ağırlık 2546 · boy 292 · aralık 200 · yer tutucu 134 · renk 4 |
| Ara (tur 2, 1440 koyu BASİT, kural-1 kusuru) | 42 | 42 | 147 | 5 725 | 114 | 0/0 | ağırlık 114 |
| **SONRA BASİT** (10.10, `sonra2/`) | 42 | 168 | 738 | 22 852 | **0** | **0/0** | — |
| **SONRA PRO** (10.10, `sonra2/`) | 42 | 168 | 766 | 28 290 | **0** | **0/0** | — |
| Son düzeltme sonrası (arama aralığı) BASİT+PRO | 4 (/urunler /cariler /islemler /kur) | 32 | 264 | 13 868 | **0** | 0/0 | — |

Her SONRA satırı 4 kombinasyondur (1440+375 × koyu+açık). Native `<select>`: Yıldız'da hiçbir ekranda görünür native select kalmadı (0/0) — açılır listelerin tamamı ortak Seçici paneli; yine de select/option kuralı blokta (select 22/300, option 19/400) ve testte.
Notlar: ÖNCE PRO 40 ekran (o tarihte menü 40 rota); "önce" taramaları tur 1'in aynı ağaçtaki koşumlarıdır (blok öncesi). Tarama yan sayacı "kesik yazı 249–251" kural değildir: örnekleme (A/B) gösterdi ki hepsi 1 px genişlikte ekran okuyucu öğeleri (`.gorsel-gizli`, `.gorunmez`, `sayfa-basi-aciklama` clip) — yatay kayan sayfa 0.

## 5. Gerçek tarayıcı sıkışma ölçümü (`sikisma-olc.js`, 42 rota × BASİT/PRO × 1440/375 × koyu/açık = 336 ölçüm)

Sıfır olmayan sayaçların TAMAMI A/B ile (aynı sayfada TIPO bloğu VAR/YOK, `ab2.mjs`) incelendi:

| Rota | Sayaç | VAR | YOK (blok çıkarılmış) | Sahip / gerekçe |
|---|---|---|---|---|
| /cariler 1440 (BASİT+PRO) | K7 hizasız 1 (`div.yz-bas` "7 MÜŞTERİ · 2 ALACAK…") | 1 | 1 | Önceden var — YILDIZ-4C Müşteriler başlığı (bu paket değil) |
| /ekranlar, /isletme | K8 jargon 1 ("r-zarar"; kiracı adı "PAKETYILDIZTIPO-TEST") | 1 | 1 | Önceden var — metin/fikstür, tipografi değil |
| /islemler 375 (BASİT+PRO) | K9 binen 3 ("+ Başka ödeme"/"+ Para üstü" ⟂ Fark "0 ₺") | 3 | 0 (812 boy) · **5 (740 boy)** | Önceden var: ≤1000 px'te Fark+Kaydet `position: fixed` alt çubuğu (YILDIZ-4B/4C) akıştaki içeriğin üstünü açılışta örtüyor; içerik 300 px alt boşlukla kaydırılabilir. Blok yokken bile 375×740'ta 5 binme → ekran boyuna bağlı mevcut tasarım; tuval gövde aralığı (normal) içeriği birkaç px aşağı itince 812'de de görünür oldu. Tezgâh yerleşimi bu paketin alanı DEĞİL → **YILDIZ-4C'ye devir** |

Bu paketin ürettiği iki gerçek kusur (#8 kutucuk etiketi, #9 "Ayrıntı →") düzeltildi ve yeniden ölçüldü: /siparisler K9 0, /raporlar K3 0/0 K9 0.

## 6. Yan yana görüntüler (tuval kalıbı | uygulama) — Read ile bakılan 3

`/private/tmp/kuyumcu-serit/pYILDIZ-TIPO/yan/`
1. `K1-koyu-liste-secici.png` — tuval Y_Urunler | /urunler Seçici paneli açık (koyu 1440): tetik "Hepsi" 22/300, panel arama "Listede ara" 22/300 normal + alt çizgi, seçenekler 19/400, etkin satır altı çizili, kıl çizgi ayraç, gölge yok; etiketler DURUM/MADEN/AYAR 12 px büyük harf .18em — tuvalin "ÜRÜN/BARKOD/AYAR" başlık diliyle aynı.
2. `K2-koyu-form-cekmece-modal.png` — tuval Y_Isletme (`.k` form) | çekmece "Ürünü Düzenle" | modal "Yeni Cari" (koyu 1440): etiket 12/400/.18em/büyük harf soluk, değer 22/300 — tuval `.k label` / `.k .v` birebir.
3. `K4-koyu375.png` — 375: Seçici paneli, çekmece, modal; sığma ve kesilme yok (tuvalde telefon hâli yok, 375 tuval ölçekli gösterim).
Ayrıca açık tema bileşimi `K3-acik-form-secici.png` üretildi (Read bütçesi için açılmadı; ölçüm JSON'u `t3-acik-olcum.json`: tetik/arama 22/300 normal, seçenek 19/400, renk rgb(20,20,20) = açık `--m`).

## 7. Eski 8 şema — piksel FARK

`sema-fark.mjs` (YILDIZ-4C aracı) bu koşumda BASİT 64 karşılaştırmada **9 FARK** verdi; inceleme: hepsi AÇIK/KAPALI/AÇIK sırasında iki kare arasında gidip gelen periyodik animasyon (aynı hâl iki kez çekilince farklı sha). Aracı değiştirmeden (4C'nin dosyası), animasyon/geçiş/imleç dondurularak kendi A/B betiğimle (`fark-ab.mjs`: A = dosya açık, B = TIPO bloğu çıkarılmış kopya, C = dosya kapalı, A ve B ikişer kez) ölçüldü:

| Mod | Karşılaştırma (4 rota × 8 şema × 2 tema) | Dosya AÇIK↔KAPALI | **TIPO bloğu VAR↔YOK** | Kararsız (aynı hâl iki çekimde farklı) |
|---|---|---|---|---|
| BASİT | 64 | 1 (kararsız satır) | **0** | 2 |
| PRO | 64 | 1 (kararsız satır) | 1 (kararsız satır) | 5 |
| PRO tekrar (/ , /cariler) | 32 | **0** | **0** | 1 (başka satır) |

Kalan tekil farkların hepsi "kararsız" satırlardadır (sayfa kendiliğinden değişiyor: kur/saat yenilemesi) ve tekrar çekimde kayboluyor → **8 eski şemada bu paketin piksel etkisi 0**. Ayrıca blokta her seçici `html:root[data-sema="yildiz"]` ile başlar (YT_08 + Y1_02 kilitler). Not (4C'ye): `sema-fark.mjs` animasyon dondurmadığı için yanlış FARK üretiyor.

## 8. Testler

Yeni/değişen: `Kuyumcu.Tests/YildizTipoTestleri.cs` (YT_01–YT_09; YT_09 yeni, YT_03/06/07 güncellendi). Filtreli (`YildizTipo|Yildiz1Testleri`): **21/21 geçti**.

Tam süit (bir kez, `/private/tmp/kuyumcu-serit/trx-pYILDIZ-TIPO/_Mac_2026-10-10_14_17_36.trx`, KUYUMCU_TEST_DB=kuyumcu_test_t9): **12 101 toplam · 12 094 geçti · 4 başarısız · 3 koşulmadı**.

| Kırmızı | Bu paket mi? | Kanıt |
|---|---|---|
| RaporDortTestleri.RD_KH01 / RD_KH02 | Hayır | Tekrarda da kırmızı: `BadRequestException: Rapor dönemi en çok 400 gün olabilir.` (KarZararServisi.DonemDogrula) — sunucu iş kuralı, CSS'e değmez |
| KesifP5EkranTestleri.SATIS4c_…(PROFESYONEL) | Hayır | Filtreli tekrarda YEŞİL (paralel koşum kararsızlığı; bUnit CSS yüklemez) |
| FaturaEkranTestleri.Y4C_K12_…(basit: False) | Hayır | Filtreli tekrarda YEŞİL (4C testi) |

→ **Bu paketin yeni kırmızısı 0.**

## 9. Mutasyon kanıtı (13/13 kırmızı, hepsi cmp AYNI)

Yöntem: kopya `/private/tmp/kuyumcu-serit/mut-yedek/YILDIZ-TIPO-t2/` → `perl -0777 -pi` tek eşleşme → filtreli test → geri kopya → `cmp`.

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| MT2-1 | Ağırlık kuralı dışlamasını eski çıplak `[id^="yildiz-"] *` hâline döndür (kabuk kusuru) | YT_03 | AYNI |
| MT2-2 | Girdi boyu 22 → 19 px | YT_07 | AYNI |
| MT2-3 | Etiket `uppercase` → `none` | YT_07 | AYNI |
| MT2-4 | Sınıfsız düğme rengi `var(--metin)` → `inherit` | YT_09 | AYNI |
| MT2-5 | `.fis-arama-ad` 400 kuralını boşalt | YT_09 | AYNI |
| MT2-6 | Seçili sekme 500 → 600 | YT_03, YT_09 | AYNI |
| MT2-7 | Panel sayısı `--tipo-akis` adını boz | YT_09 | AYNI |
| MT2-8 | "Ayrıntı →" kuralından Yıldız kapsamını kaldır | YT_08, Y1_02 | AYNI |
| MT2-9 | "Ayrıntı →" `nowrap` → `normal` | YT_09 | AYNI |
| MT2-10 | Gövde aralığı `normal` → `−0.011em` | YT_02 | AYNI |
| MT2-11 | Yer tutucu rengi → `GrayText` | YT_05 | AYNI |
| MT2-12 | Tarama aracı boy kümesine 21 px ekle (tuvalde yok) | YT_01 | AYNI |
| MT2-13 | Arama kutusu aralığı `normal` → `−0.01em` | YT_07 | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı · 0 Hata**.

## 10. Denetim (`bash denetim.sh`)

314 ✓ · 3 ✗ — hiçbiri bu paketin değil:
- P-30 DEMO_INSAN_SURESI — kurucu kalemi (istemde hariç tutuldu).
- P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > tavan 47 — sayaç yalnız `Pages/`, `Bilesenler/`, `Layout/` `.razor` dosyalarını sayar; bu paket hiçbir razor dosyasına dokunmadı → başka şerit.
- GIRDI-2 TEST_GECERSIZ_FIKSTUR 1 — `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (başka şerit).
CSS sayaçları ✓ (P-18 SABIT_RENK 0, TEMA_HAM_PALET_VURGU 0, P-36b TEMA_KONTRAST_IHLALI 0, DUGME2_SABIT_RENK 0).

## 11. Canlı doğrulama (API + SQL)

Paket yalnız görünüm; API davranışı değişmedi. Canlı kanıt: 5188 API + 5189 vekil + başsız Chrome ile yukarıdaki taramalar. Görünüm uçları: `GET /api/v1/kullanici/gorunum` 200 `{sema: yildiz, semaKaynagi: kiraci}`; 8 şema ölçümü için `PATCH {"sema":"platin"}` 200 → `{sema: platin, semaKaynagi: kullanici}`; geri `PATCH {"sema":null}` 200 → `{sema: yildiz, semaKaynagi: kiraci}` (PATCH "null = sil" sözleşmesi canlıda doğrulandı). `PATCH /api/v1/kullanicilar/ben/arayuz-modu {"arayuzModu":"PROFESYONEL"}` 200 (küçük harf "basit" → 400, doğru reddetme).
Test kiracısı `PAKETYILDIZTIPO-TEST` (01M4H200N191WQ7WYHTGYQ95VT) `zsh kiraci-temizle.sh … --uygula` ile silindi; tenant_id kolonlu tüm tablolarda kalan satır **0**; `tenants` sayısı **21** (kiracı açılmadan önceki sayı; diğer şeritlerin kiracılarına dokunulmadı). 5188/5189 ve bu şeridin Chrome süreçleri durduruldu (port boş).

## 12. DoD sözleşmesi

Yeni varlık/kayıt türü yok, uç yok, migration yok, razor değişikliği yok, para/veri/yasal davranış değişmedi. Sözlük/metin değişmedi. Sözleşme-DoD (PATCH/DELETE/denetim) uygulanmaz.

## 13. Dokunulan dosyalar

- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` — YALNIZ `/* YILDIZ-TIPO */ … /* YILDIZ-TIPO sonu */` bloğu (4C kurallarına dokunulmadı).
- `dotnet/Kuyumcu.Tests/YildizTipoTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/betikler/tur/yildiz-tipo-tara.mjs` (sözdizimi düzeltmesi + önbellek kapatma)
- Bu rapor.
Dokunulan razor sayfası: yok. (git: üçü de izlenmeyen ağaçta `??`.)

## 14. Açık kalanlar

1. **/islemler 375 alt çubuk örtmesi (K9)** — YILDIZ-4C/4B Tezgâh yerleşimi: sabit Fark+Kaydet çubuğu açılışta "+ Başka ödeme"/"+ Para üstü"yü örtüyor (blok yokken de 375×740'ta 5 binme). Sahip: YILDIZ-4C.
2. `sema-fark.mjs` animasyon dondurmuyor → yanlış FARK (bu koşumda 9). Sahip: aracın sahibi (YILDIZ-4C). Önerilen: ölçüm sekmesine `*{animation:none;transition:none;caret-color:transparent}`.
3. Tam süit: RD_KH01/RD_KH02 (rapor dönemi 400 gün sınırı) — sahibi rapor/maliyet şeridi; SATIS4c ve Y4C_K12 kararsız (tekrarda yeşil).
4. Denetim P-11 (48>47, razor) ve GIRDI-2 (MASAKESZAMANSinirTestleri.cs:187) — başka şeritler.
5. "Yeni Cari" modal başlığında "Cari" sözcüğü ve modalın kenarlı "Kimlik ve ticari bilgiler" grubu tuval dilinde değil — metin/yerleşim, tipografi paketi kapsamı dışı (gözlem).
6. Artımlı derleme tuzağı: aynı ağaçta paralel derlemelerde `dotnet build Kuyumcu.Tests` bir kez eski test DLL'ini bıraktı (testler eski beklentiyle koştu); `touch` + yeniden derleme ile çözüldü. Diğer şeritler için not.

## 15. Kurucu / mali müşavir

Mevzuat içeren iş yok.
