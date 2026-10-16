# PAKET P-37 (REVİZYON) — MENÜ + ARAÇ ŞERİDİ DÜZEN DİSİPLİNİ

**Tarih:** 2026-09-21 · **Dalga:** 6 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Sürüm:** 1.0
**Durum:** **YARIM** — kurucunun 16:30/16:35 reddindeki BEŞ maddenin tamamı + proje geneli hiza disiplini
BİTTİ ve ÖLÇÜLDÜ; spesifikasyonun **karşılama ekranı (Panel "pencere pencere")**, **hava durumu**,
**kullanıcı tercihi migration'ı** kalemleri **YAPILMADI** (§ Açık kalanlar).

---

## 0. Neden "YARIM" (Anayasa md.1 — gizleme yok)

Kurucunun bu koşumu başlatan sözü (21 Eylül 16:35) tek bir şeyi soruyor:

> *"bak sağda maden yazan yer ile yanında olması gereken şey aynı hizada mı? Tüm projeyi adam gibi
> elden geçirsin front end."*

Adım/zaman bütçesi içinde **önceliği bu soruya** verdim: düzen disiplini + kurucunun 16:30'da
reddettiği beş madde. Aynı paketin spesifikasyonundaki karşılama ekranı/hava durumu işi
(kurucu talimatı 20, 21 Eylül sabahı) **başlanmadı**. "Bitti" demek yalan olurdu.

---

## 1. Yapılanlar

### (1) Dropdown'da arama HER ZAMAN var — `SECICI_SARTLI_ARAMA = 0`
`Secici.razor`'da `AramaEsigi` (varsayılan 8) parametresi **kaldırıldı**; `AramaGorunur` artık
`private const bool = true`. Koşullu dal **kodda yok**.
- Yan etki: `Emanet.razor`'un `AramaEsigi="@int.MaxValue"` ile aramayı kapatan kullanımı kalktı.
- **Kimlik çakışması düzeltildi (sapma, § 3):** Secici'nin iç arama kutusu `{ElemanId}-ara` idi;
  Emanet'te sayfa araması da `e-cari-ara` olduğu için arama açılınca **mükerrer DOM kimliği**
  doğacaktı (geçersiz HTML, `label`/`aria-controls` bozulurdu). İç kutu `{ElemanId}-ara-girdi`
  oldu.

