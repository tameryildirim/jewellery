# PAKET 35 — Kâr/zarar analizi derinleştirme ve kanıtlı hatalar — **FAZ A** uygulama raporu

> Tarih: 2026-09-17 · Dalga 3 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Model: Opus
> Kapsam: **yalnız Faz A** (üç kanıtlı ölçüm hatası + güvenilirliğin üç uca yayılması + dönem
> karşılaştırması). Faz B (kategori/şube/kullanıcı/reyon kırılımı) **sevk edilmedi** — ayrı sevk
> kararı spesifikasyonun "Faz A ve Faz B ayrı sevk edilecek" koşulu gereğidir.

---

## 1. ZORUNLU ÖLÇÜM (Bertan koşulu) — iddia kodda doğrulandı

`MaliyetMotoru.PartiIade` (`Kuyumcu.Api/Modules/Maliyet/MaliyetMotoru.cs`, İade bölümü) açıldı ve
ters tüketim satırı **birebir ölçüldü**:

| Ölçülen | Değer | Sonuç |
|---|---|---|
| `CikisHareketId` | `iadeHareketId` (ters kayıt **İADE** hareketine bağlanır) | iddia **DOĞRU** |
| `Adet` | `Math.Round(-geri, 3)` → **NEGATİF** | iddia **DOĞRU** |
| `BirimMaliyetTl` / `BirimMaliyetHas` | `n.BirimTl` / `n.BirimHas` = **orijinal partininki** | iddia **DOĞRU** |

Yani düzeltme ters yöne çalışmıyor: iade kimlikleri maliyet kümesine alınınca negatif satır
maliyeti **geri çıkarır** ve bu maliyet **çıktığı** birim maliyettir.

## 2. REGRESYON ÖNCE — karakterizasyon ölçümü (paket ÖNCESİ gerçek çıktı)

Kod değiştirilmeden önce iki fikstür koşuldu ve çıktı donduruldu (ham ölçüm çıktısı):

```
OLCUM hasilatTl=6000,00 maliyetTl=4000,00 karTl=2000,00 satisAdedi=1 hasilatHas=0,8788
   (4 adet satış 6.000 TL + 2 adet iade 3.000 TL + 500 TL işçilik)
OLCUM hasilatTl=2000,00 maliyetTl=1000,00 karTl=1000,00 satisAdedi=1
   (takas farkı +2.000 TL, verilen malın maliyeti 1.000 TL)
```

Yani paket öncesi: **iade hiç görünmüyordu** (tamamı iade edilen mal tam kâr yazıyordu),
**takas farkı ciro sayılıyordu**, **işçilik hiç yoktu**. Bu ölçüm `KarZararIadeTestleri.cs`
dosyasının başına yorum olarak yazıldı; yeni testlerin rakamları onunla karşılaştırılabilir
("düzeltme mi bozma mı" sorusu bu yüzden cevaplanabilir).

## 3. Yapılanlar

### 3.1 `KarZararServisi.Donem`
- Hasılat süzgeci `SATIS + SATIS_IADE + TAKAS + ISCILIK` satırlarını **çeker**, ama toplar:
  `hasilatTl = brutHasilatTl − iadeTl` (**net**), `takasFarkTl` ve `iscilikTl` **ayrı kalem**.
- Maliyet kümesine **iade hareket kimlikleri** de alındı → ters tüketim satırı (Adet<0) negatif
  maliyet olarak toplanır: `maliyetTl = brutMaliyetTl − iadeMaliyetTl`. **Yeniden değerleme yok.**
- **TAKAS'ın maliyet bacağı da dışarıda**: hasılatı sayılmayan malın maliyeti sayılırsa kâr ikinci
  kez bozulurdu (SQL kanıtı §7.3: takas tüketimi 1.000 TL rapora girmedi).
- `donemKariTl = karTl + iscilikTl`; `karMarjiYuzde` **brüt kârdan** hesaplanır (işçilik marjı bozmaz).
- `karKoprusu[]`: BRUT_HASILAT → IADE → NET_HASILAT → MALIYET → IADE_MALIYETI → BRUT_KAR →
  ISCILIK → DONEM_KARI. Ara toplam olmayan satırların toplamı **kuruşu kuruşuna** dönem kârıdır.
