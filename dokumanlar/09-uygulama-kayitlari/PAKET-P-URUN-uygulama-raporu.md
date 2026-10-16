# PAKET P-URUN — uygulama raporu (K6 · Y19 · O13)

Dalga DENETIM-VERI · şerit t3 (API 5164 · vekil 5165 · CDP 9612/9613 · KUYUMCU_TEST_DB=kuyumcu_test_t3) · Taşkın (backend) + Ertuğ (ön yüz) · 11.10.2026 · COMMIT YOK.
Kaynak: `EKRAN-DENETIM-KULLANICI-GOZU-2026-10-10.md` (K6, Y19, O13).

## 1. Spesifikasyon ↔ kod doğrulaması (mevcut durum)

| İddia | Kodda bulunan | Sonuç |
|---|---|---|
| K6: yeni ürün formunda ayar "18 Ayar" dolu gelir | `Urunler.razor` `yeniAyar = "K18"` (alan + `YeniModalAc`); ayrıca maden değişince `MadeninIlkAyari` ilk ayarı OTOMATİK seçiyordu | DOĞRU (+ ikinci varsayılan kaynağı bulundu) |
| K6: ayarsız ürün kaydedilebilir mi? (sunucu) | Controller yolunda `[Required]` var → 400. Servis içi yolda `Ayarlar.Bul(null)` → `ArgumentNullException` (500) | Servis içi kontrol EKSİKTİ |
| Y19: gram ve açılış stoğu ekranın altında | Ölçüldü (Yıldız koyu 1280×800, yeni sıra öncesi): `u-gram` alt kenarı 697 px, pencere gövdesi 652 px'te bitiyor → GİZLİ | DOĞRU |
| Y19: boş Kaydet'te gram hatası görünmüyor | Ortak `kuyumcuOdak.alana` zaten modal gövdesini kaydırıyor; gerçek Chrome'da 16/16 kombinasyonda hata görünür ve odak `u-gram`'da (bkz. § 6) | Mevcut kod doğru; kök neden alanın ilk ekranda olmamasıydı |
| O13: liste AYAR sütunu 916/750 | `Urunler.Yildiz.cs` listede `YildizMilyem(u.AyarId)` — yalnız YILDIZ listesi; Platin/PRO ızgarası zaten `KurKaynakAdi.AyarAdi` ("22 Ayar") | DOĞRU (yalnız Yıldız) |
| Spesifikasyonda "Birleşik koşullar" bölümü | Kaynak belgede bu başlık YOK (grep 0). Bağlayıcı koşullar istemdeki EK KOŞULLAR'dan alındı | Not |

Mevzuat: bu pakette yasal hüküm yok (ayar seçimi iş kuralı). Mali müşavire soru yok.

## 2. Yapılanlar

**Sunucu (Taşkın) — `Kuyumcu.Api/Modules/Urun/UrunModulu.cs` (yalnız doğrulama):** `Olustur` içinde `AyarBul`'dan önce `string.IsNullOrWhiteSpace(dto.AyarId)` → `BadRequestException(PUrunSozlugu.AyarZorunlu).AlanIle("ayarId")` (servis içi yol 500 yerine 400 + alan). Controller yolu değişmedi (`[Required]` "Ayar zorunludur").

**Sözlük — `Kuyumcu.Core/I18n/Sozlukler.PUrun.cs` (yeni, TR+EN konumsal record):** `AyarSecin`, `AyarSecilmedi`, `AyarZorunlu`, `AdAyarCelisiyor`.

