# Paket 48 — Faz A uygulama raporu (denetim izli storno/düzeltme motoru + bağlam damgası)

> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Dalga 2 · Tarih: 2026-09-16
> Durum: **BİTTİ (Faz A kapsamıyla; sapmalar aşağıda)** · Kanıtlar bu raporda.

## 1. Yapılanlar

### Veri modeli (2 migration)
| Migration | İçerik | kuyumcu_net'e uygulandı |
|---|---|---|
| `20260916172636_DenetimBaglamDamgasi` | `denetim_kayitlari.sube_id`, `.kasa_id` (VARCHAR(36) NULL) + `IX_denetim_kayitlari_tenant_id_sube_id_tarih` | ✔ (`dotnet ef database update`, SHOW COLUMNS ile doğrulandı) |
| `20260916172703_HareketStornoKaynagi` | `hareketler.storno_kaynak_id` (VARCHAR(36) NULL) + `IX_hareketler_tenant_id_storno_kaynak_id` | ✔ |

`storno_kaynak_id`, `iade_kaynak_hareket_id`'den AYRI kolondur: iade ticari olgudur (kalan iade hakkını tüketir, ciroda görünür), storno hata düzeltmesidir (net etkisi sıfır). Aynı kolona binseydi bir kayıt hatası müşterinin iade hakkını yer ve ciro raporunu şişirirdi (Eray kapısı).

### Bağlam damgası
- `DenetimKaydi.SubeId/KasaId` + `DenetimServisi.Olustur(..., subeId, kasaId)` (sonda opsiyonel → mevcut çağrılar bozulmadı).
- `DenetimYazmaDinleyicisi`: AÇIK, tip güvenli `switch` (Hareket → şube · Kasa → şube+kasa · KasaHareket → kasa · GunKapanisi → şube+kasa · Sube → kendi). **Refleksiyon kullanılmadı.** Varlıktan şube çıkmazsa oturumun `subeId` claim'i; o da yoksa **NULL** — varsayılan şube ATANMAZ.
- `DenetimServisi.OkumaKaydet/ListeOkumaKaydet/YazmaKaydet` oturumun şubesini damgalar; `YazmaKaydet` ayrıca kasa damgası alabilir.
- `GET /api/v1/denetim`: `subeId`/`kasaId` süzgeçleri + yanıtta `subeId/subeAd/kasaId/kasaAd`. `GET /denetim/ozet`: `subeler` kırılımı (damgasızlar `subeId=null` kaleminde; "(bilinmiyor)" uydurulmaz).

### Storno motoru (YENİ dosyalar)
- `Modules/Satis/StornoServisi.cs` — `POST /api/v1/islemler/{hareketId}/storno`, `[RolGerekli("SAHIP","MUDUR")]`, sınıf düzeyinde `[ModulYetkisi("islem")]` + `[Authorize]` (SatisController).
  - Beyaz liste: **ISCILIK · VERESIYE · TAHSILAT · EMANET_GIRIS · EMANET_CIKIS**. Liste dışı → 409, mesaj doğru yolu söyler.
  - Çift storno yasak (kaynak zaten stornolanmış → 409; storno satırının kendisi → 409).
  - Gerekçe ≥ 20 / ≤ 1000, **servis içinde elle** doğrulanır (DTO DataAnnotations yalnız controller yolunda çalışır).
  - Kur uydurulmaz: yeni `kurSnapshotId` zorunlu (`KurDogrula`).
  - Kasa bacağı **kaynağın `kasa_hareketleri` satırlarından türetilir**; istemcinin gönderdiği `kasaId` yalnız karşılaştırılır (uyuşmazlık → 400). `KasaDogrula(an=now)` ile K6 para birimi + **gün kilidi bugüne** uygulanır.
  - Kaynağın günü KAPALIYSA storno yalnız SAHİP'e açıktır (Mehmet koşulu); denetim detayına `KAPALI GÜN STORNOSU` işareti düşer.
  - Ters kayıt `HareketFabrikasi.Ekle`'den geçer (denetim.sh `Hareketler.Add` sayacı 1'de kaldı): `TlTutari=-kaynak`, `HasGrami=-kaynak`, tür KORUNUR, `Aciklama="STORNO: <gerekçe>"`, `StornoKaynakId=kaynak.Id`.
  - Ödeme izi TERSLENİR, sıfırlanmaz: kaynağın nakit/veresiye payları taşınır; kaynak borç doğurduysa ters satır borç kapatır (`vade=null`), kaynak borç azalttıysa ters satır borç doğurur ve `VadeKapisi.VadeHesapla` ile vade konur. Borcu ARTIRAN yönde `KrediLimitiDogrula` çalışır.
  - Yan etkiler ters alınır: kasa (bakiye − bacak, işaretli ters `kasa_hareketleri` satırı) · cari bakiye (türe göre AÇIK switch; bilinmeyen tür default dalında reddedilir).
  - `DenetimServisi.YazmaKaydet(islem,"Storno",…, hassas:true)` **aynı transaction'da, SaveChanges'tan önce** (ADR-0021: çağrılır, yeniden yazılmaz).
  - **REDDEDİLEN deneme** `DenetimServisi.RedKaydet` ile **transaction DIŞINDA** yazılır (Mehmet/ADR-0004: geri alınan denemede iz kaybolmasın).
