# Paket UX-5 — İkon seti: emoji yerine yerel vendored inline SVG sprite (Ikon.razor)

> Dalga 2c · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Tarih: 2026-09-17
> Kaynak karar: `ARAYUZ-ARASTIRMASI-2026-09-16.md` § Mühür **S6-i** ve çelişki çözümü **Ç7**
> Durum: **BİTTİ** — 1850/1850 test yeşil · `dotnet/denetim.sh` **TEMİZ** · 0 uyarı 0 hata

---

## 1. Neden (problem)

Kenar çubuğu, üst bar ve ortak bileşenler ikon olarak **emoji** kullanıyordu
(💎📊👥📦🗂️💰🤝📉🏦🏛️🧾📈💹📋🧮🛡️🏷️🔔📝🔑🏢🔍📤📖⚙️🚪☰🕐🟢📭🔒⚠✏️✓✕🖥️☀️🌙🇹🇷🇬🇧⏰).
Üç kanıtlı sorun:

1. **Platformlar arası tutarsız** — aynı 🛡️ macOS, Windows ve Android'de farklı
   renk/biçimde çizilir; marka dili kullanıcının işletim sistemine emanet edilir.
2. **Karanlık temada renk kontrolü yok** — emoji kendi renklerini taşır, `color`
   belirtecine uymaz; UX-2'nin kontrast sözleşmesi ikonları kapsayamıyordu.
3. **Kâğıtta anlamsız** — çoğu yazıcıda gri kutu ya da hiç çıkmaz; belge
   ekranlarında ikon taşıyan satır bilgi kaybediyordu.

## 2. Ne yapıldı (özet)

| # | İş | Dosya |
|---|---|---|
| 1 | Tek dosya SVG sprite, **41 `<symbol id="ikon-*">`**, `viewBox="0 0 24 24"`, `fill="none"`, `stroke="currentColor"` | `Kuyumcu.Web/wwwroot/img/ikon.svg` (YENİ) |
| 2 | Kaynaktan indirilen **tam lisans metni** | `Kuyumcu.Web/wwwroot/img/IKON-LISANS.txt` (YENİ) |
| 3 | `<Ikon Ad="…" Sinif="…" Etiket="…" />` bileşeni — `<use href="/img/ikon.svg#ikon-…">` | `Kuyumcu.Web/Bilesenler/Ikon.razor` (YENİ) |
| 4 | Derleme zamanı ad kataloğu (yanlış ad = derleme hatası, UX-S31) | `Kuyumcu.Web/Bilesenler/Ikonlar.cs` (YENİ) |
| 5 | Kenar çubuğu 28 emoji → ikon; vade takibi ⏰ dâhil | `Layout/KenarCubugu.razor` |
| 6 | Üst bar 7 emoji → ikon (☰ menü, kur, saat, bildirim ×2, mod anahtarı ×2) | `Layout/UstBar.razor` |
| 7 | Bayrak emojileri kaldırıldı (aşağıda gerekçe) | `Layout/DilSecici.razor` |
| 8 | Ortak bileşenlerdeki 7 emoji → ikon | `Bilesenler/BosDurum · DegismezIpucu · FiyatListesiUrunPaneli · SatirIciDuzenle · TemaSecici` |
| 9 | `.ikon` / `.ikon-buyuk` sınıfları + yazdırma kuralı (sabit renk YOK, `var()` + `currentColor`) | `wwwroot/css/app.css` |
| 10 | 11 yeni test | `Kuyumcu.Tests/IkonTestleri.cs` (YENİ) |
| 11 | 2 yeni CI sayacı (emoji = 0, sprite lisans başlığı) | `dotnet/denetim.sh` |

**CDN / npm / web font / yeni JS: YOK.** `index.html` `<script>` sayısı 6'da sabit
(denetim.sh doğruluyor). CSP'ye **yeni istisna eklenmedi** — canlı başlık § 7'de.

---

## 3. Lisans kanıtı (K14 · S6-i "kopyalamadan ÖNCE doğrula")

