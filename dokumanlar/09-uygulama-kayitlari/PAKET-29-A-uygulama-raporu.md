# PAKET 29-A — Çek / Senet Portföyü (Faz A) · Uygulama Raporu

> Dalga 4 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü · Tarih: 2026-09-18
> Durum: **BİTTİ** · Derleme 0 uyarı / 0 hata · Tam süit **2600/2600 yeşil** · `dotnet/denetim.sh` **TEMİZ**
> **COMMIT YAPILMADI** (kurucunun kırmızı çizgisi). Çalışma ağacında duruyor.

---

## 1. Özet

Kıymetli evrak (ALINAN çek/senet) sıfırdan kuruldu: tablo, varlık, fail-closed durum kapısı,
10 uç, iki ekran, TR+EN sözlük, modül yetkisi (`evrak`) ve 80 yeni test. Evrak girişi cari
borcunu **defter üzerinden** kapatır, karşılıksız çıkarsa borç ters kayıtla **geri doğar**.
Evrak tablosu bakiyenin kaynağı değildir; bakiyeyi yalnız `hareketler` defteri değiştirir (K4-B).

**A1 dilimi:** tablo + `EvrakDurumKapisi` + giriş / portföy listesi / tahsile ver / portföye al.
**A2 dilimi:** tahsil (kasa|banka) + karşılıksız + vade takvimi + `evrak` modül yetkisi (K4-F).
Her iki dilim sonunda da aynı kapı koşuldu (aşağıdaki çıktılar son koşuma aittir).

---

## 2. Önkoşul denetimi (uygulamadan ÖNCE doğrulandı)

| Önkoşul | Durum | Kanıt |
|---|---|---|
| 48-A / `StornoServisi` canlıda | **VAR** | `Kuyumcu.Api/Modules/Satis/StornoServisi.cs:71` beyaz liste mevcut |
| 45 MFA kalemi | **VAR** | `Kuyumcu.Api/Modules/Auth/MfaModulu.cs` mevcut, `MfaKurtarmaKodu` indeksleri DbContext'te |
| S-2 record dondurma yürürlükte | **VAR (tavan DOLU: 129/129)** | 29-A **hiç yeni istemci record'u açmadı**; yanıt tipleri `Kuyumcu.Core/Sozlesmeler/EvrakSozlesmeleri.cs`'e kondu |
| `Cariler.razor`'a dokunan paketler aynı dalgada koşmuyor | **UYULDU** | `Cariler.razor` bu pakette **HİÇ AÇILMADI** (evrak kutusu devredildi) |
| `dotnet build` yeşil, temiz ağaç | **VAR** | migration üretiminden önce 0/0 derleme alındı |
| K4-F tohumlama sıralama kilidi | **ZATEN AÇIK** — sapma, bkz. §3.1 | paket 6 Faz A mekanizmayı kurmuş |

**Son migration sapması:** spesifikasyon "son migration `20260916060026_BelgeDeposu`" diyordu; gerçek
son migration **`20260917220208_S3bDuzeltilebilirlik`** idi. Yeni migration ondan SONRA üretildi.

---

## 3. Spesifikasyondan sapmalar ve gerekçeleri

### 3.1 K4-F (modül-başı yetki tohumlaması) ZATEN UYGULANMIŞ
Spesifikasyon `YetkiModulu.TohumlaGerekiyorsa`'nın tek `__yapilandirildi__` markerinden modül-başı
işaretçiye çevrilmesini istiyordu. **Kod okundu: bu iş paket 6 Faz A'da yapılmış** — `ModulMarker(modul)`
(`__tohum:<modul>__`), `Moduller.TohumlamaOncesi` sabit listesi ve üç durumlu `TohumlaGerekiyorsa`
yürürlükte ve dosyanın kendi yorumunda "29/31/32/45/46 yalnız kendi anahtarlarını ekler; buraya
dokunmaz" yazıyor. **29-A mekanizmaya DOKUNMADI**, yalnız `"evrak"` anahtarını `Moduller.Tumu` ve
`Varsayilan`'a ekledi. Regresyon testi (`Mevcut_kiracida_yeni_modul_200`) ve M29-09 mutasyonu
yine de yazıldı — mekanizma 29-A'nın uçlarında **ölçülüyor**. Sıralama kilidi (paket 3/6/46)
bu yüzden 29-A ile **açılmış sayılır**.

### 3.2 Çift giriş benzersizliği: `banka_ayirt_edici` kolonu
Spesifikasyon UNIQUE indeksi `(tenant_id, evrak_turu, banka_adi, evrak_no)` üzerinde istiyordu.
MySQL UNIQUE indeksi **çoklu NULL'a izin verir** ve SENET'te `banka_adi` NULL'dur → o indeks
senetleri **hiç yakalamazdı**. Bertan'ın birleşik koşulu "banka_adi yerine sabit bir ayırt edici"
diyordu; `banka_adi`'na `"SENET"` yazmak ekranda **yanlış bilgi** gösterirdi. Bu yüzden
**asla NULL olmayan ayrı bir kolon** (`banka_ayirt_edici` = `BankaAdi ?? evrakTuru`) açıldı ve
UNIQUE oradan kuruldu. Servis içi elle kontrol TOCTOU'ya karşı **ikinci katman olarak duruyor**
(M29-07 iki katmanı da ölçüyor).

### 3.3 `BakiyeGuncelle`: yerel kopya
Talimat "bakiye tek yol `CariModulu.BakiyeGuncelle`" diyordu. **O metot `CariServisi` içinde
`private`'tır** (`CariModulu.cs:1130`) ve `CariModulu.cs` bu pakette DOKUNULMAZ listededir.
Kod tabanındaki mevcut âdet: `EmanetModulu.cs:281` ve `SatisModulu.cs:1763` **aynı sözleşmede
yerel kopya** tutuyor. 29-A bu deseni izledi (delta + çağıranın transaction'ı, `SaveChanges`
çağırmaz, döviz kolonuna dokunmaz). **AÇIK kalem:** `BakiyeGuncelle`'nin tek yere toplanması
ayrı bir paket işidir (bkz. §11).

