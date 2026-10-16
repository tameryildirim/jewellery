# PAKET Q9-2: FATURA, taslak faturada satır ekleme ve düzeltme (KRİTİK) · UYGULAMA RAPORU

- **Tarih:** 2026-09-24 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** DEMO DÜZELTME (QA turu 9)
- **Kaynak:** `ham/qa-turu-9-bulgular.json`, `/faturalar` rotasındaki iki kayıt: KRİTİK/DUZENLENEBILIRLIK "Fatura kalemleri düzenlenemiyor: taslak Düzenle kipinde kalem bölümü yok" ve DUZENLENEMEYEN özeti.
- **Durum:** BİTTİ. Paket kapısının yedi maddesi aşağıda kanıtlarıyla var: build 0/0, 18/18 kendi testim, denetim TEMİZ, 18/18 mutasyon kırmızı, canlı + gerçek tarayıcı, kiracı silindi, tam süit 5072/5072.
- İzolasyon: testler `KUYUMCU_TEST_DB=kuyumcu_test_l`, API 5174, vekil 5173, kendi sekmem (tab-26, sonra tab-28). 5199/5175/5176/5179/5181 portlarına dokunulmadı.

---

## 1. Bulgunun yeniden üretimi (kod yazılmadan ÖNCE)

Test kiracısı `PAKETQ9-2-TEST` açıldı. Kiracıya 1 kalemli taslak (2×1000 + 1×500), 1 kesilmiş ve 1 satırsız taslak fatura eklendi.

| İddia | Ölçüm (değişiklikten önce) | Sonuç |
|---|---|---|
| Düzenle çekmecesinde kalem bölümü yok | `#cekmece-fatura` girdileri: `fd-duzenle-cari`, `fd-duzenle-tarih`, `fd-duzenle-aciklama`, `fd-duzenle-toplam`. `#fd-kalem-tablosu` ve `si-fatura-kalem-*` YOK. Metin şuydu: "Toplam kalemlerden hesaplanır; kalemleri düzenleyin." Kullanıcıyı kalem düzenlemeye çağırıyor ama o yolu açmıyor (ekran görüntüsü alındı). | **ÜRETİLDİ** |
| Kalem ekleme ucu | `POST /api/v1/faturalar/{id}/kalemler` → **404 "Uç bulunamadı"** | **ÜRETİLDİ** (uç yok) |
| P-54 hücreleri | Kod ölçümüne göre "Kalemleri Göster" penceresinde TASLAK için `SatirIciDuzenle` hücreleri VAR (P-54). QA'nın "salt okunur" gözlemi kesilmiş belge ya da P-54 öncesi yayınla açıklanır. Asıl eksik Düzenle kipinde bu yolun ve ekleme yolunun olmamasıydı. | Kısmen kapanmış: düzeltme hücresi vardı, ekleme ve Düzenle kipi yoktu |
| Kalem silme | P-22 sunucu kuralı "kalem SİLİNMEZ". `DELETE …/kalemler/{id}` → 404 | Kural korunur, gerekçe + yol (UX-S25) |

## 2. Yapılanlar

### 2.1 Backend (Taşkın): `Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`
- **Yeni servis `FaturaServisi.TaslakKalemEkle`** (TASLAK + KALEMLİ belgeye satır ekler):
  - Kapı `TaslakBul` (P-22). Kesilmiş, ödenmiş ya da iptal belge → 400: "Yalnız TASLAK… iptal edip yeni belge kesmektir."
  - **Satırsız (BASLIK) taslak → 400.** Gerekçe: toplam elle girilmişti, ilk satır bu toplamı sessizce ezerdi. Gösterilen yol: Toplam alanı ya da iptal + satırlı yeniden kesim.
  - **Hareket bağlı satır → 400.** Damga ve has dayanağı "Fişten Fatura Kes" yolunda kurulur (B-4).
  - Elle sınır denetimleri (DataAnnotations yalnız controller yolunda çalışır): miktar > 0, fiyat/işçilik/indirim ≥ 0, açıklama ≤ 300, açıklama ya da ürün zorunlu, satır toplamı > 0, en çok `KalemUstSiniri` (200) kalem.
  - Hesap `KalemYolu` ile birebir aynı kuralla yapılır: `miktar × birim fiyat + işçilik − indirim`, `OzelMatrahMotoru` (tek motor). KUYUM'da ürün zorunlu. Has bacağı ürün has gramı × kiracının EN SON kur kaydıdır. Bu kural harekete bağlı olmayan kalemdeki mevcut davranışın aynısıdır. Kur yoksa fail-closed 400.
  - KDV türü ve oranı belgeden alınır (UX-S21: kullanıcıya sorulmaz). Ürün kartından ayar, milyem, gram, has ve taş ibaresi kopyalanır.
  - Başlık KALEMLERDEN toplanır: Toplam, KdvTutar, HasDeger, AraToplam. KUYUM'da HasGram ve HasBirimFiyat da toplanır.
  - Okuma sınırlı: `.OrderBy(Sira).Take(KalemUstSiniri + 1)` (B-8).
  - Yanıt `KalemYaniti`. Ekran toplamı buradan okur.
