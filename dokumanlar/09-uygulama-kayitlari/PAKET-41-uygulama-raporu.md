# PAKET 41 — MASAK kapısının banka hareketlerine yayılması · uygulama raporu

> Tarih: 2026-09-18 · Dalga 4 · Roller: Taşkın (backend) + Ertuğ (frontend)
> Durum: **BİTTİ** · `dotnet build` 0 uyarı 0 hata · `dotnet/denetim.sh` TEMİZ ·
> tam süit **2622/2622 yeşil, 0 başarısız** · 12 mutasyon kanıtı · canlı doğrulama 5198'de yapıldı.

---

## 1. Ne yapıldı (özet)

MASAK kimlik tespiti eşiği bugüne kadar **yalnız defter hareketlerini** görüyordu; elle
girilen banka dekontu kapıyı hiç görmüyordu (`BankaModulu.cs` içinde "masak" kelimesi 0 kez
geçiyordu). Bu paket kapıyı banka yoluna yaydı ve kümülatif toplamı **çift saymaya yol
açmadan** iki kaynaktan okur hâle getirdi.

- `masak_kimlik_tespitleri.kaynak_tur` + `masak_beyanlari.kaynak_tur` (VARCHAR(20) NOT NULL
  DEFAULT 'HAREKET') ve `banka_hareketleri.hareket_id` (VARCHAR(36) NULL) kolonları,
  `(tenant_id, cari_id, olusturuldu)` indeksiyle **aynı migration'da** geldi.
- `MasakKapisi.GunlukIslemToplam` **iki ayrık sorguya** bölündü (defter tarafı aynen kaldı;
  banka tarafı `hareket_id IS NULL` süzgeciyle okunur). CASE/UNION/COALESCE bileşimi yazılmadı.
- `MasakKapisi.Kaydet` imzasına **zorunlu (varsayılansız)** `string kaynakTur` eklendi;
  derleyici defterdeki **12** çağrı noktasını tek tek elden geçirmeye zorladı.
