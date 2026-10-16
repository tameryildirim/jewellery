# FRONTEND POPUP DENETİMİ — 2026-09-21

> **Alan:** açılan pencereler (modal / çekmece / açılır panel) + gereksiz kaydırma çubukları.
> **Tur türü:** **SALT OKUNUR.** Hiçbir kaynak dosyası değiştirilmedi, `dotnet build`/`dotnet test`
> koşulmadı, git işlemi yapılmadı. Arka planda P-22 dalgası aynı ağaca yazıyor.
> **Kurucunun şikâyeti:** *"açılan popupların içerisinde gereksiz scroll barlar vs var"* ·
> *"görseller kurumsal ve şık olmalı"*.

## 0. Ölçüm ortamı ve dürüstlük notları (Anayasa md.1)

| Konu | Gerçek durum |
|---|---|
| Sunucu | `http://localhost:5199` — **DALGA 4 Release anlık görüntüsü**, ağaçtaki en yeni razor/css değişiklikleri burada OLMAYABİLİR. Aşağıdaki CSS/razor kök nedenleri **çalışma ağacındaki kaynaktan** okundu; tarayıcı ölçümleri 5199'dan alındı. İkisi arasındaki fark ölçülmedi. |
| Kiracı | `QA-TEST-FRONTEND` — **veri tabanı boş.** Cari 0, ürün 0, kasa 0, banka hesabı 0, fatura 0 kayıt. Tek dolu ekranlar: Kullanıcılar (1 satır), Vitrin (29 satır). |
| Sonuç | **Satır düzeyi pencereler (Düzenle çekmecesi, Detay, Storno, Pasife Al onayı, Kasa Geçmişi, Fatura Kalemleri, Şube Stokları…) veri olmadığı için AÇILAMADI.** Uydurulmadı; §1'de tek tek sebebiyle işaretlendi. |
| İstek sınırı | Tur boyunca API tekrar tekrar `429 — "Sunucu şu anda çok yoğun (istek sınırı)"` döndü (paralel çalışan diğer ajan + bu tur). Bazı sayfalar bu yüzden hiç yüklenemedi. |
| Ekran görüntüsü | **ALINAMADI.** Tarayıcı paneli gizli olduğu için (`Screenshot timed out: the Browser pane is not displayed`); başka ajanın sekmesi ön planda olduğundan panel öne alınmadı. Rapordaki her sayı DOM ölçümüdür, göz kararı değildir. |
| Tema | Ölçümler **koyu tema** altında yapıldı (`--golge-xl` koyu varyantı okundu: `0 20px 25px -5px rgba(0,0,0,.6)`). Açık temada gölge değeri `rgb(0 0 0 / .2)`'dir; geometri aynıdır. |
| Viewport emülasyonu | Panel gizliyken emülasyon uzun betikler sırasında bir kez kendiliğinden düştü. Bu yüzden **her ölçüm kendi `innerWidth` değerini birlikte kaydeder**; raporda yazan genişlik ölçüm anındaki gerçek `innerWidth`tir. Düşen ölçüm tekrarlandı. |
| Veri güvenliği | Hiçbir Kaydet / Sil / Onayla / Storno / Gönder düğmesine basılmadı. Tetikleyiciler kaynaktan çıkarılan izin listesiyle sınırlandı; pencereler Esc ya da İptal ile kapatıldı. |

---

## 1. Pencere envanteri

### 1.1 Kaynaktaki toplam (çalışma ağacı, `dotnet/Kuyumcu.Web`)

| Desen | Adet | Tanım dosyası |
|---|---|---|
| `<ModalPencere>` (ortada açılan kutu) | **55** | `Bilesenler/ModalPencere.razor` |
| `<Cekmece>` (sağdan gelen panel) | **27** | `Bilesenler/Cekmece.razor` |
| Global arama açılır paneli (`role="dialog"`) | **1** | `Bilesenler/GlobalArama.razor:40` |
| **TOPLAM** | **83** | |

Ham `<dialog>` etiketi ya da üçüncü bir modal deseni **yok** — iyi haber: tek kaynak iki bileşen.
Ancak `Pos.razor:284` ve `PosTahsilatlar.razor:215` `class="modal-alt"` kullanıyor ve
**`.modal-alt` hiçbir CSS dosyasında tanımlı değil** → o iki pencerenin düğme satırı stilsiz.

### 1.2 Bu turda GERÇEKTEN açılıp ölçülen pencereler (17)

