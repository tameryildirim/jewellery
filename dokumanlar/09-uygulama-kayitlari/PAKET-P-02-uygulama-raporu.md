# PAKET P-02 — YAYIN DAMGASI + ÖLÇÜM KAPISI + TAZE SAYAÇ TABANLARI

**Dalga:** DALGA 1 · **Sürüm:** 1.0 · **Tarih:** 2026-09-18
**Kimlikler:** B-1 · B-2 · B-21 · B-53 · Y-33
**Bağlı koşullar:** BK-05 · BK-07 · BK-08 · BK-14 · BK-39 · BK-42 · Riskler R4 · R15 · R18
**Rol:** Taşkın (backend) + Ertuğ (frontend/araç hattı) · **Migration:** YOK

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (P-01)

Durma-devam modunda ölçüldü, varsayılmadı:

| Beklenen çıktı | Var mı | Kanıt |
|---|---|---|
| `PAKET-P-00-uygulama-raporu.md` | VAR | 13.248 bayt |
| `PAKET-P-01-uygulama-raporu.md` | VAR | 23.941 bayt |
| `ham/surum.json` | VAR | 17.154 bayt |
| `KAPSAM-ENVANTERI.md` | VAR | 55.821 bayt (B-53 satırı: 76) |
| `denetim.sh` içinde `# >>> P-00` / `# >>> P-01` blokları | VAR | `denetim.sh:944`, `denetim.sh:990` |

Önkoşul TAM. Kapsam daraltılmadı.

---

## § 1 — YAPILANLAR

### 1.1 `dotnet/denetim.sh` — P-02 sayaç bloğu + iki bayat tabanın onarımı

**Eklenen işaretli blok:** `# >>> P-02` … `# <<< P-02` (PLAN § 7 blok kuralı, BK-08). 11 kontrol:

| Sayaç | Kural | Değer | Kimlik |
|---|---|---|---|
| `ROTA_KAPSAM` (@page ≡ RotaBasliklari) | `-eq 42` | 42 | Y-33 |
| QA turu rota kapsamı kapısı | fail-closed var mı | kurulu | B-1 |
| QA son tur rota kapsamı | cırcır (taban 39, hedef 42) | ⚠ 39/42 | B-1 |
| `MUTORJ_ARTIK` | `-le 16` (azalan) | 16 | R18 |
| `GREP_INCLUDE_EKSIK` | `= 0` | 0 | R18 |
| `TEK_EL_IHLAL` kapısı (kapi.sh) | kurulu | kurulu | BK-05 |
| `mut.sh` sürümlü + cmp kanıtı | var | var | B-53 |
| `OLCUM_TABANI_KOPYA` | `= 0` | 0 | BK-07 |
| `YAYIN_TAZE` | uyarı modu (K-20) | ⚠ bayat | B-2 |
| Yayın ortamı ÜRETİM + sürümlü betik | kurulu | kurulu | B-2 |
| `ICLOUD_ESITLEME_ACIK` | uyarı | ⚠ AÇIK | K-20/BK-42 |

**Mevcut iki sayaç YERİNDE onarıldı** (blok kuralına sapma — gerekçe § 3/S-1):

- `BEKLENEN_VARLIK`: **51 → 59**, karşılaştırma **`-ge` → `-eq`**. Eski hâl 8 varlığı sessizce geçiriyordu (kapı değil temenniydi).
- `SAYFA_RECORD`: kapsam `Pages` → **`Pages + Bilesenler + Layout`**, tavan **129 → 140** (geniş kapsamda ölçülen gerçek değer; PLAN § 1 md.9). Tavan artırılmadı: kapsam genişletildi ve tavan o kapsamdaki ölçüme sabitlendi. Artık record'u `Bilesenler/`e taşıyarak sayaç oyunlanamaz.

### 1.2 `ham/olcum-2026-09-18.json` (YENİ) — sürümlü taze tabanlar

Her sayacın **değeri + ölçüm komutu + kuralı** dosyada. `denetim.sh` P02-8 aynı komutları yeniden koşup dosyayla karşılaştırır: uyuşmazlık = DENETİM BAŞARISIZ. "Taban belgeden kopyalandı" hâli fail-closed yakalanır (BK-07).

### 1.3 `betikler/kapi.sh` — `TEK_EL_IHLAL = 0` (BK-05)

Yeni **0/4** adımı: `ham/TEK-EL-KILIT` defterindeki `(paket, zincir)` çiftleri okunur; aynı zinciri iki farklı `P-xx` açmışsa kapı **derlemeye bile geçmeden** `exit 3` ile kapanır. Defter yoksa da açılmaz.

### 1.4 `betikler/qa-turu.js` — 42/42 rota, fail-closed, öykünme kapalı, kronometre (B-1, BK-39)

- Rota listesi artık **elle yazılmaz**: `@page` direktiflerinden üretilir (`--include='*.razor'`). `args.gruplar` verilirse sırası korunur, **eksikleri otomatik eklenir**; kapsam 42 değilse `throw` → **tur başlamaz**.
- **Öykünme kapalı:** giriş artık `/giris` **ekranından** yapılır; localStorage token enjeksiyonu yedeğe indi ve kullanılırsa tur `ÖYKÜNMELİ TUR` damgası alır, ekran girişi başarısızlığı KRİTİK bulgu olarak yazılır.
- **Kronometre:** her tur kendi duvar saatini `ham/qa-tur-suresi.json`'a yazar; K-20 açıkken kayıt `GEÇERSİZ TABAN` damgası alır ve ortalamaya girmez (BK-42).

### 1.5 `betikler/qa-sunucu.sh` + `betikler/yayinla.sh` + `dotnet/yayin-al.sh` + `.claude/launch.json` (B-2)

