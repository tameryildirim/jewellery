# PAKET P-61 — Satır eylemleri taşma menüsü + yoğun tabloların yoğunluk ayarı — UYGULAMA RAPORU

- Şerit: SERİT G (tur 8 triyajı) · izole test vt `kuyumcu_test_g` · canlı port 5182
- Rol: Taşkın (backend) + Ertuğ (frontend) · Tarih: 2026-09-24
- Durum: **BİTTİ** (şerit kapısının 7 maddesi sağlandı, aşağıda kanıtlarıyla)

## 1. Spesifikasyon ↔ kod doğrulaması (sapmalar ve gerekçeleri)

| Spesifikasyondaki "mevcut durum" | Ölçülen gerçek (24.09) | Karar |
|---|---|---|
| `/cariler` 8 düğme, satır 305 px | S3-2 zaten "2 görünür + ⋯" kalıbını **elle** yazmıştı (yerli `popover`). Ölçüm: satır **74 px**, 1280x800'de **6** satır görünüyor (AD sütunu 114 px'e sıkışıp adı 3 satıra bölüyordu) | Kalıp ortak bileşene taşındı, id'ler aynı kaldı. Yoğunluk CSS'i eklendi: **55 px, 8 satır** |
| `/urunler` 4'ten çok eylem | S3-3 aynı kalıbı elle yazmıştı | Ortak bileşene taşındı (id'ler aynı) |
| `/faturalar` FATURA NO ve EYLEMLER 1280 px'te kırılıyor | DOĞRULANDI: TASLAK satırda 4 düğme, eylem sütunu 274 px, tablo **1118/962 px** (yatay taşma) | Dar sütun kipi + numara tek satır → **962/962** |
| `/siparisler` 4'ten çok eylem | **YANLIŞ**: satırda tek eylem var (`sip-detay-*` bağlantısı, P-06 düzenleme yolu) | **Dokunulmadı.** Taşma menüsü gerekmiyor. Dosya ayak izinde olduğu için rapora yazıldı |
| `/gider-pusulalari` satırda "Düzenle" yok | **YANLIŞ**: P-55 "Düzenle"yi (`gp-duzenle-*`) zaten eklemiş. Satırda 3 eylem var, bir hücrede `style="white-space:nowrap"` satır içi stil duruyordu | Ortak bileşene bağlandı (3 eylem, hepsi satırda). Satır içi stil kaldırıldı. Yeni id'ler: `gp-detay-*`, `gp-yazdir-*`. Düzenle eylemi test ve mutasyonla korunuyor |
| `/yedekler` satırda Düzenle | SERİT E'nin dosyası | **DEVREDİLDİ**: `Yedekler.razor` ve `YedekEkranTestleri` dosyalarına dokunulmadı |
| `SatirIciDuzenle.razor` önce denetlenecek | Denetlendi. TEK ALANLI değer düzeltmesi yapıyor (girdi + Kaydet/İptal); satırın eylem kümesini taşımıyor. `DuzenlenebilirListe` hiçbir sayfada kullanılmıyor | Yeni bileşen `SatirEylemleri.razor` gerekçeli. S3-2/S3-3/S3-6'daki üç elle yazılmış kopyadan ikisi tek kaynağa toplandı. Kasalar SERİT dışında kaldı |

Yasal/mevzuat hükmü yok. Migration yok. Sunucu ucu değişmedi (yalnız istemci işi).

## 2. Yapılanlar

