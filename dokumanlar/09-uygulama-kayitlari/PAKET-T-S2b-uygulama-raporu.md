# PAKET T-S2b — Satış PRO tuvali, kalan bulgu: kampanya indirimli üründe işçilik · uygulama raporu (25.09.2026)

Rol: Taşkın (backend; sunucu değişmedi) + Ertuğ (frontend). Dalga: SATIŞ SON (L-1 + T-S2b).
Durum: **BİTTİ**. Paket kapısının yedi maddesi sağlandı:
- derleme 0 hata;
- T-S2 filtresinde 108/108 test geçti, satış çevresinde 693/693 test geçti;
- `denetim.sh` sonucu DENETİM TEMİZ;
- 6/6 mutasyon kırmızıya döndü ve hepsinde cmp AYNI çıktı;
- canlıda gerçek Chrome'da ölçüldü;
- test kiracısı silindi, tenants sayısı 12'den 12'ye döndü;
- tam süitte 5914/5914 test geçti, yani yeni kırmızı yok.

## 0. Bulgu ve kök neden
T-S2'nin 3. turunda doğrulayıcı şu bulguyu bıraktı: kampanya indirimli üründe PRO kalem tablosunun **İşçilik** hücresi indirim ÖNCESİ değeri gösteriyordu.

Kök neden `FiyatMotoru.Teklif` içinde. Motor `ListeFiyati = HasDegeri + MarjTl + IscilikTl` hesaplıyor. Ardından kampanya indirimini **fiyatın bütününden** düşüyor: `SatisFiyati = Liste − İndirim`. İndirim parçalara dağıtılmıyor. Yanıttaki `iscilikTl` bu yüzden hâlâ indirim öncesi liste işçiliği. Kalem ise indirim sonrası `SatisFiyati` ile ekleniyordu. `FisProKalemKarti.TeklifIscilik` yalnız `SatisFiyati == birimFiyat` koşuluna baktığı için bu kalem, indirim öncesi işçiliği satırın işçiliği diye basıyordu. Bu uydurma bir finansal rakamdı (UX-S21).

Aynı sınıftan iki kardeş kusur daha bulundu ve kapatıldı (doğrulayıcı henüz yakalamamıştı):
1. **Satır iskontosu.** Tutar iskontodan sonraki rakamı gösteriyordu, işçilik ise iskontodan önceki rakamı.
2. **Fiş iskontosu.** Genel toplam indirimli rakamı gösteriyordu, alt şeritteki "İşçilik" toplamı ise indirimsiz rakamı.

Ayrıca "bilinmeyen değer '—'" koşulu için iki düzeltme yapıldı:
- Milyem kayıtlı değilse (0) hücre artık "0" yerine "—" yazıyor.
- Kalemin ürün kartı listede yoksa gram ve has hücreleri ile "Toplam has" "—" yazıyor. Eskiden 0 sayılıyordu.

## 1. Yapılanlar
| Dosya | Değişiklik |
|---|---|
| `Kuyumcu.Web/Bilesenler/Fis/FisProKalemKarti.razor` | `TeklifIscilik` artık `IndirimTutari: null or 0m` şartını da arıyor: indirimli teklifte değer `null` olur. Yeni parametre `FisIskontoYuzde` eklendi. Şerit, işçilik toplamını fiş iskontosunu hesaba katarak alıyor. "Toplam has" `ToplamHasBilinen` üzerinden hesaplanıyor; bilinmiyorsa "—" yazıyor. |
| `Kuyumcu.Web/Bilesenler/Fis/FisModelleri.cs` | `SatirIscilik`: satır iskontosu > 0 ise `null` döner (hediye satırı yine 0). `ToplamIscilik(kalemler, fisIskontoYuzde)`: fiş iskontosu varsa ve hediye olmayan satır varsa `(0, Eksik)` döner, ekranda "—" görünür. Yeni `ToplamHasBilinen` eklendi. |
| `Kuyumcu.Web/Bilesenler/Fis/FisKalemTablosu.razor` | PRO satırındaki Milyem, Gram ve Has hücrelerine yeni id'ler verildi: `islem-kalem-milyem-{ix}`, `-gram-`, `-has-`. Bilinmiyorsa "—" yazılır. Mevcut id'lere dokunulmadı. |
| `Kuyumcu.Web/Pages/Islemler.razor` | Tek öznitelik eklendi: `FisIskontoYuzde="fisIskontoYuzde"`. L-1'in satış fişi yolu (`Kaydet`/POST) değişmedi. |
| `Kuyumcu.Core/I18n/Sozlukler.SatisPro.cs` | `IscilikBilinmiyor` metni TR ve EN olarak genişletildi. Artık üçüncü sebebi de söylüyor: "indirim (kampanya/iskonto) fiyatın bütününe uygulandı; işçilik payı ayrıştırılmaz". |
| `Kuyumcu.Tests/TS2TuvalProTestleri.cs` | Sahte API'ye iki bayrak eklendi: `teklifKampanya` (liste 10000, işçilik 500, indirim 1000, satış 9000) ve `urunMilyem`. `KalemliPro(beklenenFiyat)` artık beklenen fiyatı parametre olarak alıyor. 5 yeni test yazıldı. |

