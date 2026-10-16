# PAKET P-42 — Yayın dizinini kalıcılaştır + yayın sonrası salt-okunur + tek kaynak yol

**Tarih:** 23 Eylül 2026 · 18:05–18:25 (gerçek ölçülen süre ≈ **1 sa 20 dk**, triyaj tahmini 2 sa)
**Rol:** Taşkın (backend) + Ertuğ (frontend/betik hattı) · Opus
**Dalga:** YAYIN HATTI + ÖLÇÜM ARACI (tur 8 triyajı Katman A + P-49)
**Durum:** **BİTTİ** — kabul koşullarının tamamı kanıtla karşılandı (bir tanesi ölçülmüş sapmayla, § Sapmalar).

> ⚠ Bu paket 23.09 18:50'deki planlı duruştan ÖNCE başlamış, **yarım bırakılmıştı**.
> Bu oturum o izi tamamladı: yazılmış olan mekanizma **ölçüldü**, eksik kalan
> canlı doğrulama + mutasyon kanıtı + bozuk yayın anlık görüntüsü kapatıldı.

---

## 0. Yol notu (Anayasa md.1 — varsayım yok)

Görev metni proje kökünü `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery`
diye veriyor. **Ölçüldü:** bu bir **sembolik bağ**tır →
`/Users/ramazanakkocan/ByGatech-yerel/jewellery`. İkisi aynı ağaçtır
(`readlink` çıktısı ile doğrulandı); bu raporda gerçek yol kullanılır.

---

## 1. Yapılanlar

### 1.1 Tek kaynak yol — `betikler/yayin-yolu.sh` (YENİ, 3.854 bayt)
Yayın dizini artık **tek yerde** tanımlıdır. Öncelik sırası:
1. `$KUYUMCU_YAYIN_DIZINI` (fikstür/test/geçici koşum — **hâlâ üstün**)
2. `$HOME/.kuyumcu-yayin` (**kalıcı varsayılan**, oturumdan bağımsız)

`kuyumcu_yayin_devral()` eski, oturuma özgü dizinden **bir kez kopyalar**
(taşımaz); silmeyi kurucu yapar (`rm`/`mv` kancası bloklar). Eski oturum kökü
**sabit yazılmaz**, `id -u` + joker ile bulunur — böylece bu dosyada da oturuma
özgü yayın yolu literali olmaz.

### 1.2 Tüketiciler tek kaynaktan okur
`yayinla.sh` · `qa-sunucu.sh` · `nobetci.sh` (2 yer: SON-DURUM satırı + 5199
kurtarması) · `yayin-butunluk.sh` · `kapi.sh` → hepsi `source .../yayin-yolu.sh`.
Beş kopya yol **kalmadı**.

### 1.3 `.claude/launch.json` yolu SABİTLEMEZ, betiğe DEVREDER
JSON kabuk betiği `source` edemediği için yol kopyalanmaz;
`"yayinYolu": "dokumanlar/09-uygulama-kayitlari/betikler/yayin-yolu.sh"` ile
sürümlü betiğe devredilir + gerekçe `yayinYoluNotu` alanında yazılıdır.

### 1.4 Yayın sonrası SALT-OKUNUR (fail-closed'in ikinci ucu)
`yayinla.sh`:
- **publish öncesi** `chmod -R u+w "$HEDEF"` (önceki kilidi aç — yoksa ikinci
  yayın "Access denied" ile ölür ve kurucu bir daha yeni sürüm göremez),
- **envanter + damga yazıldıktan SONRA** `chmod -R a-w "$HEDEF"`,
- ardından `[ -w "$HEDEF/wwwroot/index.html" ]` → yazılabilirse **YAYIN KİLİT RET, exit 1**.

Sıra bağlayıcıdır: kilit envanter/damgadan sonra kapanır, aksi hâlde kendi
damgamızı yazamayız.

