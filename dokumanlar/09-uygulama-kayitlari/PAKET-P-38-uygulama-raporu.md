# PAKET P-38 — "SAHİPSİZ KALAN 6 BULGU" UYGULAMA RAPORU

- **Tarih:** 2026-09-22 · **Dalga:** 6 · **Sürüm:** 1.0
- **Kaynak:** `TRIYAJ-TUR7-2026-09-21.md` § 6 (6 sahipsiz bulgu) + § 6.2 (B-39 çelişkisi)
- **Roller:** Taşkın (backend) + Ertuğ (frontend)
- **Durum:** BİTTİ — derleme 0 hata / 0 uyarı · tam süit **3754 geçti, 0 başarısız** ·
  `dotnet/denetim.sh` **DENETİM TEMİZ** · canlı doğrulama 5198'de yapıldı · test kiracısı silindi.

---

## 1. Kimlik kapanış tablosu

| # | Rota | Kimlik | Ne yapıldı | Kanıt |
|---|---|---|---|---|
| 112 | `/vitrin` | **B-18** | Sunucu marj kapısı: canlı kotasyon biliniyorken fiyatı ≤ 0 yapan marj **400**; hesap tarafı ₺0,00 basmak yerine satırı `MARJ_GECERSIZ` ile atlar | canlı 400 + 5 test + M38-1/M38-2 |
| 163 | `/emanet` | **B-70** | **ÜRETİLEMEDİ** — sunucu açıklamayı yazıyor ve geri veriyor (aşağıda § 4). 2 regresyon testiyle kilitlendi | SQL kanıtı + 2 test |
| 67 | `/urunler` | **B-66** | "Yeni satışta seçilemez" sözü SUNUCUDA zaten tutuluyordu (`UrunBul … && u.Aktif`); söz artık ekranda da tutuluyor | canlı 404 + 2 test |
| 107 | `/islemler` | **B-66** | Ürün seçim listesi `aktif=true` süzgeciyle çekiliyor (8 seçim listesi düzeltildi) | canlı liste + bUnit + M38-3 |
| 159 | `/faturalar` | **B-25** | Fiş seçicisi artık **gözatılabilir**: sorgusuz çağrıda sunucu en son fişleri döner (`gozatma: true`) | canlı 200 + 2 test + M38-4 |
| 229 | `/banka/ekstre/{id}` | **B-18** | Cari eşleşmesi olmayan satır varsayılan olarak **onaylanmaz**; BEKLİYOR'da kalır (düzeltilebilir), rıza `carisizOnay` ile açıkça verilir, sonuç sayıları yanıtta | canlı 2 tur + 2 test + M38-5 |
| 48 | `/cariler` | **B-39** | İleri ve geri eylem **aynı yerde ve simetrik**: aktif satırda "Pasife Al", pasif satırda "Aktife al". Onay modalı + bağlı kayıt uyarısı korundu | 2 bUnit testi + M38-6 |

---

## 2. Yapılanlar (kod)

### 2.1 B-18 · pano marjı (`/vitrin`)
- `PanoFiyatServisi.Dogrula(ayar, kotasyon)` — ikinci parametre **kalemin canlı kotasyonu**.
  Kotasyon biliniyorsa: `kotasyon.Alis + AlisMarjTl > 0`, `kotasyon.Satis + SatisMarjTl > 0`
  ve kayıt anında **ters makas reddi**. Kotasyon yoksa yalnız mutlak tavan (1.000.000) uygulanır.
- `PanoFiyatServisi.Uygula` artık negatif fiyatı **0'a çekmiyor**, `null` döndürüyor;
  `Hesapla` bu satırı yeni `AtlamaSebebi.MarjGecersiz` sebebiyle atlıyor. Aşağı yuvarlamanın
  fiyatı 0'a indirdiği durum da aynı kapıdan geçiyor.
- `PanoAyarServisi.Guncelle` kaydetmeden önce kotasyonu okuyup kapıya veriyor (yeni özel
  `Kotasyon(tenantId, kod)`; hesap YENİDEN YAZILMADI).
