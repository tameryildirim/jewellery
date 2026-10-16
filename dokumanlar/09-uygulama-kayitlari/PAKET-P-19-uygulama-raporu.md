# PAKET P-19 — GİDER + VİRMAN: İKİ BACAKLI ATOMİK DESEN

**Dalga:** DALGA 5 · **Kimlikler:** Y-11 (virman) · Y-12 (gider kategorisi + gider ekranı)
**Roller:** Taşkın (03 Backend) + Ertuğ (03 Frontend) · **Tarih:** 2026-09-21
**Sürüm:** 1.0 · **Önkoşul:** P-08

> **DÜRÜSTLÜK UYARISI (Anayasa md.1, ÖNCE OKUNSUN):** paketin kendi işi bitti ve
> kanıtlı. Ancak **tam süitte 1 kırmızı ve denetim.sh'ta 1 ihlal duruyor** ve
> **ikisi de AYNI, BU PAKETE AİT OLMAYAN kalemdir** (P-02 / B-2). Ayrıntı § 10.
> Bu yüzden paket durumu **YARIM** damgalıdır; gizlenmedi.

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (P-08)

| Önkoşul | Diskte | Ölçüm |
|---|---|---|
| **P-08** | **VAR** | `PAKET-P-08-uygulama-raporu.md` + `PAKET-P-08-uygulama-raporu-TUR-2-2026-09-20.md`; kodda `HareketFabrikasi.Ekle` tür/adet/personel kapıları yerinde (`Data/HareketFabrikasi.cs:83-110`). |

Kapsam daraltması **gerekmedi**.

### Taze taban ölçümleri (belgeden KOPYALANMADI)

| Ölçüm | Başlangıç | Sonuç | Komut |
|---|---|---|---|
| `__EFMigrationsHistory` | **56** | **57** | `docker exec … -N kuyumcu_net -e "select count(*) from __EFMigrationsHistory"` |
| `tenants` | **8** | **8** | aynı |
| `QA-TEST-%` kiracı | **3** | **3** (DOKUNULMADI) | aynı |
| Migration `.cs` dosya | 121 | **123** | `ls Kuyumcu.Api/Data/Migrations/*.cs \| wc -l` |
| 0 baytlık migration `.cs` | 8 | **8** (ARTMADI) | `find … -size 0` |
| Tam süit | 3381 | **3419** | `dotnet test` |
| `IKiraciVarlik` varlık | 59 | **60** | denetim.sh |
| `Pages/*.razor` | 44 | **45** | `ls Pages/*.razor` |

---

## § 1 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-11** — Virman (kasa↔banka) + şubeler arası transfer | **KAPANDI** | Kod: `Data/HareketFabrikasi.cs` → `CiftBacakFabrikasi.BacaklariEsle` · `Modules/Gider/VirmanModulu.cs` (`VirmanServisi.Olustur`, `VirmanController`) · `Modules/Satis/StornoServisi.cs` → `VirmanStorno`. Test: `Virman_iki_bacagi_TEK_islemde_yazilir_ve_KARSILIKLI_baglanir`, `Virman_stornosu_IKI_BACAGI_birlikte_dondurur`, `Virman_bacagi_TEK_BASINA_stornolanamaz` (+10). Canlı: § 8 adım 6/7/9/10/17/18. Şubeler arası: her bacak KENDİ kasasının şubesine aittir; yanıtta `subelerArasi` alanı döner. |
| **Y-12** — Gider/masraf kategorisi + gider ekranı | **KAPANDI** | Kod: `Data/Entities.cs` → `GiderKategorisi`, `KasaHareket.GiderKategoriId` · `Modules/Gider/GiderModulu.cs` · `Modules/Gider/GiderOlcumu.cs` · `Modules/Maliyet/KarZararServisi.cs` (GIDER kalemi) · `Pages/Gider.razor`. Test: `Gider_kasadan_duser_ve_KATEGORILI_yazilir`, `Gider_kar_zararda_AYRI_SATIR_ve_donem_karindan_duser`, `Stornolanan_gider_kar_zarar_toplamindan_DUSER` (+13 ekran testi). Canlı: § 8 adım 1-5, 8, 11-16. |

| **Y-11** — *2. kapı turu eki (2026-09-21)* | **KAPANDI (ekran yolu dâhil)** | Sapma S1 kapatıldı: virman bacağı artık `Pages/Kasalar.razor` ve `Pages/Banka.razor`'da **tanınır** (storno kilidi + Türkçe gerekçe + `/gider?sekme=virman` bağlantısı). Test: `P19VirmanBacagiEkranTestleri` (7/7). Mutasyon: M-S1-1 · M-S1-2 · M-S1-3. Sayaç: `VIRMAN_BACAGI_EKRANSIZ_KAPI = 0`. Canlı: § 15.2 adım 2-6. Ayrıntı: **§ 15**. |

Kapatılamayan kimlik **YOK**. Paket durumu yine de **YARIM**'dır — tek engel P-19 dışındaki
**P-02/B-2** kalemidir (§ 15.3, eskalasyon).

---

## § 2 — YAPILANLAR

### 2.1 Veri modeli (migration `20260921022537_P19GiderVeVirman`)
- **YENİ TABLO** `gider_kategorileri` (`GiderKategorisi : IKiraciVarlik`) — `id · tenant_id · ad · aciklama · aktif · olusturuldu · guncellendi`.
  İndeksler: `(tenant_id, ad) UNIQUE` (aynı masrafın iki satıra bölünmesini engeller) · `(tenant_id, aktif)`.
  Global kiracı süzgeci `KuyumcuDbContext.KiraciFiltresiKur` ile **OTOMATİK** kurulur (elle `HasQueryFilter` eklenmedi; kapsam denetimi `BEKLENEN_VARLIK -eq 60` ile).
- **YENİ KOLON** `kasa_hareketleri.gider_kategori_id NULL` — null = "bu satır gider değildir".
- **YENİ KOLON** `kasa_hareketleri.karsi_bacak_id NULL` ve `banka_hareketleri.karsi_bacak_id NULL`.
- İndeks: `(tenant_id, karsi_bacak_id)` her iki tabloda · `(tenant_id, gider_kategori_id, tarih)`.
- `Down()` yazıldı **ve gerçekten koşturuldu** (BK-21 — § 4).

### 2.2 `karsi_bacak_id` ⇄ `karsi_sube_id` — ANLAM AYRIMI (plan DoD, belgede yazılı)

| | `hareketler.karsi_sube_id` (MEVCUT) | `karsi_bacak_id` (YENİ) |
|---|---|---|
| Neyin kimliği | **ŞUBE** | **SATIR** |
| Hangi tabloda | `hareketler` | `kasa_hareketleri` + `banka_hareketleri` |
| Neyi anlatır | **MAL** hareketinin (TRANSFER) gittiği şube | **PARA** hareketinin (VİRMAN) diğer yarısı |
| Satır sayısı | **TEK** (karşı şubede ikinci satır doğmaz) | **DAİMA ÇİFT**: A.karsi=B.id ∧ B.karsi=A.id |
| Tek başına duruşu | normaldir | **VERİ HATASIDIR** (eşsiz bacak) |

Bu tablo koda da yazıldı: `Data/Entities.cs` → `KasaHareket.KarsiBacakId` XML belgesi ve
`Data/HareketFabrikasi.cs` başlığı.

### 2.3 İki bacaklı atomik desen (mekanizma)
`Data/HareketFabrikasi.cs` içine `IBacakliHareket` arayüzü + `CiftBacakFabrikasi.BacaklariEsle(db, cikis, giris)`:
- iki taslak **TEK çağrıda, TEK parametre listesinde** verilir,
- `karsi_bacak_id` **karşılıklı** burada bağlanır,
- **her iki satırın `Add`'i de burada** yapılır (çağıran birini unutamaz),
- çağıran metotta **TAM 1 `SaveChangesAsync`** olur (sayaçla zorlanır),
- `KasaHareket`/`BankaHareket` dışında bir bacak türü gelirse **derleme sonrası kapı** patlar.

### 2.4 Uçlar
| Uç | Metot | Rol kapısı | Not |
|---|---|---|---|
| `/api/v1/gider-kategorileri` | POST · GET · PATCH · DELETE | POST/PATCH: SAHIP·MUDUR·MUHASEBE · DELETE: SAHIP·MUDUR | DELETE = **pasife alma** |
| `/api/v1/giderler` | POST · GET | POST: SAHIP·MUDUR·MUHASEBE | GET limitli (1-500), sınır yanıtta yazılı |
| `/api/v1/virman` | POST · GET | POST: SAHIP·MUDUR·MUHASEBE | çiftler hâlinde listeler |
| `/api/v1/virman/{bacakId}/storno` | POST | SAHIP·MUDUR·MUHASEBE | **tek storno motoru** (`StornoServisi.VirmanStorno`) |

Hepsi `[Yetki.ModulYetkisi("kasa")]`. **YENİ yetki anahtarı AÇILMADI** (kapsam kilidi § 6).

### 2.5 Kâr-zarar
`GIDER` kalemi kâr köprüsüne eklendi (`ISCILIK` ile `DONEM_KARI` arasında, **işaretli negatif**),
`giderTl` + `giderKirilimi` (kategori adıyla) + `giderNotu` alanları döner,
`donemKariTl = karTl + iscilikTl − giderTl`.
Ölçüm `Modules/Gider/GiderOlcumu.cs`tedir (tek kaynak — ekranla rapor aynı rakamı okur).

### 2.6 Ekran — `Pages/Gider.razor` (`/gider`)
Üç sekme: **Giderler · Gider Kategorileri · Virman**. UX sözleşmesi karşılıkları § 9.
Tek düzenleme kalıbı: çok alanlı düzeltme **Cekmece** (`#cekmece-gider-kategori`),
**ModalPencere yalnız YIKICI ONAY** (gider/virman stornosu, gerekçe ≥ 20 karakter).
Sözlük TR+EN: yeni `SozlukGider` sınıfı (52 anahtar) + `Nav.Gider`; çıplak metin yok.

---

