# PAKET P-13 — KART / AYAR / UYUM EKRAN BORÇLARI

**Dalga:** DALGA 3 · **Sürüm:** 1.0 · **Tarih:** 2026-09-21
**Roller:** Taşkın (backend) + Ertuğ (frontend)
**Kimlikler:** B-39 · B-40 · B-47 · B-48 · B-27 · B-49 · B-12 · B-17 · **B-61** (P-00 triyajı)
**Önkoşul:** P-06 — **DİSKTE VAR** (`PAKET-P-06-uygulama-raporu.md`; `Bilesenler/DuzenlenebilirListe.razor`, genişletilmiş `SatirIciDuzenle.razor`, `Modules/Meta/DuzeltmePolitikasi.cs` hepsi mevcut). Kapsam daraltılmadı.
**Migration:** EVET — `20260920222736_P13MasakEkBeyanBagi` (`__EFMigrationsHistory` 54 → **55**).

---

## § 0 — Bir cümlede

Bu paket "kayıt oluşturulabiliyor ama düzeltilemiyor / ayar var ama sorulmuyor / iç kod kullanıcıya basılıyor" sınıfındaki yedi ekran borcunu kapattı: kategori artık **silinmeden pasife alınıyor** (bağlı kayıt sayısıyla), cari pasife alma **bağlı kayıt özeti + denetim izi** üretiyor, kur kaynağı ve maliyet yöntemi **boşken kilitli değil**, `/yedekler` iptal gerekçesi **okunuyor**, MASAK'a **gönderilmiş** bildirimin düzeltme yolu (**ek beyan**) hem sunucuda hem ekranda var, gider pusulasında satıcı adresi **boş bırakılamıyor** (VUK md.234, yapılandırmadan), ve `DOVIZ_HESAP_FAZ_B` gibi ham kodlar ekrandan **kalktı**.

---

## § 1 — Kimlik kapanış tablosu (ZORUNLU)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-39** — aktif cari pasife alınamıyor | **KAPANDI** | Ekran yolu P-06 öncesinde eklenmişti; bu pakette DoD'un eksik yarısı (**bağlı kayıt sayısı uyarısı** + **denetim izi**) kapandı. Sunucu: `Modules/Cari/CariModulu.cs:655-700` (`DeaktifEt` + `BagliKayitlariGetir`), uç `CariModulu.cs:1472` `GET /cari/{id}/bagli-kayitlar`. Ekran: `Pages/Cariler.razor:446-458` (`#c-pasif-bagli`), `Cariler.razor` `PasifOnayAc()`. Test: `Cari_BagliKayitlar_HareketVeBakiyeSayar`, `Cari_PasifeAlinir_DenetimIziBagliKaytlariYazar`, `Cari_PasifOnayi_BagliKayitSayisiniOkurVeGosterir`. Canlı: § 9 adım 19-22 + SQL |
| **B-40** — `/urunler/agac` kategori pasife alınamıyor | **KAPANDI** | Sunucu: `Modules/Urun/KategoriModulu.cs` `PasifeAl` + `PasifeAlmaKapisi` + `BagliKayitlar`; uçlar `DELETE /urun-kategorileri/{id}`, `GET …/bagli-kayitlar`. Ekran: `Pages/UrunAgaci.razor` `#k-pasife-{id}` · `#k-aktife-al-{id}` · ortak onay penceresi. Test: 5 adet (`Kategori_*`). Canlı: § 9 adım 1-7 |
| **B-47** — `/isletme` kur kaynağı ve maliyet yöntemi boşken kilitli | **KAPANDI** | Sunucu: yeni uç `PATCH /admin/tenant/maliyet-yontemi` (`AdminModulu.cs`, `MaliyetYontemiDto`); kur kaynağı MEVCUT uçla (`PATCH /kur/kaynak`) bağlandı — ikinci yazma yolu açılmadı. Ekran: `Pages/Isletme.razor:167-200` iki seçici + "belirlenmedi" rozeti. Test: `MaliyetYontemi_*` (2), `Isletme_KurKaynagiVeMaliyetYontemi_BoskenSecilebilir`, güncellenen `C4_14_…_SECILEBILIR`. Canlı: § 9 adım 8-10 |
| **B-48** — `/yedekler` iptal gerekçesi gösterilmiyor | **KAPANDI** | Sunucu zaten `iptalGerekce` döndürüyordu (`YedekModulu.cs:307`) ve istemci sözleşmesi taşıyordu (`YedeklerSozlesmeleri.cs:23-25`) — **ekran okumuyordu**. `Pages/Yedekler.razor:140-152` `#yd-iptal-gerekce-{id}`. Test: `Yedekler_IptalGerekcesi_EkrandaGorunur` |
| **B-12** — `/uyum` GONDERILDI kaydında düzeltme yolu | **KAPANDI** | Yeni varlık alanı `MasakSupheliBildirim.DuzeltilenBildirimId` + migration; uç `POST /masak/supheli/{id}/ek-beyan` (`MasakModulu.cs`); ekran `Pages/Uyum.razor` `#uy-ek-beyan-{id}` + duruma göre değişen `DegismezIpucu`. Test: 3 HTTP + 1 bUnit. Canlı: § 9 adım 13-17 |
| **B-49** — gider pusulası satıcı kimliği + `/uyum` düğmeleri | **KISMEN** | (a) **Satıcı kimliği alanları**: `Pages/Islemler.razor:619-680` içinde ZATEN VARDI (önceki paket); bu pakette DoD'un açık kalan yarısı — **adres boş bırakılamaz (VUK md.234)** — sunucuda kapatıldı (`GiderPusulasiKapisi.Dogrula`, yapılandırmadan: `HurdaAyarlari.SaticiAdresiZorunlu`). (b) **`/uyum` düğmeleri** ölçüldü: `uy-gonderildi-*` / `uy-kapat-*` gerçek istek atıyor (UX-4d md.10'da kapatılmış). (c) **Ekranda alan-altı "adres zorunlu" işareti** `Islemler.razor`'a KONMADI — o dosya P-15/P-16/P-18 tek-el zincirindedir (§ 7). Sunucu 400'ü hata bandında Türkçe görünür. `devredildi: P-16` |
| **B-27** — S-3f "kapandı" denen iki bulgunun yeniden ölçümü | **KISMEN** | (1) `/kullanicilar` tek SAHİP: metin ölçüldü, artık yanlış değil (`SozlukYonetim.TekSahipUyari` = "Önce sahipliği devredin") ama **devir akışı yok** → `devredildi: SAHİP devir akışı paketi`. (2) `/raporlar` pasta: kod tarafı ölçüldü ve **teste bağlandı** (`B27_TekDilimliPasta_KapaliTamDaireCizer` — %100 tek dilimde iki yarım yaylı kapalı daire). `getBBox()` DOM ölçümü hâlâ tarayıcı turuna ait: `devredildi: QA turu 7` |
| **B-17** — kuruluş sihirbazı negatif açılış (K-24'e bağlı) | **KAPANDI (plan hükmüyle)** | Plan "negatif açılış reddedilir" hükmü KOYMAZ; **mevcut tutucu davranış korundu** (istemci `Kurulum.razor:199-203`, sunucu `AcilisServisi.cs:504-518` — ikisi de değiştirilmedi) ve **gerekçe ekrana yazıldı**: `#kur-tl-kural` (`SozlukKurulum.NegatifAcilisGerekce`, K-24'ün beklemede olduğu açıkça yazılı). Yan kazanç: `aria-describedby="kur-tl-kural"` hedefi ilk kez GERÇEKTEN var |
| **B-61** — `/isletme` işletme kimliği telefonu doğrulanmıyor | **KAPANDI (gerileme kilidi kuruldu)** | Ölçüm: doğrulama ZATEN vardı (`AdminModulu.cs:289-290` sunucu, `Isletme.razor:433-435` istemci — önceki pakette kapanmış). Bu pakette **gerileme testi** eklendi: `IsletmeTelefonu_SubeyleAyniKuraldanGecer`. Canlı: § 9 adım 11-12 (`"abc"` → 400, geçerli → 200) |

