# PAKET P-78 — SAYI GİRİŞİ tr-TR AYRIŞTIRMA (demo düzeltmesi) · uygulama raporu

- Tarih: 2026-09-24 · Rol: Taşkın (backend) + Ertuğ (frontend) · Model: Opus
- Dalga: DEMO DÜZELTME. İzole test VT `kuyumcu_test_h`, canlı doğrulama portu **5180**. 5181, 5199 ve 5200 portlarına dokunulmadı.
- **Durum: BİTTİ.** Paket kapısının yedi maddesi karşılandı (§ 11). Tam süitte kalan 3 kırmızı ve `denetim.sh`'taki 2 ihlal, bu koşum sırasında ağaçta **eş zamanlı çalışan menü şeridine** ait (S1-g / kılavuz). Ayrıntı § 12'de.

## 1. Sorun (ölçüldü, yeniden doğrulandı)

Kullanıcı girdisi `decimal.TryParse(…, InvariantCulture)` ile okunuyordu. Sonuç: Türkçe **"45.000" 45** okunuyordu, **"1,5" ise 15**. Satır içi düzenleme değeri de `ToString("0.00", Invariant)` ile "45000.00" basılıyordu. Kullanıcı ekranda noktalı yazımı görüp aynı biçimde yazıyordu.

**Tam liste (grep ile yeniden çıkarıldı; Web projesi, bin/obj hariç).** Spesifikasyondaki 11 yer doğrulandı. Aynı hata sınıfından **3 ek yer** daha bulundu:

| Yer | Durum |
|---|---|
| `Bilesenler/Fis/FisKalemTablosu.razor` AdetDegistir / IskontoDegistir / FiyatDegistir + gösterim (55/63/73) | bağlandı |
| `Pages/Pos.razor` OranKaydet + gösterim `Bicim.Makine(o.KomisyonOran)` | bağlandı |
| `Pages/Vezne.razor` `Sayi()` + ön doldurmalar (dTutar, kalem Adet/BirimFiyat/Iscilik, bacak Tutar, KalaniAta) | bağlandı |
| `Pages/SiparisDetay.razor` 822/825 + iç metin saklama (Makine → Girdi) | bağlandı |
| `Pages/Siparisler.razor` `Sayi()` + gram ön doldurma | bağlandı |
| `Pages/Vitrin.razor` yuvarlama çözümü + seçenek anahtarı | bağlandı |
| `Bilesenler/Form/FormAlani.cs` `Ondalik()` | **tek merkez oldu** |
| **EK** `Bilesenler/SatirIciDuzenle.razor` AralikHatasi (önce CurrentCulture, sonra Invariant deniyordu: "1.500" sessizce 1500 ya da 1,5 okunuyordu) | bağlandı |
| **EK** `Pages/Sayim.razor` SatirIciSay (`NumberStyles.Any` + Invariant: "1,5" = 15) + gösterim | bağlandı |
| **EK** `Pages/Faturalar.razor` KalemKaydet (`decimal.Parse(deger.Replace(',', '.'))`: "45.000" = 45, "1.250,50" = FormatException) + 4 gösterim | bağlandı |

**Kapsam dışı bırakılanlar (gerekçeli):**
- Tarih ayrıştırmaları: `Raporlar:1446`, `Panel:984`, `Evraklar:491` (tarih, sayı değil).
- Yapılandırma okuması: `CanliKurServisi:77` (int, yapılandırma).
- `Gider.razor:570-571`: **sunucunun hata metnini** ayrıştırıyor (makine metni, kullanıcı girdisi değil).
- `Urunler.razor:1085`: `Sayi` bileşeninden gelen decimal → Invariant ToString → Invariant parse zinciri. Makine içi gidiş-dönüş ve doğru.
- `Kurulum.razor:699 Sayi()`: çağıranı yok (ölü yardımcı). Özellik silinmez kuralı gereği dokunulmadı.
- `Banka:794`, `Faturalar:806/1048` KDV oranı: seçici anahtarları, makine biçimi.
- `Faturalar:1289 dToplam`: `type="number"` girdisi. Tarayıcı değeri Invariant verir, mevcut ayrıştırma doğru.

