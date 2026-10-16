# PAKET S3-6: Kasalar + Bildirimler (uygulama raporu)

**Tarih:** 2026-09-24 · **Dalga:** S3B, günlük ekranlar B · **Rol:** Taşkın (backend) + Ertuğ (frontend), Opus
**Spesifikasyon:** `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 3, § 5, § 5B, § 6 (S-3) + orkestratör ek koşulu: "Kasalar tur 8'de HİÇ ölçülemedi (bozuk yayın) — P-43 sonrası yeniden ölç, sonra şablonu uygula. Bildirimler: kapalı zamanlanmış iş arayüzden AÇILABİLSİN (tur 8 bulgusu). Liste+çekmece şablonu, boş durum = 1 cümle + 1 düğme."
**Durum:** BİTTİ. Paket kapısının 7 maddesi sağlandı (§ 10). Tek sapma: **kurulum ayarında** kapalı olan iş ekrandan açılmaz. Gerekçesi § 2-1'de.

---

## 1. Yapılanlar

### Kasalar (`Pages/Kasalar.razor`)

| # | Ne | Ayrıntı |
|---|---|---|
| 1 | **Yeniden ölçüm (ek koşul).** Tur 8'de ölçülemeyen ekran koda karşı ölçüldü | Kart başına **6 eylem** vardı: ✏️ (metinsiz ikon), Hareket, Geçmiş, Günü Kapat, Pasife Al, Aktife al. Plan § 5B md.5'e göre ikon metinsiz olmaz. **Çok alanlı 4 form `ModalPencere` içindeydi** (yeni kasa, hareket, geçmiş, gün sonu), oysa DoD EK md.2 düzenlemeyi Cekmece'ye bağlar. Yeni kasa formunda şube seçilemiyordu (UX-S26). Para birimi rozeti ve döviz seçicisi ham kod basıyordu (`TRY`, `EUR`; UX-S31). Boş durumda düğme yoktu. `catch` içinde 3 yol veriyi düşürüyordu (UX-S28). |
| 2 | **Kartta en çok 3 eylem ve "⋯"** (taslak n3) | Aktif kasada görünen eylemler: **+ Hareket · Geçmiş · 🌙 Günü Kapat · ⋯**. "⋯" penceresinde **✏️ Düzenle** ve **🗑 Pasife Al** var; bakiyeli kasada Pasife Al kilitli, sebebi de düğmenin altında yazılı. Pasif kasada görünen eylemler: **♻ Aktife al · Geçmiş · ⋯**. Bu kartta Hareket düğmesi pencerede kilitli durur; sebebi kartta ve düğmenin `title`'ında yazılı. Pencere olarak yerli HTML `popover` kullanıldı: **JS yok, CSP istisnası yok.** S3-2 Cariler desenidir, yeni CSS yazılmadı. **Silinen özellik yok.** |
| 3 | **Liste ve çekmece** (DoD EK md.2) | Yeni kasa (`#cekmece-kasa-yeni`), hareket (`#cekmece-kasa-hareket`), gün sonu (`#cekmece-kasa-gun`) ve geçmiş (`#cekmece-kasa-gecmis`) çekmeceye taşındı. `ModalPencere` yalnız yıkıcı onay için kaldı: storno ve pasife alma. Sayfadaki `ModalPencere` sayısı **6'dan 2'ye** indi. İç kimliklerin hepsi korundu (`kasa-ad`, `kasa-doviz`, `kasa-olustur`, `h-*`, `gun-*`, `kh-*`). Çekmecelerin kendi Kaydet düğmesi çizilmez (`KaydetGoster=false`), böylece tek birincil düğme kalır. S3-5 deseni izlendi. |
| 4 | **Geçmiş çekmecesi ve storno onayı üst üste binmez** | Storno onayı açılınca geçmiş çekmecesi gizlenir, ama kasa bağlamı (`gecmisKasaId`) korunur çünkü storno ucu onu kullanır. Vazgeç'e basılınca aynı kasanın geçmişi geri gelir. Storno bitince de geçmiş yenilenmiş hâliyle açılır. Böylece iki odak tuzağı üst üste binmiyor. |
| 5 | **UX-S26 / UX-S22: yeni kasa ile düzenleme aynı alanları taşır** | Yeni kasa çekmecesinde ad, döviz ve **şube** var. Tek şube varsa seçici çizilmez; şube salt okunur satırda yazar (`#kasa-sube-tek`) ve gövdeye kendiliğinden girer. Birden çok şube varsa seçici çıkar (`#kasa-sube`), ilk şube önceden seçilidir. Eski kod şubeyi sessizce `subeler[0]` yapıyordu. |
| 6 | **UX-S31: ham kod yok** | Rozette "Euro", "Türk lirası · Pasif" yazar. Döviz seçicisinde görünen ad okunur, gövdeye giden değer yine koddur (`TRY`). İki çekmece tek diziden beslenir (`DovizSecenekleri`, UX-S26). Bilinmeyen para birimi kendi koduyla görünür, ad uydurulmaz. |
| 7 | **İki yüz, tek gövde** | "Kasa sayısı" ve "Aktif kasa" özet kartları `ProAlan` içine alındı. Basit yüzde yalnız **Toplam TL Bakiye** görünür; etiket hangi para birimini topladığını söyler (UX-S32). Profesyonel yüz bugünkü gibi 3 kart gösterir. Basit yüzde de her eylem duruyor. |
| 8 | **Boş durum = 1 cümle + 1 düğme** | "Henüz kasa açılmamış." ve altında **+ Yeni kasa** (`#kasa-bos-yeni`) var; düğme çekmeceyi açar. Boş durum yalnız istek başarılı olup liste gerçekten boşsa çizilir. Canlıda yeni kiracının 0 kasayla başladığı ölçüldü (§ 6 adım 3), yani bu yol gerçekten kullanılıyor. |
| 9 | **UX-S28: hata "kayıt yok"a çevrilmiyor** | (a) Liste hatası son bilinen listeyi artık silmiyor (`KasaListesi = []` kaldırıldı). (b) Başka bir kasanın geçmişi açılırken liste istekten **önce** boşaltılıyor, `catch` içinde değil. Eski kasanın satırları yeni kasanın başlığı altında görünmüyor ve hata "hareket yok" boş durumuna dönmüyor. (c) Kur okunamazsa sebep sessizce yutulmuyor; geçmiş çekmecesinde yazılıyor (`#kh-kur-hata`). Bu sayfada `DIS_HATADA_VERI_DUSUREN` sayısı **3'ten 0'a** indi. |
| 10 | **UX-S30** | Gün sonunda fark varken gerekçe girilmemişse kaydet düğmesi kilitli olur ve sebebi düğmenin altında yazar (`#gun-kapat-kilit`). Eskiden sebep yalnız alanın ipucundaydı. Hareket çekmecesinin tutar alanı kasanın para birimini taşır (UX-S21). |