### 1.5 Testler
- `dotnet/Kuyumcu.Tests/YayinYoluTestleri.cs` (YENİ, 7.733 bayt) — 3 test, **kaynak ağacı** okur (kabuk betikleri derlenmez, derleyici koruyamaz).
- `betikler/testler/yayin-butunluk-testleri.sh` — P-41'in 8 testine P-42'nin 2 testi eklendi (gerçek `dotnet publish -c Release`, 5199'a dokunmadan fikstür dizininde).

---

## 2. Kabul koşulları — karşılama tablosu

| # | Koşul | Durum | Kanıt (ölçülen) |
|---|---|---|---|
| 1 | `grep -rn '/private/tmp/claude-501' betikler/*.sh .claude/launch.json` → yayın dizini için satır kalmaz | ✅ | Tek satır kaldı: `qa-sunucu.sh:25 SIR_GECICI=…` — **sır** dizini geriye-uyum satırı (K-5'in işi), üstündeki yorum blokunda açıkça gerekçeli; kabul koşulunun istisnası |
| 2 | Yayın sonrası `test -w $HEDEF/wwwroot/index.html` BAŞARISIZ | ✅ | `dr-xr-xr-x` dizin · `test -w` başarısız · gerçek yazma denemesi: `permission denied` |
| 3 | `zsh betikler/yayinla.sh` ikinci kez koşunca başarılı biter | ✅ | `test_ikinci_yayin_kosumu_basarili` ✓ (gerçek ikinci Release publish) + canlıda 18:15 ikinci koşum `YAYIN HAZIR` |
| 4 | Sunucu yeni yoldan kalkar, `/kilavuz.html` → 200 | ✅ (boyut sapmalı) | 5198: `HTTP=200 BAYT=60845` (§ Sapma S-2). Bugün eski yayında: **404 0** |
| 5 | `SON-DURUM.txt` "Ekranında (5199) gördüğün sürüm" satırı yeni yoldan okunur ve tarih tutar | ✅ (mekanizma) | Satır kodu yeni yoldan koşturuldu → `23.09 18:15`, yayın damgasıyla **birebir**. Diskteki `SON-DURUM.txt` hâlâ `22.09 06:05` yazıyor: nöbetçi süreçleri **14:32:49'da**, P-42 değişikliğinden ÖNCE başlatılmış ve eski betiği bellekte koşuyor (§ Açık kalanlar A-1) |

---

## 3. Sapmalar ve gerekçeleri

**S-1 — Canlı doğrulama 5199'da değil 5198'de yapıldı.**
Kırmızı çizgi: "5199 portuna dokunma". **Ölçülen topoloji:** 5199'u
`yerel-tls-koprusu.py` (pid 12590) dinliyor, köprü 5200'e taşıyor, 5200'de
`dotnet Kuyumcu.Api.dll` (pid 12577) **eski, oturuma özgü** `…/scratchpad/qa-yayin`
dizininden koşuyor. `qa-sunucu.sh` köprüyü **ön planda** açtığı için tam koşum
5199'u yeniden bağlamak zorunda kalırdı. Bu yüzden aynı ikili, **yeni kalıcı
dizinden** 5198'de (TLS) ayağa kaldırıldı ve doğrulandı; sonra durduruldu
(`lsof -iTCP:5198` boş).

**S-2 — `/kilavuz.html` boyutu 60.885 değil 60.845 bayt.**
Kaynak dosya (`Kuyumcu.Web/wwwroot/kilavuz.html`) **bugün 18:02'de** başka bir
şerit (DALGA 7c, kılavuz içeriği) tarafından güncellendi: 60.885 → 60.845.
Triyajdaki 60.885 rakamı 14:30 ölçümüdür. Kapının iddiası "dosya servis ediliyor
ve boyut yayın çıktısıyla tutuyor"dur; ikisi de tutuyor (yayındaki dosya da
60.845). Tahmin değil, ölçüm.

**S-3 — 17:13'teki yayın anlık görüntüsü BOZUKTU; yeniden üretildi.**
İlk canlı denemede yeni dizinden kalkan API **açılışta öldü**:
`System.TypeLoadException: Could not load type 'Kuyumcu.Contracts.Kisit.KisitBeyaniYaniti'`.
**Ölçüm:** 17:13 publish'inin `Kuyumcu.Contracts.dll`'i bu tipi **içermiyordu**
(`strings … | grep -c` → 0), oysa tip kaynakta 13:44'ten beri var. Yani 17:13
publish'i **tutarsız** bir Release çıktısıyla üretilmiş (paralel koşan şeritler
sırasında). Düzeltme: **elle yama yok** — sürümlü betikle yeniden yayın
(`zsh betikler/yayinla.sh P-42-canli`, 18:15) → `strings … | grep -c` → 1, API açıldı.
**Kurucuya/orkestratöre not:** `yayin-butunluk.sh` envanteri *kendi ürettiği*
çıktının bütünlüğünü doğrular; **bayat ama tutarlı** bir derlemeyi yakalayamaz.
Bu boşluk P-42'nin kapsamı dışındadır, yeni bir kalem olarak açılmalı
(öneri: publish sonrası `--no-incremental` ya da açılış dumanı testi).

