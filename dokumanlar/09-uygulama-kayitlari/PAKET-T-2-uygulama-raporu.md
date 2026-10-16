# PAKET T-2 — Açılış bakiyeleri ve işletme kuruluş sihirbazı (Faz A) · uygulama raporu

> Tarih: 2026-09-17 · Dalga 3 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü
> Durum: **BİTTİ (Faz A)** · Derleme 0 hata / 0 uyarı · 2280/2280 test yeşil · `dotnet/denetim.sh` **TEMİZ**

---

## 1. Yapılanlar

### Veri modeli (yeni)
- `Data/Kurulum.cs` (**YENİ**): `KurulumDurumu` (kiracı başına TEK satır, UNIQUE(tenant_id)) ve
  `AcilisKalemi` (uygulanan her açılış kaleminin izi) + `AcilisTuru` / `AcilisKalemDurumu` /
  `KurulumAdimlari` sabitleri + `AcilisKalemi.AnahtarUret`.
- `Data/Entities.cs`: `Sabitler.HareketTurleri` dizisine `"ACILIS"` eklendi (gerekçe yorumda).
- `Data/KuyumcuDbContext.cs`: 2 yeni `DbSet` + 3 indeks
  (`UNIQUE(tenant_id)`, `UNIQUE(tenant_id, tur, benzersiz_anahtar)`, `(tenant_id, oturum_id)`).
  Kiracı süzgeci jenerik `KiraciFiltresi` ile **otomatik** gelir (ikisi de `IKiraciVarlik`).

### Servis / kapılar
- `Modules/Kurulum/AcilisKaynaklari.cs` (**YENİ**, SAF — DbContext görmez): satır sözleşmesi,
  hata/uyarı kodları, kültürden bağımsız doğrulama, prova jetonunun kanonik metni + SHA-256 özeti.
- `Modules/Kurulum/AcilisServisi.cs` (**YENİ**): kuru prova · uygula · geri al.
- `Modules/Kurulum/KurulumModulu.cs` (**YENİ**): `KurulumAyarlari`, `KurulumServisi`
  (durum · PATCH durum · mühürle · mühür kapısı · açılış penceresi kapısı · satır tavanı),
  `KurulumController`.
- `Modules/Cari/CariModulu.cs` (**DEĞİŞTİ**): `Duzeltme` metodu `tur` + `krediLimitiUygula`
  parametreleriyle genişletildi; gövde `DuzeltmeYaz` (private, hareket kimliğini döner) olarak
  ayrıldı; `AcilisYaz` ince sarmalayıcı eklendi; transaction `IsBaslatVeyaDevral` ile devralınabilir
  hâle geldi (47-A altyapısı).
- `Modules/Meta/DuzeltmePolitikasi.cs` (**DEĞİŞTİ**): `ACILIS` hareket türü + `KurulumDurumu`
  (DUZENLENEBILIR) + `AcilisKalemi` (STORNO) kayıtları.