- `Vitrin.razor` yeni sebebi sözlükten çeviriyor (UX-S31); `Pano.SebepMarjGecersiz` TR+EN eklendi.

**Neden "makul aralık" diye bir sayı UYDURULMADI (Madde 1):** −999.999 TL zaten mutlak tavanın
(1.000.000) *altındaydı*; tavanı elle küçültmek ikinci bir uydurma eşik olurdu. Kuralın gerçek
dayanağı ölçülebilir: **panoya basılacak fiyat pozitif olmak zorundadır.** Kotasyon bilinmediği
anda (kaynak henüz çekmedi) yalnız eski mutlak tavan geçerlidir; bu boşluk gizlenmiyor,
`Dogrula_kotasyon_bilinmiyorsa_yalniz_MUTLAK_TAVANI_uygular` testiyle **belgeleniyor**.

### 2.2 B-66 · pasif kayıt seçilemez
- `Islemler.razor` ve `Faturalar.razor` ürün listesi → `?aktif=true&limit=200`.
- Cari seçim listeleri de aynı kurala alındı: `Banka`, `Urunler` (konsinye), `Faturalar`,
  `Evraklar`, `BankaEkstre`, `Emanet` → `aktif=true`.
- Yönetim ekranları (`Cariler`, `Urunler`) süzgeci kullanıcıdan alır; onlara DOKUNULMADI.
- Sunucu tarafı zaten fail-closed'dı (`SatisModulu.UrunBul … && u.Aktif` → 404); bu
  **regresyon testiyle kilitlendi**, yeniden yazılmadı.

### 2.3 B-25 · fiş seçicisi
- `AramaTurleri.Gozatilabilir = [FIS]` + `Gozatilir(tur)` (tek kaynak, sözleşme dosyasında).
- `AramaServisi.Ara`: gözatılabilir türde sorgu asgarinin altındaysa **en son kayıtlar** listelenir
  (kısa sorgu süzgeç sayılmaz — tek harf ULID'de gürültüdür); yanıta `gozatma` alanı eklendi.
- `KayitSecici.razor`: gözatılabilir türde ilk açılışta sorgusuz liste çeker, durum satırında
  "En son kayıtlar listeleniyor — daraltmak için yazmaya başlayın" yazar (`Kabuk.AramaGozatma`, TR+EN).
- Diğer türlerin sözleşmesi DEĞİŞMEDİ (`Sorgusuz_CARI_aramasi_hala_BOS_doner…` testi bunu bekler).

### 2.4 B-18 · carisiz ekstre satırı
- `EkstreOnaylaDto.CarisizOnay` (varsayılan **false**). Cari eşleşmesi olmayan satır:
  - **HATALI işaretlenmez** (hatalı satır bir daha onaylanamaz; buradaki eksik düzeltilebilir bir eksiktir),
  - BEKLİYOR'da kalır → `PATCH /bankalar/ekstre/satir/{id}` ile cari bağlanıp onaylanabilir,
  - sebebi satır bazında yanıtta yazar.
- Yanıt ve denetim izi `carisizAtlanan` / `carisizUretilen` sayılarını taşır (UX-S27).
- `BankaEkstre.razor`: seçimde carisiz satır varsa uyarı + "Carisiz onayla" kutusu (UX-S30),
  sonuç bandına iki sayı da yazılır.

### 2.5 B-39 · cari pasife alma (çelişkinin çözümü)
- `PasifOnayAc(CariVeri)` çekmeceden bağımsızlaştı; `pasifAday` alanı eklendi.
- Liste satırı: aktif → `c-pasife-al-{id}`, pasif → `c-aktife-al-{id}` (simetri).
- Çekmecedeki `#c-pasife-al` düğmesi KORUNDU, aynı modalı açar. Modal başlığı artık cari adını yazar.
- Listeden pasife alma açık bir çekmeceyi yalnız aynı cariyse kapatır.

**DERS (rapora yazılması istendi):** *bir paket "ekran yolu zaten vardı" diyerek kapanış ilan
ederse, o yolun KULLANICI TARAFINDAN BULUNABİLİR olduğu da kanıtlanmalıdır.* P-13 raporu B-39'u
"düğme zaten vardı (UX-4d md.13)" diyerek kapattı; düğme gerçekten vardı ama **yalnız Detay
çekmecesinin içinde** — geri alma bir tık, asıl işlem üç tıktı. QA'nın "listeden pasife alınamıyor"
teşhisi doğruydu. Bundan sonra kapanış kanıtı "kod var" değil, **"düğme kullanıcının baktığı yerde
ve tıklanınca şu isteği atıyor"** olmalıdır (bu paketteki `Listeden_pasife_alma_ONAY_ister_ve_DELETE_atar`
testi tam olarak bunu ölçer).

