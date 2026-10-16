# PAKET S3-3 — ÜRÜNLER + ETİKETLER · Uygulama raporu

- Tarih: 2026-09-24 (01:20–03:40 yerel saat)
- Rol: Taşkın (backend) + Ertuğ (frontend) · Dalga S3A (günlük ekranlar A)
- Durum: **BITTI**. Paket kapısının yedi maddesi aşağıda kanıtlarıyla var. Tam süitte kalan tek kırmızı bu paketin değil: tek başına iki kez yeşil geçiyor, sahibiyle birlikte "Açık kalanlar"da yazılı.
- Kaynak: `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § 3, § 5, § 5B, § 6 S-3 · orkestratör koşulu (plan-s3a.js S3-3) · S3-2 şablonu (`PAKET-S3-2-uygulama-raporu.md`) · tuval notu n3.

## 1. Spesifikasyonun koda karşı doğrulanması

| İddia (koşul / QA tur 8) | Koddaki durum (ölçüldü) | Sonuç |
|---|---|---|
| Ürünler: süzgeç satırında 4 açılır liste | `Urunler.razor` Filtreler yuvasında maden, ayar, kategori ve reyon için 4 `Secici` vardı. Durum süzgeci yoktu | Doğru. Durum seçicisi eklendi, 4 seçici katlandı |
| Stok adedi sütunu yok | Liste sütunları: Görsel, Ad, Barkod, "Kategori", Ayar, Gram, Has, Durum. `GET /urunler` (`List<Data.Urun>`) stok alanı taşımıyor | Doğru. Sütun eklendi (bkz. sapma 1) |
| Stok penceresinde ham kimlik | `<td>@s.SubeId</td>` ULID basıyordu. Sayım penceresinde de `@sayimSube` basılıyordu | Doğru. İki yer de şube adına geçti |
| Satırda çok eylem | 4 düğme vardı: Stok, Düzenle, Etiket, Pasife al/Aktife al | "3 + ⋯" düzenine geçti |
| (ek bulgu) Ham kod ekranda | Listedeki "Kategori" sütunu `u.Tip`'i ham basıyordu (ZIYNET, PIRLANTA). Ürün grubu seçenek adları da ham koddu. Maden kilidi metni `'GUMUS' ürün grubu…` diye başlıyor ve sözlükte olmayan çıplak bir metindi | Düzeltildi (UX-S31, plan § 5B md.2) |
| (ek bulgu) UX-S31 ad çakışması | Liste başlığında "Kategori" ürün grubu için kullanılıyordu. Aynı ekrandaki "Kategori" süzgeci ise ağaç kategorisini süzüyordu | Sütun başlığı "Ürün grubu" oldu |
| (ek bulgu) UX-S29 sessiz kırpma | Liste isteğinde `limit` yoktu, sunucu varsayılanı (50) sessizce kırpıyordu | `limit=50` artık açıkça isteniyor, sınır ekranda yazıyor, sınıra değince uyarı çıkıyor |
| Etiketler: sonuçsuz aramada boş durum yok | Tablo yalnız başlıkla kalıyordu | Doğru. Boş durum eklendi |
| Etiketler: liste boşalsa da seçim kalıyor | `secili` kümesi aramadan bağımsızdı. Önizleme ve baskı görünmeyen ürünleri de basıyordu | Doğru. Seçim artık hep görünen listenin alt kümesi |
| Etiketler: "abc" için yanıltıcı mesaj | Seçim yokken kilit sebebi yalnız "Seç" yazıyordu | "Önce listeden ürün seçin." oldu |
| 30 etiketlik önizleme çalışmalı | `secili × kopya` kadar `.urun-etiket` çiziliyor | Korundu: bUnit'te 3×10=30 ölçüldü, canlıda 30 |

**Mevzuat:** Bu paket hiçbir vergi, KDV, MASAK, tevsik ya da KMTS hükmüne dokunmuyor. Sunucu kodu değişmedi. md.11/(e) taş ibaresi ekranda aynı yerlerde görünmeye devam ediyor. Basit modda **taş varsa taş kökeni alanı katlanmıyor**. Çelikte satış fiyatı zorunlu olduğundan o alan da katlanmıyor. Pırlantada 4C açık kalıyor. Eşik sayısı istemciye eklenmedi. WebSearch gerekmedi.

## 2. Yapılanlar

### 2.1 Ürünler — satırda 3 eylem ve "⋯" (iki modda da, S3-2 şablonu)
- Görünen eylemler: **Stok** (pasif satırda yerine **Aktife al**) · **Düzenle** · **⋯**. Hepsi `#u-eylem-{id}` içinde.
- "⋯" penceresi `#u-menu-{id}` yerli HTML `popover="auto"` ile yapıldı: JS yok, CSP istisnası yok. İçinde **Etiket**, **Pasife al** (onay modalı aynen duruyor) ve **Kapat** var. Pasif satırda Stok da buraya geçiyor. **Özellik silinmedi.**
- Menüdeki her düğmede `popovertarget` ve `popovertargetaction="hide"` var: tek tık pencereyi kapatıyor ve düğmenin kendi isteğini gönderiyor. Mevcut `u-aktife-al-{id}` id'si aynen kaldı.