- **Yeni uç:** `POST /api/v1/faturalar/{id}/kalemler`. `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` PATCH ile aynıdır. Sınıfta `[ModulYetkisi("fatura")]` var.
- Denetim izi: `DenetimYazmaDinleyicisi` otomatik yazar. Canlıda `EKLEME FaturaKalemi` bir arttı (§ 7). Yeniden yazılmadı.
- **Migration: GEREKMEDİ.** Şema değişmedi (`fatura_kalemleri` mevcut).

### 2.2 Sözleşme: `Kuyumcu.Contracts/Sayfalar/FaturalarSozlesmeleri.cs`
- `FaturaVeri`'nin sonuna varsayılanlı `string ParaBirimi = "TRY"` eklendi. Sunucu yanıtında zaten vardı (`CariliYanit`). Yeni satırın tutar alanları belgenin para birimini taşır (UX-S32).

### 2.3 Sözlük: `Kuyumcu.Core/I18n/Sozlukler.FaturaTaslak.cs`
- P-53/P-54'ün bu şerit için açtığı ayrı dosyaya 13 anahtar eklendi, TR + EN. Konumsal record olduğu için eksik anahtar derleme hatası verir. HUB'a dokunulmadı. Jargon yok ("kalem" yerine "satır").

### 2.4 Ekran (Ertuğ): `Kuyumcu.Web/Pages/Faturalar.razor`
- **Düzenle çekmecesinde "Satırlar" bölümü var** (`#fd-kalem-bolumu`).
  - KALEMLİ taslakta her satır bir kutu (`.fd-cekmece-kalem`). Açıklama, miktar, birim fiyat, işçilik ve indirim `SatirIciDuzenle` ile düzeltilir. Bu, P-54 ucunun ve tek alanlı kalıbın aynısıdır, yeni kalıp açılmadı. Hata alan altında gösterilir.
  - Genel toplam (`#fd-cekmece-kalem-genel-toplam`) ve satır toplamları sunucu yanıtından gelir. Çekmecenin Toplam alanı da tazelenir.
  - Başarı çekmecenin içinde görünür (`#fd-kalem-basari`). Sayfa bandı çekmecenin arkasında kaldığı için bu gerekliydi.
  - **Yeni satır formu** (`#fd-kalem-yeni`) oluşturma formuyla aynı alan kümesini taşır (UX-S26): ürün (ortak `Secici`), açıklama (`Metin`), miktar (`Sayi`), birim fiyat, işçilik ve indirim (`Para`, belge para birimiyle). Kaydetmeden önce satır toplamı önizlenir (UX-S23). "Satırı ekle" düğmesi ikincil; birincil düğme çekmecenin Kaydet'idir, ekran başına tek birincil kuralı korunur.
  - Boş ya da geçersiz gönderimde istek gitmez, alan altında Türkçe mesaj çıkar. Denetlenenler: miktar zorunlu ve > 0; fiyat zorunlu ve ≥ 0; açıklama ya da ürün; KUYUM'da ürün; satır toplamı > 0.
  - Sunucu 400'ü ilgili alanın altına yazılır: önce `alanlar` sözlüğü, sonra mesaj anahtar sözcüğü. Çekmece kapanmaz.
  - Kaydederken düğme kilitlenir ve sebep yazar (`#fd-kalem-yeni-ekle-kilit`, UX-S30).
  - Satırlar okunamazsa hata bandı çıkar, boş duruma çevrilmez (UX-S28). Ürün listesi yüklenemezse ayrı bant çıkar. 200 sınırına değince bu söylenir (UX-S29).
  - **Satırsız taslak:** `DegismezIpucu FaturaSatirsizTaslak` gösterilir. Metin gerekçeyi ve gerçekten var olan yolları yazar: Toplam alanı, İptal Et + Yeni Fatura.
  - **Tek satır silme:** P-54'teki `DegismezIpucu FaturaKalemSilme` çekmecede de gösterilir. Silme düğmesi yok (§ 3.2).
