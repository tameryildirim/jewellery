# PAKET F-2-min — Kayıt ucu kilidi ve deneme asgarisi · Faz A uygulama raporu

> Dalga 3 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Tarih: 2026-09-17
> Kapsam: **YALNIZ Faz A**. Faz B (`/sistem/kiracilar` Blazor konsolu,
> sessionStorage) MÜHÜR 4 / K5 gereği **dalga 6**'ya (T-4b yanı) alındı ve bu
> dalgada dosyası **rezerve edilmedi**.

## 1. Özet — ne kapandı

"Bedava, sınırsız kiracı açma" bulgusu kapandı:

* `POST /api/v1/auth/kayit` artık **tek kullanımlık, süreli bir davet jetonu**
  ister; davetsiz istek **403** döner ve **hiçbir yan etki** (tenant / şube /
  kullanıcı) oluşmaz.
* Kiracı `DENEME` durumunda, `deneme_bitis = now + davet.deneme_gun` ile doğar.
* Süre dolunca ya da durum `SALT_OKUNUR` olunca **yazma kapanır**; **okuma,
  giriş ve dışa aktarım açık kalır** (veri rehin alınmaz).
* Yönetim `/api/v1/sistem/*` uçlarındadır ve **yapılandırma sırrıyla**
  korunur. **'SISTEM' rolü AÇILMADI** (MÜHÜR 4 / K1).
* Mevcut 8 kiracı migration sonrası `AKTIF`, `deneme_bitis = NULL` ile açıldı;
  yazmaları **kesilmedi** (canlı doğrulandı).

## 2. Derleme

```
dotnet build Kuyumcu.sln --nologo   →  0 Hata · 0 Uyarı (API/Core/Web)
```

Tam çözüm koşumunda görünen **4 uyarı, önceden var olan** `xUnit2008`
uyarılarıdır (`Kuyumcu.Tests/MadenTestleri.cs:78-83`); bu pakette
dokunulmadı ve sayıları **artmadı**.

## 3. Migration

* **Ad:** `20260917194819_KiraciDenemeVeDavet`
* **Dosyalar:** `.cs` 4 353 bayt · `.Designer.cs` 148 741 bayt (ikisi de > 0).
* **Up() gövdesi okundu:** `tenants` → `durum VARCHAR(16) NOT NULL
  DEFAULT 'AKTIF'`, `deneme_bitis DATETIME(6) NULL`, `davet_id VARCHAR(36)
  NULL`; `IX_tenants_durum_deneme_bitis`; yeni `kiraci_davetleri` tablosu +
  `IX_kiraci_davetleri_jeton_hash (UNIQUE)` +
  `IX_kiraci_davetleri_gecerlilik_kullanildi_mi`.
* **kuyumcu_net'e uygulandı:**

```
Applying migration '20260917194819_KiraciDenemeVeDavet'.  Done.
SELECT COUNT(*), SUM(durum='AKTIF'), SUM(deneme_bitis IS NULL) FROM tenants;
→ 8   8   8
```

Yani **üretim durdurucu risk gerçekleşmedi**: hiçbir mevcut kiracı `DENEME`
doğmadı.

> Not (bu paketten önce var olan durum, bilgi amaçlı): `Migrations/` altında
> 45 `.cs` var, uygulanmış migration 41. Fark, 2026-09-15'ten kalan **4 adet
> 0 baytlık** `FiyatListesiVeKampanya` artığıdır ve `dotnet/denetim.sh`
> beyaz listesindedir. Bu pakette bunlara **dokunulmadı**.

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız | Kaynak |
|---|---|---|---|---|
| Tam süit (paket sonrası) | **2386** | **2386** | **0** | `trx-pF-2-min/_Mac_2026-09-17_23_05_20.trx` `<Counters .../>` |
| Tam süit (ara koşum, kararsız test düzeltilmeden) | 2386 | 2385 | 1 | `_Mac_2026-09-17_23_03_24.trx` |

