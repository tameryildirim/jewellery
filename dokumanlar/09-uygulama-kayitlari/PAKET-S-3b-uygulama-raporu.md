# PAKET S-3b — Düzeltilebilirlik (uygulama raporu)

> Dalga 3-H (hata düzeltme). Roller: Taşkın (backend) + Ertuğ (frontend).
> Tarih: 2026-09-18. Kapsam kaynağı: `HAZIRLIK-KURULU-3.md` § engel 1 (KRİTİK),
> engel 3'ün belge ayağı, QA turu 3 bulguları (/kasalar · /kur · /gider-pusulalari ·
> /denetim-izi · /isletme).
> **Yeni ekran / yeni modül / yeni özellik AÇILMADI** (kurucu talimatı 13 · kapsam kilidi).
> **Commit yok** — çalışma ağacında bırakıldı.

---

## 1. Özet

Kurucunun bağlayıcı şartı — *"her veri girişi düzeltilebilir olsun"* — dört kalemde
**kanıtla** kapandı; bir kalem **dürüst sınırıyla** kapandı (belge düzeltilir, stok
bacağı düzeltilmez); kalan kalemler gerekçesiyle **devredildi**.

| Ölçüm | Değer |
|---|---|
| Derleme | `dotnet build Kuyumcu.sln` → **0 hata · 0 uyarı** |
| Denetim | `dotnet/denetim.sh` → **DENETİM TEMİZ** |
| Tam süit | **2485/2485 yeşil** (önce 2464) · 0 atlanan |
| Yeni test | **21** (18 HTTP/saf + 2 bUnit + 1 sözleşme) |
| Mutasyon | 22 koşum · **20 kanıt** · **2 TEST AÇIĞI bulundu ve kapatıldı** |
| Migration | `20260917220208_S3bDuzeltilebilirlik` — `kuyumcu_net`e uygulandı (47 migration) |
| Canlı | 5198'de gerçek MySQL ile 19 adım · test kiracısı silindi (tenants 8 → 8) |

### Kapanan kalemler

| # | Kalem (kurul tur 3 / QA turu 3) | Durum |
|---|---|---|
| 1 | `/kasalar` — elle girilen kasa hareketi hiç düzeltilemiyor (YÜKSEK) | **KAPANDI** — tek motorda (StornoServisi) gerekçeli ters kayıt |
| 2 | `/kasalar` — hareket açıklaması hiçbir yerde geri okunamıyor (YÜKSEK) | **KAPANDI** — alan artık kaydediliyor ve geçmişte basılıyor |
| 3 | `/kasalar` — modal önceki kasanın "Gider" seçimini hatırlıyor (YÜKSEK) | **KAPANDI** — form tümüyle sıfırlanıyor, başlıkta kasa adı, bantta yön |
| 4 | `/kur` — manuel kur kaydı düzeltilemiyor/silinemiyor (YÜKSEK) | **KAPANDI** — gerekçeli iptal, "geçersiz" rozeti, fail-closed bağlı-işlem kapısı |
| 5 | `/gider-pusulalari` — kayıt hiç düzeltilemiyor + yanıltıcı "Kaydet" (KRİTİK) | **KAPANDI (sınırlı)** — VUK md.234 iptal + yeni pusula; **stok/maliyet bacağı değişmez** (aşağıda) |
| 6 | `/denetim-izi` — tam TCKN gösteren pusula okuması hassas işaretlenmiyor (YÜKSEK) | **KAPANDI** — `GiderPusulasi` hassas varlık listesine alındı |
| 7 | `/isletme` — pasif şube arayüzden aktife alınamıyor (YÜKSEK) | **KAPANDI** — `PATCH /subeler/{id}` `aktif` alanını taşıyor, satırda düğme var |

---

## 2. Yapılanlar

### 2.1 Elle kasa hareketi — açıklama + storno (engel 1 · QA /kasalar)

**Gerçek sebep koda bakılarak doğrulandı, spesifikasyon iddiası iki yerde eksikti:**

- `KasaHareketDto.Aciklama` ekrandan gönderiliyordu ama `kasa_hareketleri` tablosunda
  **karşılık gelen kolon yoktu** — sunucu değeri sessizce atıyordu (Anayasa md.1 ihlali).
  QA bunu "açıklama listede görünmüyor" diye raporlamıştı; sebep gösterim değil, **veri
  kaybıydı**.
- Ekran tarafında `StornoAcik(h) => h.HareketId is { Length: > 0 } && kurId is not null`
  olduğu için elle hareket (hareket_id NULL) **kalıcı olarak** kilitliydi.

**Düzeltme:**