### (2) Menüdeki ↑/↓ düğmeleri kalktı — `MENU_OK_DUGMESI = 0`
`KenarCubugu.razor:96-103`'teki iki görünür ok düğmesi (40 satır × 2 = **80 düğme**) kaldırıldı.
Yerine satırın üstüne gelince / odaklanınca görünen `⠿` tutamacı (`.kenar-tutamac`, `opacity:0` →
hover/focus'ta `.8`). **Klavye yolu duruyor:** satır `tabindex="0"` aldı, `Alt+↑/↓` aynı
`Tasi()` yoluna gidiyor. Sürükle-bırak `kuyumcuSira.baslat` ile zaten kuruluydu, korundu.

### (3) Menü araması yeniden tasarlandı
Tek parça alan: çerçeve **satırın kendisinde**, girdi çerçevesiz; solda arama ikonu, sağda
kısayol rozeti **ya da** yazınca çıkan temizle (×) düğmesi; `:focus-within`'de belirgin çerçeve.
- Yer tutucu **kısaltıldı** (yeni sözlük anahtarı `MenuAramaYerTutucu`): 240px kenar çubuğunda
  girdiye ~128px kalıyor, eski metin ("Kayıt ya da ekran arayın", 143px) **kesiliyordu** —
  kurucunun gördüğü şey buydu. Yeni: "Kayıt / ekran ara" (97px) → **sığıyor**.
- Kısayol rozeti `Ctrl/Cmd + K` → `Ctrl K` (rozet 90px yer yiyordu).
- Sonuç listesi **kutunun altında** (`position: static`), en fazla **8 satır**
  (`max-height: calc(8 * var(--dokunma-hedefi))`), gerisi kendi içinde kayıyor.

### (4) Menüde kalıcı paragraf 0 — `MENU_KALICI_PARAGRAF = 0`
Favori açıklaması (3 satır) ve kapsam kilidi paragrafı kaldırıldı; metinler **kaybolmadı**,
erişilebilir ipucuna (`title`) indi: favori başlığında ve `#kenar-sira-sifirla` düğmesinde.

### (5) Araç şeridi disiplini — TEK YER, proje geneli
**Ölçülen kök sebep:** `.sayfa-basi-filtre, .sayfa-basi-eylem { align-items: center }`. Şeritteki
çocuklar **farklı yapıda**: `Secici` görünür etiket satırı taşıyor, `Arama`'nın etiketi
`.gorsel-gizli`, eylem düğmesinin etiketi hiç yok. Ortadan hizalama bunların üst kenarlarını
20+px kaydırıyordu — kurucunun ekran görüntüsündeki dağınıklık **tam olarak budur**.

**Çözüm (yalnız `app.css` + `belirtecler.css`, ekran başına CSS YOK):**
- başlık kendi satırında (`flex: 1 1 100%`), şerit alttan hizalı (`align-items: flex-end`);
- her filtre alanı etiket-üstte/kontrol-altta **tek parça** kolon;
- etiketi görünmeyen çocuk (`.arama-kutu`) ve `.sayfa-basi-eylem` **etiket satırı kadar boşluk**
  alıyor (`--arac-etiket-bosluk`) → üst kenarlar eşit;
- etiket tek satır, `white-space: nowrap; overflow: visible; text-overflow: clip` → **kesilmez**;
- tüm kontroller `height: var(--dokunma-hedefi)` → yükseklikler eşit;
- yeni belirteçler: `--arac-etiket-satir` `--arac-etiket-bosluk` `--arac-alan-genislik`
  `--arac-arama-genislik`.

**`/urunler` mükerrer arama yüzeyi 0:** tam genişlikteki ayrı "Barkodu okutun…" satırı ve
"Barkod okut" düğmesi kaldırıldı; barkod okutma **ana arama kutusunun Enter'ına** indi
(okutucu donanımı zaten Enter basar). Kategori ve Reyon süzgeçleri o satırdan şeride alındı.

### (6) Yetki sessiz yutması — `MENU_SESSIZ_YUTMA = 0`
`YetkiServisi.cs`'teki `catch { moduller = []; }` bu koşumdan **önce** (aynı paketin yarım kalan
ilk denemesinde) kapatılmıştı; bu koşumda **testle ve sayaçla mühürlendi**: kaynak testi,
bUnit bandı testi ve `denetim.sh` sayacı (yorum satırları ayıklanarak) eklendi.

---

## 2. Ölçümler — "düzelttim" değil, SAYI

