# PAKET S-2 — Acil düzeltme: istemci/sunucu yanıt şekli uyumsuzlukları (uygulama raporu)

> Dalga 2c, ilk iş. **Uygulayan: Tamer Bey (orkestratör), elden.** Gerekçe Madde 1 ile aşağıda: paket üç kez alt ajana verildi, üçünde de ajan makine boğulması yüzünden "ilerleme yok" ile düştü (ajan yükü değil — biri yalnız 9 araç çağrısı yapıp dondu). Ölçüm: 2026-09-17 06:05, yük ortalaması 17,2 · `iCloudDriveCore` %85,8 CPU · boş bellek ~100 MB · tek `dotnet build` 29 dk 51 sn. Kalıcı çözüm kurucu kalemi 15 (proje klasörünün iCloud dışına alınması).

## 1. Kapsam ve sonuç

| # | Kalem | Durum |
|---|---|---|
| 1 | `/urunler` Şube stokları modalı açılışta `NullReferenceException` | **KAPANDI** |
| 2 | `/uyum` tam belge no aramasında `DeserializeUnableToConvertValue` | **ZATEN KAPALI** (UX-4d md. 18, dalga 2b) — kanıt aşağıda |
| 3 | `dotnet/denetim.sh` istemci model (record) sayacı | **KAPANDI** |
| — | 101 modellik geniş tarama | **AYRILDI** → paket **S-2T** (salt okunur rapor; kod değiştirmez) |

## 2. Madde 1 — Stok modalı NullReferenceException

**Kök neden (kanıt):** sunucu `UrunModulu.StokListele` (Kuyumcu.Api/Modules/Urun/UrunModulu.cs:659-680) zarf döndürüyor:
`{ urunId, toplamAdet, toplamHasGram, subeStoklar:[{subeId, adet, toplamHasGram, guncellendi}] }`.
İstemci ise `Urunler.razor:659` içinde `record StokYaniti(List<StokVeri> Stok)` ile **`stok`** alanını bekliyordu. Alan bulunamayınca `Stok` null kalıyor, `StokAc` içinde `stoklar = yanit.Stok` atanıyor ve modal çizilirken `stoklar.Count` üzerinde NRE fırlıyordu. Kullanıcı "Beklenmeyen bir hata oluştu" bandı görüyordu (QA turu 2, /urunler; iki kez yeniden üretildi).

**Düzeltme** (`Kuyumcu.Web/Pages/Urunler.razor`):

| Satır | Önce | Sonra |
|---|---|---|
| 661 | `record StokYaniti(List<StokVeri> Stok)` | `record StokYaniti(List<StokVeri>? SubeStoklar)` |
| 927 | `stoklar = yanit.Stok` | `stoklar = yanit.SubeStoklar ?? []` |

`StokVeri(string SubeId, int Adet, decimal ToplamHasGram)` alanları sunucuyla uyumlu; sunucunun ek `guncellendi` alanı istemcide kullanılmıyor (fazla alan JSON okumada sorun değil).

**Testin neden hatayı kaçırdığı (asıl bulgu):** `Kuyumcu.Tests/UrunEkranTestleri.cs` içindeki sahte API, gerçek sunucu şekli yerine `{"stok":[…]}` döndürüyordu. Yani test, üründe olmayan bir sözleşmeyi doğruluyordu. Sahte yanıt **gerçek şekle** çevrildi (zarf + `subeStoklar` + `guncellendi`).

**Yeni test:** `UrunEkranTestleri.Stok_modali_GERCEK_yanit_seklinde_satirlari_cizer_hata_vermez` — modal açılır, şube satırı ve adet çizilir, "Beklenmeyen" bandı yok, boş durum gösterilmez.

**Mutasyon kanıtı (M-S2-1):** alan adı `SubeStoklar` → `Stok` ve çağrı `yanit.Stok ?? []` yapıldı; koşum:

```
Başarısız! - Başarısız: 3, Başarılı: 20, Toplam: 23
  ✗ Stok_modali_GERCEK_yanit_seklinde_satirlari_cizer_hata_vermez
  ✗ Stok_modalinda_SAYIM_DUZELT_dugmesi_vardir
  ✗ Sayim_kaydedilince_dogru_SUBE_ucuna_PATCH_gider
```