- Kesilmiş fatura: Düzenle eylemi yok. Kalem penceresinde hücre yok, `DegismezIpucu FaturaKalemi` (iptal + yeni belge yolu) var. Bu davranış değişmedi, canlıda ölçüldü.
- Mevcut id'ler değişmedi. Yeni id'ler `fd-kalem-*` ve `fd-cekmece-kalem-*` önekli. ModalPencere 48 → 48, `duzenlenenId` 1/1. Yeni CSS yok, app.css'e dokunulmadı. Mevcut sınıflar kullanıldı: `kart kart-govde`, `ozet-kutu`, `form-dikey`. Satır içi style yok, sabit renk yok.

## 3. Sapmalar ve gerekçeleri
1. **Spesifikasyon dosyasında "Birleşik koşullar" bölümü yok.** `qa-turu-9-bulgular.json` 171 kayıtlık düz bir liste. Bağlayıcı hedef görev metnindeki EK KOŞULLAR'dan alındı.
2. **Kaldırma yerine gerekçeli DegismezIpucu.** Sunucu kuralı (P-22) "kalem silinmez". Finansal belge satırı için pasifleştirme şema değişikliği ister: `fatura_kalemleri`'ne kolon eklenir ve çıktı, e-belge, rapor okuyucularının hepsi süzmeye başlar. Bu paketin dosya alanını aşar. Görev bu dalı açıkça izin veriyor. Gerçek yol: hücreyi düzelt ya da taslağı İptal Et + yeniden kes (taslak numara tüketmez).
3. **Satırsız taslağa ekleme kapalı.** Elle girilmiş toplamın ilk satırla sessizce ezilmesini önlemek için. Ekranda gerekçe ve yol yazılı.
4. **İlk sürümde ham `<input>`/`<select>` vardı.** denetim.sh `P-36 HAM_INPUT` (47 > 42) ve `B-8 ToListAsync` (146 > 145) ✗ verdi. Form ortak girdi ailesine (Secici/Metin/Sayi/Para) taşındı ve okuma `.Take` ile sınırlandı. Denetim TEMİZ oldu.
5. **İlk tarayıcı ölçümünde gerçek kusur çıktı.** "Satırlar" başlığı ile ipucu metni aynı satıra biniyordu (`binen 1`). Ayrıca kart iç dolgusu 0 olduğu için tutar kenara yapışıyordu. Çözüm: `form-dikey` + `kart-govde` + `ozet-kutu` (mevcut sınıflar). Ölçüm yeniden yapıldı, sonuç 0.

## 4. Mevzuat notu (MEVZUAT KURALI)
- **Yeni mevzuat hükmü YAZILMADI.** "Kesilmiş belge değişmez, düzeltme yolu iptal + yeni belge" kuralı P-22'den beri sunucudaydı (`TaslakBul`). Ekran yalnız bu kuralın gerekçesini gösteriyor.
- KUYUM satırında has bedeli, mevcut `KalemYolu` kuralının aynısıyla türetildi (tek motor `OzelMatrahMotoru`, fail-closed). Yeni oran ya da eşik eklenmedi. Web araştırması gerekmedi.
- **Mali müşavire not (bilgi):** Taslağa sonradan eklenen KUYUM satırı "kiracının en son kur kaydı"nı kullanır, eski satırlar kendi kurlarını korur. Belgedeki özet kur ağırlıklı ortalamadır. Bu davranış kalemli fatura oluşturmadakiyle aynıdır ve bu pakette değişmedi. Taslakta farklı kurlu satır karışımı kabul edilebilir mi, teyit edilmesi yararlı olur.

## 5. Testler

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Filtre `Q92` (yeni 18 test, son kod) | 18 | 18 | 0 |
| İlgili süitler (`Fatura, P54, P53, P22, UX1C, UX4, Sozluk, Sozlesme, Q92, P61, P78, Duzenle, Dugme, Ikon, MobilParite, Metin`) | 990 | 990 | 0 |
| **Tam süit** (`kuyumcu_test_l`, trx: `scratchpad/trx-pQ9-2`, son koşum) | **5072** | **5072** | **0** |

Bu paket **+18 test** ekledi. Q9-1 raporunda ağaç 5054'tü.