- **`Bilesenler/SatirEylemleri.razor` (YENİ)**
  - Sıralı `Eylem` listesi alır.
  - Eylem sayısı 3'ten fazlaysa ilk `BirincilSinir` (=2) eylem satırda görünür, kalanlar "⋯" penceresine gider. 3 veya daha az eylemde hepsi satırda kalır, menü çizilmez.
  - İsteğe bağlı `Birincil` değeri `BirincilSinir` ile sınırlıdır ve onu aşamaz. `DarSutun` kipi, birincili aşan eylemi her zaman taşır.
  - Yerli HTML `popover="auto"` kullanılır: JS yok, CSP istisnası yok. Tablo sarmalayıcısının `overflow` ayarı menüyü kırpmaz.
  - "⋯" düğmesi gerçek bir `<button type=button>`; `aria-haspopup="dialog"`, `aria-controls` ve `aria-expanded` taşır.
  - `aria-expanded` değeri pencerenin yerli `toggle` olayından türetilir. Blazor `ontoggle` olayını yakalama evresinde dinler. Böylece Esc, dışarı tıklama, öğe seçimi ve Kapat aynı olaydan geçer.
  - Eski id/öznitelik sözleşmesi aynen korunur: `{önek}-eylem-*`, `{önek}-menu-ac-*`, `{önek}-menu-*`, `{önek}-menu-kapat-*`, `popovertargetaction="hide"`.
  - `Eylem` bir record değil, sınıftır. Nedeni denetim.sh'taki "istemcide yeni record yazılmaz" kuralı (tavan 3); ilk sürüm record olduğu için bu sayaç kırılmıştı, düzeltildi.
- **`Kuyumcu.Core/I18n/Sozlukler.SatirEylemleri.cs` (YENİ, TR+EN)**: Ortak hub olan `Sozlukler.cs` dosyasına dokunulmadı.
- **`Pages/Cariler.razor`**
  - Elle yazılmış S3-2 kalıbı `<SatirEylemleri Onek="c">` + `CariEylemleri(c)` ile değiştirildi. Sıra ve id'ler aynı.
  - Döviz kilidinin yazılı sebebi (`c-doviz-kilit-*`) `MenuEk` üzerinden taşınıyor.
  - Tabloya `tablo-sik tablo-sik--kimlik` sınıfı eklendi.
- **`Pages/Urunler.razor`**: S3-3 kalıbı `<SatirEylemleri Onek="u">` + `UrunEylemleri(u)` ile değiştirildi. Tabloya `tablo-sik` eklendi.
- **`Pages/Faturalar.razor`**
  - `<SatirEylemleri Onek="fk" Birincil="1" DarSutun="true">` kullanılıyor. Satırda durumun tek birincil eylemi görünür:
    - TASLAK → Düzenle
    - KESİLDİ → Ödendi işaretle
    - diğer durumlar → Kalemleri göster
  - Kes ve İptal pencereye alındı; onay modalı aynen duruyor (DoD EK 4).
  - Numara `<span class="hucre-tek-satir" id="fk-no-*">` içinde, tarih hücresi tek satır.
  - Neden dar sütun: 10 sütunlu tabloda diğer 8 sütunun en dar hâli yaklaşık 620 px ölçüldü. Numara ve eylem için yaklaşık 340 px kalıyor.
- **`Pages/GiderPusulalari.razor`**: `<SatirEylemleri Onek="gp">` ile Detay, Düzenle ve Yazdır satırda. Satır içi `style` kaldırıldı. Tabloya `tablo-sik` eklendi.
- **`wwwroot/css/app.css`**: Yalnız dosya sonuna, belirteçli tek P-61 bloğu eklendi; mevcut kurallar değişmedi, sabit renk yok.
  - `.satir-eylem > .btn { nowrap }`
  - `.tablo-sik`: dikey ve yan boşluk `--bosluk-2`
  - `.tablo-sik--kimlik`: ilk sütuna 10rem taban
  - `.tablo-sik .satir-eylem`: aralık `--bosluk-1`
  - `.hucre-tek-satir`
- Sahte API: Yeni uç yok. Yeni test dosyasındaki sahte API her uca gerçekçi yanıt döndürüyor.