**Yeni test sayısı: 54** (trx'ten sayıldı):

| Dosya | Adet |
|---|---|
| `KiraciKayitKilidiTestleri.cs` | 8 |
| `KiraciDurumKapisiTestleri.cs` | 19 |
| `SistemUclariTestleri.cs` | 16 |
| `DenemeBandiEkranTestleri.cs` | 6 |
| `DuzeltmePolitikasiTestleri.cs` (eklenen) | 5 |

**Paket öncesi sayı ÖLÇÜLMEDİ** (bu oturumda taban koşum yapılmadı);
komut çıktısından **türetilmiş** değer: 2386 − 54 = **2332**. Sayısal iddia
yasağı gereği bu bir ölçüm değil, açık bir çıkarımdır.

### Ara koşumda yakalanan kararsızlık (gizlenmedi)

İlk tam süitte `Gecersiz_Jeton_Kayit_Reddedilir_Kullanici_Tablosu_Degismez`
kırmızıya döndü:

```
Assert.Equal() Failure: Values differ   Expected: 752   Actual: 764
```

Sebep testin kendisiydi: yan etki yokluğunu **genel satır sayısıyla**
sınıyordu, oysa tam süitte başka koleksiyonlar paralel koşuyor ve
`kullanicilar` sayısı sürekli değişiyor. Test **izolasyona dayanıklı** hâle
getirildi (kiracı adı ve e-posta teste özgü üretiliyor, iddia o kayda
bakıyor). Ürün kodu bu yüzden **değiştirilmedi**.

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/F-2-min/<kod>/` altına kopyalandı →
`perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test` →
yedek geri kopyalandı → `cmp` ile birebir aynılık kanıtlandı (**her satırda
"cmp: birebir aynı"**).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-F2-01 | `KiraciDurumKapisi`: `DenemeBitis >= now` → `>= DateTime.MinValue` | `Deneme_Suresi_Gecmis_Kiraci_Urun_PATCH_403`, `Durum_beyaz_listesi_bilinmeyeni_KAPATIR` | cmp birebir |
| M-F2-02 | Muafiyet listesine `/api/v1` (geniş önek) | `SaltOkunur_Kiraci_Satis_POST_403`, `Muafiyet_listesi_GENIS_onek_ICERMEZ`, `Benzer_isimli_yollar_MUAF_DEGILDIR` ×3, `Bilinmeyen_Durum_Degeri_Yazmayi_KAPATIR` (7 kırmızı) | cmp birebir |
| M-F2-03 | Muafiyetten `/api/v1/disa-aktarim` silindi | `SaltOkunur_Kiraci_DisaAktarim_POST_ve_Indir_200`, `Muaf_yollar_SEGMENT_sinirinda_eslesir` ×4 | cmp birebir |
| M-F2-04 | Kayıtta davet geçerlilik kontrolü `if (davet is null) return` ile atlandı | `Suresi_Gecmis_Davet_Reddedilir`, `Iptal_Edilmis_Davet_Reddedilir`, `Gecersiz_Jeton_Kayit_Reddedilir_...` | cmp birebir |
| M-F2-04b | Boş/eksik jeton kapısı kaldırıldı | `Davetsiz_Kayit_403_ve_Hicbir_Tenant_Olusmaz` | cmp birebir |
| M-F2-05 | `kullanildi_mi = true` → `false` | `Kullanilmis_Davet_Ikinci_Kez_Kabul_Edilmez`, `Es_Zamanli_...`, `Gecerli_Davet_Tenant_DENEME_...` | cmp birebir |
| M-F2-06 | Tüketim koşulu `&& !d.KullanildiMi` kaldırıldı (yarış koruması) | `Es_Zamanli_Iki_Kayit_Ayni_Jetonla_Yalniz_Birini_Basarir` | cmp birebir |
| M-F2-07 | `SistemAnahtariKapisi`: "anahtar yoksa geç" davranışı | `Varsayilan_DEGISTIR_Anahtari_Ucu_Kapatir` | cmp birebir |
| M-F2-08 | `FixedTimeEquals` → `gelen == beklenen` | **testler yeşil kaldı** → `dotnet/denetim.sh` sayacı kırmızı: `✗ F-2-min: anahtarda '==' karşılaştırması 1 (0 olmalı)` | cmp birebir |
| M-F2-09 | Ham jeton `olusturan_not` kolonuna yazıldı | `Davet_Uretilir_Ham_Jeton_Bir_Kez_Doner_Ve_Listede_GORUNMEZ`, `PATCH_Govdede_Olmayan_Alan_Korunur` — **ama** `Ham_Jeton_...Saklanmaz` YEŞİL kaldı = **test açığı** | cmp birebir |
| M-F2-09b | Aynı mutasyon, test güçlendirildikten sonra | `Ham_Jeton_Veritabaninda_Saklanmaz_Yalniz_Hash_Vardir` | cmp birebir |
| M-F2-10 | PATCH'te `NotGonderildi` kontrolü kaldırıldı (kör yazma) | `PATCH_Govdede_Olmayan_Alan_Korunur` | cmp birebir |
| M-F2-11 | `Isletme.razor`: `disabled` koşulundan `SaltOkunur` çıkarıldı | `SaltOkunur_Iken_Kaydet_Dugmesi_Disabled_Ve_Neden_Yazili`, `Deneme_suresi_dolmus_kiracida_da_yazma_kilitlidir` | cmp birebir |
| M-F2-12 | Durum beyaz listesinde `_ => false` → `_ => true` | `Bilinmeyen_Durum_Degeri_Yazmayi_KAPATIR`, `SaltOkunur_Kiraci_Satis_POST_403`, `Durum_beyaz_listesi_bilinmeyeni_KAPATIR` | cmp birebir |
| M-F2-13 | Operatör başlığı kontrolü kaldırıldı | `Operator_Basligi_Olmadan_Mutasyon_400` | cmp birebir |
| M-F2-14 | Servis içi gerekçe (≥20) kontrolü kaldırıldı | **hiçbir test kırılmadı → TEST AÇIĞI** (DataAnnotations yalnız controller yolunda koşar) | cmp birebir |
| M-F2-14b | Aynı mutasyon, servisi DOĞRUDAN çağıran test eklendikten sonra | `Servis_DOGRUDAN_cagrildiginda_da_gerekce_zorunludur` ×3 | cmp birebir |
| M-F2-15 | `onbellek.Bosalt(kiraci.Id)` silindi | `AKTIF_ten_SALT_OKUNUR_a_gecis_ANINDA_uygulanir`, `SureUzatma_PATCH_DenemeBitisi_Ileri_Alir_Ve_Yazma_Yeniden_Acilir` | cmp birebir |

**İki test açığı bulundu ve kapatıldı** (M-F2-09 → 09b, M-F2-14 → 14b).
Son mutasyondan sonra TEMİZ derleme: `0 Hata · 0 Uyarı`.

## 6. denetim.sh

```
./dotnet/denetim.sh  →  DENETİM TEMİZ.

  ✓ ExecuteUpdate/Delete (beyaz liste 5)           5
  ✓ F-2-min: sistem anahtarı FixedTimeEquals       1
  ✓ F-2-min: anahtarda '==' karşılaştırması        0
  ✓ F-2-min: appsettings Sistem:Anahtari           yer tutucu
  ✓ F-2-min: üretim şablonunda Sistem:Anahtari     yok
  ✓ F-2-min: kapı muafiyetinde geniş önek          0
  ✓ F-2-min: 'SISTEM' rolü (açılmadı)              0
```

Beyaz liste 4 → 5 değişikliği **gerekçelidir**: davet tüketimi ırk-güvenli
koşullu `UPDATE` ister; `kiraci_davetleri` **IKiraciVarlik değildir**, yani
yazma denetçisi ve denetim izi o tabloya zaten işlemez. Betikte gerekçe
yazılıdır.

## 7. Canlı doğrulama (localhost:5198, kuyumcu_net)

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` davetsiz | 403, `tenants` değişmez | **403**; `tenants` 8 → 8 |
| 2 | `POST /sistem/davetler` anahtarsız | 403 | **403** `{"message":"Sistem yönetimi anahtarı geçersiz."}` |
| 3 | `POST /sistem/davetler` doğru anahtar, operatör başlığı YOK | 400 | **400** `X-Sistem-Operator başlığı zorunludur...` |
| 4 | `POST /sistem/davetler` (anahtar + operatör) | 201, ham jeton bir kez | **201**; `jeton`, `email:"p***@o***.test"`, `durum:"BEKLIYOR"` |
| 5 | SQL: `jeton_hash`, ham jeton araması | hash dolu, ham jeton hiçbir kolonda yok | `262d5f63…66ca`, `kullanildi_mi=0`; ham jeton araması **0 satır** |
| 6 | Davetle `POST /auth/kayit` | 201, kiracı DENEME + 14 gün | **201**; `durum=DENEME`, `deneme_bitis=2026-10-01`, `davet_id` dolu; davet `kullanildi_mi=1`, `kullanan_tenant_id` = yeni kiracı; `tenants` 8 → 9 |
| 7 | `UPDATE tenants SET deneme_bitis = NOW() - INTERVAL 1 DAY` → `POST /urunler` | 403 | **403** "Deneme süreniz doldu: kayıtlarınız duruyor…" |
| 8 | Aynı kiracıda `GET /urunler` | 200 | **200** `[]` |
| 9 | `POST /disa-aktarim` + `/{jeton}/indir` | 200 | **200** ve **200 (2 728 bayt)**; `file` → `Zip archive data … AES Encrypted` |
| 10 | `PATCH /sistem/kiracilar/<id>` (denemeBitis +30 gün, gerekçe) | 200 | **200** `kalanGun:30`, `yazabilirMi:true`, `degisenAlanlar:["deneme_bitis"]` |
| 11 | Uzatmadan hemen sonra `POST /urunler` | 201 (önbellek gecikmesi YOK) | **201** |
| 12 | `SELECT … denetim_kayitlari … detay LIKE 'SURE_UZATMA%'` | uzatma başına 1 satır | **1**; `kullanici_id IS NULL = 1`; detay: `SURE_UZATMA · operatör: taskin-backend · alanlar: deneme_bitis · gerekçe: …` |
| 13 | `durum=AKTIF` → `POST /urunler` | 201 | **201** |
| 14 | `durum=SALT_OKUNUR` → POST / GET / dışa aktarım | 403 / 200 / 200 | **403 / 200 / 200** |
| 15 | Mevcut kiracılar migration sonrası | hepsi AKTIF | `AKTIF 8 · DENEME 1` (DENEME olan = test kiracısı) |
| 16 | API `Sistem__Anahtari` **olmadan** yeniden başlatıldı → `GET /sistem/kiracilar` | fail-closed | **503** `Sistem yönetimi uçları bu kurulumda kapalıdır` |

**Temizlik:** API durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**),
5199 (kurucunun inceleme sunucusu) **hiç dokunulmadı** ve hâlâ dinliyor. Test
kiracısı `tenant_id` kolonu olan **48 tablodan** ve `tenants`'tan silindi,
ilişkili davetler silindi:

