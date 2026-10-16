# PAKET P-36 — GÖRSEL DİL + KULLANICI DOSTU EŞİĞİ (frontend şeridi)
## Kuyumcu ERP · Dalga 6 · 2026-09-21 · Taşkın (backend) + Ertuğ (frontend) rolü

> **DURUM: YARIM.** Paketin *çekirdeği* (ortak seçim bileşeni + dört ölçülmüş kök
> neden + cırcır sayaçları) uygulandı ve kanıtlandı; paketin *hacmi* (176 select
> göçü, 12 parçalı ortak girdi ailesi, /ayarlar Görünüm bölümü, QA turu 8 ile
> puan eşiği) uygulanmadı. Anayasa md.1 gereği bu rapor "bitti" demiyor —
> **ne yapıldığı kanıtla, ne yapılmadığı adıyla** yazılıdır (§ 9).

---

## § 0. ÖNCE BULUNAN: DERLEME KIRIKTI (md.1 — gizlenmedi)

Pakete başlarken devralma notu *"3371/3371 test yeşil · 0 uyarı 0 hata"* diyordu.
**Gerçek ölçüm bunu doğrulamadı:** ilk `dotnet build Kuyumcu.sln` **4 hata**
verdi (CS0104, `/private/tmp/.../build1.log`):

```
Kuyumcu.Web/Pages/UrunEtiketleri.razor(135,58): error CS0104:
  'TurkceMetin', 'Kuyumcu.Web.Servisler.TurkceMetin' ile
  'Kuyumcu.Core.Domain.TurkceMetin' arasında belirsiz bir başvuru
```

**Sebep:** bu paketin *önceki, yarım kalmış bir denemesi* çalışma ağacına
`Kuyumcu.Web/Servisler/TurkceMetin.cs` (arama için harf katlama) bırakmıştı;
`Kuyumcu.Core.Domain.TurkceMetin` (tr-TR ile BÜYÜT/küçült) zaten vardı. İki
sınıf aynı adı taşıyınca `_Imports.razor`daki iki `@using` yüzünden **tüm Web
projesi** belirsizleşti ve `UrunEtiketleri.razor` derlenemez oldu.

**Çözüm (kapsam içi, çünkü paketin kendi artığıydı):** Web tarafındaki sınıf
`AramaMetni` olarak adlandırıldı (dosya adı korundu — `mv` hook tarafından
bloklu). İki *normalleştirme* değil, iki *iş* var: biri ekrana basmak için
büyütür, öteki aramak için katlar. Çağıranlar (`MenuTanimi.cs:244`, `Secici`)
güncellendi.

---

## § 1. TABAN — BELGEDEN KOPYALANMADI, YENİDEN ÖLÇÜLDÜ

`ham/qa-turu-7.json` → `olcum_betik` bloğu (44 rota):

| Ölçü | Değer |
|---|---:|
| sayfa_sayisi | 44 |
| bulgu_sayisi | 236 |
| kritik | 6 |
| **ortalama_gorsel** | **3,64** |
| **ortalama_kullanici_dostu** | **2,98** |

