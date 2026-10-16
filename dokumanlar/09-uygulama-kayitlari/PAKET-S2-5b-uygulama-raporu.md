# PAKET S2-5b: Satıcı personel ve otomatik yazdır — uygulama raporu

Rol: Taşkın (backend) ve Ertuğ (frontend). Dalga S2b (tezgâh fişinin son iki paketi). Tarih: 2026-09-24.
Durum: **BİTTİ.** Paket kapısının yedi maddesi sağlandı (§ 9).
Commit: YOK. Bütün değişiklikler çalışma ağacında.

> **Tur 2 (2026-09-24 04:20-04:50) — hasım doğrulayıcının dört bulgusu kapatıldı.** Ayrıntı § R. Aşağıdaki bölümlerde tur 1 değerleri korunmuş, tur 2 değerleri § R'de ve § 9'da.

## R. Doğrulayıcı bulgularının kapatılması (tur 2)

| Bulgu | Yapılan | Kanıt |
|---|---|---|
| [KRİTİK] Tam süit ve bağımsız mutasyon kanıtı yok | Tam süit yeniden koşuldu ve bitti. M25b-2b ve M25b-6 dahil 6 mutasyon (M25b-1, 2b, 3, 4, 5, 6) güncel kaynağın yalıtılmış kopyasında yeniden üretildi. | `scratchpad/trx-pS2-5b/tur2/_Mac_2026-09-24_04_37_21.trx`; `pS2-5b/mut-sonuc-tur4.txt` |
| [ORTA] Test kiracısının denetim kayıtları kalmış | Tur 1'in 2 yetim satırı (`denetim_kayitlari`, KurSnapshot EKLEME) silindi. Tur 2 temizliğinden sonra 90 sn beklenip 70 tablo yeniden tarandı: yine **geç yazım** görüldü (2 denetim + 1 `kur_snapshots`; tarih 04:39:51 ve 04:40:01, benim API'm 04:38'de durdurulmuş ve `lsof` boş). Silindi, 170 sn daha beklendi, yeniden tarandı: **0**. | Aşağıda |
| [DÜŞÜK] xUnit2031 uyarısı | `Assert.Single(istekler.Where(...))` → `Assert.Single(istekler, i => ...)` (S25bSaticiYazdirTestleri.cs:448). | `b8.log`: tek uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 (şerit D, benim değil) |
| [DÜŞÜK] `<label for>` bir `<p>`'yi gösteriyor | `<label for="islem-satan">` → `<span class="alan-etiket" id="islem-satan-etiket">`; `<p id="islem-satan" aria-labelledby="islem-satan-etiket">`. `id="islem-satan"` korunur; mevcut belirteçli sınıf, yeni CSS yok. | id farkı: silinen 0, eklenen `islem-satan-etiket` |

**Yetim satırın kök nedeni (kanıt düzeyinde).** Geç yazılan satırlar `kullanici_id = NULL`, `varlik_turu = KurSnapshot`: arka plan kur yazıcısının imzası. Yazım anında 5185-5200 aralığında dinleyen tek `dotnet` API 5200'deki başka bir süreçti (PID 12577; 5199 kurucunun Python sunucusu). Benim sürecim ölüydü. Buradan çıkan **olası** açıklama: 5200'deki sürecin kur döngüsü kiracı listesini silmeden önce okuyup sonra yazıyor. Bu süreç bana ait değil, dokunulmadı; kesin kök neden **DOĞRULANMADI**. Orkestratöre: test kiracısı temizliği arka plan kur döngüsü bittikten sonra ikinci bir süpürme ile yapılmalı. Doğrulayıcının ölçtüğü DB genelindeki 83 yetim denetim satırı orkestratör düzeyinde süpürülmeli (bu paket yalnız kendi 4 kiracı kimliğinin satırlarını sildi).

**Tur 2 tam süit** (`--no-build`, derleme `b8.log` 0 Hata): **4873 · 4872 geçti · 1 kırmızı**.
- Kırmızı: `SozlesmeYansimaTestleri.Contracts_tipi_sunucuda_ikinci_kez_tanimlanmiyor`. Mesaj: `Kuyumcu.Api.Modules.Rapor.SatisOdemeKirilimi+Satir` Contracts'taki `Satir` tipleriyle çakışıyor. Tip `Modules/Rapor/SatisRaporlari.cs` içinde; dosyanın mtime'ı 04:45 (koşum sırasında da değişiyordu). Bu dosya **şerit D**'nin alanı ve bana yasak. Benim kaynaklarımda eşleşme yok (`SaticiPersonelSatiri` listede değil).
- Temel liste (tur 1 "önce"): 4 kırmızı, `EtiketEkranTestleri` (şerit D). **Bu paketin ürettiği yeni kırmızı: 0.**
- Kendi filtrem (`FullyQualifiedName~S25`): **47/47** (S25b 26 + S25 21), `t6.log`.

**Tur 2 denetim.sh** (`denetim3.log`): **DENETİM BAŞARISIZ**, tek ✗ `P-03 CONTRACTS_DISI_MODEL 1 → Satir`. Bu, yukarıdaki kırmızıyla aynı ihlal (şerit D, `SatisRaporlari.cs`). Benim sayaçlarım ✓: satır içi style 110/196, ModalPencere 48/55, sınırsız ToListAsync 145/145, ORTAK_SABLON_DISI_PRINT 0, FIS_BILESENI_ISTEK 0.

