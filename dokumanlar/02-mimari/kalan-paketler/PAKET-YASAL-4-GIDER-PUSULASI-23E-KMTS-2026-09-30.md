# PAKET YASAL-4 — Gider pusulası süresi + stopaj borcu · KDV 23/e görünürlüğü · KMTS alanı

> Tarih: 2026-09-30 · Hazırlayan: Tamer Bey koordinasyonu (şartname ajanı; KOD YAZILMADI) · Durum: ŞARTNAME — ekip oyu bekliyor
> Kapattığı çelişkiler (BYGATECH-PLAN-RAPORU §3a): **(2)** 23/e "hariç" beyanı Faturalar sonunda kapalı katlanırda · **(3)** gider pusulasında 7 gün yalnız uyarı, kesilen stopajın vergi dairesine borç kaydı yok · **(6)** KMTS alanı elle ve doğrulamasız. Ek: **(8)**'in 23/e yarısı.
> Önkoşul: **YASAL-1** (`GIDER_PUSULASI_GUN`, `STOPAJ_*`, `KDV_OZEL_MATRAH_KODU`, `KMTS_KOD_DESENI` anahtarları).

---

## 1. Amaç

Kurucu önceliği: **"Pusula kadar sade + devletin tarif ettiği her kural otomatik ve görünür."**

- **Gider pusulası:** Pusula hurda alışla **aynı anda** zaten düzenleniyor; program "7 gün geçti" diye **yanlış alarm** vermemeli, yalnız gerçekten belgesiz kalan alış için geri sayım göstermeli. Yanlış pusula düzeltilirken stok **ikinci kez** girilmemeli.
- **Stopaj:** Kesilen stopaj dükkânın parası değil, vergi dairesinin alacağıdır. Program ay sonunda "Bu ay X TL stopaj kestiniz; son ödeme günü …" desin, ödenince tek düğmeyle kapatılsın, mali müşavire liste tek düğmeyle çıksın.
- **KDV 23/e:** Kuyum faturasında has bedelinin KDV dışı olduğu **faturanın kendisinde** görünsün; "özel matrah kodu onay bekliyor" bilgisi sayfa sonunda saklanmasın, tek satır rozetle görünsün.
- **KMTS:** Perakende kuyumcuya yükümlülük yoksa alan tezgâhı kalabalıklaştırmasın; yükümlülük/şema çıkarsa **tek yerden** açılsın ve doğrulansın. Ekranda "sunucu doğrulamaz" gibi geliştirici cümlesi olmasın.

---

## 2. Mevcut durum (kod kanıtı)

### 2.1 Gider pusulası 7 gün (çelişki 3a)
| Kanıt | Ne gösteriyor |
|---|---|
| `Modules/Hurda/GiderPusulasiKapisi.cs:9-17` | Pusula hurda alış hareketiyle **aynı commit'te** yazılır (FAZ 2). |
| `GiderPusulasiKapisi.cs:192-193` | `TeslimTarihi = now; DuzenlemeSonTarihi = now.AddDays(PusulaDuzenlemeGunu)` — belge teslim anında düzenlendiği hâlde "son tarih" ileriye kurulur. |
| `Modules/Hurda/HurdaModulu.cs:170-199` `SureTakibi` | `DuzenlemeSonTarihi <= simdi+2` olan **her** pusula listelenir; `< simdi` olanlar "gecikmiş" sayılır → **7 günden eski bütün pusulalar "süresi DOLMUŞ"** görünür (iptal ayrımı da yok). |
| `HurdaModulu.cs:49` | Listede `gecikti = simdi > g.DuzenlemeSonTarihi` — aynı yanlış anlam. |
| `Kuyumcu.Web/Pages/Panel.razor:254-256`, `:573-575` | Panel bu sayıyı **KRİTİK** satır olarak basıyor: "gider pusulasının 7 günlük düzenleme süresi DOLDU" (`Kuyumcu.Core/I18n/Sozlukler.cs:3792`). |
| `Pages/GiderPusulalari.razor:38-47`, `:167` | Aynı sayaç kırmızı StatKart + "süre geçti" uyarısı. |
| `HurdaModulu.cs:336-353` `Iptal` | İptal yalnız belgeyi hükümsüz kılar; düzeltme yolu: **"Doğru bilgilerle YENİ bir hurda alışı girin"** → yeni hareket = stok/kasa **ikinci kez** (iptal edilen alış hareketi yerinde kalır, `HurdaModulu.cs:215-221`). |
| `HurdaModulu.cs:228-237` | Teslim tarihi bilerek kilitli (7 günü uzatmasın). |
| `Data/GiderPusulasi.cs:106` | `DuzeltilenId` kolonu VAR ama iptal→yeni akışında doldurulmuyor (yeni alış bağımsız). |
| MÜKELLEF satıcı | `GiderPusulasiKapisi.cs:36` `TedarikciFaturaNo` — isteğe bağlı; fatura gelmezse takip yok. |

### 2.2 Stopaj borcu (çelişki 3b)
| Kanıt | Ne gösteriyor |
|---|---|
| `Data/GiderPusulasi.cs:14-20` | "ESNAF_MUAF → %2 stopaj (hurda mal alımlarında GVK md.94)"; **kaynaklar blog/danışmanlık sitesi** (`:26-31` "Özdoğrular", "Proventus") — birincil değil. |
| `GiderPusulasiKapisi.cs:169-191` | Stopaj hesaplanıyor, net ödeniyor; stopaj tutarı **hiçbir deftere/borca** yazılmıyor. |
| `HurdaModulu.cs:159-161` | Belgede: "Kesilen stopaj vergi dairesine beyan edilmelidir; bu sürümde vergi borcu hesabı tutulmamaktadır." |
| `appsettings.json:122-126` | `EsnafMuafStopajOran 0.02`, `NihaiTuketiciStopajOran 0.0` — tarihsiz. |
| `mevzuat-gereksinimleri.md:132` (K2 soru 2) | "Hurda alımında stopaj oranları nedir?" — mali müşavire gitmiş, cevapsız (S-4). |