### Bildirimler (`Pages/Bildirimler.razor`)

| # | Ne | Ayrıntı |
|---|---|---|
| 11 | **Kapalı hatırlatma arayüzden açılır (tur 8 bulgusu 28)** | Ölçüm şunu gösterdi: iş paneli `ProAlan` içindeydi, yani **basit yüzde** sahibin kapattığı bir hatırlatmayı yeniden açmanın hiçbir yolu yoktu. Yeni kiracının varsayılanı da basit yüzdür (plan D1). Artık basit yüzde (`ProAlan` `BasitAlternatif`) SAHİP ve MÜDÜR için **"Kapalı hatırlatmalar"** kartı çıkıyor (`#bildirim-kapali-isler`). Kapalı her iş tek satırda gösterilir ve **Aç** düğmesiyle (`#is-ac-{kod}`) açılır. Bu düğme profesyonel yüzdeki kutuyla **aynı** ucu (`PATCH /isler/ayarlar`) ve aynı gövdeyi kullanır; gövdede yalnız `kapaliIsler` gider, diğer ayarlar korunur. Sonuç banda yazılır: "{iş adı} yeniden açıldı." ve sunucunun mesajı. Profesyonel yüzde kutu olduğu için kart çizilmez; aynı soruya iki cevap verilmez. |
| 12 | **Kurulum ayarında kapalı iş** | Aç düğmesi kilitli, olay işleyicisi yok. Gri "Kurulumda kapalı" rozeti var. Sebep ve yol sade dille yazılı: "Bu hatırlatma kurulum ayarında kapalı. Açılması için kurulumu yapan kişiye söyleyin." Sunucu da aynı cevabı verir. Gerekçe § 2-1'de. |
| 13 | **Boş durum = 1 cümle + 1 düğme** | Açık sekme boşsa **Kapatılanlara bak** düğmesi çıkar. Kapalı ya da Tümü sekmesi boşsa **Açık bildirimlere dön** çıkar. İkisi de gerçek bir GET isteği atar. |
| 14 | **Tur 8: "sekme değişince açıklama güncellenmiyor"** | SayfaBaşı açıklaması seçili sekmeyi anlatır: Açık, Kapatılan ya da Tümü. |
| 15 | **Tur 8: "Yenile sessiz"** | Elle yenileme sonucu banda yazar: "Liste yenilendi: N bildirim." (UX-S27). Hata varsa başarı bandı çıkmaz. |
| 16 | **Ham kod 0** (profesyonel iş paneli) | Çalışma geçmişinde iş kodunun yerine sunucunun verdiği ad görünür (`VADE_HATIRLATMA` yerine "Vade hatırlatma"). Durum sütununda `BASARILI/HATALI/BASLADI` yerine "Başarılı/Hatalı/Sürüyor" yazar. İşler tablosundaki "son durum" sütunu da aynı biçimde çevrildi. |
| 17 | **DoD/1 sayacı** | Sayfadaki düğmelerde `@onclick` ilk satıra alındı. Bildirimler'de denetim sayacına giren "olay işleyicisiz görünen" düğme **7'den 0'a**, Kasalar'da **6'dan 2'ye** indi. Kalan 2 düğme yerli popover'ın "⋯" ve "Kapat" düğmeleridir. İkisi `popovertarget` ile tarayıcıda pencereyi açıp kapatır; S3-2 Cariler'de de böyledir. |

### Ortak

| # | Ne | Nerede |
|---|---|---|
| 18 | Metinler ayrı dosyada, TR ve EN. Konumsal record kullanıldı; bir dilde anahtar eksikse derleme kırılır. Para birimi ve koşum durumu çevirisi **tek yerde** (`ParaAdi`, `DurumAdi`) | `Kuyumcu.Core/I18n/Sozlukler.KasaBildirim.cs` (YENİ) |
| 19 | 30 yeni bUnit testi | `Kuyumcu.Tests/S36KasaBildirimTestleri.cs` (YENİ) |
| 20 | Test düzeneği uyarlaması. `KasaKapatmaTestleri` ve `GunKapanisiEkranTestleri` sınıflarına `ArayuzModuServisi` kaydı eklendi, çünkü özet kartları artık `ProAlan` içinde ve `ProAlan` mod servisini istiyor. Varsayılan mod PROFESYONEL olduğu için DOM bugünküyle aynı. **Hiçbir iddia gevşetilmedi.** | `Kuyumcu.Tests/ErisimKapatmaTestleri.cs` (2 satır) |

**Yeni CSS yok · satır içi style yok (0) · sabit renk yok · CDN/script yok · sunucuya dokunulmadı · migration yok.**

