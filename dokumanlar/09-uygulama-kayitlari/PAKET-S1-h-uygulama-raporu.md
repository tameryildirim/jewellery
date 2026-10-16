# PAKET S1-h — GÖRSEL SIĞMA 1 · uygulama raporu (24.09.2026)

Rol: Taşkın (backend) + Ertuğ (frontend) · Opus · Dalga DEMO DÜZELTME (S1-h → S1-i).
Kaynak: kurucu 24.09 08:30, kendi Chrome'unda /ayarlar: *"olmuş mu, yer yok gibi dibine girmişsin, dar dar"* ·
*"o sayfa tren gibi aşağıya doğru gidiyor, birbiriyle alakasız kısımları tab tab ayır"* ·
*"sadece bu ekrana takılma, diğer ekranlara da bak"*. Tarama: `ham/GORSEL-TARAMA-2026-09-24-0845.md`.

İzolasyon: testler `KUYUMCU_TEST_DB=kuyumcu_test_j`; canlı API **5176**, vekil **5175**, kendi sekmem **tab-21**.
5179/5181/5199/5200 ve başka sekmelere dokunulmadı. COMMIT YOK.

---

## 1. Yapılanlar

| # | Kapsam | Ne yapıldı | Dosya |
|---|---|---|---|
| 1 | Türkçe dil verisi | `<BlazorWebAssemblyLoadAllGlobalizationData>true</…>` eklendi (gerekçe yorumu ile). | `Kuyumcu.Web/Kuyumcu.Web.csproj` |
| 2 | Ortak sekme bileşeni | `Bilesenler/Sekmeler.razor` (YENİ): `role=tablist/tab/tabpanel`, `aria-selected`, `aria-controls`, `aria-labelledby`, dolaşan `tabindex` (0/-1), klavye ← → Home End (sarmalı), seçili sekme URL'de `?sekme=` (yeni tarih kaydı → geri tuşu çalışır, derin bağlantı çalışır, bilinmeyen kod → ilk sekme), `LocationChanged` izlenir, odak halkası + belirteç renkleri. TÜM paneller çizilir, seçili olmayan `hidden` taşır → taşınan bölümlerin id'leri/davranışları aynen yaşar. Tanım `SekmeTanimi` ayrı `.cs`'te (razor record tavanı). | `Bilesenler/Sekmeler.razor`, `Bilesenler/SekmeTanimi.cs` |
| 3 | /ayarlar sekmeli ve geniş | Bölümler **Görünüm** (Görünüm kartı + katlanmış ayrıntılar: tema, benim görünümüm, dükkân görünümü, arayüz modu) · **Hesap** (profil + dil, 2 sütun) · **Güvenlik** (şifre + iki aşamalı giriş, 2 sütun) · **Sistem** (künye) · **Destek** (yalnız SAHİP; içerik listeleri ve form/geçmiş 2 sütun). 32rem'lik `.ayarlar-sutun` kaldırıldı → `.ayarlar-sayfa` ana alanın tamamı. Emoji kalıntıları (✏️ 🏢 🟢 ⚙️ 🇹🇷 🇬🇧) → `Ikon` (Kalem, Isletme, BasitMod, Ayarlar) / dil kodu rozeti (TR/EN). `#ay-rol` ham "SAHIP" yerine sözlükten "Sahip" (UX-S31; tanınmayan kod tahmin edilmez, olduğu gibi kalır). | `Pages/Ayarlar.razor`, `Core/I18n/Sozlukler.Ayarlar.cs` (YENİ, TR+EN) |
| 4 | Karo adları | `/ekranlar` ve `/islemler` ızgarası sabit 8 sütun yerine `repeat(auto-fill, minmax(12rem/10rem, 1fr))`; `.ekran-karo-ad { word-break: normal }` (S1-d'nin `break-word`'ünü ezer); ≤600 px'te /ekranlar tek, /islemler iki sütun. `FisIslemIzgarasi` ızgarasına `islem-izgara-karolar` sınıfı. `Ekranlar.razor` DEĞİŞMEDİ (yalnız CSS). | `wwwroot/css/app.css`, `Bilesenler/Fis/FisIslemIzgarasi.razor` |
| 5 | Altın kartı binmesi | `#pozisyon-disarida` satırına `pozisyon-disarida` sınıfı: `display:flex; flex-wrap:wrap` → satır içi span'ler iki satıra bölünüp komşusunun kutusuna binmez. Telefonda kırılım öğeleri 44 px dokunma kutusunu dolgu ile kurar. | `Bilesenler/AltinPozisyonuKarti.razor`, `app.css` |
| 6 | Ölçüm sahte-kırığı | Ana alandaki `.btn`'ler `min-height + padding-block:0 + line-height:normal` ile kuruluydu; tek satır yazı kutuda 1,7 satırdan yüksek görünüyor ve `sikisma-olc.js` "kırık" sayıyordu (Range ile satır sayısı 1). `.ana-icerik .btn` yüksekliği aynı kalacak şekilde dolgu ile kuruldu (`line-height:1.3`, `padding = (--dugme-yuksekligi − 1,3em)/2`); `.gk-alt-dugme` 50 px'i dolgu ile. | `app.css` |

`app.css` değişikliği YALNIZ dosya sonuna eklenen S1-h bloğudur (yedekle `diff`: 107 eklenen satır, 0 silinen/değişen satır).
`Bilesenler/GorunumKarti.razor` ve `Pages/Ekranlar.razor` dosya alanındaydı ama değişiklik gerekmedi (CSS yeterli).

### Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
`Pages/Ayarlar.razor` · `Bilesenler/Sekmeler.razor` (yeni) · `Bilesenler/Fis/FisIslemIzgarasi.razor` · `Bilesenler/AltinPozisyonuKarti.razor`.
`SatisAnaliz/Uyum/Gider`'deki elle yazılmış tablist'lere DOKUNULMADI (S1-i taşıyacak).

## 2. Sapmalar ve gerekçeler

