# PAKET PATCH-3 — uygulama raporu (02.10, dalga PATCH-3 · t9 · 5188/5189)

Rol: Taşkın (backend) + Ertuğ (frontend). Kaynak: `PAKET-PATCH-2-uygulama-raporu.md` § 13 ve `PAKET-PATCH-SOZLESME-uygulama-raporu.md` § T3.11 / § 12 açık kalanları.
Kanıt kopyaları: `ham/PATCH-3-olcum/` (canlı betik + 2 koşum çıktısı, mutasyon sürücüsü + sonuç + tur 1'de 25 günlük (`mut-log/`), tur 2'de 31 mutasyonun günlükleri (`mut-log-tur2/`), tarayıcı ölçüm betikleri + `olcum.jsonl` + 22 ekran görüntüsü, denetim, tam süit özeti, tarayıcı sonrası SQL). Mutasyon yedekleri ve TRX: `/private/tmp/kuyumcu-serit/PATCH-3/`, `/private/tmp/kuyumcu-serit/trx-pPATCH-3/`. Proje ağacına TRX, yedek ya da geçici dosya konmadı.

## 0. TUR 2 — doğrulama bulgularının kapatılması (02.10, ikinci ve son tur)

| Bulgu (önem) | Yapılan | Kanıt |
|---|---|---|
| **KRİTİK (hakem):** Ürün düzenle, "Kod (isteğe bağlı)" boşaltılınca Kaydet sessizce etkisiz kalıyordu | Kod sunucuda zorunlu olduğu için `d-kod` bileşeni `Zorunlu="true" Durum="form" HataAlani="d-kod"` aldı (yeni ürün formundaki `u-kod` ile aynı, UX-S26). Etiket artık yıldızlı. Boş gönderimde alan altında "Bu alan zorunludur." çıkıyor, `aria-invalid=true` oluyor, odak ve kaydırma bu alana gidiyor, PATCH gitmiyor. Aynı sessiz etkisizlik kalıbı çekmecedeki diğer alanlarda da vardı: istemci ya da sunucu bu alanlara hata yazabiliyordu ama alan bunu çizmiyordu. Bu yüzden `Urunler.razor`'daki **23 alana** (`d-tas`, `d-satis`, `d-iscilik`, `d-karat`, `d-barkod` …, ayrıca yeni ürün modalının `u-…` alanları) `Durum="form" HataAlani` bağlandı. Alansız bir sunucu hatası gelirse çekmece bandın kendisine kaydırılıyor (`Id="d-urun-hata"`, `OdakIlkHataya` yedeği) | bUnit `Duzenlemede_KOD_ZORUNLU_isaretli_bosaltilinca_ALAN_ALTI_mesaj_PATCH_YOK`; mutasyonlar M6-4 ve M6-5; başsız Chrome 8/8 kombinasyon (§ 11-T2); SQL'de kod `P3-A` değişmedi |
| **KRİTİK (hakem):** "Gram Ağırlık (isteğe bağlı)" boşaltılınca "Sıfırdan büyük bir değer girin" hatası çıkıyordu; boş kutunun altında "0,000 g" görünüyordu | ADET biriminde gram zorunlu olduğu için etiket birime göre değişiyor: `Zorunlu="@(dBirim != "GRAM")"`. Aynı değişiklik yeni ürün modalında `u-gram` için de yapıldı. GRAM biriminde alan kilitli ve sebebi yazılı. Boşaltılan kutu "0,000 g" önizlemesi göstermiyor, çünkü boş durum ayrıca izleniyor (`DGramDegisti`). Doğrulama 0 üzerinden çalışmaya devam ediyor. Kıymetsiz madende satış fiyatı da koşullu zorunlu (`Zorunlu="@(!MadenKiymetli(..))"`) gösteriliyor | bUnit `Duzenlemede_ADET_biriminde_GRAM_ZORUNLU_isaretli_bos_kayitta_ALAN_ALTI_mesaj_PATCH_YOK`; mutasyon M6-6; Chrome ölçümünde `#d-gram-onizleme` artık "binen" listesinde yok |
| **ORTA:** Range iletisindeki alt sınır sunucu kültürüne bağlıydı ("0.01 ile 9.999.999…,99") | `DogrulamaSozlugu.Okunur` artık **her** sınırı tr-TR ile yazıyor: küçükler binliksiz ondalıkla ("0,01", "3650"), ≥ 1.000.000 olanlar binlik ayırıcıyla. Sonuç süreç kültüründen bağımsız. Testteki yanlış sabit ("Tutar 0.01 ile …") düzeltildi. Kültürsüz (invariant) girdiyle 3 yeni vaka eklendi: "0.001 and 100000", "0.5 exclusive", "999999999.999999" | M6-1 (2 test kırmızı); API `LC_ALL=C LANG=C` ile başlatılıp canlı denendi: "Tutar 0,01 ile 9.999.999.999.999,99 …", "Adet 0,001 ile 100000 …" (`ham/PATCH-3-olcum/kultur-LC_ALL_C.txt`) |
| **DÜŞÜK:** Rezerv adedinin üst sınırı oluşturmada 100.000, PATCH'te 999.999.999,999 idi | Tek sınır: `RezervOlusturDto.AdetUst = 100_000`. DTO `Range(0.001, AdetUst)` bu sabiti okuyor (yanıltıcı "sıfırdan büyük" ErrorMessage kaldırıldı, ileti artık AralikMesaji'ndan geliyor). Servis içinde tek kapı var: `SatisServisi.RezervAdetKapisi`. Oluşturma ve PATCH ikisi de bunu çağırıyor; 0 < adet ≤ 100.000 değilse 400 alan:adet. Daha sıkı ve önceden var olan sınır seçildi: stok kapısı zaten pratikte koruyordu, kolon tavanı sınırı ise iş anlamı taşımıyor | Saf test 2+5 vaka + tek kaynak testi; HTTP PATCH `{"adet":100001}` vakası; M6-2 ve M6-3; canlıda POST ve PATCH 100001 → 400 alan:adet, SQL'de rezerv değişmedi |
| **DÜŞÜK:** OverflowException genel olarak 400'e çevriliyordu; iç hesap hatası günlükte ayırt edilemiyordu | `LogWarning(ex, …)`: istisnanın kendisi (iç halkalar ve yığın izi) günlüğe yazılıyor. Davranış (400) değişmedi | `ham/PATCH-3-olcum/api-gunluk-tasma-ayrinti.txt`: DbUpdateException → MySqlException "Out of range value for column 'has_gram_bakiye'" zinciri görünüyor. Günlük satırı için test yok (davranış değil gözlem; mutasyon kapsamı dışında) |
| **DÜŞÜK:** Mutasyon tablosu 25 diyordu, kanıt 23 günlüktü | Tur 1'in ikinci koşumundaki M5-8 ve M5-9 günlükleri `/private/tmp/…/mut-log2`'de duruyordu, ham dizine kopyalanmamıştı. İkisi `mut-log/`'a eklendi (25 günlük; M5-8 2 kırmızı, M5-9 1 kırmızı). Ayrıca tur 2'de listenin TAMAMI ve 6 yeni mutasyon yeniden koşuldu: **31/31 kırmızı, 31/31 cmp AYNI** (`mut2.py`, `mut-sonuc-tur2.txt`, `mut-log-tur2/` 31 test günlüğü) | § 8 |
| **ORTA:** 5199 inceleme sunucusu kapalı | Bu paketin işi değil, 5199'a dokunulmadı (istem kuralı). Orkestratörün gece yetkisi kapsamında yeniden başlatması gerekiyor | — |