## 2. Sapmalar ve gerekçeleri

1. **"Kapalı zamanlanmış iş arayüzden açılabilsin" için ekip oyu.** Kapanmanın iki ayrı sebebi var ve ikisi ayrı değerlendirildi:
   - **Kiracının kendi kapattığı iş.** Artık **iki yüzde de** arayüzden açılıyor (§ 1-11). Tur 8'de ölçülen asıl boşluk buydu: basit yüzde bu işi açacak bir yol yoktu.
   - **Kurulum ayarında (`appsettings Isler:*:Etkin=false`) kapalı iş.** Bu iş ekrandan **açılmaz**; kilitli, gerekçeli ve sade dille yolu yazılı. Oy: Bertan, Mehmet, Taşkın ve Ertuğ oybirliğiyle. Gerekçeler:
     (a) Bu **P-11'in mühürlü fail-closed kuralıdır**: "ayar yalnız DARALTIR". `IsAyarServisi` ve `IsCalistirici` bunu sağlar, `P11IsAyarTestleri.Yapilandirmada_KAPALI_is_kiraci_ayariyla_ACILAMAZ` da sınar. Kiracının bu işi açabilmesi, kurucunun ya da ByGatech'in işletme düzeyindeki kapatma anahtarını devre dışı bırakır.
     (b) **Mevzuat kuralı** (kurucu talimatı, 23.09): kurulumda kapalı tutulan tek aday TEVSIK_KURTARMA. Dayanağı birincil kaynaktan **doğrulanmadığı** için `appsettings` notuna göre varsayılan KAPALI. Doğrulanmamış hükme dayanan bir işi kiracının açabilmesi fail-closed ilkesini çiğner.
     (c) Değişiklik **sunucu modülü** (`Modules/Isler`) ister. Bu dosya bu şeridin alanında değil ("S-B: sunucu modülleri — DOKUNMA").
     (d) Ölçüm: bugünkü üç işin üçü de kurulumda **açık** (`appsettings.json` → Vade, Masak ve Saklama için `Etkin: true`; canlı adım 14: üçünde de `yapilandirmaEtkin=True`). Kurulumda kapalı iş şu an **yalnız teoride** var; kullanıcının karşılaştığı durum kiracının kapattığı iştir.
   Kurulum düzeyinde açılabilirlik istenirse sunucuya yeni bir "kurulum varsayılanı / kilit" ayrımı gerekir; bu bir S-B paketi olur (§ 9-2).
2. **Spesifikasyon doğrulaması.** "Kasalar tur 8'de hiç ölçülemedi" iddiası doğru (`QA-TARAYICI-TURU-…-tur8.md` satır 10: ENGELLENDİ). Ölçüm koda karşı yapıldı (§ 1-1). Tarayıcıda görsel ölçüm yapılmadı (§ 6 sonu).
3. **Kasalar tablo değil kart listesi olarak kaldı.** Liste + çekmece şablonunun bağlayıcı üç kuralı uygulandı: satırda en çok 3 eylem ve "⋯", düzenleme çekmecede, liste kaybolmuyor. Kart düzeni **korundu**. Gerekçe: bir dükkânda 1–4 kasa olur, büyük bakiye sayısı (36 px `stat-deger`) kartta okunur. Ayrıca `.stat-izgara > .kart` yapısına bağlı 9 mevcut test (`KasaKapatma`, `GunKapanisiEkran`) değişmeden yeşil kaldı. Tuvalde (`tuval/project/`) Kasalar için ayrı bir taslak yok; `Musteriler.dc.html` tablo desenini gösteriyor. Tuvalden sapma yok, çünkü Kasalar için bir taslak tanımlanmamış.
4. **Gün sonu kapanışı çekmecede.** Bu işlem yıkıcı değil: sunucu fark varsa gerekçe istiyor ve gerekçeli yeniden açma ucu var (`/gun-kapanisi/{tarih}/ac`). Bu yüzden "yıkıcı onay" değil, form sayıldı.
5. **Taban tam süit bu sefer derlendi** ve **4720/4720 yeşil** geçti (bilinen kırmızı YOK; § 4).

## 3. Migration

Gerekmedi. Şema değişikliği yok, sunucuya dokunulmadı.

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| **Taban tam süit** (02:08, `trx-pS3-6-taban/_Mac_2026-09-24_02_08_51.trx`) | 4720 | 4720 | 0 | Kırmızı listesi **boş** |
| İlgili mevcut ekran testleri (13 filtre: BildirimEkran, ErisimKapatma, UX1C2Ekran, P19VirmanBacagiEkran, GiderEkran, UX4cEkran, TasarimBelirtec, OlcumKapisi, Sozlesme*, S1d, P36c, P39, P37) | 394 | 394 | 0 | `pS3-6/t1.log` |
| S36 (yeni) | 30 | 30 | 0 | `pS3-6/t3.log` |
| Tam süit 1 (`pS3-6/tam.log`) | 4761 | 4752 | **9** | **Bu paketin ürünü:** `KasaKapatmaTestleri` ×3 ve `GunKapanisiEkranTestleri` ×6 hata verdi: "no registered service ArayuzModuServisi". Sebep: ProAlan artık sayfa düzeyinde. Bu iki sınıf `ErisimKapatmaTestleri.cs` dosyasında olduğu için filtrem (`~ErisimKapatma`) onları kaçırmıştı. **Düzeltildi** (§ 1-20). |
| Düzeltme sonrası (KasaKapatma + GunKapanisiEkran + KullaniciErisim + S36) | 59 | 59 | 0 | `pS3-6/t4.log` |
| **Tam süit 2, son** (`pS3-6/tam2.log`, `trx-pS3-6/`) | **4778** | **4776** | **2** | İkisi de SERİT B P-73'e ait (§ 10). Test sayısı: taban 4720 → 4778 (+30 bu paket, gerisi öteki şeritler) |