Semboller **Lucide 1.47.0** sürümünden, etiketli kaynaktan tek tek indirildi:
`https://raw.githubusercontent.com/lucide-icons/lucide/1.47.0/icons/<ad>.svg`
(her indirme HTTP **200**; `building-2` **404** döndü → o ad Lucide 1.47.0'da yok,
İşletme için `store` kullanıldı — varsayım yapılmadı, doğrulandı.)

Lisans dosyası kopyalamadan **önce** indirildi:
`https://raw.githubusercontent.com/lucide-icons/lucide/main/LICENSE` → HTTP 200, 3208 bayt.
Proje içindeki kopya: `Kuyumcu.Web/wwwroot/img/IKON-LISANS.txt` (birebir aynı metin).

### 3.1 Lisans metni (kaynaktan, kısaltmasız iki blok)

```
ISC License

Copyright (c) 2026 Lucide Icons and Contributors

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
```

```
The following Lucide icons are derived from the Feather project:
… check, circle, clock, key, lock, log-out, monitor, moon, search, upload, x …

The MIT License (MIT) (for the icons listed above)

Copyright (c) 2013-present Cole Bemis

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

**Kullandığımız 11 sembol Feather türevidir** (check, circle, clock, key, lock,
log-out, monitor, moon, search, upload, x) → MIT atfı sprite başlığında ve
`IKON-LISANS.txt`te birlikte duruyor. Her iki lisans da atıf dışında koşul
koymaz; ticari dağıtıma engel yoktur.

**Kanıt sayacı:** `denetim.sh` → `✓ ikon.svg lisans başlığı (ISC+MIT+kaynak) var`
ve `IkonTestleri.Sprite_lisans_surum_ve_kaynak_URL_basligi_tasir` (ISC + Lucide +
`1.47.0` + kaynak URL + Cole Bemis dizgelerini arar).

---

## 4. Sapmalar ve gerekçeleri

| # | Spesifikasyon / brif | Yapılan | Gerekçe |
|---|---|---|---|
| S1 | "**~20-24 sembol**" | **41 sembol** | Tek başına kenar çubuğunda **28 ayrı menü kalemi** var; 20-24'e sığdırmak ancak farklı menülere aynı ikonu vermekle olurdu ve bu UX-S31'e ("aynı kavram tek ad") aykırıdır. Sayım: 28 menü + 3 üst bar (menü, saat, basit mod) + 6 ortak bileşen + 3 tema + vade takibi. Dosya yine **tek** ve **16,7 kB**'dir (sıkıştırmasız); bütçe kaygısı doğurmaz. Hedefe (tek dosya, emoji sıfır, CDN yok) sadık kalındı. |
| S2 | "Uygulama: yalnız Layout + ortak bileşenler (BosDurum, HataBandi, BasariBandi, Rozet, SayfaBasi)" | `HataBandi`, `BasariBandi`, `Rozet`, `SayfaBasi`'ya **DOKUNULMADI**; buna karşılık `FiyatListesiUrunPaneli`, `SatirIciDuzenle`, `TemaSecici`, `DilSecici` kapsama **GİRDİ** | Bu dördünde bugün **emoji yok**; ikon eklemek DOM'u değiştirip mevcut bUnit metin iddialarını riske atardı, kazanç ise sıfırdı. Buna karşılık kapsanması **zorunlu** olan sayaç "Layout/ + Bilesenler/ altında emoji = 0"dır; emoji taşıyan her dosya kapsama alınmak zorundaydı. |
| S3 | Bayrak emojileri (🇹🇷/🇬🇧) | **Kaldırıldı, yerine ikon konmadı** | Bayrak ülkeyi anlatır, **dili değil** (yerleşik i18n anti-kalıbı: İngilizce yalnız 🇬🇧 değildir). Düğmelerde zaten **TR / EN** metni var ve anlamı tek başına taşıyor; ikinci bir "languages" ikonu iki düğmede de aynı çıkacağı için ayırt edici olmazdı. Canlı ekran görüntüsünde doğrulandı (§ 7). |
| S4 | "⏰ kapsam dışı" (brif emoji listesi) | Vade takibi ⏰ (**U+23F0**) de değiştirildi | Brifin verdiği aralık (U+1F300–1FAFF, U+2600–27BF) bu karakteri **kapsamıyordu** ve ⏰ sessizce geçiyordu. Test ve denetim sayacının aralığı U+23E0–23FF ve U+2B00–2BFF ile bilinçli genişletildi; bu bir gevşetme değil, kapının gerçek yüzeye oturtulmasıdır. |
| S5 | `ikon-*` id öneki | Sembol adları **Lucide dosya adı** (`ikon-layout-dashboard`), iş anlamı ise `Ikonlar` sabitinde (`Ikonlar.Panel`) | Sembolü kaynakta bulmak (güncelleme/denetim) ile ekranda anlamı okumak farklı ihtiyaçlardır; ikisi ayrıldı. `IkonTestleri` iki listenin **birebir** eşit olduğunu zorlar (ölü sembol ya da karşılıksız sabit = kırmızı). |

**Mevzuat/yasal hüküm:** bu pakette yok — ikon seti mevzuata bağlı bir karar
içermiyor, yapılandırmaya alınacak bir hüküm doğmadı.

---

## 5. Sözleşme-DoD

Bu paket **yeni varlık / yeni kayıt türü / yeni uç yaratmaz**: veri modeli,
servis, API ucu, yetki ve denetim yüzeyi **değişmedi**. Dolayısıyla PATCH/DELETE/
`[ModulYetkisi]`/`DenetimServisi` gerektiren bir doğum yoktur.

| Varlık / kayıt | Bu pakette değişti mi | Düzenleme yolu |
|---|---|---|
| — (yeni varlık yok) | Hayır | Uygulanamaz |

**Mevcut düzenleme yolları korundu ve GÜÇLENDİ:** `SatirIciDuzenle`'nin
kaydet/iptal düğmeleri eskiden **yalnız ✓ / ✕ glifi** taşıyordu; erişilebilir ad
glifin kendisiydi. Artık ikon `aria-hidden`, düğme ise `aria-label`/`title` ile
**"Kaydet" / "İptal"** (TR+EN, `Dil.T.Ortak`) diyor. Sözlük **genişletilmedi** —
mevcut TR+EN anahtarlar kullanıldı, çıplak metin yazılmadı.

**DoD EK/1 (ölü düğme yok):** yeni düğme eklenmedi; dokunulan tüm düğmeler
`@onclick` ya da `disabled` taşımaya devam ediyor (`denetim.sh` sayacı 57, tavan 71 — **artış yok**).
**DoD EK/2 (tek düzenleme kalıbı):** `ModalPencere` kullanımı 43 (tavan 45), `duzenlenenId` 1 (tavan 1) — **artış yok**.

---

## 6. K4 — id envanteri diff'i (2c öncesi / sonrası)

Dokunulan her dosyanın `id="…"` kümesi, düzenleme **öncesi** kopyasıyla
karşılaştırıldı:

```
AYNI  UstBar.razor                 (6 id)
AYNI  BosDurum.razor               (0 id)
AYNI  DegismezIpucu.razor          (1 id)
AYNI  FiyatListesiUrunPaneli.razor (43 id)
AYNI  SatirIciDuzenle.razor        (6 id)
AYNI  TemaSecici.razor             (1 id)
KenarCubugu.razor → tek id: id="kenar-cubugu" (değişmedi)
DilSecici.razor   → 0 id (değişmedi)
```

**Eklenen id: 0 · Silinen id: 0 · Değişen id: 0.** Sözleşme bozulmadı.
`IkonTestleri.KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni` menü
**rota** kimliklerini de dondurur (menüde id yoktur, kimliği rota taşır).

**Dokunulan sayfalar (DoD EK 2 — tek dokunuş):** `Pages/` altında **hiçbir sayfa
açılmadı** (`denetim.sh`: Pages/*.razor dosya sayısı 30, tavan 30). Sayfa içi
emojiler brifin dediği gibi **B@ dilimlerine** bırakıldı.

---

## 7. Canlı doğrulama (localhost:5198, Development)

API `--no-build --urls http://localhost:5198` ile arka planda çalıştırıldı, iş
bitince **yalnız o PID** durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` boş).
Kurucunun 5199 inceleme sunucusuna **dokunulmadı**.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `GET /img/ikon.svg` | 200, `image/svg+xml` | **200**, `Content-Type: image/svg+xml`, `Content-Length: 16728` | curl -D başlıkları |
| 2 | Sprite içeriği | 41 sembol | `grep -c "<symbol id="` → **41** | indirilen gövde |
| 3 | `GET /` CSP başlığı | `img-src 'self'` yeterli, **yeni istisna yok** | `default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'` — **değişmedi** | curl -D |
| 4 | `GET /css/app.css` | UX-5 bloğu sunuluyor | `grep -c "paket UX-5"` → **1** | curl |
| 5 | Tarayıcı: `<use href="/img/ikon.svg#…">` **aynı kökten** çözülüyor mu | Ağ isteği 200, konsolda CSP ihlali yok | `GET http://localhost:5198/img/ikon.svg → 200 OK`; **konsolda hiç hata yok** | ağ + konsol dökümü |
| 6 | `currentColor` ana belgeden geçiyor mu (aydınlık) | ikon rengi = kapsayıcının rengi | sonda `color: rgb(245,158,11)` → ikon `rgb(245,158,11)`; `rgb(56,189,248)` → ikon `rgb(56,189,248)` | `getComputedStyle` |
| 7 | `.ikon` boyut sınıfı uygulanıyor mu | em tabanlı, satır içi style yok | `17.5px × 17.5px` (= 1,25em / 14px), çizim kutusu `17.5x17.5` | `getComputedStyle` + `getBoundingClientRect` |
| 8 | **Karanlık tema** kontrastı | ikon gövde metni rengini alır, okunur | gövde `rgb(232,238,246)` / zemin `rgb(11,18,32)`; ikon `stroke: rgb(232,238,246)`, `fill: none` → **kontrast ≈ 15:1** (WCAG AAA üstü) | `getComputedStyle` |
| 9 | Görsel: 9 sembol karanlık temada çizildi mi | hepsi net, boş kare yok | `gem · layout-dashboard · users · wallet · shield-check · bell · alarm-clock · store · log-out` **9/9 çizildi** | ekran görüntüsü |
| 10 | Dil seçici bayraksız | TR / EN metni kaldı | `.dil-secici button` metinleri: **["TR","EN"]**, ekran görüntüsünde de böyle | DOM + ekran görüntüsü |