**Yeni testler**
- `Kuyumcu.Tests/Q92FaturaKalemEklemeHttpTestleri.cs` [HTTP + gerçek MySQL, 8]:
  - `Q92_taslaga_satir_EKLENIR_ve_BASLIK_TOPLAMI_kalemlerden_toplanir` (eklenen satır PATCH ile de düzelir)
  - `Q92_urunlu_satirda_ACIKLAMA_urun_adindan_gelir_ve_urun_kopyalanir`
  - `Q92_KESILMIS_faturaya_satir_EKLENMEZ_400_ve_DUZELTME_YOLU_yazar`
  - `Q92_SATIRSIZ_taslaga_satir_EKLENMEZ_elle_toplam_EZILMEZ`
  - `Q92_miktar_SIFIR_negatif_fiyat_ve_bos_aciklama_400_veri_DEGISMEZ`
  - `Q92_hareket_bagli_satir_sonradan_EKLENMEZ_400`
  - `Q92_KUYUM_taslakta_urunsuz_satir_400_urunlu_satirda_HAS_BEDELI_sunucuda_turetilir`
  - `Q92_TEZGAHTAR_satir_ekleyemez_403_baska_kiracinin_belgesi_404`
- `Kuyumcu.Tests/Q92FaturaKalemEkranTestleri.cs` [bUnit, 10; durumlu sahte API, yeni uca gerçekçi yanıt]:
  - `Q92_Duzenle_cekmecesinde_SATIRLAR_duzenlenebilir_ve_YENI_SATIR_formu_var`
  - `Q92_yeni_satir_POST_edilir_ve_TOPLAM_sunucu_yanitindan_okunur`
  - `Q92_bos_gonderimde_ALAN_ALTINDA_mesaj_ve_POST_GITMEZ`
  - `Q92_sunucu_400_ILGILI_ALANIN_ALTINA_yazilir_cekmece_acik_kalir`
  - `Q92_cekmecede_satir_DUZELTILIR_PATCH_ve_toplam_cekmecede_yenilenir`
  - `Q92_satirsiz_taslakta_EKLEME_YOK_gerekce_ve_gercek_yol_yazili`
  - `Q92_satir_okuma_hatasi_BOS_DURUMA_cevrilmez_hata_bandi_gorunur`
  - `Q92_kuyum_taslakta_urunsuz_satir_ALAN_ALTINDA_reddedilir_POST_GITMEZ`
  - `Q92_Ekle_dugmesi_kaydederken_KILITLI_ve_sebebi_yazili`
  - `Q92_KESILMIS_faturada_Duzenle_YOK_ve_satirlar_KILITLI_sebep_yazili`

## 6. Mutasyon kanıtları
Yöntem: kaynak `scratchpad/mut-yedek/Q9-2/` altına kopyalandı. Tek eşleşmeli değişiklik yapıldı; eşleşme sayısı 1 değilse değişiklik uygulanmadı. Filtreli `dotnet test -v n` koşuldu, yedek geri kopyalandı ve `cmp` karşılaştırması yapıldı (betik `scratchpad/q92/mut.py`).
- Uygulama perl yerine Python `str.count == 1` + `replace` ile yapıldı. Sebep: `perl -pi` içinde die olursa dosya yarım kalabiliyor. Eşdeğer ve daha güvenli.
- Web mutasyonları ortak girdi ailesine geçişten SONRA son kod üzerinde yeniden koşuldu. API mutasyonları 1–8, `.Take` eklenmeden önceki sürümde koşuldu; `.Take` satırı mutasyon hedeflerinin hiçbirine değmez.
- Son mutasyondan sonra `touch` + TEMİZ build: 0 hata, 0 uyarı. Ardından Q92 18/18 yeşil.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M92-1 | `TaslakBul` yerine durum kapısız okuma | `Q92_KESILMIS_faturaya_satir_EKLENMEZ_400_ve_DUZELTME_YOLU_yazar`, `Q92_TEZGAHTAR_…_404` | cmp=AYNI |
| M92-2 | Başlık `Toplam` kalemlerden toplanmıyor | `Q92_taslaga_satir_EKLENIR_ve_BASLIK_TOPLAMI_…`, `Q92_KUYUM_…` | cmp=AYNI |
| M92-3 | Satırsız taslak kapısı `if (false)` | `Q92_SATIRSIZ_taslaga_satir_EKLENMEZ_elle_toplam_EZILMEZ` | cmp=AYNI |
| M92-4 | KUYUM ürün zorunluluğu kalktı | `Q92_KUYUM_taslakta_urunsuz_satir_400_…` | cmp=AYNI |
| M92-5 | Hareket bağlı satır kapısı kalktı | `Q92_hareket_bagli_satir_sonradan_EKLENMEZ_400` | cmp=AYNI |
| M92-6 | Satır toplamı > 0 kapısı kalktı | `Q92_miktar_SIFIR_negatif_fiyat_ve_bos_aciklama_400_veri_DEGISMEZ` | cmp=AYNI |
| M92-7 | `[RolGerekli]` silindi | `Q92_TEZGAHTAR_satir_ekleyemez_403_…` | cmp=AYNI |
| M92-8 | KUYUM başlık `HasGram` güncellenmiyor | `Q92_KUYUM_…_HAS_BEDELI_sunucuda_turetilir` | cmp=AYNI |
| M92-9 | Ekran toplamı yerel hesaptan | `Q92_yeni_satir_POST_edilir_ve_TOPLAM_sunucu_yanitindan_okunur` | cmp=AYNI |
| M92-10 | "Açıklama ya da ürün" istemci kuralı silindi | `Q92_bos_gonderimde_ALAN_ALTINDA_mesaj_ve_POST_GITMEZ` | cmp=AYNI |
| M92-11 | Sunucu 400'ünün alana yazılması silindi | `Q92_sunucu_400_ILGILI_ALANIN_ALTINA_yazilir_cekmece_acik_kalir` | cmp=AYNI |
| M92-12 | Satırsız taslakta da kalem okunuyor | `Q92_satirsiz_taslakta_EKLEME_YOK_gerekce_ve_gercek_yol_yazili` | cmp=AYNI |
| M92-13 | Okuma hatası boş listeye çevrildi | `Q92_satir_okuma_hatasi_BOS_DURUMA_cevrilmez_hata_bandi_gorunur` | cmp=AYNI |
| M92-14 | Ekle düğmesi `disabled="@(false)"` | `Q92_Ekle_dugmesi_kaydederken_KILITLI_ve_sebebi_yazili` | cmp=AYNI |
| M92-15 | KUYUM ürün istemci kuralı kalktı | `Q92_kuyum_taslakta_urunsuz_satir_ALAN_ALTINDA_reddedilir_POST_GITMEZ` | cmp=AYNI |
| M92-16 | Çekmecede düzeltme sonucu yazılmıyor | `Q92_cekmecede_satir_DUZELTILIR_PATCH_ve_toplam_cekmecede_yenilenir` | cmp=AYNI |
| M92-17 | Çekmece satırları hiç yüklemiyor | 5 test: `Q92_Duzenle_cekmecesinde_…`, `Q92_Ekle_dugmesi_…`, `Q92_bos_…`, `Q92_cekmecede_…`, `Q92_kuyum_…` | cmp=AYNI |
| M92-18 | Birim fiyat zorunluluğu kalktı | `Q92_bos_gonderimde_ALAN_ALTINDA_mesaj_ve_POST_GITMEZ` | cmp=AYNI |

