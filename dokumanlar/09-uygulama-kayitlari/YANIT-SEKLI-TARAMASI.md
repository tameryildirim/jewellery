# YANIT ŞEKLİ ↔ İSTEMCİ MODELİ TARAMASI (Paket S-2T)

> **Kapsam:** SALT OKUNUR. Hiçbir kaynak dosya değiştirilmedi. Bu belge tek çıktıdır.
> **Tarih:** 2026-09-17 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** 2c
> **Yöntem:** Kuyumcu.Web içindeki elle yazılmış istemci modellerinin (record) karşılık
> geldiği API dönüş şekliyle **uç bazında** karşılaştırılması. Otomatik çıkarım +
> şüpheli/kısmi bulunan her satırın **elle koda bakılarak** doğrulanması.

---

## 0. Ölçüm — sayılar (kanıtlı)

| Ölçü | Değer | Kanıt |
|---|---|---|
| Kuyumcu.Web içinde `record` tanımı (Pages+Bilesenler+Servisler+Layout) | **167** | `grep -rn "record " Kuyumcu.Web/{Pages,Bilesenler,Servisler,Layout}` (`.mutorj` artıkları hariç) |
| Yalnız `Pages/` altında istemci record | **122** | `dotnet/denetim.sh` → "Pages içinde istemci record tanımı 122 (tavan 122)" |
| Kurul tur 2'de bildirilen sayı | 101 | `HAZIRLIK-KURULU-2.md:211` — **sapma:** o günden bu yana (dalga 2a/2b) yeni sayfa/kayıt eklendiği için sayı 101 → 122 (Pages) / 167 (tüm Web) oldu. Tarama bu güncel sayı üzerinden yapıldı. |
| Taranan **tipli** API çağrısı (`GetAsync<T>/PostAsync<T>/PatchAsync<T>/PutAsync<T>/DeleteAsync<T>`, `JsonElement` hariç) | **132** | scratchpad `tipli.txt` |
| API'de bulunan uç (Route+Http* çifti) | **174** | scratchpad `api3.txt` |

> Not: `JsonElement` ile okunan çağrılar (şekil sözleşmesi olmayan, alan alan
> `TryGetProperty` ile gezilen yerler) bu taramanın dışındadır — onlarda derleme
> zamanı sözleşmesi zaten yok, çalışma zamanında da çökme değil sessiz eksiklik üretirler.

### Durum dağılımı (132 çağrı)