**Yeni testler (30):**
- **Kasalar:**
  - S36_01: aktif kartta 3 eylem ve "⋯"; Düzenle ile Pasife al pencerede, satırda değil; `popovertargetaction=hide`.
  - S36_02: pasif kartta ilk eylem Aktife al; Hareket pencerede, kilitli ve sebepli.
  - S36_03: bakiyeli kasada Pasife al kilitli, sebep yazılı; boş kasada açık.
  - S36_04: para birimi kodu kartta ve seçicide görünmüyor; gövdeye kod gidiyor.
  - S36_05 / S36_06: basit yüzde 1 özet kartı, profesyonelde 3.
  - S36_07: yeni kasa çekmecede; tek şubede seçici yok; POST gövdesi ayrıştırılıyor (ad, USD, s1).
  - S36_08: çok şubede seçici var; seçilen şube gövdeye gidiyor.
  - S36_09: boş adda alan altı mesaj çıkıyor, istek atılmıyor.
  - S36_10: boş durumda 1 cümle ve 1 düğme; düğme çekmeceyi açıyor.
  - S36_11: tazeleme hatası son listeyi silmiyor.
  - S36_12: ilk okuma hatasında boş durum yok, hata bandı var.
  - S36_13: hareket çekmecede; gövde ayrıştırılıyor; yeni bakiye banda yazılıyor.
  - S36_14: gün sonunda fark varken kilit ve sebep; gerekçe girilince açılıyor.
  - S36_15: storno onayı açılınca geçmiş gizleniyor, vazgeçilince geri geliyor.
  - S36_16: storno doğru uca gidiyor; sonuç banda yazılıyor, geçmiş geri geliyor.
  - S36_17: başka kasanın geçmiş hatasında eski satırlar görünmüyor, boş durum yok.
  - S36_18: kur hatasının sebebi yazılı; defter bacağı kilitli, elle hareket açık.
  - S36_19: kart ve SayfaBaşı'ndaki **her** düğme (21 düğme) ya iş yapıyor (istek, çekmece ya da onay açıyor) ya kilitli ve sebepli, ya da var olan bir popover'ı hedefliyor.
  - S36_20: TR/EN sözlük eksiksiz; çevirici bilinmeyen kodda ad uydurmuyor.
- **Bildirimler:**
  - S36_21: basit yüzde kapalı hatırlatma kartı var, iş paneli yok; kartta ham kod yok.
  - S36_22: Aç düğmesi PATCH atıyor; gövdede **yalnız** `kapaliIsler` var ve açılan kod listede değil; bantta iş adı ve sunucu mesajı var.
  - S36_23: kurulumda kapalı iş kilitli; title, sebep ve yol yazılı; olay işleyicisi yok, istek atılmıyor.
  - S36_24: tezgâhtar kartı görmüyor.
  - S36_25: kapalı iş yoksa kart çizilmiyor.
  - S36_26: profesyonel yüzde tek cevap var: kutu var, kart yok.
  - S36_27: boş durumda 1 cümle ve 1 düğme; düğme `durum=KAPALI` GET'i atıyor, sonra düğme metni değişiyor.
  - S36_28: sekme değişince açıklama değişiyor.
  - S36_29: Yenile bildirim sayısını banda yazıyor.
  - S36_30: iş panelinde ham durum ve iş kodu yok.

## 5. Mutasyon kanıtları

Yöntem şöyle işledi:
1. Kaynak `scratchpad/mut-yedek/S3-6/<kod>-<dosya>.orj` olarak kopyalandı.
2. Değişiklik yapılmadan önce eşleşme sayısının **tam 1** olduğu doğrulandı; ardından değişiklik düz metin değiştirmeyle yapıldı.
3. Derleme ayrı koşuldu ve "0 Hata" doğrulandı. Eşzamanlı şeritlerden gelen MSB4018 kilidine karşı en çok 4 deneme yapıldı.
4. Filtreli test koşuldu. Filtre: `S36|ErisimKapatma|UX1C2Ekran|BildirimEkran|P19VirmanBacagiEkran|GiderEkran`.
5. Yedek geri kopyalandı ve `filecmp` ile birebir aynı olduğu doğrulandı.

Betik `scratchpad/pS3-6/mut.py`, sonuçlar `mut-sonuc.txt` ve `mut-sonuc2.txt`. Son mutasyondan sonra iki dosya ilk yedekle **cmp birebir** çıktı, `dotnet build Kuyumcu.sln` sonucu **0 Hata / 0 Uyarı**.

**Bayat ikili uyarısı (Anayasa md.1):** İlk koşuda M36-15 ve M36-18 bir önceki mutasyonun kırmızı testini birebir tekrarladı: M36-15 S36_25'i, M36-18 S36_23'ü verdi. Bu, ikilinin bayat olduğunu gösteriyor (artımlı derleme ya da eşzamanlı şerit derlemesi). Bu iki koşum **geçersiz sayıldı**. Aynı riski taşıyan M36-2, M36-9 ve M36-22 de (kırmızı testleri bir öncekiyle aynıydı) `--no-incremental` tam derlemeyle **yeniden** koşuldu (`mut-sonuc2.txt`). Aşağıdaki tabloda bu beş mutasyonun yeniden koşum sonucu yer alıyor.