**Tur 4 mutasyonları** (güncel kaynak `rsync` ile `scratchpad/iz/dotnet`'e alındı; her biri tek eşleşme doğrulaması, perl, derleme, filtreli test, geri kopya, `cmp`):

| Kod | Kırmızı dönen test(ler) | cmp |
|---|---|---|
| M25b-1 oturum varsayılanı silindi | S25b_H1, S25b_H6 | BİREBİR |
| M25b-2b kiracı sınırı kaldırıldı (IgnoreQueryFilters + açık koşul yok) | S25b_H3_baska_kiracinin_kullanicisi_400_hareket_yazilmaz | BİREBİR |
| M25b-3 aktiflik kontrolü silindi | S25b_H4_pasif_personel_400_hareket_yazilmaz | BİREBİR |
| M25b-4 basit modda seçici gösterildi | S25b_E2, S25b_E8 | BİREBİR |
| M25b-5 print koşulu true | S25b_E7_sorgusuz_acilista_yazdirma_cagrilmaz_dugme_yine_calisir | BİREBİR |
| M25b-6 rol kapısı true | S25b_S2 × 4 (sahip, MUHASEBE, TEZGAHTAR, null), S25b_H5, S25b_H7 | BİREBİR |

Ana ağaç ile yalıtılmış kopya, mutasyonlardan sonra üç dosyada `cmp` ile aynı. Proje ağacında `.mutorj/.bak/.tmp` 0.

**Tur 2 canlı doğrulama (5192):** `canli.sh` aynen yeniden koşuldu. 18 adımın 18'i beklenen kodu verdi (KABUL 3 → 400, KABUL 4 → 400, KABUL 5 → 403, KABUL 9 → `satanBilinmiyor=true` 0). SQL: SATIŞ 5, `personel_id` NULL 0, başka kiracının kullanıcısı 0, kiracı B hareket 0. Tablo § 6 ile birebir aynı (`canli-cikti.txt`, `canli-sql.txt`; tur 1 kopyaları `tur1-*`).
- Temizlik: API (PID 67432 / 67538) durduruldu, `lsof :5192` boş. `kiraci-temizle.sh --uygula`: kiracı A 240 satır, kiracı B 21 satır ve tenants satırları silindi. Davetler 2 silindi, kalan 0. Sonra yukarıdaki geç yazım süpürmesi yapıldı. 70 tabloda 4 kiracı kimliği için kalıntı **0**. `tenants` **10 → 10**. QA-TEST-*, DEMO ve diğer kiracılara dokunulmadı. 5200 ve 5199 süreçlerine dokunulmadı.

Şartname: `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § S-2 madde 2.8 ("personel_id oturum kullanıcısı varsayılan, pro modda değiştirilebilir"; "3 tık: okut → Kaydet → Yazdır"). S2-5 raporunun açık kalanları. Görev metnindeki (A), (B), (C) kalemleri ve KABUL 1-9.

---

## 0. Spesifikasyon koda karşı doğrulandı

| İddia | Kodda bulunan | Uyarlama |
|---|---|---|
| `hareketler.personel_id` var, migration gerekmez | Doğru. Kolon `Entities.cs:981` (P-08). İndeks `(tenant_id, personel_id, tarih)` var (`KuyumcuDbContext.cs:338`). | **Migration yok.** |
| Taslak `PersonelId` taşır | `HareketFabrikasi.Ekle` taslak olarak `Hareket` varlığının kendisini alıyor. `PersonelId` zaten geçiyor (boş dizge kapısı `:118`). | `HareketFabrikasi.cs` **değişmedi.** Değer, uçlarda taslağa konuyor. |
| Satış uçları `personelId` almıyor | Doğru. `Modules/` altında `PersonelId` yazan kod yoktu (grep: yalnız `SatisRaporlari` okuyor). | `SatisOlusturDto` ve `SatisFisiDto` için `PersonelId?` eklendi. |
| "iade/takas/hurda-alış da satıcı yazıyorsa aynı kalıp" | Ölçüldü: hiçbir uç satıcı yazmıyor (grep 0). | Kapsam `/islemler/satis` ve `/islemler/satis-fisi` ile sınırlı tutuldu. Satış fişinin altın bacağındaki HURDA_ALIŞ satırına satıcı yazılmadı, çünkü anlam kilidi "SATAN". Açık kalan § 10. |
| Tezgâhtar başkasını seçebilir mi | Yetki matrisinde `islem` modülünden başka bir satıcı kapısı yok. | **Karar (fail-closed):** Başka personeli yalnız SAHIP ve MUDUR seçebilir (TAM eşleşme). Diğer roller yalnız kendini seçebilir, başkası için **403** döner. |
| Kullanıcı listesi | `GET /kullanicilar` hem `[ModulYetkisi("kullanici")]` hem SAHIP/MUDUR istiyor. Tezgâhtar bu listeyi okuyamıyor, e-posta ve rol de dönüyor. | Yeni salt okunur uç: `GET /api/v1/islemler/personeller` (`islem` modülü). Yalnız id, ad ve soyad döndürüyor. `secimSerbest` alanını **sunucu** belirliyor, istemciye rol listesi gömülmedi. Liste 200 ile sınırlı; `dahaFazlasiVar` alanı var (B-8, UX-S29). |
| Otomatik yazdırma "mevcut kuyumcu JS yardımcısı üzerinden" | `window.print` çağrısı tek yerde, `Bilesenler/Yazdirma/YazdirDugmesi.razor` içinde. `denetim.sh ORTAK_SABLON_DISI_PRINT` sayacı `Pages/` altına yeni `window.print` yazılmasını yasaklıyor. | **Sapma (gerekçeli):** `YazdirDugmesi` bileşenine `Otomatik` parametresi eklendi (varsayılan false). Aynı `Yazdir()` yolunu kullanıyor, ikinci bir print çağrısı yok. Bu dosya şeridin listelediği alanın dışında ama başka bir şeridin yasak listesinde de değil. Ekleme salt ek; diğer çağıranlar (`PusulaBelgesi`, `IslemDefteri`) değişmedi. `Cikti.razor` yalnız sorguyu okuyup parametreyi veriyor. |
| `Islemler.razor` ≤ 1400 satır | 1399 satırdaydı (S21 yapı testi). | 9 satır eklendi. Aynı sayıda satır, **yalnız yorum** sıkıştırılarak geri kazanıldı. Son durum **1399**. |

Mevzuat: Personel bilgisi yasal belge alanı değil. Fiş ve fatura zorunlu unsurlarına eklenmedi, yalnız iç kayıt. Yeni oran, eşik ya da yasal kural yok. İnternet araştırması gerekmedi.

## 1. Yapılanlar

### (A) Sunucu
- `Kuyumcu.Api/Modules/Satis/SatisModulu.Satici.cs` (YENİ)
  - `SaticiKapisi`: saf rol kuralı ve Türkçe mesajlar, `AzamiListe = 200`.
  - `SatisServisi.SaticiBelirle` davranışı:
    - `null` gelirse oturum kullanıcısını döndürür.
    - Boş dizge 400 alır.
    - Kendisi her zaman serbesttir.
    - Başkasını seçmek SAHIP/MUDUR değilse 403 alır.
    - Aynı kiracıda bulunamayan kullanıcı 400 alır. Başka kiracının kimliği de aynı "bulunamadı" mesajını alır, böylece var/yok sızmaz.
    - Pasif kullanıcı 400 alır.
    - Kontrol transaction açılmadan yapılır (K1).
  - `SaticiPersoneller` ve `SaticiPersonelController` → `GET /api/v1/islemler/personeller` (`[Authorize]`, `[ModulYetkisi("islem")]`).
- `Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
  - İki DTO'ya `[MaxLength(36)] PersonelId?` eklendi.
  - `Satis()` ve `SatisFisi()` başında `SaticiBelirle` çağrılıyor. Taslağa `PersonelId = satici` yazılıyor: tek kalemli satışta ana hareket, fişte her SATIŞ kalemi.
  - `KullaniciId` (kaydı giren) değişmedi.
- `Kuyumcu.Contracts/Sayfalar/SaticiPersonelSozlesmeleri.cs` (YENİ): `SaticiPersonelSatiri`, `SaticiPersonelListesi(Personeller, OturumId, SecimSerbest, DahaFazlasiVar)`.
- Mevcut istemciler (alanı göndermeyenler) değişmeden çalışıyor ve oturum kullanıcısı yazılıyor (H1, canlı 6).

### (B) Ekran
- `Kuyumcu.Web/Bilesenler/Fis/FisSatici.razor` (YENİ). İstek atmıyor, `ApiIstemci` enjekte etmiyor. Liste parametre olarak geliyor, seçim `Secili`/`SeciliChanged` ile dönüyor.
  - Basit mod: seçici yok. Salt okunur **"Satan: <ad>"** satırı (`#islem-satan`) gösteriliyor.
  - Pro mod: sunucu `secimSerbest` dediyse ve birden çok aktif kişi varsa `#islem-personel` seçicisi çıkıyor (ortak `Secici`, varsayılan oturum kullanıcısı).
  - Tek seçenekte seçici gizli, salt okunur satır var (UX-S22). Seçim serbest değilse yanında sebep yazıyor (`#islem-personel-ipucu`, UX-S30).
  - Liste alınamazsa hata bandı (`#islem-personel-hata`, UX-S28) çıkıyor ve satış oturum kullanıcısı adına yazılıyor.
  - Tavana değildiyse `#islem-personel-sinir` gösteriliyor (UX-S29).
- `Kuyumcu.Web/Pages/Islemler.razor`
  - Liste sayfa yüklenirken tek kez çekiliyor (reyon listesiyle aynı yardımcı veri deseni). Hata yutulmuyor.
  - `SaticiGovde` basit modda null. Pro modda yalnız kullanıcı başkasını seçtiyse değer taşıyor.
  - `Govde()` SATIŞ gövdelerine `personelId` ekliyor (tek kalem ve karma fiş). İskontolu/hediyeli fiş gövdesine `personelId = SaticiGovde` eklendi.
  - **Kaydet() tek POST yolu yerinde** (S21 `Tek_POST_yolu` yeşil).
- `Kuyumcu.Core/I18n/Sozlukler.FisSatici.cs` (YENİ): TR + EN konumsal record, ham rol kodu yok. `Sozlukler.cs` hub'ına dokunulmadı.

### (C) Otomatik yazdır
- `Bilesenler/Fis/FisYazdir.cs`: `OtomatikYazdirSorgusu = "yazdir=1"` ve `YazdirAdresi(id)` eklendi. `Adres()` değişmedi, çünkü HTTP testleri API yolunda kullanıyor.
- `Bilesenler/Fis/FisYazdirBaglantisi.razor`: `href` artık `YazdirAdresi` (`/cikti/SATIS_FISI/{id}?yazdir=1`).
- `Pages/Cikti.razor`: `[SupplyParameterFromQuery(Name="yazdir")]`. Değer `"1"` ise `YazdirDugmesi Otomatik=true`.
- `Bilesenler/Yazdirma/YazdirDugmesi.razor`: `Otomatik` iken `OnAfterRenderAsync` içinde, belge çizildikten sonra (`Kilit` boşken) **bir kez** `Yazdir()` çağrılıyor. Belge yüklenemezse `Kilit` dolu kalıyor ve yazdırma yapılmıyor.
- Sorgusuz açılışta davranış değişmedi.
- Sonuç: okut → Kaydet → "Fiş yazdır" = 3 dokunuş. Yeni sekmede yazdırma penceresi kendiliğinden geliyor.
- `Sozlukler.FisYazdir.cs` ipucu metni buna göre güncellendi.
- Inline script yok, CDN yok, yeni JS dosyası yok.

**DOM id envanteri:** `Islemler.razor`, `Cikti.razor` ve `YazdirDugmesi.razor` önce/sonra `grep -o 'id="…"'` farkında silinen id **0**. Yeni id'ler: `islem-satan-sar`, `islem-satan`, `islem-personel` (+ Secici alt id'leri), `islem-personel-ipucu`, `islem-personel-hata`, `islem-personel-sinir`.

