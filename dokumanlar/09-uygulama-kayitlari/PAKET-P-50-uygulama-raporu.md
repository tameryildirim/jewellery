# PAKET P-50 — Negatif kasadan çıkış yolu: /gider formunda bakiye + eylem bağlantılı hata

**Rol:** Taşkın (backend, sunucu değişmedi) + Ertuğ (frontend) · **Dalga:** PARA VE SAYI DOĞRULUĞU (QA tur 8 triyajı, cumartesi kümesi)
**Tarih:** 2026-09-23 · **Süre:** 21:40 → 22:05 ölçüldü, rapor dahil yaklaşık 30 dakika. Triyajın tahmini 2 saatti; bu bir **tahmindi**, ölçüm değildi.
**Durum:** BİTTİ. Paket kapısının yedi maddesi sağlandı. Tam süit **4369/4369** yeşil.

---

## 1. Spesifikasyon koda karşı doğrulandı (önce ölçüldü, sonra yazıldı)

| İddia / "DOĞRULANAMADI" kalemi | Ölçüm | Sonuç |
|---|---|---|
| Dosya ayak izi: `Pages/Gider.razor` | `test -e` → VAR | ✓ |
| Dosya ayak izi: `Kuyumcu.Tests/GiderEkranTestleri.cs` (YENİ) | `test -e` → YOK | Yeni dosya olarak açıldı ✓ |
| **UYGULAYICI İLK İŞ:** Gider.razor kasa listesini hangi uçtan, bakiyeyle mi çekiyor? | `Yukle()` içinde `GET /kasalar` → `List<KasaSz.KasaVeri>`. Sözleşme `KasaVeri(Id, Ad, ParaBirimi, **Bakiye**, Aktif, SubeId)`. Canlıda da `bakiye` alanı dönüyor (§ 7, adım 4). | Bakiye elde vardı ama **ekrana basılmıyordu**. Yeni uç gerekmedi. |
| Yetersiz bakiye metni | `KasaBakiyeKapisi.Mesaj` (P-44): `'{ad}' kasasının bakiyesi yetersiz: {bakiye:0.00 Invariant} {pb} var, {istenen} {pb} isteniyor. Nakit kasa eksiye düşemez.` Ekran bunu `FormDurumu.SunucuMesaji` ile **ham** olarak `#gd-hata` bandına basıyordu. | Hata doğrulandı. |
| `/kasalar?kasa={id}&yon=giris` rotası | `Kasalar.razor` içinde `SupplyParameterFromQuery` **yoktu**, yani sorgu yok sayılıyordu. | Bağlantı ölü olurdu. Bkz. Sapma S1. |
| `/kasalar?virman={id}` rotası | `/kasalar` sayfasında virman formu **yok**. Virman P-19'dan beri `/gider` sayfasının sekmesi; `Kasalar.razor` zaten `/gider?sekme=virman` bağlantısı veriyor. | Bağlantı ölü olurdu. Bkz. Sapma S2. |
| `/raporlar/kasa` rotası | `Pages/KasaRaporu.razor:1 @page "/raporlar/kasa"` | ✓ |
| (b) `Dil.cs` yok, gerçek dosya `DilServisi.cs` | Doğru. Bu pakette ikisine de dokunulmadı. | — |
| (c) `SatirIciDuzenle.razor` var | Doğru. Bu pakette yeni düzenleme bileşeni yazılmadı. | — |
| (a) Fikstür kasasının eksiye düşme yolu | Ölçüm P-44'e aitti. `KasaBakiyeKapisi.cs` başlığında dört `ALIS` (−858.500,00) ve bir `HURDA_ALIS` (−24.548,80) olarak kayıtlı. Bu pakette yeniden ölçülmedi, çünkü paket sunucuya dokunmuyor. | — |
| Mevzuat | Paketin mevzuat hükmü yok. "Nakit kasa eksiye düşemez" kuralı P-44'ündür ve bu pakette sunucu değişmedi. | — |

## 2. Yapılanlar