---

## 3. DoD sözleşmesi (Bertan koşulu)

Bu pakette **yeni varlık/kayıt türü açılmadı** — dolayısıyla yeni PATCH/DELETE/yetki/denetim
yüzeyi doğmadı. Dokunulan kayıtların düzeltilebilirliği:

| Kayıt | Düzenleme yolu | Yıkıcı eylem | Not |
|---|---|---|---|
| Pano kalem ayarı (`kiraci_ayarlari`) | `PATCH /pano/ayarlar/{kod}` (mevcut) | `POST …/varsayilana-don` | Finansal değil; kapı MUDUR+; denetim izi mevcut servisle yazılıyor |
| Ekstre satırı | `PATCH /bankalar/ekstre/satir/{id}` (mevcut) | yok | **P-38 ile düzeltilebilirlik KORUNDU**: carisiz satır artık HATALI'ya düşmüyor, BEKLİYOR'da kalıyor |
| Banka hareketi | `POST /bankalar/hareket/{id}/eslestir` (mevcut) | hard DELETE **YOK** | Finansal varlık; yalnız eşleştirme/düzeltme |
| Cari | `PATCH /cari/{id}` · `DELETE /cari/{id}` = pasife alma | pasife al ↔ aktife al | P-38 ile **ikisi de liste satırında** |
| Ürün | `PATCH /urunler/{id}` · `DELETE` = pasife alma | pasife al ↔ `{aktif:true}` | Söz ("yeni satışta seçilemez") artık gerçek |
| Emanet hareketi | açıklama satır içi PATCH (mevcut) | `DegismezIpucu` ile gerekçelendirilmiş | Deftere yazılmış hareket değişmez; açıklama serbest |

Yeni sözlük anahtarları TR+EN olarak eklendi (çıplak metin yok):
`Pano.SebepMarjGecersiz`, `Kabuk.AramaGozatma`, `Banka.CarisizSatirUyari`,
`Banka.CarisizOnayla`, `Banka.CarisizOnaylaIpucu`, `Banka.CarisizUretilen`, `Banka.CarisizAtlanan`.

---

## 4. Sapmalar ve gerekçeleri (Anayasa md.1)

### 4.1 B-70 (#163) — "emanet açıklaması sessizce yutuluyor" **ÜRETİLEMEDİ**
Triyaj bunu KRİTİK ve "sunucu sözleşmesi eksik" diye tanımlıyordu. **Ölçüldü, doğru değil:**

- Sunucu: `EmanetGirisDto.Aciklama` **var** (`EmanetModulu.cs:41`) ve deftere yazılıyor
  (`Aciklama = dto.Aciklama`, `EmanetModulu.cs:130`).
- İstemci: `Emanet.razor:543` `aciklama` alanını gövdeye **koyuyor**.
- Canlı kanıt (5198, `PAKET38-TEST` kiracısı):
  `POST /api/v1/emanet/giris … "aciklama":"P38 canli dogrulama aciklamasi"` → **200**
  SQL: `select tur, aciklama from hareketler …` → `EMANET_GIRIS  P38 canli dogrulama aciklamasi`
  `GET /api/v1/emanet/{cariId}` → `"aciklama":"P38 canli dogrulama aciklamasi"`