**Ekran (Ertuğ) — `Pages/Urunler.razor`, `Pages/Urunler.PUrun.cs` (yeni partial), `Pages/Urunler.Yildiz.cs`:**
- K6 fail-closed: `yeniAyar` varsayılanı `""`; seçici "Ayar seçin" boş seçenekle, `Zorunlu` (*) ve alan altı sunucu hatası (`Hata=form.Mesaj("u-ayar")`, `ayarId` eşlemesi zaten vardı ama çizilmiyordu). Seçilmeden Kaydet KİLİTLİ, sebep altlıkta "Ayarı seçin." (`u-kaydet-kilit`, mevcut kalıp). Kod içi koruma da var (kilit atlanırsa POST yok, alan altı mesaj).
- Maden değişince ayar ilk ayara ATLAMAZ: `MadeninAyariVarsayilansiz` — seçili ayar madende varsa korunur, madenin TEK ayarı varsa (çelik) kendiliğinden konur (UX-S22), değilse boşalır ve Kaydet yeniden kilitlenir.
- Ad ↔ ayar çelişkisi: `AdindakiAyar` adda "22 Ayar / 14 ayar / 24K / 18 k / 22 krt / 21 ayr" yakalar (yalnız altın ayarları; "karat/ct" BİLEREK yok — pırlantada ağırlıktır; ondalık parçası ve iki farklı ayar = tahmin yok). Çelişkide ayar kutusunun altında tek satır amber `u-ayar-celiski` "Adda 22 Ayar yazıyor, seçilen 18 Ayar. Doğru mu?" — kaydı ENGELLEMEZ. Düzenleme çekmecesinde aynı uyarı (`d-ayar-celiski`, UX-S26).
- Y19 sıra: ad / maden+ayar / kod+gram / barkod+açılış stoğu / ürün grubu / "Daha fazla". Telefonda (tek sütun) ilk beş kutu ad · maden · ayar · kod · gram. Maden ayardan önce kalır (PAKET 20 bağı). Ayarın altındaki "Seçilen madenin saflık tablosu" açıklama satırı kaldırıldı (sade ilke; sözlük anahtarı duruyor).
- O13: Yıldız listesi AYAR sütunu `YildizAyarKisa` → "22" / "18" / "925" (formla aynı ad kaynağından, `KurKaynakAdi.AyarAdi`'nın ilk sayısı). `YildizMilyem` silinmedi (Etiketler Yıldız'ı ve Y5A_07 kullanıyor).
- Mevcut ürünler DEĞİŞMEDİ; migration YOK. Yeni CSS yok, satır içi stil yok, yeni element id'leri `u-ayar-celiski`, `d-ayar-celiski` (mevcut id'ler aynı).

**Sapma/gerekçe:**
1. Mevcut 8 test dosyasında (başka paketlerin yeni ürün testleri) `#u-gram` girişinden sonra tek satır `SeciciYardim.Sec(e, "u-ayar", "K18")` eklendi; iki testte boş "Ayar seçin" seçeneği sayımdan/kod-ad kuralından ayrıldı. Neden: bu testler varsayılan K18'e dayanıyordu; K6 bu varsayılanı kaldırınca 16 test kırmızıya döndü (t1.log). Seçim eklenince testler KENDİ kuralını (taş/görsel/karat) sınamaya devam eder — aksi hâlde ayar kilidi yüzünden "kilitli" iddiaları boşuna geçerdi. Dosyalar: GocCTestleri, S33UrunlerEtiketlerEkranTestleri, UrunEkranTestleri, P10EkranTestleri, P16EkranTestleri, UrunAcilisAtomikTestleri, UrunForm2Testleri, KesifP4EkranTestleri (toplam 14 satır; yedekleri `/private/tmp/kuyumcu-serit/P-URUN/test-yedek/`).
2. "Modal üst menünün üstüne biniyor" (Y19 ilk cümlesi): Yıldız'da yeni ürün penceresi sağdan tam boy çekmece (`panelUst 0`); bu ortak `ModalPencere`/Yıldız katman davranışı, DOSYA ALANI dışında — dokunulmadı (açık kalan).
3. "Arkadaki sayfada ikinci dolu Yeni ürün düğmesi" (Y19): Yıldız liste başlığı YILDIZ-4D/5A şeridinin alanı — dokunulmadı (açık kalan).

## 3. DoD sözleşmesi

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Ürün kartı (`urunler`) | Mevcut PATCH `/urunler/{id}` + ekranda "Düzenle" çekmecesi (ayar dahil), pasife al/aktifleştir mevcut | Yeni varlık YOK. Yeni form alanı yok; ayar zaten düzenlenebilir. Çelişki uyarısı düzenlemede de var. |

## 4. Testler

