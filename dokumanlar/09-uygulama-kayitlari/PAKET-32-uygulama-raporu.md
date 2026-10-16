# PAKET 32 — Banka ekstresi içe aktarma + IBAN/ünvandan cari eşleştirme (Faz A) — UYGULAMA RAPORU

> Tarih: 2026-09-18 · Dalga 4 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü
> Durum: **BİTTİ (Faz A)** · Derleme 0 hata 0 uyarı · `dotnet/denetim.sh` **TEMİZ** · tam süit **2698/2698 yeşil**
> Kapsam kilidi: yalnız Faz A. MT940 (Faz B) **YAZILMADI** — gerekçe § 3.1.

---

## 1. Yapılanlar

| Alan | Yapılan |
|---|---|
| Veri modeli | 3 yeni tablo (`ekstre_ithalatlari`, `ekstre_satirlari`, `cari_ibanlari`), üçü de `IKiraciVarlik`; `banka_hareketleri` + 3 kolon (`ekstre_satir_id`, `banka_referans`, `dedupe_anahtar`) + 1 BENZERSİZ indeks |
| Migration | `20260918002942_BankaEkstresiIceAktarma` — `kuyumcu_net`'e uygulandı, Down/Up ileri-geri doğrulandı (§ 7) |
| Servis | `EkstreServisi` (Yukle · Getir · SatirGuncelle · Onayla · Oneriler) — **kendi `BankaHareketleri.Add`'ini AÇMAZ** |
| Saf sınıflar | `EkstreAyristirici` (CSV, DbContext görmez) · `CariEslestirici` (tr-TR ünvan normalleştirme + Jaccard) · `DedupeAnahtari` |
| Refaktör | `BankaModulu.HareketEkle` → `HareketEkleIc(hesapId, dto, islem, ekstreSatirId, bankaReferans)`; **davranış değişmedi**, MASAK iki fazı ve yön katsayısı birebir aynı |
| Uçlar | 6 uç (§ 2), hepsinde `[ModulYetkisi("banka")]` + sınıf düzeyi `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` |
| Ekran | YENİ `/banka/ekstre/{IthalatId}` (sayfalı, süzgeçli, satır başına düzenleme) + `/banka`'ya "+ Ekstre Yükle" (Çekmece) |
| Menü | **DEĞİŞMEDİ** — `KenarCubugu.razor` 'banka' satırına dokunulmadı |
| Denetim | `denetim.sh`'e `BankaHareketleri.Add` kuralı eklendi (İLK İŞ olarak, kod yazılmadan önce) |
| Sözlük | `SozlukBanka`'ya 37 yeni anahtar, TR + EN ikisi de dolduruldu; ekranda çıplak metin yok |
| İstemci tipleri | `Kuyumcu.Core/Sozlesmeler/EkstreSozlesmeleri.cs` — Pages altına **yeni record YAZILMADI** (tavan 129 korundu) |

### 1.1 Üç cümlelik sözleşme (kod + test ile sabitlendi)
1. **Yükleme hiçbir şey yazmaz.** `/ekstre/yukle` `banka_hareketleri`'ne **sıfır** satır yazar; yanıt `uretilenHareketSayisi: 0` alanını **sunucudan** döner.
2. **Onay olmadan hareket yok.** Yalnız `POST /onayla` üretir ve **yalnız gövdede kimliği açıkça verilen** satırlar için. "Tümünü onayla" ucu **yoktur**; boş liste 400, 500'den fazla satır 400.
3. **Tek yazma yolu.** `Onayla` → `BankaServisi.HareketEkleIc`. `EkstreModulu.cs`'te `BankaHareketleri.Add` **geçmez**; `denetim.sh` bunu CI'da zorlar (mutasyon M32-15 kanıtı).

---

## 2. Uçlar

| Fiil | Yol | Roller | Kapılar |
|---|---|---|---|
| GET | `/api/v1/bankalar/ekstre/sablon.csv` | SAHIP·MUDUR·MUHASEBE | — |
| POST | `/api/v1/bankalar/{id}/ekstre/yukle?tekrar=` | SAHIP·MUDUR·MUHASEBE | `[RequestSizeLimit(3_000_000)]` · `[EnableRateLimiting(OranSiniri.Ekstre)]` (20/sa) · ≤2 MB ham · ≤10.000 satır · ≤2.000 karakter/satır · ≤50 alan/satır |
| GET | `/api/v1/bankalar/ekstre/{id}?durum=&limit=&offset=` | SAHIP·MUDUR·MUHASEBE | `limit ∈ [1,100]` — hem `[Range]` hem servis içi (iki katman, ayrı ölçüldü) |
| PATCH | `/api/v1/bankalar/ekstre/satir/{id}` | SAHIP·MUDUR·MUHASEBE | ONAYLANDI → 409 · IBAN çakışması → 409 |
| GET | `/api/v1/bankalar/ekstre/satir/{id}/oneriler` | SAHIP·MUDUR·MUHASEBE | ilk 500 aktif cari (TB-19) |
| POST | `/api/v1/bankalar/ekstre/{id}/onayla` | SAHIP·MUDUR·MUHASEBE | boş liste 400 · >500 satır 400 · `[EnableRateLimiting(OranSiniri.Ekstre)]` · satır bazında MASAK kapısı |

