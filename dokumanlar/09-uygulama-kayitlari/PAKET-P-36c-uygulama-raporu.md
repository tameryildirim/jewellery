# PAKET P-36c — GÖÇ: 174 SELECT + HAM GİRDİLER + 83 PENCERENİN TEK TİPE İNMESİ
## Kuyumcu ERP · Dalga 6 · 2026-09-21 · Taşkın (backend) + Ertuğ (frontend) rolü

> **DURUM: YARIM.** Paketin **birinci ayağı (select göçü)** ve **pencere
> davranışlarının ölçülmüş çekirdeği (odak tuzağı + kaydırma kilidi)**
> uygulandı ve kanıtlandı. Paketin **ikinci ayağı (ham girdi göçü)** ve
> **üçüncü ayağının görsel tek tipleştirmesi (genişlik/yarıçap/gölge/punto/
> düğme sırası)**, ayrıca **46 rota ölçümü** ve **QA turu + kabul eşiği**
> UYGULANMADI. Anayasa md.1 gereği bu rapor "bitti" demiyor: ne yapıldığı
> kanıtla, ne yapılmadığı adıyla § 9'da yazılıdır.

---

## § 0. ÖNCE ÖLÇÜLDÜ (belgeden kopyalanmadı)

Devralma notu "3371/3371 test yeşil" diyordu; **gerçek ölçüm 3608 test**
gösterdi (P-36b sonrası). Paket öncesi taban, denetim.sh'ın KENDİ komutlarıyla
yeniden ölçüldü:

| Sayaç | Paket ÖNCESİ | Ölçüm komutu |
|---|--:|---|
| HAM_SELECT | **176** | `grep -rho '<select[ >]' Pages Bilesenler Layout --include='*.razor' --exclude='Secici.razor' \| wc -l` |
| HAM_INPUT | **357** | `grep -rhoE '<(input\|textarea)' … --exclude-dir=Form \| wc -l` |
| `<Secici>` kullanımı | **2** (P-36 pilotu) | `grep -rho '<Secici[ >]' … \| wc -l` |
| Derleme | 0 hata / 0 uyarı | `dotnet build Kuyumcu.sln --nologo` |
| Test | 3608 / 3608 yeşil | `dotnet test` |

Serbest/kilitli ayrımı da ölçüldü: **176 selectin 26'sı KİLİTLİ ekranlarda**
(Faturalar 10 · Siparisler 5 · Raporlar 4 · Sayim 3 · Pos 2 · KasaRaporu 1 ·
KarZarar 1), **150'si serbest ekranlarda** (ayrıştırılabilen 148 blok).

---

## § 1. YAPILAN 1 — SELECT GÖÇÜ (90 serbest select → ortak `<Secici>`)

### 1.1 Yöntem: mekanik ama sessiz değil

Göç elle değil, **tek bir dönüştürücüyle** (scratchpad `goc.py`) yapıldı;
derleyici ve 3608 mevcut test kapı olarak kullanıldı. Dönüştürücünün
uyduğu kurallar:

* **Eleman kimliği BİREBİR korunur.** `<select id="d-sube">` →
  `<Secici ElemanId="d-sube">`. Kimlik değişirse mevcut test kırmızıya döner —
  bu bilerek yapılan bir kilittir (mutasyon **M36c-4** bunu kanıtlıyor).
* **Etiket kopyalanmaz, TAŞINIR.** `<label for="X">…</label>` satırı silinir,
  metni `Etiket` parametresine geçer (Secici kendi `<label for>`ünü çizer).
  İki etiket bırakılsaydı ekran aynı soruyu iki kez sorardı.
* **Boş seçenek metni** (`<option value="">—</option>`) `BosSecenekMetni`ne
  iner; seçenek listesinde aynı yerde, aynı metinle kalır.
* `@bind` → `@bind-Deger`, `@bind:after` → `@bind-Deger:after`;
  `value=@X @onchange=H` kalıbı → `Deger` + `DegerChanged` (H aynı
  `ChangeEventArgs` sözleşmesiyle çağrılır, imzası DEĞİŞMEZ).
* `required` → `Zorunlu`.

**Sonuç:** serbest ekranlarda **148 → 58** ham select. `<Secici>` kullanımı
**2 → 97**. Toplam HAM_SELECT **176 → 84** (84'ün 26'sı kilitli ekranlarda).

### 1.2 Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ)