- `Modules/Satis/StornoSuzgeci.cs` — `(kaynak, ters)` ÇİFTİNİ dışlayan tek `Where`. `TevsikKapisi.GunlukNakitToplam` ve `MasakKapisi.GunlukIslemToplam` bunu çağırır.
- `DuzeltmePolitikasi`: ISCILIK/EMANET_GIRIS/EMANET_CIKIS damgası `STORNO_YOK` → `DEFTER` + `HedefUc=api/v1/islemler/{hareketId}/storno`; VERESIYE/TAHSILAT'ta storno birincil, düzeltme fişi `AlternatifYol`. TAKAS/HURDA_ALIS/TRANSFER `STORNO_YOK` olarak KALDI (motor bunları kabul etmiyor — "sunucuda uç var, ekranda düğme yok"un tersi de kabul edilmez, UX-S25).

### Ekran (Ertuğ)
- `Pages/DenetimIzi.razor`: tabloya **Şube** ve **Kasa** sütunları (damgasız satır `—`), filtre satırına `#f-sube` (UX-S22: tek şube varsa seçici GİZLENİR, `#f-sube-tek` salt okunur satırı çıkar), sorguya `subeId`. Mevcut id'ler (`#f-baslangic`, `#f-bitis`, `#f-eylem`, `#f-varlik`) DEĞİŞMEDİ. Şube listesi alınamazsa hata bandı çıkar, "kayıt yok"a çevrilmez (UX-S28).
- Yeni düğme eklenmedi → "tıklanınca hiçbir şey olmayan düğme" riski yok. Storno DÜĞMESİ bu pakette bilinçli olarak yazılmadı (Eray kararı: 2c'de UX-1-C sayfalarında bu uca bağlanacak).
- **Dokunulan sayfalar (tek dokunuş kaydı):** `Kuyumcu.Web/Pages/DenetimIzi.razor`.

## 2. Sapmalar ve gerekçeleri (Anayasa md.1)

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | **Faz B (basım izi / `DenetimEylemi.Basim` / `POST /denetim/basim` / `PusulaBelgesi.razor`) YAPILMADI.** | Görev kapsamı "yalnız Faz A"; Faz B/C dalga 8'e bırakıldı (orkestratör talimatı). |
| S2 | **Storno beyaz listesinden TAKAS, HURDA_ALIS, TRANSFER ÇIKARILDI** (409 + açıklayıcı mesaj). | Bu türler stok + maliyet partisi geri alması gerektirir; partisi sonradan tüketilmiş malı bugünkü maliyetle geri koymak kâr-zararı bozar (spesifikasyonun kendi REDDEDİLMİŞ alternatifi) ve kısmi geri alma yapılmaz. Aşkın'ın koşulu TRANSFER'in çıkarılmasını zaten şart koşmuştu (karşı şube gün kilidi tanımsız). **Kalan üç tür açık borçtur** (§7). |
| S3 | **Tevsik ve MASAK kapıları TERS SATIRA uygulanmadı**; bunun yerine iki kümülatife StornoSuzgeci uygulandı. | Kapı ters satıra uygulansaydı 25.000 TL'lik bir nakit hatanın stornosu "aynı gün 50.000 TL nakit" sayılır ve hata **düzeltilemez** olurdu. Spesifikasyonun kendi canlı doğrulama adımı stornonun BAŞARILI olmasını bekliyor. Storno yeni bir müşteri işlemi değil, var olanın hükümsüz kılınmasıdır; kapılar kaynakta çalıştı. Gün kilidi, K6 para birimi, rol, kiracı ve kredi limiti kapıları TAM uygulanıyor. → **Mali müşavir teyidi isteniyor** (§8). |
| S4 | `Bilesenler/DuzeltAkisi.razor` iskeleti OLUŞTURULMADI. | Spesifikasyonun dosya ayak izi bu dosyayı `DOKUNULMAZ (UX-1'in)` diye işaretliyor; bağlanmayan, testsiz bir bileşen UX-1 Faz D ile çakışır. Politika haritası (`HedefUc`) ekranın bağlanacağı sözleşmeyi zaten veriyor. |
| S5 | `HurdaModulu` "yazdırıldı" metni DEĞİŞTİRİLMEDİ. | **Spesifikasyonun "mevcut durum" iddiası artık geçersiz:** kod bugün zaten `"gider pusulası belgesi görüntülendi"` yazıyor ve ayrı bir `POST {id}/yazdirildi` ucu var (UX-4d madde 20 ile kapanmış). Yapılacak iş kalmadı. |
| S6 | `GET /denetim` süzgeçleri imzanın SONUNA eklendi (`subeId`, `kasaId` varsayılan null). | Mevcut konumsal çağrılar (üç test dosyası) kırılmasın; sözleşme genişledi, değişmedi. |
| S7 | Rol kümesi SAHIP+MUDUR (orkestratör notundaki MUHASEBE alınmadı). | Spesifikasyonun uç tablosu SAHIP/MUDUR diyor; Mehmet'in kapalı-gün koşulu MUDUR'a bile kısıt getiriyor. Dar olan seçildi (fail-closed). |
| S8 | Çok kasalı (çok bacaklı fiş) hareketlerin stornosu 409. | Tek kasalı geri alma kanıtlanabiliyor; çok bacaklı fişte kısmi geri alma riski var. Fail-closed. |

Mevzuat: koda hiçbir eşik/hüküm gömülmedi. Tevsik (30.000) ve MASAK (185.000) eşikleri eskisi gibi yapılandırmadan okunuyor; bu paket yalnız **çift saymayı** düzeltti.

## 3. Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Hata, 0 Uyarı** (son koşum: mutasyonlar geri alındıktan sonra).

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Önce (bu paketten önceki süit) | 1473 | 1473 | 0 |
| `DenetimDamgaTestleri` (YENİ, DB) | 7 | 7 | 0 |
| `StornoTestleri` (YENİ, DB) | 15 | 15 | 0 |
| `StornoHttpTestleri` (YENİ, HTTP) | 9 | 9 | 0 |
| `DenetimIziEkranTestleri` (bUnit, +4 yeni) | 12 | 12 | 0 |
| Duzeltme/Denetim/Tevsik/Masak regresyonu | 242 | 242 | 0 |
| **TAM SÜİT** (`trx-p48`) | **1508** | **1508** | **0** |

Yeni test sayısı: **35**.

## 5. Mutasyon kanıtları
Yöntem: kaynak scratchpad'e kopyalandı → tek eşleşmeli `perl -0777 -pi -e` → filtreli koşum → yedek geri kopyalandı → `cmp` ile birebir aynı olduğu kanıtlandı.

| Kod | Mutasyon | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M48-1 | Dinleyicideki şube çıkarımı kaldırıldı (damga hep null) | `DenetimDamgaTestleri.Hareket_yazilinca_sube_damgalanir` (+2) | ✔ aynı |
| M48-2 | "Şube belirlenemezse ilk şube yazılır" | `DenetimDamgaTestleri.Sube_belirlenemezse_NULL_yazilir` | ✔ aynı |
| M48-3 | Çift storno koruması kaldırıldı | `StornoTestleri.Ayni_hareket_iki_kez_stornolanamaz`, `StornoHttpTestleri.Ayni_kayda_ikinci_storno_409` | ✔ aynı |
| M48-4 | Tür beyaz listesi kaldırıldı | `StornoTestleri.SATIS_storno_istegi_409_…` (+3) | ✔ aynı |
| M48-5 | Ters satırın işareti pozitif bırakıldı | `StornoTestleri.Iscilik_stornosu_kasa_bakiyesini_geri_alir` | ✔ aynı |
| M48-6 | StornoSuzgeci tevsik toplamından kaldırıldı | `StornoTestleri.Storno_cifti_tevsik_haddini_ikinci_kez_doldurmaz` | ✔ aynı |
| M48-7 | Süzgeç TEK TARAFLI yapıldı (yalnız ters satır atıldı) | `StornoTestleri.Storno_cifti_MASAK_kumulatifinde_cift_saymaz` + tevsik testi | ✔ aynı |
| M48-8 | `YazmaKaydet` çağrısı silindi | `StornoTestleri.Storno_denetim_izine_gerekceli_yazilir`, `StornoHttpTestleri.Denetim_izinde_storno_kaydi_gorunur_…` | ✔ aynı |
| M48-9 | Gerekçe ≥20 kontrolü (servis + DTO) kaldırıldı | `StornoTestleri.Gerekce_19_karakter_400_20_karakter_gecer`, `StornoHttpTestleri.Gerekce_19_karakter_400` | ✔ aynı |
| M48-10 | Kaynak satır ters kayıt yerine yerinde güncellendi (`TlTutari=0`) | `StornoTestleri.Kaynak_satir_degismez` (+4) | ✔ aynı |
| M48-11 | Gün kilidi atlandı (`KasaDogrula` an parametresiz) | `StornoHttpTestleri.Bugun_kapaliysa_409_kaynak_gunu_kapaliysa_SAHIP_gecer` | ✔ aynı |
| M48-12 | Cari ters etkisi uygulanmadı (kısmi yan etki) | `StornoTestleri.Nakit_tahsilat_stornosu_cari_bakiyeyi_geri_alir` | ✔ aynı |
| M48-13 | Reddedilen deneme izi yazılmadı | `StornoTestleri.Reddedilen_storno_denemesi_de_ize_yazilir` | ✔ aynı |
| M48-14 | Ekranda şube süzgeci sorguya eklenmedi | `DenetimIziEkranTestleri.Sube_secilince_istek_sorgusuna_subeId_eklenir` | ✔ aynı |

Test açığı bulunmadı (her mutasyon en az bir testi kırdı). Son mutasyondan sonra TEMİZ derleme yapıldı.

## 6. Denetim betiği
`dotnet/denetim.sh` → **DENETİM TEMİZ**
`IgnoreQueryFilters 0 · Find/FindAsync 0 · ExecuteUpdate/Delete 4 · Hareketler.Add 1 · VeresiyeKapanisi 2 · IKiraciVarlik 33 · HasQueryFilter 2 · Finansal varlıkta [HttpDelete] 0`

## 7. Canlı doğrulama (localhost:5198, kiracı `PAKET48-TEST`)

| Adım | Beklenen | Gerçek | SQL / gövde kanıtı |
|---|---|---|---|
| Nakit işçilik 25.000 TL | 200 | **200** | `hareketler`: `ISCILIK 25000.00 nakit 25000.00 storno_kaynak_id NULL`; kasa bakiye 25.000 |
| Bağlam damgası | şube/kasa damgalı | **damgalı** | `denetim_kayitlari`: `EKLEME KasaHareket sube=01M2NMVX7QWW… kasa=01M2NMW7A68…`; `EKLEME Hareket sube=… kasa NULL` |
| `POST /islemler/{id}/storno` | 200 + ters kayıt | **200** | gövde `{"stornoHareketId":"01M2NMWSFJ…","tlTutari":-25000.00,"kasaBakiyesi":0.00,"kapaliGunStornosu":false}`; `hareketler`: `-25000.00 … storno_kaynak_id=01M2NMWCTN…`, `iade_kaynak_hareket_id NULL`; `kasa_hareketleri`: `+25000 / −25000` |
| Kaynak satır | değişmemiş | **değişmemiş** | kaynak satır hâlâ `25000.00`, `storno_kaynak_id NULL` |
| Denetim kaydı | yazılır | **yazıldı** | `EKLEME Storno varlik_id=01M2NMWSFJ… sube=… kasa=01M2NMW7A68… detay="ISCILIK stornosu (kaynak …) · gerekçe: …"` |
| Aynı kayda ikinci storno | 409 | **409** | `{"message":"Bu hareket zaten stornolanmıştır…"}`; defterde üçüncü satır YOK |
| Reddedilen deneme izi | yazılır | **yazıldı** | `REDDEDILDI Storno varlik_id=01M2NMWCTN… detay="storno REDDEDİLDİ (zaten stornolanmış) · gerekçe: …"` |
| TEZGAHTAR storno | 403 | **403** | `{"message":"Bu işlem için gerekli rol: SAHIP veya MUDUR"}` |
| Yabancı kiracının hareketi | 404 | **404** | `{"message":"Hareket bulunamadı"}` |
| Storno sonrası yeni 10.000 nakit | 200 (had ikinci kez dolmaz) | **200** | süzgeç olmasaydı 25.000+10.000=35.000 → 400 olurdu |
| Ardından 25.000 nakit daha | 400 (kapı hâlâ çalışıyor) | **400** | `"…toplamı 35.000,00 TL, 30.000,00 TL haddini 5.000,00 TL aşıyor… (günün önceki toplamı 10.000,00 TL)"` |
| `GET /denetim?varlikTuru=Storno&subeId=…` | damgalı satırlar | **2 satır** | `subeAd:"Merkez Şube"`, `kasaId` dolu |

**Temizlik:** API PID 38840 durduruldu, `lsof -nP -iTCP:5198 -sTCP:LISTEN` boş. Test kiracısı `tenant_id` kolonu olan TÜM tablolardan silindi; `tenants` sayısı **7 → 7** (başlangıç değeri). `QA-TEST-` önekli kiracılara DOKUNULMADI. 5199'a dokunulmadı.

## 8. DoD sözleşmesi

| Varlık / kayıt | Düzeltme yolu | Uç | Yetki | Denetim |
|---|---|---|---|---|
| `Hareket` (ISCILIK, VERESIYE, TAHSILAT, EMANET_GIRIS, EMANET_CIKIS) | **STORNO** (ters kayıt) | `POST /api/v1/islemler/{hareketId}/storno` | `[ModulYetkisi("islem")]` + `[RolGerekli("SAHIP","MUDUR")]`; kapalı gün stornosu yalnız SAHİP | `YazmaKaydet` (varlık `Storno`, hassas) + reddedilende `RedKaydet` |
| `Hareket` (SATIS, ALIS) | İADE | `satis-iade` / `alis-iade` | mevcut | mevcut |
| `Hareket` (CARI_DUZELTME, VERESIYE, TAHSILAT — alternatif) | DÜZELTME FİŞİ | `POST /api/v1/cari/{id}/duzeltme` | paket 33 | paket 33 |
| `Hareket` (TAKAS, HURDA_ALIS, TRANSFER, SAYIM_DUZELTME, iadeler) | **SALT OKUNUR — gerekçesi yazılı** (`STORNO_YOK`) | — | — | — |
| `KasaHareket`, `DenetimKaydi`, `GunKapanisi` | değişmez (append-only) | — | — | — |
| `DenetimKaydi.sube_id/kasa_id` | değişmez; yanlış damga düzeltilemez, NULL kalır | — | — | append-only testiyle bekçilenir |

Finansal varlıklarda **hard DELETE yok** (denetim.sh: finansal varlıkta `[HttpDelete]` = 0). Defter yerinde değişmez; hükümsüzlük yalnız ters satırın varlığından okunur.

## 9. Değişen dosyalar
```
dotnet/Kuyumcu.Api/Data/DenetimIzi.cs
dotnet/Kuyumcu.Api/Data/DenetimYazmaDinleyicisi.cs
dotnet/Kuyumcu.Api/Data/Entities.cs
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916172636_DenetimBaglamDamgasi.cs (+ .Designer.cs)
dotnet/Kuyumcu.Api/Data/Migrations/20260916172703_HareketStornoKaynagi.cs (+ .Designer.cs)
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/Denetim/DenetimModulu.cs
dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs
dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs
dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs        (YENİ)
dotnet/Kuyumcu.Api/Modules/Satis/StornoSuzgeci.cs        (YENİ)
dotnet/Kuyumcu.Api/Modules/Tevsik/TevsikKapisi.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                    (TR + EN, 9 yeni üye)
dotnet/Kuyumcu.Web/Pages/DenetimIzi.razor
dotnet/Kuyumcu.Tests/DenetimDamgaTestleri.cs             (YENİ)
dotnet/Kuyumcu.Tests/StornoTestleri.cs                   (YENİ)
dotnet/Kuyumcu.Tests/StornoHttpTestleri.cs               (YENİ)
dotnet/Kuyumcu.Tests/EkranTestleri.cs                    (sahte API + 4 yeni test)
dotnet/Kuyumcu.Tests/TestOrtami.cs                       (HttpContext'li bağlam aşırı yüklemesi)
```
COMMIT YAPILMADI (kurucu kırmızı çizgisi). Depoda önceki oturumlardan kalma `*.mutorj` artıkları var; **bu görevde üretilmedi**, silinmesi kurucuya bırakıldı.

## 10. Koşul karşılama

| Koşul (kaynak) | Durum | Kanıt |
|---|---|---|
| Mehmet — storno 200, ters kayıt SQL'de, denetim kaydı SQL'de, ikinci storno 409, TEZGAHTAR 403, yabancı kiracı 404 | KARŞILANDI | §7 tablosu |
| Mehmet — reddedilen deneme AYRI bağlamda | KARŞILANDI | `RedKaydet`, M48-13, §7 |
| Mehmet — damga yoksa NULL, append-only gevşemez | KARŞILANDI | M48-2, `Damga_append_only_korumasini_BOZMAZ` |
| Mehmet — kapalı gün stornosu yalnız SAHİP | KARŞILANDI | `StornoServisi` rol kapısı; gün kilidi M48-11 |
| Aşkın — ASİMETRİK süzgeç testi (25.000 → storno → 10.000 geçer) | KARŞILANDI | `Storno_cifti_tevsik_haddini_ikinci_kez_doldurmaz` + §7 canlı 400/200 çifti |
| Aşkın — süzgeç mutasyonu İKİ YÖNLÜ | KARŞILANDI | M48-6 (tamamen kaldır) + M48-7 (tek taraflı) |
| Aşkın — TRANSFER belirsiz bırakılmaz | KARŞILANDI (çıkarıldı) | S2 |
| Bertan — TevsikKapisi/MasakKapisi OKUNMADAN değiştirilmez, sorgu şekli rapora yazılır | KARŞILANDI | §11 |
| Eray — "ters iade" taklidi yok, storno ayrı kolon/ayrı iz | KARŞILANDI | `storno_kaynak_id` ayrı; `iade_kaynak_hareket_id` boş (SQL) |
| Eray — storno DÜĞMESİ bu pakette yazılmaz | KARŞILANDI | §1 Ekran |
| MÜHÜR 2 — finansal hard DELETE yok, her yeni uçta yetki+iz, tek dokunuş | KARŞILANDI | §8, denetim.sh, §1 "Dokunulan sayfalar" |
| UX-S22/S25/S28/S31 | KARŞILANDI | tek şube → seçici gizli; politika haritası; hata bandı; sözlük TR+EN |
| Mevzuat hükmü koda gömülmez | KARŞILANDI | eşikler yapılandırmadan; yeni kural eklenmedi |

## 11. Okunan kapı dosyalarının gerçek sorgu şekli (Bertan/Mehmet/Aşkın koşulu)
- `TevsikKapisi.GunlukNakitToplam` (static): yerel gün aralığı → `hareketler` üzerinde **iki ayrık toplam** — (1) `nakit_tutar != null` olanların `SUM(nakit_tutar)`, (2) eski satırlar için `nakit_tutar == null && odeme_turu=='NAKIT'` → `SUM(|tl_tutari|)`. StornoSuzgeci **ortak `gun` sorgusuna** eklendi; iki toplam da çifti görmez.
- `MasakKapisi.GunlukIslemToplam` (static): yerel gün + `KapiyaTabiTurler ∈ {SATIS, ALIS, HURDA_ALIS, TAKAS, ISCILIK, VERESIYE, TAHSILAT}` → `SUM(|tl_tutari|)`. Süzgeç `SumAsync`'ten önce eklendi.
- Mevcut `TevsikTestleri` + `MasakEntegrasyonTestleri`/`MasakBaglantiliIslemTestleri`/`MasakKuralTestleri`/`MasakServisTestleri` süzgeçten sonra **242/242 yeşil** (Denetim/Duzeltme koşumu dâhil).

## 12. Açık kalanlar (envantere)
1. **TAKAS / HURDA_ALIS / TRANSFER stornosu** — stok + maliyet partisi geri alma motoru yazılmadı (409). Faz C-2 olarak ayrı paket + ayrı oy gerekir (dürüst tahmin 1,5-2 gün).
2. **Faz B — basım izi** (`DenetimEylemi.Basim`, `POST /denetim/basim`, `PusulaBelgesi.razor` fail-closed yazdırma, beyaz liste + Mehmet'in "olmayan belgeId → 404" koşulu): dalga 8.
3. **Adet sayan raporlar** storno çiftini 2 sayar — `StornoSuzgeci` yazıldı ama 34-37 raporlarına uygulanmadı (spesifikasyonun kendi kapsam sınırı). Açık borç.
4. **Çok bacaklı fiş (fis_id) stornosu** 409 — ayrı tasarım gerektirir.
5. **Storno ekran düğmesi** dalga 2c'de UX-1-C sayfalarına bağlanacak (politika haritası `HedefUc` hazır).
6. Eski denetim satırları için şube/kasa geriye doldurulmadı (bilinçli; uydurma damga yazılmaz).

## 13. Kurucuya / mali müşavire notlar
- **S3 (tevsik & MASAK kapılarının ters satıra uygulanmaması) mali müşavir teyidi ister.** Bu tasarımda storno "yeni bir nakit işlem" sayılmaz; bir hata düzeltmesinin müşterinin 30.000 TL nakit haddini ya da 185.000 TL MASAK eşiğini ikinci kez doldurmadığı kabul edilmiştir. Aksi görüş gelirse kod değişikliği tek noktadadır (`StornoServisi` içinde kapı çağrısı eklemek).
- **Fatura IPTAL durumu bir storno mudur?** (spesifikasyonun açık sorusu) — bu paket faturaya DOKUNMADI; muhasebe açısından yeterliliği mali müşavir teyidi ister.
- KVKK md.12 ve VUK md.219/253 metinleri bu görevde DOĞRULANMADI; kod hiçbir yasal hükme dayandırılmadı, tasarım ADR-0004 + K-UX-2 mimari kararına dayanıyor.
- "Her basım izlenir" iddiası YAPILMADI (Faz B'ye kaldı ve orada da kapsam sınırı yazılı olacak).