### 3.4 Tahsil bacağının hareket TÜRÜ: `TAHSILAT`
`Sabitler.HareketTurleri`'ne yalnız `EVRAK_GIRIS · EVRAK_KARSILIKSIZ · EVRAK_CIRO` eklendi (talimat
böyle). Evrağın **paraya dönen** bacağı bu yüzden mevcut `"TAHSILAT"` türüyle yazılıyor. Bu
**MASAK parite şartının doğrudan karşılığıdır**: tahsil bacağı `KapiyaTabiTurler` içinde zaten
olan `TAHSILAT` ile **birebir aynı** ölçülür, yeni bir kaçış yolu açılmaz.

### 3.5 `EVRAK_CIRO` türü şimdiden eklendi
Ciro Faz B'dedir ve **kod yazılmadı**; tür `Entities.cs`'e belge değeri olarak kondu ki Faz B bu
dosyaya ikinci kez dokunmasın (TEK DOKUNUŞ kuralı). `DuzeltmePolitikasi`'nda `STORNO_YOK` damgalı.

### 3.6 Mevzuat: hiçbir "DOGRULANMALI" hüküm koda gömülmedi
5941 s. Çek Kanunu ibraz süreleri (10/30/60 gün), TTK md. 776/780 zorunlu unsurlar ve
"çekin tevsik edici vasıta sayılması" — **hiçbiri koda yazılmadı**. Keşide yeri/tarihi, borçlu adı
ve kimlik no Faz A'da **opsiyoneldir**. İbraz süresi uyarısı yoktur (varsayılan KAPALI bile
değil — hiç yok). Kurucu/mali müşavir notu §11'de.

---

## 4. Derleme, migration, süit ve denetim (kapı çıktıları)

```
$ dotnet build Kuyumcu.sln --nologo
    0 Uyarı
    0 Hata

$ dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo --logger trx
Başarılı!  - Başarısız: 0, Başarılı: 2600, Atlanan: 0, Toplam: 2600, Süre: 34 s

$ bash dotnet/denetim.sh
DENETİM TEMİZ.            (çıkış kodu 0)
```

**Test sayısı:** önce **2520** → sonra **2600** (+**80 yeni test**)
`EvrakTestleri` 27 · `EvrakDurumKapisiTestleri` 28 (tablo güdümlü Theory) · `HttpEvrakTestleri` 12 · `EvrakEkranTestleri` 13.

**Migration:** `20260917232251_KiymetliEvrak`
- `dotnet build` **YEŞİLKEN** ve `--no-build` **OLMADAN** üretildi (§6 tuzağı).
- `.cs` 8 369 bayt, `.Designer.cs` 166 379 bayt — ikisi de > 0, `Up()` gövdesi `kiymetli_evraklar`
  tablosunu + 6 indeksi (biri UNIQUE) içeriyor; okundu.