| Kod | Değişiklik (kuralı kır) | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M36-1 | Kart rozetinde para birimi kodu (`kasa.ParaBirimi`) | S36_04 | birebir |
| M36-2 | Döviz seçicisinin görünen metni = kod | S36_04 (tam derleme ile yeniden koşum) | birebir |
| M36-3 | Özet kartları her modda (`ProAlan ZorunluGorunur="true"`) | S36_05 | birebir |
| M36-4 | Seçilen şube yok sayılır (`subeler[0].Id`) | S36_08 | birebir |
| M36-5 | Tek şubede de seçici (`Count > 0`) | S36_07 | birebir |
| M36-6 | Liste hatasında `KasaListesi = []` geri geldi | S36_11 | birebir |
| M36-7 | Başka kasaya geçerken eski geçmiş boşaltılmaz | S36_17 | birebir |
| M36-8 | Storno onayı açılınca geçmiş çekmecesi gizlenmez | S36_15 | birebir |
| M36-9 | Vazgeç'te geçmiş geri gelmez | S36_15 (tam derleme ile yeniden koşum) | birebir |
| M36-10 | Kur hatası sessizce yutulur | S36_18 | birebir |
| M36-11 | Gün sonu kilit sebebi hiç yazılmaz | S36_14 | birebir |
| M36-12 | Bakiyeli kasada Pasife al kilitsiz | S36_03 | birebir |
| M36-13 | Boş durum düğmesi kaybolur (id değişir) | S36_10 | birebir |
| M36-14 | Kapalı iş olmasa da kart çizilir | S36_25 | birebir |
| M36-15 | Aç düğmesi işi açmaz, **kapatır** (`false`) | S36_22 (tam derleme ile yeniden koşum) | birebir |
| M36-16 | Aç sonucu banda yazılmaz | S36_22 | birebir |
| M36-17 | Kurulumda kapalı işin düğmesi açık ve işleyicili | S36_23 | birebir |
| M36-18 | KAPALI sekmesinde açıklama değişmez | S36_28 (tam derleme ile yeniden koşum) | birebir |
| M36-19 | Yenile yine sessiz (`@onclick="Yukle"`) | S36_29 | birebir |
| M36-20 | Boş durum düğmesi yanlış sekmeye gider | S36_27 | birebir |
| M36-21 | Çalışma geçmişinde ham durum kodu | S36_30 | birebir |
| M36-22 | Çalışma geçmişinde ham iş kodu | S36_30 (tam derleme ile yeniden koşum) | birebir |
| M36-23 | Pasif kasanın kilitli Hareket düğmesi kaybolur | S36_02, UX1C2EkranTestleri.Kasalar_pasif_kasada_HAREKET_dugmesi_KILITLI_ve_sebebi_yazar | birebir |

**23 mutasyonun 23'ü kırmızı. Test açığı yok.** Eşdeğer olduğu için koşulmayan tek mutasyon var: kartı saran `YoneticiMi &&` koşulunun kaldırılması. Tezgâhtarda `/isler` hiç çağrılmıyor (`IsleriYukle` bekçisi), `isler` null kalıyor ve kart zaten çizilmiyor. S36_24 bu davranışı sınıyor; koşul derinlemesine savunma olarak duruyor.

## 6. Canlı doğrulama

**Ortam ve kurulum:**
- Port **5190** kullanıldı (ek koşuldaki izolasyon portu). Veritabanı `kuyumcu_net`.
- API şu komutla başlatıldı: `dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5190`. Başlatan süreç 28597, dinleyen süreç 28603.
- Kiracı tek kullanımlık bir davetle açıldı: `kiraci_davetleri` satırı `PAKETS3-6-DAVET-01`, SHA-256 özetiyle. Jeton, parola ve e-posta ekrana basılmadı; jeton dosyası iş bitince boşaltıldı.
- Kiracı: **PAKETS3-6-TEST** (`01M38AF2CYMCP8EQCP0SD7VJEK`).
- Betik `scratchpad/pS3-6/canli.py`, çıktı `canli.json`. Yanıtların hepsi okundu.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit (davetli) | 201 | 201 | tenantId alındı |
| 2 | GET /subeler | 200, 1 şube | 200 | 1 şube, yani ekranda şube seçicisi gizli ve `#kasa-sube-tek` çiziliyor (UX-S22) |
| 3 | GET /kasalar | 200 | 200 | **0 kasa**: yeni kiracı boş durumla başlıyor ("Henüz kasa açılmamış." + Yeni kasa) |
| 4 | POST /kasalar {ad, subeId, paraBirimi:USD} (yeni kasa çekmecesinin gövdesi) | 200/201 | 200 | id var, paraBirimi=USD, subeId eşleşti |
| 5 | POST /kasalar/{id}/hareket GIRIS 100 | 200 + bakiye | 200 | bakiye=100.0 (banda yazılan değer) |
| 6 | POST hareket CIKIS 30 | 200, bakiye 70 | 200 | bakiye=70.0 |
| 7 | PATCH /kasalar/{id} ad ("⋯" → Düzenle çekmecesi) | 200 | 200 | ad="S36 Dolar Kasası" |
| 8 | DELETE /kasalar/{id} bakiyeli (Pasife al kilidi) | 400/409 | 400 | "Bakiyesi sıfır olmayan kasa deaktif edilemez" (ekrandaki kilit sebebiyle aynı kural) |
| 9 | GET /kasalar/{id}/hareketler (geçmiş çekmecesi) | 200, 2 satır | 200 | 2 satır, ödeme türü NAKIT |
| 10 | POST …/hareket/{hid}/storno (onay) | 200 + stornoKasaHareketId | 200 | stornoKasaHareketId var |
| 11 | GET /kasalar | bakiye 100 | 200 | bakiye=100.0 |
| 12 | POST gun-kapanisi, fark −10, gerekçesiz | 400 | 400 | "…10,00 TL fark var… Fark varsa gerekçe ZORUNLUDUR." (ekranda düğme zaten kilitli, S36_14). ⚠ Kasa **USD** olduğu hâlde mesaj "TL" diyor: sunucu tarafında UX-S32 ihlali (§ 9-1) |
| 13 | POST gun-kapanisi, fark −10, gerekçeli | 200 | 200 | kasaId, yerelGun=2026-09-24, fark=−10.0 |
| 14 | GET /isler | 200 | 200 | 3 iş: MASAK_SIB_SURESI, SAKLAMA_ONIZLEME, VADE_HATIRLATMA; üçünde de yapilandirmaEtkin=True, kiraciKapatti=False, etkin=True |
| 15 | PATCH /isler/ayarlar kapaliIsler=[MASAK_SIB_SURESI] | 200 | 200 | kapaliIsler=['MASAK_SIB_SURESI'] |
| 16 | POST /isler/MASAK_SIB_SURESI/calistir (kapalıyken) | 400 | 400 | "Bu iş bu işletmenin ayarlarında kapalı: MASAK_SIB_SURESI" |
| 17 | GET /isler (kartın verisi) | kiraciKapatti=true | 200 | yapilandirmaEtkin=True, kiraciKapatti=True, etkin=False, ad="MASAK bildirim süresi". Basit yüzde bu iş kartta, **Aç** düğmesi açık |
| 18 | PATCH /isler/ayarlar kapaliIsler=[] (**Aç** düğmesinin gövdesi) | 200 + mesaj | 200 | kapaliIsler=[], mesaj="Zamanlanmış iş ayarları kaydedildi", baslangicSaat=0 (değişmedi) |
| 19 | GET /isler | etkin=true | 200 | kiraciKapatti=False, etkin=True |
| 20 | POST …/calistir (açıldıktan sonra) | 200 | 200 | durum=BASARILI, "İş çalıştı: 0 yeni bildirim, 0 tekrar (atlandı)." |
| 21 | GET /bildirimler?durum=ACIK | 200 | 200 | 0 satır: boş durum ve **Kapatılanlara bak** düğmesi |
| 22 | GET /bildirimler?durum=KAPALI (boş durum düğmesinin isteği) | 200 | 200 | 0 satır |
| 23 | GET /isler jetonsuz | 401 | 401 | |