- `BankaServisi.HareketEkle` iki fazlı kapıya bağlandı (Dogrula tx'ten önce, Kaydet tx içinde
  SaveChanges'ten önce) ve **ileri tarihli dekont 400** ile reddedilir hâle geldi.
- `/banka` dekont modalı mevcut `Bilesenler/MasakFormu.razor` bileşenini kullanır; ikiz form
  yazılmadı, POS taşınmadı.

---

## 2. K41-1 (Mehmet şartlı vetosu) nasıl kapatıldı

| Şart | Uygulama | Kanıt |
|---|---|---|
| Kümülatif `dto.Tarih`e BAĞLANMAZ | `masak.DogrulaKumulatif(..., now)` — `now = DateTime.UtcNow` (BankaModulu.cs) | test `DUN_tarihli_100000_arti_BUGUN_tarihli_100000_ikinci_istek_400` |
| Banka kümesi `Olusturuldu` ile okunur | `b.Olusturuldu >= bas && b.Olusturuldu < bit` (MasakKapisi.cs) | mutasyon **M41-5** (`b.Tarih`e çevrilince test KIRMIZI) |
| İleri tarihli dekont 400 | `if (tarih > now.AddMinutes(5)) throw BadRequestException("İleri tarihli dekont kabul edilmez")` | test `Ileri_tarihli_dekont_400_doner` + mutasyon **M41-6** · canlı: `[400]` |
| İndeks aynı migration'da, sorgunun okuduğu kolonda | `HasIndex(h => new { h.TenantId, h.CariId, h.Olusturuldu })` | canlı `SHOW INDEX` → 3 kolon |

> 5 dakikalık tolerans bilinçlidir: istemci saati ile sunucu saati arasındaki küçük kayma
> meşru dekontu reddetmesin diye. Yapılandırılabilir değildir (tek sabit, ADR-0005 kapsamı
> dışında bir teknik tolerans).

---

## 3. Spesifikasyondan sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | `Kaydet` çağrı noktası sayısı **8 değil, 12** (SatisModulu ×8 + CariModulu ×4). Koşul metni "10" diyordu. | Ölçüm: `grep -n "masak.Kaydet"` → SatisModulu 551, 660, 783, 1074, 1201, 1312, 1550, 1632; CariModulu 572, 649, 986, 1066. Hepsine açıkça `MasakKaynakTuru.Hareket` yazıldı. Tahmin edilmedi, sayıldı. |
| S2 | Dekont modalına **cari seçici** eklendi (`#dekont-cari`). | Spesifikasyonun kendi ekran şartı ("cari seçilmeden büyük tutar girilirse uyarı") ve paketin hedefi (100.000 satış + 100.000 havale) cari olmadan canlıda doğrulanamıyordu. Sunucu `cariId`'yi ZATEN kabul ediyordu; yeni uç/ekran açılmadı, mevcut `cariler` listesi kullanıldı (UX-S24). |
| S3 | `MasakFormu.razor`'a **opsiyonel `Degisti` EventCallback**'i eklendi. | bUnit kanıtıyla bulunan GERÇEK hata: alanlar çocuk bileşende bağlanıyor, "kaydedilebilir mi" kararı üst bileşende veriliyordu; Blazor yalnız olayın sahibi bileşeni yeniden çizdiği için **tüm alanlar dolduğu hâlde Kaydet KİLİTLİ kalıyordu**. Geri çağrı bağlanmadığında hiçbir şey yapmaz → `Islemler.razor` davranışı DEĞİŞMEDİ (608 ekran testi yeşil). Mutasyon M41-12 bunu kilitler. |
| S4 | Migration'daki `defaultValue` EF tarafından `""` üretildi, elle `"HAREKET"` yapıldı. | Koşul DEFAULT 'HAREKET' istiyor; EF varsayılanı 8 yıl saklanan eski satırları BOŞ kaynakla bırakırdı. Model tarafında `HasDefaultValue` KULLANILMADI (model ile anlık görüntü arasında sahte fark doğmasın; varlık zaten her zaman açık değer yazar). |
| S5 | Mutasyon listesinden "Dogrula tx içine alınırsa test kırmızı" ÇIKARILDI (Aşkın'ın düzeltmesi), yerine M41-10 ("Kaydet SaveChanges'ten SONRA çağrılırsa") kanıtlandı. | Bağlayıcı koşulda böyle yazıyor; tx dispose zaten geri alır, test yeşil kalırdı. |
| S6 | Spesifikasyon "GunlukIslemToplam'a banka kümesi eklenmezse → *satış + dekont kümülatifi* KIRMIZI" diyordu; ölçümde o test **yeşil kaldı**, yerine iki-dekont testleri kırmızıya döndü (M41-3). | Çapraz testte defter bacağı (100.000) + işlemin kendi tutarı (100.000) zaten 185.000'i aşıyor; banka kümesi o testte belirleyici değil. Banka kümesinin gerçekten sayıldığını **`Dekont_100000_arti_dekont_100000_ikinci_istek_400`** ve **`DUN_tarihli...`** kanıtlar. Gerçek ölçüm raporlanmıştır (Anayasa md.1). |

**Mevzuat işaretleri:** "Bankacılık kanalında kuyumcunun kimlik tespiti yükümlülüğü devam ediyor
mu" sorusu spesifikasyonda **DOGRULANMALI** işaretlidir. Kod bu hükmü **sıkı taraftan** (fail-closed)
uygular: havale de eşiğe tabidir. Eşik rakamları koda gömülü DEĞİL, `MasakEsikleri`
yapılandırmasından gelir (ADR-0005) — paket yeni eşik getirmedi. → **Kurucuya/mali müşavire not**
(bkz. § 10).

---

## 4. Migration

- Ad: **`20260917235727_MasakBankaKaynagi`**
- Up: `masak_kimlik_tespitleri.kaynak_tur` + `masak_beyanlari.kaynak_tur` (VARCHAR(20) NOT NULL
  DEFAULT 'HAREKET'), `banka_hareketleri.hareket_id` (VARCHAR(36) NULL),
  `IX_banka_hareketleri_tenant_id_cari_id_olusturuldu`.
- `kuyumcu_net` üzerinde uygulandı (`dotnet ef database update --connection ...` → `Applying migration ... Done.`).

**Canlı ölçüm (kuyumcu_net):**

```
SHOW COLUMNS FROM masak_kimlik_tespitleri LIKE 'kaynak_tur'; → kaynak_tur varchar(20) NO  HAREKET
SHOW COLUMNS FROM masak_beyanlari        LIKE 'kaynak_tur'; → kaynak_tur varchar(20) NO  HAREKET
SHOW COLUMNS FROM banka_hareketleri      LIKE 'hareket_id'; → hareket_id varchar(36) YES NULL
INDEX IX_banka_hareketleri_tenant_id_cari_id_olusturuldu    → tenant_id, cari_id, olusturuldu (3 kolon)
```

**İleri/geri ölçümü:** `database update KiymetliEvrak` → `Reverting migration ... Done.`; üç kolon
ve indeks sorgusu **0 satır** döndü. Ardından `database update` → `Applying migration ... Done.`;
üç kolon + 3 kolonlu indeks geri geldi, mevcut 3 tespit/3 beyan satırı `kaynak_tur='HAREKET'`
değeriyle korundu.

---

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Paket öncesi (ölçülen taban) | 2600 | 2600 | 0 | 0 |
| `--filter MasakBankaTestleri` | 15 | 15 | 0 | 0 |
| `--filter BankaEkranTestleri` | 6 | 6 | 0 | 0 |
| `--filter MasakEntegrasyonTestleri` | 10 | 10 | 0 | 0 |
| `--filter ~EkranTestleri` (regresyon) | 608 | 608 | 0 | 0 |
| **Tam süit** (`trx-p41`) | **2622** | **2622** | **0** | 0 |

**Yeni test sayısı: 22** (15 HTTP + 6 bUnit + 1 DB regresyonu) + mevcut 2 teste `kaynak_tur` assert'i eklendi.

Yeni dosyalar:
- `dotnet/Kuyumcu.Tests/MasakBankaTestleri.cs` [HTTP, gerçek MySQL] — eşik/sınır, yön ayrımı yok,
  beyan formu kademesi, çapraz kümülatif (defter+banka), iki dekont kümülatifi, **K41-1 geçmiş
  tarih**, ileri tarih reddi, `hareket_id` dolu satır kümülatife girmez, carisiz akış, yabancı
  kiracı 404, TEZGAHTAR 403, faturaya dönüştürme MASAK istemez.
- `dotnet/Kuyumcu.Tests/BankaEkranTestleri.cs` [bUnit] — eşik altı alan yok, eşik üstü
  `/masak/gereksinim` çağrısı, boş alanda Kaydet KAPALI + sebep, dolu alanda POST ve gövdedeki
  `masak` bloğunun alan adları, carisiz kümülatif uyarısı, DegismezIpucu.

---

## 6. Mutasyon kanıtları (her biri uygulandı → test kırmızı → `cmp` ile birebir geri alındı)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M41-1 | `DogrulaKumulatif` çağrısı kaldırıldı (kümülatifsiz `Dogrula(0m, ...)`) | `Esik_ustu_dekont_KIMLIKSIZ_...`, `GIDEN_yonde_de_esik...`, `Sinir_184999_99...`, `Satis_100000_arti_dekont...`, +5 (toplam 9) | cmp birebir |
| M41-2 | `masak.Kaydet(...)` çağrısı silindi | `Esik_ustu_dekont_KIMLIKLI_200_doner_ve_kaynak_tur_BANKA_HAREKET_yazilir` | cmp birebir |
| M41-3 | `GunlukIslemToplam` banka kümesini toplamıyor (`return defter;`) | `Dekont_100000_arti_dekont_100000_ikinci_istek_400`, `DUN_tarihli_100000_...` | cmp birebir |
| M41-4 | `hareket_id IS NULL` süzgeci kaldırıldı | `Hareketle_eslesmis_dekont_kumulatife_IKINCI_KEZ_sayilmaz` | cmp birebir |
| M41-5 | Banka penceresi `b.Olusturuldu` yerine `b.Tarih` ile okunuyor (**K41-1**) | `DUN_tarihli_100000_arti_BUGUN_tarihli_100000_ikinci_istek_400` | cmp birebir |
| M41-6 | İleri tarih reddi kaldırıldı | `Ileri_tarihli_dekont_400_doner` | cmp birebir |
| M41-7 | Kapı yalnız GELEN yönüne uygulanıyor | `GIDEN_yonde_de_esik_uygulanir_yon_ayrimi_yoktur` | cmp birebir |
| M41-8 | `kaynakTur` yok sayılıp sabit `BANKA_HAREKET` yazılıyor | `Defter_akislari_kaynak_tur_HAREKET_yazar_banka_degil`, `Tahsilat_EsikUstu_...`, `Takas_FarkKucukAmaDegerBuyuk_...` | cmp birebir |
| M41-9 | Eşik operatörü `>=` → `>` | `Sinir_184999_99_gecer_185000_00_reddedilir` + 4 test (toplam 5) | cmp birebir |
| M41-10 | `Kaydet`, `SaveChangesAsync`'ten SONRA çağrılıyor | `Esik_ustu_dekont_KIMLIKLI_200_doner_ve_kaynak_tur_BANKA_HAREKET_yazilir` | cmp birebir |
| M41-11 | Ekranda Kaydet kilidi kaldırıldı (`disabled="@kaydediliyor"`) | `Kimlik_alanlari_BOSKEN_kaydet_KAPALI_ve_sebebi_yazili` | cmp birebir |
| M41-12 | `Degisti` geri çağrısı kaldırıldı | `Kimlik_alanlari_DOLUNCA_POST_atilir_ve_masak_blogu_dogru_alan_adlariyla_gider` | cmp birebir |

Mutasyon betiği: `scratchpad/mut.sh`, yedekler `scratchpad/mut-yedek/p41/`. Son mutasyondan sonra
TEMİZ `dotnet build` (0 uyarı, 0 hata) ve tam süit yeniden koşuldu.

---

## 7. `HareketId`'yi tüketen okuma yollarının taraması (Bertan şartı)

ADR-0009 gereği yabancı anahtar yoktur; `hareket_id` artık iki tabloyu işaret edebilir.
Aşağıdaki yüzeylerin TAMAMI tarandı:

| Dosya:satır | Ne yapıyor | Karar |
|---|---|---|
| `Modules/Masak/MasakModulu.cs:90` (tespit listesi) | `m.HareketId` yansıtıyor | **DALLANDIRILDI** — yanıta `m.KaynakTur` eklendi |
| `Modules/Masak/MasakModulu.cs:110` (tespit detayı) | `m.HareketId` + beyan | **DALLANDIRILDI** — `m.KaynakTur` eklendi |
| `Modules/Masak/MasakModulu.cs:144` (belge no ile arama) | liste ile aynı alan kümesi | **DALLANDIRILDI** — `m.KaynakTur` eklendi |
| `Modules/Masak/MasakModulu.cs:167-183` (şüpheli bildirim oluştur) | **kullanıcıdan gelen** `dto.HareketId`'yi `hareketler`de doğruluyor | **ETKİLENMİYOR** — şüpheli bildirim ayrı bir varlıktır (`masak_supheli_bildirimler`), bu paket ona kaynak yazmaz; doğrulama bilinçli olarak defterle sınırlıdır |
| `Modules/Masak/MasakModulu.cs:272` (şüpheli listesi) | `s.HareketId` | **ETKİLENMİYOR** — aynı gerekçe |
| `Web/Servisler/UyumServisi.cs:129` `KimlikTespitSatir` | sunucu listesini karşılıyor | **DALLANDIRILDI** — `string? KaynakTur = null` eklendi (varsayılanlı; eski/bilinmeyen kayıt null) |
| `Web/Servisler/UyumServisi.cs:139` `KimlikTespitDetay` | detay | **DALLANDIRILDI** — `KaynakTur` eklendi |
| `Web/Servisler/UyumServisi.cs:150` `SupheliSatir` | şüpheli bildirim | **ETKİLENMİYOR** |
| `Web/Servisler/UyumServisi.cs:166` `SupheliOlusturDto` | kullanıcı girdisi | **ETKİLENMİYOR** |
| `Web/Servisler/UyumServisi.cs:200,210` (gider pusulası kayıtları) | farklı varlık (`gider_pusulalari`) | **ETKİLENMİYOR** |
| `Web/Pages/Uyum.razor:113-128` (tespit listesi satırı) | `HareketId` EKRANA BASILMIYOR | **ETKİLENMİYOR** (sütun yok) |
| `Web/Pages/Uyum.razor` tespit detayı (Çekmece) | tespitin künyesi | **DALLANDIRILDI** — "Kaynak" satırı eklendi (`#uy-tespit-kaynak`), ham kod değil sözlükten çevrilmiş ad basılır (UX-S31) |
| `Web/Pages/Uyum.razor:334-335,543` (şüpheli formu `hareketId` girdisi) | kullanıcı girdisi | **ETKİLENMİYOR** |
| `Modules/Evrak/EvrakModulu.cs:415` | yalnız yorumda geçiyor | **ETKİLENMİYOR** |

`MasakVarliklari.cs` içindeki `HareketId` XML yorumu "kaynak_tur'e göre `hareketler` ya da
`banka_hareketleri`" notuyla güncellendi (hem tespit hem beyan).

---

## 8. Canlı doğrulama (localhost:5198, kiracı `BANKA-MASAK-TEST`)

API `--urls http://localhost:5198` ile arka planda çalıştırıldı; **5199'a dokunulmadı**
(kurucunun inceleme sunucusu çalışır hâlde bırakıldı, `lsof -iTCP:5199` 3 satır).

| # | Adım | Beklenen | Gerçek | SQL kanıtı |
|---|---|---|---|---|
| 1 | 100.000 GELEN dekont (cari A) | 200 | **200** | `banka_hareketleri` 1 satır, `hareket_id IS NULL` = 1 |
| 2 | Aynı cariye 2. 100.000, kimliksiz | 400, mesajda kümülatif 200.000,00 | **400** — "…(Bu cariyle bugün kümülatif **200.000,00** TL — eşik 185.000 TL; bu işlem 100.000,00 TL)" | satır sayısı **1**'de kaldı, bakiye **100.000,00** |
| 3 | Aynı dekont kimlik+beyan bloğuyla | 200, tespit `kaynak_tur='BANKA_HAREKET'` | **200** | `masak_kimlik_tespitleri`: `BANKA_HAREKET \| 01M2RXVHDG… \| 100000.00` (kümülatif değil, işlemin kendi tutarı); `masak_beyanlari`: `BANKA_HAREKET \| AMAC_BEYANI`; dekont 2 satır |
| 4 | ÇAPRAZ KÜMÜLATİF: 100.000 veresiye (defter) + 100.000 dekont, kimliksiz (cari B) | 400 | **400** — kümülatif notu 200.000,00 | — |
| 5 | 184.999,99 | 200 | **200** | — |
| 6 | 185.000,00 | 400 | **400** | — |
| 7 | 2.000.000,01 kimlikli, `fonKaynagi` yok | 400 | **400** ("Nakit işlem beyan…") | — |
| 8 | GIDEN 185.000 kimliksiz | 400 | **400** | — |
| 9 | Cari YOK + 185.000 | 400 | **400** | — |
| 10 | Cari YOK + 100.000 | 200 | **200** | — |
| 11 | İleri tarihli (2026-09-25) 1.000 | 400 | **400** — "İleri tarihli dekont kabul edilmez" | — |
| 12 | 185.000'lik GELEN dekontu faturaya dönüştür | 200 (MASAK istemez) | **200**, `FTR-…` üretildi | — |

**Temizlik:** API PID ile durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
`information_schema`'dan `tenant_id` kolonu olan **50 tablodan** test kiracısının satırları
silindi; `tenants` sayısı **8 → 9 → 8** (başlangıç değerine döndü). Test için üretilen tek
kullanımlık davet satırı da silindi (`kiraci_davetleri` = 0). **`QA-TEST-` önekli kiracılara
dokunulmadı.**

---

## 9. DoD sözleşmesi (Bertan mührü)

| Varlık / kayıt | Düzenleme yolu | Gerekçe |
|---|---|---|
| `masak_kimlik_tespitleri` satırı (bu paketin ürettiği) | **SALT OKUNUR** — PATCH/DELETE ucu YOK | Mevzuat gereği 8 yıl saklanan append-only yükümlülük kaydı. Ekranda `DegismezIpucu` ile gerekçe + doğru yol yazılıdır: dekont modalında `#degismez-ipucu-MasakKimlikTespiti`, `/uyum` tespit detayında mevcut ipucu. Hatalı tespit için **yeni tespit** girilir. |
| `masak_beyanlari` satırı | **SALT OKUNUR** | Aynı gerekçe; tespitle aynı commit'te doğar. |
| `banka_hareketleri` satırı (dekont) | Bu pakette **değişmedi**: storno/düzenleme yok, `DegismezIpucu` mevcut (`#degismez-ipucu-BankaHareketi`) | Düzenlenebilirliği **S-3/48-A kuyruğuna devredilmiştir** (kapsam dışı, kurucu kalemi olarak açık). Finansal satırda hard DELETE YAZILMADI. |
| `banka_hareketleri.hareket_id` | Yazılmaz (daima null) | Doldurulması paket 32 kararıdır; XML yorumunda gerekçesi yazılıdır. |

**Yeni varlık türü açılmadı**, dolayısıyla yeni PATCH/DELETE ucu da açılmadı. Mevcut uç
(`POST /bankalar/{id}/hareket`) rolleri değişmedi: `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`,
modül yetkisi `banka` (TEZGAHTAR 403 testle sabitlendi). Denetim izi `SaveChanges` hattından
otomatik yazılır (`DenetimYazmaDinleyicisi`), yeniden yazılmadı.

**DoD EK (her düğme ya istek atar ya kilitli + sebep):** `#dekont-kaydet` eksik MASAK alanında
KİLİTLİ ve sebebi `#dekont-kaydet-sebep` içinde yazılıdır (UX-S30). **Tek düzenleme kalıbı:**
yeni "duzenlenenId" kalıbı açılmadı, `Pages/` altında yeni ModalPencere düzenleme kullanımı yok
(denetim.sh sayfa tipi tavanı 129/129 — **artmadı**).

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** `Kuyumcu.Web/Pages/Banka.razor`,
`Kuyumcu.Web/Pages/Uyum.razor` (yalnız tespit detayına "Kaynak" satırı),
`Kuyumcu.Web/Bilesenler/MasakFormu.razor` (geriye uyumlu `Degisti` parametresi).

---

## 10. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| K41-1 (kümülatif sunucu saatiyle, banka `Olusturuldu`, ileri tarih 400) | **KARŞILANDI** | § 2 · M41-5, M41-6 · canlı 11 |
| İndeks migration ile birlikte, `(tenant_id, cari_id, olusturuldu)` | **KARŞILANDI** | § 4 canlı `SHOW INDEX` |
| Üç kolon tek migration'da, `kaynak_tur` DEFAULT 'HAREKET' | **KARŞILANDI** | § 4 `SHOW COLUMNS` |
| `GunlukIslemToplam` iki ayrık sorgu, CASE/UNION/COALESCE yok | **KARŞILANDI** | MasakKapisi.cs · M41-3 |
| `hareket_id IS NULL` süzgeci ilk günden | **KARŞILANDI** | M41-4 |
| `Kaydet` imzasında ZORUNLU `kaynakTur`, 12 çağrı noktası elden geçti | **KARŞILANDI (sapma S1: 12, 10 değil)** | § 3 · M41-8 |
| İki fazlı kapı sırası (Dogrula tx öncesi, Kaydet tx içinde SaveChanges öncesi) | **KARŞILANDI** | M41-10 · canlı 2 (satır yazılmadı) |
| `HareketId = null` açıkça yazıldı | **KARŞILANDI** | BankaModulu.cs · test `Esik_ustu_dekont_KIMLIKLI...` |
| "Dogrula tx içine alınırsa" mutasyonu listeden çıkarıldı, yerine M41-10 | **KARŞILANDI** | § 3 S5 |
| `HareketId` tüketen tüm okuma yolları tarandı ve dallandırıldı | **KARŞILANDI** | § 7 tablosu |
| Mevcut `MasakFormu.razor` kullanıldı, ikiz form yok, POS taşınmadı | **KARŞILANDI** | Banka.razor · Islemler.razor'a dokunulmadı |
| Yeni istemci tipi (sayfa record'u) açılmadı | **KARŞILANDI** | denetim.sh 129/129 |
| Gövde anonim nesne, alan adları `MasakBilgiDto` ile birebir | **KARŞILANDI** | bUnit `..._masak_blogu_dogru_alan_adlariyla_gider` |
| UX-1: MASAK tespiti için DegismezIpucu | **KARŞILANDI** | bUnit `MASAK_blogunda_DEGISMEZ_IPUCU...` |
| Kapsam dondurulmuş (tevsik/gün kilidi/vade/FaturayaDonustur/risk/PATCH ucu açılmadı) + gerekçeler kodda | **KARŞILANDI** | BankaModulu.cs yorumları · canlı 12 · test `Faturaya_donusturme_MASAK_ISTEMEZ...` |
| Kiracı izolasyonu: elle TenantId + yabancı cari 404 | **KARŞILANDI** | test `Yabanci_kiracinin_carisiyle_dekont_404` |
| ExecuteUpdate/Delete beyaz listesi büyütülmedi, hard DELETE yok, ikinci yazma motoru yok | **KARŞILANDI** | denetim.sh TEMİZ (beyaz liste 5, değişmedi) |
| Sozlukler TR+EN, çıplak metin yok | **KARŞILANDI** | `SozlukBanka` 7 yeni anahtar + `SozlukUyum` 4 anahtar, TR ve EN |
| `dotnet build` 0 uyarı/hata · denetim.sh TEMİZ · tam süit 0 başarısız | **KARŞILANDI** | § 5 |
| Test kiracısı silindi, `QA-TEST-*` korundu, tenants sayısı geri döndü | **KARŞILANDI** | § 8 temizlik |

---

## 11. Değişen dosyalar

```
dotnet/Kuyumcu.Api/Data/Entities.cs                                   (BankaHareket.HareketId + gerekçe)
dotnet/Kuyumcu.Api/Data/MasakVarliklari.cs                            (MasakKaynakTuru + 2×kaynak_tur + XML)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs                           (yeni indeks + gerekçe)
dotnet/Kuyumcu.Api/Data/Migrations/20260917235727_MasakBankaKaynagi.cs          (YENİ)
dotnet/Kuyumcu.Api/Data/Migrations/20260917235727_MasakBankaKaynagi.Designer.cs (YENİ)
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs                       (iki ayrık sorgu + zorunlu kaynakTur)
dotnet/Kuyumcu.Api/Modules/Masak/MasakModulu.cs                       (3 yansıtmaya kaynakTur)
dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs                       (DTO + kapı + ileri tarih + gerekçeler)
dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs                       (8 çağrı noktası)
dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs                         (4 çağrı noktası)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                                 (TR+EN)
dotnet/Kuyumcu.Web/Pages/Banka.razor                                  (cari seçici + MASAK bloğu + kilit sebebi)
dotnet/Kuyumcu.Web/Pages/Uyum.razor                                   (tespit detayında Kaynak satırı)
dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor                        (opsiyonel Degisti geri çağrısı)
dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs                           (2 sözleşme kaydına KaynakTur)
dotnet/Kuyumcu.Tests/MasakBankaTestleri.cs                            (YENİ, 15 test)
dotnet/Kuyumcu.Tests/BankaEkranTestleri.cs                            (YENİ, 6 test)
dotnet/Kuyumcu.Tests/MasakEntegrasyonTestleri.cs                      (+1 test, +2 assert)
dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs                            (UyumServisi + /masak/esikler sahte yanıtı)
dotnet/Kuyumcu.Tests/UX4cEkranTestleri.cs                             (/masak/esikler sahte yanıtı)
```

`dotnet/denetim.sh` **değiştirilmedi**. `dokumanlar/02-mimari/…` belgelerine dokunulmadı.
`SONRAKI-OTURUM-PROMPT.md`'ye dokunulmadı. **Commit yapılmadı.**

---

## 12. Açık kalanlar / devredilenler

1. **Dekontun düzenlenebilirliği kapanmadı** (QA tur 3 `/banka`: "dekont düzenlenemiyor",
   "Bekliyor durumu elle mutabık işaretlenemiyor"). Kapsam dışıydı → **S-3/48-A kuyruğu**.
   Kurucunun "düzenlenemeyen kalem = 0" kapısı bu yönüyle AÇIK kalmaya devam ediyor.
2. **`banka_hareketleri.hareket_id` bugün her zaman null.** Doldurulması paket 32 (banka ekstresi
   içe aktarma / havale bacağının otomatik dekontu) kararıdır. Kolon "ölü" görünür; XML yorumunda
   neden var olduğu yazılıdır — **silinmemelidir**, süzgeç onunla çalışır.
3. **İstemci eşik yükleme hatası:** `/masak/esikler` alınamazsa ekran MASAK formunu açmaz ve
   hata bandı gösterir; kapıyı yine **sunucu** uygular (fail-closed orada durur, 400 döner).
   İstemci tarafında ayrı bir "eşik alınamadı → Kaydet kilitli" kademesi açılmadı (kapsam).
4. **Kümülatifin bilinen gevşemeleri** değişmedi (TAKAS fark ölçümü, EVRAK_GIRIS'in dışarıda
   olması, HURDA_ALIS stopaj sonrası net) — MasakKapisi XML'inde yazılı, bu paket dokunmadı.
5. **Gün kilidi ve vade kapısı** banka hareketine bağlanmadı (ayrı kararlar).
6. **Operasyon notu (Madde 1 gereği açıkça):** canlı doğrulama sırasında MySQL parolasını
   değişkene alırken ilk denemede yanlış satır yakalandı (`grep MYSQL_ROOT_PASSWORD | head -1`
   dosyanın 3. satırındaki **yorum** satırına düşüyor); düzeltme sırasında `docker-compose.yml`
   satırı maskeleme denemesi başarısız oldu ve parola değeri bir kez ajan çıktısına düştü.
   Değer, depoda zaten düz metin duran **yerel geliştirme** parolasıdır ve üretimde
   kullanılmamaktadır (üretim: `docker-compose.uretim.yml` + `.env`). Yine de kurucu isterse
   yerel parolayı döndürebilir. Doğru komut: `grep -E "^[[:space:]]*MYSQL_ROOT_PASSWORD:" …`.

---

## 13. Kurucuya / mali müşavire notlar

1. **Bankacılık kanalında kuyumcunun kimlik tespiti yükümlülüğü devam ediyor mu?**
   Spesifikasyonda **DOGRULANMALI** işaretli. Ürün **sıkı tarafı** seçti: havale/EFT de
   185.000 TL eşiğine tabidir ve kimlik tespiti istenir. Yanlışsa sonuç "gereksiz kimlik
   tespiti"dir (zararsız); tersi seçilseydi sonuç "eksik tespit" olurdu (cezalı).
   **Mali müşavir teyidi isteniyor.** Teyit "yükümlülük yok" derse, kapı yapılandırmadan
   (yeni bir `MasakEsikleri` anahtarı) gevşetilebilir — kod değişikliği gerektirmez tasarım
   bugünden hazır değildir, ayrı küçük bir kalem olur.
2. **İleri tarihli dekont artık reddediliyor** (5 dk tolerans). Kuyumcu "yarın valörlü" bir
   dekontu bugünden girmek isterse bu akış kapalıdır; ihtiyaç doğarsa valör alanı ayrı bir
   kalem olarak açılmalıdır.
3. **Eşik rakamları koda gömülü değildir** (`MasakEsikleri`); MASAK rehberi güncellenince
   yapılandırmadan değişir.
