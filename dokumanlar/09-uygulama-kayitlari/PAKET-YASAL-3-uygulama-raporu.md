# PAKET YASAL-3 — MASAK: kümülatifte brüt ölçü · Peşin art arda sorusu · saklama otomatiği · yaptırım taraması: uygulama raporu

> Tarih: 2026-10-01 · Rol: Taşkın (backend) + Ertuğ (frontend) · Tur 1 + Tur 2 (§ 0) + **Tur 3 (§ 00)** · Test DB `kuyumcu_test_t2` · Canlı API 5162 / vekil 5163 · CDP 9901–9920 · COMMIT YOK.
> Şartname: `dokumanlar/02-mimari/kalan-paketler/PAKET-YASAL-3-MASAK-KUMULATIF-SAKLAMA-2026-09-30.md` (şartnamede "Birleşik koşullar" başlığı yok; bağlayıcı olarak § 4 Kapsam, § 6 Kabul, § 7 Mutasyon, § 10 Şerit dosya alanı uygulandı).

## 00. TUR 3 — orkestratör bulgusu (01.10 03:25): açık bakiye ölçüsünde sabit para birimi listesi

> Bu bölüm § 0 (tur 2) ve § 1–14'ün (tur 1) üstünde geçerlidir. Onlar değiştirilmedi.

### 00.1 Sonuç
- **Durum: BİTTİ.** Paralel şerit kapısının 7 maddesi sağlandı.
- Build: `dotnet build Kuyumcu.sln` sonuçları:
  - İlk build: **0 hata / 2 uyarı**. İki uyarı da aynı CS8601'dir ve başka şeridin dosyasında: `Kuyumcu.Tests/Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49`.
  - Son mutasyondan sonraki temiz build: **0 uyarı / 0 hata**.
- Filtreli koşum: `Yasal3AcikBakiye*`, `YaptirimDbTestleri`, `YaptirimTur2DbTestleri` → **8/8 yeşil**.
- Mutasyon: 4 kural mutasyonu yapıldı, dördü de kırmızı verdi. 1 bağımsızlık kanıtı koşuldu ve beklendiği gibi yeşil kaldı. Hepsi geri alındı, `cmp` AYNI (§ 00.4).
- Tam süit (`/private/tmp/kuyumcu-serit/trx-pYASAL-3-t3/_Mac_2026-10-01_03_21_42.trx`): **8392 toplam · 8390 geçti · 1 başarısız · 1 atlandı**.
  - Tek kırmızı: `KarZararEkranTestleri.P35_08_Her_dugme_gercek_istek_atar`. Tur 2 devir listesinde de vardı (§ 0.7, ay sınırı). Sahibi KarZarar/P35 şeridi. Tek başına yeniden koşulduğunda yine kırmızı (deterministik). Bu paketin dosyalarına dokunmuyor.
  - **Yeni kırmızı farkı: 0.**
- `denetim.sh`: **302 ✓ / 1 ✗**. Kalan ✗ `P-30 DEMO_INSAN_SURESI` (orkestratörün, tur 2 ile aynı). YASAL-3 sayaçları ✓.
- Canlı API 5162 + SQL: § 00.5. Test kiracısı silindi. `tenants` sayısı 14.
- Ekran değişmedi: razor, css ve sözlük dosyalarına dokunulmadı. Bu yüzden tarayıcı ölçümü gerekmedi. Durum etiketleri (ACIK, VARLIK_YOK) tur 2'de ölçülen ekranda gösteriliyor (§ 0.6).

### 00.2 Bulgu → yapılan
**Bulgu:** `YaptirimServisi.ListeGuncelle` açık varlığı sabit bir listeyle ölçüyordu: `TlBakiye, HasGramBakiye, UsdBakiye, EurBakiye`. `GbpBakiye` bu listede yoktu. Bu yüzden yalnız sterlin borcu olan kilitli cari için görev açılmıyor ve eşleşme `VARLIK_YOK` oluyordu. Bu sessiz bir MASAK bildirim kaçağıdır. Emanet ise madenler toplanarak tek sayıyla ölçülüyordu.

**Yapılan:** Ölçü tek yere taşındı: `YaptirimServisi.CariVarlikVar(db, tenantId, cariId)` (`Modules/Masak/YaptirimServisi.cs`).
- **`BakiyeKolonlari`:** `CariBakiye` üzerindeki, adı `…Bakiye` ile biten her `decimal` özellik. Bugün bunlar TL, has gram, USD, EUR ve GBP. Elle tutulan bir para birimi listesi yok. Bakiye satırına yeni bir birim kolonu eklenirse ölçü onu kendiliğinden kapsar. `AcikBakiyeVar(CariBakiye?)` bu kümeden okur.
- **Emanet:** artık **maden başına** ayrı ayrı netleşir (`GroupBy(Maden)`). Altın ve gümüş emaneti toplanıp birbirini sıfırlayamaz. Gümüş emaneti cari bakiye satırına girmediği için (`EmanetKurali.CariHasKolonunaGirer` yalnız ALTIN) tek izi hareket defteridir. Ölçü onu oradan okur.
- **Bekleyen sipariş:** önceki gibi ölçüye dahil.
- **Gümüş bakiyesi:** `cari_bakiyeler` tablosunda ayrı bir gümüş kolonu YOK. Gümüş varlığı yalnız emanet olarak doğuyor ve maden başına emanet ölçüsüyle kapsanıyor. İleride bir `GumusBakiye` kolonu eklenirse adı `…Bakiye` ile biteceği için `BakiyeKolonlari` onu kendiliğinden alır.