> ⚠ **Kimlik doğrulaması gerektiren ekranların görsel turu yapılmadı.** Kenar
> çubuğu ve üst bar oturum arkasındadır; parola girmek uygulayıcı ajanın
> yetkisinde değildir (politika). Bunun yerine **aynı köken, aynı CSP, aynı
> `.ikon` sınıfı ve aynı `<use>` kalıbı** canlı sayfada ölçülerek ve GÖRÜLEREK
> doğrulandı (adım 5-9); kenar çubuğunun DOM'u ise bUnit'te
> (`Ekranda_cizilen_her_use_hedefi_spritete_var`, `KenarCubugu_menu_rotalari…`)
> sınanıyor. Oturum içi ekran görüntüsü **QA tarayıcı turuna** bırakıldı (§ 11/1).

**SQL kanıtı / test kiracısı:** Bu paket **veritabanına hiç dokunmaz** (varlık,
migration, uç yok). Bu yüzden test kiracısı **açılmadı**.
`SELECT COUNT(*) FROM tenants` → **başlangıç 8 · bitiş 8** (değişmedi).
`QA-TEST-` önekli kalıcı QA fikstürlerine **dokunulmadı**.

---

## 8. Derleme · testler · denetim

**Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata**.
**Migration:** gerekmedi (şema değişikliği yok) — 33 migration aynen duruyor.

