# PAKET TOPLU-2 — Uygulama raporu (şerit t5 · 01.10.2026)

> TUR 2 (tasarım hakeminin 3 KRİTİK bulgusu) raporun SONUNDA: "# TUR 2".

Rol: Taşkın (backend) + Ertuğ (frontend). HASIMSIZ (yalnız görünüm). COMMIT YOK · dosya silme YOK · migration YOK.
Kaynak: `ham/TOPLU-TASARIM-KUYRUGU-2026-09-29.md` (tüm bölümler, 15:40 "demo videosu" eki dahil) + KESIF-P8 / AUTH-SIFIRLA /
TOPLU-1 / KESIF-P6 / KESIF-P4 raporlarının açık kalanları + `ham/KESIF-KARARLAR-2026-09-30.md`.
Yöntem: önce her maddeyi güncel ağaçta ÖLÇTÜM (başsız Chrome, `sikisma-olc.js`, test kiracısı `PAKETTOPLU-2-TEST`, API 5166, vekil 5167,
CDP 9501–9507). Kapanmış maddeleri kanıtla atladım; kalan görünüm maddelerini yaptım; para/veri/yasal davranışa dokunan maddeleri
"hasımlı pakete devir" diye ayırdım.

## 1. Madde tablosu (madde · önce ölçüm · sonra ölçüm · kanıt)