`Banka · BankaEkstre · Cariler · DenetimIzi · Emanet · Evraklar ·
Fiyatlandirma · Gider · Hurda · IslemDefteri · Islemler · Kasalar ·
Kullanicilar · Kurulum · UrunAgaci · Urunler · Uyum · VeriIceAktarim · Vezne`
ve bileşenler `BelgeYukleyici · FiyatListesiUrunPaneli · MasakFormu ·
ModalPencere · Cekmece · Secici`.
**Kilitli ekranlara DOKUNULMADI** (Faturalar, KarZarar, KasaRaporu, Kilavuz,
Pos, Raporlar, SatisAnalizi, Sayim, Siparisler) — bulguları ilgili paketlere
devredildi (§ 9).

### 1.3 Ölçülen ve düzeltilen İKİ gerçek kusur (göçün ortaya çıkardığı)

1. **`e-cari-ara` kimlik çakışması (Emanet).** Sayfanın kendi sunucu-arama
   kutusu `#e-cari-ara` ile Secici'nin türettiği `{ElemanId}-ara` aynı kimliğe
   düşüyordu. Çözüm: bu ekranda Secici'nin **kendi arama kutusu kapatıldı**
   (`AramaEsigi="@int.MaxValue"`) — arama SUNUCUDA yapılır. Gerekçe UX-S22:
   aynı ekranda aynı sorunun iki cevabı olamaz.
2. **Ham `<select>`in sakladığı test açığı.** Eski testler `.Change("c1")` ile
   **listede olmayan** bir değeri de atayabiliyordu (tarayıcı select'i sessizce
   kabul ediyor). Ortak Secici gerçek listeden seçtiği için bu testler kırıldı;
   yardımcı listeyi BEKLER ve bulamazsa **açıklayıcı hatayla kırılır** — sessiz
   kabul kalktı (mutasyon **M36c-2** bu yolu kilitliyor).

### 1.4 Testlerin göçü — TEK KAPI

`Kuyumcu.Tests/SeciciYardim.cs` eklendi. Testler seçimi **yalnız** buradan
yapar (`Sec` · `Secenekler` · `ListeMetni` · `Deger()` · `Ad()`); ikinci bir
"seçiciye tıklama" kalıbı açılmadı (denetim.sh `SECICI_TEST_TEK_KAPI`).
76 çağrı yeri mekanik olarak bu kapıya taşındı, 13 dosyada `option`/`value`
okumaları `data-deger`/`.Ad()`'e çevrildi.

`Secici.razor`a **üç `data-deger` özniteliği** eklendi (tetik düğmesi + boş
seçenek + seçenek satırı). Bu bir görüntü değişikliği DEĞİLDİR: gövdeye giden
kodu DOM'da adreslenebilir yapar; **ekrana basılmaz** (B-25 / UX-S31 korunur).

---

## § 2. YAPILAN 2 — PENCERE DAVRANIŞLARI (ölçülmüş iki kök neden)

### 2.1 ODAK TUZAĞI — kapanışta odak tetikleyiciye DÖNMÜYORDU

**Ölçülen hata (görev metnindeki `ModalPencere.razor:73-77` /
`Cekmece.razor:93-97`):** `Acik=false` olunca panel `@if` bloğuyla **DOM'dan
çıkıyor**, `kuyumcuCekmece.kapat` **ancak ondan sonra** (`OnAfterRenderAsync`)
**kopmuş bir `ElementReference` ile** çağrılıyordu. Odak hiçbir zaman
tetikleyiciye dönmüyordu ve hata `catch { }` ile **yutuluyordu** (md.1 ihlali).

**Düzeltme:** kapatma yolu TEKe indirildi (`KapatIstegi`), odak geri verme
**panel hâlâ DOM'dayken** onun İÇİNDEN çağrılıyor, yutma yalnız
`JSDisconnectedException` ile sınırlı:

```csharp
private async Task KapatIstegi()
{
    await OdakGeriVer();        // panel HÂLÂ DOM'da
    await Kapat.InvokeAsync();
}
```

Esc yolu da aynı kapıya bağlandı (`TusYakala → KapatIstegi`). İkinci bir
erişilebilirlik mekanizması AÇILMADI.

### 2.2 ARKA PLAN KAYDIRMA KİLİDİ — yoktu

`cekmece.js` içine **sayaçlı** kilit kondu (`__kuyumcuPencereSayisi`);
`body.pencere-acik` sınıfı sayaç 0'a inince kalkar. İç içe/ardışık iki panel
birbirinin kilidini açmaz.