## 2. Yapılanlar

### 2.1 Tek merkez ayrıştırıcı — `FormAlani.SayiCoz` (Kuyumcu.Web/Bilesenler/Form/FormAlani.cs)

`SayiCozumu(SayiSonucu Sonuc, decimal? Deger)` döner. `Sonuc` şunlardan biridir: `Tamam · Bos · Gecersiz · Belirsiz`. `Mesaj(dil)` belirsiz/geçersiz için sözlük metnini verir. `Ondalik(ham)` artık `SayiCoz(ham).Deger` sarmalayıcısıdır; P-36b girdi ailesi (Sayi/Para/Gram) de otomatik bu kurallara geçti.

Kurallar:
1. Boşluk, NBSP ve dar NBSP temizlenir. Baştaki "-" korunur; negatif olup olamayacağına çağıran karar verir.
2. `^\d{1,3}(\.\d{3})+(,\d+)?$` binlik kalıbıdır: nokta binlik, virgül ondalık sayılır.
3. Yalnız virgül varsa ondalıktır ("1,5" = 1,5). Hiç ayırıcı yoksa tam sayıdır.
4. Tek nokta ve noktadan sonra 3 hane DEĞİLSE ("22.5", "45000.00") ondalık nokta kabul edilir (geri uyum). Tam kısım "0" ise ("0.500") de ondalıktır.
5. Tek nokta + tam 3 hane ("1.500") → **BELİRSİZ → null + özel mesaj** (fail-closed, sessiz tahmin yok).
6. Virgül + nokta binlik kalıbına uymuyorsa ("1,500.25") → geçersiz, null.

**Sapma / yorum (gerekçeli):** Spesifikasyon hem "'45.000' → 45000" hem "'1.500' → belirsiz" diyor. İkisi de "tek nokta + 3 hane" biçiminde. Ayırıcı kural olarak **tek grup "000" ise binlik** alındı. Gerekçe: eski makine gösterimleri (`0.##`, `0.###`, `Bicim.Makine`) sondaki sıfırları hiç yazmaz. Bu yüzden ".000" yalnız insan elinden, Türkçe binlik olarak gelir. "12.500" gibi sıfırsız gruplar gerçekten iki anlamlıdır (12,5 gram mı, 12.500 TL mi) ve reddedilir.

### 2.2 Metinler — `Kuyumcu.Core/I18n/Sozlukler.SayiGirisi.cs` (yeni, TR+EN)

Konumsal record kullanıldı; eksik anahtar derleme hatası verir (UX-S31). HUB `Sozlukler.cs`'e dokunulmadı.
- TR Belirsiz: "Ondalık için virgül kullanın: 1,5 ya da 1500"
- TR Geçersiz: "Sayı anlaşılmadı. Örnek: 1,5 · 45.000 · 1500"
- EN karşılıkları dolu.

### 2.3 Gösterim — `Bicim.Girdi(decimal, kalip = "0.##########")`

tr-TR, **gruplamasız** ve nokta içermez: 45000 → "45000,00", 1,5 → "1,5". Gidiş-dönüş birebirdir. `Bicim.Makine` DEĞİŞMEDİ; API, SVG ve sorgu dizesi için Invariant kalır. P-57 ile çelişki yok: `Bicim.cs`'e yalnız ekleme yapıldı.

`SayisalAlan.MakineDeger` (girdi kutusu değeri) artık `Bicim.Girdi`'dir. Gerekçe: tr-TR çözücü eski Makine yazımı "1.125"i belirsiz sayar ve kutu kendi değerini geri okuyamazdı (M78-5 bunu kanıtlıyor).

### 2.4 Ekranlar

- Her yerde belirsiz/geçersiz girdi değeri **değiştirmez** ve Türkçe sebep yazar:
  - Fiş, Fatura, SiparisDetay ve girdi ailesi: alan altında.
  - Vezne: `Metin Hata=` ile alan altında; ayrıca gönderim kapısı var.
  - Siparisler: kalem hata satırında (`#sip-f-kalem-hata`).
  - Pos ve Sayım: sayfa bandında (bu iki sayfanın mevcut hata kalıbı).