**SQL kanıtı** (`scratchpad/pS3-6/canli-sql.txt`):

```
kasalar:            S36 Dolar Kasası  USD  100.00  aktif 1  sube_var 1
kasa_hareketleri:   100.00 NAKIT "S36 açılış" ters 0
                    -30.00 NAKIT "S36 yanlış çıkış" ters 0
                     30.00 NAKIT "STORNO: S36 canlı: yanlış çıkış girildi, geri alınıyor" ters 1
gun_kapanislari:    2026-09-24  sayılan 90.00  defter 100.00  fark -10.00  "S36 akşam sayımında 10 eksik"
kiraci_ayarlari:    0 satır  (Aç = kapaliIsler=[] → ayar satırı SİLİNDİ, varsayılana dönüldü)
is_calismalari:     MASAK_SIB_SURESI  BASARILI  elle 1
denetim_kayitlari:  IsAyari "…güncellendi (is_kapali_isler)" ×2 · IS_ELLE_CALISTIRMA ×1 · IsCalismasi ×2 — toplam 24 satır
```

**Tarayıcıda görsel doğrulama YAPILMADI.** Ekrana girmek için giriş formuna parola girmek gerekir ve bu ajan bunu yapmaz. Ekran davranışı 30 bUnit testiyle (kimlikler üzerinden) ve uçların canlı yanıtlarıyla doğrulandı. Popover penceresinin tarayıcıda açılıp kapanması, 375/768/1440 px'te taşma ve kart yüksekliği orkestratörün 5199 yayınına ve QA turuna bırakıldı (§ 9-4).

**Kapanış:**
- API durduruldu (`kill 28597 28603`). `lsof -nP -iTCP:5190 -sTCP:LISTEN` **0 satır** döndü.
- PAKETS3-6-TEST kiracısı `tenant_id` kolonu olan **70 tablonun hepsinden** ve `tenants` tablosundan silindi. Kalan satır **0**. Davet satırı da silindi (0).
- `QA-TEST-*` ve `DEMO-*` kiracılarına (5 adet) dokunulmadı.
- `tenants` sayısı başlangıçta **10**, sonda **10**.

## 7. denetim.sh

`bash dotnet/denetim.sh` → **rc=0, DENETİM TEMİZ** (`pS3-6/denetim-son.txt`). Bu paketin iki sayfasındaki ölçüm (önce → sonra; aynı perl/grep komutları):

