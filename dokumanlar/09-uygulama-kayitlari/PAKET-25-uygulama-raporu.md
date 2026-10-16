# PAKET 25 — Reyon/vitrin bazlı stok bölümlemesi · FAZ A — uygulama raporu

> Tarih: 2026-09-17 · Dalga 3 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü
> Kaynak spesifikasyon: `dokumanlar/02-mimari/kalan-paketler/PAKET-25-reyon-vitrin-bazli-stok-bolumlemesi.md` (DOKUNULMADI)
> Durum: **BİTTİ (Faz A)** · Faz B (sayım kapsamının yerleşimden okunması) KAPSAM DIŞI bırakıldı — spesifikasyonun kendi faz ayrımı.

---

## 0. Önkoşul doğrulaması (ilk adım)

| Önkoşul | Doğrulama | Sonuç |
|---|---|---|
| Paket 23 (sayım oturumu) | `Modules/Sayim/SayimModulu.cs` + `SayimOturumuTestleri` mevcut ve yeşil; `kalan-paketler/` altında paket 23 spesifikasyonu YOK (bitmiş kümede) | **TAMAM** |
| Paket 24 (ürün ağacı / reyon kartı) | `Data/UrunKategorisi.cs` `reyonlar` tablosu + `KategoriModulu.ReyonServisi` mevcut; `kalan-paketler/` altında paket 24 YOK | **TAMAM** |
| Reyon PATCH / pasife alma uçları (UX-1-B) | `KategoriModulu.cs` → `ReyonController.Guncelle` (PATCH) ve `PasifeAl` (DELETE) ZATEN VARDI — yeniden yazılmadı, **kullanıldı** | **TAMAM** |

---

## 1. Spesifikasyon ↔ kod sapmaları ve gerekçeleri