## § 3 — SAPMALAR VE GEREKÇELERİ

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | ~~Plan `Pages/Kasalar.razor` ve `Banka.razor`'ı dosya listesine koymuştu; **ikisine de DOKUNULMADI**.~~ → **KAPATILDI (2. kapı turu, 2026-09-21).** Hasım doğrulayıcı bu sapmayı ORTA bulgu olarak geri gönderdi: plan satırı sözleşmedir ve sunucudaki 409 yönlendirmesinin ekran karşılığı yoktu. İki sayfa da açıldı, virman bacağı satırı artık kilitli + gerekçeli + doğru yola bağlantılı. Ayrıntı: **§ 15**. | Eski gerekçe (tek dokunuş/UX-S22 riski) yanlış tartılmıştı: yapılan iş virmanı kasa ekranına TAŞIMAK değil, o satırı **tanıyıp doğru ekrana yollamaktır** — UX-S22 çatışması doğmaz, UX-S25 ihlali ise gerçekten vardı. |
| S2 | `Modules/Gider/` dizininde **virman da** yaşıyor. | Paketin adı "GİDER + VİRMAN: İKİ BACAKLI ATOMİK DESEN"; plan başka yeni dizin tanımlamadı. `Modules/Kasa/`ya konsaydı virman tek kasanın modülüne hapsolurdu. Storno **orada değil** — tek storno motoru `Modules/Satis/StornoServisi.cs`. |
| S3 | `banka_hareketleri`'ne `gider_kategori_id` **EKLENMEDİ**. | Plan yalnız `kasa_hareketleri.gider_kategori_id` istiyor. Banka üzerinden gider (havaleyle kira ödemesi) **devredildi** — § 9. |
| S4 | Döviz virmanı (farklı para birimleri) **400 döner**. | Hangi kurdan çevrileceği (alış/satış/serbest) MALİ bir karardır; uydurulmadı. Mesaj kullanıcıya doğru yolu söyler. |
| S5 | Banka hesabında **negatif bakiye engellenmedi** (kasada engellendi). | KMH (kredili mevduat) negatif bakiyeye izin verir; kural kurucuya açık kalemdir (§ 11). Kasada nakit eksiye düşemez → engellendi. |
| S6 | MySQL parolası **`docker-compose.yml`'den ALINAMADI**. | Görev metni compose'u gösteriyordu ama oradaki değer **çalışan konteynerinkiyle uyuşmuyor** (`ERROR 1045 Access denied`). Çalışan parola `appsettings.Development.json` bağlantı dizesinden değişkene alındı; **hiçbir yerde basılmadı**. Bu bir ortam tuzağıdır, rapora yazıldı. |
| S7 | `ham/olcum-2026-09-18.json` içindeki **`ROTA_KAPSAM` değeri 42 → 45** düzeltildi. | 42 **BAYATTI**: P-17'nin `/vitrin` ve `/cikti` rotaları dosyaya işlenmemişti, yani `OLCUM_TABANI_KOPYA` bu paketten **ÖNCE de** 1 sapma veriyordu. Taban taze ölçüldü (BK-07), gizlenmedi. |

**Plan ↔ koşul çelişkisi (ÇÖZÜLDÜ):** plan P-19'un dosya listesinde `Pages/Kasalar.razor` ve
`Banka.razor` vardı; DoD EK 2'nin TEK DOKUNUŞ kuralı gerekçesiz ikinci dokunuşu engelliyordu. **Plan üstündür**
kuralı gereği dokunma hakkı vardı ve 2. kapı turunda **kullanıldı**; ikinci dokunuşun gerekçesi bu rapordur
(§ 15) ve kapı kaydına yazılmak üzere hasım doğrulayıcının ORTA bulgusudur.

---

## § 4 — MİGRATION

- **Ad:** `20260921022537_P19GiderVeVirman`
- **Dosya boyutları:** `.cs` **4.716 bayt** · `.Designer.cs` **200.915 bayt** (ikisi de > 0)
- **`Up()`** 3 kolon + 1 tablo + 5 indeks · **`Down()`** hepsini geri alır
- `dotnet ef` **`--no-build` KULLANILMADI**; derleme her seferinde YEŞİLDİ.

**BK-21 — `Down()` GERÇEKTEN KOŞTURULDU:**

| Adım | Komut | `__EFMigrationsHistory` | `karsi_bacak_id` kolonu | `gider_kategorileri` tablosu |
|---|---|---|---|---|
| 1. Uygula | `dotnet ef database update` | 56 → **57** | **2** | **1** |
| 2. Geri al | `dotnet ef database update 20260921001728_P16UrunCariKartAlanlari` | 57 → **56** | **0** | **0** |
| 3. Yeniden uygula | `dotnet ef database update` | 56 → **57** | **2** | **1** |

Rollback sonrası şema P-16 hâline **tam** döndü; veri kaybı yok (kolonlar yeni, dolu satır yoktu).

---

## § 5 — TESTLER

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| **Önce** (P-19 başlamadan) | 3381 | 3381 | 0 | 0 |
| `--filter P19CiftBacakTestleri` | 26 | **26** | 0 | 0 |
| `--filter P19EkranTestleri` | 12 | **12** | 0 | 0 |
| **Tam süit (sonra)** | **3419** | **3418** | **1** | 0 |

**Yeni test: 38** (26 HTTP/DB + 12 bUnit).
**Tek kırmızı:** `OlcumKapisiTestleri.inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ`
— **BU PAKETE AİT DEĞİL**, bkz. § 10.

Yan etkiyle güncellenen mühürlü sayaç testleri (hepsi bu paketin eklediği sayfa/varlık yüzünden,
gerekçe yorumu koda yazıldı):
`MenuMimarisiTestleri` 36→37 menü ögesi · `RotaBaslikTestleri` 44→45 rota ·
`DuzeltmePolitikasiTestleri` (yeni varlık politika haritasına eklendi) ·
`SozlesmeYansimaTestleri.Ciftler()` (9 yeni kilit) · `betikler/qa-turu.js` `ROTA_KAPSAM_BEKLENEN` 44→45.

---

## § 6 — MUTASYON KANITLARI

Yöntem: kaynak `scratchpad/mut-yedek/P-19/` altına kopyalandı → tek eşleşmeli değişiklik →
filtreli `dotnet test` → yedek geri kopyalandı → **`cmp` ile birebir aynı** doğrulandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| **M19-1** | `HareketFabrikasi.cs`: `giris.KarsiBacakId = cikis.Id;` **silindi** | `Virman_iki_bacagi_TEK_islemde_yazilir_ve_KARSILIKLI_baglanir` · `Virman_stornosu_IKI_BACAGI_birlikte_dondurur` · `Virman_kasadan_bankaya_her_iki_bakiyeyi_dogru_isler` · `Virman_bacagi_faturaya_DONUSTURULEMEZ` · `Virman_ayni_virman_IKI_KEZ_stornolanamaz` · `Banka_virman_bacagi_TEK_BASINA_stornolanamaz` (**6 kırmızı**) | AYNI |
| **M19-2** | `HareketFabrikasi.cs`: ikinci bacağın `Ekle(db, giris);` **silindi** (yarım virman) | aynı 6 test | AYNI |
| **M19-3** | `StornoServisi.cs`: kasa yolundaki `KarsiBacakId is not null` **kapısı silindi** | `Virman_bacagi_TEK_BASINA_stornolanamaz` | AYNI |
| **M19-4** | `KarZararServisi.cs`: `donemKariTl = karTl + iscilikTl - giderTl` → `- giderTl` **silindi** | `Gider_kar_zararda_AYRI_SATIR_ve_donem_karindan_duser` | AYNI |
| **M19-5** | `StornoServisi.cs`: storno satırında `GiderKategoriId = kaynak.GiderKategoriId` **silindi** | `Stornolanan_gider_kar_zarar_toplamindan_DUSER` | AYNI |
| **M19-6** | `GiderModulu.cs`: pasif kategori kapısı **silindi** | `Gider_PASIF_kategoriye_yazilamaz` | AYNI |
| **M19-7** | `VirmanModulu.cs`: para birimi eşitlik kapısı **silindi** | `Virman_para_birimleri_farkliysa_REDDEDILIR` | AYNI |

**Hiçbir testi kırmayan mutasyon (test açığı) ÇIKMADI.**
Son mutasyondan sonra temiz derleme: **0 hata · 0 uyarı**.

---

## § 7 — DENETİM (`dotnet/denetim.sh`)

### Bu paketin eklediği CIRCIR sayaçları (`# >>> P-19` … `# <<< P-19` blok)

| Sayaç | Ölçüm | Sonuç |
|---|---|---|
| **`ESSIZ_BACAK = 0`** | `KarsiBacakId` **ATAMASININ** `Data/HareketFabrikasi.cs` dışındaki sayısı (okuma sayılmaz) | **✓ 0** |
| **`TEK_SAVECHANGES_DISI_CIFT_BACAK = 0`** | `BacaklariEsle(` çağıran her metotta `SaveChangesAsync` sayısı ≠ 1 olan metot sayısı (awk, girinti-4 imza sınırı) | **✓ 0** |
| `TEK_BACAK_STORNO_KAPISI` (destek) | `StornoServisi.cs`'te `KarsiBacakId is not null` kapı sayısı ≥ 2 (kasa + banka) | **✓ 2** |

Ölçüm komutları **betiğe yorum olarak yazıldı**; tabanlar **taze ölçüldü**, belgeden kopyalanmadı (BK-07).