```
SELECT COUNT(*) FROM tenants;                       → 8   (başlangıç değeri)
SELECT COUNT(*) FROM kiraci_davetleri;              → 0
SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKETF2-TEST%';  → 0
SELECT COUNT(*) FROM tenants WHERE ad LIKE 'QA-TEST-%';      → 3   (fikstür KORUNDU)
```

## 8. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Pasife alma / geri dönüş | Yetki | Denetim | Ekran |
|---|---|---|---|---|---|
| `KiraciDaveti` (YENİ) | `PATCH /api/v1/sistem/davetler/{id}` — üç durumlu (gönderilmeyen alan korunur, null siler) | `iptal:true` ↔ `iptal:false` (**geri dönüş yolu var**); **hard DELETE YOK** | `SistemAnahtariKapisi` + zorunlu `X-Sistem-Operator` | Ekranda değil, uçta: davet mutasyonları anahtar + operatör başlığıyla korunur | **Faz B (dalga 6)** — bu dalgada yönetim `curl` ile, runbook `docs/RUNBOOK-PILOT-DAVET.md` |
| `Tenant.durum` / `deneme_bitis` / `davet_id` (YENİ alanlar) | `PATCH /api/v1/sistem/kiracilar/{id}` (gerekçe ≥20 karakter) | `aktif:false` ile pasife alma; `durum` ile DENEME↔AKTIF↔SALT_OKUNUR; **kiracı SİLİNMEZ** | `SistemAnahtariKapisi` + `X-Sistem-Operator` | **`DenetimServisi.SistemYazmaKaydet`** (mevcut `DenetimKaydi.Olustur` fabrikası; yeniden yazılmadı) | `/isletme` bandı durumu **gösterir** (salt okunur); değiştirme yolu **bilinçli olarak kiracıda DEĞİL** |
| `Tenant` kimlik alanları (mevcut) | `PATCH /api/v1/admin/tenant/kimlik` (değişmedi) | — | `[RolGerekli("SAHIP")]` | mevcut | `/isletme` |