**Karar:** alan EKLENMEDİ (eklenecek bir şey yoktu); bulgu **regresyon testiyle kilitlendi**
(`Emanet_girisindeki_ACIKLAMA_deftere_yazilir`, `Emanet_hareket_listesi_ACIKLAMAYI_geri_verir`).
En olası açıklama: QA turu 7 **dalga 4 anlık görüntüsünü** ölçtü (triyaj § 8/3 aynı zaman-sırası
sorununu B-2 için de yazıyor); dalga 5 sonrası kod bu davranışı taşımıyor. **Bu bir tahmindir ve
doğrulanmamıştır** — kesin olan, bugünkü kodun bulguyu üretmediğidir. Tur 8 bu kimliği
yeniden ölçmelidir.

### 4.2 Mevcut bir testin sözleşmesi değişti
`PanoFiyatTestleri.AsiriNegatifMarjFiyatiSifirinAltinaDusuremez` **bulgunun kendisini
mühürlüyordu**: "aşırı negatif marjda alış 0 olur" diye geçiyordu — müşteriye dönük panodaki
"₺0,00" tam olarak buydu. Test yeni sözleşmeye çevrildi (satır atlanır, sebep `MARJ_GECERSIZ`)
ve gerekçesi testin başına yazıldı. Gizlenmedi, silinmedi.

### 4.3 Mevcut bir test yardımcısı açık niyet taşıyacak şekilde güncellendi
`EkstreIceAktarmaTestleri.Onayla(...)` artık `carisizOnay: true` gönderiyor. O dosyanın
senaryoları (mükerrer, MASAK, tek yazma yolu) carinin varlığıyla ilgili değil ve **eskiden de
carisiz onaylıyordu**; davranış değişmedi, niyet açık yazıldı. Yeni kuralın kendisi
`P38HttpTestleri` içinde ayrıca sınanıyor.

### 4.4 Mekanizma kuralının ikinci paragrafı — **DEVREDİLDİ**
Görev metni iki ayrı sayaç seti istedi. İkinci set
(`HAREKETLERDE_ODEME_YONTEMI_YENI_YOL=0`, `FIS_TOPLAM_ODEME_KALEMI_FARKI=0`) bu paketin
kapsam kilidinin dışında (ödeme/fiş toplamı konusu; P-38 dört işle sınırlı) ve tabanı
**taze ölçüldüğünde 0 değil**: `OdemeTuru =` ataması Api içinde 20+ yerde geçiyor
(`Evrak`, `Gider`, `Virman`, `Satis`, `Storno`, `Cari`, `Kurulum`…). Bunların çoğu meşru;
"yeni yol" tanımı bu paketin mühürleyemeyeceği bir muhasebe kararıdır. Ayrıca asıl değişmez
(**deftere yazan tek yol `HareketFabrikasi.Ekle`**) `denetim.sh`'ta zaten cırcırlı:
`denetle "Hareketler.Add (yalnız HareketFabrikasi)" … 1` (satır 70) ve POS/banka karşılıkları.
Uydurma bir tanımla sahte bir 0 üretmektense **devredildi** yazıldı (Madde 1).

---

## 5. Migration

**GEREKMEDİ.** Şema değişikliği yok: `EkstreOnaylaDto.CarisizOnay` istek gövdesi alanıdır,
kolon değildir; `hareketler.aciklama` kolonu ZATEN vardı (ölçüldü — canlı SQL yukarıda).
`__EFMigrationsHistory` sayısı **61** (işlem öncesi = sonrası; yeni migration üretilmedi).

---

## 6. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Filtreli `~P38` | 20 | 20 | 0 | 0 |
| **Tam süit** (`trx-pP-38`) | **3754** | **3754** | **0** | 0 |

**Yeni test sayısı: 20** (`Kuyumcu.Tests/P38SahipsizBulgularTestleri.cs`)
— 6 saf (`P38PanoSafTestleri`), 11 HTTP+MySQL (`P38HttpTestleri`), 3 ekran/bUnit (`P38EkranTestleri`).
Ayrıca 1 mevcut test yeni sözleşmeye çevrildi (§ 4.2).