**18/18 mutasyon kırmızı.** Test açığı çıkmadı.

## 7. denetim.sh
Son koşum: **`DENETİM TEMİZ.` (exit 0).**

Bu paketin sayaçları:

| Sayaç | Değer |
|---|---|
| `IgnoreQueryFilters` | 0 |
| `Find/FindAsync` | 0 |
| `ExecuteUpdate/Delete` | 5 |
| `Hareketler.Add` | 1 |
| `record` | 3 (tavan 3) |
| satır içi style | 109 |
| ModalPencere | 48 (tavan 55, artış 0) |
| `duzenlenenId` | 1/1 |
| Sınırsız ToListAsync | 145 (tavan 145) |
| SABIT_RENK | 0 |
| HAM_SELECT | 29 (tavan 30) |
| HAM_INPUT | 42 (tavan 42) |
| HAM_*_SERBEST | 0 |

Ara koşumda çıkan ✗'lerin ikisi de bu paketindi ve giderildi (§ 3.4).

## 8. Canlı doğrulama (API 5174 + vekil 5173, kendi sekmem, kuyumcu_net)

### 8.1 API + SQL

| Adım | Beklenen | Gerçek |
|---|---|---|
| Değişiklikten önce: `POST …/kalemler` | uç yok | **404** "Uç bulunamadı" |
| Kesilmiş belgeye `POST …/kalemler` | 400 + yol | **400** "Yalnız TASLAK fatura düzenlenebilir; bu belge KESILDI durumunda. Kesilmiş belgenin düzeltme yolu iptal edip yeni belge kesmektir." |
| Satırsız taslağa `POST …/kalemler` | 400 + gerekçe | **400** "Bu taslak satırsız (yalnız toplam) kaydedildi; satır eklenemez. …" |
| Miktar 0 | 400 alan düzeyinde | **400** `alanlar.miktar = "Miktar 0,001 veya daha büyük olmalıdır"` |
| `DELETE …/kalemler/{id}` | uç yok (P-22) | **404** |

### 8.2 Tarayıcı (gerçek tıklama ve klavye)