---

## 4. Derleme

```
dotnet build Kuyumcu.sln --nologo →  2 Uyarı · 0 Hata (RC=0)
```
**2 uyarının ikisi de P-42'nin dosyasında DEĞİL:**
`Kuyumcu.Tests/OlcumKapisiTestleri.cs(525,32)` ve `(525,52)` — CS8604,
**P-30b / DALGA 7c** şeridinin dosyası. Çakışma kuralı gereği dokunulmadı.

## 5. Migration

**Gerekmedi.** P-42 şema değiştirmez (yalnız kabuk betikleri + `launch.json` +
yeni test dosyası). `dotnet ef` çalıştırılmadı. Migration sayısı: 66 (değişmedi).

---

## 6. Test koşumları

| Kapsam | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| `--filter YayinYoluTestleri` | 3 | 3 | 0 | P-42 .NET kapısı |
| `betikler/testler/yayin-butunluk-testleri.sh` | 10 | 10 | 0 | P-41'in 8'i + P-42'nin 2'si (gerçek publish ×2) |
| Tam süit (`dotnet test … --logger trx`) | **4042** | **4039** | **3** | 3 kırmızının **hiçbiri P-42'nin dosyası değil** |

**Yeni test sayısı (P-42):** 5 (3 xUnit + 2 kabuk).
Devralma anındaki **16 kırmızı → 3**e indi (diğer şeritler kendi izlerini kapattı).

### Kalan 3 kırmızı — sahipleri (DOKUNULMADI, yaz-seri kuralı)
| Test | Sahibi |
|---|---|
| `OlcumKapisiTestleri.denetim_sh_HER_CAGRI_DIZININDEN_AYNI_KARARI_VERIR` | **P-30b / DALGA 7c** (devir notunda ismen yazılı) |
| `B1GorunumSecenekleriTestleri.B1_4_Kagit_daima_beyaz_yazdirma_kaynaklari_temaya_baglanmaz` | **B-1** (S-B şeridi, `Pages/Ayarlar.razor`) |
| `IadeEkranTestleri.Kaynak_secilince_KALAN_hak_ve_ORANTILI_tutar_gosterilir` | İade/PARA şeridi |

---

## 7. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| **M42-1** | `nobetci.sh:171` → `YAYIN="$YAYIN_DIZINI"` yerine eski `/private/tmp/.../qa-yayin` literali | `Betiklerde_OturumaOzguYayinYoluKalmadi` **KIRMIZI** + `Nobetci_YayinYolunuTekKaynaktanOkur` KIRMIZI (3'te 2 başarısız) | `cmp` birebir ✓ |
| **M42-2** | `yayinla.sh` sonundaki `chmod -R a-w "$HEDEF"` satırı silindi | `test_yayin_sonrasi_salt_okunur` **KIRMIZI** ("elle yama BAŞARILI oldu (kilit yok)") | `cmp` birebir ✓ |
| **M42-3** | `yayinla.sh` başındaki `chmod -R u+w "$HEDEF"` satırı silindi | `test_ikinci_yayin_kosumu_basarili` **KIRMIZI** (ikinci yayın çıkış=1) — `test_yayin_sonrasi_salt_okunur` YEŞİL kaldı (temiz ayrışma) | `cmp` birebir ✓ |
| **M42-4** | `launch.json` `yayinYolu` → `/Users/ramazanakkocan/.kuyumcu-yayin` elle sabitlendi | `LaunchJson_YayinYoluSabitDegil` **KIRMIZI** | `cmp` birebir ✓ |

Test açığı **yok**: her mutasyon en az bir testi kırdı, M42-2/M42-3 ayrı ayrı
hedef testlerini kırdı. Son mutasyondan sonra `dotnet build` 0 hata.

---

## 8. Denetim

```
bash dotnet/denetim.sh  →  RC=0 · DENETİM TEMİZ.
  ✓ P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM       47 (tavan 47 — cırcır YALNIZ azalır)
```
**Devir notundaki tek ihlal kapandı.** Ölçüm zinciri (gizlenmiyor):
- 18:06 ilk koşum: `✗ P-11 … 48 > tavan 47` — **P-42'nin izi değil**: P-42 hiçbir
  `.razor` dosyasına dokunmaz; sayaç yalnız `Pages/`, `Bilesenler/`, `Layout/`
  altındaki `.razor` dosyalarını okur.
