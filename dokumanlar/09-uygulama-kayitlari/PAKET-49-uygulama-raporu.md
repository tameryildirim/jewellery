# PAKET 49 — Fiyat listesi = ürün görünümü (toplu/tekil marj, geçersiz kılma, düzenleme)

> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Dalga 2 · 2026-09-16
> Durum: **BİTTİ** — derleme 0 hata 0 uyarı · `dotnet test` **1245/1245 yeşil** · `denetim.sh` **TEMİZ** · canlı doğrulama 5198'de gerçek veriyle yapıldı · test kiracısı silindi · **COMMIT YOK**.

## 0. Önkoşul doğrulaması (ilk adım — varsayım yok)

| Önkoşul (UX-4a) | Kanıt | Sonuç |
|---|---|---|
| `Fiyatlandirma.razor`'da Düzenle eylemi | `#fl-duzenle-{id}` düğmesi + `ListeDuzenle(l)` (satır 91, 296) | VAR |
| Form çift amaçlı (id doluysa PATCH) | `ListeForm.Id` + `ListeKaydet` içinde POST/PATCH ayrımı | VAR (belgedeki "duzenlenenId" adı kodda `ListeForm.Id`) |
| `FiyatModulu`'nde `[ModulYetkisi("urun")]` | FiyatModulu.cs:269 + yazma uçlarında `[RolGerekli("SAHIP","MUDUR")]` | VAR |
| `Bilesenler/FiyatListesiUrunPaneli.razor` | 204 satır, UX-4a madde 3 paneli | VAR |
| UX-4a madde 1 (ListeGuncelle varsayılan hatası) kapalı | FiyatModulu.cs "KORUMA SATIRI" yorumu + `if (varsayilanIstendi) liste.Varsayilan = true;` | VAR |

Önkoşullar karşılandığı için pakete devam edildi.

## 1. Yapılanlar

### Veri modeli
- **Yeni varlık** `FiyatListesiUrunFiyati` → `fiyat_listesi_urun_fiyatlari` (`Data/FiyatListesi.cs`): `id, tenant_id, liste_id, urun_id, sabit_fiyat decimal(18,2) NULL, has_marj_yuzde decimal(7,3) NULL, iscilik_carpan_yuzde decimal(7,2) NULL, not varchar(200) NULL, aktif, olusturuldu, guncellendi`. `IKiraciVarlik` → global sorgu filtresi + `KiraciYazmaDenetcisi` otomatik kapsar (DbContext refleksiyonla tüm `IKiraciVarlik` türlerine filtre kurar).
- **İndeksler:** `UNIQUE (tenant_id, liste_id, urun_id)` ve `(tenant_id, liste_id)` — `SHOW INDEX` ile doğrulandı (Non_unique=0 / 1).
- Silme yok; `aktif=false` ile pasife alınır (denetim izi korunur).

### Motor (tek hesap yolu korunarak)
`FiyatMotoru.Hesapla(..., FiyatListesiUrunFiyati? gecersizKilma = null)`; `FiyatTeklifi`'ye `Kaynak` alanı (`FiyatKaynagi.Liste | UrunGecersizKilma | UrunSabitFiyat`) eklendi — varsayılan değerli parametre olduğu için **mevcut çağıranlar ve testler kırılmadı**. Öncelik: (1) geçersiz kılma sabit fiyatı → (2) ürün kartı sabit fiyatı → (3) geçersiz kılmanın marj/çarpanı listeninkinin **yerine** (toplanmaz; boş alan listeden) → (4) liste. Pasif/boş geçersiz kılma yok sayılır. Gümüş ve `hasKuru ≤ 0` fail-closed kuralları aynen duruyor; **Q3 mührü** motorun kural-2 doc-comment'ine yazıldı.