**Neden `tenants.durum` kiracıya salt okunur:** abonelik/deneme süresi
kiracının **kendi kendine uzatabileceği** bir alan olamaz; düzeltme yolu
sistem yöneticisinin ucudur ve ekranda bu **yazılıdır**
(`Dil.T.Isletme.SureUzatmaNotu`: "Süre uzatımı yalnız sistem yöneticisi
tarafından yapılır."). `DuzeltmePolitikasi` haritasında `Tenant`
DUZENLENEBILIR kalır (kimlik alanları), `KiraciDaveti` DUZENLENEBILIR olarak
eklendi; ikisi de gerçek uca işaret ediyor (yansımalı test bunu tarıyor).

**Finansal varlıkta hard DELETE:** bu pakette hiçbir finansal varlığa DELETE
ucu açılmadı; yeni tabloda da DELETE yok.

## 9. DoD EK (kurul tur 1) karşılanması

1. **Her düğme ya istek atar ya disabled + sebep:** `/isletme` ekranında
   salt-okunur iken `#i-kimlik-duzenle`, `#s-yeni-sube-ac`, `#i-plan`,
   `#i-varsayilan-arayuz-modu`, AI ve vezne anahtarları, şube düzenle/pasife al
   **disabled**; sebep `#i-yazma-kilidi` içinde yazılı (UX-S30). bUnit ile
   sınandı; mutasyon M-F2-11 bunu kanıtladı.
2. **Tek düzenleme kalıbı:** yeni `ModalPencere` düzenleme kullanımı
   **açılmadı** (bu pakette hiçbir yeni düzenleme ekranı yok); `Cekmece`
   kalıbına dokunulmadı. `denetim.sh` sayaçları TEMİZ.
3. **Sessiz etkisizlik yok:** kayıt formuna **davet kodu alanı** eklendi
   (`Giris.razor`, `#kayit-davet`, `form.Zorunlu` + `AlanHatasi`) — alan
   olmasaydı form sunucuda sessizce 403 alırdı.
4. **Yıkıcı eylem + geri dönüş:** davet iptali geri alınabilir
   (`{"iptal":false}`); kiracı pasife alma `aktif:true` ile geri alınır.

## 10. Sapmalar ve gerekçeleri

| # | Tasarım metni | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | `KayitDto.DavetJetonu` **[Required]** | `[Required]` **konmadı**; doğrulama serviste, tek tip **403** | `[Required]` eksik alana 400 (alan hatası), geçersiz jetona 403 verirdi → uç bir **jeton doğrulama oracle'ı** olurdu. İş kuralı ("davetsiz istek 403, tek tip mesaj") üstün tutuldu. Ekranda alan yine zorunlu. |
| S2 | Yer tutucu anahtarda "**503/403**" | **503** seçildi | "Yanlış anahtar" (403) ile "bu kurulumda uç kapalı" (503) farklı şeylerdir; ikisini ayırmak yöneticinin sorunu teşhis etmesini sağlar, saldırgana ek bilgi vermez (anahtar zaten yok). |
| S3 | `SELECT COUNT(*) … WHERE varlik='TENANT' AND islem='SURE_UZATMA'` | `varlik_turu='Tenant' AND detay LIKE 'SURE_UZATMA%'` | `denetim_kayitlari` tablosunda **`islem` diye bir kolon yok** (`eylem` = EKLEME/GUNCELLEME…). İşlem adı detayın **başına** yazıldı, SQL ile süzülebiliyor. Sabitler: `SistemServisi.DenetimVarligi` / `DenetimIslemi`. |
| S4 | Faz A dosya ayak izinde `Giris.razor` **yok** | `Giris.razor` değiştirildi (davet kodu alanı) | Kayıt ucu kilitlenince **var olan kayıt formu sessizce kırılırdı** (DoD EK md.3 ihlali). Bu bir kapsam genişlemesi değil, kilidin zorunlu tamamlayıcısıdır. Tek dokunuş kaydı § 12'de. |
| S5 | Faz A ayak izinde `Meta/DuzeltmePolitikasi.cs` yok | Eklendi (`KiraciDaveti` kaydı) | Yeni bir `DbSet` eklemek `Her_varlik_haritada_VARDIR` testini **zorunlu olarak** kırar; harita güncellenmeden paket derlenip geçemez. |
| S6 | Faz A ayak izinde `Data/DenetimIzi.cs` yok | `SistemYazmaKaydet` eklendi | Mevcut `YazmaKaydet` **`AktifKullanici` ister**; sistem ucunda kullanıcı yoktur ve uydurulamaz (`kullanici_id` "sistem işlemlerinde null" diye belgelenmiş). Kayıt üretimi **yeniden yazılmadı**, aynı `Olustur` fabrikası kullanıldı. |
| S7 | Faz A testlerinde `SistemKiracilar_*` bUnit testleri | **Yazılmadı** | Faz B dalga 6'ya alındı (MÜHÜR 4 / K5); ekranı olmayan testin yazılması mümkün değil. Dosyada gerekçe yorumu var. |
| S8 | `Es_Zamanli_…` testi HTTP üzerinden | Servis DI kapsamından, `Task.WhenAll` ile **5 paralel**, gerçek MySQL | Kayıt ucu **IP başına 3/saat** oran sınırlıdır; 5 paralel HTTP isteği 429'a takılır ve sınanan şey yarış olmaktan çıkardı. Yarış **veritabanı düzeyindedir**. |

## 11. Birleşik / mühür koşullarının karşılanması

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) `SistemKapsami.cs` "tam liste" yorumu genişletilmeden `Sistem()` çağrılmaz | **KARŞILANDI** | Liste 7. ve 8. maddelerle genişletildi + "8 maddelik liste" özeti güncellendi (`SistemKapsami.cs:23-33,39`) |
| (2) `KiraciDurumKapisi` YENİ mekanizma olarak belgelenir; MVC dışı kapsam listelenir | **KARŞILANDI** | `KiraciDurumKapisi.cs` başlık bloğu: üç desen farkı + kapsam dışı yollar (`/saglik`, `/saglik/hazir`, `/hubs/kur`, `UseStaticFiles`, `MapFallback`) |
| (3) İkinci `AddControllers` açılmaz | **KARŞILANDI** | `Program.cs` tek `.AddControllers(o => o.Filters.Add<KiraciDurumKapisi>())` |
| (4) Muafiyet segment sınırıyla; `/api/v1` testle yasak | **KARŞILANDI** | `MuafMi()` + 10 test (`Muafiyet_listesi_GENIS_onek_ICERMEZ`, Theory ×9) + `denetim.sh` sayacı + M-F2-02 |
| (5) `TenantGetir` entity dönüşü kaldırılır, açık `TenantDto` | **KARŞILANDI** | `AdminModulu.cs` `TenantDto` + `Yansit()`; mevcut alan adları değişmedi, yeni alanlar eklendi |
| (6) `BasitMenu.cs` ayak izinden çıkarılır, menüye eklenmediği testle kanıtlanır | **KARŞILANDI** | `BasitMenu.cs` **değiştirilmedi**; test `Sistem_konsolu_MENUYE_eklenmedi` (BasitMenu + `KenarCubugu.razor` taranıyor) |
| (7) Anahtar yalnız ortam değişkeni; appsettings'te DEGISTIR-; FixedTimeEquals; placeholder sayacı | **KARŞILANDI** | `appsettings.json` `"Anahtari":"DEGISTIR-…"`; canlı 503 (madde 16); `denetim.sh` 4 yeni sayaç; M-F2-08 |
| (8) Sistem yöneticisi kimliği kod öncesi karara bağlı — (A) yapılandırma sırrı | **KARŞILANDI** | `AktifKullanici.cs`, `[RolGerekli]` listeleri, `BasitMenu.cs`, `KenarCubugu.razor` **hiç değişmedi**; test `SISTEM_rolu_ACILMADI_rol_hiyerarsisi_DEGISMEDI` + `denetim.sh` sayacı |
| (9) Eşzamanlılık gerçek paralel ≥5; AKTİF→SALT_OKUNUR yön testi; migration varsayılan testi; bilinmeyen durum üretimi yazılı | **KARŞILANDI (migration varsayılanı düzeltme turu 2'de kapandı: `Migration_tenants_durum_kolonuna_AKTIF_varsayilani_koyar`, mutasyon M-F2-19)** | `Es_Zamanli_…` (Task.WhenAll ×5, gerçek MySQL); `AKTIF_ten_SALT_OKUNUR_a_gecis_ANINDA_uygulanir` + `Onbellek_omru_dolunca_…`; **migration varsayılanı** için `AKTIF_Kiraci_Tum_Mutasyonlar_Etkilenmez` + `TestKurulum.KiraciKur` ile doğan kiracının varsayılanı (aşağıdaki açık madde) ; `Bilinmeyen_Durum_Degeri_Yazmayi_KAPATIR` içinde "nasıl üretilir" yorumu |
| (10) sessionStorage Faz B'ye; MFA takvim kapısı; PII maskeleme; canlı temizlik; tek katman riski handoff'a | **KARŞILANDI (düzeltme turu 2)** | Faz B ertelendi; PII **maskelendi** (`p***@o***.test`, test var); canlı temizlik yapıldı ve doğrulandı (§ 16.5); **tek katman riski + K4 MFA takvim kapısı + T-1 nginx IP izin listesi `~/Desktop/ByGatech/hafiza/SESSION-HANDOFF.md` sonuna yazıldı** (düzeltme turu 2) |
| K3 (Mühür 4) — `X-Sistem-Operator` zorunlu, başlıksız 400 **+ gerekçe ≥20 + istek kimliği + IP → DenetimServisi kaydı** | **KARŞILANDI (düzeltme turu 2'de tamamlandı — § 16)** | ⚠ İLK TURDA EKSİKTİ: iz yalnız `KiraciGuncelle`'de vardı; `DavetUret` ve `DavetGuncelle` **hiçbir iz bırakmıyordu** (hasım doğrulayıcının KRİTİK bulgusu, haklı). Şimdi: `SistemServisi.SistemIziYaz` (tek yol) üç mutasyonun **hepsinde**; testler `Davet_Uretimi_Denetim_Izi_Birakir`, `Davet_Iptali_Ve_Geri_Alinmasi_Denetim_Izi_Birakir`, `SureUzatma_Denetim_Kaydi_Yazar`; mutasyonlar M-F2-16/17/18; canlı SQL § 16.4 (3 iz satırı, IP `::1`, `kullanici_id NULL`) |
| K6 — runbook `docs/` altında, ikinci kök yok | **KARŞILANDI** | Mevcut `docs/RUNBOOK-PILOT-DAVET.md` **dolduruldu**; yeni dosya/kök açılmadı |
| MÜHÜR 2 — ücretsiz pilot, fiyat metni yok | **KARŞILANDI** | Ne kodda ne runbook'ta fiyat/teklif/tahsilat metni var |

### Migration varsayılanı için otomatik test — dürüst durum (ARTIK KAPALI, § 16.2)

> ⚠ Aşağıdaki paragraf **ilk turun** durumunu anlatır; düzeltme turu 2'de
> `Migration_tenants_durum_kolonuna_AKTIF_varsayilani_koyar` testi eklendi ve
> boşluk kapandı. Tarihsel kayıt olarak bırakılmıştır.

Koşul "migration varsayılan durum='AKTIF' için otomatik entegrasyon testi"
istiyordu. Uygulanan: test şeması **migration ile** kurulur
(`TestVeritabani.SemayiBirKezKur` → `Database.Migrate()`), ve
`AKTIF_Kiraci_Tum_Mutasyonlar_Etkilenmez` bu şemada doğan kiracının
yazabildiğini sınar. **Ancak** `TestKurulum.KiraciKur` `Durum` alanını EF
varsayılanıyla (`KiraciDurumlari.Aktif`) yazar; yani test **kolon
varsayılanını** değil, **CLR varsayılanını** kanıtlar. Veritabanı düzeyindeki
`DEFAULT 'AKTIF'` yalnız **canlı doğrulama madde 15** ile kanıtlandı
(kuyumcu_net: 8/8 AKTIF). Bu boşluk § 13'te açık kalem olarak duruyor.

## 12. Tek dokunuş (MÜHÜR 2) — dokunulan sayfalar

| Sayfa | Bu dalgada ilk mi? | Gerekçe |
|---|---|---|
| `Kuyumcu.Web/Pages/Isletme.razor` | Dalga 3'te **ilk** dokunuş | Deneme/salt-okunur bandı + yazma kilidi (paket ayak izinde var) |
| `Kuyumcu.Web/Pages/Giris.razor` | Dalga 3'te **ilk** dokunuş | Kayıt formuna davet kodu alanı — kilidin zorunlu tamamlayıcısı (sapma S4). Ayak izinde yoktu; **gerekçe bu satırdır**. |

Başka hiçbir `.razor` sayfası açılmadı.

## 13. Açık kalanlar

1. ~~**Migration kolon varsayılanı için CI testi yok**~~ → **KAPANDI** (düzeltme
   turu 2): `KiraciDurumKapisiTestleri.Migration_tenants_durum_kolonuna_AKTIF_varsayilani_koyar`
   hem `information_schema` iddiası (NOT NULL + `COLUMN_DEFAULT='AKTIF'`) hem de
   `durum` kolonu VERİLMEDEN ham SQL INSERT davranış iddiası yapar;
   `TestKurulum.KiraciKur` imzası değişmedi.
2. ~~**Tek katman koruma riski handoff'a yazılmadı**~~ → **KAPANDI** (düzeltme
   turu 2): `~/Desktop/ByGatech/hafiza/SESSION-HANDOFF.md` sonuna "F-2-min —
   KABUL EDİLEN RİSK" bloğu yazıldı (tek katman + K4 MFA takvim kapısı + T-1
   nginx IP izin listesi).
