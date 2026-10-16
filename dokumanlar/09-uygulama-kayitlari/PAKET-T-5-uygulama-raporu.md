# PAKET T-5 — Migration hijyeni: boş migration çiftlerinin tespiti + CI kuralı

> Dalga 3 · MÜHÜR 5 KOŞUL 6 · kurul tur 2 engel 20 (HAZIRLIK-KURULU-2.md sat. 42, 212, 213)
> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend payı yok — bu pakette ekran değişikliği YOKTUR)
> Tarih: 2026-09-17 · **COMMIT YOK** (çalışma ağacında)

---

## 1. Özet — ne yapıldı, ne YAPILMADI

**Yapılan:** bugünkü migration/artık borcu AD AD ölçüldü, canlı `__EFMigrationsHistory`
ile karşılaştırıldı, temiz MySQL'e sıfırdan kurulumla doğrulandı; ve **kapı kapatıldı**:
`dotnet/denetim.sh` § 12 + `Kuyumcu.Tests/MigrationHijyeniTestleri.cs` (11 test)
artık *yeni* bir 0 baytlık migration dosyası, *yeni* bir güdük Designer ya da *yeni*
bir `.mutorj` artığı girerse KIRMIZI verir. Bugünkü 25 kalem gerekçeli beyaz listededir.