### 2.2 Ürünler — süzgeç satırı: tek seçici ve "Süzgeç ▾"
- Görünenler: arama kutusu (barkod okutma bu kutunun içinde, P-37) ve **Durum** seçicisi (`#urun-durum-filtre`: Hepsi / Aktif / Pasif). Seçim sunucuya `aktif=true|false` olarak gidiyor. Bu, QA tur 8'in "durum süzgeci yok" bulgusunu kapatıyor.
- **Basit modda** maden, ayar, kategori ve reyon seçicileri `#urun-suzgec-ac` ("Süzgeç ▾") düğmesinin arkasında katlı duruyor. Katlanınca değerler korunuyor. Kaç süzgecin etkin olduğu düğmede yazıyor ("Süzgeç (1) ▴"), yani görünmez süzgeç kalmıyor.
- Derin bağlantıyla (P-15, ör. `?maden=ALTIN`) katlı bir seçiciye değer gelirse süzgeç açık başlıyor.
- **Profesyonel modda** dört seçici bugünkü gibi hep açık.

### 2.3 Ürünler — stok adedi sütunu, şube adı, liste sınırı
- Yeni **Stok** sütunu, bütün şubelerdeki adetlerin toplamını gösteriyor (ör. "3 adet"). Sayı bir düğme (`#u-stok-adet-{id}`, UX-S23): tıklanınca şube stokları penceresi açılıyor.
- Adet, görünen satırlar için mevcut `GET /urunler/{id}/stok` ucundan okunuyor (bkz. sapma 1). Bir satırın adedi alınamazsa hücrede "?" yazıyor ve sebep `#urun-stok-hata` bandında görünüyor. Liste yerinde kalıyor (UX-S28). Süzgeç değişirken gelen eski yanıt yeni listeye yazılmıyor (jeton denetimi).
- Stok penceresinde ve sayım penceresinde **şube adı** görünüyor (`GET /subeler`, `#u-stok-sube-{id}`, `#s-sube`). Ad bulunamazsa önce yerleşim yanıtındaki ada, o da yoksa "Tanımsız şube" metnine düşülüyor. **Ham kimlik basılmıyor.** Şube listesi alınamazsa ayrı bir bant çıkıyor (`#urun-sube-hata`), stok tablosu gizlenmiyor.
- Liste isteği `limit=50` gönderiyor. `#urun-sinir` sınırı yazıyor. 50 kayda ulaşılırsa `#urun-sinir-asildi` uyarısı çıkıyor (UX-S29).

### 2.4 Ürünler — sütunlar ve ham kod
- **Basit modda** Barkod, Ürün grubu ve Durum sütunları yok. Barkod aramada ve etikette duruyor. Pasif ürün, ad hücresinde "Pasif" rozetiyle işaretleniyor (`#u-pasif-rozet-{id}`).
- **Profesyonel modda** sütun başlığı "Kategori" yerine "Ürün grubu" oldu. Hücrede kodun karşılığı olan ad yazıyor ("Ziynet", "Pırlanta"). Ürün grubu seçeneklerinin görünen adı da sözlükten geliyor. Seçeneğin değeri hâlâ kod olduğu için sunucu sözleşmesi değişmedi.
- Maden kilidi metni sözlüğe taşındı ve grup adını kullanıyor: "Gümüş ürünü gümüş olur; maden gruptan gelir."

### 2.5 Ürünler — basit modda form en çok 8 alan ve "Daha fazla ▾"
- **Yeni ürün** formunda görünen alanlar: ad, kod, barkod, ürün grubu, maden, ayar, gram ve açılış stoğu (**8**). Kalanlar `#u-daha-fazla` düğmesiyle açılıyor: rodyum, gerçek milyem, birim, taş ağırlığı, işçilik, satış fiyatı, kategori, reyon, konsinye, açıklama, parite, KMTS, RFID, görsel adresi ve kart bölümü.
- **Düzenleme çekmecesinde** görünen girdiler: ad, kod, barkod, ürün grubu, maden, ayar ve gram (**7**). Galeri ve belge yükleyici de katlanan kısımda (`#d-daha-fazla`).
- **Kural kapısı olan alan katlanmıyor:**
  - taş ağırlığı > 0 ise taş kökeni ve taş ibaresi (md.11/(e));
  - ürün pırlantaysa 4C (karat zorunlu);
  - kıymetsiz madende (çelik) satış fiyatı ve "has hesaplanmaz" notu;
  - kartta konsinye işaretliyse konsinye sahibi ve bedeli.
- **Gizlemek silmek değil:** gövde çizimden değil değişkenlerden kuruluyor. Katlı bir alan, düzenlemede sunucudan yüklenen değeri PATCH'te aynen geri yazıyor (S33_18, canlı SQL).
- Katlı bir alana hata düşerse grup **kendiliğinden açılıyor** ve istek gönderilmiyor (S33_19). Bu, hem istemci doğrulaması hem sunucunun 400 yanıtı için geçerli (`KatliHataVarsaAc`).
- Profesyonel modda "Daha fazla" düğmesi çizilmiyor. DOM bugünküyle aynı.

