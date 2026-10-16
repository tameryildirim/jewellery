# PAKET P-00 — KAPSAM ENVANTERİ + KÖKEN TABLOSU + TRİYAJ KURALI — uygulama raporu

> **Dalga:** DALGA 0 (kod dokunulmaz) · **Tarih:** 2026-09-18 · **Sürüm:** 1.0
> **Kapsam kimlikleri:** B-50 · B-51 · B-58 · R-16 · Y-42
> **Çıktı:** `dokumanlar/09-uygulama-kayitlari/KAPSAM-ENVANTERI.md` (55.821 bayt, 334 satır)

## 0. Önkoşul ölçümü (durma-devam)
Önkoşul yok (planın ilk adımı). Okunması gereken kaynakların tamamı diskte **VAR** olarak doğrulandı:
`QA-TARAYICI-TURU-2026-09-16-tur4/tur5/tur6.md`, `ham/qa-turu-4/5/6.json`, `PAKET-S-3e/S-3f-uygulama-raporu.md`,
`HAZIRLIK-KURULU-3/4.md`, `EKRAN-ALAN-DOGRULAMA-2026-09-18.md`, `EKRAN-ALAN-ILISKI-KIYAS-RAPORU-2026-09-18.md`,
`KURUCUYA-KARARLAR-2026-09-16.md`, `YAPABILECEKLERIMIZ-2026-09-17.md`, `RAKIP-KIYAS-RAPORU-2026-09-16.md`,
`dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md`, `ham/muhur4/5/6.md`, `docs/RUNBOOK-PILOT-DAVET.md`.

## 1. Yapılanlar

1. **Kimlik → kaynak tablosu (172 satır).** PLAN § 12 / § 4 / § 6 tablolarından 158 kimlik makineyle çıkarıldı,
   § 13 ekinden Y-43 + Y-44 eklendi, triyajdan 12 kimlik doğdu. Her kimliğin kaynağı **gerçek bir dosya satırıdır**
   (dosya:satır + o satırdan alıntı); eşleme betikle yapılıp satır satır gözden geçirildi.
2. **Kaynağı plan oturumundan gelen kimlikler `[SENTEZ]` işaretlendi** (B-58, Y-42, R-15, D-8) — "eski raporda vardı"
   iddiası kurulmadı (Anayasa md.1).
3. **QA turu 4 + 5 + 6'nın 538 bulgusu tek tek kimliğe damgalandı** (`ham/triyaj-kayit.json`). Kaynak, belgenin
   düzyazısı değil `ham/qa-turu-{4,5,6}.json` makine kaydıdır; sayılar oradan **sayıldı**, kopyalanmadı.