1. **Sekme adları tuvalden farklı** — tuval (Ayarlar.dc.html) `Görünüm · Dükkân · Kur · Yazıcı · Güvenlik` gösteriyor; bu sayfada Dükkân/Kur/Yazıcı bölümü YOK (onlar /isletme, /kur ekranlarında). Görev metni "isimleri Ayarlar.razor'daki gerçek bölümlerden çıkar" dediği için: Görünüm · Hesap · Güvenlik · Sistem · Destek. Görsel biçim tuvaldeki hap şerit.
2. **Beşinci sekme "Destek"** — Sistem sekmesi (künye + destek paketi) 1280'de 2,1 ekran boyuydu (> 1,6 koşulu). Destek paketi ayrı sekmeye alındı; yalnız SAHİP görür (kart zaten yalnız SAHİP'e çiziliyordu — boş sekme açılmaz).
3. **Profil düzenle birincil düğmesi sayfa başında kaldı** — "ekran başına tek birincil eylem" (K7) ve mevcut `#ay-profil-duzenle` testleri; Hesap sekmesine taşınmadı.
4. **Tüm paneller DOM'da (`hidden`)** — sekmeye taşınan özellik silinmesin, id'ler ve mevcut bUnit testleri (Ayarlar/Görünüm/MFA/Destek/Sürüm) aynen geçsin diye. Genel `[hidden]{display:none !important}` (S1-g) gizler; ölçümde `[hidden]` olup görünen = 0.
5. **Mutasyon M3 "karo adına break-all ekle → tarayıcı kırık>0" beklentisi GERÇEKLEŞMEDİ** ve bu dürüstçe raporlanıyor: yeni ızgarada karo yeterince geniş olduğu için `break-all` tek başına hiçbir adı bölmüyor (1280 px, /ekranlar ve /islemler: kırık 0). Asıl koruma genişliktir; bu yüzden kök nedeni geri getiren mutasyon da koşuldu: `auto-fill` satırı silinip `break-all` eklenince tarayıcıda **kırık 20, tek kelime kırık 2** ("Emanet" harf ortasından). CSS kilit testi her iki mutasyonda da KIRMIZI.
6. **İngilizce tarayıcı dili emüle edilemedi** (tarayıcı bölmesinde dil emülasyonu yok; `.NET`'e JS'ten çağrılabilir bir `JSInvokable` yok). Kanıt yerine: sunulan `/_framework/blazor.boot.json` → `globalizationMode: "all"`, ICU kaynakları yalnız `icudt.dat` (tam veri); özellik silinince aynı derleme `sharded` + `icudt_CJK.dat, icudt_EFIGS.dat, icudt_no_CJK.dat` üretiyor (EFIGS = en/fr/es/it/de, Türkçe YOK). Yani tarayıcı dili ne olursa olsun yüklenen dosya tam ICU'dur. Ekranda kur şeridi `₺6.742,49` (tr-TR), `TRY` kalıbı 0.
7. **Görünüm sekmesi 375 px'te 2,7–2,9 ekran** — 8 tema + 3 blok telefonda sığmaz; "boy ≤ 1,6" koşulu taramadaki gibi 1280 px ölçüsüdür ve 1280'de tüm sekmelerde sağlandı (bkz. tablo).
8. **`.ana-icerik .btn` dolgu kuralı tüm ana alan düğmelerini etkiler** — görünüm değişmez (yükseklik aynı, yazı ortalı); /islemler'deki iki sahte kırığı ("Diğer ▾", "Ekle", "Ödemeyi böl", "Satış Kaydet") sıfırladı. Denetimin K4 "düğme ölçeği iki değer" kilidi yeşil (P39 K4).

## 3. Birincil kaynak (mevzuat dışı, teknik doğrulama)
Microsoft Learn — *ASP.NET Core Blazor globalization and localization* (aspnetcore-8.0 dahil):
https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-8.0 —
"By default, Blazor loads a subset of globalization data that contains the app's culture." ve dosya listesi:
`icudt_EFIGS.dat`: *en-\*, fr-FR, es-ES, it-IT, de-DE* (Türkçe yok). Mevzuat hükmü içermez.

## 4. Migration
Gerekmedi (şema değişikliği yok).

## 5. Test sayıları

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Yeni `S1hGorselSigmaTestleri` (filtreli) | 14 | 14 | 0 |
| İlgili kümeler (S1h + S1f + B1/Görünüm + UX1C4 + P31 + Sürüm + Tema + S1d + AltınPozisyonu + S22 + S1g + TasarımBelirteç + P39 + MobilParite + P37*) | 313 | 313 | 0 |
| Tam süit 1. koşum (trx `trx-pS1-h`) | 5054 | 5053 | **1** — `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir` (BENİM ürünüm: S1-c kilidi kendi bloğundan dosya SONUNA kadar "overflow" kelimesini yasaklıyor; S1-h bloğunda `overflow-wrap` vardı) |
| Düzeltme sonrası (S1c + S1h + S1f + S1d + P39 + TasarımBelirteç + S1g + AltınPozisyonu) | 174 | 174 | 0 |
| Tam süit 2. koşum (trx `trx-pS1-h-2`) | 5054 | 5054 | 0 |
| 2. tur ilgili kümeler (S1h+S1c+S1d+S1f+S1g+P39+TasarımBelirteç+Ekranlar), 14rem düzeltmesinden sonra | 192 | 192 | 0 |
| **Tam süit 3. koşum (trx `trx-pS1-h-3`, 2. tur son durum)** | **5062** | **5062** | **0** |

Önce/sonra: paket öncesi tam süit 5040 test (5054 − 14 yeni); yeni test sayısı **14**.

### 5a. Tam süit 2. koşum
`dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj -v n --logger trx` (KUYUMCU_TEST_DB=kuyumcu_test_j) → trx `<Counters total="5054" executed="5054" passed="5054" failed="0">`. Bu paketin ürettiği yeni kırmızı: **0** (1. koşumdaki tek kırmızı benimdi ve düzeltildi). Başkasına ait kırmızı: yok.

## 6. Mutasyon kanıtları (kaynak yedeği `scratchpad/mut-yedek/S1-h/`, her biri `cmp` ile BİREBİR geri alındı)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M1-1 | csproj'dan `BlazorWebAssemblyLoadAllGlobalizationData` satırı silindi | `S1h_01_csproj_TUM_dil_verisini_yukler_Ingilizce_tarayicida_TRY_cikmaz` | cmp BİREBİR |
| M1-2 | aynı silme + `dotnet build Kuyumcu.Web` | boot.json: `sharded` [icudt_CJK, icudt_EFIGS, icudt_no_CJK] → geri alınınca `all` [icudt.dat] | cmp BİREBİR |
| M2-1 | Sekmeler'de `aria-selected` kaldırıldı | S1h_02, S1h_03, S1h_04, S1h_05, S1h_09 | cmp BİREBİR |
| M3-1 | `.ekran-karo-ad { word-break: break-all }` | `S1h_11_CSS_karo_ve_kisa_etiket_HARF_ORTASINDAN_bolunmez` | cmp BİREBİR |
| M3-5 | `.ekran-karo-ad { … overflow-wrap: anywhere }` | S1h_11 | cmp BİREBİR |
| M3-2 | (tarayıcı) yalnız `break-all` | ölçüm /ekranlar kırık **0**, /islemler kırık **0** — genişlik korur (§ 2 md.5) | cmp BİREBİR |
| M3-3 | (tarayıcı) `auto-fill` silindi + `break-all` | /ekranlar 8 sütun, **kırık 20**, tek kelime kırık 2 ("Emanet") | cmp BİREBİR |
| M3-4 | `auto-fill` satırı silindi | S1h_11 | cmp BİREBİR |
| M4-1 | `.pozisyon-disarida`'dan `display:flex` silindi | `S1h_12_CSS_blogu_belirtecle_Ayarlar_genis_sekme_hap_pozisyon_flex`; tarayıcıda (satır içi stil yalnız ölçüm anında) `display:block` → **binen 1** (`#pozisyon-emanet ⟂ "— sizin değil — pozisyona girmez"`), geri → **0** | cmp BİREBİR |
| M5-1 | `NavigateTo(..., replace: true)` (geri tuşu bozulur) | `S1h_03_Sekmeler_tiklama_URLye_sekme_yazar_ve_TARIH_KAYDI_acar` (ilk deneme başka bir sürecin `obj/Kuyumcu.Web.dll` kilidi yüzünden DERLENMEDİ — MSB4018 IOException; tekrar koşuldu, kırmızı) | cmp BİREBİR |
| M6-1 | `#ay-rol` ham kod | `S1h_10_Ayarlar_emoji_yok_ham_rol_kodu_yok_dar_sutun_yok` | cmp BİREBİR |
| M7-1 | Destek sekmesi herkese | `S1h_07_Ayarlar_SAHIP_olmayan_Destek_sekmesini_gormez` | cmp BİREBİR |
| M8-1 | panellerden `hidden` kaldırıldı | S1h_02, S1h_03, S1h_04, S1h_09 | cmp BİREBİR |
| M3-6 | (2. tur) `.ekranlar-izgara` `minmax(14rem)` → `minmax(12rem)` geri | `S1h_11_CSS_karo_ve_kisa_etiket_HARF_ORTASINDAN_bolunmez` (1/14 kırmızı); tarayıcıda 12rem + 18 px = "Sipariş ve tamir" kırık 1 (görüntü turu ilk koşumu) | cmp BİREBİR (`scratchpad/mut-yedek/S1-h-g/`) |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata**.