### Dokunulan sayfalar (DoD EK 2 — tek dokunuş)
`Pages/Cariler.razor`, `Pages/Urunler.razor`, `Pages/Faturalar.razor`, `Pages/GiderPusulalari.razor`.
Dokunulmayanlar: `Siparisler.razor` (gerek yok) ve `Yedekler.razor` (SERİT E'ye devredildi).

## 3. DoD sözleşmesi

Yeni varlık veya kayıt türü yok. Bileşen yalnız mevcut eylemleri yeniden yerleştiriyor, hiçbir eylem silinmedi.

| Ekran | Düzenleme / pasife alma yolu | Not |
|---|---|---|
| /cariler | Düzenle görünür; Pasife al "⋯" içinde, onay modalı var; pasif satırda "Aktife al" görünür | Değişmedi (S3-2), yer tek kaynağa taşındı |
| /urunler | Düzenle görünür; Pasife al "⋯" içinde; pasif satırda Aktife al | Değişmedi (S3-3) |
| /faturalar | TASLAK: Düzenle görünür. Kes, İptal ve Ödendi onaylı; KESİLMİŞ belge için DegismezIpucu | Finansal belge: hard DELETE yok (değişmedi) |
| /gider-pusulalari | Düzenle görünür (PATCH adres + mal açıklaması); iptal çekmecede | Vergi belgesi silinmez (değişmedi) |

Her düğme ya gerçek bir istek atar ya da `disabled` + `title` sebebi taşır. "⋯" düğmesi yerli pencere açar; S3-2/S3-6 ile aynı istisna.

## 4. Testler (yeni: 15 test örneği)

| Test | Kapsam |
|---|---|
| `SatirEylemleriTestleri.UctenFazlaEylem_TasmaMenusuneDuser` (4/6/9) | 2 görünür + ⋯, taşanlar sırasıyla pencerede, her öğe `hide` ve gerçek iş |
| `SatirEylemleriTestleri.UcVeAzEylem_HepsiSatirda_MenuYok` (1/2/3) | Tek öğeli menü yok |
| `SatirEylemleriTestleri.TasmaMenusu_KlavyeIleAcilirVeEscKapatir` | button/type, aria-controls/haspopup/label, `popover=auto`, toggle → aria-expanded true/false, öğe seçimi |
| `SatirEylemleriTestleri.DarSutun_TekBirincil_GerisiTasar` (1/2/4) | Dar sütun kipi |
| `SatirEylemleriTestleri.Birincil_ust_siniri_asamaz` | `Birincil=7` → yine 2 |
| `SatirEylemleriTestleri.KilitliEylem_disabled_ve_sebebi_yazili` | disabled + title, tehlike sınıfı, aria-label |
| `CariEkranTestleri.SatirdaGorunurEylem_EnFazlaUc` | Her satırda ≤3 görünür, ⋯ aria-expanded=false, pencerede ≥4 eylem, dışarıda başıboş düğme yok |
| `GiderPusulaEkranTestleri.Satirda_DuzenleEylemiVar` (YENİ dosya) | Düzenle satırda, etkin, adlı; Detay/Yazdır duruyor; eylem hücresinde style yok; tıklama → GET + çekmece |
| `FaturaEkranTestleri.TaslakSatir_GorunurEylem_EnFazlaUc_KesIptalMenude` | TASLAK: [Düzenle, ⋯]; pencere [Göster, Kes, İptal, Kapat]; Kes → onay modalı |
| `YedekEkranTestleri.Satirda_DuzenleEylemiVar` | **YAZILMADI — SERİT E'ye devredildi** (Yedekler.razor bu şeridin dışında) |

**Filtreli koşumlar (son temiz derlemeden sonra):**
- P-61 testleri ile S3-2/S3-3 (S32/S33), Pusula (P-55), Fatura (P-53/P-54), Sipariş (P-52), Ürün, UX1C3, SozlesmeYansima ve OlcumKapisi testleri birlikte koşuldu: **262/262 geçti, 0 başarısız.**

**Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_g`, trx: `scratchpad/trx-pP-61/tam3-2.trx`):
- Önce: 4931, sonra: 4946 (+15).
- Sonuç: **4944 geçti, 2 başarısız.**
  - `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` — "Unknown database 'kuyumcu_t1_eski'"
  - `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` — "Unknown database 'kuyumcu_t1_hazir'"
  - İkisi de sabit adlı ortak vt kullanıyor; paralel şerit aynı anda silip kuruyor. Tek başına tekrar koşuldu: **3/3 geçti** (Emanet kırmızısı dahil). Bu paketin ürünü değil.
- Önceki koşum (`_Mac_2026-09-24_06_09_28.trx`) 4 kırmızı gösterdi:
  - 3'ü bu paketin ilk sürümündeki record yüzündendi: `SozlesmeYansima...record_tanimi`, `OlcumKapisi.SAYFA_RECORD_TAVAN...`, `OlcumKapisi.P02_blogundaki...`. Record → sınıf ile düzeltildi; sonraki koşumlarda yeşil.
  - 4'üncüsü `KilitGerekceTestleri.Emanet_kilitli_dugmeler_sebebini_yazar` (SERİT F alanı). Tek başına koşumda yeşil.
- Bir ara koşum 832 "başarısız" verdi. Hepsi `FileNotFoundException` ve `deps.json`: paralel derleme bin'i sildi. **Kanıt sayılmadı**, yeniden koşuldu.

**Bu paketin yeni kırmızısı: 0.**

## 5. Mutasyon kanıtları

Yöntem: yedek → `perl -0777 -pi` ile tek eşleşme → `--no-incremental` derleme → filtreli test → yedeği geri kopyala → `cmp`.

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| M61-1 | `BirincilSinir = 2` → `8` | `SatirEylemleriTestleri.UctenFazlaEylem_TasmaMenusuneDuser` (4/6/9), `CariEkranTestleri.SatirdaGorunurEylem_EnFazlaUc`, `S32_01` (BASIT+PRO), `S32_02`, `S32_03`, `S33_01` (2 mod), `S33_02`, `S33_03`, `TasmaMenusu_KlavyeIleAcilirVeEscKapatir`, `Birincil_ust_siniri_asamaz` (14 kırmızı). Record→sınıf sonrası **tekrar koşuldu**: 4/4 hedef kırmızı | cmp BİREBİR (iki kez) |
| M61-2 | GiderPusulalari.razor'dan Düzenle eylemi kaldırıldı | `GiderPusulaEkranTestleri.Satirda_DuzenleEylemiVar` (+ `PusulaEkranTestleri` 4 test) | cmp BİREBİR |
| M61-3 | `Degisti()` gövdesi boşaltıldı (aria-expanded izlenmiyor) | `SatirEylemleriTestleri.TasmaMenusu_KlavyeIleAcilirVeEscKapatir` | cmp BİREBİR |
| M61-4 | Faturalar'dan `Birincil="1" DarSutun="true"` kaldırıldı | `FaturaEkranTestleri.TaslakSatir_GorunurEylem_EnFazlaUc_KesIptalMenude` | cmp BİREBİR |

Son mutasyondan sonra temiz derleme alındı: `dotnet build Kuyumcu.sln` **0 hata**. Tek uyarı (xUnit1031) `EtiketEkranTestleri.cs:145` içinde; P-66 şeridinin dosyası, bu paketin değil.

## 6. denetim.sh
`DENETİM TEMİZ.` (286 ✓).
- İlk koşumda bu paketin record'u iki sayacı kırmıştı: "Pages+Bilesenler+Layout record tanımı 4 (tavan 3)" ve "P-02 OLCUM_TABANI_KOPYA". Düzeltildi.
- Satır içi `style` sayısı 1 azaldı (GiderPusulalari). ModalPencere sayısı artmadı.

## 7. Canlı doğrulama (port 5182, `kuyumcu_net`)

Kiracı: `PAKETP-61-TEST Kuyumculuk` = `01M38MMVCFGMSWW78QETEGNEPC`. Tek kullanımlık davetle `POST /auth/kayit` → **201**.

Tarayıcı ölçümü aynı kökenli bir yerel ölçüm vekiliyle (127.0.0.1:5310 → 5182) yapıldı. Böylece jeton hiçbir çıktıya yazılmadı. Görünüm 1280x800.

| Adım | Beklenen | Gerçek |
|---|---|---|
| 12 cari `POST /cari` | 201 | 12 × **201** |
| /cariler satır yüksekliği | ≤ 96 px | **49–55 px** (önce 74 px) |
| /cariler 1280x800 görünür satır | ≥ 8 | **8** (önce 6); yatay taşma yok: 962/962 |
| Satırda görünen düğme | ≤ 3 | **3** (Tahsilat, Düzenle, ⋯) |
| ⋯'e odak + **Enter** | pencere açık, aria-expanded=true | `:popover-open`=true, **aria-expanded="true"** |
| **Esc** | kapanır, aria-expanded=false, odak düğmede | kapandı, **"false"**, odak `c-menu-ac-*` |
| Enter → **Tab** → Enter (Detay) | odak pencereye geçer, öğe işini yapar, pencere kapanır | odak `c-detay-*`; pencere kapandı, aria "false", Detay modalı açıldı |
| Dışarı tıklama | kapanır, aria false | kapandı, **"false"** |
| 3 fatura `POST /faturalar` | 200 | 3 × **200**; 2'si `PATCH …/durum` → **200** (KESILDI, ODENDI) |
| /faturalar 1280 px | numara ve eylem kırılmaz, yatay taşma yok | tablo **962/962** (önce 1118/962); `FTR-2026-00000x` tek satır (17 px); eylem tek satır (28 px): `Ödendi İşaretle+⋯`, `Kalemleri Göster+⋯`, `Düzenle+⋯` |
| TASLAK ⋯ → Kes → onay → Onayla | onay modalı, sonra KESİLDİ | pencere [Kalemleri Göster, Kes, İptal Et, Kapat]; onay metni göründü; bant "…Kesildi durumuna geçirildi"; satır `FTR-2026-000003 / Kesildi` |
| Kasa açılışı + 2 hurda alış | gider pusulası doğar | kasa hareketi **200**; hurda alış **200** × 2 → `GP-2026-000001/2` |
| /gider-pusulalari satırı | Detay, Düzenle, Yazdır satırda | `gp-detay`, `gp-duzenle`, `gp-yazdir`; 55 px; 962/962 |
| Düzenle → adres değiştir → Kaydet | PATCH ve başarı bandı | bant "Pusula güncellendi. (GP-2026-000002)" |
| /urunler (9 ürün, `POST /urunler` 9 × **201**) | ≤3 görünür, yatay taşma yok | `Stok+Düzenle+⋯`, **962/962**, satır 55 px (kimlik tabanı ürünlerde kaldırıldıktan sonra; önce 74 px) |

SQL kanıtı (`kuyumcu_net`, kiracı süzgeçli):
```
select pusula_no, satici_adres from gider_pusulalari where tenant_id='01M38MMVCFGMSWW78QETEGNEPC';
GP-2026-000001  Merkez mah. 1
GP-2026-000002  P61 canli duzenleme adresi      ← ekrandaki Düzenle ile yazıldı
select fatura_no, durum from faturalar where tenant_id='…';   (Kes öncesi)
NULL TASLAK · FTR-2026-000002 ODENDI · FTR-2026-000001 KESILDI
select count(*) from cariler where tenant_id='…';  → 12
```

**Temizlik**
- API (PID 61533) ve vekil (5310) `kill` ile durduruldu. `lsof` 5182 ve 5310 **boş**.
- Test kiracısının satırları `tenant_id` sütunu olan **70 tablodan** silindi (sonrasında hepsinde 0). Davet ve `tenants` satırı da silindi.
- `tenants` = **10** (başlangıç 10). `QA-TEST-%` = **4** (dokunulmadı). `PAKETP-61%` = 0.
- Scratchpad'deki geçici jeton/parola dosyaları `printf ''` ile boşaltıldı.

## 8. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"` süzülmüş, yalnız bu paketin dosyaları)
```
 M dotnet/Kuyumcu.Web/Pages/Cariler.razor
 M dotnet/Kuyumcu.Web/Pages/Faturalar.razor
 M dotnet/Kuyumcu.Web/Pages/Urunler.razor
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css
?? dotnet/Kuyumcu.Web/Bilesenler/SatirEylemleri.razor
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.SatirEylemleri.cs
   dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor        (dosya zaten izlenmiyor — ?? — P-61 düzenledi)
   dotnet/Kuyumcu.Tests/SatirEylemleriTestleri.cs         (YENİ; Tests dizini git'te izlenmiyor)
   dotnet/Kuyumcu.Tests/GiderPusulaEkranTestleri.cs       (YENİ)
   dotnet/Kuyumcu.Tests/CariEkranTestleri.cs              (1 test eklendi)
   dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs            (1 test eklendi)
```
Commit yapılmadı.

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| /cariler satır ≤ 96 px | KARŞILANDI | canlı 49–55 px |
| 1280x800'de ≥ 8 cari satırı | KARŞILANDI | canlı 8 |
| Satırda görünür eylem ≤ 3 | KARŞILANDI | bUnit + canlı (cariler, ürünler 3; faturalar 2; pusula 3) |
| Taşma menüsü klavyeyle açılıp kapanıyor, aria-expanded doğru | KARŞILANDI | canlı Enter/Esc/Tab/dışarı tıklama + bUnit + M61-3 |
| /gider-pusulalari satırında Düzenle | KARŞILANDI | test + M61-2 + canlı PATCH ve SQL |
| /yedekler satırında Düzenle | DEVREDİLDİ (SERİT E) | dosya sınırı gereği dokunulmadı |
| /faturalar 1280 px'te kırılmasız | KARŞILANDI | 1118→962/962 px, numara ve eylem tek satır |
| dotnet build 0 hata | KARŞILANDI | 0 hata (1 uyarı başka şeridin dosyasında) |
| Paketin testleri 0 başarısız | KARŞILANDI | 262/262 |
| denetim.sh TEMİZ | KARŞILANDI | DENETİM TEMİZ |
| Paketin yeni kırmızısı 0 | KARŞILANDI | tam süit 4944/4946; 2 kırmızı ortak vt yarışı, tek başına yeşil |