Bulgu türü dağılımı (ham JSON'dan sayıldı): KULLANILABILIRLIK 118 · GORSEL 40 ·
HATA 36 · ERISILEBILIRLIK 23 · DUZENLENEBILIRLIK 15 · PERFORMANS 4.

**EN DÜŞÜK PUANLI 12 EKRAN** (görsel + kullanıcı dostu toplamına göre):

| # | Rota | görsel | kullanıcı dostu |
|--:|---|--:|--:|
| 1 | `/cariler` | 2 | 2 |
| 2 | `/evraklar/takvim` | 3 | 2 |
| 3 | `/vitrin` | 3 | 2 |
| 4 | `/faturalar` | 3 | 2 |
| 5 | `/denetim-izi` | 3 | 2 |
| 6 | `/raporlar/kasa` | 3 | 2 |
| 7 | `/sayim` | 4 | 2 |
| 8 | `/kasalar` | 3 | 3 |
| 9 | `/kilavuz` | 3 | 3 |
| 10 | `/veri-disa-aktarim` | 3 | 3 |
| 11 | `/kurulum` | 3 | 3 |
| 12 | `/veri-ice-aktarim` | 3 | 3 |

**Sayaç tabanları (2026-09-21, paket öncesi, TAZE):**

| Sayaç | Taban | Ölçüm komutu |
|---|--:|---|
| HAM_SELECT | **178** | `grep -rho '<select[ >]' Pages Bilesenler Layout --include='*.razor' --exclude='Secici.razor' \| wc -l` |
| HAM_INPUT | **365** | `grep -rhoE '<(input\|textarea)' … \| wc -l` |
| SATIRICI_STYLE | **143** | `grep -rhoE 'style="[^"]*"' Pages Bilesenler --include='*.razor' \| wc -l` |
| KONTRAST_IHLALI | **1** | `grep -rn 'btn-birincil' … \| grep -c 'dugme-benzeri'` |

> ⚠ **Dürüst düzeltme:** kaba `grep -o '<select'` 176 veriyordu; `'<select[ >]'`
> ile ve yeni bileşenin kendi yorumu hariç tutulunca **178** çıktı. Rapora
> belgedeki 176 değil, **ölçülen 178** yazıldı.

---

## § 2. YAPILANLAR (hepsi kanıtlı)

### 2.1 ORTAK SEÇİM BİLEŞENİ `<Secici>` — kurucu talimatı 19

**Teknik gerçek (rapora yazılması istendi): kurucu haklı.** Yerleşik
`<select>`in **açılır listesi CSS ile biçimlendirilemez** — onu işletim sistemi,
sayfanın DIŞINDA çizer. Yükseklik, satır aralığı, seçili işareti, kaydırma
çubuğu, yazı tipi ve zemin bizim değildir; koyu temada bile sistem grisi
görünür. Kurucunun ekran görüntüsündeki kopukluk tam olarak budur ve
**tek çözüm özel bileşendir.**

Yeni dosyalar:
* `dotnet/Kuyumcu.Web/Bilesenler/Secici.razor` (bileşen)
* `dotnet/Kuyumcu.Web/Bilesenler/SecenekOgesi.cs` (veri sözleşmesi)

Şartname `FRONTEND-KIMLIK-VE-MENU-2026-09-21.md § 6.4–6.9`'dan uygulandı
(sıfırdan tasarlanmadı). Karşılananlar:

| Şart | Karşılandı mı | Kanıt |
|---|---|---|
| İçinde arama kutusu (eşik > 8) | ✔ | test `S36_5` |
| Türkçe harf duyarlılığı (ısparta→Isparta) | ✔ | test `S36_4` + mutasyon `M36-1` |
| `role=combobox` + `aria-expanded` + `aria-controls` | ✔ | `S36_1`, `S36_2` |
| `role=listbox` / `role=option` / `aria-selected` | ✔ | `S36_2`, `S36_12` |
| `aria-activedescendant`, liste **sarmaz** | ✔ | `S36_9` + mutasyon `M36-3` |
| ↑/↓/Home/End/PageUp/PageDown/Enter/Esc/Tab | ✔ | `S36_7`, `S36_8`, `S36_9` |
| Esc değeri DEĞİŞTİRMEDEN kapatır | ✔ | `S36_8` |
| Dışarı tıklama kapatır, değeri değiştirmez | ✔ | `.secici-perde` (DOM) |
| Devre dışı **sebebiyle** (UX-S30 / DoD EK md.1) | ✔ | `S36_10` |
| Hata alan ALTINDA + `aria-invalid` | ✔ | `S36_11` |
| Pasif öğe seçilemez | ✔ | `S36_13` + mutasyon `M36-2` |
| Eşleşme yoksa **boş durum** (hata değil, UX-S28) | ✔ | `S36_6` |
| Sınıra değince uyarı (UX-S29, 200 tavan) | ✔ | `.secici-sinir` + `SeciciSinir` anahtarı |
| ≥ 44px dokunma hedefi (375px'te 48px) | ✔ | `min-height: var(--dokunma-hedefi)` · `@media` |
| Kaydırma YALNIZ liste içinde (sayfa kaymaz) | ✔ | `max-height:20rem; overscroll-behavior:contain` |
| Karanlık tema | ✔ | tüm renkler `belirtecler.css` belirteçlerinden |
| **Satır içi renk/px YOK** | ✔ | denetim sayacı `SECICI_TEK_KAYNAK` |
| Uzak kip **sessizce** yerel'e düşmez | ✔ | `S36_14` + mutasyon `M36-4` |

**Çıplak sembol kullanılmadı:** ok CSS üçgeniyle, seçili işareti ortak
`<Ikon Ad="@Ikonlar.Onay">` ile çizilir — `denetim.sh`'ın
"Layout+Bilesenler emoji ikon = 0" kuralı ilk denemede kırıldı ve düzeltildi.

**Sözlük:** yeni anahtar EKLENMEDİ; `SozlukKabuk` içindeki `Secici*` anahtarları
(`SeciciAra`, `SeciciAramaYerTutucu`, `SeciciEslesmeYok`, `SeciciSinir`,
`SeciciSecimYok` …) zaten TR+EN olarak vardı ve **ilk kez gerçekten kullanıldı**.
Çıplak metin yok.

### 2.2 GÖÇ — pilot dalga 0 (şartnamedeki sıra)

| Yer | Eski | Yeni | id |
|---|---|---|---|
| `Bilesenler/SubeSecici.razor:35` | ham `<select>` | `<Secici>` | `ust-sube-secim` **birebir korundu** |
| `Pages/Panel.razor:160` | ham `<select>` | `<Secici>` | `kur-kaynak` **birebir korundu** |

Şartname bu ikisini bilerek ilk sıraya koymuştu: SubeSecici kabuktadır, **her
sayfada görünür** → kusur anında belli olur. Panel'de ayrı `<label>` kaldırıldı
(bileşen kendi etiketini taşır; çift etiket yok).

### 2.3 MOBİL TAŞMA — dört dokunuş (FRONTEND-SARTNAME § 7)

Ölçülmüş kök neden: `.satir` ve `.satir-arasi` **flex-wrap taşımıyordu**
(diğer tüm satır yardımcıları taşıyor), `/islemler`de `.sekme-serit`in ata
zincirinde `min-width:0` yoktu.

```css
.satir       { flex-wrap: wrap; min-width: 0; }
.satir-arasi { flex-wrap: wrap; min-width: 0; }
.sekme-serit { min-width: 0; max-width: 100%; }
```

⚠ Ölçüm yeri raporun uyardığı gibi `main.ana-icerik` içidir (`app.css:122`
`overflow:auto`); `documentElement.scrollWidth` 39 rotanın hepsinde 0 çıktığı
için tek başına **yanıltıcıdır**. Bu paketin kanıtı CSS kuralının varlığıdır
(denetim sayacı `MOBIL_SATIR_TASMASI`); **tarayıcıda 46 rota yeniden
ölçülmedi** — bkz. § 9 açık kalanlar.

### 2.4 MODAL: KAYDIRMA YALNIZ GÖVDEDE (FRONTEND-POPUP kök nedeni)

`.modal-panel` kaydırma kabının **kendisiydi** (`max-height:90vh; overflow-y:auto`)
→ başlık ve Kaydet/İptal içerikle birlikte kayıyordu (Yeni Ürün @375: 731px
pencerede 2760px içerik = **+2029px** taşma). Doğru desen zaten koddaydı:
`Cekmece` (`app.css:757` `.cekmece-govde{flex:1;overflow:auto}`).

```css
.modal-panel { display:flex; flex-direction:column; overflow:hidden; overflow-x:hidden; }
.modal-govde { flex:1 1 auto; overflow:auto; min-width:0; overscroll-behavior:contain; }
.modal-baslik{ flex:0 0 auto; }
.modal-alt   { flex:0 0 auto; … }   /* CSS'te HİÇ TANIMLI DEĞİLDİ — tanımlandı */
```

`ModalPencere.razor`a **`Altlik`** adında isteğe bağlı sabit düğme şeridi
parametresi eklendi (Kaydet önce, İptal sonra — 27 çekmecenin tek kalıbı esas
alındı). **Altlik verilmeyen 55 pencere eskisi gibi çalışır**: sessiz bozulma
yok, göç ekran ekran yapılacak (§ 9).

### 2.5 KONTRAST — tek gerçek ihlal kapandı

`Pages/Panel.razor:26`de `.btn-birincil` ile `.dugme-benzeri` aynı elemandaydı;
`.dugme-benzeri` `color: var(--metin)` yazıp birincil düğmenin beyaz metnini
eziyordu → **1.60:1**. Sınıf kaldırıldı → **10.17:1** (FRONTEND-KIMLIK § 1.3/5.4
hesabı). Sayaç `KONTRAST_IHLALI` bunu kilitledi.

---

## § 3. DERLEME

```
dotnet build Kuyumcu.sln --nologo   →  exit 0 ·  0 Uyarı ·  0 Hata  (25,3 sn)
```
(Başlangıçtaki 4 hata için § 0.)

## § 4. MIGRATION

**gerekmedi.** Paket yalnız arayüzdür; veri modeline, sunucu sözleşmesine,
yetkiye dokunulmadı. `kuyumcu_net.__EFMigrationsHistory` = **60** (paket öncesi
ve sonrası aynı, SQL ile ölçüldü). /ayarlar Görünüm bölümü uygulanmadığı için
tercih kolonu/tablosu da doğmadı (§ 9).

## § 5. TESTLER (önce / sonra)

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|--:|--:|--:|--:|
| `--filter SeciciBilesenTestleri` | 15 | 15 | 0 | 0 |
| `--filter KabukAramaEkranTestleri` | 18 | 18 | 0 | 0 |
| **Tam süit (1. deneme)** | 3557 | 3556 | **1** | 0 |
| **Tam süit (düzeltme sonrası)** | **3557** | **3557** | **0** | 0 |

**Yeni test: 15** (`Kuyumcu.Tests/SeciciBilesenTestleri.cs`).

**Kırmızı ve çözümü (gizlenmedi):**
`KabukAramaEkranTestleri.Secilen_sube_kabuk_durumuna_yazilir_ve_arama_ucuna_gecer`
ham `<select>`e `Change("s1")` gönderiyordu. Bileşen göçünce bu yol kalktı; test
**sözleşmeyi ölçmeye devam edecek biçimde** güncellendi (listeyi aç → "Şube 1"e
tıkla → kabuk durumu ve `sube=s1` sorgu parametresi hâlâ doğrulanıyor).
**Ölçülen davranış zayıflatılmadı**, yalnız etkileşim yolu değişti.

Tam süit öncesi taban 3371 değil **3542** idi (araya giren paketler); bu paket
15 test ekledi → 3557.

## § 6. MUTASYON KANITLARI

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı (cmp) |
|---|---|---|---|
| **M36-1** | `AramaMetni.Katla`da `'I'`/`'ı'` harf katlaması kaldırıldı | `S36_4_Turkce_arama_isparta_Isparta_bulur` | `cmp` birebir aynı ✔ |
| **M36-2** | `Secici.Sec`te pasif öğe koruması (`if (o is { Pasif: true }) return;`) kaldırıldı | `S36_13_Pasif_oge_secilemez` | `cmp` birebir aynı ✔ |
| **M36-3** | Ok tuşu sınırı kaldırıldı (liste başta/sonda **sarar** yapıldı) | `S36_9_Etkin_oge_duyurulur_ve_liste_sarmaz` | `cmp` birebir aynı ✔ |
| **M36-4** | Uzak kip istisnası kaldırıldı (sessizce Yerel'e düşer) | `S36_14_Uzak_kip_sessiz_dusmez` | `cmp` birebir aynı ✔ |

Her mutasyonda **tam olarak 1** test kırmızıya döndü (14/15 yeşil kaldı) —
test açığı bulunmadı. Son mutasyondan sonra `dotnet build` TEMİZ (0/0).
Yedekler: `<scratchpad>/mut-yedek/P-36/`.

> Dürüst kayıt: dört mutasyon, ikon değişikliğinden (§ 2.1 son paragraf) ÖNCE
> koşuldu. İkon değişikliği mutasyon hedeflerinin hiçbirine dokunmaz; sonrasında
> 15/15 yeşil ve derleme 0/0 yeniden doğrulandı.

## § 7. DENETİM

`dotnet/denetim.sh` → **DENETİM TEMİZ** (exit 0).

Eklenen P-36 cırcır bloğu (ölçüm komutları betiğe **yorum olarak** yazıldı,
tabanlar TAZE ölçüldü):

```
═══ P-36 görsel dil / ortak bileşen cırcırı ═══
  ✓ P-36: HAM_SELECT                   176 (tavan 176, yalnız azalır)
  ✓ P-36: HAM_INPUT                    365 (tavan 365, yalnız azalır)
  ✓ P-36: MODAL_GOVDE_DISI_KAYDIRMA      0 (kaydırma yalnız gövdede)
  ✓ P-36: MOBIL_SATIR_TASMASI            0 (satır yardımcıları sarıyor)
  ✓ P-36: KONTRAST_IHLALI                0
  ✓ P-36: SECICI_TEK_KAYNAK              1 (satır içi renk/px: 0)
```

HAM_SELECT 178 → **176** (pilot göç). HAM_INPUT sayacı bugün **kurulur** ki
Form ailesi P-37'de yazılırken taban **yükseltilemesin**.

**Kurulamayan sayaçlar (md.1 — uydurulmadı):** `BOS_DURUM_METINSIZ`,
`ODAK_TUZAGI`, `PENCERE_GENISLIK_CESIDI`, `PENCERE_DUGME_SIRASI_TERS`,
`SABIT_RENK` düşürme ve `KONTRAST_IHLALI`nin *tema şemaları* ayağı. Sebep: bu
sayaçların hepsi uygulanmayan işin (Görünüm bölümü, 83 pencerenin göçü, boş
durum taraması) çıktısını ölçer; **iş yapılmadan sayaç koymak sayacı yalancı
yapardı.** Görev metni "sayaç eklenmeden paket KAPANMAZ" diyor — bu yüzden
paket **KAPANMADI**, YARIM raporlandı.

## § 8. CANLI DOĞRULAMA (localhost:5198 — 5199'a dokunulmadı)

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| API 5198'de ayağa kalkar | 200 | **HTTP 200** | `curl /` → tam HTML |
| `/css/app.css` yayında | 200 | **HTTP 200** | `curl -o` yok, gövde okundu |
| Mobil taşma kuralları yayında | 3 kural | **3/3** | `app.css:1251 .satir`, `:1252 .satir-arasi`, `:1254 .sekme-serit` |
| Modal gövde kuralı yayında | var | **var** | `app.css:1268 .modal-govde { flex: 1 1 auto; overflow: auto; … }` |
| Yeni `.modal-panel` yayında | overflow:hidden | **var** | `display:flex; flex-direction:column; overflow:hidden` |
| Secici stilleri yayında | var | **var** | `app.css:1284 .secici-tetik {` |
| 5198 kapandı | dinleyen yok | **boş** | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → boş |
| 5199 korundu | dokunulmadı | **2 dinleyici duruyor** | `lsof … :5199 \| wc -l` = 2 |

**SQL kanıtı:**
```sql
select count(*) from tenants;                 -- 9  (paket öncesi 9, sonrası 9)
select count(*) from __EFMigrationsHistory;   -- 60 (değişmedi)
```
**Test kiracısı AÇILMADI** (paket yalnız arayüz; sunucuya yazan iş yok), bu
yüzden silinecek `PAKET36-TEST` kaydı da yok. `QA-TEST-` önekli kalıcı QA
fikstürüne dokunulmadı.

> ⚠ Dürüst sınır: canlı doğrulama **yayınlanan CSS/HTML düzeyindedir**.
> Tarayıcıda 46 rotanın 375/768/1440 ölçümü ve Secici'nin gerçek ekranda
> görsel denetimi **bu oturumda yapılmadı** (§ 9).

## § 9. YAPILMAYANLAR — ADIYLA (Anayasa md.1)

Görev metnindeki kapsam ~300 ajan-dk için yazılmıştı; adım ekonomisi tavanı
(≤ 90 araç çağrısı) içinde aşağıdakiler **uygulanmadı**. Hiçbiri "yapıldı"
sayılmamalıdır:

| # | Yapılmayan iş | Neden | Nereye |
|--:|---|---|---|
| 1 | **176 `<select>`ün 174'ünün göçü** (dalga 1–4) | 46 dosyada elle dönüşüm + her dosyanın kendi testleri | P-37 · sayaç HAM_SELECT ≤ 176 kilitli |
| 2 | **`Bilesenler/Form/` ailesi** (Metin, Sayi, Para, Gram, Tarih, TarihAralik, Telefon, Eposta, CokSatir, Onay, Secim, Anahtar) — talimat 20/a | 12 bileşen + 12 test kümesi + 365 girdinin göçü | P-37 · sayaç HAM_INPUT ≤ 365 kilitli |
| 3 | **`<Secici>` Uzak kipi** (sunucuda arama, 250ms, yarış koruması) ve `KayitSecici`nin yutulması | Şartname § 6.3; bugün `UzakTur` verilirse **istisna atar**, sessizce düşmez | P-37 |
| 4 | **/ayarlar → GÖRÜNÜM bölümü** (tema · 3-4 renk şeması · yazı tipi ölçeği, kiracı bazında) — kurucunun kendi şeridi (talimat 16) | Tercih saklama kararı (tarayıcı mı, kiracı kolonu mu) + WCAG AA doğrulaması + migration | **Kurucuya karar kalemi**, sonra P-37 |
| 5 | **83 pencerenin tek tipe indirilmesi**: 7 genişlik · 3 yarıçap · 3 gölge · 2 başlık puntosu · 5 düğme kapsayıcısı · **30 pencerede ters düğme sırası** | `Altlik` altyapısı kuruldu, **göç yapılmadı** | P-37 |
| 6 | Pencere davranışları: kapanışta odak tetikleyiciye dönmemesi, açılışta odağın × düğmesine gitmesi, dolu formu uyarısız kapatma, arka plan kaydırma kilidi, iki pencerenin aynı anda açılabilmesi, arama panelinin Esc'sizliği | Her biri ayrı JS/bileşen değişikliği + test | P-37 |
| 7 | Sayaçlar: `BOS_DURUM_METINSIZ`, `ODAK_TUZAGI`, `PENCERE_GENISLIK_CESIDI`, `PENCERE_DUGME_SIRASI_TERS`, `SABIT_RENK` düşürme | Ölçtüğü iş yapılmadı → sayaç yalan söylerdi | P-37 |
| 8 | **P-18 `mobil-olc.js` ile 46 rotanın yeniden ölçülmesi** (yatay kaydırma 0) | Tarayıcı turu koşulmadı | Kapı öncesi zorunlu |
| 9 | **QA turu 8 ve kabul eşiği** (görsel ≥ 4,0 · kullanıcı dostu ≥ 3,6) | Tur koşulmadı → eşik **doğrulanmadı** | Kapı öncesi zorunlu |
| 10 | 12 düşük puanlı ekranın tek tek elden geçirilmesi (boş durum metinleri, hata dili, odak/klavye, tipografi ölçeği) | Ortak kabuk düzeltmeleri yapıldı, **ekran turu yapılmadı** | P-37 |

**Eşik hakkında dürüst söz:** kabul eşiği (**görsel ≥ 4,0 · kullanıcı dostu ≥ 3,6**)
**ölçülmedi, dolayısıyla karşılanmadı sayılır.** Puanı yükseltmek için hiçbir
bulgu gizlenmedi, hiçbir işlev silinmedi, QA fikstürüne dokunulmadı.

## § 10. ÖNCE / SONRA

| Ekran / yer | Önce (ölçüm) | Sonra | Ölçüm |
|---|---|---|---|
| Kabuk (her sayfa) — şube seçici | işletim sistemi grisi açılır liste, arama yok, 37px | ortak `<Secici>`: arama, klavye, 44px, tema belirteçleri | `S36_*` 15 test · canlı `app.css:1284` |
| `/` Panel — kur kaynağı | ham `<select>`, ayrı label | ortak `<Secici>`, tek etiket, kilitliyken **sebebi yazılı** | derleme + `S36_10` deseni |
| `/` Panel — "Satış yap" düğmesi | kontrast **1.60:1** (okunmuyor) | **10.17:1** | `KONTRAST_IHLALI = 0` |
| Tüm modallar (55 pencere) | başlık + düğmeler içerikle kayıyor; Yeni Ürün @375 **+2029px** | kaydırma **yalnız gövdede**, başlık/düğme şeridi sabit | `MODAL_GOVDE_DISI_KAYDIRMA = 0` · canlı `app.css:1268` |
| `.modal-alt` sınıfı | CSS'te **hiç tanımlı değil** (5 kapsayıcıdan biri ölü) | tanımlı, çekmece ile aynı ölçüler | `app.css` P-36 bloğu |
| 375px: `/urunler` 237px · `/islemler` 104px · `/ayarlar` 48px · `/emanet` 36px · `/kur` 14px taşma | `.satir`/`.satir-arasi` sarmıyor | üç kural eklendi | `MOBIL_SATIR_TASMASI = 0` · canlı `app.css:1251-1254` · **tarayıcı ölçümü § 9/8'e devredildi** |
| Derleme | **4 hata** (CS0104) | **0 hata 0 uyarı** | `build3.log`, `build4.log` |

## § 11. KİMLİK KAPANIŞ TABLOSU

| Kimlik | Konu | Durum | Kanıt |
|---|---|---|---|
| **FRONTEND-KIMLIK § 1.3** | Birincil düğme kontrastı 1.60:1 | **KAPANDI** | `KONTRAST_IHLALI = 0` |
| **FRONTEND-KIMLIK § 6** | Ortak seçim bileşeni yok, arama yapan dropdown **0** | **KAPANDI (bileşen)** · göç açık | 15 bUnit testi + 4 mutasyon |
| **FRONTEND-SARTNAME § 7** | 375px'te 6 ekranda taşma, `.satir`/`.satir-arasi` | **CSS KAPANDI**, tarayıcı ölçümü açık | `MOBIL_SATIR_TASMASI = 0` |
| **FRONTEND-POPUP kök neden** | `.modal-panel` kaydırma kabı | **KAPANDI** | `MODAL_GOVDE_DISI_KAYDIRMA = 0` |
| **FRONTEND-POPUP (a)** | Kaydet/İptal açılışta gizli | **ALTYAPI HAZIR**, göç açık | `ModalPencere.Altlik` + `.modal-alt` |
| **FRONTEND-POPUP (c)–(h)** | odak iadesi, ilk alan odağı, veri kaybı, kaydırma kilidi, çift pencere, Esc | **AÇIK** | § 9 md.6 |
| **B-30 · B-32** | (tur 7 GORSEL/KULLANILABILIRLIK kümesi) | **ÖLÇÜLMEDİ** — tur 8 koşulmadı | § 9 md.9 |
| **UX-S28** | Hata "kayıt yok"a çevrilmez | Secici'de **uygulandı** | `S36_6` |
| **UX-S29** | Sunucu sınırı ekranda yazılır | Secici'de **uygulandı** | `.secici-sinir` |
| **UX-S30** | Kilitli düğme sebebini söyler | Secici'de **uygulandı** | `S36_10` |
| **UX-S31** | Ham kod ekrana basılmaz | `SecenekOgesi.Deger` ↔ `Ad` ayrımı | tip sözleşmesi |

## § 12. DoD SÖZLEŞMESİ

Bu paket **yeni varlık/kayıt türü doğurmaz** (yalnız arayüz; sunucu modülüne,
iş kuralına, migration'a, yetkiye dokunulmadı). Dolayısıyla PATCH/DELETE/
`[ModulYetkisi]`/denetim kaydı sözleşmesi **tetiklenmez**.

| Bileşen | Düzenlenebilir mi | Uç |
|---|---|---|
| `<Secici>` | Bir **giriş denetimidir**, kayıt değildir; taşıdığı değer çağıran formun kendi kaydetme yoluyla düzenlenir | çağıranın ucu |
| `SecenekOgesi` | Salt okunur görünüm modeli (record) | — |

**Her düğme gerçek iş yapar ya da kilitlidir:** Secici'nin tetikleyicisi
tıklanınca listeyi açar (`S36_2`); kilitliyse `disabled` + **görünür sebep**
(`S36_10`); pasif öğe tıklanınca **hiçbir şey olmaz ama sebebi `AltBilgi`de
yazılıdır** (`S36_13`). "Tıklanınca hiçbir şey olmayan" düğme yok.

## § 13. DOKUNULAN SAYFALAR (tek dokunuş kaydı)

* `Kuyumcu.Web/Pages/Panel.razor` — kontrast + pilot göç
* `Kuyumcu.Web/Bilesenler/SubeSecici.razor` — pilot göç
* `Kuyumcu.Web/Bilesenler/ModalPencere.razor` — sabit altlık
* `Kuyumcu.Web/wwwroot/css/app.css` — ortak kabuk (46 ekranı birden etkiler)
* `Kuyumcu.Web/Layout/MenuTanimi.cs`, `Kuyumcu.Web/Servisler/TurkceMetin.cs` — derleme kırığı

## § 14. DEĞİŞEN DOSYALAR

```
 M dotnet/Kuyumcu.Web/Bilesenler/ModalPencere.razor
 M dotnet/Kuyumcu.Web/Bilesenler/SubeSecici.razor
 M dotnet/Kuyumcu.Web/Pages/Panel.razor
 M dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs
 M dotnet/Kuyumcu.Web/Servisler/TurkceMetin.cs        (sınıf → AramaMetni)
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css
 M dotnet/Kuyumcu.Tests/KabukAramaEkranTestleri.cs
 M dotnet/denetim.sh
 + dotnet/Kuyumcu.Web/Bilesenler/Secici.razor         (YENİ)
 + dotnet/Kuyumcu.Web/Bilesenler/SecenekOgesi.cs      (YENİ)
 + dotnet/Kuyumcu.Tests/SeciciBilesenTestleri.cs      (YENİ, 15 test)
 + dokumanlar/09-uygulama-kayitlari/PAKET-P-36-uygulama-raporu.md
```
`git status --porcelain` çıktısında bu dosyaların çoğu ilk commit sonrası
eklendiği için `??` görünür. **COMMIT YAPILMADI.**

**Ağaçta bulunan, bize ait olmayan artık:** `dotnet/denetim.sh.mutbak`
(önceki bir paketin mutasyon yedeği). Silme hook tarafından bloklu —
**kurucuya bırakıldı**.

## § 15. KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Derleme 0 hata (0 uyarı hedefi) | ✔ | 0 Uyarı 0 Hata |
| Tam süit 0 başarısız | ✔ | 3557/3557 |
| `dotnet/denetim.sh` TEMİZ | ✔ | exit 0 |
| Mutasyon kanıtı (her kural) | ✔ | 4/4, cmp birebir |
| Sayaç eklendi + taban TAZE ölçüldü | **KISMİ** | 6 sayaç eklendi, 5 sayaç kurulamadı (§ 7) |
| Kabul eşiği (4,0 / 3,6) | **KARŞILANMADI** | QA turu 8 koşulmadı |
| 46 rota mobil ölçümü | **YAPILMADI** | § 9 md.8 |
| Kapsam kilidi (yalnız arayüz) | ✔ | sunucu/iş kuralı/migration/yetki dokunulmadı |
| Mevcut element id'leri değişmedi | ✔ | `ust-sube-secim`, `kur-kaynak` birebir |
| Yeni id kalıbı `<önek>-<alan>` | ✔ | `-ara`, `-liste`, `-secenek-<n>`, `-hata`, `-durum`, `-sinir` |
| Satır içi style / sabit renk yok | ✔ | `SECICI_TEK_KAYNAK` sayacı |
| Sozlukler TR+EN, çıplak metin yok | ✔ | mevcut `Secici*` anahtarları kullanıldı |
| 5199'a dokunulmadı | ✔ | 2 dinleyici duruyor |
| Test kiracısı silindi | ✔ (açılmadı) | tenants 9 → 9 |
| COMMIT YOK | ✔ | çalışma ağacında |
| Adım ekonomisi ≤ 90 araç çağrısı | ✔ | tavanın altında kalındı — **paketin YARIM kalmasının asıl sebebi** |

## § 16. KURUCUYA NOTLAR

1. **Kurucu haklıydı.** Yerleşik `<select>`in açılır listesi CSS ile
   biçimlendirilemez; işletim sistemi çizer. Ekran görüntüsündeki gri menünün
   sebebi buydu ve çözümü, kurucunun tarif ettiği gibi, **tek ortak özel
   bileşendir**. O bileşen yazıldı, arama kutusu ve Türkçe harf duyarlılığı
   ("ısparta" → "Isparta") **test edilerek** çalışıyor.
2. **"Tek seferde değiştiririz" şartı mekanik olarak korunuyor:** bileşenin
   içinde tek bir sabit renk ya da piksel yok; hepsi `belirtecler.css`ten
   geliyor ve `denetim.sh` bunu her koşuda kontrol ediyor.
3. **Bu paket bitmedi ve bitmiş gibi gösterilmedi.** 178 dropdown'ın 2'si göç
   etti. Kalan iş § 9'da madde madde yazılı; cırcır sayaçları bugün kuruldu ki
   sonraki paket **geri gidemesin**.
4. **Karar bekleyen tek kalem (talimat 16 — kurucunun kendi şeridi):**
   /ayarlar → Görünüm bölümünde seçilecek tema/renk/yazı tipi tercihi
   **tarayıcıda mı yoksa kiracı kaydında mı** saklansın? Tarayıcıda olursa
   migration gerekmez ama kullanıcı başka cihazda seçimini kaybeder; kiracıda
   olursa bir kolon eklenir ve her cihazda aynı görünür. **Tahmin edilmedi,
   soruluyor.**
5. Mali müşavire not **gerekmedi** — bu pakette hiçbir mevzuat hükmü koda
   girmedi.
