# PAKET S3-5: Kur + Vitrin + Emanet (uygulama raporu)

**Tarih:** 2026-09-24 · **Dalga:** S3B, günlük ekranlar B · **Rol:** Taşkın (backend) + Ertuğ (frontend), Opus
**Spesifikasyon:** `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 3, § 5, § 5B, § 6 (S-3) + orkestratör ek koşulu: "Demonun 7. ve 8. adımları. Kur ekranında HAS önde; Vitrin panosu (bugün yalnız Kur ekranından düğmeyle açılıyor, menüde YOK; Tüm ekranlar karosunda VAR mı, ölç); Emanet: gram/ayar/maden/cari kilitli (gerekçeli, storno yolu ekranda). Liste+çekmece şablonu."
**Durum:** BİTTİ. Paket kapısının 7 maddesi sağlandı. Son tam süitteki tek kırmızı test başka bir şeride ait (§ 9).

---

## 1. Yapılanlar

| # | Ne | Nerede |
|---|---|---|
| 1 | **HAS önde (demo adım 7).** Güncel kur kartı sayfanın **ilk kartı** oldu (eskiden kaynak seçici kartı öndeydi). Kartın başında `#kur-has-onde` var: "Has altın · 1 gram" başlığı, sayfanın en büyük sayısı olarak has fiyatı (`#kur-has-deger`), yanında küçük satırda gram altın, dolar, euro, sterlin. Değeri gelmeyen döviz satırı **yazılmaz** (sıfır uydurulmaz). Kaynak ve son güncelleme saati okunur adla yazılır ("Altınkaynak · Son güncelleme: …"). Sınıflar S3-1 Panel'in belirteçli `.panel-soru` ailesidir. **Yeni CSS yazılmadı.** | `Pages/Kur.razor` |
| 2 | **İki yüz, tek gövde.** Kaynak seçici kartı ve kur geçmişi `<ProAlan>` içine alındı. Profesyonel modda DOM aynı (id'ler değişmedi). Basit modda yerlerinde tek cümlelik not ve **Görünümü değiştir →** (`/ayarlar`) bağlantısı çıkar (`#kur-pro-notu`). Özellik silinmedi. Has kartı, ayar gram fiyatları, sarrafiye pariteleri, "Şimdi çek", "Manuel kur gir" ve "Vitrin panosunu aç" iki yüzde de durur. **Mod yetki değildir:** sunucu kapıları aynen geçerli. | `Pages/Kur.razor` |
| 3 | **Ham kod 0 (PLAN § 5B md.2, UX-S31).** Kur geçmişi rozeti `MANUAL`/`ALTINKAYNAK` yerine "Elle girilen (manuel)"/"Altınkaynak" yazar. Ayar tablosu `K22` yerine "22 Ayar" yazar. Ad, tek kaynak olan `Kuyumcu.Core.Domain.Ayarlar`'dan gelir. Parite kodu sütunu (`CEYREK_YENI`) basit yüzde **yok**. Profesyonel yüzde ürün kodu gibi kalır. | `Pages/Kur.razor` |
| 4 | **Manuel kur girişi çekmecede.** DoD EK md.2'ye göre çok alanlı form `ModalPencere` yerine `Cekmece` (`#cekmece-kur-manuel`) içinde açılır. Alan kimlikleri (`m-has` … `m-gbp`, `m-kaydet`) **değişmedi**. Çekmecenin kendi Kaydet düğmesi çizilmez (`KaydetGoster=false`), böylece tek kaydet düğmesi `#m-kaydet` olur ve iki birincil düğme çıkmaz. Vazgeç düğmesi çekmecenin altındadır. Boş ya da sıfır has girilirse alanın altında mesaj çıkar ve istek atılmaz (mevcut `FormDurumu`). | `Pages/Kur.razor` |
| 5 | **Tek çevirici.** Kur kaynağı ve ayar adı çevirisi `KurKaynakAdi` sınıfında **tek yerde** duruyor. Kur ve Vitrin ekranları aynı adı kullanıyor (UX-S31: aynı kavramın tek adı olur). Bilinmeyen kod kendi adıyla görünür, ad uydurulmaz. | `Bilesenler/KurKaynakAdi.cs` (YENİ) |
| 6 | **Vitrin.** Pano başlığındaki kaynak artık okunur adla yazılıyor (eskiden ham `ALTINKAYNAK` basılıyordu). SayfaBaşı'na **← Kur ekranı** dönüş bağlantısı (`#vitrin-kur-don`) eklendi. | `Pages/Vitrin.razor` |
| 7 | **Vitrin menü ölçümü (ek koşul: "Tüm ekranlar karosunda VAR mı, ölç").** Sonuç: **VAR.** `Ekranlar.razor` yetkili kullanıcıya alt blokta `#ekranlar-alt-vitrin` bağlantısını çiziyor. `S1dEkranlarTestleri` bunu sınıyor (satır 366 var, satır 375 yetkisiz kullanıcıda yok). Kenar çubuğunda yok, çünkü `MenuTanimi.MenusuzRotalar["/vitrin"]` kayıtlı ve gerekçesi yazılı. Bu yüzden menüye eklenmedi; Ekranlar.razor ve MenuTanimi.cs bu şeridin dosyası değil. | ölçüm |
| 8 | **Emanet: liste + çekmece (demo adım 8).** Emanet alma ve iade formları `ModalPencere` yerine `Cekmece` içinde açılıyor (`#cekmece-emanet-giris`, `#cekmece-emanet-cikis`). Kimlikler değişmedi, tek kaydet düğmesi korunuyor. | `Pages/Emanet.razor` |
| 9 | **Ayrıntı çekmecesi: kilitli alanlar gerekçeli.** Her satırda **Ayrıntı** düğmesi var (`#eh-detay-{id}`; satırda en çok 3 eylem: Ayrıntı, Storno ve açıklamanın yerinde düzenlenmesi). Çekmece salt okunurdur, Kaydet düğmesi **çizilmez**. Tür, tarih ve açıklama gösterilir. **"Değiştirilemez"** başlığı altında has gram, maden ve müşteri yer alır. `DegismezIpucu EmanetHareketiKilitli` gerekçeyi ("Has gram, maden ve müşteri emanet bakiyesini oluşturur.") ve düzeltme yolunu ("Yanlışsa önce geri alın, sonra doğrusunu yeniden girin.") yazar. **Geri al (düzeltme fişi)** düğmesi (`#eh-detay-geri-al-{id}`) çekmeceyi kapatır ve mevcut **ortak storno onayını** açar (gerekçe ≥ 20). Tek yol olarak 48-A motoru kullanılır (`POST /islemler/{id}/storno`); taklit storno yok. Geri alınamayan kayıtta düğme kilitlidir ve sebebi yanında yazar (UX-S30). | `Pages/Emanet.razor` |
| 10 | **Ters kaydın adı.** Canlı ölçümde görüldü: 48-A ters kaydı defterde kaynak türüyle (`EMANET_GIRIS`, eksi has) yazılıyor ve ekranda "Giriş" diye okunuyordu. Artık hem listede hem çekmecede **"Geri alma kaydı"** yazıyor. | `Pages/Emanet.razor` |
| 11 | **Ayar seçicileri (giriş ve iade).** Görünen metin "22 Ayar", "925 Gümüş"; değer yine kod (`K22`). Gövdeye kod gider, sunucu sözleşmesi değişmedi. İki çekmece aynı diziden beslenir (UX-S26). | `Pages/Emanet.razor` |
| 12 | **UX-S28.** Müşteri listesi hatası listeyi artık boşaltmıyor (`cariler = []` kalktı). Kur hatası da son bilinen kuru silmiyor (`kur = null` kalktı). Hata bandı ayrı çıkıyor. denetim.sh `DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM` 47'den 45'e indi. | `Pages/Emanet.razor` |
| 13 | **Sözleşme.** `HareketVeri`'ye isteğe bağlı `Maden` alanı eklendi (varsayılan `null`). Sunucu bu alanı zaten gönderiyordu (canlı adım 11: `maden=ALTIN`). Eski yanıtta alan yoksa ekranda "—" yazar. | `Kuyumcu.Contracts/Sayfalar/EmanetSozlesmeleri.cs` |
| 14 | **Metinler ayrı dosyada, TR ve EN.** Konumsal record kullanıldı; bir dilde anahtar eksikse derleme kırılır. Jargon yok ("Geri al (düzeltme fişi)", "Müşteri"). | `Kuyumcu.Core/I18n/Sozlukler.KurVitrinEmanet.cs` (YENİ) |
| 15 | 18 yeni bUnit testi. 3 mevcut test dosyasında gerekçeli uyarlama yapıldı (§ 4). | `Kuyumcu.Tests/S35KurVitrinEmanetTestleri.cs` (YENİ) |

**Yeni CSS yok · satır içi style eklenmedi · sabit renk yok · CDN/script yok.** `ModalPencere` kullanımı 55'ten 52'ye indi (üç düzenleme modalı çekmeceye taşındı). Kalan iki `ModalPencere` bu sayfalarda yalnız yıkıcı onay için kullanılıyor: kur iptali ve storno.

## 2. Sapmalar ve gerekçeleri

1. **Spesifikasyon doğrulaması.** "Vitrin panosu menüde YOK" iddiası doğru. "Tüm ekranlar karosunda VAR mı" sorusunun cevabı **VAR** (§ 1-7). Ekleme gerekmedi.
2. **"Gram/ayar kilitli" maddesi.** Emanet girişinde brüt gram ve ayar **defterde saklanmıyor**. `EmanetModulu.Giris` bunları has grama çevirip yalnız `has_grami` ve `maden` yazıyor; gram ve ayar yalnız sunucu günlüğüne düşüyor. Bu yüzden ayrıntı çekmecesi ekranda olmayan bir değeri uydurmuyor. Kilitli alan olarak **has gram, maden ve müşteri** yazılıyor. Gram ve ayar için dürüst bir satır var: "Gram ve ayar kayıtta has grama çevrilip saklanır." Brüt gram ve ayarın saklanması şema değişikliği ve sunucu işi olduğu için **S-B şeridine** bırakıldı (§ 9-2). Sunucu tarafında kilit zaten var: canlı adım 13'te `hasGrami` PATCH'i 400 döndü ve "yalnız açıklama düzeltilebilir; … STORNO + yeni kayıt" dedi.
3. **Mevcut testlerde uyarlama (5 yer, 3 dosya).** Bu testler, planın yasakladığı ham kodun ekranda göründüğünü doğruluyordu. Hiçbiri zayıflatılmadı; her biri görünen ad ve kod ayrımını sınayacak biçimde sıkılaştırıldı:
   - `EmanetKurEkranTestleri.cs`: `Ayar_bazli_gram_fiyatlari_gosterilir` artık "K22" yerine "22 Ayar" bekliyor ve `>K22<` metninin **olmadığını** doğruluyor. `MANUEL_girilen_kur_gecmiste_ayirt_edilir` satırı "Elle girilen" metniyle buluyor. Kur sınıfına `ArayuzModuServisi` kaydı eklendi.
   - `S3eKurKaynagiEkranTestleri.cs` (2 satır): son bilinen ayar tablosu kanıtı "K22" yerine "22 Ayar".
   - `UX4cEkranTestleri.cs` `M10`: K995 ve G999 artık seçenek **değerinde** aranıyor, ayrıca görünen **adları** da sınanıyor.
4. **Parite kodu profesyonel yüzde kalır.** `CEYREK_YENI` gibi kodlar ürün kodu niteliğinde. Pusula da ürün kodunu gösteriyor. Basit yüzde kod yok.
5. **Sunucuya dokunulmadı, migration yok.** Sunucu modülleri S-B şeridinin alanı. Gereken veri mevcut uçlarda zaten var (canlı § 6).
6. **Taban tam süit derlenemedi.** Paket başında (24.09 00:36) alınan taban koşumu derlenemedi (`CIKIS=1`). Sebep, başka bir şeridin yarım işi: `Faturalar.razor` CS0103 ×29 (`FT`, `cariYuklemeHatasi`, `duzenleForm`). Ek koşula göre bilinen kırmızı yoktu (orkestratör 23.09 22:00 kaydı). Bu yüzden (7) numaralı madde son tam süitteki kırmızıların **dosya sahipliği** üzerinden kanıtlandı (§ 4, § 9).
7. **Tuval.** Kur, Vitrin ve Emanet için ayrı taslak yok. Görsel dil `Panel.dc.html`'in soru kartından alındı (`.panel-soru`, büyük sayı 36 px). Liste + çekmece kuralı `canvas.json` n3 notundan alındı.

## 3. Migration

Gerekmedi. Şema değişikliği yok.

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Taban tam süit (00:36) | derlenemedi | — | — | başka şeridin `Faturalar.razor` CS0103 hataları (§ 2-6) |
| Paket filtresi (22 sınıf: S35, Kur, Emanet, Vitrin ekranları, P17, P12, UX1C2, UX4b, UX4d, S3e, S1d, SozlesmeKapsam, SozlesmeYansima, P36c, P37, P39, TasarimBelirtec, OlcumKapisi …) | 506 | 506 | 0 | `pS3-5/t4.log` |
| S35 + UX4cEkran + UrunEkran | 88 | 88 | 0 | `pS3-5/t5.log` |
| Tam süit 1 (01:35, `trx-pS3-5/_Mac_2026-09-24_01_35_30.trx`) | 4670 | 4667 | 3 | 1'i bu paketin `UX4c M10` testiydi (düzeltildi, t5'te yeşil). 2'si `UrunEkranTestleri` (S3-A, § 9) |
| **Tam süit 2, son (01:40, `trx-pS3-5/_Mac_2026-09-24_01_40_37.trx`)** | **4702** | **4701** | **1** | `DenetimIziTestleri.DenetimIzi_ManuelKurGirisi_IzdeKalir`: S-B şeridine ait (§ 9-1) |