- `Modules/ModulKayit.cs` (**DEĞİŞTİ**): `KurulumServisi` + `AcilisServisi` DI kaydı (Program.cs'e dokunulmadı).
- `Modules/Yetki/YetkiModulu.cs` (**DEĞİŞTİ**): `"kurulum"` modül anahtarı (MÜDÜR varsayılanında var → yalnız görüntüleme).
- `dotnet/denetim.sh` (**DEĞİŞTİ**): yeni sayaçlar + üç tavan güncellemesi (bkz. § 7).

### Uçlar
| Fiil | Yol | Sınıf kapısı | Metot kapısı |
|---|---|---|---|
| GET | `/api/v1/kurulum/durum` | `[Authorize]` + `[RolGerekli("SAHIP","MUDUR")]` + `[ModulYetkisi("kurulum")]` | — |
| PATCH | `/api/v1/kurulum/durum` | aynı | `[RolGerekli("SAHIP")]` |
| POST | `/api/v1/kurulum/muhurle` | aynı | `[RolGerekli("SAHIP")]` |
| POST | `/api/v1/kurulum/acilis/prova` | aynı | `[RolGerekli("SAHIP")]` |
| POST | `/api/v1/kurulum/acilis/uygula` | aynı | `[RolGerekli("SAHIP")]` |
| POST | `/api/v1/kurulum/acilis/geri-al` | aynı | `[RolGerekli("SAHIP")]` |

### Ekran
- `Kuyumcu.Web/Pages/Kurulum.razor` (**YENİ**, rota `/kurulum`, id öneki `kur-`): altı adımlık durum
  listesi (sunucudan türetilmiş), açılış satır düzenleyicisi, kuru prova sonucu tablosu, mühürleme.
- `Kuyumcu.Web/Layout/KenarCubugu.razor` (**DEĞİŞTİ**): `/kurulum` menü girişi (SAHİP+MÜDÜR).
- `Kuyumcu.Web/Layout/RotaBasliklari.cs` (**DEĞİŞTİ**): `/kurulum` başlık haritası.
- `Kuyumcu.Core/I18n/Sozlukler.cs` (**DEĞİŞTİ**): `SozlukKurulum` (48 anahtar) TR+EN, `Nav.Kurulum`,
  `VarlikAdlari["KurulumDurumu"]` / `["AcilisKalemi"]` TR+EN.
- `Kuyumcu.Core/Sozlesmeler/KurulumSozlesmeleri.cs` (**YENİ**): istemci yanıt sözleşmeleri
  (Pages altına record yazılmadı — S-2 / MÜHÜR 5 KOŞUL 3).
- bUnit sahte API: `KurulumEkranTestleri.SahteApi` her yeni uca gerçekçi yanıt verir (UX-S17).

---

## 2. Mühürlenen kararlar (Birleşik koşullar)

| # | Çelişki / koşul | Karar | Gerekçe |
|---|---|---|---|
| 1 | **ACILIS mi CARI_DUZELTME mi** (mühür 2 ↔ PAKET-47:75) | **ACILIS**, ikinci yazma yolu AÇILMADI | `CariServisi.Duzeltme`'ye `tur` parametresi geçirildi; gövde tek (`DuzeltmeYaz`). Böylece bakiye satırı, döviz bacağı, kredi limiti ve denetim izi TEK yoldan geçer. `AcilisYaz` gövdesiz bir sarmalayıcıdır (yalnız hareket kimliğini döndürmek için var). |
| 2 | **FAIL-CLOSED 1 dili** | Düzeltildi | `Sabitler.HareketTurleri`'ne ekleme **belge değeridir** (kodda zorlanmaz — Entities.cs'in kendi yorumu). Gerçek kapılar: `denetim.sh` "Kurulum/ altında dogrudan Add = 0" sayacı + `Sabitler_HareketTurleri_ile_DuzeltmePolitikasi_TAM_ortusur` (çift yönlü) testi. İkisi de M10 mutasyonuyla kanıtlandı. |
| 3 | **DuzeltmePolitikasi kaydı** (`Defter\|StornoYok` bit birleşimi yok) | **TEK anahtar: `Defter`** + gerçek hedef uç | 48-A motoru var ama ACILIS onun beyaz listesinde DEĞİL. Bunun yerine **var olan** `POST /kurulum/acilis/geri-al` ucu hedef yol olarak yazıldı; alternatif yol `api/v1/cari/{id}/duzeltme`. `Storno_HedefUc_lari_GERCEK_uca_isaret_eder` testi ikisini de yansımayla doğruluyor. |
| 4 | **Kasa bacağı** | `CariModulu.cs`'deki mevcut "aynı transaction içinde `db.KasaHareketleri.Add`" kalıbı | `KasaServisi.HareketGir` parametrelenmedi → ikinci kasa yazma sözleşmesi doğmadı. Gün kapanışı kilidi `IslemDogrulayici.KasaDogrula(..., an: now)` ile uygulanıyor. |
| 5 | **acilis_kalemleri UNIQUE** | `UNIQUE(tenant_id, tur, benzersiz_anahtar)`; `benzersiz_anahtar` STOK'ta `"{subeId}\|{urunId}"`, diğerlerinde `hedefId` | Aynı ürünün ikinci şubedeki meşru açılışı reddedilmiyor. Motoru Faz B'de açılacak ama **şema kısıtı bugün yazıldı** ve DB düzeyinde sınandı (`Ayni_urun_iki_subede_acilabilir...`, mutasyon M3b). |
| 6 | **Mühür ZORUNLU + süre sınırı** | Mühür zorunlu; ayrıca **açılış penceresi** (yapılandırma `Kurulum:AcilisPenceresiGun`, varsayılan 30 gün) | Mühürsüz kiracıda açılış uçları süresiz açık kalmıyor. İki 409 **ayrı hata adıyla** ayrışıyor: `"Acilis Muhurlu"` ve `"Acilis Penceresi Kapandi"` (T-1'in `/saglik/hazir` semantiğinden bağımsız). |
| 7 | **Kredi limiti kapısı** | **Uyar ama geçir** + SAHİP'in ayrı onayı | Açılış MEVCUT borcun kaydıdır, yeni risk doğurmaz; limit üstü borcu olan gerçek müşteri reddedilse dükkân kurulamaz. Bedeli: prova `limitAsimiOnayiGerekli=true` döner ve uygula gövdesinde `limitAsimiOnayi` gelmezse **400**. Mutasyon M12 ile kanıtlı DB testine bağlandı. |
| 8 | **MANUAL kur adımı Faz A'ya** | Alındı — **yeni uç açılmadan** | `POST /api/v1/kur/manuel` zaten var (`KurModulu.cs:1002`, SAHİP+MÜDÜR). Sihirbaz KUR adımını durum ucundan (`sayilar.kur`) okur, eksikse ekranda `#kur-kur-yok` uyarısı + `/kur` bağlantısı çizer ve açılış düğmesi kilitlenir (sebebi yazılı). |
| 9 | **KVKK okuma izi + MUDUR 403 testi** | İkisi de yazıldı | `Cari_acilisinda_KISISEL_VERI_uyarisi_ekranda_YAZAR` (bUnit) + sunucuda `DenetimServisi.ListeOkumaKaydet`; `MUDUR_durumu_GORUR_ama_acilis_uygulayamaz_403` (HTTP, 5 uç tek tek). |

---

## 3. DoD sözleşmesi

| Varlık / kayıt | Düzenleme yolu | Denetim | Ekran eylemi |
|---|---|---|---|
| `KurulumDurumu` | **PATCH** `/api/v1/kurulum/durum` — gövdede yoksa **korunur**, boş dizi **siler** (test: `Durum_PATCH_i_gonderilmeyen_alani_KORUR_null_ise_SILER`, mutasyon M15) | `DenetimServisi.YazmaKaydet` (mühürde) + otomatik yazma dinleyicisi | Adım başına "Bu adımı atla / Atlamayı geri al" düğmesi |
| `AcilisKalemi` | **Yerinde düzenlenmez** (defter izi). Mühürden ÖNCE: `POST /kurulum/acilis/geri-al` → **ters ACILIS kaydı** + kalem `GERI_ALINDI`. Mühürden SONRA: `POST /cari/{id}/duzeltme` | Her kalem için `YazmaKaydet(islem,"ACILIS",…)` + gerekçe | `#kur-geri-al` düğmesi + `DegismezIpucu Varlik="AcilisKalemi"` (gerekçe + doğru yol yazılı) |
| `hareketler` (ACILIS) | **HARD DELETE YOK** — yalnız ters kayıt (append-only, ADR-0004). Canlı SQL: geri almadan sonra 2 satır, net 0,00 | `denetim_kayitlari` append-only | `DegismezIpucu` |
| `kasa_hareketleri` (açılış satırı) | Silinmez; ters ACILIS kendi kasa satırını yazar | otomatik | — |

**Salt okunur olanlar ve nedeni:** `AcilisKalemi` ve `hareketler` yerinde düzeltilemez — VUK md.219
(kayıtların belgeye dayanması, yerinde düzeltme yasağı). Ekran bunu `DegismezIpucu` ile yazıyor ve
doğru düzeltme yolunu (geri alma / cari düzeltme fişi) gösteriyor; "sunucuda uç var, ekranda düğme yok"
durumu yok (UX-S25).

**DoD EK / DoD EK 2:**
- Her düğme ya gerçek istek atar ya görünür değişiklik yapar ya da `disabled` + sebep taşır
  (`Her_dugme_ya_ISTEK_atar_ya_GORUNUR_degisiklik_yapar_ya_KILITLIDIR`). Yönlendirmeler `<button>`
  değil `<a href>` olarak çizildi.
- Tek düzenleme kalıbı korundu: `duzenlenenId` sayacı **1** (artış yok); `ModalPencere` yalnız
  **yıkıcı onay** için kullanıldı (mühürleme + geri alma) — tavan 45→46, gerekçe denetim.sh içinde yazılı.
- Her form alanı boş/geçersizken alan altı Türkçe mesaj (`GerekceAlani`, `#kur-prova-kilit`,
  `#kur-uygula-kilit`, `#kur-geri-al-kilit`, `#kur-muhur-kilit`).
- Yıkıcı eylem ortak onay modalı + geri dönüş yolu (`Muhurleme_ONAY_MODALI_ister_ve_onaylanmadan_istek_ATILMAZ`).
- **Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** `Pages/Kurulum.razor` (YENİ), `Layout/KenarCubugu.razor`,
  `Layout/RotaBasliklari.cs`. **`Pages/Isletme.razor`ye DOKUNULMADI** (Faz B kapsamı).

---

## 4. Sapmalar ve gerekçeleri

| # | Spesifikasyon | Uygulama | Gerekçe |
|---|---|---|---|
| S1 | `POST /kurulum/sube`, `POST /kurulum/kullanici` ince sürücüleri | **Yazılmadı** | Üçünün de sunucu karşılığı var (`/subeler`, `/kullanicilar`, `/kur/manuel`). "İnce sürücü" bile olsa ikinci bir uç yüzeyi açmak Bertan'ın "ikiz yol doğmasın" koşuluna terstir. Sihirbaz mevcut uçların ekranlarına yönlendiriyor (`#kur-adim-*-git`). |
| S2 | `GET /kurulum/sablon/{tur}` CSV şablonu | **Faz B** | Fazlar bölümü CSV şablon/toplu yüklemeyi açıkça Faz B'ye koyuyor. 47-A'nın `CsvOkuyucu`/`IceAktarimKapisi` altyapısı yerinde duruyor; `AcilisKaynaklari` kolon/doğrulama tek yerde tanımlı, Faz B onu **aynen** tüketecek (ikinci doğrulama kopyası yazılmadı). |
| S3 | STOK açılışı (DEVIR partisi, birim maliyet NULL, ürün kartı yoksa hata) | **Faz B** — ama **şema kısıtı bugün yazıldı** | Kapsam kararı orkestratörden bağlayıcı ("yalnız Faz A"). `AcilisTuru.Stok` + şube boyutlu benzersizlik anahtarı bugün var ve DB düzeyinde sınandı; `AcilisTuru.FazA` beyaz listesi STOK isteğini 400 ile reddeder (uydurma davranış yok). |
| S4 | Oturum geri alma Faz B'de | **Faz A'ya alındı** | Bertan'ın 3. koşulu ("48-A varsa Defter + hedef uç") ancak GERÇEK bir uç varsa karşılanabilirdi; `DuzeltmePolitikasi`ye olmayan bir uç yazmak UX-S25 ihlali olurdu. Geri alma 48-A motorundan değil, **aynı ACILIS yolundan ters kayıtla** yapılıyor (ikinci silme yolu açılmadı). |
| S5 | `/isletme` kurulum durumu kartı | **Faz B** | Fazlar bölümünde Faz B. TEK DOKUNUŞ kuralı gereği `Isletme.razor` bu dalgada açılmadı. |
| S6 | "Aynı hedefe ikinci açılış → 409" | 409 **ConflictException** | İlk uygulamada 400 dönüyordu; canlı doğrulamada yakalandı, düzeltildi ve M16 mutasyonuyla teste bağlandı. |
| S7 | "Başka kiracının carisine açılış → 404" | 404 **NotFoundException** | Kiracı sınırı ihlali doğrulama hatası değil; "başka kiracının kaydı" ile "hiç yok" istemciye AYNI görünmeli (K1). |
| S8 | Döviz kasası/hesabı açılışı | **Reddediliyor** (`DOVIZ_HESAP_FAZ_B` satır hatası) | TL dışı kasada açılışın defter TL bacağı kur dönüşümü ister; tahmin edilmedi, açıkça reddedildi (Anayasa md.1). |

---

## 5. Migration

- **Ad:** `20260917143620_KurulumVeAcilis`
- Üretim: `dotnet ef migrations add KurulumVeAcilis --project Kuyumcu.Api` (build'li, `--no-build` KULLANILMADI).
- `.cs` 5 434 bayt, `.Designer.cs` 145 416 bayt; `Up()` iki tablo + üç indeks içeriyor (okundu, doğrulandı).
- Uygulama: `dotnet ef database update --project Kuyumcu.Api --connection …` →
  `Applying migration '20260917143620_KurulumVeAcilis'. Done.`
- Kanıt (kuyumcu_net): `SHOW TABLES LIKE 'acilis_kalemleri'` → `acilis_kalemleri`;
  `SHOW TABLES LIKE 'kurulum_durumu'` → `kurulum_durumu`;
  `SELECT COUNT(*) FROM __EFMigrationsHistory` → **40**.

---

## 6. Test sayıları

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Tam süit (öncesi, aynı komut) | 2238 | 2238 | 0 | 0 |
| **Tam süit (sonrası)** | **2280** | **2280** | **0** | **0** |
| `AcilisBakiyeTestleri` [DB] | 14 | 14 | 0 | 0 |
| `KurulumHttpTestleri` [HTTP] | 14 | 14 | 0 | 0 |
| `KurulumEkranTestleri` [bUnit] | 11 | 11 | 0 | 0 |
| `DuzeltmePolitikasiTestleri` (yeni 3) | 22 (3 yeni) | 22 | 0 | 0 |

**Yeni test sayısı: 42.** Kırmızı yok. Tam süit çıktısı:
`Toplam test sayısı: 2280 / Geçti: 2280` (trx: `scratchpad/trx-pT-2/…trx`).

> Süit sırasında `RotaBaslikTestleri` 3 test kırmızıya döndü (`/kurulum` başlık haritasında yoktu —
> testin kendisi bunu zorluyor, UX-S13). `RotaBasliklari.cs`ye satır eklendi ve sabit 35→36 yapıldı.
> Gizlenmedi, burada yazılıdır.

---

## 7. denetim.sh

Çıktı: **`DENETİM TEMİZ.`**

Eklenen sayaçlar:
- `✓ Kurulum/ altında dogrudan Add (0 olmalı)  0` — `Hareketler.Add|Cariler.Add|Urunler.Add` ⇒ 0.
- `✓ Açılış → mevcut cari yolu çağrısı (>=1)` — `cariServisi.AcilisYaz` çağrısı kaybolursa kırmızı.

Güncellenen tavanlar (hepsi gerekçesi yazılı):
- `BEKLENEN_VARLIK` 43 → **45** (`KurulumDurumu` + `AcilisKalemi`).
- `Pages: ModalPencere kullanımı` 45 → **46** (tek kullanım, **yıkıcı onay**; düzenleme değil).
- `Pages/*.razor dosya sayısı` 35 → **36** (`Kurulum.razor`).
- `Pages: @onclick'siz ve disabled'sız <button>` tavanı **DEĞİŞMEDİ** (71); gerçek sayı 78'e çıkmıştı,
  markup düzeltildi → **69** (tavanın altına indi).

---

## 8. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/T-2/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli
değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile **birebir aynı** olduğu kanıtlandı
(her satırda `GERI ALINDI (cmp birebir)`).

| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M1 | `OdemeIzi.Yok` → `OdemeIzi.Nakitten(dto.TlDelta)` (yalnız ACILIS yolunda) | `Acilis_tevsik_gunluk_nakit_toplamini_DEGISTIRMEZ`, `Acilis_hareketinin_nakit_ve_veresiye_payi_SIFIRDIR` |
| M3a | İdempotans kontrolü (`ZatenAcilis`) etkisizleştirildi | `Ayni_hedefe_ikinci_acilis_REDDEDILIR` |
| M3b | `AnahtarUret`'ten ŞUBE boyutu kaldırıldı | `Ayni_urun_iki_subede_acilabilir_ayni_subede_ikinci_kez_ACILAMAZ` |
| M4 | Prova jetonu karşılaştırması `if (false)` | `Prova_HICBIR_kayit_yazmaz_ve_JETONSUZ_uygulama_REDDEDILIR`, `Prova_jetonu_olmadan_uygula_400` |
| M5 | `Uygula`'dan `MuhurDogrula` çağrısı silindi | `Muhurden_sonra_acilis_KAPANIR`, `Muhurlu_kiracida_acilis_uygula_409` |
| M6 | `uygula` ucundan `[RolGerekli("SAHIP")]` silindi | `MUDUR_durumu_GORUR_ama_acilis_uygulayamaz_403` |
| M7 | Servis içi gerekçe uzunluk kontrolü `< 0` | `Gerekce_asgarinin_altinda_SERVIS_ICINDE_reddedilir` |
| M8 | Transaction kaldırıldı, satır satır `SaveChanges` + `Commit` | `Bir_satir_patlarsa_TUM_oturum_geri_alinir` (+8 test) |
| M9 | `#kur-uygula` → `disabled="@(false)"` | `Prova_temiz_degilken_Uygula_dugmesi_KAPALI_ve_sebebi_yazili`, `Gerekce_20_karakterden_kisayken_Uygula_KAPALI` |
| M10 | `DuzeltmePolitikasi`'ndan ACILIS satırı silindi | `ACILIS_turu_DuzeltmePolitikasi_haritasinda_KAYITLI`, `Sabitler_HareketTurleri_ile_DuzeltmePolitikasi_TAM_ortusur`, `ACILIS_DUZENLENEBILIR_DEGILDIR`, `Defter_turleri_DUZENLENEBILIR_DEGILDIR` |
| M11 | `HasGrami = -dto.HasDelta` → ACILIS'te ters çevrildi | `Acilis_isaret_sozlesmesi_VERESIYE_ile_ayni_yonde` |
| M12 | Kredi limiti onay kapısı `if (false)` | `Kredi_limiti_asan_acilis_ONAYSIZ_reddedilir_ONAYLA_gecer` |
| M13 | Mühür onay kutusu kontrolü `if (false)` | `Muhur_ONAYSIZ_400` |
| M14 | Açılış penceresi kontrolü `if (false)` | `Acilis_penceresi_dolunca_MUHURSUZ_kiracida_bile_409` |
| M15 | PATCH "gövdede yoksa koru" → her zaman ata | `Durum_PATCH_i_gonderilmeyen_alani_KORUR_null_ise_SILER` |
| M16 | Çakışma (409) dalı etkisizleştirildi | `Ayni_hedefe_ikinci_acilis_REDDEDILIR` |

### Kapatılan iki TEST AÇIĞI (Anayasa md.1)
1. **M6 ilk koşumda HİÇBİR testi kırmadı.** Sebep: sınıf düzeyindeki `[RolGerekli("SAHIP","MUDUR")]`
   TEZGÂHTAR'ı zaten kesiyordu; MÜDÜR testi yalnız `prova` ve `muhurle`ye bakıyordu.
   → `MUDUR_durumu_GORUR_ama_acilis_uygulayamaz_403` testine `uygula`, `geri-al` ve `PATCH durum`
   uçları tek tek eklendi; M6 tekrarlandı → KIRMIZI.
2. **M7 ilk koşumda HİÇBİR testi kırmadı.** Sebep: `[MinLength(20)]` denetleyici yolunda önce
   yakalıyordu; servis içi elle kontrolü (FAIL-CLOSED 10) hiçbir test görmüyordu.
   → `Gerekce_asgarinin_altinda_SERVIS_ICINDE_reddedilir` (servisi DOĞRUDAN çağıran DB testi) eklendi;
   M7 tekrarlandı → KIRMIZI.

Son mutasyondan sonra: `dotnet build Kuyumcu.sln --nologo` → **0 uyarı, 0 hata**.

---

## 9. Canlı doğrulama (localhost:5198, kuyumcu_net)

Test kiracısı: `PAKETT-2-TEST Kuyumcu` (`tenantId=01M2QY0EE1KF4AXBQMQYC7RTZ3`).

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `GET /kurulum/durum` (yeni kiracı) | eksik adım listesi | `eksikAdimlar:["KASA_BANKA"]`, `muhurlenebilir:false`, 6 adım | HTTP 200 |
| 2 | `POST /acilis/prova` (3 satır: sıfır + olmayan hedef + yinelenen) | `temiz:false`, jeton yok, satır no'lu hatalar | `temiz:false`, `provaJetonu:null`, `hataliSatir:3`, kodlar `SIFIR_SATIR` / `YINELENEN_SATIR` / `HEDEF_BULUNAMADI` | HTTP 200 |
| 3 | Prova sonrası defter | ACILIS satırı YAZILMAMIŞ | `SELECT COUNT(*) FROM hareketler WHERE tur='ACILIS'` → **0** | SQL |
| 4 | `POST /acilis/prova` (temiz, kasa 25 000,50) | `temiz:true` + jeton | jeton `40DB5C50…CA08` | HTTP 200 |
| 5 | `POST /acilis/uygula` (gerekçe 38 krk) | 200, 1 satır | `{"oturumId":"01M2QY142Q…","uygulananSatir":1,"tlToplam":25000.50}` | HTTP 200 |
| 6 | Defter izi | `nakit_tutar=0.00 AND veresiye_tutar=0.00` | `ACILIS 25000.50 0.00 0.00 NULL` | SQL |
| 7 | Kasa bacağı | `hareket_id` DOLU, ACILIS hareketine işaret | `bakiye=25000.50`, `hareket_id=01M2QY142Z…`, `odeme_turu=DIGER` | SQL |
| 8 | `acilis_kalemleri` | 1 kalem, `benzersiz_anahtar=hedefId` | `KASA … UYGULANDI 25000.50` | SQL |
| 9 | Aynı gövde İKİNCİ kez | **409** + "zaten açılış yapılmış" | `{"error":"Conflict","statusCode":409}` (ilk koşumda 400 çıktı → düzeltildi, bkz. S6) | HTTP 409 |
| 10 | Cari açılış provası (7 500 TL, limit 1 000) | limit UYARISI, jeton yine gelir | `temiz:true`, `uyariliSatir:1`, `limitAsimiOnayiGerekli:true`, `kisiselVeriIzi:true` | HTTP 200 |
| 11 | Cari uygula `limitAsimiOnayi:false` | **400** | "…bu satırları gördüğünüzü onaylamanız gerekir" | HTTP 400 |
| 12 | Cari uygula `limitAsimiOnayi:true` | 200 | `uygulananSatir:1, tlToplam:7500, hasToplam:12.5` | HTTP 200 |
| 13 | Cari bakiye + işaret | CSV/gövde ile birebir; has deftere ters | `tl_bakiye=7500.00`, `has_gram_bakiye=12.500000`; hareket `has_grami=-12.500000` | SQL |
| 14 | Tevsik günlük özeti | açılış öncesi/sonrası AYNI | `bugunkuNakitToplam: 0.00` (7 500 TL açılıştan SONRA) | HTTP 200 |
| 15 | TEZGÂHTAR `POST /acilis/uygula` | 403 | **403** | HTTP |
| 16 | MÜDÜR `GET /kurulum/durum` | 200 | **200** | HTTP |
| 17 | MÜDÜR `POST /acilis/uygula` | 403 | **403** | HTTP |
| 18 | `POST /acilis/geri-al` (cari oturumu) | ters kayıt, kalem GERI_ALINDI | `{"geriAlinan":1}`; `tl_bakiye=0.00`, `has_gram_bakiye=0.000000`; kalem durumları `GERI_ALINDI:1 / UYGULANDI:1`; `hareketler` ACILIS = **3** (silme yok) | HTTP 200 + SQL |
| 19 | `POST /kurulum/muhurle` | 200, `muhurlendi:true` | `muhurTarihi`, `muhurleyenKullaniciId`, `muhurGerekcesi` dolu | HTTP 200 |
| 20 | Mühür sonrası `POST /acilis/uygula` | 409 + AYRI hata adı | `{"error":"Acilis Muhurlu","statusCode":409}` | HTTP 409 |
| 21 | `denetim_kayitlari` (varlik_turu='ACILIS') | kalem sayısı + mühür | **6** (2 uygulama + 1 geri alma + 1 mühür + 2 prova okuma izi) | SQL |
| 22 | Test kiracısı temizliği | tenants başlangıç sayısına döner | `tenants=8` (başlangıç 8), `acilis_kalemleri=0`, `kurulum_durumu=0`, `hareketler WHERE tur='ACILIS'=0` | SQL |

API süreci (`PID 27467` → yeniden başlatmalarla) durduruldu:
`lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş (0 satır)**. 5199 portuna DOKUNULMADI.
`QA-TEST-` önekli kiracılara dokunulmadı.

---

## 10. Değişen dosyalar (bu paket)

> `git status --porcelain` çalışma ağacında ÖNCEKİ paketlerin değişikliklerini de gösteriyor
> (tek commit var). Aşağıdaki liste bu pakette dokunulan dosyalardır.

**YENİ**
```
dotnet/Kuyumcu.Api/Data/Kurulum.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260917143620_KurulumVeAcilis.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260917143620_KurulumVeAcilis.Designer.cs
dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisKaynaklari.cs
dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisServisi.cs
dotnet/Kuyumcu.Api/Modules/Kurulum/KurulumModulu.cs
dotnet/Kuyumcu.Core/Sozlesmeler/KurulumSozlesmeleri.cs
dotnet/Kuyumcu.Web/Pages/Kurulum.razor
dotnet/Kuyumcu.Tests/AcilisBakiyeTestleri.cs
dotnet/Kuyumcu.Tests/KurulumHttpTestleri.cs
dotnet/Kuyumcu.Tests/KurulumEkranTestleri.cs
```
**DEĞİŞTİ**
```
dotnet/Kuyumcu.Api/Data/Entities.cs
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs
dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor
dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs
dotnet/Kuyumcu.Tests/TestOrtami.cs          (Metin/Bayrak yardımcıları)
dotnet/Kuyumcu.Tests/DuzeltmePolitikasiTestleri.cs
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs  (35 → 36)
dotnet/denetim.sh
```
COMMIT YAPILMADI (kurucu yasağı).

---

## 11. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| ACILIS/CARI_DUZELTME çelişkisi kod yazılmadan mühürlendi, tek yazma yolu | ✅ | § 2/1; `DuzeltmeYaz` tek gövde; denetim.sh "Kurulum/ altında dogrudan Add = 0" |
| FAIL-CLOSED 1 dili düzeltildi, gerçek kapı denetim.sh + tam örtüşme testi | ✅ | § 2/2; M10 |
| DuzeltmePolitikasi kaydı tek anahtar + gerçek uç | ✅ | § 2/3; `Storno_HedefUc_lari_GERCEK_uca_isaret_eder` |
| Kasa bacağı mevcut kalıp, ikinci kasa sözleşmesi yok | ✅ | `AcilisServisi.KasaAcilisi`; `KasaModulu.cs` DEĞİŞMEDİ |
| acilis_kalemleri UNIQUE STOK'ta şube boyutlu, teste bağlı | ✅ | § 2/5; M3b |
| Mühür ZORUNLU + süre sınırı + ayrı 409 kodu | ✅ | § 2/6; canlı 20; M5, M14 |
| Kredi limiti kararı mühürlendi + mutasyon kanıtlı DB testi + SAHİP onayı | ✅ | § 2/7; M12; canlı 10-12 |
| MANUAL kur adımı Faz A'da | ✅ | § 2/8; `#kur-kur-yok` + `#kur-kur-ekle` |
| KVKK okuma izi ekran testinde + MUDUR 403 HTTP testi | ✅ | § 2/9; M6 |
| Sevk sırası 47-A + T-2 birlikte, T-1/T-3'ten önce | ✅ | 47-A yerinde (`Modules/Iceaktarim/`), T-1/T-3 açılmadı |
| SÖZLEŞME-DoD (PATCH/DELETE/yetki/denetim/ekran eylemi) | ✅ | § 3 |
| DoD EK 1-4 (ölü düğme yok · tek düzenleme kalıbı · alan altı mesaj · yıkıcı onay) | ✅ | § 3; denetim.sh sayaçları |
| DoD EK 2 TEK DOKUNUŞ | ✅ | § 3 "Dokunulan sayfalar" |
| MÜHÜR 3: tek yayın yolu / sır disiplini / tek-el / MFA kapısı / sayısal iddia yasağı | ✅ | systemd/deploy/docs'a DOKUNULMADI; yeni sır yok; Entities/DbContext/ModulKayit tek elden; üretim yayını yok; § 6 ve § 5 rakamları komut çıktısından |
| UX-S20/21/22/24/25/27/28/29/30/31 | ✅ | `Kurulum.razor` yorum başlığı + bUnit testleri |
| Finansal varlıkta hard DELETE yok | ✅ | denetim.sh `Finansal varlıkta [HttpDelete] = 0`; geri alma ters kayıt |

---

## 12. Açık kalanlar (Faz B ve sonrası)

1. **Stok açılışı** (DEVIR partisi + `birim_maliyet_tl` NULL + ürün kartı yoksa HATA) — motor Faz B;
   şema ve benzersizlik anahtarı hazır. İlgili 2 mutasyon (M2, M12-spec) Faz B'de kanıtlanacak.
2. **CSV şablon indirme + toplu yükleme** — 47-A'nın `CsvOkuyucu`/`IceAktarimKapisi` üzerinden Faz B.
3. **`/isletme` kurulum durumu kartı** — Faz B (TEK DOKUNUŞ gereği bu dalgada açılmadı).
4. **Açılış devri raporlarda ayrışması** — 34-A/35-A süzgeçleri `ACILIS` türünü henüz "devir" olarak
   ayırmıyor. Şu an ACILIS satırları `/raporlar/islem-defteri` tür süzgecinde ham liste içinde
   görünür; kâr-zarara **katkı vermez** (TL bacağı gelir/gider sınıfı taşımıyor) ama bu **doğrulanmadı**,
   yalnız kod okundu. Faz B'de ölçülmeli.
5. **Döviz kasası/banka hesabı açılışı** — bilerek reddediliyor (`DOVIZ_HESAP_FAZ_B`).
6. **Geri alma 48-A storno motoruna bağlanması** — bugün ters ACILIS kaydıyla yapılıyor; 48-A beyaz
   listesine ACILIS eklenmesi ayrı bir karar ister (stok bacağı Faz B'de doğunca).
7. **Kaynak ağacındaki eski `.mutorj` / `.bak` artıkları** — 15 Eylül oturumundan kalma 10 dosya var
   (`Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj`, `Kuyumcu.Tests/MadenDefteriTestleri.cs.bak`, …).
   Bu pakette **oluşturulmadı ve dokunulmadı**; denetim.sh beyaz listesi onları tanıyor.
   Silme işi kurucuya bırakıldı (hook `rm` engelliyor).

---

## 13. Kurucuya / mali müşavire notlar

1. **VUK md.185-186 (dönem başı envanteri / açılış bilançosu) — DOĞRULANMALI.** Tasarım bu madde
   numaralarını "hafızadan" işaretlemişti. **Koda hiçbir madde numarası ya da yasal eşik sabit
   yazılmadı**; program envanter defteri üretmiyor, yalnız veriyi taşıyor. Mali müşavir teyidi
   gerekiyor.
2. **Açılış penceresi 30 gün** — bu bir mevzuat sayısı DEĞİL, Mehmet'in güvenlik koşuludur ve
   **yapılandırmadan** okunuyor (`appsettings` → `Kurulum:AcilisPenceresiGun`). Varsayılan tutucudur;
   kademeli kurulum yapan dükkân için kurucu kararıyla uzatılabilir. Ayar dosyasına satır
   **eklenmedi** (varsayılan kodda, davranış yapılandırma eksikken de aynı).
3. **Kredi limiti kararı ticari bir karardır:** açılış limiti aşsa bile engellenmiyor, SAHİP'in
   ekranda onayı isteniyor. Eğer muhasebe pratiğiniz "limit üstü açılış hiç yazılmasın" diyorsa bu
   tek satırlık bir değişiklik (`AcilisUyariKodu.KrediLimitiAsimi` → hata) — kurucu/mali müşavir
   söylesin.
4. **459 s. VUK tevsik haddi ve MASAK kümülatifi açılıştan ETKİLENMİYOR** (canlı 14 numaralı satır).
   Bu bilinçli: açılışta para el değiştirmiyor; aksi tasarım haddi sahte biçimde doldururdu.
5. **Mühür geri alınamaz.** Mühürden sonra açılış bakiyesi hiçbir yoldan yeniden girilemez; düzeltme
   yalnız cari düzeltme fişi ya da ters kayıtla yapılır. Pilot dükkânlara bu anlatılmalı.
6. Ürün hâlâ **ÜCRETSİZ PİLOT** kapsamında; bu pakette fiyat/teklif/tahsilat içeren hiçbir metin
   üretilmedi.
