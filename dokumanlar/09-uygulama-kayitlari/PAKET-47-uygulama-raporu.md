# PAKET 47 — Veri göçü aracı (kapılı, kuru provalı içe aktarım) · Faz A — UYGULAMA RAPORU

> Tarih: 2026-09-17 · Dalga 3 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Model: Opus
> Karar: **SEÇENEK 2 — kapılı içe aktarım** (Bertan KOŞULLU + Mehmet KOŞULLU + Aşkın KOŞULLU).
> Seçenek 3 (geçmiş hareket/fatura/kasa içe aktarımı) **VETO — uygulanmadı**.
> Durum: **BİTTİ** · 2237/2237 test yeşil · `dotnet/denetim.sh` **TEMİZ** · 0 hata 0 uyarı.

---

## 1. Yapılanlar

### Veri modeli
| Tablo | Durum | Not |
|---|---|---|
| `ice_aktarim_oturumlari` | YENİ · `IKiraciVarlik` | durum: PROVA · UYGULANDI · GERI_ALINDI · IPTAL; sayaçlar (toplam/geçerli/hatalı/atlanan/uygulanan); `dosya_sha256`; `gerekce` (≥20); `ip_adresi` |
| `ice_aktarim_satirlari` | YENİ · `IKiraciVarlik` | `satir_no` · `ham_json` (**AES-256-GCM şifreli**, TEXT) · `sonuc` · `hata_kodu` · `hata_mesaji` · `hedef_varlik_turu/id` |
| `stok_partileri` | DEĞİŞİKLİK YOK | Faz B (açılış stoku) kapsam dışı |

İndeksler: `(tenant_id, olusturuldu)` · `(tenant_id, durum)` · `(tenant_id, oturum_id, satir_no)` · `(tenant_id, oturum_id, sonuc)`.
Global kiracı filtresi jenerik `KiraciFiltresi` ile otomatik geldi; ayrıca **elle `TenantId` şartı** her sorguda yazılı (ADR-0010, iki katman).

### Uçlar (hepsi sınıf düzeyinde `[RolGerekli("SAHIP")]`)
| Fiil | Yol | Kapı |
|---|---|---|
| GET | `/api/v1/ice-aktarim/sablonlar` | — |
| GET | `/api/v1/ice-aktarim/sablon/{kaynak}.csv` | UTF-8 BOM + başlık + örnek satır |
| POST | `/api/v1/ice-aktarim/prova` | `OranSiniri.IceAktarim` (5/sa) · gerekçe ≥20 · dosya kapısı · satır/kolon tavanı |
| GET | `/api/v1/ice-aktarim/{id}/satirlar` | TB-19: limit zorunlu, tavan yapılandırmadan (200) · kimlik alanları **maskeli** |
| POST | `/api/v1/ice-aktarim/{id}/uygula` | `OranSiniri.IceAktarim` · durum=PROVA · onay metni birebir · **tek transaction** |
| POST | `/api/v1/ice-aktarim/{id}/geri-al` | durum=UYGULANDI · **soft delete** (pasife alma) |
| PATCH | `/api/v1/ice-aktarim/{id}` | PATCH sözleşmesi · yalnız PROVA (uygulanmışta 409) |
| POST | `/api/v1/ice-aktarim/{id}/iptal` | PROVA oturumunun "pasife alma" karşılığı |
| GET | `/api/v1/ice-aktarim/gecmis` | limit tavanlı |

### Dosyalar
**YENİ**
- `dotnet/Kuyumcu.Api/Data/IceAktarim.cs` — iki varlık + durum/politika sabitleri
- `dotnet/Kuyumcu.Api/Modules/Iceaktarim/CsvOkuyucu.cs` — RFC 4180 ayrıştırıcı + başlık kapısı + kültürsüz alan okuyucuları
- `dotnet/Kuyumcu.Api/Modules/Iceaktarim/IceAktarimKapisi.cs` — dosya kapısı + `IceAktarimAyarlari`
- `dotnet/Kuyumcu.Api/Modules/Iceaktarim/Kaynaklar.cs` — `IIceAktarilabilir` + `CariKaynagi` + `UrunKaynagi` + `DtoDogrulayici`
- `dotnet/Kuyumcu.Api/Modules/Iceaktarim/IceAktarimModulu.cs` — servis + controller
- `dotnet/Kuyumcu.Api/Common/VeriIslemi.cs` — transaction bağlamı devralma (bkz. sapma S-3)
- `dotnet/Kuyumcu.Core/Sozlesmeler/IceAktarimSozlesmeleri.cs` — istemci sözleşme record'ları
- `dotnet/Kuyumcu.Web/Pages/VeriIceAktarim.razor` — üç adımlı ekran
- `dotnet/Kuyumcu.Api/Data/Migrations/20260917140632_IceAktarim.cs` (+ `.Designer.cs`)
- 4 test dosyası (aşağıda)