### 1a. Bu pakette DÜZELTİLEN (14)
| # | Madde (kaynak) | Önce | Sonra | Kanıt |
|---|---|---|---|---|
| T2-A | Kenar menüde iki öğe birden seçili (ZO-ISLEM-A, ZO-BELGE, ORTAK-1, 15:40 demo) | /raporlar/islem-defteri ve /raporlar/satis-analizi'de "Rapor merkezi" + sayfa öğesi birlikte vurgulu (s1 görüntüleri) | Her rotada tek `a.aktif`: "İşlem defteri", "Satış analizi", "POS alacakları" (son tur `hz`, 1440/1280/960/375 × BASİT/PRO) | `KenarCubugu.razor` `Eslesme()` + saf `DahaUzunOgeVar()`; menüde daha uzun eşleşen öğe varsa üst öğe tam eşleşir, menüde karşılığı olmayan alt rota (/siparisler/{Id}) üst öğeyi vurgulamaya devam eder |
| T2-B | /cikti basılı ekstre: "Sunucu sınırı…", "CARİ EKSTRESİ", taraf "Cari" (KESIF-P8 #2, karar 1) | s1: başlık "CARİ EKSTRESİ", taraf "Cari", "Sunucu sınırı: en fazla 200 hareket (sınıra değilmedi)"; K8 jargon 1 | Canlı JSON: `"belgeAdi":"CARİ HESAP EKSTRESİ"`, `"karsi":{"baslik":"Müşteri"…}` (TEDARIKCI carisinde "Tedarikçi"), `"not":"Bu belgede en fazla 200 hareket gösterilir; bu hesabın tüm hareketleri belgede."`; görüntüde K8 0 | `CiktiModulu.cs` (yalnız metin; satır tavanı ve kırpma davranışı DEĞİŞMEDİ) |
| T2-C | Kurulum PRO metninde iç kalem kodu "(K-24)" (KESIF-P8 #7) | `#kur-tl-kural` PRO "…kararı beklemede (K-24) olduğu için…" | "…kararı beklemede olduğu için…" (kural cümlesi aynı) | `Sozlukler.Toplu2.cs` `KurulumNegatifAcilisPro`; HUB değişmedi |
| T2-D | /siparisler durum süzgeci yerli `<select>` (KESIF-P8) | s1 görüntü: tarayıcı select'i | Ortak `Secici` (kimlik `sip-durum-suzgec` aynı); bUnit seçim → `GET /siparisler?durum=HAZIR` | `Siparisler.razor` |
| T2-E | 375 üst çubuk mod anahtarı sıkışık (KATMAN-1b, KESIF-P8) | 375: seçili "Basit" 44 px DAİRE, "Profesyonel" ~12 px | Hap biçimi, iki düğme `--yazi-sm`, yatay dolgu (sip-yeni/g-cariler 375 görüntüleri) | `ortak.css` (T2-1) |
| T2-F | /cariler 375 "Hepsi · Borçlu · Toptancı" anahtarı öğeleri sağa yığıyor (ölçümde bulundu) | s1 cariler 375: hapın solu boş | Üç öğe kabı eşit paylaşır (g-cariler 375 koyu görüntü) | `ortak.css` (T2-2) |
| T2-G | Veri içe aktar: şablon düğmesi katlanıra yapışık, "(CSV)" teknik eki (ZO-BELGE) | s1 1280: 0 px boşluk | 12 px boşluk, "Boş şablonu indir" (f-ice 960 görüntü) | `ortak.css` (T2-3), `VeriIceAktarim.razor`, `Toplu2Sozlugu.IceSablonIndir` |
| T2-H | Kenar çubuğu kalıcı hata bandı 4 satır, 13 px, uzun 2 cümle (KATMAN-2 YÜKSEK + K17) | "Grubun açık/kapalı durumu kaydedilemedi, geri alındı. Menünün kendisi yerinde duruyor." + metin "Kapat" düğmesi, sütun | "Menü düzeni kaydedilemedi." tek satır, `--yazi-sm`, × ikon düğme (`aria-label`/`title` "Kapat", kimlik `kenar-sira-hatasi-kapat` aynı) | `MenuSozlugu.cs`, `KenarCubugu.razor`, `ortak.css` (T2-5). ⚠ Canlı görüntü YOK: bant yalnız sunucu katlama tercihi yazamayınca çıkar; test kiracısında üretmek veriyi bozmak demek. Kanıt bUnit (`KATMAN2MenuTercihTestleri` 2 test tıklama + metin) + CSS testi |
| T2-I | `#urun-durum-filtre` / `#cari-tur-filtre` ezik 84 px (KESIF-P4, P8) | BASİT 1280: 84 px | BASİT: 120 px (`h-urunler`/`h-cariler` 1280/960). PRO'da UYGULANMADI: 10rem/7,5rem denemesi /urunler PRO 1280'de 6 süzgeçli satırı ikiye böldü (`yeniTop` 216 → 296, görüntü h-urunler PRO) → PRO doğal genişlikte bırakıldı | `ortak.css` (T2-6, yalnız `html[data-mod="basit"]`) |
| T2-J | Ürün listesi PRO 1280 DURUM sütunu eylem hücresinin altında (KESIF-P4) | tablo 988 > kap 962; K9 binen 6 ("Aktif" rozeti ⟂ "Stok") | tablo 962 = kap; K9 0; "Aktif" görünür (f-urunler PRO 1280 görüntü) | Kök neden ölçüldü: tek bağlayıcı en dar genişlik "ÜRÜN GRUBU" başlığı (`white-space:nowrap`); `max-width`/`width`/barkod yazısı DENENDİ, etkisiz (tablo hücresinde işlemez). Çözüm: PRO başlığı "Grup", tam ad `title`'da (`Urunler.razor` tek `<th>`; SUBE-KAPSAM-B'nin stok çağrısı bölgesine dokunulmadı) |
| T2-K | /vitrin "Kalem" başlığı ortada, değerler solda (KATMAN-1b) | th metni x=533, td x=318 | th `text-align:start`, ikisi x=318 | `ortak.css` (T2-8) |
| T2-L | Satış analizi BASİT'te "Bu kayıt değiştirilemez" kutusu (ORTAK-1 YÜKSEK) | s1 1280: amber kutu açılışta | Kapalı "Rapordaki bir rakam yanlışsa" katlanırında (Rapor merkezi `#rp-duzeltme`, İşlem defteri `#id-duzeltme` ile aynı kalıp); gerekçe SİLİNMEDİ | `SatisAnalizi.razor` |
| T2-M | Giriş ekranında ham istisna adı "(OturumDustuHatasi)" (ORTAK-1, KESIF-P8 #5) | `kisitHatasi = $"… ({ex.GetType().Name})"` | Yalnız sade metin; tür adı `Console.Error`'a | `Giris.razor` |
| T2-N | Şifre sıfırlamada kısa parolada kural cümlesi iki kez (AUTH-SIFIRLA) | ipucu + hata = aynı cümle 2 kez | Hata varken ipucu basılmaz; cümle 1 kez | `SifreSifirla.razor` (`Ipucu` koşullu) |

### 1b. Güncel ağaçta ZATEN KAPANMIŞ (ölçüm/kod kanıtıyla atlandı)
| Madde | Kanıt |
|---|---|
| POS: geri al sonrası aday listesi yenilenmiyor (ZO-ISLEM-A YÜKSEK) | `PosTahsilatlar.razor` `GeriAlUygula`: `adayAnahtari++` + `Yukle()` (KESIF-P1) |
| Banka 375 rozetler, iki birincil, iki '—' kutusu (ZO-ISLEM-A) | KESIF-P6 rapor § 11 (ölçüm + `BankaEkranTestleri.KESIFP6_Banka_iki_bos_alan…`) |
| Segment seçili öğe birincil mavi (DUGME-2 YÜKSEK) | `dugme.css` (3)/(3b): `--secili-dolgu` lacivert (KESIF-P7) |
| Siparişler 375 müşteri seçici taşması (DUGME-2) | sip-yeni 375 BASİT ölçüm: yatay 0, K3 0 |
| Faturalar ~70 px boş bant (DUGME-2) | s1 faturalar 1280 görüntü: düğmeler doğrudan başlık altında |
| Siparişler "Vazgeç" birincil (DUGME-2) | sip-yeni ölçüm `hz`: "Vazgeç", sınıf `dugme dugme-ikincil` |
| ia-gerekce / MASAK beyan kutusu monospace (ZO-BELGE, KESIF-P5) | TOPLU-1 `textarea { font-family: inherit }` (ortak.css) |
| Çek-senet (i) ipucu ekran dışına taşıyor (ZO-BELGE YÜKSEK) | evrak-ipucu 1280 ölçüm: kutu 800–1056 px < 1280 |
| İşlem defteri iç kaydırma (ZO-BELGE YÜKSEK) | s1 islem-defteri tam sayfa görüntü: 24 satır sayfa akışında, iç kaydırma yok |
| İşlem defteri "Karışık" (ZO-BELGE) | aynı görüntü: Ödeme sütunu "Kart"/"Nakit"/"Veresiye" |
| Çek-senet durum süzgeci bölünmüş (ZO-BELGE) | s1 evraklar görüntü: dört durum tek grupta |
| Ürün düzenleme çekmecesi 18 px kırılma (KATMAN-1b YÜKSEK) | u-duzenle BASİT/PRO görüntü: tek sütun, etiketler tek satır |
| Kasalar ⋯ menüsü (KATMAN-1b YÜKSEK) | kasa-menu ölçüm: Düzenle opaklık 1, devre dışı "Kullanımdan kaldır" saydam zemin, menü 352 px |
| /vezne şubesiz ham metin (ORTAK-1) | `VezneModulu.cs:787` metni sade ("Kasa kuyruğu için kullanıcıya şube atanmalı…") |
| SayfaBasi açıklama satırları / K17 (ORTAK-1) | `ortak.css` P7-K17 görsel gizleme |
| Müşteri Düzenle çekmecesi K16 (KESIF-P8) | cari-duzenle görüntü: adres/TC/e-posta/limit "Daha fazla" içinde kapalı |
| PRO üst çubuk iki satır (ZO-BELGE, KESIF-P8) — 1280'de | s1 banka/pos PRO 1280: tek satır (EUR dahil) |
| RFID "K-21"/"DIŞINDADIR", parite ham kodu (KESIF-P4) | `Sozlukler.cs:3981-3982`, `Sozlukler.KesifP4.cs` |
| /siparisler "Siparişi oluştur" biçimsiz (KEŞİF) | KESIF-P6 `Siparis_olustur_dugmesi_tasarim_sinifi` |
| /denetim-izi K9 (KESIF-P7 DÜŞÜK) | s1 denetim-izi 1280/375 × BASİT/PRO: K9 0 |
| Kur kaynağı onaysız / POS ölçek sunucu kapısı (KESIF-P1) | KESIF-P6 (Panel salt okunur; `OranDogrula` 400) |
| Satış·Havale "Kasa" +14 px (SATIS-3 kurucu) | KEŞİF 30.09: yeniden üretilemedi; s1 islemler 1280 görüntüde ödeme satırı hizalı |
| Fatura taslak toplam / kalem bandı / adet salt okunur (BELGE-KALAN, 08:50) | TOPLU-1 §1 #4-5, tur 2 `AdetSatistanGelir` |

### 1c. DEVİR — para/veri/yasal davranış ya da başka şeridin alanı (dokunulmadı)
| Madde | Neden | Sahip/öneri |
|---|---|---|
| Hurda/eski altında kimliğin iki kez istenmesi; ödeme varsayılanının eşik üstünde Nakit kalması; BASİT MASAK metinleri (SATIS-3 YÜKSEK/ORTA) | MASAK/tevsik yasal kapısı ve para davranışı | **hasımlı pakete devir** (SATIŞ + YASAL) |
| İade müşteri süzgeci, eski altın fiyatının kurdan dolması, Diğer formlarında ortak ürün arama, ürün aramasında fiyat '—', tür adları, kasa kalıbı (SATIS-3 ORTA) | işlem davranışı / fiyat | **hasımlı pakete devir** (SATIŞ şeridi) |
| Fatura kalem PATCH has birikimi, eski taslak geri doldurma (08:50) | para/veri | **hasımlı pakete devir** |
| Aynı kullanıcı 2 sekme jeton yarışı (KEŞİF) | güvenlik/işlev | **hasımlı pakete devir** (Mehmet) |
| Geçersiz girdi kapısı eksik ekranlar (08:50 sınıf) | doğrulama davranışı | GEÇERSİZ-GİRDİ paketi |
| Has/gram biçimi tutarsız: pozisyon kartı "g has" 1 hane, Panel "1875,7636", cari detay 4 ondalık (ZO-BELGE, ORTAK-1, KATMAN-1b) | Ekranlar arası tek kural (`Bicim.Has` → karar 5 "binlikli 4 hane") — onlarca ekran + test; tek ekranda yamamak tutarsızlığı artırır | BICIM-TEK paketi |
| Panel "Yasal liste henüz yüklenmedi" bandı; Kur "Deneme kur kaynağı" rozeti (15:40 demo YÜKSEK) | İkisi de yasal/sözleşme ifşası (fail-closed MASAK uyarısı, sözleşmesiz kaynak beyanı); gizlemek davranış kararıdır | **kurucu kararı + hasımlı paket** |
| Panel PRO ikinci hata bandı (KATMAN-2) | `Panel.razor` SUBE-KAPSAM-B'nin eşzamanlı dosyası | SUBE-KAPSAM-B bitince |
| Rapor merkezi "Gün sonu" kartı kalabalık (ZO-BELGE ORTA) | `Raporlar.razor` SUBE-KAPSAM-B alanı, rapor uçları RAPOR-YASLANDIRMA'da | rapor şeridi |
| KarZarar sayıları `ToString("N0")` (tarayıcı kültürü; "/fiyatlandirma ABD biçimi" kökü) | `KarZarar.razor` SUBE-KAPSAM-B alanı. `Fiyatlandirma.razor`'da kültürsüz `ToString` YOK (grep) | SUBE-KAPSAM-B sonrası |
| demo-tohumla.js 40 sn kur beklemesi (15:40) | ürün değil araç; bu turda da 404 alındı, `POST /kur/guncelle` ile aşıldı | orkestratör aracı |

### 1d. AÇIK KALAN görünüm maddeleri (ölçülemedi ya da bu turda yapılmadı — dürüst liste)
| Madde | Durum |
|---|---|
| /kurulum 375 uzun sayfa (KESIF-P8) | DENENDİ, GERİ ALINDI: adım adını 5rem tabana indirmek "Kullanıcıla·r ve roller" kırığı üretti (K3 kırık 3, f-kurulum 375 dört bileşimde). Kural kaldırıldı; g-kurulum 375 BASİT koyu + PRO açık yeniden ölçüldü: K3 0. Tasarım kararı (adım satırı ızgarası) gerekir |
| /cikti ekstre 375 kırpık (KESIF-P8) | Bilinçli iç kaydırma (keşif #51/#53 kararı, ortak.css); 5 sütun 285 px'e sığmıyor. Karar: kart düzeni mi, kaydırma mı |
| /cikti "Yazdır" koyu temada soluk (KESIF-P8) | f-cikti PRO koyu görüntü: koyu temanın birincil rengi (diğer ekranlarla aynı) — tasarım kararı |
| 375 genel arama katmanı serbest yüzen (KESIF-P8, DUGME-2) | açılmış arama katmanı ölçülmedi (KATMAN) |
| Döviz/Veresiye BASİT açıklama satırları (KESIF-P8) | ödeme türü değiştirilmiş satış formu ölçülmedi; dosya SATIŞ bileşenleri |
| Hurda "Daha fazla" açıcısı select gibi (ZO-ISLEM-A) | Değiştirilmedi: `zo-islem.css` kalıbı tuval Musteriler.dc.html "Daha fazla (…)" satırının birebir ölçüsü (DUGME-2'de tuvalden sapma "gerileme" sayıldı). Tuval ↔ hakem çelişkisi orkestratör kararı |
| Hurda kartı değer sırası, hurda defteri sayı biçimi, Faturalar No sütunu hizası, Kasa raporu geçmiş tekrar, /vezne 960 eylemler, /vezne ürün seçici 54 px, /gider 960 Tutar | Test kiracısında ilgili veri yok (hurda hareketi 0, fatura 0, tek kasa/tek gün, vezne fişi 0); boş listede ölçüm 0 ama bu KANIT DEĞİL — veri yazan ölçüm turu gerekir |
| Vitrin pano çekmecesi 12-13 px yardımcı yazılar; vitrin/kasalar BASİT açıklama satırları (KATMAN-1b) | yapılmadı |
| `#cekmece-cari` tek yazı boyu (tuval 15/16/17 ↔ katman 18) | orkestratör kararı bekliyor |
| Uyum/Etiketler metin yapışık, Bildirimler sekme biçimi, çekmecede alan altı mesaj (ORTAK-1 DÜŞÜK) | yapılmadı |
| /kullanicilar ve Müşteri çekmecesi BASİT K8 "e-posta" | jargon kataloğu "E-posta"yı sayıyor; Türkçe gündelik sözcük — katalog kararı |
| "Yeni X" cümle düzeni, `PasifKisa`, `SiparisDetay` AyarId, `Kur.KaynakAciklama` salt okunur kartta, kullanılmayan HUB anahtarları, Fiyat paneli "Gram" başlığı | HUB (P8) / orkestratör kararı |
| /islemler BASİT 1280 "Kasa seçin…" sarması | tek kasalı kiracıda üretilemedi |
| Şubesiz kullanıcıda küresel şerit teknik metni (ZO-ISLEM-A) | şubesiz kullanıcı fikstürü kurulmadı |

## 2. Sapmalar ve gerekçeleri
- **Ürünler PRO başlığı "Ürün grubu" → "Grup" (title'da tam ad):** CSS çözümleri ölçülerek elendi (bkz. T2-J). UX-S31 korunur ("Kategori" değil). `S33UrunlerEkranTestleri.S33_14` beklentisi buna göre uyarlandı (gerekçe testte yazılı; "Kategori" yasağı aynen duruyor).
- **`KurulumEkranTestleri.Q105_Profesyonel_modda_acilis_kurali_ayni_kalir`** artık `Toplu2Sozlugu.Tr.KurulumNegatifAcilisPro`'yu bekler ve "K-24" yokluğunu sınar (kural cümlesi aynı).
- **Süzgeç asgari genişliği yalnız BASİT** (PRO satır bölünmesi ölçüldü).
- **TEK DOKUNUŞ — dokunulan sayfalar:** `Giris.razor`, `SifreSifirla.razor`, `Siparisler.razor`, `Kurulum.razor`, `VeriIceAktarim.razor`, `SatisAnalizi.razor`, `Urunler.razor` (yalnız ürün grubu `<th>`; SUBE-KAPSAM-B'nin stok çağrısı bölgesi DEĞİL), `Layout/KenarCubugu.razor`. Panel/Kasalar/Hurda/KarZarar/Raporlar (SUBE-KAPSAM-B) ve RaporModulu (RAPOR-YASLANDIRMA) açılmadı.
- **Api dosyası:** `CiktiModulu.cs` yalnız 3 basılı metin (P8 "API metni sahipsiz → orkestratör" maddesi bu pakete verildi). Davranış/veri değişmedi.
- `Sozlukler.cs` HUB'a anahtar EKLENMEDİ; yeni metinler `Sozlukler.Toplu2.cs`.

## 3. Migration
Gerekmedi (şema değişmedi).

## 4. Testler
- Yeni: `Kuyumcu.Tests/Toplu2Testleri.cs` — **20 test durumu**: `Toplu2SafTestleri` 10 Fact + 1 Theory (7 satır) = 17; bUnit `Toplu2SiparisTestleri` 1, `Toplu2SifreSifirlaTestleri` 1, `Toplu2SatisAnaliziTestleri` 1.
- Uyarlanan: `KurulumEkranTestleri.Q105_Profesyonel…`, `S33UrunlerEtiketlerEkranTestleri` (S33_14) — gerekçeler §2.
- Filtreli son koşum (Toplu2 + Kurulum + Siparis + AuthSifirla + KATMAN2 + MenuKatlama + P37c + SatisAnalizi + S1eGiris + P17Cikti + S33Urunler): **261/261 geçti**.

## 5. Mutasyon tablosu (yedek `/private/tmp/kuyumcu-serit/mut-yedek/TOPLU-2/`, perl tek değişiklik → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| MT2-01 | `DahaUzunOgeVar` → `false &&` | `T2_Menu_tek_vurgu_en_uzun_eslesme` ×4 satır (/pos/tahsilatlar, /raporlar/islem-defteri, /raporlar/kasa, /urunler/agac) | AYNI |
| MT2-02 | mod anahtarı 375 dolgusu `var(--bosluk-1)` | `T2_Mod_anahtari_375_okunur_boy_ve_yatay_dolgu` | AYNI |
| MT2-03 | segment öğesi `flex: 0 0 auto` | `T2_Musteri_segmenti_dar_ekranda_esit_pay` | AYNI |
| MT2-04 | içe aktar boşluğu `margin-top: 0` | `T2_Ice_aktar_sablon_dugmesi_katlanira_yapismaz` | AYNI |
| MT2-06 | hata bandı `flex-direction: column` | `T2_Kenar_hata_bandi_tek_satir_kisa_cumle` | AYNI |
| MT2-06b | `MenuSozlugu.KatlamaKaydedilemedi` eski uzun metin | `T2_Kenar_hata_bandi_tek_satir_kisa_cumle` | AYNI |
| MT2-07 | süzgeç `min-width: 0` (son kural üzerinde yeniden koşuldu) | `T2_Suzgec_secicileri_asgari_genislik` | AYNI |
| MT2-08 | vitrin th `text-align: center` | `T2_Vitrin_kalem_basligi_basa_hizali` | AYNI |
| MT2-09 | `BelgeAdi` "CARİ EKSTRESİ" | `T2_Basili_ekstre_resmi_ad_ve_taraf_etiketi` | AYNI |
| MT2-10 | `CariTarafEtiketi` MUSTERI → "Cari" | `T2_Basili_ekstre_resmi_ad_ve_taraf_etiketi` | AYNI |
| MT2-11 | `Giris.razor` tür adı geri | `T2_Giris_kisit_hatasi_istisna_adi_tasimaz` | AYNI |
| MT2-12 | `Siparisler.DurumSecildi` → `durum = null` | `Toplu2SiparisTestleri.T2_Durum_suzgeci_Secici_secim_sunucuya_durum_ile_gider` | AYNI |
| MT2-13 | `SifreSifirla` ipucu koşulsuz | `Toplu2SifreSifirlaTestleri.T2_Kisa_parolada_kural_cumlesi_bir_kez_yazilir` | AYNI |
| MT2-14 | `sa-duzeltme` `BaslangictaAcik="true"` | `Toplu2SatisAnaliziTestleri.T2_Satis_analizi_gerekce_kapali_katlanirda_acilinca_gorunur` | AYNI |
| MT2-15 | Urunler başlığı `UrunGrubu` | `S33UrunlerEkranTestleri.S33_14_Profesyonel_modda_grup_sutunu_AD_basar_ham_kod_basmaz` | AYNI |
| MT2-16 | Kurulum PRO HUB metni | `KurulumEkranTestleri.Q105_Profesyonel_modda_acilis_kurali_ayni_kalir` | AYNI |
| MT2-17 | VeriIceAktarim HUB "(CSV)" metni | `T2_Toplu2_metinlerinde_ic_kod_yok` | AYNI |

Anayasa md.1 notları: (1) MT2-15 ilk koşumda GEÇTİ — derleme paralel şeridin dosya kilidiyle bayat dll üretmişti (MT2-16'nın ilk derlemesi de MSB4018 verdi); ikisi tek başına derlenerek yeniden koşuldu, sonuçlar yukarıda (kırmızı). (2) MT2-05 (kurulum kuralı) koşuldu ve kırmızıydı, fakat kural ölçümde K3 kırık ürettiği için kod + testiyle birlikte geri alındı → tabloda yok. Son mutasyondan sonra TEMİZ `dotnet build Kuyumcu.sln`: 0 uyarı, 0 hata.

## 6. denetim.sh
`bash denetim.sh` (son kodla): 303 ✓, tek ✗ **P-30 DEMO_INSAN_SURESI** (kapsam dışı, kabulde hariç). Bu paketin sayaçlarında ihlal yok (`/private/tmp/kuyumcu-serit/TOPLU-2/denetim2.txt`).

## 7. Canlı doğrulama (API 5166 · vekil 5167)
Kiracı `PAKETTOPLU-2-TEST Kuyumculuk` (01M3VP82XWZ6F7SNQAJGNGMV6Z), `demo-tohumla.js --prova-kiraci` + `--eposta` (6 cari, 12 ürün, 9 satış).
| İstek | Beklenen | Gerçek |
|---|---|---|
| GET /api/v1/cikti/CARI_EKSTRE/{Tuna Akarsu} | 200, resmî ad, taraf "Müşteri", sade sınır notu | 200 · `belgeAdi` "CARİ HESAP EKSTRESİ" · `karsi.baslik` "Müşteri" · `not` "Bu belgede en fazla 200 hareket gösterilir; bu hesabın tüm hareketleri belgede." |
| GET /api/v1/cikti/CARI_EKSTRE/{Anadolu Döküm} | 200, taraf "Tedarikçi" | 200 · "Tedarikçi" |
SQL: `cariler` → Tuna Akarsu = MUSTERI, Anadolu Döküm = TEDARIKCI; `hareketler` Tuna Akarsu için 2 satır = ekstrede 2 satır.
Temizlik: API ve vekil PID'leri durduruldu (5166/5167 dinleyici yok); `zsh kiraci-temizle.sh 01M3VP82… --uygula` → kiracı 0, cariler 0, hareketler 0. tenants: başlangıç 12 → son 13; fark `PAKETSUBE-KAPSAM-B-TEST` (SUBE-KAPSAM-B şeridinin, bu paket açmadı). Fikstür/anahtar dosyaları `printf ''` ile boşaltıldı.

## 8. Gerçek tarayıcı ölçümü (başsız Chrome, cdp.mjs + sikisma-olc.js)
Toplam **308 ölçüm** (`olcum.jsonl`, görüntüler `/private/tmp/kuyumcu-serit/TOPLU-2/foto/`):
- Önce turu (s1): 34 rota × 1280/375 × BASİT/PRO açık = 136.
- Etkileşim turu (s-tur2): ürün/cari çekmecesi, kasa ⋯, çek-senet (i), yeni sipariş, kasa raporu, gider, gider pusulası, vezne, satış × 1280/375 × BASİT açık + PRO koyu = 40.
- Son tur (s-son, son kodla): 11 rota × {1440,1280,960,375 BASİT açık; 1440,1280,960,375 PRO açık; 1280,375 BASİT koyu; 1280,375 PRO koyu} = 132.
| Rota | Bileşim | K1 | K3 | K5 TRY | K7 | K8 | K9 | yatay |
|---|---|---|---|---|---|---|---|---|
| /cariler, /siparisler, /urunler, /veri-ice-aktarim, /raporlar/satis-analizi, /raporlar/islem-defteri, /pos/tahsilatlar, /cikti ekstre, /gider, /vezne | 12 bileşim × 10 rota | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| /kurulum | 12 bileşim (son turda geri alınan kural AÇIKKEN) | 0 | 0/0 (1440-960) · **0/3 (375)** | 0 | 0 | 0 | 0 | 0 |
| /kurulum (kural geri alındıktan sonra) | 375 BASİT koyu · 375 PRO açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| /urunler (süzgeç kuralı son hali) | PRO açık 1280 · BASİT koyu 1280/960 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
Dar sütun: ölçülen ekranlarda en geniş blok ana alanın tamamı (kart/tablo %100); dar sütun yok.
Read ile bakılan görüntüler: s1_hurda BASİT 1280/375, s1_evraklar, s1_veri_ice_aktarim, s1_raporlar, s1_faturalar, s1_siparisler, s1_vitrin (tam), s1_raporlar_islem_defteri (tam), s1_cikti 1280/375, s1_kurulum 375, s1_cariler 375, s1_urunler BASİT 1280 + PRO 1280, s1_banka PRO, s1_pos PRO, s1_fiyatlandirma, s1_satis_analizi, u-duzenle BASİT açık + PRO koyu, cari-duzenle, islemler BASİT 1280, sip-yeni BASİT 375, f-kurulum BASİT 375, g-cariler BASİT koyu 375, f-cikti PRO koyu 1280, f-urunler PRO 1280, f-ice BASİT 960, h-urunler PRO 1280 + BASİT koyu 960.

## 9. Tam süit (son kodla, `KUYUMCU_TEST_DB=kuyumcu_test_t5`, trx `/private/tmp/kuyumcu-serit/trx-pTOPLU-2-son/`)
**8920 toplam · 8916 geçti · 2 başarısız · 2 atlandı**:
- `SozlesmeKapsamTestleri.Kapsam_orani_beyan_edilen_degerle_ayni` (58≠59) ve `Kilitsiz_tip_listesi_circir_ile_donduruldu` ("Mutabakat" yeni istemci tipi) — `Kuyumcu.Contracts`'a yeni tip ekleyen şeridin (RAPOR-YASLANDIRMA / SUBE-KAPSAM-B); TOPLU-2 Contracts'a dokunmadı.
İlk tam süitte (ara kod) ayrıca RAPORYASLANDIRMA ×6 (RAPOR-YASLANDIRMA, o anda yarım), `GorunumSubesiEkranTestleri.K14` (SUBE-KAPSAM-B), `PosBarkodTestleri.Fiyat_SUNUCU…` (yük altında zaman aşımı; tek başına ve son süitte GEÇTİ) kırmızıydı — hiçbiri bu paketin dosyası değil.
→ **Bu paketin yeni kırmızısı: 0.**
Not: paralel şeritler derlemeyi iki kez kırdı (RaporModulu.cs `CariYaslandirmaKurali`, KabukServisi `AramaSubesi`); her seferinde bekleyip yeniden derlendi, son derleme 0/0.

## 10. DoD sözleşmesi
Yeni varlık/kayıt türü YOK, yeni uç YOK. Değişen tek sunucu çıktısı basılı belge metni (salt okunur belge; düzeltme yolu belgedeki DegismezIpucu'da: kaynak kayıt). Yeni düğme: kenar hata bandının × düğmesi (`@onclick=Tercih.HatayiKapat`, istek atmaz ama yerel durumu kapatır — KATMAN-2 testleri tıklamayı sınar; denetim "@onclick'siz düğme" sayacı değişmedi).

## 11. Değişen dosyalar (TOPLU-2'nin)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Toplu2.cs` (yeni) · `dotnet/Kuyumcu.Core/I18n/MenuSozlugu.cs`
- `dotnet/Kuyumcu.Api/Modules/Cikti/CiktiModulu.cs`
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `dotnet/Kuyumcu.Web/Pages/Giris.razor`, `SifreSifirla.razor`, `Siparisler.razor`, `Kurulum.razor`, `VeriIceAktarim.razor`, `SatisAnalizi.razor`, `Urunler.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/ortak.css` (sona "PAKET TOPLU-2" bölümü; TOPLU-1 kuralları değişmedi)
- `dotnet/Kuyumcu.Tests/Toplu2Testleri.cs` (yeni), `KurulumEkranTestleri.cs`, `S33UrunlerEtiketlerEkranTestleri.cs`
- bu rapor

## 12. Koşul karşılama
| Koşul | Durum |
|---|---|
| Önce ölç, kapanmışı kanıtla atla | KARŞILANDI (§1b) |
| Para/veri/yasal davranışa dokunma | KARŞILANDI (§1c devir) |
| build 0/0 | KARŞILANDI (son derleme 0 uyarı 0 hata) |
| Her değişiklik için test + mutasyon | KARŞILANDI (17 mutasyon, hepsi kırmızı + cmp AYNI) |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI |
| Tam süit 0 yeni kırmızı | KARŞILANDI |
| Başsız Chrome 1440/1280/960/375 × BASİT/PRO × açık/koyu + Read | KARŞILANDI (değişen ekranlar; koyu 1280/375); kenar hata bandı canlı üretilemedi (bUnit kanıtı) |
| Canlı API + SQL | KARŞILANDI (§7) |
| Test kiracısı silindi | KARŞILANDI |
| Kaynaklardaki TÜM açık görünüm maddeleri | **KISMEN** — §1d'deki maddeler yapılmadı/ölçülemedi |

## 13. Kurucu / mali müşavir
Mevzuat hükmü yazılmadı/değiştirilmedi. Kurucu kararı gereken iki demo maddesi (§1c): Panel "Yasal liste henüz yüklenmedi" bandı ve Kur "Deneme kur kaynağı" rozeti — ikisi de yasal/sözleşme ifşası; gizleme kararı kurucunun.

---

# TUR 2 (01.10.2026, 17:00–18:10) — tasarım hakeminin üç KRİTİK bulgusu + kalan görünüm maddeleri

Ölçüm/araçlar: `/private/tmp/kuyumcu-serit/TOPLU-2/t2/` (API 5166, vekil 5167, CDP 9511–9515, test kiracısı
`PAKETTOPLU-2-TEST T2 Kuyumculuk` 01M3VWY8CBW8CSS89GF30KX2GY, `demo-tohumla.js --prova-kiraci`: 6 cari, 12 ürün, 9 satış).
Para/veri/yasal davranışa dokunulmadı; sunucu kodu bu turda DEĞİŞMEDİ. Migration yok.

## T2.1 Madde tablosu (madde · önce · sonra · kanıt)
| # | Madde | Önce ölçüm | Sonra ölçüm | Kanıt |
|---|---|---|---|---|
| K1 | Döviz Veresiye BASİT'te geliştirici notları + "isteğe bağlı" ↔ "Kaydet kilitli" çelişkisi (K18); Döviz Tahsilat aynı çelişki + "USD kasası yok" büyük boş durum formun ortasında | Hakem görüntüleri (Q_/P_doviz_veresiye): 3 not koşulsuz (`Cariler.razor:893/896/897`), "bu sürümde YOKTUR", dv-tutar "Efektif Miktar (isteğe bağlı)" + "Kaydet kilitli: Efektif Miktar" | BASİT: not YOK; PRO: iki not sade dille (Toplu2Sozlugu `DovizFisBacagiNot`, `DovizVadeNot`; "YOKTUR" yok). Tutar alanı ZORUNLU (`*`, aria-required), adı "Tutar (USD)", kilit sebebi aynı ad. Tahsilatta kasa satırı para biriminin hemen altında `SaltOkunur #dt-kasa-yok` ("USD kasası yok — önce USD kasası açın."), boş-durum kutusu yok. 16 bileşim × 2 çekmece: K1/K3/K5/K7/K8/K9/K10/yatay = 0 | `foto/dv__*`, `foto/dt__*` (Read: dv BASİT açık 1440, dv PRO açık 1440, dv BASİT koyu 375, dt BASİT açık 375) |
| K2 | PRO üst çubuk 1440'ta iki satır (K14) | `ub.mjs once`: PRO 1440'ta 24 rotanın 14'ü **93 px** (kur verisi + saat + "· Altınkaynak" gelince); PRO 1280 ve BASİT 68 | `ub.mjs sonra`: 24 rota × PRO/BASİT × **1440/1280/1600 = 68 px** (144 ölçüm); ölçüm matrisinde /raporlar/islem-defteri 1440/1280 dört bileşimde 68 | `ortak.css (T2-9)`: ≥1440 PRO'da kaynak adı saatin title'ına iner (1280 kuralıyla aynı; silinmez), aralıklar 18/14. Görüntü `foto/ub-sonra-PRO-1440_raporlar_islem-defteri.png` (Read: tek satır, saat görünür) |
| K3 | Cari ekstresi: "Belge no" ham kayıt kimliği 375'te 6 satır; teknik açıklama satırı; "değiştirilemez" kutusu belgenin içinde; tablo belgede yatay kayıyor (sw 529/cw 285) | Hakem C_ekstre_BASIT_karanlik_375 | Ekstrede "Ekstre tarihi 01.10.2026" (#cikti-no, tek satır 19 px); diğer belgelerde kayıt kimliği `belge-no-kimlik` (tek satır 14 px, küçük, soluk); teknik satır KALDIRILDI; düzenleyen-eksik uyarısı belgenin DIŞINDA, önizlemenin üstünde (#cikti-kimlik-uyari); 375'te hareket tablosu dikey kart (sw=cw=285, 960'ta 578=578, 1440'ta 734=734). 16 bileşim × (ekstre, fiş): tüm K = 0 | `foto/ekstre__*`, `foto/fis__*` (Read: ekstre BASİT açık 375, PRO koyu 375) |
| D1 | Bildirimler sekme seçimi başka ekranlardan farklı (kuyruk DÜŞÜK) | `bildirimler__BASIT_aydinlik_1280` (önce): ayrı gri gölgeli düğmeler | Ortak `.sekmeler-serit/.sekmeler-sekme` hapı, seçili lacivert; role=tab + aria-selected; id'ler aynı | `Bildirimler.razor`, `ortak.css (T2-13)`; Read: bildirimler BASİT açık 1280, PRO koyu 960 |
| D2 | (ölçümde bulundu) Bildirimler PRO 960 "Son çalışma" tarih-saati iki satıra kırılıyor | PRO açık/koyu 960: K3 kırık **6** | 16 bileşim K3 0/0 | `Bildirimler.razor` td `hucre-tek-satir` (+ id `is-son-calisma-{Kod}`) |
| D3 | Uyum risk sekmesi açıklama satırları karta yapışık (kuyruk DÜŞÜK) | margin 0 (önce görüntü) | üst 12 px, alt 12 px (probe: kart 937 → açıklama 949; kapalı-not 983 → ızgara 995) | `ortak.css (T2-14)` |
| D4 | Etiketler "En çok 500 kayıt…" tablo başlığına yapışık; kartın iç boşluğu yok (arama kutusu kart kenarında) | önce görüntü | 16 px kart dolgusu, sınır notu 12/8 px aralı | `ortak.css (T2-15)` |

Kapanmış sayılan önceki §1d maddeleri: "/cikti ekstre 375 kırpık" (K3), "Döviz/Veresiye BASİT açıklama satırları" (K1), "PRO üst çubuk iki satır" (K2), "Bildirimler sekme biçimi" (D1), "Uyum/Etiketler metin yapışık" (D3/D4).

## T2.2 Sapmalar
- Ekstrede belge no yerine ekstre tarihi İSTEMCİDE seçilir (`A4Belge.EkstreMi`); sunucu sözleşmesi (`BelgeNo` = cari kimliği, `BelgeNoKayitKimligi`) değişmedi — sunucuya dokunmamak için. Canlı: `GET /cikti/CARI_EKSTRE/01M3VWZ7DAWA6VSDBK48QHEWR8` 200, `belgeNo`=cari kimliği, ekran "01.10.2026" basar.
- `Q94` iki testin beklentisi değişti (teknik açıklama satırı artık YOK; numara sınıfı `belge-no-kimlik`) — tasarım hakeminin kararı; testlerde gerekçe yazılı.
- BASİT'te `VeresiyeIpucu`/`TahsilatIpucu` da gizlendi (hakem "üç not"); PRO'da duruyor. Gerçek saklanmıyor: PRO'da iki sade not.
- Tahsilatta kasa satırı tutardan ÖNCEYE alındı (UX-S24: para birimine bağlı alan onun altında).
- 375'te kart düzeninde başlık satırı `display:none` (görsel-gizli 1 px başlıklar ölçerde "binen" sayılıyordu); değer adları hücre etiketinde (`data-etiket`, `::before`).
- DOKUNULMAYAN: Vitrin pano çekmecesi ipucu 13 px (ortak `.alan-ipucu` kademesi — tüm formları etkileyen tasarım sistemi kararı), Uyum "SİYASİ NÜFUZ SAHİBİ…" 3 satır (yasal terimin kısaltılması uyum sahibinin kararı).
- TEK DOKUNUŞ — bu turda açılan sayfalar: `Cariler.razor` (yalnız döviz çekmeceleri + `DovizKilitSebebi`), `Cikti.razor`, `Bildirimler.razor`, `Bilesenler/Yazdirma/A4Belge.razor`. Tur 1'in sayfaları yeniden açılmadı.

## T2.3 Testler
Yeni (Toplu2Testleri.cs): `Toplu2Tur2CssTestleri` 2, `Toplu2Tur2KuyrukTestleri` 2, `Toplu2CiktiTestleri` 3 (bUnit), `Toplu2DovizCekmeceTestleri` 3 (bUnit) = **10**. Uyarlanan: `Q94CiktiBelgeTestleri` 2.
Filtreli: Toplu2 + Bildirim 115/115; Toplu2 + Cari + Q94 + P17 + KesifSozluk + TK1 + UX1C3 + B1 195/195.

## T2.4 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/TOPLU-2-t2/`, hepsi cmp AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| MT2-18 | BASİT döviz notları koşulsuz (`@if (true)`) | T2_BASIT_doviz_veresiyede_gelistirici_notu_yok_tutar_zorunlu_sade_ad |
| MT2-19 | dv-tutar `Zorunlu="false"` | T2_BASIT_…sade_ad · T2_PRO_doviz_veresiye_notlari_sade_dil_bu_surumde_YOKTUR_basilmaz |
| MT2-20 | kilit sebebi eski "Efektif Miktar" | T2_BASIT_…sade_ad |
| MT2-21 | kasa-yok satırı → BosDurum | T2_Doviz_tahsilatta_kasa_yok_satiri_bos_durum_kutusu_degil_tutar_zorunlu |
| MT2-22 | DovizVadeNot "bu sürümde YOKTUR" | T2_PRO_doviz_veresiye_notlari_sade_dil_bu_surumde_YOKTUR_basilmaz |
| MT2-23 | ≥1440 PRO kaynak `display: inline` | T2_PRO_ust_cubuk_1440_kaynak_adi_ipucuna_iner_araliklar_daralir |
| MT2-24 | `EkstreMi => false` | T2_Ekstrede_belge_no_yerine_ekstre_tarihi_kimlik_basilmaz |
| MT2-25 | numara sınıfı hep `kir-uzun` | T2_Satis_fisinde_kayit_kimligi…, Q94_belge_no_kayit_kimligi…, Q94_belge_basligi_tek_satir… |
| MT2-26 | kimlik uyarısı `if (false)` | T2_Duzenleyen_eksik_uyarisi_belgenin_DISINDA_onizlemenin_ustunde |
| MT2-27 | kart hücresi `display: table-cell` | T2_Belge_kayit_kimligi_tek_satir_ve_375_kart_duzeni |
| MT2-28 | `data-etiket` kaldırıldı | T2_Ekstrede_belge_no_yerine… |
| MT2-29 | teknik açıklama satırı geri | Q94_belge_no_kayit_kimligi… · T2_Ekstrede_belge_no_yerine… |
| MT2-30 | kimlik `white-space: normal` | T2_Belge_kayit_kimligi_tek_satir_ve_375_kart_duzeni |
| MT2-31 | Bildirimler eski `secili/ikincil` sınıfı | T2_Bildirimler_sekmeleri_ortak_sekme_hapi_ailesinde |
| MT2-32 | `#et-sinir` margin 0 | T2_Uyum_risk_ve_Etiketler_metin_satirlari_karta_yapismaz |
| MT2-33 | `#uy-risk-aciklama` margin-top 0 | T2_Uyum_risk_ve_Etiketler… |
| MT2-34 | bildirim sekmesi köşe `--yaricap-kontrol` | T2_Bildirimler_sekmeleri… |
| MT2-35 | Etiketler kart dolgusu 0 | T2_Uyum_risk_ve_Etiketler… |
| MT2-36 | son çalışma hücresi `hucre-tek-satir` yok | T2_Bildirimler_sekmeleri… |
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 hata 0 uyarı (`t2/b5.log`). Tam derlemede görülen 2 uyarı başka şeritlerin dosyalarında: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs` CS8601, `KesifP2Testleri.cs` xUnit2029.
Anayasa md.1 notu: ilk ölçüm matrisi mutasyon derlemeleriyle eşzamanlı koştuğu için (WASM dll'i sayfa yüklemesinde okunur) GEÇERSİZ sayıldı; temiz derlemeden sonra matris BAŞTAN koşuldu (aşağıdaki sayılar ikinci koşumdur).

## T2.5 Gerçek tarayıcı (başsız Chrome; `t2/olcum.jsonl`, görüntüler `t2/foto/`)
Rota × {1440,1280,960,375} × {BASİT,PRO} × {açık,koyu} = 16 bileşim: dv (Döviz Veresiye çekmecesi), dt (Döviz Tahsilat), /cikti/CARI_EKSTRE, /cikti/SATIS_FISI, /raporlar/islem-defteri, /uyum?sekme=risk, /urunler/etiketler, /bildirimler → **128 + 16 (bildirimler son hali) ölçüm; K1 0 · K3 0/0 · K5 (TRY) 0 · K7 0 · K8 0 · K9 0 · K10 0 · yatay 0** (tek istisna bildirimler PRO 960 K3 kırık 6 → D2 ile düzeltildi, yeniden ölçüm 16/16 temiz). Çekmece K9'u yalnız çekmecenin içinde ölçüldü (arka sayfa görüntüden SONRA gizlenip ölçülür; arka plan ↔ çekmece kesişimi binme değildir). Üst çubuk: 24 rota tablosu § T2.1 K2. Dar sütun: belgeler ana alanın tamamı; çekmeceler kendi genişliği.
Read ile bakılanlar: ub-once/ub-sonra PRO 1440 islem-defteri, dv BASİT açık 1440, dv PRO açık 1440, dv BASİT koyu 375, dt BASİT açık 375, ekstre BASİT açık 375, ekstre PRO koyu 375, uyum-risk/etiketler/bildirimler BASİT açık 1280 (önce ve sonra), bildirimler PRO koyu 960.

## T2.6 Tam süit (son kodla, `KUYUMCU_TEST_DB=kuyumcu_test_t5`, trx `/private/tmp/kuyumcu-serit/trx-pTOPLU-2-t2/`)
**8965 toplam · 8962 geçti · 1 başarısız · 2 atlandı.** Kırmızı: `PosBarkodTestleri…SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` (Islemler.razor; WaitFor zaman aşımı) — tek başına 3/3 GEÇTİ; tur 1'de de aynı dosya yük altında kararsızdı; bu paketin dosyası değil. Önceki (aynı kodla, D2 öncesi) koşumda ayrıca `GorunumSubesiEkranTestleri.Emanet_cari_secilince…subeId` kırmızıydı (SUBE-KAPSAM-B şeridinin yarım işi). **Bu paketin yeni kırmızısı: 0.**

## T2.7 denetim.sh
302 ✓. ✗ P-30 DEMO_INSAN_SURESI (kapsam dışı). Son koşumda ayrıca ✗ **P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > 47**: bu paketin değil — 17:2x koşumunda (Cariler değişikliği dahil) 47 idi; sonrasında TOPLU-2 dosyalarında catch bloğu değişmedi (Cariler.razor mutasyon yedeğiyle cmp AYNI, Bildirimler/Cikti/A4Belge sayacı 0); artış o arada değişen SUBE-KAPSAM-B dosyalarından (Hurda/Urunler/Raporlar/KasaRaporu.razor 17:43–17:48). Bu paketin sayaçları ✓ (ilk koşumdaki "@onclick'siz düğme 72>71" Cikti.razor'da tek satıra alınarak giderildi).

## T2.8 Canlı doğrulama
| İstek | Beklenen | Gerçek |
|---|---|---|
| GET /api/v1/cikti/CARI_EKSTRE/{Tuna Akarsu} | 200, sözleşme değişmedi | 200 · tur CARI_EKSTRE · belgeAdi "CARİ HESAP EKSTRESİ" · belgeNo = cari kimliği · belgeNoKayitKimligi true · karşı "Müşteri / Tuna Akarsu" · 2 satır · düzenleyen adres/vergi no boş (→ ekranda uyarı belgenin dışında) |
SQL: `hareketler` Tuna Akarsu 2 satır (= ekstre 2 satır); `cariler` Tuna Akarsu MUSTERI; `kasalar` yalnız TRY 1 (→ canlı "USD kasası yok" satırı ölçüldü).
Temizlik: API/vekil durduruldu (5166/5167 dinleyici yok); `zsh kiraci-temizle.sh 01M3VWY8… --uygula` → tenants'ta 0, cariler 0, hareketler 0. tenants: başlangıç 13 → son 12 (fark: `PAKET-RAPOR-YASLANDIRMA-TEST` o şeridin kendi temizliği; bu paket açtığı kiracıyı sildi). fik/anahtar/jeton dosyaları `printf ''` ile boşaltıldı.

## T2.9 DoD sözleşmesi
Yeni varlık/uç YOK. Yeni düğme yok (Bildirimler sekmeleri mevcut düğmeler, @onclick aynı; Cikti "İşletme kartından doldurun" taşındı, @onclick aynı).

## T2.10 Değişen dosyalar (tur 2)
`dotnet/Kuyumcu.Core/I18n/Sozlukler.Toplu2.cs` · `dotnet/Kuyumcu.Web/Pages/Cariler.razor` · `Pages/Cikti.razor` · `Pages/Bildirimler.razor` · `Bilesenler/Yazdirma/A4Belge.razor` · `wwwroot/css/ortak.css` (TOPLU-2 bölümünün sonu, T2-9…T2-15) · `Kuyumcu.Tests/Toplu2Testleri.cs` · `Kuyumcu.Tests/Q94CiktiBelgeTestleri.cs` · bu rapor.

## T2.11 Açık kalanlar (tur 2 sonrası)
- Vitrin pano çekmecesi yardımcı yazı 13 px / başlık ölçüsü — ortak ipucu kademesi kararı (orkestratör/tasarım).
- Uyum "SİYASİ NÜFUZ SAHİBİ OLDUĞUNU BEYAN EDEN MÜŞTERİ" 3 satır — yasal terim kısaltması uyum/kurucu kararı.
- /cikti "Yazdır" koyu temada (tema birincil rengi), /kurulum 375 uzun sayfa, 375 arama katmanı, Hurda "Daha fazla" tuval çelişkisi, veri gerektiren ölçümler (hurda/fatura/vezne), HUB metin maddeleri, şubesiz fikstür — tur 1 §1d'deki gerekçelerle AÇIK.
- §1c devirleri aynen (hasımlı paketler, SUBE-KAPSAM-B, rapor şeridi, BICIM-TEK).
- Başkasına ait kırmızılar: `GorunumSubesiEkranTestleri.Emanet_cari_secilince…` (SUBE-KAPSAM-B), `PosBarkodTestleri.SATISTAN_ALISA…` (kararsız, satış şeridi), denetim P-11 48>47 (SUBE-KAPSAM-B), P-30 (kapsam dışı).

---

# TUR 3 (01.10.2026, 19:40–20:45) — önceki doğrulamanın KRİTİK bulgusu: süzgeç seçicisi açılınca sayfa zıplıyor

Araçlar: `/private/tmp/kuyumcu-serit/TOPLU-2/t3/` (API 5166, vekil 5167, CDP 9510–9519; test kiracısı
`PAKETTOPLU-2-TEST T3 Kuyumculuk` 01M3W61Q63AGC8WG0T526Y0GB3, `demo-tohumla.js --prova-kiraci`: 6 cari, 12 ürün, 9 satış).
Ölçüm sürücüsü `t3/acilir.mjs`: her senaryoda tetiği görünür yapar, KAPALI durumda tetik/şerit komşuları/sayfa başı/sayfa başından
sonraki blok/ilk tablo kutularını ölçer, tetiğe tıklar, AÇIK durumda aynılarını ölçer, ekran görüntüsü alır, `sikisma-olc.js`'i hem
kapalı hem açık durumda (açıkta ayrıca panel gizlenerek = "panel dışı binme") koşar. Sunucu kodu, para/veri/yasal davranış DEĞİŞMEDİ.
Migration yok. Bu turda yalnız `ortak.css` (T2-16) ve `Toplu2Testleri.cs` değişti; razor dosyası açılmadı.

## T3.1 Madde tablosu
| Madde | Önce ölçüm (başsız Chrome) | Sonra ölçüm | Kanıt |
|---|---|---|---|
| KRİTİK: süzgeç seçicisi açılınca tetik 320 px'e genişliyor, şerit kırılıyor, tablo itiliyor | BASİT açık 1280: `#urun-durum-filtre` tetik **120 → 320 px**, tablo **+199 px**; `#cari-tur-filtre` 120 → 320, tablo **+331**; `#sip-durum-suzgec` 143,3 → 320, liste **+375**. 375: tetik 351 (değişmez) ama tablo **+211 / +355 / +388** (`t3/acilir.jsonl` etiket `once`, görüntü `foto/once_urunler__BASIT_aydinlik_1280.png` — Read: panel tabloyu aşağı itmiş, araya boş bant) | 3 rota × {1440,1280,960,375} × {BASİT,PRO} × {açık,koyu} = **48 ölçüm**: tetik genişlik farkı **0**, tetik y farkı **0**, tablo y farkı **0**, sayfa başından sonraki blok y farkı **0**, şerit komşularının x/y/w farkı **0**, yatay kaydırma 0, K1 0, K3 0/0, K5 0, panel dışı K9 = kapalı K9 = 0; panel tetiğe bağlı (alt kenarın 4 px altında; `position: absolute`, kırpan atası olan BASİT 960 /urunler'de mevcut `.sabit` yolu ile `fixed`) | `ortak.css (T2-16)`; Read: `sonra_urunler__BASIT_aydinlik_1280` (`dene_` ile aynı), `sonra_urunler__BASIT_karanlik_960`, `sonra_urunler__PRO_aydinlik_960` (sarmış ikinci satırın üstünde katman), `sonra_cariler__BASIT_aydinlik_375`, `sonra_siparisler__PRO_karanlik_1440` |
| Gerileme yok: Secici kullanan DİĞER (form içi) ekranlar | BASİT açık 1280/375 önce: 6 ekranda panel `absolute`/`fixed`, aralık 4, komşu alan y farkı 0 | 6 ekran (`/islemler #islem-cari`, `/gider #gd-kategori`, `/emanet #e-cari`, Yeni ürün çekmecesi `#u-tip`, Yeni sipariş `#sip-f-tur`, Kasa hareketi çekmecesi `#h-tur`) × 16 bileşim = **96 ölçüm**: tetik genişlik/y farkı 0, komşu alan y farkı 0, yatay 0, K1/K5 0, K3 0/0, panel dışı K9 = kapalı K9 (Yeni ürün 6, Kasa hareketi 3 — kapalı durumda da aynı: çekmece ↔ arka sayfa kesişimi, önceki turdaki gerekçe; açılış bir binme EKLEMİYOR) | Read: `sonra_kasa-hareket__BASIT_aydinlik_375` |

Toplam bu turda **144 "sonra" + 6+16 "önce" + 6 deneme** ölçüm (`t3/acilir.jsonl`, `t3/son-*.log`).
"Açık" durumdaki K9 (3–6) panelin altındaki tablo/form yazısıyla kesişimidir: katman BİLEREK içeriğin üstündedir (açılır listenin
tanımı); bu yüzden ölçüt "panel dışı K9 ≤ kapalı K9" olarak alındı ve 144/144 sağlandı.

Tarayıcı düzeyinde mutasyon (MT2-37-tarayıcı): T2-16 bloğunda `position: absolute` → `relative` yapılıp /urunler BASİT 1280
yeniden ölçüldü → tetik **120 → 320**, tablo **+195 px** (zıplama geri geldi); yedek geri kopyalandı, `cmp` AYNI.

## T3.2 Sapma ve gerekçe (Anayasa md.1 — açıkça)
- **P-37c kararının tersine çevrilmesi.** app.css'teki P-37c kuralı (`.sayfa-basi-filtre .secici-liste-kap { position: relative }`)
  paneli bilerek AKIŞTA tutuyordu ("şerit sarınca panel alt satırın üstüne biner"). Doğrulama bulgusu bunun bedelini ölçtü (tablo
  200–375 px itiliyor) ve mutlak katman istedi. Uygulanan: ortak.css T2-16, şeritte paneli akış dışı/tetiğe bağlı yapar;
  açıkken hizayı kapalıyla aynı tutar (P-37c'nin `:has(...) { align-items: flex-start }` kuralları artık bir şey korumuyor,
  yalnız farklı boylu komşuları kaydırırdı). P-37c'nin kaygısı: panel açıkken `.secici-perde` (z 40) zaten tüm sayfayı kaplar;
  alttaki kontrol o anda tıklanamaz — her açılır listede olduğu gibi — dışarı tıklama paneli kapatır. Görüntü
  `sonra_urunler__PRO_aydinlik_960`: panel sarmış "Kategori/Reyon" satırının üstünde, okunur ve tıklanabilir.
- **app.css P-37c kuralı ve onun koruyucuları DEĞİŞTİRİLMEDİ** (dosya alanım dışında: `P37cPanelVeKapsamTestleri` 5 test
  app.css'i okur, `denetim.sh` `PANEL_AKIS_DISI` sayacı app.css bloğunu greple sınar). Sonuç: o sayaç hâlâ ✓ basıyor ve metni
  "(panel akışta · kendi sütun genişliğinde)" diyor — **bu metin artık ekrandaki davranışı anlatmıyor**, çünkü ortak.css T2-16
  aynı öğede o kuralı geçersiz kılıyor. Sayacı/testi değiştirmek kapı aracına dokunmak olacağından yapmadım; **orkestratöre
  devir:** `PANEL_AKIS_DISI` sayacı ve P37c'nin 4 testi yeni geometriye (şeritte panel mutlak + tetik genişliği sabit + açık hiza =
  kapalı hiza) göre yeniden yazılmalı ya da app.css kuralı T2-16 ile birleştirilip P-37c kuralı emekliye ayrılmalı.
- `.musteri-sade` (tuval yüzü /cariler) için ayrı hiza kuralı YAZILMADI: orada şeritteki tek seçici `#cari-tur-filtre`'dir ve o
  varken kapalı hiza da `flex-end`'dir (ortak.css P7-K7a, `:has(#cari-tur-filtre)`); ölçüm: /cariler 16 bileşim komşu farkı 0.
- T2-6'daki BASİT süzgeç asgari genişliği (7,5rem) korunur; artık açılınca 320 px'e büyümediği için tetik kapalı/açık AYNI.

## T3.3 Testler (Toplu2Testleri.cs, +4)
- `Toplu2Tur3SeciciCssTestleri.T3_Serit_secici_listesi_akis_disi_tetige_bagli_mutlak_katman` — absolute, `top: calc(100% + var(--bosluk-1))`,
  `left: 0`, `margin-top: 0`, kuralda min-width YOK; `.yukari` / `.sola` karşılıkları.
- `…T3_Kirpan_ata_sabit_yerlesimi_ezilmez_ve_kural_app_cssten_sonra_yuklenir` — şerit panel kurallarının hepsi `:not(.sabit)`;
  index.html'de ortak.css app.css ve katman.css'ten sonra; 20rem yalnız liste kuralında (app.css (2)).
- `…T3_Panel_acikken_hiza_kapali_ile_ayni_alttan` — iki `:has` hiza geçersiz kılması `flex-end`.
- `Toplu2Tur3SeciciDomTestleri.T3_Siparis_durum_suzgeci_listesi_tetikle_ayni_kapta_seritte_dogar_tetik_yerinde` (bUnit) — gerçek
  Siparisler DOM'unda liste tetikle aynı `.secici-kap`'ta ve `.sayfa-basi .sayfa-basi-filtre` içinde doğar (CSS seçicisi eşleşir),
  tetik/panelde satır içi style yok.
- Filtreli koşum (Toplu2 + P37c + Siparis + Secici): **336/336 geçti** (`t3/f1.log`).

## T3.4 Mutasyonlar (yedek `/private/tmp/kuyumcu-serit/mut-yedek/TOPLU-2-t3/ortak.css`, perl tek değişiklik → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| MT2-37 | T2-16 `position: absolute` → `relative` | T3_Serit_secici_listesi_akis_disi_tetige_bagli_mutlak_katman | AYNI |
| MT2-37-tarayıcı | aynı mutasyon, canlı sayfa | /urunler BASİT 1280: tetik 120→320, tablo +195 px (ölçüm `mut37`) | AYNI |
| MT2-38 | ana kuraldan `:not(.sabit)` silindi | T3_Kirpan_ata_sabit… · T3_Serit_secici… | AYNI |
| MT2-39 | `.sayfa-basi:has(…)` hizası `flex-start` | T3_Panel_acikken_hiza_kapali_ile_ayni_alttan | AYNI |
| MT2-40 | `margin-top: 0` → `var(--bosluk-1)` | T3_Serit_secici… | AYNI |
| MT2-41 | `.filtre-yuvasi:has(…)` hizası `flex-start` | T3_Panel_acikken_hiza… | AYNI |
| MT2-42 | `.yukari` panelde `bottom: auto` | T3_Serit_secici… | AYNI |
bUnit DOM testi için mutasyon KOŞULMADI: kıracak değişiklik `Secici.razor`'da (SUBE-KAPSAM-B'nin eşzamanlı dosyası, "yalnız ad
değişimi") — paralel şeridin derlemesini bozmamak için dokunulmadı; test destekleyici kanıttır, kritik kural CSS testleriyle + tarayıcı
mutasyonuyla korunur.

## T3.5 Derleme
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln --nologo`: **0 Uyarı, 0 Hata** (`t3/b-son.log`). ortak.css mutasyon yedeğiyle `cmp` AYNI.

## T3.6 Tam süit (son kodla, `KUYUMCU_TEST_DB=kuyumcu_test_t5`, trx `/private/tmp/kuyumcu-serit/trx-pTOPLU-2-t3/`)
**9041 toplam · 9030 geçti · 9 başarısız · 2 atlandı.** Kırmızıların hiçbiri bu paketin dosyası değil; TOPLU-2'nin yeni kırmızısı **0**:
- `DugmeIkiTestleri.D2_01_index_dugme_cssi_olcek_cssten_SONRA_ortak_cssten_ONCE_yukler`, `OlcekTekTestleri.OL5_index_olcek_cssi_ortak_cssten_HEMEN_ONCE_yukler`
  — `wwwroot/index.html` 20:00'de başka şeritçe değişti (dugme.css ile ortak.css arasına `css/toplu3.css` ve `css/sube-kapsam-b.css` eklendi;
  TOPLU-3 / SUBE-KAPSAM-B). Aynı kök denetimdeki `DUGME2_BAGLANTI_SIRASI` ✗. TOPLU-2 index.html'e dokunmadı.
- `ParaDemoDbTestleri.PD1_*` ×3, `ParaDemoHttpTestleri.PD1_HTTP_onizleme_ve_gun_kapat_kabul_senaryosu` — PARA-DEMO şeridi.
- `SozlesmeKapsamTestleri.Kapsam_orani_beyan_edilen_degerle_ayni`, `…Kilitsiz_tip_listesi_circir_ile_donduruldu` — Kuyumcu.Contracts'a tip ekleyen şerit (tur 1'den beri).
- `UX4dEkranTestleri.M24_Sozlesmesiz_kaynak_seciliyken_PILOT_uyarisi_gorunur` — Kur ekranı (Kur.razor 17:48 sonrası başka şeritçe değişti); TOPLU-2 Kur'a dokunmadı.
`P37cPanelVeKapsamTestleri` (5) ve `Toplu2*` (tümü) tam süitte YEŞİL.

## T3.7 denetim.sh (son kodla, test kiracısı silindikten sonra, `t3/denetim2.txt`)
301 ✓, 3 ✗ — hiçbiri TOPLU-2'nin değil:
- P-30 DEMO_INSAN_SURESI (kapsam dışı, kabulde hariç).
- P-23 ODEME_KALEMSIZ_YOL 1 — canlı DB ölçümü; satır `PAKETPARA-DEMO-TEST Kuyumculuk` kiracısının TAHSILAT/NAKIT hareketi (SQL ile doğrulandı). PARA-DEMO şeridi.
- DUGME-2 DUGME2_BAGLANTI_SIRASI 1 — yukarıdaki index.html sırası (başka şerit).
İlk koşumda (`t3/denetim.txt`) görülen "@onclick'siz düğme 72 > 71" ikinci koşumda ✓ (başka şeridin dosyası düzeldi); TOPLU-2'nin razor
dosyaları bu turda açılmadı ve 17:48'deki ✓71 koşumundan beri değişmedi (Cariler.razor'daki fark PARA-DEMO'nun; ölü düğme sayısı 7 = 7).
⚠ `P-37c: PANEL_AKIS_DISI` ✓ basıyor ama metni artık davranışı anlatmıyor — bkz. T3.2, orkestratöre devir.

## T3.8 Canlı doğrulama (5166, yalnız görünüm paketi — sözleşme değişmedi)
| İstek | Beklenen | Gerçek |
|---|---|---|
| POST /api/v1/auth/giris | 200 | 200 |
| GET /api/v1/siparisler | 200, 0 sipariş | 200 · 0 kayıt · toplam 0 |
| GET /api/v1/siparisler?durum=HAZIR | 200 (süzgecin gönderdiği sorgu) | 200 · 0 · toplam 0 |
| GET /api/v1/cari | 200, SQL ile aynı | 200 · 7 kayıt |
| GET /api/v1/urunler | 200, SQL ile aynı | 200 · 12 kayıt |
SQL (kuyumcu_net, kiracı 01M3W61Q…): cariler 7, urunler 12, siparisler 0 — yanıtlarla aynı.
Temizlik: API ve vekil PID'leri `kill` ile durduruldu (5166/5167 dinleyici yok); `zsh kiraci-temizle.sh 01M3W61Q63AGC8WG0T526Y0GB3 --uygula` rc 0 →
tenants'ta 0, cariler 0, hareketler 0. **tenants: başlangıç 15 → son 15.** fik.json ve sistem anahtarı dosyası `printf ''` ile boşaltıldı,
tohum raporundaki e-posta "<silindi>" yapıldı. 5199/5200'e dokunulmadı.

## T3.9 DoD sözleşmesi
Yeni varlık/uç/düğme YOK. Değişiklik yalnız CSS yerleşimi (açılır liste katmanı).

## T3.10 Değişen dosyalar (tur 3)
- `dotnet/Kuyumcu.Web/wwwroot/css/ortak.css` — sona "TOPLU-2 tur 3" bölümü (T2-16); önceki kurallar değişmedi.
- `dotnet/Kuyumcu.Tests/Toplu2Testleri.cs` — `Toplu2Tur3SeciciCssTestleri` (3) + `Toplu2Tur3SeciciDomTestleri` (1).
- bu rapor.
TEK DOKUNUŞ: bu turda razor sayfası açılmadı.

## T3.11 Koşul karşılama (tur 3)
| Koşul | Durum | Kanıt |
|---|---|---|
| KRİTİK: açık/kapalı tetik genişliği ve tablo y aynı (±1 px), 1440/1280/960/375 × BASİT/PRO × açık/koyu | KARŞILANDI | 48/48 ölçüm, tüm farklar 0 (T3.1) |
| Secici kullanan diğer ekranlarda gerileme yok (≥6 ekran) | KARŞILANDI | 6 ekran × 16 bileşim = 96 ölçüm, farklar 0 |
| bUnit/CSS testi + mutasyon | KARŞILANDI | 4 test; 6 kod mutasyonu + 1 tarayıcı mutasyonu, hepsi kırmızı/zıplama, cmp AYNI |
| build 0/0 | KARŞILANDI | `t3/b-son.log` |
| denetim TEMİZ (P-30 hariç) | KENDİ SAYAÇLARIM ✓; ağaç 2 ✗ başka şeritlerin (P-23 PARA-DEMO verisi, DUGME2 index.html) | T3.7 |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 9 kırmızı, sahipleriyle T3.6 |
| Gerçek başsız Chrome + Read ile görüntü | KARŞILANDI | T3.1 (Read edilen 8 görüntü) |
| Canlı API + SQL | KARŞILANDI | T3.8 |
| Test kiracısı silindi, tenants başlangıç değeri | KARŞILANDI | 15 → 15 |
| Para/veri/yasal davranışa dokunma | KARŞILANDI | yalnız CSS + test |

## T3.12 Açık kalanlar (tur 3 sonrası)
- **Orkestratöre devir:** `denetim.sh PANEL_AKIS_DISI` sayacı + `P37cPanelVeKapsamTestleri`'nin 4 geometri testi app.css'teki eski (akış içi)
  kuralı koruyor; ekrandaki davranış artık ortak.css T2-16'dır. Sayaç/test yeni geometriye göre yeniden yazılmalı ya da app.css kuralı emekliye ayrılmalı.
- Gözlem (ölçüm sırasında görüldü, bu paketin kapsamı dışı, dokunulmadı): /urunler 960 BASİT/PRO'da satır eylemleri ("Stok · Düzenle · ⋯")
  ürün adının altına ikinci satıra iniyor (`sonra_urunler__*_960` görüntüleri) — SUBE-KAPSAM-B'nin Urunler.razor alanı bitince ayrı madde.
- Tur 1 §1c/§1d ve tur 2 T2.11'deki açık kalanlar aynen geçerli (hasımlı devirler, kurucu kararları, veri gerektiren ölçümler).
- Başkasına ait kırmızılar: T3.6 listesi (index.html sırası ×2 + DUGME2, PARA-DEMO ×4 + P-23, SozlesmeKapsam ×2, UX4d M24), P-30.