1. `kasa_hareketleri` + `aciklama` (VARCHAR 500) ve + `storno_kaynak_id` (VARCHAR 36,
   `(tenant_id, storno_kaynak_id)` indeksli).
2. `KasaServisi.HareketGir` açıklamayı **yazar** ve gerekçeli denetim izi düşer
   (`KasaHareketi` · yön + tutar + açıklama).
3. **İKİNCİ MOTOR YAZILMADI** (Bertan mimari vetosu): ters kayıt üretme yetkisi
   `StornoServisi` içinde kaldı — yeni metot `KasaHareketiStorno`.
   `HareketFabrikasi.Ekle` **kullanılmaz**, çünkü kasa hareketi deftere yazmaz;
   ters satır `kasa_hareketleri`ne yazılır ve kaynağına `storno_kaynak_id` ile **bağlanır**
   (QA turu 3 önerisi birebir).
4. Kapılar (hepsi fail-closed, klasik Storno ile aynı): yalnız elle hareket · çift storno
   yasak · storno satırının kendisi stornolanamaz · gerekçe ≥ 20 karakter · gün kilidi
   (bugün kapalıysa yazılmaz; kaynağın günü kapalıysa **yalnız SAHİP**) · kaynak satır
   **DEĞİŞMEZ**.
5. Uç: `POST /api/v1/kasalar/{id}/hareket/{hareketId}/storno` — `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`,
   sınıf düzeyinde `[ModulYetkisi("kasa")]`.

**Ekran (`Kasalar.razor`):**
- `HareketModalAc` formu **yön dâhil** sıfırlar (`hareketYon = "GIRIS"`), başlıkta kasa adı
  yazar (UX-S22) — QA'nın "yanlış kasaya yanlış yönde kayıt" senaryosu bu iki değişiklikle kapanır.
- Kaydetten sonra POST yanıtı **okunur**: bantta `Gelir/Gider · tutar · yeni bakiye`
  (UX-S27, para birimiyle — UX-S32).
- Geçmişte açıklama satırı (`#kh-aciklama-{id}`) ve storno rozeti; elle hareketin storno
  düğmesi **açık**; kilitli kalanın sebebi yazılı (UX-S30).
- Ekran kendi ters kaydını **üretmez**, yalnız ucu çağırır ("ters iade" taklidi yok).

### 2.2 Manuel kur kaydı — gerekçeli iptal (engel 1 · QA /kur)

- `kur_snapshots` + `iptal_edildi` · `iptal_gerekce` · `iptal_tarihi`.
- `KurServisi.ManuelKurIptal`: satır **silinmez**, fiyatı **değişmez**; yalnız "geçersiz"
  damgası vurulur. `GuncelKur` ve `KaydetVeyaMevcut` iptal edilmiş satırı **atlar**.
- **FAIL-CLOSED**: kura bağlanmış en az bir hareket varsa **409** ve denetim izine RED kaydı —
  geçmiş bir işlemin kuru, o işlem dururken geçersiz olamaz (has hesabı açıklanamaz hâle gelirdi).
  Mesaj kaç işlem olduğunu ve doğru sırayı (önce storno) **söyler** (UX-S30).
- Dış kaynaklı (`ALTINKAYNAK` vb.) kayıt **iptal edilemez** — düzeltme yolu "Şimdi çek".
- Uç: `POST /api/v1/kur/manuel/{id}/iptal` — `[RolGerekli("SAHIP","MUDUR")]`.
- Ekran: geçmiş satırında "İptal et" (dış kaynakta **kilitli + sebebi yazılı**), iptal
  edilende "Geçersiz" rozeti + gerekçe metni; gerekçeli onay modalı.

### 2.3 Gider pusulası — VUK md.234 iptal + yeni pusula (engel 1/3 · QA KRİTİK)

- `gider_pusulalari` + `iptal_edildi` · `iptal_gerekce` · `iptal_tarihi`.
- `HurdaServisi.Iptal`: belge **silinmez**, "İPTAL" damgasıyla saklanır; liste, detay ve
  **belge** çıktısı bayrağı taşır. Yanıt, doğru düzeltme yolunu **sunucudan** döndürür (UX-S27).
- **Alan güncelleyen bir PATCH AÇILMADI** — basılmış vergi belgesinin içeriği geriye dönük
  değiştirilseydi belge zinciri savunulamaz olurdu (VUK md.234 append-only).
- Uç: `POST /api/v1/gider-pusulalari/{id}/iptal` — `[RolGerekli("SAHIP","MUDUR")]`
  (MUHASEBE **göremez yazamaz**: 403 ile kanıtlandı).