⚠ CSS'te **`overflow-y: hidden`** kullanıldı, `overflow: hidden` DEĞİL:
yatay taşma gizlenmez (Anayasa md.1 / denetim.sh `P-18: MOBIL_TASMA_GIZLEME`).
Bu, denetim koşumunda **gerçekten yakalandı** ve düzeltildi — gizlenmedi.

---

## § 3. DoD SÖZLEŞMESİ (Bertan koşulu)

Bu paket **yeni varlık/kayıt türü ÜRETMEZ**: veri modeli, uç, yetki ve denetim
kaydı tarafında **tek satır değişiklik yoktur**. Değişen yalnız **çizen**
katmandır (Razor bileşenleri + test yardımcısı + CSS/JS).

| Varlık | Düzenlenebilir mi | Uç | Not |
|---|---|---|---|
| — | — | — | Bu pakette yeni varlık YOK |

Mevcut ekranların düzenleme yolları **değişmedi**: göç edilen her seçici aynı
alanı aynı uca aynı gövdeyle gönderir (3608 mevcut test bunu doğruluyor).
**Salt okunur hâle gelen hiçbir alan yoktur.**

**DoD EK md.1 (her düğme ya istek atar ya kilitlidir):** ortak Secici'nin
tetiği de bir `<button>`dır; tıklanınca istek atmaz ama **sessiz değildir** —
seçenek listesini açar (`aria-expanded=true`). `RaporEkranTestleri`nin düğme
taraması bu gerekçeyle `role=combobox` düğmelerini dışarıda tutacak şekilde
güncellendi (gerekçe testin içinde yazılıdır).

---

## § 4. MİGRATION

**GEREKMEDİ** — paket yalnız arayüz katmanına dokunur; varlık, kolon, indeks
ya da sorgu filtresi değişikliği yoktur. `kuyumcu_net` şeması ve 56 migration
olduğu gibi kaldı.

---

## § 5. TESTLER

| Koşum | Toplam | Başarılı | Başarısız |
|---|--:|--:|--:|
| Paket ÖNCESİ (taban) | 3608 | 3608 | 0 |
| Göç sonrası, testler güncellenmeden | 3608 | 3491 | **117** |
| Yardımcı + çağrı yeri göçü sonrası | 3608 | 3607 | 1 |
| **Paket SONRASI (nihai)** | **3617** | **3617** | **0** |

**Yeni test sayısı: 9** (`Kuyumcu.Tests/P36cGocTestleri.cs`, `C36c_1…C36c_9`).

117 kırmızının tamamı **göç hatası değil, sözleşme değişikliğiydi**: eleman
kimlikleri korunduğu hâlde eleman TÜRÜ `select` → `combobox` oldu, bu yüzden
`onchange` yerine tıklama kullanılır. Hiçbir iddia zayıflatılmadı; tersine iki
yerde **güçlendirildi** (§ 1.3/2 ve mutasyon M36c-3).

---

## § 6. MUTASYON KANITLARI

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| **M36c-1** | `ModalPencere.KapatIstegi`den `await OdakGeriVer();` silindi | `C36c_4_Kapat_dugmesi_odagi_panel_DOMdayken_geri_verir`, `C36c_5_Escape_de_odagi_geri_verir` | `cmp` birebir |
| **M36c-2** | `Secici` seçenek satırından `data-deger="@o.Deger"` silindi | `C36c_2_Secenek_satiri_gonderilecek_kodu_tasir` | `cmp` birebir |
| **M36c-3** | `cekmece.js`ten kilit **artırma** satırı silindi | (1. deneme: **hiçbiri — TEST AÇIĞI**) → test güçlendirildi → `C36c_7_Kaydirma_kilidi_sayacli_ve_TEK_yerde` | `cmp` birebir |
| **M36c-4** | `Kasalar.razor`da `ElemanId="h-tur"` → `"h-tur-x"` | `UX1C2EkranTestleri.Kasalar_hareket_modali_her_acilista_YONU_SIFIRLAR` | `cmp` birebir |

**M36c-3 dürüst notu:** ilk denemede mutasyon **hayatta kaldı**. Test yalnız
`__kuyumcuPencereSayisi` adının dosyada geçip geçmediğine bakıyordu; artırma
satırı silinse bile azaltma satırı adı taşıdığı için yeşil kalıyordu. Test
sayacın **iki ucunu** da ölçecek biçimde düzeltildi ve mutasyon tekrarlandı.