- **Storno (48-A devri)**: hareket sorgusu `StornoSuzgeci.StornoCiftiniDisla` kullanır (ikinci
  süzgeç YAZILMADI); storno çifti ciro/adet/kâra katılmaz, `storno{adet,tutarTl,notu}` **ayrı** döner.
- `karsilastir=ONCEKI_DONEM|GECEN_YIL` — **isteğe bağlı, varsayılan kapalı**; kapalı liste kontrolü
  **servis içinde** (`BadRequestException`), dönem **eşit gün sayısı** kadar alınır; `onceki{...}`
  ve `degisimYuzde{...}` döner (önceki sıfırsa yüzde **null** — anlamsız yüzde uydurulmaz).
- Güvenilirlik artık ortak `RaporGuvenilirligi.Hesapla`dan gelir (imza/kod kümesi/eşikler
  0,9-0,5 **değişmedi**); mutlak hacim üzerinden ölçülür (tamamı iade edilen dönem "VERI_YOK"
  demesin diye).

### 3.2 `UrunBazli` ve `StokDegerlemesi`
- `UrunBazli`: TAKAS çıkarıldı, SATIS_IADE eklendi (satır bazında `brutHasilatTl/iadeTl/hasilatTl`),
  **satır başına `hasilatHas/maliyetHas/karHas`** (has bacağı hiçbir kırılımda gizlenmez),
  satır ve toplam **güvenilirlik**, `limit` servis içi doğrulama (1–500), `dahaFazlasiVar` (UX-S29).
- `StokDegerlemesi`: **`guvenilirlik`** eklendi; konsinye dışlama gerekçesi yanıtta duruyor.

