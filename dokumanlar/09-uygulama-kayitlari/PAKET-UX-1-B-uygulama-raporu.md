# PAKET UX-1-B — "Her veri girilen yer düzeltilebilir" Faz B (API boşlukları) — uygulama raporu

> Dalga 2 · Tarih: 2026-09-16 · Uygulayıcı: Taşkın (backend) + Ertuğ (sözleşme gözden geçirme) · Model: Opus
> Kapsam: YALNIZ sunucu uçları. **Hiçbir .razor sayfasına dokunulmadı** (ekran bağlama UX-1-C / dalga 2c).

## 1. Ne yapıldı (özet)

| # | İş | Sonuç |
|---|---|---|
| 1 | `PATCH /api/v1/reyonlar/{id}` + `DELETE /api/v1/reyonlar/{id}` (pasife alma) açıldı | ✅ |
| 2 | `PATCH /api/v1/admin/tenant/kimlik` (ad · adres · vergi dairesi · vergi no · telefon) + `IsletmeKimlikBilgileri` migration | ✅ |
| 3 | `KasaGuncelleDto` alan bazlı PATCH'e geçirildi (`Ad` artık [Required] DEĞİL) + `paraBirimi`/`subeId` + üç kaynaklı kapı | ✅ (Bertan KOŞUL 1 — BLOKLAYICI — kapandı) |
| 4 | `DELETE /api/v1/bankalar/{id}` = pasife alma + bakiye/hareket kapısı | ✅ |
| 5 | Düzeltme politikası haritası güncellendi: `Reyon` UC_YOK → DUZENLENEBILIR; `Tenant` hedef ucu kimlik ucuna çevrildi | ✅ |
| 6 | `dotnet/denetim.sh`'a "Finansal varlıkta [HttpDelete] (0 olmalı)" sayacı eklendi | ✅ |
| 7 | 45 yeni test (4 yeni dosya + politika testine 1 ek) | ✅ |

**Fiyat uçlarına DOKUNULMADI.** Mehmet K1'in istediği rol kapısı zaten yerinde: `FiyatModulu.cs:720,723,728,731,798,803,811` hepsi `[RolGerekli("SAHIP","MUDUR")]` taşıyor (paket 49/UX-4a ile geldi) ve sınıf düzeyinde `[Yetki.ModulYetkisi("urun")]` var. Doğrulandı, değiştirilmedi.

## 2. Sapmalar ve gerekçeleri

| Sapma | Spesifikasyon | Yapılan | Gerekçe |
|---|---|---|---|
| S1 | `PATCH /api/v1/admin/tenant` kimlik alanlarını taşısın | **`PATCH /api/v1/admin/tenant/kimlik`** ayrı rota | `/admin/tenant` ucu paket UX-3 Faz A ile `TenantArayuzModuDto`'ya bağlandı ve `VarsayilanArayuzModu` alanı `[Required]`. Kimliği oraya koymak, o ucun zorunlu alanını gevşetmeyi (UX-3 sözleşmesini bozmayı) ve tek gövdede iki sorumluluğu toplamayı gerektirirdi. Mehmet K2'nin "ayrı DTO, toplu atama yok" ilkesi ayrı rotayla daha güçlü karşılanır. `ArayuzModuTestleri` DEĞİŞTİRİLMEDEN yeşil. |
| S2 | Reyon şube değiştirme denemesi → **400** | Şube alanı DTO'da hiç yok → istek **200**, satır DEĞİŞMEZ | Mehmet K6 açıkça "DTO'ya koymamak yeter; test: subeId gönderilir → satır değişmez" diyor. İki koşul çelişiyordu; bağlayıcı olan K6 (Birleşik koşullar) uygulandı. Test `Sube_degistirilemez` hem 200'ü hem satırın değişmediğini doğruluyor. |
| S3 | Kasa para birimi/şube kapısı ihlalinde 409 | 409 (ConflictException) | Sapma yok; not: eski `DeaktifEt` bakiye kapısı 400 döndürüyor (BadRequestException) — o satıra DOKUNULMADI (mevcut davranış korundu), yeni kapı 409. |
| S4 | Reyon DELETE ürün kapısı | Aynı kapı `PATCH {aktif:false}` yolunda da çağrılır | Tek kural, iki giriş — ikinci mekanizma açılmasın diye (Bertan). Banka için de aynı desen. |

