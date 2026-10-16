# FRONTEND — DÜĞME DİLİ · MENÜ ARAMASI · KURUMSAL GÖRSEL KİMLİK
## Kuyumcu ERP · 2026-09-21 · Ertuğ (03 Frontend, 3. ajan) · **SALT OKUNUR TUR**

> **Bu turda hiçbir kaynak dosya değiştirilmedi.** Sebep: arka planda P-22 uygulama dalgası
> aynı ağaca yazıyor. `dotnet build` / `dotnet test` **koşulmadı**, git/commit/rm/mv yok.
> Kendi tarayıcı sekmemde (`tab-5`) çalışıldı, başka sekmeye ve 5198'e dokunulmadı.
> Kaydetme/silme düğmelerine basılmadı, hiçbir veri değiştirilmedi.
>
> **Önceki turlar:** 1. ajan envanteri çıkardı (`FRONTEND-SARTNAME-2026-09-21.md`), 2. ajan
> popup/kaydırma alanına bakıyor. Bu belge **o sayımları tekrarlamaz**; düğme dili, menü
> araması, görsel kimlik ve (kurucunun 2. talimatı üzerine) **ortak seçim bileşeni** üzerine
> derinleşir.
>
> **Ara çıktılar:** `scratchpad/fe3-dugmeler2.json` (431 düğme), `scratchpad/fe3-selectler.json`
> (176 dropdown), `scratchpad/fe3-kontrast.py` (kontrast hesaplayıcı).

---

## § 0. GİRİŞ VE ÖLÇÜM KOŞULLARI (neyi ölçtüm, neyi ölçemedim)

**Giriş YAPILDI** (1. turda yapılamamıştı). `scratchpad/qa-fikstur.json` bu turda dolu geldi
(736 bayt). Oturum, **parola alanına hiçbir değer yazılmadan**, fikstürdeki erişim jetonu
`localStorage` anahtarlarına (`kuyumcu-access` / `kuyumcu-refresh`) konularak açıldı.
Kullanıcı: `QA Frontend`, rol **SAHIP**, arayüz modu **Profesyonel** (ekrandan doğrulandı).
Jeton/parola değeri bu belgeye **yazılmadı**.

**Ölçülen ekranlar (canlı, 5199):** `/` (Panel), `/cariler`, `/islemler`, `/urunler`,
`/kasalar` — 1440×900 ve 375×812'de. Menü, arama paleti ve çekmece ayrıca ölçüldü.

### ÖLÇÜLEMEYENLER (dürüst kayıt)
| Ne | Neden |
|---|---|
| Dolu veriyle tablo/satır düğmeleri (Düzenle/Sil/Storno satır içi) | QA kiracısı **boş** — "Cari bulunamadı", "Ürün bulunamadı", "Kasa bulunamadı". Satır düğmeleri DOM'a hiç basılmadı. Bu yüzden canlı düğme sayıları kaynak sayılarından düşüktür; **kaynak taraması tamdır, canlı tur örneklemdir.** |
| Native `<select>` **açılır listesinin** ekran görüntüsü | Açılır liste işletim sistemi tarafından, sayfa dışında çizilir; tarayıcı ekran görüntüsüne girmez. Bunun yerine `appearance`, `background-image` ve `<option>` hesaplanmış stilleri ölçüldü (§ 7.1). |
| 41 ekranın canlı hâli | Adım bütçesi (≤120 araç çağrısı). Kaynak taraması 46 sayfanın tamamını kapsar. |