Tur 2'de dokunulan dosyalar: `Pages/Urunler.razor` (aynı paketin ikinci turu; tek dokunuş kaydı bu paket içinde), `SatisModulu.P21.cs`, `DogrulamaSozlugu.cs`, `Program.cs` (yalnız günlük satırı), `PatchUcTestleri.cs`, `P10EkranTestleri.cs`. Hepsinin tur 1 hâli `/private/tmp/kuyumcu-serit/PATCH-3/tur2-yedek/` altında.
Not: Mutasyon koşumu sırasında M1-1 bir kez başka bir şeridin eşzamanlı derlemesiyle dosya yarışına girdi (MSB4018, `Kuyumcu.Tests.deps.json`) ve `BUILD-KIRMIZI` verdi. Sürücüye MSB4018 için yeniden deneme eklendi, M1-1 tek başına yeniden koşuldu ve 2 test kırmızı oldu. Sonuç dosyasında bu yeniden koşumun sonucu duruyor; ilk koşumun derleme günlüğü `mut-log-tur2/M1-1.build.txt`.

## 1. Spesifikasyon ile kodun karşılaştırılması

| İddia | Kodda durum (02.10) | Sonuç |
|---|---|---|
| `SatisModulu.cs:~954` alış fişi kalem `k.Adet <= 0m` yuvarlamadan önce | Doğru (satır 954) | Düzeltildi |
| `P21.cs:~212` rezerv oluşturma `dto.Adet <= 0m` yuvarlamadan önce | Doğru (satır 212); ayrıca 400 alansızdı | Düzeltildi (+ alan:adet) |
| `P21.cs:~367` rezerv PATCH `adOge.GetDecimal()` dize/1e30 → 500 | Doğru. Ek bulgu: `aciklama` ve `bitisGunu` için de `GetString()` sayı gelince 500 veriyordu | Üçü de düzeltildi |
| Satış DTO'larında `Range(…, double.MaxValue)` | 26 + 7 üye (SatisModulu.cs + P21.cs). Ek bulgu: `TakasDto.EskiUrunGram` ve `EskiUrunBirimFiyatTlHasGram` için **hiç** aralık yoktu (negatif gram kabul ediliyordu) | Kolon tavanı + `Range(0, …)` |
| Ürün/kategori PATCH'inde isteğe bağlı alan "" ile siliniyor (null = koru) | Doğru. **Ek ve daha ağır bulgu:** ekran boşaltılan barkod, taş ibaresi, 4C ve satış fiyatı için **null** gönderiyordu, yani sunucu "koru" anlıyordu. Bu alanların silinmesi sessizce **etkisizdi**. Kategori ekranı "üst yok" için null gönderiyordu, kategori köke taşınmıyordu | Üç durumlu sözleşmeye geçildi (§ 3) |
| Şube `{ad:null}` → 400, ürün `{ad:null}` → 200 | Doğru | Kural yazıldı: zorunlu alanda null = KORU (§ 3) |
| 6 haneli yuvarlamada ToEven ile AwayFromZero karışık kullanılıyor | Doğru. API'de `Math.Round(…, 6)` ToEven çağrısı 23 yerde (Rapor, Emanet, Kurulum, Sayım, Yerleşim, Maliyet) | Tek yardımcı yazıldı, Satış ona bağlandı. Kalan yerler bu paketin dosya alanı dışında (§ 13) |
| MySQL 1264 → 500 | Doğru (canlıda: emanet girişi gram 1e15 → 500) | Genel son savunma hattı (§ 4) |
| `AralikMesaji` üst sınır ≥10⁹ ise yalnız "X veya daha büyük" diyor | Doğru. Ek bulgu: `Range(MinimumIsExclusive=true)` iletisi hiç çevrilmiyordu ("The field IscilikTl must be between 0 exclusive and …" ekrana İngilizce düşüyordu). Küçük alt sınır "1E-06" diye yazılıyordu | Düzeltildi (§ 5) |

## 2. Satış modülü (madde 1, 2, 3) — yalnız girdi doğrulama, tavan ve yuvarlama

Ödeme ve kasa mantığına dokunulmadı.

- **AlisFisi (alış fişi kalemi):** önce `k.Adet = Yuvarla3(k.Adet)`, sonra `<= 0` kontrolü (400 alan:kalemler), ardından `KolonTavani.Denetle` ile adet decimal(12,3) ve birim fiyat decimal(15,2) sınırı. Kalem tutarı artık yuvarlanmış adetle hesaplanır (1,0004 × 100 → 100,00; canlıda SQL `1.000 100.00`).
- **RezervOlustur (rezerv oluşturma):** önce yuvarlama, sonra `<= 0` kontrolü (400 alan:adet), sonra tavan kontrolü. Bloke artık yuvarlanmış adetle oynar.
- **RezervGuncelle (rezerv PATCH):** `adet` değeri `KolonTavani.JsonSayi` ile okunur. Dize, `true` ya da decimal'e sığmayan sayı 400 alan:adet döner (500 değil). Sıra yuvarla → `<= 0` → tavan. `aciklama` ve `bitisGunu` alanları yeni `KolonTavani.JsonMetin` ile türce denetlenir (sayı gelirse 400 alan). Geçersiz tarih iletisine de alan eklendi. **`{adet:null}` artık KORUR (200).** Eskiden 400 "silinemez" dönüyordu; değişiklik § 3'teki tutarlılık kuralının gereği.
- **Tüm satış yazma uçlarında kolon tavanı:** `KolonTavani.UstAdet / UstTutar / UstHas` sabitleri tanımlandı (öznitelik double ister). Her `Range(…, double.MaxValue)` kolon kapasitesine çekildi: adet/gram → decimal(12,3), TL → decimal(15,2), has → decimal(15,6). Bu, adet × fiyat çarpımındaki `OverflowException` 500'ünü kaynağında kapatır. Hesaplanan toplam kolona sığmazsa § 4'teki ağ devreye girer. Kapsanan uçlar: `satis`, `alis`, `alis-fisi`, `hurda-alis`, `hurda-satis`, `transfer`, `satis-fisi`, `satis-iade`, `alis-iade`, `takas`, `iscilik`, `avans`, `avans/mahsup`, `avans/iade`, `degisim`, `rezervler` (+PATCH), `konsinye-cikis`, `konsinye-iade`, `siparisler/{id}/satisa-cevir` (DTO bu dosyada). Gövdesi yalnız metin olan uçlarda (`rezervler/{id}/iptal`, `{id}/storno`) sayı alanı yok.
- `SatisModulu.Yuvarla3` ve `Yuvarla6` artık `KolonTavani`'ya yönlenir (tek kural, § 5).

## 3. Ürün, kategori ve şube — "Verildi" bayraklı DTO ve tutarlılık kuralı (madde 4)

**Karar ve gerekçe.** İki seçenek vardı: (a) DTO'da "Verildi" bayrağı, (b) ham JsonElement ile alanın gövdede olup olmadığını okumak. (a) seçildi. Gerekçeleri:
1. Projede zaten kurulu desen bu: `KategoriId`/`ReyonId`/P-16 alanları, `SubeGuncelleDto`, `CariGuncelleDto`. Yeni bir desen açılmadı (DoD: ikinci kalıp yok).
2. DataAnnotations (MaxLength, Range, RegularExpression) ve PATCH-2'nin sınıf düzeyi `JsonNumberHandling.Strict` kuralı korunur. JsonElement'e geçilse bunlar elle yazılmak zorunda kalırdı.
3. DTO'yu doğrudan kuran iç çağrılar ve testler kırılmaz: setter bayrağı kendisi koyar.

**Sözleşme (tüm PATCH uçları için tek kural):**

| Alan türü | Gövdede YOK | `null` | `""` / boşluk | Değer |
|---|---|---|---|---|
| İsteğe bağlı (açıklama, parite, KMTS, barkod, RFID, görsel, taş ibaresi, 4C ×6, satış fiyatı; kategori üst; şube adres/telefon) | KORU | **SİL** | **SİL** (geriye uyum, bir sürüm boyunca) | doğrula + yaz |
| Zorunlu (ürün tip/kod/ad/ayar/birim/gram; kategori ad; şube ad; rezerv adet) | KORU | **KORU** | 400 alan (ad/kod) | doğrula + yaz |