Yeni dosya `Kuyumcu.Tests/PUrunTestleri.cs` — 17 yöntem / 37 durum:
- Saf (PUrunSafTestleri): PU_S1 (14 ad kalıbı) · PU_S2 çelişki · PU_S3 maden→ayar · PU_S4 Yıldız kısa ayar · PU_S5 sözlük TR/EN.
- bUnit (PUrunEkranTestleri): PU_B1 boş ayar → kilit + sebep + POST yok + alan altı mesaj (BASIT/PRO) · PU_B2 "22 Ayar"+18 → uyarı, kilit yok, gövde K18 (BASIT/PRO) · PU_B3 doğru ayar uyarı yok, gövde K22 · PU_B4 maden değişince ayar boşalır + kilit · PU_B5 yeniden açılışta seçim sızmaz · PU_B6 alan sırası (BASIT/PRO) · PU_B7 düzenleme çekmecesinde uyarı · PU_B8 Yıldız listesi "22"/"18", 916/750 yok.
- DB (PUrunVeriTestleri): PU_D1 servis ayarsız (null/""/"  ") → 400 + alan ayarId, 0 satır · PU_D2 seçilen ayar, milyem 916, has.
- HTTP (PUrunHttpTestleri): PU_H1 ayarId yok/null/"" → 400, 0 satır · PU_H2 K22 → 201.