**DEĞİŞTİ**
`KuyumcuDbContext.cs` (2 DbSet + 4 indeks + şifreleme dönüştürücüsü) · `OranSiniri.cs` (`IceAktarim` politikası 5/sa) · `ModulKayit.cs` (DI) · `Program.cs` (ayar bağlama) · `appsettings.json` (`IceAktarim` bölümü) · `Cari/CariModulu.cs` (2 satır, sapma S-3) · `Meta/DuzeltmePolitikasi.cs` (2 varlık damgası + `ICE_AKTARIM` gerekçe anahtarı) · `Core/I18n/Sozlukler.cs` (TR+EN `SozlukIceAktarim` + `Nav.VeriIceAktarim` + gerekçe + varlık adları) · `Web/Bilesenler/Ikonlar.cs` · `Web/Layout/KenarCubugu.razor` · `Web/Layout/RotaBasliklari.cs` · `Tests/RotaBaslikTestleri.cs` (34→35) · `dotnet/denetim.sh` (2 sayaç + 2 yeni kapı)

### Dokunulan sayfalar (MÜHÜR 2 · tek dokunuş)
- `Kuyumcu.Web/Pages/VeriIceAktarim.razor` — **YENİ** (bu dalgada ilk ve tek dokunuş)
- `Kuyumcu.Web/Layout/KenarCubugu.razor` — yalnız TEK bağlantı bloğu eklendi (SAHİP bloğu, mevcut DOM'a dokunulmadı)

---

## 2. Sapmalar ve gerekçeleri

| # | Spesifikasyon | Kod gerçeği | Yapılan |
|---|---|---|---|
| **S-1** | Test: `Uygulanan_cari_kartinda_vergi_no_SIFRELI_saklanir`; canlı: "`cariler.vergi_no` düz metin olmadığını göster" | **YANLIŞ.** ADR-0003 alan şifrelemesi bugün yalnız `MasakKimlikTespiti.BelgeNo` ve `GiderPusulasi.SaticiTckn` sütunlarına kuruludur (`KuyumcuDbContext.cs` `HasConversion` çağrıları). `cariler.vergi_no` **düz metindir** ve bu paket onu değiştirmez (Cari modülü DEĞİŞMEZ kuralı) | Testin **HEDEFİ** korundu (= "içe aktarım kendi Add'ini yapmaz, servis yolundan geçer"), ölçüm gerçeğe çevrildi: `Uygulanan_cari_SERVIS_yolundan_gecer_cari_bakiye_satiri_dogar` — doğrudan `db.Cariler.Add` yazılsaydı `cari_bakiyeler` satırı **hiç doğmazdı**. Mutasyon M47-2 bunu kanıtlar. **Kurucuya not:** cari kartındaki vergi no/TCKN şifrelenecekse bu AYRI bir paket ve migration'dır. |
| **S-2** | `ham_json` kimlik alanları **maskelenerek** yazılır | Maskeleyerek yazmak uygulama adımını imkânsızlaştırır: prova ve uygula iki ayrı istektir; maskelenmiş telefon karta **maskeli olarak** yazılırdı (veri bozulması) | Mehmet'in koşulundaki **ikinci seçenek** uygulandı: "**ya da ham_json AES şifreli**". `ham_json` diskte AES-256-GCM ile şifrelidir (ADR-0003 altyapısı), **ekrana giden kopya maskelidir** (satır listesi ucu). Kanıt: `Ham_satir_DISKTE_sifreli_saklanir_ADR0003` + `Satir_listesinde_kimlik_alanlari_MASKELI_doner` + canlı SQL. |
| **S-3** | "CariModulu / UrunModulu DEĞİŞTİRİLMEZ" | `CariServisi.Olustur` kendi `BeginTransactionAsync()`'ini açıyor; EF Core açık transaction varken ikincisini **fırlatır**. "Tek transaction" değişmezi bu yüzden kırılıyordu | `Common/VeriIslemi.cs` yardımcısı eklendi ve `CariModulu.cs`'te **2 satır** değişti: `BeginTransactionAsync()` → `IsBaslatVeyaDevral()`, `tx.CommitAsync()` → `if (tx is not null) await tx.CommitAsync()`. Tek başına çağrıldığında **davranış aynıdır**. Alternatifler reddedildi: kendi Add'i (FAIL-CLOSED 3 ihlali) ve satır satır commit (kısmi göç). Kanıt: `Cari_uygulamasi_DIS_transaction_icinde_calisir_ic_ice_HATA_YOK`. |
| **S-4** | Test: `Acilis_bakiyesi_CARI_DUZELTME…`, `Acilis_stoku_DEVIR_partisi…`, mutasyon "DEVIR partisinde NULL yerine 0" | Faz B kalemi | **Kapsam dışı** (görev metni: "yalnız Faz A"). Yerine kapsamın kod düzeyinde kapalı olduğu sınandı: `Gecmis_hareket_kaynagi_YOKTUR_seceneK3_VETO` (satis/kasa/fatura/acilis-bakiye/acilis-stok → 400). Mutasyon yuvası **M47-5**'e `ham_json` şifreleme kanıtı kondu. |
| **S-5** | `GUNCELLE` yinelenen politikası | Bertan + Mehmet ortak koşulu: Faz A'da **sunulmaz** | Kod `GUNCELLE`'yi **reddeder** (400) ve sebebini yazar. Kanıt: `GUNCELLE_politikasi_FAZ_A_da_KABUL_EDILMEZ`. |
| **S-6** | "`CsvOkuyucu` satır satır `IAsyncEnumerable` — `ToListAsync` yok" | Dosya base64 gövdeyle geldiği için zaten **tamamen bellektedir**; asenkron akış hiçbir bellek kazancı sağlamaz | Senkron `IEnumerable` + **satır/kolon/bayt tavanı** (CSV bomb savunması). Tavan aşılınca oturum hiç açılmaz. |
| **S-7** | "dosya magic-byte (BelgeKapisi deseni)" | **CSV'nin magic byte'ı yoktur** | Kapı **negatif imza** uygular: PDF/JPEG/PNG/ZIP-XLSX/OLE2-XLS/GZIP imzaları + NUL baytı + katı UTF-8 çözümü reddedilir. Tahmin yok, kanıt var (Anayasa md.1). Kanıt: `Ikili_dosya_CSV_sanilmaz_ve_oturum_ACILMAZ`. |
| **S-8** | `denetim.sh` "DEĞİŞİKLİK GEREKMEZ" | `BEKLENEN_VARLIK=41` ve `Pages/*.razor` tavanı 34 çırçır sayaçlarıdır; yeni iki varlık ve yeni sayfa bunları kırardı | 41→**43**, 34→**35** (gerekçe yorumda). Ayrıca **iki YENİ kapı** eklendi (aşağıda). |

### Mevzuat işaretleri
- **DOGRULANDI (bu pakette hüküm EKLENMEDİ):** MASAK 185.000 TL kümülatifi ve 459 s. VUK 30.000 TL tevsik haddi — içe aktarımda **para el değiştirmez, deftere yazılmaz**; canlı doğrulamada `hareketler` ve `kasa_hareketleri` **0 satır** kaldı.
- **DOGRULANMALI (koda sabit yazılmadı):** Kuyum Ticareti Yönetmeliği md.11/(e) taş kökeni — bu paket **yeni hüküm eklemez**, mevcut `TasKapisi`'ni içe aktarım yolunda da zorunlu kılar. RG atfının birincil metinden teyidi bu paketin işi değildir.
- **Eşikler koda gömülmedi (ADR-0005):** `MaksSatir` · `MaksDosyaBayt` · `MaksAlanSayisi` · `GerekceAsgari` · `UygulaOnayMetni` · `SatirListesiTavan` → `appsettings.json` → `"IceAktarim"`. Değerler **ölçülmedi**; Mehmet'in başlangıç değerleridir ve `_not` alanında böyle yazılıdır.

---

## 3. Derleme ve migration

```
dotnet build Kuyumcu.sln --nologo → 0 Hata, 0 Uyarı
```

Migration: **`20260917140632_IceAktarim`** (build'li `dotnet ef migrations add`, `--no-build` KULLANILMADI).
`kuyumcu_net` uygulaması: `dotnet ef database update --connection …` → `Applying migration '20260917140632_IceAktarim'. Done.`
Kanıt:
```
SHOW TABLES LIKE 'ice_aktarim%';
ice_aktarim_oturumlari
ice_aktarim_satirlari
```

---

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| `CsvOkuyucuTestleri` [saf] | 9 | 9 | 0 | 0 |
| `IceAktarimTestleri` [DB] | 23 | 23 | 0 | 0 |
| `IceAktarimHttpTestleri` [HTTP] | 9 | 9 | 0 | 0 |
| `IceAktarimEkranTestleri` [bUnit] | 10 | 10 | 0 | 0 |
| **Tam süit (sonda)** | **2237** | **2237** | **0** | **0** |

**Yeni test sayısı: 51.** Paket öncesi süit: **2186** → sonrası **2237**.

Tam süit sırasında dört mevcut test kırmızıya döndü ve **hepsi bu paketin eksiği yüzündendi** (gizlenmedi, çözüldü):
- `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` → iki yeni varlık `DuzeltmePolitikasi` haritasına damgalandı.
- `RotaBaslikTestleri` ×3 → `/veri-ice-aktarim` rota başlığı haritasına eklendi, rota sayacı 34→35.

---

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/p47/` altına kopyalandı → `perl -0777 -pi -e` ile **tek eşleşmeli** değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile **birebir aynı** olduğu kanıtlandı. Son mutasyondan sonra **temiz `dotnet build`: 0 hata**. Projeye `.mutorj` bırakılmadı (`denetim.sh` sayacı 0).

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| **M47-1** | `CsvOkuyucu.BasliklariDenetle` zorunlu kolon kontrolü (`k.Zorunlu && !gelen.Contains(k.Ad)` → `false`) | `CsvOkuyucuTestleri.Bilinmeyen_kolon_HATA_degil_UYARI_zorunlu_kolon_eksikse_HATA` | `cmp` AYNI |
| **M47-2** | Satır uygulaması servis yolunu atlar, doğrudan `db.Cariler.Add` yazar | `IceAktarimTestleri.Uygulanan_cari_SERVIS_yolundan_gecer_cari_bakiye_satiri_dogar` **ve** `Bir_satir_patlarsa_TUM_oturum_geri_alinir_kismi_goc_YOK` | `cmp` AYNI |
| **M47-3** | `TasKapisi` istisnası `catch` içinde **yutulur** (satır GEÇERLİ sayılır) | `IceAktarimTestleri.Tas_agirligi_var_kokeni_yok_satir_HATA_ADR0013` | `cmp` AYNI |
| **M47-4** | `Uygula`'daki transaction kaldırılır (satır satır `SaveChanges`) | `IceAktarimTestleri.Bir_satir_patlarsa_TUM_oturum_geri_alinir_kismi_goc_YOK` | `cmp` AYNI |
| **M47-5** | `ham_json` AES dönüştürücüsü `OnModelCreating`'den kaldırılır (S-4 yerine geçen kalem) | `IceAktarimTestleri.Ham_satir_DISKTE_sifreli_saklanir_ADR0003` | `cmp` AYNI |
| **M47-6** | `[RolGerekli("SAHIP")]` → `[RolGerekli("SAHIP", "MUDUR")]` | `IceAktarimHttpTestleri.MUDUR_dahi_ice_aktaramaz_yalniz_SAHIP_403` | `cmp` AYNI |
| **M47-7a** | Elle `TenantId` şartı `OturumBul`'dan silinir | **YEŞİL KALDI** — global filtre tuttu (ADR-0010'un dürüst kanıtı: tek katman düşerse diğeri tutar) | `cmp` AYNI |
| **M47-7b** | 7a'ya EK olarak global filtre de devre dışı (`HasQueryFilter(e => e.TenantId != null)`) | `IceAktarimTestleri.Baska_kiracinin_oturumu_okunamaz_404` | `cmp` AYNI |

**Test açığı bulunamadı**: listedeki her mutasyon (7a hariç, ki o bilinçli olarak iki aşamalıdır) en az bir testi kırmızıya çevirdi.

---

## 6. Denetim çıktısı

`dotnet/denetim.sh` → **DENETİM TEMİZ** (çıkış kodu 0). Paketin eklediği/değiştirdiği satırlar:

```
  ✓ Iceaktarim/ altında dogrudan Add (0 olmalı)  0      ← YENİ KAPI (FAIL-CLOSED 3)
  ✓ İçe aktarım → mevcut servis çağrısı (>=2) 2         ← YENİ KAPI (sessiz kayıp koruması)
  ✓ IKiraciVarlik uygulayan varlık                46     (eşik 41 → 43)
  ✓ Pages/*.razor dosya sayısı                    35     (tavan 34 → 35)
  ✓ Pages: @onclick'siz ve disabled'sız <button>  69     (tavan 71 — ARTMADI)
  ✓ Pages: ModalPencere kullanımı                 45     (tavan 45 — yıkıcı onay, tek kullanım)
  ✓ Pages: duzenlenenId kalıbı                     1     (tavan 1 — ikinci kalıp açılmadı)
  ✓ Pages içinde istemci record tanımı           121     (tavan 122 — ARTMADI, sözleşmeler Core'da)
  ✓ Hareketler.Add (yalnız HareketFabrikasi)       1     (defter yazma yolu açılmadı)
  ✓ ExecuteUpdate/Delete (beyaz liste 4)           4     (beyaz liste büyümedi)
  ✓ Kaynak ağacı: beyaz liste dışı .mutorj        0     (mutasyon artığı yok)
```

---

## 7. Canlı doğrulama (localhost:5198, test kiracısı `PAKET47-TEST Kuyumcu`)

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /ice-aktarim/sablon/cari.csv` (SAHİP) | 200, UTF-8 BOM + başlık | **200**, BOM + `tur,ad,telefon,email,vergiNo,adres,notlar,krediLimitiTl,varsayilanVadeGun` + örnek satır |
| 2 | `POST /ice-aktarim/prova` (3 cari) | 200, toplam 3 / geçerli 3 / hatalı 0 / **yazılanKayit 0** | **200** `{"toplamSatir":3,"gecerliSatir":3,"hataliSatir":0,"yazilanKayit":0,"durum":"PROVA"}` |
| 3 | **Kuru prova kanıtı** — `GET /cari?limit=200` | cari sayısı DEĞİŞMEZ (0) | **0** · SQL: `cariler 0 · oturum 1 · satir 3` |
| 4 | **Sahne tablosu şifreli mi** (değer basılmadı) | SIFRELI | SQL: `CASE WHEN ham_json LIKE 'v1:%' AND ham_json NOT LIKE '%<telefon>%' …` → **SIFRELI** |
| 5 | `POST /{id}/uygula` (onay birebir) | 200, 3 kayıt | **200** `{"durum":"UYGULANDI","uygulananSatir":3,"geriAlinabilir":true}` |
| 6 | `GET /cari` + SQL | 3 aktif cari, 3 `cari_bakiyeler`, **defter boş** | `cariler 3 · cari_bakiyeler 3 · hareketler 0 · kasa_hareketleri 0` (servis yolu + defter dokunulmadı) |
| 7 | Denetim izi | 2 kayıt, `hassas=1`, gerekçe ile | SQL: `EKLEME 1 "kuru prova · kaynak: cari · dosya: cari.csv · 3 satır (3 geçerli…"` · `EKLEME 1 "uygulandı · kaynak: cari · 3 kayıt oluşturuldu…"` |
| 8 | İKİNCİ kez uygula | 409 | **409** `"Bu oturum 'UYGULANDI' durumunda…"` |
| 9 | `GET /{id}/satirlar?limit=5` | kimlik alanları MASKELİ | **200** — `"telefon":"*******1111","vergiNo":"******1111","adres":"[MASKELİ]"` |
| 10 | `POST /{id}/geri-al` | 200, 3 pasif | **200** `{"durum":"GERI_ALINDI","pasifeAlinan":3}` · `GET /cari?aktif=true` → **0** · SQL `aktif=0 → 3` (**silinmedi**) |
| 11 | Taş ağırlığı var kökeni yok (ürün) | satır HATA, md.11/(e) mesajı | **200**, `gecerliSatir 0 / hataliSatir 1`, `{"satirNo":2,"hataKodu":"TAS_KOKENI","hataMesaji":"… md.11/(e) — RG 24.06.2026/33290."}` |
| 12 | `kaynak=satis` (seçenek 3 yüzeyi) | 400 | **400** `"Bilinmeyen içe aktarım kaynağı: 'satis'. Geçerli değerler: cari, urun…"` |
| 13 | Oran sınırı (5/sa aşıldıktan sonra) | 429 | **429** ×4 `{"error":"TooManyRequests"}` |
| 14 | MUDUR jetonuyla `POST /prova` | 403 | **403** `"Bu işlem için gerekli rol: SAHIP"` · MUDUR `GET /sablonlar` → **403** |
| 15 | Jetonsuz `GET /sablonlar` | 401 | **401** |
| 16 | `PATCH /{id}` (PROVA oturumu) | 200, gerekçe güncellenir | **200**, `gerekce` yeni değerle döndü, `yinelenenPolitikasi` **korundu** (PATCH sözleşmesi) |
| 17 | `PATCH /{id}` (UYGULANDI oturumu) | 409 | **409** |
| 18 | `GET /gecmis?limit=10` | 2 oturum, sayaçlar | **200** `{"toplam":2,…}` |

**Temizlik:** API PID 13793 durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
Test kiracısı `information_schema`dan üretilen 46 `DELETE` ile tüm `tenant_id` kolonlu tablolardan silindi + `tenants` satırı silindi.
`tenants` sayısı: **8 → 8** (başlangıçtaki değer). Artık satır kontrolü (cariler + oturum + satır + kullanıcı + denetim): **0**.
`QA-TEST-` önekli 3 kalıcı QA kiracısına **dokunulmadı** (sayı 3, değişmedi).

---

## 8. DoD sözleşmesi (MÜHÜR 2 + DoD EK 1/2)

| Varlık | Düzenleme yolu | Pasife alma / geri dönüş | Yetki | Denetim kaydı | Ekran |
|---|---|---|---|---|---|
| `IceAktarimOturumu` | **PATCH** `/ice-aktarim/{id}` (gerekçe · yinelenen politikası) — yalnız PROVA | **POST `/iptal`** (PROVA) · **POST `/geri-al`** (UYGULANDI → kartlar pasife) | `[RolGerekli("SAHIP")]` sınıf düzeyi | `DenetimServisi.YazmaKaydet` **çağrılır** (prova · uygula · geri-al · patch · iptal), `hassas=true` | Geçmiş tablosunda **SatirIciDuzenle** ile gerekçe; "Provayı iptal et" ve "Geri al" düğmeleri |
| `IceAktarimSatiri` | **YOK — salt okunur.** `DuzeltmePolitikasi`: `SALT_OKUNUR` + `ICE_AKTARIM` gerekçesi | — | aynı | oturum kaydı üzerinden | **`<DegismezIpucu>`**: "İçe aktarım satırı, dosyadaki bir satırın sonucudur ve kanıt kaydıdır; değiştirilmez. Düzeltmek için dosyayı düzeltip yeni bir kuru prova çalıştırın." + alternatif yol |

- **Finansal varlıkta hard DELETE:** bu pakette **hiç DELETE ucu yok**; geri alma `Aktif=false` (mevcut `DeaktifEt` servisleri). Deftere (hareketler/kasa/banka/fatura) **hiç dokunulmuyor**.
- **DoD/1 — ölü düğme yok:** 7 düğmenin **hepsi** ya istek atar ya `disabled` taşır; `denetim.sh` sayacı 69 (tavan 71) — **artmadı**. bUnit: `Dosya_ve_gerekce_yokken_PROVA_kilitli_ve_sebep_YAZAR`, `Onay_kutusu_yazilmadan_UYGULA_pasif_ve_istek_GITMEZ`.
- **DoD/2 — tek düzenleme kalıbı:** tek alanlı düzeltme **SatirIciDuzenle**; `ModalPencere` **yalnız yıkıcı onay** (geri al / iptal) — sayaç 45 (tavan 45), `duzenlenenId` kalıbı 1 (tavan 1), **ikinci kalıp açılmadı**.
- **DoD/3 — sessiz form yasağı:** gerekçe alanı sayaç + alan altı Türkçe mesaj (`#ia-gerekce-hata`), dosya hatası `#ia-dosya-hata`, kilit sebebi `#ia-kilit-sebep` / `#ia-uygula-kilit`.
- **DoD/4 — yıkıcı eylem onayı:** geri al ve iptal ortak onay modalından geçer; geri dönüş yolu **aktife alma** (kayıtlar silinmez, mesajda yazılı). bUnit: `Uygulandiktan_sonra_GERI_AL_gorunur_ve_ONAY_ISTER` — tıklama tek başına istek atmıyor.

### UX sözleşmesi (UX-S21…S33) karşılığı
| Madde | Karşılık |
|---|---|
| UX-S21 | Kaynak kolon listesi, tavanlar ve onay metni **sunucudan** gelir; kullanıcıdan istenmez |
| UX-S22 | `sablon.KaynakListesi.Count <= 1` ise seçici gizlenir, değer salt okunur satır olur |
| UX-S23 | Soyut kaydın (oturum) etkisi **aynı ekranda**: sayaçlar + hata tablosu + "yazılan kayıt" |
| UX-S24 | Kaynak değişince prova sonucu **sessizce durmaz**, sıfırlanır; politika değişince `provaYenilenmeli` döner |
| UX-S25 | Oturum düzeltilebilir; satır için `DegismezIpucu` + doğru düzeltme yolu |
| UX-S26 | Oluşturma ve düzenleme aynı alan kümesini (gerekçe + politika) taşır; seçenek listesi **tek diziden** (`sablon.PolitikaListesi`) |
| UX-S27 | Her yazma başarı bandıyla biter; banda sunucunun döndürdüğü **uygulanan/pasife alınan sayı** yazılır; POST yanıtı okunur |
| UX-S28 | `catch` bloklarında liste **boşaltılmaz**; boş durum yalnız `gecmisYuklendi && sayı==0` iken çıkar, hata bandı ayrıdır |
| UX-S29 | Geçmiş limiti (`#ia-gecmis-siniri`) ve ilk 100 hata sınırı (`#ia-hata-siniri`) ekranda yazar |
| UX-S30 | Kilitli her düğmenin yanında eksik koşul listesi |
| UX-S31 | `PROVA/UYGULANDI/GERI_ALINDI/IPTAL` ve `cari/urun` ham kodları sözlükten çevrilir |
| UX-S32 | Bu ekranda **tutar yoktur** (para yok, defter yok) — madde uygulanmaz |
| UX-S33 | Dönem seçici yoktur — madde uygulanmaz |

---

## 9. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Yalnız SEÇENEK 2; seçenek 3 VETO | **KARŞILANDI** | `Gecmis_hareket_kaynagi_YOKTUR_seceneK3_VETO` · canlı #12 (400) |
| Kapsam yalnız Faz A (cari + ürün kartı) | **KARŞILANDI** | `Kaynaklar.Hepsi` = `[CariKaynagi, UrunKaynagi]`; `acilis-bakiye`/`acilis-stok` → 400 |
| ADR-0017/ADR-0014: rakip veritabanına bağlanma YASAK | **KARŞILANDI** | Yalnız base64 CSV gövdesi; hiçbir dış bağlantı kodu yok |
| Kişisel veri: `ham_json` düz metin duramaz | **KARŞILANDI** (AES seçeneğiyle — sapma S-2) | M47-5 · canlı #4 · `Ham_satir_DISKTE_sifreli_saklanir_ADR0003` |
| FAIL-CLOSED 1: doğrulanamayan satır uygulanmaz, yarım kart yok | **KARŞILANDI** | `Zorunlu_kolon_eksik_dosya_REDDEDILIR` · `Gecersiz_ayar_kodu_satiri_HATA_diger_satirlar_GECERLI` |
| FAIL-CLOSED 2: taş ağırlığı > 0 + köken boş = HATA, istisna yutulmaz | **KARŞILANDI** | M47-3 · canlı #11 |
| FAIL-CLOSED 3: kendi Add'i yok, mevcut servis çağrılır | **KARŞILANDI** | `denetim.sh` iki yeni kapı · M47-2 · canlı #6 (`cari_bakiyeler` 3) |
| Tek transaction; bir satır patlarsa hepsi geri alınır | **KARŞILANDI** | M47-4 · `Bir_satir_patlarsa_TUM_oturum_geri_alinir_kismi_goc_YOK` (gerçek `ConflictException` ile, satır sayısı SQL'le karşılaştırıldı) |
| İç içe transaction davranışı MySQL'de doğrulandı (Aşkın) | **KARŞILANDI** | `Cari_uygulamasi_DIS_transaction_icinde_calisir_ic_ice_HATA_YOK` · sapma S-3 |
| Yinelenen varsayılanı HATA; GUNCELLE Faz A'da yok | **KARŞILANDI** | `Yinelenen_kod_politikasi_HATA_ise_satir_reddedilir` · `GUNCELLE_politikasi_FAZ_A_da_KABUL_EDILMEZ` |
| Sınıf düzeyi `[RolGerekli("SAHIP")]` | **KARŞILANDI** | M47-6 · canlı #14 (MUDUR 403) |
| Denetim izi `Hassas=true`, gerekçeli | **KARŞILANDI** | `Her_adim_denetim_izine_GEREKCEYLE_yazilir_hassas` · canlı #7 |
| Geri alma SOFT delete; `ExecuteUpdate/Delete` yok | **KARŞILANDI** | canlı #10 (`aktif=0`, 3 satır duruyor) · `denetim.sh` beyaz liste 4 (artmadı) |
| Oran sınırı `ice-aktarim` 5/sa (dışa aktarımın altında) | **KARŞILANDI** | `OranSiniri.cs` · `Oran_siniri_asilinca_429` · canlı #13 |
| `MaksSatir` 5.000 · `MaksDosyaBayt` ≤ 2 MB · yapılandırmadan | **KARŞILANDI** | `appsettings.json` → `"IceAktarim"` · `Program.cs` bağlama |
| Testler rakam sabitlemez, tavan+1 → 400 | **KARŞILANDI** | `Satir_tavani_asilinca_oturum_ACILMAZ` (ayar nesnesinden) · `Dosya_tavani_asilinca_400…` (`Services.GetRequiredService<IceAktarimAyarlari>()`) · `Satir_listesi_limitsiz_cagrilamaz_TB19` |
| CSV bomb / alan sayısı tavanı; enjeksiyon tırnağı girişte sökülür; `MarkupString` yok | **KARŞILANDI** | `MaksAlanSayisi` · `Bastaki_CSV_enjeksiyon_tirnagi_SOKULUR` · sayfada `MarkupString` **0** |
| `Ondalik_NOKTA_ile_okunur` testi kültürü açıkça tr-TR'ye çeker | **KARŞILANDI** | `CsvOkuyucuTestleri.Ondalik_NOKTA_ile_okunur_tr_TR_kulturunde_de` |
| İki katmanlı kiracı koruması (ADR-0010) | **KARŞILANDI** | M47-7a (yeşil kalır) + M47-7b (kırmızı) |
| TB-19 limit zorunlu, tavan | **KARŞILANDI** | `Satir_listesi_limitsiz_cagrilamaz_TB19` · `sinirdaMi` alanı |
| `Sozlukler` TR+EN, çıplak metin yok | **KARŞILANDI** | `SozlukIceAktarim` (42 anahtar) TR+EN · `Nav.VeriIceAktarim` · `IceAktarimGerekce` |
| Mevcut element id'leri değişmedi; yeni id'ler `<önek>-<alan>` | **KARŞILANDI** | Tüm yeni id'ler `ia-…` / `si-iceaktarim-…` |
| Ücretsiz pilot: fiyat/teklif/tahsilat metni üretilmedi | **KARŞILANDI** | Ekranda ve sözlükte fiyat/paket/teklif ifadesi yok |
| Canlı veri üzerinde deneme YOK (Mehmet) | **KARŞILANDI** | Yalnız `PAKET47-TEST` kiracısı; iş sonunda silindi, `tenants` 8→8 |
| COMMIT YOK | **KARŞILANDI** | `git add/commit/push` çalıştırılmadı |

---

## 10. Açık kalanlar

1. **ADR-0016 (b) paket 32 ile ORTAK yazılacak** — Bertan + Mehmet + Aşkın koşulu: içe aktarım ADR'si paket 32 (banka ekstresi içe aktarma) ile **tek** belgede yazılacak ve ADR-0014'ün "geri alma yoktur" güvenlik gerekçesinin nasıl karşılandığı **madde madde** gösterilecek. ⚠ **`0016` numarası ZATEN KULLANILMIŞ** (`adr/0016-reyon-yerlesimi-defter-kaydi-degildir.md`); ortak ADR **0017** (ya da sonraki boş numara) olarak açılmalı. Bu rapor ADR yazmadı — numaralandırma orkestratör/Bertan kalemidir. **Faz B (T-2) bu ADR oylanmadan başlamamalı.**
2. **Mehmet'in (06 Güvenlik) bu uygulamaya onayı ALINMADI.** Spesifikasyonun riskler bölümü "Faz A, güvenlik onayı olmadan sevk edilmez" diyor. Kod hazır ve koşulların tamamı karşılandı, ama **sevk kararı Mehmet'in turundadır.**
3. **`ham_json` saklama süresi ADR-0008'e bağlanmadı.** `SaklamaOnizlemeIsi` bu tabloyu bilmiyor. Süre değeri bu pakette **uydurulmadı**; ADR-0008 sahibinin kararı gerekiyor.
4. **Tavan rakamları ölçülmedi.** 5.000 satır / 2 MB / 5 istek-saat — gerçek bir dükkânın kart sayısı ve büyük dosyada MySQL kilit süresi **ölçülmedi**. Yapılandırmadan değiştirilebilir; "sınırsız" denmedi.
5. **Faz B (T-2) kapsamı:** açılış bakiyesi (`CariServisi.Duzeltme` → `CARI_DUZELTME`), açılış stoku (`UrunServisi.StokSayimGuncelle` → `SAYIM_DUZELTME` + `DEVIR` partisi + `birim_maliyet_tl=NULL`), ortak kuruluş sihirbazı ekranı ve `kur_snapshot_id` kuralı **bu pakette yoktur**.
6. **Excel (.xlsx) ayrıştırma yok** — yeni bağımlılık; kullanıcı "CSV UTF-8" kaydeder, kapı `.xlsx` imzasını tanıyıp **anlaşılır Türkçe hata** verir.
7. **Rakip kolon adları bilinmiyor** (PARITE §6). Satış vaadi dürüst tutuldu: ekranda "tek tık göç" **yazmıyor**, "şablona yapıştır" yazıyor. Otomatik alan eşleme (AI) **yapılmadı** — KVKK yurt dışı aktarımı gerekçesi.

---

## 11. Kurucuya / mali müşavire notlar

- **Kurucuya (kod kararı):** `cariler.vergi_no` bugün **düz metindir** (yalnız MASAK belge no ve gider pusulası TCKN'si şifreli). Spesifikasyon bunun tersini varsayıyordu. İçe aktarım bu durumu **değiştirmez**; cari kartındaki vergi no/TCKN şifrelenecekse ayrı bir paket + migration gerekir. Karar sizde değil ekipte, ama **paket sınırının dışında** kaldığı için raporlanıyor (Anayasa md.1).
- **Mali müşavire (doğrulanmalı):** Kuyum Ticareti Yönetmeliği md.11/(e) taş kökeni beyanı için kodda **RG 24.06.2026/33290** atfı var. Bu paket yeni hüküm eklemedi, mevcut kapıyı içe aktarım yolunda da zorunlu kıldı. **Atfın birincil metinden teyidi hâlâ açıktır.**
- **Mali müşavire (bilgi):** içe aktarım **hiçbir defter kaydı üretmez** — canlı doğrulamada `hareketler` ve `kasa_hareketleri` 0 satır kaldı. Geçmiş satış/fatura/kasa hareketi **bilerek aktarılmıyor**: geriye dönük hareket 459 s. VUK tevsik haddi günlük kümülatifini ve MASAK bağlantılı işlem kümülatifini yanlış hesaplatırdı. Açılış bakiyeleri ayrı pakette (T-2), **para el değiştirmeyen düzeltme** olarak gelecek.
- **Kurucuya (iş):** ürün ücretsiz pilot aşamasında olduğu için bu pakette hiçbir fiyat/teklif/tahsilat metni üretilmedi.