| # | Ekran | Tetikleyen düğme (id) | Pencere | Tür | Açıldı mı |
|---|---|---|---|---|---|
| 1 | Cariler | `#cari-yeni-ac` "+ Yeni Cari" | Yeni Cari | modal | ✅ |
| 2 | Ürünler | `#urun-yeni-ac` "+ Yeni Ürün" | Yeni Ürün | modal | ✅ |
| 3 | Kasalar | `#kasa-yeni` "+ Yeni Kasa" | Yeni Kasa | modal | ✅ |
| 4 | Banka | `#banka-yeni` "+ Yeni Hesap" | Yeni Hesap | modal | ✅ |
| 5 | Kullanıcılar | `#k-yeni` "+ Yeni Kullanıcı" | Yeni Kullanıcı | modal | ✅ |
| 6 | Kullanıcılar | satır "Düzenle" (id yok) | Kullanıcı Düzenle | modal | ✅ |
| 7 | Kullanıcılar | satır "Şifre Değiştir" (id yok) | Şifre Değiştir | modal | ✅ |
| 8 | Kur | `#kur-manuel-ac` "✎ Manuel Kur Gir" | Manuel Kur | modal | ✅ |
| 9 | Uyum | `#uy-yeni-bildirim` "+ Yeni bildirim" | Şüpheli İşlem Bildirimi | modal | ✅ |
| 10 | Ürün Ağacı | `#agac-yeni-kategori` "+ Yeni Kategori" | Kategori | çekmece | ✅ |
| 11 | Ürün Ağacı | "+ Yeni Reyon" (id yok) | Reyon | çekmece | ✅ |
| 12 | İşletme | `#i-kimlik-duzenle` | İşletme Kimliği | çekmece | ✅ |
| 13 | İşletme | `#s-yeni-sube-ac` "+ Yeni Şube" | Şube | çekmece | ✅ |
| 14 | İşletme | satır "✏️ Düzenle" | Şube Düzenle | çekmece | ✅ |
| 15 | Vitrin | `#vitrin-duzenle-GRAM` | Pano Kalemi | çekmece | ✅ |
| 16 | Evraklar | `#evrak-yeni` "Evrak gir" | Evrak | çekmece | ✅ |
| 17 | Kabuk (her ekran) | `#ust-arama-ac` | Global arama paneli | açılır panel | ✅ |

**9 modal + 7 çekmece + 1 açılır panel ölçüldü.**

### 1.3 AÇILAMAYANLAR ve sebebi (66 örnek)

| Ekran / pencere | Sebep (ölçülmüş) |
|---|---|
| Emanet → "+ Emanet Al" (`#e-giris-ac`), "↩ İade Et" (`#e-cikis-ac`) | **Tetikleyici `disabled=true`** — ekran "Müşteri seçin" diyor, kiracıda cari yok. |
| Cariler → Düzenle çekmecesi, Detay (sürüklenebilir), Düzeltme, Tahsilat, Veresiye, Döviz veresiye, Döviz tahsilat, Pasife al onayı (8 pencere) | Cari listesi **0 satır** → satır düğmeleri hiç çizilmiyor. |
| Ürünler → Şube Stokları, Ürün Düzenle çekmecesi, Etiket, Sayım Düzelt, Pasife Al (5) | Ürün listesi **0 satır**. |
| Kasalar → Kasa Düzenle, Hareket, Kasa Geçmişi, Storno, Pasife Al, Gün Kapanışı (6) | Kasa **0 satır**. |
| Banka → Hesap Düzenle, Dekont Gir, Faturaya Dönüştür, Ekstre Yükle, Pasife Al, Storno (6) | Hesap **0 satır**; `#bh-dekont-gir` ve `#bh-ekstre-yukle` aktif hesap olmadan çizilmiyor. |
| Vezne → "Yeni fiş" (`#vz-yeni-ac`) | Düğme **pencere açmıyor** — sayfa içi satır-içi form açıyor. Vezne çekmecesi yalnız var olan fişi düzenlerken açılıyor (fiş 0). |
| Vezne iptal, Gider kategori + storno, Gider Pusulaları detay + iptal, Hurda tahlil farkı, Bildirim kapatma, Yedek iptal, POS Tahsilat iptal, Sipariş Detay geçiş onayı, Banka Ekstre onayı, Veri İçe Aktarım onayı (13) | İlgili listeler boş ya da sayfa `429` istek sınırı yüzünden hiç yüklenmedi. |
| Kurulum sihirbazı (`Kurulum.razor:369`, `Acik="true"`) | `/kurulum` **oturum açıkken sihirbazı çizmiyor** — normal kabuk geliyor. |
| Uyum → Kimlik Tespiti çekmecesi, Gönderim onayı, Kapatma onayı, Elle risk (4) | Uyum verisi 0; "Kimlik Tespitleri" bir sekme, pencere değil. |
| Ayarlar (Profil çekmecesi, MFA Kapat, MFA Kurtarma) · Faturalar (4 modal + 1 çekmece) · KasaRaporu (Gün sayımı çekmecesi) · Pos (2 çekmece + 1 modal) · Sayim (1 modal) (13) | **KİLİTLİ EKRANLAR** — bu turda dokunulmadı, bulgular devredilecek (§7). |

---

## 2. Gereksiz kaydırma çubukları — ölçülen

Ölçüm yöntemi (görev tanımındaki seçici, pencerenin kendisi + içindeki her kapsayıcı):
`scrollHeight>clientHeight+1 || scrollWidth>clientWidth+1` **ve** hesaplanmış `overflow` `auto|scroll`.

### 2.1 Bulunan kaydırma alanları