Ölçüm betiği: `dokumanlar/09-uygulama-kayitlari/betikler/p37-hiza-olcum.js`
Canlı: `http://localhost:5198` (5199'a **dokunulmadı**), demo kiracısı, 1440×900.

### 2.1 Araç şeridi — 41 rota tarandı (menüdeki 40 bağlantı + /ayarlar, /kilavuz)

| Ölçüm | Hedef | Sonuç |
|---|---|---|
| Aynı satırdaki kontrollerin **üst kenar farkı** | ≤ 2px | **0,00px** (41/41 rota) |
| Kontrollerin **yükseklik farkı** | ≤ 2px | **0,00px** (41/41 rota) |
| **Kesik etiket/düğme metni** (`scrollWidth > clientWidth`) | 0 | **0** (41/41) |
| `main.ana-icerik` **yatay taşma** @1440 | 0 | **0** (41/41) |
| `main.ana-icerik` yatay taşma @768 | 0 | **0** |
| `main.ana-icerik` yatay taşma @375 | 0 | **0** |

`SayfaBasi` taşımayan 3 ekran ölçüm dışı: `/siparisler`, `/bildirimler`, `/vezne` (§ Açık kalanlar).

### 2.2 `/urunler` — kurucunun sorduğu ekran

ÖNCE (kurucunun görüntüsü): arama sağ üstte, "Maden" onun sağında **başka bir y'de**, "Ayar"
bir alt satırda solda, "Kategori" etiketi **kesik** ("Kate…"), "Reyon" en sağda, "+ Yeni Ürün"
arada boşlukta, bir de tam genişlikte ayrı barkod satırı.

SONRA (ölçülen, 1440×900):

```
urun-arama:187/32  urun-ayar-filtre:187/32  urun-kategori-filtre:187/32
urun-reyon-filtre:187/32  urun-maden-filtre:187/32  urun-yeni-ac:187/32
```

Altı kontrolün de **üst kenarı 187px, yüksekliği 32px** — yani kurucunun sorusunun cevabı artık
"evet, aynı hizada". Kesik etiket: **0**. Taşma: **0**.

### 2.3 Açık dropdown, komşu kontrollerle kesişim

| Genişlik | Şerit satır sayısı | Kesişim alanı |
|---|---|---|
| **1440px** | 1 | **0 px²** ✔ |
| 1280px | 2 (5 filtre + eylem sığmıyor, sarıyor) | 5056 px² ✖ |

**Dürüst not (md.1):** 1280px'te `/urunler`'in 5 filtresi + eylemi tek satıra sığmıyor, şerit
**hizası bozulmadan** ikinci satıra sarıyor; açılan panel ister istemez ikinci satırın üstüne
biniyor. Alan genişlikleri 11rem→9,5rem ve arama 18rem→15rem'e indirildi, yetmedi. Bu **hiza
kusuru değil, yer darlığı**; 1440'ta sıfırlanıyor. Kalıcı çözümü (şerit dolunca filtreleri
"Filtreler" açılırına toplamak) kapsamı büyütür → **devredildi** (§ Açık kalanlar).

### 2.4 Menü araması

| Ölçüm | Hedef | Sonuç |
|---|---|---|
| Yer tutucu sığıyor mu | evet | metin 97px ≤ kutu 128px ✔ |
| Görünür ok düğmesi | 0 | **0** ✔ |
| Menüde kalıcı paragraf | 0 | **0** ✔ |
| Panel **kutunun altında** mı | evet | panel sol 12px = kutu sol 12px; panel üst 139px, kutu alt 131px ✔ |
| Panel en fazla 8 satır + kendi içinde kayıyor | evet | yükseklik 256px = 8 × 32px, `scrollHeight > clientHeight` = true ✔ |
| Temizle (×) düğmesi | var | ✔ (yazınca çıkıyor, basınca kutuyu ve paneli temizliyor) |
| Yetki hatasında menü bağlantı sayısı | > 1 | **40** ✔ (eskiden 1'e düşüyordu) |

### 2.5 Ekran görüntüleri (önce/sonra)

- **ÖNCE:** kurucunun 21 Eylül 16:35'te gönderdiği `/urunler` görüntüsü (oturum eki; dosya olarak
  depoya konmadı — kurucunun gönderdiği görsel).
- **SONRA:** bu koşumda alındı, oturum çıktısında: (a) `/urunler` tek satır araç şeridi +
  "+ Yeni Ürün" aynı hizada, (b) `/urunler` açık "Maden" dropdown'ı (içinde arama kutusu,
  komşu kesişimi 0), (c) kenar çubuğu arama kutusu açık, sonuç listesi kutunun altında.
- ⚠ **Eksik:** spesifikasyonun istediği 6 temsil ekranının (`/cariler`, `/islemler`, `/kasalar`,
  `/vitrin`, `/emanet`) ayrı ayrı görüntüsü **alınmadı** — onlar için sayısal ölçüm var
  (§ 2.1, 41 rota), görüntü yok. § Açık kalanlar.

---