| Adım | Beklenen | Gerçek |
|---|---|---|
| Düzenle → boş "Satırı ekle" | alan altı mesaj, istek yok | `alan-hata-fd-kalem-yeni-aciklama: Açıklama yazın ya da ürün seçin.`, `alan-hata-fd-kalem-yeni-fiyat: Bu alan zorunludur.`, POST sayısı 0 |
| Klavyeyle "Q92 canli yuzuk", 2, "1.250,50", işçilik 100 → Ekle | önizleme ₺2.601,00; POST 200; toplam 5.101,00 | bant "Satır eklendi: 3. satır · yeni toplam ₺5.101,00"; satır sayısı 3; liste satırı ₺5.101,00; form sıfırlandı |
| Çekmecede 1. satır miktarı 2 → 3 | PATCH 200, toplam 6.101,00 | bant "Fatura kalemi güncellendi; belge toplamı yeniden hesaplandı."; çekmece ve liste ₺6.101,00 |
| Satırsız taslakta Düzenle | ipucu var, ekleme yok | `#degismez-ipucu-FaturaSatirsizTaslak` metni doğru, `#fd-kalem-yeni-ekle` yok |
| Kesilmiş fatura | Düzenle yok; kalem hücresi yok; yol metni | `fk-duzenle-…` yok; kalem penceresinde `si-fatura-kalem-*` 0; `degismez-ipucu-FaturaKalemi` "Yanlış faturayı iptal edin … yeni bir fatura kesin" |

### 8.3 SQL kanıtı (son kodla ikinci kiracı, tarayıcıdan girilen veri)

```
faturalar        TASLAK  fatura_no NULL  toplam 6101.00  kdv 1016.83  ara 5084.17
fatura_kalemleri 1 Q92 bilezik      3.000  1000.00  isc NULL    satir 3000.00  kdv 500.00
                 2 Q92 kolye        1.000   500.00  isc NULL    satir  500.00  kdv  83.33
                 3 Q92 canli yuzuk  2.000  1250.50  isc 100.00  satir 2601.00  kdv 433.50
denetim_kayitlari EKLEME Fatura 1 · GUNCELLEME Fatura 2 · EKLEME FaturaKalemi 3 · GUNCELLEME FaturaKalemi 1
```

Başlık toplamı = 3000 + 500 + 2601 = 6101. KDV = 500 + 83,33 + 433,50 = 1016,83. Taslak numara tüketmedi. İlk kiracıda da aynı sonuç ölçüldü (EKLEME FaturaKalemi 4 = 3 + 1).

### 8.4 Gerçek tarayıcı ölçüm tablosu (sikisma-olc.js ölçütleri, son kod)
Ölçülenler: sayfa (`main`, çekmece kapalı) ve çekmece (`#cekmece-fatura` kökü, satırlar + yeni satır formu).

Çekmece ölçümünde gövdenin kaydırma alanının dışında kalan öğeler "binen" sayılmadı: `getBoundingClientRect` kırpmayı görmez, yani bunlar yanlış pozitiftir. Ölçüme ek olarak şunlar sayıldı: gerçek tek kelime kırılması (Range satır sayısı), `.fd-cekmece-kalem` kutusunda kenara yapışan metin, çekmece gövdesinin yatay taşması.

| Genişlik | Mod | Tema | Kök | taşan | binen | tek kelime kırık | yatay kaydırma (sayfa/gövde) | [hidden] görünen | 'TRY' kalıbı | kenara yapışık | en geniş blok |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1280×800 | Profesyonel | koyu | sayfa / çekmece | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | %95 / %100 |
| 1280×800 | Basit | koyu | sayfa / çekmece | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | %95 / %100 |
| 1280×800 | Basit | açık | sayfa / çekmece | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | %95 / %100 |
| 1280×800 | Profesyonel | açık | sayfa / çekmece | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | %95 / %100 |
| 375×812 | Basit | açık | sayfa / çekmece | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | %94 / %100 |
| 375×812 | Profesyonel | açık | sayfa / çekmece | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | %94 / %100 |
| 375×812 | Basit | koyu | sayfa / çekmece | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | %94 / %100 |
| 375×812 | Profesyonel | koyu | sayfa / çekmece | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | %94 / %100 |

- Ham betiğin "kırık" sayacında 1280'de kalanlar çok kelimeli sarmalardır, tek kelime kırılması değildir: `th "ARA TOPLAM"` ve `#fk-no-yok-… "Numara kesimde verilir"`. İkisi de liste tablosunun mevcut hücreleridir, bu pakette dokunulmadı.
- Ara ölçümde bulunan gerçek kusur (`fd-kalem-bolumu-baslik ⟂ ipucu`, binen 1) düzeltildi ve yeniden ölçüldü (§ 3.5).