- Ekran: **yanıltıcı kilitli "Kaydet" düğmesi kaldırıldı** (`Cekmece.KaydetGoster`), yerine
  gerçek "Pusulayı iptal et (VUK md.234)" düğmesi + gerekçeli onay modalı + liste ve detayda
  "İPTAL" rozeti.
- `DuzeltmePolitikasi`: `GiderPusulasi` artık `SaltOkunur/STORNO_YOK` değil,
  `Storno/PUSULA` + hedef uç. Yeni gerekçe anahtarı `PUSULA` (TR+EN).

### 2.4 Denetim izi — pusula okuması hassas (QA /denetim-izi)

`HassasVarliklar` listesine `nameof(GiderPusulasi)` eklendi. Tam TCKN gösteren detay ve belge
okumaları artık `hassas=1` yazılıyor; KVKK md.12 süzgecinde görünüyorlar.
⚠ **Sapma:** liste okuması da hassas işaretleniyor (liste TCKN'yi maskeler ama ad-soyad
gösterir). Fazla işaretleme tutucu yöndedir; eksik işaretlemenin tersine bilgi kaybettirmez.

### 2.5 Pasif şubeyi aktife alma (QA /isletme)

`SubeOlusturDto` + `Aktif` (PATCH sözleşmesi: **gövdede yoksa korunur**, gönderilirse uygulanır).
Ekranda pasif satıra "Aktife Al" düğmesi; ölü `DegismezIpucu` **kaldırıldı** (artık yanlış bilgiydi).

---

## 3. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Yetki | Denetim | Ekran eylemi |
|---|---|---|---|---|
| Elle kasa hareketi | **Ters kayıt** `POST /kasalar/{id}/hareket/{hid}/storno` (gerekçe ≥20) | SAHIP·MUDUR·MUHASEBE | `KasaHareketiStorno` hassas=1 + RED kaydı | Geçmiş modalında "Storno" (açık) |
| Defter fişinin kasa bacağı | **Yok** — fişin kendi stornosu (409, yol yazılı) | — | RED kaydı | Kilitli + DegismezIpucu |
| Manuel kur kaydı | **İptal** `POST /kur/manuel/{id}/iptal` (gerekçe ≥20) | SAHIP·MUDUR | `KurIptali` hassas=1 + RED kaydı | Satırda "İptal et" |
| Dış kaynak kur kaydı | **Yok** — yenisi çekilir (409) | — | — | Kilitli + sebebi yazılı |
| Gider pusulası (belge) | **İptal + yeni pusula** `POST /gider-pusulalari/{id}/iptal` | SAHIP·MUDUR | `GiderPusulasi` hassas=1 | Çekmecede "Pusulayı iptal et" |
| Hurda alış hareketi (stok/maliyet) | **Yok** — `ErtelenenTurler` | — | — | DegismezIpucu, sınır açıkça yazılı |
| Şube (aktiflik) | **PATCH** `aktif` | SAHIP·MUDUR | otomatik yazma izi | "Pasife Al" / "Aktife Al" |

**Finansal varlıkta hard DELETE YOK**: kasa hareketi, kur kaydı ve pusula **silinmez**;
ters kayıt ya da "geçersiz/İPTAL" damgası ile hükümsüz kılınır.

---

## 4. Sapmalar ve gerekçeleri

1. **`GerekceDto` adında yeni bir sunucu DTO'su açıldı** (`Common/GerekceDto.cs`).
   `StornoDto` kur anlık görüntüsünü **zorunlu** kılar; elle kasa hareketinin ve manuel kur
   kaydının kur bacağı yoktur. `StornoDto` kullanılsaydı kur verisi hiç olmayan bir kiracıda
   bu iki düzeltme yolu da kapanırdı. (Bu bir *istemci* record'u değildir; `Pages` record
   tavanı 129 **artmadı**.)
2. **`denetim.sh` ModalPencere tavanı 47 → 49.** İki yeni ModalPencere de DoD EK madde 4'ün
   **yıkıcı onay** kullanımıdır (kur iptali, pusula iptali); ikisi de ≥20 karakter gerekçe
   ister. Düzenleme kalıbı (`duzenlenenId`) sayısı **artmadı**. Gerekçe betiğe yazıldı.
3. **"Ölü düğme" sayacı (tavan 71) artırılmadı**: yeni düğmelerin `@onclick`/`disabled`
   nitelikleri `<button` ile **aynı satıra** alındı; sayaç 71'de kaldı.
4. **Beş mevcut test yeniden yazıldı** — hepsi *eski* (artık yanlış olan) sözleşmeyi
   kodluyordu: "elle hareket storno edilemez", "kur kaydı düzeltilemez", "pasif şubede düğme
   yok", "pusulada kilitli Kaydet var", "pusula metni hiçbir yol vaat etmez". Her birinde
   iddia **tersine çevrildi** ve dosyaya neden değiştiği yazıldı. Hiçbiri silinmedi.