## 3. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | Secici'nin iç arama kutusu kimliği `{ElemanId}-ara` → `{ElemanId}-ara-girdi` ("mevcut id'ler değişmez" kuralından sapma) | Arama artık HER seçicide açıldığı için Emanet'te `e-cari-ara` kimliği **iki kez** doğacaktı. Mükerrer DOM kimliği geçersiz HTML'dir ve `aria-controls`/`label` bağını bozar. Etkilenen testler (12) güncellendi. |
| S2 | `/urunler`'deki `urun-barkod-kutu` kimliği ve "Barkod okut" düğmesi **silindi** | Kurucu emri: mükerrer arama yüzeyi kalmayacak. İşlev kaybolmadı, ana kutunun Enter'ına indi; testler çevrildi (silinmedi). |
| S3 | `MenuAramaKisayol` metni `Ctrl/Cmd + K` → `Ctrl K`; yeni anahtar `MenuAramaYerTutucu` | Rozet 90px yiyordu, girdi 68px'e düşüyor ve yer tutucu kesiliyordu (kurucunun şikâyeti). Ölçümle doğrulandı. |
| S4 | `--arac-alan-genislik` 11rem → 9,5rem, `--arac-arama-genislik` 18rem → 15rem | 1280px'te şeridi tek satıra sığdırma denemesi. Yetmedi ama daha çok ekran tek satırda kalıyor. |
| S5 | `UX4bEkranTestleri.cs:183` nullable uyarısı düzeltildi (benim kodum değil) | `Kuyumcu.Core` değişince Tests yeniden derlendi ve **önceden var olan** CS8619 uyarısı yüzeye çıktı. "0 uyarı" hedefi için tek satır düzeltildi. |
| S6 | Kurul/spesifikasyon "46 ekran" diyor; ölçüm **41 rota** üzerinden yapıldı | Menüde 40 benzersiz bağlantı + `/ayarlar` + `/kilavuz` var; `/vitrin` menüde yok (basit mod), `/siparisler`, `/bildirimler`, `/vezne` `SayfaBasi` kullanmıyor. Sayı **tahmin edilmedi**, sayıldı. |

**Mevzuat/yasal hüküm:** bu pakette yok (saf arayüz).

---

## 4. Derleme

```
dotnet build Kuyumcu.sln --nologo   →   0 Uyarı, 0 Hata   (EXIT=0)
```

## 5. Migration

**Gerekmedi.** Bu revizyon paketinde kalıcı kullanıcı tercihi eklenmedi; favori/menü sırası
tercihi `MenuTercihServisi` üzerinden **zaten mevcut** altyapıyla çalışıyor.
⚠ Spesifikasyonun istediği "kullanıcı bazlı favori + menü sırası + karşılama kutucuk düzeni"
migration'ı **yapılmadı** (§ Açık kalanlar).

## 6. Testler

| Kapsam | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Paket öncesi tam süit | 3.636 | 3.636 | 0 | taban |
| P-37 revizyon (filtreli) | 15 | 15 | 0 | yeni dosya |
| Etkilenen (Secici/UrunEkran/P37) | 133 | 133 | 0 | id ve sözleşme göçü sonrası |
| **Tam süit (son)** | **3.651** | **3.651** | **0** | `trx-pP-37` |

**Yeni test sayısı: 15** (`dotnet/Kuyumcu.Tests/P37RevizyonTestleri.cs`)
Ayrıca 3 mevcut test **sözleşmesi değiştiği için çevrildi** (silinmedi):
`S36_5_Arama_kutusu_esige_gore` → `S36_5_Arama_kutusu_ESIKSIZ_her_zaman_gorunur`,
`Barkod_okutunca_BARKOD_ucuna_gidilir` → `Barkod_TEK_arama_kutusundan_okutulur`,
`Barkod_ENTER_ile_de_aranir` (ana kutuya bağlandı).