## 7. Denetim
`bash dotnet/denetim.sh` → **DENETİM TEMİZ** (çıkış 0). *(1. tur sonu.)* **2. tur (09:55): DENETİM BAŞARISIZ, çıkış 1** — iki kırık: `Sınırsız ToListAsync (Modules · B-8) 146 (tavan 145)` ve `P-36: HAM_INPUT 47 > tavan 42`; ikisi de S1-h dosyalarında değil (bu paketin dosyalarında `<input` 0, Modules değişikliği yok), eşzamanlı Fatura/Banka şeridinin ürünü — § 13 md.0. S1-h sayaçları (record tavanı, P-10, P-02, P39 K4, satır içi stil, sabit renk) ✓.
Ara durum (dürüst kayıt): ilk koşumda `SekmeTanimi` razor içinde record olduğu için
`Pages+Bilesenler+Layout record tanımı 4 (tavan 3)`, `P-10 BU_PAKETTE_EKLENEN_RECORD +1` ve
`P-02 OLCUM_TABANI_KOPYA` KIRIKTI → record `Bilesenler/SekmeTanimi.cs`'e taşındı (SecenekOgesi.cs deseni) → TEMİZ.
P39 K4 "düğme ölçeği iki değer" kilidi ilk sürümde `min-height: var(--dokunma-hedefi)` yüzünden kırıldı → `var(--dugme-yuksekligi, 2.25rem)` → yeşil.

## 8. Canlı doğrulama (gerçek tarayıcı — API 5176 · vekil 5175 · sekme tab-21)

Kiracı **PAKETS1-h-TEST** (`01M38ZKQT5G50RNY3PJD8N4X90`): `POST /api/v1/sistem/davetler` **400** (ilk deneme: "Gerekçe zorunludur" — gövdeyi okudum) → gerekçeyle **201** → `POST /api/v1/auth/kayit` **201** (`tenantId` döndü). Parola `chmod 600` dosyada, çıktıya yazılmadı; oturum `inceleme-vekili.py` ile parola yazmadan açıldı.
Altın kartı çakışmasını üretmek için gerçek veri: `POST /cari` **201** (PAKETS1-h Emanet Müşterisi) → `GET /kur/guncel` **200** → `POST /emanet/giris` **200** (22 ayar 18,45 g → 16,9 g has) — Panel ve /raporlar'da `#pozisyon-emanet "Müşteri emaneti 16,9 g"` göründü.

Ölçüm aracı: `betikler/sikisma-olc.js`'in birebir mantığı + `Range.getClientRects` satır sayımı (cok = ≤24 karakterlik yaprak metnin >1 satır olması; tek = tek kelimelik olanlar).
Sütunlar: kart% · kırık · taşan · binen · boy · yatay kaydırma · [hidden]-görünen · TRY kalıbı · çok satır (Range) · tek kelime kırık.

### 8a. 1280×800
| Rota | Mod | Tema | kart% | kırık | taşan | binen | boy | kayd. | gizli | TRY | cok | tek |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /ayarlar?sekme=gorunum | PRO | açık | 95 | 0 | 0 | 0 | 1,6 | 0 | 0 | 0 | 0 | 0 |
| /ayarlar?sekme=hesap | PRO | açık | 95 | 0 | 0 | 0 | 1,0 | 0 | 0 | 0 | 0 | 0 |
| /ayarlar?sekme=guvenlik | PRO | açık | 95 | 0 | 0 | 0 | 1,0 | 0 | 0 | 0 | 0 | 0 |
| /ayarlar?sekme=sistem | PRO | açık | 95 | 0 | 0 | 0 | 1,1 | 0 | 0 | 0 | 0 | 0 |
| /ayarlar?sekme=destek | PRO | açık | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar | PRO | açık | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 |
| /islemler | PRO | açık | 77 | 0 | 0 | 0 | 1,8 | 0 | 0 | 0 | 0 | 0 |
| / (Panel) | PRO | açık | 95 | 5* | 1* | **0** | 1,5 | 0 | 0 | 0 | 8* | 0 |
| /raporlar | PRO | açık | 95 | 16* | 0 | **0** | 4,5 | 0 | 0 | 0 | 0 | 0 |
| /ayarlar (5 sekme) | BASİT | açık | 95 | 0 | 0 | 0 | 1,6 / 1,0 / 1,0 / 1,0 / 1,4 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar | BASİT | açık | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 |
| /islemler | BASİT | açık | 77 | 0 | 0 | 0 | 2,2 | 0 | 0 | 0 | 0 | 0 |
| / (Panel) | BASİT | açık | 95 | 0 | 0 | **0** | 1,3 | 0 | 0 | 0 | 0 | 0 |
| /raporlar | BASİT | açık | 95 | 0 | 0 | **0** | 1,5 | 0 | 0 | 0 | 0 | 0 |
| /ayarlar (5 sekme) | PRO | koyu | 95 | 0 | 0 | 0 | 1,6 / 1,0 / 1,0 / 1,1 / 1,4 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar · /islemler | PRO | koyu | 95 · 77 | 0 · 0 | 0 | 0 | 1,4 · 1,8 | 0 | 0 | 0 | 0 | 0 |
| / · /raporlar | PRO | koyu | 95 | 5* · 16* | 1* · 0 | **0 · 0** | 1,5 · 4,5 | 0 | 0 | 0 | 8* · 0 | 0 |
| /ayarlar (5 sekme) | BASİT | koyu | 95 | 0 | 0 | 0 | 1,5† / 1,0 / 1,0 / 1,1 / 1,5 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar · /islemler · / · /raporlar | BASİT | koyu | 95 · 77 · 95 · 95 | 0 | 0 | **0** | 1,4 · 2,2 · 1,3 · 1,3 | 0 | 0 | 0 | 0 | 0 |