**Yeni testler (18):** S35_01 HAS ilk kartta ve kaynaktan önce · S35_02 alt satır gram ve döviz yazıyor, eksik değer uydurulmuyor, kaynak okunur adla · S35_03 basit yüz: kaynak ve geçmiş yok, not ve bağlantı var, has ve pariteler var · S35_04 profesyonel yüz tam · S35_05 basit yüzün görünen metninde ham kod yok · S35_06 geçmiş rozeti okunur adla · S35_07 manuel giriş çekmecede, tek kaydet düğmesi, Vazgeç istek atmıyor · S35_08 boş has için alan altı mesaj ve istek yok, geçerli değerde POST gövdesi ayrıştırılıyor · S35_09 TR/EN sözlük eksiksiz ve İngilizce yüz · S35_10 Vitrin kaynağı okunur adla · S35_11 Vitrin'den Kur'a dönüş · S35_12 giriş çekmecede, ayar adı görünüyor ve gövdeye kod gidiyor · S35_13 iade çekmecede · S35_14 ayrıntı çekmecesi: kilitli alanlar ve gerekçe var, girdi ve Kaydet yok, yazma isteği yok · S35_15 ayrıntıdan geri alma ortak onaya gidiyor ve 48-A ucuna POST atıyor (gövde ayrıştırılıyor, fiş no bantta) · S35_16 geri alınamayan kayıtta düğme kilitli, sebep yazılı, tıklama bir şey yapmıyor, ters kayıt "Geri alma kaydı" · S35_17 ayrıntıda Vazgeç istek atmıyor · S35_18 müşteri listesi hatası son bilinen listeyi silmiyor.

