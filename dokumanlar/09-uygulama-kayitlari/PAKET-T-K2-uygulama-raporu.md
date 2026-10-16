# PAKET T-K2 — KABUK TUVALE (kenar çubuğu + üst bar) · uygulama raporu

Tarih: 2026-09-24 · Şerit: T-KABUK (vt `kuyumcu_test_t1` · canlı API 5160 · vekil 5161) · Rol: Taşkın + Ertuğ
Durum beyanı: **BİTTİ** — paralel şerit kapısının 7 maddesi sağlandı (§ 10). Tam süitteki tek kırmızı başka şeridin testidir, tek başına yeşildir (§ 6).

## 1. Kaynak ve spesifikasyon doğrulaması
Tuval: `tuval/project/Panel.dc.html` (BASİT), `Profesyonel.dc.html` (PRO), `Musteriler.dc.html`, `Ayarlar.dc.html` + `ham/TUVAL-png/*.png`.
Yöntem: tuvaldeki ve üründeki kabuğun **her görünür öğesi** başsız Chrome'da (CDP) `getBoundingClientRect` + computed style ile ölçüldü (`scratchpad/tk2/olc-kabuk.js`). Ölçümler piksel düzeyinde karşılaştırıldı (§ 8).

Koda karşı "mevcut durum" (başlangıç ölçümü, `olc-once-*.txt`):
- Menü 1440×900'de **995 px** (BASİT) / sayfa boyu (PRO, 2152 px) uzunluğundaydı; kullanıcı kartı ekranın dışına (y=893) düşüyordu.
- Marka "MİHENK" (600), mavi kare yok; TR/EN seçici marka satırındaydı.
- Arama "Menüde ara" 14 px; "Tüm ekranlar" 16 px ama y=162 (tuval 128).
- Açık grup başlığında sayı görünüyordu; kapalı grubun önizlemesi **700** kalınlıktaydı (tuval 400), grup aralığı 80 px (tuval 63).
- PRO menü BASİT görünümündeydi (ikonlu, 17 px, önizlemeli); Favoriler başlığı ve yıldız yoktu.
- PRO üst bar 1440'ta **iki satıra iniyordu** (93 px): "Müşteri, ürün ara" düğmesi.
- Şube seçici üst bardaydı (tuvalde yok).