Son mutasyondan sonra: `dotnet build Kuyumcu.sln --nologo` → **0 hata 0 uyarı**.

---

## § 7. DENETİM

```
═══ P-36 görsel dil / ortak bileşen cırcırı ═══
  ✓ P-36: HAM_SELECT                               84 (tavan 84, yalnız azalır)
  ✓ P-36: HAM_INPUT                                357 (tavan 357, yalnız azalır)
  ✓ P-36: MODAL_GOVDE_DISI_KAYDIRMA                0 (kaydırma yalnız gövdede)
  ✓ P-36: MOBIL_SATIR_TASMASI                      0 (satır yardımcıları sarıyor)
  ✓ P-36: KONTRAST_IHLALI                          0
  ✓ P-36: SECICI_TEK_KAYNAK                        1 (satır içi renk/px: 0)

═══ P-36c göç + pencere cırcırı ═══
  ✓ P-36c: SECICI_GOC                              97 (taban 92, yalnız artar)
  ✓ P-36c: ODAK_TUZAGI                             0 (odak tetikleyiciye döner, yutma yok)
  ✓ P-36c: PENCERE_KAYDIRMA_KILIDI                 1 (tek yer, sayaçlı)
  ✓ P-36c: SECICI_TEST_TEK_KAPI                    1

DENETİM TEMİZ.
```

**Tavan politikası:** `HAM_SELECT` tavanı **176 → 84** indirildi (yalnız azalır).
`SECICI_GOC` **yalnız artar** tabanı 92 — göç geri alınamaz. **Hiçbir tavan
ARTIRILMADI.**

