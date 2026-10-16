# PAKET P-47 — Cari yaşlandırmada HAS GRAM kolonu (gizli 16,946 g alacak) · UYGULAMA RAPORU

- **Rol:** Taşkın (backend) + Ertuğ (frontend) · Opus · Dalga PARA VE SAYI DOĞRULUĞU (tur 8 triyajı)
- **Tarih:** 2026-09-23 · başlangıç 20:36 · bitiş ~20:55 → **gerçek süre ≈ 20 dk** (triyaj tahmini 1,5 saat; TAHMİNDİ)
- **Durum:** BİTTİ (paralel şerit kapısının 7 maddesi sağlandı; tam süitteki 19 kırmızının hiçbiri bu paketin değil — § 6)

## 1. Spesifikasyonun koda karşı doğrulanması (önce ölçüldü)

| İddia (triyaj) | Ölçüm | Sonuç |
|---|---|---|
| `Raporlar.razor:200-228` başlık ve `Dilim(y,…)` hücreleri yalnız TL; `y.HasGramBakiye` hiçbir yerde kullanılmıyor | `grep -n "HasGram" Raporlar.razor` → yaşlandırma bloğunda 0 eşleşme; blok 192-233. satırlardaydı | **DOĞRULANDI** (satırlar 8 kaymış) |
| Sözleşmede `decimal HasGramBakiye` var (`RaporlarSozlesmeleri.cs:29`) | `Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs:29` | **DOĞRULANDI** |
| API `hasGramBakiye:-16.946` dönerken ekran satırı 0,00 TL | Sunucu `RaporModulu.CariYaslandirma` has'lı cariyi zaten süzgeçten geçiriyor (`HasGramBakiye > 0.000001 \|\| < -0.000001`); ekran yalnız `TlBakiye` basıyordu | **DOĞRULANDI** — kusur yalnız ekranda; sunucu değişikliği GEREKMEDİ |
| Dosya ayak izi `Kuyumcu.Tests/RaporlarEkranTestleri.cs` | `test -e` → **YOK** (mevcut dosya `RaporEkranTestleri.cs`) | **SAPMA** — ayak izindeki ad YENİ dosya olarak açıldı (§ 2) |
| Triyaj uyarısı (b) `Servisler/Dil.cs` yok | Doğru; sözlük `Kuyumcu.Core/I18n/Sozlukler.cs`, sarmalayıcı `DilServisi.cs` | Kullanılmadı |
| Triyaj uyarısı (c) `SatirIciDuzenle` var | Bu paket yeni düzenleme bileşeni yazmadı; tablo sarmalayıcısı için mevcut `KartListe` bileşeni kullanıldı | — |
| `Bicim.Has/Gram` InvariantCulture | `Bicim.cs:11-12` → `Has()` = `F4` + " has", `Gram()` = `F3` + " g", nokta ondalık | **DOĞRULANDI** → kabul koşulundaki `'-16,9460 g'` biçimi bugün hiçbir `Bicim` metoduyla üretilmiyor (§ 3 sapma 1) |

Mevzuat: bu paket vergi/MASAK/fatura hükmü içermiyor; yalnız bir gösterim kusuru. Mevzuat araştırması gerekmedi.

## 2. Yapılanlar