### Servis / uçlar (hepsi `[ModulYetkisi("urun")]` altında)
| Uç | Rol kapısı | Not |
|---|---|---|
| `GET /fiyat/listeler/{id}/onizleme?kurSnapshotId=&kategoriId=&q=&yalnizGecersizKilinan=&limit=&offset=` | okuma (urun) | SALT HESAP — hiçbir satır yazmaz; `toplam/limit/offset/satirlar[]`, her satırda `kaynak` ve `gecersizKilma` özeti |
| `GET /fiyat/listeler/{id}/urun-fiyatlari` | okuma (urun) | tanımlı geçersiz kılma KAYITLARI, sayfalı |
| `POST /fiyat/listeler/{id}/urun-fiyatlari` | `SAHIP, MUDUR` | çift kayıt → 409 |
| `PATCH /fiyat/listeler/{id}/urun-fiyatlari/{urunId}` | `SAHIP, MUDUR` | **gönderilmeyen alan korunur, açık null siler**; `aktif=false` pasife alır |
| `POST /fiyat/listeler/{id}/toplu-uygula[?onizleme=true]` | `SAHIP, MUDUR` | önizleme kuru çalıştırma; yazma çağrısı **parmak izi** ister, tutmazsa 409 |
| `GET /fiyat/teklif` (mevcut) | okuma (urun) | yanıta `kaynak` eklendi — geriye dönük uyumlu, POS değişmedi |

`FiyatServisi.TeklifListeyle` geçersiz kılmayı okuyup motora besler; POS da bu yoldan geçtiği için **ikinci hesap yolu açılmadı** (paket 1↔38 tek motor ilkesi). Kategori alt ağacı `Urun.KategoriServisi.AltAgacIdleri` ile çözülür (ürün listesi filtresiyle aynı kod). `KategoriZinciri` tek gövdeye indirildi (bellekteki harita + DB sarmalayıcı) — önizleme N ürünü tek sorguyla çözer.

### Ekran (UX-4a paneli → tam ürün görünümü)
`Bilesenler/FiyatListesiUrunPaneli.razor` **aynı bileşen içinde** genişletildi; ikinci panel/ikinci mekanizma bırakılmadı. Ürün başına `/fiyat/teklif` çağrısı kaldırıldı, yerine tek önizleme ucu geldi.
- Süzgeçler: kategori (`#flp-kategori`), arama (`#flp-ara`), yalnız geçersiz kılınanlar (`#flp-yalniz-gk`).
- Tablo: kod, ürün, ayar, gram, has değeri, marj, işçilik, **liste fiyatı**, indirim, satış fiyatı, **kaynak rozeti** (Liste / Ürüne özel / Ürün kartı sabit fiyatı); fiyat üretilemeyen satırda **sebep rozeti** (Gümüş / Fiyat üretilemedi).
- Satır içi tekil düzenleme: `#flp-ozel-{urunId}` → form (sabit fiyat | has marjı | işçilik çarpanı | not) + **"Ürüne özel fiyatı kaldır"** (PATCH `aktif=false`, DELETE değil).
- Toplu uygulama: çoklu seçim + "Seçili ürünlere toplu uygula" / "Bu kategori ve alt dallarına uygula" → **etki özeti modalı** (etkilenen ürün, toplam fark, ilk 10 satır eski→yeni, atlananlar ve sebepleri) → "Onayla ve uygula" (parmak iziyle).
- UX-3: toplu uygulama, seçim sütunu ve ürüne özel eylem `ProAlan` içinde; basit modda liste fiyatı **salt okunur** ve sebebi yazılı (`#flp-basit-not`).
- UX-S28: hata "kayıt yok"a çevrilmiyor; UX-S29: sayfa sınırı ve "daha fazlası var" uyarısı; UX-S30: kilitli düğmenin sebebi yazılı; başarı/hata bantları var; mevcut element id'leri korundu, yeni id'ler `flp-*` önekli; sabit renk/satır içi style yok.
- `Sozlukler.cs`: 33 yeni anahtar TR+EN (çıplak metin yok). UX-4a'nın "sonraki pakette gelecek" ipucu metni güncellendi.

