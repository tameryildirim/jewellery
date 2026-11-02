# PAKET YILDIZ-AMBLEM-4 — Kabuk amblemi 40 px → 52 px, çizgi 1,2 → 1,0 px (uygulama raporu)

**Tarih:** 10.10.2026 · **Rol:** Ertuğ (frontend) · **Şerit:** t7 (API 5176 · vekil 5177 · CDP 9850/9851 · KUYUMCU_TEST_DB=kuyumcu_test_t7) · **Hasımsız paket** (yalnız görünüm)
**Kurucu (10.10, aynen):** "ama sanki son değişikliklerde amblem benim tasarlattığım gibi olmamış"
**Kök neden (doğrulandı):** `kaynak/Amblem_sol.dc.html` ile `kaynak/amblem-v2-40px/Amblem_sol.dc.html` arasında `diff` yalnız 56. satırı (amb-parla bloğu) gösteriyor: 40→52 px, perspective 640→832 px, yüz 24.9×23.6→32.4×30.6, translateZ / sol pay 12.4→16.2, stroke-width 1.2→1.0. Uygulama eski 40 px değerleri taşıyordu.

## 1. Yapılanlar
| Dosya | Değişiklik |
|---|---|
| `Kuyumcu.Web/Bilesenler/Kabuk/YildizUstMenu.razor` | `KabukTasi` = `("yildiz-amb-ti", "32.4", "30.6", "16.2", "1.0")` (tuval dizgeleri birebir); 3 yorum satırı 52 px. Giriş (`GirisTasi` 59.7/56.5/29.9/2.2) **DEĞİŞMEDİ**. |
| `Kuyumcu.Web/wwwroot/css/tema-yildiz.css` `/* AMBLEM */` bloğu | `.amb-parla .amb-ik` 52×52, `perspective: 832px`; taç/pavyon sol pay −16.2px, taç üst pay −30.6px; 8 yüz `translateZ(16.2px)`. Giriş (`.giris-amb`, 96/1536) dokunulmadı. Dar masaüstü eşikleri +12 px: sıkı düzen `1001–1304 → 1001–1316`, saat gizli `≤1080 → ≤1092` (gerekçe § 4). Yalnız AMBLEM bloğu; YILDIZ-5B vb. bloklara dokunulmadı. |
| `Kuyumcu.Tests/YildizAmblem2Testleri.cs` | YA2_01 adı (tas_52), YA2_02 / YA2_10 `S = 52` ve tuval dizgeleri, YA2_12 eşikler 1316/1092. |
| `Kuyumcu.Tests/YildizAmblem4Testleri.cs` (yeni, 4 test) | Değerleri ELLE yazmaz: tuval `Amblem_sol(.acik).dc.html` amb-parla bloğunu ayrıştırır, CSS AMBLEM bloğu ve razor çıktısıyla karakter karakter karşılaştırır. YA4_01 koyu=açık aynı taş, 52/832, çizgi 1.0 · YA4_02 CSS kutu+perspektif+8 yüz pay/köken/dönüşüm birebir, eski 40 px değerleri kabuk kurallarında yok · YA4_03 razor 8 yüz width/height/viewBox/d/stroke-width birebir · YA4_04 giriş 96 px değişmedi, boşluk 14 px. |

Konum: 72 px şeritte taş dikey ortalı — uygulama `#yildiz-marka` 64, 9.5 · 176×52 = tuval `a.amb` 64, 9.5 · 176×52 (ölçüldü). Menü öğeleri tuvalle aynı x/y (Bugün 304 · Tezgâh 400.8 · Müşteriler 504.5 · Kasa 647.8 · Tüm ekranlar 729.3; y 18.8 — `t/menu-x.mjs`).

Sapma: yok (spesifikasyon hedefi birebir). Para/veri/yasal davranışa dokunulmadı. Migration: gerekmedi. Mevzuat: konu dışı.

## 2. Testler
| Koşum | Toplam | Geçen | Başarısız |
|---|---|---|---|
| Filtre `YildizAmblem` (son kod) | 45 (önce 41 + yeni 4) | 45 | 0 |
| Filtre `YildizAmblem|Yildiz1|Yildiz4A` | 83 | 83 | 0 |
| Tam süit (bir kez, son kod; trx `/private/tmp/kuyumcu-serit/trx-pYILDIZ-AMBLEM-4`) | 12362 | 12337 | 22 (3 atlanan) |