- Vezne FisAc / DuzenlemeKaydet / TahsilEt ve Siparisler Kaydet'e **gönderim kapısı** eklendi. Eskiden bozuk sayı sessizce 0/1 sayılıp gönderiliyordu.
- Siparisler gram/adet kutuları `type="number"` → `type="text" inputmode="decimal"` oldu (id'ler aynı). Gerekçe: number girdisi tarayıcıya göre virgülü ya reddediyor ya da noktalıya çeviriyordu; kullanıcının yazdığı ile çözülen ayrışıyordu. Girdi ailesiyle aynı tür.
- Mevcut DOM id'lerinin hiçbiri değişmedi. Yeni CSS, satır içi style ya da sabit renk yok.
- Sunucu tarafı DEĞİŞMEDİ (JSON sayı alır).

## 3. Migration

Gerekmedi (şema değişikliği yok).

## 4. Derleme

`dotnet build Kuyumcu.sln --nologo` → **0 Hata · 0 Uyarı** (son temiz derleme mutasyonlardan sonra).

İlk derlemede görülen 1 uyarı (`EtiketEkranTestleri.cs(145) xUnit1031`) bu paketin değil: dosya 03:07'de başka bir şerit tarafından yazılmış. Son derlemede görülmedi.

## 5. Testler

### Yeni testler — 55 vaka, hepsi yeşil (`FullyQualifiedName~P78`: 55/55)

- `Kuyumcu.Tests/P78SayiGirisiTestleri.cs`:
  - `FormAlaniSayiTestleri`: 16 kabul + 3 belirsiz + 6 geçersiz + 3 boş + sözlük.
  - `GidisDonusTestleri`: 8 vaka.
  - `FisKalemTablosuTurkceTestleri`:
    - `FisKalemTablosu_TurkceFiyatVeAdet`
    - `Belirsiz_fiyat_…`
    - `Gosterilen_fiyat_TR_gruplamasiz_…`
    - `Demo_senaryosu_adim3_adet_1_2_1_aynen_calisir`
  - `SatirIciDuzenleSayiTestleri`
  - `SayisalAlanGidisDonusTestleri`
  - `PosOranTurkceTestleri` (2)
- Sayfa testleri, gerçekçi sahte API taşıyan mevcut sınıflara `P78_` önekiyle eklendi:
  - `P12EkranTestleri` — Vezne (2)
  - `S3aSiparisDuzenlemeEkranTestleri` — SiparisDetay (2)
  - `SiparisEkranTestleri` — Siparisler (2)
  - `P17VitrinEkranTestleri` — Vitrin (1)
  - `FaturaEkranTestleri` — Faturalar (2)
  - `UX1C2EkranTestleri` — Sayım (1)

### Değişen mevcut testler — 7 doğrulama, yalnız beklenen YAZIM

Hepsi girdi kutusunun `value` özniteliğini Invariant yazımla bekliyordu ("12.5", "100.00", "6690.66", "12500.5", "7.5", "43.5", "0.52"). Yeni davranış tr-TR gruplamasız yazımdır ("12,5" …). Sayısal değer aynıdır; yalnız yazım değişti.

Bu değişiklik spesifikasyonun kapsam (3) maddesinin doğrudan sonucu ve doğru olan budur: eski yazımlar tr-TR çözücüde belirsizdir. Her değişikliğin yanına gerekçe yorumu yazıldı. Değişen yerler:
- `S3aEkranTestleri:97`
- `P12EkranTestleri:156`
- `B4AltinBacagiEkranTestleri:159`
- `P16EkranTestleri:222/294`
- `UX4bEkranTestleri:208`
- `UrunEkranTestleri:448`

Testin sınadığı davranış (alanın varlığı, değerin dolu gelmesi) korundu.

### Koruma altındaki testler yeşil

S21/S22/S25/S25b fiş testleri, UX1C1 satır içi adet, P-57 Bicim testleri, P39 K7 (`FormAlani.Ondalik("1.0")` = 1 → kural 4) ve ilgili 23 sınıf filtresi: **1292 testin 1290'ı** ilk koşumda geçti. Kalan 2 kırmızı, yukarıda gerekçelenen yazım testleriydi.

### Tam süit

| Koşum | Toplam | Geçen | Kırmızı |
|---|---|---|---|
| Son koşum (`trx-pP-78-son`, 07:07) | 5017 | 5014 | 3 |

Kırmızıların üçü de bu paketin değil (§ 12):
- `KilavuzUretimTestleri.Ekran_isaretinin_basligi_ekranin_MENU_ADINI_tasir`
- `MenuMimarisiTestleri.Basit_modda_BOS_grup_basligi_cizilmez`
- `S1gMenuTuvalTestleri.Sira_sifirla_YALNIZ_kayitli_sira_varken_cikar_ipucu_tasir_ve_DELETE_atar`

İlk tam koşumda (07:02, 5001 test) 24 kırmızı vardı. Bunların 5'i bu paketin yazım testleriydi ve düzeltildi. Kalan 19'u menü şeridinin o anki yarım işiydi; yeniden koşumda 17'si kendiliğinden yeşile döndü.

Başlangıç (devir notu, 06:39 demo kapısı): 4946/4946. Aradaki test sayısı artışı = bu paketin 55 testi + menü şeridinin yeni testleri.

## 6. Mutasyon kanıtları

Yöntem: yedek `scratchpad/mut-yedek/P-78/` altına alındı → `perl -0777 -pi` ile tek eşleşmeli değişiklik → `dotnet test --filter P78` → yedek geri kopyalandı → `cmp` ile birebir aynı olduğu kanıtlandı. Son mutasyondan sonra temiz derleme: 0 hata, 0 uyarı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) (seçme) | Geri alındı |
|---|---|---|---|
| M78-1 | Binlik kalıbı silindi (`^(?!)$`) | `Kabul_ornekleri…(ham: "45.000")`, `"1.250.000,50"`, `FisKalemTablosu_TurkceFiyatVeAdet`, `P78_vezne_duzenlemede_1_5_adet_ve_45_000_fiyat…`, `P78_fatura_kalem_fiyati_45_000…` (19 kırmızı) | cmp AYNI |
| M78-2 | Belirsizlik dalı "nokta = ondalık"a çevrildi | `Tek_nokta_ve_tam_uc_hane_BELIRSIZ…(ham: "1.500")`, `Belirsiz_fiyat_KALEMI_DEGISTIRMEZ…`, `Pos_belirsiz_oran…`, `P78_yeni_sipariste_belirsiz_gram…` (11 kırmızı) | cmp AYNI |
| M78-3 | FisKalemTablosu FiyatDegistir eski Invariant parse'a döndü | `FisKalemTablosu_TurkceFiyatVeAdet`, `Belirsiz_fiyat…`, `Gosterilen_fiyat…` | cmp AYNI |
| M78-3b | FisKalemTablosu AdetDegistir eski Invariant parse'a döndü | aynı 3 test | cmp AYNI |
| M78-4 | `Bicim.Girdi` Invariant'a döndü | `GidisDonusTestleri.Gosterilen_deger…` (7 vaka), `Gosterilen_fiyat…`, `Pos_komisyon_orani_TR…`, `P78_vezne_duzenlemede…` (11 kırmızı) | cmp AYNI |
| M78-5 | `SayisalAlan.MakineDeger` → `Bicim.Makine` | `Gram_kutusu_uc_haneli_degeri_TR_basar_ve_kendi_degerini_BELIRSIZ_saymaz` | cmp AYNI |
| M78-6 | FisKalemTablosu fiyat gösterimi Invariant'a döndü | `Gosterilen_fiyat_TR_gruplamasiz_ve_ayni_deger_geri_kaydedilince_DEGISMEZ` | cmp AYNI |
| M78-7 | Vezne `Sayi()` Invariant'a döndü | `P78_vezne_duzenlemede_1_5_adet_ve_45_000_fiyat_TURKCE_okunur` | cmp AYNI |
| M78-8 | Siparisler `Sayi()` Invariant'a döndü | `P78_yeni_sipariste_1_5_gram_ve_2_5_adet_TURKCE_okunur` | cmp AYNI |