**PATCH sözleşmesi:** `cariId` ve `durum` **nullable**'dır. Gövdede yoksa alan **değişmez**; `cariId: ""` bağı **siler**. Tam gövdeyle satırı üzerine yazan uç yazılmadı (kurul engel 4).

---

## 3. Sapmalar ve gerekçeleri

| # | Spesifikasyon | Yapılan | Gerekçe |
|---|---|---|---|
| **S-1** | Faz B: MT940 ayrıştırıcı (`:20: :25: :28C: :60F: :61: :86: :62F:`) | **YAZILMADI.** `format=MT940` **400** döner ve mesaj sebebi söyler | Kapsam kilidi Faz A'dır; ayrıca elimizde **gerçek banka MT940 çıktısı yok**. "Kamuya açık dokümana göre yazıldı, doğrulanmadı" bir kanıt boşluğudur (Anayasa md.1) ve doğrulanmamış bir ayrıştırıcı sessizce yanlış tutar üretirdi. Faz B'nin sevk kararı ayrı verilir. |
| **S-2** | `denetim.sh` kuralı `BankaHareketleri.Add = 1` | Kural **= 3** yazıldı, yorumda üç yol **isimle** sayıldı | **ÖLÇÜLDÜ:** paket 32'den ÖNCE zaten 3 yazma yolu vardı — `BankaModulu.cs` (meşru kapı), `EvrakModulu.cs:511` (çek tahsili, paket 29), `AcilisServisi.cs:399` (açılış bakiyesi, T-2). "1" yazmak ya yalan bir sayaç ya da kapsam dışı iki refaktör olurdu. Kuralın koruduğu şey **sayının artmamasıdır**: 4. yol açılınca çıkış kodu 1 (M32-15 kanıtı). Teknik borç **TB-32-1**. |
| **S-3** | `ham_satir VARCHAR(500)` | Kolon tipi **TEXT**, içerik 500 karaktere kırpılıp **AES şifreli** yazılır | Şifreli metin düz metinden uzundur; VARCHAR(500) şifreli değeri kesip veriyi bozardı. Kırpma 500'de kalıyor, saklama biçimi değişti. Aynı gerekçeyle `karsi_iban` VARCHAR(34) → VARCHAR(255). |
| **S-4** | `(tenant_id, iban)` BENZERSİZ | Benzersizlik **`(tenant_id, iban_hash)`** üzerinde | AES-GCM her yazmada farklı nonce üretir → aynı IBAN farklı şifreli metne iner ve şifreli sütundaki UNIQUE kısıtı **hiç tetiklenmezdi**. `MasakHashDinleyicisi` deseni (AS-2 kararı). Kural aynı: bir IBAN iki cariye bağlanamaz (M32-8). |
| **S-5** | "Para birimi hesabın para birimidir; MT940 `:60F:` uyuşmazsa dosya 400" | Para birimi hesaptan yazılıyor; **uyuşmazlık kontrolü YOK** | CSV'de para birimi alanı **yoktur** — karşılaştırılacak bir değer yok. Bu kontrol `:60F:` etiketine bağlıdır ve Faz B ile gelir. "USD hesaba TRY ekstresi 400" canlı adımı bu yüzden **koşulamadı** (§ 7, adım 21). |
| **S-6** | Canlı: "aynı dosyayı `?tekrar=true` ile yükle → **tüm** satırlar MUKERRER" | Ölçülen: 10 satır → **6 mükerrer / 3 yeni / 1 hatalı** | Mükerrer ölçütü **defterdeki hareketle** karşılaştırmadır, sahne satırıyla değil. İlk ithalatta yalnız 5 satır onaylanmıştı; onaylanmayanlar deftere hiç yazılmadığı için ikinci yüklemede hâlâ YENİ'dir. Beklenti spesifikasyonda "hepsi onaylanmış" varsayımıyla yazılmış. Ölçüm doğrudur, tasarım doğrudur. |
| **S-7** | Dedupe anahtarında `tarih(yyyy-MM-dd)` | **İşletme günü** (`Zaman.UtcdenYerele`) kullanıldı | **BULUNAN HATA:** elle dekont `2026-09-01T00:00Z`, aynı havalenin ekstre satırı `2026-08-31T21:00Z` (yerel gün başı) olarak saklanıyor. UTC gününe bakan anahtar bu iki **aynı** havaleyi farklı sanıyordu ve mükerrer koruması sessizce hiç çalışmıyordu. `Elle_girilmis_dekontun_aynisi_ekstrede_MUKERRER_isaretlenir` testi bunu kırmızıyla yakaladı. |
| **S-8** | `ham_satir` "şifreli saklanır **ya da** IBAN maskelenerek yazılır" | **İKİSİ DE**: diskte AES şifreli, **ekrana giden kopyada IBAN maskeli** | Test (`Ekranin_gordugu_IBAN_MASKELIDIR`) ilk turda **kırmızı** geldi: ham satır ekrana tam IBAN'la gidiyordu. Yalnız diskte şifrelemek, şifrelemeyi "yedeği koruyan" bir önleme indirgerdi (paket 47'nin "ekrana giden kopya maskelidir" kararıyla aynı). |
| **S-9** | Elle dekontta mükerrer davranışı yazılmamıştı | Aynı anahtarlı ikinci **elle** dekont artık **409** döner | UNIQUE indeksin doğal sonucudur. Mesaj çıkmaz sokak bırakmaz: "gerçekten ikinci bir işlemse banka referansını ya da açıklamayı ayırt edici doldurun" (referans/açıklama anahtara girer). **Davranış değişikliğidir**, raporda açıkça duruyor. |
| **S-10** | Yükleme formu için modal | **Cekmece** kullanıldı | DoD EK 2: ModalPencere **yalnız yıkıcı onay** içindir. Onay modalı (geri alınamaz toplu üretim) modal kaldı; yükleme formu Çekmece'ye alındı. `denetim.sh` ModalPencere sayacı bu yüzden yalnız **+1** (50→51) ve gerekçesi betiğin içinde yazılı. |