- Aynı ihlal 16:51 (P-D1), 16:58 (P-52) ve 17:04 (S1-a) raporlarında da
  "benim değil" diye kayıtlı → paketler arası ortak miras.
- 18:22 son koşum: 47 — sahibi şerit bu arada düşürdü. P-42 **dokunmadı**.

---

## 9. Canlı doğrulama

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| Yayın üretimi (sürümlü betik, kalıcı yol) | `YAYIN HAZIR` + envanter + damga | ✅ | `YAYIN HAZIR (P-42-canli) 18:15 → /Users/ramazanakkocan/.kuyumcu-yayin · uyarı 0 · hata 0` · envanter **76 kayıt** doğrulandı · damga `f528b5210e7c…` |
| Salt-okunur kilit | `test -w` başarısız | ✅ | `dr-xr-xr-x` · `permission denied` (gerçek yazma denemesi) |
| Sunucu yeni yoldan kalkar | `/saglik` 200 | ✅ | 5198 TLS, ~2 sn |
| `/kilavuz.html` | 200 + gerçek gövde | ✅ | `HTTP=200 BAYT=60845`, gövde `<!DOCTYPE html><html lang="tr">…` |
| **Bugünkü arıza (karşılaştırma)** | — | **404 0** | Koşan 5200 süreci hâlâ eski `…/scratchpad/qa-yayin` dizininden servis ediyor (`lsof -d cwd` ile ölçüldü) — tur 8'de grup 1'in 6 rotasının düşme sebebi |
| `/kunye` | MİHENK gövdesi | ✅ | `<title>MİHENK</title>` |
| Sunucu durduruldu | 5198 boş | ✅ | `lsof -nP -iTCP:5198 -sTCP:LISTEN` boş |

**Kiracı:** P-42 kiracı **oluşturmadı** (kapsamı kabuk hattı).
`select count(*) from tenants` → **9** (başlangıç = bitiş). `PAKET%` önekli kiracı: **0**.
`QA-TEST-` fikstürlerine dokunulmadı.

**Canlı günlükteki hata (gizlenmiyor):** 5198 koşumunda
`Duplicate entry '…-MASAK_SIB_SURESI-2026-09-23' for key 'IX_is_calismalari_tenant_id_is_kodu_calisma_anahtari'`
satırları görüldü. **Sebep ölçüldü:** aynı anda 5200'de ikinci bir API örneği
koşuyordu; günlük iş **tekillik indeksi** ikinci örneğin aynı günlük işi yeniden
yazmasını engelledi — yani kapı **çalıştı**. Kalıcı tek örnek koşumda çıkmaz.

---

## 10. SQL kanıtı

```sql
select count(*) from tenants;                      -- 9   (başlangıç = bitiş)
select count(*) from tenants where ad like 'PAKET%';-- 0   (test kiracısı açılmadı)
```

---

## 11. Değişen dosyalar

| Dosya | Durum |
|---|---|
| `dokumanlar/09-uygulama-kayitlari/betikler/yayin-yolu.sh` | YENİ (tek kaynak) |
| `dokumanlar/09-uygulama-kayitlari/betikler/yayinla.sh` | u+w / a-w kilidi + tek kaynak |
| `dokumanlar/09-uygulama-kayitlari/betikler/qa-sunucu.sh` | tek kaynak (`source`) |
| `dokumanlar/09-uygulama-kayitlari/betikler/nobetci.sh` | 2 yerde tek kaynak |
| `dokumanlar/09-uygulama-kayitlari/betikler/yayin-butunluk.sh` | tek kaynak |
| `dokumanlar/09-uygulama-kayitlari/betikler/kapi.sh` | tek kaynak + kapı çıktı kökü `$HOME/.kuyumcu-kapi` |
| `dokumanlar/09-uygulama-kayitlari/betikler/testler/yayin-butunluk-testleri.sh` | +2 P-42 testi |
| `dotnet/Kuyumcu.Tests/YayinYoluTestleri.cs` | YENİ (3 test) |
| `.claude/launch.json` | yayın yolu betiğe devredildi (`git status`: ` M .claude/launch.json`) |
| `dokumanlar/09-uygulama-kayitlari/PAKET-P-42-uygulama-raporu.md` | bu rapor |

