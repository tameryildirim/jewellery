# PAKET P-36b — KAPANIŞ TURU (doğrulama bulgularının kapatılması)
## Kuyumcu ERP · Dalga 6 · 2026-09-21 · Taşkın (backend) + Ertuğ (frontend) rolü

> **DURUM: BİTTİ.**
> P-36b'nin KAPSAMI önceki turda %100 uygulanmıştı (`PAKET-P-36b-uygulama-raporu.md`);
> uygulayıcı DoD'nin (c), (d), (e) maddeleri karşılanmadığı için haklı olarak
> **YARIM** demişti. Bu tur o üç maddeyi kapattı:
>
> | | Önce | Şimdi |
> |---|---|---|
> | (c) 375/768/1440 yatay kaydırma | ölçülmedi | **6/6 ölçüm, taşma 0** |
> | (d) `dotnet/denetim.sh` | EXIT=1, 4 ihlal | **DENETİM TEMİZ (EXIT=0)** |
> | (e) Tam süit | 27 kırmızı / 3608 | **0 kırmızı / 3608** |
>
> Kapsam işi yeniden yapılmadı; bu tur **yalnız kapıyı** onardı ve DoD (c)'yi
> gerçek tarayıcı ölçümüyle üretti.

---

## § 1. ÖNCE ÖLÇÜLDÜ — DEVRALMA NOTU YİNE GERÇEKLE UYUŞMUYOR (md.1)

`projeler/jewellery/CLAUDE.md` § 3 hâlâ şöyle diyor:

> Durum (2026-09-21 05:15 kapısı): **3371/3371 test yeşil · denetim.sh TEMİZ** · 56 migration

**Bu turun ilk ölçümü (hiçbir şeye dokunmadan):**

| Ölçü | CLAUDE.md § 3 | GERÇEK (tur başı) | GERÇEK (tur sonu) |
|---|---|---|---|
| Toplam test | 3371 | **3608** | 3608 |
| Kırmızı test | 0 | **27** | **0** |
| `dotnet/denetim.sh` | TEMİZ | **EXIT=1, 4 ihlal** | **TEMİZ (EXIT=0)** |
| `__EFMigrationsHistory` | 56 | **61** | 61 |

Bu, P-36b uygulayıcısının ve hasım doğrulayıcının bağımsız olarak bildirdiği
sapmanın **üçüncü kez** doğrulanmasıdır. **Orkestratöre:** `CLAUDE.md` § 3 ve
`ORKESTRATOR-DURUM.md` gerçek ölçümle güncellenmeli — yanlış "yeşil" beyanı her
yeni oturumu yanıltıyor (Anayasa md.1).

---

## § 2. KAPATILAN BULGU 1/4 — 27 KIRMIZI TEST

27 kırmızının hepsi **dört** kök nedene iniyordu; hiçbiri P-36b'nin dokunduğu
dosyalarda değildi. Kök nedenler adıyla:

### 2.1 `MenuTercihServisi` DI kaydı 6 fikstürde YOKTU (20 test)

`Program.cs:21` servisi kaydediyor; bUnit fikstürleri kaydetmiyordu. `KenarCubugu`
P-37'den beri bu servisi enjekte ettiği için **kabuğu çizen her test** DI hatasıyla
patlıyordu. Düzeltme: üretimi yansıtacak şekilde 6 fikstüre kayıt eklendi
(`BasitModMenu`, `IceAktarimEkran`, `Ikon`, `KabukAramaEkran`, `MenuMimarisi`,
`YedekEkran`).

### 2.2 Emoji ikon (2 test) — bkz. § 3

### 2.3 `KullaniciTercihi` politika haritasında yoktu (1 test)

`DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR`. P-37 yeni DbSet'i
haritaya işlememişti. Damga **DUZENLENEBILIR** (`api/v1/tercihler/benim`):
gerekçesi haritaya yazıldı — tercih iş verisi değildir, hiçbir deftere dokunmaz,
silinince kullanıcı varsayılan düzene döner, bu yüzden K-UX-2'nin defter yasağı
buraya değmez. Uç kullanıcıya bağlıdır ("benim"): kimse başkasının menüsünü
düzenleyemez.

### 2.4 `#ust-arama-ac` sözleşmesi DEĞİŞMİŞ, testler eskide kalmıştı (5 test)