### 3.1 Mevzuat / doğrulanmamış hükümler
- **AS-3 (döviz ekstresi TL karşılığı)** — kapsam DIŞI, kod yazılmadı. `banka_hareketleri`'nde `kur_snapshot_id` yok; bu paket hesabın para biriminde saklar ve **çevirmez**.
- **AS-5 (gün kilidi)** — kapsam DIŞI. Kapanmış güne düşen satır onaylanabilir; denetim izinde satır sayısıyla birlikte kayıtlıdır. Gelecek tarihli satır **HATALI**'dır (paket 41 K41-1 ile aynı gerekçe).
- **KVKK / ADR-0003 kapsamı** — `cari_ibanlari.iban`, `ekstre_satirlari.karsi_iban` ve `ham_satir` **şifreli**. Mevcut `BankaHareket.KarsiIban` **düz metin kaldı**; geriye dönük şifreleme bu pakette yapılmadı → **TB-32-2**.

---

## 4. Migration

**Ad:** `20260918002942_BankaEkstresiIceAktarma`
- **Up:** 3 tablo + `banka_hareketleri`'ne 3 kolon + 8 indeks (2'si BENZERSİZ: `(tenant_id, banka_hesap_id, dedupe_anahtar)` ve `(tenant_id, iban_hash)`, ayrıca `(tenant_id, ithalat_id, satir_no)`).
- **Geriye doldurma YAPILMAZ:** paket 32 öncesi satırların `dedupe_anahtar`'ı NULL kalır. MySQL'de NULL benzersizliği ihlal etmediği için **indeks geçmişe karşı koruma sağlamaz**. Bu sınır hem kolon yorumunda, hem `DbContext` yorumunda, hem **ekranda** (`#ekstre-mukerrer-sinir`) yazılıdır. **"Mükerrer imkânsız" denmedi.**
- **Uygulama kanıtı (`kuyumcu_net`):** `SHOW TABLES LIKE 'ekstre%'` → `ekstre_ithalatlari`, `ekstre_satirlari`; `cari_ibanlari` var; `dedupe_anahtar varchar(64) YES NULL`.
- **İleri/geri:** `database update MasakBankaKaynagi` → "Reverting migration ... Done"; sonra `database update` → "Applying migration ... Done". Down sonrası mevcut dekontlar **okunabilir kaldı** (`banka_hareketleri` 23 satır). ⚠ Down üç sahne tablosunu **düşürür**, yani sahne verisi gider — bu tabloların doğası gereği (defter değil, prova kaydı) kabul edilmiş davranıştır.

---

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `EkstreAyristiriciTestleri` + `CariEslestirmeTestleri` [saf] | 23 | 23 | 0 |
| `EkstreIceAktarmaTestleri` [HTTP + MySQL] | 25 | 25 | 0 |
| `EkstreIzolasyonTestleri` [HTTP + MySQL] | 12 | 12 | 0 |
| `EkstreEkranTestleri` [bUnit] | 15 | 15 | 0 |
| **TAM SÜİT (son)** | **2698** | **2698** | **0** |

**Önce/sonra:** dalga kapısı kaydında 1650 idi; bu paket öncesi ölçülen taban **2623**, sonrası **2698** → **+75 yeni test**.
TRX: `<scratchpad>/trx-p32/`.

**Tam süitte yakalanan 4 kırmızı (bu paketin açtığı, düzeltildi):**
- `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` → üç yeni varlık `DuzeltmePolitikasi` haritasına eklendi (EkstreSatiri = DUZENLENEBILIR, EkstreIthalati/CariIban = SALT_OKUNUR + doğru düzeltme yolu).
- `RotaBaslikTestleri` ×3 → `/banka/ekstre/{IthalatId}` `RotaBasliklari.Harita`'ya eklendi (yoksa üst bar "Sayfa bulunamadı" yazıyordu), rota sayacı 39 → 40.

---