5. **`KurServisi` birincil kurucusuna `DenetimServisi` eklendi** (iptalin gerekçeli izi için).
   Doğrudan `new KurServisi(...)` yapan tek test güncellendi.

---

## 5. Migration

`dotnet/Kuyumcu.Api/Data/Migrations/20260917220208_S3bDuzeltilebilirlik.cs`
(+`.Designer.cs`, + snapshot). `--no-build` **kullanılmadı**; derleme yeşilken üretildi.

```
kasa_hareketleri  + aciklama VARCHAR(500) NULL
                  + storno_kaynak_id VARCHAR(36) NULL
                  + IX_kasa_hareketleri_tenant_id_storno_kaynak_id
kur_snapshots     + iptal_edildi TINYINT(1) NOT NULL DEFAULT 0
                  + iptal_gerekce VARCHAR(1000) NULL  + iptal_tarihi DATETIME(6) NULL
gider_pusulalari  + iptal_edildi TINYINT(1) NOT NULL DEFAULT 0
                  + iptal_gerekce VARCHAR(1000) NULL  + iptal_tarihi DATETIME(6) NULL
```

`kuyumcu_net`e uygulandı (`dotnet ef database update` → `Applying migration ... Done.`).
Kanıt:

```
SHOW COLUMNS FROM kasa_hareketleri LIKE 'aciklama';          → aciklama varchar(500) YES
SHOW COLUMNS FROM kasa_hareketleri LIKE 'storno_kaynak_id';  → storno_kaynak_id varchar(36) YES
SHOW COLUMNS FROM kur_snapshots    LIKE 'iptal_edildi';      → iptal_edildi tinyint(1) NO 0
SHOW COLUMNS FROM gider_pusulalari LIKE 'iptal_edildi';      → iptal_edildi tinyint(1) NO 0
```

Yeni varlık eklenmedi; üç mevcut `IKiraciVarlik` tablosuna kolon eklendi, kiracı süzgeci
kapsamı değişmedi.

---

## 6. Testler

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| `S3bDuzeltilebilirlikHttpTestleri` (yeni) | 18 | 18 | 0 | 0 |
| `UX1C2` + `UX1C3` + `UX1C4` ekran | 116 | 116 | 0 | 0 |
| **Tam süit (sonra)** | **2485** | **2485** | **0** | **0** |
| Tam süit (önce) | 2464 | 2464 | 0 | 0 |

Yeni test sayısı: **21**.

### Kararsızlık (Madde 1 — gizlenmedi)

`UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir`
**altı tam süit koşumunun ikisinde** kırmızıya döndü, dördünde yeşildi; tek başına
(`--filter`) ve sınıf bazında **her seferinde yeşil**. Hata `#alan-hata-islem-adet`
elementinin bulunamaması — `Islemler.razor` POS akışı, bu pakette **dokunulmadı**.
Paralel bUnit koşumuna bağlı **mevcut bir kararsızlıktır**; S-3b'nin sonucu değildir.
Son üç ardışık tam koşum **2485/2485 yeşil**. Kararsızlık kuyruğa yazılmalı (aşağıda).

---

## 7. Mutasyon tablosu

Yöntem: kaynak `scratchpad/mut-yedek/S3b/` altına kopyalandı → `perl -0777 -pi -e` ile tek
eşleşmeli değişiklik → derleme + filtreli test → yedek geri kopyalandı → `cmp` ile birebir
aynılık **kanıtlandı** (her satırda "geri alindi: cmp AYNI").

| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M-S3b-01 | `StornoServisi`: defter-bacağı kapısı `if(false)` | `Defter_fisinin_kasa_bacagi_tek_basina_stornolanamaz_409` |
| M-S3b-02 | `StornoServisi`: çift-storno sorgusu `false` | `Ayni_kasa_hareketi_IKINCI_KEZ_stornolanamaz_409` |
| M-S3b-03 | `GerekceDogrula`: eşik 20 → 1 | **HİÇBİRİ — TEST AÇIĞI** (aşağı bak) |
| M-S3b-03b | aynı mutasyon, test güçlendirildikten sonra | `Gerekce_kapisi_SERVIS_ICINDE_de_kapalidir_SAF` |
| M-S3b-04 | Ters kayıtta `kasa.Bakiye + tersTutar` → `kasa.Bakiye` | `Elle_kasa_hareketi_stornolanir_ve_bakiye_geri_doner` |
| M-S3b-05 | `KasaServisi`: `Aciklama = null` | `Elle_kasa_hareketinin_aciklamasi_KAYDEDILIR` |
| M-S3b-06 | `ManuelKurIptal`: bağlı-işlem kapısı `if(false)` | **HİÇBİRİ — TEST AÇIĞI** (aşağı bak) |
| M-S3b-06b | aynı mutasyon, test deterministik hâle getirildikten sonra | `Kura_bagli_islem_varsa_iptal_409_ve_kayit_GECERLI_kalir` |
| M-S3b-07 | `GuncelKur`: `&& !s.IptalEdildi` kaldırıldı | `Manuel_kur_iptal_edilir_ve_guncel_kur_ONA_DONMEZ` |
| M-S3b-08 | `ManuelKurIptal`: MANUAL kaynak kapısı `if(false)` | `Dis_kaynakli_kur_kaydi_iptal_edilemez_409` |
| M-S3b-09 | `HurdaServisi.Iptal`: "zaten iptal" kapısı `if(false)` | `Ayni_pusula_ikinci_kez_iptal_edilemez_409` |
| M-S3b-10 | `HassasVarliklar`'dan `GiderPusulasi` çıkarıldı | `Pusula_detayi_okunmasi_HASSAS_denetim_kaydi_yazar` |
| M-S3b-11 | `SubeServisi`: `aktif` uygulaması silindi | `Pasife_alinan_sube_PATCH_ile_geri_acilir` |
| M-S3b-12 | `SubeServisi`: `sube.Aktif = dto.Aktif ?? true` (PATCH sözleşmesi bozuldu) | `Sube_PATCH_gonderilmeyen_aktif_alanini_KORUR` |
| M-S3b-13 | `Kasalar.razor`: `hareketYon = "GIRIS"` silindi | `Kasalar_hareket_modali_her_acilista_YONU_SIFIRLAR` |
| M-S3b-14 | `Kasalar.razor`: `StornoAcik` elle harekette false | `Kasalar_ELLE_girilen_hareket_ARTIK_STORNOLANABILIR_ve_KENDI_ucuna_POST_atar` |
| M-S3b-15 | `Kasalar.razor`: açıklama hep "(açıklama girilmemiş)" | `Kasalar_gecmisinde_ELLE_hareketin_ACIKLAMASI_gorunur` |
| M-S3b-16 | `Kur.razor`: iptal düğmesi hep `disabled` | `Kur_MANUEL_kaydi_ARTIK_gerekceyle_IPTAL_edilebilir` |
| M-S3b-17 | `GiderPusulalari.razor`: `KaydetGoster="false"` kaldırıldı | `C3_10_...YANILTICI_Kaydet_YOKTUR` **ve** `C3_D2_Pusula_kilit_sebebi_YERINDE...` |
| M-S3b-18 | `Isletme.razor`: düğme `disabled="@(true)"` | *derleme kırmızı* — mutasyon geçersiz sayıldı |
| M-S3b-18b | `Isletme.razor`: `@onclick` boşa alındı | `C4_13_Pasif_sube_ARTIK_AKTIFE_ALINIR_ve_PATCH_aktif_true_gonderir` |
| M-S3b-19 | `HurdaServisi.Iptal`: `g.IptalEdildi = false` | `Gider_pusulasi_gerekceyle_IPTAL_edilir_ve_SILINMEZ` |
| M-S3b-20 | `StornoServisi`: "storno satırının kendisi" kapısı `if(false)` | **HİÇBİRİ — TEST AÇIĞI** (aşağı bak) |
| M-S3b-20b | aynı mutasyon, yeni test eklendikten sonra | `STORNO_satirinin_KENDISI_stornolanamaz_409` |

### Bulunan üç test açığı (hepsi kapatıldı)

- **M-S3b-03** — HTTP testi tek başına yetmiyordu: controller yolundaki
  `[MinLength(20)]` de 400 döndürdüğü için **servis içindeki** elle doğrulama kaldırılsa bile
  test yeşil kalıyordu. → Saf test eklendi (`Gerekce_kapisi_SERVIS_ICINDE_de_kapalidir_SAF`);
  DTO DataAnnotations'ın yalnız controller yolunda çalıştığı değişmezi artık **ölçülüyor**.
- **M-S3b-06** — test "iki kez iptal dene" biçimindeydi; ikinci çağrı zaten "zaten iptal
  edilmiş" 409'unu alıyordu, bu yüzden **bağlı işlem** kapısı kaldırılsa bile yeşil kalıyordu.
  → Test deterministik hâle getirildi: taze kur + ona bağlı işlem + **tek** iptal denemesi,
  mesaj ayrıştırılarak sınanıyor, kaydın hâlâ geçerli olduğu SQL'le değil DbContext'le doğrulanıyor.