**Düğme denetimi (DoD EK md.1):** yeni düğmeler şunlar: `#eh-detay-*` (çekmeceyi açar), `#eh-detay-geri-al-*` (onayı açar ya da kilitlidir, sebebi yazılıdır), çekmece Vazgeç düğmeleri (kapatır) ve `#vitrin-kur-don` (bağlantı). Her biri S35_07, S35_14, S35_15, S35_16, S35_17 ve S35_11 ile tıklanarak sınandı.

## 5. Mutasyon kanıtları

Yöntem şöyle: kaynak `scratchpad/mut-yedek/S3-5/<kod>.orj` olarak kopyalandı. Değişiklik `perl -0777 -pi` ile yapıldı ve eşleşme sayısının **tam 1** olduğu önce doğrulandı (`\Q…\E` düz metin). Ardından filtreli test koşuldu (`S35|EmanetEkranTestleri|KurEkranTestleri|S3eKurKaynagi|UX1C2|P17Vitrin`), yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu doğrulandı. Betikler `scratchpad/pS3-5/mut.sh` ve `mut2.sh`, tablo `mut.tsv`, sonuçlar `mut-sonuc.txt` ve `mut-sonuc2.txt`.

**Geçersiz sayılan koşumlar:** İlk betikte derleme ve test tek adımdaydı. Eşzamanlı şeritlerin derlemesi (`MSB4018 ConvertDllsToWebCil … being used by another process`, `StaticWebAssets … compressed/*.gz can not be found`) bayat ikili bıraktı. Bu yüzden M35-5, M35-8, M35-10 ve M35-13'ün ilk sonuçları geçersiz sayıldı. M35-10 bir önceki mutasyonun kırmızısını birebir tekrarladığı için bayat ikili olduğu anlaşıldı. İkinci betik (`mut2.sh`) derlemeyi ayrı yapıyor, "0 Hata" olduğunu doğruluyor ve testi `--no-build` ile koşuyor. Dört mutasyon bu betikle yeniden koşuldu. M35-13'ün ikinci koşumu da bayat ikiliye denk geldi (M35-10'un kırmızısı çıktı). Üçüncü, tek başına koşum geçerli.

| Kod | Değişiklik (kuralı kır) | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M35-1 | Has kartında kaynak ham kodla (`guncel.Kaynak`) | S35_02, S35_05 | birebir |
| M35-2 | Geçmiş rozeti ham kod (`@k.Kaynak`) | KurEkranTestleri.MANUEL_girilen_kur_gecmiste_ayirt_edilir, S35_06 | birebir |
| M35-3 | `<ProAlan ZorunluGorunur="true">` (basit yüz de kaynağı ve geçmişi çizer) | S35_03, S35_05 | birebir |
| M35-4 | Parite kodu hücresi her modda (`@if (true)`) | S35_05 | birebir |
| M35-5 | Ayar tablosu ham kod (`@a.AyarId`) | KurEkranTestleri.Ayar_bazli_gram_fiyatlari_gosterilir, S3eKurKaynagi.Basarisiz_cekme_son_bilinen_kuru_EKRANDA_BIRAKIR, S3eKurKaynagi.Guncel_kur_OKUMASI_503_…, S35_05 (2. koşum) | birebir |
| M35-6 | Manuel çekmecede `KaydetGoster="true"` (iki kaydet) | S35_07 | birebir |
| M35-7 | Sterlin null iken 0 basılır | S35_02 | birebir |
| M35-8 | Vitrin kaynağı ham kod (`@p.Kaynak`) | S35_10 (2. koşum) | birebir |
| M35-9 | Emanet ayar seçicisi görünen metni = kod | S35_12, S35_13 | birebir |
| M35-10 | Ayrıntıda geri al düğmesi hiç kilitlenmez | S35_16 (2. koşum) | birebir |
| M35-11 | Geri alırken ayrıntı çekmecesi kapanmaz | S35_15 | birebir |
| M35-12 | Maden ham kodla (`ALTIN`) | S35_14 | birebir |
| M35-13 | Müşteri hatasında `cariler = []` (UX-S28 ihlali) | S35_18 (3. koşum) | birebir |
| M35-14 | Ters kayıt "Geri alma kaydı" yerine "Giriş" | S35_16 | birebir |
| M35-15 | Giriş çekmecesinde `KaydetGoster="true"` | S35_12 | birebir |
| M35-16 | Manuel kurda has doğrulaması silinir | S35_08, UX1C2EkranTestleri.Kur_manuel_HAS_SIFIRKEN_istek_ATILMAZ_ve_alan_alti_mesaj_cikar | birebir |

16/16 mutasyon en az bir adı yazılı testi kırdı. Test açığı bulunmadı. Son mutasyondan sonra `dotnet build Kuyumcu.sln` sonucu **0 uyarı, 0 hata** (`pS3-5/build3.log`). Kalıntı taraması da temiz: mutasyon desenleri ağaçta 0.

## 6. Canlı doğrulama

Ortam: port **5190** (ek koşuldaki izolasyon portu), veritabanı `kuyumcu_net`. API şu komutla başlatıldı: `dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5190`. Başlatan süreç 64707, dinleyen süreç 64795. Kiracı tek kullanımlık bir davetle açıldı (`kiraci_davetleri` satırı `PAKETS3-5-DAVET-01`, SHA-256 özetiyle). Jeton ve parola ekrana basılmadı. Kiracı: **PAKETS3-5-TEST** (`01M3848VTXMRC6WRRWP91W0Q89`). Betik `scratchpad/pS3-5/canli.py`, çıktı `canli.json`. Yanıtların hepsi okundu.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit (davetli) | 201 | 201 | tenantId alındı |
| 2 | GET /kur/guncel (has kartının verisi) | 200, has > 0 | 200 | has=6755.53 · gram=6788.97 · usd=48.982 · eur=55.778 · gbp=65.055 · kaynak=ALTINKAYNAK · ayar=[K8,K14,K18,K21…] → ekranda "₺6.755,53" büyük sayı, "Altınkaynak", "8 Ayar …" |
| 3 | GET /kur/kaynaklar (profesyonel yüzün seçicisi) | 200 | 200 | [ALTINKAYNAK, ANKARA, TRUNCGIL], seçili ALTINKAYNAK |
| 4 | POST /kur/manuel (çekmeceden) | 200/201 | 201 | id var, kaynak=MANUAL |
| 5 | POST /kur/manuel has=0 | 400 | 400 | "Altın fiyatı sıfırdan büyük olmalıdır" (sunucu kapısı; ekran bu isteği zaten atmıyor, S35_08) |
| 6 | GET /kur/gecmis | 200, MANUAL satırı | 200 | 2 satır, kodlar [ALTINKAYNAK, MANUAL] → rozetlerde "Altınkaynak" ve "Elle girilen (manuel)" |
| 7 | GET /kur/guncel (manuel girişten sonra) | has=6912.5, MANUAL | 200 | has=6912.5, kaynak=MANUAL |
| 8 | GET /pano/guncel (Vitrin) | 200 | 200 | kaynak=ALTINKAYNAK, 7 satır → ekranda "Altınkaynak" |
| 9 | POST /cari | 201 | 201 | cari açıldı |
| 10 | POST /emanet/giris K22 10 g | 200/201 | 200 | hasGrami=9.16, maden=ALTIN, madenAdi=Altın |
| 11 | GET /emanet/{cari} (ayrıntı çekmecesinin verisi) | 200, `maden` alanı var | 200 | bakiye=9.16 · tur=EMANET_GIRIS · has=9.16 · **maden=ALTIN** · stornoKaynakId=null · fisId=null → sözleşmeye eklenen `Maden` alanı sunucudan geliyor |
| 12 | PATCH /emanet/hareketler/{id} açıklama | 200 | 200 | açıklama "S35 bilezik 2 adet" |
| 13 | PATCH kilitli alan (hasGrami) | 400 | 400 | "…yalnız açıklama düzeltilebilir; gram/ayar/cari düzeltmesi yalnız STORNO + yeni kayıt yoluyla…" (çekmecedeki gerekçe sunucuyla aynı) |
| 14 | POST /islemler/{id}/storno ("Geri al") | 200/201, stornoHareketId | 200 | stornoHareketId geldi |
| 15 | GET /emanet/{cari} (geri almadan sonra) | ters kayıt var, bakiye 0 | 200 | 2 satır, ters kayıt var, ters_has=−9.16, bakiye=0.0 |
| 16 | Aynı satıra ikinci storno | 400/409 | 409 | "Bu hareket zaten stornolanmıştır…" (ekranda düğme kilitli, S35_16) |
| 17 | PATCH arayuz-modu BASIT + GET /auth/ben | 200/200, BASIT | 200/200 | etkinArayuzModu=BASIT (Kur ekranının basit yüzü) |
| 18 | GET /kur/guncel jetonsuz | 401 | 401 | |

**SQL kanıtı** (`scratchpad/pS3-5/canli-sql.txt`):

```
hareketler:     EMANET_GIRIS  9.160000  tl 0.00  ALTIN  ters_mi 0  "S35 bilezik 2 adet"
                EMANET_GIRIS -9.160000  tl 0.00  ALTIN  ters_mi 1  "STORNO: S35 canlı: gram yanlış…"
kur_snapshots:  ALTINKAYNAK 6755.53 · MANUAL 6912.50 · ALTINKAYNAK 6755.53 (iptal_edildi 0)
kullanicilar:   arayuz_modu BASIT
denetim_kayitlari: 19 satır (yazma işlemleri iz bıraktı)
```

Ters kaydın `tur` değeri `EMANET_GIRIS` ve has değeri eksi. § 1-10'daki "Geri alma kaydı" düzeltmesinin sebebi bu ölçüm.

**Tarayıcıda görsel doğrulama YAPILMADI.** Ekrana girmek için giriş formuna parola girmek gerekir ve bu ajan bunu yapmaz. Ekran davranışı 18 bUnit testiyle (id'ler üzerinden) ve uçların canlı yanıtlarıyla doğrulandı. 375/768/1440 px taşma ölçümü orkestratörün 5199 yayınına ve QA turuna bırakıldı.

**Kapanış:**
- API durduruldu (`kill 64707 64795`). `lsof -nP -iTCP:5190 -sTCP:LISTEN` **0 satır** döndü.
- PAKETS3-5-TEST kiracısı `tenant_id` kolonu olan **70 tablonun hepsinden** ve `tenants` tablosundan silindi. Kalan satır **0**. Davet satırı da silindi (0).
- `QA-TEST-*` ve `DEMO-Vitrin` kiracılarına dokunulmadı.
- `tenants` sayısı: başlangıçta **10**, sonda **10**.

## 7. denetim.sh

İki koşum yapıldı:
- **01:35, `DENETİM TEMİZ.`** (`scratchpad/pS3-5/denetim.log`). Bu koşum bu paketin bütün kod değişikliklerinden sonra yapıldı.
- **01:49, `DENETİM BAŞARISIZ`** (`denetim2.log`). Tek ihlal şu satır: `✗ S2-3: ALAN_ISARETI_EKSIK 1 (masak=1 ≥2 · tevsik=1 ≥1 · ara katman)`. **Bu ihlal bu paketten gelmiyor.** Sayaç S-2 şeridinin S2-3 bölümü: denetim.sh'a 01:24'te eklendi, peşin müşteri eşik reddinin işaretlerini `Bilesenler/Fis/*` ve `Islemler.razor` içinde sayıyor. `FisMusteriSatiri.razor` 01:25'te, `Islemler.razor` 01:33'te değişti. Bu paket o dosyalara dokunmadı. Sahibi **S-2**.

Bu paketin ilgili sayaçları iki koşumda da ✓ ve aynı değerde:

```
✓ Pages: ModalPencere kullanımı                52 (tavan 55)      ← bu paket −3
✓ Pages: duzenlenenId kalıbı                   1 (tavan 1)        ← ikinci düzenleme kalıbı açılmadı
✓ Pages: @onclick'siz ve disabled'sız <button>  70 (tavan 71)
✓ P-11: DIS_HATADA_VERI_DUSUREN_YOL              0 (/kur + /bildirimler)
✓ P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM       45 (tavan 47)     ← bu paket −2 (Emanet)
✓ P-12: STORNO_GORUNMEYEN_EKRAN                  0 (emanet'te eh-storno- korundu)
✓ Pages+Bilesenler+Layout satır içi style=     110 (tavan 196)    ← bu paket 0 ekledi
```

## 8. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Açıklama |
|---|---|---|
| — (yeni varlık YOK) | — | Bu paket veri modeli, PATCH ya da DELETE ucu açmadı. |
| Manuel kur kaydı | yeni giriş (çekmece) + gerekçeli iptal (ortak onay) | Mevcut yollar. Form çekmeceye taşındı, iptal yıkıcı onay olarak `ModalPencere`'de kaldı. |
| Dış kaynaklı kur kaydı | salt okunur | `DegismezIpucu KurKaydi`: düzeltilmez, yenisi çekilir (profesyonel yüz). |
| Emanet hareketi: açıklama | `SatirIciDuzenle` → `PATCH /emanet/hareketler/{id}` | Mevcut. |
| Emanet hareketi: has gram · maden · müşteri | **kilitli**, ayrıntı çekmecesinde gerekçeli | `DegismezIpucu EmanetHareketiKilitli` ve düzeltme yolu: **Geri al (düzeltme fişi)**, yani 48-A `POST /islemler/{id}/storno` ortak onayla, ardından yeni giriş. Finansal kayıtta hard DELETE yok. |
| Vitrin pano kalemi | mevcut çekmece (`PATCH /pano/ayarlar/{kod}` + varsayılana dön) | Dokunulmadı. |

**Dokunulan sayfalar (DoD EK 2):** `Pages/Kur.razor`, `Pages/Vitrin.razor`, `Pages/Emanet.razor`.

## 9. Açık kalanlar (sahibiyle)

1. **Son tam süitteki tek kırmızı test başka bir şeride ait:** `DenetimIziTestleri.DenetimIzi_ManuelKurGirisi_IzdeKalir` (satır 295, "kullanıcısız GÜNCELLEME" iddiası). Bu paket sunucu koduna ve bu teste dokunmadı. Aynı koşum sırasında `Kuyumcu.Tests/DenetimIziTestleri.cs` (01:40) ve `Kuyumcu.Api/Data/DenetimYazmaDinleyicisi.cs` (P-72 notlu) başka bir şerit tarafından değiştirildi. Sahibi **S-B** (sunucu modülleri, P-72).
   - Tam süit 1'deki `UrunEkranTestleri` ×2 kırmızısı (Stok_modali_*) **S3-A**'ya aitti. `Urunler.razor` 01:29'da, test dosyası 01:36'da değişti. Son koşumda ve t5'te yeşil.
2. **Emanet girişinde brüt gram ve ayar defterde saklanmıyor** (§ 2-2). Ayrıntı çekmecesi bunları gösteremiyor. Öneri: `hareketler`'e `gram_agirlik`, `tas_agirlik` ve `ayar_id` kolonları (migration) ve `EmanetModulu.Giris`'in bunları yazması. Sahibi **S-B** (sunucu modülleri). Şu an ekranda dürüst not var, uydurma yok.
3. **denetim.sh `S2-3: ALAN_ISARETI_EKSIK` = 1** (01:49): S-2 şeridinin yarım işi (§ 7). Orkestratörün dalga kapısında ölçülmeli.
4. **Görsel ölçüm** (375/768/1440 taşma, tasarım denetçisi) yapılmadı. Orkestratörün 5199 yayınına ve QA turuna bırakıldı.
5. **Kalıntı dosya:** kimlik envanteri karşılaştırmasında bir komut, proje ağacı dışına bir geçici dosya bıraktı: `/tmp/x` (437 bayt, yalnız Vitrin.razor id listesi, sır yok). Proje ağacında `.mutorj`, `.bak` ya da `.tmp` yok. Hook `rm` komutunu bloklar, bu dosyanın silinmesi kurucuya bırakıldı.

## 10. Kurucuya / mali müşavire not

Bu paket hiçbir mevzuat hükmü yazmadı ve eşik sayısı gömmedi. Emanetin işletme malı olmadığı notu (`EmanetMulkiyetUyarisi`) ve storno kuralları mevcut sunucu davranışından olduğu gibi geliyor.

## 11. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısının bu pakete ait satırları. Sayfalar ağaçta izlenmediği için `??` görünüyor. Ön eki boş satırlar dosya düzeyinde listelenmiyor, çünkü üst dizinleri ağaçta toplu `??` olarak görünüyor:

```
?? dotnet/Kuyumcu.Web/Pages/Kur.razor                         (değişti)
?? dotnet/Kuyumcu.Web/Pages/Vitrin.razor                      (değişti)
?? dotnet/Kuyumcu.Web/Pages/Emanet.razor                      (değişti)
?? dotnet/Kuyumcu.Web/Bilesenler/KurKaynakAdi.cs              (YENİ)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KurVitrinEmanet.cs      (YENİ)
   dotnet/Kuyumcu.Contracts/Sayfalar/EmanetSozlesmeleri.cs    (HareketVeri + Maden = null)
   dotnet/Kuyumcu.Tests/S35KurVitrinEmanetTestleri.cs         (YENİ)
   dotnet/Kuyumcu.Tests/EmanetKurEkranTestleri.cs             (§ 2-3)
   dotnet/Kuyumcu.Tests/S3eKurKaynagiEkranTestleri.cs         (§ 2-3, 2 satır)
   dotnet/Kuyumcu.Tests/UX4cEkranTestleri.cs                  (§ 2-3, M10)
   dokumanlar/09-uygulama-kayitlari/PAKET-S3-5-uygulama-raporu.md (bu rapor)
```

**Mevcut DOM id'leri:** karşılaştırma son eski kopyaya göre yapıldı (`scratchpad/b4iz/…/Pages`, 23.09 18:02; sayfalar git'te izlenmiyor). Kaybolan `id`/`ElemanId`/`Id` sayısı: **Kur 0 (28→39), Emanet 0 (28→38), Vitrin 0 (20→21)**. Yalnız ekleme yapıldı.

Commit yok. Sözlükler.cs'e (ortak hub), Islemler.razor'a, Bilesenler/Fis/*'e, Panel, Cariler ve Urunler'e ve sunucu modüllerine dokunulmadı.

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Kur ekranında HAS önde | ✓ | S35_01, S35_02; M35-1, M35-7; canlı 2, 7 |
| Vitrin: menüde yok, Tüm ekranlar karosunda ölçüm | ✓ VAR | `Ekranlar.razor` `#ekranlar-alt-vitrin`, S1dEkranlarTestleri (yeşil); dönüş bağlantısı S35_11 |
| Emanet: kilitli alanlar gerekçeli, storno yolu ekranda | ✓ (has/maden/müşteri; brüt gram ve ayar saklanmıyor, § 2-2) | S35_14, S35_15, S35_16; M35-10, 11, 12, 14; canlı 11, 13–16 |
| Liste + çekmece şablonu | ✓ | 3 form çekmecede (S35_07, S35_12, S35_13); ModalPencere 55→52 |
| Özellik silinmez (görünür / katlı / profesyonel) | ✓ | S35_03, S35_04; ProAlan + Görünüm bağlantısı |
| Mevcut DOM id'leri değişmez | ✓ | kaybolan id 0 (§ 11) |
| Tek POST yolu / Islemler'e dokunulmadı | ✓ | Islemler.razor ve Fis/* değişmedi |
| Yetki ≠ mod | ✓ | basit yüzde manuel giriş duruyor (S35_03); sunucu kapıları aynı (canlı 5, 13, 16, 18) |
| Jargon ve ham kod 0 (basit) | ✓ | S35_05, S35_06, S35_10, S35_12; M35-2, 3, 4, 5, 8, 9 |
| Sabit renk / satır içi style / CDN yok | ✓ | yeni CSS yok, style sayısı artmadı (denetim) |
| Sozlukler.cs'e dokunulmadı, ayrı dosya TR+EN | ✓ | `Sozlukler.KurVitrinEmanet.cs`, S35_09 |
| Her düğme istek atar ya da kilitli ve sebepli | ✓ | § 4 düğme denetimi |
| Boş/geçersiz form için alan altı mesaj | ✓ | S35_08; M35-16 |
| UX-S28 hata ≠ kayıt yok | ✓ | S35_18; M35-13; denetim 47→45 |
| Paket kapısı (1) derleme 0 hata | ✓ | 0 uyarı, 0 hata (build3.log) |
| (2) kendi testlerim 0 başarısız | ✓ | 506/506 + 88/88 |
| (3) kendi sayaçlarım ✓ | ✓ | 01:35 DENETİM TEMİZ; 01:49 koşumunda bu paketin sayaçlarının hepsi ✓, tek ihlal S-2'nin S2-3 sayacı (§ 7) |
| (4) mutasyon | ✓ | 16/16 kırmızı, cmp birebir |
| (5) canlı | ✓ | § 6, 18/18 adım beklenen kodda |
| (6) test kiracısı silindi | ✓ | 70 tablo, kalan 0; tenants 10→10 |
| (7) yeni kırmızı üretilmedi | ✓ | son tam süitte tek kırmızı S-B'ye ait (§ 9-1) |