## 6. Mutasyon tablosu

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M32-1 | `Yukle` satırları doğrudan deftere yazsın | `Yukleme_sonrasi_banka_hareketleri_SIFIR_ekstre_satirlari_N_olur` | `cmp` AYNI |
| M32-2 | Defterdeki anahtar kontrolü kaldırıldı | `Elle_girilmis_dekontun_aynisi_ekstrede_MUKERRER_isaretlenir` | `cmp` AYNI |
| M32-3 | Boş `satirIdler` kabul edilsin | `Bos_satirIdler_listesi_400_doner` | `cmp` AYNI |
| M32-4 | 500 satır tavanı kaldırıldı | `Bes_yuzden_fazla_satir_onaylanmaya_calisilirsa_400_doner` | `cmp` AYNI |
| M32-5 | 409 **katman 1** (durum kontrolü) kaldırıldı | `Ayni_satir_ikinci_kez_onaylanamaz_ve_IKINCI_hareket_olusmaz` | `cmp` AYNI |
| M32-6 | 409 **katman 2**: modelden `IsUnique()` kaldırıldı | ⚠ **YEŞİL KALDI — TEST AÇIĞI.** Şema migration'dan kurulduğu için model mutasyonu kısıtı düşürmüyor | `cmp` AYNI |
| **M32-6b** | Aynı kural **migration**'da `unique: true` → kaldırıldı | `Benzersiz_indeks_ayni_anahtarli_IKINCI_hareketi_VERITABANINDAN_reddeder` **KIRMIZI** | `cmp` AYNI |
| M32-7 | Ünvan skoru eşik üstünde **otomatik bağlasın** | `Unvan_benzerligi_ASLA_otomatik_baglamaz_oneri_ucu_yalnizca_SIRALAR` | `cmp` AYNI |
| M32-8 | "IBAN başka cariye bağlı" 409'u kaldırıldı | `Ayni_IBANi_ikinci_bir_cariye_baglama_denemesi_409_doner` | `cmp` AYNI |
| M32-9 | Ayrıştırıcı bozuk satırı **atlasın** (HATALI yazmasın) | `Bozuk_tarihli_satir_HATALI_isaretlenir_digerleri_AYRISMAYA_DEVAM_EDER` | `cmp` AYNI |
| M32-10 | Gelecek tarih kontrolü kaldırıldı | `Gelecek_tarihli_satir_HATALI_olur_ve_onaya_SECILEMEZ` | `cmp` AYNI |
| M32-11 | 10.000 satır tavanı kaldırıldı | `Satir_tavani_asilirsa_400_ve_KISMI_sonuc_DONMEZ` + `On_bin_bir_satirlik_dosya_400_doner_ve_HICBIR_TABLOYA_yazilmaz` | `cmp` AYNI |
| M32-12 | `UnvanNormalle` invariant kültürle yazıldı | `Tr_TR_kulturu_ilker_ILKER_olur_INVARIANT_KULTURDE_BILE` | `cmp` AYNI |
| M32-13 | Servis içi `limit` kontrolü kaldırıldı | ⚠ **YEŞİL KALDI — TEST AÇIĞI.** HTTP testi controller `[Range]` niteliğini ölçüyordu | `cmp` AYNI |
| **M32-13b** | Aynı mutasyon, **yeni** servis-katmanı testine karşı | `Servis_katmani_da_limit_tavanini_kendisi_uygular` **KIRMIZI** | `cmp` AYNI |
| M32-14 | `KarsiIban` + `HamSatir` şifreleme dönüştürücüsü kaldırıldı | `Karsi_IBAN_ve_ham_satir_DISKTE_SIFRELI_saklanir` | `cmp` AYNI |
| **M32-15** | `Onayla` içine `db.BankaHareketleri.Add(...)` yazıldı | `./denetim.sh` → `✗ BankaHareketleri.Add 4 (beklenen 3)` · **çıkış kodu 1** | `cmp` AYNI |
| M32-16 | Ekran `limit`/`offset` göndermesin | `Ilk_acilista_SAYFALI_istek_atilir_limit_100_offset_0` | `cmp` AYNI |
| M32-17 | MUKERRER satır varsayılan **seçili** gelsin | `MUKERRER_satirin_onay_kutusu_VARSAYILAN_OLARAK_ISARETSIZ` | `cmp` AYNI |
| M32-18 | `Onayla` kapı hatasını yutup ONAYLANDI yazsın | `MASAK_kapisi_toplu_onayda_da_calisir_esik_ustu_satir_kimliksiz_GECMEZ` | `cmp` AYNI |
| M32-19 | Ham satır maskesiz gitsin | `Ekranin_gordugu_IBAN_MASKELIDIR` | `cmp` AYNI |
| M32-20 | Aynı dosya 409'u kaldırıldı | `Ayni_dosya_ikinci_kez_yuklenince_409_ve_IKINCI_ithalat_olusmaz` | `cmp` AYNI |
| M32-21 | ONAYLANDI satırda PATCH 409'u kaldırıldı | `Onaylanmis_satirda_PATCH_409_doner` | `cmp` AYNI |
| M32-22 | MT940 reddi kaldırıldı (sessizce CSV gibi okusun) | `MT940_formati_400_ile_reddedilir_sessizce_CSV_gibi_okunmaz` | `cmp` AYNI |

**23 mutasyon · 21 doğrudan kırmızı · 2 test açığı bulundu ve testler güçlendirilerek kırmızıya çekildi (M32-6b, M32-13b).**
Son mutasyondan sonra: `dotnet build Kuyumcu.sln` → **0 hata 0 uyarı**, `./denetim.sh` → **TEMİZ**.

---