Spesifikasyonun dört mutasyonunun dördü de (binlik kalıbı, belirsizlik dalı, FisKalemTablosu parse, gösterim) kırmızı üretti. Test açığı yok.

## 7. Denetim (`dotnet/denetim.sh`)

EXIT=1: **284 ✓**, 2 ✗. İki ✗ de bu paketin değil:

```
✗ P-14: MENU_AYNI_AD        1 Nav anahtarı sözlükte YOK — sayac gecersiz
✗ P-33: KILAVUZ_URETILMIS   0 — kilavuz.html kaynaktan üretilmemiş/bayat
```

Bu iki ihlal menü şeridinindir (§ 12). Bu paketin dokunduğu sayaçların hepsi ✓:

| Sayaç | Değer |
|---|---|
| GIRDI_AILESI_DISI_BICIM | 0 |
| GIRDI_AILESI_TAM | 15/15 |
| GIRDI_AILESI_TEK_KAYNAK | 1 |
| GIRDI_AILESI_KULLANIM | 366 |
| ORTAK_GIRDI_ISTISNA | 3 |
| SATIR_ICI_STYLE | 109 (tavan 143) |
| SABIT_RENK | 0 |
| ModalPencere | 48 (tavan 55) |
| SOZLUK_SAYI | 0 |