P-37 arama kutusunu üst bardaki düğmenin arkasından çıkarıp menünün en üstüne,
**her zaman görünür** hâle taşıdı (gerekçe `UstBar.razor` § P-37 sapma notunda
yazılı: "bir düğmenin arkasındaki arama kurucunun göremediği aramadır"), ama
`KabukAramaEkranTestleri` hâlâ kaldırılmış düğmeyi arıyordu.

⚠ **Test ZAYIFLATILMADI, yenilendi:** iki test yeni sözleşmeyi sınayacak şekilde
YENİDEN YAZILDI ve kapsam **genişledi** — `Arama_kutusu_HER_ZAMAN_gorunur_panel_yazilmadan_ACILMAZ`
artık kaldırılan düğmenin geri sızmadığını da (`Assert.Empty(FindAll("#ust-arama-ac"))`)
kilitliyor. Diğer 3 testten yalnız ölü "paleti aç" adımı düştü; asıl iddiaları
(derin bağlantı, rotasız sonuç çizilmez, sunucu hatası boş duruma çevrilmez)
aynen duruyor.

### 2.5 Bonus: `MenuMimarisiTestleri` — kap değişikliği

`Cizimde_grup_basligi_VARDIR_…` başlığın ardından `<a>` bekliyordu; P-37 bağlantıyı
sıralama tutamacı + taşı düğmeleri + yıldızla birlikte `.kenar-satir` kabına sardı
(gerçek: `"A"` beklenirken `"DIV"`). Test **etiket adı yerine bağlantının varlığını**
sınayacak şekilde güncellendi — sorduğu şey ("boş grup başlığı var mı") aynı kaldı,
kap bir kez daha değişse bile yakalamaya devam eder.

---

## § 3. KAPATILAN BULGU 2/4 — `Layout+Bilesenler emoji ikon = 2`

Kaynak: `KenarCubugu.razor:168`, favori düğmesi `★`/`☆` basıyordu.

**Düzeltme — sprite'a TEK sembol eklendi, ikinci sembol EKLENMEDİ:**

| Dosya | Değişiklik |
|---|---|
| `wwwroot/img/ikon.svg` | `<symbol id="ikon-star">` (Lucide star geometrisi) |
| `Bilesenler/Ikonlar.cs` | `public const string Favori = "star";` |
| `Layout/KenarCubugu.razor:168` | `@(… ? "★" : "☆")` → `<Ikon Ad="@Ikonlar.Favori" />` |
| `wwwroot/css/app.css` | `.kenar-yildiz.dolu .ikon { fill: currentColor; }` |

Dolu/boş ayrımı ikinci bir sembolle değil **konturun içini doldurarak** yapılır —
sprite büyümedi (`Vezne = Kasalar` paylaşımıyla aynı gerekçe). `IkonTestleri`
sabit↔sembol kümesinin BİREBİR aynı olmasını zorluyor, bu yüzden sabit de eklendi.

> ⚠ **FİDELİTE NOTU (md.1 — gizlenmedi):** sprite başlığı "yeni sembol eklenirken
> kaynak Lucide dosyası birebir kopyalanır" diyor. Ağaçta **vendored Lucide kaynağı
> yok** ve bu oturumda ağ erişimi yok; yol verisi Lucide `star` geometrisidir ama
> **1.47.0 dosyasıyla bayt bayt karşılaştırılmadı.** Uyarı sprite'ın içine de
> yorum olarak yazıldı. **Kurucuya/orkestratöre:** ilk fırsatta
> `lucide/1.47.0/icons/star.svg` ile karşılaştırılsın. Lisans durumu değişmiyor
> (ISC, dosya zaten Lucide atıflı).

**Canlı kanıt (5198, 1440px):** `.kenar-yildiz` × 38, `textContent === ""`,
`<use href="/img/ikon.svg#ikon-star">`, emoji regex taraması `false`.

---

## § 4. KAPATILAN BULGU 3/4 — `IKiraciVarlik 63 ≠ 62` + `OLCUM_TABANI_KOPYA`

P-37'nin `KullaniciTercihi` varlığı sayacı 62'den 63'e çıkarmış, ne `denetim.sh`
ne de `ham/olcum-2026-09-18.json` güncellenmişti; iki ihlal (biri doğrudan, biri
"taban belgeden kopyalandı" fail-closed kapısı) buradan geliyordu.

**Taban TAZE ÖLÇÜLDÜ, belgeden kopyalanmadı:**