3. **45-API (MFA) takvim kapısı:** `/api/v1/sistem/*` uçları kodda hazır ama
   **üretime açılmamalıdır** (Mehmet veto yetkili). T-1 nginx şablonundaki
   `deny all` blokunun IP izin listesi kurulum anında doldurulmalıdır.
4. **Faz B** (`/sistem/kiracilar` konsolu): dalga 6, T-4b yanı; canlı öncesi
   ayrı CSP/XSS onayı şart, `localStorage` yasak (K7-K8).
5. **`GET /sistem/davetler` erişim logu:** e-posta **maskelendi**; ayrıca
   erişim logu tutulmuyor. Maskeleme Mehmet'in koşulunu ("maskeleme **ya da**
   erişim logu") karşılıyor, ama Faz B'de liste ekranı gelince erişim izi de
   değerlendirilmeli.
6. ~~**Davet üretim oran sınırı `OranSiniri.Kayit`**~~ → **DEĞİŞTİ** (düzeltme
   turu 2, canlı doğrulamada ölçülen gerçek arıza): ayrı `OranSiniri.SistemDavet`
   ilkesi açıldı (IP başına 20/saat). Ayrıntı § 16.3.

## 14. Kurucuya / mali müşavire notlar

* **Varsayılan deneme süresi 14 gün** bir **tasarım varsayımıdır**, ölçüm
  değil. `appsettings.json` → `Sistem:VarsayilanDenemeGun` ile değişir, koda
  gömülmedi (ADR-0005). **Kurucu/satış kararı bekliyor.**
