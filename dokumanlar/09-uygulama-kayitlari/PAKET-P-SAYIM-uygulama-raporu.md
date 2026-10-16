# PAKET P-SAYIM — uygulama raporu (denetim K5 + Y10)

Dalga DENETIM-VERI · şerit t3 (API 5164 · vekil 5165 · CDP 9611 · KUYUMCU_TEST_DB=kuyumcu_test_t3) · Taşkın (backend) + Ertuğ (ön yüz) · 10–11.10.2026 · COMMIT YOK.

Kaynak: `EKRAN-DENETIM-KULLANICI-GOZU-2026-10-10.md` K5, Y10 ve § Paketleme 3 ("okut+Enter=1, kenar boşluğu, otomatik oturum adı").

## 1. Spesifikasyonun koda karşı doğrulanması

| İddia (denetim 10.10) | Kodda bulunan | Sonuç |
|---|---|---|
| K5: okut+Enter → "Adet (isteğe bağlı)" altında "Bu alan zorunludur" | `Sayim.razor` `Say()`: `if (sayAdet is not {}) sayForm.Ekle("sy-adet", ZorunluAlan)` — etiket `Sayi` bileşeninde Zorunlu=false olduğu için "(isteğe bağlı)" yazıyordu | DOĞRU → düzeltildi |
| Sunucu davranışı | `POST /sayim/{id}/say` yalnız MUTLAK `sayilanAdet` kabul ediyordu ("son sayım geçerli"); "aynı barkod tekrar = +1" mümkün değildi | Sunucuya `artis` eklendi |
| Y10: Yıldız sayımda kenar boşluğu sıfır, "Sayıma dön" sağdan kesik | Gerçek Chrome'da ölçüldü (aşağıda): başlık sol 64 px (1280) / 16 px (375), "Sayıma dön" sağ boşluk 64 px, yatay kaydırma 0 | **Artık doğru değil** — YILDIZ-5A şeridi (t1) düzeltmiş (Yildiz5AFormTestleri.Y5A_F06 CSS kilidi). Bu pakette CSS'e DOKUNULMADI |
| Y10: "Sayım başlat" doğrudan saymıyor; ad zorunlu | Yıldız'da "Sayım başlat" → form (Ad boş, zorunlu) → Kaydet → okutma için ayrıca "Barkod okut" | DOĞRU → düzeltildi |
| Y10: "Bu kayıt değiştirilemez" uzun metni; "SİSTEM / GÜNCEL / OTURUMDA HAREKET VAR" sütunları | Özgün (ayrıntı) ekranda; tek şubede artık form/ayrıntıya uğramadan sayılıyor | Dosya alanı "yalnız mantık + etiket" → metin/sütun adı değişmedi, açık kalan olarak yazıldı |
| "Birleşik koşullar" bölümü | Kaynak belgede bu başlıkta bölüm YOK (grep "birle" 0) | Görevdeki EK KOŞULLAR bağlayıcı alındı |

## 2. Yapılanlar

**Kural (K5):** okut + Enter = 1 adet (adet boşsa 1) · aynı barkod tekrar = +1 · adet yazılmışsa o adet satırın sayılanı olur (mutlak — eski davranış korunur) · ADET biriminde kesir yok (UrunAdetKapisi) · gramla satılan üründe boş adette gram tahmin edilmez, alan altında "Gramla satılan üründe gramı yazın." Sayım kapanışı/düzeltme (`SayimServisi.Kapat`) DEĞİŞMEDİ.