### 3.3 QA turu 3 bulgusu — "HAS hasılatı aylık özetle uyuşmuyor"
İki **farklı büyüklük** ölçülüyordu ve ikisi de doğruydu; hata **beyan eksikliğiydi**:
`hasilatHas` = TL hasılatın satış günü kuruyla has karşılığı (kâr bu bacaktan ölçülür),
aylık özetteki `satis.toplamHas` = fiilen el değiştiren malın defter has gramı.
Çözüm: kâr-zarar yanıtına **`hasilatHasDefterGram`** (aylık özetle **aynı** büyüklük) ve
**`hasTanimNotu`** eklendi; ekranda "Defter has gramı" kartı + tanım cümlesi (#kz-has-tanim) çizilir.
Artık tek serviste iki rakam yan yana ve hangisinin ne olduğu yazıyor.

### 3.4 Ekran (`KarZarar.razor`)
Kâr köprüsü tablosu (#kz-kopru) · **takas farkı AYRI kart + gerekçe metni** (#kz-takas, zorunlu) ·
işçilik kartı (#kz-iscilik) · storno bloğu (#kz-storno) · karşılaştırma seçici (#kz-karsilastir) ve
değişim satırı (#kz-karsilastirma) · HAS tanım notu + defter gramı · ürün tablosunda **İade** ve
**HAS kâr** sütunları · stok değerlemesi güvenilirlik rozeti. Mevcut kartlar, id'ler ve
`DegismezIpucu` **korundu**; ham kod (BRUT_HASILAT…) ekrana basılmaz, sözlükten çevrilir (UX-S31).

### 3.5 Sözlük
`SozlukKarZarar`'a 22 anahtar (TR + EN): BrutHasilat, Iade, NetHasilat, BrutKar, DonemKari,
IscilikGeliri, TakasFarki, TakasFarkiNotu, KarKoprusu, Kalem, Tutar, Karsilastir, KarsilastirmaYok,
OncekiDonem, GecenYil, Degisim, Storno, StornoNotu, HasKar, HasDefterGram, HasTanimNotu, IadeAdedi.

## 4. Sapmalar ve gerekçeleri

| Sapma | Gerekçe |
|---|---|
| `/kar-zarar/kirilim` ucu **yazılmadı** | Faz B kapsamı; orkestratör talimatı "yalnız Faz A". `RaporKatalogu.cs`'e de bu yüzden dokunulmadı (katalog kaydı Faz B'de doğar). |
| **M130** (kırılım HAS sütunu) kanıtı **yok** | Kırılım ucu Faz B'de doğduğu için mutasyon uygulanacak kod yok. Faz B'ye devredildi. |
| `hasilatTl` artık **net** | Spesifikasyonun canlı doğrulama koşulu ("hasilatTl satış toplamı EKSİ iade"). Alan adı/türü korundu, tanımı düzeldi; `brutHasilatTl` ayrıca döner. |
| TAKAS **maliyeti** de dışlandı | Spesifikasyon yalnız hasılat tarafını yazıyordu; hasılatı sayılmayan malın maliyetini saymak kârı ikinci kez bozardı (fail-closed yorum). Raporlanan ve testlenen bir karardır. |
| Yeni istemci record'ları `Kuyumcu.Web/Servisler/KarZararModelleri.cs`'e | `denetim.sh` "Pages içinde istemci record" tavanı 122; sayfada tanımlansaydı denetim KIRMIZI olurdu (nitekim ilk denemede 125 ile kırıldı, taşındı). |
| Storno mutasyonu için yanıta `iscilikAdedi` eklendi | Test açığı kapatma (bkz. §5.1). |

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Paket öncesi (dalga 3 devralma) | 2.161 | 2.161 | 0 |
| `KarZararIadeTestleri` (yeni, DB) | 16 | 16 | 0 |
| `KarZararEkranTestleri` (yeni, bUnit) | 8 | 8 | 0 |
| **Tam süit (1. koşum)** | **2.185** | **2.185** | **0** |
| Tam süit (2. koşum) | 2.185 | 2.184 | 1 — `UX1C1EkranTestleri.Pos_gecersiz_kalemde_…` |
| Tam süit (3. koşum) | 2.185 | 2.184 | 1 — `KarmaOdemeEkranTestleri.Satistan_alisa_gecince_karma_kipi_KAPANIR` |
| **Tam süit (4. koşum)** | **2.185** | **2.185** | **0** |

**Yeni test sayısı: 24.**

> ⚠ **KARARSIZLIK KANITIYLA RAPORLANIYOR (Anayasa md.1, gizlenmiyor):** 2. ve 3. koşumda **farklı**
> birer POS ekran testi (her ikisi de `Islemler.razor` üzerinde, bu pakette **dokunulmayan** dosya)
> kırmızı döndü; ikisi de tek başına koşturulduğunda **yeşil** (POS testleri 3 kez tekrar edildi →
> 3/3 yeşil; KarmaOdeme sınıfı → 10/10 yeşil) ve 1. ile 4. tam koşum **tam yeşil**. Bu,
> `SONRAKI-OTURUM-PROMPT §6`'da adı yakalanamayan **bUnit kararsızlığıdır**; adı artık iki örnekle
> kayıt altında. Paket 35 dosyalarıyla ilişkisi yok.

### 5.1 Bulunan test açığı (kapatıldı)
**Storno mutasyonu ilk denemede hiçbir testi kırmadı.** Sebep: storno çifti **TL'de net sıfırdır** —
süzgeç kaldırılsa bile `iscilikTl` 500 kalıyordu. → Yanıta `iscilikAdedi` eklendi ve test
`iscilikAdedi == 1` assert eder (süzgeçsiz hâlde 3 sayılır); mutasyon tekrarlandı → **KIRMIZI**.

## 6. Mutasyon kanıtları

| Kod | Değişiklik (tek eşleşme, `perl -0777 -pi -e`) | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| **M123-1** | `hasilatTl = brutHasilatTl - iadeTl` → `= brutHasilatTl` | `Tamami_iade_edilen_mal_kar_birakmaz` | `cmp` birebir |
| **M124-1** | Maliyet kümesinden iade kimlikleri çıkarıldı | `Kismi_iade_maliyeti_orantili_duser` | `cmp` birebir |
| **M125-1** | İade maliyeti **bugünkü parti fiyatıyla** (3.000) yeniden değerlendi | `Iade_CIKTIGI_maliyetle_geri_alinir` | `cmp` birebir |
| **M126-1** | `satislar` süzgecine `|| Tur == "TAKAS"` | `Takas_farki_hasilata_KATILMAZ` | `cmp` birebir |
| **M127-1** | `satislar` süzgecine `|| Tur == "ISCILIK"` | `Iscilik_geliri_ayri_kalemdir` | `cmp` birebir |
| **M128-1** | `UrunBazli` satırından güvenilirlik `null`landı | `UrunBazli_guvenilirlik_dondurur` | `cmp` birebir |
| **M129-1** | Karşılaştırma **takvim ayı** olarak alındı (`AddMonths(-1)`) | `Karsilastirma_donemi_esit_gun_sayisidir` | `cmp` birebir |
| **M131-2** | `StornoCiftiniDisla` kaldırıldı | `Storno_cifti_ciroyu_ve_adedi_DEGISTIRMEZ_ayri_gorunur` | `cmp` birebir |
| **M130** | — | **Faz B'ye devredildi** (kırılım ucu bu pakette yok) | — |

**Son mutasyondan sonra temiz derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Hata, 0 Uyarı**.

## 7. Denetim ve canlı doğrulama

### 7.1 `dotnet/denetim.sh`
İlk koşumda **kırmızı**: `✗ Pages içinde istemci record tanımı 125 (tavan 122)` — dört yeni record
sayfada tanımlanmıştı. Servislere taşındı, tavan **yükseltilmedi**. Son durum: **DENETİM TEMİZ**
(`Pages: duzenlenenId kalıbı 1/1` · `ModalPencere 44/45` · `Pages/*.razor 34/34` değişmedi).

### 7.2 Canlı (localhost:**5198** — kurucunun 5199'una DOKUNULMADI)
Test kiracısı: **`PAKET35-TEST canli`** (`01M2QT14C4RHNS6X8WM52QQNJ8`).

| # | İşlem | HTTP |
|---|---|---|
| 1 | Kayıt + kur fotoğrafı (`altinHasFiyati` 6.824,24) | 201 / 200 |
| 2 | Ürün A ve B (K22, 10 g) | 201 · 201 |
| 3 | ALIS A: 10 adet × 1.000 · ALIS B: 2 × 800 | 200 · 200 |
| 4 | SATIS A: 4 × 1.500 · SATIS B: 2 × 2.000 | 200 · 200 |
| 5 | SATIS_IADE A: 1 adet · SATIS_IADE B: **tamamı (2 adet)** | 200 · 200 |
| 6 | TAKAS (fark **+2.000**) · ISCILIK 500 | 200 · 200 |

| Uç | HTTP | Ölçülen |
|---|---|---|
| `GET /raporlar/kar-zarar` | 200 | brüt 10.000 · **iade 5.500** · **net hasılat 4.500** · brüt maliyet 5.600 · **iade maliyeti 2.600** · **maliyet 3.000** · kâr 1.500 · **işçilik 500 ayrı** · **takas farkı 2.000 ayrı** · dönem kârı 2.000 |
| aynı yanıt | | `hasilatHas` 0,6594 · `hasilatHasDefterGram` **27,48** · `hasTanimNotu` dolu · köprü 8 satır · `storno{adet:0}` |
| `…&karsilastir=ONCEKI_DONEM` (15 gün) | 200 | `gunSayisi 15` · `onceki 2026-08-19 → 2026-09-02`, **gunSayisi 15** · `degisimYuzde` alanları null (önceki dönem 0 → yüzde uydurulmadı) |
| `…&karsilastir=GECEN_YIL` | 200 | `onceki 2025-09-03 → 2025-09-17`, **gunSayisi 15** |
| `…&karsilastir=GECEN_HAFTA` | **400** | `karsilastir beklenen biçimde değil` |
| `GET /raporlar/kar-zarar/urun` | 200 | A: brüt 6.000 / iade 1.500 / net 4.500 / kâr 1.500 / **karHas 0,2198** · **B: net 0, kâr 0** (tamamı iade) · `guvenilirlik TAM` |
| `…/urun?limit=0` | **400** | `Limit 1 ile 500 arasında olmalıdır` |
| `GET /raporlar/stok-degerlemesi` | 200 | 8 adet · 7.600 TL · **`guvenilirlik: TAM`** |

### 7.3 SQL karşılaştırması (birebir)
```
hareketler (tur, adet, Σtl, Σ|has|):
  ALIS 2 −11600,00 109,9200 | ISCILIK 1 500,00 0 | SATIS 2 10000,00 54,9600
  SATIS_IADE 2 −5500,00 27,4800 | TAKAS 1 2000,00 0
stok_parti_tuketimleri × hareketler (tur, Σadet, Σmaliyet):
  SATIS 6,000 5600,00 | SATIS_IADE −3,000 −2600,00 | TAKAS 1,000 1000,00
ürün bazında net tüketim:  P35-A 4,000 · P35-B 0,000   ← tamamı iade edilen ürün NET SIFIR
```
Rapor rakamlarının **dördü de** SQL ile birebir: hasılat 10.000−5.500 = 4.500 · maliyet
5.600−2.600 = 3.000 · takas farkı 2.000 (ve takasın 1.000 TL'lik tüketimi **maliyete girmedi**) ·
işçilik 500. `hasilatHasDefterGram` 27,48 = (6−3) adet × 9,16 g.

### 7.4 Temizlik
API (PID 2031) durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. Test kiracısı
`tenant_id` kolonu olan **tüm** tablolardan silindi; `tenants` **9 → 8**, kiracının hareket sayısı
**0**. Kalan kiracılar: Demo Kuyumculuk, ByGatech Kuyumculuk, Düzeltme Sonrası Kuyumculuk,
Demo Kuyumcu, Ramazan Deneme, **QA-TEST-1/2/3** (kalıcı QA fikstürlerine DOKUNULMADI).

## 8. Değişen dosyalar
```
dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs     (değiştirildi)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                     (değiştirildi — TR+EN)
dotnet/Kuyumcu.Web/Pages/KarZarar.razor                   (değiştirildi)
dotnet/Kuyumcu.Web/Servisler/KarZararModelleri.cs         (YENİ)
dotnet/Kuyumcu.Tests/KarZararIadeTestleri.cs              (YENİ — 16 test)
dotnet/Kuyumcu.Tests/KarZararEkranTestleri.cs             (YENİ — 8 bUnit testi)
```
`MaliyetMotoru.cs`, `denetim.sh`, `KuyumcuDbContext.cs`, `Entities.cs`, `ModulKayit.cs`,
`RaporKatalogu.cs`, `MaliyetTestleri.cs` **DOKUNULMADI**. **Migration gerekmedi** (şema değişikliği yok).
**Dokunulan sayfalar (tek dokunuş kaydı): `Pages/KarZarar.razor`.**

## 9. DoD sözleşmesi
Bu paket **yeni varlık/kayıt türü doğurmaz** — salt okunur rapor uçlarıdır; PATCH/DELETE/pasife alma
yüzeyi **yoktur ve olmamalıdır**.

| Varlık | Düzenlenebilir mi | Nasıl |
|---|---|---|
| Kâr-zarar / kâr köprüsü / kırılım rakamları | **HAYIR — türetilmiş** | Ekranda `DegismezIpucu` (Varlik="KarZarar") duruyor: rakam düzeltilmez, **kaynak hareket** düzeltilir. |
| İade | Evet, ama buradan değil | `POST /islemler/satis-iade` (satış iadesi resmî düzeltme yoludur). |
| Takas / işçilik satırı | Evet, ama buradan değil | İşçilik **storno** ile (48-A motoru), takas `ErtelenenTurler`'de → düzeltme yolu `DuzeltmePolitikasi`'nda yazılı. |
| Finansal hard DELETE | **YOK** | Bu pakette hiçbir silme ucu yoktur. |

Yetki: üç uç da `[Yetki.ModulYetkisi("rapor")]` + `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`
(değişmedi). Denetim kaydı: uçlar okuma yapar, `DenetimServisi` yeniden yazılmadı.
**Her düğme** istek atar (bUnit `P35_08`, üç dönem düğmesi tek tek tıklanıp isteği ölçüldü);
sessiz form yok (aralık hatası alan altı Türkçe mesaj — mevcut `#kz-aralik-hata` korundu).

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| ZORUNLU ÖLÇÜM (ters tüketim) | ✅ | §1 |
| REGRESYON ÖNCE (karakterizasyon) | ✅ | §2 + test dosyası başlığı |
| YANIT ŞEKLİ KORUNUR + şekil regresyon testi | ✅ | `Yanit_sekli_KORUNUR_eski_alanlar_durur` + bUnit `P35_05` |
| `Guvenilirlik` ortak ölçek, eşikler değişmedi | ✅ | `RaporGuvenilirligi.Hesapla` (0,9 / 0,5) |
| İki bacak ayrı assert (tam iade) | ✅ | `Tamami_iade_edilen_mal_kar_birakmaz` (hasılat **ve** maliyet ayrı) |
| M125 fikstürü farklı maliyetli ikinci parti | ✅ | 1.000 vs 3.000; mutasyon 15.000 üretti → kırmızı |
| M129 `onceki.baslangic/bitis` assert | ✅ | 15 gün, iki seçenek de |
| Kapalı liste **servis içinde** | ✅ | `BadRequestException` + test |
| KULLANICI ekseni yalnız ad + rakam | ➖ | Faz B (kırılım ucu bu pakette yok) |
| Yeni dosya `KarZararIadeTestleri.cs` | ✅ | `MaliyetTestleri.cs`'e tek satır eklenmedi |
| Karşılaştırma isteğe bağlı, varsayılan kapalı | ✅ | canlı: parametresiz çağrıda `onceki: null` |
| Takas farkı ayrı kart + açıklama (ZORUNLU) | ✅ | `#kz-takas` + bUnit `P35_02` |
| Storno çifti dışlanır, ayrı görünür (48-A devri) | ✅ | `StornoSuzgeci` kullanıldı, ikinci süzgeç yok; M131-2 |
| QA3 — pasta grafiği kültür hatası | ✅ (paket 34-A'da kapanmış) | `PastaGrafik.razor` `InvariantCulture` ile biçimliyor |
| QA3 — aylık veresiye/tahsilat sıfır | ✅ (paket 34-A'da kapanmış) | `RaporModulu.Aylik` veresiye payı + kasa girişi |
| QA3 — HAS hasılatı uyuşmazlığı | ✅ | §3.3 + `HAS_hasilati_defter_grami_ile_BIRLIKTE_beyan_edilir` |
| Ücretsiz pilot (fiyat/teklif metni yok) | ✅ | Üretilen hiçbir metinde fiyat yok |

## 11. Açık kalanlar
1. **Faz B** (kategori/şube/kullanıcı/reyon kırılımı + `/kar-zarar/kirilim` ucu + katalog kaydı +
   M130) — sevk edilmedi, ayrı paket/dalga.
2. **bUnit kararsızlığı**: POS ekran testleri (`UX1C1EkranTestleri`, `KarmaOdemeEkranTestleri`) tam
   süitte ara sıra kırmızı, tek başına yeşil. Bu pakete ait değil; adları artık kayıtlı.
3. **Geçmiş dönem raporları değişti** — bu doğru yönde bir değişimdir ama **eski çıktılarla
   tutmaz**: iade artık düşülüyor, takas farkı ciroda değil. Sürüm notu Köprü'nün işi.
4. Takasın **brüt cirosu ölçülemiyor** (deftere yalnız fark yazılıyor). Spesifikasyondaki seçenek
   **(a)** uygulandı: yalnız fark, açıkça beyan. Brüt değer kolonu eklemek ayrı paket kararıdır.

## 12. Kurucuya / mali müşavire notlar
- **VUK md.274/275 ve md.186 hükümleri koda YAZILMADI** (spesifikasyonda "DOGRULANMALI"). Rapor
  yalnız kiracının seçtiği maliyet yönteminin sonucunu gösterir; stok değerlemesi ekranı resmî
  envanter yerine geçtiğini **iddia etmez** ("yönetim raporu" ibaresi duruyor).
- **İşçilik gelirinin maliyeti modellenmemiştir** (imalat/atölye Katman 1 kapsam dışı). Kâr
  köprüsünde işçilik brüt kârdan **sonra** gelir; marj yüzdesine karışmaz.
- **Müşteriye söylenmesi gereken:** bu sürümden itibaren geçmiş dönemlerin kâr-zarar çıktısı
  değişecektir (iade düşülüyor, takas farkı ciroda değil). Rakam "düştü" değil, **düzeldi**.