Derleme: `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata**.

---

## 7. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M38-1** | `PanoFiyatServisi`: `ya <= 0m` → `ya <= -9_000_000m` (sıfır tabanı kapatıldı) | `Dogrula_kotasyon_biliniyorken_fiyati_sifira_indiren_marji_REDDEDER` · `Pano_marji_fiyati_sifira_indirirse_400_doner` | `cmp` aynı |
| **M38-2** | `Uygula`: `return null` → `return 0m` (eski ₺0,00 davranışı) | `Marj_fiyati_sifira_indirirse_satir_VITRINE_CIKMAZ` | `cmp` aynı |
| **M38-3** | `Islemler.razor`: `?aktif=true&limit=200` → `?limit=200` | `Islemler_urun_listesini_YALNIZ_aktif_suzgeciyle_ceker` | `cmp` aynı |
| **M38-4** | `AramaModulu`: `&& AramaTurleri.Gozatilir(istenenTur)` → `&& false` | `Fis_aramasi_SORGUSUZ_son_fisleri_listeler` | `cmp` aynı |
| **M38-5** | `EkstreModulu`: `&& !dto.CarisizOnay` → `&& false` (kapı kapatıldı) | `Cari_eslesmeyen_ekstre_satiri_SESSIZCE_onaylanmaz` | `cmp` aynı |
| **M38-6** | `Cariler.razor`: satır düğmesinin kimliği `c-pasife-al-{id}` → `c-pasife-yok-{id}` | `Cari_listesinde_AKTIF_satirda_PASIFE_AL_dugmesi_vardir` · `Listeden_pasife_alma_ONAY_ister_ve_DELETE_atar` | `cmp` aynı |

Her mutasyondan sonra dosya yedekten geri kopyalandı ve `cmp` ile **birebir aynı** olduğu
kanıtlandı. Son mutasyondan sonra temiz derleme: **0 Uyarı, 0 Hata**.
Test açığı bulunmadı (her mutasyon en az bir testi ADIYLA kırdı).

---

## 8. Denetim (`dotnet/denetim.sh`)

Çıktı: **`DENETİM TEMİZ.`**

Eklenen/güncellenen sayaçlar (ölçüm komutları betiğe yorum olarak yazıldı, tabanlar TAZE ölçüldü):

| Sayaç | Değer | Ölçüm |
|---|---|---|
| `P-00: envanter kimlik sayısı` | **174** (taban 172 → **174**) | `awk '/^## 2\./,/^## 3\./' KAPSAM-ENVANTERI.md \| grep -cE '^\| [BYRDK]-[0-9]+ \|'` |
| `P-38: PANO_MARJ_SINIRSIZ` | **0** | kotasyonlu `Dogrula` imzası + `MarjGecersiz` sebebi + `return 0m` yokluğu + `Guncelle`'nin kotasyon geçirmesi (4 şart) |
| `P-38: OLUSTURMADA_YUTULAN_ALAN` | **0** | her `*GuncelleDto`/`*PatchDto` alan kümesi − eşleşen `*OlusturDto`/`*GirisDto` (PATCH bayrakları ve `Aktif`/`IptalGerekce` hariç) |
| `P-38: PASIF_KAYIT_SECILEBILIR` | **0** | `grep -rnE '/(cari\|urunler)\?' Pages/ \| grep -v 'aktif=' \| grep -v 'string.Join'` |

Yan bulgu (kendi kodumda yakalandı): `P-18: SABIT_RENK` sayacı yorumdaki `#229` ifadesini
hex renk sanıp kırdı; yorum "bulgu 229" olarak yazıldı. Sayaç tavanı ARTIRILMADI.

---