**Mevzuat/DOGRULANMALI hükmü kod içine sabitlenmedi:** vergi no biçimi `^\d{10,11}$` ile yalnız BİÇİM olarak doğrulanır; VKN/TCKN algoritmik doğrulaması YAPILMAZ (spesifikasyon böyle bir hüküm istemiyordu, varsayım üretilmedi — bkz. §9 notlar).

## 3. Migration

- Ad: `20260916142643_IsletmeKimlikBilgileri`
- Dosyalar: `dotnet/Kuyumcu.Api/Data/Migrations/20260916142643_IsletmeKimlikBilgileri.cs` (1989 bayt) + `.Designer.cs` (102.746 bayt) + güncel `KuyumcuDbContextModelSnapshot.cs`
- `Up()`: `tenants` tablosuna `adres varchar(500) NULL`, `telefon varchar(20) NULL`, `vergi_dairesi varchar(100) NULL`, `vergi_no varchar(11) NULL`. İndeks eklenmedi (tekil kiracı satırı; sorgulanmıyor).
- `kuyumcu_net`'e uygulandı (`dotnet ef database update`, build'li, `--no-build` KULLANILMADI). Kanıt:

```
SHOW COLUMNS FROM tenants → … varsayilan_arayuz_modu varchar(12) | adres varchar(500) | telefon varchar(20) | vergi_dairesi varchar(100) | vergi_no varchar(11)
```

Toplam migration sayısı: 25 → **26**.

## 4. Derleme ve denetim

- `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata** (son koşum, mutasyonlar geri alındıktan sonra).
- `dotnet/denetim.sh` → **DENETİM TEMİZ.** Yeni satır:

```
  ✓ ArayuzModu kapı yüzeyinde (0 olmalı)        0
  ✓ Finansal varlıkta [HttpDelete] (0 olmalı)    0
  ✓ CORS                                           beyaz liste