**Aynı kalıbın taranması:** `grep -rn "UsdBakiye\|EurBakiye\|GbpBakiye\|TlBakiye" Kuyumcu.Api`
- Paketin kendi dosyalarında (Masak/*, SaklamaSuresiIsi, SaklamaUygulaIsi, KuyumcuDbContext yaptırım kapısı) başka sabit para birimi listesi yok.
- Saklama otomatiğinin "son 8 yılda hareket var mı" kontrolü `Hareketler` tablosuna para biriminden bağımsız bakıyor.
- Yaptırım kilidi kapısı kayıt **türü** listesine bakıyor, para birimine bakmıyor.
- Paket dışında aynı kalıp şu yerlerde var: `Modules/Cari/CariModulu.cs:896-898` (pasife alma ön koşulu, USD/EUR/GBP elle) ve `Modules/Rapor/RaporModulu.cs:721-725, 783`. Bugün üç dövizi de içeriyorlar, yani bugün kaçak yok; ancak yeni bir birim eklendiğinde kaçabilirler. Bu dosyalar başka şeritlerin alanında (Cari, Rapor). Dosya alanı kuralı gereği **değiştirilmedi**, § 00.6'ya yazıldı.

### 00.3 Testler
Yeni dosya `Kuyumcu.Tests/Yasal3Tur3Testleri.cs`, **4 test**:
- `Yasal3AcikBakiyeOlcusuTestleri` (saf):
  - `Her_doviz_birimi_tek_basina_acik_bakiye_sayilir`: her `DovizBakiye.ParaBirimleri` birimi `DovizBakiye.Ekle` ile tek başına yazılır ve açık sayılmalıdır.
  - `TL_ve_has_tek_basina_acik_sifir_satir_acik_degil`
  - `Bakiye_kolonlari_varligin_tum_bakiye_alanlarini_kapsar`
- `Yasal3AcikBakiyeDbTestleri.Yalniz_GBP_has_ya_da_gumus_emanetli_cariye_gorev_acilir_sifirda_acilmaz` (DB). Dört cari kurulur ve beklenenler şunlar:

| Cari | Varlık | Beklenen |
|---|---|---|
| G | yalnız GBP 250 | görev AÇILIR |
| H | yalnız has 3,5 g | görev AÇILIR |
| S | yalnız GÜMÜŞ emanet 50 g (bakiye satırı sıfır, ön koşul olarak da denetlenir) | görev AÇILIR |
| Z | her şey sıfır | görev AÇILMAZ, durum `VARLIK_YOK` |

  Toplu beklenen: `kilitlenenCari 4`, `acilanGorev 3`, son gün RG+30, 3 KRİTİK bildirim.

### 00.4 Mutasyon tablosu (tur 3)
Yedek `/private/tmp/kuyumcu-serit/mut-yedek/YASAL-3-t3/`. Betik `/private/tmp/kuyumcu-serit/YASAL-3/t3/mut.sh`. Her mutasyonda build + filtreli koşum yapıldı, ardından yedekten geri kopyalandı ve `cmp` AYNI çıktı.

| Kod | Değişiklik | Kırmızı test(ler) |
|---|---|---|
| MY3-25 | **GBP ölçüden çıkarıldı**: `BakiyeKolonlari` süzgecine `&& p.Name != "GbpBakiye"` eklendi | `Yalniz_GBP_has_ya_da_gumus_emanetli_cariye_gorev_acilir_sifirda_acilmaz`, `Bakiye_kolonlari_varligin_tum_bakiye_alanlarini_kapsar`, `Her_doviz_birimi_tek_basina_acik_bakiye_sayilir` (3 kırmızı) |
| MY3-26 | Has ölçüden çıkarıldı (`!= "HasGramBakiye"`) | aynı DB testi, `Bakiye_kolonlari_…`, `TL_ve_has_tek_basina_…` (3 kırmızı) |
| MY3-27 | Maden başına emanet denetimi silindi | `Yalniz_GBP_has_ya_da_gumus_emanetli_…` (1 kırmızı) |
| MY3-28 | Eski sabit liste geri yazıldı (`Tl/Has/Usd/Eur`) | `Yalniz_GBP_has_ya_da_gumus_emanetli_…`, `Her_doviz_birimi_tek_basina_…` (2 kırmızı) |
| MY3-29 (bağımsızlık kanıtı) | Küresel `DovizBakiye.ParaBirimleri`'nden GBP çıkarıldı | **Beklenen yeşil, gerçek 8/8 yeşil.** Ölçü artık bu listeye bağlı değil: GBP bakiyesi liste dışında kalsa bile kolon üzerinden yakalanıyor. Bu bir test açığı değil, bulgunun "yeni birim eklense de kaçmaz" şartının kanıtı. Orkestratörün tarif ettiği "GBP'yi listeden çıkar" mutasyonunun yeni koddaki karşılığı MY3-25'tir, ve o kırmızıdır. |

### 00.5 Canlı doğrulama (API 5162 · kiracı `PAKETYASAL3-TEST-T3 Altın Sarayı`)
Betik `/private/tmp/kuyumcu-serit/YASAL-3/t3/canli/canli.mjs`, çıktı `canli.log`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| G carisine 100 GBP döviz veresiye | 200 | 200 |
| S carisine G925 20 g emanet girişi | 200 | 200 `hasGrami 18.5, maden GUMUS` |
| Yaptırım listesi yükleme (G, S, Z) | 200, kilit 3, görev 2 | 200 `{"eklenen":3,"kilitlenenCari":3,"acilanGorev":2}` |
| `GET /masak/yaptirim` | G ACIK, S ACIK, Z VARLIK_YOK | G `ACIK` 2026-10-31 · S `ACIK` 2026-10-31 · Z `VARLIK_YOK` null |

SQL (`kuyumcu_net`):
- `cari_bakiyeler`:
  - G: `tl 0 · has 0 · usd 0 · eur 0 · gbp 100.00`. Eski kod bu cariyi `VARLIK_YOK` sayardı.
  - S ve Z: tüm kolonlar 0.
- `hareketler`:
  - G: `DOVIZ_VERESIYE GBP 100.00`
  - S: `EMANET_GIRIS GUMUS 18.500000`
- `yaptirim_eslesmeleri`:
  - G: `TAM/LISTE_GUNCELLEME/ACIK/2026-10-31`
  - S: `TAM/LISTE_GUNCELLEME/ACIK/2026-10-31`
  - Z: `TAM/LISTE_GUNCELLEME/VARLIK_YOK/NULL`
- `bildirimler` YAPTIRIM: 2.

Temizlik:
- API `kill` ile durduruldu (dotnet run ve alt süreç). 5162 LISTEN 0.
- `zsh kiraci-temizle.sh 01M3TDF4S3Y7H7PNNMR6Y9MDZ9 --uygula` çalıştırıldı: "SİLİNDİ … kalan kiracı sayısı: 14". Kiracının artık olmadığı doğrulandı (`COUNT 0`).
- Kiracı-üstü satırlar silindi:
  - `yaptirim_kayitlari` 3 satır (tablo yeniden 0)
  - `kiraci_davetleri` 1
  - SİSTEM denetim satırı 2
- `tenants` sayısı 14 (tur 2 başlangıcıyla aynı). Not: Bu turda kiracı açılmadan önce ayrı bir sayım alınmadı. Kiracı varken alınan sayım da 14 çıktı, çünkü aynı anda başka bir şerit kendi kiracısını açtı ya da sildi. Son durumda bu paketin kiracısı yok; QA-TEST-* kiracılarına dokunulmadı.
- `fikstur.json` ve `sis.anahtar` `printf ''` ile boşaltıldı. Sistem anahtarı süreç içinde rastgele üretildi ve çıktıya basılmadı.

### 00.6 Açık kalanlar (tur 3)
1. **Paket dışı, aynı kalıp:**
   - `Modules/Cari/CariModulu.cs:896-898` (pasife alma ön koşulu): döviz bakiyesini USD/EUR/GBP diye elle sayıyor.
   - `Modules/Rapor/RaporModulu.cs:721-725` ve `:783` (`dovizBakiyesiVar`): aynı şekilde elle sayıyor.

   Bugün kaçak yok, çünkü üç dövizin üçü de listede. Yeni bir birim eklendiğinde kaçabilirler. Sahipleri Cari ve Rapor şeritleri. Öneri: `YaptirimServisi.AcikBakiyeVar` ya da onun `Modules/Cari/DovizBakiye` içine taşınmış bir karşılığı kullanılsın.
2. `KarZararEkranTestleri.P35_08` (ay sınırı, sahibi P35 şeridi): tur 2'den devreden kırmızı.
3. Tur 2 § 0.10'daki maddeler aynen açık.

### 00.7 Değişen dosyalar (tur 3)
- `dotnet/Kuyumcu.Api/Modules/Masak/YaptirimServisi.cs`: `BakiyeKolonlari`, `AcikBakiyeVar`, `CariVarlikVar`. Liste güncellemesi bunları çağırıyor.
- `dotnet/Kuyumcu.Tests/Yasal3Tur3Testleri.cs` (YENİ)
- `Modules/Cari/DovizBakiye.cs` yalnız MY3-29 sırasında geçici olarak değişti, sonra geri alındı (`cmp` AYNI).

### 00.8 DoD sözleşmesi (tur 3 eki)
Yeni varlık, uç ya da kolon yok, migration gerekmedi. Değişen tek şey yaptırım eşleşmesinin `ACIK` / `VARLIK_YOK` ayrımını yapan ölçü. Düzenleme yolları tur 2'deki gibi: PATCH `/masak/yaptirim-eslesmeleri/{id}`.

## 0. TUR 2 — hasım doğrulama bulgularının kapatılması (2026-10-01 02:00–03:00)

> Bu bölüm tur 2'dir ve tur 1 bölümlerinin (§ 1–14) üstünde geçerlidir. Tur 1 metni aşağıda değiştirilmeden duruyor.

### 0.1 Sonuç
- **Durum: BİTTİ** (paralel şerit kapısının 7 maddesi sağlandı; tam süitte kalan 6 kırmızının hiçbiri bu paketin değil, § 0.7).
- Build: `dotnet build Kuyumcu.sln` → **0 uyarı / 0 hata** (build3, son mutasyondan sonra temiz).
- Filtreli koşum (YASAL-3'ün tüm sınıfları + MASAK/Peşin/Tevsik komşuları): **103/103 yeşil**.
- Mutasyon: tur 2'de 9 mutasyon yapıldı. Hepsi kırmızı verdi, hepsi geri alındı ve `cmp` birebir AYNI çıktı (§ 0.4).
- `denetim.sh`: 302 ✓, 1 ✗. Kalan ✗ `P-30 DEMO_INSAN_SURESI` (tarih kapısı 30.09 geçti; orkestratörün). YASAL-3 sayaçları ✓.
- Canlı API 5162 + SQL: 15 adım (§ 0.5). Gerçek başsız Chrome: 24 ölçüm, hepsinde 0 (§ 0.6).
- Test kiracısı silindi. `tenants` sayısı 14 (başlangıç) → 14 (son). Kiracı-üstü test satırları da silindi.

### 0.2 Bulgu → yapılan
| # | Bulgu | Yapılan | Kanıt |
|---|---|---|---|
| 1 KRİTİK | Kilitli cariden avans, emanet vb. alınabiliyor | **Tek merkezi kapı `KuyumcuDbContext.YaptirimKilidiniKoru(Async)`**, SaveChanges'ta çalışır (sistem carisi korumasıyla aynı desen). Cari taşıyan her YENİ para/mal kaydına bakar: `Hareket, BankaHareket, PosTahsilat, Siparis, Rezervasyon, VezneFisi, Irsaliye, Fatura`. Kilitli cari (TAM eşleşme + karar yürürlükte) → 400 `YAPTIRIM_ENGEL`, sözlük cümlesi. Kapı dışında kalanlar: MASAK kimlik tespiti, şüpheli işlem bildirimi, tevsik ihlali ve eşleşme kaydı (bildirim yükümlülüğü kilitten bağımsız sürer). | 9 HTTP test (`YaptirimKilidiYollariTestleri`) · MY3-17/17b/24 · canlıda 14 yol 400 + SQL 0 |
| 2 ORTA | Varlıksız eşleşme ekranda "MASAK'a bildirildi" | Yeni durum `VARLIK_YOK` ("Kilitli — bildirilecek varlık yok"). Kullanıcı PATCH ile bu duruma geçemez (`Hepsi`'de yok). `BILDIRILDI` artık **her** eşleşmede tarih + evrak no ister: liste güncellemesi istisnası kaldırıldı. Görevin sonradan yeniden açılması gerekmez: bakiye, emanet ya da sipariş doğuracak her yazım bulgu 1'in kapısında durur. | `Varliksiz_eslesme_bildirildi_sayilmaz_kanitsiz_kapatilamaz`, K11b · MY3-18/19/22 · canlı SQL `VARLIK_YOK` · görüntü |
| 3 ORTA | Risk ekranında eşik bölme satırı ve T-1 notu yok | (a) `GET /masak/yaptirim` artık `esikBolmeSinyali` döner: `{gun 30, adet, sinir 20, son[tarih, kullanıcı]}`. Kaynak `denetim_kayitlari` `PESIN_ART_ARDA_FARKLI_KISI`, kişisel veri yok. Uyum → Risk ekranında (YasalListePaneli) sayı cümlesi ve ne yapılacağı yazılır. Son kayıtlar kapalı Katlanir'da, sınır başlıkta yazılı. (b) T-1 notu Yasal sınırlar ekranındaki MASAK kimlik eşiği kartına tek satır (`#ys-yorum-…`) olarak eklendi. YASAL-1 şeridi kapanmış olduğu için bekletilmedi; "Dokunulan sayfalar" listesine yazıldı. | `Esik_bolme_sinyali_ozette_sayilir`, 2 bUnit, `Yasal3_T1_yorum_notu…` · MY3-20/23 · canlı `adet 1` · görüntü |
| 4 DÜŞÜK | Saklama aday seçiminde açlık | Atlama kuralları (bekletme, aynı hash ile canlı tespit, carinin son 8 yıldaki hareketi) **sorguya** taşındı. `Take` artık yalnız gerçekten maskelenecek kayıtları sayar. Beyan seçimi de sorguda. Sayaçlar ayrı `CountAsync` ile tutulur. Test için `AdaylariBul(…, parti)` aşırı yüklemesi eklendi. | `Parti_sinirini_atlanan_adaylar_doldurmaz` (parti=1) · MY3-21 |
| 5 DÜŞÜK | 5199 dinlemiyor | Bu pakete ait değil; 5199'a dokunulmadı. Orkestratöre bırakıldı. | — |

**Sapma (bulgu 1):** Bulgunun önerisi "`IslemDogrulayici.CariAktifDogrula` yanına tek merkezi kapı" idi. Bunun yerine SaveChanges kapısı seçildi. Gerekçe: `CariAktifDogrula` yalnız 5 çağrı noktasında çalışıyor. Emanet, vezne, evrak, banka ve sipariş yolları onu çağırmıyor, ve yeni bir yol eklendiğinde yeniden unutulabilir. SaveChanges kapısı hangi modül, uç ya da betik yazarsa yazsın çalışır. `DogrulaKumulatif` içindeki erken denetim yerinde kaldı (katmanlı savunma). Bu yüzden veresiye, tahsilat ve banka yolları iki katmanla korunuyor (MY3-17b).
**Sınır:** `ExecuteUpdate/Delete` SaveChanges'ı atlar. Beyaz liste 4 değişmedi.

### 0.3 Testler
Yeni **15 test vakası**:
- `Yasal3Tur2Testleri.cs` (YENİ):
  - `YaptirimKilidiYollariTestleri`: 9 HTTP test. Kapsanan yollar: avans NAKIT ve HAVALE, avans iadesi, emanet giriş ve çıkış, değişim, rezerv ve konsinye, sipariş ve vezne fişi, çek, veresiye, tahsilat ve banka hareketi. Her testte beklenen: 400, `kod YAPTIRIM_ENGEL`, kayıt sayısı değişmedi.
  - `YaptirimTur2DbTestleri`: 2 test.
- `SaklamaOtomatikTestleri.Parti_sinirini_atlanan_adaylar_doldurmaz`: 1 test.
- `Yasal3EkranTestleri`: 2 bUnit test (VARLIK_YOK, sinyal var ve yok).
- `YasalSinirlarEkranTestleri.Yasal3_T1_yorum_notu_yalniz_MASAK_kimlik_esigi_kartinda`: 1 test.

Güncellenen: `K11b` (varlıksız eşleşme artık `VarlikYok`).

Tam süit (`/private/tmp/kuyumcu-serit/trx-pYASAL-3-t2/_Mac_2026-10-01_02_39_04.trx`): **8346 toplam · 8339 geçti · 6 başarısız · 1 atlandı** (§ 0.7).

### 0.4 Mutasyon tablosu (tur 2)
Hepsi `/private/tmp/kuyumcu-serit/mut-yedek/YASAL-3-t2/` yedeğinden geri alındı ve `cmp` ile AYNI çıktı.

| Kod | Değişiklik | Kırmızı test(ler) |
|---|---|---|
| MY3-17 | SaveChangesAsync'ten kapı çağrısı silindi | YaptirimKilidiYollariTestleri: avans(NAKIT), avans(HAVALE), avans iadesi, emanet, değişim, rezerv+konsinye, sipariş+vezne, çek/senet (8 kırmızı) |
| MY3-17b | Sync ve async kapıların ikisi de silindi, yalnız veresiye/tahsilat/banka testi koşuldu | Kırmızı YOK. Bu yollar zaten `DogrulaKumulatif` kilidiyle korunuyor (katmanlı savunma, test açığı değil). Aynı yollar MY3-10 (tur 1) kapsamında. |
| MY3-18 | Varlıksız eşleşme yeniden `Bildirildi` yazıldı | K11b, Varliksiz_eslesme_bildirildi_sayilmaz_kanitsiz_kapatilamaz |
| MY3-19 | BILDIRILDI kanıt kuralına liste güncellemesi istisnası geri eklendi | Varliksiz_eslesme_bildirildi_sayilmaz_kanitsiz_kapatilamaz |
| MY3-20 | Sinyal sorgusundan 30 gün koşulu çıkarıldı | Esik_bolme_sinyali_ozette_sayilir |
| MY3-21 | Aday seçimi `uygun` yerine `bekletmeDisi` sorgusundan yapıldı | Parti_sinirini_atlanan_adaylar_doldurmaz, K7_…, Carisiyle_son_8_yilda_… |
| MY3-22 | Ekranda `VARLIK_YOK` etiketi silindi | Tur2_varliksiz_eslesme_bildirildi_yazmaz_… † |
| MY3-23 | T-1 not koşulu bozuldu | Yasal3_T1_yorum_notu_yalniz_MASAK_kimlik_esigi_kartinda |
| MY3-24 | Kapı tür listesinden Siparis, Rezervasyon, VezneFisi çıkarıldı | Kilitli_cariye_rezerv_ve_konsinye_yapilamaz, Kilitli_cariye_siparis_ve_vezne_fisi_acilamaz |

† MY3-22'nin ilk koşumu yeşil çıktı. Sebep: aynı `bin`'i paylaşan başka bir şeridin derlemesi build ile test arasında Web dll'ini yeniledi. Tekrar koşumda (MY3-22b, `Web.dll` 02:35) test kırmızıya döndü ("Sub-string not found", satır 304). İki koşumdan sonra da geri alındı ve `cmp` AYNI çıktı.

Tur 1 mutasyonları MY3-1…16 geçerli. Tek fark: MY3-10'un (`DogrulaKumulatif` kilidi silindi) koruması artık merkezi kapıyla da sağlanıyor.

### 0.5 Canlı doğrulama (API 5162 · kiracı `PAKETYASAL3-TEST-T2 Altın Sarayı` · `01M3TB1NT5T2Q2Q85GQ5XZF2BB`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| E carisine kilit öncesi 1.000 satış + K22 10 g emanet | 200 | 200 · 200 (has 9,16) |
| Liste yükleme (V: varlıksız, E: emanetli) | 200 | 200 `eklenen 2, kilitlenenCari 2, acilanGorev 1` |
| V avans 5.000 HAVALE / 1.000 NAKIT | 400 | 400 `[kod YAPTIRIM_ENGEL]` ×2 |
| V emanet giriş K22 10 g | 400 | 400 YAPTIRIM_ENGEL |
| V rezerv · konsinye çıkış · sipariş · vezne fişi · çek girişi · veresiye · tahsilat | 400 | 400 YAPTIRIM_ENGEL (7/7) |
| E emanet çıkış 5 g · E değişim | 400 | 400 YAPTIRIM_ENGEL ×2 |
| Peşin 20.000 → 15.000 beyansız → "başka biri" | 200 / 400 SORU / 200 | 200 / 400 `PESIN_ART_ARDA_SORU` / 200 |
| `GET /masak/yaptirim` | sinyal 1, V=VARLIK_YOK, E=ACIK | `esikBolmeSinyali {gun 30, adet 1, sinir 20, son[{kullanici "Yasal"}]}` · V `VARLIK_YOK` sonGun null · E `ACIK` 2026-10-31 |
| YASAL-2 devri: aynı cari 20.000 + 15.000 nakit tahsilat | 400, kodsuz tek cümle | 400 `[kod NAKIT_SINIRI]` "Bu kişiyle bugün nakit toplamı 35.000,00 TL oluyor … kalanı kartla ya da havaleyle ödenmeli." |

SQL (`kuyumcu_net`):
- V için yeni kayıt yok: hareket 0, sipariş 0, rezerv 0, vezne 0.
- E'de yalnız kilit öncesi 2 hareket var (SATIS −9,16 has; EMANET_GIRIS 9,16 has).
- `PESIN_ART_ARDA_FARKLI_KISI` denetim satırı: 1.
- Eşleşmeler: V `TAM/LISTE_GUNCELLEME/VARLIK_YOK/NULL/NULL`, E `TAM/LISTE_GUNCELLEME/ACIK/2026-10-31/NULL`.

Temizlik:
- API ve vekil `kill` ile durduruldu; 5162/5163 LISTEN 0.
- `zsh kiraci-temizle.sh … --uygula` çalıştırıldı. Ardından "Kiracı YOK" doğrulandı.
- Kiracı-üstü satırlar silindi: `yaptirim_kayitlari` 2 (tablo yeniden 0), davet 1, SİSTEM denetimi 2.
- `tenants` 14 → 14. QA-TEST-* kiracılarına dokunulmadı.
- `fikstur.json` ve `sis.anahtar` `printf ''` ile boşaltıldı.

### 0.6 Gerçek başsız Chrome (vekil 5163, CDP 9901–9904; betik `/private/tmp/kuyumcu-serit/YASAL-3/t2/canli/ui.mjs`, `olcum.jsonl`, `foto/`)
| Ekran | 1280 B açık | 1280 P açık | 1280 B koyu | 1280 P koyu | 375 B açık | 375 P açık | 375 B koyu | 375 P koyu |
|---|---|---|---|---|---|---|---|---|
| R2 Uyum → Risk (VARLIK_YOK satırı + sinyal) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| R3 sinyal katlanırı açık | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| T1 Yasal sınırlar, T-1 notu | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Hücredeki "0" şu ölçülerin tamamının sıfır olduğunu gösterir: yatay kaydırma, K1 görünen `[hidden]`, K3 taşan, K3k kırık kısa etiket, K5 `TRY`, K9 binen, K12, K13 kırpılan, K10b kesik, modal içi taşma.

Read ile görülen görüntüler: `R2-uyum-risk-PRO-1280-aydinlik`, `R2-uyum-risk-BASIT-375-aydinlik`, `R3-uyum-sinyal-acik-BASIT-375-karanlik`, `T1-yasal-sinirlar-PRO-1280-karanlik`.
- Varlıksız satırda gri "Kilitli — bildirilecek varlık yok" rozeti görünüyor ve kapat düğmesi yok. Emanetli satırda "Bildirim bekliyor · Son gün 31.10.2026" ve "Bildirildi olarak kapat" görünüyor.
- Sinyal cümlesi, açıklaması ve kapalı "Son kayıtlar (en yeni 20) 1" görünüyor. Açıldığında "01.10.2026 02:41 · Yasal" satırı çıkıyor.
- 185.000 ₺ kartında T-1 notu kutu içinde görünüyor.

Paralel koşum notu: 4 kombinasyon aynı kullanıcıyla paralel açılınca oturum düştü (bilinen tuzak). Ölçümler sıralı olarak yeniden alındı.

### 0.7 Tam süit kırmızıları (hiçbiri bu paketin değil)
| Test | Sahip / sebep | Kanıt |
|---|---|---|
| CariBakiyeTur2Testleri.Evrak_tahsili_stornosu_cariye_yazmaz_TL_aynasi_sifir, CARIBAKIYESinirTestleri.Evrak_tahsili_stornosu_cari_TL_yi_ve_aynayi_bozmaz | CARI-BAKIYE şeridi (Evrak) | Tam süitten sonra iki test hem kapı AÇIKKEN hem kapı silinmişken (KANIT-evrak) 2/2 yeşil. Kırmızı, tam süit sırasında şeridin eşzamanlı değişikliğinden geliyor. |
| BelgeKalanEkranTestleri.BK_Tercih_anahtarlari_sozlukte_tam | BELGE-KALAN / Görünüm şeridi (`gorunum-subesi` tercih anahtarı) | Yeniden koşumda yeşil (kararsız / eşzamanlı) |
| KesifP5EkranTestleri.SATIS4a_Vazgec_… | KESIF-P5 | Yeniden koşumda yeşil (bUnit zamanlama) |
| GorunumSubesiTestleri.K3_Aylik…, KarZararEkranTestleri.P35_08… | Ay sınırı: yerel 1 Ekim, UTC 30 Eylül | Tur 1 devir listesinde (§ 13 md.1) de vardı; yeniden koşumda yine kırmızı |

**Fark (bu paketin ürettiği yeni kırmızı): 0.**

### 0.8 DoD sözleşmesi (tur 2 eki)
- `yaptirim_eslesmeleri.durum` alanına `VARLIK_YOK` eklendi. Bu durumu yalnız sistem yazar. Kullanıcı eşleşmeyi PATCH ile `ACIK` ya da `BILDIRILDI` durumuna (tarih + evrak no ile) geçirebilir. Ekranda eşleşme salt okunur rozetle görünür; kilit için DegismezIpucu var.
- Eşik bölme sinyali salt okunurdur (denetim izinden türetilir, denetim kaydı değiştirilemez).

Yeni tablo ya da kolon eklenmedi. Migration gerekmedi (`durum` MaxLength 20; `VARLIK_YOK` 10 karakter).

### 0.9 Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`: merkezi yaptırım kapısı
- `dotnet/Kuyumcu.Api/Data/Yaptirim.cs`: `VarlikYok`
- `dotnet/Kuyumcu.Api/Modules/Masak/YaptirimServisi.cs`: VARLIK_YOK, kanıt kuralı, sinyal
- `dotnet/Kuyumcu.Api/Modules/Masak/SaklamaSuresiIsi.cs`: sorguda atlama
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yasal3.cs`: 8 yeni anahtar, TR + EN
- `dotnet/Kuyumcu.Web/Bilesenler/Uyum/YasalListePaneli.razor`, `UyumYasalModelleri.cs`
- `dotnet/Kuyumcu.Web/Pages/YasalSinirlar.razor`: tek koşullu not satırı (**Dokunulan sayfalar**'a eklendi)
- `dotnet/Kuyumcu.Tests/Yasal3Tur2Testleri.cs` (YENİ), `Yasal3SaklamaYaptirimTestleri.cs`, `Yasal3EkranTestleri.cs`, `YasalSinirlarEkranTestleri.cs`

### 0.10 Açık kalanlar (tur 2)
1. Kapıya takılan denemeler için ayrı `YAPTIRIM_ENGEL` denetim satırı yazılmıyor. Engel SaveChanges içinde atıldığı için aynı iş birimi geri alınıyor. Tezgâhtaki MASAK kimlik noktası tur 1'deki gibi denetim ve bildirim yazıyor.
2. Tur 1 § 13'teki md.2 (ProYanSutun, SATIS), md.3, md.6 ve md.7 aynen açık. md.4 ve md.5 bu turda kapandı.
3. 5199 inceleme sunucusu: orkestratöre.

## 1. Sonuç
Şartnamenin dört kapsam maddesi (§ 4.1 brüt ölçü, § 4.2 Peşin art arda, § 4.3 saklama otomatiği, § 4.4 yaptırım, § 4.5 risk bildirimi) ve YASAL-2 devri (günlük kapı red metni) uygulandı.
- Build: ürün projeleri (Core/Web/Api) 0 hata. Son tam `Kuyumcu.sln` derlemem (build14, 01:33) **0 uyarı / 0 hata**. Şu an solüsyonda 25 hata var ama hepsi `Kuyumcu.Tests` içinde. Sebep: SATIS-4 şeridi testleri yarı güncelledi, `Satis4Yardim` sınıfı henüz yok (bkz. § 13).
- Benim testlerim: 114/114 yeşil. Filtre: yeni 36 test ile etkilenen sınıflar.
- Mutasyon: 16/16 kırmızı, her biri geri alındıktan sonra `cmp` birebir aynı.
- `denetim.sh`: YASAL-3 sayaçları ✓. Kalan 2 ✗ başka şeritlere ait (§ 9).
- Canlı API ve SQL doğrulaması yapıldı. Gerçek başsız Chrome'da 53 ölçüm alındı ve görüntüler Read ile görüldü.
- Test kiracısı silindi.

## 2. Yapılanlar
### 2.1 § 4.1 Kümülatifte brüt ölçü
- `hareketler.masak_olcu_tl DECIMAL(15,2) NULL` eklendi (`Hareket.MasakOlcuTl`). Değerler:
  - TAKAS: `Yuvarla2(masakTutar)`, yani Max(eski, yeni). İkinci bir formül yazılmadı.
  - HURDA_ALIS: brüt `toplamTl`. Fiş altın bacağında `d.BrutTl`.
- `MasakKapisi.GunlukIslemToplam` iki ayrık sorgu yapar (A5 kararı, COALESCE yok): `MasakOlcuTl != null` satırlarda `SUM(masak_olcu_tl)`, `== null` satırlarda `SUM(ABS(tl_tutari))`. "BİLİNEN EKSİK ÖLÇÜM" notundan takas ve hurda maddeleri çıktı; yalnız EVRAK_GIRIS kaldı. Tevsik bu kolonu okumaz.
- Migration geri doldurması yalnız kanıtı olan satırları kapsar (`gider_pusulalari.brut_tutar`, hareket_id + tenant eşleşmesi).

### 2.2 § 4.2 Peşin Müşteri art arda (`Modules/Masak/PesinPencereKapisi.cs`, YENİ)
- Pencere: aynı kullanıcı + aynı şube, son `Masak:PesinPencereDakika` (appsettings, 60; politika, T-4). Storno çifti dışlanır. Ölçü iki ayrık sorguyla okunur.
- Tetik: önceki pencere en az 1 işlem ve (ölçü + bu işlem ≥ MASAK kimlik eşiği (bugün) **ya da** nakit + bu işlem > tevsik haddi).
- `masak.ayniKisiBeyani` alanına göre:
  - yok → 400, gövdede `kod: PESIN_ART_ARDA_SORU` ve `alanlar.ayniKisiBeyani`.
  - `true` → 400 "Müşteriyi seçin…" (alan `cariId`).
  - `false` → işlem geçer. Denetim satırı yazılır: `PESIN_ART_ARDA_FARKLI_KISI` (`DenetimServisi.YazmaKaydet`; kişisel veri yok, tutarlar ve pencere).
- Hata gövdesine isteğe bağlı `kod` eklendi (`AlanIsareti.KodIle`, `ApiException.cs`). İşaretsiz gövdeler değişmedi.
- Ekran: `Bilesenler/Fis/PesinArtArdaSoru.razor` (YENİ) ve `Pages/Islemler.Yasal3.cs` (YENİ partial). "Evet — müşteriyi seç" istek atmaz. "Hayır, başka biri" aynı fişi beyanla yeniden gönderir.

### 2.3 § 4.3 Saklama otomatiği
- `SaklamaSuresiIsi.AdaylariBul` tek ayıklama motorudur. Aday şu durumlarda atlanır:
  - (a) aynı `belge_no_hash` ile süresi dolmamış daha yeni tespit var,
  - (b) carisiyle son SaklamaYili içinde hareket var,
  - (c) aktif bekletme var (TÜMÜ ya da TESPİT).
- Beyan, tespitini izler. Tespiti olmayan beyan kendi süresiyle maskelenir (paket öncesi davranış).
- `Isler/Isler/SaklamaUygulaIsi.cs` (YENİ, `SAKLAMA_UYGULA`, günlük):
  - Önümüzdeki 30 gün için tek bildirim. Tekillik anahtarı ufkun en yeni adayına bağlı (K-9).
  - `Kuru=false` ise `SaklamaSuresiIsi.Uygula(otomatik: true)` çalışır ve denetime "(otomatik)" etiketiyle, kullanıcısız yazar.
  - `Isler:SaklamaUygula:Kuru` varsayılanı **true** (kurucu K-2).
  - `SAKLAMA_ONIZLEME` işi kuru olmayan kipte susar.
- `saklama_bekletmeleri` (YENİ tablo, `SaklamaBekletmeServisi` + `SaklamaYaptirimController`, `[ModulYetkisi("uyum")]`):
  - `GET /masak/saklama-bekletmeleri`
  - `POST /masak/saklama-bekletme`
  - `PATCH /masak/saklama-bekletme/{id}`: gövdede yoksa korur; null → 400 zorunlu.
  - `POST /masak/saklama-bekletme/{id}/kaldir`
  - Hepsi `[RolGerekli("SAHIP")]`. Bitiş en çok 1 yıl, gerekçe en az 20 karakter; iki kural da servis içinde elle denetlenir.
- Ekran: `Bilesenler/Uyum/SaklamaBekletmePaneli.razor` (YENİ). Beklet/Düzenle çekmecesi (Cekmece), kaldırma için OnayKipi, silinmez kayıt için DegismezIpucu, geçmiş gizlemeler kapalı Katlanir'da.
- ADR-0008 dosyasına güncelleme bölümü eklendi.

### 2.4 § 4.4 Yaptırım taraması
- `yaptirim_kayitlari` (KİRACI-ÜSTÜ, beyaz liste) ve `yaptirim_eslesmeleri` (kiracı) eklendi. Kimlik no AES-GCM ile şifreli, arama HMAC ile.
- `YaptirimServisi` (YENİ) tarama noktaları:
  1. MASAK kimlik tespiti (Faz 1, Dogrula geçtikten sonra): TAM eşleşme → 400 `YAPTIRIM_ENGEL`, KRİTİK bildirim, `RedKaydet` ile denetim `YAPTIRIM_ENGEL` (ayrı SaveChanges; red geri alınırken iz kaybolmaz).
  2. Cari oluşturma/güncelleme: kayıt açılır, TAM eşleşmede cari kilitlenir. Kilitli cariyle her `DogrulaKumulatif` 400.
  3. Hurda satıcısı (HurdaAlis ve fiş altın bacağı): TAM → 400.
  4. Liste güncellemesi: yeni satır mevcut cariyle TAM eşleşirse cari kilitlenir. Açık bakiye/emanet/bekleyen sipariş varsa KRİTİK görev açılır, son gün RG + 30. Yoksa görev açılmaz.
  5. Ad benzerliği: engel değil. UYARI bildirimi yazılır ve risk girdisine işlenir.
- `POST /sistem/yaptirim-listesi`: yalnız SistemAnahtari. Satır silinmez; kaldırılan karar damgalanır. Tek istekte en çok 50.000 satır.
- Kiracı uçları:
  - `GET /masak/yaptirim`
  - `PATCH /masak/yaptirim-eslesmeleri/{id}`: "bildirildi" ancak tarih ve evrak no ile; TAM eşleşme "aynı kişi değil" yapılamaz.
  - `GET /masak/uyum-ozet` (Panel).
- Ekran:
  - `Bilesenler/Uyum/YasalListePaneli.razor`: liste tarihi ve kaynağı, 14 günden eskiyse sarı bant, eşleşmeler, kapatma çekmecesi, "aynı kişi değil" için OnayKipi, kilit için DegismezIpucu.
  - `Bilesenler/Uyum/UyumYasalBantlari.razor`: Panelde mavi katlanır saklama, sarı eski liste, kırmızı eşleşme bantları.
  - Tezgâhta `YAPTIRIM_ENGEL` gelince kırmızı bant (sözlük cümlesi) çıkar ve Kaydet kilitlenir, sebebi yazılı.

### 2.5 § 4.5 Risk
- `RiskServisi.GirdiTopla` artık `YaptirimEslesmesiVar` değerini gerçek eşleşmeden okur ("aynı kişi değil" hariç).
- Cari YÜKSEK'e yeni geçtiğinde SAHİP/MUHASEBE'ye tek bildirim gider (`RISK_YUKSEK`). Engel yok. Elle ezme yolu da aynı bildirimi üretir.

### 2.6 § 4.6 Eşik teyidi
YASAL-1 tablosunda zaten doğru: 185.000 DOGRULANDI, 200.000 / 2 M / 20 M TEYITSIZ. Kod değişmedi. T-1 notunu "Yasal sınırlar" ekranına yazmadım, açık kalan.

### 2.7 YASAL-2 devri — günlük kapı red metni
`TevsikKurali.Degerlendir` iki metni tek sade cümleye indirildi. Görünür metinde "459/VUK/§/Tebliğ/md./tevsik" yok ve cümle ne yapılacağını söyler. Örnek:
> "Bu kişiyle bugün nakit toplamı 35.000,00 TL oluyor (önceki 20.000,00 TL, bu işlem 15.000,00 TL) ve 30.000,00 TL nakit sınırını 5.000,00 TL aşıyor; kalanı kartla ya da havaleyle ödenmeli."

- Red artık `kod: NAKIT_SINIRI` taşır. İstemci (Islemler BASİT, Hurda) reddi bu kodla tanır; eski metin yedeği de korundu.
- Dayanak tahsilat penceresindeki mevcut "Neden?" ipucunda kalır (YASAL-2).

## 3. Sapmalar ve gerekçeleri
| # | Şartname | Yapılan | Gerekçe |
|---|---|---|---|
| 1 | `ayniKisiBeyani` gövdenin üst düzeyinde | `masak.ayniKisiBeyani` | 15 çağrı noktası ve 10+ DTO yerine tek MASAK bloğu kullanıldı. İstemci gövdesi tek ifadeyle değişti (`MasakGovdesi`). |
| 2 | Hata kodu yeni alan olarak | `kod` gövdeye yalnız işaretli redde eklenir, `alanlar.ayniKisiBeyani` de verilir | T-4a gövde sözleşmesi korundu. |
| 3 | K-4 örneği 100.000 + 90.000 | 20.000 + 15.000 NAKİT (tevsik bileşeni). MASAK bileşeni 6 × 29.000 + 29.000 HAVALE ile sınandı. | B-3 K2 kuralı Peşin kartta tek işlemi 30.000 ile sınırlar; 100.000 hiç kayda giremez. |
| 4 | Tevsik pencere bileşeni "toplam nakit" | Önceki nakit + bu işlemin TAMAMI | Kapı bu işlemin nakit payını bilmez. Tutucu yön: yalnız fazladan soru çıkar, hiçbir işlem gizlice geçmez. |
| 5 | Migration `*_HareketMasakOlcu` | Tek migration `Yasal3MasakOlcuSaklamaYaptirim` | Kolon ve iki tablo tek şema adımında. İçerik okundu (§ 4). |
| 6 | `saklama_bekletmeleri` append-only | Silinmez. PATCH (bitiş/gerekçe) ve "kaldır" damgası var. | SÖZLEŞME-DoD her varlık için PATCH + pasife alma istiyor. Her değişiklik denetime yazılır. |
| 7 | Uyum → "Yasal liste" ayrı sekme | Risk sekmesinin başına tek satırlık çağrı | Uyum.razor KESIF şeridinde. Yeni sekme 5 satır düzenleme isterdi, tek satır tercih edildi. |
| 8 | GiderPusulasiKapisi.Dogrula içinde tarama | SatisModulu HurdaAlis + SatisFisi altın bacağında `hurda.Dogrula` yanında | Kapı db'siz ve senkron. |
| 9 | Kuru koşum şartnamede "öneri" | Varsayılan `Kuru=true` | Geri alınamaz işlem. Açma kurucu kararı (K-2). |
| 10 | Panel "Bekleyen işler" | Bantlar sayı bildirir (`SayiDegisti`), kutu "Başka iş yok. Yasal uyarılar üstteki kutularda." der | Tarayıcıda "Bekleyen iş yok — bugün temiz" ile kırmızı bant çelişkisi görüldü (YASAL-2 t3 deseni). |

## 4. Migration
- Ad: `20260930211943_Yasal3MasakOlcuSaklamaYaptirim`. Build'li `dotnet ef` ile üretildi; .cs 9.910 B, Designer 253.616 B.
- `Up()`: `AddColumn masak_olcu_tl` + geri doldurma SQL'i + 3 tablo + 5 indeks. Başka şeridin model farkı yok, okunarak doğrulandı.
- `kuyumcu_net`'e uygulandı; `__EFMigrationsHistory` son satırı bu migration.
- Geri doldurma kanıtı (K-3, canlı SQL):

| Ölçü | Değer |
|---|---|
| HURDA_ALIS toplam | 13 |
| Pusulalı HURDA_ALIS | 9 |
| `masak_olcu_tl` dolu | 9 |
| `brut_tutar`'dan farklı olan | 0 |
| Pusulasız HURDA_ALIS, NULL | 4 |
| Geçmiş TAKAS, ölçüsü bilinmiyor (NULL) | 3 |
| Diğer türlerde dolu | 0 |

## 5. Mevzuat
Birincil kaynaklar şartname § 3.1'de, 30.09.2026'da okundu ve bu pakette yeniden kullanıldı. Yeni bir yasal sayı koda yazılmadı.
- 185.000: YASAL-1 tablosundan okunuyor.
- Saklama 8 yıl, son işlem: Tedbirler Yön. md.46.
- KVKK md.7 ve md.11/2.
- 6415 md.10 ve 12/3 (RG + 30 gün).
- MASAK GT 34 md.5 ("ivedilikle bildir ve ertele").

Politika değerleri (60 dk pencere, 14 gün eski liste, 1 yıl bekletme) mevzuat değildir ve yapılandırmada/sabitte "politika" damgasıyla durur. T-1, T-2, T-3, T-5 DOĞRULANAMADI; davranış değişmedi.

## 6. DoD sözleşmesi
| Varlık | Oluştur | Düzenle (PATCH) | Pasife al / sil | Yetki | Denetim | Ekran |
|---|---|---|---|---|---|---|
| `saklama_bekletmeleri` | POST `/masak/saklama-bekletme` | PATCH `/{id}` (bitiş, gerekçe) | POST `/{id}/kaldir` (silinmez) | uyum + SAHIP | YazmaKaydet ×3 yol | Beklet / Düzenle / Kaldır; kaldırılan için DegismezIpucu |
| `yaptirim_eslesmeleri` | sistem (tarama) | PATCH `/masak/yaptirim-eslesmeleri/{id}` | silinmez; kilit elle açılmaz (6415 md.10) | uyum + SAHIP | YazmaKaydet / RedKaydet | Kapat (çekmece), Aynı kişi değil (onay), DegismezIpucu |
| `yaptirim_kayitlari` | POST `/sistem/yaptirim-listesi` | aynı uç (kaldır damgası) | silinmez | SistemAnahtari | SistemYazmaKaydet | kiracıda salt okunur (tarih, kaynak) |
| `hareketler.masak_olcu_tl` | hareketle birlikte | — | — | — | — | türetilmiş (finansal defter; hard delete yok) |

DuzeltmePolitikasi haritasına 3 varlık eklendi.

## 7. Testler
- Yeni **36 test**:
  - `Yasal3MasakTestleri.cs`: 13 (MasakBrutOlcu 3 + Http 1, PesinArtArda 4 + Http 1, TevsikGunlukRedMetni 4)
  - `Yasal3SaklamaYaptirimTestleri.cs`: 13 (SaklamaOtomatik 7, YaptirimDb 2, YaptirimHttp 4)
  - `Yasal3EkranTestleri.cs`: 10 bUnit
- Güncellenen: `IsCatisiSafTestleri` (M6-6: 0 → tam 1 yalnız SaklamaUygulaIsi; DI iş listesi).
- Kabul maddeleri arasında K-12 (Panel sarı bant: 15 gün eski var, 13 gün yok) bUnit ile karşılanır.
- Yeni red metnine göre ifade assert'leri güncellendi (davranış aynı):
  - TevsikTestleri, IadeTestleri, YasalParametreHttp, B4AltinBacagi, NakitPayiYazma ×4, KumulatifKapiSubeden ×2, P23c, L1TevsikFisHttp, SatisFisi, SUBEKAPSAMASinir, MaliyetTestleri
- Peşin art arda kuralına uyarlandı: PesinMusteriTestleri B3_08 ve L1bAlisFisiHttp L1bH_01/02. İkinci Peşin işlem "başka biri" beyanıyla gönderiliyor.
- Filtreli son koşum: **114/114**.
- Tam süit (`trx-pYASAL-3/_Mac_2026-10-01_01_42_12.trx`, build14 kodu): **8266 toplam · 8226 geçti · 39 başarısız · 1 atlandı**. Başarısızların sahipleri § 13'te.
- Tam süit sonrası tek değişiklik: bir `record`'un .razor'dan .cs'e taşınması (davranışsız, denetim sayacı için). Ürün projeleri 0 hatayla derlendi; filtreli testler yeniden yeşil.

## 8. Mutasyon tablosu (hepsi geri alındı, `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| MY3-1 | Takasta `MasakOlcuTl = farkTl` | MasakBrutOlcuTestleri.K1_…, MasakBrutOlcuHttpTestleri.K1_HTTP_… |
| MY3-2 | Hurdada `MasakOlcuTl = odenecek` (net) | MasakBrutOlcuTestleri.K2_hurdada_kumulatif_brut_bedelle_olculur |
| MY3-3 | Kümülatif sorgu (1) çıkarıldı | K1_…, K2_…, K1_HTTP_… |
| MY3-4 | Peşin pencerede kullanıcı koşulu kaldırıldı | PesinArtArdaTestleri.K4b_baska_kullanicinin_pesin_satisi_soru_acmaz |
| MY3-5 | Son işlem (hash) kontrolü silindi | SaklamaOtomatikTestleri.K7_… |
| MY3-6 | Bekletme kontrolü silindi | SaklamaOtomatikTestleri.K8_… |
| MY3-7 | Yaptırım TAM'da throw yerine return | YaptirimHttpTestleri.K10_… |
| MY3-8 | Ad benzerliğinde throw | YaptirimHttpTestleri.K11_… |
| MY3-9 | Red metnine "459 s. VUK GT md.257" geri eklendi | TevsikGunlukRedMetniTestleri.Red_metni_kodsuz_…(0, 45000) |
| MY3-10 | Cari kilidi denetimi silindi | YaptirimDbTestleri.K11b_… |
| MY3-11 | Panel sarı bant koşulu `false` | Yasal3EkranTestleri.K12_Panel_liste_15_gun_eski_… |
| MY3-12 | "Hayır" beyanı set etmiyor | Yasal3EkranTestleri.Pesin_art_arda_soru_Hayir_… |
| MY3-13 | SAKLAMA_YAKIN anahtarına gün eklendi | SaklamaOtomatikTestleri.K9_otuz_gun_kala_iki_tur_tek_bildirim |
| MY3-14 | YÜKSEK'te kalışta da bildirim | YaptirimDbTestleri.K14_risk_yuksege_gecis_tek_bildirim |
| MY3-15 | Kuru varsayılanı false | SaklamaOtomatikTestleri.Kuru_kosumda_is_maskelemez |
| MY3-16 | `YaptirimKilidi` her zaman null | Yasal3EkranTestleri.Yaptirim_engeli_kirmizi_bant_ve_Kaydet_kilitli_sebepli |

Son mutasyondan sonra temiz `dotnet build` yapıldı (build10–14, 0/0).

## 9. denetim.sh
**Benim eklediğim ya da güncellediğim sayaçlar:**

| Sayaç | Sonuç |
|---|---|
| `IKiraciVarlik uygulayan varlık` | 74 ✓ (72 → 74) |
| `Isler/ altında Uygula(` | tam 1, yalnız SaklamaUygulaIsi ✓ |
| `Sınırsız ToListAsync` | 143 ≤ 145 ✓ (yeni sorguların hepsi `.Take`) |
| `YASAL-3: YASAL3_OLCU_IKI_SORGU_EKSIK` | 0 ✓ |
| `YASAL-3: YASAL3_SATIR_SILME` | 0 ✓ |
| `P-02: OLCUM_TABANI_KOPYA` | 0 ✓ (ölçüm json'u 74'e güncellendi) |
| `P-08: HARETURU_DISI_SABIT` | 0 ✓ |
| razor record | ✓ (record .cs'e taşındı) |

**Son koşumda kalan ✗ (bana ait değil):**
- `P-30: DEMO_INSAN_SURESI`: tarih kapısı 30.09 geçti.
- `B-3: ESIK_ISTEMCIDE 5 > 4`: yeni satır `Pages/Islemler.Satis4.cs:80` (SATIS-4 şeridi).
- Önceki koşumda görülen `P-10: K21_DONANIM_BEYANI` (Urunler.razor, KESIF şeridi) son koşumda yok.

## 10. Canlı doğrulama
Kurulum:
- API 5162 `--no-build`, `Isler__Etkin=false`: kurucunun veritabanında arka plan maskelemesi olmasın diye.
- `Isler__SaklamaUygula__Kuru=false`: yalnız kendi elle koşumlarım için.
- Süreç içi rastgele sistem anahtarı, dosyada chmod 600, çıktıya basılmadı.
- Kiracı: `PAKETYASAL3-TEST Altın Sarayı Kuyumculuk` (`01M3T5NGZ6Q7DAAAZQBDDC6FN1`).

| Adım | Beklenen | Gerçek |
|---|---|---|
| K1 takas 900k↔910k kimlikli → 100.000 satış | 400, kümülatif 1.010.000 | 400 "…kümülatif 1.010.000,00 TL…" · SQL TAKAS `tl_tutari 10000, masak_olcu_tl 910000` · 100k satış 0 satır |
| K2 hurda brüt 180.000 esnaf muaf (HAVALE) → 6.000 satış | 400, 186.000 | 400 "…kümülatif 186.000,00 TL…" · SQL `-176400 / 180000`; pusula `brut 180000, net 176400` (ilk deneme VERESIYE 400 "Ödeme türü beklenen biçimde değil": HTTP'de hurda VERESIYE kabul edilmiyor, HAVALE ile tekrarlandı) |
| K4 Peşin 20k → 15k beyansız / true / false | 400 SORU / 400 / 200 | 400 `[kod PESIN_ART_ARDA_SORU]` · 400 "Müşteriyi seçin…" · 200 · SQL denetim `PESIN_ART_ARDA_FARKLI_KISI` 1 |
| T (YASAL-2 devri) aynı gün 20k + 15k nakit | 400, kodsuz tek cümle | 400 `[kod NAKIT_SINIRI]` "Bu kişiyle bugün nakit toplamı 35.000,00 TL oluyor…; kalanı kartla ya da havaleyle ödenmeli." |
| K13 JWT → `POST /sistem/yaptirim-listesi` | 401/403 | 403 |
| Liste yükleme (sistem anahtarı) | 200 | 200 `eklenen 3, kilitlenenCari 1, acilanGorev 1` |
| K10 listedeki TCKN ile 200k satış | 400, hareket 0, bildirim 1, denetim 1 | 400 `YAPTIRIM_ENGEL` · o cariye 200k satış 0 · KRİTİK "işlem durduruldu" 1 · denetim `YAPTIRIM_ENGEL/REDDEDILDI` 1 |
| K11 yalnız ad benzer | 200 + UYARI | 200 · UYARI "Olası yasal liste eşleşmesi" · eşleşme AD_BENZERLIGI |
| K11b liste ↔ 5.000 TL bakiyeli cari | KRİTİK görev, son gün RG + 30 | KRİTİK "…son gün 29.10.2026" (RG 29.09) · eşleşme `LISTE_GUNCELLEME/ACIK/2026-10-29` · kilitli cariye satış 400 |
| Saklama: süresi dolmuş carisiz tespit | bekletmede maskelenmez, kaldırınca maskelenir | B1 201 · B2 400 "Gerekçe en az 20 karakter olmalı (4/20)." · B3 PATCH yalnız bitiş (gerekçe korundu) · B4 iş: 0 kayıt · B5 kaldır 200 · B6 iş: 1 kayıt, 1 bildirim · SQL `maskeli 1, uyruk/doğum/adres NULL, tutar 190000` · denetim "(otomatik)", kullanıcı NULL |
| Uyum özeti / yasal liste | 200 | `acikTam 2, acikBenzerlik 1, listeTarihi 2026-10-01` |
| Tarayıcıda "Hayır, başka biri" tıklaması | kayıt + sonuç | "Son fiş: Satış · 01:40 · 1.500 ₺" · SQL 1.500 satış 1, denetim +1 |

Temizlik:
- API ve vekil `kill` ile durduruldu; 5162/5163 LISTEN 0.
- `zsh kiraci-temizle.sh 01M3T5NGZ6Q7DAAAZQBDDC6FN1 --uygula` sonrası ikinci koşum "Kiracı YOK" dedi.
- Kiracı-üstü test satırları elle silindi: `yaptirim_kayitlari` 3 satır (tablo yeniden 0), `kiraci_davetleri` 1 satır, `SISTEM` denetimi 2 satır.
- `tenants`: başlangıç 13, son 14. Aradaki fark benim değil: KESIF-P5 ve KESIF-P3 şeritleri 22:20–22:23 UTC'de kiracı açtı. `PAKETYASAL3%` sayısı 0.
- `fikstur.json` ve `sis.anahtar` `printf ''` ile boşaltıldı.
- QA-TEST-* kiracılarına dokunulmadı.

### 10.1 Gerçek başsız Chrome ölçümü
Betikler ve görüntüler: `/private/tmp/kuyumcu-serit/YASAL-3/tar/` (`ui.mjs`, `olcum.jsonl`, `foto/`).

| Ekran | 1280 BASİT açık | 1280 PRO açık | 1280 BASİT koyu | 1280 PRO koyu | 375 BASİT açık | 375 PRO açık | 375 BASİT koyu | 375 PRO koyu |
|---|---|---|---|---|---|---|---|---|
| D Panel bantları | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| S Uyum → Saklama (bekletme) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| R Uyum → Risk (yasal liste) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| P Tezgâh Peşin sorusu | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Y Tezgâh yaptırım engeli | 0 | 0 | 0 | 0 | 0 | 0 | 0 | K3k 1 † |
| B Beklet çekmecesi | K9 6 * | — | — | — | — | K9 6 * | — | — |
| K Kapat çekmecesi | — | K9 2 * | — | — | — | — | K9 6 * | — |

Hücredeki "0" şu ölçülerin tamamının sıfır olduğunu gösterir: yatay kaydırma, K3 taşan, K3k kırık kısa etiket, K9 binen, K1 görünen `[hidden]`, K5 `TRY`, K13 kırpılan, K10b kesik, modal içi taşma.

\* Çekmecenin arkasındaki sayfa öğeleri (örtü). Çekmece içi taşma 0.

† `#islem-kaydet` "Kaydet ve yazdır" dar koyu temada iki satıra kırılıyor. Bu SATIS düzeni: aynı durum açık temada tek satır. Bkz. § 13.

Ölçüm sırasında düzeltilenler (görüntü ile görüldü):
- Bekletme tablosunda kısa etiketler kırılıyordu (K3k 2–3). Durum ayrı sütuna alındı, `hucre-tek-satir` eklendi.
- Yasal liste tablosunda başlıklar yanlıştı ("Kapsam"), DegismezIpucu satır başına tekrarlanıyordu (K9 5). Başlıklar düzeltildi, ipucu tek kez yazılıyor.
- 375'te tablolar `KartListe` ortak sarmalayıcısına alındı (kaydırma ipucu).
- Soru düğmeleri 375'te taştı. `dugme-satir`, `nowrap` ve ikincil stil verildi; tek birincil kuralı korundu.
- "Beklet" ikincil yapıldı (sayfanın "+ Yeni bildirim"i birincil).
- Uyum sayfasında `?sekme=` ile aynı sayfa içi geçiş boş panel çiziyordu. Bu ölçüm betiğinin yan etkisi; önce Panel'e dönülerek ölçüldü.

Read ile görülen görüntüler:
- `D-panel-PRO-1280-aydinlik`, `D-panel-BASIT-375-karanlik`
- `S-uyum-saklama-PRO-1280-aydinlik`, `S-uyum-saklama-BASIT-375-karanlik`
- `B-bekletme-cekmece-BASIT-1280-aydinlik`
- `R-uyum-yasal-liste-BASIT-1280-aydinlik`, `R-uyum-yasal-liste-PRO-375-aydinlik`
- `K-eslesme-kapat-cekmece-BASIT-375-karanlik`
- `P-pesin-soru-PRO-1280-aydinlik`, `P-pesin-soru-BASIT-375-aydinlik`, `P-pesin-soru-PRO-1280-karanlik`
- `Y-yaptirim-engel-PRO-1280-aydinlik`, `Y-…-PRO-375-aydinlik`, `Y-…-PRO-375-karanlik`
- `H-pesin-hayir-sonrasi-PRO-1280-aydinlik`

## 11. Değişen dosyalar (bu paket)
**API:**
- `Data/Entities.cs` (Hareket.MasakOlcuTl)
- `Data/KuyumcuDbContext.cs` (3 DbSet, indeksler, şifreleme, beyaz liste)
- `Data/SaklamaBekletme.cs` (YENİ), `Data/Yaptirim.cs` (YENİ)
- `Data/Migrations/20260930211943_Yasal3MasakOlcuSaklamaYaptirim(.Designer).cs` (YENİ), `KuyumcuDbContextModelSnapshot.cs`
- `Common/ApiException.cs` (KodIle + gövde `kod`)
- `Modules/Masak/MasakKapisi.cs`, `MasakKuralMotoru.cs` (PesinPencereDakika), `SaklamaSuresiIsi.cs`, `RiskServisi.cs`
- `Modules/Masak/PesinPencereKapisi.cs`, `YaptirimServisi.cs`, `SaklamaYaptirimModulu.cs` (YENİ)
- `Modules/Isler/Isler/SaklamaUygulaIsi.cs` (YENİ), `SaklamaOnizlemeIsi.cs` (Etkin)
- `Modules/Satis/SatisModulu.cs` (takas/hurda ölçüsü, 9 çağrıya `islem`, hurda ve altın satıcısı tarama), `SatisModulu.AltinBacagi.cs` (brüt ölçü)
- `Modules/Cari/CariModulu.cs` (CariTara ×2)
- `Modules/Sistem/SistemModulu.cs` (liste ucu)
- `Modules/Tevsik/TevsikKurali.cs` (red metni, RedKodu), `TevsikKapisi.cs` (KodIle)
- `Modules/Meta/DuzeltmePolitikasi.cs` (3 varlık)
- `Modules/ModulKayit.cs`, `Program.cs`, `appsettings.json` (Masak:PesinPencereDakika, Isler:SaklamaUygula)

**Core:** `I18n/Sozlukler.Yasal3.cs` (YENİ, TR+EN)

**Web:**
- `Bilesenler/Fis/PesinArtArdaSoru.razor` (YENİ)
- `Bilesenler/Uyum/{UyumYasalBantlari, SaklamaBekletmePaneli, YasalListePaneli}.razor`, `UyumYasalModelleri.cs` (YENİ)
- `Pages/Islemler.Yasal3.cs` (YENİ)
- `Pages/Islemler.Odeme.cs`: 2 satır (YasalKilitSebebi, NAKIT_SINIRI tanıma)
- `Servisler/ApiIstemci.cs` (ApiHata.Kod)

**Dokunulan sayfalar (tek satırlık çağrılar):**
- `Pages/Islemler.razor`: 1.399 satır, satır sayısı değişmedi. `masak = MasakGovdesi` ×9 aynı ifade; soru bileşeni 1; catch 1; başarı sıfırlama 1; MusteriDegisti 1.
- `Pages/Panel.razor`: bant bileşeni 1; BosDurum ×2 ifade; alan bildirimi 1.
- `Pages/Uyum.razor`: SaklamaBekletmePaneli 1, YasalListePaneli 1.
- `Pages/Hurda.razor`: NAKIT_SINIRI kodu ×2.

**Testler:**
- Yeni: `Yasal3MasakTestleri.cs`, `Yasal3SaklamaYaptirimTestleri.cs`, `Yasal3EkranTestleri.cs`
- Güncel: `IsCatisiSafTestleri`, § 7'deki ifade/uyarlama dosyaları

**Diğer:**
- `dotnet/denetim.sh`: varlık 74, Uygula tam 1, YASAL-3 bloğu, tavan notu
- `dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json` (74)
- `dokumanlar/02-mimari/adr/0008-saklama-suresi-maskeleme.md` (güncelleme bölümü)

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| K-1 takas kümülatif 1.010.000 → 400 | ✓ | DB + HTTP test, canlı, SQL 910000 |
| K-2 hurda brüt 186.000 → 400 | ✓ | DB test, canlı |
| K-3 geri doldurma | ✓ | § 4 SQL (fark 0; 4 pusulasız + 3 takas NULL) |
| K-4 Peşin soru / false / true | ✓ (tutar uyarlandı, § 3 md.3) | DB + HTTP + bUnit, canlı |
| K-5 61 dk pencere dışı | ✓ | K5_61dk_arayla_soru_yok |
| K-6 süresi dolan maskelenir, tutar korunur | ✓ | K6_… + canlı B6 |
| K-7 son işlem kuralı | ✓ | K7_… |
| K-8 bekletme | ✓ | K8_… + canlı B4/B6 |
| K-9 iki tur tek bildirim | ✓ | K9_… |
| K-10 tam eşleşme engeli | ✓ | HTTP + canlı + SQL |
| K-11 ad benzerliği geçer | ✓ | HTTP + canlı |
| K-11b 30 günlük görev | ✓ | DB test + canlı |
| K-12 Panel eski liste bandı | ✓ | bUnit (15/13) + tarayıcı (16 gün) |
| K-13 JWT 401/403 | ✓ | HTTP + canlı 403 |
| K-14 YÜKSEK geçişi tek bildirim | ✓ | DB test |
| K-15 mevcut MASAK sınır testleri | ✓ | MasakEntegrasyon/Tevsik sınıfları yeşil |
| K-16 tarayıcı ölçümü | ✓ | § 10.1 |
| Mutasyon 1–8 (+8 ek) | ✓ | § 8 |
| YASAL-2 devri: kodsuz tek cümle | ✓ | TevsikGunlukRedMetni + MY3-9 + canlı T |
| Yeni kırmızı üretmeme (7) | ✓ (kanıtla) | § 13 |

## 13. Açık kalanlar (sahibiyle)
1. **Tam süitteki 39 kırmızı benim değil.**
   - 32'si SATIS3/CumaSAldim/BasitModPos/SATIS3Sinir/KATMAN2Sinir ekran testleri, Islemler ödeme varsayılanı ile ilgili. **Sahibi: SATIS-4 şeridi.**
     - Kanıt 1: aynı testler benim Islemler değişikliklerim yerindeyken 00:44–00:48 koşumunda (8250 test, test6) yeşildi.
     - Kanıt 2: SATIS-4 şeridi `Islemler.razor` (01:30), `FisOdemeKasa.razor` (01:28) ve `Islemler.Satis4.cs` (01:45) dosyalarını değiştirdi.
     - Kanıt 3: şu an test projesi `Satis4Yardim` eksikliğinden derlenmiyor.
   - `OrtakGorunumKiyasSaflik.KY10_vezne…` → Vezne.razor (KESIF).
   - `P78.FisKalemTablosuTurkce…` → sayı biçimi (FisKalem, başka şerit).
   - `GorunumSubesiTestleri.K3_Aylik` ve `KarZararEkranTestleri.P35_08` → 1 Ekim 00:3x yerel / 30 Eylül UTC ay sınırı.
   - `TevsikIhlalDbTestleri.K16 ×2` ve `KesifP2HttpTestleri.CariEkstre` → gece yarısı sınırı ("−40 dk" önceki güne düşüyor), önceki koşumlarda görüldü.
2. **ProYanSutun çelişkisi.** Yaptırım engeli açıkken PRO sağ sütun "Yasal kapılar uygun" yazıyor; bu kutu yalnız nakit ve kimlik eşiğine bakıyor. Sahibi SATIS (`FisProYanSutun.razor`). Öneri: `YasalHata` yerine ayrı bir "engel" parametresi.
3. **375 PRO koyu:** "Kaydet ve yazdır" iki satıra kırılıyor. SATIS düzeni, açık temada tek satır.
4. **T-1 notu** ("bağlantılı işlem = aynı gün aynı kişi" yorumu teyit bekliyor) "Yasal sınırlar" ekranına yazılmadı. YasalSinirlar.razor YASAL-1 alanında.
5. **"Risk listesinde eşik bölme sinyali" satırı** ayrıca çizilmedi. İz denetim kaydında (`PESIN_ART_ARDA_FARKLI_KISI`).
6. **Peşin tuzağı (CariModulu/BankaModulu yolları):** bu yollar `DogrulaKumulatif`'i `islem` vermeden çağırıyor. Peşin kartı orada kullanılırsa pencere kiracı genelidir (fail-closed yön) ve denetim SistemYazmaKaydet ile yazılır.
7. **Peşin eşik mesajı** hâlâ "tevsik (459 s. VUK GT)" yazıyor (SistemCarisi.EsikMesaji, B-3). Bu paketin devri değildi; ayrı düzeltme gerekir.
8. **`denetim.sh` başka şerit ✗:** `P-30 DEMO_INSAN_SURESI` (tarih geçti) ve `B-3 ESIK_ISTEMCIDE` (Islemler.Satis4.cs, SATIS-4).

## 14. Kurucu / mali müşavir
- **K-2:** Otomatik maskeleme şu an kuru (yalnız bildirim). Öneri: 90 gün sonra `Isler:SaklamaUygula:Kuru=false`.
- **K-3:** Yaptırım listesinin kaynağı ve biçimi. Altyapı resmî ücretsiz yayını (MASAK sayfası) ByGatech operatörünün sistem ucundan yüklemesine göre kuruldu. Listenin 14 günde bir tazelenmesi hizmet taahhüdü (SLA) konusudur. Ticari sağlayıcı kurucu kararıdır.
- **K-4 / T-2 / T-3:** Teyitsiz kademeler (200.000 amaç beyanı; 2 M / 20 M) tutucu biçimde zorunlu kalıyor.
- **K-5:** Peşin pencere 60 dk, aynı kullanıcı + aynı şube (politika). Sahada ölçülerek ayarlanmalı.
- **T-1:** "Bağlantılı işlem" okuması (aynı yerel gün + aynı cari) mali müşavir teyidi bekliyor.
- **T-5:** Takasta Max(eski, yeni) tutucu yorumu mali müşavir teyidi bekliyor.
- **Mali müşavire:** Yaptırım eşleşmesinde "işlemi ertele + MASAK'a ivedilikle bildir" akışı MASAK GT 34 md.5'e göre kuruldu. Bildirim biçimi (MASAK Online / yazı) teyit edilmeli; ürün yalnız tarih + evrak no ile kapatma kaydı tutar.