### DoD sözleşmesi (Bertan koşulu)
| Varlık | Düzenleme yolu | Pasife alma | Yetki | Denetim | Ekran eylemi |
|---|---|---|---|---|---|
| `FiyatListesiUrunFiyati` | `PATCH /fiyat/listeler/{id}/urun-fiyatlari/{urunId}` (koru/sil sözleşmesi) | `aktif=false` (hard DELETE yok) | `[ModulYetkisi("urun")]` + `[RolGerekli("SAHIP","MUDUR")]` | `DenetimServisi.YazmaKaydet` (ekleme/güncelleme, gerekçeli) + otomatik yazma izi | `#flp-ozel-{urunId}` formu + "Ürüne özel fiyatı kaldır" |
| Toplu uygulama (işlem) | — (kayıt değil, işlem) | — | aynı | `YazmaKaydet("FiyatListesiTopluUygulama", …)` özet detayla | Etki özeti + onay modalı |
| `Hareket.ListeFiyati` | salt okunur | — | — | defter | `DuzeltmePolitikasi` T("Hareket") — storno yolu (VUK md.219) |

Yeni varlık **`Modules/Meta/DuzeltmePolitikasi.cs`** haritasına `D("FiyatListesiUrunFiyati", "api/v1/fiyat/listeler/{id}/urun-fiyatlari/{urunId}")` olarak eklendi (UX-1 haritası her DbSet varlığını zorunlu kılıyor; bu eksik tam süitte kırmızı verdi ve kapatıldı).

## 2. Sapmalar ve gerekçeleri