---

## § 2 — Sapmalar ve gerekçeleri (Anayasa md.1)

| # | Plan / koşul ne diyor | Ölçülen gerçek | Ne yapıldı |
|---|---|---|---|
| S1 | "Aktif/pasif kolonu Urun/Cari/Kategori'de var mı **önce ölçülür**; yoksa migration EVET" | Ölçüldü: `aktif` kolonu **Cari** (`Entities.cs:317`), **Urun** (`:482`), **UrunKategorisi** (`UrunKategorisi.cs:38`), **Reyon** (`:71`) — hepsinde VAR | Aktif/pasif için migration **gerekmedi**. Migration yalnız B-12'nin ek beyan bağı için üretildi (§ 3) |
| S2 | B-39 "aktif cari pasife alınamıyor" | Ekranda "Pasife al" düğmesi + onay modalı + "Aktife al" ZATEN VARDI (UX-4d md.13) | Kimlik "zaten kapandı" diye geçilmedi: DoD'un **ölçülebilir eksik yarısı** (bağlı kayıt sayısı + denetim izi) kapatıldı. Sunucu `DeaktifEt` hiçbir denetim izi yazmıyordu — bu gerçek bir açıktı |
| S3 | B-49 "gider pusulası arayüzden hiç üretilemiyor — satıcı kimliği alanları formda yok" | Alanlar `Islemler.razor`'da VAR; eksik olan **adres zorunluluğu** | DoD'un gerçek maddesi ("adres boş bırakılamaz") uygulandı; `Islemler.razor`'a DOKUNULMADI (tek-el) |
| S4 | B-27 "iki bulgunun yeniden ölçümü" | Bulgulardan biri tarayıcı DOM ölçümü ister (getBBox) — bu turda tarayıcı turu yok | Kod tarafı **teste bağlandı** ve mutasyonla kanıtlandı (M13-12); DOM ölçümü açık kalan olarak yazıldı. "Ölçtüm" denmedi |
| S5 | Plan: "`MasakModulu.cs:314-326` maskeleme deseni kullanılır" | O satır aralığı bugün **controller** bölümü; maskeleme deseni `MasakServisi.Maskele` (`MasakModulu.cs:57-58`) ve gider pusulası listesi **zaten** onu kullanıyor (`HurdaModulu.cs:44`) | Satır numarası kaymış; desen doğrulandı, **ikinci bir maskeleme kopyası yazılmadı** |
| S6 | DoD EK md.2 — ikinci düzenleme kalıbı açılmaz | Kategori onayı için ikinci bir `ModalPencere` eklenince `Pages: ModalPencere kullanımı` cırcırı 54 → 55 oldu | Yeni pencere **kaldırıldı**; reyonun onay penceresi **ortaklaştırıldı** (tek pencere, iki içerik; kimlikler `r-pasif-*` / `k-pasif-*` korundu). Tavan **artırılmadı** |
| S7 | UX-S22 — aynı soruya iki cevap olmaz | Kategori çekmecesinde onaysız, uyarısız bir "Aktif" işaret kutusu vardı | Kutu kaldırıldı, düzenleme PATCH'i artık `aktif` **göndermiyor** (PATCH sözleşmesi: gövdede yoksa koru). Aktiflik tek yoldan değişir |

---

## § 3 — Migration