### 2.1 `Pages/Gider.razor`
1. **Seçicide bakiye:** `KasaSecenekleri` her kasayı `Ad (PB) · Bicim.Para(Bakiye, PB)` biçiminde yazar (UX-S21/S32). Kullanıcı eksi kasayı seçmeden önce görür.
2. **Bakiye rozeti `#gd-kasa-bakiye`:** Seçili kasanın ya da tek olduğu için sabit kasanın bakiyesini tr-TR para biçiminde gösterir. Eksi bakiyede rozet `rozet-kirmizi` olur ve yanına ek bir kırmızı "Eksi bakiye" rozeti çıkar.
3. **Kalıcı uyarı şeridi `#gd-eksi-kasa-uyari`** (`bant bant-uyari`, `role=status`): Seçili kasa eksideyse formun **üstünde** durur. Biçimlendirilmiş bakiyeyi ve `/raporlar/kasa` bağlantısını (`#gd-eksi-kasa-rapor`) taşır.
4. **İstemci erken kapısı:** `GiderKilidi` artık `tutar > bakiye` durumunda da kilitler. Sebep düğmenin yanında yazar: "Tutar kasa bakiyesini aşıyor (kasada … var)" (UX-S30). Sunucu kapısı tek gerçek kaynak olarak kalır.
5. **Yetersiz bakiye bandı `#gd-yetersiz`** (`bant bant-hata`, **`role=alert`**) iki durumda çıkar:
   - **Sunucu reddi:** Ekrandaki bakiye bayat olabilir. Ekran bakiyeyi yeterli sanırken 400 gelirse devreye girer. `catch (ApiHata e) when (YetersizBakiyeReddi(e))` ham mesajı `#gd-hata`ya **yazmaz**. Mesajdaki InvariantCulture sayıları regex ile ayrıştırılır, `Bicim.Para` ile tr-TR biçimine çevrilir ve sözlük cümlesine konur. Kalıp tutmazsa sayısız genel cümle basılır; ham metin **hiçbir durumda** gösterilmez. Ardından `/kasalar` tazelenir ki rozet gerçek bakiyeyi söylesin.
   - **İstemci erken uyarısı:** Seçili kasa ile girilen tutar bakiyeyi aşıyorsa aynı bant aynı bağlantılarla çıkar.
   - İki eylem bağlantısı taşır: **"Kasaya giriş yap"** `/kasalar?kasa={id}&yon=giris` (`#gd-yetersiz-giris`) ve **"Virman yap"** `/gider?sekme=virman&hedef={id}` (`#gd-yetersiz-virman`, bkz. S2).
   - Sunucunun reddi yalnız **aynı kasa ve aynı tutar** ekrandayken gösterilir. Kullanıcı kasayı ya da tutarı değiştirince kendiliğinden kalkar; bayat bant kalmaz.
6. **Derin bağ `hedef`:** Yeni `[SupplyParameterFromQuery(Name="hedef")]` eklendi. P-19'un `sekme` işlemesi `OnInitializedAsync`'ten `OnParametersSet`'e taşındı. Gerekçe: bant bağlantısı **aynı sayfaya** yalnız sorguyu değiştirerek döner, Blazor bileşeni yeniden kurmaz, yalnız parametreleri yeniler. İmza koruması (`uygulananSorgu`) aynı sorgunun kullanıcının sekme seçimini ezmesini engeller. P-19'un derin bağ testleri yeşil kaldı.

### 2.2 `Pages/Kasalar.razor` (ayak izi dışı, bkz. S1)
`[SupplyParameterFromQuery] kasa` ve `yon` eklendi. Yükleme sonrası kimlik **aktif** bir kasayla eşleşirse `HareketModalAc(kasa)` çağrılır ve yön `GIRIS` olur (`yon=cikis` gelirse `CIKIS`). Tanınmayan kimlik yok sayılır: liste açılır, yanlış kasaya form açılmaz. Mevcut element id'leri değişmedi.