**Ölçmediğim işin sayacını AÇMADIM.** `PENCERE_GENISLIK_CESIDI`,
`PENCERE_DUGME_SIRASI_TERS`, `BOS_DURUM_METINSIZ` ve `SABIT_RENK` düşürmesi
bu pakette **uygulanmadığı için sayaçları da yazılmadı** — yazılsaydı sayaç
yalan söylerdi (P-36'nın kurduğu ilke).

---

## § 8. CANLI DOĞRULAMA (localhost:5198 — 5199'a DOKUNULMADI)

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| API ayağa kalkıyor | 200 | **200 · `Healthy`** | `curl /saglik` |
| Kabuk sayfası | 200 | **200** | `curl /` |
| Göçen JS canlıda | kilit kodu var | **HTTP 200, `__kuyumcuPencereSayisi` 4 kez** | `curl /js/cekmece.js` |
| Göçen CSS canlıda | kilit sınıfı var | **HTTP 200, `body.pencere-acik { overflow-y: hidden; }` 1 kez** | `curl /css/app.css` |
| Kiracı sayısı (önce) | — | **9** | `select count(*) from tenants` |
| Kiracı sayısı (sonra) | 9 | **9** | aynı sorgu |
| Test kiracısı | 0 | **0** | `... where ad like 'PAKET%'` |
| API durduruldu | 5198 boş | **boş** | `lsof -nP -iTCP:5198 -sTCP:LISTEN` |

⚠ **Dürüst sınır:** Blazor WASM olduğu için `curl` ile **ekran içeriği**
doğrulanamaz; göçen seçicilerin tarayıcıda çizilmesi bu pakette **tarayıcıyla
doğrulanmadı** (§ 9 madde 4). Kanıt bu adımda **derleme + 3617 bUnit testi +
canlı varlık servisi**dir; "ekranda gördüm" DENMİYOR.

**Bu pakette test kiracısı AÇILMADI** (arayüz paketi, veri yazmıyor);
dolayısıyla silinecek kiracı da yok. `QA-TEST-` önekli kalıcı fikstürlere
dokunulmadı.

---

## § 9. YAPILMAYANLAR — ADIYLA (Anayasa md.1)

| # | Yapılmayan iş | Neden | Nereye |
|--:|---|---|---|
| 1 | **Kalan 58 serbest `<select>`in göçü** | Dönüştürücünün güvenli sınırı dışında kaldılar: `@if` içeren gövde, `new[] { … }` gibi süslü parantezli kaynak ifadesi, dinamik `id="@($"{Onek}-tur")"`, etiketi `<label for>` ile bağlı OLMAYAN alan, `disabled` olup **sebebi yazılmamış** seçici (7 adet — UX-S30 gereği sebep metni ÜRETİLMESİ gerekir, uydurulamaz). Her biri elle karar ister. | P-36d · `HAM_SELECT` tavanı **84**'te kilitli |
| 2 | **Ham girdi göçü (YAPILACAK 2)** — 357 `<input>`/`<textarea>` → `Bilesenler/Form/` ailesi | Hiç başlanmadı; adım ekonomisi select göçü + testlerin göçüne (117 kırmızının çözümü) gitti | P-36d · `HAM_INPUT` tavanı **357**'de kilitli |
| 3 | **83 pencerenin GÖRSEL tek tipe inmesi (YAPILACAK 3'ün büyük kısmı)**: 7 genişlik → 3 boy · 3 yarıçap → 1 · 3 gölge → 2 · 2 başlık puntosu → 1 · 5 düğme-satırı sınıfı → 1 · 3 "eşit genişlik" yöntemi → 1 · **30 pencerede ters düğme sırası** | Ölçüm ve altyapı (`.modal-alt`) P-36'da hazırdı; göç yapılmadı | P-36d |
| 4 | Pencere davranışlarının kalanı: **dolu formu uyarısız kapatma (onay)**, **iki pencerenin aynı anda açılamaması**, **arama paneline Esc** | Her biri ayrı bileşen + test; odak/kaydırma çekirdeği önceliklendi | P-36d |
| 5 | **46 rotanın 375/768/1440 ölçümü** (`main.ana-icerik.scrollWidth`) | Tarayıcı turu koşulmadı | Kapı öncesi zorunlu |
| 6 | **QA turu 8 ve kabul eşiği** (görsel ≥ 4,0 · kullanıcı dostu ≥ 3,6; taban 3,64 / 2,98) | Tur koşulmadı → eşik **ÖLÇÜLMEDİ, dolayısıyla KARŞILANMADI sayılır** | Kapı öncesi zorunlu |
| 7 | Sayaçlar `PENCERE_GENISLIK_CESIDI`, `PENCERE_DUGME_SIRASI_TERS`, `BOS_DURUM_METINSIZ`, `SABIT_RENK` düşürmesi | Ölçtükleri iş yapılmadı → sayaç yalan söylerdi | madde 3 ile birlikte |
| 8 | 12 düşük puanlı ekranın tek tek elden geçirilmesi | Ortak kabuk düzeltmeleri yapıldı, ekran turu yapılmadı | P-36d |

**Eşik hakkında dürüst söz:** kabul eşiği **ölçülmedi**. Puanı yükseltmek için
hiçbir bulgu gizlenmedi, hiçbir işlev silinmedi, QA fikstürüne dokunulmadı.

---

## § 10. ÖNCE / SONRA

| Ölçü | ÖNCE | SONRA |
|---|--:|--:|
| HAM_SELECT (tüm ağaç) | 176 | **84** |
| HAM_SELECT (serbest ekranlar) | 148 | **58** |
| `<Secici>` kullanımı | 2 | **97** |
| HAM_INPUT | 357 | 357 *(değişmedi)* |
| Test | 3608 / 0 kırmızı | **3617 / 0 kırmızı** |
| Derleme | 0 hata 0 uyarı | **0 hata 0 uyarı** |
| Odak kapanışta tetikleyiciye döner mi | **HAYIR** (kopmuş referans + `catch {}`) | **EVET** (panel DOM'dayken, yutma yok) |
| Pencere açıkken arka plan kayar mı | **EVET** | **HAYIR** (sayaçlı kilit, yalnız dikey) |
| Kimlik kapanışı | — | **90 kimlik birebir korundu** (M36c-4 kanıtı) |

### § 10.1 Kimlik kapanış tablosu (özet)

Göç edilen 90 serbest select kimliğinin **tamamı** birebir korundu; korunmadığı
an mevcut testler kırmızıya döner (mutasyon M36c-4). Dolaylı kanıt: kimlikler
korunmasaydı 3617 testin **en az 76'sı** (SeciciYardim'e taşınan çağrı yerleri)
kırmızı kalırdı; hepsi yeşil.

Türetilen kimlikler sözleşmesi: `<id>-sar · -ara · -liste · -liste-kap ·
-perde · -secenek-bos · -secenek-<n> · -hata · -durum · -sinir`.
**Bilinen tek çakışma riski** tarandı ve raporlandı:
`Islemler#islem-reyon-hata` (Secici'nin `-hata`sı yalnız `Hata` parametresi
verildiğinde çizilir, bu ekranda verilmiyor — bugün çakışma YOK, P-36d'de
temizlenmeli) ve `Emanet#e-cari-ara/-sinir` (çözüldü, § 1.3).

---

## § 11. DEĞİŞEN DOSYALAR (60)

`git status --porcelain` tüm kampanyanın kirli ağacını gösterdiği için,
**bu paketin** değiştirdiği dosyalar paket öncesi anlık görüntüyle `cmp`
karşılaştırılarak çıkarıldı:

**Web (24):** `Bilesenler/{Secici,ModalPencere,Cekmece,BelgeYukleyici,
FiyatListesiUrunPaneli,MasakFormu}.razor` ·
`Pages/{Banka,BankaEkstre,Cariler,DenetimIzi,Emanet,Evraklar,Fiyatlandirma,
Gider,Hurda,IslemDefteri,Islemler,Kasalar,Kullanicilar,Kurulum,UrunAgaci,
Urunler,Uyum,VeriIceAktarim,Vezne}.razor` ·
`wwwroot/css/app.css` · `wwwroot/js/cekmece.js`

**Test (35):** `SeciciYardim.cs` *(yeni)* · `P36cGocTestleri.cs` *(yeni)* ve
33 mevcut ekran testi dosyası (çağrı yeri göçü)

**Denetim (1):** `dotnet/denetim.sh`

---

## § 12. KOŞUL KARŞILAMA

| Koşul | Durum | Kanıt |
|---|---|---|
| Göç edilen ekranların mevcut testleri yeşil | ✔ | 3617/3617 |
| Eleman kimlikleri birebir korundu | ✔ | M36c-4 |
| Mutasyon kanıtı | ✔ | 4 mutasyon, biri test açığı bulup kapattı |
| `dotnet build` 0 hata 0 uyarı | ✔ | § 0 / § 6 |
| `denetim.sh` TEMİZ | ✔ | § 7 |
| Tam süit 0 başarısız | ✔ | § 5 |
| Test kiracısı silindi | ✔ (açılmadı) | § 8 |
| Rapor + kimlik kapanış + önce/sonra | ✔ | § 10 |
| Migration | ✔ gerekmedi | § 4 |
| **HAM_SELECT → 0 (serbest)** | ✘ **58 kaldı** | § 9 md.1 |
| **HAM_INPUT → 0 (serbest)** | ✘ **hiç başlanmadı** | § 9 md.2 |
| **PENCERE_GENISLIK_CESIDI ≤ 3** | ✘ ölçülmedi/yapılmadı | § 9 md.3 |
| **PENCERE_DUGME_SIRASI_TERS = 0** | ✘ yapılmadı | § 9 md.3 |
| **ODAK_TUZAGI = 0** | ✔ | § 2.1 · § 7 |
| **BOS_DURUM_METINSIZ = 0** | ✘ yapılmadı | § 9 md.7 |
| **SABIT_RENK tavanı düşürüldü** | ✘ yapılmadı | § 9 md.7 |
| **46 rota 375/768/1440 ölçümü** | ✘ | § 9 md.5 |
| **QA turu + kabul eşiği (4,0 / 3,6)** | ✘ **ölçülmedi → karşılanmadı** | § 9 md.6 |

---

## § 13. KURUCUYA / MALİ MÜŞAVİRE NOTLAR

* **Mali müşavire not gerekmiyor:** bu pakette hiçbir mevzuat hükmü koda
  yazılmadı, vergi/oran/eşik dokunulmadı.
* **Kurucuya:** ürünün bütün açılır listelerinin "tek seferde değiştirilebilir"
  olması hedefinin **%61'i** tamamlandı (serbest ekranlarda 148 → 58). Kalan
  58'in 7'si **sizin kararınızı bekliyor**: bunlar kilitli (`disabled`)
  seçiciler ve UX-S30 gereği yanlarına "neden kilitli" metni yazılması gerekiyor
  — metni uydurmadım. Kalan 51'i P-36d'de mekanik olarak bitirilebilir.
* **Kurucuya:** pencerelerde ölçülmüş iki gerçek kusur kapandı (kapanışta odak
  kaybı, arka planın kayması). Pencerelerin **görünüş** tek tipleşmesi
  (genişlik/köşe/gölge/düğme sırası) bu pakette **yapılmadı**.