Mutasyon geri alındı; koşum: `Başarılı! - Başarısız: 0, Başarılı: 23, Toplam: 23`. Geri alma kodla doğrulandı (satır 661 ve 927 mutasyon öncesi hâlinde).

## 3. Madde 2 — /uyum belge no araması (zaten kapalı)

`Kuyumcu.Web/Servisler/UyumServisi.cs:43` bugün zarfı okuyor:

```csharp
public Task<BelgeAramaSonucu> BelgeNoIleAra(string belgeNo)
    => api.GetAsync<BelgeAramaSonucu>($"/masak/kimlik-tespitleri/ara?belgeNo={Uri.EscapeDataString(belgeNo)}");
```

`BelgeAramaSonucu(int Adet, decimal ToplamTutarTl, DateTime? IlkIslem, DateTime? SonIslem, List<KimlikTespitSatir> Kayitlar)` — sunucunun `{adet, toplamTutarTl, ilkIslem, sonIslem, kayitlar}` zarfıyla birebir. Dosyadaki açıklama da düzeltmenin UX-4d madde 18 kapsamında yapıldığını söylüyor. **Bu kalem S-2 kapsamından düşer**; ekranda kalan bir sorun varsa QA turu 3 ölçümünde görülecek.

## 4. Madde 3 — denetim.sh istemci model sayacı (sözleşme dondurma)

MÜHÜR 5 KOŞUL 3 gereği ortak sözleşme projesi (SÖZLEŞME-1) 1.1'e ertelendi; yerine **yüzey dondurma** geldi. `dotnet/denetim.sh`:

- `WEB="Kuyumcu.Web"` değişkeni eklendi.
- Yeni sayaç: **"Pages içinde istemci record tanımı"**, tavan **122** (bugünkü ölçüm). Tavanı aşmak denetimi **kırar**.
- Gerekçe koda yorum olarak yazıldı: uyumsuz alan adı = çalışma zamanı hatası (Stok modalı NRE, /uyum zarfı).

Koşum: `✓ Pages içinde istemci record tanımı 122 (tavan 122)` → **DENETİM TEMİZ**.

## 5. Değişen dosyalar

- `dotnet/Kuyumcu.Web/Pages/Urunler.razor` (2 satır + açıklama)
- `dotnet/Kuyumcu.Tests/UrunEkranTestleri.cs` (sahte yanıt gerçek şekle; 1 yeni test)
- `dotnet/denetim.sh` (WEB değişkeni + record tavan sayacı)

## 6. Kanıtlar

| Kontrol | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln` | 0 hata (süre 29:51 — makine boğulması, § başlık) |
| `UrunEkranTestleri` (filtreli) | 23/23 yeşil |
| Mutasyon M-S2-1 | 3 test kırmızı → geri alındı → 23/23 yeşil |
| `dotnet/denetim.sh` | DENETİM TEMİZ (yeni sayaç dâhil) |
| Migration | Yok (şema değişmedi) |
| Test kiracısı | Açılmadı (canlı doğrulama bu pakette gerekmedi; ekran düzeltmesi bUnit ile kanıtlandı) |

## 7. Açık kalanlar / devir

- **Tam süit** bu raporda koşulmadı: makine yükü nedeniyle tam süit ~30 dk sürüyor ve dalga 2c'nin sonraki paketi zaten kapı öncesi tam süit koşacak. Kapı kaydı (`kapi.sh`) tek gerçek kaynaktır — S-2 tek başına "kapı geçti" iddiasında değildir.
- **Canlı doğrulama (5198):** yapılmadı; gerekçe: değişiklik yalnız istemci alan adı ve test sahtesi. QA turu 3 aynı ekranı 5199'daki yayında ölçecek; yayın dalga 2c kapısından sonra yenilenecek.
- **S-2T (tarama):** 101 istemci modelinin uç bazında karşılaştırılması ayrı pakette; çıktısı `YANIT-SEKLI-TARAMASI.md` ve bulguların UX-1-C1..C4'e eşlemesi.
- **Record tavanı:** 122 bugünün sayısıdır; UX-1-C paketleri yeni istemci record yazmayacak, gerekirse Kuyumcu.Core sözleşme tipleri kullanılacak.