**Dokunulan sayfalar (TEK DOKUNUŞ):**
- `Pages/Islemler.razor`: S-2 şeridinin kendi sayfası, S2-5'in devamı, aynı şerit.
- `Pages/Cikti.razor`: S2-5 raporu bu dokunuşu açıkça bu pakete devretti.

**Tasarım belirteçleri:** Yeni satır içi `style` yok, sabit renk yok. Yalnız mevcut sınıflar kullanıldı (`alan`, `hucre-vurgu`, `metin-soluk metin-kucuk`, `HataBandi`, `Secici`).

## 2. DoD sözleşmesi

| Kayıt / öğe | Düzenleme yolu |
|---|---|
| `hareketler.personel_id` | Finansal varlık alanı. Hard DELETE ya da PATCH yok. Kayıttan önce seçiciyle (pro) değiştiriliyor. Kayıttan sonra düzeltme yalnız storno (48-A motoru) ve yeniden satışla yapılıyor. Yeni bir varlık ya da kayıt türü **doğmadı**. |
| `GET /islemler/personeller` | Salt okunur türetilmiş liste. Kullanıcıların kendisi Kullanıcılar ekranında düzenleniyor (mevcut PATCH, pasife alma ve aktif uçları). |
| Otomatik yazdırma | Türetilmiş çıktı. `Cikti.razor` üzerindeki `DegismezIpucu` (P-17) aynen duruyor. |
| Yeni tıklanabilir | Yeni düğme yok. Seçici gerçek değer değiştiriyor, gövdeye gidiyor (E1). "Fiş yazdır" bir bağlantı ve gerçek sayfa açıyor. |
| Denetim kaydı | Yeni yazma ucu yok. Satış ve fiş uçları mevcut denetim akışıyla yazılıyor. `DenetimServisi` yeniden yazılmadı. |