## 2. Yapılanlar
| # | Tuval maddesi | Uygulama |
|---|---|---|
| 1 | Mavi kare + elmas + "Mihenk" Sora 21/700 | `KenarCubugu`: `.kenar-marka-rozet` (34 px, 9 px yarıçap, `--vurgu`); yazım `KabukTuvalSozlugu.MarkaYazimi` — `Marka.Ad`'dan tr-TR ile **türetilir** (ikinci kaynak yok). TR/EN seçici kart menüsüne taşındı. |
| 2 | "Ekran ara" (PRO "⌘K") | Yer tutucu "Ekran ara"/"Find screen"; PRO'da `#menu-arama-kisayol` ipucu (title: Ctrl+K/⌘K neyi açar). S1-j menü içi süzme AYNEN (testleri yeşil). Görsel-gizli etiket erişilebilir ad olarak kaldı. |
| 3 | "Tüm ekranlar (38)" | Ölçüler tuvale çekildi (9/12 px dolgu, 16 px; PRO 7/12, 15 px). |
| 4 | PRO FAVORİLER | 13 px BÜYÜK HARF başlık; satırlar sarı yıldız (`--amber-400`); favori satırı vurgulanmaz (tek vurgu grup satırında, tuval gibi). |
| 5 | Açık grup / kapalı grup + önizleme | Açık: 15/600 BÜYÜK HARF soluk + ok (sayı yalnız ekran okuyucuda). Kapalı: 17/600 ad + sayı + ok + 14/400 tek satır önizleme. PRO: ikonsuz, 26 px içeriden 16 px satırlar, başlıklar beyaz 15/600, önizleme yok. Bildirimler satırında kırmızı sayı hapı (`#kenar-bildirim-rozet`). |
| 6 | Kullanıcı kartı + dişli | Kart = düğme (`#kenar-kullanici-menu-ac`, aria-expanded); "Ramazan A." (tam ad title'da); rol (PRO: " · şube"); dişli `#kenar-ayarlar-disli` → `/ayarlar`. Kart menüsü (`#kenar-kullanici-menu`): Dil (TR/EN), çoklu şube seçici, Kılavuz, Ayarlar, "Sırayı sıfırla", "Katlamayı sıfırla", Çıkış. Esc kapatır; ekran değişince kapanır. |
| 7 | Üst bar | Başlık/kur şeridi/Yardım/hap T-K1 + S1-c ile zaten tuvaldeydi; **sayfa sekmeleri yuvası** eklendi (`<UstBarSekmeleri>` → `#ust-sayfa-sekmeleri`, AnaYerlesim'den cascade). Şube seçici üst bardan kaldırıldı (kart menüsüne). Geniş ekranda "Müşteri, ürün ara" düğmesi ve normal bildirim zili gizli (bkz. § 3). |
| — | Menü ekran boyunda | ≥769 px: `position: sticky; height: 100vh`, liste kendi içinde kayar, kart dipte (1440: kart altı 880, 1280: 780). |

Dokunulan sayfalar (TEK DOKUNUŞ kaydı): **hiçbir `Pages/*.razor` açılmadı**. Dokunulan kabuk dosyaları: `Layout/KenarCubugu.razor`, `Layout/UstBar.razor`, `Layout/AnaYerlesim.razor`, `Bilesenler/SubeSecici.razor`, `wwwroot/css/tuval-kabuk.css`; yeni: `Layout/UstBarSekmeYuvasi.cs`, `Layout/UstBarSekmeleri.razor`, `Kuyumcu.Core/I18n/Sozlukler.KabukTuval.cs`. `DilSecici.razor` ve `MenuTanimi.cs` DEĞİŞMEDİ. app.css ve Sozlukler hub'ı DEĞİŞMEDİ.

## 3. Sapmalar ve gerekçeleri (tuvalde olmayan ama silinmeyen)
| Öğe | Karar | Gerekçe |
|---|---|---|
| "Müşteri, ürün ara" (#ust-kayit-ara) | ≥769 px'te gizli (DOM'da), ≤768'de görünür | Tuvalde üst barda yok; PRO 1440'ta barı iki satıra indiriyordu. Palet SİLİNMEDİ: Ctrl/⌘+K açar; PRO menü kutusundaki "Ctrl K" ipucu bunu söyler (title). Palet açılınca kabı görünür (`:has`). |
| Üst bar bildirim zili | Normal rozet ≥769'da gizli; **hata rozeti (?) görünür** | Tuvalde sayı menüdeki "Bildirimler" satırında haptır → yeni `#kenar-bildirim-rozet`. Yoklama tek: UstBar → `BildirimSayisiDegisti` → AnaYerlesim → KenarCubugu. UX-S28: hata rozeti gizlenmez. Dar ekranda (çekmece) zil görünür. |
| Şube seçici | Üst bardan kart menüsüne | Tuvalde üst barda şube yok. UX-S28: `/subeler` okunamazsa (`SubeSecici.HataDegisti`) kart menüsü KENDİLİĞİNDEN açık durur, hata bandı görünür. |
| TR/EN, Kılavuz, Çıkış, sıfırlama düğmeleri | Kart menüsü | Tuvalde yok; özellik silinmedi. Kılavuz üst bardaki "Yardım" ile de açılır. S1-j: arama alt bloktan eşleşirse menü açılır. |
| Ok işaretleri ▾/▸ | Ikon bileşeni (chevron-down/right, 12 px) | Kural: ikon = Ikon bileşeni (ince çizgi); ▾/▸ metin karakteri yerine sprite. |
| Menü ikonları (Panel ev, Satış kart, Emanet kilit, Sipariş pano) | Mevcut sprite ikonları | `wwwroot/img/ikon.svg` şerit alanı dışında; ev/kredi kartı sembolü sprite'ta yok. İkon sözlüğü "Tüm ekranlar" sayfasıyla ortak — tek şeritte değiştirmek iki ekranı ayrıştırırdı. Kurucuya not. |
| "⌘K" yazısı | "Ctrl K" (mevcut `Kabuk.MenuAramaKisayol`) | Platform algılama JS gerektirir (şerit dışı dosya); kısayol dinleyicisi hem Ctrl hem ⌘ kabul eder, title ikisini de yazar. |
| 769–1439 px PRO kur kaynağı ("· Altınkaynak") | Gizli; saatin ipucunda "Kaynak: …" | Tuval 1440 içindir; 1280 PRO'da bar 93 px'e iniyordu. Bilgi kaybolmaz (PRO saat title'ı). 1440'ta görünür. |
| Tezgah tuvalindeki "Tema" noktaları | Eklenmedi | canvas.json notu: tuvalin DEMO aracı; tema seçimi Ayarlar → Görünüm'de. |
| Tuval örnek verisi (Ramazan A., 3 bildirim) | Gerçek veri | Kiracının kullanıcısı "Demo K."; açık bildirim 0 → hap çizilmez (bUnit'te 3 ile kilitli). |
| Sayfa sekmeleri yuvası | Blazor Sections yerine cascade | `SectionOutlet` aynı test bağlamında ikinci UstBar'ı reddediyordu (ölçüldü: 2 mevcut test `InvalidOperationException`). Cascade'de statik durum yok. |

## 4. Migration
Gerekmedi (şema değişikliği yok).

## 5. DoD sözleşmesi
Yeni varlık/kayıt türü **yok**. Mevcut kayıtlar: kullanıcı tercihleri (favoriler, katlama, sıra) — düzenleme/silme yolları aynen: favori ekle/çıkar "Tüm ekranlar"daki yıldız (PATCH), "Katlamayı sıfırla"/"Sırayı sıfırla" (DELETE) artık kart menüsünde. Her yeni düğme gerçek etki üretir: kart düğmesi menüyü açar/kapatır (test), Çıkış oturumu kapatır ve `/giris`'e gider (test), dil düğmesi dili değiştirir (test). Kısayol ipucu etkileşimsiz `<kbd>`dir (düğme değil). ModalPencere kullanımı yok.

## 6. Derleme ve testler
- `dotnet build Kuyumcu.sln`: **0 uyarı 0 hata** (son: `b9.log`). (Arada 1 uyarı görüldü: `EtiketEkranTestleri.cs(145)` xUnit1031 — başka şeridin dosyası, sonraki derlemede kayboldu.)
- Yeni: `TK2KabukTuvalTestleri` **20 test**, 20/20 yeşil. Güncellenen (tuval metni): `P37RevizyonTestleri` (yer tutucu), `S1jMenuAramaTestleri` (yer tutucu), `MenuKatlamaTestleri` (marka "Mihenk" + Marka.Ad'dan türetme), `S1gMenuTuvalTestleri` ("Ramazan A." + tam ad title), `IkonTestleri` (dişli bağlantısı `/ayarlar`).
- Kabuk filtreli koşum (17 sınıf): **0 başarısız** (360 → 361 test).
- Tam süit (`trx-pT-K2/_Mac_2026-09-24_23_03_30.trx`): **5606 · 5605 geçti · 1 başarısız** — `Dogrulayici.Q102EkranSinirTestleri.DQ102_turkce_giris_1_5_kabul_1_500_belirsiz_red` (Hurda ekranı, Q10-2/Q10-A şeridi; T-K2 dosyası değil). Tek başına koşum: **10/10 geçti** → kararsız, başkasına ait.
- Ara koşumda `KabukAramaEkranTestleri.Secilen_sube_kabuk_durumuna...` bir kez kırmızı, sonraki iki koşumda yeşil (statik `KabukServisi.Ortak` paylaşımı; test değişmedi, SubeSecici davranışı değişmedi).

## 7. Mutasyon kanıtları (kopya: `scratchpad/mut-yedek/T-K2/`, her biri `cmp` AYNI)
| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M1 | Marka `MarkaYazimi` → `Marka.Ad` | `MenuKatlamaTestleri.Marka_satiri_TEK_KAYNAKTAN...`, `TK2_Marka_satiri_mavi_kare...` |
| M2 | Kart menüsünden `<DilSecici />` silindi | `TK2_Kart_menusu_TASINANLARI...`, `TK2_Marka_satiri_mavi_kare...` |
| M3 | Kart düğmesi durumu değiştirmiyor | `TK2_Kart_dugmesi_menuyu_ACAR_KAPATIR_Esc_kapatir` |
| M4 | Şube hatası menüyü açmıyor | `TK2_Sube_listesi_okunamazsa_kart_menusu_KENDILIGINDEN...` |
| M5 | UstBar sayıyı kabuğa bildirmiyor | `TK2_UstBar_yokladigi_bildirim_sayisini_kabuga_BILDIRIR` |
| M6 | Hap sayı 0'da da çiziliyor | `TK2_Bildirim_sayisi_menu_satirinda_hap_sifirda_HIC_cizilmez` |
| M7 | CSS: kayıt-ara gizleme silindi | `TK2_CSS_ust_barda_kayit_arama_dugmesi...` |
| M8 | Kısayol ipucu BASİT'te | `TK2_Arama_yer_tutucusu_Ekran_ara_PRO_da_kisayol...` |
| M9 | `UstBarSekmeleri.Dispose` boş | `TK2_Sayfa_sekmeleri_yuvasi...` |
| M10 | Favori satırı ekran ikonu | `TK2_PRO_favori_satiri_SARI_YILDIZ...` |
| M11 | Esc anahtarı yanlış | `TK2_Kart_dugmesi_menuyu_ACAR_KAPATIR_Esc_kapatir` |
| M12 | UstBar'a `<SubeSecici />` geri | `TK2_Coklu_subede_secici_KART_MENUSUNDE_ust_barda_YOK` |
| M13 | Arama eşleşmesi menüyü açmıyor | `TK2_Arama_ALT_BLOKTAN_eslesirse...` |
| M14 | CSS'e sabit renk `#ffffff` | `TK2_CSS_tuval_olculeri_ve_BELIRTEC_disi_renk_YOK` |
| M15 | `SubeSecici.HataDegisti` çağrılmıyor | `TK2_Sube_listesi_okunamazsa...` |
| M16 | Sekme yuvası hiç çizilmiyor | `TK2_Sayfa_sekmeleri_yuvasi...` |
| M17 | PRO saat ipucunda kaynak yok | `TK2_PRO_saat_ipucu_KAYNAK_adini_tasir...` |
| M18 | CSS: boş kayıt-arama kabı gizleme silindi | `TK2_CSS_ust_barda_kayit_arama_dugmesi...` |
| M19 | CSS: 769–1439 kaynak gizleme silindi | `TK2_PRO_saat_ipucu_KAYNAK_adini_tasir...` |

Madde 1 notları: (a) M5 ve M7'nin ilk koşumu eşzamanlı şeritlerin `obj/` kilidi (`Kuyumcu.Web.pdb/dll being used by another process`) ve benim geri yüklemede `copy2`'nin eski mtime'ı koruması yüzünden BAYAT DLL ile koştu (M7'de M5'in kırmızısı göründü). Betik düzeltildi (geri yüklemede yeni mtime + kilitte 5 kez yeniden derleme); M5/M7 temiz koşumda yukarıdaki tek doğru testi kırdı. (b) M17 ilk koşum `NETSDK1177 apphost is already signed` (başka şeridin eşzamanlı derlemesi) → yeniden koşuldu. Son mutasyondan sonra `touch` + temiz `dotnet build`: 0 uyarı 0 hata.

## 8. Görsel karşılaştırma (ASIL kanıt) — 1440×900
Görüntüler: `ham/TUVAL-KARSILASTIRMA/T-K2-Panel-1440.png`, `T-K2-Profesyonel-1440.png`, `T-K2-Musteriler-1440.png`, `T-K2-Ayarlar-1440.png` (solda tuval, sağda ürün) + `T-K2-kartmenu-PRO-1440.png`, `T-K2-kartmenu-BASIT-koyu-1440.png`, `T-K2-1280-PRO-koyu-cariler.png`, `T-K2-375-cekmece-kartmenu.png`.

Bölge bölge (ölçüm px, x/y üst-sol; tuval → ürün):
| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Menü kutusu | 268×900, dolgu 20/14, #14213d | 268×900, 20/14, `--kenar-yuzey` | AYNI |
| Marka kare | 34×34 @22,22, 9 px, #2457d6 | 34×34 @22,22, 9 px, `--vurgu` | AYNI |
| Marka yazı | "Mihenk" Sora 21/700 beyaz | "Mihenk" Sora 21/700 | AYNI |
| Arama kutusu (BASİT) | 240×46 @14,70, 9 px, "Ekran ara" 16 | 240×46 @14,70, 9 px, "Ekran ara" 16 | AYNI |
| Arama kutusu (PRO) | 240×43 @14,68, 15 px, "Ekran ara ⌘K" | 240×43 @14,68, 15 px, "Ekran ara" + "Ctrl K" | GEREKÇELİ FARK (§ 3 ⌘K) |
| Tüm ekranlar | "Tüm ekranlar (38)" metin y≈137, 16 px | y=137, 16 px | AYNI |
| Açık grup başlığı | "GÜNLÜK İŞ ▾" 15/600 #8ea2c4 y=189 | 15/600 `--menu-baslik` BÜYÜK HARF y=188 + chevron | AYNI (ok: GEREKÇELİ FARK — Ikon) |
| Satırlar (BASİT) | 240×42 @14, y 216/260/303/347/391, 17 px, aktif 600 | 240×42 @14, y 215/259/303/347/391, 17 px, aktif 600 | AYNI (±1 px) |
| Bildirimler hapı | "3" kırmızı hap | canlı veri 0 → yok (bUnit: 3 ile hap) | GEREKÇELİ FARK (veri) |
| Kapalı gruplar | ad 17/600 y=449,512,576,639,702; önizleme 14/400 | y=448,512,575,639,702; önizleme 14/400 | AYNI |
| Kullanıcı kartı | 240×62 @14,818, 10 px; RA 36 px; ad 16/500; rol 14; dişli 20 | 240×62 @14,818, 10 px; DK 36 px; "Demo K." 16/500; "Sahip" 14; dişli 20 | AYNI (veri farkı) |
| PRO Favoriler | başlık y=166 13 px; satırlar 191/229/266 16 px sarı yıldız | y=166 13 px; 191/… 16 px sarı yıldız | AYNI |
| PRO gruplar | Günlük iş y=318; Panel 347; Satış (aktif) 383; Yönetim 675 | 318; 347; 383; 675 | AYNI |
| PRO kart | 240×59 @14,821; "Sahip · Merkez şube" 13 | 240×59 @14,821; "Sahip · Merkez Şube" 13 | AYNI |
| Üst bar | 68 px; başlık Sora 22/600 @296; HAS @392; Yardım @1151; hap @1230 | 68 px; @296; @392; @1151; @1230 | AYNI |
| Üst bar PRO | EUR + "14:32 · Altınkaynak" | EUR + "23:00 · Altınkaynak" | AYNI (1280'de kaynak ipucuda — § 3) |
| Kayıt ara düğmesi / zil | yok | geniş ekranda gizli | GEREKÇELİ FARK (§ 3) |
| Menü ikonları | ev/kart/kilit/pano | pano-ızgara/para/el sıkışma/kalem | GEREKÇELİ FARK (§ 3 sprite) |
| Sayfa gövdesi (Satış formu, Müşteriler tablosu, Ayarlar) | tuval | T-SATIS / T-PANEL-MUSTERI / T-K3 şeritleri | Kapsam dışı (başka şerit) |

## 9. Gerçek tarayıcı ölçümü — `betikler/sikisma-olc.js` (K1–K11 + yatay) + kabuk ek sayaçları
Araç: başsız Chrome + CDP (`scratchpad/tk2/cek.mjs`, aracın TAMAMI sayfaya yüklenip `__olcTam` koşuldu) — 48 kombinasyon: rota {/, /islemler, /cariler, /ayarlar} × {1440, 1280, 375} × {BASİT, PRO} × {açık, koyu}; ekran görüntüleri `scratchpad/tk2/png/matris/` (48 dosya). Çapraz doğrulama: tarayıcı bölmesinde bu şeridin 5161 sekmesi (`tab-50`; sekme sınırı dolu, yeni sekme açılamadı — "Could not open a new tab"), 1440 BASİT `/`: asideH 900 · headerH 68 · K1 0 · kabuk taşan 0 · TRY 0 · yatay 0 · "Mihenk" · "Ekran ara" · kart altı 880 (başsızla birebir).

| Genişlik | Mod | Tema | Rotalar | K1 | K3 taşan/kırık | K5 TRY | K6 dar | K7 | K8 | K9 | K10 | K11 | Yatay | Menü boyu | Üst bar | Kart görünür |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | BASİT | açık | 4 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 900 | 68 | evet (880) |
| 1440 | BASİT | koyu | 4 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 900 | 68 | evet |
| 1440 | PRO | açık | 4 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 900 | 68 | evet |
| 1440 | PRO | koyu | 4 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 900 | 68 | evet |
| 1280 | BASİT | açık/koyu | 4+4 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 800 | 68 | evet (780) |
| 1280 | PRO | açık/koyu | 4+4 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 800 | **68** (düzeltme öncesi 93) | evet |
| 375 | BASİT | açık/koyu | 4+4 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | çekmece | 129 | çekmecede evet |
| 375 | PRO | açık/koyu | 4+4 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | çekmece | 181 | çekmecede evet |
K2 (dar sütun) 83–98 %; `null` = ölçüm anında sayfa kartı çizilmemişti (T-K1'deki aynı not); K2/K4 sayfa gövdesinin ölçüsüdür (T-K2 dosyası değil). 375'te tuval yok: çekmece açık + kart menüsü görüntüsü `T-K2-375-cekmece-kartmenu.png`, yatay 0.

## 10. Canlı doğrulama (API 5160 · vekil 5161 · kuyumcu_net)
Test kiracısı `PAKETT-K2-TEST Kuyumculuk` (`01M3ACRGZT554RC4MVTD9Z5SVG`) `betikler/demo-tohumla.js --prova-kiraci` ile ürün uçlarından kuruldu (6 cari, 12 ürün, 9 satış; sistem anahtarı yalnız bu API sürecine verilen rastgele değer, yazılmadı).
| İstek | HTTP | Yanıt |
|---|---|---|
| POST /api/v1/auth/giris | 200 | accessToken var |
| GET /bildirimler/sayi | 200 | `{"sayi":0,"enYuksekOnem":null}` → menüde hap yok (doğru) |
| GET /subeler | 200 | tek şube "Merkez Şube" → kart "Sahip · Merkez Şube" (PRO), seçici yok (UX-S22) |
| GET /tercihler/benim | 200 | `menu-favoriler: ["/islemler","/kur","/faturalar"]` → PRO menü FAVORİLER: Satış · Kur · Faturalar |
SQL: `kullanici_tercihleri` (kiracı süzgeçli) `anahtar=menu-favoriler`, `deger=["/islemler","/kur","/faturalar"]` (tarayıcıda "Tüm ekranlar" yıldızlarıyla yazıldı); `bildirimler` kiracı satırı 0.
Temizlik: 5160/5161 durduruldu, `lsof` iki port için boş. `kiraci-temizle.sh … --uygula` → silindi; ardından information_schema'dan 70 tenant_id'li tablo tarandı: `kur_snapshots`'ta 1 artık satır (API'nin son kur yoklaması, 20:02:19 UTC) bulundu ve silindi → **70 tablo toplam 0 satır**; `kiraci_davetleri` PAKETT-K2 satırı 1 → 0. tenants: başlangıç **15** → son **15** (başka şeritlerin arada açtığı/sildiği kiracılara dokunulmadı; QA-TEST-* ve DEMO-* dokunulmadı).

## 11. Denetim
`dotnet/denetim.sh` → **DENETİM TEMİZ.** (rc=0)

## 12. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paket)
`dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` · `Layout/UstBar.razor` · `Layout/AnaYerlesim.razor` · `Bilesenler/SubeSecici.razor` · `wwwroot/css/tuval-kabuk.css` · yeni: `Layout/UstBarSekmeYuvasi.cs`, `Layout/UstBarSekmeleri.razor`, `Kuyumcu.Core/I18n/Sozlukler.KabukTuval.cs`, `Kuyumcu.Tests/TK2KabukTuvalTestleri.cs` · testler: `MenuKatlamaTestleri.cs`, `S1gMenuTuvalTestleri.cs`, `S1jMenuAramaTestleri.cs`, `P37RevizyonTestleri.cs`, `IkonTestleri.cs` · rapor + `ham/TUVAL-KARSILASTIRMA/T-K2-*.png` (8). (`DilSecici.razor`'un "M" durumu bu paketten önceki değişikliktir.) Commit YOK.

## 13. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Marka kare+elmas, "Mihenk"; TR/EN tepeden kalkar, taşınır | SAĞLANDI | § 2, § 8, M1/M2 |
| (2) "Ekran ara" (PRO ⌘K); S1-j süzme aynen; üst etiket yok | SAĞLANDI (⌘K → "Ctrl K" gerekçeli) | S1j testleri yeşil, M8 |
| (3) "Tüm ekranlar (38)" | SAĞLANDI | § 8 |
| (4) PRO Favoriler yıldızlı | SAĞLANDI | § 8, M10 |
| (5) Açık/kapalı grup + önizleme, seçili vurgu | SAĞLANDI | § 8 |
| (6) Kart: rozet, ad, rol(+şube), dişli → /ayarlar; ek düğmeler kart menüsünde | SAĞLANDI | § 2, M3/M11/M15 |
| (7) Üst bar: başlık, kur şeridi, Yardım, hap; PRO EUR+kaynak; şube seçici karta; sekme yuvası | SAĞLANDI | § 2, M9/M12/M16/M17 |
| Tema noktaları eklenmez | SAĞLANDI | § 3 |
| Mevcut id'ler korunur | SAĞLANDI | tüm mevcut id'ler yerinde (#kenar-marka, #menu-arama-girdi, #kenar-alt-*, #kenar-sira/katlama-sifirla, #ust-sube-*, #ust-kayit-ara, #ust-bildirim-rozet…) |
| app.css / Sozlukler hub'ı dokunulmaz; sabit renk yok | SAĞLANDI | test + M14 |
| Gerçek tarayıcı: 1440/1280/375 × BASİT/PRO × açık/koyu, K1..K11 = 0 | SAĞLANDI | § 9 |
| Görsel karşılaştırma + bölge tablosu | SAĞLANDI | § 8 |
| Paralel kapı 7 madde (build · testlerim · denetim · mutasyon · canlı · kiracı silindi · yeni kırmızı yok) | SAĞLANDI | § 6, 7, 10, 11 |

## 14. Açık kalanlar (sahibiyle)
1. `Dogrulayici.Q102EkranSinirTestleri.DQ102_turkce_giris_1_5_kabul_1_500_belirsiz_red` — tam süitte kırmızı, tek başına 10/10 yeşil — **Q10-2 / Q10-A** (Hurda ekranı).
2. `KabukAramaEkranTestleri.Secilen_sube_kabuk_durumuna_yazilir_ve_arama_ucuna_gecer` — bir ara koşumda kırmızı, sonra yeşil; statik `KabukServisi.Ortak` paralel test paylaşımı — **T-KABUK (sonraki paket) / test altyapısı**; önerim: bu test sınıfını paralel olmayan koleksiyona almak.
3. Menü ikonları tuvalden farklı (ev, kredi kartı sembolleri sprite'ta yok) — `wwwroot/img/ikon.svg` sahibi; kurucu onayıyla sprite'a 2 sembol eklenebilir.
4. T-K3: Ayarlar sekmelerini `<UstBarSekmeleri>` ile üst bara koyacak (yuva hazır, testli).
5. Eşzamanlı şeritler aynı `obj/` klasörünü paylaştığı için mutasyon koşumlarında bayat DLL / dosya kilidi riski ölçüldü (§ 7 not) — orkestratöre: şerit başına ayrı `--artifacts-path` önerilir.

## 15. Kurucuya / mali müşavire notlar
Mevzuat içeren hüküm yok; mali müşavir teyidi gerekmez. Kurucuya: "⌘K" yerine "Ctrl K" gösteriliyor (her iki tuş çalışır); menü ikonları için sprite'a ev/kart sembolü eklenmesi isteniyorsa ayrı iş.