1. **`GET .../urun-fiyatlari` = ham geçersiz kılma kayıtları.** Spesifikasyon bu ucu hem "tekil geçersiz kılma ekle/güncelle" hem "liste × ürün görünümü" diye anıyordu; hesaplanan görünümü `onizleme` zaten veriyor. İki hesaplayan uç = iki doğruluk kaynağı olurdu; bu yüzden `urun-fiyatlari` **kayıt** ucu, `onizleme` **hesap** ucu olarak ayrıldı. Ekran yalnız `onizleme`yi kullanır.
2. **Toplu uygulamada "atlandı" listesi genişletildi.** Spesifikasyon yalnız gümüşü istiyordu; ürün kartında sabit fiyatı olan ve zaten sabit fiyatlı geçersiz kılması bulunan ürünler de atlanıp sebebi yazılıyor (`URUN_SABIT_FIYAT`, `GECERSIZ_KILMA_SABIT_FIYAT`). Gerekçe: bu ürünlerde marj değişikliği fiyatı değiştirmezdi — UX-4 madde 4 "sessiz etkisizlik" yasağı.
3. **Onay kapısı sunucuda.** Ek koşul gereği toplu uygulama yazma çağrısı önizlemeden dönen `parmakIzi` (liste + hedef + yüzdeler + her satırın eski/yeni fiyatı üzerinden SHA-256) olmadan 409 döner. Ekran kapısı güvenlik kapısı sayılmaz (Mehmet'in genel ilkesi).
4. **bUnit: `LISTE_YOK` boş durumu yerine 404.** Panel artık listeyi açıkça seçtiği için "liste yok" durumu üretilmiyor; boş durum testi "önizleme 0 satır döndü" senaryosuna, uyarı testi "sunucu hatası hata bandına düşer (boş durum ÇIKMAZ)" senaryosuna bağlandı.
5. **UX-4a'nın istemci-tarafı "toplu marj önizlemesi" kutusu kaldırıldı.** Aritmetiği ekranda tekrarlıyordu (ikinci hesap yolu). Yerine sunucu önizlemesi + onay modalı geldi; ilgili iki UX-4a testi paket-49 davranışına göre yeniden yazıldı (test sayısı düşmedi).
6. **Mevzuat işaretlemesi:** spesifikasyonda paket 49'a ait `DOGRULANMALI/VARSAYIM` damgalı bir hüküm yok; FEY md.11 / TRHTUY md.14 kuralları **mevcut** `IndirimOncesiFiyat` ve `Hareket.ListeFiyati` mekanizmalarıyla karşılanıyor, yeni sabit rakam koda yazılmadı.

## 3. Migration

- Ad: **`20260916101547_FiyatListesiUrunFiyatlari`** (`dotnet build` YEŞİL sonrası, `--no-build` KULLANILMADI).
- `Up()`: `CreateTable fiyat_listesi_urun_fiyatlari` (11 kolon) + 2 indeks; `.Designer.cs` `BuildTargetModel` içeriyor (101.948 bayt, 0 bayt değil).
- `kuyumcu_net`'e uygulandı — `__EFMigrationsHistory` son iki satır:
```
20260916101547_FiyatListesiUrunFiyatlari
20260916081339_FaturaKalemleri
```
- İndeks kanıtı (`SHOW INDEX FROM fiyat_listesi_urun_fiyatlari`): `IX_..._tenant_id_liste_id_urun_id` **Non_unique=0** (tenant_id, liste_id, urun_id) · `IX_..._tenant_id_liste_id` Non_unique=1.

## 4. Testler

| Kapsam | Dosya | Önce | Sonra | Yeni |
|---|---|---|---|---|
| Saf motor | `FiyatMotoruTestleri.cs` | 13 | 21 | +8 |
| DB/servis | `FiyatListesiTestleri.cs` | 10 | 21 | +11 |
| HTTP (yetki + onay kapısı) | `HttpFiyatYetkiTestleri.cs` | 7 | 10 | +3 |
| bUnit (yeni dosya) | `FiyatListesiUrunGorunumuTestleri.cs` | 0 | 7 | +7 |
| bUnit (UX-4a, yeniden yazılan 2 test) | `FiyatlandirmaUX4aEkranTestleri.cs` | 11 | 11 | 0 |
| **Tam süit** | — | **1216** | **1245** | **+29** |

Tam süit: `dotnet test … --logger trx` → **Toplam 1245, Geçti 1245, Başarısız 0** (trx: `scratchpad/trx-p49b/`). İlk koşumda 1 kırmızı vardı — `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR`: *"Politika haritasında damgası olmayan varlık(lar): FiyatListesiUrunFiyati"* → politika haritasına damga eklenerek çözüldü (gizlenmedi).

`dotnet/denetim.sh` → **DENETİM TEMİZ** (IgnoreQueryFilters 0 · Find/FindAsync 0 · ExecuteUpdate/Delete 4 · Hareketler.Add 1 · HasQueryFilter var · ArayuzModu kapı yüzeyinde 0).

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/p49/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir aynı olduğu kanıtlandı. Son mutasyondan sonra temiz `dotnet build` (0 hata 0 uyarı).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M49-1 | Öncelik ters: `if (gk?.SabitFiyat is > 0m)` → `if (urun.SatisFiyati is null && gk?.SabitFiyat is > 0m)` (ürün kartı öne geçer) | `Gecersiz_kilma_sabit_fiyati_URUN_KARTI_sabit_fiyatini_EZER` | `cmp` aynı |
| M49-2 | Geçersiz kılma marjı listeninkine EKLENİR: `gk?.HasMarjYuzde ?? liste.HasMarjYuzde` → `(gk?.HasMarjYuzde ?? 0m) + liste.HasMarjYuzde` | `Gecersiz_kilma_marji_listeninkiyle_TOPLANMAZ`, `Yalniz_marj_gecersiz_kilmasinda_iscilik_carpani_LISTEDEN_gelir` | `cmp` aynı |
| M49-3 | Çift kayıt koruması kaldırıldı: `AnyAsync(x => …)` → `AnyAsync(x => false)` | `Ayni_liste_ve_urun_icin_ikinci_gecersiz_kilma_409` | `cmp` aynı |
| M49-4 | Servisin kendi kiracı koşulu düşürüldü: `f.Id == listeId && f.TenantId == tenantId` → `f.Id == listeId` | `Baska_kiracinin_listesine_gecersiz_kilma_404` | `cmp` aynı |
| M49-5 | `?onizleme=true` dalında yazma serbest bırakıldı (`if (onizleme) return sonuc;` kaldırıldı, damga koşuluna `&& !onizleme`) | `Toplu_uygula_onizlemesi_HICBIR_SATIR_YAZMAZ`, `Toplu_uygula_onaysiz_cagrilirsa_409_doner` | `cmp` aynı |
| M49-6 | `Hareket.ListeFiyati`'na geçersiz kılma ÖNCESİ fiyat: `TeklifListeyle` içinde `gk = null` | `Gecersiz_kilmayla_dusen_fiyat_HAREKETE_yazilir_ve_30_gun_tabanini_dusurur` | `cmp` aynı |
| M49-7 | Ekranda onay modalı atlandı: panelin önizleme çağrısından `?onizleme=true` kaldırıldı (koşul KALDIRMA biçiminde — Razor `disabled` tuzağı) | `Toplu_uygulama_ONCE_etki_ozeti_gosterir_onaysiz_YAZMA_YOK` | `cmp` aynı |

**Test açığı bulundu ve kapatıldı (M49-4):** ilk kurulumda kiracı izolasyon testi yalnız kiracıya bağlı DbContext ile koşuyordu; global sorgu filtresi mutasyonu maskeliyordu. Test, **sistem kapsamında (filtresiz) bağlamla** da çağrı yapacak biçimde güçlendirildi — servisin kendi `tenant_id` koşulu artık tek başına sınanıyor.

## 6. Canlı doğrulama (localhost:5198 — kurucunun 5199 sunucusuna DOKUNULMADI)

Test kiracısı: `PAKET49-TEST` (`01M2MWJ2ZE441JCMX2BMBYYRXV`). Kur: `01M2MWJKYE7PDDMZB912NGQ71A`, has fiyatı 6.794,38.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` | 201 | **201** |
| 2 | `POST /fiyat/listeler` PERAKENDE %8 · TOPTAN %2 | 200 · 200 | **200 · 200** |
| 3 | 3 ürün: K22 10 g (işçilik 500) · sabit fiyatlı yüzük (25.000) · G925 12 g | 201 ×3 | **201 ×3** |
| 4 | `GET …/onizleme` (PERAKENDE) | 3 satır, kaynaklar LISTE / URUN_SABIT_FIYAT / gümüşte teklif yok | **200** — bilezik `listeFiyati 67.715,44 kaynak LISTE`; yüzük `25.000,00 URUN_SABIT_FIYAT`; kolye `teklifVar:false sebep:GUMUS` |
| 5 | `POST …/urun-fiyatlari` (TOPTAN, bilezik, marj %1) | 200 | **200**, kayıt id `01M2MWKBKJ…` |
| 6 | TOPTAN önizleme ÖNCE/SONRA | fiyat düşer, kaynak URUN_GECERSIZ_KILMA | **63.981,25 → 63.358,89**, `kaynak URUN_GECERSIZ_KILMA` |
| 7 | PERAKENDE önizleme (kesişim kuralı) | DEĞİŞMEZ | **67.715,44 LISTE** |
| 8 | Aynı (liste, ürün) için ikinci POST | 409 | **409** — "Bu listede bu ürünün fiyatı zaten tanımlı; güncelleyin" |
| 9 | `POST …/toplu-uygula` (damgasız) | 409 | **409** — "Önizleme özeti onaylanmadı…" |
| 10 | `POST …/toplu-uygula?onizleme=true` (kategori, işçilik çarpanı %50) | özet döner, YAZMAZ | **`uygulandi:false, hedeflenen:3, uygulanacak:1, toplamFark:-250.00, parmakIzi:2bbfb3bb…`; atlandı: KOLYE=GUMUS, YUZUK=URUN_SABIT_FIYAT** |
| 11 | `POST …/toplu-uygula` (damgalı) | 200, yazar | **200 `uygulandi:true`** |
| 12 | PERAKENDE önizleme (sonra) | işçilik 500→250, fiyat 250 düşer | **`iscilikTlUygulanan 250,00 · listeFiyati 67.465,44 · kaynak URUN_GECERSIZ_KILMA`** |
| 13 | `GET /fiyat/teklif` (toptancı cari) | önizlemedeki satış fiyatıyla BİREBİR | **63.358,89 · `kaynak URUN_GECERSIZ_KILMA`** |
| 14 | `POST /islemler/satis-fisi` (1 adet, 63.358,89) | 200 | **200**, fiş `01M2MWNC8P…` |
| 15 | SQL: `hareketler.liste_fiyati` | geçersiz kılınmış fiyat | **`SATIS 63358.89 63358.89 0.00`** (tl_tutari · liste_fiyati · indirim) |
| 16 | `PATCH …/urun-fiyatlari/{urunId}` `{aktif:false}` | 200, fiyat listeye döner | **200; TOPTAN önizleme 63.981,25 `kaynak LISTE`** |
| 17 | SQL: denetim izi | ekleme + güncelleme + toplu uygulama kayıtları | **`FiyatListesiUrunFiyati EKLEME "ekleme: …"`, `… "güncelleme: …"`, `FiyatListesiUrunFiyati GUNCELLEME "Aktif, Guncellendi"`, `FiyatListesiTopluUygulama EKLEME "toplu uygulama: hedef=3 uygulanan=1 atlanan=2 … çarpan=50 …"`** |

SQL tablo kanıtı (temizlikten önce):
```
liste            urun          marj   carpan  aktif  not
PERAKENDE(…S7Y)  …PD5WZE       NULL   50.00     1    kategori kampanyası
TOPTAN(…V0XS)    …PD5WZE       1.000  NULL      0    toptancı anlaşması
```

**Temizlik:** API durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**); 5199'daki kurucu sunucusu çalışmaya devam ediyor (dokunulmadı). `information_schema`'dan çekilen **33 tenant_id'li tablodan** ve `tenants`'tan test kiracısı silindi: `tenants` **7 → 6** (başlangıç değeri), `fiyat_listesi_urun_fiyatlari` / `hareketler` / `urunler` kiracı satır sayısı **0**.

## 7. Değişen dosyalar

```
dotnet/Kuyumcu.Api/Data/FiyatListesi.cs                      (yeni varlık eklendi)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs                  (DbSet + 2 indeks)
dotnet/Kuyumcu.Api/Data/Migrations/20260916101547_FiyatListesiUrunFiyatlari.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916101547_FiyatListesiUrunFiyatlari.Designer.cs
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatMotoru.cs
dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
dotnet/Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor
dotnet/Kuyumcu.Web/Pages/Fiyatlandirma.razor
dotnet/Kuyumcu.Tests/FiyatMotoruTestleri.cs
dotnet/Kuyumcu.Tests/FiyatListesiTestleri.cs
dotnet/Kuyumcu.Tests/HttpFiyatYetkiTestleri.cs
dotnet/Kuyumcu.Tests/FiyatlandirmaUX4aEkranTestleri.cs
dotnet/Kuyumcu.Tests/FiyatListesiUrunGorunumuTestleri.cs    (yeni)
dokumanlar/09-uygulama-kayitlari/PAKET-49-uygulama-raporu.md (bu dosya)
```
`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısında proje ağacına ait başka bir değişiklik bu paketten doğmadı. **COMMIT YAPILMADI.**

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Önkoşul UX-4a sevk edilmiş | ✔ | §0 tablosu |
| Q2: yeni uçlar `[ModulYetkisi("urun")]`; yazma `SAHIP/MUDUR`; TEZGAHTAR → 403 | ✔ | `TEZGAHTAR_urune_ozel_fiyat_TANIMLAYAMAZ_403`, `Onizleme_TEZGAHTARA_acik_yazma_MUDURE_acik` |
| Q3: gümüşte yalnız sabit fiyat; toplu uygulamada "atlandı" | ✔ | `Gumus_urun_SABIT_fiyatli_gecersiz_kilmayla_FIYATLANIR`, `Gumus_urunde_marj_gecersiz_kilmasi_REDDEDILIR…`, canlı adım 10 |
| Tablo + indeksler + IKiraciVarlik + HasQueryFilter + yazma denetçisi | ✔ | §3 · `denetim.sh` |
| Migration build'li üretildi, `--no-build` yok, Designer dolu, `__EFMigrationsHistory` kanıtı | ✔ | §3 |
| Önizleme salt hesap | ✔ | `Onizleme_hicbir_satir_YAZMAZ`, M49-5 |
| Toplu uygulama onaysız çağrılamaz (409) | ✔ | canlı adım 9, HTTP testi |
| Öncelik kuralları + `hareketler.liste_fiyati` | ✔ | M49-1/M49-6, canlı adım 15 |
| Tek motor (ikinci hesap yolu yok) | ✔ | `TeklifListeyle` → `TeklifHesapla` → `FiyatMotoru`; ekranda formül yok |
| Panel tam ürün görünümüne dönüştü, iki mekanizma kalmadı | ✔ | UX-4a testi artık `/fiyat/teklif` çağrısı OLMADIĞINI doğruluyor |
| Basit modda salt okunur (UX-3) | ✔ | `ProAlan` + `#flp-basit-not` |
| Sözlük TR+EN, çıplak metin yok | ✔ | `Sozlukler.cs` 33 anahtar ×2 dil |
| DoD sözleşmesi | ✔ | §1 DoD tablosu |
| Kapılar ve `HareketFabrikasi` tek yazma yolu değişmedi | ✔ | `denetim.sh` sayaçları sabit |
| Canlı doğrulama + test kiracısı silindi | ✔ | §6 |
| COMMIT YOK | ✔ | §7 |

## 9. Açık kalanlar / kurucuya notlar

1. **Sayfalama ekranda yalnız ilk 50 satır.** Sunucu `offset` destekliyor; panelde sayfa gezinme düğmesi yok, sınır ve daraltma yolu yazıyor (UX-S29). İleride sayfa düğmesi istenirse küçük bir ek iştir.
2. **Toplu uygulama tek seferde en fazla 200 ürün** (`EnFazlaSatir`); kategori hedefinde bu sınır ürün sayısına değil seçili listeye uygulanır — kategori altında 200'den çok ürün varsa hepsi işlenir. Çok büyük kataloglarda parçalı uygulama gerekebilir; bugünkü perakende dükkân ölçeğinde sınır sorun değil.
3. **Mali müşavir / kurucu notu (bilgi amaçlı, kod değişikliği gerektirmez):** geçersiz kılmayla fiyat düşürmek **indirim ilanı değil yeni liste fiyatıdır**; fişte indirim satırı doğurmaz, yalnız sonraki 30 günün "indirim öncesi fiyat" tabanını düşürür (FEY md.11 / TRHTUY md.14). Bu yorum motorun ve `IndirimOncesiFiyat`'ın belgelenmiş davranışıdır; mali müşavirin teyidi alınırsa belgeye iliştirilmesi iyi olur.
4. **Kurucunun elle silmesi gereken artıklar (hook `rm` engelliyor, bu pakette OLUŞTURULMADI):** `Modules/Fiyat/FiyatModulu.cs.mutorj`, `Modules/Fiyat/FiyatMotoru.cs.mutorj` ve diğer eski `*.mutorj` dosyaları önceki oturumlardan kalmadır; derlemeye girmez. Bu paketin mutasyon yedekleri yalnız scratchpad altında tutuldu.