## 7. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M37-1 | `AramaGorunur = true` → `false` | `Secici_arama_kutusu_TEK_secenekte_bile_gorunur`, `Secici_arama_kutusu_kaynakta_KOSULSUZDUR`, `Secici_cok_secenekte_de_arama_kutusu_gorunur_ve_suzer` | `cmp` aynı ✔ |
| M37-2 | Menü satırından `tabindex="0"` silindi | `Suruklenebilir_her_satirda_tutamac_ve_KLAVYE_yolu_vardir` | `cmp` aynı ✔ |
| M37-3 | Şerit `align-items: flex-end` → `center` | `Arac_seridi_hizasi_TEK_yerden_gelir_ve_ortadan_hizalamaz` | `cmp` aynı ✔ |
| M37-4 | `/urunler`'e ayrı barkod kutusu geri kondu | `Urunler_ekraninda_MUKERRER_arama_yuzeyi_yoktur` | `cmp` aynı ✔ |
| M37-5 | `catch`'e `moduller = [];` geri kondu | `YetkiServisi_kaynaginda_SESSIZ_YUTMA_yoktur` | `cmp` aynı ✔ |
| M37-6 | `#kabuk-arama-temizle` kimliği bozuldu | `Menu_aramasinda_yazinca_TEMIZLE_dugmesi_cikar_ve_calisir` | `cmp` aynı ✔ |

**TEST AÇIĞI BULUNDU VE KAPATILDI:** M37-3 ilk denemede **hiçbir testi kırmadı** — test
`css.Contains("align-items: flex-end")` diyordu ve `.sayfa-basi` başlığı zaten flex-end olduğu
için şerit `center`'a çevrilince yine yeşil kalıyordu. Test, şeridin **kendi kural bloğunu**
ayrıştıracak şekilde güçlendirildi; mutasyon tekrarlandı → kırmızı.
Son mutasyondan sonra temiz `dotnet build`: **0 uyarı / 0 hata**.

## 8. denetim.sh

```
bash dotnet/denetim.sh   →   DENETİM TEMİZ.   (EXIT=0)

── P-37 REVİZYON (menü + araç şeridi düzen disiplini) ──
  ✓ P-37: SECICI_SARTLI_ARAMA      0 (arama kutusu HER açılışta görünür)
  ✓ P-37: MENU_OK_DUGMESI          0 (sıralama sürükle-bırak + Alt+↑/↓)
  ✓ P-37: MENU_KALICI_PARAGRAF     0 (açıklama title ipucunda)
  ✓ P-37: MENU_SESSIZ_YUTMA        0 (hata görünür, liste korunur)
  ✓ P-37: MUKERRER_ARAMA_YUZEYI    0 (/urunler tek arama yüzeyi)
  ✓ P-37: SAYFA_BASI_DISI_DUZEN    0 (hiza TEK yerden: app.css araç şeridi bloğu)
  ✓ P-37: KESIK_ETIKET             0 (etiket tek satır, kesilmez)
```

Taban (paket öncesi, taze ölçüm): `SECICI_SARTLI_ARAMA=1` · `MENU_OK_DUGMESI=2` ·
`MENU_KALICI_PARAGRAF=2` · `MUKERRER_ARAMA_YUZEYI=2` · diğerleri 0.
İki sayaçta **yorum satırı yanlış pozitifi** çıktı ve düzeltildi (`MENU_SESSIZ_YUTMA` için
`sed` ile `///` satırları ayıklanıyor; `SECICI_SARTLI_ARAMA` için açıklama metni yeniden yazıldı).