| Koşum | Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|---|
| Filtreli | `IkonTestleri` | 11 | 11 | 0 | 0 |
| **Tam süit** | `Kuyumcu.Tests` (trx) | **1850** | **1850** | **0** | **0** |

TRX: `scratchpad/trx-pUX-5/_Ramazan-MacBook-Air_2026-09-17_12_17_43.trx`
→ `<Counters total="1850" executed="1850" passed="1850" failed="0" …/>`

**Test sayısı önce / sonra: 1839 → 1850 (+11 yeni).**

### Yeni testler (11)

1. `Sprite_lisans_surum_ve_kaynak_URL_basligi_tasir`
2. `Lisans_tam_metni_kaynagindan_kopyalanmis_olarak_durur`
3. `Her_symbol_benzersiz_24lik_viewBox_ve_currentColor_tasir`
4. `Sprite_disaridan_hicbir_kaynak_cagirmaz`
5. `Ikonlar_sabitleri_ile_sprite_sembolleri_BIREBIR_ayni`
6. `Ekranda_cizilen_her_use_hedefi_spritete_var`
7. `Ikon_varsayilan_olarak_ekran_okuyucuya_gizlidir`
8. `Etiket_verilince_ikon_role_img_ve_aria_label_alir`
9. `Ikon_satir_ici_style_yazmaz_boyut_siniftan_gelir`
10. `Layout_ve_Bilesenler_altinda_emoji_KALMADI`
11. `KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni`