### 2.3 `Kuyumcu.Core/I18n/Sozlukler.cs` (HUB, yalnız ekleme)
`SozlukGider` sınıfına 9 anahtar eklendi, TR ve EN birlikte: `KasaBakiyesi`, `KasaBakiyeEksi`, `KilitBakiyeYetersiz`, `YetersizBakiye`, `YetersizBakiyeGenel`, `KasayaGirisYap`, `VirmanYapBaglanti`, `EksiKasaUyari`, `KasaRaporunaGit`. Mevcut hiçbir anahtar değişmedi.

### 2.4 Sunucu
**Değişmedi.** `GiderModulu` iş kuralı ve mesaj metni P-44'ündür (DOKUNULMAZ). Migration yok.

## 3. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | Ayak izi dışı `Pages/Kasalar.razor`a dokunuldu: `kasa`/`yon` sorgusu eklendi (yaklaşık 20 satır). | Kabul koşulu "ikisi de doğru rotayı açar (query dolu)" diyor. Kasalar sorguyu okumasaydı bağlantı düz liste sayfasına düşerdi ve çıkış yolu yarım kalırdı. **TEK DOKUNUŞ:** Kasalar.razor bu dalgada P-50 tarafından açıldı. Triyaj § 3.3'e göre bu sayfaya P-71 de dokunacak; eklenen blok `OnInitializedAsync` başına yerleşti ve P-71'in `/kur/guncel` taşımasıyla aynı satırlara değmiyor. |
| S2 | "Virman yap" bağlantısı `/kasalar?virman={id}` yerine `/gider?sekme=virman&hedef={id}` oldu. | `/kasalar` sayfasında virman formu yok (P-19 kararı: virman /gider sekmesidir). Triyajın rotası ölü bağlantı üretirdi. Hedef, spesifikasyonun amacı olan "çıkış yolu" ile birebir tutarlı: virman sekmesi açılır ve **hedef olarak eksi kasa seçili gelir**. Bu davranış bir testle ve bir mutasyonla kanıtlandı. |
| S3 | Test listesine üç kanıt testi eklendi (toplam 8). | "Doğru rotayı açar" koşulunu `href` eşitliğinin ötesinde sınamak için: `Kasaya_giris_baglantisi_kasalar_ekraninda_GIRIS_formunu_acar`, `Kasalar_sorgusu_tanimsiz_kasada_form_ACMAZ` (negatif kontrol), `Virman_baglantisi_virman_sekmesini_hedef_kasa_secili_acar`. |
| S4 | Bandın görünür iletisi: "'{kasa}' kasasında yeterli para yok: kasada X var, Y isteniyor. Nakit kasa eksiye düşemez." | Sunucu metni aynen kullanılmadı, çünkü içindeki sayılar ham. Anlam aynı; jargon yok ("bakiye yetersiz" yerine "yeterli para yok"). |