**YAPILMAYAN (bilerek):** hiçbir dosya **silinmedi**. Silme Kurucu kalemidir
(`KURUCUYA-KARARLAR-2026-09-16.md` md. 4; ayrıca TurkReport kancası `rm`/`mv`'i bloklar).
Bu pakette **migration ÜRETİLMEDİ** (MÜHÜR 5 KOŞUL 5: dalga 3 seri hattının başı;
`dotnet ef migrations add` çalıştırılmadı). Şema değişmedi.

---

## 2. Tespit tablosu — 0 baytlık dosyalar ve güdük Designer'lar

Ölçüm: `find dotnet -name '*.cs' -size 0` + `grep -L BuildTargetModel *.Designer.cs`
Canlı teyit: `docker exec kuyumcu-mysql mysql … kuyumcu_net -e "SELECT MigrationId FROM __EFMigrationsHistory …"`

| # | Dosya | Bayt | `__EFMigrationsHistory`'de? | EF derlemesinde (`migrations list`)? | ModelSnapshot ile ilişki | Karar |
|---|---|---:|---|---|---|---|
| 1 | `20260915131748_FiyatListesiVeKampanya.cs` | 0 | **HAYIR** | HAYIR | yok (tip üretmiyor) | beyaz liste — silme Kurucu'da |
| 2 | `20260915131748_FiyatListesiVeKampanya.Designer.cs` | 0 | — | — | yok | beyaz liste |
| 3 | `20260915132123_FiyatListesiVeKampanya.cs` | 0 | **HAYIR** | HAYIR | yok | beyaz liste |
| 4 | `20260915132123_FiyatListesiVeKampanya.Designer.cs` | 0 | — | — | yok | beyaz liste |
| 5 | `20260915132303_FiyatListesiVeKampanya.cs` | 0 | **HAYIR** | HAYIR | yok | beyaz liste |
| 6 | `20260915132303_FiyatListesiVeKampanya.Designer.cs` | 0 | — | — | yok | beyaz liste |
| 7 | `20260915132645_FiyatListesiVeKampanya.cs` | 0 | **HAYIR** | HAYIR | yok | beyaz liste |
| 8 | `20260915132645_FiyatListesiVeKampanya.Designer.cs` | 0 | — | — | yok | beyaz liste |
| 9 | `20260915115025_VadeTakibi.Designer.cs` | **336** | migration'ın kendisi **EVET uygulandı** | EVET | **BuildTargetModel YOK** → o adımın model anlık görüntüsü kayıp | beyaz liste — düzeltmesi ayrı kalem (§ 8) |

**Kurul sayımı ile sapma:** kurul "4 boş migration çifti" dedi; ölçülen **4 çift = 8 dosya**
(çift = migration + Designer). Sayı tutarlı; rapor dosya adedini de yazıyor.
Ayrıca kurulun ayrıca saymadığı **9. kalem** (güdük `VadeTakibi.Designer.cs`) bu pakette
bulundu ve aynı kapıya bağlandı.

### SQL kanıtı (gerçek çıktı)

```
$ docker exec kuyumcu-mysql mysql -uroot -p"$P" -N kuyumcu_net \
    -e "SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId LIKE '%FiyatListesiVeKampanya%' OR MigrationId LIKE '%VadeTakibi%';"
20260915115025_VadeTakibi
20260915132909_FiyatListesiVeKampanya

$ … -e "SELECT COUNT(*) FROM __EFMigrationsHistory;"
33
```

→ Dört boş kimliğin (131748 / 132123 / 132303 / 132645) **hiçbiri** geçmişte yok.
Yalnız beşinci deneme (132909) uygulanmış. **Üretimde belirsizlik yoktur:** EF bu
dosyaları hiç görmemiş, veritabanı da onları hiç görmemiş.

```
$ dotnet ef migrations list --project Kuyumcu.Api --no-connect   → 33 migration
```
33 (derleme) = 33 (canlı `__EFMigrationsHistory`) — **birebir**.

### Temiz MySQL'e sıfırdan kurulum kanıtı

```
CREATE DATABASE kuyumcu_t5_dogrulama;
$ dotnet ef database update --project Kuyumcu.Api --connection "…database=kuyumcu_t5_dogrulama…"
… Applying migration '20260916184850_SiparisCatisi'.
Done.                                            (EXIT=0)

kuyumcu_t5_dogrulama: __EFMigrationsHistory = 33 satır, 42 tablo
kuyumcu_net        :                          33 satır, 42 tablo
$ diff <tablo listesi net> <tablo listesi t5>  → TABLO KUMESI AYNI (fark yok)
```
Sıfırdan kurulan şema, canlı şemayla **birebir aynı**. Boş dosyalar kurulumu bozmuyor,
eksik bir adım da bırakmıyor. Doğrulama veritabanı iş sonunda `DROP DATABASE` ile
kaldırıldı (`SHOW DATABASES LIKE 'kuyumcu_t5_dogrulama'` → 0 satır).

---

## 3. `.mutorj` artıkları envanteri (16 adet)

Ölçüm: `find . -name '*.mutorj' -not -path '*/bin/*'` = **16**.
`cmp` ile her yedek kendi kaynağıyla karşılaştırıldı:

| Yol (`dotnet/` göreli) | Kaynakla aynı mı (`cmp`) |
|---|---|
| `Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj` | **AYNI** |
| `Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs.mutorj` | **AYNI** |
| `Kuyumcu.Api/Modules/Vade/VadeKapisi.cs.mutorj` | **AYNI** |
| `Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs.mutorj` | FARKLI (eski sürüm) |
| `Kuyumcu.Api/Modules/Fiyat/FiyatMotoru.cs.mutorj` | FARKLI |
| `Kuyumcu.Api/Modules/Rapor/RaporModulu.cs.mutorj` | FARKLI |
| `Kuyumcu.Api/Modules/Satis/SatisModulu.cs.mutorj` | FARKLI |
| `Kuyumcu.Api/Modules/Sayim/SayimModulu.cs.mutorj` | FARKLI |
| `Kuyumcu.Api/Modules/Urun/BelgeModulu.cs.mutorj` | FARKLI |
| `Kuyumcu.Api/Modules/Urun/PirlantaKapisi.cs.mutorj` | FARKLI |
| `Kuyumcu.Api/Modules/Urun/UrunModulu.cs.mutorj` | FARKLI |
| `Kuyumcu.Web/Pages/Fiyatlandirma.razor.mutorj` | FARKLI |
| `Kuyumcu.Web/Pages/Islemler.razor.mutorj` | FARKLI |
| `Kuyumcu.Web/Pages/Sayim.razor.mutorj` | FARKLI |
| `Kuyumcu.Web/Pages/Urunler.razor.mutorj` | FARKLI |
| `Kuyumcu.Web/Pages/VadeTakibi.razor.mutorj` | FARKLI |

**Risk teyidi (kurulun endişesi doğru çıktı):** 13 dosya kaynağından FARKLIDIR — yani
**eski kod kopyalarıdır**. Bir ajan `Urunler.razor.mutorj:786`'ya bakıp "canlı kod" sanırsa
yanlış satırı okur. 3 dosya kaynağıyla aynıdır (zararsız ama gereksiz).
Hiçbiri git tarafından **izlenmiyor** (`git ls-files | grep -c mutorj` = 0) — yani depoya
girmemişler, yalnız çalışma ağacında duruyorlar. **Silme Kurucu kalemidir.**

---

## 4. Yapılan değişiklikler

### 4.1 `dotnet/denetim.sh` § 12 — "T-5 MİGRATION HİJYENİ" (yeni 5 sayaç)

| Sayaç | Kural | Bugünkü değer |
|---|---|---|
| `Migrations: beyaz liste dışı 0 baytlık .cs` | beyaz liste dışı 0 baytlık dosya = **HATA** | 0 ✓ |
| `Migrations: BuildTargetModel'siz Designer` | beyaz liste dışı güdük Designer = **HATA** | 0 ✓ |
| `Kaynak ağacı: beyaz liste dışı .mutorj` | beyaz liste dışı artık = **HATA** | 0 ✓ |
| `T-5 beyaz listesi güncel` | beyaz listedeki bir kalem artık yoksa = **HATA** ("ÇÜRÜDÜ") | 25 kalem ✓ |
| `.gitignore: *.mutorj` | satır yoksa = **HATA** | var ✓ |

Beyaz liste `BOS_MIGRATION_BEYAZ` (8) + `GUDUK_DESIGNER_BEYAZ` (1) + `MUTORJ_BEYAZ` (16)
değişkenlerinde **ad ad ve gerekçeli** durur. "Çürüme" sayacı, Kurucu dosyaları silince
beyaz listenin sessizce eskimesini engeller: silme sonrası denetim kırmızı olur ve
`SILME_NOTU` tam olarak ne yapılacağını yazar (Anayasa md. 1 — sessiz eskime yasak).

### 4.2 `dotnet/Kuyumcu.Tests/MigrationHijyeniTestleri.cs` — 11 yeni test (saf, DB'siz)

1. `Beyaz_liste_disinda_sifir_baytlik_migration_dosyasi_yok`
2. `Sifir_bayt_beyaz_listesi_curumez_her_kalem_hala_yerinde_ve_bos`
3. `Beyaz_liste_disindaki_her_Designer_BuildTargetModel_tasir`
4. `Guduk_Designer_beyaz_listesi_curumez`
5. `Bos_migration_kimlikleri_EFnin_gordugu_kumede_yok` (reflection: `MigrationAttribute`)
6. `Dolu_migration_dosyasi_ile_derlenen_migration_kumesi_birebir_ayni` (dosya kümesi = derleme kümesi; sabit sayı yazılmadı)
7. `ModelSnapshot_son_migrationin_Designer_modeliyle_ayni_varlik_kumesini_tasir`
8. `Beyaz_liste_disinda_mutorj_artigi_yok`
9. `Mutorj_beyaz_listesi_curumez`
10. `Gitignore_yeni_mutorj_artiklarini_disarida_tutar`
11. `Denetim_sh_ayni_beyaz_listeyi_ve_uc_sayaci_tasir` (**ikizlik**: iki liste ayrışamaz)

### 4.3 `.gitignore` — `*.mutorj`

Yeni artıkların depoya girmesini engeller. Mevcut 16 artık zaten izlenmiyor;
bu satır onları **silmez** (silme Kurucu kalemi).

---

## 5. Sapmalar ve gerekçeleri

| Spesifikasyon iddiası | Kodda gerçek | Karar |
|---|---|---|
| "4 boş migration çifti" | 4 çift = **8 dosya**, hepsi 0 bayt — doğrulandı | Hedefe sadık; rapor dosya adedini de veriyor |
| kurul: boş kimliklerin canlıda olup olmadığı "DOĞRULANMALI" | **Yok** (SQL kanıtı § 2) | Belirsizlik kapandı |
| kurul engel 213: "mut.sh'in `dotnet/betikler/` altına sürümlenmesi" | mut.sh projede yok, geçici dizinde | **Yapılmadı** — EK KOŞULLAR'daki T-5 kapsamı (5 madde) bu kalemi içermiyor; kapsam dışına taşmadım. `.gitignore` payı yapıldı. → § 8 açık kalan |
| kurul: "CI'da pre-push `dotnet test && denetim.sh`" | git kancası yazılmadı | **Yapılmadı** — kancalar Kurucu'nun makinesindeki `.git` yapılandırmasına dokunur ve COMMIT/git yasağı kapsamındadır. → § 8 açık kalan |
| güdük `VadeTakibi.Designer.cs` | kurul ayrıca saymamıştı; **336 bayt, BuildTargetModel yok** | Aynı kapıya bağlandı (yeni bulgu) |

**Mevzuat/yasal hüküm:** bu pakette mevzuata bağlı hiçbir hüküm yok (saf teknik hijyen);
koda sabit yazılan varsayım da yok.

**Migration:** *gerekmedi* — şema değişmedi, `dotnet ef migrations add` çalıştırılmadı
(MÜHÜR 5 KOŞUL 5/6: T-5 dalga 3 seri hattının **başıdır**, migration üretmez).
`kuyumcu_net` migration sayısı **33** (değişmedi).

---

## 6. Derleme, testler, denetim

```
$ dotnet build Kuyumcu.sln --nologo
    0 Uyarı
    0 Hata

$ dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo --logger trx \
    --results-directory …/scratchpad/trx-pT-5
Başarılı! - Başarısız: 0, Başarılı: 1861, Atlanan: 0, Toplam: 1861, Süre: 53 s
```

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---:|---:|---:|---:|
| `MigrationHijyeniTestleri` (filtreli) | 11 | 11 | 0 | 0 |
| **Tam süit (sonra)** | **1861** | **1861** | **0** | 0 |
| Tam süit (önce — ölçülmedi, türetildi: 1861 eksi 11) | 1850 | 1850 | 0 | 0 |

Yeni test sayısı: **+11**.

```
$ dotnet/denetim.sh
  ✓ Migrations: beyaz liste dışı 0 baytlık .cs     0
  ✓ Migrations: BuildTargetModel'siz Designer      0
  ✓ Kaynak ağacı: beyaz liste dışı .mutorj         0
  ✓ T-5 beyaz listesi güncel                       25 kalem
  ✓ .gitignore: *.mutorj                           var

DENETİM TEMİZ.                                     (EXIT=0)
```

---

## 7. Mutasyon kanıtları (8/8 öldürüldü)

Yöntem: kaynak `…/scratchpad/mut-yedek/T-5/` altına kopyalandı → `perl -0777 -pi -e` ile
TEK eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile
birebir aynılık kanıtlandı. Son mutasyondan sonra temiz `dotnet build` (0 uyarı 0 hata).

| Kod | Değişiklik | Kırmızıya dönen test | Geri alım |
|---|---|---|---|
| M5-1 | `BosMigrationBeyazListesi`'nden `20260915132645_…cs` düşürüldü | `Beyaz_liste_disinda_sifir_baytlik_migration_dosyasi_yok` | cmp OK |
| M5-2 | `GudukDesignerBeyazListesi` boşaltıldı | `Beyaz_liste_disindaki_her_Designer_BuildTargetModel_tasir` | cmp OK |
| M5-3 | `MutorjBeyazListesi`'nden `Urunler.razor.mutorj` düşürüldü | `Beyaz_liste_disinda_mutorj_artigi_yok` | cmp OK |
| M5-4 | dolu-dosya süzgeci `Length > 0` → `Length >= 0` | `Dolu_migration_dosyasi_ile_derlenen_migration_kumesi_birebir_ayni` | cmp OK |
| M5-5 | son migration seçimi `.Last()` → `.First()` | `ModelSnapshot_son_migrationin_Designer_modeliyle_ayni_varlik_kumesini_tasir` | cmp OK |
| M5-6 | `.gitignore`'dan `*.mutorj` satırı silindi | `Gitignore_yeni_mutorj_artiklarini_disarida_tutar` **+ denetim.sh ✗ `.gitignore: *.mutorj YOK` → DENETİM BAŞARISIZ** | cmp OK |
| M5-7 | `denetim.sh` beyaz listesinde `VadeTakibi.Designer.cs` → `YOK.Designer.cs` | denetim.sh ✗ `güdük Designer` **+** ✗ `T-5 beyaz listesi ÇÜRÜDÜ` → BAŞARISIZ; ayrıca `Denetim_sh_ayni_beyaz_listeyi_ve_uc_sayaci_tasir` | cmp OK |
| M5-8 | `denetim.sh` § 12 başlığı değiştirildi (ikizlik koptu) | `Denetim_sh_ayni_beyaz_listeyi_ve_uc_sayaci_tasir` | cmp OK |

Test açığı (hiçbir testi kırmayan mutasyon) **bulunmadı**.

---

## 8. Canlı doğrulama tablosu

Bu pakette **HTTP ucu yoktur** (ekran/API değişikliği yok), bu yüzden canlı doğrulama
veritabanı ve EF araç zinciri üzerinden yapıldı; 5198 API'si başlatılmadı, 5199'a
dokunulmadı.

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| `__EFMigrationsHistory` satır sayısı (`kuyumcu_net`) | 33 | **33** | SQL § 2 |
| 4 boş kimlik canlıda var mı | HAYIR | **yok** (sorgu yalnız 132909 + VadeTakibi döndü) | SQL § 2 |
| `dotnet ef migrations list` | 33 | **33** | § 2 |
| Temiz MySQL'e `database update` | EXIT 0, 33 migration | **EXIT 0, 33 satır, 42 tablo** | § 2 |
| Sıfırdan kurulan şema = canlı şema | fark yok | **`diff` boş** | § 2 |
| Doğrulama DB'si temizlendi | 0 | **`SHOW DATABASES LIKE …` → 0 satır** | § 2 |
| `tenants` sayısı (başlangıç → son) | 8 → 8 | **8 → 8** | `SELECT COUNT(*) FROM tenants` |
| Ağaçta `.mutorj` sayısı (iş sonu) | 16 (değişmedi) | **16** | `find` |
| Ağaçta `.bak`/`.tmp` artığı | 0 | **0** | `find` |

**Test kiracısı:** bu pakette kiracı **açılmadı** (gerek yoktu); `QA-TEST-` önekli
kalıcı fikstürlere dokunulmadı.

---

## 9. DoD sözleşmesi

Bu paket **yeni varlık/kayıt türü üretmez** — veri modeli, uç, ekran ve denetim kaydı
değişmemiştir. SÖZLEŞME-DoD'un varlık maddeleri bu yüzden **uygulanamaz (N/A)**, boş
bırakılmamıştır:

| Kalem | Durum | Gerekçe |
|---|---|---|
| Yeni varlık → PATCH + DELETE/pasife alma | **N/A** | Yeni varlık yok (0 migration, `Entities.cs` değişmedi) |
| `[ModulYetkisi]`/`[RolGerekli]` | **N/A** | Yeni uç yok |
| `DenetimServisi.YazmaKaydet` | **N/A** | Yazma yolu eklenmedi |
| Ekranda Düzenle/Pasife al ya da `DegismezIpucu` | **N/A** | Ekran dosyası açılmadı |
| Sozlukler TR+EN | **N/A** | Kullanıcıya dönen yeni metin yok; tüm yeni metinler geliştirici-kapısı çıktısıdır (denetim.sh + test mesajı), ekrana çıkmaz |
| Finansal varlıkta hard DELETE | **uyuldu** | Hiçbir şey silinmedi |

**Düzenlenebilirlik notu (UX-S25 ruhu, geliştirici yüzeyinde):** beyaz liste kalemleri
"değişmez" değildir — Kurucu silince beyaz liste kalkar ve bunu *denetim kendisi söyler*
(`T-5 beyaz listesi ÇÜRÜDÜ` + `SILME_NOTU`). Sessiz/görünmez kural bırakılmadı.

**UX sözleşmesi (Katman 8):** bu pakette ekran değişikliği olmadığı için UX-S21…S33
maddeleri uygulanamaz; **dokunulan sayfa: YOK** (TEK DOKUNUŞ / MÜHÜR 2 açısından bu
paket sıfır sayfa harcar). `denetim.sh` sayaçları (`Pages/*.razor` = 30, ModalPencere = 43,
`duzenlenenId` = 1) **değişmedi**.

---

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgecinden bu paketin dokunduğu satırlar:

```
 M .gitignore                                            (+7 satır: *.mutorj ve gerekçesi)
 ? dotnet/denetim.sh                                     (+~95 satır: § 12; dosya zaten izlenmiyor)
 ? dotnet/Kuyumcu.Tests/MigrationHijyeniTestleri.cs      (YENİ, 11 test)
 ? dokumanlar/09-uygulama-kayitlari/PAKET-T-5-uygulama-raporu.md (bu rapor)
```

Başka hiçbir dosya değiştirilmedi (mutasyon turları `cmp` ile birebir geri alındı).
**Commit yapılmadı.**

---

## 11. Açık kalanlar

1. **Silme — Kurucu kalemi (§ 12 tek komut).** 8 boş dosya + 16 `.mutorj` = 24 dosya.
2. **`VadeTakibi.Designer.cs` güdüklüğü.** Migration uygulanmış ama model anlık görüntüsü
   yok → o adıma kadar `dotnet ef migrations script` üretmek kör kalıyor. Düzeltmesi
   dosyanın elle yeniden üretilmesini gerektirir (riskli: sonraki 18 migration'ın
   Designer'ı zaten sağlam; **pratik etkisi yalnız geriye dönük script üretimi**).
   Öneri: ayrı bir T-5b kalemi, dalga 3 seri hattının SONUNDA, Kurucu silmesinden sonra.
3. **`mut.sh` sürümlenmesi** (`dotnet/betikler/` altına) — kapsam dışı bırakıldı (§ 5).
   Betik hâlâ yalnız geçici dizinde; oturum değişince kaybolma riski sürüyor (Madde 1 riski,
   kurul engel 213). Ayrı kalem olarak orkestratöre bırakıldı.
4. **pre-push git kancası** (`dotnet test && denetim.sh`) — git yapılandırmasına dokunduğu
   ve COMMIT/git yasağı kapsamında olduğu için yapılmadı. Kurucu onayında.

---

## 12. Kurucuya — tek komutluk silme önerisi

> `KURUCUYA-KARARLAR-2026-09-16.md` **md. 4**'e bağ. Bu komut **yalnız Kurucu tarafından**
> çalıştırılır (TurkReport kancası ajanlarda `rm`'i bloklar). Komut çalıştıktan **sonra**
> `denetim.sh` "T-5 beyaz listesi ÇÜRÜDÜ" diye kırmızı verir — bu **beklenen** davranıştır
> ve beyaz listenin temizlenmesi gerektiğini söyler (§ 12 ve `MigrationHijyeniTestleri`
> içindeki üç dizi boşaltılır, sonra `dotnet test` + `denetim.sh` yeniden yeşildir).

```bash
cd /Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery

# (a) 8 adet 0 baytlık migration dosyası — canlıda ve EF'te KARŞILIĞI YOK (rapor § 2)
rm dotnet/Kuyumcu.Api/Data/Migrations/20260915131748_FiyatListesiVeKampanya.cs \
   dotnet/Kuyumcu.Api/Data/Migrations/20260915131748_FiyatListesiVeKampanya.Designer.cs \
   dotnet/Kuyumcu.Api/Data/Migrations/20260915132123_FiyatListesiVeKampanya.cs \
   dotnet/Kuyumcu.Api/Data/Migrations/20260915132123_FiyatListesiVeKampanya.Designer.cs \
   dotnet/Kuyumcu.Api/Data/Migrations/20260915132303_FiyatListesiVeKampanya.cs \
   dotnet/Kuyumcu.Api/Data/Migrations/20260915132303_FiyatListesiVeKampanya.Designer.cs \
   dotnet/Kuyumcu.Api/Data/Migrations/20260915132645_FiyatListesiVeKampanya.cs \
   dotnet/Kuyumcu.Api/Data/Migrations/20260915132645_FiyatListesiVeKampanya.Designer.cs

# (b) 16 adet mutasyon yedeği — 13'ü ESKİ KOD KOPYASI (rapor § 3); hiçbiri git'te izlenmiyor
find dotnet -name '*.mutorj' -delete

# (c) teyit
dotnet build dotnet/Kuyumcu.sln --nologo     # 0 hata bekleniyor (EF bu dosyaları zaten görmüyordu)
```

**Risk:** yok denecek kadar düşük — (a)'daki dosyalar boştur, EF derlemesinde tip
üretmezler (`migrations list` 33'ü siliş öncesi de veriyordu) ve `__EFMigrationsHistory`'de
kayıtları yoktur; (b)'dekiler git tarafından izlenmiyor ve kaynak dosyaların yanında
duran eski kopyalardır.

**Mali müşavire not:** bu pakette mevzuat etkisi **yoktur** (saf teknik hijyen).
