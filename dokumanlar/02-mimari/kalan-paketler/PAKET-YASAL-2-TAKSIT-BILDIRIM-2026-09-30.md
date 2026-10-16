# PAKET YASAL-2 — Tevsik: taksitte her ödeme banka (TB-15) · 5 iş günü kendiliğinden bildirim (TB-16)

> Tarih: 2026-09-30 · Hazırlayan: Tamer Bey koordinasyonu (şartname ajanı; KOD YAZILMADI) · Durum: ŞARTNAME — ekip oyu bekliyor
> Kapattığı kalemler: **TB-15** (`dokumanlar/08-degerlendirme/teknik-borc-envanteri.md:265-273`, `mevzuat-gereksinimleri.md:55` V-03b) · **TB-16** (`teknik-borc-envanteri.md:277-283`, `mevzuat-gereksinimleri.md:57` V-03d) · MIHENK-PLAN-RAPORU §4c.
> Önkoşul: **YASAL-1** (tarihli had) · **ŞUBE-KAPSAM ŞK-A** (kümülatifin şubeden bağımsız okunması). PAKET-06 Faz A (bildirim + iş çatısı) ağaçta (`Modules/Isler/`).

---

## 1. Amaç

Kurucu önceliği: **"Pusula kadar sade + devletin tarif ettiği her kural otomatik ve görünür."**

- **TB-15:** 30.000 TL'yi aşan bir satış/alış **veresiye ya da taksitle** kapatılıyorsa, sonraki **her ödeme** (tutarı ne olursa olsun) kart/havale/PTT ile alınmalı. Program bunu kendisi bilir: tahsilat ekranında o müşteri için "Nakit" seçeneği o borç için **kapanır**, yerine tek cümle ve iki düğme ("Kartla al", "Havaleyle al") çıkar. Tezgâhtar kural bilmek zorunda değildir.
- **TB-16:** Kapı her şeye rağmen bir ihlali kaçırdıysa (kural kapatılmış, eski kayıt, eşzamanlı iki fiş, veri göçü, şube açığı), program ihlali **ertesi sabah kendisi bulur**, patrona **son günü** ile gösterir ve bildirim dilekçesini **tek düğmeyle** hazırlar. Kayıt "bildirildi" diye kapatılana kadar Panel'de kırmızı durur.

---

## 2. Mevcut durum (kod kanıtı)

### 2.1 TB-15
| Kanıt | Ne gösteriyor |
|---|---|
| `Modules/Tevsik/TevsikKurali.cs:104-127` | "İşlem tutarı kuralı" (§4.1.2) VAR ama **yalnız aynı fişteki** bacaklara bakıyor: `islemToplamTutari > Had` ise o fişin nakit payı reddedilir. |
| `Modules/Tevsik/TevsikKapisi.cs:99-130` | Kümülatif yalnız **aynı yerel gün** (`Zaman.YerelGunAraligiUtc`). |
| `Modules/Cari/CariModulu.cs:1015-1017` | Nakit tahsilatta `tevsik.Dogrula(db, tenant, id, tlTutari, now)` — **`islemToplamTutari` verilmiyor**, borcun hangi işlemden doğduğuna bakılmıyor. |
| `CariModulu.cs:1430-1431` (döviz tahsilat), `Modules/Evrak/EvrakModulu.cs:490-492` (evrak nakit tahsili) | Aynı: kaynak işlem bilinmiyor. |
| `Modules/Vade/VadeKapisi.cs:184-260` `AcikBorclar` | Defteri eskiden yeniye yürüyüp **açık partileri FIFO** türetiyor (`partiler: Id, Tarih, Vade, Kalan`) — kaynak hareketin kimliği **zaten elde**. |
| `Data/Entities.cs` `Hareket.FisId` (`fis_id`), `VeresiyeTutar`, `NakitTutar` | Kaynak fişin toplamı türetilebilir (fişin hareketlerinin `|TlTutari|` toplamı). |
| Örnek açık (teknik-borc-envanteri.md:269) | 62.000 TL'lik alım 3 güne bölünüp günde 20.000 TL nakit ödenirse kapı hiçbirini durdurmaz. |

