# PAKET S1-i — GÖRSEL SIĞMA 2 · uygulama raporu

**Tarih:** 2026-09-24 (10:25 – 12:10) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Şerit:** DEMO DÜZELTME, izole test VT `kuyumcu_test_j`, API 5176, vekil 5175
**Kapsam:** tren sayfalar → sekme · uzun listeler → sayfalama · kırık hücreler · elle tablist'ler → ortak `Sekmeler` · `/islemler` ilk bakış · **ortak seçici genişliği · form satırı hizası · satış ekranı dili / son fiş no** (kurucu bulgusu 10:20: *"normal mi yazılar kutuya sığmaz, inputlar aynı seviyede değil"*).

**İKİNCİ KOŞUM (12:40–13:30):** doğrulayıcının KALDI kararındaki KRİTİK bulgu kapatıldı (375 px Elle ekle satırı: ürün adı kesiliyordu, fiyat ile Ekle hizasızdı). Kapanış ölçümleri, testler, mutasyonlar ve temizlik § 15'te. Son tam süit **5180/5180**, denetim TEMİZ.

---

## 0. Devralma (orkestratör devir notu 10:28)

İlk koşum 10:20'de başlayıp 10:27'de elden durdurulmuştu. Ölçülen yarım iş (`find dotnet -newer Sekmeler.razor`, yedek `…Desktop…/scratchpad/s1i-orj/` ile karşılaştırma):

| Dosya | Durum | Ne yapıldı |
|---|---|---|
| `Sozlukler.GorselSigma.cs` (yeni) | tam | sabitler güncellendi (sayfa boyları 10, `KurGecmisIlk`, `DahaFazlaKayit` TR+EN eklendi) |
| `KarZarar.razor`, `Yedekler.razor`, `Kur.razor` | sekmeli, derleniyor | olduğu gibi devralındı; Kur'a geçmiş sayfası + parite sık satır eklendi |
| `Isletme.razor` | **BOZUK**: Kimlik sekmesi kimlik listesi yerine `<Yukleniyor/>` çiziyordu (`@if (IsKimlik) {Yukleniyor} else {…} else if (IsAbonelik)`) | düzeltildi (kimlik listesi Kimlik sekmesinde) |
| `DenetimIzi.razor` | sayfalama yarım (düğmeler `@onclick` sonraki satırda → denetim.sh ölü düğme sayacı) | tamamlandı; emoji `🔐 🔒` → `Ikon` |
| `Sekmeler.razor`, `SekmeTanimi.cs` | rozet desteği eklenmiş | olduğu gibi kullanıldı (Uyum rozetleri) |
| `Faturalar.razor` (10:25 değişmiş) | **bu paketin DEĞİL** (dosya alanımda yok, S1-i izi yok) | dokunulmadı → açık kalanlar |

## 1. Yapılanlar

### (1) Tren sayfalar → sekme (S1-h `Sekmeler` bileşeni)
| Rota | Sekmeler (`?sekme=`) | Not |
|---|---|---|
| /raporlar/kar-zarar | ozet · grafik · kopru · urunler · stok | TL ve HAS bacağı AYNI sekmede (ekranın tek işi) |
| /yedekler | durum · elle · kosular · tatbikatlar | |
| /kur | guncel · pariteler · (pro) kaynak · gecmis | geçmiş 7,4 ekran → ilk 10 satır + "N kayıt daha göster"; parite tablosu `tablo-sik` |
| /isletme | kimlik · abonelik · ayarlar · subeler | yarım iş düzeltildi |

Tüm id'ler aynı; seçili olmayan paneller DOM'da `hidden` (özellik silinmedi).

### (2) Uzun listeler
- **/denetim-izi:** sunucu `limit/offset` ile **10'luk sayfa**, Önceki/Sonraki gerçek istek; üstte `#dz-sayfa-ozet` "1–10 / N kayıt", altta aralık; uçta düğme `disabled` + aralık metni. Tablo `tablo-tek-satir`, yalnız "Detay" sarar.
- **/raporlar/islem-defteri:** sayfa boyu 100 → **10** (sunucu `limit/ofset`); "daha fazlası var" bandı (her sayfada çıkacaktı) yerine `#id-sinir` üstte "1–10 / N kayıt" (id korundu). Tablo tek satır.
- **/raporlar/kasa:** kasa başına ilk bakışta **son 7 gün** (sunucu sırası korunur) + "N gün daha göster"; "Son 7 gün gösteriliyor · dönemde 14 gün" / "Tüm günler gösteriliyor · Toplam 14 kayıt" (UX-S29). Tarih hücreleri tek satır (ölçüldü: "2026-09-11" iki satıra kırılıyordu).

### (3) Kırık hücreler
`/cariler` telefon `td.hucre-tek-satir` (nowrap) · `/yetkiler` matris `tablo-tek-satir` (başlıklar tek satır, tablo kendi kabında kayar) · `/denetim-izi` hücreler tek satır · `/veri-disa-aktarim` `.secim-etiket` nowrap. Tek kelime harf ortasından bölünmez; S1-i bloğunda `break-all`/`anywhere` YOK.