### 8b. 375×812
| Rota | Mod | Tema | kart% | kırık | taşan | binen | boy | kayd. | gizli | TRY | cok | tek |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /ayarlar gorunum/hesap/guvenlik/sistem/destek | PRO | açık | 94 | 0 | 0 | 0 | 2,9 / 1,2 / 1,5 / 1,6 / 2,6 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar · /islemler | PRO | açık | 91 · 94 | 0 · 0 | 0 | 0 | 4,2 · 2,3 | 0 | 0 | 0 | 0 | 0 |
| / · /raporlar | PRO | açık | 94 | 5* · 18* | 0 | **0 · 0** | 2,7 · 7,2 | 0 | 0 | 0 | 3* · 1* | 0 |
| /ayarlar (5 sekme) | BASİT | açık | 94 | 0 | 0 | 0 | 2,8 / 1,2 / 1,4 / 1,4 / 2,5 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar · /islemler · / · /raporlar | BASİT | açık | 91 · 94 · 94 · 94 | 0 · 0 · 1* · 0 | 0 | **0** | 4,2 · 2,9 · 2,0 · 2,3 | 0 | 0 | 0 | 0 | 0 |
| /ayarlar (5 sekme) | PRO | koyu | 94 | 0 | 0 | 0 | 2,9 / 1,2 / 1,5 / 1,6 / 2,6 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar · /islemler · / · /raporlar | PRO | koyu | 91 · 94 · 94 · 94 | 0 · 0 · 5* · 18* | 0 | **0** | 4,2 · 2,3 · 2,7 · 7,2 | 0 | 0 | 0 | 0·0·3*·1* | 0 |
| /ayarlar (5 sekme) | BASİT | koyu | 94 | 0 | 0 | 0 | 2,8 / 1,2 / 1,4 / 1,4 / 2,5 | 0 | 0 | 0 | 0 | 0 |
| /ekranlar · /islemler · / · /raporlar | BASİT | koyu | 91 · 94 · 94 · 94 | 0 · 0 · 1* · 0 | 0 | **0** | 4,2 · 2,9 · 2,0 · 2,3 | 0 | 0 | 0 | 0 | 0 |

\* Panel (`/`) ve /raporlar'ın kırık/taşan değerleri bu paketin dosya alanı DIŞINDAKİ öğelerdendir ve S1-i'ye devredilir:
`#panel-duzen-modu "Kutucukları düzenle"`, `#panel-satis-yap "Satış Yap"`, `#panel-alis-yap`, `#panel-tahsilat`, `#panel-hurda` (Panel.razor düğmeleri — Range ile 1 satır, sabit yükseklik kaynaklı sahte kırık), `p.stat-deger "16,9002 has"` (taşan), `#panel-borclu-eylem-… "Öde"` (375 BASİT), `#rp-kar-zarar "Kâr-Zarar"`, `#rm-r01 "Günlük Özet"` vb. (Raporlar.razor PRO). Bu paketin Panel/raporlar koşulu **binen 0**'dır ve her kombinasyonda sağlandı; önceki `#pozisyon-emanet ⟂ "— sizin değil — pozisyona girmez"` binmesi tarayıcıda yeniden üretilip (M4-1) giderildiği kanıtlandı.
† BASİT koyu /ayarlar Görünüm ilk ölçümde 1,7 çıktı; sebep mod değişikliğinden sonra 4 sn görünen başarı bandı (`#gk-basari`). Band söndükten sonra tekrar ölçüldü: **1,5** (kalıcı durum).
Ek: Okyanus ve Fildişi (açık menü) SEÇİLİ iken (rozet "Seçili" görünürken) 1280 ve 375'te binen 0, kırık 0, tek kelime kırık 0; ölçüm sonrası Platin + Aydınlık'a geri dönüldü.

### 8c. Sekme davranışı (tarayıcı, gerçek DOM olayları)
| Adım | Beklenen | Gerçek |
|---|---|---|
| `/ayarlar?sekme=guvenlik` derin bağlantı | Güvenlik seçili, paneli açık, Görünüm gizli | `aria-selected` guvenlik · `ayar-panel-guvenlik` hidden=false · `ayar-panel-gorunum` hidden=true |
| → (ArrowRight) | Sistem, odak Sistem'de, URL `?sekme=sistem` | `?sekme=sistem · ayar-sekme-sistem · odak ayar-sekme-sistem · tabindex -1-1-10-1` |
| End | Destek | `?sekme=destek · odak ayar-sekme-destek` |
| → (sondan) | başa sarar | `?sekme=gorunum · odak ayar-sekme-gorunum` |
| ← | sona sarar | `?sekme=destek` |
| Tarayıcı GERİ (history.back) ×2 | önceki sekmeler sırayla | `?sekme=gorunum` → `?sekme=destek`, panel ve aria-selected URL'yi izledi |