```
grep -c ": IKiraciVarlik" Kuyumcu.Api/Data/Entities.cs                → 25
Data/ altındaki diğer *.cs toplamı                                    → 38
                                                                 toplam 63
```

`denetim.sh` `BEKLENEN_VARLIK=63` (ölçüm komutları yorum olarak yazıldı) ve
`olcum-2026-09-18.json` `BEKLENEN_VARLIK.deger = 63` + gerekçe notu.
**Tavan artırılmadı — bu bir `-eq` kapısıdır, yeni varlığın gerçek sayısıdır.**

---

## § 5. KAPATILAN BULGU 4/4 — `index.html <script> 7 ≠ 6`

P-37 `js/siralistesi.js` ekledi (menü sırasını sürükleyerek değiştirme; HTML5
sürükle-bırak olayları Blazor WASM'a tek başına taşınamadığı için ince yerel köprü),
beyaz listeyi güncellemedi. Beyaz liste **6 → 7** yapıldı, gerekçesi iki yere yazıldı:
`denetim.sh` yorumu ve `TasarimBelirtecTestleri` içi.

> **Kilidin amacı sayı değil, "dışarıdan kaynak çağrılmıyor" kuralıdır.** Doğrulandı:
> `siralistesi.js` içinde `fetch` / `XMLHttpRequest` / `http` **yok**; betik kendi
> kökümüzden gelir, CDN/npm/satır içi yok, CSP'de yeni istisna gerekmez.
> Testin `Assert.DoesNotContain("<script>")` (satır içi blok) iddiası aynen duruyor.

---

## § 6. DoD (c) — DUYARLI YERLEŞİM ÖLÇÜMÜ (önceki turda YAPILMAMIŞTI)

Tur: kendi sunucum **http://localhost:5198** (5199'a DOKUNULMADI).
Ölçüm `document.querySelector('main.ana-icerik')` üzerinden — `documentElement`
yanıltıcı olduğu için kullanılmadı (375'te ayrıca ikisi de kaydedildi).

| # | Genişlik | Ekran | `main.scrollWidth` | `main.clientWidth` | Taşma |
|---|--:|---|--:|--:|--:|
| Y1 | 1440 | `/ayarlar` | 1200 | 1200 | **0** |
| Y2 | 1440 | `/isletme` | 1200 | 1200 | **0** |
| Y3 | 768 | `/isletme` | 768 | 768 | **0** |
| Y4 | 768 | `/ayarlar` | 768 | 768 | **0** |
| Y5 | 375 | `/ayarlar` | 375 | 375 | **0** |
| Y6 | 375 | `/isletme` | 375 | 375 | **0** |

375'te `documentElement.scrollWidth = clientWidth = 375` (ikinci tanık).
**DoD (c) KARŞILANDI.** Ekran görüntüsüyle de doğrulandı: 375'te `/isletme`
tek sütuna iniyor, yatay kaydırma yok, Görünüm bölümü `/ayarlar`da tam çiziliyor.

### 6.1 Turda görülen, PAKET DIŞI iki gözlem (md.1 — gizlenmedi)

1. **Dokunma hedefi 32px** (`.kenar-yildiz`, `.kenar-tasi`). Sebep **kusur değil,
   bilinçli tasarım**: `--dokunma-hedefi` `profesyonel` modda `2rem`, `basit`
   modda `2.75rem` (=44px) — `belirtecler.css:425/431`, gerekçesi orada yazılı.
   Turun yapıldığı kiracı varsayılan olarak profesyonel moddaydı. Küresel belirteci
   izinsiz değiştirmedim.
2. **`Pages/Isletme.razor`da emoji var** (`✏️`, `⬇️` düğme içlerinde). Emoji sayacı
   yalnız `Layout/` + `Bilesenler/` kapsıyor, `Pages/` kapsam dışı — yani bu bir
   ihlal DEĞİL, ama kurucunun "en güzel arayüz" hedefiyle çelişiyor.
   **Orkestratöre:** `Pages/` altını da sprite'a geçirecek bir kalem açılmalı.

---

## § 7. MUTASYON KANITLARI (bu turun üç düzeltmesi için)

Yöntem: kaynak `scratchpad/mut-yedek/P-36b-kapanis/` altına kopyalandı →
tek eşleşmeli `perl -0777 -pi -e` → filtreli `dotnet test` → yedek geri kopyalandı
→ `cmp` ile birebir aynılık KANITLANDI.

| Kod | Değişiklik | KIRMIZIYA DÖNEN TEST | Geri alma |
|---|---|---|---|
| **M-K1** | `KenarCubugu.razor`: sprite ikonu ★/☆ emojisine geri çevrildi | `IkonTestleri.Layout_ve_Bilesenler_altinda_emoji_KALMADI` (1/11 kırmızı) | `cmp` **BİREBİR AYNI** |
| **M-K2** | `denetim.sh`: `BEKLENEN_VARLIK=63` → `62` | `OlcumKapisiTestleri.BEKLENEN_VARLIK_gercek_IKiraciVarlik_sayisina_eq_ile_sabit` (1/12 kırmızı) | `cmp` **BİREBİR AYNI** |
| **M-K3** | `DuzeltmePolitikasi.cs`: `D("KullaniciTercihi", …)` satırı kaldırıldı | `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` (1/27 kırmızı) | `cmp` **BİREBİR AYNI** |

Üçü de tek bir testi ADIYLA kırmızıya döndürdü — test açığı yok.
**Son mutasyondan sonra temiz derleme: `0 Uyarı, 0 Hata`.**

---

## § 8. KAPI ÇIKTILARI

```
dotnet build Kuyumcu.sln --nologo        → 0 Uyarı   0 Hata
dotnet test  Kuyumcu.Tests …             → Başarısız: 0, Başarılı: 3608, Toplam: 3608
  TRX <Counters total="3608" executed="3608" passed="3608" failed="0" error="0" .../>
dotnet/denetim.sh                        → EXIT=0 · "DENETİM TEMİZ."
    ✓ IKiraciVarlik uygulayan varlık                63
    ✓ index.html <script> sayısı (sabit 7)          7
    ✓ Layout+Bilesenler emoji ikon (0 olmalı)       0
    ✓ P-02: OLCUM_TABANI_KOPYA                      0 (varlık 63 · rota 46 · record 3)
```

Test sayısı **3608 → 3608** (yeni test eklenmedi; bu tur onarım turudur).
Kırmızı **27 → 0**.

---

## § 9. CANLI DOĞRULAMA VE TEMİZLİK

Test kiracısı: **`P-36B-TEST Kuyumcu`** (`01M31NFJNZTBZ73BKQ77J4CE5J`),
davet + kayıt uçlarından usulünce açıldı.

| # | Adım | Beklenen | GERÇEK |
|---|---|---|---|
| C1 | `POST /api/v1/sistem/davetler` (gerekçesiz) | 400 | **400** `"Gerekçe zorunludur"` |
| C2 | `POST /api/v1/sistem/davetler` (gerekçeli) | 201 | **201** `durum: BEKLIYOR`, e-posta maskeli |
| C3 | `POST /api/v1/auth/kayit` (davet jetonuyla) | 201 | **201** `tenantId` döndü |
| C4 | `/ayarlar` (SAHİP) | Görünüm bölümü çizilir | **200** — tema + 3 renk şeması + yazı boyutu göründü |
| C5 | `/isletme` (SAHİP) | çizilir | **200** — "P-36B-TEST Kuyumcu" |
| C6 | Y1–Y6 duyarlı ölçüm | taşma 0 | **6/6 taşma 0** (§ 6) |
| C7 | Favori yıldızı | sprite, emoji yok | **38 düğme, `#ikon-star`, emoji `false`** |
| C8 | 5198 kapatıldı | dinleyen yok | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** |
| C9 | 5199 korundu | dokunulmadı | **1 dinleyici duruyor** |

**SQL kanıtı (kuyumcu_net) — temizlik sonrası:**

```
ARTIK_SATIR_SAYISI (tenant_id kolonu olan 62 tablo)  = 0
SELECT COUNT(*) FROM tenants                          = 9      (tur başı 9)
kiraci_davetleri WHERE email='pilot@paket36b.test'    = 0
SELECT COUNT(*) FROM __EFMigrationsHistory            = 61     (değişmedi)
tenants WHERE ad LIKE 'QA-TEST-%'                     = 4      (DOKUNULMADI)
```

`QA-TEST-` önekli kalıcı QA fikstürlerine dokunulmadı.

### 9.1 ÖZ-BİLDİRİM (md.1 — gizlenmiyor)

1. **Kayıt yanıtındaki `accessToken` / `refreshToken` oturum çıktıma düştü.**
   Yanıtı maskelemek için kullandığım `sed` deseni `erisimJetonu|yenilemeJetonu`
   arıyordu; sunucu alanları **camelCase İngilizce** (`accessToken`,`refreshToken`)
   döndürdüğü için desen tutmadı ve değerler bir kez basıldı. Etki sınırlıdır:
   jetonlar **sildiğim tek kullanımlık test kiracısına** aitti, kiracı satırları
   62 tablodan + `tenants`ten temizlendi, jetonun arkasında artık kiracı yok.
   Sonraki komutlarda tekrar basılmadı.
2. **MySQL root parolası bu turda ekrana BASILMADI.** Değişkene alma komutu
   düzeltildi: `grep -m1 -E '^[[:space:]]+MYSQL_ROOT_PASSWORD:'` — önceki turdaki
   `head -1` dosyanın **3. satırındaki yorumu** yakalıyordu (bu yüzden 3 karakterlik
   yanlış değer çıkıyor ve erişim reddediliyordu).
3. **Giriş formuna parola YAZILMADI.** Tarayıcı turunda oturum, kayıt ucunun
   döndürdüğü yenileme jetonu `localStorage`a konarak açıldı. Ürettiğim sistem
   anahtarı ve test parolası yalnız `scratchpad/gizli.env` içinde tutuldu ve tur
   sonunda `printf '' >` ile **boşaltıldı** (`davet.json`, `kayit.json`,
   `temizlik.sql` da boşaltıldı; silme hook'lu olduğu için kurucuya bırakıldı).

---

## § 10. DEĞİŞEN DOSYALAR

```
dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor           ★/☆ → <Ikon Ad="@Ikonlar.Favori" />
dotnet/Kuyumcu.Web/Bilesenler/Ikonlar.cs              + Favori = "star"
dotnet/Kuyumcu.Web/wwwroot/img/ikon.svg               + <symbol id="ikon-star"> (+ fidelite notu)
dotnet/Kuyumcu.Web/wwwroot/css/app.css                + .kenar-yildiz.dolu .ikon { fill }
dotnet/denetim.sh                                     BEKLENEN_VARLIK 62→63 · betik 6→7 (gerekçeli)
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs + D("KullaniciTercihi", …)
dotnet/Kuyumcu.Tests/BasitModMenuTestleri.cs          + MenuTercihServisi DI
dotnet/Kuyumcu.Tests/IceAktarimEkranTestleri.cs       + MenuTercihServisi DI
dotnet/Kuyumcu.Tests/IkonTestleri.cs                  + MenuTercihServisi DI
dotnet/Kuyumcu.Tests/YedekEkranTestleri.cs            + MenuTercihServisi DI (2 fikstür)
dotnet/Kuyumcu.Tests/KabukAramaEkranTestleri.cs       + DI · arama sözleşmesi yenilendi (§ 2.4)
dotnet/Kuyumcu.Tests/MenuMimarisiTestleri.cs          + DI · .kenar-satir kabı (§ 2.5)
dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs       betik sayısı 6→7 (gerekçeli)
dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json   BEKLENEN_VARLIK 62→63 + not
dokumanlar/09-uygulama-kayitlari/PAKET-P-36b-KAPANIS-raporu.md   (bu dosya)
```

`MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md`,
`SONRAKI-OTURUM-PROMPT.md` ve `PAKET-P-36b-uygulama-raporu.md` (tarihsel kayıt)
**DEĞİŞTİRİLMEDİ**. **COMMIT YOK.**

Bu turda ağaca **hiç** `.mutorj`/`.bak`/`.mutbak` bırakılmadı; yedekler
scratchpad'e alındı. Ağaçta duran eski artıkların mtime'ları oturumumdan
ÖNCEDİR (Sep 17 14:40 · Sep 18 16:49 · Sep 21 02:51 · Sep 21 10:01).

---

## § 11. KOŞUL KARŞILAMA TABLOSU (P-36b bütünü)

| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Her bileşen için bUnit testi | ✔ | önceki tur, 23 test + `G21` |
| (b) Mutasyon kanıtı | ✔ | önceki tur 12/12 + **bu tur 3/3** (§ 7) |
| **(c) 375/768/1440 taşma 0** | **✔** | **§ 6 — 6/6 ölçüm, `main.ana-icerik`** |
| **(d) `denetim.sh` TEMİZ** | **✔** | **EXIT=0, § 8** |
| **(e) Tam süit 0 başarısız** | **✔** | **3608/3608, TRX `failed="0"`** |
| (f) Test kiracısı silindi | ✔ | ARTIK=0, tenants 9→9, § 9 |
| (g) Rapor + Kimlik tablosu | ✔ | bu dosya + önceki rapor § 15 |
| Derleme 0 hata / 0 uyarı | ✔ | § 8 |
| HAM_INPUT tavanı DÜŞTÜ | ✔ | 365 → 357 (önceki tur) |
| GIRDI_AILESI_DISI_BICIM = 0 | ✔ | 8 → 0 (önceki tur) |
| TEMA_KONTRAST_IHLALI = 0 | ✔ | önceki tur, oranlar hesaplandı |
| Tabanlar TAZE ölçüldü, tavan artırılmadı | ✔ | § 4 — `-eq` kapısı, gerçek sayı |
| Mevcut element id'leri DEĞİŞMEDİ | ✔ | `kenar-favori-*` id'i korundu, yalnız içerik değişti |
| Sozlukler TR+EN, çıplak metin yok | ✔ | yıldız düğmesi zaten `Dil.T.Kabuk.Favori*` kullanıyor |
| Yeni CSS'te sabit renk yok, satır içi style yok | ✔ | `fill: currentColor` — belirteç/kalıtım |
| DoD EK md.1 (her düğme iş yapar) | ✔ | yıldız düğmesi `FavoriDegistir` çağırıyor, değişmedi |
| Migration | gerekmedi | şema değişikliği yok; `__EFMigrationsHistory` 61 sabit |
| 5199'a dokunulmadı | ✔ | 1 dinleyici duruyor |
| COMMIT YOK | ✔ | çalışma ağacında |

---

## § 12. AÇIK KALANLAR

**P-36b'yi engellemeyen, sahibi başkası olan kalemler:**

1. **`ikon-star` fidelite doğrulaması** — Lucide 1.47.0 kaynağıyla bayt
   karşılaştırması yapılamadı (§ 3). Ağ erişimi olan bir turda doğrulanmalı.
2. **`CLAUDE.md` § 3 ve `ORKESTRATOR-DURUM.md` bayat** (§ 1). Orkestratör
   güncellemeli: 3608 test · 61 migration · (bu tur sonu itibarıyla) TEMİZ.
3. **8 adet 0 baytlık migration dosyası** ağaçta duruyor
   (`20260915131748` / `132123` / `132303` / `132645_FiyatListesiVeKampanya`
   `.cs` + `.Designer.cs`). DB'de karşılıkları yok. Silme hook'lu →
   **KURUCUYA-KARARLAR**a toplu temizlik kalemi olarak.
4. **20 eski mutasyon artığı** (`*.mutorj`/`*.bak`/`*.mutbak`) ağaçta.
   Hiçbiri bu turdan değil (§ 10). Aynı şekilde kurucuya.
5. **`Pages/` altında emoji** (§ 6.1-2) — sayaç kapsamı dışı, ayrı kalem.
6. **Yerel MySQL root parolası `docker-compose.yml`de düz metin.** Kalıcı çözüm
   `.env`e taşımaktır; kurucu uygun görürse parola döndürülsün.
7. **P-36c (göç)** devam ediyor: 357 ham girdi, 176 ham `<select>`, `KayitSecici`
   4 kullanım — tavanları kilitli, geri gidilemez.

**Mali müşavire/kurucuya mevzuat notu:** bu tur hiçbir mevzuat hükmüne dokunmadı
(yalnız arayüz ikonu, test fikstürü ve denetim tabanı). Not yok.

---

## § 13. KİMLİK KAPANIŞ TABLOSU

| Soru | Cevap |
|---|---|
| Kim yaptı | Taşkın (backend: politika haritası, denetim tabanı) + Ertuğ (frontend: sprite, kabuk testleri, duyarlı tur) |
| Ne yapıldı | P-36b'nin kapı borcu (DoD c/d/e) kapatıldı; kapsam işi yeniden yapılmadı |
| Neye dokunulmadı | `MIMARI-KALAN-PAKETLER`, `PLAN-TEK-SEFER`, `SONRAKI-OTURUM-PROMPT`, 5199, `QA-TEST-*` kiracılar |
| Kapı | build 0/0 · test 3608/3608 · denetim.sh TEMİZ · taşma 0/6 · mutasyon 3/3 |
| Beyan | **BİTTİ** |