* **KVKK md.11-12 / VUK 253/256:** deneme bitse bile okuma, giriş ve dışa
  aktarım açık bırakıldı; "veri rehin alma" riski bilinçli olarak kapatıldı.
  Hukuki nitelendirme **hukukçu görüşüyle doğrulanmalı** — kod bu görüşe göre
  değişmeyecek biçimde (kapı tek yerde, muafiyet listesi tek satır) yazıldı.
* **6502 s. TKHK / mesafeli sözleşmeler:** bu pakette **ücretlendirme ve
  otomatik yenileme YOKTUR**; deneme bitince yalnız yazma kapanır. Fiyat ilanı
  yapıldığı anda (F-1, dalga 8) bu madde yeniden değerlendirilmelidir.
* **Sistem anahtarı tek sırdır.** Sızarsa tüm kiracı listesi ve süre yönetimi
  açılır. Rotasyon prosedürü T-1 yayın paketinde tanımlıdır; anahtar
  **yalnız ortam değişkeninde** tutulmalı, kabuk geçmişine yazılmamalıdır
  (runbook'ta `read -rs` ile anlatıldı).

## 15. Değişen dosyalar

> `git status --porcelain` bu depoda tek commit olduğu için neredeyse tüm
> ağacı "yeni" gösteriyor (kullanışsız). Aşağıdaki liste **bu pakette elle
> dokunulan** dosyalardır.

**Yeni:**
* `dotnet/Kuyumcu.Api/Modules/Sistem/SistemModulu.cs`
* `dotnet/Kuyumcu.Api/Modules/Sistem/SistemAnahtariKapisi.cs`
* `dotnet/Kuyumcu.Api/Modules/Sistem/KiraciDurumKapisi.cs`
* `dotnet/Kuyumcu.Api/Data/Migrations/20260917194819_KiraciDenemeVeDavet.cs` (+ `.Designer.cs`)
* `dotnet/Kuyumcu.Tests/KiraciKayitKilidiTestleri.cs`
* `dotnet/Kuyumcu.Tests/KiraciDurumKapisiTestleri.cs`
* `dotnet/Kuyumcu.Tests/SistemUclariTestleri.cs`
* `dotnet/Kuyumcu.Tests/DenemeBandiEkranTestleri.cs`

**Değişen:**
* `dotnet/Kuyumcu.Api/Data/Entities.cs` · `KuyumcuDbContext.cs` ·
  `SistemKapsami.cs` · `DenetimIzi.cs` · `Migrations/KuyumcuDbContextModelSnapshot.cs`
* `dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs` ·
  `Modules/Admin/AdminModulu.cs` · `Modules/Meta/DuzeltmePolitikasi.cs`
* `dotnet/Kuyumcu.Api/Program.cs` · `appsettings.json`
* `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (TR + EN, 13 yeni anahtar)
* `dotnet/Kuyumcu.Web/Pages/Isletme.razor` · `Pages/Giris.razor`
* `dotnet/Kuyumcu.Tests/TestOrtami.cs` · `HttpOrtami.cs` · `AuthTestleri.cs` ·
  `DuzeltmePolitikasiTestleri.cs`
* `dotnet/denetim.sh`
* `docs/RUNBOOK-PILOT-DAVET.md`

**COMMIT YAPILMADI.** Çalışma ağacında bırakıldı.


---

## 16. DÜZELTME TURU 2 (2026-09-17) — hasım doğrulayıcı bulgularının kapatılması

Hasım doğrulayıcının üç bulgusu vardı. Hepsi **haklıydı**; aşağıda kanıtlarıyla
kapatıldı. Bu bölüm Anayasa md.1 gereği ilk turun yanlış "KARŞILANDI" beyanını
da açıkça düzeltir.

### 16.1 [KRİTİK] Davet üretimi ve iptali denetim izi bırakmıyordu (K3 ihlali)

**Doğrulandı:** `SistemModulu.cs`'te denetim çağrısı yalnız `KiraciGuncelle`
içindeydi. `DavetUret` sadece `db.Davetler.Add(...)` + `SaveChangesAsync()`,
`DavetGuncelle` ise `operatorAdi`yi **yalnız yanıt gövdesinde** döndürüyordu.
Otomatik yol da kapalıydı: `DenetimYazmaDinleyicisi` `tenantId` boş olan
girişleri `continue` ile atlar, `KiraciDaveti` ise (kasten) `IKiraciVarlik`
değildir. Sonuç: **yeni kiracı açma yetkisini üreten ayrıcalıklı eylemin izi
yoktu.**

**Seçilen yol (bulgudaki (a) seçeneği — şema değişikliği YOK):** kiracı-üstü iz
satırı. `SistemServisi.SistemTenantId = "SISTEM"` sabiti ile
`denetim_kayitlari.tenant_id` damgalanır. Gerekçe kodda yazılıdır: kiracı
kimlikleri ULID'dir (26 karakter), `"SISTEM"` hiçbir kiracıya eşit olamaz;
satır global kiracı filtresi yüzünden hiçbir kiracının denetim ekranında
görünmez. `KiraciYazmaDenetcisi` sistem bağlamında `TenantId`'nin **açıkça**
verilmesini zorunlu kılar — bu koşul sağlanır. **Migration gerekmedi.**

Ek olarak K3'ün ikinci yarısı da tamamlandı: `gerekce` (≥20 karakter) artık
`DavetUretDto` ve `DavetGuncelleDto` için de **zorunludur** (daha önce yalnız
`KiraciGuncelleDto`'daydı). Doğrulama tek yerde: `OperatorVeGerekceDogrula`
(DataAnnotations yalnız controller yolunda koştuğu için servis içi elle kontrol).
İz satırı: `<İŞLEM> · operatör: <ad> · <ek> · gerekçe: <metin> · istek: <TraceIdentifier>`;
IP ve user-agent `DenetimServisi.Olustur` damgasından gelir. **Ham jeton ve
sistem anahtarının değeri ize yazılmaz** (test + canlı SQL ile kanıtlı).

### 16.2 [ORTA] Migration kolon varsayılanı için otomatik test yoktu

Eklendi: `Kuyumcu.Tests/KiraciDurumKapisiTestleri.cs` →
`Migration_tenants_durum_kolonuna_AKTIF_varsayilani_koyar`. Test **migration ile
kurulan** test şemasında (`Database.Migrate()`):
1. `information_schema.COLUMNS` → `tenants.durum` için `COLUMN_DEFAULT='AKTIF'`
   ve `IS_NULLABLE='NO'`;
2. `durum` kolonu **verilmeden** ham SQL `INSERT` → okunan değer `AKTIF`
   (satır `finally` içinde silinir, fikstür kirlenmez).

### 16.3 [YENİ BULGU — canlı doğrulamada ölçüldü] Davet üretimi kayıt kotasına takılıyordu

Canlı doğrulamanın ilk turunda `POST /api/v1/sistem/davetler` **429** döndü.
Sebep: uç `OranSiniri.Kayit` ilkesindeydi (IP başına **3/saat**) ve bu bölüm
`POST /auth/kayit` ile **paylaşılıyordu**. Yani (a) aynı IP'den gelen sıradan
kayıt denemeleri yöneticinin davet üretmesini kilitleyebiliyordu, (b) gerekçesiz
/başlıksız **400 alan denemeler bile** kotayı yiyordu (sınırlayıcı kapıdan önce
koşar). Pilot runbook'u fiilen çalışmaz hâldeydi.

**Düzeltme:** `OranSiniri.SistemDavet` ("sistem-davet", IP başına 20/saat) ilkesi
açıldı ve yalnız davet üretim ucuna verildi. Kilit test:
`Davet_Ucu_Kayit_Oran_Kotasini_PAYLASMAZ` (mutasyon M-F2-20). Kayıt ucunun
3/saat kotası **değişmedi**.

### 16.4 Yeni testler ve mutasyon kanıtları

| Kod | Değişiklik (tek eşleşme) | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M-F2-16 | `DavetUret` içindeki `SistemIziYaz(...)` çağrısı silindi | `Davet_Uretimi_Denetim_Izi_Birakir` **ve** `Davet_Iptali_Ve_Geri_Alinmasi_Denetim_Izi_Birakir` | `cmp` birebir aynı |
| M-F2-17 | `DavetGuncelle` içindeki `SistemIziYaz(...)` çağrısı silindi | `Davet_Iptali_Ve_Geri_Alinmasi_Denetim_Izi_Birakir` | `cmp` birebir aynı |
| M-F2-18 | `OperatorVeGerekceDogrula`'daki gerekçe uzunluk kontrolü silindi | `Servis_DOGRUDAN_cagrildiginda_da_davet_gerekcesi_zorunludur` + `Servis_DOGRUDAN_cagrildiginda_da_gerekce_zorunludur` (3 InlineData) | `cmp` birebir aynı |
| M-F2-19 | Migration'da `defaultValue: "AKTIF"` kaldırıldı | `Migration_tenants_durum_kolonuna_AKTIF_varsayilani_koyar` | `cmp` birebir aynı |
| M-F2-20 | Davet ucunun ilkesi `SistemDavet` → `Kayit` | `Davet_Ucu_Kayit_Oran_Kotasini_PAYLASMAZ` | `cmp` birebir aynı |

Yeni testler (6 adet): `Davet_Uretimi_Denetim_Izi_Birakir`,
`Davet_Iptali_Ve_Geri_Alinmasi_Denetim_Izi_Birakir`,
`Gerekcesiz_Davet_Uretimi_400_Ve_Kayit_Dogmaz`,
`Servis_DOGRUDAN_cagrildiginda_da_davet_gerekcesi_zorunludur`,
`Davet_Ucu_Kayit_Oran_Kotasini_PAYLASMAZ`,
`Migration_tenants_durum_kolonuna_AKTIF_varsayilani_koyar`.

**Tam süit (düzeltme turu 2 sonrası):** `Toplam test sayısı: 2392 · Geçti: 2392`
(3. koşum, trx `scratchpad/trx-pF-2-min-son2`).
2. koşumda **1 kararsız kırmızı** çıktı ve **gizlenmedi**:
`UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir`
— `Bunit.ElementNotFoundException: #alan-hata-islem-adet`. Aynı test 1. koşumda
yeşil, tek başına koşturulduğunda yeşil, 3. koşumda yeşil; F-2-min ile ilgisi
yok (POS ekranı bUnit zamanlaması). **Kararsızlık kaydı olarak orkestratöre
bırakılıyor.**

**Derleme:** `0 Hata`. `4 Uyarı` çıkıyor, hepsi
`Kuyumcu.Tests/MadenTestleri.cs:78,79,80,83` (xUnit2008) — **bu pakette
dokunulmayan dosya**, uyarılar önceki dalgadan geliyor; artımlı derlemede
görünmedikleri için önceki kapı kayıtlarında "0 uyarı" yazmış olabilir.
`dotnet/denetim.sh` → **DENETİM TEMİZ** (çıkış 0).

### 16.5 Canlı doğrulama (localhost:5198, kuyumcu_net, düzeltme turu 2)

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | Yer tutucu anahtarla (`Sistem__Anahtari` ortam değişkeni YOK) `GET /api/v1/sistem/kiracilar` | 503 fail-closed | **503** — "Sistem yönetimi uçları bu kurulumda kapalıdır" |
| 2 | Operatör başlıksız `POST /sistem/davetler` | 400 | **400** — "X-Sistem-Operator başlığı zorunludur…" |
| 3 | Gerekçesiz `POST /sistem/davetler` | 400 | **400** — `{"alanlar":{"gerekce":["Gerekçe zorunludur"]}}` |
| 4 | Tam istek `POST /sistem/davetler` | 201 + ham jeton bir kez | **201**, `id=01M2RGZWQFTNR9EFAB6TKTBASK`, `email=p***@p***.test` (maskeli) |
| 5 | Davetsiz `POST /auth/kayit` | 403 | **403** — tek tip mesaj |
| 6 | Davetli `POST /auth/kayit` | 201 + davet tüketilir | **201**, `tenantId=01M2RH03VR28G36Z51Y5WXW04J`; SQL: `kullanildi_mi=1`, `kullanan_tenant_id` = o kiracı |
| 7 | Aynı jetonla ikinci kayıt | 403 | **403** |
| 8 | `PATCH /sistem/kiracilar/{id}` → `SALT_OKUNUR` | 200 | **200**, `yazabilirMi=false`, `degisenAlanlar=["durum"]` |
| 9 | SALT_OKUNUR kiracıda `POST /api/v1/cari` | 403 | **403** — `"kiraciDurumu":"SALT_OKUNUR"` |
| 10 | Muafiyet: SALT_OKUNUR kiracıda `POST /api/v1/disa-aktarim` | kapı engellemez | **200** — talep açıldı (veri rehin alınmıyor) |
| 11 | Muafiyet: `GET /api/v1/cari` | 200 | **200** |
| 12 | `PATCH /sistem/davetler/{id}` iptal | 200 + `durum=IPTAL` | **200**, `operator_=mehmet-guvenlik` |

**SQL kanıtı (denetim izi):** `denetim_kayitlari WHERE tenant_id='SISTEM'` → **3
satır**: `DAVET_URETIMI · operatör: askin-qa …`, `DAVET_URETIMI · operatör:
tamer-patron …`, `DAVET_GUNCELLEME · operatör: mehmet-guvenlik · alanlar: iptal ·
yeni durum: IPTAL …`; hepsinde `kullanici_id NULL`, `ip_adresi ::1`,
`varlik_turu KiraciDaveti`. Ayrıca `SURE_UZATMA izi=1` (kiracının kendi
tenant_id'siyle). Sızıntı kontrolleri: `ham jeton izde=0`, `anahtar izde=0`,
`ham jeton davet tablosunda=0` (yalnız SHA-256 özeti).

**Temizlik:** API durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` → 0 satır); test
kiracısı ve tüm `tenant_id` taşıyan 48 tablodaki satırları, `SISTEM` iz satırları
ve `PAKETF2MIN-TEST%` davetleri silindi. Doğrulama: `tenants=8` (başlangıçtaki
değer), `davetler=0`, `SISTEM izi=0`, `PAKETF2MIN kalan=0`, **`QA fikstur=3`
(QA-TEST- kiracılarına dokunulmadı)**.

### 16.6 Düzeltme turunda değişen dosyalar

* `dotnet/Kuyumcu.Api/Modules/Sistem/SistemModulu.cs` — iz yazımı, `Gerekce`
  alanları, `SistemTenantId`/`SistemIziYaz`/`OperatorVeGerekceDogrula`, oran ilkesi
* `dotnet/Kuyumcu.Api/Common/OranSiniri.cs` — `SistemDavet` ilkesi (20/saat)
* `dotnet/Kuyumcu.Tests/SistemUclariTestleri.cs` — 5 yeni test + gerekçe alanları
* `dotnet/Kuyumcu.Tests/KiraciDurumKapisiTestleri.cs` — migration varsayılan testi
* `dotnet/Kuyumcu.Tests/KiraciKayitKilidiTestleri.cs` — davet gövdesine gerekçe
* `docs/RUNBOOK-PILOT-DAVET.md` — zorunlu `gerekce`, güncel curl örnekleri
* `~/Desktop/ByGatech/hafiza/SESSION-HANDOFF.md` — tek katman riski + K4 + T-1 IP listesi