## 7. Canlı doğrulama (localhost:5198 · `kuyumcu_net` · kiracı `PAKET32-TEST Kuyumculuk`)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | Kiracı + TRY hesap + 2 cari | 200/201 | **200/201** | curl |
| 2 | Elle 1 dekont (5.000 TL GELEN) | 200, `dedupe_anahtar` DOLU | **200** · SQL `COUNT=1, dedupe NOT NULL=1` | curl + SQL |
| 3 | 10 satırlık CSV yükle | 10 / 8 yeni / 1 mükerrer / 1 hatalı · hareket **1'de kalır** | **HTTP 200** · `{'satirSayisi':10,'yeniSayisi':8,'mukerrerSayisi':1,'hataliSayisi':1,'uretilenHareketSayisi':0}` · SQL `banka_hareketleri=1`, `ekstre_satirlari=10` | curl + SQL |
| 4 | Aynı dosya tekrar | 409, ikinci ithalat YOK | **409** · SQL `ekstre_ithalatlari=1` | curl + SQL |
| 5 | Satır listesi `?limit=100&offset=0` | 200, sayfalı | **200** · `toplam 10 limit 100` · 1=MUKERRER, 6=HATALI ("Tarih çözülemedi: '99.99.9999'"), diğerleri YENI | curl |
| 6 | 3. satırı cariye bağla | 200, IBAN öğrenilir | **200** · SQL `cari_ibanlari` → `kaynak=OGRENILDI`, `iban` sütunu **`v1:WKGEVXiX/…`** (şifreli), `iban_hash=15540532D5CE…` | curl + SQL |
| 7 | 5 satır onayla | 200, 5 hareket, bakiye net toplam kadar | **200** `{basarili:5, uretilenHareketSayisi:5}` · SQL `hareket=6` · `bakiye=-2595.40` (5000 −12500 +7250,50 +1000 −45,90 −3300 = **−2595,40** elle hesapla birebir) · `ekstre_satirlari ONAYLANDI=5, banka_hareket_id dolu=5` | curl + SQL |
| 8 | Aynı 5 satırı yeniden onayla | İkinci hareket YOK | **200** `{basarili:0, basarisiz:5}` · her satır "Bu satır zaten onaylanmış" · SQL `hareket=6` | curl + SQL |
| 9 | Boş `satirIdler` | 400 | **400** | curl |
| 10 | ONAYLANDI satırda PATCH | 409 | **409** "Onaylanmış ekstre satırı değiştirilemez… storno/düzeltme fişi" | curl |
| 11 | `?tekrar=true` ile aynı dosya | mükerrer artar, hareket artmaz | **200** `{yeni:3, mukerrer:6, hatali:1}` · SQL `hareket=6` (sapma S-6) | curl + SQL |
| 12 | Öğrenme kanıtı (2. ithalat) | aynı IBAN'lı satırlar otomatik eşleşir | SQL: satır 3 ve 8 → `eslesme_kaynagi=IBAN`, `eslesen_cari_id` DOLU | SQL |
| 13 | `GET /ekstre/sablon.csv` | 200, beklenen başlıklar | **200** · `tarih;aciklama;borc;alacak;bakiye;referans;karsiIban;karsiUnvan` | curl |
| 14 | TEZGAHTAR ile yükleme | 403 | **403** | curl |
| 15 | Kimliksiz istek | 401 | **401** | curl |
| 16 | 4,63 MB gövde | 413 | **413** "İstek gövdesi çok büyük" | curl |
| 17 | 10.001 satır | 400, hiçbir tabloya yazılmaz | **400** "…tavan 10000… Kısmi içe aktarım YAPILMAZ" | curl |
| 18 | `format=MT940` | 400 | **400** "MT940 ayrıştırıcısı bu sürümde YOKTUR (Faz B)…" | curl |
| 19 | `GET /oneriler` | otomatik uygulanan 0 | **200** · `esik 0.6`, `otomatikUygulanan 0`, `oneriler [('ALI VELI', 1)]` — **skor 1,00 olmasına rağmen bağlanmadı** | curl |
| 20 | Denetim izi | yükleme ve onay AYRI kayıt | SQL: `ekstre yüklendi · … · 10 s…` / `ekstre onaylandı · seçilen: 5 · başarılı: 5 · … üretilen banka hareketi: 5` / `… başarılı: 0 · hatalı: 5 · üretilen banka hareketi: 0` | SQL |
| 21 | USD hesaba TRY ekstresi → 400 | — | **KOŞULAMADI** — CSV'de para birimi alanı yok (sapma S-5); kontrol `:60F:` etiketine bağlıdır ve Faz B ile gelir | — |
| 22 | Migration ileri/geri | Down düşürür, Up geri koyar, dekontlar okunur | **Reverting… Done** → `SHOW TABLES LIKE 'ekstre%'` boş, `banka_hareketleri` **23 satır okunabilir**; **Applying… Done** → 3 tablo geri geldi | SQL |

**MASAK kapısı kanıtı:** canlı adımda tetiklenmedi (tutarlar eşik altı); HTTP testiyle kanıtlandı — 200.000 TL'lik tek satırın onayı kimlik bilgisi olmadan **geçmez**, `basarili=0` ve `banka_hareketleri=0` kalır (`MASAK_kapisi_toplu_onayda_da_calisir_esik_ustu_satir_kimliksiz_GECMEZ`, mutasyon M32-18). Paket 41 **sevk edilmiş** durumdadır (`BankaServisi` içinde `MasakKapisi` iki fazlı bağlı) — kanca bırakmaya gerek kalmadı.