### 2.2 TB-16
| Kanıt | Ne gösteriyor |
|---|---|
| `appsettings.json:131` `_not` + `:147-149` `"TevsikKurtarma": { "Etkin": false }` | İş kodu planlanmış, **yazılmamış**; gerekçe: dayanak birincil kaynaktan doğrulanmamıştı. |
| `Modules/Isler/Isler/` | Yalnız `MasakSibSuresiIsi.cs`, `SaklamaOnizlemeIsi.cs`, `VadeHatirlatmaIsi.cs` var. |
| `Kuyumcu.Tests/IsSenaryoTestleri.cs:287`, `:292` | `TevsikKurtarmaIsi` **olmadığını** ve `Isler:TevsikKurtarma:Etkin=false` olduğunu zorlayan test. |
| `PAKET-06…md:56`, `:170`, `:206` | Faz B tasarımı: yalnız `Tevsik:Etkin=false` kiracıda, son 5 **takvim** günü; "iş günü hesabı yapılmaz (resmî tatil takvimi yok)". |
| `Common/ResmiTatiller.cs:26-90` | **Artık resmî tatil takvimi VAR** (sabit tatiller kodda, dinî bayramlar yapılandırmadan). PAKET-06'daki "takvim yok" gerekçesi düştü. |
| `Modules/Tevsik/TevsikKapisi.cs:158-159` (Kapsam kaydı md.4) | "Kapı transaction'dan önce ve kilitsiz okur; eşzamanlı iki fiş haddi birlikte delebilir (TOCTOU)" — ihlalin kapıya rağmen doğabileceği yazılı. |
| `TevsikKapisi.cs:143-147` (md.1), `:148-150` (md.2), `:160-166` (md.5) | Eski KARMA satırlar, kimliksiz müşteri ve Peşin Müşteri art arda fişleri ölçülmüyor. |
| PAKET-SUBE-KAPSAM § 2.6 | Şubeye kilitli kullanıcıda kümülatif şubeye bölünüyor (çıkarım). |

---

## 3. Mevzuat

> Doğrulama: 30.09.2026, birincil metinler okunarak.

### 3.1 Doğrulanan hükümler
| # | Hüküm | Birincil kaynak | Alıntı (≤15 kelime) |
|---|---|---|---|
| M-1 | **Kısım kısım ödeme (TB-15):** haddi aşan işlemde, bedel **farklı tarihlerde** kısım kısım ödenirse işlemin toplam tutarı esas alınır; her bir tahsilat/ödeme aracı finansal kurum kanalıyla yapılır. Tebliğ örneği: 12.000 TL'lik hizmetin 1.000 TL'lik taksitleri de bankadan (ilk metin; o tarihte had 7.000 TL) | 459 s. VUK GT §4.1.2 (konsolide) · https://www.mevzuat.gov.tr/File/GeneratePdf?mevzuatNo=21303&mevzuatTur=Teblig&mevzuatTertip=5 | "işlemin toplam tutarı dikkate alınacak ve her bir tahsilat ve ödeme" · "tevsik zorunluluğu kapsamında aracı finansal kurumlar kanalıyla gerçekleştirilecektir" |
| M-2 | Aynı gün aynı kişiyle yapılan işlemler toplanır; **aştığı işlemden itibaren** bankadan (mevcut kümülatif kapının dayanağı) | 459 s. VUK GT §4.1.1 (ilk metin) · https://www.resmigazete.gov.tr/eskiler/2015/12/20151224-6.htm | "işlemlerin her biri işlem bazında belirlenen haddin altında kalsa bile, aştığı işlemden itibaren" |
| M-3 | Mükellef olmayanlar (vatandaş) da kapsamda | 572 s. VUK GT, RG 18.10.2024/32696 · https://www.resmigazete.gov.tr/eskiler/2024/10/20241018-9.htm | "mükellef olmayanlar da, tevsik zorunluluğu kapsamında olanlardan yapacakları alımlarına ilişkin" |
| M-4 | Had 30.000 TL, **30.11.2024**'ten itibaren (öncesi 7.000 TL) | 575 s. VUK GT, RG 30.11.2024/32738 · https://www.resmigazete.gov.tr/eskiler/2024/11/20241130-10.htm | "“7.000” ibareleri “30.000” şeklinde" · "Bu Tebliğ yayımı tarihinde yürürlüğe girer." |
| M-5 | **Ceza:** işlem tutarının %10'u, maddedeki asgari tutarlardan az olmamak üzere (2026 asgarileri: 1. sınıf 35.000 · 2. sınıf 17.000 · diğer 8.700 TL — 588 s. GT) | VUK mük. md.355 (7524 s. K. md.13, RG 02.08.2024/32620) · https://www.mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf | "işleme konu tutarın % 10'u nispetinde özel usulsüzlük cezası kesilir" |
| M-6 | **Kendiliğinden bildirim (TB-16):** ödemeyi **yapan**, ödemeyi takip eden 5 iş günü içinde kendiliğinden idareye bildirirse **ödeme yapan adına** ceza kesilmez | VUK mük. md.355 · aynı URL | "ödemeyi takip eden beş iş günü içerisinde kendiliğinden idareye bildirmesi halinde" |