Tam süitteki 22 kırmızının **hiçbiri bu paketin dosyalarına dokunmuyor** (Yildiz/Amblem testi 0): DegisimEkran (DG_N_PRO), FaturaEkran Y4C_K12, CumaSAldimSade ×3, UX4bEkran ×3, SATIS2SinirTestleri ×6, SATIS3SinirTestleriT18, KesifP5Ekran SATIS4a, Toplu1Saf TARIH_TEK (`Pages/EvrakTakvimi.Yildiz.cs:72`, `Pages/KasaRaporu.Yildiz.cs:56` — YILDIZ-5B şeridi), SATIS3Ekran ×3, RaporDort RD_KH01/02 ("Rapor dönemi en çok 400 gün"). Sahipleri: satış/ödeme ekranları (ISLEM-BANT/SATIS şeritleri), YILDIZ-5B, rapor şeridi.

Derleme: `dotnet build Kuyumcu.sln` son temiz derleme **0 uyarı 0 hata** (ilk derlemede görülen 6 uyarı başka şeritlerin test dosyalarındaydı: SUBEKAPSAMASinir, KesifP2, SonKalanlar, Yildiz2).

## 3. Mutasyon kanıtı (yedek `/private/tmp/kuyumcu-serit/mut-yedek/YILDIZ-AMBLEM-4/`, tek eşleşme, geri kopya + cmp)
| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M4-1 | CSS `.amb-ik` 52px → 40px | YA4_02, YA2_10(kabuk) | AYNI |
| M4-2 | razor `KabukTasi` çizgi "1.0" → "1.2" (yeniden derlemeli) | YA4_03, YA2_02(kabuk) | AYNI |
| M4-3 | CSS `perspective: 832px` → 640px | YA4_02, YA2_10(kabuk) | AYNI |
| M4-4 | CSS k1 taç `translateZ(16.2px)` → 12.4px | YA4_02, YA2_10(kabuk) | AYNI |
| M4-5 | CSS sıkı düzen eşiği 1316 → 1304 | YA2_12 | AYNI |
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0/0.

## 4. Gerçek başsız Chrome ölçümü (betikler/tur/cdp.mjs; API 5176 → vekil 5177)
**a) Tuval ↔ uygulama, 10 kare** (`amblem4-kiyas.mjs` = AMBLEM-2 aracı; tek düzeltme: kırpma çerçevesi ekran üstünü aşmasın diye y≥0 — 52 px taş y=9.5'te, 12 px pay negatif y veriyordu ve ilk koşumda iki satırlık ekran dışı şerit sabit 400 piksel "fark" üretti):
| Yüzey/tema | 0 sn | 2 sn | 6 sn | 9,5 sn | 14 sn | azaltma |
|---|---|---|---|---|---|---|
| kabuk koyu (Amblem_sol) | 0 | 0 | 0 | 0 | 0 | 6 px, en büyük 1/255 |
| kabuk açık (Amblem_sol_acik) | 0 | 0 | 0 | 0 | 0 | 410 px, en büyük 1/255 |
| giriş koyu (Giris4) | 0 | 0 | 0 | 0 | 0 | 44 px |
| giriş açık (Giris4a) | 0 | 0 | 0 | 0 | 0 | 823 px |
Kutular: kabuk tuval = uygulama `64,9.5 176×52`; giriş `610,236 220×150` (DEĞİŞMEDİ). Azaltma karesi: çalışan animasyon 0, taş 20° (matrix3d 0.939693…), yazı opak 1. Yan yana görüntü Read ile bakıldı: `/private/tmp/kuyumcu-serit/YILDIZ-AMBLEM-4/kiyas2/kabuk-karanlik-yan-yana.png` (sol tuval | sağ uygulama, 6 satır özdeş).

**b) Sıkışma (sikisma-olc.js) — rota `/` × 1440/1366/1280/1024/375 × BASİT/PRO × koyu/açık = 20 durum, sorunlu 0** (`t/olc-t2_.json`):
| Genişlik | yatay taşma | ad sağ ≤ iç kenar | K1 K3 K3c K9 Y TRY K10 | amblem | 22 AYAR | saat |
|---|---|---|---|---|---|---|
| 1440 | 0 | 1376 ≤ 1376 | 0 | 64,9.5 176×52 | görünür | görünür |
| 1366 | 0 | 1302 ≤ 1302 | 0 | 64,9.5 176×52 | gizli | görünür |
| 1280 | 0 | 1256 ≤ 1256 | 0 | 24,9.5 176×52 | gizli | görünür |
| 1024 | 0 | 1000 ≤ 1000 | 0 | 24,9.5 176×52 | gizli | gizli |
| 375 | 0 | 359 ≤ 359 | 0 | 16,5.5 176×52 | — | — |
(Her satır BASİT+PRO × koyu+açık için aynı.) **Rotalar** `/`, `/islemler`, `/cariler`, `/kasa`, `/ekranlar` × 1440/1366/1280/1024/375 × koyu/açık × BASİT/PRO = 100 durum: yatay taşma 0, ad iç kenarın içinde 100/100. Görüntüler (Read): `t/ust-375-profesyonel-aydinlik.png` (taş + ByGatech + MENÜ + DK, sığıyor), `t/ust-1366-basit-karanlik.png`.

