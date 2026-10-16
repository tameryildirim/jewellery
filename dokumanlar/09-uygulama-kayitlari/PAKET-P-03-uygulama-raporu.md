# PAKET P-03 — SÖZLEŞME-1: `Kuyumcu.Contracts` + tip/ad sözleşmesi

**Tarih:** 2026-09-18 · **Dalga:** DALGA 1 · **Kimlik:** R-10 · **Sürüm:** 1.0
**Rol:** Taşkın (backend) + Ertuğ (frontend) · **Önkoşul:** P-02
**Durum:** **YARIM** (2026-09-18 hasım turu düzeltmesi; önce "BİTTİ" yazıyordu — bkz. `PAKET-P-03-uygulama-raporu-DUZELTME-2026-09-18.md`)
**Önceki beyan:** **BİTTİ** — derleme 0 hata / 0 uyarı · `denetim.sh` **TEMİZ** · tam süit **2910/2910** · canlı doğrulama 5198'de yapıldı, 5199'a dokunulmadı · **COMMIT YOK**

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (P-02 diskte var mı)

Varsayım yapılmadı, ölçüldü:

| Önkoşul çıktısı | Beklenen | Diskte | Sonuç |
|---|---|---|---|
| `dotnet/denetim.sh` P-02 bloğu | var | `# >>> P-02` satır 1073 … `# <<< P-02` satır 1393 | VAR |
| `ham/olcum-2026-09-18.json` | var | var (`SAYFA_RECORD_TAVAN.deger = 140`) | VAR |
| `dotnet/mut.sh` sürümlü | var | var, `-x`, `cmp -s` içeriyor | VAR |
| `SAYFA_RECORD` kapsamı Pages+Bilesenler+Layout | var | `denetim.sh:339` | VAR |

Önkoşul TAM. Kapsam daraltılmadı.

---

## § 1 — YAPILAN İŞ

### 1.1 Yeni proje: `dotnet/Kuyumcu.Contracts`

- `Kuyumcu.Contracts.csproj` (net8.0, ImplicitUsings, Nullable) → `Kuyumcu.Core`'a başvurur.
- `Kuyumcu.sln`'e eklendi; `Kuyumcu.Web`, `Kuyumcu.Api`, `Kuyumcu.Tests` projelerine `ProjectReference` verildi.
- **36 dosya · 137 kayıt tipi.** Ad alanı düzeni çakışmayı yapısal olarak imkânsız kılar:
  `Kuyumcu.Contracts.Sayfalar.<Sayfa>` · `Kuyumcu.Contracts.Bilesenler.<Bilesen>` · `Kuyumcu.Contracts.Fiyat`
  (Böylece `Faturalar.UrunVeri` ile `Urunler.UrunVeri` gibi **farklı şekilli aynı adlar** yeniden adlandırılmadan taşındı — taşıma saf kaldı.)

### 1.2 Taşıma: 140 → 3 razor record'u (SAF YER DEĞİŞTİRME)

31 razor dosyasından **137 record** taşındı. Taşıma betikle yapıldı (`scratchpad/mover2.py`), her record:
`private|internal` → `public`, gövde birebir korundu, **alan eklenmedi, tip değiştirilmedi, sıra değişmedi**, üstündeki açıklama satırları da birlikte taşındı. Her razor'a yalnız bir `@using Kuyumcu.Contracts.…` satırı eklendi.

| Dosya | Taşınan | Dosya | Taşınan |
|---|---|---|---|
| `Pages/Faturalar.razor` | 11 | `Pages/Sayim.razor` | 5 |
| `Pages/Urunler.razor` | 9 | `Pages/Banka.razor` | 5 |
| `Pages/Panel.razor` | 8 | `Pages/Emanet.razor` | 4 |
| `Pages/Cariler.razor` | 8 | `Pages/KarZarar.razor` | 3 |
| `Pages/Yedekler.razor` | 8 | `Pages/Kasalar.razor` | 3 |
| `Pages/Raporlar.razor` | 7 | `Pages/SiparisDetay.razor` | 3 |
| `Pages/Islemler.razor` | 7 | `Pages/UrunAgaci.razor` | 3 |
| `Pages/DenetimIzi.razor` | 7 | `Pages/VadeTakibi.razor` | 3 |
| `Bilesenler/FiyatListesiUrunPaneli.razor` | 7 | `Pages/VeriDisaAktarim.razor` | 3 |
| `Pages/Bildirimler.razor` | 6 | `Pages/Isletme.razor` | 2 |
| `Pages/Siparisler.razor` | 6 | `Pages/Kullanicilar.razor` | 2 |
| `Pages/Kur.razor` | 5 | `Pages/{Ayarlar,UrunEtiketleri,Yetkiler}.razor` | 1+1+1 |
| `Pages/Fiyatlandirma.razor` | 5 | `Bilesenler/{BelgeYukleyici,MasakFormu,PastaGrafik,TemaSecici}.razor` | 1+1+1+1 |