| Durum | Adet |
|---|---|
| **UYUMLU** | 69 |
| **UYUMSUZ-ÇÖKER** (çalışma zamanı hatası) | **0** (kurulun bildirdiği 2 kalem S-2'de kapandı — §2) |
| **UYUMSUZ-SESSİZ** (alan null/0 kalıyor) | **2** |
| **DOĞRULANMADI** | 61 |

**DOĞRULANMADI neden var (Madde 1 — tahmin yok):** bu 61 çağrının ucu ya ham
varlık/`List<T>` döndürüyor ya da gövde çok katmanlı servis zincirinden geçiyor;
otomatik çıkarım şekli **kesin** veremedi. Tahmin edilmedi, "uyumlu" sayılmadı.
Bunların elle doğrulanması **SÖZLEŞME-1** paketinin (Kuyumcu.Contracts) işidir:
sözleşme tek kaynağa taşındığında bu sınıf tamamen ortadan kalkar.

---

## 1. UYUMSUZ bulgular — ciddiyet sırasına göre

Çalışma zamanı hatası üreten (UYUMSUZ-ÇÖKER) **yeni** bir kalem bulunamadı.
Aşağıdaki iki kalem sessiz sınıftır: istek başarılı olur, alan `null`/varsayılan kalır,
ekran yanlış/boş değer gösterir.

### B-1 (SESSİZ, orta) — Aylık rapor "net has gram" hiç dolmuyor
| | |
|---|---|
| Sayfa | `dotnet/Kuyumcu.Web/Pages/Raporlar.razor:307` (çağrı), `:254-258` (model) |
| Model | `record AylikYanit(int Yil, int Ay, IslemToplam? Satis, IslemToplam? Alis, decimal KarTl, decimal? NetHasGram, List<AylikGun>? Gunler)` |
| Uç | `GET /api/v1/raporlar/aylik` → `Kuyumcu.Api/Modules/Rapor/RaporModulu.cs:645` → `AylikOzet` `:89`, zarf `:163-176` |
| API alanları | `yil, ay, satis, alis, tahsilat, veresiye, karTl, karKaynagi, satisAlisFarkiTl, karZarar, netHasGramHareketi, hareketSayisi, gunler` |
| Uyuşmazlık | İstemci **`NetHasGram`**, API **`netHasGramHareketi`**. Ad farklı → alan daima `null`. |
| Etki | Aylık raporda net has gram kartı boş/"—" kalır. UX-S28 ihlali değil (hata değil), ama **UX-S31** (tek ad, tek kaynak) ihlali. İlginç olan: `RaporModulu.cs:139-145` yorumunda "net has gram tek tanım" mührü var; sunucu adı düzeltilmiş, istemci adı geride kalmış. |
| Düzeltme | `NetHasGram` → `NetHasGramHareketi` (ya da `[JsonPropertyName]`). **UX-1-C3** (Raporlar). |

### B-2 (SESSİZ, düşük) — POS ürün seçicisinde ölü `Ayar` alanı
| | |
|---|---|
| Sayfa | `dotnet/Kuyumcu.Web/Pages/Islemler.razor:1174` (çağrı), `:637-639` (model), kullanım `:317, :527, :597` |
| Model | `record UrunVeri(string Id, string Ad, string? AyarId, string? Ayar, decimal HasGrami, decimal? IscilikTl, decimal? SatisFiyati, string? Birim = "ADET")` |
| Uç | `GET /api/v1/urunler` → `UrunModulu.cs:826` → `:387` — dönüş **`List<Data.Urun>`** (ham varlık) |
| API alanları | `Urun` varlığının kendi özellikleri (`Entities.cs:193-305`): `Id, TenantId, Tip, Kod, Ad, Aciklama, AyarId, Milyem, GramAgirlik, Birim, TasAgirlik, HasGrami, IscilikTl, SatisFiyati, …` — **`Ayar` diye bir özellik yok** |
| Uyuşmazlık | `Ayar` daima `null`. Ekranda `@(u.AyarId ?? u.Ayar)` yazıldığı için görünür bir bozulma yok (ölü yedek), ama sözleşmede karşılığı olmayan alan **varsayım** taşıyor. |
| Düzeltme | Alanı kaldır ya da sunucuda `ayar` (ayar adı) üret. **UX-1-C1** (İşlemler). |

---

## 2. Kurulun bildirdiği iki çöken kalem — bugünkü durum

| Kalem | Kurul kaydı | Bugün | Kanıt |
|---|---|---|---|
| Stok modalı NRE | `Urunler.razor:653 record StokYaniti(List<StokVeri> Stok)` ↔ API `subeStoklar` | **S-2'de kapandı** | İstemci artık `Urunler.razor:661 record StokYaniti(List<StokVeri>? SubeStoklar)`; API `UrunModulu.cs:659-679` → `{ urunId, toplamAdet, toplamHasGram, subeStoklar[] }`. Ad + nullability uyumlu. |
| `/uyum` DeserializeUnableToConvertValue | API `{adet, kayitlar}` zarfı ↔ istemci `List<…>` | **S-2'de kapandı / bugün üretilemiyor** | `UyumServisi.cs:25` `GetAsync<List<KimlikTespitSatir>>("/masak/kimlik-tespitleri")` ↔ `MasakModulu.cs:315` → `TespitleriListele` `:77` → `return liste.Select(m => new { … })` = **JSON dizisi**. Zarf yok, tür uyumlu. |

> **Madde 1 uyarısı:** `/uyum` sayfasının diğer 26 UyumServisi kaydı bu taramada
> **DOĞRULANMADI** kutusundadır (§3). "Sayfanın tamamı temiz" denmiyor; yalnız
> kurulun kanıtladığı çağrının bugün uyumlu olduğu gösteriliyor.

---

## 3. Uç bazında tablo

Aşağıda her satır: **sayfa:satır · model · uç · API kaynağı:satır · durum**.
UYUMLU satırların tam alan listesi scratchpad dosyalarında (`karsilastirma3.txt`,
`ozet2.txt`) üretildi; burada kanıt olarak kaynak dosya:satır verilmiştir.

### 3.1 Elle doğrulanmış satırlar (otomatik tarama şüpheli/kısmi işaretledi)

| # | Sayfa:satır | Model | Uç | API kanıtı | Durum |
|---|---|---|---|---|---|
| 1 | `Pages/UrunAgaci.razor:244` | `KategoriDugum` | `GET /urun-kategorileri?sadeceAktif=false` | `Urun/KategoriModulu.cs:100` → `Dugum` `:188` → `{id, ad, sira, aktif, urunSayisi, altlar}` | **UYUMLU** |
| 2 | `Pages/Urunler.razor:812` | `KategoriDugum` | `GET /urun-kategorileri` | aynı (`KategoriModulu.cs:188`) | **UYUMLU** |
| 3 | `Pages/Urunler.razor:970` | `UrunTamKart` (23 alan) | `GET /urunler/{id}` | `UrunModulu.cs:501-507` ham `Urun` varlığı; alanlar `Entities.cs:193-305` | **UYUMLU** (uyarı: §4/N-1) |
| 4 | `Pages/UrunEtiketleri.razor:169` | `UrunVeri` | `GET /urunler?aktif=true&limit=500` | `UrunModulu.cs:826` → `:387` `List<Data.Urun>` | **UYUMLU** |
| 5 | `Pages/Fiyatlandirma.razor:271` | `UrunOzet` | `GET /urunler` | aynı | **UYUMLU** |
| 6 | `Pages/Siparisler.razor:472` | `UrunSatiri` | `GET /urunler?aktif=true&limit=100` | aynı | **UYUMLU** |
| 7 | `Pages/Islemler.razor:1174` | `UrunVeri` | `GET /urunler?limit=200` | aynı; `Ayar` karşılıksız | **UYUMSUZ-SESSİZ (B-2)** |
| 8 | `Pages/Islemler.razor:1031` | `List<IadeAdayi>` | `GET /islemler/iade-adaylari` | `Satis/SatisModulu.cs:1257` → `:1296-1300` `{id, Tarih, TlTutari, urunId, urunAdi, toplamAdet, iadeEdilenAdet, kalanAdet, CariId}` | **UYUMLU** |
| 9 | `Pages/Islemler.razor:677` | `Teklif` | `GET /fiyat/teklif` | `Fiyat/FiyatModulu.cs:739` → `:760` (`teklifVar=false, sebep`) ve `:763-774` (`indirimOncesiFiyat` dâhil) | **UYUMLU** |
| 10 | `Pages/Urunler.razor:1052` | `EtiketVeri` | `GET /urunler/{id}/etiket` | `UrunModulu.cs:467` → `:471-499`, `etiketMetni` mevcut | **UYUMLU** |
| 11 | `Pages/KarZarar.razor:230` | `OzetVeri` | `GET /raporlar/kar-zarar` | `Maliyet/KarZararServisi.cs:35` → `:94-120` (`hasilatTl, hasilatHas, maliyetiBilinenAdet` mevcut) | **UYUMLU** |
| 12 | `Pages/Kur.razor:281` | `KaynakDurumu` | `GET /kur/kaynaklar` | `Kur/KurModulu.cs:696` → `{kaynaklar, varsayilan, secili, sozlesmesizKaynaklarAcik, seciliSozlesmesiz, manuelKod, uyari}` | **UYUMLU** |
| 13 | `Pages/Raporlar.razor:307` | `AylikYanit` | `GET /raporlar/aylik` | `Rapor/RaporModulu.cs:645` → `:89`, zarf `:163-176` | **UYUMSUZ-SESSİZ (B-1)** |
| 14 | `Servisler/UyumServisi.cs:99` | `PusulaBelge` | `GET /gider-pusulalari/{id}/belge` | `Hurda/HurdaModulu.cs:75` — anonim gövde içinde Türkçe metin/yorum var, otomatik çıkarım güvenilir değil | **DOĞRULANMADI** |

### 3.2 Otomatik doğrulanan UYUMLU satırlar (örnekler, kanıtlı)

| Sayfa:satır | Model | Uç | API kanıtı |
|---|---|---|---|
| `Pages/Kasalar.razor:416` | `HareketYaniti(Hareketler)` | `GET /kasalar/{id}/hareketler` | `Kasa/KasaModulu.cs:469` → `:404` `{kasa, hareketler}` |
| `Pages/Banka.razor:379` | `HesapYaniti(Hesaplar)` | `GET /bankalar` | `Banka/BankaModulu.cs:415` → `:221` `{hesaplar}` |
| `Pages/Yetkiler.razor:99` | `YetkiMatrisi(Moduller, Roller, Matris)` | `GET /yetkiler` | `Yetki/YetkiModulu.cs:237` `{moduller, roller, matris}` |
| `Pages/Cariler.razor:635` | `EkstreYaniti(Bakiye, Hareketler)` | `GET /cari/{id}/ekstre` | `Cari/CariModulu.cs:864` → `:766` `{bakiye, hareketler}` |
| `Pages/Cariler.razor:657` | `CariDetayVeri` (10 alan) | `GET /cari/{id}` | `Cari/CariModulu.cs:372` `{id, tenantId, tur, ad, telefon, email, vergiNo, adres, notlar, krediLimitiTl, varsayilanVadeGun, fiyatListesiId, aktif, olusturuldu, guncellendi, bakiye}` |
| `Pages/Sayim.razor:167/185/198/226` | `Detay` (14 alan) | `GET/POST /sayim…` | `Sayim/SayimModulu.cs:120` zarfı |
| `Pages/Sayim.razor:211` | `KapatYaniti(Uygulanan, Atlanan, Detay)` | `POST /sayim/{id}/kapat` | `SayimModulu.cs:173` `{uygulanan, atlanan, detay}` |
| `Pages/VeriDisaAktarim.razor:186/227` | `GecmisYanit(Toplam, Kayitlar)` | `GET /disa-aktarim/gecmis` | `Disaaktarim/DisaAktarimModulu.cs:255` `{toplam, kayitlar}` |
| `Pages/Faturalar.razor:823` | `KalemYaniti` (6 alan) | `GET /faturalar/{id}/kalemler` | `Fatura/FaturaModulu.cs:961` `{faturaId, faturaNo, kalemKaynagi, hasDegerKaynagi, toplam, kalemler}` |
| `Pages/Faturalar.razor:667` | `FaturalanabilirYanit` | `GET /faturalar/faturalanabilir-islemler` | `FaturaModulu.cs:429` `{gun, limit, dahaVar, islemler}` |
| `Pages/Faturalar.razor:788` | `FisYaniti` (8 alan) | `GET /islemler/fis/{fisId}` | `Satis/SatisModulu.cs:1824` → `FisOku` `:1716`, zarf `:1752-1774` |
| `Pages/SiparisDetay.razor:314` | `Kart` (21 alan) | `GET /siparisler/{id}` | `Siparis/SiparisModulu.cs:671` → `Detay` `:283`, zarf `:301-330` (`cariAd` `:308`) |
| `Pages/Panel.razor:268` | `GunlukOzet` | `GET /raporlar/gunluk` | `Rapor/RaporModulu.cs:638` → `:29`, zarf `:55-83` (`tarih, satis, alis, hurda, netHasGramHareketi, hareketSayisi, kasalar`) |
| `Servisler/UyumServisi.cs:106` | `Mutabakat` | `GET /raporlar/pos-fatura-mutabakati` | `RaporModulu.cs:195` `{tarih, tahsilat, satis, alis, hurda, fatura, fark, mutabik, uyarilar, faturasizIslemler}` |
| `Servisler/UyumServisi.cs:25` | `List<KimlikTespitSatir>` | `GET /masak/kimlik-tespitleri` | `Masak/MasakModulu.cs:315` → `:77` dizi döner |
| `Pages/Urunler.razor:926` | `StokYaniti(SubeStoklar)` | `GET /urunler/{id}/stok` | `UrunModulu.cs:659-679` |

(Kalan UYUMLU satırların tam listesi ve alan kümeleri scratchpad `ozet2.txt` dosyasındadır;
tümü aynı yöntemle — uç → servis metodu → anonim nesne alan adları — çıkarıldı.)

### 3.3 DOĞRULANMADI kutusu (61 çağrı) — neden ve nereye düşer

Otomatik çıkarımın şekli kesinleştiremediği çağrılar; hiçbiri "uyumlu" varsayılmadı.
Başlıca üç kalıp:
1. **Ham `List<Varlık>` / `IQueryable` dönen uçlar** — şekil varlık sınıfının kendisi;
   alan alan doğrulaması varlık tanımına bakmayı gerektirir (örn. `/kasalar`, `/subeler`,
   `/reyonlar`, `/kullanicilar`, `/fiyat/listeler`, `/fiyat/kampanyalar`, `/kur/gecmis`).
2. **Çok katmanlı servis zinciri** (kontrolör → servis → yardımcı) — örn. `/kur/guncel`
   (`KurModulu.cs:783` → `SnapshotZenginlestir` `:940`), `/emanet/{cariId}`, `/bildirimler`, `/isler`.
3. **`UyumServisi` altındaki 26 kayıt** (MASAK/gider pusulası/saklama) — tek serviste
   toplandıkları için sayfa bazlı değil servis bazlı doğrulama gerekir.

---

## 4. UX-1-C paketi eşlemesi

| Bulgu | Sayfa | Düşeceği paket |
|---|---|---|
| B-2 — `UrunVeri.Ayar` karşılıksız | İşlemler (POS) | **UX-1-C1** (Giriş/Panel/İşlemler/Cariler/Ürünler/Ağaç/Etiketler) |
| B-1 — `AylikYanit.NetHasGram` ↔ `netHasGramHareketi` | Raporlar | **UX-1-C3** (Gider pusulası/Uyum/Raporlar/Kâr-zarar/Vade/Denetim izi) |
| §3.1/14 — gider pusulası belge şekli doğrulanamadı | Gider pusulası | **UX-1-C3** |
| §3.3 kalıp-1 (ham varlık dönen liste uçları) | Kasa/Banka/Kur/Fiyat/Emanet/Sayım/Fatura | **UX-1-C2**; kullanıcı/yetki/işletme/ayar tarafı **UX-1-C4** |
| §3.3 kalıp-3 (UyumServisi 26 kayıt) | Uyum | **UX-1-C3** |
| Yapısal çözüm (tüm kutuyu kapatan) | — | **SÖZLEŞME-1** (Kuyumcu.Contracts) |

---

## 5. Yan bulgular (kapsam dışı ama kayda geçer)

- **N-1 — Ham varlık serileştirmesi (gizlilik + sözleşme):** `GET /urunler`, `GET /urunler/{id}`
  ve `GET /cari/{id}` istemciye **`tenantId`, `olusturuldu`, `guncellendi`** gibi iç alanları da
  yolluyor (`UrunModulu.cs:501-507`, `CariModulu.cs:372`). İstemci bunları okumuyor
  (sessiz fazlalık), ama sözleşme yüzeyini ve yanıt boyutunu gereksiz büyütüyor;
  kiracı kimliğinin istemciye inmesi Mehmet'in (06 Güvenlik) bakacağı bir kalem.
  **Bu pakette düzeltilmedi (salt okunur).**
- **N-2 — `.mutorj` artıkları hâlâ ağaçta:** `Kuyumcu.Web/Pages/` altında `Urunler.razor.mutorj`,
  `Islemler.razor.mutorj`, `Sayim.razor.mutorj`, `Fiyatlandirma.razor.mutorj`,
  `VadeTakibi.razor.mutorj`; `Kuyumcu.Api/Modules/Urun/` altında `UrunModulu.cs.mutorj`,
  `BelgeModulu.cs.mutorj`, `PirlantaKapisi.cs.mutorj`. Derlemeye girmiyorlar ama
  aramaları kirletiyorlar (kurul tur 2 bulgusu (c) ile aynı). **Silme kurucuya bırakıldı**
  (TurkReport hook `rm` bloklar).
- **N-3 — Sayı sapması:** kurulun "101 record" rakamı bugün 122 (Pages) / 167 (tüm Web).
  `denetim.sh` sayacının tavanı da 122'ye çekilmiş durumda; SÖZLEŞME-1 hedefi
  "Pages içinde record = 0" olduğuna göre bu tavan geçici bir tampon, kalıcı hedef değil.

---

## 6. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Salt okunur — kaynak dosya değiştirilmedi | ✅ | `git status --porcelain` çıktısındaki `M` satırlarının tamamı oturum başındaki snapshot ile birebir aynı; bu paketin tek yeni dosyası bu rapor + `PAKET-S-2T-uygulama-raporu.md` |
| Rapor `dokumanlar/09-uygulama-kayitlari/YANIT-SEKLI-TARAMASI.md` | ✅ | bu dosya |
| Her satırda dosya:satır kanıtı | ✅ | §1, §2, §3 tabloları |
| Uyumsuzlar ciddiyet sırasına göre | ✅ | §1 (çöken 0 → sessiz 2) |
| UX-1-C eşlemesi | ✅ | §4 |
| `dotnet/denetim.sh` TEMİZ | ✅ | "DENETİM TEMİZ." |
| Derleme/test/git yok | ✅ | çalıştırılmadı (değişiklik yok) |
| S-2'nin iki kalemi işaretlendi | ✅ | §2 |

## 7. Açık kalanlar

1. **61 çağrı DOĞRULANMADI** — SÖZLEŞME-1 kapsamında uç uç doğrulanmalı ya da
   `Kuyumcu.Contracts` ile derleme zamanına taşınarak sınıf tamamen kaldırılmalı.
2. B-1 ve B-2'nin düzeltmesi bu pakette **yapılmadı** (kapsam salt okunur); UX-1-C3 ve
   UX-1-C1'e devredildi.
3. N-1 (ham varlık serileştirmesi) Mehmet'in güvenlik incelemesine düşer.
4. `.mutorj` artıklarının silinmesi kurucunun elinde (hook engeli).

## 8. Kurucuya / mali müşavire not

Bu pakette **hiçbir mevzuat hükmü koda yazılmadı, hiçbir hüküm yorumlanmadı**;
tarama tamamen teknik (JSON alan adı eşleşmesi) olduğu için mali müşavire
çıkarılacak bir kalem doğmadı. Gider pusulası belge ucunun şekli (§3.1/14)
doğrulanamadığından, o ekranın mevzuata uygun alan kümesi taşıdığı **iddia edilmiyor** —
UX-1-C3'te ayrıca doğrulanmalıdır.
