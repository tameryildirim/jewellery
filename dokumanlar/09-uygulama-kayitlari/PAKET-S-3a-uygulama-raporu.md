# PAKET S-3a — Sessiz veri kaybı ve çalışma zamanı sağlamlığı (uygulama raporu)

> Dalga 3-H (hata düzeltme). Roller: Taşkın (backend) + Ertuğ (frontend).
> Tarih: 2026-09-18. Kapsam kaynağı: `HAZIRLIK-KURULU-3.md` § engel 4, 11, 15, 19.
> **Yeni ekran / yeni modül / yeni özellik AÇILMADI** (kurucu talimatı 13 · kapsam kilidi).

---

## 1. Özet

Üç borç kanıtla kapandı, bir borç **zaten kapalı bulundu ve regresyona bağlandı**, üç kalem
gerekçesiyle **devredildi**. Ölçüm: derleme **0 uyarı · 0 hata**, `dotnet/denetim.sh` **TEMİZ**,
tam süit **2464/2464 yeşil (0 başarısız)**, 4 mutasyonun **dördü de** ilgili testi kırmızıya
çevirdi, canlı doğrulama 5198'de gerçek MySQL verisiyle 4 adımda yapıldı ve test kiracısı silindi
(kiracı sayısı 8 → 8).

| # | Kurul engeli | Durum |
|---|---|---|
| 4 | Sipariş düzenle-kaydet kalem verisini imha ediyor (KRİTİK) | **KAPANDI** — sunucu + istemci, 4 HTTP + 3 bUnit testi, canlı kanıt |
| 11 | Tek deserialize hatası render ağacını öldürüp sessiz veri kaybı yapıyor (KRİTİK) | **S-3a ayağı KAPANDI** (ortak `KabukHatasi` hata sınırı + görünür bant + destek kodu). SÖZLEŞME-1 ortak DTO ayağı **devredildi** (aşağı bak) |
| 15 | Yenileme jetonu yarışı / oturum sessizce düşüyor (YÜKSEK) | **ApiIstemci ayağı ZATEN KAPALI** bulundu (UX-4d md.15); doğrulandı ve raporlandı. QA turu 4 ayağı devredildi |
| 19 | tr-TR kültür hatası (YÜKSEK) | **Ortak `Bicim.Makine(...)` + denetim.sh sayacı** eklendi; 44 kalıntı için tavan ratchet'i kuruldu (süpürme 34-Ö'ye devredildi) |

---

## 2. Yapılanlar

### 2.1 Engel 4 — sipariş kaleminde sessiz veri kaybı (KRİTİK)

**Gerçek sebep koda bakılarak doğrulandı (spesifikasyon iddiası doğruydu, ama sebep iki taraflıydı):**

- **Sunucu** (`Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs`): PATCH gövde düzeyinde
  "gönderilmeyen = koru" sözleşmesini uyguluyordu ama **kalem dizisinde uygulamıyordu** —
  gelen dizi ham DTO'ya deserialize edilip mevcut küme silinip yeniden yazılıyordu
  (`// Kalem kimliği DIŞARIDAN gelmez: küme tümüyle değişir (sil + yaz).`).
- **İstemci** (`Kuyumcu.Web/Pages/SiparisDetay.razor`): düzenleme formundaki `FormKalemi`
  yalnız `Tanim` + `Adet` taşıyordu; yükleme sırasında `Tanim = k.Tanim ?? k.UrunAd` ile ürün adı
  **serbest tanıma çevriliyor**, kaydetmede `urunId`/`ayarId`/`gramAgirlik` **hiç gönderilmiyordu**.

**Düzeltme (iki taraflı, tek sözleşme):**

1. Sunucuda kalem düzeyinde PATCH birleştirmesi (`KalemBirlestir`): `id` verilen kalem
   **güncellenir** (kimlik kararlı kalır), gövdede **bulunmayan alan korunur**, açıkça `null`
   yazılan alan **silinir**; `id` verilmeyen kalem **doğar**; dizide **anılmayan** mevcut kalem
   silinir (küme anlamı korunur); başka siparişin kalem kimliği **404** alır.
2. İstemcide `FormKalemi` kalemin tam alan kümesini taşır (`Id`, `UrunId`, `UrunAd`, `Tanim`,
   `AyarId`, `Gram`, `Adet`); form ürün bağını **salt okunur rozetle** gösterir (UX-S21),
   ayar ve gram alanları açılır (UX-S26: oluşturma formu = düzenleme formu); PATCH gövdesi
   kalemi kimliği ve tüm alanlarıyla geri gönderir.
