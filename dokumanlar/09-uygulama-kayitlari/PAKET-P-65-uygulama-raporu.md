# PAKET P-65 — Onay ve kilit standardı: yıkıcı eylemde onay, devre dışı düğmede gerekçe

**Şerit:** SERİT F (tur 8 triyajı) · VT `kuyumcu_test_f` · canlı port **5183**
**Rol:** Taşkın (backend) + Ertuğ (frontend) · **Tarih:** 2026-09-24 06:25
**Kaynak:** `TRIYAJ-TUR8-2026-09-23.md` P-65 = görsel E49 (onay/kilit standardı) + UX-1 B48 (DegismezIpucu yaygınlaştırma)
**Durum beyanı:** **BİTTİ** — paket kapısının 7 maddesi sağlandı (§ 11).

---

## 1. Özet

- **Yeni ortak bileşen `Bilesenler/OnayKipi.razor`.** `ModalPencere`'yi sarar, onu kopyalamaz. Sözleşmesi:
  1. Başlık eylemin adıdır ("Fiyat listesini pasife al").
  2. Onay düğmesi eylemin fiilini taşır, vazgeç düğmesi "Vazgeç" der. Çağıran iki düğmeye aynı fiili verirse (/yedekler'deki "İptal" ile "İptal et" gibi), bileşen vazgeç metnini yok sayar ve "Vazgeç" basar.
  3. Geri dönülemez eylemde kullanıcı onay kelimesini yazar (`GeriDonulemez` + `OnayKelimesi`). Kelime yazılana kadar onay düğmesi kapalı kalır ve kilidin sebebi ekranda yazar.
  4. Geri dönüş yolu (`GeriDonusYolu`) ve sunucu hatası (`Hata`) pencerenin içinde gösterilir.
- **Yıkıcı eylemler onay kipine bağlandı:**
  - `/vitrin` — varsayılana dön. Satırdan da çekmeceden de bu pencere açılır. Eskiden çekmecedeki düğme onay sormadan POST atıyordu.
  - `/fiyatlandirma` — liste ve kampanya pasife alma. Aktifleştirme yıkıcı değil, eskisi gibi onaysız çalışır.
  - `/kullanicilar` — erişim kapatma. Eski pencerenin başlığı "Pasife al", vazgeç düğmesi "İptal" idi. Bu eylem artık OnayKipi'den geçiyor.
- **Devre dışı düğmelere gerekçe eklendi:**
  - `/kur` — ~100 kilitli "İptal et" düğmesi sebebini `title` ile yazar. Görünür ipucuna `aria-describedby` ile bağlanır.
  - `/emanet` — "+ Emanet Al" ve "İade Et" düğmeleri `title` taşır. Sebep ayrıca görünür satırda (`#e-kilit`) yazar. Satırdaki "Geri al" düğmesine ve iade Kaydet düğmesine de `title` eklendi.
  - `/evraklar` — Kaydet kilidi eksik alanların adını tek tek sayar.
- **DegismezIpucu ile gerekçe ve düzeltme yolu (dört ekran):**
  - /kullanicilar — kendi rol kilidi "Sahipliği Devret" yolunu düğmenin adıyla söyler.
  - /evraklar — kayıtlı evrakın tür kilidi gerekçesini ve düzeltme yolunu yazar.
  - /bildirimler — kurulumda kapalı hatırlatmanın nasıl açılacağı basit ve profesyonel yüzde yazılı.
  - /kur — elle girilen kur kaydı yalnız gerekçeli iptal edilebilir; doğru değer "İptal et" ve ardından "Manuel giriş" ile girilir.
- **Kilit ve davranış:** yeni kilit eklenmedi, kilit koşulları değişmedi. Yalnız açıklama eklendi; tek fark, yıkıcı eylemlerin önüne bir onay adımı girmesi.
- **Metinler:** hepsi yeni `Kuyumcu.Core/I18n/Sozlukler.OnayKilit.cs` dosyasında (TR+EN, konumsal record; eksik anahtar derleme hatası verir). **`Sozlukler.cs` HUB dosyasına DOKUNULMADI.**
- **Sunucu:** API, veri modeli ve migration değişmedi. Bu paket yalnız istemci tarafında çalışır.

## 2. Spesifikasyonun koda karşı doğrulanması ve sapmalar

| Spesifikasyon iddiası | Kodda bulunan | Karar |
|---|---|---|
| /vitrin'de yıkıcı eylem onaysız | Satır akışında P-67'nin satır içi onayı vardı. Çekmecedeki `#pano-varsayilana-don` ise **onaysız** POST atıyordu (P-17 davranışı). | İkisi de OnayKipi'ye bağlandı. P-67 kimlikleri (`vitrin-varsayilana-don-evet/vazgec/soru-{Kod}`) OnayKipi'nin `OnayId/VazgecId/AciklamaId` parametreleriyle **korundu**. |
| /fiyatlandirma'da pasife alma onaysız | Doğru: `fl-durum-*` ve `km-durum-*` düğmeleri doğrudan PATCH atıyordu. | Pasife alma onaya gider. Aktifleştirme (geri dönüş yolu) onaysız kaldı. |
| /kullanicilar'da yıkıcı onay | Ortak bir ModalPencere vardı; gövdesi `pasifHedef` ile değişiyordu. Başlık "Pasife Al", vazgeç "İptal" idi. | Erişim kapatma gövdesi OnayKipi'ye taşındı ve `#k-pasife-onayla` kimliği korundu. Sahiplik devri gövdesi (şifre alanı içerdiği için) ModalPencere'de kaldı. |
| /kur'da 100 kilitli "İptal et" | Doğru: `disabled` vardı ama `title` yoktu. İpucu tablonun altındaydı ve düğmeye bağlı değildi. | `title` ve `aria-describedby="degismez-ipucu-KurKaydi"` eklendi. |
| /evraklar evrak türü kilidi | Seçicide `DevredisiSebebi=TurDegismez` vardı. Metin "iptal edip yenisini açın" diyordu, ama **sunucuda evrak iptal ucu YOK** (EvrakModulu: tahsile-ver, portfoye-al, tahsil-edildi, ciro, rucu, karsiliksiz, PATCH). | DegismezIpucu eklendi. Düzeltme yolu olarak `Dil.T.Evrak.DegismezYol` kullanıldı: karşılıksız işaretle ya da giriş hareketini storno et, sonra yeniden gir. Bu, sunucuda gerçekten var olan yoldur. Eski `TurDegismez` metni HUB'da olduğu için değiştirilmedi → **açık kalan** (§ 12). |
| /bildirimler kapalı iş | Basit yüzde düğme `title` taşıyordu ve kilit satırı vardı. Profesyonel yüzdeki kutunun kilit sebebi etiketin kendisini tekrar ediyordu (`DevredisiSebebi=IsAcikKapali`). | İki yüze de DegismezIpucu (açma yolu) eklendi. Profesyonel kutunun sebebi gerçek gerekçe oldu (`YapilandirmaKapaliIpucu`). |
| Test adı `KurEkranTestleri.*` | `KurEkranTestleri` sınıfı zaten `EmanetKurEkranTestleri.cs` içinde vardı; ilk derleme CS0101 verdi. | Mevcut sınıf `partial` yapıldı (tek sözcük). Yeni testler `KurEkranTestleri.cs` dosyasında aynı fikstürü kullanıyor; ikinci fikstür yazılmadı. |
| `ModalPencere.razor` / `DegismezIpucu.razor` değişikliği | Gerek çıkmadı. OnayKipi ModalPencere'yi sarıyor, DegismezIpucu da olduğu gibi yeterli. | **Dokunulmadı.** 55 kullanan ekran etkilenmedi. |
| Geri dönülemez eylemde yazarak onaylama | Bağlanan üç ekrandaki eylemlerin hepsinin geri dönüş yolu var: aktifleştir, aktif et, varsayılandan yeniden düzenle. | Yazarak onaylama bileşende hazır ve test edildi, ama bu üç eylemde açılmadı. Açılsaydı geri dönüşü olan eylemleri gereksiz yere zorlaştırırdı. |

**Mevzuat:** Bu paket mevzuat hükmü içermiyor; vergi, KDV ya da MASAK kuralı yazılmadı ve değiştirilmedi.

## 3. DoD sözleşmesi (Bertan koşulu)

Paket yeni bir varlık ya da kayıt türü **doğurmadı**. Sunucu tarafında değişiklik yok.

Onay kipine bağlanan eylemlerin geri dönüş yolları:

| Varlık | Yıkıcı eylem (onaylı) | Geri dönüş yolu (ekranda yazılı) | Uç |
|---|---|---|---|
| Fiyat listesi | Pasife al | Aynı satırda "Aktifleştir" | `PATCH /fiyat/listeler/{id}` (SAHIP, MUDUR) |
| Kampanya | Pasife al | Aynı satırda "Aktifleştir" | `PATCH /fiyat/kampanyalar/{id}` (SAHIP, MUDUR) |
| Kullanıcı | Erişimi kapat (soft) | Aynı satırda "Aktif Et" | `DELETE /kullanicilar/{id}` · `PATCH /kullanicilar/{id}/aktif` |
| Pano kalem ayarı | Varsayılana dön | "Düzenle" çekmecesinden yeniden girilir | `POST /pano/ayarlar/{kod}/varsayilana-don` (SAHIP, MUDUR) |

**Salt okunur kalanlar ve gerekçeleri:**
- Dış kaynaklı kur kaydı: düzeltilmez, "Şimdi çek" ile yenisi alınır.
- Manuel kur kaydı: düzenlenmez, yalnız gerekçeli iptal edilir.
- Kayıtlı evrakın türü: karşılıksız ya da storno, ardından yeniden giriş.
- Kurulumda kapalı hatırlatma: sunucu ayarından açılır.
- Kendi rolünüz: başka bir SAHİP değiştirir ya da "Sahipliği Devret" kullanılır.

Bunların hepsi ekranda DegismezIpucu ile yazılı. Finansal varlıklarda hard delete yok, bu pakette silme ucu da yok.

## 4. Dokunulan sayfalar (DoD EK 2 — tek dokunuş)

`Pages/Vitrin.razor` · `Pages/Fiyatlandirma.razor` · `Pages/Kullanicilar.razor` · `Pages/Kur.razor` · `Pages/Emanet.razor` · `Pages/Evraklar.razor` · `Pages/Bildirimler.razor`

Bu sayfalar daha önce başka paketlerce açılmıştı (P-67 Vitrin, P-17, S3-5, P-09, P-11 …). Yeniden açılmalarının gerekçesi: tur 8 triyajı P-65'i doğrudan bu yedi sayfaya atadı; bu, çakışma haritası § 3.3'teki sıraya uygun bir yeniden açılıştır.

## 5. Migration

**Gerekmedi.** Şema değişikliği yok.

## 6. Derleme

- `dotnet build Kuyumcu.sln --nologo` (asıl ağaç, son temiz derleme): **0 Hata · 1 Uyarı**.
- Tek uyarı bu paketin değil: `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031` (P-66 dosyası).
- Anlık kopyadaki derlemede de 1 uyarı vardı, tam süit koşumunun kendi derlemesinde 0 uyarı. Aynı P-66 uyarısı.

## 7. Testler

### 7.1 Yeni testler: 17

- **`Kuyumcu.Tests/OnayKipiTestleri.cs`** (YENİ, 7 test):
  - `YikiciEylem_OnaysizCalismaz` *(spesifikasyon)*
  - `YikiciEylem_Satirdan_da_onay_kipinden_gecer_Vazgec_istek_atmaz`
  - `YikiciEylem_Fiyatlandirma_pasife_alma_onaysiz_PATCH_atmaz`
  - `Aktiflestirme_yikici_degildir_onaysiz_calisir_davranis_degismedi`
  - `YikiciEylem_Kullanici_erisim_kapatma_onaysiz_DELETE_atmaz`
  - `OnayVeVazgecDugmeleri_AyniFiiliTasimaz` *(spesifikasyon)*
  - `GeriDonulemez_eylemde_kelime_yazilmadan_onay_ACILMAZ_sebebi_yazili`
- **`Kuyumcu.Tests/KilitGerekceTestleri.cs`** (YENİ, 6 test):
  - `KullaniciKendiRolKilidiSahipligiDevretSoyler` *(spesifikasyon)*
  - `EvrakTuruKilidiGerekceGosterir` *(spesifikasyon)*
  - `BildirimKapaliIsAcmaYoluYazili` *(spesifikasyon)*
  - `BildirimKapaliIsAcmaYolu_profesyonel_yuzde_de_yazili`
  - `ManuelKurKaydiIptalYoluYazili` *(spesifikasyon)*
  - `Emanet_kilitli_dugmeler_sebebini_yazar`
- **`Kuyumcu.Tests/KurEkranTestleri.cs`** (YENİ dosya, mevcut `KurEkranTestleri` sınıfının `partial` parçası, 3 test):
  - `DevreDisiIptalDugmesi_GerekceMetniTasir` *(spesifikasyon)*
  - `Kur_ekraninda_devre_disi_HER_dugme_bos_olmayan_gerekce_tasir`
  - `Iptal_onayinda_vazgec_ve_onay_ayni_fiili_tasimaz`
- **`Kuyumcu.Tests/EvrakEkranTestleri.cs`** (+1 test): `KaydetKilidi_EksikAlanlariListeler` *(spesifikasyon)*

Spesifikasyon listesindeki 8 testin **8'i de** yazıldı ve yeşil.

**Sahte API:** `{}` varsayılanı kullanılmadı. Ortak `P65SahteApi` tanımadığı uca **404** döner. Her uç sunucu sözleşmesindeki alan adlarıyla yanıt verir. JSON gövdeleri metin araması yerine **ayrıştırılarak** sınanır (`JsonDocument…GetProperty("aktif")`).

### 7.2 Davranış değişikliği nedeniyle güncellenen 3 mevcut test

Üç test yıkıcı eylemin artık onay adımından geçmesi yüzünden güncellendi. Hepsi bu şeridin sayfalarının test dosyasında. Değişiklik yalnız onay tıklamasını eklemek; beklenen istek ve gövde denetimleri aynı kaldı.

- `P17EkranTestleri.Ozel_kalemde_varsayilana_don_ACIK_ve_istek_atar`: önce istek gitmediği doğrulanıyor, sonra `#vitrin-varsayilana-don-evet-GRAM` tıklanıyor.
- `FiyatlandirmaUX4aEkranTestleri.Pasife_al_TAM_govdeyle_PATCH_atar`: `#fl-pasif-onayla` tıklaması eklendi.
- `UX1C2EkranTestleri.Fiyatlandirma_pasife_alinca_KENDI_mesajini_yazar`: `#fl-pasif-onayla` tıklaması eklendi.
- Ayrıca `EmanetKurEkranTestleri.cs` dosyasında `KurEkranTestleri` sınıfına `partial` sözcüğü eklendi (§ 2).

### 7.3 Koşumlar

| Kapsam | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Filtreli: OnayKipi, KilitGerekce, KurEkran, EvrakEkran | 47 | 47 | 0 |
| Filtreli: 7 sayfanın ve ModalPencere/DegismezIpucu kullanan tüm test sınıfları (47 sınıf, grep ile listelendi) | 548 | 548 | 0 |
| **Tam süit** — 06:15 anlık kopyası (`scratchpad/pP-65/kopya`, `bin/obj/.git` hariç) | **4946** | **4946** | **0** |

**Neden anlık kopya:** Asıl ağaçta tam süit iki kez paralel derleme tuzağına düştü:
- Başka bir şeridin temiz derlemesi `Kuyumcu.Tests/bin` içindeki `Kuyumcu.Api.deps.json` dosyasını koşum sırasında sildi ve HTTP testleri "Can't find … deps.json" hatası verdi. Bu koşum **kanıt sayılmadı**.
- Bunun üzerine B-4 yöntemiyle ağacın anlık kopyası alındı, derlendi ve koşuldu.

Tam süit kanıtı: trx `scratchpad/trx-pP-65/_Mac_2026-09-24_06_17_13.trx` → `total=4946 passed=4946 failed=0`.

**Madde (7) — yeni kırmızı yok:** Tam süitte **0 kırmızı**, dolayısıyla devir listesiyle fark 0. Bu paket yeni kırmızı test üretmedi.

**Test sayısı önce/sonra:** Paket başında tam süit ölçülmedi, çünkü ağaç şeritlerle eşzamanlı değişiyor. Sonra **4946** ölçüldü. Bu paketin katkısı **+17** test.

## 8. Mutasyon kanıtları

**Yöntem:**
1. Kaynak dosya `scratchpad/mut-yedek/P-65/` altına kopyalandı.
2. `perl -0777 -pi` ile tek eşleşmeli bir değişiklik yapıldı (her maddede diff 2 satır).
3. Derleme ve filtreli `dotnet test -v n` koşuldu.
4. Yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu gösterildi.

**Betikler ve kayıtlar:**
- Betikler: `scratchpad/mut-p65.sh` (M65-1…7) ve `mut-p65c.sh` (M65-8…10, yeniden deneme).
- Kayıtlar: `mut-p65.log`, `mut-p65c.log`, `mut-M65-*-{build,test}.txt`.

**Paralel derleme tuzağı (ölçüldü):**
- M65-8, M65-9 ve M65-10'un ilk iki koşumunda derleme başka şeritlerle çakıştı. Görülen hatalar: `Kuyumcu.Contracts … ref dll bulunamadı`, `ConvertDllsToWebCil` G/Ç hatası ve `Kuyumcu.Web.dll bulunamadı`. Build başarısız olduğu için bu koşumlar **KANIT SAYILMADI**.
- Üçüncü denemede artımlı derleme **başarılı** oldu (`build=0`).
- Kırmızıya dönen test, mutasyonlu kodun derlendiğinin doğrudan kanıtıdır.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı (cmp) |
|---|---|---|---|
| M65-1 *(spes.)* | `Vitrin.razor`: çekmece düğmesi `OnayIste(d)` → `VarsayilanaDon(d)` (onay kipinden çıkar) | `OnayKipiTestleri.YikiciEylem_OnaysizCalismaz` | BİREBİR |
| M65-2 *(spes.)* | `Kur.razor`: kilitli İptal düğmesinin `title`'ı boşaltıldı | `KurEkranTestleri.DevreDisiIptalDugmesi_GerekceMetniTasir` | BİREBİR |
| M65-3 *(spes.)* | `Kullanicilar.razor`: kilit metni eski `KendiRolunYolu` hâline döndürüldü | `KilitGerekceTestleri.KullaniciKendiRolKilidiSahipligiDevretSoyler` | BİREBİR |
| M65-4 *(spes.)* | `Evraklar.razor`: `DegismezIpucu Varlik="EvrakTuru"` kaldırıldı | `KilitGerekceTestleri.EvrakTuruKilidiGerekceGosterir` | BİREBİR |
| M65-5 | `OnayKipi.razor`: aynı-fiil koruması (`AyniFiil`) kaldırıldı | `OnayKipiTestleri.OnayVeVazgecDugmeleri_AyniFiiliTasimaz` | BİREBİR |
| M65-6 | `Evraklar.razor`: kilit sebebi eski "Bu alan zorunludur." metnine döndü | `EvrakEkranTestleri.KaydetKilidi_EksikAlanlariListeler` | BİREBİR |
| M65-7 | `Bildirimler.razor`: açma yolu eski metne döndü | `KilitGerekceTestleri.BildirimKapaliIsAcmaYoluYazili` | BİREBİR |
| M65-8 | `Kur.razor`: manuel kur ipucu yolu dış kaynak yoluna çevrildi | `KilitGerekceTestleri.ManuelKurKaydiIptalYoluYazili` | BİREBİR |
| M65-9 | `Fiyatlandirma.razor`: liste düğmesi `ListeDurumIste` → `ListeDurumDegistir` (onaysız) | `OnayKipiTestleri.YikiciEylem_Fiyatlandirma_pasife_alma_onaysiz_PATCH_atmaz` | BİREBİR |
| M65-10 | `Emanet.razor`: "+ Emanet Al" `title`'ı silindi | `KilitGerekceTestleri.Emanet_kilitli_dugmeler_sebebini_yazar` | BİREBİR |

**Sonuç:**
- **10/10 mutasyon ADI yazılı testi kırdı.** Spesifikasyonun 4 mutasyonu 4/4 kırmızı.
- Test açığı bulunmadı.
- Son mutasyondan sonra temiz derleme alındı: 0 hata.

## 9. denetim.sh

- **Başlangıç (asıl ağaç):** `DENETİM TEMİZ.`
- **Ara koşum:** 3 ✗ vardı: `record tanımı 4 > 3`, `P-10 BU_PAKETTE_EKLENEN_RECORD`, `P-02 OLCUM_TABANI_KOPYA`. Üçünün de kaynağı `Bilesenler/SatirEylemleri.razor:97 public sealed record Eylem(`, yani **SERİT G'nin** dosyası. Bu dosyaya dokunulmadı.
- **Kendi ✗'im (düzeltildi):** OnayKipi'deki ham girdi etiketi `P-36 HAM_INPUT 43 > 42` ve `HAM_INPUT_SERBEST 1` üretti. Ortak `<Metin>` Form bileşenine geçildi ve sayaç 42'ye döndü.
- **Son koşum (asıl ağaç):** `DENETİM TEMİZ.` (SERİT G'nin record'u da taşınmış görünüyor).

Bu paketin sayaçları:

| Sayaç | Değer |
|---|---|
| Pages: ModalPencere kullanımı | 48 (tavan 55) — **artmadı** |
| P-13 GEREKCESIZ_KILITLI_ALAN | 0 |
| P-36 HAM_INPUT | 42 (tavan 42) |
| P-36c HAM_INPUT_SERBEST | 0 |
| P-36c ODAK_TUZAGI | 0 |
| P-36c PENCERE_DUGME_SIRASI_TERS | 0 (OnayKipi: ikincil önce, eylem sonda) |
| @onclick'siz ve disabled'sız düğme | 53 (tavan 71) |

## 10. Canlı doğrulama (port 5183, `kuyumcu_net`)

**Kurulum:**
- API anlık kopyadan başlatıldı. Böylece başka şeritlerin derlemesi koşum sırasında ikilileri değiştiremedi. Komut: `ASPNETCORE_ENVIRONMENT=Development nohup dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5183`. Başlatan süreç 81186, dinleyen süreç 81240.
- Kiracı `PAKETP-65-TEST Kuyumculuk`, tek kullanımlık davetle açıldı (SHA-256 özetiyle). Jeton ve parola ekrana basılmadı.
- Betik `scratchpad/pP-65/canli.py`, çıktılar `canli-cikti.txt` (1. koşum) ve `canli-cikti-2.txt` (2. koşum). Yanıtların hepsi okundu.

**1. koşum — iki beklenmedik yanıt:**
- Adım 3: `PATCH` → **400**. Sebep: kiracının ilk fiyat listesi otomatik olarak **varsayılan** oluyor ve sunucu varsayılan listeyi pasife almıyor. Bu, ekrandaki kilidin (`fl-durum` kilitli ve gerekçeli) sunucudaki karşılığıdır; 2. koşumda 2b adımı olarak ayrıca doğrulandı.
- Adım 5: kampanya `POST` → **400**. Yanıt gövdesi 1. koşumda kaydedilmedi, bu yüzden **sebebi ölçülmedi**. 2. koşumda tarihler bugünden başlatılınca (2026-09-24 → 2026-10-10) 200 döndü.

Bunun üzerine betik düzeltilip ikinci bir kiracıyla yeniden koşuldu.

**2. koşum — tablo:**

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` | 201 | **201** | tenantId var, jeton var |
| 2 | `POST /fiyat/listeler` (ilk liste) | 200/201 | **200** | aktif=True, **varsayilan=True** |
| 2b | Varsayılan listeyi pasife al (ekranda kilitli ve gerekçeli) | 400 | **400** | "Varsayılan fiyat listesi pasife alınamaz; önce başka bir listeyi varsayılan yapın" |
| 2c | `POST /fiyat/listeler` (ikinci liste) | 200/201 | **200** | varsayilan=False |
| 3 | `PATCH /fiyat/listeler/{id}` aktif=false (onay kipinin gönderdiği gövde) | 200 | **200** | SQL `fiyat_listeleri.aktif=0` |
| 4 | `PATCH` aktif=true (geri dönüş: Aktifleştir, onaysız) | 200 | **200** | SQL `aktif=1` |
| 5 | `POST /fiyat/kampanyalar` | 200/201 | **200** | id var |
| 6 | `PATCH /fiyat/kampanyalar/{id}` aktif=false (onay) | 200 | **200** | SQL `kampanyalar.aktif=0` |
| 7 | `POST /kullanicilar` (TEZGAHTAR) | 201 | **201** | id var |
| 8 | `DELETE /kullanicilar/{id}` (onay kipinin onayı, soft) | 200/204 | **200** | SQL `kullanicilar.aktif=0` (satır silinmedi) |
| 9 | `PATCH /kullanicilar/{id}/aktif` (geri dönüş: Aktif et) | 200/204 | **200** | SQL `aktif=1` |
| 10 | `GET /pano/ayarlar` | 200 | **200** | ilk kalem GRAM |
| 11 | `PATCH /pano/ayarlar/GRAM` satisMarjTl=15 | 200 | **200** | ozel=True, SQL `kiraci_ayarlari` satır=1 |
| 12 | `POST /pano/ayarlar/GRAM/varsayilana-don` (onay) | 200 | **200** | ozel=False, SQL `kiraci_ayarlari` satır=0 |
| 13 | `POST /kur/manuel` | 200/201 | **201** | kaynak=MANUAL |
| 14 | `GET /kur/gecmis` | 200 | **200** | kaynaklar = ALTINKAYNAK + MANUAL. Ekranın iki kolu da gerçek veride var: dış kaynak satırı kilitli ve `title` taşıyor, MANUAL satırı açık ve ManuelKurKaydi ipucu çiziliyor. |

**Yayın kanıtı (5183'ün sunduğu istemci):**
- `/_framework/blazor.boot.json` → 200.
- `Kuyumcu.Web.wasm` (3.068.697 bayt) içinde `OnayKipi` ×2 ve `OnayKilitSozlugu` ×1 bulundu.
- `Kuyumcu.Core.wasm` içinde `OnayKilitMetinleri` tipi ve UTF-16 "Kutuya" dizgesi bulundu.
- Yani yeni bileşen ve sözlük yayına giriyor.

**Tarayıcıyla tıklama yapılmadı:** tarayıcıda oturum açmak parola girmeyi gerektiriyordu. Ekran davranışı 548 + 4946 bUnit testiyle kanıtlandı.

**Yan gözlem (bu paket DIŞI):** API günlüğünde zamanlanmış işler için `Duplicate entry … is_calismalari.IX_is_calismalari_tenant_id_is_kodu_calisma_anahtari` hataları görüldü. Aynı gün başka bir API örneği aynı işleri daha önce koşmuş; benzersiz anahtar tekrar koşumu engelliyor ama günlüğe `fail:` düşüyor.

**Temizlik:**
- API `kill 81186` ile durduruldu; `lsof :5183` boş.
- 5199'a dokunulmadı (dinleyici pid 12590 ayakta).
- İki test kiracısı (`01M38PQYP7P421KQX82Q8MYJ7G`, `01M38PRRZBAZN5H8XV4NJ6J3KR`) silindi: `tenant_id` kolonu olan **70 tablodan**, iki `kiraci_davetleri` satırından ve `tenants`'tan.
- Kalan satır **0**. **tenants: 10 → 10.** `QA-TEST-%` kiracılarına (4) dokunulmadı.

## 11. Koşul karşılama

### Kabul koşulları

| Koşul | Durum | Kanıt |
|---|---|---|
| Yıkıcı işaretli hiçbir eylem onay kipi olmadan çalışmıyor | ✅ | OnayKipiTestleri: Vitrin (çekmece ve satır), Fiyatlandırma (liste ve kampanya), Kullanıcılar. M65-1 ve M65-9 kırmızı. |
| Onay ve vazgeç düğmeleri farklı fiil taşıyor | ✅ | `OnayVeVazgecDugmeleri_AyniFiiliTasimaz`, `Iptal_onayinda_vazgec_ve_onay_ayni_fiili_tasimaz`, M65-5 |
| Devre dışı her düğmede boş olmayan gerekçe (title ya da yanındaki açıklama) | ✅ | `Kur_ekraninda_devre_disi_HER_dugme…`, `Emanet_kilitli…`, denetim P-13 = 0 |
| Kaydet kilidi varken eksik alanların adı ekranda listeleniyor | ✅ | `KaydetKilidi_EksikAlanlariListeler`, M65-6 |
| Dört ekranın her birinde kilitli alanın yanında gerekçe ve düzeltme yolu okunuyor | ✅ | KilitGerekceTestleri × 5, M65-3, 4, 7, 8 |
| Hiçbir ekranda mevcut düzenleme yeteneği kaybolmadı | ✅ | Kilit koşulları değişmedi. Aktifleştirme onaysız (`Aktiflestirme_yikici_degildir…`). Filtreli 548/548 ve tam süit 4946/4946 yeşil. |
| Gerekçe metinleri sözlükten geliyor, gömülü dize yok | ✅ | `Sozlukler.OnayKilit.cs` (TR+EN). Düğme adları `{0}` ile gerçek etiketten geliyor. |
| dotnet test 0 başarısız, denetim.sh TEMİZ | ✅ | Tam süit 4946/0. denetim son koşum TEMİZ. |

### Paket kapısı (paralel şerit maddesi)

| Madde | Durum | Kanıt |
|---|---|---|
| (1) build 0 hata | ✅ | Son temiz derleme: 0 Hata |
| (2) bu paketin testleri 0 başarısız | ✅ | 47/47 |
| (3) bu paketin sayaçları ✓ | ✅ | § 9 |
| (4) mutasyon kanıtı | ✅ | 10/10 |
| (5) canlı doğrulama | ✅ | § 10 |
| (6) test kiracısı silindi | ✅ | tenants 10 → 10 |
| (7) yeni kırmızı yok | ✅ | Tam süit 0 kırmızı |

## 12. Açık kalanlar

1. **`Sozlukler.cs` HUB'daki `Evrak.TurDegismez` metni** hâlâ "iptal edip yenisini açın" diyor, ama sunucuda evrak iptal ucu yok. Bu şerit HUB'a dokunamadığı için metin düzeltilmedi. Ekranda artık yanında doğru yolu yazan DegismezIpucu var. Metnin HUB'ı sahiplenen pakette düzeltilmesi önerilir (sahibi: orkestratör / HUB sırası).
2. **Yazarak onaylama** bileşende hazır, ama bağlanan üç eylemin hiçbiri geri dönülemez olmadığı için kullanılmadı. **/yedekler'deki "iptal" onayı** (E49'daki örnek) SERİT E'nin dosyasında (`Yedekler.razor`) olduğu için bu şeritte bağlanmadı. OnayKipi o ekrana bağlandığında aynı-fiil koruması kendiliğinden devreye girer (sahibi: SERİT E / sonraki paket).
3. **Kullanıcılar'daki sahiplik devri penceresi** hâlâ ModalPencere. İçinde şifre ve rol alanları olan bir formdur, bu yüzden OnayKipi'ye taşınmadı. Vazgeç düğmesi "İptal", onay düğmesi "Sahipliği Devret"; fiiller zaten farklı.
4. Yan gözlem (bu paket dışı): zamanlanmış işin tekrar koşumu `is_calismalari` benzersiz anahtarında `fail:` günlüğü üretiyor (§ 10).
5. Tam süit asıl ağaçta paralel derleme tuzağı yüzünden koşulamadı. 06:15 anlık kopyasında koşuldu. Dalga kapısı yine orkestratörün ölçümüdür.

## 13. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgecinden, bu paketin dokunduğu dosyalar:

**YENİ:**
- `dotnet/Kuyumcu.Web/Bilesenler/OnayKipi.razor`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.OnayKilit.cs`
- `dotnet/Kuyumcu.Tests/OnayKipiTestleri.cs`
- `dotnet/Kuyumcu.Tests/KilitGerekceTestleri.cs`
- `dotnet/Kuyumcu.Tests/KurEkranTestleri.cs`

**DEĞİŞEN:**
- `dotnet/Kuyumcu.Web/Pages/Vitrin.razor`
- `dotnet/Kuyumcu.Web/Pages/Fiyatlandirma.razor`
- `dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor`
- `dotnet/Kuyumcu.Web/Pages/Kur.razor`
- `dotnet/Kuyumcu.Web/Pages/Emanet.razor`
- `dotnet/Kuyumcu.Web/Pages/Evraklar.razor`
- `dotnet/Kuyumcu.Web/Pages/Bildirimler.razor`
- `dotnet/Kuyumcu.Tests/EvrakEkranTestleri.cs`
- `dotnet/Kuyumcu.Tests/EmanetKurEkranTestleri.cs` (yalnız `partial`)
- `dotnet/Kuyumcu.Tests/P17EkranTestleri.cs`
- `dotnet/Kuyumcu.Tests/FiyatlandirmaUX4aEkranTestleri.cs`
- `dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs`

**DOKUNULMAYANLAR:**
- `ModalPencere.razor`, `DegismezIpucu.razor`, `Sozlukler.cs` (HUB), `app.css`.
- S-2b, SERİT E ve SERİT G dosyaları.

Git durumuna dair not: `ModalPencere.razor` `M` görünüyor, ama bu ilk commit'e göre önceden var olan bir farktır; bu paket o dosyaya yazmadı. Sayfaların çoğu ilk commit'ten sonra oluşturulduğu için `??` görünüyor.

## 14. Kurucuya / mali müşavire notlar

Mevzuat içeriği yok; mali müşavir teyidi gerekmiyor. Ürün kararı olarak kayda geçen tek nokta: **pasife alma yıkıcı sayıldı ve onay istiyor, aktifleştirme onaysız.** Bunun gerekçesi ekran başına tek birincil eylem ilkesi ve geri dönüş yolunu zorlaştırmamak.