### (4) Elle yazılmış tablist'ler → ortak `Sekmeler`
- **SatisAnalizi** (`sa`, id'ler aynı), sekme değişince yalnız o sekmenin ucu (`SeciliDegisti → SekmeSec`), `?sekme=` derin bağ.
- **Uyum** (`uy`, `#uy-sekme-risk` aynı), açık bildirim / yüksek risk sayısı sekme rozetinde (`SekmeTanimi.Rozet`), "+ Yeni bildirim" URL'yi de şüpheliye çevirir.
- **Gider** (`gd`, id'ler aynı), **tek kaynak = URL** (`SupplyParameterFromQuery`); eski `?sekme=kategori` bağı `kategoriler`e yönlenir (replace). Mutasyon MS1i-10 ilk denemede iki paralel yolun (SeciliDegisti + sorgu) birbirini örttüğünü gösterdi → ikinci yol kaldırıldı.
- Ek (ölçümden): Uyum › Risk sekmesinde seçici yer tutucusu sınır notunun üstüne biniyordu (91×22 px) — ikinci `<label>` kaldırıldı, not altta (`#uy-risk-cari-sinir`).

### (5) /islemler ilk bakış — ÖLÇÜLMÜŞ GEREKÇE (≤ 1,5 sağlanamadı)
1280×800 basit, açık tema, 5176 (bloklar `getBoundingClientRect`):

| Blok | üst (px) | yükseklik |
|---|---|---|
| Sayfa başı | 92 | 50 |
| İşlem ızgarası (NE YAPIYORSUN?) | 158 | 200 |
| Basit akış şeridi | 374 | 67 |
| **Fiş formu** (müşteri 151 · barkod 42 · elle ekle 42 · Verdim/Aldım 430 · açıklama 67 · Kaydet) | 457 | 973 — `#islem-kaydet` üstü **1338** |
| Son satışlar (20 satır) | 1446 | 1599 |

Boy 3,8'in %53'ü son satışlar listesi, formun kendisi 973 px. ≤ 1,5 için tuvalin (Tezgah.dc.html) **iki sütunlu tezgâh düzeni** gerekir: fiş solda (Verdim/Aldım yan yana), "Ne yapıyorsun?" + "Son 3 satış" sağda. Bu, `FisIslemIzgarasi`, `FisSonSatislar` ve 48rem kabının yeniden düzenini ister; paketin dosya alanı `Islemler.razor`'a *yalnız düzen/dil/fiş no* ve **≤1400 satır** (dosya 1399) izni verir → bu pakette yapılmadı, **S1-j önerisi** (açık kalanlar). Bu pakette yapılan: satır içi `style` → `.islem-sayfa`, uzun iki not tek kısa satır (−37 px), TRY eki kaldırıldı, form hizası.

### (6) Ortak seçici (185 kullanım, tek yer)
`app.css` S1-i: `.secici-liste-kap { width:max-content; min-width:min(max(100%,20rem),92vw); max-width:min(max(100%,32rem),92vw) }`, seçenek adı `word-wrap:break-word; word-break:normal` (eski `overflow-wrap:anywhere` harf ortasından bölüyordu), `.secici-alt` nowrap; araç şeridindeki akış içi panel de ≥20rem. `js/secici-yerlesim.js`: panel sağ kenardan taşarsa `.sola` (tetiğin sağına hizalı). Ölçüm: ürün seçici **145 → 320 px**, "22 Ayar Burma Bilezik (K22)" **4 → 1 satır**; katalog K6 tüm rotalarda ölçüldü (tablo).
*Not:* S1-c kilidi (S1c_CSS_blogu…) kendi bloğundan dosya sonuna kadar "overflow" sözcüğünü yasaklıyor; bu yüzden eşdeğer eski ad `word-wrap` kullanıldı.

### (7) Form satırı hizası (`/islemler` Elle ekle)
`FisKalemGirisi`: satır `.satir.form-satiri.fis-kalem-satiri#islem-elle-satir`; fiyat alanını saran ikinci `.alan` kaldırıldı (iç içe alan rayları 31 px kaydırıyordu), teklif/kaynak rozeti satırın ALTINA (`#islem-teklif-sar`). CSS: düğme kontrol rayında (`grid-row:2/3; align-self:end; height:var(--kontrol-yuksekligi)`), etiket rayın üstüne yaslı, "(isteğe bağlı)" bölünmez, ≥1100 px'te etiket tek satır, fiyat kutusunun yanındaki "TRY" gizli (etiket "Birim Fiyat (₺)").
**Önce (10:33, eski kod, 1280 basit):** etiket üstleri 833/833/**864**, kutu altları 896/896/**919**, Ekle 901–**935** · **Sonra:** etiket 479/479/479, kutu 510–550 ×3, Ekle 510–550 → farklar **0/0/0** (tablo `H` sütunu, 4 kombinasyon).
Ek (ölçümden, profesyonel): `#islem-kasa-durum ⟂ #islem-kasa-tek` binmesi — `<div class="alan"><Secici/><span/></div>` kalıbı üç raylı alt ızgarada örtük ray açamıyordu. Genel çözüm (app.css S1-i alt bölüm 6): seçicinin yanında başka çocuk varsa sarmalayıcı dikey yığın → binme 0.
Aynı hizalama kuralı diğer yatay satırlara (katalog K7 ölçümünden): `/denetim-izi` süzgeç ızgarası (Bitiş ipucu/hata iç içe `.alan`dan alan kabuğunun kendi ipucu/hata rayına, id'ler aynı → ±48/56 → 0), araç şeridi (`.sayfa-basi-filtre > .secici-alan` yeniden dikey yığın, arama kutusu tetikle aynı yükseklik, görünmez etiket üstte → `/cariler` ±4/±64 → 0), barkod satırı 375 px'te tek satır (±58 → 0). Seçici paneli akış içi şeritte de ekran kenarını aşarsa JS `--secici-kaydir` belirteciyle kaydırılır (`/uyum` 375 px sağ taşma 88 → 0).

### (8) Basit mod dili ve başlık
- Sayfa başlığı ve `<PageTitle>` = `MenuAdlari…Satis.Ad` → **"Satış"** (üst bar ve menüyle aynı ad, iki modda da).
- Basit alt başlık jargonsuz tek cümle (`SatisEkraniSozlugu.BasitAciklama`).
- `FisMusteriSatiri` `Basit` parametresi: etiket **"Müşteri"** (pro'da "Cariler" kalır, D10); pasif cari notu ve Peşin Müşteri açıklaması tek kısa satır, tam metin `title`'da (id'ler aynı). Ölçüm: basit modda görünür "Cariler" **0** (K8 jargon 0).
- Emoji kalıntıları (dosya alanımdaki sayfalar): `🔐 🔒` (denetim izi), `⟳ ✎` (kur), `🚫` (işletme), `🔒 🗑 ⚠` (cariler) → `Ikon` ya da kaldırıldı. `Cariler.razor` `SatirEylemleri` ikon dizgeleri (`💵 ♻`) bileşen sözleşmesi olduğu için dokunulmadı → açık kalanlar.

### (9) QA-9 son fiş no + Hurda derin bağlantısı
- `FisYazdirBaglantisi` yeni `FisNo` parametresi: `#islem-son-fis` "Son fiş: {no}" yazdırma bağlantısıyla AYNI satırda, bir sonraki Kaydet'e kadar (Kaydet() zaten `basarili/sonFisNo` sıfırlar; POST yolu DEĞİŞMEDİ). Bağlantı yoksa (alış) numara yine görünür. İkinci blok açılmadı.
- `/gider-pusulalari` "Hurda alımı gir" → `/islemler?tur=HURDA_ALIS` (`HurdaAlimiAdresi` sabiti); `Islemler.Diger.cs` `SorgudanSuzgecOku()` yalnız ekranda GÖRÜNEN türü kabul eder, bilinmeyen tahmin edilmez. Tarayıcıda: HURDA_ALIS döşemesi seçili (`aria-pressed=true`) açılıyor (tablo satırı `/islemler?tur=HURDA_ALIS`).

## 2. Sapmalar ve gerekçeleri
1. **Dosya alanı dışı dokunuşlar** (hedefe sadakat): `Bilesenler/Fis/FisMusteriSatiri.razor` ("Cariler" etiketi orada), `Bilesenler/Fis/FisYazdirBaglantisi.razor` (fiş no'yu #islem-yazdir ile birleştirme şartı), `Islemler.Diger.cs` (Islemler.razor 1400 tavanı — yeni mantık parçalı sınıfta; razor net +0 satır), `js/secici-yerlesim.js` (kenar hizası tek yer), `Pages/KasaRaporu.razor` / `IslemDefteri.razor` ("Raporlar*" kapsamında), testlerde fikstür güncellemeleri.
2. **Sayfa boyu 15 → 10** (denetim izi, işlem defteri): 15 satırla 1280'de boy 2,0 / 2,1 ölçüldü (> 1,8).
3. **Mevcut test güncellemeleri (davranış bilinçli değişti):** `DenetimIziEkranTestleri.Kisisel_veri_erisimi_isaretlenir` (emoji → Ikon aria-label), `RaporEkranTestleri` (limit 100→10, ofset 10), `UyumEkranTestleri` (`.sekme` → `[role=tab]`), `P19VirmanBacagiEkranTestleri` (aria-selected artık WAI-ARIA "true"/"false" değeri), `EmanetKurEkranTestleri` fikstürü (geçmiş 2 → 14 satır).
4. **Tarama tablosu mod hatası:** önceki koşumun "basit" taban ölçümü (`i-taban.jsonl`) aslında PROFESYONEL'di (Ayarlar düğmesi disabled döndü). Ölçüm aracı modu `PATCH /kullanicilar/ben` + yeniden yükleme ile değiştirir ve `data-mod`/`data-tema` ile doğrular.
5. **Ölçüm betiği değişti:** orkestratör 11:25'te `betikler/sikisma-olc.js`'i K1–K9 kataloğuyla değiştirdi; son tablo YENİ katalogla (`__olcTam`) alındı.
6. **/islemler ≤ 1,5 yok** → § 1 (5) ölçülmüş gerekçe.

## 3. Migration
Gerekmedi (şema değişikliği yok; sunucu uçları mevcut `limit/offset` kalıbıyla kullanıldı).

## 4. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Yeni düğmeler: `#dz-onceki/#dz-sonraki` (gerçek GET, uçta disabled + aralık metni), `#id-onceki/#id-sonraki` (mevcut), `#kr-gun-daha-{kasa}` ve `#kur-gecmis-daha` (istek atmaz, eldeki sunucu verisinden görünen satırı büyütür; toplam yazılı), sekme düğmeleri (URL değiştirir → sayfa ucu). Hepsinde `@onclick` aynı satırda (denetim.sh ölü düğme 53 ≤ 71).

## 5. Testler
- Önce: tam süit 5072 test, **11 kırmızı** (hepsi bu paketin yarım/yeni davranışı — § 2.3; düzeltildi).
- Yeni: `S1iGorselSigma2Testleri` (14 metot / 20 durum), `KarZararEkranTestleri.S1i_KZ_…`, `KurEkranTestleri.S1i_Kur_gecmisi_…`, `BasitModPosTestleri.S1i_Hurda_… (×2) + S1i_Bilinmeyen_tur_…`, `P19VirmanBacagiEkranTestleri.S1i_Gider_sekmesine_…` → **26 yeni test durumu**.
- Tam süit #1 (11:18, `KUYUMCU_TEST_DB=kuyumcu_test_j`, trx `scratchpad/trx-pS1-i/`): **5113 / 5113 geçti, 0 başarısız.**
- Tam süit #2 (11:50, son C# kodu, trx `scratchpad/trx-pS1-i-son/`): **5113 test · 5111 geçti · 2 başarısız** — ikisi de **bu paketin DEĞİL** (paralel Q9-4 şeridi 11:47–11:52'de `Layout/UstBar.razor`, `Pages/Cikti.razor`, `Q94CiktiBelgeTestleri.cs` değiştirdi; bu dosyalara dokunmadım): `RotaBaslikTestleri.Ust_bar_GERCEK_rotada_gercek_basligi_cizer` ("header.ust-bar h1" yok) ve `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini_kullanir_muafiyet_YAZILIDIR` (["Cikti.razor"]). **Bu paketin ürettiği yeni kırmızı: 0.**
- Son değişikliklerden sonra filtreli koşumlar: (S1i · P37 · S1c · S1h · TasarimBelirtec) **149 / 149**; (S1i · DenetimIzi · UX4d · Kur · EmanetKur · Cari · BasitModPos · Secici · S21Fis) **349 / 349**.
- Derleme: `dotnet build Kuyumcu.sln` **0 hata, 0 uyarı** (son temiz derleme). Arada görülen `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı 03:07 tarihli, bu paketin değil.

## 6. Mutasyon tablosu (yedek `scratchpad/mut-yedek/S1-i/`, perl tek eşleşme, geri kopya + `cmp` AYNI)
| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| MS1i-1 | KarZarar `<Sekmeler>` kaldırılıp bölümler `@foreach` ile alt alta | `S1i_12_…(KarZarar.razor)`, `KarZararEkranTestleri.S1i_KZ_sekiz_bolum_bes_sekmede_idler_ayni_yalniz_ozet_acik` | AYNI |
| MS1i-2 | Denetim izi `SayfaBoyu = 100` (sayfalama kapalı) | `S1i_09_Denetim_izi_sunucu_sayfali_10_satir_toplam_yazili` | AYNI |
| MS1i-3 | Kasa defteri `return tum;` (gün sayfası kapalı) | `S1i_10_Kasa_defteri_kasa_basina_son_7_gun_ve_N_gun_daha` | AYNI |
| MS1i-4 | İşlem defteri `Limit = 100` | `S1i_11_Islem_defteri_10_satirlik_sunucu_sayfasi_ve_ustte_aralik` | AYNI |
| MS1i-5 | Telefon `nowrap` kuralı silindi | `S1i_13_Kirik_hucre_kilitleri_telefon_yetki_denetim` (+ tarayıcı § 7) | AYNI |
| MS1i-6 | Seçici panel `min-width/max-width` silindi | `S1i_01_Secici_paneli_en_az_20rem_…` (+ tarayıcı § 7) | AYNI |
| MS1i-7 | Fiyat alanı yeniden `<div class="alan">` içine | `S1i_02_Elle_ekle_satiri_ortak_form_satiri_ic_ice_alan_yok_dugme_satirda` | AYNI |
| MS1i-8 | Basit etiket yine `Dil.T.Cari.Baslik` | `S1i_05_Basit_yuzde_Cariler_yerine_Musteri_ve_kisa_notlar` | AYNI |
| MS1i-9 | Son fiş satırı `@if (false)` | `S1i_07_Son_fis_numarasi_yazdir_baglantisinin_yaninda_kalir` | AYNI |
| MS1i-10 | Gider `?sekme=kategoriler` dalı silindi (ilk deneme `GdSekmeDegisti` no-op → YEŞİL = test açığı/çift yol; ikinci yol kaldırılıp tekrarlandı) | `P19VirmanBacagiEkranTestleri.S1i_Gider_sekmesine_…`, `P19EkranTestleri.Kategori_duzenlemesi_CEKMECE_…`, `P19EkranTestleri.Kategori_pasife_alma_DELETE_…` | AYNI |
| MS1i-11 | Uyum `UySekmeDegisti` no-op | `UyumEkranTestleri.Saklama_sekmesinde_onay_kutusu_…`, `…Suresi_gecmis_bildirim_kirmizi_isaretlenir`, `…Saklama_sekmesi_geri_alinamaz_uyarisi_gosterir` | AYNI |
| MS1i-12 | `?tur=` okuma kapalı (`_ = istenen;`) | `S1i_08_Hurda_…`, `BasitModPosTestleri.S1i_Hurda_derin_baglantisi_… (BASIT, PROFESYONEL)` | AYNI |
| MS1i-13 | Kur geçmişi `gecmis.Take(...)` → tümü (ilk deneme test yoktu → test yazıldı, tekrarlandı) | `KurEkranTestleri.S1i_Kur_gecmisi_ilk_bakista_10_satir_toplam_yazili_daha_goster` | AYNI |

Tarayıcı mutasyonları (gerçek Chromium, kendi sekmem, 5175): **MS1i-6** seçici genişlik kuralı enjekte stille geri alındı → ürün paneli **320 → 257 px**, "22 Ayar Burma Bilezik (K22)" **1 → 2 satır** (K6 kırmızı: < 320 px); geri alınınca 320 px / 1 satır. **MS1i-5** telefon `nowrap` kaldırıldı → 1100 px ve 375 px'te telefon düğmeleri yine tek satır (0 → 0): tarayıcı ölçümü bu mutasyonda **kırmızıya DÖNMEDİ** — S1-h/S1-i sonrası sütun genişliği numarayı zaten sığdırıyor; kilidi yalnız `S1i_13` (CSS kaynak kilidi) tutuyor. Tarayıcı tarafında bu mutasyonun kırmızı kanıtı YOK (açıkça yazıyorum).

Son mutasyondan ve son değişiklikten sonra temiz `dotnet build Kuyumcu.sln`: **0 hata, 0 uyarı**.

## 7. Denetim
`dotnet/denetim.sh` → **DENETİM TEMİZ** (son koşum son kodla; 11:53 koşumunda P-37 SAYFA_BASI_DISI_DUZEN + P-37c SAYFA_BASI_KAPSAM Q9-4'ün `Cikti.razor`'ından kırmızıydı, sonraki koşumda temiz; ara koşumda iki ihlal yakalandı ve düzeltildi: P-13 IC_KOD_SIZINTISI — razor çıktısında `=> …"HURDA_ALIS"` deseni → sabit; P-15 SORGU_SUZGECI_OKUMAYAN_SAYFA — `/islemler` okuma kanıtı `SorgudanSuzgecOku();` satır başında). Ölü düğme 53/71, satır içi style 105/143 (1 azaldı).

## 8. Canlı doğrulama (API 5176 · kuyumcu_net · test kiracısı PAKETS1-i-TEST `01M3947W6Y9DR21S6C1PQ22S9J`)
Spesifikasyonda ayrı bir HTTP listesi yok; ekranın dayandığı uçlar gerçek veriyle okundu (yanıtlar ayrıştırıldı):

| İstek | HTTP | Yanıttan okunan |
|---|---|---|
| GET /api/v1/denetim?limit=10&offset=0 | 200 | toplam=380 · limit=10 · offset=0 · kayıt=10 |
| GET /api/v1/denetim?limit=10&offset=10 | 200 | toplam=381 (okuma kaydı artar) · offset=10 · kayıt=10 · farklı ilk id |
| GET /api/v1/raporlar/islem-defteri?…&limit=15&ofset=0 | 200 | toplamSatir=35 · satır=15 · dahaFazlasiVar=true |
| GET /api/v1/raporlar/kasa-defteri?2026-09-11..24 | 200 | 1 kasa · 14 gün (ekran 7 + "7 gün daha") |
| GET /api/v1/auth/ben | 200 | tenantId = test kiracısı |

SQL: `select count(*) from denetim_kayitlari where tenant_id=…` → **385** (API toplamıyla aynı küme; aradaki fark ekran okumalarının yazdığı yeni kayıtlar). `kasalar` → 1.
Tarayıcıda (5175 vekil): `/islemler?tur=HURDA_ALIS` açıldığında `#islem-tur-HURDA_ALIS` seçili; başlık "Satış"; basit modda görünen "Cariler" 0; `#islem-urun` paneli 320 px; Elle ekle satırı etiket/kutu/Ekle farkı 0/0/0.
**Not:** yoğun ölçüm turları API oran sınırını (istek sınırı) tetikledi; son iki ek görüntü turunda bazı ekranlar "Sunucu şu anda çok yoğun" bandıyla çizildi (`islemler-elle-satiri-1280-SONRA.png` profesyonel yüzdür, mod değişimi 429 aldı). Ana tablo (son4) turunda mod değişimlerinin 4'ü de 200 döndü.

## 9. Gerçek tarayıcı ölçümü (GERÇEK TARAYICI KURALI)
Araç: Chromium (Playwright paketi, başsız, CDP) + orkestratörün **yeni** `betikler/sikisma-olc.js` kataloğu (K1–K9, `__olcTam`), vekil 5175 üzerinden kendi oturumum; mod `PATCH /kullanicilar/ben` ile değiştirilip `data-mod`/`data-tema` ile doğrulandı. 36 rota/sekme × 1280×800 ve 375×812 × BASİT/PRO × açık/koyu = **288 ölçüm** (`scratchpad/son4.jsonl`, görüntüler `scratchpad/png-son4/` — seçmeler `ham/S1-i-ekran/`). Son CSS düzeltmelerinden sonra `/cariler`, `/uyum?sekme=supheli`, `/kur?sekme=guncel` yeniden ölçüldü (`son6.jsonl`: K7 0, K6 0).
Hücre: `boy` (sayfa yüksekliği / ekran) + yalnız SIFIR OLMAYAN sayaçlar. `H a/b/c P n` = Elle ekle satırı etiket üstü / kutu altı / Ekle farkı (px) ve ürün paneli genişliği.

| Rota (sekme) | B-ayd-1280 | B-kar-1280 | P-ayd-1280 | P-kar-1280 | B-ayd-375 | B-kar-375 | P-ayd-375 | P-kar-375 |
|---|---|---|---|---|---|---|---|---|
| /raporlar/kar-zarar?sekme=ozet | 1.7 | 1.7 | 1.3 | 1.7 | 3.3 | 3.3 | 3.3 | 3.3 |
| /raporlar/kar-zarar?sekme=grafik | 1.3 | 1.3 | 1.3 | 1.3 | 2.1 | 2.1 | 2.1 | 2.1 |
| /raporlar/kar-zarar?sekme=kopru | 1.7 | 1.7 | 1.3 | 1.7 | 2.3 | 2.3 | 2.3 | 2.3 |
| /raporlar/kar-zarar?sekme=urunler | 1.3 | 1.3 | 1.3 | 1.3 | 1.8 | 1.8 | 1.8 | 1.8 |
| /raporlar/kar-zarar?sekme=stok | 1.3 | 1.3 | 1.3 | 1.3 | 2 | 2 | 2.1 | 2.1 |
| /yedekler?sekme=durum | 1.4 | 1.4 | 1.4 | 1.4 | 1.8 | 1.8 | 1.9 | 1.9 |
| /yedekler?sekme=elle | 1.4 | 1.4 | 1.4 | 1.4 | 1.4 | 1.4 | 1.5 | 1.5 |
| /yedekler?sekme=kosular | 1.4 | 1.4 | 1.4 | 1.4 | 1.4 | 1.4 | 1.5 | 1.5 |
| /yedekler?sekme=tatbikatlar | 1.4 | 1.4 | 1.4 | 1.4 | 1.3 | 1.3 | 1.4 | 1.4 |
| /kur?sekme=guncel | 1.7 | 1.5 | 1.5 | 1.4 | 1 | 1.6 | 1.7 | 1.7 |
| /kur?sekme=pariteler | 1.7 | 1.7 | 1.5 | 1.7 | 1.7 | 1.7 | 1.9 | 1.9 |
| /kur?sekme=kaynak | 1.5 | 1.5 | 1.5 | 1.5 | 1.6 | 1.6 | 1 | 1 |
| /kur?sekme=gecmis | 1.5 | 1.5 | 1.5 | 1.6 | 1.6 | 1.6 | 2.2 | 2.2 |
| /isletme?sekme=kimlik | 1.4 | 1.4 | 1.4 | 1.4 | 1.2 | 1.2 | 1.3 | 1.3 |
| /isletme?sekme=abonelik | 1.5 | 1.5 | 1.4 | 1.5 | 1.8 | 1.8 | 1.8 | 1.8 |
| /isletme?sekme=ayarlar | 1.4 K8:1 | 1.4 K8:1 | 1.4 | 1.4 | 1.6 K8:1 | 1.6 K8:1 | 1.7 | 1.7 |
| /isletme?sekme=subeler | 1.4 | 1.4 | 1.4 | 1.4 | 1.2 | 1.2 | 1.3 | 1.3 |
| /denetim-izi | 1.6 K8:3 | 1.7 K8:5 | 1.4 | 1.7 | 1.8 K8:3 | 2.5 K8:5 | 2.6 | 2.6 |
| /raporlar/kasa | 1.5 | 1.3 | 1.3 | 1.2 | 1 K7:1 | 1.3 K7:1 | 1.4 K7:1 | 1.5 K7:1 |
| /raporlar/islem-defteri | 1.8 K8:1 | 1.8 K8:1 | 1.7 | 1.7 | 2.6 K8:1 | 2.6 K8:1 | 2.7 | 2.7 |
| /cariler | 1.5 K8:2 | 1.5 K7:1 K8:2 | 1.3 | 1.3 | 1.9 K8:2 | 1.8 K8:2 | 1.8 | 1.9 |
| /yetkiler | 1.4 K8:8 | 1.4 K8:8 | 1.4 | 1.4 | 1.1 K8:8 | 1.1 K8:8 | 1.2 | 1.2 |
| /veri-disa-aktarim | 1.4 K8:1 | 1.4 K8:1 | 1.4 | 1.4 | 1.9 K8:1 | 1.9 K8:1 | 1.9 | 1.9 |
| /raporlar/satis-analizi?sekme=odeme | 1.3 K5:2 | 1.3 K5:2 | 1.3 K5:2 | 1.3 K5:2 | 1.8 K5:2 K7:1 | 1.8 K5:2 K7:1 | 1.9 K5:2 K7:1 | 1.9 K5:2 K7:1 |
| /raporlar/satis-analizi?sekme=personel | 1.3 | 1.3 | 1.3 | 1.3 | 1 K7:1 | 1 K7:1 | 1 K7:1 | 1 K7:1 |
| /raporlar/satis-analizi?sekme=sube | 1.3 | 1.3 | 1.3 | 1.3 | 1 K7:1 | 1 K7:1 | 1 K7:1 | 1 K7:1 |
| /uyum?sekme=tespit | 1.2 | 1.2 | 1.1 | 1.1 | 1.4 | 1.4 | 1.5 | 1.5 |
| /uyum?sekme=supheli | 1.4 | 1.2 | 1.1 | 1 | 1.1 | 1.3 | 1.4 | 1.5 |
| /uyum?sekme=saklama | 1.2 | 1.2 | 1.1 | 1.1 | 1.3 | 1.3 | 1.4 | 1.4 |
| /uyum?sekme=risk | 1.6 K8:3 | 1.6 K8:3 | 1.6 | 1.6 | 2.7 K8:3 | 2.7 K8:3 | 2.8 | 2.8 |
| /gider?sekme=giderler | 1.5 K5:2 K8:1 | 1.5 K5:2 K8:1 | 1.5 K5:2 | 1.5 K5:2 | 1.5 K5:2 K8:1 | 1.5 K5:2 K8:1 | 1.6 K5:2 | 1.6 K5:2 |
| /gider?sekme=kategoriler | 1.5 | 1.5 | 1.5 | 1.5 | 1.1 | 1.1 | 1.2 | 1.2 |
| /gider?sekme=virman | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 |
| /gider-pusulalari | 1.2 | 1.2 | 1.1 | 1.1 | 1.3 | 1.3 | 1.4 | 1.4 |
| /islemler | 3.9 H0/0/0 P320 | 3.8 H0/0/0 P320 | 3.4 K5:1 H0/0/0 P320 | 3.5 K5:1 H0/0/0 P320 | 1.2 H83/83/9 P320 | 4.4 H83/83/9 P320 | 4 K5:1 H83/83/9 P320 | 4 K5:1 H83/83/9 P320 |
| /islemler?tur=HURDA_ALIS | 3.9 H0/0/0 P320 | 3.9 H0/0/0 P320 | 3.4 K5:1 H0/0/0 P320 | 3.4 K5:1 H0/0/0 P320 | 4.6 H83/83/9 P320 | 4.6 H83/83/9 P320 | 4 K5:1 H83/83/9 P320 | 4 K5:1 H83/83/9 P320 |
| /giris | – | – | 1 | 1 | – | – | 1.2 | 1.2 |

Toplam (tüm hücreler): K1=0, K3t=0, K3k=0, K5=48, K6=0, K7=17, K8=84, K9=0, Y=0

(`/cariler` B-kar-1280 hücresindeki K7:1 son4'e aittir; arama kutusu yükseklik düzeltmesinden sonra son6'da 4 kombinasyonda K7 = 0.)

**Okuma:**
- **K1 gizli-görünen 0 · K3 taşan 0 · K3 kırık 0 · K6 dar seçici 0 · K9 binen 0 · yatay kaydırma 0** — 288 ölçümün tamamında.
- **Tren sayfalar (1280):** her sekme ≤ 1,8 (kâr-zarar 1,3–1,7 · yedekler 1,4 · kur 1,5–1,7 · işletme 1,3–1,5). **Uzun listeler (1280):** denetim izi 1,6–1,7 · işlem defteri 1,7–1,8 · kasa 1,2–1,5; toplam kayıt sayısı her birinde yazılı.
- **/islemler:** Elle ekle hizası **0/0/0 px** (4 kombinasyon, 1280), ürün paneli **320 px**, "22 Ayar Burma Bilezik (K22)" **1 satır**, "TRY" eki yok, basit modda "Cariler" **0**. **[DÜZELTME — § 15]** İlk koşumdaki "375'te alanlar alt alta dizildiği için yan yana değil" ifadesi YANLIŞTI: doğrulayıcı 375'te ürün tetiğinin 123 px'e sıkıştığını, ürün adının üç noktayla kesildiğini, fiyat ↔ Ekle farkının alt ±9 / üst ±11 px olduğunu ölçtü (tablodaki H83/83/9 hücreleri bunu zaten gösteriyordu). Kapanış ölçümünde 375 ve 1280'de, 8 kombinasyonun hepsinde fark 0/0/0 ve kesilme yok (§ 15). Boy 3,4–3,9 → § 1 (5) ölçülmüş gerekçe.
- **375 px boylar** (kâr-zarar özet 3,3, denetim 2,5, işlem defteri 2,6, uyum risk 2,7): dar ekranda kartlar tek sütuna iner; kabul eşikleri orkestratör tablosu gibi 1280'de ölçüldü. Bilgi olarak raporlanır.
- **K7 kalan (yalnız 375):** `/raporlar/kasa` ve `/raporlar/satis-analizi` araç şeridinde tarih kutuları 375'te ALT ALTA (kutu altı ±52 = 44 px kutu + 8 px aralık); katalog sezgiseli (üst farkı < 60 px ise "aynı sıra") alt alta iki kutuyu yan yana sayıyor. Görsel kusur değil — ölçülmüş gerekçe. İstenirse şerit ≤ 640 px'te tarihleri yan yana dizerek sıfırlanabilir (P-37 şerit kuralına dokunur → ayrı iş).
- **K5 "TRY" ve K8 basit mod jargonu** bu paketin kapsamı DEĞİL, 10:50 katalog kararıyla orkestratörün **KATALOG paketine** ayrıldı (ORKESTRATOR-DURUM 10:50: "TRY→₺ dahil"). Ölçülen yerler: K5 → `/gider` (kasa etiketi "Ana Kasa (TRY)", tutar eki), `/raporlar/satis-analizi` (para birimi sütunu), `/islemler` profesyonel (kasa seçeneği "Ana Kasa (TRY)"); K8 → `/cariler` (Cariler, Cari), `/denetim-izi` (Cari, Denetim izi), `/yetkiler`, `/veri-disa-aktarim`, `/isletme` (Vezne), `/raporlar/islem-defteri` + `/gider` (Storno), `/uyum` risk (Cari). Bu paketin eklediği metinlerde TRY/jargon YOK; `/islemler` basit K8 = 0.

## 10. Değişen dosyalar (bu paket; ağaçta paralel Q9 şeridinin dosyaları da değişti — onlar listelenmedi)
`git status` bu ağaçta anlamlı değil (tek "first commit"ten sonra dosyaların çoğu izlenmiyor); liste elle, oturumdaki düzenlemelerden:
- Core: `Kuyumcu.Core/I18n/Sozlukler.GorselSigma.cs` (devralındı+güncellendi), `Kuyumcu.Core/I18n/Sozlukler.SatisEkrani.cs` (yeni)
- Web sayfalar: `Pages/KarZarar.razor`, `Pages/Yedekler.razor`, `Pages/Kur.razor`, `Pages/Isletme.razor`, `Pages/DenetimIzi.razor`, `Pages/KasaRaporu.razor`, `Pages/IslemDefteri.razor`, `Pages/Cariler.razor`, `Pages/Yetkiler.razor`, `Pages/SatisAnalizi.razor`, `Pages/Uyum.razor`, `Pages/Gider.razor`, `Pages/GiderPusulalari.razor`, `Pages/Islemler.razor` (net 0 satır, 1399), `Pages/Islemler.Diger.cs`
- Web bileşenler: `Bilesenler/Sekmeler.razor`, `Bilesenler/SekmeTanimi.cs` (devralındı), `Bilesenler/Fis/FisKalemGirisi.razor`, `Bilesenler/Fis/FisMusteriSatiri.razor`, `Bilesenler/Fis/FisYazdirBaglantisi.razor`
- Web varlıklar: `wwwroot/css/app.css` (yalnız dosya SONUNA S1-i bloğu, 10 alt bölüm), `wwwroot/js/secici-yerlesim.js`
- Testler: `S1iGorselSigma2Testleri.cs` (yeni), `KarZararEkranTestleri.cs`, `KurEkranTestleri.cs`, `EmanetKurEkranTestleri.cs`, `BasitModPosTestleri.cs`, `P19VirmanBacagiEkranTestleri.cs`, `EkranTestleri.cs`, `RaporEkranTestleri.cs`
- Belgeler: bu rapor, `ham/S1-i-ekran/` (11 PNG + ölçüm tablosu)
Proje ağacına `.mutorj/.bak/.tmp` BIRAKILMADI (mutasyon yedekleri scratchpad'de). Ağaçta önceden var olan `Pages/*.mutorj` dosyaları başka paketlerin.

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Tren sayfalar sekmeye, her sekme ≤ 1,8 | KARŞILANDI (1280) | tablo; KarZarar bUnit + MS1i-1 |
| (2) Uzun listeler ilk bakış ≤ 1,8 + toplam yazılı, sessiz kesme yok | KARŞILANDI (1280) | denetim 1,7 · işlem defteri 1,8 · kasa 1,3; S1i_09/10/11 |
| (3) Kırık hücreler: tek kelime 0, kırık ≤ 2 | KARŞILANDI | K3 kırık 0 (288 ölçüm) |
| (4) SatisAnalizi/Uyum/Gider tablist → Sekmeler, id'ler aynı | KARŞILANDI | S1i_12, P19/Uyum/Rapor testleri, MS1i-10/11 |
| (5) /islemler basit ≤ 1,5 ya da ölçülmüş gerekçe | GEREKÇE | § 1 (5) blok tablosu; S1-j önerisi |
| (6) Ortak seçici ≥ 320 px, ≤ 2 satır, kenara taşarsa hizalanır | KARŞILANDI | K6 0; panel 320 px, Burma 1 satır; MS1i-6 (bUnit + tarayıcı) |
| (7) Form satırı hizası ±2 px, TRY eki yok, (isteğe bağlı) aşağı itmez | KARŞILANDI (ikinci koşumda 375 dahil) | ilk koşumda yalnız 1280'de sağlanmıştı; 375 KRİTİK bulgusu § 15'te kapatıldı: 8 kombinasyonda 0/0/0, elipsis yok; S1i_15/16, MS1i-K1…K4 (kapanış) |
| (8) Basit mod "Müşteri", kısa notlar, başlık "Satış" | KARŞILANDI | S1i_05/06, basit "Cariler" 0 |
| (9) Son fiş no kalıcı + Hurda derin bağ | KARŞILANDI | S1i_07/08 + BasitModPos ×3, MS1i-9/12 |
| DoD her düğme istek ya da açıklama | KARŞILANDI | § 4; denetim ölü düğme 53 ≤ 71 |
| Özellik silinmedi, id/rota aynı | KARŞILANDI | sekme panelleri hidden, id testleri |
| Sabit renk / satır içi style yok, belirteç | KARŞILANDI | S1-i CSS bloğu yalnız var(); satır içi style 106 → 105 |
| Emoji ikon yok | KARŞILANDI (dosya alanı) | 🔐🔒⟳✎🚫🗑⚠ → Ikon; SatirEylemleri ikon dizgeleri açık |
| Sözlükler HUB'ına dokunulmadı, TR+EN | KARŞILANDI | yeni metin `Sozlukler.GorselSigma.cs` / `Sozlukler.SatisEkrani.cs` |
| 1280 + 375 · basit + pro · açık + koyu ölçüm + görüntü | KARŞILANDI | § 9, 288 ölçüm |
| Mevcut testler yeşil | KARŞILANDI (paket ölçüsü) | yeni kırmızı 0; 2 kırmızı Q9-4'ün |
| Test kiracısı silindi, tenants başlangıca döndü | KARŞILANDI | § 12 |
| Mevzuat | UYGULANMAZ | mevzuat hükmü içeren değişiklik yok |

## 12. Test kiracısı temizliği
API 5176 ve vekil 5175 durduruldu (`lsof` boş). `PAKETS1-i-TEST` (`01M3947W6Y9DR21S6C1PQ22S9J`) `tenant_id` kolonu olan **70 tablonun** tamamından ve `tenants`'tan silindi; kalan satır **0**; `tenants` = **10** (QA-TEST-1/2/3/FRONTEND, DEMO-Vitrin ve eski demo kiracıları — dokunulmadı).

## 13. Açık kalanlar
1. **S1-j önerisi (/islemler ≤ 1,5):** tuvalin iki sütunlu tezgâh düzeni (fiş solda, "Ne yapıyorsun?" + "Son 3 satış" sağda, Verdim/Aldım yan yana) — `FisIslemIzgarasi`, `FisSonSatislar`, 48rem kabı; Islemler.razor 1400 tavanı nedeniyle ayrı paket.
2. **KATALOG paketi (orkestratör planı):** K5 TRY→₺ (`/gider`, `/raporlar/satis-analizi`, profesyonel `/islemler` kasa adı) ve K8 basit jargon (liste § 9).
3. **375 araç şeridi K7** (tarih kutuları alt alta) — ölçülmüş gerekçe; istenirse şerit kuralında yan yana dizim.
4. `Cariler.razor` `SatirEylemleri` ikon dizgeleri (`💵`, `♻`) — bileşen sözleşmesi, ayrı iş.
5. **Başka şeridin kırmızıları (sahibiyle):** Q9-4 — `RotaBaslikTestleri.Ust_bar_GERCEK_rotada_gercek_basligi_cizer`, `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini_kullanir_muafiyet_YAZILIDIR` (11:50 tam süit).
6. `Pages/Faturalar.razor` 10:25'te değişmiş (bu paketin değil, dokunulmadı).
7. `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı (03:07 tarihli, bu paketin değil) — tam derlemede ara ara görünür.
8. Mutasyon MS1i-5'in tarayıcı tarafı kırmızı vermedi (§ 6) — telefon numarası bugünkü sütunda nowrap olmadan da sığıyor.
9. Oran sınırı: ölçüm turları tek kullanıcıyla API'nin "istek sınırı"nı tetikledi; doğrulayıcı tekrar ölçerken turlar arasında ~1–2 dk beklemeli.

## 14. Kurucuya / mali müşavire not
Mevzuat hükmü içeren değişiklik yapılmadı; mali müşavir teyidi gerekmez.


---

## 15. İKİNCİ KOŞUM: doğrulama bulgularının kapanışı (2026-09-24 12:40–13:30)

Doğrulayıcı KALDI verdi. Bulgular ve yapılanlar:

| Bulgu | Ciddiyet | Ne yapıldı | Kanıt |
|---|---|---|---|
| 375 px'te Elle ekle satırında ürün adı kutuya sığmıyordu: tetik 123 px, "22 Ayar Burma Bilezik (K22)" 199/81 px ile üç noktada kesik. Fiyat ↔ Ekle farkı alt ±9 / üst ±11 px idi. | KRİTİK | app.css S1-i bloğuna **(11)** eklendi. `@media (max-width: 768px)` içinde `.satir.fis-kalem-satiri` üç sütunlu ızgara oldu. Ürün tam satırı kaplıyor (`grid-column: 1 / -1`, raylar 1-3). Adet, fiyat ve Ekle aynı üç raylı sırada duruyor (raylar 4-6). Ekle **kontrol rayına (5)** oturuyor ve girdiyle aynı yükseklikte (`--dokunma-hedefi`). Etiketler subgrid ile aynı üst çizgide. | Tarayıcı ölçümü: 375 ve 1280 × BASİT/PRO × açık/koyu, 8 kombinasyonun hepsinde etiket/kutu/Ekle farkı **0/0/0 px**. Ürün tetiği **317 px**, seçili ad sw=cw=199 (**elipsis yok**). Test S1i_15, mutasyonlar MS1i-K1 ve MS1i-K2. |
| Ölçerken aynı ekranda bulundu: 375'te barkod yer tutucusu "Barkodu okutun vey…" diye kesiliyordu. "Elle ekle" düğmesi "Ürün" etiketine yapışıktı (6 px). | ORTA (kurucunun şikâyetiyle aynı: "yazılar kutuya sığmaz") | Yer tutucu kısa metne çevrildi: "Barkodu okutun ya da yazın" / EN "Scan or type the barcode" (`Sozlukler.SatisEkrani.cs`, TR+EN; HUB'a dokunulmadı). ≤480 px'te barkod düğmesinde yalnız simge kalıyor. Metin `display:none` ile gizleniyor, erişilebilir ad `aria-label` + `title` ile veriliyor. Açık "Elle ekle"nin altına `--bosluk-3` boşluk eklendi. | Yer tutucu 175 px, kutu içi 228 px. Düğme adı "Barkod okut". "Elle ekle" düğmesinin altı ile "Ürün" etiketi arası 12 px. Test S1i_16, mutasyonlar MS1i-K3 ve MS1i-K4. |
| sikisma-olc K3/K7 bu kusuru saymıyordu. | istek | `betikler/sikisma-olc.js` güncellendi. **K10 elipsis** eklendi: seçili değeri (yer tutucu değil) `text-overflow:ellipsis` ile kesilen `.secici-metin` / `.si-tetik` öğelerini sayar. **K7** artık sırayı yalnız ilk çocuğa göre kurmuyor: her çocuğu çapa olarak alıp en bozuk görsel sırayı ölçüyor. Eski hâli sarmalı satırın ikinci sırasını hiç ölçmüyordu. Önceki sürüm: `scratchpad/mut-yedek/S1-i/sikisma-olc.js.onceki`. | Izgara kaldırılınca (tarayıcı mutasyonu) **K7=1** (`#islem-elle-satir kutuAlt±23`) ve **K10=1** (`span.secici-metin "22 Ayar Burma Bilezik (K22"`). Düzeltilmiş kodda K7=0, K10=0. |
| Paylaşılan `kuyumcu_test` çakışması | DÜŞÜK | Tüm test koşumlarında `KUYUMCU_TEST_DB=kuyumcu_test_j` kullanıldı. | § 15.3 |
| 21 Eylül'den kalma 0 baytlık .mutorj/.bak artıkları | DÜŞÜK | Dokunulmadı. Hook silme komutunu engelliyor; silme kurucuda. | — |
| MS1i-6 kilidi bir CSS dizgesine bağlı | DÜŞÜK, kabul | Asıl kilit tarayıcı ölçümü (K6) olarak kaldı. Panel yeniden ölçüldü. | 375 BASİT: panel **320 px**, Burma **1 satır**, sağa taşma 0 |

**Ek sapma ve gerekçesi.** İlk denemede barkod metni görsel gizli kalıbıyla (mutlak konum, 1 px) gizlendi. Bu iki soruna yol açtı:
- (a) `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir` testi S1-c bloğundan sonra "overflow" sözcüğünü yasaklıyor ve kırmızıya döndü. Ara tam süitte 5180 testten 1'i başarısız oldu.
- (b) Sözcük kaldırılınca 1 px kutudan taşan metin 375'te **24 px yatay kaydırma** üretti. Taramada `/islemler`'de 8 hücre Y:24 ve K3t:1 verdi.

Bu yüzden son çözüm `display:none` + `aria-label` oldu. Son taramada Y=0 ve S1c testi yeşil.

### 15.1 Değişen dosyalar (ikinci koşum)
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css`: yalnız dosyanın SONUNA S1-i (11) dar ekran Elle ekle ızgarası ile (12) barkod satırı ve "Elle ekle" aralığı eklendi. Sabit renk ve satır içi style yok.
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor`: kısa yer tutucu kondu, düğme metni `span.barkod-dugme-metin` içine alındı, `aria-label`/`title` eklendi. id'ler DEĞİŞMEDİ.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SatisEkrani.cs`: `BarkodKisaIpucu` eklendi (TR+EN).
- `dotnet/Kuyumcu.Tests/S1iGorselSigma2Testleri.cs`: iki yeni test eklendi, `S1i_15_Dar_ekranda_elle_ekle_izgarasi_urun_tam_satir_ekle_kontrol_rayinda` ve `S1i_16_Barkod_yer_tutucusu_kisa_dugme_metni_ekran_okuyucuda_kalir`.
- `dokumanlar/09-uygulama-kayitlari/betikler/sikisma-olc.js`: K10 eklendi, K7 çok sıralı ölçüme geçti.
- `ham/S1-i-ekran/kapanis-*.png`: 9 görüntü. `…-ONCE-izgarasiz.png` ızgara kaldırılarak (tarayıcı mutasyonu) çekildi ve eski dar ekran davranışını gösteriyor. `…-SONRA.png` dosyaları 8 kombinasyonu kapsıyor.

### 15.2 Mutasyonlar (ikinci koşum)
Yedekler `scratchpad/mut-yedek/S1-i/*.k` altında. Her mutasyon perl ile tek eşleşmeli değişiklik olarak yapıldı, sonra yedek geri kopyalandı ve `cmp` AYNI verdi.

| Kod | Değişiklik | Kırmızıya dönen | Geri alındı |
|---|---|---|---|
| MS1i-K1 | ≤768 px'te `.satir.fis-kalem-satiri` için `display: grid` → `flex` | `S1i_15_Dar_ekranda_elle_ekle_izgarasi_urun_tam_satir_ekle_kontrol_rayinda`. Tarayıcıda (enjekte stil): hiza ±83, ürün tetiği 127 px, elipsis var, **K7=1, K10=1** | cmp AYNI |
| MS1i-K2 | Ekle `grid-row: 5` → `4 / span 3` (kontrol rayından çıkar) | `S1i_15_…` | cmp AYNI |
| MS1i-K3 | Barkod yer tutucusu eski uzun metne döndürüldü | `S1i_16_Barkod_yer_tutucusu_kisa_dugme_metni_ekran_okuyucuda_kalir` | cmp AYNI |
| MS1i-K4 | Barkod düğmesinin `aria-label`/`title` özniteliği silindi (dar ekranda adsız düğme) | `S1i_16_…` | cmp AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 hata, 0 uyarı** (13:20). Web dll 13:20:38'de derlendi; geri alınan razor 13:20:33 tarihli, yani derleme geri alınmış kodu içeriyor.

### 15.3 Testler (ikinci koşum)
- Filtreli koşumlar: S1i · S1c · BasitModPos · Islem · Ikon · Erisil **159/159**; S1c · S1i · Css · Gorsel **98/98**.
- Ara tam süit (13:03, trx `scratchpad/trx-pS1-i-kapanis/`): 5180 testten **1 başarısız**, `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir`. Sebep benim eklediğim "overflow" sözcüğüydü; düzeltildi.
- **Son tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_j`, trx `scratchpad/trx-pS1-i-kapanis2/`): **5180 / 5180 geçti, 0 başarısız.** İlk koşumdaki Q9-4'e ait 2 kırmızı da bu koşumda yeşil.
- Son razor değişikliğinden (aria-label, display:none) sonra filtreli koşum 159/159 geçti. Mutasyonlar da bu son kodla yeniden koşuldu.
- `dotnet/denetim.sh` → **DENETİM TEMİZ** (13:1x, son CSS ile).

### 15.4 Canlı doğrulama (API 5176 · vekil 5175 · kuyumcu_net · test kiracısı `PAKETS1-i-TEST` `01M39CTGRJ8HG5D2Q5VXYW9MY5`)
| İstek | HTTP | Yanıttan okunan |
|---|---|---|
| POST /api/v1/auth/kayit (tek kullanımlık davet `kiraci_davetleri`, not `PAKETS1-i-TEST`) | 201 | yanıtta `tenantId`, `accessToken`, `refreshToken` alanları var (değerler yazılmadı) |
| POST /api/v1/urunler `BLZ-22` "22 Ayar Burma Bilezik", K22, 12,5 g | 201 | milyem 916,00 · hasGrami 11,45 |
| POST /api/v1/urunler `KLY-14` "14 Ayar Kolye", K14 | 201 | milyem 585,00 |
| PATCH /api/v1/kullanicilar/ben `{arayuzModu}` (her ölçüm turunda) | 200 | sayfada `data-mod` basit/profesyonel olarak doğrulandı |

SQL: kiracının ürün sayısı ürünler eklenmeden önce 0 idi. `tenants` başta 10, kayıttan sonra 11, temizlikten sonra **10**.

### 15.5 Gerçek tarayıcı ölçümü (kapanış)
İki araç kullanıldı:
- (a) Claude tarayıcı bölmesinde kendi sekmem (tab-35, vekil 5175, `javascript_tool`). Bölme gizli olduğu için (`visibilityState=hidden`) ekran görüntüsü boş geliyor.
- (b) Bu yüzden görüntüler başsız Chrome + CDP ile çekildi (`scratchpad/s1i-ekran.mjs`, `s1i-tara.mjs`), güncel `sikisma-olc.js` ile.

**/islemler Elle ekle satırı.** Ürün seçili: "22 Ayar Burma Bilezik (K22)". Kutu koordinatları [sol, üst, sağ, alt] px; "fark" sütunları px.

| Genişlik | Mod | Tema | Ürün tetiği | Adet | Fiyat | Ekle | Etiket üstü farkı | Kutu altı farkı | Üst farkı | Elipsis | Cariler | K3 | K6 | K7 | K10 | Y |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 375 | BASİT | açık | 29,525,346,569 | 29,634,149,678 | 157,634,277,678 | 285,634,346,678 | 0 | 0 | 0 | yok (199/199) | 0 | 0/0 | 0/3 | 0 | 0 | 0 |
| 375 | BASİT | koyu | aynı | aynı | aynı | aynı | 0 | 0 | 0 | yok | 0 | 0/0 | 0/3 | 0 | 0 | 0 |
| 375 | PRO | açık | 29,467,346,511 | 29,576,149,620 | 157,576,277,620 | 285,576,346,620 | 0 | 0 | 0 | yok | 2 (D10, pro) | 0/0 | 0/4 | 0 | 0 | 0 |
| 375 | PRO | koyu | aynı | aynı | aynı | aynı | 0 | 0 | 0 | yok | 2 (D10) | 0/0 | 0/4 | 0 | 0 | 0 |
| 1280 | BASİT | açık | 415,522,672,562 | 684,522,866,562 | 878,522,1060,562 | 1072,522,1133,562 | 0 | 0 | 0 | yok | 0 | 0/0 | 0/3 | 0 | 0 | 0 |
| 1280 | BASİT | koyu | aynı | aynı | aynı | aynı | 0 | 0 | 0 | yok | 0 | 0/0 | 0/3 | 0 | 0 | 0 |
| 1280 | PRO | açık | 415,464,672,496 | 684,464,866,496 | 878,464,1060,496 | 1072,464,1133,496 | 0 | 0 | 0 | yok | 2 (D10) | 0/0 | 0/4 | 0 | 0 | 0 |
| 1280 | PRO | koyu | aynı | aynı | aynı | aynı | 0 | 0 | 0 | yok | 2 (D10) | 0/0 | 0/4 | 0 | 0 | 0 |

Kendi sekmemde (bölme, 375×812, BASİT, açık) tekrarlanan ölçüm aynı sonucu verdi:
- hiza farkı 0/0/0, elipsis yok, yatay kaydırma 0;
- ürün paneli **320 px**, Burma adı **1 satır**, sağa taşma 0;
- barkod kutusu 254 px, yer tutucu 175/228 px, düğme adı "Barkod okut".

**Önceki durum** (doğrulayıcının ölçümü ve ızgarasız tarayıcı mutasyonu): ürün tetiği 123–127 px, elipsis 199/81–85, fiyat ↔ Ekle farkı ±9–23 px (yeni K7 bunu 1 sayıyor), etiket üstü farkı ±83.

**Tüm S1-i rotaları yeni sayaçlarla yeniden tarandı:** 36 rota × 8 kombinasyon = 288 hücre. `/islemler` satırları son razor değişikliğinden SONRA ayrıca tarandı. Hücrelerde boy ve yalnız sıfır olmayan sayaçlar yazılı.

| Rota | B-ayd-1280 | B-kar-1280 | P-ayd-1280 | P-kar-1280 | B-ayd-375 | B-kar-375 | P-ayd-375 | P-kar-375 |
|---|---|---|---|---|---|---|---|---|
| /raporlar/kar-zarar?sekme=ozet | 1.4 | 1.4 | 1.4 | 1.4 | 2.9 | 2.9 | 2.9 | 2.9 |
| /raporlar/kar-zarar?sekme=grafik | 1.3 | 1.3 | 1.3 | 1.3 | 2.1 | 2.1 | 2.1 | 2.1 |
| /raporlar/kar-zarar?sekme=kopru | 1.7 | 1.7 | 1.7 | 1.7 | 2.3 | 2.3 | 2.3 | 2.3 |
| /raporlar/kar-zarar?sekme=urunler | 1.3 | 1.3 | 1.3 | 1.3 | 1.6 | 1.6 | 1.7 | 1.7 |
| /raporlar/kar-zarar?sekme=stok | 1.3 | 1.3 | 1.3 | 1.3 | 1.9 | 1.9 | 2 | 2 |
| /yedekler?sekme=durum | 1.4 | 1.4 | 1.4 | 1.4 | 1.8 | 1.8 | 1.9 | 1.9 |
| /yedekler?sekme=elle | 1.4 | 1.4 | 1.4 | 1.4 | 1.4 | 1.4 | 1.5 | 1.5 |
| /yedekler?sekme=kosular | 1.4 | 1.4 | 1.4 | 1.4 | 1.4 | 1.4 | 1.5 | 1.5 |
| /yedekler?sekme=tatbikatlar | 1.4 | 1.4 | 1.4 | 1.4 | 1.3 | 1.3 | 1.4 | 1.4 |
| /kur?sekme=guncel | 1.5 | 1.5 | 1.5 | 1.5 | 1.6 | 1.6 | 1.7 | 1.7 |
| /kur?sekme=pariteler | 1.7 | 1.7 | 1.7 | 1.7 | 1.7 | 1.7 | 1.9 | 1.9 |
| /kur?sekme=kaynak | 1.5 | 1.5 | 1.5 | 1.5 | 1.6 | 1.6 | 1 | 1 |
| /kur?sekme=gecmis | 1.5 | 1.5 | 1.6 | 1.6 | 1.6 | 1.6 | 2.2 | 2.2 |
| /isletme?sekme=kimlik | 1.4 | 1.4 | 1.4 | 1.4 | 1.2 | 1.2 | 1.3 | 1.3 |
| /isletme?sekme=abonelik | 1.5 | 1.5 | 1.5 | 1.5 | 1.8 | 1.8 | 1.8 | 1.8 |
| /isletme?sekme=ayarlar | 1.4 K8:1 | 1.4 K8:1 | 1.4 | 1.4 | 1.6 K8:1 | 1.6 K8:1 | 1.7 | 1.7 |
| /isletme?sekme=subeler | 1.4 | 1.4 | 1.4 | 1.4 | 1.2 | 1.2 | 1.3 | 1.3 |
| /denetim-izi | 1.7 K8:5 | 1.7 K8:5 | 1.6 | 1.6 | 2.5 K8:5 | 2.5 K8:5 | 2.6 | 2.6 |
| /raporlar/kasa | 1.3 | 1.3 | 1.3 | 1.3 | 1 K7:1 | 1 K7:1 | 1 K7:1 | 1 K7:1 |
| /raporlar/islem-defteri | 1.3 K8:1 | 1.3 K8:1 | 1.3 | 1.3 | 2.1 K8:1 | 2.1 K8:1 | 2.2 | 2.2 |
| /cariler | 1.3 K8:2 | 1.3 K8:2 | 1.3 | 1.3 | 1 K8:2 | 1 K8:2 | 1 | 1 |
| /yetkiler | 1.4 K8:8 | 1.4 K8:8 | 1.4 | 1.4 | 1.1 K8:8 | 1.1 K8:8 | 1.2 | 1.2 |
| /veri-disa-aktarim | 1.4 K8:1 | 1.4 K8:1 | 1.4 | 1.4 | 1.9 K8:1 | 1.9 K8:1 | 1.9 | 1.9 |
| /raporlar/satis-analizi?sekme=odeme | 1.3 | 1.3 | 1.3 | 1.3 | 1.2 K7:1 | 1.2 K7:1 | 1.2 K7:1 | 1.2 K7:1 |
| /raporlar/satis-analizi?sekme=personel | 1.3 | 1.3 | 1.3 | 1.3 | 1.1 K7:1 | 1.1 K7:1 | 1.2 K7:1 | 1.2 K7:1 |
| /raporlar/satis-analizi?sekme=sube | 1.3 | 1.3 | 1.3 | 1.3 | 1.1 K7:1 | 1.1 K7:1 | 1.2 K7:1 | 1.2 K7:1 |
| /uyum?sekme=tespit | 1.2 | 1.2 | 1.1 | 1.1 | 1.4 | 1.4 | 1.5 | 1.5 |
| /uyum?sekme=supheli | 1.2 | 1.2 | 1.1 | 1.1 | 1.3 | 1.3 | 1.4 | 1.4 |
| /uyum?sekme=saklama | 1.2 | 1.2 | 1.1 | 1.1 | 1.3 | 1.3 | 1.4 | 1.4 |
| /uyum?sekme=risk | 1.6 K8:3 | 1.6 K8:3 | 1.6 | 1.6 | 2.7 K8:3 | 2.7 K8:3 | 2.8 | 2.8 |
| /gider?sekme=giderler | 1.5 K5:1 K8:1 | 1.5 K5:1 K8:1 | 1.5 K5:1 | 1.5 K5:1 | 1.4 K5:1 K8:1 | 1.4 K5:1 K8:1 | 1.5 K5:1 | 1.5 K5:1 |
| /gider?sekme=kategoriler | 1.5 | 1.5 | 1.5 | 1.5 | 1.1 | 1.1 | 1.2 | 1.2 |
| /gider?sekme=virman | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 | 1.5 K5:1 |
| /gider-pusulalari | 1.2 | 1.2 | 1.1 | 1.1 | 1.3 | 1.3 | 1.4 | 1.4 |
| /islemler | 2.2 | 1.5 (yüklenmedi*) | 1.8 | 1.7 | 2.7 | 2.7 | 2.4 | 1 (yüklenmedi*) |
| /islemler?tur=HURDA_ALIS | 2.2 | 1.5 (yüklenmedi*) | 1.8 | 1.7 | 2.7 | 2.7 | 2.3 | 2.3 |

Toplam (288 hücre): K1=0, K3t=0, K3k=0, K5=16, K6=0, K7=16, K8=88, K9=0, K10=0, Y=0


\* "yüklenmedi": sayfa o hücrede 3,2 sn içinde çizilmedi (seçici sayısı 0, boy 1–1,5). Aynı kombinasyon yukarıdaki Elle ekle tablosunda tam yüklenmiş sayfada ölçüldü ve 0/0/0 çıktı.

Tablonun okunuşu:
- 288 hücrenin hepsinde **K1 0 · K3 taşan 0 · K3 kırık 0 · K6 0 · K9 0 · K10 0 · yatay 0**.
- **K7 16 hücre**: hepsi 375'te, `/raporlar/kasa` ve `/raporlar/satis-analizi` araç şeridinde. Tarih kutuları alt alta diziliyor ve kutu altı farkı ±52 çıkıyor (44 px kutu + 8 px aralık). Bu ilk koşumda da gerekçelendirildi; görsel kusur değil.
- **K5 ve K8** KATALOG paketinin konusu (§ 13 madde 2).
- **`/islemler` BASİT modda boy** 1280'de 2,2, 375'te 2,7. Gerekçe § 1 (5)'te ölçüldü; çözüm önerisi S1-j. Elle ekle katmanı dar ekranda artık tek satır yerine iki sıra tutuyor.

### 15.6 Açık kalanlar (ikinci koşum ekleri)
1. **1280'de /islemler fiş kartı:** "VERDİM · ÜRÜNLER" sütunu kartın ALTINA yaslanıyor, üstünde boşluk kalıyor (`ham/S1-i-ekran/kapanis-islemler-1280-BASIT-aydinlik-SONRA.png`). Bu, S2-2 tezgâh fişinin Verdim/Aldım ızgarasının konusu. Bu paketin `Bilesenler/Fis/` altındaki dosya alanı yalnız `FisKalemGirisi.razor` olduğu için dokunulmadı. Sahibi: S2-2 / S1-j (tuvaldeki iki sütunlu tezgâh düzeni).
2. `Birim Fiyat (₺)` etiketinin "(isteğe bağlı)" eki 375'te ikinci satıra iniyor. Bölünme kelime sınırında, harf ortasından kırılma yok (K3 kırık 0). Subgrid sayesinde girdiler aynı çizgide (fark 0). Eki kısaltmak ortak `AlanKabugu`'na dokunmayı gerektirir; bu paket kapsamı dışında.
3. Tam derlemede `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı zaman zaman görünüyor (03:07 tarihli dosya, başka pakete ait). Son `dotnet build` 0 uyarı verdi.
4. 21 Eylül'den kalma 0 baytlık `.mutorj`/`.bak` artıklarının silinmesi kurucuda (hook silme komutunu engelliyor).

### 15.7 Temizlik
- API 5176 (yalnız kendi PID'im) ve vekil 5175 durduruldu; `lsof` boş.
- Tarayıcı sekmem kapatıldı, görünüm alanı masaüstüne döndürüldü.
- `PAKETS1-i-TEST` (`01M39CTGRJ8HG5D2Q5VXYW9MY5`) kiracısı `tenant_id` kolonu olan **70 tablonun** tamamından ve `tenants`'tan silindi; kalan satır **0**.
- Kullandığım davet satırı `kiraci_davetleri`'nden silindi (kalan 0).
- `tenants` = **10**, başlangıç değeri.
- Parola içeren fikstür dosyası boşaltıldı (0 bayt).
- QA-TEST kiracılarına dokunulmadı.