| Alan | Değer |
|---|---|
| Ad | `20260920222736_P13MasakEkBeyanBagi` |
| Neden | B-12 — gönderilmiş bildirimin düzeltmesi (ek beyan) aslına BAĞLI doğmalı; bağ olmadan zincir denetimde okunamaz |
| `.cs` boyutu | **889 bayt** (0 bayt DEĞİL) |
| `.Designer.cs` boyutu | **196.035 bayt** |
| `Up()` | `AddColumn<string> duzeltilen_bildirim_id varchar(36) NULL` → `masak_supheli_bildirimler` |
| `Down()` | `DropColumn duzeltilen_bildirim_id` (yazıldı) |
| Uygulama | `dotnet ef database update --connection …` (build'li, `--no-build` KULLANILMADI) → `Applying migration '20260920222736_P13MasakEkBeyanBagi'. Done.` |
| `__EFMigrationsHistory` | **54 → 55** (SQL ile doğrulandı) |
| Şema kanıtı | `SHOW COLUMNS FROM masak_supheli_bildirimler LIKE 'duzeltilen_bildirim_id'` → `varchar(36) YES NULL` |

> **Not (BK-21 kapsamı):** BK-21 `Down()` tatbikatını P-05/P-19/P-21/P-23/P-26/P-34 için zorunlu kılar; P-13 o listede değildir. `Down()` **yazıldı**, bu turda **koşturulmadı** — dürüstçe açık kalan (§ 12/1).

---

## § 4 — Yapılanlar (dosya dosya)

### Sunucu
- **`Modules/Urun/KategoriModulu.cs`** — `KategoriServisi` artık `DenetimServisi?` alıyor. Yeni: `PasifeAl` (soft delete), `BagliKayitlar`, `Sayilar`, `PasifeAlmaKapisi` (ürün > 0 ya da aktif alt kategori > 0 → **409 + sayı**). `Guncelle` içinde `aktif:false` **aynı kapıdan** geçer (arka kapı yok) ve durum değişimi denetim izine düşer. Uçlar: `DELETE /urun-kategorileri/{id}` (`[RolGerekli("SAHIP","MUDUR")]`), `GET …/bagli-kayitlar`.
- **`Modules/Cari/CariModulu.cs`** — `DeaktifEt` bağlı kayıt özetini hesaplar, **denetim izine gerekçeli yazar** ve özeti yanıtta döndürür. Yeni `GET /cari/{id}/bagli-kayitlar` (`SAHIP·MUDUR·MUHASEBE`). **Pasife alma ENGELLENMEZ**: borçlu müşterinin kartı da kapanabilir; kayıp olan şey sessizlikti.
- **`Modules/Admin/AdminModulu.cs`** — `MaliyetYontemiDto` + `MaliyetYontemiGuncelle` + `PATCH /admin/tenant/maliyet-yontemi` (SAHIP, sınıf düzeyi kapı). Geçersiz değer 400; değişim denetim izine `maliyet-yontemi-degisti: X → Y` olarak düşer. Geçmişe dönük etki YOK (yorumda yazılı).
- **`Modules/Masak/MasakModulu.cs`** — `SupheliOlustur` ikiye ayrıldı (`SupheliOlusturKayit` çekirdeği + kaydeden sarmalayıcı) → **tek yazma yolu**. Yeni `SupheliEkBeyan`: yalnız `GONDERILDI` kayıtta, ek beyanın ek beyanı yasak, bağlar (hareket/cari/kimlik tespiti/tutar) devralınır, denetim izi düşer. Uç: `POST /masak/supheli/{id}/ek-beyan` → **201**. `Sekillendir` `duzeltilenBildirimId` taşır.
- **`Modules/Hurda/GiderPusulasiKapisi.cs`** — `HurdaAyarlari.SaticiAdresiZorunlu` (varsayılan `true`, **yapılandırmadan okunur**, ADR-0005) + `Dogrula` içinde pusula gerektiren satıcı tiplerinde adres kontrolü. MÜKELLEF'te aranmaz.
- **`Data/MasakVarliklari.cs`** — `MasakSupheliBildirim.DuzeltilenBildirimId` (append-only gerekçesi yorumda).

### Sözleşme (tek tip, iki taraf)
- **`Kuyumcu.Contracts/Sayfalar/CarilerSozlesmeleri.cs`** → `CariBagliKayit`
- **`Kuyumcu.Contracts/Sayfalar/UrunAgaciSozlesmeleri.cs`** → `KategoriBagliKayit`
- **`Kuyumcu.Core/Sozlesmeler/KurulumSozlesmeleri.cs`** → `AcilisSatirSonucu.Kimlik` + `AcilisHataKimligi` haritası (iç kod → `AC-01…AC-11`)
- İkisi de `SozlesmeSekilTestleri.Uclar()`'a eklendi → **kilitsiz tip sayısı ARTMADI** (cırcır korundu); `{kategori}` fikstürü teste eklendi.
- **`Kuyumcu.Web/Servisler/UyumServisi.cs`** → `EkBeyan(...)`, `SupheliSatir.DuzeltilenBildirimId`.

### Ekran
- **`Pages/UrunAgaci.razor`** — ağaç satırında Pasife al / Aktife al; ortak onay penceresi (bağlı kayıt sayısıyla); çekmecedeki "Aktif" kutusu kaldırıldı; PATCH artık `aktif` göndermiyor.
- **`Pages/Cariler.razor`** — onay penceresinde bağlı kayıt satırı (`#c-pasif-bagli`), okunurken "okunuyor" satırı, hata bandı ayrı (UX-S28). Ek olarak UX-S30 borcu kapatıldı: `#th-kaydet-kilit` (tahsilat kaydet kilidinin sebebi) ve `th-kasa` için `aria-describedby`.
- **`Pages/Isletme.razor`** — kur kaynağı + maliyet yöntemi seçicileri, kaynak listesi SUNUCUDAN (`/kur/kaynaklar`), belirlenmemişken rozet + açıklama.
- **`Pages/Yedekler.razor`** — iptal gerekçesi durum hücresinde.
- **`Pages/Uyum.razor`** — GONDERILDI satırında "Ek beyan düzenle", ek beyan rozeti, duruma göre doğru `AlternatifYol`; form UX-S26 gereği aynı form.
- **`Pages/Kurulum.razor`** — `#kur-tl-kural` gerekçesi (B-17); hata tablosunda ham kod yerine **hata kimliği** (`#kur-hata-kimlik-{no}`).

### Sözlük (TR + EN, çıplak metin yok)
`SozlukKurulum.NegatifAcilisGerekce` · `HataKimligi` · `SozlukYonetim.KurKaynagiSec` · `MaliyetYontemiSec` · `MaliyetFifo/Lifo/Ortalama` · `AyarBelirlenmedi` · `MaliyetYontemiNotu` · `KurKaynagiNotu` · `SozlukUrunAgaci.KategoriPasifeAl` · `KategoriPasifOnay` · `KategoriBagliKayit` · `KategoriBagliYok` · `KategoriPasifRozet` · `SozlukCari.PasifeAlBagli` · `PasifeAlBagliYukleniyor` · `SozlukUyum.EkBeyan` · `EkBeyanBaslik` · `EkBeyanAciklama` · `EkBeyanBasarili` · `EkBeyanRozet` · `GonderildiDuzeltmeYolu` · `SozlukYedek.IptalGerekceEtiket` — **21 anahtar × 2 dil**.

---

## § 5 — DoD sözleşmesi (Bertan koşulu — hangi varlık nasıl düzeltilir)

| Varlık / kayıt | Düzenleme | Pasife alma / geri alma | Yetki | Denetim izi | Ekran eylemi |
|---|---|---|---|---|---|
| `UrunKategorisi` | `PATCH /urun-kategorileri/{id}` (ad, üst, sıra) | `DELETE` = pasife alma (hard DELETE YOK) · geri dönüş `PATCH {aktif:true}` | `SAHIP·MUDUR` (silme) · `+MUHASEBE` (patch) | `YazmaKaydet` — pasife/aktife alındı | `#k-pasife-{id}` · `#k-aktife-al-{id}` |
| `Cari` | `PATCH /cari/{id}` | `DELETE` = soft (bakiye ve hareket SİLİNMEZ) · geri dönüş `PATCH {aktif:true}` | `SAHIP·MUDUR` | `YazmaKaydet` — bağlı kayıt sayılarıyla | `#c-pasife-al` · `#c-aktife-al-{id}` |
| Kiracı ayarı: maliyet yöntemi | `PATCH /admin/tenant/maliyet-yontemi` | Geri alma = başka değer seçmek (geçmiş yeniden hesaplanmaz) | `SAHIP` | `YazmaKaydet` — eski → yeni | `#i-maliyet-yontemi` |
| Kiracı ayarı: kur kaynağı | `PATCH /kur/kaynak` (MEVCUT uç) | Başka kaynak seçmek | `SAHIP·MUDUR` | Mevcut kur modülü izi | `#i-kur-kaynagi` |
| `MasakSupheliBildirim` (ACIK) | **SALT OKUNUR** | Gerekçeli **kapatma** + yeni bildirim | `SAHIP·MUDUR·MUHASEBE` | Mevcut | `DegismezIpucu` + `#uy-kapat-{id}` |
| `MasakSupheliBildirim` (GONDERILDI) | **SALT OKUNUR — geri alınmaz** (8 yıl saklama) | **EK BEYAN**: aslına bağlı YENİ bildirim | `SAHIP·MUDUR·MUHASEBE` | `YazmaKaydet` — düzeltilen bildirim kimliğiyle | `#uy-ek-beyan-{id}` |
| `GiderPusulasi` | `PATCH` (adres/mal açıklaması — SİLİNEMEZ) | Hard DELETE YOK; **iptal** + yeni pusula (VUK md.234) | Mevcut | Mevcut | `#gp-iptal-{id}` |
| `YedekTalebi` (BEKLIYOR) | — | Gerekçeli iptal | Mevcut | Mevcut | `#yd-iptal-{id}` |
| `YedekTalebi` (işlenmiş) | **SALT OKUNUR** — işlenmiş talep kanıttır | — | — | — | `DegismezIpucu` + iptal gerekçesi okunur |
| Açılış kalemi (`/kurulum`) | **SALT OKUNUR** — mühür öncesi geri alma | `POST /kurulum/acilis/geri-al` | Mevcut | Mevcut | Mevcut `DegismezIpucu` |

**Neden salt okunur:** MASAK bildirimi ve gider pusulası **yükümlülük/vergi belgesidir**; içeriği sonradan değiştirilirse denetimde "o gün ne beyan edildi" sorusu cevapsız kalır. Düzeltme yolu her ikisinde de **yeni, bağlı bir kayıttır** (ek beyan / yeni pusula), yerinde değişiklik değil.

---

## § 6 — Testler (önce / sonra)

| Ölçü | Önce | Sonra |
|---|---|---|
| Tam süit | **3.179** (3.204 − 25) | **3.204** · Geçti 3.204 · Başarısız **0** · Atlanan 0 |
| Yeni test | — | **25** (`P13KartAyarUyumTestleri` 18 + `P13EkranTestleri` 7; içinde B-27 saf testi) |
| Uyarlanan mevcut test | — | **3** (`KurulumEkranTestleri.Hata_satirlari_…` ham kod → kimlik · `UX1C4EkranTestleri.C4_14_…_SECILEBILIR` sözleşme değişimi · `SozlesmeSekilTestleri.Uclar` iki yeni kilit + `{kategori}` fikstürü) |
| Susturulan / atlanan test | **0** | **0** |
| Derleme | — | `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı / 0 Hata** |

**Kararsızlık kaydı (Anayasa md.1):** ilk tam süit koşumunda `BildirimEkranTestleri.Her_dugme_ya_istek_atar_ya_da_disabled_dir` **1 kez** kırmızı verdi (`is-calistir-VADE_HATIRLATMA: tıklanınca hiçbir şey olmadı`). Tek başına ve sonraki iki tam koşumda **yeşil**; bu paketin dokunduğu hiçbir dosya `Bildirimler.razor` değil. **Paralel koşumda zamanlama kararsızlığı** olarak kaydedildi, gizlenmedi (§ 12/4).

---

## § 7 — Mutasyon tablosu (12/12 — hepsi kırmızıya döndü)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M13-1 | `KategoriModulu`: ürün kapısı `if (urun > 0)` → `if (false)` | `Kategori_UrunBagliykenPasifeAlinamaz_SayiMesajdaYazar`, `Kategori_PatchAktifFalse_AyniKapidanGecer` | `cmp` birebir |
| M13-2 | `Guncelle` içindeki `PasifeAlmaKapisi` çağrısı silindi (PATCH arka kapısı) | `Kategori_PatchAktifFalse_AyniKapidanGecer` | `cmp` birebir |
| M13-3 | `GiderPusulasiKapisi`: adres kontrolü `if (false && …)` | `HurdaSaticiAdresiZorunludur` | `cmp` birebir |
| M13-4 | `SupheliEkBeyan`: durum kapısı `if (false)` | `EkBeyan_YalnizGonderilmisBildirimeDuzenlenir` | `cmp` birebir |
| M13-5 | Ek beyan bağı (`DuzeltilenBildirimId`) yazılmıyor | `EkBeyan_AsliDegistirmez_VeBaglar`, `EkBeyanin_EkBeyani_Duzenlenemez` | `cmp` birebir |
| M13-6 | `MaliyetYontemiGuncelle`: doğrulama `if (false)` | `MaliyetYontemi_GecersizDegerReddedilir` | `cmp` birebir |
| M13-7 | `DeaktifEt`: denetim izi satırı silindi | `Cari_PasifeAlinir_DenetimIziBagliKaytlariYazar` | `cmp` birebir |
| M13-8 | `AcilisHataKimligi` haritasından `DOVIZ_HESAP_FAZ_B` düşürüldü | `AcilisHataKimligi_TumIcKodlariKapsar`, `AcilisHataKimligi_HamKodDondurmez` | `cmp` birebir |
| M13-9 | `Yedekler.razor`: iptal gerekçesi bloğu `@if (false)` | `Yedekler_IptalGerekcesi_EkrandaGorunur` | `cmp` birebir |
| M13-10 | `Isletme.razor`: maliyet seçicisi yeniden kilitlendi (`disabled="@(true)"`) | `Isletme_KurKaynagiVeMaliyetYontemi_BoskenSecilebilir` | `cmp` birebir |
| M13-11 | `UrunAgaci.razor`: satırdaki "Pasife al" düğmesi silindi | `Kategori_PasifeAlDugmesi_BagliKaytlariOkurVeDeleteAtar`, `Kategori_DuzenlemeKaydi_AktifligeDokunmaz` **+ `denetim.sh` `KATEGORI_PASIFE_ALMA_YOLU` 2/3 FAIL** | `cmp` birebir |
| M13-12 | `PastaGrafik.razor`: 360° özel durumu `if (false)` | `B27_TekDilimliPasta_KapaliTamDaireCizer` | `cmp` birebir |

Hiçbir mutasyon "sessiz" geçmedi → bu pakette **test açığı ölçülmedi**. Son mutasyondan sonra `dotnet build` → **0 Uyarı / 0 Hata**.

---

## § 8 — denetim.sh (MEKANİZMA KURALI)

`# >>> P-13` / `# <<< P-13` işaretli blok eklendi. Ölçüm komutları **betiğin içinde yorum olarak** yazılı; tabanlar **bugün taze ölçüldü** (belgeden kopyalanmadı — BK-07).

| Sayaç | Değer | Anlam |
|---|---|---|
| `P-13: GEREKCESIZ_KILITLI_ALAN` | **0** | 7 sayfada, kural kaynaklı her kilit ±14 satır içinde sebep taşır (geçici/rol kilitleri elenir) |
| `P-13: IC_KOD_SIZINTISI` | **0** | `PAKET n` / `FAZ_X` / `@…HataKodu` / çıktıdaki `BÜYÜK_HARF_ALT_ÇİZGİ` deseni yok |
| `P-13: KATEGORI_PASIFE_ALMA_YOLU` | **3/3** | uç + pasife al + aktife al birlikte (UX-S25 mekanik kilidi) |

Taban ölçümü sırasında **4 gerçek eksik bulundu ve kapatıldı** (sayaç "zaten sıfırdı" diye yazılmadı): `th-kasa` (aria bağı yok), `th-kaydet` (kilit sebebi yok), `/uyum` maskeleme düğmesi (sebep yok), `/kurulum` adım atlama düğmesi (mühür sebebi yok).

Çıktı: `dotnet/denetim.sh` → **DENETİM TEMİZ** (141 ✓, 0 ✗; ⚠ satırları P-02 devirleridir, bu paketle ilgisiz).

> ⛔ **DÜZELTME (tur 2, 2026-09-21 — Anayasa md.1):** yukarıdaki iki sayaç ilk teslimde **fiilen hiç koşmuyordu.** Betik bash (`#!/usr/bin/env bash`), döngüler ise `for f in ${=P13_SAYFALAR}` (zsh sözdizimi) yazılmıştı; bash `bad substitution` verip döngüyü **hiç çalıştırmadı**, `P13_KILIT`/`P13_SIZINTI` başlangıç 0 kaldı ve betik yine de "✓ … (7 sayfa tarandı)" basıp `exit 0` döndü. Yani ilk rapordaki "7 sayfa tarandı" beyanı **kanıt değildi**. Düzeltildi ve gerçek tabanlar yeniden ölçüldü — ayrıntı ve kanıt § 14.

**Tavan artırılmadı.** Tersine, iki cırcır ihlali (`ModalPencere` 54→55, `@onclick'siz button` 71→73, `SATIR_ICI_STYLE` 155→156) **kod düzeltilerek** geri alındı; tavana dokunulmadı.

---

## § 9 — Canlı doğrulama (localhost:**5198** · 5199'a DOKUNULMADI)

Test kiracısı **`PAKETP13-TEST Kuyumculuk`**. Tüm yanıt gövdeleri **okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /urun-kategorileri` "P13 Ziynet" | 201 | **201** · id döndü |
| 2 | `POST /urun-kategorileri` alt "P13 Bilezik" | 201 | **201** |
| 3 | `GET /urun-kategorileri/{ust}/bagli-kayitlar` | 200 | **200** · `altKategoriSayisi: 1, urunSayisi: 0, aktif: true` |
| 4 | `DELETE /urun-kategorileri/{ust}` (alt var) | 409 | **409** · "Bu kategorinin **1 aktif alt kategorisi** var; önce alt kategoriler pasife alınmalıdır" |
| 5 | `DELETE /urun-kategorileri/{alt}` | 200 | **200** · `{"mesaj":"Kategori pasife alındı","aktif":false}` |
| 6 | `DELETE /urun-kategorileri/{ust}` (artık temiz) | 200 | **200** · aynı yanıt |
| 7 | `PATCH /urun-kategorileri/{ust}` `{aktif:true}` | 200 | **200** · `aktif: true` (geri dönüş yolu çalışıyor) |
| 8 | `PATCH /admin/tenant/maliyet-yontemi` `{"KAFAMA_GORE"}` | 400 | **400** · "Geçersiz maliyet yöntemi… Seçilebilir: FIFO, LIFO, ORTALAMA" |
| 9 | `PATCH /admin/tenant/maliyet-yontemi` `{"LIFO"}` | 200 | **200** · `maliyetYontemi: "LIFO"` |
| 10 | `GET /kur/kaynaklar` | 200 | **200** · `["ALTINKAYNAK","ANKARA","TRUNCGIL"]` (ekran listeyi buradan alıyor) |
| 11 | `PATCH /admin/tenant/kimlik` `{telefon:"abc"}` | 400 | **400** · alan altı Türkçe mesaj (`alanlar.telefon`) |
| 12 | `PATCH /admin/tenant/kimlik` `{telefon:"05551112233"}` | 200 | **200** · `telefon: 05551112233` |
| 13 | `POST /masak/supheli` (320 karakter gerekçe) | 201 | **201** · `durum: ACIK`, `duzeltilenBildirimId: null` |
| 14 | `POST /masak/supheli/{id}/ek-beyan` (kayıt ACIK) | 400 | **400** · "Ek beyan yalnız MASAK'a GÖNDERİLMİŞ bildirim için düzenlenir; bu bildirim ACIK durumunda." |
| 15 | `PATCH /masak/supheli/{id}/gonderildi` | 200 | **200** · `durum: GONDERILDI`, `ref: MSK-P13-1` |
| 16 | `POST /masak/supheli/{id}/ek-beyan` | 201 | **201** · yeni id, `duzeltilenBildirimId` = asıl, `islemTutariTl: 250000` (bağ DEVRALINDI) |
| 17 | `GET /masak/supheli?durum=GONDERILDI` | asıl değişmemiş | **200** · asıl `GONDERILDI`, gerekçe uzunluğu **320** (üzerine yazılmadı), `duzeltilen: null` |
| 18 | `POST /cari` | 201 | **201** |
| 19 | `GET /cari/{id}/bagli-kayitlar` (boş) | 200 | **200** · `hareketSayisi: 0, tlBakiye: 0.00` |
| 20 | `POST /cari/{id}/veresiye` 900 TL | 200 | **200** |
| 21 | `GET /cari/{id}/bagli-kayitlar` (dolu) | 200 | **200** · `hareketSayisi: 1, tlBakiye: 900.00` |
| 22 | `DELETE /cari/{id}` | 200 + özet | **200** · `{"mesaj":"Cari deaktif edildi","aktif":false,"bagli":{…"tlBakiye":900.00}}` |
| 23 | `POST /islemler/hurda-alis` **adressiz** | 400 | **400** · "Hurda alış: **Satıcı adresi zorunludur (VUK md.234 — gider pusulası)**" |
| 24 | `POST /islemler/hurda-alis` adresli | 200 + pusula | **200** · `hasGrami: 4.58`, `giderPusulasi.pusulaNo: GP-2026-000001` |

### SQL kanıtı (kuyumcu_net, test kiracısı silinmeden önce)

```
cari_aktif       0
kategori         P13 Ziynet   1
kategori         P13 Bilezik  0
ek_beyan         01M30G2WW6…  ACIK        duzeltilen=01M30G2JWG…
ek_beyan         01M30G2JWG…  GONDERILDI  duzeltilen=NULL
denetim          Cari                  → "cari pasife alındı (hareket: 1 · TL bakiye: 900,00 · has bakiye: 0,000000)"
denetim          UrunKategorisi        → "kategori pasife alındı" ×2, "kategori aktife alındı" ×1
denetim          Tenant                → "maliyet-yontemi-degisti: (belirlenmemiş) → LIFO"
denetim          MasakSupheliBildirim  → "ek beyan düzenlendi (düzeltilen bildirim: 01M30G2JWG…, referans: MSK-P13-1)"
tenant_ayar      maliyet_yontemi=LIFO  telefon=05551112233
```

### Temizlik
- API **PID 93825** `kill` ile durduruldu · `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır**.
- **5199'a hiç dokunulmadı** (`lsof` ile doğrulandı).
- Test kiracısı `tenant_id` kolonu olan **57 tablodan** silindi + `kiraci_davetleri` satırı + `tenants` satırı. `SELECT COUNT(*) FROM tenants` → **8** (başlangıç değeriyle aynı). `cariler`/`masak_supheli_bildirimler` kalıntısı **0**.
- **`QA-TEST-` önekli kalıcı QA fikstürlerine dokunulmadı.**
- Jeton/parola hiçbir çıktıya yazılmadı; geçici jeton dosyaları `printf '' >` ile boşaltıldı.

---

## § 10 — Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı depo genelinde 311 satır (önceki paketlerin çalışma ağacı). **Bu paketin dokunduğu dosyalar:**

**Sunucu**
- `dotnet/Kuyumcu.Api/Modules/Urun/KategoriModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Hurda/GiderPusulasiKapisi.cs`
- `dotnet/Kuyumcu.Api/Data/MasakVarliklari.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20260920222736_P13MasakEkBeyanBagi.cs` (+ `.Designer.cs`, + güncellenen `KuyumcuDbContextModelSnapshot.cs`)

**Sözleşme / sözlük**
- `dotnet/Kuyumcu.Contracts/Sayfalar/CarilerSozlesmeleri.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/UrunAgaciSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/Sozlesmeler/KurulumSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs`

**Ekran (Dokunulan sayfalar — MÜHÜR 2 / tek dokunuş beyanı)**
- `dotnet/Kuyumcu.Web/Pages/UrunAgaci.razor`
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/Kuyumcu.Web/Pages/Isletme.razor`
- `dotnet/Kuyumcu.Web/Pages/Yedekler.razor`
- `dotnet/Kuyumcu.Web/Pages/Uyum.razor`
- `dotnet/Kuyumcu.Web/Pages/Kurulum.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/PastaGrafik.razor` *(yalnız mutasyon turu — dosya `cmp` ile birebir geri alındı, kod DEĞİŞMEDİ)*
- **`Pages/GiderPusulalari.razor`** plan listesindeydi ama **açılmadı**: ölçümde kusur bulunmadı (iptal düğmesi gerekçeli, TCKN maskeli). Gereksiz dokunuş yapılmadı.

**Betik / test**
- `dotnet/denetim.sh` (`# >>> P-13` bloğu)
- `dotnet/Kuyumcu.Tests/P13KartAyarUyumTestleri.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/P13EkranTestleri.cs` (YENİ)
- `dotnet/Kuyumcu.Tests/KurulumEkranTestleri.cs` · `UX1C4EkranTestleri.cs` · `SozlesmeSekilTestleri.cs` (sözleşme uyarlaması)

**Rapor:** `dokumanlar/09-uygulama-kayitlari/PAKET-P-13-uygulama-raporu.md`
`MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md` ve `SONRAKI-OTURUM-PROMPT.md` dosyalarına **DOKUNULMADI**.

---

## § 11 — Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan bloğu bağlayıcı (Dosyalar/DoD/Sayaçlar) | ✅ | § 1, § 4, § 8; sapmalar § 2'de gerekçeli |
| Kapsam kilidi — yalnız bu paketin kimlikleri | ✅ | Başka paketin kimliğine dokunulmadı; büyüyen işler "devredildi" (§ 1, § 12) |
| Hard DELETE yok (cari, kategori) | ✅ | `DELETE` = soft; SQL'de satırlar yerinde (`aktif=0`) |
| Bağlı kayıt sayısı uyarısı | ✅ | Kategori 409 + sayı · cari onayda sayı + denetim izi |
| Kur kaynağı / maliyet yöntemi boşken kilitli değil | ✅ | § 9 adım 8-10 · `C4_14_…_SECILEBILIR` |
| `/yedekler` iptal gerekçesi okunur | ✅ | `#yd-iptal-gerekce-{id}` + test |
| `/uyum` GONDERILDI düzeltme yolu | ✅ | Ek beyan ucu + ekran + § 9 adım 13-17 |
| Kimlik tespiti kaydı üretilip ölçüldü | ⚠ KISMEN | Kimlik tespiti kaydı **mevcut akıştan** (POS/MASAK eşiği) doğar; bu pakette yeni bir üretim yolu açılmadı. `/uyum` tespit sekmesi ve maskeli liste ölçüldü, kusur bulunmadı. `devredildi: QA turu 7 (rota taraması)` |
| Gider pusulası maskeleme deseni | ✅ | Tek kaynak `MasakServisi.Maskele`, ikinci kopya yazılmadı (§ 2/S5) |
| Adres boş bırakılamaz (VUK md.234) | ✅ | Yapılandırmadan; § 9 adım 23-24; M13-3 |
| `/isletme` telefon şube formuyla hizalı | ✅ | Tek kaynak `Iletisim.TelefonGecerli`; gerileme testi eklendi |
| B-17 tutucu davranış korundu + gerekçe ekranda | ✅ | Kod değiştirilmedi; `#kur-tl-kural` eklendi |
| K-25 gelmezse geçmiş boş adresli kayıtlar zorlanmaz | ✅ | Kural yalnız YENİ kayıt yolunda (`Dogrula`); listeleme/okuma dokunulmadı |
| Ham hata kodu gösterilmez + hata kimliği | ✅ | `AcilisHataKimligi`; `IC_KOD_SIZINTISI=0`; M13-8 |
| Sayaçlar denetim.sh'a cırcır olarak eklendi | ✅ | § 8 (+ 1 destek sayacı) |
| Tavan/eşik artırılmadı | ✅ | 3 cırcır ihlali kod düzeltilerek geri alındı (§ 8) |
| Tip ve sözleşme (ad + tip, Contracts'ta) | ✅ | `CariBagliKayit`, `KategoriBagliKayit` tek tip; şekil kilidine eklendi |
| Pages'te yeni istemci record açılmadı | ✅ | İkisi de `Kuyumcu.Contracts` altında |
| PATCH sözleşmesi (gönderilmeyen = değişmedi) | ✅ | Kategori PATCH'i `aktif` göndermiyor; cari `aktif` tek başına gidiyor |
| Tek yazma yolu / tek storno motoru | ✅ | `HareketFabrikasi` ve `StornoServisi`'ne dokunulmadı; MASAK ek beyanı tek çekirdekten (`SupheliOlusturKayit`) |
| Kiracı izolasyonu (IKiraciVarlik + filtre) | ✅ | Yeni kolon mevcut varlığa eklendi; yeni varlık açılmadı |
| DoD EK md.1 — her düğme istek atar ya da gerekçeli disabled | ✅ | `GEREKCESIZ_KILITLI_ALAN=0`; 4 eksik gerekçe kapatıldı |
| DoD EK md.2 — tek düzenleme kalıbı, yeni ModalPencere yok | ✅ | Kategori onayı reyon penceresiyle ORTAKLAŞTIRILDI (§ 2/S6) |
| DoD EK md.3 — alan altı Türkçe mesaj | ✅ | `#th-kaydet-kilit`, sunucu 400'leri Türkçe + hata bandı |
| DoD EK md.4 — yıkıcı eylem onayı + geri dönüş | ✅ | Kategori/cari: onay penceresi + "Aktife al" |
| Sozlukler TR + EN, çıplak metin yok | ✅ | 21 anahtar × 2 dil |
| bUnit sahte API gerçekçi yanıt | ✅ | `P13EkranTestleri.SahteApi` — dokunulan her uca gerçek şekil |
| Mevcut element id'leri değişmedi | ✅ | Yalnız yeni id eklendi (`k-pasife-*`, `c-pasif-bagli`, `uy-ek-beyan-*`, `kur-tl-kural`, `th-kaydet-kilit`, `yd-iptal-gerekce-*`) |
| SVG/CSV sayıları InvariantCulture | ✅ | `PastaGrafik` kuralı teste bağlandı (B-27) |
| Yeni CSS'te sabit renk yok / satır içi style yok | ✅ | Yeni style eklenmedi; `esit-pay` sınıfı kullanıldı, `SATIR_ICI_STYLE` tavanı korundu |
| Tam süit 0 başarısız | ✅ | 3.204/3.204 |
| `denetim.sh` TEMİZ | ✅ | § 8 |
| Test kiracısı silindi, `tenants` başlangıca döndü | ✅ | 8 → 8 |
| COMMIT YOK | ✅ | Hiçbir git komutu çalıştırılmadı |

---

## § 12 — Açık kalanlar

1. ~~`Down()` tatbikatı koşturulmadı.~~ **KAPANDI — düzeltme turu 2 (2026-09-21), bkz. § 14.2.** İzole kopya veritabanında (`kuyumcu_p13down`) Down() + yeniden Up() koşturuldu ve kolon/geçmiş sayımıyla kanıtlandı.
2. **`/raporlar` pasta `getBBox()` DOM ölçümü** hâlâ yapılmadı — kod tarafı teste bağlandı, tarayıcı ölçümü `devredildi: QA turu 7`.
3. **`/kullanicilar` tek SAHİP devir akışı** yok; metin artık doğru ama akış açılmadı (yeni özellik = kapsam büyümesi). `devredildi: SAHİP devir akışı paketi`.
4. **`BildirimEkranTestleri.Her_dugme_…` kararsızlığı** (§ 6). Bu paketin dosyalarıyla ilgisi yok; `devredildi: P-18 / QA betikleri` (paralel koşumda ateşle-unut zamanlaması).
5. **`Islemler.razor` satıcı adresi alan-altı işareti** — tek-el zinciri nedeniyle konmadı. `devredildi: P-16`.
6. **Kimlik tespiti kaydının üretilip ölçülmesi** — mevcut akıştan doğuyor; ayrı bir üretim yolu açılmadı (§ 11).
7. **Tahmin aşımı:** plan 360 ajan-dk; gerçekleşen bu oturumda daha kısa sürdü, ancak 3 cırcır ihlalinin geri alınması ve 5 mevcut testin/ sözleşmenin uyarlanması planda bütçelenmemişti — sapma, "yeni uç açan her paket sözleşme kilidi ve cırcır bedeli öder" kalemi olarak `ORKESTRATOR-DURUM.md`ye yazılmalı.

---

## § 13 — Kurucuya / mali müşavire notlar

1. **VUK md.234 — satıcı adresi (MALİ MÜŞAVİR TEYİDİ İSTENİR).** Gider pusulası düzenlenen her alımda satıcı adresi artık **zorunlu**. Bu bir okumadır, kesinlik değil: kural **koda gömülmedi**, `HurdaAyarlari:SaticiAdresiZorunlu` ile tek satırdan kapatılabilir. Teyit farklı çıkarsa kod değişmez, yapılandırma değişir. **Yalnız yeni kayıtları bağlar**; geçmiş adressiz pusulalar zorlanmıyor (K-25).
2. **K-24 (kredili mevduat / negatif açılış) bekliyor.** Kasa ve banka açılışında negatif bakiye hâlâ **reddediliyor** ve gerekçesi artık ekranda yazılı. Karar gelirse tek yerden (`AcilisKaynaklari`) gevşetilir. Plan bu konuda **tek taraflı hüküm koymadı**.
3. **MASAK ek beyanı.** Gönderilmiş bir bildirimin düzeltmesi artık "aslına bağlı yeni bildirim" olarak üretiliyor. **MASAK'ın kendi sistemindeki ek beyan/düzeltme usulüyle birebir eşleştiği DOĞRULANMADI** — ürün içinde yapılan şey, kaydın izlenebilir kalmasıdır; MASAK'a fiilî gönderim elle yapılır ve referans numarası girilir. Uyum danışmanına sorulması gereken kalem.
4. **Maliyet yöntemi artık seçilebilir** (FIFO / LIFO / ORTALAMA). Değişiklik **geçmişe dönük değildir**: kapanmış partiler yeniden hesaplanmaz. Muhasebe tutarlılığı için yöntemin dönem içinde değiştirilmemesi önerilir — bu kural **koda konmadı**, çünkü mevzuat teyidi yok.

---

## § 14 — DÜZELTME TURU 2 (2026-09-21) — hasım doğrulayıcı bulgularının kapatılması

Doğrulayıcı üç bulgu bıraktı (1 KRİTİK, 1 ORTA kapsam dışı, 1 DÜŞÜK). Bu turda **uygulama kodu değişmedi**; yalnız `dotnet/denetim.sh` düzeltildi ve eksik kanıtlar üretildi.

### 14.1 [KRİTİK] P-13 sayaçları hiç çalışmıyordu — KAPANDI

**Bulgu doğrulandı.** `dotnet/denetim.sh` bash betiğidir; P-13 bloğundaki iki döngü zsh'e özgü `${=VAR}` kelime-bölme sözdizimi kullanıyordu. Bash bunu `bad substitution` ile reddediyor, döngü gövdesi **bir kez bile** çalışmıyor, sayaçlar başlangıç değeri 0'da kalıyor ve betik "✓ … 7 sayfa tarandı" basıp `exit 0` döndürüyordu. **Sıfır dosya taranıyordu.** İlk raporun ve koşul tablosunun "KARSILANDI — 7 sayfa tarandı" beyanı kanıtsızdır (Anayasa md.1 ihlali, burada kabul ve tashih edilir).

**Düzeltme** (`dotnet/denetim.sh`, P-13 bloğu):

| Ne | Önce | Sonra |
|---|---|---|
| Liste | `P13_SAYFALAR="… … …"` (dizge) | `P13_SAYFALAR=(Cariler.razor UrunAgaci.razor Isletme.razor Yedekler.razor Uyum.razor GiderPusulalari.razor Kurulum.razor)` (**bash dizisi**) + `P13_BEKLENEN_SAYFA=7` |
| Döngü 1 (satır ~2538) | `for f in ${=P13_SAYFALAR}; do` | `P13_TARANAN=0` + `for f in "${P13_SAYFALAR[@]}"; do` + gövdede `P13_TARANAN=$((P13_TARANAN + 1))` |
| Döngü 2 (satır ~2583) | `for f in ${=P13_SAYFALAR}; do` | `P13_TARANAN2=0` + `for f in "${P13_SAYFALAR[@]}"; do` + gövdede `P13_TARANAN2=$((P13_TARANAN2 + 1))` |
| Karar | `if [ "$P13_KILIT" -eq 0 ]` → ✓ | **önce tarama kanıtı:** `if [ "$P13_TARANAN" -ne "$P13_BEKLENEN_SAYFA" ] → ✗ "TARAMA YAPILMADI (n/7 sayfa) — sayac gecersiz"; HATA=1` · `elif [ "$P13_KILIT" -eq 0 ]` → ✓ (**sabit "7" yerine ölçülen `$P13_TARANAN` basılır**) |

Doğrulayıcının istediği "boş döngüde TEMİZ demeyi engelleyen koruma" bu **tarama sayacı kapısı** ile eklendi: sayaç artık yalnız *gerçekten 7 dosya taranmışsa* ✓ verebilir; dosya adı değişir/silinirse ya da döngü yine boş dönerse denetim **kırmızıya** düşer.

**Gerçek tabanlar (taze ölçüm, düzeltilmiş betikle — `scratchpad/denetim-p13-son.log:160-162`):**

```
✓ P-13: GEREKCESIZ_KILITLI_ALAN                  0 (-eq 0: 7 sayfa tarandı)
✓ P-13: IC_KOD_SIZINTISI                         0 (-eq 0: 7 sayfa tarandı)
✓ P-13: KATEGORI_PASIFE_ALMA_YOLU                uç + pasife al + aktife al
DENETİM TEMİZ.        (141 ✓ · 0 ✗ · exit 0)
```

Doğrulayıcının elle ölçümüyle (GERCEK_KILIT=0, GERCEK_SIZINTI=0) **birebir uyuşuyor** → bugünkü değerler doğruydu, kusur sayacın *mekanizmasındaydı*. MEKANİZMA KURALI artık fiilen karşılanıyor: sayaç gelecekteki gerilemeyi yakalar.

**Sayacın gerçekten çalıştığının mutasyon kanıtı** (`denetim.sh` mutasyonları; uygulama kodu değişmedi):

| Kod | Değişiklik | Sonuç | Geri alma |
|---|---|---|---|
| M13-13 | `"${P13_SAYFALAR[@]}"` → eski `${=P13_SAYFALAR}` (zsh-ism geri konur) | `bad substitution` (satır 2556 ve 2601) → **✗ TARAMA YAPILMADI (0/7 sayfa) — sayac gecersiz** ×2, **exit 1** (eski betik burada ✓ + exit 0 veriyordu) | `cmp` birebir |
| M13-14 | `Uyum.razor` sonuna gerekçesiz `<button disabled="@(veriYok)">` | **✗ P-13: GEREKCESIZ_KILITLI_ALAN 1 (-eq 0 olmalı — Uyum.razor:1)**, exit 1 | `cmp` birebir |
| M13-15 | `Kurulum.razor` sonuna `<span>Hata: DOVIZ_HESAP_FAZ_B</span>` | **✗ P-13: IC_KOD_SIZINTISI 1 (-eq 0 olmalı — Kurulum.razor:1)**, exit 1 | `cmp` birebir |

Üç mutasyonun üçü de kırmızı verdi; üçünün geri alınması `cmp` ile birebir doğrulandı. Mutasyon sonrası `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı / 0 Hata**.

### 14.2 [DÜŞÜK] `P13MasakEkBeyanBagi.Down()` koşturulmadı — KAPANDI

Üretim verisi olan `kuyumcu_net`'e **dokunulmadı**. İzole bir kopya veritabanı (`kuyumcu_p13down`) 55 migration ile sıfırdan kuruldu, sonra geri alma ve yeniden uygulama koşturuldu:

| Adım | Komut | Sonuç |
|---|---|---|
| 1 | `dotnet ef database update --project Kuyumcu.Api --connection "…database=kuyumcu_p13down…"` | `Applying … P13MasakEkBeyanBagi.` → **Done.** (exit 0) · `__EFMigrationsHistory` = **55** |
| 2 | `dotnet ef database update 20260920215658_P12BankaHareketStornoKaynak …` | **`Reverting migration '20260920222736_P13MasakEkBeyanBagi'.` → Done.** (exit 0) |
| 3 | SQL: kolon var mı? | `SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='kuyumcu_p13down' AND TABLE_NAME='masak_supheli_bildirimler' AND COLUMN_NAME='duzeltilen_bildirim_id'` → **0** (DropColumn çalıştı) · history = **54** |
| 4 | `dotnet ef database update …` (ileri) | `Applying … P13MasakEkBeyanBagi.` → **Done.** · kolon geri geldi: **`varchar(36)` / NULL** · history = **55** |
| 5 | Temizlik | `DROP DATABASE kuyumcu_p13down` · `SHOW DATABASES` → `information_schema kuyumcu kuyumcu_net kuyumcu_test mysql performance_schema sys` (kopya yok) |

`kuyumcu_net` doğrulaması (tur sonrası): `__EFMigrationsHistory` = **55** · `masak_supheli_bildirimler` = **7 satır** · `duzeltilen_bildirim_id` dolu = **0** · `tenants` = **8**. Geri alma yolu **kanıtlanmıştır**.

### 14.3 [ORTA] P-12'deki aynı zsh-ism — DEVREDİLDİ (kapsam dışı)

`dotnet/denetim.sh:2440` `for y in ${=P12_FINANSAL}; do` aynı hatayı taşıyor. Doğrulayıcının kendisi "P-13'ün sorumluluğu değildir, orkestratör kuyruğuna alınmalıdır" diyor; **KAPSAM KİLİDİ** gereği başka paketin sayacına dokunulmadı. Ölçüm: betikte kalan `${=` kullanımı **2** adet → satır **1586** (zaten `$P05_BEKLENEN` ile düzeltilmiş, `${=}` yalnız *yorum metninde* geçiyor — zararsız) ve satır **2440** (P-12, **gerçek hata**). `devredildi: dalga 3 kapısı — "sayaç birleştirme" kalemi`, tek iş: `${=P12_FINANSAL}` → `"${P12_FINANSAL[@]}"` (dizi) + P-12 tabanının yeniden ölçümü.

### 14.4 Tur 2 kapı ölçümleri (yeniden koşuldu)

| Kapı | Sonuç | Kanıt |
|---|---|---|
| Derleme | **0 Uyarı / 0 Hata** | `dotnet build Kuyumcu.sln --nologo` → exit 0 |
| Tam süit | **Toplam 3204 · Geçti 3204 · Başarısız 0 · Atlanan 0** | `dotnet test … --logger trx --results-directory …/trx-pP-13` · süre 48,86 sn · `test-p13-full.log:49392-49394` |
| denetim.sh | **DENETİM TEMİZ** (141 ✓, 0 ✗, exit 0) · `bad substitution` satırı **0** | `denetim-p13-son.log` |
| Canlı (5198) | API ayağa kalktı (`GET /saglik` → **200 `Healthy`**); P-13 uçlarının **tümü kayıtlı** (kimliksiz istek → **401 `Token bulunamadı`**, 404 değil): `GET /api/v1/urun-kategorileri`, `GET /api/v1/urun-kategorileri/{id}/bagli-kayitlar`, `GET /api/v1/kur/kaynaklar`, `GET /api/v1/masak/supheli`, `GET /api/v1/cari/{id}/bagli-kayitlar`, `PATCH /api/v1/admin/tenant/maliyet-yontemi`, `POST /api/v1/masak/supheli/{id}/ek-beyan`. Yanıt gövdeleri **okundu**. § 9'daki 24 adımlık işlevsel tur **tekrarlanmadı** — bu turda uygulama kodu değişmediği için (yalnız betik + kanıt); bu bir sapmadır ve burada açıkça beyan edilir. | PID 16238 · `kill` sonrası `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** · **5199'a dokunulmadı** (o portta zaten dinleyen yok, yalnız gözlendi) |
| Test kiracısı | Bu turda **açılmadı** → silinecek kayıt yok. `SELECT COUNT(*) FROM tenants` → **8** · `… WHERE ad LIKE 'PAKET%TEST%'` → **0** · `QA-TEST-` fikstürlerine **dokunulmadı** | SQL |

### 14.5 Tur 2'de değişen dosya

```
 M dotnet/denetim.sh      (yalnız P-13 bloğu: dizi + tarama sayacı kapısı + yorum güncellemesi)
```
Uygulama kodu, migration, test, razor **değişmedi**. Geçici dosyalar yalnız scratchpad altında; proje ağacına artık bırakılmadı. Git komutu çalıştırılmadı (COMMIT YOK).

### 14.6 Tur 2'de fark edilen, bu turda BIRAKILMAYAN artıklar (kurucuya)

`git status --porcelain` çalışma ağacında **önceki oturumlardan kalma** yedek/mutasyon artıkları gösteriyor — bu turda üretilmediler, silinmeleri `rm`/`mv` gerektirdiği için **kurucuya bırakıldı** (kırmızı çizgi: hook `rm`/`mv` bloklar):
`dotnet/denetim.sh.mutbak` · `dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs.bak` · `dotnet/Kuyumcu.Web/Pages/{Fiyatlandirma,Islemler,Urunler,VadeTakibi,Sayim}.razor.mutorj` · `dotnet/Kuyumcu.Api/{Data/HareketFabrikasi.cs,Modules/Rapor/RaporModulu.cs,Modules/Sayim/SayimModulu.cs,Modules/Urun/PirlantaKapisi.cs}.mutorj` (ve benzerleri). Ayrıca `dotnet/denetim.sh` git tarafından **takip edilmiyor** (`??`) — betiğin sürümlenmesi orkestratör kararına bırakıldı.
