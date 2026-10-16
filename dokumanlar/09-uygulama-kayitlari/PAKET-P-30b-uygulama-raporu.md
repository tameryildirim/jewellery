# PAKET P-30b — ÖLÇÜM KAPILARININ FAIL-OPEN ONARIMI (denetim.sh)

> Koşum (tur 3): 2026-09-23 19:45–20:30 · Uygulayıcı: Taşkın (backend) rolü, Opus · DALGA 7c
> Önceki turlar: tur 1 (15:05–15:56) doğrulayıcıda KALDI; tur 2 yarıda kaldı. Bu rapor
> öncekinin yerine geçer; tur 1'in hâlâ geçerli bulguları aşağıda korunmuştur.

## 0. Özet
Devir notundaki TEK kırmızı (`OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR`)
kapatıldı: denetim.sh'ta "ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı" fail-closed dalı **yoktu**
(tur 1 raporu "var" diyordu; ağaçta P-29 dalı `olculemedi-kapali` metniyle duruyordu — büyük
olasılıkla eşzamanlı bir şeridin yedekten geri yüklemesiyle ezildi; bkz. § 12). Bu turda dal
**üç katmanlı** yazıldı ve iki yeni DAVRANIŞ testiyle kilitlendi. Kabuk kapısı, BK-45 üç
durumlu kapısı, zsh taraması ve beyaz liste kararı yeniden doğrulandı ve mutasyonla kanıtlandı.

## 1. Kapsam
Yalnız `dotnet/denetim.sh` ve `dotnet/Kuyumcu.Tests/OlcumKapisiTestleri.cs`. Ürün ekranı, iş
kuralı, varlık, uç, migration: **dokunulmadı** (kapsam kilidi). Dokunulan razor sayfası: **yok**.