- "null = koru" kuralı **zorunlu alanlar için her yerde** geçerli. Şube `{ad:null}` artık 200 döner ve adı korur. Eski test `PATCH_sube_null_adres_SILER_null_ad_400_alan_ad` bu kurala göre güncellendi, yeni adı `…_null_ad_KORUR_bos_ad_400_alan_ad`. Boş/boşluk ad 400 vermeye devam eder.
- Şube adres ve telefonu `""` ile gelirse artık kolona `""` değil NULL yazılır.
- Ürün POST'unda kolon tavanı için PATCH ile **ortak** kapı açıldı: `UrunServisi.UrunKolonKapisi` (UX-S26).
- Bilinçli olarak üç duruma çevrilmeyen alanlar: `TasKokeni` (taş varken zorunlu; koşullu zorunlu alan null = koru), `GercekMilyem` (boşaltma "ayarın standart milyemine dön" demektir ve has yeniden hesaplanır; davranış kararı ayrı bir iştir), `IscilikTl` (kolon NOT NULL; 0 = temizle), konsinye üçlüsü (kendi bütünlük kapısı var: `KonsinyeKapisi`).

**İstemci (Kuyumcu.Web).** `Pages/Urunler.razor` düzenleme gövdesi anonim nesneden `Dictionary<string, object?>`'e çevrildi. Ekranın gösterdiği isteğe bağlı alan her zaman gönderilir ve boşaltılan alan `null` gider. Görünmeyen alan (BASİT moddaki KMTS) gövdeye hiç konmaz, sunucu korur. Pırlanta dışı tipte 4C alanları `null` gider ve silinir: tip değişince eski 4C artık PirlantaKapisi'na takılmaz. Kategori ekranı (`UrunAgaci.razor`) zaten `ustId:null` gönderiyordu, yeni sözleşmeyle bu artık doğru anlamı taşıyor. **Bu dosyaya dokunulmadı** (tek dokunuş kuralı). Sunucu bir sürüm boyunca `""` gönderimini de kabul eder (§ 13: kaldırma takvimi). bUnit testi `Duzenlemede_BOSALTILAN_alan_GOVDEDE_BOS_…` yeni sözleşmeye göre güncellendi: `…_GOVDEDE_NULL_…`, anahtar gövdede ve değeri null.

## 4. Son savunma hattı — MySQL 1264 ve taşma → 400 (madde 5, yalnız `Program.cs` hata çevirisi)

`app.UseMiddleware<HataYakalayici>()` satırının hemen içine bir satır içi ara katman eklendi. İstisna zincirinin herhangi bir halkasında MySQL hata numarası **1264** ("Out of range value for column 'x'") ya da `OverflowException` varsa istisna `BadRequestException`'a çevrilir. HataYakalayici aynı gövde biçimiyle 400 yazar (T-4a). Kolon adı iletiden çıkarılır ve camelCase alana çevrilir (`has_gram_bakiye` → `hasGramBakiye`); ileti `DogrulamaSozlugu.AlanAdi` ile Türkçeleştirilir. Hata numarası, HataYakalayici'daki 1062 dalıyla aynı yöntemle yansıma kullanılarak okunur; sağlayıcıya derleme bağımlılığı yoktur. İşlem zaten geri alınmıştır, kısmi yazım olmaz (canlı SQL: emanet hareketi 0, has bakiyesi 0).
Not: alan, taşan **kolondan** türetilir; bu her zaman kullanıcının gönderdiği alan değildir. Örneğin emanet `gramAgirlik:1e15` isteğinde taşan kolon `cari_bakiyeler.has_gram_bakiye` olduğu için yanıt alan:hasGramBakiye taşır. Uç düzeyindeki kolon kapıları (KolonTavani) birincil savunmadır, ağ ise sonuncusudur.
POST kolon tavanları: Ürün (uç kapısı), Satış (DTO + ağ), Fatura/Sipariş/Vezne/Satın alma (PATCH-SOZLESME/PATCH-2 uç kapıları + bu ağ). Bu dört modülün dosyalarına dokunulmadı.

## 5. Tek 6 hane kuralı ve Range iletisi (madde 5)

- `KolonTavani.Yuvarla6` (AwayFromZero) tek yardımcıdır. Testle `OzelMatrahMotoru.Yuvarla6` ile birebir aynı sonucu verdiği kanıtlandı. `SatisModulu.Yuvarla6` bu yardımcıya bağlandı (yansıma testi var). Bu pakette yazılan yeni kod da bu yardımcıyı kullanır.
- `DogrulamaSozlugu.AralikMesaji` artık **tek ileti** üretir ve ileti iki yönde de doğrudur. Yalnız gerçekten sınırsız üst sınır gizlenir (double.MaxValue → "E+308"). Sonlu her üst sınır, büyükse binlik ayırıcıyla yazılır: "Tutar 0,01 ile 9.999.999.999.999,99 arasında olmalıdır". Exclusive Range iletisi çevrildi: "İşçilik (TL) 0 değerinden büyük, en çok 9.999.999.999.999,99 olmalıdır". Bilimsel alt sınır ondalığa çevrildi: "Has gram 0,000001 ile …". Sınır sayısı kültürden bağımsız okunur (double "G" biçiminde binlik ayırıcı olmaz, tek ayırıcı ondalık ayırıcıdır). Satış DTO'larının ham PascalCase etiketleri (`Iade tutari TL`, `Eski urun gram` …) için sözlüğe 17 Türkçe ad eklendi. Mevcut UX-4d iletileri ("Kredi limiti (TL) 0 veya daha büyük olmalıdır", "Vade (gün) 0 ile 3650 arasında olmalıdır") değişmedi; testleri yeşil.

## 6. DoD sözleşmesi

Yeni varlık ya da kayıt türü doğmadı, migration **gerekmedi** (şema değişmedi). Uçlar, `[RolGerekli]`/`[ModulYetkisi]` öznitelikleri ve `DenetimServisi.YazmaKaydet` çağrıları değişmedi. Finansal varlıklara silme eklenmedi.

| Varlık | Düzenleme yolu | Bu paketteki değişiklik |
|---|---|---|
| Ürün | PATCH /urunler/{id} (SAHIP, MUDUR, MUHASEBE) · pasife al/aktife al | İsteğe bağlı alanlar üç durumlu; POST/PATCH ortak kolon kapısı |
| Ürün kategorisi | PATCH /urun-kategorileri/{id} · DELETE = pasife al | Üst kategori üç durumlu (null = kök) |
| Şube | PATCH /subeler/{id} (SAHIP, MUDUR) · DELETE = deaktif | ad:null = koru; "" ile silinen alanlarda kolona NULL |
| Rezervasyon | PATCH /islemler/rezervler/{id} · iptal ucu | Tür/taşma 400; adet null = koru |
| Satış hareketleri | Değişmez; düzeltme yolu storno/iade (48-A) | Yalnız girdi aralıkları |

## 7. Testler