### 8d. Türkçe dil verisi (tarayıcı)
`/_framework/blazor.boot.json` (5176'nın sunduğu): `globalizationMode = "all"`, `resources.icu = ["icudt.dat"]`.
`navigator.languages = ["tr","tr-TR"]` (bölmede dil emülasyonu yok — § 2 md.6). Kur şeridi: `₺6.738,55 · ₺6.172,51 · ₺48,96`;
sayfa metninde `TRY\d` kalıbı **0** (tüm rota × genişlik × mod × tema ölçümlerinde).

### 8e. Ekran görüntüleri (2. tur, 24.09 09:40–09:53 — doğrulayıcı bulgusu [KRITIK] kapatıldı)
Tarayıcı bölmesi bu turda da gizliydi (`tabs_context`: "The Browser pane is currently hidden"); bölmeyi açmak kurucu/orkestratör işi
olduğundan açılmadı. Görüntüler **gerçek Chromium ile** (Playwright önbelleğindeki *Google Chrome for Testing* 1217, `--headless=new`,
Chrome DevTools Protokolü üzerinden `node` betiği `scratchpad/tur.mjs`) alındı ve **dosya olarak** kaydedildi:
`dokumanlar/09-uygulama-kayitlari/ham/S1-h-goruntuler/` — **72 PNG** (9 rota × 1280/375 × BASİT/PRO × açık/koyu, tam sayfa), 9,9 MB.
Adlandırma: `<mod>-<tema>-<genişlik>-<rota>.png` (ör. `basit-karanlik-1280-ayarlar-gorunum.png`, `pro-aydinlik-375-panel.png`).

Kurulum: kendi API'm **5176** (`Sistem__Anahtari` ortam değişkeni, rastgele, chmod 600 dosyada, yazdırılmadı) → davet **201** →
kayıt **201** (kiracı `PAKETS1-h-TEST Goruntu`, `01M392DC5QWQ7V527PBE4PBXGC`) → cari **201** → `/kur/guncel` **200** →
`/emanet/giris` **200** (K22 18,45 g → 16,9002 g has) · vekil **5175** (`inceleme-vekili.py`, parola yazılmadan `/__giris`).
Mod/tema geçişi ekrandan (`#gk-mod-*`, `#gk-tema-*` tıklaması; `secili` sınıfı ve `data-tema` ile doğrulandı), ardından 5,5 sn
durulma (başarı bandı söner) + her rotada 2 sn bekleme ve **iki ölçüm** (ikincisi kayda geçer) — doğrulayıcının "[DUSUK] ara render" notu
bu bekleme ile giderildi.

**İngilizce tarayıcı (bulgu § 2 md.6 kapandı):** tarayıcı `--lang=en-US` + `Accept-Language: en-US` ile açıldı; ölçülen
`navigator.language = "en-US"`, `navigator.languages = ["en-US"]`. İlk (önbelleksiz) yüklemede yüklenen ICU kaynağı
`performance.getEntriesByType('resource')` → **`icudt.dat`** (tam veri; sonraki yüklemeler Blazor önbelleğinden geldiği için kaynak
listesinde görünmez); sunulan `blazor.boot.json` → `globalizationMode: "all"`, `icu: ["icudt.dat"]`. Kur şeridi İngilizce tarayıcıda:
`HAS ₺6.727,48 · GRAM 22 ₺6.162,37 · USD ₺48,96 · EUR ₺55,76` — **72 ölçümün 72'sinde `TRY` kalıbı 0**.

**Bu turda bulunan ve düzeltilen kusur (Anayasa md.1):** yeni kiracının varsayılan yazı boyutu **Büyük 18 px** (karo adı 17 px).
Bu durumda `/ekranlar` 1280'de 12rem'lik ızgara 194 px karo → ikon + yıldız payından sonra 112 px yazı alanı bırakıyor ve
**"Sipariş ve tamir" iki satıra düşüyordu** (ölçüm: kırık 1, Range çok satır 1; görüntü alındı). Önceki turdaki kiracı 16 px'te
olduğu için görünmemişti. Düzeltme `app.css` S1-h bloğunda `.ekranlar-izgara` `minmax(12rem → 14rem, 1fr)` (1280'de 5 sütun,
235 px karo); kilit testi `S1h_11` 14rem'e güncellendi; mutasyon M3-6 aşağıda. Sonrasında 8 ölçümün 8'inde /ekranlar kırık 0.

#### Görüntü turu ölçüm tablosu (İngilizce tarayıcı, `sikisma-olc.js` birebir + Range satır sayımı)
Sütunlar: kart% · kırık · taşan · binen · boy · yatay kaydırma · [hidden]-görünen · TRY kalıbı · ₺ sayısı · çok satır (Range) · tek kelime kırık.

| Mod | Tema | Genişlik | Rota | kart% | kırık | taşan | binen | boy | kayd. | gizli | TRY | ₺ | cok | tek | dil | ICU* | görüntü |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| PRO | açık | 1280 | /ayarlar?sekme=gorunum | 95 | 0 | 0 | 0 | 1,5 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-aydinlik-1280-ayarlar-gorunum.png |
| PRO | açık | 1280 | /ayarlar?sekme=hesap | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-aydinlik-1280-ayarlar-hesap.png |
| PRO | açık | 1280 | /ayarlar?sekme=guvenlik | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-aydinlik-1280-ayarlar-guvenlik.png |
| PRO | açık | 1280 | /ayarlar?sekme=sistem | 95 | 0 | 0 | 0 | 1,1 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-aydinlik-1280-ayarlar-sistem.png |
| PRO | açık | 1280 | /ayarlar?sekme=destek | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-aydinlik-1280-ayarlar-destek.png |
| PRO | açık | 1280 | /ekranlar | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-aydinlik-1280-ekranlar.png |
| PRO | açık | 1280 | /islemler | 76 | 0 | 0 | 0 | 1,8 | 0 | 0 | 0 | 5 | 0 | 0 | en-US | — | pro-aydinlik-1280-islemler.png |
| PRO | açık | 1280 | / | 95 | 5 | 1 | 0 | 1,5 | 0 | 0 | 0 | 10 | 8 | 0 | en-US | — | pro-aydinlik-1280-panel.png |
| PRO | açık | 1280 | /raporlar | 95 | 16 | 0 | 0 | 4,5 | 0 | 0 | 0 | 34 | 0 | 0 | en-US | — | pro-aydinlik-1280-raporlar.png |
| PRO | açık | 375 | /ayarlar?sekme=gorunum | 94 | 0 | 0 | 0 | 2,8 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-aydinlik-375-ayarlar-gorunum.png |
| PRO | açık | 375 | /ayarlar?sekme=hesap | 94 | 0 | 0 | 0 | 1,3 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-aydinlik-375-ayarlar-hesap.png |
| PRO | açık | 375 | /ayarlar?sekme=guvenlik | 94 | 0 | 0 | 0 | 1,5 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-aydinlik-375-ayarlar-guvenlik.png |
| PRO | açık | 375 | /ayarlar?sekme=sistem | 94 | 0 | 0 | 0 | 1,6 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-aydinlik-375-ayarlar-sistem.png |
| PRO | açık | 375 | /ayarlar?sekme=destek | 94 | 0 | 0 | 0 | 2,6 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-aydinlik-375-ayarlar-destek.png |
| PRO | açık | 375 | /ekranlar | 91 | 0 | 0 | 0 | 4,2 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-aydinlik-375-ekranlar.png |
| PRO | açık | 375 | /islemler | 94 | 0 | 0 | 0 | 2,3 | 0 | 0 | 0 | 1 | 0 | 0 | en-US | — | pro-aydinlik-375-islemler.png |
| PRO | açık | 375 | / | 94 | 5 | 0 | 0 | 2,7 | 0 | 0 | 0 | 6 | 3 | 0 | en-US | — | pro-aydinlik-375-panel.png |
| PRO | açık | 375 | /raporlar | 94 | 18 | 0 | 0 | 7,2 | 0 | 0 | 0 | 30 | 1 | 0 | en-US | — | pro-aydinlik-375-raporlar.png |
| PRO | koyu | 1280 | /ayarlar?sekme=gorunum | 95 | 0 | 0 | 0 | 1,5 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-karanlik-1280-ayarlar-gorunum.png |
| PRO | koyu | 1280 | /ayarlar?sekme=hesap | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-karanlik-1280-ayarlar-hesap.png |
| PRO | koyu | 1280 | /ayarlar?sekme=guvenlik | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-karanlik-1280-ayarlar-guvenlik.png |
| PRO | koyu | 1280 | /ayarlar?sekme=sistem | 95 | 0 | 0 | 0 | 1,1 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-karanlik-1280-ayarlar-sistem.png |
| PRO | koyu | 1280 | /ayarlar?sekme=destek | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | pro-karanlik-1280-ayarlar-destek.png |
| PRO | koyu | 1280 | /ekranlar | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-karanlik-1280-ekranlar.png |
| PRO | koyu | 1280 | /islemler | 76 | 0 | 0 | 0 | 1,8 | 0 | 0 | 0 | 5 | 0 | 0 | en-US | — | pro-karanlik-1280-islemler.png |
| PRO | koyu | 1280 | / | 95 | 5 | 1 | 0 | 1,5 | 0 | 0 | 0 | 10 | 8 | 0 | en-US | — | pro-karanlik-1280-panel.png |
| PRO | koyu | 1280 | /raporlar | 95 | 16 | 0 | 0 | 4,5 | 0 | 0 | 0 | 34 | 0 | 0 | en-US | — | pro-karanlik-1280-raporlar.png |
| PRO | koyu | 375 | /ayarlar?sekme=gorunum | 94 | 0 | 0 | 0 | 2,8 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-karanlik-375-ayarlar-gorunum.png |
| PRO | koyu | 375 | /ayarlar?sekme=hesap | 94 | 0 | 0 | 0 | 1,3 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-karanlik-375-ayarlar-hesap.png |
| PRO | koyu | 375 | /ayarlar?sekme=guvenlik | 94 | 0 | 0 | 0 | 1,5 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-karanlik-375-ayarlar-guvenlik.png |
| PRO | koyu | 375 | /ayarlar?sekme=sistem | 94 | 0 | 0 | 0 | 1,6 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-karanlik-375-ayarlar-sistem.png |
| PRO | koyu | 375 | /ayarlar?sekme=destek | 94 | 0 | 0 | 0 | 2,6 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-karanlik-375-ayarlar-destek.png |
| PRO | koyu | 375 | /ekranlar | 91 | 0 | 0 | 0 | 4,2 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | pro-karanlik-375-ekranlar.png |
| PRO | koyu | 375 | /islemler | 94 | 0 | 0 | 0 | 2,3 | 0 | 0 | 0 | 1 | 0 | 0 | en-US | — | pro-karanlik-375-islemler.png |
| PRO | koyu | 375 | / | 94 | 5 | 0 | 0 | 2,7 | 0 | 0 | 0 | 6 | 3 | 0 | en-US | — | pro-karanlik-375-panel.png |
| PRO | koyu | 375 | /raporlar | 94 | 18 | 0 | 0 | 7,2 | 0 | 0 | 0 | 30 | 1 | 0 | en-US | — | pro-karanlik-375-raporlar.png |
| BASIT | açık | 1280 | /ayarlar?sekme=gorunum | 95 | 0 | 0 | 0 | 1,5 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-aydinlik-1280-ayarlar-gorunum.png |
| BASIT | açık | 1280 | /ayarlar?sekme=hesap | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-aydinlik-1280-ayarlar-hesap.png |
| BASIT | açık | 1280 | /ayarlar?sekme=guvenlik | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-aydinlik-1280-ayarlar-guvenlik.png |
| BASIT | açık | 1280 | /ayarlar?sekme=sistem | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-aydinlik-1280-ayarlar-sistem.png |
| BASIT | açık | 1280 | /ayarlar?sekme=destek | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-aydinlik-1280-ayarlar-destek.png |
| BASIT | açık | 1280 | /ekranlar | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-aydinlik-1280-ekranlar.png |
| BASIT | açık | 1280 | /islemler | 76 | 0 | 0 | 0 | 2,2 | 0 | 0 | 0 | 5 | 0 | 0 | en-US | — | basit-aydinlik-1280-islemler.png |
| BASIT | açık | 1280 | / | 95 | 0 | 0 | 0 | 1,3 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | basit-aydinlik-1280-panel.png |
| BASIT | açık | 1280 | /raporlar | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 5 | 0 | 0 | en-US | — | basit-aydinlik-1280-raporlar.png |
| BASIT | açık | 375 | /ayarlar?sekme=gorunum | 94 | 0 | 0 | 0 | 2,7 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-aydinlik-375-ayarlar-gorunum.png |
| BASIT | açık | 375 | /ayarlar?sekme=hesap | 94 | 0 | 0 | 0 | 1,2 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-aydinlik-375-ayarlar-hesap.png |
| BASIT | açık | 375 | /ayarlar?sekme=guvenlik | 94 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-aydinlik-375-ayarlar-guvenlik.png |
| BASIT | açık | 375 | /ayarlar?sekme=sistem | 94 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-aydinlik-375-ayarlar-sistem.png |
| BASIT | açık | 375 | /ayarlar?sekme=destek | 94 | 0 | 0 | 0 | 2,5 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-aydinlik-375-ayarlar-destek.png |
| BASIT | açık | 375 | /ekranlar | 91 | 0 | 0 | 0 | 4,2 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-aydinlik-375-ekranlar.png |
| BASIT | açık | 375 | /islemler | 94 | 0 | 0 | 0 | 2,9 | 0 | 0 | 0 | 2 | 0 | 0 | en-US | — | basit-aydinlik-375-islemler.png |
| BASIT | açık | 375 | / | 94 | 1 | 0 | 0 | 2 | 0 | 0 | 0 | 1 | 0 | 0 | en-US | — | basit-aydinlik-375-panel.png |
| BASIT | açık | 375 | /raporlar | 94 | 0 | 0 | 0 | 2,3 | 0 | 0 | 0 | 2 | 0 | 0 | en-US | — | basit-aydinlik-375-raporlar.png |
| BASIT | koyu | 1280 | /ayarlar?sekme=gorunum | 95 | 0 | 0 | 0 | 1,5 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-karanlik-1280-ayarlar-gorunum.png |
| BASIT | koyu | 1280 | /ayarlar?sekme=hesap | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-karanlik-1280-ayarlar-hesap.png |
| BASIT | koyu | 1280 | /ayarlar?sekme=guvenlik | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-karanlik-1280-ayarlar-guvenlik.png |
| BASIT | koyu | 1280 | /ayarlar?sekme=sistem | 95 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-karanlik-1280-ayarlar-sistem.png |
| BASIT | koyu | 1280 | /ayarlar?sekme=destek | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 3 | 0 | 0 | en-US | — | basit-karanlik-1280-ayarlar-destek.png |
| BASIT | koyu | 1280 | /ekranlar | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-karanlik-1280-ekranlar.png |
| BASIT | koyu | 1280 | /islemler | 76 | 0 | 0 | 0 | 2,2 | 0 | 0 | 0 | 5 | 0 | 0 | en-US | — | basit-karanlik-1280-islemler.png |
| BASIT | koyu | 1280 | / | 95 | 0 | 0 | 0 | 1,3 | 0 | 0 | 0 | 4 | 0 | 0 | en-US | — | basit-karanlik-1280-panel.png |
| BASIT | koyu | 1280 | /raporlar | 95 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 5 | 0 | 0 | en-US | — | basit-karanlik-1280-raporlar.png |
| BASIT | koyu | 375 | /ayarlar?sekme=gorunum | 94 | 0 | 0 | 0 | 2,7 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-karanlik-375-ayarlar-gorunum.png |
| BASIT | koyu | 375 | /ayarlar?sekme=hesap | 94 | 0 | 0 | 0 | 1,2 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-karanlik-375-ayarlar-hesap.png |
| BASIT | koyu | 375 | /ayarlar?sekme=guvenlik | 94 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-karanlik-375-ayarlar-guvenlik.png |
| BASIT | koyu | 375 | /ayarlar?sekme=sistem | 94 | 0 | 0 | 0 | 1,4 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-karanlik-375-ayarlar-sistem.png |
| BASIT | koyu | 375 | /ayarlar?sekme=destek | 94 | 0 | 0 | 0 | 2,5 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-karanlik-375-ayarlar-destek.png |
| BASIT | koyu | 375 | /ekranlar | 91 | 0 | 0 | 0 | 4,2 | 0 | 0 | 0 | 0 | 0 | 0 | en-US | — | basit-karanlik-375-ekranlar.png |
| BASIT | koyu | 375 | /islemler | 94 | 0 | 0 | 0 | 2,9 | 0 | 0 | 0 | 2 | 0 | 0 | en-US | — | basit-karanlik-375-islemler.png |
| BASIT | koyu | 375 | / | 94 | 1 | 0 | 0 | 2 | 0 | 0 | 0 | 1 | 0 | 0 | en-US | — | basit-karanlik-375-panel.png |
| BASIT | koyu | 375 | /raporlar | 94 | 0 | 0 | 0 | 2,3 | 0 | 0 | 0 | 2 | 0 | 0 | en-US | — | basit-karanlik-375-raporlar.png |

\* ICU sütunu boş: yalnız ilk (önbelleksiz) yüklemede kaynak listesinde görünür — deneme koşumunda `icudt.dat` ölçüldü (yukarıda).
Özet: /ayarlar (5 sekme) 40/40 ölçümde kart% 94–95, kırık 0, taşan 0, binen 0; 1280'de boy 1,0–1,5 (≤ 1,6 ✓). /ekranlar ve /islemler
16/16 kırık 0, tek 0. Panel ve /raporlar 16/16 **binen 0** (BASİT Panel'de `#pozisyon-emanet "Müşteri emaneti 16,9 g"` ve
`#pozisyon-disarida` `display:flex`, satırlar 326–343 / 326–364 px, binme yok — `basit-aydinlik-1280-panel.png`). 375 px'te yatay kaydırma 0
(36/36). Panel PRO (kırık 5, taşan 1) ve /raporlar PRO (kırık 16/18) değerleri önceki turla aynıdır ve S1-i'ye devredilmiş öğelerdir
(§ 13 md.1); `pro-aydinlik-1280-panel.png`'de "Hızlı eylemler" düğmelerinin ("Satış Yap", "Alış Yap", "Hurda Alış") gerçekten iki satıra
düştüğü GÖRÜLÜYOR — Range sayımı bunları yakalamıyor çünkü yazı ikonla aynı düğmede çıplak metin düğümü; S1-i bunu ölçmeli.

İlk turdaki (bölme, tab-21) dört görüntü transkripttedir; yukarıdaki 72 dosya bunların yerini alan kalıcı kanıttır.

### 8f. Kapanış (temizlik) — § 10'da

## 9. DoD sözleşmesi
Bu paket YENİ varlık/kayıt türü, yeni uç, yeni form ya da yeni yazma işlemi EKLEMEDİ (yalnız sunum/yerleşim).
- Sekmeler bileşeni istek atmaz; her sekme düğmesi gerçek bir eylem yapar (URL + seçim) — "tıklanınca hiçbir şey olmayan düğme" yok.
- Taşınan bölümlerin düzenleme yolları aynen: profil → Çekmece (`#ay-profil-duzenle`), şifre formu, MFA kur/kapat (ModalPencere yalnız yıkıcı onay), tema/yazı/mod seçimleri, destek paketi (DegismezIpucu korunuyor), rol → DegismezIpucu (değişmez gerekçesi).
- Yeni ModalPencere düzenleme kullanımı: 0. Yeni CSS'te sabit renk 0, satır içi stil 0 (test S1h_12).

## 10. Kapanış
- Sekme tab-21 kapatıldı (görünüm boyutu önce `desktop`'a döndürüldü); başka sekmeye dokunulmadı.
- Vekil (5175) ve API (5176, `dotnet run` ebeveyni + dinleyen alt süreç) yalnız kendi PID'leriyle `kill`; `lsof -iTCP:5176/5175 -sTCP:LISTEN` → **boş**.
- `betikler/kiraci-temizle.sh 01M38ZKQT5G50RNY3PJD8N4X90 --uygula` → 312 satır + tenants 1 satır silindi (cari_bakiyeler 2, cariler 2, denetim_kayitlari 197, hareketler 1, is_calismalari 3, kullanicilar 1, kur_snapshots 99, refresh_tokenler 4, subeler 1, telemetri_olcumleri 2).
- `kiraci_davetleri`'ndeki kendi davet satırım: 1 → 0.
- SQL kanıtı: `tenant_id` kolonlu TÜM tablolarda bu kiracının satırı toplamı **0**; `tenants` **10 → 10** (başlangıçla aynı); `PAKETS1-h%` adlı kiracı 0. QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- Parola/anahtar/jeton dosyaları (`fikstur.json`, `sa.txt`, `kayit*.json`, `davet.json`) `printf "" >` ile boşaltıldı.

### 10 ek — 2. tur kapanışı
- API 5176 (ebeveyn + dinleyen), vekil 5175 ve başsız Chromium (9377) yalnız kendi PID'leriyle durduruldu → üç port da **boş**.
- `kiraci-temizle.sh 01M392DC5QWQ7V527PBE4PBXGC --uygula` → 161 satır + tenants 1 satır silindi. Sonra `tenant_id` kolonlu 70 tablonun toplamı **1** çıktı (`kur_snapshots`, 06:53:57 UTC — API kapanırken kur yoklayıcısının yazdığı satır); o satır da silindi → toplam **0**. Davet satırı `01M392DC3YF5MAAPR0NC5TDRF6` silindi (1 → 0).
- `tenants`: 11 (başka şeridin `PAKETQ9-2-TEST`'i dahil; bu paketin kiracısı yok). QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- `fikstur.json` ve `sa.txt` `printf "" >` ile boşaltıldı (0 bayt).

## 11. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin payı)
- ` M dotnet/Kuyumcu.Web/Kuyumcu.Web.csproj`
- ` M dotnet/Kuyumcu.Web/Pages/Ayarlar.razor`
- ` M dotnet/Kuyumcu.Web/wwwroot/css/app.css` (yalnız sona ek)
- `?? dotnet/Kuyumcu.Web/Bilesenler/Sekmeler.razor` (YENİ)
- `?? dotnet/Kuyumcu.Web/Bilesenler/SekmeTanimi.cs` (YENİ)
- `?? dotnet/Kuyumcu.Core/I18n/Sozlukler.Ayarlar.cs` (YENİ)
- `?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisIslemIzgarasi.razor` (izlenmeyen dizinde; 1 sınıf eklendi)
- `?? dotnet/Kuyumcu.Web/Bilesenler/AltinPozisyonuKarti.razor` (izlenmeyen; 1 sınıf eklendi)
- `?? dotnet/Kuyumcu.Tests/S1hGorselSigmaTestleri.cs` (YENİ, izlenmeyen Tests dizini içinde; 2. turda 14rem kilidi)
- 2. tur: `app.css` S1-h bloğunda tek satır (`minmax(14rem, 1fr)`) + gerekçe yorumu; `dokumanlar/09-uygulama-kayitlari/ham/S1-h-goruntuler/` (72 PNG, YENİ)

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Türkçe dil verisi, csproj özelliği + kilit testi | ✓ | csproj; S1h_01; boot.json `all/icudt.dat`; M1-1/M1-2 |
| 'TRY' kalıbı 0 | ✓ | tüm ölçümlerde `TRY 0`; İngilizce (`en-US`) Chromium'da 72/72 TRY 0, `₺6.727,48` |
| Ekran görüntüsü (gerçek tarayıcı kuralı) | ✓ | 72 PNG `ham/S1-h-goruntuler/` (1280+375 × BASİT+PRO × açık+koyu × 9 rota) |
| (2) Ortak Sekmeler (tablist/tab/tabpanel, ok tuşları, ?sekme=, geri, aria-selected, odak halkası, belirteç) | ✓ | Sekmeler.razor; S1h_02–05; § 8c; CSS S1h_12 |
| (3) /ayarlar sekmeli, kart% ≥ 90 | ✓ | 1280: 95, 375: 94 |
| /ayarlar kırık 0, taşan 0 | ✓ | tüm sekme × mod × tema × genişlik |
| /ayarlar boy ≤ 1,6 (her sekmede, 1280) | ✓ | 1,6 / 1,0 / 1,0 / 1,1 / 1,4 (375'te Görünüm 2,8–2,9 — § 2 md.7) |
| Okyanus + Seçili rozeti binmez; Profesyonel taşmaz; başlıklar tek satır; "16 px" tek satır | ✓ | binen 0, taşan 0, cok 0 (Okyanus/Fildişi seçiliyken de) |
| (4) /ekranlar ve /islemler kırık 0; tek kelime harf ortasından bölünmez | ✓ | tüm kombinasyonlarda kırık 0, tek 0 (2. turda 18 px varsayılanında bulunan "Sipariş ve tamir" kırığı 14rem ile kapatıldı); S1h_11; M3, M3-6 |
| (5) Panel ve /raporlar binen 0 | ✓ | tüm kombinasyonlarda 0; M4-1 tarayıcıda 1 → 0 |
| 375 px yatay kaydırma 0 | ✓ | tüm rotalarda 0 |
| Mevcut Ayarlar/Görünüm testleri YEŞİL (id'ler aynı) | ✓ | 313/313 ilgili küme |
| Emoji ikon yok | ✓ | S1h_10 |
| Sözlük hub'ına dokunulmadı; yeni metin Sozlukler.Ayarlar.cs | ✓ | Sozlukler.cs değişmedi |
| Mutasyonlar (csproj · aria-selected · break-all) | ✓ (break-all tarayıcı kısmı § 2 md.5) | § 6 |
| denetim.sh TEMİZ | ✓ | § 7 |
| Test kiracısı silindi, tenants başlangıç sayısında | ✓ | 10 → 10; kiracı satırı toplamı 0 (§ 10) |

## 13. Açık kalanlar
0. **2. tur notları (24.09 09:55):** (a) `dotnet/denetim.sh` bu turda **BAŞARISIZ** çıktı: `Sınırsız ToListAsync (Modules · B-8) 146 (tavan 145)` ve `P-36: HAM_INPUT 47 > tavan 42`. Bu paketin dosyalarında `<input|textarea` sayısı 0 (Ayarlar.razor, Sekmeler.razor, FisIslemIzgarasi.razor, AltinPozisyonuKarti.razor) ve S1-h Modules'a dokunmadı; aynı anda başka bir şerit `Pages/Faturalar.razor` (09:50) ve `Modules/Fatura/FaturaModulu.cs` (09:47), `Modules/Banka/BankaModulu.cs` (09:32) dosyalarını değiştiriyordu — sahibi o şerit (Fatura/Banka). S1-h'nin 1. tur sonunda denetim TEMİZ idi (§ 7). (b) `tenants` başlangıçta 10 idi, bitişte **11**: fazladan kiracı `PAKETQ9-2-TEST` başka şeridin; bu paketin kiracısı silindi (bkz. § 10 ek). (c) Kurucunun 5199 Release anlık görüntüsü hâlâ eski `sharded` ICU modunda olabilir — orkestratör dalga kapısında yayını yenilemeli (doğrulayıcı [ORTA] bulgusu; bu paket 5199'a dokunmaz).
1. **S1-i'ye**: Panel düğmeleri (`#panel-duzen-modu`, `#panel-satis-yap`, `#panel-alis-yap`, `#panel-tahsilat`, `#panel-hurda`, `#panel-borclu-eylem-*`) ve Raporlar kartları (`#rp-kar-zarar`, `#rm-r01…`) sabit yükseklik kaynaklı "kırık" ölçümü; `p.stat-deger` taşması (PRO). Bu paketin dosya alanı dışında.
2. **Ağaçta eşzamanlı başka iş**: çalışma sırasında `Kuyumcu.Api/Modules/Banka/BankaModulu.cs` 09:10:53'te değişti (bende değil) ve bir derlemede `CS0162 Ulaşılamayan kod (746,20)` uyarısı verdi; bir mutasyon derlemesi başka sürecin `obj/Kuyumcu.Web.dll` kilidi yüzünden düştü. "Şerit tek başına" varsayımı ağaçta doğrulanmadı — orkestratörün bilgisine.
3. ~~375 px ekran görüntüsü alınamadı~~ → 2. turda 72 dosya olarak alındı (§ 8e).
4. ~~İngilizce tarayıcı dili emüle edilemedi~~ → 2. turda `--lang=en-US` Chromium ile ölçüldü: `navigator.language en-US`, `icudt.dat`, TRY 0 (§ 8e).

## 14. Kurucuya / mali müşavire not
Mevzuat içeren değişiklik YOK. Ürün metni fiyat/teklif/tahsilat içermiyor.
