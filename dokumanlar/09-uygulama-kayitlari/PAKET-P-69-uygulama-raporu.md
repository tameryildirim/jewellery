# PAKET P-69 — `toplamHas` adlandırması: "toplam" değil "altın" (Uygulama Raporu)

**Şerit:** D (etiket + vitrin + rapor adlandırma) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Tarih:** 2026-09-24
**Durum:** BİTTİ — paket kapısının 7 maddesi kanıtlı (aşağıda). Commit YOK.

## 1. Spesifikasyon ↔ kod doğrulaması (md.1)

| İddia (TRIYAJ-TUR8 § P-69) | Kodda gerçek durum | Sonuç |
|---|---|---|
| `/raporlar/aylik` `satis.toplamHas` yalnız ALTIN | `RaporModulu.AylikOzet` → `HareketToplami.AltinHas(satislar, mutlak:true)` | DOĞRU |
| Adı "toplam has" diyor | Alan adı `toplamHas`; ekranda `Dil.T.Urun.ToplamHas` = "Toplam Has" | DOĞRU |
| Ekranda "Toplam Has" etiketi | Raporlar.razor'da **3 yerde** (maden dağılımı başlığı, stok değeri başlığı, reyon stok başlığı) + maden uyarısı metni "Toplam has YALNIZ altındır" / sunucunun ham `` `toplamHas` `` metni | DOĞRU (spesifikasyondan geniş) |
| Aylık altın has'ı ekranda kartta | Kart `NetHasHareketi` ("Net has gram hareketi") — yalnız altın ama etiket bunu söylemiyor | SAPMA: kartın adı "Toplam Has" değil; yine de aynı adlandırma hatası → kart da düzeltildi |
| Dosya ayak izi | 5 dosyanın hepsi `test -e` ile VAR | DOĞRU |