## 10. Açık kalanlar
1. **YedekEkranTestleri.Satirda_DuzenleEylemiVar + /yedekler satır eylemleri** → SERİT E'ye devredildi. `SatirEylemleri` bileşeni hazır: `<SatirEylemleri Onek="…" Eylemler="…"/>`.
2. **Kasalar.razor** (S3-6) hâlâ elle yazılmış ⋯ kalıbı taşıyor. Ortak bileşene taşınması şerit dışı; öneri olarak kalıyor.
3. `MigrationHazirlikTestleri` ve `YayinAcilisHttpTestleri` sabit adlı ortak vt (`kuyumcu_t1_*`) kullanıyor. Paralel şeritlerde yarış yaratıyor → **orkestratör**. `KUYUMCU_TEST_DB` önekine bağlanmalı.
4. Faturalar'da durum değişikliği başarı bandı fatura **kimliğini** yazıyor ("01M… faturası Kesildi…"); fatura numarası yerine (UX-S27). P-53/P-54 alanı, bu paketin değişikliği değil → Faturalar sahibine.
5. `aria-expanded` yerli `toggle` olayını sayarak çevriliyor. Tarayıcı aynı görevde aç-kapa yaparsa iki olayı tek olaya birleştirebilir; bu yalnız programatik aç-kapada olur, kullanıcı etkileşiminde canlıda sapma görülmedi. Gerekirse `ToggleEvent.newState` için özel olay tipi ayrı bir iş olarak değerlendirilebilir.

## 11. Kurucuya / mali müşavire notlar
Mevzuat hükmü yok; mali müşavir teyidi gerekmez.