Filtreli koşumlar: `FullyQualifiedName~PUrun` 47/47 (filtre adı "PUrun" geçen başka sınıfları da yakalıyor; trx'te `Kuyumcu.Tests.PUrun*` = 37/37) · ürün kapsamı (PUrun|Urun|GocC|KesifP4|P16Ekran|P10|S33Urunler|UX1C1Ekran) 659/659 (t5.log).

**Tam süit (son kodla, tur başına bir kez):** `dotnet test … --logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pP-URUN` → Toplam **12569** · Geçti **12562** · Başarısız **4** · Atlanan 3 (önceden var olan Skip). Bu paketin 37 testi 37/37 geçti.
Kırmızılar (trx'ten) — hepsi devir listesinde (P-SAYIM / SATINALMA-EKRAN-2 raporları), **FARK = 0 yeni kırmızı**:
- `RaporDortTestleri.RD_KH01_…`, `RD_KH02_…` — tarih bağımlı (rapor paketi sahibi).
- `Yildiz5ATestleri.Y5A_04_emanet_satirlari_fifo_storno_esik` — YILDIZ-4D/5A şeridi (t1).
- `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_…` — YASAL şeridi.

## 5. Mutasyon kanıtları (yöntem: yedek → `perl -0777 -pi` tek eşleşme → `touch` → `dotnet build Kuyumcu.Tests` (0 Hata şartı; derleme hatasında test KOŞULMAZ) → PUrun filtresi → yedeği geri kopyala → `cmp`)

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M1 | Kaydet kilidinden "ayar seçilmedi" dalı silindi | PU_B1 (BASIT, PRO), PU_B4, PU_B5 | AYNI |
| M2 | `YeniModalAc` içinde `yeniAyar = "K18"` (eski varsayılan) | PU_B1 (BASIT, PRO), PU_B5 | AYNI |
| M3 | Maden değişince ilk ayara atla (`Count > 0 ? [0]`) | PU_S3, PU_B4 | AYNI |
| M4 | Sunucu servis içi ayar kontrolü silindi | PU_D1 (null, "", "  ") | AYNI |
| M5 | Çelişki koşulu tersine (`addaki != seciliAyar`) | PU_S2, PU_B2 (BASIT, PRO), PU_B3, PU_B7 | AYNI |
| M6 | Yıldız listesi `YildizAyarKisa` → `YildizMilyem` | PU_B8 | AYNI |
| M7 | Ürün grubu satırı zorunluların önüne taşındı (Y19 sırası bozuldu) | PU_B6 (BASIT, PRO) | AYNI |

Dürüstlük notu (md.1): ilk M1 koşusu 0 kırmızı verdi — Web derlemesi artımlı kontrolde yeniden derlenmemişti (paylaşılan ağaçta başka şeridin eşzamanlı derlemesi; `Kuyumcu.Web.dll` saati mutasyondan eskiydi). Betik `touch` + dll saat yazımı + "0 Hata değilse test koşma" ile düzeltildi, M1 yeniden koşuldu (4 kırmızı). M5'in ilk iki denemesi derlemede düştü (biri `addaki` tanımsız — mutasyon biçimi değiştirildi; biri başka şeridin `blazor.boot.json` kilidi) → test koşulmadı, geri alındı, cmp AYNI, yeniden koşuldu. Son mutasyondan sonra temiz derleme: `build-son.log` 0 Hata · 8 Uyarı (hepsi başka dosyalarda: SUBEKAPSAMASinirTestleri:49, KesifP2Testleri:455, SonKalanlarTestleri:593/824, Yildiz2Testleri:194/234, YildizKatmanTestleri:144/189; bu paketin dosyalarında 0).

## 6. Gerçek Chrome ölçümü (başsız Chrome, cdp.mjs, CDP 9612, tek profil `/private/tmp/kuyumcu-serit/P-URUN/profil`)

Senaryo her kombinasyonda: şema/tema/mod API ile → `/urunler` → "Yeni ürün" → açılış ölçümü + sikisma-olc `__olcTam` → gerçek yazım (CDP `Input.insertText`): ad "22 Ayar Halka Küpe", kod, gram 3,40 → ayar seçici tıklanır, "18 Ayar" seçeneği tıklanır → ölçüm → gram silinir (Backspace) → Kaydet → hata/odak ölçümü → gram 3,40 → Kaydet.

| Şema · tema · mod · genişlik | Zorunlular (ad·kod·ayar·gram) ilk ekranda | Açılışta ayar / Kaydet | Çelişki uyarısı (22↔18) | Boş gram → hata görünür / odak | Kayıt | K1 | K3 taşan/kırık | K3c | K5 TRY | K9* | K10c | Yatay |
|---|---|---|---|---|---|---:|---|---:|---:|---:|---:|---:|
| platin açık BASIT 1280 | 4/4 (+açılış stoğu) | "Ayar seçin" / kilitli "Ayarı seçin." | var, kilit yok | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| platin açık BASIT 375 | 4/4 | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| platin açık PRO 1280 | 4/4 (+açılış) | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| platin açık PRO 375 | 4/4 | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| platin koyu BASIT 1280 | 4/4 (+açılış) | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| platin koyu BASIT 375 | 4/4 | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| platin koyu PRO 1280 | 4/4 (+açılış) | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| platin koyu PRO 375 | 4/4 | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| yıldız açık BASIT 1280 | 4/4 (+açılış) | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| yıldız açık BASIT 375 | 4/4 | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| yıldız açık PRO 1280 | 4/4 (+açılış) | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| yıldız açık PRO 375 | 4/4 | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| yıldız koyu BASIT 1280 | 4/4 (+açılış) | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 1** | 0 |
| yıldız koyu BASIT 375 | 4/4 | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| yıldız koyu PRO 1280 | 4/4 (+açılış) | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |
| yıldız koyu PRO 375 | 4/4 | aynı | var | evet / u-gram | Kaydedildi | 0 | 0/0 | 0 | 0 | 6 | 0 | 0 |

Önce/sonra (Yıldız koyu BASIT 1280, aynı ölçer): yeni sıradan önce `u-gram` GİZLİ (alt 697 > gövde 652), `u-acilis-stok` GİZLİ; sonra ikisi de görünür, yalnız `u-tip` (ürün grubu, isteğe bağlı) aşağıda. 375'te ara düzende `u-gram` GİZLİ (687 > 664 Yıldız, 729 > 677 Platin) çıktı → sıra "kod+gram / barkod+açılış" olarak değiştirildi → 16/16 görünür.

\* K9 "binen" 6: hepsi pencerenin ARKASINDAKİ sayfa öğeleri ile pencere içi öğe kesişimi (ör. `span "18 kayıt gösteriliyor" ⟂ secici-metin "Altın"`, `#urun-yeni-ac ⟂ #u-maden-kilit`, `span.yz-liste-d "3,40" ⟂ BARKOD etiketi`) — örtü katmanının altında kalan arka plan; ölçer kapalı katmanı ayırmıyor. Pencere içinde gerçek binme yok (görüntülerde doğrulandı).
\*\* K10c 1: `#u-kod "PU-YKB1280-85899"` — ölçüm betiğinin ürettiği 16 karakterlik test kodu yarım sütun kutuya sığmadı (girdi değeri, kutu kayar). Kod kutusunun genişliği bu pakette değişmedi (önceden de yarım sütun); gerçek kodlar (BLZ-22-01) sığıyor. Bilgi.

Yıldız tipografi (`yildiz-tipo-tara.mjs --rotalar /urunler --katman 1`, 1280+375, koyu+açık): BASIT ve PRO **KURAL DIŞI 0**, native select 0, yatay 0, katman 10; "kesik yazı" 1/genişlik (ölçer ayrıntı vermiyor; liste sayfası, bu paketin değiştirdiği hücre "22" kısa). Tuval kıyası (Bugün/Tezgâh/Müşteriler/Tüm ekranlar): bu paket o ekranlara ve hiçbir CSS dosyasına dokunmadı → koşulmadı (YILDIZ-TEZGAH-ODEME raporundaki gerekçeyle aynı).

Yıldız listesi canlı (O13): `/urunler?q=PU-C5` 1280 ve 375 → satır "22 Ayar Halka Küpe | KJ5KVHQBHEM | **22** | 3,40 | 0 | —", başlık "ÜRÜN | BARKOD | AYAR | GRAM | ADET | FİYAT".

Ekran görüntüleri: `/private/tmp/kuyumcu-serit/P-URUN/foto/<sema>-<tema>-<mod>-<genişlik>-{1-acilis,2-celiski}.png` (32) + `yildiz-aydinlik-PRO-375-0-ust.png`. Göz ile bakılanlar (4): yıldız-koyu-BASIT-1280-1-acilis (ara düzen; gram ekran altında — sıra değişikliğini tetikledi), yıldız-koyu-BASIT-1280-2-celiski ("18 Ayar" seçili, altında "Adda 22 Ayar yazıyor, seçilen 18 Ayar. Doğru mu?", Kaydet açık), yıldız-açık-PRO-375-1-acilis (ölçer sonrası kaydırılmış gövde), yıldız-açık-PRO-375-0-ust (üstten: Ürün adı*, Maden (kilitli, sebep), Ayar* "Ayar seçin", Kod*, Gram* — hepsi görünür; altlıkta "Ayarı seçin." ve soluk Kaydet).

## 7. Canlı doğrulama (API 5164, test kiracısı `PAKETP-URUN-TEST Kuyumcu`)

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /auth/kayit (tek kullanımlık davet) | 201 | 201 |
| POST /urunler ayarId YOK | 400 | 400 `{"alanlar":{"ayarId":["Ayar zorunludur"]}}` |
| ayarId null | 400 | 400 aynı |
| ayarId "" | 400 | 400 aynı |
| ayarId "K99" | 400 | 400 "Ayar geçersiz — kabul edilen değerler: …" alan ayarId |
| ayarId "K22" | 201 | 201 ayarId K22 · milyem 916 · has 3.1144 |
| Tarayıcı ×16 (ad "22 Ayar…", kullanıcı 18 seçti, uyarıyı gördü) | kayıt seçilenle | 16/16 "Kaydedildi." |

SQL (kuyumcu_net, kiracı): `PU-C5 · K22 · 916.00 · 3.114400`; PU-C1..C4 için satır **0**; ayar_id NULL/boş ürün **0**; tarayıcı kayıtları `K18 · 750.00 · has 2.550000` (kullanıcının bilinçli seçimi — uyarı görünür, engellemez; ölçüm betiğinin ilk iki denemesi dahil 33 satır).

Temizlik: API (5164), vekil (5165), Chrome (9612/9613) yalnız kendi PID'leriyle durduruldu; portlar boş. `zsh kiraci-temizle.sh <kiracı> --uygula` → çıkış 0, `tenants` satırı 0, `urunler` 0; kendi davet satırı (`kiraci_davetleri`, olusturan_not 'P-URUN serit') silindi (0). `tenants` = 23 = başlangıç. QA-TEST- kiracılarına dokunulmadı.

## 8. Denetim

`dotnet/denetim.sh` (son kodla, denetim2.log, çıkış 1): ✗ yalnız iki sayaç, ikisi de bu paketin değil — `P-30 DEMO_INSAN_SURESI` (tarih bağımlı, kurucu kalemi) ve `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1` (`Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, MASAK şeridi). Bu paketin dokunduğu sayaçlar (DIL-SUNUCU çıplak ileti, DIL-EKRAN, GIRDI-2 diğerleri, ModalPencere düzenleme, satır içi stil) ✓.

## 9. Değişen dosyalar (bu paket)

- `dotnet/Kuyumcu.Core/I18n/Sozlukler.PUrun.cs` (yeni)
- `dotnet/Kuyumcu.Web/Pages/Urunler.PUrun.cs` (yeni)
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor`
- `dotnet/Kuyumcu.Web/Pages/Urunler.Yildiz.cs` (tek satır: liste ayar hücresi)
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs` (yalnız doğrulama)
- `dotnet/Kuyumcu.Tests/PUrunTestleri.cs` (yeni)
- Test uyarlaması (tek satır seçimler): `GocCTestleri.cs`, `S33UrunlerEtiketlerEkranTestleri.cs`, `UrunEkranTestleri.cs`, `P10EkranTestleri.cs`, `P16EkranTestleri.cs`, `UrunAcilisAtomikTestleri.cs`, `UrunForm2Testleri.cs`, `KesifP4EkranTestleri.cs`

Dokunulan sayfalar (TEK DOKUNUŞ): `Pages/Urunler.razor` (+ `.Yildiz.cs` tek satır). Not: `Urunler.razor` 02:03:12'de başka bir süreç tarafından da yazıldı/dokunuldu (içerik bu paketin yedeğiyle `cmp` AYNI çıktı); eşzamanlı şeritler (YILDIZ-4D/5A) Ürünler Yıldız dalında çalışıyor olabilir — birleştirmede dikkat.

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Ayar varsayılanı yok; seçilmeden Kaydet kilitli + sebep | KARŞILANDI | PU_B1, M1/M2; Chrome 16/16 "Ayarı seçin." kilitli |
| '22 Ayar' + 18 seçimi uyarı, kaydı engellemez | KARŞILANDI | PU_B2, M5; Chrome 16/16 uyarı + kayıt |
| Mevcut ürünler değişmez, migration yok | KARŞILANDI | şema değişikliği yok |
| Zorunlular ilk ekranda | KARŞILANDI | PU_B6, M7; Chrome 16/16 ad·kod·ayar·gram görünür |
| Hata alana kayar | KARŞILANDI | Chrome 16/16 boş gram hatası görünür, odak u-gram |
| O13 listede "22 / 18" | KARŞILANDI | PU_B8, M6; canlı satır "22" |
| HTTP ayarsız POST 400 | KARŞILANDI | PU_H1; canlı 400 ×3 |
| Canlı SQL yeni ürün doğru ayarla | KARŞILANDI | PU-C5 K22/916; ayarsız 0 satır |
| Mutasyon ≥3 kırmızı, cmp AYNI | KARŞILANDI | 7/7 |
| build 0 hata | KARŞILANDI | build-son 0 Hata (8 uyarı başka dosyalarda) |
| denetim TEMİZ (bu paketin sayaçları) | KARŞILANDI | yalnız P-30 ve GIRDI-2 (başkasının) ✗ |
| Tam süit 0 YENİ kırmızı | KARŞILANDI | 12569 test, 4 kırmızı = devir listesi, fark 0 |
| Test kiracısı silindi, tenants başlangıçta | KARŞILANDI | 23 = 23 |
| Sade ilke (tek birincil düğme, açıklama satırı yok) | KARŞILANDI | yeni uyarı yalnız çelişkide tek satır; "saflık tablosu" açıklama satırı kaldırıldı |

## 11. Açık kalanlar (sahipleriyle)

- Y19 "pencere üst menünün üstüne biniyor": Yıldız yeni ürün penceresi sağ çekmece olarak tam boy (`panelUst 0`) — ortak ModalPencere/Yıldız katman kararı, P-FORM-GORUNURLUK / YILDIZ şeridi.
- Y19 "arkada ikinci dolu Yeni ürün düğmesi": Yıldız liste başlığı (YILDIZ-4D/5A şeridi, t1).
- Açılış stoğu ipucunun 3–4 satırlık açıklaması ("Elinizde bulunan adet. Boş bırakılırsa…") sade ilkeye aykırı (açıklama satırı) — bu pakette sadeleştirilmedi (mevcut metin, K6/Y19 kapsamı dışı); öneri: tıklayınca açılan ipucuna taşınsın.
- Ölçer K9 kapalı katmanın altındaki arka plan öğelerini "binen" sayıyor (6/ölçüm) — ölçer iyileştirmesi önerisi.
- Tam süitte bu paketin olmayan 4 kırmızı: `RD_KH01`, `RD_KH02` (rapor, tarih bağımlı) · `Y5A_04` (YILDIZ-4D/5A, t1) · `Yasal4HttpTestleri.Mutabakat_bugun_iptal…` (YASAL şeridi). Denetim: `P-30 DEMO_INSAN_SURESI` (kurucu), `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (MASAK şeridi).