## 9. Canlı doğrulama (localhost:5198, kiracı `PAKET38-TEST`)

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| `PATCH /pano/ayarlar/GRAM {alisMarjTl:-999999}` | 400 | **400** | `"Alış marjı fiyatı sıfıra ya da altına indiriyor: kotasyon 6708.58 + marj -999999 = -993290.42…"` |
| `PATCH /pano/ayarlar/GRAM {alisMarjTl:-50}` | 200 | **200** | `/pano/guncel` → GRAM alış **6650** (₺0,00 YOK), `atlananlar: []` |
| `POST /emanet/giris` + `aciklama` | deftere yazılır | **yazıldı** | SQL: `EMANET_GIRIS \| P38 canli dogrulama aciklamasi` |
| `GET /emanet/{cari}` | açıklama döner | **döndü** | `"aciklama":"P38 canli dogrulama aciklamasi"` |
| `DELETE /urunler/{id}` → `GET /urunler?aktif=true` | pasif ürün YOK | **0 eşleşme** | süzgeçsiz listede **1 eşleşme** (kayıp değil, seçilemez) |
| `POST /islemler/satis` pasif ürünle | 404 | **404** | `"Ürün bulunamadı veya aktif değil"` |
| `GET /arama?tur=FIS&limit=10` (sorgusuz) | son fişler | **1 sonuç**, `gozatma:true` | `"P38 Test Musteri · 21.09.2026 / 1 kalem · 1000.00 TRY"` |
| `GET /arama?tur=CARI&limit=10` (sorgusuz) | boş, `gozatma:false` | **sayi 0, gozatma false** | sözleşme değişmedi |
| Ekstre onay (carisiz, rızasız) | 0 başarılı | **basarili 0 · carisizAtlanan 1 · uretilenHareketSayisi 0** | satır HATALI değil |
| Satıra cari bağla → onayla | 1 başarılı | **basarili 1 · uretilenHareketSayisi 1** | düzeltme yolu çalışıyor |

**SQL kanıtı ve temizlik:**
`tenants` başlangıç **9** → test kiracısı açıldı (10) → `tenant_id` kolonu olan **62 tablodan**
silindi + `tenants` satırı silindi → **9**. `kiraci_davetleri` 6 → 5 (kendi ürettiğim davet silindi).
`QA-TEST-` önekli kalıcı QA fikstürlerine **dokunulmadı**. API PID'i durduruldu:
`lsof -nP -iTCP:5198 -sTCP:LISTEN` → boş. **5199'a dokunulmadı.**

---

## 10. Dokunulan sayfalar (tek dokunuş kuralı, DoD EK 2)

`Vitrin.razor` · `Cariler.razor` · `Islemler.razor` · `Faturalar.razor` · `Banka.razor` ·
`BankaEkstre.razor` · `Emanet.razor` · `Evraklar.razor` · `Urunler.razor` ·
`Bilesenler/KayitSecici.razor`

> `Cariler` ve `Islemler` bu dalgada başka paketlerce de açılmış olabilir; P-38'in dokunuşu
> **kapı kaydına yazılı gerekçeye dayanır**: B-39 (üçüncü kez KRİTİK ölçülen bulgu) ve B-66
> (sahipsiz kritik). `Urunler`/`Banka`/`Evraklar`/`Faturalar`/`Emanet` dokunuşları **tek satırlık
> sorgu düzeltmesidir** (`aktif=true`), görsel dile ve menüye DOKUNULMADI (onlar P-36 / P-37).

---

## 11. Değişen dosyalar

> `git status --porcelain | grep -v -E "/(bin|obj)/"` bu depoda **tüm dalgaların** birikmiş
> değişikliğini gösteriyor (tek commit var). Bu paketin dokunduğu dosyalar tek tek:

**Sunucu**
- `dotnet/Kuyumcu.Api/Modules/Pano/PanoFiyatServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Pano/PanoModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Arama/AramaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Banka/EkstreModulu.cs`

**Sözleşme / sözlük**
- `dotnet/Kuyumcu.Contracts/Sayfalar/AramaSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/Sozlesmeler/EkstreSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`