## 8. Canlı doğrulama

Port 5180, `kuyumcu_net`, kiracı `PAKETP-78-TEST Kuyumculuk` = `01M38SB78ERQZEFT6FJHJR6VFE`.

Başlangıç: `COUNT(*) tenants` = **10**, `QA-TEST-%` = **4**.

| # | Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|---|
| 1 | Tek kullanımlık davet (SHA-256 özeti, `kiraci_davetleri`) → `POST /api/v1/auth/kayit` | 201 | **201**, anahtarlar `accessToken, refreshToken, tenantId` |
| 2 | `GET /kur/guncel` | kur id | var |
| 3 | `POST /urunler` (ZIYNET K22 10 g) | 201 | **201** |
| 4 | `POST /kasalar` (şubesiz) → `POST /kasalar` (`subeId` ile) | 400 → 200 | **400** "Şube zorunludur" → **200** |
| 5 | `POST /cari` (TEDARIKCI) | 201 | **201** |
| 6 | `POST /islemler/alis` stok girişi (3 adet, NAKIT, `odenenTl:0`) | 200 | **200** |
| 7 | `POST /islemler/satis` NAKIT **45000** | tevsik kuralı reddeder | **400** "Tevsik zorunluluğu: … 45.000,00 TL, 30.000,00 TL haddini aşıyor" (beklenen iş kuralı; bu paketle ilgisiz) |
| 8 | `POST /islemler/satis` VERESİYE, `birimFiyatTl:45000`, `adet:1` (ekranda "45.000" yazılınca kalemin taşıdığı değer — bUnit `FisKalemTablosu_TurkceFiyatVeAdet`) | 200 | **200**, `tlTutari: 45000.0`, `adet: 1.0` |
| 9 | `GET /_framework/Kuyumcu.Web.wasm` | bu turun derlemesi | **200**, 3 076 889 bayt. SHA-256 (ilk 20 hane) bin'deki dosyayla aynı: `47a688672450c4c02e0f`. İkili dosyada `SayiCoz` / `SayiCozumu` / `Girdi` tanımlayıcıları var. |

SQL:

```
SELECT tur, tl_tutari, adet, odeme_turu, veresiye_tutar FROM hareketler
WHERE tenant_id='01M38SB78E…' AND id='01M38SF4ST…'
→ SATIS  45000.00  1.000  VERESIYE  45000.00
```

