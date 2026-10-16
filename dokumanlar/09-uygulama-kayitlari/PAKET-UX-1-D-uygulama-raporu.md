# PAKET UX-1-D — "Her veri girilen yer düzeltilebilir" · Faz D (dalga 3+4 ekranları) · UYGULAMA RAPORU

> Tarih: 2026-09-18 · Dalga 4 (son paket) · Roller: Taşkın (backend) + Ertuğ (frontend)
> Durum: **YARIM** — kapı ölçütlerinin (build/test/denetim/canlı) tamamı geçti, ancak
> paketin iki ek koşulu (ekstre parti geri alma, 48-A Faz B'ye bağlı kalemler)
> **ÖNKOŞUL EKSİKLİĞİ** nedeniyle uygulanmadı ve "devredildi" satırı olarak yazıldı.
> Derleme **0 uyarı / 0 hata** · tam süit **2816/2816 yeşil** · `dotnet/denetim.sh` **TEMİZ**
> · migration **gerekmedi** · **COMMIT YAPILMADI**.

---

## 0. ÖNCE: ÖNKOŞUL DOĞRULAMASI (Anayasa md.1 — atlamadan)

Görev metnindeki önkoşullar koda karşı tek tek doğrulandı. **İkisi karşılanmıyor**
ve bu, paketin kapsamını doğrudan daralttı:

| # | Önkoşul | Durum | Kanıt |
|---|---|---|---|
| 1 | 48-A storno motoru (tek `IStornoMotoru`) canlıda çalışır | ✅ VAR | `Kuyumcu.Api/Modules/Satis/StornoServisi.cs` (462 satır), tek giriş |
| 2 | **48-A Faz B + S-3b: StornoServisi'ne hurda alış / gider pusulası / kasa hareketi / manuel kur türleri eklenmiş** | ❌ **YOK** | `StornoServisi.cs:71` beyaz liste hâlâ **5 tür**: `ISCILIK, VERESIYE, TAHSILAT, EMANET_GIRIS, EMANET_CIKIS`. `ErtelenenTurler` hâlâ `TAKAS, HURDA_ALIS, TRANSFER`. Kasa hareketi ve manuel kur türü listede **hiç yok** |
| 3 | UX-1 Faz A çıktıları hazır (`DuzeltmePolitikasi.cs`, `DegismezIpucu.razor`, `GerekceAlani.razor`) | ✅ VAR | dosyalar yerinde; bu pakette **yeniden yazılmadı**, yalnız yerleştirildi |
| 3b | `DuzeltAkisi.razor` | ⚠ **YOK** | `Kuyumcu.Web/Bilesenler/` altında böyle bir dosya yok. Yerine `Cekmece.razor` + `SatirIciDuzenle.razor` tek düzenleme kalıbı olarak yürürlükte (DoD EK md.2). Spesifikasyonun HEDEFİNE sadık kalındı, ad değişikliği rapora yazıldı |
| 4 | Dalga 3 (19, 25, 27, 34-A, 35-A, 47-A, T-2, T-4a) + dalga 4 (29-A, 41, 32, 40-A, 31) BİTMİŞ | ✅ VAR | `ORKESTRATOR-DURUM.md` 00:39 ve 02:16 kapıları; ilgili PAKET-*.md raporları BİTTİ |
| 5 | S-3a "tam gövde ile üzerine yazma yasağı" uygulanmış | ✅ VAR | `PAKET-S-3a-uygulama-raporu.md`; POS/vezne uçlarında canlı doğrulandı (§ 6) |
| 6 | S-2 record dondurması yürürlükte | ✅ VAR | `denetim.sh:310` `SAYFA_RECORD_TAVAN=129` |
| 7 | MFA kuralı: 45-API/45-UI kapanmadan finansal düzenleme uçları canlıya çıkmaz | ⚠ AÇIK | Bu pakette **yeni finansal düzenleme ucu açılmadı**; kural ihlal edilmedi |

### ⚠ SAYAÇ SAPMASI (görev metni ↔ gerçek kod)

Görev metni "record tavanı **122** ve ExecuteUpdate beyaz listesi **4** büyümemiş"
diyor. Gerçek değerler:

| Sayaç | Görev metni | Gerçek (denetim.sh) | Kim büyüttü |
|---|---|---|---|
| `SAYFA_RECORD_TAVAN` | 122 | **129** | dalga 4 paketleri (29-A/32/40-A/31), UX-1-D **ÖNCESİ** |
| ExecuteUpdate/Delete beyaz listesi | 4 | **5** | aynı |

**UX-1-D bu sayaçların İKİSİNİ DE büyütmedi:** bu pakette 0 yeni istemci record
tanımlandı ve 0 yeni `ExecuteUpdate/Delete` yazıldı. `denetim.sh` bu pakette
**değiştirilmedi** (dosya damgası 04:59, benim ilk dokunuşumdan önce). Sayaçların
122→129 ve 4→5 yükselişi orkestratöre bildirilir (Madde 1: gizlenmedi).

---

## 1. Spesifikasyonun koda karşı doğrulanması — ne DOĞRU ÇIKTI, ne DEĞİŞTİ

Görev, 11 rotada "düzenlenemeyen kalem = 0" hedefliyordu. Kodu tek tek okudum
(`grep`/`sed` ile, satır satır) ve **dalga 3+4 paketlerinin UX sözleşmesini büyük
ölçüde zaten uyguladığını** buldum. QA turu 4'ün (04:15) ölçtüğü boşlukların çoğu
o turdan SONRA gelen paketlerle kapanmış:

| QA turu 4 bulgusu (kapsamımdaki) | Bugünkü gerçek | Kanıt |
|---|---|---|
| `/cariler` cari pasife alınamıyor (gerileme) | **KAPALI** — `c-pasife-al` + onay modalı + `c-aktife-al-{id}` | `Cariler.razor:173-176, 413-428` |
| `/cariler` PEP beyanı düzenlenemez | **KAPALI** — düzenle çekmecesinde `duz-pep` + `duz-pep-gorev`, PATCH `/cari/{id}/pep` | `Cariler.razor:376-406`, `CariModulu.cs:1325` |
| `/urunler/agac` kategori pasife alınamıyor | **KAPALI** — kategori çekmecesinde `kAktif`; reyon için `r-pasife-al-{id}` + `r-aktife-al-{id}` | `UrunAgaci.razor:150-163, 320` |
| `/veri-ice-aktarim` parti geri alma yok | **KAPALI** — `ia-geri-al` düğmesi + onay | `VeriIceAktarim.razor:233` |
| `/kurulum` mühür sonrası açılış | **KAPALI (gerekçeli)** — `DegismezIpucu Varlik="AcilisKalemi"` ×2 + `kur-geri-al` | `Kurulum.razor:53, 307-308` |
| `/banka/ekstre` onaylanmış satır | **KAPALI (gerekçeli)** — satır içi `DegismezIpucu` + `ATLANDI` geri alma | `BankaEkstre.razor:183-199` |
| `/uyum` risk / kimlik tespiti kayıtları | **KAPALI (gerekçeli)** — `DegismezIpucu Varlik="CariRiskProfili"` ve `"MasakKimlikTespiti"` | `Uyum.razor:406, 543`, `Banka.razor:308` |
| `/pos` cihaz düzenleme + pasife alma | **KAPALI** — `pos-duzenle-{id}`, `pos-pasif-onayla`, `pos-aktif-{id}` | `Pos.razor:97-112, 272` |
| `/pos/tahsilatlar` alacak satırı | **KAPALI (gerekçeli)** — `DegismezIpucu Varlik="PosTahsilat"` + gerekçeli iptal | `PosTahsilatlar.razor:129, 193` |
| `/evraklar` evrak kartı | **KAPALI** — `e.Duzenlenebilir` koşullu düzenleme + tutar/cari için `DegismezIpucu` | `Evraklar.razor:145, 192` |

**Geriye iki GERÇEK boşluk kaldı** — ikisi de "sunucu kuralı doğru ama ekran
sessiz/yalancı" tipinde. UX-1-D'nin kod işi bu ikisidir.

---

## 2. Yapılan iş

### 2.1 `/vezne` — VEZNEDE fişinin kilidi artık GEREKÇELİ (QA turu 4 grup 1)

**Bulgu:** `Vezne.razor`'da "Düzenle" düğmesi yalnız `ACIK` fişte çiziliyordu;
`VEZNEDE` durumuna geçince **sessizce kayboluyordu**. Kullanıcı düğmeyi arıyor,
bulamıyor, neden kaybolduğunu öğrenemiyordu → QA'nın "düzenlenemeyen kalem"
sayacına giriyordu.

**Sunucu kuralı DOĞRU ve DEĞİŞTİRİLMEDİ** (`VezneKapisi.DuzenlemeDurumuDogrula`,
`VezneModulu.cs:159`): vezneye gönderilen fişin tutarı damgalıdır, kasada o para
bekletilir. Görev metni "bekleyen fişin tahsil bacakları tahsil edilmeden PATCH ile
düzeltilebilsin" diyordu; **bu, paket 19'un mimari kararını (R4/R5) tersine
çevirmek olurdu** ve iki ayrı düzenleme kapısı doğururdu. Spesifikasyonun HEDEFİ
("kullanıcı çıkmazda kalmasın") sadakatle korundu, aracı değiştirildi:
UX-S25 + UX-S30 gereği **devre dışı düğme çizilmedi**, satırın kendisine gerekçe
kutusu kondu ve GERÇEKTEN AÇIK olan yol (gerekçeli iptal → yeni fiş) yanında duruyor.

- `Kuyumcu.Web/Pages/Vezne.razor` — `VEZNEDE` satırına `<DegismezIpucu
  Varlik="VezneFisi-{id}">` eklendi (`ACIK` satırda **çizilmez**, çünkü orada yol açık).
- `Kuyumcu.Core/I18n/Sozlukler.cs` — 2 yeni anahtar, **TR + EN**:
  `Vezne.VezneleDegismez`, `Vezne.VezneleDegismezYolu`. Çıplak metin yok.

### 2.2 `/banka` — kalıcı devre dışı "Storno" düğmesi KALDIRILDI

**Bulgu (görev metninin "YANILTICI DÜĞME YASAK" maddesinin tam karşılığı):**
`Banka.razor` dekont satırında `disabled="@true"` yazan, **hiçbir koşulda
açılmayan** bir `bh-storno-{id}` düğmesi duruyordu. Kullanıcıya "bir yolu var ama
sana kapalı" diyordu; oysa banka dekontu için ters kayıt yolu **sunucuda hiç
yoktur** — `StornoServisi` yalnız `Hareketler` tablosunu çevirir, `banka_hareketleri`
onun beyaz listesinde değildir.

- Düğme kaldırıldı; yerine satır başına `<DegismezIpucu Varlik="BankaHareketi-{id}">`
  kondu (gerekçe + gerçek düzeltme yolu: ters yönde yeni dekont).
- Tablonun altındaki **tek toplu** ipucu kaldırıldı ve satır başına taşındı:
  kullanıcı düğmeyi aradığı YERDE gerekçeyi görür.
- Yeni sözlük anahtarı gerekmedi (`Banka.DekontDegismez` / `DekontDegismezYolu` zaten TR+EN).

### 2.3 Politika haritası — kapsam kanıtı

`Modules/Meta/DuzeltmePolitikasi.cs`'e **yeni satır eklenmesi gerekmedi**: dalga 3+4
paketleri 12 varlığın hepsini zaten damgalamış. Bunu varsaymak yerine **kanıtladım**:
`KuyumcuDbContext`'teki 62 DbSet ile haritadaki adlar `comm` ile karşılaştırıldı →
**eksik 0**. Görev metnindeki adlar ile koddaki gerçek adlar arasında şu eşleme var
(uydurma ad yazılmadı):

| Görev metnindeki ad | Koddaki gerçek varlık | Damga |
|---|---|---|
| VezneFisi | `VezneFisi` | DUZENLENEBILIR |
| EvrakKaydi | `KiymetliEvrak` | DUZENLENEBILIR |
| EvrakDurumHareketi | *(ayrı varlık YOK)* — `EVRAK_GIRIS/KARSILIKSIZ/CIRO` **hareket türü** olarak haritada | STORNO |
| PosCihazi | `PosCihaz` | DUZENLENEBILIR |
| PosAlacak | `PosTahsilat` | SALT_OKUNUR |
| EkstreIthalati | `EkstreIthalati` | SALT_OKUNUR |
| EkstreSatiri | `EkstreSatiri` | DUZENLENEBILIR |
| IceAktarimPartisi | `IceAktarimOturumu` | DUZENLENEBILIR |
| AcilisBakiyesi | `AcilisKalemi` | STORNO |
| PepBeyani | *(ayrı varlık YOK)* — `cariler` tablosunda 3 kolon; `Cari` üzerinden | DUZENLENEBILIR |
| MasakBankaKaydi | `MasakKimlikTespiti` | SALT_OKUNUR |
| ReyonYerlesimi | `StokYerlesimi` | STORNO |

### 2.4 Derleme uyarısı temizliği (kapı şartı)

Tam (önbelleksiz) derlemede **5 önceden var olan `xUnit2031` uyarısı** çıktı
(`EkstreEkranTestleri.cs` ×3, `EkstreIceAktarmaTestleri.cs` ×2 — paket 32 artığı).
DoD "0 uyarı" istediği için düzeltildi: `Assert.Single(x.Where(p))` →
`Assert.Single(x, p)`. Davranış aynı, iddia aynı.

### Migration

**Gerekmedi.** Bu pakette veri modeli değişmedi; yeni varlık/kolon/indeks yok.
`dotnet ef` çalıştırılmadı (prosedür gereği yalnız şema değişikliğinde koşulur).

---

## 3. DoD SÖZLEŞMESİ — hangi varlık hangi uçla düzeltilir, hangisi neden salt okunur

Bu pakette **yeni varlık/kayıt türü doğmadı**, dolayısıyla yeni PATCH/DELETE/yetki/
denetim yükümlülüğü doğmadı. Dokunulan 11 rotanın taşıdığı türler için sözleşme:

| Varlık | Damga | Düzeltme yolu (ekranda görünen) | Neden |
|---|---|---|---|
| `VezneFisi` (ACIK) | DUZENLENEBILIR | `PATCH /vezne/fisler/{id}` → `#vz-duzenle-{id}` çekmecesi | tutar henüz damgalanmadı |
| `VezneFisi` (VEZNEDE) | *(uç 409)* | **DegismezIpucu** + `#vz-iptal-{id}` gerekçeli iptal (≥ gerekçe kilidi) | kasada para bekliyor; damgalı tutar sessizce değişemez |
| `VezneFisi` (TAHSIL/IPTAL) | SALT_OKUNUR | DegismezIpucu (terminal) + satış iadesi | defter yazıldı |
| `PosCihaz` | DUZENLENEBILIR | `PATCH /pos/cihazlar/{id}` (`#pos-duzenle-{id}`) + pasife al/aktifleştir | cihaz kimliği bir defter satırı değil |
| `PosTahsilat` | SALT_OKUNUR | `POST /pos/tahsilatlar/{id}/iptal` (≥20 karakter gerekçe) | satıştan türetilir; **hard DELETE YOK** |
| `BankaHareket` (dekont) | STORNO (motor kapsamı dışı) | **DegismezIpucu** + ters yönde yeni dekont | `StornoServisi` yalnız `Hareketler`'i çevirir; olmayan uç vaat edilmez |
| `MasakKimlikTespiti` / `MasakBeyani` / `MasakSupheliBildirim` | SALT_OKUNUR | DegismezIpucu (yeni beyan / düzeltme bildirimi) | MASAK yükümlülük kaydı |
| `CariRiskProfili` | SALT_OKUNUR | `POST /masak/risk/{cariId}/elle` (YENİ satır doğurur) | append-only tarihçe |
| `Cari` (PEP beyanı dâhil) | DUZENLENEBILIR | `PATCH /cari/{id}` + `PATCH /cari/{id}/pep`; pasife al + aktife al | beyan hatası kilitlenmez |
| `KiymetliEvrak` | DUZENLENEBILIR (yalnız PORTFOYDE) | `PATCH /evraklar/{id}`; tutar/cari için DegismezIpucu | tutar deftere borç-kapama satırı yazdı |
| `EkstreSatiri` | DUZENLENEBILIR (onaydan ÖNCE) | `PATCH /bankalar/ekstre/satir/{id}` (cari bağla / atla / atlamayı geri al) | onaydan sonra banka hareketi doğdu |
| `EkstreIthalati` | SALT_OKUNUR | satır bazlı yol | başlık üretilen hareketlerin kaynağı |
| `IceAktarimOturumu` | DUZENLENEBILIR | `PATCH /ice-aktarim/{id}` + `#ia-iptal` + `#ia-geri-al` | prova aşaması |
| `IceAktarimSatiri` | SALT_OKUNUR | dosyayı düzelt → yeni prova | kanıt kaydı |
| `KurulumDurumu` | DUZENLENEBILIR (mühre kadar) | `PATCH /kurulum/durum` | mühürden sonra 409 |
| `AcilisKalemi` | STORNO | `POST /kurulum/acilis/geri-al`; mühürden sonra cari düzeltme fişi | açılış devri defter izi |
| `StokYerlesimi` | STORNO | `/reyonlar/tasima` · `/reyonlar/geri-al` | adet, hareketlerin sonucu |
| `Reyon` / `UrunKategorisi` | DUZENLENEBILIR | çekmece + pasife alma/aktifleştirme | kimlik kaydı |

**Finansal varlıkta hard DELETE ucu YAZILMADI** (yeni uç zaten yazılmadı);
pasife alma ve gerekçeli iptal yolları korundu.

---

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Önce (dalga 4 / paket 31 kapısı) | 2806 | 2806 | 0 | 0 |
| **Sonra (tam süit, `--logger trx`)** | **2816** | **2816** | **0** | **0** |

**Yeni test: 10** (+1 mevcut test güncellendi).

| # | Test | Tür | Ne kanıtlar |
|---|---|---|---|
| 1 | `VezneEkranTestleri.UX1D_VEZNEDE_fiste_DUZENLE_YOK_ama_GEREKCE_ve_ALTERNATIF_YOL_VAR` | bUnit | VEZNEDE satırında `#vz-duzenle-v1` yok, `#degismez-ipucu-VezneFisi-v1` var, içinde **0 düğme**, `#vz-iptal-v1` açık |
| 2 | `VezneEkranTestleri.UX1D_ACIK_fiste_DUZENLE_VAR_ve_gerekce_kutusu_CIZILMEZ` | bUnit | Yol açıkken "değiştirilemez" yazılmaz (ters yanlış) |
| 3 | `UX1DPolitikaTestleri.Dalga3_ve_4_varliklarinin_TAMAMI_haritada_ve_BEKLENEN_damgada` | saf | 13 varlık × sabit beklenen damga |
| 4 | `UX1DPolitikaTestleri.Dalga_varliklarinin_gerekce_anahtari_TANIMLI_kumeden_gelir` | saf | çıplak metin yok |
| 5 | `UX1DPolitikaTestleri.Defter_ve_MASAK_turleri_ASLA_DUZENLENEBILIR_damgalanamaz` | saf | **mevzuat kapısı** (VUK md.219 / MASAK) |
| 6 | `UX1DPolitikaTestleri.Dokunulan_sayfalarda_KALICI_devre_disi_dugme_KALMAZ` | saf (kaynak taraması) | 12 razor dosyasında `disabled="@true"` = 0 |
| 7 | `UX1DPolitikaTestleri.Dokunulan_sayfalarin_TAMAMI_DegismezIpucu_ya_da_duzenleme_yolu_tasir` | saf | "hiçbir şey yok" hâli bulgudur |
| 8 | `HttpPosTestleri.PosCihaz_PATCH_yalniz_bir_alan_gonderince_DIGERLERI_KORUNUR` | HTTP+DB | **kurul engel 4**: yalnız `ad` gönder → oran/valör/kasa/şube/aktif SQL'de aynı |
| 9 | `HttpPosTestleri.PosCihaz_pasife_alinir_ve_AYNI_uctan_geri_acilir` | HTTP+DB | yıkıcı eylemin geri dönüş yolu; satır SİLİNMEZ |
| 10 | `HttpPosTestleri.Yabanci_kiracinin_POS_cihazina_PATCH_404_doner` | HTTP+DB | **kiracı izolasyonu**: düzeltme yolu sızıntı açmaz |
| ~ | `UX1C2EkranTestleri.Banka_dekont_satirinda_STORNO_KILITLI_ve_GERCEK_gerekce_yazar` (**güncellendi**) | bUnit | artık "disabled düğme VAR" değil, "**düğme YOK**, satır ipucusu var" iddiası |

---

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/UX-1-D/` altına kopyalandı → `perl -0777 -pi -e`
ile tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı →
`cmp` ile birebir aynılık kanıtlandı.

| Kod | Dosya | Değişiklik (kuralı bozma) | KIRMIZIYA DÖNEN test(ler) | Geri alındı (`cmp`) |
|---|---|---|---|---|
| **M-UX1D-01** | `Modules/Pos/PosModulu.cs` | `if (dto.PesinKomisyonOran is { } oran)` koşulu kaldırıldı → gönderilmeyen oran `0`'a yazılıyor (tam-gövde regresyonu) | `HttpPosTestleri.PosCihaz_PATCH_yalniz_bir_alan_gonderince_DIGERLERI_KORUNUR` | ✅ AYNI |
| **M-UX1D-02** | `Pages/Vezne.razor` | `@if (fis.Durum == "VEZNEDE")` → `@if (false)` (gerekçe kutusu çizilmiyor = sessiz kilit) | `VezneEkranTestleri.UX1D_VEZNEDE_fiste_DUZENLE_YOK_ama_GEREKCE_ve_ALTERNATIF_YOL_VAR` | ✅ AYNI |
| **M-UX1D-03** | `Pages/Banka.razor` | Kalıcı `disabled="@true"` düğme geri kondu (yanıltıcı düğme) | `UX1DPolitikaTestleri.Dokunulan_sayfalarda_KALICI_devre_disi_dugme_KALMAZ` | ✅ AYNI |
| **M-UX1D-04** | `Modules/Meta/DuzeltmePolitikasi.cs` | `S("MasakKimlikTespiti", …)` → `D("MasakKimlikTespiti", "api/v1/masak/kimlik/{id}")` (MASAK kaydı düzenlenebilir sayıldı) | `Defter_ve_MASAK_turleri_ASLA_DUZENLENEBILIR_damgalanamaz` **ve** `Dalga3_ve_4_varliklarinin_TAMAMI_haritada_ve_BEKLENEN_damgada` (2 test) | ✅ AYNI |

Son mutasyondan sonra **TEMİZ derleme: 0 uyarı / 0 hata**. Test açığı bulunmadı
(her mutasyon en az bir testi kırmızıya çevirdi).

---

## 6. Canlı doğrulama — `localhost:5198`, test kiracısı `PAKETUX1D-TEST`

5199 portuna **DOKUNULMADI**. API `nohup` ile 5198'de kaldırıldı (PID 59560),
iş sonunda yalnız o PID `kill` ile durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN`
**boş (0 satır)**.

Kiracı, F-2-min kapısı gereği **davet jetonuyla** açıldı (geçici davet satırı
`uxd-davet-1`, iş sonunda silindi). Yanıt gövdeleri **okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` (davet jetonlu) | 201 | **201** | `tenantId=01M2S5MWDRNE5PNR6WG6A2SBGM` |
| 2 | `GET /meta/duzeltme-politikalari` jetonsuz | 401 | **401** | `{"message":"Token bulunamadı",...,"statusCode":401}` |
| 3 | `GET /meta/duzeltme-politikalari` jetonlu | 200 + 13 varlığın damgası | **200** · 62 varlık / 20 hareket türü | 13/13 damga beklenenle **birebir** (aşağıdaki tablo) |
| 4 | `POST /pos/cihazlar` (oran 0.0240, valör 3) | 200 | **200** | `id=01M2S5NJP0KMWKPJ25H4JA4HPQ` |
| 5 | `PATCH /pos/cihazlar/{id}` **yalnız `ad`** | 200 + diğer alanlar SQL'de AYNI | **200** | SQL öncesi: `UXD POS · 0.0240 · 3 · <kasa> · <şube> · 1` → sonrası: `UXD POS düzeltildi · 0.0240 · 3 · <kasa> · <şube> · 1` |
| 6 | `PATCH {aktif:false}` | 200, satır SİLİNMEZ | **200** | SQL: `aktif=0`, `satir_var=1` |
| 7 | `PATCH {aktif:true}` | 200, alanlar korunur | **200** | SQL: `aktif=1 · 0.0240 · 3` |
| 8 | `POST /vezne/fisler` (1 kalem, 5000 TL) | 200, `durum=ACIK` | **200** | `id=01M2S5QR3MS2JXWD6F6VD4FFG0`, `duzenlenebilir:true` |
| 9 | `PATCH /vezne/fisler/{id}` **yalnız `aciklama`** | 200 + kalemler KORUNUR | **200** | SQL: `aciklama='UX-1-D duzeltildi', durum=ACIK`; kalem sayımı **1**, toplam **5000.00** (gönderilmeyen `kalemler` silinmedi) |
| 10 | `POST /vezne/fisler/{id}/veznele` | 200 (ACIK→VEZNEDE) | **200** | — |
| 11 | `PATCH /vezne/fisler/{id}` VEZNEDE iken | **409** (ekranın DegismezIpucu iddiası doğru mu) | **409** | `"Yalnız ACIK durumdaki fiş düzenlenebilir (bu fişin durumu: VEZNEDE)…"` — **ekranda Düzenle çizmek YALAN olurdu** |
| 12 | `POST /vezne/fisler/{id}/iptal` gerekçeli | 200 (alternatif yol GERÇEKTEN açık) | **200** | SQL: `durum=IPTAL`, `iptal_gerekce='UX-1-D canli dogrulama: yanlis fis, ipta…'` |

**Adım 3 çıktısı (gövdeden okundu):**

```
VezneFisi DUZENLENEBILIR · StokYerlesimi STORNO · IceAktarimOturumu DUZENLENEBILIR
IceAktarimSatiri SALT_OKUNUR · KurulumDurumu DUZENLENEBILIR · AcilisKalemi STORNO
KiymetliEvrak DUZENLENEBILIR · MasakKimlikTespiti SALT_OKUNUR · EkstreIthalati SALT_OKUNUR
EkstreSatiri DUZENLENEBILIR · CariRiskProfili SALT_OKUNUR · PosCihaz DUZENLENEBILIR
PosTahsilat SALT_OKUNUR
```

### Temizlik

`information_schema`'dan `tenant_id` kolonu olan **58 tablo** tek tek silindi,
geçici davet satırı ve kiracı silindi. Artık satır taraması: **boş (0 satır)**.

```
select count(*) from tenants;  →  8  (başlangıçtaki değerin AYNISI)
select count(*) from tenants where ad like 'QA-TEST-%';  →  3  (DOKUNULMADI)
```

---

## 7. Denetim çıktısı

`dotnet/denetim.sh` → **DENETİM TEMİZ** (çıkış kodu 0). Kritik sayaçlar:

```
✓ ExecuteUpdate/Delete (beyaz liste 5)           5      ← UX-1-D büyütmedi
✓ Pages içinde istemci record tanımı           129 (tavan 129)  ← UX-1-D büyütmedi (0 yeni record)
✓ Kültürsüz biçimli ToString (Web)              44 (tavan 44)
✓ Kaynak ağacı: beyaz liste dışı .mutorj         0
✓ Pages: doğrulama mesajsız EditForm yok         0 / 0
DENETİM TEMİZ.
```

---

## 8. Değişen dosyalar

```
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs          (+2 anahtar × TR+EN = 6 satır)
dotnet/Kuyumcu.Web/Pages/Vezne.razor           (VEZNEDE satırına DegismezIpucu)
dotnet/Kuyumcu.Web/Pages/Banka.razor           (kalıcı disabled Storno → satır içi DegismezIpucu)
dotnet/Kuyumcu.Tests/UX1DPolitikaTestleri.cs   (YENİ — 5 saf test)
dotnet/Kuyumcu.Tests/VezneEkranTestleri.cs     (+2 bUnit test)
dotnet/Kuyumcu.Tests/HttpPosTestleri.cs        (+3 HTTP/DB test)
dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs     (1 test güncellendi — yeni sözleşme)
dotnet/Kuyumcu.Tests/EkstreEkranTestleri.cs    (xUnit2031 uyarı temizliği ×3)
dotnet/Kuyumcu.Tests/EkstreIceAktarmaTestleri.cs (xUnit2031 uyarı temizliği ×2)
```

`MIMARI-KALAN-PAKETLER-2026-09-15.md`, paket spesifikasyonu, `SONRAKI-OTURUM-PROMPT.md`
ve `denetim.sh` **DEĞİŞTİRİLMEDİ**. **COMMIT YAPILMADI.**

---

## 9. DEVREDİLDİ (kod yazılmadı — talimat 13: kapsam büyütülmedi)

| # | Kalem | Neden devredildi | Nereye |
|---|---|---|---|
| D-1 | **`/banka/ekstre` toplu onayın parti bazlı geri alınması** | Ekstre onayı `banka_hareketleri` üretir; `StornoServisi` yalnız `Hareketler`'i çevirir. Parti geri alma için ya (a) `StornoServisi`'ne banka hareketi türü eklenmeli (= **48-A Faz B**, önkoşul 2 karşılanmadı) ya da (b) **ikinci bir geri-alma motoru** yazılmalı — Bertan'ın mimari vetosu ve görev metnindeki riskin kendisi. İkisi de UX-1-D'nin yetkisi dışında | 48-A Faz B |
| D-2 | **`/veri-ice-aktarim` ile ekstrenin AYNI parti geri alma yolunu paylaşması** | D-1'e bağlı; içe aktarımın kendi `ia-geri-al` yolu zaten var, ekstrenin yolu yok. Tek yol ancak D-1 kapanınca kurulabilir | 48-A Faz B |
| D-3 | **`/kurulum` altı adımın mühürden önce aynı formdan yeniden düzenlenmesi** | `PATCH /kurulum/durum` + `kur-geri-al` + `AcilisKalemi` DegismezIpucu zaten var; "aynı formdan yeniden düzenleme" (UX-S26 tam karşılığı) `Kurulum.razor`'un altı adım gövdesini yeniden yazmayı gerektiriyor — bu, T-2'nin sayfasına **ikinci dokunuş** (DoD EK 2: gerekçesiz ikinci dokunuş = engel) ve kapsam büyütmesi | S-3d / T-2 Faz B |
| D-4 | **Hurda alış / gider pusulası / kasa hareketi / manuel kur storno bağlantısı** | `StornoServisi` beyaz listesinde bu türler yok (önkoşul 2). "Sunucuda uç var, ekranda düğme yok"un TERSİ de yasak: olmayan uca düğme çizilmedi | 48-A Faz B + S-3b |
| D-5 | **Evrak ciro zinciri / POS mutabakat düzeltmesi** | Yeni ekran/modül açardı (talimat 13 ihlali) | 29-A Faz B / 31 Faz C |
| D-6 | **`/destek` (T-4a) rotası** | Kapsam listesinde vardı; `Kuyumcu.Web/Pages/` altında **`Destek.razor` YOK** — T-4 sürüm damgası/hata kimliği olarak uygulanmış, ayrı destek sayfası doğmamış. Olmayan sayfa için test/kod yazılmadı | T-4b |

---

## 10. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Kapsam sayarak kilitlendi (11 rota), yeni ekran/modül AÇILMADI | ✅ | 0 yeni razor, 0 yeni uç, 0 yeni varlık |
| Politika haritası 12 varlığı kapsıyor, ekran `/meta/duzeltme-politikalari`'ndan okuyor | ✅ | § 2.3 + canlı adım 3; razor'da sabit politika yok |
| "Haritada olmayan yeni varlık = KIRMIZI" testi genişletildi | ✅ | 3 yeni saf test (§ 4 #3-5) |
| `/vezne` + `/pos/tahsilatlar` düzeltme yolu | ⚠ KISMEN | vezne: ACIK PATCH ✅ (canlı #9), VEZNEDE gerekçeli kilit ✅ (canlı #11-12); POS alacak gerekçeli iptal ✅ (mevcut). **Tahsil bacağının PATCH'i açılmadı** — gerekçe § 2.1 |
| `/pos` cihaz PATCH + pasife alma | ✅ | canlı #4-7 + 3 test |
| `/evraklar` (29-A) düzenleme + takvim salt görüntü | ✅ | 29-A ile geldi, doğrulandı (§ 1) |
| `/banka/ekstre` toplu onay geri alınabilir | ❌ | **D-1** — önkoşul 2 karşılanmadı |
| MASAK/uyum SALT_OKUNUR + DegismezIpucu, devre dışı düğme YOK | ✅ | `Uyum.razor:406,543` · `Banka.razor:308` · test #6 |
| Cariler PEP beyanı DUZENLENEBILIR, geri alınabilir | ✅ | `PATCH /cari/{id}/pep` (`CariModulu.cs:1325`) |
| `/kurulum` + açılış geri alma | ⚠ KISMEN | geri-al + DegismezIpucu var; adım yeniden düzenleme **D-3** |
| PATCH semantiği korundu (tam gövde yazma YOK) | ✅ | canlı #5 ve #9 SQL kanıtı + test #8 + **M-UX1D-01** |
| YENİ istemci record TANIMLANMADI (tavan büyümedi) | ✅ | denetim.sh 129/129 |
| Yanıltıcı düğme yok (kalıcı disabled / boş Kaydet) | ✅ | § 2.2 + test #6 + **M-UX1D-03** |
| Kiracı izolasyonu: yabancı kiracıya PATCH 404, DB testiyle | ✅ | test #10 |
| Hard DELETE üretilmedi; iptal = pasife alma / storno | ✅ | 0 yeni DELETE ucu |
| Build 0 uyarı 0 hata | ✅ | § 2.4 |
| `denetim.sh` TEMİZ | ✅ | § 7 |
| Tam süit 0 başarısız | ✅ | 2816/2816 |
| Her iş kuralı için mutasyon kanıtı | ✅ | 4 mutasyon, hepsi kırmızı, hepsi `cmp` ile geri alındı |
| Canlı doğrulama + test kiracısı silindi, `QA-TEST-*` korundu | ✅ | § 6 |
| **Kapı ölçütü: QA turu 4'te bu 11 rotada düzenlenemeyen kalem = 0** | ⚠ ÖLÇÜLMEDİ | QA turu 5 koşmadan iddia edilemez (Madde 1). Bu paket bilinen kalemlerin 2'sini kapattı, 6'sını devretti |

---

## 11. Açık kalanlar

1. **D-1/D-2/D-4 — 48-A Faz B zinciri.** `StornoServisi` beyaz listesi 5 türde
   duruyor. Kurul engel 1 ("25 veri girişi düzeltilemiyor") bu paketle KAPANMADI;
   UX-1-D yalnız ekran tarafını dürüstleştirdi.
2. **D-3 — `/kurulum` UX-S26.** Oluşturma=düzenleme kuralı kurulum sihirbazında
   tam karşılanmıyor.
3. **D-6 — `/destek` rotası yok.** Kapsam listesindeki 11 rotadan biri gerçekte
   doğmamış; orkestratörün kapsam listesi düzeltilmeli.
4. **`DuzeltAkisi.razor` bileşeni yok.** Görev metni onu "UX-1 Faz A çıktısı"
   sayıyor; gerçekte `Cekmece` + `SatirIciDuzenle` kalıbı var. Belge düzeltilmeli.
5. **Sayaç sapması** (record 122→129, ExecuteUpdate 4→5) — § 0.
6. **Kaynak ağacında artıklar:** `dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs.bak`
   ve 16 `.mutorj` dosyası (hepsi bu paketten ÖNCE vardı, `denetim.sh` beyaz
   listesinde). Silme işi Kurucu'ya bırakıldı (hook `rm`'i blokluyor).
7. **QA turu 5 gerekli:** "düzenlenemeyen kalem = 0" kapısı ancak canlı ölçümle
   kapanır.

## 12. Kurucuya / mali müşavire notlar

- **Mevzuat kilidi bilinçlidir ve testle korunuyor.** MASAK kimlik tespiti, şüpheli
  bildirim, risk profili, açılış devri, denetim izi ve defter hareketleri
  **DUZENLENEBILIR damgalanamaz**; biri denerse `Defter_ve_MASAK_turleri_ASLA_
  DUZENLENEBILIR_damgalanamaz` testi kırmızı verir. Gerekçe: VUK md.219 (defter
  kayıtlarının silinmez/değiştirilmez olması) ve MASAK yükümlülük kaydı saklama
  zorunluluğu. **Bu iki dayanak mali müşavir onayı ister** — kodda hüküm sabit
  yazılmadı, yalnız "değiştirilemez" davranışı korundu.
- **Banka dekontu için storno yolu YOKTUR** ve ekran artık bunu açıkça söylüyor.
  Yanlış dekontun düzeltilmesi ters yönde ikinci bir dekontla yapılır; muhasebe
  açısından bunun kabul edilebilirliği mali müşavire sorulmalıdır.
- **Ürün hâlâ ÜCRETSİZ PİLOT konumundadır** (Mühür 2). Bu raporda fiyat, teklif,
  tasarruf yüzdesi veya tahsilat içeren hiçbir metin üretilmedi.