| Pencere | Genişlik × yükseklik (viewport) | Kaydıran kapsayıcı | Dikey taşma | Yatay taşma | İç içe mi | Değerlendirme |
|---|---|---|---|---|---|---|
| **Yeni Cari** (modal) | 1440×900 | `.modal-panel` (pencerenin KENDİSİ) | **+362 px** (2. ölçüm +438 px) | 0 | hayır | ❌ GEREKSİZ — başlık ve düğmeler de kayıyor |
| **Yeni Cari** | 768×1024 | `.modal-panel` | **+559 px** | 0 | hayır | ❌ |
| **Yeni Cari** | 375×812 | `.modal-panel` | **+704 px** | 0 | hayır | ❌ |
| **Yeni Ürün** (modal) | 1440×900 | `.modal-panel` | **+1246 px** | 0 | hayır | ❌ en ağır |
| **Yeni Ürün** | 768×1024 | `.modal-panel` | **+1611 px** | 0 | hayır | ❌ |
| **Yeni Ürün** | 375×812 | `.modal-panel` | **+2029 px** | 0 | hayır | ❌ 731 px'lik pencerede 2760 px içerik = **3,8 ekran** |
| **Yeni Kullanıcı** (modal) | 375×812 | `.modal-panel` | **+22 px** | 0 | hayır | ❌ **1–20 px bandına komşu "gereksiz çubuk"** — 22 px için tam boy çubuk |
| **Yeni Kullanıcı** | 1366×660 (yaygın dükkân PC'si) | `.modal-panel` | **+23 px** | 0 | hayır | ❌ aynı sebep; düğmeler görünür ama çubuk var |
| **Evrak gir** (çekmece) | 375×812 | `.cekmece-govde#cekmece-evrak-govde` | +192 px | 0 | hayır | ✅ DOĞRU — yalnız gövde kayıyor, başlık ve düğmeler sabit |
| Global arama paneli | 1440×900 | — | 0 | 0 | — | ✅ (`ka`/`kas` sorgusunda taşma yok) |

**"Gereksiz" tanımına giren küçük taşma (≤25 px): 2 ölçüm** — Yeni Kullanıcı @375 (+22 px) ve
@1366×660 (+23 px). Bu taşmanın büyük kısmı `.modal-govde`'nin `16px` alt iç boşluğudur:
**16 px'lik nefes payı yüzünden tam boy kaydırma çubuğu çıkıyor.**

### 2.2 İÇ İÇE kaydırma

**Ölçülen 17 pencerenin hiçbirinde iç içe kaydırma yakalanmadı** (hiçbir kapsayıcı `ici:true` dönmedi).
Sebep: ölçülebilen pencereler veri yokluğundan tablosuzdu.

**Ama yapısal risk kaynakta duruyor** (ölçülemedi, veri gerektiriyor):

| Dosya:satır | Pencere | Risk |
|---|---|---|
| `Pages/Faturalar.razor:212-480` | Yeni Fatura | `.modal-panel` (dikey `auto`) İÇİNDE `.tablo-sar` (yatay `auto`) → **iç içe** |
| `Pages/Faturalar.razor:483-584` | Fişten Kes | aynı |
| `Pages/Faturalar.razor:587-690` | Fatura Kalemleri | aynı |
| `Pages/Urunler.razor:538-680` | Şube Stokları | 2 adet `<table>`, `.tablo-sar` **yok** → tablo doğrudan panele taşar, panelin kendi yatay çubuğunu tetikler |
| `Pages/Pos.razor:127-277` | POS Cihaz çekmecesi | `<table>` var, `.tablo-sar` yok → `.cekmece-govde` yatay çubuk alır |

### 2.3 KÖK NEDENLER (dosya:satır)

1. **`wwwroot/css/app.css:319-321`**
   ```
   .modal-panel { … max-width: 32rem; margin: 0 1rem; max-height: 90vh; overflow-y: auto; }
   ```
   **Pencerenin tamamı kaydırma kabıdır.** Başlık (`.modal-baslik`) ve sayfaların gövdeye koyduğu
   düğme satırı bu kabın içindedir → içerik uzayınca ikisi de yukarı/aşağı kayar. Çekmecede
   olduğu gibi "üç parçalı" (sabit başlık · kayan gövde · sabit alt) bir düzen **yok**.
2. **Aynı satır — yatay çubuk bonusu.** CSS şartnamesi gereği bir eksen `visible` değilse diğeri de
   `auto`'ya hesaplanır. **Ölçüldü:** `.modal-panel` hesaplanmış `overflow-x = auto`.
   Yani geniş bir tablo/uzun bir metin panelin tamamına **yatay** çubuk da ekler.
3. **`Bilesenler/ModalPencere.razor:24-26`** — bileşende `.cekmece-alt` karşılığı bir `<footer>` **yok**;
   yalnız `<div class="modal-govde">@ChildContent</div>` var. Her sayfa düğmelerini bu kayan gövdenin
   içine koymak zorunda. (Karşılaştır: `Bilesenler/Cekmece.razor:26/31/34` = `header` + `govde` + `footer`.)
4. **`wwwroot/css/app.css:333`** — `.modal-govde { padding: 1rem 1.5rem; }` → 16 px alt boşluk,
   §2.1'deki 22–23 px'lik "gereksiz çubuk"ların ana bileşeni.
5. **`wwwroot/css/app.css:757`** (karşı örnek, DOĞRU) — `.cekmece-govde { flex:1; overflow:auto; }`
   birlikte `app.css:729-742` `.cekmece{display:flex;flex-direction:column}` → yalnız gövde kayar.
6. **`wwwroot/css/app.css:1042-1043`** — `.kabuk-arama-paneli { max-height:70vh; overflow-y:auto; }`
   (bu turda taşmadı ama aynı "tek parça kayan kap" desenini tekrarlıyor).

---

## 3. Sığmayan pencereler ve gizli kalan düğmeler

Ölçüt: pencere açıldığı anda (kaydırmadan) ilk ve son düğme görünür mü?
`ilkDgGorunur/sonDgGorunur` = düğmenin dikdörtgeni hem viewport'un hem panelin içinde mi.

| Pencere | Viewport | Panel yüksekliği | Ekranı taşıyor mu | İptal görünür | Kaydet görünür | Sonuç |
|---|---|---|---|---|---|---|
| **Yeni Cari** | 1440×900 | 810 px (`max-height:90vh`) | hayır (45→855) | ❌ | ❌ | **Kullanıcı kaydırmadan KAYDEDEMEZ** |
| **Yeni Cari** | 768×1024 | 922 px | hayır | ❌ | ❌ | aynı |
| **Yeni Cari** | 375×812 | 731 px | hayır | ❌ | ❌ | aynı |
| **Yeni Ürün** | 1440×900 | 810 px | hayır | ❌ | ❌ | aynı — 1246 px kaydırma sonrası düğmeler |
| **Yeni Ürün** | 768×1024 | 922 px | hayır | ❌ | ❌ | aynı |
| **Yeni Ürün** | 375×812 | 731 px | hayır | ❌ | ❌ | **en kötü: 2029 px kaydırma** |
| Yeni Kullanıcı | 375×812 / 1366×660 | 731 / 594 px | hayır | ✅ | ✅ | çubuk var ama düğme görünür |
| Yeni Kasa / Yeni Hesap / Manuel Kur / Kullanıcı Düzenle / Şifre Değiştir / Uyum bildirimi | 1440·768·375 | 270–617 px | hayır | ✅ | ✅ | sığıyor |
| 7 çekmecenin tamamı | 1440·768·375 | 900 / 1024 / 812 px (tam boy) | hayır | ✅ | ✅ | **düğmeler her zaman sabit ve görünür** |

**Başlık:** ölçülen hiçbir pencerede başlık açılışta gizli değildi (`basGorunur:true`), ama modalde
başlık `position:static` olduğu için **aşağı kaydırınca başlık ekrandan çıkar** — 2760 px'lik
Yeni Ürün formunda kullanıcı hangi pencerede olduğunu kaybeder.

**Özet: 2 pencere × 3 genişlik = 6 ölçümde birincil eylem (Kaydet) ve İptal açılışta ERİŞİLEMEZ.**
Ölçülemeyen büyük formlar (Ürün Düzenle çekmecesi hariç, Fatura modalleri, Şube Stokları) aynı
desenden beslendiği için aynı riski taşır — **ölçülmedi, tahmin edilmedi, sadece işaretlendi.**

---

## 4. Davranış bulguları

Aşağıdakiler **gerçek klavye/fare girdisiyle** (sentetik değil) doğrulandı; işaretlenenler DOM olayıyla.

| # | Davranış | Ölçülen sonuç | Kök neden |
|---|---|---|---|
| D1 | **Esc kapatıyor mu?** | ✅ **EVET.** Gerçek `Escape` tuşuyla Yeni Cari kapandı. Ölçülen 16 modal/çekmecenin tamamında Esc kapattı. | `ModalPencere.razor:96`, `Cekmece.razor:104` |
| D2 | **Açılışta odak nereye?** | ⚠ **Kapat (×) düğmesine.** Gerçek tıklamayla açılan Yeni Cari'de `document.activeElement = BUTTON.modal-kapat`. | `wwwroot/js/cekmece.js` ilk odaklanabilir öğeyi seçiyor; DOM'da ilk öğe `.modal-kapat` (`ModalPencere.razor:12`). Klavye kullanıcısı forma değil "kapat"a düşüyor. |
| D3 | **Kapanınca odak tetikleyiciye dönüyor mu?** | ❌ **HAYIR.** Gerçek fare tıklamasıyla açılan, gerçek Esc ile kapatılan modal sonrası `activeElement = BODY` (beklenen: `#cari-yeni-ac`). | `ModalPencere.razor:73-77` / `Cekmece.razor:93-97`: kapanış dalında `kuyumcuCekmece.kapat(panelRef)` çağrılıyor **ama `@if (Acik)` bloğu o render'da panelı DOM'dan çoktan çıkarmış**; çağrı düşüyor ve `catch { }` sessizce yutuyor. Sözleşme yazıda var, çalışmada yok. |
| D4 | **Dışarı tıklayınca ne oluyor?** | ❌ **Uyarısız kapanıyor ve girilen veri siliniyor.** Yeni Cari'ye `DENEME-KAYITSIZ` yazıldı, `.modal-arka` tıklandı → pencere kapandı, hiçbir onay çıkmadı; yeniden açıldığında alan boş. Aynısı çekmecede: `.cekmece-arka` tıklaması dolu formu kapattı. | `ModalPencere.razor:6` ve `Cekmece.razor:22` arka planda doğrudan `@onclick="KapatIstegi"`; "kaydedilmemiş değişiklik" kontrolü yok. **Parasal ekranda veri kaybı riski.** |
| D5 | **Panel içine tıklama kapatıyor mu?** | ✅ Hayır, doğru (`@onclick:stopPropagation`, `ModalPencere.razor:9`). | — |
| D6 | **Arka plan sayfa kayıyor mu (scroll lock)?** | ❌ **Kilit YOK.** Modal açıkken `document.body` hesaplanmış `overflow = visible`, `documentElement.scrollHeight > innerHeight` → belge kaydırılabilir durumda. Kaynakta da hiçbir yerde gövde kilidi yok: `app.css` içinde modal açıkken `overflow:hidden` kuralı, `wwwroot/js/*.js` içinde `body.style.overflow` ataması **yok**. | Eksik mekanizma. |
| D7 | **Üst üste iki pencere açılabiliyor mu?** | ❌ **EVET, açılıyor.** Global arama paneli açıkken `#kasa-yeni` tıklandı → `document.querySelectorAll('[role=dialog]').length = 2`; hem `.kabuk-arama-paneli` hem `.modal-panel` aynı anda açık. | Tek bir "aktif yüzen katman" yöneticisi yok. z-index: arama paneli 40, çekmece arka 40 / panel 41, modal 50 → **çekmece açıkken arama paneli çekmecenin altında kalır.** |
| D8 | **Arama paneli sayfa değişiminde kapanıyor mu?** | ❌ **HAYIR.** `/kasalar` → `/cariler` gezinmesinden sonra `.kabuk-arama-paneli` hâlâ açıktı. | `GlobalArama.razor`'da rota değişiminde kapanma yok. |
| D9 | **Arama panelinde Esc çalışıyor mu?** | ❌ Panelde `@onkeydown` yok (`GlobalArama.razor:40-42`); yalnız `#kabuk-arama-kapat` düğmesi kapatıyor (ölçüldü: düğme çalıştı, panel kapandı). | Modal/çekmecenin Esc sözleşmesinin dışında kalmış üçüncü yüzen katman. |

---

## 5. Kurumsal görünüm sapmaları

Hepsi tarayıcıda hesaplanmış (`getComputedStyle`) değerlerdir.

### 5.1 İki desen, iki ayrı dil

| Özellik | **Modal** (`.modal-panel`, 55 örnek) | **Çekmece** (`.cekmece`, 27 örnek) | **Arama paneli** (1) |
|---|---|---|---|
| Genişlik @1440 | **512 px** | **480 px** | **480 px** |
| Genişlik @768 | **744 px** (Kur'da 729 px — çubuk payı) | **768 px** (tam ekran) | — |
| Genişlik @375 | **351 px** | **375 px** (tam ekran) | ~343 px |
| En büyük yükseklik | `90vh` (810 / 922 / 731 px) | `100vh` (sabit tam boy) | `70vh` (630 / 594 px) |
| Köşe yarıçapı | **16 px** | **0 px** | **8 px** |
| Gölge | `0 20px 25px -5px` (`--golge-xl`) | `0 -1px 2px …` (`--golge-yuksek`, ince) | **none** |
| Başlık iç boşluğu | **16 px 24 px** | **12 px 16 px** | 12 px (panel geneli) |
| Gövde iç boşluğu | **16 px 24 px** | **16 px** | 12 px |
| Alt (düğme) alanı | **YOK** — düğmeler gövdenin içinde, kayar | **`<footer class="cekmece-alt">`**, 12 px 16 px, sabit | yok |
| Başlık `h2` | **21 px / 600** | **16 px / 600** | — |
| Kaydırma kabı | pencerenin kendisi | yalnız gövde | pencerenin kendisi |
| z-index | 50 | 40 (perde) / 41 (panel) | 40 |
| Düğme sınıf ailesi | `.btn .btn-birincil / .btn-ikincil` | `.dugme .dugme-birincil / .dugme-ikincil` | `.dugme .dugme-kucuk` |
| **Düğme sırası** | **İptal → Kaydet** (birincil SAĞDA) | **Kaydet → İptal** (birincil SOLDA) | — |

### 5.2 Sayılarla "kaç farklı değer kullanılmış"

| Ölçüt | Farklı değer | Değerler |
|---|---|---|
| Ölçülen pencere genişliği (3 kırılım) | **7** | 351 · 375 · 480 · 512 · 729 · 744 · 768 px |
| Köşe yarıçapı | **3** | 0 px · 8 px · 16 px |
| Gölge | **3** | `--golge-xl` · `--golge-yuksek` · yok |
| Başlık `h2` punto | **2** | 21 px · 16 px |
| Yatay iç boşluk | **3** | 24 px (modal) · 16 px (çekmece) · 12 px (arama) |
| En büyük yükseklik kuralı | **3** | `90vh` · `100vh` · `70vh` |
| Düğme sınıf ailesi | **2** | `.btn-*` · `.dugme-*` |
| **Birincil düğmenin yeri** | **2** | modallerde sağda (43 örnek), Uyum'un 3 modalinde ve **27 çekmecenin tamamında** solda → **30 pencere ters** |
| Düğme satırı kapsayıcı sınıfı (modal) | **≥5** | `satir` (≈26) · `satir-arasi` (≈23) · `dugme-satir` (Uyum, 3) · `satir-eylemler` (2) · **`modal-alt` (Pos + PosTahsilatlar, 2 — CSS'te TANIMSIZ)** |
| Düğmeyi eşit genişlik yapma yöntemi | **3** | satır içi `style="flex:1"` (Cariler/Ürünler'de 14 düğme) · `.esnek` sınıfı (`app.css:180`) · `.esit-pay` (Kullanıcılar) |
| Sürüklenebilir pencere | **1/55** | yalnız `Cariler.razor:811` (Cari Detay) — tek istisna, keşfedilemez |

### 5.3 Öne çıkan sapmalar

- **S1 — Modal dar ekranda GENİŞLİYOR.** 1440'ta 512 px, 768'de **744 px**. Küçük ekranda %45 daha
  geniş pencere. Kök: `app.css:570` `@media (max-width:768px){ .modal-panel{ max-width:none } }`.
  768 px tam da kırılım sınırı olduğu için tablet **modal ile çekmece arasında** kalıyor.
- **S2 — Birincil eylemin yeri ekrana göre değişiyor.** Kuyumcu bir modalde "sağdaki mavi düğme =
  Kaydet" öğreniyor, çekmecede aynı yerde **İptal** buluyor. 30 pencerede ters. Kök:
  `Cekmece.razor:39-49` (Kaydet önce çiziliyor) ⟷ sayfaların modal düzeni.
- **S3 — `.modal-alt` stilsiz.** `Pos.razor:284`, `PosTahsilatlar.razor:215` sınıfı kullanıyor,
  `app.css`/`belirtecler.css` içinde karşılığı yok → o iki pencerenin düğme satırı hizasız.
- **S4 — Satır içi `style="flex:1"`** 14 modal düğmesinde (`Cariler.razor:364,368,574,592,593,655,656,686,687`,
  `Urunler.razor:526,531,1086,1087,1101,1102`) — `app.css:180`'de `.esnek` zaten varken.
- **S5 — Gölge/yarıçap üç ayrı dünyada.** Kurumsal bir ürün üç farklı yüzen katman diline sahip olamaz.

---

## 6. TEK TİP PENCERE ŞARTNAMESİ (öneri — uygulanmadı)

> Amaç: tek bir yüzen katman dili. `ModalPencere` ve `Cekmece` aynı iskeleti paylaşır;
> fark yalnız **konum ve giriş animasyonu**dur.

### 6.1 Ölçü
| Belirteç | Değer | Kullanım |
|---|---|---|
| `--pencere-dar` | `28rem` (448 px) | onay/uyarı, tek alan (Pasife Al, Storno, Gün Kapanışı) |
| `--pencere-orta` | `34rem` (544 px) | standart form (Yeni Kasa, Yeni Hesap, Manuel Kur) |
| `--pencere-genis` | `48rem` (768 px) | tablolu/çok sütunlu (Fatura Kalemleri, Şube Stokları, Yeni Ürün) |
| Dar ekran (`≤640px`) | `width:100vw; height:100dvh; border-radius:0` | pencere **tam ekran sayfaya** dönüşür; ara boy yok |
| 641–1024 px | seçilen boy, `max-width: calc(100vw - 2rem)` | **asla büyümez** — S1 kapanır |
| En büyük yükseklik | `min(90dvh, 48rem)` | `vh` yerine `dvh` (mobil adres çubuğu) |

**Kural:** genişlik penceredeki içeriğe göre bu **üç** değerden biridir; dördüncü değer yasaktır.

### 6.2 İskelet — TEK kaydırma alanı
```
.pencere            { display:flex; flex-direction:column; max-height:min(90dvh,48rem); overflow:clip; }
.pencere-bas        { flex:0 0 auto; }                 /* SABİT — asla kaymaz */
.pencere-govde      { flex:1 1 auto; overflow:auto; overscroll-behavior:contain; }
.pencere-alt        { flex:0 0 auto; }                 /* SABİT — Kaydet her zaman görünür */
```
- **Kaydırılabilir kap sayısı: pencere başına 1** — yalnız `.pencere-govde`.
- `.pencere` üzerinde `overflow:auto` **yasak** (bugünkü `app.css:321` bu yüzden kalkmalı):
  aksi hâlde başlık ve düğmeler kayar, üstelik CSS şartnamesi yatay çubuğu da açar.
- Gövdenin içindeki tablolar `.tablo-sar` ile sarılır **ve** `.tablo-sar` yalnız **yatay** kayar
  (`overflow-x:auto; overflow-y:visible`) → dikey iç içe kaydırma yapısal olarak imkânsız olur.
- Gövde dip boşluğu için `padding-block-end` yerine son çocuğa `margin-block-end:0` + kabın
  `padding-block: var(--bosluk-4)` → 16 px yüzünden çıkan 22–23 px'lik sahte çubuk kapanır.
  Kabul kriteri: **`scrollHeight - clientHeight ≤ 0` ya da `> 48 px`** — arada kalan taşma hatadır.
- Gövde kayarken üst/alt kenara `mask-image` ile 12 px "devam ediyor" tonlaması: kullanıcı
  çubuğa bakmadan daha fazla içerik olduğunu görür.

### 6.3 Düğme sırası ve hizası
- Alt şerit: `display:flex; justify-content:flex-end; gap:var(--bosluk-2)`.
- **Sıra sabit: … ⟨yıkıcı⟩ · İptal · **Kaydet (birincil, EN SAĞDA)**.** Çekmece dâhil.
  (`Cekmece.razor:39-49`'daki Kaydet-önce düzeni ve Uyum'un 3 modali buna çevrilir.)
- Dar ekranda (`≤640px`) düğmeler alt alta, **birincil üstte**, ikisi de tam genişlik.
- Eşit genişlik gerektiğinde tek yöntem: `.esnek`. `style="flex:1"` ve `.esit-pay` kaldırılır.
- Tek düğme ailesi: `.dugme .dugme-birincil / .dugme-ikincil / .dugme-tehlike`. `.btn-*` takma ad kalır.
- Kilitli birincil düğme **sebebini yazar** (çekmecedeki `cekmece-kilit-sebep` sözleşmesi modale de gelir).

### 6.4 Esc / odak / arka plan
| Kural | Bugün | Şartname |
|---|---|---|
| Esc kapatır | ✅ modal+çekmece, ❌ arama paneli | Üç katman da kapanır; tek yardımcı (`kuyumcuCekmece`). |
| Açılışta odak | ❌ Kapat (×) düğmesine | **İlk form alanına**; alan yoksa panelin kendisine. `.pencere-kapat` DOM'da başlığın sonuna alınır ya da `ac()` "kapat"ı atlar. |
| Kapanışta odak | ❌ BODY'ye düşüyor | **Tetikleyiciye döner.** Düzeltme: tetikleyici referansı panel öğesinde değil **bileşen alanında** tutulur; geri verme `Acik=false` render'ından ÖNCE (`OnParametersSetAsync` kapanış dalında) ya da JS tarafında `WeakRef` ile yapılır — `panelRef` DOM'dan çıktıktan sonra kullanılamaz. |
| Dışarı tıklama | ❌ dolu formu uyarısız kapatıyor | Form **kirliyse** (`Kirli=true`) dışarı tıklama kapatmaz; "Değişiklikler kaydedilmedi" onayı çıkar. Salt okunur pencere doğrudan kapanır. |
| Arka plan kaydırma | ❌ kilit yok | Pencere açıkken `document.body { overflow:hidden; padding-right: <çubuk genişliği> }` — sayfanın "zıplamaması" için telafi payı. Kapanışta geri alınır. `overscroll-behavior:contain` ile gövde sonunda sayfa kaydırma zinciri kesilir. |
| Üst üste pencere | ❌ 2 pencere aynı anda | **Tek aktif yüzen katman.** Yeni pencere açılırken önceki kapanır (ya da bilinçli yığın: z-index 50 + `1×derinlik`, perde yalnız en üstte). Arama paneli **rota değişiminde kapanır**. |
| z-index | 40/41/50 dağınık | Tek ölçek: perde `--z-perde:900`, pencere `--z-pencere:901`. |

### 6.5 Boşluk ve görünüm ölçeği (tek değer)
| Öğe | Değer |
|---|---|
| Başlık iç boşluğu | `var(--bosluk-4) var(--bosluk-5)` = 16/24 px |
| Gövde iç boşluğu | `var(--bosluk-5)` = 24 px (dar ekranda `--bosluk-4`) |
| Alt şerit iç boşluğu | `var(--bosluk-4) var(--bosluk-5)`, üstte 1 px `--ayrac`, zemin `--yuzey-2` |
| Alanlar arası | `var(--bosluk-4)` |
| Köşe yarıçapı | `var(--yaricap-xl)` = 16 px (dar ekranda tam ekranda 0) |
| Gölge | `var(--golge-xl)` — **tek** gölge |
| Başlık `h2` | `var(--yazi-lg)` / `var(--kalin)` — modal ve çekmecede aynı (bugün 21 px ⟷ 16 px) |
| Perde | `rgb(0 0 0 / .45)`, `backdrop-filter: blur(2px)` |
| Sürüklenebilirlik | Ya tüm pencerelerde ya hiçbirinde. Tek örnek (`Cariler.razor:811`) ya yaygınlaştırılır ya kaldırılır. |

---

## 7. Uygulama listesi — SERBEST / KİLİTLİ

### 7.1 SERBEST (bu ajanın alanı, 36 ekran kapsamında)

| # | İş | Dosya | Etki |
|---|---|---|---|
| U1 | `.modal-panel`'den `overflow-y:auto` + `max-height` kaldır; üç parçalı flex iskelet kur | `app.css:319-321` | 55 modal |
| U2 | `ModalPencere.razor`'a `<footer class="modal-alt">` + `AltIcerik` RenderFragment ekle | `ModalPencere.razor:24-26` | 55 modal |
| U3 | Sayfaların düğme satırlarını gövdeden `AltIcerik`'e taşı, sırayı **İptal → Kaydet** yap | 26 sayfa | 43+3 modal |
| U4 | `Cekmece.razor` alt şeridinde Kaydet'i **İptal'den sonraya** al | `Cekmece.razor:39-49` | 27 çekmece |
| U5 | `.modal-alt` sınıfını CSS'te tanımla (ya da U2 ile birlikte tek yere bağla) | `app.css` + `Pos.razor:284`, `PosTahsilatlar.razor:215` (*Pos KİLİTLİ — yalnız CSS tarafı serbest*) | 2 pencere |
| U6 | Genişliği üç belirtece indir; `@media (max-width:768px){max-width:none}` kaldır, `≤640px` tam ekran | `app.css:570` | tüm modaller |
| U7 | Gövde dip boşluğunu düzelt (22–23 px sahte çubuk) | `app.css:333` | ölçülen 2 vaka |
| U8 | Odak: açılışta ilk **form alanına** (Kapat'a değil) | `cekmece.js` `ac()` + `ModalPencere.razor:12` | tümü |
| U9 | Odak: kapanışta tetikleyiciye dönüş — `panelRef` yerine bileşen alanında saklanan referans | `ModalPencere.razor:73-77`, `Cekmece.razor:93-97` | tümü |
| U10 | Arka plan kaydırma kilidi (`body overflow:hidden` + çubuk telafisi) | yeni JS + iki bileşen | tümü |
| U11 | Dışarı tıklamada "kaydedilmemiş değişiklik" koruması | `ModalPencere.razor:6`, `Cekmece.razor:22` | tümü |
| U12 | Tek aktif yüzen katman; arama paneli rota değişiminde kapanır; Esc kazanır | `GlobalArama.razor:40`, z-index ölçeği | 3 katman |
| U13 | `style="flex:1"` → `.esnek`; `.esit-pay` birleştir | `Cariler.razor` (9), `Urunler.razor` (6) | 14 düğme |
| U14 | Modal içi tabloları `.tablo-sar` ile sar, `.tablo-sar`'ı yalnız yatay yap | `Urunler.razor:538-680`, `app.css:258` | iç içe kaydırmayı yapısal olarak kapatır |
| U15 | Çekmece ile modalin başlık puntosunu/boşluğunu eşitle | `app.css:323-333` ⟷ `743-763` | görsel birlik |

### 7.2 KİLİTLİ — dokunulmadı, bulgu devredilir

| Ekran / bileşen | Devredilen bulgu |
|---|---|
| **Ayarlar** | Profil çekmecesi + MFA Kapat + MFA Kurtarma modalleri ölçülemedi; U1–U15 bu ekrana da uygulanmalı. |
| **Faturalar** | `Faturalar.razor:212-480`, `483-584`, `587-690` → `.modal-panel` (dikey auto) içinde `.tablo-sar` (yatay auto) = **iç içe kaydırma**, yapısal. 4 modal + 1 çekmece. Ayrıca düğme satırı 17 kez `satir-arasi`, 3 kez `satir` — tek ekranda iki farklı sınıf. |
| **Pos** | `Pos.razor:284` **`modal-alt` CSS'te tanımsız**; `Pos.razor:127-277` çekmecesinde `.tablo-sar`sız `<table>`. |
| **KarZarar · KasaRaporu · Raporlar · SatisAnalizi · Kilavuz** | `KasaRaporu.razor:144` gün sayımı çekmecesi ölçülemedi; diğerlerinde pencere yok ama aynı belirteç ölçeği geçerli. |
| **Sayim** | `Sayim.razor:182` iptal onayı — İptal-önce sırası, U3 kapsamına alınmalı. |
| **Siparisler** | `SiparisDetay.razor:288` geçiş onayı modali. *(Not: kilit listesinde "Siparisler" yazıyor; `SiparisDetay.razor` ayrı dosya — belirsizlik kurucuya/orkestratöre bırakıldı, bu turda ikisine de dokunulmadı.)* |
| **`Bilesenler/HataBandi.razor`, `Bilesenler/KabukHatasi.razor`** | Pencere içermiyor; ölçüm sırasında yalnız `429` bandı olarak göründü. |

---

## 8. Kanıt özeti

- Açılan ve ölçülen pencere: **17** (9 modal · 7 çekmece · 1 açılır panel) / kaynaktaki 83.
- Gereksiz kaydırma çubuğu ölçümü: **8** (2 pencere × 3 genişlik + Yeni Kullanıcı × 2 kırılım).
- İç içe kaydırma: ölçülen pencerelerde **0**; kaynakta yapısal risk **5 pencere**.
- Açılışta kaydetme düğmesi erişilemeyen pencere: **2** (Yeni Cari, Yeni Ürün) — her üç genişlikte de.
- Davranış kusuru: **D2, D3, D4, D6, D7, D8, D9** (7 kalem).
- Kurumsal sapma: **7 genişlik · 3 yarıçap · 3 gölge · 2 punto · 2 düğme ailesi · 30 pencerede ters düğme sırası · 5 düğme-satırı sınıfı (1'i tanımsız) · 3 eşit-genişlik yöntemi.**
