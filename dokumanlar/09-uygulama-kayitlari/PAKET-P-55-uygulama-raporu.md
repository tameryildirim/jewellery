# PAKET P-55 — Gider pusulası: satırda Düzenle + belge ekranından dönüş + kilit gerekçelerinin görünürlüğü

> Şerit C (gider pusulası + vade + kurulum + rapor grafikleri) · QA turu 8 triyajı · KRİTİK
> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü · 2026-09-24
> Durum: **BİTTİ** (paket kapısının 7 maddesi sağlandı — aşağıda kanıtlarıyla)

## 1. Spesifikasyonun koda karşı doğrulanması

| İddia (triyaj) | Kodda ölçülen | Sonuç |
|---|---|---|
| PATCH yalnız satıcı adresi + mal açıklaması (VUK md.234) | `HurdaModulu.cs` `HurdaServisi.Guncelle` (satır 244-295) — yalnız `saticiAdres`, `malAciklamasi`; iptalli belge 409; boş değer 400 | DOĞRU (satır numarası kaymış: 402-421 değil 244-295; denetleyici 405-411) |
| Ekran bunu söylemiyor | Çekmecede TEK genel `DegismezIpucu` vardı; kilitli alanların yanında gerekçe yoktu; listede "Düzenle" yoktu (yalnız "Detay") | DOĞRU |
| Belge ekranından pusulaya dönüş yok | `PusulaBelgesi.razor` "Geri" → `/gider-pusulalari` (listeye; hangi pusula olduğu kayboluyordu) | DOĞRU |
| İptalli pusulada Kaydet kilitli | ZATEN VAR (S-3d/DP4: `DuzenlemeKilitSebebi` + Çekmece `KilitSebebi`) ama testi yoktu | Mevcut kural testle kilitlendi (M55-2) |
| `Kuyumcu.Tests/PusulaEkranTestleri.cs` | YOKTU | Oluşturuldu |

## 2. Yapılanlar

**`Pages/GiderPusulalari.razor`**
- Her liste satırına **Düzenle** düğmesi (`#gp-duzenle-{id}`, aria-label "{no} pusulasını düzenle"). Tekil GET atar (gerçek istek — DoD EK 1) ve çekmeceyi **düzenleme kipinde** açar: düzeltilebilir iki alan ("Düzeltilebilen alanlar" başlığı + açıklama) kilitli alanların **üstünde** çizilir. "Detay" ile açılışta yerleşim eskisi gibi (düzenlenebilir blok altta).
- Çekmece başlığı **"Gider pusulası — Detay / Düzenle"**.
- Kilitli dört alanın her birine **ayrı `DegismezIpucu`**: satıcı adı (`#degismez-ipucu-GiderPusulasi-Satici`), TCKN (`-Tckn`), tutar (brüt/stopaj/net, `-Tutar`), tarih (teslim/son tarih, `-Tarih`). Her biri gerekçe + düzeltme yolunu yazar: *"Pusulayı iptal edip yeni pusula düzenleyin: aşağıdaki "Pusulayı iptal et (VUK md.234)" düğmesiyle belgeyi gerekçeli olarak hükümsüz kılın, sonra doğru bilgilerle yeni bir hurda alışı girin."* Anlatılan yol ekranda GERÇEKTEN var (çekmecedeki iptal düğmesi) — `UX1C3KilitDurustlukTestleri.VaatKaliplari` yasaklı kalıplarının hiçbiri kullanılmadı.
- Mevcut genel `#degismez-ipucu-GiderPusulasi` KORUNDU (id değişmedi; üç mevcut test ona bağlı).
- `?ac={id}` / `?duzenle={id}` sorgu parametreleri: belge ekranından dönüşte ilgili pusula (detay ya da düzenleme kipinde) açılır.
- Süre geçmiş pusulada dürüst uyarı `#gp-sure-gecti` (bkz. § 3 sapma).
- Çekmece `Kapat` satır içi `{ }` lambdası `CekmeceKapat` yöntemine taşındı (Razor öznitelikte `{ }` lambda yasağı).
- Düzenlenebilir blok tek `RenderFragment` (`DuzenlenebilirBlok`) — iki kipte aynı alan kümesi, tek kaynak (UX-S26).