- **Sunucu** `Kuyumcu.Api/Modules/Sayim/SayimModulu.cs`: `SayimSayDto.Artis` (decimal?, [Range 0..999999999,999]); `Say()`: `artis` verilirse yeni sayılan = mevcut (yoksa 0) + artış. Red kuralları: `artis` + `sayilanAdet` birlikte → 400 (alan artis); artış 0 → 400 SayiSifir, eksi → 400; ADET biriminde kesirli artış → 400 SayiTam; toplam kolon tavanını (999.999.999,999) aşarsa 400 (500 değil). Gövdede ikisi de yoksa eski 400 "Sayılan adet girilmedi" AYNEN. Yeni sunucu iletisi `SunucuIletiSozlugu.Veri.cs`'ye TR/EN çifti olarak eklendi (DIL-SUNUCU sayacı).
- **Ekran** `Kuyumcu.Web/Pages/Sayim.razor` (tüm şemalar): `Say()` boş adette `artis = 1` yollar, yazılmış adette `sayilanAdet`; kutu istek gitmeden HEMEN boşalır (tabanca art arda okutsa ikinci barkod silinmez), okutmalar `SemaphoreSlim` ile SIRAYLA gider (iki artış aynı satırı ezmez), başarı bandı satırın YENİ toplamını yazar ("Sayıldı: 22 Ayar Burma Bilezik × 3"), hata olursa okutulan değer kutuya geri döner ve hata alan altında + bantta (UX-S28). Odak her okutmadan ve oturum açılışından sonra `sy-barkod`'a döner (`kuyumcuOdak.alana`). `YeniAc()` adı işletme saatiyle otomatik verir: "Sayım 10.10.2026 23:45" (değiştirilebilir; boşaltılırsa eski "ad zorunlu" kapısı aynen çalışır). Oturum açılınca Yıldız'da okutma alanı doğrudan açılır.
- **Yıldız mantığı** `Kuyumcu.Web/Pages/Sayim.Yildiz.cs` (yalnız iki yöntem + bir bağlam satırı; görünüm/CSS YILDIZ-5A'nın): tek şubede "Sayım başlat" FORM SORMAZ → MEVCUT `Ac` (POST /sayim, ad otomatik, kapsam "Tüm ürünler") → okutma ekranı. Reyon/kategoriyle sınırlı sayım isteğe bağlı: ikincil `.yz-b2` "Reyon ya da kategori seçerek başlat" yalnız tek şubede VE reyon/kategori varken görünür, mevcut Yıldız formunu (ad dolu) açar. Çok şubede form açılır (şube seçimi zorunlu).
- **Sözlük** `Kuyumcu.Core/I18n/Sozlukler.cs` Sayim: `OtomatikAd` ("Sayım {0}" / "Count {0}"), `GramMiktarYaz`, `BolumleBaslat` — TR+EN.
- Mevcut element kimlikleri değişmedi; yeni kimlik yok (ikincil düğme mevcut `yildiz-sayim-b2`).

## 3. Sapmalar ve gerekçeleri

1. **Y10 kenar boşluğu**: kodda zaten düzelmiş bulundu (ölçümle kanıtlı, § 7); CSS'e dokunulmadı (YILDIZ-5A şeridinin alanı).
2. **Başka şeridin/eski paketlerin üç testi yeni sözleşmeye uyarlandı** (davranış değişikliği bu paketin emri; testlerin amacı korundu):
   - `Yildiz5AFormTestleri.Y5A_F05` — eski: "Sayım başlat → form, ad boş → istek yok". Yeni: tek şubede form yok, POST /sayim otomatik adla. (Boş ad kapısı aşağıdaki iki testte korunur; formun Yıldız kalıbı PS_B7'de.)
   - `UX1C2EkranTestleri.Sayim_bos_adla_oturum_acma_ISTEK_ATMAZ…` ve `Q95SayimFormTestleri.Q95_ad_BOS_sube_DOLU_hata_yalniz_ADDA` — ad artık dolu geldiği için test önce adı siler (`Input("")`), sonra eski kuralı aynen ölçer.
   Yedekler: `/private/tmp/kuyumcu-serit/P-SAYIM/yedek/`.
3. Yazılmış adet "o kadar" = mutlak (satırın sayılanı) yorumlandı: mevcut satır içi düzeltme ve `SatirDegismezYolu` metni ("doğru adedi yeniden sayın — satır güncellenir") bu anlamı taşıyor; okutma (boş adet) artırır.
4. Migration GEREKMEDİ (şema değişmedi; yalnız DTO alanı).

## 4. DoD sözleşmesi

Yeni varlık/kayıt türü YOK. `SayimSatiri` mevcut: sayılan değeri aynı uçla (`POST /sayim/{id}/say`, mutlak ya da artış) ve satır içi düzenlemeyle düzeltilir; "sayılmadı"ya geri dönüş DegismezIpucu ile gerekçeli (değişmedi). `SayimOturumu` adı PATCH ile düzeltilebilir (B-44, değişmedi); iptal ortak onay modalıyla (değişmedi). Yetki: `[ModulYetkisi("sayim")]` uç sınıfında (değişmedi). Finansal varlık yok. Her düğme gerçek istek atar: "Sayım başlat" (POST /sayim), "Reyon ya da kategori seçerek başlat" (formu açar; Kaydet POST), "Say"/Enter (POST …/say).

## 5. Testler

Yeni: `Kuyumcu.Tests/PSayimTestleri.cs` — **17 test** (DB 8 · HTTP 1 · bUnit 8):
PS_01 okut artışı 1/2/3 · PS_02 mutlak + artış + mutlak · PS_03 ADET'te kesir red, satır değişmez · PS_04 (0, −1) red · PS_05 artış+mutlak birlikte red · PS_06 GRAM'da kesirli artış kabul · PS_07 tavan 400 · PS_H1 gerçek HTTP boru hattı (3×200 → 3, kesir 400, boş 400, Türkçe ileti JSON'dan ayrıştırılarak) · PS_B1 okut+Enter ×3 → 3 istek `artis:1`, `sayilanAdet` yok, bant "× 3", adet hatası yok, etiket "(isteğe bağlı)" · PS_B2 yazılan adet mutlak, ardından okut +1 · PS_B3 gramlı üründe boş adet istek atmaz, alan altında gram ister · PS_B4 boş barkod Enter → barkod alanında hata · PS_B5 otomatik ad · PS_B6 (BASIT, PRO) Yıldız "Sayım başlat" form sormadan + okut ×3 · PS_B7 reyon varsa ikincil seçenek formu otomatik adla açar.

| Koşum | Toplam | Geçti | Kaldı |
|---|---:|---:|---:|
| PSayim filtreli (ilk) | 17 | 16 | 1 (PS_H1 kendi testimin JSON kaçışı → düzeltildi) |
| Sayım ilgili tüm sınıflar (PSayim, Sayim*, Yildiz5A*, UX1C2, Q95, UX4c, GocC, SADE1, P22, Q105, S3c) | 185 | 185 | 0 |
| Tam süit (tek kez, trx `/private/tmp/kuyumcu-serit/trx-pP-SAYIM`) | 12446 | 12435 | 8 (3 atlandı) |
| Tam süit sonrası düzeltilen Q95 + sayım sınıfları, 3 kez | 189 | 189 | 0 (×3) |

Tam süitteki 8 kırmızıdan **1'i bu paketin** idi (`Q95SayimFormTestleri.Q95_ad_BOS_sube_DOLU_hata_yalniz_ADDA` — otomatik ad) → düzeltildi, filtreli 3 koşuda yeşil. İlk yeniden koşuda bir kez daha kırmızı göründü: aynı anda başka şeridin derlemesi `ConvertDllsToWebCil` dosya kilidi hatası verdi (build-son2.log, MSB4018 IOException) ve test DLL'i eskiydi; temiz derleme sonrası hata ayıklama dökümüyle ve 3 koşuda yeşil. **Kalan 7 kırmızı bu paketin DEĞİL** (§ 10).

## 6. Mutasyon kanıtları (her biri: yedek → tek değişiklik → filtreli test → geri al → `cmp` AYNI)

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alındı |
|---|---|---|---|
| M1 | Sunucu: `sayilan = mevcut + artis` → `sayilan = artis` | PS_01, PS_02, PS_06, PS_07, PS_H1 | cmp AYNI |
| M2 | Sunucu: artışta ADET kesir kapısı silindi | PS_03, PS_H1 | cmp AYNI |
| M3 | Sunucu: "artış + mutlak birlikte" reddi silindi | PS_05 | cmp AYNI |
| M4 | Ekran: boş adette `artis = 1m` → `sayilanAdet = 1m` | PS_B1, PS_B2, PS_B6(BASIT), PS_B6(PRO) | cmp AYNI |
| M5 | Ekran: eski kural geri ("adet boşsa zorunlu") | PS_B1, PS_B2, PS_B3, PS_B4, PS_B6(BASIT), PS_B6(PRO) | cmp AYNI |
| M6 | Ekran: otomatik ad kaldırıldı (`yeni = new()`) | PS_B5, PS_B6(BASIT), PS_B6(PRO), Y5A_F05 (+1) | cmp AYNI |
| M7 | Yıldız: tek şubede doğrudan `Ac` kaldırıldı | PS_B6(BASIT), PS_B6(PRO), Y5A_F05 | cmp AYNI |

Son mutasyondan sonra temiz derleme: `dotnet build Kuyumcu.sln` 0 Hata · 0 Uyarı (build-son.log); son derleme (build-son4) 0 Hata · 6 Uyarı — 6 uyarının hepsi başka dosyalarda (SUBEKAPSAMASinirTestleri.cs:49, KesifP2Testleri.cs:455, SonKalanlarTestleri.cs:593/824, Yildiz2Testleri.cs:194/234); bu paketin dosyalarında uyarı 0.

## 7. Gerçek Chrome ölçümü (başsız Chrome, cdp.mjs, CDP 9611, tek profil `/private/tmp/kuyumcu-serit/P-SAYIM/profil`)

Senaryo her kombinasyonda gerçek kullanıcı akışı: açık oturum varsa API ile iptal → şema/tema/mod API ile → `/sayim` → "Sayım başlat" (Platin: "+ Yeni sayım" → otomatik adlı form → "Oturumu Aç"; Yıldız: tek tık) → odaktaki kutuya `Input.insertText("8690000000074")` + gerçek `keyDown/keyUp Enter` × 3 (barkod tabancası taklidi; kutuya elle odak VERİLMEDİ, odak uygulamadan geldi) → API'den satır okunur → sikisma-olc.js `__olcTam()`.

| Şema · tema · mod · genişlik | API sayılan | Odak (açılış/son) | Bant | Adet hatası | Yatay | K1 | K3 taşan/kırık | K5 TRY | K7 | K8 | K9 | K10/K10c | K11 | K12 |
|---|---:|---|---|---|---:|---:|---|---:|---:|---:|---:|---|---:|---:|
| platin açık BASIT 1280 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/1* | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| platin açık BASIT 375 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| platin açık PRO 1280 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/1* | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| platin açık PRO 375 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| platin koyu BASIT 1280 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/1* | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| platin koyu BASIT 375 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| platin koyu PRO 1280 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/1* | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| platin koyu PRO 375 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| yıldız açık BASIT 1280 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| yıldız açık BASIT 375 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| yıldız açık PRO 1280 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| yıldız açık PRO 375 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| yıldız koyu BASIT 1280 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| yıldız koyu BASIT 375 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| yıldız koyu PRO 1280 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |
| yıldız koyu PRO 375 | 3 | sy-barkod/sy-barkod | × 3 | yok | 0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0/0 | 0 | 0 |

\* Platin 1280 K3 kırık 1: `td.hucre-vurgu "14 Ayar Kolye KLY-14-01"` — özgün sayım ızgarasının ürün hücresi (ad + kod) iki satıra iniyor. Kısa etiket değil, tablo hücresi; bu paket ızgaraya dokunmadı (önceden var olan durum) → açık kalan. K2 (dar sütun) Platin 97–98 %, Yıldız ölçülmedi (null, iki sütunlu tuval kalıbı).

Y10 kenar ölçümü (Yıldız): okutma ekranında başlık sol 64 px (1280) / 16 px (375); ayrıntı görünümünde `yildiz-sayim-ust`/`-baslik` sol 64/16 px, "Sayıma dön" sağ boşluk 64 px (1280), 375'te sol 16 px; scrollWidth = viewport. Kesik yok.

Ekran görüntüleri: `/private/tmp/kuyumcu-serit/P-SAYIM/foto/<sema>-<tema>-<mod>-<genişlik>.png` (16 adet). Göz ile bakılanlar (3): yildiz-karanlik-BASIT-1280 (1 / 3 ürün, "Sayıldı: 22 Ayar Burma Bilezik × 3", liste 0 · 3 · +3, imleç barkod kutusunda), yildiz-aydinlik-PRO-375 (aynı, kenar 16 px, taşma yok), platin-aydinlik-BASIT-1280 ("Sayım 10.10.2026 23:46" otomatik ad, Adet "(isteğe bağlı)", odak çerçevesi barkodda, bant yeşil).

Dürüstlük notu: ilk toplu koşuda `yildiz-aydinlik-BASIT-375`'te ölçer, "Sayım başlat"tan 2,5 sn sonra `sy-barkod`'u bulamadı ve betik çöktü (oturum açılmış, okutma yapılmamış — SQL'de sayılan NULL olan tek satır). Ölçer betiği 10 sn'ye kadar bekleyecek şekilde düzeltildi (yeniden tıklama gerekirse sayılıyordu: 0 kez gerekti), aynı kombinasyon dahil kalan 7 kombinasyon yeniden koşuldu ve geçti. Elle tekrarda (hata2.mjs) tek tıkla 3 sn içinde okutma alanı açık, odak barkodda.

## 8. Canlı doğrulama (API 5164, test kiracısı `PAKETP-SAYIM-TEST Kuyumcu`, 3 ürün barkodlu)

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /auth/kayit (tek kullanımlık davetle) | 201 | 201 |
| POST /urunler ×3 | 201 | 201 ×3 |
| Tarayıcıda okut+Enter ×3 (16 kombinasyon) | sayılan 3 | 16/16 → 3 |
| POST …/say `{barkod, artis:1}` (4. okutma) | 200, sayılan 4 | 200, 4 |
| `{artis:1.5}` | 400 kesir | 400 "Küsuratlı yazılamaz; tam sayı girin." alan artis |
| `{artis:0}` | 400 | 400 "Değer 0'dan büyük olmalı." alan artis |
| `{artis:-1}` | 400 | 400 (DataAnnotations Range) alan artis |
| `{artis:1, sayilanAdet:2}` | 400 | 400 "Sayılan adet ile artış birlikte gönderilemez" |
| `{barkod}` (ikisi de yok) | 400 eski kural | 400 "Sayılan adet girilmedi" |
| `{sayilanAdet:2}` | 200 mutlak 2 | 200, 2 |
| bilinmeyen barkod | 404 | 404 "Barkod bulunamadı: 0000000000000" |
| POST …/kapat | 200, uygulanan 1 | 200 {uygulanan:1, atlanan:0, KAPANDI} |

SQL (kuyumcu_net): `sayim_satirlari` barkod 8690000000074 → 16 tarayıcı oturumunda `sayilan_adet = 3.000` (17. satır NULL = yukarıdaki çöken ölçüm denemesi); kapanan oturum `KAPANDI · 2.000`; `stoklar.adet` = 2.000 (kapanış davranışı değişmeden stoğa yazdı).

Temizlik: API (5164), vekil (5165), Chrome (9611) yalnız kendi PID'leriyle durduruldu, portlar boş. `zsh kiraci-temizle.sh 01M4KRYN9HP65FD5RH881H6X79 --uygula` → kiracı yok (0); `tenants` = 24 = başlangıç. Kendi davet satırı (`kiraci_davetleri`, olusturan_not 'P-SAYIM serit') silindi (0). QA-TEST- kiracılarına dokunulmadı.

## 9. Denetim

`dotnet/denetim.sh` (son): ✗ yalnız iki sayaç, ikisi de bu paketin değil:
- P-30 DEMO_INSAN_SURESI — görevde hariç tutulan.
- GIRDI-2 TEST_GECERSIZ_FIKSTUR 1 — `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (geçersiz IBAN fikstürü; dosya 09.10 15:38, doğrulayıcı/MASAK şeridi).
İlk koşuda bu paketin ürettiği DIL-SUNUCU (çıplak Türkçe sunucu iletisi) ✗ çıktı → TR/EN çifti eklendi → ✓. DTO_DECIMAL_RANGESIZ tavanı artmadı (yeni alan [Range]'li).

## 10. Açık kalanlar (sahipleriyle)

Tam süitte bu paketin olmayan 7 kırmızı (Sayım'a dokunmuyor):
- `Yildiz5ATestleri.Y5A_04_emanet_satirlari_fifo_storno_esik` (beklenen "Emanette", gelen "Asti") — YILDIZ-4D/5A şeridi (t1).
- `Yildiz1Testleri.Y1_10_kural_govdelerinde_sabit_renk_yok` (`.yz-tezgah-odeme-govde` gradyanı) — YILDIZ-TEZGAH-ODEME şeridi (t7).
- `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula…`, `TevsikIhlalDbTestleri.Ayni_cari_gun_GUNLUK_varsa_TAKSIT…`, `TevsikIhlalDbTestleri.K9_kural_kapali_kiracida…` — YASAL-KAPI-3 şeridi (t9) alanı.
- `RaporDortTestleri.RD_KH01/RD_KH02` ("Rapor dönemi en çok 400 gün olabilir") — tarih bağımlı (gün 11.10'a döndü), rapor paketi sahibi.
- Denetim GIRDI-2 (MASAKESZAMAN fikstürü) — doğrulayıcı/MASAK şeridi.
- Platin sayım ızgarasında ürün hücresi (ad + kod) 1280'de iki satır (K3 kırık 1) — önceden var; ızgara bu paketin alanı dışında.
- Y10'un metin kısmı: özgün ayrıntı ekranında uzun DegismezIpucu metni ve "Sistem / Güncel / Oturumda hareket var" sütun adları (BASİT dil sadeleştirmesi) — "yalnız mantık + etiket" alanı dışında; Yıldız yüzünde tek şubede artık bu ekrana uğranmıyor. Öneri: P-DIL/YILDIZ-KALAN.
- `artis: -1` reddinde DataAnnotations iletisi alan adını ham "Artis" yazıyor (mevcut `SayilanAdet` ile aynı kalıp); ekran eksi artış göndermediği için kullanıcıya çıkmaz.
- Proje ağacında bu paketin olmayan `dotnet/Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` duruyor (başka şerit; silinmedi).

## 11. Kurucu / mali müşavir

Mevzuat hükmü içermez (stok sayımı ekran/akış kuralı). Soru yok.

## 12. Dokunulan sayfalar / değişen dosyalar

Dokunulan sayfa: `Pages/Sayim.razor` (+ `Pages/Sayim.Yildiz.cs` mantık). İkinci dokunuş gerekçesi: YILDIZ-5A Yıldız görünümünü yazdı; bu paket yalnız mantığı (doğrudan başlatma, okutma kuralı) değiştirdi, görünüm/CSS'e dokunmadı.

`git status --porcelain | grep -v -E "/(bin|obj)/"` içinden bu paketin dosyaları:
- `dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs` (izlenmeyen dizin)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` ( M)
- `dotnet/Kuyumcu.Core/I18n/SunucuIletiSozlugu.Veri.cs`
- `dotnet/Kuyumcu.Web/Pages/Sayim.razor`
- `dotnet/Kuyumcu.Web/Pages/Sayim.Yildiz.cs`
- `dotnet/Kuyumcu.Tests/PSayimTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/Yildiz5AFormTestleri.cs` (Y5A_F05 uyarlandı)
- `dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs` (1 satır)
- `dotnet/Kuyumcu.Tests/Q95KasaSayimCariTestleri.cs` (1 satır)

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| okut + Enter = 1 (adet boşsa 1) | KARŞILANDI | PS_B1, PS_01, Chrome 16/16 |
| aynı barkod tekrar = +1 | KARŞILANDI | PS_01, PS_B1, canlı 4. okutma 4 |
| adet yazılmışsa o kadar | KARŞILANDI | PS_02, PS_B2, canlı `sayilanAdet:2` |
| ADET'te kesir yok | KARŞILANDI | PS_03, PS_H1, canlı 400 |
| Etiket ile kural aynı (isteğe bağlı → zorunlu hatası yok) | KARŞILANDI | PS_B1 etiket + hata yok; Chrome adet hatası 16/16 yok |
| Sayım kapanışı/düzeltme değişmez | KARŞILANDI | SayimOturumuTestleri yeşil; canlı kapat 200, stok 2 |
| Kenar boşluğu (Y10) | KARŞILANDI (önceden düzelmiş, ölçüldü) | § 7 kenar ölçümü |
| Otomatik oturum adı | KARŞILANDI | PS_B5, PS_B6, SQL oturum adları |
| "Sayım başlat" doğrudan okutmaya | KARŞILANDI | PS_B6, M7, Chrome Yıldız 8/8 |
| Gerçek Chrome BASİT+PRO, 1280+375, platin+yıldız, açık+koyu | KARŞILANDI | § 7 tablo (16) + 3 görüntü |
| bUnit + HTTP testleri | KARŞILANDI | 17 yeni test |
| Mutasyon ≥ 3 kırmızı, cmp AYNI | KARŞILANDI | 7/7 |
| build 0 hata | KARŞILANDI | build-son4: 0 Hata (6 uyarı başkalarının) |
| denetim TEMİZ (P-30 hariç) | KISMEN — bu paketin sayacı yok; GIRDI-2 başka şeridin | § 9 |
| tam süit 0 yeni kırmızı | KARŞILANDI | tek kendi kırmızısı (Q95) düzeltildi; 7 başkasına ait |
| Test kiracısı silindi, tenants başlangıçta | KARŞILANDI | 24 = 24 |
| Yıldız koruması (tuval kalıbı, yeni öğe) | KARŞILANDI | yeni öğe yalnız mevcut `.yz-b2`; CSS'e dokunulmadı |