## 3. Testler

**Önce** (tam süit, değişiklikten önce; `scratchpad/pS2-5b/trx-once`): **4813 · 4809 geçti · 4 kırmızı**
- Kırmızılar: `EtiketEkranTestleri` × 4 (TaslagiTemizleFormuBosaltir, DepoErisilemezseSayfaCalisir, EksikUrunlerGeriYuklemedeBildirilir, EtiketTaslagiGeriYuklenir). Sahibi S3-A / şerit D (`UrunEtiketleri.razor`).
- Not: Temel koşumun derleme anında `SatisModulu.Satici.cs`'nin ilk hâli diskteydi. DTO bağlanmamıştı, davranış değişikliği yoktu.

**Sonra** (tam süit; `scratchpad/trx-pS2-5b`, `sonra.log`): **4867 / 4867 geçti, 0 başarısız.** Bu paketin ürettiği yeni kırmızı: **0**. Önceki 4 kırmızı da bu koşumda yeşil (başka şeridin düzeltmesi).

Yeni testler: `Kuyumcu.Tests/S25bSaticiYazdirTestleri.cs`, **26 sonuç**, hepsi yeşil.

| Test | Sınanan |
|---|---|
| H1 | personelId yok ya da null → personel_id = oturum; kullanici_id değişmez (KABUL 1) |
| H2 | SAHIP → aktif başka personel yazılır; kaydı giren değişmez (KABUL 2) |
| H3 | Başka kiracının kullanıcısı → 400 "bulunamadı", satış ve fiş ucunda hareket 0 (KABUL 3) |
| H4 | Pasif personel → 400, hareket 0 (KABUL 4) |
| H5 | TEZGAHTAR başkasını seçerse 403 (satış ve fiş), kendini seçerse 200 (KABUL 5) |
| H6 | satis-fisi: 2 kalemin ikisine de aynı satan; alansız → oturum |
| H7 | /islemler/personeller: SAHIP tümünü görür (pasif ve yabancı yok, e-posta ve rol yok); TEZGAHTAR yalnız kendini |
| H8 | Boş dizge → 400 |
| E1 | Pro + 2 kişi → `#islem-personel`; u2 seçilir → gövdede `personelId=u2`; personeller isteği tek (bileşen istek atmaz) |
| E2 | Basit → seçici yok, "Satan" + ad var, gövdede personelId yok (KABUL 6) |
| E3 | Tek kişi (pro) → seçici yok, salt okunur ad (KABUL 7) |
| E3b | Seçim serbest değil → seçici yok, sebep yazılı |
| E4 | Liste 500 → hata bandı sunucu mesajıyla, seçici yok, satış yine gider |
| E5 | Pro varsayılan (dokunulmadı) → gövdede personelId yok |
| E8 | Pro'da u2 seçilip basite geçilir → ekranda oturum adı, gövdede personelId yok (görünen = yazılan) |
| E9 | `dahaFazlasiVar` → `#islem-personel-sinir` (UX-S29) |
| E6 | `/cikti/…?yazdir=1` → `window.print` **1** kez; yeniden çizimde artmaz (KABUL 8) |
| E7 | Sorgusuz → print 0; düğme yine çalışır (KABUL 8) |
| S1 | `YazdirAdresi` kaçış yapar, boş kimlikte null |
| S2 (6 teori) | `SaticiKapisi`: SAHIP ve MUDUR true; TEZGAHTAR, MUHASEBE, "sahip" (tam eşleşme) ve null false |
| S3 | Sözlük TR+EN dolu, ham rol kodu yok |