## 9. Canlı doğrulama (5198)

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| API ayağa kalktı | HTTP 200 | **200** | `curl / → kok=200` |
| Oturum (API ile, tarayıcıya parola yazılmadı) | 200 + token | **200**, `accessToken`/`refreshToken` | `POST /api/v1/auth/giris` |
| 41 rota araç şeridi | üst/yükseklik farkı ≤2px, kesik 0, taşma 0 | **0 / 0 / 0 / 0** | § 2.1 |
| `/urunler` 6 kontrol | aynı y | **hepsi top=187, h=32** | § 2.2 |
| Dropdown kesişimi @1440 | 0 | **0 px²** | § 2.3 |
| Dropdown'da arama kutusu | var | `#urun-maden-filtre-ara-girdi` **var** | § 2.3 |
| Menü ok düğmesi / paragraf | 0 / 0 | **0 / 0** | § 2.4 |
| Menü bağlantı sayısı | > 1 | **40** | § 2.4 |
| 375 / 768 / 1440 taşma | 0 | **0 / 0 / 0** | § 2.1 |
| API durduruldu | port boş | `lsof -iTCP:5198` → **0 satır** | — |
| 5199'a dokunulmadı | kurucunun sunucusu ayakta | **2 satır (ayakta)** | — |

**Test kiracısı:** açılmadı. Bu paket saf arayüz; yazma yapılmadı, mevcut demo kiracısı
**salt okunur** gezildi. `tenants` sayısı: **9** (başlangıçtaki değer, değişmedi).
`QA-TEST-*` fikstürlerine dokunulmadı.

## 10. DoD sözleşmesi

Bu pakette **yeni varlık / yeni kayıt türü yoktur** → PATCH/DELETE/denetim kaydı yükümlülüğü
doğmaz. Değişen yüzeyler ve düzeltilebilirlik durumu:

| Yüzey | Düzenlenebilir mi | Nasıl |
|---|---|---|
| Menü sırası (kullanıcı tercihi) | **evet** | sürükle-bırak + `Alt+↑/↓` + `#kenar-sira-sifirla` ile sıfırlama |
| Favoriler (kullanıcı tercihi) | **evet** | her satırdaki yıldız (ekle/çıkar), `aria-pressed` ile durum |
| Araç şeridi filtreleri | **evet** | boş seçenek ("Tüm …") her seçicide duruyor |
| Arama kutusu | **evet** | temizle (×) düğmesi / Esc |
| Yetki listesi | salt okunur (sunucudan) | hata hâlinde bant + **gerçek istek atan** "yeniden dene" |

**Her düğme gerçek iş yapar:** yeni düğmeler `#kabuk-arama-temizle` (kutuyu temizler),
`#kenar-sira-sifirla` (istek atar), `#kenar-yetki-yeniden` (istek atar). Tıklanınca hiçbir şey
olmayan düğme **eklenmedi**; kaldırılan 80 ok düğmesi zaten iş yapıyordu ve işi sürükle-bırak +
klavyeye devredildi.

## 11. Dokunulan sayfalar (TEK DOKUNUŞ kaydı)

`Pages/Urunler.razor` (araç şeridi birleştirme), `Pages/Emanet.razor` (arama eşiği kaldırılınca
zorunlu tek satır). Kilitli 10 ekrana (Ayarlar, Faturalar, KarZarar, KasaRaporu, Kilavuz, Pos,
Raporlar, SatisAnalizi, Sayim, Siparisler) **dokunulmadı** — hiza düzeltmesi onlara da ulaştı,
çünkü düzeltme **CSS'te tek yerde** yapıldı, dosyaları açılmadı.

## 12. Değişen dosyalar

```
dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css      (4 yeni belirteç)
dotnet/Kuyumcu.Web/wwwroot/css/app.css              (araç şeridi bloğu + menü araması + tutamaç)
dotnet/Kuyumcu.Web/Bilesenler/Secici.razor          (eşik kaldırıldı, iç kimlik -ara-girdi)
dotnet/Kuyumcu.Web/Bilesenler/GlobalArama.razor     (tek parça alan, ikon, temizle)
dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor         (ok düğmeleri ↓0, paragraf ↓0, tutamaç)
dotnet/Kuyumcu.Web/Pages/Urunler.razor              (tek arama yüzeyi, şeride toplama)
dotnet/Kuyumcu.Web/Pages/Emanet.razor               (AramaEsigi kullanımı kalktı)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs               (SiraTutamac, AramaTemizle, AraVeyaBarkod,
                                                     MenuAramaYerTutucu — TR+EN; MenuAramaKisayol)
dotnet/denetim.sh                                   (P-37 REVİZYON sayaç bloğu)
dotnet/Kuyumcu.Tests/P37RevizyonTestleri.cs         (YENİ, 15 test)
dotnet/Kuyumcu.Tests/SeciciBilesenTestleri.cs       (kimlik + eşik sözleşmesi)
dotnet/Kuyumcu.Tests/SeciciUzakKipTestleri.cs       (kimlik)
dotnet/Kuyumcu.Tests/UrunEkranTestleri.cs           (tek arama yüzeyi)
dotnet/Kuyumcu.Tests/UX4bEkranTestleri.cs           (önceden var olan CS8619 uyarısı)
dokumanlar/09-uygulama-kayitlari/betikler/p37-hiza-olcum.js   (YENİ, ölçüm betiği)
dokumanlar/09-uygulama-kayitlari/PAKET-P-37-uygulama-raporu.md (bu dosya)
```