**Sunucu değişmedi. Migration yok. Yeni uç yok.** Tutar ve genel toplam, sunucunun indirim sonrası `satisFiyati` değerinden geliyor; bunlar zaten doğruydu. "İndirim sonrası işçilik payı" gibi yeni bir sayı istemcide **üretilmedi**. Yüzdeyle bölüştürme yapmak da bir uydurma olurdu. ISCILIK_YUZDE kampanyasında `işçilik − indirim` hesabı istemcide yapılabilirdi, ama bu da yasaklanan istemci hesabı olurdu. Sunucu bu kırılımı döndürmediği sürece hücre "—" yazar.

## 2. Sapmalar ve gerekçeleri
- **Kardeş kusurlar (satır ve fiş iskontosu) kapsama alındı.** Devir notu "tüm PRO kalem tablosu sütunlarını sunucunun hesapladığı değerlerden göster; bilinmeyen '—'" diyor. İki kusur da aynı dosya alanında (Fis/*).
- **Canlı doğrulama portu.** Görev metninin genel kuralı 5198 diyor; paketin izolasyon maddesi ise 5162 ve 5163'ü veriyor. Özel madde uygulandı: API 5162'de, vekil 5163'te koştu. Test veritabanı `KUYUMCU_TEST_DB=kuyumcu_test_t2`.
- **Test kiracısının adı `PAKETT-S2b-TEST`.** Önek kuralı `PAKET<no>-TEST` ve paket numarası `T-S2b`.

## 3. DoD sözleşmesi
Yeni varlık ya da kayıt türü yok. Değişen her şey salt görünüm. İşçilik, milyem, gram ve has hücreleri **türetilmiş, salt okunur** değerler; düzenleme yolu yok, çünkü bunlar kayıt değil. Kalemin düzenlenebilir alanları (adet, birim fiyat, satır iskontosu, hediye, sil) T-S2'de olduğu gibi ✎ satırında kaldı. "—" hücresinin `title`'ı nedeni söylüyor (UX-S30).

## 4. Testler
Önce: T-S2 filtresinde 103 test vardı (TS2TuvalPro + TS2SinirTestleri + Tur2). Sonra 108/108 geçiyor. Doğrulayıcının testi `TS2SinirTestleriTur2.Kampanya_indirimli_teklifte_iscilik_indirim_oncesi_500_YAZILMAZ` artık yeşil.

Yeni testler (5):
- `TS2b_indirimli_teklifte_ve_iskontolu_satirda_iscilik_BILINMIYOR_null` (saf test)
- `TS2b_kampanyali_kalemde_iscilik_hucre_ve_serit_TIRE_indirim_oncesi_500_BASILMAZ` (bUnit)
- `TS2b_satir_iskontosu_girilince_iscilik_TIRE_kaldirilinca_geri_gelir` (bUnit)
- `TS2b_fis_iskontosu_girilince_serit_iscilik_TIRE_silinince_geri_gelir` (bUnit)
- `TS2b_milyem_kayitli_degilse_hucre_TIRE_0_uydurulmaz` (bUnit)

Satış çevresi filtresi (`Islem|S2|TS1|L1|Fis|Satis`) 693/693 geçti.

Tam süit: `trx-pT-S2b/_Mac_2026-09-25_05_53_47.trx`, **5914/5914** geçti, 0 başarısız. Derleme 0 hata. Tek uyarı xUnit1031 ve `EtiketEkranTestleri.cs:145` içinde; bu paketin dosyası değil. Test derlemesi 0 uyarı verdi.

## 5. Mutasyon kanıtları
Yöntem: yedek `scratchpad/mut-yedek/T-S2b/` altına alındı, `perl -0777 -pi` ile değişiklik yapıldı, `dotnet test --filter TS2` koşuldu, yedek geri kopyalandı ve `cmp` ile karşılaştırıldı.

| Kod | Değişiklik | Kırmızıya dönen testler | cmp |
|---|---|---|---|
| M-TS2b-1 | `TeklifIscilik` içinden `IndirimTutari: null or 0m` kaldırıldı | `TS2SinirTestleriTur2.Kampanya_indirimli_teklifte_iscilik_indirim_oncesi_500_YAZILMAZ`, `TS2b_indirimli_…`, `TS2b_kampanyali_…` | AYNI |
| M-TS2b-2 | `SatirIscilik` içinden satır iskontosu şartı kaldırıldı | `TS2b_indirimli_…`, `TS2b_satir_iskontosu_…` | AYNI |
| M-TS2b-3 | Fiş iskontosu şartı `> 0m` yerine `> 1000m` yapıldı | `TS2b_fis_iskontosu_…`, `TS2b_indirimli_…` | AYNI |
| M-TS2b-4 | `Islemler.razor` içinde `FisIskontoYuzde` parametresi kaldırıldı | `TS2b_fis_iskontosu_…` | AYNI |
| M-TS2b-5 | `ProMilyem` içinden `Milyem > 0` şartı kaldırıldı | `TS2b_milyem_kayitli_degilse_…` | AYNI |
| M-TS2b-6 | `ToplamHasBilinen` şartı `true` yapıldı | `TS2b_indirimli_…` | AYNI |

Son mutasyondan sonra temiz derleme yapıldı: 0 hata.

## 6. Denetim
`dotnet/denetim.sh` rc=0 döndü, **DENETİM TEMİZ** (✗ sayısı 0).

## 7. Canlı doğrulama (API 5162, vekil 5163, kiracı `PAKETT-S2b-TEST`)
Kurulum API üzerinden yapıldı:

| Adım | Sonuç |
|---|---|
| `/auth/kayit` | 201 |
| `/auth/giris` | 200 |
| Kasa | 200 |
| Liste Vitrin (%5 marj, çarpan %100) | 200 |
| 3 ürün | 201 ×3 |
| Cari Hasan Kaya | 201 |
| `POST /fiyat/kampanyalar` (URUN = ALY-00098, TUTAR_YUZDE %10, bugün) | 200 |

`GET /fiyat/teklif` yanıtları okundu:

| Ürün | Sunucu yanıtı |
|---|---|
| ALY-00098 (kampanyalı) | `listeFiyati 26470.8, hasDegeri 24067.43, marjTl 1203.37, iscilikTl 1200 (İNDİRİM ÖNCESİ), indirimTutari 2647.08, satisFiyati 23823.72, kampanyaId dolu, sabitFiyat false` |
| BLZ-00412 | `sabitFiyat true, iscilikTl 0` (ayrıştırılmadı) |
| KLY-7 | `iscilikTl 70, indirim 0, satisFiyati 76.49` |

Gerçek Chrome'da (başsız, CDP; `ts2b.mjs` + `sikisma-olc.js`) üç kalemli fiş ve Hasan Kaya ile, koşumlar arasında 40 sn bekleyerek ölçüldü. Tüm koşumlarda HTTP 4xx sayısı 0:

| Koşum | Mod | Tema | K1 | K3 taşan/kırık | K5 TRY | K7 | K8 | K9 | K10 | K11 | Yatay | İşçilik hücreleri (Alyans · Burma · Kolye) | Şerit |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440×900 | PRO | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — · — · 70 ₺ | 70 ₺ + |
| 1440×900 | PRO | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — · — · 70 ₺ | 70 ₺ + |
| 1280×800 | PRO | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — · — · 70 ₺ | 70 ₺ + |
| 1280×800 | PRO | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — · — · 70 ₺ | 70 ₺ + |
| 375×812 | PRO | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — · — · 70 ₺ | 70 ₺ + |
| 375×812 | PRO | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | — · — · 70 ₺ | 70 ₺ + |
| 1280×800 | BASİT | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | (basit tablo; PRO sütunu yok) | — |
| 1280×800 | BASİT | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | aynı | — |
| 375×812 | BASİT | açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | aynı | — |
| 375×812 | BASİT | koyu | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | aynı | — |

- PRO koşumlarında K2 (kart sağ yüzde) 97–98 çıktı. Dar sütun yok.
- 1280 koyu koşumundaki satırlar:
  - `ALY-00098 | Alyans | 14 | 585 | 6,10 | 3,5685 | 1 | — | 23.836,06 ₺`
  - `BLZ-00412 | Burma bilezik | 22 | 916 | 4,30 | 3,9388 | 1 | — | 28.750 ₺`
  - `KLY-7 | Kolye ince zincir | 22 | 916 | 0,001 | 0,0009 | 1 | 70 ₺ | 76,49 ₺`
- Alyans'ın tutarı koşumdan koşuma birkaç kuruş değişti (23.823,72 → 23.837,85). Sebep canlı has kurunun güncellenmesi. Tutar her koşumda sunucunun o anki `satisFiyati` değeri.
- Önceki davranışta Alyans hücresi **1.200 ₺** (indirim öncesi) yazıyordu. Şimdi **"—"** yazıyor ve title nedeni söylüyor.
- "Toplam has" 7,5082 g: 3,5685 + 3,9388 + 0,0009, dört hane korundu.
- Kesik hücre ölçümü yalnız "Eylemler" başlığını raporladı. Bu, görsel olarak gizli (`gorunmez`) erişilebilirlik metni; K3 = 0.

**Görsel karşılaştırma** (`ham/TUVAL-KARSILASTIRMA/`):
- `T-S2b-Profesyonel-1440.png`: tuval ile yan yana.
- `T-S2b-Profesyonel-1280-koyu.png`
- `T-S2b-Profesyonel-375-tam.png`

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Kalem tablosu kolonları | Barkod · Ürün · Milyem · Gram · Has · Adet · İşçilik · Tutar | Aynıları + Ayar + ✎ | GEREKÇELİ FARK (T-S2'de gerekçelendi, değişmedi) |
| İşçilik hücresi | Örnek veri "1.600" / "2.400" | Kampanyalı ve sabit fiyatlı üründe "—", bilinen üründe sunucu değeri | GEREKÇELİ FARK: tuvaldeki örnek veri, veri değil; bilinmeyen değer uydurulmaz (UX-S21) |
| Alt şerit İşçilik | "4.000 ₺" | "70 ₺ +" (yalnız bilinen satırlar; title nedeni söyler) | GEREKÇELİ FARK (aynı gerekçe) |
| KDV | "860 ₺" | "Faturada" | GEREKÇELİ FARK (MEVZUAT KURALI; T-S2 kararı korundu) |
| Diğer bölgeler | — | T-S2 3. turdaki gibi | AYNI (bu paket dokunmadı) |

Test kiracısı temizlendi:
- API (PID 24510, alt süreç) ve vekil durduruldu; 5162 ve 5163 LISTEN boş.
- `tenant_id` kolonu olan bütün tablolardan kiracının satırları silindi; kalan satır toplamı 0.
- `tenants` kaydı ve davet kaydı silindi.
- **tenants 12 → 12**. `QA-TEST-` önekli kiracılara dokunulmadı.

## 8. Dokunulan sayfalar
Yalnız `Pages/Islemler.razor`: tek öznitelik eklendi. T-SATIS şeridinin kendi sayfası; gerekçesi bu paketin konusu olan T-S2 kalan bulgusu. Bileşenler `Bilesenler/Fis/*` altında. `app.css`, `Bicim.cs` ve Sözlükler hub'ına dokunulmadı.

## 9. Değişen dosyalar
Bu paketin dosyaları:
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisProKalemKarti.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisModelleri.cs`
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemTablosu.razor`
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SatisPro.cs`
- `dotnet/Kuyumcu.Tests/TS2TuvalProTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-T-S2b-uygulama-raporu.md`
- `ham/TUVAL-KARSILASTIRMA/T-S2b-*.png` (3 dosya)

`git status` ağaç genelinde commit'siz çalışma olduğunu gösteriyor. Fis/, SatisPro ve tuval-satis dosyaları zaten izlenmeyen (`??`) durumda; Islemler.razor `M` durumunda.

## 10. Koşul karşılama
| Koşul | Durum |
|---|---|
| Kampanyalı üründe işçilik indirim öncesi yazılmaz | KARŞILANDI: hücre ve şerit "—"; canlıda Alyans "—" |
| Tüm PRO sütunları sunucu ya da kart değerinden, bilinmeyen "—" | KARŞILANDI: milyem 0 ve kartı olmayan ürün "—"; satır ve fiş iskontosunda işçilik "—" |
| T-S2 önceki tur düzeltmeleri korunur | KARŞILANDI: TS2 ve TS2Sinir testlerinin tamamı yeşil; has 4 hane, sabit fiyatta "—" |
| L-1 çok kalemli satış fişi yolu bozulmaz | KARŞILANDI: L1 ve Satis filtreleri yeşil; Kaydet/POST değişmedi |
| Gerçek tarayıcı ölçümü (1280/375 · BASİT/PRO · açık/koyu) ve 1440 karşılaştırma | KARŞILANDI: 10 koşumda K = 0, yatay 0 |
| Mevzuat: KDV istemcide hesaplanmaz | KARŞILANDI: değişmedi, "Faturada" |
| COMMIT yok | KARŞILANDI |

## 11. Açık kalanlar ve notlar
- **Sunucu tarafı öneri (Taşkın, ayrı paket):** `/fiyat/teklif` yanıtına indirim sonrası bileşen kırılımı eklenebilir, örneğin `iscilikIndirimSonrasiTl`. Özellikle ISCILIK_YUZDE kampanyasında kırılım belirli. Eklenirse PRO hücresi "—" yerine bu değeri gösterebilir. Bu paketin dosya alanı dışında olduğu için yapılmadı.
- Tam süitte başkasına ait kırmızı yok (5914/5914).
- Kurucuya ya da mali müşavire yeni mevzuat notu yok. KDV 23/e kararı T-S2'deki gibi sunucuda kalıyor.