**Ekran görüntüleri** (bu oturumda, tab-26/tab-28):
- (a) Değişiklikten önce Düzenle çekmecesi: satır bölümü yok, "kalemleri düzenleyin" ipucu var.
- (b) 1280 koyu, boş gönderim sonrası alan altı mesajlar.
- (c) 1280 koyu, "Satır eklendi: 3. satır · yeni toplam ₺5.101,00" bandı + 3 satır.
- (d) 1280 koyu, düzeltme sonrası "Fatura kalemi güncellendi" + ₺6.101,00.
- (e) 1280 koyu, satırsız taslakta gerekçe kutusu.
- (f) 1280 koyu ve açık, son sürüm satır kutuları ve yeni satır formu (önizleme ₺2.601,00, TRY eki).
- (g) 375 açık, satır kutuları.
- (h) 375 koyu, yeni satır formu.

### 8.5 Temizlik
- API ve vekil durduruldu: `lsof :5174` = boş, `:5173` = boş. Sekmeler kapatıldı, görüntü alanı `desktop`'a döndü.
- İki test kiracısı `kiraci-temizle.sh --uygula` ile silindi: `01M392GC55JQFP76DKYPXTX18M` ve `01M394QY0WY47ZDRZ63CJ5XBRB` (`PAKETQ9-2-TEST`). `tenant_id` kolonlu 70 tablonun hepsinde bu iki kiracıya ait satır sayısı **0** (UNION ölçümü). Davet satırları `Q92DAVET…` silindi (0).
- **tenants:** Başlangıçta 11'di; bu sayıya başka şeridin `PAKETS1-h-TEST`'i dahildi. O şerit kendi kiracısını sildi, ölçülen 10 oldu. İkinci kiracımdan önce 11, sildikten sonra **11**. Kendi kiracılarımın ikisi de 0. QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.
- Fikstür ve jeton dosyaları `printf '' >` ile boşaltıldı.
- ⚠ Not: scratchpad başka şeritlerle ortak. İlk adımda kök dizine `api.py`, `api.pid`, `vekil.pid`, `api-5174.log` yazdım (sonra `q92/` alt dizinine geçtim). Aynı adlı dosyaları başka bir şerit kullanıyorduysa üzerine yazılmış olabilir; `api-5174.log` Q9-1'in günlüğü olabilir.

## 9. Değişen dosyalar (bu paket)
- `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` (M): `TaslakKalemEkle` servisi + POST ucu
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor` (M): çekmecede Satırlar bölümü + yeni satır formu
- `dotnet/Kuyumcu.Contracts/Sayfalar/FaturalarSozlesmeleri.cs`: `FaturaVeri.ParaBirimi` (varsayılanlı)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.FaturaTaslak.cs`: 13 yeni anahtar TR+EN
- `dotnet/Kuyumcu.Tests/Q92FaturaKalemEklemeHttpTestleri.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/Q92FaturaKalemEkranTestleri.cs` (YENİ)
- `dokumanlar/09-uygulama-kayitlari/PAKET-Q9-2-uygulama-raporu.md` (bu rapor)