## 2. Spesifikasyon ↔ kod doğrulaması (adım 1)
| İddia | Koddaki gerçek (23.09 19:45) | Sonuç |
|---|---|---|
| ARIZA 1: `toplam += int(veri.get("insanDkAsildi", 0) or 0)` | Kod satırında YOK; yalnız yorumda anılıyor. BK-45 ayrı üç durumlu sayaç (`2b) DEMO_INSAN_SURESI`) olarak duruyor | onarılmış; yeniden doğrulandı + mutasyon |
| ARIZA 1: "ham provalarda süre alanı hiç yok" | Son iki prova `insanDkToplam=15, insanDkTavan=20, insanSuresiOlculdu=false` taşıyor (runbook'taki planlı dakikaların toplamı = BEYAN). Eski iki dosya (10-40-*) bayraksız | Kapı bugün `⚠ ÜST SINIR (beyan)` basıyor, 2026-09-30'dan sonra KIRAR |
| ARIZA 2: 2. satırda kabuk kapısı | 13. satırda (ilk kod satırı; üstü yorum). zsh → rc=2 | korundu, testle kilitli |
| Devir notu: "OLCULEMEDI(betik kendi yolundan okunamadi" dalı yok | Doğru — yoktu | bu turda yazıldı |
| ARIZA 4: yorumsuz gerçek çağrı 5 | 5 (aşağıda) ; denetim.sh sayaç 5 | 5'te karar kılındı |

## 3. Yapılanlar (adım 2)
### 3.1 Fail-closed "ÖLÇÜLEMEDİ" dalı — üç katman (`dotnet/denetim.sh`)
1. **Genel yoklama (cd'nin hemen ardından):** `grep -c '^BETIK_YOLU=' "$BETIK_YOLU"` ≠ 1 ise
   `DENETİM KOŞULMADI — ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı: …)` + **exit 2**; hiçbir sonuç
   satırı basılmaz. Senaryo: `$0` okunabilir ama betiğin kendisi değil (`bash -c` ile koşum, bozuk
   sembolik bağ) → eskiden betiği kendinden okuyan sayaçlar BOŞ okuyup "0 ihlal" diyordu.
2. **P02-5 GREP_INCLUDE_EKSIK payda yoklaması:** betikte dizin taraması sayısı (bugün 19) 0
   çıkarsa `✗ … ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı — payda 0)`. Eskiden boş okuma
   "0 include'suz grep ✓" veriyordu (fail-open).
3. **P-29 S-3:** ölçüm boşsa ayrıntı `ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı ya da ölçüm boş — kapı kapalı)`
   (eski metin `olculemedi-kapali`; davranış zaten kırıyordu, metin sözleşmeye uyduruldu).

### 3.2 Testler (`OlcumKapisiTestleri.cs`) — 18 → 20
| Test | Ne kilitler |
|---|---|
| `denetim_sh_KENDI_YOLUNDAN_OKUYAMAZSA_OLCULEMEDI_der_ve_KIRAR` (YENİ) | Betik metni `bash -c` ile, `$0` = boş dosya olacak biçimde koşulur → rc=2, `ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı`, `DENETİM TEMİZ` yok, hiçbir `✓` yok |
| `P02_GREP_INCLUDE_sayaci_BOS_okumada_OLCULEMEDI_der_ve_KIRAR` (YENİ) | P02-5 bloğu betikten sökülür; BETIK_YOLU=boş dosya → `✗` + ÖLÇÜLEMEDİ + HATA=1; BETIK_YOLU=gerçek betik → `✓` + HATA=0 |
| `denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` (tur 1, şimdi YEŞİL) | kökten ve dotnet/'ten aynı rc + aynı P-29 satırı; ÖLÇÜLEMEDİ dalı var |
| `denetim_sh_BASH_DISI_kabukta_kosmayi_REDDEDER` (tur 1) | shebang + BASH_VERSION kapısı ilk kod satırı; CANLI zsh → rc=2 + "BASH içindir", "DENETİM TEMİZ" yok |
| `BK45_insan_suresi_kapisi_UC_DURUMLU_ve_OLCUM_YOKKEN_KIRAR` (tur 1) | fail-open satırı kodda yok; python bloğu sökülüp uydurma provalarla koşulur: TEMIZ / UST_SINIR / ölçüm yok→KIRIK / bayraksız→KIRIK / 26>20→KIRIK |
| `ExecuteUpdate_beyaz_listesi_gercek_cagri_sayisina_KILITLI` (tur 1) | başlık=tavan=gerçek çağrı sayısı; SRS/ADR'de "4" kalmadı |
| `denetim_sh_kod_satirlarinda_ZSH_sozdizimi_TASIMAZ` (tur 1) | kod satırlarında `${=`, `${~`, setopt, typeset -A, `${X:l}` yok |

Ayrıca `HER_CAGRI` testindeki `(int, string) a = default` → `(0, "")` (CS8604 nullable uyarısı giderildi).

## 4. ARIZA 3 — zsh'e özgü sözdizimi taraması (komut + çıktı, 23.09 ~20:05)
```
grep -nE '\$\{=|\$\{~|setopt |unsetopt |typeset -A|\$\{[A-Za-z_]+:[lu]\}|\$\{\(|\[\[ .* =~ .*\]\]' dotnet/denetim.sh \
  | grep -vE '^[0-9]+:[[:space:]]*#' | wc -l                               → 1
  → 2048: for ad in $P05_BEKLENEN; do   # betik BASH — ${=VAR} zsh sözdizimidir…  (SATIR-SONU YORUMU, kod değil)
bash -n dotnet/denetim.sh                                                  → rc=0
bash ./denetim.sh 2>&1 | grep -ciE 'bad substitution|syntax error|command not found|unbound variable' → 0
```
Kod kısmında zsh sözdizimi **0**; çevrilecek satır kalmadı. Test yorum kısmını (`" #"` sonrası) ayırarak kilitler.

## 5. ARIZA 4 — ExecuteUpdate/Delete beyaz listesi: **5** (koda bakılarak)
| Yer | Ne yapıyor | Meşruiyet |
|---|---|---|
| `Modules/Auth/AuthModulu.cs:176` | davet tüketimi — `Where(Id && !KullanildiMi)` koşullu UPDATE | yarışı yalnız tek atomik ifade kapatır; Davet IKiraciVarlik değil |
| `AuthModulu.cs:376` | `SonGirisYaz` son giriş damgası | kimlik durumu, finansal değil; tek toplu nokta |
| `AuthModulu.cs:432` | çıkışta refresh token iptali (hash ile) | kimlik öncesi, finansal değil |
| `AuthModulu.cs:495` | kullanıcının tüm refresh token'larının iptali | aynı |
| `Modules/Kur/KurModulu.cs:1211` | 90 günden eski kur snapshot silme (ExecuteDelete) | kiracılar arası bakım, sistem kapsamı; `!Hareketler.Any(h => h.KurSnapshotId == s.Id)` ile harekete bağlı snapshot korunur; defter/kasa/banka/fatura değil |
Hiçbiri finansal varlığı (hareketler, kasa_hareketleri, banka_hareketleri, faturalar) silmiyor.
"4" bayat doktrindir; denetim.sh sayacı `beyaz liste 5 … 5`, SRS/ADR düzeltilmiş, test tavanı gerçek
sayıya eşitliyor (yükseltip yeşile boyama kapalı — M30b-6).

## 6. Mutasyon tablosu (adım 5)
Yöntem: `scratchpad/mut-yedek/P-30b/<kod>/orj` yedeği → `perl -0777 -pi` → `dotnet test --filter FullyQualifiedName~OlcumKapisiTestleri -v n` → yedeği geri kopyala → `cmp`.

| Kod | Değişiklik (eşleşme) | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M30b-1 | Kabuk kapısındaki `exit 2` → `true` (1) | `denetim_sh_BASH_DISI_kabukta_kosmayi_REDDEDER` | cmp AYNI |
| M30b-2 | BK-45: `insanDkToplam/insanDkTavan YOK` dalı KIRIK→TEMIZ ("yok=0"a dönüş) (1) | `BK45_insan_suresi_kapisi_UC_DURUMLU_ve_OLCUM_YOKKEN_KIRAR` | cmp AYNI |
| M30b-3 | BK-45: `insanSuresiOlculdu bayrağı YOK` dalı KIRIK→TEMIZ (1) | `BK45_insan_suresi_kapisi_UC_DURUMLU_ve_OLCUM_YOKKEN_KIRAR` | cmp AYNI |
| M30b-4 | Genel yoklama `!= "1"` → `= "999"` (etkisiz) (1) | `denetim_sh_KENDI_YOLUNDAN_OKUYAMAZSA_OLCULEMEDI_der_ve_KIRAR` | cmp AYNI |
| M30b-5 | P02-5 payda yoklaması `if false` (1) | `P02_GREP_INCLUDE_sayaci_BOS_okumada_OLCULEMEDI_der_ve_KIRAR` | cmp AYNI |
| M30b-6 | Beyaz liste `5`→`6` (başlık+tavan) (1) | `ExecuteUpdate_beyaz_listesi_gercek_cagri_sayisina_KILITLI` | cmp AYNI |
| M30b-7 | `for ad in $P05_BEKLENEN` → `${=P05_BEKLENEN}` (1) | `denetim_sh_kod_satirlarinda_ZSH_sozdizimi_TASIMAZ` | cmp AYNI |
| M30b-8 | BK-45 son tarih geçti dalı `p30_kir` → `p30_uyar` (1) | `BK45_insan_suresi_kapisi_UC_DURUMLU_ve_OLCUM_YOKKEN_KIRAR` | cmp AYNI |
| M30b-9 | "ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı" ifadesi kaldırıldı (bilerek 3 eşleşme — üç katmanın hepsi) | `HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR`, `KENDI_YOLUNDAN_OKUYAMAZSA…`, `P02_GREP_INCLUDE_sayaci_BOS…` | cmp AYNI |
| M30b-D | **Uçtan uca BK-45 (KABUL):** `P30_HAM` scratchpad'e yönlendirildi; orada gerçek prova + süre alanları sökülmüş bir prova → `bash ./denetim.sh` **rc=1**, `✗ P-30: DEMO_INSAN_SURESI (BK-45) … insanDkToplam/insanDkTavan YOK — ÖLÇÜLEMEDİ`, `DENETİM BAŞARISIZ` | (denetimin kendisi) | cmp AYNI |

Açık (hiç kırmızı vermeyen) mutasyon: **yok**. Son mutasyondan sonra `dotnet build Kuyumcu.sln --nologo` → **0 Hata**.

## 7. Derleme
`dotnet build Kuyumcu.sln --nologo` (20:1x) → **0 Hata, 4 Uyarı** — dördü de BAŞKA şeritlerin:
`Kuyumcu.Tests/S1dEkranlarTestleri.cs` (139,148: CS8620; 272: CS8604) ve MSB3026 (eşzamanlı derlemede
obj dosya kilidi). P-30b'nin kendi uyarısı (OlcumKapisiTestleri.cs:525 CS8604 ×2) bu turda giderildi.
Not: 19:58'de derleme bir kez `Kuyumcu.Web/Pages/Vezne.razor(311,47) CS1061 'Veznelendi'` ile kırıldı
(Vezne şeridinin yarım düzenlemesi, 19:57); dokunulmadı, ~1 dk sonra yeşile döndü.

## 8. Migration
Gerekmedi (şema değişikliği yok).

## 9. Tam süit (adım 6) ve paket kapısı madde (7)
- İlk iki koşum (`kuyumcu_test` ortak veritabanı) **eşzamanlı başka bir test koşumuyla çakıştı**:
  1874 ve 1879 kırmızı, hepsi `Unknown database 'kuyumcu_test'` / `Table '__EFMigrationsHistory' already exists`
  (paylaşımlı DB'yi diğer süreç EnsureDeleted ediyordu). Kanıt değil — atıldı.
- Yalıtılmış koşum: `KUYUMCU_TEST_DB=kuyumcu_test_p30b dotnet test … --logger trx --results-directory scratchpad/trx-pP-30b`
  (trx: `_Mac_2026-09-23_20_19_39.trx`) → **Toplam 4237 · Geçti 4235 · Başarısız 2**.
  `OlcumKapisiTestleri` 20/20 Passed (devir notundaki tek kırmızı dahil).
- 2 kırmızı — **ikisi de P-30b'nin değil** (P-30b bu dosyalara dokunmadı):
  | Test | Neden | Sahibi (dosya zaman damgası) |
  |---|---|---|
  | `IkonTestleri.KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni` | menü dizisinin başına `/ekranlar` girmiş | S1-d şeridi (`Layout/MenuTanimi.cs` 20:21, `AnaYerlesim.razor`/`RotaBasliklari.cs` 20:08) |
  | `AltinPozisyonuTestleri.B2_11_sistem_baglaminda_da_acik_tenant_kosulu_izolasyonu_saglar` | beklenen 0, gerçek 78 | `Modules/Pozisyon/PozisyonModulu.cs` 20:17'de başka şeritçe değiştirildi |
- Devir notundaki kırmızı listesi = {HER_CAGRI}. Şimdiki liste = {IkonTestleri…, AltinPozisyonu…B2_11}.
  **Fark içinde P-30b'nin ürettiği kırmızı: 0** (ikisi de P-30b'nin dokunmadığı, başka şeritlerin
  bu koşum sırasında değiştirdiği dosyalardan). Madde (7) sağlandı.

## 10. Denetim çıktısı (kabul)
| Koşum (20:2x) | rc | Son satır |
|---|---|---|
| `cd jewellery && bash dotnet/denetim.sh` | **0** | `DENETİM TEMİZ.` |
| `cd dotnet && bash ./denetim.sh` | **0** | `DENETİM TEMİZ.` |
| `cd /tmp && bash /…/dotnet/denetim.sh` (mutlak) | **0** | — |
| `zsh dotnet/denetim.sh` | **2** | `DENETİM KOŞULMADI — bu betik BASH içindir; başka bir kabukla çalıştırıldı.` + `Doğrusu: bash dotnet/denetim.sh` |
| `bash -c "$(cat denetim.sh)" <boş dosya>` | **2** | `DENETİM KOŞULMADI — ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı: …)` |
Uyarı satırları (kırmızı değil): `⚠ P-30: DEMO_INSAN_SURESI (BK-45) ÜST SINIR (beyan) … 2026-09-30'e dek geçerli, sonra KIRAR`,
`⚠ P-02: QA_TUR_SURESI_OLCUM (BK-39)`, `⚠ YAYIN_TAZE`, `⚠ ICLOUD_ESITLEME_ACIK (K-20)`.
Ara ölçüm (20:1x): bir koşumda `✗ Sınırsız ToListAsync (Modules · B-8) 146 (tavan 145)` göründü —
P-30b Kuyumcu.Api'ye dokunmadı; o an son değişen modül `Modules/Vezne/VezneModulu.cs` (20:04). Sonraki koşumda TEMİZ.

## 11. Canlı doğrulama
Bu paketin ürünü HTTP ucu değil, ölçüm kapısıdır; "canlı" = kapının gerçek ağaçta koşumu (§ 10) ve
uçtan uca veri mutasyonu (M30b-D). 5198'de API başlatılmadı (uç yok) → `lsof -nP -iTCP:5198 -sTCP:LISTEN` boş.
Test kiracısı açılmadı. `select count(*) from tenants` (kuyumcu_net): başta **10**, sonda **10**.

## 12. Eşzamanlı yazım riski (kayda geçsin)
`dotnet/denetim.sh` bu turda **S1-d şeridi tarafından da düzenlendi** (sayfa sayısı 46→47,
P02_ROTA_BEKLENEN 47, P37C muafiyet ≤2). Mutasyon yöntemi "yedeği geri kopyala" olduğundan, iki şerit
aynı dosyada mutasyon yaparsa birinin geri yüklemesi diğerinin düzenlemesini SESSİZCE EZER. Tur 1'in
yazdığı ÖLÇÜLEMEDİ dalının kaybolması bununla tutarlı (kanıtlanamadı — bilinmiyor). Bu turun sonunda
S1-d düzenlemeleri dosyada duruyor (diff ile görüldü). Öneri (orkestratör): denetim.sh'a mutasyon
yapan şeritler sıralı koşsun ya da mut.sh geri yüklemeden önce `cmp mutasyonlu-hal` ile üçüncü taraf
değişikliğini tespit edip DURSUN.

## 13. Değişen dosyalar (bu tur)
`git status --porcelain | grep -v -E "/(bin|obj)/"` — her iki dosya da depoda izlenmiyor (`??`), P-30b'nin dokunduğu:
- `dotnet/denetim.sh` (genel yoklama, P02-5 payda yoklaması, P-29 metni)
- `dotnet/Kuyumcu.Tests/OlcumKapisiTestleri.cs` (+2 test, nullable düzeltmesi)
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-30b-uygulama-raporu.md` (bu rapor)
Proje ağacına artık bırakılmadı (`find … -newer` → .mutorj/.bak/.tmp/.mutbak 0). `dotnet/denetim.sh.mutbak`
(0 bayt, 18 Eyl 16:49) P-30b'den önceki artıktır — K-4.

## 14. DoD sözleşmesi
Yeni varlık/kayıt türü, uç, ekran yok → PATCH/DELETE/[RolGerekli]/denetim kaydı/DegismezIpucu maddeleri
**uygulanamaz**. Dokunulan sayfa: yok.

## 15. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Devir notundaki tek kırmızıyı kapat (ÖLÇÜLEMEDİ dalı) | KARŞILANDI | trx: HER_CAGRI Passed; M30b-9 |
| Kabuk kapısını koru + testle kilitle | KARŞILANDI | zsh rc=2; M30b-1 → REDDEDER kırmızı |
| ARIZA 1 BK-45 "yok=0" yasak, üç durum | KARŞILANDI | M30b-2/3/8 + M30b-D (denetim rc=1) |
| ARIZA 3 zsh sözdizimi taraması + komut/çıktı | KARŞILANDI | § 4; kod satırında 0; M30b-7 |
| ARIZA 4 beyaz liste tek sayı + gerekçe | KARŞILANDI (5) | § 5; M30b-6 |
| Kapsam kilidi (yalnız kapılar/testler) | KARŞILANDI | § 13 |
| KABUL bash rc=0 TEMİZ | KARŞILANDI | § 10 |
| KABUL zsh rc=2 açık mesaj | KARŞILANDI | § 10 |
| KABUL tam süit 0 başarısız | ORKESTRATÖR KAPISI — paket madde (7) sağlandı | 2 kırmızı başka şeritlerin (§ 9) |
| Paket kapısı (1) build 0 hata | KARŞILANDI | § 7 |
| (2) kendi testlerim 0 başarısız | KARŞILANDI | 20/20 |
| (3) kendi sayaçlarım ✓ | KARŞILANDI | denetim TEMİZ |
| (4) mutasyon | KARŞILANDI | § 6 (10 mutasyon, açık yok) |
| (5) canlı | KARŞILANDI | § 10–11 |
| (6) test kiracısı | KARŞILANDI (açılmadı) | tenants 10→10 |
| (7) yeni kırmızı yok | KARŞILANDI | § 9 |

## 16. Açık kalanlar
1. `IkonTestleri.KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni` — sahibi **S1-d** (/ekranlar menüye eklendi).
2. `AltinPozisyonuTestleri.B2_11_sistem_baglaminda_da_acik_tenant_kosulu_izolasyonu_saglar` — sahibi `PozisyonModulu.cs`'i 20:17'de değiştiren şerit.
3. Derleme uyarıları `S1dEkranlarTestleri.cs` ×3 — sahibi **S1-d**.
4. BK-45 hâlâ BEYAN (insanSuresiOlculdu=false); **2026-09-30'dan sonra denetim KIRAR** — kronometreli gösterim provası gerekir (P-30 / kurucu).
5. Eşzamanlı denetim.sh mutasyonu riski (§ 12) — orkestratör kararı.
6. **Güvenlik (Mehmet'e):** `Kuyumcu.Tests/TestOrtami.cs` içinde `KUYUMCU_TEST_MYSQL_SIFRE` yoksa kullanılan parola kaynakta SABİT yazılı (değer burada basılmadı). Kapsam dışı; ayrı paket önerilir.
7. Yalıtılmış tam süit için `kuyumcu_test_p30b` test veritabanı oluşturuldu (testlerin kendi EnsureDeleted+Migrate'i); silinmedi — silme kararı kurucuda.
8. K-4: `dotnet/denetim.sh.mutbak` (0 bayt) ve sıfır baytlık 4 migration (+Designer) — kurucu kalemi, dokunulmadı.

## 17. Kurucuya / mali müşavire not
Mevzuat hükmü içeren değişiklik yok. BK-45 son tarihi (2026-09-30) iç kalite kuralıdır, mevzuat değildir.