**Ürüne etkisi (M-6'dan çıkan, önemli):** muafiyet **ödeyen taraf** içindir.
- Kuyumcu **ödeyen** ise (vatandaştan hurda/altın **alış**, tedarikçiye tediye): 5 iş günü içinde bildirirse kuyumcuya ceza kesilmez → ürün **son günü** gösterir ve dilekçeyi hazırlar.
- Kuyumcu **tahsil eden** ise (müşteriye satış): muafiyet müşteriye aittir; kuyumcu için bu yolla muafiyet **yoktur**. Ürün yine ihlali gösterir ama "cezadan kurtarır" cümlesi yazmaz; "Mali müşavirinize bugün danışın" yazar.
- Bu yüzden `tevsik_ihlalleri` tablosuna **`yon`** (`TAHSILAT` | `ODEME`) kolonu eklenir; ekran metni ve geri sayım `yon=ODEME` için kurulur.

### 3.2 Teyit edilemeyenler
| # | Konu | Durum |
|---|---|---|
| T-1 | "İdareye bildirme"nin **biçimi ve kanalı** (dilekçe mi, interaktif vergi dairesi mi, hangi vergi dairesi, hangi ekler) — md.355'te şekil yok; bunu düzenleyen tebliğ/GİB duyurusu bulunamadı | **DOĞRULANAMADI — mali müşavir teyidi.** Ürün dilekçe şablonunu "taslak — mali müşavirinizle kontrol edin" damgasıyla üretir |
| T-2 | "Beş iş günü" sayımında arife yarım gününün iş günü sayılıp sayılmayacağı | DOĞRULANAMADI — ürün tutucu sayar (iş günü sayar → son gün erken) |
| T-3 | TB-15'te had tarihi: işlem günü mü, her ödemenin günü mü | DOĞRULANAMADI — öneri işlem günü (YASAL-1 § 11/1) |
| T-4 | Takas (trampa) kaynaklı borcun taksitlerinin §4.1.2 kapsamında olup olmadığı | DOĞRULANAMADI — takas kapsam dışı tutulur (`TevsikKurali.cs:63-66` ile tutarlı), S-1 sorusuna eklenir |
| T-5 | 2025 yılı md.355 asgari ceza tutarları | DOĞRULANAMADI (yalnız 2026 okundu) — üründe ceza rakamı zaten gösterilmez |

---

## 4. Kapsam

### 4.1 TB-15 — "Büyük işlemin taksiti" kapısı

**Kavram — kaynak işlem:** Bir açık borç partisinin doğduğu hareket; o hareket bir fişe aitse (`fis_id` dolu) **fişin tamamı**, değilse hareketin kendisi. **Kaynak işlem tutarı** = storno çifti dışlanmış hareketlerin `|TlTutari|` toplamı (mevcut `StornoCiftiniDisla` deseni). **Kaynak gün** = kaynak işlemin yerel günü.

**Kural (saf, `TevsikKurali.TaksitDegerlendir`):**
```
girdi: bu ödemenin nakit payı N, FIFO ile tüketeceği partiler [(partiId, tüketilen T_i, kaynakTutar K_i, kaynakGun G_i)]
her parti için: buyuk_i = K_i > Had(G_i)            // YASAL-1: kaynak günün haddi
izinliNakit = Σ T_i  (buyuk_i = false olanlar)
N > izinliNakit  → RED (fail-closed), mesajda izinliNakit yazılır
```
- Ödeme **önce küçük (eşik altı) partilere değil, FIFO sırasına** göre dağıtılır (defterin tek doğruluk kuralı, `VadeKapisi.cs:171-173`). Nakit payının hangi partiye düştüğünü kullanıcı seçemez → kural öngörülebilir.
- **Vadesiz bakiye** (açılış bakiyesi, eski satırlar — `VadeKapisi.cs:252-256`): kaynak işlem bilinmez. **Öneri: geçer + denetim kaydı `TEVSIK_TAKSIT_DOGRULANAMADI` + Uyum listesinde görünür satır.** (Fail-closed seçilirse devir müşterilerinin tüm nakit tahsilatı kilitlenir — kurucu kararı K-A.)
- Aynı kural **tediye** yönünde (kuyumcunun tedarikçiye/vatandaşa taksitle ödemesi) cari bakiyesinin işaretiyle çalışır (`VadeKapisi.cs:199-200` `alacakYonu`).
- Kapı transaction'dan **önce** çalışır (mevcut tevsik deseni); kümülatif gün kuralı **ayrıca** çalışmaya devam eder (iki kapı, iki ayrı red mesajı).

**Uygulanacak yollar:** `CariModulu.TahsilatGir` (`:969`), `CariModulu.DovizTahsilatGir` (`:1408`), `EvrakModulu` nakit tahsil (`:490-492`), satış fişi içinde "eski borçtan tahsil" bacağı varsa `SatisModulu.cs` fiş yolu (uygulayıcı grep: `TAHSILAT` türü üreten her `HareketFabrikasi.Ekle` çağrısı).

**Fiş anında görünürlük (sade):** Veresiye bacaklı bir fişin toplamı haddi aşıyorsa fiş onayında tek satır: **"Kalan borç sonradan yalnız kartla ya da havaleyle alınabilir."** (engel değil, bilgi). Yazdırılan fişin altına aynı cümle.

**Önizleme ucu genişler:** `GET /api/v1/tevsik/gunluk?cariId=` (`TevsikController.cs:42-80`) yanıtına:
```
taksit: { kuralDevrede: bool, nakitAlinabilir: decimal, sebep: "30.000 TL'yi aşan 12.03 tarihli satıştan kalan borç" }
```
Tahsilat ekranı bu alana göre "Nakit" seçeneğini **kapatır** (devre dışı + neden) — ekran tahmin etmez, sunucu söyler.

### 4.2 TB-16 — Geç fark edilen ihlal: bul, göster, bildirim dilekçesi hazırla

**Yeni tablo `tevsik_ihlalleri`** (`IKiraciVarlik`; şube taşımaz — ihlal mükellef geneli):
| Kolon | Not |
|---|---|
| `id`, `tenant_id` | |
| `tur` | `GUNLUK_KUMULATIF` · `ISLEM_TUTARI` · `TAKSIT` |
| `yon` | `TAHSILAT` (kuyumcu tahsil etti) · `ODEME` (kuyumcu ödedi) — § 3.1 M-6: muafiyet yalnız ödeyene |
| `cari_id` | NULL olamaz (kimliksiz satışta ihlal ölçülemez — bilinen sınır) |
| `ihlal_gunu` | yerel DATE |
| `nakit_toplam_tl`, `had_tl` | had = YASAL-1 `TEVSIK_HADDI_TL(ihlal_gunu)` |
| `hareket_idleri` | JSON dizi (kanıt; kişisel veri yok) |
| `bildirim_son_gunu` | `ihlal_gunu` + `TEVSIK_BILDIRIM_IS_GUNU` iş günü (**`ResmiTatiller` ile**; yıl için bayram girilmemişse süre **kısa** hesaplanır → güvenli yön; ekranda "bayramlar girilmedi" notu) |
| `durum` | `ACIK` → `BILDIRILDI` \| `SURE_DOLDU` \| `GECERSIZ` |
| `kapatma_tarihi`, `kapatan_kullanici_id`, `kapatma_gerekcesi` (≥20 kr.), `bildirim_evrak_no` | Kapatma **bir kez** yazılır; silme yok (ADR-0004) |
| `olusturuldu` | |
Benzersizlik: UNIQUE (`tenant_id`, `cari_id`, `ihlal_gunu`, `tur`) — iş iki kez koşsa da tek satır.

**İş `TEVSIK_KURTARMA`** (`Modules/Isler/Isler/TevsikKurtarmaIsi.cs`, PAKET-06 çatısı, `IZamanlanmisIs`):
- Her tur: son **10 takvim günü** (5 iş gününü her zaman kapsar) için, **kiracı geneli** (şube süzgeçsiz — `SistemKapsami.Kiraci`, IsZamanlayici zaten bu kapsamda koşar) cari × yerel gün nakit toplamı `TevsikKapisi.GunlukNakitToplam` ile **aynı fonksiyonla** hesaplanır (ikinci formül yazılmaz — ADR-0016 tek motor ruhu).
- `ISLEM_TUTARI`: fiş toplamı > had iken nakit payı > 0 olan fişler.
- `TAKSIT`: TB-15 kuralına aykırı nakit tahsilatlar (kural kapalıyken ya da "doğrulanamadı" geçişleri).
- Her yeni satır için **bildirim** (PAKET-06 `bildirimler`): görünür roller SAHİP, MUHASEBE; başlık "Nakit sınırı aşıldı — bildirim için son gün 07.10"; gövdede **ad/TCKN yok** (PAKET-06 Mehmet koşulu); cari yalnız bağlantıyla.
- `bildirim_son_gunu` geçmiş ve `ACIK` → `SURE_DOLDU` (otomatik; kayıt kalır).
- **Varsayılan: AÇIK** (`Isler:TevsikKurtarma:Etkin = true`) — dayanak § 3.1 M-6 ile DOĞRULANDI. Metin yöne göre: `yon=ODEME` → "Son güne kadar vergi dairesine bildirirseniz ceza kesilmez" + geri sayım; `yon=TAHSILAT` → "Bu muafiyet ödeyen müşteriye aittir; mali müşavirinize bugün danışın" (geri sayım yok, kayıt görünür kalır).
- `IsSenaryoTestleri.cs:287`, `:292` bu paketle **tersine** çevrilir (iş VAR, Etkin=true).

**Uçlar:**
- `GET /api/v1/tevsik/ihlaller?durum=ACIK` — SAHİP, MUDUR, MUHASEBE (MASAK uçlarıyla aynı rol seti; TEZGÂHTAR yok).
- `GET /api/v1/tevsik/ihlaller/{id}/dilekce` — yazdırılabilir **bildirim dilekçesi** verisi (belge motoru P-5 şablonu). Karşı tarafın tam kimliği yalnız bu uçta, **okuma izi zorunlu** (`denetim.OkumaKaydet`, hassas). İçerik alanları § 3.1'deki hüküm neyi istiyorsa o; hüküm şekil öngörmüyorsa şablon "mali müşavir teyidi" damgalı serbest dilekçe olur (§ 3.2).
- `POST /api/v1/tevsik/ihlaller/{id}/bildirildi` — gövde: `bildirimTarihi`, `evrakNo` (isteğe bağlı), `gerekce`. `bildirimTarihi > bildirim_son_gunu` ise kayıt yine kapanır ama durum `BILDIRILDI` + `gec: true` (yalan beyan üretilmez).
- `POST /api/v1/tevsik/ihlaller/{id}/gecersiz` — yalnız SAHİP; gerekçe ≥20 karakter (ör. "havale yapılmış, kasaya yanlış NAKİT girilmiş — düzeltme fişi X"). Denetim izine yazılır.
- `POST /api/v1/isler/TEVSIK_KURTARMA/calistir` — mevcut elle tetikleme ucu (SAHİP/MÜDÜR, 3/dk).

### 4.3 Ekranlar — ekranda nasıl görünür
| Yer | Görünüm | Tür |
|---|---|---|
| Cari → Tahsilat (Basit + Pro) | Büyük işlemden kalan borçta "Nakit" düğmesi **devre dışı**; altında tek cümle: "Bu borç 30.000 TL'yi aşan bir satıştan. Kartla ya da havaleyle alın." + "Kartla al" / "Havaleyle al". Kısmen izinliyse: "En çok 4.000 TL nakit alınabilir." | **engel** (sunucu 400 ile de) |
| Satış fişi onayı (veresiye bacağı + toplam > had) | Tek satır bilgi: "Kalan borç sonradan yalnız kartla/havaleyle alınır." | otomatik bilgi |
| Panel (SAHİP/MUHASEBE) | Açık ihlal varsa KRİTİK satır: `ODEME` yönünde "1 nakit sınırı aşımı — bildirim için son gün 07.10 (3 iş günü)"; `TAHSILAT` yönünde "1 nakit sınırı aşımı — mali müşavirinize danışın" → Uyum ekranına bağlantı | **uyarı** |
| Uyum → "Nakit sınırı aşımları" (YENİ sekme) | Liste: tarih · müşteri · tutar · son gün (geri sayım) · durum; satırda **"Dilekçeyi yazdır"** (birincil) ve "Bildirildi olarak işaretle"; kapalılar katlanır akordeonda (geçmiş kayıtlar katlanır kuralı) | otomatik + tek düğme |
| Bildirimler | Aynı kayıt bildirim olarak (PAKET-06) | otomatik |
| Tezgâhtar | İhlal listesini görmez | — |

Jargon: ekranda "459", "355", "tevsik" kelimesi yok; "nakit sınırı" denir. Dayanak yalnız Yardım/katlanır "Neden?" içinde.

---

## 5. Kapsam dışı
- Dilekçenin GİB'e **elektronik gönderimi** (API yok; kuyumcu/mali müşavir verir). Ürün yalnız belgeyi hazırlar ve kapanışı kaydeder.
- Karşı tarafa (vatandaşa) bildirim.
- Ceza hesaplayıcı (ekranda ceza rakamı yazılmaz — plan raporu §7: "ceza rakamı yardımda yazar, ekranda değil").
- Kimliksiz (Peşin Müşteri / `cariId` null) satışlarda kişi bazlı ihlal tespiti — kişi kayıtta değil (YASAL-3 § 4.2'deki art arda fiş sinyali yalnız MASAK içindir; tevsik için aynı sinyal oyda ayrıca tartışılır).
- Eski KARMA satırların nakit payının geri-doldurulması (`TevsikKapisi.cs:143-147`).

---

## 6. Kabul ölçütleri
| # | Ölçüt | Kanıt |
|---|---|---|
| K-1 | 62.000 TL veresiye satış (tek fiş) → 3 gün arayla 20.000 TL nakit tahsilat denemesi: **üçü de 400**, `hareketler`'de yeni TAHSILAT satırı 0 | HTTP testi + canlı SQL |
| K-2 | Aynı senaryoda 20.000 TL **kart** tahsilatı → 200 | HTTP testi |
| K-3 | Cari: 10.000 TL eski veresiye (tek küçük satış) + sonra 50.000 TL veresiye satış. 15.000 TL nakit tahsilat → FIFO: 10.000 küçük partiye, 5.000 büyüğe → **400**, mesaj "en çok 10.000 TL nakit"; 10.000 TL nakit → 200 | HTTP testi |
| K-4 | Sınır: kaynak işlem tam 30.000,00 → taksit nakit serbest; 30.000,01 → taksit nakit 400 | saf + HTTP |
| K-5 | Kaynak işlem 2024-11-20 tarihli 25.000 TL veresiye (o gün had **7.000** — YASAL-1 § 3.1) → 2026'daki 5.000 TL nakit taksit **400** (**kaynak günün haddi**; K-B kararı bu yönde çıkarsa). Aynı tutar 2024-12-05 tarihli işlemde (had 30.000) → taksit nakit 200 | DB testi |
| K-6 | Vadesiz (açılış) bakiyeden nakit tahsilat → 200 + `denetim_kayitlari`'nda `TEVSIK_TAKSIT_DOGRULANAMADI` 1 satır (detayda kişisel veri yok) | HTTP + SQL |
| K-7 | Tediye yönü: 40.000 TL veresiye alış → 2 × 10.000 TL nakit ödeme → 400 | HTTP testi |
| K-8 | `GET /tevsik/gunluk?cariId=` K-3 carisinde `taksit.nakitAlinabilir = 10000` | HTTP testi |
| K-9 | `Tevsik:Etkin=false` kiracıda aynı gün 20.000 + 15.000 nakit → iş koşunca `tevsik_ihlalleri` 1 satır, `tur=GUNLUK_KUMULATIF`, `nakit_toplam_tl=35000` | DB testi (IsCalistirici) |
| K-9b | Vatandaştan 35.000 TL nakit hurda alışı (kural kapalı kiracı) → ihlal `yon=ODEME`, `bildirim_son_gunu` dolu; müşteriye 35.000 nakit satış → `yon=TAHSILAT`, Panel metni "mali müşavirinize danışın", geri sayım yok | DB + bUnit |
| K-10 | İş aynı gün 2 kez koşar → hâlâ 1 satır, 1 bildirim | DB testi |
| K-11 | `bildirim_son_gunu`: ihlal Cuma 2026-10-23; sayım ihlali **izleyen** günden başlar, hafta sonu ve 29 Ekim (sabit resmî tatil, `ResmiTatiller.cs:37`) atlanır → 26, 27, 28, 30 Ekim, 2 Kasım → **2026-11-02**. 28 Ekim arife yarım günü **iş günü sayılır** (varsayım — § 3.2'ye yazılı; kısa hesap güvenli yöndür). | saf test |
| K-12 | Son gün geçince `ACIK` → `SURE_DOLDU`; kayıt silinmez | DB testi |
| K-13 | Bildirim gövdesinde TCKN/ad/belge no geçmez (regex taraması) | IsSenaryoTestleri |
| K-14 | TEZGÂHTAR `GET /tevsik/ihlaller` → 403; dilekçe ucu okuma izi yazar | HTTP testi |
| K-15 | Ekran: tahsilatta "Nakit" devre dışı + tek cümle; Panel KRİTİK satır; Uyum sekmesi — 1280/375 · Basit/Pro · açık/koyu = 8 görüntü, taşma 0 | gerçek tarayıcı |
| K-16 | Şube: A şubesinde 20.000 + B şubesinde 15.000 (iki tezgâhtar, gate kapalı kiracı) → iş ihlali **bulur** | DB testi |
| K-17 | `dotnet test` 0 kırmızı; `denetim.sh` TEMİZ; test kiracısı silindi | kapı |

---

## 7. Mutasyon önerileri
1. `TaksitDegerlendir`'de `K_i > Had` yerine `>=` → K-4 KIRMIZI.
2. Kaynak tutar fiş yerine yalnız tek hareketten okunsun → K-1 (çok kalemli fiş varyantı) KIRMIZI.
3. FIFO yerine en yeni partiden tüketilsin → K-3 KIRMIZI.
4. `CariModulu.TahsilatGir`'den taksit kapısı çağrısı silinsin → K-1 KIRMIZI.
5. Had kaynak gün yerine bugünle okunsun → K-5 KIRMIZI.
6. İhlal taraması istek bağlamı (şube süzgeçli) DbContext ile yapılsın → K-16 KIRMIZI.
7. UNIQUE indeks kaldırılsın → K-10 KIRMIZI.
8. İş günü hesabında `ResmiTatiller` atlansın → K-11 KIRMIZI.
9. Bildirim gövdesine cari adı eklensin → K-13 KIRMIZI.

---

## 8. Riskler
- **Saha sürtünmesi:** "Pusula'da taksiti nakit alıyorduk" itirazı. Azaltım: engel tek cümle + iki düğme; fişte önceden bilgi satırı.
- **FIFO algısı:** kullanıcı "bu ödeme küçük borç için" diyemez. Mevzuat açısından tutucu; ticari itiraz olursa "hangi borca sayılsın" seçimi ayrı paket (mahsup tablosu — `VadeKapisi.cs:171-173` gerekçesiyle bugün reddedilmiş).
- **Performans:** her nakit tahsilatta carinin tüm defteri yürünür (`AcikBorclar` zaten böyle). Büyük carilerde ölçülmeli (TB-19).
- **Hukuki güven:** dilekçe şablonu hukuki danışmanlık gibi algılanabilir → "Mali müşavirinizle kontrol edin" damgası; § 3.2 teyitsizse "cezadan kurtarır" cümlesi yok.
- **Takas:** `TevsikKurali.cs:63-66` takası işlem tutarı kuralının dışında tutuyor; taksit kuralı da takas kaynaklı partilere **uygulanmaz** (tutarlılık) — mali müşavir sorusu S-1 ile birlikte.

---

## 9. Tahmini süre (en çok 2 tur)
- **Y2-A (TB-15):** tur 1 ~4 saat, tur 2 ~2 saat.
- **Y2-B (TB-16):** tur 1 ~4 saat (tablo+iş+uçlar+dilekçe şablonu+ekran), tur 2 ~2 saat.
- Toplam **~1,5 iş günü**, iki ayrı şerit (tahmin; ölçülmedi).

---

## 10. Şerit dosya alanı önerisi
**Y2-A (TB-15) yazar:** `Modules/Tevsik/TevsikKurali.cs` (yalnız yeni `TaksitDegerlendir`), `Modules/Tevsik/TevsikKapisi.cs` (yeni `TaksitDogrula`; `GunlukNakitToplam`'a DOKUNMAZ — ŞK-A'nın), `Modules/Tevsik/TevsikController.cs`, `Modules/Vade/VadeKapisi.cs` (yalnız `AcikBorclar`'dan parti+kaynak döndüren saf yardımcı), `Modules/Cari/CariModulu.cs` (yalnız `TahsilatGir`/`DovizTahsilatGir` kapı satırı), `Modules/Evrak/EvrakModulu.cs` (yalnız `:490-492`), `Kuyumcu.Web/Pages/Cariler.razor` (yalnız tahsilat çekmecesi), testler `TevsikTaksitTestleri.cs` (YENİ).
**Y2-B (TB-16) yazar:** `Data/TevsikIhlali.cs` (YENİ), `Data/KuyumcuDbContext.cs` (DbSet+indeks — YASAL-1 ile aynı dosya → sıralı), `Data/Migrations/*_TevsikIhlalleri.cs`, `Modules/Isler/Isler/TevsikKurtarmaIsi.cs` (YENİ), `Modules/Tevsik/TevsikIhlalModulu.cs` (YENİ), `Modules/ModulKayit.cs` (1 satır), `appsettings.json` (`Isler:TevsikKurtarma`), `Kuyumcu.Web/Pages/Uyum.razor` (yeni sekme), `Pages/Panel.razor` (1 satır `UyumSatiri`), belge şablonu `Kuyumcu.Web/Belgeler/TevsikDilekce*.razor` (YENİ), `Kuyumcu.Tests/IsSenaryoTestleri.cs` (:287/:292 tersine), `TevsikIhlalTestleri.cs` (YENİ), `denetim.sh` (`BEKLENEN_VARLIK` +1).
**Sıra:** YASAL-1 → ŞK-A → Y2-A ∥ Y2-B (Y2-A ile Y2-B yalnız `TevsikController.cs`'te değil, farklı dosyalarda çalışır; `KuyumcuDbContext.cs` sıralı).

---

## 11. Kurucu / mali müşavir kararı gereken noktalar
- **K-A:** Vadesiz/devir bakiyesinden nakit tahsilat: geçsin + iz (öneri) mi, engellensin mi?
- **K-B:** Taksitte hangi günün haddi (işlem günü öneri) — YASAL-1 § 11/1 ile aynı soru.
- **K-C:** Bildirim yolu ve biçimi (dilekçe mi, interaktif vergi dairesi mi, hangi idare) — § 3.2.
- **K-D:** Birincil metne göre muafiyet yalnız **ödeyen** tarafa (§ 3.1 M-6). Tahsilat yönündeki ihlalde kuyumcunun başka bir kurtarma yolu (pişmanlık vb.) olup olmadığı mali müşavire sorulur; ürün bu soruya cevap uydurmaz.
