# PAKET P-09 — KİMLİK, ERİŞİM, MFA VE YETKİ MATRİSİ · UYGULAMA RAPORU

- **Tarih:** 2026-09-20 · **Dalga:** DALGA 2 · **Sürüm:** 1.0
- **Roller:** Taşkın (backend) + Ertuğ (frontend) · **Model:** Opus
- **Plan bloğu:** `PLAN-TEK-SEFER-2026-09-18.md` satır 296-318
- **Kimlikler:** B-6 · B-20 · B-35 · B-36 · B-37 · Y-39 + Ramazan **K3 dilimi**
- **Bağlı koşullar:** BK-10 (Mehmet MFA vetosu) · BK-13 · BK-22 (Aşkın: bUnit tek başına
  yetmez, mutasyon kanıtı şart) · BK-35 (Ramazan: `PlanGuncelle` DALGA 2'ye çekildi)

> **Anayasa md.1 notu:** bu paketin **kod gövdesi önceki bir oturumda yazılmış ama
> raporlanmamış, sayacı eklenmemiş ve testi yazılmamıştı.** Bu oturum ("devam") kodu
> olduğu gibi devralmadı: koda karşı doğruladı, **iki gerçek kusur buldu ve düzeltti**
> (§ 3.1 ve § 3.2), sayaçları taze ölçümle ekledi, 32 test yazdı, 11 mutasyon kanıtı
> üretti ve canlı doğrulamayı 5198'de koştu. Devralınan kodun "çalıştığı" varsayılmadı.

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (P-04)

| Ölçü | Sonuç |
|---|---|
| `dokumanlar/09-uygulama-kayitlari/PAKET-P-04-uygulama-raporu.md` | **MEVCUT** |
| `PAKET-P-00 … P-08` raporları | **MEVCUT** (P-03 düzeltme eki + P-08 tur-2 dâhil) |
| Kapsam daraltma gerekti mi? | **HAYIR** — önkoşul çıktısı diskte var, kapsam tam |

## § 0.1 — PLAN RİSK MADDESİNİN DOĞRULANMASI

Plan: *"`[RolGerekli]` özniteliğinin **tanım dosyası hasım turunda bulunamadı** —
paket başlamadan aranır (**doğrulanmalı**)."*

**Ölçüldü ve BULUNDU:** `dotnet/Kuyumcu.Api/Auth/RolFiltresi.cs:10`

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RolGerekliAttribute(params string[] roller) : Attribute, IAuthorizationFilter
```

Depoda **176 satırda** referans edilir (kaynak + test). Risk maddesi **kapandı**;
hasım turunun bulamama nedeni büyük olasılıkla `--include='*.cs'` süzgeci olmadan
koşan bir grep'tir (PLAN § 1 md.2'deki `.mutorj` kirliliği sorunuyla aynı aile).

---

## § 1 — KİMLİK KAPANIŞ TABLOSU (zorunlu bölüm)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-6** — MFA uçtan uca canlı ölçüm; yarım kalan kurulum `/ayarlar`'da kilit yaratmaz | **KAPANDI** | Uç: `Modules/Auth/MfaModulu.cs:451` `Sifirla` + `:556` `POST /auth/mfa/sifirla`; ekran: `Pages/Ayarlar.razor:249,608`; test: `B6_Yarim_kalan_MFA_kurulumu_SIFIRLANABILIR`; canlı adım 14-15 (kur 200 → `durum=BEKLIYOR` → sıfırla 200 → satır 0, iz `mfa-kurulum-sifirlandi`) |
| **B-20** — kurtarma kodu yalnız başarılı doğrulamada tüketilir; `mfa-kurtarma-kullanildi` DB'de satır; uç adları hizalı | **KAPANDI** | `MfaModulu.cs:413-418` (parola yanlışken `KodCozumle` **hiç çağrılmaz**), `:497` iz satırı; test: `B20_Kurtarma_kodu_kullanimi_izde_SATIR_birakir_kod_YAZILMAZ` (satır **ve** kodun yazılmadığı **ve** `hassas=1`); **mutasyon M-P09-9** (BK-22: bUnit değil, mut kanıtı) |
| **B-35** — başarısız girişte kullanıcı varlığı ifşa edilmez; `:244` ve `:431` aynı generic 401 | **KAPANDI** | `AuthModulu.cs:252-256` (+ `SahteHash` ile **zamanlama yan kanalı** da eşitlendi), `:440-446`, sabit `:461`; testler: `B35_Olmayan_eposta_ve_yanlis_sifre_AYNI_401_ve_AYNI_metni_doner`, `B35_Ben_ucu_silinmis_kimlikte_404_DEGIL_401_doner`; canlı adım 2-3 (**iki istek de HTTP 401, metin birebir aynı**); mutasyon M-P09-1, M-P09-2 |
| **B-36** — tek SAHİP çıkmazı + devir; son SAHİP kendini kilitleyemez | **KAPANDI** | Kapı `KullaniciModulu.cs:471` `SonSahipKapisi`, devir `:388` + uç `:606`; **sıra kusuru düzeltildi (§ 3.1)**; testler: 5 adet (`B36_*`); canlı adım 8, 12, 16, 17; mutasyon M-P09-3, M-P09-4 |
| **B-37** — kullanıcı kaydı silinemiyor / pasife alınamıyor | **KAPANDI** | `AktiflikDegistir` `:362` + uç `PATCH /{id}/aktif` `:598`; `DELETE` ucu **hard DELETE yapmaz**; testler: 3 adet (`B37_*`); canlı adım 6-7 (DELETE sonrası satır DB'de, `aktif=0`; PATCH ile `aktif=1`); mutasyon M-P09-5 |
| **Y-39** — yetki matrisi `RotaBasliklari`'ndan otomatik üretilir; 10 yeni anahtar; negatif test | **KAPANDI** | Tek kaynak `Kuyumcu.Core/Erisim/RotaModulleri.cs`; `Moduller.Tumu = RotaModulleri.Modulleri` (`YetkiModulu.cs:33`); testler: 15 adet (`Y39_*`, iki yönlü kilit dâhil); canlı adım 13 (**22 modül**, yeni 10 anahtarın hepsi listede); sayaçlar `ROTA_YETKI_ESLESMEZ=0`, `OZNITELIKSIZ_UC=0`; negatif kanıt: mutasyon M-P09-10 (özniteliksiz uç → sayaç kırıldı), M-P09-11 (haritadan satır silindi → sayaç + 2 test kırıldı), M-P09-6 |
| **K3 dilimi (Ramazan)** — yetkisiz `PlanGuncelle` | **KAPANDI** | `AdminModulu.cs:175` servis içi kapalı liste + `:191` denetim izi + sınıf düzeyi `[RolGerekli("SAHIP")]` `:331`; testler: 3 adet (`K3_*`); canlı adım 9-11 (TEZGÂHTAR 403 · SAHİP 200 · uydurma plan 400) + SQL izi `plan-degisti: BASLANGIC → KURUMSAL`; sayaç `YETKISIZ_PLAN_YAZAN_UC=0`; mutasyon M-P09-7, M-P09-8 |

**Devredilen:** yok. Abonelik tablolarının kalanı zaten plan gereği **DALGA 7 / P-27**'dedir
(bu paketin kapsamı yalnız güvenlik dilimiydi).

---

## § 2 — DoD SÖZLEŞMESİ (Bertan mührü, 2026-09-16 12:12)

Bu paket **yeni varlık/tablo açmadı** (bkz. § 5 Migration). Dokunulan kayıt türleri:

| Kayıt türü | Düzenleme yolu | Silme / pasife alma | Yetki | Denetim izi | Ekran eylemi |
|---|---|---|---|---|---|
| `Kullanici` (rol) | `PATCH /kullanicilar/{id}/rol` | — (rol silinmez) | `[RolGerekli("SAHIP","MUDUR")]` | `kullanici-rol-degisti` | Kullanıcılar → satır içi rol |
| `Kullanici` (aktiflik) | `PATCH /kullanicilar/{id}/aktif` (iki yön) | **pasife alma** (hard DELETE **YOK**) | `[RolGerekli("SAHIP","MUDUR")]` | `kullanici-pasife-alindi` / `-aktiflestirildi` | Kullanıcılar → "Erişimi kapat" / "Aktifleştir" (ortak yıkıcı onay modalı) |
| `Kullanici` (sahiplik) | `POST /kullanicilar/{id}/sahiplik-devret` | — | `[RolGerekli("SAHIP")]` + **parola onayı** | `sahiplik-devredildi` | Kullanıcılar → "Sahipliği devret" (`k-devret-{id}`) |
| `Tenant.Plan` | `PATCH /admin/plan` | — (plan silinmez, değişir) | sınıf düzeyi `[RolGerekli("SAHIP")]` | `plan-degisti: X → Y` | İşletme ekranı |
| `KullaniciMfa` (yarım kurulum) | `POST /auth/mfa/sifirla` | **kayıt silinir** — finansal varlık DEĞİL, kişinin kendi kimlik kurulumudur; ETKİN kurulum bu uçla silinemez | `[HerkeseAcik("kendi hesabın")]` + jeton kimliği | `mfa-kurulum-sifirlandi` | Ayarlar → "Yarım kalan kurulumu sıfırla" |

**Salt okunur kalan ve gerekçesi:** `denetim_kayitlari` (iz kaydı tanım gereği değiştirilemez;
`DegismezIpucu` ile ekranda yazılıdır). **Finansal varlıkta hard DELETE:** bu pakette **yok**.

### DoD EK 1 / EK 2 karşılanması
- **Her düğme ya istek atar ya disabled + sebep:** `Kullanicilar.razor` devir düğmeleri
  `disabled="@kaydediliyor"` + `k-devir-kilit` ("Kilit sebebi: şifre gerekli") taşır.
- **Tek düzenleme kalıbı / ModalPencere tavanı:** tavan **ARTIRILMADI**. Devir onayı ayrı
  bir pencere olarak açılmıştı (54 > tavan 53); **iki yıkıcı onay tek ortak modalda
  birleştirildi** (DoD EK 1 md.4 "ortak onay modalı") → sayaç **53**'e döndü.
  `duzenlenenId` kalıbı **1**'de kaldı (ikinci kalıp açılmadı).
- **Dokunulan sayfalar (tek dokunuş kaydı):** `Pages/Kullanicilar.razor`,
  `Pages/Yetkiler.razor`, `Pages/Ayarlar.razor` — üçü de planın P-09 dosya listesindedir.

---

## § 3 — SPESİFİKASYON ↔ KOD DOĞRULAMASI VE SAPMALAR

Plandaki satır numaraları kaymıştı (beklenen); hedefe sadık kalındı. İki **gerçek kusur**
bulundu ve düzeltildi:

### § 3.1 — B-36 kapısı ULAŞILAMAZ durumdaydı (bulgu, düzeltildi)

`SonSahipKapisi` üç yazma yolunda da `HedefUzerindekiYetkiKontrol`'ün **arkasında**
duruyordu. Rütbe kapısı `Seviye(islem.Rol) <= Seviye(hedefRol)` ile çalışır; hedef SAHİP
iken hiçbir rol SAHİP'ten yüksek olmadığı için **rütbe kapısı her zaman önce patlıyor**,
tek-SAHİP kapısı **hiç çalışmıyordu**. Sonuç: kullanıcı "eşit roldeki kullanıcılar
üzerinde işlem yapamazsınız" görüyor, **çıkış yolunu (devir) hiç öğrenemiyordu** — B-36'nın
kilit hissi tam olarak buydu.

**Düzeltme:** üç metotta (`Guncelle`, `RolDegistir`, `AktiflikDegistir`) sıra ters çevrildi
— **önce tek-SAHİP kapısı, sonra rütbe kapısı**. Bilgi sızıntısı doğurmaz: kullanıcı
listesi zaten `[RolGerekli("SAHIP","MUDUR")]` ile aynı kitleye rolleri gösteriyor.
Kanıt: canlı adım 12'de mesaj artık **"…Önce 'Sahipliği devret' ile yeni bir SAHİP
atayın."**; test `B36_Son_SAHIP_pasife_alinamaz_ve_mesaj_DEVRI_soyler` bunu metinle kilitler.

### § 3.2 — Vezne TAHSİL ucu "kasa" yerine "vezne" anahtarına çevrilmişti (güvenlik genişlemesi, geri alındı)

Devralınan kodda `VezneTahsilController` `[ModulYetkisi("vezne")]` taşıyordu — oysa
sınıfın **kendi doc-comment'i** ve Mehmet'in koşulu `"kasa"` diyor. Bu bir **genişlemedir**:
"vezne" yetkisi olup **"kasa" yetkisi olmayan** bir kullanıcı parayı tahsil edebiliyordu.
Y-39 "Vezne kendi anahtarını alsın" der; **para ucunu** açmasını istemez.
**Geri alındı:** fiş uçları `"vezne"`, PARA ucu `"kasa"` (`Modules/Vezne/VezneModulu.cs:735`).
Kanıt: `HttpVezneTestleri.Kasa_yetkisi_KALDIRILAN_tezgahtar_tahsil_EDEMEZ` yeniden **yeşil**.

### § 3.3 — Y-39'un bilinçli sözleşme değişikliği: 3 mevcut test güncellendi

Yeni anahtarlar (`pos`, `vezne`, `fiyat-listesi`, `sayim`) eski ödünç anahtarların
(`kasa`, `islem`, `urun`) yerini aldı. Bu **planın istediği değişikliktir** ve varsayılan
matris bugünkü erişimi birebir korur; ama **eski sözleşmeyi kodlayan 3 test kırmızıya
döndü**. Gizlenmedi, adlarıyla güncellendi:

| Eski test adı | Yeni ad / değişiklik |
|---|---|
| `HttpVezneTestleri.Islem_yetkisi_kaldirilan_tezgahtar_fis_ACAMAZ` | `Vezne_yetkisi_kaldirilan_tezgahtar_fis_ACAMAZ` (anahtar `islem` → `vezne`) |
| `HttpPosTestleri.PosUclari_KasaModuluKapaliKiracida_403` | `PosUclari_PosModuluKapaliKiracida_403` — ayrıca **`__tohum:pos__` markeri** yazıldı; yazılmazsa modül-başı tohumlama varsayılan izni açıyor ve test yanlışlıkla 200 görüyordu |
| `HttpFiyatYetkiTestleri.Urun_yetkisi_OLMAYAN_MUDUR_fiyat_listelerini_goremez_403` | `Fiyat_yetkisi_OLMAYAN_MUDUR_fiyat_listelerini_goremez_403` (matris `fiyat-listesi` / `sayim` üzerinden kurulur) |

**Susturulan test: 0.** Hiçbiri `Skip` edilmedi; üçü de aynı kuralı **yeni anahtarla** ölçüyor.

### § 3.4 — Plan ile koşullar arasında çelişki
**Yok.** Plan bloğunun Dosyalar/DoD/Sayaçlar listeleri birebir uygulandı.

---

## § 4 — MEKANİZMA: `dotnet/denetim.sh` SAYAÇLARI (`# >>> P-09` … `# <<< P-09`)

Blok `# <<< P-08`'in hemen ardına eklendi. **Tabanlar 2026-09-20'de TAZE ölçüldü**
(BK-07); her sayacın ölçüm komutu betikte **yorum olarak** yazılıdır. Hiçbir tavan
artırılmadı.

| Sayaç | Değer | Tanım / vakum koruması |
|---|---|---|
| `P-09: ROTA_YETKI_ESLESMEZ` | **0** (42 rota) | `Pages/*.razor` `^@page` ↔ `RotaModulleri.Harita` **iki yönlü**; rota sayısı < 40 ise ölçüm geçersiz sayılır ve denetim kırılır |
| `P-09: OZNITELIKSIZ_UC` | **0** (257 uç) | Her `[Http*]` eylemi metot ya da sınıf düzeyinde `RolGerekli` / `ModulYetkisi` / `AllowAnonymous` / `HerkeseAcik` taşır; tespit edilen uç sayısı < 250 ise ayrıştırıcı bozuk sayılır |
| `P-09: TEK_SAHIP_KILIT_YOLU` | **0** (4 yazan metot) | `Kullanici.Rol` / `.Aktif` yazan her metot `SonSahipKapisi`'nı çağırır (muaf: `Olustur`, `SahiplikDevret`); yazan metot < 3 ise ölçüm geçersiz |
| `P-09: YETKISIZ_PLAN_YAZAN_UC` | **0** (1 yazan nokta) | `tenant.Plan` yazan her nokta SAHİP kapısı + kapalı liste + denetim izi; `AdminModulu`'nda **SAHİP dışı rol listeleyen** `[RolGerekli]` açılırsa kırılır (yorum satırları sayılmaz) |
| `P-09: KIMLIK_IFSA_YOLU + MFA BÜTÜNLÜĞÜ` | **TEMİZ** | Generic 401 sabiti + `SahteHash` + giriş/`ben` yolunda 404 yok + `mfa-kurtarma-kullanildi` + `mfa-kurulum-sifirlandi` |

> **Ayrıştırıcı düzeltmesi (md.1):** ilk yazımda `TEK_SAHIP_KILIT_YOLU` awk'ı `sub()` içinde
> `\1` geri başvurusu kullanıyordu — POSIX awk bunu desteklemez ve sayaç **4 yerine 1**
> yazan metot görüp "ölçüm geçersiz" verdi. Gizlenmedi: `match()`/`substr()` ile
> yeniden yazıldı, sonuç 4'e çıktı.

> **`YETKISIZ_PLAN_YAZAN_UC` sıkılaştırması (md.1):** ilk hâli mutasyon **M-P09-8**'de
> (sınıf kapısı `SAHIP` → `SAHIP,MUDUR`) **yeşil kaldı**, çünkü grep doc-comment'lerdeki
> `[RolGerekli("SAHIP")]` metnine takılıyordu. Sayaç düzeltildi ve mutasyon tekrarlandı:
> artık **kırmızı** (`M-P09-8b`).

---

## § 5 — MIGRATION

**GEREKMEDİ.** Plan "ŞARTLI — SAHİP devri için denetim olayı yeterli olabilir
(**doğrulanmalı**)" diyordu. **Doğrulandı:**

- Sahiplik devri mevcut `kullanicilar.rol` kolonuyla ve `DenetimServisi.YazmaKaydet`
  olayıyla tam karşılanıyor; yeni tablo/kolon gerekmiyor.
- Yetki matrisi otomatik üretimi **kod düzeyi bir türetmedir** (`Moduller.Tumu`); veri
  geçişi gerekmiyor, çünkü modül-başı tohumlama markeri (`__tohum:<modul>__`, paket 6
  mekanizması) yeni anahtarları **idempotent** biçimde eski kiracılarda da açıyor.
  Canlı adım 13 bunu kanıtladı: yeni kiracıda 22 modül, yeni 10 anahtarın hepsi listede.

`__EFMigrationsHistory` = **53** (değişmedi). 0 baytlık migration sayısı **artmadı**.

---

## § 6 — DERLEME VE TEST

| Ölçü | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Hata · 0 Uyarı** |
| Tam süit ÖNCE (ORKESTRATOR-DURUM, 2026-09-20 06:12 kapısı) | 3032 / 3032 |
| Tam süit SONRA | **3064 / 3064 · Başarısız 0 · Atlanan 0** (49,2 sn) |
| Yeni test sayısı | **32** (`Kuyumcu.Tests/P09KimlikErisimTestleri.cs`) |
| Güncellenen mevcut test | 3 (§ 3.3) · **susturulan 0** |
| TRX | `…/scratchpad/trx-pP-09b/_Mac_2026-09-20_23_16_51.trx` |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** |

Denetim çıktısındaki 4 `⚠` satırı **bu paketten değildir** ve kapıyı kırmaz: P-02'nin
QA tur kapsamı (39/42), 5199 yayın anlık görüntüsünün bayatlığı, K-20 iCloud eşitlemesi
**AÇIK** (süre ölçümleri "geçersiz taban" damgalı — BK-42) ve BK-39 tur süresi ölçümü.

### Test kırılımı (32)
- **B-35:** 2 HTTP · **B-36:** 5 HTTP · **B-37:** 3 HTTP
- **Y-39:** 1 saf + 10 `[Theory]` + 3 saf + 1 HTTP = 15
- **K3:** 2 HTTP + 1 servis · **B-6:** 1 HTTP · **B-20:** 1 HTTP

---

## § 7 — MUTASYON KANITLARI (11/11)

Yöntem: kaynak `scratchpad/mut-yedek/P-09/` altına kopyalandı → `perl -0777 -pi -e` ile
**tek eşleşmeli** değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile
birebir doğrulandı. **Hiçbir mutasyon testsiz kalmadı.**

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-P09-1 | `AuthModulu`: giriş `UnauthorizedException(GirisHatasi)` → `NotFoundException("Kullanıcı bulunamadı")` | `B35_Olmayan_eposta_ve_yanlis_sifre_AYNI_401_ve_AYNI_metni_doner` **+ sayaç** `KIMLIK_IFSA_YOLU` | cmp birebir |
| M-P09-2 | `AuthModulu.Ben`: `?? UnauthorizedException` → `?? NotFoundException` | `B35_Ben_ucu_silinmis_kimlikte_404_DEGIL_401_doner` | cmp birebir |
| M-P09-3 | `SonSahipKapisi`: `aktifSahip <= 1` → `<= 0` | `B36_Son_SAHIP_pasife_alinamaz_ve_mesaj_DEVRI_soyler`, `B36_Son_SAHIBIN_rolu_de_dusurulemez` | cmp birebir |
| M-P09-4 | `SahiplikDevret`: parola doğrulaması `if (false)` | `B36_Devir_yanlis_parolayla_reddedilir_ve_roller_DEGISMEZ` | cmp birebir |
| M-P09-5 | `AktiflikDegistir`: `hedef.Aktif = aktif` → `db.Kullanicilar.Remove(hedef)` (hard DELETE) | `B37_DELETE_kaydi_SILMEZ_pasife_alir_satir_DB_de_KALIR`, `B37_Pasif_kullanici_GERI_aktiflestirilebilir_ve_iz_yazar` | cmp birebir |
| M-P09-6 | `Moduller.Tumu` türetme yerine eski 8'li sabit liste | **11 test** (`Y39_Moduller_Tumu_…` + 10 `Y39_Yeni_modul_anahtarlari_…`) | cmp birebir |
| M-P09-7 | `AdminServisi.PlanGuncelle`: kapalı liste kapısı `if (false)` | `K3_Servis_uydurma_plani_REDDEDER_DTO_ya_guvenmez` **+ sayaç** `YETKISIZ_PLAN_YAZAN_UC` | cmp birebir |
| M-P09-8 | `AdminController` sınıf kapısı `SAHIP` → `SAHIP, MUDUR` | `K3_Plan_ucu_MUDURu_reddeder` (sayaç o hâliyle **yeşil kaldı → sayaç açığı**) | cmp birebir |
| **M-P09-8b** | aynı mutasyon, **sayaç sıkılaştırıldıktan sonra** | **sayaç** `YETKISIZ_PLAN_YAZAN_UC` = 1 ≠ 0 → **DENETİM KIRMIZI** | cmp birebir |
| M-P09-9 | `MfaServisi.Kaldir`: kurtarma kodu **parola doğrulanmadan** çözümlenir (yan etki öne alınır) | `UX1C4MfaDevirTestleri.Kapatma_basarisizsa_kurtarma_kodu_HALA_gecerlidir` (**BK-22 / Aşkın koşulu**) | cmp birebir |
| M-P09-10 | `AuthController`'a **özniteliksiz** `[HttpGet("p09-negatif")]` ucu eklendi | **sayaç** `OZNITELIKSIZ_UC` = 1 ≠ 0 (planın istediği **negatif test**) | cmp birebir |
| M-P09-11 | `RotaModulleri.Harita`'dan `["/emanet"]` satırı silindi | **sayaç** `ROTA_YETKI_ESLESMEZ` = 1 (`/emanet`) + `Y39_Harita_ile_RotaBasliklari_IKI_YONLU_eslesir`, `Y39_Yeni_modul_anahtarlari_matriste_VAR("emanet")` | cmp birebir |

**Bulunan test açığı ve kapatılması (md.1):** M-P09-3'ün ilk koşumunda
`B36_Son_SAHIBIN_rolu_de_dusurulemez` **yeşil kaldı** — yalnız durum koduna (403) bakıyordu
ve rütbe kapısı da 403 döndüğü için mutasyonu göremiyordu. Test, reddin **hangi kapıdan**
geldiğini metinle kilitleyecek biçimde güçlendirildi; mutasyon tekrarlandı ve **kırmızıya
döndü**. Son mutasyondan sonra **TEMİZ `dotnet build` (0/0)** koşuldu.

---

## § 8 — CANLI DOĞRULAMA (localhost:5198 · 5199'a DOKUNULMADI)

API `--no-build` ile 5198'de başlatıldı (`lsof` ile LISTEN doğrulandı). Test kiracısı:
**`PAKET-P-09-TEST`** (`01M3078ZA6DWHB6FANFEQWW41S`). Davet jetonu yerel üretildi,
DB'ye **yalnız SHA-256 özeti** yazıldı; hiçbir sır rapora girmedi. Tüm yanıt gövdeleri
**okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek | SQL / kanıt |
|---|---|---|---|---|
| 1 | `POST /auth/kayit` (davetli) | 201 | **HTTP 201** | `tenants` 8 → 9 |
| 2 | `POST /auth/giris` — **olmayan** e-posta | 401 generic | **HTTP 401** · "E-posta veya şifre hatalı" | — |
| 3 | `POST /auth/giris` — **yanlış** parola | adım 2 ile **aynı** kod ve metin | **HTTP 401** · **metin birebir aynı** | B-35 kapandı |
| 5 | `POST /kullanicilar` (TEZGAHTAR) | 201 | **HTTP 201** | `rol=TEZGAHTAR, aktif=true` |
| 6 | `DELETE /kullanicilar/{id}` | 200 + **satır kalır** | **HTTP 200** · "Kullanıcı pasife alındı" | `kullanicilar` satırı **var**, `aktif=0` |
| 7 | `PATCH /kullanicilar/{id}/aktif {true}` | 200 | **HTTP 200** · "yeniden aktif edildi" | `aktif=1` |
| 8 | `PATCH /kullanicilar/{sahip}/aktif {false}` (kendi hesabı) | 403 | **HTTP 403** · "Kendi hesabınızın durumunu değiştiremezsiniz" | self-guard |
| 9 | `PATCH /admin/plan` — TEZGÂHTAR jetonu | 403 | **HTTP 403** · "Bu işlem için gerekli rol: SAHIP" | plan **değişmedi** |
| 10 | `PATCH /admin/plan {KURUMSAL}` — SAHİP | 200 + iz | **HTTP 200** · `plan=KURUMSAL` | `denetim_kayitlari`: `EKLEME · Tenant · plan-degisti: BASLANGIC → KURUMSAL` |
| 11 | `PATCH /admin/plan {BEDAVA}` | 400 | **HTTP 400** · "Plan beklenen biçimde değil" | `tenants.plan` **KURUMSAL** kaldı |
| 12 | Pasife alınmış SAHİP'in **hâlâ geçerli** jetonu son SAHİBİ kilitlemeye çalışır | 403 + **devir yolu** | **HTTP 403** · **"Bu işletmenin tek SAHİBİ pasife alınamaz ya da rolü düşürülemez. Önce 'Sahipliği devret' ile yeni bir SAHİP atayın."** | B-36 kapısı **ulaşılabilir** (§ 3.1) |
| 13 | `GET /yetkiler` — SAHİP | 200 + türetilmiş liste | **HTTP 200** · **22 modül** | `banka,bildirim,cari,denetim-izi,emanet,evrak,fatura,fiyat-listesi,islem,kasa,kullanici,kur,kurulum,pos,rapor,sayim,siparis,urun,uyum,veri-aktarim,vezne,yedek` — **Y-39'un 10 yeni anahtarının hepsi** |
| 14 | `POST /auth/mfa/kur` | 200 | **HTTP 200** · `hane=6, adimSaniye=30`, giz 32 karakter | `kullanici_mfa.durum = BEKLIYOR` |
| 15 | `POST /auth/mfa/sifirla` | 200 + kilit yok | **HTTP 200** · "Yarım kalan kurulum sıfırlandı" | `kullanici_mfa` **0 satır**; iz: `mfa-kuruldu`, `mfa-kurulum-sifirlandi` |
| 16 | `POST …/sahiplik-devret` — **yanlış** parola | 400 | **HTTP 400** · "Şifre hatalı" | roller **değişmedi** |
| 17 | `POST …/sahiplik-devret` — doğru parola | 200 | **HTTP 200** · `yeniSahipId=…8K2DC`, `yeniRolunuz=MUDUR` | eski SAHİP `MUDUR`, hedef `SAHIP`; **aktif SAHİP sayısı = 1**; iz: `sahiplik-devredildi: yeni SAHİP …, devreden … → MUDUR` |

> **Canlı olarak ölçülemeyen tek şey:** adım 12'nin kurulumu (ikinci SAHİP'in pasife
> alınması) HTTP'den yapılamıyor — bir SAHİP başka bir SAHİP'i rütbe kapısı yüzünden
> pasife alamaz. Bu yüzden **yalnız kurulum** SQL ile yapıldı; **ölçülen davranışın
> kendisi gerçek HTTP isteğidir** (403 + mesaj). Gizlenmedi.