## 4. Derleme · Migration
- `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata** (son temiz derleme mutasyonlardan sonra alındı: `build3.log`).
- Migration: **gerekmedi**, çünkü şema değişmedi.

## 5. Testler

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| `GiderEkranTestleri` + `P19*` (filtreli) | 53 | 53 | 0 |
| `OlcumKapisiTestleri` + `SozlesmeYansimaTestleri` + `GiderEkranTestleri` + `P19*` (record düzeltmesi sonrası, yorum düzeltmesinden önce) | 91 | 89 | 2 → düzeltildi, bkz. not |
| **Tam süit** (`trx-pP-50`, son kod) | **4369** | **4369** | **0** |

Yeni test sayısı **8**. Pakete giren tam süit sayısı 4361 (paket öncesi taban ayrıca koşulamadı; bkz. not 1).

**Notlar (Anayasa md.1, gizlenmedi):**
1. Paket başında taban tam süiti arka planda başlatıldı ama **koşmadı**: `timeout: command not found` (macOS'ta `timeout` yok). Bu yüzden "önce" kırmızı listesi alınamadı. Karşılaştırma son koşuma göre yapıldı: 0 kırmızı, dolayısıyla bu paketin ürettiği kırmızı da 0.
2. İlk tam süit (ara sürüm) **4 kırmızı** verdi: `OlcumKapisiTestleri.P02_blogundaki_her_sayac…`, `OlcumKapisiTestleri.SAYFA_RECORD_TAVAN…`, `SozlesmeYansimaTestleri.Razor_code_blogunda_istemci_record_tanimi_acilmiyor`, `SozlesmeYansimaTestleri.Tasinan_tipler_Contracts…`. Dördü de **bu paketin ürünüydü**: Gider.razor'a açtığım `private sealed record YetersizBakiyeBilgisi` (razor record tavanı 3). Değer demetiyle `(string KasaId, decimal Tutar, string Metin)?` değiştirildi. Ardından yorum satırındaki "record" kelimesi de sayaca takıldı (`grep "record "`); yorum yeniden yazıldı. Son tam süit 0 kırmızı.

## 6. Mutasyon kanıtları
Yöntem: yedek `scratchpad/mut-yedek/P-50/` altına alındı. Her mutasyon `perl -0777 -pi` ile tek eşleşmeli değişiklikti (eşleşme sayısı basıldı). Ardından `dotnet test --filter GiderEkranTestleri -v n` koşuldu, yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu kanıtlandı. Tur 2 **son kodla** yeniden koşuldu (tuple düzeltmesinden sonra); aşağıdaki sonuçlar tur 2'nindir. Son mutasyondan sonra temiz derleme: 0/0.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M50-1 | Bakiye rozeti kaldırıldı (`<Rozet …>@Bicim.Para(bakiyeKasa…)</Rozet>` silindi) | `Kasa_secilince_bakiye_TL_biciminde_gorunur` | cmp birebir |
| M50-2 | İki eylem bağlantısı silindi ve bant `e.Message` (ham sunucu metni) basar hâle getirildi (3 eşleşme) | `Yetersiz_bakiye_bandi_iki_eylem_baglantisi_tasir` + `Ham_invariant_tutar_bicimi_ekranda_gecmez` | cmp birebir |
| M50-3 | `role="alert"` özniteliği silindi | `Yetersiz_bakiye_bandi_role_alert_tasir` | cmp birebir |
| M50-4 | Uyarı şeridi koşulu `Bakiye < 0m` yerine `Bakiye < -10_000_000m` yapıldı | `Negatif_bakiyeli_kasada_uyari_seridi_ve_rapor_baglantisi_var` | cmp birebir |
| M50-5 (ek) | Kasalar.razor: `HareketModalAc(sorguKasasi)` kaldırıldı | `Kasaya_giris_baglantisi_kasalar_ekraninda_GIRIS_formunu_acar` | cmp birebir |
| M50-6 (ek) | Gider.razor: `hedef` sorgusunun `virmanHedef`e atanması kaldırıldı | `Virman_baglantisi_virman_sekmesini_hedef_kasa_secili_acar` | cmp birebir |
| M50-7 (ek) | İstemci erken kilidi etkisizleştirildi (`… && false`) | `Negatif_bakiyeli_kasada_uyari_seridi_ve_rapor_baglantisi_var` | cmp birebir |

M50-7'nin ilk denemesi (`: false`) derleme hatası verdi (`CS0103 'sk'`). Mutasyon yeniden yazılıp yeniden koşuldu; bu bir test açığı değildi. Hiçbir mutasyon testleri yeşil bırakmadı, test açığı yok.

## 7. Canlı doğrulama (localhost:5198 · `kuyumcu_net`)
Test kiracısı **`PAKET50-TEST Kuyumculuk`** (`01M37T52ZSF0S931511SGD22PE`), elle eklenen tek kullanımlık davet satırıyla açıldı. 5199'a dokunulmadı.

| # | İstek | Beklenen | Gerçek | Yanıt (okundu) |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` (davetli) | 201 | **201** | `tenantId`, `accessToken`, `refreshToken` |
| 2 | `POST /kasalar` "P50 Merkez Kasa" TRY | 200 | **200** | `bakiye: 0.00` |
| 3 | `POST /gider-kategorileri` "Kira" | 200 | **200** | kategori id |
| 4 | `POST /giderler` 100 (bakiye 0) | 400 + P-44 mesajı | **400** | `'P50 Merkez Kasa' kasasının bakiyesi yetersiz: 0.00 TRY var, 100.00 TRY isteniyor. Nakit kasa eksiye düşemez.` |
| 5 | `POST /kasalar/{id}/hareket` GIRIS 500 | 200 | **200** | `bakiye: 500.00` |
| 6 | `POST /giderler` 100 | 200 | **200** | `kasaBakiyesi: 400.00` |
| 7 | *(SQL, yalnız kendi kiracım)* `UPDATE kasalar SET bakiye=-738298.80` ile fikstür durumu taklit edildi → `GET /kasalar` | 200 + eksi bakiye | **200** | `"bakiye":-738298.80` (ekranın biçimlendirdiği ham sayı) |
| 8 | `POST /giderler` 1500 (eksi kasa) | 400 + ham invariant sayı | **400** | `… yetersiz: -738298.80 TRY var, 1500.00 TRY isteniyor. …`. **Gider.razor'daki regex bu canlı mesajla eşleşti:** `{ad:'P50 Merkez Kasa', b:'-738298.80', pb:'TRY', i:'1500.00'}` (aynı desen Python `re` ile sınandı). |
| 9 | Çıkış yolu: eksi kasaya `POST /kasalar/{id}/hareket` GIRIS 1000 | 200 (giriş kapıya tabi değil) | **200** | `bakiye: -737298.80` |
| 10 | `GET /gider` (Blazor kabuğu) | 200 | **200** | `<title>MİHENK</title>`, `_framework/blazor.webassembly.js` |