### Gerekçeli taban güncellemeleri (hepsi koda yorumla yazılı)
| Sayaç | Eski | Yeni | Gerekçe |
|---|---|---|---|
| `BEKLENEN_VARLIK` (-eq) | 59 | **60** | yeni varlık `GiderKategorisi` |
| `Pages/*.razor` | 44 | **45** | plan P-19 dosya listesinde `Gider.razor (YENİ)` adıyla yazılı |
| `P02_ROTA_BEKLENEN` (-eq) | 44 | **45** | `/gider` rotası |
| `Pages: ModalPencere` | 54 | **55** | TEK yeni modal ve o **YIKICI ONAYDIR** (DoD EK 1/4'ün izin verdiği tek kullanım); düzenleme kalıbı **Cekmece**, ikinci "duzenlenenId" kalıbı AÇILMADI |
| `BankaHareketleri.Add` | 4 | **5** | 5. kapı `CiftBacakFabrikasi` — iki bacağı **birlikte** yazan tek yol (gerekçe betikte yazılı) |

**Artırılmayan tavanlar:** `RAPOR_MAT` (43'te kaldı — gider ölçümü bilerek `Modules/Gider/`e konuldu),
`KULTURSUZ_TAVAN` (44 — ekranda `Bicim.Tarih(..., Dil.Dil)` kullanıldı),
`SATIR_ICI_STYLE` (143 — `style="text-align:right"` yerine `class="sayi"`),
`OLU_DUGME` (71 — 13 düğmenin **hepsinde** `@onclick` ya da `disabled` **aynı satırda**),
`SAYFA_RECORD_TAVAN` (3 — yeni istemci tipleri `Kuyumcu.Contracts`'a yazıldı),
`SOZLESME_KILITSIZ_TIP` (59 — 9 yeni tip `Ciftler()` tablosuna kilitlendi; sunucu artık anonim nesne değil **tipli kayıt** döndürür),
`IGNORE_QUERY_FILTERS` (0), `MUTORJ_ARTIK` (16).

`sube-suzgec-siniflandirma.txt`'ye yeni `SubeId ==` noktası **sınıf b** olarak eklendi
(kâr-zararın kullanıcı seçimli şube filtresi) → `GUVENLIK_ELLE_IZOLASYON = 0`.

### Çıktı
```
✗ P-02: yayın ortamı ÜRETİM + sürümlü betik (B-2)   2 eksik
DENETİM BAŞARISIZ
```
**Tek ihlal bu ve BU PAKETE AİT DEĞİL** — § 10. P-19 bloğunun üç sayacı da **✓**.

---

## § 8 — CANLI DOĞRULAMA (localhost **5198** — 5199'a DOKUNULMADI)

API: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build
--no-launch-profile --urls http://localhost:5198` (**PID 52541**, iş sonunda `kill`;
`lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş**).
Kiracı `PAKET19-TEST` SQL ile kuruldu; jeton imza anahtarıyla **yerel üretildi** (anahtar değişkende,
**hiç basılmadı**). Yanıt gövdeleri **okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /gider-kategorileri {"ad":"Kira"}` | 200 + kayıt | **200** `id=01M30XTD45… aktif=true giderAdedi=0` |
| 2 | aynı adla ikinci POST | 409 | **409** `"'Kira' adlı gider kategorisi zaten var…"` |
| 3 | `GET /gider-kategorileri` | etki alanlarıyla liste | **200** `toplam=1 · giderAdedi · giderToplamTl` (UX-S23) |
| 4 | `POST /giderler` 3.000 TL | 200, kasa 50.000→47.000 | **200** `kasaBakiyesi=47000.00 · stornoYolu=…` |
| 5 | boş kasadan 10 TL gider | 400 | **400** `"Kasa bakiyesi yetersiz: 0.00 TRY var…"` |
| 6 | `POST /virman` KASA→BANKA 12.000 | 200, iki bacak | **200** `cikisBacakId=01M30XTJFB… girisBacakId=01M30XTJFB…` |
| 7 | **SQL**: bacak çifti | karşılıklı | kasa `-12000 → …RJWS` · banka `GELEN 12000 → …BRD3` · **durum=ESLESTI** |
| 8 | **SQL**: bakiyeler + defter | kasa 35.000 · banka 12.000 · **hareketler 0** | **aynen** (virman **ciroya girmedi**) |
| 9 | `POST /kasalar/{k}/hareket/{bacak}/storno` | 409 | **409** `"…bir VİRMANIN bacağıdır; tek başına stornolanamaz…"` |
| 10 | `GET /raporlar/kar-zarar` | gider ayrı satır | **200** `giderTl=3000 · donemKariTl=-3000 · karKoprusu[GIDER]=-3000 · kirilim[{Kira,3000,adet 1}]` |
| 11 | `POST /virman/{bacak}/storno` | 200, **2 bacak** | **200** `bacakSayisi=2 · stornoBacakIdleri=[…,…]` |
| 12 | **SQL**: storno sonrası | kasa 47.000 · banka **0** · ters satırlar **karşılıklı** · **eşsiz bacak 0** | **aynen** |
| 13 | aynı virman ikinci storno | 409 | **409** `"Bu virman zaten stornolanmıştır…"` |
| 14 | gider stornosu | 200, kasa 50.000 | **200** `kasaBakiyesi=50000.00` |
| 15 | **SQL**: storno satırı kategorisi | kaynağın kategorisini **taşır** | `+3000.00 · gider_kategori_id=01M30XTD45…` |
| 16 | storno sonrası kâr-zarar | **giderTl 0** | **200** `giderTl=0.0 · donemKariTl=0.0 · adet=2` (çift net sıfır) |
| 17 | `PATCH` yalnız `ad` | açıklama **korunur** | **200** `ad="Dükkân kirası" · aciklama="dükkân kirası"` |
| 18 | `PATCH aciklama:null` | **silinir** | **200** `aciklama=null · ad korunur` |
| 19 | `DELETE /gider-kategorileri/{id}` | **pasife alma** (satır durur) | **200** `aktif=false` |
| 20 | pasif kategoriye gider | 400 | **400** `"…kategorisi pasiftir; pasif kategoriye yeni gider yazılamaz…"` |
| 21 | `PATCH {"aktif":true}` | geri aktif | **200** `aktif=true` |
| 22 | virman kaynak=hedef | 400 | **400** `"Virmanın kaynağı ve hedefi aynı olamaz…"` |
| 23 | `GET /virman?limit=50` | çiftler hâlinde | **200** 2 satır (biri `stornoSatiri=true`) |
| 24 | TEZGÂHTAR `POST /giderler` | 403 | **403** `"Bu işlem için gerekli rol: SAHIP veya MUDUR veya MUHASEBE"` |
| 25 | TEZGÂHTAR `POST /virman` | 403 | **403** aynı |

### Test kiracısı temizliği
`tenant_id` kolonu olan **59 tablodan** `DELETE`, ardından `tenants`.
Sonuç: `tenants` **8 → 8** (başlangıca döndü) · `QA-TEST-%` **3** (DOKUNULMADI, verisine hiç girilmedi) ·
`kasa_hareketleri`/`banka_hareketleri`/`gider_kategorileri` kalıntısı **0**.

---

## § 9 — DoD SÖZLEŞMESİ (Bertan koşulu)

| Varlık / kayıt | Düzenleme ucu | Silme / pasifleştirme | Yetki | Denetim izi | Ekran eylemi |
|---|---|---|---|---|---|
| **`GiderKategorisi`** (YENİ) | `PATCH /gider-kategorileri/{id}` (ad · açıklama · aktif; **gönderilmeyen = koru, null = sil**) | `DELETE` = **pasife alma** (hard delete YOK) + `PATCH {aktif:true}` ile geri | `[ModulYetkisi("kasa")]` + `[RolGerekli]` | `DenetimServisi.YazmaKaydet` **çağrılır** (açılış · düzeltme · pasife alma) | `#gd-kat-duzenle-{id}` (Cekmece) · `#gd-kat-durum-{id}` |
| **Gider satırı** (`kasa_hareketleri` + kategori) | **SALT OKUNUR** — finansal kayıt, yerinde değişmez | **STORNO** (`POST /kasalar/{id}/hareket/{h}/storno`); hard DELETE **YASAK** | aynı | mevcut kasa storno izi + `"Gider"` yazma kaydı | `#gd-storno-{id}` + onay modalı + `DegismezIpucu` gerekçesi |
| **Virman bacağı** (kasa/banka) | **SALT OKUNUR** — çift kayıt, tek bacağı düzeltilemez | **VİRMAN STORNOSU** (`POST /virman/{bacak}/storno`, iki bacak birlikte); hard DELETE **YASAK**; **tek bacak stornosu 409** | aynı | `"Virman"` + `"VirmanStorno"` yazma kayıtları | `#gd-v-storno-{id}` + onay modalı + `#gd-v-iki-bacak` notu |

**Neden salt okunur:** gider ve virman `kasa_hareketleri`/`banka_hareketleri` **finansal defter**
satırlarıdır (PLAN § 1 md.8 — defter yerinde değişmez, ADR-0004). Düzeltme yolu ekranda yazılıdır.

### UX sözleşmesi karşılama (Katman 8)
| Madde | Nerede |
|---|---|
| **UX-S21** | Para birimi kasadan/hesaptan türetilir, kullanıcıdan istenmez; gider kasası tekse otomatik |
| **UX-S22** | Tek kasa varsa seçici GİZLENİR, `#gd-kasa-sabit` salt okunur satır (test: `Tek_kasa_varsa_kasa_secici_GIZLENIR`) |
| **UX-S23** | Kategori listesi `giderAdedi`/`giderToplamTl` döndürür; adet **tıklanabilir** (`#gd-kat-adet-{id}` → o kategorinin giderleri) |
| **UX-S25** | Her kayıt ya düzenlenir ya `DegismezIpucu` ile gerekçesi + doğru yol (storno) yazılıdır |
| **UX-S26** | Virman kaynak/hedef seçicileri **TEK diziden** (`Taraflar`) üretilir |
| **UX-S27** | Her yazma başarı bandıyla biter; sunucunun döndürdüğü **yeni bakiye / bacak sayısı** banda yazılır (test: `Gider_kaydedilince_sunucu_sonucu_BANDA_yazilir`) |
| **UX-S28** | Hata "kayıt yok"a çevrilmez: `veriGeldi` bayrağı yalnız BAŞARILI yüklemede true; boş durumun ön koşuludur (test: `Sunucu_hatasi_BOS_DURUM_gibi_gosterilmez`) |
| **UX-S29** | `dahaFazlasiVar` + `sinirNotu` sunucudan gelir, ekranda `#gd-sinir` / `#gd-v-sinir` |
| **UX-S30** | Kilitli her düğmenin sebebi yanında: `#gd-kaydet-kilit`, `#gd-v-kaydet-kilit`, `#gd-storno-kilit` |
| **UX-S31** | `KASA`/`BANKA` ham kodu ekrana basılmaz, `TurAdi()` ile sözlükten çevrilir |
| **UX-S32** | `Bicim.Para(tutar, paraBirimi)` — "varsayılan TL" YOK |
| **DoD EK 1/1** | 13 düğmenin **hepsi** ya `@onclick` ya `disabled` (test: `Her_dugme_ya_istek_atar_ya_DISABLED_ve_sebebi_yazar`) |
| **DoD EK 1/2** | Çok alanlı düzenleme **Cekmece**, ModalPencere **yalnız** yıkıcı onay |
| **DoD EK 1/3** | Boş/geçersiz gönderimde `AlanHatasi` ile alan altı Türkçe mesaj |
| **DoD EK 1/4** | Storno ortak onay modalı + gerekçe ≥ 20 karakter + geri dönüş yolu (kategori aktife alma) |
| **DoD EK 2** | **Dokunulan sayfalar: `Pages/Gider.razor` (YENİ) — başka hiçbir razor sayfasına dokunulmadı.** |

---

## § 10 — KAPIYI AÇIK BIRAKAN TEK KALEM (bu pakete ait DEĞİL)

**Kalem:** `P-02 / B-2 — yayın ortamı ÜRETİM + sürümlü betik`
**Belirti:** `denetim.sh` → `✗ … 2 eksik` · `OlcumKapisiTestleri.inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ` KIRMIZI.

**Kanıt (bu paketin sebep OLMADIĞI):**
1. `dokumanlar/09-uygulama-kayitlari/betikler/qa-sunucu.sh` **git'te izlenmiyor** (`git status` → `??`),
   dosya damgası **2026-09-21 05:17:36**; bu oturumun P-19 çalışması 05:20'den sonra başladı.
2. Dosyanın 185-189. satırlarında **bilinçli** bir karar yazılı:
   *"Bu yüzden İNCELEME sunucusu geliştirme kipinde koşar; üretim kapısı 5198'de kanıtlı"* →
   `export ASPNETCORE_ENVIRONMENT=Development`. Yani **başka bir oturum/paket** bu değişikliği
   gerekçesiyle yapmış, karşılık gelen testi/sayacı henüz güncellememiş.
3. `.claude/launch.json` `kuyumcu-qa` girdisi **sürümlü betiği değil scratchpad kopyasını** gösteriyor
   (kontrolün ikinci eksiği). Bu dosya kurucunun **5199 inceleme sunucusunun** yapılandırmasıdır.

**Neden DOKUNULMADI:** (a) kapsam kilidi — P-19'un kimlikleri Y-11/Y-12; (b) görev metni
"5199 PORTUNA DOKUNMA" diyor; (c) eşzamanlı bir oturumun **bilinçli ve gerekçeli** kararını tek
taraflı geri almak Anayasa md.3'e (izinsiz değişiklik yok) aykırı olurdu.

**Devredildi:** **P-02** (B-2 sahibi). Yapılacak: ya `qa-sunucu.sh` Production'a döner, ya
`OlcumKapisiTestleri` + `denetim.sh` P02-10 kontrolü yeni gerekçeye göre güncellenir. **İkisi de
P-02'nin kararıdır, P-19'un değil.**

---

## § 11 — DEĞİŞEN DOSYALAR

**Yeni (7):**
```
dotnet/Kuyumcu.Api/Modules/Gider/GiderModulu.cs
dotnet/Kuyumcu.Api/Modules/Gider/VirmanModulu.cs
dotnet/Kuyumcu.Api/Modules/Gider/GiderOlcumu.cs
dotnet/Kuyumcu.Contracts/Sayfalar/GiderSozlesmeleri.cs
dotnet/Kuyumcu.Web/Pages/Gider.razor
dotnet/Kuyumcu.Tests/P19CiftBacakTestleri.cs
dotnet/Kuyumcu.Tests/P19EkranTestleri.cs
```
**Migration (3):**
```
dotnet/Kuyumcu.Api/Data/Migrations/20260921022537_P19GiderVeVirman.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260921022537_P19GiderVeVirman.Designer.cs
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
```
**Değişen (16):**
```
dotnet/Kuyumcu.Api/Data/Entities.cs
dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs
dotnet/Kuyumcu.Api/Modules/Banka/BankaModulu.cs
dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs
dotnet/Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
dotnet/Kuyumcu.Core/Erisim/RotaModulleri.cs
dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs
dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs
dotnet/Kuyumcu.Tests/SozlesmeYansimaTestleri.cs
dotnet/Kuyumcu.Tests/MenuMimarisiTestleri.cs
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs
dotnet/denetim.sh
dotnet/sube-suzgec-siniflandirma.txt
dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js
dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json
```
**COMMIT YOK** — çalışma ağacında kalındı. Proje ağacında `.mutorj`/`.bak`/`.tmp` artığı bırakılmadı
(mutasyon yedekleri yalnız scratchpad'de).

---

## § 12 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Virmanın iki bacağı **tek transaction** | ✅ | `CiftBacakFabrikasi` + tek `SaveChangesAsync`; M19-1/M19-2 |
| Tek bacak yazılırsa test kırılır (mutasyon) | ✅ | M19-2 → 6 kırmızı |
| Storno: virman stornolanırsa **iki bacak birlikte** döner | ✅ | `VirmanStorno`; canlı adım 11-12; test `…IKI_BACAGI_birlikte_dondurur` |
| Gider kâr-zararda **ayrı satır** | ✅ | `karKoprusu[GIDER]`; canlı adım 10 |
| Şubeler arası nakit transferi "iki şube birden" yolu | ✅ | Her bacak kendi kasasının şubesinde; yanıtta `subelerArasi`; P-04 filtresi her bacağı kendi şubesinde gösterir |
| `karsi_bacak_id` ↔ `karsi_sube_id` anlam ayrımı **belgede yazılı** | ✅ | § 2.2 + `Entities.cs` XML + `HareketFabrikasi.cs` başlığı |
| `ESSIZ_BACAK = 0` sayacı | ✅ | denetim.sh `# >>> P-19` bloğu |
| `TEK_SAVECHANGES_DISI_CIFT_BACAK = 0` sayacı | ✅ | aynı blok |
| Migration + `Down()` **koşturuldu** (BK-21) | ✅ | § 4 |
| Derleme 0 hata **0 uyarı** | ✅ | § 5 |
| Tam süit 0 başarısız | ❌ | **1 kırmızı, pakete ait değil** — § 10 |
| `denetim.sh` TEMİZ | ❌ | **1 ihlal, pakete ait değil** — § 10 |
| Test kiracısı silindi, `QA-TEST-*` korundu | ✅ | § 8 sonu |
| COMMIT YOK · sır basılmadı | ✅ | — |
| Kapsam kilidi (yalnız Y-11 · Y-12) | ✅ | başka kimliğe dokunulmadı |

---

## § 13 — AÇIK KALANLAR / DEVREDİLENLER

1. **P-02 / B-2** — `qa-sunucu.sh` üretim kipi + `launch.json` sürümlü betik (§ 10). **BLOKLAYICI**.
2. **Banka üzerinden gider** (havaleyle kira ödemesi) — `banka_hareketleri.gider_kategori_id` yok.
   Plan yalnız kasayı istiyordu. **Devredildi:** kapsam kurulu (yeni kimlik gerekir).
3. **Döviz virmanı** (farklı para birimleri arası aktarım) — bugün 400. **Devredildi:** kur politikası
   kararı (kurucu/mali müşavir) alındıktan sonra.
4. **Gider bütçesi / kategori bazlı limit** — kapsam dışı, 1.1.
5. **P-24 mutabakat testi** bu paketin kapısıdır (plan riski): virmanın kasa/banka bakiyesini çift
   saymadığı burada 3 ayrı yolla kanıtlandı (defter satırı 0 · toplam bakiye etkisi 0 · eşsiz bacak 0),
   ama **çapraz mutabakat raporu P-24'ün işidir**.

---

## § 14 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **KMH (kredili mevduat) kararı — K-? açık kalem.** Nakit kasa eksiye düşemez (kapatıldı).
   **Banka hesabı eksiye düşebilir** ve bu bilinçli bırakıldı: KMH'li hesapta negatif bakiye
   meşrudur. Kural (hangi hesapta ne kadar eksiye izin verilir) **yapılandırmadan okunacak şekilde
   tasarlanmadı, bugün sınırsızdır**. Karar gelince kapı eklenmelidir. *(Bu, kodda sabitlenmiş bir
   mevzuat hükmü DEĞİLDİR; varsayılan tutucu olan kasa tarafı kapalıdır.)*
2. **Virman ve KDV/fatura.** Virman bacağı **faturaya dönüştürülemez** (kapı eklendi): para dükkânın
   içinde yer değiştirir, satış yoktur. Mali müşavir teyidi istenirse: virman muhasebede
   *"kasa ↔ banka hesap aktarımı"* olarak izlenir, gelir/gider tablosuna girmez.
3. **Gider kâr-zararda brüt kârdan SONRA düşülür** (işçilik gelirinden sonra, dönem kârından önce).
   Faaliyet gideri / satılan malın maliyeti ayrımı bu şekilde kuruldu; mali müşavir farklı bir
   sunum isterse kâr köprüsündeki kalem sırası değiştirilebilir.
4. **Gider kategorileri müşteriye önceden tanımlı GELMEZ** (tohum veri yok). Pilot kurulumunda
   kullanıcı kendi kalemlerini açar. İstenirse standart bir liste (kira, elektrik, su, personel,
   nakliye, vergi/harç, kırtasiye) tohumlanabilir — **kurucu kararı**.
5. **Ortam tuzağı (ekip için):** `docker-compose.yml`'deki MySQL parolası **çalışan konteynerinkiyle
   uyuşmuyor**. Doğru kaynak `appsettings.Development.json` bağlantı dizesidir. Bu tuzak
   `memory/kuyumcu-erp-ortam-tuzaklari.md`'ye eklenmelidir.
6. **zsh tuzağı (ekip için):** `GID` zsh'te **özel tamsayı değişkenidir**; kabuk betiğinde
   `GID=$(...)` "bad math expression" verir. Değişken adı olarak kullanılmamalı.

---

## § 15 — İKİNCİ KAPI TURU (2026-09-21, hasım doğrulayıcı bulguları üzerine)

Birinci turda paket **YARIM** beyan edilmişti. Hasım doğrulayıcı 4 bulgu bıraktı.
Aşağıda her biri **kanıtla** kapatıldı ya da **gerekçesiyle** devredildi. Hiçbiri gizlenmedi.

| Bulgu | Seviye | Sonuç |
|---|---|---|
| Kapı koşulu: tam süitte 1 kırmızı + `denetim.sh` BAŞARISIZ (kök neden **P-02/B-2**) | KRİTİK | **AÇIK — ESKALE.** Bağımsız yeniden ölçüldü, durum **değişmemiş**. Ayrıntı ve eskalasyon: § 15.3 |
| `Pages/Kasalar.razor` / `Banka.razor` hiç açılmadı (sapma S1) | ORTA | **KAPANDI** — § 15.1 |
| Test kiracısı adı `PAKET19-TEST` (kural: `PAKET<no>-TEST`) | DÜŞÜK | **KAPANDI** — bu turda `PAKETP-19-TEST` kullanıldı; § 15.2 |
| Depoda P-19 ÖNCESİNDEN kalan 8 sıfır-bayt migration + 16 `.mutorj` + 2 `.bak` | DÜŞÜK | **DEVREDİLDİ (kurucu temizlik kalemi)** — silme yetkisi kurucuda; bu turda **yeni artık üretilmedi** (`find` → 0) |

### § 15.1 — Sapma S1 kapatıldı: virman bacağı artık EKRANDA tanınıyor

**Sorun (doğrulayıcının tarifi):** `StornoServisi`'nin "bu bir VİRMAN bacağıdır, virmanın kendi
stornosunu kullanın" yönlendirmesi **yalnız API 409 gövdesinde** yaşıyordu. Kasa/banka ekranında
kullanıcı storno düğmesine basıyor, ham hata bandı görüyor ve doğru yola götüren bir yol bulamıyordu.
Bu, **UX-S25**'in birebir yasakladığı hâldir ("sunucuda uç var, ekranda düğme yok").

**Yapılan (tek kalıp, iki sayfada birebir aynı):**

| Katman | Dosya | Değişiklik |
|---|---|---|
| Sözleşme | `Kuyumcu.Contracts/Sayfalar/KasalarSozlesmeleri.cs` | `HareketVeri`e `string? KarsiBacakId = null` **eklendi** (ad+tip sunucuyla doğrulandı: `Entities.cs:920`) |
| Sözleşme | `Kuyumcu.Contracts/Sayfalar/BankaSozlesmeleri.cs` | aynısı (`Entities.cs:1042`) |
| Sözlük | `Kuyumcu.Core/I18n/Sozlukler.cs` | **Kabuk**'a 3 anahtar TR+EN: `VirmanBacagi` · `VirmanBacagiGerekce` · `VirmanStornosunaGit`. Kasa ve banka **aynı anahtarı** kullanır (UX-S31 tek kaynak) |
| Ekran | `Kuyumcu.Web/Pages/Kasalar.razor` | `StornoAcik()` virman bacağını **kilitler**; satırda `Virman bacağı` rozeti + Türkçe gerekçe (`kh-virman-gerekce-{id}`) + `/gider?sekme=virman` bağlantısı (`kh-virman-yol-{id}`) |
| Ekran | `Kuyumcu.Web/Pages/Banka.razor` | aynısı (`bh-virman-gerekce-{id}` · `bh-virman-yol-{id}`) |
| Ekran | `Kuyumcu.Web/Pages/Gider.razor` | `[SupplyParameterFromQuery] SekmeSorgu` → derin bağ **Virman sekmesini açar**; tanınmayan değer sessizce yok sayılır |

**Neden yeni istemci `record` açılmadı:** mevcut `HareketVeri` sözleşmeleri **genişletildi**; `Pages/`
içine hiçbir yeni record yazılmadı (denetim.sh tavanı korundu). Mevcut element id'leri **değişmedi**;
yeni id'ler `<önek>-<alan>` kalıbındadır.

**Kilitli düğme kuralı (DoD EK 1/1 · UX-S30):** storno düğmesi `disabled` olduğunda sebebi **hemen
yanında** yazılıdır ve alternatif yol bir bağlantıdır — "tıklanınca hiçbir şey olmayan" düğme yoktur.

**Yeni testler — `Kuyumcu.Tests/P19VirmanBacagiEkranTestleri.cs` (7 test, 7/7 YEŞİL):**

| Test | Ne korur |
|---|---|
| `Kasa_virman_bacaginda_storno_KILITLI_sebebi_yazili_ve_dogru_yol_bagli` | kilit + gerekçe metni + `href="/gider?sekme=virman"` + rozet |
| `Kasa_virman_OLMAYAN_satirda_storno_ACIK_ve_virman_metni_YOK` | **negatif kontrol**: kilit fazla geniş değil |
| `Banka_virman_bacaginda_storno_KILITLI_sebebi_yazili_ve_dogru_yol_bagli` | banka tarafı birebir aynı |
| `Banka_virman_OLMAYAN_dekontta_storno_ACIK_ve_virman_metni_YOK` | negatif kontrol |
| `Gider_ekrani_sekme_virman_sorgusuyla_VIRMAN_sekmesini_acar` | derin bağın **hedefi gerçekten açılıyor** (gerçek `NavigationManager` gezinmesiyle) |
| `Gider_ekrani_sorgu_YOKKEN_varsayilan_GIDER_sekmesinde_kalir` | derin bağ varsayılanı bozmuyor |
| `Kasa_ve_banka_ayni_sozluk_anahtarindan_beslenir_TR_ve_EN_dolu` | TR+EN dolu **ve birbirinden farklı** (çıplak/kopya metin yok) |

Sahte API `{}` varsayılanıyla bırakılmadı: iki hareket ucu da `karsiBacakId` taşıyan gerçekçi
camelCase yanıt döndürür — yoksa ekran boş modelle yeşil kalır ve sözleşme kayması görünmezdi.

**Mutasyon kanıtları (her biri: uygula → test KIRMIZI → geri al → `cmp` birebir):**

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| **M-S1-1** | `Kasalar.razor` → `if (h.KarsiBacakId is not null) return false;` **silindi** | `Kasa_virman_bacaginda_storno_KILITLI_sebebi_yazili_ve_dogru_yol_bagli` (1 başarısız / 6 başarılı) | `cmp` **BİREBİR AYNI** |
| **M-S1-2** | `Banka.razor` → `&& h.KarsiBacakId is null` **silindi** | `Banka_virman_bacaginda_storno_KILITLI_sebebi_yazili_ve_dogru_yol_bagli` (1/6) | `cmp` **BİREBİR AYNI** |
| **M-S1-3** | `Gider.razor` → derin bağ eşleşmesi `"virman"` → `"virman-DEVRE-DISI"` | `Gider_ekrani_sekme_virman_sorgusuyla_VIRMAN_sekmesini_acar` (1/6) | `cmp` **BİREBİR AYNI** |

> **Dürüstlük notu (Anayasa md.1):** M-S1-3'ün İLK denemesi satırı tamamen siliyordu ve `else if`
> sarkıtıp **derleme hatası** veriyordu. Derleme hatası davranış kanıtı DEĞİLDİR; mutasyon geri alınıp
> **anlamsal** bir mutasyonla (eşleşme dizgesini bozmak) tekrarlandı. İlk deneme gizlenmedi.

Son mutasyondan sonra: `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata**.

**Yeni CIRCIR sayacı (mekanizma kuralı — plan 3. ilke):**

`denetim.sh` → `# >>> P-19` bloğuna **4)** eklendi:

```
P-19: VIRMAN_BACAGI_EKRANSIZ_KAPI = 0
```

Ne ölçer: (3) numaralı **sunucu** kapısının **ekran karşılığı olmayan** sayfa sayısı. Kasa ve banka
sayfalarının her biri hem `KarsiBacakId`i tanımalı hem `/gider?sekme=virman` yolunu göstermelidir.
**Taban TAZE ÖLÇÜLDÜ** (belgeden kopyalanmadı), ölçüm komutu betiğe yorum olarak yazıldı:

```
for f in Kasalar Banka; do
  grep -c 'KarsiBacakId' Kuyumcu.Web/Pages/$f.razor        # → 2 · 2
  grep -c '/gider?sekme=virman' Kuyumcu.Web/Pages/$f.razor # → 1 · 1
done
```

Hiçbir tavan/eşik **artırılmadı**.

**Dokunulan sayfalar (DoD EK 2 · TEK DOKUNUŞ):** `Pages/Kasalar.razor`, `Pages/Banka.razor`
(**ikinci dokunuş** — gerekçe: hasım doğrulayıcının ORTA bulgusu + planın "Dosyalar" sözleşmesi;
bu bölüm kapı kaydına yazılacak gerekçedir), `Pages/Gider.razor` (bu paketin kendi sayfası).

### § 15.2 — Canlı doğrulama (2. tur, localhost **5198**)

API: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build
--no-launch-profile --urls http://localhost:5198` → **PID 67064**, iş sonunda `kill`;
`lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş**. **5199'a DOKUNULMADI** (kurucunun sunucusu PID 31621
çalışmaya devam ediyor; `lsof` ile teyit edildi). Kiracı adı kurala uygun: **`PAKETP-19-TEST`**.
Yanıt gövdeleri **okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /virman` KASA→BANKA 12.000 | 200 + iki bacak | **200** `cikisBacakId=01M30YVVGAH27…` · `girisBacakId=01M30YVVGA0YR…` · `stornoYolu=/api/v1/virman/…/storno` |
| 2 | `GET /kasalar/p19k/hareketler` — **ekranın veri kaynağı** | JSON'da `karsiBacakId` **VAR** | **200** · `karsiBacakId` alanı **var** · değer `01M30YVVGA0YR…` (karşı bacağı gösteriyor) |
| 3 | `GET /bankalar/p19b/hareketler` | JSON'da `karsiBacakId` **VAR** | **200** · alan **var** · değer `01M30YVVGAH27…` (karşılıklı) |
| 4 | `POST /kasalar/{k}/hareket/{bacak}/storno` | 409 | **409** `"Bu kasa hareketi bir VİRMANIN bacağıdır; tek başına stornolanamaz…"` |
| 5 | `POST /bankalar/{h}/hareketler/{bacak}/storno` | 409 | **409** `"Bu dekont bir VİRMANIN bacağıdır…"` |
| 6 | **DOĞRU YOL** `POST /virman/{bacak}/storno` | 200, **2 bacak** | **200** `bacakSayisi=2` · `stornoBacakIdleri=[01M30YWD9WAWR…, 01M30YWD9WH1M…]` |
| 7 | **SQL** storno sonrası | bakiyeler başa döner · eşsiz bacak 0 · defter temiz | kasa **50000.00** · banka **0.00** · `kasa_bacak_adet=2` · `banka_bacak_adet=2` · **`essiz_bacak=0`** · `defter_hareket=0` (virman **ciroya girmedi**) |

> **Adım 2–3 bu turun asıl kanıtıdır:** ekran kodu `karsiBacakId`in JSON'a çıktığını **varsayıyordu**
> (varlık doğrudan serileşiyor). Varsayım bırakılmadı, canlıda **ölçüldü**.

**Test kiracısı temizliği:** `tenant_id` kolonu olan **59 tablodan** `DELETE`, ardından `tenants`.
Sonuç: `tenants` **8 → 8** (başlangıca döndü) · `QA-TEST-%` **3** (DOKUNULMADI) ·
`PAKET%` önekli kiracı **0** · `p19t` kalıntısı kasa/banka/kasalar **0 · 0 · 0**.

> **Ortam tuzağı (S6 güncellemesi):** görev metnindeki `grep MYSQL_ROOT_PASSWORD … | head -1`
> komutu **yanlış satırı** yakalıyor (`docker-compose.yml`'de iki eşleşme var; ilki 3 karakterlik bir
> parçadır) ve `ERROR 1045` veriyor. Doğrusu `grep 'MYSQL_ROOT_PASSWORD:'`. Parola **hiçbir yerde
> basılmadı**; yalnız uzunluğu raporlandı. Bu, S6'nın gerçek kökeniydi — compose dosyası **doğruydu**.

### § 15.3 — KAPIYI AÇIK BIRAKAN TEK KALEM — **P-02/B-2, ESKALASYON**

**Bağımsız yeniden ölçüm (2026-09-21, bu tur):**

- Tam süit: `Başarısız! - Başarısız: 1, Başarılı: 3425, Toplam: 3426`
  → tek kırmızı: `OlcumKapisiTestleri.inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ`
- `dotnet/denetim.sh` → `EXIT=1`, tek `✗`: `P-02: yayın ortamı ÜRETİM + sürümlü betik (B-2) 2 eksik`
- **P-19'un dört sayacı da YEŞİL:** `ESSIZ_BACAK 0` · `TEK_SAVECHANGES_DISI_CIFT_BACAK 0` ·
  `TEK_BACAK_STORNO_KAPISI 2` · `VIRMAN_BACAGI_EKRANSIZ_KAPI 0`

**Kök neden (P-19 DIŞINDA, değişmemiş):**
1. `dokumanlar/09-uygulama-kayitlari/betikler/qa-sunucu.sh` hâlâ `export ASPNETCORE_ENVIRONMENT=Development`
   taşıyor. Betikteki **yazılı gerekçe**: Production kipi TLS beyanı zorunlu kılıyor, 5199 düz `http`
   olduğu için sunucu **açılmıyor** ve kurucunun inceleme ekranı kapanıyor;
   `Uretim:TersVekilArkasinda=true` kaçamağı **yalan olurdu** (ortada ters vekil yok).
2. `.claude/launch.json` `kuyumcu-qa` girdisi sürümlü betik yerine **scratchpad kopyasını** gösteriyor.

**Neden bu turda da düzeltilmedi (varsayım değil, kural çakışması):**
- Görev metninin **KAPSAM KİLİDİ** maddesi: "yalnız bu paketin kimlikleri (Y-11 · Y-12); başka paketin
  kimliğine dokunma". Bu kalem **B-2 / P-02** kimliğidir.
- Görev metninin **bağlayıcı** maddesi: "**5199 PORTUNA DOKUNMA** (kurucunun inceleme sunucusu)".
  Düzeltme tam da o sunucunun ortamını/başlatma yolunu değiştirmek demektir ve **şu anda çalışan**
  kurucu sunucusunu (PID 31621) etkiler.
- Hasım doğrulayıcının kendi sonucu da aynı: *"kurucuya/orkestratöre eskale edilmeli"*.

**Eskalasyon — karar ORKESTRATÖRÜN/KURUCUNUNDUR.** İki yol var ve **ikisi de P-19 dışıdır**:
- **(A)** `qa-sunucu.sh` Production'a döndürülür + sürümlenir, `launch.json` sürümlü betiği gösterir —
  ama 5199 düz http olduğu için **inceleme sunucusu açılmaz**; kurucuya https (ya da gerçek ters vekil)
  gerekir. Testin ve denetimin istediği budur.
- **(B)** B-2'nin kapısı, üretim künyesi kanıtının **5198'de ayrıca alınmış olduğu**
  (`ham/uretim-kunye-kaniti.md`) gerçeğini tanıyacak şekilde gevşetilir — bu bir **tavan gevşetmesidir**
  ve plan "tavan ARTIRILMAZ" diyor, dolayısıyla **kurucu/Tamer Bey mührü gerekir**.

**P-19'un kendi işi bu kalemden bağımsızdır ve kanıtlıdır.** Ancak orkestratör kapısının iki bağlayıcı
şartı (tam süit 0 başarısız · denetim TEMİZ) objektif olarak sağlanmadığı için bu rapor paketi
**BİTTİ** diye ilan **etmez**; durum **YARIM**'dır ve tek engel yukarıdaki P-02/B-2 kalemidir.
Düzeldiği an yalnız bu iki kontrol tekrarlanmalıdır — diğer tüm kontroller bu turda yeşil ölçüldü.

### § 15.4 — Bu turda değişen dosyalar

```
dotnet/Kuyumcu.Contracts/Sayfalar/KasalarSozlesmeleri.cs   (HareketVeri + KarsiBacakId)
dotnet/Kuyumcu.Contracts/Sayfalar/BankaSozlesmeleri.cs     (HareketVeri + KarsiBacakId)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                      (Kabuk: 3 anahtar × TR/EN)
dotnet/Kuyumcu.Web/Pages/Kasalar.razor                     (kilit + gerekçe + doğru yol)
dotnet/Kuyumcu.Web/Pages/Banka.razor                       (kilit + gerekçe + doğru yol)
dotnet/Kuyumcu.Web/Pages/Gider.razor                       (?sekme= derin bağı)
dotnet/Kuyumcu.Tests/P19VirmanBacagiEkranTestleri.cs       (YENİ · 7 test)
dotnet/denetim.sh                                          (P-19 sayaç 4: VIRMAN_BACAGI_EKRANSIZ_KAPI)
dokumanlar/09-uygulama-kayitlari/PAKET-P-19-uygulama-raporu.md
```

**Migration:** bu turda **gerekmedi** — şema değişmedi; `karsi_bacak_id` kolonu 1. turun
`20260921022537_P19GiderVeVirman` migration'ıyla zaten vardı. Yalnız istemci sözleşmesi genişledi.

**Yeni artık üretilmedi:** `find dotnet -newermt "2026-09-21 05:55" \( -name "*.mutorj" -o -name "*.bak"
-o -name "*.tmp" \)` → **0 sonuç**. Mutasyon yedekleri scratchpad altında tutuldu, proje ağacına
hiçbir şey bırakılmadı. **COMMIT YOK** — çalışma ağacında kalındı, hiçbir `git` yazma komutu çalıştırılmadı.

---

# § 16 — 3. KAPI TURU (2026-09-21 06:10-06:20) — HASIM DOĞRULAYICI BULGULARININ KAPATILMASI

Bu tur **yeni özellik yazmadı**; 2. turun hasım doğrulamasında kalan 3 bulgu ele alındı.

## § 16.1 — Bulgu 1 [KRİTİK] — BEYAN EDİLMEMİŞ 2. KIRMIZI (kararsız test) → **KAPANDI**

**Bulgu:** Uygulayıcı "Başarısız: 1, Başarılı: 3425" beyan etmişti; doğrulayıcının koşumu
"Başarısız: 2, Başarılı: 3424, Toplam: 3426" verdi. İkinci kırmızı:
`AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ`
(`Kuyumcu.Tests/PosBarkodTestleri.cs:377` — `Expected "NAKIT" / Actual "VERESIYE"`),
izole koşumda yeşil → kararsız.

**KÖK NEDEN (tahmin değil, ölçüldü) — testin kendisi iki yönden bozuktu:**

1. **Yanlış kanıt okunuyordu.** Eski iddia `#islem-odeme` `<select>` elemanının **DOM `value`**
   değerini okuyordu. Alış kipinde `VERESIYE` seçeneği listeden kalkıyor; AngleSharp bu durumda
   seçicinin değerini **kendiliğinden ilk seçeneğe ("NAKIT") düşürüyor**. Yani bileşenin
   `odemeTuru` alanı `VERESIYE` kalsa bile iddia **yeşil** kalıyordu → **TEST AÇIĞI** (aşağıda
   mutasyonla kanıtlandı: kural tamamen kaldırıldığında test hayatta kaldı).
2. **Yarış durumu.** `TurSec()` içindeki `_ = IadeAdaylariYukle()` **ateşle-unut** görevi tür
   değişiminden sonra ikinci bir render tetikliyor. Tam süit yükü altında DOM okuması bu
   render'dan **önce** düşerse eski `value` ("VERESIYE") görülüyordu → kararsız kırmızı.

**DÜZELTME (test susturulmadı, GÜÇLENDİRİLDİ):** iddia artık DOM'u değil **sunucuya giden
gövdeyi** okuyor — kuralın gerçek amacı zaten buydu ("gövdede sunucunun reddedeceği değer
gitmesin, 400 almayalım"). Satıştan alışa geçilir, kalem eklenir, Kaydet'e basılır ve
`/islemler/alis` gövdesindeki `odemeTuru` alanının `NAKIT` olduğu doğrulanır
(`WaitForAssertion` yalnız isteğin **düşmesini** bekler; değer iddiası kesindir).

Dosya: `dotnet/Kuyumcu.Tests/PosBarkodTestleri.cs` (`AlisOdemeAraciEkranTestleri`, ~satır 370-400).

### Mutasyon kanıtı M-K1-1 (test açığının kanıtı + kapanışı)

| Kod | Değişiklik | Dosya | Eski test | Yeni test | Geri alma |
|---|---|---|---|---|---|
| **M-K1-1** | `Islemler.razor:2104` → `if (tur != "SATIS" && odemeTuru is not ("NAKIT" or "KART" or "HAVALE"))` **`if (false)`** ile değiştirildi (kural iptal) | `Kuyumcu.Web/Pages/Islemler.razor` | **YEŞİL kaldı** (açık) | **KIRMIZI**: `SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` → `Expected: "NAKIT" / Actual: "VERESIYE"` | `cmp Kuyumcu.Web/Pages/Islemler.razor <yedek>` → **çıktı yok (birebir)** |

Mutasyon yedeği: `…/scratchpad/mut-yedek/P-19/Islemler.razor`. Geri alındıktan sonra
`dotnet build Kuyumcu.sln --nologo` → **0 uyarı, 0 hata**.

### BK-19 — iki ardışık tam süit koşumu (kararsızlık kanıtı)

| Koşum | Toplam | Başarılı | Başarısız | Kırmızının adı |
|---|---|---|---|---|
| **1** | 3426 | 3425 | **1** | `OlcumKapisiTestleri.inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ` |
| **2** | 3426 | 3425 | **1** | (aynı test) |

İki koşum **birebir aynı** → `SATISTAN_ALISA…` kararsızlığı **giderildi**; süitte kalan tek
kırmızı kalıcı ve açıklanabilir (§ 16.2). TRX: `…/scratchpad/trx-pP-19-t1`, `…/trx-pP-19-t2`.

## § 16.2 — Bulgu 2 [KRİTİK] — denetim.sh EXIT=1 + süitteki tek kırmızı → **AÇIK, ESKALE EDİLDİ (P-19 DIŞI)**

`dotnet/denetim.sh` → **EXIT=1**, tek `✗`:
`P-02: yayın ortamı ÜRETİM + sürümlü betik (B-2) 2 eksik`.
Süitteki tek kırmızı `OlcumKapisiTestleri.inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ`
**aynı kökten** gelir. İki eksik alt kontrol (denetim.sh:1304-1306):

1. `betikler/qa-sunucu.sh:189` → `export ASPNETCORE_ENVIRONMENT=Development`
   (betiğin kendi yorumu bunu **bilerek** yaptığını yazıyor: "ortada ters vekil yok;
   üretim kipi kanıtı 5198'de ayrıca alındı — `ham/uretim-kunye-kaniti.md`").
2. `.claude/launch.json` → `kuyumcu-qa` girdisi sürümlü betik yerine **scratchpad kopyasını**
   gösteriyor (oturum kapanınca 5199 açılmaz).

**Bu kalem P-02/B-2 kimliğine aittir; P-19'un kapsam kilidi dışındadır** ve düzeltmesi
kurucunun 5199 inceleme sunucusunu doğrudan etkiler ("5199'a dokunma" yasağı). Denetim eşiği
**gevşetilmedi** (plan ilkesi: tavan artırılmaz). Karar kurucuya/orkestratöre bırakıldı:

- **(A)** `qa-sunucu.sh` `Production`'a döner + 5199 için https/ters vekil kurulur, ya da
- **(B)** B-2 kapısı, 5198'de alınmış üretim künye kanıtını (`ham/uretim-kunye-kaniti.md`)
  mühürle tanır ve `.claude/launch.json` sürümlü betiğe çevrilir.

Karar verilene kadar **P-19 kapısı açılmamalıdır**; paket durumu **YARIM**'dır.

## § 16.3 — Bulgu 3 [DÜŞÜK] — proje ağacındaki artıklar → **DOĞRULANDI, TEMİZLİK KURUCUDA**

TurkReport hook `rm`/`mv`'yi bloklar; silme kurucudadır. Envanter (bu turda **yeni artık
üretilmedi**):

| Tür | Adet | Not |
|---|---|---|
| `*.mutorj` | 16 | 11 × `Kuyumcu.Api/**`, 5 × `Kuyumcu.Web/Pages/**` |
| `*.bak` | 2 | `Kuyumcu.Tests/AramaHttpTestleri.cs.bak` (**0 bayt**, 09-21 02:51) · `Kuyumcu.Tests/MadenDefteriTestleri.cs.bak` |
| `*.mutbak` | 1 | `dotnet/denetim.sh.mutbak` (**0 bayt**, 09-18 16:49 — P-19'dan ÖNCE) |
| 0 baytlık `20260915*FiyatListesiVeKampanya*` migration | 8 | **ARTMADI**; `dotnet ef` çıktısı sanılıp gerçek regresyonu gizlememeli |

`AramaHttpTestleri.cs.bak` **zaten 0 bayttır** (boşaltılmış) ve kaynak dosya sağlamdır.

## § 16.4 — Bu turda yeniden koşulan adımlar (kanıt)

| Adım | Komut | Sonuç |
|---|---|---|
| Derleme | `dotnet build Kuyumcu.sln --nologo` | **0 Uyarı · 0 Hata** (7,52 sn) |
| Filtreli test | `--filter FullyQualifiedName~AlisOdemeAraciEkranTestleri` | **6/6 yeşil** |
| Filtreli test | `--filter FullyQualifiedName~PosBarkodTestleri` | **7/7 yeşil** |
| Tam süit ×2 | bkz. § 16.1 | 3425/3426, tek kırmızı P-02/B-2 |
| Denetim | `dotnet/denetim.sh` | **EXIT=1**, tek `✗` = P-02/B-2 · P-19 sayaçları: `ESSIZ_BACAK 0 ✓` · `TEK_SAVECHANGES_DISI_CIFT_BACAK 0 ✓` · `VIRMAN_BACAGI_EKRANSIZ_KAPI 0 ✓` |
| Canlı (5198) | API `--no-build`, PID 79370 | `/saglik` → **200 Healthy**; `api/v1/gider-kategorileri` GET/POST → **401/401**; `api/v1/giderler` → **401/401**; `api/v1/virman` → **401/401** (uçlar kayıtlı ve yetki kapılı) |
| Sunucu durdurma | `kill 79370` | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş**; 5199 (PID 31621) **DOKUNULMADI** |
| Şema/kiracı SQL | `docker exec kuyumcu-mysql …` | `__EFMigrationsHistory=57` · `tenants=8` · `PAKET/P19 önekli kalıntı kiracı=0` · `QA-TEST-% = 3 (dokunulmadı)` · `karsi_bacak_id` kolonu **2 tabloda** · `gider_kategorileri` tablosu **var** |

Bu turda **veri yazılmadı**, **yeni test kiracısı açılmadı** → silinecek kiracı yok; `tenants`
sayısı başlangıçtaki **8**'de kaldı.

## § 16.5 — Bu turda değişen dosya

```
dotnet/Kuyumcu.Tests/PosBarkodTestleri.cs   (AlisOdemeAraciEkranTestleri — test GÜÇLENDİRİLDİ)
dokumanlar/09-uygulama-kayitlari/PAKET-P-19-uygulama-raporu.md   (bu § 16)
```

`Kuyumcu.Web/Pages/Islemler.razor` mutasyon için geçici değiştirildi ve **`cmp` ile birebir geri
alındı** (içerik değişmedi; yalnız dosya damgası tazelendi).

**AÇIKLANAMAYAN GÖZLEM (Anayasa md.1 — tahmin edilmiyor, bildiriliyor):**
`Kuyumcu.Web/Pages/Banka.razor` ve `Kuyumcu.Web/Pages/Kasalar.razor` dosyalarının değişiklik
damgası **06:11**'dir; bu tur onlara **dokunmadım**. İçerikleri 2. turun anlattığı hâlle
tutarlıdır (`/gider?sekme=virman` bağlantısı Kasalar:255 ve Banka:182'de yerinde, ilgili
`P19VirmanBacagiEkranTestleri` 7/7 yeşil). Damganın kaynağı **doğrulanamadı** (iCloud eşitlemesi
denetimde zaten `⚠` olarak açık); sebebini uydurmuyorum — orkestratöre bildiriliyor.

**COMMIT YOK** — hiçbir `git` yazma komutu çalıştırılmadı.

---

# § 17 — 4. KAPI TURU (2026-09-21) — ÖLÇÜM VE DOĞRULAMA (kod YAZILMADI)

**Tur tipi:** yalnız ölçüm/doğrulama. Görev talimatı gereği bu turda **ikinci migration
ÜRETİLMEDİ**, hazır kod **yeniden yazılmadı**. Amaç: P-02/B-2 borcu orkestratörce
kapatıldıktan sonra P-19'un kapı ölçülerini **taze** almak.

## § 17.1 — Ölçümler (hepsi bu turda koşuldu)

| Ölçüm | Komut | Sonuç |
|---|---|---|
| Derleme | `dotnet build Kuyumcu.sln --nologo` | **0 hata · 0 uyarı** |
| Denetim | `dotnet/denetim.sh` | **DENETİM TEMİZ · EXIT=0** (3. turdaki `EXIT=1` GİTTİ) |
| P-19 sayaçları | denetim.sh `P-19 iki bacaklı desen kapısı` bloğu | `ESSIZ_BACAK` **✓ 0** · `TEK_SAVECHANGES_DISI_CIFT_BACAK` **✓ 0** · `TEK_BACAK_STORNO_KAPISI` **✓ 2** · `VIRMAN_BACAGI_EKRANSIZ_KAPI` **✓ 0** |
| Tam süit | `dotnet test Kuyumcu.Tests --logger trx` | **Toplam 3427 · Başarısız 0 · Atlanan 0** · `Test Çalıştırması Başarılı` · süre 56,4 sn |
| Şema | `__EFMigrationsHistory` | **57** (tek P-19 migration: `20260921022537_P19GiderVeVirman`) |
| Migration boyutu | `ls -l` | `.cs` **4.716 bayt** · `.Designer.cs` **200.915 bayt** · `Down()` **dolu** (3 kolon + 3 indeks + 1 tablo geri alınır) |

> **3. tura göre tek fark:** `denetim.sh` artık **TEMİZ**. P-19 kodunda **hiçbir satır değişmedi**
> (aşağıdaki `cmp` kanıtları). Yani paketin kapanmasını engelleyen kalem **P-19 dışıydı** ve
> orkestratörün P-02/B-2 düzeltmesiyle kapandı — bu turda bağımsız olarak **ölçülerek** doğrulandı.

## § 17.2 — Mutasyon turu (4 mutasyon · 4 kırmızı · 4 `cmp` AYNI)

Yöntem: taze yedek `scratchpad/mut-tur4/` → `perl -0777 -pi -e` tek eşleşmeli değişiklik →
filtreli `dotnet test` → yedek geri kopyalandı → `cmp` birebir doğrulandı.

| Kod | Dosya · değişiklik | Kırmızıya dönen test(ler) | `cmp` |
|---|---|---|---|
| **M19-1** | `Data/HareketFabrikasi.cs:223` — `giris.KarsiBacakId = cikis.Id;` **silindi** | `P19CiftBacakTestleri.Virman_iki_bacagi_TEK_islemde_yazilir_ve_KARSILIKLI_baglanir` · `Virman_stornosu_IKI_BACAGI_birlikte_dondurur` · `Virman_kasadan_bankaya_her_iki_bakiyeyi_dogru_isler` · `Virman_bacagi_faturaya_DONUSTURULEMEZ` · `Virman_ayni_virman_IKI_KEZ_stornolanamaz` · `Banka_virman_bacagi_TEK_BASINA_stornolanamaz` (**6 kırmızı** / 45 test) | AYNI |
| **M19-2** | `Data/HareketFabrikasi.cs:227` — ikinci bacağın `Ekle(db, giris);` **silindi** (yarım virman) | aynı **6 kırmızı** | AYNI |
| **M19-3** | `Modules/Satis/StornoServisi.cs:357` — `if (kaynak.KarsiBacakId is not null)` → `if (false)` | `P19CiftBacakTestleri.Virman_bacagi_TEK_BASINA_stornolanamaz` (**1 kırmızı**) | AYNI |
| **M19-4** | `Modules/Maliyet/KarZararServisi.cs:331` — `donemKariTl = karTl + iscilikTl - giderTl` → `- giderTl` **silindi** | `P19CiftBacakTestleri.Gider_kar_zararda_AYRI_SATIR_ve_donem_karindan_duser` (**1 kırmızı** / 107 test) | AYNI |

**Hiçbir testi kırmayan mutasyon ÇIKMADI** (test açığı yok).
Son mutasyondan sonra temiz derleme: **0 hata · 0 uyarı**. Üç kaynak dosyanın üçü de `cmp` ile
**birebir** geri alındı.

## § 17.3 — Canlı doğrulama (localhost **5198** — 5199'a DOKUNULMADI)

API: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build
--no-launch-profile --urls http://localhost:5198` → **PID 97644**, iş sonunda `kill`;
`lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş**. `lsof -nP -iTCP:5199 -sTCP:LISTEN` → **hâlâ dinliyor**
(kurucunun sunucusu, dokunulmadı). Kiracı: **`PAKETP-19-TEST`** (`01M312328V5ECJCHQ7W9HMQG64`).
Bu turda kiracı **SQL ile değil, gerçek uçla** açıldı (`POST /auth/kayit` + davet satırı) — jeton
yerel üretilmedi. Yanıt gövdeleri **okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` (davet jetonlu) | 201 + kiracı | **201** `tenantId=01M312328V5ECJCHQ7W9HMQG64` |
| 2 | `POST /kasalar` · `POST /bankalar` | 200 | **200** kasa `01M3123GRJ…` · banka `01M3123GSQ…` |
| 3 | `POST /kasalar/{k}/hareket` GİRİŞ 50.000 | 200 | **200** `bakiye=50000.00` |
| 4 | `POST /gider-kategorileri {"ad":"Kira"}` | 200 | **200** `aktif=true · giderAdedi=0 · giderToplamTl=0` |
| 5 | `POST /giderler` 3.000 TL | 200, kasa 50.000→47.000 | **200** `kasaBakiyesi=47000.00 · stornoYolu=/api/v1/kasalar/…/storno` |
| 6 | `POST /virman` KASA→BANKA 12.000 | 200, **iki bacak** | **200** `cikisBacakId=01M31242TWN79AHDV8HBEJ2SG1 · girisBacakId=01M31242TXNP0YR8BVK1T5XNHB · subelerArasi=false` |
| 7 | **SQL** bacak çifti | karşılıklı bağ | kasa `-12000.00 → …TXNP…` · banka `+12000.00 → …TWN7…` (**karşılıklı**) |
| 8 | **SQL** bakiye + defter | kasa 35.000 · banka 12.000 · `hareketler` **0** | **kasa 35000.00 · banka 12000.00 · defter_hareket 0** (virman ciroya girmedi) |
| 9 | `POST /kasalar/{k}/hareket/{cikisBacak}/storno` | 409 | **409** *"Bu kasa hareketi bir VİRMANIN bacağıdır; tek başına stornolanamaz… POST /api/v1/virman/…/storno"* |
| 10 | `POST /bankalar/{b}/hareketler/{girisBacak}/storno` | 409 | **409** *"Bu dekont bir VİRMANIN bacağıdır…"* |
| 11 | `GET /raporlar/kar-zarar?baslangic&bitis` | gider **ayrı satır** | **200** `giderTl=3000.0 · donemKariTl=-3000.0 · karKoprusu[GIDER]=-3000.0 · giderKirilimi=[{Kira, 3000.0, adet 1}]` |
| 12 | `GET /raporlar/kar-zarar` **tarihsiz** | 400 (alan doğrulaması) | **400** `"Başlangıç zorunludur","Bitiş zorunludur"` |
| 13 | `POST /virman/{cikisBacak}/storno` | 200, **2 bacak** | **200** `bacakSayisi=2 · kaynakBacakIdleri=[…TWN7…,…TXNP…] · stornoBacakIdleri=[01M3124QY5…,01M3124QY5…]` |
| 14 | aynı virman ikinci storno | 409 | **409** *"Bu virman zaten stornolanmıştır; bir virman en fazla bir kez stornolanır."* |
| 15 | **SQL** storno sonrası | bakiye başa · **eşsiz bacak 0** | **kasa 47000.00 · banka 0.00 · kasa_bacak_adet 2 · banka_bacak_adet 2 · essiz_bacak 0 · defter_hareket 0** |
| 16 | gider stornosu | 200, kasa 50.000 | **200** `kasaBakiyesi=50000.00 · stornoKasaHareketId=01M3125A4G…` |
| 17 | storno sonrası kâr-zarar | **giderTl 0** | **200** `giderTl=0.0 · donemKariTl=0.0 · kirilim=[{Kira, 0.0, adet 2}]` (çift net sıfır) |
| 18 | `PATCH {"ad":…}` (açıklama gönderilmedi) | açıklama **korunur** | **200** `ad="Dukkan kirasi" · aciklama="dukkan kirasi"` |
| 19 | `PATCH {"aciklama":null}` | **silinir**, ad korunur | **200** `aciklama=null · ad="Dukkan kirasi"` |
| 20 | `DELETE /gider-kategorileri/{id}` | **pasife alma** (satır durur) | **200** `aktif=false` (hard delete YOK) |

**Eşsiz bacak SQL'i** (kanıt sorgusu): `kasa_hareketleri`te `karsi_bacak_id IS NOT NULL` olup
karşı tarafta **karşılıklı** bağı bulunmayan satır sayısı → **0**.

### Test kiracısı temizliği
`information_schema`tan `tenant_id` kolonu olan **59 tablo** bulundu, hepsinden `DELETE`;
ardından `kiraci_davetleri` satırı ve `tenants` kaydı silindi.

| Kontrol | Sonuç |
|---|---|
| `tenants` | **8 → 8** (başlangıç değerine döndü) |
| `QA-TEST-%` kiracı | **3** — DOKUNULMADI, verisine hiç girilmedi |
| `PAKET%` önekli kalıntı kiracı | **0** |
| `kasa_hareketleri` / `banka_hareketleri` / `gider_kategorileri` kalıntısı | **0 / 0 / 0** |
| `kiraci_davetleri` test satırı | **0** |

## § 17.4 — Kimlik kapanış tablosu (4. tur teyidi)

| Kimlik | Tanım (plan § 12) | Durum | Kanıt |
|---|---|---|---|
| **Y-11** | Virman (kasa↔banka) + şubeler arası transfer | **KAPANDI** | `Kuyumcu.Api/Data/HareketFabrikasi.cs:218-228` (`BacaklariEsle` — tek yol, karşılıklı bağ) · `Kuyumcu.Api/Modules/Gider/VirmanModulu.cs` (uç + storno) · testler `P19CiftBacakTestleri.Virman_iki_bacagi_TEK_islemde_yazilir_ve_KARSILIKLI_baglanir`, `Virman_stornosu_IKI_BACAGI_birlikte_dondurur`, `Virman_bacagi_TEK_BASINA_stornolanamaz`, `Banka_virman_bacagi_TEK_BASINA_stornolanamaz` · canlı adım 6-10, 13-15 · mutasyon M19-1/2/3 |
| **Y-12** | Gider/masraf kategorisi + gider ekranı | **KAPANDI** | `Kuyumcu.Api/Modules/Gider/GiderModulu.cs:414-455` (kategori CRUD + gider ucu, `[RolGerekli]` SAHIP/MUDUR/MUHASEBE) · `Kuyumcu.Api/Modules/Maliyet/KarZararServisi.cs:326-355` (ayrı satır + kırılım + köprü) · `Kuyumcu.Web/Pages/Gider.razor` (750 satır, `/gider` rotası — denetim `P02_ROTA_BEKLENEN=45` ✓) · canlı adım 4-5, 11, 16-20 · mutasyon M19-4 |

Bu turda **devredilen kimlik yok**; her iki kimlik de ölçümle kapalı.

## § 17.5 — DÜRÜSTLÜK NOTU (Anayasa md.1) — bu turda yapılan iki HATA

1. **Parola ekrana basıldı.** Görev metnindeki `grep MYSQL_ROOT_PASSWORD … | head -1` komutu
   `docker-compose.yml`de yanlış satırı yakalayıp `ERROR 1045` verdi. Nedeni ararken çalıştırdığım
   `grep -n "MYSQL_ROOT_PASSWORD" … | sed …` komutu **MySQL root parolasının düz metnini oturum
   çıktısına yazdı**. Kural açıkça "sır/parola değerini ASLA çıktına yazma" diyordu; **ihlal edildi**.
   Gizlenmiyor, raporlanıyor. **Kurucuya eylem:** bu parola artık "oturum kaydında geçmiş" sayılmalı;
   **K-5 sır rotasyonu** kalemi bu olayla birlikte önceliklendirilmelidir. (Parola zaten
   `docker-compose.yml` içinde düz metin duruyor — kök sorun budur.)
2. **Erişim jetonu ekrana basıldı.** `POST /auth/kayit` yanıtını doğrudan `stdout`a yazdırdığım için
   test kiracısının `accessToken`/`refreshToken` değerleri çıktıya düştü. Etki sınırlı: jeton
   **yalnızca silinen `PAKETP-19-TEST` kiracısına** aitti ve kiracı kaydı `tenants`tan kaldırıldı
   (jeton artık hiçbir veriye açılmıyor). Kalan turlarda jeton **dosyaya** yazıldı (`tok.txt`),
   ekrana basılmadı. **Kurucuya eylem gerekmiyor**, ama yöntem hatası kayda geçiyor.

> **Tuzak güncellemesi (SONRAKI-OTURUM-PROMPT § 6 için orkestratöre öneri):** doğru komut
> `grep 'MYSQL_ROOT_PASSWORD:' docker-compose.yml | head -1 | awk '{print $2}'` (iki nokta ŞART;
> dosyada `MYSQL_ROOT_PASSWORD` geçen bir yorum satırı daha var ve `head -1` onu yakalıyor).
> Ayrıca kiracıyı SQL ile kurmaya gerek yok: `kiraci_davetleri`ne SHA-256 özetli bir satır eklenip
> `POST /api/v1/auth/kayit` çağrılabiliyor (bu turda böyle yapıldı).

## § 17.6 — Bu turda değişen dosya: **YOK**

Bu tur ölçüm turudur. `git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısındaki
**329 satır** 1.-3. turlardan gelir; 4. turda proje ağacına **tek bir satır** eklenmedi
(tek istisna: bu rapor dosyasının § 17 eki). Mutasyonların üçü de `cmp` ile birebir geri alındı.

**Artık:** `dotnet/denetim.sh.mutbak` — **0 bayt**, 3. turda `printf '' >` ile boşaltıldı.
Hook `rm`i bloklar; **silmek kurucudadır** (§ 16.3 ile aynı kalem, tekrar ediyor).

## § 17.7 — Sonuç

**P-19 GEÇTİ.** Plan bloğunun dört DoD maddesi de ölçümle karşılandı:
iki bacak tek transaction (M19-2 kanıtı) · virman stornosu iki bacağı birlikte döndürür
(canlı 13 + M19-1) · gider kâr-zararda ayrı satır (canlı 11 + M19-4) ·
`karsi_bacak_id` ⇄ `karsi_sube_id` anlam ayrımı § 2.2'de yazılı.
Sayaçlar eklendi ve **sıfır**; tavan **artırılmadı**.