`Kuyumcu.Tests/PatchUcTestleri.cs` (YENİ): 27 test metodu, **93 vaka**. Sınıflar `PatchUcHttpTestleri`, `PatchUcServisTestleri` ve `PatchUcSafTestleri`. HTTP vakaları kodu ve alanı gövdeyi ayrıştırarak denetler; veritabanında satırı önce ve sonra okur.
Güncellenen mevcut testler (gerekçe § 3): `PatchSozlesmeTestleri.PATCH_sube_null_adres_SILER_null_ad_KORUR_bos_ad_400_alan_ad` ve `P10EkranTestleri.Duzenlemede_BOSALTILAN_alan_GOVDEDE_NULL_gider_silme_kararini_tasir`. Doğrulayıcı testi `Dogrulayici/PATCH2SinirTestleri.PS2_rezerv_PATCH_adet_dize_ve_1e30_400_alan_adet_500_DEGIL` Skip'teydi; testin kendi yorumu "düzeltilince Skip kaldırılır" diyor. Skip kaldırıldı, 2/2 vaka YEŞİL.
Filtreli koşumlar:
- PatchUc: 93/93.
- PatchUc, PATCH2Sinir: 113/113.
- İlgili geniş küme (PatchUc, PatchSozlesme, PatchIki, P10, P16, UX4d, UrunEkran, S33Urun, UX4b, KesifP4, UX1C1, HttpUrunBag, Rezerv, L1b, S26Diger, Sube, Kategori, Takas, Avans, Yasal4, MadenUc, P21): 1152 başarılı, 0 başarısız, 2 önceden atlanmış.
- Doğrulama sözlüğü kümesi (UX4d, Dogrulama, PatchIki, PATCHSOZLESME, PatchSozlesme, PatchUc): 370/370.

## 8. Mutasyon tablosu

Yöntem: kaynak dosya `/private/tmp/kuyumcu-serit/PATCH-3/mut-yedek/<dosya>.orijinal-<kod>` yoluna kopyalanır. TEK eşleşmeli değişiklik yapılır (eşleşme ≠ 1 ise durur). Ardından `dotnet build --no-incremental` ve `dotnet test --filter "PatchUc|PatchSozlesmeHttp"` koşulur, yedek YENİ mtime ile geri kopyalanır ve `cmp` ile karşılaştırılır. Sürücü: `ham/PATCH-3-olcum/mut.py`; günlükler `ham/PATCH-3-olcum/mut-log/`.

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | AlisFisi `k.Adet = Yuvarla3(k.Adet)` silindi | P3_alis_fisi_kalem_adet_1_0004_yuvarlanmis_1_000_yazilir, P3_servis_alis_fisi_kalem_adet_yuvarlaninca_sifir_RET_alan_kalemler | AYNI |
| M1-2 | RezervOlustur yuvarlama silindi | P3_servis_rezerv_OLUSTUR_adet_yuvarlaninca_sifir_RET_bloke_DEGISMEZ | AYNI |
| M2-1 | Rezerv PATCH `JsonSayi` → `GetDecimal()` | P3_rezerv_PATCH_hatali_tur_ve_tasma_400_alan_500_DEGIL_SQL_DEGISMEZ | AYNI |
| M2-2 | Rezerv PATCH null = koru koşulu silindi | P3_rezerv_PATCH_adet_null_KORUR_200_zorunlu_alan_silinemez | AYNI |
| M2-3 | Rezerv açıklama `JsonMetin` → `GetString()` | P3_rezerv_PATCH_hatali_tur_ve_tasma_… (aciklama:5) | AYNI |
| M3-1 | `UstTutar = double.MaxValue` | P3_satis_yazma_ucu_kolon_tavanini_asan_sayi_400_alan_500_DEGIL | AYNI |
| M3-2 | `UstAdet = double.MaxValue` | P3_satis_yazma_ucu_kolon_tavanini_asan_sayi_400_alan_500_DEGIL | AYNI |
| M3-3 | AlisFisi birim fiyat tavan kapısı silindi | P3_servis_alis_fisi_kalem_birim_fiyat_kolon_tavani_RET | AYNI |
| M4-1 | `AciklamaVerildi` → `Aciklama is not null` | P3_urun_PATCH_istege_bagli_alan_null_SILER_SQL_NULL_digerleri_AYNI | AYNI |
| M4-2 | `BarkodVerildi` → `Barkod is not null` | aynı | AYNI |
| M4-3 | `SatisFiyatiVerildi` → `SatisFiyati is not null` | aynı | AYNI |
| M4-4 | Kategori `UstIdVerildi` → `UstId is not null` | P3_kategori_PATCH_ustId_null_ve_bos_KOKE_tasir_SQL_NULL | AYNI |
| M4-5 | Şube `&& dto.Ad is not null` silindi | PATCH_sube_null_adres_SILER_null_ad_KORUR_bos_ad_400_alan_ad, P3_sube_PATCH_ad_null_KORUR_adres_bos_dize_NULL_olur | AYNI |
| M4-6 | Şube adres boş → NULL kuralı silindi | P3_sube_PATCH_ad_null_KORUR_adres_bos_dize_NULL_olur | AYNI |
| M4-7 | `PirlantaRenkVerildi` → `is not null` | P3_pirlanta_4C_renk_null_SILER_karat_KORUNUR | AYNI |
| M4-8 | Taş ibaresi eski `??` kalıbı | P3_urun_PATCH_bos_dize_GERIYE_UYUM_ile_de_SILER, P3_urun_PATCH_istege_bagli_alan_null_SILER_… | AYNI |
| M5-1 | `KolonTavani.Yuvarla6` → ToEven | P3_tek_6_hane_kurali_AwayFromZero_fatura_motoruyla_BIREBIR, P3_satis_modulu_6_hane_yuvarlamasi_TEK_yardimciya_baglidir | AYNI |
| M5-2 | Ağ `when (false && …)` | P3_MySQL_1264_kolon_tasmasi_genel_agda_400_alan_ile_satir_YAZILMAZ | AYNI |
| M5-3 | `n == 1264` → `1265` | P3_MySQL_1264_…, P3_ag_MySQL_1264_alanli_400_olur | AYNI |
| M5-4 | Ürün POST `UrunKolonKapisi` devre dışı | P3_servis_urun_OLUSTUR_kolon_tavani_UC_KAPISINDA_400_alan_veritabanina_GITMEZ | AYNI |
| M5-5 | AralikMesaji ≥10 haneli üst sınırı gizler (eski davranış) | P3_AralikMesaji_sonlu_ust_sinir_HER_ZAMAN_yazilir_tek_ileti_iki_yonde_dogru | AYNI |
| M5-6 | OverflowException dalı silindi | P3_ag_tasma_OverflowException_400_olur_baska_hata_CEVRILMEZ | AYNI |
| M5-7 | `SatisModulu.Yuvarla6` → `Math.Round(d, 6)` | P3_satis_modulu_6_hane_yuvarlamasi_TEK_yardimciya_baglidir | AYNI |
| M5-8 | Exclusive Range deseni bozuldu | P3_Range_exclusive_ve_bilimsel_alt_sinir_TURKCE_ingilizce_SIZMAZ | AYNI |
| M5-9 | Bilimsel alt sınır çevirisi kapatıldı | P3_Range_exclusive_ve_bilimsel_alt_sinir_TURKCE_ingilizce_SIZMAZ | AYNI |