| Sayaç | Kasalar | Bildirimler | Ağaç geneli (son) |
|---|---|---|---|
| `@onclick'siz ve disabled'sız <button>` (DoD/1) | 6 → 2 | 7 → 0 | 58 (tavan 71; taban 71'di) |
| `ModalPencere` kullanımı (DoD/2) | 6 → 2 | 1 → 1 | 48 (tavan 55; taban 52'ydi) |
| `DIS_HATADA_VERI_DUSUREN_YOL` (UX-S28) | 3 → 0 | 0 → 0 | 41 (tavan 47; taban 45'ti) |
| `SATIR_ICI_STYLE` | 0 → 0 | 0 → 0 | 110 |
| P-12 `STORNO_GORUNMEYEN_EKRAN` · P-19 `VIRMAN_BACAGI_EKRANSIZ_KAPI` · P-11 `BILDIRIM_BASIT_MENUDE` | ✓ | ✓ | ✓ |

Ağaç genelindeki düşüşe başka şeritlerin kapanışları da katkıda bulundu. Cırcır tavanları `denetim.sh` ortak dosya olduğu için **düşürülmedi**; orkestratöre öneri § 9-5'te.

**DOM id sözleşmesi:** Özgün dosya ile yeni dosyanın id envanteri karşılaştırıldı. Envanter `id="…"`, `id="@($"…")"`, `ElemanId` ve `Id` kalıplarıyla çıkarıldı (`comm -23`). **Kaybolan id: Kasalar 0, Bildirimler 0.** Yalnız ekleme yapıldı:
- Kasalar: `kasa-bos`, `kasa-bos-yeni`, `kasa-liste`, `kasa-kart-*`, `kasa-bakiye-*`, `kasa-eylem-*`, `kasa-menu-*`, `kasa-menu-ac-*`, `kasa-menu-kapat-*`, `kasa-duzenle-*`, `kasa-bakiyeli-not-*`, `kasa-sube`, `kasa-sube-tek`, `gun-kapat-kilit`, `kh-kur-hata` ve çekmeceler (`cekmece-kasa-yeni`, `-hareket`, `-gun`, `-gecmis`).
- Bildirimler: `bildirim-bos`, `bildirim-bos-cta`, `bildirim-kapali-isler`, `bildirim-kapali-is-*`, `is-ac-*`, `is-ac-kilit-*`, `is-ac-hata`.

## 8. Değişen dosyalar

Bu paketin yazdığı dosyalar:

```
 M dotnet/Kuyumcu.Web/Pages/Kasalar.razor
?? dotnet/Kuyumcu.Web/Pages/Bildirimler.razor        (depoda izlenmiyordu; bu paket DÜZENLEDİ, yeni değil)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KasaBildirim.cs  (YENİ)
?? dotnet/Kuyumcu.Tests/                            (dizin bütünüyle izlenmiyor; içinde:)
     S36KasaBildirimTestleri.cs  (YENİ)
     ErisimKapatmaTestleri.cs    (2 yerde DI kaydı eklendi)
?? dokumanlar/09-uygulama-kayitlari/PAKET-S3-6-uygulama-raporu.md (bu rapor)
```

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısında öteki şeritlerin ve daha önceki paketlerin yüzlerce satırı da görünüyor (tek commit'li ağaç). Tamamı `scratchpad/pS3-6/git-status.txt` dosyasında. **Dokunulan sayfalar (DoD EK 2):** `Pages/Kasalar.razor`, `Pages/Bildirimler.razor`; ikisi de bu şeridin alanında. Sunucu modüllerine, `Sozlukler.cs` hub'ına, `app.css`'e, ortak bileşenlere ve `denetim.sh`'a dokunulmadı.

## 9. Açık kalanlar

1. **Sunucu, UX-S32 (sahibi S-B / Kasa modülü):** `KasaModulu.cs:346` gün sonu fark mesajı kasanın para biriminden bağımsız olarak "TL" yazıyor. Canlı adım 12'de USD kasada "10,00 TL fark" çıktı. Ekran bu isteği fark varken zaten atmıyor (düğme kilitli), ama mesaj yanlış. Düzeltme sunucu şeridinin işi.
2. **Kurulum düzeyinde kapalı işin açılması (sahibi S-B, oya bağlı):** § 2-1. İstenirse sunucuda "kurulum varsayılanı (kiracı açabilir)" ile "kurulum kilidi (mevzuat, açılamaz)" ayrımı gerekir: `IZamanlanmisIs`'e bir bayrak, `IsAyarServisi`'ne bir `AcikIsler` anahtarı, P-11 testlerinin güncellenmesi. Mevzuat kaynaklı işler (TEVSIK_KURTARMA) kilitli kalır.
3. **Bildirim kartı ile ModalPencere:** "Kapat" onayı (not alanıyla) `ModalPencere`'de kaldı. Geri alınamaz bir damga olduğu için DoD EK md.4 kapsamında yıkıcı onay sayıldı.
4. **Görsel ölçüm:** popover penceresi, 375 px'te kart genişliği (4 eylem sarılıyor, `satir-eylemler` flex-wrap) ve kart yüksekliği tarayıcıda ölçülmedi. Bunlar QA turunda ölçülecek.
5. **Cırcır tavanları:** Ağaç geneli ölçümleri tavanın altında: ölü düğme 58/71, ModalPencere 48/55, veri düşüren 41/47. Orkestratör dalga kapısında tavanları bu değerlere çekebilir.
6. **Tam süitteki başkasına ait kırmızılar (sahibi SERİT B, P-73):** `P73KurZamanlayiciTestleri.KurZamanlayici_BirSaatlikSimulasyon_KiraciBasinaEnFazla60Snapshot` ve `YayinDosyaSozlesmesiTestleri.ProductionSablonuVeOrnegiBirebirAynidir`. Kanıt § 10'da.

## 10. Paket kapısı (paralel şerit kapısı, 7 madde)

| # | Madde | Durum | Kanıt |
|---|---|---|---|
| 1 | dotnet build 0 hata | ✓ | `Kuyumcu.sln` 0 Hata / 0 Uyarı (`pS3-6/b-son.log`, `b6.log`) |
| 2 | Bu paketin testleri 0 başarısız | ✓ | S36 30/30; ilgili sınıflar 394/394; düzeltilen iki sınıf 59/59 |
| 3 | Bu paketin sayaçları ✓ | ✓ | § 7 |
| 4 | Mutasyon kanıtı | ✓ | 23/23 kırmızı, cmp birebir (§ 5) |
| 5 | Canlı doğrulama | ✓ | 23 adım + SQL (§ 6) |
| 6 | Test kiracısı silindi | ✓ | 70 tablo 0 satır, tenants 10 → 10 |
| 7 | Yeni kırmızı üretilmedi | ✓ | Taban kırmızı listesi boştu. Son tam süitte 2 kırmızı var; ikisi de SERİT B P-73'e ait, bu paketin farkı 0 (aşağıda) |

**Son tam süit** (`pS3-6/tam2.log`, `trx-pS3-6/`): **4778 test, 4776 geçti, 2 başarısız.** İki kırmızının ikisi de başka bir şeride ait; bu paketin farkı **0**:

| Kırmızı test | Hata | Sahibi ve kanıt |
|---|---|---|
| `P73KurZamanlayiciTestleri.KurZamanlayici_BirSaatlikSimulasyon_KiraciBasinaEnFazla60Snapshot` (dosya `S3eKurKaynagiHttpTestleri.cs`) | `Expected: 60`, değer farklı | **SERİT B, P-73** (kur zamanlayıcısı; orkestratör 01:05 kaydı: "P-73 `KurModulu.cs`"). Test dosyası **02:48:48**'de değiştirildi, yani bu paketin taban koşumundan (02:08) sonra ve başka bir şerit tarafından. Bu paket kur dosyalarına dokunmadı. |
| `YayinDosyaSozlesmesiTestleri.ProductionSablonuVeOrnegiBirebirAynidir` | `appsettings.Production.json` ile `.ornek` dosyası 1811. karakterde ayrışıyor | `Kuyumcu.Api/appsettings*.json` üçü birden **02:46:24**'te değişti; bu paketin taban koşumundan sonra ve başka bir şerit tarafından (P-73'ün yapılandırma anahtarı). Bu paket `appsettings`'e dokunmadı. |

Tabanda (02:08) iki test de yeşildi; ikisi de bu paketin dokunmadığı dosyalardan kırıldı. Bu paketin ürettiği 9 kırmızı (§ 4, tam süit 1) düzeltildi ve son koşumda yok.

## 11. DoD sözleşmesi

**Yeni varlık ya da kayıt türü açılmadı.** Ekran mevcut uçlara bağlandı. Kayıtların düzenlenme yolları:

| Varlık | Düzenleme / geri dönüş yolu | Ekrandaki yer |
|---|---|---|
| Kasa | PATCH /kasalar/{id} (ad; döviz ve şube K4 kapısıyla) · DELETE = pasife alma · PATCH aktif=true = aktife alma | "⋯" → Düzenle (çekmece) · "⋯" → Pasife Al (onay) · pasif kartta Aktife al |
| Kasa hareketi (finansal) | Hard DELETE YOK. Açıklama: PATCH (SatirIciDuzenle). Tutar ve yön: storno (48-A motoru) | Geçmiş çekmecesi → Storno (onay, gerekçe ≥ 20). Kilitli satırlarda DegismezIpucu, virman bacağında doğru yola bağlantı |
| Gün sonu kapanışı | Gerekçeli yeniden açma ucu (`/gun-kapanisi/{tarih}/ac`, mevcut) | Ekrana bu pakette eklenmedi; kapsam dışı, mevcut durum |
| Zamanlanmış iş ayarı (kiraci_ayarlari) | PATCH /isler/ayarlar (gövdede yok = koru, null/[] = sil) | Profesyonel yüzde kutu, basit yüzde **Aç** |
| Bildirim | Kapatma = damga (silinmez). Kapatılan bildirim değişmez | DegismezIpucu (mevcut) |
| İş koşumu | Değişmez (koşum kanıtı) | DegismezIpucu (mevcut) |

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Kasalar yeniden ölç, sonra şablonu uygula | ✓ | § 1-1 ölçümü, § 1-2…10 |
| Liste + çekmece şablonu (satırda en çok 3 eylem ve "⋯", düzenleme çekmecede) | ✓ | S36_01/02/07/13/14/15; ModalPencere 6 → 2 |
| Boş durum = 1 cümle + 1 düğme (iki ekran) | ✓ | S36_10, S36_27 |
| Bildirimler: kapalı iş arayüzden açılsın | ✓ (kiracının kapattığı iş, iki yüzde) · gerekçeli sapma (kurulumda kapalı iş) | S36_21/22/23/26, canlı 15–20, § 2-1 |
| Özellik silinmez | ✓ | Bütün eylemler duruyor (menüde ya da ProAlan'da); S36_05 |
| Mevcut DOM id'leri değişmez | ✓ | § 7, kaybolan 0 |
| Tek POST yolu (Islemler.razor) | ✓ (dokunulmadı) | — |
| Yetki ≠ mod; eşik istemcide yok | ✓ | Kart yalnız SAHİP/MÜDÜR'e çiziliyor ve sunucu kapısı aynen duruyor (canlı 16). Eşik sayısı yok |
| CDN, npm, inline script, inline style, sabit renk YOK | ✓ | § 7 |
| Sozlukler.cs hub'ına dokunma; ayrı dosya | ✓ | `Sozlukler.KasaBildirim.cs` |
| Şerit dosya alanı | ✓ | Yalnız Kasalar.razor ve Bildirimler.razor (sayfa); test ve sözlük dosyaları yeni |
| İzolasyon (`_z` VT, 5190, build-server shutdown) | ✓ | Her test koşumunda KUYUMCU_TEST_DB=kuyumcu_test_z |
| UX-S21/22/26/27/28/30/31/32 | ✓ | § 1 tablosu |
| Mevzuat kuralı | ✓ | Mevzuat hükmü yazılmadı. Kurulumda kapalı (mevzuat kaynaklı) iş açılamaz kalıyor (fail-closed) |
| Commit yok | ✓ | — |

## 13. Kurucuya ve mali müşavire notlar

- Mali müşavire gidecek yeni bir soru yok. TEVSIK_KURTARMA'nın dayanağı (VUK mük. md. 355/4) hâlâ **DOĞRULANMADI** (mevcut not, `appsettings.json`) ve iş kurulumda kapalı. Bu paket o kapıyı gevşetmedi.
- Kurucuya soru yok. Görsel ölçüm QA turunda yapılacak.