| # | Spesifikasyon iddiası | Koddaki gerçek | Ne yapıldı |
|---|---|---|---|
| S1 | "Uçlar yalnız GET/POST; PATCH/DELETE YOK (KategoriModulu.cs:282-287)" | PATCH ve DELETE **UX-1-B'de açılmıştı** | Yeniden yazılmadı; mevcut uçlar kullanıldı, yalnız pasife alma kapısına YERLEŞİM koşulu eklendi. |
| S2 | "denetim.sh BEKLENEN_VARLIK 31→32" | Dosyada değer **40** idi (satır 136), karşılaştırma `-ge` | Birleşik koşul uygulandı: mutlak sayı uydurulmadı, **mevcut değer okunarak 40 → 41** yapıldı (paketin eklediği 1 varlık kadar). Gerçek sayı bugün 44. |
| S3 | "Modules/Stok/YerlesimServisi.cs" | `Modules.Stok` ad alanı, `Data.Stok` VARLIK adıyla çakıştı (CS0118: `new Stok{...}` derlenmiyor) | Ad alanı **`Modules/Yerlesim/`** olarak kuruldu. Dosya adları ve içerik spesifikasyondaki gibidir; yalnız klasör/ad alanı adı değişti. Gerekçe: mevcut kodda `new Stok` yazan her yer kırılırdı. |
| S4 | Urun kartında `Maden` kolonu varmış gibi okuma | Maden kolonu **YOK**; `Ayarlar.MadenBul(ayarId)` ile türetiliyor (RaporModulu.StokDegeri deseni) | Maden her yerde ayar kodundan türetildi — ikinci bir kaynak açılmadı. |
| S5 | "yanıtta `yerlesimUyarisi` döner" | Satış/alış yanıtı ham `Hareket` varlığıdır; şekli değiştirmek istemci sözleşmesini kırar (S-2 dersi) | Yanıt şekli **EKLEMELİ** yapıldı: uyarı varsa gövde sunucunun kendi JSON ayarlarıyla düğüme çevrilip yalnız `yerlesimUyarisi` alanı EKLENİR; `id`/`fisId` dâhil mevcut alanların hepsi aynen kalır. Uyarı yoksa yanıt **bit bit aynıdır**. |
| S6 | "Pages altında istemci record'ları" | `denetim.sh` "Pages içinde istemci record tanımı" TAVANI (122) var — yeni istemci record'u yasak | Bütün yanıt şekilleri **`Kuyumcu.Core/Sozlesmeler/YerlesimSozlesmeleri.cs`** dosyasına yazıldı (VezneSozlesmeleri deseni). Pages sayacı 121 ≤ 122. |
| S7 | I1 eşzamanlılığı "iyimser mi kilit mi" açık bırakılmış | Birleşik koşul karar zorunlu kılıyor | **İyimser kilit** seçildi: `StokYerlesimi.Guncellendi` üzerinde `[ConcurrencyCheck]`. ExecuteUpdate/ExecuteDelete KULLANILMADI (beyaz liste 4'te kaldı). Çakışma `HataYakalayici`de **409**'a çevrilir. Mevcut `stoklar` deseni DEĞİŞTİRİLMEDİ — yeni kilit felsefesi getirilmedi. |
| S8 | Sayım bacağı "Faz B" | Faz A'da sayım `stoklar`ı üzerine yazıyor; yerleşim düzeltilmezse I1 **sayım anında bozulurdu** | `StokSayimGuncelle` içine yerleşim bacağı eklendi (fark>0 → varsayılan reyona/atanmamışa, fark<0 → atanmamış önce). Sayım KAPSAMININ reyondan okunması Faz B'de kaldı. |
| S9 | Düzeltme politikası haritası | Yeni varlık haritada olmazsa `DuzeltmePolitikasiTestleri` kırmızı | `StokYerlesimi` **STORNO** damgasıyla eklendi (`TURETILMIS` gerekçesi, hedef uç `reyonlar/tasima`, alternatif `reyonlar/geri-al`). DUZENLENEBILIR seçilmedi: satıra PATCH açmak izsiz düzeltme olurdu; harita testi de DUZENLENEBILIR için PATCH/PUT/DELETE ucu şart koşuyor. |

---

## 2. Veri modeli ve migration

**Migration adı:** `20260917102535_StokYerlesimi`
**Uygulama kanıtı (kuyumcu_net):** `dotnet ef database update` → `Applying migration '20260917102535_StokYerlesimi'. Done.`

```
SHOW COLUMNS FROM stok_yerlesimleri;
id           varchar(36)     NO  PRI
tenant_id    varchar(36)     NO  MUL
sube_id      varchar(36)     NO
urun_id      varchar(36)     NO
reyon_id     varchar(36)     NO
adet         decimal(12,3)   NO
guncellendi  datetime(6)     NO
```

İndeksler: `UNIQUE (tenant_id, sube_id, urun_id, reyon_id)` · `(tenant_id, reyon_id)` · `(tenant_id, sube_id, urun_id)` · **ek:** `reyonlar` üzerinde `UNIQUE (tenant_id, sube_id, ad)` (bugüne kadar yalnız kodda kontrol ediliyordu; eşzamanlı iki POST iki aynı adlı reyon üretebilirdi).

`stoklar` tablosu **DEĞİŞMEDİ**. `urunler.reyon_id` **KALDIRILMADI**; anlamı "varsayılan/ana reyon" olarak daraltıldı ve kodda belgelendi.

**Geçiş verisi dürüstlüğü:** migration'da `urunler.reyon_id`den otomatik yerleştirme YAPILMADI. Tüm mevcut stok **ATANMAMIŞ** başlar ve ekranda açıkça öyle görünür.

---

## 3. Uçlar

| Fiil | Yol | Roller | Kapılar |
|---|---|---|---|
| GET | `/api/v1/reyonlar/{id}/stok` | tüm roller | `ModulYetkisi("urun")` · sayfalı (limit 1-500, varsayılan 200) · `sinirdaMi` alanı |
| GET | `/api/v1/urunler/{id}/yerlesim` | tüm roller | `ModulYetkisi("urun")` · şube toplamı `stoklar`dan, `atanmamis` hesaplanır |
| POST | `/api/v1/reyonlar/yerlesim` | SAHIP · MUDUR · TEZGAHTAR | I1 kapısı · SubeDogrula · reyon şube+aktiflik · transaction · DenetimKaydi |
| POST | `/api/v1/reyonlar/tasima` | SAHIP · MUDUR · TEZGAHTAR | kaynak≠hedef · iki reyon aynı şube + aktif · kaynakta yeterlilik · DenetimKaydi |
| POST | `/api/v1/reyonlar/geri-al` | SAHIP · MUDUR | pasif reyondan da geri alınabilir (boşaltma yolu) · DenetimKaydi |
| PATCH | `/api/v1/reyonlar/{id}` | SAHIP · MUDUR | **mevcut uç** + yerleşim kapısı (içinde mal varsa aktif=false → 400) |
| GET | `/api/v1/raporlar/reyon-stok` | SAHIP · MUDUR · MUHASEBE | `ModulYetkisi("rapor")` · GroupBy SUNUCUDA (TB-19) · madenler ayrı · atanmamış ayrı |
| POST | mevcut `islemler/*` | değişmedi | DTO'lara **opsiyonel** `reyonId` (transferde ayrıca `hedefReyonId`); tevsik/MASAK/vade/gün kilidi DOKUNULMADI |

**İş kuralları (fail-closed):** I1 ihlali 400 · negatif yerleşim yok, 0'a düşen satır silinir · reyon şubesine kilitli · pasif reyona yerleştirme/taşıma 400 · çıkışta reyon verildiyse o reyonda yetersizlik 400 (başka reyondan SESSİZCE alınmaz) · reyon verilmediyse önce ATANMAMIŞ, sonra reyonlar **Ad artan** sırada + uyarı · girişte varsayılan reyon yalnız AYNI şubede ve aktifse kullanılır · reyon içi taşıma deftere/kasaya/maliyet partisine DOKUNMAZ.

---

## 4. DoD sözleşmesi (MÜHÜR 2 + DoD EK, bağlayıcı)

| Varlık | Düzenleme yolu | Ekrandaki karşılığı | Denetim |
|---|---|---|---|
| `Reyon` | PATCH `/reyonlar/{id}` (ad·tip·sorumlu·aktif) + DELETE = pasife alma | `/urunler/agac` satırında **Düzenle · Pasife al · Aktifleştir** (UX-1-C1'de açılmıştı) | `DenetimServisi.YazmaKaydet` ÇAĞRILIR |
| `StokYerlesimi` | **Ters hareketle**: POST `/reyonlar/tasima` · POST `/reyonlar/geri-al` | `/urunler/agac` **Taşı** çekmecesi · `/urunler` stok modalında **Reyona yerleştir** | Her üç yazma ucu da `YazmaKaydet` çağırır (kim · hangi reyondan hangi reyona · kaç adet) |
| `Stok` (şube stoğu) | DEĞİŞMEDİ — salt okunur, `DegismezIpucu` ile gerekçesi ekranda duruyor | mevcut | mevcut |

**Neden `StokYerlesimi`ne PATCH yok (DegismezIpucu gerekçesi):** `adet` elle yazılan bir değer değil, yerleştirme/taşıma hareketlerinin **sonucudur**. Doğrudan yazmak izsiz düzeltme olurdu. Düzeltme yolu **vardır** ve ekrandadır: taşıma ya da atanmamışa geri alma. Bu karar `Modules/Meta/DuzeltmePolitikasi.cs` haritasına STORNO damgasıyla ve **ADR-0016**'ya yazıldı.

**Finansal varlıkta hard DELETE:** YOK. Bu paket `hareketler`/`kasa_hareketleri`/`banka_hareketleri`/`faturalar` tablolarına hiç dokunmaz.

**Her düğme istek atar ya da disabled + sebep:**
`#r-tasi-{id}` (boş/pasif reyonda disabled + `#r-tasi-kilit-{id}` sebebi) · `#cekmece-reyon-tasima-kaydet` (Çekmece'nin `KaydetAktif`/`KilitSebebi` mekanizması: ürün seçilmedi → hedef seçilmedi → adet geçersiz → yetersiz mal) · `#yer-yerlestir-{subeId}` (disabled + `#yer-kilit-{subeId}`) — hepsi bUnit testleriyle tutuluyor.

**Tek düzenleme kalıbı:** yeni düzenleme yüzeyi `Cekmece` ile açıldı (`#cekmece-reyon-tasima`). `denetim.sh` sayaçları: ModalPencere kullanımı 44 (tavan 45, **artmadı**), `duzenlenenId` kalıbı 1 (tavan 1).

**Sessiz form yasağı:** `#tasi-adet-hata` / `#tasi-adet-asim` / `#yer-adet-asim-{subeId}` — alan altı Türkçe mesajlar.

**Dokunulan sayfalar (tek dokunuş kaydı):** `Pages/UrunAgaci.razor` · `Pages/Urunler.razor` · `Pages/Islemler.razor` · `Pages/Raporlar.razor`. `Pages/Sayim.razor` **AÇILMADI** (Faz B).

**Ek UX sözleşmesi karşılığı:** UX-S21 (şube oturumdan, maden ayardan, has gram üründen — sorulmaz) · UX-S22 (tek aktif reyonda seçici gizlenir, değer otomatik konur; taşımada kaynak reyon salt okunur satır) · UX-S23 (yerleşik adet sayısı **tıklanabilir**, reyon içeriğini açar; yerleştirme sonucu aynı ekranda yenilenir) · UX-S25 (düzeltme yolu var; yoksa gerekçe yazılı) · UX-S27 (yazma uçları stok/yerleşik/atanmamış üçlüsünü yanıtta döndürür) · UX-S28 (hata boş listeye çevrilmez; yerleşim/reyon yüklenemezse ayrı **uyarı bandı**) · UX-S29 (reyon içeriğinde `sinirdaMi` → "daha fazlası var") · UX-S30 (kilitli düğmenin sebebi yazılı) · UX-S31 (VITRIN/KASA/DEPO/CANTA sözlükten; yeni `Yerlesim` bölümü TR+EN, `required` → eksik anahtar derleme hatası) · UX-S32/S33 (bu pakette tutar yok; rapor maden birimini etiketinde söyler).

---

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Tam süit ÖNCE (dalga 3 devralma) | 1919 (**ölçülmedi, hesaplandı**: 1962 − 43) | — | — | — |
| Tam süit SONRA | **1962** | **1962** | **0** | 0 |

**Yeni test sayısı: 43** (trx'ten sayıldı). ⚠ Madde 1 notu: paket başlangıcındaki tam süit sayısı bu oturumda AYRI ölçülmedi; dalga 2c kapısının kaydı 1850, araya dalga 3'ün T-5 ve 19 paketleri girmişti. "Önce" sütunu bu yüzden ölçüm değil, çıkarımdır; ölçülmüş tek değer **sonra = 1962/1962**.

| Dosya | Katman | Adet |
|---|---|---|
| `Kuyumcu.Tests/StokYerlesimiTestleri.cs` | DB | 11 |
| `Kuyumcu.Tests/YerlesimCikisTestleri.cs` | DB | 10 |
| `Kuyumcu.Tests/ReyonYonetimTestleri.cs` | HTTP | 10 |
| `Kuyumcu.Tests/ReyonRaporTestleri.cs` | DB + kaynak taraması | 4 |
| `Kuyumcu.Tests/YerlesimEkranTestleri.cs` | bUnit | 6 + 2 |

Güncellenen (silinmeyen) test: `UrunAgaciEkranTestleri.Reyonun_KART_BAZLI_oldugu_ekranda_yazar` → `Kapsam_sinirinin_YENI_gercegi_ekranda_yazar` (Bertan koşulu: uyarı metni SİLİNMEDİ, DARALTILDI; test yeni gerçeği ölçüyor).

**Regresyon kabul ölçütü (Aşkın koşulu):** `IadeTestleri`, `SatisFisiTestleri`, `TransferTestleri` (ve tüm süit) 0 başarısız — "mevcut aritmetik değişmez" iddiasının kanıtı budur.

**Eşzamanlılık testi (koşul):** `StokYerlesimiTestleri.Esszamanli_iki_dusum_ikincisini_reddeder` — iki paralel bağlam aynı yerleşim satırını düşürür, ikincisi `DbUpdateConcurrencyException` alır, satırda aşırı-düşüm OLMAZ.

**TB-19 ölçülebilir ölçüt (koşul):** `ReyonRaporTestleri.Rapor_gruplamayi_sunucuda_yapar` — kaynak taraması: `ReyonStok` gövdesinde `GroupBy` sayısı ≥ `ToListAsync` sayısı.

---

## 6. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/p25/` altına kopyalandı → tek eşleşmeli değişiklik → filtreli test → yedek geri kopyalandı → `cmp` ile birebir aynılık kanıtlandı (her satırda **GERI-ALINDI-CMP-OK**). Son mutasyondan sonra temiz `dotnet build`: **0 uyarı 0 hata**.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M25-1 | I1 kapısı (`adet > atanmamis`) kaldırıldı | `StokYerlesimiTestleri.Atanmamistan_fazlasi_yerlestirilemez` | cmp OK |
| M25-2 | Çıkışta reyon yetersizse 400 yerine sessizce genel sıra | `YerlesimCikisTestleri.ReyonId_ile_satis_o_reyonda_yetersizse_reddedilir` | cmp OK |
| M25-3 | Çıkış sırası "atanmamış önce" yerine doğrudan reyonlar | `YerlesimCikisTestleri.Reyonsuz_satis_once_atanmamisi_tuketir` | cmp OK |
| M25-4 | Reyonun şube kilidi kaldırıldı | `StokYerlesimiTestleri.Baska_subenin_reyonuna_yerlestirilemez` | cmp OK |
| M25-5 | Pasife almanın yerleşim kapısı kaldırıldı | `ReyonYonetimTestleri.Icinde_yerlesim_olan_reyon_pasife_alinamaz` | cmp OK |
| M25-6 | Reyon içi taşıma `HareketFabrikasi` ile deftere yazıldı | `ReyonYonetimTestleri.Tasima_hareketler_defterine_satir_yazmaz` | cmp OK |
| M25-7 | Taşımada `DenetimServisi.YazmaKaydet` kaldırıldı | `ReyonYonetimTestleri.Tasima_denetim_kaydi_yazar` | cmp OK |
| M25-8 | Varsayılan reyonun ŞUBE kontrolü kaldırıldı | `YerlesimCikisTestleri.Varsayilan_reyon_baska_subedeyse_atanmamisa_girer` | cmp OK |
| M25-9 | Raporda madenler tek toplamda birleştirildi | `ReyonRaporTestleri.Rapor_madenleri_ayri_toplar` | cmp OK |
| M25-10 | `[ConcurrencyCheck]` (iyimser kilit) kaldırıldı | `StokYerlesimiTestleri.Esszamanli_iki_dusum_ikincisini_reddeder` | cmp OK |

**Test açığı bulunmadı** — her mutasyon en az bir testi kırdı; hiçbir mutasyon sessiz geçmedi.

> Not (Madde 1): `Tasima_denetim_kaydi_yazar` testi ilk hâlinde YANLIŞ ölçüyordu — `DenetimYazmaDinleyicisi`in OTOMATİK satır izini de sayıyordu ve sayı tutmuyordu. Test, DoD'un istediği **anlamlı** izi (detayında "reyon taşıma · kaynak → hedef · adet" geçen kayıt) ölçecek şekilde düzeltildi; mutasyon ondan sonra doğru testi kırdı.

---

## 7. Denetim

```
$ dotnet/denetim.sh
  ✓ IKiraciVarlik uygulayan varlık                44
  ✓ Pages içinde istemci record tanımı          121 (tavan 122)
  ✓ Pages: ModalPencere kullanımı                44 (tavan 45)
  ✓ Pages: duzenlenenId kalıbı                   1 (tavan 1)
  ✓ Finansal varlıkta [HttpDelete] (0 olmalı)    0
  ...
DENETİM TEMİZ.
```

Derleme: `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata**.

---

## 8. Canlı doğrulama (localhost:5198, gerçek veriyle)

Test kiracıları: **PAKET25-TEST** ve **PAKET25-TEST-YABANCI** (iş sonunda silindi). 1 şube + sonradan 2. şube, 3 reyon (VITRIN-A · KASA · DEPO), 2 ürün (P25-A = ADET, P25-G = GRAM).

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | 10 adet ALIŞ (reyonsuz) | stok 10, yerleşim 0, uyarı döner | **200** · `yerlesimUyarisi: "Mal ATANMAMIŞ olarak girdi…"` | SQL: `stoklar.adet=10`, `COUNT(stok_yerlesimleri)=0` |
| 2 | 4 adet VITRIN-A'ya yerleştir | tek satır 4, atanmamış 6 | **200** · `{stokAdedi:10, yerlesikAdet:4, atanmamis:6}` | SQL: `VITRIN-A → 4.000` |
| 3 | VITRIN-A → KASA 2 taşı | stok 10 AYNI, 2/2, **hareket sayısı değişmez**, denetim +1 | **200** | SQL: `stoklar.adet=10` (taşıma öncesi=sonrası), `hareketler COUNT 1 → 1`, yerleşim `VITRIN-A 2 · KASA 2`, denetim kaydı: `"reyon taşıma · şube … · VITRIN-A → KASA · 2 adet"` |
| 4 | `reyonId=KASA` ile **3** adet satış (KASA'da 2 var) | **400** | **400** · `"'KASA' reyonunda yeterli mal yok — çıkış başka reyondan yapılmaz."` | Şube stoğu 10 iken bile reddedildi (fail-closed) |
| 5 | `reyonId=KASA` ile 2 adet satış | 200, KASA satırı **silinir** | **200** | SQL: yalnız `VITRIN-A 2` kaldı; stok 10 → 8 |
| 6 | Reyonsuz 5 adet satış (atanmamış 6) | atanmamıştan düşer, yerleşim DOKUNULMAZ, uyarı YOK | **200**, uyarı yok | SQL: `VITRIN-A 2.000` (aynı), stok 8 → 3 |
| 7 | Reyonsuz 3 adet satış (atanmamış 1 + VITRIN 2) | atanmamış biter, reyon tüketilir, **uyarı döner** | **200** · `yerlesimUyarisi: "…VITRIN-A (2)…"` | SQL: yerleşim satırı 0 adet, stok 0 |
| 8 | GRAM ürün: 7,5 g alış → 2,5 g yerleştir → 0,3 g taşı | 3 ondalıkta bozulma yok | **200/200/200** | SQL: `VITRIN-A 2.200 · DEPO 0.300`, stok `7.500` |
| 9 | İçinde 0,3 g varken DEPO'yu pasife alma | **400** | **400** · `"Bu reyonda 0,3 adet mal duruyor; önce malı boşaltın…"` | — |
| 10 | 0,3 g geri-al → tekrar pasife al | 200 / 200 | **200** · `atanmamis 5.300` → **200** · `aktif:false` | — |
| 11 | Şube-2'ye 3 g transfer (hedefReyonId BOŞ) | hedefte ATANMAMIŞ 3 | **200** · uyarı: "Mal ATANMAMIŞ olarak girdi" | SQL: `stoklar` Merkez 4.5 / Şube-2 3.0; Şube-2'de yerleşim satırı YOK |
| 12 | `GET /raporlar/reyon-stok` | SQL toplamıyla birebir | **200** | Rapor: VITRIN-A adet 2,2 / ALTIN 2,2 g; atanmamış Merkez 2,3 · Şube-2 3,0 — SQL `GROUP BY` çıktısıyla **birebir aynı** |
| 13 | Yabancı kiracı jetonuyla `/reyonlar/{id}/stok` | **404** (400 değil) | **404** · `"Reyon bulunamadı"` | Varlık sızdırılmadı |
| 14 | `GET /urunler/{id}/yerlesim` | şube × reyon + atanmamış | **200** · `[('Merkez Şube',4.5,2.2,2.3), ('P25 Şube-2',3.0,0,3.0)]` | I1 korunuyor |

**Temizlik:** API (PID 27416) durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. 5199 (kurucunun inceleme sunucusu) **hiç dokunulmadı**, hâlâ dinliyor. Test kiracılarının verisi `tenant_id` kolonu olan 44 tablodan silindi; `stok_yerlesimleri`nde **öksüz satır 0**.

**Kiracı sayısı:** başlangıç **8** → iş sonu **8** (5 kurucu/demo + QA-TEST-1/2/3 fikstürleri korundu, hiçbirine dokunulmadı).

---

## 9. Değişen dosyalar

**Yeni**
- `dotnet/Kuyumcu.Api/Data/StokYerlesimi.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20260917102535_StokYerlesimi.cs` (+ `.Designer.cs`, `KuyumcuDbContextModelSnapshot` güncellendi)
- `dotnet/Kuyumcu.Api/Modules/Yerlesim/YerlesimServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Yerlesim/YerlesimModulu.cs`
- `dotnet/Kuyumcu.Core/Sozlesmeler/YerlesimSozlesmeleri.cs`
- `dotnet/Kuyumcu.Tests/StokYerlesimiTestleri.cs` · `YerlesimCikisTestleri.cs` · `ReyonYonetimTestleri.cs` · `ReyonRaporTestleri.cs` · `YerlesimEkranTestleri.cs`
- `dokumanlar/02-mimari/adr/0016-reyon-yerlesimi-defter-kaydi-degildir.md`

**Değişen**
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` (DbSet + 4 indeks)
- `dotnet/Kuyumcu.Api/Data/DenetimYazmaDinleyicisi.cs` (bağlam damgası: `StokYerlesimi → SubeId`)
- `dotnet/Kuyumcu.Api/Common/ApiException.cs` (`DbUpdateConcurrencyException` → 409)
- `dotnet/Kuyumcu.Api/Modules/Urun/KategoriModulu.cs` (`yerlesikAdet` + pasife alma yerleşim kapısı)
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` (sayımın yerleşim bacağı)
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` (`StokDegistir` imzası + yerleşim bacağı, DTO'lara opsiyonel reyon, transfer, takas, `HareketGetir` eklemeli uyarı)
- `dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs` (`ReyonStok`)
- `dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` (StokYerlesimi damgası)
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` (DI)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (yeni `Yerlesim` bölümü TR+EN + `UrunAgaci.KapsamUyarisi` daraltıldı)
- `dotnet/Kuyumcu.Web/Pages/UrunAgaci.razor` · `Urunler.razor` · `Islemler.razor` · `Raporlar.razor`
- `dotnet/Kuyumcu.Tests/UrunAgaciEkranTestleri.cs` (kapsam uyarısı testi güncellendi, silinmedi)
- `dotnet/denetim.sh` (`BEKLENEN_VARLIK` 40 → 41)
- `dokumanlar/02-mimari/adr/README.md` (ADR-0016 satırı)

Spesifikasyon ve `MIMARI-KALAN-PAKETLER-2026-09-15.md` dosyalarına **DOKUNULMADI**. `SONRAKI-OTURUM-PROMPT.md` **açılmadı** (orkestratör yazacak).

---

## 10. Koşul karşılama tablosu

| Koşul (birleşik koşullar + DoD) | Durum | Kanıt |
|---|---|---|
| denetim.sh sayacı mutlak değil, mevcut değer okunarak artırılır | **KARŞILANDI** | 40 → 41 (dosyada okunan gerçek değer) |
| I1 eşzamanlılığı çözülmeden sevk edilmez; iyimser kilit ya da FOR UPDATE, ExecuteUpdate YOK | **KARŞILANDI** | `[ConcurrencyCheck]` + 409 · `denetim.sh` ExecuteUpdate beyaz listesi 4'te |
| Eşzamanlılık DB testi zorunlu | **KARŞILANDI** | `Esszamanli_iki_dusum_ikincisini_reddeder` (M25-10 ile kanıtlı) |
| 25, SatisModulu'na dokunan TEK paket olarak seri sırada | **KARŞILANDI** | Dalga 3 seri kuyruğu; başka paket paralel çalışmadı |
| Regresyon: Iade/SatisFisi/Transfer 0 başarısız | **KARŞILANDI** | Tam süit 1962/1962 |
| Geçiş verisi: hepsi ATANMAMIŞ, migration'da otomatik yerleştirme yok | **KARŞILANDI** | Migration Up() yalnız tablo+indeks; canlı adım 1 |
| KapsamUyarisi silinmez, daraltılır; ekran testi güncellenir | **KARŞILANDI** | Sozlukler TR+EN yeni metin · `Kapsam_sinirinin_YENI_gercegi_ekranda_yazar` |
| PATCH sorumlu kullanıcı kiracı doğrulaması; yabancı reyon 404 (HTTP testi de) | **KARŞILANDI** | Mevcut `ReyonServisi.Guncelle` kontrolü + `Yabanci_kiracinin_reyonu_404` + canlı adım 13 |
| Geri-al SAHIP/MUDUR; TEZGAHTAR 403 HTTP testi | **KARŞILANDI** | `GeriAl_TEZGAHTAR_403` |
| Taşıma/atama/geri-al DenetimKaydi'na yazılır, deftere yazılmaz, karar ADR'ye bağlanır | **KARŞILANDI** | M25-6 · M25-7 · **ADR-0016** |
| Rapor TB-19 ölçütü somut | **KARŞILANDI** | Kaynak tarama testi (GroupBy ≥ ToListAsync) |
| Her yeni varlık: PATCH/pasife alma + rol + denetim + ekranda yol ya da DegismezIpucu | **KARŞILANDI** | § 4 DoD sözleşmesi tablosu |
| Her düğme istek atar ya da disabled + sebep | **KARŞILANDI** | 6 bUnit testi (`Bos_reyonda_tasi_dugmesi_kilitli…`, `Tasima_adet_gecersizse…`, `Gecerli_tasima_istek_atar`) |
| Tek düzenleme kalıbı (Cekmece/SatirIciDuzenle), ikinci duzenlenenId kalıbı yok | **KARŞILANDI** | `denetim.sh`: ModalPencere 44 (artmadı), duzenlenenId 1 |
| Ücretsiz pilot: fiyat/teklif/tahsilat metni üretilmez | **KARŞILANDI** | Bu pakette hiçbir fiyat/teklif metni yok |

---

## 11. Açık kalanlar

1. **Faz B (kapsam dışı, bilinçli):** sayım oturumu kapsamının reyondan okunması + reyon sorumlusu fark raporu + `Sayim.razor` notu. Faz A sonrası `KapsamUyarisi` metni bu gerçeği **açıkça yazıyor** ("sayım kapsamı hâlâ ürün KARTINDAN okunur").
2. **Boş dosya artığı:** `dotnet/Kuyumcu.Api/Modules/Stok/YerlesimServisi.cs` ve `YerlesimModulu.cs` ad alanı çakışması yüzünden `Modules/Yerlesim/` altına taşındı; eski iki dosya `printf '' >` ile **boşaltıldı** (hook `rm`'i bloklar). Derlemeye etkisi yok. **Silme işi kurucuya kalıyor** — `Modules/Stok/` klasörü boş dosyalarla duruyor.
3. **Faz A kapsam dışı (spesifikasyon):** maliyet partisinin reyon bazına indirilmesi, reyon bazlı kâr/zarar, raf etiketi/QR, RFID, şubeler arası reyon eşleme, reyon devir bakiyesi.
4. **`RaporModulu.StokDegeri` hâlâ sınırsız `ToListAsync` yapıyor** (mevcut TB-19 borcu, bu paketin dışı). Yeni rapor bu deseni **kopyalamadı**.
5. **Islemler ekranında reyon seçici yalnız oturumun şube claim'i varken çıkar.** Çok şubeli yönetici oturumunda (şube claim'i yok) seçici hiç gösterilmiyor — yanlış şubenin reyonunu listelemektense hiç göstermemek tercih edildi. Şube seçici POS'a eklenirse (kapsam dışı) bu da açılabilir.

---

## 12. Kurucuya / mali müşavire notlar

1. **Mevzuat dayanağı YOK — bu paket bir İŞLETME İÇİ KONTROL aracıdır.** Spesifikasyonun kendisi VUK md.186 (envanter), VUK md.253 (saklama) ve Kuyum Ticareti Yönetmeliği maddelerini **DOGRULANMALI** olarak işaretlemişti. Reyon/vitrin bazlı stok tutmayı ZORUNLU kılan bir hüküm bu oturumda **doğrulanmadı** ve koda hiçbir mevzuat eşiği/kapısı yazılmadı. Ürün, reyon bölümlemesini bir **uyum/mevzuat gereği** olarak tanıtmamalıdır — "sayım ve sorumluluk kolaylığı" olarak tanıtılmalıdır.
2. **Reyon taşıma kaydı resmî belge sayılmıyor.** Kayıt `denetim_kayitlari`nda append-only durur (silinemez), ama VUK md.253 saklama yükümlülüğüne tabi bir "belge" olduğu iddia edilmiyor. Mali müşavir aksini söylerse taşıma izinin belge düzeyine çıkarılması (saklama süresi, numaralandırma) ayrı bir iş kalemidir.
3. **Denetçi sorusuna hazır cevap:** "mal yer değiştirdi ama defterde satır yok" sorusunun gerekçesi **ADR-0016**'dadır: reyon içi taşımada adet, has ve para değişmez; defter bir değer/mülkiyet kaydıdır. İz denetim izindedir.
4. **Geçiş günü beklentisi:** yayına alındığında mevcut tüm stok "ATANMAMIŞ" görünecektir. Bu bir hata değildir — sistem hangi şubedeki hangi adedin hangi vitrinde durduğunu **bilmiyor** ve uydurmuyor. Kullanıcıya "reyonlarım boş göründü" sorusunun cevabı ekranda yazıyor.