**Temizlik:** API PID `kill` edildi → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **0 satır**. **5199 (kurucunun inceleme sunucusu) çalışmaya devam ediyor, dokunulmadı.** Test kiracısı `tenant_id` kolonu olan **53 tablodan** + `kiraci_davetleri` + `tenants`'tan silindi:

```
tenants = 8 (başlangıç 8)  ·  banka_hareketleri = 17 (başlangıç 17)
ekstre_ithalatlari = 0 · ekstre_satirlari = 0 · cari_ibanlari = 0
```
`QA-TEST-1/2/3` kiracıları **dokunulmadı**.

---

## 8. Denetim çıktısı

```
  ✓ BankaHareketleri.Add (yalnız BankaServisi)    3
  ✓ IKiraciVarlik uygulayan varlık                54
  ✓ Pages içinde istemci record tanımı          129 (tavan 129)
  ✓ IgnoreQueryFilters (yasak)                    0
  ✓ Find/FindAsync (yasak)                        0
  ✓ ExecuteUpdate/Delete (beyaz liste 5)          5
  ✓ Finansal varlıkta [HttpDelete] (0 olmalı)     0
DENETİM TEMİZ.
```

`denetim.sh`'te **üç sayaç** gerekçeli olarak güncellendi (hepsi betiğin içinde yazılı):
- `BEKLENEN_VARLIK` 47 → **50** (3 yeni `IKiraciVarlik`).
- ModalPencere tavanı 50 → **51** — tek yeni modal, **geri alınamaz toplu üretimin onayıdır** (DoD EK 1/4 zorunlu kılıyor). Yükleme formu Çekmece'dir, düzenleme modalı sayısı **artmadı**.
- Pages dosya sayısı 39 → **40** — kapsam kilidinin açıkça izin verdiği tek yeni ekran; menüye satır eklenmedi.