3. Geçerlilik ölçütü sunucuyla hizalandı: "ürün var **ya da** tanım var". Eskiden ürün bağlı
   kalemde tanım boş olduğu için **Kaydet sessizce etkisiz kalıyordu** (UX-S27 ihlali).

### 2.2 Engel 11 — çökme sessiz veri kaybına dönüşüyor (S-3a ayağı)

Yeni ortak bileşen `Kuyumcu.Web/Bilesenler/KabukHatasi.razor` (Blazor `ErrorBoundary` sarmalayıcısı)
`AnaYerlesim.razor` içinde **her sayfa kökünü** sarar (hem giriş sayfası hem `<main>` gövdesi):

- sayfa ağacı çökse bile kabuk (menü, üst bar) ayakta kalır;
- kullanıcı **boş ekran değil**, `HataBandi` ile **görünür hata bandı** görür (`#kabuk-hata-bandi`);
- hata `ApiHata` ise sunucunun korelasyon kimliği **destek kodu** olarak basılır
  (`X-Istek-Kimligi`; kimlik yoksa rozet **hiç çizilmez** — uydurma kod gösterilmez, md.1);
- `#kabuk-hata-yeniden` düğmesi `Recover()` çağırır (her düğme gerçek iş yapar — DoD ek md.1).

### 2.3 Engel 19 — tr-TR kültür hatası (S-3a ayağı)

- `Bicim.Makine(decimal|double|int|DateOnly)` eklendi: SVG path/koordinat, `input type="number"`,
  sorgu dizesi ve JSON gövdesi için **daima InvariantCulture**, birim/simge taşımaz.
  İnsan biçimi (`Tl`, `Para`, `Gram`, `Miktar`) **değiştirilmedi**.
- `denetim.sh`'e ratchet sayacı eklendi: **"Kültürsüz biçimli ToString (Web)" = 44 (tavan 44)**.

### 2.4 Engel 15 — yenileme jetonu yarışı

Koda bakıldığında **zaten uygulanmış** bulundu (`ApiIstemci.TekUcuslaYenileAsync`, UX-4d md.15):
tek uçuş (single-flight) semaforu, tek kez oturum düşürme, tek yönlendirme ve
`OturumDustuHatasi`. Mevcut regresyon: `Kuyumcu.Tests/UX4dOturumTestleri.cs`.
**Yeniden yazılmadı** (ikinci motor yasağı). Kalan ayak (QA turu 4 uzun ömürlü jeton,
`kapi.sh` "ölçülemeyen rota = 0" sayacı) süreç işidir — devredildi.

---

## 3. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| 1 | Kurul metni "kalem güncellemelerinde tam-gövde değiştirme yasağı"nı **yalnız sunucu** işi gibi tarifliyordu; düzeltme **iki taraflı** yapıldı | Yalnız sunucu düzeltilseydi istemci alanları hiç göndermediği için veri yine kaybolurdu; yalnız istemci düzeltilseydi başka bir istemci aynı tuzağa düşerdi. Hedefe sadık kalındı |
| 2 | "Kültürsüz ToString **= 0**" sayacı yerine **tavan 44** ratchet'i kuruldu | Bugün Web'de 44 kültürsüz biçim çağrısı var ve çoğu **insan bağlamı** (N0, dd.MM.yyyy). Hepsini bu pakette süpürmek kapsam kilidini ihlal ederdi. Uydurma bir "0" yazmak md.1 ihlali olurdu; sayı **artamaz** |
| 3 | `denetim.sh` sayacı ilk yazımda `grep -P` kullanıyordu ve **sahte 0** üretti | BSD grep `-P` desteklemez; sessizce boş sonuç döndü. İki aşamalı taşınabilir süzmeye çevrildi ve gerçek sayı (44) ölçüldü. Bu dosyada bırakılsaydı **kapı sahte TEMİZ** verirdi |
| 4 | SÖZLEŞME-1 (~30 uç için ortak `Kuyumcu.Contracts` DTO) **yapılmadı** | Kapsam kilidi + tek paket boyutu. Bu pakette engel 11'in **hasar kontrolü** ayağı (ErrorBoundary) sevk edildi; DTO birleştirme ayrı dilim. **devredildi: SÖZLEŞME-1** |

---

## 4. Migration