### Temizlik
- API süreci **yalnız kendi PID'i** ile durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
- **5199'a hiç dokunulmadı** (zaten dinleyici yoktu).
- Test kiracısı `tenant_id` kolonu olan **58 tablodan** + `kiraci_davetleri` satırı +
  `tenants` satırı silindi. **`tenants` sayısı 8 → 9 → 8** (başlangıç değerine döndü);
  `kullanicilar` ve `denetim_kayitlari` artığı **0**.
- **`QA-TEST-1/2/3` fikstürlerine dokunulmadı** (listelendi, değiştirilmedi).
- Proje ağacına yeni `.mutorj` / `.bak` / `.tmp` **bırakılmadı**; mevcut 10 artık
  **önceki paketlerden kalmadır** ve silinmesi K-4 ile kurucudadır (hook `rm`'i bloklar).

---

## § 9 — DEĞİŞEN DOSYALAR

`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgecinden P-09'a ait olanlar:

**Bu oturumda değişen/eklenen:**
- `dotnet/denetim.sh` — `# >>> P-09` … `# <<< P-09` sayaç bloğu (5 sayaç)
- `dotnet/Kuyumcu.Tests/P09KimlikErisimTestleri.cs` — **YENİ**, 32 test
- `dotnet/Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs` — B-36 sıra kuralı (§ 3.1)
- `dotnet/Kuyumcu.Api/Modules/Vezne/VezneModulu.cs` — TAHSİL ucu `kasa`'ya geri (§ 3.2)
- `dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor` — ortak yıkıcı onay modalı + 2 düğme biçimi
- `dotnet/Kuyumcu.Tests/HttpVezneTestleri.cs`, `HttpPosTestleri.cs`, `HttpFiyatYetkiTestleri.cs` — § 3.3

**Önceki oturumda yazılmış, bu oturumda doğrulanan P-09 gövdesi:**
- `dotnet/Kuyumcu.Core/Erisim/RotaModulleri.cs` (**YENİ** — matrisin tek kaynağı)
- `dotnet/Kuyumcu.Api/Auth/HerkeseAcik.cs` (**YENİ** — gerekçeli "herkese açık" beyanı)
- `dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs`, `Modules/Auth/MfaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs`, `Modules/Admin/AdminModulu.cs`
- `dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs`, `Pages/Yetkiler.razor`, `Pages/Ayarlar.razor`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (TR+EN anahtarlar; çıplak metin yok)

**COMMIT YAPILMADI** — çalışma ağacında kalındı (kırmızı çizgi).

---

## § 10 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Her iş kuralı için hatayı yeniden üreten test | **EVET** | 32 yeni test, § 6 kırılımı |
| Mutasyon kanıtı (kır → adıyla kırmızı → geri al → `cmp`) | **EVET** | § 7, 11 mutasyon + 1 test açığı kapatıldı |
| Canlı doğrulama 5198 (HTTP kodu + SQL) | **EVET** | § 8, 17 adım |
| `dotnet/denetim.sh` TEMİZ | **EVET** | § 4, § 6 |
| Tam süit 0 başarısız | **EVET** | 3064/3064 |
| Test kiracısı silindi, `QA-TEST-*` korundu | **EVET** | § 8 temizlik, `tenants` 8 |
| Sayaçlar `denetim.sh`'a cırcır olarak eklendi | **EVET** | `ROTA_YETKI_ESLESMEZ` · `OZNITELIKSIZ_UC` · `TEK_SAHIP_KILIT_YOLU` · `YETKISIZ_PLAN_YAZAN_UC` (+ bütünlük sayacı) = **0** |
| Tavan/eşik artırılmadı | **EVET** | ModalPencere 54 → **53**'e düşürüldü (birleştirme), ölü düğme 73 → **71** |
| Migration prosedürü | **GEREKMEDİ** | § 5, `__EFMigrationsHistory` = 53 |
| Yeni istemci `record` açılmadı (Pages) | **EVET** | `P-06: BU_PAKETTE_EKLENEN_RECORD = 0` |
| Finansal varlıkta hard DELETE yok | **EVET** | Kullanıcı pasife alınır; bu pakette finansal varlık yazılmadı |
| Sır/parola çıktıya yazılmadı | **EVET** | Jeton ve parola yalnız scratchpad değişkeninde; iş sonunda boşaltıldı |
| COMMIT / `rm` / `mv` / git komutu yok | **EVET** | Yalnız `git status --porcelain` (salt okunur) |
| **BK-10** (Mehmet MFA vetosu) | **KANIT HAZIR** | 401 hizalaması + özniteliksiz uç negatif testi + MFA canlı: § 1, § 7, § 8. **Vetoyu kaldırma yetkisi Mehmet'tedir** — bu rapor onun turuna girdidir |
| **BK-13 / BK-35** (`PLAN_DEGISIMI_IZSIZ=0`) | **EVET** | Canlı adım 10'daki DB satırı + `YETKISIZ_PLAN_YAZAN_UC = 0` |
| **BK-22** (Aşkın: bUnit tek başına yetmez) | **EVET** | M-P09-9 mutasyon kanıtı |

---

## § 11 — AÇIK KALANLAR

1. **Mehmet'in vetosu bu raporla KALKMADI** — plan "canlı kanıt olmadan kalkmaz" diyor;
   kanıt üretildi, **kaldırma kararı Mehmet'in turundadır**.
2. **`SonSahipKapisi` yine de savunma katmanıdır.** Bugün onu HTTP'den tetikleyen tek
   gerçek senaryo, pasife alınmış bir SAHİP'in **hâlâ geçerli access token'ıdır**
   (jeton ömrü 15 dk). Kapı kaldırılırsa o pencerede kiracı kilitlenebilir — sayaç bunu
   korur. Jeton iptal (parola sürümü / oturum sonlandırma) genişletmesi **kapsam dışı**,
   ileri pakete not edildi.
3. **`.mutorj` / `.bak` artıkları (10 adet)** duruyor; silme **K-4 ile kurucudadır**
   (TurkReport hook `rm`'i bloklar). Bu paket yeni artık **eklemedi**.
4. **K-20 iCloud eşitlemesi AÇIK** — denetim `⚠` veriyor; bu paketin **süre** ölçümleri
   "geçersiz taban" damgalıdır (BK-42). Doğruluk ölçümleri etkilenmez.
5. **P-02 QA tur kapsamı 39/42** ve **5199 yayın anlık görüntüsü BAYAT** — ikisi de
   P-02/orkestratör kalemi, bu paketin kapsamı değil.
6. **Tahmin/gerçekleşen:** plan 300 ajan-dk. Gerçekleşen bu oturum için ölçülemedi
   (devralınan gövde önceki oturumda yazıldı, süresi kayıtlı değil). **Uydurulmadı** —
   `ORKESTRATOR-DURUM.md`'ye "kısmen ölçülemez" diye yazılması gerekir (BK-43).

---

## § 12 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **Mali müşavire not: YOK.** Bu paket mevzuat hükmü içermiyor; hiçbir yasal eşik veya
  oran koda sabitlenmedi.
- **Kurucuya, güvenlik (kısa):** bu paketten önce sisteminizde **üç gerçek açık** vardı ve
  üçü de kapandı: (1) giriş ekranı, bir e-postanın kayıtlı olup olmadığını tek istekte
  ele veriyordu; (2) tek sahipli bir işletmede sahip ayrılırsa hesap **kilitleniyordu** ve
  ekran çıkış yolunu söylemiyordu; (3) **abonelik planı**, yetki kapısı ve iz kaydı
  olmadan değiştirilebiliyordu. Ayrıca kullanıcı kaydı artık **hiçbir yolda silinmiyor**,
  yalnız pasife alınıyor ve geri açılabiliyor.
- **Kurucuya, karar gerektiren: YOK.** Bu paket hiçbir kurucu kalemine bağlı değildi.
- **Hatırlatma (K-20):** iCloud eşitlemesi hâlâ açık; takvim tahminleri bu yüzden
  "geçersiz taban" damgalıdır.

---
---

# TUR 2 — HASIM DOĞRULAMA BULGULARININ KAPANIŞI (2026-09-20, 23:20–23:55)

> Hasım doğrulayıcı turu **1 KRİTİK · 1 ORTA · 3 DÜŞÜK** bulgu bıraktı. Bu bölüm
> onları tek tek kapatır. **Anayasa md.1:** kapanmayan kalem "kapandı" diye
> yazılmadı; kapanamayan iki düşük kalem gerekçesiyle AÇIK bırakıldı.

## § T2.1 — [KRİTİK] Sessiz yetki genişlemesi: anahtar göçü kapısı — **KAPANDI**

### Bulgunun doğrulanması (önce iddia sınandı, sonra düzeltildi)
Bulgu haklıydı. `YetkiModulu.cs` tohumlaması, eski kiracıda **yalnız
`Moduller.TohumlamaOncesi`** (8 eski anahtar) için satır yazmayı atlıyordu.
P-09 ise dört kapıyı **yeniden adlandırmıştı** ve bu adlar o listede yoktu:

| Yeni anahtar | Devraldığı eski anahtar | Kanıt (kod) |
|---|---|---|
| `pos` | `kasa` | `Modules/Pos/PosModulu.cs:867` — "POS artık KENDİ yetki anahtarını taşır (\"kasa\" değil)" |
| `vezne` | `islem` | `Modules/Vezne/VezneModulu.cs:688` + sınıf doc'u ":681" — fiş uçları eskiden `islem` |
| `sayim` | `urun` | `Modules/Sayim/SayimModulu.cs:246` |
| `fiyat-listesi` | `urun` | `Modules/Fiyat/FiyatModulu.cs:813` |

Sonuç: eski kiracıda `Varsayilan` satırları **koşulsuz** yazılıyor, SAHİBİN
TEZGÂHTAR'dan bilerek aldığı `kasa` / `islem` / `urun` erişimi **yeni ad altında
geri açılıyordu** — üstelik para el değiştiren uçlarda (POS, vezne fişi).

### Hangi anahtarların göçmen SAYILMADIĞI ve nedeni (uydurulmadı, ölçüldü)
`emanet` ve `kur` haritaya **girmedi**. Gerekçe tahmin değil, `git diff` ile
ölçüldü: bu iki controller'da P-09 öncesi **hiç `ModulYetkisi` yoktu**
(`git diff -U0` çıktısı yalnız `+[Yetki.ModulYetkisi("emanet")]` ve
`+[Yetki.ModulYetkisi("kur")]` gösteriyor — eksi satır yok). Uç, kimliği olan
herkese açıktı; bunları koşullu yapmak erişimi **genişletmez, var olanı
KALDIRIRDI** (fail-closed regresyon). Menü anahtarları (`uyum`, `yedek`,
`veri-aktarim`, `denetim-izi`) da girmedi; uçları ayrıca `[RolGerekli]` ile kapalı.

### Düzeltme
- `Moduller.AnahtarGocu` (yeni anahtar → devraldığı eski anahtar[lar]) —
  `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs:48-80`
- `TohumlaGerekiyorsa` içinde `GocKapisi(rol, modul)` —
  aynı dosya `:~230-250`; eski kiracıda mevcut `(rol, modul)` satırları **bir kez**
  okunur, göçmen anahtarın satırı ancak rol eski anahtarın **tamamına** halen
  sahipse yazılır. Marker her hâlükârda yazılır (tohumlama TEK SEFERLİKTİR).
- Yeni test dosyası: `dotnet/Kuyumcu.Tests/P09AnahtarGocuTestleri.cs` — **11 test**

### Yeni testler (11)
| Test | Ölçtüğü |
|---|---|
| `AnahtarGocu_haritasi_kilitli_ve_tutarlidir` | harita birebir; her göçmen `Tumu`'da, `TohumlamaOncesi`'nde DEĞİL; her eski anahtar `TohumlamaOncesi`'nde |
| `Eski_kiracida_SAHIBIN_kaldirdigi_anahtar_yeni_ad_altinda_GERI_ACILMAZ` ×3 (`kasa`/POS · `islem`/vezne · `urun`/sayım) | 403 **+ DB'de satır yazılmadığı** |
| `Fiyat_listesi_de_urun_kaldirilinca_acilmaz` | 403 |
| `Eski_anahtari_DURAN_rol_yeni_anahtari_alir_200` ×4 | **fail-closed regresyon yasağı** |
| `Yeni_kiracida_goc_kapisi_hicbir_seyi_kapatmaz` | ilk kurulumda kapı devre dışı |
| `Gercekten_YENI_kapilar_gocmen_sayilmaz_ve_kosulsuz_tohumlanir` | `emanet`/`kur` koşulsuz + DB satırı |

### Mutasyon kanıtları (2/2)
| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M-P09-12** | `if (modeller.Contains(modul) && GocKapisi(rol, modul))` → `if (modeller.Contains(modul))` (bulgunun tarif ettiği hatalı hâl) | `Eski_kiracida_SAHIBIN_kaldirdigi_anahtar_yeni_ad_altinda_GERI_ACILMAZ` (**3 varyantın hepsi**) + `Fiyat_listesi_de_urun_kaldirilinca_acilmaz` → **4 kırmızı** · ayrıca `denetim.sh` sayacı **KIRILDI** | `cmp` **birebir aynı** |
| **M-P09-13** | `AnahtarGocu` haritasından `["pos"] = ["kasa"]` satırı silindi | `…GERI_ACILMAZ(eskiAnahtar: "kasa", …)` + `AnahtarGocu_haritasi_kilitli_ve_tutarlidir` → **2 kırmızı** | `cmp` **birebir aynı** |

### Yeni sayaç (cırcır) — `# >>> P-09` bloğuna eklendi
`P-09: GOC_KAPISIZ_TOHUMLAMA = 0` · taban **TAZE ölçüldü** (2026-09-20), ölçüm
komutu betikte yorum olarak yazılı. Vakum koruması: `AnahtarGocu` satır sayısı
**4'ün altına düşerse** kırılır (tavan artmaz, yalnız azalır).
**Sayacın kendisi mutasyonla sınandı:** M-P09-12 uygulandığında
`✗ P-09: GOC_KAPISIZ_TOHUMLAMA  tohumlama döngüsü GocKapisi'sız (kapılı=0 açık=1)`
ve `denetim.sh` **çıkış kodu 1**.

---

## § T2.2 — [ORTA] Kapsam beyanı ile gerçeğin uzlaştırılması — **KAPANDI (kısmi ölçüm dürüstçe işaretli)**

Bulgu haklıydı: tur 1'in `degisen_dosyalar` listesi eksikti. Ölçüm iki bağımsız
araçla yeniden yapıldı.

**Araç 1 — `grep -rln "P-09"` (Kuyumcu.Api/Core/Web/Tests, `*.cs` + `*.razor`): 23 dosya**
```
Kuyumcu.Api/Auth/HerkeseAcik.cs · Modules/Admin/AdminModulu.cs · Modules/Auth/AuthModulu.cs
Modules/Auth/MfaModulu.cs · Modules/Emanet/EmanetModulu.cs · Modules/Fiyat/FiyatModulu.cs
Modules/Kullanici/KullaniciModulu.cs · Modules/Kur/KurModulu.cs · Modules/Pos/PosModulu.cs
Modules/Sayim/SayimModulu.cs · Modules/Tevsik/TevsikController.cs · Modules/Vezne/VezneModulu.cs
Modules/Yetki/YetkiModulu.cs · Kuyumcu.Core/Erisim/RotaModulleri.cs · Kuyumcu.Core/I18n/Sozlukler.cs
Kuyumcu.Tests/HttpFiyatYetkiTestleri.cs · HttpPosTestleri.cs · HttpVezneTestleri.cs
Kuyumcu.Tests/P09AnahtarGocuTestleri.cs · P09KimlikErisimTestleri.cs
Kuyumcu.Web/Pages/Ayarlar.razor · Kullanicilar.razor · Yetkiler.razor
```
(+ `dotnet/denetim.sh` — "P-09" 24 kez geçiyor.)

**Araç 2 — çalışma penceresi mtime (`-newermt "2026-09-20 22:40"`): 37 dosya.**
Fark eden 14 dosya tek tek sınıflandırıldı:

| Dosya | Sınıf | Kanıt |
|---|---|---|
| `Modules/Masak/MasakModulu.cs` | **P-09 içerik değişikliği** | `HerkeseAcik` kullanıyor — bu öznitelik **P-09'da doğdu** (`Auth/HerkeseAcik.cs:4`), `OZNITELIKSIZ_UC=0` süpürmesi |
| `Modules/Meta/DuzeltmePolitikasi.cs` | **P-09** | aynı (`HerkeseAcik`) |
| `Modules/Meta/SurumModulu.cs` | **P-09** | aynı (`HerkeseAcik`) |
| `Modules/Sube/SubeModulu.cs` | **P-09** | aynı (`HerkeseAcik`) · tek TAKİPTEKİ dosya: `git diff --numstat` **+20/-0** |
| `Modules/Denetim/DenetimModulu.cs` | **P-09** | `[ModulYetkisi("denetim-izi")]` — anahtar P-09'da doğdu |
| `Modules/Disaaktarim/DisaAktarimModulu.cs` | **P-09** | `[ModulYetkisi("veri-aktarim")]` |
| `Modules/Iceaktarim/IceAktarimModulu.cs` | **P-09** | `[ModulYetkisi("veri-aktarim")]` |
| `Modules/Yedek/YedekModulu.cs` | **P-09** | `[ModulYetkisi("yedek")]` |
| `Modules/Urun/BelgeModulu.cs` | **OLASI P-09 — doğrulanamadı** | her uçta `[RolGerekli]` var (süpürme deseni) ama anahtar eski (`urun`); takip dışı, git ayırt edemiyor |
| `Kuyumcu.Tests/UX1C4MfaDevirTestleri.cs` | **OLASI P-09 — doğrulanamadı** | MFA/devir konusu P-09 ile örtüşüyor (7 eşleşme) |
| `Data/FaturaHareketBagi.cs` | **DOĞRULANAMADI** | yetki özniteliği yok, P-09 izi yok |
| `Data/HareketFabrikasi.cs` | **DOĞRULANAMADI** | aynı |
| `Kuyumcu.Tests/ArayuzModuTestleri.cs` | **DOĞRULANAMADI** | aynı |
| `Kuyumcu.Tests/K4ReferansKapisiTestleri.cs` | **DOĞRULANAMADI** | aynı |

> **Neden "doğrulanamadı" yazıyor, "no-op" değil (md.1):** depoda tek bir
> `first commit` var ve bu 14 dosyanın **13'ü takip DIŞI** (`git ls-files
> --error-unmatch` hata veriyor). İçerik farkı ile dokunuşsuz yeniden yazımı
> ayırt edecek bir taban **yok**. Tahmin edilmedi; ölçülemediği yazıldı.
> **Kurucuya öneri:** bir "taban anlık görüntüsü" commit'i (ya da `git stash
> create` benzeri salt okunur etiket) olmadan bu ayrım hiçbir pakette
> yapılamayacak. Karar kurucunundur (COMMIT YASAĞI kırmızı çizgidir).

### DoD EK 2 — plan dosya listesi dışındaki dokunuşların gerekçesi
| Dosya (plan listesinde YOK) | Gerekçe |
|---|---|
| `Modules/{Pos,Vezne,Sayim,Fiyat}` | **Y-39 zorunlu kıldı:** matris `RotaModulleri`'nden türetilince bu ekranların ödünç anahtarı kendi anahtarıyla değişmek zorundaydı; aksi hâlde `ROTA_YETKI_ESLESMEZ ≠ 0` |
| `Modules/{Emanet,Kur,Denetim,Disaaktarim,Iceaktarim,Yedek,Masak,Meta/*,Sube,Tevsik}` | **`OZNITELIKSIZ_UC = 0` zorunlu kıldı:** sayaç fail-closed; beyansız uç kalırsa denetim kırılır |
| `Kuyumcu.Api/Auth/HerkeseAcik.cs` | yukarıdaki sayacın "bilerek açık" beyanı — yeni dosya |
| `dotnet/denetim.sh` | mekanizma kuralı (sayaç cırcırı) |

**Tur 2'de benim dokunduğum dosyalar (tam liste, 3):**
1. `dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs` (AnahtarGocu + GocKapisi)
2. `dotnet/Kuyumcu.Tests/P09AnahtarGocuTestleri.cs` (**YENİ**)
3. `dotnet/denetim.sh` (`GOC_KAPISIZ_TOHUMLAMA` sayacı)
4. bu rapor dosyası

---

## § T2.3 — [DÜŞÜK] § 8 tablosunda eksik "adım 4" — **KAPANDI (numara boşluğu olarak)**

Tur 1 tablosunda 4 numara **gerçekten yok** ve 4 numaralı bir adım **koşulmadı**.
Sayılan satır **16**'dır, 17 değil. Numaralandırma tur 1'de kaydı ve düzeltilmedi.
**Düzeltme:** tur 1 § 8 tablosu **16 adımdır**; "17 adım" ifadesi yanlıştır.
Numara 4 boştur (atlanmış numara, atlanmış kontrol değil). Tur 2'nin kendi
canlı tablosu (§ T2.5) **kesintisiz** numaralandırıldı.

---

## § T2.4 — [DÜŞÜK] "SQL kanıtı" başlığı altında SELECT metni yok — **KISMEN KAPANDI**

Tur 1'in kanıt hücreleri sonuç özetiydi; sorgu metinleri oturum scratchpad'inde
kalmıştı ve o dizin oturuma özeldir — **bugün erişilemiyor**. Tur 1 kiracısı da
silinmiş durumda, dolayısıyla o satırların sorguları **yeniden koşulamaz**.
**Uydurulmadı.** Bunun yerine tur 2'nin tüm canlı adımları **sorgu metniyle
birlikte** kayda geçti (§ T2.5). Tur 1 hücreleri sonuç özetleri olarak durur ve
**"sorgu metni kayıtlı değil"** damgasını taşır.

---

## § T2.5 — CANLI DOĞRULAMA TUR 2 (localhost:5198 · 5199'a DOKUNULMADI)

Test kiracısı **`PAKETP-09-TEST`** (`01M308B84D6KGDFCBH90G89GYK`). Davet jetonu
yerel üretildi, DB'ye **yalnız SHA-256 özeti** yazıldı; hiçbir sır rapora girmedi.
Tüm gövdeler **okundu** (`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek | Kullanılan SELECT / kanıt |
|---|---|---|---|---|
| 1 | Başlangıç kiracı sayımı | — | **8** | `SELECT COUNT(*) FROM tenants;` |
| 2 | `POST /auth/kayit` (davetli) | 201 | **HTTP 201** | `SELECT COUNT(*) FROM tenants;` → **9** |
| 3 | `POST /auth/giris` — **olmayan** e-posta | generic 401 | **HTTP 401** · `"E-posta veya şifre hatalı"` | gövde okundu (B-35) |
| 4 | `POST /auth/giris` — **yanlış** parola | adım 3 ile **aynı** kod ve metin | **HTTP 401** · **metin birebir aynı** | gövde okundu (B-35) |
| 5 | `GET /yetkiler` — SAHİP | 200 + türetilmiş liste | **HTTP 200** · **22 modül** | `banka,bildirim,cari,denetim-izi,emanet,evrak,fatura,fiyat-listesi,islem,kasa,kullanici,kur,kurulum,pos,rapor,sayim,siparis,urun,uyum,veri-aktarim,vezne,yedek` (Y-39) |
| 6 | `POST /kullanicilar` (TEZGAHTAR) | 201 | **HTTP 201** | `SELECT CONCAT(rol,'\|',aktif) FROM kullanicilar WHERE tenant_id='<T>' AND rol='TEZGAHTAR';` → `TEZGAHTAR\|1` |
| 7 | **Eski kiracı simülasyonu**: `rol_yetkileri` tek `__yapilandirildi__` markerine indirildi; TEZGÂHTAR'dan `kasa`,`islem`,`urun` **kaldırıldı** | kurulum | TEZGÂHTAR = `banka,cari,fatura` | `SELECT GROUP_CONCAT(modul ORDER BY modul) FROM rol_yetkileri WHERE tenant_id='<T>' AND rol='TEZGAHTAR';` · `SELECT COUNT(*) FROM rol_yetkileri WHERE tenant_id='<T>' AND rol='__SISTEM__';` → **1** |
| 8 | API **yeniden başlatıldı** (30 dk'lık matris önbelleği temizlensin) | — | 5198 HAZIR | `lsof -nP -iTCP:5198 -sTCP:LISTEN` |
| 9 | `GET /pos/cihazlar` — TEZGÂHTAR | **403** | **HTTP 403** · `"Bu ekran için yetkiniz yok (pos)"` | gövde okundu |
| 10 | `GET /vezne/kuyruk` — TEZGÂHTAR | **403** | **HTTP 403** · `"Bu ekran için yetkiniz yok (vezne)"` | gövde okundu |
| 11 | `GET /sayim` — TEZGÂHTAR | **403** | **HTTP 403** · `"Bu ekran için yetkiniz yok (sayim)"` | gövde okundu |
| 12 | `GET /fiyat/listeler` — TEZGÂHTAR | **403** | **HTTP 403** · `"Bu ekran için yetkiniz yok (fiyat-listesi)"` | gövde okundu |
| 13 | **Tohumlama gerçekten koştu mu?** | markerler yazılmış olmalı | **1 → 23** | `SELECT COUNT(*) FROM rol_yetkileri WHERE tenant_id='<T>' AND rol='__SISTEM__';` → **23** |
| 14 | Göçmen anahtar satırı **yazılmadı** | 0 | **0** | `SELECT COUNT(*) FROM rol_yetkileri WHERE tenant_id='<T>' AND rol='TEZGAHTAR' AND modul IN ('pos','vezne','sayim','fiyat-listesi');` |
| 15 | **Fail-closed regresyon yok**: eski anahtarları DURAN MÜDÜR dördünü de aldı | 4 | **4** | `SELECT COUNT(*) FROM rol_yetkileri WHERE tenant_id='<T>' AND rol='MUDUR' AND modul IN ('pos','vezne','sayim','fiyat-listesi');` |
| 16 | Gerçekten YENİ kapılar koşulsuz tohumlandı | 2 | **2** | `SELECT COUNT(*) FROM rol_yetkileri WHERE tenant_id='<T>' AND rol='TEZGAHTAR' AND modul IN ('kur','emanet');` |
| 17 | `GET /kur/guncel` — TEZGÂHTAR | **200** | **HTTP 200** | erişim korundu (fail-closed regresyon yok) |
| 18 | TEZGÂHTAR listesi son hâli | göçmen yok, yeni kapı var | `banka,cari,emanet,fatura,kur,siparis` | `SELECT GROUP_CONCAT(modul ORDER BY modul) FROM rol_yetkileri WHERE tenant_id='<T>' AND rol='TEZGAHTAR';` |

> **İLK DENEMEDE YANLIŞ ÖLÇTÜM — GİZLENMEDİ (md.1):** 9-12. adımların ilk
> koşumunda dört uç da 403 döndü, **ama gerekçe farklıydı**:
> `"Şube bağlamı çözülemedi…"`. Test kullanıcısının `sube_id`'si NULL'dı ve şube
> ara katmanı yetki süzgecinden **önce** çalışıyor; marker sayısı **1'de kalmıştı**,
> yani tohumlama hiç koşmamıştı. "403 aldım, geçti" **denmedi**: kullanıcıya şube
> atandı, API yeniden başlatıldı ve ölçüm tekrarlandı. Yukarıdaki tablo **ikinci,
> geçerli** koşumdur (marker 1 → 23, mesajlar modül kapısının kendi mesajı).

### Temizlik
- API **yalnız kendi PID'leriyle** durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş (0)**.
- **5199'a hiç dokunulmadı** → `lsof -nP -iTCP:5199 -sTCP:LISTEN` **0** (zaten dinleyici yoktu).
- Test kiracısı `tenant_id` kolonu olan **58 tablodan** + `kiraci_davetleri` + `tenants`
  satırından silindi. `SELECT COUNT(*) FROM tenants;` → **8 → 9 → 8** (başlangıca döndü);
  `kullanicilar` artığı **0**, davet artığı **0**.
- `QA-TEST-1/2/3 Kuyumculuk` fikstürleri **listelendi, değiştirilmedi**.
- Proje ağacına yeni `.mutorj`/`.bak`/`.tmp` **eklenmedi**
  (`find … -newermt "2026-09-20 23:20"` → **0**; mevcut 17 artık önceki paketlerden).

---

## § T2.6 — TUR 2 DERLEME / TEST / DENETİM

| Ölçüm | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Hata · 0 Uyarı** |
| Tam süit ÖNCE (tur 1) | 3064 / 3064 |
| Tam süit SONRA | **3075 / 3075 · Başarısız 0 · Atlanan 0** (48 sn) |
| Yeni test (tur 2) | **11** (`P09AnahtarGocuTestleri.cs`) |
| TRX | `…/scratchpad/trx-pP-09c/_Mac_2026-09-20_23_29_39.trx` |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** (çıkış 0) · P-09 bloğu **6 sayaç**, hepsi ✓ |
| Migration | **GEREKMEDİ** — şema değişmedi; `__EFMigrationsHistory` = **53** (değişmedi) |

---

## § T2.7 — TUR 2 KOŞUL KARŞILAMA

| Bulgu | Durum | Kanıt |
|---|---|---|
| [KRİTİK] anahtar göçü / sessiz yetki genişlemesi | **KAPANDI** | § T2.1 · 11 test · M-P09-12/13 · canlı 9-18 · yeni sayaç |
| [ORTA] kapsam beyanı / değişen dosyalar | **KAPANDI** (4 dosya "ölçülemez" damgalı) | § T2.2 |
| [DÜŞÜK] eksik adım 4 | **KAPANDI** | § T2.3 — numara boşluğu; tur 1 = 16 adım |
| [DÜŞÜK] SELECT metni yok | **KISMEN** | § T2.4 — tur 2 tam metinli; tur 1 yeniden koşulamaz (kiracı silinmiş, günlük yok) |
| [DÜŞÜK] 5199 bayat yayın | **BU PAKETİN DEĞİL** | P-02 kalemi; kurala uyup 5199'a dokunulmadı (`lsof` → 0 dinleyici) |

## § T2.8 — TUR 2 AÇIK KALANLAR

1. **Tur 1'in § 8 SELECT metinleri kalıcı olarak kayıp** (§ T2.4). Sonuç değerleri
   duruyor, sorgu metinleri yeniden üretilmedi.
2. **4 dosyanın P-09 ile ilişkisi ölçülemedi** (§ T2.2). Taban commit'i olmadan
   çözülemez; karar kurucunundur.
3. **5199 yayın anlık görüntüsü BAYAT** — orkestratör kapısında `yayinla.sh`
   koşulmalı (P-02 kalemi).
4. **Mehmet'in vetosu** hâlâ onun turunda; bu bölüm veto turuna ek girdidir —
   kritik bulgu kapandığı için girdi **güçlendi**.