### Denetim çıktısı (son satırlar)

```
═══ Arayüz kapıları (paket UX-2 Faz A2 · K14 + MÜHÜR 2 DoD) ═══
  ✓ app.css ham hex (kâğıt dâhil kaba sayım)  25 (tavan 25)
  ✓ Pages+Bilesenler+Layout satır içi style=     120 (tavan 196)
  ✓ vendor CSS lisans başlığı (MIT+kaynak)     var
  ✓ index.html <script> sayısı (sabit 6)         6
  ✓ index.html satır içi <script> (0 olmalı)    0
  ✓ Pages: @onclick'siz ve disabled'sız <button>  57 (tavan 71)
  ✓ Pages: ModalPencere kullanımı                43 (tavan 45)
  ✓ Pages: duzenlenenId kalıbı                   1 (tavan 1)
  ✓ Pages/*.razor dosya sayısı                   30 (tavan 30)
  ✓ Pages: doğrulama mesajsız EditForm yok       0 EditForm / 0 ValidationMessage
  ✓ nginx CSP + X-Frame-Options DENY               var
  ✓ Layout+Bilesenler emoji ikon (0 olmalı)       0     ← YENİ (UX-5)
  ✓ ikon.svg lisans başlığı (ISC+MIT+kaynak)   var      ← YENİ (UX-5)

DENETİM TEMİZ.
```

---