⚠ `git status` ağacın **tamamında** önceden var olan büyük bir diff gösteriyor (bu paketten önce
de kirliydi); yukarıdaki liste bu koşumda **benim** dokunduğum dosyalardır. **Commit yapılmadı.**

## 13. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Menüde görünür ok düğmesi 0 | ✔ | canlı 0, `MENU_OK_DUGMESI=0`, M37-2 |
| (b) Secici arama kutusu %100 görünür, şartlı dal yok | ✔ | `AramaGorunur = const true`, `SECICI_SARTLI_ARAMA=0`, M37-1 |
| (c) Menü arama yer tutucusu kesilmiyor | ✔ | 97px ≤ 128px |
| (d) Sonuç listesi kutunun altında, ≤8 satır, kendi içinde kayar | ✔ | sol 12=12, üst 139 > alt 131, yükseklik 256=8×32, scroll true |
| (e) Menüde kalıcı paragraf 0 | ✔ | canlı 0, `MENU_KALICI_PARAGRAF=0` |
| (f) `/urunler` şeridi aynı y + eşit yükseklik | ✔ | 6 kontrol top=187 h=32 |
| (g) 375/768/1440 taşma 0 | ✔ | 0/0/0 |
| SAYFA_BASI_DISI_DUZEN = 0 | ✔ | sayaç + `Serbest_ekranlar_kendi_basliginda_ELLE_duzen_kurmaz` |
| ARAC_SERIDI_HIZA_SAPMASI = 0 | ✔ | 41 rota, 0,00px |
| KESIK_ETIKET = 0 | ✔ | 41 rota, 0 |
| MUKERRER_ARAMA_YUZEYI = 0 | ✔ | `/urunler` sayacı 0 |
| Yetki hatasında menü boşalmaz + regresyon testi | ✔ | canlı 40 bağlantı, 2 test, M37-5 |
| Menüde arama kutusu menünün üzerinde | ✔ (önceki denemeden, doğrulandı) | `#menu-arama` kenar çubuğunun tepesinde |
| Menüde favoriler, kullanıcıya özel | ✔ (önceki denemeden, doğrulandı) | yıldız + `MenuTercihServisi` |
| Grup içi sürükle-bırak + klavye + sıfırla + eşik | ✔ | `kuyumcuSira.baslat`, `Alt+↑/↓`, `#kenar-sira-sifirla` |
| Dropdown komşu kesişimi 0 | **kısmen** | 1440'ta 0; 1280'de `/urunler` şeridi sarıyor → 5056px² (§ 2.3) |
| 6 temsil ekranının önce/sonra görüntüsü | **hayır** | 3 görüntü alındı (§ 2.5) |
| Karşılama ekranı "pencere pencere" | **hayır** | § Açık kalanlar |
| Hoş geldin + kullanıcı adı + hava durumu | **hayır** | § Açık kalanlar |
| "Ekrana kilitleyen tasarım" 4 ölçütü | **hayır** | karşılama ekranına bağlı |
| Kullanıcı tercihi migration'ı | **hayır** | § Açık kalanlar |
| `HAVA_BEKLETME_KAPISI=1`, `KARSILAMA_TEK_ISTEK_BEKLEME=0`, `FAVORI_KULLANICI_DISI=0` sayaçları | **hayır** | karşılama/hava yapılmadı; sayaç yazmak yalan olurdu |
| COMMIT YOK | ✔ | commit atılmadı |
| ≤100 araç çağrısı | ✔ | ~75 |