Ekran davranışı (yazılan metin → kalem değeri) bUnit ile kanıtlandı. Tarayıcıda oturum açılmadı, çünkü parola girişi ajanlara kapalı.

**Temizlik:**
- API durduruldu: PID 6213 ve dinleyen 6217 `kill` edildi. `lsof -nP -iTCP:5180 -sTCP:LISTEN` → **boş**.
- `tenant_id` sütunu olan **70 tablodan** kiracı satırları silindi, kalan satır **0**. `tenants` satırı ve davet satırı da silindi.
- Son sayımlar: `COUNT(*) tenants` = **10** (başlangıç 10) · `PAKETP-78%` = 0 · davet = 0 · `QA-TEST-%` = **4** (dokunulmadı).
- Scratchpad'deki jeton, parola ve token dosyaları `printf ''` ile boşaltıldı.

## 9. DoD sözleşmesi

Yeni varlık ya da kayıt türü **yok**; bu paket yalnız girdi ayrıştırma ve gösterim içeriyor. Düzenleme yolları mevcut uçlarla aynıdır:
- fiş kalemi: satır içi, ekran durumu
- vezne fişi: PATCH
- sipariş: POST/PATCH
- POS taksit oranı: PATCH
- pano ayarı: PATCH
- sayım satırı: POST say
- fatura kalemi: PATCH

Her düğme ya istek atar ya da belirsiz/geçersiz sayıda **istek atmadan sebebini yazar** (testlerle kanıtlandı: Vezne, Siparisler, Pos, Fatura, Sayım).

## 10. Dokunulan sayfalar (TEK DOKUNUŞ kaydı)

Pos · Vezne · SiparisDetay · Siparisler · Vitrin · Sayim · Faturalar.

Bileşenler: FisKalemTablosu · SatirIciDuzenle · Form/FormAlani.cs · Form/SayisalAlan.cs. Servis: Bicim.cs.

Gerekçe: aynı hata sınıfının tek dalgada kapatılması (P-78 kapsam 2, "listelenen TÜM kullanıcı girdisi yerleri").

## 11. Koşul karşılama (paket kapısı)

| # | Koşul | Durum | Kanıt |
|---|---|---|---|
| 1 | Build 0 hata | ✓ | 0 Hata · 0 Uyarı |
| 2 | Paketin testleri 0 başarısız | ✓ | P78 filtresi 55/55 |
| 3 | Paketin sayaçları ✓ | ✓ | § 7 |
| 4 | Mutasyon kanıtı | ✓ | § 6: 9 mutasyon, hepsi kırmızı, cmp AYNI |
| 5 | Canlı doğrulama | ✓ | § 8: SQL 45000.00 |
| 6 | Test kiracısı silindi | ✓ | tenants 10 → 10, kalan satır 0 |
| 7 | Yeni kırmızı test üretilmedi | ✓ | Tam süitteki 3 kırmızının hepsi menü şeridinin (§ 12). Bu paketin 7 yazım kırmızısı gerekçeyle düzeltildi. |

Kabul örnekleri (tamamı testte):

| Girdi | Sonuç | Kanıt |
|---|---|---|
| '45.000' | 45000 | ✓ |
| '1.250.000,50' | 1250000,50 | ✓ |
| '1,5' | 1,5 | ✓ |
| '22.5' | 22,5 | ✓ |
| '1500' | 1500 | ✓ |
| '1.500' | null + mesaj | ✓ |
| '1,500.25' | null | ✓ |
| '' | null (0 değil) | ✓ |
| '-3,5' | -3,5 (negatif kuralı çağıranda) | ✓ |
| Gidiş-dönüş | değer değişmez | ✓ |
| FisKalemTablosu | 45.000 → 45000, 1,5 → 1,5 | ✓ |
| Vezne, Siparisler, SiparisDetay, Pos, Vitrin (+ Sayım, Fatura) | '1,5' → 1,5 | ✓ |
| Demo adım 3 (adet 1 → 2 → 1) | çalışıyor | ✓ |

## 12. Açık kalanlar