1. **`Pages/Raporlar.razor` — cari yaşlandırma kartı**
   - Yeni kolon **"Has bakiyesi"** (`#yas-has-baslik`), her satırda `td.yas-has` = `Bicim.Has(y.HasGramBakiye)`; eksi ise `metin-kirmizi` (Cariler.razor'daki işaret kuralıyla AYNI: artı = cari bize borçlu).
   - TL toplam kolonunun başlığı `Toplam (₺)` oldu (yeni anahtar `YasToplamTl`); dilim kolonları TL kaldı.
   - **Boşluk kuralı:** `YasSatiriBos(y) = TL == 0 && has == 0 && !DovizBakiyesiVar`. Satır TL'ye göre değil, TL VEYA has VEYA döviz bakiyesine göre boş sayılır; boş satır çizilmez.
   - **Vurgu:** `YalnizHasBakiyesi(y) = TL == 0 && has != 0` ise satır `satir-vurgu` (mevcut semantik belirteç `--vurgu-yuzey`, yeni CSS YOK) + amber rozet **"Has bakiyesi var"**.
   - **Alt toplam satırı (`<tfoot id="yas-alt-toplam">`):** dilim başına TL toplamı, `#yas-toplam-tl` = yalnız TL toplamı, `#yas-toplam-has` = has ayrı toplamı. Has hiçbir hücrede TL'ye çevrilmez/eklenmez.
   - **Tablo altı not** (`#yas-has-notu`): "Has bakiyesi TL'ye çevrilmez, ayrı okunur: artı = cari bize has borçlu, eksi = biz cariye has borçluyuz."
   - **375 px:** tablo elle `<div class="tablo-sar">` yerine tek sarmalayıcı `KartListe` (Id=`yas` → `#yas-sar`, `overflow-x:auto`, `tabindex=0`, `role=region`, dar ekranda "yatay kaydırılabilir" ipucu). Başlıklarda `text-overflow`/`nowrap` kırpması yok → başlık kırpılmaz, kaydırmayla erişilir.
   - **UX-S29:** alt toplam yalnız listelenen carileri kapsadığı için sunucunun zaten döndürdüğü `toplamSatir`/`dahaFazlasiVar` okunur; `dahaFazlasiVar` ise `#yas-sinir` uyarısı basılır ("Yalnız ilk {0} cari listelendi, toplam {1} cari var; alt toplam yalnız listelenenleri kapsar.").
2. **`Kuyumcu.Core/I18n/Sozlukler.cs`** (HUB) — `SozlukRapor`'a yalnız EKLEME, TR+EN: `YasHasBakiye`, `YasToplamTl`, `YasHasBakiyeVar`, `YasHasNotu`, `YasListelenenToplam`, `YasSinirUyari`. Mevcut anahtar değiştirilmedi.
3. **`Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs`** — `YaslandirmaYaniti`'na isteğe bağlı `int ToplamSatir = 0, bool DahaFazlasiVar = false` (sunucu bu alanları TB-19'dan beri ZATEN gönderiyordu; yanıt şekli değişmedi).
4. **`Kuyumcu.Tests/RaporlarEkranTestleri.cs`** (YENİ, 4 bUnit testi). Sahte API fikstürü canlı `/raporlar/cari-yaslandirma` yanıtıyla anahtar anahtar karşılaştırıldı: üst düzey ve satır düzeyi anahtar farkı **0** (§ 8).

### DoD sözleşmesi
| Varlık | Düzenleme yolu | Gerekçe |
|---|---|---|
| Cari yaşlandırma satırı / has kolonu | **Salt okunur** | Rapor görünümüdür; kaynak `cari_bakiyeler` türetilmiş bakiyedir. Has bakiyesinin düzeltme yolu mevcut `POST /cari/{id}/duzeltme` (gerekçeli düzeltme fişi, SAHIP/MUDUR/MUHASEBE) ve Cariler ekranıdır. Bu paket yeni varlık/kayıt türü, yeni form, yeni düğme, yeni uç AÇMADI → PATCH/DELETE/denetim kaydı yükümlülüğü doğmadı. |