`ExecuteUpdate/Delete` beyaz listesi **büyütülmedi** (toplu durum güncellemesi izlenen varlıklarla yapılıyor: `SayaclariTazele`). `IgnoreQueryFilters` **0**. Pages istemci record sayacı **artmadı** (tüm yanıt tipleri `Kuyumcu.Core.Sozlesmeler`'de).

---

## 9. DoD sözleşmesi

| Varlık | Düzenlenebilir mi | Uç | Gerekçe |
|---|---|---|---|
| `EkstreSatiri` | **EVET** (ONAYLANMADAN önce) | `PATCH /bankalar/ekstre/satir/{id}` — cari bağla / bağı kaldır (`cariId: ""`) / atla / **atlamayı geri al** | Ekranda satır başına cari seçici + Atla/Geri al düğmesi |
| `EkstreSatiri` (ONAYLANDI) | **HAYIR** | PATCH **409** | Ekranda `DegismezIpucu`: gerekçe (bu satırdan banka hareketi üretildi) + **doğru yol** (üretilen hareketin storno/düzeltme fişi). Sessiz disabled düğme **yok**. |
| `EkstreIthalati` | Dolaylı | satır uçları üzerinden | Sahne başlığıdır; üretilen hareketlerin kaynağı olduğu için **silinmez**. "Pasife alma" karşılığı satırları ATLANDI yapmaktır. |
| `CariIban` | Dolaylı | `PATCH …/satir/{id}` ile `cariId` | İkinci bir "IBAN yönetimi" yüzeyi açmak, "bir IBAN tek cariye" kuralını iki yerden korumak demekti. |
| `BankaHareket` (üretilen) | **HAYIR** (finansal) | — | Hard DELETE **yok**; düzeltme storno/düzeltme fişi yoludur (K-UX-2). |

**Finansal varlıkta hard DELETE:** yok. `ekstre_ithalatlari` / `ekstre_satirlari` silinmez; iptal yalnız durum alanıyla (`ATLANDI`).
**Denetim kaydı:** `DenetimServisi.YazmaKaydet` **çağrılır** (yeniden yazılmadı) — yükleme ve onay ayrı satır, satır sayısıyla.
**Her düğme:** ekrandaki her `<button>` ya gerçek istek atar ya `disabled` + görünür sebep taşır (`#ekstre-onay-sebep`, Çekmece `KilitSebebi`). `denetim.sh` "ölü düğme" sayacı **artmadı**.

---

## 10. Değişen dosyalar

**Yeni (11):**
```
dotnet/Kuyumcu.Api/Data/BankaEkstresi.cs
dotnet/Kuyumcu.Api/Modules/Banka/EkstreAyristirici.cs
dotnet/Kuyumcu.Api/Modules/Banka/CariEslestirici.cs
dotnet/Kuyumcu.Api/Modules/Banka/EkstreModulu.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260918002942_BankaEkstresiIceAktarma.cs (+ .Designer.cs)
dotnet/Kuyumcu.Core/Sozlesmeler/EkstreSozlesmeleri.cs
dotnet/Kuyumcu.Web/Pages/BankaEkstre.razor
dotnet/Kuyumcu.Tests/EkstreAyristiriciTestleri.cs
dotnet/Kuyumcu.Tests/CariEslestirmeTestleri.cs
dotnet/Kuyumcu.Tests/EkstreIceAktarmaTestleri.cs
dotnet/Kuyumcu.Tests/EkstreIzolasyonTestleri.cs
dotnet/Kuyumcu.Tests/EkstreEkranTestleri.cs
```

**Değişen (11):**
```
dotnet/Kuyumcu.Api/Data/Entities.cs                  (BankaHareket + 3 kolon)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs          (3 DbSet + 8 indeks + 3 şifreleme dönüştürücüsü)
dotnet/Kuyumcu.Api/Data/MasakHashDinleyicisi.cs      (CariIban + EkstreSatiri hash doldurma)
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs      (HareketEkleIc refaktörü + dedupe + 409)
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs             (EkstreServisi DI)
dotnet/Kuyumcu.Api/Common/OranSiniri.cs              (Ekstre politikası 20/sa)
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs (3 yeni varlık damgası)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                (37 anahtar × TR + EN)
dotnet/Kuyumcu.Web/Pages/Banka.razor                 ("+ Ekstre Yükle" + Çekmece)
dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs          (yeni rota başlığı)
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs           (39 → 40 rota)
dotnet/denetim.sh                                     (yeni kural + 3 gerekçeli sayaç)
```

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** `Pages/Banka.razor` (ilk dokunuş bu dalgada), `Pages/BankaEkstre.razor` (yeni).
**Dokunulmayan:** `KenarCubugu.razor`, `MIMARI-KALAN-PAKETLER-2026-09-15.md`, paket spesifikasyonu, `SONRAKI-OTURUM-PROMPT.md`.
**COMMIT YOK** — çalışma ağacında.

---

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Kapsam kilidi (3 tablo · 3 kolon · 6 uç · 1 ekran · menü değişmez) | **KARŞILANDI** | § 1, § 2; `KenarCubugu.razor` değişmedi |
| `denetim.sh` `BankaHareketleri.Add` kuralı İLK İŞ | **KARŞILANDI (sayı sapmasıyla)** | Kural kod yazılmadan önce eklendi; sayı 3 — sapma S-2 |
| `HareketEkleIc` refaktörü, davranış değişmez | **KARŞILANDI** | Tam süit yeşil; MASAK/banka regresyon testleri (`MasakBankaTestleri`, `BankaPasifeAlmaTestleri`) yeşil |
| `Onayla` yalnız `HareketEkleIc` çağırır | **KARŞILANDI** | M32-15 → `denetim.sh` çıkış kodu 1 |
| Paket 41 MASAK kapısı atlanmıyor | **KARŞILANDI** | M32-18 + `MASAK_kapisi_toplu_onayda_da_calisir…` |
| AS-2 şifreleme (IBAN + ham satır, HMAC hash) | **KARŞILANDI** | M32-14 + canlı SQL (`v1:WKGEVXiX/…`) |
| Kiracı izolasyonu (IKiraciVarlik, filtre, 404'ler) | **KARŞILANDI** | `EkstreIzolasyonTestleri` 12/12 |
| `IgnoreQueryFilters` = 0 · ExecuteUpdate beyaz listesi büyümedi | **KARŞILANDI** | `denetim.sh` TEMİZ |
| Yükleme deftere SIFIR yazar | **KARŞILANDI** | M32-1 + canlı adım 3 |
| "Tümünü onayla" ucu yok · boş 400 · >500 400 | **KARŞILANDI** | M32-3, M32-4 + canlı adım 9 |
| Finansal varlıkta hard DELETE yok | **KARŞILANDI** | `denetim.sh` "Finansal varlıkta [HttpDelete] = 0" |
| Dedupe anahtarı elle girişte de dolu | **KARŞILANDI** | Canlı adım 2 (`dedupe NOT NULL=1`) |
| "Mükerrer imkânsız" DENMEDİ | **KARŞILANDI** | Ekranda `#ekstre-mukerrer-sinir`; kolon/DbContext yorumları |
| 409 iki katman AYRI ölçüldü | **KARŞILANDI** | M32-5 (durum) + M32-6b (indeks) |
| Fail-closed ayrıştırma (HATALI, atlanmaz) | **KARŞILANDI** | M32-9 + canlı adım 5 (satır 6 HATALI) |
| DoS tavanları uç kuralında | **KARŞILANDI** | Canlı 16/17 + M32-11 + saf testler (satır uzunluğu, alan sayısı) |
| `[RequestSizeLimit]` · `OranSiniri.Ekstre` · `[ModulYetkisi]` · TEZGAHTAR 403 | **KARŞILANDI** | Canlı 14/16 + `EkstreIzolasyonTestleri` |
| Otomatik eşleşme YALNIZ IBAN; ünvan asla bağlamaz | **KARŞILANDI** | M32-7 + canlı adım 19 (skor 1,00, bağlanmadı) |
| Bir IBAN tek cariye; ikinci deneme 409 + sahip adı | **KARŞILANDI** | M32-8 + `Ayni_IBANi_ikinci_bir_cariye…` |
| `UnvanNormalle` açıkça tr-TR; test invariant kültürde koşar | **KARŞILANDI** | M32-12 |
| PATCH "gönderilmeyen alan = koru" | **KARŞILANDI** | `PATCH_gonderilmeyen_alani_DEGISTIRMEZ` + bUnit gövde testleri |
| Aynı pakette yüklenen veri düzenlenebilir | **KARŞILANDI** | § 9 DoD tablosu |
| Pages'de yeni istemci record YOK | **KARŞILANDI** | `denetim.sh` 129/129 |
| Uçtan uca deserialize testi (gerçek JSON) | **KARŞILANDI** | `EkstreEkranTestleri` — sahte API sunucu DTO'suyla birebir |
| Ekran sayfalı · sunucuda Count · sınırsız ToListAsync yok | **KARŞILANDI** | M32-16 + `Limit_tavani_asilirsa…` + servis katmanı testi |
| `ham_satir` yalnız metin (MarkupString yok) · CSV enjeksiyon tırnağı sökülür | **KARŞILANDI** | `Ham_satir_METIN_olarak_basilir_MarkupString_YOK` + `CSV_enjeksiyon_tirnagi…` |
| Boş durum + hata/başarı bandı | **KARŞILANDI** | `BosDurum`, `HataBandi`, `BasariBandi`; UX-S28 (hata boş duruma çevrilmiyor) |
| ADR ortak belgesi (AS-1) | **KARŞILANMADI** | § 12/1 — açık kalem |
| 375 px taşma ölçümü | **ÖLÇÜLMEDİ** | § 12/2 — açık kalem |

---

## 12. Açık kalanlar

1. **Ortak içe aktarım ADR'si YAZILMADI (AS-1).** Önkoşul doğrulandı: `dokumanlar/02-mimari/adr/` altında içe aktarımı düzenleyen ADR **yok**; `0016` numarası `reyon-yerlesimi`'ne verilmiş. Paket 47 raporu da (§ Açık kalanlar/1) bunu "paket 32 ile ORTAK yazılacak, numara orkestratör/Bertan kalemidir" diye bırakmış. **Bu rapor ADR açmadı** — numara ve metin tek elden yazılmalı (önerilen: ADR-0017 "Dosyadan içe aktarım kapılıdır: deftere yazan tek yol korunur, insan onayı zorunlu, prova yazmaz"). Kod tarafı ilkeye **zaten uyuyor** (sahne tablosu, insan onayı, tek yazma yolu).
2. **375 px taşma ölçümü yapılmadı.** Yeni ekran `tablo-sar` sarmalayıcısı ve mevcut belirteçlerle yazıldı, satır içi `style` ve sabit renk yok; ama `scrollWidth ≤ clientWidth` tarayıcıda **ölçülmedi** (QA turu kalemi). 9 sütunlu tablo dar ekranda yatay kaydırma gerektirebilir.
3. **Faz B (MT940)** — sevk kararı ayrı verilecek; gerçek banka çıktısı olmadan yazılmaması bilinçli karardır.
4. **TB-32-1:** `EvrakModulu.cs:511` ve `AcilisServisi.cs:399` `HareketEkleIc`'e taşınmalı; taşınınca `denetim.sh` sayacı 3 → 1 olur.
5. **TB-32-2:** mevcut `BankaHareket.KarsiIban` / `KarsiTarafAd` düz metin; geriye dönük şifreleme ayrı paket + migration ister.
6. **Mükerrer geçmiş koruması yok** (NULL'lu eski satırlar). Geriye doldurma bilinçli olarak yapılmadı (Down geri dönülemez veri üretmesin).
7. **`ekstre_satirlari` saklama süresi ADR-0008'e bağlanmadı** — `SaklamaOnizlemeIsi` bu tabloyu bilmiyor (paket 47'nin `ham_json` borcuyla aynı).

---

## 13. Kurucuya / mali müşavire notlar

- **Ücretsiz pilot kuralı korundu:** bu pakette fiyat/teklif/tahsilat içeren hiçbir metin üretilmedi.
- **Mükerrer koruması geçmişe işlemez.** Programda **bu sürümden önce** girilmiş banka hareketleri mükerrer kontrolüne girmez; ekranda da böyle yazıyor. Eski dönem ekstresi yüklenirken elle karşılaştırma gerekir.
- **Aynı gün, aynı tutar, referanssız iki meşru havale** ayırt edilemez: ikincisi MUKERRER işaretlenir ve kullanıcıya bırakılır. Sessizce yutulmaz; kullanıcı "yine de al" diyebilir (referans/açıklama girilerek).
- **Elle dekont davranış değişikliği:** aynı hesapta aynı gün, aynı tutar, aynı yön ve aynı açıklamayla ikinci dekont artık **409** döner. Gerçekten iki ayrı işlemse banka referansı ya da açıklama ayırt edici doldurulmalıdır.
- **KVKK:** ekstredeki karşı taraf IBAN'ı ve ham satır **diske şifreli** iner, ekranda **maskeli** görünür. Mevcut dekontlardaki IBAN'lar hâlâ düz metindir (TB-32-2) — mali müşavir/kurucu kararına göre ayrı paketle şifrelenebilir.
- **Banka sözleşmesi gerekmez:** dosyayı müşteri kendi bankasından alır (TCMB DUY2026-13 uyarınca ÖHVPS'ye doğrudan bağlanma lisans ister ve yapılmadı).