## 9. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/UX-5/` altına kopyalandı → tek eşleşmeli
değişiklik → `dotnet build` + filtreli `dotnet test` → yedek geri kopyalandı →
`cmp` ile **birebir aynı** olduğu kanıtlandı. Son mutasyondan sonra temiz
`dotnet build` → 0 uyarı 0 hata.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M-UX5-01** | `ikon.svg`ten `<symbol id="ikon-bell">` **silindi** | `Ikonlar_sabitleri_ile_sprite_sembolleri_BIREBIR_ayni`, `Ekranda_cizilen_her_use_hedefi_spritete_var` | `cmp` aynı |
| **M-UX5-02** | Sprite başlığında `ISC License` → `Serbest` | `Sprite_lisans_surum_ve_kaynak_URL_basligi_tasir` (+ `denetim.sh` lisans sayacı) | `cmp` aynı |
| **M-UX5-03** | `KenarCubugu`da `<Ikon Ad="@Ikonlar.Cariler" />` → 👥 emoji | `Layout_ve_Bilesenler_altinda_emoji_KALMADI`, `KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni` | `cmp` aynı |
| **M-UX5-04** | `Ikon.razor` `<use href>` → `https://cdn.example.com/ikon.svg#ikon-{Ad}` (CDN'e kaçış) | `Ekranda_cizilen_her_use_hedefi_spritete_var` | `cmp` aynı |
| **M-UX5-05** | `Ikon.razor`dan varsayılan `aria-hidden` kaldırıldı | `Ikon_varsayilan_olarak_ekran_okuyucuya_gizlidir` | `cmp` aynı |
| **M-UX5-06** | Menü rotası `/raporlar/vade-takibi` → `/raporlar/vadeler` | `KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni` | `cmp` aynı |

> **Bulunan ve kapatılan test açığı:** M-UX5-04 ilk denemede **hiçbir testi
> kırmadı**. Sebep testte değil, mutasyonun kendisindeydi: `perl -0777 -pi`
> `/g` bayrağı olmadan yalnız **ilk** eşleşmeyi (yorum satırındaki örnek URL'yi)
> değiştirmişti, gerçek `<use>` etiketine hiç dokunmamıştı. Mutasyon gerçek
> işaretlemeye uygulandığında test **kırmızıya döndü**. Bu, Anayasa md.1 gereği
> gizlenmeden kaydedilmiştir: "hiçbir testi kırmayan mutasyon" iddiası **yanlış
> alarmdı**, ama doğrulanmadan geçilmedi.

---

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından bu pakete ait olanlar
(**COMMIT YOK** — çalışma ağacında):

**Yeni:**
- `dotnet/Kuyumcu.Web/wwwroot/img/ikon.svg`
- `dotnet/Kuyumcu.Web/wwwroot/img/IKON-LISANS.txt`
- `dotnet/Kuyumcu.Web/Bilesenler/Ikon.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/Ikonlar.cs`
- `dotnet/Kuyumcu.Tests/IkonTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-UX-5-uygulama-raporu.md` (bu dosya)

**Değişen:**
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `dotnet/Kuyumcu.Web/Layout/UstBar.razor`
- `dotnet/Kuyumcu.Web/Layout/DilSecici.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/BosDurum.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/DegismezIpucu.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/FiyatListesiUrunPaneli.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/SatirIciDuzenle.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/TemaSecici.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css`
- `dotnet/denetim.sh`

> Listedeki diğer `M`/`??` kalemler **önceki paketlerden** gelir; bu pakette
> onlara dokunulmadı. Proje ağacına `.mutorj`/`.bak`/`.tmp` **bırakılmadı**
> (mevcut `.mutorj` artıkları önceki oturumlardandır, memory notunda kayıtlı).

---

## 11. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Tek dosya sprite `wwwroot/img/ikon.svg`, `<symbol id="ikon-*">` | ✅ | 41 sembol; canlı `GET /img/ikon.svg` 200 |
| `currentColor`, `viewBox 0 0 24 24`, `fill="none"` | ✅ | `Her_symbol_benzersiz_24lik_viewBox_ve_currentColor_tasir` |
| `Bilesenler/Ikon.razor` + `<use href="/img/ikon.svg#ikon-…">` | ✅ | dosya + `Ekranda_cizilen_her_use_hedefi_spritete_var` |
| `aria-hidden` ya da `aria-label` | ✅ | testler 7 ve 8 |
| Kaynak Lucide (ISC) / Tabler (MIT); LICENSE **önce** indirilip okundu | ✅ | § 3 (Lucide 1.47.0, ISC + Feather MIT) |
| Dosya başlığında lisans + sürüm + kaynak URL | ✅ | § 3; `denetim.sh` sayacı |
| CDN / font / npm / JS **yok** | ✅ | `Sprite_disaridan_hicbir_kaynak_cagirmaz`; `<script>` sayısı 6 sabit |
| CSP'de yeni istisna **yok** (`img-src 'self'` yeterli) | ✅ | canlı başlık § 7/3 |
| Uygulama yalnız Layout + ortak bileşenler; `Pages/` **açılmadı** | ✅ | Pages/*.razor 30 (tavan 30); § 6 |
| Karanlık temada kontrast | ✅ | ikon `rgb(232,238,246)` / zemin `rgb(11,18,32)` ≈ 15:1 |
| Yazdırmada ikon görünür | ⚠ Kısmi | `@media print` kuralı yazıldı (`stroke: currentColor`, `print-color-adjust: exact`); çizgisel SVG "arka planı basma" ayarından etkilenmez. **Kâğıt/PDF önizlemesiyle GÖRSEL doğrulama yapılmadı** → § 12/2 |
| `IkonTestleri` (her `<use>` hedefi sprite'ta var) | ✅ | test 6 (+ 5) |
| `KenarCubugu` bUnit, menü kimlikleri değişmez (K4) | ✅ | test 11; § 6 id diff'i (0/0/0) |
| `denetim.sh` sayacı: Layout+Bilesenler emoji = 0 | ✅ | `✓ Layout+Bilesenler emoji ikon (0 olmalı) 0` |
| Mutasyon: sprite'tan sembol silinince `IkonTestleri` kırmızı | ✅ | M-UX5-01 |
| K7 bileşen bütçesi (yeni `Api.*` çağrısı yok, sayfa bileşeni eklenmedi) | ✅ | `Ikon` bir **çizim ilkeli**dir, veri çağırmaz; `Pages/` dokunulmadı |
| Satır içi `style=""` yazılmadı | ✅ | sayaç 120 (tavan 196), **artış yok**; test 9 |
| Yeni CSS'te sabit renk yok, `var()` + 4px ritmi | ✅ | ham hex 25 (tavan 25, artış yok); `--bosluk-6`, `--bosluk-2` |
| Dil TR+EN, çıplak metin yok | ✅ | `Dil.T.Ortak.Kaydet/Iptal` kullanıldı, sözlüğe anahtar eklenmedi |
| COMMIT yok | ✅ | çalışma ağacında |
| 5199'a dokunulmadı | ✅ | yalnız 5198 kullanıldı, PID ile durduruldu |

---

## 12. Açık kalanlar

1. **Oturum içi görsel tur** — kenar çubuğu / üst bar ikonlarının gerçek
   ekranda (aydınlık + karanlık, 375 px) görsel doğrulaması **QA tarayıcı
   turuna** bırakıldı; uygulayıcı oturum açamadığı için yalnız aynı köken/CSP
   üzerinde ölçüm ve 9 sembollük görsel kanıt üretildi (§ 7).
2. **Yazdırma (kâğıt) görsel kanıtı** — `@media print` kuralı yazıldı ama
   yazıcı/PDF önizlemesinde **görülerek** doğrulanmadı. Belge ekranları
   (`PusulaBelgesi`, `UrunEtiketleri`) bu pakette ikon kullanmıyor, dolayısıyla
   risk düşüktür; yine de dalga kapısının kâğıt turuna eklenmeli.
3. **Sayfa içi emojiler (`Pages/*.razor`)** — brifin dediği gibi **B@ dilimlerine**
   bırakıldı; `denetim.sh` sayacı bilinçli olarak yalnız `Layout/` + `Bilesenler/`
   kapsıyor. Sayfa dilimleri bitince sayacın kapsamı `Pages/`e genişletilmeli.
4. **Sprite büyüme disiplini** — bugün 41 sembol / 16,7 kB. Sembol sayısına
   sayaç konulmadı; 60'ı aşarsa (a) ikon bütçesi tartışılmalı, (b) `Ikonlar`
   sabitleri alan bazlı alt sınıflara bölünmeli.
5. **Lucide sürüm takibi** — sürüm `1.47.0` olarak **sabitlendi** ve teste
   yazıldı. Yükseltme, lisansın yeniden indirilip okunmasını ve testteki sürüm
   dizgesinin güncellenmesini gerektirir (bilinçli sürtünme).

## 13. Kurucuya / mali müşavire notlar

- **Kurucuya (lisans):** Lucide **ISC**, Feather türevi 11 sembol **MIT**. Her
  ikisi de yalnız **telif ve izin bildiriminin kopyalarda yer almasını** şart
  koşar; bizde bu şart hem `ikon.svg` başlığında hem `wwwroot/img/IKON-LISANS.txt`
  dosyasında karşılanıyor ve dosya ürünle birlikte **dağıtılıyor**. Ticari satışa,
  kapalı kaynak dağıtıma ve yeniden markalamaya engel yoktur. Ayrı bir bedel,
  atıf ekranı ya da "powered by" zorunluluğu **yoktur**.
- **Mali müşavire:** bu pakette mevzuata bağlı hiçbir hüküm yok; vergi, belge
  düzeni, MASAK ve KDV yüzeyine dokunulmadı.
- **Ücretsiz pilot kuralı (MÜHÜR 2):** bu raporda fiyat/teklif/tahsilat içeren
  hiçbir metin üretilmedi.