**Sapmalar ve gerekçeleri:**
1. **Sozlukler.cs HUB'a dokunulmadı** (orkestratör koşulu): yeni metinler `Kuyumcu.Core/I18n/Sozlukler.RaporHasAdi.cs` (RaporGrafikSozlugu deseni, konumsal record → eksik anahtar = derleme hatası, TR + EN).
2. **Testler `RaporSetiTestleri.cs` yerine yeni `P69HasAdlandirmaTestleri.cs`**: RaporSetiTestleri'ne aynı dalgada P-48 dokunuyor (çakışma haritası); iki şerit aynı test dosyasını açmasın.
3. **Günlük özet (`/raporlar/gunluk`) de `altinToplamHas` taşır** (satis/alis/hurda): aynı alan adı aynı kavram için iki uçta farklı olmasın (UX-S31). `toplamHas` orada da korunur.
4. **Ekran maden uyarısı sözlükten basılır**: sunucunun `madenUyarisi` metni ham alan adı (`` `toplamHas` ``) taşır ve yalnız Türkçedir → UX-S31 ihlali. Sunucu alanı API tüketicileri için DEĞİŞMEDİ (mevcut `MadenDefteriTestleri.DonemRaporuMadenleriToplamaz` yeşil); ekran aynı anlamı `RaporHasSozlugu.MadenUyarisi` ile iki dilde basar.
5. **"Has (altın)" etiketi koşulludur**: kart etiketi dönemde ALTIN dışı maden hareket gördüyse "Net has gram hareketi — Has (altın)" olur; yalnız altın varken eski etiket korunur (mevcut `UX1C3EkranTestleri` "Net has gram hareketi" araması iki durumda da yeşil). Tablo başlıkları (maden/stok/reyon) koşulsuz yeni anahtardadır — üçü de her durumda doğru ve "toplam" demez.
6. **Bugün yüzü (S3-4) değiştirilmedi**: `bugun-satis-has` "g has" yazar, veri `PanelSz.GunlukOzet` (PanelSozlesmeleri.cs — şerit sınırı dışı) ve `Sozlukler.RaporBugun.cs` (S3-4'ün dosyası). Açık kalanlara yazıldı.

Mevzuat hükmü içermez (adlandırma paketi) — web araştırması gerekmedi.

## 2. Yapılanlar

- **RaporModulu.cs** — `GunlukOzet`: satis/alis/hurda düğümlerine `altinToplamHas`; `AylikOzet`: satis/alis düğümlerine `altinToplamHas`. `toplamHas` KALDIRILMADI; yorumlarda "YALNIZ ALTIN (eski ad)" yazar.
- **RaporlarSozlesmeleri.cs** — `IslemToplam`'a `decimal? AltinToplamHas = null` (varsayılanlı, eski sunucuyla kırılmaz) + `AltinHas => AltinToplamHas ?? ToplamHas`.
- **Sozlukler.RaporHasAdi.cs (YENİ)** — `RaporHasMetinleri` (HasAltin, NetHasAltin, MadenHasBaslik, StokHasBaslik, ReyonHasBaslik, MadenUyarisi) TR + EN.
- **Raporlar.razor** — net has kartı `NetHasEtiketi` (altın dışı maden varsa "— Has (altın)"); üç tablo başlığı yeni anahtar + yeni id'ler `rapor-maden-has-baslik`, `rapor-stok-has-baslik`, `reyon-stok-has-baslik` (mevcut id değişmedi); maden uyarısı sözlükten; NetHas yedek formülü `AltinHas` okur. S3-4 "Bugün" yüzüne dokunulmadı; satır içi style / sabit renk eklenmedi.

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Raporlar.razor` (bu dalgada P-47 de dokundu; P-69 spesifikasyonu "P-47'den SONRA" diyor — gerekçeli ikinci dokunuş, P-47 kolonları/testleri bozulmadı: `RaporlarEkranTestleri` tam süitte yeşil).

**DoD sözleşmesi:** yeni varlık/kayıt türü YOK, yeni uç YOK, yeni form/düğme YOK — rapor okuma yanıtına salt-okunur, türetilmiş bir alan eklendi (hesaplanan değer; düzenlenecek kayıt değil). Migration gerekmedi.

## 3. Derleme
`dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata** (son temiz derleme, mutasyonlardan sonra).
Not: ilk iki derleme denemesi paralel şeritlerin eşzamanlı derlemesi nedeniyle `CS0006 Meta veri dosyası bulunamadı` / `MSB3030 Kuyumcu.Web.dll kopyalanamadı` ile düştü; kod hatası değil, yeniden koşumda geçti. Bir ara derlemede görülen 2 uyarı (`EtiketEkranTestleri.cs:145 xUnit1031`, `S25bSaticiYazdirTestleri.cs:439 xUnit2031`) başka şeritlerin dosyalarıdır; son derlemede yoklar.

## 4. Testler (KUYUMCU_TEST_DB=kuyumcu_test_m)

| Test | Tür | Sonuç |
|---|---|---|
| `P69_01_Aylik_yanit_altinToplamHas_alanini_tasir` | DB (servis) — altın K22 + gümüş G999 satışı | Geçti |
| `P69_02_toplamHas_alani_KALDIRILMADI_sekil_regresyonu` | DB — aylık satis/alis + günlük satis/alis/hurda | Geçti |
| `P69_03_Ekran_altin_disi_maden_varken_Toplam_Has_yazmaz` | bUnit — gerçekçi sahte API (aylık, stok, reyon) | Geçti |
| `P69_04_Sozluk_TR_EN_madeni_soyler` | saf | Geçti |

Filtreli koşum: **4/4 geçti**. Tam süit: **4867 / 4867 geçti, 0 başarısız** (trx: `scratchpad/trx-pP-69`). Yeni test sayısı: 4.

## 5. Mutasyon kanıtları (yedek → perl tek eşleşme → test → geri kopya → cmp)

| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M69-1 | Aylık `satis.altinToplamHas` yanıttan çıkarıldı | P69_01, P69_02 | birebir |
| M69-2 | Aylık `satis.toplamHas` yanıttan silindi | P69_02 | birebir |
| M69-3 | Maden tablosu başlığı eski `Dil.T.Urun.ToplamHas`'a geri alındı | P69_03 ("Toplam Has" bulundu, poz. 3543) | birebir |
| M69-4 | Kart etiketi koşulu `false` (eski etiket) | P69_03 | birebir |
| M69-5 | Maden uyarısı sunucu ham metnine geri alındı | P69_03 (`toplamHas` bulundu) | birebir |

⚠ M69-3'ün İLK koşumu yeşil çıktı (4/4). İnceleme: aynı anda koşan başka şeritlerin derlemesi `Kuyumcu.Web.dll`'i mutasyonsuz ağaçtan yeniden üretmişti (derleme yarışı). Aynı mutasyon iki kez yeniden koşuldu (M69-3 ve M69-3b) → iki koşumda da P69_03 KIRMIZI, hata iletisi `r-maden-has-baslik">Toplam Has</th>`. Test açığı değil; yarış kanıtıyla raporlanıyor.

## 6. Denetim
`dotnet/denetim.sh` → **DENETİM TEMİZ** (rc=0; 286 ✓). İlgili sayaçlar: `SABIT_RENK 0`, `SATIR_ICI_STYLE 110 (tavan 143)`, `SOZLUK_SAYI 0`, `BENZERSIZLIK_SOZLUKSUZ 0`.

## 7. Canlı doğrulama (localhost:5185 · kuyumcu_net)

Test kiracısı `PAKETP-69-TEST Kuyumculuk` (`01M38FTB1R05EPZTQK3SVKX0PS`) — davet satırı SQL ile eklenip `POST /auth/kayit` ile açıldı. Parola/jeton ekrana basılmadı.

| # | İstek | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` | 201 | **201** |
| 2 | `POST /urunler` K22 / G925 | 201 | **201** (`maden=ALTIN hasGrami=9.16` · `maden=GUMUS hasGrami=9.25`) |
| 3 | `POST /islemler/alis` (ilk deneme, kasa boş) | 400 kapı | **400** "kasa bakiyesi yetersiz" (P-44 kapısı; beklenen) |
| 4 | `POST /kasalar/{id}/hareket` GIRIS 10.000 | 200 | **200** |
| 5 | `POST /islemler/alis` ×2 (3 adet) | 200 | **200 / 200** |
| 6 | `POST /islemler/satis` ×2 (2 adet) | 200 | **200 / 200** |
| 7 | `GET /raporlar/aylik` | 200 + `altinToplamHas` | **200** — `satis: toplamHas=18.32 altinToplamHas=18.32 madenDagilimi=[GUMUS 18.5, ALTIN 18.32]`; `alis: 27.48 / 27.48 [GUMUS 27.75, ALTIN 27.48]` |
| 8 | `GET /raporlar/gunluk` | 200 + `altinToplamHas` | **200** — satis 18.32/18.32, alis 27.48/27.48, hurda 0/0 |
| 9 | Koşul: `satis.altinToplamHas == madenDagilimi[ALTIN]` | True | **True** |
| 10 | Koşul: `toplamHas` alanı duruyor | True | **True** |

SQL kanıtı: `select maden,tur,count(*),round(sum(abs(has_grami)),4) from hareketler where tenant_id=… group by maden,tur` → `ALTIN ALIS 1 27.4800 · ALTIN SATIS 1 18.3200 · GUMUS ALIS 1 27.7500 · GUMUS SATIS 1 18.5000`. 18,32 (altın) + 18,50 (gümüş) hiçbir alanda toplanmadı.

Ekran (tarayıcıda) canlı gezilmedi: test kiracısına tarayıcıda parolayla girmek güvenlik kuralıyla yasak; ekran kabulü bUnit P69_03 ile gerçek sunucu şeklinde (canlıda ölçülen alan adları) sınandı.

**Temizlik:** API (PID) `kill` ile durduruldu, `lsof :5185` boş; 5199'a dokunulmadı (dinleyici ayakta). Kiracı `tenant_id` kolonu olan tüm tablolardan + davet + tenants satırı silindi; kalan satır 0. **tenants: 10 → 10**. `QA-TEST-%` kiracıları (4) dokunulmadı.

## 8. Değişen dosyalar
```
 M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor
?? dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs   (dosya ağaçta zaten izlenmiyordu; P-69 IslemToplam'a alan ekledi)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.RaporHasAdi.cs          (YENİ)
?? dotnet/Kuyumcu.Tests/P69HasAdlandirmaTestleri.cs           (YENİ)
dokumanlar/09-uygulama-kayitlari/PAKET-P-69-uygulama-raporu.md (bu rapor)
```

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| `/raporlar/aylik` yanıtında `satis.altinToplamHas` var ve `madenDagilimi` ALTIN satırına eşit | ✅ | P69_01; canlı #7, #9 |
| Ekranda altın dışı maden varken "Toplam Has" görünmez, madeni söyleyen etiket görünür | ✅ | P69_03; M69-3/4/5 kırmızı |
| Eski `toplamHas` alanı duruyor (şekil regresyonu yok) | ✅ | P69_02; canlı #10; `MadenDefteriTestleri`, `P25OlcekTestleri` yeşil |
| `IslemToplam` istemci sözleşmesine `AltinToplamHas` (varsayılanlı) | ✅ | RaporlarSozlesmeleri.cs; mevcut sahte API'ler (alan yok) tam süitte yeşil |
| Sozlukler.cs HUB'a dokunulmadı, TR+EN ayrı dosyada | ✅ | Sozlukler.RaporHasAdi.cs |
| S3-4 Bugün / S34RaporBugunTestleri bozulmadı | ✅ | tam süit 4867/4867 |
| dotnet test 0 başarısız | ✅ | 4867/4867 |
| denetim.sh TEMİZ | ✅ | rc=0 |
| Mutasyon listesi (3 madde) + 2 ek | ✅ | § 5 |
| Test kiracısı silindi, tenants başlangıç değerinde | ✅ | 10 → 10 |

## 10. Açık kalanlar
- **Bugün yüzü "g has"** (`Raporlar.razor` S3-4 bölümü, `Sozlukler.RaporBugun.cs` `HasBirim`): değer yalnız altın, TR metin maden söylemiyor (EN "g fine gold" söylüyor). Veri `PanelSozlesmeleri.GunlukOzet` (şerit sınırı dışı). Sahibi: S3-4 / panel şeridi — ayrı küçük paket önerilir.
- **Sunucu `madenUyarisi` metni** (`RaporOrtak.MadenUyarisi`, şerit sınırı dışı) ham alan adı `` `toplamHas` `` içerir; API tüketicisi için kalabilir, ancak `altinToplamHas`'ı da anması önerilir.
- Diğer ekranlar (`KarZarar.razor`, `Stok` vb.) `Dil.T.Urun.ToplamHas` ("Toplam Has") anahtarını kullanmaya devam ediyor olabilir — bu paketin dosya sınırı dışında; kapsamı yalnız /raporlar.
- Tam süitte başka şeride ait kırmızı YOK (4867/4867).

## 11. Kurucuya / mali müşavire not
Mevzuat içermez. Gösterilen has gram sayıları değişmedi; yalnız adları doğrulandı ("Has (altın)").