**c) Eşiklerin yeniden ölçümü** (`t/bosluk.mjs`, adın sığacağı en geniş alan, 52 px ↔ aynı sayfada 40 px'e geri zorlanmış hâl):
| Genişlik | 1440 | 1317 | 1316 | 1305 | 1093 | 1092 | 1081 | 1013 | 1001 |
|---|---|---|---|---|---|---|---|---|---|
| 40 px (eski) | 86 | 99 | — | 87 | 111 | — | 99 | 98 | 86 |
| 52 px, eski eşik | 74 | 87 | 86 | 75 | 99 | 98 | 87 | 86 | 74 |
| 52 px, yeni eşik (1316/1092) | 74 | 87 | 322 | 311 | 99 | 165 | 154 | 86 | 74 |
→ eşikler +12 px kaydırılınca bant içindeki en dar alan eskisine döndü (1317: 87, 1093: 99). Taşma hiçbir genişlikte yok (60/60 uzun ad durumu `t/uzun.log`: scrollWidth = innerWidth, ad iç kenarın içinde).

**d) 8 şema FARK 0** — kiracı şeması `PATCH /gorunum {"sema":"platin"}` (200) → `sema-fark.mjs` rota `/`,`/islemler`,`/cariler`,`/ekranlar`: **64 karşılaştırma · FARK 0** → şema geri `yildiz` (200).
**e) Yıldız tipo taraması** `yildiz-tipo-tara.mjs` BASİT ve PRO × 1440+375 × koyu+açık: düğüm 182 · **KURAL DIŞI 0** · yatay kayan 0 · kesik yazı 0.
**f) tuval-kiyas (Bugün/Tezgâh/Müşteriler/Tüm):** eşleme JSON'ları ağaçta yok (AMBLEM-2 raporundaki not aynen geçerli). Ayrıca Bugun4/Tezgah4/Musteri4/Tum4 tuvalleri amblem v2'yi hiç içermiyor (`amb-parla` 0 — eski 40×40 svg). Eşdeğer kanıt: Amblem_sol tuvalindeki menü öğeleri ile uygulamanınki x/y'de birebir (§ 1).

## 5. Canlı doğrulama (5176) + SQL
| Adım | Beklenen | Gerçek |
|---|---|---|
| `demo-tohumla.js --prova-kiraci --sema yildiz` | kiracı + veri | `01M4KHTCVZZGP77Y57M981JT13` "PAKETYILDIZAMBLEM4-TEST Tunalılar Kuyumculuk" · cari 6 · ürün 12 · satış 9 |
| `POST /api/v1/auth/giris` (fikstür chmod 600, parola basılmadı) | 200 + jeton | 200, jeton var |
| `GET /api/v1/gorunum` | yildiz | `{"sema":"yildiz","yaziOlcegi":"18","temaModu":"sistem"}` |
| `PATCH /gorunum` platin / yildiz | 200 ×2 | 200 `Görünüm kaydedildi` ×2 |
| `GET /css/tema-yildiz.css` (vekil) | kaynakla aynı | `cmp` AYNI; `width: 52px; height: 52px; perspective: 832px` 1 kez |
| SQL `kiraci_ayarlari` | gorunum_semasi yildiz | `01M4KHTCVZZGP77Y57M981JT13 · gorunum_semasi · yildiz` · `gorunum_yazi_olcegi · 18` |
**Temizlik:** API (PID 5798 + çocuk 5801) ve vekil (PID 7094) yalnız kendi PID'leriyle durduruldu; `lsof` 5176/5177 boş; 5199 dinliyor (dokunulmadı). `zsh kiraci-temizle.sh 01M4KHTCVZZGP77Y57M981JT13 --uygula` rc 0 → tenants'ta bu kiracı 0, kiraci_ayarlari 0. **Tenants: başta 24, sonda 24.** Fikstür, jeton, giriş yanıtı ve geçici sistem anahtarı `printf ''` ile boşaltıldı.