### 2.6 Etiketler (demonun 6. adımı)
- Arama değişince aramaya uymayan seçimler **düşüyor**. Kaç seçimin düştüğü `#et-secim-kaldirildi` içinde yazıyor. Önizleme ve baskı yalnız görünen seçili ürünleri içeriyor. Liste boşalınca basılacak etiket 0, Yazdır kilitli. Arama temizlenince eski seçim geri gelmiyor.
- Sonuçsuz arama için boş durum var (`#et-arama-bos`): `"abc" ile eşleşen ürün yok. Aramayı değiştirin.` Liste gerçekten boşsa "Etiketi basılacak ürün yok." çıkıyor. Hata durumunda boş durum çıkmıyor, hata bandı çıkıyor (UX-S28).
- Seçim özeti `#et-secili-ozet` içinde görünüyor ("3 ürün seçili · 30 etiket"). Seçim yokken kilit sebebi "Önce listeden ürün seçin." oluyor.
- "Tümünü seç" düğmesi seçilecek ürün yoksa, "Seçimi temizle" düğmesi de seçim yoksa kilitli (DoD EK/1). Tümünü seç ile kopya 10 verilince 30 etiketlik önizleme çalışıyor.

### 2.7 Sözlük ve sözleşme
- Yeni dosya `Kuyumcu.Core/I18n/Sozlukler.Urunler.cs` (ortak hub `Sozlukler.cs`'ye dokunulmadı). İçeriği:
  - `UrunListeMetinleri`, `UrunGrubuAdlari`, `EtiketEkranMetinleri`: TR ve EN karşılıkları olan konumsal record'lar. Eksik anahtar derleme hatası veriyor.
  - `UrunGrubuAdlari`, `UrunTipleri`'nin 11 kodunun tamamını karşılıyor (S33_21 sınıyor).
- `Kuyumcu.Contracts/Sayfalar/UrunlerSozlesmeleri.cs` dosyasına iki ekleme yapıldı:
  - `UrunVeri`'ye isteğe bağlı `Birim` alanı. Sunucu bu alanı zaten döndürüyordu, stok sütunu "adet" ya da "g" birimini buradan okuyor.
  - Yeni `UrunSubeVeri(Id, Ad)` kaydı.
- CSS değişmedi. S3-2'nin sınıfları (`.satir-eylem`, `.satir-menu*`, `.form-daha-fazla`) yeniden kullanıldı. `app.css` açılmadı, sabit renk ve satır içi stil eklenmedi.

## 3. Sapmalar ve gerekçeleri
1. **Stok adedi satır başına istekle okunuyor.** Liste ucu stok taşımıyor. Sunucu modülleri bu şeridin alanı değil (orkestratör koşulu: "S-B: sunucu modulleri DOKUNMA"). Toplu stok ucu `raporlar/stok-degeri` ise yalnız SAHİP, MÜDÜR ve MUHASEBE rollerine açık. Tezgâhtar göremediği için bu uç kullanılamadı. Onun yerine herkese açık `GET /urunler/{id}/stok` ucu, görünen en çok 50 satır için paralel okunuyor. **Öneri (backend kuyruğuna):** `GET /urunler` yanıtına `toplamStok` alanı eklenirse tek istekte çözülür ve bu istemci kodu sadeleşir.
2. **"Tek Secici" = Durum seçicisi.** Koşul "süzgeç satırı tek Secici + 'Süzgeç ▾' (4 dropdown katlanır)" diyor. Dört seçicinin tamamı katlanınca görünen tek seçici olarak QA tur 8'in "durum süzgeci yok" bulgusunu kapatan Durum seçicisi seçildi. Durum seçicisi profesyonel modda da görünüyor (yeni özellik). Katlama yalnız basit modda, çünkü profesyonel mod = bugünkü ekran.
3. **Ürünler için ayrı bir taslak yok.** Tuvalde `Urunler.dc.html` bulunmuyor. Koşulun dediği gibi `Musteriler.dc.html` ve not n3 (liste + çekmece, 3 eylem + ⋯, basit form + Daha fazla) şablon alındı.
4. **"⋯" yalnız simge.** S3-2 ile aynı gerekçe: taslak bunu gösteriyor. Düğmede `aria-label="{ad} — Diğer işlemler"` ve `title` var.
5. **Yeni ürün formu ModalPencere'de kaldı.** Formu taşımak mevcut `#u-kaydet` kalıbındaki testleri kırardı. ModalPencere sayacı artmadı (Urunler 5 → 5). Düzenleme zaten çekmecede.
6. **Satır yüksekliği:** 1440 px'te 53 px (basit) ve 63 px (profesyonel) ölçüldü, hedef ≤ 64 px tuttu. **375 px'te 69 px** ölçüldü, hedefin 5 px üstünde. Tablo `KartListe` içinde yatay kayıyor, sayfa taşması 0. Satırı düşürmek için `app.css`'e satır kuralı eklemek gerekiyor. `app.css` bu şeridin dosya alanında değil, bu yüzden açık kaleme yazıldı.
7. **Mevcut testlerde yapılan uyarlamalar:**
   - `UrunEkranTestleri`: iki stok testi artık ham `"s1"` yerine şube adı `"Merkez"` bekliyor. Bu değişiklik paketin hedefinin kendisi.
   - Üç yerde `[role=dialog]` seçicisi `[role=dialog][aria-modal=true]` oldu. Satır menüsü de `role=dialog` taşıdığından seçici modalı ayırt etmeli.
   - Ürün grubu listesi testi artık "PLATIN" yerine "Platin" bekliyor ve ham `DEGERLI_TAS` kodunun görünmediğini sınıyor.
   - Ürünler ekranını çizen üç test sınıfına (`UrunEkranTestleri`, `P10UrunEkranTestleri`, `UrunEkraniTasIbaresiTestleri`) `ArayuzModuServisi` kaydı eklendi.
   - Hiçbir test silinmedi ve hiçbir beklenti gevşetilmedi.

## 4. DoD sözleşmesi (varlık → düzenleme yolu)
Bu paket **yeni varlık ya da kayıt türü açmadı**, migration da yok. Mevcut yolların hepsi korundu ve ekranda bulunabiliyor:

| Varlık / işlem | Uç | Ekran yolu |
|---|---|---|
| Ürün düzenle | PATCH /urunler/{id} [SAHIP, MUDUR, MUHASEBE] | satırda **Düzenle** → çekmece (`#cekmece-urun-kaydet`) |
| Ürün pasife al | DELETE /urunler/{id} (yumuşak) | **⋯ → Pasife al** → onay modalı |
| Ürün aktife al | PATCH /urunler/{id} {aktif:true} | pasif satırda **görünen ilk eylem** (`#u-aktife-al-{id}`) |
| Etiket | GET /urunler/{id}/etiket | **⋯ → Etiket** |
| Şube stoğu (salt okunur) | GET /urunler/{id}/stok | satırda **Stok** ya da stok sayısı. Elle yazılamaz: DegismezIpucu (değişmedi) |
| Sayım düzeltmesi | PATCH /urunler/{id}/stok/{subeId} | stok penceresinde **Sayım Düzelt** (defter üzerinden, değişmedi) |
| Etiket baskısı | — (istemci SVG, ADR-0007) | salt baskı. Kayıt oluşturmuyor |

## 5. Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata**. Son temiz derleme bütün mutasyonlardan sonra alındı (`pS3-3-b8.log`).

Ara derlemelerde üç kez MSB3026, MSB3030 ya da MSB4018 hatası görüldü: dosya başka bir süreç tarafından kullanılıyordu (`Kuyumcu.Web.pdb`, `blazor.build.boot-extension.json`, `tmp-webcil/Kuyumcu.Web.wasm`). Bunlar aynı ağaçta paralel derleme yapan şeritlerin kilitleri. Beklenip yeniden denendi ve üçü de yeşile döndü. C# hatası değiller.

## 6. Testler

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Tam süit ÖNCE (paket başlamadan, 01:21) | 4670 | 4668 | 2 |
| Ürün ve etiket ekranı sınıfları + S33 + P37 + sözlük + tasarım | 307 | 307 | 0 |
| Yeni S33 sınıfları (`FullyQualifiedName~S33`) | 29 | 29 | 0 |
| S33 + UrunEkran + P10Urun + TasIbaresi + UX4b + P16 (son düzeltmeden sonra) | 103 | 103 | 0 |
| **Tam süit SON** (trx: `scratchpad/trx-pS3-3`) | **4720** | **4719** | **1** |

- **ÖNCE koşumunun kırmızıları:** `UX4cEkranTestleri.M10_Ayar_listesi_UC_EKRANDA_TEK_KAYNAKTAN_beslenir` ve `S35EmanetEkranTestleri.S35_12_Giris_CEKMECEDE_ayar_gorunen_adla_govdeye_KOD_gider`. İkisi de Emanet.razor'da, S3-B şeridinin alanında. Son koşumda ikisi de kapalıydı.
- **SON koşumun kırmızısı:** `PusulaEkranTestleri.IptalliPusuladaKaydetKilitli`. Test `GiderPusulalari` ekranını sınıyor ve bu paket o ekrana dokunmadı. Aynı test tek başına **iki kez yeşil** geçti (5/5, 5/5). Test dosyasının başlığı `M55-2 İptal durum kontrolü kaldırılır → IptalliPusuladaKaydetKilitli KIRMIZI` diyor. En olası açıklama: süit derlenirken P-55 şeridinin mutasyon turu aynı ağaçta çalışıyordu.
- **Kapı maddesi 7 — bu paketin ürettiği yeni kırmızı: 0.**

**Yeni testler** (`dotnet/Kuyumcu.Tests/S33UrunlerEtiketlerEkranTestleri.cs`, **29 test**):
- `S33UrunlerEkranTestleri`: S33_01 (Theory, PRO + BASIT) ve S33_02 … S33_22.
- `S33EtiketEkranTestleri`: S33_E1 … S33_E6.

## 7. Mutasyon kanıtları (22/22 kırmızı, 22/22 `cmp` birebir)
Yöntem: güncel kaynak `scratchpad/mut-yedek/S3-3/guncel/` altına kopyalandı. Her mutasyon tek eşleşmeli bir değişiklikti (Python `count==1` denetimi). Değişiklikten sonra `os.utime` yapıldı ve `dotnet build Kuyumcu.Tests` çalıştı. Ardından `dotnet test --no-build --filter "FullyQualifiedName~S33|FullyQualifiedName~UrunEkranTestleri"` koşuldu (71–72 test). Yedek geri kopyalandı, yine `os.utime` yapıldı ve dosya `filecmp` (shallow=False) ile karşılaştırıldı. Betik `scratchpad/mut-s33.py`, sonuçlar `scratchpad/pS3-3-mut-sonuc.json`.

İlk turda iki ölçüm sorunu çıktı:
- **M3-2:** ilk koşumda derleme M3-1'in DLL'ini tazelemedi ve kırmızı `S33_03` (M3-1'in izi) göründü. Tek başına yeniden koşulunca doğru testler kırmızı oldu.
- **M3-8 ve M3-22:** başka şeridin dosya kilidi derlemeyi düşürdü. Beklenip yeniden koşuldu.

Aşağıdaki tablo her mutasyonun **geçerli** koşumunu gösteriyor.

| Kod | Mutasyon | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M3-1 | Etiket menü öğesinden `popovertargetaction="hide"` silindi | S33_03_Menu_yerli_popover_her_oge_pencereyi_kapatir | AYNI |
| M3-2 | Stok toplamı yalnız ilk şubeyi alıyor (`Take(1)`) | S33_05_Stok_adedi_sutunu_subeleri_toplar…, S33_06_… | AYNI |
| M3-3 | Stok hatası sessiz yutuluyor (`Hata: null`) | S33_06_Stok_adedi_alinamazsa_hucre_soru_isareti_ve_HATA_BANDI_liste_kalir | AYNI |
| M3-4 | Bilinmeyen şubede ham kimlik basılıyor | S33_07_Stok_penceresinde_ham_sube_kimligi_YOK_sube_ADI_var | AYNI |
| M3-5 | `SuzgecGorunur => true` (basit modda katlama yok) | S33_08_Basit_modda_dort_secici_KATLI_Suzgec_dugmesi_acar_ve_sayar | AYNI |
| M3-6 | Durum süzgeci uca gitmiyor | S33_10_Durum_suzgeci_aktif_parametresiyle_uca_gider_ve_limit_ACIKCA_istenir | AYNI |
| M3-7 | `limit=50` gönderilmiyor | S33_10_… | AYNI |
| M3-8 | Derin bağlantıda süzgeç açık başlamıyor | S33_11_Derin_baglantidan_gelen_katli_suzgec_ACIK_baslar | AYNI |
| M3-9 | Grup sütunu ham kod basıyor (`@u.Tip`) | S33_14_Profesyonel_modda_grup_sutunu_AD_basar_ham_kod_basmaz | AYNI |
| M3-10 | `YeniTumAlanlar => true` | S33_16_Basit_modda_yeni_form_sekiz_alan…, S33_19_…, S33_20_… | AYNI |
| M3-11 | `KatliHataVarsaAc` etkisiz (`return;`) | S33_19_Katli_alanda_hata_varsa_grup_kendiliginden_acilir_istek_atilmaz | AYNI |
| M3-12 | Çelikte satış fiyatı katlanıyor | S33_20_Kural_kapisi_olan_alan_katlanmaz_celikte_satis_fiyati_gorunur | AYNI |
| M3-13 | PATCH'te `kategoriId` yalnız "Daha fazla" açıkken gidiyor (sessiz silme) | S33_18_Basit_modda_katli_alanlar_PATCHte_AYNEN_geri_yazilir_silinmez | AYNI |
| M3-14 | Etiket: aramada seçim kesişimi yapılmıyor | S33_E2_Arama_degisince_GORUNMEYEN_secim_duser… | AYNI |
| M3-15 | Etiket: sonuçsuz arama boş durumu yok | S33_E1_Sonucsuz_aramada_BOS_DURUM_aranan_metinle_cikar | AYNI |
| M3-16 | Etiket: hata durumunda boş durum gösteriliyor | S33_E6_Hata_bos_duruma_CEVRILMEZ_hata_bandi_cikar | AYNI |
| M3-17 | Pasif satırın menüsünde Stok yok | S33_02_Etiket_ve_Pasife_al_menude_durur_pasif_satirda_Stok_menude | AYNI |
| M3-18 | EN "Süzgeç" = TR | S33_21_Sozluk_TR_EN_dolu_farkli_ve_her_urun_grubunun_adi_var | AYNI |
| M3-19 | Maden kilidi ham kodu yazıyor | S33_15_Maden_kilidi_grup_ADINI_yazar_kodu_yazmaz | AYNI |
| M3-20 | "Tümünü seç" hiç kilitlenmiyor | S33_E4_Secim_yokken_kilit_sebebi… | AYNI |
| M3-21 | Pasif satırda da Stok görünüyor (4 eylem) | S33_01_Satirda_uc_gorunen_eylem_aktifte_Stok_pasifte_Aktife_al | AYNI |
| M3-22 | Çekmecede belge yükleyici basit modda katlanmıyor | S33_22_Basit_modda_cekmece_en_cok_sekiz_girdi_belge_yukleyici_katli | AYNI |

Son mutasyondan sonra üç kaynak (`Urunler.razor`, `UrunEtiketleri.razor`, `Sozlukler.Urunler.cs`) `cmp` ile yedeğe birebir eşit. Temiz derleme: **0 Uyarı, 0 Hata**.

## 8. Denetim (`dotnet/denetim.sh`)
Son koşum: **DENETİM TEMİZ** (çıkış 0, `pS3-3-denetim2.log`). Bu paketi ilgilendiren sayaçlar:

| Sayaç | Değer | Bu paketin katkısı |
|---|---|---|
| @onclick'siz ve disabled'sız `<button>` | 67 ✓ (tavan 71) | Urunler 1 → 3. "⋯" açıcısı ve menüdeki "Kapat", yerli `popovertarget` ile çalışıyor (tarayıcı eylemi, ölü düğme değil; S3-2'deki durumla aynı). Etiketler 1 → 1 |
| ModalPencere kullanımı | 48 ✓ (tavan 55) | Urunler 5 → 5 |
| P-11 SATIR_ICI_STYLE | 110 ✓ | Urunler 6 → 6, Etiketler 0 → 0 |
| P-18 SABIT_RENK | 0 ✓ | CSS açılmadı |
| P-13 / P-31 IC_KOD_SIZINTISI | 0 ✓ | — |
| P-15 DUZ_SELECT_LIMIT | açık 9 (tavan 9) ✓ | `limit=200` eklenmedi |
| index.html `<script>` sayısı | 8 ✓ | JS yok |
| Layout+Bilesenler emoji | 0 ✓ | Bileşen eklenmedi |
| B-3 ESIK_ISTEMCIDE | 4 (tavan 4) ✓ | Eşik eklenmedi |

**DOM id sözleşmesi:** paket öncesi kopyaya karşı `diff` yapıldı. Silinen `id` ya da `ElemanId`: Urunler **0**, Etiketler **0**. Eklenen id'ler şunlar:
- Ürünler (sabit): `urun-durum-filtre`, `urun-suzgec-ac`, `urun-sinir`, `urun-sinir-asildi`, `urun-stok-hata`, `urun-sube-hata`, `u-daha-fazla`, `d-daha-fazla`, `s-sube`.
- Ürünler (satıra bağlı): `u-eylem-*`, `u-menu-*`, `u-menu-ac-*`, `u-stok-*`, `u-duzenle-*`, `u-etiket-*`, `u-pasife-al-*`, `u-menu-kapat-*`, `u-stok-adet-*`, `u-grup-*`, `u-pasif-rozet-*`, `u-stok-sube-*`.
- Etiketler: `et-arama-bos`, `et-secili-ozet`, `et-secim-kaldirildi`, `et-tumunu-sec`, `et-secimi-temizle`.

## 9. Canlı doğrulama (port 5191, gerçek Chrome headless + CDP, gerçek veri)
- API: `dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5191`. Önce PID 39160/39234, son derlemeden sonra yeniden başlatıldı (80501).
- Kiracı: bu paket için açılan tek kullanımlık bir davetle (`kiraci_davetleri`, jeton SHA-256) `POST /auth/kayit` → **201**, `PAKETS3-3-TEST Kuyumculuk`, `01M387PZMAM3897AWPA2F13WX4`. Jeton ve parola ekrana basılmadı, jeton dosyaları iş sonunda boşaltıldı.
- Betikler: `scratchpad/pS3-3-kur.sh`, `pS3-3-canli.js`, `pS3-3-duzenle.js`. Ekran görüntüleri: `scratchpad/pS3-3-*.png`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /urunler` × 4 | 201 | **201 × 4**. 3 üründe açılış stoğu var (3 / 2 / 1) |
| `DELETE /urunler/{S33-04}` | 200, yumuşak | **200** `{"mesaj":"Ürün deaktif edildi"}` |
| `GET /urunler?aktif=false&limit=50` | yalnız pasif | **200**, 1 kayıt ("S33 Eski Kupe", aktif=false) |
| `GET /urunler?aktif=true&limit=50` | 3 aktif | **200**, 3 kayıt |
| `GET /urunler/{S33-01}/stok` | subeStoklar | **200**, `subeId 01M387PZMAWWQTWY802H1WW1Z6`, adet 3.0 |
| `GET /subeler` | aynı id → ad | **200**, `01M387PZMAWWQTWY802H1WW1Z6 → "Merkez Şube"` |
| PRO 1440 liste | 3 eylem + ⋯, Stok sütunu, "Ürün grubu" | başlıklar: Görsel, Ürün Adı, Barkod, **Ürün grubu**, Ayar, Gram, Has Gram, **Stok**, Durum, Eylemler. Aktifte "📦 Stok · ✏️ Düzenle · ⋯", pasifte "♻ Aktife al · ✏️ Düzenle · ⋯". Stok 3 / 2 / 1 / 0 adet. Satır **63 px**. Sınır metni görünür |
| PRO ⋯ penceresi | açık, ekran içinde | `:popover-open` true: Etiket, Pasife Al, Kapat. Ekran içinde ✓ |
| Stok penceresi | şube adı, ULID yok | "Merkez Şube 3 42.5940 has". ULID deseni **yok** ✓ |
| `PATCH /kullanicilar/ben/arayuz-modu` BASIT | 200 | **200** `{"arayuzModu":"BASIT","etkinArayuzModu":"BASIT"}` |
| BASIT 1440 liste | Barkod/Grup/Durum sütunu yok, pasif rozeti adda | başlıklar: Görsel, Ürün Adı, Ayar, Gram, Has Gram, Stok, Eylemler. "S33 Eski Kupe**Pasif**". Satır **53 px** ✓ |
| BASIT süzgeç | Durum görünür, 4 seçici katlı | durum ✓, "Süzgeç ▾", maden yok → tıklayınca `aria-expanded=true` ve 4/4 seçici görünür ✓ |
| BASIT yeni ürün formu | ≤ 8 alan | **8**: u-ad, u-kod, u-barkod, u-tip, u-maden, u-ayar, u-gram, u-acilis-stok. "Daha fazla" ile 29 alan ✓ |
| BASIT çekmece | ≤ 8 girdi | ilk ölçümde 9 (7 alan + belge yükleyici 2). Belge yükleyici katlandı (M3-22 / S33_22). Yeniden ölçümde **7** ✓ |
| BASIT çekmecede yalnız adı değiştir → Kaydet | PATCH 200, bant, katlı alanlar korunur | bant "Kaydedildi.", çekmece kapandı ✓ (SQL aşağıda) |
| BASIT 375 px | yatay taşma 0 | scrollWidth 375 = clientWidth 375 ✓. ⋯ penceresi ekran içinde ✓. Satır **69 px** (bkz. sapma 6) |
| Etiketler: tümünü seç + kopya 10 | 30 etiket önizleme | 3 aktif ürün × 10 = **30** `.urun-etiket`. Düğme "Etiketleri Yazdır (30)". Özet "3 ürün seçili · 30 etiket" ✓ |
| Etiketler: arama "zzz-yok" | boş durum, seçim düşer | `"zzz-yok" ile eşleşen ürün yok. Aramayı değiştirin.` Önizleme **0**. "Aramada görünmeyen 3 seçim kaldırıldı." Yazdır kilitli, sebep "Önce listeden ürün seçin." Arama temizlenince önizleme 0 kaldı ✓ |

SQL kanıtı (`urunler`, kiracı `01M387PZ…`). S33-01 basit modda yalnız adı değiştirilerek kaydedildi. Katlı alan olan işçilik (250.00) korundu:
```
S33-01  S33 Burma Bilezik (duz2)  ZIYNET K22 15.5000 iscilik 250.00  S33001 aktif 1
S33-02  S33 Tektas Yuzuk          ZIYNET K14  3.2000 iscilik   0.00  S33002 aktif 1
S33-03  S33 Ince Kolye            ZIYNET K18  6.0000 iscilik   0.00  S33003 aktif 1
S33-04  S33 Eski Kupe             ZIYNET K14  2.0000 iscilik   0.00          aktif 0
stoklar: S33-01 3.000 · S33-02 2.000 · S33-03 1.000 — sube_id 01M387PZMAWWQTWY802H1WW1Z6 = "Merkez Şube"
```
`denetim_kayitlari` (DenetimServisi yolu): `EKLEME Urun 4 · GUNCELLEME Urun 2` (pasife alma ve çekmece PATCH'i) · `EKLEME Stok 3` · `EKLEME StokPartisi 3` · `EKLEME Hareket 3`.

Temizlik:
- API durduruldu (`kill` 39160/39234 ve 80501). `lsof -iTCP:5191 -sTCP:LISTEN` boş (0 satır).
- `betikler/kiraci-temizle.sh 01M387PZMAM3897AWPA2F13WX4 --uygula` çalıştırıldı: önce rapor kipinde 167 satır ve `tenants`'ta 1 satır listelendi, sonra silindi. Yeniden sorgu: "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)".
- Davet satırı `PS33DAVET…` silindi, kalan 0.
- **tenants: başta 10, sonda 10.** `PAKETS3-3%` kiracısı 0. QA-TEST-* ve DEMO kiracılarına dokunulmadı.

## 10. Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor`
- `dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor`

İkisi de bu şeridin dosya alanında. Panel.razor ve Cariler.razor bu pakette açılmadı.

## 11. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` + paket içi ayrım)
`git status` Kuyumcu.Tests ve Kuyumcu.Contracts'ı klasör olarak `??` gösteriyor. Bu paketin dokunduğu dosyalar:
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor` (M)
- `dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor` (?? — önceki paketlerden izlenmeyen dosya, bu pakette değişti)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Urunler.cs` (YENİ)
- `dotnet/Kuyumcu.Contracts/Sayfalar/UrunlerSozlesmeleri.cs` (`UrunVeri.Birim`, `UrunSubeVeri`)
- `dotnet/Kuyumcu.Tests/S33UrunlerEtiketlerEkranTestleri.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/UrunEkranTestleri.cs` · `dotnet/Kuyumcu.Tests/P10EkranTestleri.cs` · `dotnet/Kuyumcu.Tests/EkranTestleri.cs` (mod servisi kaydı, seçici ve beklenti uyarlaması; § 3/7)
- `dokumanlar/09-uygulama-kayitlari/PAKET-S3-3-uygulama-raporu.md` (bu rapor)

Proje ağacına .mutorj, .bak ya da .tmp bırakılmadı. `Pages/*.razor.mutorj` dosyaları 21 Eylül tarihli, bu pakete ait değil.

## 12. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Liste + çekmece şablonu (S3-2) | KARŞILANDI | § 2.1, § 2.5 · S33_01–04, S33_16–22 |
| Süzgeç satırı: tek Secici + "Süzgeç ▾" (4 seçici katlı) | KARŞILANDI | § 2.2 · S33_08–11 · canlı BASIT |
| Stok adedi sütunu | KARŞILANDI (sapma 1) | § 2.3 · S33_05–06 · canlı 3/2/1/0 adet |
| Stok penceresinde ham kimlik → şube adı | KARŞILANDI | S33_07 · canlı "Merkez Şube", ULID yok |
| Etiketler: sonuçsuz aramada boş durum | KARŞILANDI | S33_E1 · canlı |
| Etiketler: eski seçim ekranda kalmasın | KARŞILANDI | S33_E2 · canlı önizleme 30 → 0 |
| 30 etiketlik önizleme çalışmaya devam etmeli | KARŞILANDI | S33_E3 · canlı 30 |
| Özellik silinmez | KARŞILANDI | Bütün eylemler ve alanlar menüde ya da "Daha fazla" arkasında. Silinen id 0 |
| DOM id'leri değişmez | KARŞILANDI | diff: silinen id 0 |
| Tek POST yolu / Islemler.razor'a dokunulmaz | KARŞILANDI | Islemler ve Bilesenler/Fis açılmadı |
| Yetki ≠ mod, yasal kapı atlanmaz | KARŞILANDI | Taş kökeni, 4C, çelik fiyatı ve konsinye katlanmıyor (S33_20). Sunucu kapısı değişmedi |
| Sabit renk / satır içi stil / CDN yok | KARŞILANDI | SABIT_RENK 0, style 6 → 6, betik 8 |
| Sözlük ayrı dosyada, TR + EN | KARŞILANDI | Sozlukler.Urunler.cs · S33_21 |
| Jargon ve ham kod 0 (§ 5B) | KARŞILANDI (bu ekranlarda) | Grup adları ve maden kilidi sözlükten geliyor · S33_14–15 |
| İzolasyon: test VT `kuyumcu_test_y`, port 5191, build-server shutdown | KARŞILANDI | Bütün `dotnet test` çağrıları `KUYUMCU_TEST_DB=kuyumcu_test_y` ile koştu. Canlı doğrulama 5191'de, 5199'a dokunulmadı |
| Kapı (1) derleme 0 hata | KARŞILANDI | 0 Uyarı, 0 Hata |
| Kapı (2) kendi testlerim 0 başarısız | KARŞILANDI | 29/29 S33 · 307/307 ilgili sınıflar |
| Kapı (3) sayaçlarım ✓ | KARŞILANDI | § 8, DENETİM TEMİZ |
| Kapı (4) mutasyon kanıtı | KARŞILANDI | 22/22 kırmızı, 22/22 cmp AYNI |
| Kapı (5) canlı doğrulama | KARŞILANDI | § 9 |
| Kapı (6) test kiracısı silindi | KARŞILANDI | tenants 10 → 10 |
| Kapı (7) yeni kırmızı yok | KARŞILANDI | Son koşumun tek kırmızısı başka şeridin ve tek başına 2/2 yeşil |
| Basit modda satır ≤ 64 px | KISMEN | 1440 px: 53 px ✓ · 375 px: 69 px (sapma 6) |

## 13. Açık kalanlar
1. **`PusulaEkranTestleri.IptalliPusuladaKaydetKilitli`** tam süitte bir kez kırmızı oldu, tek başına iki kez yeşil geçti. Sahibi: P-55 (GiderPusulalari) şeridi. Muhtemel sebep, o şeridin mutasyon turunun bu süitin derlemesiyle çakışması.
2. **Toplu stok ucu (backend kuyruğu, S-B):** `GET /urunler` yanıtına `toplamStok` alanı eklenirse satır başına stok isteği kalkar (sapma 1).
3. **375 px satır yüksekliği 69 px:** `app.css`'e `.urun-satir > td` dikey boşluk kuralı eklenmeli. `app.css` bu şeridin alanı değil, sahibi S-1 ya da orkestratör.
4. **Plan § 5B sözlük göçü:** "Pasife al" → "Kullanımdan kaldır" ve "Cari" → "Müşteri" metinleri ortak hub'da (`Sozlukler.cs`) yapılmalı. Bu şerit o dosyaya dokunamaz.
5. **Ayar rozeti** ("K22") listede kod olarak görünmeye devam ediyor. Kuyumcu terimi olduğu için bırakıldı. P-58 ekran sözlüğü paketinin kapsamında.
6. **Yeni ürün formunda konsinye:** konsinye işaretlenip "Daha fazla" kapatılırsa sahip ve bedel alanları gizlenir. Kaydet kilidinin sebebi yine yazılıyor (UX-S30) ama alan görünmüyor. Düzenlemede bu durum çözüldü (`DuzTumAlanlar || dKonsinye`). Yenide aynı kural bir sonraki dokunuşta eklenebilir.

## 14. Kurucuya / mali müşavire notlar
- Mevzuat hükmü yazılmadı ve değiştirilmedi. Mali müşavir teyidi gerektiren bir kalem yok.
- Kurucuya görsel tercih sorusu yok. Ürünler için ayrı bir taslak olmadığından Müşteriler taslağının düzeni (n3) uygulandı.