### ⚠ YAYIN ile AĞAÇ ARASINDA KANITLI FARK
5199, **DALGA 4 anlık görüntüsüdür** (yayın `scratchpad/qa-yayin`, 05:16'da alınmış).
`Layout/MenuTanimi.cs` ise **08:34'te** değişmiş. Ölçülen fark:

* **Ağaçta** `MenuTanimi.Ogeler` **38** bağlantı tanımlıyor (+ `AltBlok` 2 = Kılavuz, Ayarlar).
* **Canlıda** menüde **36** bağlantı çiziliyor.
* **Eksik olan tam olarak ikisi:** `/gider` (*Gider ve Virman*) ve `/hurda`
  (*Hurda Stoğu ve Ramat Çıkışı*) — ikisi de kaynakta `MenuGrubu.ParaBanka` altında tanımlı,
  canlı "Para ve Banka" grubunda **yok** (canlıda 8 öğe, kaynakta 10).
* Rol SAHIP ve mod Profesyonel olduğu için bu bir yetki/mod filtresi **değildir**; yayının
  eskiliğidir. (Kaynaktaki yorum satırı da hâlâ "36 menü bağlantısı" diyor — `MenuTanimi.cs:81`
  yorumu 38'e güncellenmemiş.)

**Sonuç:** § 2'deki menü ölçümleri 36 bağlantı üzerindendir; şartname 38'e göre yazılmıştır.

---

## § 1. DÜĞME DİLİ BULGULARI

### 1.1 Kaç düğme sistemi var — 1. ajanın sayımının DOĞRULANMASI

Bağımsız bir tarayıcıyla (tırnak farkında, Razor yorumları düşülmüş) yeniden sayıldı.
**1. ajanın sayıları doğrulandı**; aradaki fark tek bir sebepten geliyor ve o da ölçüldü:

| Sistem | 1. ajan | Bu tur | Fark | Açıklama |
|---|---:|---:|---:|---|
| `.btn …` | 247 | **253** | +6 | fark = `<a class="btn …">` (düğme gibi görünen bağlantı) |
| `.dugme …` | 94 | **101** | +7 | fark = `<a class="dugme …">` |
| çıplak sınıf (ne `.btn` ne `.dugme`) | 62 | **62** | 0 | **birebir aynı** |
| hiç sınıf yok | 15 | **15** | 0 | **birebir aynı** |
| **TOPLAM** | **418** | **431** | **+13** | 418 (`<button>`) + 13 (`<a class=btn\|dugme>`) = 431 |

→ **1. ajanın sayımı yalnız `<button>` içindir ve doğrudur.** Bu tur ayrıca *düğme gibi
davranan 13 bağlantıyı* ekler. Şartname açısından önemi: bunlar `<button>` değildir, klavye ve
ekran okuyucu davranışları farklıdır, ama görsel olarak düğmedirler — **aynı şartnameye tabi
olmalıdırlar**.

**Çıplak sınıf sisteminin dökümü (62 düğme):** `ikincil` 28 · `sekme` 14 ·
`@SekmeSinif(…)` 7 · `secici-sonuc` 3 · `pill` 2 · `@(Dil.Dil…)` 2 · `cekmece-kapat` 1 ·
`modal-kapat` 1 · `si-tetik` 1 · `kenar-ortu` 1 · `@(Modu.BasitMi…)` 1 ·
`@(Modu.ProfesyonelMi…)` 1.
Yani **"üçüncü sistem" aslında tek bir sistem değil, 12 ayrı ad-hoc sınıftır.**

**Canlıda doğrulanan karışım** (`/islemler`, 1440px): görünen 12 düğmenin **4'ü `.btn`,
8'i `ciplak:sekme`** — tek ekranda iki sistem yan yana.

### 1.2 `.btn` ile `.dugme` gerçekten aynı mı? — satır satır karşılaştırma

`app.css:163-189` (`.btn`) ve `app.css:197-212` (`.dugme`) gövdeleri aynı
(`inline-flex; gap:.5rem; font-weight:500; font-size:.875rem; padding:.5rem 1rem;
border-radius:var(--radius-md); border:1px solid transparent`). Farklar:

| | `.btn` çıplak | `.dugme` çıplak |
|---|---|---|
| zemin | **yok** (şeffaf) | `var(--yuzey)` |
| kenarlık rengi | `transparent` | `var(--kenarlik)` |
| metin | miras | `var(--metin)` |
| `-birincil` kenarlığı | **yok** | `var(--vurgu)` |
| ek varyant | `-buyuk`, `-tam` | `-satir` (grup sarmalayıcı) |

→ Çıplak `.btn` görünmez bir düğme, çıplak `.dugme` ikincil düğme gibi durur.
**Aynı sınıf adını yazan iki geliştirici farklı şey görür.** Tek sistemde birleşme zorunlu.

### 1.3 ⛔ KANITLI KRİTİK HATA — birincil düğme okunmuyor (kontrast 1.60:1)

**Canlıda ölçüldü** (Panel, koyu tema): `panel-satis-yap` düğmesinin hesaplanmış rengi
`rgb(232,238,246)` (`--metin`), zemini `rgb(240,180,41)` (`--vurgu`).

* **Kontrast: 1.60:1** — WCAG AA metin eşiği 4.5:1. **KALDI.**
* Aynı düğme **açık temada: 2.91:1** — o da **KALDI**.
* Doğrusu (`--vurgu-metin` uygulansaydı): koyu temada **10.17:1** — geçerdi.

**Kök neden (kesin):** `Pages/Panel.razor:26`
`<a class="btn btn-birincil dugme-benzeri" id="panel-satis-yap" …>`
`.dugme-benzeri` (`app.css:675-679`) `color: var(--metin)` yazıyor ve `.btn-birincil`
(`app.css:170`) ile **CSS özgüllüğü eşit** (ikisi de tek sınıf); `.dugme-benzeri` dosyada
**daha sonra** geldiği için kazanıyor ve birincil düğmenin metin rengini eziyor.

**Karşılaştırma kanıtı:** aynı `.btn.btn-birincil`, `.dugme-benzeri` **olmadan**
(`/cariler` → "+ Yeni Cari") `rgb(20,16,10)` = `--vurgu-metin` ile doğru çiziliyor.

`.dugme-benzeri` kullanan 3 yer: `Pages/Panel.razor:26` (**bozuk — `.btn-birincil` ile
birlikte**), `Pages/Bildirimler.razor:95` (tek başına, sorunsuz),
`Pages/Siparisler.razor:319` (tek başına, **kilitli dosya**).

### 1.4 Adlandırma tutarsızlığı — iş → etiket tablosu

Etiketler i18n anahtarlarından **grup bazlı** çözüldü (`Sozlukler.cs` TR bloğu). Bu önemli:
naif çözümleme `Dil.T.Pos.Iptal`i "İptal" sanar, oysa değeri *"Tahsilatı iptal et"*tir.

| İş | Kullanılan etiketler (ekran sayısı) | Toplam farklı etiket |
|---|---|---|
| **Kaydetme / oluşturma / onaylama** | `Kaydet` (18) · `Onayla` (9) · `Ekle` (2) · `Uygula` (2) · `Evet` (2) | **5** |
| **Vazgeçme / kapatma** | `İptal` (27) · `Kapat` (6) · `Vazgeç` (4) · `Geri` (2) · `✕`/`&times;` (3) | **5** |
| **Silme / geri alma** | `İptal Et` (4) · `Sil` (1) · `Storno` (4) · `Pasife al` (6) + `Pasife Al` (5) | **5** |
| **Düzenleme** | `Düzenle` (13) · `Düzelt` · `Güncelle` | **3** |
| **Etkinleştirme** | `Aktife al` (6) · `Aktifleştir` (3) | **2** |

**En ciddi üç bulgu:**

1. **`İptal` iki ayrı işi birden anlatıyor.** 27 ekranda *"vazgeç, hiçbir şey yapma"*
   anlamında; 4 ekranda (`İptal Et`) *"var olan kaydı iptal et — yıkıcı işlem"* anlamında.
   Kuyumcu tezgâhında bu ikisinin karışması **veri kaybı riskidir**.
2. **Aynı etiket, iki farklı büyük/küçük harf yazımı:** `Pasife al` (6 ekran) ve
   `Pasife Al` (5 ekran); `Aktife al` / `Aktifleştir`. Tek bir cümle düzeni yok.
3. **Sözlükte aynı anahtar adı ≥2 farklı metne bağlanmış: 150 anahtar.** Düğmeleri doğrudan
   ilgilendirenler:

| Anahtar | Farklı metin | Değerler |
|---|---:|---|
| `Iptal` | **5** | `İptal` · `İptal Et` · `Fişi iptal et` · `Provayı iptal et` · `Tahsilatı iptal et` |
| `Kaydet` | **4** | `Kaydet` · `Değişiklikleri kaydet` · `Gerekçeyi güncelle` · `Ramat çıkışını kaydet` |
| `Kapat` | 2 | `Kapat` · `Kapat ve Deftere Yaz` |
| `Onayla` | 2 | `Onayla` · `Evet, karşılıksız işaretle` |
| `Duzenle` | 2 | `Düzenle` · `Kasayı Düzenle` |
| `Yazdir` | 2 | `Yazdır` · `Etiketleri Yazdır` |

→ `Dil.T.X.Kaydet` yazan geliştirici ne göreceğini **bilemez**. (Not: `Kaydet = "Gerekçeyi
güncelle"` eşlemesi anahtar adının anlamıyla da çelişiyor.)

**i18n dışı, doğrudan koda gömülü düğme metni: 14 adet / 12 dosya.**
`Bilesenler/Cekmece.razor:14` ve `Bilesenler/ModalPencere.razor:12` (`&times;`) ·
`Layout/DilSecici.razor:5,9` (`TR`/`EN`) · `Layout/UstBar.razor:73` (`⟳`) ·
`Pages/BankaEkstre.razor:209,211` (`←`/`→`) · `Pages/Islemler.razor:290` (`✕`) ·
`Pages/Kasalar.razor:45` (`✏️` — **emoji, ikon sistemi dışı**) · `Pages/Sayim.razor:68` (`→`,
kilitli) · `Pages/UrunAgaci.razor:105` · `Pages/SiparisDetay.razor:188`.

**Görünür etiketi hiç olmayan (yalnız ikon) düğme: 7 adet.**
`Pages/Fiyatlandirma.razor:92` · `Pages/Gider.razor:219` · `Pages/Islemler.razor:28` ·
`Bilesenler/TemaSecici.razor:11` · `Bilesenler/GlobalArama.razor:92` ·
`Bilesenler/KayitSecici.razor:81` · `Pages/Ayarlar.razor:34` (kilitli).

### 1.5 Hiyerarşi — bir ekranda kaç birincil düğme?

Ölçüm: `birincil` sınıfı taşıyan düğmelerin dosya başına sayısı.

* **1'den çok birincil düğmesi olan dosya: 22 / 38** (düğmesi olan dosyalar içinde) → **%58'inde
  görsel hiyerarşi yok.**
* En ağırları: `Pages/Ayarlar.razor` **6** (kilitli) · `Pages/Uyum.razor` **6** ·
  `Pages/Cariler.razor` **5** · `Banka` / `Kasalar` / `Kullanicilar` / `Kurulum` /
  `Urunler` / `Sayim`(kilitli) **4'er**.
* **Hiç birincil düğmesi olmayan 13 sayfa:** `Bildirimler, Cikti, DenetimIzi, EvrakTakvimi,
  IslemDefteri, KasaRaporu*, PusulaBelgesi, SatisAnalizi*, SiparisDetay, Siparisler*,
  VadeTakibi, Vezne, Vitrin` (*=kilitli). Bu ekranlarda kullanıcı **"asıl eylem hangisi"**
  sorusunun görsel cevabını hiç almıyor — `Pages/Vezne.razor` ve `Pages/SiparisDetay.razor`
  düğmelerinin çoğu zaten **hiç sınıfsız** (1. ajanın 15 sınıfsız düğmesinin 10'u bu iki dosyada).

**Tehlikeli eylem ayrışıyor mu?** Kısmen. `.btn-tehlike`/`.dugme-tehlike` **37 düğmede,
19 dosyada** var ve renk sistemi doğru (`--tehlike-dolgu` + beyaz metin = **4.83:1, geçti**).
Ama:
* Yıkıcı işlerin bir kısmı tehlike sınıfı **almıyor** — `Storno` (4 ekran) ve `İptal Et`
  (4 ekran) düğmelerinin tamamı tehlike sınıfı taşımıyor; nötr görünüyorlar.
* `Sil` yalnız **1 ekranda** o etiketle geçiyor; silme işi ağırlıkla "Pasife al" (11 ekran)
  ile yapılıyor — bu aslında **doğru bir ürün kararı** (kuyumcuda kayıt silinmez, pasifleşir)
  ama düğme dili bunu anlatmıyor: "Pasife al" nötr bir ikincil düğme gibi duruyor.

### 1.6 Durum — yükleniyor, kilit ve çift gönderim

| Ölçüt | Değer | Kanıt |
|---|---:|---|
| `<Yukleniyor>` bileşeni | 63 kullanım | tutarlı, **korunmalı** |
| `disabled=` kullanan dosya | 44 | |
| çift gönderim bayrağı (`kaydediliyor`/`gonderiliyor`/`guncelleniyor`…) | **25 dosya** | 44 dosyanın **19'unda bayrak yok** |
| `aria-disabled` | 8 kullanım | |
| **`aria-busy`** | **0** | ekran okuyucu "yükleniyor"u hiç duymuyor |

→ **Çift gönderim koruması dosyaların yarısında yok.** Kuyumcuda çift gönderim = **çift fiş**;
bu, projenin daha önce kapattığı "çifte/üçlü kayıt" hatasının arayüz tarafındaki kalıntısıdır.

**`disabled` gerekçesi:** doğru kalıp **var ama yalnız 2 yerde**:
`Bilesenler/KayitSecici.razor` ("Ara" düğmesi kısa sorguda kapalı + sebebi yanında yazılı,
`aria-disabled` ile) ve `Pages/Giris.razor:137`. Kalan kilitli düğmelerde sebep yazmıyor —
kendi sözleşmesi (DoD EK md.1 / UX-S30 "her düğme ya istek atar ya disabled + sebep")
**44 dosyanın 42'sinde uygulanmamış.**

### 1.7 Yerleşim — hizalama ve boyut

* **Ortak düğme grubu sarmalayıcısı pratikte yok.** `.dugme-satir` (`app.css:212`,
  `display:flex; gap:.5rem`) **yalnız 7 kez / 2 dosyada** kullanılıyor. 431 düğmenin geri
  kalanı her ekranın kendi yerleşimiyle diziliyor → **sağ/sol hizası ve aralığı ekrandan
  ekrana değişiyor.**
* Modal alt satırı için ortak sınıf yok (`modal-alt`/`form-alt` benzeri **2 kullanım**).
* **Sıra tutarsız:** ölçülebilen 3 ikilinin **hepsinde** (`Pages/Uyum.razor:625,657,684`)
  onay düğmesi **önce**, iptal sonra yazılmış. Örneklem küçük ama kural yok — bir yönde
  sabitlenmeli.
* **Canlı yükseklik ölçümü — aynı ekranda dört farklı düğme boyu:**

| Ekran | Görünen düğme | Farklı yükseklikler | 44px altı |
|---|---:|---|---:|
| `/islemler` | 12 | **30 · 35 · 41 · 44 px** | 11 / 12 |
| `/urunler` | 2 | 35 · 41 px | 2 / 2 |
| `/cariler` | 1 | 35 px | 1 / 1 |
| `/` (Panel) | 1 | 31 px | 1 / 1 |

* **Dokunma hedefi:** `--dokunma-hedefi: 2.75rem` (44px) `belirtecler.css:157`'de tanımlı ama
  `.btn`/`.dugme` bunu **kullanmıyor**. Ölçülen: menü aç düğmesi **32×34px**, arama düğmesi
  **36px**, menü bağlantıları **34px**, select **37px** — **hiçbiri 44px'e ulaşmıyor.**
* **İkonlu/ikonsuz karışıklığı:** `<Ikon>` bileşeni var ve yaygın, ama
  `Pages/Kasalar.razor:45` **emoji (`✏️`)**, `Layout/UstBar.razor:73` **ham karakter (`⟳`)`,
  `Pages/BankaEkstre.razor:209,211` **ok karakterleri** kullanıyor — ikon sistemi dışı 4 yer.

---

## § 2. MENÜ VE ARAMA BULGULARI (ölçümle)

### 2.1 Menüde arama kutusu **YOK** — arama üst barda, düğme arkasında

| Soru | Ölçülen cevap |
|---|---|
| Sol menüde arama kutusu var mı? | **HAYIR.** `.kenar-nav` içinde hiç `<input>` yok (ölçüldü: `menudeAramaKutusu: false`). |
| Nerede duruyor? | Üst barda, `#ust-arama-ac` **düğmesi** — 1440px'te `x:310, y:23`, **74×36px**. Sayfa başlığının hemen sağında. |
| Görünür mü? | Evet ama **kutu değil düğme**: "🔍 Ara". Yani kullanıcı bir **arama alanı görmüyor**, bir düğme görüyor. |
| İpucu metni | Panel açılınca: **"Kayıt ya da ekran arayın"** (`Dil.T.Kabuk.AramaIpucu`) — metin doğru ve kapsamı doğru anlatıyor, ama **ancak panel açıldıktan sonra görünüyor**. |

**→ Kurucunun "menüde arama yapamıyorum" şikâyetinin birinci sebebi budur:** arama menüde
değil; üst barda, iki tık arkasında duran bir düğmenin içinde.

### 2.2 Komut paleti var, **kısayolu yok** (tarayıcıda denendi)

| Kısayol | Sonuç |
|---|---|
| `Cmd+K` | **panel açılmadı** (ölçüldü: `cmdK_sonrasiPanelAcikMi: false`) |
| `Ctrl+K` | **panel açılmadı** |
| `Escape` | **panel kapandı** ✔ (tek çalışan kısayol) |

Kaynak doğrulaması: `Bilesenler/GlobalArama.razor:208-211` yalnız `Escape` dinliyor
(`TusaBasildi`); global bir tuş dinleyicisi hiçbir yerde yok.

**Odak yönetimi de yok:** panel açıldığında odak **açma düğmesinde kalıyor**
(ölçüldü: `girdideMiOdak: false`, `acilistaOdak: "ust-arama-ac"`). Kullanıcı paneli açıyor,
sonra **ayrıca kutuya tıklamak zorunda**. Bu, şikâyetin ikinci sebebi.

### 2.3 Arama **neyi buluyor** — ekran adı araması ÇALIŞIYOR

Kurucunun beklentisi *"ekran adıyla aratıp o ekrana gitmek"*. **Bu yetenek var ve çalışıyor.**
Dört terim canlıda denendi:

| Yazılan | "EKRANLAR" bölümünde çıkanlar | Sonuç |
|---|---|---|
| `cari` | **Cariler** | ✔ |
| `kasa` | **Kasalar** · **Kasa Hareketleri** | ✔ |
| `fatura` | **Faturalar** | ✔ |
| `kur` | **Kur** · **Kuruluş Sihirbazı** | ✔ |

Kaynak: `GlobalArama.razor:171-180` — kaynak `MenuTanimi.Ogeler` + `AltBlok`, menüyle **aynı**
yetki ve mod koşulu, en çok **6** sonuç, `MenuTanimi.Sadelestir` ile eşleşme.

**Ama üç kusur ölçüldü:**

1. **Sonuçta grup adı gösterilmiyor.** `kasa` → "Kasalar" (Para ve Banka) ve "Kasa Hareketleri"
   (Dökümler) yan yana çıkıyor; kullanıcı hangisinin ne olduğunu ayırt edemiyor.
2. **Ekran sonuçları 6 ile sınırlı** (`.Take(6)`) ve sınıra değdiği **söylenmiyor** — kayıt
   tarafında "daha var" uyarısı varken (`kabuk-arama-daha-var`) ekran tarafında yok.
3. **⛔ Hata bandı başarılı sonucun ÜSTÜNE biniyor.** Dört denemenin **dördünde de** panelin en
   üstünde kırmızı bant vardı: *"Arama yapılamadı: Sunucu şu anda çok yoğun (istek sınırı)…"*.
   Bu bant **kayıt** aramasının hatası; **ekran** araması aynı anda başarıyla çalışıyordu.
   Kullanıcının gördüğü şey: kırmızı "arama yapılamadı" yazısı. **"Arama çalışmıyor" algısının
   doğrudan kaynağı budur.**

### 2.4 ⛔ EN CİDDİ BULGU — yetki çekilemeyince menü VE arama sessizce çöküyor

375px ölçümü sırasında yakalandı ve kanıt zinciri tamamlandı:

| Adım | Ölçülen |
|---|---|
| 1 | Menü **36 → 1 bağlantıya** düştü (yalnız "Panel" kaldı), grup başlığı 6 → 1 |
| 2 | `.kullanici-ad` DOM'a **hiç basılmadı** (`kullaniciAdiBasilmisMi: false`), kenar-alt yalnız "Kılavuz \| Ayarlar \| Çıkış" → **`Oturum.Kullanici is null`** |
| 3 | Aramaya `cari` yazıldı → **"EKRANLAR" bölümü hiç çizilmedi**, ekranda "Eşleşen kayıt ya da ekran yok." |
| 4 | Hiçbir yerde "yetkiler yüklenemedi" uyarısı **yok** |

**Kök neden (kaynakta kanıtlı):** `Servisler/YetkiServisi.cs:30`

```
catch { moduller = []; }
```

Çıplak `catch`, **her** hatayı (429, ağ kesintisi, JSON hatası) yutuyor ve modül listesini
**boşaltıyor**. `Erisebilir(...)` false dönüyor; `KenarCubugu.Gorunur` ve
`GlobalArama.Gorunur` **aynı** koşulu kullandığı için (`KenarCubugu.razor:74-77`,
`GlobalArama.razor:182-185`) **menü ve komut paleti birlikte boşalıyor**.

Bu, projenin kendi **UX-S28** kuralının ("hata 'yok'a çevrilmez") ve **Anayasa md.1**'in
ihlalidir: geçici bir sunucu hatası, kalıcı ve **sessiz** bir "yetkin yok" ekranına dönüşüyor.

**Dürüst sınır:** tetikleyici bu turda 5199'un **istek sınırıydı** (429). Üretimde 429'un ne
sıklıkta olacağını **ölçmedim**. Ama mekanizma tetikleyiciden bağımsızdır: *herhangi* bir geçici
hata aynı sonucu verir. (Sunucu sakinleşince menü kendiliğinden 36'ya döndü — yani veri
kaybı yok, görünürlük kaybı var.)

### 2.5 Menü yapısı — 6 grup, 36 bağlantı (canlı)

| Grup | Adet | Ekranlar |
|---|---:|---|
| Günlük İş | 5 | Panel · İşlemler · Emanet · Siparişler · Bildirimler |
| Müşteri ve Stok | 6 | Cariler · Ürünler · Ürün Ağacı · Ürün Etiketleri · Fiyat Listeleri · Sayım |
| Para ve Banka | 8 | Kasalar · Vezne · POS Cihazları · POS Hakedişleri · Banka · Çek / Senet · Vade Takvimi · Kur |
| Belge ve Uyum | 3 | Faturalar · Gider Pusulaları · Uyum |
| Dökümler | 6 | Raporlar · Kâr-Zarar · Gecikmiş Alacaklar · Hareket Dökümü · Satış Analizi · Kasa Hareketleri |
| Yönetim | 8 | Kullanıcılar · İşletme · Yetkiler · Denetim İzi · Kuruluş Sihirbazı · Veri Dışa Aktarımı · Veri İçe Aktarımı · Yedekler |
| *(grup dışı alt blok)* | 2+1 | Kılavuz · Ayarlar · **Çıkış** (sınıfsız ham düğme, `KenarCubugu.razor:61`) |

* **Grup başlıkları açılır-kapanır DEĞİL.** Ölçüldü: `<h2>`, `cursor: auto`,
  `aria-expanded: null`, içinde düğme yok. 36 bağlantının tamamı her zaman açık → uzun liste.
  Boş grup başlığı çizilmiyor (`KenarCubugu.razor:31-34`) — bu **doğru** davranış.
* **Ad çarpışması:** birebir aynı iki ad **yok**. Ama yakın adlar arama sonucunda ayırt
  edilemiyor (§ 2.3.1): *Kasalar* / *Kasa Hareketleri*, *POS Cihazları* / *POS Hakedişleri*,
  *Veri Dışa* / *Veri İçe Aktarımı*, *Ürünler* / *Ürün Ağacı* / *Ürün Etiketleri*.
* **Aktif ekran işaretleniyor** ✔ — `NavLink ActiveClass="aktif"`, ölçüldü (`/` → "Panel" aktif).
* **Menüsüz erişilebilir rotalar** (gerekçeli, doğru): `/banka/ekstre/{IthalatId}`,
  `/cikti/{Tur}/{Id}`, `/gider-pusulalari/{Id}/belge`, `/siparisler/{Id}` — hepsi detay
  rotası, menüde olmamaları doğru.

### 2.6 375px davranışı — çekmece doğru, ayrıntılar eksik

| Ölçüt | Ölçülen | Değerlendirme |
|---|---|---|
| Menü biçimi | **Çekmece** — `position:fixed`, genişlik 240px, kapalıyken `translateX(-240px)` | ✔ doğru desen |
| Yatay kaydırma | `clientWidth 375 = scrollWidth 375` | ✔ taşma yok |
| Örtü (overlay) | **var** — `<button class="kenar-ortu">`, `rgba(15,23,42,.45)`, `aria-label="Menüyü kapat"` | ✔ örnek davranış |
| Bağlantıya basınca kapanma | `KenarCubugu.razor:104-105` `Nav.LocationChanged → Kabuk.Kapat()` | ✔ |
| **Sayfa kilidi** | `body overflow: visible`, `html overflow: visible` → **kilitlenmiyor** | ✘ çekmece açıkken arka plan kayabiliyor |
| **Odak yönetimi** | Açılınca odak `H1`'de kalıyor, çekmeceye taşınmıyor; odak tuzağı yok | ✘ klavye kullanıcısı çekmeceye Tab'layarak ulaşmak zorunda |
| Menü düğmesi boyutu | **34×32px** | ✘ 44px altı |
| Menü bağlantısı yüksekliği | **34px** | ✘ 44px altı |
| Arama düğmesi | 375px'te hâlâ **"Ara" metniyle** görünüyor (74×36px) | ⚠ `app.css:1113` `.kabuk-arama-dugme span { display:none }` var ama etiket bir `<span>` içinde değil (`GlobalArama.razor:36` doğrudan metin düğümü) → **kural tutmuyor** |
| **Arama paneli taşması** | Panel genişliği 343px ama **sağ kenarı 628px** (görünüm 375px) → **253px taşma** | ✘ `app.css:1112` genişliği düzeltiyor ama **konumu** düzeltmiyor; panel açma düğmesinin hizasından sağa akıyor |

---

## § 3. BUGÜNKÜ GÖRSEL KİMLİĞİN TEŞHİSİ (sayılarla)

Ölçüm: `belirtecler.css` (375 satır) tanımları + `app.css` (1237 satır) **kullanımları**.

### 3.1 Belirteç sistemi — tanımlar

| Belirteç ailesi | Tanım | Not |
|---|---:|---|
| **Renk** | **172 tanım / 84 benzersiz ad** | açık + koyu tema; 2 koyu blok (`prefers-color-scheme` **ve** `[data-tema]`) |
| Boşluk | 13 tanım / 9 ad | `--bosluk-1…8` + `--bosluk-hucre` |
| Yarıçap | **5** (`sm .375 · .5 · lg .75 · xl 1rem · tam 999px`) | + 3 eski takma ad (`--radius-md/lg/xl`, `--r`) |
| Gölge | **14 tanım / 6 ad** (`sm · (temel) · lg · xl` + `--golge-katman` · `--golge-yuksek`) | |
| Yazı boyutu | **8** (`xs .75 · sm .8125 · md .875 · gövde 14px · lg 1 · xl 1.125 · 2xl 1.5 · 3xl 2.25rem`) | `--yazi-govde` **px**, diğerleri rem — karışık birim |
| Ağırlık | 3 (`500 · 600 · 700`) | |

**Teşhis:** belirteç sistemi **kurulmuş ve iyi**. Sorun tanımda değil, **kullanımda**.

### 3.2 Belirteç KULLANILMAYAN ham değerler — asıl hastalık

`app.css` içinde bildirim başına ham (belirteçsiz) değer oranı:

| Özellik | Toplam bildirim | Belirteç kullanan | **HAM değer** | Farklı ham değer |
|---|---:|---:|---:|---:|
| `border-radius` | 42 | 39 | 3 | 3 ✔ **iyi** |
| `box-shadow` | 6 | 6 | **0** | 0 ✔ **örnek** |
| `font-size` | 90 | 22 | **68** | **20** ✘ |
| `font-weight` | 37 | 4 | **33** | 3 ✘ |
| `gap` | 69 | 30 | **39** | **13** ✘ |
| `padding` | 80 | 21 | **59** | **31** ✘✘ |

**Okunuşu:**
* **Boşluk ölçeği fiilen yok.** 9 boşluk belirteci tanımlı ama `padding` bildirimlerinin
  **%74'ü** ham; **31 farklı** ham dolgu değeri dolaşıyor
  (`.75rem 1rem` ×5, `.375rem .75rem` ×4, `1rem 1.5rem` ×4, `.5rem .75rem` ×3, `2px`, `.25rem`,
  `1rem .75rem`, `.75rem 1.25rem`…). `gap` için 13 farklı ham değer (`.3rem`, `.35rem`, `.6rem`,
  `3mm`, `1mm` dahil).
* **Tipografi ölçeği delinmiş.** 8 boyut tanımlı, **27 farklı** boyut kullanılıyor; bunların
  **20'si ham**. İçinde **pt birimleri** var (`9pt` ×5, `7pt` ×3, `11pt` ×2, `8pt` ×2, `6pt` ×2)
  — bunlar yazdırma sayfalarından geliyor ama ekran kurallarıyla aynı dosyada, ölçekten kopuk.
* **Ağırlıklar elle yazılıyor:** `600` ×18, `700` ×9, `500` ×6 — `--kalin*` belirteçleri
  yalnız 4 kez kullanılmış.
* **Köşe yarıçapı ve gölge disiplinli** — bunlar korunmalı, örnek alınmalı.

### 3.3 Kırılma noktaları

`app.css`: **480px ×3 · 720px ×1 · 768px ×6 · 1024px ×1** → **4 farklı** kırılma noktası,
biri (**720px**, `app.css:137`, kur bandını gizliyor) tek başına kalmış ve 768 ile çakışıyor.
`belirtecler.css`'te medya sorgusu yok (doğru).

### 3.4 Renk paleti — kaç renk gerçekten iş görüyor?

84 benzersiz renk adı, ama işlevsel olarak:
* **Ham palet basamakları (semantik değil):** amber 9 · slate 9 · yeşil 4 · kırmızı 3 ·
  mavi 2 · sarı 2 = **29 ham basamak**. Bunların doğrudan kullanılması yasak olmalı, ama
  `Pages/Giris.razor:69` `background: var(--amber-400)` diye **ham palet** kullanıyor.
* **Semantik katman:** yüzey 4 · metin 3 · kenar 2 · vurgu 5 · başarı 4 · uyarı 4 · tehlike 6 ·
  bilgi 4 · kenar çubuğu 6 · giriş paneli 3 = **41 semantik belirteç**.
* **Grafik paleti:** 5 renk.
* **Üç ayrı "koyu tema" tanımı** aynı değerleri tekrarlıyor (`belirtecler.css:206-260`
  `prefers-color-scheme` ve `:274-…` `[data-tema]`) → **bakım borcu**: bir rengi değiştiren
  iki yeri birden değiştirmek zorunda.

### 3.5 ⚖ Kontrast — mevcut palet **aslında sağlam** (dürüst tespit)

27 renk çifti WCAG 2.1 formülüyle **hesaplandı** (`scratchpad/fe3-kontrast.py`).
**Metin çiftlerinin tamamı AA'yı geçiyor.** Örnekler:

| Çift | Açık tema | Koyu tema |
|---|---:|---:|
| Gövde metni / yüzey | **14.63:1** | **14.05:1** |
| Soluk metin / yüzey | 7.58:1 | 7.96:1 |
| Birincil düğme (doğru eşleşme) | 5.02:1 | 10.17:1 |
| Tehlike düğmesi (beyaz / `#dc2626`) | 4.83:1 | 4.83:1 |
| Bağlantı / vurgu metni | 5.02:1 | 8.80:1 |
| Uyarı metni | 6.38:1 | 9.83:1 |
| Kenarlık / yüzey (≥3:1 gerekli) | 3.16:1 | 6.25:1 |
| Rozet: vurgu metni / vurgu-yüzey | 4.84:1 | 8.36:1 |
| Rozet: tehlike metni / tehlike-yüzey | 5.91:1 | 6.28:1 |
| Kenar çubuğu metni / kenar yüzeyi | 6.96:1 | 7.35:1 |
| Grafik 1-5 / yüzey (≥3:1) | 5.02 – 7.10:1 | — |

**Eşiğin altında kalan 4 çift ve dürüst değerlendirmesi:**

| Çift | Oran | Değerlendirme |
|---|---:|---|
| ⛔ `.btn-birincil` + `.dugme-benzeri` (Panel) | **1.60:1** koyu / **2.91:1** açık | **GERÇEK İHLAL** — § 1.3, canlıda ölçüldü, tek satırlık kusur |
| `--ayrac` / yüzey | 1.23:1 | dekoratif ayraç; `belirtecler.css:101` zaten "kontrast kapsamı dışında" diyor → **ihlal değil** |
| `--vurgu-kenar` / `--vurgu-yuzey` (bant kenarı) | 1.20:1 | bandın iç kenarı; bant zaten zeminle ayrışıyor → **ihlal değil, ama kenar işlevsiz** |
| `--kenar-ayrac` / kenar yüzeyi | 1.72:1 | menü grupları arası ayraç — dekoratif, ama **grup başlıklarını ayırt etmeyi zorlaştırıyor** |

→ **Kontrast ihlali sayısı: 1** (kod kusuru). Palet değerleri kalabilir.

### 3.6 "Önce" hâli — 4 temsil ekranı, her biri tek cümlede

*(Ekran görüntüleri bu turda alındı; koyu tema, 1440×900. Görseller oturum kaydındadır —
belgeye gömülmedi, çünkü bu tur dosya yazmıyor.)*

| Ekran | Kurumsal durmayan şey (tek cümle) |
|---|---|
| **Panel** (`/`) | Tek eylem düğmesi ("Satış Yap") sarı zeminde **okunmayan açık gri metinle** çiziliyor (1.60:1) ve üst bar aynı anda kur bandı + son sorgu + şube kapsamı + mod anahtarı + arama düğmesini sıkıştırdığı için sayfanın asıl işi görsel olarak ikinci planda kalıyor. |
| **İşlemler** (`/islemler`) | Aynı formda **dört farklı düğme yüksekliği** (30/35/41/44px) ve üç ayrı düğme sistemi (`.btn`, `ciplak:sekme`, tam genişlik birincil) yan yana duruyor; 8 sekme + 4 gri sistem dropdown'u + ortada kırmızı hata bandı ile ekran "tezgâh aracı" değil "form yığını" gibi görünüyor. |
| **Ürünler** (`/urunler`) | Süzgeç satırındaki 4 dropdown (*Tüm Madenler / Tüm Ayarlar / Tüm kategoriler / Tüm reyonlar*) **işletim sisteminin gri açılır listesini** kullanıyor; kapalı hâlleri projenin koyu yüzeyinde, açık hâlleri sistem grisinde — kurucunun şikâyet ettiği tam da bu kopukluk. |
| **Cariler** (`/cariler`) | Sayfa başlığı, arama kutusu, "Tümü" dropdown'ı, "Pasifleri göster" onay kutusu ve "+ Yeni Cari" düğmesi **tek bir hizalama kuralı olmadan** aynı satıra dizilmiş; düğme 35px, dropdown 37px, arama kutusu 35px — üç farklı yükseklik yan yana. |

---

## § 4. ÖNERİLEN KURUMSAL GÖRSEL DİL (kontrast oranları hesaplanmış)

**Tasarım ilkesi:** kuyumcu ekranı gün boyu bakılan bir **tezgâh aracıdır**. Süs değil sadelik;
para ve gram rakamları en okunur öğe olmalı. Bu yüzden öneri **paleti değiştirmek değil,
kullanımı daraltmaktır** — çünkü § 3.5'te ölçüldüğü gibi mevcut renkler zaten AA'yı geçiyor.
Renkleri değiştirmek, geçen 27 çifti yeniden riske atmak olurdu.

### 4.1 Renk — 1 ana + 1 vurgu + nötr ölçek + 4 durum

| Rol | Açık tema | Koyu tema | Gerekçe |
|---|---|---|---|
| **Ana (marka/vurgu)** | `--vurgu` **#b45309** | **#f0b429** | Amber = altın; kuyumcu için doğru ve zaten kurulu |
| **Ana üstü metin** | `--vurgu-metin` #ffffff | #14100a | |
| **Ana (koyu hâl, hover)** | `--vurgu-guclu` #92400e | #fcd34d | |
| **Nötr ölçek (5 kademe)** | yüzey #ffffff · yüzey-2 #f8fafc · yüzey-3 #f1f5f9 · kenarlık #8592a5 · metin #1e293b | #16202e · #1b2634 · #223044 · #93a1b4 · #e8eef6 | 9 slate basamağı **5'e** iner |
| **Durum (yalnız 4)** | başarı #047857 · uyarı #8a5206 · tehlike #b91c1c · bilgi #1d4ed8 | #34d399 · #fbbf24 · #f87171 · #93c5fd | dekorasyon için **kullanılmaz** |

**Kaldırılacaklar:** 29 ham palet basamağı doğrudan kullanımdan çıkar (yalnız semantik
belirteçleri beslerler); grafik paleti **5 → 3**'e iner.

**Hesaplanmış kontrast — önerilen 3 renkli grafik ölçeği:**

| Seri | Açık tema / beyaz | Koyu tema / #16202e | Eşik |
|---|---:|---:|---:|
| grafik-1 (#b45309 / #f0b429) | **5.02:1** ✔ | **8.80:1** ✔ | ≥3:1 |
| grafik-2 (#1d4ed8 / #60a5fa) | **6.70:1** ✔ | **6.45:1** ✔ | ≥3:1 |
| grafik-3 (#047857 / #34d399) | **5.48:1** ✔ | **8.53:1** ✔ | ≥3:1 |

**Düzeltilecek 2 değer (hesaplandı):**
* `--kenar-ayrac` #334155 → **#3d4f68** (kenar yüzeyi #0f172a'ya karşı **2.55:1**; 3:1 için
  **#47597a** = **3.16:1** ✔) — menü grupları görünür ayrılsın diye.
* `--vurgu-kenar` / `--tehlike-kenar` bant kenarları: ya 3:1'e çıkarılsın ya da **kaldırılsın**
  (şu hâlleriyle 1.20:1 ve 1.32:1 — görünmüyorlar, yalnız kod yükü).

### 4.2 Boşluk — 6 kademe (31 ham değer yerine)

| Ad | Değer | Kullanım |
|---|---|---|
| `--bosluk-1` | `.25rem` (4px) | ikon-metin arası, rozet içi |
| `--bosluk-2` | `.5rem` (8px) | düğme grubu aralığı, alan-etiket arası |
| `--bosluk-3` | `.75rem` (12px) | girdi iç dolgusu (dikey), tablo hücresi |
| `--bosluk-4` | `1rem` (16px) | kart iç dolgusu, form alanları arası |
| `--bosluk-5` | `1.5rem` (24px) | kart arası, bölüm içi |
| `--bosluk-6` | `2rem` (32px) | sayfa bölümleri arası |

`--bosluk-7` (3rem) ve `--bosluk-8` (4rem) **yalnız sayfa üst/alt payı** için kalır.
`3mm`/`1mm` gibi yazdırma değerleri **ayrı bir yazdırma bloğuna** taşınır.

### 4.3 Tipografi — 5 kademe (27 farklı boyut yerine)

| Ad | Değer | Ağırlık | Kullanım |
|---|---|---|---|
| `--yazi-xs` | `.75rem` (12px) | 500 | rozet, yardım metni, tablo alt bilgisi |
| `--yazi-sm` | `.8125rem` (13px) | 500 | ikincil metin, sütun başlığı |
| `--yazi-md` | `.875rem` (14px) | 400/500 | **gövde — varsayılan**, düğme, girdi |
| `--yazi-lg` | `1.125rem` (18px) | 600 | kart başlığı, bölüm başlığı |
| `--yazi-xl` | `1.5rem` (24px) | 600 | sayfa başlığı, KPI rakamı |

**Ağırlık yalnız 3:** 400 (gövde) · 500 (vurgulu gövde, düğme) · 600 (başlık).
`700` kullanımdan kalkar (9 yerde var, `600` yeterli).
**Rakamlar için ayrı kural:** para/gram/has değerleri `--yazi-sayi` (tabular) +
`font-variant-numeric: tabular-nums` + sağa dayalı — bu zaten `.sayi` sınıfında var, **yaygınlaştırılmalı**.
`--yazi-govde: 14px` → `.875rem`'e çevrilir (birim karışıklığı biter).
`pt` birimli 14 bildirim **yalnız `@media print` bloğuna** taşınır.

### 4.4 Köşe yarıçapı — tek aile, 3 kademe

| Ad | Değer | Kullanım |
|---|---|---|
| `--yaricap-sm` | `.375rem` | rozet, küçük düğme, ikon düğmesi |
| `--yaricap` | `.5rem` | **düğme, girdi, dropdown — varsayılan** |
| `--yaricap-lg` | `.75rem` | kart, modal, panel |

`--yaricap-xl` (1rem) kaldırılır; `--yaricap-tam` (999px) **yalnız** pill/rozet için kalır.
Eski takma adlar (`--radius-md/lg/xl`, `--r`) **tek tek** asıllarına çevrilir.

### 4.5 Gölge — iki kademe

| Ad | Değer | Kullanım |
|---|---|---|
| `--golge-sm` | `0 1px 2px rgb(0 0 0 / .06)` | kart, tablo başlığı — **yüzeyde duran** her şey |
| `--golge-lg` | `0 10px 25px rgb(15 23 42 / .12)` | modal, çekmece, açılır liste — **yüzeyin üstünde uçan** her şey |

`--golge`, `--golge-xl`, `--golge-katman`, `--golge-yuksek` kaldırılır (6 → 2).
Koyu temada aynı iki ad, daha koyu değerlerle tanımlanır (zaten öyle).

### 4.6 Kırılma noktaları — 3 tane

`480px` (küçük telefon) · `768px` (tablet/çekmece eşiği) · `1024px` (masaüstü).
**`720px` kaldırılır** → `768px`'e taşınır (`app.css:137`).

---

## § 5. TEK TİP DÜĞME ŞARTNAMESİ

### 5.1 Tek sistem: `.dugme`

**Karar:** `.btn` ailesi **kaldırılır**, `.dugme` ailesi tek sistem olur.
*Gerekçe:* (a) proje dili Türkçe, sınıf adları da Türkçe olmalı; (b) `.dugme-satir` gibi
yardımcılar zaten bu ailede; (c) `EkranTestleri` hâlihazırda `.dugme-birincil`e bağlı
(`app.css:190-196` yorumu bunu söylüyor) — ters yön testleri kırardı.
*Geçiş:* `.btn` seçicileri `.dugme`'nin **takma adı** olarak bir sürüm boyunca korunur,
253 kullanım dosya dosya çevrilir, sonra takma ad silinir.

### 5.2 Varyantlar — beş tane, fazlası yok

| Varyant | Görünüm | Ne zaman | Ekran başına |
|---|---|---|---|
| `.dugme-birincil` | dolu vurgu zemin + `--vurgu-metin` | ekranın **asıl** eylemi | **en çok 1** |
| `.dugme-ikincil` | `--yuzey` zemin + `--kenarlik` çerçeve | yan eylemler | serbest |
| `.dugme-sessiz` *(yeni)* | zeminsiz, çerçevesiz, yalnız metin/ikon | satır içi eylem, "Düzenle", tablo hücresi | serbest |
| `.dugme-tehlike` | dolu `--tehlike-dolgu` + beyaz | **yıkıcı** eylem (iptal et, storno, sil) | serbest |
| `.dugme-baglanti` *(yeni)* | `<a>` için; düğme görünümü, bağlantı anlamı | gezinme (`dugme-benzeri` bunun yerine geçer) | serbest |

**`.dugme-sessiz` neden gerekli:** bugün 62 çıplak sınıflı düğmenin çoğu (`ikincil` 28,
`secici-sonuc` 3, ikon düğmeleri) tam olarak bu ihtiyacı karşılamaya çalışıyor.

**Boyut:** `.dugme-kucuk` (tablo satırı) · varsayılan · `.dugme-buyuk` (giriş/kurulum).
**`.dugme-tam`** (tam genişlik) yalnız dar ekran ve tek eylemli formlarda.

### 5.3 Zorunlu kurallar

1. **Ekranda tek birincil.** İkinci bir "asıl eylem" gerekiyorsa biri ikincile iner.
   *Ölçülebilir kapı:* `denetim.sh` sayacı `BIRDEN_COK_BIRINCIL = 0` (bugün **22 dosya**).
2. **Yıkıcı eylem hep `.dugme-tehlike`.** `Storno`, `İptal Et`, `Sil`, `Pasife al` bu sınıfı
   alır. *Kapı:* bu etiketlerden biri tehlike sınıfı olmadan geçmez.
3. **Tek metin, tek iş.** Sözlükte iş başına **tek** anahtar:

   | İş | **Tek etiket** | Yerine geçtikleri |
   |---|---|---|
   | kaydet/oluştur | **Kaydet** | Ekle, Uygula, Evet, Oluştur |
   | onayla (geri dönüşsüz onay) | **Onayla** | Evet, Tamam |
   | vazgeç (hiçbir şey yapma) | **Vazgeç** | İptal, Kapat *(kapatma için)*, Geri |
   | pencereyi kapat | **Kapat** | ✕, &times; *(ikon + `aria-label="Kapat"`)* |
   | kaydı iptal et (yıkıcı) | **İptal et** | İptal Et, Storno |
   | düzenle | **Düzenle** | Düzelt, Değiştir |
   | pasife al | **Pasife al** | Pasife Al, Sil |

   → **`İptal` kelimesi tek başına YASAKLANIR** (iki anlamlı). "Vazgeç" (nötr) ve
   "İptal et" (yıkıcı) ayrışır.
   *Kapı:* `Sozlukler.cs`'te bir anahtar adı **tek** metne bağlanır; bugün 150 anahtar
   çakışıyor, düğme anahtarları (`Iptal`, `Kaydet`, `Kapat`, `Onayla`, `Duzenle`, `Yazdir`)
   önce çözülür.
4. **Her düğmenin görünür metni olur.** Yalnız ikonlu düğme `aria-label` **ve** `title`
   taşır (bugün 7 düğmenin etiketi yok).
5. **`type` zorunlu.** `<form>` içinde olmasa bile: `type="button"` varsayılan.
6. **Çift gönderim koruması zorunlu.** Ağ isteği atan her düğme:
   `disabled="@calisiyor"` + `aria-busy="@calisiyor"` + metin değişimi.
   *Kapı:* istek atan düğmesi olup bayrağı olmayan dosya = 0 (bugün **19 dosya**).
7. **Kilidin sebebi yazılır.** `disabled` olan her düğme `title` **ve** görünür bir yardım
   metniyle sebebini söyler (`KayitSecici` kalıbı örnek alınır) + `aria-disabled`.
8. **Dokunma hedefi ≥ 44px.** `.dugme { min-height: var(--dokunma-hedefi) }`;
   `.dugme-kucuk` **36px**'in altına inmez ve yalnız işaretleyici cihaz beklenen tablo
   satırlarında kullanılır.
9. **Grup yerleşimi tek sınıfla.** Her düğme grubu `.dugme-satir` içine girer
   (bugün **7 kullanım**; hedef: her grup). Modal/form alt satırı `.dugme-satir--alt`:
   `justify-content: flex-end`, **sıra: ikincil (Vazgeç) solda, birincil (Kaydet) sağda**,
   yıkıcı eylem **en solda ve ayrık** (`margin-right: auto`).
10. **İkon disiplini.** Yalnız `<Ikon>` bileşeni; emoji (`✏️`) ve ham karakter (`⟳`, `←`, `→`,
    `✕`) yasak. Aynı işin ikonu her ekranda aynı.
11. **Odak tek kaynaktan.** `:focus-visible` grubu (`app.css:689-704`) tek yetkili;
    `app.css:230`'daki `.girdi:focus` kuralı kaldırılır (fareyle tıklayınca halka çiziyor ve
    kenarlığı siliyor — iki sistem çakışıyor).
12. **Düğme gibi görünen 13 `<a>`** `.dugme-baglanti` alır; `.btn-birincil` ile **birlikte
    kullanılmaz** (§ 1.3'teki hatanın kaynağı).

### 5.4 Hemen düzeltilecek tek satır

`Pages/Panel.razor:26` → `class="btn btn-birincil dugme-benzeri"` içindeki `dugme-benzeri`
kaldırılır (ya da `.dugme-benzeri` kuralından `color` bildirimi çıkarılır).
Bu tek değişiklik **1.60:1 → 10.17:1** yapar. *(Bu tur yazılmadı; uygulama turuna devredildi.)*

---

## § 6. ORTAK SEÇİM BİLEŞENİ ŞARTNAMESİ — `<Secici>`
### (kurucunun 2026-09-21 talimatı: *"hepsi aynı olmalı… içinde arama… bir tane bize özel şık dropdown… tek seferde değiştiririz"*)

### 6.1 Teknik gerçek: kurucu haklı — native `<select>` biçimlendirilemez

**Ölçüldü** (`/urunler`, `/islemler`, `/cariler`, canlı):
`appearance: auto` · `background-image: none` (özel ok **yok**) · `<option>` hesaplanmış
dolgusu `0px 2px 1px`, yarıçapı `0px`, zemini **şeffaf**.

* **Kapalı hâl** stillenmiş: `--yuzey` zemin, `--kenarlik` çerçeve, `8px` yarıçap, 37px yükseklik.
* **Açık hâl** (açılır liste) **işletim sistemi tarafından, sayfa dışında çizilir.** CSS oraya
  ulaşmaz: yükseklik, satır aralığı, seçili işareti, kaydırma çubuğu, yazı tipi ve zemin
  **bizim değil**. Koyu temada bile sistem grisi görünür — kurucunun ekran görüntüsündeki
  kopukluk tam olarak budur.
* Sonuç: **CSS ile çözülemez. Tek çözüm özel bir bileşendir.**

### 6.2 Envanter — 176 `<select>`, kategorilere ayrılmış

| Kategori | Adet | Ne demek | Arama kutusu |
|---|---:|---|---|
| **KISA-SABİT** | **41** | Kodda yazılı, ≤12 seçenek (tür, durum, yön, para birimi, ay) | seçenek > 8 ise |
| **DEĞİŞKEN-LİSTE** | **108** | Sunucudan gelen ama sınırlı küme (kasa, şube, banka, rol, kategori, reyon, fiyat listesi) | **evet, istemcide** |
| **BÜYÜK-KÜME** | **27** | Cari, ürün, fiş, hareket — binlerce kayıt olabilir | **evet, sunucuda** |

**Dosya dağılımı (kilitli işaretli):**

| Dosya | `<select>` | Dosya | `<select>` |
|---|---:|---|---:|
| `Pages/Urunler.razor` | **34** | `Pages/Isletme.razor` | 4 |
| `Pages/Islemler.razor` | **21** | `Pages/Kasalar.razor` | 4 |
| `Pages/Cariler.razor` | **14** | `Pages/Raporlar.razor` 🔒 | 4 |
| `Pages/Faturalar.razor` 🔒 | 10 | `Pages/DenetimIzi.razor` | 3 |
| `Pages/Kullanicilar.razor` | 8 | `Pages/Hurda.razor` | 3 |
| `Pages/Evraklar.razor` | 7 | `Pages/Sayim.razor` 🔒 | 3 |
| `Pages/Vezne.razor` | 7 | `Pages/Uyum.razor` | 3 |
| `Pages/Banka.razor` | 6 | `BankaEkstre` · `Kur` · `Kurulum` · `VeriIceAktarim` | 2'şer |
| `Pages/Siparisler.razor` 🔒 | 5 | `Pos.razor` 🔒 | 2 |
| `Pages/UrunAgaci.razor` | 5 | `KarZarar` 🔒 · `KasaRaporu` 🔒 · `Panel` · `PosTahsilatlar` · `Vitrin` | 1'er |
| `Emanet` · `Fiyatlandirma` · `Gider` · `IslemDefteri` | 4'er | `BelgeYukleyici` · `FiyatListesiUrunPaneli` · `MasakFormu` · `SubeSecici` | 1'er |

**Toplam 176 = 150 SERBEST + 26 KİLİTLİ.**

**27 BÜYÜK-KÜME adayının tamamı** (arama kutusu **zorunlu**):
`Banka:314` · `BankaEkstre:145` · `Cariler:295,322` · `Emanet:34` · `Evraklar:245` ·
`Islemler:58,358,687,757` · `UrunAgaci:231` · `Urunler:22,375,431,894,914` · `Uyum:291` ·
`Vezne:68,90,343,362` · 🔒`Faturalar:158,209,250,415` · 🔒`Siparisler:83,150`.

**Mevcut tutarsızlıklar (envanterden):**
* Sınıf: `.secim` 142 · **`.girdi` 23** (yanlış sınıf) · koşullu sınıf 11.
* Boş seçenek: **yok** 67 · **`—`** 52 (anlamsız) · `"… Seçin"` ~30 · `"Tümü"` ~11 · melez 2.
* **`id` yok: 6** (170'inde var — eleman kimliği sözleşmesi büyük ölçüde tutmuş).
* **Arama yapabilen dropdown: 0.**

### 6.3 Karar: **tek bileşen, iki kip** (gerekçeli)

**İki ayrı bileşen değil, tek `<Secici>` — `Kaynak` parametresiyle iki kip.**

*Gerekçe:*
1. Kurucunun asıl isteği *"beğenmediğimiz bir yer olursa **tek seferde** değiştiririz"*.
   İki bileşen = iki görünüm = iki bakım yeri; ilk isteği baştan bozar.
2. Açılan liste, klavye, odak, dar ekran davranışı, kontrast ve ARIA **her iki kipte
   birebir aynıdır**. Farklı olan tek şey *seçeneklerin nereden geldiğidir* — bu bir
   **veri kaynağı** farkıdır, görünüm farkı değil.
3. Ayırmak, § 1'deki `.btn`/`.dugme` hatasını dropdown'da tekrarlamak olur.

**`KayitSecici` ile ilişki: `<Secici>` onu YUTAR.**
`KayitSecici` (P-15) bugün *açılır liste değil*, **sürekli açık** bir arama kutusu + sonuç
listesidir; yalnız **4 yerde** kullanılıyor (`PosTahsilatlar:159`, `Uyum:602`, `Hurda:195`,
🔒`Faturalar:492`) ve yalnız ULID'li varlık türlerini biliyor. İçindeki **değerli mekanizma
korunur ve `<Secici>`'nin `Uzak` kipine taşınır**: sunucuda arama, 250ms bekleme,
**istek sırası ile yarış koşulu koruması**, "en çok N kayıt" uyarısı, hata ≠ boş durum ayrımı
(UX-S28), ULID'i ekrana basmama (B-25). Göç bitince `KayitSecici` **kaldırılır** —
aynı işi yapan iki bileşen bırakılmaz.

**`SubeSecici` ile ilişki:** o bir *kabuk bileşenidir* (kapsam göstergesi + rozet + "tek şube
ise seçici gösterme" kuralı). **Kalır**, ama içindeki ham `<select>`
(`SubeSecici.razor:35-43`) `<Secici>` ile değiştirilir. Bugün 1 kullanımı var (UstBar).

### 6.4 Bileşen sözleşmesi

```
Bilesenler/Secici.razor
```

| Parametre | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `ElemanId` | `string` | ✔ | **Mevcut `<select>` id'si birebir korunur** (170 id değişmez). Türetilenler: `<ElemanId>-girdi`, `-liste`, `-secenek-<n>`, `-temizle`, `-durum`, `-hata`, `-ara` |
| `Etiket` | `string` | ✔ | Görünür `<label for="@ElemanId">` |
| `Deger` | `string?` | — | Seçili değer (ULID ya da kod) |
| `DegerChanged` | `EventCallback<string?>` | — | İki yönlü bağlama |
| `Secenekler` | `IReadOnlyList<SecenekOgesi>?` | kip: **Yerel** | Kısa/değişken liste |
| `UzakTur` | `string?` | kip: **Uzak** | `AramaTurleri` sabiti; verilirse sunucuda aranır |
| `BosSecenekMetni` | `string?` | — | `null` → boş seçenek **yok**; aksi hâlde metin |
| `YerTutucu` | `string?` | — | Kapalı hâlde, seçim yokken görünen metin |
| `Zorunlu` | `bool` | — | `false` ise etiket yanına "(isteğe bağlı)"; `true` ise `aria-required` |
| `Devredisi` | `bool` | — | |
| `DevredisiSebebi` | `string?` | — | **`Devredisi` ise ZORUNLU** — `title` + görünür durum metni (DoD EK md.1) |
| `Hata` | `string?` | — | Alan altı hata; `.hatali` sınıfı + `aria-invalid` |
| `Coklu` | `bool` | — | Çoklu seçim (**ilk sürümde `false` sabit**; bugün ihtiyaç yok, sözleşmede yeri açık) |
| `AramaEsigi` | `int` | — | Varsayılan **8**: bu kadar seçenekten çoksa arama kutusu çıkar |
| `EnFazlaYukseklik` | `int` | — | Varsayılan **320px** |

```csharp
public sealed record SecenekOgesi(
    string Deger,          // gövdeye giden kod/ULID — EKRANA BASILMAZ
    string Ad,             // görünen ad
    string? AltBilgi = null,   // ikinci satır (telefon, kod, bakiye)
    string? RozetMetni = null, // tür rozeti
    bool Pasif = false);       // seçilemez ama görünür (sebebiyle)
```

**Kip seçimi:** `UzakTur` doluysa **Uzak**, değilse **Yerel**. İkisi birden verilirse
derleme zamanı değil, **çalışma zamanı istisnası** (sessiz kip seçimi yok — md.1).

### 6.5 Arama kutusu

| Konu | Karar |
|---|---|
| **Ne zaman görünür** | Yerel kipte seçenek sayısı **> 8**; Uzak kipte **her zaman** |
| **Nerede arar** | Yerel: istemcide, yüklü liste üzerinde · Uzak: `/arama?q=…&limit=10` (+ `&sube=` kabuk kapsamı) |
| **Asgari sorgu** | Uzak: **2 karakter** (`AramaServisi.AsgariSorgu` ile aynı); altındaysa sebebi yazılı (*"Aramak için en az 2 karakter yazın."*) |
| **Gecikme** | **250ms** (`KayitSecici.BeklemeMs` değeri korunur). Yerel kipte gecikme **yok** (anında süzer) |
| **Yarış koşulu** | İstek sıra numarası: geç gelen yanıt erken yanıtın üstüne **yazılmaz** (`KayitSecici`'den taşınır) |
| **Türkçe harf duyarlılığı** | **`MenuTanimi.Sadelestir`** ile aynı normalleştirme: küçültme `tr-TR` kültürüyle **değil**, harf haritasıyla (`İ→i`, `I→ı→i`, `ş→s`, `ğ→g`, `ö→o`, `ç→c`, `ü→u`) yapılır ve **her iki yön** eşlenir → *"ısparta"* yazınca **"Isparta"**, *"ISPARTA"* ve *"İsparta"* bulunur. Aksan duyarsız, büyük/küçük duyarsız, `Ordinal` karşılaştırma. **Bu kural bileşende değil, ortak yardımcıda durur ve GlobalArama ile paylaşılır** (iki normalleştirme yasak) |
| **Eşleşme biçimi** | Ad **içinde geçen** (substring), baştan eşleşenler **önce** sıralanır |
| **Sonuç yokken** | Yerel: *"Eşleşen seçenek yok."* · Uzak: `<BosDurum>` *"Eşleşen kayıt yok."* — **yalnız istek başarılı ve sonuç gerçekten boşken** (UX-S28) |
| **Hata durumunda** | Ayrı `<HataBandi>`; **"sonuç yok"a çevrilmez**. Liste son bilinen hâlinde kalır |
| **Sınıra değince** | *"En çok N kayıt gösterilir; daraltmak için yazmaya devam edin."* (UX-S29) |

### 6.6 Klavye ve erişilebilirlik

**Yapı:** `role="combobox"` + `aria-expanded` + `aria-controls` + `aria-haspopup="listbox"`;
liste `role="listbox"`, öğeler `role="option"` + `aria-selected`;
etkin öğe `aria-activedescendant="<ElemanId>-secenek-<n>"`.
Odak **girdide kalır** (öğelere taşınmaz) — ekran okuyucu etkin öğeyi `aria-activedescendant`
üzerinden okur.

| Tuş | Davranış |
|---|---|
| `Enter` / `Space` / `↓` (kapalıyken) | Listeyi **aç**, ilk (ya da seçili) öğeyi etkin yap |
| `↑` / `↓` | Etkin öğeyi değiştir, görünür alana kaydır; **liste başında/sonunda durur** (sarmaz) |
| `Home` / `End` | İlk / son öğe |
| `PageUp` / `PageDown` | 10 öğe atla |
| `Enter` | Etkin öğeyi **seç**, listeyi kapat, odak girdide kalır |
| `Esc` | Kapat, **değeri değiştirmeden**; arama metnini temizle. İkinci `Esc` seçimi temizler (`Zorunlu` değilse) |
| `Tab` | Kapat ve **seçimi uygula** (etkin öğe varsa), sonraki alana geç |
| Harf yazma | Arama kutusuna gider; arama kutusu yoksa **ilk eşleşen öğeye atlar** (typeahead) |
| Dışarı tıklama | Kapat, değeri değiştirme |

**Odak halkası:** `--odak-halka` tek kaynağından, `:focus-visible` ile
(`app.css:689-704` grubuna `.secici-tetik` eklenir). Fareyle tıklamada halka **çizilmez**.

### 6.7 Görünüm — yalnız belirteçlerle (kontrast hesaplanmış)

**Tetikleyici (kapalı hâl) — `<select>` ile aynı ölçüler, değişiklik yok:**

| Özellik | Değer |
|---|---|
| yükseklik | `min-height: var(--dokunma-hedefi)` → **44px** (bugün 37px) |
| iç dolgu | `var(--bosluk-2) var(--bosluk-3)` |
| kenarlık | `1px solid var(--kenarlik)` |
| köşe | `var(--yaricap)` (.5rem) |
| yazı | `var(--yazi-md)` |
| zemin / metin | `var(--yuzey)` / `var(--metin)` |
| **ok ikonu** | `<Ikon>` bileşeninden, `var(--metin-soluk)` — **artık sistem oku değil** |

**Açılan liste:** `var(--yuzey)` zemin · `1px solid var(--kenarlik)` · `var(--yaricap)` ·
**`var(--golge-lg)`** · `z-index` modal katmanının **altında** · genişlik tetikleyiciyle eşit
(en az; içerik taşarsa 1.5 katına kadar).

**Durumlar:**

| Durum | Görünüm |
|---|---|
| kapalı | yukarıdaki temel |
| **hover** | zemin `var(--yuzey-2)` |
| **odaklı** | `outline: 2px solid var(--odak-halka)`, `outline-offset: 2px`; **kenarlık silinmez** (§ 5.3 md.11) |
| **açık** | tetikleyici kenarlığı `var(--vurgu)`, ok 180° döner |
| **hatalı** | kenarlık `var(--tehlike)` + `aria-invalid="true"` + alan altı `.alan-hata` |
| **devre dışı** | `opacity: .6`, `cursor: not-allowed`, **sebebi yazılı** |
| **seçenek: etkin** | zemin `var(--yuzey-3)` |
| **seçenek: seçili** | zemin `var(--vurgu-yuzey)`, metin `var(--vurgu)`, **solda ✓ ikonu** (yalnız renkle anlatılmaz) |
| **seçenek: pasif** | `opacity:.6`, seçilemez, sebebi `AltBilgi`de |

**Hesaplanmış kontrast oranları:**

| Çift | Açık tema | Koyu tema | Eşik | Sonuç |
|---|---:|---:|---:|---|
| Seçenek metni / liste zemini (`--metin`/`--yuzey`) | **14.63:1** | **14.05:1** | 4.5 | ✔ |
| Alt bilgi (`--metin-soluk`/`--yuzey`) | **7.58:1** | **7.96:1** | 4.5 | ✔ |
| Etkin öğe metni / `--yuzey-3` | **12.11:1** *(#1e293b/#f1f5f9)* | **11.16:1** *(#e8eef6/#223044)* | 4.5 | ✔ |
| **Seçili öğe: `--vurgu` / `--vurgu-yuzey`** | **4.84:1** | **8.36:1** | 4.5 | ✔ |
| Tetikleyici kenarlığı / yüzey | **3.16:1** | **6.25:1** | 3.0 | ✔ |
| Açık hâl kenarlığı (`--vurgu`) / yüzey | **5.02:1** | **8.80:1** | 3.0 | ✔ |
| Hata kenarlığı (`--tehlike`) / yüzey | **6.47:1** | **5.93:1** | 3.0 | ✔ |
| Odak halkası / yüzey | **5.02:1** | **8.80:1** | 3.0 | ✔ |
| Yer tutucu (`--metin-soluk`) / yüzey | **7.58:1** | **7.96:1** | 4.5 | ✔ |

→ **Önerilen dropdown'ın 9 renk çiftinin tamamı AA'yı geçiyor** (her iki temada).

### 6.8 Dar ekran (375px)

**Karar: tam ekran seçim katmanı** (aşağı açılan liste değil).

*Gerekçe:* 375px'te tetikleyicinin altında ortalama 300px yer kalıyor; arama kutusu +
6 seçenek + kaydırma sığmıyor ve § 2.6'da ölçülen **arama paneli taşması** (253px) aynı
hatanın dropdown'da tekrarı olurdu.

| Kural | Değer |
|---|---|
| Sunum | `position: fixed; inset: 0` — üstte başlık + arama, altta liste |
| Başlık | Alan etiketi + **"Kapat"** düğmesi (44px) |
| Seçenek satırı | **min 48px**, tam genişlik, ayraçlı |
| Sayfa kilidi | Katman açıkken `body { overflow: hidden }` — **§ 2.6'da çekmecede eksik olan kural burada zorunlu** |
| Odak | Katman açılınca odak **arama kutusuna** taşınır (§ 2.2'deki hata tekrarlanmaz), katman içinde **odak tuzağı**, kapanınca odak tetikleyiciye döner |
| Geçiş eşiği | `768px` (tek kırılma noktası) |

### 6.9 Uzun liste

* Liste **en fazla `320px`** yüksek (yaklaşık 7 satır), sonrası **liste içinde** kaydırılır.
* `overscroll-behavior: contain` → liste sonuna gelince **sayfa kaymaz**.
* **200'den çok seçenek** yerel kipte yüklenmez; bu sayıyı aşan kaynak **Uzak kipe geçmek
  zorundadır** (`AramaServisi`). *Gerekçe:* bugün bazı ekranlar `?limit=200` ile listeyi
  dolduruyor ve 200. kayıttan sonrası **sessizce yok** (UX-S29 ihlali, `KayitSecici` yorumunda
  kayıtlı). Bileşen bu durumda **uyarı basar**, sessiz kırpmaz.
* Sanallaştırma **ilk sürümde yok** (320px'lik pencere için gereksiz karmaşıklık).

### 6.10 Göç planı — 176 yer, 5 dalga

**Sıra ilkesi:** *önce en riskli, sonra en kalabalık.* Gerekçe: bileşenin kusuru en zor
ekranda erken görünsün; kolay ekranlar düzeltilmiş bileşene göç etsin.

| Dalga | Kapsam | Adet | Neden bu sırada |
|---:|---|---:|---|
| **0** | Bileşeni yaz + `Bilesenler/SubeSecici.razor:35` ve **`Pages/Panel.razor`** (1 select) ile pilot | 2 | En küçük yüzey; kabukta her sayfada görünür → kusur anında belli olur |
| **1** | **27 BÜYÜK-KÜME** (Uzak kip) — `Islemler`, `Vezne`, `Cariler`, `Urunler`, `Emanet`, `Banka`, `BankaEkstre`, `Evraklar`, `UrunAgaci`, `Uyum` | 21 serbest (+6 🔒) | **En riskli**: cari/ürün seçimi yanlışsa yanlış fiş kesilir. `KayitSecici`'nin 4 kullanımı da burada emilir |
| **2** | `Pages/Urunler.razor` (34) + `Pages/Islemler.razor` (21) kalanı | ~48 | **En kalabalık iki ekran**; bileşen burada ayakta kalırsa her yerde kalır |
| **3** | `Cariler` (14) · `Kullanicilar` (8) · `Evraklar` (7) · `Vezne` (7) · `Banka` (6) · `UrunAgaci` (5) kalanları | ~35 | Orta yoğunluk |
| **4** | Kalan 20 serbest dosya (1–4 select) | ~44 | Kuyruk temizliği |
| **—** | **26 KİLİTLİ select** — 🔒`Faturalar`(10) `Siparisler`(5) `Raporlar`(4) `Sayim`(3) `Pos`(2) `KarZarar`(1) `KasaRaporu`(1) | 26 | **BU TURDA DOKUNULMAZ**; bulgu ilgili P-xx paketine devredilir |

**Her dalganın kapısı:** `dotnet test` 0 başarısız + `dotnet/denetim.sh` TEMİZ + canlıda
gerçek veriyle doğrulama + mutasyon kanıtı.

**Göç sırasında uyulacak dönüşüm kuralları:**
* `id` **değişmez** (170 id korunur; id'siz 6 select'e id **eklenir**).
* `class="girdi"` yazan **23 select** `<Secici>`'ye geçerken bu yanlış sınıftan kurtulur.
* Boş seçenek metni normalleşir: **`—` (52 yer) yasaklanır**; süzgeçlerde `Tümü`,
  formlarda `"<Alan> seçin"`, isteğe bağlı alanlarda `BosSecenekMetni=null` + `YerTutucu`.
* Seçenek sırası **açıkça** belirtilir (bugün API sırası kullanılıyor): ad'a göre `tr-TR`
  sıralama, varsayılan/öne çıkan öğe en üstte.

### 6.11 Geri dönüş güvenliği — 176 yeri birden etkileyen bileşenin testi

**bUnit birim testleri (`Kuyumcu.Tests`), bileşenin kendisi için:**

| # | Test | Neyi korur |
|---:|---|---|
| 1 | Kapalıyken liste DOM'da yok; `aria-expanded="false"` | Sızan panel |
| 2 | `Enter`/`↓` açar, `Esc` **değeri değiştirmeden** kapatır | Klavye sözleşmesi |
| 3 | `↑`/`↓` `aria-activedescendant`'ı doğru öğeye taşır, uçlarda **durur** | Klavye gezinme |
| 4 | `Enter` seçer, `DegerChanged` **bir kez** tetiklenir | Çift olay |
| 5 | `Secenekler.Count <= 8` → arama kutusu **yok**; `> 8` → **var** | Eşik kuralı |
| 6 | Türkçe: `"ısparta"` → `"Isparta"`; `"IŞIK"` → `"ışık"`; `"ÇİÇEK"` → `"cicek"` bulur | **Türkçe harf duyarlılığı** |
| 7 | Uzak kip: 2 karakterden kısa sorgu **istek atmaz**, sebebi ekranda yazılı | UX-S30 |
| 8 | Uzak kip: geç gelen yanıt **erken yanıtın üstüne yazılmaz** | Yarış koşulu |
| 9 | Sunucu hatası → `<HataBandi>` görünür, **`<BosDurum>` görünmez** | **UX-S28 — hata ≠ boş** |
| 10 | Başarılı + boş yanıt → `<BosDurum>` görünür, hata bandı yok | UX-S28 |
| 11 | `Devredisi=true` + `DevredisiSebebi=null` → **istisna** | md.1: sessiz kilit yasak |
| 12 | `Deger` dışarıdan boşaltılınca bileşen temizlenir | Modal yeniden açılışı |
| 13 | Seçili öğe **✓ ikonu** taşır (yalnız renkle anlatılmaz) | Erişilebilirlik |
| 14 | 200'den çok seçenek yerel kipte → **uyarı basar**, sessiz kırpmaz | UX-S29 |
| 15 | `ElemanId` verilince türetilen id'ler `<ElemanId>-…` kalıbına uyar | Kimlik sözleşmesi |

**Ekran testleri:** göç eden her ekranın mevcut `EkranTestleri` testi, `<select>` yerine
`<Secici>` sorgulayacak şekilde güncellenir — **id'ler değişmediği için seçiciler korunur.**

**`denetim.sh` sayaçları (fail-closed kapılar):**

| Sayaç | Hedef | Bugün |
|---|---:|---:|
| `HAM_SELECT` — `.razor` içinde `<select>` | **0** *(göç sonunda; kilitli dosyalar muaf)* | **176** |
| `SECICI_ID_YOK` | 0 | 6 |
| `SECICI_BOS_SECENEK_TIRE` — `—` kullanan | 0 | 52 |
| `SECIM_SINIFI_GIRDI` — select'e `.girdi` | 0 | 23 |
| `BIRDEN_COK_BIRINCIL` (§ 5.3) | 0 | 22 dosya |

**Mutasyon kanıtı:** `Secici.razor` ve Türkçe normalleştirme yardımcısı için
`dotnet/mut.sh` koşulur; **Türkçe harf haritasındaki her satır** ve **yarış koşulu sıra
numarası** öldürülmeyen mutant bırakmamalıdır — bu ikisi sessizce bozulursa kimse fark etmez.

---

## § 7. UYGULAMA LİSTESİ — SERBEST / KİLİTLİ

**KİLİTLİ (bu turda ve uygulama turunda dokunulmaz, bulgular ilgili P-xx paketine devredilir):**
`Pages/Ayarlar.razor` · `Pages/Faturalar.razor` · `Pages/KarZarar.razor` ·
`Pages/KasaRaporu.razor` · `Pages/Kilavuz.razor` · `Pages/Pos.razor` · `Pages/Raporlar.razor` ·
`Pages/SatisAnalizi.razor` · `Pages/Sayim.razor` · `Pages/Siparisler.razor` ·
`Bilesenler/HataBandi.razor` · `Bilesenler/KabukHatasi.razor`

### 7.1 SERBEST — yapılacaklar (öncelik sırasıyla)

| # | İş | Dosya(lar) | Etki | Emek |
|---:|---|---|---|---|
| **S1** | ⛔ `dugme-benzeri` sınıfını kaldır (kontrast 1.60:1 → 10.17:1) | `Pages/Panel.razor:26` | **kritik** | 1 satır |
| **S2** | ⛔ `catch { moduller = []; }` → hatayı yut**ma**, bandı göster, yeniden dene | `Servisler/YetkiServisi.cs:30` | **kritik** — menü+arama çöküşü | küçük |
| **S3** | Arama panelini açınca odağı kutuya taşı (`OnAfterRenderAsync` + `FocusAsync`) | `Bilesenler/GlobalArama.razor` | yüksek | küçük |
| **S4** | Komut paleti kısayolu ekle (`Ctrl/Cmd+K`) + düğmede ipucu göster | `GlobalArama.razor` + `UstBar.razor` | yüksek | orta |
| **S5** | Arama sonucunda **grup adını** göster, ekran sonucu sınırını (6) söyle | `GlobalArama.razor:72-87` | yüksek | küçük |
| **S6** | Kayıt araması hatası panelin **üstünü kaplamasın** — "Kayıtlar" başlığının altına in | `GlobalArama.razor:64` | yüksek | küçük |
| **S7** | Arama paneli 375px'te **taşıyor** (sağ kenar 628px) — konumu düzelt | `app.css:1112` | yüksek | küçük |
| **S8** | 375px'te "Ara" metni gizlenmiyor — etiketi `<span>`e al | `GlobalArama.razor:36` | orta | 1 satır |
| **S9** | Çekmece açıkken `body { overflow: hidden }` + odağı çekmeceye taşı | `app.css` + `KenarCubugu.razor` | orta | küçük |
| **S10** | **`<Secici>` bileşenini yaz** ve § 6.10'daki 5 dalgada 150 serbest `<select>`i göçür | `Bilesenler/Secici.razor` + 31 dosya | **kurucu isteği** | **büyük** |
| **S11** | `.btn` → `.dugme` tekleştirmesi (§ 5.1), 253 kullanım | 28 dosya | yüksek | büyük |
| **S12** | Sözlükte düğme anahtarı çakışmalarını çöz (`Iptal` 5 → 2 anlam ayrı anahtar) | `Sozlukler.cs` | yüksek | orta |
| **S13** | Ekran başına tek birincil düğme (22 dosya) | 19 serbest dosya | yüksek | orta |
| **S14** | `Storno` / `İptal Et` düğmelerine `.dugme-tehlike` | 8 ekran | yüksek | küçük |
| **S15** | Çift gönderim bayrağı olmayan 19 dosyaya bayrak + `aria-busy` | 19 dosya | **yüksek** (çift fiş) | orta |
| **S16** | `.dugme { min-height: 44px }` + `.dugme-kucuk ≥ 36px` | `app.css` | orta | küçük |
| **S17** | `app.css:230` `.girdi:focus` kuralını kaldır (odak çift sistemi) | `app.css:230` | orta | 1 satır |
| **S18** | Düğme gruplarını `.dugme-satir` içine al; modal alt satırı sabit sıra | ~30 dosya | orta | orta |
| **S19** | i18n dışı 14 düğme metni + emoji/ham ok karakterlerini `<Ikon>`+sözlüğe çevir | 12 dosya | orta | küçük |
| **S20** | Ham `padding`/`gap`/`font-size`/`font-weight` → belirteç (§ 4.2-4.3) | `app.css` | orta | büyük |
| **S21** | `720px` kırılma noktasını `768px`'e taşı | `app.css:137` | düşük | 1 satır |
| **S22** | `--kenar-ayrac` #334155 → #47597a (3.16:1) | `belirtecler.css` | düşük | 1 satır |
| **S23** | Koyu tema tanımının **iki kopyasını** tek kaynağa indir | `belirtecler.css:206-300` | düşük | orta |
| **S24** | `MenuTanimi.cs:81` yorumu "36" → **"38"** (yanlış yorum) | `Layout/MenuTanimi.cs` | düşük | 1 satır |

### 7.2 KİLİTLİ — devredilen bulgular

| Dosya | Bulgu | Devredilecek |
|---|---|---|
| `Pages/Ayarlar.razor` | **6 birincil düğme** (en ağır hiyerarşi ihlali); etiketsiz düğme (`:34`) | P-xx Ayarlar |
| `Pages/Faturalar.razor` | **10 `<select>`** (4'ü BÜYÜK-KÜME: `:158,209,250,415`); 3 birincil; `KayitSecici` kullanıcısı (`:492`) | P-xx Fatura |
| `Pages/Siparisler.razor` | 5 `<select>` (2 BÜYÜK-KÜME); **sınıfsız düğmeler** (`:70,240`); `dugme-benzeri` (`:319`) | P-xx Sipariş |
| `Pages/Raporlar.razor` | 4 `<select>`; satır içi `min-width:320px` (`:167,178`) | P-xx Rapor |
| `Pages/Sayim.razor` | 3 `<select>`; 4 birincil; ham `→` karakteri (`:68`) | P-xx Sayım |
| `Pages/Pos.razor` | 2 `<select>`; biçimsiz sayı basımı (`:104-106`) | P-xx POS |
| `Pages/KarZarar.razor` · `Pages/KasaRaporu.razor` · `Pages/SatisAnalizi.razor` | 1'er `<select>`; birincil düğme yok; `ToString("N0")` `Bicim`i atlıyor | P-xx Rapor seti |
| `Pages/Kilavuz.razor` | `<HataBandi>` kullanmayan tek sayfa | P-xx Kılavuz |
| `Bilesenler/HataBandi.razor` · `Bilesenler/KabukHatasi.razor` | `class="ikincil"` çıplak sistem (`KabukHatasi:32`) | P-xx Kabuk |

---

## § 8. ÖZET — kurucunun üç cümlesine üç cevap

**1. *"Ekranlardaki butonlar…"***
431 düğme **3 ana + 12 ad-hoc** sistemle çiziliyor; `.btn` ve `.dugme` gövde gövde aynı ama
çıplak hâlleri farklı görünüyor. Aynı iş için **5'e kadar farklı etiket** var ve `İptal`
kelimesi hem "vazgeç" hem "kaydı iptal et" anlamında kullanılıyor. **22 ekranda birden fazla
birincil düğme** var, tek ekranda **4 farklı düğme yüksekliği** ölçüldü, hiçbiri 44px'e
ulaşmıyor. Panel'deki asıl düğme ise **okunmuyor (1.60:1)** — tek satırlık bir sınıf çakışması.

**2. *"…menüde arama yapma…"***
Menüde arama kutusu **yok**; arama üst barda bir düğmenin arkasında. Kısayol **yok**
(Cmd/Ctrl+K denendi, açmıyor), panel açılınca odak kutuya **gitmiyor**. Ekran adıyla arama
**aslında çalışıyor** (cari→Cariler, kasa→Kasalar+Kasa Hareketleri, fatura→Faturalar,
kur→Kur+Kuruluş Sihirbazı) ama her denemede üstte kırmızı *"Arama yapılamadı"* bandı
duruyordu. Dahası: yetki isteği başarısız olunca **menü 36→1 bağlantıya düşüyor ve arama
hiçbir ekran bulamıyor** — sessizce, tek bir uyarı bile vermeden.

**3. *"…görseller kurumsal ve şık olmalı"* + *"böyle dropdown olmaz"***
Renk paleti aslında **sağlam**: hesaplanan 27 çiftin tamamı WCAG AA'yı geçiyor, tek gerçek
ihlal yukarıdaki kod kusuru. Asıl sorun **disiplin**: 31 farklı ham dolgu, 27 farklı yazı
boyutu, 4 kırılma noktası. Dropdown'da ise kurucu **teknik olarak haklı** — native `<select>`
açılır listesini işletim sistemi çizer, CSS oraya ulaşamaz; **176 dropdown'un tamamı** bu
yüzden projenin dışında duruyor ve **hiçbirinde arama yok**. Tek çözüm özel bir bileşendir:
§ 6'daki `<Secici>` — tek kaynak, iki kip, 9 renk çifti de AA geçiyor, 5 dalgada 150 serbest
`<select>` göçer, 26 kilitli olan devredilir.

---

*Ölçüm ve yazım: Ertuğ (03 Frontend) · 2026-09-21 · salt okunur tur · Anayasa md.1: ölçülmeyen
hiçbir sayı yazılmadı, ölçülemeyenler "ÖLÇÜLMEDİ (sebep)" olarak § 0'da kayıtlıdır.*