### 2.3 KDV 23/e (çelişki 2 + 8)
| Kanıt | Ne gösteriyor |
|---|---|
| `Kuyumcu.Web/Pages/Faturalar.razor:176-187` | Özel matrah beyanı **sayfa sonunda kapalı `Katlanir`** (ZO-BELGE, kurucu göz #2 kararıyla oraya indirildi). Hata varsa üstte açık kart (`:38-43`). |
| `Modules/Fatura/KdvAyarlari.cs:73-81` `KaynakBeyani` | "HARİÇ… mali müşavir teyidi (K-10) bekleniyor… `Kdv:OzelMatrahKodu = 805` yazılarak açılır" — geliştirici/yapılandırma dili. |
| `Modules/Fatura/OzelMatrahMotoru.cs:15` | "KURAL (KDVK md.23/e — ⚠ DOGRULANMALI, birincil metin okunmadı)". |
| `appsettings.json:117-121` | `Kdv.OzelMatrahKodu: ""` (KAPALI). |
| `Kuyumcu.Core/I18n/Sozlukler.cs:4587`; `Sozlukler.Q105.cs:73` | Pro ve Basit metinleri; Basit metin sade, Pro metin "K-10", "yapılandırma" jargonu. |

### 2.4 KMTS (çelişki 6)
| Kanıt | Ne gösteriyor |
|---|---|
| `Data/Entities.cs:670` (`urunler.kmts_kodu`), `:939` (`hareketler.kmts_kodu`) | Opsiyonel, 100 karakter serbest metin. |
| `Modules/Urun/UrunModulu.cs:124`, `:277`, `:453`, `:740-741`; `Modules/Satis/SatisModulu.cs:288`, `:1214` | Yalnız `Trim`; biçim denetimi yok. |
| `Kuyumcu.Web/Pages/Urunler.razor:470-472`, `:901-903` | Ürün formunda (yeni + düzenle) alan **her zaman** çiziliyor. |
| `Kuyumcu.Core/I18n/Sozlukler.cs:3983` | İpucu: "Opsiyonel; **sunucu doğrulamaz**, olduğu gibi saklanır…" — jargon. |
| `Modules/Disaaktarim/Kaynaklar.cs:113`, `Modules/Iceaktarim/Kaynaklar.cs:248` | Dışa/içe aktarımda kolon. |
| `dokumanlar/02-mimari/adr/0006-kmts-kapsam-disi.md` | Perakende kapsam dışı; 12.08.2026 değişikliği **incelenmedi**. |

---

## 3. Mevzuat

> Doğrulama: 30.09.2026, birincil metinler okunarak.

### 3.1 Doğrulanan hükümler
| # | Hüküm | Birincil kaynak | Alıntı (≤15 kelime) |
|---|---|---|---|
| M-1 | Gider pusulası belge düzenleme zorunluluğu olmayanlardan alımda | VUK md.234 (7338 s. K. ile değişik) · https://www.mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf | "bu Kanun kapsamındaki belgeleri düzenleme zorunluluğu bulunmayanlara" |
| M-2 | **Süre: teslimden itibaren azami 7 gün** | VUK md.234 · aynı URL | "malın teslimi veya hizmetin yapıldığı tarihten itibaren azami yedi gün içinde düzenlenir" |
| M-3 | **Süresinde düzenlenmeyen pusula hiç düzenlenmemiş sayılır** | VUK md.234 · aynı URL | "Bu süre içerisinde düzenlenmeyen gider pusulası hiç düzenlenmemiş sayılır." |
| M-4 | İçerik: işin mahiyeti, emtianın cins/nev'i, miktar, bedel; iki tarafın ad-soyad/unvan ve adresi; tarih; iki nüsha, seri-sıra no | VUK md.234 · aynı URL | "işin mahiyeti, emtianın cins ve nev'i ile miktar ve bedelini" |
| M-5 | Esnaf muaflığındakilerden **mal alımında** tevkifat | GVK md.94/13 · https://www.mevzuat.gov.tr/mevzuatmetin/1.4.193.pdf | "Esnaf muaflığından (...) yararlananlara mal ve hizmet alımları karşılığında yapılan ödemelerden" |
| M-6 | Kapı kapı hurda toplayanlar esnaf muafıdır | GVK md.9/1-7 · aynı URL | "münhasıran kapı kapı dolaşmak suretiyle her türlü hurda maddeyi toplayarak" |
| M-7 | **Oran: hurda mal alımı %2** (ilk metin) | 2009/14592 BKK, RG 03.02.2009/27130, 13. bent · https://www.resmigazete.gov.tr/eskiler/2009/02/20090203-3.htm | "b) Hurda mal alımları için % 2, c) Diğer mal alımları için % 5" |
| M-8 | Tevkifat beyanı ertesi ay 23'ü (kanun), ödeme 26'sı; uygulamada muhtasar ve prim hizmet beyannamesi 26'sı | GVK md.98, md.119 · aynı GVK URL; GİB süreler belgesi · https://cdn.gib.gov.tr/api/gibportal-file/file/getFileResources?objectKey=arsiv%2Fyardim-kaynaklar%2Fyararli-bilgiler%2FBeyannameVermeveOdemeSureleri.pdf | "ertesi ayın yirmiüçüncü günü akşamına kadar" · "İzleyen ayın 26 ncı günü saat 23.59'a kadar verilir." |
| M-9 | **KDVK 23/e:** ziynet eşyası ve sikke altın tesliminde matrah = bedel − **külçe altın bedeli** | KDVK md.23/e · https://www.mevzuat.gov.tr/mevzuatmetin/1.5.3065.pdf | "sikke altınların teslim ve ithalinde matrah, külçe altın bedeli düşüldükten sonra kalan miktardır" |
| M-10 | **Has bedeli fiyatı: Borsa İstanbul'da son resmi işgünü külçe altın KAPANIŞ fiyatı**, mamulün ayarına göre | KDV Genel Uygulama Tebliği §III/A-4.2.1 · https://www.mevzuat.gov.tr/File/GeneratePdf?mevzuatNo=19631&mevzuatTur=Teblig&mevzuatTertip=5 | "Borsa İstanbul'da işlem yapılan son resmi işgününde külçe altın için oluşan kapanış fiyatı" |
| M-11 | İşçilik (hurda altından imalat dahil) ayrıca KDV'ye tabi | Aynı Tebliğ §III/A-4.2.1 | "külçe altından (hurda altın dahil) altın mamulü … imalatı ile uğraşanların" |
| M-12 | **Gümüş eşyada** da özel matrah: bedel − külçe **gümüş** bedeli (KDVK 23/g yetkisi) | Aynı Tebliğ §III/A-4.2.2 | "teslim bedelinden külçe gümüş bedeli düşüldükten sonra kalan miktar" |
| M-13 | **805** = altın ziynet/sikke teslimi özel matrah kodu (e-belge) | GİB UBL-TR Kod Listeleri V1.43, Özel Matrah Kodları · ebelge.gib.gov.tr/dosyalar/kilavuzlar/UBLTR_1.2.1_Kilavuzlar.zip | "805 Altından Mamül veya Altın İçeren Ziynet Eşyaları İle Sikke Altınların Teslimi" |
| M-14 | KMTS 2026 değişiklikleri var: Tebliğ 2026/1 (RG 29.07.2026/33324) + 12.08.2026 Usul-Esaslar değişikliği; yükümlüler **rafineriler ve KMTS'ye tabi tüzel kişiler** | https://www.resmigazete.gov.tr/eskiler/2026/07/20260729-2.htm · https://www.kmts.gov.tr/duyuru/kiymetli-maden-takip-sistemi-uygulamasina-iliskin-usul-ve-esaslar-degisikligi-hakkinda | "Rafineriler ve KMTS'ye tabi tüzel kişiler tarafından … stok giriş ve çıkışlarının" |

**Ürüne etkileri (yeni bulgular dâhil):**
- **M-3 → 7 gün kuralı "uyarı" olamaz:** süresi geçmiş pusula hukuken yoktur. Bugün pusula alışla aynı anda düzenlendiği için bu risk yalnız **iptal → yeniden düzenleme** yolunda doğar (§ 4.1). Son günden sonra yeniden düzenleme → **engel** (§ 4.1 güncel kural).
- **M-10 → YENİ UYUMSUZLUK:** has bedeli bugün işlemin **piyasa kur kaydından** (`KurSnapshot.AltinHasFiyati`, kaynaklar `ALTINKAYNAK, TRUNCGIL, HAREM, KAPALICARSI, IAB, MANUAL` — `Data/Entities.cs:113`, `:341`; kullanım `Modules/Fatura/FaturaModulu.cs:364-369`) hesaplanıyor. Tebliğ **Borsa İstanbul son resmi işgünü kapanış** fiyatını ister. Fark her iki yönde KDV hatası doğurur → § 4.3b.
- **M-12 → YENİ OLASI HATA (çıkarım, test ile doğrulanacak):** `FaturaModulu.HasBacaklariniTopla` (`:344-370`) `Math.Abs(h.HasGrami) > 0` olan **her** satırı `kur.AltinHasFiyati` ile çarpıyor; `Hareket.Maden` süzülmüyor (FaturaModulu'nda `Maden` 0 isabet). Gümüş ayarları has hesaplar (`Kuyumcu.Core/Domain/Ayarlar.cs:110-115`, `:87` `HasHesaplanir = true` varsayılanı) → gümüş kalemli KUYUM faturada gümüş has gramı **altın fiyatıyla** matrahtan düşülür → **KDV eksik hesaplanır**. § 4.3b.
- **M-7/M-5/M-6 → %2 oranı** birincil ilk metinle doğrulandı (kodun "Özdoğrular/Proventus" ikincil kaynağı yerine bu URL yazılır).
- **M-14 → KMTS:** perakende kuyumcuya yükümlülük incelenen metinlerde **yok** → alan gizlenir (§ 4.4); ADR-0006 "yeniden doğrulandı 30.09.2026" notu alır.

### 3.2 Teyit edilemeyenler
| # | Konu | Durum |
|---|---|---|
| T-1 | Mükellef olmayan gerçek kişiden (esnaf muafı değil) hurda/ziynet alımında tevkifat getiren bir Cumhurbaşkanı Kararı (GVK 94/18, 94/20 yetkisi) | **DOĞRULANAMADI** — md.94'te özel bent yok; ürün %0 (TEYITSIZ) |
| T-2 | 2009/14592 BKK 13. bent oranlarında 2009 sonrası değişiklik | **DOĞRULANAMADI** (yalnız ilk metin) |
| T-3 | "26'sı" beyan süresinin dayanak tebliği; üç aylık muhtasar seçeneğinin kuyumcuya uygulanıp uygulanmadığı | **DOĞRULANAMADI** — GİB süreler belgesiyle 26 kullanılır, TEYITSIZ |
| T-4 | İptal edilen gider pusulasının yerine **aynı alış için** yeni pusula düzenlenmesi usulü (7 gün içinde) | **DOĞRULANAMADI** — md.234'te iptal/yeniden düzenleme hükmü yok; mali müşavir teyidi |
| T-5 | "Son resmi işgünü kapanışı"nın teslimin **aynı günü** mü (seans kapanmışsa) yoksa **önceki** işgünü mü olduğu; kapanış fiyatının hangi birimde (TL/kg, USD/ons) ve hangi kurla gram TL'ye çevrileceği | **DOĞRULANAMADI** — mali müşavir + Borsa İstanbul bülteni |
| T-6 | Borsa İstanbul kapanış verisinin ticari kullanım koşulu / lisansı | **DOĞRULANAMADI** — kurucu kalemi (K-3 kur verisi lisansı ile birlikte) |
| T-7 | KMTS'nin perakende kuyumcuya yükümlülük getirmesi | **DOĞRULANAMADI** (incelenen metinlerde yok) |
| T-8 | 805 kodunun kullanımının **mali müşavir onayı** (kod listesi doğrulandı; kuyumcunun kendi faturalarına uygulanması yorumu) | K-10 kurucu kalemi — açık |

---

## 4. Kapsam

### 4.1 Gider pusulası süre takibinin anlamını düzelt (çelişki 3a)
- **Kural:** Pusula kaydı varsa ve iptal edilmemişse süre **karşılanmıştır** (belge teslimle aynı anda düzenlendi). Süre takibine girenler yalnız:
  1. **İptal edilmiş ve yerine yenisi düzenlenmemiş** pusula → son gün = **orijinal** `TeslimTarihi + GIDER_PUSULASI_GUN(teslim günü)`.
  2. **MÜKELLEF** satıcıdan alış, `TedarikciFaturaNo` boş → "satıcı faturası bekleniyor" (son gün § 3'teki fatura düzenleme süresine göre; teyitsizse yalnız bilgi, geri sayım yok).
- **Veri:** `gider_pusulalari.duzeltilen_id` artık doldurulur. **Son günden sonra yeniden düzenleme ENGELLENİR** (409): VUK 234'e göre süresinde düzenlenmeyen pusula "hiç düzenlenmemiş sayılır" (§ 3.1 M-3) — geçersiz belge basmak yanlış güven verir. Mesaj: "7 gün geçti; bu alış için artık gider pusulası düzenlenemez. Mali müşavirinize danışın." + Uyum ekranında "belgesiz alış" kaydı (kapatma gerekçeli). Panel geri sayımı (§ 4.1) bu duruma düşmeden önce uyarır.
- **Yeni uç `POST /api/v1/gider-pusulalari/{id}/yeniden-duzenle`** (SAHİP, MÜDÜR; hurda yetkili roller): iptal edilmiş pusulanın **aynı `hareket_id`**'si için doğru satıcı bilgileriyle **yeni pusula**. Tutar/gram/has/ayar/teslim tarihi **eski belgeden** gelir (değiştirilemez — tutar hatası hareket stornosuyla düzeltilir, bu uçla değil). Yeni hurda alışı **girilmez** → stok/kasa ikinci kez yazılmaz.
- `Iptal` yanıtındaki `duzeltmeYolu` metni (`HurdaModulu.cs:350-353`) "Yeniden düzenle" yoluna çevrilir; "YENİ bir hurda alışı girin" cümlesi kalkar.
- `SureTakibi` (`HurdaModulu.cs:170-199`) yalnız yukarıdaki iki kümeyi döndürür; `gecikti/kalanGun` (`:49-50`) yalnız iptal edilmiş + yenisiz pusulada anlamlıdır, diğerlerinde alan **dönmez**.
- Panel metni: "Yerine yenisi düzenlenmemiş iptal edilmiş gider pusulası: 1 — son gün 03.10" (sayı 0 ise satır yok).

### 4.2 Stopaj borcu (çelişki 3b)
- **Türetme (yeni tablo açmadan borç, tabloyla ödeme):**
  - Dönem borcu = `SUM(stopaj_tutar)` — `gider_pusulalari`, `iptal_edildi = 0`, `teslim_tarihi` yerel ayı = dönem (iptal edilen + yeniden düzenlenen pusulada stopaj tek sayılır; test).
  - **Yeni tablo `stopaj_odemeleri`** (`IKiraciVarlik`, append-only): `donem` (YYYY-MM), `tutar`, `odeme_tarihi`, `hareket_id` (kasa/banka çıkışı — mevcut GİDER yolu, kategori "Vergi — stopaj"), `tahakkuk_fis_no` (isteğe bağlı), `kullanici_id`, `olusturuldu`. Düzeltme = ters kayıt.
  - Kalan = borç − ödemeler. **Son ödeme günü** = dönemi izleyen ayın `STOPAJ_BEYAN_GUNU`'ncü günü (YASAL-1; § 3'teki hükme göre; üç aylık beyan seçeneği § 3.2).
- **Uçlar:** `GET /api/v1/stopaj/donemler?yil=` (SAHİP, MÜDÜR, MUHASEBE) → dönem · kesilen · ödenen · kalan · son gün · pusula sayısı. `POST /api/v1/stopaj/odemeler` → mevcut gider/kasa yazma yolunu **çağırır** (ikinci kasa yazma yolu açılmaz; `denetim.sh Hareketler.Add=1` korunur). `GET /api/v1/stopaj/donemler/{donem}/liste.csv` → mali müşavir için pusula no · tarih · satıcı adı · TCKN (tam; **okuma izi zorunlu**, dışa aktarım jetonu P-14 deseni) · brüt · oran · stopaj.
- **Zamanlanmış iş `STOPAJ_HATIRLATMA`** (PAKET-06 çatısı): son güne 5 gün kala ve son gün, kalan > 0 ise SAHİP/MUHASEBE bildirimi. Son gün geçmiş ve kalan > 0 → Panel KRİTİK.
- **Kasa görünümü:** Panel/kasa kartında "Bu ay kesilen stopaj (vergi dairesine ait): X TL" — kasa bakiyesi **değişmez** (para kasada), ama ayrıca gösterilir ki patron o parayı harcanabilir sanmasın.
- Stopaj oranları YASAL-1 tablosundan, **teslim gününün** oranıyla (geçmiş belge yeniden hesaplanmaz; belge üzerindeki `stopaj_oran` otoritedir).

### 4.3 KDV 23/e görünürlüğü (çelişki 2 + 8)
- **Faturanın kendisinde (en önemli yer):** KUYUM türlü her faturanın ekran ve baskı görünümünde toplamlar bloğunda tek satır: **"Has altın bedeli (KDV'ye tabi değil): X TL"**, altında "KDV matrahı: Y TL". Değerler `OzelMatrahMotoru`'ndan (tek motor; yeni formül yok). Baskı şablonu (P-5 belge motoru) aynı satırı taşır.
- **Faturalar sayfası:** sayfa sonundaki `Katlanir` (`Faturalar.razor:176-187`) **kaldırılır**; yerine yalnız kiracıda KUYUM fatura varsa ve kod KAPALI ise başlık altında tek satır **rozet**: "Özel matrah kodu: mali müşavir onayı bekleniyor" → tıklayınca YASAL-1 "Yasal sınırlar" sayfasındaki `KDV_OZEL_MATRAH_KODU` satırı (uzun metin orada, katlanır "Neden?" içinde). Hata durumunda üstteki açık kart (`:38-43`) korunur (UX-S28).
- **Metinler:** `KdvAyarlari.KaynakBeyani` geliştirici dili (`Kdv:OzelMatrahKodu`, "K-10") ekrandan kalkar; o metin yalnız sunucu günlüğü / sistem ucu içindir. Ekran metni Basit ve Pro'da aynı sade cümle (`Sozlukler.Q105.cs:73` tabanı).
- **Motor yorumu:** `OzelMatrahMotoru.cs:15` "⚠ DOGRULANMALI" notu § 3.1'deki birincil metinle güncellenir (doğrulanırsa `DOGRULANDI` + URL; doğrulanamazsa not kalır ve "Yasal sınırlar"da rozet "teyit bekliyor").
- **805 kodu:** § 3'te GİB kod listesinde birincil olarak bulunursa YASAL-1 tablosunda `KDV_OZEL_MATRAH_KODU` satırı `teyit=DOGRULANDI (kod listesi)` ama **`KAPALI`** kalır — açılış yine mali müşavir imzasıyla (plan kuralı, `KdvAyarlari.cs:13-21`). Açılış = sistem ucu ile ileri tarihli satır (YASAL-1).

### 4.3b Has bedelinin fiyatı ve madeni (YENİ — § 3.1 M-10, M-12)
- **Maden süzgeci (ivedi, küçük):** `HasBacaklariniTopla` altın bacaklarını `Hareket.Maden == ALTIN` ile süzer; gümüş bacakları **ayrı** "külçe gümüş bedeli" olarak (M-12) gümüş fiyatıyla hesaplanır; gümüş fiyatı yoksa **fail-closed 400** (bugünkü altın deseni `FaturaModulu.cs:364` ile aynı). Platin/paladyum için özel matrah hükmü bulunmadı → bu madenlerde has bedeli **düşülmez** (tam bedel matraha girer; tutucu — fazla KDV, ceza değil) ve faturada "özel matrah uygulanmadı" satırı yazar.
- **Fiyat kaynağı:** YENİ tablo `borsa_kapanis_fiyatlari` (sistem geneli): `gun` (DATE, işgünü), `maden` (ALTIN/GUMUS), `birim_fiyat_tl_gram_has` (DECIMAL(15,4)), `kaynak` (bülten adı/URL), `olusturuldu`. Güncelleme: `POST /sistem/borsa-kapanis` (SistemAnahtari) ya da lisans çıkarsa otomatik çekme (ayrı iş). `OzelMatrahMotoru` **değişmez** (fiyatı parametre alır); yalnız `HasBacaklariniTopla` fiyatı `KurSnapshot` yerine **teslim gününden önceki son işgünü** kapanışından okur (T-5 kararına göre "aynı gün / önceki gün" tek yerde parametre: YASAL-1 `KDV_KAPANIS_GUN_KAYDIRMA`).
- **Geçiş (tutucu):** tablo boşsa ya da o gün için kapanış yoksa → bugünkü davranış (`KurSnapshot`) **sürer** ama fatura satırında ve "Yasal sınırlar"da "has bedeli piyasa kurundan hesaplandı — borsa kapanışı yok" notu görünür (sessiz değil). Kurucu kararı (K-3 lisans) gelince tablo dolar ve kural kendiliğinden devreye girer.
- **Geçmiş faturalar yeniden hesaplanmaz** (kesilmiş belge değişmez); fark raporu: "Son 12 ay KUYUM faturalarında piyasa kuru ile borsa kapanışı arasındaki KDV farkı" (mali müşavire bilgi, SAHİP ekranı) — kapanış verisi varsa.

### 4.4 KMTS alanı (çelişki 6)
- **Varsayılan gizli:** kiracı ayarı `kmtsKullanimi` (Ayarlar → İşletme, SAHİP; varsayılan **kapalı**). Kapalıyken `Urunler.razor:470-472` ve `:901-903` alanları **çizilmez**; veri silinmez, dışa aktarım kolonu korunur. Açıkken alan Pro'da "+ Ek bilgiler" katlanırında, Basit'te hiç görünmez.
- **Doğrulama tek yerden:** YASAL-1 anahtarı `KMTS_KOD_DESENI` (düzenli ifade). **Boş/KAPALI** iken sunucu yalnız uzunluk ve yazdırılabilir karakter denetler (bugünkü davranış). Resmî biçim yayımlanınca sistem ucuyla desen yazılır → `UrunModulu` ve `SatisModulu` aynı yardımcıdan (`Common/KmtsKodu.cs`, YENİ) 400 döner; **mevcut kayıtlar** desenle taranır, uymayanlar Uyum ekranında listelenir (silinmez).
- **İpucu metni** (`Sozlukler.cs:3983`) jargonsuz: "Rafineriden aldığınız külçe/sikkede yazan takip kodu. Yoksa boş bırakın." (içerik § 3'teki birincil tanıma göre son hâlini alır).
- **12.08.2026 değişikliği:** § 3'te birincil olarak okunursa ADR-0006'ya "yeniden doğrulandı: 2026-09-30" + sonuç yazılır. Perakendeye yükümlülük getiriyorsa bu paket **yetmez** → ayrı entegrasyon paketi açılır (uydurma entegrasyon yok, ADR-0012 ruhu).

### 4.5 Ekranlar — ekranda nasıl görünür
| Yer | Görünüm | Tür |
|---|---|---|
| Panel | Yanlış "pusula süresi doldu" KRİTİK satırı **kaybolur**; yalnız iptal-yenisiz varsa "1 pusula yeniden düzenlenmeli — son gün 03.10" | otomatik |
| Panel | Ayın 20'sinden itibaren (ya da son güne 5 gün kala): "Ekim stopajı 1.240 TL — 26 Kasım'a kadar ödenmeli" + "Ödendi olarak kaydet" | otomatik + tek düğme |
| Gider pusulaları | Kırmızı "gecikmiş" kartı yerine: "İptal edilip yeniden düzenlenmeyen: N"; satırda iptal edilmiş pusulada birincil düğme **"Yeniden düzenle"** | otomatik + tek düğme |
| Gider pusulaları → "Stopaj" sekmesi (YENİ) | Dönem tablosu; "Mali müşavire liste" (CSV) ve "Ödendi" düğmeleri; geçmiş dönemler katlanır | otomatik |
| Fatura (ekran + baskı) | "Has altın bedeli (KDV'ye tabi değil)" satırı | otomatik, görünür |
| Faturalar listesi | Başlık altında tek satır sarı rozet (kod kapalıyken, KUYUM fatura varsa) | bilgi |
| Ürün formu | KMTS alanı yok (varsayılan); ayar açıksa Pro'da katlanırda | sade |

---

## 5. Kapsam dışı
- Muhtasar beyannamenin GİB'e gönderimi (e-Beyanname entegrasyonu yok; mali müşavir yapar).
- Stopajın muhasebe fişine (yevmiye) otomatik aktarımı — ürün defter-i kebir tutmuyor.
- e-Fatura/e-Arşiv UBL'de özel matrah alanı (PAKET-E-BELGE Faz B).
- KMTS sistemine elektronik bildirim/entegrasyon.
- Hurda alış hareketinin tutar hatasının düzeltilmesi (storno yolu — mevcut).

---

## 6. Kabul ölçütleri
| # | Ölçüt | Kanıt |
|---|---|---|
| K-1 | 10 gün önce düzenlenmiş, iptal edilmemiş pusula → `sure-takibi` gecikmiş **0**, Panel'de satır yok (bugün 1 ve KRİTİK) | HTTP + bUnit |
| K-2 | İptal edilmiş, yenisiz, teslimi 5 gün önce → yaklaşan 1, son gün = teslim + 7 | HTTP testi |
| K-3 | `yeniden-duzenle` → yeni pusula aynı `hareket_id`, `duzeltilen_id` = eski; `hareketler` satır sayısı **değişmez**; hurda has bakiyesi değişmez | HTTP + SQL |
| K-4 | Teslimden 9 gün sonra yeniden düzenleme → **409**, yeni pusula 0; Uyum'da "belgesiz alış" kaydı 1. Teslimden 6 gün sonra → 200 | HTTP testi |
| K-5 | İptal edilmemiş pusulada `yeniden-duzenle` → 409 | HTTP testi |
| K-6 | Ekim'de ESNAF_MUAF 3 pusula (stopaj 200 + 300 + 740) + 1 iptal-yeniden düzenlenen (stopaj 100) → `stopaj/donemler` Ekim kesilen **1.340** (iptal edilen sayılmaz, yenisi sayılır) | DB testi |
| K-7 | 1.340 TL ödeme kaydı → kalan 0; kasada 1 GİDER hareketi, `stopaj_odemeleri` 1 satır; `denetim.sh Hareketler.Add` sayacı değişmez | HTTP + SQL + denetim |
| K-8 | Kalan > 0 ve son gün geçti → Panel KRİTİK; kalan 0 → satır yok | bUnit |
| K-9 | CSV: satır sayısı = dönemin iptal edilmemiş stopajlı pusula sayısı; indirme okuma izi yazar; TEZGÂHTAR 403 | HTTP testi |
| K-10 | KUYUM fatura ekran ve baskı HTML'inde "KDV'ye tabi değil" satırı ve `HasDeger` değeri motorla aynı (fark 0,00) | bUnit + tarayıcı |
| K-10b | Gümüş (G925, 10 g) + altın (22 ayar, 5 g) kalemli KUYUM fatura: has bedeli = altın has × altın kapanış + gümüş has × gümüş kapanış; **gümüş satırı altın fiyatıyla çarpılmaz** (bugünkü koda karşı yazılırsa KIRMIZI beklenir — çıkarımın doğrulaması) | DB testi |
| K-10c | `borsa_kapanis_fiyatlari`'nda Cuma kapanışı var, teslim Pazar → Cuma kapanışı kullanılır; tablo boş → piyasa kuru + faturada not satırı | DB + bUnit |
| K-11 | Faturalar sayfasında `#fk-ozel-matrah-beyani` Katlanir **0**; KUYUM fatura varken rozet 1, yokken 0 | bUnit |
| K-12 | Ekranda "Kdv:OzelMatrahKodu", "K-10", "805", "yapılandırma" dizgeleri 0 (Basit + Pro) | bUnit metin taraması |
| K-13 | `kmtsKullanimi=false` → ürün formunda `#u-kmts` ve `#d-kmts` 0; `true` → Pro'da 1, Basit'te 0 | bUnit |
| K-14 | `KMTS_KOD_DESENI` dolu (test deseni) → uymayan kod 400 (ürün ve satış yolu); boşken serbest | HTTP testi |
| K-15 | Tarayıcı: Panel, Gider pusulaları (Stopaj sekmesi), Fatura baskısı, Ürün formu — 1280/375 · Basit/Pro · açık/koyu, taşma 0 | gerçek tarayıcı |
| K-16 | `dotnet test` 0 kırmızı; `denetim.sh` TEMİZ (`OZEL_MATRAH_BEYANSIZ_EKRAN` sayacı yeni yerleşimle güncellenmiş) | kapı |

---

## 7. Mutasyon önerileri
1. `SureTakibi` eski koşula (`DuzenlemeSonTarihi <= esik`) dönsün → K-1 KIRMIZI.
2. `yeniden-duzenle` yeni hurda hareketi yazsın → K-3 KIRMIZI.
3. Son gün yeni belgenin tarihinden hesaplansın ya da son gün kontrolü silinsin → K-2/K-4 KIRMIZI.
4. Stopaj toplamına iptal edilenler dahil edilsin → K-6 KIRMIZI.
5. Stopaj ödemesi kasaya doğrudan `Hareketler.Add` ile yazılsın → K-7 (denetim sayacı) KIRMIZI.
6. Fatura baskısından has satırı kaldırılsın → K-10 KIRMIZI.
7. Katlanır geri eklensin → K-11 KIRMIZI.
8. KMTS alanı ayar bakılmadan çizilsin → K-13 KIRMIZI.
9. `HasBacaklariniTopla`'dan maden süzgeci silinsin → K-10b KIRMIZI.
10. Kapanış günü teslim günü yerine bugünden seçilsin → K-10c KIRMIZI.

---

## 8. Riskler
- **Stopaj oranı teyitsiz** (S-4): yanlış oranla kesilen stopaj hem satıcıya eksik ödeme hem vergi farkı demektir. Bu paket oranı **değiştirmez**, yalnız görünür kılar; oran YASAL-1'de "teyit bekliyor" rozetiyle durur.
- **Yeniden düzenleme** mevzuatta "iptal + yeni belge" olarak kabul görmüyorsa (§ 3.2) akış mali müşavirle teyit edilmeli; akış yine de stok çift yazımını kapattığı için bugünkünden güvenlidir.
- **Faturalar sayfası ZO-BELGE ve kurucunun fatura yuvarlama görevinin alanı** (ORKESTRATOR-DURUM 29.09 06:45) → çakışma; şerit sırası § 10.
- **Kasa algısı:** stopaj kasada fiziksel durur; ayrı gösterim kasa bakiyesini değiştirmez — kullanıcı iki rakamı karıştırabilir → metin: "vergi dairesine ait".
- **KMTS yükümlülük değişikliği** § 3'te doğrulanırsa kapsam büyür (ayrı paket).

---

## 9. Tahmini süre (en çok 2 tur)
- **Y4-A** (4.1 pusula süresi + yeniden düzenle): tur 1 ~3 saat, tur 2 ~1,5 saat.
- **Y4-B** (4.2 stopaj): tur 1 ~4 saat, tur 2 ~2 saat.
- **Y4-C** (4.3 23/e görünürlük + 4.4 KMTS): tur 1 ~3 saat, tur 2 ~1,5 saat.
- **Y4-D** (4.3b maden süzgeci + borsa kapanış tablosu): tur 1 ~3 saat, tur 2 ~1,5 saat. **Maden süzgeci en öncelikli** (olası KDV eksik hesabı) — K-10b testi önce ağaçta koşturulup çıkarım doğrulanmalı.
- Toplam ~2 iş günü, üç şerit (tahmin; ölçülmedi).

---

## 10. Şerit dosya alanı önerisi
- **Y4-A:** `Modules/Hurda/HurdaModulu.cs` (yalnız `GiderPusulasiServisi` bölümü: `SureTakibi`, `Iptal`, yeni `YenidenDuzenle`, controller `:377-430`), `Modules/Hurda/GiderPusulasiKapisi.cs` (yalnız `Kaydet` imzasına `duzeltilenId`), `Data/GiderPusulasi.cs` (yalnız yorumlar — kaynak satırları birincil URL ile; migration YOK, `duzeltilen_id` zaten var `:106`), `Kuyumcu.Web/Pages/GiderPusulalari.razor`, `Pages/Panel.razor` (yalnız `:254-256`, `:573-575` pusula satırları), `Kuyumcu.Core/I18n/Sozlukler.cs` (yalnız `Pusula*`/`PusulaSuresiGecti` anahtarları).
- **Y4-B:** `Data/StopajOdemesi.cs` (YENİ), `Modules/Hurda/StopajModulu.cs` (YENİ), `Modules/Gider/GiderModulu.cs` (yalnız sistem kategorisi "Vergi — stopaj" + çağrılabilir yazma yardımcısı; yeni kasa yolu YOK), `Modules/Isler/Isler/StopajHatirlatmaIsi.cs` (YENİ), `Data/KuyumcuDbContext.cs` (DbSet — YASAL-1/Y2-B ile sıralı), `Kuyumcu.Web/Pages/GiderPusulalari.razor` (Stopaj sekmesi — Y4-A ile aynı dosya → **Y4-A'dan sonra**), `Pages/Panel.razor` (1 satır), `denetim.sh` (`BEKLENEN_VARLIK` +1).
- **Y4-C:** `Kuyumcu.Web/Pages/Faturalar.razor` (yalnız `:38-43`, `:176-187`, `:958-990` özel matrah bölümü), fatura baskı şablonu (`Kuyumcu.Web/Belgeler/` altındaki fatura belgesi — uygulayıcı grep), `Modules/Fatura/KdvAyarlari.cs` (metin), `Modules/Fatura/OzelMatrahMotoru.cs` (yalnız yorum), `Kuyumcu.Web/Pages/Urunler.razor` (yalnız `:470-472`, `:901-903`), `Common/KmtsKodu.cs` (YENİ), `Modules/Urun/UrunModulu.cs` + `Modules/Satis/SatisModulu.cs` (yalnız KMTS satırları `:453`, `:740`, `:1214`), `Pages/Isletme.razor` veya `Ayarlar.razor` (kmtsKullanimi anahtarı — uygulayıcı mevcut işletme ayarı deposunu seçer), `adr/0006-…md`, `Sozlukler.cs` / `Sozlukler.Q105.cs` (ilgili anahtarlar).
- **Y4-D:** `Modules/Fatura/FaturaModulu.cs` (yalnız `HasBacaklariniTopla` `:344-370`), `Data/BorsaKapanisFiyati.cs` (YENİ), `Data/Migrations/*_BorsaKapanis.cs`, `Modules/Sistem/SistemModulu.cs` (yalnız yeni uç — YASAL-1 ve Y3-C ile aynı dosya → sıralı), testler `OzelMatrahMadenTestleri.cs` (YENİ).
- **Sıra:** YASAL-1 → (Y4-D ∥ Y4-A ∥ Y4-C) → Y4-B. Y4-D, YASAL-1'den bağımsız başlayabilir (maden süzgeci kısmı). Faturalar.razor'a dokunan başka açık şerit varsa (ZO-BELGE kapanışı, fatura yuvarlama görevi) Y4-C onlardan **sonra**.

---

## 11. Kurucu / mali müşavir kararı gereken noktalar
- **S-4:** Hurda alımında stopaj oranları (ESNAF_MUAF %2, nihai tüketici %0) — § 3'e göre.
- **S-3 / K-10:** KDVK 23/e metni ve 805 kodu birincil kaynakla doğrulandı; açık kalan: kuyumcunun faturalarında 805'in açılmasına **mali müşavir onayı**.
- **YENİ — has bedeli fiyatı:** Tebliğ Borsa İstanbul son işgünü kapanışını istiyor (§ 3.1 M-10); ürün piyasa kurunu kullanıyor. (a) Kapanış verisinin kaynağı/lisansı (kurucu, K-3 ile), (b) "son işgünü"nün aynı gün mü önceki gün mü olduğu (mali müşavir, T-5).
- **YENİ — gümüş/platin faturası:** gümüş için 23/g özel matrahı uygulanacak mı; platin/paladyumda has bedeli düşülmemesi (tutucu) kabul mü?
- **Yeniden düzenleme:** iptal edilen gider pusulasının yerine aynı alış için yeni pusula düzenlenmesi usulü (§ 3.2).
- **Stopaj beyan dönemi:** aylık mı üç aylık mı (işletmenin durumuna bağlı) — kiracı ayarı olarak mı sorulsun?
- **KMTS 12.08.2026 değişikliği:** § 3 sonucu perakendeye yükümlülük getiriyorsa entegrasyon paketi ve bütçe.