**Ekran**
- `dotnet/Kuyumcu.Web/Bilesenler/KayitSecici.razor`
- `dotnet/Kuyumcu.Web/Pages/Vitrin.razor`
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor`
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor`
- `dotnet/Kuyumcu.Web/Pages/Banka.razor`
- `dotnet/Kuyumcu.Web/Pages/BankaEkstre.razor`
- `dotnet/Kuyumcu.Web/Pages/Emanet.razor`
- `dotnet/Kuyumcu.Web/Pages/Evraklar.razor`
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor`

**Test / denetim**
- `dotnet/Kuyumcu.Tests/P38SahipsizBulgularTestleri.cs` *(yeni)*
- `dotnet/Kuyumcu.Tests/PanoFiyatTestleri.cs` *(sözleşme değişikliği, § 4.2)*
- `dotnet/Kuyumcu.Tests/EkstreIceAktarmaTestleri.cs` *(yardımcı, § 4.3)*
- `dotnet/denetim.sh` *(P-38 sayaçları + P00-3 tabanı 174)*

**COMMIT YAPILMADI** (kurucu kuralı).

---

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Her kural için hatayı yeniden üreten test | ✅ | 20 yeni test; § 1 tablosu |
| (b) Mutasyon kanıtı (kır → kırmızı → geri al → `cmp`) | ✅ | § 7, 6 mutasyon |
| (c) 5198'de canlı doğrulama | ✅ | § 9, 10 satır |
| (d) `dotnet/denetim.sh` TEMİZ | ✅ | `DENETİM TEMİZ.` |
| (e) Tam süit 0 başarısız | ✅ | 3754 geçti |
| (f) Test kiracısı silindi | ✅ | tenants 9 → 9 |
| (g) Rapor + kimlik kapanış tablosu | ✅ | bu belge |
| Sayaç eklendi (3 + P00-3) | ✅ | § 8 |
| Kapsam kilidi (yeni ekran/menü/görsel dil yok) | ✅ | § 10 |
| 5199'a dokunulmadı | ✅ | yalnız 5198 kullanıldı |
| MIMARI-… ve PLAN belgelerine dokunulmadı | ✅ | — |

---

## 13. Açık kalanlar / devredilenler

1. **B-70 tur 8'de yeniden ölçülmeli.** Bugünkü kod bulguyu üretmiyor (§ 4.1); "kapandı"
   demiyorum, "**üretilemedi + kilitlendi**" diyorum. Zaman-sırası varsayımı doğrulanmadı.
2. **`HAREKETLERDE_ODEME_YONTEMI_YENI_YOL` / `FIS_TOPLAM_ODEME_KALEMI_FARKI` sayaçları
   devredildi** (§ 4.4) — ödeme/fiş toplamı sahibi olan pakete.
3. **Pano marj kapısı kotasyon yokken zayıftır** (yalnız 1.000.000 mutlak tavan). Kaynak
   çekmeden önceki ilk saniyelerde saçma bir marj kaydedilebilir; kaydedilse bile **vitrine
   ₺0,00 çıkmaz** (ikinci kapı `MARJ_GECERSIZ`). Kalıcı çözüm: kotasyon yoksa marj kaydını
   reddetmek — ürün kararı olduğu için tek başıma almadım, kurula düşer.
4. **Sistem denetim izi satırları.** Canlı doğrulama için üretilen davet/kayıt işlemleri
   `denetim_kayitlari`'na düştü; kiracıya bağlı olanlar kiracıyla birlikte silindi,
   **kiracı-üstü sistem kayıtları append-only olduğu için SİLİNMEDİ** (defter budanmaz).
5. Triyaj § 6.2'deki diğer iki çelişki (**B-2** `/ayarlar` künyesi, **B-48** RPO aşımı)
   bu paketin kapsamında değildi; B-48 bir **işletim olayıdır** (yedek zamanlayıcısı
   18.09.2026'dan beri koşmuyor) ve **Batuhan'a eskale edilmelidir**.

## 14. Kurucuya / mali müşavire not

Bu pakette mevzuat hükmü koda sabitlenmedi; yasal/mali bir karar üretilmedi.
Tek ürün kararı gerektiren nokta § 13/3'tür (kur kaynağı henüz veri vermemişken pano marjı
kaydedilebilsin mi?) ve ekip kuruluna bırakılmıştır.