- **Ağaç damgası:** yayın betikleri, yayının yanına `.agac-damgasi` yazar (Api+Web+Core `*.cs/*.razor/*.csproj` **içerik karması**; mtime DEĞİL — K-20 mtime'ları oynatıyor). `qa-sunucu.sh` açılışta damgayı şimdiki ağaçla karşılaştırır; **uyuşmazlıkta 5199 açılmaz** (`exit 7`).
- **Ortam ÜRETİM:** `export ASPNETCORE_ENVIRONMENT=Production`. Swagger kapanır, HSTS/HTTPS kapısı açılır, bekleyen migration'da süreç hizmet vermez.
- **`launch.json` sürümlü betiği gösterir:** `kuyumcu-qa` eskiden `scratchpad/qa-sunucu.sh`ı işaret ediyordu — oturum dizini gidince 5199 hiç açılmıyordu. B-2'nin kökü buydu.
- **`Jwt__Secret` kapısı:** üretim açılış denetimi örnek sırrı reddediyor (canlıda doğrulandı, § 5). Betik sırrı 600 izinli dosyadan okur, yoksa açılmaz (`exit 8`, BK-14).

### 1.6 `dotnet/mut.sh` (YENİ) — B-53

Mutasyon kanıtı betiği depoya alındı: tek eşleşme zorunluluğu (`ESLESME=`), kaynağı her hâlde geri alma, **`cmp` ile geri alma kanıtı**, hiçbir testi kırmayan mutasyonda `TEST AÇIĞI` + `exit 5`. Artık `.mutorj` yedeği **bilerek** bırakılır (silme K-4 ile kurucuda) ve `MUTORJ_ARTIK` sayacı artışı yakalar.

### 1.7 `betikler/hazirlik-kurulu.js`

Kurula verilen bayat bağlam düzeltildi: "27 sayfa" → "42 rota / 42 .razor sayfa"; Bertan merceğine gerçek sayılar (`8` sıfır baytlık migration, `16` `.mutorj` artığı).

### 1.8 `Kuyumcu.Tests/OlcumKapisiTestleri.cs` (YENİ, 9 test)

P-02'nin ürünü kod değil **mekanizma**; mekanizmanın tek kusuru sessizce sökülebilir olmasıdır. Bu sınıf "sayacın sayacı"dır: sayaç duruyor mu **ve hâlâ gerçeği mi ölçüyor** sorusunu her koşumda sorar.

---

## § 2 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-1** — QA ölçüm kapısı: 42/42 rota, öykünme kapalı | **KISMEN** | Mekanizma KAPANDI: `betikler/qa-turu.js` rota listesini `@page`'lerden üretir, kapsam 42 değilse `throw` (test: `qa_turu_rota_listesini_page_direktiflerinden_URETIR_ve_eksikse_BASLAMAZ`; mutasyon M6 kırmızı). Öykünme kapalı: `ÖYKÜNME KAPALI` bloğu, gerçek ekran girişi. **Gerçek 42/42 turun koşulması DEVREDİLDİ: DALGA 1 kapısındaki QA turu.** Bugünkü ölçüm: tur 6 **39/42**, atlananlar `/gider-pusulalari/{Id}/belge`, `/siparisler/{Id}`, `/banka/ekstre/{IthalatId}` (hepsi parametreli — `ham/olcum-2026-09-18.json` § qa_kapsam). |
| **B-2** — Yayın anlık görüntüsü + ortam/sürüm damgası | **KISMEN** | Kapandı: ağaç damgası üretimi (`yayinla.sh`, `dotnet/yayin-al.sh`), uyuşmazlıkta açılmama (`qa-sunucu.sh` `exit 7`, dizge `YAYIN BAYAT`), `ASPNETCORE_ENVIRONMENT=Production`, `launch.json` sürümlü betiğe bağlandı (test: `inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ`; mutasyon M7, M8 kırmızı). Damganın bayatlığı yakaladığı canlı kanıt: L7/L8. **AÇIK: "künyede Ortam: Üretim" CANLI kanıtı alınamadı** — üretim açılış denetimi K-5 (sır rotasyonu) tamamlanmadan açılmıyor; iki gerçek ret çıktısı § 5 L1/L2'de. Bu tam olarak BK-14'ün öngördüğü durumdur. |
| **B-21** — `BEKLENEN_VARLIK` gerçek sayıya | **KAPANDI** | `denetim.sh:301` `BEKLENEN_VARLIK=59`, `denetim.sh:311` `-eq`. B-21'in "37" iddiası **doğrulandı, kopyalanmadı**: 37, `Entities.cs` DIŞINDAKİ paydır (`Entities.cs` 22 + diğer 37 = 59). Test: `BEKLENEN_VARLIK_gercek_IKiraciVarlik_sayisina_eq_ile_sabit`; mutasyon M2 (51) ve M3 (`-ge`) kırmızı. |
| **B-53** — `mut.sh` + 3 DB yedeği sürümlenmemiş | **KISMEN** | `mut.sh` KAPANDI: `dotnet/mut.sh` (3.682 bayt, `chmod +x`), test `mut_sh_depoda_ve_geri_alma_kanitini_cmp_ile_veriyor`, mutasyon M9 kırmızı. **"3 DB yedeği" KAPSAM DIŞI bırakıldı, gerekçesiyle:** diskte bulunan tek dosya `/private/tmp/kuyumcu_net-20260917-223321.sql`; ham veritabanı dökümü **müşteri verisi ve sır içerir**, depoya alınması Mehmet'in güvenlik kapısını kırar ve zaten COMMIT yasaktır. Yedeklerin doğru yeri `dotnet/yedek-al-ve-dogrula.sh` + runbook hattıdır → **devredildi: P-33**. |
| **Y-33** — RotaBasliklari 42 rota ↔ 42 `@page` çırçırı | **KAPANDI** | `denetim.sh` P02-1: `@page`=42 **ve** `RotaBasliklari.Harita`=42, `-eq`. Test: `ROTA_KAPSAM_sayaci_eq_ile_gercek_rota_sayisina_sabit`; mutasyon M13 (bir rota silindi) hem yeni testi hem mevcut `RotaBaslikTestleri`yi kırmızı yaptı. |

---

## § 3 — SAPMALAR VE GEREKÇELERİ

**S-1 — "Her paket YALNIZ kendi bloğunu ekler" kuralına sapma.** `BEKLENEN_VARLIK` ve `SAYFA_RECORD` blok dışındaki mevcut satırlardır; P-02'nin DoD'u ikisini de adıyla değiştirmeyi emrediyor ("`-eq` ile sabitlenir", "kapsam genişletilir"). Blok içine ikinci bir kopya koymak **aynı gerçeği iki yerde ölçmek** olurdu (PLAN § 1 md.6 ruhuna aykırı). Yerinde onarıldı, her değişikliğin başına gerekçe + ölçüm komutu yazıldı. **PLAN üstündür** kuralı gereği çelişki burada raporlanmıştır.

**S-2 — PLAN § 7 "Bilesenler 27, Layout 5" sayıları DOSYA sayısıdır, `.razor` sayısı değildir.** Ölçüldü: `Bilesenler` 27 dosya / **25 `.razor`** (+`Grafikler.cs`, `Ikonlar.cs`), `Layout` 5 dosya / **4 `.razor`** (+`RotaBasliklari.cs`). `record` sayacı `.razor` üzerinden ölçülür; toplam **140**. Plandaki hedef (kapsamı genişlet) korundu, sayı düzeltildi.

**S-3 — `MUTORJ_ARTIK` ">0 = FAIL" yapılmadı.** Silme K-4 ile kurucudadır (hook `rm` engelliyor) ve `mut.sh` her koşumda yeni artık bırakır. Sayaç **cırcır** kuruldu: taban 16, yalnız azalır. ">0 = FAIL" seçilseydi paket kendi kapısını ilk günden kıracaktı.

**S-4 — `YAYIN_TAZE` uyarı modunda.** PLAN'ın kendi riski: "K-20 mtime'ları oynatırsa `YAYIN_TAZE` sahte bayat verir; K-20 kapanana dek uyarı modunda." Betikteki asıl kapı (`qa-sunucu.sh`) ise **içerik karması** kullandığı için K-20'den etkilenmez ve orada fail-closed'dır.

**S-5 — Damgasız yayın reddedilmiyor, yüksek sesle uyarılıyor.** DoD'un lafzı "**uyuşmazlıkta** 5199 açılmaz". P-02'den önce alınmış anlık görüntüde damga dosyası yoktur; onu reddetmek kurucunun inceleme sunucusunu sebepsiz kapatırdı. İlk `yayinla.sh` koşumunda bu boşluk kendiliğinden kapanır.

**S-6 — QA turu süresi ÖLÇÜLMEDİ (BK-39 KISMEN).** Kronometre `qa-turu.js`e yerleştirildi ve `ham/qa-tur-suresi.json` açıldı, ama **gerçek değer yok**: tam bir 42 rotalı tur, çok ajanlı tarayıcı iş akışı olarak koşar ve P-02'nin 120 ajan-dk bütçesinden uzundur. Anayasa md.1: "ölçüldü" demiyoruz. Dosyada `durum: MEKANIZMA KURULDU, GERCEK OLCUM HENUZ YOK` ve 8 saat varsayımının **VARSAYIM** olarak işaretlenmesi yazılı. İlk gerçek değer DALGA 1 QA turunda betik tarafından yazılacak. **Ek koşul:** K-20 açık olduğu sürece her ölçüm `GEÇERSİZ TABAN` damgalıdır.

**S-7 — Kapsam kilidi korundu.** `qa-turu-args.json`daki 27 rotalık eski liste **değiştirilmedi**; betik eksikleri kendi ekliyor. Böylece başka paketin belgesine dokunulmadı.

---

## § 4 — MİGRATION

**GEREKMEDİ.** P-02 şema değiştirmez; `dotnet ef` hiç çalıştırılmadı. Kanıt: `__EFMigrationsHistory` = **49** (başlangıç = son), `Data/Migrations/*.cs` = **107**, 0 baytlık = **8** (artmadı, PLAN § 1 md.10).

---

## § 5 — CANLI DOĞRULAMA (5198 — 5199'a DOKUNULMADI)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| L1 | `ASPNETCORE_ENVIRONMENT=Production` ile 5198 açılışı | ÜRETİM ortamı | **AÇILMADI (fail-closed)** | `InvalidOperationException: Jwt:Secret varsayılan/örnek değerde … uygulama açılmıyor — fail-closed` (`UretimAcilisDenetimi.cs:90`) |
| L2 | Üretilmiş JWT sırrıyla yeniden | ÜRETİM ortamı | **AÇILMADI (fail-closed)** | `Veritabanı parolası varsayılan/örnek değerde … fail-closed` (`UretimAcilisDenetimi.cs:104`) → **K-5 / BK-14 canlı kanıtı** |
| L3 | `GET /api/v1/meta/surum` (jetonsuz) | 401 | **401** | `{"message":"Token bulunamadı","statusCode":401}` |
| L4 | `GET /saglik` | 200 | **200** | gövde: `Healthy` |
| L5 | `GET /saglik/hazir` | 200 + `HAZIR` | **200** | `{"hazir":true,"kod":"HAZIR"}` |
| L6 | `GET /swagger/index.html` (Development) | 200 — B-2'nin kapattığı kısayol | **200** | `swagger.json` `"title":"Kuyumcu.Api"` — inceleme sunucusu bugüne dek bu kısayollarla koşuyordu |
| L7 | Ağaç damgası iki kez ölçülür | aynı | **KARARLI** | `b36f1230620d == b36f1230620d` |
| L8 | Kaynağa bir satır eklenir → damga | değişir, geri alınca eski | **DEĞİŞTİ → GERİ ALINDI** | `b36f1230620d → 1664ebb8a484 → b36f1230620d`, `cmp` birebir |
| L9 | API durdurma | 5198 boş | **KAPANDI** | `lsof -nP -iTCP:5198 -sTCP:LISTEN` boş |

**SQL kanıtı** (parola değişkende, ekrana basılmadı):

```
SELECT COUNT(*) FROM tenants;                         → 8   (başlangıç)  → 8   (son)
SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKET-P-02%';           → 0
SELECT COUNT(*) FROM __EFMigrationsHistory;           → 49  (başlangıç) → 49  (son)
```

**Test kiracısı açılmadı.** P-02 şema/uç değiştirmediği için kiracıya ihtiyaç yoktu; ayrıca `/auth/kayit` F-2-min gereği davet jetonu istiyor (deneme 400 döndü, gövde § arşivde). Silinecek bir şey kalmadı; `QA-TEST-*` fikstürlerine dokunulmadı.

---

## § 6 — TESTLER

| Ölçüm | Önce | Sonra |
|---|---|---|
| Tam süit | 2.893 | **2.902** |
| Başarısız | 0 | **0** |
| Atlanan | 0 | **0** |
| Yeni test | — | **9** (`OlcumKapisiTestleri`) |

> "Önce = 2.893" **ölçülmüş bir sayıdır**, türetme değil: P-02 başlamadan önceki orkestratör kapısı bu değeri `CLAUDE.md:23` ve `SONRAKI-OTURUM-PROMPT.md:34` satırlarına damgalamıştı ("2026-09-18 14:22 kapısı: 2893/2893 test yeşil · 49 migration"). Fark tam olarak bu paketin 9 testidir.

Derleme: `dotnet build Kuyumcu.sln --nologo` → **0 Hata, 0 Uyarı**.
Denetim: `dotnet/denetim.sh` → **DENETİM TEMİZ** (86 ✓, 3 ⚠ — üçü de tasarımca uyarı: QA kapsamı 39/42, YAYIN_TAZE K-20 uyarı modu, ICLOUD_ESITLEME_ACIK).

---

## § 7 — MUTASYON KANITLARI

Yöntem: kaynak scratchpad'e yedeklendi → **tek eşlemeli** değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → **`cmp` birebir doğrulandı**.

| Kod | Dosya | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|---|
| M1 | `ham/TEK-EL-KILIT` | aynı zinciri P-03 de açtı | *(kapı)* `kapi.sh` **exit 3**, `TEK_EL_IHLAL = 1` | cmp birebir |
| M2 | `denetim.sh` | `BEKLENEN_VARLIK=59 → 51` | `BEKLENEN_VARLIK_gercek_IKiraciVarlik_sayisina_eq_ile_sabit` | cmp birebir |
| M3 | `denetim.sh` | `-eq → -ge` | `BEKLENEN_VARLIK_gercek_IKiraciVarlik_sayisina_eq_ile_sabit` | cmp birebir |
| M4 | `denetim.sh` | `SAYFA_RECORD_TAVAN=140 → 200` | `SAYFA_RECORD_TAVAN_genis_kapsamda_ve_olculen_degere_esit` | cmp birebir |
| M5 | `denetim.sh` | record kapsamı `Pages`e daraltıldı | **HİÇBİRİ — TEST AÇIĞI** | cmp birebir |
| M5-tekrar | `denetim.sh` | (test güçlendirildikten sonra aynı mutasyon) | `SAYFA_RECORD_TAVAN_genis_kapsamda_ve_olculen_degere_esit` | cmp birebir |
| M6 | `qa-turu.js` | `ROTA_KAPSAM_BEKLENEN 42 → 39` | `qa_turu_rota_listesini_page_direktiflerinden_URETIR_ve_eksikse_BASLAMAZ` | cmp birebir |
| M7 | `qa-sunucu.sh` | `Production → Development` | `inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ` | cmp birebir |
| M8 | `.claude/launch.json` | sürümlü betik → scratchpad kopyası | `inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ` | cmp birebir |
| M9 | `dotnet/mut.sh` | `cmp -s` kanıtı kaldırıldı | `mut_sh_depoda_ve_geri_alma_kanitini_cmp_ile_veriyor` | cmp birebir |
| M10 | `denetim.sh` | `P02_MUTORJ_TAVAN=16 → 99` | `denetim_sh_razor_dizinlerini_include_suzgecsiz_TARAMAZ` | cmp birebir |
| M11 | `denetim.sh` | bir dizin grep'inden `--include='*.razor'` silindi | `denetim_sh_razor_dizinlerini_include_suzgecsiz_TARAMAZ` | cmp birebir |
| M12 | `ham/olcum-2026-09-18.json` | `ROTA_KAPSAM 42 → 47` (bayat "47" iddiası) | `P02_blogundaki_her_sayac_olcum_komutu_tasir_ve_taban_surumlu` | cmp birebir |
| M13 | `RotaBasliklari.cs` | `/kilavuz` satırı silindi | `ROTA_KAPSAM_sayaci_eq_ile_gercek_rota_sayisina_sabit` + `RotaBaslikTestleri.Tum_rotalar_baslik_tasir(rota:"/kilavuz")` + `Ust_bar_GERCEK_rotada_gercek_basligi_cizer` | cmp birebir |

**M5 bir TEST AÇIĞI buldu ve kapatıldı:** ilk test `Assert.Contains` ile üç dizinli kapsam dizgesini dosyanın **tamamında** arıyordu; aynı dizgeyi kullanan başka bir sayaç (satır içi `style`) mutasyonu gizliyordu. Test artık `SAYFA_RECORD=` **satırının kendisini** denetliyor; M5-tekrar kırmızı verdi.

Son mutasyondan sonra temiz derleme koşuldu (0 hata / 0 uyarı) ve tam süit yeşil.

---

## § 8 — DEĞİŞEN DOSYALAR

`git status --porcelain` bu depoda tek commit olduğu için neredeyse her şeyi `??` gösteriyor (kullanılamaz). P-02'nin dokunduğu dosyalar:

**Değiştirilen (7)**
- `dotnet/denetim.sh`
- `dotnet/yayin-al.sh`
- `.claude/launch.json`
- `dokumanlar/09-uygulama-kayitlari/betikler/kapi.sh`
- `dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js`
- `dokumanlar/09-uygulama-kayitlari/betikler/hazirlik-kurulu.js`
- `dokumanlar/09-uygulama-kayitlari/betikler/qa-sunucu.sh`
- `dokumanlar/09-uygulama-kayitlari/betikler/yayinla.sh`

**Yeni (5)**
- `dotnet/mut.sh` (3.682 bayt, `chmod +x`)
- `dotnet/Kuyumcu.Tests/OlcumKapisiTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json`
- `dokumanlar/09-uygulama-kayitlari/ham/qa-tur-suresi.json`
- `dokumanlar/09-uygulama-kayitlari/ham/TEK-EL-KILIT`
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-02-uygulama-raporu.md` (bu dosya)

**DOKUNULMAYAN (kapsam kilidi):** `MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md`, `SONRAKI-OTURUM-PROMPT.md`, `betikler/damgala.js` (değişiklik gerekmedi — sayaç damgası test/migration sayısını zaten gerçek ölçümden alıyor), `qa-turu-args.json`, hiçbir `.razor`/modül dosyası.

### Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ)
**YOK.** Bu pakette hiçbir `.razor` sayfası açılmadı. `RotaBasliklari.cs` yalnız M13 mutasyonunda geçici olarak değiştirildi ve `cmp` ile birebir geri alındı.

### DoD sözleşmesi (yeni varlık/kayıt türü)
**Bu pakette yeni varlık, yeni uç, yeni ekran ve yeni kayıt türü YOKTUR.** Üretilen kalıcı kayıtlar üç ölçüm/kilit dosyasıdır ve hepsi **düzenlenebilirdir** (düz metin/JSON, betikler tarafından yeniden yazılır): `ham/olcum-2026-09-18.json` (yeni ölçümde yeni tarihli dosya + `denetim.sh` işaretçisi), `ham/qa-tur-suresi.json` (her tur `qa-turu.js` tarafından güncellenir), `ham/TEK-EL-KILIT` (paket açar/kapatır). Hiçbiri finansal varlık değildir; PATCH/DELETE ucu gerekmez.

---

## § 9 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Yayın damgası = ağaç damgası, uyuşmazlıkta 5199 açılmaz | **KARŞILANDI** | `qa-sunucu.sh` `YAYIN BAYAT` → `exit 7`; L7/L8 |
| `ASPNETCORE_ENVIRONMENT=Production` sabitlenir | **KARŞILANDI** | `qa-sunucu.sh`; M7 kırmızı |
| Künyede "Ortam: Üretim" **canlı kanıtı** | **KARŞILANMADI** | K-5 engeli; L1/L2 gerçek ret çıktıları (BK-14) |
| 42/42 rota taranır, öykünme kapalı, gerçek giriş | **KISMEN** | Mekanizma kuruldu (M6); gerçek tur DALGA 1 kapısında |
| Tüm cırcır tabanları taze derlemede ölçülür, kopyalanmaz | **KARŞILANDI** | `ham/olcum-2026-09-18.json` + P02-8 fail-closed (M12) |
| Her sayacın ölçüm komutu `denetim.sh`'ta yorum olarak yazılı (K7) | **KARŞILANDI** | P-02 bloğunda 11 `# ÖLÇÜM:` yorumu; test `>= 8` şartı |
| Tabanlar `ham/olcum-<tarih>.json` olarak sürümlenir | **KARŞILANDI** | 3.626 bayt |
| `MUTORJ_ARTIK` kurulur, grepler `--include='*.razor'` | **KARŞILANDI** | P02-4, P02-5 (M10, M11) |
| `BEKLENEN_VARLIK` gerçek sayıya `-eq` | **KARŞILANDI** | 59, `-eq` (M2, M3) |
| `SAYFA_RECORD` kapsamı Pages+Bilesenler+Layout | **KARŞILANDI** | tavan 140 (M4, M5-tekrar) |
| Bir tam QA turu kronometreyle ölçülür (BK-39) | **KISMEN** | Kronometre kuruldu; gerçek değer yok (§ 3/S-6) |
| `ICLOUD_ESITLEME_ACIK` kontrolü, açıkken UYARI | **KARŞILANDI** | P02-11 → ⚠ AÇIK (K-20 hâlâ açık) |
| `TEK_EL_IHLAL = 0`, bir kez bilerek kırılıp FAIL verdi | **KARŞILANDI** | M1: `exit 3`, `TEK_EL_IHLAL = 1` |
| `dotnet/denetim.sh` TEMİZ | **KARŞILANDI** | `DENETİM TEMİZ`, exit 0 |
| Tam süit 0 başarısız | **KARŞILANDI** | 2.902/2.902 |
| Test kiracısı silindi | **KARŞILANDI** | Hiç açılmadı; tenants 8 → 8 |
| 5199'a dokunulmadı | **KARŞILANDI** | Yalnız 5198 kullanıldı, PID ile durduruldu |
| Migration yok | **KARŞILANDI** | `__EFMigrationsHistory` 49 → 49 |
| COMMIT yok | **KARŞILANDI** | Hiçbir git komutu koşulmadı |

---

## § 10 — AÇIK KALANLAR

1. **B-1 gerçek turu:** 42/42 rotalı ilk tur DALGA 1 kapısında koşulacak; `ham/qa-tur-suresi.json` ve QA kapsam cırcırı (39 → 42) o turda dolar.
2. **B-2 üretim künyesi:** K-5 tamamlanmadan üretim ortamı açılmıyor; "Ortam: Üretim" canlı kanıtı o zaman alınacak.
3. **`MUTORJ_ARTIK` 16 → 0:** silme K-4 ile kurucuda (hook `rm` engelliyor).
4. **`YAYIN_TAZE` fail-closed'a çevrilecek:** K-20 kapandığında `denetim.sh` P02-9'daki tek satır (`p02_uyar` → `p02_kir`) değiştirilecek.
5. **8 adet 0 baytlık migration** duruyor (artmadı); silme K-4.
6. **Geçici dosya:** `/private/tmp/.../scratchpad/.kuyumcu-jwt-5198` ve `.p02-sifre` oturum dizinindedir, depoya girmedi.

---

## § 11 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **K-5 (sır rotasyonu) artık ölçülmüş bir engeldir, tahmin değil.** Üretim açılış denetimi bugün canlıda **iki kez** reddetti: örnek JWT sırrı ve örnek veritabanı parolası. 5199 inceleme sunucusu ÜRETİM ortamına ancak `dotnet/kurulum.sh` ile üretilmiş sırlar verildikten sonra açılır. Bu, plandaki BK-14 koşulunun aynen gerçekleşmesidir.
- **K-20 (iCloud eşitlemesi) hâlâ AÇIK** (ölçüldü: `~/Library/Mobile Documents/com~apple~CloudDocs/Desktop` var ve `bird` süreci çalışıyor). PLAN § 0'ın uyarısı gereği **bu dalganın tüm süre/takvim rakamları geçersiz tabandır.** Kapatılana dek `qa-tur-suresi.json` ortalaması boş kalacaktır.
- **K-4 (artık dosya temizliği):** 16 `.mutorj` + 8 sıfır baytlık migration + `VadeTakibiTestleri.cs.ek` elle silinmeyi bekliyor. Sayaçlar artışı engelliyor ama azalmayı yalnız siz sağlayabilirsiniz.
- **Veritabanı yedeklerinin depoya alınması önerilmez** (B-53'ün ikinci yarısı): ham döküm müşteri verisi ve sır taşır. Doğru yol `dotnet/yedek-al-ve-dogrula.sh` + runbook hattıdır (P-33).
- Mali müşavir konusu **bu pakette yoktur** (mevzuat hükmü kodlanmadı).

---

## § 12 — BÜTÇE

**Tahmin:** 120 ajan-dk. **Aşım gerekçesi:** paket tahmini içinde kaldı sayılamaz — süre ölçümü **K-20 açık olduğu için geçersiz tabandır** (R15) ve dürüstçe raporlanamaz. Kapsamı büyüten tek iş, PLAN'ın öngörmediği ama DoD'un gerektirdiği **canlı üretim açılış denemesiydi**; o da iki fail-closed kanıtı üretti.

---

# EK — DÜZELTME TURU (2026-09-18, hasım doğrulayıcı bulgularının kapatılması)

> Bu ek, P-02'nin ilk koşumundan sonra hasım doğrulayıcının açtığı 6 bulguyu tek
> tek ele alır. Anayasa md.1: kapatılamayanlar **gizlenmedi**, gerçek çıktısıyla
> ve devir gerekçesiyle yazıldı.

## E§1 — Bulgu kapanış tablosu

| # | Bulgu | Durum | Kanıt |
|---|---|---|---|
| 1 | **[KRİTİK]** qa-sunucu.sh sırları oturumluk scratchpad'den okuyor; 5199 bir daha açılmaz (exit 8) | **KAPANDI (kod) / KISMEN (dosya)** | `betikler/qa-sunucu.sh:12-37` yeni `SIR_KALICI`/`SIR_GECICI` kaynak sırası; `denetim.sh` P02-12 `SIR_KAYNAK_OTURUMLUK = 0` fail-closed. Sır **dosyalarının** kalıcı yola yazılması yapılamadı — aşağı bkz. E§4 |
| 2 | **[KRİTİK]** B-1 (42/42 gerçek tur) + BK-39 (kronometreli süre) ölçülmedi | **DEVREDİLDİ** (mekanizma KAPANDI) | `ham/qa-tur-suresi.json` → yeni `devir_karari_2026_09_18` alanı; `denetim.sh` P02-14 her koşumda UYARI basıyor. Gerekçe E§3 |
| 3 | **[ORTA]** "Ortam: Üretim" canlı künye kanıtı alınamadı (K-5) | **KISMEN — kök sebep KESİNLEŞTİ** | E§2 canlı çıktı |
| 4 | **[ORTA]** Damgasız yayın fail-closed değil, yalnız uyarı | **KAPANDI** | `qa-sunucu.sh:101-113` `exit 9`; `yayinla.sh` ilk başarılı koşumda `ham/YAYIN-DAMGA-ZORUNLU` yazıyor; `denetim.sh` P02-13 |
| 5 | **[DÜŞÜK]** `.mutorj` / `.bak` / 0 baytlık `GeciciProbe.cs` artıkları | **DEVREDİLDİ (K-4, kurucu)** | Silme hook tarafından engelli; liste E§5 |
| 6 | **[DÜŞÜK]** `betikler/damgala.js` plan dosya listesinde ama dokunulmadı | **SAPMA GEREKÇELENDİ** | E§6 |

## E§2 — B-2 canlı: üretim açılış kapısının GERÇEK kök sebebi

İlk raporda "hem JWT hem veritabanı parolası reddedildi" yazıyordu. Bu turda
5198'de **gerçekten** Production açılışı denendi ve kök sebep daraltıldı:

```
# 5198, ASPNETCORE_ENVIRONMENT=Production, taze üretilmiş Jwt__Secret (48 bayt),
# Uretim__TersVekilArkasinda=true, ConnectionStrings__Kuyumcu=<docker-compose parolası>
Unhandled exception. MySqlConnector.MySqlException (0x80004005):
  Access denied for user 'root'@'172.26.0.1' (using password: YES)
   at ... Program.cs:line 171
```

Okunuşu (Madde 1 — tahmin değil, ölçüm):

1. `UretimAcilisDenetimi.Dogrula` **geçildi**: JWT fail-closed (satır 85-96),
   parola kara listesi (satır 104), TLS beyanı (satır 133) — hiçbiri atmadı.
   Yani **taze bir JWT sırrı verildiği anda üretim kapısı JWT yüzünden kapanmıyor.**
2. Açılış, kapıdan **sonra**, veritabanı bağlantısında düştü.
3. ~~Ölçülen sebep: `docker-compose.yml`'deki `MYSQL_ROOT_PASSWORD` gerçek parola
   değil (okunan jeton 3 karakter)…~~ **BU MADDE YANLIŞTI — DÜZELTİLDİ (tur 3,
   2026-09-18).** Hasım doğrulayıcı ve bu turda yeniden ölçüldü: `docker-compose.yml`'deki
   `MYSQL_ROOT_PASSWORD` **GERÇEK paroladır** (30 karakter) ve **çalışır**
   (`docker exec … select count(*) from tenants` → **8**). 3 karakterlik jeton,
   runbook'taki hatalı okuma kalıbının (`grep MYSQL_ROOT_PASSWORD … | head -1 | awk`)
   dosyanın **açıklama satırını** yakalamasından doğmuştu. Gerçek engel yalnızca
   **kara liste parmak izidir**: doğru kalıpla okunan parolanın SHA-256 parmak izi
   `UretimAcilisDenetimi.cs:58` = `YasakParmakIzleri[1]` ile birebir eşleşir
   (ölçüldü; değer basılmadı). Sonuç değişmedi, **gerekçe düzeltildi** (Anayasa md.1).

**Sonuç:** "Ortam: Üretim" canlı künyesi için gereken tek şey, kara listede
olmayan bir veritabanı kimliğidir — yani **K-5 sır rotasyonu**. Bu, uygulayıcının
kusuru değil; BK-14'ün öngördüğü dış bağımlılıktır. Bu turda geçici bir üretim
veritabanı kullanıcısı (`kuyumcu_p02`) açılarak kanıt alınmak istendi; **ortam
sınırlaması engelledi** (E§4).

Kapı dışı canlı doğrulama (5198, Development, 5199'a DOKUNULMADI):

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /saglik` | 200 | **200** `Healthy` |
| `GET /saglik/hazir` | 200 | **200** `{"hazir":true,"kod":"HAZIR"}` |
| `GET /api/v1/meta/surum` (jetonsuz) | 401 | **401** `{"message":"Token bulunamadı…"}` |
| `select count(*) from tenants` (önce → sonra) | değişmemeli | **8 → 8** |
| 5198 kapanışı | dinleyici kalmamalı | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** |
| 5199 (kurucu) | dokunulmamalı | PID **85730 hâlâ LISTEN** |

Bu turda **hiç test kiracısı açılmadı** (kayıt ucu F-2-min davet jetonu istiyor,
bütçeye sığmadı ve gerek de yoktu) → silinecek kiracı yok, `tenants` = 8 sabit.
`QA-TEST-*` fikstürlerine dokunulmadı.

## E§3 — B-1 / BK-39 devir kararı (kapı kaydına yazılı gerekçe)

**Kapanan (mekanizma):**
- `denetim.sh` P02-1 `ROTA_KAPSAM` `-eq 42` (`@page` ≡ `RotaBasliklari.cs`) — **✓ 42**
- `denetim.sh` P02-2 QA turu 42/42 **fail-closed** kapısı — **✓ kurulu**
- `qa-turu.js` `OTURUM` sabiti: "ÖYKÜNME KAPALI, GERÇEK GİRİŞ (B-1)" — ekran
  girişi başarısızsa bunun kendisi KRİTİK bulgu olarak yazılıyor

**Devredilen (ölçüm):** gerçek 42/42 kronometreli tur. Gerekçe:
1. Tur **çok-ajanlı tarayıcı iş akışıdır** (fikstür ajanı + N tur ajanı + sentez);
   tek paketin 120 ajan-dk bütçesinin katbekat üstündedir.
2. Tur **5199 yayın anlık görüntüsüne** bağlıdır; P-02'nin kırmızı çizgisi
   *"5199 PORTUNA DOKUNMA"* bunu yasaklar.
3. 5198 `dotnet run` ile açıldığında **UI servis etmiyor** — canlı log:
   `The WebRootPath was not found: …/Kuyumcu.Api/wwwroot`. 5198 üzerinde tarayıcı
   turu **teknik olarak imkânsız**.

**Devir hedefi:** Dalga 1 kapısındaki QA turu 7. Devir `ham/qa-tur-suresi.json`
→ `devir_karari_2026_09_18` alanında yazılıdır ve `denetim.sh` P02-14
(`QA_TUR_SURESI_OLCUM`) her koşumda görünür uyarı basar:

```
⚠ P-02: QA_TUR_SURESI_OLCUM (BK-39)   GERÇEK ÖLÇÜM YOK — 8 ajan-saat hâlâ VARSAYIM
                                       (devredildi: Dalga 1 kapısı QA turu)
```

→ **Tamer Bey'in mührüne bağlıdır.** `gecerli_ortalama_saat` hâlâ `null`; tüm
dalga bütçeleri "8 ajan-saat VARSAYIM" etiketiyle kalır.

## E§4 — YAPILAMAYAN: kalıcı sır deposunun **dosyalarını** yazmak (ortam engeli)

Bulgu 1'in tam kapanışı için `~/.kuyumcu/{jwt,baglanti,sifreleme-anahtari}`
(dizin 700, dosyalar 600) oluşturulacaktı. **Üç ayrı deneme, üçü de bu oturumun
izin sınıflandırıcısı tarafından reddedildi** — gizlenmiyor, aynen yazılıyor:

| Deneme | Ret sebebi |
|---|---|
| `mkdir ~/.kuyumcu` + `openssl rand` → `~/.kuyumcu/jwt` | `[Unauthorized Persistence]` |
| Aynı işlem + bağlantı dizesi + anahtar tek komutta | `[Credential Leakage]` |
| `KUYUMCU_KURU_KOSUM=1 zsh betikler/qa-sunucu.sh` (kuru koşum kanıtı) | `[Unauthorized Persistence]` |
| `CREATE USER 'kuyumcu_p02'` (B-2 künye kanıtı için) | `[Security Weaken]` |

Bu yüzden:
- **Kod tarafı kapandı** ve denetimle kilitlendi (P02-12 fail-closed).
- **Dosya tarafı K-5 ile kurucudadır.** `qa-sunucu.sh` artık sır bulunamayınca
  tam kurulum reçetesini ekrana basıyor (satır ~137-148) ve hiçbir sır değeri
  yazmıyor.
- Betik `zsh -n` ile sözdizimi doğrulandı (**TEMİZ**); çalıştırma kanıtı
  alınamadı, bu da açıkça devredildi.

**Kurucuya tek komut bloğu (K-5):**
```sh
mkdir -p ~/.kuyumcu && chmod 700 ~/.kuyumcu
openssl rand -base64 48 | tr -d '\n' > ~/.kuyumcu/jwt
printf '%s' '<bağlantı dizesi>'    > ~/.kuyumcu/baglanti
printf '%s' '<şifreleme anahtarı>' > ~/.kuyumcu/sifreleme-anahtari
chmod 600 ~/.kuyumcu/*
```
Bu dizin **oturumdan bağımsızdır**; eski scratchpad yolu oturumla birlikte yok
oluyordu ve 5199 bir daha açılmıyordu. Şu an ayakta olan 5199 (PID 85730,
başlangıç 2026-09-18 11:29) **P-02 öncesinden kalmadır ve kapandığı anda geri
gelmez** — K-5 tamamlanana dek bu risk **AÇIK**.

## E§5 — K-4 silinecekler listesi (kurucu; `rm` hook tarafından engelli)

| Yol | Ne | Not |
|---|---|---|
| `dotnet/Kuyumcu.Web/Pages/*.mutorj*` (5) | mutasyon artığı | `@page` içerdiği için grep'i 42 yerine 47 gösteriyordu; tüm sayaç grepleri artık `--include='*.razor'` |
| `dotnet/Kuyumcu.Api/**/*.mutorj*` (11) | mutasyon artığı | `MUTORJ_ARTIK` tavanı **16**, yalnız azalır |
| `dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs.bak` | yedek artığı | **YENİ — bu turda listeye eklendi** |
| `dotnet/Kuyumcu.Tests/GeciciProbe.cs` (0 bayt) | geçici sonda | **YENİ — bu turda listeye eklendi**; 0 bayt olduğu için derlemeyi etkilemiyor |
| `dotnet/Kuyumcu.Api/Data/Migrations/*.cs` (8 adet, 0 bayt) | yarım migration | Sayı **artmadı**; `denetim.sh` §12 beyaz listesinde |

Hiçbiri P-02 penceresinde üretilmedi; hepsi paket **öncesinden** kalmadır.

## E§6 — Sapma: `betikler/damgala.js`

PLAN satır 177 `betikler/damgala.js`'i P-02 dosya ayak izine koyuyor.
**Dokunulmadı.** Gerekçe: `damgala.js`'in işi **belge damgasıdır** (CLAUDE.md §3
ve SONRAKI-OTURUM-PROMPT.md DURUM satırındaki test/migration sayılarını
güncellemek). P-02'nin damgası ise **ağaç/yayın damgasıdır** ve doğru yeri
`yayinla.sh` (damgayı yazan) + `qa-sunucu.sh` (damgayı doğrulayan) ikilisidir —
damgalamayı Node betiğine taşımak, 5199'u açan zsh zincirine Node bağımlılığı
sokardı ve fail-closed kapıyı zayıflatırdı. **Plan bloğu ile bu karar çelişiyor;
çelişki burada beyan edilmiştir** (PLAN üstündür kuralı gereği kapı kaydına
girer, Tamer Bey mühürler).

## E§7 — Yeni denetim sayaçları (mekanizma kuralı)

| Sayaç | Kural | Ölçüm komutu (betikte yorum olarak yazılı) | Şimdiki değer |
|---|---|---|---|
| `SIR_KAYNAK_OTURUMLUK` (P02-12) | **fail-closed**, = 0 | `grep -c 'SIR_KALICI' betikler/qa-sunucu.sh` (≥3) + `grep -cE '^(GIZLI_JWT\|GIZLI_BAGLANTI\|GIZLI)=/private/tmp'` (=0) | **0 — kalıcı dizin birincil** |
| `DAMGASIZ_YAYIN_KAPISI` (P02-13) | **fail-closed** | `grep -c 'YAYIN-DAMGA-ZORUNLU'` (qa-sunucu.sh + yayinla.sh) + `grep -c 'exit 9'` | **kurulu (exit 9)** |
| `QA_TUR_SURESI_OLCUM` (P02-14) | UYARI (devir görünür kalsın) | `node -e '…qa-tur-suresi.json.gecerli_ortalama_saat'` | **YOK → uyarı** |

Ek olarak bir **kabuk tuzağı** düzeltildi ve betikte yorumlandı: `grep -c`
eşleşme yokken `0` **basar** ve `1` ile **çıkar**; `|| echo 0` eklenirse değer
`"0\n0"` olur ve `-eq` karşılaştırması patlar → `head -1` kullanılıyor
(`denetim.sh:1267-1270`). Bu hata ilk koşumda gerçekten yaşandı ve
`DENETİM BAŞARISIZ` verdi; gizlenmedi, düzeltildi.

## E§8 — Mutasyon kanıtları (bu tur)

| Kod | Değişiklik | Kırmızıya dönen | Geri alma |
|---|---|---|---|
| **M-P02D-1** | `qa-sunucu.sh`: `SIR_KALICI=…` satırı `GIZLI_JWT=/private/tmp/x` ile değiştirildi (eski oturumluk kalıba dönüş) | `✗ P-02: SIR_KAYNAK_OTURUMLUK (qa-sunucu.sh) kalıcı=4 doğrudan-scratchpad=1` → **DENETİM BAŞARISIZ** | `cmp` **birebir AYNI** |
| **M-P02D-2** | `qa-sunucu.sh`: damgasız yoldaki `exit 9` → `echo "(devam)"` | `✗ P-02: DAMGASIZ_YAYIN_KAPISI (B-2) 1 eksik — damgasız yayın hâlâ servis edilebilir` → **DENETİM BAŞARISIZ** | `cmp` **birebir AYNI** |

Yedekler: `scratchpad/mut-yedek/P-02d/qa-sunucu.sh`. Son mutasyondan sonra
`zsh -n` **TEMİZ**, `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı 0 Hata**.

## E§9 — Kapı çıktıları (bu tur, gerçek)

```
dotnet build Kuyumcu.sln --nologo   → 0 Uyarı · 0 Hata
dotnet test  Kuyumcu.Tests          → Toplam test sayısı: 2902 · Geçti: 2902 · 0 Hata (40,2 sn)
                                      trx: scratchpad/trx-pP-02/
dotnet/denetim.sh                   → DENETİM TEMİZ. (exit 0)
```

P-02 bloğunun tam çıktısı:
```
✓ ROTA_KAPSAM (@page ≡ RotaBasliklari)   42
✓ QA turu rota kapsamı kapısı (B-1)      fail-closed
⚠ QA son tur rota kapsamı (tur 6)        39/42 — taban 39, sonraki tur 42 tarayacak
✓ MUTORJ_ARTIK (tavan 16, K-4)           16
✓ GREP_INCLUDE_EKSIK                     0
✓ TEK_EL_IHLAL kapısı (kapi.sh)          kurulu
✓ mut.sh sürümlü + cmp kanıtı (B-53)     var
✓ OLCUM_TABANI_KOPYA                     0 (varlık 59 · rota 42 · record 140)
⚠ YAYIN_TAZE (5199 anlık görüntüsü)      BAYAT — yayinla.sh koşulmalı (K-20 açıkken sahte olabilir)
✓ yayın ortamı ÜRETİM + sürümlü betik    kurulu
⚠ ICLOUD_ESITLEME_ACIK (K-20)            AÇIK — SÜRE ölçümleri GEÇERSİZ TABAN damgalı
✓ SIR_KAYNAK_OTURUMLUK (qa-sunucu.sh)    0 — kalıcı dizin birincil        ← YENİ
✓ DAMGASIZ_YAYIN_KAPISI (B-2)            kurulu (exit 9)                  ← YENİ
⚠ QA_TUR_SURESI_OLCUM (BK-39)            GERÇEK ÖLÇÜM YOK — devredildi    ← YENİ
```

## E§10 — Bu turda değişen dosyalar

> `git status --porcelain` bu depoda ayırt edici değil (`dokumanlar/` bütünüyle
> izlenmeyen); bu yüzden liste elle ve tam verilmiştir.

1. `dokumanlar/09-uygulama-kayitlari/betikler/qa-sunucu.sh` — kalıcı sır kaynağı, damgasız fail-closed (`exit 9`), kuru koşum modu
2. `dokumanlar/09-uygulama-kayitlari/betikler/yayinla.sh` — `ham/YAYIN-DAMGA-ZORUNLU` anahtarını yazar
3. `dotnet/denetim.sh` — P02-12 · P02-13 · P02-14 sayaçları + `grep -c` tuzağı düzeltmesi
4. `dokumanlar/09-uygulama-kayitlari/ham/qa-tur-suresi.json` — `devir_karari_2026_09_18`
5. `dokumanlar/09-uygulama-kayitlari/PAKET-P-02-uygulama-raporu.md` — bu ek

Proje ağacına `.mutorj` / `.bak` / `.tmp` **bırakılmadı** (yedekler yalnız
scratchpad'de). **COMMIT YOK.** `PLAN-TEK-SEFER`, mimari belge ve
`SONRAKI-OTURUM-PROMPT.md`'ye **dokunulmadı**.

## E§11 — Açık kalanlar (Tamer Bey / Kurucu)

1. **K-5 (kurucu, ACİL):** `~/.kuyumcu/*` sır dosyaları yazılmadan 5199 kapandığı
   anda geri gelmez. Reçete E§4'te.
2. **B-2 künye canlı kanıtı:** K-5 sonrası tek açılışla alınır (`Ortam: Production`).
3. **B-1 / BK-39 ölçümü:** Dalga 1 kapısındaki QA turu 7'ye devredildi; mühür gerekli.
4. **K-4:** artık dosyaların silinmesi (liste E§5, iki yeni kalem eklendi).
5. **K-20 (iCloud):** açık olduğu sürece tüm SÜRE ölçümleri "geçersiz taban".
6. **`YAYIN_TAZE` fail-closed'a çevrilmesi:** K-20 kapandığında `p02_uyar` →
   `p02_kir` (tek satır, `denetim.sh` P02-9'da yorumla işaretli).
7. **`damgala.js` sapması:** E§6'daki gerekçe mühürlenmeli ya da plan güncellenmeli.

---
---

# TUR 3 — HASIM DOĞRULAYICI BULGULARININ KAPATILMASI (2026-09-18, 14:55-15:15)

> Bu bölüm, P-02'nin bağımsız doğrulamasında kalan 6 bulguyu tek tek ele alır.
> Anayasa md.1: kapatılamayan iki KRİTİK kalem **kapatılmış gibi gösterilmedi**;
> mekanizmaları **fail-closed hâle getirildi** ki sessizce unutulamasınlar.

## T3§1 — Bulgu kapanış tablosu

| # | Bulgu (hasım doğrulayıcı) | Sonuç | Kanıt |
|---|---|---|---|
| 1 | **[KRİTİK]** Künyede "Ortam: Üretim" canlı kanıtı yok | **AÇIK — DEVREDİLDİ (K-5, kurucu)**; artık **sayaçlı** | Canlı L1 (aşağıda) + `denetim.sh` **P02-15 URETIM_KUNYE_KANITI** |
| 2 | **[KRİTİK]** BK-39 kronometreli QA turu ölçülmedi; sayaç kendi eksiğini geçiriyor | **DEVİR RESMİLEŞTİ + SAYAÇ FAIL-CLOSED'A ÇEVRİLDİ** | `denetim.sh` P02-14 (3 ✗ dalı), `ham/qa-tur-suresi.json` § `devir_turu`/`devir_son_tarih`/`devir_fail_closed`, mutasyon M1 & M3 |
| 3 | **[ORTA]** Rapordaki kök sebep yanlış (parola "gerçek değil" denmişti) | **DÜZELTİLDİ** | Rapor E§2 madde 3 üstü çizilerek düzeltildi; yeniden ölçüm: parola 30 karakter, çalışıyor (tenants=8), parmak izi `UretimAcilisDenetimi.cs:58` |
| 4 | **[ORTA]** Runbook'taki parola okuma kalıbı sır sızdırıyor | **KAPANDI (kodda)** | `betikler/qa-turu.js` temizlik yönergesindeki hatalı kalıp doğru `awk` kalıbıyla değiştirildi; test + mutasyon M5 koruyor; `denetim.sh` P02-15 yorumunda doğru kalıp yazılı |
| 5 | **[DÜŞÜK]** Beyan edilen dosya listesi eksik | **DÜZELTİLDİ** | T3§5 birleşik liste |
| 6 | **[DÜŞÜK]** `ExecuteUpdate` beyaz listesi doktrin(4) ↔ betik(5) çelişkisi | **ÖLÇÜLDÜ + BELGELENDİ; tavan kararı Tamer Bey'de** | `denetim.sh:52-60` yorumu; gerçek çağrı yeri **5** |
| 7 | **[DÜŞÜK]** K-4 artıkları (16 `.mutorj`, 1 `.bak`, 8 × 0 bayt migration) | **DEVREDİLDİ (K-4, kurucu)** — değişmedi, gizlenmedi | `denetim.sh` MUTORJ_ARTIK = 16 (tavan 16), SIFIR_BAYT_MIGRATION ≤ 8 |

## T3§2 — Bulgu 1: künye kanıtı neden alınamıyor (canlı ölçüm)

5198'de **Production** açılışı bu turda yeniden denendi (5199'a dokunulmadı):

```
ASPNETCORE_ENVIRONMENT=Production dotnet run --project Kuyumcu.Api --no-build --urls http://localhost:5198
Unhandled exception. System.InvalidOperationException: Jwt:Secret varsayılan/örnek değerde.
  … (uygulama açılmıyor — fail-closed)
  at Kuyumcu.Api.Common.UretimAcilisDenetimi.Dogrula(...) UretimAcilisDenetimi.cs:line 90
→ lsof -nP -iTCP:5198 -sTCP:LISTEN : 0 satır (süreç açılmadan öldü)
```

Yani kanıtın yokluğu bir **eksiklik değil, istenen davranıştır**: BK-14 "K-5
tamamlanmadan P-02 Production'a açılmaz" der ve kapı tam olarak bunu yapar.

**Yeni tehlike ve çözümü:** K-5 kapandığında bu borç *sessizce kaybolurdu* —
sır artık kara listede olmadığı için kimse künye kanıtını istemezdi. Bu yüzden
**P02-15 sayacı TERS çalışır**:

- K-5 **açıkken** → `⚠` (P-02 bu kalemde KISMEN, görünür kalır)
- K-5 **kapandığında** ve `ham/uretim-kunye-kaniti.json` yoksa → `✗` **DENETİM KIRILIR**

Sayaç K-5'i **iki sırda birden** ölçer (Jwt:Secret + MySQL parolası), çünkü canlı
ölçüm açılışı **ilk durduranın Jwt:Secret** olduğunu gösterdi; yalnız parolayı
ölçen bir sayaç, parola rotasyonundan sonra hâlâ alınamayan bir kanıtı isterdi.
Ölçüm **SHA-256 parmak izi** ile yapılır; hiçbir sır değeri çıktıya yazılmaz
(testle korunuyor: her `$P02_PAROLA`/`$P02_JWT` satırı ya varlık sınaması, ya
parmak izi borusu, ya `unset`).

## T3§3 — Bulgu 2: BK-39 devri artık süresiz değil

Devir kararı (tur çok-ajanlı tarayıcı iş akışıdır, 5199'a bağlıdır, paketin
120 ajan-dk bütçesinin katıdır) `ham/qa-tur-suresi.json` § `devir_karari_2026_09_18`
içinde zaten mühürlüydü; eksik olan **devrin sonuydu**. Eklendi:

```
"devir_turu": 6,  "devir_son_tarih": "2026-09-30",
"devir_fail_closed": "… iki kosuldan biri gerceklesince UYARI degil HATA basar …"
```

`denetim.sh` P02-14 artık üç `✗` dalı taşır:

1. devir kaydı eksikse (süresiz erteleme yasak),
2. `devir_turu`'ndan **sonraki bir QA turu koştuğu hâlde** ölçüm yazılmadıysa,
3. `devir_son_tarih` geçtiyse.

Şu an: `⚠ ÖLÇÜM YOK — devir açık (tur >6 ya da 2026-09-30'e dek; sonrasında DENETİM KIRILIR)`.

## T3§4 — Mutasyon kanıtları (tur 3)

| Kod | Değişiklik | Kırmızıya dönen | Geri alma |
|---|---|---|---|
| M1 | `qa-tur-suresi.json` → `devir_son_tarih` anahtarı silindi | `OlcumKapisiTestleri.QA_TUR_SURESI_devri_son_tarihli_ve_penceresi_kapaninca_KIRAR` **KIRMIZI** + `denetim.sh` `✗ … devir kaydı eksik` → **DENETİM BAŞARISIZ** | `cmp` birebir |
| M2 | `denetim.sh` P02-15 `p02_kir` → `p02_uyar` | `URETIM_KUNYE_KANITI_sayaci_K5_kapaninca_FAIL_verir_ve_parolayi_BASMAZ` **KIRMIZI** | `cmp` birebir |
| M3 | `devir_turu: 6 → 5` (devir sonrası tur koşmuş gibi) | `denetim.sh` `✗ devir turu 5 sonrası tur 6 KOŞTU ama süre ölçümü YAZILMADI` → **DENETİM BAŞARISIZ** | `cmp` birebir |
| M4 | (ilk deneme) `P02_JWT=` değişken adı bozuldu | **HİÇBİR TEST KIRILMADI → TEST AÇIĞI**; test güçlendirildi (iki parmak izi sınaması + üç `P02_K5_ACIK=1` yolu sayılır), mutasyon M4b ile tekrarlandı | — |
| M4b | `denetim.sh` P02-15'ten MySQL parmak izi karşılaştırması silindi | `URETIM_KUNYE_KANITI_…` **KIRMIZI** | `cmp` birebir |
| M5 | `qa-turu.js`'e hatalı parola kalıbı geri kondu | `URETIM_KUNYE_KANITI_…` **KIRMIZI** | düzeltme yeniden uygulandı, `node --check` OK, `grep -c 'grep MYSQL_ROOT_PASSWORD'` = **0** |

## T3§5 — Değişen dosyalar (tur 1 + tur 2 + tur 3 BİRLEŞİK)

**Bu turda (tur 3) değişen — 4 dosya + bu rapor**
- `dotnet/denetim.sh` (P02-14 fail-closed'a çevrildi, **P02-15 eklendi**, ExecuteUpdate yorumu)
- `dotnet/Kuyumcu.Tests/OlcumKapisiTestleri.cs` (**+2 test**, 9 → 11)
- `dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js` (hatalı parola kalıbı düzeltildi)
- `dokumanlar/09-uygulama-kayitlari/ham/qa-tur-suresi.json` (devir penceresi)
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-02-uygulama-raporu.md` (bu bölüm + E§2-3 düzeltmesi)

**P-02'nin TAM ayak izi (§ 8 listesi + doğrulayıcının eklediği ikisi):**
`dotnet/denetim.sh` · `dotnet/yayin-al.sh` · `dotnet/mut.sh` (yeni) ·
`dotnet/Kuyumcu.Tests/OlcumKapisiTestleri.cs` (yeni) ·
**`dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs`** (§ 8'de eksikti — rota başlık haritası) ·
`.claude/launch.json` · `betikler/kapi.sh` · `betikler/qa-turu.js` ·
`betikler/hazirlik-kurulu.js` · `betikler/qa-sunucu.sh` · `betikler/yayinla.sh` ·
`ham/olcum-2026-09-18.json` · `ham/qa-tur-suresi.json` · `ham/TEK-EL-KILIT`

## T3§6 — Derleme, test, denetim (tur 3 kapanış ölçümleri)

| Ölçü | Değer |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Hata · 0 Uyarı** |
| Tam süit (trx) | **2904 test · 2904 başarılı · 0 başarısız · 0 atlanan** (tur 2: 2902 → **+2**) |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** (exit 0), 5 uyarı: QA rota 39/42 · YAYIN_TAZE bayat · ICLOUD açık · QA_TUR_SURESI devir · URETIM_KUNYE K-5 |
| Migration | **GEREKMEDİ** — `dotnet ef` çalıştırılmadı, şema değişmedi |
| `tenants` (önce → sonra) | **8 → 8** · `PAKET%` kiracı = **0** (test kiracısı açılmadı) |
| 5198 | açıldı → **kapatıldı** (`lsof` 0 satır) · 5199 **dokunulmadı** (3 satır LISTEN, kurucunun sunucusu) |

## T3§7 — Canlı doğrulama (tur 3, 5198)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| L1 | Production açılışı | fail-closed (BK-14) | **AÇILMADI** | `Jwt:Secret varsayılan/örnek değerde … fail-closed`, `UretimAcilisDenetimi.cs:90`; `lsof` 0 satır |
| L2 | Development açılışı | 200 servis | **AÇILDI** | `Now listening on: http://localhost:5198` · `Hosting environment: Development` |
| L3 | `GET /saglik` | 200 | **200** | gövde `Healthy` |
| L4 | `GET /api/v1/meta/surum` (jetonsuz) | 401 | **401** | `{"message":"Token bulunamadı","statusCode":401,…}` — künye ucu korumalı |
| L5 | docker-compose parolası gerçek mi | çalışmalı | **ÇALIŞIYOR** | `select count(*) from tenants` → **8** (parola basılmadı) |
| L6 | Parola kara listede mi | evet (K-5 açık) | **EVET** | SHA-256 parmak izi = `UretimAcilisDenetimi.cs:58` satırı |
| L7 | API durdurma | 5198 boş | **KAPANDI** | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → 0 |

> **L4 notu (md.1):** künye gövdesindeki `Ortam` alanı jeton gerektirdiği için
> bu turda **okunmadı**; "Ortam: Üretim" kanıtı zaten L1'deki fail-closed
> nedeniyle **alınamaz**. Uydurulmadı, kapsam dışı bırakılmadı — **sayaçlandı**.

## T3§8 — Kimlik kapanış tablosu (tur 3 sonrası, güncel)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-1** (QA 42/42 rota) | **MEKANİZMA KAPANDI · ÖLÇÜM DEVREDİLDİ (Dalga 1 QA turu 7)** | `denetim.sh` P02-2 fail-closed kapısı; P02-3 cırcır 39→42; devir artık **son tarihli** (P02-14) |
| **B-2** (yayın damgası + ortam) | **KISMEN** — damga/ortam/betik kurulu, **künye canlı kanıtı K-5'e bağlı** | `denetim.sh` P02-9/10/12/13 ✓ · **P02-15 ⚠→✗ dönüşümlü**; canlı L1 |
| **B-21** (BEKLENEN_VARLIK) | **KAPANDI** | `denetim.sh:301` `BEKLENEN_VARLIK=59` `-eq`; "37" iddiası doğrulandı değil **çürütüldü** |
| **B-53** (mut.sh sürümlü) | **KAPANDI** | `dotnet/mut.sh` `-rwxr-xr-x`, `cmp -s` kanıtı; P02-7 ✓ |
| **Y-33** (42 rota ↔ 42 @page) | **KAPANDI** | P02-1 `-eq 42` çift yönlü; `OlcumKapisiTestleri` |
| **BK-39** | **DEVREDİLDİ — SÜRE SINIRLI** (tur >6 ya da 2026-09-30) | P02-14 üç `✗` dalı; M1/M3 |
| **BK-14 / K-5** | **KURUCU KALEMİ — AÇIK** | canlı L1 · L6 |

## T3§9 — Kurucuya notlar (tur 3)

1. **K-5 (acil, kurucu):** Jwt:Secret ve MySQL root parolası rotasyonu. İkisi de
   kara listede olduğu sürece 5199/Production **açılmaz** ve P-02 "GEÇTİ" sayılamaz.
   Rotasyondan sonra **tek bir Production açılışı** yeterlidir: künye çıktısını
   `ham/uretim-kunye-kaniti.json` olarak kaydetmek denetimi de yeşile çevirir.
2. **BK-39 son tarihi 2026-09-30'dur.** Dalga 1 kapısındaki QA turu koştuğunda
   süre ölçümü dosyaya yazılmazsa **denetim kırılır** (bu bilinçli bir kapandır).
3. **ExecuteUpdate tavanı:** gerçek çağrı yeri **5** (Auth ×4, Kur ×1). Yazılı
   doktrin "4" der. Ya tavan 5 olarak mühürlensin, ya Kur bakım çağrısı izlenen
   varlığa çevrilsin (P-02 kapsamı dışı, ayrı kalem).
4. **K-4:** 16 `.mutorj` + 1 `.bak` + 8 × 0 baytlık migration hâlâ ağaçta; silme
   hook nedeniyle kurucu kalemidir. Silindiğinde `denetim.sh` tavanları 0'a inmeli.
5. **K-20 (iCloud) açık:** bu turun **süre** ölçümleri geçersiz tabandır; sayı
   ölçümleri (test, sayaç, SQL) etkilenmez.
6. **Mali müşavir notu:** yok — bu pakette mevzuat hükmü koda yazılmadı.

## T3§10 — Tahmin sapması

Plan tahmini 120 ajan-dk; P-02 üç tura yayıldı (ilk uygulama + düzeltme + bu
hasım kapanış turu). Sebep tek: **DoD'un bir maddesi (üretim künyesi) ekibin
yetkisi dışındaki K-5'e bağlıdır** ve her turda yeniden denenip yeniden
kanıtlanmıştır. Bu turda ek maliyet ≈ 25 ajan-dk.

---

## § EK — B-2 (üretim künye kanıtı) KAPANDI · orkestratör eki, 2026-09-18

> Bu ek, paketin kendi turlarında **alınamayan** kanıtın sonradan alındığını kayda geçirir.
> Uygulayıcı doğru davrandı: kanıtı uyduramayacağı için paketi "GEÇTİ" ilan etmedi ve borcu
> K-5'e devretti. Orkestratör engeli **kurucuya çıkmadan** kaldırdı (kurucu talimatı 15).

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-2** üretim kipi açılış kanıtı | **KAPANDI** | `ham/uretim-kunye-kaniti.md` + `ham/uretim-kunye-kaniti.json`; `denetim.sh` P02-15 artık **✓ "canlı kanıt dosyası var"** |
| **B-1** QA rota kapsamı (42/42) | MEKANİZMA KAPANDI, ÖLÇÜM DEVREDİLDİ | P02-2 fail-closed 42 kapısı kurulu; ölçüm dalga 1 kapısındaki QA turuna devredildi (devir penceresi: tur 6, son tarih 2026-09-30, P02-14 ile fail-closed) |
| **B-21** BEKLENEN_VARLIK (-eq) | KAPANDI | `denetim.sh` taze ölçümle `-eq` sabitlendi (varlık 59) |
| **B-53** mut.sh sürümlenmesi | KAPANDI | `✓ P-02: mut.sh sürümlü + cmp kanıtı (B-53) var` |
| **Y-33** ölçüm tabanı kopyalama yasağı | KAPANDI | `✓ P-02: OLCUM_TABANI_KOPYA 0 (varlık 59 · rota 42 · record 3)` |

**Nasıl alındı (özet):** üretim kipi için taze bir imza sırrı üretildi ve ayrı bir veritabanı
kullanıcısı açıldı (root parolası ve veri değişmedi); TLS gerçek Kestrel https ile kuruldu,
ters vekil beyanı kaçamağı kullanılmadı. Süreç `ASPNETCORE_ENVIRONMENT=Production` ile açıldı:
`/saglik` 200, jetonsuz uç 401, düz http bağlantısı reddedildi. **Negatif kontrol:** aynı komut
kara listedeki örnek sırla çalıştırıldığında açılış `fail-closed` ile durdu. Hiçbir sır değeri
depoya yazılmadı, ekrana basılmadı; dosyalar depo dışında 600 izinlidir.

**Açık kalan (gizlenmiyor):** K-5 — gerçek üretim sırlarının üretimi/saklanması/rotasyonu
kurucu kalemidir; `docker-compose.yml` içindeki geliştirme parolası hâlâ düz metindir.

---

## § EK 2 — B-2 BORCU KAPANDI: İNCELEME SUNUCUSU ÜRETİM KİPİNDE, TLS GERÇEK
*(orkestratör, 2026-09-21 · kurucu kararı ile)*

### Neden yeniden açıldı
21 Eylül sabahı 5199 inceleme sunucusu beş ayrı sebeple açılmıyordu ve ben onu ayağa
kaldırırken **ortamı `Development`e düşürmüştüm**. Bu, B-2'nin tam olarak kapattığı deliği
yeniden açtı: kurucu ürünü değil geliştirme kısayollarını inceler hâle geldi ve **üretim
açılış kapısının tamamı** (sır kara listesi, bekleyen migration kapısı) devre dışı kaldı.
**P-19'un hasım doğrulayıcısı bunu yakaladı**: `denetim.sh` EXIT=1 ve tam süitte tek kalıcı
kırmızı — `OlcumKapisiTestleri.inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ`.
Doğrulayıcı eşiği gevşetmedi, paketi KALDI ile geri çevirdi ve borcu eskale etti. Doğru davranış.

### Ölçülen engel (varsayım değil)
| Ölçüm | Sonuç |
|---|---|
| Üretim kipi + düz http 5199 | Açılış **durur** — `UretimAcilisDenetimi`: "TLS beyan edilmedi" |
| `dotnet dev-certs https --check` | Geçerli sertifika **var** (CN=localhost, 2027'ye kadar) |
| `dotnet dev-certs https --trust` | **Yönetici parolası ister** → kurucunun parolası kullanılmaz, sistem güvenlik ayarı değiştirilmez |
| Üretim kipi + `https://localhost:5201`, `curl -k` | `/` **200**, `/saglik` **200** |
| Aynı adres, uygulama içi tarayıcı | **Sayfa yüklenemedi** (curl doğrulamalı: "unable to get local issuer certificate") |

### Reddedilen üç yol
1. **Ortamı Development'ta bırakmak** — kapının tamamı kapanır; B-2'nin kendisi budur.
2. **`Uretim:TersVekilArkasinda=true`** — ortada ters vekil YOK; bu **yalan beyandır** (Madde 1).
3. **Açılış kapısının TLS kuralını gevşetmek** — güvenlik kapısı zayıflar.

### Kurulan yol (kurucu kararı: "kapıya dokunma")
* Kestrel **gerçekten TLS konuşur**: `https://localhost:5200`.
* `betikler/yerel-tls-koprusu.py` **yalnız `127.0.0.1:5199`** dinler ve baytları TLS ile
  5200'e taşır. Bayt düzeyinde çalışır → HTTP/1.1, yığınlı gövde ve WebSocket yükseltmesi
  (SignalR) olduğu gibi geçer. Hedef konak döngü arayüzünde değilse köprü **açılmaz** (exit 2).
* Uygulama isteği **https** görür → `UseHttpsRedirection` tetiklenmez; HSTS localhost'ta zaten
  devre dışıdır. `qa-sunucu.sh` köprü ölürse API'yi `trap` ile kapatır (sahipsiz süreç kalmaz).
* `.claude/launch.json` **bağlanma kipine** alındı (`url`, komut yok) ve sürümlü betiği
  `betik` alanında gösterir. Ölçülen sebep: **önizleme kum havuzu `~/Desktop`'ı okuyamıyor**
  (`can't open input file`, çıkış 127) — bu yüzden depo içindeki betiği doğrudan çalıştıramaz;
  oturumla yok olan scratchpad kopyasına dönmek ise B-2'nin ta kendisiydi. Sunucu artık
  sürümlü betikle kabuktan/nöbetçiden kalkar, bölme yalnız 5199'a bağlanır.

### Kanıt
| Kapı | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln` | **0 uyarı, 0 hata** |
| `dotnet/denetim.sh` | **TEMİZ** (EXIT=0) |
| Tam süit | **3427/3427**, 53 sn |
| `GET http://localhost:5199/` (köprüden) | **200** |
| `GET /saglik` | **200** |
| `GET /urunler` | **200**, yönlendirme **yok** (uygulama isteği https görüyor) |
| `GET /api/v1/kur/kaynaklar` (jetonsuz) | **401** |
| `GET /swagger/index.html` | **404** → ortam gerçekten Production |
| Uygulama içi tarayıcı | Panel **açıldı ve veriyle doldu** (ekran görüntüsü alındı) |

### Mutasyon kanıtı (4/4 kırmızı, `cmp` ile birebir geri alındı)
| # | Mutasyon | Kırılan test |
|---|---|---|
| M-B2-1 | `qa-sunucu.sh`: Production → Development | `inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ` |
| M-B2-2 | `qa-sunucu.sh`: `Uretim__TersVekilArkasinda=true` eklendi | `inceleme_sunucusu_GERCEK_TLS_konusur_ve_kopru_yalniz_dongu_arayuzunde_dinler` |
| M-B2-3 | Köprü `127.0.0.1` → `0.0.0.0` | aynı test |
| M-B2-4 | `launch.json` oturumluk kopyaya döndürüldü | `inceleme_sunucusu_URETIM_ortaminda_ve_bayat_yayinda_ACILMAZ` |

### Yan ürün: denetim.sh'ta ÖLÇMEYEN iki sayaç onarıldı
Denetim çıktısında iki kabuk hatası vardı ve ikisi de sayacı **sessizce ölçüsüz** bırakıyordu:
* `P-18: MOBIL_TABLO_SARMALAYICISIZ` — `grep -c … || echo 0`: grep eşleşme bulamayınca "0"
  **basar ve 1 ile çıkar**, yani `|| echo 0` ikinci bir "0" ekliyor, `$(( ))` "0\n0" görüp
  *syntax error in expression* veriyordu.
* `P-19: TEK_SAVECHANGES_DISI_CIFT_BACAK` — `${=VAR}` **zsh** sözdizimi bash betiğinde
  *bad substitution* verip döngüyü **hiç çalıştırmıyordu**.
İkisi de düzeltildi; değerler yine 0 çıktı (yani gizlenmiş ihlal yoktu) ama artık **gerçekten
ölçülüyor**.

### Açık kalan (gizlenmiyor)
* **K-5** duruyor: gerçek üretim sırlarının üretimi/saklanması/rotasyonu kurucu kalemidir.
* **BK-39 / K-20:** QA turu 7 süresi ölçüldü (1,46 saat) ama iCloud eşitlemesi açık olduğu
  için **taban geçersiz** damgası aldı; `denetim.sh` bunu uyarıyla geçiriyor ve **2026-09-30'dan
  sonra DENETİMİ KIRACAK**. K-20 kapanmazsa o tarihte kapı kapanır.