**Gerekmedi.** Şema değişikliği yok: kalem PATCH birleştirmesi mevcut `siparis_kalemleri`
tablosunda çalışır, yeni alan/varlık yok. `dotnet ef` **çalıştırılmadı**.

---

## 5. Testler

| Kapsam | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Yeni S-3a testleri (filtreli) | 12 | 12 | 0 | `--filter FullyQualifiedName~S3a` |
| Tam süit (sonra) | **2464** | **2464** | **0** | trx: `scratchpad/trx-pS-3a/_Mac_2026-09-18_00_48_12.trx` |

Yeni test dosyaları (12 test):

- `dotnet/Kuyumcu.Tests/S3aSessizVeriKaybiTestleri.cs` — 4 HTTP (gerçek boru hattı) + 2 saf kültür testi
- `dotnet/Kuyumcu.Tests/S3aEkranTestleri.cs` — 3 bUnit sipariş düzenleme + 3 bUnit `KabukHatasi`

---

## 6. Mutasyon kanıtları

| Kod | Kod/dosya | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|---|
| M1-1 | `SiparisModulu.cs` · `KalemBirlestir` | `UrunId/Tanim/AyarId/GramAgirlik = taban?.X` → `= null` (eski "sil + yaz" davranışı) | `S3aSiparisKalemKorumaTestleri.PATCH_kalemde_gonderilmeyen_alan_KORUNUR_urun_ayar_gram_silinmez`, `…PATCH_kalemde_acik_null_SILER` | `cmp` **AYNI** |
| M1-2 | `SiparisDetay.razor` · `Kaydet` | PATCH gövdesinden `urunId` satırı silindi | `S3aSiparisDuzenlemeEkranTestleri.Kaydet_kalemi_kimligi_urunu_ayari_ve_gramiyla_GERI_GONDERIR` | `cmp` **AYNI** |
| M2-1 | `Bicim.cs` · `Makine(decimal)` | `InvariantCulture` → `CurrentCulture` | `S3aMakineBicimiTestleri.Makine_trTR_kulturunde_bile_nokta_ayirici_uretir` | `cmp` **AYNI** |
| M3-1 | `KabukHatasi.razor` · `Kimlik` | `istisna is ApiHata a ? a.IstekKimligi : null` → `null` | `S3aKabukHatasiTestleri.Sunucu_korelasyon_kimligi_varsa_destek_kodu_basilir_yoksa_UYDURULMAZ` | `cmp` **AYNI** |

Test açığı bulunmadı (dört mutasyonun dördü de yakalandı). Son mutasyondan sonra temiz derleme:
**0 Uyarı · 0 Hata**.

---

## 7. Denetim

`dotnet/denetim.sh` → **DENETİM TEMİZ.** (RC=0)
Yeni satır: `✓ Kültürsüz biçimli ToString (Web)  44 (tavan 44)`
`Pages içinde istemci record tanımı` sayacı **artmadı** — yeni istemci record açılmadı.

---

## 8. Canlı doğrulama (localhost:5198, gerçek MySQL verisi)