- `kuyumcu_net`'e uygulandı: `Applying migration '20260917232251_KiymetliEvrak'. Done.`
- `information_schema` doğrulaması: `kiymetli_evraklar` tablosu **VAR (1)**.
- `__EFMigrationsHistory` = **44 uygulanmış migration** (dosya sistemindeki 48 `.cs`'in 4'ü
  15 Eylül'de yarıda kesilmiş, denetim.sh T-5 beyaz listesindeki 0 baytlık artıklardır).

> ⚠ **denetim.sh `bash` ile koşulmalıdır** (shebang `#!/usr/bin/env bash`). `zsh denetim.sh` ile
> koşulduğunda "T-5 beyaz listesi ÇÜRÜDÜ" **yanlış pozitifi** çıkıyor: satır 629'daki
> `for ad in ${BOS_MIGRATION_BEYAZ}` zsh'de satır satır bölünmüyor. Beyaz listedeki dosyaların
> **hepsi diskte mevcut** (elle doğrulandı). Bu bir 29-A regresyonu DEĞİLDİR; betiğin zsh'e
> taşınabilirlik borcudur (bkz. §11 açık kalem A-3).

---

## 5. denetim.sh değişiklikleri (gerekçeleri betiğe yazıldı)

| Satır | Sayaç | Eski → Yeni | Gerekçe |
|---|---|---|---|
| ~66 | `VeresiyeKapanisi` | **2 → 3** | İkinci meşru çağrı `EvrakServisi.Gir`. Kuralın amacı **vadesiz borç DOĞURMAYI** yakalamaktır; evrak girişi borç **KAPATAN** yoldur. `OdemeIzi.Etiketten("VERESIYE", …, null)` ile sayıyı 2'de tutma alternatifi **REDDEDİLDİ** — denetimi kandırmak denetimi öldürür. |
| ~262 | `BEKLENEN_VARLIK` | **46 → 47** | `KiymetliEvrak` IKiraciVarlik uyguluyor. |
| ~453 | `Pages: ModalPencere` | **49 → 50** | `Evraklar.razor` TEK ModalPencere kullanır ve **DÜZENLEME DEĞİL, YIKICI ONAYDIR** (karşılıksız işaretleme). `duzenlenenId` kalıbı sayacı **1'de KALDI** (ikinci düzenleme kalıbı açılmadı). |
| ~477 | `Pages/*.razor` | **37 → 39** | `/evraklar` + `/evraklar/takvim`. |

> **Paket 5 ve 6'nın "VeresiyeKapanisi = 2 değişmez" iddiası ARTIK GEÇERSİZDİR.**
> Sonraki paketler bu sayıyı **3** olarak devralır. Gerekçe betiğe yorum olarak yazıldı.
> **Başka hiçbir sayaç gevşetilmedi.** `IgnoreQueryFilters=0`, `Find/FindAsync=0`,
> `ExecuteUpdate/Delete` beyaz listesi **5'te kaldı** (kiymetli_evraklar üzerinde kullanılmadı),
> `Hareketler.Add=1`, `Pages record tavanı 129` (artmadı).

---

## 6. MASAK parite şartı (Mehmet'in kurul mührü)

`MasakKapisi.cs:167` **`KapiyaTabiTurler` DEĞİŞİKLİK ÖNCESİ tam içeriği:**

```csharp
["SATIS", "ALIS", "HURDA_ALIS", "TAKAS", "ISCILIK", "VERESIYE", "TAHSILAT",
 "DOVIZ_VERESIYE", "DOVIZ_TAHSILAT"]
```

**Uygulanan parite:**

| Evrak bacağı | Yazılan hareket türü | `KapiyaTabiTurler`'de mi | Gerekçe |
|---|---|---|---|
| Evrak GİRİŞİ | `EVRAK_GIRIS` | **HAYIR (K4-D gevşemesi)** | Altta zaten kümülatife sayılmış bir SATIS/VERESIYE var; para el değiştirmiyor. İkinci kez sayılırsa çift sayma. |
| Nakit/banka TAHSİL | `TAHSILAT` (mevcut tür) | **EVET** | `TAHSILAT` ile **birebir aynı** davranış — yeni tür açılmadığı için parite otomatiktir. |
| KARŞILIKSIZ | `EVRAK_KARSILIKSIZ` | **EVET (eklendi)** | `VERESIYE` ile aynı şeydir (borç yeniden doğar) → `VERESIYE` neyse odur. |

"Evrak girişi kümülatife girmez" notu **`MasakKapisi.cs`'deki bilinen-eksik-ölçüm notunun yanına
kod yorumu olarak** yazıldı (üçüncü madde) ve gevşemenin **yalnız girişe ait** olduğu aynı yorumda
vurgulandı. Yaptırım listesi / dış abonelik gerektiren **hiçbir kontrol eklenmedi** (kapsam dışı).

---

## 7. Mutasyon kanıtları — M29-01..M29-11 (ON BİRİ de koşuldu)

Yöntem: kaynak `scratchpad/mut-yedek/p29/` altına kopyalandı → `perl -0777 -pi -e` ile **tek
eşleşmeli** değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → **`cmp` ile birebir aynı
olduğu kanıtlandı**. Her satırda "GERİ ALINDI: cmp birebir aynı" çıktısı alındı.

| Kod | Mutasyon | KIRMIZIYA DÖNEN TEST(LER) — gerçek adlar | Sonuç |
|---|---|---|---|
| **M29-01** | `EvrakDurumKapisi.Dogrula` gövdesi boşaltıldı | `EvrakDurumKapisiTestleri.Izinsiz_gecislerin_tamami_reddedilir` (17 Theory örneği) · `…Bilinmeyen_durum_FAIL_CLOSED_reddedilir` · `…Tahsil_edildi_SON_durumdur_hicbir_gecisi_yoktur` · `EvrakTestleri.Gecersiz_gecis_400` · `HttpEvrakTestleri.Gecersiz_gecis_400_gövde_ile` | **20 başarısız / 10 başarılı** |
| **M29-02** | Girişte `TlTutari = -dto.Tutar` → `+dto.Tutar` | `EvrakTestleri.Giris_cari_bakiyesini_azaltir` · `EvrakTestleri.Giris_sonrasi_acik_borc_duser` | **2 başarısız / 0 başarılı** |
| **M29-03** | `OdemeIzi.VeresiyeKapanisi(dto.Tutar)` → `OdemeIzi.Yok` | `EvrakTestleri.Giris_sonrasi_acik_borc_duser` | **1 başarısız / 0 başarılı** — ⚠ bkz. TEST AÇIĞI notu |
| **M29-04** | Karşılıksızda `VadeKapisi.Hazirla` kaldırıldı, sabit vade yazıldı | `EvrakTestleri.Karsiliksiz_kredi_limitini_asamaz` | **1 başarısız / 2 başarılı** |
| **M29-05** | `KasaDogrula(..., "TRY", now)` → `an` parametresi kaldırıldı (derlenir!) | `EvrakTestleri.Kapanmis_gune_tahsil_yazilamaz` | **1 başarısız / 0 başarılı** |
| **M29-06** | Nakit tahsilde `tevsik.Dogrula` kaldırıldı | `EvrakTestleri.Tevsik_haddi_asan_nakit_tahsil_400` | **1 başarısız / 0 başarılı** |
| **M29-07** | Servis içi elle çift-giriş kontrolü kaldırıldı | `EvrakTestleri.Ayni_senet_iki_kez_girilemez` · `EvrakTestleri.Ayni_banka_ve_cek_no_iki_kez_girilemez` | **2 başarısız / 0 başarılı** |
| **M29-08** | `[Yetki.ModulYetkisi("evrak")]` kaldırıldı | `HttpEvrakTestleri.Yetkisiz_rol_403` | **1 başarısız / 0 başarılı** |
| **M29-09** | `Moduller.TohumlamaOncesi`'ne `"evrak"` eklendi (eski kiracı yeni modülü hiç almaz) | `HttpEvrakTestleri.Mevcut_kiracida_yeni_modul_200` | **1 başarısız / 0 başarılı** — ⚠ bkz. TEST AÇIĞI notu |
| **M29-10** | Liste ucundaki `limit` kıskacı kaldırıldı | `HttpEvrakTestleri.limit_5000_400` · `EvrakTestleri.Limit_200_uzeri_400` | **2 başarısız / 0 başarılı** |
| **M29-11** | `KiymetliEvrak.BorcluKimlikNo` AES converter'ı kaldırıldı | `EvrakTestleri.Kimlik_no_diske_sifreli_iner` · `EvrakTestleri.Hash_ile_arama_bulur_duz_metinle_bulmaz` | **2 başarısız / 0 başarılı** |

### ⚠ İKİ TEST AÇIĞI BULUNDU VE KAPATILDI (Anayasa md.1 — gizlenmedi)

**(a) M29-03 ilk koşumda `Giris_sonrasi_acik_borc_duser`'ı KIRMADI (test YEŞİL kaldı).**
Sebep: test yalnız **toplamı** ölçüyordu. `VadeKapisi.AcikBorclar` bakiyeyi tavan kabul ettiği için
ödeme izi hiç verilmese de toplam 20.000 çıkıyordu. **Düzeltme:** test **iki farklı vadeli parti**
(20.000 → +10 gün, 30.000 → +60 gün) kurup evrak girişinden sonra **kalan borcun VADESİNİ**
doğruluyor (FIFO ile eski parti tükenmeli, geriye +60 günlük 20.000 kalmalı). Mutasyon tekrarlandı →
**KIRMIZI**. İlk (yanlış) çıktı: `Başarısız: 1, Başarılı: 1` (yalnız bakiye testi kırılmıştı).

**(b) M29-09 ilk koşumda `Mevcut_kiracida_yeni_modul_200`'ü KIRMADI.**
Sebep: fikstür eski kiracının satırlarını **sınanan sabitten** (`Moduller.TohumlamaOncesi`)
türetiyordu; mutasyon o sabite `"evrak"` ekleyince fikstür de `"evrak"` satırını yazıyor ve test
yeşil kalıyordu. **Düzeltme:** fikstür `HttpEvrakTestleri.EskiAnahtarlar` adıyla **donduruldu**
(geçmişi anlatan elle yazılı liste, bugünkü sabit değil). Mutasyon tekrarlandı → **KIRMIZI**.
İlk (yanlış) çıktı: `Başarılı: 1, Başarısız: 0`.

Son mutasyondan sonra **TEMİZ derleme (0/0)**, tam süit **2600/2600** ve `DENETİM TEMİZ` yeniden alındı.

---

## 8. Canlı doğrulama (localhost:5198 — kurucunun 5199'una DOKUNULMADI)

Test kiracısı: **`PAKET29-TEST`** (`01M2RW129WTTM6DXNJYX0B0FTZ`) ·
izolasyon için ikinci kiracı **`PAKET29-TEST-B`** (`01M2RW3ZWERSXD8X4170N3FRX9`).
Gövdeler **okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | GERÇEK (HTTP) | SQL kanıtı |
|---|---|---|---|---|
| 1 | Kiracı + kur (10-20 sn bekleme) | kur dolu | `201` kayıt · `/kur/guncel` → `altinHasFiyati 6815.75` | — |
| 2 | 50.000 TL veresiye satış | bakiye +50.000 | `200` | `cari_bakiyeler.tl_bakiye = 50000.00` |
| 3 | **30.000 TL çek girişi** | **201** | **`201`** · yanıt `cariTlBakiye: 20000.0` | `kiymetli_evraklar` **1 satır**; `hareketler` `EVRAK_GIRIS` → `tl_tutari=-30000.00`, `veresiye_tutar=30000.00`, `nakit_tutar=0.00`, `vade_tarihi=NULL`; `cari_bakiyeler.tl_bakiye=20000.00` |
| 4 | Borçlu kimlik no diskte | düz metin **DEĞİL** | — | `WHERE borclu_kimlik_no='<numara>'` → **0 satır**; kolon değeri `v1:` ile başlıyor, uzunluk 55; `borclu_kimlik_no_hash` uzunluk 64. **Gerçek numara bu rapora yazılmadı.** |
| 5 | `GET /raporlar/vade-takibi?ufukGun=60` | açık borç 20.000, **vadesiz ARTMAMIŞ** | `200` → `{gecikenToplam:0, yaklasanToplam:20000.0, vadesizToplam:0, acikToplam:20000.0}` | — |
| 6 | Tahsile ver | 200, durum `TAHSILE_VERILDI` | `200` | durum `TAHSILE_VERILDI` |
| 7 | **İkinci kez tahsile ver** | **400** | **`400`** · gövde: `'TAHSILE_VERILDI' durumundan 'TAHSILE_VERILDI' durumuna geçilemez. İzinli geçişler: PORTFOYDE, TAHSIL_EDILDI, KARSILIKSIZ` | — |
| 8 | Karşılıksız (25 karakter gerekçe) | bakiye **50.000** | `200` · `cariTlBakiye: 50000.00` | `cari_bakiyeler.tl_bakiye=50000.00`; `EVRAK_KARSILIKSIZ` → `tl_tutari=+30000.00`, `veresiye_tutar=30000.00`, **`vade_tarihi=2026-09-18` (DOLU)** |
| 9 | **Aynı banka + çek no ikinci giriş** | **400/409** | **`409`** · gövde: `Bu evrak zaten kayıtlı: CEK · Ziraat · P29-CEK-1` | — |
| 10 | İkinci çeki **nakit tahsil** (5.000 TL) | kasa artar | `200` | `kasalar.bakiye=5000.00`; `kasa_hareketleri` 1 satır `5000.00 / NAKIT`; `hareketler TAHSILAT` → `tl_tutari=5000.00`, `nakit_tutar=5000.00`, **`veresiye_tutar=0.00`**; `cari_bakiyeler.tl_bakiye` **DEĞİŞMEDİ (45000.00)** → çift sayma YOK |
| 11 | Kasa günü kapat | 200 | `200` · `{yerelGun:2026-09-18, fark:0.00, kilitli:true}` | — |
| 12 | **Kapalı güne nakit tahsil** | **400 gün kilidi** | **`400`** · gövde: `18.09.2026 günü bu kasa için KAPATILMIŞTIR (sayılan 7.000,00 TL, fark 0,00 TL). Kapanmış güne hareket yazılamaz…` | — |
| 13 | **Başka kiracı `GET /evraklar/{id}`** | **404** | **`404`** · gövde `Evrak bulunamadı` | — |
| 13b | Başka kiracı `POST …/portfoye-al` | 404 | `404` | — |
| 14 | `GET /evraklar/takvim` | GÜN toplamı | `200` → `{toplamAdet:1, toplamTutar:1500.00, gunler:[{gun:2026-10-13, adet:1, tutar:1500.00}]}` — **satır değil GÜN** | — |
| 15 | Takvim > 366 gün | 400 | `400` | — |
| 16 | `GET /evraklar?limit=5000` | 400 | `400` | — |
| 17 | `PATCH` portföyde OLMAYAN evrak | 400 | `400` | — |
| 18 | Hash ile arama | doğru kimlikle 1, yanlışla 0 | `200` → `bulunan=1` / `yanlis_kimlikle=0` | — |
| 19 | Nakit tahsil, **kasa seçilmeden** | 400 | `400` · `Nakit tahsil için kasa seçimi gerekli` | — |

**Temizlik:**
- API PID 75439 `kill` ile durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` = **0 satır**.
- **5199 (kurucunun inceleme sunucusu) çalışmaya devam ediyor — DOKUNULMADI** (`dotnet PID 67515`).
- Her iki test kiracısı tenant_id kolonu olan TÜM tablolardan silindi; `PAKET29-TEST%` davetleri silindi.
- `SELECT COUNT(*) FROM tenants` = **8** (başlangıçtaki değer). `kiymetli_evraklar` = **0 satır**.
- `QA-TEST-*` fikstürlerine (QA-TEST-1/2/3) **DOKUNULMADI**.
- ⚠ `betikler/kiraci-temizle.sh` **"Kiracı YOK" dedi ve silemedi**: betiğin parola çıkarımı
  (`grep MYSQL_ROOT_PASSWORD | head -1`) docker-compose.yml'deki **3. satırdaki YORUMU** yakalıyor.
  Temizlik betiğin **aynı güvenlik kontrolleriyle** elle yapıldı (`QA-TEST-*` reddi + `PAKET29-TEST`
  ad eşleşmesi zorunluluğu). Betik hatası §11 A-2'de açık kalem.

---

## 9. DoD sözleşmesi (Bertan koşulu — bağlayıcı)

### Yeni varlık: `KiymetliEvrak`
| Gereklilik | Karşılanma |
|---|---|
| PATCH (gönderilmeyen alan = koru, null = sil) | `PATCH /api/v1/evraklar/{id}` — `EvrakDuzeltDto` her alanda `…Verildi` bayrağı taşır (CariGuncelleDto deseni). Testler: `Gonderilmeyen_alan_KORUNUR_null_lanmaz` (DB), `Patch_gonderilmeyen_alani_KORUR` (HTTP). |
| DELETE ya da pasife alma ucu | **HARD DELETE YOK** (finansal varlık). Yaşam döngüsü yalnız durum geçişidir; geri alma yolu `…/karsiliksiz` (borç geri doğar) ve giriş hareketinin stornosudur. `DuzeltmePolitikasi`'nda yazılı. |
| `[ModulYetkisi]` / `[RolGerekli]` | Sınıf düzeyi `[Yetki.ModulYetkisi("evrak")]`; yazma uçlarında `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` TAM eşleşme. |
| Denetim kaydı | `DenetimServisi.YazmaKaydet` **ÇAĞRILIR** (yeniden yazılmadı), `VarlikTuru="KiymetliEvrak"`. Test: `Her_durum_degisimi_denetim_kaydi_birakir`. |
| Ekranda Düzenle / gerekçe | `/evraklar` satırında `Düzenle` düğmesi (PORTFOYDE'de aktif, aksi hâlde `disabled` + sebep); tutar ve kaynak cari için `DegismezIpucu`. |

### Hangi alan düzenlenebilir, hangisi neden salt okunur

| Alan | Düzenlenebilir mi | Gerekçe / doğru yol |
|---|---|---|
| `vadeGunu`, `evrakNo`, `bankaAdi`, `bankaSube`, `kesideYeri`, `kesideTarihi`, `borcluAd`, `borcluKimlikNo`, `notlar` | **EVET** — PORTFOYDE durumunda, alan bazlı PATCH | Bunlar evrağın **tanımlayıcı** bilgisidir; deftere yazılmadılar. |
| `tutar` | **HAYIR** | Deftere yazılmış bir borç-kapama satırını doğurdu; değiştirilseydi bakiye ile defter ayrışırdı. **Doğru yol:** karşılıksız işaretle (borç geri doğar) ya da giriş hareketini storno et, sonra doğru evrakı yeniden gir. Ekranda `DegismezIpucu` ile yazılı. |
| `kaynakCariId` | **HAYIR** | Aynı gerekçe (hareketin `cari_id`'si ve bakiye deltası ona bağlı). |
| `durum` | **HAYIR (doğrudan)** | Yalnız `EvrakDurumKapisi`'nden geçen adanmış uçlarla değişir. |
| Tahsile verme / tahsil / karşılıksız damgaları | **HAYIR** | Olayın kendisidir; düzeltme yolu ters durum geçişidir. |
| PORTFOYDE **dışındaki** durumlarda hiçbir alan | **HAYIR** | Çek numarası bankaya bildirilmiş bir gerçektir. 400 + ekranda `disabled` + sebep. |

**QA turu 4 kapısı: `/evraklar` için düzenlenemeyen kalem = 0** — her kalem ya düzenlenebilir ya da
gerekçesi + doğru yolu ekranda yazılı.

### DoD EK (kurul tur 1)
1. **Her düğme ya istek atar ya disabled + sebep taşır.** bUnit testi:
   `Son_durumdaki_evrakta_TIKLANINCA_HICBIR_SEY_OLMAYAN_dugme_YOKTUR` (son durumda eylem düğmeleri
   **hiç çizilmez**, yerine `#evrak-son-durum-{id}` açıklaması çıkar; `Düzenle` disabled + `title`).
   `denetim.sh` "ölü düğme" sayacı **artmadı**.
2. **TEK DÜZENLEME KALIBI:** giriş ve düzeltme **aynı `Cekmece`** bileşeninde, **aynı alan kümesiyle**
   (UX-S26). `ModalPencere` **yalnız** karşılıksız onayı için. `duzenlenenId` kalıbı sayacı **1'de kaldı**
   (29-A kendi alanını `cekmeceEvrakId` adlandırdı — ikinci kalıp açılmadı).
3. **Sessiz etkisizlik yok:** eksik alanda Kaydet **kilitli** ve sebebi çekmece altında yazılı;
   alan altı Türkçe mesajlar (`#evrak-form-*-hata`). Test: `Eksik_alanla_gonderim_SESSIZ_ETKISIZ_degildir`.
4. **Yıkıcı eylem** (karşılıksız) → ortak onay modalı + ≥20 karakter gerekçe (servis içinde de kontrol)
   + geri dönüş yolu (`TAHSIL_EDILDI` geçişi izinli, ekranda görünür).

### DoD EK 2 (mühür 2)
- **Dokunulan sayfalar (bu dalga):** `Pages/Evraklar.razor` (YENİ) · `Pages/EvrakTakvimi.razor` (YENİ) ·
  `Layout/KenarCubugu.razor` (menü, 1 blok) · `Layout/RotaBasliklari.cs` (2 satır).
  **`Pages/Cariler.razor` AÇILMADI.**
- **Storno taklidi YOK:** "ters iade" üretilmedi; geri alma yolu karşılıksız + 48-A motorudur.
- **Fiyat/teklif/tahsilat pazarlama metni üretilmedi.**

---

## 10. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Kapsam DONDURULDU (yalnız Faz A "İçinde" maddeleri) | **UYULDU** | Ciro/VERİLEN/döviz/ibraz/görsel/reeskont/icra **kod yazılmadı** |
| `KiymetliEvrak : IKiraciVarlik` + 6 indeks | **UYULDU** | migration `Up()`; UNIQUE `banka_ayirt_edici` üzerinden (§3.2) |
| `GET /evraklar/{id}` tenant şartı ELLE de yazıldı (ADR-0010) | **UYULDU** | `EvrakBul(id, tenantId)`; test `Baska_kiracinin_evraki_gorulemez…` + canlı 404 |
| `kiymetli_evraklar` üzerinde `ExecuteUpdate/Delete` YOK | **UYULDU** | denetim.sh beyaz liste **5'te kaldı** |
| Defter tekeli: `HareketFabrikasi.Ekle` | **UYULDU** | `Hareketler.Add` sayacı **1** |
| `VadeKapisi.cs` / `HareketFabrikasi.cs` / `CariModulu.cs` DOKUNULMADI | **UYULDU** | üçü de değişmedi |
| `Sabitler.HareketTurleri` +3, `OdemeTurleri` DEĞİŞMEDİ | **UYULDU** | `Entities.cs` |
| Giriş: `-tutar` + `VeresiyeKapanisi` + `VadeTarihi=null` | **UYULDU** | canlı SQL §8/3 |
| Karşılıksız: `+tutar` + `Veresiyeden(vade)` (VadeKapisi'nden) | **UYULDU** | canlı SQL §8/8 |
| Tahsil: `Nakitten`, veresiye payı **0** | **UYULDU** | canlı SQL §8/10 (`veresiye_tutar=0.00`, bakiye değişmedi) |
| Evrakta hard DELETE yok | **UYULDU** | yalnız durum geçişi |
| Durum makinesi TEK KAPI, geçiş tablosu birebir | **UYULDU** | `EvrakGecisleri.Izinli`; M29-01 |
| `KasaDogrula(..., "TRY", an)` — `an` zorunlu | **UYULDU** | M29-05 |
| `TevsikKapisi.Dogrula` nakit tahsilde, transaction'dan ÖNCE | **UYULDU** | M29-06; fail-closed testi durumun değişmediğini de ölçüyor |
| `kurSnapshotId` + `KurDogrula` para hareketi doğuran her uçta | **UYULDU** | giriş / tahsil / karşılıksız |
| Şube null → 400 · TRY dışı → 400 | **UYULDU** | `Sube_yoksa_400`, `TRY_disi_evrak_400` |
| M29-01..11 **ON BİRİ de** koşuldu | **UYULDU** | §7 |
| MASAK parite + `KapiyaTabiTurler` içeriği rapora yazıldı | **UYULDU** | §6 |
| `MasakHashDinleyicisi.cs` önce OKUNDU, GiderPusulasi deseni izlendi, yeni hash yolu AÇILMADI | **UYULDU** | aynı `Doldur` metoduna üçüncü döngü eklendi |
| `evrak` modülü TEZGAHTAR'a AÇILMADI | **UYULDU** | `Varsayilan` sözlüğü; `Yetkisiz_rol_403` |
| SAHİP'in kaldırdığı yetki tohumlamadan sonra da 403 | **UYULDU** | `Sahibin_kaldirdigi_yetki_yeniden_tohumlamadan_sonra_da_403` |
| Pages'te **yeni istemci record'u yok** | **UYULDU** | tavan 129'da kaldı; tipler `Kuyumcu.Core/Sozlesmeler/EvrakSozlesmeleri.cs` |
| Sunucu decimal → istemci decimal (int yok) | **UYULDU** | `EvrakSatiri.Tutar`, `EvrakTakvimGunu.Adet/Tutar`, `EvrakListesi.ToplamAdet` **hepsi decimal** |
| Liste zarfı `{toplamAdet, toplamTutar, satirlar[]}` | **UYULDU** | `Liste_zarfi_toplamlari_SUNUCUDA_toplar` |
| Takvim GROUP BY toplamı, limit > 200 → 400 | **UYULDU** | canlı §8/14-16; M29-10 |
| Metinler yalnız `Sozlukler.cs` `SozlukEvrak`'tan | **UYULDU** | Razor'da çıplak metin yok; TR + EN dolu |
| `RotaBasliklari` + menü | **UYULDU** | `RotaBaslikTestleri` 37 → **39 rota** (test adı da güncellendi) |
| ModalPencere her açılışta durumu SIFIRLAR, Escape kapatır | **UYULDU** | `Acildi="KarsiliksizSifirla"`; Escape `ModalPencere`'nin kendi sözleşmesi |
| Karşılıksız ≥20 karakter + onay (servis içinde de) | **UYULDU** | `Karsiliksiz_gerekcesi_20_karakterden_kisa_olamaz`; bUnit "istek GÖNDERMEZ" testi |
| bUnit sahte API'ye gerçekçi yanıtlar | **UYULDU** | 6 uç + kasalar/bankalar/cari/kur — `_ => "{}"` tuzağı kapalı |
| denetim.sh yalnız izinli iki sayaç (+2 DoD tavanı) gerekçeyle | **UYULDU** | §5 |
| Test kiracısı silindi, `tenants` başlangıç değerine döndü | **UYULDU** | §8 temizlik, 8 kiracı |
| COMMIT YOK | **UYULDU** | `git` yazma komutu hiç çalıştırılmadı |

**Karşılanamayan koşul:** yok.

---

## 11. AÇIK KALANLAR (çözülmedi — gerçek çıktısıyla)

**A-1 · `CariServisi.BakiyeGuncelle` tek yere toplanmadı (ORTA).**
`CariModulu.cs` DOKUNULMAZ olduğu için 29-A, `Emanet`/`Satis`'in yaptığı gibi yerel kopya kullandı.
Bugün kod tabanında **üç** kopya var (Emanet, Satis, Evrak) + `CariServisi`'ndeki `private` asıl.
Bir sonraki bakiye alanı (ör. dördüncü döviz) eklendiğinde biri unutulur. **Öneri:** ayrı bir
temizlik paketi — `BakiyeGuncelle`'yi `internal static` bir `CariBakiyeKapisi`'na taşı, denetim.sh'a
"cari_bakiyeler'e doğrudan atama = 1" sayacı ekle.

**A-2 · `betikler/kiraci-temizle.sh` parola çıkarımı BOZUK (DÜŞÜK ama sessiz).**
Gerçek çıktı: `Kiracı YOK: 01M2RW129WTTM6DXNJYX0B0FTZ` (kiracı gerçekten VARKEN).
Sebep: `grep MYSQL_ROOT_PASSWORD "$KOK/docker-compose.yml" | head -1` docker-compose.yml'nin
**3. satırındaki AÇIKLAMA satırını** yakalıyor; parola boş kalıyor, tüm SQL sessizce başarısız
oluyor ve betik "kiracı yok" diyerek **çıkış 1** veriyor. Silme **yapılmış sanılabilir**.
**Düzeltme (uygulanmadı — betik benim paketimin dosya ayak izinde değil):** `grep "MYSQL_ROOT_PASSWORD:"`.
Bu turda temizlik betiğin aynı güvenlik kontrolleriyle elle yapıldı.

**A-3 · `denetim.sh` zsh altında YANLIŞ POZİTİF verir (DÜŞÜK).**
`zsh dotnet/denetim.sh` → `✗ T-5 beyaz listesi ÇÜRÜDÜ  20260915131748_FiyatListesiVeKampanya.cs`
(dosya diskte MEVCUT). Satır 629/632'deki `for ad in ${BOS_MIGRATION_BEYAZ}` zsh'de satır satır
bölünmüyor (`${=VAR}` gerekli). Betiğin shebang'i `bash` olduğu için `bash denetim.sh` **TEMİZ** döner
ve kapı ölçümü budur. Kapı betiği `kapi.sh` zsh ile çağrılıyorsa denetim çağrısının `bash` olduğu
teyit edilmeli.

**A-4 · Devredildi (Faz B / başka paket) — kod YAZILMADI:**
| Kalem | Nereye |
|---|---|
| Ciro ucu (`POST /evraklar/{id}/ciro`) | **Faz B** (`EVRAK_CIRO` türü ve evrak kolonları hazır) |
| VERİLEN evrak (kendi çekimiz / borç senedimiz) | **Faz B** |
| Döviz cinsinden evrak + kur değerlemesi | **Faz B / K4-G** (bugün TRY dışı 400) |
| İbraz süresi uyarısı (5941 s. Çek Kanunu) | **Faz B** — ADR-0005: yapılandırmadan okunacak, varsayılan KAPALI |
| Çek görseli yükleme | **paket 4** (`Belge.SahipTuru="EVRAK"` sözleşmesi; yeni yükleme yolu açılmadı) |
| Reeskont | **muhasebe kararı yok** |
| İcra / protesto takibi, avukat dosyası | **kapsam dışı** |
| `Cariler.razor` "Evrakları" kutusu | **devredildi** — paket 27/UX-1 ile aynı dosya (yaz-seri). ⚠ **Sunucu ucu HAZIR:** `GET /api/v1/evraklar/cari/{cariId}`. Ekran düğmesi bir sonraki dalgada bağlanmalı (UX-S25: "sunucuda uç var, ekranda düğme yok" borcu **bilinçli ve yazılı**). |

---

## 12. Kurucuya / mali müşavire notlar

1. **459 s. VUK tevsik haddi (30.000 TL)** yalnız evrağın **NAKİT tahsil** bacağında çalışır.
   Evrak GİRİŞİNDE nakit payı 0'dır (para el değiştirmiyor) — bu fiilî durumdur, mevzuat metni
   **koda gömülmedi**.
2. **MASAK kümülatifi:** evrak GİRİŞİ bağlantılı işlem kümülatifine **GİRMEZ** (çift sayma önlemi,
   K4-D). Bu **gevşek yönde** bir ölçüm kararıdır, bir mevzuat yorumu değildir; kodda ve bu belgede
   açıkça yazılıdır. Tahsil ve karşılıksız bacakları **girer**. Mali müşavir teyidi istenir.
3. **5941 s. Çek Kanunu ibraz süreleri (10/30/60 gün) koda KONMADI.** Karşılıksız işaretlemesi
   tamamen **kullanıcı beyanıdır**; sistem hiçbir süre hesaplamıyor, uyarı vermiyor.
   İstenirse ADR-0005 uyarınca yapılandırmadan okunur ve **varsayılan KAPALI** olur.
4. **TTK md. 776/780 zorunlu unsurlar** (keşide yeri/tarihi, borçlu adı) Faz A'da **opsiyoneldir**.
   Zorunlu kılınması isteniyorsa önce madde metni doğrulanmalıdır.
5. **Borçlu kimlik numarası** ADR-0003 ile diske **şifreli** iner; arama yalnız HMAC hash üzerinden
   yapılır. `evrak` modülü bu yüzden **TEZGÂHTAR'a varsayılan olarak AÇILMAZ** (Mehmet'in mührü).
   Sahibi yetki ekranından açabilir; açarsa tezgâhtar kimlik numarası görür.
6. **Döviz cinsinden çek/senet** kabul edilmiyor (TRY dışı 400). Yetkisiz kuyumcuya döviz alım-satımı
   yasağı gözetilerek kapsam dışı bırakıldı.

---

## 13. Değişen / eklenen dosyalar (29-A'nın ayak izi)

**YENİ (8):**
```
dotnet/Kuyumcu.Api/Data/KiymetliEvrak.cs
dotnet/Kuyumcu.Api/Modules/Evrak/EvrakDurumKapisi.cs
dotnet/Kuyumcu.Api/Modules/Evrak/EvrakModulu.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260917232251_KiymetliEvrak.cs (+ .Designer.cs)
dotnet/Kuyumcu.Core/Sozlesmeler/EvrakSozlesmeleri.cs
dotnet/Kuyumcu.Web/Pages/Evraklar.razor
dotnet/Kuyumcu.Web/Pages/EvrakTakvimi.razor
dotnet/Kuyumcu.Tests/{EvrakTestleri,EvrakDurumKapisiTestleri,HttpEvrakTestleri,EvrakEkranTestleri}.cs
```

**DEĞİŞTİRİLEN (11):**
```
dotnet/Kuyumcu.Api/Data/Entities.cs                       (Sabitler.HareketTurleri +3)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs               (DbSet + 6 indeks + AES converter)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContextModelSnapshot.cs  (EF üretti)
dotnet/Kuyumcu.Api/Data/MasakHashDinleyicisi.cs           (borçlu kimlik hash'i)
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs                  (AddScoped<EvrakServisi>)
dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs           ("evrak" anahtarı — mekanizmaya dokunulmadı)
dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs           (K4-D notu + EVRAK_KARSILIKSIZ paritesi)
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs     (KiymetliEvrak + 3 hareket türü damgası)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                     (SozlukEvrak + Nav.Evraklar/EvrakTakvimi, TR+EN)
dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor               (menü bloğu)
dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs               (2 rota)
dotnet/denetim.sh                                         (4 sayaç, gerekçeleri betikte)
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs                (37 → 39, test adı da güncellendi)
```

> `git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı bu listeden **çok daha uzundur**:
> depo zaten geniş bir değişmiş/izlenmeyen ağaçla devralındı (bkz. hafıza notu "bin/obj commit'li").
> Yukarıdaki liste **yalnız 29-A'nın dokunduğu** dosyalardır.

---

## 14. Kapı özeti

| Ölçüm | Değer |
|---|---|
| `dotnet build Kuyumcu.sln` | **0 uyarı · 0 hata** |
| `dotnet test` (tam süit) | **2600 / 2600 · 0 başarısız · 0 atlanan** |
| Yeni test | **+80** (2520 → 2600) |
| `bash dotnet/denetim.sh` | **DENETİM TEMİZ** (çıkış 0) |
| Migration | **44 uygulanmış** · yeni: `20260917232251_KiymetliEvrak` |
| Mutasyon | **11 / 11 kanıtlandı** (2 test açığı bulundu ve kapatıldı) |
| Canlı doğrulama | **19 adım · hepsi beklenen HTTP + SQL** |
| Test kiracısı | **silindi** · `tenants = 8` (başlangıç) |
| Commit | **YOK** |