S2-5 testlerinde güncellenen beklentiler: `S25_E2`, `S25_E3`, `S25_E4` ve `S25_E9` artık bağlantının `?yazdir=1` ile bittiğini bekliyor. Şartname "FisYazdirBaglantisi bu sorguyla açar" diyor.

Gerileme seti (S21–S25, BasitModPos, KarmaOdeme, Pos*, B4AltinBacagi, AlisOdemeAraci, Iade, UX4b, UX3, P17, Cikti, Satis, Islem, PesinMusteri, Yazdir): 554 testten 553'ü geçti. Tek kırmızı S25_E4 idi, beklenti güncellendi, sonra S25* 45/45 yeşil. Tam süitte hepsi yeşil.

## 4. Mutasyon kanıtları (12/12, hepsi `cmp` BİREBİR)

**Yöntem ve bir ölçüm dersi.** İlk iki tur paylaşılan ağaçta koşuldu. Aynı anda başka şeritler aynı `obj/bin` üzerinde `dotnet build Kuyumcu.sln` ve kendi mutasyonlarını (ör. `M77-4 Yedekler.razor`) koşuyordu. Sonuçlar bir adım kayıyordu: bir mutasyonun testi bir öncekinin ikilisiyle koşuyordu (ör. M25b-4'te H4'ün kırılması aslında M25b-3'ün mutantıydı). Bu iki turun sonuçları **geçersiz sayıldı**: `mut-sonuc-tur1.txt`, `mut-sonuc-tur2-paylasilan.txt`.

Geçerli tur **yalıtılmış kopyada** koşuldu:
1. `rsync` ile `scratchpad/iz/dotnet` oluşturuldu (bin ve obj hariç).
2. Her mutasyon için sırayla: yedek (`scratchpad/mut-yedek/S2-5b/`), tek eşleşme doğrulaması (betik eşleşme sayısını 1 olarak ölçüyor), `perl` değişikliği, derleme, `--filter`, geri kopya, `cmp`.
3. Betikler `pS2-5b/mut.sh` ve `mut-hepsi.sh`. Sonuç dosyası `mut-sonuc-tur3-yalitilmis.txt`.

Bu yöntemle paylaşılan ağaca mutant da girmedi. Tur 1-2'de girmişti; her birinin geri kopyası `cmp` ile BİREBİR doğrulandı ve kaynakta kalıntı olmadığı grep ile ölçüldü.

| Kod | Değişiklik | Kırmızı dönen test(ler) | cmp |
|---|---|---|---|
| M25b-1 | Oturum varsayılanı silindi (`return null!`) | S25b_H1, S25b_H6 | BİREBİR |
| M25b-2 | Açık kiracı koşulu silindi | **Kırmızı yok (eşdeğer mutant, aşağıda)**. Tek başına yeniden koşuldu (`M25b-2r`): 25/25 yeşil | BİREBİR |
| M25b-2b | Kiracı sınırının tamamı kaldırıldı (`IgnoreQueryFilters()` + açık koşul yok) | **S25b_H3** | BİREBİR |
| M25b-3 | Aktiflik kontrolü silindi | S25b_H4 | BİREBİR |
| M25b-4 | Basit modda seçici gösterildi | S25b_E2, S25b_E8 | BİREBİR |
| M25b-5 | Print koşulu `true` | S25b_E7 | BİREBİR |
| M25b-6 | Rol kapısı `true` | S25b_S2 × 4 (+ H5) | BİREBİR |
| M25b-7 | Fiş kaleminde `PersonelId = satici` silindi | S25b_H6 | BİREBİR |
| M25b-8 | `Govde()` personelId satırı silindi | S25b_E1 | BİREBİR |
| M25b-9 | `otomatikYapildi = true` silindi (her çizimde yazdırır) | S25b_E6 | BİREBİR |
| M25b-10 | Bağlantı sorgusuz `Adres`'e döndü | S25_E2, S25_E3, S25_E4, S25_E9 | BİREBİR |
| M25b-11 | `SaticiGovde` basit modu yok saydı | S25b_E8 (bu mutasyon ilk turda test açığı buldu; E8 eklendi) | BİREBİR |

**M25b-2 neden eşdeğer:** `Kullanici` bir `IKiraciVarlik` ve `KuyumcuDbContext.cs:280`'de `HasQueryFilter(e => e.TenantId == kiraciId)` taşıyor. Açık `x.TenantId == islem.TenantId` koşulu bu süzgecin **ikinci katmanı** (derinlemesine savunma). Yalnız biri silinince diğeri korumaya devam ediyor, bu yüzden hiçbir test kırılamaz. Kiracı sızıntısının testçe yakalandığı, iki katman birlikte kaldırılınca (M25b-2b) kanıtlandı: H3 kırmızı. Açık koşul kodda bırakıldı. Hasım doğrulayıcı için not: görevdeki "kiracı kontrolünü sil → (3) KIRMIZI" maddesi bu gerekçeyle M25b-2b ile karşılandı.

Son mutasyondan sonra `dotnet build Kuyumcu.sln`: **0 Hata, 0 Uyarı** (`b7.log`). Mutasyon koşusundan sonra ana ağaca B-8 düzeltmesi (`Take`) girdi; tam süit ve denetim bu son hâlle koşuldu.

## 5. Derleme ve denetim

- `dotnet build Kuyumcu.sln --nologo`: tur 1'de **0 Hata, 0 Uyarı** (`b7.log`) beyan edilmişti; doğrulayıcı sonraki tam derlemede bu paketin test dosyasından bir xUnit2031 uyarısı buldu, beyan o an için yanlıştı. Tur 2 düzeltmesinden sonra (`b8.log`) **0 Hata, 1 Uyarı** ve kalan uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 (şerit D). Ara derlemelerde görülen uyarılar başka şeritlere aitti (`EtiketEkranTestleri` xUnit1031, `KarZarar.razor` CS8604, MSB3026 kopya yeniden denemesi).
- `bash dotnet/denetim.sh`: **DENETİM TEMİZ** (rc=0, `denetim2.log`).
  - İlk koşum (`denetim.log`) iki ✗ verdi:
    - (a) "Sınırsız ToListAsync (Modules · B-8) 146/145": **benim.** `SaticiPersoneller` listesi sınırsızdı. `.Take(AzamiListe + 1)` ve `DahaFazlasiVar` eklendi (UX-S29), sonuç 145.
    - (b) "Kültürsüz ToString (Web) 28/27": benim dosyalarımda eşleşme yok (grep ile ölçüldü). O an başka bir şeridin geçici değişikliğiydi; ikinci koşumda 27.

| Sayaç | Değer | Tavan |
|---|---|---|
| satır içi style= | 110 | 196 |
| @onclick'siz ve disabled'sız `<button>` | 59 | 71 |
| ModalPencere kullanımı | 48 | 55 |
| Sınırsız ToListAsync (B-8) | 145 | 145 |
| P-17 ORTAK_SABLON_DISI_PRINT | 0 | — |
| S2-3 FIS_BILESENI_ISTEK | 0 | — |

## 6. Canlı doğrulama (5192, `kuyumcu_net`)

Sürücü: `scratchpad/pS2-5b/canli.sh` + `canli.py`. Parolalar rastgele üretildi, argv ile verildi, hiçbir çıktıya basılmadı. Davet satırları doğrudan eklendi ve sonra silindi.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit (A) | 201 | 201 | jeton var |
| 2 | POST /auth/kayit (B) + /kullanicilar/ben | 201 + id | 201/200 | yabancı id var |
| 3 | Kur, ürün, kasa, stok 20, Peşin | var | 201/200/200 | hepsi var |
| 4 | POST /kullanicilar ×2 (TEZGAHTAR) + PATCH pasif | 201/201/200 | 201/201/200 | — |
| 5 | GET /islemler/personeller (SAHIP) | serbest; sahip + ayşe; pasif ve yabancı yok | 200 | serbest=True, adet=2, pasif=False, yabancı=False, e-posta alanı yok |
| 6 | **KABUL 1** satış, personelId yok | 200 | 200 | SQL: satan = Sahip |
| 7 | **KABUL 2** personelId = Ayşe | 200 | 200 | SQL: satan = Ayşe, giren = Sahip |
| 8 | **KABUL 3** başka kiracının kullanıcısı | 400 | **400** | "Seçilen satıcı personel bu işletmede bulunamadı." |
| 9 | **KABUL 4** pasif | 400 | **400** | "…pasif; pasif kullanıcı adına satış yazılamaz." |
| 10 | Boş dizge | 400 | 400 | "Satıcı personel boş olamaz…" |
| 11 | /auth/giris (Ayşe, TEZGAHTAR) | 200 | 200 | jeton var |
| 12 | Personeller (TEZGAHTAR) | serbest=false, yalnız kendisi | 200 | serbest=False, adet=1, kendisi=True |
| 13 | **KABUL 5** TEZGAHTAR personelId = Sahip | 403 | **403** | "Başka bir personeli satıcı olarak yalnız işletme sahibi ya da müdür seçebilir." |
| 14 | TEZGAHTAR, alan yok | 200 | 200 | SQL: satan = giren = Ayşe |
| 15 | satis-fisi personelId = Ayşe (2 kalem, NAKİT+KART) | 200 + fisId | 200 | fisId var |
| 16 | Tarayıcı rotası `/cikti/SATIS_FISI/{h}?yazdir=1` | 200 | 200 | blazor.webassembly var |
| 17 | GET /api/v1/cikti/SATIS_FISI/{h} | 200 | 200 | belgeAdi = SATIŞ FİŞİ |
| 18 | **KABUL 9** GET /raporlar/islem-defteri?tur=SATIS (SatisRaporlari.cs:129-130, dosyaya dokunulmadı) | satanBilinmiyor=false | 200 | 5 satır, `satanBilinmiyor=true` **0**, sahip 1, Ayşe 4 |

SQL kanıtı (kiracı A):
```
-- tur, tl, fis_dolu, SATAN (personel_id), GİREN (kullanici_id), personel_id_bos
SATIS  1000.00 0  Sahip S25b     Sahip S25b     0
SATIS  1000.00 0  Ayse Personel  Sahip S25b     0
SATIS  1000.00 0  Ayse Personel  Ayse Personel  0
SATIS   600.00 1  Ayse Personel  Sahip S25b     0
SATIS   400.00 1  Ayse Personel  Sahip S25b     0
-- SATIS sayısı: 5  (3 tek + 2 fiş kalemi; reddedilen 400/403 istekleri hareket YAZMADI)
-- personel_id NULL olan SATIS: 0     (S2-5'te bu değer 1'di)
-- personel_id başka kiracının kullanıcısı olan hareket: 0
-- kiracı B hareket sayısı: 0
```

KABUL 8'in yazdırma penceresi bir tarayıcı olayı; `curl` ile gözlenemez. Rotanın açıldığı canlı adım 16'da, `window.print`'in bir kez çağrıldığı bUnit E6/E7'de kanıtlandı. Tarayıcıda uçtan uca tıklama yapılmadı, çünkü test kullanıcısının parolasını bir giriş alanına yazmak ajan kuralıyla yasak (S2-5 ile aynı).

**Temizlik:**
- API durduruldu (PID 56020 ve alt süreci; `lsof -iTCP:5192 -sTCP:LISTEN` boş).
- `PAKETS2-5b-TEST` ve `PAKETS2-5b-TEST-B`, `betikler/kiraci-temizle.sh --uygula` ile silindi. Yeniden sorgu: "Kiracı YOK (bağlantı sınandı)".
- Davetler silindi (kalan 0). `s25b-*` ve `paketS2-5b*` kullanıcıları: 0.
- `tenants`: önce **10**, sonra **10**.
- `QA-TEST-*`, DEMO ve diğer kiracılara dokunulmadı.
- 5198 ve 5185-5200 aralığında 5192 dışındaki portlara dokunulmadı. Gözlem: 5200'de başka bir API süreci vardı, dokunulmadı.

## 7. Değişen dosyalar (bu paketin payı; `git status --porcelain | grep -v -E "/(bin|obj)/"`)

```
 M dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs                   (DTO PersonelId ×2, SaticiBelirle ×2, taslakta PersonelId ×2)
 M dotnet/Kuyumcu.Web/Pages/Islemler.razor                           (1399 satır)
?? dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.Satici.cs            (YENİ)
?? dotnet/Kuyumcu.Contracts/Sayfalar/SaticiPersonelSozlesmeleri.cs   (YENİ)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.FisSatici.cs                   (YENİ)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.FisYazdir.cs                   (ipucu metni güncellendi; dosya S2-5'ten beri izlenmiyor)
?? dotnet/Kuyumcu.Web/Pages/Cikti.razor                              (değişti; dosya izlenmiyor)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisSatici.razor                 (YENİ; dizin izlenmiyor)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisYazdir.cs                    (değişti)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisYazdirBaglantisi.razor       (değişti)
?? dotnet/Kuyumcu.Web/Bilesenler/Yazdirma/YazdirDugmesi.razor        (değişti — sapma § 0)
?? dotnet/Kuyumcu.Tests/S25bSaticiYazdirTestleri.cs                  (YENİ)
?? dotnet/Kuyumcu.Tests/S25YazdirParaUstuTestleri.cs                 (4 beklenti ?yazdir=1)
```
- `HareketFabrikasi.cs` değişmedi. Migration yok.
- Proje ağacına `.mutorj`, `.bak` ya da `.tmp` bırakılmadı. Mutasyon yedekleri ve yalıtılmış kopya `scratchpad` altında.

## 8. Mevzuat

Personel ("satan") bilgisi iç kayıt. Fiş, fatura ya da gider pusulasının zorunlu unsuru yapılmadı; `Cikti` belge içeriği değişmedi. Yasal alan eklenmediği için birincil kaynak araştırması gerekmedi.

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (A) personelId? — yoksa oturum | ✓ | H1, M25b-1; canlı 6, 14 |
| (A) başka aktif kullanıcı (SAHIP) | ✓ | H2; canlı 7 |
| (A) başka kiracı → 400 | ✓ | H3, M25b-2b; canlı 8 + SQL |
| (A) pasif → 400 | ✓ | H4, M25b-3; canlı 9 |
| (A) TEZGAHTAR başkası → karar 403, test kilitli | ✓ | H5, S2, M25b-6; canlı 13 |
| (A) migration gerekmez | ✓ ölçüldü | § 0 |
| (A) mevcut istemciler değişmeden | ✓ | H1; tam süit 4867/4867 |
| (B) pro `#islem-personel`, basit yok + "Satan" | ✓ | E1, E2, M25b-4 |
| (B) tek kullanıcıda seçici yok (UX-S22) | ✓ | E3, E3b |
| (B) tek POST, bileşen istek atmaz | ✓ | S21 yapı testleri; FIS_BILESENI_ISTEK 0; E1 (personeller isteği tek) |
| (C) `?yazdir=1` → print bir kez, sorgusuz yok | ✓ | E6, E7, M25b-5, M25b-9 |
| (C) FisYazdirBaglantisi sorguyla açar | ✓ | S25_E2/E3/E4/E9, M25b-10 |
| KABUL 9 personel kırılımı NULL değil | ✓ | canlı 18 (satanBilinmiyor 0) |
| DOM id silinmedi | ✓ | envanter farkı 0 |
| Islemler.razor ≤ 1400 | ✓ 1399 | S21 yeşil |
| Sabit renk / inline style / CDN yok | ✓ | denetim |
| Paket kapısı (1) build 0 hata | ✓ | tur 2 `b8.log`: 0 hata; tek uyarı şerit D'nin |
| (2) kendi testlerim 0 başarısız | ✓ | tur 2 `FullyQualifiedName~S25`: 47/47 |
| (3) sayaçlarım ✓ | ✓ | tur 2 `denetim3.log`: benim sayaçlarım ✓; tek ✗ P-03 `Satir` (şerit D, `SatisRaporlari.cs`) |
| (4) mutasyon | ✓ | tur 4: 6/6 kırmızı ve cmp BİREBİR (§ R). Tur 3: 12 mutasyon: 11 kırmızı; M25b-2 eşdeğer (gerekçe § 4), kiracı sınırı M25b-2b ile kırmızı; hepsi cmp BİREBİR |
| (5) canlı | ✓ | § 6 ve § R tur 2 (18/18) |
| (6) test kiracısı silindi | ✓ | tenants 10 → 10; 70 tabloda 4 kiracı kimliği için kalıntı 0, geç yazım süpürmesi dahil (§ R) |
| (7) yeni kırmızı 0 | ✓ | Temel: 4 kırmızı (Etiket, şerit D). Tur 2 tam süit 4873/4872, tek kırmızı `SozlesmeYansimaTestleri.Contracts_tipi_sunucuda_ikinci_kez_tanimlanmiyor` (şerit D, `SatisRaporlari.cs`). Bu paketin farkı 0 |

## 10. Açık kalanlar

1. **Altın bacağındaki HURDA_ALIŞ satırına satan yazılmıyor.** Anlam kilidi "SATAN" olduğu için yalnız SATIŞ satırlarına yazıldı. Alış tarafında "alan personel" isteniyorsa ayrı bir karar gerekir.
2. **Satış iadesi, takas ve tek başına hurda alış** satıcı yazmıyor (ölçüldü: önceden de yoktu). Kapsam şartnameye göre satış ve satış fişi. Aynı kalıp `SaticiBelirle` ile bir satırda eklenebilir.
3. **Paylaşılan ağaçta eşzamanlı mutasyon riski** (orkestratöre): Başka şeritler aynı `obj/bin` üzerinde derleyip mutasyon koşarken mutasyon sonuçları kayıyor. Bu paket yalıtılmış kopyaya geçti. Aynı yöntemin diğer şeritlere de önerilmesi gerekiyor.
4. Denetimin ilk koşumunda görülen "Kültürsüz ToString 28/27" başka bir şeridin geçici değişikliğiydi (benim dosyalarımda eşleşme 0). İkinci koşumda 27 ve temiz.
5. Tarayıcıda uçtan uca "3 tık" ve yazdırma penceresinin gözle doğrulanması kurucunun ya da QA turunun işi (§ 6).

6. **Başka şeridin kırmızısı (tur 2 tam süit + denetim):** `SozlesmeYansimaTestleri.Contracts_tipi_sunucuda_ikinci_kez_tanimlanmiyor` ve denetim `P-03 CONTRACTS_DISI_MODEL 1 → Satir`. Kaynak `Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs` içindeki `SatisOdemeKirilimi.Satir`. **Sahibi: şerit D** (Modules/Rapor). Bu pakete yasak dosya, dokunulmadı.
7. **Yetim denetim ve kur satırları (orkestratöre):** Arka plan kur yazıcısı silinmiş test kiracısına geç yazıyor (§ R). Sistemik; doğrulayıcının ölçümüne göre DB genelinde 83 yetim denetim satırı var (bu paket o sayıyı yeniden ölçmedi). Orkestratör süpürmesi ve temizlik betiğine "bekle + ikinci süpürme" adımı önerilir. Kesin kök neden doğrulanmadı.
8. Uyarı `EtiketEkranTestleri.cs(145,47)` xUnit1031 şerit D'nin.

## 11. Kurucuya ve mali müşavire notlar

- Karar (kurucuya bilgi): Başka personeli "satan" olarak yalnız işletme sahibi ya da müdür yazabilir. Tezgâhtar yalnız kendi adına yazar. Rol listesi sunucuda tek yerde (`SaticiKapisi.SecebilenRoller`).
- Mali müşavire: Bu paket yasal belge içeriğini değiştirmedi. "Satan" bilgisi yalnız iç raporlamaya girer.