Test kiracısı: **PAKETS3A-TEST** (iş sonunda silindi). 1 cari, 1 ürün (S3A-1, K22, 12,5 g),
1 sipariş **S-2026-000001** (TASLAK), 1 ürün bağlı kalem.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `PATCH /siparisler/{id}` gövdesi `{aciklama, kalemler:[{id, adet}]}` — QA turu 3'ün ölümcül isteği | 200, ürün/ayar/gram **korunur** | **200** · kalem: `urunId=01M2RNHSGR…, ayarId=K22, gramAgirlik=12.5` | SQL: `select id,urun_id,ayar_id,gram_agirlik,adet from siparis_kalemleri` → `01M2RNHXSD… 01M2RNHSGR… K22 12.500 1.000` |
| 2 | `{kalemler:[{id, urunId:null, tanim:"serbest kunye"}]}` | 200, `urunId` **silinir**, gönderilmeyen `gramAgirlik` **korunur** | **200** · `urunId=null, tanim="serbest kunye", ayarId=K22, gramAgirlik=12.5` | SQL: `urun_id = NULL`, `ayar_id=K22`, `gram_agirlik=12.500` |
| 3 | `{kalemler:[{id:"01BADBAD…"}]}` (bu siparişe ait olmayan kalem kimliği) | **404** | **404** · `"Kalem 1: bu siparişte 01BADBAD… kimlikli kalem yok"` | Yanıt gövdesi okundu; `istekKimligi` döndü |
| 4 | `{kalemler:[{tanim:"yepyeni kalem", adet:2}]}` (id'siz) | Eski kalem silinir, yeni kalem doğar | **200** · yeni kimlik `01M2RNJC2S…`, eski kimlik yok | SQL: `count(*) from siparis_kalemleri where siparis_id=… ` → **1** |

**Temizlik:** API (PID 19192) durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
**5199 (kurucunun inceleme sunucusu) hiç durdurulmadı/dokunulmadı** — hâlâ dinliyor.
Test kiracısının verisi `tenant_id` kolonu olan **49 tablodan** silindi, davet kaydı silindi.
**Kiracı sayısı: başlangıç 8 → iş sonu 8.** `QA-TEST-1/2/3` fikstürlerine **dokunulmadı**.

> Not: canlı doğrulama için API tek seferlik `Sistem__Anahtari` **ortam değişkeniyle** başlatıldı
> (davet jetonu üretmek için; F-2-min kaydı davetsiz açılmıyor). Hiçbir yapılandırma dosyası
> değiştirilmedi; anahtar ve jeton dosyaları iş sonunda boşaltıldı, hiçbir sır çıktıya yazılmadı.

---

## 9. DoD sözleşmesi (Bertan koşulu)

Bu pakette **yeni varlık / yeni kayıt türü doğmadı** — mevcut `siparis_kalemleri` kayıtlarının
**düzenlenebilirliği onarıldı**. Tabloya göre:

| Varlık | Düzenleme yolu | Yetki | Denetim | Ekran eylemi |
|---|---|---|---|---|
| `SiparisKalemi` (TASLAK sipariş) | `PATCH /api/v1/siparisler/{id}` · kalem `id` ile alan alan güncelleme; anılmayan kalem silinir | `[ModulYetkisi("siparis")]` + `[RolGerekli(SAHIP, MUDUR, MUHASEBE, TEZGAHTAR)]` (mevcut, değişmedi) | `DenetimYazmaDinleyicisi` SaveChanges üzerinden **kendiliğinden** (ikinci yazım yapılmadı) | `/siparisler/{id}` → **Düzenle** → ürün rozeti + tanım/ayar/gram/adet alanları + **Değişiklikleri kaydet** |
| `SiparisKalemi` (TASLAK dışı sipariş) | **Salt okunur** | — | — | `DegismezIpucu`: "Yanlışlık varsa siparişi gerekçeli olarak iptal edin ve yenisini açın" (mevcut, korundu) |

Finansal varlıklarda hard DELETE **yok**; sipariş kalemi finansal defter kaydı değildir
(hareket/kasa/banka/fatura değil) ve TASLAK aşamasında küme düzenlemesi zaten sözleşmeydi.
Storno taklidi **yapılmadı**; ikinci motor **açılmadı**.

**Dokunulan sayfalar (tek dokunuş kaydı):** `Kuyumcu.Web/Pages/SiparisDetay.razor`,
`Kuyumcu.Web/Layout/AnaYerlesim.razor`. Yeni bileşen: `Bilesenler/KabukHatasi.razor`.

---

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgecinden (depo `?? ` gösteriyor;
`dotnet/Kuyumcu.Api/Modules/Siparis/`, `Pages/`, `Tests/` dizinleri depoda izlenmiyor):

**Değiştirilen**
- `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs` (kalem PATCH birleştirmesi + `KalemBirlestir`)
- `dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor` (düzenleme formu tam alan kümesi)
- `dotnet/Kuyumcu.Web/Layout/AnaYerlesim.razor` (sayfa kökü hata sınırı)
- `dotnet/Kuyumcu.Web/Servisler/Bicim.cs` (`Makine(...)`)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (TR+EN: `Siparis.UrundenGeldi`, `Kabuk.SayfaHatasi`, `Kabuk.SayfaHatasiYeniden`)
- `dotnet/denetim.sh` (kültürsüz biçim sayacı)

**Yeni**
- `dotnet/Kuyumcu.Web/Bilesenler/KabukHatasi.razor`
- `dotnet/Kuyumcu.Tests/S3aSessizVeriKaybiTestleri.cs`
- `dotnet/Kuyumcu.Tests/S3aEkranTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-S-3a-uygulama-raporu.md` (bu dosya)

**COMMIT YAPILMADI.** `git add/commit/push`, `checkout/stash/reset/rm/mv` **çalıştırılmadı**.

---

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Hatayı yeniden üreten test | ✅ | 4 HTTP + 3 bUnit sipariş testi |
| Mutasyon kanıtı (geri al → kırmızı → iade) | ✅ | § 6, dördü de kırmızı, `cmp` AYNI |
| 5198 canlı doğrulama (HTTP + SQL) | ✅ | § 8, 4 adım |
| `dotnet/denetim.sh` TEMİZ | ✅ | § 7 |
| Tam süit 0 başarısız | ✅ | 2464/2464 |
| Test kiracısı silindi, QA-TEST-* korundu | ✅ | kiracı 8 → 8 |
| Kapsam kilidi (yeni ekran/modül/özellik yok) | ✅ | Yalnız mevcut ekranların borcu |
| Yeni istemci record yasağı | ✅ | Yeni record açılmadı; denetim sayacı artmadı |
| Tip sözleşmesi kuralı | ✅ | `gramAgirlik`/`adet` istemcide `decimal`, sunucu `decimal?`/`decimal` (`Siparis.cs:108-118`, `SiparisModulu.cs` DTO) |
| PATCH semantiği + regresyon testi | ✅ | Kalem düzeyinde "gönderilmeyen = değişmedi" + `PATCH_kalemde_acik_null_SILER` |
| Her düğme gerçek iş yapar | ✅ | `#kabuk-hata-yeniden` → `Recover()`; `#sipd-d-kaydet` → PATCH (bUnit ile ölçüldü) |
| Sözlük TR+EN, çıplak metin yok | ✅ | 3 yeni anahtar iki dilde |
| Mevcut element id'leri değişmedi | ✅ | Yeni id'ler `sipd-d-kalem-ayar/gram/urun-{sıra}`, `kabuk-hata-*` |
| Sır ekrana basılmadı | ✅ | Parola/jeton değişkende tutuldu, dosyalar boşaltıldı |

---

## 12. Açık kalanlar (devredildi)

1. **devredildi: SÖZLEŞME-1** — ~30 uç için ortak `Kuyumcu.Contracts` DTO ve
   `Pages` record tavanının 121'den azalan hedefe bağlanması. Bu pakette yalnız **hasar kontrolü**
   (ErrorBoundary) sevk edildi; ikizlenmiş sözleşmenin kendisi duruyor.
2. **devredildi: 34-Ö** — 44 kültürsüz biçim çağrısının süpürülmesi ve sınırsız `ToListAsync`
   ölçek borcu. `Bicim.Makine(...)` ve ratchet sayacı hazır; süpürme ayrı dilim.
3. **devredildi: QA turu 4** — dört rotanın (ayarlar, kullanıcılar, yetkiler, veri dışa aktarım)
   canlı ölçümü, uzun ömürlü QA jetonu, `kapi.sh` "ölçülemeyen rota = 0" sayacı.
   **Madde 1: bu rotalar hâlâ ölçülmedi; MFA/dışa aktarım iddiaları kapalı SAYILMAZ.**
4. **devredildi: UX-1-C3 / S-3b** — gider pusulası satıcı kimliği alanları, `/uyum` düğmeleri,
   storno kapsamı. Bu dilimin konusu değildi.
5. **DOĞRULANMALI** — "aynı desen diğer PATCH uçlarında taranır" maddesi **taranmadı**
   (adım ekonomisi). Sipariş dışındaki koleksiyon-içi PATCH uçlarının aynı tuzağı taşıyıp
   taşımadığı bilinmiyor; tahmin edilmedi.

---

## 13. Kurucuya / mali müşavire notlar

1. **Depoda 10+ `.mutorj` artığı var** (örn. `dotnet/Kuyumcu.Web/Pages/Islemler.razor.mutorj`,
   `dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj`). Önceki dalgaların mutasyon yedekleri;
   **bu oturumda üretilmedi ve silinmedi** (silme yetkisi kurucuda; TurkReport hook `rm` bloklar).
   Derlemeye girmiyorlar ama denetim taramalarını kirletiyorlar — kurucunun silmesi önerilir.
2. **Mevzuat hükmü koda sabitlenmedi**: bu pakette vergi/mevzuat hükmü işlenmedi
   (VUK 234 gider pusulası S-3b'de). Mali müşavir onayı gerektiren bir değişiklik **yok**.
3. **Ücretsiz pilot kuralı korundu**: fiyat/teklif/tahsilat metni üretilmedi.