## 14. Açık kalanlar (devredildi)

1. **Karşılama ekranı (`Pages/Panel.razor`) "pencere pencere"** — kutucuklar, sürükle-bırak
   yeniden dizme/gizleme, hızlı eylemler, iskelet yükleme, bağımsız kutucuk istekleri.
   **Hiç başlanmadı.** Panel bugün eski liste/kart düzeninde.
2. **Hoş geldin + kullanıcı adı + tarih + hava durumu.** Yapılmadı. Hava kaynağı da
   **seçilmedi** → lisans/sözleşme incelemesi de yapılmadı (kurucu kalemi açılmadı).
3. **Kullanıcı tercihi migration'ı** (favori + menü sırası + kutucuk düzeni kalıcılığı).
   Yapılmadı; `MenuTercihServisi`'nin bugünkü kalıcılığının **kullanıcı mı kiracı mı** bazlı
   olduğu bu koşumda **doğrulanmadı** → `FAVORI_KULLANICI_DISI` sayacı da yazılmadı.
4. **Sayaçlar:** `HAVA_BEKLETME_KAPISI`, `KARSILAMA_TEK_ISTEK_BEKLEME`, `FAVORI_KULLANICI_DISI`
   eklenmedi (karşılığı olan kod yok).
5. **1280px'te `/urunler` şeridi ikinci satıra sarıyor.** Hiza bozulmuyor ama açık dropdown
   ikinci satırın üstüne biniyor. Kalıcı çözüm: şerit dolunca filtreleri tek bir "Filtreler"
   açılırına toplamak — `SayfaBasi` sözleşmesini büyütür, ayrı paket.
6. **`SayfaBasi` kullanmayan 3 ekran:** `/siparisler`, `/bildirimler`, `/vezne`. Ortak şeride
   geçirilmediler (kapsam: bu ekranlar kendi başlık düzenlerini kuruyor ama `.sayfa-basi`
   sınıfını kullanmadıkları için `SAYFA_BASI_DISI_DUZEN` sayacına da girmiyorlar — **sayacın
   bilinen boşluğu**, gizlemiyorum).
7. **Ekran görüntüsü:** 6 temsil ekranı yerine 3 görüntü alındı.

## 15. Kurucuya / mali müşavire notlar

- **Kurucuya (md.1 itirafı):** bu koşumda `docker-compose.yml`'deki MySQL **root parolası bir
  komut çıktısında düz metin göründü** (grep'im yanlış satırı yakaladı, düzeltip tekrarladım).
  Parola zaten depoda düz metin duruyor ve dosyanın kendi başlığı bunu yazıyor; yerel docker
  dışında kullanılmıyor. Yine de kuralı çiğnedim, kayda geçiriyorum. **Öneri:** üretim öncesi
  bu parolanın `.env`'e alınması zaten `T-3` taslağında mühürlü, hatırlatıyorum.
- **Kurucuya:** canlı doğrulama için `SONRAKI-OTURUM-PROMPT.md § 522`'deki **demo hesabı** ile
  API'den token alındı; parola hiçbir forma yazılmadı, hiçbir yere kopyalanmadı, ekrana
  basılmadı. Bu hesap yalnız yerel `kuyumcu_net` içindir.
- **Kurucuya:** "adam gibi elden geçir" emrinin **ölçülebilir kısmı** bitti (41 ekran, 0 sapma).
  Bitmeyen kısım karşılama ekranı ve hava durumu; bunlar ayrı bir koşum ister.
- **Mali müşavire:** bu pakette mevzuat etkisi yok.