**Tur 1: 25/25 kırmızı, 25/25 cmp AYNI.** Günlüklerin hepsi `mut-log/` altında (25 dosya; M5-8 ve M5-9 tur 2'de eklendi).

**Tur 2 (son kod, aynı yöntem, filtre `PatchUc|PatchSozlesmeHttp|P10UrunEkran`): 31/31 kırmızı, 31/31 cmp AYNI.** M1-1…M5-9 yeniden koşuldu. M1-2 kalıbı RezervAdetKapisi'na göre güncellendi; kırmızı testler yukarıdakilerle aynı. Yeni mutasyonlar:

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M6-1 | Okunur küçük sınırı ham bırakır (eski davranış) | P3_AralikMesaji_sonlu_ust_sinir_…, P3_Range_exclusive_ve_bilimsel_alt_sinir_… | AYNI |
| M6-2 | RezervAdetKapisi üst sınır kontrolü `if (false)` | P3_rezerv_PATCH_hatali_tur_ve_tasma_… (adet:100001), P3_rezerv_adet_kapisi_sinir_DISI_400_alan_adet | AYNI |
| M6-3 | DTO `Range(0.001, AdetUst)` → `999999999.999` | P3_rezerv_adet_ust_siniri_OLUSTURMA_DTO_Range_ile_TEK_kaynak | AYNI |
| M6-4 | `d-kod` Zorunlu kaldırıldı | Duzenlemede_KOD_ZORUNLU_isaretli_bosaltilinca_ALAN_ALTI_mesaj_PATCH_YOK | AYNI |
| M6-5 | `d-kod` Durum/HataAlani kaldırıldı (tur 1 hâli) | Duzenlemede_KOD_ZORUNLU_… | AYNI |
| M6-6 | `d-gram` Zorunlu → `@(false)` | Duzenlemede_ADET_biriminde_GRAM_ZORUNLU_… | AYNI |

Son mutasyondan sonra temiz derleme: `dotnet build Kuyumcu.sln --no-incremental` → **0 Hata, 2 Uyarı** (uyarılar başka şeritlerin test dosyalarında, § 14). Tam süit derlemesi: 0 Hata, 0 Uyarı.

Anayasa md.1 gereği gizlenmeyen not: ilk mutasyon koşumu (`mut-sonuc-ilk-GECERSIZ.txt`) **geçersiz sayıldı**. Sürücü yedeği `shutil.copy2` ile geri kopyalıyordu; bu komut dosyanın eski mtime'ını da geri getirir, dolayısıyla artımlı derleme geri alınan dosyayı yeniden derlemedi. Sonuçta M5-2, M5-6 ve M5-7 bir önceki mutasyonun kırmızısını gösterdi. Ayrıca M5-4 hiçbir testi kırmadı: HTTP yolunda § 4'teki ağ aynı alanı veriyordu ve bu bir test açığıydı. Düzeltmeler: geri kopyalama yeni mtime ile yapıldı, derleme `--no-incremental` yapıldı, servis yolu testi eklendi (`P3_servis_urun_OLUSTUR_…`) ve listenin TAMAMI yeniden koşuldu. Tablo ikinci koşumdur. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln --no-incremental`: **0 Hata**. Derlemedeki 2 uyarı başka şeritlerin test dosyalarındadır (§ 13).

## 9. Tam süit ve denetim

**Tur 2 (son kod, tek koşum):** `KUYUMCU_TEST_DB=kuyumcu_test_t9 dotnet test … --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pPATCH-3-t2` → **Toplam 9738 · Geçti 9736 · Başarısız 0 · Atlandı 2** (`TOPLU1SinirTestleriTur2.T2_Kasa_adi_kesme_isaretli` ve `SATIS3EkranTestleri.DOG_T24_…`, ikisi de önceden Skip'li ve bu paketle ilgisiz). **Bu paketin yeni kırmızısı: 0.** Özet: `ham/PATCH-3-olcum/tam-suit-ozet-t2.txt`. `denetim.sh` (tur 2): rc=1, **303 ✓**, tek ✗ P-30 (istemde HARİÇ). Ham: `ham/PATCH-3-olcum/denetim-t2.txt`.
Tur 2 filtreli koşumlar: PatchUc + P10UrunEkran + PATCH2Sinir 137/137; geniş küme (PatchUc, P10, PATCH2Sinir, Urun, UX4d, Dogrulama, Rezerv, PatchSozlesme) **819/819**.

Tur 1 kaydı:

`KUYUMCU_TEST_DB=kuyumcu_test_t9 dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj -v n --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pPATCH-3` (21:34, tek koşum): **Toplam 9625 · Geçti 9622 · Başarısız 0 · Atlandı 3**. Atlananlar:
- `PATCH2SinirHttpTestleri.PS2_rezerv_…`: bu pakette Skip kaldırıldı, sonradan 2/2 yeşil.
- `SATIS3EkranTestleri.DOG_T24_…` ve `TOPLU1SinirTestleriTur2.T2_Kasa_adi_kesme_isaretli`: önceden Skip'liydi, bu paketle ilgisiz.

Tam süitten sonra üretim kodu DEĞİŞMEDİ; yalnız o tek testin Skip özniteliği kaldırıldı. **Bu paketin yeni kırmızısı: 0.** Özet: `ham/PATCH-3-olcum/tam-suit-ozet.txt`.

`bash dotnet/denetim.sh`: rc=1, **303 ✓**. Tek ✗ `P-30: DEMO_INSAN_SURESI (BK-45)` (istemde HARİÇ). Ham çıktı: `ham/PATCH-3-olcum/denetim.txt`.

## 10. Canlı doğrulama (API 5188, bağımsız SQL — kuyumcu_net)

**Tur 2 koşumu (son kod, Release anlık görüntüsü `/private/tmp/kuyumcu-serit/PATCH-3/yayin`):** betikler `canli-p3-t2.mjs` ve `canli-p3-t2-kur.mjs`, çıktılar `canli-p3-t2-kosum.txt` ve `canli-p3-t2-kur-kosum.txt`. Kiracı `01M3Z1Y83RYHPMRB20NAM01XS1` (`PAKETPATCH-3-TEST …`). Yeni kiracıda kur ilk 40 saniyede gelmedi. Bu yüzden kur gerektiren adımlar (alış fişi, emanet) aynı kiracıda ayrı betikle yeniden koşuldu ve kur ilk denemede geldi. Bu durum gizlenmedi; ilk koşumdaki "Kur kaydı zorunludur" 400'leri çıktıda duruyor. 5xx sayısı: **0**. Tablodaki bütün satırlar tur 2'de yeniden alındı: alış 1,0004 → `1.000 100.00` · 0,0004 → 400 alan · rezerv PATCH 6/6 400, SQL değişmedi · null → korur · 0,0005 → `0.001/0.001` · satış tavanı **19/19** · emanet 1e15 → 400 alan:hasGramBakiye, SQL `0 0 / 0 0` · ürün POST 3/3 · ürün/kategori/şube üç durumlu sözleşmenin SQL'i birebir aynı. Tur 2'ye özgü adımlar:

| Adım | Beklenen | Gerçek | SQL |
|---|---|---|---|
| POST rezerv adet 100001 | 400 alan:adet, Türkçe | 400 alan:adet "Adet 0,001 ile 100000 arasında olmalıdır" | rezerv sayısı 1 / 1 |
| PATCH rezerv `{"adet":100001}` | 400 alan:adet (eskiden stok kapısına kadar gidiyordu) | 400 alan:adet "Rezerve adet en çok 100.000 olabilir" | `0.001 ∅ ∅ 0.001` önce = sonra |
| API `LC_ALL=C LANG=C` ile: avans iade 1e20, işçilik 0, hurda satış 0, rezerv 100001 | Küçük sınır tr-TR | "Tutar **0,01** ile …" · "İşçilik (TL) 0 değerinden büyük …" · "Has gram **0,000001** …" · "Adet **0,001** ile 100000 …" | — |
| Emanet 1e15 sonrası API günlüğü | İstisna ayrıntısı | `Sayı taşması 400'e çevrildi … DbUpdateException` + iç halka `MySqlException … 'has_gram_bakiye'` | — |

Temizlik (tur 2): API (PID dosyasından) ve vekil durduruldu, CDP Chrome kapatıldı; 5188/5189/9981 `lsof` → 0. `zsh betikler/kiraci-temizle.sh 01M3Z1Y83RYHPMRB20NAM01XS1 --uygula` → rc 0. tenant_id kolonu olan **75** tabloda bu kimlik için toplam satır **0**. tenants başlangıçta **16**, sonda **16**. QA-TEST- kiracılarına dokunulmadı. Anahtar ve fikstür dosyaları /private/tmp altında chmod 600 tutuldu ve iş sonunda boşaltıldı. Çıktılarda parola/anahtar sayımı (`grep -ci "sifre|password|anahtar"`) → 0.

Tur 1 kaydı:

Kiracı `PAKETPATCH-3-TEST …` sistem davetiyle açıldı. Rastgele anahtar /private/tmp altında chmod 600 tutuldu ve yalnız API ortam değişkenine verildi; iş sonunda anahtar dosyası da fikstür dosyası da boşaltıldı. Betik: `ham/PATCH-3-olcum/canli-p3.mjs`. 1. koşum `canli-p3-kosum1.txt`; 2. koşum `canli-p3-kosum2.txt`, sözlük düzeltmesinden sonra API yeniden başlatılarak alındı ve son kanıt odur. SQL hatası yalnız çıkış koduyla yazılır. Çıktılarda parola dizgesi `grep -c` → **0**. **İki koşumda da 5xx sayısı: 0.**

| Adım | Beklenen | Gerçek | SQL kanıtı |
|---|---|---|---|
| POST alis-fisi kalem adet 1,0004 (veresiye) | 200, kolonda 1,000 | 200 | `adet 1.000 · tl 100.00` |
| POST alis-fisi adet 0,0004 | 400 alan | 400 alan:kalemler[0].adet "Adet 0,001 ile 999.999.999,999 arasında olmalıdır" | hareket sayısı 2 / 2 |
| POST rezerv adet 0,0004 | 400 alan:adet | 400 alan:adet | — |
| PATCH rezerv `"1.500"` / `1e30` / `0.0004` / `true` / `aciklama:5` / `bitisGunu:20261231` | 400 alan (500 değil) | 6/6 400 (adet ×4, aciklama, bitisGunu) | `2.000 ∅ ∅ bloke 2.000` önce = sonra (DEĞİŞMEDİ) |
| PATCH rezerv `{"adet":null}` | 200, korur | 200 | `2.000 … 2.000` |
| PATCH rezerv `{"adet":0.0005}` | 200, 0,001 | 200 | `0.001 … bloke 0.001` |
| Satış 19 yazma ucu, kolon tavanını aşan 1e20 | 400 + alan | **19/19** 400 + alanlar, Türkçe ileti | hareket yazılmadı |
| POST emanet/giris gram 1e15 (uç kapısı yok → ağ) | 400 | 400 alan:hasGramBakiye "Has gram bakiyesi çok büyük — …" | emanet hareketi 0/0, has bakiyesi 0/0 |
| POST ürün satisFiyati 1e14 / işçilik 1e11 / minStok 1e10 | 400 alan | 3/3 400, kendi alanıyla | ürün sayısı 1 / 1 |
| PATCH ürün `{"barkod":null,"satisFiyati":null}` | 200, NULL | 200 | `barkod NULL · satis_fiyati NULL`, diğerleri aynı |
| PATCH ürün `{"aciklama":""}` (geriye uyum) | 200, NULL | 200 | `aciklama NULL` |
| PATCH ürün `{"ad":null,"kod":null,"iscilikTl":75}` | 200, ad/kod korunur | 200 | ad/kod aynı, işçilik 75.00, isteğe bağlılar aynı |
| PATCH kategori `{"ad":null,"sira":7}` | 200, ad+üst korunur | 200 | `Burma <ust> 7` |
| PATCH kategori `{"ustId":null}` | 200, kök | 200 | `Burma NULL 7` |
| PATCH şube `{"ad":null}` / `{"ad":"  "}` / `{"adres":""}` | 200 korur / 400 alan:ad / 200 NULL | aynen | ad korundu, adres NULL |
| Range iletisi (avans iade 1e20, işçilik 1e20 ve 0, hurda satış 0, alış iade 1e20) | Türkçe, tek ileti | "Tutar 0,01 ile 9.999.999.999.999,99 arasında olmalıdır" · "İşçilik (TL) 0 değerinden büyük, en çok 9.999.999.999.999,99 olmalıdır" · "Has gram 0,000001 ile 999.999.999,999999 arasında olmalıdır" · "İade tutarı (TL) …" | — |

API iki kez başlatıldı (ikincisi temiz derlemeden sonra) ve ikisi de yalnız kendi PID'iyle durduruldu. 5188, 5189, 9981 ve 9982 portlarında `lsof` → 0 satır. 5199/5200'e dokunulmadı.
Test kiracıları `01M3YXM664KK89PFS10C78TQ69` ve `01M3YXZK0WR9X0CTY5R3MM1JG3`, `zsh betikler/kiraci-temizle.sh <id> --uygula` ile silindi. tenant_id kolonu olan 75 tabloda iki kimlik için toplam satır **0**; `PAKETPATCH-3%` kiracı **0**. tenants başlangıçta **16**, sonda **26**. Fark bu paketin değil: listede eşzamanlı şeritlerin kiracıları görülüyor (`PAKETKAR-RAPOR-2-TEST-HAKEM-H/K/T` ×10). Bu paketin kiracı sayısına katkısı 0. QA-TEST- kiracılarına dokunulmadı.

## 11. Ekran — gerçek (başsız) Chrome ölçümü

### 11-T2. Tur 2 ölçümü (son kod; hakem senaryosu dahil)
Betik `olc-p3-t2.mjs` (CDP 9981, profil `/private/tmp/kuyumcu-serit/PATCH-3/profil-t2`, vekil 5189 → 5188). Ham sonuç `olcum-t2.jsonl` (24 satır), özet `olc-t2-ozet.txt`, görüntüler `foto-t2/` (24 ölçüm + 6 işlev). Her mod × tema × genişlik için üç ölçüm yapıldı: ürün çekmecesi, kategori çekmecesi ve **zorunlu senaryosu**. Zorunlu senaryosunda kod ve gram boşaltılıp Kaydet'e basılıyor.

| Ölçüm | 1280 / 375 · BASİT / PRO · açık / koyu (8 kombinasyonun tamamı) |
|---|---|
| Ürün çekmecesi K1 · K3 · K5 · K10 · yatay | 0 · 0/0 · 0 · 0 · 0 (8/8) |
| Kategori çekmecesi K1 · K3 · K5 · K9 · K10 · yatay | 0 · 0/0 · 0 · 0 · 0 · 0 (8/8) |
| Zorunlu senaryosu, Kaydet ÖNCESİ | `d-kod` ve `d-gram`: yıldız var, "(isteğe bağlı)" yok (8/8) |
| Zorunlu senaryosu, Kaydet SONRASI | ikisinde de `aria-invalid=true`; kod altında "Bu alan zorunludur.", gram altında "Sıfırdan büyük bir değer girin."; **odak `d-kod`'da ve mesajı ekranda görünüyor** (8/8); çekmece açık kalıyor; bant "Form eksik ya da hatalı — işaretli alanları düzeltin." (8/8); K1/K3/K5/yatay 0 |
| SQL (ölçüm sonrası) | `P3-A 10.0000`: kod ve gram DEĞİŞMEDİ, PATCH gitmedi. İşlev adımı: barkod silindi → `NULL`, açıklama `Vitrin T` korundu; kategori köke taşındı → `Burma NULL 7` (`sql-tarayici-t2.txt`) |

Mod ve tema her satırda gerçekte doğrulandı (`modGercek`, `data-tema` ve gövde zemini). K9 gerekçesi tur 1 ile aynı. Çekmecedeki 1–2 sayım, sabit alt eylem çubuğunun (İptal/Kaydet) arkasından kayan içerikten geliyor (örnek: `#alan-hata-d-gram ⟂ İptal`, `Gerçek Milyem ⟂ İptal`). Sayfa kapsamındaki 4–6 sayım, sayfanın üstüne binen çekmece katmanından geliyor. Hakemin gördüğü `#d-gram-onizleme "0,000 g"` öğesi artık hiçbir satırda yok. Dar sütun: çekmece K2 %96–97, sayfa K2 %97–98.
Read ile görülen görüntüler: `zorunlu__PRO_aydinlik_1280` (kod kırmızı çerçeveli, yıldızlı, altında mesaj, barkod "(isteğe bağlı)"), `zorunlu__BASIT_karanlik_375` (kod ve gram yıldızlı ve kırmızı, gram kutusu boş, "0,000 g" yok, koyu temada okunur), `urun__PRO_karanlik_1280` (Kod * yıldızlı, normal durum) ve `kategori__BASIT_aydinlik_375`.

### 11-T1. Tur 1 kaydı

Değişen ekran: `Pages/Urunler.razor` (düzenleme çekmecesinin gövde sözleşmesi; işaretleme ve CSS değişmedi). Kategori düzenleme ekranı (`/urunler/agac`) dosyası değişmedi, ama sözleşmesi değiştiği için o da ölçüldü ve işlevi denendi.
Araç: `betikler/tur/cdp.mjs` (CDP 9981/9982, profil /private/tmp altında), `inceleme-vekili.py` 5189 → 5188 (parolasız oturum), `sikisma-olc.js`. Ölçüm iki kapsamda yapıldı: sayfa (`main`) ve açık çekmece (`aside.cekmece`). Betikler `olc-p3.mjs` ve `olc-p3-kapali.mjs`; ham sonuç `olcum.jsonl`, `olc-cikti.txt` ve `olcum-cekmece-kapali.txt`.

| Rota (çekmece açık) | Genişlik | Mod | Tema (data-tema / gövde zemini) | Sayfa K1 · K3 · K5 · Y | Çekmece K1 · K3 · K5 · K9 · K10 · Y |
|---|---|---|---|---|---|
| /urunler → Ürünü Düzenle | 1280 | BASİT | aydinlik rgb(242,244,247) | 0 · 0/0 · 0 · 0 | 0 · 0/0 · 0 · 1 · 0 · 0 |
| /urunler → Ürünü Düzenle | 375 | BASİT | aydinlik | 0 · 0/0 · 0 · 0 | 0 · 0/0 · 0 · 1 · 0 · 0 |
| /urunler → Ürünü Düzenle | 1280 / 375 | BASİT | karanlik rgb(14,18,25) | 0 · 0/0 · 0 · 0 | 0 · 0/0 · 0 · 1 · 0 · 0 |
| /urunler → Ürünü Düzenle | 1280 / 375 | PRO | aydinlik / karanlik | 0 · 0/0 · 0 · 0 | 0 · 0/0 · 0 · 2 · 0 · 0 |
| /urunler/agac → kategori Düzenle | 1280 / 375 | BASİT | aydinlik / karanlik | 0 · 0/0 · 0 · 0 | 0 · 0/0 · 0 · 0 · 0 · 0 |
| /urunler/agac → kategori Düzenle | 1280 / 375 | PRO | aydinlik / karanlik | 0 · 0/0 · 0 · 0 | 0 · 0/0 · 0 · 0 · 0 · 0 |

Satırların hepsi `olcum.jsonl` içinde ayrı ayrı (16 satır, mod ve tema gerçekte doğrulanmış) duruyor. Tüm satırlarda taşan yazı 0, kırılan kısa etiket 0, "TRY" 0, [hidden] olup görünen 0, yatay kaydırma 0.
**K9 (üst üste binen) gerekçesi:**
- Çekmece açıkken sayfa kapsamındaki 4–6 sayım, sayfa öğesinin üstüne gelen çekmece katmanından kaynaklanıyor (örnek: "Hepsi ⟂ Barkod"). Aynı sayfalar çekmece kapalıyken 8/8 kombinasyonda **K9 = 0** (`olcum-cekmece-kapali.txt`).
- Çekmece içindeki 1–2 sayım, sabit alt eylem çubuğunun (İptal/Kaydet) altından kayan içerikten geliyor (örnek: "Gerçek Milyem ⟂ İptal"). Bu, Cekmece bileşeninin tasarımıdır.
- Bu paket işaretlemeye dokunmadı; düzen paket öncesiyle birebir aynı.
- Dar sütun: K2 ölçülemedi (çekmece içinde kart yok); sayfa K2 %97–98.

Görüntüler `ham/PATCH-3-olcum/foto/` altında 22 dosya. Read ile görülenler: `urun__PRO_aydinlik_1280`, `urun__BASIT_karanlik_375`, `kategori__PRO_karanlik_375`, `kategori__BASIT_aydinlik_1280`, `islev-urun-yeniden-acildi`. Etiketler tek satırda, alanlar tam genişlikte, koyu temada okunur kontrast var, alt çubuk sabit.
**İşlev (PRO, açık, 1280), gerçek tıklamayla:**
1. Ürün çekmecesinde barkod `B-TARAYICI` silindi ve Kaydet'e basıldı. Çekmece kapandı, listede barkod "—" görünüyor. Çekmece yeniden açılınca barkod boş, açıklama "Vitrin T" korunmuş. **SQL: `barkod NULL · aciklama Vitrin T · ad/kod aynı · rfid RF-1`**. Ürün sayfasında paketten önce aynı işlem barkodu silmiyordu (null = koru).
2. Kategori çekmecesinde Üst kategori seçicisi açılıp "— kök seviye —" seçildi ve Kaydet'e basıldı. **SQL: `Burma NULL 7`**. Paketten önce kategori köke taşınmıyordu.

`sql-tarayici.txt` ve görüntüler: `islev-urun-barkod-bos-once`, `islev-urun-kaydedildi`, `islev-urun-yeniden-acildi`, `islev-kategori-ust-liste-acik`, `islev-kategori-ust-bos-once`, `islev-kategori-kaydedildi`.

## 12. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` (yalnız bu pakete ait olanlar; ağaçtaki diğer değişiklikler başka şeritlerindir):
- ` M dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — AlisFisi yuvarla/kontrol/tavan; DTO Range tavanları (26 + Takas 2); Yuvarla3/6 → KolonTavani
- `?? dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.P21.cs` — rezerv oluştur/PATCH; DTO Range tavanları (7)
- ` M dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` — UrunGuncelleDto üç durumlu 14 alan; Guncelle; UrunKolonKapisi (POST+PATCH)
- `?? dotnet/Kuyumcu.Api/Modules/Urun/KategoriModulu.cs` — UstId üç durumlu
- ` M dotnet/Kuyumcu.Api/Modules/Sube/SubeModulu.cs` — ad null = koru; adres/telefon "" → NULL
- `?? dotnet/Kuyumcu.Api/Modules/KolonTavani.cs` — UstAdet/UstTutar/UstHas, Yuvarla6, Yuvarla3, JsonMetin
- ` M dotnet/Kuyumcu.Api/Program.cs` — YALNIZ hata çevirisi: sayı taşması ağı + `SayiTasmasiCevir`, `KolonAdi`, `KolonAlani`
- `?? dotnet/Kuyumcu.Core/I18n/DogrulamaSozlugu.cs` — AralikMesaji tek ileti, exclusive deseni, Okunur, 17 alan adı (**dosya alanı listesinde adı yoktu**; madde 5 "AralikMesaji tek ileti" açıkça bu dosyayı istiyor)
- ` M dotnet/Kuyumcu.Web/Pages/Urunler.razor` — yalnız `UrunGuncelle()` gövdesi (sözlük, null = sil, KMTS anahtarı koşullu)
- `dotnet/Kuyumcu.Tests/PatchUcTestleri.cs` (YENİ), `PatchSozlesmeTestleri.cs`, `P10EkranTestleri.cs`, `Dogrulayici/PATCH2SinirTestleri.cs` (Skip kaldırıldı). Test dizini izlenmiyor.

Dokunulan sayfalar (tek dokunuş kaydı): `Pages/Urunler.razor` (tur 1: `UrunGuncelle()` gövdesi; tur 2: aynı paketin hakem düzeltmesi, yani d-kod/d-gram/u-gram/d-satis/u-satis-fiyati zorunluluk işareti, 23 alanın Durum/HataAlani bağı, `d-urun-hata` bant kimliği, `OdakIlkHataya` yedeği, `DGramDegisti`). Tur 2'de değişen diğer dosyalar: `SatisModulu.P21.cs` (AdetUst + RezervAdetKapisi), `DogrulamaSozlugu.cs` (Okunur), `Program.cs` (yalnız `LogWarning(ex, …)`), `PatchUcTestleri.cs`, `P10EkranTestleri.cs` (+2 bUnit). Sözlükler.cs, CSS ve bileşen değişmedi.

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Alış kalemi + rezerv oluştur: önce 3 hane, sonra ≤ 0 | KARŞILANDI | M1-1, M1-2; servis + HTTP testleri; canlı SQL 1.000 / 0.001 |
| (2) Rezerv PATCH dize → 400 alan:adet (500 değil) | KARŞILANDI | M2-1/2/3; 8 vaka; doğrulayıcı testi Skip'ten çıktı ve yeşil; canlı 6/6 |
| (3) Satış TÜM yazma uçları kolon tavanı → 500 yok | KARŞILANDI | 26 HTTP vakası; M3-1/2/3; canlı 19/19 400, 5xx = 0 |
| (4) Ürün/kategori "Verildi" DTO + istemci + "" geriye uyum + gerekçe | KARŞILANDI | § 3; M4-1…M4-8; canlı + tarayıcı + SQL NULL |
| (4) Şube/ürün null tutarlılık kuralı | KARŞILANDI | § 3 tablo; M4-5; canlı |
| (5) Tek 6 hane yardımcısı | KISMEN (dosya alanı) | Yardımcı + Satış bağlı (M5-1, M5-7). Diğer 23 ToEven çağrısı alan dışı, aşağıda listelendi |
| (5) POST kolon tavanları (Satış/Ürün/Fatura/Sipariş/Vezne/Satın alma) | KARŞILANDI | Satış DTO; Ürün uç kapısı (M5-4); diğer dördü PATCH-2/SOZLESME uç kapısı + ağ |
| (5) MySQL 1264 → 400 (alanıyla) | KARŞILANDI | M5-2, M5-3, M5-6; canlı emanet 400 |
| (5) AralikMesaji tek ileti | KARŞILANDI | M5-5, M5-8, M5-9; canlı iletiler |
| Mutasyon (her kural ≥1 kırmızı, cmp AYNI) | KARŞILANDI | Tur 1 25/25 (25 günlük); tur 2 31/31 (31 günlük) |
| Tur 2 hakem KRİTİK ×2 (kod/gram sessiz etkisizlik, etiket çelişkisi) | KARŞILANDI | § 0; bUnit ×2; M6-4/5/6; Chrome 8/8 |
| Tur 2 ORTA (kültürden bağımsız Range iletisi) | KARŞILANDI | M6-1; LC_ALL=C canlı |
| Tur 2 DÜŞÜK (rezerv tek üst sınır, taşma günlüğü, mutasyon kanıtı) | KARŞILANDI | M6-2/3; günlük alıntısı; mut-log 25 + mut-log-tur2 31 |
| Ekran: 1280 + 375 · BASİT + PRO · açık + koyu, Read ile görüldü | KARŞILANDI | § 11, 16 ölçüm + 6 işlev görüntüsü |
| Build 0/0 | 0 hata · **2 uyarı başka şeritte** | Uyarılar `Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49` ve `KesifP2Testleri.cs:455`; bu paketin dosyalarında uyarı 0 |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | 303 ✓ |
| Tam süit 0 yeni kırmızı | KARŞILANDI | Tur 2: 9736/9738, 0 başarısız, 2 önceden atlanmış (tur 1: 9622/9625) |
| Test kiracısı silindi | KARŞILANDI | satır 0 |
| Sır basılmadı | KARŞILANDI | parola sayımı 0; anahtar ve fikstür dosyaları boşaltıldı |

## 14. Açık kalanlar

- **5199 inceleme sunucusu kapalı** (doğrulayıcı bulgusu). Paketle ilişkisi yok ve 5199'a dokunulmadı. Orkestratörün gece yetkisi kapsamında yeniden başlatması gerekiyor.
- `UrunAgaci.razor` kategori çekmecesinde "Üst kategori" etiketi "(isteğe bağlı)" ibaresini taşımıyor (Read görüntüsü). Tek dokunuş kuralı gereği bu pakette açılmadı; sahibi ürün ağacı ekranı.
- Taşma günlüğü (`LogWarning(ex, …)`) için birim testi yok. Bu davranış değil gözlemdir; kanıtı canlı günlük alıntısıdır.

- **6 hane ToEven çağrıları (dosya alanı dışı):** Bu yerler ToEven kullanıyor ve tek yardımcıya (`KolonTavani.Yuvarla6`) bağlanmalı. Önem sırası:
  - **Deftere ya da kolona yazanlar (öncelikli):** `Kurulum/AcilisYazici.cs` 147/179/223/278 · `Maliyet/MaliyetMotoru.cs` 65/77/164/258/281/346.
  - **Yalnız gösterenler:** `Sayim/SayimModulu.cs` 150/173 · `Yerlesim/YerlesimServisi.cs` 345/399 · `Emanet/EmanetModulu.cs` 318 · `Rapor/RaporModulu.cs` 138/1138/1182/1273/1319/1335/1349 · `Rapor/RaporOrtak.cs` 223. Toplamların hepsi zaten 6 haneli değerlerin toplamıdır, bu yüzden yuvarlama sonucu değiştirmez.
  - Sahipler: Rapor = KAR-RAPOR-2 şeridi; Maliyet/Kurulum/Sayım/Yerleşim/Emanet = orkestratör.
- **Geriye uyum takvimi:** ürün, kategori ve şube PATCH'inde `""` ile silme bir sürüm boyunca kabul edilir. Kaldırılınca `""` için ne yapılacağı (400 mü, null'a eşit mi) ürün kararıdır. Kuyumcu.Web artık `""` göndermiyor.
- **Ağın alan seçimi:** 1264'te alan, taşan kolondan türetilir. Kullanıcının gönderdiği alanla aynı olmayabilir (emanet gram → hasGramBakiye). Emanet girişinde (`EmanetGirisDto.GramAgirlik`, `Range(…, double.MaxValue)`) uç kapısı yok; Emanet sahibine önerilir: KolonTavani.Denetle.
- **1406 (Data too long):** metin kolonu taşması ağa alınmadı (istemde yalnız 1264 vardı). DTO MaxLength'leri birincil korumadır.
- **Kasa/Cari/Virman/Çıktı PATCH uçları:** istem gereği bu pakette yok (PARA-DEMO-3 sonrası PATCH-4).
- **Başka şeritlerin derleme uyarıları:** `SUBEKAPSAMASinirTestleri.cs:49` (CS8601, `Hareket.Maden` atanması) ve `KesifP2Testleri.cs:455` (xUnit2029). Ayrıca derleme sırasında bir kez başka bir şeridin eşzamanlı derlemesiyle Web `ConvertDllsToWebCil` dosya yarışı yaşandı (MSB4018). 30 sn sonra tekrar denenince yeşildi; testler o derlemeden sonra koşuldu.
- `/private/tmp/x`: mutasyon özetinin yanlışlıkla yazılan bir kopyası (kuyumcu-serit dışı). `printf ''` ile boşaltıldı; silinmesi kurucuya bırakıldı.
- `UrunGuncelleDto` dışındaki tüm PATCH uçlarında zorunlu alan null = koru kuralının taranması (Cari, Fiyat, Kampanya …) bu paketin dosya alanı dışında; kural § 3'te yazılı, uygulaması orkestratörde.

## 15. Kurucu / mali müşavir

Mevzuat hükmü içeren değişiklik yok. Değişiklikler girdi doğrulaması, kolon kapasitesi, yuvarlama kuralının teke indirilmesi ve iletilerden ibaret; eşik, oran ve vergi kuralı eklenmedi ya da değişmedi. Mali müşavire soru yok.