**`Pages/PusulaBelgesi.razor`**
- `#gpb-geri` (id DEĞİŞMEDİ) artık **"← Pusulaya dön"** ve `/gider-pusulalari?ac={Id}` adresine gider (doğru kimlik).
- Yeni bağlantı **"Pusulayı düzenle"** `#gpb-duzenle` → `/gider-pusulalari?duzenle={Id}`.
- Yeni satır `#gpb-iptal-yeri` (ekranda, `yazdirma-gizle` — kâğıda çıkmaz): iptalin nerede yapıldığını yazar.

**`Kuyumcu.Core/I18n/Sozlukler.GiderPusulasi.cs`** (YENİ, partial deseni — ortak hub `Sozlukler.cs`'e DOKUNULMADI): `GiderPusulasiDuzenMetinleri` konumsal record, TR + EN (eksik anahtar = derleme hatası).

**Backend:** değişiklik GEREKMEDİ — sunucu kilidi (PATCH alan beyaz listesi, iptalli 409, boş değer 400, `[RolGerekli("SAHIP","MUDUR")]`, `DenetimServisi.YazmaKaydet`) zaten vardı ve canlıda doğrulandı (§ 7).

## 3. Sapmalar ve gerekçeleri

1. **"Düzenleme son tarihi geçmişse Kaydet kilitlenir" UYGULANMADI → dürüst uyarı basıldı.** Mevzuat kuralı gereği birincil kaynak arandı: WebSearch (GİB/mevzuat.gov.tr); `https://www.gib.gov.tr/mevzuat/kanun/434` içerik döndürmedi, `https://mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf` ("unable to verify the first certificate") açılamadı. VUK md.234'ün, **süre geçtikten sonra basılmış pusulada adres/mal açıklaması düzeltmesini** yasaklayıp yasaklamadığı **DOĞRULANAMADI**. Ayrıca sunucu (`HurdaServisi.Guncelle`) süre kilidi uygulamıyor; yalnız istemcide kilit koymak "aynı kural iki tarafta" ilkesini bozar ve `HurdaModulu.cs` bu şeridin dosya sınırı DIŞINDADIR. Tahminle mevzuat kilidi yazmamak için: kilit YOK, çekmecede `#gp-sure-gecti` uyarısı ("Süre geçtikten sonra yapılan düzeltmenin uygun olup olmadığını mali müşavirinize sorun"). → **mali müşavir teyidi gerekir** (§ 11).
2. **`EkranTestleri.cs` → `GiderPusulasiEkranTestleri.Ekranda_SILME_veya_DUZENLEME_dugmesi_YOKTUR` uyarlandı.** Test "ekranda hiç 'düzenle' düğmesi yok" diyordu; P-55 bunu açıkça istiyor (ve S-3d'den beri PATCH vardı). "Sil" yasağı AYNEN korundu; "düzenle" yalnız `gp-duzenle-*` kimlikli satır eylemi için serbest, başka hiçbir düzenle düğmesi olamaz.
3. **Kabul "listede yansır":** liste ucu adres/mal açıklaması DÖNDÜRMEZ (TCKN maskeli özet). Yansıma üç yerden kanıtlandı: kaydet sonrası liste yeniden okunur (bUnit), tekil GET ve belge yeni değeri döndürür (HTTP + canlı), DB satırı güncel (SQL).
4. Test listesindeki `AdresVeAciklamaDuzenlenipKaydedilir` iki katmanda yazıldı: HTTP (`PusulaDuzenleHttpTestleri.AdresVeAciklamaDuzenlenipKaydedilir` — gerçek 200 + DB) ve ekran (`PusulaEkranTestleri.AdresVeAciklamaDuzenlenipKaydedilir_Ekran` — PATCH gövdesi JSON ayrıştırılarak sınanır).

## 4. Migration
Gerekmedi (şema değişikliği yok).

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| P-55 filtreli (`PusulaEkranTestleri` + `PusulaDuzenleHttpTestleri`) | 6 | 6 | 0 |
| İlgili gerileme süzgeci (S3f, UX1C3*, EkranTestleri, S3d, S3b, GiderPusulasi*, Rota, B1, P37, Marka, Sozluk, Dugme) — ilk koşum | 1254 | 1251 | 3 → 1'i benim (`Ekranda_SILME_veya_DUZENLEME_dugmesi_YOKTUR`, § 3.2 ile uyarlandı, sonra yeşil); 2'si başka şeritlerin (S33 Ürünler, S22 Tezgâh fişi) |
| Uyarlama sonrası `GiderPusulasiEkranTestleri` + `PusulaEkranTestleri` | 11 | 11 | 0 |
| **TAM SÜİT** (`KUYUMCU_TEST_DB=kuyumcu_test_k`, trx `scratchpad/trx-pP-55/_Mac_2026-09-24_02_08_54.trx`) | **4720** | **4720** | **0** |

Test sayısı: önce 4714 (4720 − bu paketin 6 yeni testi), sonra 4720. Yeni test: 6.
Tam süitte kırmızı yok → bu paketin ürettiği yeni kırmızı **0** (kapı maddesi 7). İlk gerileme koşumundaki S33/S22 kırmızıları tam süit anında yeşildi (başka şeritlerin ara durumuydu).

Yeni testler: `SatirdaDuzenleEylemiVar` · `KilitliAlanlardaGerekceGorunur` · `BelgeEkranindaPusulayaDonusVar` · `AdresVeAciklamaDuzenlenipKaydedilir_Ekran` · `IptalliPusuladaKaydetKilitli` · `AdresVeAciklamaDuzenlenipKaydedilir` (HTTP).

## 6. Mutasyon kanıtları
Yöntem: yedek `scratchpad/mut-yedek/P-55/` → `perl -0777 -pi -e` → `dotnet test --filter PusulaEkranTestleri -v n` → yedeği geri kopyala → `cmp` birebir (her seferinde `CMP-AYNI`). Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 uyarı 0 hata.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| M55-1 | Dört alan-başı `<DegismezIpucu Varlik="GiderPusulasi-(Satici\|Tckn\|Tutar\|Tarih)"…/>` çağrısı silindi | **KilitliAlanlardaGerekceGorunur**, SatirdaDuzenleEylemiVar (2/5 kırmızı) | cmp birebir |
| M55-2 | `DuzenlemeGecerli`: `detay is { IptalEdildi: false } &&` → `detay is not null &&` | **IptalliPusuladaKaydetKilitli** (1/5 kırmızı) | cmp birebir |
| M55-3 (ek) | `PusulaBelgesi.Geri()` → `Nav.NavigateTo("/gider-pusulalari")` (kimliksiz dönüş) | **BelgeEkranindaPusulayaDonusVar** | cmp birebir |
| M55-4 (ek) | `DuzenleAc`: `duzenlemeKipi = true` → `false` | **SatirdaDuzenleEylemiVar** | cmp birebir |

Not: M55-3 ve M55-4 aynı koşumda (farklı dosya/farklı davranış) uygulandı; her biri yalnız kendi testini kırdı (M55-3 yalnız belge dönüşünü, M55-4 yalnız düzenleme kipinin sırasını etkiler).

## 7. Canlı doğrulama (port 5186, `kuyumcu_net`, kiracı `PAKETP55-TEST Kuyumculuk`)
Davet jetonu yerelde üretildi, DB'ye yalnız özeti yazıldı; jeton/parola/erişim jetonu ekrana basılmadı. Pusulalar GERÇEK yoldan (`POST /islemler/hurda-alis`) üretildi. Yanıt gövdeleri okundu.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` (davetli) | 201 | **201** (tenantId, accessToken döndü) |
| 2 | `POST /kasalar` + `POST /kasalar/{id}/hareket` GIRIS 1000 | 200 | **200 / 200** (ilk hurda alışı kasa 0 iken **400** "bakiyesi yetersiz… eksiye düşemez" — kapı çalışıyor) |
| 3 | `POST /islemler/hurda-alis` ×2 | 200 + pusula | **200 ×2** → GP-2026-000001, GP-2026-000002 |
| 4 | `POST /gider-pusulalari/{G2}/iptal` | 200 | **200** `iptalEdildi:true` + `duzeltmeYolu` |
| 5 | `PATCH /gider-pusulalari/{G1}` adres+mal | 200 | **200** `adres=Kızılay Mah. 9 · mal=Hurda 22 ayar bilezik (2 adet)` |
| 6 | `PATCH {G1}` yalnız mal | 200, adres korunur | **200** adres **Kızılay Mah. 9** korundu, mal "(3 adet)" |
| 7 | `PATCH {G2}` (iptalli) | 409 | **409** "İptal edilmiş gider pusulası düzenlenemez…" |
| 8 | `PATCH {G1}` yalnız `saticiAdSoyad` (kilitli alan) | 400 | **400** "Düzenlenebilir alan gönderilmedi… İPTAL + YENİ PUSULA" |
| 9 | `GET /gider-pusulalari/{G1}` | yeni değerler, mali alanlar aynı | **200** adres/mal yeni · ad "Canli Test Satici" · brüt 91.6 · iptal False |
| 10 | `GET /gider-pusulalari` | iki satır, TCKN maskeli | **200** `[GP-…002 iptal=True, GP-…001 iptal=False]`, TCKN `*******0146` |
| 11 | `GET /gider-pusulalari/{G1}/belge` | yeni değerler belgede | **200** `Kızılay Mah. 9 · Hurda 22 ayar bilezik (3 adet)` |

SQL kanıtı:
```
select pusula_no,satici_adres,mal_aciklamasi,satici_ad_soyad,brut_tutar,iptal_edildi from gider_pusulalari where tenant_id='<P55>';
GP-2026-000001  Kızılay Mah. 9  Hurda 22 ayar bilezik (3 adet)  Canli Test Satici  91.60  0
GP-2026-000002  Ulus Mah. 1     Hurda bilezik                   Canli Test Satici  91.60  1
select eylem,varlik_turu,hassas,detay from denetim_kayitlari where varlik_id='<G1>' and detay like '%düzenlendi%';
EKLEME  GiderPusulasi  1  gider pusulası GP-2026-000001 düzenlendi · değişen alan: satıcı adresi, mal açıklaması   (×2)
EKLEME  GiderPusulasi  1  gider pusulası GP-2026-000001 düzenlendi · değişen alan: mal açıklaması
```
Dürüstlük notları: (a) İlk denemede SQL ile doğrudan pusula satırı eklendi; TCKN alanı şifreli beklendiği için GET/PATCH **500** verdi (`CryptographicException: Şifreli alan biçimi tanınmadı`). Bu fikstür hatasıydı (ürün kusuru değil); 2 satır silindi ve pusulalar gerçek hurda alışıyla üretildi. (b) Ekran (Blazor) canlıda tarayıcıyla denenmedi: oturum açmak parola girmeyi gerektirir; ekran davranışı bUnit ile kanıtlandı.

API durduruldu (PID 92242 `kill`; `lsof -nP -iTCP:5186 -sTCP:LISTEN` boş). Test kiracısı `information_schema`'dan `tenant_id` kolonu olan TÜM tablolardan + `tenants` + tüketilen davet satırından silindi; kalan satır 0; **tenants 10 → 10**. `QA-TEST-*` kiracılarına dokunulmadı.

## 8. denetim.sh
`DENETİM TEMİZ.` (rc=0, 286 ✓). Önce/sonra farkı yalnız sayaç düşüşleri (ModalPencere 52→48, çıplak buton 70→67, P-11 45→42 — kardeş şeritlerin işi) ve SECICI_GOC 163→164; bu paket ModalPencere, çıplak `<button>`, satır içi `style` ya da sabit renk EKLEMEDİ.

## 9. DoD sözleşmesi

| Varlık | Düzenleme yolu | Salt okunur kısım ve neden |
|---|---|---|
| Gider pusulası (yeni varlık DEĞİL) | `PATCH /api/v1/gider-pusulalari/{id}` (`[RolGerekli("SAHIP","MUDUR")]`, `DenetimServisi.YazmaKaydet` hassas=true) — satıcı adresi + mal açıklaması; ekranda satır "Düzenle" + çekmece Kaydet | Satıcı adı, TCKN, tutarlar, tarihler: her birinin yanında `DegismezIpucu` gerekçe + yol (iptal + yeni hurda alışı). Pasife alma karşılığı: `POST /{id}/iptal` (gerekçeli, ortak onay modalı). Hard DELETE yok (vergi belgesi). |
| Yeni varlık/kayıt türü | — | Açılmadı. |

## 10. Dokunulan sayfalar ve değişen dosyalar
Dokunulan sayfalar: `Pages/GiderPusulalari.razor`, `Pages/PusulaBelgesi.razor`.

- `dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor` (değişti)
- `dotnet/Kuyumcu.Web/Pages/PusulaBelgesi.razor` (değişti)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.GiderPusulasi.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/PusulaEkranTestleri.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/EkranTestleri.cs` (yalnız `GiderPusulasiEkranTestleri.Ekranda_SILME_veya_DUZENLEME_dugmesi_YOKTUR` gövdesi, § 3.2)

(Depo tek "first commit" üzerinde; `git status --porcelain` bu dosyaları izlenmeyen/değişmiş olarak listeler.)

## 11. Koşul karşılama

| Kabul koşulu | Durum | Kanıt |
|---|---|---|
| Her satırda "Düzenle" görünür ve çekmeceyi düzenleme kipinde açar | KARŞILANDI | `SatirdaDuzenleEylemiVar`; M55-4 |
| Kilitli dört alanın her birinde gerekçe + düzeltme yolu okunur | KARŞILANDI | `KilitliAlanlardaGerekceGorunur`; M55-1 |
| Belge ekranında pusulaya dönüş bağlantısı doğru kimliğe gider | KARŞILANDI | `BelgeEkranindaPusulayaDonusVar`; M55-3 |
| Adres + mal açıklaması düzenlenip kaydedilir (200) ve yansır | KARŞILANDI | HTTP testi + ekran testi + canlı #5/#6/#9/#11 + SQL |
| İptalli pusulada Kaydet kilitli ve nedeni yazılı | KARŞILANDI | `IptalliPusuladaKaydetKilitli`; M55-2; canlı #7 (409) |
| (Kapsam) Son tarih geçmişse Kaydet kilidi | SAPMA — kilit yerine uyarı | § 3.1; mali müşavir teyidi gerekir |

## 12. Açık kalanlar
- **Mali müşavir teyidi:** Düzenleme süresi (7 gün) geçmiş gider pusulasında satıcı adresi/mal açıklaması düzeltmesine izin var mı? Birincil kaynak (VUK md.234 metni) bu oturumda açılamadı; DOĞRULANAMADI. Teyit gelirse kilit sunucuda (HurdaServisi.Guncelle — başka şeridin dosyası) yapılandırmadan okunan bir kuralla eklenmeli, ekran aynı kuralı yansıtmalı.
- **Denetim kaydı eylem adı:** pusula düzenlemesi `denetim_kayitlari.eylem = 'EKLEME'` olarak düşüyor (canlı SQL). Düzenleme için beklenen ad değil; `DenetimServisi`/yazma dinleyicisi SERİT B'nin alanında — sahibine iletilmeli.
- **Aynı değerle PATCH 400 döner** ("Düzenlenebilir alan gönderilmedi"): değişiklik yoksa sunucu hata veriyor; ekran bunu hata bandında gösterir. Sunucu dosyası bu şeridin dışında; iyileştirme önerisi olarak not.
- P-59 / P-61 / P-63 aynı sayfaya (`GiderPusulalari.razor`) dokunacak: satır eylemleri (`#gp-duzenle-{id}`) ve `DuzenlenebilirBlok` yapısı korunmalı.