**SQL kanıtı:** `SELECT bakiye FROM kasalar WHERE id=…` sonucu `-737298.80`. `kasa_hareketleri` sonucu `500.00 "P50 canli giris"`, `-100.00 NULL` (gider), `1000.00 "P50 cikis yolu"`.

**Ekran:** Tarayıcıda canlı açılmadı. Giriş için test kullanıcısının parolasını forma yazmak gerekiyordu ve bu ajanın güvenlik kuralı buna izin vermiyor. Ekran davranışı bUnit ile, sunucunun canlıda doğrulanan **birebir yanıt şekliyle** kurulmuş fikstürle sınandı (adım 4/8 mesajı ile adım 7 `bakiye` alanı). Kabul koşulundaki "QA tarayıcı turunda ölçülebilir" maddesi bir sonraki QA turunda ölçülecek. Ölçülecek seçiciler: `#gd-kasa-bakiye`, `#gd-eksi-kasa-uyari`, `#gd-eksi-kasa-rapor`, `#gd-yetersiz[role=alert]`, `#gd-yetersiz-giris`, `#gd-yetersiz-virman`.

**Temizlik:**
- API PID 87218 (`dotnet run`) ve dinleyici 87440 `kill` ile durduruldu. `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş** döndü. 5199 dinleyicisi yerinde.
- `tenant_id` kolonu olan **70 tablonun tamamından** kiracı satırları, `tenants` satırı ve davet satırı silindi. Kalan satır denetimi **0**.
- `SELECT COUNT(*) FROM tenants` → **başlangıçta 10, sonda 10**. `QA-TEST-*` kiracılarına (4 adet) dokunulmadı.
- Parola, jeton ve token içeren geçici dosyalar `printf "" >` ile boşaltıldı (silme kurucuya bırakıldı).

## 8. denetim.sh
Son koşum (`denetim3.log`): **DENETİM BAŞARISIZ**. Kalan iki ✗ **bu pakete ait değil**:

| ✗ | Değer | Sahibi (ölçüm) |
|---|---|---|
| `ArayuzModu (yalnız 6 dosya)` | 8 (beklenen 6) | Yeni `Kuyumcu.Api/Modules/Gorunum/GorunumModulu.cs` ve `KullaniciGorunumModulu.cs` (değişiklik saati 22:00). Başka şerit; P-50 API'ye dokunmadı. |
| `Kültürsüz biçimli ToString (Web)` | 28 (tavan 27) | `Pages/Yedekler.razor` (6 eşleşme, değişiklik saati 21:52; Yedekler zinciri P-56/P-76…). Gider.razor ve Kasalar.razor'da **0** eşleşme var. |

P-50'nin kendi sayaçları ✓: `Pages+Bilesenler+Layout record tanımı 3 (tavan 3)` · `P-02 OLCUM_TABANI_KOPYA 0` · `P-10/P-06/P-16 BU_PAKETTE_EKLENEN_RECORD 0` · `P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 47 (tavan 47)` · `Pages: ModalPencere kullanımı 55 (tavan 55)`, yani yeni ModalPencere yok. Ara koşumda bu paketin ürettiği 3 ✗ (record 4 > 3, OLCUM_TABANI_KOPYA, P-11 toplam 48 > 47) düzeltildi. P-11'deki artış, `catch` içinde `hata = null;` satırından geliyordu; satır kaldırıldı, çünkü `hata` zaten try öncesinde boşaltılıyor.

## 9. DoD sözleşmesi (varlık ve düzenlenebilirlik)
Bu paket **yeni varlık ya da kayıt türü doğurmadı**. Mevcut kayıtların durumu:

| Kayıt | Düzeltme yolu | Not |
|---|---|---|
| Gider (kasa hareketi) | STORNO. Mevcut `DegismezIpucu` ve satırdaki "Storno" düğmesi korundu. | Finansal kayıt: hard delete yasak. |
| Kasa bakiyesi | Kasaya GİRİŞ hareketi (yeni bağlantı: /kasalar giriş formu) ya da VİRMAN (yeni bağlantı: /gider virman sekmesi, hedef seçili) | Bakiye elle düzenlenemez, yalnız hareketle değişir. Bu paket bu yolları **ekrana bağladı**. |

**Düğme ve bant denetimi (DoD EK 1):** Yeni `<button>` yok. Eklenen her eylem gerçek rotası olan bir `<a href>`. Kaydet düğmesinin yeni kilidi `disabled` durumunda sebebini yanında yazıyor (UX-S30). Yeni CSS yok, satır içi style yok; yalnız mevcut `bant-uyari`, `bant-hata` ve `rozet-kirmizi` sınıfları kullanıldı. Metinler Sozlukler üzerinden geliyor (TR+EN), çıplak metin yok.

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Gider.razor` · `Pages/Kasalar.razor`