- **Eş zamanlı şerit (sahibi: menü / S1-g sadeleştirme şeridi).** Bu koşum "tek başına" olarak açılmıştı, ama ağaçta aynı anda menü işi yürüdü:
  - Değişen dosyalar: `ArayuzModuServisi.cs` (06:55:18) ve `Layout/KenarCubugu.razor` (06:56:30).
  - Değişikliklerin bir kısmı henüz yarım: `ArayuzModu.cs`, `MenuSozlugu.cs`, `Sozlukler.Menu.cs`, `KilavuzSozlugu.cs`, `Kuyumcu.Core/Kilavuz/`, `Pages/Kilavuz.razor`, `wwwroot/kilavuz.html`, `docs/KULLANIM_KILAVUZU.md`, `docs/kilavuz-sablon.html`.
  - Bir mutasyon derlemesi bu yarım iş yüzünden bir kez kırıldı (`KenarCubugu.razor(231) MenudeGorunur`). Dosyalar oturunca yeniden koşuldu.
  - Bu şeride ait kırmızılar: tam süitte 3 test, denetimde 2 ihlal (P-14 MENU_AYNI_AD, P-33 KILAVUZ_URETILMIS).
  - Bu paket o dosyalara **dokunmadı**. Kurucunun "menüyü kontrol et" isteği o şeridin işidir.
- `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı: başka şeridin dosyası (03:07). Son derlemede görünmedi.
- Pos ve Sayım'da sayı hatası sayfa bandında, alan altında değil. Bu iki sayfa tek alanlı satır içi düzenlemede zaten sayfa bandı kalıbını kullanıyor; kalıp değişikliği ayrı bir UX paketi ister.
- `Faturalar.razor`'daki `catch (FormatException)` artık ulaşılmaz (ayrıştırma önceden yapılıyor). Özellik silinmez kuralı gereği bırakıldı.
- `Kurulum.razor:699 Sayi()` ölü yardımcı; silinmedi.

## 13. Kurucuya / mali müşavire not

Mevzuat hükmü yok; bu paket yalnız sayı yazımıyla ilgili. Canlı adım 7'deki tevsik reddi mevcut kuraldır ve P-78 ona dokunmadı.

## 14. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketin payı. `??` işareti, dosyanın ilk commit'ten beri izlenmediğini gösterir; commit yok.

```
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.SayiGirisi.cs            (yeni)
?? dotnet/Kuyumcu.Web/Bilesenler/Form/FormAlani.cs
?? dotnet/Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs
 M dotnet/Kuyumcu.Web/Servisler/Bicim.cs
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemTablosu.razor
?? dotnet/Kuyumcu.Web/Bilesenler/SatirIciDuzenle.razor
?? dotnet/Kuyumcu.Web/Pages/Pos.razor
?? dotnet/Kuyumcu.Web/Pages/Vezne.razor
?? dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor
?? dotnet/Kuyumcu.Web/Pages/Siparisler.razor
?? dotnet/Kuyumcu.Web/Pages/Vitrin.razor
?? dotnet/Kuyumcu.Web/Pages/Sayim.razor
 M dotnet/Kuyumcu.Web/Pages/Faturalar.razor
?? dotnet/Kuyumcu.Tests/P78SayiGirisiTestleri.cs               (yeni)
?? dotnet/Kuyumcu.Tests/P12EkranTestleri.cs
?? dotnet/Kuyumcu.Tests/S3aEkranTestleri.cs
?? dotnet/Kuyumcu.Tests/SiparisEkranTestleri.cs
?? dotnet/Kuyumcu.Tests/P17EkranTestleri.cs
?? dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs
?? dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs
?? dotnet/Kuyumcu.Tests/B4AltinBacagiEkranTestleri.cs
?? dotnet/Kuyumcu.Tests/P16EkranTestleri.cs
?? dotnet/Kuyumcu.Tests/UX4bEkranTestleri.cs
?? dotnet/Kuyumcu.Tests/UrunEkranTestleri.cs
?? dokumanlar/09-uygulama-kayitlari/PAKET-P-78-uygulama-raporu.md
```