## 6. denetim.sh
`DENETİM BAŞARISIZ` — 314 ✓, 3 ✗, **hiçbiri bu paketin değil**: P-30 DEMO_INSAN_SURESI (kabulde hariç tutulan), TOPLU-1 TARIH_TEK 2 (`EvrakTakvimi.Yildiz.cs:72`, `KasaRaporu.Yildiz.cs:56` — YILDIZ-5B), GIRDI-2 TEST_GECERSIZ_FIKSTUR 1 (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`). Amblem/Yıldız kabuk sayaçları ✓.

## 7. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`)
`dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` (??, paylaşılan dosya — yalnız AMBLEM bloğu), `dotnet/Kuyumcu.Web/Bilesenler/Kabuk/YildizUstMenu.razor`, `dotnet/Kuyumcu.Tests/YildizAmblem2Testleri.cs`, `dotnet/Kuyumcu.Tests/YildizAmblem4Testleri.cs` (yeni) — son üçü izlenmeyen dizinlerde. Bu rapor. COMMIT YOK.
Dokunulan sayfalar: yok (yalnız kabuk bileşeni `YildizUstMenu.razor`, ikinci dokunuş gerekçesi: kurucu düzeltmesi, YILDIZ-AMBLEM-2/3'ün devamı).

## 8. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| 10 kare tuval↔uygulama fark ~0 + yan yana | KARŞILANDI | § 4a: 10/10 kare FARK 0, kiyas2/*-yan-yana.png |
| Taşma 0 (1440/1366/1280/1024/375) | KARŞILANDI | § 4b/4c: 20 + 100 + 60 durum |
| 8 şema FARK 0 | KARŞILANDI | § 4d: 64 karşılaştırma FARK 0 |
| Giriş kareleri değişmedi | KARŞILANDI | § 4a giriş 10/10 FARK 0; YA4_04 |
| Mutasyon ≥3 (52→40, 1.0→1.2, perspective) cmp AYNI | KARŞILANDI | § 3: 5 mutasyon |
| build 0/0 | KARŞILANDI | son temiz derleme |
| denetim TEMİZ (P-30 hariç) | KISMEN — başka şeritlerin 2 ✗'si | § 6; bu paketten ✗ 0 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 22 kırmızının hiçbiri Yildiz/Amblem dosyası değil |
| Test kiracısı silindi, tenants başa döndü | KARŞILANDI | 24 → 24 |
| Yıldız tipo kural dışı 0 | KARŞILANDI | § 4e |

## 9. Açık kalanlar
1. **1440 ve 1001–1012 px'te ad alanı 74–86 px** (önce 86–98): 52 px taş +12 px yer aldı. 1440 tuvalin kendisi (değiştirilemez, YA2_12 de 1440'a kural sızmasını yasaklar), ≤1000 kuralı 4A'nın. "HATİCE K." (75 px) bu iki noktada 1 px "…" ile kesiliyor (taşma yok). Tuvaldeki ad "HAKAN T." (73 px) tam görünüyor. Kurucu/tasarım kararı gerekir: 1440'ta 22 AYAR'ın gizlenmesi ya da adın baş harflere inmesi tuvali değiştirir.
2. denetim.sh TARIH_TEK (YILDIZ-5B) ve TEST_GECERSIZ_FIKSTUR (MASAK-EŞZAMAN şeridi) — sahiplerine.
3. tuval-kiyas eşleme JSON'ları kalıcı yere alınmalı; Bugun4/Tezgah4/Musteri4/Tum4 tuvalleri eski amblemi taşıyor (orkestratör/tasarım oturumu).
4. Disk notu: 8 şema ölçümü ikinci bir profil dizininde koştu (`profil-sema`); dosya silme yasak olduğundan bırakıldı.
5. Paylaşılan `tema-yildiz.css`'te mutasyonlar yedekten geri kopyalandı (cmp AYNI); her mutasyon penceresi ~20 sn — bu sürede başka şerit aynı dosyaya yazdıysa geri kopya onu ezmiş olabilir; son `served.css` = kaynak, YILDIZ-5B bloğuna bu pakette dokunulmadı.

## 10. Kurucuya / mali müşavire
Mali müşavir sorusu yok. Kurucuya: § 9 madde 1 (1440'ta uzun adların 1 px kesilmesi) — tuval kararı.
