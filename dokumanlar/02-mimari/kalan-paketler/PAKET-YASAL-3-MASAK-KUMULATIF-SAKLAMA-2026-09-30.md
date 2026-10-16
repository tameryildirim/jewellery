# PAKET YASAL-3 — MASAK: kümülatifte brüt değer · kimliksiz art arda fiş · saklama otomatiği · yaptırım taraması · eşik teyidi

> Tarih: 2026-09-30 · Hazırlayan: Tamer Bey koordinasyonu (şartname ajanı; KOD YAZILMADI) · Durum: ŞARTNAME — ekip oyu bekliyor
> Kapattığı çelişkiler (MIHENK-PLAN-RAPORU §3a): **(4)** saklama anonimleştirmesi elle, risk skorlama kapalı, yaptırım taraması yok · **(5)** kümülatifte takasta yalnız fark, hurdada stopaj sonrası net, Peşin Müşteri art arda fiş kümülatifi yok · **(8)** MASAK eşiği (S-2) ve 23/e birincil metni teyitsiz (23/e kısmı YASAL-4'te).
> Önkoşul: **YASAL-1** (tarihli eşik) · **ŞUBE-KAPSAM ŞK-A** (`MasakKapisi.GunlukIslemToplam` şubeden bağımsız).

---

## 1. Amaç

Kurucu önceliği: **"Pusula kadar sade + devletin tarif ettiği her kural otomatik ve görünür."**

- Kimlik eşiği **el değiştiren gerçek değerle** ölçülsün: takasta fark değil büyük bacak, hurdada stopaj sonrası net değil brüt bedel — tek işlemde olduğu gibi **kümülatifte de**.
- Kimliksiz (Peşin Müşteri) art arda fişlerle eşik bölünmesin: program tek soru sorsun ("Aynı müşteri mi?"), cevap kayda geçsin.
- 8 yılı dolan kimlik verisi **kendiliğinden** maskelensin; patron 30 gün önceden haber alsın, gerekirse gerekçeyle bekletsin.
- Yaptırım (malvarlığı dondurma) listesindeki kişiyle işlem **yapılamasın**; eşleşme tezgâhta tek cümleyle durdursun.
- Eşiğin kendisi (185.000 TL) birincil kaynakla tabloya bağlansın (YASAL-1); teyitsiz kademeler ekranda "teyit bekliyor" diye dursun.

---

## 2. Mevcut durum (kod kanıtı)

### 2.1 Kümülatif ölçüm açıkları (çelişki 5)
| Kanıt | Ne gösteriyor |
|---|---|
| `Modules/Masak/MasakKapisi.cs:24-26` | "TAKAS TUZAĞI: kapıya farkTl DEĞİL Max(eskiDeger, yeniDeger) verilir" — tek işlemde doğru. |
| `Modules/Satis/SatisModulu.cs:2309` / `:2353` | `masakTutar = Math.Max(eskiDeger, yeniDeger)` ama deftere `TlTutari = farkTl` yazılıyor. |
| `MasakKapisi.cs:137-142` | "BİLİNEN EKSİK ÖLÇÜM — kümülatif deftere yazılan TL bacağından okunur: TAKAS farkTl… HURDA_ALIS stopaj SONRASI net". |
| `MasakKapisi.cs:228-234` | Kümülatif `SUM(ABS(h.TlTutari))` — yani takas 1,8 M el değiştirse bile ertesi işlemin kümülatifine 10.000 girer. |
| `SatisModulu.cs:1222` | `TlTutari = Yuvarla2(-odenecek) // stopaj sonrası NET`. |
| `MasakKapisi.cs:156-167`, `:182-193` | Peşin Müşteri kümülatife girmez; tek işlem eşiğe ulaşırsa ret; "aynı kişi aynı gün bu kartla eşik ALTI birden çok işlem yaparsa bağlantılı işlem toplamı ölçülmez". |
| `MasakKapisi.cs:143-153` | EVRAK_GIRIS kümülatife girmez (ölçüm doğruluğu kararı, çift sayım önlemi) — bu paket DEĞİŞTİRMEZ. |
| PAKET-SUBE-KAPSAM § 2.6 | Şubeye kilitli kullanıcıda kümülatif şubeye bölünüyor (çıkarım). |

### 2.2 Saklama (çelişki 4a)
| Kanıt | Ne gösteriyor |
|---|---|
| `Modules/Masak/SaklamaSuresiIsi.cs:21-23` | "ELLE TETİKLENİR. Otomatik zamanlayıcı BİLİNÇLİ olarak eklenmedi". |
| `Modules/Masak/MasakModulu.cs:367-381` | `GET /masak/saklama-onizleme`, `POST /masak/saklama-uygula` — yalnız SAHİP. |
| `Modules/Isler/Isler/SaklamaOnizlemeIsi.cs:22` | `SAKLAMA_ONIZLEME` işi yalnız **sayı** bildiriyor, uygulamıyor. |
| `Modules/Masak/MasakKuralMotoru.cs:197` | `SaklamaBitisi(t) => t.AddYears(SaklamaYili)` — **her kaydın kendi tarihinden** 8 yıl. Müşteriyle daha sonraki işlemler süreyi uzatmıyor (bkz. § 3 md.46). |
| `dokumanlar/02-mimari/adr/0008-saklama-suresi-maskeleme.md` | "Kuyumcu işi hiç tetiklemezse süresi dolmuş kişisel veri sistemde kalır… kapanmış bir madde değildir". |

### 2.3 Risk ve yaptırım (çelişki 4b-c)
| Kanıt | Ne gösteriyor |
|---|---|
| `appsettings.json:67-80` `MasakRisk.Etkin=false`, `YuksekRiskliUlkeler: []` | Skor hesaplanıyor, hiçbir akışı değiştirmiyor; ülke listesi boş. |
| `Modules/Masak/RiskAyarlari.cs:13-15` | "Risk skoru HİÇBİR koşulda satış/MASAK/tevsik kapısına bağlanmaz". |
| `Modules/Masak/RiskMotoru.cs:46` | `YaptirimEslesmesiVar` — "FAZ B girdisi. Faz A'da HER ZAMAN false verilir." |
| `grep -rni yaptirim Modules` | Yalnız puan sabiti ve sebep kodu; **liste, tablo, tarama yok**. |
| `PAKET-40-masak-risk-skorlama-pep-beyani-yaptirim-listesi-.md` | Faz B (yaptırım) açılmamış; kurucu kalemi "yaptırım listesi veri bedeli" (plan raporu §3a). |

### 2.4 Eşik teyidi (çelişki 8)
| Kanıt | Ne gösteriyor |
|---|---|
| `MasakKuralMotoru.cs:24-31` | 185.000 / 200.000 / 2.000.000,01 / 20.000.000,01 — dördü tek sınıfta, dayanak farkı ayrılmamış. |
| `mevzuat-gereksinimleri.md:35`, `:40` | M-01 ✅ (185.000), M-06 ❌ (2 M / 20 M "kaynak doğrulanamadı"). 200.000 "amaç beyanı" için satır YOK. |
| Plan raporu §3a | "eşik ikincil kaynaktan (S-2 teyidi)". |

---

## 3. Mevzuat

> Doğrulama: 30.09.2026, birincil metinler okunarak. Tedbirler Yönetmeliği konsolide metni: https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf

### 3.1 Doğrulanan hükümler
| # | Hüküm | Birincil kaynak | Alıntı (≤15 kelime) |
|---|---|---|---|
| M-1 | Kuyumcu MASAK yükümlüsüdür | Tedbirler Yön. md.4/1-k | "Kıymetli maden, taş veya mücevher alım satımı yapanlar ile bu işlemlere aracılık edenler." |
| M-2 | **Kimlik eşiği 185.000 TL**, tek işlem **ya da bağlantılı işlemler toplamı** (çelişki 8 / S-2) | Tedbirler Yön. md.5/1-b; rakam 6702 s. CK, RG 14.01.2023/32073 (konsolide dipnot) | "İşlem tutarı ya da birbiriyle bağlantılı birden fazla işlemin toplam tutarı" · "yüzseksenbeşbin TL … veya üzerinde olduğunda" |
| M-3 | "Bağlantılı işlem" md.3'te **tanımlı değil**; en yakın hüküm md.32/2 (devamlı bilgi verme) | Tedbirler Yön. md.32/2 | "Mahiyeti itibarıyla birbiriyle bağlantılı işlemler tek bir işlem olarak değerlendirilir." |
| M-4 | Şüpheli işlem bildirimi 10 iş günü | Tedbirler Yön. md.28/2 | "şüphenin oluştuğu tarihten itibaren en geç on iş günü içinde Başkanlığa bildirilir" |
| M-5 | **Saklama 8 yıl, SON İŞLEM tarihinden** | Tedbirler Yön. md.46 | "son işlem tarihinden itibaren sekiz yıl süre ile muhafaza etmek" |
| M-6 | Uyum programı yükümlüleri arasında kuyumcu **yok** (risk temelli yaklaşım kuyumcuya bağlayıcı yazılamaz) | Uyum Programı Yönetmeliği md.4 · https://mevzuat.gov.tr/File/GeneratePdf?mevzuatNo=12426&mevzuatTur=KurumVeKurulusYonetmeligi&mevzuatTertip=5 | Liste bankalar…"g) … Kıymetli madenler aracı kuruluşları," — perakende kuyumcu (md.4/1-k) sayılmıyor |
| M-7 | KVKK: periyodik imha aralığı en çok 6 ay | Kişisel Verilerin Silinmesi, Yok Edilmesi veya Anonim Hale Getirilmesi Hakkında Yönetmelik md.11/2 (RG 28.10.2017) · https://www.resmigazete.gov.tr/eskiler/2017/10/20171028-10.htm | "Bu süre her halde altı ayı geçemez." |
| M-8 | KVKK: işleme şartları kalkınca resen silme/anonimleştirme | Aynı Yönetmelik md.7 · aynı URL | "resen veya ilgili kişinin talebi üzerine silinmesi, yok edilmesi veya anonim hâle getirilmesi gerekir" |
| M-9 | Malvarlığı dondurma kararına aykırı işlem **hükümsüz** | 6415 s. K. md.10 · https://www.mevzuat.gov.tr/mevzuatmetin/1.5.6415.pdf | "Malvarlığının dondurulması kararına aykırı olarak yapılan her türlü tasarruf ve işlem hükümsüzdür." |
| M-10 | Karar RG'de yayımla sonuç doğurur; alacak/borcu olanlar **30 gün** içinde bildirir | 6415 md.12/1, 12/3 · aynı URL | "Resmî Gazete'de yayımı ile birlikte hukuki sonuçlarını doğurur" · "en geç otuz gün içinde Başkanlığa bildirirler" |
| M-11 | Kitle imha silahları kararları gecikmeksizin uygulanır; listeler internet sitesinde yayımlanır | 7262 s. K. md.3/1, 3/3 · https://www.mevzuat.gov.tr/mevzuatmetin/1.5.7262.pdf | "gecikmeksizin uygulanır" · "internet sitelerinde gecikmeksizin yayımlanır" |
| M-12 | **YENİ (23.09.2026):** yükümlüler, listelenen kişilerin istediği işlemleri **ivedilikle MASAK'a bildirme ve erteleme** tedbirlerini almakla yükümlü | MASAK Genel Tebliği Sıra No 34, md.5 (yayımı tarihinde yürürlükte) · https://www.mevzuat.gov.tr/File/GeneratePdf?mevzuatNo=46302&mevzuatTur=Teblig&mevzuatTertip=5 | "işlemlerin ivedilikle Başkanlığa bildirilmesine ve ertelenmesine yönelik gerekli tedbirleri almakla yükümlüdürler" |
| M-13 | Talep üzerine 7 gün içinde bildirim | MASAK Genel Tebliği Sıra No 12 §2.1 · https://masak.hmb.gov.tr/masak-genel-tebligi-sira-no-12/ | "yedi gün içinde" |
| M-14 | Resmî dondurma listeleri ve arama sayfası (veri kaynağı) | MASAK · https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar | (liste sayfası; 6415 md.5/6/7 ve 7262 bölümleri) |

**Ürüne etkileri:**
- M-2 → S-2 sorusunun **rakam** kısmı kapandı (185.000 TL doğrulandı). "Bağlantılı işlem"in **aynı gün / aynı kişi** diye okunması ise yorumdur (M-3: tanım yok) → teyit (§ 3.2 T-1).
- M-5 → bugünkü `SaklamaBitisi(t) = t + 8 yıl` (`MasakKuralMotoru.cs:197`) **erken maskeleyebilir**; § 4.3'teki "son işlem" düzeltmesi zorunludur.
- M-7/M-8 → ADR-0008'in "elle tetiklenir" kararı KVKK'nın **resen** imha ve **6 ay** periyot hükmüyle gerilimde; § 4.3 otomatiği bu yüzden gerekir.
- M-6 → risk skoru **bağlayıcı yapılmaz** (`RiskAyarlari.cs:13-15` kararı birincil kaynakla desteklendi); görünür bildirim yeterlidir.
- M-9…M-12 → yaptırım taraması **yükümlülük** (M-12 kuyumcu dahil tüm md.4 yükümlülerine). Eşleşmede doğru davranış: **işlemi yapma (ertele) + MASAK'a ivedilikle bildir**. Mevcut müşteride eşleşme + açık bakiye/emanet → **30 gün** içinde bildirim (M-10).

### 3.2 Teyit edilemeyenler
| # | Konu | Durum |
|---|---|---|
| T-1 | "Bağlantılı işlem"in pencere tanımı (aynı gün mü, aynı kişi mi, başka ölçüt mü) — yönetmelikte tanım yok (M-3) | **DOĞRULANAMADI — mali müşavir/MASAK rehberi teyidi.** Ürün bugünkü "aynı yerel gün + aynı cari" okumasını korur |
| T-2 | Nakit İşlem Beyan Formu (2 M / 20 M): HMB'de bulunan tebliğ metni (https://ms.hmb.gov.tr/uploads/sites/12/2025/08/SAK-Genel-Teblig-SIRA-NO-31-6203d6d5cd7cf747.pdf) md.4/4 yükümlülüğü **finansal kuruluşlara** veriyor ("ikimilyonbir TL ile yirmimilyon TL (bu tutarlar dahil) arası nakit işlemlerde"); kuyumcu tanım dışı. RG'deki nihai metin ve sıra numarası | **DOĞRULANAMADI** — kuyumcuya doğrudan yükümlülük bulunamadı; ürün kademeleri `TEYITSIZ` tutar |
| T-3 | 200.000 TL "işlem amacı beyanı" eşiği | **DOĞRULANAMADI** — yönetmelikte bulunamadı |
| T-4 | Peşin Müşteri art arda fiş penceresi (60 dk, aynı kullanıcı) | Mevzuat hükmü DEĞİL — politika/sinyal; birincil dayanak aranmaz, "yasal" etiketi taşımaz |
| T-5 | Takasta MASAK ölçüsünün Max(eski, yeni) olması (el değiştiren brüt değer) | Yönetmelikte takas özel hükmü yok — tutucu yorum; mali müşavir S-1 ile birlikte |
| T-6 | 6702 s. CK'nın RG orijinal metni | Konsolide dipnottan DOĞRULANDI; orijinal RG sayfası ayrıca açılmadı |

---

## 4. Kapsam

### 4.1 Kümülatifte brüt ölçü (çelişki 5a-b)
- **Veri modeli:** `hareketler.masak_olcu_tl DECIMAL(15,2) NULL` (YENİ kolon). Anlamı: "bu satırda el değiştiren değerin TL karşılığı" — MASAK ölçüsü.
  - TAKAS: `Max(eskiDeger, yeniDeger)` (`SatisModulu.cs:2309`'daki aynı değişken; ikinci formül yazılmaz).
  - HURDA_ALIS: gider pusulasının `BrutTutar`'ı (stopaj öncesi).
  - Diğer türler: NULL (TlTutari zaten doğru ölçü).
- **Kümülatif:** `GunlukIslemToplam` iki ayrık sorgu desenini korur (`MasakKapisi.cs:205-213` A5 kararı): (1) `masak_olcu_tl IS NOT NULL` → `SUM(masak_olcu_tl)`, (2) `IS NULL` → `SUM(ABS(tl_tutari))`. Bileşik COALESCE yazılmaz.
- **Geçmiş satırlar:** migration ile yalnız **kanıtı olan** satırlar geri doldurulur: HURDA_ALIS ↔ `gider_pusulalari.brut_tutar` (hareket_id eşleşmesi); TAKAS satırı için brüt değer defterde yoksa **NULL kalır** (uydurulmaz), rapora "geçmiş takas ölçüsü bilinmiyor: N satır" yazılır.
- `MasakKapisi.cs:137-142` "BİLİNEN EKSİK ÖLÇÜM" notu takas/hurda maddelerinden arındırılır (yalnız EVRAK_GIRIS kalır).
- **Tevsik etkilenmez:** tevsik nakit payı (`nakit_tutar`) ölçer; bu kolon yalnız MASAK içindir.

### 4.2 Kimliksiz art arda fiş (çelişki 5c)
- **Kural (sinyal, mevzuat yorumu DEĞİL):** Peşin Müşteri kartıyla yapılan işlemde, **aynı kullanıcı + aynı şube**, son `MASAK_PESIN_PENCERE_DK` (appsettings politika ayarı, öneri 60 dk) içindeki Peşin Müşteri işlemleri + bu işlem ≥ `MASAK_KIMLIK_ESIGI_TL(bugün)` ise sunucu **tek soru** ister: `ayniKisiBeyani: bool?`.
  - Gövdede yoksa → 400 `Kod=PESIN_ART_ARDA_SORU` (ekran soruyu gösterir).
  - `true` → "Gerçek müşteri seçin" (mevcut K2 yolu; kimlik formu açılır).
  - `false` → işlem geçer + `masak_beyanlari`'na değil, `denetim_kayitlari`'na `PESIN_ART_ARDA_FARKLI_KISI` (kullanıcı, tutar, pencere toplamı; kişisel veri yok) + risk listesinde "eşik bölme sinyali" satırı.
- Aynı pencere **tevsik** haddi için de işler (`TEVSIK_HADDI_TL`): art arda Peşin fişlerde toplam nakit > had → aynı soru (tevsik tarafı YASAL-2 ile birlikte uygulanır, burada tek bileşen).
- **Neden sinyal:** mevzuat "bağlantılı işlem"i kişi üzerinden kurar; kimliksiz satışta kişiyi program bilemez. Soru, sorumluluğu beyanla tezgâhtara bağlar ve kayda geçirir (Anayasa md.1: tahmin yok).

### 4.3 Saklama otomatiği (çelişki 4a)
- **Süre başlangıcı düzeltmesi (§ 3.1'deki md.46 metnine göre):** kimlik tespiti belgeleri için süre, o kimlikle yapılan **son işlemden** başlar. Uygulama (tabloyu değiştirmeden): maskeleme adayı seçilirken aynı `belge_no_hash`'e (`Data/MasakVarliklari.cs:114`) sahip, `SaklamaBitis`'i henüz dolmamış **daha yeni** bir tespit varsa aday **atlanır** (append-only korunur; `saklama_bitis` yeniden yazılmaz). Aynı kural cari bazında da uygulanır: kaydın `cari_id`'siyle son 8 yıl içinde herhangi bir hareket varsa aday atlanır (kimlik tespiti tekrarlanmamış ama müşteri ilişkisi sürüyor olabilir).
- **İş `SAKLAMA_UYGULA`** (YENİ, `Modules/Isler/Isler/SaklamaUygulaIsi.cs`):
  1. Günlük tur: `SaklamaBitis` önümüzdeki 30 gün içinde dolacak kayıt sayısı → SAHİP'e bildirim "N kaydın kimlik bilgileri 30 gün sonra kendiliğinden gizlenecek" (kişisel veri yok).
  2. `SaklamaBitis` geçmiş ve **bekletme yoksa** → `SaklamaSuresiIsi.Uygula` (mevcut maskeleme, tek motor) çalışır; denetim izine "otomatik" etiketiyle yazılır.
  3. Periyot: en geç **KVKK Yönetmeliği md.11/2 sınırı** (§ 3.1) içinde; iş her gün koştuğu için sınır her zaman sağlanır.
- **Bekletme (yasal saklama/uyuşmazlık):** `POST /masak/saklama-bekletme` — SAHİP; kapsam (tümü ya da belirli kimlik tespiti), bitiş tarihi (en çok 1 yıl, yenilenebilir), gerekçe ≥20 karakter. Tablo `saklama_bekletmeleri` (append-only). Bekletme bitince iş kendiliğinden devam eder.
- `ADR-0008` güncellenir: "Elle tetiklenir" → "Kendiliğinden, 30 gün önceden haber + gerekçeli bekletme". Mehmet'in ADR-0008 gerekçesi ("haberi olmadan çalışmamalı") **30 gün önceden bildirim** ile karşılanır.
- `denetim.sh`: `Isler/` altında `SaklamaSuresiIsi.Uygula` sayacı 0 → **1** (yalnız `SaklamaUygulaIsi.cs`); başka yerde 0 kalır.

### 4.4 Yaptırım (malvarlığı dondurma) taraması (çelişki 4c)
- **Veri:** `yaptirim_kayitlari` (YENİ, **sistem geneli**, kiracıya bağlı değil): kaynak (§ 3.1'deki resmî liste adı), karar no/RG tarihi, ad-soyad/unvan (normalize: büyük harf, Türkçe karakter katlama), doğum tarihi, uyruk, kimlik/pasaport/VKN (varsa; **hash + şifreli**), eklenme, kaldırılma tarihi (karar kaldırılırsa; satır silinmez).
- **Güncelleme:** `POST /sistem/yaptirim-listesi` (yalnız SistemAnahtari) — ByGatech merkezî günceller (kuyumcu liste yüklemez). Listenin tarihi ve kaynak URL'si her kiracının Uyum ekranında görünür ("Liste son güncelleme: 29.09.2026"). Liste **14 günden eski** ise SAHİP'e sarı bant.
- **Tarama noktaları (fail-closed):**
  1. MASAK kimlik tespiti yazılırken (`MasakKapisi.Kaydet` öncesi, Faz 1'de): kimlik no **tam eşleşme** → **400 ENGEL (işlem ertelenir — M-12)**, mesaj: "Bu kişiyle işlem yapılamaz (yasal liste). Yöneticinize haber verin." + SAHİP'e acil bildirim ("MASAK'a ivedilikle bildirin") + denetim `YAPTIRIM_ENGEL` + Uyum ekranında **"MASAK bildirimi yapıldı"** kapatma kaydı (tarih, evrak no) bekleyen görev.
  2. Cari oluşturma/güncelleme (TCKN/VKN girilince): tam eşleşme → kayıt açılır ama cari **kilitli** işaretlenir (`carilere` yeni alan DEĞİL — `yaptirim_eslesmeleri` tablosunda kayıt; satış yolları bu tabloya bakar) → o cariye her işlem 400.
  3. Hurda alış satıcısı (gider pusulası TCKN): tam eşleşme → 400.
  4. **Liste güncellenince mevcut carilerle eşleşme** (M-10): yeni eklenen liste satırı mevcut bir cariyle tam eşleşirse ve carinin açık bakiyesi / emaneti / bekleyen siparişi varsa → SAHİP'e KRİTİK görev: "Bu müşteriyle ilgili alacak/borç/emanet bilgisini **30 gün içinde** MASAK'a bildirin — son gün GG.AA" (son gün = kararın RG yayım tarihi + 30 gün; RG tarihi liste satırında tutulur).
  5. **Ad-soyad benzerliği** (kimlik no yoksa): **ENGEL DEĞİL** — işlem geçer, SAHİP'e "olası eşleşme, kontrol edin" bildirimi + risk puanı (`YaptirimPuan`). Yanlış pozitifle tezgâh kilitlenmez.
- `RiskMotoru.cs:46` `YaptirimEslesmesiVar` gerçek girdiye bağlanır.
- **Veri bedeli / kaynak:** § 3.1'de resmî kaynak bulunduysa ücretsiz resmî yayın kullanılır; ücretli ticari liste **kurucu kararı** (K-3).

### 4.5 Risk skorlama (çelişki 4b) — "otomatik ve görünür", bağlayıcı değil
- Skor **zaten** hesaplanıyor ve gösteriliyor (`RiskAyarlari.cs:6-9`). Bu paket bağlayıcılığı **açmaz** (birincil dayanak § 3'te bulunmadıkça — `RiskAyarlari.cs:13-15` gerekçesi korunur).
- Eklenen: YÜKSEK seviyeye **yeni geçen** cari için SAHİP/MUHASEBE'ye otomatik bildirim: "Bu müşteri için şüpheli işlem bildirimi gerekip gerekmediğini değerlendirin" + ŞİB taslağı açma bağlantısı. Engel yok.
- `YuksekRiskliUlkeler` boş kalır; liste yalnız § 3.1'de resmî kaynak bulunursa sistem ucundan doldurulur.

### 4.6 Eşik teyidi (çelişki 8)
- 185.000 TL → YASAL-1 tablosunda `MASAK_KIMLIK_ESIGI_TL`, § 3.1 M-2 ile **`DOGRULANDI`** (tarihçe 20.000 → 75.000 → 85.000 → 185.000 tohumlanır). "Bağlantılı işlem = aynı gün aynı kişi" yorumu T-1 olarak "Yasal sınırlar" ekranında "yorum — teyit bekliyor" notuyla durur; **davranış değişmez**.
- 200.000 (amaç beyanı), 2 M, 20 M → ayrı anahtarlar, `TEYITSIZ` (§ 3.2 T-2, T-3: bulunan metin 2 M/20 M formunu **finansal kuruluşlara** yüklüyor, kuyumcuya değil). **Öneri:** teyitsiz kademelerin zorunlu kıldığı alanlar (amaç beyanı 300 karakter vb.) tezgâhta **zorunlu kalır** (tutucu yön — kuyumcuyu cezaya sokmaz, yalnız biraz yavaşlatır); kurucu gevşetmeyi seçebilir (K-4).

### 4.7 Ekranlar — ekranda nasıl görünür
| Yer | Görünüm | Tür |
|---|---|---|
| Tezgâh, takas / hurda | Değişiklik yok; kümülatif doğru ölçüldüğü için kimlik formu gerektiğinde kendiliğinden açılır (mevcut bant) | otomatik |
| Tezgâh, Peşin Müşteri art arda | Tek soru penceresi: "Az önceki alışverişi yapan müşteri mi?" [Evet — müşteriyi seç] [Hayır, başka biri] | **uyarı + tek karar** |
| Tezgâh, yaptırım tam eşleşme | Kırmızı bant: "Bu kişiyle işlem yapılamaz (yasal liste). Yöneticinize haber verin." Kaydet kapalı | **engel** |
| Panel (SAHİP) | "N kaydın kimlik bilgisi 30 gün sonra gizlenecek" (mavi, katlanır); "Yasal liste 14 günden eski" (sarı); "Olası liste eşleşmesi: 1" (kırmızı) | otomatik bilgi/uyarı |
| Uyum → Saklama | Önizleme sayısı + "Beklet" (gerekçeli) + geçmiş otomatik maskelemeler (katlanır) | otomatik |
| Uyum → Yasal liste | Son güncelleme tarihi, kaynak, eşleşme kayıtları | bilgi |
| Yasal sınırlar (YASAL-1) | MASAK satırlarında "Doğrulandı" / "Mali müşavir teyidi bekliyor" rozeti | bilgi |

---

## 5. Kapsam dışı
- MASAK Online'a elektronik gönderim (API yok — M-07).
- Risk skorunun işlem engeline bağlanması.
- PEP listesinin dış kaynaktan taranması (bugün beyan esaslı; PAKET-40 Faz A).
- Ticari yaptırım veri sağlayıcısı sözleşmesi (kurucu).
- EVRAK_GIRIS'in kümülatife alınması (bilinçli ölçüm kararı korunur).

---

## 6. Kabul ölçütleri
| # | Ölçüt | Kanıt |
|---|---|---|
| K-1 | Aynı cari, aynı gün: 900.000 eski altın ↔ 910.000 ürün takası (fark 10.000, kimlikli) → ardından 100.000 TL satış: kümülatif **1.010.000** ölçülür → kimlik bloğu yoksa **400** | HTTP + SQL (`masak_olcu_tl=910000`) |
| K-2 | ESNAF_MUAF hurda alış brüt 180.000 (stopaj %2 → net 176.400) + aynı gün 6.000 satış → kümülatif **186.000 ≥ 185.000** → 400 (bugün 182.400 → geçiyordu) | HTTP testi |
| K-3 | Migration sonrası HURDA_ALIS satırlarının `masak_olcu_tl` = eşleşen `brut_tutar` (fark 0); pusulasız HURDA_ALIS ve eski TAKAS satırları NULL; sayılar raporda | canlı SQL |
| K-4 | Peşin Müşteri, aynı kullanıcı, 40 dk arayla 100.000 + 90.000 → ikinci istek `ayniKisiBeyani` yoksa 400 `PESIN_ART_ARDA_SORU`; `false` ile 200 + denetim satırı 1; `true` ile 400 "gerçek müşteri seçin" | HTTP testi |
| K-5 | Aynı senaryo 61 dk arayla → soru yok (pencere dışı) | HTTP testi |
| K-6 | Saklama: `SaklamaBitis` dün dolmuş, bekletme yok → iş koşar → `ad_soyad='[SAKLAMA SÜRESİ DOLDU]'`; tutar/tarih/kademe değişmemiş | DB testi |
| K-7 | Aynı belge hash'iyle 2 yıl önce yeni tespit var → eski kayıt **maskelenmez** (son işlem kuralı) | DB testi |
| K-8 | Bekletme aktifken iş kaydı maskelemez; bekletme bitince maskeler | DB testi |
| K-9 | 30 gün kala bildirim 1 kez üretilir (iki tur → 1 bildirim) | DB testi |
| K-10 | Yaptırım listesinde TCKN X → X ile MASAK kimlikli satış **400**, `hareketler` yeni satır 0, SAHİP bildirimi 1, denetim `YAPTIRIM_ENGEL` 1 | HTTP + SQL |
| K-11 | Yalnız ad benzerliği (TCKN farklı) → satış 200 + "olası eşleşme" bildirimi | HTTP testi |
| K-11b | Liste güncellemesi mevcut cariyle tam eşleşir, carinin 5.000 TL açık bakiyesi var → SAHİP'e KRİTİK görev, son gün = RG tarihi + 30 gün; bakiye 0 ve emanet yoksa görev yok | DB testi |
| K-12 | Liste 15 gün eski → Panel sarı bant (bUnit); 13 gün → yok | bUnit |
| K-13 | JWT ile `POST /sistem/yaptirim-listesi` → 401/403 | HTTP testi |
| K-14 | Risk YÜKSEK'e geçiş → 1 bildirim; aynı seviyede kalış → yeni bildirim yok | DB testi |
| K-15 | Mevcut MASAK sınır testleri (184.999,99 / 185.000 / …) değişmeden yeşil | `dotnet test` |
| K-16 | Tarayıcı: soru penceresi, yaptırım bandı, Panel bantları — 1280/375 · Basit/Pro · açık/koyu, taşma 0 | gerçek tarayıcı |

---

## 7. Mutasyon önerileri
1. Takasta `masak_olcu_tl = farkTl` yazılsın → K-1 KIRMIZI.
2. Hurdada `masak_olcu_tl = NetOdenen` → K-2 KIRMIZI.
3. Kümülatif sorgu (1) kaldırılsın (yalnız TlTutari) → K-1/K-2 KIRMIZI.
4. Peşin penceresinde kullanıcı koşulu yerine kiracı geneli (herkes) → K-4 varyantı (başka kullanıcı) KIRMIZI.
5. Son-işlem hash kontrolü silinsin → K-7 KIRMIZI.
6. Bekletme kontrolü silinsin → K-8 KIRMIZI.
7. Yaptırım tam eşleşmede `throw` yerine bildirim → K-10 KIRMIZI.
8. Ad benzerliğinde `throw` → K-11 KIRMIZI.

---

## 8. Riskler
- **Otomatik maskeleme geri alınamaz.** Yanlış `SaklamaBitis` (ör. son-işlem kuralı yanlış uygulanırsa) kişisel veriyi erken yok eder → 30 gün önceden bildirim + K-7 + ilk 90 gün "yalnız önizle" modunda koşma önerisi (`Isler:SaklamaUygula:Kuru=true`), sonra açma.
- **Yaptırım yanlış pozitifi** tezgâhı kilitler → yalnız kimlik no tam eşleşmesi engeller.
- **Liste güncelliği:** listeyi ByGatech taşır → hizmet taahhüdü (SLA) konusu; güncellenmeyen liste yanlış güven verir → tarih her zaman görünür.
- **Soru yorgunluğu:** yoğun dükkânda Peşin penceresi sık tetiklenirse → pencere/eşik politikası appsettings'te, ölçülerek ayarlanır.
- **Kolon ekleme (`hareketler`):** en büyük tablo; migration süresi canlı veride ölçülmeli (T-1 kurulum tatbikatı).

---

## 9. Tahmini süre (en çok 2 tur)
- **Y3-A** (4.1 + 4.2 + 4.6): tur 1 ~4 saat, tur 2 ~2 saat.
- **Y3-B** (4.3 saklama): tur 1 ~3 saat, tur 2 ~1,5 saat.
- **Y3-C** (4.4 yaptırım + 4.5 risk bildirimi): tur 1 ~4 saat, tur 2 ~2 saat — **resmî liste kaynağı ve biçimi netleşmeden başlamaz** (§ 3.2 / K-3).
- Toplam ~2 iş günü, üç şerit (tahmin; ölçülmedi).

---

## 10. Şerit dosya alanı önerisi
- **Y3-A:** `Modules/Masak/MasakKapisi.cs` (yalnız `GunlukIslemToplam` gövdesi — ŞK-A'dan SONRA), `Modules/Satis/SatisModulu.cs` (yalnız `:2309-2353` takas ve `:1222` hurda satırına `MasakOlcuTl` ataması — SATIŞ şeridiyle kapıda birleştirme), `Data/Entities.cs` (yalnız `Hareket.MasakOlcuTl`), `Data/Migrations/*_HareketMasakOlcu.cs`, `Modules/Cari/SistemCarisi.cs` + yeni `Modules/Masak/PesinPencereKapisi.cs`, istemci soru penceresi `Kuyumcu.Web/Bilesenler/Fis/PesinArtArdaSoru.razor` (YENİ; `Islemler*.razor`'a tek çağrı satırı — SATIŞ şeridi sahibi), testler `MasakBrutOlcuTestleri.cs`, `PesinArtArdaTestleri.cs`.
- **Y3-B:** `Modules/Masak/SaklamaSuresiIsi.cs`, `Modules/Isler/Isler/SaklamaUygulaIsi.cs` (YENİ), `Data/SaklamaBekletme.cs` (YENİ), `Modules/Masak/MasakModulu.cs` (yalnız bekletme uçları), `Kuyumcu.Web/Pages/Uyum.razor` (yalnız Saklama sekmesi), `adr/0008-…md`, `denetim.sh` (saklama sayacı), `appsettings.json` (`Isler:SaklamaUygula`).
- **Y3-C:** `Data/Yaptirim.cs` (YENİ), `Modules/Masak/YaptirimServisi.cs` (YENİ), `Modules/Masak/MasakKapisi.cs` (yalnız Faz 1 çağrısı — Y3-A ile aynı dosya → sıralı), `Modules/Masak/RiskMotoru.cs`/`RiskServisi.cs`, `Modules/Sistem/SistemModulu.cs` (liste ucu), `Modules/Cari/CariModulu.cs` (yalnız kimlik alanı yazımı sonrası tarama çağrısı), `Modules/Hurda/GiderPusulasiKapisi.cs` (yalnız `Dogrula`'ya tarama çağrısı — YASAL-4 ile aynı dosya → sıralı), `Kuyumcu.Web/Pages/Uyum.razor` (Yasal liste sekmesi — Y3-B ile aynı dosya → sıralı).
- **Genel sıra:** YASAL-1 → ŞK-A → Y3-A → (Y3-B ∥ Y2-B) → Y3-C.

---

## 11. Kurucu / mali müşavir kararı gereken noktalar
- **K-1 (S-2):** 185.000 TL birincil kaynakla doğrulandı; açık kalan yalnız "bağlantılı işlem" penceresi (aynı gün / aynı kişi) yorumu (§ 3.2 T-1).
- **K-2:** Otomatik maskeleme açılsın mı (öneri: evet, 90 gün "kuru" koşumdan sonra)?
- **K-3:** Yaptırım listesi kaynağı: resmî ücretsiz yayın yeterli mi, ticari sağlayıcı mı (bedel)?
- **K-4:** Teyitsiz MASAK kademeleri (200.000 amaç beyanı, 2 M / 20 M) zorunlu mu kalsın? Bulunan metin 2 M/20 M formunu finansal kuruluşlara yüklüyor → mali müşavir "kuyumcuya uygulanmaz" derse kademeler kapatılır (tabloda ileri tarihli `KAPALI` satırı; kod değişmez). Teyide kadar öneri: tutucu, açık kalsın.
- **K-5:** Peşin Müşteri art arda penceresi: 60 dk, aynı kullanıcı + aynı şube (öneri) — sahada ölçülerek.