- **M-S3b-20** — "ters kaydın tersi yazılamaz" kapısının **hiçbir testi yoktu**; kaldırıldığında
  defterde sonsuz storno zinciri doğuyordu. → `STORNO_satirinin_KENDISI_stornolanamaz_409` eklendi.

Son mutasyondan sonra TEMİZ derleme: **0 hata · 0 uyarı**.

---

## 8. Denetim çıktısı

```
$ bash dotnet/denetim.sh
  ✓ Pages içinde istemci record tanımı          129 (tavan 129)
  ✓ Pages: ModalPencere kullanımı                49 (tavan 49)
  ✓ Pages: @onclick'siz ve disabled'sız <button>  71 (tavan 71)
DENETİM TEMİZ.
```

---

## 9. Canlı doğrulama (localhost:5198, gerçek MySQL)

Kiracı: `PAKETS-3b-TEST` (sistem anahtarı + tek kullanımlık davetle açıldı).
Yanıt gövdeleri **okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` (davetsiz) | 403 | **403** "Geçerli bir davet jetonu gerekiyor" |
| 2 | `POST /sistem/davetler` + `POST /auth/kayit` | 201 / 201 | **201 / 201** |
| 3 | `POST /kasalar` (şubesiz) | 400 | **400** "Şube zorunludur" |
| 4 | `POST /kasalar/{id}/hareket` CIKIS 150 + açıklama | 200, bakiye −150 | **200**, `bakiye: -150.0` |
| 5 | `GET /kasalar/{id}/hareketler` | `aciklama` dolu | **`'Dükkân kirası — Eylül'`** |
| 6 | `POST .../hareket/{h}/storno` gerekçe "yanlış" | 400 | **400** "Gerekçe en az 20 karakter olmalıdır" |
| 7 | `POST .../hareket/{h}/storno` geçerli | 200, bakiye 0 | **200** `tutar 150.0 · kasaBakiyesi 0.0` |
| 8 | Aynı hareket ikinci storno | 409 | **409** "zaten stornolanmıştır" |
| 9 | SQL: `kasa_hareketleri` | 2 satır, ters satır kaynağa bağlı | **−150 / +150 · storno_kaynak_id = kaynak id** |
| 10 | SQL: denetim izi | `KasaHareketiStorno` hassas=1 + RED kaydı | **ikisi de var** |
| 11 | `POST /kur/manuel` 6800 | 201 | **201** |
| 12 | `GET /kur/guncel` (iptalden önce) | MANUAL 6800 | **MANUAL 6800** |
| 13 | `POST /kur/manuel/{id}/iptal` | 200 `iptalEdildi=true` | **200**, gerekçe döndü |
| 14 | `GET /kur/guncel` (iptalden sonra) | başka kayıt | **ALTINKAYNAK 6820.29** |
| 15 | Aynı kur ikinci iptal / dış kaynak iptali | 409 / 409 | **409 / 409** (mesajlar doğru yolu söylüyor) |
| 16 | SQL: `kur_snapshots` | satır duruyor, `iptal_edildi=1` | **6800.0000 · MANUAL · 1 · gerekçe dolu** |
| 17 | `POST /islemler/hurda-alis` (satıcı bloğuyla) | 200 + pusula doğar | **200**, `GP-2026-000001`, listede TCKN maskeli |
| 18 | `POST /gider-pusulalari/{id}/iptal` (kısa / geçerli / ikinci) | 400 / 200 / 409 | **400 / 200 / 409**; `duzeltmeYolu` döndü |
| 19 | `GET /gider-pusulalari/{id}/belge` | `iptalEdildi=true` + gerekçe | **true** + gerekçe |
| 20 | SQL: `gider_pusulalari` · denetim | belge duruyor, 6 hassas okuma | **`GP-2026-000001 · 1 · gerekçe · iptal_tarihi dolu`**, `hassas_pusula_okuma = 6` |
| 21 | SQL: `hareketler` | `HURDA_ALIS 1` (iptal stok bacağına DOKUNMADI) | **HURDA_ALIS 1** |
| 22 | Şube: oluştur → pasife al → `PATCH aktif=true` | 200/200/200 `aktif=True` | **200 / 200 / 200 · aktif: True** |

**Temizlik:** API PID'i durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**);
`information_schema`'dan `tenant_id` kolonu olan **49 tablodan** kiracı silindi, `tenants`
ve `kiraci_davetleri` temizlendi. `SELECT COUNT(*) FROM tenants` → **8** (başlangıçtaki değer).
Kalan kiracılar arasında `QA-TEST-1/2/3` **dokunulmadan** duruyor.
Sistem anahtarı, erişim jetonu ve davet jetonu dosyaları `printf '' >` ile boşaltıldı;
hiçbiri rapora ya da çıktıya yazılmadı.

---

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı depo genelinde zaten kirli
(ilk commit'ten beri izlenmeyen dosyalar var). Bu pakette **dokunulan** dosyalar:

**Sunucu**
- `dotnet/Kuyumcu.Api/Common/GerekceDto.cs` *(yeni)*
- `dotnet/Kuyumcu.Api/Data/Entities.cs` · `Data/GiderPusulasi.cs` · `Data/KuyumcuDbContext.cs` · `Data/DenetimIzi.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20260917220208_S3bDuzeltilebilirlik.cs` (+`.Designer.cs`, +snapshot) *(yeni)*
- `dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Sube/SubeModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs`

**Ortak**
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (TR+EN, 16 yeni anahtar)

**İstemci**
- `dotnet/Kuyumcu.Web/Bilesenler/Cekmece.razor`
- `dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs`
- `dotnet/Kuyumcu.Web/Pages/Kasalar.razor` · `Pages/Kur.razor` · `Pages/GiderPusulalari.razor` · `Pages/Isletme.razor`

**Test / betik**
- `dotnet/Kuyumcu.Tests/S3bDuzeltilebilirlikHttpTestleri.cs` *(yeni)*
- `dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs` · `UX1C3EkranTestleri.cs` · `UX1C3KilitDurustlukTestleri.cs` · `UX1C4EkranTestleri.cs` · `UX1C3DuzeltmeSozlesmeTestleri.cs` · `UX4dServisTestleri.cs`
- `dotnet/denetim.sh` (ModalPencere tavanı 47 → 49, gerekçesiyle)

### Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ)

`Kasalar.razor`, `Kur.razor`, `GiderPusulalari.razor`, `Isletme.razor`.
Dördü de daha önce UX-1-C2/C3/C4 dalgalarında açılmıştı. **İkinci dokunuşun yazılı gerekçesi:**
bu paket yeni bir ekran işi değil, o dalgaların kapatamadığı **kurul engeli 1'in ekran ayağıdır**;
sunucuda düzeltme yolu **bu pakette doğdu** ve UX-S25 gereği ("sunucuda uç var, ekranda düğme
yok" kabul edilmez) düğmenin aynı pakette açılması zorunluydu. Ayrı bir dalgaya bırakılsaydı
ürün, ucu olan ama düğmesi olmayan bir durumda kalırdı.

---

## 11. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Hatayı yeniden üreten test | ✅ | 21 yeni test; 18'i yeni HTTP dosyasında |
| Mutasyon kanıtı (geri al → kırmızı) | ✅ | § 7, 20 kanıt + 3 kapatılmış test açığı |
| 5198'de canlı doğrulama (HTTP + SQL) | ✅ | § 9, 22 adım |
| `dotnet/denetim.sh` TEMİZ | ✅ | § 8 |
| Tam süit 0 başarısız | ✅ | 2485/2485 (son 3 ardışık koşum) |
| Kendi test kiracımı sildim, QA-TEST-* dokunulmadı | ✅ | tenants 8 → 8, QA-TEST-1/2/3 duruyor |
| Tek storno motoru, ikinci motor yok | ✅ | `StornoServisi.KasaHareketiStorno` |
| `Hareketler.Add` yok / `HareketFabrikasi` tek kapı | ✅ | denetim.sh TEMİZ |
| `[RolGerekli]` tam eşleşme + denetim kaydı | ✅ | 403 testleri + `hassas=1` SQL |
| PATCH semantiği (gönderilmeyen = koru) | ✅ | `Sube_PATCH_gonderilmeyen_aktif_alanini_KORUR` + M-S3b-12 |
| Tip sözleşmesi (ad **ve** tip doğrulandı) | ✅ | `HareketVeri`, `KurVeri`, `PusulaSatir`, `PusulaDetay` — her birinde kaynak dosya:alan yorumu |
| Yeni `Pages` istemci record'u yok | ✅ | tavan 129 → 129 |
| Sözlük TR+EN, çıplak metin yok | ✅ | `Sozlukler.cs`, 16 anahtar × 2 dil |
| Her düğme istek atar ya da kilitli + sebebi yazar | ✅ | ölü düğme sayacı 71'de sabit; M-S3b-16/18b |
| Yıkıcı eylem ortak onay modalı + geri dönüş yolu | ✅ | kur/pusula iptal modalları, gerekçe ≥20 |
| Finansal varlıkta hard DELETE yok | ✅ | § 3 |
| Yeni ekran/modül/özellik yok (kapsam kilidi) | ✅ | mevcut 4 ekranın borcu kapatıldı |
| Boş durum + hata/başarı bandı | ✅ | mevcut bileşenler korundu; pusulaya `BasariBandi` eklendi |

---

## 12. Açık kalanlar (devredildi)

Kapsam kilidi gereği **açılmadı**; her biri kuyruktaki mevcut bir pakete bağlandı:

- **`/gider-pusulalari` — hurda alışının stok/maliyet bacağı hâlâ geri alınamıyor.**
  İptal yalnız BELGEYİ hükümsüz kılar. "Hiç almadığımız malı almış görünüyoruz" vakası
  düzeltilemez; ekran bunu **açıkça yazıyor**. → **devredildi: 48-A Faz B** (parti geri alma).
- **`/islemler` Hurda Alış formunda satıcı bloğu** (tip/ad-soyad/TCKN/adres) — sunucu alanı
  kabul ediyor (canlı doğrulamada API üzerinden çalıştı), **form sormuyor**.
  → **devredildi: UX-1-C3** (kurul engel 3'ün ekran ayağı).
- **`/faturalar`** — TASLAK fatura/kalem düzenleme, "Ödendi" geri alma, fiş seçici.
  → **devredildi: 48-A Faz B + UX-1-D**.
- **`/banka`** — dekont yön/tutar/durum düzeltme, "Bekliyor" mutabakat eylemi.
  → **devredildi: S-3c**.
- **`/emanet`** — hareketin ham gram/ayar/taş değerleri hiçbir ekranda geri okunamıyor
  (storno var, **görünürlük** yok). → **devredildi: S-3c**.
- **`/bildirimler`** — zamanlanmış iş ayarları ve eşikler ekrandan değiştirilemiyor.
  → **devredildi: S-3c**.
- **`/isletme`** — kur kaynağı / maliyet yöntemi hiç belirlenmemişken bile seçilemiyor;
  abonelik planı değişimi doğrulanamadı. → **devredildi: S-3c / F-2-min**.
- **Oturum düşmesi** — açık form uyarısız kapanıyor, `/giris?donus=` yok.
  → **devredildi: S-3c** (S-3a'da ApiIstemci ayağı kapanmıştı, ekran ayağı açık).
- **`/kilavuz`** — "Uygulamayı başlatma" bölümü geliştiriciye yazılmış ve **yanlış port**
  (5000) veriyor. → **devredildi: T-2 / Köprü teslimat paketi**.
- **QA fikstürü uzun ömürlü token** — `/veri-disa-aktarim`, `/kullanicilar`, `/yetkiler`,
  `/ayarlar` turu oturum bitişi yüzünden hiç ölçülemedi; Madde 1 gereği **kapanmış sayılmıyor**.
  → **devredildi: QA turu 4 hazırlığı**.
- **Kararsız test** `Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir`
  (§ 6). → **devredildi: Aşkın / test altyapısı**.

---

## 13. Kurucuya ve mali müşavire notlar

1. **Gider pusulası iptali VUK md.234'ün "iptal + yeni belge" yolunu uygular; muhasebe
   kaydını DEĞİŞTİRMEZ.** Hurda alışının stok ve maliyet kaydı olduğu gibi durur. Yani
   *"satıcının adı/TCKN'si yanlış yazılmış"* düzeltilir; *"bu alım hiç olmadı"* düzeltilmez.
   Bu ikinci vaka bugün **yalnız mali müşavir eliyle** kayıt dışı düzeltilebilir.
   Mali müşavire sorulacak: iptal edilen pusulanın yerine kesilen yeni pusulanın **numara
   serisi** nasıl yürütülmeli (aynı seri devam mı, iptal numarası boşta mı kalmalı)?
2. **Stopaj beyanı hâlâ sistemde tutulmuyor.** Belgedeki not bunu söylüyor; iptal bu durumu
   değiştirmez. İptal edilen bir pusulanın stopajı beyan edilmişse **düzeltme beyannamesi**
   gerekir — sistem bunu hatırlatmıyor.
3. **Manuel kur iptali fail-closed:** kura bağlı işlem varsa reddedilir. Bu bilinçli bir
   tercihtir; gevşetilmesi istenirse önce "bağlı işlemleri toplu storno" akışı gerekir
   (bugün yok).
4. **Kapalı gün düzeltmesi yalnız SAHİP'e açık.** Gün kapanış tutanağından sonra defterin
   net toplamının sessizce değişmemesi için konuldu (Mehmet/06 Güvenlik koşulu).
5. Ürün, dalga 2b+2c ve QA turu 4 bitene dek **yalnız ücretsiz pilot**; bu raporda fiyat,
   teklif veya tahsilat içeren hiçbir ifade yoktur.