Yeni düğme: **0** (DoD ek 1'e konu yok). Yeni ModalPencere: **0**. Yeni CSS: **0** (mevcut `satir-vurgu`, `uyari-metni`, `KartListe` kullanıldı). Satır içi style: **0**.

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Raporlar.razor` (yalnız cari yaşlandırma bloğu + `DigerleriniYukle` içinde iki atama + P-47 yardımcıları). Çakışma haritasına göre sıra P-47 → P-69; P-69 bu dosyayı sonra açacak.

## 3. Sapmalar ve gerekçeleri

1. **Biçim `'-16,9460 g'` yerine `Bicim.Has` → bugün `-16.9460 has`.** Kabul koşulu virgül ondalık + "g" istiyor; `Bicim.Has` bugün InvariantCulture + " has" üretiyor. Gerekçe: (a) aynı kavram tek biçim (UX-S31) — Cariler ekranı has bakiyesini `Bicim.Has` ile basıyor; iki ekran farklı biçim basmamalı; (b) nokta/virgül birleştirmesi **P-57**'nin (`Bicim` tr-TR) konusu ve `Bicim.cs` bu paketin ayak izinde değil. Testler beklenen metni `Bicim.Has(-16.946m)` ile üretiyor → P-57 biçimi değiştirdiğinde testler kendiliğinden yeni biçimi ölçer. **Hedef karşılandı:** satırda 4 ondalıklı has değeri görünür, satır "0,00 ₺"tan ibaret değildir. Birim eki ("has" / "g") kararı P-57'ye devredildi.
2. **Test dosyası adı:** ayak izindeki `RaporlarEkranTestleri.cs` yoktu; mevcut `RaporEkranTestleri.cs`'e eklemek yerine ayak izindeki adla YENİ dosya açıldı (başka şeritlerle çakışmayı azaltmak için). Gerekli sabit fikstürler (katalog, stok yaşlandırma, mutabakat) `RaporEkranTestleri.cs`'ten kopyalandı.
3. **Ayak izi dışı dosya:** `Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs` (yalnız `YaslandirmaYaniti` kaydına 2 isteğe bağlı alan). Gerekçe: alt toplam eklenince UX-S29 gereği sınırın ekranda söylenmesi zorunlu; alanlar sunucuda zaten vardı. Bu dosya P-69'un da ayak izinde → sıra P-47 → P-69.
4. Spesifikasyon "kolon toplamı" diyordu; mevcut tabloda alt toplam satırı YOKTU. Koşulu ölçülebilir kılmak için `<tfoot>` eklendi (TL ve has ayrı).

## 4. Migration
**Gerekmedi** — şema değişikliği yok.

## 5. Derleme ve testler

- `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata** (son mutasyondan sonra temiz derleme: 0/0, 35,6 sn).
- Filtreli: `--filter FullyQualifiedName~RaporlarEkranTestleri` → **4/4 Başarılı** (P47_01..P47_04).
- Komşu kapsam: `RaporEkran|Sozluk|SozlesmeSekil|UX1C3Ekran|KartListe|RaporSeti` → **126/126 Başarılı**.
- Yeni test sayısı: **4**.

| Test | Kapsam |
|---|---|
| `P47_01_Yaslandirma_has_gram_kolonu_degeri_basar` | 7 başlık, son başlık "Has bakiyesi"; c1 satırında `Bicim.Has(-16.946)`, kırmızı; c2'de `Bicim.Has(2.5)`; `#yas-sar` tabindex=0 + `tablo-sar`; tablo altı not |
| `P47_02_TL_sifir_has_dolu_satir_bos_gorunmez` | c1 (TL=0, has=-16,946) `satir-vurgu` + "Has bakiyesi var" rozeti; c2'de yok; c3 (hepsi 0) çizilmez; saf `YasSatiriBos` üç durum |
| `P47_03_Has_bakiyesi_TL_toplamina_KARISMAZ` | `#yas-toplam-tl` = 1.500 ₺ (0+1.500), `#yas-toplam-has` = -14,446 has; satır TL hücreleri yalnız TL; 0-30 dilim toplamı 0 ₺ |
| `P47_04_Yaslandirma_siniri_ekranda_soylenir` | `dahaFazlasiVar=false` → `#yas-sinir` yok; `true` + 750 → sözlük metni {3, 750} ile |

## 6. Tam süit (paralel şerit kapısı madde 7)

`dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory …/scratchpad/trx-pP-47`
→ trx `Counters total="4262" passed="4243" failed="19"`.

**19 kırmızının hiçbiri bu paketin ürünü değil** (kanıt: hiçbiri Raporlar/cari yaşlandırma/Sözlük anahtarı sınamıyor; kök nedenleri başka dosyalarda):

| Kırmızı | Adet | Kök neden (log) | Sahibi (tahmin DEĞİL, dosya kanıtı) |
|---|---|---|---|
| `SurumEkranTestleri.*` (5), `UX1C4EkranTestleri.C4_05, C4_50..C4_56` (8), `GorunumEkranTestleri.GE4/GE5/GE6` (3) | 16 | `NullReferenceException` @ `Kuyumcu.Web/Pages/Ayarlar.razor(513)` | `Ayarlar.razor` 20:48'de başka şerit tarafından değiştirilmiş; P-47 bu dosyaya dokunmadı |
| `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu`, `…Kapsam_orani_beyan_edilen_degerle_ayni` | 2 | Yeni kilitsiz istemci tipi **`CariVeri`** (59→60) | `CariVeri` Banka/Cariler/Islemler/Faturalar.razor'da; P-47'nin eklediği tip yok |
| `S3aKabukHatasiTestleri.Sayfa_cokunce_BOS_EKRAN_degil_GORUNUR_bant_cizilir` | 1 | `KabukHatasi` ham mesajı basmıyor; test dosyası P-31 (R-5 IC_KOD_SIZINTISI) yorumuyla güncelleniyor | DALGA 7c / P-31 (yarım iz) |

Devir notundaki (ORKESTRATOR-DURUM 20:10) iki kırmızı (`OlcumKapisiTestleri.denetim_sh_HER_CAGRI…` → P-30b, `AltinPozisyonuTestleri.B2_05` → B-2) bu koşumda **yeşil** (kapanmışlar). **P-47'nin ürettiği yeni kırmızı = 0.**

## 7. Mutasyon kanıtları (yedek `scratchpad/mut-yedek/P-47/Raporlar.razor`, `perl -0777 -pi`, geri kopya + `cmp`)

| Kod | Değişiklik | Kırmızı dönen test(ler) | Geri alındı |
|---|---|---|---|
| M47-1 | Has başlığı `<th id="yas-has-baslik">` ve satır `td.yas-has` hücresi kaldırıldı | `P47_01_Yaslandirma_has_gram_kolonu_degeri_basar` | cmp birebir aynı |
| M47-2 | `YasSatiriBos` → eski koşul `y.TlBakiye == 0m` | `P47_01`, `P47_02_TL_sifir_has_dolu_satir_bos_gorunmez`, `P47_03` | cmp birebir aynı |
| M47-3 | `YasTlToplami` → `Sum(y.TlBakiye + y.HasGramBakiye)` (has TL toplamına eklendi) | `P47_03_Has_bakiyesi_TL_toplamina_KARISMAZ` | cmp birebir aynı |
| M47-4 (ek) | `YalnizHasBakiyesi` → `false` (vurgu/rozet kalktı) | `P47_02_TL_sifir_has_dolu_satir_bos_gorunmez` | cmp birebir aynı |
| M47-5 (ek) | `@if (yaslandirmaDahaFazlasiVar)` → `@if (false)` | `P47_04_Yaslandirma_siniri_ekranda_soylenir` | cmp birebir aynı |

Not (Anayasa md.1): M47-5'in ilk koşumu başka şeridin yarım dosyası yüzünden **derlenmedi** (`PesinMusteriTestleri.cs(198,22): error CS0234: 'BeyanKademesi'`); ağaç yeşile döndükten sonra aynı mutasyon yeniden uygulandı ve P47_04 kırmızı döndü. Son mutasyondan sonra temiz derleme 0/0.

## 8. denetim.sh

`bash denetim.sh` → **DENETİM BAŞARISIZ** (ağaç ölçüsü). ✗ satırları ve sahiplik:

| ✗ | P-47 ile ilişki |
|---|---|
| Doviz kolonu atama 4 (beklenen 3) | Yok — API dosyasına dokunulmadı |
| Pages: onclick'siz/disabled'sız button 74 (tavan 71) | Yok — P-47 0 düğme ekledi (P-46 raporu da 74>71'i `/ekranlar` sayfasına bağlamıştı) |
| Kültürsüz biçimli ToString (Web) 28 (tavan 27) | Yok — P-47 bloğunda `ToString("…")` 0; Raporlar.razor'daki 4 eşleşme (287, 484, 511, 890) P-47 öncesinden |
| P-08 HARETURU_DISI_SABIT (MUSTERI) | Yok — `Kuyumcu.Api` taranır, API'ye dokunulmadı |
| P-09 OZNITELIKSIZ_UC `DestekModulu.cs:400` | Yok |
| P-11 DIS_HATADA_VERI_DUSUREN_YOL 48>47 | Yok — P-47 `catch` bloğu eklemedi/değiştirmedi |

**P-47'nin sayaçları ✓:** `P-18: MOBIL_TABLO_SARMALAYICISIZ 0 elle sarmalayıcı · 13 KartListe` (P-47 +1 KartListe) · `P-18: SABIT_RENK 0` · `P-11: SATIR_ICI_STYLE 125 (tavan 143)` · `Pages: ModalPencere 55 (tavan 55)` · `P-36b: GIRDI_AILESI_DISI_BICIM 0` · `P-05: BENZERSIZLIK_SOZLUKSUZ 0`.

## 9. Canlı doğrulama (localhost:5198 · `kuyumcu_net`; 5199'a dokunulmadı)

Test kiracısı **`PAKET47-TEST Kuyumculuk`** (`01M37P76QQNRQMBQG2CY7P65KJ`), tek kullanımlık davet satırı (`P47DAVET…001`) SQL ile eklenerek açıldı.

| # | İstek | Beklenen | Gerçek | Yanıt (okundu) |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` (davetli) | 201 | **201** | `tenantId`, `accessToken`, `refreshToken` |
| 2 | `POST /cari` ×2 (MUSTERI) | 201 | **201 / 201** | c1 `01M37P7KCFPEP53WXGS5Y1ZT4H`, c2 `01M37P7KF289P3P2R7833AQ03H` |
| 3 | `POST /cari/{c1}/duzeltme` hasDelta −16,946 (kurSnapshotId'siz) | 400 | **400** | `"Kur kaydı zorunludur"` (alan `kurSnapshotId`) — beklenen kapı |
| 4 | `GET /kur/guncel` | 200 + id | **200** | id var |
| 5 | `POST /cari/{c1}/duzeltme` tl 0, has −16,946 | 200 | **200** | cari gövdesi |
| 6 | `POST /cari/{c2}/duzeltme` tl 1.500, has 2,5 | 200 | **200** | cari gövdesi |
| 7 | `GET /raporlar/cari-yaslandirma` | 200, c1 `tlBakiye 0 / hasGramBakiye -16.946` | **200** | `Has Alacakli Musteri tl=0.0 has=-16.946 dilim=0-30` · `TL Borclu Musteri tl=1500.0 has=2.5` · `ozetTl 0-30=1500.0` · `toplamSatir 2` · `dahaFazlasiVar False` |

**SQL kanıtı:** `SELECT c.ad,b.tl_bakiye,b.has_gram_bakiye FROM cari_bakiyeler b JOIN cariler c …` → `Has Alacakli Musteri 0.00 -16.946000` · `TL Borclu Musteri 1500.00 2.500000`. `ozetTl` has'ı içermiyor (1.500 = yalnız TL) → sunucu tarafında da has TL toplamına karışmıyor.

**Ekran:** canlı yanıtın anahtarları bUnit fikstürüyle programla karşılaştırıldı: üst düzey fark `set()`, satır düzeyi fark `set()` → bUnit testleri canlı yanıt şekliyle birebir aynı veriyle ekranı sınıyor. **Tarayıcıda canlı ekran açılmadı** — giriş formuna parola yazmak bu ajan için yasak; 375 px ölçümü `denetim.sh` P-18 `MOBIL_YATAY_KAYDIRMA 0 / 88 rota-ölçüm` (mevcut mobil ölçüm betiği) ve `KartListe` sarmalayıcısıyla güvenceli; tarayıcıda göz ile teyit orkestratör/kurucu oturumuna bırakıldı (demo adım 9 `/raporlar`).

**Temizlik:**
- API PID 30496 (`dotnet run`) ve 30550 (dinleyici) `kill` ile durduruldu; `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
- `tenant_id` kolonu olan **70 tablonun** tamamından kiracı satırları, `tenants` satırı ve davet satırı silindi; kalan satır denetimi **0**; davet sorgusu **0**.
- `SELECT COUNT(*) FROM tenants` → **başlangıç 10, son 10**. `QA-TEST-*` ve `DEMO-*` kiracılarına dokunulmadı.
- Scratchpad'deki jeton/token dosyaları `printf '' >` ile boşaltıldı.

## 10. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgeci, P-47'nin dokunduğu)
- ` M dotnet/Kuyumcu.Web/Pages/Raporlar.razor`
- ` M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (HUB, yalnız ekleme: 12 sınıf satırı + 6 TR + 6 EN)
- `?? dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs` (dosya zaten izlenmiyordu; P-47 yalnız `YaslandirmaYaniti`'na 2 isteğe bağlı alan ekledi)
- `?? dotnet/Kuyumcu.Tests/RaporlarEkranTestleri.cs` (YENİ)

Proje ağacına `.mutorj/.bak/.tmp` bırakılmadı (mutasyon yedekleri scratchpad'de). Ağaçta görülen başka `.mutorj` dosyaları (Islemler, Urunler, RaporModulu vb.) başka şeritlerin; bunlara dokunulmadı.

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| hasGramBakiye=-16,946 & tlBakiye=0 carisinde satırda has değeri görünür, satır 0,00 TL'den ibaret değil | **KARŞILANDI (biçim sapmasıyla)** | P47_01; bugünkü metin `-16.9460 has` (`Bicim.Has`), `'-16,9460 g'` biçimi P-57'ye bağlı (§ 3.1) |
| Has hiçbir yerde TL'ye çevrilerek toplanmaz; TL toplamı yalnız TL, has kolonu ayrı toplanır | **KARŞILANDI** | P47_03 + M47-3; canlı `ozetTl` = 1.500 (has hariç) |
| TL=0 & has≠0 satırı boş sayılmaz, vurgulanır, "Has bakiyesi var" rozeti | **KARŞILANDI** | P47_02 + M47-2 + M47-4 |
| Tablo altında tek cümlelik not | **KARŞILANDI** | P47_01 (`#yas-has-notu`) |
| 375 px'te yatay kaydırma, başlık kırpılmaz | **KARŞILANDI (bUnit + CSS kanıtı; tarayıcıda göz ile ölçülmedi)** | `KartListe` → `.tablo-sar{overflow-x:auto}`, `tabindex=0`; `.veri-tablosu th`'de kırpma kuralı yok |
| Test listesi P47_01..03 | **KARŞILANDI** (+P47_04) | 4/4 yeşil |
| Mutasyon listesi (3 madde) | **KARŞILANDI** (+2 ek) | § 7 |
| dotnet test 0 başarısız, denetim.sh TEMİZ | **Paket kapısı: KARŞILANDI · Ağaç kapısı: orkestratörde** | P-47 yeni kırmızı 0, P-47 sayaçları ✓; ağaçta 19 kırmızı + 6 ✗ başka şeritlerin (§ 6, § 8) |
| UX-S28/29/31/32 | **KARŞILANDI** | Hata yolu değişmedi (boşa çevirme yok); sınır uyarısı; sözlük TR+EN; TL kolonları `Bicim.Tl`, has ayrı birimle |

## 12. Açık kalanlar
1. **P-57 (Bicim tr-TR):** `Bicim.Has` virgül ondalık + birim eki ("has" mı "g" mı) kararı → kabul koşulundaki `'-16,9460 g'` biçimi o paketle gelir. Testler `Bicim.Has` ile ürettiği için kırılmaz.
2. **Tam süitte başkasına ait 19 kırmızı:** Ayarlar.razor(513) NRE → 16 test (Ayarlar'ı 20:48'de düzenleyen şerit); `CariVeri` kilitsiz tip → 2 test (Cariler/Banka/Islemler/Faturalar'ı düzenleyen şerit); `S3aKabukHatasiTestleri` → P-31 (DALGA 7c).
3. **denetim.sh ✗ (6):** § 8'deki tablo — hiçbiri P-47'nin değil.
4. Mevcut kusur (kapsam dışı, not edildi): yaşlandırma listesi boşken kart hiç çizilmiyor (boş durum metni yok) ve yaşlandırma yükleme hatası genel `hata` bandına düşüyor. P-47 bu davranışı değiştirmedi; UX-S28 tamamlama için P-69 veya ayrı bir paket ele alabilir.
5. Tarayıcıda 375 px göz ile teyit: orkestratör/kurucu oturumu (demo 9. adım `/raporlar`).

## 13. Kurucuya / mali müşavire not
Mevzuat hükmü içermez. Görünür davranış: has bakiyesi artık yaşlandırmada ayrı kolon; TL karşılığı HESAPLANMAZ (bilinçli: has'ın TL değeri güne göre değişir ve TL alacağıyla toplanırsa yanlış sayı üretir).