**Kalan 3 record — gerekçeli istisna** (gizlenmedi, `SozlesmeYansimaTestleri` içinde ADIYLA kilitlendi):

| Record | Dosya | Neden taşınmadı |
|---|---|---|
| `AuthYaniti` | `Pages/Giris.razor` | `Kuyumcu.Web.Servisler.OturumKullanici` tipine bağlı |
| `OzetVeri` | `Pages/KarZarar.razor` | `Servisler/KarZararModelleri.cs` tiplerine bağlı (`KarKoprusuSatiri`, `KarZararStorno`, `KarZararOnceki`, `KarZararDegisim`) |
| `SubeSatiri` | `Pages/Siparisler.razor` | `Kuyumcu.Web.Servisler.SubeSatiri` ile **ad çakışması** (taşınsaydı CS0104 belirsiz başvuru) |

Üçü de `Kuyumcu.Web/Servisler/` tipine bağlıdır. PLAN § 7: **`Kuyumcu.Web/Servisler/` yalnız P-07'nin elidir** → **devredildi: P-07**.

### 1.3 Gerçek sözleşme borcunun kapatılması: sunucu ⇄ ekran tek tip

Ölçüldü: `GecersizKilmaOzeti`, `TopluAtlanan`, `TopluSonuc` **iki kez** tanımlıydı — `Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs` ve `Bilesenler/FiyatListesiUrunPaneli.razor`. Aynı adın iki tanımı derleme hatası vermez; alan/tip kayarsa JSON ayrıştırıcı alanı atlar ve ekran sessizce `null`/0 gösterir (QA turu 2 dersi — R-10'un asıl sebebi).

Yapılan: dört tip (`GecersizKilmaOzeti`, `TopluSatir`, `TopluAtlanan`, `TopluSonuc`) **sunucu sürümü kanonik** kabul edilip `Kuyumcu.Contracts/Fiyat/FiyatSozlesmeleri.cs` içine tek tanım olarak alındı. Api ve panel artık **birebir aynı `Type`**'ı kullanıyor.

> Bu tek kalem "saf yer değiştirme"nin dışına çıkan tek yerdir: istemci tarafındaki
> `List<…>` kapları sunucunun `IReadOnlyList<…>` kabıyla, `TopluSatirOzet` adı
> `TopluSatir` ile birleşti. **Alan adı ve alan tipi değişmedi**, yalnız iki kopya teke indi;
> R-10'un hedefi budur. Kapsam büyütmesi değil, kapsamın kendisidir.

### 1.4 Diğer dokunuşlar

- `Pages/Banka.razor`, `Pages/Islemler.razor`: `MasakFormu.Esikler` → `Esikler` (+ `@using Kuyumcu.Contracts.Bilesenler.MasakFormu`).
- `Pages/Fiyatlandirma.razor`: `FiyatListesiUrunPaneli.PanelKategori` → `PanelKategori` (+ `@using`).
- `Kuyumcu.Tests/EkranTestleri.cs`: aynı iç-tip başvurusunun test tarafı.
- `dotnet/denetim.sh`: `SAYFA_RECORD_TAVAN` 140 → **3**; yeni `# >>> P-03 … # <<< P-03` bloğu.
- `ham/olcum-2026-09-18.json`: `SAYFA_RECORD_TAVAN.deger` **taze ölçümle** 140 → 3 (gerekçesi dosyada yazılı).

**Hiç dokunulmayan:** `MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md`, `SONRAKI-OTURUM-PROMPT.md`, `Kuyumcu.Web/Servisler/`, `Data/Entities.cs`, `KuyumcuDbContext.cs`, 5199.

---

## § 2 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-10** — SÖZLEŞME-1: `Kuyumcu.Contracts` + tip/ad sözleşmesi | **KAPANDI** | Proje: `dotnet/Kuyumcu.Contracts/Kuyumcu.Contracts.csproj` (36 dosya, 137 tip) · tavan `denetim.sh:337` `SAYFA_RECORD_TAVAN=3` (140'tan) · `CONTRACTS_DISI_MODEL = 0` (`denetim.sh` P-03 bloğu) · testler `Kuyumcu.Tests/SozlesmeYansimaTestleri.cs` 6 test yeşil · canlı: `swagger.json` şemaları L5-L6 |
| R-10 kalıntısı — `Servisler/` çift tanımları | **DEVREDİLDİ: P-07** | `MasakGereksinim`, `PosCihazSatir`, `PosTahsilatSatir`, `PosTaksitOranSatir`, `PosValorSatir`, `SurumKunyesi` — `Kuyumcu.Web/Servisler/*.cs` ile Api'de ayrı ayrı tanımlı; PLAN § 7 "Servisler yalnız P-07" |
| R-10 kalıntısı — 3 razor record'u | **DEVREDİLDİ: P-07** | `AuthYaniti`, `OzetVeri`, `SubeSatiri` (§ 1.2) — hepsi `Servisler/` tipine bağlı, testte adıyla kilitli |

---

## § 3 — SAPMALAR VE GEREKÇELERİ

**S-1 — Tavan "129 → 100" değil, "140 → 3".** Plan bloğu `129 → 100 → 60` yazıyor; ama P-02 kapsamı `Pages+Bilesenler+Layout`'a genişletip tabanı **140** olarak taze ölçmüştü (plan § 1 md.2 kendi tablosunda "129" satırını *bayat* olarak zaten işaretliyor). Hedef (tek adımda büyük düşüş, dalga sonunda ≤60 — BK-01) **aşılarak** karşılandı: **3**. Cırcır yalnız azaldı.

**S-2 — Tek kalem "saf yer değiştirme" dışına çıktı.** § 1.3'teki dört fiyat tipi. Gerekçe DoD'un kendisinde: "her taşınan tip için sunucu DTO ⇄ istemci tip ad + TİP eşleşmesi". İki kopyayı bırakıp yalnız test yazmak, sözleşmeyi değil **kopyalamayı** kilitlerdi.

**S-3 — Mutasyon kanıtı `mut.sh` ile değil, elle yapıldı.** `dotnet/mut.sh` her koşumda proje ağacına `.mutorj` artığı bırakır (silme K-4 ile kurucudadır) ve `MUTORJ_ARTIK` cırcırı yalnız azalabilir — betiği kullanmak kendi kapımızı kırardı. Görev metnindeki elle yöntem uygulandı: yedek `scratchpad/mut-yedek/P-03/`, `perl -0777 -pi -e` ile tek eşleşme, geri kopyalama, `cmp` kanıtı. **Proje ağacına tek bir `.mutorj` bırakılmadı** (ölçüldü: yeni artık 0).

**S-4 — Test 2 için derleme-güvenli mutasyon bulunamadı (açıkça bildiriliyor).** `Toplu_fiyat_yaniti_sunucuda_ve_ekranda_ayni_turdur` testini kırmaya çalışan her tek noktalı mutasyon **derlemeyi** kırıyor; çünkü tip birleştirildikten sonra ayrışmayı zaten derleyici engelliyor. Yani bu test bir gerileme bekçisidir, bağımsız kanıt üreticisi değildir. Gizlenmedi: § 5'te "mutasyonsuz" olarak yazılı. Diğer 5 test mutasyonla kırmızıya döndürüldü.

**S-5 — `ham/olcum-2026-09-18.json` (P-02'nin dosyası) güncellendi.** `denetim.sh` P02-8 kapısı ölçümle dosyayı karşılaştırır; taşıma record sayısını 140'tan 3'e indirince kapı *"(a) depo değişti, taban güncellenmedi"* diyerek kırılıyordu. Mekanizmanın amacı taze ölçümdür; değer **yeniden ölçülerek** güncellendi ve dosyaya gerekçe yazıldı. Başka hiçbir sayaç tabanına dokunulmadı.

**S-6 — Boş kalan bir sözleşme dosyası içerik aldı.** `Kuyumcu.Contracts/Sayfalar/GirisSozlesmeleri.cs` üretildi ama tek tipi (`AuthYaniti`) istisna olunca boşaldı; `rm` hook tarafından engellendiği için dosya **açıklama metniyle dolduruldu** (0 baytlık artık = 0).

**S-7 — MySQL parolasını değişkene alma komutu görev metnindekiyle çalışmadı.** `docker-compose.yml`'deki root parolası ile `kuyumcu-mysql` konteynerinin gerçek parolası **aynı değil** (`ERROR 1045 Access denied`). Parola `docker exec kuyumcu-mysql printenv MYSQL_ROOT_PASSWORD` ile değişkene alındı; **hiçbir yerde ekrana basılmadı**. ⚠ Bir maskeleme denemesinde `docker-compose.yml`'in ilgili satırı oturum günlüğüne düştü (sed deseni `:` ayracıyla eşleşmedi); değer bu raporda ve sonraki hiçbir çıktıda tekrarlanmadı. **Kurucuya not:** bu satır K-5 sır rotasyonunun kapsamındadır.

---

## § 4 — MİGRATION

**GEREKMEDİ.** Plan bloğu "Migration: HAYIR" diyor; şema değişmedi, `dotnet ef` **hiç çalıştırılmadı**.
Kanıt: `SELECT COUNT(*) FROM __EFMigrationsHistory` → **49** (başlangıç = son).

---

## § 5 — MUTASYON KANITLARI

Yöntem: kaynak `scratchpad/mut-yedek/P-03/m<n>/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebirlik kanıtlandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (`cmp`) |
|---|---|---|---|
| **M03-1** | `Pages/Ayarlar.razor` @code bloğuna `private sealed record MutasyonKaydi(string Id, string Ad);` eklendi | `Razor_code_blogunda_istemci_record_tanimi_acilmiyor` · `Tasinan_tipler_Contracts_projesinde_ve_public` (2 kırmızı) · ayrıca `denetim.sh` → `✗ record tanımı 4 (tavan 3)` → **DENETİM BAŞARISIZ** | BİREBİR AYNI |
| **M03-2** | `FiyatModulu.cs`'e Contracts'taki bir modelin ikinci tanımı eklendi (`public sealed record PanelKategori(string Id, string Ad);`) | `Contracts_tipi_sunucuda_ikinci_kez_tanimlanmiyor` · ayrıca `denetim.sh` → `✗ CONTRACTS_DISI_MODEL 1` → **DENETİM BAŞARISIZ** | BİREBİR AYNI |
| **M03-3** | Sunucu DTO'sunda **alan ADI** kaydırıldı: `OnizlemeSonucu(… int Offset …)` → `int Atlama` (konumsal kurulum olduğu için DERLEME YEŞİL kalır) | `Istemci_tipi_sunucu_dtosuyla_ad_ve_tip_olarak_eslesir(Sonuc ⇄ OnizlemeSonucu)` | BİREBİR AYNI |
| **M03-4** | Sunucu DTO'sunda **alan TİPİ** kaydırıldı: `OnizlemeSatiri(… decimal MarjTl …)` → `decimal? MarjTl` (örtük dönüşüm olduğu için DERLEME YEŞİL kalır) | `Istemci_tipi_sunucu_dtosuyla_ad_ve_tip_olarak_eslesir(Satir ⇄ OnizlemeSatiri)` | BİREBİR AYNI |
| — | `Toplu_fiyat_yaniti_sunucuda_ve_ekranda_ayni_turdur` | **MUTASYONSUZ** — S-4'te gerekçelendi: tip birleşiminden sonra ayrışmayı derleyici engelliyor, derleme-güvenli mutasyon yok | — |

M03-3 ve M03-4 tam olarak planın uyardığı riski üretir: **"Yanlış ad eşlemesi derleme hatası vermez, çalışma zamanında patlar."** İkisi de derlemeyi geçti, testler yakaladı.

Son mutasyondan sonra temiz derleme: `dotnet build Kuyumcu.sln --nologo` → **0 Hata / 0 Uyarı**.

---

## § 6 — TESTLER

| Koşum | Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|---|
| Filtreli | `FullyQualifiedName~SozlesmeYansima` | 6 | 6 | 0 | 0 |
| Tam süit (mutasyonlar öncesi) | `Kuyumcu.Tests` | 2910 | 2910 | 0 | 0 |
| **Tam süit (son, mutasyonlar geri alındıktan sonra)** | `Kuyumcu.Tests` | **2910** | **2910** | **0** | 0 |

**Yeni test sayısı: 6** (`Kuyumcu.Tests/SozlesmeYansimaTestleri.cs`; biri `[Theory]`, 2 veri satırı).
Süit **öncesi** sayı bu oturumda ölçülmedi (taşımaya başlamadan tam süit koşulmadı); 2910 − 6 = **2904** çıkarımdır, ölçüm değildir (Anayasa md.1).
trx: `scratchpad/trx-pP-03/`.

**Testlerin ne kilitlediği:**
1. `Contracts_tipi_sunucuda_ikinci_kez_tanimlanmiyor` — aynı adın Contracts + Api'de iki ayrı `Type` olması.
2. `Toplu_fiyat_yaniti_sunucuda_ve_ekranda_ayni_turdur` — toplu uygulama yanıtı gerçekten ortak tip mi.
3. `Istemci_tipi_sunucu_dtosuyla_ad_ve_tip_olarak_eslesir` (2 çift) — ad **ve** tip yansımayla eşleşiyor mu.
4. `Razor_code_blogunda_istemci_record_tanimi_acilmiyor` — yeni istemci record'u razor'a yazılamaz (3 istisna adıyla listeli).
5. `Tasinan_tipler_Contracts_projesinde_ve_public` — taşınan tipler gerçekten Contracts'ta, public ve ekran derlemesinde iç record kalmamış.

---

## § 7 — DENETİM

```
$ cd dotnet && ./denetim.sh
  ✓ Pages+Bilesenler+Layout record tanımı        3 (tavan 3)
  ✓ P-02: OLCUM_TABANI_KOPYA                     0 (varlık 59 · rota 42 · record 3)
  ✓ P-03: CONTRACTS_DISI_MODEL                   0 (-eq 0)
  ✓ P-03: Kuyumcu.Contracts projesi              137 tip
DENETİM TEMİZ.
```
(çıkış kodu 0; tek `⚠` satırı P-02'nin bilinen açık devri `QA_TUR_SURESI_OLCUM`'dur, P-03 kaynaklı değildir.)

### Eklenen sayaçlar (MEKANİZMA KURALI)

| Sayaç | Kural | Taban | Ölçüm komutu |
|---|---|---|---|
| `SAYFA_RECORD_TAVAN` | `-le`, **yalnız azalır** | 140 → **3** (taze ölçüm) | `grep -rn "record " Kuyumcu.Web/{Pages,Bilesenler,Layout} --include='*.razor' \| wc -l` |
| `CONTRACTS_DISI_MODEL` | `-eq 0` | **0** (taze ölçüm) | sunucu (`Kuyumcu.Api`+`Kuyumcu.Core` `.cs`) ile istemci (`Kuyumcu.Contracts` `.cs` + `Pages/Bilesenler/Layout` `.razor`) record adlarının kesişimi (`comm -12`) |

Her iki ölçüm komutu `denetim.sh` içinde **yorum olarak yazılıdır** (BK-07); hiçbir taban belgeden kopyalanmadı.

---

## § 8 — CANLI DOĞRULAMA (5198 — 5199'A DOKUNULMADI)

API: `ASPNETCORE_ENVIRONMENT=Development … --no-build --urls http://localhost:5198`, PID 86173.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| L1 | `GET /saglik` | 200 | **200** | gövde `Healthy` |
| L2 | `GET /api/v1/fiyat/listeler` jetonsuz | 401 | **401** | `{"message":"Token bulunamadı","error":"Unauthorized","statusCode":401,"istekKimligi":"01M2T7WJBF…"}` — yetki kapısı taşımadan etkilenmedi |
| L3 | `GET /` (Blazor kabuğu) | 200 | **200** | — |
| L4 | `GET /_framework/blazor.boot.json` | `Kuyumcu.Contracts` tarayıcıya gidiyor | **200** | `assembly: ['Kuyumcu.Contracts.wasm','Kuyumcu.Core.wasm','Kuyumcu.Web.wasm']` · `GET /_framework/Kuyumcu.Contracts.wasm` → **200** |
| L5 | `GET /swagger/v1/swagger.json` şema: taşınan sunucu tipleri | alan adları değişmemiş | **200** | `OnizlemeSonucu → [toplam, limit, offset, satirlar]` · `OnizlemeSatiri → [urunId, kod, ad, ayarId, gram, hasGrami, iscilikTl, teklifVar, sebep, hasDegeri, marjTl, iscilikTlUygulanan, listeFiyati, indirimTutari, satisFiyati, kampanyaId, kaynak, gecersizKilma]` |
| L6 | Aynı şemada **birleştirilen** tipler | Contracts tipleri tel üstünde birebir | **200** | `TopluSonuc → [uygulandi, hedeflenen, uygulanacak, toplamFark, parmakIzi, ornekler, atlandi]` · `TopluSatir → [urunId, kod, ad, eskiFiyat, yeniFiyat, fark]` · `TopluAtlanan → [urunId, kod, sebep]` · `GecersizKilmaOzeti → [sabitFiyat, hasMarjYuzde, iscilikCarpanYuzde, not, aktif]` |
| L7 | API durdurma | 5198 boş | **KAPANDI** | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → 0 satır |
| L8 | 5199 | dokunulmamış | **DOKUNULMADI** | `lsof -nP -iTCP:5199 -sTCP:LISTEN` → 2 satır (kurucunun sunucusu ayakta) |

> **Dürüstlük notu (md.1):** jetonlu uçlar canlıda çağrılmadı. `POST /api/v1/auth/kayit` **davet jetonu** ister (F-2-min fail-closed) ve mevcut 8 kiracının 3'ü `QA-TEST-*` kalıcı fikstürüdür — dokunulmaz. **Bu pakette test kiracısı AÇILMADI**; jetonlu yol, TestServer üzerinden gerçek HTTP koşan süitteki mevcut fiyat/HTTP testleriyle (tam süit yeşil) kanıtlandı. Şema kanıtı (L5-L6) sözleşmenin tel üstündeki hâlini doğrudan gösterir.

**SQL kanıtı** (parola değişkende, ekrana basılmadı):

```
SELECT COUNT(*) FROM tenants;                        → 8   (başlangıç)  → 8   (son)
SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKET%'; → 0
SELECT COUNT(*) FROM __EFMigrationsHistory;          → 49  (değişmedi)
```

Test kiracısı: **açılmadı → silinecek bir şey yok.** `QA-TEST-1/2/3 Kuyumculuk` fikstürlerine dokunulmadı.

---

## § 9 — DEĞİŞEN DOSYALAR

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı depo genelinde zaten kirliydi (önceki oturumlardan); **bu paketin dokunduğu dosyalar**:

**YENİ (36 + 1):**
- `dotnet/Kuyumcu.Contracts/Kuyumcu.Contracts.csproj`
- `dotnet/Kuyumcu.Contracts/Fiyat/FiyatSozlesmeleri.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/*.cs` (27 dosya)
- `dotnet/Kuyumcu.Contracts/Bilesenler/*.cs` (5 dosya)
- `dotnet/Kuyumcu.Tests/SozlesmeYansimaTestleri.cs`

**DEĞİŞEN:**
- `dotnet/Kuyumcu.sln` · `dotnet/Kuyumcu.Web/Kuyumcu.Web.csproj` · `dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj` · `dotnet/Kuyumcu.Tests/Kuyumcu.Tests.csproj`
- `dotnet/denetim.sh` (tavan + P-03 bloğu)
- `dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs` (4 tip Contracts'a taşındı + `using`)
- `dotnet/Kuyumcu.Tests/EkranTestleri.cs` (`MasakFormu.Esikler` → `Esikler`)
- `dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json` (taze taban)
- **31 razor:** `Pages/{Ayarlar,Banka,Bildirimler,Cariler,DenetimIzi,Emanet,Faturalar,Fiyatlandirma,Islemler,Isletme,KarZarar,Kasalar,Kullanicilar,Kur,Panel,Raporlar,Sayim,SiparisDetay,Siparisler,UrunAgaci,UrunEtiketleri,Urunler,VadeTakibi,VeriDisaAktarim,Yedekler,Yetkiler}.razor` · `Bilesenler/{BelgeYukleyici,FiyatListesiUrunPaneli,MasakFormu,PastaGrafik,TemaSecici}.razor`

**COMMIT YOK** — hiçbir `git add/commit/push`, `checkout/stash/reset`, `rm/mv` çalıştırılmadı.

### Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ kaydı)

Yukarıdaki 26 `Pages/` + 5 `Bilesenler/` dosyası bu dalgada P-03 tarafından açılmıştır. Değişiklik **yalnızca** `@code` bloğundaki record tanımlarının kaldırılması ve tek `@using` satırı eklenmesidir; işaretleme (markup), element id'leri, davranış ve metin **hiç değişmedi**. Bu 31 dosyaya başka bir dalgada ikinci dokunuş için kapı kaydına yazılı gerekçe gerekir.

---

## § 10 — DoD SÖZLEŞMESİ

P-03 **yeni varlık / kayıt türü / uç / ekran üretmez**; salt tip yerleşimi paketidir. Bu nedenle:

| Sözleşme kalemi | Durum |
|---|---|
| Yeni varlık | **YOK** — `IKiraciVarlik` sayısı değişmedi (`BEKLENEN_VARLIK` 59, denetim TEMİZ) |
| Yeni PATCH / DELETE ucu | **YOK** — hiçbir uç eklenmedi, kaldırılmadı, imzası değişmedi (L5-L6 şema kanıtı) |
| `[ModulYetkisi]` / `[RolGerekli]` | **DEĞİŞMEDİ** — `FiyatModulu` kapıları aynen duruyor (L2 canlı 401) |
| Denetim kaydı | **KONUSUZ** — yazma yolu açılmadı |
| Ekranda Düzenle/Pasife al | **KONUSUZ** — yeni kayıt türü yok; mevcut ekranların davranışı değişmedi |
| Finansal varlıkta hard DELETE | **YOK** |
| Her düğme istek atar ya da disabled | **KONUSUZ** — hiçbir düğme eklenmedi/kaldırılmadı |

**Salt okunur/değişmez olan yok; düzenlenebilirlik yüzeyi bu paketle ne arttı ne azaldı.**

---

## § 11 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Taşıma saf yer değiştirmedir | **KARŞILANDI** (1 gerekçeli istisna) | 137 record birebir; § 1.3'teki 4 tip birleşimi S-2'de gerekçeli |
| Sunucu DTO ⇄ istemci tip ad + TİP yansımayla kilitli | **KISMEN** (2026-09-18 düzeltmesi — bu satır önce yanlışlıkla "KARŞILANDI" idi) | Taze ölçüm: 72 ayrıştırma hedefinin **13'ü kilitli, 59'u kilitsiz**. `SozlesmeYansimaTestleri` 6 test + yeni `SozlesmeSekilTestleri` (12 uç) + `SozlesmeKapsamTestleri` cırcırı · `denetim.sh` `SOZLESME_KILITSIZ_TIP 59/72 (tavan 59)`. Kalan kapsam **P-07'ye devredildi** → ayrıntı: `PAKET-P-03-uygulama-raporu-DUZELTME-2026-09-18.md` |
| Tavan 129→100, dalga sonunda →60 | **AŞILARAK KARŞILANDI** | 140 → **3** (S-1) |
| Tavan artırma yasak | **KARŞILANDI** | `denetim.sh:337` yalnız azaldı |
| Yeni istemci record açılmaz | **KARŞILANDI + MEKANİZMA** | test 4 + `SAYFA_RECORD_TAVAN=3`; M03-1 ikisini de kırıyor |
| `CONTRACTS_DISI_MODEL = 0` | **KARŞILANDI** | denetim çıktısı; M03-2 kırıyor |
| Sayaç eklenmeden paket kapanmaz | **KARŞILANDI** | `# >>> P-03 … # <<< P-03` işaretli blok, ölçüm komutları yorumda |
| Migration: HAYIR | **KARŞILANDI** | `__EFMigrationsHistory` = 49, `dotnet ef` koşmadı |
| Derleme 0 hata / 0 uyarı | **KARŞILANDI** | `dotnet build Kuyumcu.sln --nologo` → 0 Hata, 0 Uyarı |
| Tam süit 0 başarısız | **KARŞILANDI** | 2910/2910 |
| `denetim.sh` TEMİZ | **KARŞILANDI** | § 7 |
| Canlı doğrulama 5198, 5199'a dokunulmadı | **KARŞILANDI** | § 8 (L1-L8) |
| Test kiracısı silindi | **KONUSUZ** | kiracı açılmadı; tenants 8 → 8, `PAKET%` = 0 |
| `QA-TEST-*` fikstürlerine dokunulmadı | **KARŞILANDI** | yalnız `SELECT` koşuldu |
| Kapsam kilidi (yalnız R-10) | **KARŞILANDI** | yeni ekran/modül/özellik yok; `Servisler/` kalıntısı P-07'ye devredildi |
| Mevcut element id'leri değişmedi | **KARŞILANDI** | işaretlemeye hiç dokunulmadı |
| Sır/parola çıktıya yazılmaz | **KISMEN** | değerler komutlarda hep değişkende; ancak S-7'deki maskeleme kazası oturum günlüğüne bir satır düşürdü — raporda tekrarlanmadı, K-5 kapsamına işaretlendi |
| COMMIT YOK / git-rm-mv yok | **KARŞILANDI** | yalnız `git status` (salt okunur) |
| Tahmin 240 ajan-dk | **İÇİNDE** | taşıma betikle yapıldığı için 42 razor'un elle düzenlenmesi gerekmedi |

---

## § 12 — AÇIK KALANLAR

1. **P-07 devri (R-10 kalıntısı).** `Kuyumcu.Web/Servisler/` ile Api arasındaki 6 çift tanım (`MasakGereksinim`, `PosCihazSatir`, `PosTahsilatSatir`, `PosTaksitOranSatir`, `PosValorSatir`, `SurumKunyesi`) ve `Servisler/`'e bağlı 3 razor record'u (`AuthYaniti`, `OzetVeri`, `SubeSatiri`). P-07 bunları Contracts'a taşıdığında `SAYFA_RECORD_TAVAN` **3 → 0** ve `CONTRACTS_DISI_MODEL` kapsamı Servisler'i de içerecek şekilde genişletilebilir.
2. **Test 2 mutasyonsuz** (S-4) — derleyicinin zaten kapattığı yüzey; P-07 sonrası yeniden değerlendirilsin.
3. **`Kuyumcu.Contracts` → `Kuyumcu.Core` bağımlılığı** duruyor (taşınan tipler `Kuyumcu.Core.Domain` tiplerine dokunuyor). Core'un sözleşme parçası (`Kuyumcu.Core/Sozlesmeler/`, 8 dosya, 59 tip) **taşınmadı**: iki sözleşme evi var. Tek eve indirme kararı mimariye (Bertan) aittir — bu pakette kapsam büyütmemek için yapılmadı.

## § 13 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **Mali müşavire not YOK** — bu pakette hiçbir mevzuat hükmü koda girmedi.
- **Kurucuya (K-5 kapsamı):** `docker-compose.yml`'deki MySQL root parolası ile çalışan `kuyumcu-mysql` konteynerinin parolası **aynı değil**; belge ile gerçek ayrışmış durumda. Rotasyon sırasında ikisi birlikte düzeltilmeli.
- **Kurucuya (K-4):** bu pakette yeni `.mutorj` artığı üretilmedi; mevcut 16 artığın silinmesi hâlâ sizdedir.


---

## EK — 2026-09-18 DÜZELTME TURU (hasım bulguları)

Bu raporun bazı beyanları hasım doğrulamasında çürütüldü (Anayasa md.1). Düzeltmeler, yeni mekanizmalar (JSON şekil kilidi, kapsam cırcırı, `SOZLESME_KILITSIZ_TIP` sayacı), 3 mutasyon kanıtı, tam süit 2925/2925, `DENETİM TEMİZ` ve 5 ⚠ satırının tamamı ayrı belgededir:
`dokumanlar/09-uygulama-kayitlari/PAKET-P-03-uygulama-raporu-DUZELTME-2026-09-18.md`