## 10. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` (P-50 satırları):
- ` M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`: 9 anahtar, TR ve EN birlikte
- ` M dotnet/Kuyumcu.Web/Pages/Kasalar.razor`: `kasa`/`yon` derin bağı
- `?? dotnet/Kuyumcu.Web/Pages/Gider.razor`: P-19'dan beri git'te izlenmiyor, commit yok
- `?? dotnet/Kuyumcu.Tests/GiderEkranTestleri.cs`: YENİ
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-50-uygulama-raporu.md`: bu rapor

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Kasa seçilince bakiye ekranda görünür (TL biçiminde) | ✓ | `Kasa_secilince_bakiye_TL_biciminde_gorunur` (`₺10.000,00`, eksi `738.298,80` + kırmızı rozet), M50-1 |
| Seçicide her kasanın bakiyesi ve para birimi | ✓ | Aynı testin liste iddiası (`SeciciYardim.ListeMetni`) |
| Eksi kasada kırmızı rozet | ✓ | `.rozet-kirmizi` iddiası |
| Tutar bakiyeyi aşarsa kaydet istemcide engellenir; sunucu tek gerçek kaynak | ✓ | `Negatif_…` testi: disabled, sebep "bakiyesini aşıyor", POST yok; M50-7. Sunucu değişmedi. |
| Yetersiz bakiye bandında "Kasaya giriş yap" ve "Virman yap", query dolu | ✓ (S2 sapmasıyla) | `Yetersiz_bakiye_bandi_iki_eylem_baglantisi_tasir`, M50-2 |
| İki bağlantı da doğru rotayı açar | ✓ | `Kasaya_giris_…_GIRIS_formunu_acar` (M50-5), `Virman_baglantisi_…_hedef_kasa_secili_acar` (M50-6) |
| Bant role=alert taşır | ✓ | `Yetersiz_bakiye_bandi_role_alert_tasir`, M50-3 |
| Eksi kasada uyarı şeridi + çalışan /raporlar/kasa bağlantısı | ✓ | `Negatif_bakiyeli_kasada_uyari_seridi_ve_rapor_baglantisi_var` (href + `KasaRaporu` RouteAttribute), M50-4 |
| Ham '-738298.80 TRY' biçimi hiçbir yerde görünmez | ✓ | `Ham_invariant_tutar_bicimi_ekranda_gecmez` (tüm markup: `-738298.80`, `738298.80 TRY`, `1500.00 TRY` yok), M50-2. Canlı mesaj regex'le eşleşiyor (§ 7/8). |
| GiderModulu iş kuralı ve mesajı değişmez | ✓ | API dosyalarına dokunulmadı |
| QA tarayıcı turunda ölçülebilir | Seçiciler hazır | Tarayıcı ölçümü sonraki QA turunda yapılacak (§ 7) |

## 12. Açık kalanlar
1. **Başka şeritlerin denetim ✗'leri** (sahibiyle): `ArayuzModu 8/6` → Görünüm modülü şeridi (`Modules/Gorunum/*`). `Kültürsüz ToString 28/27` → `Pages/Yedekler.razor` şeridi. Orkestratör kapısında ölçülmeli.
2. **Tarayıcı ölçümü:** Kabul koşulunun "QA tarayıcı turunda ölçülebilir" kısmı QA tur 9'a bırakıldı (§ 7'deki seçiciler).
3. **Mesaj sözleşmesi bağımlılığı:** Ekran, sunucunun P-44 mesajını "bakiyesi yetersiz" alt dizgesiyle tanıyor ve sayıları regex ile ayrıştırıyor. P-44 mesajı değişirse ekran yine ham metin basmaz, sayısız genel cümleye düşer. Ancak "yetersiz" tanıması da koparsa hata genel banda gider. Kalıcı çözüm: sunucunun 400 gövdesine makine okunur bir `kod` alanı (ör. `KASA_BAKIYE_YETERSIZ`) ve sayısal alanlar eklemesi. Bu bir **sunucu** değişikliği olduğu için bu pakette yapılmadı (DOKUNULMAZ). Bertan/Taşkın için öneridir.
4. **Kasalar.razor TEK DOKUNUŞ kaydı:** P-71 aynı sayfayı açacak (triyaj § 3.3). İkinci dokunuşun gerekçesi kapı kaydına yazılmalı.
5. **Kalibrasyon:** Triyaj 2,0 saat tahmin etti; gerçekleşen yaklaşık 0,5 saat. Tam süit (~4 dk × 2) ve 14 mutasyon koşumu bu süreye dahil.

## 13. Kurucuya / mali müşavire notlar
- Mevzuat hükmü yok. "Nakit kasa eksiye düşemez" kuralı P-44'ündür; bu paket yalnız ekranda gösteriyor.
- Canlı fikstür kasası (QA-TEST kiracısı, −738.298,80 TL) bu pakette **değiştirilmedi**. Kullanıcı artık ekranda iki çıkış yolu görüyor: kasaya giriş ya da virman. Fikstür kasasını düzeltmek (eksi bakiyenin kökündeki ALIS/HURDA_ALIS kayıtlarının muhasebesel düzeltmesi) kurucu kararı olarak kalıyor.