`dokumanlar/` ve `dotnet/Kuyumcu.Tests/` ağacı git'te **izlenmiyor** (`??`),
bu yüzden `git status --porcelain` yalnız `launch.json`'ı tek tek gösteriyor.
**COMMIT YAPILMADI.**

---

## 12. DoD sözleşmesi

P-42 **yeni varlık/kayıt türü doğurmaz** — veri modeli, uç, ekran, sözlük
anahtarı eklenmedi. Dolayısıyla PATCH/DELETE/`[RolGerekli]`/denetim kaydı
yükümlülüğü **konusuz**dur.

| Nesne | Düzenlenebilir mi | Gerekçe |
|---|---|---|
| Yayın dizini yolu | **Evet** — `KUYUMCU_YAYIN_DIZINI` ortam değişkeni her zaman üstündür; varsayılan tek dosyada (`yayin-yolu.sh`) değişir | Yapılandırma, kayıt değil |
| Yayın anlık görüntüsü (`$HOME/.kuyumcu-yayin`) | **Salt okunur — KASITLI** | DegismezIpucu: "Yayın elle düzeltilmez; yalnız sürümlü betikle yeniden üretilir (`zsh betikler/yayinla.sh`)". 23/09 12:05'te elle dokunuş tam bu yüzden sessizce geçmişti |
| Eski oturum yayın dizini | **Silme yolu yok (kasıtlı)** | `rm`/`mv` kancası bloklar; geri dönüşü olmayan silme **kurucunundur**. Devralma yalnız **kopyalar** |

**UX sözleşmesi:** P-42 ekran açmaz; UX-S21…S33 maddeleri **konusuz**.
Tek dokunuş kuralı: **hiçbir `.razor` sayfası açılmadı** (dokunulan sayfa: yok).

---

## 13. Açık kalanlar

- **A-1 (orkestratöre):** `nobetci.sh` süreçleri (pid 87257/87314/87402) **14:32:49**'da,
  P-42 değişikliğinden önce başlatılmış ve eski betiği bellekte koşuyor; bu yüzden
  `SON-DURUM.txt` hâlâ eski yolun tarihini (`22.09 06:05`) yazıyor. **Nöbetçiler
  yeniden başlatılınca** yeni yoldan okuyacaklar (mekanizma kanıtlandı: aynı kod
  yolu `23.09 18:15` üretiyor). Süreçleri durdurmak/başlatmak orkestratör kalemidir.
- **A-2 (kurucuya):** Kurucunun 5199 ekranı **şu an eski, eksik yayından** servis
  ediliyor (`/kilavuz.html` → 404). Kalıcı yayın hazır ve doğru
  (`$HOME/.kuyumcu-yayin`, 18:15). 5199/5200 ikilisinin yeniden başlatılması
  (kırmızı çizgi) kurucu/orkestratör kalemidir; ondan sonra `/kilavuz` 200 döner.
- **A-3:** Eski oturum yayın dizini
  (`/private/tmp/claude-501/-Users-ramazanakkocan-Desktop-…/scratchpad/qa-yayin`)
  **duruyor**; içeriği kopyalandı. Silmek kurucunun kalemidir (`rm` kancası bloklar).
- **A-4 (yeni kalem önerisi):** `yayin-butunluk.sh` **bayat ama tutarlı** derlemeyi
  yakalayamıyor (§ S-3). Açılış duman testi ya da `--no-incremental` önerilir.
- **A-5:** `YAYIN-ETIKET` hâlâ "DALGA 6 BİTTİ (22.09 06:05)" diyor; etiketi
  orkestratör günceller (P-42 kapsamı dışı).
- **Mevzuat:** Bu pakette **mevzuat hükmü yok** — mali müşavir notu gerekmedi.

## 14. Çakışma disiplini

`denetim.sh`, `OlcumKapisiTestleri.cs`, `/destek`, `docs/` kılavuz içeriği,
`kurulum.sh`, `KasaBakiyeKapisi.cs`, Kasa/Vezne/Maliyet/Rapor modülleri,
`Raporlar.razor`, `Gider.razor`, `Ayarlar.razor` **okundu ama yazılmadı**.
5199'a ve yayın anlık görüntüsüne **elle yama yapılmadı**; yayın yalnız
sürümlü betikle üretildi.