```

## 5. Testler (önce / sonra)

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Paket öncesi taban (hesaplanan: 1439 − 45) | 1394 | 1394 | 0 | 0 |
| Yeni testler (filtreli: 4 yeni dosya + DuzeltmePolitikasiTestleri) | 63 | 63 | 0 | 0 |
| **Tam süit (sonra)** | **1439** | **1439** | **0** | **0** |

trx: `…/scratchpad/trx-pUX-1-B/_Ramazan-MacBook-Air_2026-09-16_17_37_34.trx` → `total="1439" executed="1439" passed="1439" failed="0"`.

**Yeni test sayısı: 45**
- `Kuyumcu.Tests/ReyonDuzenlemeTestleri.cs` — 10
- `Kuyumcu.Tests/IsletmeBilgileriTestleri.cs` — 15 (2 Theory: 4 + 3 satır)
- `Kuyumcu.Tests/KasaDuzenlemeTestleri.cs` — 9
- `Kuyumcu.Tests/BankaPasifeAlmaTestleri.cs` — 10 (1 Theory: 3 satır)
- `Kuyumcu.Tests/DuzeltmePolitikasiTestleri.cs` — +1 (`Finansal_modullerde_HARD_DELETE_ucu_ACILMAMISTIR`)

Hepsi gerçek HTTP boru hattından geçer (`[Collection("http")]`) → `[RolGerekli]`/`[ModulYetkisi]` ve JSON bağlaması gerçekten sınanır; saf test yalnız yansıma tabanlı uç envanterinde.

## 6. Mutasyon kanıtları

Yöntem: kaynak `…/scratchpad/mut-yedek/UX1B/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir aynı olduğu kanıtlandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M-UX1-02 | Reyon PATCH'ine `SubeId` alanı + şube yazma eklendi (kapı kaldırıldı) | `ReyonDuzenlemeTestleri.Sube_degistirilemez` | CMP-OK-M02 |
| M-UX1-03 | Kasa `GecmisiBosMu` içindeki `kasa_hareketleri` kapısı silindi | `KasaDuzenlemeTestleri.Hareketli_kasada_para_birimi_degismez` | CMP-OK-M03 |
| M-UX1-04 | Tenant kimlik `Uygula`'daki "gövdede yok → KORUNUR" erken çıkışı kaldırıldı (yoksa null yaz) | `IsletmeBilgileriTestleri.Alan_govdede_yoksa_korunur` ×4 (adres/vergiNo/vergiDairesi/telefon) + `Null_gonderilen_alan_silinir` | CMP-OK-M04 |
| M-UX1-B-05 | `AdminServisi.KimlikGuncelle` içindeki `denetim?.YazmaKaydet(...)` çağrısı silindi | `IsletmeBilgileriTestleri.Kimlik_degisimi_DENETIM_KAYDI_yazar` + `Ayni_degerle_PATCH_denetim_kaydi_URETMEZ` | CMP-OK-M05 |
| M-UX1-B-06 | Reyon PATCH ucundan `[RolGerekli("SAHIP","MUDUR")]` kaldırıldı | `ReyonDuzenlemeTestleri.TEZGAHTAR_reyonu_DUZENLEYEMEZ_403` | CMP-OK-M06 |
| M-UX1-B-07 | Banka pasife alma yerine HARD DELETE (`db.BankaHesaplari.Remove`) | `BankaPasifeAlmaTestleri.Temiz_hesap_DELETE_ile_PASIFE_ALINIR_satir_DURUR` + `Pasif_hesap_PATCH_ile_GERI_ACILIR` | CMP-OK-M07 |
| M-UX1-B-08 | `FaturaController`'a `[HttpDelete("{id}")]` eklendi | `denetim.sh` → `✗ Finansal varlıkta [HttpDelete] (0 olmalı) 1 (beklenen 0)` **ve** `DuzeltmePolitikasiTestleri.Finansal_modullerde_HARD_DELETE_ucu_ACILMAMISTIR` | CMP-OK-M08 |
| M-UX1-B-09 | Reyon `PasifeAl` içindeki `UrunKapisi` çağrısı silindi | `ReyonDuzenlemeTestleri.Urunlu_reyon_DELETE_409_ve_satir_aktif_kalir` | CMP-OK-M09 |
| M-UX1-B-10 | `KasaGuncelleDto.Ad`'a `[Required]` geri kondu (Bertan KOŞUL 1 geri alma) | `KasaDuzenlemeTestleri` 6 test (`Ad_govdede_yoksa_KORUNUR_ve_400_ALINMAZ`, `Bakiyesiz_..._DEGISIR`, `Temiz_kasada_sube_TASINIR`, `Bakiyeli_kasada_sube_degismez`, `Hareketli_kasada_para_birimi_degismez`, `Kasa_PATCH_DENETIM_KAYDI_yazar`) | CMP-OK-M10 |

Son mutasyondan sonra TEMİZ derleme: `0 Uyarı, 0 Hata`; `denetim.sh` → TEMİZ.

**Anayasa md.1 — bir talimat literal olarak uygulanamadı:** brifteki "denetim.sh'a 'finansal varlıklarda [HttpDelete] = 0' sayacı eklenir (mutasyon: **sayaç kaldırılınca** denetim kırmızı)" maddesinin ikinci yarısı mantıken imkânsızdır: bir kapıyı SİLMEK o kapının ateşlenmesine yol açamaz (sayaç silinirse denetim yine TEMİZ der). Sayacın gerçekten koruduğu şey M-UX1-B-08 ile kanıtlandı: finansal modüle `[HttpDelete]` eklenince hem `denetim.sh` hem saf test kırmızıya döner. Sayaç ayrıca testle ÇİFTLENDİ ki betik dosyası bozulsa bile kapı test süitinde kalsın.

## 7. DoD sözleşmesi — varlık × düzeltme yolu

Kaynak: `Modules/Meta/DuzeltmePolitikasi.cs` (sunucudaki TEK harita). "Ekran eylemi" sütunu bu pakette AÇILMADI (UX-1-C dalga 2c) — ekransız kalan satırlar orada bağlanacak.