4. **Y-42 triyaj kuralı yazıldı** (24 saat · dört kutu · damga · ekip oyu · kapsam freni) — KAPSAM-ENVANTERI.md § 7.
5. **R-16 (34-Ö paket-aile eşlemesi) satır satır doğrulandı** — § 8; "tahmindir" etiketi kalktı.
6. **B-50 (tur 4'ün "listelenmedi" denen 17 maddesi) listelendi** — DP4-05…DP4-21 = 17 madde, 17'si de kimliğe bağlandı.
7. **B-51 (S-3e açık kalanlarının S-3f'te kapanıp kapanmadığı) ölçüldü** — 8 kalemden **1'i KISMEN**, 7'si **AÇIK**;
   her biri kimliğe bağlandı (§ 5).
8. **Sayaçlar `dotnet/denetim.sh`'a cırcır olarak eklendi** (`# >>> P-00` / `# <<< P-00` bloğu, § 7 dosya çakışma
   haritasındaki blok kuralına uygun).

### Ölçülen sayılar (belgeden değil, dosyalardan)

| Ölçüm | Değer | Komut |
|---|---|---|
| Envanterdeki kimlik | **172** | `awk '/^## 2\./,/^## 3\./' KAPSAM-ENVANTERI.md \| grep -cE '^\| [BYRDK]-[0-9]+ \|'` |
| Kökensiz kimlik | **0** | aynı satırlar içinde ters tırnak içermeyenler |
| QA tur 4 bulgu / damgasız | 142 / **0** | `ham/qa-turu-4.json` |
| QA tur 5 bulgu / damgasız | 193 / **0** | `ham/qa-turu-5.json` |
| QA tur 6 bulgu / damgasız | 203 / **0** | `ham/qa-turu-6.json` |
| Tur 5'in devraldığı kalem | 148 (KAPANDI 14 · KISMEN 15 · AÇIK 119) | JSON `onceki_bulgular` sayımı |
| DALGA 0'da doğan yeni kimlik | **14** (B-59…B-70 + Y-43, Y-44) | `YENI_KIMLIK_DALGA` |

## 2. § Kimlik kapanış tablosu

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-50** — QA turu 4'ün kalan 17 maddesi listelenmedi | **KAPANDI** | `KAPSAM-ENVANTERI.md` § 6: DP4-05…DP4-21 = 17 madde, 17'si de kimliğe bağlı; kimliksiz kalan 0 |
| **B-51** — S-3e açık kalanlarının S-3f'te kapandığı ölçülmedi | **KAPANDI** | § 5: 8 S-3e kalemi + 6 S-3f kalemi tek tek ölçüldü (1 KISMEN, geri kalanı AÇIK) ve kimliğe bağlandı |
| **B-58** — Tur 4/5'in 119 açık + ≈59 yeni bulgusunun kimliklendirilmesi | **KAPANDI** | `ham/triyaj-kayit.json`: 538 bulgunun 538'i damgalı; `denetim.sh` → `BULGU_DAMGASIZ = 0` |
| **R-16** — 34-Ö paket-aile eşlemesi | **KAPANDI** | § 8: 5 satırın 5'i `MIMARI-KALAN-PAKETLER` ile karşılaştırıldı; 4'ü geçersiz, 1'i geçerli; karar yazıldı |
| **Y-42** — Triyaj (intake) kuralı | **KAPANDI (D0 ayağı)** | § 7 kuralı + `ham/triyaj-kayit.json` + `denetim.sh` `BULGU_DAMGASIZ` cırcırı. **Kapanış ayağı P-35'te** (PLAN § 12: Y-42 → P-00 + P-35) |

## 3. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| 1 | Kimlik sayısı 160 → **172** | Triyaj, tur 4/5/6'da tekrar eden 12 bulgu temasını (ham değer gösterimi, başarı bandı, erişilebilirlik, metin temizliği, boş durum, form paritesi…) mevcut hiçbir kimliğin kapsamadığını gösterdi. PLAN § 2'deki P-00 risk satırı bunu öngörüyor: **"gerileme değil, dürüst sayım"**. Yeni kimlikler yeni iş açmaz; var olan bulguları sahiplendirir. |
| 2 | Triyaj damgası `ham/surum.json` yerine `ham/triyaj-kayit.json`'a yazıldı | `surum.json` **paket** anahtarlıdır (paket → sürüm kararı) ve P-01'in alanıdır; bulgu düzeyinde damga oraya sığmaz. Kural metni (§ 7 md.3) ikisini birlikte tanımlar: bulgu damgası triyaj kaydında, paket/sürüm kararı `surum.json`'da. `surum.json`'a **dokunulmadı**. |
| 3 | Bulgu → kimlik eşlemesi kural tabanlı betikle yapıldı | 538 bulgunun elle eşlenmesi denetlenemez olurdu. Kural tablosu ve çıktı `ham/triyaj-kayit.json`'da açıktır; damgasız kalan 0'a inene dek kurallar elle gözden geçirilerek güçlendirildi. Eşleme **tema düzeyindedir**, bulgunun kapanışını iddia etmez. |
| 4 | Migration üretilmedi | Plan bloğu: **Migration: HAYIR**. Şema değişikliği gerekmedi. |
| 5 | B-22 ve B-52'nin kökeni zayıf | B-22 (/bildirimler basit mod menüsünde yok) için en yakın kanıt `ORKESTRATOR-DURUM.md:138`; B-52 (34-A denetim zenginleştirme) için `PAKET-34-uygulama-raporu.md:62`. İkisi de gerçek satırdır ama kimliğin sözcüğü sözcüğüne kaynağı değildir — tabloda o satırlar görünür, uydurma yapılmadı. |

## 4. Migration
**GEREKMEDİ.** Şema değişikliği yok, `dotnet ef` çalıştırılmadı. Uygulanmış migration sayısı değişmedi:
`SELECT COUNT(*) FROM __EFMigrationsHistory` → **49** (kuyumcu_net).

## 5. Derleme
`dotnet build Kuyumcu.sln --nologo` → **Oluşturma başarılı · 0 Uyarı · 0 Hata** (çıkış kodu 0).

## 6. Testler

| Kapsam | Toplam | Geçti | Başarısız | Not |
|---|---|---|---|---|
| Tam süit (`Kuyumcu.Tests`) | **2893** | **2893** | **0** | Süre 38,3 sn · trx: `scratchpad/trx-pP-00/` |

**Yeni C# testi yazılmadı (0).** Bu paket kod dokunmaz; mekanizması kabuk sayacıdır ve kanıtı `denetim.sh` mutasyonlarıdır
(aşağıda). Test sayısı önce 2893 → sonra 2893 (değişmedi) — bu dürüst sonuçtur, gizlenmiyor.

## 7. Mutasyon kanıtları (mekanizma sayaçları)

| Kod | Değişiklik | Kırmızıya dönen kapı | Geri alma (cmp) |
|---|---|---|---|
| M1-1 | `KAPSAM-ENVANTERI.md` içinde B-1'in Kaynak hücresi `—` yapıldı | `P-00: KIMLIK_KOKENSIZ 1 (beklenen 0)` → DENETİM BAŞARISIZ | `cmp` AYNI |
| M1-2 | `ham/triyaj-kayit.json` içinde bir bulgunun `kimlik` alanı `null` yapıldı | `P-00: BULGU_DAMGASIZ 1` → DENETİM BAŞARISIZ | `cmp` AYNI |
| M1-3 | Envanterden 5 kimlik satırı (K-22…K-26) silindi | `P-00: envanter kimlik sayısı 167 (en az 172)` → DENETİM BAŞARISIZ | `cmp` AYNI |
| M1-4 | `KAPSAM-ENVANTERI.md` `printf '' >` ile boşaltıldı | `P-00: kapsam envanteri belgesi YOK` → DENETİM BAŞARISIZ | `cmp` AYNI |

Dört mutasyonun dördü de kapıyı kırdı; **test açığı bulunmadı**. Son mutasyondan sonra `denetim.sh` → **DENETİM TEMİZ**.

## 8. denetim.sh
Yeni blok: `# >>> P-00` … `# <<< P-00` (yalnız bu paketin sayaçları; mevcut sayaçlara ve tavanlara dokunulmadı).
```
  ✓ P-00: kapsam envanteri belgesi                 var
  ✓ P-00: KIMLIK_KOKENSIZ                          0 (kimlik 172)
  ✓ P-00: envanter kimlik sayısı (>= 172)        172
  ✓ P-00: BULGU_DAMGASIZ                           0
  i P-00: YENI_KIMLIK_DALGA (raporlanır)          14

DENETİM TEMİZ.
```
Ölçüm komutları betiğe yorum olarak yazıldı. Taban (172) bugün **taze ölçüldü**, belgeden kopyalanmadı; cırcır yalnız yukarı çalışır.

## 9. Canlı doğrulama (5198, Development)
Bu paket uç/ekran/şema değiştirmediği için canlı doğrulama **gerileme kontrolü** olarak koşuldu (5199'a dokunulmadı):

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET http://localhost:5198/` | 200 | **200** (HTML gövde döndü) |
| 2 | `GET /api/v1/kur/guncel` (jetonsuz) | 401 | **401** `{"message":"Token bulunamadı", ... "istekKimligi":"01M2T342…"}` |
| 3 | `POST /api/v1/auth/giris` boş gövde | 400 + alan mesajları | **400** `{"message":["E-posta zorunludur","Şifre zorunludur"], "alanlar":{...}}` — 500 yok |
| 4 | SQL: `SELECT COUNT(*) FROM tenants` | 8 (değişmemeli) | **8** |
| 5 | SQL: `SELECT COUNT(*) FROM __EFMigrationsHistory` | 49 | **49** |

API yalnız kendi PID'imizle başlatılıp durduruldu; `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır**.
**Test kiracısı açılmadı**, bu yüzden silinecek kiracı yoktur; `QA-TEST-` fikstürlerine dokunulmadı (tenants 8 → 8).

## 10. Değişen dosyalar

| Dosya | Durum |
|---|---|
| `dokumanlar/09-uygulama-kayitlari/KAPSAM-ENVANTERI.md` | YENİ (55.821 bayt) |
| `dokumanlar/09-uygulama-kayitlari/ham/kapsam-envanteri.json` | YENİ (37.839 bayt — kimlik → kaynak makine kaydı) |
| `dokumanlar/09-uygulama-kayitlari/ham/triyaj-kayit.json` | YENİ (111.951 bayt — 538 bulgu damgası) |
| `dotnet/denetim.sh` | P-00 sayaç bloğu eklendi |
| `dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md` | tek satır kapsam freni kaydı |
| `dokumanlar/09-uygulama-kayitlari/PAKET-P-00-uygulama-raporu.md` | YENİ (bu dosya) |

`MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md`, `SONRAKI-OTURUM-PROMPT.md` ve `ham/surum.json`
**değiştirilmedi**. Commit yapılmadı, hiçbir git komutu çalıştırılmadı, dosya silinmedi/taşınmadı.

## 11. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Her kimlik → kaynak satır satır | ✅ | § 2 tablosu, 172/172 satır dolu; `KIMLIK_KOKENSIZ = 0` |
| Kaynağı bulunamayan kimlik işaretli | ✅ | `[SENTEZ]` etiketi (B-58, Y-42, R-15, D-8) |
| Tur 4 (148 devralınan) + tur 5 (193 / 119 açık / ≈59 yeni) satır satır açıldı | ✅ | `ham/triyaj-kayit.json`, § 3 tablosu; tur 6 da eklendi |
| S-3e § 12 ve S-3f § 11-12 açık kalanları listelendi | ✅ | § 5 |
| K-23…K-26 ve B-55/B-56 buradan doğar | ✅ | § 5 sonu + § 2'de kaynak satırları `PAKET-S-3f-uygulama-raporu.md` |
| Y-42 triyaj kuralı (24 saat, dört kutu, damga) | ✅ | § 7 |
| Kapsam freni ölçüldü ve ORKESTRATOR-DURUM'a yazıldı | ✅ | `YENI_KIMLIK_DALGA = 14`, ORKESTRATOR-DURUM son satır |
| Sayaçlar denetim.sh'a cırcır olarak eklendi | ✅ | § 8 |
| Mutasyon kanıtı + cmp geri alma | ✅ | § 7, 4/4 kırmızı |
| Tam süit 0 başarısız | ✅ | 2893/2893 |
| denetim.sh TEMİZ | ✅ | § 8 |
| Canlı doğrulama, 5199'a dokunulmadı | ✅ | § 9 |
| Test kiracısı silindi / fikstüre dokunulmadı | ✅ | kiracı açılmadı, tenants 8 → 8 |
| Kapsam kilidi (yalnız bu paketin kimlikleri) | ✅ | kod dosyası değişmedi; yalnız belge + denetim.sh sayaç bloğu |
| Migration | ✅ (gerekmedi) | § 4 |

## 12. Plan ile çelişki
**Tek çelişki:** plan P-00 DoD'u triyaj damgasını `surum.json`'a yazdırıyor; `surum.json` paket anahtarlı olduğu için
bulgu damgası oraya yazılamaz (yazılsaydı P-01'in alanı bozulurdu). Çözüm § 3/2'de: bulgu damgası `triyaj-kayit.json`,
paket/sürüm kararı `surum.json`. Kuralın özü (damgasız bulgu kapıyı kırar) **korunmuştur** ve sayaçla zorlanmaktadır.

## 13. Açık kalanlar
1. **Y-42'nin kapanış ayağı P-35'te** (plan böyle diyor): dalga kapılarında triyaj kaydının otomatik güncellenmesi ve
   `surum.json` paket damgasının bağlanması orada yapılacak.
2. **B-59 … B-70 henüz hiçbir pakete yazılı değil.** Bunlar tema kimlikleridir; dalga kapısında ekip oyuyla kutuya
   (mevcut pakete ek / yeni paket / 1.1 / kapsam dışı) yazılmalıdır. Öneri (oy bekliyor): B-59/B-63/B-65/B-68 → P-06 +
   P-14 şablon hattı · B-60/B-67/B-70 → P-06 · B-62 → P-24 · B-64 → P-25 · B-66 → P-06 · B-61 → P-13 · B-69 → P-16.
3. **Kaynağı zayıf iki kimlik** (B-22, B-52) — § 3/5.
4. Tur 6'nın 203 bulgusu da damgalandı ama planın § 12 tablosu tur 6'yı içermiyordu; tur 6 kaynaklı iki kimlik (Y-43, Y-44)
   dışındaki bulgular mevcut kimliklere düştü — **yeni kimlik doğurmadı**.

## 14. Kurucuya / mali müşavire notlar
1. **Kimlik sayısı 158 → 172.** Bu bir kötüleşme değil: aynı bulgular ilk kez tek tek sahiplendirildi. Kapsam freni
   (BK-33) artık ölçülüyor; DALGA 1'in hedefi ≤ 14 yeni kimlik.
2. **Mevzuat hükmü kodlanmadı.** Bu pakette hiçbir yasal hüküm koda yazılmadı; K-10 (KDV 23/e, KMTS), K-23 (ATOLYEDE/HAZIR
   geri dönüşü), K-24 (KMH negatif açılış), K-25 (stopaj oranı / 7 gün) kararları kurucuda/mali müşavirde durmaya devam eder.
3. **K-4 (depo temizliği) hâlâ kurucuda:** `Pages/` altındaki `.mutorj` artıkları ve 0 baytlık migration dosyaları grep tabanlı
   sayaçları kirletmeye devam ediyor; ekibin kancası silmeyi engelliyor.