`git status --porcelain | grep -v /(bin|obj)/` çıktısında Faturalar.razor ve FaturaModulu.cs `M` görünür. Diğerleri depoda izlenmeyen dizinlerde (`??`). Kalan `??` fatura dosyaları önceki paketlerindir.

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Faturalar.razor`. Başka razor sayfası açılmadı. S1-h/S1-i dosyalarına ve app.css'e dokunulmadı.

## 10. DoD sözleşmesi

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Fatura kalemi (TASLAK): ekleme | `POST /faturalar/{id}/kalemler` · `[RolGerekli(SAHIP,MUDUR,MUHASEBE)]` + `[ModulYetkisi("fatura")]` · ekranda Düzenle çekmecesi → "Yeni satır ekle" | Denetim izi otomatik (EKLEME FaturaKalemi) |
| Fatura kalemi (TASLAK): düzeltme | `PATCH /faturalar/{id}/kalemler/{kalemId}` (P-54) · çekmecede ve kalem penceresinde `SatirIciDuzenle` | Gönderilmeyen alan korunur, null gelen alan silinir (işçilik/indirim). P-54 testleri geçerli |
| Fatura kalemi (TASLAK): silme | **Uç yok (sunucu kuralı P-22)** · `DegismezIpucu FaturaKalemSilme` sebep + yol (hücreyi düzelt / İptal Et + yeniden kes) | Finansal belge satırında hard DELETE yasak |
| Satırsız (BASLIK) taslak | Toplam başlıktan düzeltilir (P-53) · satır eklenmez: `DegismezIpucu FaturaSatirsizTaslak` | Elle toplam sessizce ezilmez |
| Kesilmiş/ödenmiş/iptal fatura | Değişmez (400) · `DegismezIpucu FaturaKalemi` + liste `DegismezIpucu Fatura` | Yol: iptal + yeni belge |

Yeni düğmenin ("Satırı ekle") tıklaması gerçek POST atar ya da istemci doğrulamasıyla alan altında mesaj yazar; kaydederken disabled olur ve sebep yazar. Yeni varlık türü yok.

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Bulgu önce kendi ölçümümle üretildi | ✓ | § 1 (DOM + 404) |
| Taslakta kalem EKLEME | ✓ | HTTP `Q92_taslaga_satir_EKLENIR_…`; tarayıcı § 8.2; SQL § 8.3 |
| Taslakta kalem DÜZELTME (Düzenle kipinde) | ✓ | `Q92_cekmecede_satir_DUZELTILIR_…`; tarayıcı 2 → 3 |
| Toplam yenilenir (SQL) | ✓ | 5101 → 6101, başlık = Σ satır |
| Kaldırma: uç yoksa gerekçeli yol | ✓ | DegismezIpucu FaturaKalemSilme (çekmecede de) |
| Kesilmiş faturada kilit + doğru yol metni | ✓ | HTTP 400 metni; tarayıcı § 8.2; `Q92_KESILMIS_…` ×2 |
| Mevzuat kilidi açılmadı, hüküm yazılmadı | ✓ | § 4 |
| Tek yazma yolu atlanmadı | ✓ | Hareket yazılmıyor; `Hareketler.Add` 1 |
| DOM id / rota değişmedi | ✓ | yalnız yeni `fd-kalem-*` id'leri |
| Tek düzenleme kalıbı | ✓ | ModalPencere 48 → 48, `duzenlenenId` 1/1, SatirIciDuzenle + Cekmece |
| Form: boş/geçersizde alan altı mesaj | ✓ | `Q92_bos_…`, `Q92_sunucu_400_…`; tarayıcı |
| Her düğme istek atar ya da disabled + sebep | ✓ | `Q92_Ekle_dugmesi_…` |
| UX-S21/22/23/25/26/27/28/29/30/31/32 | ✓ | KDV belgeden; önizleme; gerekçeler; aynı alan kümesi; sonuç bandı; hata ≠ boş; 200 sınırı; kilit sebebi; sözlük; para birimi |
| Sözlük HUB'a dokunulmadı, TR+EN | ✓ | `Sozlukler.FaturaTaslak.cs` |
| app.css / S1-h/i dosyaları | ✓ dokunulmadı | § 9 |
| Gerçek tarayıcı 1280/375 × basit/pro × açık/koyu | ✓ | § 8.4 tablo + ekran görüntüleri |
| Test + mutasyon | ✓ | 18 test, 18/18 mutasyon kırmızı |
| Build 0 hata | ✓ | son build 0 hata. Tek uyarı başka dosyada (§ 12) |
| Tam süit: bu paket yeni kırmızı üretmedi | ✓ | 5072/5072 |
| denetim.sh | ✓ TEMİZ | § 7 |
| Test kiracısı silindi | ✓ | § 8.5 |

## 12. Açık kalanlar
1. **Derleme uyarısı (başka dosya):** `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031` test projesi yeniden derlendiğinde çıkıyor. Sahibi etiket ekranı şeridi (Q9-1 raporunda da kayıtlı). Bu paketin dosyası değil.
2. **Ortak Sayi/Para bileşeni davranışı (değiştirilmedi):** Kullanıcı geçerli bir değerden sonra geçersiz metin yazarsa bileşen alan altında "sayı anlaşılmadı" yazar ama `Deger` son geçerli değerde kalır. Bu durumda "Satırı ekle" son geçerli değerle gönderilir. Tüm ekranlarda aynı; düzeltilecekse Bilesenler/Form şeridinde yapılmalı.
3. **Taslak kalem PATCH'inde (P-54, değiştirilmedi):** Miktar değişince KUYUM başlığının `HasGram`/`HasBirimFiyat` kanıt alanları tazelenmiyor. Ekleme yolunda tazeleniyor. Ayrı küçük paket adayı (Taşkın).
4. **Satır silme/pasifleştirme** istenirse ayrı paket gerekir: şema + okuyucuların süzmesi (§ 3.2).
5. `SatirIciDuzenle` tetikleyicisi kısa değerde (ör. "2") dar bir tıklama alanı (~28 px) oluşturuyor. Ortak bileşen olduğu için dokunulmadı. Mobil dokunma hedefi olarak ayrıca ölçülmeli.
6. Mali müşavir notu: § 4 (taslağa sonradan eklenen KUYUM satırının kuru).