### 7.1 DUZENLENEBILIR (14 varlık)

| Varlık | PATCH ucu | Pasife alma / DELETE | Yetki | Denetim kaydı | Ekran eylemi |
|---|---|---|---|---|---|
| Tenant (işletme kimliği) | **PATCH /admin/tenant/kimlik (YENİ)** | yok (kiracı kapatma ayrı yönetim işi) | sınıf `[RolGerekli("SAHIP")]` | **YazmaKaydet "TenantKimlik" (YENİ)** | UX-1-C (Isletme.razor) |
| Sube | PATCH /subeler/{id} | DELETE /subeler/{id} (pasife alma) | SAHIP/MUDUR | otomatik yazma dinleyicisi | var |
| Kullanici | PATCH /kullanicilar/{id} | DELETE (pasife alma) | SAHIP/MUDUR | otomatik | var |
| Cari | PATCH /cari/{id} | DELETE (pasife alma) | rol kapılı | otomatik + düzeltme fişi | var |
| Urun | PATCH /urunler/{id} | DELETE (pasife alma) | urun modülü | otomatik | var (kart UX-1-C'de tamamlanacak) |
| Stok | PATCH /urunler/{id}/stok/{subeId} | — (türetilmiş) | urun modülü | otomatik | var |
| **Kasa** | PATCH /kasalar/{id} — **artık alan bazlı** (ad · para birimi · şube · aktif) | DELETE /kasalar/{id} (pasife alma) | PATCH SAHIP/MUDUR · DELETE SAHIP | **YazmaKaydet "Kasa" (alan adları)** | UX-1-C |
| **BankaHesap** | PATCH /bankalar/{id} | **DELETE /bankalar/{id} (YENİ — pasife alma)** | PATCH SAHIP/MUDUR · DELETE **SAHIP** | **YazmaKaydet "BankaHesap" (YENİ)** | UX-1-C (Banka.razor) |
| FiyatListesi | PATCH /fiyat/listeler/{id} | aktif=false | SAHIP/MUDUR | var (49) | var (UX-4a) |
| Kampanya | PATCH /fiyat/kampanyalar/{id} | aktif=false | SAHIP/MUDUR | var | var |
| FiyatListesiUrunFiyati | PATCH /fiyat/listeler/{id}/urun-fiyatlari/{urunId} | aktif=false | SAHIP/MUDUR | YazmaKaydet | var |
| UrunKategorisi | PATCH /urun-kategorileri/{id} | aktif=false (PATCH) | MUHASEBE+ | otomatik | var |
| RolYetki | PUT /yetkiler | — | SAHIP | otomatik | var |
| **Reyon** | **PATCH /reyonlar/{id} (YENİ)** | **DELETE /reyonlar/{id} (YENİ — pasife alma, ürünlüyse 409)** | SAHIP/MUDUR | **YazmaKaydet "Reyon" (YENİ)** | UX-1-C (UrunAgaci.razor) |
| Belge | PATCH/DELETE /belgeler/{belgeId} | pasife alma | urun modülü | var | var |

### 7.2 STORNO (defter — yerinde düzenleme YOK)

`Hareket`, `KasaHareket`, `BankaHareket`, `GiderPusulasi` → gerekçe anahtarı `DEFTER`; `Fatura` → `FATURA`.
Hareket türleri: SATIS/ALIS/CARI_DUZELTME/VERESIYE/TAHSILAT'ın ters kayıt yolu var (iade / düzeltme fişi); TAKAS, HURDA_ALIS, EMANET_GIRIS/CIKIS, TRANSFER, SAYIM_DUZELTME, ISCILIK, SATIS_IADE, ALIS_IADE → `STORNO_YOK` (motor paket 48).
**Gerekçe anahtarı:** ADR-0004 (append-only defter) + VUK md.219 (defter kayıtları silinemez/karalanamaz).

### 7.3 SALT_OKUNUR (gerekçesi yazılı)

| Varlık | Gerekçe anahtarı | Dayanak |
|---|---|---|
| DenetimKaydi | DENETIM | ADR-0021 append-only denetim izi |
| MasakKimlikTespiti · MasakBeyani · MasakSupheliBildirim | MASAK | 5549 s.K. yükümlülük kaydı |
| DisaAktarimTalebi | DISA_AKTARIM | KVKK md.11 kanıt kaydı |
| GunKapanisi | KAPANIS | sayım tutanağı kesinliği |
| SayimOturumu · SayimSatiri | SAYIM | kapanan sayım değişmez |
| KurSnapshot · RefreshToken · StokPartisi · StokPartiTuketimi | SISTEM | sistem üretir |
| CariBakiye | TURETILMIS | hareketlerden türetilir → düzeltme fişi (`/cari/{id}/duzeltme`) |
| FaturaKalemi | FATURA | VUK md.253 · düzeltme yolu faturanın durumu |

**`UC_YOK` damgalı varlık KALMADI** (tek örnek Reyon'du, bu paketle kapandı). Anahtar sözlükte rezerv olarak duruyor.

### 7.4 Finansal varlıkta hard DELETE — 0

`hareketler`, `kasa_hareketleri`, `banka_hareketleri`, `faturalar`, `gider_pusulalari`, `gun_kapanislari`, `denetim_kayitlari`, `masak_*`, `sayim_*` üzerinde `[HttpDelete]` **yoktur** ve iki kapıyla korunur: `denetim.sh` sayacı + `DuzeltmePolitikasiTestleri.Finansal_modullerde_HARD_DELETE_ucu_ACILMAMISTIR`. Mevcut 9 `[HttpDelete]` ucunun tamamı `aktif=0` yazar (satır silmez).

## 8. Canlı doğrulama (localhost:5198, gerçek MySQL `kuyumcu_net`)

Test kiracısı: **PAKETUX-1-B-TEST Kuyumcu** (`01M2NAG7KWM388ZVRG8XRRJ4AX`). Başlangıç `tenants` sayısı: **7**.

| # | Adım | Beklenen | Gerçekleşen | SQL kanıtı |
|---|---|---|---|---|
| 1 | POST /reyonlar | 201 | **201** | satır oluştu |
| 2 | PATCH reyon `{ad, tip}` | 200 + satır değişti | **200** | `ad=Pırlanta vitrini, tip=KASA, aktif=1` |
| 3 | PATCH reyon `{subeId: <2. şube>}` | satır DEĞİŞMEZ | **200**, şube aynı | `sube_id=01M2NAG7KW57T7GFJ5KQJGBG66` (eski) |
| 4 | DELETE reyon (1 ürün bağlı) | 409 + aktif kalır | **409** `"Bu reyonda 1 ürün var…"` | `aktif=1` |
| 5 | Ürün başka reyona taşındı → DELETE | 200, satır DURUR | **200** `{"mesaj":"Reyon pasife alındı","aktif":false}` | `aktif=0`, satır duruyor |
| 6 | PATCH reyon `{aktif:true}` | geri açılır | **200** | `aktif=1` |
| 7 | PATCH /admin/tenant/kimlik (5 alan) | 200 | **200** | `ad, adres, vergi_dairesi=Bayrampaşa, vergi_no=1234567890, telefon=02125550101` |
| 8 | GET /admin/tenant | birebir aynı | **birebir** | — |
| 9 | PATCH yalnız `{telefon}` | ad + vergi no KORUNUR | **200** | `ad=PAKETUX-1-B-TEST Kuyumculuk A.Ş. | adres=Kuyumcukent No:12 | vergi_no=1234567890 | telefon=02129998877 | plan=BASLANGIC` |
| 10 | PATCH `{vergiNo:"123"}` | 400 | **400** `{"alanlar":{"vergiNo":["Vergi no 10 ya da 11 hane olmalıdır"]}}` | satır değişmedi |
| 11 | Kimlik denetim izi | 2 satır, DEĞER yok | **2 satır** | `EKLEME TenantKimlik "…değişen alanlar: ad, adres, vergi_dairesi, vergi_no, telefon"` ve `"…: telefon"`, `hassas=1` |
| 12 | Kasa aç → PATCH `{paraBirimi:"USD"}` (ad GÖVDEDE YOK) | 200, ad korunur | **200** | `ad=UX1B kasa, para_birimi=USD, bakiye=0.00` |
| 13 | 2 kasa hareketi (GIRIS 100 + CIKIS 100) → bakiye 0 | — | **200/200** | `bakiye=0.00`, `kasa_hareketleri=2` |
| 14 | Hareketli kasada `{paraBirimi:"TRY"}` | 409, satır değişmez | **409** `"Hareket görmüş kasanın para birimi ya da şubesi değiştirilemez — yeni kasa açın"` | `para_birimi=EUR` (değişmedi) |
| 15 | Aynı kasada yalnız `{ad}` PATCH | 200 (kapı ada takılmaz) | **200** | `ad=UX1B kasa yeni ad, para_birimi=EUR` |
| 16 | Banka hesabı + 250 TL dekont → DELETE | 409 | **409** `"Bakiyesi sıfır olmayan banka hesabı kapatılamaz"` | `bakiye=250.00, aktif=1` |
| 17 | Temiz 2. hesap → DELETE | 200, satır DURUR | **200** | `Vakıf | aktif=0` (satır duruyor) |
| 18 | GET /meta/duzeltme-politikalari | Reyon DUZENLENEBILIR | **DUZENLENEBILIR / SERBEST / api/v1/reyonlar/{id}**; Tenant → `api/v1/admin/tenant/kimlik` | — |
| 19 | API durduruldu (yalnız kendi PID) | 5198 boş | **`lsof -nP -iTCP:5198 -sTCP:LISTEN` → 0 satır** | — |
| 20 | Test kiracısı 32 tablodan + `tenants`'tan silindi | sayı 7'ye döner | **7** | `SELECT COUNT(*) FROM tenants → 7`; `reyonlar` kiracı satırı 0 |

**QA-TEST-1/QA-TEST-2 kiracılarına DOKUNULMADI** (son listede ikisi de duruyor). 5199 portuna hiç dokunulmadı.

Yan gözlem (bu paketin kapsamı dışı, kayda geçiyor): `PATCH /urunler/{id}` gövdesinde `reyonId: null` göndermek ürünün reyon bağını TEMİZLEMİYOR (null = "dokunma" olarak işleniyor). Ürünü başka reyona taşıyarak ilerlendi. Bu, ürün PATCH'inin üç-durum sözleşmesindeki bir boşluktur → §10 açık kalanlar.

## 9. Değişen dosyalar

```
M dotnet/Kuyumcu.Api/Data/Entities.cs                       (Tenant kimlik kolonları)
M dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs           (TenantKimlikGuncelleDto + KimlikGuncelle + uç)
M dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs           (DELETE = pasife alma + kapı + denetim)
M dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs             (alan bazlı PATCH + üç kaynaklı kapı + denetim)
? dotnet/Kuyumcu.Api/Modules/Urun/KategoriModulu.cs         (ReyonGuncelleDto + Guncelle/PasifeAl + 2 uç)
? dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs     (Reyon + Tenant damgaları)
? dotnet/Kuyumcu.Api/Data/Migrations/20260916142643_IsletmeKimlikBilgileri.cs (+ .Designer.cs)
? dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
? dotnet/denetim.sh                                          (finansal [HttpDelete] sayacı)
? dotnet/Kuyumcu.Tests/ReyonDuzenlemeTestleri.cs            (YENİ)
? dotnet/Kuyumcu.Tests/IsletmeBilgileriTestleri.cs          (YENİ)
? dotnet/Kuyumcu.Tests/KasaDuzenlemeTestleri.cs             (YENİ)
? dotnet/Kuyumcu.Tests/BankaPasifeAlmaTestleri.cs           (YENİ)
? dotnet/Kuyumcu.Tests/DuzeltmePolitikasiTestleri.cs        (+1 test)
```

(`?` = dosyanın bulunduğu dizin git'te henüz izlenmiyor; `M` = izlenen dosyada değişiklik. **Commit YAPILMADI.**)

Dokunulan sayfalar (TEK DOKUNUŞ kaydı): **HİÇBİRİ** — bu paket 0 razor dosyasına dokundu, 27 sayfada DOM değişikliği 0 (tüm bUnit ekran testleri değiştirilmeden yeşil).

## 10. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Bertan KOŞUL 1 (BLOKLAYICI) — Kasa PATCH üç durumlu, `Ad` [Required] değil | ✅ | `KasaGuncelleDto.Ad` artık `string?`; `Ad_govdede_yoksa_KORUNUR_ve_400_ALINMAZ` + canlı adım 12; M-UX1-B-10 geri alma mutasyonu 6 testi kırıyor |
| Mehmet K1 — fiyat uçlarında rol kapısı | ✅ (zaten vardı) | `FiyatModulu.cs:720,723,728,731,798,803,811` `[RolGerekli("SAHIP","MUDUR")]`; uçlara dokunulmadı |
| Mehmet K2 — tenant PATCH'inde toplu atama yok, ayrı DTO, vergi no `^\d{10,11}$` | ✅ | `TenantKimlikGuncelleDto` yalnız 5 alan; `Plan_ve_AI_govdeden_ATANMAZ` testi |
| Mehmet K3 — kimlik değişimi denetim izine | ✅ | `YazmaKaydet(islem,"TenantKimlik",…,hassas:true)`; canlı adım 11; M-UX1-B-05 |
| Mehmet K4 — kasa kapısı ÜÇ kaynağa bakar (bakiye + kasa_hareketleri + gun_kapanislari) | ✅ | `GecmisiBosMu`; 3 test (`Hareketli…`, `Gun_kapanisli…`, `Bakiyeli…`); M-UX1-03 |
| Mehmet K5 — banka DELETE = pasife alma, hareketi varsa 409, rol SAHIP | ✅ | `HesapPasifeAl` + `KapatmaKapisi`; `SAHIP_disi_rol_hesabi_KAPATAMAZ_403`; M-UX1-B-07 |
| Mehmet K6 — reyon sorgusunda AÇIK `r.TenantId == islem.TenantId`, DTO'da SubeId YOK | ✅ | `KategoriModulu` `FirstOrDefaultAsync(r => r.Id == id && r.TenantId == islem.TenantId)`; `Yabanci_kiracinin_reyonu_404`, `Sube_degistirilemez` |
| Find/FindAsync · IgnoreQueryFilters yasağı · ExecuteUpdate/Delete = 4 | ✅ | `denetim.sh` TEMİZ (0 / 0 / 4) |
| Üç durumlu PATCH mevcut `XGonderildi` desenini kullanır, yeni desen açılmaz | ✅ | `TenantKimlikGuncelleDto.*Gonderildi`, `ReyonGuncelleDto.SorumluKullaniciIdGonderildi` — `KullaniciModulu.SubeIdGonderildi` deseninin aynısı |
| Her yeni PATCH/DELETE ucu `[ModulYetkisi]` + `[RolGerekli]` + denetim kaydı | ✅ | Reyon (`urun` + SAHIP/MUDUR), Banka (`banka` + SAHIP), Kasa (`kasa` + SAHIP/MUDUR), Tenant (sınıf `SAHIP`); 4 denetim testi |
| Finansal varlıkta hard DELETE yasağı + denetim.sh sayacı | ✅ | §7.4; M-UX1-B-08 çift kırmızı |
| HTTP testleri: yetkisiz rol 403 · yabancı kiracı 404 · kapı ihlali 409 | ✅ | 403 ×7 satır, 404 ×3, 409 ×6 |
| 27 sayfada DOM değişikliği 0 (bUnit sabit) | ✅ | 0 razor dosyası değişti; tam süit 1439/1439 |
| Tam süit 0 başarısız · denetim.sh TEMİZ · canlı doğrulama · test kiracısı silindi | ✅ | §4, §5, §8 |
| UX-S21…S33 | ⧗ kısmen (sunucu tarafı) | Bu pakette ekran yok; sunucu tarafı ilgili maddeler karşılandı: S25 (her kayıt düzeltilebilir ya da gerekçesi yazılı — `UC_YOK` kalmadı), S26 (oluşturma/düzenleme alan kümesi: reyon PATCH'i POST'un tüm alanlarını kapsar, şube hariç ve gerekçesi yazılı), S27 (yazma uçları güncel varlığı geri döner), S31 (harita gerekçeleri sözlükten, çıplak metin yok). Ekran maddeleri (S22/S23/S24/S28/S29/S30/S32/S33) UX-1-C'nin işi. |

## 11. Açık kalanlar

1. **Ekran bağlama (UX-1-C / dalga 2c):** Reyon düzenle/pasife al (UrunAgaci.razor), işletme kimlik kartı (Isletme.razor, `#ikimlik-*` önekiyle — Bertan K7), banka hesabı pasife alma (Banka.razor), kasa para birimi/şube alanları (Kasalar.razor). Sunucu hazır, ekranda düğme YOK → UX-S25 "uç var, ekranda düğme yok" borcu bu paketin sonunda AÇIK; kapanma yeri 2c.
2. **`PATCH /urunler/{id}` içinde `reyonId: null` bağı temizlemiyor** (canlı adım 5'te gözlendi). Ürün PATCH'i bu pakette kapsam dışıydı; UX-1-C ürün kartını açarken üç-durum desenine geçirilmeli.
3. **`HurdaModulu.cs:85` düzenleyen bloğu** hâlâ yalnız `tenant.Ad` + `sube.Ad` basıyor. Bertan KOŞUL 5: kolonlar eklendiğine göre gider pusulasında adres/vergi dairesi/vergi no da gösterilmeli — belge çıktısı değişikliği olduğu için UX-1-C/belge paketine bırakıldı (bu pakette belge çıktısına dokunmadım).
4. **`.mutorj` artıkları — 16 dosya** duruyor (silme yasağı; bu paket yeni artık ÜRETMEDİ, mutasyon yedekleri scratchpad'dedir): `Web/Pages/` 5 (Fiyatlandirma, Islemler, Urunler, VadeTakibi, Sayim) + `Api/` 11 (Data/HareketFabrikasi, Rapor/RaporModulu, Sayim/SayimModulu, Urun/PirlantaKapisi, Urun/BelgeModulu, Urun/UrunModulu, Satis/OdemeSepeti, Satis/SatisModulu, Fiyat/FiyatMotoru, Fiyat/FiyatModulu, Vade/VadeKapisi). Kurucuya bildirilir; ajan silmez.
5. **Kasa `DeaktifEt` 400 / yeni kapı 409 tutarsızlığı:** eski pasife alma ucu bakiye ihlalinde 400, yeni para birimi/şube kapısı 409 döner. Mevcut davranışı bozmamak için dokunulmadı; tek biçime getirmek ayrı bir karar ister.

## 12. Kurucuya / mali müşavire notlar

- **Vergi no yalnız BİÇİM olarak doğrulanıyor** (10 veya 11 hane). VKN kontrol basamağı algoritması ve TCKN algoritması UYGULANMADI — spesifikasyon böyle bir hüküm istemediği için varsayım üretilmedi. Faturada/gider pusulasında satıcı VKN'si yanlış girilirse sistem bunu yakalamaz. Algoritmik doğrulama istenirse ayrı bir karar maddesi olarak açılmalı (mali müşavir teyidi).
- **Gider pusulası ve fatura belgelerinde satıcı kimliği** artık veritabanında var ama BELGEYE BASILMIYOR (§11.3). Belgenin VUK'a uygun "düzenleyen" bilgisini taşıması için 2c'de bağlanacak; bu bir eksikliktir ve gizlenmiyor.
- **Fatura IPTAL'i storno mudur?** sorusu (spesifikasyonun açık sorusu) hâlâ AÇIK: `FaturaModulu` PATCH durum ile IPTAL yazıyor, ters kayıt üretmiyor. Bu paket faturaya dokunmadı. Mustafa/mali müşavir teyidi gereken bir muhasebe sorusudur.
